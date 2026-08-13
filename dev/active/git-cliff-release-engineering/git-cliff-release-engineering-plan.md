<!-- ABOUTME: Executable implementation plan for provider-neutral release engineering and git-cliff rendering. -->
<!-- ABOUTME: Re-baselines the changelog report around trusted tooling, exact Git objects, and deterministic evidence. -->

# Git-Cliff Release Engineering - Implementation Plan

Last Updated: 2026-08-13 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Implement the CTO-reviewed git-cliff design for automated, highly curated changelogs on governed release lines while remaining independent of any Git hosting provider.
- **Task directory:** `dev/active/git-cliff-release-engineering/`
- **Planning status:** Draft; ready for user review before implementation.
- **Primary intent:** `ci-cd-change` from `.agents/contract/intents.yaml`.
- **Cross-cutting guardrail:** `ip-clean-room`, required by the CI/CD intent because git-cliff is a third-party build dependency.
- **Planning skill:** `implementation-plan`.
- **Future implementation guidance:** `ip-clean-room`, `conventional-commit`, `.agents/rules/ip-clean-room.md`, and the amended `ci-cd-change` contract created by Task 1.1.
- **Primary layers:** DevOps/build tooling, release governance, documentation, and tests. Product Domain, Application, Persistence, API, and Blazor runtime behavior are out of scope.
- **Complexity:** XL. The work spans a trusted-tool bootstrap, Git object and signature validation, deterministic cross-platform serialization, SemVer and parallel release-line policy, restricted security-release handling, a third-party renderer, evidence integration, and forge adapters.
- **Estimated delivery:** Six reviewable phases, approximately 12-18 focused engineering days plus release-key, artifact-store, and forge configuration approvals.

### Contract drift discovered during planning

`AGENTS.md` and the planning skill still name `.claude/contract/intents.yaml`, while the verified canonical catalog is `.agents/contract/intents.yaml`. The current `ci-cd-change` intent also omits the future `eng/release/**`, `docs/releases/**`, release-engine test, solution, and shared adapter paths. Task 1.1 must correct this contract before implementation expands into those paths.

## 1. Executive Summary

ISLAMU Event will gain a small, tested .NET release-engineering CLI that owns release policy and emits a sanitized, versioned `release-context.v1.json`. A pinned git-cliff binary will consume that context only to render Markdown. Final release attestation will run with a previously promoted release-engine bundle, not code or policy supplied by the candidate branch.

The release model is:

- `develop` remains the default integration branch and receives no continuously generated changelog.
- `v<major>.<minor>` branches own release lines.
- `main` identifies the exact commit of the newest stable release.
- stable tags use `v<major>.<minor>.<patch>`; prereleases may use `-alpha.N`, `-beta.N`, or `-rc.N` under the policy defined in Phase 2.
- every release ends at one reviewed preparation commit `B`; the release branch head, candidate attestation, signed tag target, committed release note, and stable `main` update all identify `B`.

Each canonical release note has three layers:

1. maintainer-owned outcome summary from `summary.md`;
2. a filtered, traceable `What's Changed` list rendered by git-cliff from normalized context;
3. the complete provider-neutral Git range.

High-impact or multi-commit outcomes use immutable change fragments. Ordinary release-visible fixes and features may continue to derive detailed entries from their Conventional Commit subjects. Provider-specific pull request links, usernames, comparison URLs, and contributor sections may be produced later as noncanonical publication views only.

### Non-goals

- No changelog service, database, broker, provider SDK, or plugin framework.
- No automatic release approval, version selection, tag creation, ref push, artifact publication, or deployment.
- No generated `[Unreleased]` churn on `develop`.
- No historical reinterpretation of the existing 2,122-commit development history.
- No GitHub-specific metadata in canonical release context or notes.
- No AI authority over inclusion, breaking impact, versioning, grouping, or publication.
- No product runtime, API, data model, or UI change.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| `develop` is the current branch and has no release tags. | `git branch --show-current`; `git tag --list`; HEAD `3e9c90fed55073f77fc0410d837b6bf3cb8e2aac` | High | Tag count is zero. |
| `develop` has 2,122 commits, `main` has 206, and `develop` is 1,916 commits ahead. | `git rev-list --count develop`, `main`, and `main..develop` | High | Confirms a prospective cutover is required. |
| The current release process is manual and explicitly rejects adding a required `release.yml` until evidence bundling is stable. | `docs/RELEASE_CHECKLIST.md` | High | Automation must preserve manual approval. |
| `.ci/` is the shared provider-neutral implementation surface, but GitHub is currently the authoritative deployment/evidence provider. | `.ci/README.md`; `docs/CI_CD_GOVERNANCE.md` | High | Canonical release logic must move below provider adapters without pretending current deployment is provider-neutral. |
| Existing release evidence is assembled by a standalone C# script. | `.ci/scripts/generate-release-evidence-bundle.cs` | High | It records `GeneratedAtUtc` and GitHub environment fields, so it is not a deterministic canonical release manifest. |
| Release-impact PR metadata is GitHub-specific early feedback. | `.github/workflows/release-impact.yml`; `.ci/scripts/validate-release-impact-pr.cs` | High | It calls the GitHub PR files API; it cannot be canonical release policy. |
| Conventional Commits are currently advisory for manual curation. | `.agents/skills/conventional-commit/SKILL.md` | High | Current scope vocabulary contains only public capability scopes. |
| Current semantic-version docs mix an `[Unreleased]` list, current beta classification, and future roadmap. | `docs/semantic_versioning/CHANGELOG.md`; `docs/semantic_versioning/v1.0.0.md` | High | They are not a stable generated release-note source. |
| The proposed git-cliff report reviewed all 58 upstream documentation files. | `dev/report/git-cliff-changelog-automation-report.md` | High | The report is a useful functional source, but its pre-CTO architecture is no longer authoritative. |
| No release-engine project, release descriptor, change-fragment directory, signing policy, allowed-signer file, or release context exists. | `rg --files` and signing-policy searches | High | These are new artifacts, not current behavior. |
| No overlapping active or paused git-cliff/release workstream exists. | Search of `dev/active/` and `dev/pause/` | High | This workstream is not duplicating an existing plan. |
| The planning baseline builds. | `dotnet build --configuration Release --verbosity quiet` on 2026-08-13 | High | No runtime code is changed by this planning task. |

### 2.2 Existing Implementation

#### Git and release ownership

- `develop`, `main`, and the intended `v<major>.<minor>` release-line model are documented, but no release-line branch or tag currently exists in the local repository.
- `main` and `develop` are intentionally far apart; the gap must not be treated as drift.
- There is no repository-owned signature trust root, protected-tag verification policy, prerelease policy, backport identity, or atomic stable-main runbook.

#### CI/CD and evidence

- `.ci/scripts/` contains repository-owned file-based C# validators and evidence writers.
- `.ci/scripts/write-artifact-checksums.cs` and `.ci/scripts/generate-release-evidence-bundle.cs` provide useful checksum and durable evidence foundations.
- `.github/workflows/release-impact.yml` validates GitHub pull-request metadata from trusted base code without executing pull-request head code.
- GitHub deployment workflows and retained artifacts remain the implemented production/staging path. This plan does not remove or silently replace them.

#### Commit and release-note contracts

- The conventional-commit skill already teaches outcome-led subjects and omission of internal-only types.
- It does not yet distinguish public and engineering scopes, require fragment links for high-impact changes, define `Changelog-Reason`, or define stable backport identity.
- `docs/semantic_versioning/` is curated planning/history content. There is no `docs/releases/` source/output split.

