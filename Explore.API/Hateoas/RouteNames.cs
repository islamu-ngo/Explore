// ABOUTME: Central route name catalog for API endpoint metadata and HAL link generation.
// ABOUTME: Keeps controller route names stable and discoverable for OpenAPI and clients.

namespace Explore.API.Hateoas;

/// <summary>
/// Route name constants for HATEOAS link generation.
/// These must match the Name property on route attributes in controllers.
/// </summary>
public static class RouteNames
{
    #region SEO Routes

    public const string GetSitemap = "GetSitemap";

    #endregion

    #region Organization Routes

    public const string GetOrganizations = "GetOrganizations";
    public const string GetOrganizationById = "GetOrganizationById";
    public const string GetMyOrganizations = "GetMyOrganizations";
    public const string CreateOrganization = "CreateOrganization";
    public const string UpdateOrganization = "UpdateOrganization";
    public const string UpdateOrganizationApprovalStatus = "UpdateOrganizationApprovalStatus";
    public const string DeleteOrganization = "DeleteOrganization";
    public const string GetOrganizationEvents = "GetOrganizationEvents";
    public const string GetOrganizationMembers = "GetOrganizationMembers";

    #endregion

    #region Event Routes

    public const string GetEvents = "GetEvents";
    public const string GetEventById = "GetEventById";
    public const string GetEventCalendar = "GetEventCalendar";
    public const string GetMyEvents = "GetMyEvents";
    public const string GetEventCreationContext = "GetEventCreationContext";
    public const string GetEventSessionCreateContext = "GetEventSessionCreateContext";
    public const string GetEventProgramSummary = "GetEventProgramSummary";
    public const string GetEventPublishReadiness = "GetEventPublishReadiness";
    public const string CreateEvent = "CreateEvent";
    public const string PublishEvent = "PublishEvent";
    public const string UpdateEvent = "UpdateEvent";
    public const string UpdateEventStatus = "UpdateEventStatus";
    public const string DeleteEvent = "DeleteEvent";
    public const string GetEventSessions = "GetEventSessions";
    public const string GetEventCategories = "GetEventCategories";
    public const string GetEventTags = "GetEventTags";
    public const string GetEventTemplateSyncDiff = "GetEventTemplateSyncDiff";
    public const string ApplyEventTemplateSync = "ApplyEventTemplateSync";
    public const string GetEventTemplateSyncHistory = "GetEventTemplateSyncHistory";

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
    public const string GetEventSessionLanguages = "GetEventSessionLanguages";
    public const string CreateEventSessionLanguage = "CreateEventSessionLanguage";
    public const string DeleteEventSessionLanguage = "DeleteEventSessionLanguage";
    public const string GetEventSessionSpeakers = "GetEventSessionSpeakers";
    public const string GetEventSessionAgendaItems = "GetEventSessionAgendaItems";
    public const string GetEventRegistrations = "GetEventRegistrations";
    public const string GetEventSessionTemplateSyncDiff = "GetEventSessionTemplateSyncDiff";
    public const string ApplyEventSessionTemplateSync = "ApplyEventSessionTemplateSync";
    public const string GetEventSessionTemplateSyncHistory = "GetEventSessionTemplateSyncHistory";

    public const string GetEventSessionGroupsByEvent = "GetEventSessionGroupsByEvent";
    public const string GetEventSessionGroupById = "GetEventSessionGroupById";
    public const string GetEventSessionGroupSessions = "GetEventSessionGroupSessions";
    public const string CreateEventSessionGroup = "CreateEventSessionGroup";
    public const string UpdateEventSessionGroup = "UpdateEventSessionGroup";
    public const string DeleteEventSessionGroup = "DeleteEventSessionGroup";
    public const string AssignEventSessionToGroup = "AssignEventSessionToGroup";
    public const string UnassignEventSessionFromGroup = "UnassignEventSessionFromGroup";

    #endregion

    #region Actor Routes

