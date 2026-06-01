<!-- ABOUTME: Reusable I-VSD output templates for audits, memos, reviews, strategy, governance, and postmortems. -->
<!-- ABOUTME: Ensures formal outputs include scope, evidence, claim boundaries, findings, recommendations, and validation gaps. -->

# Report Templates

Every formal template must include a title, `Last Updated: xxxx-xx-xx`, scope, claim boundary, findings by severity, recommendations, stakeholders, principles/domains, validation gaps, escalation, evidence reviewed, missing evidence, and context inventory. Use [action-routing.md](action-routing.md) to decide which Markdown report file(s) to create for the requested action(s). For artifact-based reviews, use [context-discovery.md](context-discovery.md) before writing findings so project documentation, text artifacts, policies, plans, configs, tests, code, relevant project-context integrations, and user-provided paths inform the moral analysis without cluttering the user-facing intake. Generated files go under `islamic-value-sensitive-design/` relative to the current workspace or user-approved destination and must use `i-vsd-*.md` filenames.

## Required Headings For Generated Reports

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

Place `Last Updated: xxxx-xx-xx` directly under the report title and above `## Scope`, using the current date when creating or updating a report. Place the main findings table immediately after `## Claim Boundary` so readers see the useful assessment before source inventory. Keep `Evidence Reviewed`, `Missing Evidence`, and `Context Inventory` near the bottom unless the user explicitly asks for an evidence-first appendix.

Before writing, check whether the mapped report file already exists. If it exists, update that report in place: preserve still-valid prior findings, evidence, missing-evidence notes, and action history; revise or supersede stale content only when the new evidence supports it. If it does not exist, create a new report from the mapped template.

Every generated report must state that it is I-VSD design reasoning and traceability, not a fatwa, Sharia certification, product certification, or empirical proof of ethical outcomes.

When a report reviews a concrete feature request, add `## Common Overlooked Failures And Outcomes` after `## Recommendations` or as an action-specific subsection under `## Findings`. Use [feature-risk-patterns.md](feature-risk-patterns.md) to name feature-specific mistakes, possible bad outcomes, provider questions, and positive outcomes from responsible implementation.

## Executive I-VSD Review

```text
Scope:
Claim boundary: I-VSD design reasoning, not fatwa, certification, or proof.
Top risks:
Strengths:
Priority recommendations:
Stakeholders:
Validation gaps:
Evidence reviewed:
```

## Detailed Moral Design Audit

```text
Scope and exclusions:
Method: stakeholder map, provider responsibility map, principle/domain review, evidence classification.
Findings: severity, principle, domain, evidence, missing evidence, recommendation.
Roadmap: quick fixes, structural changes, evidence-building, escalation.
Validation gaps:
```

## Compliance-Style Checklist

Use categories and finding levels from [compliance-checks.md](compliance-checks.md). Include “not reviewed” instead of guessing.

## Design Direction Memo

```text
User job:
Provider responsibility:
Common overlooked failures and outcomes:
Protective defaults:
Pricing/limits/consent clarity:
Dark-pattern check:
Accessibility/localization:
Rejected manipulative alternatives:
Evidence needed:
```

## Implementation Or Code Review Memo

```text
Reviewed artifacts:
Principle-to-implementation traceability:
Architecture/security/data/account/workspace/tenant boundaries:
Common overlooked failures and outcomes:
Tests or docs supporting the claim:
Operational gaps:
Recommendations:
```

## Moral Diff Review

Use [moral-diff-review-workflow.md](moral-diff-review-workflow.md) before PRs, pushes, or CI/CD handoff when the user asks to evaluate all changes. The review must include unpushed commits, commit titles/bodies, target/upstream branch context, staged changes, unstaged changes, and non-code artifacts: docs, configs, tests, policies, generated files, scripts, metadata, and untracked files intended for review.

Include a parseable YAML block in `## Findings` with `pass`, `hard_violations`, `soft_violations`, and `soft_violation_count`. Also include human-readable sections for diff scope, commit context reviewed, hard violations, soft violations, trust/promise changes, data/permission/telemetry changes, documentation/user-facing claim changes, and recommended fixes before PR. If commit context specifically and consistently justifies an otherwise suspicious permission, data, telemetry, or breaking-change diff, record the accepted justification instead of counting it as a violation.

## Product Strategy Moral Risk Report

Cover mission, funding, pricing, sponsorship, open source, self-hosting, lock-in, enshittification, command-oriented success, and autonomy from unethical pressure.

## AI/Data Governance Review

Cover source integrity, labels, hallucination/uncertainty, bias, intrusive signals, human escalation, non-personalized alternatives, retention, and access controls.

## Business Model And Monetization Review

Cover riba/gharar/deception concerns, data monetization, sponsor influence, hidden fees, cancellation, switching costs, stewardship, and scholarly/legal escalation.

## Incident Or Harm Postmortem

```text
Incident:
Affected stakeholders:
Provider responsibility breached:
Principles/domains:
Evidence:
Immediate correction:
Restitution or user repair:
Prevention:
Operational validation needed:
Trust repair communication:
```
