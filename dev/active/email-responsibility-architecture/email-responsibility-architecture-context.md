<!-- ABOUTME: Resume context for the CTO-revised lifecycle-email implementation workstream. -->
<!-- ABOUTME: Records verified repository state, fixed architecture decisions, prerequisites, risks, and the next task. -->

# Email Lifecycle Delivery Context

> **Status:** Re-baselined in implementation — committed foundations plus preserved main-checkout SMTP and unrelated work
> **Last Updated:** 2026-07-19 Europe/Brussels
> **Progress:** 45/51 implementation tasks complete; phase verification remains separate
> **Current phase:** Phase 5 reporting email and provider convergence; Phase 1, Phase 3, and Phase 4 runtime verification remain open
> **Next task:** 6.1 add heavy-moderation required channel materialization; Task 5.9 runtime proof remains deferred

## Objective

Finish transactional product email for registration transitions, critical event/session changes, reporting, local/Coop moderation decisions, heavy-moderation attendee availability, and reminders by extending the existing PostgreSQL EmailDispatch and MailKit SMTP implementation. Mailpit remains the local development sink.

## Resume Brief

The user accepted the Senior CTO corrections. Tasks 0.1–0.7, 1.1–1.6e, 2.1–2.5, 3.1–3.6b, 4.1–4.5, and 5.1–5.8 are implementation-complete. The explicit recipient-delivery migration and fanout foundations are committed. Commit `9bfaf1e0` contains Task 1.5 retention/redaction, the Task 1.6a production boundary, and the earlier SMTP operations patch; later SMTP operations, fanout, event-trigger, and reporting changes remain preserved in the main checkout with their migrations and the strict controller architecture guard. Full Phase 1/3/4/5 testing, Docker-backed PostgreSQL runtime, and explicit Mailpit verification remain open; this affects phase evidence, not the implementation task checkboxes.

Task 1.4 added cancellation/revocation preference and unsubscribe mapping while keeping `ReportCaseUpdate` fail-closed. Task 5.1 now supplies distinct persisted case-update/follow-up authority, false-safe migration semantics, and exact dispatch-time checks, but no reporting email is enabled yet. Task 1.5 added bounded/dry-runnable parent-aware redaction, explicit resolve-without-replay, the single permanent `ContentRedactedAt` fence, immediate purged-tenant suppression, and migration/schema/operator documentation. Task 1.6a separates the send contract from connection testing: `InstanceSettingsController` uses MediatR, `SmtpHealthCheck` uses `IEmailConnectionTester`, and only Infrastructure owns `SmtpEmailService`.

Task 1.6b replaces select-before-claim with `ClaimPendingBatchAsync` and `TryClaimSpecificAsync`. Task 1.6c removes claim-time attempt charging and the process-local limiter. The serializable eligibility transition now locks/refills persisted global and tenant buckets, defers without transport evidence, or atomically allocates the next attempt and provider-handoff fence. Exact lease/attempt predicates protect success, retryable failure, dead-letter, unknown, and reconciliation. Cancellation releases unfenced claims, and stale recovery returns unfenced work to retry while preserving fenced uncertainty as `Unknown`.

Task 1.6d makes readiness active-only by excluding paused tenants from due, retry, stale, unknown, parked, dead-letter, oldest-age, and tenant-backlog aggregates. It separates actual SMTP provider attempts from pre-handoff skip/rate outcomes, closes all email/RabbitMQ metric vocabularies, replaces tenant GUID labels with bounded backlog rank, exposes persisted optional-reminder deferral as a current observable gauge, and redacts numeric or string tenant/address/recipient/subject/body/evidence/event-title/user/provider fields at the public health boundary.

Task 1.6e adds instance-wide pause/audit and bounded SMTP rate-override state to the existing singleton PostgreSQL processor row. Batch/specific claims, RabbitMQ pointer publication, and final pre-handoff eligibility share that authority; paused work consumes no attempt or rate token, while stale recovery continues. Global CQRS/API/HAL uses existing instance-setting authorization. Tenant-scoped `Unknown` reconciliation now requires an explicit delivered/not-delivered decision and atomically aligns the outbox, attempt, receipt, and email-delivery ledgers. Generic replay excludes `Unknown`, repairs linked retry state, and HAL exposes only transitions persistence accepts. Health publishes only sanitized global booleans, and operator runbooks cover Mailpit, cleanup, compromised tenants, reconciliation, and tenant-versus-instance diagnosis.

Task 3.4a adds the missing durable handoff from the general outbox to fanout execution. The Application handoff strictly parses the PII-free v1 pointer, validates its aggregate envelope, exact-loads tenant/occurrence state, and no-ops only for authoritative supersession. Persistence serializes ensure/claim with the existing occurrence advisory lock, creates one independent-ID pending unleased run under the explicit occurrence FK/unique constraint, and returns an existing run idempotently. Retry identifiers are generated outside execution-strategy delegates; failed attempts clear tracked state; ambiguous successful claim commits return the same owner/token. The composite dispatcher returns after ensure, leaving recipient leases to later workers.

