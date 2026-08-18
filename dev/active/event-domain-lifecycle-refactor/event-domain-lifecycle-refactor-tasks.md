<!-- ABOUTME: Executable task ledger for centralizing Event and EventSession lifecycle invariants in the Domain layer. -->
<!-- ABOUTME: Mirrors the implementation plan phases and keeps implementation work separate from phase verification evidence. -->

# Event Domain Lifecycle Refactor — Tasks

Last Updated: 2026-08-18 Europe/Brussels

## Workstream Checkpoint

- [x] Read the Event Draft Lifecycle Architecture Consultation.
- [x] Classify the planning task against repository intents and document the fallback contract.
- [x] Inspect current Domain entities, lifecycle handlers, readiness, HAL, persistence, generated-contract flow, and tests.
- [x] Check dev/active and dev/pause for overlapping lifecycle workstreams.
- [x] Attempt Context7 and Tavily research and record both tool blockers.
- [x] Complete source-free official-document fallback research and provenance register.
- [x] Reconcile the accepted user-owned ADR-026 with the repository-specific plan without modifying the ADR.
- [x] Run the planning-session Release build baseline.
- [x] Write synchronized plan, context, and task artifacts.
- [ ] User reviews and approves the five decisions in plan section 17.

Status: planning complete; implementation not started.

- Completed planning checkpoints: 9 of 10.
- Current priority: user review of plan section 17.
- Next approved implementation slice: Task 1.1.
- Discovered work: HAL parent-state DTO input and test-builder migration are included in Phases 3 and 4.
- Deferred work: new submit/review/approve/reject commands, a generic rules engine, Domain events, and schema changes are out of scope.

## Global Execution Rules

- Read all three artifacts once before the first task. On cold resume, read context/tasks first and only the relevant plan sections.
- Re-read current AGENTS.md, intents.yaml, path rules, and required skills before editing runtime files.
- Preserve unrelated dirty-worktree changes, especially the existing Quartz work.
- Do not remove or bypass manual FluentValidation.
- Do not add a state-machine/rules-engine package, Domain events, compatibility shims, or a migration.
- Do not hand-edit generated migrations, snapshots, OpenAPI, API inventory, or NSwag client output.
- Run each phase’s Release build and one listed test-project command once, at phase end, after its implementation tasks.
- Update this ledger and the context artifact immediately after every completed task/gate.

## Phase 1 — Establish Domain Lifecycle Authority

Goal: one fixed lifecycle authority per aggregate, with semantic entity methods and exhaustive Domain coverage.

### Task 1.1 — Add Event lifecycle rules and entity behavior

- [ ] Create src/Explore.Domain/Services/Lifecycle/EventLifecycleRules.cs with two ABOUTME lines.
- [ ] Encode the Event ordinary transition matrix in one pure switch/predicate implementation.
- [ ] Add query methods needed by handlers/readiness/HAL and Ensure methods used by Event.
- [ ] Add Event.Publish, Cancel, Archive, ApplyLightModeration, ApplyHeavyModeration, RestoreAfterLightModeration, EnsureDraftEditable, and SynchronizeFederatedLifecycle.
- [ ] Use a private mutation primitive; do not add public SetStatus.
- [ ] Require UTC occurredAt.
- [ ] Update UpdatedAt only for a real mutation and do not rotate ConcurrencyStamp.
- [ ] Return no mutation for same-target calls.
- [ ] Leave state and UpdatedAt unchanged for invalid transitions.
- [ ] Add exhaustive EventLifecycleRulesTests for every EventStatusEnum pair.
- [ ] Add Event entity tests for mutation, no-op, invalid atomicity, UTC, moderation, and federation seams.

Acceptance:

- [ ] Draft -> Published/Cancelled/Archived is allowed.
- [ ] Published -> Cancelled/light Moderated is allowed.
- [ ] Cancelled/Completed -> Archived is allowed.
- [ ] Moderated -> Published is available only through the explicit restoration method.
- [ ] Published -> Archived and all other undocumented ordinary edges are rejected.
- [ ] Heavy moderation remains a separate explicit override.

### Task 1.2 — Add EventSession lifecycle rules and entity behavior

