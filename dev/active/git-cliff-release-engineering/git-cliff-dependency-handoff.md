<!-- ABOUTME: Provides the source-free git-cliff runtime facts and constraints approved for implementation. -->
<!-- ABOUTME: Excludes third-party source, snippets, tests, ASTs, prose excerpts, and internal design expression. -->

# Sanitized Git-Cliff Dependency Handoff

Access date: 2026-08-13. Public official release pages and documentation were
lawfully accessed for observable release, CLI, integrity, and license facts.

## Facts

- Latest stable official release: git-cliff `v2.13.1`, published 2026-04-26:
  <https://github.com/orhun/git-cliff/releases/tag/v2.13.1>.
- Official CLI documentation identifies `--from-context`, `--offline`, and
  `--no-exec`: <https://git-cliff.org/docs/usage/args/>.
- Official context documentation identifies JSON context input by file or standard
  input: <https://git-cliff.org/docs/usage/load-context/>.
- Official remote documentation states that offline mode prevents external calls
  even when remote configuration exists:
  <https://git-cliff.org/docs/configuration/remote/>.
- Official project license expression: `MIT OR Apache-2.0`:
  <https://github.com/orhun/git-cliff>.
- Official binary-release documentation identifies PGP-signed tarballs and
  fingerprint `1D2D410A741137EBC544826F4A92FA17B6619297`:
  <https://git-cliff.org/docs/installation/binary-releases/>.
- Approved targets are Linux x64 musl and Windows x64 MSVC. Exact archive and
  executable SHA-256 values are recorded in `eng/release/toolchain.lock.json` and
  `docs/legal/dependencies/git-cliff.md`.
- The selected Linux executable directly returned `git-cliff 2.13.1` and exposed
  all three required flags in its help output.
- The expected Linux archive `.asc` URL returned HTTP 404. PGP verification is
  unverified.

## Implementation Constraints

- Parse a versioned JSON lock and accept only explicitly listed OS/architecture
  records.
- Require a caller-supplied local bundle directory and the exact executable name.
- Verify executable SHA-256 before execution.
- Invoke `--version` through `ProcessStartInfo.ArgumentList` with redirected,
  bounded output and a bounded timeout.
- Require the exact locked version response.
- Fail closed for absent or malformed lock, absent bundle or executable, unsupported
  platform, digest mismatch, process failure, excessive output, timeout, or version
  mismatch.
- Never download at runtime and never infer trust from a provider or ambient PATH.
- git-cliff may later render normalized context only; it does not own ISLAMU release
  selection, classification, versioning, ranges, trust, or canonical evidence.

## Research Tool Constraints

- Tavily was attempted directly on 2026-08-13 and failed with HTTP 432 usage limit.
- Context7 was attempted twice on 2026-08-13 and failed with OAuth `invalid_grant`.
- Official web release pages and documentation were used as the fallback.

## Clean-Room Attestation

This handoff contains facts, observable behavior, constraints, URLs, identifiers
required for interoperability, and independently measured digests only. It contains
no third-party implementation source, snippet, AST, decompiled artifact, SQL,
migration, test, comment, copied documentation prose, or asset.
