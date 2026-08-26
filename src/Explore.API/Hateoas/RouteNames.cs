// ABOUTME: Central route name catalog for API endpoint metadata and HAL link generation.
// ABOUTME: Keeps controller route names stable and discoverable for OpenAPI and clients.

namespace Explore.API.Hateoas;

/// <summary>
/// Route name constants for HATEOAS link generation.
/// These must match the Name property on route attributes in controllers.
/// </summary>
public static class RouteNames
{
    public const string RequestAdmissionTicketRecovery = "RequestAdmissionTicketRecovery";
    public const string ConsumeAdmissionTicketRecovery = "ConsumeAdmissionTicketRecovery";
    public const string GetCurrentAdmissionTickets = "GetCurrentAdmissionTickets";
    public const string GetCurrentAdmissionTicket = "GetCurrentAdmissionTicket";
    public const string ReissueCurrentAdmissionTicketQr = "ReissueCurrentAdmissionTicketQr";
    public const string ReissueCurrentAdmissionTicketPrint = "ReissueCurrentAdmissionTicketPrint";
    public const string ListAdmissionScannerCapabilities = "ListAdmissionScannerCapabilities";
    public const string IssueAdmissionScannerCapability = "IssueAdmissionScannerCapability";
    public const string RevokeAdmissionScannerCapability = "RevokeAdmissionScannerCapability";
    public const string CheckInAdmission = "CheckInAdmission";
    public const string GetAdmissionCheckIn = "GetAdmissionCheckIn";
    public const string BatchCheckInAdmissions = "BatchCheckInAdmissions";
    public const string UndoAdmissionCheckIn = "UndoAdmissionCheckIn";
    public const string GetAdmissionCheckInSummary = "GetAdmissionCheckInSummary";
    public const string GetAdmissionCheckInAudit = "GetAdmissionCheckInAudit";
    public const string ScannerCheckInAdmission = "ScannerCheckInAdmission";
    public const string ScannerBatchCheckInAdmissions = "ScannerBatchCheckInAdmissions";
    public const string ScannerUndoAdmissionCheckIn = "ScannerUndoAdmissionCheckIn";
    public const string GetAdmissionCheckInHealth = "GetAdmissionCheckInHealth";
    public const string StopAdmissionCheckIn = "StopAdmissionCheckIn";
    public const string RestoreAdmissionCheckIn = "RestoreAdmissionCheckIn";
    public const string ReconcileAdmissionCheckIn = "ReconcileAdmissionCheckIn";

    #region Managed Event Routes

    public const string GetManagementCapabilities = "GetManagementCapabilities";
    public const string TriggerManagementRegistration = "TriggerManagementRegistration";
    public const string GetManagedEventInstance = "GetManagedEventInstance";
    public const string GetManagementVersion = "GetManagementVersion";
    public const string GetManagementHealth = "GetManagementHealth";
    public const string EvaluateManagementUpgradePreflight = "EvaluateManagementUpgradePreflight";
    public const string VerifyManagementUpgradePostflight = "VerifyManagementUpgradePostflight";
    public const string RotateManagedControlPlaneCredential = "RotateManagedControlPlaneCredential";
    public const string RevokeManagedControlPlaneRegistration = "RevokeManagedControlPlaneRegistration";
    public const string EvaluateManagedTenantProvisioningPreflight = "EvaluateManagedTenantProvisioningPreflight";
    public const string ScheduleManagedTenantProvisioning = "ScheduleManagedTenantProvisioning";
    public const string GetManagedTenantProvisioningOperation = "GetManagedTenantProvisioningOperation";
    public const string CancelManagedTenantProvisioningOperation = "CancelManagedTenantProvisioningOperation";

    #endregion

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
    public const string GetOrganizationTenantEvidenceCollection = "GetOrganizationTenantEvidenceCollection";
    public const string GetOrganizationTenantEvidence = "GetOrganizationTenantEvidence";
    public const string CreateOrganizationTenantEvidenceUploadSession = "CreateOrganizationTenantEvidenceUploadSession";
    public const string SubmitOrganizationTenantEvidence = "SubmitOrganizationTenantEvidence";
    public const string ReviewOrganizationTenantEvidence = "ReviewOrganizationTenantEvidence";
    public const string GetOrganizationNotificationPreferences = "GetOrganizationNotificationPreferences";
    public const string UpdateOrganizationNotificationPreferences = "UpdateOrganizationNotificationPreferences";
    public const string SetOrganizationNotificationPreferenceMute = "SetOrganizationNotificationPreferenceMute";
    public const string DeleteOrganization = "DeleteOrganization";
    #endregion

    #region Event Routes

