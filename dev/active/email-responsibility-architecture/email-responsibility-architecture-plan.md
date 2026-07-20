<!-- ABOUTME: CTO-revised implementation plan for transactional event, registration, reporting, moderation, and reminder email. -->
<!-- ABOUTME: Defines explicit channel delivery, atomic persistence, immutable fanout, consent, supersession, and operations requirements. -->

# Email Lifecycle Delivery Implementation Plan

> **Status:** Production implementation complete — runtime proof deferred; main-checkout SMTP and unrelated work preserved
> **Last Updated:** 2026-07-20 Europe/Brussels
> **Implementation progress:** 49/51 tasks complete; phase verification remains tracked separately
> **Current task:** production implementation is complete; Tasks 5.9 and 7.3 remain deferred runtime-proof gates under the user's no-tests instruction
> **Execution location:** main repository checkout only; do not create linked worktrees or a `.worktrees` directory for this workstream
> **Repository baseline:** current `develop` HEAD `9fe1e26e`; email retention, the SMTP diagnostic boundary, and the earlier partial SMTP operations work are committed in `9bfaf1e0`. Later email/fanout changes and their migrations remain preserved in the main checkout alongside unrelated ATProto/auth/location-privacy work.

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
| Forbidden | Running `git worktree add`, creating linked worktrees or a `.worktrees` directory, or recording a worktree path for this workstream—even when a generic automation workflow recommends one; direct SMTP calls in handlers/controllers; repository-owned commits or nested UoW transactions; tenant-filter bypass without exact tenant and aggregate predicates; unverified/stale-address delivery; consent overloading; mutable-state reconstruction of past changes; exact private location without disclosure authorization; Osprey signal-as-decision; provider and local duplicate product email; editing an applied migration. |

Each phase has one Release build and one fastest relevant non-browser project test. Intent-mandated and directly affected project suites remain final merge-contract evidence and are not weakened to broad OR filters or `--minimum-expected-tests 1`.

## 4. Verified Current State

| Area | Evidence | Finding |
|---|---|---|
| SMTP/local development | `SmtpEmailService`, Aspire `AppHost`, Mailpit | MailKit SMTP and Mailpit already work; no new sender is needed. |
| Durable email | `EmailDispatchOutbox`, attempts/receipts, drain/recovery, TickerQ, optional RabbitMQ | PostgreSQL already owns delivery state, retry, unknown, dead-letter, pause, park, and replay. |
| Notification model | `NotificationIntent`, `NotificationDelivery`, `EmailDispatchOutbox`, `RecipientNotificationMaterializer` | Explicit tenant/intent/recipient relationships, channel/policy snapshots, atomic graph persistence, exact deduplication recovery, and provider-handoff settlement are implemented. |
| Registration | create/update/delete registration handlers, registration repositories, `RegistrationNotificationDeliveryService` | Application handlers own serializable UoW boundaries. Receipt, approval, rejection, waitlist promotion, self-cancellation, and organizer/system revocation materialize one in-app/email graph per real parent transition. |
| Event/session changes | cancellation and update handlers | Business mutations exist; attendee email does not. Session owns schedule/location truth. |
| Fanout | `NotificationFanoutOccurrence`, `NotificationFanoutRun`, registration audience repository | Immutable occurrences, audience cutoff, coverage timestamps, compound cursor queries, and fenced lease/checkpoint persistence are implemented. The pointer dispatcher, recipient worker, coalescing/supersession, and fair scheduler are not. |
| Reporting | `EventReport`, submission DTO/dialog, My Reports API/page | One checkbox currently covers follow-up contact only; receipt/outcome email and withdrawal do not exist. |
| Decision providers | local decision handlers, Coop inbox/effect pointer/worker/admin API, Osprey callback | Coop now uses signed provider identity, retained callback validation, fenced processing, command-success settlement, bounded retry/dead-letter, and audited HAL redrive. Osprey remains signal-only. Reporter outcome email is still absent. |
| Retention | `EmailDispatchOutbox`, retention cleanup service/processor/migration, operator docs | Commit `9bfaf1e0` adds bounded parent-aware redaction and explicit resolve-without-replay. Its Docker-backed PostgreSQL phase evidence remains incomplete. |
| SMTP boundary | `IEmailService`, `IEmailConnectionTester`, SMTP test query/handler, controller, health check, architecture guard | Product send capability is separate from diagnostic connection testing. The controller uses MediatR, health uses the narrow diagnostic contract, and `SmtpEmailService` remains the Infrastructure implementation. |
| SMTP scheduling | pending-row repository, drain service, processor/tenant state, settings, health/metrics | Batch and single-pointer work claim atomically with cross-replica Processing ceilings, then share persisted global/per-tenant SMTP admission. Rate deferral is evidence-free; provider handoff and every settlement are lease/attempt fenced; stale recovery distinguishes unfenced retry from fenced `Unknown`. Active-only readiness, closed-vocabulary telemetry, current backpressure state, and public-health redaction are implemented. |

