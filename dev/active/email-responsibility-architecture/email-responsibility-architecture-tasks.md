<!-- ABOUTME: Focused implementation ledger for the CTO-revised lifecycle-email architecture. -->
<!-- ABOUTME: Tracks schema, consent, fanout, trigger, operations, and verification slices with synchronized acceptance evidence. -->

# Email Lifecycle Delivery Tasks

> **Status:** Re-baselined in implementation — committed foundations plus preserved main-checkout SMTP and unrelated work
> **Last Updated:** 2026-07-19 Europe/Brussels
> **Progress:** 45/51 implementation tasks complete; phase verification remains separate
> **Current task:** 6.1 add heavy-moderation required channel materialization; Task 5.9 runtime proof remains deferred by the no-tests instruction
> **Completed foundation:** explicit delivery schema, atomic materializer, dispatch eligibility/provider-handoff fence, Coop effect processing, all registration lifecycle transitions, immutable fanout occurrence/audience/lease persistence, typed occurrence-recipient materialization

## Working Rules

- Check a task only after its acceptance and focused failing-first tests are recorded.
- Run one Release build and one selected non-browser project test once after all tasks in a phase; final intent-contract suites remain separate merge evidence.
- Do not use broad OR filters or `--minimum-expected-tests 1` as release evidence.
- Generate occurrence/dedup IDs before a retryable transaction delegate.
- Persist intent, channel deliveries, in-app notification, and email row atomically per recipient.
- After a PostgreSQL unique violation, roll back and use a fresh transaction; never continue in the aborted transaction.
- Never perform SMTP, HTTP, broker publish, or scheduler calls inside a transaction delegate.
- Reserve durable SMTP rate capacity before incrementing attempt count or writing provider-handoff evidence; a rate deferral is not an SMTP attempt.
- Recover stale unfenced claims to retryable work; only provider-handoff-fenced uncertainty becomes `Unknown`.
- Repositories perform persistence operations but do not own commit/transaction orchestration or create notification/email entities.
- Use exact tenant predicates and persisted current verified user email.
- Keep Osprey callbacks signal-only and reporter outcomes on `ExecuteReportDecisionCommandHandler`.
- Work only in the main repository checkout. Do not run `git worktree add`, create linked worktrees or a `.worktrees` directory, or record a worktree path for this workstream. This user constraint overrides generic automation/workflow guidance; stop and report a conflict instead of creating one.
- Synchronize plan, context, and this ledger before handoff, pause, compaction, or completion.
- Preserve the existing uncommitted email-retention/SMTP-operations patch and unrelated ATProto/auth/location-privacy work; do not stage, revert, overwrite, or re-create either workstream while updating this ledger.

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
  - Evidence: independently confirmed after DBML composite-FK parity repair; focused PostgreSQL/migration tests 7/7, verifier tests 2/2, EF pending-model check clean, and all six directly affected full suites passed with zero failures. Committed coverage lives in `CoopIncomingWebhookEffectOutboxTests` and `IncomingWebhookFrameworkTests`.

- [x] **0.6 Dispatch Coop effects with correct decision ordering.**
  - Files: general outbox dispatcher, retained-callback loader, `ProcessCoopDecisionCallbackCommandHandler`, execute-handler integration, focused application/infrastructure/API tests.
  - Acceptance: dispatch occurs outside the incoming-inbox transaction; command success is required before pointer completion; callback/pointer/dispatcher replay is idempotent; stale or out-of-order decisions cannot reopen or overwrite a completed case.
  - Evidence: `IncomingWebhookEffectProcessingService`, `IncomingWebhookEffectDrainService`, the effect repository, and focused application/persistence tests implement fenced claim/recovery, retained-payload identity validation, command-success receipt/pointer settlement, duplicate-worker exclusion, cancellation recovery, and closed-case idempotency. Phase verification is tracked separately.

- [x] **0.7 Quarantine and operate failed Coop callbacks.**
  - Files: incoming-webhook failure state/cleanup, operator controls, Coop/configuration/operations docs, focused failure tests.
  - Acceptance: poison callbacks become quarantined/dead-lettered with sanitized evidence; cleanup respects callback/effect dependency order; operators can inspect/redrive safely; documentation no longer claims unimplemented routing.
  - Evidence: bounded retry/dead-letter, generation-checked audited redrive, HAL/auth, retention ordering, aggregate health/metrics, runbooks, and API/schema parity are implemented. The fresh full API gate remains open; it is not an implementation blocker.

Phase 0 implementation is complete. Historical post-migration evidence covers build, Domain, Application, Persistence, Infrastructure, and Architecture; a fresh full API run remains unrecorded.