    public const string GetEvents = "GetEvents";
    public const string GetEventById = "GetEventById";
    public const string GetEventByPublicCode = "GetEventByPublicCode";
    public const string GetEventOpenGraphImage = "GetEventOpenGraphImage";
    public const string GetEventManagementDetails = "GetEventManagementDetails";
    public const string GetEventModerationHistory = "GetEventModerationHistory";
    public const string GetManagedEventsByActor = "GetManagedEventsByActor";
    public const string GetEventCalendar = "GetEventCalendar";
    public const string GetAttendeeEventCalendar = "GetAttendeeEventCalendar";
    public const string GetMyEvents = "GetMyEvents";
    public const string GetEventCreationContext = "GetEventCreationContext";
    public const string GetEventSessionCreateContext = "GetEventSessionCreateContext";
    public const string GetEventProgramSummary = "GetEventProgramSummary";
    public const string GetManagedEventProgramSummary = "GetManagedEventProgramSummary";
    public const string GetEventPublishReadiness = "GetEventPublishReadiness";
    public const string GetEventPublicActions = "GetEventPublicActions";
    public const string GetEventPublicAction = "GetEventPublicAction";
    public const string RedirectEventPublicAction = "RedirectEventPublicAction";
    public const string CreateEventPublicAction = "CreateEventPublicAction";
    public const string UpdateEventPublicAction = "UpdateEventPublicAction";
    public const string DeleteEventPublicAction = "DeleteEventPublicAction";
    public const string ConfigureEventParticipation = "ConfigureEventParticipation";
    public const string AttachRegistrationRequirement = "AttachRegistrationRequirement";
    public const string DetachRegistrationRequirement = "DetachRegistrationRequirement";
    public const string GetOptionalQuestionnaire = "GetOptionalQuestionnaire";
    public const string GetRegistrationWorkflow = "GetRegistrationWorkflow";
    public const string GetRegistrationAnswerAnalytics = "GetRegistrationAnswerAnalytics";
    public const string LaunchAuthenticatedNativeRegistrationAttempt = "LaunchAuthenticatedNativeRegistrationAttempt";
    public const string LaunchAuthenticatedRegistrationProviderAttempt = "LaunchAuthenticatedRegistrationProviderAttempt";
    public const string SubmitAuthenticatedNativeRegistrationAttempt = "SubmitAuthenticatedNativeRegistrationAttempt";
    public const string SkipAuthenticatedNativeRegistrationRequirement = "SkipAuthenticatedNativeRegistrationRequirement";
    public const string GetAuthenticatedNativeRegistrationRequirementProgress = "GetAuthenticatedNativeRegistrationRequirementProgress";
    public const string LaunchGuestNativeRegistrationAttempt = "LaunchGuestNativeRegistrationAttempt";
    public const string LaunchGuestRegistrationProviderAttempt = "LaunchGuestRegistrationProviderAttempt";
    public const string SubmitGuestNativeRegistrationAttempt = "SubmitGuestNativeRegistrationAttempt";
    public const string SkipGuestNativeRegistrationRequirement = "SkipGuestNativeRegistrationRequirement";
    public const string GetGuestNativeRegistrationRequirementProgress = "GetGuestNativeRegistrationRequirementProgress";
    public const string CreateRegistrationWorkflow = "CreateRegistrationWorkflow";
    public const string UpdateRegistrationWorkflow = "UpdateRegistrationWorkflow";
    public const string CreateRegistrationRequirement = "CreateRegistrationRequirement";
    public const string UpdateRegistrationRequirement = "UpdateRegistrationRequirement";
    public const string DeleteRegistrationRequirement = "DeleteRegistrationRequirement";
    public const string GetRegistrationForm = "GetRegistrationForm";
    public const string CreateRegistrationForm = "CreateRegistrationForm";
    public const string GetRegistrationFormVersion = "GetRegistrationFormVersion";
    public const string CreateRegistrationFormVersion = "CreateRegistrationFormVersion";
    public const string AddRegistrationFormSection = "AddRegistrationFormSection";
    public const string UpdateRegistrationFormSection = "UpdateRegistrationFormSection";
    public const string ReorderRegistrationFormSections = "ReorderRegistrationFormSections";
    public const string DeleteRegistrationFormSection = "DeleteRegistrationFormSection";
    public const string AddRegistrationFormField = "AddRegistrationFormField";
    public const string UpdateRegistrationFormField = "UpdateRegistrationFormField";
    public const string ReorderRegistrationFormFields = "ReorderRegistrationFormFields";
    public const string DeleteRegistrationFormField = "DeleteRegistrationFormField";
    public const string AddRegistrationFormFieldOption = "AddRegistrationFormFieldOption";
    public const string UpdateRegistrationFormFieldOption = "UpdateRegistrationFormFieldOption";
    public const string RetireRegistrationFormFieldOption = "RetireRegistrationFormFieldOption";
    public const string AddRegistrationFormRule = "AddRegistrationFormRule";
    public const string UpdateRegistrationFormRule = "UpdateRegistrationFormRule";
    public const string DeleteRegistrationFormRule = "DeleteRegistrationFormRule";
    public const string GetRegistrationFormPublishPreflight = "GetRegistrationFormPublishPreflight";
    public const string PublishRegistrationFormVersion = "PublishRegistrationFormVersion";
    public const string GetRegistrationFormTemplates = "GetRegistrationFormTemplates";
    public const string GetRegistrationFormTemplate = "GetRegistrationFormTemplate";
    public const string CreateRegistrationFormTemplate = "CreateRegistrationFormTemplate";
    public const string InstantiateRegistrationFormTemplate = "InstantiateRegistrationFormTemplate";
    public const string GetRegistrationAnswerFile = "GetRegistrationAnswerFile";
    public const string ReleaseRegistrationAnswerFile = "ReleaseRegistrationAnswerFile";
    public const string GetRegistrationProviderHealth = "GetRegistrationProviderHealth";
    public const string GetRegistrationProviderQueue = "GetRegistrationProviderQueue";
    public const string GetRegistrationProviderConnections = "GetRegistrationProviderConnections";
    public const string GetRegistrationProviderConnection = "GetRegistrationProviderConnection";
    public const string CreateRegistrationProviderConnection = "CreateRegistrationProviderConnection";
    public const string UpdateRegistrationProviderConnection = "UpdateRegistrationProviderConnection";
    public const string DeleteRegistrationProviderConnection = "DeleteRegistrationProviderConnection";
    public const string ReplaceRegistrationProviderApprovedOrigins = "ReplaceRegistrationProviderApprovedOrigins";
    public const string ImportExternalRegistrationProviderFormVersion = "ImportExternalRegistrationProviderFormVersion";
    public const string GetRegistrationProviderBindings = "GetRegistrationProviderBindings";
    public const string GetRegistrationProviderBinding = "GetRegistrationProviderBinding";
    public const string CreateRegistrationProviderBinding = "CreateRegistrationProviderBinding";
    public const string UpdateRegistrationProviderBinding = "UpdateRegistrationProviderBinding";
    public const string DeleteRegistrationProviderBinding = "DeleteRegistrationProviderBinding";
    public const string PublishRegistrationProviderBinding = "PublishRegistrationProviderBinding";
    public const string ReplaceRegistrationProviderMappings = "ReplaceRegistrationProviderMappings";
    public const string GetRegistrationChannels = "GetRegistrationChannels";
    public const string CreateRegistrationChannel = "CreateRegistrationChannel";
    public const string UpdateRegistrationChannel = "UpdateRegistrationChannel";
    public const string DeleteRegistrationChannel = "DeleteRegistrationChannel";
    public const string GetRegistrationProviderLaunchDescriptor = "GetRegistrationProviderLaunchDescriptor";
    public const string PollRegistrationProviderReconciliation = "PollRegistrationProviderReconciliation";
    public const string QueueManualRegistrationProviderImport = "QueueManualRegistrationProviderImport";
    public const string RetryRegistrationProviderParkedItem = "RetryRegistrationProviderParkedItem";
    public const string ResolveRegistrationProviderQueueItem = "ResolveRegistrationProviderQueueItem";
    public const string GetEventTicketCatalogManagement = "GetEventTicketCatalogManagement";
    public const string CreateEventTicketCatalogDraft = "CreateEventTicketCatalogDraft";
    public const string CloneEventTicketCatalogDraft = "CloneEventTicketCatalogDraft";
    public const string CreateEventTicketType = "CreateEventTicketType";
    public const string UpdateEventTicketType = "UpdateEventTicketType";
    public const string DeleteEventTicketType = "DeleteEventTicketType";
    public const string CreateEventCapacityPool = "CreateEventCapacityPool";
    public const string UpdateEventCapacityPool = "UpdateEventCapacityPool";
    public const string DeleteEventCapacityPool = "DeleteEventCapacityPool";
    public const string GetPaidEventPublicationPreflight = "GetPaidEventPublicationPreflight";
    public const string GetPaidCheckoutSaleControl = "GetPaidCheckoutSaleControl";
    public const string StopPaidCheckoutSales = "StopPaidCheckoutSales";
    public const string RequestPaidCheckoutResume = "RequestPaidCheckoutResume";
    public const string ReviewPaidCheckoutResume = "ReviewPaidCheckoutResume";
    public const string RequestPaidCheckoutReview = "RequestPaidCheckoutReview";
    public const string DecidePaidCheckoutReview = "DecidePaidCheckoutReview";
    public const string UpdateEventTicketCatalogCommercialDisclosures = "UpdateEventTicketCatalogCommercialDisclosures";
    public const string GetEventOrganizerPaymentConnection = "GetEventOrganizerPaymentConnection";
    public const string StartEventOrganizerPaymentOnboarding = "StartEventOrganizerPaymentOnboarding";
    public const string ReturnEventOrganizerPaymentOnboarding = "ReturnEventOrganizerPaymentOnboarding";
    public const string RefreshEventOrganizerPaymentOnboarding = "RefreshEventOrganizerPaymentOnboarding";
    public const string PublishEventTicketCatalog = "PublishEventTicketCatalog";
    public const string GetEventPromotions = "GetEventPromotions";
    public const string GetEventPromotion = "GetEventPromotion";
    public const string CreateEventPromotionDraft = "CreateEventPromotionDraft";
    public const string ReviseEventPromotion = "ReviseEventPromotion";
    public const string PublishEventPromotion = "PublishEventPromotion";
    public const string RevokeEventPromotion = "RevokeEventPromotion";
    public const string RotateEventPromotionCode = "RotateEventPromotionCode";
    public const string GetEventOrganizerClaims = "GetEventOrganizerClaims";
    public const string GetEventOrganizerClaim = "GetEventOrganizerClaim";
    public const string GetClaimantOrganizerClaims = "GetClaimantOrganizerClaims";
    public const string SubmitEventOrganizerClaim = "SubmitEventOrganizerClaim";
    public const string WithdrawEventOrganizerClaim = "WithdrawEventOrganizerClaim";
    public const string ReviewEventOrganizerClaim = "ReviewEventOrganizerClaim";
    public const string CreateEvent = "CreateEvent";
    public const string ImportEvent = "ImportEvent";
    public const string PublishEvent = "PublishEvent";
    public const string ModerateEventLight = "ModerateEventLight";
    public const string ModerateEventHeavy = "ModerateEventHeavy";
    public const string UnmoderateEvent = "UnmoderateEvent";
    public const string UpdateEvent = "UpdateEvent";
    public const string ArchiveEvent = "ArchiveEvent";
    public const string CancelEvent = "CancelEvent";
    public const string DeleteEvent = "DeleteEvent";
    public const string GetEventSessions = "GetEventSessions";
    public const string GetEventTemplateSyncDiff = "GetEventTemplateSyncDiff";
    public const string ApplyEventTemplateSync = "ApplyEventTemplateSync";
    public const string GetEventTemplateSyncHistory = "GetEventTemplateSyncHistory";
    #endregion

