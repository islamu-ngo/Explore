<!-- ABOUTME: Task checklist for implementing email ownership, routing, delegation, and documentation decisions. -->
<!-- ABOUTME: Breaks the Email Responsibility Architecture plan into incremental, verifiable implementation slices. -->

# Email Responsibility Architecture — Task Checklist

Last Updated: 2026-07-08 Europe/Brussels

## Status Summary

- Planning artifacts: Accepted for first implementation slice.
- Implementation code: Phase 1 Application ownership foundation, Phase 2 normalized persistence foundation, Phase 2.3 Application orchestrator, Phase 3.2 product lifecycle notification-intent integration, Phase 3.3 unsubscribe/preference preservation, Phase 4.1 account-authority lifecycle delegation contract, Phase 4.2 Keycloak delegation path, Phase 4.3 Keycloak theme/SMTP boundary documentation, Phase 5.1 ATProto/PDS account-authority ownership policy, and Phase 5.2 verified notification-email fallback implemented and verified.
- Current recommended next slice: Phase 5.3 future ISLAMU PDS Hosting Platform cell model, because product notification fallback is now guarded and the remaining PDS work is future topology documentation.
- Prior related workstream: `dev/pause/email-smtp-abstraction/` covered provider-agnostic SMTP delivery; this workstream sits above it and decides who owns each notification purpose.

## Implementation Maintenance Rules

Every implementation agent must keep these files current while working:

1. Update `email-responsibility-architecture-plan.md` when design, file scope, risks, or acceptance criteria change.
2. Update `email-responsibility-architecture-context.md` before handoff, context compaction, or pausing.
3. Update this checklist immediately after completing or deferring each task.
4. Preserve the core decision: the account authority that owns, creates, and verifies a credential token owns that identity lifecycle email; ISLAMU product-domain emails are owned by ISLAMU Notification; external user-facing ISLAMU email requires explicit delegation and local audit; shared SMTP is transport, not ownership.
5. Do not send SMTP, publish RabbitMQ, or schedule TickerQ directly from Domain, controllers, or handlers. Create durable intent first.
6. Do not weaken tenant isolation, HAL affordance gating, heavy-redaction privacy, or Clean Architecture boundaries to complete a task.
7. Every new source file must start with two `ABOUTME:` comment lines.
8. Final progress reports must teach the developer what changed: architecture pattern, project abstractions, files/classes/handlers/components, data/control flow, conventions, verification, remaining work, and docs updated.

## Phase 0 — Plan Review And Baseline

### 0.1 Review the architecture plan with the user

  - Status: Completed.
- Type: Planning.
- Layer: Documentation.
- Files:
  - `dev/active/email-responsibility-architecture/email-responsibility-architecture-plan.md`
  - `dev/active/email-responsibility-architecture/email-responsibility-architecture-context.md`
  - `dev/active/email-responsibility-architecture/email-responsibility-architecture-tasks.md`
  - `dev/active/email-responsibility-architecture/future-islamu-identity-project.md`
- Description: Confirm that the ownership table, AccountAuthority/PDS hosting model, provider defaults, delegation rules, and implementation order match product intent.
- Acceptance Criteria:
  - User has reviewed Section 3 and Section 5 of the plan plus the future Identity Project boundary doc.
  - Any requested policy changes are recorded before code starts.
  - Implementation starts only after the first slice is approved or clearly requested.
- Validation:
  - Documentation diff only.

### 0.2 Re-check the matching implementation intent before each code slice

  - Status: Completed for Phase 1; repeat before each new slice.
- Type: Governance.
- Layer: Contract.
- Files:
  - `.claude/contract/intents.yaml`
  - `docs/QUICK_REFERENCE.md`
  - matching `.claude/rules/*.md`
- Description: The planning task used a fallback dev-docs contract; implementation slices must bind to their actual intent (`add-cqrs-handler`, `add-ef-migration`, `add-write-endpoint`, `add-get-endpoint`, `add-hal-link`, `external-infrastructure-bootstrap`, or Blazor/admin UI intents as applicable).
- Acceptance Criteria:
  - Each implementation slice lists its matched intent and required docs/rules before editing.
  - If no intent matches a recurring notification-architecture slice, add a follow-up task to propose a new contract intent.
- Validation:
  - Intent and rule references recorded in context before edits.

### 0.3 Establish current build and test baseline

  - Status: Completed for Phase 1.
- Type: Verification.
- Layer: Repository-wide.
- Files:
  - `docs/OPERATIONS.md`
- Description: Run the canonical baseline before implementation, using per-project test commands rather than solution-level `dotnet test`.
- Acceptance Criteria:
  - `dotnet build --configuration Release --verbosity quiet` status recorded.
  - Any pre-existing failures are named and separated from new work.
- Validation:
  - Canonical build output summarized in context.

## Phase 1 — Notification Ownership Policy

### 1.1 Add notification ownership and category concepts

- Status: Completed on 2026-07-07.
- Type: Application model.
- Layer: Application.
- Files:
  - New: `Explore.Application/Notifications/NotificationOwnership.cs` or equivalent verified location.
  - New: `Explore.Application/Notifications/NotificationCategory.cs` or equivalent verified location.
  - New: `Explore.Application/Notifications/AccountAuthorityKind.cs`.
  - New: `Explore.Application/Notifications/ExternalWorkflowProviderKind.cs`.