### Phase 0B Verification — HISTORICAL EVIDENCE RECORDED; CURRENT GATE NOT RE-RUN

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build`

## Phase 1 — Atomic Recipient-Delivery Primitive

Tasks 1.1–1.3 completed the approved dependency checkpoint: relationship model/contracts, atomic materializer and writer migration, then the bounded pre-1.0 delivery-ledger reset with required constraints. Phase verification remains separate from these implementation checkboxes.

- [x] **1.1 Add explicit intent/delivery/email schema relationships and reset obsolete delivery ledgers.**
  - Files: `NotificationIntent`, `NotificationDelivery`, `EmailDispatchOutbox`, lookup enums/entities, EF configurations, pre-1.0 reset migration, model snapshot, `LookupTableSeeder`, `schemas/islamu-event.md`, domain/persistence tests.
  - Acceptance: delivery has channel/policy/version plus immutable consent/preference/disclosure/template/link/address-source snapshot, channel-neutral outcomes, and optional notification/email links; dispatch may narrow but never broaden the snapshot. Exactly `TenantUserVerifiedEmail` and `ManagedTenantAdministratorInvitation` enforce their tenant/member/authority rules. Recipient/source equality, external-delegation tenant equality, one-channel/one-email uniqueness, tenant-aware attempt/receipt FKs, and the two named raw nullable-principal constraints are proven. Up deletes only intent/delivery/delegation/email/attempt/receipt ledgers; notifications and event/registration/report/audit/settings/tenant/user canaries retain their values. Down restores the old empty schema and lookup codes without claiming it can reconstruct deleted delivery work; second Up is deterministic.
  - Evidence: migration `20260717131038_NormalizeRecipientNotificationDelivery`, model snapshot, seeder/schema parity, recipient-model contract tests, migration rehearsal, tenant-isolation tests, and outbox transition tests are committed.

- [x] **1.2 Create the atomic recipient channel materializer.**
  - Files: notification orchestrator/drafts, notification and email repositories, new Application service/contract, delivery-policy resolver, focused unit/integration tests.
  - Acceptance: one UoW operation creates the logical intent, all configured delivery rows, required/selected in-app notification, and eligible email outbox; no partial set can commit; a skipped channel still has a typed delivery outcome; no transport side effect runs.
  - Evidence: `RecipientNotificationMaterializer`, `IRecipientNotificationGraphRepository`, `NotificationIntentRepository`, policy resolver, DI registration, and focused materializer tests are committed.

- [x] **1.3 Implement exact unique-conflict recovery and provider-handoff settlement.**
  - Files: intent/delivery/email repository contracts and PostgreSQL implementations, UoW recovery coordination, focused fault/concurrency tests.
  - Acceptance: two workers for one occurrence/user produce one intent and one row per channel; exact conflict rolls back before fresh-transaction load/repair; unrelated DB errors throw; rollback after intent or email insertion commits nothing; SMTP acceptance followed by persistence uncertainty settles once as `Unknown` and never blind-resends.
  - Evidence: the materializer catches only `NotificationIntentDeduplicationConflictException` outside the failed UoW, reloads/repairs in a fresh UoW, and the drain/repository use the durable provider-handoff fence. Batch SQL is not needed before Task 3.4c proves a bottleneck.

- [x] **1.4 Centralize dispatch-time eligibility.**
  - Files: new eligibility evaluator, `EmailDispatchDrainService`, user/report/occurrence read contracts, delivery-state updates, infrastructure tests.
  - Acceptance: current tenant/user/email verification, changed address, deletion, optional preference, consent purpose, supersession, and required-policy behavior are rechecked before SMTP; old address is never used; every skip has a stable non-PII reason and updates email plus delivery state.
  - Evidence: current tenant/membership/address, managed-invitation authority, preference, supported-policy, superseded-delivery, typed skip, and provider-handoff checks are centralized in `EmailDispatchEligibilityEvaluator`; `RegistrationCancelled` and `RegistrationRevoked` now map to `event-updates` in both legacy preference and unsubscribe-footer switches with focused regression coverage. Case-update consent remains fail-closed until Task 5.1 supplies the new field; occurrence fences and rate controls stay in Tasks 3.5 and 1.6. The focused Infrastructure regression passed; PostgreSQL coverage builds but could not execute because Docker was unavailable.

- [x] **1.5 Add parent-aware email retention and content redaction.**
  - Files: retention settings/validator, cleanup repository/service/scheduler, email/attempt/receipt state, configuration/operations docs, focused persistence/infrastructure tests.
  - Acceptance: sent/ordinary-skipped content redacts after 180 days; unresolved failure material waits for replay or explicit resolve-without-replay; explicit resolution redacts immediately with a typed outcome; children follow parent; tenant deletion redacts and suppresses; cleanup is bounded/idempotent/dry-runnable; `ContentRedactedAt` permanently prevents claim/publish/replay.
  - Evidence: commit `9bfaf1e0` adds `EmailDispatchRetentionCleanupProcessor`, a scoped cleanup service, transactional parent/attempt/receipt/delivery redaction, immediate `Purged`-tenant suppression, explicit `operator_resolved_without_replay`, and migration `20260718203920_AddEmailDispatchContentRetention`. Focused Infrastructure tests were reported passing before interruption; Docker-backed PostgreSQL and full phase evidence remain open.

- [x] **1.6a Restore the SMTP capability boundary.**
  - Files: `IEmailService`, new narrow connection-test contract/query/handler, `SmtpEmailService`, `InstanceSettingsController`, `SmtpHealthCheck`, DI, `DurableSideEffectBoundaryTests`, focused application/architecture tests.
  - Acceptance: controllers and MediatR handlers have no `IEmailService`, MailKit, SMTP implementation, or direct-send dependency; SMTP connection testing flows through MediatR and an Application-owned capability; health uses the narrow diagnostic capability; the strict architecture guard passes.
  - Evidence: `IEmailService` is send-only; `IEmailConnectionTester` owns diagnostics; `TestInstanceSmtpConnectionQueryHandler` forwards through the narrow contract; `InstanceSettingsController` uses MediatR; `SmtpHealthCheck` uses the diagnostic contract; DI maps both interfaces to one scoped `SmtpEmailService`. Production files are committed in `9bfaf1e0`; the strict controller guard remains preserved in the main-checkout diff. Recorded checks: handler 2/2, handler boundary 1/1, controller boundary 1/1, SMTP configuration 3/3, and API Release build with zero errors. Mailpit was Docker-blocked and remains phase evidence, not Task 1.6a acceptance.
  - Effort: M. Dependencies: 1.5.

- [x] **1.6b Atomically claim fair work and coordinate optional-work backpressure.**
  - Files: pending-row repository contract/query, PostgreSQL fair-claim transaction/advisory-lock path, `EmailDispatchDrainService`, processor concurrency/backlog settings/validator/appsettings, focused persistence/infrastructure tests.
  - Acceptance: one atomic repository operation ranks and claims disjoint rows across replicas; tenant rounds prevent starvation; paused tenants are excluded; required work outranks optional non-reminder work, which outranks optional reminders; global/per-tenant active-processing ceilings are cross-instance authoritative; high/low-watermark hysteresis uses active core backlog excluding paused tenants and optional reminders so optional work cannot deadlock itself; required reminders, if policy ever marks one required, are never suppressed.
  - Evidence: `ClaimPendingBatchAsync` and `TryClaimSpecificAsync` replace selectable/row-by-row claim bypasses. `EmailDispatchOutboxRepository` uses one execution-strategy transaction plus transaction-scoped PostgreSQL advisory lock to persist the singleton `EmailDispatchProcessorState`, apply required/optional priority and tenant rounds, count committed global/per-tenant `Processing` capacity, atomically update claims, and recover same-token retries without another attempt increment. Batch and RabbitMQ single-row drainage use the same authority. Migration `20260718220406_AddEmailDispatchAtomicClaimState`, snapshot, DBML, explicit appsettings, bounded validator rules, in-memory fake, and focused tests are present in the main checkout.
  - Verification: independent review confirmed the implementation after fixing claimed-attempt off-by-one behavior and missing safe configuration maxima. Drain tests passed 28/28; processor-settings validator tests passed 22/22; Persistence, Infrastructure, and both affected test projects built with zero errors; EF reported no pending model changes; `git diff --check` passed. The exact PostgreSQL class selected 39 tests but executed zero bodies because Docker/Testcontainers had no daemon/socket and no Podman, nerdctl, finch, or local PostgreSQL server was available. Keep the Docker-backed concurrency/runtime evidence and Phase 1 verification open.
  - Effort: L. Dependencies: 1.6a.

- [x] **1.6c Persist cross-instance SMTP rate admission and correct pre-handoff recovery.**
  - Files: new persisted SMTP admission state/configuration/migration if repository evidence confirms it is required, outbox repository contracts/transitions, `EmailDispatchEligibilityEvaluator`, `EmailDispatchDrainService`, global/per-tenant rate settings/validator/appsettings/docs, focused persistence/infrastructure tests.
  - Acceptance: global and per-tenant token-bucket limits are shared across replicas; batch and `ProcessSingleAsync`/RabbitMQ/TickerQ use the same concurrency/rate admission path; rate deferral occurs before `AttemptCount` and `provider_handoff_started`, creates no attempt/receipt/provider evidence, and cannot dead-letter; cancellation before the fence safely releases or retry-defers the claim; stale unfenced processing becomes retryable while fenced uncertainty becomes `Unknown`; configuration has bounded defaults and upper limits.
  - Evidence: the process-local limiter and unfenced email repository mutators are removed. `EmailDispatchProcessorState` and `EmailDispatchTenantControl` persist global/per-tenant one-minute buckets; the serializable eligibility transaction uses PostgreSQL time, fixed global-then-tenant locking, bounded refill/clamp rules, and atomically either defers without attempt/receipt/provider evidence or consumes both tokens, increments `AttemptCount`, and writes the `provider_handoff_started` attempt/receipt fence. Batch and single-pointer paths share the claim/admission authority. Provider success/failure/reconciliation is exact tenant/outbox/lease/attempt fenced; pre-fence cancellation releases the exact claim; stale unfenced work becomes retryable while fenced/partial-fence work becomes `Unknown`. Retryable provider failure aligns outbox `RetryScheduled` with delivery `Queued`; exhausted work aligns both ledgers as `DeadLettered`.
  - Verification: independent review confirmed the implementation after two persisted-state fix loops. Drain tests passed 31/31; settings-validator tests passed 24/24; Application, Persistence, Infrastructure, and affected test projects built; EF reported no pending model changes; scoped diff checks passed; `.worktrees` is absent. The exact PostgreSQL class selected 42 tests but executed zero bodies because the unrelated untracked migration `20260718215537_BackfillUnclassifiedEventLocations` fails fixture migration with PostgreSQL `42703` (`is_deleted` missing). Keep that runtime evidence and Phase 1 verification open. Evidence: `/tmp/email-task-1.6c-rate-proof.txt` and `/tmp/email-task-1.6c-verifier.txt`.
  - Effort: L. Dependencies: 1.6b.

- [x] **1.6d Add bounded SMTP telemetry and health.**
  - Files: `BusinessMetrics`, `EmailDispatchHealthCheck`, repository aggregate queries, settings/validator, focused infrastructure/API tests.
  - Acceptance: oldest active pending age, due/retry/unknown/parked/dead-letter counts, typed skip outcomes, rate-deferral/backpressure outcomes, and bounded tenant backlog metrics are visible; paused rows do not make active readiness unhealthy; public health output exposes neither tenant identifiers nor address, subject, body, report evidence, event title, user ID, or provider ID.
  - Evidence: active health aggregates now exclude paused tenants while preserving future-retry context outside due/age decisions. Readiness exposes due/retry/stale/unknown/parked/dead-letter counts, oldest active due age, bounded rank-only tenant backlog samples, and persisted optional-reminder deferral; `Unknown` degrades and `Parked` remains informational. Provider attempts, eligibility/rate operational outcomes, RabbitMQ pointer outcomes, and current backpressure state use separate bounded instruments with closed vocabularies and no tenant/recipient/user/provider identifiers. Public health performs key-first redaction for numeric and string identifiers plus address, recipient, subject, body, report evidence, and event-title fields.
  - Verification: independent review confirmed the slice after fixing unbounded RabbitMQ labels, missing validator maxima, sampled-vs-current backpressure semantics, and stale canonical documentation. Application, Infrastructure, and ServiceDefaults Release builds passed with zero errors/warnings across the implementation/fix loops; scoped diff checks passed; `.worktrees` is absent. Focused test sources were added, but attempted test commands reached no discovery because unrelated shared-checkout compilation errors currently affect event-projection and ATProto test code. PostgreSQL runtime remains blocked by the separately recorded unrelated location migration; neither blocker is claimed as passing evidence.
  - Effort: M. Dependencies: 1.6c.

- [x] **1.6e Complete authenticated operator controls and runbooks.**
  - Files: email-dispatch admin CQRS/API/HAL, configuration/operations/self-hosting/troubleshooting docs, focused application/API tests.
  - Acceptance: existing tenant pause, park, replay, and resolve-without-replay remain server-authorized; global drain pause/rate override and unknown reconciliation are added only where absent; HAL exposes valid actions only; runbooks cover Mailpit/test SMTP, cleanup dry-run, compromised-tenant suppression, and tenant-vs-instance diagnosis.
  - Evidence: the singleton PostgreSQL processor state now owns instance-wide pause audit state and a bounded nullable global SMTP rate override. Instance-setting-authorized CQRS/API/HAL exposes sanitized control state plus valid pause/resume and set/clear-rate actions. Batch and specific claims, RabbitMQ pointer selection, and the final pre-handoff eligibility transaction all observe the global pause; paused work consumes neither attempts nor rate tokens, while stale recovery remains independent. `Unknown` work uses an explicit tenant-scoped `Delivered`/`NotDelivered` reconciliation that atomically aligns outbox, attempt, receipt, and email-delivery ledgers. Generic replay excludes `Unknown`, realigns replayable linked ledgers, and HAL no longer advertises invalid park/replay transitions. Mailpit/test-SMTP separation, retention dry-run, compromised-tenant suppression, global controls, reconciliation triage, and tenant-vs-instance diagnosis are documented.
  - Verification: independent read-only review confirmed authorization boundaries, durable pause/rate admission, reconciliation and replay state transitions, HAL state guards, OpenAPI/source-generation registration, health sanitization, migration ordering/reversibility, and runbook coverage. Domain, Application, Persistence, and Infrastructure Release source builds passed with zero warnings/errors; API passed with zero errors and existing shared-checkout warnings. Scoped `git diff --check` passed and `.worktrees` is absent. No tests or EF mutation commands were run, honoring the user's stop-tests instruction; Phase 1 runtime/Mailpit evidence remains open.
  - Effort: M. Dependencies: 1.6d.

### Phase 1 Verification — RUN ONCE AFTER TASKS 1.1–1.6e

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build`

