// ABOUTME: Static catalog of concrete resource descriptors for all DTO types participating in authorization.
// ABOUTME: Each descriptor extracts authorization metadata (ID, attributes, scope) from its DTO instance.

using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.DTOs.Category;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.DTOs.EventDay;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.DTOs.EventSessionTemplate;
using Explore.Application.DTOs.EventTemplate;
using Explore.Application.DTOs.Group;
using Explore.Application.DTOs.GroupMember;
using Explore.Application.DTOs.IndexedDid;
using Explore.Application.DTOs.Location;
using Explore.Application.DTOs.LocationRoom;
using Explore.Application.DTOs.Organization;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.DTOs.OrganizationReview;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.Tag;
using Explore.Application.DTOs.Tenant;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.DTOs.TenantSettingsDocuments;
using Explore.Application.DTOs.User;

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
    #region Core resources with unique Cerbos resource kinds

    public static readonly ResourceDescriptor<EventDto> Event = new(
        ResourceKinds.Event,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["eventId"] = dto.Id.ToString(),
            ["tenantId"] = dto.TenantId.ToString(),
            ["actorId"] = dto.ActorId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    /// <summary>List DTO variant for collection item-level permission checks.</summary>
    public static readonly ResourceDescriptor<EventListDto> EventList = new(
        ResourceKinds.Event,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["eventId"] = dto.Id.ToString(),
            ["tenantId"] = dto.TenantId.ToString(),
            ["actorId"] = dto.ActorId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

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

    public static readonly ResourceDescriptor<TenantMemberDto> TenantMember = new(
        ResourceKinds.TenantMember,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString(),
            ["userId"] = dto.UserId.ToString()
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
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

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

    public static readonly ResourceDescriptor<EventRegistrationDto> EventRegistration = new(
        ResourceKinds.EventRegistration,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId.ToString(),
            ["eventSessionId"] = dto.EventSessionId.ToString(),
            ["userId"] = dto.UserId.ToString(),
            ["tenantId"] = dto.TenantId.ToString()
        },
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
            ["tenantId"] = dto.TenantId.ToString()
        },
        dto => new AuthorizationScope(TenantId: dto.TenantId.ToString()));

    public static readonly ResourceDescriptor<OrganizationMemberDto> OrganizationMember = new(
        ResourceKinds.OrganizationMember,
        dto => dto.Id.ToString(),
        dto => new Dictionary<string, object>
        {
            ["organizationId"] = dto.OrganizationId.ToString(),
            ["userId"] = dto.UserId.ToString()
        },
        dto => new AuthorizationScope(OrganizationId: dto.OrganizationId.ToString()));

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

    public static readonly ResourceDescriptor<IndexedDidDto> IndexedDid = new(
        ResourceKinds.IndexedDid,
        dto => dto.Did);

    public static readonly ResourceDescriptor<AtprotoRecordDto> AtprotoRecord = new(
        ResourceKinds.AtprotoRecord,
        dto => dto.Id.ToString());

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
}