#### Proposed report

The existing report correctly establishes prospective cutover, three-layer notes, offline/no-exec rendering, provider-neutral canonical output, advisory versioning, release-line ownership, and manual approval. The CTO review invalidates these report details:

- candidate-controlled `.ci/scripts/generate-changelog.cs` as final authority;
- a candidate at commit `A` that is expected to match a final tag at a different commit `B`;
- human/generated marker ownership inside one Markdown file;
- exact manual duplication between summary and tag message;
- git-cliff simultaneously described as replaceable while owning release policy;
- rich release-impact metadata carried primarily by commit trailers;
- an oversized single C# script;
- underspecified signing, deterministic serialization, embargo, prerelease, and backport contracts.

### 2.3 Existing Tests And Verification Coverage

- `tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj` is the current governance test project required by the `ci-cd-change` intent.
- No dedicated test project covers `.ci/scripts/generate-release-evidence-bundle.cs` or `.ci/scripts/validate-release-impact-pr.cs`.
- No test covers changelog classification, release-line ancestry, tag signatures, deterministic release context, Markdown injection, prerelease progression, backports, or main/tag identity.
- The future release-engine test project will use synthetic temporary Git repositories and deterministic fixtures. It will not start the application, browser, Docker, Aspire, or an external service.

### 2.4 Existing Documentation And Contracts

Current authorities that implementation must reconcile:

- `AGENTS.md`, `docs/QUICK_REFERENCE.md`, and `docs/GOVERNANCE.md`;
- `.agents/contract/intents.yaml` and `.agents/rules/ip-clean-room.md`;
- `docs/CI_CD_GOVERNANCE.md`, `docs/RELEASE_CHECKLIST.md`, `docs/OPERATIONS.md`, and `docs/TESTING.md`;
- `docs/legal/IP_GOVERNANCE.md`, `docs/legal/CONTRIBUTION_GOVERNANCE.md`, `legal/CLA.md`, and `docs/DUAL_VERSIONING.md`;
- `.agents/skills/conventional-commit/SKILL.md`;
- `.ci/README.md` and the existing evidence/checksum scripts;
- `docs/semantic_versioning/CHANGELOG.md` and its version companions;
- `dev/report/git-cliff-changelog-automation-report.md` as a source report, not final architecture authority.

### 2.5 Current Pain Points / Improvement Areas

1. Candidate code can currently define any future validator that validates itself.
2. The old candidate/tag sequence cannot guarantee the same Git object.
3. Release truth is split across Markdown regions, tag messages, commit trailers, templates, and artifacts.
4. Policy ownership between ISLAMU code and git-cliff is ambiguous.
5. Current evidence JSON is useful as a bundle index but is not canonical, deterministic release identity.
6. GitHub pull-request metadata is useful feedback but is not portable release policy.
7. No cryptographic trust, key rotation, tag replacement, or protected-ref contract exists.
8. No byte-normalization contract protects output across Linux and Windows.
9. Commit text is not yet treated as hostile Markdown/Unicode input at every rendered boundary.
10. Security embargoes and parallel maintained lines have no machine-readable release contract.

### 2.6 Unknowns After Investigation

| Unknown | Search result | Resolution task |
|---|---|---|
| Exact released git-cliff version with required `--from-context`, offline, no-exec, and rendering behavior | The reviewed checkout is `v2.13.1-21-g5963160`; the report explicitly says the exact released runtime remains unselected. | 1.3 |
| Selected non-GitHub forge | Not stated in the request or repository. | 6.1; core implementation does not wait for this choice. |
| Durable internal tool-artifact store | No provider-neutral store contract found. | 1.3 and 3.3 define a local-path-plus-digest interface; the adapter chooses transport later. |
| Authorized release signer principals and operational keys | No signing policy or allowed-signers file found. | 3.3 |
| Exact prospective baseline commit and tag date | No release tags or approved cutover commit exist. | 6.2; selected by the release operator, never guessed by tooling. |
| Restricted security-release workspace/provider | No embargo lane is documented. | 3.3; routine releases remain independent of the later infrastructure choice. |

## 3. Proposed Future State

### 3.1 Ownership model

| Artifact | Authority |
|---|---|
| Git commits | Detailed engineering traceability and Conventional Commit subjects. |
| Public change fragments | Structured high-impact, grouping, backport, migration, API, security-disclosure, and operator metadata. |
| `release.yaml` | Version, release line, date, previous/base release, compatibility references, and impact dispositions. |
| `summary.md` | The single maintainer-owned public outcome narrative. |
| Trusted ISLAMU Release Engineering bundle | Commit/fragment validation, Git range selection, SemVer compatibility, canonicalization, trust verification, context generation, evidence, and orchestration. |
| `release-context.v1.json` | Sanitized, deterministic input to the renderer. |
| git-cliff | Pinned offline Markdown renderer only. |
| `release-notes.md` | Fully generated composition of summary, detailed changes, and full range. |
| Candidate attestation | Pretag identity for exact commit `B`, tools, policy, context, notes, and checksums. |
| Final release evidence | Post-tag identity including tag object ID and authorized signer. |
| Signed annotated tag | Cryptographic release boundary pointing to `B`. |
| Forge adapter | Trigger, trusted-bundle transport, artifact transport, protected ref update, and optional noncanonical enrichment. |

### 3.2 Canonical flow

1. The release engine reads explicit Git object IDs, promoted policy, public fragments, `release.yaml`, and `summary.md`.
2. It validates the Git graph, release line, commit contract, impacts, version, prerelease/backport rules, and summary coverage.
3. It writes canonical `release-context.v1.json` using UTF-8 without BOM, LF, NFC, invariant ordering, and no wall-clock input.
4. A pinned git-cliff binary renders `release-notes.md` from that context with `--offline`, `--no-exec`, an explicit packaged config, and no remote block.
5. The release-preparation commit `B` contains `release.yaml`, `summary.md`, and generated `release-notes.md`. Its message is:

   ```text
   chore(release): prepare v1.1.0

   Changelog: skip
   Changelog-Reason: release metadata commit
   ```

6. The previously promoted release-engine bundle validates `B` and creates `release-candidate.v1.json`. Candidate source, candidate templates, candidate trust roots, and candidate executables are not authoritative.
7. Review preserves `B` through a fast-forward-only, compare-and-swap branch update. Squash, merge-commit, or rebase replacement is rejected.
8. An authorized operator creates a signed annotated tag against full object ID `B`. The generated tag message contains version, compact summary, note path/hash, and candidate-attestation digest.
9. `verify-tag` creates `release-evidence.v1.json` with the tag object ID, signer principal, commit ID, policy/tool hashes, note/context hashes, and previous-release relationship.
10. A normal fast-forward Git push advances `main` to `B` only for the newest stable release. A stale remote ref or non-descendant topology is rejected without force.

### 3.3 Trusted-tool bootstrap

There are two execution lanes:

- **Candidate preview:** may build candidate release-engine source for fast feedback. It has no signing, publication, registry-write, deployment, or protected-ref credentials and is never final evidence.
- **Authoritative attestation:** downloads a previously promoted self-contained release-engine bundle plus its exact digest and signed promotion evidence. Policy, schemas, renderer config, git-cliff pin, and signer trust roots come from that bundle. The candidate checkout is input data only.

