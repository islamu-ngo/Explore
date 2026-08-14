<!-- ABOUTME: Recommends a provider-neutral git-cliff design for deterministic changelogs on governed release branches. -->
<!-- ABOUTME: Records the local documentation review, ISLAMU fit analysis, rollout gates, and clean-room handoff. -->

# Git-Cliff Changelog Automation Report

> **Audience:** Maintainers | Release operators | Contributors | AI agents  
> **Status:** Historical research and provenance record; superseded as release architecture by [ADR-025](../../docs/adr/ADR-025-provider-neutral-release-governance.md), [RELEASE_POLICY.md](../../docs/RELEASE_POLICY.md), and [RELEASE_RUNBOOK.md](../../docs/RELEASE_RUNBOOK.md). No changelog automation is implemented by this report.
> **Owner:** Platform/Ops  
> **Last Verified:** 2026-08-13  
> **Source Anchors:** local git-cliff documentation snapshot `5963160d7303111a217ee8453189d23a1c87925a`; [Release Checklist](../../docs/RELEASE_CHECKLIST.md); [CI/CD Governance](../../docs/CI_CD_GOVERNANCE.md); [Conventional Commit skill](../../.agents/skills/conventional-commit/SKILL.md); [current semantic-version changelog](../../docs/semantic_versioning/CHANGELOG.md)

## Purpose

### Authority disposition

This report retains the local documentation inventory, source register, and
sanitized functional handoff. It is not normative release authority. Where its
earlier design conflicts with the linked ADR, policy, or runbook—including
candidate-controlled final tooling, split curated/generated regions, tag-message
duplication, renderer-owned policy, or a candidate distinct from final commit `B`—the
linked canonical documents control. No compatibility shim is required because this
report design has not shipped.

This report defines how ISLAMU Event should use git-cliff to generate high-quality changelogs for governed releases without making GitHub, GitLab, Gitea/Forgejo, Bitbucket, Azure DevOps, or any other forge part of the core release-note contract.

The authoritative branch model is:

- `develop` is the default integration branch and contains ongoing development;
- `v<major>.<minor>` branches, such as `v1.0` and `v1.1`, are release lines;
- `main` points to the latest stable release;
- immutable `v<major>.<minor>.<patch>` tags identify published releases.

Changelog generation belongs to release branches and release tags. Ordinary `develop` work must not continuously generate or commit a changelog.

## Executive Decision

Adopt git-cliff as an **offline, deterministic changelog renderer and advisory version calculator**. Do not make it the release publisher, tag creator, or source of release approval.

The recommended model is:

1. A repository-owned C# entrypoint under `.ci/scripts/` invokes one pinned git-cliff version with `--offline` and `--no-exec`.
2. A provider-neutral `cliff.toml` reads only local Git commits and SemVer tags. It contains no remote-provider block, token, forge URL, PR-label rule, or contributor-username dependency.
3. Each release note has three layers: a curated outcome summary, a filtered git-cliff `What's Changed` list, and the complete tag range for auditability.
4. A release-branch candidate produces an immutable preview, sanitized JSON context, release-line compatibility result, and checksum evidence for that exact SHA.
5. A reviewed release PR commits the versioned release note to the corresponding `v<major>.<minor>` branch. An approved, signed annotated tag closes the release boundary, after which `main` may advance to that exact tagged commit when it is the latest stable line.
6. Tagging, evidence review, artifact publication, and deployment remain manually approved as required by the current [Release Checklist](../../docs/RELEASE_CHECKLIST.md).

This design keeps Git history as the portable source, git-cliff as the replaceable renderer, `.ci/` as the shared implementation surface, and each forge configuration as a thin trigger/artifact adapter.

## Why A Prospective Cutover Is Required

The current repository cannot safely regenerate its complete changelog from history:

| Observation at `3e9c90fed55073f77fc0410d837b6bf3cb8e2aac` | Consequence |
|---|---|
| The remote default branch is `develop`; `main` is the latest stable release branch. | This is intentional. Changelog generation must not run for every default-branch commit. |
| `develop` contains 2,122 commits; `main` contains 206; `develop` is 1,916 commits ahead. | The gap represents ongoing development versus the stable line, not branch drift to reconcile. A release branch selects the exact development cutoff. |
| No local or remote tags exist. | git-cliff has no real release boundary from which to build version sections or Unreleased changes. |
| 1,978 of 2,122 subjects match the repository's broad Conventional Commit forms; 144 do not, and history contains 61 merge commits. | Strict prospective validation is viable, but strict full-history rendering is not. |
| A naive `feat`/`fix`/`perf`/`revert` selection finds more than 1,000 historical candidates. | A machine-generated first release would overwhelm and contradict the curated baseline. |
| Historical scopes include implementation layers such as `api`, `app/*`, `persistence`, and `blazor/*`. | Generated notes would expose architecture mechanics instead of the user-facing capability scopes now defined by the Conventional Commit skill. |
| [The current changelog](../../docs/semantic_versioning/CHANGELOG.md) is a curated release index and roadmap with detailed `v0.1.0.md` and `v1.0.0.md` companions. | Automation must preserve this historical/roadmap material rather than overwrite it with reconstructed commit noise. |

The safe cutover is therefore prospective:

- Preserve the curated pre-automation baseline in the existing version documents.
- Record the exact release-branch base as a temporary lower-bound SHA until the first real SemVer tag exists.
- At the first approved release, create the signed annotated `v0.1.0` tag. Later release branches use the latest applicable stable tag as their lower boundary.
- Never invent a fake SemVer tag merely to make the generator easier to configure.
- Never ask git-cliff to reinterpret the pre-cutover history as if the current release-note policy had always existed.

## Target Architecture

