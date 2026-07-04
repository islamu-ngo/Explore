<!-- ABOUTME: Research and repository context for the Local/Svix webhook provider implementation plan. -->
<!-- ABOUTME: Records evidence, loaded rules, existing patterns, and open questions for future implementation agents. -->

# Webhooks Local/Svix Provider Context

Last Updated: 2026-07-04 Europe/Brussels

## Purpose

This file preserves the context used to write `webhooks-local-svix-provider-plan.md`. Implementation agents should update it whenever they discover facts that materially affect the design, scope, tests, security posture, or migration path.

## 2026-07-04 Phase 6 Re-Audit

Phase 6 incoming webhook framework is implemented and remains closed after a fresh audit against the latest request.

Verified current files:

- `Explore.Application/Contracts/Webhooks/IncomingWebhookContracts.cs` owns provider-neutral incoming callback context, verification result, verifier, and processing handler contracts.
- `Explore.Domain/IncomingWebhookMessage.cs`, `Explore.Persistence/Configurations/Entities/IncomingWebhookMessageConfiguration.cs`, and `Explore.Persistence/Repositories/IncomingWebhookMessageRepository.cs` own the tenant-scoped incoming callback idempotency/audit ledger.
- `Explore.API/Services/IncomingWebhookIntakeService.cs` reads and buffers the raw request body with `HttpRequest.EnableBuffering`, enforces body limits, rewinds the stream, verifies before JSON parsing, hashes payloads, redacts sensitive headers, captures idempotency rows, and marks processing outcomes.
- `Explore.API/Services/CoopIncomingWebhookVerifier.cs` validates Coop timestamped HMAC callbacks with fixed-time comparison.
- `Explore.API/Services/SvixIncomingWebhookVerifier.cs` validates Svix operational callbacks through `IWebhookSignatureService`, `ISecretResolver`, and the configured `Webhooks:Svix:OperationalWebhookSecretRef`.
- `Explore.API/Controllers/ModerationIntegrationController.cs` routes Coop callbacks through shared intake/capture before dispatching `ProcessCoopDecisionCallbackCommand`; duplicate captures return success without re-dispatch.
- `Explore.API/Controllers/IncomingWebhooksController.cs` exposes `POST /api/integrations/svix/operational` as an anonymous-at-edge, signature-authenticated operational callback independent of outgoing webhook provider mode.

External documentation checked through Context7:

- ASP.NET Core docs identify `HttpRequest.EnableBuffering` as the supported way to enable multiple request-body reads and rewinds.
- Svix docs require raw-body verification with `svix-id`, `svix-timestamp`, and `svix-signature` headers because framework JSON parsing/re-serialization can break signatures.

Fresh verification:

- `dotnet build --configuration Release --verbosity quiet` passed with existing warning noise.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/IncomingWebhookFrameworkTests/*" --minimum-expected-tests 1 --log-level Error --no-progress` passed: 4 tests.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/ModerationIntegrationControllerTests/*" --minimum-expected-tests 1 --log-level Error --no-progress` passed: 6 tests.
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/WebhookPersistenceTests/IncomingWebhookRepository_TryCreate_IsIdempotentPerTenantProviderMessage" --minimum-expected-tests 1 --log-level Error --no-progress` passed: 1 test.

Latest verification at 2026-07-04 01:42 CEST:

- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -m:1` passed with 0 errors and 68 existing warnings.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/IncomingWebhookFrameworkTests/*" --minimum-expected-tests 1 --log-level Error --no-progress` passed: 10/10.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ModerationIntegrationControllerTests/*" --minimum-expected-tests 1 --log-level Error --no-progress` passed: 6/6.
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/WebhookPersistenceTests/IncomingWebhookRepository_TryCreate_IsIdempotentPerTenantProviderMessage" --minimum-expected-tests 1 --log-level Error --no-progress` passed: 1/1.

No source changes were needed for Phase 6 on 2026-07-04. Continue from Phase 7 or the next unchecked item unless the user asks for deeper hardening such as encrypted payload retention, Osprey signed-callback unification, or a background dispatcher for additional incoming providers.

## Research Summary

### Local repository evidence

| Area | Evidence | Implementation impact |
| --- | --- | --- |
| Agent contract | `AGENTS.md` is canonical. `.github/copilot-instructions.md` points back to `AGENTS.md`. | Implementation must start from the contribution contract, classify intents, load matching rules, and honor critical invariants. |
| Missing include | User-provided AGENTS preamble referenced `@RTK.md`, but `RTK.md` was not present in the repo root or indexed files. | Proceed with canonical repo docs. If `RTK.md` appears later, it must be read before implementation continues. |
| Dev-docs workflow | `.claude/commands/dev-docs.md` requires plan/context/tasks in `dev/active/[task-name]/`. | This folder and three files implement that workflow for `webhooks-local-svix-provider`. |
| Generic outbox | `Explore.Domain/OutboxMessage.cs`, `Explore.API/BackgroundServices/OutboxProcessor.cs`, `Explore.Persistence/Repositories/OutboxRepository.cs`. | Outgoing webhooks should use post-commit durable dispatch and must not call providers in business transactions. |
| Dispatcher | `Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs` routes known event types and fails closed on unknown types. | Webhook routing must register explicit known event types and avoid silent drops. |
| Specialized side-effect pattern | `EmailDispatchOutbox`, `EmailDispatchAttempt`, `EmailDispatchReceipt`, `EmailDispatchProcessor`, `EmailDispatchAdminController`. | Webhooks should mirror this: generic business outbox trigger plus webhook-specific message/attempt state. |
| Incoming Coop callback | `Explore.API/Services/CoopWebhookSignatureValidator.cs` reads raw body, validates timestamp, checks HMAC with fixed-time comparison, and resets request body. | Reuse or adapt this pattern for shared incoming webhook verification. |
| Incoming Osprey/Coop controller | `Explore.API/Controllers/ModerationIntegrationController.cs` has Coop and Osprey callback endpoints with route names, rate limits, and ProblemDetails responses. | Incoming callback framework can refactor existing routes, but Coop must stay independent from outgoing provider mode. |
| Coop callback handler | `ProcessCoopDecisionCallbackCommandHandler` persists decisions idempotently and checks tenant consistency. | Incoming webhook framework must keep idempotency and tenant checks before sensitive side effects. |
| Heavy redaction payload policy | `EventModerationOutboxMessageFactory` omits event id/title/slug/URL/image/object key/original content for heavy redaction notification payloads. | Webhook payload builders must preserve this minimization. |
| HAL convention | `docs/QUICK_REFERENCE.md`, `api-hateoas.md`, `docs/BLAZOR.md`. | Webhook UI actions must be HAL-driven, never role/claim-inferred in Blazor. |
| Secret provider | `Explore.Secrets/Abstractions/ISecretProvider.cs`, `EnvironmentSecretProvider`, `SetupSecretProvider`. | Endpoint secrets and Svix tokens should be addressed by secret refs and registry updates, not ad hoc configuration leakage. |
| Configuration gap | `docs/CONFIGURATION.md` lists secret key families but no webhook namespace. | Add `webhooks/*` secret naming and configuration docs. |
| Metrics | `Explore.Application/Telemetry/BusinessMetrics.cs` centralizes bounded business counters. | Webhook counters should be added there with bounded tag cardinality. |
| Health checks | `Explore.API/Program.cs` registers health checks for email, idempotency, AI, storage, Cerbos, and others. | Add webhook health checks that respect disabled mode. |
| No existing webhooks | Repository search found Coop/Osprey callback files but no outgoing webhook subsystem, Svix provider, webhook DbSets, or Svix package. | Implementation is greenfield within established architecture. |

### Official Svix documentation through Context7 MCP

Context7 package/library lookups identified the Svix documentation as `/svix/svix-webhooks` and `/websites/svix`.

Evidence used:

- Svix uses applications, endpoints, messages, attempts, and event types as core concepts.
- Svix has official C#/.NET API usage and signature verification support.
- C# quickstart uses `SvixClient` and `MessageIn(eventType: ..., payload: ..., eventId: ...)`.
- Svix supports a configurable base URL through client options for self-hosted Svix.
- Svix supports idempotency using an `Idempotency-Key` request header.
- Svix App Portal URLs are generated by the backend and then passed to the frontend or embedded.
- Svix verification uses raw request bodies and the headers `svix-id`, `svix-timestamp`, and `svix-signature`.
- Manual verification signs `{svix-id}.{svix-timestamp}.{raw body}` with HMAC-SHA256 using the decoded `whsec_` secret material.
- Verification must use timestamp tolerance and constant-time signature comparison.

Implementation impact:

- `LocalWebhookDeliveryProvider` can be Svix-compatible at the signature layer.
- `SvixWebhookDeliveryProvider` should use the official `Svix` package instead of hand-rolling the API client.
- Svix App Portal token generation must remain backend-only.
- Incoming Svix operational callbacks need raw body verification before JSON parsing.

### 2026-07-02 implementation context update

The first implementation slice is complete in the Application layer only. It intentionally does not add persistence, HTTP delivery, the `Svix` NuGet package, API endpoints, or Blazor UI yet.

Implemented files:

- `Explore.Application/Contracts/Webhooks/WebhookContracts.cs`
- `Explore.Application/Contracts/Webhooks/WebhookEventTypeDescriptor.cs`
- `Explore.Application/Contracts/Webhooks/WebhookEventNames.cs`
- `Explore.Application/Contracts/Webhooks/IWebhookEventPublisher.cs`
- `Explore.Application/Contracts/Webhooks/IWebhookDeliveryProvider.cs`
- `Explore.Application/Contracts/Webhooks/IWebhookEndpointManager.cs`
- `Explore.Application/Contracts/Webhooks/IWebhookSignatureService.cs`
- `Explore.Application/Contracts/Webhooks/IWebhookPayloadBuilder.cs`
- `Explore.Application/Contracts/Webhooks/IWebhookEventTypeRegistry.cs`
- `Explore.Application/Contracts/Webhooks/IWebhookEventSchemaProvider.cs`
- `Explore.Application/Contracts/Webhooks/IncomingWebhookContracts.cs`
- `Explore.Application/Webhooks/WebhookEventTypeRegistry.cs`
- `Explore.Application/Webhooks/WebhookEventSchemaProvider.cs`
- `Explore.Application/Webhooks/DefaultWebhookPayloadBuilder.cs`
- `Explore.Application/ApplicationServicesRegistration.cs`
- `Event.Application.UnitTests/Webhooks/WebhookEventTypeRegistryTests.cs`
- `Event.Application.UnitTests/Webhooks/DefaultWebhookPayloadBuilderTests.cs`

Design decisions:

- Endpoint management contract models use `CreateWebhookEndpointInput` and `UpdateWebhookEndpointInput`, not `*Request`, because architecture tests reserve `Request` suffixes for query request classes in `Queries` namespaces.
- Payload construction is allow-list based from the canonical event type descriptor. Unknown event types and missing required fields fail closed.
- Heavy moderation webhook payloads are generic and linkless. Tests prove unsafe fields such as title, slug, public URL, image URI, source actor id, raw provider error, and moderator free text are omitted.
- The event schema provider emits Draft 7-compatible envelope schemas and example payloads for the initial public catalog.
- Context7 documentation for Svix C# verification and message creation was used to keep the contracts aligned with future `LocalWebhookDeliveryProvider` and `SvixWebhookDeliveryProvider` work.

Validation results:

- `dotnet build --configuration Release --verbosity quiet` passed.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/Webhook*/*|/*/*/DefaultWebhookPayloadBuilderTests/*" --minimum-expected-tests 1` passed. The command executed the Application test project and reported 1719 passing tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 239 succeeded and 1 existing skipped API metadata test.
- Existing warning volume remains, including known `AutoMapper` and `Microsoft.OpenApi` vulnerability warnings.

### 2026-07-02 Phase 2 persistence implementation update

The canonical Domain/Persistence slice is complete for the outgoing and incoming webhook ledgers. It intentionally does not add HTTP delivery workers, the `Svix` NuGet package, API endpoints, authorization actions, OpenAPI generation, or Blazor UI yet.

Implemented files:

- `Explore.Domain/WebhookConsumer.cs`
- `Explore.Domain/WebhookEventType.cs`
- `Explore.Domain/WebhookEndpoint.cs`
- `Explore.Domain/WebhookEndpointSubscription.cs`
- `Explore.Domain/WebhookMessage.cs`
- `Explore.Domain/WebhookDeliveryAttempt.cs`
- `Explore.Domain/WebhookProviderLink.cs`
- `Explore.Domain/IncomingWebhookMessage.cs`
- `Explore.Application/Contracts/Persistence/IWebhookConsumerRepository.cs`
- `Explore.Application/Contracts/Persistence/IWebhookEventTypeRepository.cs`
- `Explore.Application/Contracts/Persistence/IWebhookEndpointRepository.cs`
- `Explore.Application/Contracts/Persistence/IWebhookMessageRepository.cs`
- `Explore.Application/Contracts/Persistence/IWebhookDeliveryAttemptRepository.cs`
- `Explore.Application/Contracts/Persistence/IWebhookProviderLinkRepository.cs`
- `Explore.Application/Contracts/Persistence/IIncomingWebhookMessageRepository.cs`
- `Explore.Persistence/Configurations/Entities/WebhookConsumerConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/WebhookEventTypeConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/WebhookEndpointConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/WebhookEndpointSubscriptionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/WebhookMessageConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/WebhookDeliveryAttemptConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/WebhookProviderLinkConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/IncomingWebhookMessageConfiguration.cs`
- `Explore.Persistence/Repositories/WebhookConsumerRepository.cs`
- `Explore.Persistence/Repositories/WebhookEventTypeRepository.cs`
- `Explore.Persistence/Repositories/WebhookEndpointRepository.cs`
- `Explore.Persistence/Repositories/WebhookMessageRepository.cs`
- `Explore.Persistence/Repositories/WebhookDeliveryAttemptRepository.cs`
- `Explore.Persistence/Repositories/WebhookProviderLinkRepository.cs`
- `Explore.Persistence/Repositories/IncomingWebhookMessageRepository.cs`
- `Explore.Persistence/Migrations/20260702192022_AddWebhookSubsystem.cs`
- `Explore.Persistence/Migrations/20260702192022_AddWebhookSubsystem.Designer.cs`
- `Event.Persistence.IntegrationTests/Repositories/WebhookPersistenceTests.cs`

Modified integration points:

- `Explore.Persistence/ExploreDbContext.DbSets.cs`
- `Explore.Persistence/ExploreDbContext.QueryFilters.cs`
- `Explore.Persistence/QueryFilters/TenantFilterBypassReasons.cs`
- `Explore.Persistence/PersistenceServicesRegistration.cs`
- `Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs`

Design decisions:

- ISLAMU now owns canonical webhook state independently from LocalProvider or SvixProvider.
- `WebhookEventType` is global, while consumers, endpoints, subscriptions, messages, attempts, provider links, and incoming callback rows are tenant-scoped.
- Tenant-scoped webhook repositories use explicit tenant predicates with `TenantFilterBypassReasons.WebhookTenantOperation`.
- Cross-tenant worker/cleanup scans use `TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue`.
- JSON payload/schema/header fields are mapped to PostgreSQL `jsonb`.
- `WebhookMessage.PayloadJson` can be cleared by retention cleanup without deleting audit metadata such as event type, aggregate id, status, hash, or provider ids.
- Provider ids are stored in `WebhookProviderLink`, not treated as primary ISLAMU state.
- Incoming provider callbacks have tenant/provider/message-id uniqueness for idempotency.

Validation results:

- `dotnet build --configuration Release --verbosity quiet` passed.
- `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passed with 306 succeeded.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed with 1720 succeeded.
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed with 196 succeeded.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 239 succeeded and 1 existing skipped API metadata test.
- Existing warning volume remains, including known `AutoMapper` and `Microsoft.OpenApi` vulnerability warnings.

### 2026-07-03 Phase 5 SvixProvider core implementation update

The Svix delivery-provider core is implemented. This slice does not add the Svix App Portal route, Svix event-type sync service, webhook management API, incoming webhook framework, or Blazor management UI yet.

Documentation and package evidence:

- Context7 MCP was used for current Svix C# SDK documentation, including `SvixClient`, `ApplicationIn`, `MessageIn`, `Application.GetOrCreateAsync`, `Message.CreateAsync`, `MessageCreateOptions.IdempotencyKey`, self-hosted base URL options, and backend-only App Portal generation.
- NuGet package search confirmed the official `Svix` package latest version used here as `1.96.1`.
- Local reflection against `~/.nuget/packages/svix/1.96.1/lib/net8.0/Svix.dll` verified the runtime constructor and method signatures used by the adapter.

Implemented files:

- `Directory.Packages.props`
- `Explore.Infrastructure/Explore.Infrastructure.csproj`
- `Explore.Domain/Secrets/SecretDefinitionRegistry.cs`
- `Explore.Infrastructure/Configuration/WebhookOptions.cs`
- `Explore.Infrastructure/Configuration/WebhookOptionsValidator.cs`
- `Explore.Application/Contracts/Persistence/IWebhookProviderLinkRepository.cs`
- `Explore.Persistence/Repositories/WebhookProviderLinkRepository.cs`
- `Explore.Infrastructure/Webhooks/SvixWebhookClientContracts.cs`
- `Explore.Infrastructure/Webhooks/SvixWebhookClient.cs`
- `Explore.Infrastructure/Webhooks/SvixWebhookDeliveryProvider.cs`
- `Explore.Infrastructure/Webhooks/RuntimeWebhookDeliveryProvider.cs`
- `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- `Explore.Secrets/Extensions/ServiceCollectionExtensions.cs`
- `Explore.Infrastructure.Tests/Infrastructure/Webhooks/SvixWebhookDeliveryProviderTests.cs`
- `Explore.Infrastructure.Tests/Infrastructure/Webhooks/WebhookProviderResolverTests.cs`

Design decisions:

- `SvixWebhookClient` is an Infrastructure adapter around the official SDK, hidden behind `ISvixWebhookClient` so provider behavior can be tested without a live Svix server.
- The Svix token is resolved through `ISecretResolver` using the known secret definition `webhooks.svix.auth_token`; it is never read from frontend configuration or exposed to Blazor.
- `AddSecretManagement` now also registers secret resolution so API/Blazor composition that already calls secret management can resolve backend provider tokens.
- Canonical `WebhookMessage.MessageId` is used as both Svix `eventId` and the `Idempotency-Key`, preserving at-least-once safety across retries.
- `Message.messageInRaw` is used so the canonical ISLAMU JSON envelope is sent unchanged rather than reserialized by the SDK adapter.
- Consumer messages map to deterministic Svix application UIDs. Tenant-level messages without a consumer use a deterministic tenant UID.
- Message-level provider links intentionally do not set `ExternalAppId`, because `webhook_provider_links` has a unique app-link index per tenant/provider/app and one Svix app can receive many messages.
- `RuntimeWebhookDeliveryProvider` now delegates `Svix` and `Composite` modes to the real Svix provider. Local and DryRun/Disabled modes continue to use their existing providers.

Validation results:

- `dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --verbosity quiet` passed.
- `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed.
- Focused Svix provider tests in `Explore.Infrastructure.Tests` passed.
- `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed with 306/306.
- `dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed with 202/202.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed with 578/578.
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed with 196/196.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed with 239/240, 1 known skipped API metadata test.
- `dotnet build --configuration Release --verbosity quiet` passed.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed with 1732/1732.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed with 1454/1457, 3 known skips.
- LSP diagnostics returned no diagnostics for the changed Svix provider, configuration, registration, secret, repository contract, repository implementation, and test files.
- `git diff --check` passed.

Remaining next step:

1. Continue with Phase 4 webhook management API/HAL/authorization if the goal is user-administered LocalProvider endpoints.
2. Continue with Phase 5 App Portal and event-type sync if the goal is advanced Svix tenant administration first.
3. Continue with Phase 6 incoming webhook framework if Coop/Osprey callback unification is the priority.

### 2026-07-03 Phase 5 App Portal and event-type sync implementation update

The backend Svix App Portal service, event-type sync service, and startup event-type sync worker are implemented. This slice still does not expose a public API route, authorization action, HAL link, or Blazor UI button for opening the portal.

Documentation and SDK evidence:

- Context7 Svix docs were rechecked for C# App Portal access and event type create/update usage.
- Reflection against `Svix` `1.96.1` verified:
  - `Authentication.AppPortalAccessAsync(string, AppPortalAccessIn, AuthenticationAppPortalAccessOptions, CancellationToken)`
  - `AppPortalAccessIn.SessionId`, `ReadOnly`, `Expiry`, and `FeatureFlags`
  - `AuthenticationAppPortalAccessOptions.IdempotencyKey`
  - `EventType.CreateAsync(EventTypeIn, EventTypeCreateOptions, CancellationToken)`
  - `EventType.UpdateAsync(string, EventTypeUpdate, CancellationToken)`
  - `EventTypeCreateOptions.IdempotencyKey`
  - `EventTypeIn`/`EventTypeUpdate` fields `Name`, `Description`, `GroupName`, and `Schemas`

Implemented files:

- `Explore.Application/Contracts/Webhooks/IWebhookProviderPortalService.cs`
- `Explore.Application/Contracts/Webhooks/IWebhookProviderEventTypeSyncService.cs`
- `Explore.Infrastructure/Webhooks/SvixWebhookApplicationMapper.cs`
- `Explore.Infrastructure/Webhooks/SvixWebhookFailureClassifier.cs`
- `Explore.Infrastructure/Webhooks/SvixAppPortalService.cs`
- `Explore.Infrastructure/Webhooks/SvixEventTypeSyncService.cs`
- `Explore.API/BackgroundServices/SvixWebhookEventTypeSyncWorker.cs`
- `Explore.Infrastructure.Tests/Infrastructure/Webhooks/SvixAppPortalServiceTests.cs`
- `Explore.Infrastructure.Tests/Infrastructure/Webhooks/SvixEventTypeSyncServiceTests.cs`

Modified files:

- `Explore.Infrastructure/Webhooks/SvixWebhookClientContracts.cs`
- `Explore.Infrastructure/Webhooks/SvixWebhookClient.cs`
- `Explore.Infrastructure/Webhooks/SvixWebhookDeliveryProvider.cs`
- `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- `Explore.API/Program.cs`

- `dev/active/webhooks-local-svix-provider/webhooks-local-svix-provider-tasks.md`
- `dev/active/webhooks-local-svix-provider/webhooks-local-svix-provider-context.md`

Design decisions:

- Provider-facing portal and event-type sync contracts live in Application so future API handlers can depend on project abstractions, not Infrastructure or Svix SDK types.
- `SvixAppPortalService` issues backend-created, short-lived portal access only when webhooks are enabled, provider mode is `Svix` or `Composite`, `AppPortalEnabled` is true, and the caller supplies a session id.
- App Portal expiry defaults to 15 minutes and is clamped between 1 minute and 1 hour.
- If a consumer id is provided and the consumer is missing for that tenant, portal access fails with `webhook_consumer_not_found` instead of creating a deterministic app for a nonexistent owner.
- `SvixEventTypeSyncService` no-ops outside `Svix`/`Composite` modes or when `SyncEventTypesOnStartup` is false.
- `SvixWebhookEventTypeSyncWorker` runs once on API startup outside Testing/OpenAPI-generation hosts, resolves `IWebhookProviderEventTypeSyncService` through a scope, and logs only bounded synced/failure counts.
- Event-type sync uses the canonical Application registry and schema provider. It sends the generated JSON Schema through the SDK `Schemas` field and uses deterministic idempotency keys `svix-event-type:{name}:v{schemaVersion}`.
- The SDK adapter implements create-then-update-on-409 for event type upsert.
- `SvixWebhookApplicationMapper` centralizes tenant/consumer to Svix app UID/name/metadata logic for delivery and portal services.
- `SvixWebhookFailureClassifier` centralizes bounded Svix failure categories so raw provider errors do not leak into results.

Validation results:

- `dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --verbosity quiet` passed.
- `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed with 584/584.
- `dotnet build --configuration Release --verbosity quiet` passed.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed with 1732/1732.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed with 239/240, 1 known skipped API metadata test.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed with 1454/1457, 3 known skips.
- LSP diagnostics returned no diagnostics for the new and modified App Portal/event-type sync files.
- `git diff --check` passed.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed after startup worker registration.
- LSP diagnostics returned no diagnostics for `SvixWebhookEventTypeSyncWorker` and `Program.cs`.
- `dotnet build --configuration Release --verbosity quiet` passed after startup worker registration.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed after startup worker registration with 239/240, 1 known skipped API metadata test.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --log-level Error --no-progress` passed after startup worker registration with 1454/1457, 3 known skips.

Next implementation options:

1. Start Phase 4 webhook management API for LocalProvider endpoint CRUD and manual retry.
2. Add HAL gating and Blazor affordance for the Svix App Portal route.
3. Start Phase 6 incoming webhook framework if Coop/Osprey callback unification is the priority.

### 2026-07-03 Phase 5 Svix App Portal API route implementation update

The backend Svix App Portal route is now exposed through the API and protected by the same MediatR authorization pipeline used by other mutating commands. This slice still does not add webhook CRUD endpoints, HAL provider links, OpenAPI/client regeneration, or the Blazor "Open Advanced Webhook Portal" action.

Implemented files:

- `Explore.Application/DTOs/Webhooks/OpenSvixAppPortalRequestDto.cs`
- `Explore.Application/DTOs/Webhooks/WebhookProviderPortalAccessDto.cs`
- `Explore.Application/Responses/WebhookProviderPortalAccessCommandResponse.cs`
- `Explore.Application/Features/Webhooks/Requests/Commands/OpenSvixAppPortalCommand.cs`
- `Explore.Application/Features/Webhooks/Handlers/Commands/OpenSvixAppPortalCommandHandler.cs`
- `Explore.API/Controllers/WebhooksController.cs`
- `Event.Application.UnitTests/Features/Webhooks/OpenSvixAppPortalCommandHandlerTests.cs`
- `Event.API.IntegrationTests/Features/WebhooksControllerTests.cs`
- `cerbos/policies/islamuevent_webhook.yaml`
- `cerbos/policies/_schemas/islamuevent_webhook.json`

Modified files:

- `Explore.Application/Authorization/AuthorizationActions.cs`
- `Explore.Application/Authorization/ResourceKinds.cs`
- `Explore.Application/Authorization/MachineScopeMapping.cs`
- `Explore.Application/Serialization/ExploreJsonContext.cs`
- `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`
- `Explore.Infrastructure/Services/FallbackAuthorizationService.MachineCaller.cs`
- `Explore.API/Middleware/IdempotencyMiddleware.cs`
- `Explore.API/Hateoas/RouteNames.cs`
- `Event.API.IntegrationTests/Features/IdempotencyMiddlewareTests.cs`

Design decisions:

- `POST /api/webhooks/svix/app-portal` is controller-authenticated and delegates to `OpenSvixAppPortalCommand` instead of calling Infrastructure directly.
- `OpenSvixAppPortalCommand` uses `[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.OpenProviderPortal)]` plus `ISecureRequest` tenant/provider attributes, so both Cerbos and fallback authorization evaluate the same canonical webhook resource.
- The new `islamuevent_webhook` Cerbos policy grants instance admins all webhook actions and tenant admins the initial `webhook:*` management actions for their tenant scope.
- `FallbackAuthorizationService` treats `islamuevent_webhook` as a tenant-scoped admin resource; machine-scope mapping requires `admin:tenant` or `admin:instance` style scope coverage.
- The API success payload contains only short-lived portal URL/token metadata. The Svix API token remains server-side through the existing secret-provider-backed `SvixAppPortalService`.
- `IdempotencyMiddleware` skips this route even when a caller supplies `Idempotency-Key`, because caching a short-lived portal token for the middleware's 24-hour replay window would return expired credentials.
- Provider and configuration failures are mapped to bounded RFC7807 ProblemDetails responses with stable codes instead of raw Svix exception text.

Validation results:

- `dotnet build --configuration Release --verbosity quiet` passed.
- `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet` passed with 1736/1736.
- `dotnet test Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet` passed with 239/240, 1 known skipped API metadata test.
- `dotnet test Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet` passed with 1459/1462, 3 known skips.
- `git diff --check` passed.

### Tavily MCP research evidence

Tavily research/search/extract was used for current web documentation and security context.

Sources:

- [Svix retries](https://docs.svix.com/retries)
- [Svix overview](https://docs.svix.com/overview)
- [Svix event types](https://docs.svix.com/event-types)
- [Svix App Portal](https://docs.svix.com/app-portal)
- [Svix idempotency](https://docs.svix.com/idempotency)
- [Svix manual payload verification](https://docs.svix.com/receiving/verifying-payloads/how-manual)
- [Svix security](https://docs.svix.com/security)
- [Svix open-source server README](https://github.com/svix/svix-webhooks/blob/main/README.md)
- [OWASP SSRF Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Server_Side_Request_Forgery_Prevention_Cheat_Sheet.html)

Evidence used:

- Svix considers 2xx responses successful. Non-2xx, redirects, timeouts, and failed network calls are delivery failures.
- Svix retry schedule starts immediately, then 5 seconds, 5 minutes, 30 minutes, 2 hours, 5 hours, and 10-hour intervals.
- Svix can disable persistently failing endpoints.
- Svix event types are the subscription/filtering mechanism and can be documented with JSON Schema.
- Svix idempotency applies to POST requests and stores the first response for a matching auth token and key for a limited window.
- Svix App Portal lets consumers add endpoints, debug delivery, inspect, and replay webhooks.
- Svix security docs call out webhooks as especially SSRF-prone because consumers can configure arbitrary delivery URLs.
- Svix open-source server is real infrastructure: Rust service, PostgreSQL dependency, optional Redis for queue/cache, Docker Compose examples, JWT secret, and private-network blocking by default.
- OWASP SSRF guidance supports URL/IP validation, network segmentation, allow-listing where possible, and blocking internal metadata/private destinations.

Implementation impact:

- LocalProvider must block internal/private destinations by default and must not follow redirects.
- LocalProvider retry schedule can be simpler and less aggressive than Svix, but should follow the same success/failure semantics.
- SvixProvider must use idempotency keys and store provider IDs.
- Self-hosting docs must clearly state that LocalProvider works without Svix, while SvixProvider requires additional infrastructure.

## Loaded Repo Documents

| File | Key implementation constraints |
| --- | --- |
| `AGENTS.md` | Critical rules: repositories return entities, validators are manual, ids use the correct scalar types, GET `[AllowAnonymous]`, write `[Authorize]`, every file starts with two ABOUTME lines, HAL links are UI source of truth. |
| `.github/copilot-instructions.md` | Tool-specific pointer back to `AGENTS.md`; no backward-compatibility shims; run project-specific tests. |
| `.claude/contract/intents.yaml` | No first-class webhook intent; implementation must compose CQRS, EF migration, API, HAL, auth, Blazor, and OpenAPI intents. |
| `.claude/commands/dev-docs.md` | Plan/context/tasks required under `dev/active/[task-name]/`. |
| `dev/active/README.md` | Active docs are living implementation state and must be updated as work proceeds. |
| `docs/QUICK_REFERENCE.md` | API shape, route names, rate limits, HAL gating, tenant isolation, build/test policy. |
| `docs/GOVERNANCE.md` | Clean Architecture boundaries, API contract rules, transactional unit-of-work guidance. |
| `docs/OPERATIONS.md` | Outbox processor behavior, Basic Email Dispatch Mode, safe ops, webhook URL/token redaction in deploy scripts. |
| `docs/OUTBOX_PATTERN.md` | Generic and specialized outbox patterns, idempotency, retries, dead letters, handler/controller restrictions. |
| `docs/NOTIFICATIONS.md` | Notification fanout, dedup keys, heavy redaction payload minimization. |
| `docs/EMAIL_NOTIFICATIONS.md` | Current email pipeline and MailKit SMTP posture. |
| `docs/BLAZOR.md` | BFF boundary, generated DTOs, HAL link preservation, no browser tokens. |
| `docs/AUTHORIZATION.md` | Runtime authorization provider, local fallback, HATEOAS link authorization. |
| `docs/CONFIGURATION.md` | Secret/provider configuration model and existing secret key families. |
| `docs/MULTI_TENANCY.md` | Tenant resolution and tenant filter expectations. |

## Loaded Rule Files

| Rule | Implementation impact |
| --- | --- |
| `.claude/rules/api-controllers.md` | Controllers stay thin, routes have names, endpoints have classifications, responses use ProblemDetails metadata. |
| `.claude/rules/api-hateoas.md` | Link generation is explicit, capability-based, separate for detail/collection, and fail closed. |
| `.claude/rules/application-layer.md` | Handlers are single-purpose, validators manual, no outward dependencies. |
| `.claude/rules/domain.md` | Domain stays pure and explicit. |
| `.claude/rules/efcore-persistence.md` | DbContext pooling, named query filters, AsNoTracking for reads, tenant filter bypass only by documented reason. |
| `.claude/rules/efcore-migrations.md` | Migrations reversible, snapshots accurate, lookup sync controlled. |
| `.claude/rules/blazor-client.md` | InteractiveAuto, MudBlazor v9, wrappers, CSS isolation/BEM, accessibility, HAL affordances. |
| `.claude/rules/blazor-server.md` | BFF boundary, no token exposure, tenant/setup-secret forwarding handlers. |
| `.claude/rules/tests.md` | Never delete tests, project-specific test commands, E2E must use real infra when required. |

## Loaded Skills

| Skill | Why it mattered |
| --- | --- |
| `agentic-research` | Local-first research, official docs via Context7, external Tavily only for necessary current facts. |
| `clean-architecture-rules` | Layer boundaries and dependency direction. |
| `cqrs-mediatr-guidelines` | Command/query, handler, validator, DTO, and test conventions. |
| `dotnet-efcore-guidelines` | Repository/entity/migration/query filter conventions. |
| `outbox-pattern` | Transactional outbox and specialized side-effect ledger design. |
| `auth-patterns` | BFF token handling, API auth, HAL gating, local/provider authorization. |
| `blazor-bff-patterns` | Browser never sees tokens; service layer talks to BFF/generated client. |
| `blazor-ui-conventions` | MudBlazor v9, render modes, dialogs, HAL action affordances. |
| `design-system` | Wrapper components and design token discipline. |
| `error-tracking` | OpenTelemetry, Prometheus, Loki, safe ProblemDetails, bounded logs. |
| `aspire` | Future optional Svix composition and distributed app operations. |
| `source-command-check` | Canonical build and project-level verification policy. |
| `omo:ulw-plan` | Explore-first planning discipline. User requested the repo `dev-docs` workflow, so output stayed in `dev/active` rather than `.omo/plans`. |

## Existing Pattern Details

### Generic outbox behavior

Relevant files:

- `Explore.Domain/OutboxMessage.cs`
- `Explore.Persistence/Configurations/Entities/OutboxMessageConfiguration.cs`
- `Explore.Persistence/Repositories/OutboxRepository.cs`
- `Explore.Application/Contracts/Persistence/IOutboxRepository.cs`
- `Explore.API/BackgroundServices/OutboxProcessor.cs`
- `Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs`

Facts:

- Outbox IDs are `Guid` UUIDv7.
- Payload is jsonb.
- Status lifecycle includes pending, processing, completed, failed, dead-lettered.
- `OutboxProcessor` claims rows and delegates to `IOutboxMessageDispatcher`.
- Failed dispatches use bounded error fields and retry/dead-letter behavior.
- Unknown outbox event types throw, which fails closed.

Webhook implication:

- Create explicit webhook-capable event dispatch paths.
- Do not route arbitrary outbox rows by naming convention alone.
- Use a specialized webhook ledger for fanout and endpoint attempts, not only generic outbox rows.

### Email dispatch as the closest implementation analogue

Relevant files:

- `Explore.Domain/EmailDispatchOutbox.cs`
- `Explore.Domain/EmailDispatchAttempt.cs`
- `Explore.Domain/EmailDispatchReceipt.cs`
- `Explore.Persistence/Repositories/EmailDispatchOutboxRepository.cs`
- `Explore.API/BackgroundServices/EmailDispatchProcessor.cs`
- `Explore.API/Controllers/EmailDispatchAdminController.cs`
- `Explore.API/HealthChecks/EmailDispatchHealthCheck.cs`

Facts:

- Email dispatch has its own durable state machine, attempt records, receipts, recovery scans, and admin controls.
- Cross-tenant queue scanning uses explicit tenant-filter bypass reasons.
- User-facing/admin status rows are sanitized and do not expose recipient body or raw provider errors.
- Hosted service scheduling is separate from PostgreSQL-owned delivery state.

Webhook implication:

- `WebhookMessage` and `WebhookDeliveryAttempt` should be authoritative for delivery state.
- Delivery workers can scan cross-tenant queues only through repository methods with documented bypasses.
- Admin API must return sanitized delivery status, not raw payloads or secret-bearing URLs.

### Coop incoming callback pattern

Relevant files:

- `Explore.API/Controllers/ModerationIntegrationController.cs`
- `Explore.API/Services/CoopWebhookSignatureValidator.cs`
- `Explore.Application/Features/EventReporting/Handlers/Commands/ProcessCoopDecisionCallbackCommandHandler.cs`
- `Event.API.IntegrationTests/Features/ModerationIntegrationControllerTests.cs`

Facts:

- Coop signature validator uses timestamp header plus HMAC signature header.
- Request body is read as raw bytes with buffering and max body limit.
- Timestamp skew is bounded.
- Signatures are compared in fixed time.
- Body stream is reset for downstream JSON parsing.
- Handler checks tenant resolution and external decision id idempotency.

Webhook implication:

- Shared incoming webhook verification should preserve these properties.
- Do not parse JSON before signature verification.
- Incoming callback idempotency should become explicit and provider-agnostic.

### Authorization and HAL

Relevant files:

- `docs/AUTHORIZATION.md`
- `docs/BLAZOR.md`
- `.claude/rules/api-hateoas.md`
- `Explore.API/Hateoas/RouteNames.cs`

Facts:

- HAL links are the single source of truth for client affordances.
- Local fallback authorization denies unknown resource/action combinations by default.
- Blazor preserves generated DTO HAL links and does not inspect tokens/claims in the browser.

Webhook implication:

- Webhook management APIs need resource-scoped actions and link policies.
- Blazor pages must render buttons from links only.
- Tests should assert absent links for unauthorized users.

## External Design Constraints

### Svix-compatible signatures

Header set:

```text
svix-id
svix-timestamp
svix-signature
```

Signed content:

```text
{svix-id}.{svix-timestamp}.{raw-body}
```

Algorithm:

```text
HMAC-SHA256(signedContent, base64Decode(secretWithoutWhsecPrefix))
```

Signature header shape:

```text
v1,{base64_signature}
```

Implementation notes:

- Use raw body bytes/string exactly as sent.
- Never verify against reserialized JSON.
- Enforce timestamp tolerance.
- Use constant-time comparison.
- Support multiple signatures if needed during rotation.

### Svix delivery semantics to mirror locally

- 2xx within timeout is success.
- Non-2xx is failure.
- Redirect is failure.
- Timeout/network error is failure.
- Delivery is at least once.
- Consumers must be idempotent.

LocalProvider retry schedule selected for ISLAMU:

```text
1: now
2: +30 seconds
3: +5 minutes
4: +30 minutes
5: +2 hours
6: +6 hours
7: +12 hours
8: +24 hours
```

Reason:

- Keeps a familiar Svix-style exponential shape.
- Avoids hammering small self-hosted instances and small third-party endpoints.
- Fits LocalProvider as lightweight, not a full advanced delivery platform.

### SSRF threat model

Threat:

- LocalProvider calls user-supplied URLs from server-side infrastructure.
- Attackers can try to reach metadata services, local services, private networks, or internal admin endpoints.

Required blocks:

- localhost names
- loopback IPv4/IPv6
- RFC1918 private IPv4 ranges
- link-local ranges
- cloud metadata IPs
- internal DNS resolutions
- redirects to blocked destinations

Required controls:

- scheme allow-list
- host validation
- IP validation
- DNS result validation
- redirects disabled
- explicit operator CIDR allow-list only when needed
- tests for IPv4, IPv6, DNS, metadata, redirects

## Proposed File/Namespace Map

Exact paths may change slightly to match discovered local organization during implementation, but layer ownership should not change.

### Domain

```text
Explore.Domain/Webhooks/WebhookConsumer.cs
Explore.Domain/Webhooks/WebhookEndpoint.cs
Explore.Domain/Webhooks/WebhookEndpointSubscription.cs
Explore.Domain/Webhooks/WebhookEventType.cs
Explore.Domain/Webhooks/WebhookMessage.cs
Explore.Domain/Webhooks/WebhookDeliveryAttempt.cs
Explore.Domain/Webhooks/WebhookProviderLink.cs
Explore.Domain/Webhooks/IncomingWebhookMessage.cs
Explore.Domain/Webhooks/Webhook*.cs enum files
```

### Application

```text
Explore.Application/Contracts/Webhooks/*.cs
Explore.Application/Contracts/Persistence/IWebhook*Repository.cs
Explore.Application/Features/Webhooks/Commands/*
Explore.Application/Features/Webhooks/Queries/*
Explore.Application/Features/Webhooks/DTOs/*
Explore.Application/Features/Webhooks/Validators/*
Explore.Application/Webhooks/WebhookEventTypeRegistry.cs
Explore.Application/Webhooks/WebhookPayloadBuilder.cs
Explore.Application/Webhooks/WebhookEventSchemaProvider.cs
```

### Persistence

```text
Explore.Persistence/Configurations/Entities/Webhooks/*.cs
Explore.Persistence/Repositories/Webhooks/*.cs
Explore.Persistence/Migrations/*AddWebhookSubsystem.cs
Explore.Persistence/ExploreDbContext.DbSets.cs
Explore.Persistence/ExploreDbContext.ModelConfiguration.cs
Explore.Persistence/PersistenceServicesRegistration.cs
```

### Infrastructure

```text
Explore.Infrastructure/Webhooks/Local/*
Explore.Infrastructure/Webhooks/Svix/*
Explore.Infrastructure/Webhooks/Common/*
Explore.Infrastructure/InfrastructureServicesRegistration.cs
```

### API

```text
Explore.API/Controllers/WebhooksController.cs
Explore.API/Controllers/IncomingWebhooksController.cs
Explore.API/Hateoas/*Webhook*
Explore.API/Hateoas/RouteNames.cs
Explore.API/BackgroundServices/WebhookDeliveryProcessor.cs
Explore.API/HealthChecks/WebhookDeliveryHealthCheck.cs
Explore.API/Program.cs
```

### Blazor

```text
Explore.Blazor.Client/Pages/Admin/Webhooks/*
Explore.Blazor.Client/Components/Webhooks/*
Explore.Blazor.Client/Services/*Webhook*
```

### Tests

```text
Event.Application.UnitTests/Webhooks/*
Event.Persistence.Tests/Webhooks/*
Event.Infrastructure.Tests/Webhooks/*
Event.API.IntegrationTests/Webhooks/*
Event.Blazor.Client.Tests/Webhooks/*
Event.Architecture.Tests/*
```

## Open Questions for Implementation

These are implementation-time questions, not blockers for the plan:

1. Should the first implementation add a dedicated `webhooks` intent to `.claude/contract/intents.yaml`, or is composing existing intents sufficient for now?
2. Should Svix endpoints be mirrored into `WebhookEndpoint`, or should SvixProvider rely on the Svix App Portal and store only provider links?
3. What exact secret storage path should endpoint secrets use if the secret provider does not support write operations in the target environment?
4. Should organization-owned webhooks be enabled in V1 or delayed behind a tenant setting that defaults off?
5. Should `IncomingWebhookMessage` store only hashes by default, or allow short-lived encrypted payload retention for debugging in non-production environments?
6. Should optional real Svix integration tests use Aspire immediately, or remain fake-client-only until the self-hosted Svix compose is accepted operationally?
7. Should LocalProvider support HTTP endpoints in development only, with HTTPS required in production unless explicitly overridden?

## Decisions Already Made by This Plan

- Build LocalProvider first, then SvixProvider.
- Keep outgoing product webhooks separate from incoming callbacks.
- Do not require Svix for self-hosters to receive Coop or other incoming callbacks.
- Use Svix-compatible LocalProvider signatures.
- Use canonical ISLAMU webhook messages even when Svix is the delivery backend.
- Do not build full Svix clone features in LocalProvider V1.
- Keep delivery post-commit and outbox-backed.
- Use HAL links for UI affordances.
- Treat webhook delivery as at-least-once.
- Prioritize safety and minimization for moderation payloads.

## Evidence Gaps to Recheck Before Coding

- Package management convention for adding `Svix` package. Check `Directory.Packages.props` or project-level package references.
- Exact current test project names. The plan lists expected project names from repo conventions, but implementation should confirm with `rg --files '*Tests.csproj'`.
- Exact Cerbos policy file layout for new webhook actions.
- Current OpenAPI generation command and generated Blazor client workflow.
- Whether any active work in `dev/active` overlaps with webhooks before implementation starts.
