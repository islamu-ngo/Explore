<!-- ABOUTME: Repository-grounded implementation plan for centralizing Event and EventSession lifecycle invariants in the Domain layer. -->
<!-- ABOUTME: Preserves Application validation and orchestration while removing duplicated transition rules from handlers and HAL policies. -->

# Event Domain Lifecycle Refactor — Implementation Plan

Last Updated: 2026-08-18 Europe/Brussels

## 0. Metadata And Planning Contract

| Field | Value |
|---|---|
| Original request | Keep nullable draft-capable domain data and all existing scenario-specific validation, while adding Domain-owned Event and EventSession lifecycle behavior so handlers cannot drift or forget a transition rule. |
| Workstream | dev/active/event-domain-lifecycle-refactor |
| Status | Draft — awaiting user review before implementation |
| Complexity | XL: cross-layer invariant refactor across two entities, CQRS handlers, federation/import seams, HAL, generated API contracts, persistence materialization, and a large test-construction surface |
| Primary intent | No exact intent currently exists for centralizing an existing aggregate lifecycle. Use the documented fallback contract below. |
| Supporting intents | add-cqrs-handler conventions; add-hal-link; openapi-contract-change; ip-clean-room-governance; testing |
| Layers in scope | Domain, Application, Persistence, API/HATEOAS, generated client contract, tests, lifecycle documentation |
| Schema impact | None expected. Status FKs and lookup rows already exist; setter encapsulation is an object-model change, not a relational-model change. |
| Dependency impact | None. Reuse current Domain patterns, FluentValidation, MediatR, EF Core, HAL policies, transactional outbox, and TimeProvider. |
| Backward compatibility | Explicitly not required. Remove obsolete behavior and regenerate pre-v1 contracts without aliases, shims, or dual paths. |
| Accepted architecture decision | docs/adr/ADR-026-domain-owned-lifecycle-and-contextual-completeness.md; discovered as an existing untracked user-owned artifact and not modified by this planning workstream. |

### 0.1 Fallback Contribution Contract

Because .agents/contract/intents.yaml has no exact lifecycle-refactor intent, implementation must apply the strict union of the nearest relevant contracts:

- Domain files: docs/QUICK_REFERENCE.md, docs/GOVERNANCE.md, docs/DOMAIN.md, .agents/rules/domain.md, and clean-architecture-rules.
- Application handlers and validators: add-cqrs-handler conventions, .agents/rules/application-layer.md, and cqrs-mediatr-guidelines.
- Persistence construction/materialization: .agents/rules/efcore-persistence.md and dotnet-efcore-guidelines.
- HAL and DTO contract changes: add-hal-link, openapi-contract-change, .agents/rules/api-hateoas.md, and .agents/rules/api-controllers.md.
- Tests: .agents/rules/tests.md and docs/TESTING.md.
- External research: ip-clean-room-governance, .agents/skills/ip-clean-room/SKILL.md, and docs/legal/IP_GOVERNANCE.md.

All new files require two ABOUTME lines. Validators remain manually instantiated. Repositories continue to return entities. Generated migrations, snapshots, OpenAPI, inventory, and NSwag output must never be hand-edited.

## 1. Executive Summary

The refactor will establish one fixed lifecycle authority per aggregate in Explore.Domain:

- EventLifecycleRules owns fixed Event transition legality.
- EventSessionLifecycleRules owns fixed session transition, parent-state, and scheduling legality.
- Event and EventSession expose semantic methods such as Publish, Cancel, Archive, Complete, ApplyModeration, RestoreAfterModeration, and Reschedule.
- Lifecycle status setters become non-public after every legitimate mutation path is migrated.

This is additive validation, not a transfer of all validation into Domain. The Application layer remains responsible for command shape, authorization, tenant isolation, optimistic concurrency, lookup/repository facts, deployment/tenant policy, publish readiness, moderation-record eligibility, and transactional side effects. Entity methods enforce the invariant again immediately before mutation.

Handlers become orchestrators:

1. Validate the command with the existing manually instantiated validator.
2. Authorize and load tenant-scoped state.
3. Check optimistic concurrency.
4. Evaluate dynamic Application policy/readiness.
5. Call the semantic Domain method.
6. Persist and stage outbox/federation work transactionally.
7. Invalidate cache only after a successful commit.

HAL will consume the same pure Domain predicates as handlers/entities. Session read DTOs will carry the parent Event status required to make session affordances truthful. This removes the current independent transition matrices in EventLinkPolicy and EventSessionLifecycleAffordancePolicy.

### 1.1 Outcomes

- Nullable draft/import/archive fields remain nullable.
- Existing FluentValidation and policy/readiness validation remain and gain Domain backstops.
- No normal handler or API policy can assign EventStatusId or EventSessionStatusId directly.
- Fixed transition rules exist once and are exhaustively tested in Domain.
- HAL links and write enforcement cannot drift.
- Same-target lifecycle commands become idempotent success/no-op operations with no duplicate outbox, federation, cache, metric, or timestamp effects.
- Invalid transitions do not mutate state and map to stable Application failure codes.
- EF Core materializes entities with non-public status setters.
- OpenAPI and NSwag artifacts reflect the intentional pre-v1 contract change.

### 1.2 Non-Goals

- Do not create EventDraft or parallel draft tables.
- Do not make structurally required identifiers/status/ownership nullable.
- Do not move repository, authorization, tenant, policy, or I/O validation into Domain.
- Do not add a generic state-machine library, rules engine, reflection-driven policy engine, new base hierarchy, or new package.
- Do not introduce Domain events in this refactor; the current explicit transactional outbox remains the integration-event boundary.
- Do not add submit/review/approve/reject commands that do not already exist.
- Do not change schema, lookup values, or migrations unless pending-model-change verification disproves the no-schema assumption.
- Do not redesign ordinary Event content editing beyond adding the missing UpdateEventDraft lifecycle guard.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---|---|
| Nullable Event fields are intentional lifecycle flexibility, not a defective schema | src/Explore.Domain/Event.cs and dev/report/Event Draft Lifecycle Architecture Consultation.md | High | Required identity/tenant/status fields remain non-null; publish completeness is scenario-dependent. |
| Fixed lifecycle legality is not Domain-owned | Event.cs, EventSession.cs, and the lifecycle handlers listed below | High | Both lifecycle status properties are publicly settable. |
| The repository already has the preferred Domain rule pattern | RegistrationOrderRules.cs and RegistrationOrder.TransitionTo | High | Reuse Can/Ensure plus semantic entity mutation. |
| Dynamic lifecycle validation is already centralized in Application | EventLifecyclePolicyProvider, EventLifecycleReadinessEvaluator, and EventLocationPublicationReadinessEvaluator | High | These policy/repository-dependent checks must remain. |
| Event write enforcement and HAL disagree | ArchiveEventCommandHandler, CancelEventCommandHandler, and EventLinkPolicy | High | Current handlers accept transitions for which HAL exposes no action. |
| Session HAL cannot evaluate parent-state rules | EventSessionDto, EventSessionListDto, EventSessionMappingProfile, and EventSessionLinkPolicy | High | DTOs omit parent Event status even though the handler requires it. |
| Private setters should not require a relational change | Current EF configurations plus official EF Core constructor/private-setter guidance | Medium-High | Must still be proven by persistence/CI model checks. |
| Setter privatization has a large compile surface | Repository scan: 107 test files assign EventStatusId; 34 assign EventSessionStatusId | High | Migrate builders/factories before individual fixtures. |

