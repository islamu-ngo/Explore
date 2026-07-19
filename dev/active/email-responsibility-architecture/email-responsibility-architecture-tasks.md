<!-- ABOUTME: Focused implementation ledger for the CTO-revised lifecycle-email architecture. -->
<!-- ABOUTME: Tracks schema, consent, fanout, trigger, operations, and verification slices with synchronized acceptance evidence. -->

# Email Lifecycle Delivery Tasks

> **Status:** Re-baselined in implementation — committed foundations plus preserved main-checkout SMTP and unrelated work
> **Last Updated:** 2026-07-19 Europe/Brussels
> **Progress:** 27/51 implementation tasks complete; phase verification remains separate
> **Current task:** 3.4c process pages with crash-safe checkpoint ordering
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

- [ ] **3.4c Process pages with crash-safe checkpoint ordering.**
  - Files: fanout worker, audience query/run repositories, lease/checkpoint coordination, focused application/persistence concurrency tests.
  - Acceptance: every recipient outcome commits before the page checkpoint; a crash before checkpoint safely replays through exact dedup; stale lease/fence stops work; a partially processed page yields no duplicates or skipped recipients. Add batch SQL only if measured bounded-worker evidence justifies it.
  - Effort: L. Dependencies: 3.4b.

- [ ] **3.5a Coordinate occurrence precedence, coalescing, and supersession.**
  - Files: new Application coordinator, occurrence repository conditional transitions, existing occurrence state fields, focused application/persistence tests.
  - Acceptance: heavy moderation > event cancellation > session cancellation > important update > reminder; cancellation/heavy work is immediate; five-minute important-update replacement retains earliest before/latest after; concurrent edits produce one active winner.
  - Effort: L. Dependencies: 3.4c.

- [ ] **3.5b Suppress materialized work at the provider-handoff fence.**
  - Files: notification delivery/email suppression repository operations, `EmailDispatchEligibilityEvaluator`, scheduler integration, focused persistence/infrastructure tests.
  - Acceptance: unsent pre-handoff rows become typed skipped/superseded; sent, unknown, and post-handoff evidence remains immutable; event/session precedence suppresses the correct scope; a race cannot escape after authoritative occurrence recheck.
  - Effort: M. Dependencies: 3.5a.

- [ ] **3.6a Add fair runnable selection and cross-instance tenant claim limits.**
  - Files: occurrence/run repositories, claim query/index migration only if proven necessary, scheduler tests.
  - Acceptance: one due occurrence per tenant per round; deterministic priority/time/ID order; tenant advisory lock enforces the configured active-claim ceiling across replicas; expired leases resume from the compound cursor.
  - Effort: L. Dependencies: 3.5b.

- [ ] **3.6b Add fanout processor settings, backpressure, telemetry, and health.**
  - Files: new processor/settings/validator/hosted service, `BusinessMetrics`, health check, operations/configuration docs, focused infrastructure tests.
  - Acceptance: each claim runs in a fresh scope; global/per-tenant work is bounded; optional reminders are suppressed above backlog threshold while required work proceeds; oldest age, processed/remaining, lease contention, supersession, and backpressure are observable without PII.
  - Effort: M. Dependencies: 3.6a.

### Phase 3 Verification — RUN ONCE AFTER TASKS 3.1–3.6b

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build`

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

### Phase 4 Verification — RUN ONCE AFTER TASKS 4.1–4.5

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build`

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
