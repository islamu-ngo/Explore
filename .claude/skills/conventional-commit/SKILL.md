---
name: conventional-commit
description: Conventional Commits format with monorepo-aware scope convention for consistent, parseable commit messages.
type: convention
enforcement: enforce
priority: high
---

ABOUTME: Conventional Commits convention with monorepo scope refinement.
ABOUTME: Scope uses project/module pattern for traceability across projects.

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

## When This Skill Activates

- Any commit operation (`git commit`, `git_add_or_commit`, `gitlens_commit_composer`)
- Keywords: commit, commit message, changelog, versioning
- When the `gitkraken-cli` skill is active (auto-loads this skill for formatting)

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
| `config` | Solution-level config, CI/CD, `.claude/` |

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

### Breaking Changes

Append `!` after the scope, and include a `BREAKING CHANGE:` footer:

```
feat(api/events)!: replace EventDto with EventResource

BREAKING CHANGE: EventDto removed. All consumers must use EventResource.
```

## Non-Inferable Rules (Must Follow)

1. **Type is always first** — `feat(api/events)` not `api feat(events)`.
2. **Scope is always `project/module`** — both segments required unless the change is project-wide (then just `project`).
3. **One logical change per commit** — don't mix a feature and a refactor.
4. **Description is imperative mood** — "add" not "added" or "adds".
5. **No trailing period** in the description line.
6. **50-char soft limit** on the first line (type + scope + description).
7. **72-char hard wrap** on body lines.
8. **`BREAKING CHANGE:` in footer** — not just the `!` suffix alone.
9. When a commit spans multiple projects, use the *primary* project as scope and mention others in the body.
10. **Merge commits and automated commits** are exempt from this format.

## Multi-Project Commits

If a change necessarily spans projects (e.g., adding a new API endpoint with its handler):

```
feat(api/taxonomy): add category endpoints

Handler and DTOs added in Application layer.
Repository method added in Persistence layer.
```

Use the outermost/entrypoint project as the scope. Prefer splitting into separate commits when possible.

## Related Documentation

- [Conventional Commits v1.0.0 Spec](https://www.conventionalcommits.org/en/v1.0.0/)
- `.claude/skills/gitkraken-cli/SKILL.md` — uses this skill for commit formatting
- `docs/CONTRIBUTING.md` — contributor workflow

**Enforcement Level**: ENFORCE
