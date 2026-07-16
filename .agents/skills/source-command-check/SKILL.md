---
name: "source-command-check"
description: "Run the canonical build + per-project test suite for the repo. Matches AGENTS.md §7 Verification Policy exactly. Never uses solution-level `dotnet test`."
---

# source-command-check

Use this skill when the user asks to run the migrated source command `check`.

## Command Template

<!-- ABOUTME: One-shot build + per-project test runner matching AGENTS.md §7 exactly. -->
<!-- ABOUTME: Run BEFORE editing (baseline) and AFTER editing (verification). -->

# /check — Standard Verification Cycle

> **Source of truth:** [`AGENTS.md`](../../../AGENTS.md) §7 Verification Policy.
> This command is a convenience wrapper — the command strings themselves live in AGENTS.md and `AGENTS.md` to avoid drift.

## When to Run

- **Baseline** — before any work, to confirm the repo is green.
- **Post-edit** — after every logical change unit, before marking a todo done.
- **Pre-PR** — before handing off to `/review-pr`.

## The Commands (copy-paste, in this order)

### 1. Build (Release, quiet)

```bash
dotnet build --configuration Release --verbosity quiet
```

If exit code ≠ 0, STOP. Fix build errors before running tests.

### 2. Test Each Project Individually

```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

### 3. Scoped Run (intent-driven)

Prefer the intent's `minimum_tests` list over running everything. Example, for `add-get-endpoint`:

```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Run the full list only when the change touches multiple layers or you are about to open a PR.

## Skipped by Default


## On Failure

1. Generate a TRX for the failing project:
   ```bash
   dotnet test --project <ProjectPath> --configuration Release -- --report-trx --report-trx-filename results.trx
   ```
2. Fix failures before any planned work.
3. Do NOT delete or skip failing tests.
4. Report any failures that appear to be pre-existing (not caused by your change) separately — do not attempt to fix them without user direction.

## Anti-Patterns

- ❌ Running `dotnet test` at the solution level. Always use `--project`.
- ❌ Running without `--configuration Release`. The repo's CI and architecture tests assume Release.
- ❌ Using `findstr /i` on French-locale Windows (unreliable).
- ❌ Modifying source to make a test pass rather than fixing the root cause.
- ❌ Running tests before the build is green.

## Related

- [`AGENTS.md`](../../../AGENTS.md) §7 — canonical command source.
- [`AGENTS.md`](../../../AGENTS.md) — Codex-specific notes.
- [`docs/TROUBLESHOOTING.md`](../../../docs/TROUBLESHOOTING.md) — common failure modes.
- [`AGENTS.md`](../../../AGENTS.md) — full cold-start workflow (includes this step).
- [`/review-pr`](../../../.claude/commands/review-pr.md) — pre-PR checklist.
