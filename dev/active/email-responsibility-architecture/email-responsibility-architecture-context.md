<!-- ABOUTME: Resume context for the Email Responsibility Architecture planning workstream. -->
<!-- ABOUTME: Captures decisions, verified files, constraints, validation baseline, risks, and handoff notes for future agents. -->

# Email Responsibility Architecture — Context

Last Updated: 2026-07-08 Europe/Brussels

## Session Progress

### Completed

- Classified the current `/dev-docs` request against `.claude/contract/intents.yaml`; no exact architecture-planning intent matched, so the work uses a fallback dev-docs contract.
- Read required baseline docs: `AGENTS.md`, `dev/active/README.md`, `.claude/contract/intents.yaml`, `docs/QUICK_REFERENCE.md`, and `docs/GOVERNANCE.md`.
- Loaded relevant skills: Clean Architecture, CQRS/MediatR, EF Core, auth, Blazor BFF/UI/CSS/design-system, outbox, error tracking, and Aspire.
- Read/source-summarized canonical docs for architecture, domain, API, security, authorization, configuration, deployment, multi-tenancy, federation, operations, outbox, Blazor, and codebase structure.
- Inspected `dev/active/` and `dev/pause/`; no active matching workstream existed. Related paused stream `dev/pause/email-smtp-abstraction/` was verified and treated as narrower prior SMTP/provider work.
- Completed source-grounded current-state investigation for existing email dispatch, notification fanout, moderation/reporting, Keycloak bootstrap, Coop callbacks, SMTP config, RabbitMQ pointer dispatch, and tests.
- Created the implementation plan with current-state report, future-state design, phases, risks, and progress-reporting contract.
- Incorporated Senior CTO feedback: identity lifecycle email ownership now uses the `AccountAuthority` rule rather than an over-broad Identity Microservice ownership model.
- Verified external ATProto/PDS source facts for PDS-hosted accounts, PDS SMTP/account email behavior, multi-account PDS sizing, and PDS email confirmation/password-reset APIs.
- Implemented Phase 1 ownership foundation in `Explore.Application`: controlled ownership/category/account-authority enums, routing options, ownership resolver contract, default resolver, DI registration, and Application unit tests.
- Verified the implementation with Release build, `Event.Application.UnitTests`, `Event.Architecture.Tests`, and scoped diagnostics inspection.
- Implemented Phase 2 normalized persistence foundation: lookup-backed `NotificationIntent`, `NotificationDelivery`, `NotificationExternalDelegation`, repository contract/implementation, EF mappings, runtime lookup seeding, migration, and PostgreSQL integration tests.
- Verified Phase 2 with Release build, `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests`, and scoped diagnostics inspection.
- Implemented Phase 2.3 notification orchestrator: Application-only `INotificationOrchestrator`, `DefaultNotificationOrchestrator`, result contract, extended safe intent draft metadata, DI registration, normalized account-authority delegation lookup, migration, and tests.
- Verified Phase 2.3 with Release build, `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests`, and scoped diagnostics inspection.
- Implemented Phase 3.2 product lifecycle notification-intent integration through `EventLifecycleEmailOutboxFactory`, registration command flow, and lifecycle scheduler while preserving `EmailDispatchOutbox` as Basic Dispatch Mode source of truth.
- Implemented Phase 3.3 unsubscribe/preference preservation coverage; no runtime change was needed because `EmailDispatchDrainService` already checks product preferences before SMTP handoff.
- Implemented Phase 4.1 account-authority lifecycle delegation contract and default Application service for safe disabled/provider-not-configured outcomes plus local delegation audit when enabled.
- Implemented Phase 4.2 Keycloak delegation path in Infrastructure so ISLAMU-initiated identity lifecycle requests record local account-authority delegation audit and then ask Keycloak to send provider-owned required-action emails.
- Implemented Phase 4.3 Keycloak theme and SMTP mode boundary documentation across configuration, security, operations, and active workstream docs.
- Implemented Phase 5.1 ATProto/PDS account-authority email ownership policy across configuration, security, federation, and future Identity Project docs.
- Implemented Phase 5.2 verified notification-email fallback for product registration confirmation.

