<!-- ABOUTME: CTO-revised implementation plan for transactional event, registration, reporting, moderation, and reminder email. -->
<!-- ABOUTME: Defines explicit channel delivery, atomic persistence, immutable fanout, consent, supersession, and operations requirements. -->

# Email Lifecycle Delivery Implementation Plan

> **Status:** Draft — revised after Senior CTO review; do not start runtime implementation until Phase 0 is approved  
> **Last Updated:** 2026-07-17 Europe/Brussels  
> **Implementation progress:** 0/43 tasks complete  
> **Current task:** 0.1 — approve the logical intent, channel-delivery, and transport-work model  
> **Scope rule:** This planning update changes only the three workstream documents.

## 1. Outcome

Complete ISLAMU-owned product email by extending the existing PostgreSQL `EmailDispatchOutbox` -> `EmailDispatchDrainService` -> MailKit SMTP path. Mailpit remains the local SMTP sink. PostgreSQL remains the canonical delivery ledger; RabbitMQ and TickerQ remain pointer/wake-up mechanisms.

The completed workstream will:

- preserve and migrate the live registration-confirmation and tenant-administrator invitation flows;
- notify a user after a real registration approval, rejection, waitlist promotion, cancellation, or revocation transition;
- notify only affected attendees after event/session cancellation or a published time, timezone, location, or room change;
- acknowledge reports and send safe case outcomes under distinct, withdrawable reporter consent purposes;
- converge local API and enabled Coop decisions on the existing post-enforcement executor while keeping Osprey signal-only;
- send generic, linkless availability email after irreversible heavy moderation and leave light-moderation email deferred;
- activate reminders only after cancellation, supersession, stale-pointer, and DST behavior is proven;
- provide retention/redaction, fair multi-tenant processing, metrics, alerts, and operator controls;
- prevent SMTP dependencies in controllers, MediatR handlers, domain entities, and retryable transaction delegates.

## 2. CTO Review Disposition

Retain these foundations:

1. ISLAMU Event owns event, registration, reporting, and moderation email; Keycloak/PDS own credential-token email.
2. `EmailDispatchOutbox` is the sole SMTP work ledger.
3. High fanout persists one occurrence pointer in the mutation transaction and materializes recipient work later.
4. Reporter outcomes are created only after successful enforcement in `ExecuteReportDecisionCommandHandler`.
5. Coop converges on that executor; Osprey remains signal-only.
6. Verified persisted addresses, session-scoped recipients, disclosure checks, and generic heavy-moderation copy remain mandatory.
7. Reminders remain disabled until their safety prerequisites pass.

Replace these rejected assumptions:

- add an EF migration; do not reuse `NotificationIntent.Id` as an unenforced email primary-key convention;
- model logical intent, channel delivery, and SMTP execution separately;
- create all recipient delivery rows atomically; missing-half recovery is exceptional only;
- split reporter consent and provide withdrawal;
- persist immutable occurrence snapshots and a deterministic audience cutoff;
- add coalescing and supersession;
- move registration transaction ownership to the Application/UoW boundary;
- repair Coop routing as a separate Phase 0 prerequisite;
- keep `Reporting:CaseSlaHours` as the single response-time source;
- make retention, fairness, observability, and operator controls release requirements.

## 3. Contribution Contract

The intent catalog has no plan-only `fallback-dev-docs-plan` entry. This planning change is therefore constrained to `dev/active/email-responsibility-architecture/**`; each runtime task must classify itself under every applicable registered intent.

