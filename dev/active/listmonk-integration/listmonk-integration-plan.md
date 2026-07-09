<!-- ABOUTME: Repository-grounded implementation plan for Listmonk native direct integration and outgoing webhook exposure. -->
<!-- ABOUTME: Extends existing webhook infrastructure and consent architecture for marketing-list subscriber sync. -->

# Listmonk Integration — Implementation Plan

Last Updated: 2026-07-09 Europe/Brussels

## 0. Planning Metadata
- **Request:** Build a Listmonk integration that allows event organizers to sync attendee emails to their self-hosted Listmonk instance when attendees opt-in, with native direct API push and outgoing webhook exposure for power users.
- **Task directory:** `dev/active/listmonk-integration/`
- **Planning status:** Draft — awaiting user approval
- **Matched intents:** No exact intent match in `.claude/contract/intents.yaml` for a full integration feature spanning Domain → Application → Persistence → Infrastructure → API → Blazor. Uses a fallback dev-docs contract. Each implementation slice must reclassify against intents (likely `add-cqrs-handler`, `add-write-endpoint`, `add-get-endpoint`, `add-ef-migration`, `add-hal-link`, `blazor-component-affordance`, and `external-infrastructure-bootstrap`).
- **Relevant skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, `outbox-pattern`, `error-tracking`
- **Relevant rules:** `.claude/rules/api-controllers.md`, `.claude/rules/application-layer.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/efcore-migrations.md`, `.claude/rules/blazor-server.md`, `.claude/rules/blazor-client.md`, `.claude/rules/tests.md`
- **Primary layers touched:** Domain / Application / Persistence / Infrastructure / API / Blazor / Docs
- **Estimated complexity:** L — cross-layer feature touching 6 architectural layers, new domain entities, new outbox dispatcher, new Infrastructure HTTP client, new API endpoints, new Blazor settings UI, secrets management integration, and tenant-scoped governance settings. However, most patterns already exist in the codebase (Coop/Osprey provider pattern, webhook infrastructure, governance settings, secrets registry, **and the consent model is fully implemented**).

---

## 1. Executive Summary

ISLAMU Event already has:

- A **rich webhook infrastructure** (Svix + Local providers, event type catalog, consumer/endpoint/subscription management, delivery drain, retry/dead-letter).
- A **complete contact-sharing consent model** — `EventContactShareConsent` entity, `IContactShareConsentService`, and the registration handler (`CreateEventRegistrationCommandHandler`) already processes `ShareEmailWithOrganizer` during registration, creating durable per-organizer consent records with email snapshots.
- A **notification ownership architecture** (notification intent/delivery/delegation audit) with a marketing category.

The Listmonk integration builds on these to provide:

1. **Native Direct Integration** — organizers configure a Listmonk instance URL + API key in tenant/event settings; when an attendee registers and opts in (existing consent flow), a background worker pushes the subscriber to the Listmonk API with event metadata as custom attributes.
2. **Outgoing Webhook Exposure** — the existing `registration.created` webhook event type is enhanced to conditionally include attendee contact data when consent is granted, so power users can route it to Listmonk, Lemlist, or any tool via their own middleware.

### What already exists (no work needed)

