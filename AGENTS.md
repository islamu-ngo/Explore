<!-- ABOUTME: Canonical AI-agent contract for the ISLAMU Event platform. -->
<!-- ABOUTME: Defines the Contribution Contract, critical rules, and routing logic. -->

# AGENTS.md — Canonical Agent Contract

> **This is the canonical entrypoint for every AI tool contributing to this repository.**
> Last Updated: 2026-05-17

---

## 1. The Contribution Contract (Read First)

Every change must answer these eight questions **before editing any file**:

| # | Question | Source of Truth |
|---|---|---|
| 1 | What kind of change is this? (the *intent*) | [`.claude/contract/intents.yaml`](.claude/contract/intents.yaml) |
| 2 | Which rules are authoritative? | [`docs/QUICK_REFERENCE.md`](docs/QUICK_REFERENCE.md) + [`.claude/rules/*.md`](.claude/rules/) |
| 3 | Which files must be read first? | The intent's `must_read_docs` field |
| 4 | Which files may be changed? | The intent's `paths_in_scope` field |
| 5 | Which tests must run at minimum? | The intent's `minimum_tests` field |
| 6 | Which docs must be updated? | The intent's `docs_to_update` field |
| 7 | Which PR checklist applies? | The intent's `pr_checklist` field |
| 8 | What is forbidden? | The intent's `forbidden_without_approval` + `QUICK_REFERENCE.md` |

---

## 2. Canonical Artifacts (Single Source of Truth)

| Concern | Canonical File | Purpose |
|---|---|---|
| AI agent contract | `AGENTS.md` (this) | Every agent starts here |
| Invariant reference | `docs/QUICK_REFERENCE.md` | Global hard constraints |
| Governance | `docs/GOVERNANCE.md` | Conventions, design patterns |
| Intent registry | `.claude/contract/intents.yaml` | Machine-readable task mapping |
| Operations | `docs/OPERATIONS.md` | Build/Test details, AI operational rules |
| Durable findings | `dev/_journal/journal.md` | Decisions, non-obvious patterns |

---

## 3. Cold-Start Flow (Zero-Knowledge Agent)

1. **CLASSIFY**: Find matching intent in [`.claude/contract/intents.yaml`](.claude/contract/intents.yaml).
2. **LOAD**: Read `must_read_docs` and matching [`.claude/rules/*.md`](.claude/rules/).
3. **EDIT**: Work within `paths_in_scope`. Follow Clean Architecture: Domain → App → Infra → API.
4. **VERIFY**: Run minimum tests. Build and architecture tests must pass.
5. **ESCALATE**: If any rule conflicts with the request, stop and ask the user.

---

## 4. Rule Authority Order (Conflict Resolution)

1. **CRITICAL RULES** (Section 5 below)
2. **`docs/QUICK_REFERENCE.md`** — Global project invariants
3. **`docs/GOVERNANCE.md`** — Coding conventions and patterns
4. **Matching `.claude/rules/*.md`** — Path-scoped deltas

---

## 5. CRITICAL RULES (Project-Specific Invariants)

> **Rule #1:** Never assume an exception. Get explicit permission before breaking any rule.

1. Repositories return **entities**, never DTOs (map in handlers).
2. Validators are **manually instantiated** (no DI).
3. Use `int` for lookups, `Guid` (UUIDv7) for aggregates, `long` for cursors.
4. GET = `[AllowAnonymous]`, write = `[Authorize]`.
5. Every file must start with a two-line `ABOUTME:` comment summary.
6. **HAL links are the single source of truth for UI**: Clients must gate action affordances (Edit/Delete) by checking `_links` presence, never local role/claim inspection.

**Full list:** [`docs/QUICK_REFERENCE.md`](docs/QUICK_REFERENCE.md)

---

## 6. Task-Routing Entrypoints

| Starting Point | Go To |
|---|---|
| New request | [`.claude/contract/intents.yaml`](.claude/contract/intents.yaml) — find `triggers` |
| Known path | [`.claude/rules/`](.claude/rules/) — find matching `paths` |
| Pattern/Skill | [`.agents/skills/`](.agents/skills/) — load relevant `SKILL.md` |
| Build/Test | [`docs/OPERATIONS.md#verification-policy`](docs/OPERATIONS.md) |
| UI Workflow | [`docs/BLAZOR_DEV_WORKFLOW.md`](docs/BLAZOR_DEV_WORKFLOW.md) |
| Agent Ops | [`docs/OPERATIONS.md#ai-agent-operational-rules`](docs/OPERATIONS.md) |
| PR Review | [`.claude/commands/review-pr.md`](.claude/commands/review-pr.md) |
| Log Finding | [`.claude/commands/finding.md`](.claude/commands/finding.md) |

