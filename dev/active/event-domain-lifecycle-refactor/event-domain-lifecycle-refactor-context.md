<!-- ABOUTME: Hot handoff context for the Event and EventSession Domain lifecycle centralization workstream. -->
<!-- ABOUTME: Captures repository evidence, clean-room research provenance, decisions, baselines, blockers, and exact resume state. -->

# Event Domain Lifecycle Refactor — Context

Last Updated: 2026-08-18 Europe/Brussels

## Current Status

Planning is complete and awaiting user review. No runtime, test, schema, generated-contract, or configuration file was changed during planning.

Current blockers:

- Implementation blocker: none after user approves the five decisions in plan section 17.
- Research-tool blocker: Context7 OAuth refresh failed with invalid_grant and Tavily returned HTTP 432 plan-quota exhaustion. Official primary documentation was used as the recorded clean-room fallback.

The implementation target is not “move validation from Application to Domain.” It is:

- keep current manual validators;
- keep dynamic policy/readiness and repository-fact checks in Application;
- add Domain enforcement of fixed lifecycle invariants;
- make handlers, readiness, and HAL consume that one fixed authority;
- make status setters non-public after legitimate initialization/synchronization seams are migrated.

## Planning Deliverables

- event-domain-lifecycle-refactor-plan.md — authoritative design, phases, decisions, and quality gates.
- event-domain-lifecycle-refactor-context.md — this hot handoff and research/audit record.
- event-domain-lifecycle-refactor-tasks.md — implementation ledger with phase/task parity.

## Contribution Classification

No exact lifecycle-refactor intent exists in the current .agents/contract/intents.yaml. Use the strict union of:

- Domain/path rules and clean-architecture-rules.
- add-cqrs-handler conventions and cqrs-mediatr-guidelines for handlers/validators.
- EF Core persistence rules for private-setter materialization and initialization seams.
- add-hal-link and openapi-contract-change for HAL/DTO/generated artifacts.
- testing rules.
- ip-clean-room-governance for the explicitly requested external research.

No migration or package addition is planned.

## Repository State At Planning Time

- The worktree already contained unrelated Quartz scheduler changes and other user modifications. Do not revert, overwrite, stage, or “clean up” them.
- docs/adr/ADR-026-domain-owned-lifecycle-and-contextual-completeness.md appeared as an untracked user-owned Accepted ADR during final plan verification. It was read, found to align with this plan, and left unmodified.
- No overlapping Event lifecycle workstream was found under dev/active or dev/pause.
- The knowledge graph reported 43,670 nodes, 1,197,857 edges, 6,718 files, a matching develop/head SHA, and medium estimated risk for the lifecycle change. Broad semantic search did not resolve the concrete handlers, so repository-native rg/read inspection followed as the documented fallback.
- Current direct status-setter usage is broad: EventStatusId assignments occur in 107 test files and EventSessionStatusId assignments in 34 test files. Shared builders/factories are the safest migration leverage point.

## Baseline Verification

Planning-session command:

- dotnet build --configuration Release --verbosity quiet
- Result: exit 0; 39 projects; 0 errors; 5 warnings.

Known existing warnings:

- NU1903 for SSH.NET 2025.1.0 / GHSA-q939-rpr3-3284.

Earlier same-worktree consultation evidence, not rerun during this planning pass:

- Domain unit tests: 805 passed.
- Application unit tests: 3,737 passed and one unrelated failure:
  DurableReplay_AcceptsPreviouslyVerifiedReceiptOlderThanOneDay expected Completed but received NeedsReconciliation.

Do not weaken or alter that unrelated test in this workstream.

## Key Current-State Evidence

### Domain

- src/Explore.Domain/Event.cs
  - EventStatusId is publicly settable.
  - Nullable draft-capable fields already implement the intended single-aggregate design.
  - Domain behavior currently covers timezone/schedule projection, not lifecycle.

