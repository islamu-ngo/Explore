<!-- ABOUTME: Dependency-license decision gate protecting all intended ISLAMU outbound distribution paths. -->
<!-- ABOUTME: Separates automated metadata checks from legal and commercial distribution approval. -->

# Dependency License Gate

## Required Record

For each added or changed dependency, record:

- component, version, source, and checksum/lock evidence;
- direct/transitive and runtime/build/test/asset/optional-service role;
- authoritative license expression or contract;
- obligations for public AGPL and each intended alternative offering;
- notices, source, patent, trademark, redistribution, hosting, seat, field-of-use, and sublicensing constraints;
- decision and approver.

## Decision

- **Approve:** the assembled offering can lawfully follow every intended outbound model while the third-party component retains its own terms.
- **Replace/version-pin:** a compatible version or dependency provides the required function.
- **Separate-license review:** documented rights cover every intended build, deploy, and distribution; default/community behavior remains explicit.
- **Block:** terms affect ISLAMU-owned material or prohibit an intended outbound model, or authority is unclear.

Passing `.ci/scripts/validate-dependency-license-policy.cs` is mandatory metadata evidence, not legal certification. Unknown metadata, source-available terms, commercial contracts, scanner overrides, assets, datasets, and generated output require human review.

## Existing Dual-Version Precedent

`Directory.Packages.props` defaults to AutoMapper `14.0.0` and MediatR `12.5.0`, the last permissively licensed releases documented by this repository. `UseCommercialLuckyPennyLibraries=true` or Docker build argument `USE_COMMERCIAL_LUCKYPENNY_LIBS=true` explicitly opts into newer commercial versions; `AUTOMAPPER_COMMERCIAL_VERSION` and `MEDIATR_COMMERCIAL_VERSION` may feed build-time overrides, while `LUCKYPENNY_LICENSE_KEY` remains runtime-only. FOSS lock files preserve the default graph.

Reuse the pattern only after a new dependency independently passes this gate. The precedent demonstrates explicit default/opt-in separation; it does not approve another package or license.

## Verification

```bash
dotnet run .ci/scripts/validate-dependency-license-policy.cs -- .
dotnet restore --locked-mode
```

Also inspect the actual release artifact/SBOM for each supported edition when dependency resolution differs by build mode.