The first bundle uses a documented genesis promotion: independent review, clean-room/dependency evidence, Release build, release-engine tests, SBOM/checksums, protected approval, and a signed tooling tag. Later tool upgrades follow the same separate promotion path and cannot become authoritative merely by appearing in a release candidate.

## 4. Non-Negotiable Constraints

1. Final release validation must never execute candidate-controlled code, templates, policy, or trust roots.
2. Canonical generation must be provider-neutral, offline, and free of provider API tokens or metadata.
3. The reviewed release commit, release-line head, signed tag target, committed note, and candidate evidence must identify the same full Git commit object `B`.
4. git-cliff renders normalized context; it does not decide inclusion, impacts, grouping authority, version compatibility, tag selection, or release approval.
5. `summary.md` is the only public narrative source. `release-notes.md` and tag text are generated from repository-owned sources and are not edited independently.
6. Release policy, renderer config, git-cliff, trust roots, and the release engine are pinned inside a separately promoted trusted bundle.
7. Breaking changes cannot be skipped. Every explicit skip requires `Changelog-Reason`.
8. High-impact changes require structured fragments; embargoed details never enter normal public candidate artifacts.
9. Full object IDs are evidence; 12-character display IDs require collision proof.
10. Canonical content uses UTF-8 without BOM, LF, NFC, invariant ordering, a release date fixed in `release.yaml`, and no current-clock value.
11. `develop` does not receive continuously generated Unreleased notes.
12. The tool verifies and emits evidence but does not approve, tag, push, publish, or deploy.
13. No compatibility shims are required for the unshipped report design.
14. No external implementation source may enter the implementation context; dependency research must produce a sanitized handoff and provenance record.

## 5. Architecture And Design Decisions

### Decision 1: ISLAMU owns policy; git-cliff is a true renderer

- **Why:** Versioning, visibility, impacts, backports, and security are governance rules that must not drift with a renderer upgrade.
- **Alternatives considered:** Let git-cliff own parser/filter/bump policy; split policy across C# and `cliff.toml`.
- **Consequences:** The release engine emits `release-context.v1.json`; `cliff.toml` contains presentation logic only. A future renderer can replace git-cliff without reimplementing release governance.
- **Files/layers affected:** `eng/release/src`, `eng/release/policy`, `eng/release/cliff.toml`, release-engine tests.

### Decision 2: One proper .NET tool, no service

- **Why:** The work needs typed commands and tests but no runtime infrastructure.
- **Alternatives considered:** One oversized `.cs` script; service/plugin architecture.
- **Consequences:** One console project and one TUnit project are added under `eng/release/`; no API, database, broker, hosted worker, or SDK abstraction is introduced.
- **Files/layers affected:** `Explore.slnx`, `eng/release/src`, `eng/release/tests`.

### Decision 3: Separate human sources from generated output

- **Why:** Split ownership markers in one Markdown file are fragile.
- **Alternatives considered:** Curated/generated HTML comment markers; tag message as the summary source.
- **Consequences:** Each release directory contains `release.yaml`, `summary.md`, and fully generated `release-notes.md`. Machine context/evidence remains an artifact unless a later ADR approves committing it.
- **Files/layers affected:** `docs/releases/<version>/` and release policy docs.

### Decision 4: Hybrid commit subjects and append-only change fragments

- **Why:** Commit subjects are sufficient for simple details; security, migration, configuration, OpenAPI, operator, breaking, grouping, and backport facts need structured reviewable metadata.
- **Alternatives considered:** All metadata in trailers; fragments for every commit; provider labels.
- **Consequences:** Public fragments live under `docs/releases/changes/<change-id>.yaml`, are append-only after merge, and are referenced by `Change-Id`. Corrections use a new fragment that explicitly supersedes the earlier ID. Fragments are never deleted after release.
- **Files/layers affected:** release policy, contributing guidance, conventional-commit skill, fragment validators.

### Decision 5: Public and engineering scope registries are separate

- **Why:** Public capability scopes improve release notes, while engineering work still needs accurate internal scopes.
- **Alternatives considered:** One public-only hardcoded list; unrestricted free text.
- **Consequences:** `scope-registry.yaml` starts with the current public capability scopes and engineering scopes `ci`, `dependencies`, `architecture`, `database`, `observability`, `documentation`, `release`, `testing`, and `build`. Registry changes are versioned and reviewed.

### Decision 6: SSH signatures are the initial release-tag scheme

- **Why:** Git supports SSH-signed annotated tags with a repository-independent allowed-signers trust model and straightforward principal rotation.
- **Alternatives considered:** Support SSH and OpenPGP immediately; forge UI verification as authority.
- **Consequences:** The initial policy accepts SSH only. `allowed-signers` and release roles are packaged in the trusted bundle; key rotation/revocation creates a new promoted trust-policy version. Independent release approval remains separate evidence because one Git tag carries one cryptographic signature.

### Decision 7: Candidate and final evidence are separate to avoid hash cycles

- **Why:** A final manifest containing a tag object ID cannot also be hashed into the same tag message without a circular dependency.
- **Alternatives considered:** Omit tag identity; commit a self-referential manifest; manually duplicate summaries.
- **Consequences:** `release-candidate.v1.json` exists before the tag and its digest may enter the tag message. `release-evidence.v1.json` exists after the tag and records the tag object ID plus candidate digest.

### Decision 8: Prerelease notes are cumulative from the previous stable release

- **Why:** A final stable release must remain understandable without reconstructing every prior RC.
- **Alternatives considered:** Incremental prerelease-to-prerelease public notes.
- **Consequences:** Evidence records both `baseStableTag` and `previousPublishedTag`. `alpha.N`, `beta.N`, and `rc.N` counters are contiguous; stable promotion uses the same base version; prereleases never advance `main`; SemVer build metadata is not allowed in canonical release tags.

### Decision 9: Canonical output is provider-neutral; enrichment is secondary

- **Why:** Forge migration must not change release truth.
- **Alternatives considered:** Provider APIs/labels as the primary source; refuse all enrichment.
- **Consequences:** Canonical notes use Git identifiers and stable change IDs. A later adapter may add links, handles, and contributor acknowledgements without changing canonical checksums or classifications.

## 6. Implementation Phases

### Phase 1: Governed Foundation And Tool Selection

- **Goal:** Establish permitted paths, authoritative documents, a minimal release-engine project, and an approved git-cliff tool pin.
- **Depends on:** User approval of this plan.
- **Related skills/rules:** `ip-clean-room`, `conventional-commit`, `.agents/rules/ip-clean-room.md`, `ci-cd-change` intent.
- **Acceptance criteria:** The solution builds with the new tool/test projects; the contribution contract covers the implementation surface; `verify-tools` rejects an unpinned binary; git-cliff has exact version/license/checksum evidence; no final release behavior is claimed yet.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Remove only the unshipped release-engine project and new governance artifacts from the implementing branch. Existing manual releases remain unchanged.

#### Task 1.1: Re-baseline Release Governance And Contribution Scope

