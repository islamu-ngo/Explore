---
description: >-
  Run focused project-level tests and choose invariant-oriented evidence for
  each change.
---

# TUnit

The repository uses TUnit and runs tests project by project. Do not use solution-level `dotnet test` as the normal verification path.

## Focused development loop

Build first when product code changed:

```bash
dotnet build --configuration Release --verbosity quiet
```

Then run the owning test project with an exact TUnit tree-node filter:

```bash
dotnet test --project tests/<TestProject>/<TestProject>.csproj \
  --configuration Release \
  --treenode-filter "/*/*/*<TestClass>/*"
```

`--treenode-filter` is the supported TUnit selector; VSTest `--filter` is not interchangeable.

## Choose evidence by risk

Tests should protect behavior that could harm users or operators if it regresses:

* domain state transitions and invalid transitions;
* tenant isolation and fail-closed authorization;
* payment, refund, and erasure idempotency;
* concurrency and replay behavior;
* provider portability where the change affects persistence or infrastructure;
* wire-contract compatibility for API and generated clients.

Avoid tests that merely mirror mock calls, scrape source text, or test framework behavior.

## Infrastructure-backed lanes

Provider-specific suites intentionally use real infrastructure where semantics matter. The repository has focused lanes for PostgreSQL, SQLite, SQL Server, MariaDB, MySQL, privacy-erasure restore, email and messaging, and other runtime boundaries. Run only the lanes implicated by the change during development; broader project or release suites are phase-exit evidence.

## Documentation-only changes

For GitBook or Markdown-only work, do not run unrelated .NET builds or test suites. Validate links, structure, rendered content, and the live public surface instead.