    public const string GetActors = "GetActors";
    public const string GetActorById = "GetActorById";
    public const string GetActorByDid = "GetActorByDid";
    public const string GetActorsByTenant = "GetActorsByTenant";
    public const string CreateActor = "CreateActor";
    public const string UpdateActor = "UpdateActor";
    public const string DeleteActor = "DeleteActor";
    public const string GetActorSubscriptions = "GetActorSubscriptions";
    public const string GetActorSubscriptionByActor = "GetActorSubscriptionByActor";
    public const string SubscribeToActor = "SubscribeToActor";
    public const string UpdateActorSubscriptionNotificationLevel = "UpdateActorSubscriptionNotificationLevel";
    public const string UnsubscribeFromActor = "UnsubscribeFromActor";
    public const string GetActorEvents = "GetActorEvents";
    public const string GetActorKeyStores = "GetActorKeyStores";
    public const string GetActorKeyStoreById = "GetActorKeyStoreById";
    public const string CreateActorKeyStore = "CreateActorKeyStore";
    public const string UpdateActorKeyStore = "UpdateActorKeyStore";
    public const string DeleteActorKeyStore = "DeleteActorKeyStore";
    public const string GetActorTypes = "GetActorTypes";
    public const string GetActorTypeById = "GetActorTypeById";

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
    public const string GetApprovalStatusOptions = "GetApprovalStatusOptions";
    public const string CreateApprovalStatusOption = "CreateApprovalStatusOption";
    public const string GetApprovalStatusOptionById = "GetApprovalStatusOptionById";
    public const string UpdateApprovalStatusOption = "UpdateApprovalStatusOption";
    public const string DeleteApprovalStatusOption = "DeleteApprovalStatusOption";
    public const string GetAudienceAgeOptions = "GetAudienceAgeOptions";
    public const string GetAudienceAgeOptionById = "GetAudienceAgeOptionById";
    public const string GetAudienceGenderOptions = "GetAudienceGenderOptions";
    public const string GetAudienceGenderOptionById = "GetAudienceGenderOptionById";
    public const string GetCategoryTypeOptions = "GetCategoryTypeOptions";
    public const string GetCategoryTypeOptionById = "GetCategoryTypeOptionById";
    public const string GetCategoryTypeOptionsWithCategories = "GetCategoryTypeOptionsWithCategories";
    public const string GetDidCustodyTypeOptions = "GetDidCustodyTypeOptions";
    public const string GetDidCustodyTypeOptionById = "GetDidCustodyTypeOptionById";
    public const string GetEventFormatOptions = "GetEventFormatOptions";
    public const string GetEventFormatOptionById = "GetEventFormatOptionById";
    public const string GetEventStatusById = "GetEventStatusById";
    public const string GetLanguageById = "GetLanguageById";
    public const string GetMadhabById = "GetMadhabById";
    public const string GetRegistrationModes = "GetRegistrationModes";
    public const string GetRegistrationModeById = "GetRegistrationModeById";
    public const string GetRegistrationScopes = "GetRegistrationScopes";
    public const string GetEventSessionKinds = "GetEventSessionKinds";
    public const string GetScheduleItemKinds = "GetScheduleItemKinds";
    public const string GetVisibilityTypes = "GetVisibilityTypes";
    public const string GetVisibilityTypeById = "GetVisibilityTypeById";
    public const string GetFileTypes = "GetFileTypes";
    public const string GetFileTypeById = "GetFileTypeById";
    public const string GetGroupPositions = "GetGroupPositions";
    public const string GetGroupPositionById = "GetGroupPositionById";
    public const string GetOrganizationPositions = "GetOrganizationPositions";
    public const string GetOrganizationPositionById = "GetOrganizationPositionById";
    public const string GetEventTypeById = "GetEventTypeById";
    public const string CreateEventType = "CreateEventType";
    public const string UpdateEventType = "UpdateEventType";
    public const string DeleteEventType = "DeleteEventType";
    public const string GetTagTypeById = "GetTagTypeById";
    public const string GetTagTypesWithTags = "GetTagTypesWithTags";
    public const string GetMyFeatureFlags = "GetMyFeatureFlags";
    public const string GetAiAssistantBootstrap = "GetAiAssistantBootstrap";
    public const string GetAiConversations = "GetAiConversations";
    public const string CreateAiConversation = "CreateAiConversation";
    public const string GetAiConversation = "GetAiConversation";
    public const string SearchAiReferences = "SearchAiReferences";
    public const string SendAiMessage = "SendAiMessage";
    public const string ConfirmAiProposedAction = "ConfirmAiProposedAction";
    public const string RejectAiProposedAction = "RejectAiProposedAction";
    public const string GetAiRunStatus = "GetAiRunStatus";
    public const string CancelAiRun = "CancelAiRun";
    public const string GetPublicExperienceSettings = "GetPublicExperienceSettings";
    public const string GetPublicExperienceShell = "GetPublicExperienceShell";
    public const string GetTranslationByLanguage = "GetTranslationByLanguage";
    public const string GetAvailableTranslationLanguages = "GetAvailableTranslationLanguages";
    public const string GetEventSeries = "GetEventSeries";
    public const string GetEventSeriesById = "GetEventSeriesById";
    public const string GetTopEventSeries = "GetTopEventSeries";
    public const string CreateEventSeries = "CreateEventSeries";
    public const string UpdateEventSeries = "UpdateEventSeries";
    public const string DeleteEventSeries = "DeleteEventSeries";
    public const string GetEventRegistrationPolicies = "GetEventRegistrationPolicies";
    public const string GetEventRegistrationById = "GetEventRegistrationById";
    public const string GetRegistrationsBySession = "GetRegistrationsBySession";
    public const string GetRegistrationsByUser = "GetRegistrationsByUser";
    public const string CreateEventRegistration = "CreateEventRegistration";
    public const string UpdateEventRegistration = "UpdateEventRegistration";
    public const string DeleteEventRegistration = "DeleteEventRegistration";

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
    public const string SyncUser = "SyncUser";
    public const string GetCurrentUserAdminAuthority = "GetCurrentUserAdminAuthority";
    public const string UpdateCurrentUser = "UpdateCurrentUser";
    public const string DeleteCurrentUser = "DeleteCurrentUser";

