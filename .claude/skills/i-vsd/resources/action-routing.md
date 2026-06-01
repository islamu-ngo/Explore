<!-- ABOUTME: Action router for no-context and explicit-action I-VSD skill invocations. -->
<!-- ABOUTME: Maps user-requested review modes to Markdown report filenames, required inputs, and supporting resources. -->

# Action Routing

Use this resource after applying [scope-boundaries.md](scope-boundaries.md). It decides whether to refuse or narrow out-of-scope requests, ask the user what they want, perform a context inventory, run a guided discovery interview, run a pre-PR moral diff review, request confirmation, produce one Markdown report, or produce multiple Markdown reports.

## Routing Rule

1. If the request is totally unrelated to provider-mediated software, refuse using [scope-boundaries.md](scope-boundaries.md) and do not show the action menu or create reports.
2. If the request is software-related but adjacent and not framed around provider-mediated product responsibility, refuse as not optimized for I-VSD; invite reframing around a concrete product, user-facing feature, deployment, governance policy, data practice, operational duty, or stakeholder impact.
3. If the whole request asks for halal/haram/makrooh/wajib/Sharia-compliance judgment, refuse the ruling and refer to qualified scholarly authority.
4. If a covered provider-mediated software request includes religious-legal subquestions, continue only with design reasoning and put the excluded ruling parts under `Escalation Needed`.
5. If the user invokes `i-vsd` without product, artifact, repository, business, architecture, policy, incident, or report context, do not guess. Return the action menu in `Available Actions` and ask the user to choose one or more actions.
6. If the user gives covered context but no action, infer the most relevant actions and ask for confirmation before writing files unless the requested outcome is obvious.
7. If the user names one covered action and gives enough context, run the concise context inventory gate from [context-discovery.md](context-discovery.md), send the first response contract, and ask the user to agree or provide more context before creating or updating the mapped Markdown report file.
8. If the user names multiple covered actions, run the concise context inventory gate from [context-discovery.md](context-discovery.md), send the first response contract with all planned report filenames, and ask the user to agree or provide more context before creating or updating one Markdown report file per action and an index.
9. If the user chooses `guided-discovery`, run the context gate, then interview the user using [guided-discovery-workflow.md](guided-discovery-workflow.md) before writing the mapped report; do not treat the absence of existing context as a blocker.
10. If the user chooses `moral-diff-review`, collect the complete outgoing changeset using [moral-diff-review-workflow.md](moral-diff-review-workflow.md), including unpushed commits, commit titles/bodies, target/upstream branch context, code, docs, configs, tests, generated files, staged changes, unstaged changes, and untracked files intended for review. When the user explicitly asks to run the review, proceed without a separate agreement step unless the diff source, comparison base, or output location is ambiguous.
11. If required evidence is missing for non-discovery actions, still produce the report when useful, but mark gaps as `Missing Evidence` or `Not Reviewed`; do not invent facts.
12. If the action involves finance/riba, religious guidance, high-stakes AI, public harm, vulnerable users, contested moderation, or halal/haram/makrooh/wajib language, include a scholarly or expert escalation section.
13. For artifact-based actions, run the context discovery protocol in [context-discovery.md](context-discovery.md) before writing findings so docs, text artifacts, policies, plans, relevant project-context integrations, user-provided paths, and implementation evidence are reviewed together.

## Default Output Location

When producing reports, write them under this output folder relative to the current workspace, project root, repository root, or user-approved destination:

```text
islamic-value-sensitive-design/
```

Create the directory if needed. Every generated report file must use the `i-vsd-*.md` naming pattern. Use the exact filenames in `Action Map` so future agents can find the outputs. For multiple actions, also create or update `i-vsd-review-index.md` listing the requested actions, produced files, evidence gaps, and recommended next action.

If the user specifies another output path, ask for confirmation before using it. The default remains `islamic-value-sensitive-design/`, and filenames must still use the `i-vsd-*.md` prefix pattern.

## Existing Report Rule

After the context gate and user agreement, check whether each mapped output file already exists before writing. For example, `compliance-check` maps to `islamic-value-sensitive-design/i-vsd-compliance-check.md`; if that file exists, update the existing report instead of replacing it from scratch. Preserve useful prior findings, resolved history, evidence notes, and open gaps unless new evidence supersedes them. If a mapped file does not exist, create it from the relevant template.

When updating an existing report, refresh the `Last Updated: xxxx-xx-xx` line under the title using the current date and keep the report structure aligned with `Report File Contract`.

## Available Actions

Offer this menu for no-context invocations:

```text
Which I-VSD action do you want?

1. short-advisory - quick recommendation with principle/domain basis
2. compliance-check - pass/concern/fail style non-certification review
3. consultancy-report - structured I-VSD consultancy report
4. executive-review - brief leadership summary of moral risks and priorities
5. detailed-audit - full moral design audit across all six domains
6. business-model-review - pricing, funding, monetization, riba/gharar, lock-in
7. architecture-review - architecture, data sovereignty, security, portability
8. technical-review - implementation traceability, account/workspace/tenant, auth, and data boundaries
9. design-ux-review - UX flows, defaults, accessibility, dark patterns
10. ai-data-review - AI, algorithms, source integrity, privacy, retention
11. marketing-review - claims, comparisons, persuasion, opt-in communication
12. moderation-review - content policy, appeals, abuse, fairness, enforcement
13. governance-review - accountability, decision rights, partner/sponsor influence
14. operations-review - support, incidents, backups, EOL, continuity promises
15. legal-compliance-review - legal/privacy compliance as supporting evidence
16. anti-pattern-scan - deception, surveillance, lock-in, enshittification risks
17. implementation-code-review - code or diff review through I-VSD traceability
18. incident-postmortem - harm/incident review, repair, prevention, trust recovery
19. strategy-review - mission, positioning, partnerships, open-source stewardship
20. project-case-review - product/project-specific traceability review
21. guided-discovery - interview-led report for a new project, venture, orientation, or mission
22. moral-diff-review - pre-PR/push review of unpushed commits plus local code, docs, config, and artifact changes

You can choose one action or multiple actions, for example:
"Run compliance-check and business-model-review for this SaaS."
```

## Action Map

| Action | Markdown file | Primary template/resource | Required minimum context |
|---|---|---|---|
| `short-advisory` | `i-vsd-short-advisory.md` | `report-templates.md`, `derivation-protocol.md` | Product/design question and decision under review |
| `compliance-check` | `i-vsd-compliance-check.md` | `compliance-checks.md` | Product/service scope and evidence to check |
| `consultancy-report` | `i-vsd-consultancy-report.md` | `consultancy-workflow.md`, `feature-risk-patterns.md`, `report-templates.md` | Client/product scope, stakeholders, goals, evidence |
| `executive-review` | `i-vsd-executive-review.md` | `report-templates.md` | Product/business summary and intended audience |
| `detailed-audit` | `i-vsd-detailed-audit.md` | `report-templates.md`, all domain resources | Product scope plus enough evidence across multiple domains |
| `business-model-review` | `i-vsd-business-model-review.md` | `business-model-heuristics.md` | Pricing, funding, monetization, terms, sponsor/investor context |
| `architecture-review` | `i-vsd-architecture-review.md` | `architecture-heuristics.md`, `technical-decision-framework.md` | Architecture, hosting, data, portability, security, dependencies |
| `technical-review` | `i-vsd-technical-review.md` | `technical-decision-framework.md` | Code, source package, design docs, API boundaries, or implementation diff |
| `design-ux-review` | `i-vsd-design-ux-review.md` | `ux-and-defaults-heuristics.md`, `design-decision-framework.md`, `feature-risk-patterns.md` | User flows, screens, defaults, pricing/consent/cancellation paths |
| `ai-data-review` | `i-vsd-ai-data-review.md` | `ai-and-algorithmic-heuristics.md`, `data-governance-heuristics.md` | AI/data flow, sources, model behavior, retention, access, escalation |
| `marketing-review` | `i-vsd-marketing-review.md` | `marketing-and-communication-heuristics.md` | Claims, landing page, comparisons, messaging, emails, ads |
| `moderation-review` | `i-vsd-moderation-review.md` | `content-moderation-heuristics.md` | Content policy, enforcement flow, appeals, abuse reports |
| `governance-review` | `i-vsd-governance-review.md` | `governance-and-accountability-framework.md` | Decision rights, policies, admin powers, partner/sponsor influence |
| `operations-review` | `i-vsd-operations-review.md` | `operational-framework.md`, `evaluation-metrics.md` | Support, incident, backup, migration, EOL, and continuity practices |
| `legal-compliance-review` | `i-vsd-legal-compliance-review.md` | `legal-and-compliance-framework.md` | Relevant legal/privacy terms, notices, user rights, compliance evidence |
| `anti-pattern-scan` | `i-vsd-anti-pattern-scan.md` | `industry-anti-patterns.md` | Product claims, flows, business model, data practices, trust signals |
| `implementation-code-review` | `i-vsd-implementation-code-review.md` | `report-templates.md`, `feature-risk-patterns.md`, domain resources matching changed code | Files, diff, architecture docs, tests, source package, or implementation context |
| `incident-postmortem` | `i-vsd-incident-postmortem.md` | `report-templates.md`, `operational-framework.md` | Incident, affected users, timeline, provider actions, evidence |
| `strategy-review` | `i-vsd-strategy-review.md` | `strategic-decision-framework.md` | Mission, market, partnerships, funding, product direction |
| `project-case-review` | `i-vsd-project-case-review.md` | `project-case-patterns.md` | Current product, project, app, service, feature, architecture, UX, business, or governance area |
| `guided-discovery` | `i-vsd-guided-discovery.md` | `guided-discovery-workflow.md`, `strategic-decision-framework.md`, domain resources matching answers | User wants to shape a new project, organization, product, venture, or mission through interview questions |
| `moral-diff-review` | `i-vsd-moral-diff-review.md` | `moral-diff-review-workflow.md`, `compliance-checks.md`, `feature-risk-patterns.md`, domain resources matching changed files | Complete outgoing changeset, unpushed commit context, and local dirty work before push, PR, or CI/CD handoff |

