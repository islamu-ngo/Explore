<!-- ABOUTME: Hot execution ledger for the provider-neutral git-cliff release-engineering implementation. -->
<!-- ABOUTME: Mirrors the approved plan phases, task acceptance, dependencies, and phase-end verification gates. -->

# Git-Cliff Release Engineering - Task Checklist

Last Updated: 2026-08-23 Europe/Brussels

## Status Summary

- **Overall status:** Active implementation. Phase 7 landed the tag-anchored release-identity correction; Phase 0 governance is delivered; Phase 8 activation work is in progress.
- **Completed:** 25/25 implementation tasks, plus the discovered `activate-trust` genesis tooling. Every phase's implementation work and both verification gates are done.
- **Current priority:** hand to the two key holders. Everything the engine can do is built, tested, and rehearsed end to end; what remains is key custody, which is a two-person act by design.
- **Next recommended slice:** the release operator and the tooling promoter each generate a key on their own machine and share only the public halves; run `activate-trust`; then execute the first-release runbook. The exact release flow is already proven by `FirstGovernedReleaseTests`.
- **Approval blocker:** CLEARED. `islamic-value-sensitive-design/i-vsd-release-governance.md` is linked from this file, `-plan.md`, and `-context.md`.
- **Current handoff:** `git-cliff-release-engineering-handoff.md` records exact current evidence, blockers, and continuation steps.
- **Forge selection:** The Project Steward selected Forgejo/Codeberg, Tangled, and GitHub on 2026-08-15.
- **Phase-gate exception:** On 2026-08-14 the user explicitly authorized Tasks 2-5 to continue while the Phase 1 architecture checkbox remains blocked by four unrelated shared-worktree failures.
- **Current Phase 1 gate evidence:** CLOSED 2026-08-23. 444 total, 443 passed, 0 failed, 1 skipped. The four previously-blocking product/architecture-contract failures were fixed by their owning workstreams; none was release-engineering scope.
- **Current Phase 5 gate evidence:** CLOSED 2026-08-23 with the literal commands and no workload-resolver workaround. Full Release build 0 errors; release-engine tests 216/216. The previously recorded authorization API compile drift in the shared tree is gone.
- **Phase 6 continuation exception:** On 2026-08-15 the Project Steward selected all three adapters and authorized continuation past the documented Phase 5 environment/shared-tree blockers.

## Implementation Maintenance Rules

- Read the full workstream once at initial implementation start. On resume, read context/tasks first and only the current plan sections.
- Do not reread unchanged artifacts after every task.
- Mark a substantial task `IN PROGRESS` when it will span meaningful work or a handoff; skip status churn for a tiny task completed immediately.
- Check a substantial task immediately after its acceptance criteria are met. Reconcile small related tasks no later than phase end.
- Add discovered work under its owning phase with acceptance criteria and dependencies; keep completed count, priority, next slice, deferred work, and date accurate.
- Check a phase complete only after all implementation tasks and both phase-verification checkboxes pass.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Update the plan only when scope, architecture, sequencing, acceptance, risk, or verification strategy changes.
- Run no build/test after individual tasks. Run the one Release build and selected one-project test once at phase end.
- Do not start the app, browser, Docker, Aspire, Playwright, Chrome DevTools, or live services.
- Preserve unrelated dirty files. Never restore, delete, stage, or absorb another workstream's changes.
- Sequence behavioral work test-first: author the failing specification task (Red) before the task that writes production code (Green).

## Phase 0: Governance Deliverable - COMPLETE

- [x] **0.1 Author The Release-Governance I-VSD Report**
  - **Files:** new `islamic-value-sensitive-design/i-vsd-release-governance.md`; link it from this file, `-plan.md`, and `-context.md`.
  - **Acceptance:** A dated report covers the three provider-controlled decisions this workstream actually owns — truthfulness of the public release record when a mutable forge page can diverge from signed notes; who bears the timing decision for embargoed security disclosure to self-hosters; and the deliberate trade of contributor recognition against contributor privacy when canonical notes strip identities. Each is traced to the plan task that implements the mitigation (8.3, 3.3, 3.2 respectively), with evidence limits and escalation boundaries stated. Scope it small — this is release tooling, not user-facing product behavior — but do not skip it, because all three are real provider choices, not ceremony.
  - **Effort:** S
  - **Dependencies:** None. Blocks Phase 8 activation.
  - **Delivered:** [`islamic-value-sensitive-design/i-vsd-release-governance.md`](../../../islamic-value-sensitive-design/i-vsd-release-governance.md) dated 2026-08-23. It scopes to the three provider-controlled decisions, traces public-record truthfulness to Task 8.3, embargo disclosure timing to Task 3.3, and identity stripping to Task 3.2, and adds a fourth finding recording offline tag verifiability as a stakeholder guarantee rather than an implementation preference. Validation gaps and escalation boundaries are stated.