    #endregion

    #region Email Unsubscribe Routes

    public const string GetEmailUnsubscribe = "GetEmailUnsubscribe";
    public const string OneClickEmailUnsubscribe = "OneClickEmailUnsubscribe";

    #endregion

    #region Email Dispatch Admin Routes

    public const string GetEmailDispatchStatus = "GetEmailDispatchStatus";
    public const string PauseEmailDispatchTenant = "PauseEmailDispatchTenant";
    public const string ResumeEmailDispatchTenant = "ResumeEmailDispatchTenant";
    public const string ParkEmailDispatch = "ParkEmailDispatch";
    public const string ReplayEmailDispatch = "ReplayEmailDispatch";

    #endregion

    #region Tenant Routes

    public const string GetTenants = "GetTenants";
    public const string GetTenantById = "GetTenantById";
    public const string GetTenantBySlug = "GetTenantBySlug";
    public const string CreateTenant = "CreateTenant";
    public const string UpdateTenant = "UpdateTenant";
    public const string DeleteTenant = "DeleteTenant";
    public const string GetActiveTenantCount = "GetActiveTenantCount";

    #endregion

    #region Tenant Navigation Routes

    public const string GetTenantNavigationLinks = "GetTenantNavigationLinks";
    public const string CreateTenantNavigationLink = "CreateTenantNavigationLink";
    public const string UpdateTenantNavigationLink = "UpdateTenantNavigationLink";
    public const string DeleteTenantNavigationLink = "DeleteTenantNavigationLink";
    public const string ReorderTenantNavigationLinks = "ReorderTenantNavigationLinks";

    #endregion

    #region Tenant User Role Grant Routes

    public const string GetTenantUserRoleGrants = "GetTenantUserRoleGrants";
    public const string GetTenantUserRoleGrantById = "GetTenantUserRoleGrantById";
    public const string CreateTenantUserRoleGrant = "CreateTenantUserRoleGrant";
    public const string RevokeTenantUserRoleGrant = "RevokeTenantUserRoleGrant";

