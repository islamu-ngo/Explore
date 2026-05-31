---
name: i-vsd
description: Apply the Islamic Value Sensitive Design provider-responsibility framework for consultancy, reports, compliance checks, and design or implementation feedback.
type: workflow
enforcement: suggest
priority: high
---
<!-- ABOUTME: Workflow skill for applying Islamic Value Sensitive Design to software products and technical decisions. -->
<!-- ABOUTME: Routes agents to I-VSD resources, action menus, and Markdown report outputs while preserving claim boundaries. -->

## Purpose
Apply Islamic Value Sensitive Design (I-VSD) as a provider-responsibility design-reasoning framework grounded in selected Sunni Islamic ethical principles. Use it for consultancy, reports, compliance-style checks, direction/design feedback, implementation review, strategy, due diligence, and moral risk analysis. If invoked without enough context, return the action menu from [resources/action-routing.md](resources/action-routing.md) and ask which review(s) the user wants. If invoked with one or more actions and enough context, first perform the high-level context inventory in [resources/context-discovery.md](resources/context-discovery.md), tell the user what local docs, MCP/tool integrations, CLIs, skills, and filesystem paths appear available or missing, list the mapped Markdown report file(s) planned under `islamic-value-sensitive-design/`, and ask for agreement or additional context before the deep dive. After confirmation, produce the mapped `i-vsd-*.md` files. It is not a fatwa engine, Sharia certification, product certification, replacement for qualified Islamic legal judgment, or empirical proof of ethical outcomes.

## When to Load
- The user asks for Islamic Value Sensitive Design, Islamic software ethics, or moral review of a software product, platform, API, AI system, or business model.
- The request is for consultancy, a report, compliance-style check, product direction, due diligence, or implementation feedback using I-VSD.
- The user invokes `i-vsd` with no context and needs a menu of possible actions or deliverables.
- The user asks for one or more routed deliverables, such as business model review, architecture review, compliance check, short advisory, consulting report, AI/data review, implementation review, or incident postmortem.
- The work touches provider responsibilities across data, AI, privacy, moderation, marketing, pricing, funding, governance, operations, architecture, or UX defaults.
- The user asks whether a design respects Trust, Truthfulness, Justice, Non-Harm, Rights of People, Riba, Gharar, deception, Promise-Keeping, Excellence, Modesty, or Avoiding Spying.
- The task asks for this project's moral design feedback or maps project decisions to I-VSD traceability.

## When NOT to Load
- Generic Islamic legal questions without software design, product, provider, or implementation context.
- Requests for a fatwa, definitive halal/haram ruling, Sharia certification, or product certification.
- Personal religious advice, worship guidance, or theological debate not tied to provider-mediated software design.
- Generic product management, code review, or UX critique unrelated to provider responsibility or Islamic ethical framing.
- Prompts that ask the agent to prove an ethical outcome without stakeholder, operational, scholarly, or audit evidence.

## Must-Read Docs
- [resources/index.md](resources/index.md)
- [resources/action-routing.md](resources/action-routing.md)
- [resources/context-discovery.md](resources/context-discovery.md)
- [resources/framework-overview.md](resources/framework-overview.md)
- [resources/principles-and-domains.md](resources/principles-and-domains.md)
- [resources/derivation-protocol.md](resources/derivation-protocol.md)
- [resources/evidence-and-validation-levels.md](resources/evidence-and-validation-levels.md)
- [resources/scholarly-consultation-boundaries.md](resources/scholarly-consultation-boundaries.md)
- [resources/consultancy-workflow.md](resources/consultancy-workflow.md)
- [resources/report-templates.md](resources/report-templates.md)
- [resources/compliance-checks.md](resources/compliance-checks.md)

## Top 5 Invariants
1. Selected Sunni Islamic ethical principles set normative boundaries; compatible secular standards, laws, and industry practices only operationalize them when they do not conflict.
2. Provider responsibility must be reviewed across strategic, design, technical, operational, governance, and evaluation domains, even when the prompt names only UI, code, or business strategy.
3. Every routed action must first inventory available context, report missing or useful external context sources to the user, and ask for agreement before producing mapped `islamic-value-sensitive-design/i-vsd-*.md` files with stakeholders, principles, domains, provider responsibilities, evidence level, and claim boundary.
4. Never claim fatwa, Sharia compliance, Islamic certification, product certification, empirical proof, or guaranteed harm prevention.
5. Escalate contested religious-legal matters, finance/riba questions, religious-content guidance, public-harm uncertainty, and high-stakes AI/privacy cases to qualified scholars or other relevant experts.

