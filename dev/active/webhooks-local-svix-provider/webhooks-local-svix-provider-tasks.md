<!-- ABOUTME: Execution checklist for implementing Local and Svix webhook providers in ISLAMU Event. -->
<!-- ABOUTME: Tracks phased tasks, acceptance criteria, validation commands, and implementation progress. -->

# Webhooks Local/Svix Provider Tasks

Last Updated: 2026-07-04 Europe/Brussels

## Current State

- Overall status: implementation started.
- Planning artifacts: complete for initial review.
- Code changes: Phase 1 Application-layer foundation and canonical publisher, Phase 2 canonical Domain/Persistence model, Phase 3 LocalProvider delivery foundation/worker/manual-retry/observability slices, Phase 4 event catalog, consumer management, and endpoint list/detail/create/update/archive API/HAL surfaces, Phase 5 Svix delivery-provider/App Portal service/event-type sync cores, the backend Svix App Portal API route with webhook authorization policy, and Phase 6 incoming webhook framework are implemented.
- Migrations: `20260702192022_AddWebhookSubsystem` generated and applied by PostgreSQL integration tests.
- Tests: affected Application/Persistence/Infrastructure/API/Blazor client project builds pass; full release solution build passes; Application unit tests, Infrastructure tests, API integration tests, Persistence integration tests, Architecture tests, Blazor client tests, LSP diagnostics, and `git diff --check` pass for the implemented slices.

## Task Tracking Rules

- Update this file after each implementation slice.
- Keep completed tasks checked as soon as they land.
- Add discovered sub-tasks instead of hiding them in progress notes.
- Record test commands and failures next to the phase they validate.
- Do not mark a phase complete until its acceptance criteria and validation commands are satisfied or explicitly documented as blocked.

## Phase 0 - Baseline and Guardrails

### Planning and classification

- [x] Create `dev/active/webhooks-local-svix-provider/`.
- [x] Write `webhooks-local-svix-provider-plan.md`.
- [x] Write `webhooks-local-svix-provider-context.md`.
- [x] Write `webhooks-local-svix-provider-tasks.md`.
- [ ] Confirm whether to add a first-class `webhooks` intent to `.claude/contract/intents.yaml`.
- [x] Re-read all matching rules immediately before code implementation starts.
- [x] Re-read `AGENTS.md`, `.github/copilot-instructions.md`, `docs/QUICK_REFERENCE.md`, and `docs/GOVERNANCE.md` immediately before code implementation starts.
- [x] Confirm `RTK.md` is still absent or read it if it appears.

### Baseline discovery

- [x] Confirm current package management convention for adding the official `Svix` package.
- [x] Confirm exact test project names with `rg --files '*Tests.csproj'`.
- [ ] Confirm OpenAPI/client generation command.
- [x] Confirm Cerbos/local authorization file layout for new `webhook:*` actions.
- [ ] Confirm no active `dev/active` plan conflicts with webhook subsystem work.
- [x] Run baseline build before code changes.

Validation:

```bash
dotnet build --configuration Release --verbosity quiet
```

Acceptance:

- [x] Baseline build result is recorded.
- [x] Any pre-existing failures are documented with evidence. No pre-existing failures remained after rerun; existing warnings are recorded below.
- [x] Implementation starts only after matching rules and docs are reloaded.

## Phase 1 - Event Catalog, Envelope, and Payload Policy

### Application contracts and models

- [x] Add `WebhookEventEnvelope`.
- [x] Add `WebhookEventBuildContext`.
- [x] Add `WebhookPayloadBuildResult`.
- [x] Add `WebhookProviderMessage`.
- [x] Add `WebhookProviderPublishResult`.
- [x] Add `WebhookEventPublishResult`.
- [x] Add `WebhookSignatureHeaders`.
- [x] Add `WebhookVerificationResult`.
- [x] Add `WebhookSecretMaterial`.
- [x] Add `IWebhookEventPublisher`.
- [x] Add `IWebhookDeliveryProvider`.
- [x] Add `IWebhookEndpointManager`.
- [x] Add `IWebhookSignatureService`.
- [x] Add `IWebhookPayloadBuilder`.
- [x] Add `DefaultWebhookEventPublisher` application service.

### Event catalog

- [x] Add `WebhookEventTypeRegistry`.
- [x] Add `WebhookEventSchemaProvider`.
- [x] Add JSON Schema Draft 7-compatible schema definitions or schema DTO generation.
- [x] Add example payloads for each public event type.
- [x] Define event type group names.
- [x] Ensure event type names are Svix-compatible.

Initial event types:

- [x] `event.created`
- [x] `event.published`
- [x] `event.updated`
- [x] `event.cancelled`
- [x] `event.light_moderated`
- [x] `event.heavy_redacted`
- [x] `registration.created`
- [x] `registration.approved`
- [x] `registration.cancelled`
- [x] `report.created`
- [x] `report.decision_created`
- [x] `organization.verified`

### Payload policy

- [x] Implement stable envelope shape with `id`, `type`, `version`, `occurredAt`, `tenantId`, and `data`.
- [x] Implement payload hashing.
- [x] Implement payload retention calculation.
- [x] Implement data minimization for all event payloads.
- [x] Implement heavy moderation payload as generic/linkless.
- [x] Add tests proving heavy moderation payloads omit unsafe identity/content fields.

Validation:

```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release
```

Acceptance:

- [x] All initial event types have names, descriptions, schemas, examples, and versions.
- [x] Payload builders do not depend on Infrastructure, API, or Persistence.
- [x] Heavy-redaction minimization tests pass.
- [x] Validators are manually instantiated where applicable. No validators were added in this slice.

## Phase 2 - Domain, Persistence, and Migration

### Domain entities and enums

- [x] Add `WebhookConsumer`.
- [x] Add `WebhookConsumerKind`.
- [x] Add `WebhookConsumerStatus`.
- [x] Add `WebhookProviderMode`.
- [x] Add `WebhookEventType`.
- [x] Add `WebhookEndpoint`.
- [x] Add `WebhookEndpointStatus`.
- [x] Add `WebhookEndpointSubscription`.
- [x] Add `WebhookMessage`.
- [x] Add `WebhookMessageStatus`.
- [x] Add `WebhookDeliveryAttempt`.
- [x] Add `WebhookDeliveryAttemptStatus`.
- [x] Add `WebhookProviderLink`.
- [x] Add `WebhookProviderLinkSyncState`.
- [x] Add `WebhookExternalProvider`.
- [x] Add `IncomingWebhookMessage`.
- [x] Add `IncomingWebhookMessageStatus`.

### Repository contracts

- [x] Add `IWebhookConsumerRepository`.
- [x] Add `IWebhookEventTypeRepository`.
- [x] Add `IWebhookEndpointRepository`.
- [x] Add `IWebhookMessageRepository`.
- [x] Add `IWebhookDeliveryAttemptRepository`.
- [x] Add `IWebhookProviderLinkRepository`.
- [x] Add `IIncomingWebhookMessageRepository`.

