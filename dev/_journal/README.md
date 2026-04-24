<!-- ABOUTME: Journal entrypoint: how to record, read, and promote durable findings for ISLAMU Event. -->
<!-- ABOUTME: Complements dev/_journal/journal.md + MAJOR_DECISIONS.md with operational policy. -->

# Dev Journal

> **Purpose**: Capture durable findings — bug root causes, non-obvious patterns, system quirks, and major decisions — that should outlive a single session.
>
> **Not for**: Short-term TODOs, tentative ideas, or anything you would not want to read again in six months.

Last Updated: 2026-04-24

---

## 1. When to Record a Finding

Open `journal.md` and append an entry when you:

1. Discover a **non-obvious behavior** (e.g., middleware ordering that breaks rate limiting, EF Core soft-delete interceptor behavior, MudBlazor v9 runtime quirks).
2. Fix a **bug whose root cause is not visible from the fix alone** (the "what you really needed to know" insight).
3. Make a **design decision** that future contributors will want to understand, but that is not yet mature enough to promote to `docs/GOVERNANCE.md`.
4. Confirm or refute an **assumption from a skill/agent/rule** that was ambiguous.

If your finding is a **system-wide decision** (changes layer boundaries, affects all tenants, sets a new invariant), record it in `MAJOR_DECISIONS.md` instead.

---

## 2. How to Record a Finding

Append to [`journal.md`](journal.md) using [`FINDING_TEMPLATE.md`](FINDING_TEMPLATE.md). Enforcement checks:

- Date prefix in the form `[YYYY-MM-DD Europe/Brussels]`.
- One blank line between entries.
- At least one referenced file path or commit SHA.
- No speculation: findings must be grounded in something testable or observed.

```bash
# Append a new finding
cat dev/_journal/FINDING_TEMPLATE.md >> dev/_journal/journal.md
```

Then edit the appended block to fill it in. Commit alongside the change it describes.

---

## 3. Promotion Rules (Journal → Canonical Docs)

A finding is not meant to live in the journal forever. See [`PROMOTION_RULES.md`](PROMOTION_RULES.md) for the promotion policy.

At a glance:

| Signal | Promote To |
|---|---|
| Rule that applies to new code going forward | `docs/QUICK_REFERENCE.md` (non-inferable rule) OR new `.claude/rules/*.md` entry |
| Pattern that teaches a way of working | `.claude/skills/*/SKILL.md` |
| Architectural decision | `docs/MAJOR_DECISIONS.md` + ADR under `docs/adr/` |
| Discovery that informs governance / conventions | `docs/GOVERNANCE.md` |
| One-off debugging lesson | **Stays in journal** — no promotion needed |

The goal: the journal should shrink as a fraction of active findings over time, because stable findings graduate.

---

## 4. Reading the Journal Effectively

When investigating a problem, **search the journal before re-researching**. Most "weird" behaviors are documented.

```bash
# Example: find anything about tenant resolution
grep -n -i "tenant" dev/_journal/journal.md

# Example: find findings from the last month
grep -n "^\[2026-04" dev/_journal/journal.md
```

Cold-start agents are instructed to do this via `/bootstrap` and `AGENTS.md §8`.

---

## 5. Housekeeping

- **Do not** delete old findings. They are durable evidence, even when superseded.
- **Do** append a follow-up finding referencing the original (`Related: 2026-03-12 entry`).
- **Do** promote a finding when it meets promotion criteria, then leave the journal entry as a historical record with a `Promoted → docs/QUICK_REFERENCE.md` annotation.

---

## 6. Related

- [`journal.md`](journal.md) — chronological findings log.
- [`MAJOR_DECISIONS.md`](MAJOR_DECISIONS.md) — system-wide decisions.
- [`FINDING_TEMPLATE.md`](FINDING_TEMPLATE.md) — canonical entry format.
- [`PROMOTION_RULES.md`](PROMOTION_RULES.md) — journal-to-docs promotion policy.
- [`AGENTS.md`](../../AGENTS.md) §8 — Memory & Findings (high-level).