All remaining work must be performed in the main repository checkout. Do not run `git worktree add`, create linked worktrees or a `.worktrees` directory, or record a worktree path for this workstream. This explicit user constraint overrides generic automation/workflow guidance; if a workflow cannot proceed without a worktree, stop and report the conflict instead of creating one.

The revised core is:

```text
NotificationIntent = one business occurrence + one recipient
NotificationDelivery = one channel decision and outcome
EmailDispatchOutbox = durable SMTP execution state
```

The real EF relationships and migration are committed. New email rows keep independent IDs and carry `NotificationIntentId` plus `RecipientUserId`. Delivery rows identify channel and explicit delivery policy. One recipient operation atomically creates the intent, configured channel rows, in-app notification, and email outbox row.

High-fanout occurrence, audience, and lease/checkpoint persistence are committed. The remaining worker must consume `NotificationFanoutOccurrence` pointers and must never recreate an old change from current event/session data.

Reporter consent must split into case updates and follow-up contact, with an authenticated HAL-controlled withdrawal endpoint. `Reporting:CaseSlaHours` is the only SLA source; receipt copy says normally within the configured hours, not configurable business days.

Coop’s former incoming-webhook routing defect is repaired by Tasks 0.5–0.7. Fresh full API verification remains required before Phase 5 source convergence. Osprey remains signal-only.

## Verified Repository Facts

- `SmtpEmailService` uses MailKit; Aspire starts Mailpit in local profiles.
- `EmailDispatchOutbox` is already the PostgreSQL SMTP ledger with claims, attempts, receipts, retry, unknown, dead-letter, park/replay, and tenant pause.
- TickerQ and RabbitMQ are pointer/wake-up mechanisms; they are not business authorities.
- `NotificationIntent`, `NotificationDelivery`, and `EmailDispatchOutbox` now have explicit tenant/intent/recipient relationships, stable channel/policy snapshots, and tenant-safe uniqueness/FKs through migration `20260717131038_NormalizeRecipientNotificationDelivery`.
- `RecipientNotificationMaterializer` atomically creates intent, in-app delivery, email delivery, `Notification`, and SMTP outbox rows; exact deduplication conflicts recover only after rollback in a fresh UoW.
- `EmailDispatchEligibilityEvaluator` refreshes current verified addresses, validates managed-invitation authority, preferences, supported policy, membership, tenant state, and superseded delivery state, then establishes the provider-handoff fence.
- registration confirmation, approval, rejection, waitlist promotion, self-cancellation, organizer/system revocation, scheduled reminder materialization, and tenant-administrator invitation all use the atomic primitive.
- registration create/update/delete handlers own serializable `IUnitOfWork` boundaries and repositories return explicit transition results without owning commits or notification entities.
- event/session cancellation and schedule/location/timezone handlers do not enqueue attendee email.
- `NotificationFanoutOccurrence`, compound audience queries, `CoverageEstablishedAt`, and fenced `NotificationFanoutRun` lease/checkpoint persistence are implemented; the dispatcher/recipient worker, supersession/coalescing, and fair scheduling are not.
- `EventReport.ReporterContactConsent` and the dialog label cover only contact when more context is needed.
- My Reports already has authenticated list/detail API, HAL policies, Blazor page, and generated NSwag contracts suitable for consent withdrawal.
- `ExecuteReportDecisionCommandHandler` is the safe post-enforcement notification seam.
- Coop callbacks require a signed nonblank `ProviderDecisionId`; `CoopDecisionIncomingWebhookHandler` atomically persists one tenant-safe `IncomingWebhookEffectOutbox` pointer and settles intake without an applied-effect receipt. The fenced renewable-lease worker, retry/dead-letter path, health/metrics, retention ordering, and authenticated HAL inspection/redrive are implemented.
- Osprey callbacks record signals and may raise review priority; they do not decide or enforce.
- `docs/OPERATIONS.md` targets 180-day retention after email evidence is resolved, with unresolved failure evidence kept until operator resolution.
- All document paths referenced by the revised plan, including `docs/SECURITY-MODEL.md`, `docs/EMAIL_NOTIFICATIONS.md`, provider docs, and `schemas/islamu-event.md`, exist in the current checkout.

## Fixed Decisions

