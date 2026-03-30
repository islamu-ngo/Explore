namespace Explore.API.Hateoas;

/// <summary>
/// Route name constants for HATEOAS link generation.
/// These must match the Name property on route attributes in controllers.
/// </summary>
public static class RouteNames
{
    #region Organization Routes

    public const string GetOrganizations = "GetOrganizations";
    public const string GetOrganizationById = "GetOrganizationById";
    public const string GetMyOrganizations = "GetMyOrganizations";
    public const string CreateOrganization = "CreateOrganization";
    public const string UpdateOrganization = "UpdateOrganization";
    public const string DeleteOrganization = "DeleteOrganization";
    public const string GetOrganizationEvents = "GetOrganizationEvents";
    public const string GetOrganizationMembers = "GetOrganizationMembers";

    #endregion

    #region Event Routes

    public const string GetEvents = "GetEvents";
    public const string GetEventById = "GetEventById";
    public const string GetMyEvents = "GetMyEvents";
    public const string CreateEvent = "CreateEvent";
    public const string UpdateEvent = "UpdateEvent";
    public const string DeleteEvent = "DeleteEvent";
    public const string GetEventSessions = "GetEventSessions";
    public const string GetEventCategories = "GetEventCategories";
    public const string GetEventTags = "GetEventTags";

    // Event Aspect Routes
    public const string GetEventIslamicAspect = "GetEventIslamicAspect";
    public const string UpsertEventIslamicAspect = "UpsertEventIslamicAspect";
    public const string DeleteEventIslamicAspect = "DeleteEventIslamicAspect";
    public const string GetEventTechAspect = "GetEventTechAspect";
    public const string UpsertEventTechAspect = "UpsertEventTechAspect";
    public const string DeleteEventTechAspect = "DeleteEventTechAspect";

    #endregion

    #region Event Session Routes

    public const string GetEventSessions_List = "GetEventSessionsList";
    public const string GetEventSessionById = "GetEventSessionById";
    public const string CreateEventSession = "CreateEventSession";
    public const string UpdateEventSession = "UpdateEventSession";
    public const string DeleteEventSession = "DeleteEventSession";
    public const string GetEventSessionSpeakers = "GetEventSessionSpeakers";
    public const string GetEventSessionAgendaItems = "GetEventSessionAgendaItems";

    #endregion

    #region Actor Routes

    public const string GetActors = "GetActors";
    public const string GetActorById = "GetActorById";
    public const string GetActorByDid = "GetActorByDid";
    public const string GetActorsByTenant = "GetActorsByTenant";
    public const string CreateActor = "CreateActor";
    public const string UpdateActor = "UpdateActor";
    public const string DeleteActor = "DeleteActor";
    public const string GetActorEvents = "GetActorEvents";

    #endregion

    #region Location Routes

    public const string GetLocations = "GetLocations";
    public const string GetLocationById = "GetLocationById";
    public const string GetLocationsByCity = "GetLocationsByCity";
    public const string GetLocationsByCountry = "GetLocationsByCountry";
    public const string CreateLocation = "CreateLocation";
    public const string UpdateLocation = "UpdateLocation";
    public const string DeleteLocation = "DeleteLocation";

    #endregion

    #region Category Routes

    public const string GetCategories = "GetCategories";
    public const string GetCategoryById = "GetCategoryById";
    public const string CreateCategory = "CreateCategory";
    public const string UpdateCategory = "UpdateCategory";
    public const string DeleteCategory = "DeleteCategory";
    public const string GetCategoryChildren = "GetCategoryChildren";
    public const string GetCategoryEvents = "GetCategoryEvents";

    #endregion

    #region Tag Routes

    public const string GetTags = "GetTags";
    public const string GetTagById = "GetTagById";
    public const string CreateTag = "CreateTag";
    public const string UpdateTag = "UpdateTag";
    public const string DeleteTag = "DeleteTag";
    public const string GetTagEvents = "GetTagEvents";
    public const string GetTagTagTypes = "GetTagTagTypes";

    #endregion

    #region Registration Routes

    public const string GetRegistrations = "GetRegistrations";
    public const string GetRegistrationById = "GetRegistrationById";
    public const string CreateRegistration = "CreateRegistration";
    public const string UpdateRegistration = "UpdateRegistration";
    public const string DeleteRegistration = "DeleteRegistration";

    #endregion

    #region Lookup Table Routes

    public const string GetApprovalStatuses = "GetApprovalStatuses";
    public const string GetEventTypes = "GetEventTypes";
    public const string GetEventFormats = "GetEventFormats";
    public const string GetEventStatuses = "GetEventStatuses";
    public const string GetAudienceAges = "GetAudienceAges";
    public const string GetAudienceGenders = "GetAudienceGenders";
    public const string GetMadhabs = "GetMadhabs";
    public const string GetLanguages = "GetLanguages";
    public const string GetTagTypes = "GetTagTypes";

    #endregion

    #region User Routes

