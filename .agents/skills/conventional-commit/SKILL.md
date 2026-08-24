---
name: conventional-commit
description: "Load when asked to write, split, squash, or review git commit messages, Conventional Commits, changelog subjects, or release-note entries from a diff; not for implementing the underlying change."
type: guardrail
enforcement: block
priority: high
---
<!-- ABOUTME: Changelog-first Conventional Commits policy for reader-friendly release notes. -->
<!-- ABOUTME: Groups each commit by releasable outcome instead of code layer or file type. -->

# Conventional Commits

## Rules

- Compose commits only after implementation, tests, and required docs are complete.
- One commit is one releasable vertical outcome, including its code, tests, docs, migration, and generated artifacts.
- Split when one release-note bullet and one rollback decision cannot describe every staged line.
- Use a scope from [scope registry](../../../eng/release/policy/scope-registry.yaml); scopes describe product capability or approved engineering concern, never code layer/file type.
- The subject states user/operator benefit and remains understandable without the body.
- Breaking changes require `!`, a non-empty `BREAKING CHANGE:` footer, and cannot use `Changelog: skip`.
- High-impact, breaking, migration, or security work requires its governed change fragment and matching `Change-Id` footer.
- Internal nonbreaking work uses both `Changelog: skip` and a non-empty `Changelog-Reason`.
- A backport records the original commit in its fragment's `Backport-Of` field as a full object ID; the commit itself is an ordinary commit on the target line. Never restate the original commit's identity in the subject.
- The release-metadata commit `B` is the one commit whose terminal footers must be exactly `Changelog: skip` and `Changelog-Reason: release metadata commit`, so the generated notes do not include themselves.
- Never rewrite published history without explicit approval. A squash, rebase, or merge that replaces a prepared `B` produces a different object and invalidates its candidate attestation.

## Format

```text
type(scope): benefit-led subject

optional context explaining what changed and why

optional BREAKING CHANGE, Change-Id, Refs/Closes, or co-author trailers
```

| Type | Meaning |
|---|---|
| `feat` | New user-visible capability |
| `fix` | Restored expected behavior |
| `perf` | Proven user/operator-visible efficiency improvement |
| `revert` | Intentional reversal with resulting behavior stated |
| `docs` | Documentation-only outcome |
| `test/refactor/style/build/ci/chore` | Internal outcome; normally skipped from public notes |

Examples:

```text
feat(registration): let attendees correct submitted details
fix(events): keep drafts private until organizers publish them
perf(discovery): return event search results faster
feat(registration)!: simplify attendee check-in credentials

BREAKING CHANGE: Integrations must send credential instead of ticketCode.
```

Reject layer-oriented subjects such as `feat(api): add endpoint` or `fix(persistence): update filter`.

## Workflow

1. Inspect the complete diff and verification state.
2. Group files by releasable outcome, not directory.
3. Select type and canonical scope from release policy.
4. Add required change fragment/trailers.
5. Preview the subjects as release-note bullets and split any mixed outcome.
6. Stage exact groups and show the proposed commits before creating them unless the user already authorized commit creation.

## Resources

- [Release policy](../../../docs/RELEASE_POLICY.md)
- [Release policy schema](../../../eng/release/policy/release-policy.yaml)
- [Conventional Commits 1.0](https://www.conventionalcommits.org/en/v1.0.0/)

## Verification

- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- `git log --format='- %s' "$(git merge-base HEAD origin/develop)"..HEAD`
- Confirm every visible subject is plain-language, every skipped commit has both trailers, and every breaking commit has the required footer.
