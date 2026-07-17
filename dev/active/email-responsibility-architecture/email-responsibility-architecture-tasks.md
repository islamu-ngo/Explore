<!-- ABOUTME: Focused implementation ledger for the CTO-revised lifecycle-email architecture. -->
<!-- ABOUTME: Tracks schema, consent, fanout, trigger, operations, and verification slices with synchronized acceptance evidence. -->

# Email Lifecycle Delivery Tasks

> **Status:** Approved — Phase 0A and Task 0.5 complete; Tasks 0.6–0.7 provisionally ready for independent verification
> **Last Updated:** 2026-07-17 Europe/Brussels
> **Progress:** 5/43 tasks complete
> **Current tasks:** finish the shared 1.1–1.3 Stage C migration, then independently verify 0.6–0.7 and rerun their deferred full gates
> **Completed foundation:** SMTP/Mailpit, EmailDispatch drain/recovery, notification ownership/audit, registration confirmation, and tenant-admin invitation

## Working Rules

- Check a task only after its acceptance and focused failing-first tests are recorded.
- Run the full unfiltered directly affected project suites at every phase gate; the exact commands are in the plan.
- Do not use broad OR filters or `--minimum-expected-tests 1` as release evidence.
- Generate occurrence/dedup IDs before a retryable transaction delegate.
- Persist intent, channel deliveries, in-app notification, and email row atomically per recipient.
- After a PostgreSQL unique violation, roll back and use a fresh transaction; never continue in the aborted transaction.
- Never perform SMTP, HTTP, broker publish, or scheduler calls inside a transaction delegate.
- Repositories perform persistence operations but do not own commit/transaction orchestration or create notification/email entities.
- Use exact tenant predicates and persisted current verified user email.
- Keep Osprey callbacks signal-only and reporter outcomes on `ExecuteReportDecisionCommandHandler`.
- Synchronize plan, context, and this ledger before handoff, pause, compaction, or completion.

## Phase 0A — Approved Architecture and Policy Baseline

- [x] **0.1 Approve logical intent, channel delivery, and transport work.**
  - Files: the three workstream docs, `docs/ARCHITECTURE.md`, `docs/EMAIL_NOTIFICATIONS.md`, `docs/NOTIFICATIONS.md`, `docs/OUTBOX_PATTERN.md`.
  - Acceptance: `NotificationIntent`, `NotificationDelivery`, and `EmailDispatchOutbox` have separate responsibilities; the channel matrix defines required/optional in-app and email behavior per trigger; PostgreSQL EmailDispatch remains canonical.

- [x] **0.2 Approve reporter consent, withdrawal, and single-SLA policy.**
  - Files: workstream docs plus reporting/security/API documentation.
  - Acceptance: case-update and follow-up consent are distinct; both default false and can be withdrawn; dispatch-time behavior is defined; `Reporting:CaseSlaHours` is the only response-time setting and copy contains no exact business-day promise.

- [x] **0.3 Approve immutable occurrence, audience, coalescing, and supersession policy.**
  - Files: workstream docs plus event/notification/outbox documentation.
  - Acceptance: cutoff and live-status rules cover event/day/session registration, partially cancelled children, pending/waitlisted/approved states, stable cursor, immutable before/after data, five-minute default coalescing, and fixed precedence.

- [x] **0.4 Approve retention, redaction, fairness, observability, and operator policy.**
  - Files: `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/EMAIL_NOTIFICATIONS.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`.
  - Acceptance: 180-day resolved retention, unresolved evidence rules, tenant-deletion behavior, replay restriction after redaction, fair tenant selection, concurrency/rate/backpressure controls, metrics/alerts, and operator procedures are concrete and non-contradictory.

Phase 0A evidence is static documentation/provenance validation. No product build or test was rerun for these four policy tasks.

## Phase 0B — Coop Callback Correctness Prerequisite

Phase 0B may run in parallel with Phase 1. It blocks only Task 5.9 source convergence.

