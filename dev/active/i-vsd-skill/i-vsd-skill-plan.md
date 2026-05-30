<!-- ABOUTME: Implementation plan for creating the Islamic Value Sensitive Design agentic skill. -->
<!-- ABOUTME: Captures thesis-grounded scope, resource architecture, constraints, phases, and verification. -->

# Islamic Value Sensitive Design Skill Plan

Last Updated: 2026-05-30 Europe/Brussels

## 0. Planning Metadata

Task name: `i-vsd-skill`

Requested outcome: originally create an implementation plan for a full agentic skill at `.claude/skills/i-vsd/SKILL.md` with many referenced resource files under `.claude/skills/i-vsd/resources/`; current active request is to implement that skill from this plan.

Implementation status: re-baselined for implementation. Planning is complete; implementation has not started and no `.claude/skills/i-vsd` files exist yet.

Primary source material: `/home/amir/Amir/Obsidian/mainvault/10 PROJECTS/🟢 amirakrari-Thesis/10-Active/Thesis`.

Intent classification: no matching `.claude/contract/intents.yaml` intent exists for creating or updating `.claude/skills/*` agent-context skills.

Fallback Contract:

- Authoritative rules are `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, `.claude/skills/_SKILL_SCHEMA.md`, `.claude/commands/dev-docs.md`, and `senior-cto-feedback` skill resources.
- Files in scope for implementation are `.claude/skills/i-vsd/SKILL.md`, `.claude/skills/i-vsd/resources/*.md`, and this `dev/active/i-vsd-skill/*` workstream.
- Minimum verification after implementation is agent-context schema testing, documentation/resource link checks where applicable, and the repository build if any context test behavior changes.
- Add a follow-up task to consider a dedicated `agent_context_skill_change` intent if this kind of work recurs.

## Re-baseline — 2026-05-30 Europe/Brussels

- **Reason:** The user changed the workstream from planning to implementation, then requested a context-limit handoff before implementation files were created.
- **What changed:** This plan now governs implementation, not only planning. The implementation target remains `.claude/skills/i-vsd/SKILL.md` plus the planned resource library.
- **Plan impact:** No architecture or resource scope changed. Phase 1 remains the next slice; the skill scaffold must be created before any resource drafting.
- **Remaining work:** Implement all skill files, run schema/context verification, update context/tasks after each phase, and preserve unrelated dirty worktree changes outside this workstream.

## 1. Executive Summary

Implement a schema-compliant `i-vsd` skill that operationalizes the thesis' Islamic Value Sensitive Design framework as an all-in-one agentic workflow for consultancy, reports, compliance checks, direction/design feedback, product strategy, implementation review, due diligence, and moral risk analysis.

The skill must not be a small summary. `SKILL.md` should be concise and schema-compliant, while the framework depth lives in many resource files. The resources should translate the thesis into practical agent workflows: principle-to-domain mapping, auditable heuristic derivation, evidence-level classification, domain checklists, anti-pattern detection, report templates, and escalation boundaries for scholarly consultation.

The implementation must preserve the thesis' central boundaries: I-VSD is a provider-responsibility design-reasoning framework grounded in selected Sunni Islamic ethical principles, not a fatwa engine, Sharia certification, product certification, or proof of operational ethical outcomes.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

Repository evidence:

- `AGENTS.md` requires intent classification, authoritative docs/rules, scoped edits, minimum tests, and two-line `ABOUTME` headers for every file.
- `.claude/contract/intents.yaml` has no direct intent for agent-context skill creation.
- `.claude/skills/_SKILL_SCHEMA.md` requires `.claude/skills/<kebab-case-name>/SKILL.md`, matching frontmatter `name`, and resources under `.claude/skills/<name>/resources/*.md`.
- `Event.Architecture.Tests/AgentContextSchemaTests.cs` enforces skill frontmatter keys, required section order, and a hard `SKILL.md` line cap of 250 lines.
- `Event.Architecture.Tests/AgentContextLinkTests.cs` currently link-checks a selected migrated skill set, but future implementation should still keep all local links valid.
- Existing skills such as `clean-architecture-rules` and `cto-consultation` demonstrate the right pattern: short schema-compliant `SKILL.md`, long details in resources, relative links, and practical workflows.
- No existing `.claude/skills/i-vsd` directory was found.
- No existing `dev/active/i-vsd-skill` workstream was found.
- A paused `dev/pause/codebase-docs-update` workstream is related only as general skill/resource modernization; it does not cover I-VSD.

Thesis evidence:

- The thesis directory contains 180 Markdown files across Introduction, Theoretical Framework, Methodology, Case Study, Discussion, Conclusion, References, and Appendices.
- `Abstract.md` frames I-VSD as a reusable Islamic provider-responsibility framework for provider-mediated software solutions, using VSD as design-sensitivity reference while grounding normative authority in selected Sunni Islamic ethical principles.
- `1.2-Main-Research-Question.md` asks how selected Islamic ethical principles can be translated into a reusable provider-responsibility framework and applied to ISLAMU Event; it explicitly excludes empirical success proof and replacement of qualified Islamic legal judgement.
- `1.3-Goal-and-Scope.md` scopes I-VSD to web platforms, SaaS, APIs, self-hostable platforms, and related tools where the provider retains continuing ethical responsibility.
- `1.4-Key-Definitions.md` defines provider-mediated software solution, I-VSD, VSD, design heuristic, ethical harm, stakeholder, dark pattern, enshittification, and Islamic ethical terms.
- `2.3.1` and `2.3.2` define the selected principle set: Trust, Truthfulness, Justice, Non-Harm, Rights of People, Avoiding Interest/Usury, Avoiding Excessive Uncertainty, Avoiding Deception, Promise-Keeping, Excellence, Modesty, and Avoiding Spying.
- `3.2.1` defines the auditable derivation chain: principle, Sunni ethical context, provider responsibility, harm/value tension, design question, provider-facing heuristic, evidence needed, certainty/contestability.
- `3.2.2` defines six framework domains: Strategic, Design, Technical, Operational, Governance, and Evaluation.
- `3.2.3` through `3.2.7` define domain heuristics for data governance, content moderation, AI/algorithms, marketing/communication, and business model.
- `3.3.1` and `3.3.2` define architecture and UX heuristics, including avoiding lock-in, data sovereignty, security as moral obligation, continuity/exit, no dark patterns, transparent pricing, and context-appropriate defaults.
- `5.3.1`, `5.3.3`, `5.3.5`, and `5.3.6` establish the command-oriented frame: obeying moral commands is the success condition; worldly success metrics cannot override trustworthiness, non-harm, rights, promises, excellence, integrity, and stewardship.
- `5.4.23` prioritizes user welfare and genuine interest over user satisfaction or convenience, especially in defaults.
- `6.Conclusion.md`, `6.2.1`, and `6.2.3` define validation limits and future validation needs: scholar review, practitioner review, stakeholder research, audits, additional cases, and longitudinal operational study.
- `8.Appendices/Appendix-A*` and `A.1` through `A.7` provide reference tables for strategic, technical, design, marketing, AI/ML, operational, legal, and compliance heuristics.
- `Appendix-D-Industry-Anti-Patterns-Summary.md` provides anti-pattern categories covering deception, open-source governance abuse, privacy/surveillance, security failures, dark patterns, enshittification, lock-in, predatory pricing, fake trust signals, and unfair competition.
- `4.2.3` through `4.2.6` show ISLAMU Event application patterns for strategic, technical, UX, and business-model decisions, with explicit caution that these are illustrative traceability examples, not outcome proof.

### 2.2 Existing Implementation

There is no existing `i-vsd` skill. The repository has a mature agent-context schema and examples, so implementation should be additive rather than inventing a new format.

### 2.3 Existing Constraints

- `SKILL.md` must start with required YAML frontmatter and contain the required schema sections in order.
- `SKILL.md` must stay under 250 lines; target 60-180 lines.
- Long framework content must be in `resources/*.md` files and linked from `SKILL.md`.
- Do not add the new skill to any schema-test skip list.
- Do not introduce ASCII diagrams.
- New Markdown files should start with two `ABOUTME` comments unless a known schema requires otherwise.
- Keep thesis material local and private; do not send it to external web or research tools.

### 2.4 Existing Tests And Enforcement

The key enforcement project is `Event.Architecture.Tests/Event.Architecture.Tests.csproj`. Relevant test classes include `AgentContextSchemaTests`, `AgentContextIntentManifestTests`, `AgentContextDuplicationTests`, and documentation/link-quality tests.

### 2.5 Related Workstreams

`dev/pause/codebase-docs-update` is adjacent because it concerns skill/resource modernization. It does not contain direct I-VSD scope, so the correct action is a new active workstream.

### 2.6 Unknowns

- Whether all thesis files should be cited directly in resources or only the stable source chapters and appendices.
- Whether the final skill should be `type: workflow` or `type: reference`; the current recommendation is `workflow` because the user wants agentic consultancy, checks, reports, and feedback.
- Whether future schema/link tests should be expanded to include all resource links for new skills.
- Whether the thesis will later receive edits that require resource synchronization.

## 3. Proposed Future State

Create `.claude/skills/i-vsd/SKILL.md` plus a resource library under `.claude/skills/i-vsd/resources/`.

Recommended `SKILL.md` frontmatter:

```yaml
---
name: i-vsd
description: Apply the Islamic Value Sensitive Design provider-responsibility framework for consultancy, reports, compliance checks, and design or implementation feedback.
type: workflow
enforcement: suggest
priority: high
---
```

Recommended `SKILL.md` behavior:

- Load when asked for Islamic Value Sensitive Design, Islamic software ethics, ethical product review from the thesis framework, consultancy reports, compliance checks, design direction, strategic moral review, architecture/UX/data/AI/business model review, or ISLAMU Event moral design feedback.
- Do not load for generic Islamic legal questions, fatwa requests, Sharia certification, personal religious advice, or claims that a product is Islamically certified.
- Require the agent to identify solution boundary, provider responsibilities, stakeholders, relevant principles, domain decisions, evidence level, harms, tradeoffs, and escalation needs.
- Produce outputs with explicit claim boundaries: design reasoning, not religious-legal ruling; traceability, not empirical proof; recommendations, not certification.

Recommended resource files:

- `resources/framework-overview.md`
- `resources/glossary.md`
- `resources/principles-and-domains.md`
- `resources/derivation-protocol.md`
- `resources/evidence-and-validation-levels.md`
- `resources/consultancy-workflow.md`
- `resources/report-templates.md`
- `resources/compliance-checks.md`
- `resources/data-governance-heuristics.md`
- `resources/content-moderation-heuristics.md`
- `resources/ai-and-algorithmic-heuristics.md`
- `resources/marketing-and-communication-heuristics.md`
- `resources/business-model-heuristics.md`
- `resources/architecture-heuristics.md`
- `resources/ux-and-defaults-heuristics.md`
- `resources/strategic-decision-framework.md`
- `resources/technical-decision-framework.md`
- `resources/design-decision-framework.md`
- `resources/operational-framework.md`
- `resources/governance-and-accountability-framework.md`
- `resources/evaluation-metrics.md`
- `resources/legal-and-compliance-framework.md`
- `resources/industry-anti-patterns.md`
- `resources/scholarly-consultation-boundaries.md`
- `resources/islamu-event-case-patterns.md`

## 4. Non-Negotiable Constraints

- The skill must fully honor the thesis framework and its boundaries.
- The skill must be substantial and all-in-one, but schema-compliant.
- The skill must not claim to issue fatwas, Sharia certification, religious rulings, or empirical validation.
- The skill must not flatten Islamic principles into generic ethics. VSD is a design-sensitivity reference, not the moral source.
- The skill must preserve evidence levels: theological/Islamic validation, design validation, stakeholder validation, and operational validation.
- The skill must include all core heuristic domains and anti-pattern coverage.
- The skill must be useful for consultancy, reports, compliance checks, direction/design feedback, implementation review, and strategic/product assessment.
- All resource files must be referenced from `SKILL.md` or an index resource so future agents can discover them.
- Implementation must not alter application code or test skip lists.

## 5. Architecture And Design Decisions

Decision 1: use `SKILL.md` as workflow router, not a large knowledge dump.

Rationale: schema line limits require a concise entrypoint. Resources preserve depth without violating agent-context tests.

Decision 2: model I-VSD as a provider-responsibility review protocol.

Rationale: the thesis repeatedly frames moral accountability around provider choices in business model, UI, architecture, data, policy, operations, and evaluation.

Decision 3: separate principles, domains, heuristics, anti-patterns, templates, and escalation rules.

Rationale: this supports different user tasks without forcing every output to be a long report.

Decision 4: include report templates and compliance check formats.

Rationale: the user explicitly requested consultancy, reports, compliance checks, direction/design feedback, and more.

Decision 5: require claim-boundary language in every formal output.

Rationale: the thesis explicitly says I-VSD is not product certification, religious-legal opinion, or proof of outcome success.

Decision 6: retain local thesis references in planning docs, but avoid requiring future skill users to access the private thesis path.

Rationale: the skill should be self-contained inside `.claude/skills/i-vsd` while respecting source privacy.

## 6. Implementation Phases

### Phase 1 - Scaffold And Schema Compliance

Create `.claude/skills/i-vsd/SKILL.md` and `.claude/skills/i-vsd/resources/`. Add required frontmatter, two `ABOUTME` comments, and required schema sections in order.

Acceptance criteria:

- Folder name and frontmatter name both equal `i-vsd`.
- Required schema sections exist in order.
- `SKILL.md` is under 250 lines.
- `SKILL.md` links to the resource library.

### Phase 2 - Framework Core Resources

Create resources for framework overview, glossary, principles/domains, derivation protocol, evidence levels, and scholarly consultation boundaries.

Acceptance criteria:

- Core selected principles and six domains are represented.
- Derivation chain is auditable.
- Claim boundaries are explicit.
- Scholarly escalation triggers are explicit.

### Phase 3 - Domain Heuristic Resources

Create resources for data governance, content moderation, AI/algorithms, marketing/communication, business model, architecture, UX/defaults, operations, governance, legal/compliance, and evaluation metrics.

Acceptance criteria:

- Each resource contains actionable heuristics, review questions, anti-pattern indicators, and evidence expectations.
- Domain resources preserve the distinction between design reasoning and outcome proof.
- Data, AI, marketing, and business-model resources include the thesis' highest-risk abuse patterns.

### Phase 4 - Consultancy, Reports, And Compliance Workflows

Create resources for consultancy workflow, report templates, compliance checks, and direction/design feedback modes.

Acceptance criteria:

- The skill can produce short advisory feedback, structured compliance checklists, executive reports, design review memos, risk registers, and implementation review findings.
- Outputs include solution boundary, provider responsibility, stakeholders, principles, domains, evidence level, risks, recommendations, and escalation needs.

### Phase 5 - ISLAMU Event Case Patterns

Create `resources/islamu-event-case-patterns.md` from the case-study chapters.

Acceptance criteria:

- Case patterns cover curation, anti-scam safety, privacy-oriented strategy, ethical ticketing/payments, federation/portability, self-hosting, HATEOAS authorization affordances, tenant isolation, rate limiting, and open-source stewardship.
- The resource states the case is illustrative and not operational proof.

### Phase 6 - Verification And Cleanup

Run schema and documentation verification. Read the skill entrypoint and resources for link consistency, duplicate drift, line length, and boundary language.

Acceptance criteria:

- Agent-context schema tests pass.
- No resource is orphaned.
- No broken relative links are introduced.
- No implementation claims exceed the thesis evidence level.

## 7. Testing Strategy

Primary implementation verification:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AgentContextSchemaTests
```

Recommended broader verification:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Repository build if implementation changes any test enforcement, contracts, or shared docs:

```bash
dotnet build --configuration Release --verbosity quiet
```

Manual verification:

- Confirm `SKILL.md` has required frontmatter and required sections in order.
- Confirm `SKILL.md` is below 250 lines.
- Confirm every resource file starts with two `ABOUTME` comments.
- Confirm every resource linked by `SKILL.md` exists.
- Confirm all formal output templates include claim boundaries.
- Confirm no resource presents I-VSD as a fatwa, certification, or empirical proof.

## 8. Documentation, Configuration, And Operations Impact

Documentation impact is limited to `.claude/skills/i-vsd/*` and `dev/active/i-vsd-skill/*`.

Configuration impact should be none unless a future agent adds a dedicated intent or expands link-test coverage.

Operations impact is none for the application runtime. The skill affects agent behavior only.

## 9. Security, Authorization, Privacy, And Abuse Considerations

The skill itself does not process runtime user data, but its resource content must teach privacy and abuse analysis responsibly.

Required security/privacy coverage:

- Data minimization, purpose limitation, retention, access control, export/deletion, privileged-access logging, and intrusive-signal review.
- Avoiding unjustified spying, profiling, tracking, inference, and surveillance monetization.
- Security as moral obligation: password hashing, encryption, key management, patching, vulnerability reporting, dependency trust boundaries, continuity, backup, and exit.
- AI source integrity, hallucination disclosure, human escalation for high-impact decisions, bias/disparate-harm review, and non-manipulative recommendation alternatives.
- Moderation abuse safeguards: clear rules, escalation, appeals, scam/impersonation priority, and consistent enforcement.

The skill must not inspect local roles/claims or application authorization logic unless a user explicitly asks for repository implementation review. If reviewing this repository, HAL affordances remain the UI authorization source of truth.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

Required product considerations:

- Multi-tenancy: treat tenant isolation, admin boundaries, export/delete behavior, support workflows, and trusted tenant resolution as ethical trust boundaries.
- Federation: treat portability, instance choice, identity trust, moderation propagation, and abuse handling as provider-responsibility topics.
- Localization: treat language, cultural context, prayer-relative scheduling, and context-sensitive defaults as part of fairness and user welfare.
- Accessibility: treat WCAG and inclusive UX as excellence and justice obligations, not polish.
- Product strategy: reject dark patterns, artificial scarcity, hidden fees, manipulative defaults, spiritual guilt/fear tactics, and enshittification patterns.

## 11. Observability And Operations

The future skill should recommend operational evidence where claims depend on outcomes:

- Harm reports and unresolved ethical risks.
- User complaints, appeals, support burden, moderation outcomes, privacy/security incidents, and accessibility failures.
- Portability/export success, deletion completion, continuity/backup tests, and incident response times.
- Claim-evidence registers for marketing and product promises.
- Longitudinal review rather than one-time launch approval.

## 12. Migration And Compatibility Plan

This is additive. No migration of application data, production config, or existing skill behavior is needed.

Compatibility requirements:

- Do not modify existing skills unless needed for cross-linking and explicitly justified.
- Do not break `_SKILL_SCHEMA.md` conventions.
- Do not introduce skip-list exceptions for `i-vsd`.
- Keep resource filenames kebab-case and stable.
- If future docs index generation is used, ensure `i-vsd` remains discoverable without forcing unrelated skill edits.

## 13. Risk Register

Risk: the skill accidentally becomes a fatwa/certification tool.

Mitigation: add repeated boundary language in `SKILL.md`, report templates, compliance checks, and scholarly consultation resources.

Risk: the skill becomes too large for schema constraints.

Mitigation: keep `SKILL.md` as a router and put long content in resource files.

Risk: thesis nuance is flattened into generic ethics.

Mitigation: preserve the principle/domain/derivation/evidence structure and command-oriented framing.

Risk: resources over-copy thesis prose instead of operationalizing it.

Mitigation: convert into review questions, heuristics, templates, and evidence rubrics while citing source paths in implementation notes as needed.

Risk: private thesis source leaks to external tools.

Mitigation: keep all thesis reading local and do not send thesis content to external research/documentation tools.

Risk: future agents claim implementation is done without tests.

Mitigation: tasks file requires schema tests and manual link/boundary review before completion.

## 14. Success Metrics And Definition Of Done

Definition of Done:

- `.claude/skills/i-vsd/SKILL.md` exists and passes schema tests.
- `.claude/skills/i-vsd/resources/` contains the planned framework, heuristic, workflow, anti-pattern, and report-template resources.
- The skill supports consultancy, reports, compliance checks, direction/design feedback, implementation review, due diligence, and strategic/product assessment.
- The skill preserves thesis boundaries and does not claim fatwa/certification authority.
- Every core principle, every framework domain, and every major thesis heuristic category has a resource home.
- Verification commands have been run and documented in `i-vsd-skill-context.md`.
- `i-vsd-skill-context.md` and `i-vsd-skill-tasks.md` are updated before handoff.

Quality metrics:

- Agent can select the right workflow mode within the first response.
- Formal outputs include evidence level and escalation needs.
- Compliance checks are concrete enough to produce pass/concern/fail style findings.
- Anti-pattern resources catch deception, surveillance, dark patterns, lock-in, AI washing, fake trust signals, predatory monetization, and enshittification.

## 15. Implementation Agent Contract - KEEP DEV DOCS CURRENT

Future implementation agents must update all three files in this workstream as work progresses.

Required behavior:

- Update `i-vsd-skill-context.md` after every meaningful implementation step.
- Mark tasks in `i-vsd-skill-tasks.md` as completed only after files are edited and verified.
- Add newly discovered risks or scope changes to this plan before implementing them.
- Preserve the planning-only history and clearly distinguish implementation work from this planning session.
- Do not claim completion until tests and manual resource checks are done.

## 16. Progress Reporting Contract

When reporting progress, future agents should include:

- Files created or modified.
- Framework areas completed.
- Resources still missing.
- Tests run and results.
- Thesis-boundary or scholarly-escalation risks discovered.
- Whether the dev docs were updated in the same step.

## 17. Potential Risks & Unknowns

The biggest unknown is not repository mechanics; it is moral fidelity. The final skill must preserve the thesis' provider-responsibility structure, evidence levels, and non-certification boundaries while still being practical enough for consultancy and compliance-style outputs. The second risk is scope control: the user explicitly asked for a large all-in-one skill, but the repository schema requires a compact `SKILL.md`, so depth must be distributed across resources without becoming fragmented or hard to use.

## Appendix A - Self-Contained I-VSD Framework Extraction

This appendix is the implementation knowledge base. The future skill author should be able to write `SKILL.md` and the resource files from this plan without reopening the thesis, while still preserving the thesis' claim boundaries.

### A.1 Core Identity Of I-VSD

I-VSD is an Islamic provider-responsibility framework for software. It addresses provider-mediated software solutions where an organization, maintainer, platform owner, SaaS operator, API provider, self-hostable project steward, or related technical provider retains continuing moral responsibility for design choices, data practices, availability, governance, policies, incentives, or stakeholder effects.

I-VSD uses Value Sensitive Design as a design-sensitivity reference, not as the source of moral authority. VSD contributes attention to stakeholders, values, technical design choices, empirical investigation, and conceptual analysis. I-VSD changes the value-authority structure: selected Sunni Islamic ethical principles provide normative boundaries, while stakeholder analysis helps understand effects, harms, tradeoffs, and implementation consequences.

The framework is constructive and provider-facing. It is meant to help a software provider reason systematically about moral duties before, during, and after implementation. It should guide strategic choices, UX choices, architecture, data governance, moderation, AI behavior, marketing, business models, operations, accountability, evaluation, and discontinuation.

The framework is not:

- A fatwa engine.
- Sharia certification.
- Product certification.
- Proof that a product is ethical.
- Proof that ISLAMU Event achieved ethical outcomes.
- A replacement for qualified Islamic legal judgement.
- A generic secular VSD checklist with Islamic words added.
- A universal mandate for every software provider regardless of its explicit values.

The skill must say this repeatedly in high-stakes outputs. It can produce design reasoning, moral-risk analysis, compliance-style checks, reports, and recommendations, but it must not issue religious rulings or certify moral completion.

### A.2 Value Source Hierarchy

The thesis' value-source note is central and must be preserved in the skill.

Primary source hierarchy:

- Selected Sunni Islamic ethical sources govern the framework's values and boundaries.
- Compatible secular standards can help operationalize Islamic principles in software practice.
- Secular frameworks, legal standards, industry practices, and cultural norms do not override Islamic boundaries.
- When secular sources align with Islamic ethics, they can provide additional practical guidance.
- When secular sources conflict with Islamic principles, Islamic guidance takes precedence.

Operational hierarchy for the skill:

1. Absolute Islamic prohibitions are never compromised, such as deception and interest/usury where applicable.
2. Islamic recommendations are strongly preferred even if not legally required.
3. Complementary secular standards are adopted when they do not contradict Islamic principles.
4. Cultural best practices are considered when neutral to Islamic principles.

The skill should avoid presenting legal compliance as moral sufficiency. A practice may be legal, common, profitable, and still morally problematic under I-VSD.

### A.3 Why Standard VSD Is Insufficient For This Context

The skill should explain that I-VSD does not reject standard VSD. Instead, it modifies VSD for religiously grounded providers.

Standard VSD limitations for religious contexts:

- Standard VSD often treats values as emerging from stakeholder consultation, conceptual analysis, and philosophical reflection.
- A stakeholder survey cannot override divine command.
- User preferences cannot supersede scriptural prohibition.
- VSD literature often assumes a secular-liberal baseline and gives systematic attention to values such as autonomy and privacy, but does not systematically treat God-consciousness, Modesty, communal obligations, religious time structures, permissible business models, or avoidance of interest-based transactions.
- VSD can surface tradeoffs, but it does not by itself decide which principles are non-negotiable for an Islamic provider.

I-VSD keeps stakeholder concern, but stakeholder claims are evaluated through duties such as Trust, Justice, Non-Harm, rights of people, public good, truthfulness, promise-keeping, and accountability before God. Stakeholder preference matters, but it is not preference aggregation alone.

### A.4 Selected Principles And Their Software Meaning

The future resource `principles-and-domains.md` must include these principles and practical meanings.

Trust / Amanah:

- Software meaning: stewardship of data, infrastructure, content, community trust, uptime promises, support commitments, platform governance, and administrative power.
- Review focus: Can the provider explain what it holds in trust, who can access it, how it can fail, and how users can recover or exit?
- Typical violations: unlogged admin access, hidden dependency on fragile infrastructure, unclear support promises, misleading reliability claims, careless security, unreviewed third-party data sharing.

Truthfulness / Sidq:

- Software meaning: accurate claims about features, pricing, limitations, AI behavior, open-source status, privacy, availability, comparisons, and evidence.
- Review focus: Are claims bounded, dated, sourced, and understandable?
- Typical violations: AI washing, false scarcity, inflated feature claims, fake open-source association, hidden limitations, vague roadmap promises presented as current capability.

Justice / Adl:

- Software meaning: fair access, fair pricing, fair moderation, fair ranking, fair support, non-arbitrary enforcement, and concern for vulnerable users.
- Review focus: Are rules applied consistently and are appeal/correction paths available?
- Typical violations: arbitrary moderation, hidden ranking bias, exploitative pricing, inaccessible flows, opaque account penalties, unequal support treatment.

Non-Harm / La Darar:

- Software meaning: avoid privacy, security, financial, spiritual, social, reputational, community, and operational harms.
- Review focus: What harms can this design cause even if users consent superficially?
- Typical violations: surveillance analytics, addictive loops, scam exposure, unsafe automation, negligent backups, weak abuse controls, harmful defaults.

Rights Of People / Huquq al-Ibad:

- Software meaning: respect property, consent, reputation, dignity, privacy, fair dealing, access to what users own, and just treatment.
- Review focus: Can users understand, control, export, delete, correct, and contest matters affecting their rights?
- Typical violations: reputation damage without appeal, refusal to export user-owned data, confusing consent, hidden fees, lock-in that traps users' work.

Avoiding Interest/Usury / Riba:

- Software meaning: review financing, payment, credit, subscription, late-fee, revenue, and dependency models where interest/usury concerns may apply.
- Review focus: Does money flow depend on prohibited or ethically compromising arrangements?
- Typical violations: interest-based growth dependence, hidden finance costs, predatory credit, sponsor/investor pressure that undermines Islamic commitments.

Avoiding Excessive Uncertainty / Gharar:

- Software meaning: terms, pricing, renewals, risk, data use, dependencies, and capabilities should be clear enough for responsible choice.
- Review focus: Can a normal user understand what they are buying, giving up, depending on, and risking?
- Typical violations: unclear pricing, surprise renewals, hidden cancellation friction, ambiguous ownership, vague AI accuracy claims, undisclosed dependency risks.

Avoiding Deception / Tadlis-Ghish style concerns:

- Software meaning: avoid hidden defects, manipulative defaults, fake trust signals, dark patterns, misleading comparisons, and opaque motives.
- Review focus: Does the interface exploit inattention, fear, guilt, urgency, confusion, or asymmetry?
- Typical violations: preselected paid options, equal-looking but unequal choices, fake reviews, guilt-based prompts, hidden data sale, cancellation obstruction.

Promise-Keeping / Wafa bil-Ahd:

- Software meaning: honor privacy promises, lifetime deals, portability claims, support expectations, licensing commitments, uptime statements, pricing promises, and lifecycle promises.
- Review focus: What has the provider explicitly or implicitly promised, and how is it tracked?
- Typical violations: lifetime-plan reversals, EOL without migration, changing terms without notice, support promises without capacity, license bait-and-switch.

Excellence / Ihsan:

- Software meaning: go beyond minimum compliance in safety, reliability, accessibility, maintainability, testing, documentation, and religious trust.
- Review focus: Is the provider merely avoiding liability, or designing with care?
- Typical violations: minimal legal compliance, untested critical flows, inaccessible UI, undocumented operations, ignoring known harms because they are not illegal.

Modesty / Haya:

- Software meaning: avoid unnecessary exposure, immodest content incentives, attention exploitation, and defaults that normalize harmful visibility.
- Review focus: Does the product pressure users to expose more than needed or engage with unsuitable content?
- Typical violations: public-by-default sensitive data, viral attention loops, permissive content defaults, social comparison pressure, exploitative recommendation feeds.

Avoiding Spying / Tajassus:

- Software meaning: avoid unjustified collection, inference, tracking, profiling, monitoring, fingerprinting, and behavioral surveillance.
- Review focus: Is data collected because it is necessary, or because it is useful to the provider?
- Typical violations: cross-site tracking, intrusive telemetry, location/contact scraping, undisclosed inference, employee/user monitoring without necessity.

### A.5 Domains Of Review

Strategic domain:

- Mission, ownership, funding, investor relationships, partnerships, market positioning, governance structure, open-source posture, business commitments, curation boundary, and long-term stewardship.
- Key question: Does the provider's strategy create pressure toward deception, surveillance, interest/usury, lock-in, enshittification, or abandonment of users?

Design domain:

- UX, information architecture, defaults, flows, consent, accessibility, comparison interfaces, pricing presentation, content controls, persuasive design, and localization.
- Key question: Does the interface help users act in their genuine interest, or exploit inattention, emotion, ignorance, or friction?

Technical domain:

- Architecture, data models, security, auth, authorization, tenancy, APIs, interoperability, AI, algorithms, dependencies, hosting, backups, portability, and infrastructure.
- Key question: Do technical choices make provider responsibilities inspectable, enforceable, reversible, and maintainable?

Operational domain:

- Maintenance, support, incident response, moderation operations, privacy operations, vulnerability handling, backup/recovery, EOL, migrations, and continuity.
- Key question: Are promises operationally supported, or only stated in marketing/docs?

Governance domain:

- Accountability, decision rights, policy ownership, appeal paths, auditability, contribution governance, escalation paths, conflict handling, and partner screening.
- Key question: Who can make harmful decisions, how are they constrained, and how can affected people contest or recover?

Evaluation domain:

- Metrics, harm reviews, stakeholder feedback, audits, support records, complaints, accessibility checks, security incidents, appeal outcomes, and longitudinal monitoring.
- Key question: What evidence would show whether the provider is actually meeting its responsibilities?

### A.6 Auditable Derivation Protocol

Every major heuristic or recommendation should be traceable through this chain:

1. Name the provider decision.
2. Identify affected stakeholders, including indirect and vulnerable stakeholders.
3. Identify the Islamic principle or principles at stake.
4. State the Sunni ethical context at the level appropriate for a software-design framework.
5. Define the provider responsibility created by the principle.
6. Identify the harm, value tension, or anti-pattern risk.
7. Convert the tension into a design question.
8. Convert the design question into a provider-facing heuristic.
9. Define evidence needed to support compliance or confidence.
10. Mark certainty level, contestability, and escalation need.
11. Record tradeoffs and rejected alternatives.
12. State whether the output is a design claim, implementation-traceability claim, stakeholder-validation claim, operational-validation claim, or theological/scholarly-validation claim.

Example pattern:

- Provider decision: collect precise location for event discovery.
- Stakeholders: attendees, organizers, nearby community members, admins, potential abusers.
- Principles: Trust, Non-Harm, Avoiding Spying, Rights of People, Justice.
- Provider responsibility: collect only what is necessary and protect against misuse.
- Risk: stalking, profiling, unnecessary exposure, undisclosed inference.
- Design question: Can approximate location or user-entered region satisfy the feature with less risk?
- Heuristic: default to the least precise location that serves the stated purpose; require explicit justification for precision.
- Evidence: data inventory, purpose statement, UI copy, retention policy, access logs, abuse review.
- Claim boundary: design and implementation-traceability unless audited after deployment.

### A.7 Evidence And Validation Levels

The skill must classify claims using the thesis' evidence ladder.

Theological/Islamic validation:

- What it means: qualified Islamic scholars have reviewed principle selection, translation, and contested applications.
- Current thesis status: not formally completed.
- Skill behavior: mark as needed for finance, religious-content guidance, public harm, AI in high-stakes decisions, privacy edge cases, and disputed obligations.

Design validation:

- What it means: practitioners can understand and apply the heuristics to design decisions.
- Current thesis status: partly supported by constructive framework and case application.
- Skill behavior: produce clear heuristics, checklists, and templates; do not call this empirical proof.

Implementation-traceability evidence:

- What it means: repository/docs/design artifacts show that principles were mapped into concrete choices.
- Current thesis status: strongest for the ISLAMU Event case.
- Skill behavior: use this phrase when reviewing files, architecture, policies, or design docs.

Stakeholder validation:

- What it means: users, organizers, maintainers, affected communities, or other stakeholders have provided interview, survey, usability, participatory, or complaint/appeal evidence.
- Current thesis status: not completed.
- Skill behavior: recommend stakeholder research rather than assuming user trust or benefit.

Operational validation:

- What it means: deployed systems, audits, incident logs, support records, accessibility checks, security reviews, moderation outcomes, and longitudinal use show actual results.
- Current thesis status: not completed for broad claims.
- Skill behavior: require audits and operations evidence before claiming harm reduction, trust, reliability, accessibility success, or sustainability.

### A.8 Command-Oriented Success Frame

The skill must preserve the thesis' command-oriented principle: the provider is commanded to obey moral duties, not guaranteed worldly success.

Practical meaning:

- Reject dark patterns even if they improve conversion.
- Protect data even if selling or over-collecting it would be profitable.
- Tell the truth about limitations even if marketing would be easier without caveats.
- Decline unethical investors, sponsors, or partnerships even when they accelerate growth.
- Maintain standards even when competitors exploit users and grow faster.

Conventional metrics such as valuation, revenue, growth, retention, engagement, and market dominance are morally neutral only depending on how they are achieved and used. They cannot override Trust, Justice, Non-Harm, Rights, Truthfulness, Promise-Keeping, Modesty, or avoidance of prohibited practices.

The skill should define success as building software that pleases God, serves users ethically, protects rights, avoids harm, and trusts God with the outcome. It should not imply that ethical practice always wins the market.

### A.9 User Interest Over User Satisfaction

The thesis distinguishes user welfare/genuine interest from user satisfaction or convenience.

The skill should treat defaults as moral design decisions:

- Hide sensitive data by default.
- Minimize cookies/data collection by default.
- Make marketing opt-in by default.
- Use restrictive privacy defaults by default.
- Require meaningful confirmation for subscription renewals or irreversible actions where risk is high.
- Make cancellation and unsubscribe flows as clear as signup/subscribe flows.

A user may prefer convenience, but providers must not exploit inattention, short-term desire, ignorance, fear, guilt, or confusion. User interest may cost market share, but preserves welfare and moral integrity.

### A.10 Trust, Verification, And Misplaced Trust

Trust is not bad in I-VSD. Misplaced trust is the problem.

Legitimate trust can support:

- Account recovery.
- Operational resilience.
- Human support.
- Business continuity.
- Community cooperation.
- Voluntary consent for optional data use.
- Support for ambitious moral infrastructure before it is feature-complete.

Misplaced trust includes:

- Trusting providers with documented deception history.
- Trusting platforms with misaligned ad-surveillance incentives.
- Trusting services that violated user promises.
- Trusting entities without accountability mechanisms.
- Being manipulated by fake reviews, fake authority signals, emotional marketing, or manufactured trust.

Trustworthiness review signals:

- Track record of keeping promises.
- Incentive alignment with user interests.
- Transparent practices, limits, and policies.
- Meaningful accountability and recourse.
- Third-party audits, certifications, peer review, or public scrutiny.
- Reputation at stake and governance that makes capture harder.
- Public work, clear incentives, sponsor disclosures, and honest limitations.

Islamic trust framing for skill resources:

- Trust in God is complete.
- Trust in humans requires reasonable precautions.
- Taking precautions is not lack of faith.
- Verify, then trust.

### A.11 Technical Tradeoffs And Ethical Friction

The skill must teach that ethical design often involves tradeoffs, not automatic maximalism.

Zero-knowledge and end-to-end encryption example:

- Privacy benefit: provider cannot read user data.
- Surveillance resistance: even legal demands may not expose encrypted content.
- Breach protection: stolen provider data may be useless.
- Resiliency risk: lost keys can destroy access permanently.
- Support risk: no password reset or data recovery may be possible.
- Wealth/preservation concern: businesses can lose years of records.
- I-VSD framing: privacy and avoiding spying support zero-knowledge, but preservation of wealth, risk management, and avoiding excessive uncertainty may support hybrid or trusted recovery approaches.
- Practical heuristic: assess actual threat model, consequences of loss, hybrid possibilities, and user education before selecting maximal privacy architecture.

AI automation and operational fragility:

- AI coding agents, deployment assistants, and infrastructure automations can act faster than human review.
- Risks include destructive API calls, production-data deletion, backup loss, hallucinated legal citations, unexpected billing, and large misunderstood changes.
- Safety controls add friction: scoped credentials, human approval, dry-run modes, separate backup credentials, audit logs, rate limits, rollback plans.
- I-VSD accepts this friction when the alternative gives unreliable systems authority over user data, money, legal claims, or community trust.

### A.12 Objections And Responses The Skill Should Know

Imposition objection:

- Objection: a religious framework forces Islamic values on non-Muslim users.
- Response: I-VSD guides providers that explicitly adopt Islamic principles. It does not mandate religious practice through software. Most outcomes, such as honesty, transparent pricing, secure data, and anti-manipulation, benefit users regardless of faith.

Jurisprudential disagreement objection:

- Objection: Muslims disagree, so the framework cannot represent Islamic ethics.
- Response: I-VSD focuses on broadly accepted principles such as honesty, trustworthiness, fair dealing, and promise-keeping. Where disagreement exists, the skill must mark uncertainty and recommend chosen scholarly authority or qualified consultation.

Competitive viability objection:

- Objection: ethical constraints are fatal in exploitative markets.
- Response: the objection is serious. Ethical practice may create disadvantage, but that does not invalidate the framework. I-VSD does not promise market dominance. It guides providers that prioritize moral accountability over growth at any cost. Regulatory trends may also reduce some exploitative advantages.

Abstraction objection:

- Objection: the framework is too theoretical for developers.
- Response: the resource library must solve this by turning principles into checklists, templates, review questions, and evidence rubrics. Prepared practitioners can apply the framework systematically.

Secular alternative objection:

- Objection: secular ethics frameworks are enough.
- Response: secular frameworks are useful, but for Muslim practitioners they may not connect professional practice to spiritual accountability or define non-negotiable boundaries when exploitation is profitable.

Single-case limitation objection:

- Objection: ISLAMU Event is one self-applied case.
- Response: correct. The case provides implementation-traceability evidence, not broad empirical support. The skill must not generalize beyond design reasoning without independent evidence.

Technology neutrality objection:

- Objection: software is value-free.
- Response: defaults, categories, metrics, permissions, ranking, pricing, moderation, and incentives all express values. I-VSD makes the value source explicit rather than hiding industry assumptions behind neutrality claims.

### A.13 Industry Anti-Pattern Catalogue

The skill's anti-pattern resource should cover at least these categories.

Deception and false claims:

- AI washing.
- Fake reviews.
- Fake users, bots, or manufactured community proof.
- False open-source association.
- Exaggerated privacy/security claims.
- Roadmap features presented as current reality.
- Misleading performance or market claims.

Open-source and transparency abuse:

- Borrowing open-source trust while withholding practical freedoms.
- Legal source availability with unusable self-hosting.
- License or governance bait-and-switch.
- Sponsor influence hidden behind community branding.
- Public code without real portability or operational documentation.

Privacy and surveillance:

- Data harvesting as business model.
- Cross-context tracking.
- Unnecessary telemetry.
- Profiling and inference without clear user benefit.
- Selling or sharing user data without meaningful consent.
- Treating legal privacy compliance as sufficient moral permission.

Security negligence:

- Weak password storage.
- Poor key management.
- No vulnerability reporting path.
- Slow critical patch response.
- Missing backups or untested recovery.
- Admin powers without logs.
- Destructive automation without approval or rollback.

Dark patterns and manipulation:

- False urgency or scarcity.
- Hidden fees.
- Preselected paid options.
- Cancellation friction.
- Confusing consent.
- Guilt or spiritual-pressure prompts.
- Addictive recommendation loops.
- Engagement metrics that reward outrage, anxiety, or immodesty.

Business enshittification and lock-in:

- Start user-friendly, then extract after dependence.
- Trap user data in proprietary formats.
- Break lifetime commitments.
- Raise fees after switching costs grow.
- Degrade free tiers into coercive funnels.
- Hide migration/export limitations.

Unfair competition and reputational abuse:

- Misleading competitor comparisons.
- Selective checkmark tables.
- Negative attacks instead of proportionate critique.
- Trademark or brand confusion.
- Fake authority or fake certification signals.

### A.14 Scholarly Consultation Triggers

The skill must recommend qualified Islamic scholarly consultation when any of these appear:

- Finance, credit, late fees, interest/usury, payment structures, investment, sponsorship, or monetization with possible riba concerns.
- Public religious guidance, religious education, fatwa-like outputs, Quran/Hadith interpretation, or claims about Islamic permissibility.
- Content moderation of contested religious, political, gender, family, sectarian, or community matters.
- AI systems generating religious, legal, medical, financial, or high-impact guidance.
- Privacy or surveillance tradeoffs affecting vulnerable groups, families, minors, religious communities, or public harm.
- Jurisprudential disagreement between recognized schools or scholars.
- Cases where a heuristic would impose significant burden, exclusion, or harm and principle application is uncertain.
- Any request asking the agent to declare a product, practice, contract, or business model definitively halal/haram.

The skill should still provide design-level questions and risk analysis, but it must mark the religious-legal conclusion as outside scope.

## Appendix B - Resource Authoring Briefs

Every future resource file should start with two `ABOUTME` comments and should be practical, not merely descriptive. Each resource should include purpose, when to use, checklist/review questions, evidence expectations, anti-patterns, output guidance, and claim boundaries where relevant.

### B.1 `resources/framework-overview.md`

Write this as the conceptual entrypoint for users and agents.

Must include:

- I-VSD definition as Islamic provider-responsibility framework.
- Provider-mediated software scope: web platforms, SaaS, APIs, self-hostable systems, CLI/MCP/AI-native/mobile/plugins when provider responsibility continues.
- VSD relationship: design-sensitivity reference, not moral source.
- Value source hierarchy from Appendix A.2.
- Six domains from Appendix A.5.
- Claim boundaries.
- A quick workflow summary: scope solution, identify provider responsibilities, map stakeholders, select principles, review domains, derive heuristics, classify evidence, produce recommendations, escalate if needed.

### B.2 `resources/glossary.md`

Include concise definitions for:

- I-VSD.
- Provider-mediated software solution.
- Software service.
- Value Sensitive Design.
- Design heuristic.
- Ethical harm.
- Stakeholder.
- Dark pattern.
- Enshittification.
- Amanah / Trust.
- Sidq / Truthfulness.
- Adl / Justice.
- La Darar / Non-Harm.
- Huquq al-Ibad / Rights of People.
- Riba.
- Gharar.
- Tadlis / deception concerns.
- Haya / Modesty.
- Ihsan / Excellence.
- Tajassus / spying.
- Barakah / blessing, with warning that it is theological and not a measurable KPI.
- Fatwa, Sharia, Sunnah, Hadith, Quran, Maqasid, qiyas, public interest, certainty/contestability.

### B.3 `resources/principles-and-domains.md`

Use Appendix A.4 and A.5 as base content.

Must include:

- Principle table with software meaning, typical provider decisions, typical violations, and evidence examples.
- Domain table with strategic/design/technical/operational/governance/evaluation responsibilities.
- Principle-to-domain examples, such as Trust in technical architecture, Truthfulness in marketing, Justice in moderation, Non-Harm in privacy/security, Promise-Keeping in EOL and pricing, Modesty in content defaults, Avoiding Spying in telemetry.

### B.4 `resources/derivation-protocol.md`

Use Appendix A.6.

Must include:

- The 12-step derivation protocol.
- A reusable template block for agents to fill.
- At least three worked examples: location collection, subscription pricing/cancellation, AI-generated religious content.
- Guidance for recording rejected alternatives and tradeoffs.

### B.5 `resources/evidence-and-validation-levels.md`

Use Appendix A.7.

Must include:

- Theological/Islamic validation.
- Design validation.
- Implementation-traceability evidence.
- Stakeholder validation.
- Operational validation.
- Examples of overclaiming and corrected wording.

Correct wording examples:

- Use: “This design is traceable to the Trust and Non-Harm heuristics.”
- Avoid: “This product is Islamically compliant.”
- Use: “Operational validation would require audits, incident logs, and stakeholder feedback.”
- Avoid: “This will prevent harm.”

### B.6 `resources/consultancy-workflow.md`

Must support a professional consultancy flow:

- Intake: product, provider, users, affected communities, business model, lifecycle stage, deployment model, jurisdictions, religious commitments, known harms.
- Scope: what is being reviewed and what is out of scope.
- Stakeholder map: direct, indirect, vulnerable, administrators, maintainers, partners, future users, non-users affected by data/content.
- Provider responsibility map: data, money, content, ranking, identity, moderation, infrastructure, support, promises.
- Principle selection and domain review.
- Evidence gathering: docs, UI, code, policies, logs, audits, interviews, metrics.
- Findings: strengths, concerns, high-risk violations, unknowns, escalation needs.
- Recommendations: quick fixes, policy changes, architecture changes, evidence-building, deferred scholarly review.
- Output modes: short advisory memo, executive report, detailed audit, compliance checklist, design feedback, implementation review.

### B.7 `resources/report-templates.md`

Must include templates for:

- Executive I-VSD review.
- Detailed moral design audit.
- Compliance-style pass/concern/fail checklist.
- Design direction memo.
- Implementation/code review memo.
- Product strategy moral risk report.
- AI/data governance review.
- Business model and monetization review.
- Incident or harm postmortem.

Every template must include:

- Scope.
- Claim boundary.
- Evidence reviewed.
- Stakeholders.
- Principles/domains implicated.
- Findings by severity.
- Recommendations.
- Scholarly/stakeholder/operational validation gaps.

### B.8 `resources/compliance-checks.md`

The resource should be compliance-style but must not claim certification.

Recommended finding levels:

- Pass: evidence supports the heuristic for the reviewed scope.
- Concern: partial evidence, unclear policy, unresolved tradeoff, or implementation gap.
- Fail: design appears to violate a non-negotiable principle or creates serious unresolved harm.
- Not reviewed: outside evidence or scope.
- Requires scholarly review: design-level analysis can continue, but religious-legal conclusion is out of scope.

Core check categories:

- Value source and claim boundaries.
- Data governance.
- Privacy and avoiding spying.
- Security and resilience.
- AI and algorithmic behavior.
- Marketing and public claims.
- Pricing, cancellation, and terms.
- Business model and funding.
- Moderation and appeals.
- Accessibility and localization.
- Portability, self-hosting, and lock-in.
- Operations, support, and EOL.
- Governance, accountability, and partner screening.

### B.9 `resources/data-governance-heuristics.md`

Must include these heuristics:

- Minimize data collection.
- Make data use understandable.
- Separate required and optional data.
- Restrict sharing and preserve portability.
- Review intrusive signals separately.
- Govern retention and access.

Checklist:

- What data is collected and why?
- Is the purpose necessary for user-facing functionality?
- Can the feature work with less data, less precision, shorter retention, or local processing?
- Is sharing explicit, limited, and recipient-reviewed?
- Can users export, delete, correct, or transfer data?
- Are admin accesses role-restricted and logged?
- Are device/location/contacts/wallet/filesystem/browser/biometric/environment signals separately justified?

Evidence expectations:

- Data inventory.
- Purpose map.
- Consent copy.
- Retention schedule.
- Access-control model.
- Export/delete flow.
- Privileged-access logs.
- Incident/audit records for outcome claims.

### B.10 `resources/content-moderation-heuristics.md`

Must include these heuristics:

- Apply standards consistently.
- Rank harm by severity and urgency.
- Protect community values without arbitrary enforcement.
- Evaluate moderation outcomes.

Checklist:

- Are rules clear and findable?
- Are scams, impersonation, credential theft, harassment, exploitation, and security abuse prioritized?
- Are significant moderation decisions documented?
- Are appeals and corrections available?
- Does the provider distinguish user controls from provider enforcement?
- Are monetization/ranking decisions suspended when credible harm review is pending?
- Are vague rules avoided or clarified?

### B.11 `resources/ai-and-algorithmic-heuristics.md`

Must include these heuristics:

- Disclose AI/automation limits.
- Preserve source integrity.
- Review bias and disparate harm.
- Evaluate source openness by risk context.
- Protect user autonomy in recommendations.

Checklist:

- Is AI-generated or AI-assisted content labeled?
- Are hallucination, uncertainty, and failure modes explained?
- Is human escalation available for high-impact decisions?
- Are source, summary, inference, recommendation, and generated output clearly distinguished?
- Are citations required for factual, religious, legal, medical, or decision-critical claims?
- Are feedback loops and biased training data reviewed?
- Are non-personalized or chronological alternatives available where recommendations affect user autonomy?
- Does the design avoid dependency, outrage, spiritual anxiety, or compulsive engagement exploitation?

### B.12 `resources/marketing-and-communication-heuristics.md`

Must include these heuristics:

- Truthful and bounded claims.
- Claim-evidence register.
- Respect user attention.
- Opt-in nonessential marketing.
- Easy unsubscribe.
- No guilt, fear, or spiritual manipulation.
- Communicate material changes before they harm trust.
- Compare honestly.

Comparison requirements:

- Date comparisons.
- State assumptions.
- Acknowledge competitor strengths.
- Avoid checkmark flattening.
- Explain who should not choose the product.
- Avoid negative attacks and manufactured trust signals.

### B.13 `resources/business-model-heuristics.md`

Must include these heuristics:

- Avoid riba and ethically compromising funding where applicable.
- Reduce gharar in pricing and terms.
- Avoid exploitative monetization.
- Preserve commitments during business change.
- Evaluate success beyond revenue.

High-risk business patterns:

- Selling user data.
- Surveillance ads.
- Artificial scarcity.
- Hidden platform fees.
- High switching costs.
- Sponsor/investor influence that undermines commitments.
- Lifetime promise reversals.
- Free tier as pure coercive funnel.
- Revenue depending on confusion, addiction, surveillance, or exploitation.

Ethical revenue patterns:

- Donations.
- Clearly labeled sponsorships.
- Mission-compatible grants.
- Transparent paid hosting/support.
- Fair-value exchange.
- Open-source stewardship with honest self-hosting boundaries.

### B.14 `resources/architecture-heuristics.md`

Must include these heuristics:

- Avoid unjustified lock-in.
- Prioritize data sovereignty.
- Treat security as moral obligation.
- Design for continuity and exit.

Checklist:

- Are APIs, export formats, and docs open enough for migration?
- Is self-hosting credible and documented where promised?
- Are user-owned content and analytics/logs separated?
- Are hosting, retention, backups, and processing locations disclosed?
- Are tenant boundaries explicit?
- Are password hashing, key management, encryption, patching, and vulnerability reporting present?
- Are backups tested?
- Is shutdown/EOL documented?
- Are opaque provider dependencies avoided or disclosed?

### B.15 `resources/ux-and-defaults-heuristics.md`

Must include these heuristics:

- Eliminate dark patterns.
- Transparent pricing and limits.
- Respect cultural/contextual fit.
- Honest comparative interfaces.
- User interest over user satisfaction.

Checklist:

- Is cancellation as clear as subscription?
- Are costs, fees, renewals, dependencies, and limitations visible before commitment?
- Are paid/privacy-invasive options never preselected?
- Are false urgency, false scarcity, guilt, fear, or spiritual pressure absent?
- Are defaults protective rather than maximally permissive/profitable?
- Are sensitive data and public visibility restrictive by default?
- Are accessibility, localization, RTL, and cultural needs treated as justice/excellence requirements?

### B.16 `resources/strategic-decision-framework.md`

Must cover:

- Mission and value proposition.
- Curation boundaries.
- Funding and investor/sponsor screening.
- Organizational structure.
- Open-source posture.
- Partnerships.
- Long-term stewardship.
- Market positioning.
- Autonomy from unethical pressure.
- Command-oriented success frame.

Review questions:

- Does the strategy depend on morally prohibited or high-risk practices?
- What commitments will become hard to keep under growth pressure?
- Who can pressure the provider to compromise?
- Does the provider have a shutdown, migration, or stewardship plan?

### B.17 `resources/technical-decision-framework.md`

Must cover technical selection criteria from the thesis and Appendix E:

- Open source.
- Self-hostable.
- Data portability.
- Client instance freedom.
- No vendor lock-in.
- Privacy-respecting architecture.
- Inspectable authentication, authorization, tenancy, and administrative powers.
- Community governance.
- Standards orientation.
- Federation readiness without premature claims.
- Accessibility-oriented architecture.
- Sustainable documentation, testing, and operations.

Include claim-boundary examples:

- “This stack supports traceability” is allowed.
- “This stack proves reliability/accessibility/security/user trust” is not allowed without audits and operational evidence.

### B.18 `resources/design-decision-framework.md`

Must guide product and UX decisions:

- State user job and provider responsibility.
- Identify likely harms and exploitability.
- Define protective defaults.
- Check pricing/terms visibility.
- Check consent clarity.
- Check cultural and religious context.
- Check accessibility/localization.
- Check comparison honesty.
- Record rejected manipulative alternatives.

### B.19 `resources/operational-framework.md`

Must cover:

- Support response expectations.
- Critical issue handling, including the thesis appendix target of critical issues under 72 hours where adopted.
- Security audits and vulnerability reporting.
- Incident response.
- Backup and recovery.
- EOL notices and migration tools.
- Respectful support communication.
- Lifetime/grandfathered commitment handling.
- Open-sourcing consideration if abandonment would harm users.

### B.20 `resources/governance-and-accountability-framework.md`

Must cover:

- Policy ownership.
- Decision rights.
- Admin power limits.
- Appeals.
- Auditability.
- Contribution governance.
- Partner screening.
- Sponsor/investor influence boundaries.
- Escalation to scholars, stakeholders, security experts, accessibility experts, or legal counsel.

### B.21 `resources/evaluation-metrics.md`

Must include metrics beyond revenue/growth:

- Principle-review completion.
- Unresolved ethical risks.
- User complaints.
- Appeal volume and outcomes.
- Support burden.
- Incidents.
- Accessibility findings.
- Portability/export success.
- Privacy/security audit results.
- Moderation consistency.
- User trust indicators.
- Sustainability and maintenance signals.
- Harm reports.

Warn that Barakah is theological and not a measurable KPI.

### B.22 `resources/legal-and-compliance-framework.md`

Must include:

- GDPR/equivalent privacy principles as complementary standards when aligned.
- Access/deletion rights.
- Plain-language and reasonable terms.
- Clear change communication.
- No hidden terms.
- Clear data-use disclosure.
- Legal compliance is not moral sufficiency.
- When legal counsel is needed versus when Islamic scholarly review is needed.

### B.23 `resources/industry-anti-patterns.md`

Use Appendix A.13.

Must include:

- Anti-pattern category.
- What it looks like.
- Which principles it threatens.
- Diagnostic questions.
- Evidence to request.
- Safer alternative.
- Severity guidance.

### B.24 `resources/scholarly-consultation-boundaries.md`

Use Appendix A.14.

Must include:

- What the skill can do: design reasoning, risk mapping, heuristic application, evidence gap detection.
- What the skill cannot do: fatwa, halal/haram verdict, Sharia certification, religious legal ruling.
- Consultation triggers.
- Recommended output wording when escalation is needed.

### B.25 `resources/islamu-event-case-patterns.md`

Must include the case as illustrative patterns only.

Strategic patterns:

- Islamic curation boundary.
- Anti-scam and instance-safety posture.
- Ethical ticketing and payments.
- Privacy-oriented strategy.
- Federation and portability direction.
- Client/instance choice.
- Prayer-relative scheduling.
- Two-tier verification.

Technical patterns:

- .NET, ASP.NET Core, Blazor/MudBlazor, PostgreSQL/EF Core, Keycloak, Cerbos/local authorization, Serilog/OpenTelemetry, Docker Compose/Aspire, TUnit/bUnit/integration/architecture tests.
- Keycloak/BFF token boundary.
- Runtime-switchable Cerbos/local authorization.
- HAL/HATEOAS authorization-aware affordances.
- EF Core tenant filters.
- PII-split modeling.
- Contact-sharing consent.
- API-key hashing.
- Trusted tenant resolution.
- Idempotency.
- Rate limiting.
- Upload-safety boundaries.

UX patterns:

- Reject dark patterns.
- Neutral free-plan labels.
- Comparable subscription/cancellation clarity.
- Evidence-based availability messaging.
- No preselected paid or privacy-invasive options.
- Fee disclosure before commitment.
- Ticketing breakdown: organizer price, processing fee, platform fee/tax, refund recipient.
- Optional/proportionate post-event engagement.
- Accessibility/localization as requirements, not polish.

Business model patterns:

- Donations, clearly labeled sponsorships, mission-compatible grants/support.
- High-risk rejection: selling user data, surveillance ads, high hidden fees, artificial scarcity, financing/sponsor dependency, hidden influence.
- GNU AGPLv3 community self-hosting and possible third-party managed hosting/SaaS with governance boundaries.
- Upstream nonprofit steward separated from commercial hosting providers as a hypothesis to reduce enshittification risk, not a guarantee.

Required warning:

- The case shows traceability between principles, heuristics, and design decisions. It does not show measured stakeholder experience, production reliability, scholar approval, market viability, or long-term community impact.

## Appendix C - `SKILL.md` Authoring Blueprint

The future `SKILL.md` should be a compact workflow router. It should not attempt to include all Appendix A and B content inline.

Required frontmatter:

```yaml
---
name: i-vsd
description: Apply the Islamic Value Sensitive Design provider-responsibility framework for consultancy, reports, compliance checks, and design or implementation feedback.
type: workflow
enforcement: suggest
priority: high
---
```

Required section content:

Purpose:

- State that the skill applies I-VSD as a provider-responsibility design-reasoning framework grounded in selected Sunni Islamic ethical principles.
- State that it supports consultancy, reports, compliance checks, direction/design feedback, implementation review, strategy, and due diligence.
- State claim boundary: not fatwa, Sharia certification, product certification, or empirical proof.

When to Load:

- Islamic Value Sensitive Design.
- Islamic software ethics.
- Moral review of software/product/business model.
- Consultancy/report/compliance request using I-VSD.
- Design direction or UX/default review with Islamic ethical framing.
- Data/AI/privacy/moderation/marketing/business-model review.
- ISLAMU Event moral design feedback.

When NOT to Load:

- Generic Islamic legal questions without software design context.
- Requests for a fatwa or definitive halal/haram ruling.
- Sharia certification.
- Personal religious advice.
- Generic product management unrelated to provider responsibility.
- Generic code review unless user asks for I-VSD moral review.

Must-Read Docs:

- Link to `resources/framework-overview.md`.
- Link to `resources/principles-and-domains.md`.
- Link to `resources/derivation-protocol.md`.
- Link to `resources/evidence-and-validation-levels.md`.
- Link to `resources/consultancy-workflow.md`.
- Link to `resources/report-templates.md`.
- Link to `resources/compliance-checks.md`.
- Link to domain resources as needed.

Top 5 Invariants:

- Islamic sources set normative boundaries; compatible secular sources only operationalize.
- Provider responsibility spans strategy, design, technical, operations, governance, and evaluation.
- Every recommendation must identify principle, domain, stakeholder, evidence level, and claim boundary.
- Never claim fatwa/certification/empirical proof.
- Escalate contested religious-legal matters to qualified scholars.

Top 5 Anti-Patterns:

- Treating user preference, growth, revenue, or stakeholder consensus as overriding Islamic prohibitions.
- Producing generic ethics advice without principle/domain/evidence traceability.
- Certifying a product as Islamic or Sharia-compliant.
- Ignoring business-model, marketing, governance, or operations because the prompt only mentions UI/code.
- Recommending maximal technical privacy/security without analyzing tradeoffs, recovery, threat model, and uncertainty.

Minimal Examples:

- Short consultancy response example.
- Compliance check example.
- Design feedback example.
- Escalation example for finance/religious-content question.

Verification Hooks:

- Resource links exist.
- Output includes claim boundary.
- Output includes evidence level.
- Output includes scholarly escalation where needed.
- For repository implementation: run agent-context schema tests.

Related Skills:

- `agentic-research` for source selection and evidence discipline.
- `senior-cto-feedback` for strategic workstream review.
- `clean-architecture-rules` when reviewing Event architecture.
- `auth-patterns` when reviewing auth/security boundaries.
- `blazor-ui-conventions`, `accessibility`, and `design-system` when reviewing UI in this repository.

## Appendix D - Output Modes The Skill Must Support

Short advisory response:

- Use when user asks a quick design question.
- Include direct recommendation, principle/domain basis, one or two risks, and evidence caveat.

Structured design feedback:

- Use for UX/product/design direction.
- Include user interest, defaults, pricing/limits clarity, dark-pattern check, accessibility/localization, and tradeoffs.

Compliance-style check:

- Use when user asks “does this comply?” or “audit/check this”.
- Use pass/concern/fail/not reviewed/requires scholarly review.
- Include evidence reviewed and evidence missing.

Consultancy report:

- Use for broad product/service review.
- Include executive summary, scope, stakeholders, provider responsibilities, principle/domain analysis, findings, risks, recommendations, implementation roadmap, validation gaps.

Implementation review:

- Use when reviewing code, architecture, policies, or technical plans.
- Include traceability from principle to implementation, not just code style.
- If reviewing ISLAMU Event, respect repository invariants such as Clean Architecture, HAL affordance gating, tenant isolation, BFF token boundaries, and tests.

Strategy/business model review:

- Use for funding, pricing, partnerships, open-source, self-hosting, sponsorship, or market positioning.
- Include riba/gharar/deception/lock-in/enshittification analysis and scholarly/legal escalation where appropriate.

AI/data review:

- Use for LLMs, recommendations, ranking, automation, analytics, or data governance.
- Include source integrity, labeling, hallucination/uncertainty, bias/disparate harm, intrusive signals, human escalation, and autonomy-preserving alternatives.

Incident/postmortem review:

- Use after harm or failure.
- Include what provider responsibility was breached, affected stakeholders, evidence, restitution/correction, prevention, operational validation, and trust repair.

## Appendix E - Implementation Sequence With File-Level Detail

The implementation agent should write files in this order:

1. Create `.claude/skills/i-vsd/SKILL.md` from Appendix C.
2. Create `resources/framework-overview.md`, `resources/glossary.md`, `resources/principles-and-domains.md`, `resources/derivation-protocol.md`, and `resources/evidence-and-validation-levels.md` from Appendix A.
3. Create `resources/scholarly-consultation-boundaries.md` before templates so all templates can reference escalation boundaries.
4. Create `resources/consultancy-workflow.md`, `resources/report-templates.md`, and `resources/compliance-checks.md` from Appendix B and D.
5. Create domain resources from Appendix B.9 through B.22.
6. Create `resources/industry-anti-patterns.md` from Appendix A.13 and B.23.
7. Create `resources/islamu-event-case-patterns.md` from Appendix B.25.
8. Re-open `SKILL.md` and ensure every resource is reachable from `Must-Read Docs`, examples, or a resource index.
9. Run schema tests and manual link/boundary checks.
10. Update `dev/active/i-vsd-skill/i-vsd-skill-context.md` and `i-vsd-skill-tasks.md` with implementation and verification results.

Do not start by writing every resource as a thesis summary. Start by writing operational resources that an agent can actually use: checklists, templates, review questions, evidence rubrics, anti-pattern signals, and output rules.