    #endregion

    #region Tenant Settings Routes

    public const string GetTenantBrandingSettingsDocument = "GetTenantBrandingSettingsDocument";
    public const string ReplaceTenantBrandingSettingsDocument = "ReplaceTenantBrandingSettingsDocument";
    public const string GetTenantStorageSettings = "GetTenantStorageSettings";
    public const string UpdateTenantStorageSettings = "UpdateTenantStorageSettings";

    #endregion

    #region Role Routes

    public const string GetRoles = "GetRoles";
    public const string GetRoleById = "GetRoleById";

    #endregion

    #region Organization Member Routes

    public const string GetOrganizationMemberById = "GetOrganizationMemberById";
    public const string GetOrganizationMembersByOrganization = "GetOrganizationMembersByOrganization";
    public const string AddOrganizationMember = "AddOrganizationMember";
    public const string CreateOrganizationMember = "CreateOrganizationMember";
    public const string UpdateOrganizationMember = "UpdateOrganizationMember";
    public const string UpdateOrganizationMemberRole = "UpdateOrganizationMemberRole";
    public const string DeleteOrganizationMember = "DeleteOrganizationMember";
    public const string GetMyOrganizationInvitations = "GetMyOrganizationInvitations";
    public const string AcceptOrganizationInvitation = "AcceptOrganizationInvitation";
    public const string DeclineOrganizationInvitation = "DeclineOrganizationInvitation";

    #endregion

    #region Group Routes

    public const string GetGroups = "GetGroups";
    public const string GetGroupById = "GetGroupById";
    public const string GetMyGroups = "GetMyGroups";
    public const string CreateGroup = "CreateGroup";
    public const string UpdateGroup = "UpdateGroup";
    public const string UpdateGroupApprovalStatus = "UpdateGroupApprovalStatus";
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
    public const string GetEventSessionAgendaItemsBySession = "GetEventSessionAgendaItemsBySession";

    #endregion

    #region Storage Object Routes

    public const string GetStorageObjects = "GetStorageObjects";
    public const string GetStorageObjectById = "GetStorageObjectById";
    public const string GetStorageObjectContent = "GetStorageObjectContent";
    public const string CreateStorageObject = "CreateStorageObject";
    public const string UpdateStorageObject = "UpdateStorageObject";
    public const string DeleteStorageObject = "DeleteStorageObject";
    public const string GetPublicStorageObjectImage = "GetPublicStorageObjectImage";
    public const string GetStorageObjectPresignedDownloadUrl = "GetStorageObjectPresignedDownloadUrl";
    public const string GenerateStorageObjectUploadUrl = "GenerateStorageObjectUploadUrl";
    public const string CreateStorageUploadSession = "CreateStorageUploadSession";
    public const string UploadStorageUploadSessionContent = "UploadStorageUploadSessionContent";
    public const string CancelStorageUploadSession = "CancelStorageUploadSession";

    #endregion

    #region Organization Review Routes

    public const string GetOrganizationReviews = "GetOrganizationReviews";
    public const string GetOrganizationReviewById = "GetOrganizationReviewById";
    public const string GetOrganizationReviewsByOrganization = "GetOrganizationReviewsByOrganization";
    public const string GetOrganizationReviewsByUser = "GetOrganizationReviewsByUser";
    public const string CreateOrganizationReview = "CreateOrganizationReview";
    public const string UpdateOrganizationReview = "UpdateOrganizationReview";
    public const string DeleteOrganizationReview = "DeleteOrganizationReview";

    #endregion

    #region Notification Routes

    public const string GetNotifications = "GetNotifications";
    public const string GetNotificationById = "GetNotificationById";
    public const string GetUnreadNotificationCount = "GetUnreadNotificationCount";
    public const string GetNotificationRefreshStream = "GetNotificationRefreshStream";
    public const string MarkNotificationAsRead = "MarkNotificationAsRead";
    public const string MarkAllNotificationsAsRead = "MarkAllNotificationsAsRead";
    public const string ArchiveNotification = "ArchiveNotification";
    public const string SnoozeNotification = "SnoozeNotification";
    public const string DeleteNotification = "DeleteNotification";