| Layer | Responsibility | Provider-neutral contract |
|---|---|---|
| Git history | Release facts | Full history, signed annotated `vX.Y.Z` tags, Conventional Commit subjects, structured trailers. |
| Versioned release note | Reviewed public narrative | `docs/semantic_versioning/vX.Y.Z.md` owns the curated outcome summary and a generated detailed-change region; `CHANGELOG.md` remains the release index. |
| `cliff.toml` | Classification and rendering policy | Renders the curated tag message, filtered `What's Changed` entries, tag range, canonical ISLAMU scopes, strict unmatched-commit behavior, and no remote metadata. |
| `.ci/scripts/generate-changelog.cs` | Stable project entrypoint | Validates the approved git-cliff version, chooses the cutover/tag range, preserves the reviewed summary, invokes offline/no-exec generation, sanitizes evidence, and verifies generated regions. |
| `.ci/` fixtures | Policy regression checks | Synthetic Git histories prove grouping, breaking changes, skips, trailers, tags, and deterministic output. |
| Forge adapter | Trigger and artifact transport only | Maps release-branch PRs/pushes and release tags into the shared C# command and retains the named outputs. It does not classify commits or render Markdown. |
| Release operator | Approval and publication | Cuts `v<major>.<minor>` from the chosen `develop` SHA, reviews evidence, promotes the changelog through a release-branch PR, creates the signed patch tag, and advances `main` when that line becomes the latest stable release. |

### Release-candidate outputs

For every candidate SHA on a `v<major>.<minor>` release branch, generate these files under an artifact directory before the reviewed changelog update:

| Artifact | Purpose |
|---|---|
| `release-notes.preview.md` | Complete three-layer draft for the selected release version and exact SHA; it is not committed as an ongoing Unreleased section. |
| `git-cliff-context.json` | Sanitized machine-readable classification evidence derived from transient `--context` output. The C# entrypoint removes author/committer identities, raw bodies/messages, and unused remote fields before retention. |
| `bumped-version.txt` | Raw advisory SemVer result used to detect changes that do not fit the active release line; never an instruction to tag or publish. |
| `changelog-checksums.sha256` | Integrity evidence generated by the existing repository checksum tooling. |
| `git-cliff-version.txt` | Exact tool version and, when applicable, the approved binary checksum or image digest. |

The provider adapter may rename the uploaded artifact container, but these internal filenames should stay stable across forges.

The committed changelog contains released version sections only. git-cliff may internally process an unreleased range while preparing a candidate, but that draft remains an artifact until the release version is selected and the release-branch PR is approved. The current curated `[Unreleased]` material is legacy transition input to classify before the first automated release, not the future steady-state model.

The versioned file is intentionally partly curated and partly generated. Maintainers edit the outcome-summary region; the C# entrypoint owns the detailed `What's Changed` and full-range region. Generation must fail rather than overwrite edits outside its explicit generated markers.

### Release outputs

At an approved release boundary:

1. Cut or update the `v<major>.<minor>` release branch from the explicitly selected `develop` SHA.
2. Draft the upper outcome-summary region in `docs/semantic_versioning/vX.Y.Z.md`, assisted by the selected commits and release-impact evidence.
3. Generate the filtered `What's Changed` list and full tag range for the maintainer-selected `v<major>.<minor>.<patch>` version from the previous applicable release tag to the release-branch head.
4. Review the complete three-layer release note together with migration, security, OpenAPI, configuration, and operator evidence.
5. Promote the versioned note and its `CHANGELOG.md` index entry through a normal PR targeting the release branch.
6. Merge the PR and create the signed annotated tag on that exact commit. The annotated tag message must match the reviewed upper outcome-summary region.
7. Regenerate once from the real tag and verify that it matches the approved candidate.
8. Advance `main` to the exact tagged commit only when this is the newest stable release line. An older supported line may publish a patch without moving `main` backwards.
9. Merge or forward-port the tagged release changes into `develop` so the release tag remains reachable before the next release line is cut. The released changelog may travel as historical content, but no Unreleased section is regenerated on `develop`.
10. Forward-port applicable fixes to any newer maintained release line.
11. Include the release-note artifacts and checksums in the durable release evidence bundle.

The branch names the allowed release line. The first stable tag on `v1.1` is selected as `v1.1.0`; later stable tags on that branch are `v1.1.x`. A git-cliff result that requires a higher minor or major version is a compatibility failure for the current branch, not permission to rename the release. Move that work to the appropriate new release line or explicitly redesign the release scope. Release readiness still depends on migration rehearsal, security review, OpenAPI evidence, operator documentation, artifact provenance, and deployment approval.

## Three-Layer Release-Note Model

The public release note must not be a reformatted `git log`. It serves readers at three different levels while preserving one auditable release range.

### Layer 1: Curated outcome summary

The upper section is the primary release narrative. It groups any number of engineering commits into a smaller number of user-, integrator-, or operator-facing outcomes. Use these sections when non-empty:

1. Breaking Changes
2. Features
3. Fixes
4. Improvements
5. Security Notes
6. Upgrade And Operator Notes

One bullet may summarize several commits. For example, separate commits for registration form templates, company attendee imports, exports, and answer analytics can become one reviewed feature bullet describing the complete registration-data capability. The detailed layer still lists the selected source commits, so aggregation improves readability without losing traceability.

The curated region is a release artifact, not an unreviewed inference. A maintainer approves it in the release-branch PR. The exact approved region becomes the signed annotated tag message; git-cliff reads it through the release `message` context. Before the tag exists, the C# entrypoint supplies the same reviewed content through `--with-tag-message` for candidate rendering.

### Layer 2: Filtered `What's Changed`

Git-cliff generates a detailed list of release-visible commits. This is the maintainer/contributor audit view, not the main public narrative. It includes the Conventional Commit subject, canonical scope, and short Git SHA.

