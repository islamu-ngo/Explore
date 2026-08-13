ABOUTME: Root of the Contribution Contract — the machine-readable routing layer that maps intents to context.
ABOUTME: Any AI agent or contributor starts HERE, classifies the change, then loads only the files the intent requires.

# Contribution Contract

The **Contribution Contract** is this repository's operating model for AI-assisted contribution. It answers — deterministically, before any code is written — the eight questions every safe change must resolve:

| # | Question | Source of Truth |
|---|---|---|
| 1 | What kind of change is this? | `intents.yaml` → matched `id` |
| 2 | Which rules are authoritative? | `intents.yaml` → `load_rules` + `docs/QUICK_REFERENCE.md` + `docs/GOVERNANCE.md` |
| 3 | Which files must be read first? | `intents.yaml` → `must_read_docs` + `load_skills` |
| 4 | Which files may be changed? | `intents.yaml` → `paths_in_scope` (allow-list) |
| 5 | Which tests must run at minimum? | `intents.yaml` → `minimum_tests` |
| 6 | Which docs must be updated? | `intents.yaml` → `docs_to_update` |
| 7 | Which PR checklist applies? | `intents.yaml` → `pr_checklist` |
| 8 | Which things are forbidden without explicit approval? | `intents.yaml` → `forbidden_without_approval` + root `AGENTS.md` critical rules |

## Files

| File | Purpose |
|---|---|
| `intents.yaml` | The canonical intent catalog. Each entry is a machine-readable contract for one category of change. |
| `schema.json` | JSON Schema describing the structure of `intents.yaml` for compatible editors and tools. |
| `README.md` | This file. Human-facing explanation and usage. |

## How to use this contract (AI agent or contributor)

1. **Classify**. Read the incoming request (issue, bug, PR description, user message). Match it against `intents.yaml` using the `triggers` field. If no match, either (a) request a clarifying question from the user, or (b) fall back to `AGENTS.md` generic flow.
2. **Load**. Read every file in `must_read_docs`, activate every skill in `load_skills`, load every rule file in `load_rules`. **Do not load anything else unless absolutely required.**
3. **Edit**. Touch only files matching `paths_in_scope`. Reject changes proposed outside the allow-list unless the user explicitly widens scope.
4. **Verify**. Run every command in `verification_commands`. Run every test project in `minimum_tests`. Satisfy every item in `pr_checklist`. Update every doc in `docs_to_update` in the same PR.
5. **Escalate**. If any item in `forbidden_without_approval` is required for the change, stop and ask the user. Do not infer consent.

## Adding a new intent

1. Add a new entry to `intents.yaml`.
2. Validate the entry against `schema.json` with a compatible editor or schema tool.
3. Link the new intent from `docs/GOVERNANCE.md` → "Decision Framework" if it introduces a new decision point.
4. Exercise the new intent with at least one scenario in `.agents/benchmarks/cold-start-tasks.yaml` before it is considered production-ready.

## Adding a new field to the schema

1. Update `schema.json` with the new property (`required` if it is mandatory).
2. Backfill every existing intent in `intents.yaml` with the new field.
3. Update this README's table if the field maps to one of the eight contract questions.

## What this contract does **not** do

| Out-of-scope | Where to look instead |
|---|---|
| Describe the architecture | `docs/ARCHITECTURE.md`, `docs/GOVERNANCE.md` |
| Enumerate every rule | `docs/QUICK_REFERENCE.md` |
| Tell you how to *implement* a pattern | `.agents/skills/*/SKILL.md` |
| Handle totally novel changes | Fall back to `AGENTS.md` and ask the user |

## Related

- `AGENTS.md` — tool-neutral root entrypoint for any AI agent or contributor
- `AGENTS.md` — Claude-specific bootloader shim
- `docs/index.md` — canonical navigation root for the docs tree
- `.agents/rules/` — path-scoped rule files, referenced from `intents.yaml`
- `.agents/benchmarks/cold-start-tasks.yaml` — evaluation harness validating cold-start agent performance against these intents