- **Type:** modify/create
- **Layer:** Docs / DevOps
- **Files:** existing `.agents/contract/intents.yaml`, `docs/CI_CD_GOVERNANCE.md`, `docs/RELEASE_CHECKLIST.md`, `docs/index.md`, `dev/report/git-cliff-changelog-automation-report.md`; new `docs/adr/ADR-025-provider-neutral-release-governance.md`, `docs/RELEASE_POLICY.md`, `docs/RELEASE_RUNBOOK.md`.
- **Description:** Extend `ci-cd-change` scope and acceptance criteria for `eng/release/**`, `docs/releases/**`, selected `.ci` adapter paths, `Explore.slnx`, and release-engine tests. Split stable architecture, normative policy, and operator steps into the ADR/policy/runbook. Mark the source report as superseded where it conflicts with this plan, while retaining its git-cliff documentation inventory and provenance.
- **Acceptance Criteria:**
  - [ ] The contract permits every planned path before implementation touches it.
  - [ ] Normative text uses MUST/SHOULD/MAY consistently.
  - [ ] The ADR records trusted tooling, exact commit `B`, single source ownership, and renderer-only git-cliff.
  - [ ] Existing manual release behavior remains documented until a later phase activates automation.
- **Dependencies:** None.
- **Effort:** M
- **Required Skills/Rules:** `ip-clean-room`, `.agents/rules/ip-clean-room.md`.

#### Task 1.2: Create The Minimal Release-Engine Project

- **Type:** create/modify
- **Layer:** DevOps
- **Files:** existing `Explore.slnx`; new `eng/release/src/ISLAMU.ReleaseEngineering/ISLAMU.ReleaseEngineering.csproj`, `eng/release/src/ISLAMU.ReleaseEngineering/Program.cs`, `eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj`, initial command tests, project lock files, and `eng/release/README.md`.
- **Description:** Add one `net10.0` console project and one TUnit project with no product-project references. Start with a small first-argument command dispatcher and `verify-tools`; use `System.Diagnostics.Process` for external tools and the existing centrally pinned YamlDotNet only when policy loading begins. Do not scaffold speculative interfaces or empty directories.
- **Acceptance Criteria:**
  - [ ] `Explore.slnx` builds the tool and test project.
  - [ ] Unknown commands and invalid arguments fail with stable nonzero exit codes and bounded diagnostics.
  - [ ] The project does not reference Domain, Application, Infrastructure, API, Blazor, or Persistence.
  - [ ] Every new file starts with two `ABOUTME` lines.
- **Dependencies:** 1.1.
- **Effort:** M
- **Required Skills/Rules:** repository governance; no runtime Clean Architecture layer is entered.

#### Task 1.3: Select, Record, And Promote The Git-Cliff Runtime

- **Type:** investigate/create/modify
- **Layer:** DevOps / Legal
- **Files:** new `eng/release/toolchain.lock.json`, `docs/legal/dependencies/git-cliff.md`, and `dev/active/git-cliff-release-engineering/git-cliff-dependency-handoff.md`; modify `eng/release/README.md` and `verify-tools` tests.
- **Description:** In a separate authorized research lane, select the first released git-cliff version that proves `--from-context`, explicit config, `--offline`, `--no-exec`, and deterministic rendering. Record official release identity, version, platform artifacts, signatures/checksums, `MIT OR Apache-2.0` obligations, notices, transitive/conveyed inventory, SBOM when available, and repository license-policy results. Promote immutable Linux x64 and Windows x64 artifacts to ISLAMU-controlled storage; the release engine accepts a local bundle path plus expected digest, so storage transport remains provider-neutral.
- **Acceptance Criteria:**
  - [ ] The implementation context receives only the sanitized functional/dependency handoff.
  - [ ] Exact released version and per-platform SHA-256 values are locked.
  - [ ] `verify-tools` rejects wrong version, wrong digest, missing binary, and unapproved platform.
  - [ ] `dotnet run .ci/scripts/validate-dependency-license-policy.cs -- .` passes or a documented approval blocks implementation until resolved.
- **Dependencies:** 1.2.
- **Effort:** M
- **Required Skills/Rules:** `ip-clean-room`, dependency license gate, AFC/SSO evidence.

### Phase 2: ISLAMU Policy Engine And Normalized Context

- **Goal:** Make ISLAMU-owned code the only authority for commit validity, release visibility, impacts, grouping inputs, version compatibility, prereleases, and backports.
- **Depends on:** Phase 1.
- **Relevant files:** new policy/config/model/command files under `eng/release/`; contributor and commit guidance updated by owning tasks.
- **Related skills/rules:** `conventional-commit`, `ip-clean-room`.
- **Acceptance criteria:** A synthetic range can be normalized without invoking git-cliff; malformed or contradictory metadata fails closed; context JSON is deterministic and contains no identities or raw bodies.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Policy remains advisory until all Phase 2 fixtures pass. No branch rule or release gate is enabled.

#### Task 2.1: Implement Commit, Scope, And Skip Policy

- **Type:** create/modify
- **Layer:** DevOps / Docs
- **Files:** new `eng/release/policy/release-policy.yaml`, `eng/release/policy/scope-registry.yaml`, commit parser/validator files and tests under `eng/release/`; modify `.agents/skills/conventional-commit/SKILL.md` and `docs/CONTRIBUTING.md`.
- **Description:** Parse the prospective Conventional Commit contract, distinguish public and engineering scopes, classify release-visible versus internal commits, require both `!` and `BREAKING CHANGE:`, require `Changelog-Reason` for `Changelog: skip`, and prevent any breaking change from being skipped. Keep PR labels and forge identities out of policy.
- **Acceptance Criteria:**
  - [ ] Known public scopes may appear in canonical notes; known engineering scopes are valid but omitted by default.
  - [ ] Unknown types/scopes, contradictory trailers, missing skip reasons, and incomplete breaking metadata fail.
  - [ ] Release metadata commit `B` is recognized as an explicit explained skip.
  - [ ] Policy fixtures cover valid, omitted, included, skipped, breaking, revert, and malformed cases.
- **Dependencies:** 1.2.
- **Effort:** L
- **Required Skills/Rules:** `conventional-commit`.

#### Task 2.2: Implement Change Fragments And Release Descriptors

- **Type:** create/modify
- **Layer:** DevOps / Docs
- **Files:** new typed YAML models/validators and tests under `eng/release/src` and `eng/release/tests`; new templates/examples under `docs/releases/README.md` and `docs/releases/changes/README.md`.
- **Description:** Validate `release.yaml` and public fragments. Require fragments for breaking, security, migration, configuration, OpenAPI, and operator impacts; allow optional fragments for deterministic multi-commit grouping. Link fragments with stable `Change-Id`; use `Backport-Of` for full original commit IDs. Fragments are append-only after merge and remain after release.
- **Acceptance Criteria:**
  - [ ] Duplicate IDs, missing required impacts, missing migration/operator references, mixed incompatible groups, and fragment mutation/deletion fail.
  - [ ] `release.yaml` fixes version, line, release date, base/previous release, compatibility references, and impact dispositions.
  - [ ] Simple low-impact features/fixes can remain fragment-free.
  - [ ] No embargoed detail is accepted from the public fragment directory.
- **Dependencies:** 2.1.
- **Effort:** L
- **Required Skills/Rules:** `conventional-commit`, `ip-clean-room`.

#### Task 2.3: Implement Version, Prerelease, Backport, And Context Policy