This list still does **not** contain every commit. Internal tests, refactors, formatting, CI maintenance, planning, agent-context changes, generated artifacts, and repository chores remain omitted unless they carry `Changelog: include`. Explicit `Changelog: skip` remains available, while breaking-change protection prevents a skip rule from hiding a breaking change.

Do not include entries such as “internal stuff” merely because they exist in the release range. The full range in Layer 3 already preserves access to all engineering history.

### Layer 3: Full change range

End each release note with the exact provider-neutral range, for example:

```text
v1.0.3..v1.1.0
```

The range is the complete audit path, including internal commits excluded from `What's Changed`. The canonical document prints the range as Git identifiers. A forge adapter may turn it into a comparison hyperlink, but the provider-specific URL is not committed as canonical release data.

### Intelligent aggregation policy

Initial adoption does not need an autonomous semantic-merging engine. The selected commit context and release-impact evidence form a grouping worksheet; a maintainer or AI assistant can propose consolidated bullets, and the release PR makes the result authoritative.

For recurring multi-commit capabilities, commits may optionally carry provider-neutral trailers:

```text
Changelog-Group: registration-data-tools
Changelog-Entry: Add reusable registration tools for forms, imports, exports, and analytics.
```

When used, the release tooling enforces:

- every grouped commit is inside the selected release range;
- exactly one canonical `Changelog-Entry` exists per group;
- group members map to one public outcome category unless the group is explicitly breaking;
- breaking descriptions and all `Release-Impact` categories are preserved;
- every member SHA remains in sanitized evidence and the detailed layer;
- a group cannot silently span release versions.

AI may recommend groups and rewrite candidate prose, but it must not decide what is omitted, downgrade a breaking/security/operator impact, or publish the result. Deterministic validation plus release-PR approval remains authoritative.

### Provider enrichment

The canonical release note remains Git-provider agnostic:

- use short Git SHAs and stable, forge-independent work-item identifiers when available;
- omit provider usernames, PR labels, and provider comparison URLs;
- do not calculate “new contributors” from Git author identities because author names do not reliably map to forge accounts and may introduce privacy concerns.

A forge adapter may render a second, noncanonical publication view with PR/merge-request links, contributor handles, a comparison URL, and a New Contributors section. That enrichment must never alter grouping, inclusion, versioning, breaking-change detection, or the committed canonical note.

### Illustrative ISLAMU output

The target shape is:

```markdown
## v1.1.0

### Breaking Changes

- Registration export permissions now follow explicit tenant data-governance policies. Review custom export roles before upgrading.

### Features

- Added reusable registration tools for form templates, company attendee imports, governed exports, and answer analytics.

### Fixes

- Prevented duplicate registration recovery attempts after an interrupted provider callback.

### Improvements

- Improved registration administration with clearer retention and export-impact guidance.

### Upgrade And Operator Notes

- Apply the documented registration migration before enabling company attendee imports.

### What's Changed

- `feat(registration): add form templates` (`1a2b3c4`)
- `feat(registration): import company attendees` (`2b3c4d5`)
- `feat(registration): query answer analytics` (`3c4d5e6`)
- `feat(registration): govern contact exports` (`4d5e6f7`)
- `fix(registration): make provider recovery idempotent` (`5e6f7a8`)

### Full Change Range

`v1.0.3..v1.1.0`
```

The upper feature bullet consolidates several commits into one capability-level statement. The detailed list preserves every release-visible source commit. Internal implementation commits remain accessible through the full range without appearing as public bullets.

## Detailed Change Selection

The following mapping controls the generated `What's Changed` layer and supplies default categories for the curated summary:

| Commit signal | Default outcome category | Rule |
|---|---|---|
| Any valid breaking commit | Breaking Changes | Match before type-specific parsers. Require both the `!` marker and `BREAKING CHANGE:` footer under the project commit contract; render the breaking description once. |
| `feat` | Features | The subject must describe the delivered user/operator outcome. |
| `fix` | Fixes | Include behavior corrections, reliability fixes, privacy/security fixes, and operator-facing defect resolution. |
| `perf` | Improvements | Include only measurable behavior or resource improvements. |
| `revert` | Fixes or Improvements | A revert changes shipped behavior and must remain visible; the curated summary selects the truthful outcome category. |
| `docs` plus `Changelog: include` | Improvements or Upgrade And Operator Notes | Use only when operators, integrators, or users must act differently. |
| `refactor`, `test`, `build`, `ci`, `chore`, `style`, or other allowed internal type plus `Changelog: include` | Improvements | Explicit escape hatch for the rare internal-looking commit that changes an external contract or operator action. |
| Any commit plus `Changelog: skip` | Omitted | Allowed only when the reason is reviewable; a breaking commit remains protected from skip rules. |
| Internal types without explicit inclusion | Omitted | Tests, refactors, repository maintenance, agent context, and planning work are evidence, not release notes. |

Use the canonical capability scopes from the [Conventional Commit skill](../../.agents/skills/conventional-commit/SKILL.md): `events`, `registration`, `ticketing`, `discovery`, `notifications`, `privacy`, `access`, `storage`, `onboarding`, `federation`, `webhooks`, `localization`, `accessibility`, and `self-hosting`. Render the scope as a compact label within the bullet; do not create one heading per scope, because most releases would become sparse and difficult to scan.

Before strict automation becomes required, align [CONTRIBUTING.md](../../docs/CONTRIBUTING.md) and merge-message defaults with that scope vocabulary. Current contributor examples still teach layer scopes, so enabling a hard gate first would make documented behavior contradict enforced behavior.

### Provider-neutral release-impact trailers

Preserve release-impact facts in Git instead of relying on forge-only PR labels. Extend release-visible squash commits with zero or more trailers using the categories already enforced by the PR checklist:

- `Release-Impact: security`
- `Release-Impact: migration`
- `Release-Impact: configuration`
- `Release-Impact: openapi`
- `Release-Impact: operator`