### In Progress

- No active implementation slice is in progress.

### Next Steps

1. Recommended next implementation slice: Phase 5.3 future ISLAMU PDS Hosting Platform cell model.
2. Keep the same AccountAuthority rule for ATProto/PDS: the PDS that owns, creates, and verifies the credential token owns account lifecycle email.
3. Product registration confirmation now uses user email only when `User.EmailVerified == true`; missing or unverified identity email falls back to in-app notification rather than `EmailDispatchOutbox`.
4. Keep Keycloak/PDS provider responses, admin tokens, provider secrets, action tokens, confirmation/reset codes, and email bodies out of local results, logs, and delegation audit rows.

### Blockers

- No hard blocker for the completed Phase 5.2 notification fallback slice.
- Markdown diagnostics are not configured in this environment; C# diagnostics, build, and tests are the authority for code verification.

## Quick Resume

The key decision is: the account authority that owns, creates, and verifies a credential token owns that identity lifecycle email. Keycloak owns Keycloak lifecycle emails. ATProto/PDS owns PDS account lifecycle emails, including future ISLAMU-operated PDS cells. ISLAMU product-domain emails go through ISLAMU notification intent/outbox/audit. Coop/Osprey/provider internal emails may remain provider-owned. External provider emails to ISLAMU users are off by default and require explicit delegation plus local audit and safe payload hashing.

The current repo already has `EmailDispatchOutbox`, SMTP delivery, email dispatch drain/health/admin APIs, optional RabbitMQ pointer mode, event lifecycle email factory, moderation notification fanout, and Keycloak bootstrap. The implementation should add ownership/routing and canonical notification intent/delegation records above these primitives.

## Key Files And Responsibilities