1. ISLAMU Event owns product email; Keycloak/PDS own credential-token email.
2. PostgreSQL EmailDispatch remains the only SMTP work ledger.
3. Add explicit tenant/intent/recipient composite PK/FK relationships; no hidden shared-primary-key convention or cross-intent delivery link.
4. `NotificationDelivery` gains channel, policy, required/optional decision, channel-neutral outcome, and a link to either in-app `Notification` or `EmailDispatchOutbox`.
5. Seed policies: `RegistrationStatusOptional`, `CriticalEventUpdateOptional`, `ReportCaseUpdate`, `ReportFollowUpContact`, `ModerationAvailabilityRequired`, `ModerationContextOptional`, `ReminderOptional`, and `TenantAdministrationRequired`.
6. Intent + all channel delivery state commits atomically per recipient.
7. Missing-half repair is exceptional. A PostgreSQL unique violation must roll back; recovery starts a fresh transaction and handles only the exact constraint.
8. Batch fanout may use `ON CONFLICT DO NOTHING` to avoid N+1 existence checks.
9. Dispatch revalidates current verified email, preference, report consent, occurrence supersession, and tenant/rate-limit controls.
10. A changed user address is refreshed from persisted verified identity before SMTP; an old snapshot is never sent.
11. Registration repository methods stop beginning/committing transactions or accepting notification/outbox entities. Application/UoW owns the serializable boundary.
12. Registration transition result includes `Changed`, `ParentIntentId`, `PreviousStatus`, `FinalStatus`, `TransitionReason`, and pre-generated `OccurrenceId`.
13. `NotificationFanoutOccurrence` stores immutable occurrence, cutoff, aggregate version, change set, safe before/after values, template/version, policy, priority, not-before time, and supersession state; fanout intents reference it through an FK and unique occurrence/user constraint.
14. Audience existed by cutoff and remains eligible at processing. Event/update audiences use current Pending/Approved/Waitlisted; reminders use Approved only. Session coverage requires a live target child.
15. Stable fanout cursor is `(FirstEligibleRegistrationCreatedAt, UserId)` and the unique occurrence/user intent remains the final guard.
16. Supersession precedence is heavy moderation > event cancellation > session cancellation > important update > reminder.
17. Cancellation/heavy moderation are immediate. Important updates use a configurable five-minute default coalescing window.
18. Location copy uses immutable values only after current disclosure authorization. Heavy-moderation copy is generic and linkless.
19. Reporter consent splits into `ReportCaseUpdatesConsent` and `ReportFollowUpContactConsent`; both can be withdrawn before dispatch.
20. Reporter receipt/outcome use case-update consent; needs-more-information uses follow-up consent.
21. Receipt copy derives only from `Reporting:CaseSlaHours` (default 48). No business-day settings are introduced.
22. Local API and enabled Coop decisions converge on `ExecuteReportDecisionCommandHandler`; failed enforcement, escalation, and Osprey signals create no final email.
23. Heavy moderation creates required in-app and required operational email when a verified address exists. Light-moderation email is deferred.
24. Sent/ordinary-skipped content is redacted after 180 days. Dead-lettered/unknown/parked rows retain replay material until successful replay or explicit resolve-without-replay; explicit resolution redacts immediately. `ContentRedactedAt` is the single permanent replay fence.
25. Multi-tenant fairness, backpressure, SMTP rate limiting, metrics, alerts, cleanup, pause, inspect, and replay controls are release requirements.
26. New user-addressed relationships use tenant-aware alternate/candidate keys and composite `TenantUser` FKs; no bare global-user relationship is authoritative.
27. Recipient authority is explicit and limited to `TenantUserVerifiedEmail` or authorization-bound `ManagedTenantAdministratorInvitation`. The pre-1.0 migration resets old delivery ledgers instead of inventing a legacy recipient authority; arbitrary non-user delivery is not added.
28. Delivery snapshots policy version, consent purpose/version, preference result, disclosure, template/link authority, and address source. Current checks may narrow but never broaden that ceiling.
29. Session audience also requires immutable child `CoverageEstablishedAt <= AudienceCutoffAt`; same-scope atomic replacements inherit it and new/moved coverage receives a new timestamp.
30. `ProviderHandoff` is the suppression linearization fence. Pre-fence work can skip; post-fence I/O/protocol/persistence uncertainty is `Unknown`, never automatic resend or a claim that SMTP was recalled.
31. `EventReportDecision` remains the sole decision. One-to-one `EventReportDecisionExecution` records operational enforcement/completion state so a crash after enforcement resumes without duplicate enforcement or email.
32. `EmailDispatch:EventReminderLeadTimeHours` is the sole reminder lead setting: default 24, inclusive range 1..168. Past-due future sessions are due immediately; started sessions get no reminder. DST gaps reject and overlaps use the persisted offset/instant.
33. SMTP fair selection and claim are one PostgreSQL operation. Deterministic tenant rounds and global/per-tenant active-processing ceilings are enforced across replicas; process-local semaphores are scheduling optimizations only.
34. Global and per-tenant SMTP token buckets have persisted cross-instance authority. Rate admission occurs before attempt count or `provider_handoff_started`; rate deferral creates no SMTP attempt/receipt evidence and cannot dead-letter a row.
35. Optional-reminder hysteresis uses active core backlog that excludes paused tenants and optional reminders. Its authority is persisted or centrally coordinated so restart/replica behavior cannot disagree.
36. Stale processing recovery checks provider-handoff evidence. An unfenced claim returns safely to retryable work; only a fenced claim becomes `Unknown`.