## Phase 2 — Registration Transitions

- [x] **2.1 Move registration creation transaction ownership to Application/UoW.**
  - Files: `IUnitOfWork` isolation contract, `EfCoreUnitOfWork`, `IEventRegistrationIntentRepository`, `EventRegistrationIntentRepository`, `CreateEventRegistrationCommandHandler`, focused tests.
  - Acceptance: Application requests a serializable transaction; repository performs capacity/parent/child persistence without `BeginTransaction` or commit and no longer accepts email/notification entities; registration, integration-sync intent, and notification channels commit together.
  - Evidence: `CreateEventRegistrationCommandHandler` uses `ExecuteSerializableAsync`; repository-owned transaction parameters were removed; architecture and handler tests cover the boundary.

- [x] **2.2 Move capacity-aware registration updates to Application/UoW.**
  - Files: registration repository contract/implementation, update handler, transition result, focused concurrency tests.
  - Acceptance: repository update has no internal retry transaction; explicit result contains `Changed`, `ParentIntentId`, `PreviousStatus`, `FinalStatus`, `TransitionReason`, and pre-generated `OccurrenceId`; capacity and parent recomputation remain serializable and atomic.
  - Evidence: `EventRegistrationTransitionResult`, `UpdateEventRegistrationCommandHandler`, and repository concurrency/coverage tests are committed.

- [x] **2.3 Move registration cancellation transaction ownership to Application/UoW.**
  - Files: cancellation repository method/handler, transition result/provenance, focused capacity/rollback tests.
  - Acceptance: cancellation/release/recompute uses the Application-owned serializable boundary; parent/child soft-delete semantics and capacity release remain correct; actor provenance distinguishes self-cancel from organizer/system revoke without repository notification logic.
  - Evidence: `DeleteEventRegistrationCommandHandler` owns the serializable UoW and persists attendee/organizer/system provenance in the transition result.

- [x] **2.4 Migrate receipt, approval, rejection, and waitlist-promotion delivery.**
  - Files: registration handlers/services/factory and tests.
  - Acceptance: registration receipt preserves current behavior through the new primitive; final parent transitions create required in-app plus optional email exactly once; only an actual `Waitlisted -> Approved` sends promotion copy; multi-session child changes cannot announce contradictory status.
  - Evidence: `RegistrationNotificationDeliveryService` and create/update handlers select lifecycle templates from the finalized parent transition and materialize them inside the registration UoW.

- [x] **2.5 Add cancellation/revocation notification with safe provenance.**
  - Files: update/delete handlers, registration templates/policies, focused tests.
  - Acceptance: self-cancel and organizer/system revoke select safe distinct copy only from persisted actor provenance; one intent follows one changed parent transition; no-op/replay creates nothing; missing/unverified email leaves required in-app and typed skipped email delivery.
  - Evidence: cancellation and revocation templates/kinds, delete/update handler integration, focused registration notification tests, and Task 1.4 preference/unsubscribe category mapping are committed.

Phase 2 implementation is complete. Historical post-migration build and full Domain, Application, Persistence, and Architecture suites were green; no command was rerun for this documentation re-baseline.

