---
description: Create a repository-grounded implementation plan with persistent dev docs
argument-hint: Describe the work to plan, e.g. "refactor authentication system" or "implement event RSVP"
---

# `/dev-docs` — Strategic Implementation Planning Command

You are the **principal implementation-planning agent** for the ISLAMU Event platform. Your job is not to produce a pleasant-looking outline. Your job is to create the artifact that future implementation agents will use as the operational source of truth before touching code.

Create a comprehensive, repository-grounded implementation plan for:

> `$ARGUMENTS`

This command is part of the agentic engineering workflow:

1. The user requests a plan.
2. You investigate the current implementation deeply enough to understand reality.
3. You create persistent dev docs under `dev/active/[task-name]/`.
4. The user reviews and corrects the plan.
5. Future agents implement from the plan and must continuously update the same dev docs as work progresses.

The output must therefore be **self-contained, source-grounded, maintainable during implementation, and strict enough that another agent can continue without re-asking the user for context**.

---

## Non-Negotiable Outcome

Create or update this structure:

```text
dev/active/[task-name]/
├── [task-name]-plan.md
├── [task-name]-context.md
└── [task-name]-tasks.md
```

Every file must include:

```markdown
Last Updated: YYYY-MM-DD Europe/Brussels
```

Use a stable kebab-case `[task-name]` derived from `$ARGUMENTS`. If an active directory already exists for the same workstream, **update and re-baseline it instead of creating a duplicate**.

---

## Platform Context To Assume, Then Verify Where Relevant

- **Backend:** .NET 10, ASP.NET Core, Clean Architecture, CQRS with MediatR.
- **Frontend:** Blazor Server + WebAssembly / InteractiveAuto, MudBlazor, BEM CSS isolation.
- **Database:** PostgreSQL + PostGIS via EF Core.
- **Auth:** Keycloak OIDC/JWT, BFF token handling, Cerbos or local authorization provider.
- **Orchestration:** .NET Aspire and Docker Compose.
- **Testing:** TUnit, bUnit, integration tests, architecture/context tests.
- **Product constraints:** white-label/self-hostable platform, multi-tenancy, federation foundations, cultural filtering, prayer-relative scheduling, spatial discovery, verification system.

Do not let this context make you lazy. Treat it as orientation only; all claimed existing files/classes/behaviors must be verified.

---

## Planning Philosophy

Bad plans say what someone hopes exists. Good plans prove what exists, identify what is missing, and define executable slices with validation.

Your plan must be:

- **Evidence-based:** distinguish verified facts from assumptions.
- **Implementation-ready:** tasks name files, layers, dependencies, acceptance criteria, and verification commands.
- **Context-preserving:** future agents can resume from the docs without rediscovering everything.
- **Continuously maintainable:** implementation agents are explicitly instructed to update the plan/context/tasks files during work, not only at the end.
- **Critical:** include improvement areas, risks, unknowns, deferred decisions, and the most likely failure points.
- **Clean Architecture compliant:** Domain → Application → Infrastructure/Persistence → API/Blazor. No inward dependency violations.
- **Product-aware:** consider tenant isolation, authorization, auditability, federation/API contracts, deployment, observability, accessibility, localization, and docs impact where relevant.

---

## Required Investigation Workflow

Before writing the plan, gather enough context to produce a high-quality plan. Do not draft from memory.

### 1. Read The Agent Contract And Task System

Read these first:

- `AGENTS.md`
- `AGENTS.md`
- `dev/active/README.md`
- `.claude/contract/intents.yaml`
- `docs/QUICK_REFERENCE.md`
- `docs/GOVERNANCE.md`

### 2. Classify The Work

Map `$ARGUMENTS` to one or more entries in `.claude/contract/intents.yaml`.

For every matched intent, capture in the plan:

- intent id and title;
- `must_read_docs`;
- `load_skills`;
- `load_rules`;
- `paths_in_scope`;
- `minimum_tests`;
- `docs_to_update`;
- `unique_acceptance`;
- `forbidden_without_approval`.