- **Type:** create
- **Layer:** DevOps
- **Files:** new SemVer/range/context models and tests under `eng/release/`; new `release-context.v1.json` golden fixtures.
- **Description:** Independently compute the minimum SemVer change, validate the selected version against the active `v<major>.<minor>` line, enforce contiguous prerelease counters, distinguish `baseStableTag` from `previousPublishedTag`, reject build metadata in canonical tags, and retain stable change identity across backports/forward-ports. Emit deterministic sanitized context with full object IDs in evidence and fixed 12-character display IDs after collision checking.
- **Acceptance Criteria:**
  - [ ] Pre-1.0 and post-1.0 breaking semantics are explicit and tested.
  - [ ] Prerelease notes are cumulative from the previous stable tag and never advance `main`.
  - [ ] Older-line backports retain `Change-Id` and `Backport-Of` without being described as a new capability.
  - [ ] git-cliff bump output, when later available, is comparison evidence only; disagreement fails review.
- **Dependencies:** 2.1, 2.2.
- **Effort:** L
- **Required Skills/Rules:** release policy ADR.

### Phase 3: Git Trust, Determinism, And Security Boundaries

- **Goal:** Validate complete Git object topology, normalize every canonical byte, establish trusted execution and tag-signature roots, and define an embargo-safe lane.
- **Depends on:** Phase 2.
- **Related skills/rules:** `ip-clean-room`; security requirements in `docs/CI_CD_GOVERNANCE.md`.
- **Acceptance criteria:** Candidate input is treated as hostile data; shallow/partial/replaced history and unauthorized signatures fail; Linux and Windows fixtures produce identical bytes.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** A failed trust or determinism check stops before tag/ref/publication activity. Operators retain the reviewed branch commit and can rerun after correcting inputs or promoting a new trusted bundle.

#### Task 3.1: Implement Git Object And Release-Line Validation

- **Type:** create
- **Layer:** DevOps
- **Files:** new Git process/repository/graph validators and synthetic-repository tests under `eng/release/`.
- **Description:** Invoke Git with explicit repository paths and controlled configuration. Reject shallow clones, missing partial-clone objects, replace refs, grafts, lightweight release tags, ambiguous refs, wrong-line tags, non-ancestor previous releases, non-linear release preparation, and object-format assumptions. Use full OIDs and `--no-replace-objects`.
- **Acceptance Criteria:**
  - [ ] Selected base, previous tag, release branch head, and candidate commit resolve to explicit objects.
  - [ ] Parallel release lines select only applicable tags.
  - [ ] Missing objects and cross-line versions fail with actionable diagnostics.
  - [ ] SHA-1 and future SHA-256 object formats are not hardcoded to 40 characters.
- **Dependencies:** 2.3.
- **Effort:** L
- **Required Skills/Rules:** release policy ADR.

#### Task 3.2: Implement Canonicalization And Untrusted-Text Hardening

- **Type:** create
- **Layer:** DevOps / Security
- **Files:** new canonical JSON/Markdown/text validation files and tests under `eng/release/`.
- **Description:** Enforce UTF-8 without BOM, LF, NFC, invariant culture, stable JSON property/array order, forward-slash manifest paths, release-descriptor date, fixed final newline behavior, and no clock-derived canonical fields. Set `TZ=UTC`, `LC_ALL=C`, `LANG=C`, `GIT_TERMINAL_PROMPT=0`, `GIT_OPTIONAL_LOCKS=0`, and `GIT_CONFIG_NOSYSTEM=1`; isolate global Git config without repurposing `HOME`. Reject/escape raw HTML, control characters, bidi controls, unsafe Markdown delimiters, and oversized subjects/trailers. Use a deterministic fuzz corpus without adding a fuzzing dependency.
- **Acceptance Criteria:**
  - [ ] Canonical files are byte-identical across Windows and Linux fixture runs.
  - [ ] Candidate and final clocks cannot change canonical content.
  - [ ] Markdown/HTML injection, CR/NUL, bidi, and length attacks fail or are safely escaped as policy specifies.
  - [ ] Short-ID collisions deterministically increase display length or fail clearly.
- **Dependencies:** 3.1.
- **Effort:** L
- **Required Skills/Rules:** security and privacy constraints in this plan.

#### Task 3.3: Establish Trusted Bundle, SSH Signer, And Embargo Contracts

- **Type:** create/modify
- **Layer:** DevOps / Security / Operations
- **Files:** new `eng/release/trust/release-signing-policy.yaml`, `eng/release/trust/allowed-signers`, `eng/release/trust/rotation-history.md`, trusted-bundle manifest/verification code and tests; modify `docs/RELEASE_RUNBOOK.md`, `docs/CI_CD_GOVERNANCE.md`, and dependency/provenance records.
- **Description:** Define genesis and upgrade promotion for a self-contained release-engine bundle containing policy, renderer config, tool pins, and trust roots. Accept SSH-signed annotated release tags only, bind principals to release roles, record rotation/revocation behavior, and detect replaced tag object IDs. Define a restricted security lane where embargoed fragments are supplied from access-controlled storage outside the public checkout and normal candidate artifacts reveal no restricted metadata. Final jobs must not expose credentials while any candidate executable runs.
- **Acceptance Criteria:**
  - [ ] Authoritative validation proves the release-engine bundle digest and promotion evidence before reading candidate data.
  - [ ] Candidate changes to engine source, policy, config, tool locks, or signer files cannot alter final attestation.
  - [ ] Unauthorized, expired/revoked-by-policy, unsigned, and lightweight tags fail.
  - [ ] The embargo path emits only approved public disclosure fields after the release boundary.
- **Dependencies:** 1.3, 3.1, 3.2.
- **Effort:** XL
- **Required Skills/Rules:** `ip-clean-room`; protected credential and untrusted-code CI rules.

### Phase 4: Git-Cliff Rendering And Final Preparation Commit

- **Goal:** Render the complete three-layer note from canonical context and produce the exact release-preparation commit contract.
- **Depends on:** Phase 3.
- **Related skills/rules:** `conventional-commit`, git-cliff sanitized dependency handoff.
- **Acceptance criteria:** git-cliff owns presentation only; release notes are fully generated; the final candidate is validated at commit `B`; two runs at `B` match byte for byte.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep candidate output artifact-only. If generation differs, do not create `B` or tag; correct the descriptor/summary/policy through normal review.

#### Task 4.1: Integrate Git-Cliff As Renderer Only

- **Type:** create
- **Layer:** DevOps
- **Files:** new packaged `eng/release/cliff.toml`, renderer adapter code, and real-binary synthetic fixtures under `eng/release/`.
- **Description:** Invoke the promoted binary with `--from-context`, explicit packaged config, `--offline`, and `--no-exec`. The template may order headings, loop normalized entries, and format Markdown, but it must not parse commits, call providers, select tags, calculate authority, execute commands, or read mutable network config.
- **Acceptance Criteria:**
  - [ ] The same context renders the same Markdown without a Git repository or network.
  - [ ] A malicious or policy-bearing candidate `cliff.toml` is ignored by authoritative validation.
  - [ ] Provider fields, authors, emails, raw bodies, and remote links are absent from canonical output.
  - [ ] Replacing git-cliff with a fixture renderer does not change policy/context tests.
- **Dependencies:** 1.3, 2.3, 3.2, 3.3.
- **Effort:** L
- **Required Skills/Rules:** `ip-clean-room` sanitized handoff.

#### Task 4.2: Implement Release Composition And `prepare`

