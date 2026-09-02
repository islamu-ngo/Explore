---
description: Contribute under the AGPL, CLA, provenance, and dependency-review boundaries.
---

# Clean-Room IP & Licensing

ISLAMU Event is publicly licensed under AGPL-3.0-or-later. Non-bot contributors sign the project CLA, which can grant alternative licensing rights only for material the contributor has the right to license. The CLA never transfers rights in third-party material.

This page summarizes repository engineering policy, not legal advice.

## Clean-room contribution rule

Do not copy third-party code, SQL, migrations, tests, comments, documentation prose, expressive schemas, images, fonts, or other assets into the repository.

When external research is necessary:

1. Record source titles, URLs, access dates, and the factual behavior being studied.
2. Produce a source-free functional specification containing inputs, outputs, constraints, and standards-required identifiers.
3. Design naming, decomposition, data relationships, control flow, tests, and prose independently using repository-native patterns.
4. Record provenance and abstraction/structure/sequence/organization review evidence.

Exact public standards identifiers and wire values may be used when interoperability requires them; surrounding expression must still be independently authored.

## Dependency changes

Before adding or upgrading a dependency, record its exact version, role, license, material obligations, and effect on every intended outbound licensing path. Run the repository policy validator:

```bash
dotnet run .ci/scripts/validate-dependency-license-policy.cs -- .
```

A passing scanner is minimum engineering evidence, not a legal opinion. Terms that block an intended distribution path require documented approval or rejection of the dependency.

## Contribution checklist

* Confirm every submitted artifact is original, repository-native, or permitted by an identified public standard.
* Keep third-party source and source-derived implementation representations out of implementation context.
* Sign the CLA where required.
* Link the source register and provenance review when external research informed the change.
* Keep secrets, private tenant data, and PII out of issues, commits, tests, screenshots, and research tools.
* Update licensing or governance documentation when obligations change.
