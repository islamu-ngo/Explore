<!-- ABOUTME: Guidance for building deep but context-efficient skill resource libraries. -->
<!-- ABOUTME: Defines resource granularity, indexes, templates, checklists, and anti-bloat rules. -->

# Resource Library Patterns

## Why Resources Exist

`SKILL.md` is loaded as routing context. Resource files provide depth only when needed. This keeps default context small while preserving the full workflow for complex tasks.

## Recommended Resource Types

- `index.md`: reading map and load order.
- `*-workflow.md`: step-by-step execution sequence.
- `*-checklist.md`: concrete pass/fail review questions.
- `*-heuristics.md`: domain decision rules and examples.
- `*-template.md`: output, report, or handoff structure.
- `*-boundaries.md`: claim limits, escalation triggers, and authority constraints.

## Granularity

Create one resource per durable decision tool. Do not split every paragraph into its own file, but do not create a single resource that becomes a second oversized skill.

## Resource Index Requirements

`resources/index.md` should list every resource file with one sentence on when to use it. It should be linked from `SKILL.md` Must-Read Docs.

## Depth Without Bloat

Use short headings, checklists, and compact tables where they add precision. Avoid narrative essays, duplicated source excerpts, and generic best-practice explanations.

## Source Traceability

When a resource encodes domain research, name the source family or active workstream that produced it. If the underlying source is private or local-only, summarize the derived rule without leaking unnecessary raw content.
