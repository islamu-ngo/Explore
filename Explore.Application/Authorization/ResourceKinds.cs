// ABOUTME: Canonical catalog of resource kind string constants matching Cerbos policy resource names.
// ABOUTME: Replaces magic strings in [AuthorizeResource] attributes and ResourceDescriptorRegistry lookups.

namespace Explore.Application.Authorization;

/// <summary>
/// Canonical catalog of resource kind identifiers used for authorization.
/// All values match Cerbos resource policy names exactly.
/// <para>
/// Use these constants in <see cref="AuthorizeResourceAttribute"/>, link policies,
/// and anywhere a resource kind string is required. Avoid raw string literals.
/// </para>
/// </summary>
public static class ResourceKinds
{
    public const string Event = "event";
    public const string EventSession = "event_session";
    public const string EventSessionAgendaItem = "event_session_agenda_item";
    public const string EventRegistration = "event_registration";
    public const string EventContactShareConsent = "event_contact_share_consent";
    public const string Organization = "organization";
    public const string OrganizationMember = "organization_member";
    public const string OrganizationReview = "organization_review";
    public const string Tenant = "tenant";
    public const string TenantSetting = "tenant_setting";
    public const string TenantMember = "tenant_member";
    public const string Category = "category";
    public const string Tag = "tag";
    public const string Location = "location";
    public const string StorageObject = "storage_object";
    public const string User = "user";
    public const string AtprotoRecord = "atproto_record";
    public const string IndexedDid = "indexed_did";
    public const string InstanceSetting = "instance_setting";
    public const string CustomPropertyDefinition = "custom_property_definition";
    public const string Notification = "notification";
    public const string Actor = "actor";
    public const string Group = "group";
    public const string GroupMember = "group_member";
}