Historical implementation evidence from 2026-07-17 records a Release build with 0 errors and full Domain (439), Application (2,449), Persistence (448), Infrastructure (832), and Architecture (240 passed, 1 declared skip) suites after the delivery migration landed. Task 1.6a later recorded 2/2 handler tests, 1/1 handler boundary test, 1/1 controller boundary test, 3/3 SMTP configuration tests, and an API Release build with zero errors. Task 1.6b recorded 28/28 drain tests and 22/22 settings-validator tests. Task 1.6c recorded 31/31 drain tests, 24/24 validator tests, zero-error affected builds, clean EF model parity, and independent confirmation after two state-machine fix loops. Its 42 PostgreSQL scenarios could not enter their bodies because an unrelated location migration fails shared fixture setup. Mailpit and the current Phase 1 gate therefore remain open.

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

### 5.1 Schema relationship and recipient authority

The committed `20260717131038_NormalizeRecipientNotificationDelivery` migration implements the explicit model relationships and deliberately destructive pre-1.0 ledger reset:

- `NotificationIntent` has alternate key `(TenantId, Id, RecipientUserId)` for every new user-addressed intent;
- `NotificationDelivery` has alternate key `(TenantId, Id, NotificationIntentId, ChannelId)` and unique `(TenantId, NotificationIntentId, ChannelId)`;
- `NotificationDelivery.ChannelId` -> existing `NotificationPreferenceChannel` lookup and `DeliveryPolicyId` -> new stable `NotificationDeliveryPolicy` lookup;
- `NotificationDelivery.IsRequired`, policy code/version, consent purpose/version, preference category/result, address source, disclosure level, template key/version, and link permission form an immutable materialization-time policy snapshot;
- optional `NotificationDelivery.NotificationId` for in-app delivery;
- existing `NotificationDelivery.EmailDispatchOutboxId` remains the email-delivery link;
- `EmailDispatchOutbox.NotificationIntentId` -> `NotificationIntent.Id` while the outbox retains its own ID;
- rename `EmailDispatchOutbox.UserId` to required `RecipientUserId`; composite `(TenantId, RecipientUserId)` references `TenantUser`, never a bare global user;
- composite `(TenantId, NotificationIntentId, RecipientUserId)` FK guarantees the email belongs to the same tenant, intent, and recipient;
- composite delivery-to-email `(TenantId, EmailDispatchOutboxId, NotificationIntentId, RecipientAddressSource)` guarantees the address-source snapshot equals its outbox authority;
- composite delivery-to-notification `(TenantId, NotificationId, NotificationIntentId)` prevents cross-intent links, while notification-to-intent `(TenantId, NotificationIntentId, UserId)` enforces recipient equality;
- unique `(TenantId, NotificationIntentId)` permits exactly one email execution row per logical intent;
- unique non-null `(TenantId, EmailDispatchOutboxId)` and `(TenantId, NotificationId)` delivery links.

`EmailDispatchOutbox.RecipientAddressSource` is a required discriminator:

- `TenantUserVerifiedEmail` requires the required `RecipientUserId`, the composite `TenantUser` FK, and null managed-provisioning authority;
- `ManagedTenantAdministratorInvitation` requires the same tenant membership plus non-null composite authority `(TenantId, ManagedTenantProvisioningOperationId)`; its snapshot address must equal the decoded persisted operation-request invitation address;

No third or arbitrary address source is allowed. PostgreSQL constraints enforce tenant, recipient, channel, source-snapshot, authority, and candidate-key integrity. External delegation uses an EF-modeled `(TenantId, NotificationIntentId)` FK. Two relationships are migration-authored SQL because their nullable principals cannot be represented faithfully as EF alternate keys: email-to-managed-operation `(TenantId, ManagedTenantProvisioningOperationId)` while operation tenant is null before success, and delivery-to-notification same-intent while preserved inbox notification intent links are nullable.

