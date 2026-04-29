<!-- ABOUTME: Canonical schema every subagent file in this repo must satisfy. -->
<!-- ABOUTME: Enforced by Event.Architecture.Tests.AgentContextSchemaTests and AgentContextDuplicationTests. -->

# Agent Schema (Authoritative)

> Every `.claude/agents/*.md` (excluding `README.md`, `_AGENT_SCHEMA.md`) MUST conform to this schema.
> `AgentContextSchemaTests` validates structure.
> `AgentContextDuplicationTests` blocks re-introducing project-context blocks across agents.

## 1. File Location

```
.claude/agents/<kebab-case-name>.md
```

One file per agent. No subfolders. No resources directory — agents are intentionally **small and rereadable**.

## 2. Required YAML Frontmatter

```yaml
---
name: <kebab-case>           # Must match filename (without .md)
description: <one sentence>  # What this agent does + when to invoke it
type: diagnostic | review | implementation | domain | research
enforcement: suggest | inform
priority: critical | high | medium | low
tools: Read, Write, Edit, Bash, Glob, Grep   # Whitelist exact tools
---
```

All six fields required. `tools` is a whitelist — the agent may not use tools outside the list.

## 3. Required Sections (in order)

### `## Purpose` (≤2 sentences)

Role-scoped statement. No stack context, no project overview. Example: "Fixes 401/403 authentication bugs in BFF and API by tracing the token forwarding chain and comparing against the expected OIDC/JWT contract."

### `## When to Use`

Bulleted list. Triggers: issue categories, error signatures, code paths. If a task matches any line, this agent is a candidate.

### `## When NOT to Use`

Bulleted list. Explicit negatives. Example: "Not for general build errors — use `auto-error-resolver`."

### `## Mandatory Reads`

Numbered list. Links to the canonical artifacts the agent MUST consult every invocation:
- Always include: `CLAUDE.md`
- Always include: `docs/QUICK_REFERENCE.md`
- Plus role-specific files (agent-specific docs, skills, rules).

Every link MUST resolve. `AgentContextLinkTests` verifies.

### `## Allowed Tools`

Must mirror the `tools` list from frontmatter. Describe why each tool is allowed.

### `## Forbidden Moves`

Bulleted list. Explicit "never do this" items. Example: "Never modify files outside the intent's `paths_in_scope`." Example: "Never bypass `dotnet build` in favor of IDE build output."

### `## Output Contract`

Structured. What the agent returns to the orchestrator. Example:

```
- Summary: <2-5 sentences>
- Evidence: <commands run, files read, findings>
- Diffs: <applied or proposed>
- Next actions: <for the user or next agent>
```

### `## Done Criteria`

Numbered list. Objective, testable conditions. Example:
1. `dotnet build --configuration Release` exits 0.
2. Target test project passes locally.
3. No new files created outside intent scope.

### `## Anti-Patterns`

Bulleted list. Failure modes this agent frequently produces if unchecked. Each item is prescriptive.

### `## Related Agents`

Bulleted cross-reference to sibling agents. Minimum 1 entry.

## 4. File Length

- **Target**: 50–120 lines.
- **Hard max**: 160 lines. Agents are small and rereadable by design.

## 5. Forbidden Content (DUPLICATION GUARD)

`AgentContextDuplicationTests` blocks re-introducing any of the following in `.claude/agents/*.md`:

- Stack overview ("This repo uses .NET 10 + Blazor ...").
- Repetition of critical rules from `AGENTS.md` §5.
- Verbatim test-project lists from `AGENTS.md` §7.
- Verbatim 13-rule non-inferable list.
- ASCII diagrams.

Rule: If the content is in `AGENTS.md`, `docs/QUICK_REFERENCE.md`, or `docs/GOVERNANCE.md`, **link** — don't copy.

Detection strategy: line-hash Jaccard similarity ≥ 0.85 between any two agent files on ≥ 15 consecutive lines triggers failure.

## 6. Enforcement

`Event.Architecture.Tests.AgentContextSchemaTests` checks:
- YAML frontmatter with all 6 required fields.
- All 10 required sections present in order.
- `Mandatory Reads` contains links to at least `AGENTS.md` and `docs/QUICK_REFERENCE.md`.
- Total line count ≤ 160.

`Event.Architecture.Tests.AgentContextDuplicationTests` checks:
- Line-hash Jaccard similarity across agent files.
- Pattern-match against forbidden content (stack overview markers, etc.).

## 7. Migration Scope (v1)

All 13 agents migrated simultaneously:

- `auth-route-debugger`
- `auth-route-tester`
- `auto-error-resolver`
- `blazor-component-architect`
- `clean-code-architect`
- `code-architecture-reviewer`
- `codebase-verifier`
- `code-refactor-master`
- `documentation-architect`
- `frontend-error-fixer`
- `plan-reviewer`
- `refactor-planner`
- `web-research-specialist`

## Related

- Skill schema → [`.claude/skills/_SKILL_SCHEMA.md`](../skills/_SKILL_SCHEMA.md)
- Contribution contract → [`.claude/contract/README.md`](../contract/README.md)
- Documentation style → [`docs/DOCUMENTATION_STYLE_GUIDE.md`](../../docs/DOCUMENTATION_STYLE_GUIDE.md)
