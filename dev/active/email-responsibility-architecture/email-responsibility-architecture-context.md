<!-- ABOUTME: Resume context for the CTO-revised lifecycle-email implementation workstream. -->
<!-- ABOUTME: Records verified repository state, fixed architecture decisions, prerequisites, risks, and the next task. -->

# Email Lifecycle Delivery Context

> **Status:** Re-baselined in implementation — committed foundations plus preserved main-checkout Task 1.5/1.6 work
> **Last Updated:** 2026-07-18 Europe/Brussels
> **Progress:** 20/50 implementation tasks complete; phase verification remains separate
> **Current phase:** finish Phase 1 operational safety, then Phase 3 fanout execution
> **Next task:** 1.6a SMTP capability boundary; then 1.6b–1.6d and 3.4a

## Objective

Finish transactional product email for registration transitions, critical event/session changes, reporting, local/Coop moderation decisions, heavy-moderation attendee availability, and reminders by extending the existing PostgreSQL EmailDispatch and MailKit SMTP implementation. Mailpit remains the local development sink.

## Resume Brief

The user accepted the Senior CTO corrections. Tasks 0.1–0.7, 1.1–1.5, 2.1–2.5, and 3.1–3.3 are implementation-complete. The explicit recipient-delivery migration and fanout foundations are committed. Task 1.5 retention/redaction and partial Task 1.6 fairness/telemetry changes are present only as preserved main-checkout changes. Full build, PostgreSQL retention runtime, and explicit Mailpit verification remain open; this affects phase evidence, not the implementation task checkboxes.

Task 1.4 added cancellation/revocation preference and unsubscribe mapping while keeping `ReportCaseUpdate` deliberately fail-closed until Task 5.1 introduces distinct case-update consent. Task 1.5 added bounded/dry-runnable parent-aware redaction, explicit resolve-without-replay, the single permanent `ContentRedactedAt` fence, immediate purged-tenant suppression, and migration/schema/operator documentation. Task 1.6 is now split into four reviewable slices because the checkout already contains partial fair selection, concurrency, rate, backlog, metrics, health, and architecture-guard work.

All remaining work must be performed in the main repository checkout. Do not create linked worktrees or a `.worktrees` directory for this workstream.

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

Next implementation surfaces:

- Task 1.6a: keep the strict architecture guard and remove `InstanceSettingsController`/health coupling to `IEmailService` through a narrow Application-owned diagnostic capability.
- Task 1.6b: reconcile the existing fair pending query, concurrency, global SMTP rate, and backlog patch; add missing per-tenant rate/cross-instance semantics.
- Tasks 1.6c–1.6d: finish PII-safe health/metrics and authenticated operator controls/runbooks.
- Tasks 3.4a–3.4c: route the pointer, ensure the durable run, build typed immutable recipient materialization, and prove checkpoint/crash ordering.
- Tasks 3.5a–3.6b: occurrence supersession/coalescing, pre-handoff suppression, fair claims, processor backpressure, and fanout telemetry.
- Phases 4–7: event/session handlers, reporter consent/API/UI/email, heavy-moderation occurrence, and safe reminder caller activation.

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
| Persisted fanout pointers have no consumer yet | Tasks 3.4a–3.4c separately own pointer routing/run creation, typed recipient materialization, and checkpoint ordering. |
| Partial SMTP operations patch hides unfinished cross-instance semantics | Task 1.6b must audit existing query/drain/settings changes before extending them; no duplicate implementation or silent overwrite. |
| Architecture guard currently fails on a real controller dependency | Task 1.6a keeps the guard strict and replaces the direct `IEmailService` resolution with a CQRS diagnostic seam. |

## Verification State

- Historical implementation evidence on 2026-07-17 records a Release build with 0 errors and full Domain 439, Application 2,449, Persistence 448, Infrastructure 832, and Architecture 240 passed/1 declared skip after the recipient migration landed.
- Task 0.5 independent verification is confirmed. Focused PostgreSQL pointer/migration coverage passed 7/7; missing-ID and valid-ID verifier tests passed 1/1 each; EF reported no pending model changes.
- Task 0.5 full directly affected suites passed: Domain 428, Application 2,420, Persistence 401, Infrastructure 818, API 1,830 with 3 declared skips, and Architecture 238 with 1 declared skip; zero failures.
- Tasks 0.6–0.7 focused evidence covers fenced claim/recovery, retry/dead-letter, redrive/auth, metrics, health, operator HAL, and real PostgreSQL effect behavior. Their implementation checkboxes are complete.
- The prior missing-column blocker is resolved by migration `20260717131038_NormalizeRecipientNotificationDelivery`. A fresh full API suite and explicit Mailpit lane remain unrecorded because testing was stopped; no phase requiring those gates is called fully verified.
- Task 1.5 is present in the main checkout, including migration `20260718203920_AddEmailDispatchContentRetention`; its focused Infrastructure evidence was reported before interruption, but Docker-backed PostgreSQL execution is not recorded.
- The Task 1.6 architecture guard is present and correctly fails on `InstanceSettingsController` directly resolving `IEmailService`. Fair pending selection, concurrency/rate/backlog settings, metrics, and health also have partial main-checkout changes and remain unchecked.
- A full compile currently stops in unrelated ATProto federation validator code with two `CS9135` errors; this is not evidence against the email patch, but it blocks a clean phase build until that shared baseline is repaired.
- This 2026-07-18 re-baseline changes planning docs only and runs no build, test, SMTP, Mailpit, provider, migration, or application command.
- Planning validation: `git diff --check -- dev/active/email-responsibility-architecture`.
- Runtime phases use one Release build and one selected non-browser project test; final intent-contract suites remain merge evidence. No gate uses weak broad filters or `--minimum-expected-tests 1`.

## Handoff Rule

Finish Tasks 1.6a–1.6d before Task 3.4a consumes persisted fanout pointers. Keep Task 5.9 disabled until the fresh full API evidence is recorded. The working tree currently contains unrelated ATProto/auth/location-privacy work; do not modify, stage, revert, or include it. Use only the main checkout and never create a linked worktree or `.worktrees` directory. At every handoff, synchronize progress, evidence, risks, and the next task across context, plan, and tasks.

## Handoff Notes

### Handoff — 2026-07-18 Europe/Brussels

- **Current state:** 20/50 implementation tasks are complete. Task 1.5 is implemented but uncommitted; Task 1.6a and 1.6b have partial main-checkout changes and remain unchecked.
- **Next action:** complete 1.6a without weakening the architecture guard, then reconcile 1.6b, 1.6c, and 1.6d before starting 3.4a.
- **Blockers:** Docker is unavailable for PostgreSQL retention evidence; a clean Release build is currently blocked by unrelated ATProto `CS9135` errors; fresh API and Mailpit merge evidence remains outstanding.
- **Modified files:** existing email retention, SMTP operations, health/metrics, admin/HAL, migration/schema/docs/tests, and architecture-guard surfaces are dirty in the main checkout. Unrelated ATProto/auth/location-privacy work is interleaved and must remain untouched.
- **Validation:** no command was run for this planning re-baseline. Previous focused evidence is retained as historical, not upgraded to a completed phase gate.
- **Documentation impact:** plan/context/tasks now use 50 independently mergeable slices, single-test phase gates, immediate resolve-without-replay redaction semantics, and explicit partial-work preservation.
- **Risks:** partial SMTP operations work may have incomplete per-tenant/cross-instance behavior; persisted fanout pointers still have no consumer.
- **Notes for next contributor/agent:** work only in the main checkout; preserve all current changes; do not create worktrees, stage broadly, revert shared files, or duplicate the partial Task 1.6 implementation.