| File/Area | Responsibility / Finding |
| --- | --- |
| `dev/active/email-responsibility-architecture/email-responsibility-architecture-plan.md` | Primary implementation plan and source-grounded architecture decisions. |
| `dev/active/email-responsibility-architecture/email-responsibility-architecture-context.md` | Resume context and handoff state. |
| `dev/active/email-responsibility-architecture/email-responsibility-architecture-tasks.md` | Checklist for implementation slices and verification. |
| `dev/active/email-responsibility-architecture/future-islamu-identity-project.md` | Future Identity Project boundary doc for AccountAuthority, ISLAMU-operated PDS cells, and Identity Microservice non-ownership of credential emails. |
| `Explore.Application/Notifications/NotificationOwnership.cs` | Controlled owner vocabulary: `IslamuEvent`, `AccountAuthority`, `ExternalWorkflowProvider`, `Disabled`. |
| `Explore.Application/Notifications/AccountAuthorityKind.cs` | Account-authority kinds: Keycloak, ATProto/PDS, ISLAMU-operated PDS, local identity, external OIDC. |
| `Explore.Application/Notifications/NotificationCategory.cs` | Controlled notification lifecycle categories for routing. |
| `Explore.Application/Notifications/NotificationRoutingOptions.cs` | Category routing defaults and validation rules; prevents unsupported owner/category combinations. |
| `Explore.Application/Contracts/Notifications/INotificationOwnershipResolver.cs` | Application contract for resolving a safe notification intent draft to an ownership decision. |
| `Explore.Application/Notifications/DefaultNotificationOwnershipResolver.cs` | Default Application-only resolver; no EF, SMTP, RabbitMQ, Keycloak, PDS, Coop, or Osprey dependencies. |
| `Event.Application.UnitTests/Notifications/DefaultNotificationOwnershipResolverTests.cs` | Unit tests for default routing, account-authority routing, provider-internal routing, explicit moderation delegation, invalid config, and cancellation. |
| `Explore.Domain/NotificationIntent.cs` | Canonical durable notification-intent aggregate with UUIDv7 id, tenant, normalized category/owner/recipient/status lookups, safe payload reference/hash, correlation id, audit, and soft-delete fields. |
| `Explore.Domain/NotificationDelivery.cs` | Local delivery audit row linking notification intents to `EmailDispatchOutbox` and safe provider status metadata. |
| `Explore.Domain/NotificationExternalDelegation.cs` | Explicit external delegation audit row with normalized provider/status/recipient lookups and safe payload hash. |
| `Explore.Domain/NotificationIntentLookups.cs` and `Explore.Domain/Enums/NotificationIntentEnums.cs` | Normalized lookup entities and stable integer enum companions for notification categories, ownership, statuses, recipient kinds, delivery statuses, external delegation statuses, external workflow providers, and account-authority kinds. |
| `Explore.Domain/NotificationExternalDelegation.cs` | Delegation audit row now supports normalized account-authority lookup in addition to external workflow provider lookup. |
| `Explore.Application/Contracts/Persistence/INotificationIntentRepository.cs` | Application repository boundary for durable notification intents, delivery rows, and delegation rows; returns entities only. |
| `Explore.Persistence/Repositories/NotificationIntentRepository.cs` | Persistence implementation using entity-returning methods, exact tenant predicates, and `AsNoTracking` reads. |
| `Explore.Persistence/Configurations/Entities/NotificationIntentConfiguration.cs` | EF mappings for intent/delivery/delegation tables, UUIDv7 defaults, tenant/soft-delete-aware indexes, and safe field lengths. |
| `Explore.Persistence/Configurations/Entities/NotificationIntentLookupConfigurations.cs` | EF mappings for normalized lookup tables with stable integer ids and unique master codes. |
| `Explore.Persistence/Migrations/20260707125850_AddNotificationIntentPersistence.cs` | Migration creating normalized lookup tables and durable intent/delivery/delegation tables with reversible `Down`. |
| `Explore.Persistence/Migrations/20260707135125_AddNotificationAccountAuthorityDelegation.cs` | Migration adding normalized account-authority lookup table and delegation FK/indexes. |
| `Event.Persistence.IntegrationTests/Repositories/NotificationIntentRepositoryTests.cs` | PostgreSQL/Testcontainers tests for lookup seeding, normalized persistence, exact-tenant dedupe lookup, tenant filtering, and audit rows. |
| `Explore.Application/Contracts/Notifications/INotificationOrchestrator.cs` | Application contract for policy-aware enqueue into notification intent/delivery/delegation persistence. |
| `Explore.Application/Notifications/DefaultNotificationOrchestrator.cs` | Application-only orchestrator that calls the ownership resolver and writes repository entities without provider/API/EF dependencies. |
| `Explore.Application/Notifications/NotificationOrchestrationResult.cs` | Result model returning the persisted intent, ownership decision, and optional delivery/delegation rows. |
| `Event.Application.UnitTests/Notifications/DefaultNotificationOrchestratorTests.cs` | Unit tests for local ISLAMU delivery, account-authority delegation, non-initiated account authority, external workflow delegation, disabled routing, and required metadata validation. |
| `dev/pause/email-smtp-abstraction/*` | Prior paused SMTP abstraction work; useful historical context, not the active workstream. |
| Official Bluesky PDS README | External source verifying PDS SMTP account-email behavior and multi-account PDS sizing guidance. |
| AT Protocol Account spec | External source verifying accounts live on a PDS and can migrate between PDS hosting providers/instances. |
| `com.atproto.server.requestEmailConfirmation`, `com.atproto.server.requestPasswordReset` lexicons | External source verifying PDS-owned email confirmation and password reset lifecycle APIs. |
| `Explore.Domain/EmailDispatchOutbox.cs` | Existing durable product email dispatch state with tenant/source/recipient/body/retry/dead-letter/RabbitMQ metadata. |
| `Explore.Application/Contracts/Infrastructure/IEmailService.cs` | Current email delivery abstraction with `SendAsync` and `TestConnectionAsync`. |
| `Explore.Infrastructure/Mail/SmtpEmailService.cs` | Current SMTP delivery implementation. |
| `Explore.Infrastructure/EmailDispatchDrainService.cs` | Worker/drain service that claims outbox rows, checks preferences/unsubscribe, sends email, and records attempts/receipts/status. |
| `Explore.API/Controllers/EmailDispatchAdminController.cs` | Admin API for status, pause/resume, park, and replay. |
| `Explore.Application/Services/EventLifecycleEmailOutboxFactory.cs` | Existing Application factory creating registration/event lifecycle email outbox rows. |
| `Explore.Application/Services/EventModerationNotificationFanoutService.cs` | Existing light/heavy moderation in-app notification fanout; heavy redaction path is privacy-sensitive. |
| `Explore.Application/Services/EventPublishedNotificationFanoutService.cs` | Existing event-published notification fanout. |
| `Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs` | Routes durable outbox events to fanout/provider-sync services and fails closed for unknown event types. |
| `Explore.Application/Services/EventKeycloakIdentityContractContributor.cs` | Contributes Keycloak realm/client desired state; does not make ISLAMU identity-email sender. |
| `Explore.Application/Contracts/Identity/IAccountAuthorityLifecycleEmailService.cs` | Application contract for provider-owned identity lifecycle email requests: verification, password reset, and email-update verification. |
| `Explore.Application/Services/DefaultAccountAuthorityLifecycleEmailService.cs` | Default Application service that records safe account-authority delegation audit when enabled/configured and returns safe disabled/provider-not-configured outcomes otherwise. |
| `Event.Application.UnitTests/Services/DefaultAccountAuthorityLifecycleEmailServiceTests.cs` | Tests disabled/provider-not-configured behavior and safe delegation draft mapping for all supported identity lifecycle actions. |
| `Explore.Infrastructure/Services/Keycloak/KeycloakLifecycleEmailOptions.cs` | Runtime Keycloak required-action email options, including safe URL policy, realm/admin credentials, default client/lifespan, and account-authority kind. |
| `Explore.Infrastructure/Services/Keycloak/KeycloakAccountAuthorityLifecycleEmailService.cs` | Infrastructure adapter that records local delegation audit and calls Keycloak `execute-actions-email` for provider-owned identity lifecycle messages. |
| `Explore.Infrastructure.Tests/Infrastructure/KeycloakAccountAuthorityLifecycleEmailServiceTests.cs` | Tests Keycloak required-action calls for verify/reset/update, unsafe URL blocking, and redacted provider failure outcomes. |
| `Explore.Infrastructure/Messaging/*EmailDispatchRabbitMq*` | Optional RabbitMQ pointer transport; PostgreSQL remains canonical dispatch state. |
| `docker/keycloak/keycloak-init.sh`, `docker-compose.yml`, `Explore.AppHost/AppHost.cs` | Keycloak and product SMTP/Mailpit configuration evidence. |