### Phase 2 Verification — HISTORICAL EVIDENCE RECORDED; CURRENT GATE NOT RE-RUN

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build`

## Phase 3 — Generic Fanout Engine

- [x] **3.1 Persist immutable fanout occurrences and pointer serialization.**
  - Files: `NotificationFanoutOccurrence` entity/config/repository/migration, pointer contract/factory, JSON source generation, domain/application/persistence tests.
  - Acceptance: all required occurrence fields from the plan persist; ID exists before the retry delegate; mutation and one PII-free pointer can commit together; snapshots are immutable except explicit supersession metadata; fanout intent has an occurrence FK and unique `(TenantId, FanoutOccurrenceId, UserId)` guard.
  - Evidence: migrations `20260717160935_AddNotificationFanoutOccurrences` and `20260717165523_AddNotificationFanoutAudienceExecution`, occurrence repository, pointer factory/source generation, and migration/repository tests are committed. Dispatcher/worker ownership moved to Tasks 3.4a–3.4c.

- [x] **3.2 Implement deterministic event/session audience queries.**
  - Files: registration intent repository contract/implementation, compound cursor model, persistence tests.
  - Acceptance: cutoff/current-status predicates are exact; both parent creation and immutable child `CoverageEstablishedAt` are at/before cutoff; same-scope atomic replacement inherits the old coverage timestamp while a newly added/moved session receives a new timestamp; event-wide and target-session cohorts follow the plan; whole-event/day/session scope, partially cancelled children, pending/waitlisted/approved parents, registrations after cutoff, moved/replaced children, and duplicate children are covered; cursor is `(FirstEligibleRegistrationCreatedAt, UserId)`.
  - Evidence: `EventRegistrationIntentRepository`, coverage replacement logic, audience migration, and focused PostgreSQL cohort tests are committed.

- [x] **3.3 Add lease, checkpoint, and stale-run recovery.**
  - Files: `NotificationFanoutRun` entity/config/repository/service, scheduler/worker tests.
  - Acceptance: occurrence has at most one active lease; concurrent claims are safe; checkpoint stores compound cursor/counts; crash may replay but not skip the last page; lease expiry resumes; stale cursor replay remains idempotent.
  - Evidence: `NotificationFanoutRunRepository` implements advisory-lock claim creation, renewable token/fence/generation leases, monotonic compound checkpoints, completion, and expiry recovery with focused PostgreSQL tests.

- [x] **3.4a Route fanout pointers and ensure durable runs.**
  - Files: `CompositeOutboxMessageDispatcher`, occurrence pointer factory/repository, fanout-run repository, new Application worker, focused application/persistence tests.
  - Acceptance: the PII-free pointer is version-validated, tenant/occurrence is authoritatively loaded, and one pending run is created idempotently; the general outbox pointer completes after run creation and never holds its lease through recipient fanout.
  - Evidence: `NotificationFanoutOccurrenceHandoffService` strictly validates the v1 PII-free pointer and full outbox envelope, exact-loads tenant/occurrence authority, treats superseded work as a terminal no-op, and delegates only durable run creation. `EnsurePendingOccurrenceRunAsync` uses the existing per-occurrence PostgreSQL advisory lock, revalidates the pending occurrence plus tenant-bound event actor, returns an existing run in any durable state, or creates one independent-ID pending run with no lease under the existing unique occurrence constraint. `CompositeOutboxMessageDispatcher` returns immediately after that ensure, so `OutboxProcessor` acknowledges its own pointer without holding recipient-work state. The legacy claim path remains lazy-create compatible and now uses pre-generated independent run/concurrency/lease identifiers with same-token ambiguous-commit recovery.
  - Verification: independent review confirmed the slice after two fixes: strict unknown JSON member rejection and clean EF execution-strategy retry state. Failed attempts clear tracked state only after transaction disposal; a retry that observes its own committed owner/token returns the same claim without advancing generation/fence. Application, Persistence, and Infrastructure Release source builds passed with zero errors; scoped `git diff --check` passed; `.worktrees` is absent. Focused test sources cover handoff validation, dispatcher routing, concurrent ensure/replay, unleased pending state, and later claim, but tests were not executed per user instruction.
  - Effort: M. Dependencies: 1.6e, 3.3.

- [x] **3.4b Build typed recipient materialization from immutable occurrences.**
  - Files: `NotificationIntentDraft`, `RecipientNotificationMaterializer`, new typed template/version factory, location-disclosure integration, focused application/persistence tests.
  - Acceptance: `FanoutOccurrenceId` is persisted; one occurrence/user creates one logical intent and configured channels; current verified address is used; location authorization is recipient-specific but selects only immutable snapshot values; unknown template/version fails closed.
  - Evidence: `NotificationFanoutRecipientTemplateFactory` strictly accepts only v1 `event.cancelled`, `event.updated`, `event.session.cancelled`, and `event.session.updated` occurrences under `CriticalEventUpdateOptional`; unknown keys, versions, policies, scopes, JSON members, malformed payloads, and unsupported change fields fail closed. `NotificationFanoutRecipientMaterializationService` resolves the current persisted verified `User` address and email preference, creates required in-app plus optional email-or-typed-skip delivery, and delegates one occurrence/user graph to the atomic materializer. `NotificationIntentDraft.FanoutOccurrenceId` now reaches the intent FK, and exact PostgreSQL recovery recognizes both tenant/deduplication and tenant/occurrence/recipient unique constraints in a fresh UoW.
  - Verification: independent static review found and closed two issues before confirmation: the value-free location mask now binds tenant, event, recipient, EventLocation, and room before selecting immutable snapshot values, and title-only changes were removed from the closed v1 material-change vocabulary with both string and former numeric inputs rejected. Domain, Application, Persistence, and Infrastructure Release source builds passed with zero errors across the implementation/fix loop; scoped diff checks passed and `.worktrees` is absent. Focused test sources were added but no tests or test-project builds were run per user instruction, so Phase 3 runtime evidence remains open.
  - Effort: L. Dependencies: 3.4a.

- [x] **3.4c Process pages with crash-safe checkpoint ordering.**
  - Files: fanout worker, audience query/run repositories, lease/checkpoint coordination, focused application/persistence concurrency tests.
  - Acceptance: every recipient outcome commits before the page checkpoint; a crash before checkpoint safely replays through exact dedup; stale lease/fence stops work; a partially processed page yields no duplicates or skipped recipients. Add batch SQL only if measured bounded-worker evidence justifies it.
  - Evidence: `NotificationFanoutPageProcessor` accepts only an already-fenced claim, exact-loads and validates the pending immutable occurrence/template, renews the claim at each page and recipient boundary, validates bounded unique compound `(FirstEligibleRegistrationCreatedAt, UserId)` ordering, and materializes recipients sequentially through the existing atomic graph service. It advances the durable cursor/counts only after the full page commits and fenced-completes only after an empty page; exceptions, cancellation, stale renewal, and failed checkpoint never advance the page. `TryRenewClaimAsync` is now exact-fence idempotent and uses a non-shortening lease horizon.
  - Verification: independent static review confirmed recipient-commit-before-checkpoint ordering, exact occurrence/user dedup replay, tenant/token/fence/generation/status/lease rejection, PostgreSQL/.NET UUID cursor-order compatibility, correct replay count semantics, required in-app authority binding, early corrupt-template rejection, and absence of coalescing/selection/host/telemetry/batch-SQL scope creep. Three NSubstitute expression-tree predicates found during review were repaired to compatible `== null` and `HasValue/Value` forms. Application and Persistence Release source builds passed with zero errors before the focused source repair; scoped diff checks passed and `.worktrees` is absent. Focused test sources exist but no tests or test-project builds were run per user instruction, so runtime PostgreSQL/concurrency evidence remains an open Phase 3 gate.
  - Effort: L. Dependencies: 3.4b.

- [x] **3.5a Coordinate occurrence precedence, coalescing, and supersession.**
  - Files: new Application coordinator, occurrence repository conditional transitions, existing occurrence state fields, focused application/persistence tests.
  - Acceptance: heavy moderation > event cancellation > session cancellation > important update > reminder; cancellation/heavy work is immediate; five-minute important-update replacement retains earliest before/latest after; concurrent edits produce one active winner.
  - Evidence: `NotificationFanoutOccurrenceCoordinator` runs only inside the caller's PostgreSQL UoW, acquires the tenant-wide source-identity advisory lock before the tenant/event precedence lock, proves exact session/event authority, normalizes retry-stable timestamps to PostgreSQL microseconds, persists one winner before conditional loser supersession, and writes one stable PII-free pointer last. Exact replay validates immutable input plus every supersession hop; five-minute important-update replacement keeps the earliest safe before snapshot, latest safe after snapshot/cutoff, unioned strict change fields, and a sliding quiet window.
  - Verification: independent static review confirmed the fixed precedence/scope matrix, unrelated-session coexistence, immediate cancellation/heavy timing, deterministic update ordering, tenant-wide replay serialization across different events, fail-closed session binding and replay chains, transaction/FK ordering, and absence of provider-handoff suppression, trigger, worker, rendering, telemetry, migration, or reminder-rescheduling scope creep. Application and Persistence Release product builds passed with zero errors after the correction loop; scoped diff checks passed and `.worktrees` is absent. Focused Application/PostgreSQL test sources exist, but no tests or test-project builds were run per user instruction, so runtime concurrency evidence remains an open Phase 3 gate. Reminder replacement remains explicitly owned by Tasks 7.2–7.3.
  - Effort: L. Dependencies: 3.4c.

- [x] **3.5b Suppress materialized work at the provider-handoff fence.**
  - Files: notification delivery/email suppression repository operations, `EmailDispatchEligibilityEvaluator`, scheduler integration, focused persistence/infrastructure tests.
  - Acceptance: unsent pre-handoff rows become typed skipped/superseded; sent, unknown, and post-handoff evidence remains immutable; event/session precedence suppresses the correct scope; a race cannot escape after authoritative occurrence recheck.
  - Evidence: `NotificationFanoutOccurrenceCoordinator` invokes a PostgreSQL suppression repository only after a conditional supersession succeeds and while holding the shared event-precedence advisory lock. One bounded tenant/occurrence-linked SQL transition supersedes pending/queued email delivery and skips only pending, retry-scheduled, or unfenced-processing SMTP work; sent, unknown, dead-lettered, parked, already skipped, and provider-handoff-fenced processing evidence remains unchanged. `EmailDispatchEligibilityEvaluator` uses `READ COMMITTED`, acquires the same event lock before the outbox row lock, reloads occurrence authority, and fails closed on missing, mismatched, or superseded occurrence state before creating provider-handoff evidence.
  - Verification: independent static review confirmed exact tenant/occurrence linkage, state/fence predicates, typed and idempotent suppression, session scope, lock identity/order, final dispatch race closure, pause/rate revalidation, DI, index suitability, and focused test-source compile hazards. Application, Persistence, and Infrastructure Release product builds passed with zero errors during implementation. Scoped diff checks passed and `.worktrees` is absent. No tests or test-project builds were run per user instruction; the true two-transaction PostgreSQL provider-fence-versus-supersession race remains open Phase 3 runtime evidence.
  - Effort: M. Dependencies: 3.5a.

- [x] **3.6a Add fair runnable selection and cross-instance tenant claim limits.**
  - Files: occurrence/run repositories, claim query/index migration only if proven necessary, scheduler tests.
  - Acceptance: one due occurrence per tenant per round; deterministic priority/time/ID order; tenant advisory lock enforces the configured active-claim ceiling across replicas; expired leases resume from the compound cursor.
  - Evidence: `ClaimDueRoundAsync` performs one bounded parameterized PostgreSQL ranking over existing handoff-created occurrence runs, chooses one due pending/expired candidate per tenant by priority descending then occurrence time/UUID, and applies the same global order. Exact claims require a caller-supplied tenant ceiling, acquire tenant-claim then shared event-precedence then occurrence advisory locks, freshly revalidate occurrence/run authority, reject missing/failed/completed/active work, and preserve cursor/counts while advancing token/fence/generation on expired recovery. The literal pending-state predicate is supported by one partial global runnable index; its explicit migration is reversible.
  - Verification: independent static review confirmed existing-run-only selection, tenant/global deterministic ranking, mandatory cross-replica ceiling, stable ambiguous-commit token recovery, supersession-race closure under the shared event lock, tenant isolation, connection/parameter handling, lock-cycle safety, and config/migration/snapshot parity. The accidentally emptied shared model snapshot was restored mechanically from the latest full location/ATProto/email target model before adding only the fanout index metadata. Application and Persistence Release product builds passed after the fix loop; Persistence finished with 0 errors/0 warnings. Scoped diff checks passed and `.worktrees` is absent. Focused PostgreSQL source includes the event-lock race, but no tests or test-project builds were run per user instruction, so live fairness/index/concurrency evidence remains open.
  - Effort: L. Dependencies: 3.5b.

- [x] **3.6b Add fanout processor settings, backpressure, telemetry, and health.**
  - Files: new processor/settings/validator/hosted service, `BusinessMetrics`, health check, operations/configuration docs, focused infrastructure tests.
  - Acceptance: each claim runs in a fresh scope; global/per-tenant work is bounded; optional reminders are suppressed above backlog threshold while required work proceeds; oldest age, processed/remaining, lease contention, supersession, and backpressure are observable without PII.
  - Evidence: the API-hosted `NotificationFanoutProcessor` claims a bounded fair round and resolves every `NotificationFanoutPageProcessor` from its own asynchronous DI scope. Repository-enforced global/per-tenant active ceilings remain authoritative across replicas. A persisted high/low-watermark state defers only `ReminderOptional` occurrences; selection, exact admission, and occurrence-run producers share the global advisory lock, closing selection-to-claim and producer-to-claim races while preserving the global→tenant→event→occurrence lock order. Handoff always persists the run, so deferred reminders remain durable rather than disappearing. Aggregate metrics and health expose due/core/optional/active/expired/superseded/processed/remaining/oldest-age, contention, capacity deferral, and unavailable outcomes without tenant, event, consumer, or recipient labels.
  - Verification: independent static review found and then confirmed closure of the two reminder-backpressure races and found the page-result unavailable accounting gap, which was corrected in both result and metrics. All exact-claim contract callers were updated. The Release API product build passed across 8 projects with 0 errors; configuration JSON and scoped/all-tree diff checks passed; `.worktrees` is absent. Focused settings, fresh-scope, aggregate-health, cross-replica ceiling, and durable hysteresis test source is present, but no tests or test-project builds were run per user instruction. Phase 3 runtime evidence remains open.
  - Effort: M. Dependencies: 3.6a.

### Phase 3 Verification — RUN ONCE AFTER TASKS 3.1–3.6b

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build`