The grouping worksheet can surface these trailers for the curated Security or Upgrade And Operator Notes sections while the detailed layer retains each visible commit. The trailers do not replace detailed evidence in `API_CHANGELOG.md`, migration/operator docs, or the release checklist.

Keep the trailer set small. Do not encode reviewer identities, provider PR numbers, labels, deployment state, or evidence URLs in the git-cliff classification contract.

### Bullet shapes

A curated bullet uses plain past-tense outcome language and may consolidate several commits. Include a stable work-item identifier only when it remains meaningful outside the current forge. Do not force commit hashes into every curated bullet when the detailed layer already supplies traceability.

Each detailed `What's Changed` entry contains:

- the normalized Conventional Commit subject, including type and canonical scope;
- the short commit ID in backticks for portable traceability;
- the breaking description when applicable.

Do not render author emails, raw commit bodies, provider usernames, PR labels, contribution statistics, file counts, additions/deletions, or forge-specific hyperlinks in the canonical changelog. Those values add privacy, trust, or portability concerns without helping an operator understand the release.

## Recommended Git-Cliff Policy

The future `cliff.toml` should implement these decisions after the runtime capability gate passes:

| Setting area | Decision | Reason |
|---|---|---|
| Configuration location | Pass the repository-root `cliff.toml` explicitly. | Avoid parent/global config discovery changing CI output. |
| Changelog template | Repository-owned three-layer header/body/footer with explicit curated and generated boundaries. | Preserves the reviewed summary while deterministically regenerating detailed changes and the range. |
| Release message | Render the reviewed curated region through `message`; use `--with-tag-message` for the candidate and the signed annotated tag message for the final run. | Makes the public summary reviewable before tagging and reproducible afterward. |
| Conventional parsing | Enable conventional parsing and require valid conventional commits after the cutover. | Fails malformed release history instead of silently guessing. |
| Commit splitting | Keep disabled. | One accepted commit is one reviewable outcome; splitting a subject is not a substitute for atomic history. |
| Commit parsers | Ordered parsers for breaking, feature, fix, performance, revert, explicit inclusion, grouping metadata, and explicit skips. | Makes detailed release visibility and optional deterministic aggregation intentional and auditable. |
| Breaking protection | Enable. | A skip parser must never hide a breaking change. |
| Filtering | Filter only explicitly classified or explicitly skipped commits, and fail on unmatched commits. | Prevents unknown types or policy drift from disappearing silently. |
| Processing order | Topological tag and commit ordering; oldest commit first inside a release section. | Handles parallel `v<major>.<minor>` release lines and preserves causal reading order. |
| Tags | Accept only signed release tags matching the project `vX.Y.Z` SemVer form, including approved prerelease/build suffixes. | Deployment tags, SHA tags, and branch tags cannot become releases accidentally. |
| Branch tags | Use branch-tag filtering on the active release branch. | Prevents tags reachable only from another release line or `develop` from changing the candidate. |
| Version bump | Preserve normal SemVer signals: features propose minor, pre-1.0 breaking changes propose minor, post-1.0 breaking changes propose major, and fixes propose patch. Compare the result to the active release line instead of applying it automatically. | A `v1.0` branch cannot silently become `v1.1`; a higher bump means the selected changes do not fit that maintenance line. |
| Non-incrementing types | Treat internal-only types as non-incrementing when the pinned release supports that feature. | CI/docs-only work should not force a release version. |
| Link parsers | Omit forge links. Add only a future stable, public, forge-independent issue/RFC URL if one becomes canonical. | Keeps the committed document valid after a forge migration. |
| Remote configuration | Omit all provider blocks and force offline mode. | Removes API tokens, rate limits, remote metadata drift, and provider lock-in. |
| External commands | Do not configure command preprocessors/postprocessors; always invoke with `--no-exec`. | A changelog template should not become a subprocess execution surface. |
| Paths | Do not use include/exclude path filters for the platform changelog. | ISLAMU Event is one product across layers; path filtering can hide mixed cross-layer outcomes. |
| Submodules/repositories | Do not recurse submodules or merge other repositories. | There is no current release requirement, and merged histories complicate provenance. |
| Commit limit | Do not limit commits. | Truncation would create incomplete release evidence. |

## Git-Cliff Feature Disposition

The complete documentation set was reviewed. The following disposition captures the useful features as well as the features intentionally excluded from the core design.