## Key Decisions

- Do not make all emails come from ISLAMU Event.
- Do not let every provider send arbitrary emails.
- Identity lifecycle emails are account-authority-owned: Keycloak for Keycloak lifecycle, PDS for ATProto/PDS lifecycle, and a future local account authority only if it owns/creates/verifies the credential token.
- Product/event/registration/reporting/moderation emails are ISLAMU-owned and should use local notification intent plus existing `EmailDispatchOutbox` delivery/audit.
- External provider internal emails are provider-owned.
- External provider user-facing ISLAMU emails require explicit delegation with local `NotificationIntent`, external delegation audit, safe payload hash, provider id, and status tracking.
- RabbitMQ is optional wake-up/pointer transport, not canonical email state.
- Keycloak SMTP injection or theme sync does not transfer identity-email ownership.
- Keycloak email themes and realm SMTP are Keycloak-side customization/transport. They do not route identity lifecycle mail through ISLAMU product Basic Dispatch.
- ATProto/PDS account email is account-authority data. External PDS hosts and future ISLAMU-operated PDS cells own their hosted-account confirmation, password-reset, migration, and security emails.
- Product registration confirmation must not treat unverified identity email as notification-safe email; use verified user email only, otherwise create an in-app notification fallback.
- Future ISLAMU-operated PDS hosting defaults to multi-account PDS cells/shards/clusters; dedicated cells are deferred to premium/org/sovereign/regulatory/hard-isolation cases.
- The future ISLAMU Identity Microservice orchestrates signup policy, PDS cell selection, handle/account mapping, delegation audit, and status; it must not mint PDS credential tokens or send PDS credential emails.
- Heavy redaction emails must be generic and must omit unsafe event/report/provider fields.