## Channel Summary

| Trigger | Required in-app | Email |
|---|---:|---|
| Registration receipt/status/cancel/revoke | Yes | Optional, verified, preference-gated |
| Critical event/session change or cancellation | Yes | Optional, verified, preference-gated |
| Report receipt/outcome | Yes | Case-update consent required |
| Needs more information | Yes | Follow-up consent required |
| Heavy moderation unavailable | Yes | Required when a verified current address exists |
| Light moderation | Existing only | Deferred |
| Reminder | Product preference | Optional, preference-gated |
| Tenant admin invitation | Existing behavior | Required |

## Fanout Eligibility Snapshot

Event-wide cancellation/update:

- registration intent `CreatedAt <= AudienceCutoffAt`;
- current parent status Pending, Approved, or Waitlisted;
- not soft-deleted;
- group to one user.

Session cancellation/update adds:

- target-session child exists and is not soft-deleted;
- current child status Pending, Approved, or Waitlisted;
- whole-event, whole-day, and explicit-session scope are represented by those child rows;
- a partially cancelled target child is excluded even when another child keeps the parent live.

Reminder narrows parent and target child to Approved.

## Coop Phase 0 Prerequisite

Tasks 0.5–0.7 are an independently reviewed and merged correctness prerequisite, not email slices. They must prove:

- unique provider event/delivery identity;
- one effect row per inbox/effect kind;
- retained callback cannot be deleted while a pointer depends on it;
- payload cleanup order is explicit;
- stale/out-of-order decisions cannot reopen a completed case;
- poison input is quarantined/dead-lettered;
- callback, pointer, and dispatcher replay remain idempotent;
- the effect pointer completes only after the Coop command succeeds.

Tasks 0.5–0.7 are implementation-complete. They provide the specialized pointer entity/configuration/repository, concrete incoming handler, explicit `PointerPersisted` intake outcome, signed provider-ID requirement, payload-retention guard, fenced consumer, clean receipt recovery, bounded retry/dead-letter, replay-safe retention, health/metrics, and authenticated HAL inspection/redrive. The recipient-delivery schema blocker has been removed; only the fresh full API phase evidence remains open.

If this prerequisite is disabled or incomplete, local reporting email may proceed, but Coop decision email remains disabled and documentation must say so.

## Completed Phase 1 Dependency Checkpoint

Tasks 1.1–1.3 completed the staged pre-1.0 transition. The relationship model/contracts, atomic materializer, registration/reminder/admin-invitation writer migration, exact fresh-transaction dedup recovery, provider-handoff settlement, and transactionally bounded six-ledger reset are committed. Inbox notifications and unrelated event/registration/report/audit/settings data remain preserved. Down restores the old empty schema and lookup codes but cannot reconstruct intentionally deleted delivery rows.

## Key File Map

Implemented foundations:

- `src/Explore.Application/Notifications/RecipientNotificationMaterializer.cs` — atomic recipient graph and exact conflict recovery.
- `src/Explore.Persistence/Migrations/20260717131038_NormalizeRecipientNotificationDelivery.cs` — explicit delivery graph and pre-1.0 ledger reset.
- `src/Explore.Persistence/Services/EmailDispatchEligibilityEvaluator.cs` — current authority checks and provider-handoff fence.
- `src/Explore.Application/Services/RegistrationNotificationDeliveryService.cs` — lifecycle transition-to-template/materialization mapping.
- registration create/update/delete handlers plus `EfCoreUnitOfWork` and registration repositories — Application-owned serializable boundaries.
- `src/Explore.Domain/NotificationFanoutOccurrence.cs`, `NotificationFanoutRun.cs`, and their persistence repositories/migrations — immutable occurrence, audience cutoff, lease, and checkpoint foundation.
- `src/Explore.Application/Services/Webhooks/IncomingWebhookEffectProcessingService.cs` and `src/Explore.Infrastructure/Webhooks/IncomingWebhookEffectDrainService.cs` — durable Coop command execution.
- `src/Explore.API/Controllers/IncomingWebhookEffectsAdminController.cs` — authenticated HAL inspection/redrive.
- `src/Explore.API/Controllers/EmailDispatchAdminController.cs` plus email-dispatch CQRS/HAL — authenticated tenant controls, instance-wide processor controls, and explicit unknown reconciliation.
- `src/Explore.Persistence/Migrations/20260718220406_AddEmailDispatchAtomicClaimState.cs` and `20260719015757_AddEmailDispatchOperatorControls.cs` — cross-replica SMTP admission plus durable operator control state.
- `src/Explore.Application/Notifications/NotificationFanoutRecipientTemplateFactory.cs` and `NotificationFanoutTemplateContracts.cs` — closed strict v1 occurrence parsing and immutable recipient copy.
- `src/Explore.Application/Services/NotificationFanoutRecipientMaterializationService.cs` and `FanoutAttendeeLocationAuthorizationService.cs` — current verified-address/preference resolution, value-free recipient-bound disclosure masking, and atomic graph orchestration.
- `src/Explore.Application/Services/NotificationFanoutPageProcessor.cs` — already-fenced bounded page execution with per-recipient renewal, atomic graph materialization, strict compound ordering, and commit-before-checkpoint progression.
- `src/Explore.Application/Services/NotificationFanoutOccurrenceCoordinator.cs` plus `NotificationFanoutOccurrenceRepository` — source-before-event advisory locking, exact session/event authority, PostgreSQL-precision immutable replacement, conditional supersession, and one stable pointer inside the caller UoW.
- `src/Explore.Application/Contracts/Persistence/INotificationFanoutEmailSuppressionRepository.cs` plus `src/Explore.Persistence/Repositories/NotificationFanoutEmailSuppressionRepository.cs` — exact tenant/occurrence-linked pre-handoff email suppression that preserves immutable provider evidence.
- `src/Explore.Persistence/NotificationFanoutPrecedenceLock.cs` plus `EmailDispatchEligibilityEvaluator` — one shared event advisory-lock identity and fresh authoritative occurrence recheck before SMTP provider handoff.
- `src/Explore.Application/Contracts/Persistence/INotificationFanoutRunRepository.cs` plus `NotificationFanoutRunRepository` — bounded one-per-tenant due rounds, mandatory tenant claim ceilings, tenant/event/occurrence lock ordering, and expired-cursor recovery over existing handoff-created runs.
- `src/Explore.Persistence/Migrations/20260719183139_AddNotificationFanoutGlobalRunnableIndex.cs` plus occurrence configuration/model snapshot — one reversible partial cross-tenant due index with priority-descending metadata.
- `src/Explore.Infrastructure/NotificationFanout/NotificationFanoutProcessor.cs`, settings/validator, and health check — bounded hosted execution with one fresh scope per claim and aggregate PII-free operations state.
- `src/Explore.Domain/NotificationFanoutProcessorState.cs`, repository/configuration/migration, and `BusinessMetrics` — persisted optional-reminder hysteresis, database-authoritative ceilings, and bounded processor telemetry.
- `src/Explore.Application/Features/Events/Handlers/Commands/CancelEventCommandHandler.cs` — atomic event cancellation plus event-wide immutable occurrence/pointer and post-commit cache eviction.
- `src/Explore.Application/Features/EventSessions/Handlers/Commands/EventSessionLifecycleTransitionCommandHandlerBase.cs` plus `CancelEventSessionCommandHandler` — shared atomic transition hook, published-only session cancellation occurrence, and post-commit cache eviction.

Next implementation surfaces:

- Task 5.6: consented report receipt with canonical SLA snapshot and atomic typed delivery outcomes.
- Remaining Phases 5–7: reporter contracts/UI/receipt/outcome/follow-up/convergence, heavy-moderation occurrence, and safe reminder caller activation.

## Risks and Controls

