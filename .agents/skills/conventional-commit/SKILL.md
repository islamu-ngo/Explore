---
name: conventional-commit
description: Conventional Commits format plus atomic, monorepo-aware commit composition rules.
type: convention
enforcement: enforce
priority: high
---

ABOUTME: Conventional Commits convention with monorepo scope refinement.
ABOUTME: Enforces atomic, intent-matched commit composition and messages.

# Conventional Commits

> **Monorepo-Aware Commit Convention**
>
> Based on [Conventional Commits v1.0.0](https://www.conventionalcommits.org/en/v1.0.0/).
> Extends the spec with a project-scoped convention for this monorepo.

## Purpose

Enforce a consistent, parseable commit message format that:
- Makes git history scannable per project and module
- Enables automated changelog generation
- Communicates intent (feature, fix, refactor, etc.) at a glance
- Prevents oversized mixed-purpose commits by requiring atomic, fully related changes
- Defaults commit work to multiple atomic commits rather than a single large catch-all commit

## When This Skill Activates

- Any commit operation (`git commit`, `git_add_or_commit`, `gitlens_commit_composer`)
- Keywords: commit, commit message, staging, changelog, versioning, split commits
- When the `gitkraken-cli` skill is active (auto-loads this skill for composition and formatting)

## Commit Message Format

```
type(project/module): description

[optional body]

[optional footer(s)]
```

### Structure Rules

1. **`type` is always the first word** — no project prefix before the type.
2. **Scope** is `(project/module)` — slash-separated, in parentheses.
3. **Description** starts lowercase, no trailing period, imperative mood.
4. **Body** (optional) explains *what* and *why*, not *how*. Wrap at 72 chars.
5. **Footer** (optional) for `BREAKING CHANGE:`, issue refs (`Closes #123`), co-authors.

### Types

| Type | When to Use |
|---|---|
| `feat` | New feature or capability |
| `fix` | Bug fix |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `docs` | Documentation only |
| `test` | Adding or correcting tests |
| `chore` | Build, CI, tooling, dependency updates |
| `style` | Formatting, whitespace (no logic change) |
| `perf` | Performance improvement |
| `ci` | CI/CD pipeline changes |
| `build` | Build system or external dependency changes |
| `revert` | Reverts a previous commit |

### Project Scopes

The first segment of scope identifies the project boundary:

| Scope Prefix | Maps To |
|---|---|
| `api` | `Explore.API/` — REST endpoints, middleware, filters |
| `blazor` | `Explore.Blazor/` + `Explore.Blazor.Client/` — UI layer |
| `app` | `Explore.Application/` — CQRS handlers, DTOs, validators |
| `domain` | `Explore.Domain/` — entities, value objects, invariants |
| `persistence` | `Explore.Persistence/` — EF Core, migrations, repositories |
| `infra` | `Explore.Infrastructure/` — external services, email, storage |
| `apphost` | `Explore.AppHost/` — Aspire orchestration |
| `test` | All test projects |
| `docs` | Documentation, skills, agents |
| `config` | Solution-level config, CI/CD, `.Codex/` |

The second segment (after `/`) is the module or feature area. Keep it short — a noun, not a sentence.

### Examples

```
feat(api/taxonomy): implement category CRUD endpoints
fix(persistence/events): correct soft-delete filter on EventQuery
refactor(app/rsvp): extract validation into specification
docs(config/skills): add conventional-commit skill
test(api/events): add integration tests for event creation
chore(apphost): upgrade Aspire SDK to 9.2
style(blazor/components): fix inconsistent BEM class names
perf(persistence/queries): add covering index for event listing
```

## Commit Composition Rules

Commit composition is part of this skill, not a tool-specific workflow. Before
staging or committing, split the working tree by **intent**, **scope**, and
**work unit**. The question is not "can these files compile together?" but
"would reverting this commit revert exactly one coherent change?"

Default expectation: when a working tree contains more than one independently
understandable change, the result should be **multiple atomic commits**. A
single large commit that regroups everything is a failure unless the changes
are demonstrably one indivisible work unit.

### Atomicity Gate

Every commit must pass all of these checks:

1. **One intent only** — feature, fix, refactor, docs, tests, config, or chore.
2. **One work unit only** — files all serve the same user-visible or technical outcome.
3. **Fully related files only** — no "while I was here" edits, opportunistic cleanup, unrelated formatting, or nearby fixes.
4. **Same reason to change** — every file should answer the same "why is this in the commit?" question.
5. **Revert-safe** — reverting the commit should not remove unrelated behavior or leave unrelated work half-applied.
6. **Reviewable size** — prefer small commits; if the file list feels broad, split unless there is a concrete dependency.

### File-Count Guidance

- Treat **10–15 files per commit** as the practical upper warning limit.
- Prefer commits with fewer files whenever the change can still be understood and reverted cleanly.
- If a commit would exceed 15 files, stop and ask whether the work can be split into smaller logical steps.
- Large file counts are acceptable only when the files are all required for one indivisible work unit.
- Typical exceptions include:
  - coordinated renames/refactors that must touch many references;
  - dependency updates or lockfile regeneration;
  - generated/scaffolded boilerplate or codegen output;
  - one vertical slice that is genuinely inseparable across layers.
- If a commit has to rely on an exception, explain that in the commit body so reviewers understand why the file count is large.

### Split Triggers

Split into separate commits whenever any of these are true:

- The change touches unrelated feature areas, modules, tenants, pages, handlers, or services.
- Code and tests cover multiple behaviors that can be understood independently.
- Documentation describes a different change than the code implements.
- Formatting or mechanical cleanup appears alongside behavior changes.
- Generated files, migrations, snapshots, or lockfiles accompany source changes that can be isolated.
- A single commit would include files whose best commit types differ (`feat` + `fix`, `refactor` + `docs`, etc.).
- The commit would need "and" in the subject to describe it accurately.

Large file count is a warning sign, not an automatic failure. A broad commit is
allowed only when every file is required for the same indivisible work unit
(for example, a coordinated rename or generated migration plus its model
change). Otherwise, split it.

### Staging Discipline

1. Inspect the full working tree before staging.
2. Group files by shared intent and same work unit.
3. Plan for **multiple commits by default**, not one final umbrella commit.
4. Stage only the files for the next atomic commit.
5. Re-check staged diff before committing.
6. Commit that slice, then repeat until each remaining group is independently understandable.

Never stage all changes merely because they are present. Never use an AI commit
composer as permission to create a mixed commit; its output must still satisfy
the Atomicity Gate above.

### Commit Grouping Examples

Prefer these splits:

```text
feat(api/events): add event publish endpoint
test(api/events): cover event publish authorization
docs(api/events): document publish endpoint behavior
```

Over this mixed commit:

```text
feat(api/events): add publish endpoint and tests and docs
```

Prefer these splits:

```text
refactor(app/organizations): extract membership policy
fix(blazor/organizations): preserve selected organization filter
```

Over this partially related commit:

```text
refactor(app/organizations): update organization handling
```

### Breaking Changes

Append `!` after the scope, and include a `BREAKING CHANGE:` footer:

```
feat(api/events)!: replace EventDto with EventResource

BREAKING CHANGE: EventDto removed. All consumers must use EventResource.
```

## Non-Inferable Rules (Must Follow)

1. **Type is always first** — `feat(api/events)` not `api feat(events)`.
2. **Scope is always `project/module`** — both segments required unless the change is project-wide (then just `project`).
3. **One atomic work unit per commit** — don't mix a feature and a refactor, a bug fix and cleanup, or partially related files.
4. **Description is imperative mood** — "add" not "added" or "adds".
5. **No trailing period** in the description line.
6. **50-char soft limit** on the first line (type + scope + description).
7. **72-char hard wrap** on body lines.
8. **`BREAKING CHANGE:` in footer** — not just the `!` suffix alone.
9. When a commit spans multiple projects, use the *primary* project as scope and mention others in the body.
10. **Never commit the entire working tree by default** — explicitly select the atomic file group being committed.
11. **Multiple atomic commits are the default outcome** whenever the worktree contains more than one separable change.
12. **Merge commits and automated commits** are exempt from this format.

## Multi-Project Commits

If a change necessarily spans projects (e.g., adding a new API endpoint with its handler):

```
feat(api/taxonomy): add category endpoints

Handler and DTOs added in Application layer.
Repository method added in Persistence layer.
```

Use the outermost/entrypoint project as the scope. Prefer splitting into separate commits when possible, but do not split a single indivisible vertical slice merely to satisfy project boundaries.

## Related Documentation

- [Conventional Commits v1.0.0 Spec](https://www.conventionalcommits.org/en/v1.0.0/)
- `.Codex/skills/gitkraken-cli/SKILL.md` — uses this skill for commit composition and formatting
- `docs/CONTRIBUTING.md` — contributor workflow

**Enforcement Level**: ENFORCE
