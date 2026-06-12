<!-- ABOUTME: Auditable protocol for deriving I-VSD heuristics from principles, stakeholders, harms, and evidence. -->
<!-- ABOUTME: Includes reusable templates and worked examples for location, cancellation, and religious AI content. -->

# Derivation Protocol

Use this protocol whenever turning a moral concern into a software recommendation.

## Twelve Steps

1. Name the decision or feature.
2. Identify direct, indirect, vulnerable, provider-side, partner, future, and non-user stakeholders.
3. Select relevant I-VSD principles.
4. State the Sunni ethical context at the level you can support.
5. Name the provider responsibility.
6. Identify harms, tensions, exploitability, and foreseeable misuse.
7. Form the design question.
8. Derive a provider-facing heuristic.
9. Define evidence needed to evaluate it.
10. Mark certainty, contestability, and escalation needs.
11. Record tradeoffs and rejected alternatives.
12. Classify the claim: design reasoning, implementation traceability, stakeholder validation, operational validation, or theological/scholarly validation.

## Fill-In Template

```text
Decision:
Stakeholders:
Principles:
Provider responsibility:
Harms/tensions:
Design question:
Heuristic:
Evidence needed:
Certainty and escalation:
Tradeoffs/rejected alternatives:
Claim type:
```

## Worked Examples

Location collection:

- Stakeholders: end users, account owners, community members, admins, abusers, nearby non-users.
- Principles: Trust, Non-Harm, Rights of People, Avoiding Spying, Justice.
- Design question: can the feature work with approximate or user-controlled location?
- Heuristic: collect the least precise location by default, retain it briefly, and restrict admin access.
- Evidence: data inventory, purpose map, UI copy, retention schedule, access logs, abuse review.
- Claim type: design reasoning plus implementation traceability if code/docs confirm it.

Subscription pricing and cancellation:

- Principles: Truthfulness, Gharar reduction, Promise-Keeping, Justice, Avoiding Deception.
- Design question: are costs, renewals, limits, refunds, and cancellation visible before commitment?
- Heuristic: make cancellation as easy as signup and show all fees/renewal terms before payment.
- Evidence: pricing page, checkout flow, terms, emails, support logs, refund policy.
- Tradeoff: lower short-term retention is acceptable if friction depends on confusion.

AI-generated religious content:

- Principles: Trust, Truthfulness, Non-Harm, Rights of People, Excellence.
- Design question: could users mistake generated output for qualified Islamic guidance?
- Heuristic: label AI output, cite sources where appropriate, prevent fatwa-like claims, and route religious-legal questions to scholars.
- Evidence: prompts, system boundaries, UI labels, refusal behavior, escalation flow, test cases.
- Claim type: design reasoning; theological validation requires qualified scholarly review.

## Tradeoff Rule

Do not hide tradeoffs. Record rejected alternatives, especially when they would increase conversion, growth, convenience, or automation at the expense of user rights, clarity, recoverability, or moral duties.