Repository requirements:

- [x] Repositories return entities, never DTOs.
- [x] Read queries use `AsNoTracking` when mutation is not needed.
- [x] Worker queue scans use explicit tenant-filter bypass reasons.
- [x] Mutating repository methods accept cancellation tokens.
- [x] Bounded fields are truncated consistently before persistence.

### EF Core mappings

- [x] Add DbSets to `ExploreDbContext`.
- [x] Add entity configurations under `Explore.Persistence/Configurations/Entities/`.
- [x] Add tenant filters.
- [x] Add indexes for tenant queries.
- [x] Add indexes for worker polling.
- [x] Add indexes for provider idempotency.
- [x] Add uniqueness constraints for endpoint subscriptions.
- [x] Add jsonb mappings for payload/schema/safe headers.
- [x] Add migration `AddWebhookSubsystem`.
- [x] Add reversible `Down`.
- [x] Update persistence DI registration.

### Retention

- [x] Add payload retention fields.
- [x] Add cleanup query/repository method for expired payloads.
- [x] Ensure retention cleanup removes or redacts payload body without breaking audit status.

Validation:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Acceptance:

- [x] Migration applies through PostgreSQL integration tests.
- [x] Reversible `Down` generated and inspected.
- [x] Tenant isolation tests pass.
- [x] Repository tests prove subscription filtering and provider-link idempotency.
- [x] Architecture tests confirm layer boundaries.

## Phase 3 - LocalWebhookProvider

### Provider infrastructure

- [x] Add `WebhookOptions` with instance-level settings.
- [x] Add tenant-level webhook settings model.
- [x] Add options validation.
- [x] Add `DisabledWebhookDeliveryProvider`.
- [x] Add `DryRunWebhookDeliveryProvider`.
- [x] Add `LocalWebhookDeliveryProvider`.
- [x] Add provider selector/factory.
- [x] Register providers in Infrastructure DI.

### Signature service

- [x] Implement Svix-compatible signing.
- [x] Implement Svix-compatible verification.
- [x] Support current and previous secrets.
- [x] Enforce timestamp tolerance.
- [x] Use fixed-time comparison.
- [x] Add positive and negative signature tests.
- [x] Add raw-body verification tests.

### SSRF and HTTP safety

- [x] Add `WebhookEndpointSafetyPolicy`.
- [x] Block localhost names.
- [x] Block loopback IPv4 and IPv6.
- [x] Block RFC1918 private IPv4 ranges.
- [x] Block link-local ranges.
- [x] Block cloud metadata addresses.
- [x] Block internal DNS results.
- [x] Disable redirects.
- [x] Add explicit operator allow-list for private CIDRs.
- [x] Add tests for IPv4, IPv6, DNS/internal-name, metadata, and allow-list cases.
- [x] Add redirect-disabling tests with the HTTP delivery worker.

### Delivery worker

- [x] Add attempt claim method with lease token.
- [x] Add stale processing recovery.
- [x] Add `WebhookRetryScheduler`.
- [x] Add `WebhookDeliveryProcessor` or shared drain service.
- [x] Add status transitions for scheduled, sending, succeeded, failed, abandoned.
- [x] Record response body previews safely.
- [x] Truncate error categories and response previews.
- [x] Update endpoint last success/failure timestamps.
- [x] Mark or disable repeatedly failing endpoints.
- [x] Add manual retry support.

Retry schedule:

- [x] attempt 1: immediately
- [x] attempt 2: +30 seconds
- [x] attempt 3: +5 minutes
- [x] attempt 4: +30 minutes
- [x] attempt 5: +2 hours
- [x] attempt 6: +6 hours
- [x] attempt 7: +12 hours
- [x] attempt 8: +24 hours

### Observability

- [x] Add bounded metrics for message creation.
- [x] Add bounded metrics for attempts.
- [x] Add bounded metrics for success/failure.
- [x] Add bounded metrics for endpoint disabled.
- [x] Add safe structured logs.
- [x] Add health check for LocalProvider queue state.