    #endregion

    #region User Appearance Routes

    public const string GetCurrentUserAppearancePreferences = "GetCurrentUserAppearancePreferences";
    public const string UpdateCurrentUserAppearancePreferences = "UpdateCurrentUserAppearancePreferences";
    public const string GetAvailableThemes = "GetAvailableThemes";
    public const string GetUserAppearanceProfiles = "GetUserAppearanceProfiles";
    public const string ClonePresetToProfile = "ClonePresetToProfile";
    public const string CreateCustomAppearanceProfile = "CreateCustomAppearanceProfile";
    public const string UpdateAppearanceProfile = "UpdateAppearanceProfile";
    public const string SetActiveAppearanceProfile = "SetActiveAppearanceProfile";
    public const string SetAppearanceThemeMode = "SetAppearanceThemeMode";
    public const string GenerateAppearancePalette = "GenerateAppearancePalette";
    public const string ArchiveAppearanceProfile = "ArchiveAppearanceProfile";
    public const string DuplicateAppearanceProfile = "DuplicateAppearanceProfile";

    #endregion

    #region UI Theme Admin Routes

    public const string GetUiThemeCatalog = "GetUiThemeCatalog";
    public const string GetUiThemeDetails = "GetUiThemeDetails";
    public const string CreateUiTheme = "CreateUiTheme";
    public const string UpdateUiTheme = "UpdateUiTheme";
    public const string DeleteUiTheme = "DeleteUiTheme";

    #endregion

    #region ATProto Routes

    public const string GetAtprotoRecords = "GetAtprotoRecords";
    public const string GetAtprotoRecordById = "GetAtprotoRecordById";
    public const string GetAtprotoRecordByUri = "GetAtprotoRecordByUri";
    public const string CreateAtprotoRecord = "CreateAtprotoRecord";
    public const string DeleteAtprotoRecord = "DeleteAtprotoRecord";
    public const string GetAtprotoRecordEntries = "GetAtprotoRecordEntries";
    public const string GetAtprotoRecordEntryById = "GetAtprotoRecordEntryById";
    public const string CreateAtprotoRecordEntry = "CreateAtprotoRecordEntry";
    public const string UpdateAtprotoRecordEntry = "UpdateAtprotoRecordEntry";
    public const string DeleteAtprotoRecordEntry = "DeleteAtprotoRecordEntry";

    public const string GetIndexedDids = "GetIndexedDids";
    public const string GetIndexedDidByDid = "GetIndexedDidByDid";
    public const string CreateIndexedDid = "CreateIndexedDid";
    public const string UpdateIndexedDid = "UpdateIndexedDid";
    public const string DeleteIndexedDid = "DeleteIndexedDid";

    #endregion

    #region Contact Share Consent Routes

    public const string GetUserContactShareConsents = "GetUserContactShareConsents";
    public const string CheckConsentForOrganizer = "CheckConsentForOrganizer";
    public const string WithdrawContactShareConsent = "WithdrawContactShareConsent";
    public const string GetOrganizationSharedContacts = "GetOrganizationSharedContacts";
    public const string ExportOrganizationSharedContacts = "ExportOrganizationSharedContacts";

    #endregion

    #region Localization Admin Routes

    public const string CheckLocalizationBundleHealth = "CheckLocalizationBundleHealth";
    public const string GetLocalizationConfiguration = "GetLocalizationConfiguration";
    public const string ExportLocalizationFromTms = "ExportLocalizationFromTms";
    public const string UpdateLocalizationGovernance = "UpdateLocalizationGovernance";
    public const string TestLocalizationTmsConnection = "TestLocalizationTmsConnection";

    #endregion

    #region Custom Property Definition Routes

