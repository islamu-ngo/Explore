<!-- ABOUTME: Hot execution ledger for the provider-neutral git-cliff release-engineering implementation. -->
<!-- ABOUTME: Mirrors the approved plan phases, task acceptance, dependencies, and phase-end verification gates. -->

# Git-Cliff Release Engineering - Task Checklist

Last Updated: 2026-08-15 Europe/Brussels

## Status Summary

- **Overall status:** Active implementation; Phase 1 and Phase 5 verification remain blocked outside the release-engine slice.
- **Completed:** 16/18 implementation tasks. Phase verification is tracked separately.
- **Current priority:** Independently verify the version-agnostic Task 6.2 baseline follow-up.
- **Next recommended slice:** If confirmed, complete Task 6.2 and execute the Task 6.3 synthetic advisory release flow.
- **Current handoff:** `git-cliff-release-engineering-handoff.md` records exact current evidence, blockers, and continuation steps.
- **Forge selection:** The Project Steward selected Forgejo/Codeberg, Tangled, and GitHub on 2026-08-15.
- **Phase-gate exception:** On 2026-08-14 the user explicitly authorized Tasks 2-5 to continue while the Phase 1 architecture checkbox remains blocked by four unrelated shared-worktree failures.
- **Current Phase 1 gate evidence:** Exit 2; 377 total, 372 passed, 4 failed, 1 skipped. Independent escalation confirmed the same four product/architecture-contract failures remain outside release-engineering scope; the checkbox stays open.
- **Current Phase 5 gate evidence:** Release-engine tests pass 172/172 with clean diagnostics and evidence smoke, recorded under `MSBuildEnableWorkloadResolver=false`. 2026-08-15 SDK update: `dotnet workload repair` was run and the host SDK is healthy (`wasm-tools 10.0.109/10.0.100`); the `MSBuildEnableWorkloadResolver=false` workaround is no longer required. Literal commands are no longer host-blocked, so this gate can now be re-run and closed with exact commands; it remains open because that literal re-run has not been performed. The separate blocker — unrelated authorization API compile errors in the shared dirty tree — is unaffected by the SDK repair.
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

## Phase 1: Governed Foundation And Tool Selection - IN PROGRESS

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

- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

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

## Phase 5: Tag Closure, Stable Main, And Evidence Integration - IN PROGRESS

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

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet`

## Phase 6: Provider Adapters, Prospective Cutover, And Activation - NOT STARTED

- [x] **6.1 Define The Adapter Contract And Add The Selected Forge Adapter**
  - **Files:** new `.ci/release/adapter-contract.md`, selected provider definition under `.ci/providers/<selected-provider>/`; existing `.ci/README.md`, `docs/CI_CD_GOVERNANCE.md`, provider settings docs.
  - **Acceptance:** Selected forge is recorded; full Git/trusted-bundle/explicit-input/artifact/protected-ref contract is implemented; candidate jobs are unprivileged; final jobs run only trusted code; canonical checksums equal local output.
  - **Effort:** L
  - **Dependencies:** 3.3, 5.3, selected forge decision.

- [ ] **6.2 Establish The Prospective Baseline And Release-Doc Transition**
  - **Files:** existing `docs/semantic_versioning/**`, `docs/RELEASE_POLICY.md`, `docs/RELEASE_RUNBOOK.md`; new `docs/releases/README.md`, first `docs/releases/<version>/`; operator-created signed baseline Git tag.
  - **Acceptance:** Existing history/roadmap is preserved and labeled; approved lower-bound commit gets a signed non-SemVer baseline ignored by release selection; no old commit is reclassified; later ranges use stable tags.
  - **Effort:** M
  - **Dependencies:** 5.1, 6.1.

- [ ] **6.3 Activate Contributor And Release Gates Through An Advisory Dry Run**
  - **Files:** existing `docs/CONTRIBUTING.md`, `.agents/skills/conventional-commit/SKILL.md`, `docs/RELEASE_CHECKLIST.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, provider adapter, architecture tests; advisory evidence under this workstream.
  - **Acceptance:** Mandatory verification matrix is automated; protected settings evidence exists; required checks are always present/no-op safe; full synthetic candidate/tag/main flow passes; `develop` has no generated changelog write.
  - **Effort:** L
  - **Dependencies:** 6.1, 6.2.

### Phase 6 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Remaining / Deferred Work

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
