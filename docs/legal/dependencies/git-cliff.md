<!-- ABOUTME: Records the exact git-cliff build-tool pin, provenance, digests, and license decision. -->
<!-- ABOUTME: Defines the local promotion boundary and unresolved supply-chain evidence without overclaiming. -->

# git-cliff Dependency Decision

## Decision

git-cliff `2.13.1` is approved as an operator-supplied build-time renderer under
the MIT license option. It is not a product runtime dependency, hosted provider,
service, NuGet dependency, or transitive product dependency. ISLAMU-owned release
policy supplies normalized context; git-cliff is permitted to render that context
only in a later task.

The MIT option permits use, modification, and redistribution. Any conveyed copy
must retain git-cliff's copyright and MIT permission notice. The license grants no
trademark rights and supplies the component without warranty. No source-offer,
hosting, seat, field-of-use, or commercial-license obligation is known for this
option. Third-party notices shipped with a promoted bundle remain third-party
material and must not be represented as ISLAMU-licensed content.

## Exact Release And Artifacts

- Release: `v2.13.1`, published 2026-04-26.
- Release identity: <https://github.com/orhun/git-cliff/releases/tag/v2.13.1>
- Declared license expression: `MIT OR Apache-2.0`.
- Selected outbound option: MIT.
- Linux x64 musl archive: `git-cliff-2.13.1-x86_64-unknown-linux-musl.tar.gz`
  - Source: <https://github.com/orhun/git-cliff/releases/download/v2.13.1/git-cliff-2.13.1-x86_64-unknown-linux-musl.tar.gz>
  - Archive SHA-256: `200d2535da6d9703f3bcc8a4d159c3b55eacdb01cf2148c55b3eee9dd04d5249`
  - Executable `git-cliff` SHA-256: `25d1281e34da5c45b22d9c174425c1099e7b3aa24c9e1f2d78272df09a6a8dde`
- Windows x64 MSVC archive: `git-cliff-2.13.1-x86_64-pc-windows-msvc.zip`
  - Source: <https://github.com/orhun/git-cliff/releases/download/v2.13.1/git-cliff-2.13.1-x86_64-pc-windows-msvc.zip>
  - Archive SHA-256: `3ae3a5549e85c7ad5b20192ebcfee4371269deca51255f6f2f2e051c6541f5ca`
  - Executable `git-cliff.exe` SHA-256: `af0e46671560e716ec634b398f2eedd45b2b4e01ca1b43445094e6e0bac94039`

The Linux executable was directly observed returning `git-cliff 2.13.1`; its
help exposed `--from-context`, `--offline`, and `--no-exec`. Official usage and
context documentation is at <https://git-cliff.org/docs/usage/args/> and
<https://git-cliff.org/docs/usage/load-context/>. The offline remote behavior is
documented at <https://git-cliff.org/docs/configuration/remote/>.

## Integrity And Promotion Boundary

The official installation page documents PGP-signed tarballs and fingerprint
`1D2D410A741137EBC544826F4A92FA17B6619297`:
<https://git-cliff.org/docs/installation/binary-releases/>. The expected `.asc`
URL for the selected Linux archive returned HTTP 404 on 2026-08-13. No signature
was verified. SHA-256 and HTTPS release provenance are recorded, but PGP status is
explicitly **unverified**.

`eng/release/toolchain.lock.json` is the immutable approval manifest, not an
artifact store. A release operator must separately acquire the official archive,
verify its archive digest, extract only the named executable, verify its executable
digest, retain required notices, and promote the bundle to access-controlled
ISLAMU storage. The release engine accepts only a local bundle directory and never
downloads at runtime. Provider adapters may transport the promoted bundle later;
they do not change the lock or trust decision.

## Inventory, Evidence, And Unknowns

- The official binary archives include the executable and both MIT and Apache-2.0
  license files. Only the named executable is required by the verifier.
- No provider service or network connection is required at release-engine runtime.
- No authoritative SBOM or complete statically linked transitive-component
  inventory was established in this task. Redistribution review must resolve that
  unknown before an archive or executable is conveyed outside controlled build use.
- Windows execution was not performed in the Linux evidence lane; its archive and
  extracted executable digests were recorded.
- PGP verification remains unverified because the expected signature asset was
  unavailable. Promotion policy may require independent signature evidence before
  treating the initial bundle as authoritative.
- ISLAMU-controlled artifact-store location, retention, access policy, and genesis
  promotion approval remain Task 3.3/operator decisions.

The repository dependency-license policy validator originally failed on the
deprecated license-URL metadata of `Microsoft.Data.SqlClient.SNI.runtime 6.0.2`.
The Project Steward approved that exact version only as an optional SQL Server
native runtime under its separate Microsoft redistribution terms. The exception
remains visible in validator output, and PostgreSQL-only published artifacts must
exclude the component. Other versions fail closed and require a fresh review.
The separate FluentAssertions remediation completed on 2026-08-14 by removing
the dependency and converting its tests to native TUnit assertions. The policy
validator now passes while continuing to report the SNI exception above.
