<!-- ABOUTME: Task checklist for implementing the Islamic Value Sensitive Design agentic skill. -->
<!-- ABOUTME: Tracks executable phases, acceptance criteria, verification, and deferred work. -->

# Islamic Value Sensitive Design Skill Tasks

Last Updated: 2026-05-30 Europe/Brussels

## Status Summary

Planning status: complete.

Planning verification status: complete by read-back and heading checks after expansion.

Implementation status: not started.

Handoff refresh status: complete for context-limit compaction.

Current phase: ready for Phase 1 implementation. No `.claude/skills/i-vsd` files exist yet.

## Implementation Maintenance Rules

- Update `dev/active/i-vsd-skill/i-vsd-skill-context.md` after every meaningful implementation step.
- Update this task checklist immediately after completing or changing a task.
- Do not mark a task complete until the file change exists and has been reviewed.
- Keep `dev/active/i-vsd-skill/i-vsd-skill-plan.md`, context, and tasks aligned.
- If implementation scope changes, update the plan before making the change.
- Do not claim final completion until verification checklist is complete.
- Before writing each skill resource, reread the matching thesis source files listed in `Thesis Source Routing For Implementation` and record the files used in `i-vsd-skill-context.md`.
- If a resource concerns compliance, certification wording, halal/haram language, Islamic legal uncertainty, finance, religious content, or a contested moral judgement, reread the claim-boundary and scholarly-consultation files before drafting.

## Thesis Source Routing For Implementation

Use `dev/active/i-vsd-skill/i-vsd-skill-plan.md` as the primary implementation brief, but reread the exact thesis files below during implementation so the skill honors the full complexity of Islamic Value Sensitive Design. Thesis root: `/home/amir/Amir/Obsidian/mainvault/10 PROJECTS/🟢 amirakrari-Thesis/10-Active/Thesis`.

### Universal Files For Every Implementation Phase

- [ ] Reread `Abstract.md` before drafting final claim-boundary language.
- [ ] Reread `1.Introduction/1.2-Main-Research-Question.md` before describing purpose, scope, and research boundaries.
- [ ] Reread `1.Introduction/1.3-Goal-and-Scope.md` before defining provider-mediated software scope.
- [ ] Reread `1.Introduction/1.4-Key-Definitions.md` before writing glossary, invariants, examples, or report templates.
- [ ] Reread `Note-on-Value-Source.md` before describing Islamic value authority.
- [ ] Reread `Note-on-Value-Source-Integration-Principle.md` before combining Islamic, legal, secular, UX, compliance, or industry standards.
- [ ] Reread `2.Theoretical-Framework/2.2.3-Limitations-of-VSD-for-Religious-Contexts.md` before explaining why standard VSD is insufficient.
- [ ] Reread `2.Theoretical-Framework/2.3.1-Core-Islamic-Ethical-Principles-Applicable-to-Software-Development.md` before writing any principle-domain mapping.
- [ ] Reread `2.Theoretical-Framework/2.3.2-Summary-Islamic-Ethical-Framework-for-Software.md` before finalizing principle summaries.
- [ ] Reread `3.Methodology-Developing-the-I-VSD-Framework/3.2.1-Framework-Development-Process.md` before writing derivation protocol, consultancy flow, or audit workflow.
- [ ] Reread `3.Methodology-Developing-the-I-VSD-Framework/3.2.2-The-I-VSD-Framework-Structure.md` before writing framework overview, domains, evidence levels, or resource index.
- [ ] Reread `3.Methodology-Developing-the-I-VSD-Framework/3.1.4-Methodological-Limitations.md` before any limitation, validation, evidence, or certification-related wording.
- [ ] Reread `6.Conclusion/6.Conclusion.md` before finalizing the skill's claim boundaries and success definition.
- [ ] Reread `6.Conclusion/6.2.1-For-Practitioners.md` before writing practitioner-facing workflows and metrics.
- [ ] Reread `6.Conclusion/6.2.3-For-Islamic-Scholars.md` before writing scholarly consultation triggers.

### Compliance, Certification, And Claim-Boundary Routing

