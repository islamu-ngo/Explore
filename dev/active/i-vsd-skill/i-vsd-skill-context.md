<!-- ABOUTME: Operational context for the Islamic Value Sensitive Design skill planning workstream. -->
<!-- ABOUTME: Preserves current state, decisions, constraints, and handoff notes for future implementation agents. -->

# Islamic Value Sensitive Design Skill Context

Last Updated: 2026-05-30 Europe/Brussels

## SESSION PROGRESS (2026-05-30 Europe/Brussels)

### ✅ COMPLETED

- Planning completed for the future `.claude/skills/i-vsd` implementation.
- Repository agent contract, governance, operations, testing, active-work, and intent files were read during planning.
- Confirmed there is no `.claude/contract/intents.yaml` entry for agent-context skill creation, so this uses the fallback contract.
- Loaded and applied `senior-cto-feedback` because this is a `/dev-docs` workstream.
- Inspected `.claude/skills/_SKILL_SCHEMA.md`, `Event.Architecture.Tests/AgentContextSchemaTests.cs`, `Event.Architecture.Tests/AgentContextLinkTests.cs`, and example skills.
- Confirmed again during handoff refresh that no `.claude/skills/i-vsd` files currently exist.
- Inventoried and extracted thesis framework material into the plan appendices and task source-routing checklist.
- Re-baselined `i-vsd-skill-plan.md` from planning-only status to implementation-ready status after the user requested implementation.

### 🟡 IN PROGRESS

- Context-limit handoff refresh for `dev/active/i-vsd-skill/*`.
- No skill implementation files have been created yet.

### ⏭️ NEXT

1. Start Phase 1 by creating `.claude/skills/i-vsd/SKILL.md` and `.claude/skills/i-vsd/resources/`.
2. Keep `SKILL.md` schema-compliant: required frontmatter, required section order, exactly five Top 5 invariant items, exactly five Top 5 anti-pattern items, and under 250 lines.
3. Create resources in the plan’s Appendix E order, rereading the thesis source files listed in `i-vsd-skill-tasks.md` before each resource category.
4. Update this context file and `i-vsd-skill-tasks.md` after each meaningful implementation phase.

### ⚠️ BLOCKERS

- None known. The only immediate risk is context loss; this handoff records current state before implementation begins.

## Quick Resume

Next step is implementation, not more planning: create `.claude/skills/i-vsd/SKILL.md` and `.claude/skills/i-vsd/resources/*.md` according to the expanded `i-vsd-skill-plan.md` appendices and the thesis-routed phase checklist in `i-vsd-skill-tasks.md`.

Do not claim that the skill has been implemented yet. At handoff time, `.claude/skills/i-vsd/**` does not exist.

Read first:

1. `AGENTS.md`
2. `.claude/skills/_SKILL_SCHEMA.md`
3. `dev/active/i-vsd-skill/i-vsd-skill-plan.md`
4. `dev/active/i-vsd-skill/i-vsd-skill-tasks.md`
5. The thesis source files routed to the resource category you are about to write.

## Current Implementation State

- `.claude/skills/i-vsd/SKILL.md`: not created.
- `.claude/skills/i-vsd/resources/`: not created.
- Implementation phase: Phase 1 pending.
- Verification for the actual skill: not run because no skill files exist yet.
- Active-doc synchronization: this handoff refresh updated the plan/context/tasks only.

## Key Files

Planning files:

- `dev/active/i-vsd-skill/i-vsd-skill-plan.md`
- `dev/active/i-vsd-skill/i-vsd-skill-context.md`
- `dev/active/i-vsd-skill/i-vsd-skill-tasks.md`

Future implementation files:

- `.claude/skills/i-vsd/SKILL.md`
- `.claude/skills/i-vsd/resources/*.md`

Repository rules and tests:

- `AGENTS.md`
- `docs/QUICK_REFERENCE.md`
- `docs/GOVERNANCE.md`
- `docs/OPERATIONS.md`
- `docs/TESTING.md`
- `.claude/skills/_SKILL_SCHEMA.md`
- `.claude/commands/dev-docs.md`
- `Event.Architecture.Tests/AgentContextSchemaTests.cs`
- `Event.Architecture.Tests/AgentContextLinkTests.cs`

Thesis source root:

- `/home/amir/Amir/Obsidian/mainvault/10 PROJECTS/🟢 amirakrari-Thesis/10-Active/Thesis`