- src/Explore.Domain/EventSession.cs
  - EventSessionStatusId is publicly settable.
  - Reschedule enforces existing schedule consistency/projection.
  - Lifecycle legality remains external.

- src/Explore.Domain/Services/Registration/RegistrationOrderRules.cs
  - Repository-native precedent for pure CanTransition/EnsureCanTransition rules.

- src/Explore.Domain/RegistrationOrder.cs
  - Repository-native precedent for a semantic entity transition method.

### Event Application Paths

- PublishEventCommandHandler
  - manual validator, concurrency, policy/readiness, location readiness, outbox/federation/cache;
  - direct Published assignment.

- ArchiveEventCommandHandler
  - currently blocks only already Archived;
  - therefore permits Published-to-Archived, although Event HAL does not advertise it.

- CancelEventCommandHandler
  - currently blocks only already Cancelled;
  - more permissive than HAL.

- ModerateEventCommandHandler
  - Published-to-Moderated;
  - idempotent cascade repair when already Moderated;
  - direct Event and session assignments.

- UnmoderateEventCommandHandler
  - requires Moderated plus latest reversible moderation record;
  - direct Published assignment.

- EventHeavyRedactionApplicator
  - irreversible content redaction/safety flow;
  - directly sets Event and sessions Moderated;
  - must remain distinct from ordinary light moderation.

- UpdateEventDraftCommandHandler
  - validates and checks concurrency;
  - does not verify current Event is Draft before mutation.

- CreateEventCommandHandler
  - defaults EventStatusId 0 to Draft;
  - accepts caller status without a dedicated enum/creation-state validator;
  - constructs Event and sessions directly as Draft or Published;
  - dynamic publish readiness already exists and must remain.

### EventSession Application Paths

- EventSessionLifecycleTransitionCommandHandlerBase
  - centralizes validator/transaction/load/concurrency/parent/cache mechanics;
  - still assigns TargetStatus directly and lets each subclass own CanTransition.

- PublishEventSessionCommandHandler
  - stays separate because it owns dynamic publish policy/readiness;
  - directly assigns Published.

- ScheduleEventSessionCommandHandler
  - already calls EventSession.Reschedule;
  - fixed status eligibility should enter Domain.

- Cancel/Complete/Archive handlers
  - each owns a separate transition matrix.

### Readiness

- src/Explore.Application/Services/Lifecycle/EventLifecycleReadinessEvaluator.cs
  - dynamic RequiredEventFields and RequiredSessionFields are Application policy and remain there;
  - fixed terminal-state and parent-state checks are duplicated and should call Domain predicates.

- EventLocationPublicationReadinessEvaluator
  - uses repository-loaded location facts and remains Application-owned.

### HAL And Contracts

- EventLinkPolicy’s ordinary matrix is narrower and more coherent than current handlers:
  - Draft: publish, cancel, archive.
  - Published: cancel, light moderation.
  - Cancelled/Completed: archive.
  - Moderated: conditional unmoderate.

- EventSessionLinkPolicy calls an API-local EventSessionLifecycleAffordancePolicy that duplicates session transition rules.

- EventSessionDto and EventSessionListDto do not expose parent Event status.

- src/Explore.Application/Profiles/EventSessionMappingProfile.cs already maps EventTitle from the loaded Event navigation, so mapping ParentEventStatusId from Event.EventStatusId is repository-native.

### Persistence

- ExploreDbContext.SaveChanges rotates ConcurrencyStamp when an entity is modified. Domain lifecycle methods must not rotate it.
- UpdatedAt is populated only when absent; semantic transitions should set the caller-supplied UTC time on actual mutation.
- Event/EventSession status FKs already exist. Private setters should not produce a relational diff.
- AtprotoJetstreamRepository directly creates and updates mapped external statuses; it needs an explicit synchronization seam.
- SeedData and DatabaseSeeder need controlled initialization/transition paths.

## Agreed Target Design