If no intent matches, state that explicitly and create a “Fallback Contract” using `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, relevant skills, and inferred tests. Also add a task to consider adding a new intent if this is likely to recur.

### 3. Load Relevant Skills And Rules

Read every skill and rule relevant to the matched intent or feature area. Typical examples:

- `clean-architecture-rules` for cross-layer work;
- `cqrs-mediatr-guidelines` for handlers, requests, validators, DTO mapping;
- `dotnet-efcore-guidelines` for persistence/schema/query work;
- `auth-patterns` and `blazor-bff-patterns` for auth/authz/BFF flows;
- `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, and `accessibility` for UI work;
- `outbox-pattern` for integration-event or reliable messaging work;
- `error-tracking` for observability work;
- `aspire` for AppHost/orchestration work.

Record the loaded skills/rules in the plan and explain what constraints they impose.

### 4. Read Canonical Product/Architecture Docs As Needed

Use the request scope to decide which docs are mandatory. Common sources:

- `docs/PROJECT.md`
- `docs/ARCHITECTURE.md`
- `docs/DOMAIN.md`
- `docs/API.md`
- `docs/SECURITY-MODEL.md`
- `docs/AUTHORIZATION.md`
- `docs/BLAZOR.md`
- `docs/CONFIGURATION.md`
- `docs/DEPLOYMENT_MODES.md`
- `docs/MULTI_TENANCY.md`
- `docs/FEDERATION.md`
- `docs/OPERATIONS.md`
- `docs/TROUBLESHOOTING.md`
- `docs/CODEBASE_STRUCTURE.md`

Do not cite a doc as authority unless you actually read the relevant section.

### 5. Search The Codebase Before Claiming Current State

You must verify every existing file path, class, interface, enum, controller, handler, component, test project, command, policy, or configuration key before listing it as existing.

Use targeted searches such as:

- `Grep` / ripgrep for names and concepts;
- `Glob` for file structure;
- AST-aware search where useful;
- LSP symbols/references for code-level relationships;
- LSP diagnostics on relevant files/directories where available, to distinguish pre-existing issues from planned work;
- direct reads of the most relevant files.

For every major current-state claim, capture source evidence in the plan using one of:

- `Verified: path/to/File.cs`;
- `Verified: path/to/File.cs::SymbolName`;
- `Verified by search: pattern "..." matched path/to/File.cs`;
- `Not found: searched for "..."; task added to create/decide`.

Never assume a `Common`, shared, utility, policy, repository, or test helper exists unless verified.

### 6. Inspect Existing Active Work

Search `dev/active/` and `dev/pause/` for related workstreams. If related docs exist:

- summarize their current status;
- identify overlap or conflicts;
- decide whether this plan should update an existing workstream or create a new one;
- carry forward unresolved blockers and remaining tasks if they still apply.

### 7. Produce A Current Implementation Report Before Designing The Future State

The plan must include a report-style section answering:

- What exists today?
- What is incomplete, broken, duplicated, risky, or under-tested?
- Which files own the relevant behavior?
- Which tests currently protect the behavior?
- Which docs/API contracts/configuration files describe the behavior?
- What does the current implementation do well?
- What are the improvement opportunities and why do they matter?
- What unknowns remain after reasonable investigation?

This replaces the recurring manual workflow where the user has to ask: “report on current implementation, identify improvement areas, then write a plan.” Do all of that inside this command.

---

## Required Plan File: `[task-name]-plan.md`

The plan file is the durable strategic source of truth. Use this structure.

