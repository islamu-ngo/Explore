<!-- ABOUTME: Working context for the Registration Data Collection & Participation Platform workstream. -->
<!-- ABOUTME: Records session state, key decisions, constraints, blockers, and resume instructions for implementation agents. -->

# Registration Data Collection & Participation Platform — Context

Last Updated: 2026-07-26 Europe/Brussels

## SESSION PROGRESS (2026-07-26 Europe/Brussels)

### ✅ COMPLETED
- Full read of the combined consultation document (`registration-data-collection-consultation.md`, Reports No. 1 + No. 2, 3,786 lines).
- Repository investigation with evidence: current registration aggregate, Event participation fields, custom-property system, incoming-webhook intake, endpoint classifications, UnitOfWork, idempotency, secrets, Cerbos, Blazor registration UX, empty migration baseline.
- Planning created: 15-phase plan (P0–P14), 16 architecture decisions (D1–D16), risk register, testing strategy, per-task acceptance criteria.
- **Re-baseline (2026-07-20, same day):** fully read `hi-events-report.md` (1,591 lines; Hi.Events pinned commit `9de8863a`) and integrated its decision/design/data-model findings (not its stack/code): new decisions **D17** (five ticket pricing modes — fixed / free / donation with 0-default input / pay-what-you-can with optional minimum / Leanpub-style sliding scale with dual "You pay"–"Organizer earns" sliders), **D18** (instance-admin-only platform fee policy + LaunchGood-style platform-contribution dropdown with DB-stored messaging, 0 preselected + 5/10/15/20% computed-amount options, defaults off/zero), **D19** (Hi.Events adopt/adapt/reject boundary + AGPLv3/removable-branding/provenance rule). Hi.Events §7 defects became binding acceptance criteria (deterministic pool locking, conditional completion transitions, per-line assignment multiset, display-IDs-never-authorize, answer/subject DB constraints); its UX lessons entered checkout/authoring tasks (state-machine recovery screens, countdown/abandon, copy-buyer-details, shared-capacity visualization); its commercial breadth became the deferred inventory in new Task 14.8. New tasks: 4.5, 5.10, 14.8 (85 → 88).
- **Licensing correction (2026-07-21):** D19 hardened to an absolute **no-code-copy** rule — ISLAMU Event's CLA enables dual-licensing (non-AGPLv3 licenses for recipients who cannot use AGPLv3), so importing any Hi.Events AGPLv3 code would contaminate the codebase and destroy that capability. Hi.Events is behavior/design/data-model reference only; clean-room implementation; the report's §10 code-reuse permission is explicitly overridden (plan §0 licensing note, §4.13, D19, risk register row, tasks standing gate).
- **Studio integration re-baseline (2026-07-26):** inspected the implemented workspace shell and tests from `dynamic-event-management-ui`; added D20 and canonical organizer routes under `/studio/**`. Event navigation remains HAL-derived from the shared `EventDto`; actor-level Orders/Attendees use one proposed private/no-store `StudioContextDto`. Tasks were reworded in place, so the plan remains 15 phases / 88 tasks.
- **Phase 0 implementation (2026-07-26):** user approved the plan; ADR-016, ADR-017, and ADR-018 now lock the bounded-context, authority, and order/ticket decisions; the `registration-data-collection` contribution-contract intent and cold-start benchmark scenario were added.
- **Migration ordering resolved (Task 0.4):** the privacy-erasure-owned `ExploreDbContext`, `DataProtectionKeyContext`, and `PrivacyErasureAuthorityDbContext` init lanes exist. Registration must not regenerate or rewrite them; the first allowed registration migration is a generated additive migration after its owning phase's model/configuration/seeder changes are complete.
- **Phase 1 domain foundation:** `EventAuthorityRules`, typed provenance lookups, `ExternalActionUrl`, public action rules/entities/lookups, and organizer claim transitions are implemented with 473/473 Domain tests green. Tasks 1.2 and 1.3 are complete; Task 1.1 remains open because runtime `IsUserReported`/`EventUrl` and their callers still exist.
- **Task 1.4 model-ready persistence:** six EF configurations, six DbSets, named tenant/soft-delete filters, runtime missing-row lookup repair, Respawn lookup preservation, DBML updates, and failing-first lookup/filter tests are implemented. The red lookup run compiled and failed only for the absent mappings/seeds/filters before production changes.
- **Required provenance writers complete:** canonical CreateEvent selects `COMMUNITY_REPORTED` vs `ORGANIZER_CREATED` and assigns submitter/organizer authority explicitly; generic import selects `IMPORTED`; ATProto materialization selects `FEDERATED`; development catalog seeding selects `ORGANIZER_CREATED`. No valid implicit provenance default was added; the required lookup FK rejects the CLR sentinel `0`.
- **Global Actor compatibility:** the concurrent actor-lifecycle workstream makes Actor global and moves tenant authority to concrete participation/presentation. New `OrganizerActorId` and `EventOrganizerClaim.ClaimantActorId` persistence therefore use simple global Actor FKs; registration authorization must validate tenant participation separately and must not recreate composite tenant Actor keys.
- **Legacy backend Event fields removed:** runtime Domain, Application DTOs/validators/handlers, moderation metadata, federation publication mapping, and MCP descriptors no longer use `Event.IsUserReported` or the external `Event.EventUrl` property. `EventActorResult.IsCommunitySubmission` now drives typed provenance. Historical migration designers remain untouched, canonical first-party `IPublicUrlBuilder.GetEventUrl` remains unrelated, and generated Blazor contracts await the governed Phase 1 contract regeneration.
- **Task 1.5 Application source implemented:** `EventDto` exposes normalized provenance and reviewed `Active` public actions; concrete entity-first repositories support action and claim access paths; action create/update/delete/query handlers enforce HTTPS destinations, one primary action, review reset, tenant checks, and concurrency; claim submit/withdraw/event-query/claimant-query/review handlers enforce claimant-actor control. Approval persists claim + Event once inside a retryable transaction and exact decision retries return success without reassigning authority. Correction suggestions and unsafe-link reports reuse the hardened event-report intake via stable subcategory codes instead of duplicating report persistence.
- **Task 1.6 API/Cerbos/HAL source implemented:** public-action and organizer-claim controllers use named routes, endpoint classifications, rate policies, typed responses, HAL assemblers/policies, and stored-ID-only redirect resolution. Event and claim authorization are represented in local fallback plus bundled Cerbos policy/schema/test files. OpenAPI and the generated Blazor client contain the new operations and HAL schemas; API/AUTHORIZATION/changelog docs describe the contract.