## Phase 1: Governed Foundation And Tool Selection - COMPLETE

- [x] **1.1 Re-baseline Release Governance And Contribution Scope**
  - **Files:** existing `.agents/contract/intents.yaml`, `.gitignore`, `docs/CI_CD_GOVERNANCE.md`, `docs/RELEASE_CHECKLIST.md`, `docs/index.md`, `dev/report/git-cliff-changelog-automation-report.md`; new `docs/adr/ADR-025-provider-neutral-release-governance.md`, `docs/RELEASE_POLICY.md`, `docs/RELEASE_RUNBOOK.md`.
  - **Acceptance:** Planned paths are permitted and `eng/release/**` is not hidden by generic build-output ignores; ADR/policy/runbook own distinct stable concerns; the report retains provenance but no conflicting authority; current manual release behavior remains truthful.
  - **Effort:** M
  - **Dependencies:** None.

- [x] **1.2 Create The Minimal Release-Engine Project**
  - **Files:** existing `Explore.slnx`; new `eng/release/src/ISLAMU.ReleaseEngineering/**`, `eng/release/tests/ISLAMU.ReleaseEngineering.Tests/**`, project lock files, and `eng/release/README.md`.
  - **Acceptance:** One `net10.0` console project and one TUnit project build without product-project references; `verify-tools` and stable CLI errors are tested; no speculative service/plugin abstractions are added.
  - **Effort:** M
  - **Dependencies:** 1.1.

- [x] **1.3 Select, Record, And Promote The Git-Cliff Runtime**
  - **Files:** new `eng/release/toolchain.lock.json`, `docs/legal/dependencies/git-cliff.md`, `dev/active/git-cliff-release-engineering/git-cliff-dependency-handoff.md`; release-engine tool verification code/tests.
  - **Acceptance:** A released capability-complete git-cliff version has exact platform digests/license evidence; the implementation receives a sanitized handoff; wrong binaries fail; repository dependency-license validation passes or blocks explicitly.
  - **Effort:** M
  - **Dependencies:** 1.2.

### Phase 1 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors.
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — 444 total, 443 passed, 0 failed, 1 skipped. The four unrelated product/architecture-contract failures that blocked this checkbox since 2026-08-14 (DTO naming, generated-client ownership, tenant bypass-reason usage, User-PII inventory coverage) were fixed by their owning workstreams in the shared tree; none was release-engineering scope and none was modified here.

## Phase 2: ISLAMU Policy Engine And Normalized Context - COMPLETE

- [x] **2.1 Implement Commit, Scope, And Skip Policy**
  - **Files:** new `eng/release/policy/release-policy.yaml`, `eng/release/policy/scope-registry.yaml`, parser/validator code/tests; existing `.agents/skills/conventional-commit/SKILL.md`, `docs/CONTRIBUTING.md`.
  - **Acceptance:** Public/engineering scopes, release visibility, both breaking signals, explained skips, protected breaking changes, and malformed metadata are deterministic and fixture-proven.
  - **Effort:** L
  - **Dependencies:** 1.2.

- [x] **2.2 Implement Change Fragments And Release Descriptors**
  - **Files:** new typed YAML models/validators/tests; new `docs/releases/README.md`, `docs/releases/changes/README.md`.
  - **Acceptance:** High-impact/grouped changes have append-only fragments linked by `Change-Id`; releases fix line/version/date/range/impact dispositions; mutations, duplicates, missing evidence, and public embargo details fail.
  - **Effort:** L
  - **Dependencies:** 2.1.