- [x] **0.5 Persist a unique Coop callback effect pointer.**
  - Files: Coop verifier, incoming-webhook contracts/processing/handler, `IncomingWebhookEffectOutbox` entity/configuration/repository, retention cleanup, migration/model snapshot, schema, focused persistence/API tests.
  - Acceptance: `coop` + `moderation.coop.decision` creates one specialized `IncomingWebhookEffectOutbox` per inbox/effect kind using a nonblank signed `ProviderDecisionId`; exact ID+payload hash replay is idempotent, same ID/different hash and missing IDs quarantine; unique `(TenantId, Provider, ProviderDecisionId, EffectKind)` and `(TenantId, IncomingWebhookMessageId, EffectKind)` plus composite inbox FK enforce identity/retention; no raw callback payload enters the pointer or logs.
  - Evidence: independently confirmed after DBML composite-FK parity repair; focused PostgreSQL/migration tests 7/7, verifier tests 2/2, EF pending-model check clean, and all six directly affected full suites passed with zero failures. `.omo/evidence/email-lifecycle-delivery/task-2/` contains the receipt.

- [ ] **0.6 Dispatch Coop effects with correct decision ordering.**
  - Files: general outbox dispatcher, retained-callback loader, `ProcessCoopDecisionCallbackCommandHandler`, execute-handler integration, focused application/infrastructure/API tests.
  - Acceptance: dispatch occurs outside the incoming-inbox transaction; command success is required before pointer completion; callback/pointer/dispatcher replay is idempotent; stale or out-of-order decisions cannot reopen or overwrite a completed case.
  - Provisional evidence: implementation and focused verification are complete; fenced claim/recovery, retained-payload identity validation, command-success receipt/pointer settlement, duplicate-worker exclusion, cancellation recovery, and existing closed-case idempotency tests pass. Checkbox remains open pending independent verification after the Phase 1 Stage C migration restores shared full gates.

- [ ] **0.7 Quarantine and operate failed Coop callbacks.**
  - Files: incoming-webhook failure state/cleanup, operator controls, Coop/configuration/operations docs, focused failure tests.
  - Acceptance: poison callbacks become quarantined/dead-lettered with sanitized evidence; cleanup respects callback/effect dependency order; operators can inspect/redrive safely; documentation no longer claims unimplemented routing.
  - Provisional evidence: bounded retry/dead-letter, generation-checked audited redrive, HAL/auth, retention ordering, aggregate health/metrics, runbooks, and API/schema parity are implemented and focused tests pass. Full Persistence/API reruns remain explicitly deferred; see `.omo/evidence/email-lifecycle-delivery/task-3/`.

