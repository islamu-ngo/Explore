<!-- ABOUTME: Context-engineering practices for durable skill-authoring workstreams. -->
<!-- ABOUTME: Preserves source evidence, plan state, and handoff accuracy across agents and compaction. -->

# Context Engineering

## Goal

Skill work usually distills a large amount of planning, research, and source reading into a small reusable interface. Context engineering keeps that distillation accurate after the chat scrollback is gone.

## Working Set

For substantial skill work, maintain these surfaces:

- `dev/active/<task>/<task>-plan.md` for strategy, scope, source routing, and validation expectations.
- `dev/active/<task>/<task>-context.md` for current state, files touched, decisions, blockers, and handoff notes.
- `dev/active/<task>/<task>-tasks.md` for phase checklist and verification status.
- `.agents/skills/<skill>/resources/index.md` as the durable reading map.

## Source Distillation

When reading source material, capture durable facts in the active context before turning them into skill text. Separate:

- Verified repository facts.
- Source-derived framework claims.
- Design decisions made during this workstream.
- Assumptions or unresolved questions.
- Validation not yet performed.

## Compression Discipline

Before context compaction, update the active context with exact paths, commands, validation results, and remaining actions. A compressed chat summary should be enough to continue, but the repository files should be the source of truth for future agents.

## Avoiding Context Bloat

Keep `SKILL.md` short. Move long explanations, checklists, templates, and domain frameworks into resources. Use resource indexes so agents can load only what the task needs.

## Pausing Or Redirecting Work

When the user redirects, stop the old implementation path immediately. Mark the old task paused in todos or active docs if needed, then reclassify the new task and load the correct contract sources before editing.
