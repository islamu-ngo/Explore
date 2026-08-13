<!-- ABOUTME: Resumable context for the provider-neutral git-cliff release-engineering workstream. -->
<!-- ABOUTME: Records current evidence, key decisions, risks, dirty-worktree boundaries, and the next implementation slice. -->

# Git-Cliff Release Engineering - Context

Last Updated: 2026-08-13 Europe/Brussels

## SESSION PROGRESS (2026-08-13 Europe/Brussels)

### COMPLETED

- Created the implementation plan from the git-cliff report and the Senior CTO review.
- Verified the current branch/release topology at HEAD `3e9c90fed55073f77fc0410d837b6bf3cb8e2aac`.
- Verified that there are no Git tags, release-engine projects, signing policies, release descriptors, change fragments, or overlapping active release workstreams.
- Verified the existing CI/CD, release checklist, release-impact gate, evidence-bundle script, conventional-commit policy, semantic-version docs, clean-room policy, solution/project layout, and current source report.
- Ran the planning baseline Release build successfully.
- Re-baselined the future architecture around a trusted release-engine bundle, normalized context, renderer-only git-cliff, exact final commit `B`, separate human/generated files, change fragments, SSH-signed tags, deterministic evidence, embargo handling, and parallel release lines.

### IN PROGRESS

- Awaiting user review and approval of the implementation plan.

### NEXT

1. Review the trusted-tool bootstrap and exact-`B` release flow in the plan.
2. Confirm or correct the SSH-only initial signing decision.
3. Approve Phase 1 or request plan changes.
4. The first implementation agent starts Task 1.1 and does not touch unrelated dirty files.

### BLOCKERS

- No blocker for Phases 1-5.
- Task 6.1 requires the Project Steward to name the first non-GitHub forge before its adapter path is finalized.
- Task 3.3 requires real release signer principals/keys and an artifact-promotion authority before activation, but their absence does not block earlier implementation.

## Quick Resume

1. Read this context and `git-cliff-release-engineering-tasks.md`.
2. Read only the current phase and referenced decisions in `git-cliff-release-engineering-plan.md`; do not reread the unchanged full plan on every resume.
3. Start from Task 1.1 unless the user changes priority.
4. Treat `tasks.md` as the hot ledger; update this context only after a phase, decision, blocker, validation failure, material discovery, or handoff.
5. Preserve the unrelated dirty worktree. Do not restore deleted files or edit migration/payment/registration/agent-context work unless a later task explicitly owns the exact path.

## Current Repository Facts