| Contract item | Repository-grounded answer |
|---|---|
| Runtime intents | `add-ef-migration`, `add-cqrs-handler`, `update-repository-query`, `add-write-endpoint`, `add-hal-link`, `openapi-contract-change`, and `blazor-component-affordance`, selected per task. Breaking consent-contract changes are authorized by the CTO feedback because the product is pre-1.0. |
| Rules | `application-layer.md`, `domain.md`, `efcore-persistence.md`, `efcore-migrations.md`, `api-controllers.md`, `api-hateoas.md`, `blazor-client.md`, `blazor-server.md`, and `tests.md`. |
| Must-read docs | `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/API.md`, `docs/BLAZOR.md`, `docs/EMAIL_NOTIFICATIONS.md`, `docs/NOTIFICATIONS.md`, `docs/OUTBOX_PATTERN.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/MULTI_TENANCY.md`, `docs/COOP_INTEGRATION.md`, `docs/OSPREY_INTEGRATION.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, and `docs/ACCESSIBILITY.md`. All paths were verified in the current repository on 2026-07-17. |
| Runtime paths | Domain, Application, Persistence including new migrations, Infrastructure, API controllers/HAL, Blazor Client, generated OpenAPI client, affected tests, `schemas/islamu-event.md`, and the canonical docs named above. |
| Forbidden | Direct SMTP calls in handlers/controllers; repository-owned commits or nested UoW transactions; tenant-filter bypass without exact tenant and aggregate predicates; unverified/stale-address delivery; consent overloading; mutable-state reconstruction of past changes; exact private location without disclosure authorization; Osprey signal-as-decision; provider and local duplicate product email; editing an applied migration. |

The phase gates intentionally run all directly affected test projects, not one narrow filtered project. This is the explicit CTO-required verification exception for this cross-layer workstream. No release gate uses broad OR filters or `--minimum-expected-tests 1`.

## 4. Verified Current State

| Area | Evidence | Finding |
|---|---|---|
| SMTP/local development | `SmtpEmailService`, Aspire `AppHost`, Mailpit | MailKit SMTP and Mailpit already work; no new sender is needed. |
| Durable email | `EmailDispatchOutbox`, attempts/receipts, drain/recovery, TickerQ, optional RabbitMQ | PostgreSQL already owns delivery state, retry, unknown, dead-letter, pause, park, and replay. |
| Notification model | `NotificationIntent`, `NotificationDelivery`, `DefaultNotificationOrchestrator` | Intent and delivery rows exist, but delivery has no channel/policy identity and email has no direct intent FK. |
| Registration | `CreateEventRegistrationCommandHandler`, `EventRegistrationIntentRepository`, `EventRegistrationRepository` | Receipt is live, but registration repositories currently own serializable transactions and accept outbox entities. |
| Event/session changes | cancellation and update handlers | Business mutations exist; attendee email does not. Session owns schedule/location truth. |
| Fanout | `NotificationFanoutRun`, moderation/published fanout services | Resumable cursor patterns exist, but no immutable occurrence, lease, audience cutoff, session cohort, or supersession. |
| Reporting | `EventReport`, submission DTO/dialog, My Reports API/page | One checkbox currently covers follow-up contact only; receipt/outcome email and withdrawal do not exist. |
| Decision providers | local decision handlers, Coop inbox, Osprey callback | Local executor is correct. Coop callbacks are retained but ignored because no production handler routes them. Osprey only records signals. |
| Retention | `docs/OPERATIONS.md` data lifecycle matrix | Target is 180 days for resolved email evidence; unresolved dead-letter/unknown/parked rows stay until operator resolution. Automated parent-aware redaction is missing. |

Baseline Release build on 2026-07-17 passed: 25 projects, 0 warnings, 0 errors.

## 5. Target Data and Control Model

```text
NotificationIntent
  one business occurrence + one recipient
      |
      +-- NotificationDelivery: InApp
      |      channel policy + outcome -> Notification
      |
      +-- NotificationDelivery: Email
             channel policy + outcome -> EmailDispatchOutbox
                                              |
                                              v
                                      MailKit SMTP transport
