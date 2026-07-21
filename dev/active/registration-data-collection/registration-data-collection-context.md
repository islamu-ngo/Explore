<!-- ABOUTME: Working context for the Registration Data Collection & Participation Platform workstream. -->
<!-- ABOUTME: Records session state, key decisions, constraints, blockers, and resume instructions for implementation agents. -->

# Registration Data Collection & Participation Platform — Context

Last Updated: 2026-07-21 Europe/Brussels

## SESSION PROGRESS (2026-07-21 Europe/Brussels)

### ✅ COMPLETED
- Full read of the combined consultation document (`registration-data-collection-consultation.md`, Reports No. 1 + No. 2, 3,786 lines).
- Repository investigation with evidence: current registration aggregate, Event participation fields, custom-property system, incoming-webhook intake, endpoint classifications, UnitOfWork, idempotency, secrets, Cerbos, Blazor registration UX, empty migration baseline.
- Planning created: 15-phase plan (P0–P14), 16 architecture decisions (D1–D16), risk register, testing strategy, per-task acceptance criteria.
- **Re-baseline (2026-07-20, same day):** fully read `hi-events-report.md` (1,591 lines; Hi.Events pinned commit `9de8863a`) and integrated its decision/design/data-model findings (not its stack/code): new decisions **D17** (five ticket pricing modes — fixed / free / donation with 0-default input / pay-what-you-can with optional minimum / Leanpub-style sliding scale with dual "You pay"–"Organizer earns" sliders), **D18** (instance-admin-only platform fee policy + LaunchGood-style platform-contribution dropdown with DB-stored messaging, 0 preselected + 5/10/15/20% computed-amount options, defaults off/zero), **D19** (Hi.Events adopt/adapt/reject boundary + AGPLv3/removable-branding/provenance rule). Hi.Events §7 defects became binding acceptance criteria (deterministic pool locking, conditional completion transitions, per-line assignment multiset, display-IDs-never-authorize, answer/subject DB constraints); its UX lessons entered checkout/authoring tasks (state-machine recovery screens, countdown/abandon, copy-buyer-details, shared-capacity visualization); its commercial breadth became the deferred inventory in new Task 14.8. New tasks: 4.5, 5.10, 14.8 (85 → 88).
- **Licensing correction (2026-07-21):** D19 hardened to an absolute **no-code-copy** rule — ISLAMU Event's CLA enables dual-licensing (non-AGPLv3 licenses for recipients who cannot use AGPLv3), so importing any Hi.Events AGPLv3 code would contaminate the codebase and destroy that capability. Hi.Events is behavior/design/data-model reference only; clean-room implementation; the report's §10 code-reuse permission is explicitly overridden (plan §0 licensing note, §4.13, D19, risk register row, tasks standing gate).

### 🟡 IN PROGRESS
- Awaiting user review/approval of the implementation plan. **No implementation has started.**

### ⏭️ NEXT
1. User reviews the plan — especially D4 (aggregate replacement), D8 (`PublicTransactional`), D13 (clean-baseline/no-data-migration strategy), the new **D17/D18** (pricing modes + instance monetization), and the Phase ordering.
2. First implementation agent starts with **Task 0.1** (ADR-016) after approval.
3. Task 0.4 must resolve the migration-baseline ordering with the `optional-retained-erasure-authority` workstream **before any persistence artifact is generated**.
4. Refresh this context after the first implementation slice.

### ⚠️ BLOCKERS
- **Migration baseline:** `src/Explore.Persistence/Migrations/` contains zero files. The three init lanes (`ExploreDbContext`, `DataProtectionKeyContext`, `PrivacyErasureAuthorityDbContext`) are pending regeneration under the `platform-privacy-erasure` intent's user-approved exception. No `dotnet ef migrations add` for this workstream until that ordering is agreed (Task 0.4). Model/configuration/seeder code may proceed.
- External research MCP tools (anysearch, context7) were unavailable during planning; provider facts are consultation-cited (dated 2026-07) and must be re-verified in Tasks 10.1 / 11.1 / 12.1 before any binding activates.

