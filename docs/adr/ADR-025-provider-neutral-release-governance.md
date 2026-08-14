<!-- ABOUTME: Records the provider-neutral release architecture and trusted-boundary decisions. -->
<!-- ABOUTME: Separates stable release authority from later implementation and provider adapters. -->

# ADR-025: Provider-Neutral Release Governance

> **Status:** Accepted for implementation; automation is not active
> **Date:** 2026-08-13
> **Decision owners:** Platform/Ops and Project Steward

## Context

The current release process is manual SemVer tagging with manually authored GitHub
Releases. It remains the operative process until the planned release-engine and its
controls are implemented, verified, and activated. The prior git-cliff report is a
research/provenance record, not architecture authority.

## Decision

1. The canonical release core is provider-neutral and runs only on governed
   `v<major>.<minor>` release lines. `develop` has no generated `Unreleased`
   changelog writes.
2. ISLAMU-owned release-engine policy decides commit validity, inclusion, impact,
   version compatibility, range/tag selection, trust validation, and evidence.
   git-cliff is a pinned renderer only: it receives normalized context and performs
   no policy, provider, network, or executable-processor work.
3. Final attestation runs only from a separately promoted trusted bundle containing
   the release engine, policy, renderer configuration, tool lock, context contract,
   and signer roots. Candidate source and configuration are input data, never final
   authority.
4. Each release has exactly one final preparation commit `B`. `B` is the
   release-branch head, candidate-attestation commit, SSH-signed annotated tag
   target, committed-note commit, and stable `main` target for the newest stable
   release. A squash, merge, rebase, or ref race that changes the object invalidates
   the candidate.
5. `release.yaml` and `summary.md` are the only human-owned release inputs.
   `release-notes.md` is fully generated; no mixed ownership markers or manual tag
   message duplication are used.
6. Canonical context, notes, and manifests are deterministic: UTF-8 without BOM,
   LF, NFC, invariant ordering, explicit release date, full object IDs, and no
   wall-clock, provider identity, or author identity. Candidate and final manifests
   are separate so post-tag identity does not create a hash cycle.
7. Release tags are SSH-signed annotated tags verified against the bundled signer
   policy. The tag object ID, signer, and commit are final evidence; a forge badge is
   not authority.
8. Embargoed security details use a restricted lane outside the public checkout and
   normal candidate artifacts. Only approved disclosure fields cross the public
   boundary.
9. Provider adapters transport explicit inputs, trusted bundles, artifacts, and
   protected ref operations. They may create noncanonical enrichment, but cannot
   affect canonical release identity. No compatibility shim is needed for the
   unshipped report design.

## Consequences

The release engine is a small offline tool, not a service, provider SDK, or plugin
framework. Operators retain approval, tag creation, publication, and deployment.
Policy or renderer changes require their own trusted-bundle promotion before they can
affect final attestation. Existing semantic-version documents remain a preserved
pre-automation baseline until the prospective cutover is explicitly approved.

## References

- [Release Policy](../RELEASE_POLICY.md)
- [Release Runbook](../RELEASE_RUNBOOK.md)
- [Release Checklist](../RELEASE_CHECKLIST.md)
- [Historical git-cliff report](../../dev/report/git-cliff-changelog-automation-report.md)