- [ ] Create src/Explore.Domain/Services/Lifecycle/EventSessionLifecycleRules.cs with two ABOUTME lines.
- [ ] Encode schedule, publish, cancel, complete, archive, parent-state, and moderation predicates.
- [ ] Add EventSession.Publish, Cancel, Complete, Archive, ApplyParentModeration, and SynchronizeFederatedLifecycle.
- [ ] Route Reschedule through the fixed lifecycle schedule predicate.
- [ ] Preserve existing schedule projection and fixed/open-ended range behavior.
- [ ] Require UTC occurredAt and preserve invalid-operation atomicity.
- [ ] Add exhaustive EventSessionLifecycleRulesTests for all session and parent Event statuses.
- [ ] Add entity tests for publish schedule requirements, same-target no-op, invalid atomicity, and moderation/federation seams.

Acceptance:

- [ ] Publish requires Draft/Submitted/UnderReview/Approved, parent Published, and a valid schedule.
- [ ] Cancel requires a mutable parent and an allowed current session state.
- [ ] Complete requires session Published and parent Published.
- [ ] Archive requires Draft/Cancelled/Completed and a mutable parent.
- [ ] Schedule excludes Rejected/Cancelled/Archived/Completed/Moderated.

### Phase 1 Verification

- [ ] Run: dotnet build --configuration Release --verbosity quiet
  - Result:
- [ ] Run: dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
  - Result:
- [ ] Record verification evidence and changed files in event-domain-lifecycle-refactor-context.md.
- [ ] Mark Phase 1 complete in plan section 16.

## Phase 2 — Refactor Application Orchestration Without Removing Validation

Goal: every lifecycle handler uses Domain behavior while Application retains validators, authorization, tenancy, concurrency, dynamic policy/readiness, repository facts, and transactional side effects.

### Task 2.1 — Refactor Event command handlers and close the draft guard

- [ ] Preserve manual validator construction/execution in every affected handler.
- [ ] Add defined/supported EventStatusId validation for CreateEventDto/create flow.
- [ ] Add Event.EnsureDraftEditable before UpdateEventDraft mutations.
- [ ] Refactor PublishEventCommandHandler to call Event.Publish after dynamic readiness succeeds.
- [ ] Refactor CancelEventCommandHandler to call Event.Cancel.
- [ ] Refactor ArchiveEventCommandHandler to call Event.Archive and reject Published -> Archived.
- [ ] Refactor CreateEventCommandHandler to use only approved initial-state construction.
- [ ] Refactor ImportEventCommandHandler to use Draft/default approved construction.
- [ ] Move same-target detection before readiness and all side effects.
- [ ] Map Domain predicate failures to stable Application FailureCodes and safe messages.
- [ ] Use injected TimeProvider and generate IDs/times before retryable transaction delegates.
- [ ] Keep outbox/federation/reminder writes transactional.
- [ ] Ensure cache invalidation and success metrics occur only post-commit and only after real mutation.
- [ ] Update Event Application tests within this task.
- [ ] Update docs/DOMAIN.md within this task with the Event matrix and two-step validation ownership.

Acceptance:

- [ ] Validators still short-circuit invalid command shapes.
- [ ] Non-Draft UpdateEventDraft fails without field mutation or repository update.
- [ ] Invalid transitions call no repository update, outbox, federation planner, reminder, cache, or metric.
- [ ] Same-target requests succeed without side effects or timestamp changes.
- [ ] Valid transitions preserve required durable effects exactly once.

### Task 2.2 — Refactor moderation and EventSession handlers

- [ ] Refactor ModerateEventCommandHandler to use Event/session moderation methods.
- [ ] Refactor UnmoderateEventCommandHandler to keep moderation-record checks in Application and call Event restoration behavior.
- [ ] Refactor EventHeavyRedactionApplicator to call explicit heavy-moderation methods while preserving redaction and durable records.
- [ ] Change EventSessionLifecycleTransitionCommandHandlerBase from TargetStatus/direct assignment to an abstract semantic ApplyTransition call.
- [ ] Remove subclass-owned fixed transition matrices.
- [ ] Keep the base class responsible for shared validator, transaction, load, concurrency, hooks, and cache mechanics.
- [ ] Keep PublishEventSessionCommandHandler separate and call EventSession.Publish after dynamic readiness.
- [ ] Refactor ScheduleEventSessionCommandHandler to rely on Domain schedule eligibility.
- [ ] Refactor Cancel/Complete/Archive session handlers to call semantic methods.
- [ ] Suppress hooks/outbox/reminders/cache/metrics for no-op transitions.
- [ ] Update moderation and session Application tests within this task.

