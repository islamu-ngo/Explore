<!-- ABOUTME: Evidence-first investigation workflow for repository-grounded implementation planning. -->
<!-- ABOUTME: Defines source selection, intent classification, active-work inspection, and current-state reporting. -->

# Investigation Workflow

## Stop Condition

Stop after the three planning artifacts are complete, internally consistent, and ready for user review. Do not implement the planned change.

## 1. Mandatory Intake Phase

### 1A. I-VSD Assessment

1. Derive a stable kebab-case task name from the request.
2. Load `.agents/skills/i-vsd/SKILL.md` and follow its action routing for provider-responsibility analysis.
3. Check `islamic-value-sensitive-design/` for an existing `i-vsd-<task-name>.md`; update the mapped report instead of creating a duplicate.
4. Create or update `islamic-value-sensitive-design/i-vsd-<task-name>.md` with its evidence, principle, stakeholder, mitigation, uncertainty, and escalation traceability before plan drafting.

### 1B. Grill-Me Intake

1. Load `.agents/skills/grill-me/SKILL.md`.
2. Identify open architectural, product, failure-mode, and edge-case branches.
3. Resolve every branch answerable from repository evidence instead of asking the user.
4. For each remaining material branch, give a recommended answer with rationale and ask one targeted decision question at a time.
5. Do not draft the plan until material branches are resolved or the user explicitly defers them with the resulting risk recorded.

## 2. Establish The Workstream

1. Search `dev/active/` and `dev/pause/` for the same or overlapping work.
2. Re-baseline the existing workstream when it represents the same task; do not create a duplicate.
3. Record overlap, conflicts, inherited blockers, and still-relevant remaining work.

Use persistent dev docs for complex, cross-layer, multi-session, or multi-contributor work. Skip them for an atomic change that can be implemented and verified safely in one short slice.

## 3. Technical And Architectural Analysis

Identify major technology selections, external libraries, and competing architectural patterns. When a material fork exists, load `robin-neutral`, steel-man each viable option, and create a trade-off matrix grounded in repository constraints. Carry the selected approach and rejected alternatives into Section 5 of the plan, separate from the I-VSD report.

## 4. Resolve The Contract Before Feature Sources

