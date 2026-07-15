// ABOUTME: Explicit API-owned catalog of DTOs whose HAL OpenAPI schemas are public contract surface.
// ABOUTME: Keeps HAL schema opt-in out of Application DTOs and avoids broad namespace reflection.

namespace Explore.API.OpenApi;

internal static class HalOpenApiSchemaCatalog
{
    public static IReadOnlyList<Type> RegisteredDtoTypes { get; } =
    [
        // Event DTOs
        typeof(Explore.Application.DTOs.Event.EventDto),
        typeof(Explore.Application.DTOs.Event.EventListDto),
        typeof(Explore.Application.DTOs.EventReporting.EventReportOptionsDto),
        typeof(Explore.Application.DTOs.EventReporting.MyEventReportDto),
        typeof(Explore.Application.DTOs.EventReporting.ModerationReportDetailDto),
        typeof(Explore.Application.DTOs.EventReporting.ModerationReportQueueItemDto),
        typeof(Explore.Application.DTOs.EventReporting.ReportingRoutingStateDto),
        typeof(Explore.Application.DTOs.EventReporting.ReportingProviderStateDto),
        typeof(Explore.Application.DTOs.EventReporting.ReportingProviderTargetDto),
        typeof(Explore.Application.DTOs.EventReporting.TenantModerationReportingDashboardDto),
        typeof(Explore.Application.DTOs.EventReporting.TenantModerationReportQueueHealthDto),
        typeof(Explore.Application.DTOs.EventReporting.TenantModerationProviderSyncHealthDto),

        // EventSession DTOs
        typeof(Explore.Application.DTOs.EventSession.EventSessionDto),
        typeof(Explore.Application.DTOs.EventSession.EventSessionListDto),

        // EventSessionGroup DTOs
        typeof(Explore.Application.DTOs.EventSessionGroup.EventSessionGroupDto),
        typeof(Explore.Application.DTOs.EventSessionGroup.EventSessionGroupListDto),

        // Category DTOs
        typeof(Explore.Application.DTOs.Category.CategoryDto),
        typeof(Explore.Application.DTOs.Category.CategoryListDto),

        // Tag DTOs
        typeof(Explore.Application.DTOs.Tag.TagDto),
        typeof(Explore.Application.DTOs.Tag.TagListDto),

        // Location DTOs
        typeof(Explore.Application.DTOs.Location.LocationDto),
        typeof(Explore.Application.DTOs.Location.LocationListDto),

        // Organization DTOs
        typeof(Explore.Application.DTOs.Organization.OrganizationDto),
        typeof(Explore.Application.DTOs.Organization.OrganizationListDto),

        // Actor DTOs
        typeof(Explore.Application.DTOs.Actor.ActorDto),
        typeof(Explore.Application.DTOs.Actor.ActorListDto),
        typeof(Explore.Application.DTOs.ActorSubscription.ActorSubscriptionDto),
        typeof(Explore.Application.DTOs.ActorSubscription.ActorSubscriptionListDto),

        // Public metadata DTOs
        typeof(Explore.Application.DTOs.CustomPropertyDefinition.CustomPropertyDefinitionDto),
        typeof(Explore.Application.DTOs.CustomPropertyDefinition.CustomPropertyDefinitionListDto),
        typeof(Explore.Application.DTOs.EventCustomProperty.EventCustomPropertyDefinitionDto),
        typeof(Explore.Application.DTOs.EventCustomProperty.EventCustomPropertyDefinitionListDto),
        typeof(Explore.Application.DTOs.EventSessionCustomProperty.EventSessionCustomPropertyDefinitionDto),
        typeof(Explore.Application.DTOs.EventSessionCustomProperty.EventSessionCustomPropertyDefinitionListDto),
        typeof(Explore.Application.DTOs.EventTemplate.EventTemplateDto),
        typeof(Explore.Application.DTOs.EventTemplate.EventTemplateListDto),
        typeof(Explore.Application.DTOs.EventSessionTemplate.EventSessionTemplateDto),
        typeof(Explore.Application.DTOs.EventSessionTemplate.EventSessionTemplateListDto),
        typeof(Explore.Application.DTOs.Group.GroupDto),
        typeof(Explore.Application.DTOs.Group.GroupListDto),
        typeof(Explore.Application.DTOs.GroupMember.GroupMemberDto),
        typeof(Explore.Application.DTOs.OrganizationMember.OrganizationMemberDto),
        typeof(Explore.Application.DTOs.IndexedDid.IndexedDidDto),
        typeof(Explore.Application.DTOs.IndexedDid.IndexedDidListDto),
        typeof(Explore.Application.DTOs.LocationRoom.LocationRoomDto),
        typeof(Explore.Application.DTOs.LocationRoom.LocationRoomListDto),
        typeof(Explore.Application.DTOs.EventDay.EventDayDto),
        typeof(Explore.Application.DTOs.EventDay.EventDayListDto),
        typeof(Explore.Application.DTOs.EventAgendaItem.EventAgendaItemDto),
        typeof(Explore.Application.DTOs.EventAgendaItem.EventAgendaItemListDto),
        typeof(Explore.Application.DTOs.OrganizationReview.OrganizationReviewDto),
        typeof(Explore.Application.DTOs.TenantUserRoleGrant.TenantUserRoleGrantDto),
        typeof(Explore.Application.DTOs.TenantUserRoleGrant.TenantUserRoleGrantListDto),
        typeof(Explore.Application.DTOs.Webhooks.WebhookConsumerDto),
        typeof(Explore.Application.DTOs.Webhooks.WebhookEndpointDto),
        typeof(Explore.Application.DTOs.Webhooks.WebhookMessageDto),
        typeof(Explore.Application.DTOs.Webhooks.WebhookDeliveryAttemptDto),
        typeof(Explore.Application.DTOs.Webhooks.WebhookProviderPublicationDto),
        typeof(Explore.Application.DTOs.Webhooks.WebhookProviderPublicationAttemptDto),
        typeof(Explore.Application.DTOs.Webhooks.WebhookBulkReplayOperationDto),
        typeof(Explore.Application.DTOs.Webhooks.WebhookBulkReplayFilterDto),

        // EventSessionSpeaker DTOs
        typeof(Explore.Application.DTOs.EventSessionSpeaker.EventSessionSpeakerDto),
        typeof(Explore.Application.DTOs.EventSessionSpeaker.EventSessionSpeakerListDto),

        // EventSessionLanguage DTOs
        typeof(Explore.Application.DTOs.EventSessionLanguage.EventSessionLanguageDto),
        typeof(Explore.Application.DTOs.EventSessionLanguage.EventSessionLanguageListDto),

        // EventAspects DTOs
        typeof(Explore.Application.DTOs.EventAspects.EventIslamicAspectDto),
        typeof(Explore.Application.DTOs.EventSession.EventSessionIslamicAspectDto),
        typeof(Explore.Application.DTOs.EventAspects.EventTechAspectDto),

        // Template sync DTOs exposed through HAL diff resources
        typeof(Explore.Application.DTOs.EventTemplateSync.TemplateDiffDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.AddedDefinitionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.ModifiedDefinitionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.RetiredDefinitionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.AddedOptionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.ModifiedOptionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.RetiredOptionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.UntouchedLocalDefinitionDto),
        typeof(Explore.Application.DTOs.EventTemplateSync.FieldChangeDto),

        // Email dispatch admin DTOs
        typeof(Explore.Application.DTOs.EmailDispatch.EmailDispatchStatusDto),

        typeof(Explore.Application.DTOs.SupportAccess.SupportAccessSessionDto),
        typeof(Explore.Application.DTOs.SupportAccess.SupportAccessAuditEventDto),

        // Projection admin DTOs
        typeof(Explore.Application.DTOs.CustomPropertyProjection.ProjectionStatusDto),
        typeof(Explore.Application.DTOs.CustomPropertyProjection.ProjectionDirtyScopeDto),

        // Tenant settings DTOs
        typeof(Explore.Application.DTOs.TenantSettingsDocuments.TenantBrandingSettingsDocumentDto),

        // AI assistant DTOs
        typeof(Explore.Application.DTOs.Ai.AiAssistantBootstrapDto),
        typeof(Explore.Application.DTOs.Ai.AiConversationSummaryDto),
        typeof(Explore.Application.DTOs.Ai.AiConversationDto),
        typeof(Explore.Application.DTOs.Ai.AiRunDto),

        // Storage DTOs
        typeof(Explore.Application.DTOs.StorageObject.StorageObjectDto),
        typeof(Explore.Application.DTOs.StorageObject.StorageObjectListDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneOverviewDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneDomainOverviewDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneDnsRecordDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneOperationsDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneOperationStatusDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneOperationMetricDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneDeploymentModeRunbookDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneDeploymentModeTargetOptionDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneDeploymentModeRunbookStepDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantDetailDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantListItemDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantPlanDetailDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantPlanListItemDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantPlanVersionDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantPlanSettingDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantPlanQuotaDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantPlanAssignmentDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantEffectiveConfigurationDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantEffectiveSettingDto),
        typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantQuotaUsageDto),
        typeof(Explore.Application.DTOs.Onboarding.InstanceOnboardingStatusDto),
        typeof(Explore.Application.DTOs.Onboarding.TenantOnboardingStatusDto),
        typeof(Explore.Application.DTOs.Onboarding.InstanceStorageSettingsDto),
        typeof(Explore.Application.DTOs.Notification.NotificationPreferenceMatrixDto),
        typeof(Explore.Application.DTOs.Notification.WebPushSubscriptionDto),
        typeof(Explore.Application.DTOs.Tenant.TenantStorageSettingsDto),
    ];

    public static IReadOnlyDictionary<string, Type> DetailResourceMappings { get; } = new Dictionary<string, Type>
    {
        ["HalResourceOfEventDto"] = typeof(Explore.Application.DTOs.Event.EventDto),
        ["HalResourceOfEventListDto"] = typeof(Explore.Application.DTOs.Event.EventListDto),
        ["HalResourceOfEventReportOptionsDto"] = typeof(Explore.Application.DTOs.EventReporting.EventReportOptionsDto),
        ["HalResourceOfMyEventReportDto"] = typeof(Explore.Application.DTOs.EventReporting.MyEventReportDto),
        ["HalResourceOfModerationReportDetailDto"] = typeof(Explore.Application.DTOs.EventReporting.ModerationReportDetailDto),
        ["HalResourceOfModerationReportQueueItemDto"] = typeof(Explore.Application.DTOs.EventReporting.ModerationReportQueueItemDto),
        ["HalResourceOfReportingRoutingStateDto"] = typeof(Explore.Application.DTOs.EventReporting.ReportingRoutingStateDto),
        ["HalResourceOfTenantModerationReportingDashboardDto"] = typeof(Explore.Application.DTOs.EventReporting.TenantModerationReportingDashboardDto),
        ["HalResourceOfEventSessionDto"] = typeof(Explore.Application.DTOs.EventSession.EventSessionDto),
        ["HalResourceOfEventSessionListDto"] = typeof(Explore.Application.DTOs.EventSession.EventSessionListDto),
        ["HalResourceOfCategoryDto"] = typeof(Explore.Application.DTOs.Category.CategoryDto),
        ["HalResourceOfCategoryListDto"] = typeof(Explore.Application.DTOs.Category.CategoryListDto),
        ["HalResourceOfTagDto"] = typeof(Explore.Application.DTOs.Tag.TagDto),
        ["HalResourceOfTagListDto"] = typeof(Explore.Application.DTOs.Tag.TagListDto),
        ["HalResourceOfLocationDto"] = typeof(Explore.Application.DTOs.Location.LocationDto),
        ["HalResourceOfLocationListDto"] = typeof(Explore.Application.DTOs.Location.LocationListDto),
        ["HalResourceOfOrganizationDto"] = typeof(Explore.Application.DTOs.Organization.OrganizationDto),
        ["HalResourceOfOrganizationListDto"] = typeof(Explore.Application.DTOs.Organization.OrganizationListDto),
        ["HalResourceOfActorDto"] = typeof(Explore.Application.DTOs.Actor.ActorDto),
        ["HalResourceOfActorListDto"] = typeof(Explore.Application.DTOs.Actor.ActorListDto),
        ["HalResourceOfActorSubscriptionDto"] = typeof(Explore.Application.DTOs.ActorSubscription.ActorSubscriptionDto),
        ["HalResourceOfActorSubscriptionListDto"] = typeof(Explore.Application.DTOs.ActorSubscription.ActorSubscriptionListDto),
        ["HalResourceOfEventSessionGroupDto"] = typeof(Explore.Application.DTOs.EventSessionGroup.EventSessionGroupDto),
        ["HalResourceOfEventSessionGroupListDto"] = typeof(Explore.Application.DTOs.EventSessionGroup.EventSessionGroupListDto),
        ["HalResourceOfCustomPropertyDefinitionDto"] = typeof(Explore.Application.DTOs.CustomPropertyDefinition.CustomPropertyDefinitionDto),
        ["HalResourceOfCustomPropertyDefinitionListDto"] = typeof(Explore.Application.DTOs.CustomPropertyDefinition.CustomPropertyDefinitionListDto),
        ["HalResourceOfEventCustomPropertyDefinitionDto"] = typeof(Explore.Application.DTOs.EventCustomProperty.EventCustomPropertyDefinitionDto),
        ["HalResourceOfEventCustomPropertyDefinitionListDto"] = typeof(Explore.Application.DTOs.EventCustomProperty.EventCustomPropertyDefinitionListDto),
        ["HalResourceOfEventSessionCustomPropertyDefinitionDto"] = typeof(Explore.Application.DTOs.EventSessionCustomProperty.EventSessionCustomPropertyDefinitionDto),
        ["HalResourceOfEventSessionCustomPropertyDefinitionListDto"] = typeof(Explore.Application.DTOs.EventSessionCustomProperty.EventSessionCustomPropertyDefinitionListDto),
        ["HalResourceOfEventTemplateDto"] = typeof(Explore.Application.DTOs.EventTemplate.EventTemplateDto),
        ["HalResourceOfEventTemplateListDto"] = typeof(Explore.Application.DTOs.EventTemplate.EventTemplateListDto),
        ["HalResourceOfEventSessionTemplateDto"] = typeof(Explore.Application.DTOs.EventSessionTemplate.EventSessionTemplateDto),
        ["HalResourceOfEventSessionTemplateListDto"] = typeof(Explore.Application.DTOs.EventSessionTemplate.EventSessionTemplateListDto),
        ["HalResourceOfGroupDto"] = typeof(Explore.Application.DTOs.Group.GroupDto),
        ["HalResourceOfGroupListDto"] = typeof(Explore.Application.DTOs.Group.GroupListDto),
        ["HalResourceOfGroupMemberDto"] = typeof(Explore.Application.DTOs.GroupMember.GroupMemberDto),
        ["HalResourceOfOrganizationMemberDto"] = typeof(Explore.Application.DTOs.OrganizationMember.OrganizationMemberDto),
        ["HalResourceOfIndexedDidDto"] = typeof(Explore.Application.DTOs.IndexedDid.IndexedDidDto),
        ["HalResourceOfIndexedDidListDto"] = typeof(Explore.Application.DTOs.IndexedDid.IndexedDidListDto),
        ["HalResourceOfLocationRoomDto"] = typeof(Explore.Application.DTOs.LocationRoom.LocationRoomDto),
        ["HalResourceOfLocationRoomListDto"] = typeof(Explore.Application.DTOs.LocationRoom.LocationRoomListDto),
        ["HalResourceOfEventDayDto"] = typeof(Explore.Application.DTOs.EventDay.EventDayDto),
        ["HalResourceOfEventDayListDto"] = typeof(Explore.Application.DTOs.EventDay.EventDayListDto),
        ["HalResourceOfEventAgendaItemDto"] = typeof(Explore.Application.DTOs.EventAgendaItem.EventAgendaItemDto),
        ["HalResourceOfEventAgendaItemListDto"] = typeof(Explore.Application.DTOs.EventAgendaItem.EventAgendaItemListDto),
        ["HalResourceOfOrganizationReviewDto"] = typeof(Explore.Application.DTOs.OrganizationReview.OrganizationReviewDto),
        ["HalResourceOfTenantUserRoleGrantDto"] = typeof(Explore.Application.DTOs.TenantUserRoleGrant.TenantUserRoleGrantDto),
        ["HalResourceOfTenantUserRoleGrantListDto"] = typeof(Explore.Application.DTOs.TenantUserRoleGrant.TenantUserRoleGrantListDto),
        ["HalResourceOfWebhookConsumerDto"] = typeof(Explore.Application.DTOs.Webhooks.WebhookConsumerDto),
        ["HalResourceOfWebhookEndpointDto"] = typeof(Explore.Application.DTOs.Webhooks.WebhookEndpointDto),
        ["HalResourceOfWebhookMessageDto"] = typeof(Explore.Application.DTOs.Webhooks.WebhookMessageDto),
        ["HalResourceOfWebhookDeliveryAttemptDto"] = typeof(Explore.Application.DTOs.Webhooks.WebhookDeliveryAttemptDto),
        ["HalResourceOfWebhookProviderPublicationDto"] = typeof(Explore.Application.DTOs.Webhooks.WebhookProviderPublicationDto),
        ["HalResourceOfWebhookBulkReplayOperationDto"] = typeof(Explore.Application.DTOs.Webhooks.WebhookBulkReplayOperationDto),
        ["HalResourceOfTemplateDiffDto"] = typeof(Explore.Application.DTOs.EventTemplateSync.TemplateDiffDto),
        ["HalResourceOfEmailDispatchStatusDto"] = typeof(Explore.Application.DTOs.EmailDispatch.EmailDispatchStatusDto),
        ["HalResourceOfSupportAccessSessionDto"] = typeof(Explore.Application.DTOs.SupportAccess.SupportAccessSessionDto),
        ["HalResourceOfSupportAccessAuditEventDto"] = typeof(Explore.Application.DTOs.SupportAccess.SupportAccessAuditEventDto),
        ["HalResourceOfProjectionStatusDto"] = typeof(Explore.Application.DTOs.CustomPropertyProjection.ProjectionStatusDto),
        ["HalResourceOfProjectionDirtyScopeDto"] = typeof(Explore.Application.DTOs.CustomPropertyProjection.ProjectionDirtyScopeDto),
        ["HalResourceOfTenantBrandingSettingsDocumentDto"] = typeof(Explore.Application.DTOs.TenantSettingsDocuments.TenantBrandingSettingsDocumentDto),
        ["HalResourceOfNotificationPreferenceMatrixDto"] = typeof(Explore.Application.DTOs.Notification.NotificationPreferenceMatrixDto),
        ["HalResourceOfWebPushSubscriptionDto"] = typeof(Explore.Application.DTOs.Notification.WebPushSubscriptionDto),
        ["HalResourceOfAiAssistantBootstrapDto"] = typeof(Explore.Application.DTOs.Ai.AiAssistantBootstrapDto),
        ["HalResourceOfAiConversationSummaryDto"] = typeof(Explore.Application.DTOs.Ai.AiConversationSummaryDto),
        ["HalResourceOfAiConversationDto"] = typeof(Explore.Application.DTOs.Ai.AiConversationDto),
        ["HalResourceOfAiReferenceSearchResultDto"] = typeof(Explore.Application.DTOs.Ai.AiReferenceSearchResultDto),
        ["HalResourceOfAiRunDto"] = typeof(Explore.Application.DTOs.Ai.AiRunDto),
        ["HalResourceOfStorageObjectDto"] = typeof(Explore.Application.DTOs.StorageObject.StorageObjectDto),
        ["HalResourceOfStorageObjectListDto"] = typeof(Explore.Application.DTOs.StorageObject.StorageObjectListDto),
        ["HalResourceOfControlPlaneOverviewDto"] = typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneOverviewDto),
        ["HalResourceOfControlPlaneDomainOverviewDto"] = typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneDomainOverviewDto),
        ["HalResourceOfControlPlaneOperationsDto"] = typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneOperationsDto),
        ["HalResourceOfControlPlaneDeploymentModeRunbookDto"] = typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneDeploymentModeRunbookDto),
        ["HalResourceOfControlPlaneTenantDetailDto"] = typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantDetailDto),
        ["HalResourceOfControlPlaneTenantListItemDto"] = typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantListItemDto),
        ["HalResourceOfControlPlaneTenantPlanDetailDto"] = typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantPlanDetailDto),
        ["HalResourceOfControlPlaneTenantPlanListItemDto"] = typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantPlanListItemDto),
        ["HalResourceOfControlPlaneTenantEffectiveConfigurationDto"] = typeof(Explore.Application.DTOs.ControlPlane.ControlPlaneTenantEffectiveConfigurationDto),
        ["HalResourceOfInstanceOnboardingStatusDto"] = typeof(Explore.Application.DTOs.Onboarding.InstanceOnboardingStatusDto),
        ["HalResourceOfTenantOnboardingStatusDto"] = typeof(Explore.Application.DTOs.Onboarding.TenantOnboardingStatusDto),
        ["HalResourceOfInstanceStorageSettingsDto"] = typeof(Explore.Application.DTOs.Onboarding.InstanceStorageSettingsDto),
        ["HalResourceOfTenantStorageSettingsDto"] = typeof(Explore.Application.DTOs.Tenant.TenantStorageSettingsDto),
        ["HalResourceOfEventSessionSpeakerDto"] = typeof(Explore.Application.DTOs.EventSessionSpeaker.EventSessionSpeakerDto),
        ["HalResourceOfEventSessionSpeakerListDto"] = typeof(Explore.Application.DTOs.EventSessionSpeaker.EventSessionSpeakerListDto),
    };

    public static IReadOnlyDictionary<string, string> CollectionEmbeddedItemResourceMappings { get; } = new Dictionary<string, string>
    {
        ["HalCollectionEmbeddedOfActorListDto"] = "HalResourceOfActorListDto",
        ["HalCollectionEmbeddedOfActorSubscriptionListDto"] = "HalResourceOfActorSubscriptionListDto",
        ["HalCollectionEmbeddedOfCategoryListDto"] = "HalResourceOfCategoryListDto",
        ["HalCollectionEmbeddedOfCustomPropertyDefinitionListDto"] = "HalResourceOfCustomPropertyDefinitionListDto",
        ["HalCollectionEmbeddedOfEventAgendaItemListDto"] = "HalResourceOfEventAgendaItemListDto",
        ["HalCollectionEmbeddedOfEventCustomPropertyDefinitionListDto"] = "HalResourceOfEventCustomPropertyDefinitionListDto",
        ["HalCollectionEmbeddedOfEventDayListDto"] = "HalResourceOfEventDayListDto",
        ["HalCollectionEmbeddedOfEventListDto"] = "HalResourceOfEventListDto",
        ["HalCollectionEmbeddedOfEventReportOptionsDto"] = "HalResourceOfEventReportOptionsDto",
        ["HalCollectionEmbeddedOfMyEventReportDto"] = "HalResourceOfMyEventReportDto",
        ["HalCollectionEmbeddedOfModerationReportQueueItemDto"] = "HalResourceOfModerationReportQueueItemDto",
        ["HalCollectionEmbeddedOfEventSessionCustomPropertyDefinitionListDto"] = "HalResourceOfEventSessionCustomPropertyDefinitionListDto",
        ["HalCollectionEmbeddedOfEventSessionGroupListDto"] = "HalResourceOfEventSessionGroupListDto",
        ["HalCollectionEmbeddedOfEventSessionListDto"] = "HalResourceOfEventSessionListDto",
        ["HalCollectionEmbeddedOfEventSessionTemplateListDto"] = "HalResourceOfEventSessionTemplateListDto",
        ["HalCollectionEmbeddedOfEventTemplateListDto"] = "HalResourceOfEventTemplateListDto",
        ["HalCollectionEmbeddedOfGroupListDto"] = "HalResourceOfGroupListDto",
        ["HalCollectionEmbeddedOfGroupMemberDto"] = "HalResourceOfGroupMemberDto",
        ["HalCollectionEmbeddedOfOrganizationMemberDto"] = "HalResourceOfOrganizationMemberDto",
        ["HalCollectionEmbeddedOfIndexedDidListDto"] = "HalResourceOfIndexedDidListDto",
        ["HalCollectionEmbeddedOfLocationListDto"] = "HalResourceOfLocationListDto",
        ["HalCollectionEmbeddedOfLocationRoomListDto"] = "HalResourceOfLocationRoomListDto",
        ["HalCollectionEmbeddedOfOrganizationListDto"] = "HalResourceOfOrganizationListDto",
        ["HalCollectionEmbeddedOfOrganizationReviewDto"] = "HalResourceOfOrganizationReviewDto",
        ["HalCollectionEmbeddedOfEmailDispatchStatusDto"] = "HalResourceOfEmailDispatchStatusDto",
        ["HalCollectionEmbeddedOfSupportAccessSessionDto"] = "HalResourceOfSupportAccessSessionDto",
        ["HalCollectionEmbeddedOfSupportAccessAuditEventDto"] = "HalResourceOfSupportAccessAuditEventDto",
        ["HalCollectionEmbeddedOfProjectionStatusDto"] = "HalResourceOfProjectionStatusDto",
        ["HalCollectionEmbeddedOfProjectionDirtyScopeDto"] = "HalResourceOfProjectionDirtyScopeDto",
        ["HalCollectionEmbeddedOfTagListDto"] = "HalResourceOfTagListDto",
        ["HalCollectionEmbeddedOfTenantUserRoleGrantListDto"] = "HalResourceOfTenantUserRoleGrantListDto",
        ["HalCollectionEmbeddedOfWebhookConsumerDto"] = "HalResourceOfWebhookConsumerDto",
        ["HalCollectionEmbeddedOfWebhookEndpointDto"] = "HalResourceOfWebhookEndpointDto",
        ["HalCollectionEmbeddedOfWebhookMessageDto"] = "HalResourceOfWebhookMessageDto",
        ["HalCollectionEmbeddedOfWebhookDeliveryAttemptDto"] = "HalResourceOfWebhookDeliveryAttemptDto",
        ["HalCollectionEmbeddedOfWebhookProviderPublicationDto"] = "HalResourceOfWebhookProviderPublicationDto",
        ["HalCollectionEmbeddedOfWebhookBulkReplayOperationDto"] = "HalResourceOfWebhookBulkReplayOperationDto",
        ["HalCollectionEmbeddedOfAiConversationSummaryDto"] = "HalResourceOfAiConversationSummaryDto",
        ["HalCollectionEmbeddedOfAiReferenceSearchResultDto"] = "HalResourceOfAiReferenceSearchResultDto",
        ["HalCollectionEmbeddedOfStorageObjectListDto"] = "HalResourceOfStorageObjectListDto",
        ["HalCollectionEmbeddedOfControlPlaneTenantListItemDto"] = "HalResourceOfControlPlaneTenantListItemDto",
        ["HalCollectionEmbeddedOfControlPlaneTenantPlanListItemDto"] = "HalResourceOfControlPlaneTenantPlanListItemDto",
        ["HalCollectionEmbeddedOfEventSessionSpeakerListDto"] = "HalResourceOfEventSessionSpeakerListDto",
    };

    public static bool IsCatalogedDetailResourceSchema(string schemaName)
        => DetailResourceMappings.ContainsKey(schemaName);

    public static bool IsRegisteredDto(Type dtoType)
        => RegisteredDtoTypes.Contains(dtoType);
}
