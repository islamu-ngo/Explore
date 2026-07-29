<!-- ABOUTME: Baseline registry of every update DTO, handler file, and public PUT/PATCH endpoint. -->
<!-- ABOUTME: Tracks each baseline surface through grouped-PATCH, semantic-retention, or removal disposition. -->

# Full Property Update Sub-DTO Pattern - Exhaustive Inventory

Last Updated: 2026-07-28 Europe/Brussels

## Purpose And Coverage Gate

This file is the exhaustive baseline scope contract for the workstream. Family summaries are not sufficient. Completion requires every row in all three registers below to reach its assigned final state, including rows whose required state is deletion:

- the 59 files matching `src/Explore.Application/DTOs/**/Update*Dto.cs` at re-baseline;
- the 71 files matching `src/Explore.Application/Features/**/Handlers/Commands/Update*CommandHandler.cs` at re-baseline;
- the 104 `[HttpPut]` or `[HttpPatch]` endpoints across 53 API controllers at re-baseline.

The registers intentionally overlap. That overlap catches top-level DTOs without handlers, handlers without matching `Update*Dto.cs` files, nested DTO files, Application-only commands, and controller-local request records. A new update surface discovered during implementation must be added before the owning phase can pass.

## Disposition Codes

| Code | Required final state |
|---|---|
| `C` | Already canonical grouped route-ID PATCH; verify and retain. |
| `M` | Migrate to route-ID PATCH with nullable logical groups and `OptionalUpdate<T>` where clear is valid. |
| `S` | Keep exact-key/category/full-replacement PUT because the addressed resource is replaced completely. |
| `A` | Keep a dedicated action/workflow endpoint; it is not an entity property update. |
| `R` | Remove generic public CRUD/update exposure and retain or add only a safe dedicated workflow. |
| `N` | Nested/local DTO or Application-only surface; no independent public API operation. Its parent contract owns it. |

No row may finish as `investigate`, `other`, or an unbounded family wildcard.

## Verified Baseline Counts

| Register | Baseline count | Evidence command/pattern |
|---|---:|---|
| Update DTO files | 59 | `src/Explore.Application/DTOs/**/Update*Dto.cs` |
| Update command-handler files | 71 | `src/Explore.Application/Features/**/Handlers/Commands/Update*CommandHandler.cs` |
| Public PUT/PATCH endpoints | 104 | `\[Http(?:Put|Patch)` under `src/Explore.API/Controllers` |
| Controllers containing PUT/PATCH | 53 | Same controller scan. |
| Update-named tests | 46 | `tests/**/*Update*Tests.cs` |

Task 5.1 acceptance removes D-031/D-037/D-044/D-059, H-049/H-052/H-058/H-071, and A-044/A-071/A-077/A-098 from current repository reality. Their retained baseline rows prove that every `R` disposition reached deliberate removal rather than disappearing from scope. Any newly discovered update surface must still be added and classified before completion.

## Register 1: All 59 Update DTO Files

