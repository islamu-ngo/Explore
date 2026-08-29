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

## Invariants & Rules

1. **Releasable Vertical Slices**: One commit is one complete vertical outcome. Bundle domain, application, persistence, API, Blazor UI, generated clients (`EventApiClient.g.cs`), schemas (`openapi_islamu-event.json`), migrations, tests, and docs together.
2. **Never Commit Orphaned Generated Files**: Never isolate generated clients or schemas into a standalone commit when they belong to an underlying contract or feature change.
3. **No Layer Scopes**: Scopes describe product capability or approved engineering concerns—never code layers or folders (`api`, `domain`, `persistence`, `blazor`, `client`, `dto` are forbidden).
4. **Subject Quality**: Subject line must state user/operator benefit, use imperative mood, and remain clear without the body.
5. **Breaking Changes**: Require `!`, non-empty `BREAKING CHANGE:` footer, and must never use `Changelog: skip`.
6. **Internal Nonbreaking Work**: Commits of type `test`, `build`, `ci`, `refactor`, `style`, or internal `docs`/`fix` must carry both `Changelog: skip` and non-empty `Changelog-Reason: <reason>`.
7. **Safe Staging**: Never use blind `git add .` or `git commit -a` on mixed working trees. Explicitly name staged files per atomic commit.
8. **Release Metadata Commit**: Commit `B` is the sole commit whose terminal footers must be exactly `Changelog: skip` and `Changelog-Reason: release metadata commit`.

## Canonical Scope Registry

| Category | Allowed Scopes | Description |
|---|---|---|
| **Public** | `events`, `registration`, `ticketing`, `discovery`, `notifications`, `privacy`, `access`, `storage`, `onboarding`, `federation`, `webhooks`, `localization`, `accessibility`, `self-hosting` | User/operator capabilities (in release notes). |
| **Engineering** | `ci`, `dependencies`, `architecture`, `database`, `observability`, `documentation`, `release`, `testing`, `build` | Codebase health, build, testing, dev tooling. |

## File Clustering (Atomic Slicing)

Sort dirty working trees using this priority order:

1. **Vertical Feature/Fix Slice**: Domain + App + Persistence + UI + Generated Artifacts + Tests + Docs.
2. **Technical/Resilience Fix**: Independent database execution strategies, retries, or middleware.
3. **Test Suite Hardening**: Test fixtures, schema isolation (`current_schema()`), characterization models.
4. **Build & Package Config**: Central props (`Directory.Build.props`), lockfiles, CI pipelines.
5. **Governance & Legal Docs**: `CLA.md`, `CONTRIBUTING.md`, `README.md`, ADRs.
6. **Active Task Tracking**: Task plans, progress logs (`dev/active/*`).

## Format & Non-Interactive CLI Recipes

```text
type(scope): benefit-led subject

Optional description explaining motivation and data flow.

Changelog: skip
Changelog-Reason: concise explanation of why commit is excluded from public release notes
```

| Type | Meaning |
|---|---|
| `feat` | New user-visible capability |
| `fix` | Restored expected behavior |
| `perf` | Proven efficiency improvement |
| `revert` | Intentional reversal |
| `docs` | Documentation-only outcome |
| `test/refactor/style/build/ci/chore` | Internal outcome (normally skipped from public notes) |

### CLI Recipe

```bash
# 1. Stage exact files for one atomic outcome
git add path/to/File1.cs path/to/File2.razor path/to/GeneratedClient.g.cs

# 2. Commit non-interactively with separate -m arguments
git commit -m "feat(registration): present tenant-branded intermediary disclaimer on paid events" \
           -m "Format canonical directory notice dynamically based on tenant branding."

# 3. For internal nonbreaking commits, append skip trailers:
git commit -m "fix(database): wrap session projection rebuilds in db execution strategy" \
           -m "Execute projection rebuilds within execution strategies to withstand retries." \
           -m "Changelog: skip" \
           -m "Changelog-Reason: internal projection resilience enhancement"
```

## Anti-Pattern Catalog

| ❌ Anti-Pattern | ✅ Best Practice | Why |
|---|---|---|
| `feat(api): add public disclaimer` | `feat(registration): present disclaimer on paid events` | Layer scope rejected; scope must be capability. |
| `fix(persistence): use execution strategy` | `fix(database): wrap session projection rebuilds in db execution strategy` | `persistence` is a layer; use approved `database` scope. |
| `chore: update openapi.json and client` | *[Bundle inside originating feature commit]* | Never split generated client code from triggering feature. |
| `docs: fix readme and cla` | `docs(documentation): clarify legal entity status` | Explicit engineering scope and benefit-led subject. |
| `test: update characterization tests` | `test(testing): harden persistence integration tests` | Descriptive subject and canonical scope. |

## Resources

- [Release policy](../../../docs/RELEASE_POLICY.md)
- [Release policy schema](../../../eng/release/policy/release-policy.yaml)
- [Scope registry](../../../eng/release/policy/scope-registry.yaml)
- [Conventional Commits 1.0](https://www.conventionalcommits.org/en/v1.0.0/)

## Verification

- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/AgentContextPolicyTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
- `git log --format='- %s' "$(git merge-base HEAD origin/develop)"..HEAD`
- Confirm every visible subject is plain-language, every skipped commit has both trailers, and every breaking commit has the required footer.