## Quick Resume
1. Read this context and `registration-data-collection-tasks.md`.
2. Read only the current phase, §4 constraints, and any changed decisions from `registration-data-collection-plan.md`; do not reread the full plan on every resume.
3. The consultation file (`registration-data-collection-consultation.md`) is the deep reference — open a specific section only when a task cites it (§ references appear throughout the plan/tasks).
4. Start from the first unchecked high-priority task unless the user overrides.
5. Keep `tasks.md` current during implementation; update this context/plan only at their defined triggers (see plan §15).

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `dev/active/registration-data-collection/registration-data-collection-consultation.md` | Existing | Docs | Combined CTO consultation (Reports 1+2) — authoritative product/architecture source | Do not edit |
| `dev/active/registration-data-collection/hi-events-report.md` | Existing | Docs | Hi.Events research — behavior catalog, §7 defect-derived acceptance criteria, §9 adopt/adapt/reject, §11.4 deferred inventory | Do not edit; cited from Phases 4–8 + Task 14.8; never an architecture authority; **its §10 code-reuse permission is overridden — no Hi.Events code copy ever (CLA/dual-licensing, plan §4.13)** |
| `src/Explore.Domain/Event.cs` | Existing | Domain | Aggregate root; loses `IsUserReported`/`EventUrl` (P1), `IsRegistrationRequired` (P2), `Price`/`CurrencyCode` (P4); gains provenance fields | 219 lines today |
| `src/Explore.Domain/EventRegistrationIntent.cs` | Existing → deleted P5 | Domain | User-centric intent aggregate being replaced by `RegistrationOrder` | |
| `src/Explore.Domain/EventRegistration.cs` | Existing → rewired P5/P6 | Domain | Survives as materialized per-session admission row, participant-linked | Has `AtprotoRecordId` — federation decision in Task 5.9 |
| `src/Explore.Domain/EventContactShareConsent.cs` | Existing → rewired P5/P13 | Domain | Consent snapshot pattern; FK moves to order; subject becomes typed | |
| `src/Explore.Domain/Services/Registration/RegistrationPolicyRules.cs` | Existing | Domain | Pattern reference for all new pure rule classes | |
| `src/Explore.Domain/CustomPropertyDefinition.cs` / `CustomPropertyValue.cs` | Existing | Domain | Typed-metadata pattern + validation vocabulary to mirror (never reuse tables) — D1 | |
| `src/Explore.Domain/IncomingWebhookMessage.cs` / `IncomingWebhookEffectOutbox.cs` | Existing | Domain | Callback intake + durable effect pattern that provider callbacks extend — D7 | |
| `src/Explore.API/Attributes/EndpointClass.cs` | Existing | API | Gains `PublicTransactional` (P3, D8) | |
| `src/Explore.Persistence/ExploreDbContext.{DbSets,QueryFilters,SaveChanges}.cs` | Existing | Persistence | Central tenant/soft-delete enforcement — every new entity registers here | |
| `src/Explore.Persistence/Seed/LookupTableSeeder.cs` | Existing | Persistence | Stable IDs for ~25 new lookups across phases | Document ID ranges in `schemas/islamu-event.md` |
| `src/Explore.Persistence/Migrations/` | Existing (EMPTY) | Persistence | Blocked surface until Task 0.4 gate opens | See BLOCKERS |
| `src/Explore.Domain/Secrets/SecretDefinitionRegistry.cs` + `Enums/SecretScope.cs` | Existing | Domain | Provider credentials via bindings; scopes Instance/Tenant only (D15) | Org scope deferred |
| `cerbos/policies/islamuevent_event_registration.yaml` (+ event, consent) | Existing | AuthZ | Evolves to order semantics; new policies for claims/forms/tickets/exports | Parity tests required |
| `src/Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor` + `EventListRegistrationWorkflow.cs` | Existing → replaced P5 | Blazor | Current user-centric flow; replaced by checkout pages | Client uses generated `IEventApiClient` only |
| `docs/adr/ADR-016..018-*.md` | New (P0) | Docs | Decision records locking D1–D19 | ADR-018 also records Hi.Events rationale + AGPLv3 rule |
| `src/Explore.Domain/PlatformFeePolicy.cs`, `PlatformContributionSetting.cs`, `PlatformContributionOption.cs` | New (P4) | Domain | Instance-scoped monetization (D18): fee transparency + contribution content/options, versioned, defaults off/zero | Instance-admin-only Admin endpoints; tenant admins fail closed |
| `src/Explore.Domain/RegistrationOrderPlatformContribution.cs` + `IOrganizerEarningsCalculator` | New (P5) | Domain/Application | Buyer's contribution selection snapshot + pure decimal organizer-earnings math (D17/D18) | Contribution money segregated from organizer totals everywhere |
| `.claude/contract/intents.yaml` | Existing | Contract | Gains `registration-data-collection` intent (Task 0.3) | Model on `webhook-delivery-redesign` entry |
| `src/Explore.Domain/Registration*.cs`, `EventTicket*.cs`, `EventCapacityPool.cs`, `EventPublicAction*.cs`, `EventOrganizerClaim*.cs`, `EventParticipationConfiguration.cs` | New (P1–P9) | Domain | The target model — full inventory in plan §6 per phase | ~40 entities/lookups |
| `src/Explore.Infrastructure/Services/Registration/Providers/{Formbricks,Microsoft,Google}/**` | New (P10–P12) | Infrastructure | Capability-segregated provider adapters (D3) | Fixture-tested only |

