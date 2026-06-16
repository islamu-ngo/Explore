<!-- ABOUTME: End-to-end workflow for turning implementation knowledge into a project skill. -->
<!-- ABOUTME: Defines the practical sequence for classification, source reading, drafting, verification, and handoff. -->

# Authoring Workflow

## Purpose

Use this workflow when a task asks for a new `.agents/skills/<name>/SKILL.md` or a significant resource-library update.

## Sequence

1. Classify the task as `create-agent-context-skill` when the work changes `.agents/skills/**`, skill resources, skill schema tests, or skill-related intent routing.
2. Read `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/DOCUMENTATION_STYLE_GUIDE.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, `.agents/skills/_SKILL_SCHEMA.md`, and any active `dev/active/*` plan/context/tasks for the skill.
3. Confirm whether an existing skill folder exists before creating files; do not overwrite unrelated work without reading it.
4. Extract durable knowledge into categories: activation triggers, non-goals, invariants, anti-patterns, examples, verification hooks, and resource topics.
5. Draft `SKILL.md` as the short router, not the full textbook.
6. Create `resources/index.md` plus focused resource files for depth, templates, checklists, decision frameworks, and domain-specific heuristics.
7. Add or update the intent manifest only when the skill changes routing or should become a first-class task category.
8. Run schema, link, intent-manifest, and diff-check verification before claiming completion.
9. Update active dev docs with what changed, what was verified, and what remains if the work came from a `dev/active/*` workstream.

## Quality Bar

A good skill lets another agent execute the workflow from a cold start. It should state what to load, what to avoid, what is non-negotiable, where deeper material lives, and how to verify the result.

## Handoff Rule

If context is getting large, update `dev/active/<task>/<task>-context.md` before compression or handoff. Preserve exact file paths, commands run, validation results, and remaining risks.