Because this is pre-1.0 software, the migration does not synthesize authority or preserve incompatible delivery work. In one PostgreSQL transaction it deletes rows only from `notification_deliveries`, `email_dispatch_receipts`, `email_dispatch_attempts`, `email_dispatch_outbox`, `notification_external_delegations`, and `notification_intents`; then it installs the required schema. Existing `notifications` survive with nullable `NotificationIntentId`, and event, registration, report, audit, settings, tenant, and user data remain untouched. Lookup enum IDs, migration-local upserts, bounded runtime seeder repair, model snapshot, and `schemas/islamu-event.md` must agree.

The isolated migration rehearsal is a populated Up -> Down -> Up cycle against its own Testcontainer. It proves exact ledger-reset scope, preserved notification and unrelated business/audit/settings canaries, realistic pre-target channel/status lookups, required recipient and tenant/source constraints, clean Down lookup/schema restoration, and deterministic second Up. Down resets current delivery ledgers and restores only the old empty schema; it cannot reconstruct rows intentionally deleted by either direction. Reporter-consent migration separately receives its own Up -> Down -> Up proof because it has different data-preservation semantics.

Normalize delivery outcomes to channel-neutral meanings: `Pending`, `Queued`, `Delivered`, `Skipped`, `Failed`, `DeadLettered`, `Unknown`, `Parked`, and `Superseded`. Stable numeric IDs remain authoritative; Down restores the prior codes for IDs 2 and 3 and removes new IDs 7–9, while second Up restores the canonical set.

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

For one recipient, `RecipientNotificationMaterializer` inserts `NotificationIntent`, all configured `NotificationDelivery` rows, any in-app `Notification`, and `EmailDispatchOutbox` in one PostgreSQL transaction.

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

The current primitive intentionally handles one recipient per transaction. Task 3.4c may add a repository-level PostgreSQL `ON CONFLICT DO NOTHING ... RETURNING` batch path only if the bounded worker demonstrates an N+1 bottleneck; deduplication constraints remain the correctness authority either way.

### 5.4 Dispatch-time eligibility

`EmailDispatchEligibilityEvaluator` runs after claim and before SMTP. It revalidates tenant activity, tenant membership, current verified address, managed-invitation authority, supported policy, preference, consent, and superseded delivery state, then establishes the provider-handoff fence atomically. Task 1.4 also maps `RegistrationCancelled` and `RegistrationRevoked` consistently in both legacy preference and unsubscribe-footer category switches. The remaining consent rule is intentionally deferred:

- keep `ReportCaseUpdate` fail-closed until Task 5.1 adds distinct persisted case-update consent, then revalidate that consent before handoff;
- let Task 3.5b add the occurrence/version cancellation fence and Task 1.6b add global/per-tenant rate controls;
- retain stable non-PII skip reasons and atomically align outbox, attempt, receipt, and linked delivery outcomes.

The immutable policy snapshot is an authorization ceiling. Dispatch may narrow it using current membership, verification, consent, preference, disclosure, tenant pause, deletion, or supersession state; it must never add a channel, purpose, recipient, exact location, or link that was not authorized in the snapshot.

### 5.5 Provider handoff and settlement

The conditional `Pending/Claimed -> ProviderHandoff` transition is the cancellation, consent, preference, and supersession linearization boundary. Eligibility and the occurrence/version fence are rechecked in that same transition. Work suppressed before the fence never reaches SMTP. Work beyond the fence cannot honestly be recalled.

- explicit SMTP rejection before acceptance follows typed retryable/permanent failure policy;
- cancellation, I/O, protocol, process, or persistence uncertainty after handoff settles as `Unknown`;
- `Unknown` is terminal for automatic delivery and requires bounded operator reconciliation before replay;
- an SMTP acceptance followed by attempt, receipt, outbox, or delivery persistence failure must not cause blind resend;
- attempts, receipts, outbox, and `NotificationDelivery` settle through one idempotent reconciliation state machine.

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

