<!-- ABOUTME: Executable implementation plan for provider-neutral release engineering and git-cliff rendering. -->
<!-- ABOUTME: Re-baselines the changelog report around trusted tooling, exact Git objects, and deterministic evidence. -->

# Git-Cliff Release Engineering - Implementation Plan

Last Updated: 2026-08-23 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Implement the CTO-reviewed git-cliff design for automated, highly curated changelogs on governed release lines while remaining independent of any Git hosting provider.
- **Task directory:** `dev/active/git-cliff-release-engineering/`
- **Planning status:** Implemented through Phase 8 except the operator-blocked Task 8.2 and the deliberately deferred Task 8.4.
- **Primary intent:** `ci-cd-change` from `.agents/contract/intents.yaml`.
- **Cross-cutting guardrail:** `ip-clean-room`, required by the CI/CD intent because git-cliff is a third-party build dependency.
- **Planning skill:** `implementation-plan`.
- **Islamic value-sensitive design:** [`islamic-value-sensitive-design/i-vsd-release-governance.md`](../../../islamic-value-sensitive-design/i-vsd-release-governance.md) — public-record truthfulness (Task 8.3), embargo disclosure timing (Task 3.3), contributor recognition versus privacy (Task 3.2), and offline tag verifiability as a stakeholder guarantee (Phase 7).
- **Future implementation guidance:** `ip-clean-room`, `conventional-commit`, `.agents/rules/ip-clean-room.md`, and the amended `ci-cd-change` contract created by Task 1.1.
- **Primary layers:** DevOps/build tooling, release governance, documentation, and tests. Product Domain, Application, Persistence, API, and Blazor runtime behavior are out of scope.
- **Complexity:** XL. The work spans a trusted-tool bootstrap, Git object and signature validation, deterministic cross-platform serialization, SemVer and parallel release-line policy, restricted security-release handling, a third-party renderer, evidence integration, forge adapters, tag-anchored attestation, and a noncanonical publication projection.
- **Estimated delivery:** Eight reviewable phases, approximately 16-22 focused engineering days plus release-key, artifact-store, and forge configuration approvals.
- **Review status:** Senior CTO review on 2026-08-23 re-anchored release identity from mutable branch refs to immutable tag objects. Phase 7 delivered that correction: `GitReleaseValidationRequest` now accepts only immutable inputs, `releaseBranchRef` and `releaseLineHeadOid` are gone from `release-candidate.v1.json` and `release-evidence.v1.json`, `prepare` derives its range end from the checked-out `HEAD`, `verify-main` forward-port validation derives its target from the release tag, and a new `ReleaseRefNamespacePolicy` reserves `refs/heads/v*` for version tags while `release/<major>.<minor>` becomes the maintenance grammar.

### Contract drift discovered during planning

`AGENTS.md` and the planning skill still name `.claude/contract/intents.yaml`, while the verified canonical catalog is `.agents/contract/intents.yaml`. The current `ci-cd-change` intent also omits the future `eng/release/**`, `docs/releases/**`, release-engine test, solution, and shared adapter paths. Task 1.1 must correct this contract before implementation expands into those paths.

## 1. Executive Summary

ISLAMU Event will gain a small, tested .NET release-engineering CLI that owns release policy and emits a sanitized, versioned `release-context.v1.json`. A pinned git-cliff binary will consume that context only to render Markdown. Final release attestation will run with a previously promoted release-engine bundle, not code or policy supplied by the candidate branch.

The release model is **tag-anchored**. A release is a signed annotated tag object, not a branch position:

- **The tag is the release.** `refs/tags/v<major>.<minor>.<patch>` is the sole immutable release identity. Prereleases may use `-alpha.N`, `-beta.N`, or `-rc.N` under the policy defined in Phase 2. Every canonical fact about a release is reachable from the tag object alone: tag object ID, signer, target commit `B`, and the `release.yaml`, `summary.md`, `release-notes.md`, and `release-context.v1.json` committed at `B`.
- **Branches are workspaces, never identity.** A branch is a place to build `B` and, later, a place to build the next patch. It carries no information that the tag does not already fix. Any branch on a released line is fully reconstructible with `git switch -c release/<major>.<minor> v<major>.<minor>.<patch>`, so it may be deleted and recreated without weakening any release.
- **`develop`** remains the default integration branch and receives no continuously generated changelog.
- **Maintenance lines are lazy.** No `release/<major>.<minor>` branch is created at release time. One is opened only when a backport to an already-released line is actually required, and its only legal source is a verified signed stable tag on that line. This keeps the ref namespace proportional to real maintenance demand instead of growing one permanent branch per minor version.
- **`main` is a derived convenience pointer, not authority.** The newest stable release is a pure function of the tag set (the highest non-prerelease SemVer tag). `main` exists so that forge landing pages and shallow clones show released code, and it is verified as a fast-forward to that computed target. It is never an input to attestation, and it is never the answer to "which commit is release X".
- Every release still ends at one reviewed preparation commit `B`, and the candidate attestation, signed tag target, and committed release note all identify `B`. The release-line branch head is deliberately **not** part of that identity set.

### Attestation and mutation are separate authorities

This split is the load-bearing correction of the model:

| Concern | Reads | May read mutable refs |
|---|---|---|
| **Attestation** (`verify-candidate`, `verify-tag`, `verify-release`) | Tag object, commit `B`, tree contents at `B`, ancestry from the base tag | **No.** Reading `refs/heads/*` is a defect. |
| **Mutation** (`verify-main`, adapter push/publish actions) | Observed remote ref plus the computed target | Yes, and only as an expected-old/expected-new compare-and-swap on the action itself. |

The consequence is the property the whole workstream exists to provide: **any release can be re-verified, byte for byte, years later, on a clone that fetched only that tag** — with the line branch moved, deleted, or never created.

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
3. The reviewed release commit, signed tag target, committed note, and candidate evidence must identify the same full Git commit object `B`. The release-line branch head is explicitly excluded from this identity set.
3a. Attestation commands must resolve a release exclusively from immutable objects: the tag object, its target commit `B`, the tree at `B`, and ancestry from the recorded base tag. An attestation path that reads `refs/heads/*` is a defect, because it makes a valid past release unverifiable once the branch moves, is deleted, or was never fetched.
3b. Compare-and-swap on a mutable ref is permitted only as a precondition of a mutating action (`main` fast-forward, adapter push). It must never be a precondition of verifying an existing release.
3c. Release version tags own the `v*` ref glob. No branch may be named to match `refs/heads/v*`, so that no branch can ever become ambiguous with a version tag.
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

### Decision 10: First governed release as `v0.1.0` via signed prospective baseline

- **Why:** 11 months of development (2,164+ commits) represent an initial pre-release state without historical release tags or established breaking-change constraints. Tagging `v0.1.0` preserves SemVer 0.x breaking change freedom while anchoring the 11-month achievement in a comprehensive milestone summary (`summary.md`).
- **Alternatives considered:** Naively parsing 2,164 historical commits with git-cliff (rejected: 144 non-conforming commits, internal scope leaks, unreadable 50-page notes); tagging `v1.0.0` immediately (rejected: premature API freeze before real-world self-hosted feedback).
- **Consequences:** An operator-created `changelog-baseline-YYYY-MM-DD` tag marks the prospective cutover. `v0.1.0` is the first official release, capturing historical scope in `summary.md` and only release-line diffs in `release-notes.md`. Future releases (`v0.2.0`, `v1.0.0`) automate changelog diffs seamlessly.
- **Files/layers affected:** `docs/releases/v0.1.0/`, `docs/releases/baselines/`, `BaselineCommand.cs`, `docs/RELEASE_POLICY.md`.

### Decision 11: Lazily opened `release/<major>.<minor>` maintenance branches, never eager provisioning

This decision **supersedes** the earlier "automated `v<major>.<minor>` release line branch provisioning" design, which was rejected in the 2026-08-23 CTO review.

