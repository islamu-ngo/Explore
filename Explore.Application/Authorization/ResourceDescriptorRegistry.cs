// ABOUTME: Static registry mapping DTO types to resource kind strings for authorization checks.
// ABOUTME: Used by both HATEOAS RequirePermission extensions (API layer) and AuthorizationBehavior (Application layer).

namespace Explore.Application.Authorization;

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
/// Maps DTO types to their corresponding resource kind identifiers for authorization.
/// Centralizes the convention: each DTO family maps to a single resource kind string.
/// Lives in the Application layer so both API (HATEOAS) and Application (pipeline behaviors) can reference it.
/// </summary>
public static class ResourceDescriptorRegistry
{
    private static readonly IReadOnlyDictionary<Type, string> ResourceKinds = new Dictionary<Type, string>
    {
        // Organization
        [typeof(OrganizationDto)] = "organization",
        [typeof(OrganizationListDto)] = "organization",

        // Group
        [typeof(GroupDto)] = "group",
        [typeof(GroupListDto)] = "group",

        // Tenant Settings
        [typeof(TenantSettingsDto)] = "tenant_setting",
        [typeof(TenantSettingsListDto)] = "tenant_setting",

        // Event
        [typeof(EventDto)] = "event",
        [typeof(EventListDto)] = "event",

        // Tenant
        [typeof(TenantDto)] = "tenant",
        [typeof(TenantListDto)] = "tenant",

        // User
        [typeof(UserDto)] = "user",

        // Tenant User
        [typeof(TenantUserDto)] = "tenant_user",
        [typeof(TenantUserListDto)] = "tenant_user",

        // Tag
        [typeof(TagDto)] = "tag",
        [typeof(TagListDto)] = "tag",

        // Storage Object
        [typeof(StorageObjectDto)] = "storage_object",
        [typeof(StorageObjectListDto)] = "storage_object",

        // Organization Review
        [typeof(OrganizationReviewDto)] = "organization_review",

        // Organization Member
        [typeof(OrganizationMemberDto)] = "organization_member",

        // Location
        [typeof(LocationDto)] = "location",
        [typeof(LocationListDto)] = "location",

        // Indexed DID
        [typeof(IndexedDidDto)] = "indexed_did",
        [typeof(IndexedDidListDto)] = "indexed_did",

        // Event Session
        [typeof(EventSessionDto)] = "event_session",
        [typeof(EventSessionListDto)] = "event_session",

        // Event Session Agenda Item
        [typeof(EventSessionAgendaItemDto)] = "event_session_agenda_item",
        [typeof(EventSessionAgendaItemListDto)] = "event_session_agenda_item",

        // Event Registration
        [typeof(EventRegistrationDto)] = "event_registration",
        [typeof(EventRegistrationListDto)] = "event_registration",

        // Category
        [typeof(CategoryDto)] = "category",
        [typeof(CategoryListDto)] = "category",

        // ATProto Record
        [typeof(AtprotoRecordDto)] = "atproto_record",
        [typeof(AtprotoRecordListDto)] = "atproto_record"
    };

    /// <summary>
    /// Resolves the resource kind for a given DTO type.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no mapping exists for the given type.</exception>
    public static string ResolveResourceKind(Type resourceType)
    {
        if (ResourceKinds.TryGetValue(resourceType, out var resourceKind))
            return resourceKind;

        throw new InvalidOperationException($"No resource kind mapping configured for type '{resourceType.Name}'.");
    }

    /// <summary>
    /// Converts a <see cref="PermissionAction"/> enum value to its string representation.
    /// </summary>
    public static string ToActionString(PermissionAction action)
    {
        return action switch
        {
            PermissionAction.Read => "read",
            PermissionAction.Create => "create",
            PermissionAction.Update => "update",
            PermissionAction.Delete => "delete",
            PermissionAction.ManageMembers => "manage_members",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}
