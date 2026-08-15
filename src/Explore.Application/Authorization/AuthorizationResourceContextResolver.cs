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
            return new AuthorizationContext(resourceId, resourceAttributes, WebhookOwnershipAuthorizationFacts.From(ownership));
        }
        else if (resourceKind == ResourceKinds.Webhook && request is IWebhookOwnerScopedRequest ownerScopedRequest)
        {
            var ownership = await ResolveWebhookOwnershipAsync(ownerScopedRequest, cancellationToken);
            resourceId = ownership.OwnerId.ToString("D");
            resourceAttributes = CreateWebhookOwnerAttributes(ownership);
            return new AuthorizationContext(resourceId, resourceAttributes, WebhookOwnershipAuthorizationFacts.From(ownership));
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

        var facts = CreateFacts(resourceKind, resourceId, resourceAttributes);
        BindPersistedUserOwner(request, resourceAttributes);
        return new AuthorizationContext(resourceId, resourceAttributes, facts);
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

        var eventEntity = await eventRepository.GetEventWithDetails(eventId);
        if (eventEntity is null || tenantContext is not null && eventEntity.TenantId != tenantContext.TenantId)
            return null;

        var enriched = RemoveEventAuthorityAttributes(resourceAttributes);

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

        var session = await eventSessionRepository.GetSessionWithDetails(eventSessionId);
        if (session is null || tenantContext is not null && session.TenantId != tenantContext.TenantId)
            return null;

        var enriched = RemoveEventAuthorityAttributes(resourceAttributes);

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
        if (storageObject is null || tenantContext is not null && storageObject.TenantId != tenantContext.TenantId)
            return null;

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

        var member = await organizationMemberRepository.GetOrganizationMemberWithDetails(memberId);
        if (member is null || tenantContext is not null && member.TenantId != tenantContext.TenantId)
            return null;

        var enriched = resourceAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(resourceAttributes);

        AddIfMissing(enriched, "memberId", member.Id.ToString());
        AddIfMissing(enriched, "tenantId", member.TenantId.ToString());
        AddIfMissing(enriched, "organizationId", member.OrganizationTenant.OrganizationId.ToString());
        AddIfMissing(enriched, "userId", member.UserId.ToString());
        return enriched;
    }

    private static bool IsStorageObjectCollectionScope(IDictionary<string, object>? attributes) =>
        attributes?.TryGetValue("authorizationScope", out var scope) == true &&
        string.Equals(scope?.ToString(), "collection", StringComparison.Ordinal);

    private static IAuthorizationFacts? CreateFacts(
        string resourceKind,
        string resourceId,
        IDictionary<string, object>? attributes)
    {
        if (attributes is null)
            return null;

        return resourceKind switch
        {
            ResourceKinds.Event => CreateEventFacts(attributes),
            ResourceKinds.EventSession => CreateEventSessionFacts(resourceId, attributes),
            ResourceKinds.EventOrganizerClaim => CreateEventOrganizerClaimFacts(resourceId, attributes),
            ResourceKinds.OrganizationMember => CreateOrganizationMemberFacts(resourceId, attributes),
            ResourceKinds.EventContactShareConsent => CreateContactShareFacts(resourceId, attributes),
            ResourceKinds.SupportAccessSession => CreateSupportAccessSessionFacts(resourceId, attributes),
            ResourceKinds.StorageObject when !IsStorageObjectCollectionScope(attributes) => CreateStorageObjectFacts(resourceId, attributes),
            _ => null
        };
    }

    private static EventAuthorizationFacts? CreateEventFacts(IDictionary<string, object> attributes)
    {
        return TryGetGuidAttribute(attributes, "tenantId", out var tenantId) &&
               TryGetGuidAttribute(attributes, "eventId", out var eventId) &&
               TryGetGuidAttribute(attributes, "actorId", out var actorId)
            ? new EventAuthorizationFacts(
                tenantId,
                eventId,
                actorId,
                GetGuid(attributes, "userId"),
                GetGuid(attributes, "organizationId"),
                GetGuid(attributes, "groupId"),
                GetGuid(attributes, "organizerActorId"),
                GetGuid(attributes, "organizerUserId"),
                GetGuid(attributes, "organizerOrganizationId"),
                GetGuid(attributes, "organizerGroupId"),
                GetString(attributes, "provenanceType"),
                GetGuid(attributes, "submittedByUserId"))
            : null;
    }

    private static EventSessionAuthorizationFacts? CreateEventSessionFacts(string resourceId, IDictionary<string, object> attributes)
    {
        var phase = GetString(attributes, "authorizationPhase");
        var sessionId = string.Equals(phase, AuthorizationPhases.PreCreate, StringComparison.Ordinal)
            ? (Guid?)null
            : TryGetGuidAttribute(attributes, "eventSessionId", out var persistedSessionId)
                ? persistedSessionId
                : Guid.TryParse(resourceId, out var parsedSessionId)
                    ? parsedSessionId
                    : null;

        return TryGetGuidAttribute(attributes, "tenantId", out var tenantId) &&
               TryGetGuidAttribute(attributes, "eventId", out var eventId)
            ? new EventSessionAuthorizationFacts(tenantId, eventId, sessionId, phase)
            : null;
    }

    private static EventOrganizerClaimAuthorizationFacts? CreateEventOrganizerClaimFacts(string resourceId, IDictionary<string, object> attributes)
    {
        return TryGetGuidAttribute(attributes, "tenantId", out var tenantId) &&
               TryGetGuidAttribute(attributes, "eventId", out var eventId) &&
               (TryGetGuidAttribute(attributes, "claimId", out var claimId) || Guid.TryParse(resourceId, out claimId)) &&
               TryGetGuidAttribute(attributes, "claimantActorId", out var claimantActorId) &&
               GetString(attributes, "status") is { } status
            ? new EventOrganizerClaimAuthorizationFacts(
                tenantId,
                eventId,
                claimId,
                claimantActorId,
                GetGuid(attributes, "claimantUserId"),
                GetGuid(attributes, "claimantOrganizationId"),
                GetGuid(attributes, "claimantGroupId"),
                status)
            : null;
    }

    private static OrganizationMemberAuthorizationFacts? CreateOrganizationMemberFacts(string resourceId, IDictionary<string, object> attributes)
    {
        return TryGetGuidAttribute(attributes, "tenantId", out var tenantId) &&
               (TryGetGuidAttribute(attributes, "organizationId", out var organizationId) || Guid.TryParse(resourceId, out organizationId))
            ? new OrganizationMemberAuthorizationFacts(
                tenantId,
                organizationId,
                GetGuid(attributes, "memberId"),
                GetGuid(attributes, "userId"))
            : null;
    }

    private static ContactShareAuthorizationFacts? CreateContactShareFacts(string resourceId, IDictionary<string, object> attributes)
    {
        return TryGetGuidAttribute(attributes, "tenantId", out var tenantId) &&
               (TryGetGuidAttribute(attributes, "organizationId", out var organizationId) || Guid.TryParse(resourceId, out organizationId))
            ? new ContactShareAuthorizationFacts(tenantId, organizationId)
            : null;
    }

    private static SupportAccessSessionAuthorizationFacts? CreateSupportAccessSessionFacts(string resourceId, IDictionary<string, object> attributes)
    {
        return TryGetGuidAttribute(attributes, "tenantId", out var tenantId)
            ? new SupportAccessSessionAuthorizationFacts(
                tenantId,
                GetGuid(attributes, "sessionId") ?? (Guid.TryParse(resourceId, out var id) ? id : null),
                GetGuid(attributes, "actorUserId"),
                GetString(attributes, "mode"),
                GetString(attributes, "status"))
            : null;
    }

    private static PersistedStorageObjectAuthorizationFacts? CreateStorageObjectFacts(string resourceId, IDictionary<string, object> attributes)
    {
        return TryGetGuidAttribute(attributes, "tenantId", out var tenantId) &&
               (TryGetGuidAttribute(attributes, "storageObjectId", out var storageObjectId) || Guid.TryParse(resourceId, out storageObjectId)) &&
               GetString(attributes, "visibility") is { } visibility &&
               GetString(attributes, "lifecycleState") is { } lifecycleState
            ? new PersistedStorageObjectAuthorizationFacts(
                tenantId,
                storageObjectId,
                visibility,
                lifecycleState,
                GetString(attributes, "createdBy"),
                GetString(attributes, "owningResourceKind"),
                GetGuid(attributes, "owningResourceId"))
            : null;
    }

    private static Dictionary<string, object> RemoveEventAuthorityAttributes(IDictionary<string, object>? attributes)
    {
        var filtered = attributes is null ? [] : new Dictionary<string, object>(attributes);
        foreach (var key in new[]
                 {
                     "tenantId", "eventId", "eventSessionId", "actorId", "userId", "organizationId", "groupId",
                     "organizerActorId", "organizerUserId", "organizerOrganizationId", "organizerGroupId",
                     "provenanceType", "submittedByUserId"
                 })
        {
            filtered.Remove(key);
        }

        return filtered;
    }

    private static Guid? GetGuid(IDictionary<string, object> attributes, string name) =>
        TryGetGuidAttribute(attributes, name, out var value) ? value : null;

    private static string? GetString(IDictionary<string, object> attributes, string name) =>
        attributes.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value?.ToString())
            ? value.ToString()
            : null;

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