- **Why:** A maintenance branch carries no information a tag does not already fix. `git switch -c release/0.1 v0.1.3` reconstructs it exactly, at any time, from immutable objects. Creating one permanent branch per minor release therefore buys nothing and costs a ref namespace that grows without bound — the failure mode visible in mature forge repositories that accumulate hundreds of branches against a much smaller, meaningful set of tags. Branch count should track *active maintenance demand*, which for a pre-v1 project is normally zero.
- **Alternatives considered:**
  - *Eager `v<major>.<minor>` branch cut from `develop` at release time (the rejected prior design).* Rejected twice over. It is **unsound**: a branch cut from `develop` after `B` was tagged contains commits that were never in the release, so any hotfix built on it ships unreviewed integration work under a patch version. It is also **unnecessary**, per the reconstruction argument above.
  - *One branch per patch tag (`release/v0.1.0`).* Rejected: strictly worse ref growth for strictly less value than the tag already provides.
  - *No maintenance branches at all.* Rejected only because a real backport needs somewhere to accumulate more than one commit before the next patch tag.
- **Consequences:**
  - Nothing is provisioned at release time. The default state after `v0.1.0` is: one tag, zero new branches.
  - `open-maintenance-line` is an operator-invoked, idempotent runbook action. Its **only** legal source is a verified signed stable tag on the target line; `develop`, `main`, and arbitrary commits are rejected.
  - The branch is named `release/<major>.<minor>` — no `v` prefix — so version tags keep sole ownership of the `v*` glob (Constraint 3c).
  - The branch is disposable. Deleting a maintenance branch after its final patch is a supported, non-destructive cleanup, because every release on it remains fully verifiable from its tag.
- **Files/layers affected:** `docs/RELEASE_RUNBOOK.md`, `docs/RELEASE_POLICY.md`, `eng/release/src/ISLAMU.ReleaseEngineering/GitRepositoryValidator.cs`, `ReleaseInputPolicy.cs`.

### Decision 12: Forge release pages are a noncanonical projection with drift detection, not a synchronized copy

- **Why:** Self-hosters and contributors across GitHub, Codeberg/Forgejo, and Tangled benefit from populated release pages with archives and evidence. But a forge release body is **mutable, unsigned, and editable by any maintainer or by the forge itself**. Any acceptance criterion of the form "published bodies match canonical notes" is unenforceable by construction, and asserting it would make the weakest surface in the system look like an invariant.
- **Alternatives considered:** Treat published bodies as canonical (rejected: unsigned mutable state cannot be release truth); refuse all publication (rejected: real ergonomic loss for self-hosters); best-effort publish with no verification (rejected: silent divergence is exactly the failure being guarded against).
- **Consequences:**
  - Publication is explicitly a **derived, noncanonical view**. Canonical truth remains the signed tag plus the notes committed at `B`.
  - Each published page must carry the canonical `release-notes.md` hash and a pointer to the tag, so any reader can verify the page against the repository.
  - A `report-publication-drift` check compares each published body against the canonical hash and **reports**; it does not attempt automated repair, and drift never invalidates the release.
  - Assets (`release-evidence.v1.json`, `artifacts.sha256`, container image digests, SBOM) are attached because they are self-verifying; forge-generated `.zip`/`.tar.gz` archives are linked but never treated as reproducible artifacts.
- **Files/layers affected:** `.ci/providers/**`, `.ci/release/adapter-contract.md`, `.ci/scripts/generate-release-evidence-bundle.cs`.

### Decision 13: Attestation reads only immutable objects; mutation owns compare-and-swap

