<!-- ABOUTME: Repository-grounded implementation plan for email responsibility ownership across identity, product, moderation, and provider workflows. -->
<!-- ABOUTME: Separates notification ownership decisions from delivery providers while extending the existing EmailDispatchOutbox system. -->

# Email Responsibility Architecture — Implementation Plan

Last Updated: 2026-07-07 Europe/Brussels

## 0 Planning Metadata

- Task name: `email-responsibility-architecture`
- Scope: active implementation workstream. Phase 1 Application ownership foundation is implemented; later phases remain planned.
- Workstream directory: `dev/active/email-responsibility-architecture/`
- Prior related workstream: `dev/pause/email-smtp-abstraction/` verified as narrower SMTP/provider work. This plan supersedes its architectural scope but should reuse verified delivery primitives rather than duplicate them.
- Core architecture rule to preserve: The system that owns the lifecycle owns the email decision. The account authority that owns, creates, and verifies a credential token owns the identity lifecycle email for that credential. The ISLAMU Notification subsystem owns delivery/audit for ISLAMU product-domain emails. External providers may send their own internal emails, but user-facing ISLAMU emails should stay under ISLAMU unless explicitly delegated. Shared SMTP is transport, not ownership.

### 0.1 Contract Classification

No exact `.claude/contract/intents.yaml` intent matched a pure `/dev-docs` architecture-planning task. The current planning work therefore uses a fallback dev-docs contract. Each future implementation slice must re-open `.claude/contract/intents.yaml` and classify itself before editing code.