The technology-neutral ownership model is already accepted by ADR-026. This workstream supplies the repository-specific Event/EventSession matrices, file changes, phases, and verification needed to implement it.

### Fixed Domain Authority

- EventLifecycleRules: one pure Event transition matrix and draft-edit predicate.
- EventSessionLifecycleRules: one pure session transition/parent/schedule matrix.
- Event and EventSession semantic methods reuse those rules and own mutation.
- Domain methods accept UTC time, update UpdatedAt only on mutation, and return mutation/no-op.
- Same-target requests are idempotent no-ops.
- Invalid transitions do not mutate.

### Application Authority That Remains

- FluentValidation command shape.
- Authorization and tenant isolation.
- Optimistic concurrency.
- Repository/FK/location/moderation-history facts.
- ValidationProfile and tenant/instance required-field policy.
- Readiness diagnostics and safe FailureCode mapping.
- IUnitOfWork, outbox, federation planning, notifications/reminders, metrics, and cache.

### Event Matrix

| Current | Allowed |
|---|---|
| Draft | Published, Cancelled, Archived |
| Published | Cancelled, light Moderated |
| Cancelled | Archived |
| Completed | Archived |
| Moderated | Published through reversible unmoderation only |
| Archived | None |

Heavy moderation remains a distinct safety override.

### Session Matrix

| Action | Allowed |
|---|---|
| Schedule | Draft, Submitted, UnderReview, Approved, Published |
| Publish | Draft, Submitted, UnderReview, Approved; parent Published; valid schedule |
| Cancel | Draft, Submitted, UnderReview, Approved, Published; parent not Moderated/Archived |
| Complete | Published; parent Published |
| Archive | Draft, Cancelled, Completed; parent not Moderated/Archived |
| Parent moderation | Explicit cascade override |

No new workflow commands are added.

### Encapsulation

- Private EventStatusId and EventSessionStatusId setters.
- Draft-default and explicit validated initial-state construction at controlled creation/import/federation/seed/test seams.
- Explicit SynchronizeFederatedLifecycle methods for existing externally authoritative entities.
- No public SetStatus method.
- EF private-setter convention first; backing-field configuration only on evidence.

### HAL/API

- HAL fixed predicates call Domain rules.
- Add ParentEventStatusId to session detail/list DTOs.
- Regenerate OpenAPI, contract inventory, and NSwag client.
- Update API changelog; no compatibility shim.

## Clean-Room Research Record

### Requested Tool Attempts

| Tool | Attempt | Result |
|---|---|---|
| Context7 MCP | MCP resource startup/read | Blocked: OAuth refresh rejected with invalid_grant; refresh token malformed or invalid. |
| Tavily MCP | Searches for Microsoft DDD, EF Core private setters/backing fields, and FluentValidation scenario validation | Blocked: HTTP 432, current Tavily plan usage limit exceeded. |
| Tavily MCP | Extraction of official documentation URLs | Blocked by the same HTTP 432 quota response. |

These failures do not invalidate the repository-grounded plan, but they mean no evidence is claimed as retrieved through Context7 or Tavily. Official primary documentation was used as the fallback and is registered below.

### Source Register

Access date for every source: 2026-08-18.

1. Microsoft Learn — Designing a microservice domain model

   https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-domain-model

   Facts retained:
   - Domain entities implement behavior, not only data.
   - Aggregate roots guard consistency and are the update entry point.

2. Microsoft Learn — Designing validations in the domain model layer

   https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations

   Facts retained:
   - Aggregates enforce invariants during state changes.
   - Boundary/DTO validation and Domain invariant validation can intentionally coexist.

3. Microsoft Learn — Entity types with constructors (EF Core)

   https://learn.microsoft.com/en-us/ef/core/modeling/constructors

   Facts retained:
   - EF Core supports non-public constructors.
   - Private setters remain mapped and writable by EF.
   - Domain entities do not need DbContext injection for materialization.