| Capability | Disposition for ISLAMU Event |
|---|---|
| Custom header/body/footer, Tera conditions/loops/filters, whitespace trimming | Adopt for the project-native document. |
| Ordered commit groups | Use only if a long detailed list benefits from subsections; the curated upper layer remains the primary organization. Use parser order rather than invisible HTML ordering tricks. |
| Grouping releases by semantic-version scope | Defer; ISLAMU ships one platform version, not independent scope versions. |
| Commit/release context, footers, breaking descriptions, previous release, commit ranges | Adopt for rendering and audit evidence. |
| Release statistics and per-commit file/addition/deletion statistics | Retain only in JSON evidence if useful; omit from public notes. |
| Regex commit preprocessors | Use only for narrow, deterministic normalization proven by fixtures. Do not use them to repair bad commit policy. |
| Command preprocessors and changelog postprocessors | Reject in the canonical lane. |
| `--context` | Adopt as a transient input to the C# evidence sanitizer; never retain the raw identity-bearing context as a CI artifact. |
| `--from-context` and free-form `extra` metadata | Defer for the initial two-source composition. Adopt later only if deterministic `Changelog-Group` aggregation needs normalized synthetic group records. |
| `--bumped-version` / `--bump` | Adopt as advisory evidence only. |
| Annotated tag messages | Adopt as the final signed copy of the reviewed curated outcome summary. |
| `--with-tag-message` | Adopt for candidate rendering from the exact reviewed summary; regenerate from the real annotated tag before publication. |
| `--with-commit` | Reject for the generated changelog commit. Including the commit that updates the changelog creates self-reference and noise. |
| Explicit ranges, `--latest`, `--current`, `--unreleased` | Adopt through the C# entrypoint so preview, candidate, and verification modes are named and reproducible. |
| `--prepend` | Use only for the controlled transition that preserves the curated pre-automation baseline or for an approved release-branch update. |
| `--strip` and body-file overrides | Keep available for producing release-section artifacts, but keep the canonical template in one reviewed configuration. |
| `--use-branch-tags`, topological ordering, commit sorting | Adopt. |
| `skip_tags`, `ignore_tags`, `count_tags` | Avoid initially. A strict release-tag pattern is simpler. Add only for a documented prerelease/tag policy. |
| `--skip-commit`, `.cliffignore`, automatic `.git-blame-ignore-revs` handling | Reserve for audited legacy or mass-format exceptions; never use as routine policy. |
| Include/exclude paths and monorepo directory mode | Reject for the platform changelog; consider later only if independently versioned products appear. |
| Multiple-repository history | Reject until one release actually spans independently governed repositories. |
| Submodule changelogs | Reject; nested submodules are not supported and no current need exists. |
| GitHub/GitLab/Gitea/Forgejo/Bitbucket/Azure DevOps remote metadata | Exclude from the canonical path. An optional forge adapter may produce a noncanonical enriched artifact. |
| GitHub Actions installers/action, GitLab example, SourceHut example | Treat only as adapter examples. Do not make any one provider workflow the source of changelog semantics. |
| Python/Rust manifest embedding | Not applicable to this .NET repository; use explicit `cliff.toml`. |
| Jujutsu layouts | No current requirement. The entrypoint can accept a repository path later without changing policy. |
| Config URL | Reject. Configuration must come from the reviewed repository commit, not mutable network content. |

## Runtime Pin And Supply-Chain Decision

The reviewed documentation checkout is not itself a released runtime:

- Documentation checkout: `5963160d7303111a217ee8453189d23a1c87925a`
- Description: `v2.13.1-21-g5963160`
- Latest tag in that checkout: `v2.13.1`
- The documentation after `v2.13.1` adds capabilities relevant to this proposal, including non-incrementing commit regexes and ordered template groups.

Do not implement against the documentation HEAD or assume `v2.13.1` contains every reviewed capability. Production adoption should pin the first signed release that contains the required capability set, then prove the config against that exact binary.

Prefer an official standalone binary over a package-manager install or provider-specific setup action:

1. Verify the upstream signed release and expected checksum during an explicit tool-promotion step.
2. Mirror the approved artifact in ISLAMU-controlled durable storage.
3. Pin its version and checksum in the repository-owned C# entrypoint or adjacent reviewed tool manifest.
4. Verify `git-cliff --version` before every generation.
5. Record the binary checksum in changelog evidence.

The upstream crate declares `MIT OR Apache-2.0`, which is a preliminary permissive-license fit for ISLAMU's intended outbound paths. This is not final dependency approval. Adoption must still run the repository license policy, retain the selected license/notice evidence, review the distributed binary's transitive material, and record the exact version. A container image adds base-image, SBOM, digest, and redistribution obligations, so use one only if its operational benefit outweighs the standalone binary's smaller evidence surface. Never use `latest` or another floating tag.

## CI Flows

### Develop branch

`develop` remains free of generated changelog commits and changelog artifacts. Normal PR/build validation may enforce the repository's Conventional Commit contract, but git-cliff should not render an Unreleased platform changelog for every integration change.

This separation is intentional: `develop` describes work in progress, while a changelog describes a selected release. Squash merging remains the cleanest policy because one accepted PR becomes one outcome-oriented release fact when a release branch is later cut. If ISLAMU intentionally keeps multi-commit PRs, each selected commit must independently satisfy the same contract.

After publication, merge or forward-port the tagged release changes back into `develop` before cutting the next release line. This keeps the prior stable tag in the next branch's ancestry and carries the released changelog only as historical content; it does not authorize continuous Unreleased updates on `develop`.

### Release-branch candidate

For a PR or push on a branch matching `v<major>.<minor>`, the forge adapter supplies the release branch, previous applicable release tag or approved first-release base SHA, candidate version, and exact head SHA to the shared entrypoint. The shared logic should:

1. Validate the git-cliff runtime pin.
2. Validate that branch name, candidate version, and previous tag belong to the same release line.
3. Confirm every commit in the selected range is Conventional Commit compliant and matches one parser or an explicit skip.
4. Validate optional `Changelog-Group`/`Changelog-Entry` relationships and produce a sanitized grouping worksheet for the curated summary.
5. Preserve the reviewed curated region, then render the filtered detailed layer and full range from a full clone with all tags; shallow history is an error.
6. Run without provider tokens, deployment secrets, or write credentials.
7. Compare the generated regions to golden fixtures and publish the complete candidate artifacts for reviewers.
8. Require the versioned release note, generated regions, index entry, and candidate tag message to agree before the release PR can merge.
9. Keep the check name stable if it becomes branch-protection-required.

Merge commits inside the selected range should be prohibited by a linear-history rule or explicitly skipped as merge metadata; do not render them alongside their child commits.

### Release tag and stable-main verification

The release candidate flow uses a maintainer-supplied SemVer version and the reviewed summary through `--with-tag-message` without creating the tag. After checklist approval and release-note PR merge, the maintainer creates the signed annotated `v<major>.<minor>.<patch>` tag with that exact summary. A tag-verification run then regenerates the note from the real tag and fails if the curated or generated regions differ from the approved candidate except for expected tag timestamp fields.

