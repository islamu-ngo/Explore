<!-- ABOUTME: Canonical AI-agent contract for the ISLAMU Event platform. -->
<!-- ABOUTME: Defines the Contribution Contract, critical rules, and routing logic. -->

# AGENTS.md — Canonical Agent Contract

> **This is the canonical entrypoint for every AI tool contributing to this repository.**
> Last Updated: 2026-08-12

---

## 1. The Contribution Contract (Read First)

Every change must answer these eight questions **before editing any file**:

| # | Question | Source of Truth |
|---|---|---|
| 1 | What kind of change is this? (the *intent*) | [`.agents/contract/intents.yaml`](.agents/contract/intents.yaml) |
| 2 | Which rules are authoritative? | [`docs/QUICK_REFERENCE.md`](docs/QUICK_REFERENCE.md) + [`.agents/rules/*.md`](.agents/rules/) |
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
| Intent registry | `.agents/contract/intents.yaml` | Machine-readable task mapping |
| Operations | `docs/OPERATIONS.md` | Build/Test details, AI operational rules |
| Durable findings | `dev/_journal/journal.md` | Decisions, non-obvious patterns |

---

## 3. Cold-Start Flow (Zero-Knowledge Agent)

1. **CLASSIFY**: Find matching intent in [`.agents/contract/intents.yaml`](.agents/contract/intents.yaml) and resolve its `criticality` block (`tier`, `intake_clarification_mode`, `exploration_protocol`, `testing_strategy`, `review_protocol`).
2. **DYNAMIC ALIGNMENT & INTAKE**:
   - **Tiers 0, 1, 2 (Sovereign / Security / Privacy)**: Mandate proactive `/grill-me` alignment on edge cases, threat models, and failure recovery before editing. Conduct exhaustive knowledge-graph blast-radius exploration. Author failing *Invariant-Breaker* tests first.
   - **Tier 3 (Domain State)**: Perform bounded caller/callee tracing and standard clarification if requirements are ambiguous.
   - **Tier 4 (Standard UI / Docs)**: Proceed autonomously with economical context budgets, applying established conventions with minimal friction.
3. **LOAD**: Read `must_read_docs` and matching [`.agents/rules/*.md`](.agents/rules/).
4. **EDIT**: Work within `paths_in_scope`. Follow Clean Architecture: Domain → App → Infra → API.
5. **VERIFY & REVIEW**: Run minimum tests. For Tiers 0–2, verify real concurrency/multi-provider engine behavior, run Stryker mutation tests (>85%), and conduct Epistemic Multi-Agent Debate (MAD) review.
6. **TEACH**: Provide a comprehensive technical teaching summary explaining architectural patterns, state transitions, and rollback mechanisms.
7. **ESCALATE**: If any rule conflicts with the request, stop and ask the user.

---

## 4. Rule Authority Order (Conflict Resolution)

1. **CRITICAL RULES** (Section 5 below)
2. **`docs/QUICK_REFERENCE.md`** — Global project invariants
3. **`docs/GOVERNANCE.md`** — Coding conventions and patterns
4. **Matching `.agents/rules/*.md`** — Path-scoped deltas

---

## 5. CRITICAL RULES (Project-Specific Invariants)

> **Rule #1:** Never assume an exception. Get explicit permission before breaking any rule.

1. Repositories return **entities**, never DTOs (map in handlers).
2. Validators are **manually instantiated** (no DI).
3. Use `int` for lookups, `Guid` (UUIDv7) for aggregates, `long` for cursors.
4. GET = `[AllowAnonymous]`, write = `[Authorize]`.
5. Every file must start with a two-line `ABOUTME:` comment summary.
6. **HAL links are the single source of truth for UI**: Clients must gate action affordances (Edit/Delete) by checking `_links` presence, never local role/claim inspection.
7. **EF Core migrations are generated artifacts**: Never hand-edit migration or model-snapshot files. Fix entities/configurations or the migration generator, then delete and regenerate an unapplied development migration with `dotnet ef migrations`.
8. **IP, clean-room, and outbound-license protection**: Never ingest third-party copyleft, source-available, proprietary, or otherwise incompatible source code, snippets, ASTs, SQL, migrations, tests, comments, or assets into implementation context or copy them into this repository. Externally informed work must pass through a source-free functional specification and use independently designed project-native structure, sequence, and organization. A dependency is forbidden unless its terms preserve every intended ISLAMU outbound licensing path or the Project Steward has documented separate licensing and distribution approval. See [`docs/legal/IP_GOVERNANCE.md`](docs/legal/IP_GOVERNANCE.md).