- [ ] For `resources/compliance-checks.md`, reread `Abstract.md`, `3.Methodology-Developing-the-I-VSD-Framework/3.1.4-Methodological-Limitations.md`, `3.Methodology-Developing-the-I-VSD-Framework/3.2.1-Framework-Development-Process.md`, `3.Methodology-Developing-the-I-VSD-Framework/3.2.2-The-I-VSD-Framework-Structure.md`, `5.Discussion-and-Analysis/5.2.2-Limitations-of-the-Study.md`, `5.Discussion-and-Analysis/5.5.2-The-Jurisprudential-Disagreement-Objection-Which-Interpretation-to-Follow.md`, `5.Discussion-and-Analysis/5.5.6-The-Single-Case-Limitation-Insufficient-Validation.md`, and `6.Conclusion/6.2.3-For-Islamic-Scholars.md`.
- [ ] For `resources/evidence-and-validation-levels.md`, reread `3.Methodology-Developing-the-I-VSD-Framework/3.1.4-Methodological-Limitations.md`, `5.Discussion-and-Analysis/5.1.2-Evidence-of-Effectiveness-from-ISLAMU-Case-Study.md`, `5.Discussion-and-Analysis/5.2.2-Limitations-of-the-Study.md`, `5.Discussion-and-Analysis/5.5.6-The-Single-Case-Limitation-Insufficient-Validation.md`, and `6.Conclusion/6.Conclusion.md`.
- [ ] For `resources/scholarly-consultation-boundaries.md`, reread `6.Conclusion/6.2.3-For-Islamic-Scholars.md`, `5.Discussion-and-Analysis/5.5.2-The-Jurisprudential-Disagreement-Objection-Which-Interpretation-to-Follow.md`, `Note-on-Value-Source.md`, and `Note-on-Value-Source-Integration-Principle.md`.

### Domain Resource Routing