- **Type:** create/modify
- **Layer:** DevOps / Docs
- **Files:** new `prepare` command/composition tests under `eng/release/`; new release directory templates documented by `docs/releases/README.md`; modify `docs/RELEASE_RUNBOOK.md`.
- **Description:** Read `docs/releases/<version>/release.yaml` and `summary.md`, produce `release-context.v1.json` as an artifact, render fully generated `release-notes.md`, and emit the exact release-preparation commit message. The detailed layer includes only release-visible entries; the full range includes all commits. Fragments produce a grouping/impact worksheet, while the maintainer-owned summary remains the final narrative.
- **Acceptance Criteria:**
  - [ ] No generated-region markers or partial file ownership remain.
  - [ ] Empty sections are omitted and fixed section order is preserved.
  - [ ] Every required impact has a structured disposition and a non-empty applicable summary/operator section.
  - [ ] The generated commit message always contains the required skip and reason.
- **Dependencies:** 2.2, 4.1.
- **Effort:** L
- **Required Skills/Rules:** `conventional-commit`.

#### Task 4.3: Implement Exact-`B` Candidate Attestation

- **Type:** create
- **Layer:** DevOps / Security
- **Files:** new `verify-candidate` command, candidate-manifest serializer, and Git fixture tests under `eng/release/`.
- **Description:** After the preparation commit exists, recompute the selected range through `B`, recognize `B` only through its explicit release-metadata skip, rerender, and require exact equality. Emit `release-candidate.v1.json` with object format, full commit IDs, version/line/date, previous/base tags, policy/bundle/git-cliff digests, context/note/summary hashes, and no tag object ID or wall-clock field.
- **Acceptance Criteria:**
  - [ ] Candidate validation runs at `B`, not its parent `A`.
  - [ ] Squash/rebase/merge replacement, branch movement, or note drift invalidates the candidate.
  - [ ] The candidate manifest is deterministic and suitable for external signing/attestation.
  - [ ] Windows/Linux generation of context, notes, tag-message input, and candidate manifest is byte-identical.
- **Dependencies:** 4.2.
- **Effort:** L
- **Required Skills/Rules:** trusted bundle contract.

### Phase 5: Tag Closure, Stable Main, And Evidence Integration

- **Goal:** Close the release boundary cryptographically, protect parallel release lines, and integrate canonical manifests with the existing durable evidence bundle.
- **Depends on:** Phase 4.
- **Related skills/rules:** `ip-clean-room`; CI/CD credential and release-evidence policy.
- **Acceptance criteria:** An authorized signed tag at `B` can be verified without provider APIs; stale or wrong refs fail; evidence has one release-identity source; older-line patches cannot move `main` backwards.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** If tag verification fails, publication and `main` update remain blocked. Never delete/recreate a published tag as an automated repair; record the incident and follow the runbook.

#### Task 5.1: Implement `verify-tag` And Final Evidence

- **Type:** create
- **Layer:** DevOps / Security
- **Files:** new `verify-tag` command, tag-message generator, final-manifest serializer, and signed-tag fixtures under `eng/release/`; modify `docs/RELEASE_RUNBOOK.md`.
- **Description:** Verify annotated object type, SSH signature, authorized principal, exact target `B`, tag name/version/line, ancestry, candidate digest, release-note hash, and prior release relationship. Generate `release-evidence.v1.json` after the tag so it can contain the tag object ID without a hash cycle.
- **Acceptance Criteria:**
  - [ ] Tag text is generated from canonical sources and is never an independently edited summary.
  - [ ] Deleted/recreated tag objects are detectable by stored full tag object ID.
  - [ ] Candidate and final manifests chain through the candidate digest.
  - [ ] No provider UI signature badge is treated as verification authority.
- **Dependencies:** 3.3, 4.3.
- **Effort:** L
- **Required Skills/Rules:** release-signing policy.

#### Task 5.2: Implement `verify-main` And Parallel-Line Rules

- **Type:** create
- **Layer:** DevOps
- **Files:** new `verify-main` command, release topology tests, and runbook updates under `eng/release/` and `docs/RELEASE_RUNBOOK.md`.
- **Description:** Verify that a proposed stable-main update is a normal fast-forward from the expected remote `main` object to the newest stable tag commit `B`. Reject prereleases, older-line patches, topology gaps, force updates, and stale compare-and-swap inputs. Verify forward-port obligations without mutating branches.
- **Acceptance Criteria:**
  - [ ] `main == tag^{commit}` after a newest-stable update.
  - [ ] An older supported line can publish a patch without moving `main`.
  - [ ] A remote ref race or non-descendant candidate fails before push.
  - [ ] The tool emits the expected old/new OIDs and runbook action but never pushes itself.
- **Dependencies:** 5.1.
- **Effort:** M
- **Required Skills/Rules:** release policy ADR.

#### Task 5.3: Integrate Canonical Release Identity With Existing Evidence

- **Type:** modify/create
- **Layer:** DevOps / Docs
- **Files:** existing `.ci/scripts/generate-release-evidence-bundle.cs`, `.ci/scripts/write-artifact-checksums.cs`, `docs/CI_CD_GOVERNANCE.md`, `docs/RELEASE_CHECKLIST.md`; new fixtures/tests under `eng/release/tests` and evidence schema documentation under `eng/release/README.md`.
- **Description:** Make the existing durable bundle ingest and verify `release-evidence.v1.json` rather than independently invent release identity. Keep bundle generation time and workflow transport metadata explicitly noncanonical. Add release-governance artifact classification and reject version/SHA/tag/hash disagreement.
- **Acceptance Criteria:**
  - [ ] One canonical manifest owns version, tag object, commit, tools, policy, context, and notes hashes.
  - [ ] The bundle index may record collection time/run IDs but cannot override canonical identity.
  - [ ] Checksums include context, notes, candidate/final manifests, tool promotion evidence, and tag verification.
  - [ ] Existing container/deployment/OpenAPI/test evidence categories remain intact.
- **Dependencies:** 5.1.
- **Effort:** M
- **Required Skills/Rules:** `ci-cd-change`, `ip-clean-room`.

### Phase 6: Provider Adapters, Prospective Cutover, And Activation

- **Goal:** Expose one provider-neutral command contract, add the selected non-GitHub adapter, establish the signed baseline, and activate the release-only workflow without changing `develop` behavior.
- **Depends on:** Phase 5 and maintainer selection of a forge before Task 6.1 finishes.
- **Related skills/rules:** `ci-cd-change`, `ip-clean-room`, `conventional-commit`.
- **Acceptance criteria:** Two adapters/local invocations can produce identical canonical checksums from the same Git objects; the signed non-release baseline is approved; release branches have always-present gates; `develop` remains free of generated changelog writes.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep automation advisory and retain the manual release checklist until a full dry run passes. Disable the provider trigger without removing the shared release engine or rewriting release history.

#### Task 6.1: Define The Adapter Contract And Add The Selected Forge Adapter

- **Type:** investigate/create/modify
- **Layer:** DevOps
- **Files:** new `.ci/release/adapter-contract.md`; selected-provider definition under `.ci/providers/<selected-provider>/` after the bounded selection; modify `.ci/README.md`, `docs/CI_CD_GOVERNANCE.md`, and selected provider settings documentation.
- **Description:** First record the selected forge and its custom pipeline path. The adapter must fetch complete Git objects/tags, acquire the promoted trusted bundle by immutable digest, pass explicit local paths/OIDs/version, upload named artifacts, and use protected environment/ref controls for tag/main operations. It must not classify commits or enrich canonical context. A local/offline invocation remains the reference adapter.
- **Acceptance Criteria:**
  - [ ] Adapter inputs/outputs and permission boundaries are provider-neutral.
  - [ ] Candidate jobs have read-only credentials and no publication/signing/deployment secrets.
  - [ ] Final jobs never execute candidate source and use protected manual approval.
  - [ ] Canonical checksums match the local reference invocation.
