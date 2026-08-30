---
name: skill-authoring
description: "Load when creating, updating, auditing, simplifying, routing, or validating `.agents/skills/*`, `SKILL.md` descriptions/resources, the skill schema, or skill-related agent context tests; not for ordinary application code or generic documentation."
type: workflow
enforcement: suggest
priority: high
---
<!-- ABOUTME: Workflow skill for authoring durable project skills and resource libraries. -->
<!-- ABOUTME: Converts implementation-plan knowledge into schema-compliant, verifiable, context-efficient agent guidance. -->

## Resources
- [../../../AGENTS.md](../../../AGENTS.md)
- [../_SKILL_SCHEMA.md](../_SKILL_SCHEMA.md)
- [resources/index.md](resources/index.md) — load only the resource matching the unresolved authoring decision.

## Rules

1. Write `description` for the pre-load selector: concrete trigger language, artifacts, and exclusions for likely neighboring false positives.
2. Do not repeat activation logic in the loaded body; keep only non-inferable rules, ordered work, just-in-time resources, and verification.
3. Prefer existing repository skills and resources over creating another overlapping router.
4. Move durable depth to focused resources when the body approaches 120 lines; never make all resources mandatory.
5. Separate verified repository facts, source-backed claims, assumptions, and validation needs.

## Workflow

1. Resolve the `create-agent-context-skill` intent and inspect the existing skill catalog.
2. Draft or revise the description before the body; compare it against adjacent skill descriptions.
3. Delete duplicated purpose, activation, generic advice, and arbitrary fixed-count ceremony.
4. Validate metadata, links, size, routing boundaries, and exact verification commands.

## Verification
- `git diff --check -- .agents/contract/intents.yaml .agents/skills`
- Manually validate changed frontmatter against [../_SKILL_SCHEMA.md](../_SKILL_SCHEMA.md).