| Component | File(s) | Status |
|---|---|---|
| Consent entity | [EventContactShareConsent.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/EventContactShareConsent.cs) | ✅ Complete |
| Consent service | [ContactShareConsentService.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/Services/ContactShareConsentService.cs) | ✅ Complete |
| Consent contract | [IContactShareConsentService.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/Contracts/Services/IContactShareConsentService.cs) | ✅ Complete |
| Registration DTO consent fields | [CreateEventRegistrationDto.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/DTOs/EventRegistration/CreateEventRegistrationDto.cs) — `ShareEmailWithOrganizer`, `ConsentTextAcknowledged`, `ConsentUiVersion` | ✅ Complete |
| Handler consent wiring | [CreateEventRegistrationCommandHandler.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs#L204-L224) — calls `_consentService.ProcessRegistrationConsent()` | ✅ Complete |
| Consent export entities | `EventContactShareExport.cs`, `EventContactShareExportItem.cs` | ✅ Complete |
| Webhook event catalog | [WebhookEventNames.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/Contracts/Webhooks/WebhookEventNames.cs) — `registration.created` registered | ✅ Complete |
| Webhook delivery infrastructure | `WebhookDeliveryDrainService.cs`, Local/Svix/Runtime providers | ✅ Complete |
| Governance settings cascade | `HierarchicalSettingsResolver`, `SettingRegistry`, `GovernanceSettingKeys` | ✅ Complete |
| Secrets management | `SecretDefinitionRegistry`, `SecretBinding`, Infisical integration | ✅ Complete |

### What needs to be built

| Component | Description |
|---|---|
| Integration governance setting keys | `GovernanceSettingKeys.Integrations.Listmonk.*` — enable/disable, instance URL, default list ID, preconfirm |
| Integration secret keys | `InfrastructureSecretSettingKeys.Integrations.Listmonk.*` — API username, API key |
| Setting definitions | `IntegrationSettingDefinitions` in `SettingRegistry` |
| Integration sync outbox | New specialized outbox entity + repository + EF config + migration |
| Outbox→Listmonk wiring | After consent is granted in registration, create an `IntegrationSyncOutbox` row in the same transaction |
| Application sync contract | `IIntegrationSyncService` interface in Application layer |
| Listmonk HTTP client | Infrastructure service implementing `IIntegrationSyncService` |
| Integration sync processor | Background service draining the outbox |
| API endpoints | CQRS handlers + controller for integration settings CRUD + connection test |
| Webhook payload enhancement | Add consent-conditional attendee data to `registration.created` descriptor |
| Blazor integration settings UI | Settings page for configuring Listmonk |
| Documentation | Architecture, Configuration, Secrets, API, Outbox docs |

### Explicitly out of scope

- Lemlist integration (future, same pattern)
- Inbound sync from Listmonk → ISLAMU (unsubscribe sync is boundary-violating per product philosophy)
- Listmonk campaign creation/management from ISLAMU Event
- Email template management in Listmonk from ISLAMU Event
- Custom webhook transformers/adapters built into the platform

---

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence |
|---|---|---|
| Full webhook infrastructure exists | `Explore.Domain/Webhook*.cs` — 7 domain entities, Svix+Local providers | High |
| `registration.created` webhook event type registered | [WebhookEventNames.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/Contracts/Webhooks/WebhookEventNames.cs#L14) | High |
| Registration webhook payload currently has NO attendee data | [WebhookEventTypeRegistry.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/Webhooks/WebhookEventTypeRegistry.cs#L67-L74) — only `registrationId`, `eventId`, `status` | High |
| **Contact share consent model already exists** | [EventContactShareConsent.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/EventContactShareConsent.cs) — full entity with email snapshot, purpose code, status lifecycle | High |
| **Consent service already processes registration opt-in** | [ContactShareConsentService.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/Services/ContactShareConsentService.cs#L43-L139) — `ProcessRegistrationConsent()` creates/reactivates per-organizer consent | High |
| **Registration handler already calls consent service** | [CreateEventRegistrationCommandHandler.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs#L204-L224) — `if (dto.ShareEmailWithOrganizer) { _consentService.ProcessRegistrationConsent(...) }` | High |
| **Registration DTO already has consent fields** | [CreateEventRegistrationDto.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/DTOs/EventRegistration/CreateEventRegistrationDto.cs#L40-L52) — `ShareEmailWithOrganizer`, `ConsentTextAcknowledged`, `ConsentUiVersion` | High |
| No Listmonk references in codebase | Search: `Listmonk`, `listmonk` → 0 results | High |
| No integration governance/secret keys exist | No `GovernanceSettingKeys.Integrations` or `InfrastructureSecretSettingKeys.Integrations` sections | High |
| Governance settings system has 20+ categories | [SettingRegistry.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/Settings/SettingRegistry.cs) — code-defined, compile-time validated | High |
| Secret definition registry follows Infisical folder layout | [SecretDefinitionRegistry.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/Secrets/SecretDefinitionRegistry.cs#L1-L50) — `api`, `storage`, `keycloak`, `cerbos`, `postgresql`, `smtp`, `analytics`, `ai` folders | High |
| Outbox pattern with 3+ specialized variants | `OutboxMessage`, `EmailDispatchOutbox`, `PolicyChangeOutbox`, `PdsSyncOutbox` | High |
| Outbox processor pattern standardized | `OutboxProcessor`, `PdsSyncWorker`, `EmailDispatchProcessor` BackgroundServices | High |
| Coop/Osprey reporting provider pattern exists | `GovernanceSettingKeys.Reporting.*`, `InfrastructureSecretSettingKeys.Reporting.*`, `CoopProviderOptions` | High |
| 16 background services already registered | `Explore.API/BackgroundServices/` | High |
| User PII in separate table | [UserPii.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/UserPii.cs) — `Email`, `FirstName`, `LastName` | High |
| Event entity has all metadata needed for Listmonk attribs | [Event.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/Event.cs) — `Title`, `Slug`, `FirstSessionStartUtc`, `EventTimeZoneId` | High |
| Webhook endpoint safety policy exists | `WebhookEndpointSafetyPolicy.cs` — IP/URL validation for SSRF prevention | High |
| Webhook payload uses descriptor-driven allow-list | [DefaultWebhookPayloadBuilder.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/Webhooks/DefaultWebhookPayloadBuilder.cs) — only fields in descriptor are included | High |
| Tenant delegation locks pattern exists | [GovernanceSettingKeys.TenantDelegation](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/Constants/GovernanceSettingKeys.cs#L286-L297) — `LockSmtp`, `LockStorage`, etc. | High |

### 2.2 Unknowns After Investigation

| Unknown | Resolution Plan |
|---|---|
| Whether consent row creation is inside the same DB transaction as registration | [Handler code](file:///home/amir/ISLAMU/Github/Event/Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs#L204-L224) shows consent is called **after** `CreateWithChildrenAndCapacityAsync` returns — it's a separate DB call. Integration sync outbox row should be created **inside** the transaction for atomicity. This is a design decision. |
| Whether Listmonk supports health/ping endpoint | Web search confirmed: GET `/api/health` available. |
| Exact ReportingSettingDefinitions pattern for new definitions | Need to read one existing `*SettingDefinitions.cs` file during implementation to follow the exact pattern. |

---

## 3. Architecture & Design Decisions

### 3.1 Integration Sync Trigger Point

> **Decision:** Create the `IntegrationSyncOutbox` row inside the same transaction as the registration intent, alongside the existing `EmailDispatchOutbox` row — NOT after the consent service call.

**Why:** The consent service currently runs _after_ the atomic transaction (lines 204-224 in the handler). If we piggyback on the consent call, a server crash between transaction commit and consent processing would lose the sync intent. By putting the integration sync outbox row inside `CreateWithChildrenAndCapacityAsync`, we get transactional durability.

**Data flow:**
```
Registration Handler
  │
  ├─ [Inside Transaction] ──────────────────────────────────────────┐
  │   1. Create EventRegistrationIntent                            │
  │   2. Create EventRegistration child rows                       │
  │   3. Create EmailDispatchOutbox (existing)                     │
  │   4. ★ NEW: Create IntegrationSyncOutbox (if consent=true      │
  │      AND Listmonk integration enabled for this tenant/event)   │
  └──────────────────────────────────────────────────────────────────┘
  │
  ├─ [After Transaction]
  │   5. Enqueue notification intent (existing)
  │   6. Process contact share consent (existing)
```

**Alternative considered:** Create outbox row in the consent service itself — rejected because the consent service runs outside the registration transaction.

### 3.2 Integration Sync Outbox (Specialized Variant)

> **Decision:** Create a new `IntegrationSyncOutbox` entity following the `EmailDispatchOutbox` / `PdsSyncOutbox` pattern.

**Why:** Integration sync is a durable intent with retry/dead-letter semantics. Using the general `OutboxMessage` would require overloading `EventType` strings and lose integration-specific fields (list ID, subscriber data snapshot, integration kind).

**Entity fields:** `Id` (UUIDv7), `TenantId`, `EventId`, `UserId`, `IntegrationKind` (enum), `Status` (Pending/Processing/Completed/Failed/DeadLettered), `SubscriberPayloadJson` (email + name + event attribs snapshot), `ListmonkListId`, `AttemptCount`, `MaxAttempts`, `NextAttemptAt`, `LastError`, `CompletedAt`, `DeadLetteredAt`, `ProcessingLeaseToken`, `CorrelationId`, `CreatedAt`.

### 3.3 Integration Provider Pattern (Following Coop/Osprey)

> **Decision:** Model Listmonk as an integration provider following the existing Coop/Osprey reporting provider pattern.

**Components:**
- `GovernanceSettingKeys.Integrations.Listmonk.*` — configuration keys
- `InfrastructureSecretSettingKeys.Integrations.Listmonk.*` — credential keys  
- `IntegrationSettingDefinitions` — setting definitions in `SettingRegistry`
- `GovernanceSettingKeys.TenantDelegation.LockIntegrations` — tenant delegation lock
- `ListmonkSyncService` in Infrastructure — HTTP client implementing Application contract
- `IntegrationSyncProcessor` — BackgroundService draining the outbox

### 3.4 Webhook Payload Enhancement

> **Decision:** Add consent-conditional fields to `registration.created` webhook descriptor. Bump schema version to 2.

**New fields (all optional):**
- `consentToEmailShare` (boolean, required) — always present
- `attendeeEmail` (string, optional) — only when consent is true
- `attendeeFirstName` (string, optional) — only when consent is true
- `attendeeLastName` (string, optional) — only when consent is true

The `DefaultWebhookPayloadBuilder` already strips fields not in the descriptor, and the `WebhookEventBuildContext.Data` dictionary is populated by the caller. The caller (outbox dispatcher for `registration.created` events) must conditionally include attendee PII based on the consent flag.

### 3.5 Settings Scope

> **Decision:** Integration settings are tenant-scoped with optional per-event override for the list ID.

The Listmonk instance URL and credentials are tenant-scoped (an organizer has one Listmonk instance). The list ID can be overridden per-event (different events → different mailing lists). This mirrors how SMTP settings are tenant-scoped but individual emails are event-specific.

### 3.6 SSRF Prevention

> **Decision:** Apply `WebhookEndpointSafetyPolicy` URL validation at two points: (1) when the organizer saves the Listmonk URL, (2) when the sync processor makes the HTTP call.

The existing `WebhookEndpointSafetyPolicy` validates URLs against private IP ranges and blocked patterns. The same safety policy must be applied to Listmonk URLs since they are organizer-supplied and the platform initiates HTTP requests to them.

---

## 4. Non-Negotiable Constraints

- Repositories return **entities**, never DTOs; mapping happens in handlers.
- Validators are **manually instantiated** in handlers/services (not injected as `IValidator<T>`).
- GET endpoints = `[AllowAnonymous]`; write endpoints = `[Authorize]`.
- UI action affordances gated by **HAL links**, not local role checks.
- Tenant isolation via EF global query filters.
- **Integration sync failures must never break the attendee's registration flow.**
- Attendee email sharing requires explicit opt-in (already implemented via `ShareEmailWithOrganizer`).
- API keys stored in `SecretDefinitionRegistry` (Infisical-backed), never in plain governance settings.
- Listmonk unsubscribes do **not** propagate back to ISLAMU Event (boundary rule).
- Outbox pattern: handlers create durable intent only; actual HTTP calls happen in background workers.
- All new files start with two `ABOUTME:` comment lines.

---

## 5. Implementation Phases

### Phase 1: Domain Foundation — Integration Settings & Secret Keys
**Goal:** Define governance keys, secret keys, and setting definitions for Listmonk integration.  
**Depends on:** Nothing  
**Effort:** S

#### Task 1.1: Add Integration Governance Setting Keys
- **Modify:** [GovernanceSettingKeys.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/Constants/GovernanceSettingKeys.cs)
- Add `Integrations.Listmonk` nested class with: `Enabled`, `InstanceUrl`, `DefaultListId`, `PreconfirmSubscriptions`, `SyncOnRegistration`
- Add `TenantDelegation.LockIntegrations` key

#### Task 1.2: Add Integration Secret Setting Keys
- **Modify:** [InfrastructureSecretSettingKeys.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs)
- Add `Integrations.Listmonk` section: `ApiUsername`, `ApiKey`
- **Modify:** [SecretDefinitionRegistry.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/Secrets/SecretDefinitionRegistry.cs)
- Register both keys with `Instance | Tenant` scope, Infisical `/integrations` folder

#### Task 1.3: Add Integration Setting Definitions
- **Create:** `Explore.Domain/Settings/Definitions/IntegrationSettingDefinitions.cs`
- Define setting definitions following the existing pattern (e.g., [ReportingSettingDefinitions.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/Settings/Definitions/ReportingSettingDefinitions.cs))
- **Modify:** [SettingRegistry.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/Settings/SettingRegistry.cs) — add `IntegrationSettingDefinitions.All`

**Verification:** `dotnet build --configuration Release --verbosity quiet`

---

### Phase 2: Integration Sync Outbox Entity & Persistence
**Goal:** Create the `IntegrationSyncOutbox` specialized outbox entity, repository, EF configuration, and migration.  
**Depends on:** Nothing (parallel with Phase 1)  
**Effort:** M

#### Task 2.1: Create IntegrationSyncOutbox Domain Entity
- **Create:** `Explore.Domain/IntegrationSyncOutbox.cs`
- Entity with: `Id` (Guid/UUIDv7), `TenantId`, `EventId`, `UserId`, `IntegrationKind` (enum: `Listmonk = 1`), `Status` (enum: `Pending=1, Processing=2, Completed=3, Failed=4, DeadLettered=5`), `SubscriberPayloadJson`, `ListmonkListId` (int?), `AttemptCount`, `MaxAttempts`, `NextAttemptAt`, `LastError`, `CompletedAt`, `DeadLetteredAt`, `ProcessingLeaseToken` (Guid?), `CorrelationId` (Guid), `CreatedAt`, `CreatedBy`
- Implements `ITenantEntity`, `IAuditableEntity`
- Follow [EmailDispatchOutbox.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/EmailDispatchOutbox.cs) pattern

#### Task 2.2: Create Repository Interface
- **Create:** `Explore.Application/Contracts/Persistence/IIntegrationSyncOutboxRepository.cs`
- Methods: `Create`, `GetPendingBatchAsync`, `TryMarkAsProcessingAsync` (returns bool, optimistic concurrency), `MarkAsCompletedAsync`, `MarkAsFailedAsync`, `MarkAsDeadLetteredAsync`, `DeleteCompletedOlderThanAsync`

#### Task 2.3: Create Repository Implementation
- **Create:** `Explore.Persistence/Repositories/IntegrationSyncOutboxRepository.cs`
- `TryMarkAsProcessingAsync` uses `ProcessingLeaseToken` for optimistic concurrency

#### Task 2.4: Create EF Configuration & Migration
- **Create:** `Explore.Persistence/Configurations/Entities/IntegrationSyncOutboxConfiguration.cs`
- Tenant query filter, indexes: WorkerPoll (`Status`, `NextAttemptAt`, `CreatedAt`), Tenant (`TenantId`), Correlation (`CorrelationId`)
- **Create:** Migration in `Explore.Persistence/Migrations/`

**Verification:** `dotnet build`, `dotnet ef migrations list`

---

### Phase 3: Application Layer — Sync Contract & Registration Wiring
**Goal:** Define the Application sync contract and wire the registration handler to create integration sync outbox rows.  
**Depends on:** Phase 1, Phase 2  
**Effort:** M

#### Task 3.1: Define Application Sync Contracts
- **Create:** `Explore.Application/Contracts/Integrations/IIntegrationSyncService.cs`
- Method: `Task<IntegrationSyncResult> SyncSubscriberAsync(IntegrationSyncRequest request, CancellationToken ct)`
- **Create:** `Explore.Application/Contracts/Integrations/IntegrationSyncContracts.cs`
- `IntegrationSyncRequest` record: `SubscriberEmail`, `SubscriberFirstName`, `SubscriberLastName`, `EventId`, `EventTitle`, `EventSlug`, `EventStartUtc`, `ListmonkInstanceUrl`, `ListmonkListId`, `PreconfirmSubscriptions`
- `IntegrationSyncResult` record: `Succeeded`, `IsRetryable`, `FailureCategory`, `SafeDetail`

#### Task 3.2: Create Integration Settings Resolution Service
- **Create:** `Explore.Application/Contracts/Services/IIntegrationSettingsResolver.cs`
- Method: `Task<ListmonkIntegrationSettings?> ResolveListmonkSettingsAsync(Guid tenantId, Guid? eventId, CancellationToken ct)`
- Returns null when integration is disabled or not configured
- **Create:** `Explore.Application/Services/IntegrationSettingsResolver.cs` (implementation using `IHierarchicalSettingsResolver`)

#### Task 3.3: Wire Registration Handler to Create Outbox Row
- **Modify:** [CreateEventRegistrationCommandHandler.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs)
- After building the `emailDispatchOutbox` row (line 162-170), add logic: if `dto.ShareEmailWithOrganizer == true` AND Listmonk integration is enabled for the tenant/event, create an `IntegrationSyncOutbox` row with subscriber data snapshot
- Pass the outbox row into `CreateWithChildrenAndCapacityAsync` alongside the email dispatch outbox (may need to modify the repository method signature to accept an optional `IntegrationSyncOutbox` parameter)
- **Modify:** `IEventRegistrationIntentRepository.CreateWithChildrenAndCapacityAsync` — add optional `IntegrationSyncOutbox?` parameter

**Verification:** `dotnet test --project Event.Application.UnitTests`

---

### Phase 4: Infrastructure — Listmonk HTTP Client & Sync Processor
**Goal:** Implement the HTTP client for Listmonk API calls and the background processor draining the outbox.  
**Depends on:** Phase 2, Phase 3  
**Effort:** M-L

#### Task 4.1: Create ListmonkSyncService
- **Create:** `Explore.Infrastructure/Integrations/ListmonkSyncService.cs`
- Implements `IIntegrationSyncService`
- Uses `IHttpClientFactory` named client `"Listmonk"`
- Basic Auth header: `Authorization: Basic base64(apiUsername:apiKey)`
- POST `{instanceUrl}/api/subscribers` with JSON:
  ```json
  {
    "email": "attendee@example.com",
    "name": "First Last",
    "lists": [listId],
    "attribs": {
      "event_title": "...",
      "event_slug": "...",
      "event_start_utc": "...",
      "event_id": "...",
      "registered_at": "...",
      "source": "islamu_event"
    },
    "status": "enabled",
    "preconfirm_subscriptions": true/false
  }
  ```
- HTTP response handling: 200/201 → success, 409 → idempotent success, 4xx → non-retryable failure, 5xx → retryable failure
- **SSRF:** Validate URL with `WebhookEndpointSafetyPolicy` before HTTP call
- **Logging:** No PII in logs — log `EventId`, `IntegrationSyncKind`, not email

#### Task 4.2: Create IntegrationSyncProcessor BackgroundService
- **Create:** `Explore.Infrastructure/Integrations/IntegrationSyncProcessor.cs`
- **Create:** `Explore.Infrastructure/Integrations/IntegrationSyncProcessorSettings.cs`
- Polls `IIntegrationSyncOutboxRepository.GetPendingBatchAsync`
- Claims rows via `TryMarkAsProcessingAsync` (optimistic concurrency)
- Routes by `IntegrationSyncKind`: `Listmonk → ListmonkSyncService`
- Retry: exponential backoff `InitialRetryDelay × 2^attemptCount`, capped at `MaxRetryDelay`
- Dead-letter after `MaxAttempts` (default 5)
- Configurable: `PollingIntervalSeconds`, `BatchSize`, `MaxAttempts`, `InitialRetryDelaySeconds`, `MaxRetryDelaySeconds`

#### Task 4.3: Health Check
- **Create:** `Explore.Infrastructure/HealthChecks/ListmonkIntegrationHealthCheck.cs`
- Returns `Degraded` (not `Unhealthy`) on failure — Listmonk is optional
- Calls GET `{instanceUrl}/api/health`
- Skips when integration is disabled

#### Task 4.4: DI Registration
- **Modify:** [InfrastructureServicesRegistration.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Infrastructure/InfrastructureServicesRegistration.cs)
- Register `ListmonkSyncService` as `IIntegrationSyncService`
- Configure named HttpClient with timeout and resilience policies
- Register health check
- **Modify:** API `Program.cs` — register `IntegrationSyncProcessor` as hosted service

**Verification:** `dotnet build`, unit tests for HTTP client and processor

---

### Phase 5: API Layer — Integration Settings Endpoints
**Goal:** CQRS handlers and controller for integration settings management.  
**Depends on:** Phase 1, Phase 3  
**Effort:** M

#### Task 5.1: CQRS Handlers
- **Create:** `Explore.Application/Features/Integrations/Requests/Queries/GetIntegrationSettingsRequest.cs`
- **Create:** `Explore.Application/Features/Integrations/Handlers/Queries/GetIntegrationSettingsRequestHandler.cs`
- **Create:** `Explore.Application/Features/Integrations/Requests/Commands/UpdateIntegrationSettingsCommand.cs`
- **Create:** `Explore.Application/Features/Integrations/Handlers/Commands/UpdateIntegrationSettingsCommandHandler.cs`
- **Create:** `Explore.Application/Features/Integrations/Requests/Commands/TestListmonkConnectionCommand.cs`
- **Create:** `Explore.Application/Features/Integrations/Handlers/Commands/TestListmonkConnectionCommandHandler.cs`
- GET reads non-secret settings via governance resolver; returns `apiKeyConfigured: true/false` (never the actual key)
- PUT validates URL format, list ID > 0, applies SSRF check on URL
- Test connection calls Listmonk API health endpoint via sync service

#### Task 5.2: API Controller
- **Create:** `Explore.API/Controllers/IntegrationSettingsController.cs`
- Route: `api/integrations/settings`
- GET: `[AllowAnonymous]` (returns non-secret config only)
- PUT: `[Authorize]`
- POST `test-connection`: `[Authorize]`
- HAL links for affordance gating
- Route names in `RouteNames.cs`
- `[EndpointClassification]` on each action

**Verification:** `dotnet test --project Event.API.IntegrationTests`, `dotnet test --project Event.Architecture.Tests`

---

### Phase 6: Webhook Payload Enhancement
**Goal:** Enhance `registration.created` webhook payload with consent-conditional attendee data.  
**Depends on:** Nothing (parallel)  
**Effort:** S-M

#### Task 6.1: Update Webhook Event Type Descriptor
- **Modify:** [WebhookEventTypeRegistry.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/Webhooks/WebhookEventTypeRegistry.cs#L67-L74)
- Add to `registration.created` descriptor:
  - `consentToEmailShare` (boolean, required) — always present
  - `attendeeEmail` (string, **optional**) — only when consent true
  - `attendeeFirstName` (string, **optional**) — only when consent true
  - `attendeeLastName` (string, **optional**) — only when consent true
- Bump `SchemaVersion` to 2

#### Task 6.2: Update Registration Webhook Event Publishing
- Find where `registration.created` webhook events are published (likely in the outbox dispatcher or registration handler)
- Ensure `WebhookEventBuildContext.Data` dictionary includes the new fields, with attendee PII conditional on consent status
- The existing `DefaultWebhookPayloadBuilder` will automatically strip any fields not in the descriptor

**Verification:** `dotnet test` for webhook-related tests

---

### Phase 7: Blazor UI — Integration Settings Page
**Goal:** Blazor UI for configuring Listmonk integration and the registration consent checkbox (if not already present).  
**Depends on:** Phase 5  
**Effort:** M

#### Task 7.1: Integration Settings Page
- **Create:** Integration settings page/component in Blazor
- Listmonk toggle (enable/disable)
- Instance URL field with validation
- API Username + API Key fields (masked, with "configured" indicator)
- Default List ID field
- Preconfirm subscriptions toggle
- "Test Connection" button with visual feedback (success/failure toast)
- HAL-link gated edit affordances

#### Task 7.2: Verify Registration Consent Checkbox
- **Verify** that the Blazor registration form already renders the `ShareEmailWithOrganizer` checkbox
- If not present in UI, add it with proper labeling: "Allow the event organizer to add my email to their mailing list for event updates"

**Verification:** Manual testing, `dotnet test --project Explore.Blazor.Client.Tests` (if exists)

---

### Phase 8: Documentation & Final Testing
**Goal:** Update canonical docs, write integration tests, ensure all architecture tests pass.  
**Depends on:** All previous phases  
**Effort:** M

#### Task 8.1: Update Documentation
- `docs/ARCHITECTURE.md` — add IntegrationSyncProcessor to background services, integration sync section
- `docs/OUTBOX_PATTERN.md` — add IntegrationSyncOutbox to specialized variants table
- `docs/CONFIGURATION.md` — add integration settings
- `docs/SECRETS.md` — add integration secret keys
- `docs/API.md` — add integration settings endpoints
- `docs/API_CHANGELOG.md` — document webhook payload enhancement

#### Task 8.2: Integration Tests
- Registration with consent + enabled Listmonk → sync outbox row created
- Registration without consent → no sync outbox row
- Registration with consent + disabled Listmonk → no sync outbox row
- Listmonk HTTP client builds correct payload
- Processor retries on failure, dead-letters after max
- Integration settings CRUD via API

**Verification:** Full test suite per `docs/OPERATIONS.md`

---

## 6. Testing Strategy

| Requirement | Test Type | Project |
|---|---|---|
| Integration sync outbox persistence | Integration | `Event.Persistence.IntegrationTests` |
| Registration handler consent+outbox flow | Unit | `Event.Application.UnitTests` |
| Listmonk HTTP client payload/response handling | Unit | `Event.Infrastructure.Tests` (or new) |
| Integration sync processor lifecycle | Unit | `Event.Infrastructure.Tests` |
| Integration settings CQRS handlers | Unit | `Event.Application.UnitTests` |
| API endpoint contract | Integration | `Event.API.IntegrationTests` |
| Webhook payload consent conditional | Integration | `Event.API.IntegrationTests` |
| Architecture guardrails | Architecture | `Event.Architecture.Tests` |

---

## 7. Security, Privacy & Abuse Considerations

- **API keys:** Stored in `SecretDefinitionRegistry`, never returned in GET responses.
- **Tenant isolation:** `IntegrationSyncOutbox` has EF global query filter on `TenantId`.
- **Privacy:** Email/name only flows when explicit consent granted. Consent is immutable once registration intent created.
- **SSRF:** `WebhookEndpointSafetyPolicy` validates Listmonk URLs at save time and at sync time.
- **Rate limiting:** Settings write endpoints use `write` rate limit. Connection test is rate-limited.
- **Audit trail:** `IntegrationSyncOutbox` rows + `EventContactShareConsent` rows provide full audit.
- **No PII in logs:** Log `EventId`/`UserId`, never `Email`.

---

## 8. Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| SSRF via organizer-supplied URL | Medium | High | `WebhookEndpointSafetyPolicy` at save and sync time |
| Sync backlog during large event | Medium | Medium | Configurable batch size, polling interval |
| Credential rotation / expiry | Medium | Low | Health check detection, dead-letter alerts |
| Transaction enlargement from adding outbox row | Low | Low | Single additional INSERT, minimal overhead |

---

## 9. Success Metrics

- [ ] Organizer configures Listmonk in tenant settings
- [ ] Attendee opts in during registration → appears in Listmonk list within processing interval
- [ ] Attendee opts out → data never leaves ISLAMU boundary
- [ ] Registration succeeds regardless of Listmonk sync status
- [ ] `registration.created` webhook includes consent-conditional attendee data
- [ ] All build + test gates green
- [ ] All canonical docs updated

---

## 10. Implementation Agent Contract

Future agents implementing this plan MUST:
1. Read this plan, `listmonk-integration-context.md`, and `listmonk-integration-tasks.md` before starting.
2. Start from the highest-priority incomplete task.
3. Update all three dev-docs after each task completion.
4. Provide developer teaching summaries (not abstract status lines) to the user.
5. Never break the registration flow for a sync failure.