| Risk | Control |
|---|---|
| Pre-1.0 outbox rows cannot satisfy the final recipient model | Transactionally reset only intent/delivery/delegation/email/attempt/receipt ledgers; preserve inbox notifications and unrelated business/audit/settings data; document non-reconstructing Down. |
| PostgreSQL transaction is aborted after unique violation | Catch only outside the UoW; recover in a fresh transaction or use `ON CONFLICT DO NOTHING`. |
| Repeated edits create storms | Five-minute coalescing plus newer-unsent supersession; cancellation bypasses delay. |
| Mutable state corrupts old messages | Immutable safe before/after occurrence snapshot. |
| Live audience drifts | Frozen creation cutoff plus current eligibility recheck. |
| Consent/address changes after queue | Central dispatch-time evaluator. |
| Large tenant starvation | Fair per-tenant selection, bounded lease/concurrency/rate, backlog thresholds. |
| Retained body/address becomes privacy debt | Parent-aware 180-day redaction and immediate tenant-deletion handling. |
| Heavy moderation leaks identity | Dedicated generic/linkless policy and negative tests. |
| Coop defect expands the email PR | Independent Tasks 0.5–0.7 prerequisite with its own acceptance/gate. |
| A persisted Coop pointer is mistaken for an applied moderation decision | `PointerPersisted` creates no effect receipt; the effect worker executes the retained callback successfully before atomically creating/reusing the receipt and completing the pointer. |
| New cancellation/revocation kinds bypass legacy preference/footer mapping | Task 1.4 maps both existing switch sites and focused tests cover the behavior. |
| Fanout page crash advances past an uncommitted recipient | Task 3.4c commits each recipient graph before the compound checkpoint; partial pages replay through exact occurrence/user dedup and stale claims cannot advance the cursor. Runtime concurrency evidence remains an open phase gate. |
| Later email work regresses the completed cross-instance SMTP authority | Tasks 1.6b–1.6e own one persisted claim/rate/fence/telemetry/operator authority. Do not restore local correctness limiters, unfenced email mutators, unbounded labels, invalid HAL transitions, or alternate pause/rate state. |
| Optional backlog permanently suppresses reminders | Task 1.6b persists hysteresis and counts only active core backlog, excluding paused tenants and optional reminders while keeping required reminders eligible. Runtime PostgreSQL proof remains open. |
| Architecture guard regresses after the boundary repair | Keep the preserved strict controller/handler transport-reference guard and the MediatR diagnostic seam; do not restore controller access to `IEmailService`. |
| Supersession loses a race to SMTP eligibility | Task 3.5b shares the event advisory lock across coordinator and evaluator, reloads authoritative occurrence state under `READ COMMITTED`, suppresses only pre-handoff work, and preserves fenced/terminal evidence. Runtime two-transaction proof remains an open Phase 3 gate. |
| Fair selection claims stale superseded work or one tenant exceeds its ceiling | Task 3.6a shares the event-precedence lock before fresh claim revalidation, ranks one existing run per tenant, and counts live occurrence claims under the tenant lock. PostgreSQL runtime proof remains open. |
| A producer crosses optional-reminder backpressure between selection and claim | Task 3.6b serializes occurrence-run creation, hysteresis calculation, and exact admission through the same global advisory lock; optional work remains durable and required work continues. PostgreSQL runtime proof remains open. |

## Verification State