    public const string GetCustomPropertyDefinitions = "GetCustomPropertyDefinitions";
    public const string GetCustomPropertyDefinitionById = "GetCustomPropertyDefinitionById";
    public const string CreateCustomPropertyDefinition = "CreateCustomPropertyDefinition";
    public const string UpdateCustomPropertyDefinition = "UpdateCustomPropertyDefinition";
    public const string DeleteCustomPropertyDefinition = "DeleteCustomPropertyDefinition";
    public const string PurgeCustomPropertyDefinition = "PurgeCustomPropertyDefinition";

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
    public const string PurgeEventCustomPropertyDefinition = "PurgeEventCustomPropertyDefinition";
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
    public const string PurgeEventSessionCustomPropertyDefinition = "PurgeEventSessionCustomPropertyDefinition";
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

    #region Event Day Routes

    public const string GetEventDaysByEvent = "GetEventDaysByEvent";
    public const string GetEventDayById = "GetEventDayById";
    public const string CreateEventDay = "CreateEventDay";
    public const string UpdateEventDay = "UpdateEventDay";
    public const string DeleteEventDay = "DeleteEventDay";

    #endregion

    #region Event Agenda Item Routes

    public const string GetEventAgendaItemsByEvent = "GetEventAgendaItemsByEvent";
    public const string GetEventAgendaItemById = "GetEventAgendaItemById";
    public const string CreateEventAgendaItem = "CreateEventAgendaItem";
    public const string UpdateEventAgendaItem = "UpdateEventAgendaItem";
    public const string DeleteEventAgendaItem = "DeleteEventAgendaItem";
    public const string GetEventAgendaProjection = "GetEventAgendaProjection";

    #endregion

    #region Location Room Routes

    public const string GetLocationRoomsByLocation = "GetLocationRoomsByLocation";
    public const string GetLocationRoomById = "GetLocationRoomById";
    public const string CreateLocationRoom = "CreateLocationRoom";
    public const string UpdateLocationRoom = "UpdateLocationRoom";
    public const string DeleteLocationRoom = "DeleteLocationRoom";

    #endregion

    #region Custom Property Projection Admin Routes

    public const string GetCustomPropertyProjectionStatus = "GetCustomPropertyProjectionStatus";
    public const string RebuildCustomPropertyProjection = "RebuildCustomPropertyProjection";
    public const string RebuildSingleEventCustomPropertyProjection = "RebuildSingleEventCustomPropertyProjection";
    public const string DrainCustomPropertyProjectionDirtyScopes = "DrainCustomPropertyProjectionDirtyScopes";
    public const string GetCustomPropertyProjectionDirtyScopes = "GetCustomPropertyProjectionDirtyScopes";
    public const string GetCustomPropertyProjectionsForEvent = "GetCustomPropertyProjectionsForEvent";
    public const string GetSessionCustomPropertyProjectionStatus = "GetSessionCustomPropertyProjectionStatus";
    public const string RebuildSessionCustomPropertyProjection = "RebuildSessionCustomPropertyProjection";
    public const string RebuildSingleSessionCustomPropertyProjection = "RebuildSingleSessionCustomPropertyProjection";
    public const string GetCustomPropertyProjectionsForSession = "GetCustomPropertyProjectionsForSession";
    public const string GetCustomPropertyGovernanceReport = "GetCustomPropertyGovernanceReport";

    #endregion

    #region Sync State Routes

    public const string GetSyncStates = "GetSyncStates";
    public const string GetSyncStateById = "GetSyncStateById";
    public const string CreateSyncState = "CreateSyncState";
    public const string UpdateSyncState = "UpdateSyncState";
    public const string DeleteSyncState = "DeleteSyncState";

    #endregion

    #region User Authentication Token Routes

    public const string GetUserAuthenticationTokens = "GetUserAuthenticationTokens";
    public const string GetUserAuthenticationTokenById = "GetUserAuthenticationTokenById";
    public const string CreateUserAuthenticationToken = "CreateUserAuthenticationToken";
    public const string UpdateUserAuthenticationToken = "UpdateUserAuthenticationToken";
    public const string DeleteUserAuthenticationToken = "DeleteUserAuthenticationToken";