## Constraints And Rules

- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- Domain has zero external dependencies; Application does not use `ExploreDbContext`; Persistence/Infrastructure implement Application contracts.
- GET endpoints default to `[AllowAnonymous]`; writes/control actions require `[Authorize]` and resource authorization.
- HAL `_links` drive UI affordances; Blazor must not gate action buttons by local role/claim inspection.
- API-authoritative tenant resolution and EF tenant filters must fail closed.
- Durable intent first: handlers/controllers/domain must not send SMTP, publish RabbitMQ, call provider APIs, or schedule TickerQ directly.
- Every new file starts with two ABOUTME lines.
- Observability uses OpenTelemetry, Prometheus, and Loki; no sensitive recipient/template/provider data in logs/metrics.
- Aspire local proof should follow repo docs: foreground `aspire run --apphost Explore.AppHost/Explore.AppHost.csproj --isolated` when runtime validation is needed.

## Validation Baseline

Planning validation performed:

- Contract, skills, docs, active/pause workstreams, and source evidence were reviewed before writing the plan.
- Markdown docs should be read back after creation; no code build is required for planning-only docs.

Phase 1 implementation validation performed on 2026-07-07:

- `dotnet build --configuration Release --verbosity quiet` passed with pre-existing warning baseline.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed: 2050 succeeded, 0 failed.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 259 succeeded, 1 documented skip, 0 failed.
- `aft_inspect` scoped to the changed Application/test files reported 0 diagnostics; C# LSP server is not installed, so build/tests remain authoritative.

Phase 2 implementation validation performed on 2026-07-07:

- `dotnet build --configuration Release --verbosity quiet` passed with the existing warning baseline.
- `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passed: 317 succeeded, 0 failed.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed: 2053 succeeded, 0 failed.
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed: 254 succeeded, 0 failed, exercising the persistence surface against PostgreSQL/Testcontainers.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 259 succeeded, 1 existing documented skip, 0 failed.
- `aft_inspect` scoped to the changed code/docs reported 0 diagnostics; C# LSP server is not installed, so build/tests remain authoritative.

Phase 2.3 implementation validation performed on 2026-07-07:

- `dotnet build --configuration Release --verbosity quiet` passed with the existing warning baseline.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed: 2059 succeeded, 0 failed.
- `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passed: 317 succeeded, 0 failed.
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed: 255 succeeded, 0 failed, exercising normalized lookup/delegation persistence against PostgreSQL/Testcontainers.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 259 succeeded, 1 existing documented skip, 0 failed.
- `aft_inspect` scoped to the changed code/docs reported 0 diagnostics; C# LSP server is not installed, so build/tests remain authoritative.

Future implementation baseline:

- Always run `dotnet build --configuration Release --verbosity quiet` when code changes.
- Run intent-specific tests only; never solution-level `dotnet test` unless repo policy changes.
- Likely tests by slice: architecture tests, Application unit tests, Persistence integration tests, API integration tests, Blazor tests for admin UI, and Aspire/manual QA for runtime email dispatch.

## Current Known Risks / Unknowns