### 2.2 Existing Implementation

#### 2.2.1 Domain

- src/Explore.Domain/Event.cs keeps draft-flexible fields nullable, exposes EventStatusId publicly, and centralizes timezone/schedule projection rather than lifecycle.
- src/Explore.Domain/EventSession.cs exposes EventSessionStatusId publicly. Reschedule owns time consistency/projection but not lifecycle eligibility.
- src/Explore.Domain/Services/Registration/RegistrationOrderRules.cs and RegistrationOrder.cs use pure CanTransition/EnsureCanTransition rules plus a semantic entity transition method.

#### 2.2.2 Event Application Paths

- PublishEventCommandHandler checks dynamic publication policy/readiness, then assigns Published directly.
- ArchiveEventCommandHandler and CancelEventCommandHandler contain their own status checks, then assign directly.
- ModerateEventCommandHandler and UnmoderateEventCommandHandler contain separate transition checks and direct Event/session assignments.
- EventHeavyRedactionApplicator assigns Moderated directly during the distinct irreversible safety flow.
- CreateEventCommandHandler constructs Draft or Published Event/session state directly.
- ImportEventCommandHandler starts Draft.
- AtprotoJetstreamRepository creates and refreshes federated Event/session status from external authoritative data.
- SeedData and DatabaseSeeder create or promote known seed records directly.

These paths represent normal transitions, initialization, federated synchronization, irreversible moderation, and deterministic seed setup. They require named seams, not one public SetStatus method.

#### 2.2.3 Session Application Paths

- PublishEventSessionCommandHandler owns policy/readiness and directly publishes.
- EventSessionLifecycleTransitionCommandHandlerBase centralizes transaction mechanics, but subclasses define CanTransition while the base assigns TargetStatus.
- Cancel, Complete, and Archive session handlers each carry a separate transition matrix.
- ScheduleEventSessionCommandHandler uses EventSession.Reschedule, but fixed state eligibility remains outside the entity.
- Event moderation and heavy redaction directly cascade Moderated to sessions.

#### 2.2.4 Validation And Persistence

- EventLifecyclePolicyProvider and EventLifecycleReadinessEvaluator own dynamic ValidationProfile-based fields.
- EventLocationPublicationReadinessEvaluator owns repository-loaded location facts.
- Validators are manually constructed inside handlers.
- Moderation reversal depends on persisted moderation history.
- ExploreDbContext.SaveChanges rotates ConcurrencyStamp for modified entities.
- EF configurations already map status FKs and lookup rows.

### 2.3 Existing Tests And Verification Coverage

- Event.Domain.UnitTests covers entity scheduling and the RegistrationOrder transition-rule precedent, but has no exhaustive Event/EventSession lifecycle matrix.
- Event.Application.UnitTests covers publish, archive, cancel, moderation, unmoderation, readiness, session lifecycle handlers, schedule, and concurrency. It does not protect the missing non-Draft UpdateEventDraft guard or a single cross-surface transition authority.
- Event.API.IntegrationTests/EventLifecycleHateoasPolicyTests protects current affordances but not Domain parity or parent Event state for session publish.
- Event.Persistence.IntegrationTests/EventSessionLifecycleConstraintTests protects PostgreSQL status/schedule constraints but not private-setter round-trip materialization.
- Event.Architecture.Tests does not currently ratchet lifecycle setter visibility.
- Planning baseline: the Release build passed for 39 projects with 0 errors and five existing SSH.NET NU1903 warnings.
- Earlier same-worktree consultation runs observed 805 passing Domain tests and 3,737 passing Application tests with one unrelated durable-replay failure. Do not mask or modify it.

### 2.4 Existing Documentation And Contracts

- dev/report/Event Draft Lifecycle Architecture Consultation.md establishes the single-aggregate nullable-draft rationale and recommends entity lifecycle methods plus retained Application validation.
- docs/adr/ADR-026-domain-owned-lifecycle-and-contextual-completeness.md is Accepted for implementation and canonically requires Domain-owned fixed lifecycle behavior, retained layered validation, explicit initialization/synchronization, idempotency, outbox discipline, non-public lifecycle mutation, exhaustive tests, and server-authored affordance parity.
- docs/DOMAIN.md and docs/API.md are the durable Domain/API contracts to update.
- docs/API_CHANGELOG.md records intentional contract breakage.
- schemas/openapi_islamu-event.json, docs/API_CONTRACT_INVENTORY.md, and EventApiClient.g.cs are generated artifacts.
- .github/workflows/openapi-contract.yml defines governed OpenAPI, inventory, NSwag, invariant, and determinism checks.
- No configuration, secret, environment variable, or current schema migration is implicated.

### 2.5 Current Pain Points And Improvement Areas

- UpdateEventDraftCommandHandler lacks a Draft lifecycle guard.
- Event and session fixed transition logic is spread across handlers, readiness, and HAL.
- Public status setters allow accidental bypass and make missed-handler updates likely.
- Event handlers are more permissive than advertised HAL actions.
- Session HAL lacks parent Event status and can advertise an action the handler rejects.
- Same-target behavior is inconsistent, increasing duplicate-side-effect risk under retries.
- Fixed readiness blockers are duplicated beside dynamic policy rules.
- Direct setter use across tests makes encapsulation expensive unless builders are migrated first.

### 2.6 Unknowns After Investigation

| Unknown | Search/evidence | Resolution task |
|---|---|---|
| Whether private setters require explicit backing-field mapping in this model | Current configurations plus EF official guidance indicate no; runtime materialization has not yet been changed. | Task 3.1 adds mapping only on evidence; Task 3.2/CI proves round trip. |
| Exact files containing every construction assignment after setters become private | Bounded counts are known, but listing 141 test files in the plan would be stale/noisy. | Task 3.1 uses compiler errors and shared-builder-first migration to discover the exact bounded set. |
| Whether the current OpenAPI build target regenerates every artifact in one solution build after the DTO change | CI workflow and project targets show API schema/NSwag generation; inventory is a test generator. | Task 4.2 uses the single phase build plus one combined API test command and inspects the generated diff. |