Reuse injected `AGENTS.md`, resolve only the matching entry from `.agents/contract/intents.yaml`, and retrieve the relevant headings from `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, and `.agents/CONTEXT_ENGINEERING.md`. Record them in the context ledger and do not reread unchanged evidence.

Treat platform descriptions as orientation only. Verify every feature-specific claim from current repository files.

## 5. Classify The Requested Implementation

Match the planned work to one or more intent entries. For each match, copy into planning metadata and relevant tasks:

- intent id and title;
- `must_read_docs`;
- `load_skills` and `load_rules`;
- `paths_in_scope` and any forbidden paths;
- `minimum_tests` and verification commands, recorded as contract requirements without turning each one into a phase task;
- `docs_to_update`;
- `unique_acceptance` and PR checklist items;
- `forbidden_without_approval`.

If no intent matches, create a clearly labeled fallback contract from the agent contract, canonical docs, applicable skills/rules, inferred file scope, and proportional tests. Add a planning task to consider a reusable intent only when this work category is likely to recur.

## 6. Load Scope-Specific Sources

Load each selected skill router and matching path rule once. Retrieve only the headings or symbols needed from selected documents, then expand one named unresolved decision at a time.

Do not cite a document as authority unless its relevant section was read. Use repository sources before official documentation, and external research only when local and official sources cannot answer a material question.

## 7. Verify Current Repository Reality

Delegate broad inventory to an economical read-only scout with exact queries and the cap in `.agents/CONTEXT_ENGINEERING.md`. Use graph, structural outline, AST-aware search, and LSP definitions/references for focused follow-up; retrieve owning symbols and relevant tests rather than trusting filenames alone.

Verify every claimed existing:

- file and project;
- class, interface, enum, method, handler, repository, controller, component, or policy;
- route, HAL relation, OpenAPI operation, DTO, or generated client member;
- test fixture and verification command;
- configuration key, secret boundary, deployment resource, or operational behavior.

Use explicit evidence labels:

```text
Verified: path/to/File.cs
Verified: path/to/File.cs::SymbolName
Verified by search: pattern "..." matched path/to/File.cs
Not found: searched for "..."; task added to create or decide
```

Distinguish verified facts, source-derived constraints, design decisions, assumptions, and unresolved questions.

## 8. Report Current State Before Future State

The current-state report must answer:

- What exists now, by owning layer?
- What behavior do the implementation and contracts provide?
- Which tests protect it, and what is untested?
- Which docs, configuration, schemas, and operational contracts describe it?
- What is working well?
- What is incomplete, duplicated, unsafe, fragile, inaccessible, or hard to maintain?
- What remains unknown after reasonable investigation, and how will implementation resolve it?

Do not convert a search miss into proof of absence without recording what was searched.

## 9. Design Executable Vertical Slices

Design the future state only after the evidence report is complete. Follow repository layer ownership and prefer reviewable vertical slices over layer-wide mega-phases.

For each phase and task, specify:

- goal, owning layer, and dependencies;
- verified existing files and explicitly marked new files;
- required skills and rules;
- observable acceptance criteria;
- rollback, recovery, or failure-diagnosis behavior;
- effort based on scope, test burden, and unknowns.

Mark security, authorization, privacy, abuse, tenant isolation, federation, localization, accessibility, observability, migration, compatibility, documentation, configuration, and operations as Applicable, Not Applicable, or Needs Investigation with a reason.

Keep the implementation checklist lean:

- Create tasks only for implementation work that changes code, tests, schemas, configuration, or required documentation.
- Fold required tests and documentation into the implementation task that owns the behavior; do not create standalone testing, QA, documentation-review, reporting, or dev-doc maintenance tasks.
- Do not add a verification phase or run checks after individual tasks.
- At the end of each phase, run `dotnet build --configuration Release --verbosity quiet` once.
- At the end of each phase, run at most one `dotnet test --project <most-relevant-project>.csproj --configuration Release --verbosity quiet` command once.
- Select the fastest deterministic test project that covers the phase and does not start the application, browser, Docker, Aspire, or external services.
- Never plan Playwright, browser automation, Chrome DevTools MCP, visual QA, live-app smoke tests, manual runtime walkthroughs, or E2E test projects.
- Distribute repository-mandated test projects across existing phases without repeating them; never create extra phases solely to run more tests.

## 10. Write And Synchronize The Artifacts

Create or update:

```text
dev/active/<task-name>/
├── <task-name>-plan.md
├── <task-name>-context.md
└── <task-name>-tasks.md
```

All three files must contain `Last Updated: YYYY-MM-DD Europe/Brussels`. Cross-check status, next action, blockers, decisions, risks, phase names, task ids, and validation commands across the files before stopping.

Link `islamic-value-sensitive-design/i-vsd-<task-name>.md` from the plan, context, and tasks artifacts, and include the resolved Grill-Me decisions summary in their planning metadata or resume state.

Write the maintenance contract into the artifacts themselves so implementation agents do not need to reload this skill repeatedly:

- `tasks.md` is the hot execution ledger and must be updated during implementation, not by a later cleanup command.
- A substantial task is checked immediately after its implementation acceptance criteria are met; small related tasks may be reconciled together, but never later than phase end.
- Phase verification checkboxes remain separate from implementation checkboxes, and the phase becomes complete only after its build and selected test pass.
- `context.md` is refreshed after a phase, a meaningful decision, a blocker, validation failure, scope discovery, or handoff.
- `plan.md` changes only when scope, architecture, phase order, acceptance criteria, risk, or validation strategy changes.
- On initial implementation and cold resume, agents read task-owned context and the current task first, then retrieve only the plan heading named by that state.
- On an uninterrupted session, agents must not reread unchanged artifacts after every task; they use the current task entry and only reopen the exact section needed.