## Phase 4 — Event and Session Triggers

- [x] **4.1 Wire whole-event cancellation first.**
  - Files: `CancelEventCommandHandler`, occurrence factory/repository, cancellation templates, focused application/API tests.
  - Acceptance: state and immediate occurrence pointer commit together; audience is event-wide live-at-cutoff; repeated/already-cancelled paths create nothing; event cancellation suppresses session updates and reminders; cache invalidation is post-commit.
  - Evidence: `CancelEventCommandHandler` pre-generates the occurrence, pointer, federation IDs, and one controlled UTC timestamp before its retryable UoW. The event status write, ATProto plan, strict event-wide cancellation occurrence, lower-precedence supersession/email suppression, and pointer all persist inside that transaction. The candidate has no session scope, freezes the audience cutoff at the transition time, uses the expected pre-transition concurrency token as its stable source version, and carries only the immutable event title with no location or link. Failure and already-cancelled paths return before coordination; cache eviction runs only after successful UoW completion.
  - Verification: independent static review confirmed transaction membership, retry-stable identities, DI availability, event-wide precedence over session cancellation/update/reminder work, dispatch-race coverage through the authoritative occurrence fence, and privacy-safe copy. The Application Release product build passed 2 projects with 0 errors; diff checks passed and `.worktrees` is absent. Focused test source records occurrence/pointer creation before commit, post-commit cache observation, immutable cutoff/source data, and no work/cache for already-cancelled input. No tests or test-project builds were run per user instruction; PostgreSQL rollback/concurrency remains phase-gate evidence.

- [x] **4.2 Wire published-session cancellation.**
  - Files: `EventSessionLifecycleTransitionCommandHandlerBase`, occurrence/templates, focused tests.
  - Acceptance: only attendees covering the target session are selected; state and immediate pointer are atomic; unrelated sessions/users are excluded; target updates/reminders are suppressed; repeated transitions create nothing.
  - Evidence: the shared lifecycle base creates a transition attempt before its retryable UoW, performs tenant/parent/concurrency validation, persists the status and recalculated parent schedule, invokes a cancellation-specific in-transaction hook, and evicts caches only after success. `CancelEventSessionCommandHandler` supplies one controlled UTC time plus stable occurrence/pointer IDs and emits only when the previous status was Published. Its strict session-scoped candidate freezes event/session title, start/end, and timezone with no location/link, uses the expected pre-transition concurrency token as source version, and is coordinated atomically with the transition. Existing coordinator/audience rules suppress only same-session updates/reminders and select whole-event/day/explicit registrants covering that session; unrelated sessions remain independent.
  - Verification: independent static review confirmed stable retry inputs, published-only emission, exact tenant/event/session authority, session-local precedence, live-child audience coverage, failure/replay no-op behavior, privacy-safe snapshots, DI, and post-commit cache order. The Application Release product build passed 2 projects with 0 errors/0 warnings; diff checks passed and `.worktrees` is absent. Focused test source covers published creation/snapshot/atomic ordering, non-published silence, and already-cancelled no work. No tests or test-project builds were run per user instruction; PostgreSQL rollback/concurrency and runtime audience evidence remain open.