Most important thesis files already read/extracted:

- `Abstract.md`
- `1.Introduction/1.2-Main-Research-Question.md`
- `1.Introduction/1.3-Goal-and-Scope.md`
- `1.Introduction/1.4-Key-Definitions.md`
- `2.Theoretical-Framework/2.3.1-Core-Islamic-Ethical-Principles-Applicable-to-Software-Development.md`
- `2.Theoretical-Framework/2.3.2-Summary-Islamic-Ethical-Framework-for-Software.md`
- `3.Methodology-Developing-the-I-VSD-Framework/3.2.1-Framework-Development-Process.md`
- `3.Methodology-Developing-the-I-VSD-Framework/3.2.2-The-I-VSD-Framework-Structure.md`
- `3.Methodology-Developing-the-I-VSD-Framework/3.2.3-Data-Governance-Heuristics.md`
- `3.Methodology-Developing-the-I-VSD-Framework/3.2.4-Content-Moderation-Heuristics.md`
- `3.Methodology-Developing-the-I-VSD-Framework/3.2.5-AI-and-Algorithmic-Heuristics.md`
- `3.Methodology-Developing-the-I-VSD-Framework/3.2.6-Marketing-and-Communication-Heuristics.md`
- `3.Methodology-Developing-the-I-VSD-Framework/3.2.7-Business-Model-Heuristics.md`
- `3.Methodology-Developing-the-I-VSD-Framework/3.3.1-Architecture-Heuristics.md`
- `3.Methodology-Developing-the-I-VSD-Framework/3.3.2-User-Interface-and-Experience-Heuristics.md`
- `4.Case-Study-ISLAMU-ASBL/4.2.3-I-VSD-Applied-Strategic-Decisions.md`
- `4.Case-Study-ISLAMU-ASBL/4.2.4-I-VSD-Applied-Technical-Decisions.md`
- `4.Case-Study-ISLAMU-ASBL/4.2.5-I-VSD-Applied-User-Experience-Decisions.md`
- `4.Case-Study-ISLAMU-ASBL/4.2.6-I-VSD-Applied-Business-Model-Decisions.md`
- `5.Discussion-and-Analysis/5.3.1-The-Fundamental-Principle.md`
- `5.Discussion-and-Analysis/5.3.3-Applying-This-to-Software-Development.md`
- `5.Discussion-and-Analysis/5.3.5-The-Ultimate-Success-Metric.md`
- `5.Discussion-and-Analysis/5.3.6-Barakah-Divine-Blessing-in-Technology.md`
- `5.Discussion-and-Analysis/5.4.23-User-Interest-Versus-User-Satisfaction-A-Design-Principle.md`
- `6.Conclusion/6.Conclusion.md`
- `6.Conclusion/6.2.1-For-Practitioners.md`
- `6.Conclusion/6.2.3-For-Islamic-Scholars.md`
- `8.Appendices/Appendix-A-The-I-VSD-Framework-Reference-Tables.md`
- `8.Appendices/A.1-Strategic-Framework-Table.md` through `A.7-Legal-and-Compliance-Framework-Table.md`
- `8.Appendices/Appendix-D-Industry-Anti-Patterns-Summary.md`
- `Note-on-Value-Source.md`
- `Note-on-Value-Source-Integration-Principle.md`
- `2.Theoretical-Framework/2.2.3-Limitations-of-VSD-for-Religious-Contexts.md`
- `3.Methodology-Developing-the-I-VSD-Framework/3.1.4-Methodological-Limitations.md`
- `5.Discussion-and-Analysis/5.1.1-Strengths-of-the-I-VSD-Framework.md`
- `5.Discussion-and-Analysis/5.1.2-Evidence-of-Effectiveness-from-ISLAMU-Case-Study.md`
- `5.Discussion-and-Analysis/5.1.3-Comparison-with-Standard-VSD.md`
- `5.Discussion-and-Analysis/5.1.4-Addressing-Gaps-in-Existing-Approaches.md`
- `5.Discussion-and-Analysis/5.2.1-Challenges-Encountered.md`
- `5.Discussion-and-Analysis/5.2.2-Limitations-of-the-Study.md`
- `5.Discussion-and-Analysis/5.2.3-Technical-Limitations-of-Ethical-Implementations.md`
- `5.Discussion-and-Analysis/5.2.4-The-Role-of-Trust-in-Ethical-Business-Relationships.md`
- `5.Discussion-and-Analysis/5.5.1-The-Imposition-Objection-Religious-Framework-Forcing-Values-on-Users.md`
- `5.Discussion-and-Analysis/5.5.2-The-Jurisprudential-Disagreement-Objection-Which-Interpretation-to-Follow.md`
- `5.Discussion-and-Analysis/5.5.3-The-Competitive-Viability-Objection-Ethical-Constraints-Are-Fatal.md`
- `5.Discussion-and-Analysis/5.5.4-The-Abstraction-Objection-Framework-Too-Theoretical-for-Practitioners.md`
- `5.Discussion-and-Analysis/5.5.5-The-Secular-Alternative-Objection-Religious-Grounding-Unnecessary.md`
- `5.Discussion-and-Analysis/5.5.6-The-Single-Case-Limitation-Insufficient-Validation.md`
- `5.Discussion-and-Analysis/5.5.7-The-Technology-Neutrality-Objection-Software-Is-Value-Free.md`
- `8.Appendices/Appendix-E-Technical-Stack-Evaluation-Matrix.md`