- **Dependencies:** 3.3, 5.3; selected forge decision.
- **Effort:** L
- **Required Skills/Rules:** `ci-cd-change`; provider workflow security rules.

#### Task 6.2: Establish The Prospective Baseline And Release-Doc Transition

- **Type:** modify/create/operator action
- **Layer:** Docs / DevOps
- **Files:** existing `docs/semantic_versioning/CHANGELOG.md`, version companions, `docs/RELEASE_POLICY.md`, and `docs/RELEASE_RUNBOOK.md`; new `docs/releases/README.md` and first approved `docs/releases/<version>/` directory. The signed baseline tag is a Git ref, not a file edit.
- **Description:** Preserve existing semantic-version files as an explicitly frozen pre-automation planning/history baseline. Select the exact first lower-bound commit and create an authorized signed non-release tag named `changelog-baseline-YYYY-MM-DD`; record its full tag and commit object IDs. The release engine's strict SemVer pattern must ignore it. After the first stable tag, later releases use applicable stable tags as their lower bound.
- **Acceptance Criteria:**
  - [ ] No historical commit is reclassified by the new policy.
  - [ ] No fake SemVer tag is created.
  - [ ] Existing roadmap content is not presented as generated released history.
  - [ ] Baseline creation is an explicit operator-approved runbook action, not automatic tool behavior.
- **Dependencies:** 5.1, 6.1.
- **Effort:** M
- **Required Skills/Rules:** release policy ADR.

#### Task 6.3: Activate Contributor And Release Gates Through An Advisory Dry Run

- **Type:** modify/create
- **Layer:** DevOps / Docs
- **Files:** existing `docs/CONTRIBUTING.md`, `.agents/skills/conventional-commit/SKILL.md`, `docs/RELEASE_CHECKLIST.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, provider adapter definition, and architecture tests; new synthetic/advisory evidence under the active workstream as allowed by clean-room policy.
- **Description:** Align contributor scopes, fragments, skip reasons, breaking metadata, and backport identity. Run the complete candidate-at-`B`, signed test tag, final evidence, and main-verification flow against a synthetic or disposable local repository first. Promote the release-branch check from advisory to required only after deterministic evidence and protected provider settings are recorded. Do not generate changelogs on ordinary `develop` pushes.
- **Acceptance Criteria:**
  - [ ] All mandatory CTO verification cases are represented in release-engine or architecture fixtures.
  - [ ] Required checks remain always present or have a documented no-op path.
  - [ ] Release branch/tag/main protections and signer roles have retained provider settings evidence.
  - [ ] The manual release checklist remains the approval source; automation removes no governance gate.
- **Dependencies:** 6.1, 6.2.
- **Effort:** L
- **Required Skills/Rules:** `conventional-commit`, `ci-cd-change`, `ip-clean-room`.

## 7. Testing Strategy

Each phase runs one Release build and at most one project test command after all phase tasks. The release-engine test project owns deterministic unit tests plus synthetic Git repository fixtures. It may invoke the promoted git-cliff binary only in the rendering fixture category; policy tests remain independent of git-cliff.

Required case families include:

- commit types/scopes, skip reasons, breaking redundancy, fragment requirement, grouping, backports, and impact coverage;
- pre-1.0/post-1.0 SemVer, prerelease continuity, stable promotion, and branch/version mismatch;
- shallow/partial clones, missing objects, replace refs, grafts, ambiguous/lightweight/wrong-line tags, and parallel release lines;
- Markdown/HTML/control/bidi/length attacks and short-ID collisions;
- candidate engine/config/policy/trust changes that cannot affect trusted attestation;
- candidate `B` preservation, branch movement races, squash/merge/rebase replacement, and exact regeneration;
- authorized/unauthorized/replaced signed tags and candidate-to-final evidence chaining;
- newest-stable versus older-line `main` behavior;
- Linux/Windows byte equality and different wall clocks;
- identical canonical checksums across local and selected-provider adapters;
- embargoed security input not appearing in normal artifacts.

No planned phase starts the application, browser, Docker, Aspire, Playwright, or an external service.

## 8. Documentation, Configuration, And Operations Impact

### Documentation

- New: `docs/adr/ADR-025-provider-neutral-release-governance.md`.
- New: `docs/RELEASE_POLICY.md` and `docs/RELEASE_RUNBOOK.md`.
- New: `docs/releases/README.md`, per-release sources/outputs, and change-fragment guidance.
- New: `docs/legal/dependencies/git-cliff.md`.
- Updated: `docs/CI_CD_GOVERNANCE.md`, `docs/RELEASE_CHECKLIST.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, `docs/CONTRIBUTING.md`, `docs/index.md`, `.ci/README.md`, the conventional-commit skill, and the source report.
- Preserved: `docs/semantic_versioning/**` as the explicit pre-automation baseline/roadmap, not silently regenerated history.

### Configuration

- New trusted policy: `eng/release/policy/release-policy.yaml` and `scope-registry.yaml`.
- New tool pin: `eng/release/toolchain.lock.json` inside the promoted bundle.
- New trust policy: `eng/release/trust/release-signing-policy.yaml` and `allowed-signers`.
- New renderer config: packaged `eng/release/cliff.toml` with no provider, command, URL, or environment lookup.
- No product `appsettings`, secret-provider, Aspire, Compose, database, or API configuration changes.

### Operations

- Release operators receive explicit `validate-commits`, `prepare`, `verify-candidate`, `verify-tag`, `verify-main`, and `verify-tools` commands.
- Tooling emits stable exit codes and bounded remediation text; it does not run as a service or expose health endpoints/metrics.
- Protected forge settings, signer rotation, bundle promotion, embargo handling, rerun, ref-race recovery, and tag incident response live in the release runbook.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- **Trust boundary:** Candidate checkout is untrusted data. Final attestation executes only a previously promoted bundle.
- **Authorization:** SSH allowed-signers policy authorizes release principals. Independent approval remains protected-environment/release evidence, not a claim inferred from a signature alone.
- **Credentials:** Candidate jobs receive no tag, publication, package-write, OIDC, deployment, or registry-write credentials.
- **Injection:** Commit subjects, trailers, fragment strings, and summaries are length-bounded, normalized, and escaped/rejected before Markdown/JSON output.
- **Privacy:** Canonical context, notes, logs, and evidence omit author/committer names/emails, provider usernames, raw bodies, and unbounded exception text.
- **Embargo:** Restricted security metadata remains outside the public checkout and normal artifacts until disclosure authorization.
- **Abuse/failure:** Shallow history, ref replacement, tag substitution, policy tampering, branch races, and malicious renderer config fail closed.
- **No product auth change:** API/BFF authentication, Cerbos authorization, rate limiting, HAL affordances, tenant isolation, and user idempotency are not affected.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Reason |
|---|---|---|
| Multi-tenancy | Not Applicable | Release engineering operates on repository history, not tenant data. |
| Federation | Not Applicable | No AT Protocol or cross-instance runtime contract changes. |
| Localization | Not Applicable initially | Canonical release notes are English; Unicode normalization preserves valid text. Translated notes would be a separate derived publication contract. |
| Accessibility | Applicable | Generated Markdown uses semantic headings, readable section order, meaningful link text in enriched views, and no invisible ordering tricks. |
| Product | Applicable | Public categories/scopes and three-layer notes must describe user, integrator, and operator outcomes rather than code layers. |
| Self-hosting | Applicable | Upgrade, configuration, migration, security, and rollback impacts are mandatory fragment/release evidence where relevant. |

