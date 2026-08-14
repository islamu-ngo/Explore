// ABOUTME: Evaluates HATEOAS link visibility by batching authorization checks with deduplication.
// ABOUTME: Static checks (auth, roles, conditions) run first; permission-bound links are batch-evaluated via IAuthorizationProvider.

namespace Explore.API.Hateoas;

using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Hateoas;
using Explore.Domain;
using Explore.Domain.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public sealed class HateoasAuthorizationEvaluator : IHateoasAuthorizationEvaluator
{
    private static readonly ActivitySource HateoasAuthorizationSource = new("Explore.Hateoas.Authorization");

    private readonly IAuthorizationProvider _authorizationProvider;
    private readonly IEventRepository _eventRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<HateoasAuthorizationEvaluator> _logger;

    public HateoasAuthorizationEvaluator(
        IAuthorizationProvider authorizationProvider,
        IEventRepository eventRepository,
        ITenantContext tenantContext,
        ILogger<HateoasAuthorizationEvaluator> logger)
    {
        _authorizationProvider = authorizationProvider;
        _eventRepository = eventRepository;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates which links are allowed for the current user.
    /// Flow: static checks → build normalized checks → deduplicate → batch evaluate → map decisions back.
    /// Fail-closed: batch failure denies all permission-bound links.
    /// </summary>
    public async Task<IReadOnlyList<bool>> AreLinksAllowedAsync(
        IReadOnlyList<LinkDefinition> definitions,
        ClaimsPrincipal? user,
        HttpContext httpContext)
    {
        if (definitions.Count == 0)
            return [];

        var results = new bool[definitions.Count];
        var pendingChecks = new List<(int Index, AuthorizationRequest Check, string Key)>();

        // Phase 1: Static checks (no provider call needed)
        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            if (!PassesStaticChecks(definition, user))
            {
                results[i] = false;
                continue;
            }

            if (RequiresExplicitPermissionAction(definition))
            {
                _logger.LogWarning(
                    "Link '{Rel}' for resource '{ResourceKind}' is permission-bound but has no explicit action. Denying link.",
                    definition.Rel,
                    definition.PermissionResourceKind);
                results[i] = false;
                continue;
            }

            var check = BuildCheck(definition);
            if (check is null)
            {
                results[i] = true;
                continue;
            }

            pendingChecks.Add((i, check, check.ToDeduplicationKey()));
        }

        if (pendingChecks.Count == 0)
            return results;

        try
        {
            pendingChecks = await EnrichRegistrationFormChecksAsync(
                pendingChecks,
                results,
                httpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HATEOAS trusted registration-form context resolution failed; denying permission-bound links.");
            return results;
        }

        if (pendingChecks.Count == 0)
            return results;

        // Phase 2: Deduplicate — collapse identical checks before provider invocation
        var uniqueChecks = new List<AuthorizationRequest>();
        var keyToDecisionIndex = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var (_, check, key) in pendingChecks)
        {
            if (keyToDecisionIndex.ContainsKey(key))
                continue;

            keyToDecisionIndex[key] = uniqueChecks.Count;
            uniqueChecks.Add(check);
        }

        var deduplicatedCount = pendingChecks.Count - uniqueChecks.Count;
        if (deduplicatedCount > 0)
        {
            _logger.LogDebug(
                "HATEOAS authorization dedup: {InputCount} checks reduced to {UniqueCount} unique ({DeduplicatedCount} duplicates removed).",
                pendingChecks.Count,
                uniqueChecks.Count,
                deduplicatedCount);
        }

        // Phase 3: Batch evaluate unique checks with telemetry
        using var activity = HateoasAuthorizationSource.StartActivity("hateoas.capability_planning");
        activity?.SetTag("checks.total", pendingChecks.Count);
        activity?.SetTag("checks.unique", uniqueChecks.Count);
        activity?.SetTag("checks.deduplicated", deduplicatedCount);

        try
        {
            var allowed = await _authorizationProvider.AuthorizeBatchAsync(uniqueChecks);

            // Phase 4: Map decisions back to all original link indices via dedup key
            foreach (var (index, _, key) in pendingChecks)
            {
                var decisionIndex = keyToDecisionIndex[key];
                results[index] = decisionIndex < allowed.Count && allowed[decisionIndex].IsAllowed;
            }

            activity?.SetTag("outcome", "success");
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HATEOAS batch authorization failed; denying all {Count} permission-bound links (fail-closed).", pendingChecks.Count);
            activity?.SetTag("outcome", "fail_closed");

            foreach (var (index, _, _) in pendingChecks)
            {
                results[index] = false;
            }

            return results;
        }
    }

    private static bool PassesStaticChecks(LinkDefinition definition, ClaimsPrincipal? user)
    {
        if (definition.Condition is not null && !definition.Condition())
            return false;

        if (definition.RequiresAuth &&
            user?.Identity?.IsAuthenticated != true &&
            !definition.AdvertiseWhenAnonymous)
        {
            return false;
        }

        if (definition.RequiredRoles is { Length: > 0 })
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var hasRequiredRole = definition.RequiredRoles.Any(user.IsInRole);
            if (!hasRequiredRole)
                return false;
        }

        return true;
    }

    private AuthorizationRequest? BuildCheck(LinkDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.PermissionResourceKind))
            return null;

        var action = definition.PermissionAction;
        Debug.Assert(!string.IsNullOrWhiteSpace(action), "Permission-bound links must be screened before BuildCheck.");

        var resourceId = definition.PermissionResourceId
            ?? ExtractResourceId(definition.RouteValues)
            ?? definition.RouteName;

        var attrs = definition.PermissionResourceAttributes;
        return new AuthorizationRequest(definition.PermissionResourceKind, resourceId, action, attrs, definition.PermissionScope);
    }

    private static bool RequiresExplicitPermissionAction(LinkDefinition definition) =>
        !string.IsNullOrWhiteSpace(definition.PermissionResourceKind) &&
        string.IsNullOrWhiteSpace(definition.PermissionAction);

    private async Task<List<(int Index, AuthorizationRequest Check, string Key)>> EnrichRegistrationFormChecksAsync(
        List<(int Index, AuthorizationRequest Check, string Key)> pendingChecks,
        bool[] results,
        CancellationToken cancellationToken)
    {
        Guid[] eventIds = pendingChecks
            .Where(item => item.Check.ResourceKind == ResourceKinds.RegistrationForm)
            .Select(item => EventId(item.Check.ResourceAttributes))
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if (!pendingChecks.Any(item => item.Check.ResourceKind == ResourceKinds.RegistrationForm))
            return pendingChecks;
        if (eventIds.Length > IEventRepository.MaximumAuthorizationTargetBatchSize)
            throw new InvalidOperationException("Registration-form HAL event batch exceeds the authorization lookup bound.");

        IReadOnlyDictionary<Guid, Event> events = eventIds.Length == 0
            ? new Dictionary<Guid, Event>()
            : (await _eventRepository.GetAuthorizationTargetsByIdsAsync(eventIds, cancellationToken))
                .Where(item => item.TenantId == _tenantContext.TenantId)
                .ToDictionary(item => item.Id);
        var enriched = new List<(int Index, AuthorizationRequest Check, string Key)>(pendingChecks.Count);
        foreach ((int index, AuthorizationRequest check, string key) in pendingChecks)
        {
            if (check.ResourceKind != ResourceKinds.RegistrationForm)
            {
                enriched.Add((index, check, key));
                continue;
            }

            if (!events.TryGetValue(EventId(check.ResourceAttributes), out Event? eventEntity))
            {
                results[index] = false;
                continue;
            }

            var attributes = check.ResourceAttributes is null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(check.ResourceAttributes);
            foreach (string authorityKey in AuthorityAttributeKeys)
                attributes.Remove(authorityKey);
            attributes["eventId"] = eventEntity.Id.ToString("D");
            attributes["tenantId"] = eventEntity.TenantId.ToString("D");
            Add(attributes, "actorId", eventEntity.ActorId);
            Add(attributes, "userId", eventEntity.Actor?.UserId);
            Add(attributes, "organizationId", eventEntity.Actor?.OrganizationId);
            Add(attributes, "groupId", eventEntity.Actor?.GroupId);
            Add(attributes, "organizerActorId", eventEntity.OrganizerActorId);
            Add(attributes, "organizerUserId", eventEntity.OrganizerActor?.UserId);
            Add(attributes, "organizerOrganizationId", eventEntity.OrganizerActor?.OrganizationId);
            Add(attributes, "organizerGroupId", eventEntity.OrganizerActor?.GroupId);
            var trusted = check with { ResourceAttributes = attributes };
            enriched.Add((index, trusted, trusted.ToDeduplicationKey()));
        }

        return enriched;
    }

    private static Guid EventId(IReadOnlyDictionary<string, object>? attributes) =>
        attributes?.TryGetValue("eventId", out object? value) == true
            && Guid.TryParse(value?.ToString(), out Guid eventId)
                ? eventId
                : Guid.Empty;

    private static void Add(Dictionary<string, object> attributes, string key, Guid? value)
    {
        if (value is { } id)
            attributes[key] = id.ToString("D");
    }

    private static readonly string[] AuthorityAttributeKeys =
    [
        "tenantId", "eventId", "actorId", "userId", "organizationId", "groupId",
        "organizerActorId", "organizerUserId", "organizerOrganizationId", "organizerGroupId"
    ];

    private static string? ExtractResourceId(object? routeValues)
    {
        if (routeValues is null)
            return null;

        if (routeValues is IReadOnlyDictionary<string, object> readOnlyDictionary)
            return TryGetId(readOnlyDictionary.ToDictionary(x => x.Key, x => (object?)x.Value));

        if (routeValues is IDictionary<string, object> dictionary)
            return TryGetId(dictionary.ToDictionary(x => x.Key, x => (object?)x.Value));

        var values = routeValues
            .GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(x => x.Name, x => x.GetValue(routeValues));

        return TryGetId(values);
    }

    private static string? TryGetId(IReadOnlyDictionary<string, object?> values)
    {
        return TryGet(values, "id")
            ?? TryGet(values, "tenantId")
            ?? TryGet(values, "organizationId")
            ?? TryGet(values, "did")
            ?? TryGet(values, "userId");
    }

    private static string? TryGet(IReadOnlyDictionary<string, object?> values, string key)
    {
        foreach (var pair in values)
        {
            if (!pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                continue;

            return pair.Value?.ToString();
        }

        return null;
    }
}