| ID | DTO file under `src/Explore.Application/DTOs/` | Surface | Disposition | Task |
|---|---|---|---|---|
| D-001 | `EventReporting/UpdateMyReportCommunicationConsentDto.cs` | Reporter communication consent | M | 4.3 |
| D-002 | `EventRegistration/UpdateEventRegistrationDto.cs` | EventRegistration | C | 2.1 |
| D-003 | `Webhooks/UpdateWebhookConsumerProviderModeRequestDto.cs` | Webhook consumer provider mode | M | 4.1 |
| D-004 | `Webhooks/UpdateWebhookEndpointRequestDto.cs` | WebhookEndpoint | M | 4.1 |
| D-005 | `Integrations/UpdateListmonkIntegrationSettingsDto.cs` | Listmonk integration settings | M | 4.2 |
| D-006 | `EventReporting/UpdateReportingProviderLocksDto.cs` | Instance reporting provider locks | M | 4.3 |
| D-007 | `EventReporting/UpdateReportingRoutingSettingsDto.cs` | Tenant reporting routing | M | 4.3 |
| D-008 | `EventSession/UpdateEventSessionDto.cs` | EventSession | C | 2.1 |
| D-009 | `EventSessionSpeaker/UpdateEventSessionSpeakerDto.cs` | EventSessionSpeaker | C | 3.4 |
| D-010 | `EventCategories/UpdateEventCategoriesDto.cs` | EventCategories relationship | C | 2.2 |
| D-011 | `EventTags/UpdateEventTagsDto.cs` | EventTags relationship | C | 2.2 |
| D-012 | `EventSessionLanguage/UpdateEventSessionLanguageDto.cs` | EventSessionLanguage | C | 2.1 |
| D-013 | `User/UpdateUserDto.cs` | User | C | 2.1 |
| D-014 | `Organization/UpdateOrganizationDto.cs` | Organization | C | 2.1 |
| D-015 | `LocationRoom/UpdateLocationRoomDto.cs` | LocationRoom | C | 2.1 |
| D-016 | `Location/UpdateLocationDto.cs` | Location | C | 2.1 |
| D-017 | `Group/UpdateGroupDto.cs` | Group | C | 2.1 |
| D-018 | `EventDay/UpdateEventDayDto.cs` | EventDay | C | 2.1 |
| D-019 | `EventSeries/UpdateEventSeriesDto.cs` | EventSeries | C | 2.1 |
| D-020 | `EventAgendaItem/UpdateEventAgendaItemDto.cs` | EventAgendaItem | C | 2.1 |
| D-021 | `Event/UpdateEventDto.cs` | Event | C | 2.1 |
| D-022 | `Actor/UpdateActorDto.cs` | Actor | C | 2.1 |
| D-023 | `Actor/UpdateActorAppearanceDto.cs` | Actor appearance nested group | N | 2.1 |
| D-024 | `Category/UpdateCategoryDto.cs` | Category | C | 2.1 |
| D-025 | `User/UpdateUserProfileImageDto.cs` | User profile-image nested group | N | 2.1 |
| D-026 | `User/UpdateUserNamesDto.cs` | User names nested group | N | 2.1 |
| D-027 | `Appearance/UpdateAppearanceProfileRequestDto.cs` | AppearanceProfile | C | 3.3 |
| D-028 | `ActorSubscription/UpdateActorSubscriptionNotificationLevelDto.cs` | ActorSubscription notification level | C | 3.2 |
| D-029 | `StorageObject/UpdateStorageObjectDto.cs` | StorageObject editable metadata | M | 4.1 |
| D-030 | `Event/UpdateEventDraftRequestDto.cs` | Internal/local Event draft workflow | N | 2.3 |
| D-031 | `UserExternalLogin/UpdateUserExternalLoginDto.cs` | UserExternalLogin generic identity mapping | R | 5.1 |
| D-032 | `Tenant/UpdateTenantNavigationLinkOrderDto.cs` | Tenant navigation reorder action | A | 3.2 |
| D-033 | `Tenant/UpdateTenantNavigationLinkDto.cs` | TenantNavigationLink | C | 3.1 |
| D-034 | `Tenant/UpdateTenantDto.cs` | Tenant | C | 3.1 |
| D-035 | `TagTypeTags/UpdateTagTypeTagsDto.cs` | TagTypeTags relationship | C | 3.4 |
| D-036 | `Tag/UpdateTagDto.cs` | Tag | C | 3.1 |
| D-037 | `SyncState/UpdateSyncStateDto.cs` | ATProto SyncState internal cursor | R | 5.1 |
| D-038 | `Settings/UpdateSettingValueDto.cs` | Exact setting-key replacement | S | 1.1 |
| D-039 | `Settings/UpdateSettingBatchDto.cs` | Exact category batch replacement | S | 1.1 |
| D-040 | `OrganizationMember/UpdateOrganizationMemberRoleDto.cs` | Organization member role action | A | 6.1 |
| D-041 | `Organization/UpdateOrganizationApprovalStatusDto.cs` | Organization approval action | A | 6.1 |
| D-042 | `Localization/UpdateLocalizationGovernanceDto.cs` | Localization governance settings | M | 4.2 |
| D-043 | `GroupMember/UpdateGroupMemberRoleDto.cs` | Group member role action | A | 6.1 |
| D-044 | `IndexedDid/UpdateIndexedDidDto.cs` | IndexedDid provider-owned index row | R | 5.1 |
| D-045 | `Group/UpdateGroupApprovalStatusDto.cs` | Group approval action | A | 6.1 |
| D-046 | `ExternalApiKey/UpdateExternalApiKeyPolicyDto.cs` | External API key policy metadata | M | 4.2 |
| D-047 | `EventTemplate/UpdateEventTemplateDto.cs` | EventTemplate | M | 3.6 |
| D-048 | `EventTemplate/UpdateEventTemplateDefinitionDto.cs` | EventTemplate definition nested group | N | 3.6 |
| D-049 | `EventSessionTemplate/UpdateEventSessionTemplateDto.cs` | EventSessionTemplate | M | 3.6 |
| D-050 | `EventSessionTemplate/UpdateEventSessionTemplateDefinitionDto.cs` | EventSessionTemplate definition nested group | N | 3.6 |
| D-051 | `EventSessionGroup/UpdateEventSessionGroupRequestDto.cs` | EventSessionGroup | C | 3.4 |
| D-052 | `EventSessionCustomProperty/UpdateEventSessionCustomPropertyDefinitionDto.cs` | EventSession custom-property definition | M | 3.5 |
| D-053 | `EventSessionAgendaItem/UpdateEventSessionAgendaItemDto.cs` | EventSessionAgendaItem | C | 3.4 |
| D-054 | `EventCustomProperty/UpdateEventCustomPropertyDefinitionDto.cs` | Event custom-property definition | M | 3.5 |
| D-055 | `CustomPropertyDefinition/UpdateCustomPropertyDefinitionDto.cs` | CustomPropertyDefinition | M | 3.5 |
| D-056 | `CategoryTypeCategories/UpdateCategoryTypeCategoriesDto.cs` | CategoryTypeCategories relationship | C | 3.4 |
| D-057 | `Appearance/UpdateUserAppearancePreferencesDto.cs` | Current user appearance preferences | C | 3.3 |
| D-058 | `Appearance/UpdateUiThemeDto.cs` | UiTheme | C | 3.3 |
| D-059 | `ActorKeyStore/UpdateActorKeyStoreDto.cs` | ActorKeyStore generic key material | R | 5.1 |

## Register 2: Baseline 71 Plus Post-Baseline Update Handler Surfaces

Paths are relative to `src/Explore.Application/Features/`.