- [x] **2.3 Implement Version, Prerelease, Backport, And Context Policy**
  - **Files:** new SemVer/range/context code and `release-context.v1.json` fixtures under `eng/release/`.
  - **Acceptance:** ISLAMU independently validates minimum bump, release line, cumulative prereleases, contiguous counters, stable promotion, backport identity, full OIDs, and collision-safe display IDs; context contains no identities/raw bodies.
  - **Effort:** L
  - **Dependencies:** 2.1, 2.2.

### Phase 2 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet`

## Phase 3: Git Trust, Determinism, And Security Boundaries - COMPLETE

- [x] **3.1 Implement Git Object And Release-Line Validation**
  - **Files:** new Git process/repository/graph code and synthetic Git tests under `eng/release/`.
  - **Acceptance:** Explicit full objects and ancestry are proven; shallow/partial/replaced/grafted/ambiguous/lightweight/wrong-line histories fail; parallel lines select only applicable tags; object format is not hardcoded.
  - **Effort:** L
  - **Dependencies:** 2.3.

- [x] **3.2 Implement Canonicalization And Untrusted-Text Hardening**
  - **Files:** new JSON/Markdown/text canonicalization code and deterministic security fixtures under `eng/release/`.
  - **Acceptance:** UTF-8-no-BOM/LF/NFC/invariant ordering is byte-identical across Windows/Linux and clocks; global Git config is isolated; Markdown/HTML/control/bidi/length attacks are escaped or rejected.
  - **Effort:** L
  - **Dependencies:** 3.1.

- [x] **3.3 Establish Trusted Bundle, SSH Signer, And Embargo Contracts**
  - **Files:** new `eng/release/trust/**`, trusted-bundle code/tests; existing `docs/RELEASE_RUNBOOK.md`, `docs/CI_CD_GOVERNANCE.md`, dependency/provenance records.
  - **Acceptance:** Final attestation verifies a previously promoted bundle; candidate source/policy/config/trust cannot self-promote; SSH signer roles/rotation/revocation/replaced-tag behavior are explicit; restricted security input cannot leak into public artifacts.
  - **Effort:** XL
  - **Dependencies:** 1.3, 3.1, 3.2.

### Phase 3 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet`

## Phase 4: Git-Cliff Rendering And Final Preparation Commit - COMPLETE

- [x] **4.1 Integrate Git-Cliff As Renderer Only**
  - **Files:** new packaged `eng/release/cliff.toml`, renderer adapter, and promoted-binary fixtures under `eng/release/`.
  - **Acceptance:** `--from-context --offline --no-exec` rendering works without Git/network; template contains presentation only; candidate config is not authoritative; canonical output has no provider/identity/body data.
  - **Effort:** L
  - **Dependencies:** 1.3, 2.3, 3.2, 3.3.

- [x] **4.2 Implement Release Composition And `prepare`**
  - **Files:** new `prepare` command/composition tests; new release directory templates; existing `docs/RELEASE_RUNBOOK.md`.
  - **Acceptance:** `release.yaml` plus `summary.md` produce fully generated `release-notes.md`; three layers and impact coverage are correct; no split markers exist; the emitted commit message has skip plus reason.
  - **Effort:** L
  - **Dependencies:** 2.2, 4.1.

- [x] **4.3 Implement Exact-`B` Candidate Attestation**
  - **Files:** new `verify-candidate` command, `release-candidate.v1.json` serializer, and exact-commit fixtures under `eng/release/`.
  - **Acceptance:** Final candidate validation occurs at `B`; `B` rerenders identically; replacement/movement/drift fails; deterministic candidate evidence contains all tool/policy/context/note hashes and no tag ID/current time.
  - **Effort:** L
  - **Dependencies:** 4.2.

### Phase 4 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet`

## Phase 5: Tag Closure, Stable Main, And Evidence Integration - COMPLETE

- [x] **5.1 Implement `verify-tag` And Final Evidence**
  - **Files:** new `verify-tag`, tag-message, `release-evidence.v1.json`, and signed-tag fixtures; existing `docs/RELEASE_RUNBOOK.md`.
  - **Acceptance:** Authorized SSH annotated tag, exact `B`, tag name/line/range, candidate digest, note hash, and tag object ID are verified locally; recreated tags are detectable; no hash cycle exists.
  - **Effort:** L
  - **Dependencies:** 3.3, 4.3.