## Key Decisions

- Use active task name `i-vsd-skill` because the requested skill folder is `.claude/skills/i-vsd`.
- Treat the missing intent as a fallback-contract case rather than forcing a mismatched CQRS/UI/API intent.
- Make the future skill `type: workflow`, `enforcement: suggest`, `priority: high` unless implementation discovers a stronger schema reason otherwise.
- Keep `SKILL.md` concise and route to resources because schema tests cap skills at 250 lines.
- Resource-library approach is mandatory because the user requested a large all-in-one skill.
- The skill must be self-contained and should not require future users to read the private thesis path.
- The thesis can be cited in implementation comments or planning notes, but private material must not be sent to external tools.
- Formal outputs must include claim-boundary language: design reasoning, not fatwa/certification/operational proof.
- The expanded plan appendices are now the primary implementation source for ordinary skill writing; return to the thesis only for clarification, dispute resolution, or suspected omissions.
- Re-baseline decision on 2026-05-30: this workstream now owns implementation, not only planning, because the user explicitly requested implementation from `dev/active/i-vsd-skill/`.

## Key Decisions This Session

- Re-baselined the plan metadata instead of creating a duplicate workstream because `dev/active/i-vsd-skill/` is the single active source of truth.
- Preserved the implementation sequence: scaffold first, core resources second, domain/workflow resources after the claim-boundary and evidence resources exist.
- Did not add a journal entry because this handoff records session state only; no durable non-obvious system behavior was discovered.

## Files Modified And Why

- `dev/active/i-vsd-skill/i-vsd-skill-plan.md` — re-baselined from planning-only to implementation-ready status.
- `dev/active/i-vsd-skill/i-vsd-skill-context.md` — refreshed current state, quick resume, validation state, and context-limit handoff.
- `dev/active/i-vsd-skill/i-vsd-skill-tasks.md` — added handoff-refresh checklist and clarified next implementation slice.

## Validation State

- Commands run before this handoff update:
  - `git status --short` — showed many unrelated dirty files outside `dev/active/i-vsd-skill/` plus untracked `dev/active/i-vsd-skill/`.
  - `git diff --name-only` — showed existing tracked modifications outside this workstream; untracked `dev/active/i-vsd-skill/` is not included by `git diff`.
  - Glob `.claude/skills/i-vsd/**` — no files found.
- Commands run after editing this handoff:
  - `rtk git diff --check -- dev/active/i-vsd-skill/i-vsd-skill-plan.md dev/active/i-vsd-skill/i-vsd-skill-context.md dev/active/i-vsd-skill/i-vsd-skill-tasks.md` — passed with no output.