- Description: Define the controlled vocabulary for who owns a notification decision and which lifecycle category it belongs to. Re-check existing symbols first because a workspace LSP search timed out during planning.
- Acceptance Criteria:
  - Ownership values include `IslamuEvent`, `AccountAuthority`, `ExternalWorkflowProvider`, and `Disabled` or approved equivalents.
  - Account authority kinds include `Keycloak`, `AtprotoPds`, `IslamuOperatedPds`, `LocalIdentity`, and `ExternalOidc` or approved equivalents.
  - Categories include identity lifecycle, product/event/registration lifecycle, trust-safety reporting/moderation, provider-internal, platform operations, and marketing or approved equivalents.
  - Names are not duplicated if equivalent types already exist.
- Required Skills/Rules:
  - `clean-architecture-rules`
  - `cqrs-mediatr-guidelines`
- Validation:
  - `dotnet build --configuration Release --verbosity quiet` passed.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed.

### 1.2 Add ownership resolver contract

- Status: Completed on 2026-07-07.
- Type: Application contract.
- Layer: Application.
- Files:
  - New: `Explore.Application/Contracts/Notifications/INotificationOwnershipResolver.cs` or equivalent.
  - New: `Explore.Application/Notifications/NotificationIntentDraft.cs` or equivalent if needed.
  - New: `Explore.Application/Notifications/NotificationOwnershipDecision.cs`.
  - New: `Explore.Application/Notifications/DefaultNotificationOwnershipResolver.cs`.
- Description: Introduce a small resolver API that maps an intent draft to the owner and routing policy without knowing SMTP, RabbitMQ, or provider implementation details.
- Acceptance Criteria:
  - Resolver has cancellation-aware async API.
  - Resolver input carries category, tenant scope, recipient kind, template key, and safe payload reference or safe payload hash.
  - Resolver does not reference EF Core, SMTP, Keycloak admin clients, RabbitMQ, or Coop/Osprey clients.
- Required Skills/Rules:
  - `clean-architecture-rules`
  - `cqrs-mediatr-guidelines`
- Validation:
  - `Event.Application.UnitTests` covers identity, product, provider-internal, delegated trust-safety, invalid config, and cancellation behavior.
  - `Event.Architecture.Tests` passed.

### 1.3 Add category routing configuration shape

- Status: Completed on 2026-07-07 for Application-level options and DI registration; canonical external docs examples remain for later configuration docs.
- Type: Configuration.
- Layer: Infrastructure/API composition root.
- Files:
  - Existing: `Explore.Application/ApplicationServicesRegistration.cs`
  - New: `Explore.Application/Notifications/NotificationRoutingOptions.cs`
  - New: `Event.Application.UnitTests/Notifications/DefaultNotificationOwnershipResolverTests.cs`
  - Existing: `docs/CONFIGURATION.md` for a later docs/config example slice.
- Description: Add category routing defaults without creating a single misleading `emails.provider = Keycloak` switch.
- Acceptance Criteria:
  - Defaults route identity lifecycle to `AccountAuthority` with a configured `AccountAuthorityKind`.
  - Keycloak/PDS SMTP mode is documented as account-authority transport, not ISLAMU ownership.
  - Product/event/registration/trust-safety default to `IslamuEvent`.
  - Provider-internal defaults to external provider ownership.
  - Coop user-facing moderation emails default off unless explicit delegated mode is configured.
  - Config validation rejects unsupported owner/category combinations.
- Required Skills/Rules:
  - `clean-architecture-rules`
  - `auth-patterns`
  - `error-tracking`
- Validation:
  - Options validation tests passed in `Event.Application.UnitTests`.
  - `ApplicationServicesRegistration` registers `NotificationRoutingOptions` and `INotificationOwnershipResolver`.
  - Detailed external configuration docs remain deferred until the Phase 2/administration shape is stable.

## Phase 2 — Canonical Notification Intent Model

### 2.1 Add notification intent persistence model

- Status: Completed on 2026-07-07.
- Type: Domain/Persistence.
- Layer: Domain + Persistence.
- Files:
  - New: `Explore.Domain/NotificationIntent.cs`.
  - New: `Explore.Domain/NotificationIntentLookups.cs`.
  - New: `Explore.Domain/Enums/NotificationIntentEnums.cs`.
  - New: `Explore.Application/Contracts/Persistence/INotificationIntentRepository.cs`.
  - New: `Explore.Persistence/Repositories/NotificationIntentRepository.cs`.
  - New: `Explore.Persistence/Configurations/Entities/NotificationIntentConfiguration.cs`.
  - New: `Explore.Persistence/Configurations/Entities/NotificationIntentLookupConfigurations.cs`.
  - New: `Explore.Persistence/Migrations/20260707125850_AddNotificationIntentPersistence.cs`.