- [x] **5.2 Implement `verify-main` And Parallel-Line Rules**
  - **Files:** new `verify-main` command/topology tests; existing `docs/RELEASE_RUNBOOK.md`.
  - **Acceptance:** Only newest stable `B` may be a normal fast-forward target for `main`; prereleases/older lines/races/non-descendants fail; expected old/new OIDs are emitted; the tool never pushes.
  - **Effort:** M
  - **Dependencies:** 5.1.

- [x] **5.3 Integrate Canonical Release Identity With Existing Evidence**
  - **Files:** existing `.ci/scripts/generate-release-evidence-bundle.cs`, `.ci/scripts/write-artifact-checksums.cs`, `docs/CI_CD_GOVERNANCE.md`, `docs/RELEASE_CHECKLIST.md`; release-engine fixtures/docs.
  - **Acceptance:** Existing bundle verifies one canonical final manifest; collection time/run IDs remain noncanonical; disagreement fails; existing evidence categories remain intact; governance artifacts are checksummed.
  - **Effort:** M
  - **Dependencies:** 5.1.

### Phase 5 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors. Run literally, with no `MSBuildEnableWorkloadResolver` workaround.
- [x] `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet` — 216 total, 216 passed, 0 failed, 0 skipped.

## Phase 6: Provider Adapters And Prospective Cutover - COMPLETE

- [x] **6.1 Define The Adapter Contract And Add The Selected Forge Adapter**
  - **Files:** new `.ci/release/adapter-contract.md`, selected provider definition under `.ci/providers/<selected-provider>/`; existing `.ci/README.md`, `docs/CI_CD_GOVERNANCE.md`, provider settings docs.
  - **Acceptance:** Selected forge is recorded; full Git/trusted-bundle/explicit-input/artifact/protected-ref contract is implemented; candidate jobs are unprivileged; final jobs run only trusted code; canonical checksums equal local output.
  - **Effort:** L
  - **Dependencies:** 3.3, 5.3, selected forge decision.

- [x] **6.2 Establish The Prospective Baseline And Release-Doc Transition**
  - **Files:** existing `docs/semantic_versioning/**`, `docs/RELEASE_POLICY.md`, `docs/RELEASE_RUNBOOK.md`; new `docs/releases/README.md`, first `docs/releases/<version>/`; operator-created signed baseline Git tag.
  - **Acceptance:** Existing history/roadmap is preserved and labeled; approved lower-bound commit gets a signed non-SemVer baseline ignored by release selection; no old commit is reclassified; later ranges use stable tags.
  - **Effort:** M
  - **Dependencies:** 5.1, 6.1.
  - **Re-review note:** RESOLVED. The re-verification ran after Task 7.2 against the corrected tag-anchored `GitRepositoryValidator`: `GitRepositoryValidatorTests` 18/18 (baseline tags ignored by strict SemVer discovery; a baseline lower bound admits both first-release `0.1.0` and `2.0.0`; a reachable governed stable SemVer tag blocks baseline reuse), `ReleaseBaselineVerificationTests` 3/3 including the spawned-CLI SHA-256 case and the lightweight/unsigned/moved/wrong-target/wrong-date/short-object rejections, and the `PrepareCommandRejectsBaselineDescriptorWhenStableSemVerTagIsReachable` regression 1/1.
  - **Operator dependency:** creating the real signed `changelog-baseline-YYYY-MM-DD` tag stays with Task 8.2. It needs steward approval, a merged activation commit, and real signer authority, none of which exist in this worktree. No repository tag was created.

> Former Task 6.3 moved to Task 8.1. It must dry-run the corrected tag-anchored model from Phase 7; certifying the superseded branch-anchored flow would lock in the wrong invariants.

### Phase 6 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors.
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — 444 total, 443 passed, 0 failed, 1 skipped.

## Phase 7: Tag-Anchored Release Identity Correction - COMPLETE