```markdown
# [Human Title] — Implementation Plan

Last Updated: YYYY-MM-DD Europe/Brussels

## 0. Planning Metadata
- **Request:** original user request in one or two sentences
- **Task directory:** `dev/active/[task-name]/`
- **Planning status:** Draft / User-reviewed / Approved / In implementation / Re-baselined
- **Matched intents:** ...
- **Relevant skills:** ...
- **Relevant rules:** ...
- **Primary layers touched:** Domain / Application / Persistence / Infrastructure / API / Blazor / Docs / DevOps
- **Estimated complexity:** S/M/L/XL with rationale linked to scope, cross-layer impact, test burden, and unknowns from section 2.6

## 1. Executive Summary
Explain what will be built or changed, why it matters, and the intended user/business/platform outcome. Include what is explicitly out of scope.

## 2. Source-Grounded Current State Report
### 2.1 Evidence Log
Table with columns: Claim, Evidence, Confidence, Notes.

### 2.2 Existing Implementation
Describe current behavior by layer. Only cite verified files/classes.

### 2.3 Existing Tests And Verification Coverage
List verified test projects/files and what they cover. If tests are missing, state that explicitly.

### 2.4 Existing Documentation And Contracts
List relevant docs, OpenAPI/API contracts, configuration files, policies, and operational docs.

### 2.5 Current Pain Points / Improvement Areas
List concrete issues, gaps, duplication, under-specified behavior, UX friction, security concerns, performance risks, maintenance risks, and test gaps. Tie each item to evidence.

### 2.6 Unknowns After Investigation
List what remains unknown, what was searched, and how implementation should resolve each unknown.

## 3. Proposed Future State
Describe the target design and user/developer/operator experience. Include diagrams or flow sketches where helpful.

## 4. Non-Negotiable Constraints
Include relevant repo invariants, for example:
- repositories return entities, never DTOs;
- validators are manually instantiated;
- GET endpoints are anonymous and write endpoints are authorized;
- UI action affordances are gated by HAL links, not local role checks;
- tenant isolation is API-authoritative;
- no Clean Architecture dependency inversion violations;
- all new files start with two `ABOUTME:` lines where project rules require them;
- no compatibility shims unless explicitly approved.

## 5. Architecture And Design Decisions
For each decision:
- **Decision:** ...
- **Why:** ...
- **Alternatives considered:** ...
- **Consequences:** ...
- **Files/layers affected:** ...

## 6. Implementation Phases
Break work into vertical, reviewable slices where possible. Avoid giant vague phases.

### Phase N: [Name]
- **Goal:** ...
- **Depends on:** ...
- **Relevant files:** verified existing files and new files to create
- **Related skills/rules:** ...
- **Acceptance criteria:** verifiable bullets
- **Verification:** exact build/test/lint/docs commands or manual checks
- **Rollback / failure handling:** how to back out or diagnose failure

#### Task N.M: [Actionable Task]
- **Type:** create / modify / delete / investigate / test / docs
- **Layer:** Domain / Application / Persistence / Infrastructure / API / Blazor / Docs / DevOps
- **Files:** exact paths, marking `existing` or `new`
- **Description:** implementation-level detail sufficient for an agent to execute
- **Acceptance Criteria:** checkbox list with observable outcomes
- **Dependencies:** task ids
- **Effort:** S/M/L/XL
- **Required Skills/Rules:** ...
- **Validation:** ...

## 7. Testing Strategy
Map requirements to tests. Include unit, integration, architecture, UI/bUnit, E2E/manual smoke, policy checks, docs/context tests as applicable. Name test projects and likely test files.

## 8. Documentation, Configuration, And Operations Impact
List docs/config/deployment files to update, including `docs_to_update` from matched intents. Include Aspire/Docker/environment changes if relevant.

## 9. Security, Authorization, Privacy, And Abuse Considerations
Cover Keycloak/BFF/JWT, Cerbos/local authorization, tenant isolation, rate limiting, idempotency, audit trails, privacy/data exposure, and HAL affordances where relevant.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations
Explicitly mark each as Applicable / Not Applicable / Needs Investigation. Explain why.

## 11. Observability And Operations
Plan logging, metrics, traces, health checks, troubleshooting docs, and operator-visible failure modes where relevant.

## 12. Migration And Compatibility Plan
Include EF Core migrations, seed data, data backfills, breaking-change notes, and deployment sequencing if relevant. Because this repo is pre-v1, do not add backward-compatibility shims unless the user explicitly asks.

## 13. Risk Register
Table with Risk, Likelihood, Impact, Mitigation, Detection Signal, Owner/Task.

## 14. Success Metrics And Definition Of Done
Define functional success, quality gates, docs gates, and validation gates. Include exact tests/build commands.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT
Future agents implementing this plan MUST follow this contract:

1. Before starting any implementation slice, read this plan, `[task-name]-context.md`, and `[task-name]-tasks.md`.
2. Start from the highest-priority incomplete task unless user instruction overrides it.
3. After completing each meaningful task or discovering new scope, update:
   - this plan if architecture/scope/phases/risks changed;
   - `[task-name]-context.md` with current state, decisions, files changed, blockers, validation, and next step;
   - `[task-name]-tasks.md` by checking completed items and adding discovered tasks.
4. Do not report “done” unless docs reflect the actual current state.
5. Every implementation summary to the user must include:
   - what was implemented, explained as a developer teaching summary rather than an abstract status line;
   - which architecture/design patterns, libraries, infrastructure components, protocols, and project abstractions were used;
   - which important files/classes/interfaces/handlers/components changed and what each is responsible for;
   - the relevant data/control flow through the implementation;
   - which project conventions or industry best practices were followed and why;
   - what was verified;
   - what remains;
   - what should be worked on next.
6. If validation fails, update context/tasks with the failure, root cause if known, and next recovery action.
7. Before pausing, context reset, handoff, or PR creation, refresh all three dev docs and add/refresh a handoff section.

## 16. Progress Reporting Contract
When an implementation agent finishes a slice, its final response should use this concise structure:

- **Implemented:** medium-sized developer teaching summary of what changed, naming the patterns, libraries/infrastructure, important files/classes, and data/control flow. Do not collapse this to a single abstract sentence.
- **Verified:** ...
- **Remaining:** ...
- **Next:** ...
- **Docs updated:** plan/context/tasks updated? yes/no with reason

The `Implemented` section must leave the user with the same high-level technical understanding they would have if they had implemented the slice themselves. For example, an email-sending slice should name whether it used a transactional outbox, which queue/broker carries the message, which worker consumes it, which SMTP library/client sends mail, where retries/idempotency/error handling live, and how this follows the repo's Clean Architecture/CQRS boundaries. Do not write only “email sending implemented.”

## 17. Potential Risks & Unknowns
Write a candid critique of the part most likely to fail or become complex. Be specific, not generic.
```