When the tagged release is the newest stable line, advance `main` to that exact tagged commit through the governed repository process. The `main` job verifies all of the following without generating a new changelog:

- `main` HEAD is the intended signed stable tag commit;
- the committed versioned release note and changelog index match the verified tag output;
- version, commit SHA, evidence manifest, and checksums agree;
- no untagged release content was introduced directly on `main`.

If a supported older line such as `v1.0` publishes a patch after `v1.1` is already the latest stable line, retain the tag and line-specific changelog on `v1.0` but do not move `main` backwards.

This preserves the current prohibition on automatic semantic-release behavior while automating the error-prone classification and rendering work.

## Security And Determinism Controls

- Run with `--offline`; do not configure or inject forge API tokens.
- Run with `--no-exec`; do not permit command preprocessors or postprocessors.
- Run release-branch generation and stable-main verification with read-only repository credentials and no deployment/release secrets.
- Use an explicit local config path; forbid config URLs and uncontrolled global config discovery.
- Use a full clone and an explicit commit/range; record the exact HEAD SHA.
- Pin the runtime by version plus checksum or immutable image digest.
- Keep templates independent of environment-variable lookup. Provider adapters pass explicit arguments and artifact paths instead.
- Treat commit text as untrusted Markdown input. Render only the validated subject, canonical scope, controlled breaking footer, controlled release-impact trailers, and short SHA; do not render arbitrary bodies or raw HTML.
- Treat AI grouping and prose as an untrusted draft. Require the release PR to prove every curated bullet against selected commits/evidence and every breaking/security/operator impact remains represented.
- Protect explicit curated/generated markers: the generator may replace only its detailed/range region and must fail on missing, duplicated, or malformed boundaries.
- Do not expose author/committer emails in Markdown, JSON summaries, logs, or evidence bundles.
- Use a concurrency key per release line and cancel stale candidate previews; never cancel tag/stable-main verification after publication has begun.
- Retain checksum evidence with the release bundle rather than relying only on expiring forge artifacts.

## Rollout Plan

### Phase 0: Governance convergence

- Document the established branch contract: `develop` integration, `v<major>.<minor>` release lines, `main` latest stable, and `v<major>.<minor>.<patch>` immutable tags.
- Define how a release branch is cut, protected, patched, forward-ported, and advanced to `main` without treating the intentional `develop`/`main` gap as drift.
- Align contributor guidance, squash/linear-history policy, canonical scopes, `Changelog`/optional aggregation trailers, and `Release-Impact` trailers.
- Adopt the three-layer release-note contract and explicit curated/generated ownership boundaries.
- Choose the pre-automation baseline disposition; preserve existing curated version documents.

Exit condition: contributors can follow one documented commit contract and release operators can identify the exact base, branch, tag, and stable-main transition for every release line.

### Phase 1: Advisory proof

- Pin an approved released git-cliff runtime containing the required capabilities.
- Add `cliff.toml`, the C# entrypoint, and a small synthetic fixture matrix.
- Generate a curated summary, filtered detailed list, and full range on a test release branch without changing `develop`, `main`, or the release process.
- Compare the selected release range and consolidated bullets against maintainer-written expected notes.

Exit condition: repeated runs at the same SHA are byte-identical and maintainers accept the classification quality.

### Phase 2: Required release-branch gate

- Make malformed, unmatched, cross-line, malformed-group, marker-boundary, or forbidden classifications fail release-branch PR validation.
- Keep `develop` free of changelog generation and keep the candidate output artifact-only until the explicit release PR.
- Add release-note preview, sanitized context, version compatibility, and checksum artifacts to the release-evidence manifest as their own category.

Exit condition: the required check is always present on release branches, forge adapters invoke the same core command, and no provider metadata affects output.

### Phase 3: First automated release section

- Approve the first version and release evidence manually.
- Promote the reviewed three-layer version note through a PR targeting its `v<major>.<minor>` release branch.
- Create the first signed annotated SemVer tag from the exact curated region and verify deterministic regeneration.
- Advance `main` to that exact tagged commit and verify the stable snapshot.
- Replace the temporary first-release base SHA with tag-based ranges.

Exit condition: committed notes, signed tag, JSON context, checksums, and durable release evidence agree on version and commit SHA.

### Phase 4: Optional deterministic aggregation and forge enrichment

Only after demonstrated repetition, activate normalized `Changelog-Group`/`Changelog-Entry` records to preassemble candidate bullets; maintainers still approve the curated region. Independently, a forge adapter may generate a second, noncanonical publication artifact with PR links, contributor handles, comparison URLs, and new-contributor acknowledgements. Neither enrichment may change canonical inclusion, grouping approval, versioning, or breaking-impact decisions.

## Verification Matrix

The implementation should not become required until synthetic repositories prove these cases:

| Case | Expected result |
|---|---|
| Valid `feat`, `fix`, `perf`, and `revert` commits | Correct section, scope label, subject, and short SHA. |
| `docs` without explicit inclusion | Omitted without failing. |
| Internal type with `Changelog: include` | Included under Changed or Documentation as defined. |
| Commit with `Changelog: skip` | Omitted; reason remains reviewable in Git. |
| Breaking `!` plus `BREAKING CHANGE:` | Breaking section and advisory version bump. |
| Breaking commit also matching a skip | Still included because breaking protection wins. |
| Missing `!` or missing breaking footer | Rejected by project commit-policy validation. |
| Unknown type or scope | Non-zero result; no silent filtering. |
| Merge commit | Rejected by linear history or explicitly skipped without duplicating children. |
| Release-impact trailers | Correct curated Security/Upgrade And Operator Notes coverage without changing detailed commit inclusion. |
| Several related visible commits | One reviewed curated outcome bullet; every selected commit remains in `What's Changed` and sanitized evidence. |
| Internal commits in the selected range | Omitted from `What's Changed` but still reachable through the printed full tag range. |
| Valid `Changelog-Group` with one `Changelog-Entry` | One candidate aggregate with all member SHAs and impact categories preserved. |
| Missing/duplicate group entry or mixed incompatible categories | Non-zero validation result; no silent aggregation. |
| Breaking or security-impact commit inside a group | Impact remains explicit in the curated summary and evidence; it cannot be downgraded or skipped. |
| AI-generated summary suggestion | Has no release effect until committed and approved in the release-branch PR. |
| Pre-1.0 feature, fix, and breaking changes | Minor, patch, and minor advisory bumps respectively. |
| Post-1.0 breaking change | Major advisory bump. |
| Deployment/SHA/nonrelease tag | Not treated as a release. |
| `v1.0` branch with `v1.1.x` candidate/tag | Rejected as a cross-line version mismatch. |
| Tag from another release branch | Excluded from the active release-line candidate. |
| Empty release range | Valid empty preview; no invented release section. |
| Ordinary `develop` push | No changelog generation or changelog write. |
| Tagged newest stable release advanced to `main` | `main` verifies the exact tag and committed changelog without generating new content. |
| Patch on an older supported line | Line-specific tag/changelog succeeds without moving `main` backwards. |
| Candidate `--with-tag-message` versus real annotated tag | Curated regions are identical; mismatch fails tag verification. |
| Missing/duplicated generated-region markers | Non-zero result without overwriting curated prose. |
| Same SHA generated twice | Byte-identical Markdown/JSON after excluding intentionally time-dependent metadata. |
| No network and no executable processors | Generation succeeds; attempted remote/command use fails the policy gate. |
| Provider A and provider B adapters | Identical canonical artifact checksums for the same Git object database, reviewed summary, version, and SHA. |
| Optional forge-enriched publication | May add links/handles/new contributors but cannot alter canonical bullets, detailed inclusion, or version. |

Run the repository's Release build and architecture tests when the implementation changes `.ci/`, governance docs, or agent context. Add focused tests for the C# entrypoint and config fixtures; do not require the application integration-test matrix for a renderer-only change unless another affected intent demands it.

## Risks And Mitigations

| Risk | Mitigation |
|---|---|
| Full-history noise or false history | Prospective cutover and curated legacy baseline. |
| Forge migration changes notes | Offline Git-only canonical config; provider enrichment is noncanonical. |
| Changelog claims imply release approval | Label version bump advisory; preserve manual checklist/tag/publication approval. |
| Commit-policy drift | Strict unmatched failure plus synthetic fixture tests and one canonical scope list. |
| Curated summary omits or misstates selected changes | Keep the filtered detailed layer and grouping worksheet visible in review; require explicit breaking/security/operator coverage. |
| AI invents or over-compresses an outcome | Treat AI output as a draft only; the reviewed version file and annotated tag message are authoritative. |
| Curated and generated regions overwrite each other | Explicit markers, ownership checks, and fail-closed regeneration. |
| Generated-file merge conflicts | Candidate jobs write artifacts; committed updates happen only in PRs targeting the owning release branch. |
| Recursive automation commits | Do not use direct main write-back or include the changelog-update commit. |
| Remote API outage/rate limit/token leak | No remote configuration and mandatory offline execution. |
| Config executes arbitrary commands | No command processors and mandatory no-exec execution. |
| Unreleased documentation features differ from stable binary | Capability-gated runtime pin and config proof against the exact binary. |
| Tool artifact or image drifts | Signed/checksummed promotion, immutable mirror, exact runtime evidence. |
| Public notes leak PII or unsafe Markdown | Exclude identities/raw bodies and validate the limited rendered fields. |
| Release branches drift from their line | Validate branch/version/tag compatibility and keep separate concurrency/evidence per release line. |
| `main` receives untagged development content | Stable-main verification requires the intended signed tag at HEAD and a matching committed changelog. |

## Proposed Implementation Surface

The smallest maintainable implementation should touch only these concerns:

| Artifact | Planned role |
|---|---|
| `cliff.toml` | Provider-neutral three-layer classification and Tera rendering policy. |
| `.ci/scripts/generate-changelog.cs` | Exact runtime validation, release-note region ownership, mode/range selection, candidate tag message, invocation, evidence sanitization, output checks, and clear failure messages. |
| `.ci/changelog-fixtures/` | Minimal synthetic histories and golden three-layer outputs for filtering, aggregation, tag-message, and range regression. |
| One forge-native adapter | Trigger the shared command for `v<major>.<minor>` branches, release tags, and stable-main verification; subsequent forges repeat only this adapter. |
| `docs/semantic_versioning/vX.Y.Z.md` | Versioned three-layer release note: maintainer-owned curated region plus generated `What's Changed` and full-range region. |
| `docs/semantic_versioning/CHANGELOG.md` | Preserve the curated baseline and index approved versioned release notes; no continuously regenerated Unreleased section. |
| `docs/CONTRIBUTING.md` | Canonical scopes, squash/linear-history policy, inclusion/skip trailers, optional aggregation trailers, and release-impact trailers. |
| `docs/CI_CD_GOVERNANCE.md`, `docs/RELEASE_CHECKLIST.md`, `.ci/README.md` | Required/advisory status, artifacts, evidence retention, and provider-neutral ownership. |

Reuse the existing `.ci/scripts/write-artifact-checksums.cs` and release-evidence generator. Do not add a changelog service, plugin abstraction, database, message broker, or provider SDK.

## Clean-Room And Dependency Handoff

This report is a sanitized functional handoff. The research context inspected git-cliff's official local documentation, Git metadata, package-manifest identity/license fields, and license-file identities, not its implementation source. No upstream template, code, workflow, prose, test, or internal source organization was copied into ISLAMU Event. The recommended naming, grouping, control flow, evidence model, and file ownership were independently derived from ISLAMU's commit, CI/CD, release, licensing, and documentation governance.