## Key Decisions

Synchronized with plan §5 (D1–D19). Highest-consequence:

- **D1** New bounded context; custom-property primitives mirrored, tables never reused.
- **D2** Workflow → Requirement → Channel; five orthogonal provider dimensions; no provider enum.
- **D3** Capability-segregated interfaces; capability tuples fail closed for auto-finalization.
- **D4** `RegistrationOrder` buyer–order–participant–ticket aggregate replaces `EventRegistrationIntent` (deleted); `EventRegistration` survives participant-linked.
- **D5** One row per atomic typed answer + DB CHECK; sensitive values encrypted in a split table; no canonical JSON.
- **D7** Provider callbacks ride the existing incoming-webhook intake + durable effects; controllers never mutate registrations.
- **D8** New `EndpointClass.PublicTransactional` with mandatory rate-limit/antiforgery/idempotency/capability-token controls.
- **D10** Typed participation configuration; decorative prices deleted once the ticket catalog exists (`GENERAL_ADMISSION` default).
- **D13** Clean-baseline schema strategy: **no data migrations, no shims, no dual writes**; additive generated migrations only after the init lanes exist.
- **D16** Three independent state machines (order/attempt/submission); `ApprovalStatus` stays organizer-verdict-only.
- **D17** Five ticket pricing modes (`FIXED/FREE/DONATION/PAY_WHAT_YOU_CAN/SLIDING_SCALE`); buyer-chosen prices validated server-side against the **pinned** catalog version and snapshotted (`ChosenUnitPriceAmountSnapshot`); donation/PWYC input defaults to 0 when minimum is 0; sliding-scale = minimum + suggested + dual linked "You pay"/"Organizer earns" sliders showing the exact platform share.
- **D18** Instance monetization: `PlatformFeePolicy` (organizer-earnings transparency) + `PlatformContributionSetting` (LaunchGood-style tip — DB-stored heading/body, options `0` default + `5/10/15/20%` shown as "percentage — computed amount"); **instance-admin-only**, versioned, defaults off/zero; contribution money is instance-directed and never mixes with organizer earnings; positive total → `AwaitingPayment`, all-zero → free path.
- **D19** Hi.Events = behavior catalog only, **code source never**: adopt UX/workflow lessons, adapt concepts, reject its persistence/authorization/money/idempotency machinery; **zero code copy** — ISLAMU's CLA-based dual-licensing would be destroyed by third-party AGPLv3 code (authors are not CLA signatories); clean-room implementation from the report + plan only; report §10's code-reuse permission overridden; deferred breadth lives only in Task 14.8.

Pending decisions owned by tasks: `ActorId` rename vs narrowing (1.1), BFF antiforgery mechanism for guests (3.2), form localization model (7.8), Drive-file policy (12.4), AT Proto order federation (5.9 — default: defer).

## Constraints And Rules To Remember

