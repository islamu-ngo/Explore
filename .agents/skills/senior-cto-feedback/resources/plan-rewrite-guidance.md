<!-- ABOUTME: Rewrite guidance for improving implementation-plan workstreams after CTO review. -->
<!-- ABOUTME: Converts vague planning into executable plan/context/tasks updates with cleaner sequencing and stronger verification. -->
# Plan Rewrite Guidance

Use this when the user wants the plan improved, or when the CTO feedback should include a better implementation sequence.

The target is usually an existing `implementation-plan` workstream, not a blank outline. Rewrite the existing `plan.md`, `context.md`, and `tasks.md` so future agents can implement from them directly.

## Rewrite Principles

A better implementation plan should be:

- smaller,
- sequenced,
- testable,
- migration-aware,
- contract-aware,
- security-aware,
- self-hosting-aware,
- explicit about what breaks,
- explicit about what gets deleted,
- explicit about what future agents must keep updating.

## Rewrite The Whole Workstream

Do not rewrite only the strategy and leave the other two files stale.

When improving a workstream:

1. tighten `...-plan.md` to reflect the real architecture and sequence;
2. update `...-context.md` so the current status, next step, and risks match the rewritten plan;
3. update `...-tasks.md` so each phase and verification step maps to the rewritten plan;
4. remove or rename tasks that no longer match the recommended direction.

## Recommended Plan Shape

This shape should align with `.agents/skills/implementation-plan/SKILL.md` and its resources.

```markdown
# <Workstream Name> — Implementation Plan

Last Updated: YYYY-MM-DD Europe/Brussels

## 0. Planning Metadata
- Request
- Task directory
- Planning status
- Matched intents or fallback contract
- Relevant skills
- Relevant rules
- Primary layers touched
- Estimated complexity

## 1. Executive Summary
[What is being changed, why it matters, and what is out of scope.]

## 2. Source-Grounded Current State Report
### 2.1 Evidence Log
### 2.2 Existing Implementation
### 2.3 Existing Tests And Verification Coverage
### 2.4 Existing Documentation And Contracts
### 2.5 Current Pain Points / Improvement Areas
### 2.6 Unknowns After Investigation

## 3. Proposed Future State

## 4. Non-Negotiable Constraints

## 5. Architecture And Design Decisions

## 6. Implementation Phases

### Phase 1 — Foundation
Goal:
Files:
Tests:
Exit criteria:

### Phase 2 — Contract and Application
Goal:
Files:
Tests:
Exit criteria:

### Phase 3 — UI/BFF
Goal:
Files:
Tests:
Exit criteria:

### Phase 4 — Operations, Docs, and Hardening
Goal:
Files:
Tests:
Exit criteria:

## 7. Testing Strategy
## 8. Documentation, Configuration, And Operations Impact
## 9. Security, Authorization, Privacy, And Abuse Considerations
## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations
## 11. Observability And Operations
## 12. Migration And Compatibility Plan
## 13. Risk Register
## 14. Success Metrics And Definition Of Done
## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT
## 16. Progress Reporting Contract
## 17. Potential Risks & Unknowns
```

## Required `context.md` Rewrite Shape

Ensure the context file remains implementation-resumable:

```markdown
# <Workstream Name> — Context

Last Updated: YYYY-MM-DD Europe/Brussels

## SESSION PROGRESS (YYYY-MM-DD Europe/Brussels)
### ✅ COMPLETED
### 🟡 IN PROGRESS
### ⏭️ NEXT
### ⚠️ BLOCKERS

## Quick Resume
## Key Files And Responsibilities
## Key Decisions
## Constraints And Rules To Remember
## Validation Baseline
## Current Known Risks / Unknowns
## Handoff Notes
```

Rewrite rules:

- `NEXT` must match the first realistic implementation slice from the revised plan.
- blockers must be current, not historical clutter.
- key decisions must include any breaking-change choice and operator impact.

## Required `tasks.md` Rewrite Shape

Ensure the tasks file is execution-grade:

```markdown
# <Workstream Name> — Task Checklist

Last Updated: YYYY-MM-DD Europe/Brussels

## Status Summary
## Implementation Maintenance Rules
## Phase 1: ...
## Phase 2: ...
### Phase N Verification — one Release build and at most one project test
## Remaining / Deferred Work
```

Rewrite rules:

- every major plan phase should appear in tasks;
- every risky boundary should have observable acceptance criteria in its owning implementation task;
- each phase should name exactly one Release build and at most one fastest relevant non-browser project test at the end;
- no task should start the app/browser or use Playwright, Chrome DevTools MCP, E2E, Aspire/Docker startup, live-service smoke, or a manual runtime walkthrough;
- delete stale tasks created for a direction you are now rejecting.

## Breaking Change Rewrite Pattern

When breaking changes are allowed, replace vague compatibility language with this:

```markdown
## Compatibility Position

This workstream intentionally removes the old `<old behavior>` path.

Reason:
- Preserving it would keep duplicate semantics in `<files/components>`.
- The project is pre-v1 / in active development.
- The new contract is simpler and easier to test.

Impact:
- Existing `<clients/config/data>` must change.
- Generated client must be regenerated.
- Self-hosters must run the migration and update `<env/config>`.

Migration:
- `<migration or reset path>`

Docs:
- Update `<doc files>`.
```

## PR Split Guidance

Prefer splitting by risk boundary:

1. PR 1 — data/foundation
   - domain entities,
   - EF configuration,
   - migrations,
   - repository changes,
   - persistence tests.
2. PR 2 — application/API contract
   - commands/queries,
   - validators,
   - controllers,
   - authorization,
   - ProblemDetails,
   - OpenAPI,
   - API integration tests.
3. PR 3 — client/BFF/UI
   - generated client update,
   - Blazor services,
   - components/pages,
   - BFF endpoints,
   - component/BFF tests.
4. PR 4 — operations/docs/cleanup
   - configuration docs,
   - self-hosting docs,
   - operations docs,
   - cleanup obsolete compatibility paths,
   - delete obsolete tests.

## Anti-Patterns To Remove From Plans

Replace these phrases:

| Weak phrase | Strong replacement |
|---|---|
| “Maintain backward compatibility for now” | “Delete the old path unless a named self-hoster migration requires it.” |
| “Add tests” | “Add these specific tests for these risks in these projects.” |
| “Make tenant-aware” | “Resolve tenant from X, enforce through Y, test wrong-tenant Z.” |
| “Add config” | “Add env var, default, validation, docs, and failure behavior.” |
| “Add background worker” | “Add idempotent worker with retry, dead-letter, metrics, and recovery.” |
| “Update UI” | “Update UI after canonical API contract and generated client are stable.” |