- Phase 2.3 intentionally stops at Application orchestration and persistence boundaries; product-email integration into `EventLifecycleEmailOutboxFactory` and handler flows remains Phase 3 work.
- ATProto OIDC/PDS identity email behavior is future-facing. Current docs verify foundation-only federation, not production public ATProto login.
- ATProto OIDC/PDS login remains future-facing. Phase 5.2 installs the verified-email product-notification fallback for existing sync paths, but a full app-level notification-email management and verification UI/API is still future work.
- Future ISLAMU-operated PDS hosting needs capacity, cell allocation, migration, backup, abuse, and SLO design before implementation.
- Identity Microservice scope can easily overreach; keep it to orchestration/mapping/audit and leave credential tokens/emails to account authorities.
- Coop user-facing email delegation capability is not verified. Keep it off by default until provider contract and safe payload schema are proven.
- Keycloak template/theme and SMTP-mode internals are now documented at the ownership-boundary level. Future theme-sync implementation still needs concrete asset layout, deployment, cache, and rollback design before changing runtime automation.
- Path-specific `.claude/rules/*.md` files must be loaded per implementation slice.

## Handoff Notes

### 2026-07-06 Europe/Brussels

Planning docs were created from the user's architecture brief, Senior CTO AccountAuthority feedback, ATProto/PDS source verification, and repository investigation. Resume by reading the plan sections 1, 5, and 6 plus `future-islamu-identity-project.md`, then update this context before starting any code. The recommended first code slice is the ownership/category model, `AccountAuthorityKind`, and resolver tests because it creates the governing rule without touching SMTP, RabbitMQ, Keycloak, PDS, Coop, or Blazor UI prematurely.

### 2026-07-07 Europe/Brussels

Phase 1 Application foundation is implemented and verified. The resolver uses `NotificationRoutingOptions` defaults to route identity lifecycle to `AccountAuthority`/`Keycloak`, product/event/registration/trust-safety/platform/marketing to `IslamuEvent`, provider-internal to `ExternalWorkflowProvider`, and rejects external trust-safety delegation unless explicitly enabled. Next agent should not add provider calls here; proceed to normalized notification intent/delegation persistence only after re-classifying the Domain/Persistence slice and loading EF rules.

Phase 2 persistence foundation is implemented and verified. The new normalized lookup tables use stable integer IDs; `NotificationIntent`, `NotificationDelivery`, and `NotificationExternalDelegation` use UUIDv7 aggregate/audit IDs. Repository and EF mappings are in place, runtime lookup seeding is covered, and PostgreSQL integration tests prove lookup seeding, tenant filtering, exact-tenant dedupe lookup, and delivery/delegation audit persistence. Next agent should proceed to Phase 2.3 by adding the notification orchestrator over these repository boundaries without calling SMTP, RabbitMQ, Keycloak, PDS, Coop, or Osprey directly.

Phase 2.3 orchestrator is implemented and verified. The Application orchestrator calls the ownership resolver, creates safe `NotificationIntent` rows, attaches pending local deliveries for ISLAMU-owned notifications, records normalized account-authority delegations for ISLAMU-initiated Keycloak/PDS-style actions, records external workflow delegations only when auditing is required, and writes skipped intents for disabled routes. It still does not call SMTP, RabbitMQ, Keycloak, PDS, Coop, or Osprey directly. Next agent should start Phase 3 by routing one product lifecycle email path through the orchestrator before existing `EmailDispatchOutbox` creation.

### 2026-07-08 Europe/Brussels

Phase 3.2 and Phase 3.3 are implemented and verified. Product lifecycle email creation now centralizes matching notification-intent audit mapping in `EventLifecycleEmailOutboxFactory`, but enqueue happens after durable email work is accepted/persisted to avoid orphan audit rows for registration race losers or scheduler persistence failures. `EmailDispatchDrainService` remains the Basic Dispatch SMTP boundary and already enforces product unsubscribe/preference checks before provider handoff; regression coverage now proves every product lifecycle `EmailDispatchKind` maps to the expected preference category.

