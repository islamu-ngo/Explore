---
name: i-vsd
description: "Load when evaluating an Islamic software feature, provider policy, AI behavior, moderation, monetization, privacy, defaults, ranking, self-hosting, or deployment model for provider responsibility and Islamic value-sensitive design; not for generic fiqh rulings or ordinary technical implementation."
type: workflow
enforcement: suggest
priority: high
---
<!-- ABOUTME: Workflow for Islamic Value Sensitive Design of provider-mediated software. -->
<!-- ABOUTME: Preserves scope, evidence, scholarly, and durable-report boundaries. -->

# Islamic Value Sensitive Design

## Rules

- Apply [scope boundaries](resources/scope-boundaries.md) first; I-VSD is provider-responsibility design reasoning, not a fatwa, Sharia/product certification, or proof of ethical outcomes.
- Escalate halal/haram/makrooh/wajib, riba, contested religious-content, and other religious-legal conclusions to qualified Sunni scholarly authority.
- Selected Sunni ethical principles are normative; laws and industry standards may operationalize them only where compatible.
- Review responsibility across strategy, business model, UX, architecture, data/AI, operations, governance, support, portability, and evaluation—not only the surface named in the prompt.
- Trace findings to principles, affected stakeholders, provider-controlled decisions, evidence reviewed, missing evidence, rejected alternatives, and concrete mitigations.
- Persist every I-VSD-framed output under `islamic-value-sensitive-design/i-vsd-*.md`; update an existing mapped report instead of duplicating it.
- State uncertainty and evidence limits; never replace absent stakeholder, operational, audit, or scholarly evidence with confidence.

## Routing

- No action/context: return the [action menu](resources/action-routing.md).
- Sufficient named action: run the short [context gate](resources/context-discovery.md), obtain agreement, then inspect the relevant artifacts.
- New/low-context product: use [guided discovery](resources/guided-discovery-workflow.md).
- Pre-PR/push moral review: use [moral diff review](resources/moral-diff-review-workflow.md).
- Formal consultancy/compliance output: use the matching template and evidence level from [resource index](resources/index.md).
- High-risk feature patterns: load [feature risks](resources/feature-risk-patterns.md).
- Religious-authority uncertainty: load [scholarly boundaries](resources/scholarly-consultation-boundaries.md).

Load only the routed resources, never the full library.

## Output contract

Each report includes:

1. claim boundary and action/context;
2. findings and recommendations immediately after it;
3. principle/domain/stakeholder traceability;
4. evidence reviewed, missing evidence, rejected alternatives, and escalation needs;
5. `Last Updated: YYYY-MM-DD`.

The chat response names the written file and summarizes the recommendation and unresolved evidence; it does not duplicate the report.

## Verification

- Confirm the request is provider-mediated software responsibility and not a religious ruling.
- Confirm a Markdown report was created or updated at the mapped path.
- Check that every strong claim has evidence and every authority boundary is explicit.
- Run host-repository link, schema, and whitespace checks when this skill or its resources change.
