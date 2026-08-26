---
name: review-changes
description: "Load when asked to review a local diff/change set for correctness, regressions, blast radius, affected flows, and missing tests using the knowledge graph; use `review-pr` for intent/checklist/merge-gate evidence."
type: workflow
enforcement: suggest
priority: high
---
<!-- ABOUTME: Two-axis risk-aware diff review using Fowler smells, flow, impact, and coverage evidence. -->
<!-- ABOUTME: Evaluates Standards and Spec Fidelity independently so clean code does not mask missed requirements. -->

# Review Changes: Two-Axis Code Review

Review local diffs across two independent, non-polluting axes:
1. **Axis 1 (Standards & Smells)**: Does the code adhere to Clean Architecture, repo rules, and the Fowler Smells Baseline?
2. **Axis 2 (Spec & Intent Fidelity)**: Does the diff faithfully satisfy the requested behavior without scope creep or missed edge cases?

## The Fowler 12-Smell Baseline

Evaluate the diff against these 12 classic code smells:

| Code Smell | What It Is | How to Correct |
|---|---|---|
| **Mysterious Name** | Types, methods, or variables whose names do not reveal intent. | Rename to match domain glossary. |
| **Duplicated Code** | Same logic shape recurring across multiple files/handlers. | Extract to shared domain/application method. |
| **Feature Envy** | A method accessing another object's data more than its own. | Move method onto the owning data/aggregate. |
| **Primitive Obsession** | Raw `string`/`int` standing in for domain concepts (e.g. email, status). | Encapsulate in strongly typed value object/enum. |
| **Data Clumps** | Same 3+ parameters traveling together across methods. | Bundle into a record or parameter object. |
| **Shotgun Surgery** | One logical change forcing scattered edits across unrelated files. | Consolidate cohesion in owning module. |
| **Divergent Change** | One class modified for multiple unrelated reasons. | Split class by single responsibility. |
| **Speculative Generality** | Unused abstractions, generic hooks, or parameters not needed yet. | Delete; keep code minimal (YAGNI). |
| **Message Chains** | Long `a.B.C.D` property walks violating Demeter's Law. | Hide navigation behind a method on first object. |
| **Middle Man** | A class/handler that merely delegates without adding value. | Cut middle man; call target directly. |
| **Repeated Switches** | Repeated `switch`/`if` cascades on the same enum/type. | Replace with polymorphism or strategy pattern. |
| **Refused Bequest** | A subclass ignoring or overriding most inherited behavior. | Replace inheritance with composition. |

## Review Workflow

1. Run `detect_changes_tool` to get risk-scored change analysis.
2. Run `get_affected_flows_tool` to find impacted execution paths.
3. For each high-risk symbol, run `query_graph_tool` with `pattern="tests_for"` to check test coverage.
4. Run `get_impact_radius_tool` to understand blast radius.
5. Report findings under two distinct headings: `## Standards & Code Smells` and `## Spec & Intent Fidelity`.

## Token Efficiency Rules
- ALWAYS start with `get_minimal_context(task="<your task>")` before any other graph tool.
- Use `detail_level="minimal"` on all calls. Only escalate to "standard" when minimal is insufficient.
- Target: complete any review/debug/refactor task in ≤5 tool calls and ≤800 total output tokens.