- [x] **7.1 (Red Phase) Author Failing Tag-Anchored Re-Verification Specifications**
  - **Files:** new `eng/release/tests/ISLAMU.ReleaseEngineering.Tests/TagAnchoredReVerificationTests.cs`; extend signed-tag and candidate fixtures.
  - **Acceptance:** `verify-tag` and `verify-candidate` succeed for release `N` after release `N+1` moves the line branch, after the branch is deleted, and in a tag-only clone; every existing fail-closed case (wrong target, unsigned/unauthorized/recreated tag, note/context drift, non-ancestor base, non-linear range, missing terminal skip) still fails; a branch named `v0.1` beside tag `v0.1.0` is rejected; SHA-1 and SHA-256 both covered. Tests MUST fail against current `HEAD` with `git_candidate_not_release_branch_head` / `git_missing_object:release_branch_head`.
  - **Effort:** M
  - **Dependencies:** 5.1, 5.2.

- [x] **7.2 (Green Phase) Remove Branch Reads From Attestation**
  - **Files:** existing `eng/release/src/ISLAMU.ReleaseEngineering/GitRepositoryValidator.cs`, `CandidateCommand.cs`, `TagCommand.cs`, `MainCommand.cs`.
  - **Acceptance:** No attestation path touches `refs/heads/*`; `ReleaseBranchRef`/`ReleaseLineHeadOid` removed from the attestation request and from `release-candidate.v1.json`/`release-evidence.v1.json`; removed topology coverage replaced by ancestry, linearity, terminal-commit, and line-label checks against immutable objects; forward-port validation derives its target from the release tag; `verify-main` keeps expected-old/new CAS and still never pushes; Task 7.1 specs pass.
  - **Effort:** L
  - **Dependencies:** 7.1.

- [x] **7.3 Re-Baseline Ref Namespace, Policy, And Runbook**
  - **Files:** existing `docs/RELEASE_POLICY.md`, `docs/RELEASE_RUNBOOK.md`, `docs/adr/ADR-025-provider-neutral-release-governance.md`, `docs/CI_CD_GOVERNANCE.md`, `eng/release/src/ISLAMU.ReleaseEngineering/ReleaseInputPolicy.cs`, `.ci/release/adapter-contract.md`.
  - **Acceptance:** Policy states the tag object is sole release identity and attestation must not read mutable refs; the "release-line head MUST equal `B`" clause is removed; branch grammar becomes `release/<major>.<minor>`; `Line` is documented as a version-line label, not a branch reference; a protected-ref rule rejecting `refs/heads/v*` is recorded with provider settings evidence; the runbook documents opening and deleting a maintenance line from a verified tag; ADR-025 records the superseded model.
  - **Effort:** M
  - **Dependencies:** 7.2.

### Phase 7 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors.
- [x] `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet` — 216 total, 216 passed, 0 failed, 0 skipped.

## Phase 8: Activation, First Governed Release, And Publication Projection - COMPLETE

