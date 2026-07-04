// ABOUTME: MediatR pipeline behavior that enforces authorization before command execution.
// ABOUTME: Primary path: [AuthorizeResource] + optional ISecureRequest. Legacy path: IAuthorizedRequest (deprecated, zero production usages).

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that enforces authorization before command handlers execute.
/// Inspects requests for either the <see cref="IAuthorizedRequest"/> interface or the
/// <see cref="AuthorizeResourceAttribute"/> to determine authorization requirements.
/// If authorization returns deny, throws <see cref="AuthorizationException"/> (mapped to HTTP 403).
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, AuthorizeResourceAttribute?> AttributeCache = new();
    private static readonly ActivitySource AuthorizationActivitySource = new("Explore.Authorization");

    private readonly IAuthorizationProvider _authorizationProvider;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;
    private readonly IEventRepository? _eventRepository;
    private readonly IOrganizationMemberRepository? _organizationMemberRepository;
    private readonly IStorageObjectRepository? _storageObjectRepository;
    private readonly IEventSessionRepository? _eventSessionRepository;

    public AuthorizationBehavior(
        IAuthorizationProvider authorizationProvider,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger,
        IEventRepository? eventRepository = null,
        IOrganizationMemberRepository? organizationMemberRepository = null,
        IStorageObjectRepository? storageObjectRepository = null,
        IEventSessionRepository? eventSessionRepository = null)
    {
        _authorizationProvider = authorizationProvider;
        _logger = logger;
        _eventRepository = eventRepository;
        _organizationMemberRepository = organizationMemberRepository;
        _storageObjectRepository = storageObjectRepository;
        _eventSessionRepository = eventSessionRepository;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Path 1 (Legacy): Request implements IAuthorizedRequest directly — deprecated, zero production usages.
        // Retained for backward compatibility. New commands should use [AuthorizeResource] attribute instead.
#pragma warning disable CS0618 // IAuthorizedRequest is obsolete — bridge code must still support it
        if (request is IAuthorizedRequest authorizedRequest)
        {
            await EnforceAuthorizationAsync(
                authorizedRequest.ResourceKind,
                authorizedRequest.ResourceId,
                authorizedRequest.Action,
                authorizedRequest.ResourceAttributes,
                typeof(TRequest).Name,
                cancellationToken);

            return await next(cancellationToken);
        }
#pragma warning restore CS0618

        // Path 2: Request class has [AuthorizeResource] attribute (cached per type)
        var attribute = AttributeCache.GetOrAdd(
            typeof(TRequest),
            static t => t.GetCustomAttribute<AuthorizeResourceAttribute>());
        if (attribute is not null)
        {
            // If request also implements ISecureRequest, pull dynamic resource context from the instance
            var resourceId = (request is ISecureRequest secureRequest && secureRequest.ResourceId is not null)
                ? secureRequest.ResourceId
                : typeof(TRequest).Name;

            var resourceAttributes = (request is ISecureRequest sr)
                ? sr.ResourceAttributes
                : null;
            resourceAttributes = await EnrichResourceAttributesAsync(
                attribute.Resource,
                resourceId,
                resourceAttributes,
                cancellationToken);

            await EnforceAuthorizationAsync(
                attribute.Resource,
                resourceId,
                attribute.Action,
                resourceAttributes,
                typeof(TRequest).Name,
                cancellationToken);

            return await next(cancellationToken);
        }

        // No authorization requirements — pass through
        return await next(cancellationToken);
    }

    private async Task<IDictionary<string, object>?> EnrichResourceAttributesAsync(
        string resourceKind,
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        return resourceKind switch
        {
            ResourceKinds.Event => await EnrichEventResourceAttributesAsync(resourceId, resourceAttributes, cancellationToken),
            ResourceKinds.OrganizationMember => await EnrichOrganizationMemberResourceAttributesAsync(resourceId, resourceAttributes, cancellationToken),
            ResourceKinds.StorageObject => await EnrichStorageObjectResourceAttributesAsync(resourceId, resourceAttributes, cancellationToken),
            ResourceKinds.CustomPropertyProjection => await EnrichCustomPropertyProjectionResourceAttributesAsync(resourceAttributes, cancellationToken),
            _ => resourceAttributes
        };
    }

    private async Task<IDictionary<string, object>?> EnrichCustomPropertyProjectionResourceAttributesAsync(
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (resourceAttributes is null || resourceAttributes.ContainsKey("tenantId"))
            return resourceAttributes;

        if (TryGetGuidAttribute(resourceAttributes, "eventId", out var eventId))
        {
            if (_eventRepository is null)
                return resourceAttributes;

            var eventEntity = await _eventRepository.GetEventWithDetails(eventId);
            if (eventEntity is null)
                return resourceAttributes;

            var enriched = new Dictionary<string, object>(resourceAttributes);
            AddIfMissing(enriched, "eventId", eventEntity.Id.ToString("D"));
            AddIfMissing(enriched, "tenantId", eventEntity.TenantId.ToString("D"));
            return enriched;
        }

        if (TryGetGuidAttribute(resourceAttributes, "eventSessionId", out var eventSessionId))
        {
            if (_eventSessionRepository is null)
                return resourceAttributes;

            var session = await _eventSessionRepository.GetSessionWithDetails(eventSessionId);
            if (session is null)
                return resourceAttributes;

            var enriched = new Dictionary<string, object>(resourceAttributes);
            AddIfMissing(enriched, "eventSessionId", session.Id.ToString("D"));
            AddIfMissing(enriched, "eventId", session.EventId.ToString("D"));
            AddIfMissing(enriched, "tenantId", session.TenantId.ToString("D"));
            return enriched;
        }

        return resourceAttributes;
    }

    private async Task<IDictionary<string, object>?> EnrichEventResourceAttributesAsync(
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (_eventRepository is null ||
            !Guid.TryParse(resourceId, out var eventId))
        {
            return resourceAttributes;
        }

        if (HasEventAuthorizationContext(resourceAttributes))
            return resourceAttributes;

        var eventEntity = await _eventRepository.GetEventWithDetails(eventId);
        if (eventEntity is null)
            return resourceAttributes;

        var enriched = resourceAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(resourceAttributes);

        AddIfMissing(enriched, "eventId", eventEntity.Id.ToString());
        AddIfMissing(enriched, "tenantId", eventEntity.TenantId.ToString());
        AddIfMissing(enriched, "actorId", eventEntity.ActorId.ToString());
        AddIfMissing(enriched, "userId", eventEntity.Actor?.UserId);
        AddIfMissing(enriched, "organizationId", eventEntity.Actor?.OrganizationId);
        AddIfMissing(enriched, "groupId", eventEntity.Actor?.GroupId);

        return enriched;
    }

    private async Task<IDictionary<string, object>?> EnrichStorageObjectResourceAttributesAsync(
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (_storageObjectRepository is null ||
            IsStorageObjectCollectionScope(resourceAttributes) ||
            !Guid.TryParse(resourceId, out var storageObjectId))
        {
            return resourceAttributes;
        }

        var storageObject = await _storageObjectRepository.GetById(storageObjectId);
        if (storageObject is null)
            return resourceAttributes;

        var enriched = resourceAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(resourceAttributes);

        enriched["storageObjectId"] = storageObject.Id.ToString("D");
        enriched["tenantId"] = storageObject.TenantId.ToString("D");
        AddIfMissing(enriched, "visibility", storageObject.Visibility);
        AddIfMissing(enriched, "lifecycleState", storageObject.LifecycleState);
        AddIfMissing(enriched, "createdBy", storageObject.CreatedBy);
        AddIfMissing(enriched, "owningResourceKind", storageObject.OwningResourceKind);
        AddIfMissing(enriched, "owningResourceId", storageObject.OwningResourceId);

        return enriched;
    }

    private async Task<IDictionary<string, object>?> EnrichOrganizationMemberResourceAttributesAsync(
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (_organizationMemberRepository is null ||
            !Guid.TryParse(resourceId, out var memberId))
        {
            return resourceAttributes;
        }

        if (HasOrganizationMemberAuthorizationContext(resourceAttributes))
            return resourceAttributes;

        var member = await _organizationMemberRepository.GetOrganizationMemberWithDetails(memberId);
        if (member is null)
            return resourceAttributes;

        var enriched = resourceAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(resourceAttributes);

        AddIfMissing(enriched, "memberId", member.Id.ToString());
        AddIfMissing(enriched, "tenantId", member.TenantId.ToString());
        AddIfMissing(enriched, "organizationId", member.OrganizationId.ToString());
        AddIfMissing(enriched, "userId", member.UserId.ToString());

        return enriched;
    }

    private static bool HasEventAuthorizationContext(IDictionary<string, object>? attributes) =>
        attributes?.ContainsKey("eventId") == true &&
        attributes.ContainsKey("tenantId") &&
        (attributes.ContainsKey("organizationId") || attributes.ContainsKey("userId") || attributes.ContainsKey("groupId"));

    private static bool HasOrganizationMemberAuthorizationContext(IDictionary<string, object>? attributes) =>
        attributes?.ContainsKey("tenantId") == true &&
        attributes.ContainsKey("organizationId") &&
        attributes.ContainsKey("userId");

    private static bool IsStorageObjectCollectionScope(IDictionary<string, object>? attributes) =>
        attributes?.TryGetValue("authorizationScope", out var scope) == true &&
        string.Equals(scope?.ToString(), "collection", StringComparison.Ordinal);

    private static bool TryGetGuidAttribute(
        IDictionary<string, object> attributes,
        string attributeName,
        out Guid value)
    {
        value = Guid.Empty;

        if (!attributes.TryGetValue(attributeName, out var attributeValue))
            return false;

        if (attributeValue is Guid guidValue)
        {
            value = guidValue;
            return true;
        }

        return attributeValue is string stringValue && Guid.TryParse(stringValue, out value);
    }

    private static void AddIfMissing(IDictionary<string, object> attributes, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !attributes.ContainsKey(key))
            attributes[key] = value;
    }

    private static void AddIfMissing(IDictionary<string, object> attributes, string key, Guid? value)
    {
        if (value.HasValue && value.Value != Guid.Empty && !attributes.ContainsKey(key))
            attributes[key] = value.Value.ToString();
    }

    private async Task EnforceAuthorizationAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        string requestType,
        CancellationToken cancellationToken)
    {
        using var activity = AuthorizationActivitySource.StartActivity("authorization.evaluate");
        activity?.SetTag("resource.kind", resourceKind);
        activity?.SetTag("resource.action", action);
        activity?.SetTag("request.type", requestType);

        var correlationId = Activity.Current?.Id ?? string.Empty;

        var isAllowed = await _authorizationProvider.IsAllowedAsync(
            resourceKind,
            resourceId,
            action,
            resourceAttributes,
            cancellationToken);

        if (!isAllowed)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Authorization denied");

            _logger.LogWarning(
                "Authorization decision: {Decision} request={RequestType} resource={Resource}/{ResourceId} action={Action} correlationId={CorrelationId}",
                "deny", requestType, resourceKind, resourceId, action, correlationId);

            throw new AuthorizationException(resourceKind, action);
        }

        _logger.LogDebug(
            "Authorization decision: {Decision} request={RequestType} resource={Resource}/{ResourceId} action={Action} correlationId={CorrelationId}",
            "allow", requestType, resourceKind, resourceId, action, correlationId);
    }
}