The resolved value is validated as one bounded hour value and snapshotted with the receipt template at report submission. A later configuration change cannot rewrite already queued copy. The default receipt therefore retains its 48-hour promise snapshot.

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
- session occurrences also require a non-deleted target-session child in `Pending`, `Approved`, or `Waitlisted` whose immutable `CoverageEstablishedAt <= AudienceCutoffAt`; partially cancelled target children are excluded;
- reminder audience requires both parent and target child to be `Approved`;
- whole-event, whole-day, and explicit-session coverage is derived from the persisted child rows;
- registrations created after the cutoff do not receive an old change because they register against current data;
- group to one audience member per user and page with `(FirstEligibleRegistrationCreatedAt, UserId)` lexicographic cursor;
- a unique occurrence/user intent constraint remains the final duplicate guard.

`CoverageEstablishedAt` is set when the user first gains that session coverage. A same-scope child replacement performed atomically copies the original value; a newly added session gets the current timestamp. Parent creation time alone is never enough, so moving or adding a child after an occurrence cannot admit that user to the old cohort.

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

- Sent and ordinary skipped rows become eligible for recipient/subject/body/reply-to redaction after 180 days.
- Dead-lettered, unknown, and parked rows retain replay material until successful replay or explicit operator resolve-without-replay. Resolve-without-replay immediately redacts content and records a stable typed resolution outcome; no second retention timestamp is introduced.
- Attempts and receipts follow the parent lifecycle and cannot disappear earlier or retain PII longer.
- Redaction retains non-PII audit identifiers, tenant, kind, policy, state, counts, and timestamps; `ContentRedactedAt` prevents replay.
- Tenant deletion terminally skips pending work, redacts email/body content, removes replay authority, and retains only governance-required audit metadata.
- Cleanup is bounded, idempotent, tenant-safe, observable, and supports dry-run evidence.

### Multi-tenant fairness and backpressure

- bounded batches for fanout and SMTP;
- at most one active fanout lease per tenant by default;
- fair tenant selection (one runnable item per tenant per round before a tenant receives another slot);
- atomically claim SMTP work in PostgreSQL under deterministic tenant rounds, enforcing global and per-tenant active-processing ceilings across replicas before rows leave the repository;
- use process-local semaphores only as an optimization; batch, TickerQ, hosted-service, and RabbitMQ single-row drainage share the same authoritative admission path;
- persist global and per-tenant SMTP token-bucket state so configured rate limits are cross-instance rather than process-local;
- reserve rate capacity before incrementing SMTP attempt count or writing the provider-handoff fence; rate deferral consumes neither attempt budget nor provider evidence;
- compute optional-work pressure from active, eligible core backlog while excluding paused tenants and optional reminders themselves; store or centrally coordinate hysteresis so restart/replica behavior cannot disagree;
- backlog thresholds stop accepting/scheduling only optional reminder work before required lifecycle work;
- required cancellation/moderation work remains higher priority than optional mail.
- stale processing without a `provider_handoff_started` fence returns safely to retryable work; only fenced uncertainty becomes `Unknown`.

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

### Phase 0A — Approved architecture and policy baseline (Tasks 0.1–0.4)

The channel matrix, consent/withdrawal contract, occurrence/cohort/supersession semantics, retention/fairness/operations policy, recipient authority, handoff settlement, reminder rules, and single SLA are approved. Tasks 0.1-0.4 are complete. This documentation-only phase uses static path, provenance, JSON, and diff evidence; it does not rerun product tests.

### Phase 0B — Coop callback correctness prerequisite (Tasks 0.5–0.7)

The specialized `IncomingWebhookEffectOutbox` repair is implemented. Phase 0B blocks only Task 5.9 provider convergence until its final full API verification is recorded.

Task 0.5 is complete and independently confirmed. The implemented slice requires a signed nonblank bounded provider decision ID, atomically persists a SHA-only `IncomingWebhookEffectOutbox`, settles intake through `PointerPersisted` without creating an applied-effect receipt, protects retained callback bytes, and enforces both deduplication identities plus the tenant-safe inbox FK in PostgreSQL. Migration `20260717104030_AddIncomingWebhookEffectOutbox` passed a fresh Up -> Down -> Up rehearsal and EF reported no pending model changes; committed coverage lives in the Coop effect persistence and incoming-webhook API tests.

Tasks 0.6 and 0.7 are implementation-complete. Runtime loads and revalidates retained callbacks under a fenced renewable lease, executes `ProcessCoopDecisionCallbackCommand`, atomically settles the applied-effect receipt and pointer after success, retries transient failures, dead-letters poison input, preserves cleanup/replay ordering, emits bounded metrics/health, and exposes authenticated tenant-scoped HAL inspection/redrive. The recipient-model migration is now present; the remaining gate is a fresh full API rerun, not more Coop implementation.

