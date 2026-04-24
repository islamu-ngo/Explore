---
name: docs-lint
description: Verify that every link inside .claude/**/*.md, AGENTS.md, CLAUDE.md, and docs/index.md resolves. Mirrors the AgentContextLinkTests CI check so you catch broken links before pushing.
type: verification
priority: high
---
<!-- ABOUTME: Documentation link lint command. Mirrors Event.Architecture.Tests.AgentContextLinkTests. -->
<!-- ABOUTME: Run before opening a PR to catch broken cross-references between docs, rules, skills, and agents. -->

# /docs-lint — Documentation Link Integrity

> **Enforced by:** `Event.Architecture.Tests.AgentContextLinkTests` (in CI).
> **This command:** a local pre-flight that catches the same class of failures before you push.

## When to Run

- After editing any `.claude/**/*.md` file.
- After editing `AGENTS.md`, `CLAUDE.md`, or `docs/index.md`.
- Before opening a PR (`/review-pr` runs this implicitly).

## What It Checks

The check treats the following files as link graph roots:

1. `AGENTS.md`
2. `CLAUDE.md`
3. `docs/index.md`
4. Every `.claude/**/*.md`
5. Every `docs/**/*.md`

For every Markdown link (bracketed text followed by a parenthesized relative target) and every relative reference, the check asserts the target exists on disk.

## How to Run It

### Option A — Authoritative (uses the CI check)

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter "FullyQualifiedName~AgentContextLinkTests"
```

This is the truth. If this passes, your links are valid.

### Option B — Quick local grep (not authoritative)

```bash
grep -r -o -E '\]\([^)]+\)' .claude/ AGENTS.md CLAUDE.md docs/index.md 2>/dev/null \
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
grep -r -l "OLD_NAME" .claude/ docs/ AGENTS.md CLAUDE.md dev/
```

Update each occurrence, then rerun the link test.

## Enforcement

`AgentContextLinkTests` is a required test in CI. Merging a PR with broken links is impossible once the CI is wired up (Phase 6 of the context system rollout).

## Anti-Patterns

- ❌ Adding a link to a file that does not yet exist "it's coming in the next PR" — split or land together.
- ❌ Using absolute repo-root paths like `/docs/X.md` in markdown (use relative paths).
- ❌ Updating only one reference after a rename.
- ❌ Running solution-level `dotnet test` to filter for this check — use `--project` + `--filter`.

## Related

- [`/check`](check.md)
- [`/review-pr`](review-pr.md)
- [`AGENTS.md`](../../AGENTS.md) §11 Enforcement.
- [`.claude/contract/README.md`](../contract/README.md) — why the contract depends on link integrity.