| Fact | Verified evidence |
|---|---|
| Current branch | `develop` |
| Current HEAD | `3e9c90fed55073f77fc0410d837b6bf3cb8e2aac` |
| Commit counts | `develop`: 2,122; `main`: 206; `main..develop`: 1,916 |
| Release tags | None |
| Active/paused overlap | None found for git-cliff, changelog, or release engineering |
| Current release process | Manual SemVer tags and manually authored GitHub Releases |
| Current release-note source | `docs/semantic_versioning/CHANGELOG.md` plus version companions; mixes Unreleased history and roadmap |
| Shared CI implementation | `.ci/` with file-based C# scripts |
| Current provider authority | GitHub for deployed CI/CD evidence; provider-neutral release core does not yet exist |
| Current release-evidence generator | `.ci/scripts/generate-release-evidence-bundle.cs` |
| Current release-impact check | GitHub `pull_request_target` workflow plus GitHub PR-files API |
| Current tool manifest | Root `dotnet-tools.json`; no git-cliff/release-engine lock |
| Current tag signing policy | Not found |
| Planning baseline | Release build succeeded on 2026-08-13 |

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `dev/report/git-cliff-changelog-automation-report.md` | Existing, untracked | Report | Complete git-cliff docs analysis and original design | Preserve its source register; re-baseline conflicting architecture in Task 1.1. |
| `docs/RELEASE_CHECKLIST.md` | Existing | Docs/Ops | Current manual release contract | Explicitly blocks required `release.yml` until evidence is stable. |
| `docs/CI_CD_GOVERNANCE.md` | Existing | Docs/DevOps | Current workflow, provider, evidence, and protection policy | Currently says GitHub is authoritative for deployment/evidence. |
| `.ci/README.md` | Existing | DevOps | Shared provider-neutral implementation home | Current scripts are file-based C# scripts. |
| `.ci/scripts/generate-release-evidence-bundle.cs` | Existing | DevOps | Collects retained evidence into JSON/Markdown/checksums | Useful bundle index, not deterministic canonical release identity. |
| `.ci/scripts/write-artifact-checksums.cs` | Existing | DevOps | Existing checksum implementation | Reuse in Phase 5. |
| `.ci/scripts/validate-release-impact-pr.cs` | Existing | DevOps | GitHub PR metadata validation | Early feedback only; not canonical release policy. |
| `.agents/skills/conventional-commit/SKILL.md` | Existing | Governance | Current outcome-led commit policy and public scopes | Phase 2 adds engineering scopes, fragments, skip reasons, and backports. |
| `.agents/contract/intents.yaml` | Existing | Governance | Canonical Contribution Contract catalog | Phase 1 must add planned paths before implementation. |
| `docs/semantic_versioning/**` | Existing | Docs | Pre-automation history/roadmap | Preserve and mark as legacy baseline; do not regenerate. |
| `Explore.slnx` | Existing | Build | Solution project registry | Phase 1 adds release-engine source/test projects. |
| `eng/release/src/ISLAMU.ReleaseEngineering/` | New | DevOps | Trusted release-policy and orchestration CLI source | No product-project references. |
| `eng/release/tests/ISLAMU.ReleaseEngineering.Tests/` | New | Tests | Unit and synthetic Git fixture coverage | TUnit; no app/browser/Docker/Aspire. |
| `eng/release/policy/release-policy.yaml` | New | Governance | Release type, impact, SemVer, tag, prerelease, and visibility rules | Packaged in trusted bundle. |
| `eng/release/policy/scope-registry.yaml` | New | Governance | Public and engineering scope taxonomy | Extensible and versioned. |
| `eng/release/toolchain.lock.json` | New | Supply chain | git-cliff and release bundle pins/digests | Candidate copy is not final authority. |
| `eng/release/cliff.toml` | New | Rendering | Presentation-only git-cliff template/config | No parsing policy, provider block, URL, or executable processor. |
| `eng/release/trust/` | New | Security | SSH signer policy, allowed signers, rotation record | Promoted with trusted bundle. |
| `docs/releases/<version>/release.yaml` | New | Docs/Release data | Version, date, range, compatibility, impact dispositions | Human-owned and typed. |
| `docs/releases/<version>/summary.md` | New | Docs/Release data | Single maintainer-owned public narrative | Never duplicated manually into tag text. |
| `docs/releases/<version>/release-notes.md` | New | Docs/Generated | Fully generated three-layer public document | No generated-region markers. |
| `docs/releases/changes/<change-id>.yaml` | New | Docs/Release data | Append-only high-impact/group/backport facts | Public details only. |
| `docs/adr/ADR-025-provider-neutral-release-governance.md` | New | Architecture | Stable architecture decision | Created in Task 1.1. |
| `docs/RELEASE_POLICY.md` | New | Governance | Normative release invariants | MUST/SHOULD/MAY language. |
| `docs/RELEASE_RUNBOOK.md` | New | Operations | Operator commands, failures, recovery, rotation, cutover | Provider-neutral core with adapter notes. |
| `docs/legal/dependencies/git-cliff.md` | New | Legal/Dependency | Exact version/license/checksum/obligation decision | Created after clean-room dependency selection. |

## Key Decisions

1. ISLAMU owns commit classification, impacts, grouping inputs, SemVer, range/tag selection, and evidence. git-cliff renders normalized context only.
2. One proper .NET console project plus one TUnit project replaces the proposed multipurpose `.cs` script. No service or plugin framework is added.
3. Candidate previews may use candidate tooling for feedback; authoritative attestation always uses a previously promoted bundle.
4. The trusted bundle contains the release engine, policy, context version, git-cliff pin, renderer config, and signer trust roots.
5. The final preparation commit `B` already contains `release.yaml`, `summary.md`, and generated `release-notes.md`; candidate validation occurs at `B`.
6. `B` uses `Changelog: skip` plus `Changelog-Reason: release metadata commit`, so adding the note does not change its own rendered output.
7. Release branch integration must preserve `B` through a fast-forward-only compare-and-swap update; squash/rebase/merge replacement requires regeneration and reapproval.
8. `summary.md` is the sole public narrative source. Tag text and `release-notes.md` are generated.
9. Candidate and final evidence are separate to avoid a tag/manifest digest cycle.
10. Public change fragments are required only for high-impact or deterministic grouped changes; simple fixes/features remain commit-subject driven.
11. Public and engineering scopes are both valid, but engineering scopes are omitted from public notes by default.
12. Initial release tags use SSH signatures verified against bundled allowed signers; forge UI signature badges are not authority.
13. Prerelease notes are cumulative from the prior stable tag; `alpha.N`, `beta.N`, and `rc.N` are contiguous; prereleases never move `main`; build metadata is excluded from canonical tags.
14. `main` advances only by a normal fast-forward to the newest stable tag commit. Older-line patches do not move it backwards.
15. Existing semantic-version documents remain a frozen pre-automation baseline; the signed cutover tag is non-SemVer and ignored by release selection.
16. Security embargo metadata lives outside the public checkout and normal candidate artifacts until disclosure.
17. Provider adapters only transport explicit inputs, trusted bundles, artifacts, and protected ref actions. Provider metadata may enrich only a secondary publication.