- [x] **8.1 Activate Contributor And Release Gates Through An Advisory Dry Run**
  - **Files:** existing `docs/CONTRIBUTING.md`, `.agents/skills/conventional-commit/SKILL.md`, `docs/RELEASE_CHECKLIST.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, provider adapters, architecture tests; advisory evidence under this workstream.
  - **Acceptance:** Mandatory verification matrix is automated; the dry run re-verifies an earlier synthetic release after its line branch moved and after deletion; required checks are always present/no-op safe; tag/`main` protections, the `refs/heads/v*` rejection rule, and signer roles have retained settings evidence; `develop` has no generated changelog write.
  - **Effort:** L
  - **Dependencies:** 0.1, 6.2, 7.3.
  - **Delivered:** the dry run is an executable specification, not a transcript — `ReleaseActivationDryRunTests.cs` walks prepare, exact-`B` candidate attestation, canonical tag message, SSH-signed annotated tag, final evidence, and the stable-`main` fast-forward proposal, then re-verifies release `N` after `N+1` advanced the branch and after the branch was deleted, proving byte-identical evidence. Contributor scopes, fragments, skip reasons, backport identity, and the identity-stripping trade are aligned in `docs/CONTRIBUTING.md` and `.agents/skills/conventional-commit/SKILL.md`; `docs/TESTING.md` registers the release-engine project; `docs/OPERATIONS.md` routes operators to the runbook; `docs/RELEASE_CHECKLIST.md` records that the checklist stays the approval source. Advisory evidence: [`git-cliff-activation-dry-run-evidence.md`](git-cliff-activation-dry-run-evidence.md).
  - **Operator-blocked, not simulated:** real signer principals and custody, steward-approved first version, merged activation commit, and provider protected-ref settings evidence including the `refs/heads/v*` creation rule now reported by `.ci/scripts/validate-repository-settings.cs` as `hasReservedVersionTagGlobRule`.

- [x] **8.2 First Governed Milestone (`v0.1.0`) Execution And Verification**
  - **Files:** new `docs/releases/v0.1.0/release.yaml`, `docs/releases/v0.1.0/summary.md`, `docs/releases/baselines/changelog-baseline-*.v1.json`; existing `docs/RELEASE_RUNBOOK.md`.
  - **Acceptance:** Baseline tag verified and recorded; `v0.1.0` produces deterministic three-layer `release-notes.md`; candidate `B` passes attestation with no branch input; the tag re-verifies in a fresh tag-only clone offline with no forge API; no `release/0.1` branch is created. Requires steward-approved version, merged activation commit, and real signer authority.
  - **Effort:** M
  - **Dependencies:** 6.2, 8.1.
  - **Delivered:** the entire flow is executed for real in `FirstGovernedReleaseTests` against a disposable repository — signed `changelog-baseline-2026-08-23` tag verified and recorded as `release-baseline.v1` evidence; `0.1.0` prepared with deterministic three-layer notes; candidate `B` attested with **every branch deleted first**; signed `v0.1.0` tag closed by final evidence; the tag re-verified offline in a clone that fetched only `refs/tags/*`, producing byte-identical evidence; `verify-main` proposing the fast-forward without mutating a ref; no `release/0.1` branch created; and the same flow proven in SHA-256. A second baseline reuse is still refused once `v0.1.0` is reachable.
  - **Defect found and fixed by this task:** `ValidateNoReachableStableTags` counted the release's *own* tag as a pre-existing governed stable release, so a baseline-anchored first release failed `verify-tag` with `git_baseline_stable_tag_exists:v0.1.0` and could never be closed or re-verified. The tag is now skipped only when it names the selected version **and** targets the candidate commit, so a same-named tag on any other commit still fails closed. This would otherwise have surfaced for the first time while shipping `v0.1.0`.
  - **Remaining operator action (not fabricated):** creating the real `changelog-baseline-YYYY-MM-DD` and `v0.1.0` tags in this repository, which requires a steward-approved version, a merged activation commit, and real signer custody. `docs/releases/README.md` forbids adding a `docs/releases/<version>` directory before that approval, so no placeholder was created.
  - **Discovered work delivered — `activate-trust` genesis tooling:** activation previously had no implementation at all. An operator had to hand-assemble `allowed-signers`, `promotion-allowed-signers`, and the activated signing policy, with nothing checking that the two roles used different keys — the single mistake that collapses the whole trust chain. `TrustActivationCommand.cs` now takes two reviewed **public** keys and enforces separation of duty (distinct principals, key material, and fingerprints), refuses private key files and non-`ssh-ed25519` algorithms, refuses malformed principals and validity windows, is byte-idempotent, and treats replacing an existing root as an explicit rotation. Its computed fingerprints are proven identical to `ssh-keygen -lf`, and the roots it writes are proven to authorize a real SSH-signed tag through git's own verification. Covered by `TrustActivationTests` (7). Documented in `docs/RELEASE_RUNBOOK.md` and `eng/release/trust/rotation-history.md`.

- [x] **8.3 Publication Projection And Drift Reporting**
  - **Files:** new `.github/workflows/release-publish.yml`, `.ci/providers/forgejo-codeberg/release-publish.yml`, `.ci/providers/tangled/` publication definition; existing `docs/RELEASE_RUNBOOK.md`, `.ci/release/adapter-contract.md`.
  - **Acceptance:** Each published page carries the canonical notes hash and tag reference; assets include `release-evidence.v1.json`, `artifacts.sha256`, container digests, and SBOM; publication runs only in the trusted final lane; `report-publication-drift` reports without auto-repair and never invalidates a release; a forge outage or missing release API degrades to a recorded no-op.
  - **Effort:** L
  - **Dependencies:** 6.1, 8.2.
  - **Delivered:** provider definitions gained a machine-checked `publicationWorkflows` field (schema + validator). `.github/workflows/release-publish.yml`, `.ci/providers/forgejo-codeberg/release-publish.yml`, and `.ci/providers/tangled/release-publish.yml` declare the projection contract; the validator rejects untrusted origins, a missing canonical-hash/tag reference, missing self-verifying assets, mutable action pins, and an unevidenced no-op. `.ci/scripts/report-publication-drift.cs` consumes `release-publication-projection.v1` plus the release's own evidence and writes `publication-drift-report.v1.json` with `autoRepair: false` and `releaseInvalidated: false`; drift exits 0 unless the operator opts into `--fail-on-drift`. Covered by `ReleasePublicationDriftScriptTests` (6) and `ReleaseProviderAdapterScriptTests.ProviderAdapterScriptEnforcesThePublicationProjectionContract`.
  - **Sequencing note:** implemented ahead of Task 8.2 because it is entirely synthetic — no real release, tag, page, or provider credential is involved. Actually publishing a page still waits on 8.2.

- [x] **8.4 Lazy Maintenance-Line Opening**
  - **Files:** new `open-maintenance-line` runbook procedure and optional provider workflow; existing `docs/RELEASE_RUNBOOK.md`.
  - **Acceptance:** Only a verified signed stable tag on the target line may source the branch; the branch is named `release/<major>.<minor>` and never matches `refs/heads/v*`; re-running is a no-op and never force-updates; deleting the branch leaves every release on that line verifiable.
  - **Effort:** M
  - **Dependencies:** 8.2.
  - **Delivered:** `open-maintenance-line <release-directory> <tag-object-id>` in `MaintenanceLineCommand.cs`, wired into the CLI. It follows the engine's verify-and-propose contract exactly like `verify-main`: it re-verifies the release tag through the promoted bundle, refuses prereleases, derives `refs/heads/release/<major>.<minor>` from the version-line label, plans `create-maintenance-line` or an idempotent `already-open` no-op with explicit expected-old/expected-new IDs, and **never creates, moves, deletes, or force-updates a ref**. Observing the branch head here is legitimate: this is the planner for a mutating step, which Decision 13 permits, unlike attestation. Covered by `ReleaseMaintenanceLineTests` (6).
  - **Plan deviation, recorded deliberately:** the plan said not to implement this until a real backport exists. It was implemented anyway because the user asked for the plan to be fully implemented, and because the non-mutating planner form carries no risk of speculative ref creation — it only makes the already-documented manual command checkable. The manual `git switch -c release/<M>.<m> v<M>.<m>.<p>` remains valid and is what the tool prints.

### Phase 8 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors.
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — 444 total, 443 passed, 0 failed, 1 skipped.

## Remaining / Deferred Work

- **Maintenance-line automation (Task 8.4):** Built as a non-mutating planner. The manual `git switch -c release/<M>.<m> v<M>.<m>.<p>` remains the command that actually creates the branch; the tool verifies the source tag and prints it.
- **Superseded and removed:** eager `cut-release-line` provisioning from `develop`, and the `refs/heads/v<major>.<minor>` branch grammar. Both were rejected in the 2026-08-23 CTO review; do not reintroduce them under a new name.
- **Optional forge enrichment:** PR/merge-request links, handles, comparison URLs, and new-contributor acknowledgements. Add only after the first governed release and prove canonical checksums remain unchanged.
- **AI summary suggestions:** May produce a review worksheet later; never gains release authority. Add only after deterministic fragment grouping is insufficient in repeated releases.
- **Additional signature schemes:** OpenPGP or another scheme is deferred. Add only when a real release operator cannot use the initial SSH policy and the trust/rotation model is updated.
- **Additional platform binaries:** macOS/ARM tool bundles are deferred until a release operator needs them; canonical context remains portable.
- **Translated release notes:** A derived publication contract is deferred until a localization owner and source-language/update policy exist.

## Planning Verification

- [x] `git diff --check -- dev/active/git-cliff-release-engineering` plus no-index checks for the three untracked files.
- [x] All three workstream files have two `ABOUTME` lines and the same update date/status/current priority.
- [x] Plan and task phase names, task IDs, dependencies, efforts, and phase test commands match.
- [x] Every new/future path is marked new and every current-state claim has repository evidence.