---

## Required Context File: `[task-name]-context.md`

The context file is operational memory for future agents. Put the most immediately useful state near the top.

```markdown
# [Human Title] — Context

Last Updated: YYYY-MM-DD Europe/Brussels

## SESSION PROGRESS (YYYY-MM-DD Europe/Brussels)

### ✅ COMPLETED
- Planning created / re-baselined.
- Current-state report completed with evidence.

### 🟡 IN PROGRESS
- Awaiting user review of implementation plan.

### ⏭️ NEXT
1. User reviews plan and provides corrections/approval.
2. First implementation agent starts with task [id/name].
3. Update this context file after the first implementation slice.

### ⚠️ BLOCKERS
- None known / list exact blocker and decision needed.

## Quick Resume
1. Read `[task-name]-plan.md`.
2. Read `[task-name]-tasks.md`.
3. Start from the first unchecked high-priority task unless user instruction overrides it.
4. Keep all three dev docs updated after each meaningful implementation slice.

## Key Files And Responsibilities
Table: Path, Existing/New, Layer, Purpose, Notes.

## Key Decisions
Decision log copied from the plan in concise form.

## Constraints And Rules To Remember
Repo-specific rules, matched intents, skills, and path-scoped rules.

## Validation Baseline
Commands that must pass before completion, with intent-derived test projects.

## Current Known Risks / Unknowns
Short list with owner task ids.

## Handoff Notes
Add a dated handoff before pausing or transferring work.

### Handoff — YYYY-MM-DD Europe/Brussels
- **Current state:** ...
- **Next action:** ...
- **Blockers:** ...
- **Modified files:** ...
- **Validation:** ...
- **Documentation impact:** ...
- **Risks:** ...
- **Notes for next contributor/agent:** ...
```