| Field | Fallback Planning Contract |
| --- | --- |
| id/title | `fallback-dev-docs-plan` / Repository-grounded dev-docs implementation plan |
| must_read_docs | `AGENTS.md`, `dev/active/README.md`, `.claude/contract/intents.yaml`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, plus feature docs listed in the evidence log |
| load_skills | `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `blazor-bff-patterns`, `outbox-pattern`, `error-tracking`, `aspire`, `blazor-ui-conventions`, `blazor-css-isolation`, `design-system` |
| load_rules | Matching `.claude/rules/*.md` for future edited paths. Current planning touched docs only; future slices must reload API/Application/Persistence/Infrastructure/Blazor rules by path. |
| paths_in_scope | Current task: `dev/active/email-responsibility-architecture/**`. Future slices: only paths named in their re-classified intent and phase task. |
| minimum_tests | Current task: read-back/self-check of created markdown. Future slices: build plus intent-specific architecture/unit/integration tests. |
| docs_to_update | Current task: this plan, context, and tasks. Future implementation must update these files plus impacted canonical docs such as `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/API.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `docs/OUTBOX_PATTERN.md`, and Blazor/admin docs when applicable. |
| unique_acceptance | Cold agent can resume from docs; existing current state is evidence-tied; every phase has acceptance and validation; ownership policy is explicit and prevents single-provider email chaos. |
| forbidden_without_approval | Implementing code during planning; bypassing Clean Architecture; making Keycloak/PDS/Coop arbitrary senders for all ISLAMU emails; tenant-filter bypass; unsafe moderation payload email; weakening tests; creating compatibility shims not tied to shipped data/contracts. |

Prospective implementation intents to re-check per slice: `add-cqrs-handler`, `add-ef-migration`, `add-write-endpoint`, `add-get-endpoint`, `add-hal-link`, `blazor-component-affordance`, `external-infrastructure-bootstrap`, and auth/BFF bug intents when touching Keycloak or OIDC behavior. Add a contract-maintenance task if this repository wants a first-class `architecture-dev-docs` intent.

## 1 Executive Summary

The current codebase already has a product email delivery spine: `EmailDispatchOutbox`, `IEmailService`, `SmtpEmailService`, `EmailDispatchDrainService`, email-dispatch admin APIs, and optional RabbitMQ pointer transport. The missing architecture is not SMTP. The missing architecture is ownership: deciding which system is allowed to decide that an email should exist, which token/link/template/audit rules apply, and whether an external provider may contact an ISLAMU user.

The implementation should add a notification ownership layer above the existing dispatch infrastructure:

- Identity lifecycle emails are account-authority-owned. Keycloak sends Keycloak verification/reset/update/required-action emails. An ATProto/PDS account authority sends ATProto/PDS account verification, password reset, email-change, migration confirmation, and account-security emails. If ISLAMU later operates a PDS, that ISLAMU-operated PDS owns PDS account lifecycle emails; the ISLAMU Identity Microservice only orchestrates provisioning, mapping, and audit.
- ISLAMU product lifecycle, event lifecycle, registration, trust-safety, reporting, moderation, tenant, and platform emails are ISLAMU-owned. They should create local notification intent/audit rows and use the ISLAMU email dispatch subsystem.
- Provider-internal workflow emails may remain provider-owned, for example Coop reviewer assignment or Keycloak internal admin/server events.
- External providers may send user-facing ISLAMU emails only through explicit delegated notification channels with local notification intent, safe payload rules, external delegation audit, and delivery status tracking where available.

## 2 Source-Grounded Current State Report

### 2.1 Evidence Log

| Evidence | Finding | Planning Impact |
| --- | --- | --- |
| Verified: `docs/ARCHITECTURE.md` | Current architecture is Clean Architecture + CQRS + BFF. Background services/outbox own side effects; handlers/controllers must create durable intent only. `EmailDispatchOutbox` is documented as Basic Dispatch Mode for registration confirmation and future lifecycle emails. | Future work extends existing outbox/worker boundaries instead of sending SMTP directly from handlers. |
| Verified: `docs/DOMAIN.md` | Domain includes `Notification`, `NotificationFanoutRun`, `OutboxMessage`, `EmailDispatchOutbox`, and event-report/moderation entities. Report provider-sync outbox payloads must be metadata-only. | Notification intent model must keep safe payload boundaries and avoid leaking report evidence. |
| Verified: `docs/API.md` | Email dispatch admin APIs exist under `/api/admin/email-dispatch`; status redacts recipient email, subject, body, provider message ids, and raw provider errors. Coop/Osprey callback APIs already exist. | Ownership/routing admin APIs must follow same operator and redaction style. |
| Verified: `docs/SECURITY-MODEL.md` | Browser auth state excludes sensitive claims/tokens. Heavy redaction notifications must be generic and avoid event identity. Email dispatch operator APIs require `islamuevent_email_dispatch`. | Moderation email expansion must preserve heavy-redaction privacy and email-dispatch operator boundaries. |
| Verified: `docs/AUTHORIZATION.md` | GETs default anonymous, writes authorized, resource checks in MediatR/HATEOAS. EmailDispatch actions include View, ManageTenant, Park, Replay. Coop/Osprey callbacks use machine auth. | Any admin UI/API for ownership rules needs HAL-gated actions and secure request authorization. |
| Verified: `docs/CONFIGURATION.md` | `EmailDispatchProcessor:*`, `EmailDispatchRabbitMq:*`, SMTP governance keys, `PublicBaseUrl`, reporting provider settings, and Keycloak config are documented. RabbitMQ dispatch is optional pointer-only mode. | Add category routing config, not a single `emails.provider = Keycloak`. |
| Verified: `docs/OPERATIONS.md` | Local-full Aspire includes Mailpit, Keycloak, RabbitMQ, Coop, Osprey. SMTP and email-dispatch health checks exist. Foreground `aspire run --apphost Explore.AppHost/Explore.AppHost.csproj --isolated` is the canonical local proof in this repo. | Operational validation should include health checks and local-full dispatch proof when code is implemented. |
| Verified: `docs/OUTBOX_PATTERN.md` | General `OutboxMessage` and specialized `EmailDispatchOutbox`, `PolicyChangeOutbox`, `PdsSyncOutbox` variants exist. Email dispatch is durable PostgreSQL state; RabbitMQ only wakes/points. | Notification intent should follow durable state first and can optionally wake workers through RabbitMQ. |
| Verified: `docs/FEDERATION.md` | ATProto/PDS foundation exists, but public ATProto OAuth/login and protocol endpoints are not implemented. | ATProto identity-email ownership belongs in future architecture/config, not current implementation claims. |
| Verified external source: `https://github.com/bluesky-social/pds` | Official PDS docs describe a Personal Data Server capable of federation, document SMTP settings for verifying users' email addresses and sending other PDS emails, and include sizing guidance for 1-20 users on one PDS. | A PDS can host multiple accounts; do not design one PDS process/database/container per user by default. PDS SMTP is transport for PDS-owned account emails. |
| Verified external source: `https://atproto.com/specs/account` | AT Protocol accounts live on a PDS; the PDS provides repository hosting, authorization/authentication, and blob storage; identities can migrate between PDS hosting providers. | Treat the PDS as the account authority for ATProto account lifecycle, including future ISLAMU-operated PDS cells. |
| Verified external source: `com.atproto.server.requestEmailConfirmation` lexicon | The PDS API requests an email with a code to confirm ownership of an email address. | PDS email confirmation is a PDS account-authority email, not an ISLAMU Event product email. |
| Verified external source: `com.atproto.server.requestPasswordReset` lexicon | The PDS API initiates account password reset by email. | PDS password reset is account-authority-owned even when the PDS is operated by ISLAMU. |
| Verified: `Explore.Domain/EmailDispatchOutbox.cs::EmailDispatchOutbox` | Entity has tenant, source, recipient, subject/body snapshots, retry/dead-letter/park/unknown state, provider id, correlation id, RabbitMQ metadata, audit, and soft-delete fields. | Existing dispatch row can remain delivery-state primitive; do not replace it with a provider-specific table. |
| Verified: `Explore.Application/Contracts/Infrastructure/IEmailService.cs::IEmailService` | `SendAsync(EmailMessage, CancellationToken)` and `TestConnectionAsync(CancellationToken)` define the current delivery abstraction. | Delivery provider remains separate from notification owner. Keycloak is not an arbitrary `IEmailService` for product emails. |
| Verified: `Explore.Infrastructure/Mail/SmtpEmailService.cs::SmtpEmailService` | SMTP implementation builds MIME messages, configures SMTP security, and returns `EmailResult`. | Existing SMTP delivery provider should be reused as first concrete `IEmailDeliveryProvider` or adapted carefully. |
| Verified: `Explore.Infrastructure/EmailDispatchDrainService.cs::EmailDispatchDrainService` | Drain service claims due rows, rebinds tenant context, checks unsubscribe/preferences, builds `EmailMessage`, calls `IEmailService`, records receipts/attempts, and retries/dead-letters. | New ownership policy should enqueue into this flow for ISLAMU-owned emails. |
| Verified: `Explore.API/Controllers/EmailDispatchAdminController.cs::EmailDispatchAdminController` | Admin routes exist for status, tenant pause/resume, park, and replay. | Admin ownership/routing UI should compose with existing operator controls rather than create parallel controls. |
| Verified: `Explore.Application/Services/EventLifecycleEmailOutboxFactory.cs::EventLifecycleEmailOutboxFactory` | Creates product-domain email dispatch rows for registration confirmation, approval/rejection, waitlist promotion, reminders, cancellations, and organizer notifications. | Event/registration email ownership already trends ISLAMU-owned; generalize through notification intent/routing. |
| Verified: `Explore.Application/Services/EventModerationNotificationFanoutService.cs::EventModerationNotificationFanoutService` | Light/heavy moderation notification fanout exists with deterministic deduplication and separate heavy-redaction flow. | Trust-safety email work must reuse this distinction and keep heavy notifications generic. |
| Verified: `Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs::CompositeOutboxMessageDispatcher` | Routes durable outbox events to event-published fanout, moderation fanout, and report-provider sync; unknown types fail closed. | New event types should be explicit, idempotent, and fail closed. |
| Verified: `Explore.Application/Services/EventKeycloakIdentityContractContributor.cs::EventKeycloakIdentityContractContributor` | Contributes Keycloak realm/client desired state; no local identity-email sender was verified. | Keycloak remains the account authority for Keycloak lifecycle emails; ISLAMU may theme/configure realm but should not generate Keycloak lifecycle emails. |
| Implemented: `Explore.Application/Notifications/NotificationOwnership.cs`, `AccountAuthorityKind.cs`, `NotificationCategory.cs`, `ExternalWorkflowProviderKind.cs` | Phase 1 controlled vocabulary exists in Application, not Domain/Persistence, so no schema migration was needed for the first slice. | Later normalized persistence can reference these concepts without provider-specific coupling. |
| Implemented: `Explore.Application/Notifications/NotificationRoutingOptions.cs` | Category routing defaults and validation now keep identity lifecycle under `AccountAuthority`, product/event/registration/trust-safety/platform/marketing under `IslamuEvent`, provider-internal under `ExternalWorkflowProvider`, and external trust-safety delegation disabled unless explicit. | Provides the governing rule before adding durable intent tables or admin UI. |
| Implemented: `Explore.Application/Contracts/Notifications/INotificationOwnershipResolver.cs` and `Explore.Application/Notifications/DefaultNotificationOwnershipResolver.cs` | Resolver converts a safe `NotificationIntentDraft` into a `NotificationOwnershipDecision` without EF, SMTP, RabbitMQ, Keycloak, PDS, Coop, or Osprey dependencies. | Clean Architecture boundary is preserved; provider calls remain future delegated/orchestrated work. |
| Implemented: `Event.Application.UnitTests/Notifications/DefaultNotificationOwnershipResolverTests.cs` | Unit tests cover default account-authority routing, ISLAMU product routing, provider-internal routing, explicit external moderation delegation, invalid config, and cancellation. | Locks the first ownership rule before persistence/API work begins. |
| Implemented: `Explore.Domain/NotificationIntent.cs`, `NotificationDelivery.cs`, `NotificationExternalDelegation.cs` | Canonical durable intent, local delivery audit, and external delegation audit entities exist with UUIDv7 aggregate/audit ids, tenant fields, safe payload references/hashes, and no raw moderation/provider secret payload fields. | Phase 2 persistence now has normalized durable state above the existing email dispatch table. |
| Implemented: `Explore.Domain/NotificationIntentLookups.cs` and `Explore.Domain/Enums/NotificationIntentEnums.cs` | Persistent category, ownership, recipient, status, delivery status, delegation status, and external provider classifications use normalized lookup tables with stable integer enum companions. | Satisfies the normalized lookup-table requirement while preserving the repo invariant that lookups use `int` IDs. |
| Implemented: `Explore.Persistence/Configurations/Entities/NotificationIntentConfiguration.cs` and `NotificationIntentLookupConfigurations.cs` | EF maps normalized lookup tables, UUIDv7 intent/delivery/delegation ids, tenant/soft-delete filters, unique dedupe indexes, and restrict relationships to tenant/user/event/report/decision/email-dispatch rows. | Schema follows existing EF configuration and tenant-isolation conventions. |
| Implemented: `Explore.Application/Contracts/Persistence/INotificationIntentRepository.cs` and `Explore.Persistence/Repositories/NotificationIntentRepository.cs` | Repository boundary returns entities, supports exact-tenant dedupe lookup, and persists delivery/delegation audit rows without exposing `IQueryable` or DTOs. | Clean Architecture repository rules are preserved. |
| Implemented: `Explore.Persistence/Migrations/20260707125850_AddNotificationIntentPersistence.cs` | Migration creates lookup tables, durable intent/delivery/delegation tables, indexes, and reversible `Down` operations. | Database schema is ready for Phase 2.3 orchestration. |
| Implemented: `Event.Persistence.IntegrationTests/Repositories/NotificationIntentRepositoryTests.cs` | PostgreSQL/Testcontainers tests cover lookup seeding, normalized intent persistence, exact-tenant dedupe lookup, tenant filtering, and delivery/delegation audit rows. | Persistence surface is verified against the real database stack. |
| Implemented: `Explore.Domain/Enums/NotificationIntentEnums.cs` and `Explore.Domain/NotificationIntentLookups.cs` | Phase 2.3 added normalized `AccountAuthorityKindEnum` and `AccountAuthorityKindLookup` for Keycloak, ATProto/PDS, ISLAMU-operated PDS, local identity, and external OIDC. | Account-authority delegation audit no longer overloads external workflow provider strings or generic `Other` values. |
| Implemented: `Explore.Persistence/Migrations/20260707135125_AddNotificationAccountAuthorityDelegation.cs` | Migration adds `account_authority_kinds`, nullable account-authority FK on external delegation rows, indexes, and reversible `Down`. | Keycloak/PDS-style account-authority delegation is now normalized in persistence. |
| Implemented: `Explore.Application/Contracts/Notifications/INotificationOrchestrator.cs` and `Explore.Application/Notifications/DefaultNotificationOrchestrator.cs` | Application-only orchestrator calls ownership resolver and writes notification intent, delivery, and delegation entities through the repository boundary; it has no SMTP, RabbitMQ, Keycloak, PDS, Coop, Osprey, EF, API, or Blazor dependency. | Phase 2.3 connects policy resolution to durable intent persistence without crossing Clean Architecture boundaries. |
| Implemented: `Event.Application.UnitTests/Notifications/DefaultNotificationOrchestratorTests.cs` | Unit tests cover local ISLAMU delivery, account-authority delegation, non-initiated account authority, external workflow delegation, disabled routing, and required metadata validation. | Orchestrator behavior is locked before product email flows are migrated. |
| Verified by search: `KEYCLOAK_SMTP_*` matched `docker/keycloak/keycloak-init.sh`, `docker-compose.yml`, `Explore.AppHost/AppHost.cs` | Self-hosted/local Keycloak can receive SMTP env/config, especially Mailpit in dev. | Injecting shared SMTP into Keycloak does not transfer identity-email ownership to ISLAMU. |
| Verified by search: `EmailDispatchRabbitMq__*` matched `docker-compose.yml`, `Explore.AppHost/AppHost.cs`, `Explore.Infrastructure/Messaging/*` | RabbitMQ email-dispatch mode is optional and pointer-only. | Product email flow must keep DB outbox canonical even when RabbitMQ is enabled. |
| Verified by search: `CoopIncomingWebhookVerifier`, `X-Coop-Timestamp`, `X-Coop-Signature` matched `Event.API.IntegrationTests/Features/IncomingWebhookFrameworkTests.cs` | Coop callback signature verification exists in tests. | Coop may remain provider-internal/reviewer workflow; user-facing moderation emails require explicit delegation audit. |
| Remaining planned: `IIdentityLifecycleEmailService` or equivalent account-authority lifecycle delegation service | Ownership resolver, durable intent model, repository, and orchestrator now exist; account-authority lifecycle request services for Keycloak/PDS are still future work. | Next account-authority slice should record initiated Keycloak/PDS actions through the orchestrator without sending credential emails locally. |

### 2.2 Existing Implementation

Current strengths:

- Product email dispatch already has durable state in `Explore.Domain/EmailDispatchOutbox.cs`.
- The Application layer already creates event/registration email dispatch rows through `EventLifecycleEmailOutboxFactory`.
- The Infrastructure layer already performs SMTP delivery through `SmtpEmailService` and processes queued rows through `EmailDispatchDrainService`.
- Email dispatch has admin controls, health checks, retry/dead-letter behavior, tenant pause, receipts, unsubscribe checks, preference checks, and optional RabbitMQ pointer transport.
- Moderation/reporting already uses local report/case/decision records, safe outbox payload rules, provider callback handling, and separate light/heavy notification fanout.
- Keycloak realm/client bootstrap is contract-driven through `EventKeycloakIdentityContractContributor`, while self-hosted/local SMTP injection is available through AppHost/Docker config.

Current missing architecture:

- Application-level ownership policy and orchestrator now exist, but product/event/registration flows have not been migrated to call them yet.
- No product/event/registration command or factory path has been migrated yet to call the Phase 2.3 orchestrator before creating `EmailDispatchOutbox` rows.
- No account-authority lifecycle service yet requests Keycloak/PDS actions and records those requests through the orchestrator without pretending ISLAMU Event or the Identity Microservice owns credential emails.
- No API/admin-backed routing configuration surface exists yet for identity/product/event/registration/trust-safety/provider-internal/marketing categories.
- No verified admin UI for routing/ownership settings.

### 2.3 Existing Tests And Verification Coverage

Verified or search-backed tests include:

- Email dispatch: `Event.API.IntegrationTests/Features/EmailDispatchHealthCheckTests.cs`, `EmailDispatchTickerQJobsTests.cs`, `EventRegistrationRealRuntimeTests.cs`, `Event.API.IntegrationTests/Features/EmailDispatchAdminControllerTests.cs`, `Event.Application.UnitTests/Services/EventLifecycleEmailOutboxFactoryTests.cs`, and telemetry tests.
- Moderation/reporting: `Event.Application.UnitTests/Services/EventModerationNotificationFanoutServiceTests.cs`, `EventPublishedNotificationFanoutServiceTests.cs`, and callback/webhook integration tests.
- Auth/Keycloak bootstrap: `Event.API.IntegrationTests/Features/KeycloakBootstrapRealRuntimeTests.cs`, `InstanceOnboardingControllerTests.cs`, plus Application unit tests for Keycloak bootstrap/secret rotation handlers.
- Architecture guardrails: `Event.Architecture.Tests` covers Clean Architecture, CQRS, naming, authorization parity, and Blazor architecture checks per loaded skills.

Gaps to cover:

- Product lifecycle integration tests that prove existing `EventLifecycleEmailOutboxFactory` behavior is preserved when notification intent audit is added before `EmailDispatchOutbox` creation.
- External delegation idempotency tests at the future provider boundary.
- Keycloak identity lifecycle delegation tests.
- ATProto missing/unverified email fallback tests.
- Moderation heavy-redaction email payload tests.
- Admin UI HAL affordance tests for routing controls.

### 2.4 Existing Documentation And Contracts

Existing docs already describe the core delivery infrastructure: `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/API.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `docs/OUTBOX_PATTERN.md`, `docs/MULTI_TENANCY.md`, `docs/FEDERATION.md`, `docs/BLAZOR.md`, and `docs/CODEBASE_STRUCTURE.md`.

Documentation gap: those docs explain dispatch and provider integrations, but they do not yet state the cross-provider email responsibility policy as a canonical architecture decision.

### 2.5 Current Pain Points / Improvement Areas

- Email responsibility can be confused with SMTP credentials. The plan must separate who decides to notify from which SMTP/API provider delivers.
- Keycloak SMTP injection could be misread as ISLAMU owning identity emails. The plan must say Keycloak still owns identity lifecycle decisions.
- Future ISLAMU PDS SMTP could be misread as ISLAMU Event or Identity Microservice owning PDS credential emails. The plan must say the PDS account authority owns those emails even when ISLAMU operates the PDS infrastructure.
- A naive one-PDS-per-user topology would over-fragment operations. The default future model should be multi-account PDS cells/shards/clusters, with dedicated PDS only for premium, organizational, sovereign, or isolation-driven cases.
- Coop/Osprey moderation provider callbacks already feed local decisions; provider user-facing emails need explicit delegation guardrails so raw report evidence or unsafe event content never leaves local control.
- Existing `EmailDispatchOutbox` stores recipient/subject/body snapshots, which is useful for delivery but requires careful operator redaction and safe moderation payload rules.
- RabbitMQ is optional wake-up transport. Treating it as canonical state would break current Basic Dispatch Mode guarantees.

### 2.6 Unknowns After Investigation

- Exact `.claude/rules/*.md` path-specific rule files for future code edits were not enumerated in this planning pass; implementation agents must reload matching rules by edited path.
- LSP workspace symbol lookups for proposed new ownership symbols timed out. Future implementation should re-check names before creating new types.
- ATProto OIDC/PDS login is foundation-only in current docs; exact future OIDC provider behavior remains design work.
- Future ISLAMU PDS hosting topology, cell capacity, migration tooling, and operational SLOs remain Identity Project design work.
- The future ISLAMU Identity Microservice boundary must be kept narrow: provisioning/mapping/audit/orchestration, not PDS token minting or PDS credential email generation.
- Existing email templates and tenant-branding primitives were not deeply inspected beyond SMTP/config and event lifecycle factories.
- Current Coop production capabilities for user-facing delegated emails are not verified; treat delegated Coop user emails as advanced/off by default.

## 3 Proposed Future State

### 3.1 Ownership Categories

| Category | Owner | Default Sender | Examples |
| --- | --- | --- | --- |
| Identity lifecycle | Account authority | Keycloak, external PDS, ISLAMU-operated PDS cell, or future local account authority | verify email, reset password, email change verification, migration confirmation, MFA/required action, password changed |
| ISLAMU product lifecycle | ISLAMU Event | ISLAMU notification/email subsystem | welcome after first login, event published, tenant invite, organizer verification |
| Registration lifecycle | ISLAMU Event | ISLAMU notification/email subsystem | registration approved/rejected, waitlist promotion, event reminder, cancellation |
| Trust-safety reporting | ISLAMU Event | ISLAMU notification/email subsystem | report received, report decision, case update |
| Trust-safety moderation | ISLAMU Event | ISLAMU notification/email subsystem | organizer moderation notice, attendee light moderation notice, heavy redaction generic notice |
| Provider-internal workflow | External provider | Provider | Coop reviewer assignment, Coop admin notification, Keycloak admin/server event email |
| External delegated user notification | ISLAMU-owned decision, external provider delivery | Explicit provider only when configured | Coop reporter/organizer email only with local intent/audit/safe payload |
| Ticket/payment receipt | Transaction-owning provider if delegated | Ticketing/payment provider | receipt, refund notice, charge dispute notice |

### 3.2 Required Decision Rule

Identity/security-token emails are sent by the account authority that creates, verifies, or controls the credential token. ISLAMU business-state emails are created and owned by ISLAMU Event. Provider-console/workflow emails may be provider-owned. External provider email to ISLAMU users requires explicit delegation and local audit.

### 3.3 Desired ISLAMU-Owned Product Email Flow

1. Application command/domain event decides an ISLAMU business notification is needed.
2. Command creates `NotificationIntentDraft` with category, recipient kind, safe payload, tenant/organization/user context, template key, and source object reference.
3. `INotificationOwnershipResolver.ResolveAsync(...)` returns ownership and routing decision.
4. `INotificationOrchestrator.EnqueueAsync(...)` writes canonical `NotificationIntent` and either:
   - creates/links `EmailDispatchOutbox` for ISLAMU-owned email delivery, or
   - records external delegation state, or
   - marks disabled/provider-owned where no ISLAMU email should be sent.
5. PostgreSQL outbox remains canonical.
6. Optional RabbitMQ pointer publication wakes consumers faster, but never replaces the DB row.
7. Email worker/drain calls `IEmailDeliveryProvider`/current `IEmailService`, records attempts, receipts, status, and safe observability.

## 4 Non-Negotiable Constraints

- Repositories return entities, not DTOs.
- Validators are manually instantiated, not injected as `IValidator<T>`.
- Use `int` for lookups, `Guid`/UUIDv7 for aggregates/outbox, and `long` for cursors.
- GET endpoints default to `[AllowAnonymous]`; writes/control actions require `[Authorize]` and handler-level resource checks.
- HAL `_links` are the single source of truth for UI affordances; clients must not infer permissions from roles/claims.
- API-authoritative tenant isolation and EF tenant filters must fail closed.
- No Clean Architecture dependency violations: Domain has no external dependencies; Application does not use `ExploreDbContext`; Persistence/Infrastructure implement Application contracts.
- Every new file must start with two ABOUTME comment lines.
- No direct SMTP, RabbitMQ, TickerQ, or provider API side effect from handlers/controllers/domain. Create durable intent first.
- No compatibility shims unless tied to persisted data, shipped external contracts, or explicit approval.
- Heavy redaction emails must not include event title, slug, public URL, description, image, organizer identity, unsafe evidence, storage object path/key, raw provider payloads, or secrets.

## 5 Architecture And Design Decisions

### 5.1 Separate Ownership From Delivery

Add ownership concepts separate from delivery providers:

```csharp
public enum NotificationOwnership
{
    IslamuEvent,
    AccountAuthority,
    ExternalWorkflowProvider,
    Disabled
}

public enum AccountAuthorityKind
{
    Keycloak,
    AtprotoPds,
    IslamuOperatedPds,
    LocalIdentity,
    ExternalOidc
}

public enum NotificationCategory
{
    IdentityLifecycle,
    ProductLifecycle,
    EventLifecycle,
    RegistrationLifecycle,
    TrustSafetyReporting,
    TrustSafetyModeration,
    ProviderInternal,
    PlatformOperations,
    Marketing
}
```

`NotificationOwnership` answers who is allowed to decide/send/audit. Delivery providers answer how an ISLAMU-owned email leaves the system.

### 5.2 Delivery Provider Boundary

Proposed interface:

```csharp
public interface IEmailDeliveryProvider
{
    Task<EmailDeliveryResult> SendAsync(
        EmailDeliveryRequest request,
        CancellationToken cancellationToken);
}
```

Current `IEmailService`/`SmtpEmailService` can either become the SMTP implementation of this shape or be adapted by a thin wrapper. Do not make Keycloak or PDS a general-purpose `IEmailDeliveryProvider`; they own their account lifecycle emails, not arbitrary ISLAMU product emails. Do not make Coop a product email delivery provider unless a specific delegated moderation channel is configured and locally audited.

### 5.3 Suggested Data Model

- `notification_intents`: canonical local decision/audit record for ISLAMU-owned or delegated user-facing notifications.
- `notification_deliveries`: per-channel delivery attempt/status records, linked to current `EmailDispatchOutbox` where email is local.
- `notification_external_delegations`: external provider delegation audit with notification id, report/decision/source id, recipient kind, template key, safe payload hash, provider id, delegation status, and delivery status if returned.
- `email_routing_rules`: effective category routing and owner/provider policy, preferably seeded/defaulted through settings governance.
- `email_template_bindings`: category/template/provider binding with safe payload contract version.
- `identity_accounts`: future Identity Project account mapping from ISLAMU user to account authority, subject/DID/handle, authority kind, PDS cell when applicable, and lifecycle status.
- `identity_lifecycle_delegations`: local audit rows when ISLAMU initiates an account-authority lifecycle action such as Keycloak action email or PDS email confirmation request.
- `user_notification_addresses`: app-level product notification email addresses, separate from identity credential email claims.
- `pds_cells`: future ISLAMU PDS Hosting Platform cells/shards/clusters with capacity, region, tenant policy, health, and account allocation metadata.

Safe payload rule: store minimal rendered delivery snapshots when required for local email dispatch; store hashes/references for sensitive moderation payloads; never store raw report evidence in delegation payloads.

### 5.4 Suggested Configuration Shape

```yaml
email:
  enabled: true
  default_delivery_provider: Smtp
  outbox:
    transport: Database
    rabbitmq_enabled: false
  routing:
    identity_lifecycle: AccountAuthority
    product_lifecycle: IslamuEvent
    event_lifecycle: IslamuEvent
    registration_lifecycle: IslamuEvent
    trust_safety_reporting: IslamuEvent
    trust_safety_moderation: IslamuEvent
    provider_internal: ExternalWorkflowProvider
    marketing: IslamuEvent
auth:
  provider: Keycloak
  identity_email_owner: AccountAuthority
  account_authority_kind: Keycloak
keycloak:
  smtp_mode: ProviderManaged # or InjectSharedSmtp
  theme_sync_enabled: false
atproto:
  identity_email_owner: AccountAuthority
  account_authority_kind: AtprotoPds
  require_verified_email_for_product_email: true
pds:
  hosting_mode: SharedCells # SharedCells by default; DedicatedCell only for explicit advanced cases
  default_cell_strategy: CapacityAware
  dedicated_cell_enabled: false
coop:
  enabled: false
  internal_emails: true
  user_facing_moderation_emails: false
```

Deployment variants:

- Managed hosting: account authority sends identity email with authority-managed SMTP; ISLAMU sends product emails through ISLAMU delivery provider.
- Lightweight self-hosting: AppHost/Docker may inject shared Mailpit/SMTP into Keycloak, PDS, and ISLAMU delivery, but ownership still differs by category.
- Dev: Mailpit catches Keycloak/PDS account-authority emails and ISLAMU product emails; tests must distinguish the source/owner.

### 5.5 Future ISLAMU PDS Hosting Platform Boundary

Future ISLAMU can operate PDS infrastructure without making ISLAMU Event or the ISLAMU Identity Microservice the owner of PDS credential emails. The default model should be an ISLAMU PDS Hosting Platform made of multi-account PDS cells/shards/clusters. A cell can host many accounts, tracks capacity/region/tenant policy, and can be migrated or expanded operationally. Dedicated PDS cells are reserved for later premium, organizational, sovereign, regulatory, or hard-isolation requirements.

Boundary decisions:

- The PDS account authority owns PDS account verification, password reset, email-change, migration confirmation, and ATProto account-security emails.
- The ISLAMU Identity Microservice may orchestrate signup policy, PDS cell selection, handle reservation, account mapping, delegation audit, and account lifecycle status.
- The ISLAMU Identity Microservice must not mint PDS credential tokens, generate PDS password reset links, or send PDS account verification emails directly.
- Shared SMTP credentials or Mailpit routing do not change account authority ownership.
- ISLAMU Event consumes identity claims/mappings and sends product-domain notifications only.

### 5.6 Provider Guidance

- Cerbos/local authorization does not send email.
- Keycloak sends Keycloak account lifecycle email; ISLAMU may configure realm SMTP/theme but does not generate reset/verify/update-email content.
- ATProto/PDS owns ATProto account lifecycle email. ISLAMU is relying party/client and consumes claims. If email/email_verified is unavailable or false, ISLAMU product emails require an app-level notification email or in-app fallback.
- An ISLAMU-operated PDS cell is still the PDS account authority for its hosted ATProto accounts; Identity orchestrates provisioning/mapping/audit only.
- Coop/Osprey/moderation providers create signals/cases/decisions/callbacks. ISLAMU sends user-facing moderation/reporting by default.
- Ticket/payment provider may send receipts if it owns transaction lifecycle.
- Webhook providers such as Svix/local webhooks do not decide email purpose.

## 6 Implementation Phases

Each phase must update this plan, context, and tasks before handoff. Files below marked `New:` are planned paths and must be re-verified before implementation.

### Phase 0 — Plan Review And Baseline

- Type/Layer: docs and verification baseline.
- Files: `dev/active/email-responsibility-architecture/*`, `.claude/contract/intents.yaml` if adding a planning intent.
- Description: Review this plan with the user; decide whether to add a first-class architecture planning intent.
- Acceptance Criteria: User approves the ownership rule and first implementation slice; task added or resolved for a new intent.
- Dependencies: none.
- Effort: S.
- Required Skills/Rules: `source-command-check` for verification policy, `clean-architecture-rules` for future slice framing.
- Validation: read-back of docs; no code changes.

### Phase 1 — Notification Ownership Policy

Task 1.1: Add ownership/category enums and routing option model.

- Type/Layer: Application/domain-adjacent policy model.
- Files: New: `Explore.Application/Features/Notifications/Ownership/NotificationOwnership.cs`, `NotificationCategory.cs`, `NotificationRoutingOptions.cs` or existing equivalent if found.
- Description: Define controlled categories and allowed owners. Avoid arbitrary string categories.
- Acceptance Criteria: Every category from this plan has one default owner; account lifecycle routes include an explicit `AccountAuthorityKind`; invalid config fails validation; no delivery provider is treated as owner.
- Dependencies: Phase 0.
- Effort: S.
- Required Skills/Rules: `clean-architecture-rules`, `cqrs-mediatr-guidelines`.
- Validation: Application unit tests for default routing and invalid options; architecture tests.

Task 1.2: Add `INotificationOwnershipResolver`.

- Type/Layer: Application service.
- Files: New: `Explore.Application/Contracts/Notifications/INotificationOwnershipResolver.cs`, implementation under `Explore.Application/Services/` or feature folder.
- Description: Resolve `NotificationIntentDraft` to `NotificationOwnershipDecision` using category, provider settings, tenant lock settings, and explicit delegation flags.
- Acceptance Criteria: identity lifecycle resolves to `AccountAuthority` with a concrete `AccountAuthorityKind`; product/event/registration/trust-safety resolves to `IslamuEvent`; provider-internal resolves to `ExternalWorkflowProvider`; Coop user-facing moderation resolves to external provider only when explicit delegation config is enabled.
- Dependencies: Task 1.1.
- Effort: M.
- Required Skills/Rules: `cqrs-mediatr-guidelines`, `auth-patterns`, `dotnet-efcore-guidelines` if settings repositories are touched.
- Validation: unit tests for Keycloak, ATProto, Coop internal, Coop delegated, disabled email.

Task 1.3: Add routing config docs and validation.

- Type/Layer: configuration/docs.
- Files: `docs/CONFIGURATION.md`, `Explore.Infrastructure` or API option validation if config is bound there.
- Description: Document category routing config and enforce legal combinations.
- Acceptance Criteria: no `emails.provider = Keycloak`-style global config; account-authority ownership, Keycloak/PDS SMTP mode, and ISLAMU product delivery provider config are separately documented.
- Dependencies: Task 1.1.
- Effort: S.
- Required Skills/Rules: `error-tracking`, `aspire`.
- Validation: configuration extension tests and docs-lint.

### Phase 2 — Canonical Notification Intent Model

Task 2.1: Add `NotificationIntent` aggregate and delivery/delegation entities.

- Type/Layer: Domain + Persistence.
- Files: New: `Explore.Domain/NotificationIntent.cs`, `NotificationDelivery.cs`, `NotificationExternalDelegation.cs`; EF configurations and migration under `Explore.Persistence`.
- Description: Store local notification decision, category, owner, channel, recipient kind, safe payload hash/reference, source ids, template key, and status.
- Acceptance Criteria: intents are tenant-scoped, auditable, soft-delete compatible where appropriate, UUIDv7 ids, and do not store unsafe moderation evidence.
- Dependencies: Phase 1.
- Effort: L.
- Required Skills/Rules: `clean-architecture-rules`, `dotnet-efcore-guidelines`, `outbox-pattern`.
- Validation: persistence integration tests, migration snapshot review, architecture tests.

Task 2.2: Add Application contracts and repositories.

- Type/Layer: Application + Persistence.
- Files: New: `INotificationIntentRepository`, `INotificationExternalDelegationRepository`, Persistence repository implementations.
- Description: Repository methods return entities only and expose bounded queries for orchestrator/admin status.
- Acceptance Criteria: no DTO-returning repositories; no `IQueryable` escapes; tenant filter remains active.
- Dependencies: Task 2.1.
- Effort: M.
- Required Skills/Rules: `dotnet-efcore-guidelines`, `clean-architecture-rules`.
- Validation: repository integration tests and architecture tests.

Task 2.3: Add `INotificationOrchestrator.EnqueueAsync`.

- Type/Layer: Application service.
- Files: New: `Explore.Application/Contracts/Notifications/INotificationOrchestrator.cs`, implementation in Application.
- Description: Validate draft, resolve owner, create `NotificationIntent`, create `EmailDispatchOutbox` or external delegation as needed, and keep all durable writes in one unit of work.
- Acceptance Criteria: no direct SMTP/provider calls; idempotency/deduplication works for repeated source events; account-authority decisions create local delegation/audit only when ISLAMU initiated the action; returns local intent id.
- Dependencies: Tasks 1.2, 2.1, 2.2.
- Effort: L.
- Required Skills/Rules: `cqrs-mediatr-guidelines`, `outbox-pattern`.
- Validation: unit tests for local email, account-authority no-op/delegation record, external delegation, disabled route.

### Phase 3 — ISLAMU Email Delivery Providers

Task 3.1: Align `IEmailService` with delivery-provider naming without breaking behavior.

- Type/Layer: Application/Infrastructure.
- Files: existing `Explore.Application/Contracts/Infrastructure/IEmailService.cs`, `Explore.Infrastructure/Mail/SmtpEmailService.cs`; New wrapper only if needed.
- Description: Either keep `IEmailService` as the concrete delivery abstraction or add `IEmailDeliveryProvider` as a thin semantic wrapper around existing SMTP service.
- Acceptance Criteria: existing email dispatch tests still pass; delivery provider does not decide ownership.
- Dependencies: Phase 2.
- Effort: M.
- Required Skills/Rules: `clean-architecture-rules`, `error-tracking`.
- Validation: existing email dispatch unit/integration tests.

Task 3.2: Route orchestrated ISLAMU-owned intents into `EmailDispatchOutbox`.

- Type/Layer: Application service.
- Files: `EventLifecycleEmailOutboxFactory` or new adapter; `EmailDispatchOutbox` creation paths.
- Description: Reuse existing durable email dispatch rows for product/event/registration/trust-safety email delivery.
- Acceptance Criteria: registration/event email flows create both intent/audit and dispatch row; RabbitMQ optional pointer mode still works.
- Dependencies: Phase 2.
- Effort: M.
- Required Skills/Rules: `outbox-pattern`, `cqrs-mediatr-guidelines`.
- Validation: lifecycle factory tests plus email dispatch processor tests.

### Phase 4 — Account Authority: Keycloak Identity Email Delegation

Task 4.1: Add account-authority lifecycle delegation contract.

- Type/Layer: Application + Infrastructure.
- Files: New: `IAccountAuthorityLifecycleEmailService` or `IIdentityLifecycleEmailService` if that name remains clearer in code review; `KeycloakAccountAuthorityLifecycleEmailService`; disabled implementation.
- Description: Model actions such as request email verification, password reset, email update verification, and required action email as account-authority operations, not ISLAMU product emails.
- Acceptance Criteria: Keycloak implementation calls Keycloak admin/action-email APIs only for Keycloak account lifecycle; local audit/delegation row created when ISLAMU initiates the action; no product email uses Keycloak sender.
- Dependencies: Phase 2.
- Effort: M.
- Required Skills/Rules: `auth-patterns`, `blazor-bff-patterns`, `error-tracking`.
- Validation: unit tests with mocked Keycloak client; integration tests if existing Keycloak test harness supports it.

Task 4.2: Document Keycloak SMTP/theme ownership.

- Type/Layer: docs/config/ops.
- Files: `docs/CONFIGURATION.md`, `docs/SECURITY-MODEL.md`, `docs/OPERATIONS.md`, Keycloak bootstrap docs if present.
- Description: Clarify `auth.provider = Keycloak`, `auth.identity_email_owner = AccountAuthority`, `auth.account_authority_kind = Keycloak`, `keycloak.smtp_mode = ProviderManaged|InjectSharedSmtp`, and optional theme sync.
- Acceptance Criteria: docs state shared SMTP injection does not transfer lifecycle ownership.
- Dependencies: Task 4.1.
- Effort: S.
- Required Skills/Rules: `auth-patterns`, `aspire`.
- Validation: docs-lint and Keycloak bootstrap tests if config changed.

### Phase 5 — Account Authority: ATProto OIDC/PDS And Future ISLAMU PDS Hosting

Task 5.1: Add ATProto identity-email ownership rules.

- Type/Layer: Application/config.
- Files: ownership resolver/config docs; future ATProto auth files when implemented.
- Description: Treat ATProto/PDS as the account authority for ATProto identity lifecycle email. ISLAMU consumes claims only unless it is operating the PDS cell, in which case the PDS cell still owns those credential emails.
- Acceptance Criteria: ISLAMU Event and the Identity Microservice do not send ATProto account verification, password reset, email change, migration confirmation, or PDS security emails directly.
- Dependencies: Phase 1.
- Effort: S.
- Required Skills/Rules: `auth-patterns`, `clean-architecture-rules`.
- Validation: resolver unit tests.

Task 5.2: Add app-level product email fallback for unverified/missing OIDC email.

- Type/Layer: Application/API/Blazor if UI required.
- Files: New or existing user notification preference/contact email flows.
- Description: If `email`/`email_verified` is unavailable or unverified, require an app-level notification email or fall back to in-app notifications for product messages.
- Acceptance Criteria: product email uses verified app notification address, not identity email assumption; UI copy distinguishes notification email from identity email.
- Dependencies: Phase 2.
- Effort: M.
- Required Skills/Rules: `auth-patterns`, `blazor-ui-conventions`, `accessibility` if loaded by implementation agent.
- Validation: API/UI tests and manual QA through BFF surface.

Task 5.3: Model future ISLAMU PDS Hosting Platform cells.

- Type/Layer: Identity-platform architecture/docs-first model.
- Files: `dev/active/email-responsibility-architecture/future-islamu-identity-project.md`; future Identity Project docs and data model files when that project starts.
- Description: Capture the default topology as multi-account PDS cells/shards/clusters, not one PDS process/database/container per user.
- Acceptance Criteria: shared cells have capacity/region/tenant-policy/health/allocation metadata; dedicated cells are explicitly deferred to premium/org/sovereign/regulatory/hard-isolation cases; PDS SMTP settings are documented as PDS account-authority transport, not ISLAMU Event product email ownership.
- Dependencies: Phase 0 and future Identity Project approval.
- Effort: M.
- Required Skills/Rules: `auth-patterns`, `agentic-research`, `clean-architecture-rules`.
- Validation: docs review against PDS source evidence; future tests when Identity Project code exists.

Task 5.4: Add Identity Microservice boundary and account mapping audit.

- Type/Layer: Future Identity Project architecture.
- Files: future Identity Project docs/code; `docs/FEDERATION.md` and `docs/SECURITY-MODEL.md` when canonicalized.
- Description: Define Identity as provisioning/mapping/audit orchestration around account authorities, not the issuer of PDS credential tokens.
- Acceptance Criteria: Identity stores mappings/delegation audit; PDS account-authority emails are requested from the PDS; Identity never generates PDS password-reset links, email-confirmation codes, or PDS credential email bodies.
- Dependencies: Task 5.3.
- Effort: L.
- Required Skills/Rules: `auth-patterns`, `clean-architecture-rules`, `dotnet-efcore-guidelines`.
- Validation: future unit/integration tests for account allocation, delegation audit, and “Identity does not mint PDS tokens” boundaries.

### Phase 6 — Moderation And Reporting Email Integration

Task 6.1: Add ISLAMU-owned reporting/moderation email intents.

- Type/Layer: Application.
- Files: reporting/moderation command handlers/services; new templates/bindings.
- Description: Create local notification intents for report received, report decision, organizer moderation notice, attendee light moderation notice, and heavy-redaction generic notice.
- Acceptance Criteria: heavy-redaction emails omit unsafe event/report/provider fields; local audit exists before dispatch; duplicate decisions do not duplicate emails.
- Dependencies: Phase 2.
- Effort: L.
- Required Skills/Rules: `outbox-pattern`, `auth-patterns`, `error-tracking`.
- Validation: unit tests for safe payloads, deduplication, report decision flows, and processor dispatch.

Task 6.2: Add explicit external delegation mode for Coop user-facing emails.

- Type/Layer: Application/Infrastructure/config.
- Files: new external delegation service; Coop integration config and callback/status handling.
- Description: Allow `moderation.email.owner = ExternalProvider`, `moderation.email.provider = Coop` only when explicitly enabled. Always create local `NotificationIntent` and `NotificationExternalDelegation` first.
- Acceptance Criteria: delegated payload contains only safe fields/hash/template key; raw report evidence and unsafe event content never leave local system; delivery status is recorded if Coop returns one.
- Dependencies: Task 6.1.
- Effort: L.
- Required Skills/Rules: `auth-patterns`, `outbox-pattern`, `error-tracking`.
- Validation: integration tests for enabled/disabled delegation, HMAC callback idempotency, unsafe payload rejection.

### Phase 7 — Admin Configuration UI

Task 7.1: Add admin API for routing policy/status.

- Type/Layer: API + Application.
- Files: New: routing policy queries/commands/controllers; HATEOAS policy/assembler.
- Description: Expose safe status and allowed control actions for notification/email ownership routing.
- Acceptance Criteria: GET status redacts secrets/templates/payload; writes require authorized operator; HAL links expose allowed edit/delegate/test actions.
- Dependencies: Phases 1-2.
- Effort: M.
- Required Skills/Rules: `auth-patterns`, `cqrs-mediatr-guidelines`, `error-tracking`.
- Validation: API integration tests, authorization parity tests.

Task 7.2: Add Blazor admin UI.

- Type/Layer: Blazor BFF/client.
- Files: New components/services under existing admin/settings structure.
- Description: Display category routing, account-authority ownership, local delivery provider status, delegated provider status, and safe warnings.
- Acceptance Criteria: UI gates actions only by HAL links; uses service wrappers, MudBlazor v9 APIs, design tokens, CSS isolation/BEM, and accessible status/error copy.
- Dependencies: Task 7.1.
- Effort: L.
- Required Skills/Rules: `blazor-bff-patterns`, `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `accessibility`.
- Validation: Blazor unit/integration tests, browser manual QA.

## 7 Testing Strategy

- Architecture: run Clean Architecture, CQRS, naming, authorization parity, and Blazor architecture tests for touched layers.
- Application unit tests: resolver defaults, config validation, orchestration idempotency, safe payload hashing, local vs delegated decisions, preference/unsubscribe behavior.
- Persistence integration tests: new entities, indexes, query filters, tenant isolation, migration application, repository entity-returning behavior.
- API integration tests: routing status/controls, HAL links, authorization, redaction, invalid config responses.
- Infrastructure tests: SMTP delivery adapter unchanged, RabbitMQ pointer transport still pointer-only, Keycloak action email delegation audit, Coop delegation payload/status handling.
- Blazor tests/manual QA: admin UI through BFF surface, HAL-gated actions, accessible warnings, responsive layout.
- Local runtime proof when implementation begins: foreground `aspire run --apphost Explore.AppHost/Explore.AppHost.csproj --isolated`, then verify Mailpit/Keycloak/email-dispatch health and one happy path plus one denied/unsafe path.

## 8 Documentation, Configuration, And Operations Impact

Docs to update during implementation:

- `docs/ARCHITECTURE.md`: ownership-vs-delivery architecture and flow.
- `docs/DOMAIN.md`: notification intent/delegation entities and safe payload rules.
- `docs/API.md`: routing/admin endpoints and HAL rels.
- `docs/SECURITY-MODEL.md`: account-authority ownership, heavy-redaction email privacy, provider delegation boundaries.
- `docs/AUTHORIZATION.md`: routing/admin resource kinds/actions.
- `docs/CONFIGURATION.md`: category routing, Keycloak SMTP mode, ATProto fallback, Coop delegation flags.
- `docs/OPERATIONS.md`: health checks, metrics, Mailpit/Keycloak distinction, delegated provider failure handling.
- `docs/OUTBOX_PATTERN.md`: notification intent relationship to existing `EmailDispatchOutbox` and optional RabbitMQ pointer transport.
- `docs/BLAZOR.md` and design/accessibility docs if admin UI is added.
- `dev/active/email-responsibility-architecture/future-islamu-identity-project.md`: future Identity/PDS boundary decisions until a canonical Identity Project docs area exists.

Operations impact:

- Add safe metrics for ownership decisions and delegation outcomes without recipient/template/body/provider-secret labels.
- Add health/readiness only for enabled providers; RabbitMQ remains healthy/not-required when pointer mode is disabled.
- Preserve current email-dispatch redaction in logs/admin projections.

## 9 Security, Authorization, Privacy, And Abuse Considerations

- Identity lifecycle tokens/links are controlled by account authorities. ISLAMU Event and the future Identity Microservice must not mint password reset, email confirmation, migration confirmation, or account verification links for Keycloak/PDS accounts.
- Product emails may contain tenant/event context only when category allows it and user preferences permit it.
- Heavy redaction emails are generic and must not contain event title, slug, public URL, description, image, organizer identity, unsafe evidence, storage keys, provider payloads, or raw errors.
- External delegation must record local intent and safe payload hash before provider call.
- Tenant-specific senders require DNS verification and sender lock before use: DKIM, SPF, DMARC, bounce handling, and per-tenant from-address governance.
- Admin status APIs must redact recipient email, subject, body, provider ids, raw provider errors, unsubscribe tokens, and secrets.
- Provider callbacks remain machine-authenticated and idempotent.

## 10 Multi-Tenancy, Federation, Localization, Accessibility, Product Considerations

- Multi-tenancy: ownership/routing settings should follow the existing Instance -> Tenant -> Organization -> Group -> User cascade and locking model. Tenant filters must remain active.
- Federation: ATProto/PDS identity email ownership is future-facing. Current federation foundation does not imply ISLAMU Event or Identity can send ATProto credential emails. Future ISLAMU-operated PDS cells remain account authorities for hosted accounts.
- Localization: templates/legal wording belong to the lifecycle owner. ISLAMU product templates should be tenant/language aware; Keycloak/PDS templates belong to account-authority theming/config.
- Accessibility: admin UI must use accessible warnings, keyboard-operable controls, semantic status text, and non-color-only state indicators.
- Product: one notification purpose has one owner at a time. Avoid duplicate user emails from Keycloak/Coop/ISLAMU for the same purpose.

## 11 Observability And Operations

- Use OpenTelemetry, Prometheus, and Loki conventions already documented; no Sentry.
- Add counters/histograms for ownership decisions, local dispatch enqueue, external delegation enqueue, provider-delegation success/failure, and disabled/skipped decisions.
- Tag with bounded dimensions only: category, owner, provider kind, tenant id where safe, status/failure category. Do not tag with recipient email, subject, template body, provider message id, report evidence, or object keys.
- Preserve correlation ids through `NotificationIntent`, `EmailDispatchOutbox`, delivery attempts, and external delegation records.
- Dead-letter and unknown states remain durable for monitoring and replay/park decisions.

## 12 Migration And Compatibility Plan

- Keep existing `EmailDispatchOutbox` rows and processor behavior working.
- Add notification intent tables without rewriting old dispatch rows. New email-producing flows should write intents; existing registration email flow can be migrated by wrapping factory creation through the orchestrator.
- Do not edit applied migrations. Add small corrective migrations only.
- Backfill is optional: existing historical dispatch rows may remain without notification intent unless product/audit requires linking them.
- Config defaults should preserve current behavior for existing product emails: ISLAMU-owned, local SMTP delivery, DB outbox canonical, RabbitMQ disabled unless already enabled.

## 13 Risk Register

| Risk | Impact | Mitigation | Detection |
| --- | --- | --- | --- |
| Treating SMTP credentials as ownership | Keycloak/Coop could send wrong ISLAMU product emails | Separate ownership enums/config from delivery provider config | Config validation and docs review |
| Duplicate emails from multiple owners | User confusion and legal/compliance risk | One purpose has one owner; local intent idempotency/dedup keys | Unit tests and notification audit reports |
| Unsafe moderation payload leakage | Privacy/safety incident | Heavy-redaction safe payload contract and external delegation sanitizer | Tests that assert prohibited fields absent |
| Tenant sender spoofing | Deliverability/security failure | Require DNS verification and sender locks before tenant From activation | Admin validation, health checks |
| RabbitMQ treated as canonical | Lost/replayed email state | Keep PostgreSQL outbox canonical; RabbitMQ pointer-only | Processor tests and docs |
| ATProto current-state overclaim | Wrong implementation assumptions | Mark ATProto identity email rules future-facing until OIDC implemented | Plan/context warnings and future re-classification |
| One-PDS-per-user assumption | Expensive, fragile Identity architecture | Default to multi-account PDS cells/shards/clusters; reserve dedicated cells for explicit cases | Identity docs review and future capacity tests |
| Identity Microservice overreach | Wrong token/email authority and security boundary | Identity orchestrates provisioning/mapping/audit only; PDS/Keycloak mint credentials and send credential emails | Contract tests and delegation audit review |
| Admin UI role-gating drift | Unauthorized or hidden operations | HAL-only affordance gating | Authorization parity and Blazor tests |

## 14 Success Metrics And Definition Of Done

Done when:

- Every notification category has a documented default owner and enforced resolver behavior.
- ISLAMU product/event/registration/trust-safety emails create local notification intent/audit and use existing durable dispatch.
- Keycloak/PDS identity emails remain account-authority-owned; ISLAMU-initiated account lifecycle actions are locally audited as delegations.
- Future ISLAMU-operated PDS hosting defaults to multi-account cells/shards/clusters, with Identity orchestrating provisioning/mapping/audit but never minting PDS credential tokens.
- External user-facing provider email is impossible unless explicitly delegated and locally audited with safe payloads.
- Heavy-redaction email tests prove prohibited fields are absent.
- Admin APIs/UI expose safe routing status and HAL-gated controls.
- Docs/config/operations/security references are updated.
- Build, architecture tests, touched unit/integration tests, and manual surface QA pass or pre-existing failures are named.

## 15 Implementation Agent Contract — KEEP DEV DOCS CURRENT

Every implementation agent must update these files before handoff, context compaction, pause, or final claim:

- `dev/active/email-responsibility-architecture/email-responsibility-architecture-plan.md`
- `dev/active/email-responsibility-architecture/email-responsibility-architecture-context.md`
- `dev/active/email-responsibility-architecture/email-responsibility-architecture-tasks.md`

Required update content:

- What changed, with verified paths/classes.
- Which phase/task is complete/in progress/deferred.
- What tests/build/manual QA ran and key results.
- Any changed risks, blockers, or scope decisions.
- Any docs/config/operations files updated.

## 16 Progress Reporting Contract

Final implementation progress reports must be medium-sized developer teaching summaries. Include:

- Architecture/design patterns used, especially ownership-vs-delivery, Clean Architecture, CQRS/MediatR, transactional outbox, HAL affordance gating, and tenant isolation.
- Concrete libraries/infrastructure/protocols touched: EF Core/PostgreSQL, TickerQ, RabbitMQ pointer transport, SMTP/Mailpit, Keycloak admin/OIDC, ATProto/PDS account APIs, Coop callbacks, OpenTelemetry/Prometheus/Loki.
- Important files/classes/interfaces/handlers/components changed.
- Data/control flow from command/event to notification intent to outbox/delegation to delivery/audit.
- Conventions/best practices followed: manual validators, entity-returning repositories, no direct side effects, safe redaction, idempotency, retry/dead-letter.
- What was verified, what remains, next recommended slice, and docs updated.

## 17 Potential Risks & Unknowns

The biggest risk is conceptual drift: treating Keycloak, PDS, Coop, SMTP, RabbitMQ, or Mailpit as interchangeable email providers instead of separating lifecycle/account-authority ownership from delivery transport. The current codebase has strong Basic Dispatch Mode primitives, but no verified central ownership resolver or notification intent/delegation audit model yet. ATProto identity email behavior and ISLAMU-operated PDS hosting are future-facing because current docs describe federation foundation, not implemented ATProto login or an Identity Project. Coop user-facing delegated email should remain off by default until provider capabilities and safe payload contracts are verified.