**Full list:** [`docs/QUICK_REFERENCE.md`](docs/QUICK_REFERENCE.md)

---

## 6. Task-Routing Entrypoints

| Starting Point | Go To |
|---|---|
| New request | [`.agents/contract/intents.yaml`](.agents/contract/intents.yaml) — find `triggers` |
| Known path | [`.agents/rules/`](.agents/rules/) — find matching `paths` |
| Pattern/Skill | [`.agents/skills/`](.agents/skills/) — load relevant `SKILL.md` |
| Build/Test | [`docs/OPERATIONS.md#verification-policy`](docs/OPERATIONS.md) |
| UI Workflow | [`docs/BLAZOR_DEV_WORKFLOW.md`](docs/BLAZOR_DEV_WORKFLOW.md) |
| Agent Ops | [`.agents/CONTEXT_ENGINEERING.md`](.agents/CONTEXT_ENGINEERING.md) |
| PR Review | [`.agents/skills/review-pr/SKILL.md`](.agents/skills/review-pr/SKILL.md) |
| Log Finding | [`.agents/skills/finding/SKILL.md`](.agents/skills/finding/SKILL.md) |

---

## 7. Targeted Fetch And Reuse Rule

When a task touches a topic covered by docs, skills, or rules, retrieve the **smallest relevant heading, symbol, or bounded range once**. Follow [`.agents/CONTEXT_ENGINEERING.md`](.agents/CONTEXT_ENGINEERING.md).

**Required context:**
- Use this file from injected session context; do not reread it when it is already present.
- Resolve only the matching entry from [`.agents/contract/intents.yaml`](.agents/contract/intents.yaml); never load the full registry into task context.
- Load only the required headings from relevant `docs/*.md`, the matching skill routers, and matching path rules.
- Prefer graph, outline, symbol, heading, and diff retrieval over full-file reads.
- Reuse an in-session `path + heading/symbol + revision` ledger. Reread only after the source changes, a concrete decision lacks evidence, or contradictory evidence appears.
- Before external functional research, third-party design analysis, or dependency selection, load the relevant sections of [`.agents/skills/ip-clean-room/SKILL.md`](.agents/skills/ip-clean-room/SKILL.md) and [`docs/legal/IP_GOVERNANCE.md`](docs/legal/IP_GOVERNANCE.md); do not load the full legal chain for ordinary official framework documentation.

---

## 8. Verification Baseline

Before the first product edit, establish the green baseline once. Do not rerun an unchanged baseline; every PR must still leave the build and minimum tests green.

**Build Command:**
```bash
dotnet build --configuration Release --verbosity quiet
```

**Full Test List:** [`docs/OPERATIONS.md#full-project-test-list`](docs/OPERATIONS.md)

---

## 9. Agent Operational Baseline

- **Subagents**: Use the lowest-cost capable model. Broad read-only discovery goes to economical scouts with the bounded output contract in [`.agents/CONTEXT_ENGINEERING.md`](.agents/CONTEXT_ENGINEERING.md); the main agent keeps decisions and synthesis. Do not delegate atomic lookups. Index: [`.agents/agents/README.md`](.agents/agents/README.md).
- **Memory**: Resume substantial work from its task-owned `*-context.md`; do not automatically load plan/context/tasks together. Durable findings live in `dev/_journal/`.
- **Todos**: Create immediately for multi-step tasks.
- **Context**: Follow [`.agents/CONTEXT_ENGINEERING.md`](.agents/CONTEXT_ENGINEERING.md); duplicate unchanged context is a defect.

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