    public const string GetUsers = "GetUsers";
    public const string GetUserById = "GetUserById";
    public const string GetCurrentUser = "GetCurrentUser";
    public const string CreateUser = "CreateUser";
    public const string UpdateUser = "UpdateUser";
    public const string DeleteUser = "DeleteUser";
    public const string GetUserOrganizations = "GetUserOrganizations";
    public const string GetUserRegistrations = "GetUserRegistrations";

    #endregion

    #region Tenant Routes

    public const string GetTenants = "GetTenants";
    public const string GetTenantById = "GetTenantById";
    public const string GetTenantBySlug = "GetTenantBySlug";
    public const string CreateTenant = "CreateTenant";
    public const string UpdateTenant = "UpdateTenant";
    public const string DeleteTenant = "DeleteTenant";
    public const string GetTenantSettings = "GetTenantSettings";

    #endregion

    #region Tenant Member Routes

    public const string GetTenantMembers = "GetTenantMembers";
    public const string GetTenantMemberById = "GetTenantMemberById";
    public const string CreateTenantMember = "CreateTenantMember";
    public const string UpdateTenantMember = "UpdateTenantMember";
    public const string DeleteTenantMember = "DeleteTenantMember";

    #endregion

    #region Tenant Settings Routes

    public const string GetTenantSettingsById = "GetTenantSettingsById";
    public const string UpdateTenantSettings = "UpdateTenantSettings";

    #endregion

    #region Role Routes

    public const string GetRoles = "GetRoles";
    public const string GetRoleById = "GetRoleById";

    #endregion

    #region Organization Member Routes

    public const string GetOrganizationMemberById = "GetOrganizationMemberById";
    public const string CreateOrganizationMember = "CreateOrganizationMember";
    public const string UpdateOrganizationMember = "UpdateOrganizationMember";
    public const string DeleteOrganizationMember = "DeleteOrganizationMember";

    #endregion

    #region Group Routes

    public const string GetGroups = "GetGroups";
    public const string GetGroupById = "GetGroupById";
    public const string GetMyGroups = "GetMyGroups";
    public const string CreateGroup = "CreateGroup";
    public const string UpdateGroup = "UpdateGroup";
    public const string DeleteGroup = "DeleteGroup";
    public const string GetGroupMembers = "GetGroupMembers";
    public const string GetGroupMemberById = "GetGroupMemberById";
    public const string CreateGroupMember = "CreateGroupMember";
    public const string UpdateGroupMember = "UpdateGroupMember";
    public const string DeleteGroupMember = "DeleteGroupMember";

    #endregion

    #region Event Session Agenda Item Routes

    public const string GetEventSessionAgendaItemById = "GetEventSessionAgendaItemById";
    public const string CreateEventSessionAgendaItem = "CreateEventSessionAgendaItem";
    public const string UpdateEventSessionAgendaItem = "UpdateEventSessionAgendaItem";
    public const string DeleteEventSessionAgendaItem = "DeleteEventSessionAgendaItem";

    #endregion

    #region Storage Object Routes

    public const string GetStorageObjects = "GetStorageObjects";
    public const string GetStorageObjectById = "GetStorageObjectById";
    public const string CreateStorageObject = "CreateStorageObject";
    public const string DeleteStorageObject = "DeleteStorageObject";

    #endregion

    #region Organization Review Routes

    public const string GetOrganizationReviews = "GetOrganizationReviews";
    public const string GetOrganizationReviewById = "GetOrganizationReviewById";
    public const string CreateOrganizationReview = "CreateOrganizationReview";
    public const string UpdateOrganizationReview = "UpdateOrganizationReview";
    public const string DeleteOrganizationReview = "DeleteOrganizationReview";

    #endregion

    #region Notification Routes

    public const string GetNotifications = "GetNotifications";
    public const string GetNotificationById = "GetNotificationById";
    public const string GetUnreadNotificationCount = "GetUnreadNotificationCount";
    public const string MarkNotificationAsRead = "MarkNotificationAsRead";
    public const string MarkAllNotificationsAsRead = "MarkAllNotificationsAsRead";
    public const string DeleteNotification = "DeleteNotification";

    #endregion

    #region User Appearance Routes

    public const string GetCurrentUserAppearancePreferences = "GetCurrentUserAppearancePreferences";
    public const string UpdateCurrentUserAppearancePreferences = "UpdateCurrentUserAppearancePreferences";

    #endregion

    #region ATProto Routes

    public const string GetAtprotoRecords = "GetAtprotoRecords";
    public const string GetAtprotoRecordById = "GetAtprotoRecordById";
    public const string GetAtprotoRecordByUri = "GetAtprotoRecordByUri";
    public const string CreateAtprotoRecord = "CreateAtprotoRecord";
    public const string DeleteAtprotoRecord = "DeleteAtprotoRecord";

    public const string GetIndexedDids = "GetIndexedDids";
    public const string GetIndexedDidByDid = "GetIndexedDidByDid";
    public const string CreateIndexedDid = "CreateIndexedDid";
    public const string UpdateIndexedDid = "UpdateIndexedDid";

    #endregion

    #region Contact Share Consent Routes

