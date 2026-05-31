<!-- ABOUTME: Action router for no-context and explicit-action I-VSD skill invocations. -->
<!-- ABOUTME: Maps user-requested review modes to Markdown report filenames, required inputs, and supporting resources. -->

# Action Routing

Use this resource first after loading `i-vsd`. It decides whether to ask the user what they want, perform a context inventory, request confirmation, produce one Markdown report, or produce multiple Markdown reports.

## Routing Rule

1. If the user invokes `i-vsd` without product, repository, business, architecture, policy, incident, or report context, do not guess. Return the action menu in `Available Actions` and ask the user to choose one or more actions.
2. If the user gives context but no action, infer the most relevant actions and ask for confirmation before writing files unless the requested outcome is obvious.
3. If the user names one action and gives enough context, run the context inventory gate from [context-discovery.md](context-discovery.md), send the first response contract, and ask the user to agree or provide more context before writing the mapped Markdown report file.
4. If the user names multiple actions, run the context inventory gate from [context-discovery.md](context-discovery.md), send the first response contract with all planned report filenames, and ask the user to agree or provide more context before writing one Markdown report file per action and an index.
5. If required evidence is missing, still produce the report when useful, but mark gaps as `Missing Evidence` or `Not Reviewed`; do not invent facts.
6. If the action involves finance/riba, religious guidance, high-stakes AI, public harm, vulnerable users, contested moderation, or halal/haram language, include a scholarly or expert escalation section.
7. For repository-based actions, run the context discovery protocol in [context-discovery.md](context-discovery.md) before writing findings so docs, text artifacts, policies, plans, MCP/tool integrations, relevant CLIs, available skills, user-provided paths, and implementation evidence are reviewed together.

## Default Output Location

When producing reports, always write them under the repository-root output folder:

```text
islamic-value-sensitive-design/
```

Create the directory if needed. Every generated report file must use the `i-vsd-*.md` naming pattern. Use the exact filenames in `Action Map` so future agents can find the outputs. For multiple actions, also create `i-vsd-review-index.md` listing the requested actions, produced files, evidence gaps, and recommended next action.

If the user specifies another output path, ask for confirmation before using it. The default remains `islamic-value-sensitive-design/`, and filenames must still use the `i-vsd-*.md` prefix pattern.

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
8. technical-review - implementation traceability, tenant/auth/data boundaries
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
20. project-case-review - project-specific traceability review

You can choose one action or multiple actions, for example:
"Run compliance-check and business-model-review for this SaaS."
```

## Action Map

| Action | Markdown file | Primary template/resource | Required minimum context |
|---|---|---|---|
| `short-advisory` | `i-vsd-short-advisory.md` | `report-templates.md`, `derivation-protocol.md` | Product/design question and decision under review |
| `compliance-check` | `i-vsd-compliance-check.md` | `compliance-checks.md` | Product/service scope and evidence to check |
| `consultancy-report` | `i-vsd-consultancy-report.md` | `consultancy-workflow.md`, `report-templates.md` | Client/product scope, stakeholders, goals, evidence |
| `executive-review` | `i-vsd-executive-review.md` | `report-templates.md` | Product/business summary and intended audience |
| `detailed-audit` | `i-vsd-detailed-audit.md` | `report-templates.md`, all domain resources | Product scope plus enough evidence across multiple domains |
| `business-model-review` | `i-vsd-business-model-review.md` | `business-model-heuristics.md` | Pricing, funding, monetization, terms, sponsor/investor context |
| `architecture-review` | `i-vsd-architecture-review.md` | `architecture-heuristics.md`, `technical-decision-framework.md` | Architecture, hosting, data, portability, security, dependencies |
| `technical-review` | `i-vsd-technical-review.md` | `technical-decision-framework.md` | Code, repository, design docs, API boundaries, or implementation diff |
| `design-ux-review` | `i-vsd-design-ux-review.md` | `ux-and-defaults-heuristics.md`, `design-decision-framework.md` | User flows, screens, defaults, pricing/consent/cancellation paths |
| `ai-data-review` | `i-vsd-ai-data-review.md` | `ai-and-algorithmic-heuristics.md`, `data-governance-heuristics.md` | AI/data flow, sources, model behavior, retention, access, escalation |
| `marketing-review` | `i-vsd-marketing-review.md` | `marketing-and-communication-heuristics.md` | Claims, landing page, comparisons, messaging, emails, ads |
| `moderation-review` | `i-vsd-moderation-review.md` | `content-moderation-heuristics.md` | Content policy, enforcement flow, appeals, abuse reports |
| `governance-review` | `i-vsd-governance-review.md` | `governance-and-accountability-framework.md` | Decision rights, policies, admin powers, partner/sponsor influence |
| `operations-review` | `i-vsd-operations-review.md` | `operational-framework.md`, `evaluation-metrics.md` | Support, incident, backup, migration, EOL, and continuity practices |
| `legal-compliance-review` | `i-vsd-legal-compliance-review.md` | `legal-and-compliance-framework.md` | Relevant legal/privacy terms, notices, user rights, compliance evidence |
| `anti-pattern-scan` | `i-vsd-anti-pattern-scan.md` | `industry-anti-patterns.md` | Product claims, flows, business model, data practices, trust signals |
| `implementation-code-review` | `i-vsd-implementation-code-review.md` | `report-templates.md`, domain resources matching changed code | Files, diff, architecture docs, tests, or repository context |
| `incident-postmortem` | `i-vsd-incident-postmortem.md` | `report-templates.md`, `operational-framework.md` | Incident, affected users, timeline, provider actions, evidence |
| `strategy-review` | `i-vsd-strategy-review.md` | `strategic-decision-framework.md` | Mission, market, partnerships, funding, product direction |
| `project-case-review` | `i-vsd-project-case-review.md` | `project-case-patterns.md` | Current project feature, architecture, UX, business, or governance area |

## Synonym Matching

Map common user phrasing to actions:

| User wording | Action |
|---|---|
| `compliance`, `check compliance`, `audit checklist` | `compliance-check` |
| `business`, `monetization`, `pricing`, `funding`, `riba`, `gharar` | `business-model-review` |
| `architecture`, `infra`, `hosting`, `self-hosting`, `lock-in`, `security architecture` | `architecture-review` |
| `code`, `implementation`, `diff`, `PR`, `repository` | `implementation-code-review` or `technical-review` |
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
| `this project`, `this repo`, `current product`, `project-specific traceability` | `project-case-review` |

## Report File Contract

Every generated report file must include these headings:

```text
# <Report Title>

## Scope
## Claim Boundary
## Evidence Reviewed
## Missing Evidence
## Context Inventory
## Stakeholders
## I-VSD Principles And Domains
## Findings
## Recommendations
## Validation Gaps
## Escalation Needed
```

Add action-specific sections when useful, but never remove the required headings. In `Claim Boundary`, state that the report is I-VSD design reasoning and traceability, not a fatwa, Sharia certification, product certification, or empirical proof of ethical outcomes.

## Multi-Report Index Contract

When producing multiple reports, create `i-vsd-review-index.md` with:

```text
# I-VSD Review Index

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