    #region Event Location Routes

    public const string GetPublicEventLocations = "GetPublicEventLocations";
    public const string GetAttendeeEventLocations = "GetAttendeeEventLocations";
    public const string GetManagementEventLocation = "GetManagementEventLocation";
    public const string GetManagementEventLocations = "GetManagementEventLocations";
    public const string GetEventLocationReviewQueue = "GetEventLocationReviewQueue";
    public const string UpdateEventLocationDisclosure = "UpdateEventLocationDisclosure";
    public const string ConfirmEventLocationRemediation = "ConfirmEventLocationRemediation";

    #endregion

    #region Event Report Routes

    public const string GetEventReportOptions = "GetEventReportOptions";
    public const string SubmitEventReport = "SubmitEventReport";
    public const string GetMyEventReports = "GetMyEventReports";
    public const string GetMyEventReport = "GetMyEventReport";
    public const string UpdateMyEventReportCommunicationConsent = "UpdateMyEventReportCommunicationConsent";
    public const string GetModerationReportingRoutingState = "GetModerationReportingRoutingState";
    public const string UpdateModerationReportingRoutingSettings = "UpdateModerationReportingRoutingSettings";
    public const string TestModerationReportingProvider = "TestModerationReportingProvider";
    public const string GetTenantModerationReportingDashboard = "GetTenantModerationReportingDashboard";
    public const string GetModerationReportQueue = "GetModerationReportQueue";
    public const string GetModerationReportDetail = "GetModerationReportDetail";
    public const string TriageModerationReport = "TriageModerationReport";
    public const string AssignModerationReport = "AssignModerationReport";
    public const string DecideModerationReport = "DecideModerationReport";
    public const string ExecuteModerationReportDecision = "ExecuteModerationReportDecision";
    public const string ModerationIntegrationOspreyCallback = "ModerationIntegrationOspreyCallback";
    public const string ModerationIntegrationCoopCallback = "ModerationIntegrationCoopCallback";
    public const string IntegrationSvixOperationalCallback = "IntegrationSvixOperationalCallback";
    public const string IntegrationStripeConnectCallback = "IntegrationStripeConnectCallback";
    public const string RegistrationProviderCallback = "RegistrationProviderCallback";

