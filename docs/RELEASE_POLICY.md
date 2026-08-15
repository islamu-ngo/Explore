<!-- ABOUTME: Defines normative provider-neutral release invariants and their authority boundaries. -->
<!-- ABOUTME: Keeps ISLAMU policy independent from renderers, providers, and candidate-controlled inputs. -->

# Release Policy

> **Status:** Approved prospective policy; no release automation is active
> **Owner:** Platform/Ops and Project Steward

## Scope and precedence

This policy governs the future release engine. It does not replace the current manual
[Release Checklist](RELEASE_CHECKLIST.md) before activation. In a conflict, this
policy governs release invariants; [ADR-025](adr/ADR-025-provider-neutral-release-governance.md)
explains their architecture; [RELEASE_RUNBOOK.md](RELEASE_RUNBOOK.md) defines operator
steps.

## Canonical release contract

- Releases MUST originate from a governed `v<major>.<minor>` line. `develop` MUST
  NOT receive generated `Unreleased` changelog writes.
- ISLAMU policy MUST decide commit validity, visibility, impacts, grouping inputs,
  SemVer, range/tag selection, trust, and canonical evidence. git-cliff MUST render
  normalized context only and MUST NOT decide any of those matters.
- Canonical inputs MUST be provider-neutral. Provider metadata MAY appear only in a
  separate, noncanonical publication view and MUST NOT alter canonical checksums.
- The trusted release bundle MUST contain the authoritative engine, policy, context
  contract, renderer configuration, tool pin, and signer roots. Final attestation
  MUST verify that promoted bundle before reading candidate data.
- Candidate source, templates, policy, renderer config, tool locks, and trust roots
  MUST NOT self-promote or influence authoritative attestation.

## Release sources and exact identity

- `release.yaml` MUST contain the selected version, release line, fixed release date,
  range/base references, compatibility references, and impact dispositions.
- `summary.md` MUST be the sole maintainer-owned public narrative.
- `release-notes.md` MUST be fully generated from the validated sources and
  normalized context. It MUST NOT contain manually maintained generated regions.
- A release MUST finish at one preparation commit `B`. The release-line head,
  candidate evidence, signed annotated tag target, and stable `main` target for the
  newest stable release MUST equal the same full Git object `B`.
- A changed head, replacement, squash, merge, rebase, or regeneration drift MUST
  invalidate the candidate and require a new reviewed `B`.

## Trust, determinism, and privacy

- The renderer MUST run from the trusted bundle with explicit configuration,
  `--offline`, and `--no-exec`; it MUST NOT use provider APIs, network configuration,
  or command processors.
- Canonical files MUST use UTF-8 without BOM, LF, NFC, invariant ordering, a fixed
  `release.yaml` date, and no current-clock fields. Evidence MUST retain full object
  IDs; abbreviated display IDs require collision-safe validation.
- Release tags MUST be SSH-signed annotated tags. Lightweight, unsigned,
  unauthorized, revoked, or replaced tags MUST fail verification.
- The first governed release MAY use one explicit lower-bound tag named exactly
  `changelog-baseline-YYYY-MM-DD` instead of SemVer history, but only after an
  operator verifies that annotated SSH-signed tag with the promoted bundle and records
  both the full tag object ID and full target commit ID in `release-baseline.v1`
  evidence. This baseline is not a release, is not SemVer history, and MUST be ignored
  by strict SemVer release-tag discovery. The selected version remains an external
  steward approval decision; tooling MUST NOT infer first-release eligibility from a
  hardcoded version number.
- Fake SemVer baselines such as `v0.0.0`, lightweight tags, unsigned tags,
  unauthorized signers, recreated/moved tags, wrong dates, wrong targets, and short
  object IDs MUST fail closed. If any governed stable SemVer tag is already reachable
  from the candidate, baseline lower bounds MUST fail closed and the release MUST use
  stable SemVer tags as its lower bound.
- Candidate and final manifests MUST be distinct: the pre-tag candidate digest MAY
  be referenced by tag text, while post-tag evidence MUST record the tag object ID.
- Canonical artifacts MUST omit author/committer identities, raw commit bodies,
  provider identities, tokens, and unbounded error text.

## Disclosure and operation

- Breaking changes MUST NOT be skipped. Every nonbreaking skip MUST include a
  reviewable reason.
- High-impact, grouped, migration, configuration, OpenAPI, operator, and public
  security changes MUST have the structured public evidence required by the release
  engine. Simple low-impact changes MAY remain commit-subject driven.
- Embargoed security information MUST remain outside the public checkout and normal
  public artifacts until authorized disclosure. A public release MAY include only
  approved disclosure fields.
- The tool MUST verify and emit evidence; it MUST NOT approve, tag, push, publish,
  or deploy. A provider adapter MAY perform protected operations only after the
  independent verification and operator-approval boundaries hold.
- The historical report design has not shipped. Implementations MUST NOT add
  compatibility shims for its split-output, candidate-`A`, or renderer-policy model.
