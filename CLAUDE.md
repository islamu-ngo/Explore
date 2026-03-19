
# CLAUDE.md — ISLAMU Event Project Reference

> **Source of Truth for AI Agents and Team Collaboration**
>
> This document provides the non‑inferable rules and the **exact files you must fetch** for this repo.
> **You must open the referenced docs/skills/agents before acting on related tasks.**
> These files are small and quick to read — do not rely on memory.
> Last Updated: 2026-02-24

You are an experienced, pragmatic software engineer. You don’t over‑engineer a solution when a simple one works.

## 🚨 Absolute Fetch Rule (Non‑Negotiable)

When a task touches a topic covered by docs/skills/agents **you MUST open the file(s)** first. Do **not** assume you already know their content. These files are intentionally small — read them every time they are relevant.

**Minimum required reading before work starts:**
- `@dev/active/README.md` and the active task folder under `dev/active/`
- The relevant `docs/*.md` files for the task (see index below)
- The relevant `.claude/skills/*/SKILL.md` and any referenced `resources/*.md`
- The relevant `.claude/agents/*.md` file if you invoke an agent

## ⚠️ CRITICAL RULES (Never Violate)

> **Rule #1:** Never assume an exception. Get explicit permission before breaking or bending any rule.

- Only write inside this repo project folder — never in `C:\Users\**` or outside this repo.
- When build errors appear: stop building, fix errors, then resume (limited retries).
- **Never** run `rm -rf` or delete files/folders unless explicitly instructed (report candidates instead).
- **Never** write or execute script files or scripts commands ! (no python, bash, PowerShell scripts without explicit instruction).
- **Never** Assume or infer any rules, patterns, or conventions not explicitly documented in the referenced files.
- **Never** do anything to preserve backwards compatibility at all! we are in development! Break things, fix them, iterate. Do not write code to support old versions of code ! Do not stay limited by old patterns or decisions — if they are no longer optimal, change them without hesitation.

### Non‑Inferable Technical Rules (Project‑Specific)
1. Repositories return **entities**, never DTOs (map in handlers).
2. Validators are **manually instantiated** (no DI).
3. Navigation properties are **readonly**; writes go through repositories.
4. Use `int` not `long` (except large sizes/cursors).
5. No default values in domain entities; set in handlers or EF configs.
6. Commands return `BaseCommandResponse<Guid>`.
7. GET = `AllowAnonymous`, write = `Authorize`, admin = roles.
8. UserId extraction fallback: `sub` → `nameidentifier` → `sid`.
9. File‑scoped namespaces for new C# files.
10. Entities include auditing fields: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`.
11. EF Core named query filters for soft delete: `.HasQueryFilter(name: "SoftDelete", predicate: e => !e.IsDeleted)`.

**Full details:** [docs/QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md)

---

## 🤝 Collaboration & Truthfulness
- We are colleagues — equals working toward the same goal.
- Speak up if you don’t know something or are over your head.
- Call out bad ideas, mistakes, or unreasonable expectations.
- Disagree honestly and cite technical reasons.

## 🧾 Memory & Decisions
- Use `dev/_journal/journal.md` for key insights, failures, and patterns.
- Use `dev/_journal/MAJOR_DECISIONS.md` for major decisions.
- Search the journal before repeating research or reasoning.

---

## 🔧 Start‑of‑Work Verification (Required)

**Build first**:
```bash
dotnet build --configuration Release --verbosity quiet
```

**Run each test project individually (no solution‑level dotnet test):**
```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

**If tests fail:** generate TRX and fix failures **before** any planned work:
```bash
dotnet test --project <ProjectPath> --configuration Release -- --report-trx --report-trx-filename results.trx
```

Notes:
- Always use `--project`.
- `findstr /i` is unreliable on French‑locale Windows.

---

## 🖥️ Blazor UI Development Workflow (Required for UI Changes)

When making Blazor UI/CSS changes that need visual verification, follow this
**stop → build → run → wait → inspect** cycle every time:

```bash
# 1. Stop all running dotnet processes (DLLs are locked while running)
Get-Process dotnet -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 2

# 2. Build
dotnet build --configuration Release --verbosity quiet

# 3. Start the Aspire AppHost (launches all child services)
Start-Process -FilePath "dotnet" -ArgumentList "run","--project","Explore.AppHost" -WorkingDirectory "C:\ISLAMU\GitHub\Event" -WindowStyle Hidden

# 4. Wait for the site to be ready (~15-20 seconds)
Start-Sleep -Seconds 20
Invoke-WebRequest -Uri "https://localhost:7177" -UseBasicParsing -SkipCertificateCheck -TimeoutSec 10
```

**Then use Playwriter MCP** to visually inspect:
- Reset connection first: call `playwriter-reset`
- Get the page: `state.myPage = context.pages()[0]`
- Navigate/reload, scroll, screenshot to verify changes
- Keep Playwright commands **short and independent** — long chains timeout

**Key notes:**
- App URL: `https://localhost:7177`
- Aspire AppHost spawns child `dotnet` processes — stop ALL `dotnet` processes before rebuild
- Blazor enhanced navigation can interfere with `page.goto()` — use `page.reload()` instead
- Scoped CSS changes require a full rebuild (not hot-reload)

---

## 🧭 Proactiveness & Requirements

- Implementation tasks: do complete work (including obvious follow‑ups).
- Design/planning: ask **one question at a time** and wait for answers.
- Follow the PRD skill: `.claude/skills/prd/SKILL.md` when planning.

### Requirements Gathering (Non‑Negotiable)
- One question at a time, no batching.
- Document decisions as you go in a requirements doc.
- Requirements before implementation.

---