Phase 0 gate: Release build plus full `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, and `Event.Architecture.Tests`.

## Phase 1 — Atomic Recipient-Delivery Primitive

Tasks 1.1–1.3 share one approved dependency checkpoint: Stage A adds the relationship model/contracts; Stage B implements the atomic materializer and migrates registration, reminder-scheduler, and managed-invitation writers; Stage C transactionally resets only the obsolete pre-1.0 delivery ledgers and installs required constraints. Verification covers the complete transition, and no intermediate stage closes the checkpoint independently.

- [ ] **1.1 Add explicit intent/delivery/email schema relationships and reset obsolete delivery ledgers.**
  - Files: `NotificationIntent`, `NotificationDelivery`, `EmailDispatchOutbox`, lookup enums/entities, EF configurations, pre-1.0 reset migration, model snapshot, `LookupTableSeeder`, `schemas/islamu-event.md`, domain/persistence tests.
  - Acceptance: delivery has channel/policy/version plus immutable consent/preference/disclosure/template/link/address-source snapshot, channel-neutral outcomes, and optional notification/email links; dispatch may narrow but never broaden the snapshot. Exactly `TenantUserVerifiedEmail` and `ManagedTenantAdministratorInvitation` enforce their tenant/member/authority rules. Recipient/source equality, external-delegation tenant equality, one-channel/one-email uniqueness, tenant-aware attempt/receipt FKs, and the two named raw nullable-principal constraints are proven. Up deletes only intent/delivery/delegation/email/attempt/receipt ledgers; notifications and event/registration/report/audit/settings/tenant/user canaries retain their values. Down restores the old empty schema and lookup codes without claiming it can reconstruct deleted delivery work; second Up is deterministic.

- [ ] **1.2 Create the atomic recipient channel materializer.**
  - Files: notification orchestrator/drafts, notification and email repositories, new Application service/contract, delivery-policy resolver, focused unit/integration tests.
  - Acceptance: one UoW operation creates the logical intent, all configured delivery rows, required/selected in-app notification, and eligible email outbox; no partial set can commit; a skipped channel still has a typed delivery outcome; no transport side effect runs.

- [ ] **1.3 Implement exact unique-conflict recovery and batch insert semantics.**
  - Files: intent/delivery/email repository contracts and PostgreSQL implementations, UoW recovery coordination, focused fault/concurrency tests.
  - Acceptance: two workers for one occurrence/user produce one intent and one row per channel; exact conflict rolls back before fresh-transaction load/repair; unrelated DB errors throw; rollback after intent or email insertion commits nothing; fanout batch path uses explicit-target `ON CONFLICT DO NOTHING ... RETURNING` where proven to avoid N+1 checks. SMTP acceptance followed by persistence uncertainty settles once as `Unknown` and never blind-resends.

- [ ] **1.4 Centralize dispatch-time eligibility.**
  - Files: new eligibility evaluator, `EmailDispatchDrainService`, user/report/occurrence read contracts, delivery-state updates, infrastructure tests.
  - Acceptance: current tenant/user/email verification, changed address, deletion, optional preference, consent purpose, supersession, and required-policy behavior are rechecked before SMTP; old address is never used; every skip has a stable non-PII reason and updates email plus delivery state.

- [ ] **1.5 Add parent-aware email retention and content redaction.**
  - Files: retention settings/validator, cleanup repository/service/scheduler, email/attempt/receipt state, configuration/operations docs, focused persistence/infrastructure tests.
  - Acceptance: sent/skipped content redacts after 180 days; unresolved failure material waits for operator resolution then follows 180 days; children follow parent; tenant deletion redacts and suppresses; cleanup is bounded/idempotent/dry-runnable; redacted rows cannot replay.

- [ ] **1.6 Make the SMTP ledger operationally fair and observable.**
  - Files: pending-row query, processor settings, rate/backpressure controls, metrics/health/admin surfaces, operator docs, architecture tests.
  - Acceptance: pending selection cannot let one tenant starve others; batch/global/per-tenant/rate limits are validated; required work outranks optional reminder work; oldest age, outcomes, typed skips, dead letters, and tenant backlog are observable without recipient PII; operators can pause/drain/inspect/replay; architecture tests reject direct SMTP/send-service dependencies from controllers and MediatR handlers.

Phase 1 gate: Release build plus full `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, and `Event.Architecture.Tests`, followed by the explicit `[Category=Email]` Infrastructure/Mailpit lane with an exact recorded non-zero test count.

## Phase 2 — Registration Transitions

- [ ] **2.1 Move registration creation transaction ownership to Application/UoW.**
  - Files: `IUnitOfWork` isolation contract, `EfCoreUnitOfWork`, `IEventRegistrationIntentRepository`, `EventRegistrationIntentRepository`, `CreateEventRegistrationCommandHandler`, focused tests.
  - Acceptance: Application requests a serializable transaction; repository performs capacity/parent/child persistence without `BeginTransaction` or commit and no longer accepts email/notification entities; registration, integration-sync intent, and notification channels commit together.

