<!-- ABOUTME: Implementation plan for provider-based outgoing webhooks and incoming webhook callback handling. -->
<!-- ABOUTME: Captures architecture decisions, phases, validation, and repository constraints for Local and Svix providers. -->

# Webhooks Local/Svix Provider Implementation Plan

Last Updated: 2026-07-02 Europe/Brussels

## Status

- State: Implementation started; Phase 1 Application-layer foundation and Phase 2 canonical Domain/Persistence model are implemented and verified.
- Request: implement outgoing webhooks with `LocalWebhookProvider` and `SvixWebhookProvider`, plus a separate incoming webhook framework.
- Primary planning output: this plan, the companion context file, and the companion task checklist under `dev/active/webhooks-local-svix-provider/`.
- Implemented slice: provider-neutral webhook contracts, canonical event catalog, schema provider, payload builder, canonical webhook domain entities, EF Core mappings, repository contracts/implementations, migration, DI registration, and focused unit/integration tests.
- Backward compatibility: not required. The repository is in development mode, so the implementation should prefer clean architecture over compatibility shims.
- Product posture: self-hostable by default, enterprise-capable when Svix is configured.

## Contribution Contract Classification

No single intent in `.claude/contract/intents.yaml` fully covers a new webhook subsystem. Implementation must compose the following existing intents and obey their `must_read_docs`, `paths_in_scope`, `minimum_tests`, and rule files:

| Workstream | Matching intent | Why it applies |
| --- | --- | --- |
| Domain/Application webhook entities and CQRS handlers | `add-cqrs-handler` | New commands, queries, validators, DTOs, and application services. |
| Persistence model and migrations | `add-ef-migration`, `update-repository-query` | New EF Core entities, configurations, repositories, tenant filters, and migration. |
| Admin and integration API endpoints | `add-get-endpoint`, `add-write-endpoint`, `openapi-contract-change` | Webhook management, retry, test delivery, Svix portal, and incoming callback APIs. |
| HAL affordances | `add-hal-link` | UI actions must be emitted as HAL links, not inferred client-side. |
| Authorization policies | `cerbos-policy-change` | New `webhook:*` actions and local/Cerbos parity. |
| Blazor management UI | `blazor-component-affordance` | Webhook settings, endpoint management, delivery attempts, and portal actions. |

Required rules already loaded for this planning pass: `api-controllers`, `api-hateoas`, `application-layer`, `domain`, `efcore-persistence`, `efcore-migrations`, `blazor-client`, `blazor-server`, and `tests`.

## Current State Report

The repository already has the foundations required for this subsystem:

- `Explore.Domain/OutboxMessage.cs` is the generic transactional outbox for cross-process side effects and already names emails, webhooks, and integrations as intended side-effect classes.
- `Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs` routes known outbox event types and fails closed for unknown event types.
- `Explore.API/BackgroundServices/OutboxProcessor.cs` claims, dispatches, completes, retries, and dead-letters generic outbox rows.
- `EmailDispatchOutbox` is a strong local precedent for a specialized durable side-effect ledger with attempts, receipts, retry state, tenant rebinding, and safe admin views.
- `Explore.API/Controllers/ModerationIntegrationController.cs` already hosts incoming Coop/Osprey callback routes. Coop already reads the raw request body and verifies HMAC signatures with timestamp tolerance and constant-time comparison.
- `Explore.Application/Features/EventReporting/Handlers/Commands/ProcessCoopDecisionCallbackCommandHandler.cs` persists callback decisions idempotently before triggering local moderation behavior.
- `Explore.Application/Services/EventModerationOutboxMessageFactory.cs` already enforces the heavy-redaction rule: heavy moderation notification payloads must not expose unsafe event identity or content.
- `Explore.Application/Telemetry/BusinessMetrics.cs` is the correct place for bounded business counters.
- `Explore.Secrets` provides secret-provider abstractions, but the configuration registry does not yet define a webhook secret namespace.
- `README.md` already promises "Svix-compatible webhooks", but there is no `Svix` package, webhook domain model, webhook API surface, or webhook delivery worker in the codebase today.

One requested bootstrap include, `RTK.md`, was not present at the repository root or in the indexed file list during this planning pass. The canonical repository sources used were `AGENTS.md`, `.github/copilot-instructions.md`, `.claude/contract/intents.yaml`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, and the matching rules and skills.

## Proposed Future State

Implement a provider-based outgoing webhook subsystem:

```text
Domain/Application event
  -> durable generic outbox event after the business transaction commits
  -> WebhookEventPublisher
  -> WebhookEventEnvelope + payload builder
  -> canonical webhook_messages row
  -> IWebhookDeliveryProvider
       -> DisabledWebhookDeliveryProvider
       -> DryRunWebhookDeliveryProvider
       -> LocalWebhookDeliveryProvider
       -> SvixWebhookDeliveryProvider
       -> CompositeWebhookDeliveryProvider
```

Implement incoming callbacks separately:

```text
External provider callback
  -> [AllowAnonymous] or provider-authenticated API endpoint
  -> raw body capture
  -> IIncomingWebhookVerifier
  -> incoming_webhook_messages idempotency row
  -> command/outbox processing
  -> aggregate mutation only after verification and idempotency capture
```

The distinction is non-negotiable:

- Outgoing product webhooks are ISLAMU Event notifying external systems.
- Incoming integration callbacks are external systems notifying ISLAMU Event.
- Coop, Osprey, Svix operational callbacks, payment provider callbacks, and email provider callbacks must not depend on the outgoing provider mode.

## Core Design Decisions

### 1. ISLAMU owns the canonical webhook model

Even when delivery is delegated to Svix, ISLAMU Event must retain canonical webhook records for auditability, provider switching, HAL affordances, retention, tenant isolation, and integration consistency.

The canonical records are:

- `WebhookConsumer`
- `WebhookEventType`
- `WebhookEndpoint`
- `WebhookEndpointSubscription`
- `WebhookMessage`
- `WebhookDeliveryAttempt`
- `WebhookProviderLink`
- `IncomingWebhookMessage`

### 2. LocalProvider is intentionally limited

`LocalWebhookDeliveryProvider` must be useful out of the box, but must not become a Svix clone.

Include in V1:

- Endpoint CRUD.
- Event type subscription filtering.
- Svix-compatible signed POST delivery.
- Basic retry schedule.
- Delivery attempt logs.
- Manual retry.
- Endpoint disable after repeated failures.
- Secret rotation.
- Per-endpoint rate limiting.
- Timeout controls.
- SSRF protection.
- Safe logs, metrics, and health checks.

Exclude from V1:

- Customer-facing advanced app portal.
- Payload transformations.
- OAuth or mTLS endpoint authentication.
- FIFO endpoints.
- Polling endpoints.
- Object-storage delivery.
- Advanced routing rules.
- Zapier connector builder.
- Full analytics dashboard.

### 3. SvixProvider is the advanced delivery backend

`SvixWebhookDeliveryProvider` maps ISLAMU concepts to Svix concepts:

| ISLAMU | Svix |
| --- | --- |
| `WebhookConsumer.Id` or stable UID | Application UID |
| `WebhookEndpoint` | Endpoint, if mirrored or managed through App Portal |
| `WebhookMessage.Id` | `eventId` and idempotency key |
| `WebhookEventType.Name` | `eventType` |
| `WebhookMessage.PayloadJson` | Message payload |

Svix provider behavior:

- Use the official `Svix` C# package.
- Create or sync a Svix application for each consumer.
- Sync event types when configured.
- Create Svix messages with `eventType`, `eventId`, `payload`, and payload retention.
- Send `Idempotency-Key` with `WebhookMessage.Id`.
- Persist provider IDs in `WebhookProviderLink`.
- Generate App Portal URLs only from backend code; never expose the Svix API token to Blazor.

### 4. Delivery is post-commit and outbox-backed

Business handlers must not call HTTP endpoints, Svix, SMTP, RabbitMQ, or provider APIs inside the business transaction.

The implementation should use the existing generic outbox as the post-commit trigger and webhook-specific tables as the durable delivery ledger:

1. Business command commits aggregate state and creates a generic `OutboxMessage` for the domain/application event.
2. `CompositeOutboxMessageDispatcher` dispatches the known event.
3. Webhook routing translates eligible events into `WebhookEventEnvelope` instances.
4. `WebhookEventPublisher` creates `WebhookMessage` rows and either:
   - schedules local `WebhookDeliveryAttempt` rows, or
   - queues provider publication through the configured provider.
5. Local delivery workers or Svix handle endpoint fanout after the canonical message exists.

This mirrors the repository's current separation between generic outbox messages and specialized email dispatch state.

### 5. Local signatures are Svix-compatible

Local delivery must sign using Svix-compatible headers so consumers can reuse Svix verification libraries:

```text
svix-id: webhook message id
svix-timestamp: unix timestamp
svix-signature: v1,{base64_hmac_sha256}
```

Signed content:

```text
{svix-id}.{svix-timestamp}.{raw-body}
```

The signature service must:

- Use HMAC-SHA256.
- Decode the key from the base64 portion after `whsec_`.
- Compare signatures with `CryptographicOperations.FixedTimeEquals`.
- Enforce timestamp tolerance for verification.
- Support current and previous secrets during rotation.

### 6. Heavy moderation payloads stay generic

Sensitive payload minimization is mandatory. `event.heavy_redacted` and equivalent moderation events must not include:

- event title
- event slug
- public URL
- image URI
- storage object keys or paths
- unsafe original content
- provider endpoints
- raw provider errors
- arbitrary moderator free text
- organizer or source actor identity, unless a product decision explicitly allows it

### 7. HAL links drive all UI actions

Blazor must never infer webhook management permissions from roles or claims. It must render actions from HAL links only:

- create endpoint
- update endpoint
- delete or archive endpoint
- rotate secret
- send test event
- retry message
- view delivery attempts
- open Svix App Portal
- manage provider settings

GET routes remain `[AllowAnonymous]` by repository convention, but HAL links and write endpoints must be resource-authorized.

## Provider Modes

| Mode | Behavior |
| --- | --- |
| `Disabled` | No outgoing product webhooks. Incoming provider callbacks still work. |
| `DryRun` | Canonical messages are created, but no outbound HTTP or Svix call is made. Useful for dev/test. |
| `Local` | Built-in PostgreSQL-backed delivery worker sends signed POSTs to local endpoints. |
| `Svix` | ISLAMU creates messages in Svix; Svix handles endpoint fanout, retries, observability, and portal. |
| `Composite` | Canonical local audit plus Svix delivery, with optional local fallback only for explicitly configured internal endpoints. |

Default recommendations:

- Development: `Local`, with `DryRun` available per instance or tenant.
- Single-tenant self-host: `Local` or `Disabled`, controlled by admin setting.
- Multi-tenant SaaS: `Svix` when configured, otherwise `Local`.

## Canonical Data Model

Use `Guid` UUIDv7 primary keys for aggregates and entities, `int` for lookup IDs where applicable, and `long` only for cursors. Repositories return entities, never DTOs.

### `WebhookConsumer`

Represents an integration owner.

Fields:

- `Id` `Guid`
- `TenantId` `Guid`
- `OwnerActorId` `Guid?`
- `OwnerUserId` `Guid?`
- `ConsumerKind` enum: `Tenant`, `Organization`, `Group`, `User`, `SystemIntegration`
- `Name`
- `Status`: `Active`, `Disabled`, `Archived`
- `ProviderMode`: `Inherited`, `Disabled`, `DryRun`, `Local`, `Svix`, `Composite`
- `ExternalProviderAppId`
- audit timestamps

### `WebhookEventType`

Canonical public and internal event catalog.

Fields:

- `Id` `Guid`
- `Name` unique
- `GroupName`
- `Description`
- `SchemaJson` jsonb
- `SchemaVersion` `int`
- `IsPublic`
- `IsEnabled`
- `PayloadRetentionDays`
- audit timestamps

Initial event types:

- `event.created`
- `event.published`
- `event.updated`
- `event.cancelled`
- `event.light_moderated`
- `event.heavy_redacted`
- `registration.created`
- `registration.approved`
- `registration.cancelled`
- `report.created`
- `report.decision_created`
- `organization.verified`

### `WebhookEndpoint`

Authoritative for LocalProvider. For SvixProvider this can be a mirror/cache only when the implementation chooses local visibility over portal-only management.

Fields:

- `Id` `Guid`
- `TenantId` `Guid`
- `ConsumerId` `Guid`
- `Url`
- `Description`
- `Status`: `Active`, `Disabled`, `Failing`, `Archived`
- `SecretRef`
- `SecretVersion`
- `PreviousSecretRef`
- `PreviousSecretValidUntil`
- `ProviderEndpointId`
- `MaxAttempts`
- `TimeoutSeconds`
- `RateLimitPerMinute`
- `LastSuccessAt`
- `LastFailureAt`
- audit timestamps

### `WebhookEndpointSubscription`

Fields:

- `Id` `Guid`
- `TenantId` `Guid`
- `EndpointId` `Guid`
- `EventTypeId` `Guid`
- `IsEnabled`
- `CreatedAt`

### `WebhookMessage`

One row per emitted canonical webhook envelope.

Fields:

- `Id` `Guid`
- `TenantId` `Guid`
- `EventType`
- `EventId`
- `AggregateKind`
- `AggregateId`
- `ConsumerId` `Guid?`
- `PayloadJson` jsonb
- `PayloadHash`
- `PayloadRetentionUntil`
- `ProviderMode`
- `ProviderMessageId`
- `Status`: `Pending`, `Queued`, `Delivered`, `PartiallyFailed`, `Failed`, `Cancelled`
- `CreatedAt`
- `PublishedAt`

### `WebhookDeliveryAttempt`

LocalProvider attempt ledger.

Fields:

- `Id` `Guid`
- `TenantId` `Guid`
- `MessageId` `Guid`
- `EndpointId` `Guid`
- `AttemptNumber`
- `Status`: `Scheduled`, `Sending`, `Succeeded`, `Failed`, `Abandoned`
- `ScheduledAt`
- `SentAt`
- `CompletedAt`
- `HttpStatusCode`
- `FailureCategory`
- `ResponseBodyPreview`
- `DurationMs`
- `NextRetryAt`
- `ProcessingLeaseToken`
- `ProcessingLeaseExpiresAt`
- `CreatedAt`

### `WebhookProviderLink`

External provider object tracking.

Fields:

- `Id` `Guid`
- `TenantId` `Guid`
- `ConsumerId` `Guid?`
- `EndpointId` `Guid?`
- `MessageId` `Guid?`
- `Provider`: `Svix`
- `ExternalAppId`
- `ExternalEndpointId`
- `ExternalMessageId`
- `SyncState`: `Pending`, `Synced`, `Failed`, `Disabled`
- `LastSyncedAt`
- `LastErrorCategory`
- `RetryCount`
- audit timestamps

### `IncomingWebhookMessage`

Idempotency and audit record for external callbacks.

Fields:

- `Id` `Guid`
- `TenantId` `Guid?`
- `Provider`
- `ProviderMessageId`
- `IdempotencyKey`
- `EventType`
- `RawPayloadHash`
- `HeadersJson` jsonb with only safe, allow-listed header names
- `Status`: `Received`, `Rejected`, `Processed`, `Failed`, `Duplicate`
- `FailureCategory`
- `ReceivedAt`
- `ProcessedAt`

Never store raw incoming payloads by default unless a provider-specific retention policy explicitly allows it.

## Application Abstractions

Add these contracts under `Explore.Application/Contracts/Webhooks/` or the nearest existing application contracts namespace:

```csharp
public interface IWebhookEventPublisher
{
    Task PublishAsync(WebhookEventEnvelope envelope, CancellationToken cancellationToken);
}

public interface IWebhookDeliveryProvider
{
    string ProviderName { get; }

    Task<WebhookProviderPublishResult> PublishAsync(
        WebhookProviderMessage message,
        CancellationToken cancellationToken);
}

public interface IWebhookEndpointManager
{
    Task<WebhookEndpointResult> CreateEndpointAsync(
        CreateWebhookEndpointInput input,
        CancellationToken cancellationToken);

    Task<WebhookEndpointResult> UpdateEndpointAsync(
        UpdateWebhookEndpointInput input,
        CancellationToken cancellationToken);

    Task DisableEndpointAsync(Guid endpointId, CancellationToken cancellationToken);
}

public interface IWebhookSignatureService
{
    WebhookSignatureHeaders Sign(
        string messageId,
        DateTimeOffset timestamp,
        string rawPayload,
        WebhookSecretMaterial secret);

    WebhookVerificationResult Verify(
        string rawPayload,
        IReadOnlyDictionary<string, string> headers,
        WebhookSecretMaterial secret);
}

public interface IWebhookPayloadBuilder
{
    Task<WebhookPayloadBuildResult> BuildAsync(
        WebhookEventBuildContext context,
        CancellationToken cancellationToken);
}
```

Incoming callback contracts:

```csharp
public interface IIncomingWebhookVerifier
{
    Task<IncomingWebhookVerificationResult> VerifyAsync(
        IncomingWebhookContext context,
        CancellationToken cancellationToken);
}

public interface IIncomingWebhookHandler
{
    Task<Result> HandleAsync(
        IncomingWebhookMessage message,
        CancellationToken cancellationToken);
}
```

Concrete provider implementations live outside Domain/Application:

- `DisabledWebhookDeliveryProvider`
- `DryRunWebhookDeliveryProvider`
- `LocalWebhookDeliveryProvider`
- `SvixWebhookDeliveryProvider`
- `CompositeWebhookDeliveryProvider`

## API Surface

Use route names in `Explore.API/Hateoas/RouteNames.cs`, endpoint classifications, explicit response metadata, and generated OpenAPI operations.

Public/admin webhook management:

```text
GET    /api/webhooks/event-types
GET    /api/webhooks/consumers
POST   /api/webhooks/consumers
GET    /api/webhooks/endpoints
POST   /api/webhooks/endpoints
GET    /api/webhooks/endpoints/{id}
PUT    /api/webhooks/endpoints/{id}
DELETE /api/webhooks/endpoints/{id}
POST   /api/webhooks/endpoints/{id}/rotate-secret
POST   /api/webhooks/endpoints/{id}/test
GET    /api/webhooks/messages
GET    /api/webhooks/messages/{id}
POST   /api/webhooks/messages/{id}/retry
GET    /api/webhooks/delivery-attempts
POST   /api/webhooks/svix/app-portal
```

Incoming integration callbacks:

```text
POST /api/integrations/svix/operational
POST /api/integrations/coop/callback
POST /api/integrations/osprey/callback
POST /api/integrations/{provider}/callback
```

Incoming routes can be `[AllowAnonymous]` only when the provider cannot send a Keycloak/API token. In that case, provider signature verification is the authentication mechanism. They still need rate limiting, raw body size limits, idempotency, safe ProblemDetails, and no side effects before verification.

## Authorization Actions

Add and document:

- `webhook:view`
- `webhook:create`
- `webhook:update`
- `webhook:delete`
- `webhook:rotate-secret`
- `webhook:test`
- `webhook:retry`
- `webhook:view-delivery`
- `webhook:manage-provider`
- `webhook:open-provider-portal`

Policy intent:

- Instance admin: full webhook authority across instance-level integrations.
- Tenant admin: full authority for tenant-owned consumers and endpoints.
- Organization admin: manage organization-owned consumers and endpoints only if tenant setting allows it.
- Group admin: optional and disabled by default.
- Regular user: no webhook management by default.

Cerbos and local fallback behavior must be equivalent. Unknown webhook resource/action combinations must deny by default.

## Configuration Model

Add strongly typed options with startup validation. Use secret references for provider tokens and endpoint secrets.

Instance settings:

```json
{
  "webhooks": {
    "enabled": true,
    "provider": "Local",
    "allowTenantOverride": true,
    "defaultPayloadRetentionDays": 14,
    "local": {
      "maxAttempts": 8,
      "timeoutSeconds": 15,
      "blockPrivateNetworks": true,
      "allowedPrivateCidrs": []
    },
    "svix": {
      "baseUrl": "https://svix.example.org",
      "authTokenSecretRef": "webhooks/svix/auth-token",
      "appPortalEnabled": true,
      "syncEventTypesOnStartup": true
    }
  }
}
```

Tenant settings:

```json
{
  "webhooks": {
    "enabled": true,
    "provider": "Inherited",
    "maxEndpoints": 10,
    "allowedEventTypes": ["event.*", "registration.*", "report.*"],
    "allowOrgWebhooks": true
  }
}
```

## Security and Privacy Requirements

### SSRF protection

LocalProvider must block private/internal destinations by default:

- `localhost`
- `127.0.0.0/8`
- `::1`
- `10.0.0.0/8`
- `172.16.0.0/12`
- `192.168.0.0/16`
- link-local ranges
- cloud metadata IPs
- internal DNS results

Implementation requirements:

- Validate scheme, host, resolved IP addresses, and redirects before delivery.
- Disable redirects.
- Re-resolve DNS close to connection time or use a guarded connection strategy to reduce DNS rebinding risk.
- Allow private CIDRs only through explicit operator configuration.
- Unit and integration tests must cover hostname, IPv4, IPv6, DNS, metadata IP, and redirect cases.

### Timeout and body limits

Defaults:

- connect timeout: 3 seconds
- total request timeout: 10 to 15 seconds
- max payload size: configurable, small by default
- max response preview: 2 to 4 KB
- redirects: 0

### Logs and metrics

Never log:

- `PayloadJson`
- endpoint secret material
- full endpoint URL query string
- authorization headers
- full response body
- raw exception text from user endpoints

Log only bounded fields:

- tenant id
- event type
- message id
- endpoint id
- attempt number
- status
- failure category
- duration ms
- HTTP status code

### Endpoint secret rotation

Support:

- current secret reference
- previous secret reference
- previous secret validity window
- signing with the current secret
- optional dual-signature verification window for consumers
- explicit rotation audit event

## Observability

Add bounded counters to `Explore.Application/Telemetry/BusinessMetrics.cs`:

- `explore.webhooks.messages_created`
- `explore.webhooks.delivery_attempts`
- `explore.webhooks.delivery_success`
- `explore.webhooks.delivery_failure`
- `explore.webhooks.provider_publish_failure`
- `explore.webhooks.endpoint_disabled`
- `explore.webhooks.incoming_received`
- `explore.webhooks.incoming_rejected`

Allowed metric tags:

- provider mode
- event type group, not unbounded event names if cardinality becomes high
- failure category
- status

Do not tag on URL, full tenant slug, user id, raw exception, or endpoint description.

Add health/readiness checks:

- webhook subsystem enabled/configured
- LocalProvider queue health and oldest scheduled attempt age
- SvixProvider configured/healthy when selected
- secret provider access for configured secret refs
- no readiness failure when provider is `Disabled`

## Implementation Phases

### Phase 0 - Baseline and architectural guardrails

Deliverables:

- Confirm no existing webhook subsystem or Svix package.
- Confirm package management style before adding `Svix`.
- Record current build/test baseline before code changes.
- Optionally add a first-class `webhooks` intent to `.claude/contract/intents.yaml` if the team wants future agents to classify this directly.

Acceptance:

- Baseline command results captured.
- No implementation begins without matching rule files loaded.
- Plan/context/tasks are kept current as implementation proceeds.

Validation:

```bash
dotnet build --configuration Release --verbosity quiet
```

### Phase 1 - Event catalog, envelopes, and payload policy

Deliverables:

- `WebhookEventEnvelope`
- `WebhookEventTypeRegistry`
- `WebhookEventSchemaProvider`
- `WebhookPayloadBuilder`
- event payload DTOs and schema examples
- sensitive moderation payload minimization tests

Likely files:

- `Explore.Application/Contracts/Webhooks/*`
- `Explore.Application/Webhooks/*`
- `Explore.Application/Services/*Webhook*`
- `Event.Application.UnitTests/Webhooks/*`

Acceptance:

- Every event type has name, group, description, version, schema, and example.
- Event type names match Svix-compatible naming.
- Payloads are stable and versioned.
- Heavy moderation payloads are generic and linkless.