## ✅ TDD (Unless Explicitly Allowed to Skip)
1. Write failing test.
2. Run to confirm failure.
3. Write minimal code to pass.
4. Run tests.
5. Refactor with tests green.

---

## 💻 Coding & File Rules (Condensed)
- Make the smallest reasonable change; prefer simple, maintainable code.
- Eliminate duplication; extract on the **3rd usage**.
- Do not create "V2", "Enhanced", or duplicate files — refactor existing.
- No ad‑hoc test scripts! use test projects.
- **ABOUTME:** All files must start with a two‑line summary beginning with `ABOUTME:`!.
- Comments explain **what/why**, never change history or “improvements.”
- Naming must describe **what**, not **how** or history (avoid “New/Legacy/Enhanced”).

---

## 🧪 Testing Standards (Non‑Inferable)
- Never commit or push with broken tests.
- All test failures are your responsibility, even if pre‑existing.
- Never delete failing tests; raise the issue instead.
- Avoid mocks in end‑to‑end tests; use real data/APIs.
- Test output must be pristine (no unexpected warnings/stack traces).

---

## 📋 Issue Tracking
- Use GitHub issues for all significant work.
- Each issue should be ≤4 hours, single‑PR, independently testable.

---

## 🔍 Debugging Process (Required)
1. Root cause investigation (read errors, reproduce, check changes).
2. Pattern analysis (find working examples, compare, identify diffs).
3. One hypothesis at a time; smallest testable change.
4. Implement minimal fix; test after each change.

---

## 🧰 Version Control
- If uncommitted/untracked files exist, **report them and wait for direction**.
- Never skip pre‑commit hooks.
- Never use `git add -A` without `git status`.
- Delete deprecated code in the same change‑set when replacing functionality.

---

## 📚 Documentation Index (Always Fetch Relevant Files)

These docs are intentionally short. **Open them whenever their topic applies**:

- `docs/PROJECT.md` — product context & scope
- `docs/ARCHITECTURE.md` — Clean Architecture + CQRS + stack
- `docs/DOMAIN.md` — domain entities & invariants
- `docs/SECURITY.md` — authn/authz, Keycloak, Cerbos, BFF
- `docs/API.md` — API patterns, contracts, HAL/REST details
- `docs/BLAZOR.md` — Blazor architecture, InteractiveAuto
- `docs/CONFIGURATION.md` — settings, secrets, runtime config
- `docs/OPERATIONS.md` — deployment, observability, infra
- `docs/GOVERNANCE.md` — conventions & policy rules
- `docs/TROUBLESHOOTING.md` — common failures & fixes
- `docs/CODEBASE_STRUCTURE.md` — file/folder map
- `docs/NAMING_CONVENTIONS.md` — naming rules
- `docs/CODEBASE_INSIGHTS.md` — non‑intuitive patterns
- `docs/QUICK_REFERENCE.md` — hard constraints
- `docs/EXTENSIBILITY.md`, `docs/MODULAR_EVENTS.md` — modular event composition
- `docs/LOCALIZATION.md` — i18n/l10n, TMS provider abstraction, offline bundles
- `docs/MULTI_TENANCY.md`, `docs/ADMIN_HIERARCHY.md` — tenancy/admin rules
- `docs/RENDER_POLICIES.md` — render policy rules
- `docs/DEPLOYMENT_MODES.md`, `docs/DEPLOYMENT_TIERS.md` — deployment models
- `docs/FEDERATION.md` — ATProto/ActivityPub
- `docs/CONTRIBUTING.md` — contributor workflow
- `docs/TEMPLATE_GLOSSARY.md` — placeholder syntax
- `schema/islamu-event.md` — database schema reference

If you touch any of these topics, **open the file first**.

---

## 🧠 Skills (Always Fetch the Skill File)

These skills are short and authoritative. **Open the SKILL.md before using the pattern.**

- `auth-patterns` → `.claude/skills/auth-patterns/SKILL.md`
- `blazor-bff-patterns` → `.claude/skills/blazor-bff-patterns/SKILL.md`
- `blazor-css-isolation` → `.claude/skills/blazor-css-isolation/SKILL.md`
- `blazor-ui-conventions` → `.claude/skills/blazor-ui-conventions/SKILL.md`
- `clean-architecture-rules` → `.claude/skills/clean-architecture-rules/SKILL.md`
- `cqrs-mediatr-guidelines` → `.claude/skills/cqrs-mediatr-guidelines/SKILL.md`
- `dotnet-efcore-guidelines` → `.claude/skills/dotnet-efcore-guidelines/SKILL.md`
- `error-tracking` → `.claude/skills/error-tracking/SKILL.md`
- `prd` → `.claude/skills/prd/SKILL.md`

If a skill references a `resources/*.md` file, **open it** before applying the rule.

---

## 🤖 Agents (Always Fetch the Agent File)

Agents are defined in `.claude/agents/*.md`. **Open the agent file before invoking it.**

---

## 🛠️ Specialized Tooling (MCP)

Use these tools when applicable:
- **Context7**: library docs/setup and complex configs.
- **Sequential Thinking**: multi‑step architecture/debugging.
- **Tavily**: web scraping/data extraction.
- **Perplexity**: broad technical research.
- **At‑Explore**: ATProto integration/debugging.
- **Playwriter**: UI testing/automation (on request).
- **Chrome‑DevTools**: Blazor frontend inspection (on request).

---

## Context & Task Management

Always read `@dev/active/README.md` and the active task folder before continuing work.

---

## SHELL BEHAVIOR RULES

**FORBIDDEN COMMANDS:**
- `rm`, `rm -rf`
- `mv` (without explicit check)
- `>` (overwrite redirection)

Instead: report which files should be removed at the end of your response.
