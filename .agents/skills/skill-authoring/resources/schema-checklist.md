<!-- ABOUTME: Concrete checklist for writing schema-compliant SKILL.md files. -->
<!-- ABOUTME: Mirrors _SKILL_SCHEMA.md and AgentContextSchemaTests expectations. -->

# Schema Checklist

## File Shape

- Path is `.claude/skills/<kebab-case-name>/SKILL.md`.
- Folder name equals frontmatter `name`.
- YAML frontmatter is first when present, followed by two `ABOUTME` comments.
- Resources live under `.claude/skills/<name>/resources/*.md`.
- `SKILL.md` stays under 250 lines, with a target of 60 to 180 lines.

## Required Frontmatter

Use exactly the required keys:

```yaml
---
name: example-skill
description: One sentence describing when the skill applies.
type: guardrail | pattern | reference | workflow
enforcement: block | suggest | inform
priority: critical | high | medium | low
---
```

## Required Sections

The section order must be:

1. `## Purpose`
2. `## When to Load`
3. `## When NOT to Load`
4. `## Must-Read Docs`
5. `## Top 5 Invariants`
6. `## Top 5 Anti-Patterns`
7. `## Minimal Examples`
8. `## Verification Hooks`
9. `## Related Skills`

## List Rules

- `Top 5 Invariants` has exactly five numbered items.
- `Top 5 Anti-Patterns` has exactly five numbered items.
- `Must-Read Docs` contains links only, with no explanatory prose.
- `Verification Hooks` uses exact commands or test project names.
- `Related Skills` links to sibling `SKILL.md` files.

## Manual Checks

- Confirm no ASCII diagrams.
- Confirm no long stack overview that belongs in canonical docs.
- Confirm no duplicate copy of `docs/QUICK_REFERENCE.md` rules beyond what the skill must operationalize.
- Confirm every resource file starts with two `ABOUTME` comments.
