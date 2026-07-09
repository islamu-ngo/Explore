<!-- ABOUTME: Living context document for Listmonk integration implementation. -->
<!-- ABOUTME: Tracks current state, decisions, blockers, files changed, and validation results. -->

# Listmonk Integration — Context

Last Updated: 2026-07-09 Europe/Brussels

## Current State

**Phase:** Backend foundation implemented.  
**Status:** Settings/secrets, durable outbox, registration enqueue, NSwag Listmonk client, and hosted sync worker are in place.  
**Blockers:** None for the backend slice; API settings endpoints, Blazor UI, health check, and webhook payload enhancement remain pending.

## Key Discovery During Research

The **contact-sharing consent model is fully implemented**:
- `EventContactShareConsent` entity with per-organizer scope, email snapshot, purpose code, consent status lifecycle (granted/withdrawn)
- `IContactShareConsentService.ProcessRegistrationConsent()` handles consent during registration
- `CreateEventRegistrationDto` has `ShareEmailWithOrganizer`, `ConsentTextAcknowledged`, `ConsentUiVersion`
- `CreateEventRegistrationCommandHandler` already calls `_consentService.ProcessRegistrationConsent()` when `ShareEmailWithOrganizer == true`
- Consent is **per-organizer**, not per-event — re-registering for a different event by the same organizer reactivates the existing consent

This means the integration only needs to:
1. Define integration settings/secrets keys
2. Create an `IntegrationSyncOutbox` entity and wire it into the registration transaction
3. Build the NSwag-generated Listmonk API client and background processor
4. Add API endpoints and Blazor UI for settings management
5. Enhance the `registration.created` webhook payload

## Architecture Decisions Made

| Decision | Rationale |
|---|---|
| Sync outbox inside registration transaction | Durability — consent service runs after transaction, so outbox must be in-transaction |
| Specialized `IntegrationSyncOutbox` entity | Type safety, integration-specific fields, follows existing pattern |
| Follow generated Tolgee/Weblate provider pattern | Listmonk OpenAPI schema now drives the API client through NSwag |
| Durable worker normalizes API base URL | Worker appends `/api` when needed because generated methods are relative to Listmonk API root |
| Tenant-scoped settings, per-event list ID override | Balance between simplicity and flexibility |
| Webhook payload consent-conditional | Privacy-by-default — PII only when consent granted |

## Files Researched

### Domain
- `Explore.Domain/EventContactShareConsent.cs` — consent entity (EXISTING, COMPLETE)
- `Explore.Domain/EventRegistrationIntent.cs` — registration parent (EXISTING, NO CHANGES NEEDED)
- `Explore.Domain/EventRegistration.cs` — per-session access row (EXISTING, NO CHANGES)
- `Explore.Domain/Event.cs` — event aggregate (EXISTING, metadata for attribs)
- `Explore.Domain/User.cs` + `UserPii.cs` — user with PII (EXISTING, email source)
- `Explore.Domain/Constants/GovernanceSettingKeys.cs` — settings keys (EXISTING, WILL MODIFY)
- `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs` — secret keys (EXISTING, WILL MODIFY)
- `Explore.Domain/Secrets/SecretDefinitionRegistry.cs` — secret catalog (EXISTING, WILL MODIFY)
- `Explore.Domain/Settings/SettingRegistry.cs` — setting definitions (EXISTING, WILL MODIFY)
- `Explore.Domain/WebhookConsumer.cs` — webhook consumer entity (EXISTING, REFERENCE)
- `Explore.Domain/WebhookEndpoint.cs` — webhook endpoint entity (EXISTING, REFERENCE)
- `Explore.Domain/WebhookEventType.cs` — webhook event type (EXISTING, REFERENCE)

### Application
- `Explore.Application/Contracts/Services/IContactShareConsentService.cs` — consent contract (EXISTING, COMPLETE)
- `Explore.Application/Services/ContactShareConsentService.cs` — consent implementation (EXISTING, COMPLETE)
- `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs` — handler (EXISTING, WILL MODIFY)
- `Explore.Application/DTOs/EventRegistration/CreateEventRegistrationDto.cs` — DTO with consent fields (EXISTING, COMPLETE)
- `Explore.Application/Contracts/Webhooks/WebhookEventNames.cs` — event names (EXISTING, REFERENCE)
- `Explore.Application/Contracts/Webhooks/IWebhookEventPublisher.cs` — publisher contract (EXISTING, REFERENCE)
- `Explore.Application/Contracts/Webhooks/WebhookContracts.cs` — webhook DTOs (EXISTING, REFERENCE)
- `Explore.Application/Contracts/Webhooks/WebhookEventTypeDescriptor.cs` — event type descriptor (EXISTING, REFERENCE)
- `Explore.Application/Webhooks/WebhookEventTypeRegistry.cs` — event type catalog (EXISTING, WILL MODIFY)
- `Explore.Application/Webhooks/DefaultWebhookPayloadBuilder.cs` — payload builder (EXISTING, REFERENCE)