## Constraints And Rules To Remember

- Planning and implementation are governed by `.agents/contract/intents.yaml`; the `.claude/contract` path in older instructions is stale.
- Task 1.1 expands the current `ci-cd-change` scope before other new paths are touched.
- Final jobs must not execute candidate-controlled code or receive credentials while candidate code runs.
- `--offline`, `--no-exec`, explicit config, and `--from-context` are mandatory git-cliff capabilities.
- External git-cliff research is source-free and must produce dependency/provenance evidence before implementation uses it.
- Every new file starts with two `ABOUTME` comment lines in the file's native comment syntax.
- Do not hand-edit EF migrations or touch product runtime layers; they are out of scope.
- Do not force-push, rewrite published tags, or move `main` backwards.
- Do not emit author identities, emails, raw bodies, provider handles/tokens, restricted security details, or unbounded exception text.
- Do not use current time, locale-dependent formatting, global Git config, default SHA abbreviation, or provider APIs in canonical generation.
- Do not create continuous `[Unreleased]` changelog commits on `develop`.

## Validation Baseline

| Phase | Build, run once after phase tasks | One selected test project, run once |
|---|---|---|
| 1 | `dotnet build --configuration Release --verbosity quiet` | `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` |
| 2 | Same Release build | `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet` |
| 3 | Same Release build | Same release-engine test project, now covering Git/trust/determinism |
| 4 | Same Release build | Same release-engine test project, now covering real renderer/preparation fixtures |
| 5 | Same Release build | Same release-engine test project, now covering tag/main/evidence closure |
| 6 | Same Release build | `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` |

Planning-artifact verification is limited to `git diff --check -- dev/active/git-cliff-release-engineering` plus file/header/link consistency checks. The planned suite runs only during implementation.

## Current Known Risks / Unknowns

- **Trusted bootstrap (Task 3.3):** The first promoted bundle has no previous release-engine to validate it. Genesis promotion needs independent review, exact source/tool hashes, tests, SBOM/checksums, protected approval, and a signed tooling tag.
- **Exact git-cliff release (Task 1.3):** The reviewed docs include changes after `v2.13.1`; implementation must not guess capability availability.
- **First forge adapter (Task 6.1):** Provider is intentionally unresolved. Core commands and canonical artifacts must be complete first.
- **Signer principals (Task 3.3):** Actual SSH principals and key custody/rotation owners are not yet recorded.
- **Artifact store (Tasks 1.3/3.3):** The core accepts a local verified bundle; the selected adapter must define transport and retention.
- **Embargo operations (Task 3.3):** Access-controlled storage/provider choice remains an operational decision, while the no-leak interface is mandatory.
- **Dirty worktree:** Many unrelated agent-context, registration, migration-regeneration, organizer-payment, and report changes predate this plan. Implementation must not revert or absorb them.

## Handoff Notes

### Handoff - 2026-08-13 Europe/Brussels

- **Current state:** Planning complete; no release automation implemented.
- **Next action:** User reviews the plan, especially Decisions 1, 6, 7, and the Phase 3 trusted bootstrap. After approval, start Task 1.1.
- **Blockers:** None for Phases 1-5; selected forge is needed for Task 6.1.
- **Modified files:** Only the three new files under `dev/active/git-cliff-release-engineering/` belong to this planning task.
- **Validation:** Release build passed. All three planning files passed no-index whitespace checks, ABOUTME/date/status checks, and the 18-task/6-phase synchronization review.
- **Documentation impact:** New active planning workstream only. Canonical release docs are planned, not yet changed.
- **Risks:** Candidate/trusted execution separation is the critical design boundary; do not begin renderer templates first.
- **Notes for next contributor/agent:** Run `git status --short` before editing and preserve every unrelated dirty path. Do not restore the currently deleted `tests/Event.Architecture.Tests/AgentContextGovernanceTests.cs` as part of this workstream unless the user separately assigns it.