    public const string GetListmonkIntegrationSettings = "GetListmonkIntegrationSettings";
    public const string ResolveIntegrationSyncAmbiguity = "ResolveIntegrationSyncAmbiguity";
    public const string UpdateListmonkIntegrationSettings = "UpdateListmonkIntegrationSettings";
    public const string RotateListmonkIntegrationCredentials = "RotateListmonkIntegrationCredentials";
    public const string TestListmonkIntegrationConnection = "TestListmonkIntegrationConnection";

    #endregion

    #region Event Session Routes

    public const string GetEventSessions_List = "GetEventSessionsList";
    public const string GetEventSessionById = "GetEventSessionById";
    public const string GetManagedEventSessionById = "GetManagedEventSessionById";
    public const string GetManagedEventSessionsByEvent = "GetManagedEventSessionsByEvent";
    public const string CreateEventSession = "CreateEventSession";
    public const string CreateDraftEventSession = "CreateDraftEventSession";
    public const string ScheduleEventSession = "ScheduleEventSession";
    public const string PublishEventSession = "PublishEventSession";
    public const string ArchiveEventSession = "ArchiveEventSession";
    public const string CancelEventSession = "CancelEventSession";
    public const string CompleteEventSession = "CompleteEventSession";
    public const string UpdateEventSession = "UpdateEventSession";
    public const string DeleteEventSession = "DeleteEventSession";
    public const string GetEventSessionLanguages = "GetEventSessionLanguages";
    public const string GetManagedEventSessionLanguages = "GetManagedEventSessionLanguages";
    public const string CreateEventSessionLanguage = "CreateEventSessionLanguage";
    public const string UpdateEventSessionLanguage = "UpdateEventSessionLanguage";
    public const string DeleteEventSessionLanguage = "DeleteEventSessionLanguage";
    public const string GetEventSessionSpeakersBySession = "GetEventSessionSpeakersBySession";
    public const string CreateEventSessionSpeaker = "CreateEventSessionSpeaker";
    public const string UpdateEventSessionSpeaker = "UpdateEventSessionSpeaker";
    public const string DeleteEventSessionSpeaker = "DeleteEventSessionSpeaker";
    public const string GetEventSessionAgendaItems = "GetEventSessionAgendaItems";
    public const string GetManagedEventSessionAgendaItemsBySession = "GetManagedEventSessionAgendaItemsBySession";
    public const string GetEventSessionTemplateSyncDiff = "GetEventSessionTemplateSyncDiff";
    public const string ApplyEventSessionTemplateSync = "ApplyEventSessionTemplateSync";
    public const string GetEventSessionTemplateSyncHistory = "GetEventSessionTemplateSyncHistory";

    public const string GetEventSessionGroupsByEvent = "GetEventSessionGroupsByEvent";
    public const string GetEventSessionGroupById = "GetEventSessionGroupById";
    public const string GetManagedEventSessionGroupsByEvent = "GetManagedEventSessionGroupsByEvent";
    public const string GetManagedEventSessionGroupById = "GetManagedEventSessionGroupById";
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
    public const string GetActorByTenant = "GetActorByTenant";
    public const string SuspendActor = "SuspendActor";
    public const string ReinstateActor = "ReinstateActor";
    public const string SuspendAtprotoIdentity = "SuspendAtprotoIdentity";
    public const string ReinstateAtprotoIdentity = "ReinstateAtprotoIdentity";
    public const string GetActorSubscriptions = "GetActorSubscriptions";
    public const string GetActorSubscriptionByActor = "GetActorSubscriptionByActor";
    public const string SubscribeToActor = "SubscribeToActor";
    public const string UpdateActorSubscriptionNotificationLevel = "UpdateActorSubscriptionNotificationLevel";
    public const string UnsubscribeFromActor = "UnsubscribeFromActor";
    public const string GetActorTypes = "GetActorTypes";
    public const string GetActorTypeById = "GetActorTypeById";

    #endregion

    #region Location Routes

    public const string GetLocations = "GetLocations";
    public const string GetLocationById = "GetLocationById";
    public const string CreateLocation = "CreateLocation";
    public const string UpdateLocation = "UpdateLocation";
    public const string DeleteLocation = "DeleteLocation";
    public const string ClassifyLocationAsPrivateHome = "ClassifyLocationAsPrivateHome";
    public const string AcceptPrivateHomeOwnership = "AcceptPrivateHomeOwnership";
    public const string ApproveTenantAddress = "ApproveTenantAddress";

    #endregion

    #region Geocoding Routes

    public const string GetAddressSuggestions = "GetAddressSuggestions";

    #endregion

    #region Category Routes

    public const string GetCategories = "GetCategories";
    public const string GetCategoryById = "GetCategoryById";
    public const string CreateCategory = "CreateCategory";
    public const string UpdateCategory = "UpdateCategory";
    public const string DeleteCategory = "DeleteCategory";

    #endregion

    #region Tag Routes

    public const string GetTags = "GetTags";
    public const string GetTagById = "GetTagById";
    public const string CreateTag = "CreateTag";
    public const string UpdateTag = "UpdateTag";
    public const string DeleteTag = "DeleteTag";

    #endregion

    #region Registration Routes

