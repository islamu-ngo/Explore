---
name: "review-pr"
description: "Execute the PR review checklist for the current change. Ties intent classification to evidence - the PR may not merge unless each checklist item has a verifiable answer."
---

# review-pr

## Command Template

<!-- ABOUTME: Pre-PR and PR review checklist driven by the matched intent. -->
<!-- ABOUTME: Verifies paths in scope, tests, docs, rules, and forbidden actions before opening or approving a PR. -->

# /review-pr — Pull Request Review Checklist

> **Primary reference:** [`.claude/contract/intents.yaml`](../../../.claude/contract/intents.yaml) — every intent owns its `pr_checklist`.
> **Authority:** [`AGENTS.md`](../../../AGENTS.md) §5 CRITICAL RULES.

## When to Run

- **Self-review** — before opening a PR.
- **Review mode** — when asked to review someone else's PR.

## Step 1 — Re-Identify the Intent

Which intent in [`intents.yaml`](../../../.claude/contract/intents.yaml) does this PR implement? If the PR spans multiple intents, review each in turn and merge the checklists.

## Step 2 — Execute the Intent's PR Checklist

For each item in the intent's `pr_checklist`, collect evidence:

| Checklist Item | Evidence Type |
|---|---|
| Tests added / updated | File paths + `dotnet test` output |
| Docs updated | File paths + diff |
| Rule adherence | Lint / architecture test output |
| Authorization correct | `AuthorizationParityTests` output |
| Contract unchanged (or CHANGELOG entry) | `ApiContractArchitectureTests` output + `docs/API_CHANGELOG.md` diff |

Any item without evidence is a blocker.

## Step 3 — Scope Discipline

Run this mental check on the diff:

1. Is every changed file inside the intent's `paths_in_scope`?
2. Is no changed file inside the intent's `paths_forbidden`?
3. Are the changes minimal (not bundling unrelated refactors)?

If any answer is "no", split the PR or escalate to the user.

## Step 4 — Verification Evidence

Paste the exact commands and their outputs:

```bash
dotnet build --configuration Release --verbosity quiet
# Expected: exit 0

dotnet test --project <MinimumTestProject>/<MinimumTestProject>.csproj --configuration Release --verbosity quiet
# Expected: Passed
```

For architectural PRs, also run:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## Step 5 — Critical Rules Checklist (applies to every PR)

- [ ] No `as any`, `@ts-ignore`, `@ts-expect-error`, or equivalent C# type-suppression hack.
- [ ] No new `rm`, `mv`, or `>` shell redirection in scripts.
- [ ] No `.env`, credentials, or secrets staged.
- [ ] No backward-compatibility shims or feature flags (active development, break-and-fix).
- [ ] Every new / modified file starts with a two-line `ABOUTME:` comment header.
- [ ] No duplicated content across docs, skills, or agents (point — don't copy).

## Step 6 — Documentation Sanity

- [ ] `docs/QUICK_REFERENCE.md` updated if a new invariant was introduced.
- [ ] `docs/API_CHANGELOG.md` updated if a public contract changed.
- [ ] `dev/_journal/journal.md` appended if a non-obvious finding emerged.
- [ ] `docs/index.md` cross-references still resolve (run `/docs-lint`).

## Step 7 — Forbidden-Without-Approval Gate

Cross-check the diff against the intent's `forbidden_without_approval` list. If any item triggers, the PR MUST carry explicit approval in its description or be rejected.

## Step 8 — Output Contract

Produce a summary the reviewer can paste into the PR:

```
Intent: <intent-id>
Files changed: <count> (all in scope ✔)
Build: ✔
Tests: <project list> — all green
Docs: <files> updated
Forbidden actions: none triggered
Outstanding questions: <list or None>
```

## When Review MUST Fail

- Any checklist item lacks evidence.
- Any file is outside `paths_in_scope` or inside `paths_forbidden`.
- Any `forbidden_without_approval` item triggered without approval.
- Any architecture test (`Event.Architecture.Tests.*`) fails.
- Any critical rule violated.

## Related

- [`AGENTS.md`](../../../AGENTS.md) — get to a valid PR state.
- [`/check`](../../../.claude/commands/check.md) — run the verification commands.
- [`/docs-lint`](../../../.claude/commands/docs-lint.md) — check doc link integrity.
- [`AGENTS.md`](../../../AGENTS.md) — Contribution Contract authority.
