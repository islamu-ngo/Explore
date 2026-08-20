---
name: debug-issue
description: "Load when diagnosing a bug, exception, regression, wrong result, failing flow, or unknown root cause in repository code using callers/callees, execution flows, recent changes, and impact analysis; not for implementing a known fix without investigation."
type: workflow
enforcement: suggest
priority: high
---
<!-- ABOUTME: Knowledge-graph workflow for tracing repository bugs to their root cause. -->
<!-- ABOUTME: Uses callers, flows, recent changes, impact, and coverage before proposing a fix. -->

## Debug Issue

Use the knowledge graph to systematically trace and debug issues.

### Steps

1. Use `semantic_search_nodes_tool` to find code related to the issue.
2. Use `query_graph_tool` with `callers_of` and `callees_of` to trace call chains.
3. Use `get_flow` to see full execution paths through suspected areas.
4. Run `detect_changes_tool` to check if recent changes caused the issue.
5. Use `get_impact_radius_tool` on suspected files to see what else is affected.

### Tips

- Check both callers and callees to understand the full context.
- Look at affected flows to find the entry point that triggers the bug.
- Recent changes are the most common source of new issues.

## Token Efficiency Rules
- ALWAYS start with `get_minimal_context(task="<your task>")` before any other graph tool.
- Use `detail_level="minimal"` on all calls. Only escalate to "standard" when minimal is insufficient.
- Target: complete any review/debug/refactor task in ≤5 tool calls and ≤800 total output tokens.