```

`NotificationIntent` is not an email row. `NotificationDelivery` records whether each configured channel was required, selected, skipped, queued, sent, or failed. `EmailDispatchOutbox` stores durable SMTP execution state only.

### 5.1 Schema relationship

Add a reversible migration and explicit model relationships:

- `NotificationDelivery.ChannelId` -> existing `NotificationPreferenceChannel` lookup;
- `NotificationDelivery.DeliveryPolicyId` -> new stable `NotificationDeliveryPolicy` lookup;
- `NotificationDelivery.IsRequired` snapshots the channel decision made by the policy;
- optional `NotificationDelivery.NotificationId` for in-app delivery;
- existing `NotificationDelivery.EmailDispatchOutboxId` remains the email-delivery link;
- `EmailDispatchOutbox.NotificationIntentId` -> `NotificationIntent.Id`;
- rename/backfill `EmailDispatchOutbox.UserId` to `RecipientUserId` and retain a user FK;
- composite `(TenantId, NotificationIntentId, RecipientUserId)` FK/alternate-key mapping guarantees the email belongs to the same tenant, intent, and recipient user;
- composite delivery-to-email mapping includes tenant and intent IDs so a delivery cannot link another intent's outbox row;
- unique `(TenantId, NotificationIntentId, ChannelId)` on delivery;
- unique `(TenantId, NotificationIntentId, EmailDispatchKind)` on non-deleted email rows;
- unique non-null `(TenantId, EmailDispatchOutboxId)` and `(TenantId, NotificationId)` delivery links.

Existing email rows are backfilled rather than abandoned. A migration may use the existing outbox ID as the explicit legacy intent ID while creating a real FK, delivery row, policy, and deterministic `legacy-email-dispatch:{id}` deduplication key. New rows retain independent IDs. Lookup enum IDs, migration-local inserts, runtime seeder repair, model snapshot, and `schemas/islamu-event.md` must agree.

Normalize delivery outcomes to channel-neutral meanings: `Pending`, `Queued`, `Delivered`, `Skipped`, `Failed`, `DeadLettered`, `Unknown`, `Parked`, and `Superseded`. The authorized pre-1.0 migration deterministically maps existing email-specific statuses and preserves stable numeric IDs/backfill parity.

### 5.2 Delivery-policy codes

Seed stable codes instead of treating all trust-and-safety messages alike:

- `RegistrationStatusOptional`
- `CriticalEventUpdateOptional`
- `ReportCaseUpdate`
- `ReportFollowUpContact`
- `ModerationAvailabilityRequired`
- `ModerationContextOptional`
- `ReminderOptional`
- `TenantAdministrationRequired`

The policy resolver defines required/optional channels, preference category, consent requirement, link safety, and dispatch-time revalidation. `EmailDispatchKind` remains template/transport classification; it no longer carries all authority through switch statements.

### 5.3 Atomic recipient materialization

For one recipient, `NotificationIntent`, all configured `NotificationDelivery` rows, any in-app `Notification`, and `EmailDispatchOutbox` must be inserted in one PostgreSQL transaction.

Normal flow:

1. Generate occurrence and intent IDs before entering a retryable transaction delegate.
2. Resolve the delivery policy and channel decisions.
3. Insert intent, channel rows, in-app notification, and email row together.
4. Commit before any SMTP, broker, scheduler, or HTTP work.

Conflict flow:

1. Let the exact deduplication constraint abort and roll back the transaction.
2. Clear failed tracking and start a fresh transaction; never continue in the aborted PostgreSQL transaction.
3. Load the winning intent and deliveries.
4. Repair only a proven legacy/concurrency missing row.
5. Re-throw every unrelated database error.

Fanout materialization should use a repository-level PostgreSQL `ON CONFLICT DO NOTHING` batch primitive where it removes N+1 existence checks without weakening tenant or dedup constraints.

### 5.4 Dispatch-time eligibility

One policy evaluator runs after claim but before SMTP. It must:

- load `RecipientUserId` in the persisted tenant;
- skip missing, deleted, or unverified users;
- refresh a changed address from the current verified `User.Email` so the stale snapshot is never used;
- re-evaluate optional notification preference/unsubscribe state;
- re-evaluate report consent according to the delivery policy;
- skip a superseded/cancelled occurrence or reminder;
- apply tenant/global pause and provider rate limits;
- record a terminal, typed skip reason and update the linked delivery outcome.

## 6. Channel Policy Matrix

“Plus” means one logical intent with separate channel deliveries. A failed or unavailable email does not create a second fallback intent.

| Trigger | In-app policy | Email policy | Dispatch policy / seam |
|---|---|---|---|
| Registration received | Required | Optional; verified current email and `registration-status` preference | Existing creation transaction, migrated to atomic primitive |
| Approval/rejection/waitlist promotion | Required | Optional; `registration-status` | Final parent transition only |
| Registration self-cancel/revoke | Required | Optional; `registration-status`; copy requires actor provenance | Application-owned transition transaction |
| Whole-event/session cancellation | Required | Optional; `event-updates`; cancellation is immediate | Mutation transaction -> occurrence pointer |
| Published time/timezone/location/room update | Required | Optional; `event-updates`; five-minute default coalescing window | Mutation transaction -> immutable occurrence |
| Report receipt/status/final outcome | Required | Email only while `ReportCaseUpdatesConsent` is true | Submission or successful execute transaction |
| Needs more information | Required | Email only while `ReportFollowUpContactConsent` is true | Successful execute transaction; not a final outcome |
| Escalated report | Required status update | No final email | Remains under review |
| Osprey signal | None caused by the signal itself | None | Later local decision may use execute seam |
| Coop decision | Same as local after prerequisite | Same as local after prerequisite | Durable inbox -> effect pointer -> Coop command -> execute seam |
| Heavy moderation unavailable | Required | Required operational email when a current verified address exists; user preference cannot disable it | Irreversible heavy-moderation occurrence |
| Light moderation context | Existing in-app behavior | Deferred | `ModerationContextOptional` is reserved; no email trigger in this plan |
| Reminder | Optional according to product preference | Optional; `event-updates` | Approved registrations only after Phase 7 safety gates |
| Tenant administrator invitation | Existing behavior | Required | Existing managed-provisioning transaction |

Reporter receipt copy derives from the sole SLA setting, `Reporting:CaseSlaHours` (default 48), for example: “We normally review reports within 48 hours.” No min/max business-day configuration is added.

## 7. Reporter Consent and Withdrawal

Replace `ReporterContactConsent` across the domain, API, generated client, and UI with:

- `ReportCaseUpdatesConsent`: receipt, status updates, and final outcome;
- `ReportFollowUpContactConsent`: requests for clarification or additional evidence.

The report dialog presents two independently labelled unchecked choices. Anonymous reports cannot opt into email without a separately approved reporter-PII design.

Add an authorized reporter-owned write command/endpoint to change both choices. The My Reports detail and collection HAL policies expose `update-communication-consent` only for the owning reporter while the report is retained. Blazor renders the control only when that HAL link exists. Withdrawal takes effect at dispatch time: queued unsent work becomes `Skipped` with `consent_withdrawn`; sent email remains immutable audit history. Re-consent does not automatically replay skipped mail.

## 8. Deterministic Fanout and Immutable Occurrences

Add `NotificationFanoutOccurrence` as immutable business input and keep `NotificationFanoutRun` as mutable execution state.

`NotificationIntent.FanoutOccurrenceId` is a nullable FK for fanout-created intents. A filtered unique index on `(TenantId, FanoutOccurrenceId, UserId)` enforces one logical intent per occurrence/recipient independently of the human-readable deduplication key.

Every occurrence persists:

```text
Id / OccurrenceId
TenantId
EventId
SessionId?
OccurredAt
AudienceCutoffAt
AggregateVersion
ChangeSet
SafeBeforeSnapshot
SafeAfterSnapshot
TemplateKey
TemplateVersion
DeliveryPolicyId
Priority
NotBefore
SupersededByOccurrenceId?
SuppressionReason?
```

The occurrence ID is generated before the retry delegate. The mutation and one general-outbox pointer commit together; the pointer contains only tenant and occurrence IDs. Workers never reconstruct an older change from current mutable event/session state. Exact location can be selected from the immutable snapshot only after current recipient disclosure authorization succeeds.

Audience rules:

- registration intent existed at or before `AudienceCutoffAt` and is still eligible when processed;
- event cancellation and important event updates include current `Pending`, `Approved`, and `Waitlisted` parents; exclude `Rejected`, `Cancelled`, `Revoked`, and soft-deleted rows;
- session occurrences also require a non-deleted target-session child in `Pending`, `Approved`, or `Waitlisted`; partially cancelled target children are excluded;
- reminder audience requires both parent and target child to be `Approved`;
- whole-event, whole-day, and explicit-session coverage is derived from the persisted child rows;
- registrations created after the cutoff do not receive an old change because they register against current data;
- group to one audience member per user and page with `(FirstEligibleRegistrationCreatedAt, UserId)` lexicographic cursor;
- a unique occurrence/user intent constraint remains the final duplicate guard.

The run stores occurrence FK, lease owner/expiry, cursor tuple, counts, retry state, and concurrency stamp. A worker crash may replay the last page; atomic recipient dedup makes that safe.

## 9. Coalescing and Supersession

Precedence is fixed:

```text
Heavy moderation unavailable
    > Whole-event cancellation
    > Session cancellation
    > Important update
    > Reminder