- [x] **4.3 Wire material time, location, and room changes.**
  - Files: `UpdateEventSessionCommandHandler`, immutable change-set factory, disclosure/template logic, focused tests.
  - Acceptance: draft, cosmetic, and no-op edits create nothing; published material changes persist exact safe before/after data; multiple changed fields produce one coalesced occurrence; exact location appears only for currently authorized recipients.
  - Evidence: `UpdateEventSessionCommandHandler` creates one controlled UTC timestamp plus retry-stable occurrence/pointer IDs before its serializable UoW, then reloads and validates authoritative session and parent state inside every retry attempt. After the guarded mutation, it classifies only start, end, EventLocation, and room differences, freezes tenant-checked before/after snapshots, and coordinates one session-scoped `event.session.updated` occurrence/pointer in the same transaction. The existing coordinator provides five-minute coalescing, precedence, and the recipient materializer's current location-disclosure authorization. Unpublished and non-material edits remain silent; ambiguous published cross-parent moves are rejected before mutation until a dual-audience policy exists; cache invalidation remains post-commit.
  - Verification: independent adversarial review initially found and then confirmed fixes for cross-parent audience loss and mutable tracked-entity reuse across execution-strategy retries. The Application Release product build passed 2 projects with 0 errors/0 warnings; scoped diff checks passed and `.worktrees` is absent. Focused test source covers immutable published schedule changes, draft silence, published cross-parent rejection, and a simulated transient retry with a fresh authoritative entity. No tests or test-project builds were run per user instruction; real PostgreSQL retry/commit-ambiguity, coalescing, disclosure-denial, and cache-order evidence remain phase-gate work.

- [x] **4.4 Wire published session scheduling/rescheduling.**
  - Files: `ScheduleEventSessionCommandHandler`, occurrence/coalescing service, cache sequencing, focused tests.
  - Acceptance: schedule mutation and immutable occurrence pointer are atomic; a 10:00 -> 11:00 occurrence cannot later become 10:00 -> 12:00 by rereading state; newer unsent schedules supersede/coalesce correctly; caches invalidate after commit.
  - Evidence: `ScheduleEventSessionCommandHandler` creates one UTC timestamp and retry-stable occurrence/pointer IDs before its serializable UoW, then reloads and validates authoritative session and parent state inside every retry attempt. It captures the exact prior title/start/end/status/timezone, applies projection and day assignment, persists the guarded session and parent schedule summary, and coordinates one session-scoped immutable occurrence/pointer in that transaction only when a previously Published session materially changes start or end. Draft/Approved initial scheduling and Published no-ops remain silent. The existing coordinator remains the sole five-minute coalescing, newer-update supersession, and cancellation-precedence authority; caches invalidate only after success.
  - Verification: independent adversarial review confirmed retry freshness, one coordinator path, exact 10:00 -> 11:00 snapshot immutability, initial-schedule/no-op silence, tenant/concurrency/readiness guards, transaction membership, and post-commit cache order. The Application Release product build passed 2 projects with 0 errors; scoped diff checks passed and `.worktrees` is absent. Focused test source covers Draft silence, Published immutable rescheduling and one pointer, Published no-op silence, post-commit cache observation, and transient retry using a fresh authoritative entity. No tests or test-project builds were run per user instruction; real PostgreSQL retry/unknown-commit behavior and coordinator concurrency remain phase-gate evidence.

- [x] **4.5 Wire event-timezone reprojection.**
  - Files: `UpdateEventCommandHandler.ApplyTimezone`, occurrence snapshots/templates, focused application/API tests.
  - Acceptance: only a published timezone change that changes displayed session times creates one event-wide occurrence; snapshot identifies affected sessions without mutable reconstruction; session/cancellation precedence applies; DST gap/overlap cases are retained for Phase 7 reminder proof.
  - Evidence: `UpdateEventCommandHandler` now creates retry-stable time/federation/occurrence/pointer IDs before its serializable UoW, reloads the authoritative schedule graph and validates concurrency inside every attempt, and commits timezone reprojection, event/session persistence, ATProto planning, and one event-wide occurrence/pointer together. Published/material-display gating freezes each affected session's exact offset-bearing before/after interval in an optional backward-compatible v1 projection array; delayed rendering is linkless and snapshot-only. Canonical ID ordering, strict complete-or-legacy validation, deterministic changing-cohort coalescing/replay, valid open-ended sessions, legacy/enriched non-coalescing boundaries, and narrow whole-occurrence suppression for included-session cancellation preserve privacy, idempotency, and precedence. Cache eviction remains post-commit; reminders remain disabled.
  - Verification: independent adversarial review found and confirmed fixes for ordered-array coupling, changing-cohort replay, session-cancellation scope, mixed-version compatibility, half-enriched payloads, predecessor continuity, open-ended intervals, and first-run/replay symmetry. The Application Release product build passed 2 projects with 0 errors/0 warnings; scoped diff checks passed and `.worktrees` is absent. Focused source covers Brussels DST projection, immutable event-wide metadata, changing cohorts and replay, both cancellation arrival orders plus unrelated sessions, legacy/enriched boundaries, malformed/half-enriched data, discontinuity boundaries, tampered replay, canonical ordering, and open-ended rendering. No tests or test-project builds were run per user instruction; real PostgreSQL retry/unknown-commit, DST runtime, and phase-level integration evidence remain open.

