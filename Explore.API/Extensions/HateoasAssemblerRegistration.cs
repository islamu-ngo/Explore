namespace Explore.API.Extensions;

using Explore.API.Hateoas;
using Explore.API.Hateoas.Assemblers;
using Explore.API.Hateoas.Policies;
using Explore.Application.Contracts.Hateoas;  // For ILinkPolicy, ICollectionLinkPolicy
using Explore.Application.DTOs.Actor;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.DTOs.Category;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.DTOs.Group;
using Explore.Application.DTOs.IndexedDid;
using Explore.Application.DTOs.Location;
using Explore.Application.DTOs.Organization;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.DTOs.OrganizationReview;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.Tag;
using Explore.Application.DTOs.Tenant;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.DTOs.TenantUser;
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

        // Group
        services.AddScoped<ILinkPolicy<GroupDto>, GroupDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<GroupListDto>, GroupCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<GroupDto, GroupListDto>, GroupResourceAssembler>();

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

        // TenantUser (relationship with payload)
        services.AddScoped<ILinkPolicy<TenantUserDto>, TenantUserDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<TenantUserListDto>, TenantUserCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<TenantUserDto, TenantUserListDto>, TenantUserResourceAssembler>();

        // TenantSettings
        services.AddScoped<ILinkPolicy<TenantSettingsDto>, TenantSettingsDetailLinkPolicy>();
        services.AddScoped<ICollectionLinkPolicy<TenantSettingsListDto>, TenantSettingsCollectionLinkPolicy>();
        services.AddScoped<IResourceAssembler<TenantSettingsDto, TenantSettingsListDto>, TenantSettingsResourceAssembler>();

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

        return services;
    }
}