| ID | Handler file | Surface | Disposition | Task |
|---|---|---|---|---|
| H-001 | `EventLocations/Handlers/Commands/UpdateEventLocationPolicyCommandHandler.cs` | Event location disclosure policy | C | 3.4 |
| H-002 | `Events/Handlers/Commands/UpdateEventCommandHandler.cs` | Event | C | 2.1 |
| H-003 | `EventSessions/Handlers/Commands/UpdateEventSessionCommandHandler.cs` | EventSession | C | 2.1 |
| H-004 | `EventRegistrations/Handlers/Commands/UpdateEventRegistrationCommandHandler.cs` | EventRegistration | C | 2.1 |
| H-005 | `EventReporting/Handlers/Commands/UpdateMyReportCommunicationConsentCommandHandler.cs` | Report communication consent | M | 4.3 |
| H-006 | `InstanceOnboarding/Handlers/Commands/UpdateInstanceGovernanceSettingsCommandHandler.cs` | Legacy monolithic instance governance write | R | 1.3 |
| H-007 | `Settings/Handlers/Commands/UpdateSettingBatchCommandHandler.cs` | Category setting replacement | S | 1.1 |
| H-008 | `Settings/Handlers/Commands/UpdateSettingCommandHandler.cs` | Exact setting-key replacement | S | 1.1 |
| H-009 | `EventAgendaItems/Handlers/Commands/UpdateEventAgendaItemCommandHandler.cs` | EventAgendaItem | C | 2.1 |
| H-010 | `EventSessionAgendaItems/Handlers/Commands/UpdateEventSessionAgendaItemCommandHandler.cs` | EventSessionAgendaItem | C | 3.4 |
| H-011 | `EventSessionGroups/Handlers/Commands/UpdateEventSessionGroupCommandHandler.cs` | EventSessionGroup | C | 3.4 |
| H-012 | `LocationRooms/Handlers/Commands/UpdateLocationRoomCommandHandler.cs` | LocationRoom | C | 2.1 |
| H-013 | `EventSessionLanguages/Handlers/Commands/UpdateEventSessionLanguageCommandHandler.cs` | EventSessionLanguage | C | 2.1 |
| H-014 | `Webhooks/Handlers/Commands/UpdateWebhookEndpointCommandHandler.cs` | WebhookEndpoint | M | 4.1 |
| H-015 | `Webhooks/Handlers/Commands/UpdateWebhookConsumerProviderModeCommandHandler.cs` | Webhook provider mode | M | 4.1 |
| H-016 | `InstanceOnboarding/Handlers/Commands/UpdateAuthorizationProviderConfigurationCommandHandler.cs` | Authorization provider settings | M | 1.3 |
| H-017 | `InstanceOnboarding/Handlers/Commands/UpdateAuthProviderConfigurationCommandHandler.cs` | Authentication provider settings | M | 1.3 |
| H-018 | `Localization/Handlers/Commands/UpdateLocalizationGovernanceCommandHandler.cs` | Localization governance | M | 4.2 |
| H-019 | `TenantOnboarding/Handlers/Commands/UpdateTenantPolicySettingsCommandHandler.cs` | Broad tenant policy settings write | R | 5.3 |
| H-020 | `Integrations/Listmonk/Handlers/Commands/UpdateListmonkIntegrationSettingsCommandHandler.cs` | Listmonk settings | M | 4.2 |
| H-021 | `Users/Handlers/Commands/UpdateUserLastActiveTenantCommandHandler.cs` | Last-active-tenant selection action | A | 6.1 |
| H-022 | `Notifications/Handlers/Commands/UpdateGroupNotificationPreferenceMatrixCommandHandler.cs` | Group notification matrix | C | 3.2 |
| H-023 | `Notifications/Handlers/Commands/UpdateOrganizationNotificationPreferenceMatrixCommandHandler.cs` | Organization notification matrix | C | 3.2 |
| H-024 | `Notifications/Handlers/Commands/UpdateCurrentUserNotificationPreferenceMatrixCommandHandler.cs` | User notification matrix | C | 3.2 |
| H-025 | `ControlPlane/Handlers/Commands/UpdateControlPlaneTenantPlanVersionDraftCommandHandler.cs` | Tenant plan version draft | C | 3.1 |
| H-026 | `EventReporting/Handlers/Commands/UpdateReportingProviderLocksCommandHandler.cs` | Reporting provider locks | M | 4.3 |
| H-027 | `EventReporting/Handlers/Commands/UpdateReportingRoutingSettingsCommandHandler.cs` | Reporting routing settings | M | 4.3 |
| H-028 | `EventSessionSpeakers/Handlers/Commands/UpdateEventSessionSpeakerCommandHandler.cs` | EventSessionSpeaker | C | 3.4 |
| H-029 | `StorageObjects/Handlers/Commands/UpdateStorageObjectCommandHandler.cs` | StorageObject metadata | M | 4.1 |
| H-030 | `ExternalApiKeys/Handlers/Commands/UpdateExternalApiKeyPolicyCommandHandler.cs` | External API key policy | M | 4.2 |
| H-031 | `Groups/Handlers/Commands/UpdateGroupCommandHandler.cs` | Group | C | 2.1 |
| H-032 | `EventTags/Handlers/Commands/UpdateEventTagsCommandHandler.cs` | EventTags relationship | C | 2.2 |
| H-033 | `EventCategories/Handlers/Commands/UpdateEventCategoriesCommandHandler.cs` | EventCategories relationship | C | 2.2 |
| H-034 | `Locations/Handlers/Commands/UpdateLocationCommandHandler.cs` | Location | C | 2.1 |
| H-035 | `Organizations/Handlers/Commands/UpdateOrganizationCommandHandler.cs` | Organization | C | 2.1 |
| H-036 | `Users/Handlers/Commands/UpdateUserCommandHandler.cs` | User | C | 2.1 |
| H-037 | `EventSeries/Handlers/Commands/UpdateEventSeriesCommandHandler.cs` | EventSeries | C | 2.1 |
| H-038 | `EventDays/Handlers/Commands/UpdateEventDayCommandHandler.cs` | EventDay | C | 2.1 |
| H-039 | `Categories/Handlers/Commands/UpdateCategoryCommandHandler.cs` | Category | C | 2.1 |
| H-040 | `Actors/Handlers/Commands/UpdateActorCommandHandler.cs` | Actor | C | 2.1 |
| H-041 | `Organizations/Handlers/Commands/UpdateOrganizationApprovalStatusCommandHandler.cs` | Organization approval action | A | 6.1 |
| H-042 | `Events/Handlers/Commands/UpdateEventDraftCommandHandler.cs` | Internal Event draft workflow | N | 2.3 |
| H-043 | `EventRoleAssignments/Handlers/Commands/UpdateEventRoleAssignmentWindowCommandHandler.cs` | Application-only event role-assignment window workflow | N | 6.1 |
| H-044 | `TenantStorageSettings/Handlers/Commands/UpdateTenantStorageSettingsCommandHandler.cs` | Retired broad tenant storage handler; replaced by H-080 | R | 1.2 |
| H-045 | `InstanceOnboarding/Handlers/Commands/UpdateInstanceStorageSettingsCommandHandler.cs` | Instance storage settings | M | 1.3 |
| H-046 | `ActorSubscriptions/Handlers/Commands/UpdateActorSubscriptionNotificationLevelCommandHandler.cs` | ActorSubscription notification property | C | 3.2 |
| H-047 | `EventSessionTemplates/Handlers/Commands/UpdateEventSessionTemplateCommandHandler.cs` | EventSessionTemplate | M | 3.6 |
| H-048 | `EventTemplates/Handlers/Commands/UpdateEventTemplateCommandHandler.cs` | EventTemplate | M | 3.6 |
| H-049 | `UserExternalLogins/Handlers/Commands/UpdateUserExternalLoginCommandHandler.cs` | UserExternalLogin generic mapping | R | 5.1 |
| H-050 | `Tags/Handlers/Commands/UpdateTagCommandHandler.cs` | Tag | C | 3.1 |
| H-051 | `TagTypeTags/Handlers/Commands/UpdateTagTypeTagsCommandHandler.cs` | TagTypeTags relationship | C | 3.4 |
| H-052 | `SyncStates/Handlers/Commands/UpdateSyncStateCommandHandler.cs` | SyncState cursor | R | 5.1 |
| H-053 | `Roles/Handlers/Commands/UpdateRolePermissionsCommandHandler.cs` | Application-only complete role-permission replacement | N | 6.1 |
| H-054 | `OrganizationMembers/Handlers/Commands/UpdateOrganizationMemberRoleCommandHandler.cs` | Organization member role action | A | 6.1 |
| H-055 | `InstanceOnboarding/Handlers/Commands/UpdateResolverConfigurationCommandHandler.cs` | Resolver settings | M | 1.3 |
| H-056 | `InstanceOnboarding/Handlers/Commands/UpdateInstanceSmtpSettingsCommandHandler.cs` | SMTP settings | M | 1.3 |
| H-057 | `InstanceOnboarding/Handlers/Commands/UpdateAnalyticsGovernanceSettingsCommandHandler.cs` | Analytics governance | M | 1.3 |
| H-058 | `IndexedDids/Handlers/Commands/UpdateIndexedDidCommandHandler.cs` | IndexedDid index row | R | 5.1 |
| H-059 | `Groups/Handlers/Commands/UpdateGroupApprovalStatusCommandHandler.cs` | Group approval action | A | 6.1 |
| H-060 | `GroupMembers/Handlers/Commands/UpdateGroupMemberRoleCommandHandler.cs` | Group member role action | A | 6.1 |
| H-061 | `Footer/Handlers/Commands/UpdateTenantFooterSettingsCommandHandler.cs` | Retired broad tenant footer handler; replaced by H-078 | R | 1.2 |
| H-062 | `Footer/Handlers/Commands/UpdateFooterLinkGroupCommandHandler.cs` | FooterLinkGroup | C | 3.1 |
| H-063 | `Footer/Handlers/Commands/UpdateFooterLinkCommandHandler.cs` | FooterLink | C | 3.1 |
| H-064 | `Footer/Handlers/Commands/UpdateFooterGovernanceSettingsCommandHandler.cs` | Footer governance | M | 1.3 |
| H-065 | `EventSessionCustomProperties/Handlers/Commands/UpdateEventSessionCustomPropertyDefinitionCommandHandler.cs` | EventSession custom-property definition | M | 3.5 |
| H-066 | `EventCustomProperties/Handlers/Commands/UpdateEventCustomPropertyDefinitionCommandHandler.cs` | Event custom-property definition | M | 3.5 |
| H-067 | `CustomPropertyDefinitions/Handlers/Commands/UpdateCustomPropertyDefinitionCommandHandler.cs` | CustomPropertyDefinition | M | 3.5 |
| H-068 | `CategoryTypeCategories/Handlers/Commands/UpdateCategoryTypeCategoriesCommandHandler.cs` | CategoryTypeCategories relationship | C | 3.4 |
| H-069 | `Appearance/Handlers/Commands/UpdateUiThemeCommandHandler.cs` | UiTheme | C | 3.3 |
| H-070 | `Appearance/Handlers/Commands/UpdateCurrentUserAppearancePreferencesCommandHandler.cs` | User appearance preferences | C | 3.3 |
| H-071 | `ActorKeyStores/Handlers/Commands/UpdateActorKeyStoreCommandHandler.cs` | ActorKeyStore generic key update | R | 5.1 |
| H-072 | `EventTicketing/UpdateEventTicketTypeCommandHandler.cs` | Atomic EventTicketType replacement | S | 6.2 |
| H-073 | `EventTicketing/UpdateEventCapacityPoolCommandHandler.cs` | Atomic EventCapacityPool replacement | S | 6.2 |
| H-074 | `Tenants/Handlers/Commands/UpdateTenantCommandHandler.cs` | Tenant grouped update | C | 6.2 |
| H-075 | `EventPublicActions/Handlers/Commands/UpdateEventPublicActionCommandHandler.cs` | Atomic reviewed public-action replacement | S | 6.2 |
| H-076 | `Tenants/Handlers/Commands/UpdateTenantNavLink/UpdateTenantNavLinkCommandHandler.cs` | Tenant navigation-link grouped update | C | 6.2 |
| H-077 | `EventParticipation/Handlers/Commands/ConfigureEventParticipationCommandHandler.cs` | Atomic participation-configuration replacement | S | 6.2 |
| H-078 | `Footer/Handlers/Commands/PatchTenantFooterSettingsCommandHandler.cs` | Tenant footer grouped PATCH | C | 6.2 |
| H-079 | `TenantSettingsDocuments/Handlers/Commands/PatchTenantBrandingSettingsDocumentCommandHandler.cs` | Tenant branding grouped PATCH | C | 6.2 |
| H-080 | `TenantStorageSettings/Handlers/Commands/PatchTenantStorageSettingsCommandHandler.cs` | Tenant storage grouped PATCH | C | 6.2 |

