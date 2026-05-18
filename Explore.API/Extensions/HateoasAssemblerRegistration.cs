namespace Explore.API.Extensions;

using Explore.API.Hateoas;
using Explore.API.Hateoas.Assemblers;
using Explore.API.Hateoas.Policies;
using Explore.Application.Contracts.Hateoas;  // For ILinkPolicy, ICollectionLinkPolicy
using Explore.API.Hateoas.Resources;
using Explore.Application.DTOs.Actor;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.DTOs.Category;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.DTOs.CustomPropertyGovernance;
using Explore.Application.DTOs.CustomPropertyProjection;
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
using Explore.Application.DTOs.Notification;
using Explore.Application.DTOs.Organization;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.DTOs.OrganizationReview;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.Tag;
using Explore.Application.DTOs.Tenant;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.DTOs.TenantSettingsDocuments;
using Explore.Application.DTOs.User;

/// <summary>
/// Registers all HATEOAS resource assemblers and their link policies.
/// </summary>
public static class HateoasAssemblerRegistration
{
    /// <summary>
    /// Registers all resource assemblers for the application.
    /// Call this after AddHateoas() in Program.cs.
    /// </summary>
    public static IServiceCollection AddHateoasAssemblers(this IServiceCollection services)
    {
        // Organization
        services.AddScoped<ILinkPolicy<OrganizationDto>, OrganizationDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<OrganizationListDto>, OrganizationCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<OrganizationDto, OrganizationListDto>, OrganizationResourceAssembler>();

        // Event
        services.AddScoped<ILinkPolicy<EventDto>, EventDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<EventListDto>, EventCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<EventDto, EventListDto>, EventResourceAssembler>();

        // EventSession
        services.AddScoped<ILinkPolicy<EventSessionDto>, EventSessionDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<EventSessionListDto>, EventSessionCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<EventSessionDto, EventSessionListDto>, EventSessionResourceAssembler>();

        // EventSessionGroup (program sections/tracks/devrooms)
        services.AddScoped<ILinkPolicy<EventSessionGroupDto>, EventSessionGroupDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<EventSessionGroupListDto>, EventSessionGroupCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<EventSessionGroupDto, EventSessionGroupListDto>, EventSessionGroupResourceAssembler>();

        // Template Sync helper resources
        services.AddScoped<ILinkPolicy<EventTemplateSyncResource>, EventTemplateSyncLinkPolicy>();
        services.AddScoped<ILinkPolicy<EventSessionTemplateSyncResource>, EventSessionTemplateSyncLinkPolicy>();

        // Actor
        services.AddScoped<ILinkPolicy<ActorDto>, ActorDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<ActorListDto>, ActorCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<ActorDto, ActorListDto>, ActorResourceAssembler>();

        // Location
        services.AddScoped<ILinkPolicy<LocationDto>, LocationDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<LocationListDto>, LocationCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<LocationDto, LocationListDto>, LocationResourceAssembler>();

        // Category
        services.AddScoped<ILinkPolicy<CategoryDto>, CategoryDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<CategoryListDto>, CategoryCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<CategoryDto, CategoryListDto>, CategoryResourceAssembler>();

        // CustomPropertyDefinition
        services.AddScoped<ILinkPolicy<CustomPropertyDefinitionDto>, CustomPropertyDefinitionDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<CustomPropertyDefinitionListDto>, CustomPropertyDefinitionCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<CustomPropertyDefinitionDto, CustomPropertyDefinitionListDto>, CustomPropertyDefinitionResourceAssembler>();

        // EventTemplate
        services.AddScoped<ILinkPolicy<EventTemplateDto>, EventTemplateDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<EventTemplateListDto>, EventTemplateCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<EventTemplateDto, EventTemplateListDto>, EventTemplateResourceAssembler>();

        // EventCustomProperty
        services.AddScoped<ILinkPolicy<EventCustomPropertyDefinitionDto>, EventCustomPropertyDefinitionDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<EventCustomPropertyDefinitionListDto>, EventCustomPropertyDefinitionCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<EventCustomPropertyDefinitionDto, EventCustomPropertyDefinitionListDto>, EventCustomPropertyResourceAssembler>();

        // EventSessionTemplate
        services.AddScoped<ILinkPolicy<EventSessionTemplateDto>, EventSessionTemplateDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<EventSessionTemplateListDto>, EventSessionTemplateCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<EventSessionTemplateDto, EventSessionTemplateListDto>, EventSessionTemplateResourceAssembler>();

        // EventSessionCustomProperty
        services.AddScoped<ILinkPolicy<EventSessionCustomPropertyDefinitionDto>, EventSessionCustomPropertyDefinitionDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<EventSessionCustomPropertyDefinitionListDto>, EventSessionCustomPropertyDefinitionCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<EventSessionCustomPropertyDefinitionDto, EventSessionCustomPropertyDefinitionListDto>, EventSessionCustomPropertyResourceAssembler>();

        // Group
        services.AddScoped<ILinkPolicy<GroupDto>, GroupDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<GroupListDto>, GroupCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<GroupDto, GroupListDto>, GroupResourceAssembler>();

        // GroupMember (relationship with payload, same DTO for detail and list)
        services.AddScoped<ILinkPolicy<GroupMemberDto>, GroupMemberDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<GroupMemberDto>, GroupMemberCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<GroupMemberDto, GroupMemberDto>, GroupMemberResourceAssembler>();

        // Tag
        services.AddScoped<ILinkPolicy<TagDto>, TagDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<TagListDto>, TagCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<TagDto, TagListDto>, TagResourceAssembler>();

        // User (same DTO for detail and list)
        services.AddScoped<ILinkPolicy<UserDto>, UserDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<UserDto>, UserCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<UserDto, UserDto>, UserResourceAssembler>();

        // Tenant
        services.AddScoped<ILinkPolicy<TenantDto>, TenantDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<TenantListDto>, TenantCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<TenantDto, TenantListDto>, TenantResourceAssembler>();

        // TenantMember (relationship with payload)
        services.AddScoped<ILinkPolicy<TenantMemberDto>, TenantMemberDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<TenantMemberListDto>, TenantMemberCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<TenantMemberDto, TenantMemberListDto>, TenantMemberResourceAssembler>();

        // Tenant typed settings documents
        services.AddScoped<ILinkPolicy<TenantBrandingSettingsDocumentDto>, TenantBrandingSettingsDocumentLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<TenantBrandingSettingsDocumentDto>, TenantBrandingSettingsDocumentCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<TenantBrandingSettingsDocumentDto, TenantBrandingSettingsDocumentDto>, TenantBrandingSettingsDocumentResourceAssembler>();

        // OrganizationMember (relationship with payload, same DTO for detail and list)
        services.AddScoped<ILinkPolicy<OrganizationMemberDto>, OrganizationMemberDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<OrganizationMemberDto>, OrganizationMemberCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<OrganizationMemberDto, OrganizationMemberDto>, OrganizationMemberResourceAssembler>();

        // EventRegistration (relationship with payload)
        services.AddScoped<ILinkPolicy<EventRegistrationDto>, EventRegistrationDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<EventRegistrationListDto>, EventRegistrationCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<EventRegistrationDto, EventRegistrationListDto>, EventRegistrationResourceAssembler>();

        // EventSessionAgendaItem
        services.AddScoped<ILinkPolicy<EventSessionAgendaItemDto>, EventSessionAgendaItemDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<EventSessionAgendaItemListDto>, EventSessionAgendaItemCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<EventSessionAgendaItemDto, EventSessionAgendaItemListDto>, EventSessionAgendaItemResourceAssembler>();

        // StorageObject
        services.AddScoped<ILinkPolicy<StorageObjectDto>, StorageObjectDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<StorageObjectListDto>, StorageObjectCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<StorageObjectDto, StorageObjectListDto>, StorageObjectResourceAssembler>();

        // OrganizationReview (same DTO for detail and list)
        services.AddScoped<ILinkPolicy<OrganizationReviewDto>, OrganizationReviewDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<OrganizationReviewDto>, OrganizationReviewCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<OrganizationReviewDto, OrganizationReviewDto>, OrganizationReviewResourceAssembler>();

        // AtprotoRecord (ATProto federation)
        services.AddScoped<ILinkPolicy<AtprotoRecordDto>, AtprotoRecordDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<AtprotoRecordListDto>, AtprotoRecordCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<AtprotoRecordDto, AtprotoRecordListDto>, AtprotoRecordResourceAssembler>();

        // IndexedDid (ATProto federation identity)
        services.AddScoped<ILinkPolicy<IndexedDidDto>, IndexedDidDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<IndexedDidListDto>, IndexedDidCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<IndexedDidDto, IndexedDidListDto>, IndexedDidResourceAssembler>();

        // Notification (personal user notifications)
        services.AddScoped<ILinkPolicy<NotificationDto>, NotificationDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<NotificationListDto>, NotificationCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<NotificationDto, NotificationListDto>, NotificationResourceAssembler>();

        // Custom Property Projection Admin (D2 Operability)
        services.AddScoped<ILinkPolicy<ProjectionStatusDto>, ProjectionStatusDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<ProjectionStatusDto>, ProjectionStatusCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<ProjectionStatusDto, ProjectionStatusDto>, ProjectionStatusResourceAssembler>();
        services.AddScoped<ILinkPolicy<ProjectionDirtyScopeDto>, ProjectionDirtyScopeDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<ProjectionDirtyScopeDto>, ProjectionDirtyScopeCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<ProjectionDirtyScopeDto, ProjectionDirtyScopeDto>, ProjectionDirtyScopeResourceAssembler>();
        services.AddScoped<ILinkPolicy<RebuildProjectionResponseDto>, RebuildProjectionResponseLinkPolicy>();
        services.AddScoped<ILinkPolicy<DrainDirtyScopesResponseDto>, DrainDirtyScopesResponseLinkPolicy>();

        // Custom Property Governance (D2 Operability)
        services.AddScoped<ICollectionLinkPolicy<CustomPropertyGovernanceRowDto>, CustomPropertyGovernanceCollectionLinkPolicy>();

        // EventDay (event scheduling)
        services.AddScoped<ILinkPolicy<EventDayDto>, EventDayDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<EventDayListDto>, EventDayCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<EventDayDto, EventDayListDto>, EventDayResourceAssembler>();

        // EventAgendaItem (event scheduling)
        services.AddScoped<ILinkPolicy<EventAgendaItemDto>, EventAgendaItemDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<EventAgendaItemListDto>, EventAgendaItemCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<EventAgendaItemDto, EventAgendaItemListDto>, EventAgendaItemResourceAssembler>();

        // LocationRoom (event scheduling)
        services.AddScoped<ILinkPolicy<LocationRoomDto>, LocationRoomDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<LocationRoomListDto>, LocationRoomCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<LocationRoomDto, LocationRoomListDto>, LocationRoomResourceAssembler>();

        return services;
    }
}