- [ ] For `resources/data-governance-heuristics.md`, reread `3.Methodology-Developing-the-I-VSD-Framework/3.2.3-Data-Governance-Heuristics.md`, `2.Theoretical-Framework/2.3.1-Core-Islamic-Ethical-Principles-Applicable-to-Software-Development.md`, `5.Discussion-and-Analysis/5.2.3-Technical-Limitations-of-Ethical-Implementations.md`, and `8.Appendices/Appendix-A-The-I-VSD-Framework-Reference-Tables.md`.
- [ ] For `resources/content-moderation-heuristics.md`, reread `3.Methodology-Developing-the-I-VSD-Framework/3.2.4-Content-Moderation-Heuristics.md`, `2.Theoretical-Framework/2.3.1-Core-Islamic-Ethical-Principles-Applicable-to-Software-Development.md`, `4.Case-Study-ISLAMU-ASBL/4.2.3-I-VSD-Applied-Strategic-Decisions.md`, and `8.Appendices/Appendix-A-The-I-VSD-Framework-Reference-Tables.md`.
- [ ] For `resources/ai-and-algorithmic-heuristics.md`, reread `3.Methodology-Developing-the-I-VSD-Framework/3.2.5-AI-and-Algorithmic-Heuristics.md`, `5.Discussion-and-Analysis/5.2.1-Challenges-Encountered.md`, `5.Discussion-and-Analysis/5.2.3-Technical-Limitations-of-Ethical-Implementations.md`, and `8.Appendices/Appendix-E-Technical-Stack-Evaluation-Matrix.md`.
- [ ] For `resources/marketing-and-communication-heuristics.md`, reread `3.Methodology-Developing-the-I-VSD-Framework/3.2.6-Marketing-and-Communication-Heuristics.md`, `5.Discussion-and-Analysis/5.3.3-Applying-This-to-Software-Development.md`, `5.Discussion-and-Analysis/5.5.3-The-Competitive-Viability-Objection-Ethical-Constraints-Are-Fatal.md`, and `8.Appendices/Appendix-D-Industry-Anti-Patterns-Summary.md`.
- [ ] For `resources/business-model-heuristics.md`, reread `3.Methodology-Developing-the-I-VSD-Framework/3.2.7-Business-Model-Heuristics.md`, `5.Discussion-and-Analysis/5.3.1-The-Fundamental-Principle.md`, `5.Discussion-and-Analysis/5.3.5-The-Ultimate-Success-Metric.md`, `5.Discussion-and-Analysis/5.3.6-Barakah-The-Blessing-That-Money-Cannot-Buy.md`, `5.Discussion-and-Analysis/5.5.3-The-Competitive-Viability-Objection-Ethical-Constraints-Are-Fatal.md`, and `4.Case-Study-ISLAMU-ASBL/4.2.6-I-VSD-Applied-Business-Model-Decisions.md`.
- [ ] For `resources/architecture-heuristics.md`, reread `3.Methodology-Developing-the-I-VSD-Framework/3.3.1-Architecture-Heuristics.md`, `4.Case-Study-ISLAMU-ASBL/4.2.4-I-VSD-Applied-Technical-Decisions.md`, `5.Discussion-and-Analysis/5.2.3-Technical-Limitations-of-Ethical-Implementations.md`, and `8.Appendices/Appendix-E-Technical-Stack-Evaluation-Matrix.md`.
- [ ] For `resources/ux-and-defaults-heuristics.md`, reread `3.Methodology-Developing-the-I-VSD-Framework/3.3.2-User-Interface-and-Experience-Heuristics.md`, `5.Discussion-and-Analysis/5.4.23-User-Interest-Versus-User-Satisfaction-The-Ethics-of-Defaults.md`, `4.Case-Study-ISLAMU-ASBL/4.2.5-I-VSD-Applied-User-Experience-Decisions.md`, and `8.Appendices/Appendix-A-The-I-VSD-Framework-Reference-Tables.md`.
- [ ] For `resources/strategic-decision-framework.md`, reread `4.Case-Study-ISLAMU-ASBL/4.2.3-I-VSD-Applied-Strategic-Decisions.md`, `5.Discussion-and-Analysis/5.3.1-The-Fundamental-Principle.md`, `5.Discussion-and-Analysis/5.3.3-Applying-This-to-Software-Development.md`, `5.Discussion-and-Analysis/5.3.5-The-Ultimate-Success-Metric.md`, and `5.Discussion-and-Analysis/5.3.6-Barakah-The-Blessing-That-Money-Cannot-Buy.md`.
- [ ] For `resources/technical-decision-framework.md`, reread `4.Case-Study-ISLAMU-ASBL/4.2.4-I-VSD-Applied-Technical-Decisions.md`, `3.Methodology-Developing-the-I-VSD-Framework/3.3.1-Architecture-Heuristics.md`, and `8.Appendices/Appendix-E-Technical-Stack-Evaluation-Matrix.md`.
- [ ] For `resources/design-decision-framework.md`, reread `4.Case-Study-ISLAMU-ASBL/4.2.5-I-VSD-Applied-User-Experience-Decisions.md`, `3.Methodology-Developing-the-I-VSD-Framework/3.3.2-User-Interface-and-Experience-Heuristics.md`, and `5.Discussion-and-Analysis/5.4.23-User-Interest-Versus-User-Satisfaction-The-Ethics-of-Defaults.md`.
- [ ] For `resources/operational-framework.md`, reread `8.Appendices/Appendix-A-The-I-VSD-Framework-Reference-Tables.md`, `5.Discussion-and-Analysis/5.2.4-The-Role-of-Trust-in-Ethical-Business-Relationships.md`, `5.Discussion-and-Analysis/5.2.3-Technical-Limitations-of-Ethical-Implementations.md`, and `6.Conclusion/6.2.1-For-Practitioners.md`.
- [ ] For `resources/governance-and-accountability-framework.md`, reread `3.Methodology-Developing-the-I-VSD-Framework/3.2.2-The-I-VSD-Framework-Structure.md`, `5.Discussion-and-Analysis/5.2.4-The-Role-of-Trust-in-Ethical-Business-Relationships.md`, `6.Conclusion/6.2.1-For-Practitioners.md`, and `6.Conclusion/6.2.3-For-Islamic-Scholars.md`.
- [ ] For `resources/legal-and-compliance-framework.md`, reread `Note-on-Value-Source-Integration-Principle.md`, `8.Appendices/Appendix-A-The-I-VSD-Framework-Reference-Tables.md`, `6.Conclusion/6.2.3-For-Islamic-Scholars.md`, and `5.Discussion-and-Analysis/5.5.2-The-Jurisprudential-Disagreement-Objection-Which-Interpretation-to-Follow.md`.
- [ ] For `resources/evaluation-metrics.md`, reread `6.Conclusion/6.2.1-For-Practitioners.md`, `5.Discussion-and-Analysis/5.3.5-The-Ultimate-Success-Metric.md`, `5.Discussion-and-Analysis/5.3.6-Barakah-The-Blessing-That-Money-Cannot-Buy.md`, and `5.Discussion-and-Analysis/5.1.2-Evidence-of-Effectiveness-from-ISLAMU-Case-Study.md`.

