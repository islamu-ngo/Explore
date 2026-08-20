---
name: explore-codebase
description: "Load when asked where code lives, how modules relate, what calls a symbol, what implements a feature, or for an architecture/codebase tour using the knowledge graph; not for bug diagnosis, diff review, or refactoring."
type: workflow
enforcement: suggest
priority: medium
---
<!-- ABOUTME: Knowledge-graph workflow for locating code and understanding repository structure. -->
<!-- ABOUTME: Moves from architecture overview to focused symbols, relationships, and flows. -->

## Explore Codebase

Use the code-review-graph MCP tools to explore and understand the codebase.

### Steps

1. Run `list_graph_stats` to see overall codebase metrics.
2. Run `get_architecture_overview_tool` for high-level community structure.
3. Use `list_communities_tool` to find major modules, then `get_community` for details.
4. Use `semantic_search_nodes_tool` to find specific functions or classes.
5. Use `query_graph_tool` with patterns like `callers_of`, `callees_of`, `imports_of` to trace relationships.
6. Use `list_flows` and `get_flow` to understand execution paths.

### Tips

- Start broad (stats, architecture) then narrow down to specific areas.
- Use `children_of` on a file to see all its functions and classes.
- Use `find_large_functions` to identify complex code.

## Token Efficiency Rules
- ALWAYS start with `get_minimal_context(task="<your task>")` before any other graph tool.
- Use `detail_level="minimal"` on all calls. Only escalate to "standard" when minimal is insufficient.
- Target: complete any review/debug/refactor task in ≤5 tool calls and ≤800 total output tokens.