Acceptance:

- [ ] No normal Application lifecycle path assigns EventStatusId or EventSessionStatusId.
- [ ] Reversible moderation remains dependent on the persisted moderation record.
- [ ] Heavy moderation remains an explicit irreversible path.
- [ ] Session base-handler reuse remains without a new Event-handler mega-base.
- [ ] Parent Event state is enforced by Domain session methods.

### Task 2.3 — Make readiness reuse Domain fixed predicates

- [ ] Replace Event fixed terminal/transition checks in EventLifecycleReadinessEvaluator with EventLifecycleRules calls.
- [ ] Replace session fixed terminal/parent checks with EventSessionLifecycleRules calls.
- [ ] Preserve RequiredEventFields, RequiredSessionFields, ValidationProfile, policy provenance, and diagnostic messages.
- [ ] Preserve EventLocationPublicationReadinessEvaluator repository-fact validation.
- [ ] Update readiness unit tests for Domain parity and dynamic-policy preservation.
- [ ] Complete docs/DOMAIN.md updates for readiness versus invariant ownership.

Acceptance:

- [ ] One Domain predicate change flows to handler enforcement and readiness blockers.
- [ ] Dynamic tenant/profile required-field behavior is unchanged.
- [ ] Domain remains free of policy provider and repository dependencies.

### Phase 2 Verification

- [ ] Run: dotnet build --configuration Release --verbosity quiet
  - Result:
- [ ] Run: dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
  - Result:
- [ ] If the known durable-replay failure recurs unchanged, prove and record it as pre-existing; do not modify it.
- [ ] Record verification evidence and changed files in the context artifact.
- [ ] Mark Phase 2 complete in plan section 16.

## Phase 3 — Encapsulate Lifecycle State And Prove Persistence Compatibility

Goal: private lifecycle setters, explicit legitimate initialization/synchronization seams, and architectural/persistence enforcement.

### Task 3.1 — Privatize status setters and migrate initialization seams

- [ ] Make EventStatusId setter private.
- [ ] Make EventSessionStatusId setter private.
- [ ] Add Draft-default and explicit validated initial-state construction appropriate for EF and controlled creation/import/seed/test seams.
- [ ] Do not add a public generic status setter or bypass method.
- [ ] Update CreateEvent/ImportEvent construction if any Phase 2 temporary seam remains.
- [ ] Refactor AtprotoJetstreamRepository existing-entity refresh to SynchronizeFederatedLifecycle.
- [ ] Refactor federated new-entity materialization to explicit validated initial construction.
- [ ] Refactor SeedData Published construction.
- [ ] Refactor DatabaseSeeder promotions through semantic methods.
- [ ] Update shared Event/EventSession test builders first.
- [ ] Migrate remaining test object construction from direct setters.
- [ ] Prefer EF private-setter convention; add backing-field mapping only if a failing persistence test proves it necessary.

Acceptance:

- [ ] Entire solution compiles with both setters private.
- [ ] Native transitions cannot bypass Domain methods.
- [ ] Creation, federation, seed, and tests have explicit legitimate construction paths.
- [ ] No relational model change is introduced.

### Task 3.2 — Add architecture and persistence ratchets

- [ ] Add architecture tests asserting both status setters are non-public.
- [ ] Add architecture coverage preventing a generic lifecycle mutation seam in outward layers.
- [ ] Add PostgreSQL round-trip coverage for Event and EventSession private-setter materialization.
- [ ] Preserve status FK and session lifecycle constraint coverage.
- [ ] Keep the existing CI pending-model-change contract green.
- [ ] If CI reports pending changes, stop and diagnose entity/configuration drift; do not create or edit a migration.
- [ ] Update docs/TESTING.md within this task with focused lifecycle commands if the current document lacks them.

Acceptance:

- [ ] Architecture tests fail if a status setter becomes public.
- [ ] EF round trip restores correct statuses.
- [ ] dotnet ef migrations has-pending-model-changes reports no pending changes.
- [ ] No migration or snapshot file changes.

### Phase 3 Verification

- [ ] Run: dotnet build --configuration Release --verbosity quiet
  - Result:
- [ ] Run: dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
  - Result:
- [ ] Record verification evidence and changed files in the context artifact.
- [ ] Mark Phase 3 complete in plan section 16.

Required PR/CI evidence outside this phase’s single local test command:

- [ ] tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj passes in the PostgreSQL-enabled lane.
- [ ] Pending-model-change evidence is attached to the handoff.

## Phase 4 — Unify HAL And Regenerate The API Contract

Goal: fixed action affordances use Domain predicates, and session HAL receives parent Event status.

### Task 4.1 — Add parent status to session contracts

- [ ] Add ParentEventStatusId to EventSessionDto.
- [ ] Add ParentEventStatusId to EventSessionListDto.
- [ ] Map both from Event.EventStatusId in EventSessionMappingProfile.
- [ ] Update mapping/API tests.

Acceptance:

- [ ] Both session DTO shapes carry parent Event status.
- [ ] Mapping tests fail if Event.EventStatusId is not projected.

### Task 4.2 — Refactor HAL and regenerate governed artifacts

- [ ] Refactor EventLinkPolicy to call EventLifecycleRules for ordinary fixed actions.
- [ ] Preserve authorization and reversible-moderation eligibility conditions.
- [ ] Refactor EventSessionLinkPolicy to call EventSessionLifecycleRules.
- [ ] Delete the duplicated business-rule portion of EventSessionLifecycleAffordancePolicy.
- [ ] Retain a link-construction helper only if it has no transition logic.
- [ ] Add exhaustive Event HAL state/action tests.
- [ ] Add exhaustive session current/parent/action tests.
- [ ] Assert HAL never advertises a Domain-rejected action for the same inputs.
- [ ] Update docs/API.md with lifecycle action, idempotency, and HAL semantics.
- [ ] Update docs/API_CHANGELOG.md with the intentional pre-v1 breaking changes.
- [ ] Let the single phase-end Release build generate schemas/openapi_islamu-event.json and EventApiClient.g.cs.
- [ ] Include ApiContractInventory_Generate_WritesMarkdownToDocs in the one combined phase-end API test command.
- [ ] Inspect generated diffs for only intentional changes.
- [ ] Do not hand-edit any generated artifact or add a compatibility alias.

Acceptance:

- [ ] Published Event has Cancel and moderation actions but no Archive.
- [ ] Draft Event has Publish, Cancel, and Archive when authorized.
- [ ] Session Publish/Complete links require parent Published.
- [ ] HAL remains the client action authority; no Blazor status/claim gate is added.
- [ ] ParentEventStatusId appears in OpenAPI and generated client detail/list DTOs.
- [ ] Contract inventory is current.
- [ ] API changelog records Published-to-Archived removal, same-target no-op success, and parent status addition.
- [ ] Blazor client compiles against the regenerated contract.

### Phase 4 Verification

- [ ] Run: dotnet build --configuration Release --verbosity quiet
  - Result:
- [ ] Run: dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventLifecycleHateoasPolicyTests/*|/*/*/*/ApiContractInventory_Generate_WritesMarkdownToDocs" --minimum-expected-tests 2 --no-progress
  - Result:
- [ ] Record generated artifact diff and verification evidence in the context artifact.
- [ ] Mark Phase 4 complete in plan section 16.

## Final PR/Handoff Checklist

- [ ] All four phases and gates are complete.
- [ ] Required PostgreSQL persistence lane passes.
- [ ] Explore.Blazor.Client.Tests passes in its applicable lane.
- [ ] Stable OpenAPI invariants pass.
- [ ] Generated OpenAPI, inventory, and NSwag client are deterministic.
- [ ] No pending EF model changes.
- [ ] No migration/snapshot, new dependency, compatibility shim, or generic state machine was added.
- [ ] Domain/Application/Persistence/API dependency direction remains valid.
- [ ] Validators, authorization, tenancy, concurrency, dynamic policy/readiness, and location/moderation repository checks remain.
- [ ] Invalid/no-op lifecycle operations have no durable or cache side effects.
- [ ] docs/DOMAIN.md, docs/API.md, docs/API_CHANGELOG.md, and docs/TESTING.md match implementation.
- [ ] event-domain-lifecycle-refactor-context.md contains the final teaching handoff and exact verification evidence.
- [ ] git diff --check passes for the complete implementation diff.

## Quick Resume

1. On first implementation start, read all three artifacts once.
2. On a cold resume, read context and this task ledger first, then only the relevant plan sections.
3. Reconcile this ledger with git diff and current tests.
4. Resume the first unchecked task only.
5. Do not run a phase gate until all tasks in that phase are complete.
