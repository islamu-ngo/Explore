<!-- ABOUTME: Evidence and authority boundary rules for skill-authored guidance. -->
<!-- ABOUTME: Prevents skills from overstating validation, certification, implementation status, or source authority. -->

# Evidence Boundaries

## Rule

Every skill must be honest about what its guidance proves. A workflow skill can organize reasoning and verification; it does not automatically prove runtime behavior, product quality, legal compliance, religious judgement, security posture, or user outcomes.

## Claim Types

- Repository fact: verified by files, tests, or commands in this repository.
- Source-derived rule: extracted from a plan, research note, thesis, standard, or official documentation.
- Design recommendation: a proposed way to apply the rule to a task.
- Implementation traceability: evidence that code or docs encode a decision.
- External validation: evidence from qualified reviewers, users, audits, regulators, scholars, or production telemetry.

## Required Language

Use boundary language when a skill touches certification, legal, religious, safety, privacy, security, financial, medical, or high-impact decisions. State what the skill can do and what requires escalation.

## Escalation Triggers

Escalate or ask for approval when the skill would:

- Make a definitive external compliance or certification claim.
- Interpret contested expert material without a qualified reviewer.
- Convert a single implementation example into a universal rule.
- Use private source material in a public-facing output.
- Change agent-context tests or contracts in a way that weakens enforcement.

## Evidence In Final Responses

Final summaries should name the files changed, commands run, and validation status. If tests were not run, say exactly why and what remains.