### Phase 4 Verification — RUN ONCE AFTER TASKS 4.1–4.5

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build`

## Phase 5 — Reporting Email and Provider Convergence

- [x] **5.1 Split reporter consent in the domain and database.**
  - Files: `EventReport`, EF configuration, new migration/backfill, model snapshot, domain/persistence tests.
  - Acceptance: `ReportCaseUpdatesConsent` and `ReportFollowUpContactConsent` replace the old field; legacy true maps only to follow-up consent because that is what the old wording authorized; case-update consent backfills false; aggregate method changes consent with audit/concurrency semantics; populated Up -> Down -> Up reconstructs the old field only from follow-up consent and never widens case-update authority.
  - Evidence: `EventReport` now requires two explicit private-set consent authorities and exposes an idempotent `ChangeReporterCommunicationConsent` mutation that updates `UpdatedAt` only on change; the existing EF SaveChanges interceptor remains the canonical audit-user and concurrency-stamp seam. Temporary legacy submission/read mappings are deliberately narrow: the clarification checkbox maps only to follow-up consent and case updates are always false. Dispatch eligibility now tenant/reporter-binds the current report and checks the exact case-update or follow-up purpose flag without enabling any report email flow.
  - Verification: focused migration `20260719210000_SplitEventReportConsent` renames the legacy column to follow-up, adds/backfills case-update false, removes the temporary DB default so future writes are explicit, and reverses by dropping case-update then renaming follow-up back. Independent review confirmed false-safe Up/Down/Up semantics, EF configuration/snapshot/DBML alignment, aggregate audit/concurrency behavior, explicit factory callers, privacy-safe compatibility mappings, and fail-closed dispatch selection. Domain, Application, and Persistence Release product builds passed 1/2/4 projects respectively with 0 errors/0 warnings; diff checks passed and `.worktrees` is absent. No tests or test-project builds were run; PostgreSQL migration execution and dispatch-withdrawal runtime evidence remain open.

- [x] **5.2 Change reporter read/write and OpenAPI contracts.**
  - Files: submission/my-report/moderation DTOs, commands/validators/mappers, OpenAPI changelog, regenerated NSwag client, application/API/client contract tests.
  - Acceptance: old property is absent; both consent purposes are explicit; generated client is regenerated, not hand-edited; breaking change is documented; evidence and moderator/provider internals remain excluded.
  - Evidence: submission, My Reports, moderation queue, and moderation detail contracts now expose required `ReportCaseUpdatesConsent` and `ReportFollowUpContactConsent` booleans with no current legacy alias. Authenticated submission persists both independently; anonymous intake forces both false; read mappers copy the exact corresponding aggregate flags. Controllers remain thin MediatR boundaries and validators remain manually instantiated. The clarification-only dialog bridge temporarily sends case updates false and follow-up from the existing checkbox; moderation detail renders both values without expanding evidence/provider/private data.
  - Verification: the product API build and canonical `GenerateApiClient` target regenerated checked OpenAPI and `EventApiClient.g.cs`; all direct/HAL schemas require non-null booleans and generated models use non-nullable `bool`. `docs/API.md` and `docs/API_CHANGELOG.md` record the deliberate pre-1.0 break. Independent review confirmed old-field absence, anonymous false-safe behavior, exact read mapping, HAL/schema/client alignment, privacy boundaries, controller/validator conventions, and the temporary follow-up-only bridge. API/Application/Blazor Client Release product builds passed 8/2/1 projects with 0 errors/0 warnings; diff checks passed and `.worktrees` is absent. Canonical generation also reconciled two stale delegation fields to their already-dirty source DTO; no unrelated source was changed. No tests or test-project builds were run.

- [x] **5.3 Add two explicit choices to report submission UI.**
  - Files: `ReportEventDialog.razor`, code-behind/CSS, reporting service, accessibility/client tests.
  - Acceptance: independently labelled unchecked controls distinguish case updates from clarification contact; submitted DTO carries both; labels/focus/announcements are accessible; no consent is inferred from submission itself.
  - Evidence: `ReportEventDialog` now renders an optional `Email preferences` fieldset with independently bound, default-false case-update and follow-up checkboxes, distinct labels/descriptions, and unique `aria-describedby` targets. The generated submission DTO receives both booleans exactly. Validation/service failures use one visible `role="alert"` assertive owner; success retains the existing polite announcer; the HAL-gated caller's focus save/restore remains unchanged. Isolated BEM styling uses project tokens, logical properties, and a narrow wrapper-anchored `::deep` selector for MudText-rendered descriptions.
  - Verification: independent source review confirmed default-unchecked semantics, all four payload combinations, semantic fieldset/labels/descriptions, one assertive error path, polite success, focus/HAL preservation, CSS-isolation correctness, and no raw/global/physical-direction styling. The Blazor Client Release product build passed; scoped diff checks passed and `.worktrees` is absent. Focused bUnit/accessibility source covers labels, descriptions, unique IDs, payload combinations, and the single alert path. No tests or test-project builds were run. Fresh browser capture at 375/768/1280 remains environment-blocked: normal and runtime-isolated Aspire runs reuse fixed Postgres/RabbitMQ volumes with stale credentials, leaving API/Blazor Waiting; both AppHosts/browser sessions were cleaned up and no volume was reset.

- [x] **5.4 Add reporter-owned consent withdrawal endpoint and HAL affordance.**
  - Files: command/validator/handler, controller route/response metadata/rate limit, route names, detail and collection HAL policies, API tests.
  - Acceptance: authorized owner can update both consents; non-owner/tenant mismatch fails closed; UoW persists audit/concurrency; both My Reports representations emit `update-communication-consent` only when allowed; controller only dispatches MediatR and assembles HAL.
  - Evidence: the authenticated, write-rate-limited `PUT /api/event-reports/my/{reportId}/communication-consent` accepts two required booleans, dispatches a manually validated MediatR command, refreshes the owner-scoped report, and returns its HAL resource. The handler derives tenant/current internal user server-side, applies the same provider `User.Update` decision used by both HAL policies before opening the UoW, then tenant/owner-binds the tracked report. Changed values use the aggregate mutation and EF audit/concurrency rotation; unchanged values skip update/cache churn; changed cache eviction completes after commit and before controller refresh. Missing/wrong-tenant/non-owner collapse to generic 404 and provider denial to generic 403.
  - Verification: both My Reports detail and collection policies emit `update-communication-consent` with the exact named-route report ID only after canonical self-update authorization; missing identity/provider failure omits it and moderation resources never receive it. Independent review initially found and then confirmed the direct-write/provider parity fix occurs before transactional work. OpenAPI documents 403 and the explicit request body; canonical NSwag generation added the HAL-returning client method. Application/API/Blazor Client product builds and generation passed; final Application build had 0 errors/0 warnings and API had 0 errors with pre-existing warnings. Diff checks passed and `.worktrees` is absent. No tests or test-project builds were run.

- [x] **5.5 Add HAL-gated withdrawal controls to My Reports.**
  - Files: My Reports page/detail UI, reporting client service, generated-client use, CSS/accessibility/client tests.
  - Acceptance: UI renders/edit controls only when the HAL relation exists; withdrawal immediately refreshes resource state; no local role/claim gating; failures are accessible and do not falsely update the screen.
  - Evidence: `MyReportsPage` always renders both authoritative consent values and exposes per-report Edit/Save/Cancel only from that resource's `update-communication-consent` relation. Independent draft/submitting/error state is keyed by report ID; unchanged or in-flight saves are disabled; cancel restores resource values. `EventReportingService` requires the exact case-sensitive `PUT` relation and exact canonical consent path/report ID before generated-client dispatch, rejects malformed/extra/non-HTTP targets and mismatched returned IDs, and passes the returned HAL resource through unchanged. Success replaces only that collection item, announces once politely, and focuses an always-present summary even if the returned resource removes Edit; failure preserves authoritative state plus retryable draft and owns one row-local alert.
  - Verification: independent source review found and confirmed fixes for permissive GUID-segment HAL validation, vanished post-save focus target, direct Mud/raw typography values, and case-insensitive method acceptance. CSS remains isolated BEM with semantic tokens/logical properties and no claim/role gating. Focused source covers HAL absence/method/ID/path attacks, valid URI variants, initialization, payload, authoritative replacement/link preservation, returned-ID mismatch, failure isolation/retry, cancel, pending isolation, announcements, and focus fallback. Blazor Client Release product builds passed with 0 errors; final build warnings were shared-checkout warnings. Diff checks passed and `.worktrees` is absent. No tests, test-project builds, app startup, or fresh visual capture ran; the recorded fixed-volume Aspire blocker remains.

- [x] **5.6 Queue report receipt using the canonical SLA.**
  - Files: report email factory/templates, `SubmitEventReportCommandHandler`, policy/materializer integration, configuration/docs/tests.
  - Acceptance: report/case/provider-sync/in-app/email state commit atomically; case-update consent gates email; current verified address is required; copy thanks the reporter, derives the review window only from bounded `CaseSlaHours` (default 48), snapshots that resolved value/template so later configuration changes cannot rewrite queued copy, and excludes evidence/fingerprints; no consent or email records a typed skipped channel, not a second fallback intent.
  - Evidence: authenticated submission now pre-resolves the persisted reporter, current verified address, and reachable `trust-safety` email preference, pre-generates retry-stable IDs/time, and commits the report/target/encrypted evidence/case/provider-sync pointer plus one receipt intent and all channel state in one PostgreSQL UoW. `ReportReceiptNotificationFactory` snapshots `report.receipt` v1, `REPORT_CASE_UPDATE` v1, the bounded canonical SLA, constant linkless thank-you copy, required in-app delivery, and either optional `ReportReceipt` SMTP work or one typed skipped email channel. Anonymous intake creates no recipient graph; persisted reporters fail closed on deleted/missing membership, missing/unverified address, withdrawn case-update consent, or disabled preference. Dispatch revalidates the same current authority before provider handoff.
  - Verification: independent review found and confirmed fixes for erased recipient PII at dispatch and an unreachable preference-suppression path. The canonical `trust-safety` matrix row is now optional by guarded data migration `20260720120000_MakeTrustSafetyPreferenceOptional`; reporter case updates and light moderation honor it, while heavy moderation in-app and `ModerationAvailabilityRequired` email bypass it at their explicit required policies. Application, Persistence, and API Release product builds passed with zero errors; static privacy/diff checks passed and `.worktrees` is absent. Focused source covers atomic rollback/retry, SLA bounds, consent/address/preference skips and dispatch-time withdrawal, but no tests, test-project builds, migration execution, SMTP, or Mailpit run was performed per user instruction.

- [x] **5.7 Queue safe final outcome after successful enforcement.**
  - Files: `ExecuteReportDecisionCommandHandler`, outcome mapping/templates, focused application/persistence tests.
  - Acceptance: `EventReportDecision` remains the sole decision authority; a one-to-one `EventReportDecisionExecution` records requested/in-progress, idempotent enforcement receipt, completion-pending, and completed effect states. NoViolation/Duplicate receive safe no-action copy; LightModerate/HeavyRedact/WarnOrganizer receive safe action-taken copy; completion and notification channels commit together after enforcement; enforcement-success/completion-failure resumes without duplicate enforcement/email; failed, escalated, stale, already-closed, and replay paths create no false/duplicate final email.
  - Evidence: every local or Coop decision now owns one required `EventReportDecisionExecution` and is selected through the case's composite-FK-enforced `CurrentDecisionId`. Stable IDs/timestamps and a single tracked graph save make capture retry/unknown-commit safe and lifecycle-neutral. Execution conditionally claims an exact lease before external moderation, validates a tenant/report/decision-bound moderation-record FK for Light/Heavy, persists an idempotent receipt as `CompletionPending`, and resumes completion without repeating enforcement. One serializable completion UoW applies report/case state, current-authority recipient graphs, and `Completed`; exact replay returns without mutation. NoViolation/Duplicate use generic no-action copy; Light/Heavy use generic action-taken copy; WarnOrganizer first requires linkless in-app warnings for every current effective EventOwner with optional verified/preference-gated email. Escalate and NeedsMoreInfo remain nonterminal and create no final outcome.
  - Verification: independent privacy, concurrency, and full-acceptance review found and confirmed fixes for event deep links, decision currency, exact-token response loss, lifecycle mutation during capture, stale owner/address/preference authority, orphan receipts, unstable retry IDs/times, ambiguous provider IDs, missing Coop case-stamp echo, duplicate-group replay identity, and insert-time concurrency-stamp drift. Migration `20260719230250_AddEventReportDecisionExecution` backfills executions/current pointers, adds exact FKs/checks/indexes, and fails with an explicit remediation diagnostic on duplicate legacy receipts. Application, Persistence, and API Release product builds passed with zero errors; EF reports no pending model changes; scoped diff checks passed and `.worktrees` is absent. Focused domain/application/persistence/provider source was updated, but no tests, test-project builds, migration execution, SMTP, or Mailpit run was performed per user instruction.

- [x] **5.8 Queue needs-more-information follow-up separately.**
  - Files: execute handler mapping, follow-up template/policy, tests.
  - Acceptance: only `ReportFollowUpContactConsent` authorizes email; required in-app remains; the message is not terminal/final-decision copy; withdrawal before dispatch skips; repeated requests have distinct occurrence IDs but exact replay deduplicates.
  - Evidence: completed current `NeedsMoreInfo` executions now materialize `report.needs-more-information` v1 inside the existing serializable completion UoW. The decision ID is the source/dedup identity, so genuinely new requests remain distinct and exact local/Coop replay exits before materialization. The factory uses `REPORT_FOLLOW_UP_CONTACT`, exact `report-follow-up-contact` consent, generic explicitly non-final linkless copy, required in-app delivery, and optional `ReportNeedsMoreInformation` SMTP work or one typed skipped email channel. Completion re-resolves the current report, active tenant membership, nondeleted reporter, verified persisted address, trust-safety preference, and follow-up consent; dispatch independently revalidates current authority and withdrawal.
  - Verification: independent review found and confirmed a fail-closed correction preventing anonymous, missing/deleted, or inactive reporters from leaving a completed `WaitingReporter` case without its required in-app request. Missing in-app authority now rolls back before business completion and leaves the decision resumable at `CompletionPending`; missing/unverified address, disabled preference, or withdrawn follow-up consent still commits required in-app plus typed email skip. Application and API Release product builds passed with zero errors; privacy/direct-SMTP/diff checks passed and `.worktrees` is absent. Focused source covers consent/address/preference and replay/distinct-decision boundaries, but no tests, test-project builds, SMTP, or Mailpit run was performed per user instruction.

- [ ] **5.9 Prove local API, Coop, and Osprey source convergence.**
  - Files: local report-decision API integration tests, Coop callback/effect integration tests, Osprey regression tests, provider docs.
  - Acceptance: a successful local API decision creates the same executor-owned outcome intent; enabled Coop creates it only after callback command/enforcement success; duplicate/out-of-order callbacks do not duplicate; Osprey callback alone creates no decision email and a later local execution does.
  - Source evidence recorded, runtime proof still open: local capture and explicit execute API routes converge on `ExecuteReportDecisionCommand`; the durable Coop inbox/effect worker revalidates retained callback identity and invokes `ProcessCoopDecisionCallbackCommand`, which calls the same executor before settling the pointer; Osprey only records/deduplicates signals and may raise urgency. Source coverage now asserts local/Coop outcome ownership, stale provider-decision rejection, stale effect-claim fencing, and Osprey's negative decision/email authority. Coop/Osprey docs match the implemented routing and use valid repository paths. Static route/negative-authority/diff checks passed and `.worktrees` is absent. This task remains unchecked because no API/provider/PostgreSQL integration or regression test may be run or built under the user's explicit no-tests instruction.

### Phase 5 Verification — RUN ONCE AFTER TASKS 5.1–5.9

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build`

