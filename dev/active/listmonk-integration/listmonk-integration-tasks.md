<!-- ABOUTME: Task checklist for Listmonk integration implementation. -->
<!-- ABOUTME: Tracks completion state per task across all implementation phases. -->

# Listmonk Integration — Tasks

Last Updated: 2026-07-09 Europe/Brussels

## Phase 1: Domain Foundation — Integration Settings & Secret Keys

- [x] **1.1** Add `GovernanceSettingKeys.Integrations.Listmonk` nested class
  - Keys: `Enabled`, `InstanceUrl`, `DefaultListId`, `PreconfirmSubscriptions`, `SyncOnRegistration`
  - Add `GovernanceSettingKeys.TenantDelegation.LockIntegrations`
  - File: `Explore.Domain/Constants/GovernanceSettingKeys.cs`

- [x] **1.2** Add `InfrastructureSecretSettingKeys.Integrations.Listmonk` section
  - Keys: `ApiUsername`, `ApiKey`
  - File: `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs`

- [x] **1.3** Register Listmonk secrets in `SecretDefinitionRegistry`
  - Scope: `Instance | Tenant`, Infisical folder: `/integrations`
  - File: `Explore.Domain/Secrets/SecretDefinitionRegistry.cs`

- [x] **1.4** Create `IntegrationSettingDefinitions` and register in `SettingRegistry`
  - File (new): `Explore.Domain/Settings/Definitions/IntegrationSettingDefinitions.cs`
  - File (modify): `Explore.Domain/Settings/SettingRegistry.cs`

- [x] **1.5** Verify build passes

## Phase 2: Integration Sync Outbox Entity & Persistence

- [x] **2.1** Create `IntegrationSyncOutbox` domain entity
  - Enums: `IntegrationSyncKind`, `IntegrationSyncStatus`
  - File (new): `Explore.Domain/IntegrationSyncOutbox.cs`

- [x] **2.2** Create `IIntegrationSyncOutboxRepository` interface
  - Methods: `Create`, `GetPendingBatchAsync`, `TryMarkAsProcessingAsync`, `MarkAsCompletedAsync`, `MarkAsFailedAsync`, `MarkAsDeadLetteredAsync`, `DeleteCompletedOlderThanAsync`
  - File (new): `Explore.Application/Contracts/Persistence/IIntegrationSyncOutboxRepository.cs`

- [x] **2.3** Create `IntegrationSyncOutboxRepository` implementation
  - Optimistic concurrency via `ProcessingLeaseToken`
  - File (new): `Explore.Persistence/Repositories/IntegrationSyncOutboxRepository.cs`

- [x] **2.4** Create EF entity type configuration
  - Tenant query filter, WorkerPoll index, Tenant index, Correlation index
  - File (new): `Explore.Persistence/Configurations/Entities/IntegrationSyncOutboxConfiguration.cs`

- [x] **2.5** Create EF migration
  - Table: `integration_sync_outbox`
  - File (new): `Explore.Persistence/Migrations/...`

- [x] **2.6** Verify build and migration list

## Phase 3: Application Layer — Sync Contract & Registration Wiring

- [x] **3.1** Define `IIntegrationSyncService` contract
  - File (new): `Explore.Application/Contracts/Integrations/IIntegrationSyncService.cs`
  - Implemented as `IIntegrationSyncDrainService` for the durable worker boundary.

- [x] **3.2** Define `IntegrationSyncContracts` (request/result records)
  - File (new): `Explore.Application/Contracts/Integrations/IntegrationSyncContracts.cs`
  - Implemented as result records/enums in `IIntegrationSyncDrainService.cs`.

- [x] **3.3** Create `IIntegrationSettingsResolver` contract
  - File (new): `Explore.Application/Contracts/Services/IIntegrationSettingsResolver.cs`
  - Ponytail: reused existing `IHierarchicalSettingsResolver` and secret resolver instead of adding a one-use abstraction.

- [x] **3.4** Implement `IntegrationSettingsResolver`
  - Uses `IHierarchicalSettingsResolver` for governance settings + secrets
  - File (new): `Explore.Application/Services/IntegrationSettingsResolver.cs`
  - Implemented directly in `ListmonkRegistrationSyncOutboxFactory` and `ListmonkSyncService` using existing resolvers.

- [x] **3.5** Wire registration handler to create `IntegrationSyncOutbox` row
  - Inside the existing transaction scope
  - Only when `ShareEmailWithOrganizer == true` AND Listmonk integration enabled
  - File (modify): `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs`

- [x] **3.6** Modify `CreateWithChildrenAndCapacityAsync` to accept optional outbox row
  - File (modify): `IEventRegistrationIntentRepository` + implementation

