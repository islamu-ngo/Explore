// ABOUTME: Static registry mapping DTO types to resource kind strings for authorization checks.
// ABOUTME: Used by both HATEOAS RequirePermission extensions (API layer) and AuthorizationBehavior (Application layer).

namespace Explore.Application.Authorization;

using Explore.Application.DTOs.Actor;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.DTOs.Category;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.DTOs.Group;
using Explore.Application.DTOs.GroupMember;
using Explore.Application.DTOs.IndexedDid;
using Explore.Application.DTOs.Location;
using Explore.Application.DTOs.Notification;
using Explore.Application.DTOs.Organization;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.DTOs.OrganizationReview;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.DTOs.Tag;
using Explore.Application.DTOs.Tenant;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.DTOs.TenantSettings;
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

        // Group Member
        [typeof(GroupMemberDto)] = "group_member",
        [typeof(GroupMemberListDto)] = "group_member",

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

        // Tenant Member
        [typeof(TenantMemberDto)] = "tenant_member",
        [typeof(TenantMemberListDto)] = "tenant_member",

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

        // Event Session Group
        [typeof(EventSessionGroupDto)] = "event_session_group",
        [typeof(EventSessionGroupListDto)] = "event_session_group",

        // Event Session Agenda Item
        [typeof(EventSessionAgendaItemDto)] = "event_session_agenda_item",
        [typeof(EventSessionAgendaItemListDto)] = "event_session_agenda_item",

        // Event Registration
        [typeof(EventRegistrationDto)] = "event_registration",
        [typeof(EventRegistrationListDto)] = "event_registration",

        // Category
        [typeof(CategoryDto)] = "category",
        [typeof(CategoryListDto)] = "category",

        // Custom Property Definition
        [typeof(CustomPropertyDefinitionDto)] = "custom_property_definition",
        [typeof(CustomPropertyDefinitionListDto)] = "custom_property_definition",

        // Notification
        [typeof(NotificationDto)] = "notification",
        [typeof(NotificationListDto)] = "notification",

        // Actor
        [typeof(ActorDto)] = "actor",
        [typeof(ActorListDto)] = "actor",

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
    /// Converts a <see cref="PermissionAction"/> enum value to its Cerbos-compatible string representation.
    /// Prefer using <see cref="AuthorizationActions"/> constants directly instead of this method.
    /// </summary>
#pragma warning disable CS0618 // Intentional: bridge method for legacy PermissionAction callers
    public static string ToActionString(PermissionAction action)
    {
        return action switch
        {
            PermissionAction.Read => AuthorizationActions.View,
            PermissionAction.Create => AuthorizationActions.Create,
            PermissionAction.Update => AuthorizationActions.Update,
            PermissionAction.Delete => AuthorizationActions.Delete,
            PermissionAction.ManageMembers => AuthorizationActions.ManageMembers,
            PermissionAction.ViewSharedContacts => AuthorizationActions.ViewSharedContacts,
            PermissionAction.ExportSharedContacts => AuthorizationActions.ExportSharedContacts,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
#pragma warning restore CS0618
}