Validation:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet
dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet
dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release
```

Current validation result:

- [x] `Explore.Application` release build passed.
- [x] `Explore.Persistence` release build passed.
- [x] `Explore.Infrastructure` release build passed.
- [x] `Explore.Infrastructure.Tests` passed: 567/567.
- [x] `Event.Persistence.IntegrationTests` passed: 196/196.
- [x] `Event.Architecture.Tests` passed: 239/240 succeeded, 1 known skipped API metadata test.
- [x] `Explore.API` release build passed.
- [x] Full solution build passed.
- [x] `Event.Application.UnitTests` passed: 1732/1732.
- [x] `Explore.Infrastructure.Tests` passed: 574/574.
- [x] `Event.Persistence.IntegrationTests` passed: 196/196.
- [x] `Event.Architecture.Tests` passed: 239/240 succeeded, 1 known skipped API metadata test.

Acceptance:

- [x] Local signed POST succeeds against a test receiver.
- [x] 2xx is success.
- [x] Non-2xx is failure.
- [x] Redirect is failure.
- [x] Timeout is retryable failure.
- [x] Private/internal endpoints are blocked by default.
- [x] Attempt rows are safe and bounded.
- [x] Delivery worker can resume after crash/stale lease.
- [x] Manual retry appends an immediate scheduled attempt only for terminal attempts with active endpoints and no duplicate active attempt.
- [x] LocalProvider readiness reports disabled/non-local mode, disabled processor, due backlog, and stale sending leases without exposing endpoint URLs, payloads, or secrets.

## Phase 4 - Webhook Management API, Authorization, and HAL

### CQRS handlers

- [x] Add query for event types.
- [x] Add query for consumers.
- [x] Add query for consumer detail.
- [x] Add command to create consumer.
- [x] Add query for endpoints.
- [x] Add query for endpoint detail.
- [x] Add command to create endpoint.
- [x] Add command to update endpoint.
- [x] Add command to archive/delete endpoint.
- [x] Add command to rotate endpoint secret.
- [x] Add command to send test webhook.
- [x] Add query for messages.
- [x] Add query for message detail.
- [x] Add command to retry failed/abandoned delivery attempts.
- [x] Add query for delivery attempts.
- [ ] Add manual validators for every command/query that needs validation.

### API controllers and routes

- [x] Add route names for all webhook routes.
- [x] Add route name for event type catalog route.
- [x] Add route names for consumer management routes.
- [x] Add route name for Svix App Portal access route.
- [x] Add route name for endpoint secret rotation route.
- [x] Add `GET /api/webhooks/event-types`.
- [x] Add `GET /api/webhooks/consumers`.
- [x] Add `GET /api/webhooks/consumers/{consumerId}`.
- [x] Add `POST /api/webhooks/consumers`.
- [x] Add `GET /api/webhooks/endpoints`.
- [x] Add `POST /api/webhooks/endpoints`.
- [x] Add `GET /api/webhooks/endpoints/{id}`.
- [x] Add `PUT /api/webhooks/endpoints/{id}`.
- [x] Add `DELETE /api/webhooks/endpoints/{id}`.
- [x] Add `POST /api/webhooks/endpoints/{id}/rotate-secret`.
- [x] Add `POST /api/webhooks/endpoints/{id}/test`.
- [x] Add `GET /api/webhooks/messages`.
- [x] Add `GET /api/webhooks/messages/{id}`.
- [x] Add `POST /api/webhooks/delivery-attempts/{id}/retry`.
- [x] Add `GET /api/webhooks/delivery-attempts`.
- [x] Add `GET /api/webhooks/delivery-attempts/{id}`.
- [x] Add explicit response metadata and ProblemDetails metadata for Svix App Portal route.
- [x] Add endpoint classification and write rate limit for Svix App Portal route.

### Authorization

- [x] Add action `webhook:view`.
- [x] Add action `webhook:create`.
- [x] Add action `webhook:update`.
- [x] Add action `webhook:delete`.
- [x] Add action `webhook:rotate-secret`.
- [x] Add action `webhook:test`.
- [x] Add action `webhook:retry`.
- [x] Add action `webhook:view-delivery`.
- [x] Add action `webhook:manage-provider`.
- [x] Add action `webhook:open-provider-portal`.
- [x] Add local fallback policy behavior.
- [x] Add Cerbos policy behavior if Cerbos policies cover new actions.
- [x] Add API metadata and command/query authorization tests for consumer management.
- [x] Add API metadata and command/query authorization tests for endpoint list/detail/create/update/archive/rotate-secret.
- [ ] Add tests for tenant admin.
- [ ] Add tests for organization admin when tenant setting allows.
- [ ] Add tests for unauthorized user.

### HAL

- [x] Add HAL assembler/link policy for consumers.
- [x] Add HAL assembler/link policy for endpoints.
- [ ] Add HAL assembler/link policy for messages.
- [ ] Add HAL assembler/link policy for delivery attempts if needed.
- [ ] Add link relation constants.
- [x] Add endpoint `rotate-secret` link relation constant.
- [x] Add OpenAPI/HAL schema catalog coverage for consumer HAL resources.
- [x] Add OpenAPI/HAL schema catalog coverage for endpoint HAL resources.
- [ ] Add tests that authorized links appear.
- [ ] Add tests that unauthorized links are absent.
- [ ] Add tests that provider-specific links appear only in the right mode.

### OpenAPI and client

- [x] Update OpenAPI metadata and operation IDs for `GET /api/webhooks/event-types`.
- [x] Regenerate Blazor client using the repository command.
- [x] Update `docs/API.md` for current webhook event catalog, consumer management, endpoint list/detail/create/update/archive/rotate-secret, and Svix App Portal API surface.
- [x] Update `docs/API_CHANGELOG.md`.

Validation:

```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release
```

Current validation result:

- [x] Baseline `dotnet build --configuration Release --verbosity quiet` passed before the event-catalog API slice.
- [x] `dotnet build --configuration Release --verbosity quiet` passed after `GET /api/webhooks/event-types`.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet` passed: 1737/1737.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet` passed: 239/240, 1 known skipped API metadata test.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet` passed: 1461/1464, 3 known skips.
- [x] `git diff --check` passed after the event-catalog API slice.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed after endpoint list/detail/create: 1751/1751.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed after endpoint list/detail/create: 239/240, 1 documented skip.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passed after endpoint list/detail/create: 1473/1476, 3 documented skips.
- [x] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed after endpoint repository additions: 196/196.
- [x] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed after generated-client HAL link test hardening: 1436/1437, 1 documented skip.
- [x] `dotnet build --configuration Release --verbosity quiet` passed after endpoint list/detail/create and generated-client test hardening: 25 projects, 0 errors.
- [x] `git diff --check` passed after endpoint list/detail/create.
- [x] `dotnet build --configuration Release --verbosity quiet` passed after endpoint update/archive: 25 projects, 0 errors.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed after endpoint update/archive: 1757/1757.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passed after endpoint update/archive: 1478/1481, 3 documented skips.
- [x] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed after endpoint HAL action links: 1437/1438, 1 documented skip.
- [x] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed after endpoint repository update/archive additions: 196/196.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed after endpoint update/archive: 239/240, 1 documented skip.

Acceptance:

- [x] All write endpoints are `[Authorize]`.
- [x] `GET /api/webhooks/event-types` follows the public GET convention and exposes only provider-neutral catalog metadata.
- [x] HAL is the only UI affordance source for implemented endpoint create/update/delete affordances.
- [ ] OpenAPI/client generation is clean.

## Phase 5 - SvixProvider

### Package and options

- [x] Add official `Svix` package using repository package conventions.
- [x] Add `SvixWebhookOptions`.
- [x] Add startup validation for base URL and token secret ref.
- [ ] Add startup validation for App Portal and event-type sync behavior when those services land.
- [x] Add secret-provider lookup for Svix API token.

### Provider implementation

- [x] Add `SvixWebhookDeliveryProvider`.
- [x] Add Svix client wrapper interface for tests.
- [x] Add fake/substitute Svix client for deterministic tests.
- [x] Map consumer to Svix application UID.
- [x] Create or retrieve Svix application.
- [x] Sync event types when configured.
- [x] Add startup hosted-service wiring for configured event type sync.
- [x] Create Svix message with `eventType`.
- [x] Set `eventId` to canonical webhook message id.
- [x] Send idempotency key.
- [x] Set payload retention period.
- [x] Persist external message id/provider link.
- [x] Record bounded failure categories.

### App Portal

- [x] Add `SvixAppPortalService`.
- [x] Add backend-only route `POST /api/webhooks/svix/app-portal`.
- [x] Authorize with `webhook:open-provider-portal`.
- [x] Return only short-lived URL/token data needed by frontend.
- [x] Never expose Svix API token to Blazor.
- [x] Bypass global HTTP idempotency replay for short-lived App Portal tokens.
- [ ] Add HAL link for portal only when Svix mode and portal enabled.

### Health and operations

- [x] Add Svix health/readiness check.
- [ ] Add metrics for provider publish failures.
- [x] Add docs for self-hosted Svix dependencies.
- [x] Add optional Aspire composition notes if accepted.

Validation:

```bash
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release
```

Current validation result:

- [x] Context7 Svix documentation reviewed for official C# SDK usage, self-hosted base URL support, idempotency, message creation, and App Portal backend-only constraints.
- [x] NuGet package search confirmed official `Svix` package version `1.96.1`.
- [x] `dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --verbosity quiet` passed.
- [x] `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed.
- [x] Focused Svix provider tests passed with fake/substitute client coverage.
- [x] `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed: 306/306.
- [x] `dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed: 202/202.
- [x] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed: 578/578.
- [x] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed: 196/196.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed: 239/240, 1 known skipped API metadata test.
- [x] `dotnet build --configuration Release --verbosity quiet` passed.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed: 1732/1732.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed: 1454/1457, 3 known skips.
- [x] LSP diagnostics returned no diagnostics for the changed Svix provider, configuration, registration, secret, repository contract, repository implementation, and test files.
- [x] `git diff --check` passed.
- [x] Context7 Svix documentation reviewed for App Portal access and event type create/update APIs.
- [x] Local reflection against `Svix` `1.96.1` verified `Authentication.AppPortalAccessAsync`, `AppPortalAccessIn`, `EventType.CreateAsync`, `EventType.UpdateAsync`, `EventTypeIn`, `EventTypeUpdate`, and idempotency option shapes.
- [x] `dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --verbosity quiet` passed after App Portal/event-type sync additions.
- [x] `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed after App Portal/event-type sync additions.
- [x] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed: 584/584.
- [x] `dotnet build --configuration Release --verbosity quiet` passed after App Portal/event-type sync additions.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed: 1732/1732.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed: 239/240, 1 known skipped API metadata test.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed: 1454/1457, 3 known skips.
- [x] LSP diagnostics returned no diagnostics for new App Portal/event-type sync contracts, services, helpers, adapter updates, registration, and tests.
- [x] `git diff --check` passed after App Portal/event-type sync additions.
- [x] `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed after startup worker registration.
- [x] LSP diagnostics returned no diagnostics for `SvixWebhookEventTypeSyncWorker` and `Program.cs`.
- [x] `dotnet build --configuration Release --verbosity quiet` passed after startup worker registration.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed after startup worker registration: 239/240, 1 known skipped API metadata test.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed after startup worker registration: 1454/1457, 3 known skips.
- [x] `dotnet build --configuration Release --verbosity quiet` passed after Svix App Portal API route and webhook authorization policy additions.
- [x] `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet` passed after Svix App Portal API route additions: 1736/1736.
- [x] `dotnet test Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet` passed after webhook resource/Cerbos policy additions: 239/240, 1 known skipped API metadata test.
- [x] `dotnet test Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet` passed after Svix App Portal API route and idempotency-bypass additions: 1459/1462, 3 known skips.
- [x] `git diff --check` passed after Svix App Portal API route additions.

Acceptance:

- [x] Fake Svix provider tests cover success, idempotency, failure, and provider-link persistence.
- [x] App Portal route is backend-only and authorized.
- [x] Svix token is resolved through secret provider only.
- [x] Svix mode does not break LocalProvider mode.
- [x] Local-full config supplies a deterministic local Svix JWT secret and server-side API token binding for disposable development.

## Phase 6 - Incoming Webhook Framework

### Shared abstractions

- [x] Add `IncomingWebhookContext`.
- [x] Add `IncomingWebhookVerificationResult`.
- [x] Add `IIncomingWebhookVerifier`.
- [x] Add `IIncomingWebhookHandler`.
- [x] Add verifier registry.
- [x] Add incoming webhook persistence.
- [x] Add incoming webhook idempotency checks.

### Existing provider integration

- [x] Adapt Coop signature validation to shared interface or wrap existing validator.
- [x] Preserve Coop route behavior and tests.
- [x] Preserve Osprey route behavior and tests.
- [x] Add Svix operational webhook verifier.
- [x] Add `POST /api/integrations/svix/operational`.
- [x] Confirm no incoming callback depends on outgoing provider mode.

### Processing rules

- [x] Read raw request body before JSON parsing.
- [x] Enforce max body size.
- [x] Verify signature before processing.
- [x] Persist idempotency/incoming message row before side effects.
- [x] Return quickly.
- [x] Queue side effects through commands/outbox where appropriate.
- [x] Never directly mutate sensitive aggregates from unverified callbacks.
- [x] Add replay protection tests.

Validation:

```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release
```

Acceptance:

- [x] Invalid signature rejects safely.
- [x] Valid signature processes once.
- [x] Duplicate callback is idempotent.
- [x] Raw body verification cannot be bypassed by JSON reserialization.
- [x] Coop remains independent of `Disabled`, `Local`, `Svix`, `Composite`, and `DryRun` outgoing modes.

Progress 2026-07-03:

- Added provider-neutral incoming webhook intake in `Explore.API/Services`: registry, raw-body read/verify, bounded body enforcement, safe header capture, payload hashing, tenant-scoped ledger capture, duplicate detection, and processed/rejected state updates.
- Added `CoopIncomingWebhookVerifier` and wired the Coop callback route through shared intake before JSON command dispatch. Duplicate Coop provider message IDs return a successful idempotent response without executing the decision command again.
- Added `SvixIncomingWebhookVerifier`, `Webhooks:Svix:OperationalWebhookSecretRef`, and `POST /api/integrations/svix/operational` / `IntegrationSvixOperationalCallback`. The route verifies Svix-compatible signatures and is independent of outgoing webhook provider mode.
- Added focused API integration tests for route metadata, Coop verification, Svix signature verification, raw-body reset, safe header capture, and duplicate Coop idempotency.

Audit 2026-07-03:

- Confirmed Phase 6 is already implemented well enough to remain closed. The implementation keeps incoming callbacks separate from outgoing provider modes, verifies raw bodies before JSON parsing, captures tenant-scoped `incoming_webhook_messages` rows before Coop side effects, stores payload hashes instead of raw payloads by default, redacts sensitive headers from the ledger, and treats duplicate provider message IDs idempotently.
- Confirmed Coop callbacks enter through API-key policy authorization plus shared HMAC verifier, then dispatch Application-layer moderation commands after capture. The local command path persists the provider decision first and delegates moderation execution through the existing report-decision command/outbox-backed moderation flow rather than mutating the event directly in the controller.
- Confirmed Svix operational callbacks are `[AllowAnonymous]` only at the HTTP edge; Svix-compatible signature headers are the authentication boundary, and tenant-addressed payloads are optionally captured without coupling to `Local`, `Svix`, `Composite`, `DryRun`, or `Disabled` outgoing provider modes.
- Verification passed:
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/IncomingWebhookFrameworkTests/*" --minimum-expected-tests 1 --log-level Error --no-progress`
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/ModerationIntegrationControllerTests/*" --minimum-expected-tests 1 --log-level Error --no-progress`
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/WebhookPersistenceTests/IncomingWebhookRepository_TryCreate_IsIdempotentPerTenantProviderMessage" --minimum-expected-tests 1 --log-level Error --no-progress`

Re-audit 2026-07-04:

- Re-checked Phase 6 against the latest user request and confirmed no new source changes are needed for this phase. `IncomingWebhookIntakeService` uses ASP.NET Core request buffering to read the raw body once, rewinds the stream, verifies signatures before JSON parsing, enforces provider body-size limits, captures only payload hashes plus safe headers, and writes tenant-scoped idempotency rows before Coop side effects.
- Re-checked current official docs through Context7: ASP.NET Core documents `HttpRequest.EnableBuffering` as the supported multiple-read raw-body path, and Svix documents raw-body verification with `svix-id`, `svix-timestamp`, and `svix-signature` headers.
- Fresh verification passed:
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/IncomingWebhookFrameworkTests/*" --minimum-expected-tests 1 --log-level Error --no-progress`
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/ModerationIntegrationControllerTests/*" --minimum-expected-tests 1 --log-level Error --no-progress`
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/WebhookPersistenceTests/IncomingWebhookRepository_TryCreate_IsIdempotentPerTenantProviderMessage" --minimum-expected-tests 1 --log-level Error --no-progress`

Latest Phase 6 verification 2026-07-04 01:42 CEST:

- Confirmed Phase 6 remains implemented and closed; no source changes were needed for incoming webhooks.
- Reconfirmed through Context7 that Svix receiving verification requires the raw body and `svix-id`, `svix-timestamp`, and `svix-signature` headers. The current `SvixIncomingWebhookVerifier` delegates those semantics to the shared Svix-compatible signature service.
- Reconfirmed the repository implementation owns the expected Phase 6 boundaries:
  - `IncomingWebhookIntakeService` reads and rewinds raw request bodies, verifies before JSON parsing, enforces body limits, filters sensitive headers, hashes payloads, and persists tenant-scoped idempotency rows.
  - `ModerationIntegrationController.RecordCoopCallback` captures a verified incoming message before dispatching `ProcessCoopDecisionCallbackCommand`; duplicates return a successful idempotent response without re-running the command.
  - `IncomingWebhooksController.RecordSvixOperationalCallback` accepts anonymous-at-edge Svix operational callbacks authenticated by signature and independent from outgoing provider mode.
- Fresh verification passed:
  - `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -m:1` passed: 0 errors, 68 existing warnings.
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/IncomingWebhookFrameworkTests/*" --minimum-expected-tests 1 --log-level Error --no-progress` passed: 10/10.
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ModerationIntegrationControllerTests/*" --minimum-expected-tests 1 --log-level Error --no-progress` passed: 6/6.
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/WebhookPersistenceTests/IncomingWebhookRepository_TryCreate_IsIdempotentPerTenantProviderMessage" --minimum-expected-tests 1 --log-level Error --no-progress` passed: 1/1.

Latest Phase 6 verification 2026-07-04 03:18 CEST:

- Rechecked the latest user request to start Phase 6 and confirmed Phase 6 is already implemented and remains closed. No incoming-webhook source changes were needed in this pass.
- Verified the implemented boundary still matches the plan: incoming Coop/Svix callbacks are normal API ingestion endpoints, raw request bodies are verified before JSON processing, duplicate provider message IDs are idempotent, and incoming callbacks stay independent from outgoing `Disabled`, `Local`, `Svix`, `Composite`, and `DryRun` delivery modes.
- Fresh focused verification passed:
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/IncomingWebhookFrameworkTests/*" --minimum-expected-tests 1 --log-level Error --no-progress` passed: 10/10.
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/WebhookPersistenceTests/IncomingWebhookRepository_TryCreate_IsIdempotentPerTenantProviderMessage" --minimum-expected-tests 1 --log-level Error --no-progress` passed: 1/1.
  - Existing package/analyzer warnings remain, including `NU1903` for `AutoMapper`/`Microsoft.OpenApi` and existing domain analyzer warnings; no Phase 6 test failures.

## Phase 7 - Blazor Management UI

### Pages and components

- [x] Add webhook settings page.
- [x] Add event type catalog view.
- [x] Add consumer list/detail if exposed in UI.
- [x] Add endpoint list.
- [x] Add endpoint detail.
- [x] Add create endpoint dialog.
- [x] Add update endpoint dialog.
- [x] Add secret rotation UI.
- [x] Add test event action.
- [x] Add message list/detail.
- [x] Add delivery attempts table.
- [x] Add manual retry action.
- [x] Add provider health/status surface.
- [x] Add "Open Advanced Webhook Portal" action for Svix.

### UI rules

- [x] Use generated client/service layer.
- [x] Preserve HAL links from API responses.
- [x] Render actions only from HAL links.
- [x] Do not inspect browser-side roles or claims.
- [x] Do not display secret material after the allowed one-time reveal pattern.
- [x] Use MudBlazor v9 and project wrapper components.
- [x] Add CSS isolation with BEM naming where new component CSS is needed.
- [x] Ensure keyboard accessibility and responsive layout.

Validation:

```bash
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release
```

Manual QA gate:

- [x] Run the app locally.
- [x] Navigate to webhook settings.
- [ ] Confirm unauthorized actions are absent.
- [x] Create a local endpoint.
- [x] Rotate secret and confirm no old secret exposure.
- [x] Trigger a test webhook and observe delivery attempt status.
- [x] Retry a failed webhook from HAL affordance.
- [ ] Switch to DryRun and confirm no outbound request is sent.
- [ ] In Svix mode, open provider portal when configured.

Acceptance:

- [x] UI is action-complete for LocalProvider V1.
- [x] Svix advanced management is delegated to portal.
- [x] No client-side authorization inference exists.
- [x] Visual QA confirms no layout overlap on desktop/mobile.

Latest Phase 7 verification 2026-07-04 13:36 CEST:

- Completed responsive Blazor UI hardening for the webhook management surface:
  - Required dialog fields now update immediately, so create/rotate actions enable without blur-dependent flakiness.
  - Delivery messages and attempts stack vertically on large screens and render as explicit mobile cards on phone widths instead of relying on clipped MudTable responsive rows.
  - Mobile cards preserve HAL-gated action buttons and use deterministic `data-testid` values for retry controls while keeping `aria-label` button accessibility.
  - MudTabs now uses `MinimumTabWidth="0px"` with scoped phone styling so the selected tab strip no longer shows squeezed label fragments.
- Added/updated E2E coverage for the instance-admin webhook management flow:
  - logs in through Keycloak/BFF,
  - navigates to settings -> Webhooks,
  - verifies the initial consumer/endpoint/event/delivery surface,
  - creates a LocalProvider endpoint,
  - verifies DryRun endpoints do not expose local test actions,
  - rotates the local endpoint secret without exposing old or new secret refs,
  - schedules a test webhook,
  - retries the deterministic seeded failed attempt from the HAL affordance,
  - captures desktop, tablet, and mobile screenshots.
- Fresh visual evidence:
  - `Explore.Blazor.Client.E2ETests/bin/Release/net10.0/TestResults/playwright-artifacts/webhooks/webhook-management-desktop-1280x900.png`
  - `Explore.Blazor.Client.E2ETests/bin/Release/net10.0/TestResults/playwright-artifacts/webhooks/webhook-management-tablet-768x1024.png`
  - `Explore.Blazor.Client.E2ETests/bin/Release/net10.0/TestResults/playwright-artifacts/webhooks/webhook-management-mobile-375x900.png`
- Verification commands:
  - `dotnet build --configuration Release --verbosity quiet` - passed, 25 projects, existing package warnings only.
  - `dotnet build Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -m:1` - passed.
  - `TESTCONTAINERS_RYUK_DISABLED=true dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/WebhookManagementFlowTests/*" --minimum-expected-tests 1 --log-level Error --no-progress --maximum-parallel-tests 1 --report-trx --report-trx-filename webhooks-phase7.trx` - passed, 1 test.
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/WebhookManagementPanelTests/*" --minimum-expected-tests 1 --log-level Error --no-progress` - passed, 3 tests.
  - `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/AdminClaimsTransformationTests/*" --minimum-expected-tests 1 --log-level Error --no-progress` - passed, 15 tests.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` - passed, 239 succeeded / 1 known skipped response-metadata test.
  - `git diff --check` - passed.
- Not exercised in this browser pass:
  - live Svix App Portal open, because no Svix backend is configured in the E2E app host.
  - switching provider mode to DryRun from the browser, though the seeded DryRun endpoint action absence is asserted.

## Phase 8 - Docs, Operations, and Rollout

### Documentation

- [x] Create `docs/WEBHOOKS.md`.
- [x] Update `docs/INTEGRATIONS.md`.
- [x] Update `docs/OPERATIONS.md`.
- [x] Update `docs/CONFIGURATION.md`.
- [x] Update `docs/SECURITY-MODEL.md`.
- [x] Update `docs/API.md`.
- [x] Update `docs/API_CHANGELOG.md`.
- [x] Update `docs/BLAZOR.md`.
- [x] Update `README.md` webhook wording if needed.

Docs must explain:

- [x] outgoing versus incoming webhooks.
- [x] provider modes.
- [x] LocalProvider limits.
- [x] SvixProvider advanced path.
- [x] signature format.
- [x] payload envelope and event catalog.
- [x] heavy moderation minimization.
- [x] SSRF defaults and operator allow-list.
- [x] secret rotation.
- [x] retry schedule.
- [x] delivery attempts and manual retry.
- [x] provider switching.
- [x] self-hosting requirements.

### Operations

- [x] Add configuration examples.
- [x] Add secret ref examples.
- [x] Add health check docs.
- [x] Add metric docs.
- [x] Add safe logging notes.
- [x] Add retention cleanup operation.
- [x] Add provider switching runbook.
- [x] Add optional Svix server/Aspire notes if accepted.

Validation:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release
```

Acceptance:

- [x] Self-hosters can configure LocalProvider without Svix.
- [x] Operators can configure SvixProvider without exposing secrets to the frontend.
- [x] Docs state that incoming callbacks do not depend on outgoing provider mode.

## Final Verification Checklist

Run before claiming the implementation complete:

- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release`
- [ ] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release`
- [ ] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release`
- [x] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release`
- [ ] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release`
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release`
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release`

If a project does not exist, replace it with the closest project-specific test and record the substitution here.

Manual final QA:

- [ ] LocalProvider signed POST observed with valid Svix-compatible headers.
- [ ] Failed local delivery retries and records a safe attempt row.
- [ ] SSRF-blocked endpoint fails before outbound delivery.
- [ ] Manual retry works through HAL.
- [ ] Secret rotation works without leaking old secret.
- [ ] DryRun records canonical message but sends no outbound request.
- [x] SvixProvider fake client publishes message with event type, event id, payload, retention, and idempotency key.
- [x] Svix App Portal URL route is backend-only and authorized.
- [ ] Incoming Svix/Coop webhook rejects invalid signature.
- [ ] Incoming Svix/Coop webhook accepts valid signature once and treats duplicates idempotently.
- [ ] Blazor UI actions match HAL links.

## Risk Register

| Risk | Status | Mitigation task |
| --- | --- | --- |
| SSRF through user-owned endpoint URL | Open | Phase 3 SSRF policy and tests. |
| Duplicate delivery side effects | Open | At-least-once docs, event ids, idempotency keys, consumer guidance. |
| Secret leakage through logs/API/UI | Open | Secret refs, safe DTOs, one-time reveal, log tests. |
| Tenant data leakage from background worker | Open | Repository-owned bypass reasons, tenant rebinding, persistence tests. |
| Svix outage affects product writes | Open | Post-commit dispatch, bounded failures, retries, health checks. |
| Heavy moderation payload leaks unsafe data | Open | Payload builder tests and reuse existing minimization policy. |
| Blazor authorization drift | Open | HAL-only rendering and absent-link tests. |
| Provider switching expectations too high | Open | Docs stating advanced Svix features are not fully portable. |

## Implementation Notes Log

Use this section for dated progress notes during implementation.

### 2026-07-04 Europe/Brussels - local-full Svix config, readiness, and docs

- Confirmed `local-full` has a first-class Svix server resource in `Explore.AppHost` with PostgreSQL, Redis-backed queue/cache, `SVIX_JWT_SECRET`, and API env wiring for `Webhooks:Svix:BaseUrl`, `webhooks.svix.auth_token`, and `webhooks.svix.operational_webhook_secret`.
- Added development seeding for Svix secret bindings from `WEBHOOKS_SVIX_AUTH_TOKEN` and `WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET`; `.env`, `.env.example`, and `docker-compose.yml` now use the canonical secret refs.
- Added `webhook-svix-provider` readiness through `SvixWebhookProviderHealthCheck`; it verifies server-side secret resolution for Svix/Composite mode and reports only safe booleans, not tokens, secret refs, provider URLs, or endpoint URLs.
- Removed the persistent lifetime from the stateless Svix server container in AppHost. The stale persistent server container had kept an old random host port while API config expected `http://localhost:8071`; Svix state remains in the persistent Svix PostgreSQL volume.
- Added `docs/WEBHOOKS.md` and `docs/INTEGRATIONS.md`; updated `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `docs/SECURITY-MODEL.md`, `docs/BLAZOR.md`, `docs/index.md`, and `README.md` for LocalProvider/SvixProvider behavior, incoming callback separation, local-full Svix, security, metrics, and provider switching.
- Live Svix container check succeeded against the running local server: `GET /api/v1/health` returned 200, `POST /api/v1/app/` returned 201 with the local dev JWT, and `POST /api/v1/app/{uid}/msg/` returned 202 for an `event.published` message. A later runtime check showed `aspire ps --format Json` returns `[]`, while the old container `svix-4ccf7fcf` remains healthy on `127.0.0.1:38795`; `http://localhost:8071` is not currently listening.
- Aspire live QA remains partially blocked: `aspire start --apphost Explore.AppHost/Explore.AppHost.csproj` starts resources briefly but DCP watcher streams end with `ResponseEnded`, then AppHost exits and `aspire ps` is empty. API App Portal route and browser-level Svix-mode QA still need a stable AppHost run.
- Validation passed:
  - `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
  - `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`
  - `dotnet build Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - focused `DatabaseSeederTests/*SeedsSvixSecretBindingsFromEnvironment`
  - `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet -- --minimum-expected-tests 580 --log-level Error --no-progress` passed: 663 tests.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --log-level Error --no-progress` passed: 242/243, 1 known skipped.
  - `dotnet build Explore.AppHost/Explore.AppHost.csproj --configuration Release --verbosity quiet`
  - `dotnet build --configuration Release --verbosity quiet`
  - `git diff --check`

### 2026-07-02 Europe/Brussels

- Created initial implementation plan, context, and task checklist.
- Started implementation with the Application-layer foundation slice.
- Added provider-neutral webhook contracts under `Explore.Application/Contracts/Webhooks/`.
- Added `WebhookEventTypeRegistry`, `WebhookEventSchemaProvider`, and `DefaultWebhookPayloadBuilder` under `Explore.Application/Webhooks/`.
- Registered registry, schema provider, and payload builder in `ApplicationServicesRegistration`.
- Added Application unit tests for the event catalog, schema/example generation, payload hashing, retention, allow-listed data fields, unknown event fail-closed behavior, and heavy moderation minimization.
- Validation passed:
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/Webhook*/*|/*/*/DefaultWebhookPayloadBuilderTests/*" --minimum-expected-tests 1`
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- Existing warning volume remains, including known `AutoMapper` and `Microsoft.OpenApi` vulnerability warnings.

### 2026-07-03 Europe/Brussels

- Added the canonical `DefaultWebhookEventPublisher` application service and registered it in Application DI.
- The publisher builds provider-neutral payloads, creates `webhook_messages` idempotently by tenant/message id, emits bounded `explore.webhooks.messages_created` telemetry from the application layer, and dispatches through the selected `IWebhookDeliveryProvider`.
- Disabled provider mode now skips outgoing product webhook publication without creating canonical rows; existing queued/delivered/partially-failed/cancelled messages are treated as already handed off and are not republished.
- Provider publish success marks canonical messages queued with the provider message id; provider publish failure marks the canonical message failed and returns retryability/failure category to the caller.
- Added `DefaultWebhookEventPublisherTests` covering disabled mode, new-message creation/dispatch, idempotent queued-message handling, provider failure, and payload-builder failure.
- Validation passed:
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/DefaultWebhookEventPublisherTests/*|/*/*/DefaultWebhookPayloadBuilderTests/*|/*/*/BusinessMetricsWebhookTests/*" --minimum-expected-tests 1 --log-level Error --no-progress`
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/WebhookPersistenceTests/*" --minimum-expected-tests 1 --log-level Error --no-progress`
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress`
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress`
  - `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress`
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress`
  - `dotnet build --configuration Release --verbosity quiet`
- Existing warning volume remains, including known `AutoMapper` and `Microsoft.OpenApi` vulnerability warnings plus analyzer/nullability warnings in pre-existing tests.

### 2026-07-03 Europe/Brussels - consumer management API slice

- Added `WebhookConsumerDto` and `CreateWebhookConsumerRequestDto` with normalized enum id/name fields and no secret-bearing endpoint data.
- Added `GetWebhookConsumersQuery`, `GetWebhookConsumerByIdQuery`, and `CreateWebhookConsumerCommand` with `[AuthorizeResource(ResourceKinds.Webhook, webhook:view/create)]` and `ISecureRequest` tenant attributes.
- Added `CreateWebhookConsumerCommandHandler`, `GetWebhookConsumersQueryHandler`, and `GetWebhookConsumerByIdQueryHandler`; handlers map entities in Application, validate enum IDs manually, trim optional provider ids, default new consumers to `Active`, and return conflict failures for duplicate tenant-local names.
- Added `IWebhookConsumerRepository.GetByTenantAndNameAsync` plus EF Core implementation using explicit tenant predicate and `AsNoTracking`.
- Added `GET /api/webhooks/consumers`, `GET /api/webhooks/consumers/{consumerId}`, and `POST /api/webhooks/consumers` to `WebhooksController` with named routes, HAL responses for reads, `CreatedAtRoute` on create, write rate limiting, request timeout, and API-owned ProblemDetails mapping.
- Added `WebhookConsumerResourceAssembler`, `WebhookConsumerDetailLinkPolicy`, and `WebhookConsumerCollectionLinkPolicy`; collections can expose `create`, while Svix/Composite consumers can expose `open-provider-portal`.
- Registered webhook consumer HAL/OpenAPI/source-generated JSON metadata so generated clients receive flattened `HalResourceOfWebhookConsumerDto` and typed `HalCollectionEmbeddedOfWebhookConsumerDto.items`.
- Updated `docs/API.md` with outgoing webhook management routes and the distinction from incoming integration callbacks.
- Validation passed:
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- API integration initially exposed missing HAL OpenAPI catalog entries for `WebhookConsumerDto`; adding the detail and embedded collection mappings fixed the contract invariants.

### 2026-07-03 Europe/Brussels - endpoint management API slice

- Added `WebhookEndpointDto`, `WebhookEndpointSubscriptionDto`, and `CreateWebhookEndpointRequestDto`; read DTOs expose status/provider/subscription metadata but no secret refs or raw secret material.
- Added endpoint list/detail/create CQRS requests and handlers with `[AuthorizeResource(ResourceKinds.Webhook, webhook:view/create)]`, `ISecureRequest` tenant attributes, manual validation, active-consumer checks, duplicate URL detection, and enabled-event-type subscription validation.
- Extended webhook repositories with tenant-bounded endpoint list/duplicate lookup and event-type batch lookup methods; EF Core implementations use explicit tenant predicates, `AsNoTracking`, active/non-archived filters, and include subscriptions/event-type metadata for read models.
- Added `GET /api/webhooks/endpoints`, `GET /api/webhooks/endpoints/{endpointId}`, and `POST /api/webhooks/endpoints` to `WebhooksController` with named routes, HAL reads, `CreatedAtRoute` on create, authenticated/write rate limiting, request timeouts, and safe RFC 7807 ProblemDetails mappings.
- Added `WebhookEndpointResourceAssembler`, endpoint HAL link policies, route names, resource descriptors, source-generated JSON metadata, and OpenAPI HAL schema catalog entries for endpoint resources and collections.
- Hardened existing Blazor client AI HAL tests by replacing direct dependencies on NSwag anonymous link type numbers with `GeneratedHalLinkTestHelper`; this keeps generated-client tests stable when new OpenAPI schemas renumber anonymous link classes.
- Updated `docs/API.md` and `docs/API_CHANGELOG.md` with the endpoint management routes and contract rules.
- Validation passed:
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
  - `git diff --check`

### 2026-07-03 Europe/Brussels - endpoint update/archive API slice

- Added `UpdateWebhookEndpointRequestDto`, `UpdateWebhookEndpointCommand`, and `ArchiveWebhookEndpointCommand` with `webhook:update` and `webhook:delete` resource authorization through `ISecureRequest` tenant/endpoint attributes.
- Added update/archive handlers that validate absolute HTTP(S) URLs, bounded delivery controls, non-empty unique event subscriptions, duplicate consumer URLs, enabled event types, and tenant-scoped not-found behavior.
- Extended `IWebhookEndpointRepository` and EF Core implementation with tracked endpoint lookup, subscription replacement, and archive update methods. Subscription replacement stays repository-owned so Application handlers do not depend on EF collection state.
- Added `PUT /api/webhooks/endpoints/{endpointId}` and `DELETE /api/webhooks/endpoints/{endpointId}` to `WebhooksController` with named routes, write rate limiting, request timeouts, explicit response metadata, and bounded ProblemDetails mapping.
- Extended endpoint HAL policy so active endpoint details can advertise authorized `update` and `delete` links, while archived endpoint details emit no mutation affordances.
- Added Application, API, and Blazor client tests for command authorization, handler validation/persistence calls, controller MediatR mapping, route metadata, ProblemDetails mapping, HAL link metadata, and generated-client HAL action-link consumption.
- Updated `docs/API.md`, `docs/API_CHANGELOG.md`, and this task file with endpoint update/archive behavior.
- Validation passed:
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

### 2026-07-03 Europe/Brussels - message/attempt API and Phase 7 Blazor UI slice

- Added safe `WebhookMessageDto` and `WebhookDeliveryAttemptDto` read models, tenant-scoped message/attempt queries, and an attempt-level manual retry command that delegates to `IWebhookDeliveryDrainService`.
- Added `GET /api/webhooks/messages`, `GET /api/webhooks/messages/{messageId}`, `GET /api/webhooks/delivery-attempts`, `GET /api/webhooks/delivery-attempts/{attemptId}`, and `POST /api/webhooks/delivery-attempts/{attemptId}/retry` with route names, HAL assemblers, resource descriptors, retry affordance policy, OpenAPI HAL schema entries, and source-generated JSON metadata.
- Added `WebhookEventTypeCatalogSyncService` plus an API startup worker so canonical registry event types are upserted into `webhook_event_types`; `GET /api/webhooks/event-types` now includes persisted IDs for endpoint subscription forms while retaining registry-driven schemas and examples.
- Regenerated `Explore.Blazor.Client/Clients/EventApiClient.g.cs` so Blazor has typed webhook event type IDs, message/attempt HAL resources, and retry/test/portal methods.
- Added `IWebhookManagementService` and `WebhookManagementService` as the generated-client anti-corruption layer for the Blazor UI. The service normalizes HAL collections, returns explicit action results, and keeps API exception handling out of Razor.
- Replaced the single-tenant instance settings webhooks placeholder with `WebhookManagementPanel`, covering status metrics, event catalog, consumer/provider portal actions, endpoint create/update/archive/test/rotation dialogs, message activity, delivery attempts, and HAL-gated manual retry. Secret rotation accepts only secret references and never displays secret material.
- Added CSS isolation for the webhook panel with dense responsive grids, stable table/action-cell sizing, and BEM-style class names.
- Validation passed:
  - `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`
  - `dotnet msbuild Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1 /v:minimal`
  - `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/WebhookEventTypeCatalogSyncServiceTests/*|/*/*/GetWebhookEventTypesQueryHandlerTests/*|/*/*/WebhookConsumerHandlersTests/*" --minimum-expected-tests 1 --log-level Error --no-progress`
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/WebhooksControllerTests/*" --minimum-expected-tests 1 --log-level Error --no-progress`
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress`
  - `dotnet build --configuration Release --verbosity quiet`
  - `git diff --check`
- Architecture verification attempted:
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress`
  - Result: failed on unrelated missing AI context disclosure docs (`dev/active/ai-context-disclosure-policy/field-classification-matrix.md` and `dev/active/ai-context-disclosure-policy/ai-context-disclosure-policy-plan.md`), not on webhook architecture rules.
- Browser visual QA attempted:
  - `dotnet run --project Explore.Blazor/Explore.Blazor.csproj --configuration Release --no-build --urls http://localhost:5144`
  - `dotnet run --project Explore.API/Explore.API.csproj --configuration Release --no-build --launch-profile https`
  - Result: blocked before the webhook settings panel could render because the API/BFF repeatedly failed database calls against `tcp://188.245.51.97:5434` for `islamu_event_db`; the admin page remained behind the unhealthy runtime stack. The desktop/mobile visual-overlap gate remains open.

### 2026-07-03 Europe/Brussels - endpoint secret rotation API slice

- Added `RotateWebhookEndpointSecretRequestDto`, `RotateWebhookEndpointSecretCommand`, and `RotateWebhookEndpointSecretCommandHandler` with `webhook:rotate-secret` resource authorization through `ISecureRequest` tenant/endpoint attributes.
- The handler validates tenant/endpoint ids, bounded secret-reference length, and a bounded previous-secret overlap window; it rejects unchanged secret references, treats missing/archived endpoints as not found, stores only secret references, increments `secretVersion`, and preserves the prior reference with `previousSecretValidUntil`.
- Extended `IWebhookEndpointRepository` and EF Core implementation with `UpdateAsync` for tracked endpoint metadata updates without subscription churn.
- Added `POST /api/webhooks/endpoints/{endpointId}/rotate-secret` to `WebhooksController` with named route `RotateWebhookEndpointSecret`, write rate limiting, default request timeout, explicit response metadata, and existing bounded endpoint ProblemDetails mapping.
- Extended endpoint HAL policy so active endpoint details can advertise authorized `rotate-secret` links; archived endpoints still emit no mutation affordances. Blazor generated-client HAL tests now cover the `rotate-secret` relation.
- Regenerated governed OpenAPI/client artifacts; `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, and `Explore.Blazor.Client/Clients/EventApiClient.g.cs` now include `RotateWebhookEndpointSecret`, `RotateWebhookEndpointSecretAsync`, and `RotateWebhookEndpointSecretRequestDto`.
- Updated `docs/API.md`, `docs/API_CHANGELOG.md`, and this task file with the rotation contract and the secret-reference-only rule.
- Validation passed:
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet msbuild Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1 /v:minimal`
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet`
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet`
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet`
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet`
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet`
  - `dotnet build --configuration Release --verbosity quiet` after client regeneration
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet` after client regeneration
  - `git diff --check`