- **Why:** The workstream's core promise is that a release can be verified offline, provider-independently, and indefinitely. That promise fails the moment verification depends on a ref that a later release is *supposed* to move. The current implementation resolves `refs/heads/<line>` inside `GitRepositoryValidator` and re-checks it in `CandidateCommand`, so `v0.1.0` stops verifying as soon as `v0.1.1` is prepared, and never verifies at all on a tag-only clone.
- **Alternatives considered:** Record the branch head in the manifest and compare it later (rejected: still fails on a moved or absent branch, and the recorded value proves nothing a tag does not); relax the check to a warning (rejected: fail-open in a trust boundary).
- **Consequences:** `verify-candidate` and `verify-tag` drop all `refs/heads/*` reads. Branch-head compare-and-swap survives only inside the mutating step — the `main` fast-forward proposal and adapter push preconditions — where a stale ref genuinely is a race.
- **Files/layers affected:** `GitRepositoryValidator.cs`, `CandidateCommand.cs`, `TagCommand.cs`, `MainCommand.cs`, and their fixtures.

### Decision 14: `main` is derived from tags and verified as drift, not stored as identity

- **Why:** "Newest stable release" is already a total function of the tag set. Storing it additionally in a mutable branch creates a second value that can disagree with the first, and then requires its own race detection, backward-movement rules, and reconciliation task to keep the copy honest.
- **Alternatives considered:** Drop `main` entirely (rejected: forge default-branch and shallow-clone ergonomics are real); keep `main` authoritative (rejected: it is the duplicate, not the source).
- **Consequences:** `verify-main` computes the expected target from the highest reachable stable tag, then proposes a fast-forward with explicit expected-old and expected-new OIDs. It reports drift; it never defines which commit a release is. Prereleases and older-line patches continue to leave `main` untouched, but now because they do not change the computed newest-stable tag, not because of a separately maintained rule.

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

### Phase 6: Provider Adapters And Prospective Cutover

- **Goal:** Expose one provider-neutral command contract, add the selected forge adapters, and establish the signed prospective baseline. Activation moved to Phase 8 because it must dry-run the corrected tag-anchored model, not the superseded branch-anchored one.
- **Depends on:** Phase 5 and maintainer selection of a forge before Task 6.1 finishes.
- **Related skills/rules:** `ci-cd-change`, `ip-clean-room`, `conventional-commit`.
- **Acceptance criteria:** Two adapters/local invocations can produce identical canonical checksums from the same Git objects; the signed non-release baseline is approved.
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

> Task 6.3 (advisory activation dry run) moved to Task 8.1. It must exercise the corrected tag-anchored model delivered in Phase 7; running it against the superseded branch-anchored flow would certify the wrong invariants.

> **Delivered addition (Task 8.3):** provider definitions gained a machine-checked `publicationWorkflows` field alongside `discoveryWorkflows`. Publication is a separate surface from adapter transport discovery, and stating its contract in prose alone would leave the weakest surface in the system unchecked. `.ci/release/provider-definition.schema.json` and `.ci/scripts/validate-release-provider-adapters.cs` now enforce trusted origin, canonical-hash and tag-reference presence, self-verifying assets, pinned actions, and an evidenced recorded no-op for providers without a release API.

### Phase 7: Tag-Anchored Release Identity Correction

- **Goal:** Remove mutable-branch reads from every attestation path so that any release verifies from its tag object alone, indefinitely, on a clone that fetched only that tag.
- **Depends on:** Phase 5 (the code being corrected) and Phase 6 Task 6.1.
- **Related skills/rules:** release policy ADR, `ci-cd-change`.
- **Acceptance criteria:** A release verifies after its line branch has moved, after the branch has been deleted, and on a tag-only clone; branch compare-and-swap survives only in mutating steps; no branch may occupy `refs/heads/v*`.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** The correction is confined to the unreleased release engine and its policy docs. No published tag, ref, or release exists yet, so rollback is a revert of the owning commits.

#### Task 7.1 (Red Phase): Author Failing Tag-Anchored Re-Verification Specifications

