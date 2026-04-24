<!-- ABOUTME: Canonical schema every SKILL.md in this repo must satisfy. -->
<!-- ABOUTME: Enforced by Event.Architecture.Tests.AgentContextSchemaTests. -->

# Skill Schema (Authoritative)

> Every `.claude/skills/*/SKILL.md` MUST conform to this schema.
> `AgentContextSchemaTests` validates structure at build time.
> Skills that intentionally skip migration must be listed in the test's `SkipSchemaMigration` set.

## 1. File Location

```
.claude/skills/<kebab-case-name>/SKILL.md
```

- Folder name MUST match the skill `name` in YAML frontmatter.
- Resources (longer references) live in `.claude/skills/<name>/resources/*.md` — never inline.

## 2. Required YAML Frontmatter

```yaml
---
name: <kebab-case>           # Must match folder name
description: <one sentence>  # When this skill applies; triggers
type: guardrail | pattern | reference | workflow
enforcement: block | suggest | inform
priority: critical | high | medium | low
---
```

All five fields are **required**. `AgentContextSchemaTests` asserts presence and validates `type`/`enforcement`/`priority` enums.

## 3. Required Sections (in order)

### `## Purpose` (≤3 sentences)

What this skill protects, enforces, or accelerates. No stack context, no tech overview — point to AGENTS.md for that.

### `## When to Load`

Bulleted list. Triggers are explicit: keywords, file paths, intent IDs. If a task matches any line, the skill loads.

### `## When NOT to Load`

Bulleted list. Explicit negatives to prevent context bloat. Example: "Not for pure domain-model questions — use `domain` rule instead."

### `## Must-Read Docs`

Links only. No prose. Relative paths from repo root (`docs/ARCHITECTURE.md`, `docs/QUICK_REFERENCE.md`, etc.). Every link MUST resolve (`AgentContextLinkTests` verifies).

### `## Top 5 Invariants`

Numbered list, exactly 5 items. Each invariant is a single sentence describing a non-inferable rule. Anchor terminology to `docs/DOCUMENTATION_STYLE_GUIDE.md` baseline (Instance, Tenant, Organization, BFF, Client, API).

### `## Top 5 Anti-Patterns`

Numbered list, exactly 5 items. Each item: the anti-pattern name + one-sentence consequence. No long justifications — link to docs for context.

### `## Minimal Examples`

Optional but recommended. Use fenced code blocks. Keep to **≤40 lines per example**. Show the shortest code that illustrates correct usage. Avoid full-file examples.

### `## Verification Hooks`

Bulleted list of exact commands or test-project names that catch violations. Example: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`. Commands must be copy-pasteable.

### `## Related Skills`

Bulleted cross-reference to sibling skills. Minimum 1 entry. Use relative paths.

## 4. File Length

- **Target**: 60–180 lines total.
- **Hard max**: 250 lines. If longer, split into `resources/*.md` referenced from Must-Read Docs.

## 5. Forbidden Content

- Stack/tech-overview paragraphs (reference `AGENTS.md`).
- Duplicated invariants from `docs/QUICK_REFERENCE.md` (link, don't copy).
- ASCII diagrams (per `docs/DOCUMENTATION_STYLE_GUIDE.md`).
- "Why clean architecture matters" prose or similar meta-content.

## 6. Enforcement

`Event.Architecture.Tests.AgentContextSchemaTests` checks:
- YAML frontmatter present with all 5 required fields.
- All 8 required sections present in the documented order.
- `Top 5 Invariants` and `Top 5 Anti-Patterns` each contain exactly 5 numbered items.
- Must-Read Docs links resolve (via `AgentContextLinkTests`).
- Total line count ≤ 250.

Skills listed in the test's `SkipSchemaMigration` set are grandfathered but flagged as migration debt.

## 7. Migration Debt (v1)

The following skills are migrated to schema:

- `clean-architecture-rules`
- `cqrs-mediatr-guidelines`
- `dotnet-efcore-guidelines`
- `blazor-ui-conventions`
- `auth-patterns`

All other skills remain on the legacy format until backfilled. See `docs/DOCUMENTATION_IMPROVEMENT_RESEARCH.md` for promotion cadence.

## Related

- Agent schema → [`.claude/agents/_AGENT_SCHEMA.md`](../agents/_AGENT_SCHEMA.md)
- Intent registry → [`.claude/contract/intents.yaml`](../contract/intents.yaml)
- Documentation style → [`docs/DOCUMENTATION_STYLE_GUIDE.md`](../../docs/DOCUMENTATION_STYLE_GUIDE.md)
