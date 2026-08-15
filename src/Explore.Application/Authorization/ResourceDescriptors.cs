// ABOUTME: Static catalog of concrete resource descriptors for DTOs and bounded authorization targets.
// ABOUTME: Each descriptor extracts authorization metadata (ID, attributes, scope) from its source instance.

using Explore.Application.DTOs.ActorSubscription;
using Explore.Application.DTOs.Ai;
using Explore.Application.DTOs.Category;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.DTOs.EmailDispatch;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.DTOs.EventDay;
using Explore.Application.DTOs.EventOrganizerClaim;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.DTOs.EventSessionTemplate;
using Explore.Application.DTOs.EventTemplate;
using Explore.Application.DTOs.Group;
using Explore.Application.DTOs.GroupMember;
using Explore.Application.DTOs.Location;
using Explore.Application.DTOs.LocationRoom;
using Explore.Application.DTOs.Organization;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.DTOs.OrganizationReview;
using Explore.Application.DTOs.OrganizationTenantEvidence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.SupportAccess;
using Explore.Application.DTOs.Tag;
using Explore.Application.DTOs.Tenant;
using Explore.Application.DTOs.TenantSettingsDocuments;
using Explore.Application.DTOs.TenantUserRoleGrant;
using Explore.Application.DTOs.User;
using Explore.Application.DTOs.Webhooks;
using Explore.Domain;

namespace Explore.Application.Authorization;

/// <summary>
/// Central catalog of resource descriptors for all DTO types participating in HATEOAS authorization.
/// <para>
/// Usage in link policies:
/// <code>
/// .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.Event, dto)
/// </code>
/// replaces manual dictionary construction with type-safe, centralized metadata extraction.
/// </para>
/// <para>
/// Descriptors marked "piggybacks on tenant" use <see cref="ResourceKinds.Tenant"/> because
/// their commands authorize as the parent tenant resource, not as their own resource kind.
/// </para>
/// </summary>
public static class ResourceDescriptors
{
    public static IReadOnlyDictionary<string, object> GetWebhookOwnerAttributes(WebhookOwnershipScope ownership)
    {
        ArgumentNullException.ThrowIfNull(ownership);

        var attributes = WebhookOwnerAttributes(
            (int)ownership.Kind,
            ownership.OwnerId,
            ownership.TenantId,
            ownership.InstanceId);
        AddIfPresent(attributes, "organizationId", ownership.OrganizationId);
        AddIfPresent(attributes, "groupId", ownership.GroupId);
        AddIfPresent(attributes, "userId", ownership.UserId);
        return attributes;
    }

    #region Core resources with unique Cerbos resource kinds

