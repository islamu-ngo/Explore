<!-- ABOUTME: Resume context for the CTO-revised lifecycle-email implementation workstream. -->
<!-- ABOUTME: Records verified repository state, fixed architecture decisions, prerequisites, risks, and the next task. -->

# Email Lifecycle Delivery Context

> **Status:** Draft — Phase 0 approval required before runtime implementation  
> **Last Updated:** 2026-07-17 Europe/Brussels  
> **Progress:** 0/43 tasks complete  
> **Current phase:** Phase 0 — architecture and policy baseline  
> **Next task:** 0.1 approve the logical intent, channel-delivery, and transport-work model

## Objective

Finish transactional product email for registration transitions, critical event/session changes, reporting, local/Coop moderation decisions, heavy-moderation attendee availability, and reminders by extending the existing PostgreSQL EmailDispatch and MailKit SMTP implementation. Mailpit remains the local development sink.

## Resume Brief

Do not start the previous Task 1.1. The Senior CTO review rejected the no-migration/shared-ID convention and required a Phase 0 architecture gate.

The revised core is:

```text
NotificationIntent = one business occurrence + one recipient
NotificationDelivery = one channel decision and outcome
EmailDispatchOutbox = durable SMTP execution state
```

Add a real EF relationship and migration. New email rows keep independent IDs and carry `NotificationIntentId` plus `RecipientUserId`. Delivery rows identify channel and explicit delivery policy. One recipient operation atomically creates the intent, configured channel rows, in-app notification, and email outbox row.

High fanout uses an immutable `NotificationFanoutOccurrence` with an audience cutoff and before/after snapshot. `NotificationFanoutRun` remains mutable execution/checkpoint state. The worker never recreates an old change from current event/session data.

Reporter consent must split into case updates and follow-up contact, with an authenticated HAL-controlled withdrawal endpoint. `Reporting:CaseSlaHours` is the only SLA source; receipt copy says normally within the configured hours, not configurable business days.

Coop’s missing incoming-webhook route is a production defect independent of email. Tasks 0.5–0.7 form a separately reviewed prerequisite and must be complete before Phase 5 source convergence. Osprey remains signal-only.

## Verified Repository Facts

- `SmtpEmailService` uses MailKit; Aspire starts Mailpit in local profiles.
- `EmailDispatchOutbox` is already the PostgreSQL SMTP ledger with claims, attempts, receipts, retry, unknown, dead-letter, park/replay, and tenant pause.
- TickerQ and RabbitMQ are pointer/wake-up mechanisms; they are not business authorities.
- `NotificationIntent` has tenant/dedup uniqueness.
- `NotificationDelivery` currently links intent to optional email outbox but has no channel, policy, in-app notification link, or unique channel constraint.
- `EmailDispatchOutbox` currently has `UserId`, no `NotificationIntentId`, and uniqueness on `(TenantId, SourceType, SourceId, Kind)`.
- `DefaultNotificationOrchestrator` creates intent/delivery audit state but does not atomically create the SMTP row.
- registration confirmation and tenant-administrator invitation are live email flows.
- registration intent/registration repositories currently own serializable transactions; `EfCoreUnitOfWork` explicitly rejects nested transactions.
- event/session cancellation and schedule/location/timezone handlers do not enqueue attendee email.
- current event-wide fanout includes parent statuses `Pending`, `Approved`, and `Waitlisted`, but has only a user-ID cursor and no audience cutoff/session coverage lease.
- `EventReport.ReporterContactConsent` and the dialog label cover only contact when more context is needed.
- My Reports already has authenticated list/detail API, HAL policies, Blazor page, and generated NSwag contracts suitable for consent withdrawal.
- `ExecuteReportDecisionCommandHandler` is the safe post-enforcement notification seam.
- Coop callbacks are verified and retained but ignored because no concrete incoming handler routes `moderation.coop.decision`.
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
24. Sent/skipped content is redacted after 180 days; unresolved rows retain replay material until resolution, then use the same window. Redacted work cannot replay.
25. Multi-tenant fairness, backpressure, SMTP rate limiting, metrics, alerts, cleanup, pause, inspect, and replay controls are release requirements.

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

If this prerequisite is disabled or incomplete, local reporting email may proceed, but Coop decision email remains disabled and documentation must say so.

