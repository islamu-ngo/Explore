// ABOUTME: Resolves generic authorization resource context after request-specific enrichers run.
// ABOUTME: Preserves webhook ownership, persisted owner binding, and common resource enrichment.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Exceptions;
using Explore.Domain;

namespace Explore.Application.Authorization;

public sealed class AuthorizationResourceContextResolver(
    IEventRepository? eventRepository = null,
    IOrganizationMemberRepository? organizationMemberRepository = null,
    IStorageObjectRepository? storageObjectRepository = null,
    IEventSessionRepository? eventSessionRepository = null,
    IWebhookOwnershipScopeResolver? webhookOwnershipScopeResolver = null,
    ITenantContext? tenantContext = null)
{
    public async Task<AuthorizationContext> ResolveAsync<TRequest>(
        TRequest request,
        string resourceKind,
        string action,
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        if (resourceKind == ResourceKinds.Webhook && request is IWebhookPersistedOwnerRequest persistedOwnerRequest)
        {
            var ownership = await ResolvePersistedWebhookOwnershipAsync(persistedOwnerRequest, cancellationToken);
            resourceId = persistedOwnerRequest.OwnedResourceId.ToString("D");
            resourceAttributes = CreateWebhookOwnerAttributes(ownership);
        }
        else if (resourceKind == ResourceKinds.Webhook && request is IWebhookOwnerScopedRequest ownerScopedRequest)
        {
            var ownership = await ResolveWebhookOwnershipAsync(ownerScopedRequest, cancellationToken);
            resourceId = ownership.OwnerId.ToString("D");
            resourceAttributes = CreateWebhookOwnerAttributes(ownership);
        }

        resourceAttributes = await EnrichResourceAttributesAsync(
            resourceKind,
            resourceId,
            resourceAttributes,
            cancellationToken);

        if (resourceKind == ResourceKinds.RegistrationForm && resourceAttributes is null)
        {
            throw new AuthorizationException(resourceKind, action);
        }

        BindPersistedUserOwner(request, resourceAttributes);
        return new AuthorizationContext(resourceId, resourceAttributes);
    }

    private async Task<WebhookOwnershipScope> ResolveWebhookOwnershipAsync(
        IWebhookOwnerScopedRequest request,
        CancellationToken cancellationToken)
    {
        if (webhookOwnershipScopeResolver is null)
        {
            throw new AuthorizationException(ResourceKinds.Webhook, "resolve-owner");
        }

        var resolution = await webhookOwnershipScopeResolver.ResolveAsync(
            request.OwnerKindId,
            request.OwnerId,
            cancellationToken);
        return resolution.Scope ?? throw new AuthorizationException(ResourceKinds.Webhook, "resolve-owner");
    }

    private async Task<WebhookOwnershipScope> ResolvePersistedWebhookOwnershipAsync(
        IWebhookPersistedOwnerRequest request,
        CancellationToken cancellationToken)
    {
        if (webhookOwnershipScopeResolver is null)
        {
            throw new AuthorizationException(ResourceKinds.Webhook, "resolve-persisted-owner");
        }

        var resolution = await webhookOwnershipScopeResolver.ResolvePersistedAsync(
            request.OwnedResourceKind,
            request.OwnedResourceId,
            cancellationToken);
        return resolution.Scope ??
            throw new AuthorizationException(ResourceKinds.Webhook, "resolve-persisted-owner");
    }

    private static Dictionary<string, object> CreateWebhookOwnerAttributes(WebhookOwnershipScope ownership)
    {
        var attributes = new Dictionary<string, object>
        {
            ["ownerKindId"] = (int)ownership.Kind,
            ["ownerKind"] = ownership.Kind.ToString().ToUpperInvariant(),
            ["ownerId"] = ownership.OwnerId.ToString("D")
        };

        AddIfMissing(attributes, "tenantId", ownership.TenantId);
        AddIfMissing(attributes, "instanceId", ownership.InstanceId);
        AddIfMissing(attributes, "organizationId", ownership.OrganizationId);
        AddIfMissing(attributes, "groupId", ownership.GroupId);
        AddIfMissing(attributes, "userId", ownership.UserId);
        return attributes;
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
            ResourceKinds.EventOrganizerClaim => resourceAttributes is not null &&
                                                TryGetGuidAttribute(resourceAttributes, "eventId", out var eventId)
                ? await EnrichEventResourceAttributesAsync(eventId.ToString("D"), resourceAttributes, cancellationToken)
                : resourceAttributes,
            ResourceKinds.RegistrationForm => resourceAttributes is not null &&
                                                 TryGetGuidAttribute(resourceAttributes, "eventId", out var registrationEventId)
                ? await EnrichRegistrationFormResourceAttributesAsync(
                    registrationEventId,
                    resourceAttributes,
                    cancellationToken)
                : null,
            ResourceKinds.EventSession => await EnrichEventSessionResourceAttributesAsync(resourceId, resourceAttributes, cancellationToken),
            ResourceKinds.OrganizationMember => await EnrichOrganizationMemberResourceAttributesAsync(resourceId, resourceAttributes, cancellationToken),
            ResourceKinds.StorageObject => await EnrichStorageObjectResourceAttributesAsync(resourceId, resourceAttributes, cancellationToken),
            ResourceKinds.CustomPropertyProjection => await EnrichCustomPropertyProjectionResourceAttributesAsync(resourceAttributes, cancellationToken),
            _ => resourceAttributes
        };
    }

    private static void BindPersistedUserOwner<TRequest>(
        TRequest request,
        IDictionary<string, object>? resourceAttributes)
        where TRequest : notnull
    {
        if (request is IPersistedUserOwnerBoundRequest ownerBoundRequest &&
            resourceAttributes is not null &&
            TryGetGuidAttribute(resourceAttributes, "userId", out var ownerUserId))
        {
            ownerBoundRequest.ExpectedOwnerUserId = ownerUserId;
        }
    }

    private async Task<IDictionary<string, object>?> EnrichCustomPropertyProjectionResourceAttributesAsync(
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (resourceAttributes is null || resourceAttributes.ContainsKey("tenantId"))
            return resourceAttributes;

        if (TryGetGuidAttribute(resourceAttributes, "eventId", out var eventId))
        {
            if (eventRepository is null)
                return resourceAttributes;

            var eventEntity = await eventRepository.GetEventWithDetails(eventId);
            if (eventEntity is null)
                return resourceAttributes;

            var enriched = new Dictionary<string, object>(resourceAttributes);
            AddIfMissing(enriched, "eventId", eventEntity.Id.ToString("D"));
            AddIfMissing(enriched, "tenantId", eventEntity.TenantId.ToString("D"));
            return enriched;
        }

        if (TryGetGuidAttribute(resourceAttributes, "eventSessionId", out var eventSessionId))
        {
            if (eventSessionRepository is null)
                return resourceAttributes;

            var session = await eventSessionRepository.GetSessionWithDetails(eventSessionId);
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
        if (eventRepository is null || !Guid.TryParse(resourceId, out var eventId))
            return resourceAttributes;

        if (HasEventAuthorizationContext(resourceAttributes))
            return resourceAttributes;

        var eventEntity = await eventRepository.GetEventWithDetails(eventId);
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
        AddIfMissing(enriched, "organizerActorId", eventEntity.OrganizerActorId);
        AddIfMissing(enriched, "organizerUserId", eventEntity.OrganizerActor?.UserId);
        AddIfMissing(enriched, "organizerOrganizationId", eventEntity.OrganizerActor?.OrganizationId);
        AddIfMissing(enriched, "organizerGroupId", eventEntity.OrganizerActor?.GroupId);

        return enriched;
    }

    private async Task<IDictionary<string, object>?> EnrichRegistrationFormResourceAttributesAsync(
        Guid eventId,
        IDictionary<string, object> resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (eventRepository is null)
            return null;

        var eventEntity = await eventRepository.GetEventWithDetails(eventId);
        if (eventEntity is null || tenantContext is not null && eventEntity.TenantId != tenantContext.TenantId)
            return null;

        var enriched = new Dictionary<string, object>(resourceAttributes);
        foreach (string key in new[]
                 {
                     "tenantId", "eventId", "actorId", "userId", "organizationId", "groupId",
                     "organizerActorId", "organizerUserId", "organizerOrganizationId", "organizerGroupId"
                 })
        {
            enriched.Remove(key);
        }

        enriched["eventId"] = eventEntity.Id.ToString("D");
        enriched["tenantId"] = eventEntity.TenantId.ToString("D");
        AddIfMissing(enriched, "actorId", eventEntity.ActorId);
        AddIfMissing(enriched, "userId", eventEntity.Actor?.UserId);
        AddIfMissing(enriched, "organizationId", eventEntity.Actor?.OrganizationId);
        AddIfMissing(enriched, "groupId", eventEntity.Actor?.GroupId);
        AddIfMissing(enriched, "organizerActorId", eventEntity.OrganizerActorId);
        AddIfMissing(enriched, "organizerUserId", eventEntity.OrganizerActor?.UserId);
        AddIfMissing(enriched, "organizerOrganizationId", eventEntity.OrganizerActor?.OrganizationId);
        AddIfMissing(enriched, "organizerGroupId", eventEntity.OrganizerActor?.GroupId);
        return enriched;
    }

    private async Task<IDictionary<string, object>?> EnrichEventSessionResourceAttributesAsync(
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (eventSessionRepository is null || !Guid.TryParse(resourceId, out var eventSessionId))
            return resourceAttributes;

        if (HasEventAuthorizationContext(resourceAttributes))
            return resourceAttributes;

        var session = await eventSessionRepository.GetSessionWithDetails(eventSessionId);
        if (session is null)
            return resourceAttributes;

        var enriched = resourceAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(resourceAttributes);

        AddIfMissing(enriched, "eventSessionId", session.Id.ToString());
        AddIfMissing(enriched, "eventId", session.EventId.ToString());
        AddIfMissing(enriched, "tenantId", session.TenantId.ToString());
        return enriched;
    }

    private async Task<IDictionary<string, object>?> EnrichStorageObjectResourceAttributesAsync(
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (storageObjectRepository is null ||
            IsStorageObjectCollectionScope(resourceAttributes) ||
            !Guid.TryParse(resourceId, out var storageObjectId))
        {
            return resourceAttributes;
        }

        var storageObject = await storageObjectRepository.GetById(storageObjectId);
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
        if (organizationMemberRepository is null || !Guid.TryParse(resourceId, out var memberId))
            return resourceAttributes;

        if (HasOrganizationMemberAuthorizationContext(resourceAttributes))
            return resourceAttributes;

        var member = await organizationMemberRepository.GetOrganizationMemberWithDetails(memberId);
        if (member is null)
            return resourceAttributes;

        var enriched = resourceAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(resourceAttributes);

        AddIfMissing(enriched, "memberId", member.Id.ToString());
        AddIfMissing(enriched, "tenantId", member.TenantId.ToString());
        AddIfMissing(enriched, "organizationId", member.OrganizationTenant.OrganizationId.ToString());
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
}