- Description: Add canonical local notification intent records that capture the business decision to notify before delivery or external delegation.
- Acceptance Criteria:
  - Uses `Guid` UUIDv7 for aggregate identity.
  - Stores tenant id, category, owner, recipient kind, template key, safe payload hash/reference, correlation id, state, audit fields, and deduplication key where required.
  - Does not store raw moderation evidence, unsafe event content, tokens, SMTP credentials, or provider secrets.
  - Tenant and soft-delete filters follow existing EF conventions.
- Required Skills/Rules:
  - `clean-architecture-rules`
  - `dotnet-efcore-guidelines`
  - `outbox-pattern`
- Validation:
  - Migration generated and reviewed.
  - `Event.Persistence.IntegrationTests` covers lookup seeding, exact-tenant dedupe lookup, and tenant filtering.
  - `dotnet build --configuration Release --verbosity quiet` passed.
  - `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, and `Event.Architecture.Tests` passed.

### 2.2 Add delivery and delegation audit records

- Status: Completed on 2026-07-07 for persistence/audit foundation; account-authority delegation normalization added in Phase 2.3; admin projection tests remain for the future admin API slice.
- Type: Domain/Persistence.
- Layer: Domain + Persistence.
- Files:
  - New: `Explore.Domain/NotificationDelivery.cs`.
  - New: `Explore.Domain/NotificationExternalDelegation.cs`.
  - Updated: `Explore.Domain/NotificationIntentLookups.cs`.
  - Updated: `Explore.Domain/Enums/NotificationIntentEnums.cs`.
  - Existing/new: `Explore.Persistence/Configurations/Entities/NotificationIntentConfiguration.cs`.
  - New: `Explore.Persistence/Migrations/20260707125850_AddNotificationIntentPersistence.cs`.
  - New: `Explore.Persistence/Migrations/20260707135125_AddNotificationAccountAuthorityDelegation.cs`.
  - New: `Event.Persistence.IntegrationTests/Repositories/NotificationIntentRepositoryTests.cs`.
- Description: Track local delivery outcomes and explicit external delegation attempts without letting external providers independently decide ISLAMU user-facing email content.
- Acceptance Criteria:
  - Delivery records can link local intents to `EmailDispatchOutbox` rows.
  - Delegation records store local notification id, report/decision ids when applicable, recipient kind, template key, safe payload hash, external provider id, delegation status, and returned delivery status if any.
  - Provider errors are categorized/redacted before storage and admin projection.
- Required Skills/Rules:
  - `dotnet-efcore-guidelines`
  - `auth-patterns`
  - `outbox-pattern`
- Validation:
  - Repository and mapping tests passed through `Event.Persistence.IntegrationTests`.
  - Admin/status projection tests remain pending until an admin projection/API exists.

### 2.3 Add notification orchestrator

- Status: Completed on 2026-07-07.
- Type: Application service.
- Layer: Application.
- Files:
  - New: `Explore.Application/Contracts/Notifications/INotificationOrchestrator.cs`.
  - New: `Explore.Application/Notifications/DefaultNotificationOrchestrator.cs`.
  - New: `Explore.Application/Notifications/NotificationOrchestrationResult.cs`.
  - Updated: `Explore.Application/Notifications/NotificationIntentDraft.cs`.
  - Updated: `Explore.Application/ApplicationServicesRegistration.cs`.
  - New: `Event.Application.UnitTests/Notifications/DefaultNotificationOrchestratorTests.cs`.
- Description: Centralize enqueue decisions so commands/events create notification intents through one policy-aware path.
- Acceptance Criteria:
  - Orchestrator calls ownership resolver.
  - `IslamuEvent` owner creates local intent and pending local delivery path.
  - `AccountAuthority` owner records normalized account-authority delegation only when ISLAMU initiated an account-authority action.
  - `ExternalWorkflowProvider` user-facing paths require explicit delegated configuration and local audit.
  - `Disabled` owner records a safe skipped state when appropriate.
  - Orchestrator remains Application-only and does not call SMTP, RabbitMQ, Keycloak, PDS, Coop, Osprey, EF Core, API, or Blazor.
- Required Skills/Rules:
  - `clean-architecture-rules`
  - `cqrs-mediatr-guidelines`
  - `outbox-pattern`
- Validation:
  - `Event.Application.UnitTests` covers local ISLAMU delivery, account-authority delegation, non-initiated account authority, external workflow delegation, disabled routing, and required metadata validation.
  - `Event.Persistence.IntegrationTests` covers normalized account-authority lookup seeding and account-authority delegation persistence.
  - `dotnet build --configuration Release --verbosity quiet` passed.
  - `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, and `Event.Architecture.Tests` passed.
  - No repository returns DTOs.

## Phase 3 — ISLAMU Product Email Delivery Integration

### 3.1 Align `IEmailService` with delivery-provider terminology

- Status: Completed on 2026-07-08.
- Type: Refactor/adapter.
- Layer: Application + Infrastructure.
- Files:
  - Existing: `Explore.Application/Contracts/Infrastructure/IEmailService.cs`
  - Existing: `Explore.Infrastructure/Mail/SmtpEmailService.cs`
  - New or existing: `IEmailDeliveryProvider` adapter if approved.