Future implementation work should start from this report and the repository's canonical docs. If the implementer needs external research beyond a released git-cliff schema/CLI contract, repeat the clean-room source register and keep external implementation source out of the implementation context.

Dependency disposition:

- Component: git-cliff CLI
- Candidate license: `MIT OR Apache-2.0`
- Current result: preliminary permissive fit; final approval deferred until an exact released binary/image and its conveyed dependency inventory are selected
- Required evidence: exact version, source/release identity, signature/checksum or digest, license/notice files, SBOM/transitive review where distributed, repository license-policy result, and reproducible invocation evidence

## Source Register

### Git-cliff documentation snapshot

| Field | Value |
|---|---|
| Local root | `/home/amir/dev/Github/git-cliff/website/docs` |
| Files reviewed | 58 of 58 |
| Logical bytes | 138,286 |
| Checkout commit | `5963160d7303111a217ee8453189d23a1c87925a` |
| Checkout description | `v2.13.1-21-g5963160` |
| Worktree state | Clean at review time |
| Sorted per-file SHA-256 manifest digest | `b115aadc38cf479c53a8835034ef1b61dbfa9ceb4d04016971774dba6afce556` |
| Source class | Upstream official technical documentation in a local checkout |
| Use | Capability, configuration, CLI, templating, integration, installation, and operational behavior analysis |

Package metadata was limited to identity, version, toolchain, and license verification:

| Local file | SHA-256 | Use |
|---|---|---|
| `/home/amir/dev/Github/git-cliff/Cargo.toml` | `99d0113399d4870a9777ea37f8a8da040bd56c2346af4058bdaa31ebe9de0378` | Workspace/package identity only. |
| `/home/amir/dev/Github/git-cliff/git-cliff/Cargo.toml` | `736d13a114a135a86a877a375a87d0609b1f761c16be829fd6419e90c59119da` | CLI version, Rust version, and declared license only. |
| `/home/amir/dev/Github/git-cliff/LICENSE-MIT` | `733e4c37b548ad90d8209b1df5ff56e9b8631c21407930dd08089d0d4bc59ae5` | License identity/evidence only. |
| `/home/amir/dev/Github/git-cliff/LICENSE-APACHE` | `62c7a1e35f56406896d7aa7ca52d0cc0d272ac022b5d2796e7d6905db8a3636a` | License identity/evidence only. |

### ISLAMU source anchors

Git-history counts use repository HEAD `3e9c90fed55073f77fc0410d837b6bf3cb8e2aac`. The branch purpose and release-line model were clarified by the Project Steward on 2026-08-13: `develop` is default integration, `main` is latest stable, `v<major>.<minor>` branches own release lines, and changelogs exist only for releases. Governance analysis also reflects the pre-existing dirty worktree present during review; this report did not modify those unrelated changes.

- [AGENTS.md](../../AGENTS.md)
- [Conventional Commit skill](../../.agents/skills/conventional-commit/SKILL.md)
- [CI/CD Governance](../../docs/CI_CD_GOVERNANCE.md)
- [Release Checklist](../../docs/RELEASE_CHECKLIST.md)
- [Operations](../../docs/OPERATIONS.md)
- [Testing](../../docs/TESTING.md)
- [Dual Versioning](../../docs/DUAL_VERSIONING.md)
- [IP Governance](../../docs/legal/IP_GOVERNANCE.md)
- [Contributing](../../docs/CONTRIBUTING.md)
- [Semantic-version changelog](../../docs/semantic_versioning/CHANGELOG.md)
- [Shared CI/CD implementation](../../.ci/README.md)
- [Release evidence generator](../../.ci/scripts/generate-release-evidence-bundle.cs)

### Complete git-cliff documentation inventory

The reviewed inventory was:

```text
configuration/bump.md
configuration/changelog.md
configuration/git.md
configuration/index.md
configuration/remote.md
development/_category_.json
development/contributing.md
development/profiling.md
docker.md
github-actions/_category_.json
github-actions/git-cliff-action.md
github-actions/setup-git-cliff.md
github-actions/taiki-e-install-action.md
gitlab.md
index.md
installation/alpine-linux.md
installation/arch-linux.md
installation/binary-releases.md
installation/build-from-source.md
installation/conda-forge.md
installation/crates-io.md
installation/gentoo-linux.md
installation/homebrew.md
installation/index.md
installation/macports.md
installation/mise.md
installation/nix.md
installation/npm.md
installation/pypi.md
installation/winget.md
integration/_category_.json
integration/azure-devops.md
integration/bitbucket.md
integration/gitea.md
integration/github.md
integration/gitlab.md
integration/python.md
integration/rust.md
sourcehut.md
templating/_category_.json
templating/context.md
templating/examples.md
templating/syntax.md
tips-and-tricks.md
usage/_category_.json
usage/adding-commits.md
usage/adding-tag-messages.md
usage/args.md
usage/bump-version.md
usage/examples.md
usage/initializing.md
usage/jujutsu.md
usage/load-context.md
usage/monorepos.md
usage/multiple-repos.md
usage/print-context.md
usage/skipping-commits.md
usage/submodules.md
```

## Final Recommendation

Proceed with git-cliff after Phase 0, using a released capability-complete binary and a Git-only offline configuration. Generate three-layer notes only through governed `v<major>.<minor>` release branches: a reviewed many-commits-to-one outcome summary, a filtered git-cliff `What's Changed` list, and the complete tag range. Verify the curated summary against the signed annotated tag, let `main` remain the exact latest-stable snapshot, and keep `develop` free of generated changelog churn. Provider adapters may enrich publication with PR links, contributor handles, and comparison URLs, but release approval, canonical content, signed tagging, publication, and deployment remain explicitly governed and provider neutral.