- **Type:** create
- **Layer:** Tests
- **Files:** new `eng/release/tests/ISLAMU.ReleaseEngineering.Tests/TagAnchoredReVerificationTests.cs`; extend existing signed-tag and candidate fixtures.
- **Description:** Author failing specification tests, before touching production code, that pin the durability property the model is supposed to have. Each test builds a disposable repository, produces and signs a release, then mutates the *branch* environment and requires verification to still succeed. These must fail against current `HEAD` for the documented reasons (`git_candidate_not_release_branch_head`, `git_missing_object:release_branch_head`), proving they test the defect and not the implementation.
- **Acceptance Criteria:**
  - [ ] `verify-tag` and `verify-candidate` succeed for release `N` after release `N+1` has advanced the line branch.
  - [ ] Both succeed after the line branch is deleted outright.
  - [ ] Both succeed in a clone created with tag-only fetch, where `refs/heads/<line>` never existed.
  - [ ] Both still fail closed on the real defects: wrong tag target, unsigned/unauthorized/recreated tag, note or context drift at `B`, non-ancestor base tag, non-linear range, and a terminal commit lacking the governed release-metadata skip.
  - [ ] A branch named `v0.1` alongside tag `v0.1.0` is rejected by policy rather than resolved ambiguously.
  - [ ] Cases are asserted in both SHA-1 and SHA-256 repositories.
- **Dependencies:** 5.1, 5.2.
- **Effort:** M
- **Required Skills/Rules:** release policy ADR.

#### Task 7.2 (Green Phase): Remove Branch Reads From Attestation

- **Type:** modify
- **Layer:** DevOps / Security
- **Files:** existing `eng/release/src/ISLAMU.ReleaseEngineering/GitRepositoryValidator.cs`, `CandidateCommand.cs`, `TagCommand.cs`, `MainCommand.cs`.
- **Description:** Split `GitReleaseValidationRequest` into an attestation request that accepts only immutable inputs (candidate OID, base tag ref, previous published tag ref, selected version, line label) and a separate mutation precondition that carries observed ref state. Delete `ReleaseBranchRef`/`ReleaseLineHeadOid` from the attestation path and from `release-candidate.v1.json`; delete the `git_candidate_not_release_branch_head` re-checks in `CandidateCommand` and the live line read in `MainCommand.ValidateForwardPort`. Replace the removed topology coverage with equivalent immutable checks: ancestry from the base tag, linearity of the range, terminal-commit contract at `B`, and the line label matching the selected version's major/minor.
- **Acceptance Criteria:**
  - [ ] No attestation code path invokes `rev-parse`, `for-each-ref`, or `merge-base` against `refs/heads/*`.
  - [ ] `release-candidate.v1.json` and `release-evidence.v1.json` contain no branch ref or branch head OID field.
  - [ ] Task 7.1 specifications pass; every pre-existing fail-closed case still fails closed.
  - [ ] Forward-port validation derives its target from the release's own tag rather than the line branch.
  - [ ] `verify-main` retains expected-old/expected-new compare-and-swap and still never pushes.
- **Dependencies:** 7.1.
- **Effort:** L
- **Required Skills/Rules:** release policy ADR, trusted bundle contract.

#### Task 7.3: Re-Baseline Ref Namespace, Policy, And Runbook

- **Type:** modify
- **Layer:** Docs / DevOps
- **Files:** existing `docs/RELEASE_POLICY.md`, `docs/RELEASE_RUNBOOK.md`, `docs/adr/ADR-025-provider-neutral-release-governance.md`, `docs/CI_CD_GOVERNANCE.md`, `eng/release/src/ISLAMU.ReleaseEngineering/ReleaseInputPolicy.cs`, `.ci/release/adapter-contract.md`.
- **Description:** Correct the normative statement in `docs/RELEASE_POLICY.md` that currently requires the release-line head to equal `B`. Rename the branch grammar from `v<major>.<minor>` to `release/<major>.<minor>`, keep the `Line` descriptor field as a pure version-line *label* rather than a branch reference, and record the protected-ref rule reserving `refs/heads/v*` against creation. Document in the runbook that maintenance branches are opened on demand from a verified tag and may be deleted afterwards.
- **Acceptance Criteria:**
  - [ ] `docs/RELEASE_POLICY.md` states that the tag object is the sole release identity and that attestation must not read mutable refs.
  - [ ] The `Line` descriptor field is documented as a label; nothing derives a branch ref from it.
  - [ ] A protected-ref rule rejecting `refs/heads/v*` is recorded with the provider settings evidence.
  - [ ] The runbook documents opening and deleting a maintenance line, including the "reconstruct from tag" command.
  - [ ] ADR-025 records the superseded branch-anchored model and why it was replaced.