    public const string StartGuestRegistrationOrder = "StartGuestRegistrationOrder";
    public const string GetGuestRegistrationOrder = "GetGuestRegistrationOrder";
    public const string GetRegistrationCheckoutComposition = "GetRegistrationCheckoutComposition";
    public const string ContinueGuestRegistrationOrder = "ContinueGuestRegistrationOrder";
    public const string FinalizeGuestRegistrationOrder = "FinalizeGuestRegistrationOrder";
    public const string CancelGuestRegistrationOrder = "CancelGuestRegistrationOrder";
    public const string ClaimGuestRegistrationOrder = "ClaimGuestRegistrationOrder";
    public const string StartAuthenticatedRegistrationOrder = "StartAuthenticatedRegistrationOrder";
    public const string GetCurrentRegistrationOrder = "GetCurrentRegistrationOrder";
    public const string ContinueAuthenticatedRegistrationOrder = "ContinueAuthenticatedRegistrationOrder";
    public const string FinalizeAuthenticatedRegistrationOrder = "FinalizeAuthenticatedRegistrationOrder";
    public const string CancelAuthenticatedRegistrationOrder = "CancelAuthenticatedRegistrationOrder";
    public const string GetEventRegistrationOrders = "GetEventRegistrationOrders";
    public const string GetGuestRegistrationOrderParticipants = "GetGuestRegistrationOrderParticipants";
    public const string AddGuestRegistrationOrderParticipant = "AddGuestRegistrationOrderParticipant";
    public const string UpdateGuestRegistrationOrderParticipant = "UpdateGuestRegistrationOrderParticipant";
    public const string AssignGuestRegistrationOrderTickets = "AssignGuestRegistrationOrderTickets";
    public const string DeferGuestRegistrationOrderTickets = "DeferGuestRegistrationOrderTickets";
    public const string GetAuthenticatedRegistrationOrderParticipants = "GetAuthenticatedRegistrationOrderParticipants";
    public const string AddAuthenticatedRegistrationOrderParticipant = "AddAuthenticatedRegistrationOrderParticipant";
    public const string UpdateAuthenticatedRegistrationOrderParticipant = "UpdateAuthenticatedRegistrationOrderParticipant";
    public const string AssignAuthenticatedRegistrationOrderTickets = "AssignAuthenticatedRegistrationOrderTickets";
    public const string ImportAuthenticatedRegistrationOrderCompanyAssignmentsCsv = "ImportAuthenticatedRegistrationOrderCompanyAssignmentsCsv";
    public const string DeferAuthenticatedRegistrationOrderTickets = "DeferAuthenticatedRegistrationOrderTickets";
    public const string ApplyGuestRegistrationOrderPromotion = "ApplyGuestRegistrationOrderPromotion";
    public const string RemoveGuestRegistrationOrderPromotion = "RemoveGuestRegistrationOrderPromotion";
    public const string ApplyAuthenticatedRegistrationOrderPromotion = "ApplyAuthenticatedRegistrationOrderPromotion";
    public const string RemoveAuthenticatedRegistrationOrderPromotion = "RemoveAuthenticatedRegistrationOrderPromotion";
    public const string StartGuestRegistrationPayment = "StartGuestRegistrationPayment";
    public const string GetGuestRegistrationPayment = "GetGuestRegistrationPayment";
    public const string GetGuestPaidOrderAcceptance = "GetGuestPaidOrderAcceptance";
    public const string RetryGuestRegistrationPayment = "RetryGuestRegistrationPayment";
    public const string GetGuestRegistrationPaymentCheckoutTarget = "GetGuestRegistrationPaymentCheckoutTarget";
    public const string StartAuthenticatedRegistrationPayment = "StartAuthenticatedRegistrationPayment";
    public const string GetAuthenticatedRegistrationPayment = "GetAuthenticatedRegistrationPayment";
    public const string GetAuthenticatedPaidOrderAcceptance = "GetAuthenticatedPaidOrderAcceptance";
    public const string RetryAuthenticatedRegistrationPayment = "RetryAuthenticatedRegistrationPayment";
    public const string GetAuthenticatedRegistrationPaymentCheckoutTarget = "GetAuthenticatedRegistrationPaymentCheckoutTarget";
    public const string GetStudioRegistrationPayment = "GetStudioRegistrationPayment";
    public const string RequestAuthenticatedRegistrationRefund = "RequestAuthenticatedRegistrationRefund";
    public const string RespondAuthenticatedRegistrationMaterialChange = "RespondAuthenticatedRegistrationMaterialChange";
    public const string CreateStudioRegistrationRefund = "CreateStudioRegistrationRefund";
    public const string RetryStudioRegistrationRefund = "RetryStudioRegistrationRefund";
    public const string GetRefundCampaigns = "GetRefundCampaigns";
    public const string GetRefundCampaign = "GetRefundCampaign";
    public const string ResumeRefundCampaign = "ResumeRefundCampaign";
    public const string GetStudioContext = "GetStudioContext";

    #endregion

    #region Lookup Table Routes

    public const string GetEventTypes = "GetEventTypes";
    public const string GetEventStatuses = "GetEventStatuses";
    public const string GetMadhabs = "GetMadhabs";
    public const string GetLanguages = "GetLanguages";
    public const string GetTagTypes = "GetTagTypes";
    public const string GetApprovalStatusOptions = "GetApprovalStatusOptions";
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
    public const string GetEventSessionStatuses = "GetEventSessionStatuses";
    public const string GetEventSessionStatusById = "GetEventSessionStatusById";
    public const string GetScheduleItemKinds = "GetScheduleItemKinds";
    public const string GetVisibilityTypes = "GetVisibilityTypes";
    public const string GetVisibilityTypeById = "GetVisibilityTypeById";
    public const string GetFileTypes = "GetFileTypes";
    public const string GetFileTypeById = "GetFileTypeById";
    public const string GetGroupPositions = "GetGroupPositions";
    public const string GetGroupPositionById = "GetGroupPositionById";
    public const string GetOrganizationPositions = "GetOrganizationPositions";
    public const string GetOrganizationPositionById = "GetOrganizationPositionById";
    public const string GetTagTypeById = "GetTagTypeById";
    public const string GetTagTypesWithTags = "GetTagTypesWithTags";
    public const string GetMyFeatureFlags = "GetMyFeatureFlags";
    public const string GetAiAssistantBootstrap = "GetAiAssistantBootstrap";
    public const string GetAiAssistantModels = "GetAiAssistantModels";
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
    public const string GetHomeDiscovery = "GetHomeDiscovery";
    public const string RelayAnalyticsEvent = "RelayAnalyticsEvent";
    public const string GetTranslationByLanguage = "GetTranslationByLanguage";
    public const string GetAvailableTranslationLanguages = "GetAvailableTranslationLanguages";
    public const string GetEventSeries = "GetEventSeries";
    public const string GetEventSeriesById = "GetEventSeriesById";
    public const string GetTopEventSeries = "GetTopEventSeries";
    public const string CreateEventSeries = "CreateEventSeries";
    public const string UpdateEventSeries = "UpdateEventSeries";
    public const string DeleteEventSeries = "DeleteEventSeries";
    public const string GetEventRegistrationPolicies = "GetEventRegistrationPolicies";

    #endregion

    #region User Routes

    public const string GetCurrentUser = "GetCurrentUser";
    public const string GetUserOrganizations = "GetUserOrganizations";
    public const string SyncUser = "SyncUser";
    public const string GetCurrentUserAdminAuthority = "GetCurrentUserAdminAuthority";
    public const string UpdateCurrentUser = "UpdateCurrentUser";
    public const string DeleteCurrentUser = "DeleteCurrentUser";
    public const string GetPrivacyErasureStatus = "GetPrivacyErasureStatus";
    public const string ResolveUserTenantRedirection = "ResolveUserTenantRedirection";
    public const string UpdateUserLastActiveTenant = "UpdateUserLastActiveTenant";