### 🟡 IN PROGRESS
- Phase 1 Task 1.4: persistence model is ready; executable verification and generated migration remain gated.
- Phase 1 Task 1.5: source implementation is ready; focused tests exist but the shared actor-lifecycle test compilation must settle before execution.
- Phase 1 Task 1.6: source, generated OpenAPI/client evidence, and docs are ready; executable API/Cerbos contract verification remains blocked.

### ⏭️ NEXT
1. Let the intentional global-Actor/concrete-participation refactor restore the API test fixtures; do not revert or modify that workstream.
2. Once `Event.API.IntegrationTests` compiles, run the Task 1.6 controller/HAL contracts and regenerate `docs/API_CONTRACT_INVENTORY.md` through its governed test.
3. Run bundled Cerbos tests when the pinned CLI is available. After actor Task 2.1 stabilizes the snapshot, run Task 1.4/1.5 focused tests and generate the additive Phase 1 migration.

### ⚠️ BLOCKERS
- **Unrelated architecture test:** Phase 0 ran 304 architecture tests: 302 passed, 1 skipped, and the pre-existing `EventReportConsentArchitectureTests.ConsentAffordanceShouldExistOnlyOnMyReportsPolicies` failure remains (`reporterUserId.ToString()` expectation versus the dirty-worktree handler's `resolvedReporterUserId.ToString()`). Registration changes did not touch that surface.
- **Context7 quota:** Context7 was invoked on 2026-07-26 but its monthly quota was exhausted. Exa research used official vendor documentation; provider facts still require the dated conformance evidence in Tasks 10.1 / 11.1 / 12.1 before any binding activates.
- **Concurrent test/migration blocker (intentional):** the global Actor/concrete-participation source model now allows a dependency-inclusive Release build of `Explore.Persistence` with zero errors. Its test fixtures remain mid-refactor (`Event.Application.UnitTests --no-dependencies` currently has 306 actor-owned compile errors), and its reserved Task 2.1 migration/snapshot has not landed. Registration adapted its new Actor FKs but must not scaffold ahead of that migration.
- **Migration correctness gate:** required `EventProvenanceTypeId` has no default by design. The three production Event writers assign it explicitly and backend legacy Event fields are removed, but migration generation remains blocked until the global-Actor owning schema compiles and its migration ordering is stable.
- **Cross-workstream migration ordering:** `atproto-federation-actor-lifecycle` explicitly reserves its Task 2.1 for the deterministic global-Actor/concrete-participation migration and forbids scaffolding before its Phase 1 manifests complete. Because registration also changes Event/Actor FKs and the same `ExploreDbContextModelSnapshot`, registration's Task 1.4 migration must be generated after that Task 2.1 migration, never concurrently or from the current intermediate model.
- **Task 1.6 API test blocker:** `Event.API.IntegrationTests` currently fails compilation on 29 actor-lifecycle-owned fixture errors involving unresolved `ActorId`, `ActorTypeId`, and `OrganizationId`. The new Task 1.6 contract files have no scoped diagnostics, but they cannot execute and the generated API contract inventory cannot be refreshed until the shared project compiles.
- **Task 1.6 Cerbos test blocker:** policy YAML/JSON parses successfully, but `cerbos compile cerbos/policies --tests=cerbos/tests` cannot run because the `cerbos` binary is not installed in this environment.
- **Focused architecture baseline:** the supported TUnit `--treenode-filter` run executed 24 relevant tests: 23 passed. The one failure reports existing implicit-action calls in `UserLinkPolicy`, `EventSeriesLinkPolicy`, and `EventSessionLanguageLinkPolicy`; no Task 1.6 policy appears in the violation list.

## Task 1.4 Verification Evidence (2026-07-26)

- Failing-first lookup tests compiled before production persistence changes and failed 3/3 only for missing Phase 1 tables, seeds, filters, and indexes.
- AFT reports zero diagnostics in the scoped registration persistence files (the C# LSP itself is unavailable); focused `git diff --check` passes.
- Dependency-inclusive `dotnet build src/Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet` now passes with zero errors. Application and Persistence dependency-skipping builds also pass with zero errors. Executable Application tests remain blocked before execution by 306 actor-owned test-fixture compile errors; registration's new test files produce no compiler errors or LSP diagnostics.
- No registration migration was generated, no historical migration/snapshot was edited, and no speculative public-action/claim repositories were added.
- A compile-enforcement probe temporarily marked `EventProvenanceTypeId` as a C# `required` member and exposed the previously omitted generic import writer plus more than 100 unrelated test fixtures. The broad fixture churn was reverted; `ImportEventCommandHandler` now explicitly selects `IMPORTED`, its focused test asserts the value, and a dependency-skipping Application build reaches only 21 actor-lifecycle-owned errors with no registration error.

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
| `src/Explore.Persistence/Migrations/` | Existing | Persistence | Three privacy-erasure-owned generated init lanes form the baseline; registration adds later migrations only | Never regenerate history or hand-edit snapshots |
| `src/Explore.Domain/Secrets/SecretDefinitionRegistry.cs` + `Enums/SecretScope.cs` | Existing | Domain | Provider credentials via bindings; scopes Instance/Tenant only (D15) | Org scope deferred |
| `cerbos/policies/islamuevent_event_registration.yaml` (+ event, consent) | Existing | AuthZ | Evolves to order semantics; new policies for claims/forms/tickets/exports | Parity tests required |
| `src/Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor` + `EventListRegistrationWorkflow.cs` | Existing → replaced P5 | Blazor | Current user-centric flow; replaced by checkout pages | Client uses generated `IEventApiClient` only |
| `src/Explore.Blazor.Client/Components/Shell/Workspaces/StudioWorkspaceNavigation.razor` + `StudioEventNavigation.razor` | Existing → extended P2/P4/P5/P6/P7/P9 | Blazor | Single contextual organizer sidebar; actor navigation swaps to event navigation | Actor links come from `StudioContextDto`; event links come from shared `EventDto._links` |
| `src/Explore.Blazor.Client/Pages/Studio/StudioEventShell.razor` + `StudioEventContextState.cs` + `Routes.razor` | Existing → extended | Blazor | Canonical `/studio/**` event shell, shared event resource, and centralized routes | Never create parallel `/events/manage` organizer navigation |
| `docs/adr/ADR-016..018-*.md` | New (P0) | Docs | Decision records locking D1–D19 | ADR-018 also records Hi.Events rationale + AGPLv3 rule |
| `src/Explore.Domain/PlatformFeePolicy.cs`, `PlatformContributionSetting.cs`, `PlatformContributionOption.cs` | New (P4) | Domain | Instance-scoped monetization (D18): fee transparency + contribution content/options, versioned, defaults off/zero | Instance-admin-only Admin endpoints; tenant admins fail closed |
| `src/Explore.Domain/RegistrationOrderPlatformContribution.cs` + `IOrganizerEarningsCalculator` | New (P5) | Domain/Application | Buyer's contribution selection snapshot + pure decimal organizer-earnings math (D17/D18) | Contribution money segregated from organizer totals everywhere |
| `.claude/contract/intents.yaml` | Existing | Contract | Gains `registration-data-collection` intent (Task 0.3) | Model on `webhook-delivery-redesign` entry |
| `src/Explore.Domain/Registration*.cs`, `EventTicket*.cs`, `EventCapacityPool.cs`, `EventPublicAction*.cs`, `EventOrganizerClaim*.cs`, `EventParticipationConfiguration.cs` | New (P1–P9) | Domain | The target model — full inventory in plan §6 per phase | ~40 entities/lookups |
| `src/Explore.Infrastructure/Services/Registration/Providers/{Formbricks,Microsoft,Google}/**` | New (P10–P12) | Infrastructure | Capability-segregated provider adapters (D3) | Fixture-tested only |

## Key Decisions

Synchronized with plan §5 (D1–D20). Highest-consequence:

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
- **D20** Organizer registration operations extend the implemented Studio workspace and single contextual sidebar. Event sections are gated only by the shared event `_links`; cross-event Orders/Attendees use one authenticated `StudioContextDto` from `GET /api/studio/context?actorId={optionalActorHint}`. Public/guest checkout remains under `/registration/**`; no role-derived links, per-link probe calls, dead placeholders, or parallel management shell.

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
- Studio is the organizer UI boundary (D20): canonical `/studio/**` routes, existing navigation replacement model, and HAL-only section visibility. `configure-participation`, ticket/capacity management, orders, participants, workflow, and channel/health relations map to Registration, Ticketing, Orders, Attendees, Forms, and Integrations respectively; export remains an Attendees action.
- Baseline test-failure note: 15 pre-existing shared failures from upstream webhook fallout (see MEMORY) — snapshot at Phase 1 start; never attribute to this workstream.

## Validation Baseline

Every phase: `dotnet build --configuration Release --verbosity quiet` once, plus at most one `dotnet test --project tests/<selected>/<selected>.csproj --configuration Release --verbosity quiet` — selections per phase (plan §7): P0 Architecture, P1 Domain, P2 API, P3 Architecture, P4 Persistence, P5 Persistence, P6 Application, P7 Domain, P8 Application, P9 API, P10–P12 Infrastructure, P13 API, P14 Blazor.Client. Run only after all phase tasks complete. Never start the app, browser, Docker, Aspire, or live services for verification.

## Current Known Risks / Unknowns

- Migration history ownership — resolved for ordering; registration must remain additive and never alter the privacy-erasure-owned init lanes.
- Phase 5 ripple breadth (Task 5.9 sweep) — expect discovered tasks.
- Provider API drift vs 2026-07 citations (Tasks 10.1/11.1/12.1).
- Actor-level Studio context authority (Task 5.7): optional actor hints must fail closed and must not disclose role booleans or tenant-wide event data.
- Schema-hash and CHECK-constraint artifacts become frozen contracts at first publish (7.4/8.2).
- Full register in plan §13.

## Handoff Notes

### Handoff — 2026-07-26 Europe/Brussels (Studio integration re-baseline)
- **Current state:** Planning artifacts now use the implemented Studio workspace as the organizer UI boundary. D20 defines the single-sidebar/HAL model, canonical routes, and the minimal `StudioContextDto`; affected P2/P4/P5/P6/P7/P9/P13/P14 tasks were reworded without changing the 88-task count. No runtime code changed; workstream Draft awaiting user review.
- **Next action:** User approval (review D4/D8/D13/D17/D18/D20), then Task 0.1 (ADR-016); Task 0.4 ordering resolution before any migration generation.
- **Blockers:** Empty migration baseline owned by the erasure workstream; see BLOCKERS above.
- **Modified files:** Only the three planning artifacts in this directory (consultation file and hi-events-report untouched).
- **Validation:** Release baseline build passed before planning edits. After the docs refresh, `aft_inspect` reported no diagnostics and `git diff --check` passed. `Event.Architecture.Tests` ran 304 tests: 302 passed, 1 skipped, and 1 unrelated existing test failed (`EventReportConsentArchitectureTests.ConsentAffordanceShouldExistOnlyOnMyReportsPolicies` expects `reporterUserId.ToString()` while the current handler uses `resolvedReporterUserId.ToString()`). Existing `System.Security.Cryptography.Xml` NU1903 warnings remain.
- **Documentation impact:** Active dev docs only; implementation phases own product/API documentation when behavior ships.
- **Risks:** Actor-context authorization/disclosure joins the existing migration, Phase 5 density, money-math, provider drift, and licensing risks.
- **Notes for next contributor/agent:** Extend `StudioWorkspaceNavigation` / `StudioEventNavigation`; do not revive `Pages/Events/Manage/**`. Check-in, Communications, and Analytics remain absent until separate implementations provide both route and HAL relation.