### Workflow, Report, Anti-Pattern, And Case Routing

- [ ] For `resources/consultancy-workflow.md`, reread `3.Methodology-Developing-the-I-VSD-Framework/3.2.1-Framework-Development-Process.md`, `3.Methodology-Developing-the-I-VSD-Framework/3.2.2-The-I-VSD-Framework-Structure.md`, `6.Conclusion/6.2.1-For-Practitioners.md`, and `5.Discussion-and-Analysis/5.2.1-Challenges-Encountered.md`.
- [ ] For `resources/report-templates.md`, reread `3.Methodology-Developing-the-I-VSD-Framework/3.2.1-Framework-Development-Process.md`, `5.Discussion-and-Analysis/5.1.2-Evidence-of-Effectiveness-from-ISLAMU-Case-Study.md`, `5.Discussion-and-Analysis/5.2.2-Limitations-of-the-Study.md`, and `6.Conclusion/6.2.1-For-Practitioners.md`.
- [ ] For `resources/industry-anti-patterns.md`, reread `8.Appendices/Appendix-D-Industry-Anti-Patterns-Summary.md`, `3.Methodology-Developing-the-I-VSD-Framework/3.2.6-Marketing-and-Communication-Heuristics.md`, `3.Methodology-Developing-the-I-VSD-Framework/3.3.2-User-Interface-and-Experience-Heuristics.md`, and `3.Methodology-Developing-the-I-VSD-Framework/3.2.7-Business-Model-Heuristics.md`.
- [ ] For `resources/islamu-event-case-patterns.md`, reread `4.Case-Study-ISLAMU-ASBL/4.2.3-I-VSD-Applied-Strategic-Decisions.md`, `4.Case-Study-ISLAMU-ASBL/4.2.4-I-VSD-Applied-Technical-Decisions.md`, `4.Case-Study-ISLAMU-ASBL/4.2.5-I-VSD-Applied-User-Experience-Decisions.md`, `4.Case-Study-ISLAMU-ASBL/4.2.6-I-VSD-Applied-Business-Model-Decisions.md`, `5.Discussion-and-Analysis/5.1.2-Evidence-of-Effectiveness-from-ISLAMU-Case-Study.md`, and `5.Discussion-and-Analysis/5.5.6-The-Single-Case-Limitation-Insufficient-Validation.md`.
- [ ] For objection-handling sections in any resource, reread all files under `5.Discussion-and-Analysis/5.5.*` before finalizing.

## Phase 0 - Planning Artifacts

- [x] Read repository contract, governance, operations, testing, active-work, and intent files.
- [x] Confirm fallback contract because no skill-change intent exists.
- [x] Inspect skill schema, schema tests, and example skills.
- [x] Search active and paused workstreams for related I-VSD work.
- [x] Inventory and read core thesis source material.
- [x] Read additional thesis material on value-source hierarchy, VSD limits, methodological limitations, objections, trust, technical tradeoffs, and stack evaluation.
- [x] Create `dev/active/i-vsd-skill/i-vsd-skill-plan.md`.
- [x] Create `dev/active/i-vsd-skill/i-vsd-skill-context.md`.
- [x] Create `dev/active/i-vsd-skill/i-vsd-skill-tasks.md`.
- [x] Expand `i-vsd-skill-plan.md` with self-contained framework extraction, resource authoring briefs, `SKILL.md` blueprint, output modes, and file-level implementation sequence.
- [x] Add exact thesis-source routing requirements for every implementation phase and resource category.

Phase exit criteria:

- [x] All three dev-docs files exist.
- [x] All three dev-docs files contain `Last Updated: 2026-05-30 Europe/Brussels`.
- [x] The plan clearly states implementation has not started.
- [x] The plan includes enough extracted thesis context for ordinary implementation without reopening the thesis.

## Context-Limit Handoff Refresh - 2026-05-30 Europe/Brussels

- [x] Read `dev/active/README.md`.
- [x] Read `dev/HANDOFF_TEMPLATE.md`.
- [x] Read `.claude/commands/dev-docs.md`.
- [x] Read `dev/_journal/README.md` and `dev/_journal/FINDING_TEMPLATE.md`.
- [x] Re-read `i-vsd-skill-plan.md`, `i-vsd-skill-context.md`, and `i-vsd-skill-tasks.md`.
- [x] Run `git status --short` to identify unrelated dirty worktree changes.
- [x] Run `git diff --name-only` to identify tracked dirty files.
- [x] Confirm `.claude/skills/i-vsd/**` does not exist yet.
- [x] Re-baseline `i-vsd-skill-plan.md` from planning-only to implementation-ready status.
- [x] Refresh `i-vsd-skill-context.md` with current state, validation state, risks, and handoff.
- [x] Refresh this task file with handoff status and next implementation slice.
- [x] Run `rtk git diff --check -- dev/active/i-vsd-skill/i-vsd-skill-plan.md dev/active/i-vsd-skill/i-vsd-skill-context.md dev/active/i-vsd-skill/i-vsd-skill-tasks.md`.

Handoff exit criteria:

- [x] Plan checked and updated because outcome/status changed.
- [x] Context contains a fresh dated session progress snapshot.
- [x] Context contains a dated handoff.
- [x] Tasks reflect implementation not started and Phase 1 next.
- [x] Unrelated dirty worktree changes are called out in context.
- [x] Markdown diff check passes for touched dev-doc files.

## Phase 1 - Skill Scaffold And Schema Compliance

- [ ] Create `.claude/skills/i-vsd/`.
- [ ] Create `.claude/skills/i-vsd/resources/`.
- [ ] Create `.claude/skills/i-vsd/SKILL.md` with required YAML frontmatter.
- [ ] Add two `ABOUTME` comments after frontmatter in `SKILL.md`.
- [ ] Add required schema sections in order: `Purpose`, `When to Load`, `When NOT to Load`, `Must-Read Docs`, `Top 5 Invariants`, `Top 5 Anti-Patterns`, `Minimal Examples`, `Verification Hooks`, `Related Skills`.
- [ ] Keep `SKILL.md` under 250 lines.
- [ ] Link from `SKILL.md` to resource files or a resource index.

Acceptance criteria:

- [ ] Folder name and frontmatter `name` both equal `i-vsd`.
- [ ] `SKILL.md` is a workflow router, not a full thesis dump.
- [ ] Claim-boundary language is present in `Purpose`, `Top 5 Invariants`, and examples.

## Phase 2 - Core Framework Resources

- [ ] Create `resources/framework-overview.md`.
- [ ] Create `resources/glossary.md`.
- [ ] Create `resources/principles-and-domains.md`.
- [ ] Create `resources/derivation-protocol.md`.
- [ ] Create `resources/evidence-and-validation-levels.md`.
- [ ] Create `resources/scholarly-consultation-boundaries.md`.

Acceptance criteria:

- [ ] All core selected principles are represented.
- [ ] Six domains are represented: Strategic, Design, Technical, Operational, Governance, Evaluation.
- [ ] Derivation chain is explicit and auditable.
- [ ] Evidence-level distinctions are explicit.
- [ ] Escalation triggers for qualified scholarly review are explicit.

## Phase 3 - Domain Heuristic Resources