- Description: Decided to keep `IEmailService` as the local SMTP delivery contract for this slice. The current gap was ownership/audit routing above delivery, so adding a second `IEmailDeliveryProvider` adapter would have duplicated the existing dispatch boundary without changing behavior.
- Acceptance Criteria:
  - Existing Basic Dispatch Mode keeps using PostgreSQL durable rows.
  - Keycloak is not registered as a generic provider for arbitrary ISLAMU product emails.
  - SMTP/SES/Resend/Mailgun/local sink can be future implementations behind the same delivery boundary.
- Required Skills/Rules:
  - `clean-architecture-rules`
  - `outbox-pattern`
  - `ponytail`
- Validation:
  - `dotnet build --configuration Release --verbosity quiet` passed with known pre-existing warnings.
  - No renamed/adapter files were needed.

### 3.2 Route product lifecycle email creation through notification intent

- Status: Completed on 2026-07-08.
- Type: Application flow.
- Layer: Application.
- Files:
  - Existing: `Explore.Application/Services/EventLifecycleEmailOutboxFactory.cs`
  - Existing handlers that call event lifecycle email factory.
  - New orchestrator files from Phase 2.
- Description: Preserved current product email content/outbox behavior and centralized matching `NotificationIntentDraft` creation in `EventLifecycleEmailOutboxFactory`. Callers enqueue the notification intent after durable email work is accepted or persisted, which avoids orphan audit rows for registration race losers or failed reminder persistence while preserving existing outbox deduplication.
- Acceptance Criteria:
  - Registration confirmation, approval/rejection, waitlist promotion, reminders, cancellation, and organizer notifications remain ISLAMU-owned.
  - Existing idempotency and deduplication behavior is preserved.
  - No email body includes unsafe moderation evidence or identity-provider token content.
- Required Skills/Rules:
  - `cqrs-mediatr-guidelines`
  - `outbox-pattern`
  - `dotnet-efcore-guidelines`
- Validation:
  - Existing `EventLifecycleEmailOutboxFactory` unit tests updated and preserved.
  - Registration command tests verify the handler still skips notification enqueue for existing/race-winner results.
  - Event lifecycle scheduler tests verify reminder notification intent enqueue after durable outbox persistence, including disabled trigger mode.
  - `lsp_diagnostics` clean for changed source and test files.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed.

### 3.3 Preserve unsubscribe and preference checks

- Status: Completed on 2026-07-08.
- Type: Worker behavior.
- Layer: Infrastructure.
- Files:
  - Existing: `Explore.Infrastructure/EmailDispatchDrainService.cs`
  - Existing preference/unsubscribe repositories/services.
- Description: Ensure new notification intents do not bypass existing `UserNotificationPreference` and one-click unsubscribe behavior. No runtime code change was needed for this slice: `EmailDispatchDrainService` already resolves the product email preference category from `EmailDispatchKind`, checks `UserNotificationPreference` before message construction and SMTP handoff, and adds one-click unsubscribe only for dispatches with a product category and user id. Phase 3.2 notification-intent audit rows therefore cannot bypass the Basic Dispatch guard because product SMTP still flows through `EmailDispatchOutbox` drain processing.
- Acceptance Criteria:
  - Product/marketing categories use preference checks where appropriate.
  - Account-authority lifecycle emails are not incorrectly treated as ISLAMU product unsubscribe flows.
  - Admin projections still redact recipient, subject, body, provider ids, and raw provider errors.
- Required Skills/Rules:
  - `auth-patterns`
  - `error-tracking`
  - `outbox-pattern`
- Validation:
  - `EmailDispatchDrainServiceTests` now covers every ISLAMU product lifecycle `EmailDispatchKind` preference mapping and verifies disabled preferences skip before SMTP handoff.
  - Account-authority lifecycle emails remain separate because no account-authority lifecycle `EmailDispatchKind` maps into product unsubscribe categories.
  - `lsp_diagnostics` clean for `Explore.Infrastructure.Tests/Infrastructure/EmailDispatchDrainServiceTests.cs`.
  - `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=Email]" --minimum-expected-tests 1` passed.
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=Email]" --minimum-expected-tests 1` passed, covering email unsubscribe and email dispatch admin projection tests.

## Phase 4 — Account Authority: Keycloak Identity Email Delegation

### 4.1 Add account-authority lifecycle delegation contract

- Status: Completed on 2026-07-08.
- Type: Application contract.
- Layer: Application.
- Files:
  - New: `Explore.Application/Contracts/Identity/IAccountAuthorityLifecycleEmailService.cs`
  - New: `Explore.Application/Services/AccountAuthorityLifecycleEmailOptions.cs`
  - New: `Explore.Application/Services/DefaultAccountAuthorityLifecycleEmailService.cs`
  - Updated: `Explore.Application/ApplicationServicesRegistration.cs`
  - New tests: `Event.Application.UnitTests/Services/DefaultAccountAuthorityLifecycleEmailServiceTests.cs`