---

## 7. Absolute Fetch Rule

When a task touches a topic covered by docs / skills / rules, you **MUST open the file(s)** first.

**Minimum required reading:**
- This file (`AGENTS.md`)
- Relevant `docs/*.md` (see [`docs/index.md`](docs/index.md))
- Matching `.agents/skills/*/SKILL.md`
- Matching `.claude/rules/*.md` for the file paths you will edit

---

## 8. Verification Baseline

Every session must start with a green build. Every PR must leave the build and minimum tests green.

**Build Command:**
```bash
dotnet build --configuration Release --verbosity quiet
```

**Full Test List:** [`docs/OPERATIONS.md#full-project-test-list`](docs/OPERATIONS.md)

---

## 9. Agent Operational Baseline

- **Subagents**: Prefer delegation. Index: [`.claude/agents/README.md`](.claude/agents/README.md).
- **Memory**: Short-term in `dev/active/`, Durable in `dev/_journal/`.
- **Todos**: Create immediately for multi-step tasks.
- **Rules**: See [`docs/OPERATIONS.md#ai-agent-operational-rules`](docs/OPERATIONS.md).

### Final Teaching Summary Requirement

Before an implementation agent ends a task, pauses for the user's next prompt, performs a handoff, or claims work is complete, the final response MUST teach the user what changed. Do not give only an abstract status line such as “email sending implemented” or “docs updated.” The user is a developer and must understand the implementation without opening the diff.

The final summary must be medium-sized and technically specific. Include the architecture/design pattern used, concrete libraries/frameworks/infrastructure/protocols, important files/classes/handlers/components changed, data/control flow, relevant best practices such as transactional outbox, CQRS/MediatR, Clean Architecture, HAL affordance gating, tenant isolation, idempotency, retry/error handling, and what was verified or remains. Keep it concise enough for chat, but detailed enough that the user learns the implemented approach.

---

## 10. Tool-Specific Bootloaders

| Tool | Entry File |
|---|---|
| Claude Code | `AGENTS.md` (this) |
| GitHub Copilot | [`.github/copilot-instructions.md`](.github/copilot-instructions.md) |
| Gemini / Others | [`AGENTS.md`](AGENTS.md) |

<!-- code-review-graph MCP tools -->
## MCP Tools: code-review-graph

**IMPORTANT: This project has a knowledge graph. ALWAYS use the
code-review-graph MCP tools BEFORE using Grep/Glob/Read to explore
the codebase.** The graph is faster, cheaper (fewer tokens), and gives
you structural context (callers, dependents, test coverage) that file
scanning cannot.

### When to use graph tools FIRST

- **Exploring code**: `semantic_search_nodes_tool` or `query_graph_tool` instead of Grep
- **Understanding impact**: `get_impact_radius_tool` instead of manually tracing imports
- **Code review**: `detect_changes_tool` + `get_review_context_tool` instead of reading entire files
- **Finding relationships**: `query_graph_tool` with callers_of/callees_of/imports_of/tests_for
- **Architecture questions**: `get_architecture_overview_tool` + `list_communities_tool`

Fall back to Grep/Glob/Read **only** when the graph doesn't cover what you need.

### Key Tools

| Tool | Use when |
| ------ | ---------- |
| `detect_changes_tool` | Reviewing code changes — gives risk-scored analysis |
| `get_review_context_tool` | Need source snippets for review — token-efficient |
| `get_impact_radius_tool` | Understanding blast radius of a change |
| `get_affected_flows_tool` | Finding which execution paths are impacted |
| `query_graph_tool` | Tracing callers, callees, imports, tests, dependencies |
| `semantic_search_nodes_tool` | Finding functions/classes by name or keyword |
| `get_architecture_overview_tool` | Understanding high-level codebase structure |
| `refactor_tool` | Planning renames, finding dead code |

### Workflow

1. The graph auto-updates on file changes (via hooks).
2. Use `detect_changes_tool` for code review.
3. Use `get_affected_flows_tool` to understand impact.
4. Use `query_graph_tool` pattern="tests_for" to check coverage.