---

## Required Tasks File: `[task-name]-tasks.md`

The tasks file is the tactical checklist. It must be easy to update during implementation.

```markdown
# [Human Title] — Task Checklist

Last Updated: YYYY-MM-DD Europe/Brussels

## Status Summary
- **Overall status:** Draft / User-reviewed / In implementation / Blocked / Complete
- **Completed:** 0/N
- **Current priority:** ...
- **Next recommended slice:** ...

## Implementation Maintenance Rules
- [ ] Before starting work, read plan/context/tasks.
- [ ] After each completed task, update this checklist immediately.
- [ ] If implementation changes scope or architecture, update the plan before continuing.
- [ ] If discoveries affect future work, update the context file.
- [ ] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Baseline
- [ ] User reviews the plan and approves or corrects scope.
  - Acceptance: plan status changes from Draft to User-reviewed/Approved.
- [ ] Implementation agent confirms current repo state before first edit.
  - Acceptance: no stale assumptions from planning are used blindly.

## Phase 1: [Name] ⏳ NOT STARTED
- [ ] **1.1 [Task name]**
  - **Files:** ...
  - **Acceptance:** ...
  - **Validation:** ...
  - **Effort:** S/M/L/XL
  - **Dependencies:** ...

## Verification Checklist
- [ ] LSP diagnostics clean for modified files.
- [ ] `dotnet build --configuration Release --verbosity quiet` passes.
- [ ] Intent minimum test projects pass individually with `dotnet test --project ...`.
- [ ] Architecture/context tests pass if agent docs/rules/skills changed.
- [ ] Docs updated where behavior/config/operations/API changed.
- [ ] Dev docs refreshed with final state and remaining work.

## Remaining / Deferred Work
- Track explicit deferrals with reason and owner.
```

---

## Planning Quality Gates

Before finalizing, audit your own plan against this checklist:

- [ ] Every existing path/class/interface mentioned in Current State was verified by search/read.
- [ ] Missing files/classes are marked as new work, not described as existing.
- [ ] The plan contains a Current State Report, not just a Future State.
- [ ] Improvement areas are explicit and tied to evidence.
- [ ] Every task has acceptance criteria and validation.
- [ ] Tasks are small enough for implementation agents to complete and update docs incrementally.
- [ ] The plan tells future agents to update plan/context/tasks during implementation.
- [ ] The final progress-report contract includes “what remains” and “what next.”
- [ ] Security/authz/multi-tenancy/docs/tests are marked applicable or not applicable with rationale.
- [ ] Risks are concrete and include mitigation/detection.
- [ ] No generic “add tests” or “update docs” tasks exist without naming scope and acceptance criteria.
- [ ] The plan can survive context reset: a cold agent can resume from the three files.

If any box fails, improve the files before responding.

---

## Final Response To User

After creating/updating the files, respond with:

```markdown
Created/updated implementation planning docs for `[task-name]`:
- `dev/active/[task-name]/[task-name]-plan.md`
- `dev/active/[task-name]/[task-name]-context.md`
- `dev/active/[task-name]/[task-name]-tasks.md`

Potential Risks & Unknowns:
[Short, candid paragraph naming the most likely hard part, grounded in the investigation.]

Recommended next step:
[Ask user to review a specific section, or state the first implementation slice if the plan is already approved.]
```

Do not claim implementation work has started. This command produces the plan and operational docs only.