- [ ] **2.2 Move capacity-aware registration updates to Application/UoW.**
  - Files: registration repository contract/implementation, update handler, transition result, focused concurrency tests.
  - Acceptance: repository update has no internal retry transaction; explicit result contains `Changed`, `ParentIntentId`, `PreviousStatus`, `FinalStatus`, `TransitionReason`, and pre-generated `OccurrenceId`; capacity and parent recomputation remain serializable and atomic.

- [ ] **2.3 Move registration cancellation transaction ownership to Application/UoW.**
  - Files: cancellation repository method/handler, transition result/provenance, focused capacity/rollback tests.
  - Acceptance: cancellation/release/recompute uses the Application-owned serializable boundary; parent/child soft-delete semantics and capacity release remain correct; actor provenance distinguishes self-cancel from organizer/system revoke without repository notification logic.

- [ ] **2.4 Migrate receipt, approval, rejection, and waitlist-promotion delivery.**
  - Files: registration handlers/services/factory and tests.
  - Acceptance: registration receipt preserves current behavior through the new primitive; final parent transitions create required in-app plus optional email exactly once; only an actual `Waitlisted -> Approved` sends promotion copy; multi-session child changes cannot announce contradictory status.

- [ ] **2.5 Add cancellation/revocation notification with safe provenance.**
  - Files: update/delete handlers, registration templates/policies, focused tests.
  - Acceptance: self-cancel and organizer/system revoke select safe distinct copy only from persisted actor provenance; one intent follows one changed parent transition; no-op/replay creates nothing; missing/unverified email leaves required in-app and typed skipped email delivery.

Phase 2 gate: Release build plus full `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, and `Event.Architecture.Tests`.

## Phase 3 — Generic Fanout Engine

- [ ] **3.1 Persist immutable fanout occurrences and pointer serialization.**
  - Files: new `NotificationFanoutOccurrence` entity/config/repository/migration, pointer contract/factory, JSON source generation, dispatcher registration, domain/application/persistence tests.
  - Acceptance: all required occurrence fields from the plan persist; ID exists before the retry delegate; mutation and one PII-free pointer can commit together; snapshots are immutable except explicit supersession metadata; worker loads by tenant/occurrence pointer; fanout intent has an occurrence FK and unique `(TenantId, FanoutOccurrenceId, UserId)` guard.

- [ ] **3.2 Implement deterministic event/session audience queries.**
  - Files: registration intent repository contract/implementation, compound cursor model, persistence tests.
  - Acceptance: cutoff/current-status predicates are exact; both parent creation and immutable child `CoverageEstablishedAt` are at/before cutoff; same-scope atomic replacement inherits the old coverage timestamp while a newly added/moved session receives a new timestamp; event-wide and target-session cohorts follow the plan; whole-event/day/session scope, partially cancelled children, pending/waitlisted/approved parents, registrations after cutoff, moved/replaced children, and duplicate children are covered; cursor is `(FirstEligibleRegistrationCreatedAt, UserId)`.

- [ ] **3.3 Add lease, checkpoint, and stale-run recovery.**
  - Files: `NotificationFanoutRun` entity/config/repository/service, scheduler/worker tests.
  - Acceptance: occurrence has at most one active lease; concurrent claims are safe; checkpoint stores compound cursor/counts; crash may replay but not skip the last page; lease expiry resumes; stale cursor replay remains idempotent.

- [ ] **3.4 Materialize one recipient atomically from fanout.**
  - Files: generic fanout worker and atomic materializer integration, persistence/application tests.
  - Acceptance: one occurrence/user creates one intent and policy channels; location decision is recipient-specific but values come from immutable snapshot; batch checkpoint advances only after durable recipient outcomes; crash mid-batch creates no duplicates or lost recipients.

- [ ] **3.5 Implement coalescing and supersession state transitions.**
  - Files: occurrence repository/service, email/delivery suppression updates, scheduler integration, tests.
  - Acceptance: fixed precedence is enforced; cancellation/heavy work is immediate; five-minute default update coalescing retains earliest before/latest after; newer unsent updates supersede old work; already-materialized unsent rows are skipped; sent evidence is unchanged.

- [ ] **3.6 Add fair tenant scheduling, backpressure, and fanout telemetry.**
  - Files: runnable-occurrence query/worker settings, health/metrics, operations/configuration docs, tests.
  - Acceptance: one active tenant lease by default, fair tenant rounds, bounded global throughput, configurable limits, required work priority, optional-work backpressure, oldest-age/processed/remaining/lease-contention metrics, and no recipient PII.

Phase 3 gate: Release build plus full `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, and `Event.Architecture.Tests`.

