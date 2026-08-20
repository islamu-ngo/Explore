---
name: review-changes
description: "Load when asked to review a local diff/change set for correctness, regressions, blast radius, affected flows, and missing tests using the knowledge graph; use `review-pr` for intent/checklist/merge-gate evidence."
type: workflow
enforcement: suggest
priority: high
---
<!-- ABOUTME: Risk-aware local diff review using change, flow, impact, and coverage evidence. -->
<!-- ABOUTME: Separates correctness review from the repository's intent-driven PR gate. -->

## Review Changes

Perform a thorough, risk-aware code review using the knowledge graph.

### Steps

1. Run `detect_changes_tool` to get risk-scored change analysis.
2. Run `get_affected_flows_tool` to find impacted execution paths.
3. For each high-risk function, run `query_graph_tool` with pattern="tests_for" to check test coverage.
4. Run `get_impact_radius_tool` to understand the blast radius.
5. For any untested changes, suggest specific test cases.

### Output Format

Provide findings grouped by risk level (high/medium/low) with:
- What changed and why it matters
- Test coverage status
- Suggested improvements
- Overall merge recommendation

## Token Efficiency Rules
- ALWAYS start with `get_minimal_context(task="<your task>")` before any other graph tool.
- Use `detail_level="minimal"` on all calls. Only escalate to "standard" when minimal is insufficient.
- Target: complete any review/debug/refactor task in ≤5 tool calls and ≤800 total output tokens.