`UpdateInstanceSubResourceHandlers.cs` contains additional update-handler classes but does not match the handler-file glob. Those public operations are still individually covered by API rows A-026 through A-042.

## Register 3: Baseline 104 Plus Post-Baseline Public PUT/PATCH Endpoints

`Route` uses the controller-relative template. `PATCH` means migrate to or verify the grouped Event/EventSession convention; `PUT/action` and `PUT/full` are explicit semantic exceptions.

| ID | Controller / route name | Current route | Final disposition | Task |
|---|---|---|---|---|
| A-001 | `EventLocationController.UpdateEventLocationDisclosure` | `PATCH {eventLocationId}/disclosure` | C: grouped PATCH | 3.4 |
| A-002 | `EventController.UpdateEvent` | `PATCH {id}` | C: verify grouped PATCH | 2.1 |
| A-003 | `TenantOnboardingController.UpdateTenantOnboardingPolicySettings` | `PUT settings` | R: remove broad duplicate after remaining non-policy callers migrate | 5.3 |
| A-004 | `TenantOnboardingController.SaveTenantOnboardingStepProgress` | `PUT steps` | A: retain workflow action | 6.1 |
| A-005 | `InstanceOnboardingController.SaveInstanceOnboardingAuthProviderConfiguration` | `PUT auth-provider-configuration` | R: removed duplicate; canonical grouped PATCH is A-041 | 1.3 |
| A-006 | `InstanceOnboardingController.SaveInstanceOnboardingAuthorizationProviderConfiguration` | `PUT authz-provider-configuration` | R: removed duplicate; canonical grouped PATCH is A-042 | 1.3 |
| A-007 | `ListmonkIntegrationSettingsController.UpdateListmonkIntegrationSettings` | `PUT settings` | M: grouped settings PATCH | 4.2 |
| A-008 | `InstanceModerationReportingSettingsController.UpdateInstanceModerationReportingProviderLocks` | `PUT locks` | M: grouped settings PATCH | 4.3 |
| A-009 | `ModerationReportingRoutingController.UpdateModerationReportingRoutingSettings` | `PUT` | M: grouped settings PATCH; secrets explicit | 4.3 |
| A-010 | `EventSessionSpeakerController.UpdateEventSessionSpeaker` | `PATCH management/{id}` | C: canonical route-ID grouped PATCH | 3.4 |
| A-011 | `ControlPlaneController.UpdateControlPlaneTenantPlanVersionDraft` | `PATCH plans/versions/{versionId}` | C: grouped entity PATCH | 3.1 |
| A-012 | `ControlPlaneController.SetControlPlaneTenantSetting` | `PUT tenants/{tenantId}/settings/{key}` | S: retain exact-key PUT | 6.1 |
| A-013 | `EventAspectController.UpdateEventIslamicAspect` | `PATCH {id}/aspects/islamic` | C: POST create plus grouped PATCH update | 3.4 |
| A-014 | `EventAspectController.UpdateEventTechAspect` | `PATCH {id}/aspects/tech` | C: POST create plus grouped PATCH update | 3.4 |
| A-015 | `WebhooksController.UpdateWebhookConsumerProviderMode` | `PUT consumers/{consumerId}/provider-mode` | M: grouped PATCH | 4.1 |
| A-016 | `WebhooksController.UpdateWebhookEndpoint` | `PUT endpoints/{endpointId}` | M: grouped entity PATCH | 4.1 |
| A-017 | `EventReportsController.UpdateMyEventReportCommunicationConsent` | `PUT my/{reportId}/communication-consent` | M: grouped PATCH | 4.3 |
| A-018 | `OrganizationMemberController.UpdateOrganizationMemberRole` | `PUT role` | A: retain role action | 6.1 |
| A-019 | `UserController.UpdateCurrentUser` | `PATCH {id}` | C: verify grouped PATCH | 2.1 |
| A-020 | `OrganizationController.UpdateOrganizationNotificationPreferences` | `PATCH {id}/notification-preferences` | C: grouped PATCH | 3.2 |
| A-021 | `OrganizationController.SetOrganizationNotificationPreferenceMute` | `PUT {id}/notification-preferences/mute` | A: retain single-value action PUT | 6.1 |
| A-022 | `OrganizationController.UpdateOrganization` | `PATCH {id}` | C: verify grouped PATCH | 2.1 |
| A-023 | `OrganizationController.UpdateOrganizationApprovalStatus` | `PUT {id}/approval-status` | A: retain approval action | 6.1 |
| A-024 | `EventRegistrationController.UpdateEventRegistration` | `PATCH {id}` | C: verify grouped PATCH | 2.1 |
| A-025 | `LocationRoomController.UpdateLocationRoom` | `PATCH {id}` | C: verify grouped PATCH | 2.1 |
| A-026 | `InstanceSettingsController.UpdateInstanceModuleSettings` | `PUT modules` | M: grouped settings PATCH | 1.3 |
| A-027 | `InstanceSettingsController.UpdateInstanceEventPolicy` | `PUT events` | M: grouped settings PATCH | 1.3 |
| A-028 | `InstanceSettingsController.UpdateInstanceOrganizationPolicy` | `PUT organizations` | M: grouped settings PATCH | 1.3 |
| A-029 | `InstanceSettingsController.UpdateInstanceBrandingSettings` | `PUT branding` | M: grouped settings PATCH | 1.3 |
| A-030 | `InstanceSettingsController.UpdateInstanceDomainSettings` | `PUT domains` | M: grouped settings PATCH; explicit UI save | 1.3 |
| A-031 | `InstanceSettingsController.UpdateInstanceTenantDelegationSettings` | `PUT tenant-delegation` | M: grouped settings PATCH | 1.3 |
| A-032 | `InstanceSettingsController.UpdateInstanceAdminPortalSettings` | `PUT admin-portal` | M: grouped settings PATCH | 1.3 |
| A-033 | `InstanceSettingsController.UpdateInstanceAiAssistantGovernanceSettings` | `PUT ai-assistant` | M: grouped settings PATCH | 1.3 |
| A-034 | `InstanceSettingsController.UpdateInstanceMcpGovernanceSettings` | `PUT mcp` | M: grouped settings PATCH | 1.3 |
| A-035 | `InstanceSettingsController.UpdateInstanceRenderPolicySettings` | `PUT render-policy` | M: grouped settings PATCH | 1.3 |
| A-036 | `InstanceSettingsController.UpdateInstanceStorageSettings` | `PUT storage` | M: grouped settings PATCH; validation remains explicit | 1.3 |
| A-037 | `InstanceSettingsController.UpdateInstanceSmtpSettings` | `PUT smtp` | M: grouped settings PATCH; secret group explicit | 1.3 |
| A-038 | `InstanceSettingsController.UpdateInstanceResolverConfiguration` | `PUT resolver-config` | M: grouped settings PATCH | 1.3 |
| A-039 | `InstanceSettingsController.UpdateInstanceAnalyticsGovernanceSettings` | `PUT analytics-governance` | M: grouped settings PATCH | 1.3 |
| A-040 | `InstanceSettingsController.UpdateFooterGovernanceSettings` | `PUT footer-governance` | M: grouped settings PATCH | 1.3 |
| A-041 | `InstanceSettingsController.UpdateInstanceAuthProviderConfiguration` | `PUT auth-provider` | M: grouped settings PATCH; secret group explicit | 1.3 |
| A-042 | `InstanceSettingsController.UpdateInstanceAuthorizationProviderConfiguration` | `PUT authz-provider` | M: grouped settings PATCH; explicit validation | 1.3 |
| A-043 | `LocationController.UpdateLocation` | `PATCH {id}` | C: verify grouped PATCH | 2.1 |
| A-044 | `IndexedDidController.UpdateIndexedDid` | `PUT {did}` | R: remove generic public provider-row update | 5.1 |
| A-045 | `UserAppearanceController.UpdateCurrentUserAppearancePreferences` | `PATCH` | C: grouped settings PATCH | 3.3 |
| A-046 | `UserAppearanceController.UpdateAppearanceProfile` | `PATCH profiles/{profileId}` | C: grouped entity PATCH | 3.3 |
| A-047 | `UserAppearanceController.SetActiveAppearanceProfile` | `PUT active-profile` | A: retain selection action | 6.1 |
| A-048 | `UserAppearanceController.SetAppearanceThemeMode` | `PUT mode` | S: retain exact single preference write | 6.1 |
| A-049 | `UserAppearanceController.ArchiveAppearanceProfile` | `PUT profiles/{profileId}/archive` | A: retain archive action | 6.1 |
| A-050 | `GroupController.UpdateGroupNotificationPreferences` | `PATCH {id}/notification-preferences` | C: grouped PATCH | 3.2 |
| A-051 | `GroupController.SetGroupNotificationPreferenceMute` | `PUT {id}/notification-preferences/mute` | A: retain single-value action PUT | 6.1 |
| A-052 | `GroupController.UpdateGroup` | `PATCH {id}` | C: verify grouped PATCH | 2.1 |
| A-053 | `GroupController.UpdateGroupApprovalStatus` | `PUT {id}/approval-status` | A: retain approval action | 6.1 |
| A-054 | `TenantStorageSettingsController.UpdateTenantStorageSettings` | `PUT` | R: removed broad operation; canonical grouped PATCH is A-111 | 1.2 |
| A-055 | `UiThemeAdminController.UpdateUiTheme` | `PATCH {id}` | C: grouped entity PATCH | 3.3 |
| A-056 | `EventSessionController.UpdateEventSession` | `PATCH {id}` | C: verify grouped PATCH | 2.1 |
| A-057 | `ExternalApiKeyController.UpdateExternalApiKey` | `PUT {id}` | M: grouped policy PATCH; key material excluded | 4.2 |
| A-058 | `ActorSubscriptionController.UpdateActorSubscriptionNotificationLevel` | `PATCH actors/{targetActorId}/notification-level` | C: grouped preference PATCH | 3.2 |
| A-059 | `LocalizationAdminController.UpdateLocalizationGovernance` | `PUT governance` | M: grouped settings PATCH | 4.2 |
| A-060 | `EventSeriesController.UpdateEventSeries` | `PATCH {id}` | C: verify grouped PATCH | 2.1 |
| A-061 | `EventSessionAgendaItemController.UpdateEventSessionAgendaItem` | `PATCH {id}` | C: grouped entity PATCH | 3.4 |
| A-062 | `EmailDispatchAdminController.PauseEmailDispatchTenant` | `PUT tenants/{tenantId}/pause` | A: retain operational action | 6.1 |
| A-063 | `EmailDispatchAdminController.ParkEmailDispatch` | `PUT tenants/{tenantId}/outbox/{outboxId}/park` | A: retain operational action | 6.1 |
| A-064 | `EmailDispatchAdminController.PauseEmailDispatchProcessor` | `PUT control/pause` | A: retain operational action | 6.1 |
| A-065 | `EmailDispatchAdminController.SetEmailDispatchGlobalRateLimitOverride` | `PUT control/rate-limit` | S: retain control-value PUT | 6.1 |
| A-066 | `EventDayController.UpdateEventDay` | `PATCH {id}` | C: verify grouped PATCH | 2.1 |
| A-067 | `EventAgendaItemController.UpdateEventAgendaItem` | `PATCH {id}` | C: verify grouped PATCH | 2.1 |
| A-068 | `TenantSettingsDocumentsController.ReplaceTenantBrandingSettingsDocument` | `PUT branding` | R: removed replacement operation; canonical grouped PATCH is A-110 | 1.2 |
| A-069 | `GroupMemberController.UpdateGroupMember` | `PUT role` | A: retain member-role action | 6.1 |
| A-070 | `CategoryController.UpdateCategory` | `PATCH {id}` | C: verify grouped PATCH | 2.1 |
| A-071 | `ActorKeyStoreController.UpdateActorKeyStore` | `PUT {id}` | R: remove generic public key-material update | 5.1 |
| A-072 | `EventSessionLanguageController.UpdateEventSessionLanguage` | `PATCH {id}` | C: verify grouped PATCH | 2.1 |
| A-073 | `FooterController.UpdateFooterLinkGroup` | `PATCH link-groups/{id}` | C: grouped entity PATCH | 3.1 |
| A-074 | `FooterController.UpdateFooterLink` | `PATCH links/{id}` | C: grouped entity PATCH | 3.1 |
| A-075 | `FooterController.UpdateTenantFooterSettings` | `PUT settings` | R: removed broad operation; canonical grouped PATCH is A-109 | 1.2 |
| A-076 | `ActorController.UpdateActor` | `PATCH {id}` | R: removed generic Actor mutation route; dedicated verified workflows own lifecycle changes | 2.1 |
| A-077 | `UserExternalLoginController.UpdateUserExternalLogin` | `PUT {id}` | R: remove generic provider-mapping update | 5.1 |
| A-078 | `EventSessionGroupController.UpdateEventSessionGroup` | `PATCH {id}` | C: grouped relationship PATCH | 3.4 |
| A-079 | `NotificationController.UpdateCurrentUserNotificationPreferences` | `PATCH preferences/me` | C: grouped PATCH | 3.2 |
| A-080 | `NotificationController.SetCurrentUserNotificationPreferenceMute` | `PUT preferences/me/mute` | A: retain single-value action PUT | 6.1 |
| A-081 | `NotificationController.MarkNotificationAsRead` | `PATCH {id}/read` | A: retain state-transition PATCH | 6.1 |
| A-082 | `NotificationController.ArchiveNotification` | `PATCH {id}/archive` | A: retain state-transition PATCH | 6.1 |
| A-083 | `NotificationController.SnoozeNotification` | `PATCH {id}/snooze` | A: retain state-transition PATCH | 6.1 |
| A-084 | `EventSessionTemplateController.UpdateEventSessionTemplate` | `PATCH {id}` | M: grouped entity PATCH | 3.6 |
| A-085 | `TenantController.UpdateTenant` | `PATCH {id}` | C: grouped entity PATCH | 3.1 |
| A-086 | `TenantController.UpdateTenantNavigationLink` | `PATCH navigation/{id}` | C: grouped entity PATCH | 3.1 |
| A-087 | `TenantController.ReorderTenantNavigationLinks` | `PUT navigation/reorder` | A: retain reorder action | 6.1 |
| A-088 | `EventTemplateController.UpdateEventTemplate` | `PATCH {id}` | M: grouped entity PATCH | 3.6 |
| A-089 | `TagController.UpdateTag` | `PATCH {id}` | C: grouped entity PATCH | 3.1 |
| A-090 | `EventSessionCustomPropertyController.UpdateEventSessionCustomPropertyDefinition` | `PUT {id}` | M: grouped entity PATCH | 3.5 |
| A-091 | `EventSessionCustomPropertyController.SetEventSessionCustomPropertyValue` | `PUT value` | S: retain complete single-value replacement | 6.1 |
| A-092 | `EventSessionCustomPropertyController.SetEventSessionCustomPropertyMultiValues` | `PUT values` | S: retain complete value-set replacement | 6.1 |
| A-093 | `SettingsController.UpdateUserSettingsBatch` | `PUT user/{category}` | S: retain category replacement | 1.1 |
| A-094 | `SettingsController.UpdateUserSetting` | `PUT user/keys/{key}` | S: retain exact-key replacement | 1.1 |
| A-095 | `SettingsController.UpdateTenantSettingsBatch` | `PUT tenant/{category}` | S: retain category replacement | 1.1 |
| A-096 | `SettingsController.UpdateTenantSetting` | `PUT tenant/keys/{key}` | S: retain exact-key replacement | 1.1 |
| A-097 | `SettingsController.UpdateInstanceAtprotoFederationSetting` | `PUT instance/atproto-federation/{key}` | S: retain exact-key replacement | 1.1 |
| A-098 | `SyncStateController.UpdateSyncState` | `PUT {id}` | R: remove generic public sync-cursor update | 5.1 |
| A-099 | `StorageObjectController.UploadStorageUploadSessionContent` | `PUT upload-sessions/{uploadSessionId}/content` | S: retain complete content upload | 6.1 |
| A-100 | `StorageObjectController.UpdateStorageObject` | `PUT {id}` | M: grouped metadata PATCH | 4.1 |
| A-101 | `EventCustomPropertyController.UpdateEventCustomPropertyDefinition` | `PUT {id}` | M: grouped entity PATCH | 3.5 |
| A-102 | `EventCustomPropertyController.SetEventCustomPropertyValue` | `PUT value` | S: retain complete single-value replacement | 6.1 |
| A-103 | `EventCustomPropertyController.SetEventCustomPropertyMultiValues` | `PUT values` | S: retain complete value-set replacement | 6.1 |
| A-104 | `CustomPropertyDefinitionController.UpdateCustomPropertyDefinition` | `PUT {id}` | M: grouped entity PATCH | 3.5 |
| A-105 | `EventParticipationController.ConfigureEventParticipation` | `PATCH` | S: atomic coupled participation-configuration replacement | 6.2 |
| A-106 | `EventPublicActionController.UpdateEventPublicAction` | `PUT {actionId}` | S: atomic reviewed public-action replacement | 6.2 |
| A-107 | `EventTicketingController.UpdateEventTicketType` | `PUT ticket-types/{ticketTypeId}` | S: atomic ticket-type replacement | 6.2 |
| A-108 | `EventTicketingController.UpdateEventCapacityPool` | `PUT capacity-pools/{capacityPoolId}` | S: atomic capacity-pool replacement | 6.2 |
| A-109 | `FooterController.PatchTenantFooterSettings` | `PATCH settings` | C: grouped settings PATCH | 6.2 |
| A-110 | `TenantSettingsDocumentsController.PatchTenantBrandingSettingsDocument` | `PATCH branding` | C: grouped settings PATCH | 6.2 |
| A-111 | `TenantStorageSettingsController.PatchTenantStorageSettings` | `PATCH` | C: grouped settings PATCH with explicit credential action | 6.2 |
| A-112 | `InstanceOnboardingController.SaveInstanceOnboardingProfile` | `PATCH profile` | A: setup-secret-gated non-secret profile save before completion | 6.2 |

