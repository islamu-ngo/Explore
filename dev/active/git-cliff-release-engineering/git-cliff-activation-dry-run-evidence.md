<!-- ABOUTME: Advisory activation dry-run evidence for the governed provider-neutral release flow. -->
<!-- ABOUTME: Records what was exercised synthetically, what stays operator-blocked, and which gates remain manual. -->

# Activation Dry Run — Advisory Evidence

Last Updated: 2026-08-23 Europe/Brussels
Owning task: 8.1

## What this is

The advisory dry run is an **executable specification**, not a transcript. A transcript of a
one-off manual run rots the moment the code changes; a test suite fails loudly instead. The
dry run therefore lives in the release-engine test project and runs in every phase gate.

| Concern | Where it is proven |
|---|---|
| Full governed flow: prepare → exact `B` → candidate manifest → canonical tag message → SSH-signed annotated tag → final evidence → stable-`main` proposal | `ReleaseActivationDryRunTests.GovernedFlowClosesTwoReleasesAndProposesAStableMainFastForward` |
| Re-verification after the line branch advances to the next release, and after it is deleted, producing byte-identical evidence | `ReleaseActivationDryRunTests.DryRunReVerifiesTheEarlierReleaseAfterTheBranchMovesAndAfterItIsDeleted` |
| Canonical artifacts carry no branch ref, branch head, identity, or provider metadata | `ReleaseActivationDryRunTests.CanonicalEvidenceCarriesNoBranchIdentityIdentityOrProviderMetadata` |
| Ordinary development commits write no generated changelog outside `docs/releases/<version>/` | `ReleaseActivationDryRunTests.OrdinaryDevelopmentCommitsProduceNoGeneratedChangelogOutsideAReleaseDirectory` |
| Tag-only clone, moved branch, deleted branch, SHA-1 and SHA-256 | `TagAnchoredReVerificationTests` |
| Fail-closed on wrong tag target, unsigned tag, recreated tag, note/context drift, missing terminal skip, non-linear range, non-ancestor base | `TagAnchoredReVerificationTests`, `ReleaseTagVerificationTests`, `ReleaseCandidateVerificationTests`, `GitRepositoryValidatorTests` |
| Reserved `refs/heads/v*` namespace and `release/<major>.<minor>` maintenance grammar | `TagAnchoredReVerificationTests.VersionTagGlobIsReservedAgainstBranchesAndNeverResolvedAmbiguously` |
| Three-provider transport plans with identical canonical input and promoted-bundle checksums | `ReleaseProviderAdapterScriptTests` |
| Durable evidence bundle consuming one canonical final manifest | `ReleaseEvidenceBundleScriptTests` |
| Publication projection contract: trusted origin, canonical hash and tag reference on the page, self-verifying assets, pinned actions, recorded no-op for providers without a release API | `ReleaseProviderAdapterScriptTests.ProviderAdapterScriptEnforcesThePublicationProjectionContract` |
| Drift reported, never repaired, never invalidating the release | `ReleasePublicationDriftScriptTests` |
| Complete first-governed-release flow from a signed non-SemVer baseline, offline and branchless, SHA-1 and SHA-256 | `FirstGovernedReleaseTests` |
| Maintenance line opened only from a verified stable tag, idempotent, non-mutating, disposable | `ReleaseMaintenanceLineTests` |
| Genesis trust activation from two public keys, separation of duty enforced, roots proven to authorize a real signed tag | `TrustActivationTests` |

Run:

```bash
dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet
```

## Result at 2026-08-23

236 tests, 236 passed, 0 failed, 0 skipped, against a full-solution `dotnet build --configuration
Release --verbosity quiet` with 0 errors.

The three real provider definitions under `.ci/providers/` were also validated directly with
`.ci/scripts/validate-release-provider-adapters.cs`, which emitted `adapter_validation_passed:
providers=3` and three transport plans.

## Always-present required checks

Both provider check surfaces stay present for every event and are no-op safe, so a required
check can never sit pending on an unrelated change:

- `release-adapter-preview` — unprivileged candidate feedback. `contents:read` only, no secrets,
  no write permission, no OIDC.
- `release-adapter-final` — trusted transport after candidate execution has stopped.

`.ci/scripts/validate-release-provider-adapters.cs` enforces `alwaysPresentNoop` on both lanes for
Forgejo/Codeberg, Tangled, and GitHub, and rejects a manifest that claims success it cannot deliver
(`adapter_misleading_success_forbidden`).

## What stays manual

[RELEASE_CHECKLIST.md](../../../docs/RELEASE_CHECKLIST.md) remains the approval source. The
tooling verifies and records evidence; it does not approve, tag, push, publish, or deploy.
Automation removes no governance gate.

## Operator-blocked, deliberately not simulated

These need real authority and were **not** fabricated:

- Real release signer principals, key custody, and rotation owners. Production trust roots under
  `eng/release/trust/` remain intentionally comment-only.
- A steward-approved first governed version and a merged activation commit.
- The signed `changelog-baseline-YYYY-MM-DD` tag (Task 8.2).
- Provider protected-ref settings evidence, including the branch ruleset that must include
  `refs/heads/v*` with a `creation` rule. `.ci/scripts/validate-repository-settings.cs` now reports
  `hasReservedVersionTagGlobRule` and fails the drift check while that rule is absent; the setting
  itself must be created in repository settings before the first governed release tag.
- Tag and `main` protection rules, and environment approval evidence for the final lane.

No repository ref was created, moved, deleted, or pushed during this dry run. Every repository the
tests touch is a disposable temp-directory fixture.