## Likely File Map

Existing areas to modify:

- `src/Explore.Domain/NotificationIntent.cs`
- `src/Explore.Domain/NotificationDelivery.cs`
- `src/Explore.Domain/NotificationIntentLookups.cs`
- `src/Explore.Domain/EmailDispatchOutbox.cs`
- `src/Explore.Domain/NotificationFanoutRun.cs`
- `src/Explore.Domain/EventReport.cs`
- `src/Explore.Domain/Enums/NotificationIntentEnums.cs`
- `src/Explore.Persistence/Configurations/Entities/NotificationIntentConfiguration.cs`
- `src/Explore.Persistence/Configurations/Entities/EmailDispatchOutboxConfiguration.cs`
- `src/Explore.Persistence/Configurations/Entities/NotificationFanoutRunConfiguration.cs`
- `src/Explore.Persistence/Configurations/Entities/EventReportConfiguration.cs`
- `src/Explore.Persistence/Seed/LookupTableSeeder.cs`
- `src/Explore.Persistence/Repositories/NotificationIntentRepository.cs`
- `src/Explore.Persistence/Repositories/EmailDispatchOutboxRepository.cs`
- `src/Explore.Persistence/Repositories/EventRegistrationIntentRepository.cs`
- `src/Explore.Persistence/Repositories/EventRegistrationRepository.cs`
- `src/Explore.Persistence/EfCoreUnitOfWork.cs`
- `src/Explore.Application/Notifications/DefaultNotificationOrchestrator.cs`
- `src/Explore.Application/Services/RegistrationNotificationDeliveryService.cs`
- registration, event/session, reporting, moderation, scheduler, serialization, and dispatcher handlers/services
- `src/Explore.Infrastructure/EmailDispatchDrainService.cs`
- `src/Explore.API/Controllers/EventReportsController.cs`
- event-report route names, HAL policies, assemblers, OpenAPI contract, and generated NSwag client
- report dialog, My Reports page, reporting client service, and related CSS/tests

Planned new concepts; final names must be verified before creation:

- `NotificationDeliveryPolicy` lookup and configuration
- `NotificationFanoutOccurrence` entity/configuration/repository
- `IRecipientNotificationMaterializer`
- `IEmailDispatchEligibilityEvaluator`
- `NotificationFanoutOccurrenceRequested` pointer contract
- generic lifecycle fanout worker/coordinator
- email retention/redaction cleanup service
- reporter consent update command/DTO/validator
- focused report email template/factory
- standalone Coop incoming-handler/effect-pointer repair

## Risks and Controls

| Risk | Control |
|---|---|
| Legacy outbox rows lack intents | Deterministic migration backfill before making FK non-null. |
| PostgreSQL transaction is aborted after unique violation | Catch only outside the UoW; recover in a fresh transaction or use `ON CONFLICT DO NOTHING`. |
| Repeated edits create storms | Five-minute coalescing plus newer-unsent supersession; cancellation bypasses delay. |
| Mutable state corrupts old messages | Immutable safe before/after occurrence snapshot. |
| Live audience drifts | Frozen creation cutoff plus current eligibility recheck. |
| Consent/address changes after queue | Central dispatch-time evaluator. |
| Large tenant starvation | Fair per-tenant selection, bounded lease/concurrency/rate, backlog thresholds. |
| Retained body/address becomes privacy debt | Parent-aware 180-day redaction and immediate tenant-deletion handling. |
| Heavy moderation leaks identity | Dedicated generic/linkless policy and negative tests. |
| Coop defect expands the email PR | Independent Tasks 0.5–0.7 prerequisite with its own acceptance/gate. |

## Verification State

- Baseline Release build on 2026-07-17: passed, 25 projects, 0 warnings, 0 errors.
- No runtime code, migration, test, Mailpit message, provider call, or external state is changed by this planning update.
- Planning validation after edits: `git diff --check -- dev/active/email-responsibility-architecture`.
- Runtime phases use full directly affected project tests; no weak broad filters or `--minimum-expected-tests 1`.

## Handoff Rule

Do not start old Task 1.1. Start at Task 0.1 only after review of the revised three-document set. Complete the independently scoped Coop prerequisite before Task 5.9. At every handoff, synchronize progress, current task, evidence, changed files, and risks across context, plan, and tasks.