```

Rules:

- heavy moderation and cancellation bypass coalescing and become runnable immediately;
- a whole-event cancellation suppresses pending session cancellation/update/reminder work;
- heavy moderation suppresses all lower-priority work and all event links;
- session cancellation suppresses pending update/reminder work for that session;
- a newer unsent schedule/location occurrence supersedes the older occurrence for the same scope;
- important updates use a configurable five-minute default coalescing window; the replacement occurrence keeps the earliest safe “before” and latest safe “after” snapshot;
- only unmaterialized or unsent work can be suppressed; sent history is never rewritten;
- dispatch rechecks supersession so already-materialized unsent email cannot escape after a later cancellation.

## 10. Retention, Fairness, and Operations

### Retention and privacy

- Sent and skipped rows become eligible for recipient/subject/body/reply-to redaction after 180 days.
- Dead-lettered, unknown, and parked rows retain replay material until explicit operator resolution; after resolution they use the same 180-day window.
- Attempts and receipts follow the parent lifecycle and cannot disappear earlier or retain PII longer.
- Redaction retains non-PII audit identifiers, tenant, kind, policy, state, counts, and timestamps; `ContentRedactedAt` prevents replay.
- Tenant deletion terminally skips pending work, redacts email/body content, removes replay authority, and retains only governance-required audit metadata.
- Cleanup is bounded, idempotent, tenant-safe, observable, and supports dry-run evidence.

### Multi-tenant fairness and backpressure

- bounded batches for fanout and SMTP;
- at most one active fanout lease per tenant by default;
- fair tenant selection (one runnable item per tenant per round before a tenant receives another slot);
- configurable global concurrency and per-tenant concurrency;
- configurable instance/tenant SMTP token-bucket rate limits;
- backlog thresholds that stop accepting/scheduling optional reminder work before required lifecycle work;
- required cancellation/moderation work remains higher priority than optional mail.

### Metrics and alerts

Record no address, subject, body, report evidence, event title, or user ID. Provide:

- oldest pending email and fanout age;
- send success/failure rate and retryable/permanent split;
- dead-letter/unknown/parked counts;
- skipped-by-preference, consent, unverified-address, deletion, and supersession counts;
- fanout processed/remaining and lease contention;
- tenant backlog using tenant identifier only, with cardinality bounded by the existing metrics policy;
- alerts for age SLO breach, sustained failures, dead letters, and configured backlog limits.

### Operator controls

Document and test: Mailpit/test SMTP, tenant sender pause/suppression, global drain/fanout pause, rate-limit changes, dead-letter inspection/replay, redaction/cleanup dry run, compromised-tenant response, and tenant-vs-instance SMTP diagnosis.

## 11. Implementation Phases

### Phase 0 — Architecture and policy baseline (Tasks 0.1–0.7)

Approve the channel matrix, consent/withdrawal contract, occurrence/cohort/supersession semantics, retention/fairness/operations policy, and single SLA. Land the Coop callback repair as an independently reviewable prerequisite before reporter-decision convergence.

Coop prerequisite acceptance:

- unique provider delivery/event ID;
- one effect-outbox row per inbox record/effect kind;
- pointer FK/retention prevents cleanup from orphaning a retained callback;
- stale or out-of-order decisions cannot reopen a completed case;
- poison callbacks are quarantined/dead-lettered;
- duplicate callback, duplicate pointer, and dispatcher replay are idempotent;
- pointer completion occurs only after command success;
- payload retention and cleanup ordering are documented and tested.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build
```