    #endregion

    #region User External Login Routes

    public const string GetUserExternalLogins = "GetUserExternalLogins";
    public const string GetUserExternalLoginById = "GetUserExternalLoginById";
    public const string CreateUserExternalLogin = "CreateUserExternalLogin";
    public const string UpdateUserExternalLogin = "UpdateUserExternalLogin";
    public const string DeleteUserExternalLogin = "DeleteUserExternalLogin";

    #endregion

    #region External API Key Routes

    public const string GetExternalApiKeys = "GetExternalApiKeys";
    public const string GetExternalApiKeyById = "GetExternalApiKeyById";
    public const string CreateExternalApiKey = "CreateExternalApiKey";
    public const string UpdateExternalApiKey = "UpdateExternalApiKey";
    public const string DeleteExternalApiKey = "DeleteExternalApiKey";
    public const string GetExternalApiKeyUsageReport = "GetExternalApiKeyUsageReport";

    #endregion

    #region Managed Provider Provisioning Routes

    public const string EnsureManagedProviderClientProvisioned = "EnsureManagedProviderClientProvisioned";

    #endregion

    #region Module Routes

    public const string GetAvailableModules = "GetAvailableModules";
    public const string GetEnabledModules = "GetEnabledModules";
    public const string CheckModuleEnabled = "CheckModuleEnabled";
    public const string GetModuleSchemaUrl = "GetModuleSchemaUrl";
    public const string EnableModule = "EnableModule";
    public const string DisableModule = "DisableModule";

    #endregion

    #region Instance Settings Routes

    public const string GetInstanceModuleSettings = "GetInstanceModuleSettings";
    public const string UpdateInstanceModuleSettings = "UpdateInstanceModuleSettings";
    public const string GetInstanceEventPolicy = "GetInstanceEventPolicy";
    public const string UpdateInstanceEventPolicy = "UpdateInstanceEventPolicy";
    public const string GetInstanceOrganizationPolicy = "GetInstanceOrganizationPolicy";
    public const string UpdateInstanceOrganizationPolicy = "UpdateInstanceOrganizationPolicy";
    public const string GetInstanceBrandingSettings = "GetInstanceBrandingSettings";
    public const string UpdateInstanceBrandingSettings = "UpdateInstanceBrandingSettings";
    public const string GetInstanceDomainSettings = "GetInstanceDomainSettings";
    public const string UpdateInstanceDomainSettings = "UpdateInstanceDomainSettings";
    public const string GetInstanceTenantDelegationSettings = "GetInstanceTenantDelegationSettings";
    public const string UpdateInstanceTenantDelegationSettings = "UpdateInstanceTenantDelegationSettings";
    public const string GetInstanceRenderPolicySettings = "GetInstanceRenderPolicySettings";
    public const string UpdateInstanceRenderPolicySettings = "UpdateInstanceRenderPolicySettings";
    public const string GetInstanceDeploymentMode = "GetInstanceDeploymentMode";
    public const string UpdateInstanceDeploymentMode = "UpdateInstanceDeploymentMode";
    public const string GetInstanceStorageSettings = "GetInstanceStorageSettings";
    public const string UpdateInstanceStorageSettings = "UpdateInstanceStorageSettings";
    public const string TestInstanceStorageConnection = "TestInstanceStorageConnection";
    public const string RecalculateInstanceStorageUsage = "RecalculateInstanceStorageUsage";
    public const string GetInstanceSmtpSettings = "GetInstanceSmtpSettings";
    public const string UpdateInstanceSmtpSettings = "UpdateInstanceSmtpSettings";
    public const string TestInstanceSmtpConnection = "TestInstanceSmtpConnection";
    public const string GetInstanceResolverConfiguration = "GetInstanceResolverConfiguration";
    public const string UpdateInstanceResolverConfiguration = "UpdateInstanceResolverConfiguration";
    public const string GetInstanceAnalyticsGovernanceSettings = "GetInstanceAnalyticsGovernanceSettings";
    public const string UpdateInstanceAnalyticsGovernanceSettings = "UpdateInstanceAnalyticsGovernanceSettings";
    public const string GetInstanceAuthProviderConfiguration = "GetInstanceAuthProviderConfiguration";
    public const string UpdateInstanceAuthProviderConfiguration = "UpdateInstanceAuthProviderConfiguration";
    public const string RunInstanceKeycloakRealmDoctor = "RunInstanceKeycloakRealmDoctor";
    public const string PreviewInstanceKeycloakRealmSync = "PreviewInstanceKeycloakRealmSync";
    public const string ApplyInstanceKeycloakRealmSync = "ApplyInstanceKeycloakRealmSync";
    public const string RotateInstanceKeycloakClientSecret = "RotateInstanceKeycloakClientSecret";
    public const string GetInstanceAuthProviderConfigurationStatus = "GetInstanceAuthProviderConfigurationStatus";
    public const string GetInstanceAuthorizationProviderConfiguration = "GetInstanceAuthorizationProviderConfiguration";
    public const string UpdateInstanceAuthorizationProviderConfiguration = "UpdateInstanceAuthorizationProviderConfiguration";
    public const string GetInstanceAuthorizationProviderConfigurationStatus = "GetInstanceAuthorizationProviderConfigurationStatus";
    public const string SyncInstanceAuthorizationPolicyPackage = "SyncInstanceAuthorizationPolicyPackage";
    public const string DownloadInstanceAuthorizationPolicyPackage = "DownloadInstanceAuthorizationPolicyPackage";