- Description: Model identity lifecycle email requests as account-authority-owned actions instead of ISLAMU product email delivery. The Application service exposes request-email-verification, request-password-reset, and request-email-update-verification methods, returns disabled/provider-not-configured outcomes without provider calls, and records local `NotificationIntent`/external-delegation audit only when the account authority is enabled and configured.
- Implementation Notes:
  - The Application boundary does not call Keycloak Admin REST or send SMTP. Keycloak `execute-actions-email` integration remains Phase 4.2 infrastructure work.
  - Safe results expose only status, action, account-authority kind, local notification intent id, optional local delegation id, and reason code.
  - Request data can carry provider routing hints such as client id, redirect URI, lifespan, and current/proposed email, but the safe result and local delegation audit do not expose raw admin tokens, provider secrets, provider response bodies, or identity email body content.
- Acceptance Criteria:
  - Contract includes request email verification, password reset, and email update verification if supported by provider.
  - Contract returns a safe outcome and local delegation id where ISLAMU initiated the action.
  - Contract does not expose raw admin tokens or provider secrets.
- Required Skills/Rules:
  - `auth-patterns`
  - `clean-architecture-rules`
- Validation:
  - `lsp_diagnostics` clean for the new contract, options, default service, DI registration, and service tests.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed with 2064 tests, 0 failed.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 259 succeeded, 1 existing documented skip, 0 failed.
  - `dotnet build --configuration Release --verbosity quiet` passed with the existing warning baseline.
  - Unit tests cover disabled behavior, provider-not-configured behavior, all three supported identity lifecycle actions, safe delegation ids, and safe outcomes that do not leak current/proposed email values.

### 4.2 Implement Keycloak delegation path

- Status: Completed on 2026-07-08.
- Type: Infrastructure integration.
- Layer: Infrastructure + API composition root.
- Files:
  - `Explore.Application/Contracts/Identity/IAccountAuthorityLifecycleEmailService.cs`
  - `Explore.Infrastructure/Services/Keycloak/KeycloakLifecycleEmailOptions.cs`
  - `Explore.Infrastructure/Services/Keycloak/KeycloakAccountAuthorityLifecycleEmailService.cs`
  - `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
  - `Explore.Infrastructure.Tests/Infrastructure/KeycloakAccountAuthorityLifecycleEmailServiceTests.cs`
- Description: Allow ISLAMU admin/user flows to ask Keycloak to send Keycloak-owned identity lifecycle emails while recording local delegation when initiated by ISLAMU.
- Implementation Notes:
  - Infrastructure now overrides the Application fallback `IAccountAuthorityLifecycleEmailService` with `KeycloakAccountAuthorityLifecycleEmailService` in normal composition.
  - The adapter records local `NotificationIntent`/account-authority delegation audit first, then calls Keycloak Admin REST `execute-actions-email` for `VERIFY_EMAIL`, `UPDATE_PASSWORD`, or `UPDATE_EMAIL` required actions.
  - Keycloak remains sender and owner of action tokens, templates, and identity lifecycle emails. This path never creates `EmailDispatchOutbox` rows and never sends SMTP through ISLAMU Basic Dispatch Mode.
  - Safe results and logs expose only status, action, account-authority kind, local intent/delegation ids, status code, and safe reason codes. Raw admin tokens, provider secrets, raw Keycloak response bodies, action tokens, and identity email bodies stay out of local results, logs, and delegation audit.
  - Unsafe Keycloak base URLs are rejected unless explicitly allowed for local development.
- Acceptance Criteria:
  - Keycloak remains sender/owner of verify/reset/update email/MFA/required-action messages.
  - Self-hosted Keycloak may receive shared SMTP config injection, but ownership remains Keycloak.
  - Managed Keycloak mode does not require local SMTP injection.
  - Local delegation audit records exclude raw tokens and email body.
- Required Skills/Rules:
  - `auth-patterns`
  - `blazor-bff-patterns`
  - `error-tracking`
- Validation:
  - `lsp_diagnostics` clean for the changed Application contract, Keycloak adapter, and Infrastructure DI registration; build/tests are authority for files where C# diagnostics timed out.
  - `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed with 701 tests, 0 failed.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed with 2064 tests, 0 failed.
  - `dotnet build --configuration Release --verbosity quiet` passed with the existing warning baseline.
  - New tests cover verify/reset/update required-action calls, unsafe URL blocking without audit or HTTP, provider failure redaction after local delegation audit, and safe result serialization that excludes admin secrets, admin tokens, and raw provider failure bodies.

### 4.3 Document Keycloak theme and SMTP mode boundaries

- Status: Completed on 2026-07-08.
- Type: Documentation.
- Layer: Docs/Ops.
- Files:
  - `docs/CONFIGURATION.md`
  - `docs/SECURITY-MODEL.md`
  - `docs/OPERATIONS.md`
- Description: Documented `auth.provider = Keycloak`, `auth.identity_email_owner = AccountAuthority`, `auth.account_authority_kind = Keycloak`, `keycloak.smtp_mode`, and `keycloak.theme_sync_enabled` semantics. The docs now distinguish Keycloak-owned identity lifecycle email from ISLAMU product Basic Dispatch, even when local development or self-hosted deployments share SMTP plumbing.
- Implementation Notes:
  - `docs/CONFIGURATION.md` now describes the implemented `AccountAuthorityLifecycleEmail:*` and `KeycloakLifecycleEmail:*` sections plus logical owner policy labels.
  - `docs/SECURITY-MODEL.md` now defines the Keycloak identity email redaction boundary for results, logs, telemetry, and local delegation audit.
  - `docs/OPERATIONS.md` now documents the operational path: local delegation audit, Keycloak Admin REST `execute-actions-email`, Keycloak-owned theme rendering, and Keycloak realm SMTP delivery.
  - Context7 Keycloak documentation was used to verify email theme structure, realm SMTP configuration ownership, `execute-actions-email`, and development theme-cache flags.
