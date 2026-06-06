// ABOUTME: Builds a Cerbos SDK Principal for either a human user or an API-key-authenticated machine caller.
// ABOUTME: Centralises principal shape so Cerbos policies see the same attribute contract regardless of authentication scheme.

using Cerbos.Sdk.Builder;
using Explore.Application.Authentication;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Services;
using Explore.Domain.Enums;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Constructs the <see cref="Principal"/> sent to the Cerbos PDP for authorization checks.
/// Supports two principal flavours:
/// <list type="bullet">
/// <item><description>User principals — resolved from <see cref="IAdminContext"/> (instance-admin flag, tenant and organization memberships).</description></item>
/// <item><description>Machine principals — resolved from <see cref="IMachinePrincipalAccessor"/> with attributes projected from the API key owner type, synthesising equivalent membership maps so existing Cerbos policies evaluate consistently.</description></item>
/// </list>
/// The builder is the single translation layer: authorization services always call <see cref="BuildPrincipalAsync(System.Nullable{System.Guid}, System.Threading.CancellationToken)"/> and let the builder decide which branch to take.
/// <para>
/// Event-scoped role assignments are hydrated on demand via <see cref="EnrichWithEventAssignmentsAsync"/>
/// so Cerbos derived roles can evaluate event-team authority without a second round-trip.
/// </para>
/// </summary>
public class CerbosPrincipalBuilder
{
    private readonly IAdminContext _adminContext;
    private readonly IMachinePrincipalAccessor _machinePrincipalAccessor;
    private readonly IEventAuthoritySnapshotService _eventAuthoritySnapshotService;

    public CerbosPrincipalBuilder(
        IAdminContext adminContext,
        IMachinePrincipalAccessor machinePrincipalAccessor,
        IEventAuthoritySnapshotService eventAuthoritySnapshotService)
    {
        _adminContext = adminContext;
        _machinePrincipalAccessor = machinePrincipalAccessor;
        _eventAuthoritySnapshotService = eventAuthoritySnapshotService;
    }

    /// <summary>
    /// Dispatches to the machine or user principal builder based on whether the current request
    /// was authenticated via an external API key. Call this from authorization services; it
    /// removes the need for callers to check <see cref="IMachinePrincipalAccessor.IsMachineCaller"/>.
    /// </summary>
    /// <param name="userId">The user ID resolved from <see cref="IAdminContext"/>. May be <c>null</c> for machine callers whose API key is not owned by a user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fully-populated Cerbos <see cref="Principal"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the caller is neither a recognised user nor a machine principal.</exception>
    public Task<Principal> BuildPrincipalAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        var machineContext = _machinePrincipalAccessor.Current;
        if (machineContext is not null)
            return BuildMachinePrincipalAsync(machineContext, cancellationToken);

        if (userId is null)
            throw new InvalidOperationException(
                "Cannot build a Cerbos principal: no user ID is available and the request is not an API-key (machine) caller.");