    public const string GetUserContactShareConsents = "GetUserContactShareConsents";
    public const string CheckConsentForOrganizer = "CheckConsentForOrganizer";
    public const string WithdrawContactShareConsent = "WithdrawContactShareConsent";
    public const string GetOrganizationSharedContacts = "GetOrganizationSharedContacts";
    public const string ExportOrganizationSharedContacts = "ExportOrganizationSharedContacts";

    #endregion

    #region Custom Property Definition Routes

    public const string GetCustomPropertyDefinitions = "GetCustomPropertyDefinitions";
    public const string GetCustomPropertyDefinitionById = "GetCustomPropertyDefinitionById";
    public const string CreateCustomPropertyDefinition = "CreateCustomPropertyDefinition";
    public const string UpdateCustomPropertyDefinition = "UpdateCustomPropertyDefinition";
    public const string DeleteCustomPropertyDefinition = "DeleteCustomPropertyDefinition";

    #endregion

    #region Event Template Routes

    public const string GetEventTemplates = "GetEventTemplates";
    public const string GetEventTemplateById = "GetEventTemplateById";
    public const string CreateEventTemplate = "CreateEventTemplate";
    public const string UpdateEventTemplate = "UpdateEventTemplate";
    public const string DeleteEventTemplate = "DeleteEventTemplate";

    #endregion

    #region Event Custom Property Routes

    public const string GetEventCustomPropertyDefinitions = "GetEventCustomPropertyDefinitions";
    public const string GetEventCustomPropertyDefinitionById = "GetEventCustomPropertyDefinitionById";
    public const string CreateEventCustomPropertyDefinition = "CreateEventCustomPropertyDefinition";
    public const string UpdateEventCustomPropertyDefinition = "UpdateEventCustomPropertyDefinition";
    public const string DeleteEventCustomPropertyDefinition = "DeleteEventCustomPropertyDefinition";
    public const string GetEventCustomPropertyValues = "GetEventCustomPropertyValues";
    public const string SetEventCustomPropertyValue = "SetEventCustomPropertyValue";
    public const string SetEventCustomPropertyMultiValues = "SetEventCustomPropertyMultiValues";

    #endregion

    #region Event Session Template Routes

    public const string GetEventSessionTemplates = "GetEventSessionTemplates";
    public const string GetEventSessionTemplateById = "GetEventSessionTemplateById";
    public const string CreateEventSessionTemplate = "CreateEventSessionTemplate";
    public const string UpdateEventSessionTemplate = "UpdateEventSessionTemplate";
    public const string DeleteEventSessionTemplate = "DeleteEventSessionTemplate";

    #endregion

    #region Event Session Custom Property Routes

    public const string GetEventSessionCustomPropertyDefinitions = "GetEventSessionCustomPropertyDefinitions";
    public const string GetEventSessionCustomPropertyDefinitionById = "GetEventSessionCustomPropertyDefinitionById";
    public const string CreateEventSessionCustomPropertyDefinition = "CreateEventSessionCustomPropertyDefinition";
    public const string UpdateEventSessionCustomPropertyDefinition = "UpdateEventSessionCustomPropertyDefinition";
    public const string DeleteEventSessionCustomPropertyDefinition = "DeleteEventSessionCustomPropertyDefinition";
    public const string GetEventSessionCustomPropertyValues = "GetEventSessionCustomPropertyValues";
    public const string SetEventSessionCustomPropertyValue = "SetEventSessionCustomPropertyValue";
    public const string SetEventSessionCustomPropertyMultiValues = "SetEventSessionCustomPropertyMultiValues";

    #endregion

    #region Settings Routes

    public const string GetUserSettings = "GetUserSettings";
    public const string UpdateUserSettingsBatch = "UpdateUserSettingsBatch";
    public const string UpdateUserSetting = "UpdateUserSetting";
    public const string ResetUserSetting = "ResetUserSetting";
    public const string GetTenantScopedSettings = "GetTenantScopedSettings";
    public const string UpdateTenantSettingsBatch = "UpdateTenantSettingsBatch";
    public const string UpdateTenantSetting = "UpdateTenantSetting";
    public const string LockTenantSetting = "LockTenantSetting";
    public const string UnlockTenantSetting = "UnlockTenantSetting";

    #endregion

    #region Footer Routes

    public const string GetFooterConfig = "GetFooterConfig";
    public const string GetFooterLinkGroups = "GetFooterLinkGroups";
    public const string GetFooterLinkGroupById = "GetFooterLinkGroupById";
    public const string CreateFooterLinkGroup = "CreateFooterLinkGroup";
    public const string UpdateFooterLinkGroup = "UpdateFooterLinkGroup";
    public const string DeleteFooterLinkGroup = "DeleteFooterLinkGroup";
    public const string ReorderFooterLinkGroups = "ReorderFooterLinkGroups";
    public const string CreateFooterLink = "CreateFooterLink";
    public const string UpdateFooterLink = "UpdateFooterLink";
    public const string DeleteFooterLink = "DeleteFooterLink";
    public const string GetFooterGovernanceSettings = "GetFooterGovernanceSettings";
    public const string UpdateFooterGovernanceSettings = "UpdateFooterGovernanceSettings";
    public const string UpdateTenantFooterSettings = "UpdateTenantFooterSettings";

    #endregion

}