- Acceptance Criteria:
  - Docs distinguish template/theme customization from sender ownership.
  - Docs state that shared SMTP credentials do not transfer email decision ownership to ISLAMU.
  - Local Mailpit/dev behavior is documented without implying production defaults.
- Validation:
  - Read-back verified updated sections in `docs/CONFIGURATION.md`, `docs/SECURITY-MODEL.md`, `docs/OPERATIONS.md`, and this active checklist.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed.
  - `dotnet build --configuration Release --verbosity quiet` passed with the existing warning baseline.

## Phase 5 — Account Authority: ATProto OIDC / PDS And Future ISLAMU PDS Hosting

### 5.1 Add ATProto/PDS account-authority email ownership policy

- Status: Completed on 2026-07-08.
- Type: Application policy.
- Layer: Application/Configuration.
- Files:
  - `docs/CONFIGURATION.md`
  - `docs/SECURITY-MODEL.md`
  - `docs/FEDERATION.md`
  - `dev/active/email-responsibility-architecture/future-islamu-identity-project.md`
- Description: Encoded that ATProto/PDS identity emails belong to the PDS/account authority while ISLAMU Event remains relying party/client. If ISLAMU later operates a PDS, that PDS cell still owns PDS credential emails.
- Implementation Notes:
  - `docs/CONFIGURATION.md` now documents logical `auth.identity_email_owner = AccountAuthority` and `auth.account_authority_kind = AtprotoPds` / `IslamuOperatedPds` semantics for future ATProto login.
  - `docs/SECURITY-MODEL.md` now defines the ATProto/PDS identity email boundary and redaction requirements for PDS confirmation/reset/migration credential flows.
  - `docs/FEDERATION.md` now states federation is foundation-only today and that future PDS credential emails must not route through product `EmailDispatchOutbox`/SMTP paths.
  - Context7 AT Protocol documentation and AT Explore lexicons verified that the PDS handles account lifecycle/security/email delivery and exposes email confirmation/password reset APIs.
- Acceptance Criteria:
  - ISLAMU Event and the future Identity Microservice do not directly send ATProto account verification, password reset, email change, migration confirmation, or PDS security emails.
  - If `email` or `email_verified` is unavailable/unverified, ISLAMU product email requires app-level notification email or falls back to in-app notification.
  - User notification email is clearly separate from identity email.
- Required Skills/Rules:
  - `auth-patterns`
  - `clean-architecture-rules`
- Validation:
  - Read-back verified updated sections in `docs/CONFIGURATION.md`, `docs/SECURITY-MODEL.md`, `docs/FEDERATION.md`, and `future-islamu-identity-project.md`.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed.
  - `dotnet build --configuration Release --verbosity quiet` passed with the existing warning baseline.
  - Policy/config unit tests remain future work once ATProto login exists.
  - Docs state current repo has federation foundation only and public ATProto OAuth/login is not implemented yet.

### 5.2 Add app-level notification email flow for unverified identity email

- Status: Completed on 2026-07-08.
- Type: Product/security flow.
- Layer: Application + API/UI later.
- Files:
  - `Explore.Application/Contracts/Services/IRegistrationNotificationDeliveryService.cs`
  - `Explore.Application/Services/RegistrationNotificationDeliveryService.cs`
  - `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs`
  - `Explore.API/Controllers/ExploreControllerBase.cs`
  - `Event.Application.UnitTests/Features/EventRegistrations/Commands/CreateEventRegistrationCommandHandlerTests.cs`
  - `Event.API.IntegrationTests/Features/UserControllerTests.cs`
- Description: Provide a safe path for product email when identity provider does not provide a verified email claim.
- Implementation Notes:
  - Registration confirmation email now uses `User.Email` only when `User.EmailVerified == true`.
  - Missing or unverified identity email no longer blocks registration and no longer creates `EmailDispatchOutbox`; the handler creates an in-app `Notification` fallback after the registration intent is durably accepted.
  - ATProto/PDS email is explicit-only: `email_verified` must be present and true before the API sync path persists it as verified. Without that claim, synced ATProto email remains unverified product-notification data.
  - This slice does not add a full app-level notification-email verification UI/API. It installs the safety boundary and fallback; a future management surface must use HAL affordances if exposed.
- Acceptance Criteria:
  - App-level notification email does not become identity credential email. ✅
  - Verification state is auditable and tenant-safe. ✅
  - In-app notification fallback works when no verified notification email exists. ✅
- Required Skills/Rules:
  - `auth-patterns`
  - `cqrs-mediatr-guidelines`
  - `blazor-ui-conventions` if UI is included.
