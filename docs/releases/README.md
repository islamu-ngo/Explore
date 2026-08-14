<!-- ABOUTME: Documents the human-owned release.yaml descriptor contract for governed release lines. -->
<!-- ABOUTME: Separates release descriptors from generated notes and public change fragments. -->

# Governed Releases

This directory contains human-owned release inputs for the future provider-neutral release engine. The current manual release checklist remains authoritative until the trusted release bundle is activated.

Each governed release line uses `docs/releases/<version>/release.yaml` plus `summary.md`. `release-notes.md` is generated later and must not be edited by hand.

## `release.yaml`

`release.yaml` fixes the release identity and operator dispositions before context generation. Unknown keys fail closed.

```yaml
Version: 1.1.0
Line: v1.1
Release-Date: 2026-08-14
Base-Stable-Tag: v1.0.0
Previous-Published-Tag: v1.0.0
Release-Range:
  Base-Ref: v1.0.0
  Base-Oid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
  Previous-Ref: v1.0.0
  Previous-Oid: bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
Compatibility:
  - v1
Impact-Dispositions:
  breaking: not-applicable
  security: coordinated
  migration: planned
  configuration: not-applicable
  openapi: documented
  operator: documented
```

Rules:

- `Version` is SemVer without build metadata.
- `Line` is the active `v<major>.<minor>` release line and must match `Version`.
- `Release-Date` is fixed `yyyy-MM-dd`; canonical output must not use the current clock.
- `Base-Stable-Tag` and `Previous-Published-Tag` are explicit `v<major>.<minor>.<patch>` tags.
- `Release-Range` repeats those exact refs and fixes their full lowercase SHA-1 or SHA-256 object IDs. Short IDs, path-like refs, mismatches, missing fields, and unknown keys fail closed; Task 3.1 will resolve and verify the objects against Git.
- `Compatibility` records the release compatibility references Task 2.3 will use for SemVer and range policy.
- `Impact-Dispositions` must explicitly cover `breaking`, `security`, `migration`, `configuration`, `openapi`, and `operator`.

Simple low-impact `feat`, `fix`, `perf`, `revert`, or release-relevant `docs` commits can remain fragment-free when their Conventional Commit subject is sufficient and no required impact category applies.

## `release-context.v1.json`

The release engine emits deterministic renderer input after `release.yaml`, commit policy, and public fragments validate. The context records the selected SemVer without build metadata, the active `v<major>.<minor>` line, the fixed release date, `Base-Stable-Tag`, `Previous-Published-Tag`, minimum ISLAMU bump, release channel, public changes, and full-object evidence. Display IDs start at 12 hexadecimal characters and extend only when collision-safe; prefix collisions that cannot be made unambiguous fail.

Fragments link to the current Git commit through a terminal `Change-Id: CHG-YYYY-NNNN` commit trailer. A linked fragment enriches that current commit into one context change instead of adding a second synthetic change; missing or duplicate links fail. Prerelease contexts are cumulative from the previous stable tag and never advance `main`. `alpha.N`, `beta.N`, and `rc.N` counters must be contiguous, and stable promotion must keep the same base version as the prior prerelease. Backport entries retain both current commit `oid` and full original `Backport-Of`, but are rendered as backports rather than new capabilities on the older line.

Canonical context excludes authors, emails, forge handles or URLs, raw commit bodies, current-clock values, and ambient-locale formatting. git-cliff bump output is comparison evidence only; any disagreement is a review failure, not release authority.

## Public Change Fragments

High-impact or grouped public changes use immutable fragments under [changes/](changes/). Fragments are linked by stable `Change-Id`; corrections append a new fragment with `Supersedes` instead of mutating or deleting the prior one.
