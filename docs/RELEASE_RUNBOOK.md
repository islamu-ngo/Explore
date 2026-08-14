<!-- ABOUTME: Provides the current manual release procedure and future governed-release operator flow. -->
<!-- ABOUTME: Keeps operator approval explicit while defining trust, evidence, and recovery boundaries. -->

# Release Runbook

> **Status:** Current manual procedure active; governed flow is prospective
> **Owner:** Platform/Ops and release operators

## Current procedure

Until a trusted release bundle, release-engine verification, signer policy, provider
protection evidence, and advisory dry run are implemented, operators MUST use
[RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md). The current process is manual SemVer
tags and manually authored GitHub Releases. Do not invoke imagined release-engine
commands, claim automatic approval, or create generated changelog writes on
`develop`.

Generate the existing durable evidence bundle only as the checklist directs:

```sh
dotnet run .ci/scripts/generate-release-evidence-bundle.cs -- artifacts release-evidence
```

## Future governed release flow

1. An operator selects a governed `v<major>.<minor>` line and prepares only
   `release.yaml` and `summary.md`; public high-impact facts are supplied through
   validated change fragments.
2. The trusted bundle validates complete Git objects, policy, range, version,
   impacts, and public inputs. It normalizes context and calls the promoted git-cliff
   renderer offline with executable processors disabled.
3. The preparation command generates `release-notes.md` and creates the reviewed
   preparation commit `B`. The message explicitly explains its release-metadata
   changelog skip so the generated note does not include itself.
4. The authoritative bundle verifies candidate `B` and emits deterministic
   pre-tag candidate evidence. Review preserves `B` through a fast-forward-only
   compare-and-swap update; any replacement requires regeneration and review.
5. An authorized release operator creates an SSH-signed annotated tag targeting
   exactly `B`. Tag verification records signer, tag object ID, candidate digest,
   policy/tool hashes, and note/context hashes in final evidence.
6. For the newest stable release only, protected `main` advances by normal
   fast-forward to exactly `B`. Prereleases and older-line patches do not move
   `main` backwards.
7. The provider adapter retains artifacts and may publish a derived enriched view,
   but it cannot alter canonical notes, identity, or approval.

## Required controls before activation

- The authoritative job MUST download and verify a separately promoted bundle before
  processing candidate data. Candidate jobs MUST have no signing, publication,
  deployment, registry-write, OIDC, or protected-ref credentials.
- The bundle MUST include the release engine, policy, renderer config, tool pin, and
  SSH signer roots. The provider MUST retain protected-ref and environment approval
  evidence for final operations.
- `B`, release-line head, candidate record, tag target, committed notes, and stable
  `main` target MUST be checked as one exact full-object identity.
- Public canonical artifacts MUST be deterministic and provider-neutral. Capture
  required hashes and inputs without wall-clock, provider, author, raw-body, or token
  data.
- Restricted security inputs MUST remain in the embargo lane outside the public
  checkout. If disclosure is not authorized, stop before public generation or tag.

## Stop and recovery rules

- Stop on missing Git objects, shallow/partial history, ref replacement, policy or
  bundle mismatch, renderer drift, unauthorized/unsigned/replaced tags, or a moved
  branch. Correct the input or promote a new bundle; do not bypass the check.
- If `B` changes after candidate verification, discard its candidate evidence and
  return to generation/review. Do not retarget a tag or force-push `main`.
- If an embargo field appears in a public artifact, stop publication, revoke public
  artifact access where possible, preserve incident evidence without reproducing the
  restricted value, and use the security incident process before retrying.
- If provider transport fails after local verification, retain local evidence and
  resolve the protected adapter failure. The provider adapter is not permitted to
  regenerate or reinterpret the canonical release.