    public static readonly ResourceDescriptor<EventDto> Event = new(
        ResourceKinds.Event,
        dto => dto.Id.ToString(),
        EventAttributes,
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()),
        EventFacts);

    public static readonly ResourceDescriptor<EventDto> EventOrganizerClaimForEvent = new(
        ResourceKinds.EventOrganizerClaim,
        dto => dto.Id.ToString(),
        EventAttributes,
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()),
        EventFacts);

    /// <summary>List DTO variant for collection item-level permission checks.</summary>
    public static readonly ResourceDescriptor<EventListDto> EventList = new(
        ResourceKinds.Event,
        dto => dto.Id.ToString(),
        EventListAttributes,
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()),
        EventListFacts);

    public static readonly ResourceDescriptor<Explore.Domain.Event> EventAuthorizationTarget = new(
        ResourceKinds.Event,
        eventEntity => eventEntity.Id.ToString(),
        EventAuthorizationTargetAttributes,
        eventEntity => new AuthorizationScope(TenantId: eventEntity.TenantId.ToString()),
        EventAuthorizationTargetFacts);

    public static readonly ResourceDescriptor<OrganizationDto> Organization = new(
        ResourceKinds.Organization,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["organizationId"] = dto.Id.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    /// <summary>List DTO variant for collection item-level permission checks.</summary>
    public static readonly ResourceDescriptor<OrganizationListDto> OrganizationList = new(
        ResourceKinds.Organization,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["organizationId"] = dto.Id.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<OrganizationTenantEvidenceDto> OrganizationTenantEvidence = new(
        ResourceKinds.Organization,
        dto => dto.OrganizationId.ToString(),
        dto => new Dictionary<string, object>
        {
            ["organizationId"] = dto.OrganizationId.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<OrganizationTenantEvidenceDto> OrganizationTenantEvidenceDocument = new(
        ResourceKinds.StorageObject,
        dto => dto.DocumentStorageObjectId.ToString(),
        dto =>
        {
            var attributes = new Dictionary<string, object>
            {
                ["tenantId"] = dto.TenantId.ToString(),
                ["lifecycleState"] = StorageObjectLifecycleStates.Active,
                ["visibility"] = StorageObjectVisibilities.PrivateOwner
            };
            AddIfPresent(attributes, "createdBy", dto.DocumentCreatedBy);
            return attributes;
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<TenantDto> Tenant = new(
        ResourceKinds.Tenant,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.Id.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.Id.ToString()));

    public static readonly ResourceDescriptor<TenantBrandingSettingsDocumentDto> TenantBrandingSettingsDocument = new(
        ResourceKinds.TenantSetting,
        dto => $"{dto.SourceScopeId}:{dto.DocumentKey}",
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.SourceScopeId.ToString(),
            ["documentKey"] = dto.DocumentKey,
            ["isLockedByInstance"] = dto.IsLockedByInstance
        },
        dto => new AuthorizationScope(TenantId: dto.SourceScopeId.ToString()));

    public static readonly ResourceDescriptor<TenantUserRoleGrantDto> TenantUserRoleGrant = new(
        ResourceKinds.TenantUserRoleGrant,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString(),
            ["tenantUserId"] = dto.TenantUserId.ToString(),
            ["userId"] = dto.UserId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<SupportAccessSessionDto> SupportAccessSession = new(
        ResourceKinds.SupportAccessSession,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["sessionId"] = dto.Id.ToString(),
            ["tenantId"] = dto.TargetTenantId.ToString(),
            ["actorUserId"] = dto.ActorUserId.ToString(),
            ["mode"] = dto.ModeName,
            ["status"] = dto.StatusName
        },
        dto => new AuthorizationScope(TenantId: dto.TargetTenantId.ToString()),
        dto => new SupportAccessSessionAuthorizationFacts(dto.TargetTenantId, dto.Id, dto.ActorUserId, dto.ModeName, dto.StatusName));

    public static readonly ResourceDescriptor<SupportAccessAuditEventDto> SupportAccessAuditEvent = new(
        ResourceKinds.SupportAccessSession,
        dto => dto.SupportAccessSessionId.ToString(),
        dto => new Dictionary<string, object>
        {
            ["sessionId"] = dto.SupportAccessSessionId.ToString(),
            ["tenantId"] = dto.TargetTenantId.ToString(),
            ["auditEventId"] = dto.Id.ToString(),
            ["eventType"] = dto.EventTypeName
        },
        dto => new AuthorizationScope(TenantId: dto.TargetTenantId.ToString()),
        dto => new SupportAccessSessionAuthorizationFacts(dto.TargetTenantId, dto.SupportAccessSessionId, null, null, null));

    public static readonly ResourceDescriptor<WebhookConsumerDto> WebhookConsumer = new(
        ResourceKinds.Webhook,
        dto => dto.Id.ToString(),
        WebhookConsumerAttributes,
        dto => new AuthorizationScope(
            TenantId: dto.TenantId?.ToString(),
            OrganizationId: dto.OrganizationId?.ToString()),
        WebhookConsumerFacts);

    public static readonly ResourceDescriptor<WebhookEndpointDto> WebhookEndpoint = new(
        ResourceKinds.Webhook,
        dto => dto.Id.ToString(),
        WebhookEndpointAttributes,
        dto => new AuthorizationScope(
            TenantId: dto.TenantId?.ToString(),
            OrganizationId: dto.OwnerKindId == (int)WebhookConsumerKind.Organization
                ? dto.OwnerId.ToString()
                : null),
        WebhookEndpointFacts);

    public static readonly ResourceDescriptor<WebhookMessageDto> WebhookMessage = new(
        ResourceKinds.Webhook,
        dto => dto.Id.ToString(),
        WebhookMessageAttributes,
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<WebhookDeliveryAttemptDto> WebhookDeliveryAttempt = new(
        ResourceKinds.Webhook,
        dto => dto.Id.ToString(),
        WebhookDeliveryAttemptAttributes,
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<WebhookProviderPublicationDto> WebhookProviderPublication = new(
        ResourceKinds.Webhook,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString(),
            ["consumerId"] = dto.WebhookConsumerId.ToString(),
            ["messageId"] = dto.WebhookMessageId.ToString(),
            ["publicationId"] = dto.Id.ToString(),
            ["providerKind"] = dto.ProviderKindCode,
            ["status"] = dto.StatusCode
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<WebhookBulkReplayOperationDto> WebhookBulkReplayOperation = new(
        ResourceKinds.Webhook,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString(),
            ["bulkReplayOperationId"] = dto.Id.ToString(),
            ["status"] = dto.StatusCode
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<ActorSubscriptionDto> ActorSubscription = new(
        ResourceKinds.ActorSubscription,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString(),
            ["subscriberTenantUserId"] = dto.SubscriberTenantUserId.ToString(),
            ["subscriberUserId"] = dto.SubscriberUserId.ToString(),
            ["targetActorId"] = dto.TargetActorId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<ActorSubscriptionListDto> ActorSubscriptionList = new(
        ResourceKinds.ActorSubscription,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString(),
            ["targetActorId"] = dto.TargetActorId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<AiConversationDto> AiConversation = new(
        ResourceKinds.AiConversation,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString(),
            ["userId"] = dto.UserId.ToString(),
            ["status"] = dto.Status
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<AiConversationSummaryDto> AiConversationSummary = new(
        ResourceKinds.AiConversation,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString(),
            ["userId"] = dto.UserId.ToString(),
            ["status"] = dto.Status
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<EventSessionDto> EventSession = new(
        ResourceKinds.EventSession,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()),
        dto => new EventSessionAuthorizationFacts(dto.TenantId, dto.EventId, dto.Id, null));

    public static readonly ResourceDescriptor<EventSessionListDto> EventSessionList = new(
        ResourceKinds.EventSession,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()),
        dto => new EventSessionAuthorizationFacts(dto.TenantId, dto.EventId, dto.Id, null));

    public static readonly ResourceDescriptor<EventSessionGroupDto> EventSessionGroup = new(
        ResourceKinds.EventSessionGroup,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<EventSessionGroupListDto> EventSessionGroupList = new(
        ResourceKinds.EventSessionGroup,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<EventDayDto> EventDay = new(
        ResourceKinds.EventDay,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<EventAgendaItemDto> EventAgendaItem = new(
        ResourceKinds.EventAgendaItem,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<LocationRoomDto> LocationRoom = new(
        ResourceKinds.LocationRoom,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["locationId"] = dto.LocationId.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<EventPublicActionDto> EventPublicAction = new(
        ResourceKinds.Event,
        dto => dto.EventId.ToString(),
        EventPublicActionAttributes,
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<EventOrganizerClaimDto> EventOrganizerClaim = new(
        ResourceKinds.EventOrganizerClaim,
        dto => dto.EventId.ToString(),
        EventOrganizerClaimAttributes,
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<EventSessionAgendaItemDto> EventSessionAgendaItem = new(
        ResourceKinds.EventSessionAgendaItem,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId.ToString(),
            ["eventSessionId"] = dto.EventSessionId.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<EventSessionSpeakerDto> EventSessionSpeaker = new(
        ResourceKinds.EventSession,
        dto => dto.EventSessionId.ToString(),
        dto => new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId.ToString(),
            ["eventSessionId"] = dto.EventSessionId.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<EventSessionSpeakerListDto> EventSessionSpeakerList = new(
        ResourceKinds.EventSession,
        dto => dto.EventSessionId.ToString(),
        dto => new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId.ToString(),
            ["eventSessionId"] = dto.EventSessionId.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<CategoryDto> Category = new(
        ResourceKinds.Category,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<TagDto> Tag = new(
        ResourceKinds.Tag,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<LocationDto> Location = new(
        ResourceKinds.Location,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<StorageObjectDto> StorageObject = new(
        ResourceKinds.StorageObject,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString(),
            ["visibility"] = dto.Visibility,
            ["lifecycleState"] = dto.LifecycleState
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()),
        dto => new PersistedStorageObjectAuthorizationFacts(dto.TenantId, dto.Id, dto.Visibility, dto.LifecycleState, null, null, null));

    public static readonly ResourceDescriptor<OrganizationMemberDto> OrganizationMember = new(
        ResourceKinds.OrganizationMember,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString(),
            ["organizationId"] = dto.OrganizationId.ToString(),
            ["userId"] = dto.UserId.ToString()
        },
        dto => new AuthorizationScope(
            TenantId: dto.TenantId.ToString(),
            OrganizationId: dto.OrganizationId.ToString()),
        dto => new OrganizationMemberAuthorizationFacts(dto.TenantId, dto.OrganizationId, dto.Id, dto.UserId));

    public static readonly ResourceDescriptor<OrganizationReviewDto> OrganizationReview = new(
        ResourceKinds.OrganizationReview,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["organizationId"] = dto.OrganizationId.ToString(),
            ["userId"] = dto.UserId.ToString()
        },
        dto => new AuthorizationScope(OrganizationId: dto.OrganizationId.ToString()));

    public static readonly ResourceDescriptor<GroupDto> Group = new(
        ResourceKinds.Group,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    /// <summary>List DTO variant for collection item-level permission checks.</summary>
    public static readonly ResourceDescriptor<GroupListDto> GroupList = new(
        ResourceKinds.Group,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["groupId"] = dto.Id.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<GroupMemberDto> GroupMember = new(
        ResourceKinds.GroupMember,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["groupId"] = dto.GroupId.ToString(),
            ["userId"] = dto.UserId.ToString()
        });

    public static readonly ResourceDescriptor<UserDto> User = new(
        ResourceKinds.User,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["actorId"] = dto.ActorId.ToString()
        });

    public static readonly ResourceDescriptor<CustomPropertyDefinitionDto> CustomPropertyDefinition = new(
        ResourceKinds.CustomPropertyDefinition,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<EmailDispatchStatusDto> EmailDispatchStatus = new(
        ResourceKinds.EmailDispatch,
        dto => dto.OutboxId.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString(),
            ["outboxId"] = dto.OutboxId.ToString(),
            ["sourceType"] = dto.SourceType,
            ["sourceId"] = dto.SourceId.ToString(),
            ["deliveryStatus"] = dto.DeliveryStatus
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<IncomingWebhookEffectStatusDto> IncomingWebhookEffectStatus = new(
        ResourceKinds.Webhook,
        dto => dto.EffectOutboxId.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString(),
            ["effectOutboxId"] = dto.EffectOutboxId.ToString(),
            ["incomingWebhookMessageId"] = dto.IncomingWebhookMessageId.ToString(),
            ["effectKind"] = dto.EffectKind,
            ["status"] = dto.Status
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    #endregion

    #region Sub-resources piggybacking on parent tenant authorization

    // These use ResourceKinds.Tenant because their commands authorize via
    // [AuthorizeResource(ResourceKinds.Tenant, ...)], not their own resource kind.
    // This aligns HATEOAS link authorization with command-level authorization.
    // Note: Also fixes a latent bug where these DTO types were not registered
    // in ResourceDescriptorRegistry, causing link generation to throw.

    /// <summary>Piggybacks on tenant authorization. Commands use [AuthorizeResource(ResourceKinds.Tenant, ...)].</summary>
    public static readonly ResourceDescriptor<EventTemplateDto> EventTemplate = new(
        ResourceKinds.Tenant,
        dto => dto.TenantId.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<EventTemplateListDto> EventTemplateList = new(
        ResourceKinds.Tenant,
        dto => dto.TenantId.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    /// <summary>Piggybacks on tenant authorization. Commands use [AuthorizeResource(ResourceKinds.Tenant, ...)].</summary>
    public static readonly ResourceDescriptor<EventSessionTemplateDto> EventSessionTemplate = new(
        ResourceKinds.Tenant,
        dto => dto.TenantId.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    /// <summary>Piggybacks on tenant authorization. Commands use [AuthorizeResource(ResourceKinds.Tenant, ...)].</summary>
    public static readonly ResourceDescriptor<EventCustomPropertyDefinitionDto> EventCustomPropertyDefinition = new(
        ResourceKinds.Tenant,
        dto => dto.TenantId.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    /// <summary>Piggybacks on tenant authorization. Commands use [AuthorizeResource(ResourceKinds.Tenant, ...)].</summary>
    public static readonly ResourceDescriptor<EventSessionCustomPropertyDefinitionDto> EventSessionCustomPropertyDefinition = new(
        ResourceKinds.Tenant,
        dto => dto.TenantId.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    #endregion

    private static Dictionary<string, object> EventAttributes(EventDto dto)
    {
        var attributes = BaseEventAttributes(dto.Id, dto.TenantId, dto.ActorId);
        AddIfPresent(attributes, "userId", dto.ActorUserId);
        AddIfPresent(attributes, "organizationId", dto.ActorOrganizationId);
        AddIfPresent(attributes, "groupId", dto.ActorGroupId);
        attributes["provenanceType"] = dto.ProvenanceTypeCode ?? dto.ProvenanceTypeId.ToString();
        AddIfPresent(attributes, "organizerActorId", dto.OrganizerActorId);
        AddIfPresent(attributes, "organizerUserId", dto.OrganizerActorUserId);
        AddIfPresent(attributes, "organizerOrganizationId", dto.OrganizerActorOrganizationId);
        AddIfPresent(attributes, "organizerGroupId", dto.OrganizerActorGroupId);
        AddIfPresent(attributes, "submittedByUserId", dto.SubmittedByUserId);
        return attributes;
    }

    private static Dictionary<string, object> WebhookConsumerAttributes(WebhookConsumerDto dto)
    {
        var attributes = WebhookOwnerAttributes(
            dto.ConsumerKindId,
            dto.OwnerId,
            dto.TenantId,
            dto.InstanceId);
        attributes["consumerId"] = dto.Id.ToString();
        attributes["consumerKind"] = dto.ConsumerKindName;
        attributes["providerMode"] = dto.ProviderModeName;
        attributes["status"] = dto.StatusName;
        AddIfPresent(attributes, "organizationId", dto.OrganizationId);
        AddIfPresent(attributes, "groupId", dto.GroupId);
        AddIfPresent(attributes, "userId", dto.OwnerUserId);
        return attributes;
    }

    private static Dictionary<string, object> WebhookEndpointAttributes(WebhookEndpointDto dto)
    {
        var attributes = WebhookOwnerAttributes(
            dto.OwnerKindId,
            dto.OwnerId,
            dto.TenantId,
            dto.InstanceId);
        attributes["consumerId"] = dto.ConsumerId.ToString();
        attributes["endpointId"] = dto.Id.ToString();
        attributes["status"] = dto.StatusName;

        var ownerKind = (WebhookConsumerKind)dto.OwnerKindId;
        var ownerAttribute = ownerKind switch
        {
            WebhookConsumerKind.Organization => "organizationId",
            WebhookConsumerKind.Group => "groupId",
            WebhookConsumerKind.User => "userId",
            _ => null
        };
        if (ownerAttribute is not null)
        {
            attributes[ownerAttribute] = dto.OwnerId.ToString();
        }

        return attributes;
    }

    private static Dictionary<string, object> WebhookMessageAttributes(WebhookMessageDto dto)
    {
        var attributes = WebhookDeliveryOwnerAttributes(dto.OwnerKindId, dto.OwnerId, dto.TenantId);
        attributes["messageId"] = dto.Id.ToString();
        attributes["eventType"] = dto.EventType;
        attributes["aggregateKind"] = dto.AggregateKind;
        return attributes;
    }

    private static Dictionary<string, object> WebhookDeliveryAttemptAttributes(WebhookDeliveryAttemptDto dto)
    {
        var attributes = WebhookDeliveryOwnerAttributes(dto.OwnerKindId, dto.OwnerId, dto.TenantId);
        attributes["attemptId"] = dto.Id.ToString();
        attributes["messageId"] = dto.MessageId.ToString();
        attributes["endpointId"] = dto.EndpointId.ToString();
        attributes["outcome"] = dto.OutcomeCode;
        return attributes;
    }

    private static Dictionary<string, object> WebhookDeliveryOwnerAttributes(
        int ownerKindId,
        Guid ownerId,
        Guid sourceTenantId)
    {
        var instanceId = ownerKindId == (int)WebhookConsumerKind.Instance ? ownerId : (Guid?)null;
        Guid? ownerTenantId = ownerKindId == (int)WebhookConsumerKind.Instance ? null : sourceTenantId;
        var attributes = WebhookOwnerAttributes(ownerKindId, ownerId, ownerTenantId, instanceId);
        var ownerAttribute = (WebhookConsumerKind)ownerKindId switch
        {
            WebhookConsumerKind.Organization => "organizationId",
            WebhookConsumerKind.Group => "groupId",
            WebhookConsumerKind.User => "userId",
            _ => null
        };
        if (ownerAttribute is not null)
        {
            attributes[ownerAttribute] = ownerId.ToString();
        }

        return attributes;
    }

    private static Dictionary<string, object> WebhookOwnerAttributes(
        int ownerKindId,
        Guid ownerId,
        Guid? tenantId,
        Guid? instanceId)
    {
        var attributes = new Dictionary<string, object>
        {
            ["ownerKindId"] = ownerKindId,
            ["ownerKind"] = ((WebhookConsumerKind)ownerKindId).ToString().ToUpperInvariant(),
            ["ownerId"] = ownerId.ToString()
        };
        AddIfPresent(attributes, "tenantId", tenantId);
        AddIfPresent(attributes, "instanceId", instanceId);
        return attributes;
    }

    private static Dictionary<string, object> EventListAttributes(EventListDto dto)
    {
        var attributes = BaseEventAttributes(dto.Id, dto.TenantId, dto.ActorId);
        AddIfPresent(attributes, "userId", dto.ActorUserId);
        AddIfPresent(attributes, "organizationId", dto.ActorOrganizationId);
        AddIfPresent(attributes, "groupId", dto.ActorGroupId);
        return attributes;
    }

    private static EventAuthorizationFacts EventFacts(EventDto dto) => new(
        dto.TenantId,
        dto.Id,
        dto.ActorId,
        dto.ActorUserId,
        dto.ActorOrganizationId,
        dto.ActorGroupId,
        dto.OrganizerActorId,
        dto.OrganizerActorUserId,
        dto.OrganizerActorOrganizationId,
        dto.OrganizerActorGroupId,
        dto.ProvenanceTypeCode ?? dto.ProvenanceTypeId.ToString(),
        dto.SubmittedByUserId);

    private static EventAuthorizationFacts EventListFacts(EventListDto dto) => new(
        dto.TenantId,
        dto.Id,
        dto.ActorId,
        dto.ActorUserId,
        dto.ActorOrganizationId,
        dto.ActorGroupId,
        null,
        null,
        null,
        null,
        null,
        null);

    private static Dictionary<string, object> EventPublicActionAttributes(EventPublicActionDto dto)
    {
        var attributes = EventChildAttributes(
            dto.EventId,
            dto.TenantId,
            dto.EventActorId,
            dto.EventActorUserId,
            dto.EventActorOrganizationId,
            dto.EventActorGroupId,
            dto.EventProvenanceTypeId,
            dto.EventProvenanceTypeCode,
            dto.EventOrganizerActorId,
            dto.EventSubmittedByUserId);
        attributes["publicActionId"] = dto.Id.ToString();
        return attributes;
    }

    private static Dictionary<string, object> EventOrganizerClaimAttributes(EventOrganizerClaimDto dto)
    {
        var attributes = EventChildAttributes(
            dto.EventId,
            dto.TenantId,
            dto.EventActorId,
            dto.EventActorUserId,
            dto.EventActorOrganizationId,
            dto.EventActorGroupId,
            dto.EventProvenanceTypeId,
            dto.EventProvenanceTypeCode,
            dto.EventOrganizerActorId,
            dto.EventSubmittedByUserId);
        attributes["claimId"] = dto.Id.ToString();
        attributes["claimantActorId"] = dto.ClaimantActorId.ToString();
        AddIfPresent(attributes, "claimantUserId", dto.ClaimantActorUserId);
        AddIfPresent(attributes, "claimantOrganizationId", dto.ClaimantActorOrganizationId);
        AddIfPresent(attributes, "claimantGroupId", dto.ClaimantActorGroupId);
        attributes["status"] = dto.StatusCode ?? dto.StatusId.ToString();
        return attributes;
    }

    private static EventOrganizerClaimAuthorizationFacts EventOrganizerClaimFacts(EventOrganizerClaimDto dto) => new(
        dto.TenantId,
        dto.EventId,
        dto.Id,
        dto.ClaimantActorId,
        dto.ClaimantActorUserId,
        dto.ClaimantActorOrganizationId,
        dto.ClaimantActorGroupId,
        dto.StatusCode ?? dto.StatusId.ToString());

    private static Dictionary<string, object> EventChildAttributes(
        Guid eventId,
        Guid tenantId,
        Guid actorId,
        Guid? userId,
        Guid? organizationId,
        Guid? groupId,
        int provenanceTypeId,
        string? provenanceTypeCode,
        Guid? organizerActorId,
        Guid? submittedByUserId)
    {
        var attributes = BaseEventAttributes(eventId, tenantId, actorId);
        AddIfPresent(attributes, "userId", userId);
        AddIfPresent(attributes, "organizationId", organizationId);
        AddIfPresent(attributes, "groupId", groupId);
        attributes["provenanceType"] = provenanceTypeCode ?? provenanceTypeId.ToString();
        AddIfPresent(attributes, "organizerActorId", organizerActorId);
        AddIfPresent(attributes, "submittedByUserId", submittedByUserId);
        return attributes;
    }

    private static Dictionary<string, object> EventAuthorizationTargetAttributes(Explore.Domain.Event eventEntity)
    {
        var attributes = BaseEventAttributes(eventEntity.Id, eventEntity.TenantId, eventEntity.ActorId);
        AddIfPresent(attributes, "userId", eventEntity.Actor?.UserId);
        AddIfPresent(attributes, "organizationId", eventEntity.Actor?.OrganizationId);
        AddIfPresent(attributes, "groupId", eventEntity.Actor?.GroupId);
        attributes["provenanceType"] = eventEntity.EventProvenanceType?.MasterCode
            ?? eventEntity.EventProvenanceTypeId.ToString();
        AddIfPresent(attributes, "organizerActorId", eventEntity.OrganizerActorId);
        AddIfPresent(attributes, "organizerUserId", eventEntity.OrganizerActor?.UserId);
        AddIfPresent(attributes, "organizerOrganizationId", eventEntity.OrganizerActor?.OrganizationId);
        AddIfPresent(attributes, "organizerGroupId", eventEntity.OrganizerActor?.GroupId);
        AddIfPresent(attributes, "submittedByUserId", eventEntity.SubmittedByUserId);
        return attributes;
    }

    private static EventAuthorizationFacts EventAuthorizationTargetFacts(Explore.Domain.Event eventEntity) => new(
        eventEntity.TenantId,
        eventEntity.Id,
        eventEntity.ActorId,
        eventEntity.Actor?.UserId,
        eventEntity.Actor?.OrganizationId,
        eventEntity.Actor?.GroupId,
        eventEntity.OrganizerActorId,
        eventEntity.OrganizerActor?.UserId,
        eventEntity.OrganizerActor?.OrganizationId,
        eventEntity.OrganizerActor?.GroupId,
        eventEntity.EventProvenanceType?.MasterCode ?? eventEntity.EventProvenanceTypeId.ToString(),
        eventEntity.SubmittedByUserId);

    private static WebhookOwnershipAuthorizationFacts WebhookConsumerFacts(WebhookConsumerDto dto) => new(
        (WebhookConsumerKind)dto.ConsumerKindId,
        dto.OwnerId,
        dto.TenantId,
        dto.InstanceId,
        dto.OrganizationId,
        dto.GroupId,
        dto.OwnerUserId);

    private static WebhookOwnershipAuthorizationFacts WebhookEndpointFacts(WebhookEndpointDto dto)
    {
        var kind = (WebhookConsumerKind)dto.OwnerKindId;
        return new WebhookOwnershipAuthorizationFacts(
            kind,
            dto.OwnerId,
            dto.TenantId,
            dto.InstanceId,
            kind == WebhookConsumerKind.Organization ? dto.OwnerId : null,
            kind == WebhookConsumerKind.Group ? dto.OwnerId : null,
            kind == WebhookConsumerKind.User ? dto.OwnerId : null);
    }

    private static Dictionary<string, object> BaseEventAttributes(Guid eventId, Guid tenantId, Guid actorId) => new()
    {
        ["eventId"] = eventId.ToString(),
        ["tenantId"] = tenantId.ToString(),
        ["actorId"] = actorId.ToString()
    };

    private static void AddIfPresent(Dictionary<string, object> attributes, string key, Guid? value)
    {
        if (value.HasValue && value.Value != Guid.Empty)
            attributes[key] = value.Value.ToString();
    }
}