### Phase 1 — Atomic recipient-delivery primitive (Tasks 1.1–1.6)

Add the explicit schema/backfill, atomic channel materializer, clean unique-conflict recovery, dispatch-time eligibility, retention/redaction, metrics/operator controls, and architecture boundary test before adding business triggers.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build
```

### Phase 2 — Registration transitions (Tasks 2.1–2.5)

Move serializable orchestration to the Application/UoW boundary, return an explicit transition result, and migrate registration receipt/status/cancellation/revocation to the atomic channel primitive.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build
```

### Phase 3 — Generic fanout engine (Tasks 3.1–3.6)

Prove immutable occurrences, deterministic audience paging, lease/checkpoint recovery, atomic recipient materialization, supersession/coalescing, and tenant fairness before wiring event triggers.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build
```

### Phase 4 — Event and session triggers (Tasks 4.1–4.5)

Wire whole-event cancellation first, then session cancellation, material session updates, and timezone reprojection. Each mutation persists immutable before/after values and one occurrence pointer in its transaction; cache invalidation remains post-commit.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build
```

### Phase 5 — Reporting email and provider convergence (Tasks 5.1–5.9)

Implement split consent, submission UI, withdrawal HAL affordance, receipt, final outcome, follow-up contact, and source-convergence evidence. Coop is enabled only if the Phase 0 prerequisite is live; otherwise local API remains functional and Coop email stays disabled. Osprey remains signal-only.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build
dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build
```

### Phase 6 — Heavy moderation attendee email (Tasks 6.1–6.2)

Add required in-app plus operational email for irreversible heavy moderation. Copy is generic/linkless and the occurrence suppresses lower-priority links, updates, and reminders. Light-moderation email remains deferred.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build
```