Task 6.1 verifies every surviving baseline `A` and `S` operation, plus the POST last-active-tenant selection represented by H-021, through `SemanticUpdateExceptionArchitectureTests`. Task 6.2 extends that exact registry with the four post-baseline `S` operations A-105 through A-108. The guard binds each exception to an exact operation ID, path, verb, and non-empty route-specific rationale. It separately proves that all `N` nested DTOs and Application-only commands have no direct controller operation. H-043 and H-053 were corrected from `A` to `N` because neither has a public endpoint.

## Autosave Classification

| Control type | Save boundary | API shape | Failure behavior |
|---|---|---|---|
| Independent switch/select | Immediate `ValueChanged`. | Exact-key PUT or one grouped PATCH property. | Disable pending control; restore/reload canonical value; accessible error. |
| Independent text | Blur or bounded debounce. | Exact-key PUT or one grouped PATCH property. | No request per keystroke; retain unsaved text until resolution. |
| Coupled invariant | Explicit local Apply or one group/batch. | Strict batch PUT or one atomic PATCH group. | Entire invariant fails atomically. |
| Secret/credential | Explicit submit/rotate. | Dedicated secret group or action; never generic field autosave. | Never echo/log; clear transient input. |
| Destructive/lifecycle/external side effect | Explicit confirmation. | Dedicated action. | Preserve action-specific audit/idempotency. |
| Locked/no HAL affordance | No write. | None. | Explain server-provided restriction. |