    #endregion

    #region UI Shell Routes

    public const string GetUiShellContext = "GetUiShellContext";

    #endregion

    #region Support Access Routes

    public const string GetCurrentSupportAccessSession = "GetCurrentSupportAccessSession";
    public const string ListSupportAccessSessions = "ListSupportAccessSessions";
    public const string StartSupportAccessSession = "StartSupportAccessSession";
    public const string StopSupportAccessSession = "StopSupportAccessSession";
    public const string ForceStopSupportAccessSession = "ForceStopSupportAccessSession";
    public const string GetSupportAccessAuditEvents = "GetSupportAccessAuditEvents";

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
    public const string ResolveEmailDispatchWithoutReplay = "ResolveEmailDispatchWithoutReplay";
    public const string GetEmailDispatchProcessorControl = "GetEmailDispatchProcessorControl";
    public const string PauseEmailDispatchProcessor = "PauseEmailDispatchProcessor";
    public const string ResumeEmailDispatchProcessor = "ResumeEmailDispatchProcessor";
    public const string SetEmailDispatchGlobalRateLimitOverride = "SetEmailDispatchGlobalRateLimitOverride";
    public const string ClearEmailDispatchGlobalRateLimitOverride = "ClearEmailDispatchGlobalRateLimitOverride";
    public const string ReconcileUnknownEmailDispatch = "ReconcileUnknownEmailDispatch";

    #endregion

    #region Webhook Routes

    public const string GetWebhookEventTypes = "GetWebhookEventTypes";
    public const string GetWebhookConsumers = "GetWebhookConsumers";
    public const string GetWebhookConsumerById = "GetWebhookConsumerById";
    public const string CreateWebhookConsumer = "CreateWebhookConsumer";
    public const string UpdateWebhookConsumerProviderMode = "UpdateWebhookConsumerProviderMode";
    public const string RepairWebhookProviderBinding = "RepairWebhookProviderBinding";
    public const string GetWebhookEndpoints = "GetWebhookEndpoints";
    public const string GetWebhookEndpointById = "GetWebhookEndpointById";
    public const string CreateWebhookEndpoint = "CreateWebhookEndpoint";
    public const string UpdateWebhookEndpoint = "UpdateWebhookEndpoint";
    public const string DeleteWebhookEndpoint = "DeleteWebhookEndpoint";
    public const string RotateWebhookEndpointSecret = "RotateWebhookEndpointSecret";
    public const string TestWebhookEndpoint = "TestWebhookEndpoint";
    public const string ResumeWebhookEndpoint = "ResumeWebhookEndpoint";
    public const string PauseWebhookEndpoint = "PauseWebhookEndpoint";
    public const string GetWebhookMessages = "GetWebhookMessages";
    public const string GetWebhookMessageById = "GetWebhookMessageById";
    public const string GetWebhookMessagePayload = "GetWebhookMessagePayload";
    public const string GetWebhookDeliveryAttempts = "GetWebhookDeliveryAttempts";
    public const string GetWebhookDeliveryAttemptById = "GetWebhookDeliveryAttemptById";
    public const string RetryWebhookDeliveryAttempt = "RetryWebhookDeliveryAttempt";
    public const string RedriveIncomingWebhook = "RedriveIncomingWebhook";
    public const string GetIncomingWebhookEffectStatus = "GetIncomingWebhookEffectStatus";
    public const string RedriveIncomingWebhookEffect = "RedriveIncomingWebhookEffect";
    public const string GetWebhookProviderPublications = "GetWebhookProviderPublications";
    public const string GetWebhookProviderPublicationById = "GetWebhookProviderPublicationById";
    public const string ReconcileWebhookProviderPublication = "ReconcileWebhookProviderPublication";
    public const string AbandonWebhookProviderPublication = "AbandonWebhookProviderPublication";
    public const string GetWebhookBulkReplays = "GetWebhookBulkReplays";
    public const string GetWebhookBulkReplayById = "GetWebhookBulkReplayById";
    public const string PreviewWebhookBulkReplay = "PreviewWebhookBulkReplay";
    public const string ScheduleWebhookBulkReplay = "ScheduleWebhookBulkReplay";
    public const string CancelWebhookBulkReplay = "CancelWebhookBulkReplay";
    public const string OpenSvixAppPortal = "OpenSvixAppPortal";

    #endregion

    #region Tenant Routes

    public const string GetTenants = "GetTenants";
    public const string GetTenantById = "GetTenantById";
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
    public const string PatchTenantBrandingSettingsDocument = "PatchTenantBrandingSettingsDocument";
    public const string GetTenantStorageSettings = "GetTenantStorageSettings";
    public const string PatchTenantStorageSettings = "PatchTenantStorageSettings";
    public const string TestTenantStorageConnection = "TestTenantStorageConnection";
    public const string GetTenantPaidEventPolicySettings = "GetTenantPaidEventPolicySettings";
    public const string UpdateTenantPaidEventPolicySettings = "UpdateTenantPaidEventPolicySettings";

    #endregion

    #region Role Routes

    public const string GetRoles = "GetRoles";
    public const string GetRoleById = "GetRoleById";

    #endregion

    #region Organization Member Routes

    public const string GetOrganizationMemberById = "GetOrganizationMemberById";
    public const string GetOrganizationMembersByOrganization = "GetOrganizationMembersByOrganization";
    public const string AddOrganizationMember = "AddOrganizationMember";
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
    public const string GetGroupNotificationPreferences = "GetGroupNotificationPreferences";
    public const string UpdateGroupNotificationPreferences = "UpdateGroupNotificationPreferences";
    public const string SetGroupNotificationPreferenceMute = "SetGroupNotificationPreferenceMute";
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
    public const string UpdateStorageObject = "UpdateStorageObject";
    public const string DeleteStorageObject = "DeleteStorageObject";
    public const string GetPublicStorageObjectImage = "GetPublicStorageObjectImage";
    public const string GetStorageObjectPresignedDownloadUrl = "GetStorageObjectPresignedDownloadUrl";
    public const string CreateStorageUploadSession = "CreateStorageUploadSession";
    public const string UploadStorageUploadSessionContent = "UploadStorageUploadSessionContent";
    public const string CancelStorageUploadSession = "CancelStorageUploadSession";