Validation:

```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release
```

### Phase 2 - Canonical persistence and repositories

Deliverables:

- Domain entities/enums for webhook consumers, event types, endpoints, subscriptions, messages, delivery attempts, provider links, and incoming messages.
- EF Core configurations, DbSets, migration, indexes, and repository interfaces/implementations.
- Tenant query filters and controlled worker bypass reasons where cross-tenant background workers need queue scans.
- Payload retention fields and cleanup query shape.

Likely files:

- `Explore.Domain/Webhooks/*`
- `Explore.Application/Contracts/Persistence/IWebhook*Repository.cs`
- `Explore.Persistence/Configurations/Entities/Webhooks/*`
- `Explore.Persistence/Repositories/Webhooks/*`
- `Explore.Persistence/ExploreDbContext.DbSets.cs`
- `Explore.Persistence/ExploreDbContext.ModelConfiguration.cs`
- `Explore.Persistence/Migrations/*AddWebhookSubsystem.cs`

Acceptance:

- Repositories return entities, not DTOs.
- Tenant filters protect tenant-owned rows.
- Worker queue scans use documented bypass reasons and never expose data through user APIs.
- Migration has reversible `Down`.
- JSON columns use jsonb.
- Attempt and provider-link indexes support delivery workers and admin queries.

Validation:

```bash
dotnet test --project Event.Persistence.Tests/Event.Persistence.Tests.csproj --configuration Release
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release
```

### Phase 3 - LocalWebhookProvider

Deliverables:

- `LocalWebhookDeliveryProvider`
- `WebhookDeliveryProcessor` or TickerQ-style drain service using PostgreSQL-owned attempt state.
- `WebhookSignatureService`
- `WebhookEndpointSafetyPolicy`
- `WebhookRetryScheduler`
- safe HTTP client configuration
- endpoint auto-disable policy
- delivery health check

Likely files:

- `Explore.Infrastructure/Webhooks/*`
- `Explore.API/BackgroundServices/WebhookDeliveryProcessor.cs`
- `Explore.API/HealthChecks/WebhookDeliveryHealthCheck.cs`
- `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- `Explore.API/Program.cs`
- `Event.Infrastructure.Tests/Webhooks/*`
- `Event.API.IntegrationTests/Webhooks/*`

Acceptance:

- Signed POST works with Svix-compatible headers.
- Only 2xx is success.
- Redirects, non-2xx, timeout, network errors, and safety-policy failures are failures.
- Retry schedule:
  - attempt 1: immediately
  - attempt 2: +30 seconds
  - attempt 3: +5 minutes
  - attempt 4: +30 minutes
  - attempt 5: +2 hours
  - attempt 6: +6 hours
  - attempt 7: +12 hours
  - attempt 8: +24 hours
- Exhausted attempts fail safely.
- Repeated endpoint failure disables or marks endpoint failing according to configured policy.
- Private/internal destinations are blocked by default.
- Delivery logs and metrics are safe.

Validation:

```bash
dotnet test --project Event.Infrastructure.Tests/Event.Infrastructure.Tests.csproj --configuration Release
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release
```

### Phase 4 - Webhook management API, authorization, and HAL

Deliverables:

- Webhook management controllers.
- CQRS commands/queries and manual validators.
- Route names.
- HAL assemblers/link policies.
- Authorization action constants and local/Cerbos policy support.
- OpenAPI contract updates and generated Blazor client updates.

Likely files:

- `Explore.API/Controllers/WebhooksController.cs`
- `Explore.API/Hateoas/RouteNames.cs`
- `Explore.API/Hateoas/*Webhook*`
- `Explore.Application/Features/Webhooks/*`
- `Explore.Authorization/*` or existing authorization provider locations
- `cerbos/**`
- generated client files after OpenAPI regeneration
- `Event.API.IntegrationTests/Webhooks/*`

Acceptance:

- Tenant admin can manage tenant webhooks.
- Organization admin can manage organization-owned webhooks only when tenant settings allow it.
- Unauthorized users receive safe ProblemDetails.
- HAL links appear only when the current actor is authorized.
- Blazor actions can be driven without local role inspection.
- Write routes are `[Authorize]` and rate limited.
- GET routes follow repo convention and are `[AllowAnonymous]`, with resource-sensitive data guarded by query logic and HAL policy.

Validation:

```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release
```

### Phase 5 - SvixProvider

Deliverables:

- Add official `Svix` package according to the repository's package-management convention.
- `SvixWebhookDeliveryProvider`
- `SvixApplicationSyncService`
- `SvixEventTypeSyncService`
- `SvixAppPortalService`
- provider health check
- provider-link persistence
- options and validation

Likely files:

- `Explore.Infrastructure/Webhooks/Svix/*`
- `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- `Explore.API/Controllers/WebhooksController.cs`
- `Explore.API/HealthChecks/SvixWebhookHealthCheck.cs`
- `docs/CONFIGURATION.md`
- `Event.Infrastructure.Tests/Webhooks/Svix*`

Acceptance:

- Svix application is created/mapped using a stable ISLAMU UID.
- Event types sync to Svix when configured.
- Message creation sends `eventType`, `eventId`, `payload`, payload retention, and idempotency key.
- Provider IDs are stored in `WebhookProviderLink`.
- Svix API token is only loaded server-side from a secret reference.
- App Portal URL generation happens only in backend code.
- Svix failures create bounded provider failure state and do not leak token, endpoint URLs with query strings, or raw response bodies.

Validation:

```bash
dotnet test --project Event.Infrastructure.Tests/Event.Infrastructure.Tests.csproj --configuration Release
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release
```

### Phase 6 - Incoming webhook framework

Deliverables:

- Shared incoming webhook abstractions.
- `IncomingWebhookMessage` persistence and idempotency.
- Svix operational webhook verifier.
- Coop callback verifier adapter or refactor to the shared interface.
- Optional Osprey verifier if Osprey moves to signed callbacks.
- Incoming callback dispatcher that queues side effects through commands/outbox instead of direct unverified mutation.

Likely files:

- `Explore.Application/Contracts/Integrations/IncomingWebhooks/*`
- `Explore.API/Controllers/IncomingWebhooksController.cs`
- `Explore.API/Controllers/ModerationIntegrationController.cs`
- `Explore.API/Services/*IncomingWebhook*`
- `Explore.Persistence/Repositories/IncomingWebhookMessageRepository.cs`
- `Event.API.IntegrationTests/Features/*IncomingWebhook*`

Acceptance:

- Raw body verification happens before JSON parsing.
- Bad signatures reject with safe ProblemDetails.
- Duplicate message IDs are idempotent.
- Replay window is enforced for signed timestamped payloads.
- Provider callbacks cannot directly mutate sensitive aggregates before verification and idempotency capture.
- Coop integration remains independent from outgoing provider mode.

Validation:

```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release
```

### Phase 7 - Blazor webhook management UI

Deliverables:

- Webhook settings page.
- Endpoint list and detail views.
- Create/update endpoint dialog.
- Secret rotation UI.
- Delivery attempts table.
- Manual retry action.
- Test event action.
- Provider health status.
- Svix "Open Advanced Webhook Portal" action.

Likely files:

- `Explore.Blazor.Client/Pages/Admin/Webhooks/*`
- `Explore.Blazor.Client/Components/Webhooks/*`
- `Explore.Blazor.Client/Services/*Webhook*`
- `Explore.Blazor.Client/Explore.Blazor.Client.csproj` if needed
- generated API client files

Acceptance:

- UI renders actions only from HAL links.
- UI does not inspect roles/claims locally.
- No endpoint secret is displayed after creation/rotation except the one-time reveal pattern if implemented.
- Tables are paged, accessible, and responsive.
- Components use MudBlazor wrappers and project design-system conventions.

Validation:

```bash
dotnet test --project Event.Blazor.Client.Tests/Event.Blazor.Client.Tests.csproj --configuration Release
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release
```

Manual QA gate:

- Run the API/Blazor app locally.
- Create a LocalProvider endpoint pointing at a local test receiver.
- Trigger a test webhook.
- Observe a signed POST, a delivery attempt row, HAL-driven retry/rotate actions, and safe logs.
- Switch to DryRun and verify no outbound request is made.
- If Svix credentials are configured, generate an App Portal URL and create a Svix message through a fake or test Svix backend.

### Phase 8 - Operations, Aspire, docs, and rollout

Deliverables:

- `docs/WEBHOOKS.md`
- updates to `docs/INTEGRATIONS.md`
- updates to `docs/OPERATIONS.md`
- updates to `docs/CONFIGURATION.md`
- updates to `docs/SECURITY-MODEL.md`
- updates to `docs/API.md`
- updates to `docs/API_CHANGELOG.md`
- updates to `docs/BLAZOR.md`
- README wording if the public promise changes
- Aspire wiring for optional Svix server if the team chooses to support local Svix composition

Acceptance:

- Operators can configure `Disabled`, `DryRun`, `Local`, `Svix`, and `Composite`.
- Docs explain outgoing versus incoming webhooks.
- Docs explain LocalProvider limits and SvixProvider advanced capabilities.
- Docs list security constraints and SSRF defaults.
- Docs list operational metrics and health checks.
- Self-hosters can run LocalProvider without Svix.

Validation:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release
```

## Testing Strategy

Unit tests:

- `WebhookSignatureServiceTests`
- `WebhookPayloadBuilderTests`
- `WebhookRetrySchedulerTests`
- `WebhookEndpointSafetyPolicyTests`
- `LocalWebhookDeliveryProviderTests`
- `SvixWebhookDeliveryProviderTests` with fake Svix client
- `IncomingWebhookVerifierTests`
- moderation payload minimization tests

Persistence tests:

- tenant isolation
- endpoint subscription filtering
- message and attempt ordering
- payload retention cleanup
- provider link idempotency
- worker lease and stale-processing recovery

API integration tests:

- tenant admin endpoint CRUD allowed
- unauthorized user denied
- HAL links emitted only when authorized
- manual retry route idempotent
- rotate secret route does not leak old secret
- incoming webhook rejects bad signature
- incoming webhook accepts valid signature once
- duplicate incoming callback does not duplicate side effects

Infrastructure tests:

- HTTP timeout
- private IP blocked
- localhost blocked
- cloud metadata IP blocked
- DNS private resolution blocked
- redirect to private IP blocked
- retry exhaustion
- endpoint auto-disable
- safe logging fields only

Svix contract tests:

- fake client first for deterministic CI.
- optional real self-hosted Svix container later through Aspire/test compose.

## Verification Baseline

Do not run solution-level `dotnet test`. Use project-level tests.

Minimum full verification before claiming implementation complete:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.Tests/Event.Domain.Tests.csproj --configuration Release
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release
dotnet test --project Event.Persistence.Tests/Event.Persistence.Tests.csproj --configuration Release
dotnet test --project Event.Infrastructure.Tests/Event.Infrastructure.Tests.csproj --configuration Release
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release
dotnet test --project Event.Blazor.Client.Tests/Event.Blazor.Client.Tests.csproj --configuration Release
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release
```

If a named test project does not exist at implementation time, record that fact in the progress report and run the closest existing project-specific test coverage instead.

## Documentation Updates

Create or update:

- `docs/WEBHOOKS.md`: product webhook model, provider modes, signatures, payloads, LocalProvider limits, Svix mapping.
- `docs/INTEGRATIONS.md`: incoming callback model, Coop/Osprey/Svix operational callbacks, idempotency.
- `docs/OPERATIONS.md`: workers, retries, readiness checks, queue health, provider switching.
- `docs/CONFIGURATION.md`: options, secret refs, tenant settings, Svix server config.
- `docs/SECURITY-MODEL.md`: SSRF, secret rotation, logging policy, replay protection.
- `docs/API.md`: endpoint catalog and response contracts.
- `docs/API_CHANGELOG.md`: webhook API contract entries.
- `docs/BLAZOR.md`: HAL-driven webhook UI affordances.
- `README.md`: align public "Svix-compatible webhooks" claim with Local/Svix provider wording.

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| SSRF through user-owned endpoint URLs | Block private/internal networks by default, disable redirects, validate DNS/IP, test IPv4/IPv6/metadata/redirect cases. |
| Duplicated deliveries | Treat delivery as at-least-once; expose event/message IDs and idempotency guidance; use provider idempotency keys. |
| Secret leakage | Store secret refs, not cleartext where possible; never log secrets; one-time reveal only if needed. |
| Tenant data leakage from workers | Use repository-owned cross-tenant queue scans with explicit bypass reasons; rebind tenant before tenant-specific processing. |
| Svix outage blocks business flows | Keep provider calls post-commit; record bounded failure; retry; do not fail the original business transaction. |
| Heavy moderation data leakage | Reuse and test the existing heavy-redaction minimization pattern. |
| UI authorization drift | HAL links are the only UI affordance source; tests assert absent links for unauthorized users. |
| Provider switching loses advanced Svix state | Document that advanced Svix-only features are not fully migratable. Keep canonical ISLAMU messages as audit state. |

## Definition of Done

Implementation is complete only when:

- LocalProvider can deliver a signed webhook to a test receiver and record attempts safely.
- SvixProvider can publish through a fake deterministic client, and optionally a real configured Svix server.
- Incoming callback verification and idempotency are shared and tested.
- Webhook admin APIs expose HAL affordances and enforce resource authorization.
- Blazor UI uses only HAL links for action affordances.
- Heavy moderation payload tests prove no unsafe content leaks.
- SSRF and signature tests pass.
- Docs explain configuration, operations, security, and provider switching.
- Required project-level tests and build pass, or pre-existing failures are documented with evidence.

## Implementation Agent Contract

Before editing code, the implementation agent must:

1. Re-read `AGENTS.md`, `.github/copilot-instructions.md`, `.claude/contract/intents.yaml`, and matching rules for files being changed.
2. Re-read this plan, context, and tasks.
3. Run or record the current build baseline.
4. Update `webhooks-local-svix-provider-context.md` with new facts as they are discovered.
5. Update `webhooks-local-svix-provider-tasks.md` incrementally as tasks are completed.

During implementation:

- Keep changes within the phase scope unless a later phase blocks the current one.
- Prefer established repository patterns over new abstractions.
- Keep external provider calls out of business transactions.
- Keep all public API shape changes documented in `docs/API_CHANGELOG.md`.
- Do not weaken architecture, auth, or tenant isolation tests.

## Progress Report Contract

Every implementation progress update should include:

- files changed
- migration status
- test commands run and results
- manual QA performed
- open risks or blocked questions
- next concrete task