        return BuildSdkPrincipalAsync(userId.Value, cancellationToken);
    }

    /// <summary>
    /// Builds a Cerbos SDK <see cref="Principal"/> for the given user by querying their
    /// administrative authority across instance, tenant, and organization scopes.
    /// </summary>
    public async Task<Principal> BuildSdkPrincipalAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(cancellationToken);
        var adminTenantIds = await _adminContext.GetAdminTenantIdsAsync(cancellationToken);
        var adminOrgIds = await _adminContext.GetAdminOrganizationIdsAsync(cancellationToken);

        var tenantMemberships = adminTenantIds
            .ToDictionary(id => id.ToString(), _ => AttributeValue.StringValue("admin"));
        var orgMemberships = adminOrgIds
            .ToDictionary(id => id.ToString(), _ => AttributeValue.StringValue("admin"));

        return Principal
            .NewInstance(userId.ToString(), "islamuevent_authenticated_user")
            .WithAttribute("isInstanceAdmin", AttributeValue.BoolValue(isInstanceAdmin))
            .WithAttribute("tenantMemberships", AttributeValue.MapValue(tenantMemberships))
            .WithAttribute("orgMemberships", AttributeValue.MapValue(orgMemberships));
    }

    /// <summary>
    /// Builds a Cerbos SDK <see cref="Principal"/> for an API-key-authenticated machine caller.
    /// Projects owner-type authority into the same <c>isInstanceAdmin</c>, <c>tenantMemberships</c>,
    /// and <c>orgMemberships</c> shape used for human principals so existing policies are reusable.
    /// Adds machine-specific attributes (<c>is_machine</c>, <c>api_key_id</c>, <c>owner_type</c>,
    /// <c>owner_id</c>, <c>tenant_id</c>, <c>scopes</c>) so policies can tighten rules when desired.
    /// </summary>
    public async Task<Principal> BuildMachinePrincipalAsync(
        ApiKeyPrincipalContext machineContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(machineContext);

        var (isInstanceAdmin, tenantMemberships, orgMemberships) =
            await ResolveMachineAuthorityAsync(machineContext, cancellationToken);

        var scopeAttrs = machineContext.Scopes
            .Select(AttributeValue.StringValue)
            .ToArray();

        var principal = Principal
            .NewInstance($"api_key:{machineContext.KeyId}", "islamuevent_authenticated_user")
            .WithAttribute("isInstanceAdmin", AttributeValue.BoolValue(isInstanceAdmin))
            .WithAttribute("tenantMemberships", AttributeValue.MapValue(tenantMemberships))
            .WithAttribute("orgMemberships", AttributeValue.MapValue(orgMemberships))
            .WithAttribute("is_machine", AttributeValue.BoolValue(true))
            .WithAttribute("api_key_id", AttributeValue.StringValue(machineContext.KeyId))
            .WithAttribute("owner_type", AttributeValue.StringValue(machineContext.OwnerType.ToString().ToLowerInvariant()))
            .WithAttribute("owner_id", AttributeValue.StringValue(machineContext.OwnerId.ToString()))
            .WithAttribute("scopes", AttributeValue.ListValue(scopeAttrs));

        if (machineContext.TenantId is { } tenantId)
            principal = principal.WithAttribute("tenant_id", AttributeValue.StringValue(tenantId.ToString()));

        return principal;
    }

    private async Task<(bool IsInstanceAdmin, Dictionary<string, AttributeValue> TenantMemberships, Dictionary<string, AttributeValue> OrgMemberships)>
        ResolveMachineAuthorityAsync(ApiKeyPrincipalContext context, CancellationToken cancellationToken)
    {
        switch (context.OwnerType)
        {
            case ExternalApiKeyOwnerType.InstanceAdmin:
                return (true, new Dictionary<string, AttributeValue>(), new Dictionary<string, AttributeValue>());

            case ExternalApiKeyOwnerType.Tenant:
                var tenantMap = new Dictionary<string, AttributeValue>();
                if (context.TenantId is { } tenantTarget)
                    tenantMap[tenantTarget.ToString()] = AttributeValue.StringValue("admin");
                return (false, tenantMap, new Dictionary<string, AttributeValue>());

            case ExternalApiKeyOwnerType.Organization:
                var orgMap = new Dictionary<string, AttributeValue>
                {
                    [context.OwnerId.ToString()] = AttributeValue.StringValue("admin"),
                };
                return (false, new Dictionary<string, AttributeValue>(), orgMap);

            case ExternalApiKeyOwnerType.Group:
                // Cerbos policies do not currently express group-admin roles directly;
                // scope gating in FallbackAuthorizationService and MachineScopeMapping covers
                // group resource authority. We still emit the attributes above so a future
                // group-admin policy can key off group ownership via owner_type/owner_id.
                return (false, new Dictionary<string, AttributeValue>(), new Dictionary<string, AttributeValue>());

            case ExternalApiKeyOwnerType.User:
                // Machine principal owned by a user borrows the owner's actual authority profile.
                var ownerIsInstanceAdmin = await _adminContext.IsInstanceAdminAsync(context.OwnerId, cancellationToken);
                var ownerTenantIds = await _adminContext.GetAdminTenantIdsAsync(context.OwnerId, cancellationToken);
                var ownerOrgIds = await _adminContext.GetAdminOrganizationIdsAsync(context.OwnerId, cancellationToken);

                var userTenantMap = ownerTenantIds
                    .ToDictionary(id => id.ToString(), _ => AttributeValue.StringValue("admin"));
                var userOrgMap = ownerOrgIds
                    .ToDictionary(id => id.ToString(), _ => AttributeValue.StringValue("admin"));
                return (ownerIsInstanceAdmin, userTenantMap, userOrgMap);

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(context),
                    context.OwnerType,
                    "Unknown ExternalApiKeyOwnerType — cannot project to Cerbos authority.");
        }
    }

    public async Task EnrichWithEventAssignmentsAsync(
        Principal principal,
        Guid userId,
        Guid tenantId,
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken ct)
    {
        if (eventIds.Count == 0)
            return;

        var snapshot = await _eventAuthoritySnapshotService.GetForUserAndEventsAsync(
            tenantId, userId, eventIds, ct);

        if (snapshot.Events.Count == 0)
            return;

        var eventAssignments = new Dictionary<string, AttributeValue>();
        foreach (var (eventId, authority) in snapshot.Events)
        {
            var roles = authority.RoleCodes
                .Select(code => AttributeValue.StringValue(code))
                .ToArray();

            var permissions = authority.PermissionCodes
                .Select(code => AttributeValue.StringValue(code))
                .ToArray();

            eventAssignments[eventId.ToString()] = AttributeValue.MapValue(new Dictionary<string, AttributeValue>
            {
                ["tenantId"] = AttributeValue.StringValue(tenantId.ToString()),
                ["roles"] = AttributeValue.ListValue(roles),
                ["permissions"] = AttributeValue.ListValue(permissions)
            });
        }

        principal.WithAttribute("eventAssignments", AttributeValue.MapValue(eventAssignments));
        principal.WithAttribute("nowUtc", AttributeValue.StringValue(DateTime.UtcNow.ToString("o")));
    }
}