- [x] **3.7** Write unit tests for consent+outbox flow
  - consent=true+enabled → outbox row, consent=false → no row, consent=true+disabled → no row

## Phase 4: Infrastructure — Listmonk HTTP Client & Sync Processor

- [x] **4.1** Create `ListmonkSyncService`
  - NSwag-generated client, IHttpClientFactory named client, Basic Auth, POST /api/subscribers
  - SSRF validation, 409 idempotent success, error classification
  - File (new): `Explore.Infrastructure/Integrations/ListmonkSyncService.cs`

- [x] **4.2** Create `IntegrationSyncProcessor` BackgroundService
  - Polls outbox, claims via optimistic concurrency, routes by kind
  - Exponential backoff retry, dead-letter after max attempts
  - File (new): `Explore.Infrastructure/Integrations/IntegrationSyncProcessor.cs`
  - File (new): `Explore.Infrastructure/Integrations/IntegrationSyncProcessorSettings.cs`

- [ ] **4.3** Create `ListmonkIntegrationHealthCheck`
  - Returns Degraded (not Unhealthy), skips when disabled
  - File (new): `Explore.Infrastructure/HealthChecks/ListmonkIntegrationHealthCheck.cs`

- [x] **4.4** Register DI services
  - Named HttpClient, sync service, processor, health check
  - File (modify): `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
  - File (modify): API `Program.cs`

- [ ] **4.5** Write unit tests for HTTP client and processor

## Phase 5: API Layer — Integration Settings Endpoints

- [ ] **5.1** Create `GetIntegrationSettingsRequest` + handler
  - File (new): `Explore.Application/Features/Integrations/...`

- [ ] **5.2** Create `UpdateIntegrationSettingsCommand` + handler
  - URL validation, SSRF check, list ID validation
  - File (new): `Explore.Application/Features/Integrations/...`

- [ ] **5.3** Create `TestListmonkConnectionCommand` + handler
  - File (new): `Explore.Application/Features/Integrations/...`

- [ ] **5.4** Create `IntegrationSettingsController`
  - GET [AllowAnonymous], PUT [Authorize], POST test [Authorize]
  - HAL links, route names, endpoint classifications
  - File (new): `Explore.API/Controllers/IntegrationSettingsController.cs`
  - File (modify): `RouteNames.cs`

- [ ] **5.5** Verify architecture tests pass

## Phase 6: Webhook Payload Enhancement

- [ ] **6.1** Update `registration.created` descriptor in `WebhookEventTypeRegistry`
  - Add `consentToEmailShare` (required), `attendeeEmail`, `attendeeFirstName`, `attendeeLastName` (optional)
  - Bump SchemaVersion to 2
  - File (modify): `Explore.Application/Webhooks/WebhookEventTypeRegistry.cs`

- [ ] **6.2** Update registration webhook event publishing to include consent-conditional data
  - Populate `WebhookEventBuildContext.Data` with attendee PII when consent=true

- [ ] **6.3** Write tests for both consent paths

## Phase 7: Blazor UI — Integration Settings Page

- [ ] **7.1** Create integration settings page/component
  - Toggle, URL, credentials (masked), list ID, preconfirm, test connection button
  - HAL-link gated affordances

- [ ] **7.2** Verify registration consent checkbox exists in UI
  - If missing, add it with proper labeling

## Phase 8: Documentation & Final Testing

- [ ] **8.1** Update `docs/ARCHITECTURE.md`
- [ ] **8.2** Update `docs/OUTBOX_PATTERN.md`
- [ ] **8.3** Update `docs/CONFIGURATION.md`
- [ ] **8.4** Update `docs/SECRETS.md`
- [ ] **8.5** Update `docs/API.md`
- [ ] **8.6** Update `docs/API_CHANGELOG.md`
- [x] **8.7** Write integration tests (registration → sync flow)
  - Covered registration → durable sync outbox in `Event.Application.UnitTests`; persistence mapping verified by `Event.Persistence.IntegrationTests`.
- [ ] **8.8** Run full test suite per `docs/OPERATIONS.md`
- [x] **8.9** Update dev-docs to reflect final state

## Added Scope: NSwag Listmonk API Client

- [x] **NSwag.1** Add project-local NSwag config for Listmonk
  - File (new): `Explore.Infrastructure/nswag.listmonk.json`
  - Input: `schemas/openapi-listmonk.yaml`
  - Output: `Explore.Infrastructure/Integrations/Listmonk/Generated/ListmonkApiClient.g.cs`

- [x] **NSwag.2** Generate Listmonk client and use generated subscriber API surface
  - Generated class: `ListmonkApiClient`
  - Generated DTO: `NewSubscriber`
  - Worker call: `CreateSubscriberAsync(...)`
