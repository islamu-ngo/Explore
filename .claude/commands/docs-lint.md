---
name: docs-lint
description: Manually inspect documentation links after moving or renaming repository docs.
type: verification
priority: high
---
<!-- ABOUTME: Manual documentation link review command for repository doc moves and renames. -->
<!-- ABOUTME: Uses repository search to locate cross-references without involving the code test suite. -->

# /docs-lint — Documentation Quality

> **This command:** a manual pre-flight for documentation-only changes.

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

## How to Run It

### Search references

```bash
rg -o '\]\([^)]+\)' .claude/ AGENTS.md docs/index.md \
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

Use repository search to find every reference before renaming:

```bash
rg -l "OLD_NAME" .claude/ docs/ AGENTS.md dev/
```

Update each occurrence, then rerun the reference search.

## Anti-Patterns

- ❌ Adding a link to a file that does not yet exist "it's coming in the next PR" — split or land together.
- ❌ Using absolute repo-root paths like `/docs/X.md` in markdown (use relative paths).
- ❌ Updating only one reference after a rename.

## Related

- [`/check`](check.md)
- [`/review-pr`](review-pr.md)
- [`AGENTS.md`](../../AGENTS.md) §10 Tool-Specific Bootloaders.
- [`.claude/contract/README.md`](../contract/README.md) — why the contract depends on link integrity.