Coop prerequisite acceptance:

- unique provider delivery/event ID;
- one effect-outbox row per inbox record/effect kind;
- pointer FK/retention prevents cleanup from orphaning a retained callback;
- stale or out-of-order decisions cannot reopen a completed case;
- poison callbacks are quarantined/dead-lettered;
- duplicate callback, duplicate pointer, and dispatcher replay are idempotent;
- pointer completion occurs only after command success;
- payload retention and cleanup ordering are documented and tested.

Phase-end verification (run once after Tasks 0.5–0.7):

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
```

### Phase 1 — Atomic recipient-delivery primitive (Tasks 1.1–1.6e)

Tasks 1.1–1.6e are implementation-complete. Commit `9bfaf1e0` contains Task 1.5 and the Task 1.6a production boundary; Tasks 1.6b–1.6e and the strict controller guard remain preserved main-checkout changes. Docker-backed PostgreSQL, explicit Mailpit, and the Phase 1 test gate remain open.

Task 1.5 uses a validated API-hosted timer over a scoped Infrastructure cleanup service and one specialized PostgreSQL repository transaction. `ContentRedactedAt` is the single permanent claim/publish/replay fence. Dead-lettered, parked, and unknown work remains intact until replay or explicit resolve-without-replay; explicit resolution redacts immediately and stores the stable `operator_resolved_without_replay` outcome. Purged-tenant rows are suppressed and redacted immediately. Parent and child free text/provider identifiers redact together while typed non-PII outcomes remain.

Completed Phase 1 operational slices:

- **1.6d — Completed: bounded SMTP telemetry and health** (`M`, depends on 1.6c): active non-paused readiness, closed-vocabulary provider/operational/RabbitMQ metrics, rank-only tenant backlog, current optional-reminder backpressure, and key-first public-health redaction are implemented and independently confirmed. Focused test execution remains blocked before discovery by unrelated shared-checkout compilation failures.
- **1.6e — Completed: operator controls and runbooks** (`M`, depends on 1.6d): the durable singleton processor state now carries global pause/audit and bounded rate override state; all claim/publish/pre-handoff paths observe the pause; instance-setting authority protects global controls; tenant-scoped `Unknown` reconciliation has explicit delivered/not-delivered outcomes; replay and HAL mirror the linked-ledger state machine; health exposes sanitized global booleans; and the requested self-hosting/operator runbooks are complete. Independent static review confirmed the slice, and all affected source projects build with zero errors. Tests remain intentionally stopped.

The completed Tasks 1.1–1.3 checkpoint introduced the relationship model, migrated registration/reminder/admin-invitation writers to the materializer, and landed the bounded pre-1.0 ledger reset with required constraints. The final full API and explicit Mailpit phase evidence is still open.

Phase-end verification (run once after 1.6a–1.6e):

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build
```

### Phase 2 — Registration transitions (Tasks 2.1–2.5)

Implementation is complete. Serializable orchestration now belongs to Application/UoW, repositories return explicit transition results, receipt/status/cancellation/revocation use the atomic channel primitive, and Task 1.4 maps cancellation/revocation preferences without reopening registration transaction code.