No product decision remains hidden: the five behavior/contract choices requiring user review are explicit in section 17.

## 3. Proposed Architecture And Control Flow

### 3.1 Validation Ownership

| Rule kind | Owner | Examples |
|---|---|---|
| Boundary/shape | Application validator | Required command IDs, lengths, enum input, ranges, concurrency token |
| Authorization and tenancy | Application handler/services | Actor authority, tenant-scoped load, moderation privileges |
| Dynamic deployment policy | Application lifecycle policy/readiness | Required description/image/location/sessions by profile or tenant |
| Repository facts | Application | EventLocation completeness, moderation record reversibility, FK existence |
| Fixed lifecycle invariant | Domain rules + entity method | Draft can publish; Published can cancel; parent Event must be Published to publish a session |
| Structural storage invariant | EF/database | Required FK, status lookup, indexes, schedule constraints |
| Side effects | Application + transactional outbox | Notifications, ATProto work, reminders, metrics, cache invalidation |

Each layer intentionally validates what it uniquely knows. The same fixed transition matrix must not be copied into multiple layers.

### 3.2 Domain Shape

Add:

- src/Explore.Domain/Services/Lifecycle/EventLifecycleRules.cs
- src/Explore.Domain/Services/Lifecycle/EventSessionLifecycleRules.cs

Each rules class is a small pure switch/predicate implementation modeled after RegistrationOrderRules. It exposes query methods for HAL/readiness and Ensure methods used by the aggregate. No DI, repository access, current-user context, logging, or policy resolution enters Domain.

Event gains semantic methods:

- Publish(DateTime occurredAt)
- Cancel(DateTime occurredAt)
- Archive(DateTime occurredAt)
- ApplyLightModeration(DateTime occurredAt)
- ApplyHeavyModeration(DateTime occurredAt)
- RestoreAfterLightModeration(DateTime occurredAt)
- EnsureDraftEditable()
- SynchronizeFederatedLifecycle(EventStatusEnum status, DateTime occurredAt), explicitly limited to the federated authoritative-refresh seam

EventSession gains:

- Publish(EventStatusEnum parentStatus, DateTime occurredAt)
- Cancel(EventStatusEnum parentStatus, DateTime occurredAt)
- Complete(EventStatusEnum parentStatus, DateTime occurredAt)
- Archive(EventStatusEnum parentStatus, DateTime occurredAt)
- ApplyParentModeration(DateTime occurredAt)
- SynchronizeFederatedLifecycle(EventSessionStatusEnum status, DateTime occurredAt)
- Reschedule uses EventSessionLifecycleRules.CanSchedule before applying the existing schedule projection/range invariant.

Methods:

- validate that occurredAt is UTC;
- return whether a real mutation occurred so callers can suppress duplicate side effects;
- do not rotate ConcurrencyStamp;
- update UpdatedAt only on a real transition;
- leave state unchanged when the transition is invalid;
- treat same-target requests as no-op success.

### 3.3 Event Transition Contract

| Current | Allowed ordinary target/action |
|---|---|
| Draft | Published, Cancelled, Archived |
| Published | Cancelled, Moderated through light moderation |
| Cancelled | Archived |
| Completed | Archived |
| Moderated | Published only through reversible unmoderation after the Application moderation-record check |
| Archived | No ordinary outgoing transition |

Heavy moderation is a separate irreversible safety override, not an ordinary state-machine edge. Preserve the existing ability to hide/redact content through that explicit method and flow.

Published-to-Archived is removed. The supported ordinary path is Published to Cancelled to Archived, matching current HAL affordances.

### 3.4 EventSession Transition Contract

| Action | Fixed Domain rule |
|---|---|
| Schedule/reschedule | Current status is Draft, Submitted, UnderReview, Approved, or Published; existing schedule projection/range rules still apply. |
| Publish | Current status is Draft, Submitted, UnderReview, or Approved; parent Event is Published; the session is scheduled and valid under existing open-ended/fixed-end semantics. |
| Cancel | Current status is Draft, Submitted, UnderReview, Approved, or Published; parent Event is neither Moderated nor Archived. |
| Complete | Current status is Published and parent Event is Published. |
| Archive | Current status is Draft, Cancelled, or Completed; parent Event is neither Moderated nor Archived. |
| Parent moderation | Separate moderation cascade, not an ordinary user transition. |

No new submit, review, approve, or reject command is introduced. The enums remain available for current/future workflows.

### 3.5 Application Handler Flow

Normal lifecycle handler:

Request
  -> manually instantiate and run validator
  -> authorize actor and tenant
  -> load aggregate/repository facts
  -> compare concurrency token
  -> evaluate dynamic policy/readiness
  -> preflight the same Domain rule for a stable FailureCode
  -> execute entity semantic method inside the existing transaction
  -> persist entity and stage outbox/federation/reminder work
  -> commit
  -> invalidate cache and record post-commit telemetry

The entity Ensure call remains the final invariant boundary even after Application preflight. Application never returns raw Domain exception text.

### 3.6 Initialization And External Synchronization

- Native/default construction starts Draft.
- Explicit initial-status construction is permitted only for controlled creation, import, federation materialization, seed, and test-builder seams.
- CreateEventCommandHandler keeps its existing policy ability to create a Published graph, but validates the requested status as a defined supported creation state and constructs the Event/sessions through the approved initialization seam.
- AtprotoJetstreamRepository uses explicit federated synchronization methods for existing entities instead of direct setters.
- SeedData constructs known Published examples explicitly; DatabaseSeeder uses semantic methods when promoting already-created records.
- No public generic SetStatus or RestoreStatus method is added.

After all call sites migrate, EventStatusId and EventSessionStatusId setters become private. EF Core continues to map private setters by convention; add backing-field configuration only if persistence tests demonstrate a materialization problem.

### 3.7 HAL And Contract Flow

- EventLinkPolicy calls EventLifecycleRules for fixed action eligibility, then adds Application/API-only conditions such as unmoderation-record eligibility.
- EventSessionLinkPolicy calls EventSessionLifecycleRules and deletes the duplicated transition matrix from EventSessionLifecycleAffordancePolicy. A link-construction helper may remain if it contains no business rules.
- Add ParentEventStatusId to EventSessionDto and EventSessionListDto and map it from Event.EventStatusId.
- HAL remains the UI affordance source of truth; no Blazor claim/status reconstruction is added.
- Regenerate schemas/openapi_islamu-event.json, docs/API_CONTRACT_INVENTORY.md, and src/Explore.Blazor.Client/Clients/EventApiClient.g.cs through governed commands.
- Update docs/API_CHANGELOG.md. Do not add a compatibility alias for the new field or preserve obsolete transition behavior.