### Infrastructure
- `Explore.Infrastructure/Webhooks/WebhookEndpointSafetyPolicy.cs` — URL validation (EXISTING, WILL REUSE)
- `Explore.Infrastructure/InfrastructureServicesRegistration.cs` — DI registration (EXISTING, WILL MODIFY)

### Documentation
- `docs/ARCHITECTURE.md`, `docs/OUTBOX_PATTERN.md`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/API.md` — all researched

## Implementation Summary

Implemented backend/generated-client scope:
- Added Listmonk governance settings and secret catalog entries.
- Added `IntegrationSyncOutbox` with EF mapping, repository, tenant filters, retry fields, and migration `20260709184001_AddIntegrationSyncOutbox`.
- Wired event registration so a consented, verified attendee can enqueue a Listmonk sync row inside the existing serializable registration transaction.
- Added `Explore.Infrastructure/nswag.listmonk.json` and generated `Explore.Infrastructure/Integrations/Listmonk/Generated/ListmonkApiClient.g.cs` from `schemas/openapi-listmonk.yaml`.
- Added `ListmonkSyncService`, `IntegrationSyncDrainService`, and API hosted processor plumbing to call generated `CreateSubscriberAsync(...)` with Basic Auth and outbox retry/dead-letter handling.

Remaining scope:
- API/Blazor integration settings management UI.
- Listmonk health check.
- `registration.created` webhook payload schema enhancement.
- Broader infrastructure-level tests around Listmonk HTTP failure classification.

## Validation Results

- Baseline build before edits: `dotnet build --configuration Release --verbosity quiet` passed with pre-existing warnings.
- Application unit tests: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed, 2,091/2,091.
- NSwag generation: `dotnet nswag run nswag.listmonk.json` from `Explore.Infrastructure` passed against `schemas/openapi-listmonk.yaml`.
- Final Release build after migration/doc updates: `dotnet build --configuration Release --verbosity quiet` passed with 56 warnings and 0 errors.
- Architecture tests: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed, 262 succeeded / 1 existing skip.
- Persistence integration tests: `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed, 259/259.

## Files Changed

Key files changed or added:
- `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs`
- `Explore.Domain/Settings/Definitions/IntegrationSettingDefinitions.cs`
- `Explore.Domain/Secrets/SecretDefinitionRegistry.cs`
- `Explore.Domain/IntegrationSyncOutbox.cs`
- `Explore.Application/Contracts/Persistence/IIntegrationSyncOutboxRepository.cs`
- `Explore.Application/Contracts/Services/IListmonkRegistrationSyncOutboxFactory.cs`
- `Explore.Application/Contracts/Services/IIntegrationSyncDrainService.cs`
- `Explore.Application/Services/ListmonkRegistrationSyncOutboxFactory.cs`
- `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs`
- `Explore.Persistence/Repositories/EventRegistrationIntentRepository.cs`
- `Explore.Persistence/Repositories/IntegrationSyncOutboxRepository.cs`
- `Explore.Persistence/Configurations/Entities/IntegrationSyncOutboxConfiguration.cs`
- `Explore.Persistence/Migrations/20260709184001_AddIntegrationSyncOutbox.cs`
- `Explore.Infrastructure/nswag.listmonk.json`
- `Explore.Infrastructure/Integrations/Listmonk/Generated/ListmonkApiClient.g.cs`
- `Explore.Infrastructure/Integrations/Listmonk/ListmonkSyncService.cs`
- `Explore.Infrastructure/IntegrationSyncDrainService.cs`
- `Explore.Infrastructure/IntegrationSyncProcessorSettings.cs`
- `Explore.API/BackgroundServices/IntegrationSyncHostedDrainRunner.cs`
- `Explore.API/BackgroundServices/IntegrationSyncProcessor.cs`

## Next Step

Finish final build verification, then continue with API/Blazor settings endpoints and webhook payload enhancement in a later slice.

## Handoff Notes

If another agent picks up this work:
1. Read `listmonk-integration-plan.md` first
2. The consent model is COMPLETE — do not create new consent entities or modify `EventRegistrationIntent`
3. The registration handler at line 204-224 shows how consent currently works — integration sync should go INSIDE the transaction (before line 172), not after consent processing
4. Follow the `EmailDispatchOutbox` pattern for any future `IntegrationSyncOutbox` lifecycle changes.
5. Listmonk API calls must go through the NSwag-generated client from `Explore.Infrastructure/nswag.listmonk.json`; rerun NSwag after schema updates and restore the mandatory ABOUTME header.