- Commands still needed after skill implementation:
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AgentContextSchemaTests`
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - `dotnet build --configuration Release --verbosity quiet` if shared context/test/docs behavior changes.

## Risks / Unknowns

- Source-grounding risk: some thesis routing filenames in `i-vsd-skill-tasks.md` may differ from actual local filenames; use glob/search before relying on exact routed paths.
- Moral-fidelity risk: resources must preserve Islamic normative authority, evidence levels, and non-certification boundaries rather than becoming generic VSD.
- Test/build risk: no schema tests can pass for `i-vsd` until the skill exists and all schema-required sections are correct.
- Worktree risk: unrelated dirty files are present. The next agent must not edit or revert them unless explicitly instructed.

## Constraints

- Every new file should start with two `ABOUTME` comments.
- Do not create or modify application code for this workstream.
- Do not add test skip-list exceptions.
- Do not use solution-level `dotnet test`; run project tests individually.
- Keep `SKILL.md` under 250 lines.
- Required skill sections are `Purpose`, `When to Load`, `When NOT to Load`, `Must-Read Docs`, `Top 5 Invariants`, `Top 5 Anti-Patterns`, `Minimal Examples`, `Verification Hooks`, and `Related Skills`.
- Do not present I-VSD as a fatwa, Sharia certification, product certification, or proof of ethical outcomes.
- Do not flatten the framework into generic VSD; Islamic ethical principles are the normative source.

## Validation Baseline

Planning-session validation:

- Planning files should be read back and checked for required headings and `Last Updated: 2026-05-30 Europe/Brussels`.
- No build is required for this planning-only session unless repository policy is interpreted as requiring it for dev-docs-only changes.

Implementation validation:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AgentContextSchemaTests
```

Recommended broader validation after skill implementation:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet build --configuration Release --verbosity quiet
```

## Known Risks

- The final skill may overclaim religious/legal authority if boundary language is not repeated in entrypoint and templates.
- The final skill may become too fragmented if resource files are not indexed clearly.
- The final skill may become too shallow if resources only summarize and do not provide operational checklists/templates.
- Thesis edits may require future synchronization.
- Current intent manifest lacks an agent-context skill-change intent; recurring work should add one.

## Handoff — 2026-05-30 Europe/Brussels

### Current State

- What is completed: planning, thesis extraction, implementation sequence, task routing, and context-limit handoff refresh.
- What is in progress: implementation is ready to begin; no skill files have been created.
- What changed since the last handoff: the plan was re-baselined from planning-only to implementation-ready because the user requested implementation.

### Next Action

1. Create `.claude/skills/i-vsd/SKILL.md` from Appendix C of the plan.
2. Create `.claude/skills/i-vsd/resources/` and start core resources from Appendix E order.
3. Update this context file and `i-vsd-skill-tasks.md` immediately after Phase 1.

### Blockers

- None known.

### Modified Files

- `dev/active/i-vsd-skill/i-vsd-skill-plan.md` — re-baselined current outcome/status for implementation.
- `dev/active/i-vsd-skill/i-vsd-skill-context.md` — refreshed session state, current implementation state, validation state, and handoff.
- `dev/active/i-vsd-skill/i-vsd-skill-tasks.md` — refreshed status and handoff-refresh checklist.

### Validation

- Commands run:
  - `git status --short` — dirty worktree with unrelated changes outside this workstream.
  - `git diff --name-only` — tracked changes outside this workstream were already present.
  - Glob `.claude/skills/i-vsd/**` — no files found.
  - `rtk git diff --check -- dev/active/i-vsd-skill/i-vsd-skill-plan.md dev/active/i-vsd-skill/i-vsd-skill-context.md dev/active/i-vsd-skill/i-vsd-skill-tasks.md` — passed with no output.
- Commands still needed:
  - Skill implementation schema tests after `.claude/skills/i-vsd` exists.

### Documentation Impact

- Updated active workstream docs only. No journal entry was needed.

### Risks

- Source-grounding risks: verify thesis filenames with glob/search before each resource because some routed names may differ from the latest thesis filenames.
- Test or build risks: schema tests have not run for the skill because no skill exists yet.
- Operator/release risks: none for runtime application; this workstream only affects agent-context docs/skills.

### Notes For Next Contributor Or Agent

- Required docs/rules to read: `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `.claude/skills/_SKILL_SCHEMA.md`, `dev/active/i-vsd-skill/i-vsd-skill-plan.md`, `dev/active/i-vsd-skill/i-vsd-skill-tasks.md`.
- Assumptions made: the active workstream is `i-vsd-skill`; no implementation files exist at handoff; the private thesis must remain local.
- Do not touch / unrelated dirty files: treat every dirty path outside `dev/active/i-vsd-skill/` as unrelated concurrent/user work unless the user explicitly says otherwise. Current unrelated dirty groups include application storage/onboarding files, generated client/OpenAPI/docs updates, several other `dev/active/*` workstreams, deleted `dev/active/subscription-notification/*` files, and untracked tests/scripts.