- Historical implementation evidence on 2026-07-17 records a Release build with 0 errors and full Domain 439, Application 2,449, Persistence 448, Infrastructure 832, and Architecture 240 passed/1 declared skip after the recipient migration landed.
- Task 0.5 independent verification is confirmed. Focused PostgreSQL pointer/migration coverage passed 7/7; missing-ID and valid-ID verifier tests passed 1/1 each; EF reported no pending model changes.
- Task 0.5 full directly affected suites passed: Domain 428, Application 2,420, Persistence 401, Infrastructure 818, API 1,830 with 3 declared skips, and Architecture 238 with 1 declared skip; zero failures.
- Tasks 0.6–0.7 focused evidence covers fenced claim/recovery, retry/dead-letter, redrive/auth, metrics, health, operator HAL, and real PostgreSQL effect behavior. Their implementation checkboxes are complete.
- The prior missing-column blocker is resolved by migration `20260717131038_NormalizeRecipientNotificationDelivery`. A fresh full API suite and explicit Mailpit lane remain unrecorded because testing was stopped; no phase requiring those gates is called fully verified.
- Task 1.5 and migration `20260718203920_AddEmailDispatchContentRetention` are committed in `9bfaf1e0`; focused Infrastructure evidence was reported before interruption, but Docker-backed PostgreSQL execution is not recorded.
- Task 1.6a recorded 2/2 handler tests, 1/1 handler boundary test, 1/1 controller boundary test, 3/3 SMTP configuration tests, an API Release build with zero errors, and clean focused diagnostics. Its production files are committed in `9bfaf1e0`; the strict controller guard remains a preserved main-checkout change.
- Mailpit selected 2 tests but could not execute because neither Docker socket was available. A later Application source rebuild stopped in unrelated `AtprotoEventDescriptionFormatterTests.cs` because `AtprotoEventProjectionSourceContract` was unavailable at that point. `HEAD` has since advanced to `18556b29`, so neither result is promoted to a current Phase 1 gate.
- Task 1.6b is independently confirmed after two fix loops. Drain tests passed 28/28; settings-validator tests passed 22/22; Persistence, Infrastructure, and both affected test projects built with zero errors; EF reported no pending model changes; and `git diff --check` passed. The exact PostgreSQL class selected 39 tests, but every result failed during shared Testcontainers fixture construction and zero bodies executed because no Docker daemon/socket, alternate container runtime, or local PostgreSQL server exists. This is missing runtime evidence, not a passed PostgreSQL gate.
- Task 1.6c is independently confirmed after two verifier-driven state fixes. Drain tests passed 31/31; validator tests passed 24/24; affected builds and EF model parity passed; scoped diff checks passed. The exact PostgreSQL class selected 42 tests but zero bodies executed because unrelated migration `20260718215537_BackfillUnclassifiedEventLocations` fails fixture setup with PostgreSQL `42703` before email tests run. This remains open phase evidence, not a Task 1.6c pass claim.
- Task 1.6d is independently confirmed after correcting unbounded RabbitMQ labels, validator/documentation maxima, and sampled backpressure semantics. Active health aggregates exclude paused tenants; email metrics use closed vocabularies without tenant or recipient identifiers; optional-reminder deferral is an observable current-state gauge; public health redacts sensitive numeric and string keys before type handling. Application, Infrastructure, and ServiceDefaults Release builds passed across the implementation/fix loops, but focused tests reached no discovery because unrelated event-projection/ATProto compilation errors affect the shared checkout. PostgreSQL runtime remains blocked by the separately recorded location migration.
- Task 1.6e is independently confirmed. Global controls reuse instance-setting authority; durable pause and rate override state is observed by claim, RabbitMQ publication, and pre-handoff admission; explicit delivered/not-delivered reconciliation and repaired replay keep all delivery ledgers aligned; HAL mirrors exact persistence transitions; health exposes no operator reason/actor; and the requested runbooks are present. Domain, Application, Persistence, and Infrastructure Release source builds passed with zero warnings/errors; API passed with zero errors and existing shared-checkout warnings. Scoped diff checks passed and `.worktrees` is absent. No tests or EF mutation commands were run, honoring the user's stop-tests instruction. During implementation, an EF remove command accidentally deleted the untracked Task 1.6c migration pair; both files were immediately reconstructed from the faithful generated target model and exact reversible schema delta, hash-protected through the separate Task 1.6e migration repair, and independently checked without touching a database.
- Task 3.4a is independently confirmed after correcting execution-strategy tracker cleanup and strict pointer parsing. Application, Persistence, and Infrastructure Release source builds passed with zero errors. Static review confirms exact tenant/occurrence authority, full envelope validation, superseded no-op, pre-generated retry identifiers, advisory-lock ensure, one pending unleased run, same-token ambiguous-commit claim recovery, dispatcher return-before-claim, and no schema change. Scoped diff checks passed and `.worktrees` is absent. Focused test sources were added but not executed per user instruction.
- Task 3.4b is independently confirmed after closing recipient/room authorization-mask binding and removing unsupported title-only updates from the closed v1 contract. Four event/session cancellation/update template keys fail closed on unknown version, policy, scope, change field, malformed JSON, or extra member. Current persisted verified email plus preference determines optional email versus typed skip, required in-app remains, `FanoutOccurrenceId` is persisted, and both exact PostgreSQL unique races recover through a fresh UoW. Current disclosure returns only a tenant/event/recipient/location/room-bound field mask; rendered values come only from immutable snapshots. Domain, Application, Persistence, and Infrastructure Release source builds passed with zero errors across the implementation/fix loop; scoped diff checks passed; `.worktrees` is absent. Test sources were added but not executed per user instruction.
- Task 3.4c is independently confirmed after repairing three expression-tree-incompatible test predicates found during static review. `NotificationFanoutPageProcessor` validates the immutable template before any empty completion, renews the exact claim per page and recipient, rejects malformed or non-monotonic audience pages, commits each recipient through the atomic materializer, checkpoints only after a complete page, and stops on stale renewal/checkpoint. Persistence renewal is exact-fence idempotent and never shortens the lease. Application and Persistence Release source builds passed before the focused test-source repair; scoped diff checks passed and `.worktrees` is absent. No tests or test-project builds were run per user instruction, so PostgreSQL crash/replay and concurrency evidence remains open.
- Task 3.5a is independently confirmed after four fail-closed corrections: tenant-wide source identity now serializes before event precedence, replay chains validate event/scope/priority/order, session candidates prove their exact tenant/event binding, and candidate/derived/supersession timestamps use PostgreSQL microsecond precision. The coordinator applies the fixed precedence matrix, immediate cancellation/heavy timing, deterministic five-minute immutable update replacement, winner-before-loser FK ordering, and stable pointer-last persistence inside the caller UoW. Application and Persistence Release product builds passed with zero errors; scoped diff checks passed; `.worktrees` is absent. Focused source covers same-event concurrency, cross-event same-source replay, sub-microsecond retry, scope corruption, and conditional misses, but no tests or test-project builds were run per user instruction.
- Task 3.5b is independently confirmed. A successful conditional occurrence supersession invokes one exact tenant/occurrence suppression operation under the shared event lock; active email delivery becomes `Superseded`, only pending/retry/unfenced-processing SMTP rows become `Skipped`, and terminal or provider-fenced evidence remains immutable. Final eligibility uses `READ COMMITTED`, acquires the same event lock before the outbox lock, reloads intent/occurrence authority, and skips superseded work before rate or provider-handoff evidence. Application, Persistence, and Infrastructure Release product builds passed with zero errors; scoped diff checks passed; `.worktrees` is absent. No tests or test-project builds were run per user instruction, and the true cross-transaction race remains open Phase 3 runtime evidence.
- Task 3.6a is independently confirmed after repairing two review blockers. The exact claim path now serializes tenant capacity, event precedence, and occurrence state before a fresh pending/due authority load; a focused PostgreSQL source scenario holds the precedence lock through an uncommitted supersession and proves the later claim remains pending/unclaimed. The shared EF snapshot, accidentally emptied by the earlier location reconciliation workflow, was restored from its latest full generated Designer and differs only by the intended partial global due index. Application and Persistence Release product builds passed after the fix loop; Persistence reported 0 errors/0 warnings. Scoped diff checks passed and `.worktrees` is absent. No tests or test-project builds were run, so live cross-replica fairness, index-plan, and concurrency evidence remains open.
- Task 3.6b is independently reviewed after closing producer/selection races and page-unavailable accounting. The hosted processor resolves every page processor from a fresh scope; global/per-tenant active ceilings and persisted high/low reminder deferral are database-authoritative; run producers and exact claims share the global lock before the existing tenant/event/occurrence order; and health/metrics are aggregate and PII-free. The Release API product build passed 8 projects with 0 errors, configuration JSON and diff checks are clean, and `.worktrees` is absent. No tests or test-project builds were run, so the Phase 3 runtime gate remains open.
- Task 4.1 is independently reviewed with no blocker. The handler commits event status, ATProto plan, immutable event-wide cancellation occurrence, precedence suppression, and pointer atomically from retry-stable inputs; failure/replay guards create nothing; cache invalidation is post-commit. The Application Release product build passed 2 projects with 0 errors and diff checks are clean. No tests or test-project builds were run, so real PostgreSQL rollback/concurrency remains open Phase 4 evidence.
- Task 4.2 is independently reviewed with no blocker. The lifecycle base keeps status/schedule/fanout inside one caller transaction and cache eviction after it; the cancel hook emits one strict session occurrence only from Published, with stable source identity/cutoff and immutable title/schedule/timezone. The Application Release product build passed 2 projects with 0 errors/0 warnings and diff checks are clean. No tests or test-project builds were run, so real PostgreSQL/audience concurrency remains open Phase 4 evidence.
- This 2026-07-18 re-baseline changes planning docs only and runs no build, test, SMTP, Mailpit, provider, migration, or application command.
- Planning validation: `git diff --check -- dev/active/email-responsibility-architecture`.
- Runtime phases use one Release build and one selected non-browser project test; final intent-contract suites remain merge evidence. No gate uses weak broad filters or `--minimum-expected-tests 1`.

