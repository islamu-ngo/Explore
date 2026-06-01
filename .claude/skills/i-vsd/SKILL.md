---
name: i-vsd
description: Apply the Islamic Value Sensitive Design provider-responsibility framework to provider-mediated software across SaaS, open/closed-source, desktop, mobile, API, self-hosted, and AI systems.
type: workflow
enforcement: suggest
priority: high
---
<!-- ABOUTME: Workflow skill for applying Islamic Value Sensitive Design to software products and technical decisions. -->
<!-- ABOUTME: Routes agents to I-VSD resources, action menus, and Markdown report outputs while preserving claim boundaries. -->

## Purpose
Apply Islamic Value Sensitive Design (I-VSD) as a provider-responsibility design-reasoning framework grounded in selected Sunni Islamic ethical principles. Use it for provider-mediated software solutions, including SaaS, closed-source products, open-source projects, desktop apps, mobile apps, APIs, AI-enabled product features, self-hostable software, marketplaces, community platforms, and internal tools. It supports consultancy, reports, compliance-style checks, guided discovery, direction/design feedback, implementation review, pre-PR moral diff review, strategy, due diligence, and moral risk analysis. Before routing any action, apply [resources/scope-boundaries.md](resources/scope-boundaries.md): totally unrelated requests must be refused as outside I-VSD, adjacent software/AI requests that are not provider-mediated product responsibility must not be presented as I-VSD, and halal/haram/makrooh/wajib/Sharia-compliance questions must be escalated to qualified scholarly authority. If invoked without enough covered context, return the action menu from [resources/action-routing.md](resources/action-routing.md); if invoked with covered actions and enough context, run the concise context gate in [resources/context-discovery.md](resources/context-discovery.md), ask for agreement or added context, then check whether the mapped `islamic-value-sensitive-design/i-vsd-*.md` files already exist and update or create them. For `guided-discovery`, interview the user first using [resources/guided-discovery-workflow.md](resources/guided-discovery-workflow.md) so low-context provider-mediated software projects can become contextual reports. For `moral-diff-review`, inspect unpushed commits, commit titles/bodies, target/upstream branch context, and local dirty work before PR, push, or CI/CD handoff using [resources/moral-diff-review-workflow.md](resources/moral-diff-review-workflow.md). It is not a fatwa engine, Sharia certification, product certification, replacement for qualified Islamic legal judgment, or empirical proof of ethical outcomes.

## When to Load
- The user asks for Islamic Value Sensitive Design, Islamic software ethics, or moral review of a software product, platform, API, AI system, or business model.
- The request is for consultancy, a report, compliance-style check, product direction, due diligence, or implementation feedback using I-VSD.
- The user invokes `i-vsd` with no context and needs a menu of possible actions or deliverables.
- The user asks for one or more routed deliverables, such as guided discovery, business model review, architecture review, compliance check, short advisory, consulting report, AI/data review, implementation review, or incident postmortem.
- The user asks for a pre-PR, pre-push, pre-CI, final gatekeeper, moral compliance, or git-diff review of all local changes.
- The user is starting a new project, venture, organization, orientation, or mission and wants the agent to ask questions before recommending an implementation model.
- The work touches provider responsibilities across data, AI, privacy, moderation, marketing, pricing, funding, governance, operations, architecture, or UX defaults.
- The user asks whether a design respects Trust, Truthfulness, Justice, Non-Harm, Rights of People, Riba, Gharar, deception, Promise-Keeping, Excellence, Modesty, or Avoiding Spying.
- The task asks for moral design feedback or maps provider decisions to I-VSD traceability.

## When NOT to Load
- Requests unrelated to provider-mediated software design, implementation, operations, governance, policy, or product responsibility, such as trading advice, generic book writing, personal finance advice, worship guidance, or unrelated creative/business tasks.
- Software or AI requests that are adjacent but not framed around provider-mediated product responsibility, such as foundational AI lab strategy, model-training research agendas, benchmark design, pure ML research, generic programming help, hardware design, or academic theory.
- Generic Islamic legal questions without software design, product, provider, or implementation context.
- Requests for a fatwa, definitive halal/haram/makrooh/wajib ruling, Sharia certification, or product certification.
- Personal religious advice, worship guidance, or theological debate not tied to provider-mediated software design.
- Generic product management, code review, or UX critique unrelated to provider responsibility or Islamic ethical framing.
- Prompts that ask the agent to prove an ethical outcome without stakeholder, operational, scholarly, or audit evidence.

## Must-Read Docs
- [resources/index.md](resources/index.md)
- [resources/action-routing.md](resources/action-routing.md)
- [resources/context-discovery.md](resources/context-discovery.md)
- [resources/scope-boundaries.md](resources/scope-boundaries.md)
- [resources/framework-overview.md](resources/framework-overview.md)
- [resources/principles-and-domains.md](resources/principles-and-domains.md)
- [resources/derivation-protocol.md](resources/derivation-protocol.md)
- [resources/evidence-and-validation-levels.md](resources/evidence-and-validation-levels.md)
- [resources/scholarly-consultation-boundaries.md](resources/scholarly-consultation-boundaries.md)
- [resources/consultancy-workflow.md](resources/consultancy-workflow.md)
- [resources/feature-risk-patterns.md](resources/feature-risk-patterns.md)
- [resources/guided-discovery-workflow.md](resources/guided-discovery-workflow.md)
- [resources/moral-diff-review-workflow.md](resources/moral-diff-review-workflow.md)
- [resources/report-templates.md](resources/report-templates.md)
- [resources/compliance-checks.md](resources/compliance-checks.md)

