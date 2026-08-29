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

1. **Releasable Vertical Slices**: One commit is one complete vertical outcome. Bundle domain, app, persistence, API, Blazor UI, generated clients (`EventApiClient.g.cs`), schemas (`openapi_islamu-event.json`), migrations, tests, and docs together.
2. **No Orphaned Generated Code**: Generated clients/schemas must travel in the commit that triggered them.
3. **No Layer Scopes**: Scopes describe capability/engineering concern—never code layers (`api`, `domain`, `persistence`, `blazor`, `client`, `dto` are forbidden).
4. **Cross-Domain Precedence**: When a feature spans domains, select the primary initiating capability (`registration`).
5. **Subject Quality**: State user/operator benefit in imperative mood; clear without reading the body.
6. **Breaking Work & Change-Id**: Breaking changes require `!` and `BREAKING CHANGE:` footer. Governed security/migration work requires its change fragment and matching `Change-Id: CHG-...` footer.
7. **Internal Nonbreaking Work**: Commits of type `test`, `build`, `ci`, `refactor`, `style`, or internal `docs`/`fix` must carry both `Changelog: skip` and non-empty `Changelog-Reason: <reason>`.
8. **Safe Staging**: Never use blind `git add .` on mixed trees. Explicitly name staged files per atomic commit.
9. **Execution Protocol**: Show proposed commit plan by default; execute stage-and-commit directly when explicitly instructed.
10. **History Invariants**: Commit `B` is the sole commit whose terminal footers are `Changelog: skip` and `Changelog-Reason: release metadata commit`. Never rewrite published history on `develop` or release lines.

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
| `feat` / `fix` / `perf` | User/operator capability, bugfix, or efficiency improvement |
| `revert` / `docs` | Rollback with stated outcome, or documentation-only change |
| `test/build/ci/refactor/chore` | Internal outcome (skipped from public release notes) |

### CLI Recipes

```bash
# Vertical feature commit (single-outcome staging)
git add path/to/Domain.cs path/to/Page.razor path/to/ApiClient.g.cs
git commit -m "feat(registration): present tenant-branded intermediary disclaimer on paid events" \
           -m "Format canonical directory notice dynamically based on tenant branding."

# Internal nonbreaking commit (with required skip trailers)
git add path/to/ProjectionUpdater.cs
git commit -m "fix(database): wrap session projection rebuilds in db execution strategy" \
           -m "Execute projection rebuilds within execution strategies." \
           -m "Changelog: skip" \
           -m "Changelog-Reason: internal projection resilience enhancement"
```

## Anti-Pattern Catalog

| ❌ Anti-Pattern | ✅ Best Practice | Why |
|---|---|---|
| `feat(api): add disclaimer` | `feat(registration): present disclaimer on paid events` | Layer scope rejected; use product capability. |
| `fix(persistence): retry query` | `fix(database): wrap session projection in execution strategy` | `persistence` is a layer; use `database` scope. |
| `chore: update client` | *[Bundle in originating feature commit]* | Never split generated client from triggering feature. |
| `docs: update cla` | `docs(documentation): clarify legal entity status` | Explicit engineering scope and benefit-led subject. |
| `test: update tests` | `test(testing): harden persistence integration tests` | Descriptive subject and canonical scope. |

## Resources

- [Release policy](../../../docs/RELEASE_POLICY.md)
- [Release policy schema](../../../eng/release/policy/release-policy.yaml)
- [Scope registry](../../../eng/release/policy/scope-registry.yaml)
- [Conventional Commits 1.0](https://www.conventionalcommits.org/en/v1.0.0/)

## Verification

- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/AgentContextPolicyTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
- `git log --format='- %s' "$(git merge-base HEAD origin/develop)"..HEAD`
- Confirm every visible subject is plain-language, every skipped commit has both trailers, and every breaking commit has the required footer.
