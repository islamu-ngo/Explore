---
name: bootstrap
description: Zero-knowledge cold-start. Loads the Contribution Contract, classifies the current task against intents, and produces a ready-to-execute work plan with exact files to read, paths in scope, tests to run, and forbidden actions.
type: workflow
priority: critical
---
<!-- ABOUTME: Cold-start command for zero-knowledge agents and new contributors. -->
<!-- ABOUTME: The FIRST command any fresh session should run. Encodes the Contribution Contract workflow. -->

# /bootstrap — Cold-Start Contribution Workflow

> **Purpose:** Guide a fresh agent (or contributor) from zero knowledge to a safe, scoped change in one session.
> **Primary reference:** [`AGENTS.md`](../../AGENTS.md) — the Contribution Contract.

## Step 1 — Read the Contract (non-negotiable)

Open and internalize these files **in order** before any other action:

1. [`AGENTS.md`](../../AGENTS.md) — §1 Contribution Contract, §5 Critical Rules, §7 Verification Policy.
2. [`docs/QUICK_REFERENCE.md`](../../docs/QUICK_REFERENCE.md) — non-inferable technical invariants.
3. [`docs/index.md`](../../docs/index.md) — canonical navigation root.
4. [`.claude/contract/README.md`](../contract/README.md) — how the contract works.

Do NOT prefetch the entire docs folder. Progressive disclosure only.

## Step 2 — Classify the Request (pick ONE intent)

Open [`.claude/contract/intents.yaml`](../contract/intents.yaml). Scan the `triggers` field of each intent. The current task matches an intent when its verbs/nouns align with any trigger.

If none fits, stop and ask the user. Do NOT invent an intent or guess.

For each matched intent, record:

| Field | Value |
|---|---|
| Intent ID | `<from intents.yaml>` |
| must_read_docs | `<paths>` |
| load_skills | `<skill names>` |
| load_rules | `<rule file paths>` |
| paths_in_scope | `<glob list>` |
| paths_forbidden | `<glob list>` |
| minimum_tests | `<test project names>` |
| pr_checklist | `<short list>` |
| forbidden_without_approval | `<list>` |

## Step 3 — Load Exactly What the Intent Requires

For the selected intent:

1. Open every file in `must_read_docs`.
2. Open every skill in `load_skills` (at `.claude/skills/<name>/SKILL.md`).
3. Open every rule in `load_rules` (at `.claude/rules/<name>.md`).
4. Note every path listed in `paths_in_scope` and `paths_forbidden`.

**Do NOT open files outside this set** unless the loaded docs explicitly link to them.

## Step 4 — Establish the Baseline Green Build

Before making any edits:

```bash
dotnet build --configuration Release --verbosity quiet
```

If the build fails, STOP. Diagnose the failure first (see [`docs/TROUBLESHOOTING.md`](../../docs/TROUBLESHOOTING.md)). Do not add your own changes on top of a broken baseline.

## Step 5 — Produce a Work Plan

Write a short plan in the session (or to `dev/active/<task>/<task>-plan.md` if the task is non-trivial):

1. **Goal** — one sentence.
2. **Intent matched** — from Step 2.
3. **Change list** — files to add/modify, each inside `paths_in_scope`.
4. **Tests to run** — the intent's `minimum_tests` list.
5. **PR checklist** — from the intent's `pr_checklist`.
6. **Forbidden actions** — from the intent's `forbidden_without_approval`.

## Step 6 — Execute the Change

Apply edits strictly inside `paths_in_scope`. Follow the loaded rules. When in doubt:

1. Re-read the relevant `.claude/rules/*.md`.
2. Check the skill's `Top 5 Invariants`.
3. Check [`docs/QUICK_REFERENCE.md`](../../docs/QUICK_REFERENCE.md) for non-inferable constraints.

## Step 7 — Verify

Run the intent's `verification_commands`. Typically:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project <MinimumTestProject>/<MinimumTestProject>.csproj --configuration Release --verbosity quiet
```

For all verification cases, see [`/check`](check.md).

## Step 8 — Close Out

- Update docs listed in the intent's `docs_to_update` if behavior changed.
- If you learned something non-obvious during the work, append a finding via [`/finding`](finding.md).
- Open the PR using [`/review-pr`](review-pr.md).

## What This Command Does NOT Do

- Pick the intent for you — you classify.
- Run tests for you — see `/check`.
- Write code for you — this is the scaffolding, not the implementation.
- Commit or push — explicit user request required.

## Escalation

If any rule conflicts with the user request, or the task cannot match any intent in `intents.yaml`, STOP and ask the user. Never assume an exception.

## Related

- [`/check`](check.md) — run build + minimum tests.
- [`/finding`](finding.md) — log a durable finding.
- [`/review-pr`](review-pr.md) — PR checklist and review workflow.
- [`/docs-lint`](docs-lint.md) — verify documentation link integrity.