- Validation:
  - `lsp_diagnostics` clean on changed source/test files.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed: 2078/2078.
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=Fast]" --minimum-expected-tests 1` passed: 40/40.
  - UI management actions were not added in this slice, so no HAL affordance surface was exposed.

### 5.3 Model future ISLAMU PDS Hosting Platform cells

- Status: Pending.
- Type: Future architecture documentation.
- Layer: Identity/PDS platform docs.
- Files:
  - `dev/active/email-responsibility-architecture/future-islamu-identity-project.md`
  - Future canonical Identity Project docs when that workstream is created.
- Description: Define the future PDS topology as multi-account PDS cells/shards/clusters by default, not one PDS process/database/container per user.
- Acceptance Criteria:
  - PDS cells track capacity, region, tenant policy, health, migration status, and account allocation metadata.
  - Dedicated PDS cells are deferred to premium, organization, sovereign, regulatory, or hard-isolation cases.
  - PDS SMTP settings are documented as PDS account-authority transport, not ISLAMU Event product-email ownership.
- Required Skills/Rules:
  - `agentic-research`
  - `auth-patterns`
  - `clean-architecture-rules`
- Validation:
  - Docs read-back confirms no one-PDS-per-user default.

### 5.4 Add Identity Microservice boundary and account mapping audit

- Status: Pending.
- Type: Future architecture documentation.
- Layer: Identity service boundary.
- Files:
  - `dev/active/email-responsibility-architecture/future-islamu-identity-project.md`
  - Future Identity Project data model/docs.
- Description: Keep the future Identity Microservice as provisioning, mapping, policy, and audit orchestration around account authorities.
- Acceptance Criteria:
  - Identity stores mappings between ISLAMU users and account authority subjects/DIDs/handles/PDS cells.
  - Identity records account lifecycle delegation audit when ISLAMU initiates account-authority actions.
  - Identity never generates PDS password-reset links, email-confirmation codes, migration confirmation codes, or PDS credential email bodies.
- Required Skills/Rules:
  - `auth-patterns`
  - `clean-architecture-rules`
- Validation:
  - Docs read-back confirms Identity orchestration-only boundary.

## Phase 6 — Moderation, Reporting, And External Provider Delegation

### 6.1 Route report received and report decision notifications through local ownership

- Status: Pending.
- Type: Application flow.
- Layer: Application.
- Files:
  - Existing report command/decision handlers verified during implementation.
  - Existing: `Explore.Application/Services/EventModerationNotificationFanoutService.cs`
  - New notification orchestrator files.
- Description: Ensure reporter/organizer/user-facing report and moderation emails are ISLAMU-owned by default, using local notification intents and existing safe moderation fanout patterns.
- Acceptance Criteria:
  - Report submitted, report decision, organizer moderation notice, attendee light moderation notice, and heavy redaction generic notice are ISLAMU-owned by default.
  - Coop/Osprey/provider internal reviewer emails remain provider-owned.
  - Heavy redaction email payload excludes event title, slug, public URL, description, image, organizer identity, unsafe evidence, and storage object path/key.
- Required Skills/Rules:
  - `auth-patterns`
  - `outbox-pattern`
  - `error-tracking`
- Validation:
  - Moderation/reporting unit/integration tests.
  - Heavy redaction payload tests prove generic/linkless safe content.

### 6.2 Add explicit Coop delegated user-facing email mode

- Status: Pending.
- Type: External-provider integration.
- Layer: Application + Infrastructure + API.
- Files:
  - Existing Coop callback/verifier files verified during implementation.
  - New notification external delegation files from Phase 2.
  - Config/docs for `coop.user_facing_moderation_emails`.
- Description: Support advanced delegated mode only when explicitly configured, with local intent and delegation audit first.
- Acceptance Criteria:
  - Default remains no Coop user-facing ISLAMU email.
  - Delegated mode requires local notification intent before provider call.
  - Delegation stores local notification id, report id, decision id, recipient kind, template key, safe payload hash, external provider id, delegation status, and returned delivery status if any.
  - Coop never receives raw report evidence or unsafe event content for email payloads.
- Required Skills/Rules:
  - `auth-patterns`
  - `outbox-pattern`
  - `error-tracking`
- Validation:
  - Coop callback tests.
  - Delegated-mode config validation tests.
  - Redaction/logging tests.

### 6.3 Preserve provider callback-to-local-decision flow

- Status: Pending.
- Type: Integration safety.
- Layer: API + Application.
- Files:
  - Existing Coop/Osprey callback controllers/handlers verified during implementation.
  - Existing moderation decision command files.
- Description: Keep the flow where provider decisions call back into ISLAMU, ISLAMU creates `EventReportDecision`, executes the local moderation command, and then sends user-facing notifications by local policy.
- Acceptance Criteria:
  - Provider callbacks remain machine-authenticated and idempotent.
  - Local audit/outbox/cache/notification behavior is reused.
  - Provider errors and payloads remain redacted in logs/admin surfaces.
- Required Skills/Rules:
  - `auth-patterns`
  - `clean-architecture-rules`
  - `error-tracking`
- Validation:
  - Existing incoming webhook framework tests.
  - API integration tests for callback idempotency.

