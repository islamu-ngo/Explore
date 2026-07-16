<!-- ABOUTME: Required structure for repository-grounded implementation plan files. -->
<!-- ABOUTME: Preserves evidence, architecture decisions, executable phases, risks, and implementation-agent duties. -->

# Plan Template

Use this structure for `dev/active/<task-name>/<task-name>-plan.md`.

## Header

```markdown
# <Human Title> — Implementation Plan

Last Updated: YYYY-MM-DD Europe/Brussels
```

## 0. Planning Metadata

Record:

- original request and task directory;
- planning status: Draft, User-reviewed, Approved, In implementation, or Re-baselined;
- matched intents, relevant skills, and relevant rules;
- primary layers touched;
- S/M/L/XL complexity with evidence-based rationale.

## 1. Executive Summary

State what will change, why it matters, the intended user/business/platform outcome, and explicit non-goals.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

Use a table with `Claim`, `Evidence`, `Confidence`, and `Notes`.

### 2.2 Existing Implementation

Describe verified behavior by owning layer.

### 2.3 Existing Tests And Verification Coverage

Name verified test projects/files, protected behavior, and explicit gaps.

### 2.4 Existing Documentation And Contracts

List relevant docs, API/OpenAPI contracts, generated clients, configuration, policies, schemas, and runbooks.

### 2.5 Current Pain Points / Improvement Areas

Tie concrete correctness, security, UX, accessibility, performance, maintenance, duplication, and test gaps to evidence.

### 2.6 Unknowns After Investigation

For each unknown, record what was searched and the implementation task that will resolve it.

## 3. Proposed Future State

Describe target ownership, behavior, user/developer/operator experience, and important control/data flows.

## 4. Non-Negotiable Constraints

Reference the matched contract and list only task-relevant repository, security, tenant, architecture, API, UI, compatibility, and documentation constraints.

## 5. Architecture And Design Decisions

For each decision include:

- **Decision**
- **Why**
- **Alternatives considered**
- **Consequences**
- **Files/layers affected**

## 6. Implementation Phases

Use reviewable slices. Every phase includes:

```markdown
### Phase N: <Name>
- **Goal:**
- **Depends on:**
- **Relevant files:** existing/new status included
- **Related skills/rules:**
- **Acceptance criteria:** observable outcomes
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project <one-relevant-project>.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:**
```

Every task includes:

```markdown
#### Task N.M: <Actionable Task>
- **Type:** create / modify / delete / investigate
- **Layer:** Domain / Application / Persistence / Infrastructure / API / Blazor / Docs / DevOps
- **Files:** exact paths marked existing or new
- **Description:** implementation-level instructions
- **Acceptance Criteria:** checkboxes with observable outcomes
- **Dependencies:** task ids
- **Effort:** S / M / L / XL
- **Required Skills/Rules:**
```

Do not create standalone testing, QA, manual-review, documentation-review, reporting, dev-doc maintenance, or verification tasks. Fold necessary test-code and documentation changes into the task that owns the behavior. Run no build or test command until the phase implementation is complete.

## 7. Testing Strategy

Keep this section short. Assign exactly one fastest relevant non-browser test project to each phase, never repeat a project without a concrete reason, and never schedule more than one `dotnet test` command in a phase. Do not plan E2E, Playwright, browser automation, Chrome DevTools MCP, visual QA, live-app smoke, Aspire/Docker startup, or manual runtime verification.

Record additional intent-mandated projects as contract requirements, then distribute them across existing phases where possible; do not create artificial test-only phases.

## 8. Documentation, Configuration, And Operations Impact

Name the exact docs, schemas, generated artifacts, settings, environment variables, Aspire/Compose files, deployment material, and runbooks to update or state why none apply.

## 9. Security, Authorization, Privacy, And Abuse Considerations

Cover trust boundaries, authentication, server-side authorization, tenant isolation, HAL affordances, rate limiting, idempotency, auditability, privacy, sensitive-data handling, and abuse controls where relevant.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

Mark every concern Applicable, Not Applicable, or Needs Investigation and explain the classification.

## 11. Observability And Operations

Plan bounded logs, metrics, traces, health/readiness, troubleshooting, operator-visible failure modes, and recovery where relevant.

## 12. Migration And Compatibility Plan

Cover database/schema/data migration, seed changes, generated contracts, deployment order, rollback/reset, and breaking-change notes. Do not add compatibility shims unless explicitly approved.

## 13. Risk Register

Use a table with `Risk`, `Likelihood`, `Impact`, `Mitigation`, `Detection Signal`, and `Owner/Task`.

## 14. Success Metrics And Definition Of Done

Define observable functional success. For each phase, the automated gate is only one Release build plus at most one selected project test; do not add separate browser, runtime, manual-QA, migration-command, documentation-check, or operator-smoke gates.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

Require future implementation agents to:

1. At the first implementation start, read plan, context, and tasks once; on a cold resume, read context and tasks first, then only the plan sections needed for the current phase or changed decision.
2. During an uninterrupted session, do not reread unchanged plan/context/tasks after every task; keep the current task in working context and reopen only the exact section needed.
3. Start from the highest-priority unchecked task unless the user overrides it.
4. Treat `tasks.md` as the hot execution ledger: check a substantial task immediately after its implementation acceptance criteria are met, and reconcile smaller completed tasks together no later than phase end.
5. Keep implementation-task and phase-verification checkboxes separate; a task may be checked when its implementation is complete, but the phase is complete only after its build and selected test checkboxes pass.
6. Update the task status summary, completed count, current priority, next recommended slice, discovered tasks, deferred work, and `Last Updated` whenever task state changes.
7. Update context after a completed phase, meaningful decision, blocker, failed validation, material discovery, or before pause/compaction/transfer; do not rewrite it for trivial edits.
8. Update the plan only when scope, architecture, phase order, acceptance criteria, risks, or validation strategy changes; do not churn it for ordinary progress.
9. Record failed validation with the known cause and next recovery action in tasks/context without marking the phase complete.
10. Before pausing, compaction, transfer, or PR creation, reconcile the affected tasks, add a concise dated handoff, and identify unrelated dirty files that the next contributor must avoid.
11. Run phase verification only after all phase tasks, with one Release build and at most one selected project test; do not repeat successful commands or start the application/browser.
12. Never report completion when repository reality and the task ledger disagree.

Require every implementation summary to teach:

- what changed and why;
- architecture/design patterns, libraries, infrastructure, protocols, and project abstractions used;
- important files, classes, handlers, services, and components with their responsibilities;
- data/control flow;
- relevant repository conventions and reliability/security practices;
- verification performed, remaining work, next work, and dev-doc update status.

## 16. Progress Reporting Contract

Require this response shape after each implementation slice:

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: yes/no with reason
```

For completed implementation work, `Docs updated` must confirm that `tasks.md` was reconciled. Report context and plan separately as updated or unchanged because no trigger occurred.

## 17. Potential Risks & Unknowns

End with a candid, specific critique of the part most likely to fail, expand, or require a decision.
