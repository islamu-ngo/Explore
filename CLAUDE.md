<!-- ABOUTME: Canonical AI-agent contract for the ISLAMU Event platform (all tools). -->
<!-- ABOUTME: Defines the Contribution Contract, critical rules, verification policy, and Claude-specific operational detail. -->

# CLAUDE.md — Canonical Agent Contract

> **This is the canonical entrypoint for every AI coding tool contributing to this repository.**
> Tools that read `AGENTS.md` (OpenAI Codex, Cursor, Gemini CLI, GitHub Copilot, Zed, Aider, and others) are redirected here by the root `AGENTS.md` stub.
> Last Updated: 2026-04-24

---

## 1. The Contribution Contract (Read First)

Every change to this repository — by a human or an agent — must deterministically answer these eight questions **before editing any file**:

| # | Question | Source of Truth |
|---|---|---|
| 1 | What kind of change is this? (the *intent*) | [`.claude/contract/intents.yaml`](.claude/contract/intents.yaml) |
| 2 | Which rules are authoritative? | [`docs/QUICK_REFERENCE.md`](docs/QUICK_REFERENCE.md) + [`docs/GOVERNANCE.md`](docs/GOVERNANCE.md) + matching `.claude/rules/*.md` |
| 3 | Which files must be read first? | The intent's `must_read_docs` field |
| 4 | Which files may be changed? | The intent's `paths_in_scope` field |
| 5 | Which tests must run at minimum? | The intent's `minimum_tests` field |
| 6 | Which docs must be updated? | The intent's `docs_to_update` field |
| 7 | Which PR checklist applies? | The intent's `pr_checklist` field |
| 8 | What is forbidden without explicit approval? | The intent's `forbidden_without_approval` field + [`docs/QUICK_REFERENCE.md`](docs/QUICK_REFERENCE.md) |

**You may not skip this contract.** If no intent in `intents.yaml` matches your task, you must either (a) select the closest intent and note the deviation in the PR description, or (b) stop and propose a new intent via the procedure in [`.claude/contract/README.md`](.claude/contract/README.md).

---

## 2. Canonical Artifacts (Single Source of Truth)

The repository has **exactly one** authoritative file for each concern. Do not create forks, mirrors, or "V2" variants.

| Concern | Canonical File | Purpose |
|---|---|---|
| AI agent contract | `CLAUDE.md` (this file) | Every agent starts here |
| Tool-neutral entry (cross-tool compat) | `AGENTS.md` | 2-line pointer to this file for tools that discover `AGENTS.md` natively |
| Human navigation root | `docs/index.md` | AI retrieval index + human reading order |
| Invariant reference | `docs/QUICK_REFERENCE.md` | Hard constraints, non-inferable rules |
| Governance / decisions | `docs/GOVERNANCE.md` | Conventions, decision frameworks, design patterns |
| Architecture overview | `docs/ARCHITECTURE.md` | Layering, request flow, CQRS, multi-tenancy |
| Intent registry | `.claude/contract/intents.yaml` | Machine-readable task → context map |
| Intent schema | `.claude/contract/schema.json` | JSON Schema validating `intents.yaml` |
| Path-scoped rules | `.claude/rules/*.md` | Auto-loaded by agent tooling based on `paths:` frontmatter |
| Skills (how-to patterns) | `.claude/skills/*/SKILL.md` | Loaded on demand for specific domains |
| Subagent definitions | `.claude/agents/*.md` | Role-specific agent prompts |
| Durable findings log | `dev/_journal/journal.md` | Bug root causes, non-obvious patterns, major decisions |

Everything else is derived, illustrative, or scoped to a subdirectory.

---

## 3. Cold-Start Flow (Zero-Knowledge Agent)

If you are encountering this repository for the first time:

```
1. CLASSIFY → Read .claude/contract/intents.yaml. Find the intent that matches the request
              (bug report, feature, controller action, migration, HAL link, etc.).
              If nothing fits: stop and ask the user.

2. LOAD     → Open exactly the files listed in the intent's `must_read_docs` and any
              matching `.claude/rules/*.md` whose `paths:` glob matches files you will touch.
              Also open `dev/active/README.md` and the current active task folder under
              `dev/active/` if one exists. Do NOT prefetch the entire docs folder.

3. EDIT     → Work within `paths_in_scope`. Respect `paths_forbidden`.
              Apply the rules from the loaded skills/rules. Follow Clean Architecture
              dependency direction: Domain → Application → Infrastructure → API/Blazor.

4. VERIFY   → Run the intent's `verification_commands` (build + minimum_tests).
              All listed tests must pass on Release configuration.

5. ESCALATE → If any rule conflicts with the request, stop and ask the user.
              Never assume an exception (see CRITICAL RULES below).
```

For a guided cold-start (Claude Code), run `/bootstrap` or read [`.claude/commands/bootstrap.md`](.claude/commands/bootstrap.md).

---

## 4. Rule Authority Order (Conflict Resolution)

When rules appear to conflict, this is the priority order — highest wins:

1. **CRITICAL RULES** in this file (section 5 below) — absolute, never bent without user approval
2. **`docs/QUICK_REFERENCE.md`** — project-specific invariants, non-inferable constraints
3. **`docs/GOVERNANCE.md`** — code conventions, decision frameworks, design patterns
4. **Matching `.claude/rules/*.md`** — path-scoped rules for the file you are editing
5. **`.claude/skills/*/SKILL.md`** — domain pattern guidance (opt-in per intent)
6. **Area-specific docs** (`docs/ARCHITECTURE.md`, `docs/SECURITY.md`, `docs/API.md`, etc.) — explanatory references
7. **Journal entries** (`dev/_journal/*.md`) — durable findings that have not yet been promoted to rules
8. **Existing code patterns** — only when none of the above answer the question

If a `.claude/rules/*.md` rule appears to contradict `QUICK_REFERENCE.md` or `GOVERNANCE.md`, the canonical doc wins and the rule file must be fixed.

---

## 5. CRITICAL RULES (Never Violate Without Explicit Approval)

> **Rule #1:** Never assume an exception. Get explicit permission before breaking or bending any rule. Never write code that silently widens authorization, bypasses validation, or disables tenant isolation.

- **File scope:** Only write inside this repo — never `C:\Users\**` or elsewhere on the host machine.
- **Build discipline:** When build errors appear, stop building, fix errors, then resume (limited retries — see [`docs/TROUBLESHOOTING.md`](docs/TROUBLESHOOTING.md)).
- **No destructive commands:** Never run `rm -rf`, `mv` without explicit check, or `>` (overwrite redirection) unless explicitly instructed. Report deletion candidates at the end of your response instead.
- **No ad-hoc scripts:** Never write or execute Python / Bash / PowerShell scripts without explicit instruction. Use the existing test projects, MCP tools, and documented commands.
- **No inferred rules:** Never assume a rule or convention that is not explicitly documented in the files above. If the rule is missing, propose adding it — don't invent it.
- **No backward-compatibility scaffolding:** We are in active development. Break things, fix them, iterate. Do not write shims, feature flags, or deprecation layers to preserve old behavior. If a pattern is no longer optimal, change it.
- **No sensitive-file commits:** Never stage `.env`, credentials, private keys, or secrets. If you see them, warn the user.

### Non-Inferable Technical Rules (Project-Specific)

These are **invariants** of this codebase. They are not inferable from the code and must be followed:

1. Repositories return **entities**, never DTOs (map in handlers).
2. Validators are **manually instantiated** (no DI).
3. Navigation properties are **readonly**; writes go through repositories.
4. Use `int` not `long` (except large sizes / cursors).
5. No default values in domain entities; set in handlers or EF configs.
6. Commands return `BaseCommandResponse<Guid>` (create/update; some deletes still return `bool`).
7. GET = `[AllowAnonymous]`, write = `[Authorize]`, admin = roles.
8. UserId extraction fallback: `sub` → `nameidentifier` → `sid`.
9. File-scoped namespaces for new C# files.
10. Entities include auditing fields: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`.
11. EF Core named query filters for soft delete: `.HasQueryFilter(name: "SoftDelete", predicate: e => !e.IsDeleted)`.
12. In the Blazor UI, HAL `_links` is the **exclusive** source of action affordance. Gate mutation buttons with `dto.HasHalLink("edit")` helpers. Never use `RoleHelper.CanManage`, `IsInRole`, or claim inspection for per-resource action gating.
13. Every file must start with a two-line `ABOUTME:` comment summary.

**Full list:** [`docs/QUICK_REFERENCE.md`](docs/QUICK_REFERENCE.md)

---

## 6. Task-Routing Entrypoints

| Starting Point | Go To |
|---|---|
| You have an issue / bug report / feature request | [`.claude/contract/intents.yaml`](.claude/contract/intents.yaml) — find the matching `triggers` |
| You know the file path you will edit | [`.claude/rules/`](.claude/rules/) — find the rule whose `paths:` glob matches |
| You need a design pattern (CQRS, EF Core, Blazor, auth) | [`.claude/skills/`](.claude/skills/) — load the relevant `SKILL.md` |
| You are about to open a PR | `/review-pr` command or [`.claude/commands/review-pr.md`](.claude/commands/review-pr.md) |
| You need to run builds / tests | `/check` command or see **§8 Verification Policy** below |
| You want to log a finding (not yet a rule) | `/finding` command or [`dev/_journal/FINDING_TEMPLATE.md`](dev/_journal/FINDING_TEMPLATE.md) |
| You need Blazor visual verification | [`docs/BLAZOR_DEV_WORKFLOW.md`](docs/BLAZOR_DEV_WORKFLOW.md) |
| You are doing a cold start (first session) | Run `/bootstrap` or read [`.claude/commands/bootstrap.md`](.claude/commands/bootstrap.md) |
| You want to scaffold a new CQRS handler | `/new-handler` command or [`.claude/commands/new-handler.md`](.claude/commands/new-handler.md) |
| You want to lint cross-links | `/docs-lint` command or [`.claude/commands/docs-lint.md`](.claude/commands/docs-lint.md) |

The intent registry is the primary abstraction. **Path-scoped rules are secondary** — they refine behavior once you know which file you are editing.

---

## 7. Absolute Fetch Rule (Non-Negotiable)

When a task touches a topic covered by docs / skills / agents / rules, you **MUST open the file(s)** first. Do **not** assume you already know their content. These files are intentionally small — read them every time they are relevant.

**Minimum required reading before work starts:**
- This file (`CLAUDE.md`) — the Contribution Contract
- [`dev/active/README.md`](dev/active/README.md) and the active task folder under `dev/active/`
- The relevant `docs/*.md` files for the task (see [`docs/index.md`](docs/index.md) — the canonical navigation root)
- The matching `.claude/skills/*/SKILL.md` and any referenced `resources/*.md`
- The matching `.claude/agents/*.md` file if you invoke an agent
- The matching `.claude/rules/*.md` for the file path you will edit (auto-loaded by Claude Code v2.0.64+)

---

## 8. Verification Policy (Execution Baseline)

Every session **must** start with a green build before making changes. Every PR **must** leave the build and its declared minimum tests green.

**Shortcut:** the `/check` slash command runs the build + per-project tests in one go.

**Build first** — always, before any changes:
```bash
dotnet build --configuration Release --verbosity quiet
```

**Run each test project individually** (no solution-level `dotnet test`):
```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
```

`Explore.Blazor.Client.E2ETests` requires running infrastructure (Aspire AppHost) and is not included in standard test runs.

**On failure**, generate a TRX and fix failures **before** any planned work:
```bash
dotnet test --project <ProjectPath> --configuration Release -- --report-trx --report-trx-filename results.trx
```

Notes:
- Always use `--project`.
- `findstr /i` is unreliable on French-locale Windows.

---

## 9. Blazor UI Development Workflow

For any Blazor / CSS / MudBlazor change that needs visual verification, follow the **stop → build → run → wait → inspect** cycle documented in [`docs/BLAZOR_DEV_WORKFLOW.md`](docs/BLAZOR_DEV_WORKFLOW.md). Do not skip steps.

---

## 10. Claude-Specific Operational Rules

### Subagent Delegation

- **Prefer subagents over direct work** when a domain match exists. Index: [`.claude/agents/`](.claude/agents/) and [`.claude/agents/README.md`](.claude/agents/README.md).
- Every `task()` call **must** include `load_skills=[...]` and `run_in_background=<bool>`.
- Background grep (explore, librarian) is always `run_in_background=true`. Never block on them.
- Store every returned `session_id` — continuing the same subagent saves 70%+ tokens.

### MCP Tool Use

| MCP Server | Use For |
|---|---|
| Context7 | Library docs, configuration references, pinned API versions |
| Tavily | Web research, scraping, extraction |
| Sequential Thinking | Multi-step architecture / debugging / tradeoff analysis |
| At-Explore | ATProto / ActivityPub integration & debugging |
| Playwriter | Blazor UI testing & visual inspection |
| Chrome-DevTools | Frontend / network / performance inspection |
| GitKraken | Branches, commits, PRs, stashing (use the `gitkraken-cli` skill) |
| Aspire | Distributed-app orchestration (use the `aspire` skill) |

### Session Memory

- **Short-term** (this session's plan + tasks): `dev/active/<task-name>/` — see [`dev/active/README.md`](dev/active/README.md).
- **Durable** (findings, patterns, decisions): [`dev/_journal/journal.md`](dev/_journal/journal.md), [`dev/_journal/MAJOR_DECISIONS.md`](dev/_journal/MAJOR_DECISIONS.md). See [`dev/_journal/PROMOTION_RULES.md`](dev/_journal/PROMOTION_RULES.md) for when a finding graduates to a canonical rule.
- **Search the journal before re-researching** — most "weird" behaviors are already documented.

### Todo Discipline

- Create todos IMMEDIATELY for any multi-step task (2+ steps). Use the `todowrite` tool.
- Mark exactly ONE todo `in_progress` at a time.
- Mark `completed` as soon as done — do not batch.
- This is non-negotiable for non-trivial work.

---

## 11. Coding & File Standards

### File Rules
- Make the smallest reasonable change; prefer simple, maintainable code.
- Eliminate duplication; extract on the **3rd usage**.
- Do not create "V2", "Enhanced", or duplicate files — refactor existing.
- No ad-hoc test scripts — use the test projects.
- Every file begins with a two-line `ABOUTME:` header.
- Comments explain **what / why**, never change history or "improvements".
- Naming describes **what**, not **how** or history (avoid "New / Legacy / Enhanced").

### TDD (unless explicitly waived)
1. Write failing test.
2. Run to confirm failure.
3. Write minimal code to pass.
4. Run tests.
5. Refactor with tests green.

### Testing Standards
- Never commit or push with broken tests.
- All test failures are your responsibility, even if pre-existing.
- Never delete failing tests; raise the issue instead.
- Avoid mocks in end-to-end tests; use real data / APIs.
- Test output must be pristine (no unexpected warnings / stack traces).

### Issue Tracking
- Use GitHub issues for all significant work.
- Each issue should be ≤4 hours, single-PR, independently testable.

### Version Control
- If uncommitted / untracked files exist, **report them and wait for direction**.
- Never skip pre-commit hooks.
- Never `git add -A` without `git status`.
- Delete deprecated code in the same change-set when replacing functionality.

---

## 12. Collaboration & Truthfulness

- We are colleagues — equals working toward the same goal.
- Speak up if you don't know something or are over your head. Do not fabricate file paths, class names, or behaviors.
- Call out bad ideas, mistakes, or unreasonable expectations. Disagree honestly and cite technical reasons.
- Ask **one question at a time** during requirements gathering; do not batch.

---

## 13. Tool-Specific Bootloaders

| Tool | Entry File | Notes |
|---|---|---|
| Claude Code | `CLAUDE.md` (this file) | Canonical — everything lives here |
| GitHub Copilot | [`.github/copilot-instructions.md`](.github/copilot-instructions.md) | Thin pointer to this file |
| OpenAI Codex / Cursor / Gemini / Zed / Aider | [`AGENTS.md`](AGENTS.md) | 2-line pointer to this file |

**Rule:** Tool-specific files MUST NOT duplicate content from this file. They may add tool-specific operational details (hook names, MCP tool names, keyboard shortcuts) but must point back here for all rules and policies.

---

## 14. Enforcement

The integrity of this contract is enforced by architecture tests under `Event.Architecture.Tests/`:

- `AgentContextSchemaTests` — validates every `.claude/rules/*.md` has the required YAML frontmatter; every `.claude/skills/*/SKILL.md` and `.claude/agents/*.md` has the required sections.
- `AgentContextIntentManifestTests` — validates `.claude/contract/intents.yaml` conforms to `schema.json`, all referenced paths exist, all referenced tests exist.
- `AgentContextLinkTests` — no dead links in any `.claude/**/*.md`, `AGENTS.md`, `CLAUDE.md`, or `docs/index.md`.
- `AgentContextDuplicationTests` — prevents reintroducing duplicated project-context blocks across agents.

CI workflow: [`.github/workflows/agent-context.yml`](.github/workflows/agent-context.yml).

If you fail any of these, your PR cannot merge. Fix the context system — don't work around it.

---

## Appendix: Shell Behavior Rules

**FORBIDDEN COMMANDS:**
- `rm`, `rm -rf`
- `mv` (without explicit check)
- `>` (overwrite redirection)

Instead: report which files should be removed / moved at the end of your response and wait for direction.

---

## See Also

- **Hard invariants** → [`docs/QUICK_REFERENCE.md`](docs/QUICK_REFERENCE.md)
- **Governance + decision frameworks** → [`docs/GOVERNANCE.md`](docs/GOVERNANCE.md)
- **Architecture overview** → [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- **Navigation root (human + AI retrieval)** → [`docs/index.md`](docs/index.md)
- **Blazor visual-verification cycle** → [`docs/BLAZOR_DEV_WORKFLOW.md`](docs/BLAZOR_DEV_WORKFLOW.md)
- **Intent registry** → [`.claude/contract/intents.yaml`](.claude/contract/intents.yaml)
- **Rule files (auto-loaded)** → [`.claude/rules/`](.claude/rules/)
- **Skills** → [`.claude/skills/`](.claude/skills/)
- **Subagents** → [`.claude/agents/`](.claude/agents/)
- **Custom slash commands** → [`.claude/commands/`](.claude/commands/)
- **Cold-start benchmarks** → [`.claude/benchmarks/README.md`](.claude/benchmarks/README.md)
- **Findings journal** → [`dev/_journal/README.md`](dev/_journal/README.md)