### Phase 7 — Safe reminder activation (Tasks 7.1–7.3)

Schedule one reminder for an approved registration’s earliest covered published session, then prove supersession/cancellation, future-row stale-pointer guards, and DST/timezone reprojection before enabling the production caller.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build
```

## 12. Mandatory Fault, Concurrency, and Privacy Evidence

Each task adds focused failing-first tests and records the exact new test class/count. Phase evidence uses the complete unfiltered project suites above.

Required scenarios:

- two workers create the same occurrence/recipient;
- exact unique conflict rolls back, then a fresh transaction loads the winner;
- rollback after intent creation and after email creation leaves no partial committed set;
- worker crash after a partial batch and stale cursor replay;
- concurrent fanout lease attempts and lease expiry recovery;
- tenant mismatch and tenant-filter bypass negative tests;
- event cancellation supersedes update/reminder; session cancellation suppresses its lower work;
- consent withdrawn before dispatch;
- email changed, removed, or unverified before dispatch;
- whole-event/day/session audience coverage, partially cancelled children, waitlisted/pending parents, and duplicate child rows;
- callback replay, poison callbacks, and out-of-order Coop decisions;
- immutable 10:00 -> 11:00 occurrence followed by 12:00 update;
- DST gap/overlap, timezone reprojection, and stale TickerQ pointer for future/superseded reminders;
- negative location-disclosure and heavy-moderation redaction assertions;
- architecture test rejects controller/handler dependencies on `IEmailService`, SMTP implementations, or direct send abstractions.

## 13. Deferred/Separate Product Work

- Light-moderation attendee email is deferred because it can be noisy and reversible; only existing in-app behavior remains.
- Automatic waitlist scanning, arbitrary organizer announcements, follower new-event mail, organization/group invitation redesign, API-key/support-access alerts, and marketing/Listmonk campaigns require separate product decisions.
- Account verification, password reset, credential update, and recovery remain Keycloak/PDS responsibilities.
- Anonymous reporter email requires a separately approved PII/verification model.

## 14. Definition of Done

The workstream is complete only when all 43 tasks are checked, the Phase 0 Coop prerequisite is independently proven, every required channel policy and trigger is implemented, all phase gates and the canonical repository verification pass, Mailpit-backed delivery remains green, consent/preference/address/supersession checks are dispatch-time safe, retention/redaction and operator controls are usable, and `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/BLAZOR.md`, `docs/EMAIL_NOTIFICATIONS.md`, `docs/NOTIFICATIONS.md`, `docs/OUTBOX_PATTERN.md`, `docs/CONFIGURATION.md`, `docs/COOP_INTEGRATION.md`, `docs/OSPREY_INTEGRATION.md`, `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`, and `schemas/islamu-event.md` match runtime behavior.

Implementation agents must synchronize this plan, the context, and the task ledger before handoff, pause, compaction, or completion.