## Canonical Grouped PATCH Checklist

- Route ID is authoritative; body has no entity ID or tenant ID.
- Nullable independently saveable groups express intent.
- `OptionalUpdate<T>` distinguishes omission, set, and clear.
- Empty wrapper and present no-op group fail validation.
- Handler manually validates, loads once, authorizes every present group, checks concurrency, applies groups, and saves once.
- Multi-repository mutation uses existing `IUnitOfWork`.
- Cache invalidation and audit happen after successful persistence only.
- HAL edit relation and generated client match the canonical operation.
- Old broad operation, DTO, client overload, and tests are removed atomically.

## Exhaustive Completion Assertions

Task 6.2 must add or extend architecture/contract tests that compare repository reality to this register:

1. Every baseline `Update*Dto.cs` file is listed once as D-001 through D-059, and each current file is either represented by a retained row or added explicitly.
2. Every baseline matching update-handler file is listed once as H-001 through H-071, and each current file is either represented by a retained row or added explicitly.
3. Every baseline API `[HttpPut]`/`[HttpPatch]` endpoint is listed once as A-001 through A-104, and each current endpoint is either represented by a retained row or added explicitly.
4. Every `M` row is implemented and no longer exposes the old broad operation.
5. Every `C` row still satisfies the canonical grouped PATCH checklist.
6. Every `S` or `A` exception has an exact semantic rationale, not a wildcard exemption.
7. Every `R` row has no generic public create/update path for provider-owned, credential, or internal state.
8. Any newly discovered row blocks phase/workstream completion until classified and implemented.