Phase-end verification (run once after Tasks 2.1–2.5):

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
```

### Phase 3 — Generic fanout engine (Tasks 3.1–3.6b)

Tasks 3.1–3.6b are implemented: immutable occurrence persistence/pointers, deterministic audience paging with `CoverageEstablishedAt`, fenced lease/checkpoint recovery, idempotent general-outbox-to-pending-run handoff, typed occurrence-recipient materialization, crash-safe page processing, occurrence precedence/coalescing, provider-handoff suppression, cross-replica fair claims, and bounded processor operations are complete:

- **3.4a — Completed: route pointers and ensure runs** (`M`, depends on 1.6e): strict v1 pointer/envelope validation, authoritative tenant/occurrence reload, per-occurrence locked idempotent pending-run creation, independent pre-generated retry identifiers, and immediate general-outbox completion are implemented and independently confirmed. The handoff never claims or retains recipient-work state.
- **3.4b — Completed: build typed recipient materialization** (`L`, depends on 3.4a): the closed four-key v1 factory, current verified-address/preference resolution, occurrence-linked atomic graph, exact dual-constraint recovery, and value-free recipient-bound location mask are implemented and independently confirmed. Only immutable occurrence values reach copy; unsupported key/version/policy/scope/change/JSON input fails closed.
- **3.4c — Completed: process pages with crash-safe checkpoint ordering** (`L`, depends on 3.4b): an already-fenced claim now drives bounded deterministic audience pages through the atomic recipient materializer; every full page commits before its compound checkpoint, stale claim state stops work, empty completion validates the template first, and partial-page replay converges through exact occurrence/user dedup. Same-horizon renewal is fenced, idempotent, and non-shortening.
- **3.5a — Completed: coordinate precedence, coalescing, and occurrence supersession** (`L`, depends on 3.4c): the Application coordinator acquires tenant-wide source identity before tenant/event precedence, verifies session/event authority, normalizes PostgreSQL timestamp precision, applies conditional supersession in the caller UoW, and emits one stable pointer only for a new winner. Exact replay follows only valid event/scope/priority/order chains; cancellation/heavy work is immediate and important updates preserve earliest-before/latest-after/latest-cutoff data across a sliding five-minute window.
- **3.5b — Completed: suppress already-materialized work at the handoff fence** (`M`, depends on 3.5a): a shared event advisory lock now orders supersession against final SMTP eligibility; exact tenant/occurrence-linked SQL supersedes only active email delivery and skips only unsent or unfenced SMTP work, while the evaluator reloads authoritative occurrence state before provider-handoff evidence and preserves immutable terminal/fenced evidence.
- **3.6a — Completed: add fair runnable selection and cross-instance tenant claim limits** (`L`, depends on 3.5b): one bounded PostgreSQL round ranks existing due runs one per tenant by priority/time/UUID; exact claims require the tenant ceiling, share tenant/event/occurrence locks with supersession, preserve expired cursors, and use one reversible partial global due index.
- **3.6b — Completed: add processor settings, optional-work backpressure, metrics, and health** (`M`, depends on 3.6a): each claim runs in a fresh scope; PostgreSQL enforces global/per-tenant ceilings and a persisted high/low-watermark state under one producer/claim global lock; optional reminder work stays durable but deferred while core backlog is high; aggregate health/metrics expose backlog, age, progress, contention, supersession, and backpressure without PII.

Phase-end verification (run once after 3.4a–3.6b):

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
```

### Phase 4 — Event and session triggers (Tasks 4.1–4.5)

Whole-event cancellation, published-session cancellation, material published-session time/location/room updates, explicit published rescheduling, and event-timezone reprojection are implemented with retry-stable occurrence/pointer identities, immutable snapshots, correct event/session precedence, deterministic five-minute coalescing/replay, recipient-bound disclosure, and post-commit cache invalidation. Phase 4 implementation is complete; its runtime gate remains open under the user's no-tests instruction. Next split reporter consent before any reporting email is enabled.

Phase-end verification (run once after Tasks 4.1–4.5):

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
```

### Phase 5 — Reporting email and provider convergence (Tasks 5.1–5.9)

Split consent, explicit reporter contracts, accessible default-unchecked submission choices, the provider/owner/tenant-authorized withdrawal endpoint, and HAL-gated authoritative My Reports controls are implemented. Next queue the consented report receipt, then final outcome, follow-up contact, and source-convergence evidence. Coop is enabled only if the Phase 0 prerequisite is live; otherwise local API remains functional and Coop email stays disabled. Osprey remains signal-only.

`EventReportDecision` remains the sole business-decision authority. A one-to-one `EventReportDecisionExecution` keyed by `DecisionId` stores only operational effect state: `Requested`, `InProgress`, an idempotent enforcement receipt, `CompletionPending`, and `Completed`. Reporter outcome channels materialize only in the completion transaction after successful enforcement. Escalation is nonterminal; stale/out-of-order Coop work cannot reopen a completed decision, and a crash between enforcement and completion resumes without repeating enforcement or email.

Phase-end verification (run once after Tasks 5.1–5.9):

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
```

### Phase 6 — Heavy moderation attendee email (Tasks 6.1–6.2)

Add required in-app plus operational email for irreversible heavy moderation. Copy is generic/linkless and the occurrence suppresses lower-priority links, updates, and reminders. Light-moderation email remains deferred.

