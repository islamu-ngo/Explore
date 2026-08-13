---
name: conventional-commit
description: Write and group Conventional Commits as clear, user-facing release-note entries.
type: guardrail
enforcement: block
priority: high
---
<!-- ABOUTME: Changelog-first Conventional Commits policy for reader-friendly release notes. -->
<!-- ABOUTME: Groups commits by product outcome instead of code layer or file type. -->

## Purpose

Make release notes useful by treating every release-visible commit subject as
a reader-facing entry. Preserve Conventional Commits parsing while grouping
all work for one product outcome into one coherent commit.

## When to Load

- Before staging, composing, amending, squashing, or creating a commit.
- When splitting a working tree, ticket, pull request, or feature into commits.
- When reviewing commit messages, release notes, versioning, or changelog output.
- When `gitkraken-cli` or another commit-composition tool is active.
- Keywords: commit, commit message, staging, changelog, release note, versioning.

## When NOT to Load

- Not for choosing product requirements or deciding whether a behavior should ship.
- Not for branch naming unless the branch will also be converted into commit text.
- Not for merge commits or automation-generated commits outside contributor control.
- Not for rewriting published history without explicit user approval.
- Not as permission to combine unrelated outcomes merely because they share a ticket.

## Must-Read Docs

- [Contribution Contract](../../../AGENTS.md)
- [Conventional Commits v1.0.0](https://www.conventionalcommits.org/en/v1.0.0/)
- [Release Checklist](../../../docs/RELEASE_CHECKLIST.md)

## Top 5 Invariants

1. A release-visible commit is intentionally included in release notes; repository visibility alone does not make it release-visible, and the current manual release process makes that decision during release curation.
2. Scope uses the narrowest existing canonical product capability and never names a code layer, project, file type, or ticket ID.
3. One commit represents one releasable outcome and includes all directly supporting code, tests, documentation, migrations, and generated artifacts across layers when they share that outcome.
4. Descriptions use lowercase imperative language, lead with the benefit or restored behavior, avoid implementation mechanics and hype, and have no trailing period.
5. Every breaking commit uses both `!` after its type or scope and a `BREAKING CHANGE:` footer that states the required reader action.

## Top 5 Anti-Patterns

1. **Layer split:** Separate API, Application, Persistence, and UI commits turn one feature into duplicate changelog entries.
2. **File-type split:** Separate test, documentation, migration, or generated-code commits clutter history when those files only support the same outcome.
3. **Ticket dump:** One commit for every change under a broad ticket hides distinct user outcomes behind an internal planning boundary.
4. **Implementation subject:** Messages such as `add endpoint`, `update handler`, or `fix query filter` tell users how the code changed instead of what improves for them.
5. **Marketing fluff:** Claims such as `revolutionize`, `seamless`, `best`, or `massively faster` make changelogs untrustworthy unless the exact claim is proven.

## Minimal Examples

Use this format:

```text
type(scope): benefit-led release note

[optional user-facing context: what changed and why]

[optional BREAKING CHANGE, Refs/Closes, or co-author footer]
```

Choose the type from the released outcome, not the files changed:

| Type | Changelog meaning |
|---|---|
| `feat` | A new capability or meaningful improvement users can notice. |
| `fix` | Expected behavior is restored or users are protected from a defect. |
| `perf` | A noticeable speed, capacity, or resource-use improvement. |
| `revert` | A released behavior is intentionally undone; state the resulting experience. |
| `docs` | Documentation-only work; omit it by default, but manually include it when changed instructions alter a user or operator action. |
| `test`, `refactor`, `style`, `build`, `ci`, `chore` | Internal-only work with no product outcome; omit it from release notes unless marked as breaking. |

Do not relabel a documentation-only correction as `fix` unless runtime behavior
also changed. The repository currently curates semantic-version tags and GitHub
Release notes manually; Conventional Commits inform that process but do not
publish or version a release automatically.

Use this canonical initial scope vocabulary:

```text
events          registration    ticketing       discovery
notifications   privacy         access          storage
onboarding       federation      webhooks        localization
accessibility   self-hosting
```

Reuse the narrowest matching scope. Introduce a new product-capability scope
only when none fits; do not create aliases or layer-qualified variants such as
`api/registration`, `blazor/events`, or `persistence/privacy`.

Prefer outcome-led subjects:

```text
feat(registration): let attendees correct their registration details
fix(events): keep draft events private until organizers publish them
perf(discovery): show event search results faster
fix(self-hosting): preserve custom email settings during upgrades
```

Reject implementation-led equivalents:

```text
feat(api/registration): add PATCH endpoint
fix(persistence/events): correct query filter
perf(persistence/queries): add covering index
fix(config): update email migration
```

Keep one vertical outcome together:

```text
feat(registration): let attendees correct their registration details

Allow attendees to fix submitted details before the registration deadline so
organizers receive accurate information without manual support.

Closes #482
```

That commit may include the UI, API contract, handler, persistence, tests, and
user documentation. Do not emit separate `feat(api)`, `feat(blazor)`, `test`,
and `docs` commits for those supporting parts.

Split only when a ticket contains independently releasable outcomes:

```text
feat(registration): let attendees correct their registration details

Refs #482

fix(notifications): confirm registration changes immediately

Closes #482
```

Use the grouping test: if one release-note bullet and one product rollback
decision cannot accurately describe every staged line, split the commit by
outcome. File count and project boundaries are warning signals, never grouping
rules.

For breaking changes, explain the reader action rather than only the technical
contract change:

```text
feat(registration)!: simplify attendee check-in credentials

BREAKING CHANGE: Check-in integrations must send `credential` instead of
`ticketCode` after upgrading.
```

The subject must remain understandable when the changelog tool omits the body.
Prefer clarity over the old 50-character convention; keep the full subject
within 100 characters when practical and wrap body lines at 72 characters.

## Verification Hooks

- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- `git diff --check -- .agents/skills/conventional-commit/SKILL.md`
- `git log --format='- %s' "$(git merge-base HEAD origin/develop)"..HEAD`
- Manual: curate the Git subject preview using `docs/RELEASE_CHECKLIST.md`; confirm one plain-language entry per releasable outcome, include release-relevant `docs` deliberately, and exclude layer/file-type noise.

## Related Skills

- [GitKraken CLI](../gitkraken-cli/SKILL.md)
- [PR Review](../review-pr/SKILL.md)