Phase 4.1 Application contract is implemented. `IAccountAuthorityLifecycleEmailService` models email verification, password reset, and email-update verification as account-authority-owned actions. `DefaultAccountAuthorityLifecycleEmailService` returns safe disabled/provider-not-configured outcomes without provider calls and records local `NotificationIntent`/external-delegation audit when enabled/configured. It does not call Keycloak Admin REST or send SMTP. Phase 4.2 should implement Keycloak `execute-actions-email` delegation in Infrastructure using this contract while preserving redaction of admin tokens, provider response bodies, action tokens, secrets, and identity email content.

Phase 4.1 was verified with clean C# `lsp_diagnostics` on changed source/test files, `Event.Application.UnitTests` passing 2064/2064, `Event.Architecture.Tests` passing with 259 succeeded and 1 existing documented skip, and Release build passing with the existing warning baseline.

Phase 4.2 Keycloak delegation path is implemented. Infrastructure now overrides the Application fallback with `KeycloakAccountAuthorityLifecycleEmailService`, records safe local delegation audit through `INotificationOrchestrator`, then calls Keycloak Admin REST `execute-actions-email` for `VERIFY_EMAIL`, `UPDATE_PASSWORD`, or `UPDATE_EMAIL`. The path never uses ISLAMU SMTP/`EmailDispatchOutbox` for identity lifecycle emails and returns only safe local ids/status/reason codes. Provider failures are redacted and unsafe runtime Keycloak URLs are blocked unless explicitly allowed for local development.

Phase 4.2 was verified with clean C# diagnostics where available, `Explore.Infrastructure.Tests` passing 701/701, `Event.Application.UnitTests` passing 2064/2064, and Release build passing with the existing warning baseline. Next implementation should document Keycloak theme/custom SMTP boundaries in Phase 4.3 before changing template/theme sync or shared SMTP behavior.

Phase 4.3 documentation is implemented. `docs/CONFIGURATION.md` now documents logical identity-email ownership policy labels plus the implemented `AccountAuthorityLifecycleEmail:*` and `KeycloakLifecycleEmail:*` sections. `docs/SECURITY-MODEL.md` now states that Keycloak owns action tokens, templates, SMTP handoff, and identity email delivery, and that local logs/results/audit must exclude admin tokens, provider secrets, raw Keycloak bodies, action tokens, and rendered email content. `docs/OPERATIONS.md` now separates Keycloak identity lifecycle email operations from Basic Email Dispatch, including managed/self-hosted/local Mailpit SMTP semantics and theme-cache development flags. Next agent should start Phase 5.1 ATProto/PDS account-authority policy unless the user explicitly asks for more Keycloak theme automation.

Phase 5.1 ATProto/PDS account-authority policy is implemented. `docs/CONFIGURATION.md`, `docs/SECURITY-MODEL.md`, and `docs/FEDERATION.md` now state that PDS-hosted account lifecycle email is PDS/account-authority owned, not an ISLAMU Event product email. `future-islamu-identity-project.md` source evidence now includes AT Protocol stack/account-permission docs and the `confirmEmail` lexicon. Current repo status remains federation foundation only: public ATProto OAuth/login and ISLAMU-operated PDS hosting are not implemented. Next agent should start Phase 5.2 by designing an app-level notification email flow for cases where ATProto `email`/`email_verified` is unavailable or unverified.

Phase 5.2 verified notification-email fallback is implemented. Registration confirmation email now uses `User.Email` only when `User.EmailVerified == true`; missing or unverified identity email no longer blocks registration and no longer creates an `EmailDispatchOutbox` row. Instead, `RegistrationNotificationDeliveryService` creates an in-app `Notification` fallback after the registration intent is durably accepted. The API sync path treats ATProto/PDS email as explicit-only: absent `email_verified=true` persists the email as unverified. This slice did not add a full app-level notification-email management or verification UI/API; any future management surface must use HAL affordances.