    #endregion

    #region System Routes

    public const string GetSystemOnboardingStatus = "GetSystemOnboardingStatus";
    public const string GetSystemOnboardingPreflight = "GetSystemOnboardingPreflight";

    #endregion

    #region Instance Onboarding Routes

    public const string GetInstanceOnboardingStatus = "GetInstanceOnboardingStatus";
    public const string CompleteInstanceOnboarding = "CompleteInstanceOnboarding";
    public const string ValidateInstanceSetupSecret = "ValidateInstanceSetupSecret";
    public const string GetInstanceOnboardingAuthProviderConfiguration = "GetInstanceOnboardingAuthProviderConfiguration";
    public const string GetInstanceOnboardingAuthProviderConfigurationInternal = "GetInstanceOnboardingAuthProviderConfigurationInternal";
    public const string SaveInstanceOnboardingAuthProviderConfiguration = "SaveInstanceOnboardingAuthProviderConfiguration";
    public const string BootstrapInstanceOnboardingKeycloakRealm = "BootstrapInstanceOnboardingKeycloakRealm";
    public const string GetInstanceOnboardingAuthorizationProviderConfigurationInternal = "GetInstanceOnboardingAuthorizationProviderConfigurationInternal";
    public const string SaveInstanceOnboardingAuthorizationProviderConfiguration = "SaveInstanceOnboardingAuthorizationProviderConfiguration";
    public const string VerifyInstanceOnboardingAuthorizationProviderEndpoint = "VerifyInstanceOnboardingAuthorizationProviderEndpoint";
    public const string SyncInstanceOnboardingAuthorizationPolicyPackage = "SyncInstanceOnboardingAuthorizationPolicyPackage";
    public const string DownloadInstanceOnboardingAuthorizationPolicyPackage = "DownloadInstanceOnboardingAuthorizationPolicyPackage";

    #endregion

    #region Tenant Onboarding Routes

    public const string GetTenantOnboardingStatus = "GetTenantOnboardingStatus";
    public const string GetTenantOnboardingPolicySettings = "GetTenantOnboardingPolicySettings";
    public const string CompleteTenantOnboarding = "CompleteTenantOnboarding";
    public const string UpdateTenantOnboardingPolicySettings = "UpdateTenantOnboardingPolicySettings";
    public const string SaveTenantOnboardingStepProgress = "SaveTenantOnboardingStepProgress";

    #endregion

    #region Admin Utility Routes

    public const string ApplyDatabaseMigrations = "ApplyDatabaseMigrations";

    #endregion

}