## 4. Constraints And Invariants

- Clean Architecture dependencies remain inward-only; Domain references no Application, EF, API, or infrastructure type.
- Event and EventSession stay tenant-owned entities with required status FKs.
- Nullable draft/import/archive fields remain nullable.
- Validators remain manually instantiated and run before mutation.
- Authorization, tenant filtering, and concurrency checks remain mandatory.
- Repositories return entities, never DTOs.
- Multi-write handlers retain IUnitOfWork and transactional outbox behavior.
- No external provider call occurs inside a database transaction; ATProto planners only stage durable work.
- Cache invalidation occurs after commit.
- HAL link presence remains the sole client action authority.
- Date/time transitions use injected TimeProvider in Application and pass UTC into Domain.
- Generated EF migrations/snapshots and generated API/client files are not hand-edited.
- No new package or dependency-license review is required because no dependency is added.

## 5. Architecture Decisions

### Decision 1: Domain owns fixed legality; Application owns dynamic completeness

This follows the existing nullable-draft architecture and preserves scenario-specific validation. Moving policy/readiness into the entity would force repository/configuration concerns inward and violate Clean Architecture.

### Decision 2: Pure rule classes plus semantic entity methods

HAL needs to ask whether an action is valid without mutating an entity. A small pure rule class provides that query surface; the entity method reuses it and owns mutation. This is the same repository-native pattern used for RegistrationOrder.

### Decision 3: No generic state machine

Two small switch-based lifecycle policies are easier to audit, test, and maintain than a new framework, dependency, configuration DSL, or inheritance hierarchy.

### Decision 4: Same-target commands are idempotent no-ops

Lifecycle commands participate in retryable/outbox workflows. Standardizing repeat requests as success with no state, timestamp, outbox, federation, cache, metric, or reminder change prevents duplicate effects. This is an intentional pre-v1 behavior change.

### Decision 5: Moderation override remains distinct

Light moderation is an ordinary reversible Published-to-Moderated transition. Heavy redaction is a separate irreversible safety operation whose content redaction and record checks remain Application-owned.

### Decision 6: Private setters, explicit initialization seams

Private lifecycle setters make bypass impossible in normal code. Constructors/factories for approved initialization and explicit federation synchronization methods cover legitimate non-transition use cases without reopening a generic setter.

### Decision 7: HAL consumes Domain predicates

Duplicating a transition matrix in API policy defeats the centralization goal. Adding parent Event status to session read contracts supplies the missing input needed for truthful affordances.

### Decision 8: No schema migration

The status columns, FKs, and lookup values already exist. Private setters do not alter EF relational metadata. A pending-model-change check must prove this; if it fails, stop and investigate rather than hand-authoring a migration.

### Decision 9: Preserve explicit outbox orchestration

Domain events are not required to centralize invariants. Existing handler-managed transactional outbox behavior is already observable and reliable; changing that mechanism is unrelated scope.

## 6. Implementation Phases

Each phase ends with exactly one Release build and at most one non-browser test project command. Tests and documentation are included in their owning tasks, not deferred to separate phases.

### Phase 1 — Establish Domain Lifecycle Authority

Goal: add the single fixed transition authority and semantic aggregate methods without changing infrastructure or API contracts.

- Depends on: user approval of section 17.
- Relevant files: Event.cs, EventSession.cs, new Domain lifecycle rules, and Domain lifecycle tests.
- Related skills/rules: clean-architecture-rules, .agents/rules/domain.md, .agents/rules/tests.md.
- Phase acceptance: both aggregate matrices are explicit, semantic methods enforce them, and invalid/no-op behavior is test-protected.
- Rollback/failure handling: keep setters unchanged and stop at the failing Domain test; do not weaken an invariant to preserve a handler’s current permissive behavior.

#### Task 1.1 — Add Event lifecycle rules and entity behavior

- Type: create and modify.
- Layer: Domain and Domain tests.
- Dependencies: none after plan approval.
- Effort: M.
- Required skills/rules: clean-architecture-rules, domain.md, tests.md.

Files:

- Create src/Explore.Domain/Services/Lifecycle/EventLifecycleRules.cs.
- Modify src/Explore.Domain/Event.cs.
- Create tests/Event.Domain.UnitTests/Services/Lifecycle/EventLifecycleRulesTests.cs.
- Create or extend tests/Event.Domain.UnitTests/Entities/EventLifecycleTests.cs.

Implementation:

- Encode the Event transition table and draft-edit predicate in one pure rule class.
- Add semantic Event methods and a private mutation primitive.
- Return false for same-target no-op; throw only for invalid Domain usage.
- Require UTC occurredAt and update UpdatedAt only on mutation.
- Keep heavy moderation and federated synchronization explicit.

Acceptance:

- [ ] Every enum source/target pair has an explicit test outcome.
- [ ] Invalid transitions and non-UTC time leave status and UpdatedAt unchanged.
- [ ] Same-target calls return no mutation.
- [ ] The entity contains no policy/repository/I/O dependency.

#### Task 1.2 — Add EventSession lifecycle rules and entity behavior

- Type: create and modify.
- Layer: Domain and Domain tests.
- Dependencies: Task 1.1 for the agreed rule/method shape.
- Effort: M.
- Required skills/rules: clean-architecture-rules, domain.md, tests.md.

Files:

- Create src/Explore.Domain/Services/Lifecycle/EventSessionLifecycleRules.cs.
- Modify src/Explore.Domain/EventSession.cs.
- Create tests/Event.Domain.UnitTests/Services/Lifecycle/EventSessionLifecycleRulesTests.cs.
- Extend tests/Event.Domain.UnitTests/Entities/EventScheduleProjectionTests.cs or add EventSessionLifecycleTests.cs.

Implementation:

- Encode schedule, publish, cancel, complete, archive, parent-state, and moderation rules.
- Route Reschedule through CanSchedule while preserving current schedule projection and open-ended/fixed-end invariants.
- Add semantic session methods and explicit federation/moderation seams.

Acceptance:

- [ ] Exhaustive status/parent-status tests cover every action.
- [ ] Publish cannot occur under a non-Published parent or without a valid schedule.
- [ ] Invalid operations are atomic: no partial status/schedule/timestamp mutation.

Phase verification:

- dotnet build --configuration Release --verbosity quiet
- dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet

### Phase 2 — Refactor Application Orchestration Without Removing Validation

Goal: make every lifecycle handler call Domain behavior while preserving manual FluentValidation, authorization, concurrency, policy/readiness, transaction, outbox, and stable failure responses.