## Phase 4 — Event and Session Triggers

- [ ] **4.1 Wire whole-event cancellation first.**
  - Files: `CancelEventCommandHandler`, occurrence factory/repository, cancellation templates, focused application/API tests.
  - Acceptance: state and immediate occurrence pointer commit together; audience is event-wide live-at-cutoff; repeated/already-cancelled paths create nothing; event cancellation suppresses session updates and reminders; cache invalidation is post-commit.

- [ ] **4.2 Wire published-session cancellation.**
  - Files: `EventSessionLifecycleTransitionCommandHandlerBase`, occurrence/templates, focused tests.
  - Acceptance: only attendees covering the target session are selected; state and immediate pointer are atomic; unrelated sessions/users are excluded; target updates/reminders are suppressed; repeated transitions create nothing.

- [ ] **4.3 Wire material time, location, and room changes.**
  - Files: `UpdateEventSessionCommandHandler`, immutable change-set factory, disclosure/template logic, focused tests.
  - Acceptance: draft, cosmetic, and no-op edits create nothing; published material changes persist exact safe before/after data; multiple changed fields produce one coalesced occurrence; exact location appears only for currently authorized recipients.

- [ ] **4.4 Wire published session scheduling/rescheduling.**
  - Files: `ScheduleEventSessionCommandHandler`, occurrence/coalescing service, cache sequencing, focused tests.
  - Acceptance: schedule mutation and immutable occurrence pointer are atomic; a 10:00 -> 11:00 occurrence cannot later become 10:00 -> 12:00 by rereading state; newer unsent schedules supersede/coalesce correctly; caches invalidate after commit.

- [ ] **4.5 Wire event-timezone reprojection.**
  - Files: `UpdateEventCommandHandler.ApplyTimezone`, occurrence snapshots/templates, focused application/API tests.
  - Acceptance: only a published timezone change that changes displayed session times creates one event-wide occurrence; snapshot identifies affected sessions without mutable reconstruction; session/cancellation precedence applies; DST gap/overlap cases are retained for Phase 7 reminder proof.