## Phase 7 — Admin Configuration API And UI

### 7.1 Add admin read API for email ownership routing

- Status: Pending.
- Type: API endpoint.
- Layer: API + Application.
- Files:
  - New controller/query/DTO/HAL policy files verified during implementation.
- Description: Expose current effective routing by category so operators understand who owns each notification purpose.
- Acceptance Criteria:
  - GET endpoint is appropriately authorized for operator/admin context if exposing tenant-sensitive config; do not assume public GET if content is administrative.
  - Response includes HAL affordances for editable routes only.
  - Secrets, SMTP credentials, provider tokens, raw emails, and unsafe payloads are never returned.
- Required Skills/Rules:
  - `auth-patterns`
  - `cqrs-mediatr-guidelines`
  - `clean-architecture-rules`
- Validation:
  - API integration tests.
  - Authorization parity/HAL tests.

### 7.2 Add admin write API for safe routing changes

- Status: Pending.
- Type: API endpoint.
- Layer: API + Application + Persistence.
- Files:
  - New command/validator/repository/migration files verified during implementation.
- Description: Allow controlled category routing changes under hierarchy locks and explicit delegation safety rules.
- Acceptance Criteria:
  - Writes require authorization.
  - Validators are manually instantiated.
  - External user-facing delegation cannot be enabled without local audit support and safe payload restrictions.
  - Higher-tier setting locks block lower-tier overrides.
- Required Skills/Rules:
  - `auth-patterns`
  - `cqrs-mediatr-guidelines`
  - `dotnet-efcore-guidelines`
- Validation:
  - Command unit tests.
  - API integration tests for lock behavior and forbidden transitions.

### 7.3 Add Blazor admin UI for routing and delegation status

- Status: Pending.
- Type: UI.
- Layer: Blazor/BFF.
- Files:
  - New or existing admin settings components verified during implementation.
  - Generated API client/service wrapper files.
  - Component `.razor.css` files if needed.
- Description: Provide a safe operator view for category ownership, effective route, delegation mode, and dispatch health links.
- Acceptance Criteria:
  - UI calls scoped services, not generated clients directly from Razor.
  - Buttons/toggles are gated by HAL `_links`, not local roles/claims.
  - MudBlazor v9 APIs and shared wrapper/design-token conventions are used.
  - CSS uses BEM and scoped isolation; no global `.mud-*` overrides outside approved file.
  - Accessibility basics: labels, keyboard operation, focus management, and clear destructive delegation warnings.
- Required Skills/Rules:
  - `blazor-bff-patterns`
  - `blazor-ui-conventions`
  - `blazor-css-isolation`
  - `design-system`
  - `auth-patterns`
- Validation:
  - Blazor client tests where existing patterns support it.
  - Manual browser QA through BFF surface.

## Verification Checklist

Run only the scopes touched by each slice, plus the canonical build when the implementation is ready for review.

- [ ] `lsp_diagnostics` clean for every changed source file.
- [ ] `dotnet build --configuration Release --verbosity quiet` exits 0 or pre-existing failures are documented.
- [ ] Architecture tests for dependency, naming, authorization parity, and Blazor client boundaries when relevant.
- [ ] Application unit tests for resolver/orchestrator/category routing.
- [ ] Persistence integration tests for new notification tables and tenant filters.
- [ ] API integration tests for admin/config endpoints and provider callback behavior.
- [ ] Email dispatch tests proving Basic Dispatch Mode still drains `EmailDispatchOutbox` through `IEmailService`.
- [ ] Moderation/reporting tests proving heavy redaction remains safe and generic.
- [ ] Blazor UI tests/manual QA if admin UI is changed.
- [ ] Docs updated: `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/API.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/CONFIGURATION.md`, `docs/DEPLOYMENT_MODES.md`, `docs/MULTI_TENANCY.md`, `docs/FEDERATION.md`, `docs/OPERATIONS.md`, `docs/OUTBOX_PATTERN.md`, `docs/BLAZOR.md`, and `dev/active/email-responsibility-architecture/future-islamu-identity-project.md` as applicable.
- [ ] Dev docs updated before final response.

## Remaining / Deferred Work

- ATProto/PDS identity-email handling depends on future public ATProto OAuth/login implementation; current repo evidence shows federation foundation but not public ATProto login.
- Future ISLAMU PDS hosting needs separate capacity/cell/shard/migration/SLO design before implementation; default remains multi-account cells, not one PDS per user.
- Optional dedicated PDS cells are deferred until premium, organization, sovereign, regulatory, or hard-isolation requirements justify the operational cost.
- Future Identity Project docs must preserve the boundary that Identity orchestrates provisioning/mapping/audit and account authorities mint credentials and send credential lifecycle emails.
- Admin UI should wait until API/HAL shape is stable.
- Additional delivery providers beyond SMTP should be added only when required; current `SmtpEmailService` plus local Mailpit and existing dispatch worker are enough for initial ownership work.
- External provider user-facing email delegation should remain off until safe payload contracts, local audit, and provider capabilities are proven.
- A new `.claude/contract/intents.yaml` intent for notification ownership architecture may be useful after the first implementation slice reveals recurring file/rule scope.