- Depends on: Phase 1.
- Relevant files: Event/session command handlers, moderation services, lifecycle readiness, Application tests, and docs/DOMAIN.md.
- Related skills/rules: cqrs-mediatr-guidelines, clean-architecture-rules, application-layer.md, tests.md.
- Phase acceptance: no normal handler assigns status directly; every existing dynamic validation and durable effect remains protected.
- Rollback/failure handling: revert only the current handler slice to the last Domain-backed green state; do not restore duplicated matrices or public bypasses as a fallback.

#### Task 2.1 — Refactor Event command handlers and close the draft guard

- Type: modify.
- Layer: Application, Application tests, and Domain documentation.
- Dependencies: Task 1.1.
- Effort: XL.
- Required skills/rules: cqrs-mediatr-guidelines, application-layer.md, tests.md, clean-architecture-rules.

Files:

- src/Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs
- src/Explore.Application/Features/Events/Handlers/Commands/ArchiveEventCommandHandler.cs
- src/Explore.Application/Features/Events/Handlers/Commands/CancelEventCommandHandler.cs
- src/Explore.Application/Features/Events/Handlers/Commands/UpdateEventDraftCommandHandler.cs
- src/Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs
- src/Explore.Application/Features/Events/Handlers/Commands/ImportEventCommandHandler.cs
- Existing validators in src/Explore.Application/DTOs/Event/Validators that own the affected request shapes.
- tests/Event.Application.UnitTests/Features/Events/Commands/EventLifecycleTransitionCommandHandlerTests.cs.
- tests/Event.Application.UnitTests/Features/Events/Commands/PublishEventCommandHandlerTests.cs.
- Existing UpdateEventDraft command tests in that same bounded Commands directory.

Implementation:

- Keep and run current validators.
- Add defined/supported EventStatusId validation on create.
- Add EnsureDraftEditable before draft mutation.
- Perform dynamic publish readiness before Event.Publish.
- Replace direct status assignment with semantic methods.
- Map invalid Domain predicates to existing or deliberately documented stable FailureCodes.
- Put same-target detection before readiness and I/O so retries are success/no-op.
- Generate time/IDs with TimeProvider before retryable transaction delegates.
- Keep outbox/federation writes transactional and move cache invalidation after commit.

Acceptance:

- [ ] Validator failures still short-circuit before repository mutation.
- [ ] Non-Draft draft update fails without changing fields.
- [ ] Invalid transitions do not call Update, outbox, federation planner, reminder services, cache, or metrics.
- [ ] Valid transitions preserve current required side effects exactly once.
- [ ] Published-to-Archived is rejected; Cancelled-to-Archived succeeds.

#### Task 2.2 — Refactor moderation and EventSession handlers

- Type: modify.
- Layer: Application and Application tests.
- Dependencies: Tasks 1.1, 1.2, and 2.1.
- Effort: L.
- Required skills/rules: cqrs-mediatr-guidelines, application-layer.md, tests.md, clean-architecture-rules.

Files:

- src/Explore.Application/Features/Events/Handlers/Commands/ModerateEventCommandHandler.cs
- src/Explore.Application/Features/Events/Handlers/Commands/UnmoderateEventCommandHandler.cs
- src/Explore.Application/Features/Events/Moderation/EventHeavyRedactionApplicator.cs
- src/Explore.Application/Features/EventSessions/Handlers/Commands/EventSessionLifecycleTransitionCommandHandlerBase.cs
- src/Explore.Application/Features/EventSessions/Handlers/Commands/PublishEventSessionCommandHandler.cs.
- src/Explore.Application/Features/EventSessions/Handlers/Commands/ScheduleEventSessionCommandHandler.cs.
- src/Explore.Application/Features/EventSessions/Handlers/Commands/CancelEventSessionCommandHandler.cs.
- src/Explore.Application/Features/EventSessions/Handlers/Commands/CompleteEventSessionCommandHandler.cs.
- src/Explore.Application/Features/EventSessions/Handlers/Commands/ArchiveEventSessionCommandHandler.cs.
- tests/Event.Application.UnitTests/Features/EventSessions/Commands/EventSessionLifecycleCommandHandlerTests.cs.
- tests/Event.Application.UnitTests/Features/Events/Commands/ModerateEventCommandHandlerTests.cs.
- tests/Event.Application.UnitTests/Features/Events/Commands/UnmoderateEventCommandHandlerTests.cs.
- tests/Event.Application.UnitTests/Features/Events/Commands/HeavyRedactEventCommandHandlerTests.cs.
- tests/Event.Application.UnitTests/Features/Events/Moderation/EventHeavyRedactionApplicatorTests.cs.

Implementation:

- Preserve moderation-record reversibility, permissions, content redaction, and durable messaging in Application.
- Replace Event/session cascade assignments with explicit Domain methods.
- Change the session base class from TargetStatus/direct assignment plus subclass CanTransition to an abstract semantic ApplyTransition call; do not create a new Event-handler mega-base.
- Keep PublishEventSession separate because its policy/readiness orchestration is materially different.
- Suppress all downstream effects for same-target no-ops.

Acceptance:

- [ ] No normal Application lifecycle path assigns either status property.
- [ ] Base handler still owns shared transaction, concurrency, hooks, and cache orchestration.
- [ ] Moderation idempotency repairs only the existing documented cascade and never duplicates durable messages.
- [ ] Heavy moderation remains an explicit irreversible path.

#### Task 2.3 — Make readiness reuse Domain fixed predicates

- Type: modify.
- Layer: Application, Application tests, and Domain documentation.
- Dependencies: Tasks 1.1 and 1.2.
- Effort: M.
- Required skills/rules: cqrs-mediatr-guidelines, application-layer.md, tests.md.

Files:

- src/Explore.Application/Services/Lifecycle/EventLifecycleReadinessEvaluator.cs
- tests/Event.Application.UnitTests/Services/EventLifecycleReadinessEvaluatorTests.cs.
- docs/DOMAIN.md, updated in this owning task to document two-step validation ownership and lifecycle matrices.

Implementation:

- Replace fixed terminal/parent transition duplication with calls to Domain rules.
- Preserve policy-driven RequiredEventFields, RequiredSessionFields, error provenance, and location readiness in Application.
- Keep diagnostic readiness richer than a Domain boolean.

Acceptance:

- [ ] Changing a Domain transition predicate changes handler enforcement and readiness fixed blockers without editing separate matrices.
- [ ] Dynamic tenant/profile requirements remain unchanged and covered.

Phase verification:

- dotnet build --configuration Release --verbosity quiet
- dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet

### Phase 3 — Encapsulate Lifecycle State And Prove Persistence Compatibility

Goal: remove public mutation access, migrate legitimate construction/synchronization seams, and add architecture enforcement.