Phase 4 gate: Release build plus full `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, and `Event.Architecture.Tests`.

## Phase 5 — Reporting Email and Provider Convergence

- [ ] **5.1 Split reporter consent in the domain and database.**
  - Files: `EventReport`, EF configuration, new migration/backfill, model snapshot, domain/persistence tests.
  - Acceptance: `ReportCaseUpdatesConsent` and `ReportFollowUpContactConsent` replace the old field; legacy true maps only to follow-up consent because that is what the old wording authorized; case-update consent backfills false; aggregate method changes consent with audit/concurrency semantics; populated Up -> Down -> Up reconstructs the old field only from follow-up consent and never widens case-update authority.

- [ ] **5.2 Change reporter read/write and OpenAPI contracts.**
  - Files: submission/my-report/moderation DTOs, commands/validators/mappers, OpenAPI changelog, regenerated NSwag client, application/API/client contract tests.
  - Acceptance: old property is absent; both consent purposes are explicit; generated client is regenerated, not hand-edited; breaking change is documented; evidence and moderator/provider internals remain excluded.

- [ ] **5.3 Add two explicit choices to report submission UI.**
  - Files: `ReportEventDialog.razor`, code-behind/CSS, reporting service, accessibility/client tests.
  - Acceptance: independently labelled unchecked controls distinguish case updates from clarification contact; submitted DTO carries both; labels/focus/announcements are accessible; no consent is inferred from submission itself.

- [ ] **5.4 Add reporter-owned consent withdrawal endpoint and HAL affordance.**
  - Files: command/validator/handler, controller route/response metadata/rate limit, route names, detail and collection HAL policies, API tests.
  - Acceptance: authorized owner can update both consents; non-owner/tenant mismatch fails closed; UoW persists audit/concurrency; both My Reports representations emit `update-communication-consent` only when allowed; controller only dispatches MediatR and assembles HAL.

- [ ] **5.5 Add HAL-gated withdrawal controls to My Reports.**
  - Files: My Reports page/detail UI, reporting client service, generated-client use, CSS/accessibility/client tests.
  - Acceptance: UI renders/edit controls only when the HAL relation exists; withdrawal immediately refreshes resource state; no local role/claim gating; failures are accessible and do not falsely update the screen.

- [ ] **5.6 Queue report receipt using the canonical SLA.**
  - Files: report email factory/templates, `SubmitEventReportCommandHandler`, policy/materializer integration, configuration/docs/tests.
  - Acceptance: report/case/provider-sync/in-app/email state commit atomically; case-update consent gates email; current verified address is required; copy thanks the reporter, derives the review window only from bounded `CaseSlaHours` (default 48), snapshots that resolved value/template so later configuration changes cannot rewrite queued copy, and excludes evidence/fingerprints; no consent or email records a typed skipped channel, not a second fallback intent.

- [ ] **5.7 Queue safe final outcome after successful enforcement.**
  - Files: `ExecuteReportDecisionCommandHandler`, outcome mapping/templates, focused application/persistence tests.
  - Acceptance: `EventReportDecision` remains the sole decision authority; a one-to-one `EventReportDecisionExecution` records requested/in-progress, idempotent enforcement receipt, completion-pending, and completed effect states. NoViolation/Duplicate receive safe no-action copy; LightModerate/HeavyRedact/WarnOrganizer receive safe action-taken copy; completion and notification channels commit together after enforcement; enforcement-success/completion-failure resumes without duplicate enforcement/email; failed, escalated, stale, already-closed, and replay paths create no false/duplicate final email.

- [ ] **5.8 Queue needs-more-information follow-up separately.**
  - Files: execute handler mapping, follow-up template/policy, tests.
  - Acceptance: only `ReportFollowUpContactConsent` authorizes email; required in-app remains; the message is not terminal/final-decision copy; withdrawal before dispatch skips; repeated requests have distinct occurrence IDs but exact replay deduplicates.

- [ ] **5.9 Prove local API, Coop, and Osprey source convergence.**
  - Files: local report-decision API integration tests, Coop callback/effect integration tests, Osprey regression tests, provider docs.
  - Acceptance: a successful local API decision creates the same executor-owned outcome intent; enabled Coop creates it only after callback command/enforcement success; duplicate/out-of-order callbacks do not duplicate; Osprey callback alone creates no decision email and a later local execution does.

Phase 5 gate: Release build plus full `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, `Explore.Blazor.Client.Tests`, `Explore.Blazor.IntegrationTests`, and `Event.Architecture.Tests`.

## Phase 6 — Heavy Moderation Attendee Email

- [ ] **6.1 Add heavy-moderation required channel materialization.**
  - Files: heavy moderation occurrence creation, existing moderation fanout integration, `ModerationAvailabilityRequired` policy, focused tests.
  - Acceptance: irreversible heavy enforcement and one immediate occurrence pointer commit correctly; eligible attendees receive required in-app plus required email delivery when a verified current address exists; missing/unverified address is typed skipped; repeated enforcement is idempotent.