## Phase 6 — Heavy Moderation Attendee Email

- [ ] **6.1 Add heavy-moderation required channel materialization.**
  - Files: heavy moderation occurrence creation, existing moderation fanout integration, `ModerationAvailabilityRequired` policy, focused tests.
  - Acceptance: irreversible heavy enforcement and one immediate occurrence pointer commit correctly; eligible attendees receive required in-app plus required email delivery when a verified current address exists; missing/unverified address is typed skipped; repeated enforcement is idempotent.

- [ ] **6.2 Enforce linkless privacy and highest-precedence suppression.**
  - Files: dedicated template/payload, disclosure/supersession integration, metrics/docs/tests.
  - Acceptance: subject/body/pointer contain no title, slug, URL, description, image, organizer, evidence, decision note, provider, storage path/key, or raw error; no event link is emitted; all pending lower-priority event work is suppressed; negative privacy tests inspect serialized payload and rendered bodies. Light-moderation email remains absent.

### Phase 6 Verification — RUN ONCE AFTER TASKS 6.1–6.2

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build`

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

### Phase 7 Verification — RUN ONCE AFTER TASKS 7.1–7.3

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build`

## Phase Verification Matrix

Each phase runs one Release build plus one selected deterministic non-browser project test after all phase tasks. Repetition is intentional: Application tests own handler-heavy phases, while Infrastructure tests own SMTP processing/reminder phases.

| Phase | Selected project test |
|---|---|
| 0B | `Event.Application.UnitTests` |
| 1 | `Explore.Infrastructure.Tests` |
| 2 | `Event.Application.UnitTests` |
| 3 | `Event.Application.UnitTests` |
| 4 | `Event.Application.UnitTests` |
| 5 | `Event.Application.UnitTests` |
| 6 | `Event.Application.UnitTests` |
| 7 | `Explore.Infrastructure.Tests` |

The registered intents still require Domain, Application, Persistence, Infrastructure, API, Architecture, Blazor Client, and Blazor Integration coverage where their surfaces change. Those suites, the exact Mailpit category lane, EF pending-model check, OpenAPI/client parity, and canonical `source-command-check` remain final merge evidence; they are not additional per-phase gates.

## Final Contract Evidence Before Merge

- [ ] All 51 tasks and all required channel-matrix rows are complete.
- [ ] Coop routing prerequisite has independent acceptance/verification evidence.
- [ ] Architecture tests forbid direct SMTP/send dependencies from controllers and handlers.
- [ ] Fault/concurrency/privacy scenarios listed in the plan are covered.
- [ ] Relevant Mailpit-backed SMTP and EmailDispatch drain tests pass.
- [ ] `dotnet ef migrations has-pending-model-changes` reports no pending model change.
- [ ] Canonical per-project verification from `source-command-check` is green or any pre-existing failure is recorded precisely.
- [ ] API/OpenAPI/generated client, HAL affordances, runtime configuration, schema, and canonical docs match behavior.
- [ ] Plan/context/tasks progress, next task, files, decisions, risks, and evidence are synchronized.