## Handoff Rule

Start Task 6.1 from the completed heavy-redaction enforcement receipt, `EventModerationNotificationFanoutService`, and the generic occurrence/fanout recipient materializer. Successful irreversible heavy enforcement must atomically ensure one immediate event-wide high-precedence occurrence pointer keyed to its authoritative moderation record/decision; replay must reuse it. Eligible attendee materialization uses required linkless in-app plus required email policy, current verified addresses, typed missing/unverified skips, and no light-moderation email. Keep the event content/privacy suppression hardening in Task 6.2. Task 5.9 remains unchecked until its local/Coop/Osprey integration tests may run. The working tree currently contains unrelated ATProto/auth/location-privacy work; do not modify, stage, revert, or include it. Use only the main checkout and never create a linked worktree or `.worktrees` directory. At every handoff, synchronize progress, evidence, risks, and the next task across context, plan, and tasks.

## Handoff Notes

### Handoff — 2026-07-19 Europe/Brussels

- **Current state:** 45/51 implementation tasks are complete. Task 5.9 production routing, source coverage, and provider docs are audited, but its runtime proof remains unchecked under the no-tests instruction. Task 6.1 is the next implementation slice.
- **Next action:** add one idempotent heavy-moderation occurrence and required attendee channel materialization after authoritative irreversible enforcement.
- **Blockers:** Docker/PostgreSQL is available, but unrelated untracked migration `20260718215537_BackfillUnclassifiedEventLocations` fails shared fixture setup with PostgreSQL `42703` (`is_deleted` missing), so all 42 selected email transition tests execute zero bodies. Mailpit and the Phase 1 gate remain open.
- **Modified files:** Task 5.9 changes only focused local/Coop/Osprey test source and provider docs; production routing was not changed. Earlier email/fanout and unrelated changes remain preserved.
- **Validation:** static source audit confirmed local and durable Coop paths converge on the canonical executor and Osprey remains signal-only; diff and negative-authority checks passed and `.worktrees` is absent. No test or test-project build ran, so Task 5.9 remains unchecked.
- **Documentation impact:** plan/context/tasks retain 45/51 complete, record Task 5.9's precise evidence gap, and advance implementation to Task 6.1.
- **Risks:** Task 6.1 must bind heavy attendee notification to the exact irreversible enforcement receipt, preserve highest precedence/idempotency, and represent missing verified addresses as typed skips without exposing event content.
- **Notes for next contributor/agent:** work only in the main checkout; preserve all current changes; do not create worktrees, stage broadly, revert shared files, or duplicate the partial Task 1.6 implementation.