- [ ] **6.2 Enforce linkless privacy and highest-precedence suppression.**
  - Files: dedicated template/payload, disclosure/supersession integration, metrics/docs/tests.
  - Acceptance: subject/body/pointer contain no title, slug, URL, description, image, organizer, evidence, decision note, provider, storage path/key, or raw error; no event link is emitted; all pending lower-priority event work is suppressed; negative privacy tests inspect serialized payload and rendered bodies. Light-moderation email remains absent.

Phase 6 gate: Release build plus full `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, and `Event.Architecture.Tests`.

## Phase 7 — Safe Reminder Activation

- [ ] **7.1 Schedule one reminder for an approved registration.**
  - Files: registration lifecycle integration, `EventLifecycleScheduler`, reminder policy/settings/docs/tests.
  - Acceptance: only Approved parent and target child qualify; earliest covered published session determines one reminder; sole `EmailDispatch:EventReminderLeadTimeHours` setting defaults to `24` and accepts inclusive `1..168`; dispatch is `sessionStartUtc - lead`; a past due-at for a future session becomes due immediately after commit, while an already-started session creates none; multi-session rows do not duplicate; optional in-app/email preference behavior is explicit.

- [ ] **7.2 Suppress stale/cancelled reminders and stale pointers.**
  - Files: cancellation/update supersession, `EmailDispatchDrainService`, TickerQ single-row path, scheduler/drain tests.
  - Acceptance: registration/session/event cancellation and heavy moderation prevent send; changed time/timezone supersedes/reschedules; a stale pointer cannot send a future `Pending.NextAttemptAt` row; superseded/reminder skips update linked delivery state.

- [ ] **7.3 Prove timezone and DST reprojection.**
  - Files: reminder projection/calculation, timezone update integration, tests/docs.
  - Acceptance: UTC scheduling remains correct across DST gap/overlap, event timezone change, and local display reprojection; nonexistent local wall times fail validation; ambiguous overlaps use the persisted offset/UTC instant rather than machine-local choice; old pointer remains harmless; one live replacement reminder exists; tests record exact Europe/Brussels zones/instants.

Phase 7 gate: Release build plus full `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, and `Event.Architecture.Tests`, followed by the explicit `[Category=Email]` Infrastructure/Mailpit lane with an exact recorded non-zero test count.

## Mandatory Phase Verification Matrix

Every phase runs `dotnet build --configuration Release --verbosity quiet`, followed by each listed project with `dotnet test --project <path> --configuration Release --no-build`.

| Phase | Full test projects |
|---|---|
| 0 | `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, `Event.Architecture.Tests` |
| 1 | `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, `Event.Architecture.Tests` + explicit Infrastructure `Email`/Mailpit lane |
| 2 | `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests` |
| 3 | `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.Architecture.Tests` |
| 4 | `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, `Event.Architecture.Tests` |
| 5 | `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, `Explore.Blazor.Client.Tests`, `Explore.Blazor.IntegrationTests`, `Event.Architecture.Tests` |
| 6 | `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.Architecture.Tests` |
| 7 | `Event.Application.UnitTests`, `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, `Event.Architecture.Tests` + explicit Infrastructure `Email`/Mailpit lane |

## Final Contract Evidence Before Merge

- [ ] All 43 tasks and all required channel-matrix rows are complete.
- [ ] Coop routing prerequisite has independent acceptance/verification evidence.
- [ ] Architecture tests forbid direct SMTP/send dependencies from controllers and handlers.
- [ ] Fault/concurrency/privacy scenarios listed in the plan are covered.
- [ ] Relevant Mailpit-backed SMTP and EmailDispatch drain tests pass.
- [ ] `dotnet ef migrations has-pending-model-changes` reports no pending model change.
- [ ] Canonical per-project verification from `source-command-check` is green or any pre-existing failure is recorded precisely.
- [ ] API/OpenAPI/generated client, HAL affordances, runtime configuration, schema, and canonical docs match behavior.
- [ ] Plan/context/tasks progress, next task, files, decisions, risks, and evidence are synchronized.