- Depends on: Phase 2.
- Relevant files: aggregate setters/constructors, federation repository, seed paths, EF configuration if proven necessary, builders, architecture tests, persistence tests, and docs/TESTING.md.
- Related skills/rules: dotnet-efcore-guidelines, efcore-persistence.md, domain.md, tests.md.
- Phase acceptance: setters are private, all approved seams compile, architecture ratchets pass, and CI contract evidence proves materialization/no model drift.
- Rollback/failure handling: if EF materialization fails, first add the smallest explicit field/access-mode mapping; if model metadata changes, stop and diagnose rather than generating a migration.

#### Task 3.1 — Privatize status setters and migrate initialization seams

- Type: modify.
- Layer: Domain, Persistence, Application construction seams, and tests.
- Dependencies: Phase 2.
- Effort: XL.
- Required skills/rules: dotnet-efcore-guidelines, efcore-persistence.md, domain.md, tests.md.

Files:

- src/Explore.Domain/Event.cs
- src/Explore.Domain/EventSession.cs
- src/Explore.Persistence/Repositories/AtprotoJetstreamRepository.cs
- src/Explore.Persistence/Seed/SeedData.cs
- src/Explore.Persistence/Seed/DatabaseSeeder.cs
- Event/EventSession EF configurations only if a persistence test proves explicit backing-field mapping is needed.
- Shared test builders/factories, beginning with tests/Event.API.IntegrationTests/Builders/EventBuilder.cs.
- Affected unit/integration test construction sites.

Implementation:

- Make lifecycle status setters private.
- Provide Draft-default construction and explicit validated initial-state construction for approved seams.
- Use SynchronizeFederatedLifecycle for existing federated entities.
- Update seeds and builders to construct the intended state or apply semantic transitions.
- Prefer private setters mapped by EF convention; add HasField/UsePropertyAccessMode only if required by evidence.

Acceptance:

- [ ] All projects compile without a public setter.
- [ ] Normal transition handlers cannot bypass Domain methods.
- [ ] Federated refresh preserves its current authoritative mapping behavior through an explicit method.
- [ ] No relational model diff is generated.

#### Task 3.2 — Add architecture and persistence ratchets

- Type: modify.
- Layer: Architecture tests, Persistence integration tests, and testing documentation.
- Dependencies: Task 3.1.
- Effort: M.
- Required skills/rules: dotnet-efcore-guidelines, efcore-persistence.md, tests.md.

Files:

- tests/Event.Architecture.Tests, following existing architecture-test organization.
- tests/Event.Persistence.IntegrationTests/Repositories/EventSessionLifecycleConstraintTests.cs and/or a new focused materialization test in that directory.
- docs/TESTING.md, updated in this owning task with focused lifecycle verification commands if absent.

Implementation:

- Assert by reflection that EventStatusId and EventSessionStatusId setters are non-public.
- Add architecture coverage that Application/API do not expose a generic status mutation seam.
- Add PostgreSQL round-trip coverage for non-public setter materialization and status FK preservation.
- Keep the existing CI pending-model-change contract green. If it reports changes, stop and diagnose configuration drift; do not create or edit a migration as part of this plan.

Acceptance:

- [ ] Architecture tests fail if either setter becomes public.
- [ ] Persistence round trip loads the correct Event and session status.
- [ ] Pending-model-change CI contract is clean.

Phase verification:

- dotnet build --configuration Release --verbosity quiet
- dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet

Required PR/CI evidence outside the phase command budget:

- tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj passes in the repository’s PostgreSQL-enabled lane.
- The pending-model-change command passes using the documented Event DbContext command from docs/OPERATIONS.md.

### Phase 4 — Unify HAL And Regenerate The API Contract

Goal: make advertised lifecycle actions consume Domain rules and provide session HAL with the missing parent-state input.

- Depends on: Phases 2 and 3.
- Relevant files: session DTOs/mapping, Event and session HAL policies, API tests/docs, and generated OpenAPI/inventory/NSwag artifacts.
- Related skills/rules: add-hal-link, openapi-contract-change, api-hateoas.md, api-controllers.md, tests.md.
- Phase acceptance: session DTOs carry parent status, HAL and Domain decisions are exhaustive/parity-tested, and governed artifacts are synchronized.
- Rollback/failure handling: keep generated artifacts untouched until source DTO/HAL tests compile; if generation drifts, fix source/generator inputs and regenerate rather than editing output.

#### Task 4.1 — Add parent status to session contracts

- Type: modify.
- Layer: Application DTOs/mapping and API mapping tests.
- Dependencies: Phase 3.
- Effort: S.
- Required skills/rules: openapi-contract-change, application-layer.md, api-controllers.md, tests.md.

Files:

- src/Explore.Application/DTOs/EventSession/EventSessionDto.cs
- src/Explore.Application/DTOs/EventSession/EventSessionListDto.cs
- src/Explore.Application/Profiles/EventSessionMappingProfile.cs
- Existing mapping/API tests.

Implementation:

- Add ParentEventStatusId to both read DTOs and map Event.EventStatusId.
- Prove detail and list mapping include the loaded parent status.

Acceptance:

- [ ] Both session DTO shapes contain the required parent-state input.
- [ ] Mapping tests fail if Event.EventStatusId is omitted.

#### Task 4.2 — Refactor HAL and regenerate governed artifacts

- Type: modify and generated-output refresh.
- Layer: API/HATEOAS, API tests, documentation, OpenAPI, and generated Blazor client.
- Dependencies: Task 4.1.
- Effort: L.
- Required skills/rules: add-hal-link, openapi-contract-change, api-hateoas.md, api-controllers.md, tests.md.

Files:

- src/Explore.API/Hateoas/Policies/EventLinkPolicy.cs
- src/Explore.API/Hateoas/Policies/EventSessionLinkPolicy.cs
- Delete or reduce the business-rule portion of EventSessionLifecycleAffordancePolicy.
- tests/Event.API.IntegrationTests/Features/Hateoas/EventLifecycleHateoasPolicyTests.cs.
- docs/API.md and docs/API_CHANGELOG.md.
- schemas/openapi_islamu-event.json, docs/API_CONTRACT_INVENTORY.md, and EventApiClient.g.cs as generated outputs only.

Implementation:

- Use EventLifecycleRules and EventSessionLifecycleRules for fixed affordances.
- Retain authorization and reversible-moderation eligibility conditions.
- Add exhaustive parity tests for every Event/session/parent action input.
- Update API documentation with lifecycle, idempotency, and HAL rules.
- Document the intentional pre-v1 breaking behavior/contract changes.
- Let the single phase-end Release build regenerate the API schema and NSwag client.
- Include the API contract-inventory generator in the phase’s one combined API test command.
- Never hand-edit generated artifacts.

Acceptance:

- [ ] HAL never advertises an action the Domain rejects for the same inputs.
- [ ] Event Published does not advertise Archive.
- [ ] Session Publish is absent unless parent Event is Published and the session is publishable.
- [ ] OpenAPI, inventory, and generated client are in sync.
- [ ] API changelog names Published-to-Archived removal, same-target idempotency, and ParentEventStatusId.
- [ ] Existing Blazor code compiles against the regenerated client without a compatibility shim.
- [ ] No client-side status/claim inference is introduced.

Phase verification:

- dotnet build --configuration Release --verbosity quiet
- dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventLifecycleHateoasPolicyTests/*|/*/*/*/ApiContractInventory_Generate_WritesMarkdownToDocs" --minimum-expected-tests 2 --no-progress

The OpenAPI workflow’s stable-invariant and determinism lanes remain required PR/CI contract evidence, not extra phase commands.

## 7. Testing Strategy

### 7.1 Domain

- Exhaustive Event source/target matrix.
- Exhaustive session current/target/parent matrix.
- Schedule eligibility and existing time semantics.
- Same-target no-op return and unchanged UpdatedAt.
- Invalid transition atomicity.
- UTC timestamp enforcement.
- Heavy moderation and federated synchronization are explicit and bounded.

### 7.2 Application

- Manual validators still execute and return validation errors.
- Authorization, tenancy, concurrency, policy, location readiness, and moderation history remain enforced.
- Domain rule failure maps to stable FailureCode without leaking exception text.
- Invalid/no-op transitions produce no repository/outbox/federation/reminder/cache/metric side effects.
- Successful transitions stage each durable effect once.
- Draft update rejects every non-Draft status.
- Handler and readiness fixed-state decisions match Domain predicates.

### 7.3 Architecture And Persistence

- Status setters are non-public.
- Domain has no outward-layer dependency.
- EF round trips lifecycle status with private setters.
- No pending model changes.
- Existing status FK and schedule constraints still pass in PostgreSQL.

### 7.4 API/HAL/Contract

- Exhaustive link parity with Domain for Event/session matrices.
- Parent Event state suppresses invalid session Publish/Complete affordances.
- HAL authorization conditions remain intact.
- OpenAPI invariants and inventory generation pass.
- Generated NSwag client compiles.

### 7.5 Broader Required Suites Before PR Handoff

Run through the applicable repository CI lanes, without duplicating them inside every phase:

- tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj
- tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj
- stable OpenAPI invariants from .github/workflows/openapi-contract.yml
- the repository architecture suite

No browser, Playwright, Docker/Aspire startup, or live external-provider QA belongs in the implementation-plan phase gates.

## 8. Documentation, Configuration, And Operations Impact

| Artifact | Action |
|---|---|
| docs/DOMAIN.md | Document Domain lifecycle authority, matrices, semantic methods, and two-step validation. |
| docs/API.md | Document idempotent lifecycle commands and HAL affordance semantics. |
| docs/API_CHANGELOG.md | Record intentional pre-v1 breaking behavior/DTO changes. |
| docs/TESTING.md | Add/refresh focused lifecycle test commands while implementing architecture/persistence coverage. |
| docs/API_CONTRACT_INVENTORY.md | Regenerate; do not hand-edit. |
| schemas/openapi_islamu-event.json | Regenerate; do not hand-edit. |
| EventApiClient.g.cs | Regenerate through NSwag; do not hand-edit. |
| docs/OPERATIONS.md | No update unless implementation proves the existing generation/CI model-check instructions incomplete. |
| Configuration/secrets | No impact. |

No environment variable, Compose, Aspire, deployment manifest, operator runbook, secret, or runtime setting changes.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Entity methods do not replace handler authorization or tenant-scoped repository loads.
- Cross-tenant identifiers remain rejected before Domain mutation.
- HAL authorization conditions remain layered on top of lifecycle legality.
- Moderation record eligibility and irreversible redaction remain privileged Application flows.
- Same-target no-op handling prevents duplicate durable messages under retries.
- All external publication work remains staged through the outbox; no network I/O is added to Domain or transactions.
- Cache invalidation moves/remains post-commit so rollback cannot publish stale state transitions to readers.
- Errors expose stable codes and safe messages, not entity exception internals.
- Privacy: no new personal data is collected, exposed, retained, or logged; status changes continue to use current audit/moderation records.
- Abuse: Domain rules cannot authorize an action, so handler authorization and moderation privilege checks remain mandatory.
- Rate limiting: no endpoint or request-volume shape changes; existing API limits remain sufficient.
- Auditability: real mutations preserve current actor/outbox/moderation audit paths; no-op retries do not fabricate a new lifecycle event.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Rationale |
|---|---|---|
| Multi-tenancy | Applicable | Tenant-scoped loads and actor authorization remain Application preconditions; Domain methods intentionally have no tenant context. |
| Federation | Applicable | ATProto creation/refresh uses explicit initialization/synchronization seams; outbound publication remains transactional-outbox-driven. |
| Localization | Not Applicable | No localized resource catalog or display text feature is added. Stable existing failure messages/codes remain Application-owned. |
| Accessibility | Applicable, no UI markup change | Correct HAL affordances prevent inaccessible/dead actions from being rendered. No new visual component or interaction is introduced. |
| Product behavior | Applicable | Event matrix, same-target idempotency, Draft edit guard, and session parent-state affordances are deliberate pre-v1 product decisions. |
| OpenAPI | Applicable | ParentEventStatusId is added and governed artifacts are regenerated. |

## 11. Observability And Operations

- Preserve existing bounded BusinessMetrics and structured logs.
- Record success metrics only for real mutations, never same-target no-ops.
- Preserve moderation/event-published telemetry labels; do not add unbounded Event IDs to metric labels.
- Keep current correlation/outbox identifiers and generate them once before retryable delegates.
- Do not add a new metrics subsystem merely for the refactor.
- Preserve existing health/readiness surfaces because no new provider or background worker is added.
- Operator-visible failures remain stable Application FailureCodes, structured logs, and durable outbox state.
- Recovery remains retrying the idempotent command/outbox work after the underlying repository/provider issue is resolved.

## 12. Migration And Compatibility Plan

1. Implement Domain rules/methods while setters remain temporarily available.
2. Migrate Application lifecycle paths.
3. Migrate creation, federation, seed, and test construction.
4. Privatize setters and enable architecture ratchets.
5. Verify EF materialization and no pending model change.
6. Switch HAL to Domain rules and add parent status to contracts.
7. Regenerate governed API artifacts and update changelog/docs.
8. Run broader CI suites.

There is no data migration or dual-write period. Development deployments rebuild against the regenerated pre-v1 contract.

Rollback is a source rollback before release. Do not preserve public setters or duplicate transition logic as a fallback.

Compatibility classification:

- Additive DTO field: ParentEventStatusId on EventSessionDto and EventSessionListDto.
- Behavioral removal: Published Event can no longer archive directly.
- Behavioral normalization: same-target lifecycle actions return success/no-op and emit no effects.
- Affordance correction: session Publish/Complete links include parent Event state.
- No compatibility alias, dual handler, obsolete endpoint, or translation shim.
- Update docs/API_CHANGELOG.md and regenerate governed artifacts.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection signal | Owner/task |
|---|---|---|---|---|---|
| Setter privatization creates a large compile break across tests | High | High | Migrate shared builders first and use compiler errors as the bounded work queue. | Release build failures at object initializers. | Task 3.1 |
| Creation/federation initialization is confused with a normal transition | Medium | High | Separate validated initial construction, semantic transitions, and federated synchronization methods. | Direct-assignment search or incorrect federation tests. | Tasks 2.1, 3.1 |
| Dynamic readiness is accidentally removed or weakened | Medium | Critical | Preserve validators/policy/readiness and assert execution before mutation. | Existing readiness/handler tests fail or required-field errors disappear. | Tasks 2.1, 2.3 |
| HAL lacks a Domain-rule input or drifts again | Medium | High | Add ParentEventStatusId and exhaustive parity tests. | HAL advertises a Domain-rejected action. | Tasks 4.1, 4.2 |
| Idempotent retry duplicates effects | Medium | Critical | Domain returns mutation/no-op; stage effects only on mutation. | Duplicate outbox/reminder/federation calls in unit tests. | Tasks 2.1, 2.2 |
| EF private setter causes materialization/model drift | Low-Medium | High | Persistence round trip and existing CI pending-model-change contract; add explicit field mapping only on evidence. | Incorrect loaded status or CI model-drift failure. | Tasks 3.1, 3.2 |
| External research expression enters implementation | Low | High | Use only the source-free facts/specification in context and repository-native design. | Clean-room review finds copied code/structure. | All tasks |
| Existing dirty Quartz work is overwritten | Medium | High | Narrow path ownership and pre-edit git status checks. | Diff contains unrelated scheduler files. | All tasks |

## 14. Success Metrics And Definition Of Done

Observable success metrics:

- Zero direct normal lifecycle status assignments outside Domain semantic methods.
- Zero duplicated fixed transition matrices in Application readiness or API HAL.
- Exhaustive Domain matrix tests cover every enum source/target and parent-state input.
- Exhaustive HAL parity tests find zero advertised-but-rejected actions.
- Same-target handler tests observe zero repository/outbox/federation/reminder/cache/metric effects.
- Private-setter architecture tests and persistence round trips pass.
- Generated OpenAPI, inventory, and NSwag client have zero unexplained drift.

Definition of done:

- Domain rules are the only fixed transition authority for Event and EventSession.
- Every normal lifecycle mutation calls a semantic entity method.
- Status setters are non-public and architecture-tested.
- Manual FluentValidation, authorization, tenancy, concurrency, dynamic policy/readiness, and repository-fact validation remain.
- Draft update has a Domain-backed Draft guard.
- Same-target retries are side-effect-free no-op successes; invalid transitions are atomic with stable Application errors.
- Transactional outbox/federation/reminder behavior remains correct and cache invalidation is post-commit.
- EF private-setter round trip passes and the existing CI model-drift contract is clean.
- ParentEventStatusId is mapped and generated contracts are synchronized.
- docs/DOMAIN.md, docs/API.md, docs/API_CHANGELOG.md, and docs/TESTING.md are updated in owning tasks.
- Each phase has exactly its one Release build and selected project test evidence; broader intent-mandated CI lanes are recorded separately.
- No new dependency, migration, compatibility shim, generic state machine, or duplicated lifecycle matrix remains.

## 15. Implementation Agent Contract — Keep Dev Docs Current

1. At first implementation start, read plan, context, and tasks once. On cold resume, read context and tasks first, then only plan sections needed for the current phase or changed decision.
2. During an uninterrupted session, do not reread unchanged artifacts after every task; reopen only the exact section needed.
3. Start from the highest-priority unchecked task unless the user overrides it.
4. Treat tasks.md as the hot ledger: check a substantial task immediately when its acceptance criteria are met and reconcile smaller tasks no later than phase end.
5. Keep implementation-task and phase-verification checkboxes separate. A phase is complete only after its build and selected test pass.
6. Update the task summary, completed count, current priority, next slice, discovered/deferred work, and Last Updated whenever task state changes.
7. Update context after a completed phase, meaningful decision, blocker, failed validation, material discovery, or before pause/compaction/transfer; do not rewrite it for trivial edits.
8. Update the plan only when scope, architecture, phase order, acceptance criteria, risks, or validation strategy changes.
9. Record failed validation with cause and recovery action without marking the phase complete.
10. Before pause, compaction, transfer, or PR creation, reconcile affected tasks, add a dated handoff, and identify unrelated dirty files to avoid.
11. Run phase verification only after all phase tasks, with one Release build and at most one selected project test; do not repeat successful commands or start the app/browser.
12. Never report completion when repository reality and the ledger disagree.
13. Never hand-edit generated migrations, snapshots, OpenAPI, contract inventory, or NSwag client files.

Every implementation summary must teach what changed and why; the relevant patterns/libraries/infrastructure/protocols; important files/classes/handlers and their responsibilities; control/data flow; security/reliability conventions; verification; remaining work; and dev-doc status.

## 16. Progress Reporting Contract

Current phase state:

| Phase | Status | Exit evidence |
|---|---|---|
| Phase 1 — Domain authority | Not started | Release build + Domain unit tests |
| Phase 2 — Application orchestration | Not started | Release build + Application unit tests |
| Phase 3 — Encapsulation/persistence | Not started | Release build + Architecture tests; PostgreSQL/model evidence in CI |
| Phase 4 — HAL/API contract | Not started | Release build + one combined API/HAL/inventory test command |

After each implementation slice, report:

- Implemented: developer teaching summary
- Verified: exact evidence
- Remaining: incomplete or deferred work
- Next: recommended next slice
- Docs updated: tasks ledger yes/no, context updated/unchanged with reason, plan updated/unchanged with reason

## 17. Open Risks And Review Decisions

The following decisions are explicit in this plan and should be reviewed before implementation:

1. Use the current HAL matrix as the desired ordinary Event lifecycle contract, removing direct Published-to-Archived.
2. Standardize same-target lifecycle commands as success/no-op with no side effects.
3. Add ParentEventStatusId to session detail/list contracts so HAL can evaluate parent-state rules.
4. Permit explicit non-Draft initial construction only at controlled creation/import/federation/seed/test seams.
5. Preserve heavy moderation as a separate safety override outside the ordinary transition matrix.

No unresolved repository fact blocks implementation. The requested Context7 and Tavily retrievals were attempted but unavailable; the evidence and blocker details are recorded in the context artifact.