Repository-based reviews must also search project context broadly across code, Markdown, text files, README files, documentation, policies, plans, decisions, issues, generated reports, visible MCP/tool integrations, relevant CLIs, available skills, and user-provided filesystem paths before drawing moral conclusions.

## Top 5 Anti-Patterns
1. Treating user preference, stakeholder consensus, growth, revenue, convenience, or legal minimums as overriding Islamic prohibitions or moral duties.
2. Giving generic ethics advice without I-VSD traceability to principles, domains, stakeholders, evidence, and rejected alternatives.
3. Certifying a product, contract, business model, or implementation as Islamic, halal, Sharia-compliant, safe, or proven ethical.
4. Ignoring business model, marketing, governance, operations, support, portability, or evaluation because the prompt only mentions UI or code.
5. Recommending maximal privacy, automation, or security without analyzing recovery, support, threat model, tradeoffs, user burden, and uncertainty.

## Minimal Examples
```text
Short advisory response:
Recommendation: make cancellation as clear as signup and show renewal terms before commitment.
Basis: Truthfulness, Gharar reduction, Promise-Keeping; Design and Operational domains.
Evidence needed: pricing copy, cancellation flow, renewal notices, support logs.
Boundary: design reasoning only, not certification.
```

```text
No-context invocation:
Return the action menu from action-routing.md. Ask the user to choose one or more actions, such as compliance check, business model review, architecture review, short advisory, consultancy report, implementation review, AI/data review, or incident postmortem.
```

```text
Multi-action invocation:
If the user asks for business model and architecture review, first inventory available repository docs plus relevant MCP/CLI/skill context sources, then tell the user you plan to create islamic-value-sensitive-design/i-vsd-business-model-review.md, islamic-value-sensitive-design/i-vsd-architecture-review.md, and islamic-value-sensitive-design/i-vsd-review-index.md. Ask for agreement or additional context. After confirmation, write the files and include a short response listing produced files and missing evidence.
```

```text
Compliance-style finding:
Concern - telemetry purpose is unclear. This threatens Trust, Rights of People, and Avoiding Spying.
Evidence reviewed: privacy copy and settings UI. Missing: data inventory, retention schedule, privileged access logs.
Next step: minimize collection or justify each intrusive signal separately.
```

```text
Escalation wording:
I can map the design risks around late fees and financing, but I cannot issue a halal/haram ruling. Because riba may be implicated, this requires qualified Islamic scholarly review before a religious-legal conclusion.
```

## Verification Hooks
- Check that formal outputs include scope, claim boundary, evidence reviewed, stakeholders, principles/domains, findings, recommendations, and validation gaps.
- Use [resources/action-routing.md](resources/action-routing.md) to decide whether to show the menu, ask for missing inputs, or create one or more report files.
- Use [resources/context-discovery.md](resources/context-discovery.md) before repository-based reviews so documentation, text artifacts, MCP/tool integrations, relevant CLIs, available skills, user-provided paths, and project commitments inform the moral analysis.
- Use [resources/evidence-and-validation-levels.md](resources/evidence-and-validation-levels.md) to prevent certification or proof language.
- Use [resources/scholarly-consultation-boundaries.md](resources/scholarly-consultation-boundaries.md) whenever finance, religious guidance, public harm, contested moderation, or high-stakes AI/privacy is involved.
- For repository skill changes, run the host project's skill schema, resource-link, and documentation quality checks.
- Run `git diff --check -- .claude/skills/i-vsd` before handoff when editing this skill inside a repository.

## Related Skills
- [../agentic-research/SKILL.md](../agentic-research/SKILL.md)
- [../senior-cto-feedback/SKILL.md](../senior-cto-feedback/SKILL.md)
- [../clean-architecture-rules/SKILL.md](../clean-architecture-rules/SKILL.md)
- [../auth-patterns/SKILL.md](../auth-patterns/SKILL.md)
- [../blazor-ui-conventions/SKILL.md](../blazor-ui-conventions/SKILL.md)