    #endregion

    #region Organization Review Routes

    public const string GetOrganizationReviews = "GetOrganizationReviews";
    public const string GetOrganizationReviewsByOrganization = "GetOrganizationReviewsByOrganization";
    public const string GetOrganizationReviewsByUser = "GetOrganizationReviewsByUser";
    public const string CreateOrganizationReview = "CreateOrganizationReview";

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
    public const string GetCurrentUserNotificationPreferences = "GetCurrentUserNotificationPreferences";
    public const string UpdateCurrentUserNotificationPreferences = "UpdateCurrentUserNotificationPreferences";
    public const string SetCurrentUserNotificationPreferenceMute = "SetCurrentUserNotificationPreferenceMute";
    public const string GetWebPushConfiguration = "GetWebPushConfiguration";
    public const string GetVapidPublicKey = "GetVapidPublicKey";
    public const string GetCurrentUserWebPushSubscription = "GetCurrentUserWebPushSubscription";
    public const string SubscribeCurrentUserWebPushSubscription = "SubscribeCurrentUserWebPushSubscription";
    public const string UnsubscribeCurrentUserWebPushSubscription = "UnsubscribeCurrentUserWebPushSubscription";

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

    public const string BootstrapAtprotoSession = "BootstrapAtprotoSession";
    public const string GetCurrentAtprotoSession = "GetCurrentAtprotoSession";
    public const string RefreshCurrentAtprotoSession = "RefreshCurrentAtprotoSession";
    public const string DeleteCurrentAtprotoSession = "DeleteCurrentAtprotoSession";
    public const string GetAtprotoEventSource = "GetAtprotoEventSource";

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
    public const string ExportLocalizationBundle = "ExportLocalizationBundle";
    public const string ImportLocalizationBundle = "ImportLocalizationBundle";
    public const string ExportLocalizationFromTms = "ExportLocalizationFromTms";
    public const string UpdateLocalizationGovernance = "UpdateLocalizationGovernance";
    public const string RotateLocalizationTmsApiKey = "RotateLocalizationTmsApiKey";
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
    public const string ResetTenantSetting = "ResetTenantSetting";
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
    public const string GetTenantFooterSettings = "GetTenantFooterSettings";
    public const string PatchTenantFooterSettings = "PatchTenantFooterSettings";

    #endregion

    #region Event Aspect Routes

    public const string GetEventIslamicAspect = "GetEventIslamicAspect";
    public const string GetManagedEventIslamicAspect = "GetManagedEventIslamicAspect";
    public const string CreateEventIslamicAspect = "CreateEventIslamicAspect";
    public const string UpdateEventIslamicAspect = "UpdateEventIslamicAspect";
    public const string DeleteEventIslamicAspect = "DeleteEventIslamicAspect";
    public const string GetEventTechAspect = "GetEventTechAspect";
    public const string GetManagedEventTechAspect = "GetManagedEventTechAspect";
    public const string CreateEventTechAspect = "CreateEventTechAspect";
    public const string UpdateEventTechAspect = "UpdateEventTechAspect";
    public const string DeleteEventTechAspect = "DeleteEventTechAspect";

    #endregion

    #region Event Team Routes

    public const string GetEventTeam = "GetEventTeam";
    public const string GetEventTeamAssignablePresets = "GetEventTeamAssignablePresets";
    public const string GetCurrentUserEventPermissions = "GetCurrentUserEventPermissions";
    public const string AssignEventRole = "AssignEventRole";
    public const string RevokeEventRole = "RevokeEventRole";

    #endregion

    #region Event Day Routes

    public const string GetEventDaysByEvent = "GetEventDaysByEvent";
    public const string GetManagedEventDaysByEvent = "GetManagedEventDaysByEvent";
    public const string GetEventDayById = "GetEventDayById";
    public const string CreateEventDay = "CreateEventDay";
    public const string UpdateEventDay = "UpdateEventDay";
    public const string DeleteEventDay = "DeleteEventDay";

    #endregion

    #region Event Agenda Item Routes

    public const string GetEventAgendaItemsByEvent = "GetEventAgendaItemsByEvent";
    public const string GetEventAgendaItemById = "GetEventAgendaItemById";
    public const string GetManagedEventAgendaItemsByEvent = "GetManagedEventAgendaItemsByEvent";
    public const string GetManagedEventAgendaItemById = "GetManagedEventAgendaItemById";
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

    #region User Authentication Token Routes

    public const string GetUserAuthenticationTokens = "GetUserAuthenticationTokens";
    public const string GetUserAuthenticationTokenById = "GetUserAuthenticationTokenById";
    public const string DeleteUserAuthenticationToken = "DeleteUserAuthenticationToken";

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

    public const string GetInstancePlatformMonetizationSettings = "GetInstancePlatformMonetizationSettings";
    public const string UpdateInstancePlatformMonetizationSettings = "UpdateInstancePlatformMonetizationSettings";
    public const string GetInstancePaidEventPolicySettings = "GetInstancePaidEventPolicySettings";
    public const string UpdateInstancePaidEventPolicySettings = "UpdateInstancePaidEventPolicySettings";

    public const string GetSchedulerAdminOverview = "GetSchedulerAdminOverview";
    public const string GetSchedulerAdminJobs = "GetSchedulerAdminJobs";
    public const string PauseScheduler = "PauseScheduler";
    public const string ResumeScheduler = "ResumeScheduler";
    public const string PauseSchedulerJob = "PauseSchedulerJob";
    public const string ResumeSchedulerJob = "ResumeSchedulerJob";
    public const string TriggerSchedulerJob = "TriggerSchedulerJob";
    public const string ResetSchedulerJobErrorState = "ResetSchedulerJobErrorState";
    public const string InterruptSchedulerJob = "InterruptSchedulerJob";