- **Dependencies:** 7.2.
- **Effort:** M
- **Required Skills/Rules:** release policy ADR, `ci-cd-change`.

### Phase 8: Activation, First Governed Release, And Publication Projection

- **Goal:** Dry-run the corrected model, ship the first governed release `v0.1.0` from the runbook, and only then automate publication and lazy maintenance lines.
- **Depends on:** Phase 7.
- **Related skills/rules:** `ci-cd-change`, `conventional-commit`, provider adapter contract.
- **Acceptance criteria:** The synthetic flow passes end to end against tag-anchored verification; `v0.1.0` exists as a signed tag with deterministic three-layer notes and is verifiable offline without any branch; publication is a reporting projection that cannot alter canonical checksums.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Publication and maintenance-line automation are additive and independently disableable. If either fails, the release itself is unaffected because it is already closed by its signed tag.

#### Task 8.1: Activate Contributor And Release Gates Through An Advisory Dry Run

- **Type:** modify/create
- **Layer:** DevOps / Docs
- **Files:** existing `docs/CONTRIBUTING.md`, `.agents/skills/conventional-commit/SKILL.md`, `docs/RELEASE_CHECKLIST.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, provider adapter definitions, architecture tests; new synthetic/advisory evidence under the active workstream as allowed by clean-room policy.
- **Description:** Align contributor scopes, fragments, skip reasons, breaking metadata, and backport identity. Run the complete candidate-at-`B`, signed test tag, final evidence, and main-verification flow against a synthetic or disposable local repository, **including the Task 7.1 durability cases**. Promote checks from advisory to required only after deterministic evidence and protected provider settings are recorded. Do not generate changelogs on ordinary `develop` pushes.
- **Acceptance Criteria:**
  - [ ] All mandatory verification cases are represented in release-engine or architecture fixtures.
  - [ ] The dry run re-verifies an earlier synthetic release after the line branch has moved and after it has been deleted.
  - [ ] Required checks remain always present or have a documented no-op path.
  - [ ] Tag/`main` protections, the `refs/heads/v*` rejection rule, and signer roles have retained provider settings evidence.
  - [ ] The manual release checklist remains the approval source; automation removes no governance gate.
- **Dependencies:** 6.2, 7.3.
- **Effort:** L
- **Required Skills/Rules:** `conventional-commit`, `ci-cd-change`, `ip-clean-room`.

#### Task 8.2: First Governed Milestone (`v0.1.0`) Execution And Verification

- **Type:** operator action / verify
- **Layer:** Operations / Governance
- **Files:** new `docs/releases/v0.1.0/release.yaml`, `docs/releases/v0.1.0/summary.md`, `docs/releases/baselines/changelog-baseline-*.v1.json`; existing `docs/RELEASE_RUNBOOK.md`.
- **Description:** Execute the prospective baseline cutover, author the milestone summary for `v0.1.0`, and run `prepare`, `verify-candidate`, `verify-tag`, and `verify-main` from the runbook. **No publication automation and no maintenance branch are required to complete this task** — the release is closed by its signed tag.
- **Acceptance Criteria:**
  - [ ] `changelog-baseline-YYYY-MM-DD` is verified and recorded in baseline evidence.
  - [ ] `v0.1.0` preparation produces deterministic `release-notes.md` with all three layers.
  - [ ] Candidate `B` passes full attestation with no branch input.
  - [ ] The tag re-verifies in a fresh tag-only clone, offline, with no forge API involved.
  - [ ] No `release/0.1` branch is created, proving lazy maintenance lines are genuinely optional.
- **Dependencies:** 6.2, 8.1.
- **Effort:** M
- **Required Skills/Rules:** `conventional-commit`, release policy ADR.

#### Task 8.3: Publication Projection And Drift Reporting

- **Type:** create/modify
- **Layer:** DevOps / CI-CD
- **Files:** new `.github/workflows/release-publish.yml`, `.ci/providers/forgejo-codeberg/release-publish.yml`, `.ci/providers/tangled/` publication definition; existing `docs/RELEASE_RUNBOOK.md`, `.ci/release/adapter-contract.md`.
- **Description:** Publish the canonical `release-notes.md` to GitHub, Forgejo/Codeberg, and Tangled release pages as an explicitly noncanonical projection, attach self-verifying assets, and link forge-provided source archives. Add `report-publication-drift`, which compares each published body against the canonical notes hash and reports.
- **Acceptance Criteria:**
  - [ ] Every published page carries the canonical notes hash and its tag reference.
  - [ ] Attached assets include `release-evidence.v1.json`, `artifacts.sha256`, container image digests, and SBOM.
  - [ ] Publication runs only in the trusted final lane and cannot be triggered by unprivileged candidate code.
  - [ ] Drift is reported, never auto-repaired, and never invalidates the release.
  - [ ] A forge outage or a provider lacking release APIs degrades to a recorded no-op with operator evidence, not a failed release.
- **Dependencies:** 6.1, 8.2.
- **Effort:** L
- **Required Skills/Rules:** `ci-cd-change`, provider adapter contract.

#### Task 8.4 (Deferred Until Demanded): Lazy Maintenance-Line Opening

- **Type:** create/modify
- **Layer:** DevOps / CI-CD
- **Files:** new `open-maintenance-line` runbook procedure and optional provider workflow; existing `docs/RELEASE_RUNBOOK.md`.
- **Description:** Implement the idempotent `open-maintenance-line` action described in Decision 11. **Do not implement this task until a real backport to an already-released line is required.** Until then the documented manual command in the runbook is the complete solution.
- **Acceptance Criteria:**
  - [ ] The only accepted source is a verified signed stable tag on the target line; `develop`, `main`, and arbitrary commits are rejected.
  - [ ] The created branch is named `release/<major>.<minor>` and never matches `refs/heads/v*`.
  - [ ] Re-running against an existing branch is a no-op and never force-updates.
  - [ ] Deleting the branch afterwards leaves every release on that line fully verifiable.
- **Dependencies:** 8.2, plus a real backport requirement.
- **Effort:** M
- **Required Skills/Rules:** `ci-cd-change`.

## 7. Testing Strategy

Each phase runs one Release build and at most one project test command after all phase tasks. The release-engine test project owns deterministic unit tests plus synthetic Git repository fixtures. It may invoke the promoted git-cliff binary only in the rendering fixture category; policy tests remain independent of git-cliff.

Required case families include:

- commit types/scopes, skip reasons, breaking redundancy, fragment requirement, grouping, backports, and impact coverage;
- pre-1.0/post-1.0 SemVer, prerelease continuity, stable promotion, and branch/version mismatch;
- shallow/partial clones, missing objects, replace refs, grafts, ambiguous/lightweight/wrong-line tags, and parallel release lines;
- Markdown/HTML/control/bidi/length attacks and short-ID collisions;
- candidate engine/config/policy/trust changes that cannot affect trusted attestation;
- candidate `B` preservation, branch movement races, squash/merge/rebase replacement, and exact regeneration;
- **tag-anchored durability:** re-verifying release `N` after release `N+1` moved the line branch, after the branch was deleted, and in a tag-only clone where the branch never existed;
- **branch/tag namespace safety:** a branch matching `refs/heads/v*` is rejected rather than resolved ambiguously;
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
| Past release becomes unverifiable once its line branch moves, is deleted, or was never fetched | Certain in current code | Critical | Attestation reads only tag object, `B`, tree at `B`, and ancestry; branch CAS confined to mutating steps | `git_candidate_not_release_branch_head` or `git_missing_object:release_branch_head` on a valid release | 7.1, 7.2 |
| Branch named `v0.1` becomes ambiguous with tag `v0.1` | Low but permanent once it happens | High | Reserve `refs/heads/v*` by protected-ref rule; maintenance branches use `release/<major>.<minor>` | `git_ambiguous_ref` diagnostic; forge ref-picker confusion | 7.3 |
| Maintenance branch cut from `develop` ships unreleased work under a patch version | Medium under prior design | Critical | Only a verified signed stable tag may source a maintenance line | Branch base is not the tag's target commit | 8.4 |
| Ref namespace grows one permanent branch per minor release | High under prior design | Medium | Lazy opening on real backport demand; branches are disposable and tag-reconstructible | Branch count outgrowing active maintenance lines | 8.4 |
| Published forge release body silently diverges from canonical notes | Medium | Medium | Publication is a declared noncanonical projection carrying the canonical hash; drift is reported | `report-publication-drift` mismatch | 8.3 |

## 14. Success Metrics And Definition Of Done

The workstream is complete only when:

1. `release-context.v1.json`, `release-notes.md`, candidate manifest, tag message, and final evidence are deterministic for the same inputs.
2. Windows and Linux fixture checksums match.
3. Candidate release-engine/config/policy/trust changes cannot influence authoritative attestation until separately promoted.
4. Commit `B`, signed tag target, committed note, and candidate/final evidence agree on the full object ID. The release-line branch head is excluded by design.
4a. Every released tag re-verifies byte for byte after its line branch has moved, after that branch has been deleted, and in a clone that fetched only the tag. No attestation path reads `refs/heads/*`.
4b. No branch occupies `refs/heads/v*`, so a version tag can never be shadowed by a branch of the same name.
5. Every breaking/high-impact change is represented and no breaking change can be skipped.
6. The selected version satisfies ISLAMU SemVer, prerelease, and release-line policy independently of git-cliff.
7. An authorized SSH signature and immutable tag object ID are verified locally without forge APIs.
8. `main` can advance only by normal fast-forward to the commit computed from the highest reachable stable tag, and never for prereleases or older-line patches. `main` is reported as drift against that computed target, never consulted as release identity.
9. Canonical checksums are identical through the local reference and selected non-GitHub adapter.
10. Existing evidence bundling consumes one canonical release identity rather than creating another.
11. `develop` receives no generated Unreleased changelog writes.
12. The exact git-cliff dependency, license obligations, checksums, notices, and promotion evidence are retained.
13. All eight phase gates pass, task/context ledgers are current, and the final release runbook is usable without GitHub-specific assumptions.
14. A dated `islamic-value-sensitive-design/i-vsd-release-governance.md` is linked from `plan.md`, `context.md`, and `tasks.md`, covering public-record truthfulness, embargo disclosure timing, and the contributor-attribution trade-off.

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

The hardest part is the trusted release-engine bootstrap, not Markdown generation. A final job is trustworthy only if the executable, policy, renderer config, git-cliff pin, and allowed signers all come from a promotion boundary the candidate cannot rewrite. The first implementation review should challenge that boundary before investing in templates or forge YAML.

The second-hardest part, and the one the 2026-08-23 CTO review caught after implementation had already shipped 16 tasks, is subtler: **it is easy to build a system with perfect immutable evidence and then gate that evidence behind a mutable ref.** Phases 1-6 produce genuinely deterministic, signed, provider-neutral artifacts — and then `GitRepositoryValidator` refuses to confirm them unless a branch happens to still point at the right commit. Durability of verification is a distinct property from determinism of generation, and it has to be tested by *mutating the environment after the fact*, which no amount of golden-fixture testing will surface.

The remaining user decisions are: approval of the first governed version, real signer principals and custody, and whether any maintenance line is ever needed (Task 8.4 stays unbuilt until one is).
