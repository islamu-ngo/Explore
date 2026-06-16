---
name: docs-lint
description: Verify documentation link and quality checks through the Event.Architecture.Tests project. Mirrors the agent-context CI check so you catch broken docs before pushing.
type: verification
priority: high
---
<!-- ABOUTME: Documentation link lint command. Mirrors Event.Architecture.Tests.AgentContextLinkTests. -->
<!-- ABOUTME: Run before opening a PR to catch broken cross-references between docs, rules, skills, and agents. -->

# /docs-lint — Documentation Quality

> **Enforced by:** `Event.Architecture.Tests` documentation and agent-context tests (in CI).
> **This command:** a local pre-flight that catches the same class of failures before you push.

## When to Run

- After editing any `.claude/**/*.md` file.
- After editing `AGENTS.md`, `AGENTS.md`, `docs/index.md`, or canonical docs.
- Before opening a PR (`/review-pr` runs this implicitly).

## What It Checks

The link checks treat the following files as link graph roots:

1. `AGENTS.md`
2. `.github/copilot-instructions.md`
3. `docs/index.md`
4. `.claude/contract/**/*.md`
5. `.claude/rules/**/*.md`
6. `.claude/agents/**/*.md`
7. migrated `.agents/skills/*/SKILL.md`
8. selected custom `.claude/commands/*.md`
9. journal/benchmark index files

For every Markdown link (bracketed text followed by a parenthesized relative target), the check asserts the target exists on disk.

The documentation quality checks also validate metadata/source anchors for newly canonical docs and block stale VSTest-style filter examples in root docs and custom commands.

## How to Run It

### Option A — Authoritative (uses the CI check)

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

This is the truth. If this passes, your documentation links and docs quality checks are valid for the current CI gate.

### Option B — Quick local grep (not authoritative)

```bash
grep -r -o -E '\]\([^)]+\)' .claude/ AGENTS.md AGENTS.md docs/index.md 2>/dev/null \
  | awk -F: '{print $1 ": " $2}'
```

Scan the output for typos, then verify specific paths with:

```bash
ls -la <path>
```

## Common Failure Modes

| Symptom | Cause | Fix |
|---|---|---|
| Link fails for `.claude/rules/foo.md` | New rule not yet created | Create the rule or remove the link |
| Link fails for `docs/Foo.md` | Case sensitivity mismatch | Match exact filename casing |
| Link fails for `../../docs/X.md` | Wrong depth from `.claude/...` | Count folder levels carefully |
| Link fails for a skill | Skill folder renamed | Rename every reference (use `grep -r`) |
| Link fails for a test project | Typo in project name | Verify in `AGENTS.md` §7 list |

## After a Rename / Move

Use grep to find every reference before renaming:

```bash
grep -r -l "OLD_NAME" .claude/ docs/ AGENTS.md AGENTS.md dev/
```

Update each occurrence, then rerun the link test.

## Enforcement

`AgentContextLinkTests` is a required test in CI. Merging a PR with broken links is impossible once the CI is wired up (Phase 6 of the context system rollout).

## Anti-Patterns

- ❌ Adding a link to a file that does not yet exist "it's coming in the next PR" — split or land together.
- ❌ Using absolute repo-root paths like `/docs/X.md` in markdown (use relative paths).
- ❌ Updating only one reference after a rename.
- ❌ Running solution-level `dotnet test` for this check — use the architecture test project command above.
- ❌ Using VSTest-style `--filter` with TUnit projects — use whole-project runs or TUnit `--treenode-filter` only when locally verified.

## Related

- [`/check`](check.md)
- [`/review-pr`](review-pr.md)
- [`AGENTS.md`](../../AGENTS.md) §11 Enforcement.
- [`.claude/contract/README.md`](../contract/README.md) — why the contract depends on link integrity.