## Top 5 Invariants
1. Selected Sunni Islamic ethical principles set normative boundaries; compatible secular standards, laws, and industry practices only operationalize them when they do not conflict.
2. Provider responsibility must be reviewed across strategic, design, technical, operational, governance, and evaluation domains, even when the prompt names only UI, code, or business strategy.
3. Every routed action must first give a concise, relevant context gate that names only actionable workspace, repository, source-package, artifact, or project-context integrations before asking for agreement to update or create mapped `islamic-value-sensitive-design/i-vsd-*.md` files with `Last Updated: xxxx-xx-xx`, stakeholders, principles, domains, provider responsibilities, evidence level, and claim boundary.
4. Never answer unrelated, adjacent-but-out-of-framework, or religious-legal authority requests in the name of I-VSD; refuse or narrow the response using the scope boundary rules.
5. Escalate contested religious-legal matters, finance/riba questions, religious-content guidance, public-harm uncertainty, and high-stakes AI/privacy cases to qualified scholars or other relevant experts.

Artifact-based reviews must search relevant product, policy, implementation, operational, and stakeholder context broadly after confirmation, but the pre-review user-facing context gate stays short and lists only context sources relevant to the requested I-VSD action.

## Top 5 Anti-Patterns
1. Treating user preference, stakeholder consensus, growth, revenue, convenience, or legal minimums as overriding Islamic prohibitions or moral duties.
2. Giving generic ethics advice without I-VSD traceability to principles, domains, stakeholders, evidence, and rejected alternatives.
3. Certifying a product, contract, business model, or implementation as Islamic, halal, haram, makrooh, wajib, Sharia-compliant, safe, or proven ethical.
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
Guided discovery invocation:
If the user is starting from scratch or does not know what context matters, choose guided-discovery. Run the concise context gate, tell the user little existing context is acceptable, ask structured questions about mission, stakeholders, business model, governance, moderation, data, tech, and provider capacity, then update or create islamic-value-sensitive-design/i-vsd-guided-discovery.md with contextual recommendations and rejected alternatives.
```

```text
Multi-action invocation:
If the user asks for business model and architecture review, first say you found workspace/repository docs plus any relevant project-context integrations, then tell the user you plan to update or create islamic-value-sensitive-design/i-vsd-business-model-review.md, islamic-value-sensitive-design/i-vsd-architecture-review.md, and islamic-value-sensitive-design/i-vsd-review-index.md. Ask for agreement or additional context. After confirmation, check for existing files, update them if present, create any missing files, and include a short response listing changed files and missing evidence.
```

```text
Compliance-style finding:
Concern - telemetry purpose is unclear. This threatens Trust, Rights of People, and Avoiding Spying.
Evidence reviewed: privacy copy and settings UI. Missing: data inventory, retention schedule, privileged access logs.
Next step: minimize collection or justify each intrusive signal separately.
```

```text
Out-of-scope invocation:
I can't answer that as an I-VSD request. This skill is only for provider-mediated software design and provider-responsibility review. The request is outside I-VSD scope, so any answer would not be an I-VSD-authorized response.
```

```text
Escalation wording:
I can map the design risks around late fees and financing, but I cannot issue a halal, haram, makrooh, wajib, or Sharia-compliance ruling. Because riba may be implicated, this requires qualified Islamic scholarly review before a religious-legal conclusion.
```

## Verification Hooks
- Check that formal outputs include `Last Updated: xxxx-xx-xx` under the title, place findings immediately after claim boundary, and keep evidence reviewed, missing evidence, and context inventory near the bottom.
- Use [resources/scope-boundaries.md](resources/scope-boundaries.md) before routing so unrelated requests, adjacent-but-not-optimized software requests, and fiqh/fatwa authority requests are refused or narrowed instead of answered under I-VSD attribution.
- Use [resources/action-routing.md](resources/action-routing.md) to decide whether to show the menu, ask for missing inputs, or create one or more report files.
- Use [resources/feature-risk-patterns.md](resources/feature-risk-patterns.md) for concrete feature consultations so reports include common overlooked failures, possible bad outcomes, provider questions, and positive outcomes of responsible implementation.
- Use [resources/guided-discovery-workflow.md](resources/guided-discovery-workflow.md) when the user needs interview-led context formation before a report.
- Use [resources/moral-diff-review-workflow.md](resources/moral-diff-review-workflow.md) for pre-PR/push reviews so unpushed commits, commit context, target/upstream branch, code, docs, config, tests, generated artifacts, and intended untracked files are checked for hard and soft moral violations.
- Use [resources/context-discovery.md](resources/context-discovery.md) before artifact-based reviews so the user-facing gate stays concise while documentation, text artifacts, relevant project-context integrations, user-provided paths, and provider commitments inform the deep moral analysis.
- Use [resources/evidence-and-validation-levels.md](resources/evidence-and-validation-levels.md) to prevent certification or proof language.
- Use [resources/scholarly-consultation-boundaries.md](resources/scholarly-consultation-boundaries.md) whenever finance, religious guidance, public harm, contested moderation, or high-stakes AI/privacy is involved.
- When adapting this folder inside a host repository, run that repository's skill schema, resource-link, documentation, and whitespace checks if available.
- Run a whitespace/diff check before handoff when editing this skill inside a version-controlled workspace.

## Related Skills
- None required. This folder is designed to be portable across host projects and should not depend on project-specific implementation skills.