## 11. Observability And Operations

The CLI should emit one bounded result per validation category with stable failure codes such as `invalid_commit`, `missing_fragment`, `wrong_release_line`, `untrusted_tool`, `unauthorized_signer`, `branch_moved`, and `nondeterministic_output`. Logs must not contain commit bodies, author identities, secrets, restricted security text, provider tokens, or raw external process output without redaction.

Evidence artifacts are the operational observability surface:

- normalized context;
- rendered note and tag message;
- candidate/final manifests;
- checksum manifest;
- toolchain promotion and signature verification evidence;
- provider adapter summary with expected/observed full OIDs.

No background process, metrics endpoint, trace pipeline, or health check is justified.

## 12. Migration And Compatibility Plan

- **Database/schema/data:** Not applicable.
- **Product API/generated client:** Not applicable.
- **Release documents:** Prospective cutover only. Existing semantic-version files stay available and are marked as legacy planning/history. New generated releases use `docs/releases/`.
- **Commit policy:** Strict validation begins only at the signed baseline. Earlier history is never rewritten or retroactively failed.
- **Tags:** The signed non-release baseline is ignored by strict SemVer release selection. No existing tags require migration because none exist.
- **Prereleases:** New policy allows only `alpha.N`, `beta.N`, and `rc.N` with contiguous counters and no build metadata.
- **Rollback:** Before activation, disable the adapter/required check and continue the current manual release checklist. After a tag is published, never rewrite it as rollback; publish a corrective release and retain incident evidence.
- **Compatibility shims:** None. The prior report architecture has not shipped.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Candidate validates malicious changes to its own release tooling | High without control | Critical | Previously promoted bundle; no candidate code in final lane | Bundle/source digest mismatch | 3.3 |
| Release note approved at `A` but tag points to replacement `B` | High without control | Critical | Final preparation commit and candidate validation both at exact `B`; FF-only CAS | Branch/head/candidate OID mismatch | 4.3 |
| Tool/policy/trust lock in candidate changes final result | Medium | Critical | Package all authoritative inputs in trusted bundle | Candidate versus trusted digest report | 3.3 |
| Circular tag/manifest hashing | Medium | High | Separate pretag candidate and post-tag final manifests | Manifest cannot resolve expected digest chain | 5.1 |
| git-cliff stable release lacks reviewed capability | Medium | High | Capability proof before pin/promotion | `verify-tools`/renderer fixture failure | 1.3, 4.1 |
| Output differs across OS, locale, clock, or Git config | Medium | High | Explicit canonicalization and cross-platform fixtures | Checksum mismatch | 3.2, 4.3 |
| Commit text injects Markdown/HTML or bidi confusion | Medium | High | Normalize, bound, escape/reject, deterministic fuzz corpus | Sanitizer test/failure code | 3.2 |
| Security embargo leaks through public artifacts | Low/Medium | Critical | Restricted external input lane and disclosure gate | Restricted-field canary appears in public output | 3.3 |
| Parallel release line selects wrong tag or moves `main` backward | Medium | High | Topological line validation and newest-stable rule | `wrong_release_line` / `main_regression` | 3.1, 5.2 |
| Fragment process becomes contributor burden | Medium | Medium | Require only for high-impact/grouped changes; simple commits stay fragment-free | High correction/supersession rate | 2.2 |
| Existing evidence bundle becomes a competing truth | Medium | High | Ingest/verify canonical manifest; mark collection metadata noncanonical | Version/SHA disagreement | 5.3 |
| Selected forge lacks required protected-ref or artifact features | Unknown | High | Keep core local/offline; record compensating control before adapter activation | Provider capability assessment fails | 6.1 |
| Signing key is lost or revoked | Low | Critical | Rotation history, multiple authorized release roles, explicit revocation procedure | Signer verification/availability failure | 3.3 |
| Dirty worktree causes unrelated edits to be overwritten | High in current checkout | High | Implementation agents touch only task-owned paths and record unrelated dirty files in each handoff | `git status --short` drift outside scope | All phases |

## 14. Success Metrics And Definition Of Done

The workstream is complete only when:

1. `release-context.v1.json`, `release-notes.md`, candidate manifest, tag message, and final evidence are deterministic for the same inputs.
2. Windows and Linux fixture checksums match.
3. Candidate release-engine/config/policy/trust changes cannot influence authoritative attestation until separately promoted.
4. Commit `B`, release-line head, signed tag target, committed note, and candidate/final evidence agree on the full object ID.
5. Every breaking/high-impact change is represented and no breaking change can be skipped.
6. The selected version satisfies ISLAMU SemVer, prerelease, and release-line policy independently of git-cliff.
7. An authorized SSH signature and immutable tag object ID are verified locally without forge APIs.
8. `main` can advance only by normal fast-forward to the newest stable tag commit and never for prereleases or older-line patches.
9. Canonical checksums are identical through the local reference and selected non-GitHub adapter.
10. Existing evidence bundling consumes one canonical release identity rather than creating another.
11. `develop` receives no generated Unreleased changelog writes.
12. The exact git-cliff dependency, license obligations, checksums, notices, and promotion evidence are retained.
13. All six phase gates pass, task/context ledgers are current, and the final release runbook is usable without GitHub-specific assumptions.

## 15. Implementation Agent Contract - KEEP DEV DOCS CURRENT

1. At first implementation start, read all three workstream files once. On a cold resume, read context and tasks first, then only the current phase or changed decision in this plan.
2. Do not reread unchanged artifacts after every task.
3. Start from the highest-priority unchecked task unless the user overrides it.
4. Treat `git-cliff-release-engineering-tasks.md` as the hot ledger. Check a substantial task immediately after its acceptance criteria are met; reconcile small related tasks no later than phase end.
5. Keep implementation and phase-verification checkboxes separate. A phase is complete only after its one Release build and selected one-project test pass.
6. Update completed count, current priority, next slice, discovered/deferred work, and `Last Updated` whenever task state changes.
7. Update context after a completed phase, meaningful decision, blocker, failed validation, material discovery, or before pause/compaction/transfer.
8. Update this plan only when scope, architecture, phase order, acceptance criteria, risk, or validation strategy changes.
9. Record a failed check with its cause and next recovery action; never mark the phase complete.
10. Before pause, transfer, PR, or compaction, reconcile the affected tasks and add a dated context handoff naming unrelated dirty files to avoid.
11. Run phase checks only after all phase tasks. Do not repeat a successful command whose inputs have not changed, and do not start the application/browser/Docker/Aspire.
12. Never claim release automation is active when the repository, provider settings, trusted bundle, or task ledger disagrees.

Every implementation summary must teach what changed, why, the release trust/data flow, key files and commands, security/reliability controls, exact verification, remaining work, and dev-doc status.

## 16. Progress Reporting Contract

After each implementation slice, report:

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: tasks yes/no; context/plan updated or unchanged with reason
```

## 17. Potential Risks & Unknowns

The hardest part is the trusted release-engine bootstrap, not Markdown generation. A final job is trustworthy only if the executable, policy, renderer config, git-cliff pin, and allowed signers all come from a promotion boundary the candidate cannot rewrite. The first implementation review should challenge that boundary before investing in templates or forge YAML. The only future user decision that blocks the last phase is which non-GitHub forge will host the first adapter; it does not block Phases 1-5.