- No matched single intent — fallback contract composed of `add-write-endpoint`, `add-get-endpoint`, `add-hal-link`, `add-cqrs-handler`, `add-ef-migration`, `update-repository-query`, `blazor-component-affordance`, `cerbos-policy-change`, `openapi-contract-change`; dedicated intent created in Task 0.3.
- Repo invariants (plan §4): entities-from-repositories, manual validators, Guid/int/long ID rules, HAL-only affordances, tenant filters, `IUnitOfWork` with **zero external IO inside transactions**, normalized `Id/Code/Name` lookups, controller authoring standard, Blazor isolation, ABOUTME headers, file-scoped namespaces.
- Consultation anti-pattern lists (§24 Report 1, §33 Report 2) are binding forbidden moves.
- Dev-mode waiver is active: backward compatibility artifacts are forbidden, not optional.
- NSwag/OpenAPI regeneration is a discrete, governed final step of any API-changing phase.
- Hi.Events reject-list is binding (plan §4.13): no mutable published prices, no JSON canonical answers, no public/display IDs as authorization, no cache-only idempotency, no float money, no attendee-derived inventory release, no external calls inside transactions; never add "Powered by Hi.Events" branding.
- **NO Hi.Events code copy — ever** (plan §4.13, D19): CLA/dual-licensing protection; no file, snippet, migration, SQL, or asset from the Hi.Events repo; no opening/transcribing/paraphrase-translating its source during implementation; clean-room from `hi-events-report.md` + plan only.
- Money rules (plan §4.14): decimal-only with explicit per-currency rounding; monetization defaults off/zero; instance-admin-only enablement — tenant-level enablement is a forbidden move.
- Baseline test-failure note: 15 pre-existing shared failures from upstream webhook fallout (see MEMORY) — snapshot at Phase 1 start; never attribute to this workstream.

## Validation Baseline

Every phase: `dotnet build --configuration Release --verbosity quiet` once, plus at most one `dotnet test --project tests/<selected>/<selected>.csproj --configuration Release --verbosity quiet` — selections per phase (plan §7): P0 Architecture, P1 Domain, P2 API, P3 Architecture, P4 Persistence, P5 Persistence, P6 Application, P7 Domain, P8 Application, P9 API, P10–P12 Infrastructure, P13 API, P14 Blazor.Client. Run only after all phase tasks complete. Never start the app, browser, Docker, Aspire, or live services for verification.

## Current Known Risks / Unknowns

- Migration-baseline deadlock (Task 0.4) — top blocker.
- Phase 5 ripple breadth (Task 5.9 sweep) — expect discovered tasks.
- Provider API drift vs 2026-07 citations (Tasks 10.1/11.1/12.1).
- Schema-hash and CHECK-constraint artifacts become frozen contracts at first publish (7.4/8.2).
- Full register in plan §13.

## Handoff Notes

### Handoff — 2026-07-21 Europe/Brussels (Hi.Events re-baseline 2026-07-20 + licensing correction 2026-07-21)
- **Current state:** Planning artifacts complete and re-baselined with `hi-events-report.md` findings + D17/D18/D19 (pricing modes, instance monetization, Hi.Events boundary); D19 hardened to an absolute no-code-copy rule (CLA/dual-licensing — report §10 overridden); 88 tasks across P0–P14; no runtime code changed; workstream Draft awaiting user review.
- **Next action:** User approval (review D4/D8/D13 and new D17/D18 first), then Task 0.1 (ADR-016); Task 0.4 ordering resolution before any migration generation.
- **Blockers:** Empty migration baseline owned by the erasure workstream; see BLOCKERS above.
- **Modified files:** Only the three planning artifacts in this directory (consultation file and hi-events-report untouched).
- **Validation:** None run for planning (docs-only); `git diff --check` on this directory is the proportional check.
- **Documentation impact:** None yet; §8 of the plan enumerates per-phase doc obligations (now incl. ADMIN_GUIDE/CONFIGURATION for monetization and the Task 14.8 deferred-design records).
- **Risks:** See risk register (plan §13 — now incl. money-math, monetization-abuse, and Hi.Events scope-creep rows).
- **Notes for next contributor/agent:** The consultation document is the deep specification; the Hi.Events report is the behavior/defect reference — both are cited by section (§n) from the plan/tasks instead of duplicated. Read cited sections lazily, per task. Do not begin Phase 5 without re-reading plan §17 (its density grew with 5.10 + sliding-scale UX; the split-candidate note is there).