Phase-end verification (run once after Tasks 6.1–6.2):

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build
```

### Phase 7 — Safe reminder activation (Tasks 7.1–7.3)

Schedule one reminder for an approved registration’s earliest covered published session, then prove supersession/cancellation, future-row stale-pointer guards, and DST/timezone reprojection before enabling the production caller.

The sole reminder setting is `EmailDispatch:EventReminderLeadTimeHours`, default `24`, inclusive range `1..168`. Dispatch time is `sessionStartUtc - lead`. If that time has passed but the session is still future, persist the reminder due immediately after commit; if the session has started, create none. Nonexistent local wall times are rejected at command validation. An ambiguous overlap uses the persisted event/session offset or UTC instant, never the machine-local timezone.

Production implementation now carries a strict `event-reminder:v2` authority containing the session ID, exact UTC ticks, and normalized event timezone. Reminder copy includes deterministic event-local date/time, the timezone ID, and the exact UTC instant; timezone-only event changes persist new local projections before reprojecting the existing reminder graph in the same transaction. Blazor is the local-wall input boundary: it rejects nonexistent DST-gap values, refuses to guess a new ambiguous overlap occurrence, and preserves an existing overlap occurrence from its persisted UTC instant. Direct API commands carry exact instants, so a nonexistent wall time is not representable at that boundary. Runtime Europe/Brussels, PostgreSQL, TickerQ, SMTP, and Mailpit proof remains deferred by the no-tests instruction.

Phase-end verification (run once after Tasks 7.1–7.3):

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build
```

## 12. Testing Strategy and Mandatory Fault Evidence

Test code belongs to the task that owns the behavior. Do not run per-task verification commands. At phase end, run the one Release build and selected non-browser project command shown in Section 11. Repetition of Application tests is deliberate for handler-heavy phases; Infrastructure tests own SMTP/reminder processing phases.

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
- legacy email and reporter-consent Up -> Down -> Up reconciliation;
- provider-handoff races where cancellation/consent/supersession wins before the fence and cannot claim recall after it;
- enforcement succeeds but outcome completion persistence fails, then recovers without repeated enforcement or email;
- immutable 10:00 -> 11:00 occurrence followed by 12:00 update;
- DST gap/overlap, timezone reprojection, and stale TickerQ pointer for future/superseded reminders;
- negative location-disclosure and heavy-moderation redaction assertions;
- architecture test rejects controller/handler dependencies on `IEmailService`, SMTP implementations, or direct send abstractions.

Before merge, satisfy the registered intents across the directly changed surfaces: Domain, Application, Persistence, Infrastructure, API, Architecture, Blazor Client, and Blazor Integration where applicable; run the exact Mailpit email category lane with a recorded non-zero count; prove EF has no pending model changes; and run canonical `source-command-check`. These are final contract evidence, not extra phase gates.

## 13. Deferred/Separate Product Work

- Light-moderation attendee email is deferred because it can be noisy and reversible; only existing in-app behavior remains.
- Automatic waitlist scanning, arbitrary organizer announcements, follower new-event mail, organization/group invitation redesign, API-key/support-access alerts, and marketing/Listmonk campaigns require separate product decisions.
- Account verification, password reset, credential update, and recovery remain Keycloak/PDS responsibilities.
- Anonymous reporter email requires a separately approved PII/verification model.

## 14. Cross-Cutting Classification

| Concern | Classification | Plan treatment |
|---|---|---|
| Security / authorization | Applicable | Server-authorized CQRS writes, strict HAL affordances, architecture guard, no transport calls from presentation/application handlers. |
| Privacy / abuse | Applicable | Verified current address, split withdrawable consent, immutable disclosure ceiling, linkless heavy-moderation copy, bounded redaction. |
| Multi-tenancy | Applicable | Composite tenant keys, tenant-scoped queries/leases, fair scheduling, per-tenant concurrency/rate controls. |
| Federation / providers | Applicable | Coop converges only after durable command success; Osprey remains signal-only; Keycloak/PDS retain credential email. |
| Localization | Needs investigation | Template localization/versioning remains task-owned; no new unlocalized public copy may ship silently. |
| Accessibility | Applicable in Phase 5 | Two consent controls, status/failure announcements, and HAL-gated My Reports controls require client accessibility coverage. |
| Compatibility | Not required | Pre-1.0 breaking schema/API changes are allowed; migrations remain deterministic and reversible without compatibility shims. |

## 15. Risk Register