    public const string GetControlPlaneOverview = "GetControlPlaneOverview";
    public const string GetControlPlaneDomains = "GetControlPlaneDomains";
    public const string GetControlPlaneOperations = "GetControlPlaneOperations";
    public const string GetControlPlaneDeploymentModeRunbook = "GetControlPlaneDeploymentModeRunbook";
    public const string TransitionControlPlaneDeploymentMode = "TransitionControlPlaneDeploymentMode";
    public const string GetControlPlaneTenants = "GetControlPlaneTenants";
    public const string GetControlPlaneTenantById = "GetControlPlaneTenantById";
    public const string CreateControlPlaneTenant = "CreateControlPlaneTenant";
    public const string ActivateControlPlaneTenant = "ActivateControlPlaneTenant";
    public const string SuspendControlPlaneTenant = "SuspendControlPlaneTenant";
    public const string ArchiveControlPlaneTenant = "ArchiveControlPlaneTenant";
    public const string ReactivateControlPlaneTenant = "ReactivateControlPlaneTenant";
    public const string ScheduleControlPlaneTenantPurge = "ScheduleControlPlaneTenantPurge";
    public const string GetControlPlaneTenantPlans = "GetControlPlaneTenantPlans";
    public const string GetControlPlaneTenantPlanByKey = "GetControlPlaneTenantPlanByKey";
    public const string CreateControlPlaneTenantPlanDraft = "CreateControlPlaneTenantPlanDraft";
    public const string CreateControlPlaneTenantPlanVersionDraft = "CreateControlPlaneTenantPlanVersionDraft";
    public const string UpdateControlPlaneTenantPlanVersionDraft = "UpdateControlPlaneTenantPlanVersionDraft";
    public const string PublishControlPlaneTenantPlanVersion = "PublishControlPlaneTenantPlanVersion";
    public const string ArchiveControlPlaneTenantPlanVersion = "ArchiveControlPlaneTenantPlanVersion";
    public const string CloneControlPlaneTenantPlan = "CloneControlPlaneTenantPlan";
    public const string ValidateControlPlaneTenantPlanDraft = "ValidateControlPlaneTenantPlanDraft";
    public const string PreviewControlPlaneTenantPlanDiff = "PreviewControlPlaneTenantPlanDiff";
    public const string GetControlPlaneTenantEffectiveConfiguration = "GetControlPlaneTenantEffectiveConfiguration";
    public const string LockControlPlaneTenantSetting = "LockControlPlaneTenantSetting";
    public const string UnlockControlPlaneTenantSetting = "UnlockControlPlaneTenantSetting";
    public const string SetControlPlaneTenantSetting = "SetControlPlaneTenantSetting";
    public const string GetControlPlaneTenantPlanAssignment = "GetControlPlaneTenantPlanAssignment";
    public const string SwitchControlPlaneTenantPlanAssignment = "SwitchControlPlaneTenantPlanAssignment";
    public const string ApplyControlPlaneTenantPlanAssignment = "ApplyControlPlaneTenantPlanAssignment";
    public const string RollbackControlPlaneTenantPlanAssignment = "RollbackControlPlaneTenantPlanAssignment";
    public const string GetInstanceModuleSettings = "GetInstanceModuleSettings";
    public const string GetInstanceAtprotoFederationSettings = "GetInstanceAtprotoFederationSettings";
    public const string UpdateInstanceAtprotoFederationSetting = "UpdateInstanceAtprotoFederationSetting";
    public const string ResetInstanceAtprotoFederationSetting = "ResetInstanceAtprotoFederationSetting";
    public const string LockInstanceAtprotoFederationSetting = "LockInstanceAtprotoFederationSetting";
    public const string UnlockInstanceAtprotoFederationSetting = "UnlockInstanceAtprotoFederationSetting";
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
    public const string GetInstanceAdminPortalSettings = "GetInstanceAdminPortalSettings";
    public const string UpdateInstanceAdminPortalSettings = "UpdateInstanceAdminPortalSettings";
    public const string UpdateInstanceModerationReportingProviderLocks = "UpdateInstanceModerationReportingProviderLocks";
    public const string GetInstanceAiAssistantGovernanceSettings = "GetInstanceAiAssistantGovernanceSettings";
    public const string UpdateInstanceAiAssistantGovernanceSettings = "UpdateInstanceAiAssistantGovernanceSettings";
    public const string GetInstanceMcpGovernanceSettings = "GetInstanceMcpGovernanceSettings";
    public const string UpdateInstanceMcpGovernanceSettings = "UpdateInstanceMcpGovernanceSettings";
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
    public const string GetInstanceAuthorizationPolicyPackageStatus = "GetInstanceAuthorizationPolicyPackageStatus";

    #endregion

    #region System Routes

    public const string GetSystemOnboardingStatus = "GetSystemOnboardingStatus";
    public const string GetSystemOnboardingPreflight = "GetSystemOnboardingPreflight";

    #endregion

    #region Instance Onboarding Routes

    public const string GetInstanceOnboardingStatus = "GetInstanceOnboardingStatus";
    public const string SaveInstanceOnboardingProfile = "SaveInstanceOnboardingProfile";
    public const string CompleteInstanceOnboarding = "CompleteInstanceOnboarding";
    public const string ValidateInstanceSetupSecret = "ValidateInstanceSetupSecret";
    public const string GetInstanceOnboardingAuthProviderConfiguration = "GetInstanceOnboardingAuthProviderConfiguration";
    public const string GetInstanceOnboardingAuthProviderConfigurationInternal = "GetInstanceOnboardingAuthProviderConfigurationInternal";
    public const string BootstrapInstanceOnboardingKeycloakRealm = "BootstrapInstanceOnboardingKeycloakRealm";
    public const string GetInstanceOnboardingAuthorizationProviderConfigurationInternal = "GetInstanceOnboardingAuthorizationProviderConfigurationInternal";
    public const string VerifyInstanceOnboardingAuthorizationProviderEndpoint = "VerifyInstanceOnboardingAuthorizationProviderEndpoint";
    public const string SyncInstanceOnboardingAuthorizationPolicyPackage = "SyncInstanceOnboardingAuthorizationPolicyPackage";
    public const string DownloadInstanceOnboardingAuthorizationPolicyPackage = "DownloadInstanceOnboardingAuthorizationPolicyPackage";

    #endregion

    #region Tenant Onboarding Routes

    public const string GetTenantOnboardingStatus = "GetTenantOnboardingStatus";
    public const string GetTenantOnboardingPolicySettings = "GetTenantOnboardingPolicySettings";
    public const string CompleteTenantOnboarding = "CompleteTenantOnboarding";
    public const string SaveTenantOnboardingStepProgress = "SaveTenantOnboardingStepProgress";

    #endregion
}