4. Microsoft Learn — Implementing a microservice domain model with .NET

   https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/net-core-microservice-domain-model

   Facts retained:
   - Entity updates should flow through methods/constructors.
   - Persistence-specific field mapping belongs to the infrastructure/persistence layer.

5. Microsoft Learn — Backing Fields (EF Core)

   https://learn.microsoft.com/en-us/ef/core/modeling/backing-field

   Facts retained:
   - Backing fields support encapsulation.
   - HasField and property access modes are available if convention mapping is insufficient.

6. FluentValidation — RuleSets

   https://docs.fluentvalidation.net/en/latest/rulesets.html

   Facts retained:
   - RuleSets can group scenario-specific validator rules.
   - The repository already has ValidationProfile/policy-provider conventions, so this refactor does not add a second scenario mechanism merely because RuleSets exist.

### Source-Free Specification And Independent Design

Permitted functional facts:

- Aggregate methods should enforce fixed state invariants.
- Boundary validation and Domain invariant validation are complementary.
- EF Core can materialize entities with private setters/non-public construction.
- Backing-field mapping is an optional persistence technique.

Independently selected repository-native design:

- Follow RegistrationOrderRules rather than external code structure.
- Use two small switch-based rule classes, not a generic state machine.
- Preserve current MediatR handlers and explicit transactional outbox.
- Preserve EventLifecyclePolicyProvider/readiness for dynamic requirements.
- Keep heavy moderation and federated synchronization explicit.
- Add parent status to DTOs because current HAL lacks a required input.
- Add no new dependency.

No external source code, snippets, ASTs, SQL, migrations, tests, comments, or structural organization may be copied into implementation.

Dependency-license decision: none; no dependency is added or changed.

## Known Blockers And Risks

### Non-Blocking Research Tool Issues

- Context7 credentials must be refreshed by the environment owner if tool-specific evidence is desired later.
- Tavily quota must reset or be upgraded if Tavily-specific retrieval is required later.

The plan is still actionable because repository evidence plus official primary documentation resolves the architecture.

### Implementation Risks

- Large test construction migration after setter privatization.
- Creation versus transition versus federation synchronization must remain visibly distinct.
- Same-target no-op must suppress all side effects.
- HAL parent-state mapping changes the generated client contract.
- Existing dirty worktree changes require narrow edits.

## Generated Contract Commands

Use the Phase 4 gate, not hand edits or extra phase builds:

1. dotnet build --configuration Release --verbosity quiet
2. dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventLifecycleHateoasPolicyTests/*|/*/*/*/ApiContractInventory_Generate_WritesMarkdownToDocs" --minimum-expected-tests 2 --no-progress

The build regenerates the governed API schema/NSwag output. The combined API command verifies HAL and writes the contract inventory. Stable OpenAPI invariants and determinism remain PR/CI evidence.

Generated outputs:

- schemas/openapi_islamu-event.json
- docs/API_CONTRACT_INVENTORY.md
- src/Explore.Blazor.Client/Clients/EventApiClient.g.cs

## Exact Resume State

Before first implementation:

1. Read all three workstream artifacts once.
2. Re-read current AGENTS.md, intents.yaml, relevant path rules, and relevant skills.
3. Inspect git status and identify unrelated Quartz/user changes.
4. Confirm the five review decisions in plan section 17.
5. Start Task 1.1 only.

On a cold resume, read context and tasks first, then only the plan sections needed for the current phase or a changed decision.

The first implementation file to open is src/Explore.Domain/Services/Registration/RegistrationOrderRules.cs as the repository-native pattern, followed by src/Explore.Domain/Event.cs and the existing Event enums/tests.

## Handoff Rule

After each task:

- update this context with the exact files changed and verification result;
- update the matching task checkbox;
- record any plan deviation and why;
- do not mark a phase complete until both its Release build and single test-project gate pass.
