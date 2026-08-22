---
name: epistemic-mad-review
description: Load when orchestrating Multi-Agent Debate (MAD), adversarial code reviews, or pull-request deliberation to enforce Response Anonymization, expert persona assignment, and weighted post-hoc voting.
type: workflow
enforcement: suggest
priority: high
---

<!-- ABOUTME: Multi-Agent Debate (MAD) protocol enforcing epistemic validity, Response Anonymization, and weighted consensus. -->
<!-- ABOUTME: Eliminates sycophantic conformity (85.5% modal error adoption) and self-bias in automated code review. -->

# Epistemic Multi-Agent Debate (MAD) Review

## Rules

1. **Absolute Response Anonymization**: Strip all agent names, model identities, role attributions, and conversation metadata from debate transcripts before feeding arguments to reviewer agents.
2. **Asymmetrical Expert Personas**: Assign distinct, specialized reasoning paths (Security Auditor, DB Architect, Performance Engineer, Compliance Officer) rather than homogeneous agent teams.
3. **Directed Acyclic Graph (DAG) Topologies**: Route feedback through structured, unidirectional review stages rather than unconstrained, cyclic peer-to-peer discussions.
4. **Weighted Post-Hoc Voting**: Avoid expensive, fragile forced consensus loops; aggregate independent reasoning paths through mathematically weighted scoring based on domain expertise.
5. **The Invariant-Breaker Verification**: Require reviewer agents to propose concrete, failing exploit test cases rather than passive prose summaries.

## Workflow

1. **Persona Initialization**: Instantiate 2–3 distinct specialist personas based on the intent's `mandatory_reviewers` (e.g. `security-privacy-agent` for trust boundaries, `backend-engineer-agent` for persistence).
2. **Anonymized Argument Generation**: Each persona independently analyzes the diff and generates findings, risks, and proposed exploit tests without viewing peer outputs.
3. **Anonymized Cross-Evaluation**: If critiques diverge, present the competing arguments to each agent with all identity headers masked (e.g. `[Proposal A]`, `[Proposal B]`).
4. **Weighted Decision Aggregation**: Aggregate votes where the primary domain expert holds 60% voting weight and secondary reviewers hold 40%.
5. **Actionable Output Synthesis**: Produce a structured JSON/YAML finding artifact containing validated defects, required patches, and new Invariant-Breaker test code.

## Verification

- Verify that all review output artifacts are formatted as structured JSON/YAML with zero identity attribution.
- Confirm that any identified security or correctness defect includes a reproducible unit or integration test case.