- [ ] Create `resources/data-governance-heuristics.md`.
- [ ] Create `resources/content-moderation-heuristics.md`.
- [ ] Create `resources/ai-and-algorithmic-heuristics.md`.
- [ ] Create `resources/marketing-and-communication-heuristics.md`.
- [ ] Create `resources/business-model-heuristics.md`.
- [ ] Create `resources/architecture-heuristics.md`.
- [ ] Create `resources/ux-and-defaults-heuristics.md`.
- [ ] Create `resources/strategic-decision-framework.md`.
- [ ] Create `resources/technical-decision-framework.md`.
- [ ] Create `resources/design-decision-framework.md`.
- [ ] Create `resources/operational-framework.md`.
- [ ] Create `resources/governance-and-accountability-framework.md`.
- [ ] Create `resources/legal-and-compliance-framework.md`.
- [ ] Create `resources/evaluation-metrics.md`.

Acceptance criteria:

- [ ] Each resource includes actionable review questions.
- [ ] Each resource includes evidence expectations.
- [ ] Each resource includes anti-pattern signals where relevant.
- [ ] Domain resources preserve the thesis boundary between design reasoning and outcome proof.

## Phase 4 - Consultancy, Reports, And Compliance Workflows

- [ ] Create `resources/consultancy-workflow.md`.
- [ ] Create `resources/report-templates.md`.
- [ ] Create `resources/compliance-checks.md`.
- [ ] Create `resources/industry-anti-patterns.md`.

Acceptance criteria:

- [ ] Consultancy workflow supports intake, scoping, stakeholder mapping, principle selection, domain review, evidence assessment, recommendations, and escalation.
- [ ] Report templates support executive summaries, detailed audits, design feedback, implementation review, compliance-style checks, and risk registers.
- [ ] Compliance checks can produce pass/concern/fail style findings without claiming certification.
- [ ] Anti-pattern resource covers deception, surveillance, dark patterns, lock-in, AI washing, fake trust signals, predatory monetization, unfair competition, and enshittification.

## Phase 5 - ISLAMU Event Case Patterns

- [ ] Create `resources/islamu-event-case-patterns.md`.
- [ ] Include strategic patterns from `4.2.3`.
- [ ] Include technical patterns from `4.2.4`.
- [ ] Include UX patterns from `4.2.5`.
- [ ] Include business-model patterns from `4.2.6`.
- [ ] State clearly that the case is illustrative traceability, not operational proof.

Acceptance criteria:

- [ ] Case resource covers curation, anti-scam safeguards, ticketing/payments, privacy, federation/portability, self-hosting, tenant isolation, authorization affordances, rate limiting, and open-source stewardship.
- [ ] Case resource distinguishes design claims from validated outcomes.

## Phase 6 - Verification And Cleanup

- [ ] Confirm all new files start with two `ABOUTME` comments, except where YAML frontmatter must come first.
- [ ] Confirm all resource files are linked from `SKILL.md` or a resource index.
- [ ] Confirm no resource uses forbidden ASCII diagrams.
- [ ] Confirm no resource claims fatwa, Sharia certification, product certification, or empirical proof.
- [ ] Confirm `SKILL.md` is under 250 lines.
- [ ] Run schema-focused architecture tests.
- [ ] Run broader architecture tests.
- [ ] Run repository build if any shared schema/test/docs behavior changed.
- [ ] Update `i-vsd-skill-context.md` with verification results.
- [ ] Update this file with final status.

Validation commands:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AgentContextSchemaTests
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet build --configuration Release --verbosity quiet
```

## Verification Checklist

- [x] Planning files read back after creation.
- [x] `Last Updated: 2026-05-30 Europe/Brussels` present in all planning files.
- [x] Implementation plan includes required `/dev-docs` sections.
- [x] Context file includes SESSION PROGRESS, Quick Resume, Key Files, Key Decisions, Constraints, Validation Baseline, Known Risks, and Handoff Notes.
- [x] Tasks file includes status summary, maintenance rules, phase checklist, verification checklist, and deferred work.
- [x] Future implementation tests documented.
- [x] Expanded plan appendices verified by heading/content search.
- [x] Thesis routing references in this tasks file validated against the local thesis folder.

## Remaining Or Deferred Work

- [ ] Implement the actual `.claude/skills/i-vsd` skill and resources.
- [ ] Consider adding an `agent_context_skill_change` intent to `.claude/contract/intents.yaml` if skill authoring becomes recurring work.
- [ ] Consider expanding link tests to cover all migrated skill resource links.
- [ ] Re-sync resources if the thesis source changes materially.