## Synonym Matching

Map common user phrasing to actions:

| User wording | Action |
|---|---|
| `compliance`, `check compliance`, `audit checklist` | `compliance-check` |
| `business`, `monetization`, `pricing`, `funding`, `riba`, `gharar` | `business-model-review` |
| `architecture`, `infra`, `hosting`, `self-hosting`, `lock-in`, `security architecture` | `architecture-review` |
| `code`, `implementation`, `diff`, `PR`, `source package`, `repository` | `implementation-code-review` or `technical-review` |
| `before PR`, `pre-PR`, `before push`, `before pushing`, `unpushed commits`, `review all changes`, `git diff`, `moral diff`, `CI gate`, `pre-CI`, `final gatekeeper` | `moral-diff-review` |
| `UX`, `design`, `defaults`, `dark patterns`, `screens`, `flow` | `design-ux-review` |
| `AI`, `algorithm`, `model`, `recommendation`, `automation`, `data governance` | `ai-data-review` |
| `marketing`, `landing page`, `claims`, `comparison`, `email` | `marketing-review` |
| `moderation`, `content`, `appeals`, `abuse`, `policy enforcement` | `moderation-review` |
| `governance`, `accountability`, `admin powers`, `sponsor`, `partner` | `governance-review` |
| `operations`, `support`, `incident`, `backup`, `EOL`, `SLA` | `operations-review` |
| `legal`, `GDPR`, `terms`, `privacy policy`, `user rights` | `legal-compliance-review` |
| `anti-pattern`, `red flags`, `moral risk scan` | `anti-pattern-scan` |
| `incident`, `harm`, `postmortem`, `trust repair` | `incident-postmortem` |
| `strategy`, `positioning`, `partnership`, `mission`, `open source stewardship` | `strategy-review` |
| `this project`, `this repo`, `this app`, `current product`, `current service`, `product-specific traceability`, `project-specific traceability` | `project-case-review` |
| `new project`, `starting from scratch`, `orientation`, `entrepreneurship`, `ask me questions`, `help me decide`, `I do not know yet`, `mission discovery` | `guided-discovery` |

## Report File Contract

Every generated report file must include these headings:

```text
# <Report Title>

Last Updated: xxxx-xx-xx

## Scope
## Claim Boundary
## Findings
## Recommendations
## Stakeholders
## I-VSD Principles And Domains
## Validation Gaps
## Escalation Needed
## Evidence Reviewed
## Missing Evidence
## Context Inventory
```

Add action-specific sections when useful, but never remove the required headings. Every report must include `Last Updated: xxxx-xx-xx` directly under the title and above `Scope`, using the current date. Put the primary findings table immediately after `Claim Boundary`; keep `Evidence Reviewed`, `Missing Evidence`, and `Context Inventory` near the bottom. In `Claim Boundary`, state that the report is I-VSD design reasoning and traceability, not a fatwa, Sharia certification, product certification, or empirical proof of ethical outcomes.

## Multi-Report Index Contract

When producing multiple reports, create `i-vsd-review-index.md` with:

```text
# I-VSD Review Index

Last Updated: xxxx-xx-xx

## Requested Actions
## Produced Reports
## Shared Scope
## Shared Evidence Reviewed
## Cross-Cutting Risks
## Missing Evidence Across Reports
## Recommended Reading Order
## Next Recommended Action
```

The index is navigational. It must not replace the individual action reports.