| Risk | Likelihood | Impact | Mitigation / detection | Owner |
|---|---:|---:|---|---|
| Partial SMTP operations patch is mistaken for complete | High | High | Rate/fencing and bounded health/telemetry are implemented; keep 1.6e unchecked until authenticated operator controls and runbooks pass. | 1.6e |
| One tenant starves others or exceeds provider limits | Medium | High | Atomic tenant rounds, cross-instance Processing ceilings, persisted global/per-tenant token buckets, active-only readiness, and rank-bounded backlog telemetry are implemented; retain runtime PostgreSQL evidence as an open phase gate. | 3.6a–3.6b |
| Rate deferral exhausts retries without SMTP | High | High | Implemented: durable rate denial clears the claim without attempt, receipt, provider evidence, or dead-letter authority. | 1.6c evidence |
| Unfenced crash is mislabeled provider uncertainty | Medium | High | Implemented: recovery returns unfenced work to retryable state and marks only fenced/partial-fence uncertainty `Unknown`. | 1.6c evidence |
| Old occurrence sends mutable current data | Medium | High | Immutable snapshots, typed template versions, snapshot-only disclosure, crash/replay tests. | 3.4b–3.4c |
| Cancellation loses a race to SMTP | Medium | High | Supersession transition plus authoritative pre-handoff fence; post-handoff becomes Unknown, never recalled. | 3.5b |
| Reporter consent is over-broadened | Medium | High | Separate purposes, false-safe backfill, withdrawal at dispatch, no anonymous reporter email. | 5.1–5.8 |
| Dirty shared checkout causes accidental overwrite | High | High | Main checkout only, preserve unrelated ATProto/auth/location changes, narrow staging/diffs, no worktrees. | Every task |
| Current baseline prevents clean verification | High | Medium | Record the latest unrelated ATProto compile failure and Docker gaps without misattributing them; rerun the phase gate from the then-current `HEAD`. | 1.6e / phase gate |

## 16. Definition of Done

The workstream is complete only when all 51 tasks are checked, the Phase 0 Coop prerequisite is independently proven, every required channel policy and trigger is implemented, all phase gates and the canonical repository verification pass, Mailpit-backed delivery remains green, consent/preference/address/supersession checks are dispatch-time safe, retention/redaction and operator controls are usable, and `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/BLAZOR.md`, `docs/EMAIL_NOTIFICATIONS.md`, `docs/NOTIFICATIONS.md`, `docs/OUTBOX_PATTERN.md`, `docs/CONFIGURATION.md`, `docs/COOP_INTEGRATION.md`, `docs/OSPREY_INTEGRATION.md`, `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`, and `schemas/islamu-event.md` match runtime behavior.

Implementation agents must synchronize this plan, the context, and the task ledger before handoff, pause, compaction, or completion.

## 17. Implementation Agent Contract

1. On a cold resume, read context and tasks first, then only the current phase and changed decisions from this plan.
2. Work from the highest-priority unchecked task unless the user overrides it; never recreate completed or partial main-checkout work.
3. Treat `tasks.md` as the hot ledger. Check substantial tasks immediately after acceptance is met and keep phase verification checkboxes separate.
4. Update context after a phase, decision, blocker, failed validation, material discovery, or handoff. Update this plan only for strategy/scope/acceptance/risk changes.
5. Generate IDs/timestamps before retry delegates; keep SMTP/HTTP/broker/scheduler calls outside transactions; recover PostgreSQL unique conflicts only in a fresh UoW.
6. Use only the main checkout. Do not create a linked worktree or `.worktrees`, broadly stage, revert shared files, or overwrite unrelated dirty work.
7. Run only the phase-end commands listed in Section 11 during phase implementation. Record failures precisely and never mark a phase complete while its gate is open.

Implementation slice summaries must state: implemented architecture and control flow, exact verification evidence, remaining work, next slice, and whether tasks/context/plan were synchronized.

## 18. Potential Risks and Unknowns

All planned production email code is now present. The remaining risk is evidence rather than an unimplemented product seam: Task 5.9 still needs live local/Coop/Osprey decision convergence proof, and Task 7.3 still needs runtime Europe/Brussels/PostgreSQL/TickerQ verification of exact UTC scheduling, local display, one-live-graph reprojection, and harmless old pointers. Both remain unchecked under the user's no-tests instruction. The source implementation uses no machine-local scheduling authority and passed affected product builds plus independent static review.
