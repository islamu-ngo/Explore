---
name: optimize-ef-core-queries
description: "Load when EF Core reads are slow, emit N+1 or excessive SQL, over-fetch, track too many entities, cause high DB load, or need projection/split-query/compiled-query analysis; not for general migrations, DbContext setup, or repository conventions."
type: pattern
enforcement: suggest
priority: high
license: MIT
---
<!-- ABOUTME: Measurement-first workflow for improving EF Core query shape and cost. -->
<!-- ABOUTME: Preserves repository, tenant, authorization, and tracking boundaries. -->

# Optimize EF Core Queries

## Rules

- Measure query count, generated SQL, duration, rows, and allocations before and after the change.
- Preserve tenant filters, authorization predicates, ordering, pagination, and result semantics.
- Repositories return entities; handlers do not receive `IQueryable` or access `DbContext`.
- Prefer server-side projection for summaries; use `Include` only when the entity graph is actually needed.
- Use `AsNoTracking()` for read-only entity loads and identity resolution only when duplicate entity instances matter.
- Choose split queries for large/multiple collections only after considering extra round trips and consistency.
- Use compiled queries only for measured hot paths where compilation overhead is material.
- Never enable sensitive-data logging outside a safe local development environment.
- Raw SQL is the last rung and must remain parameterized.

## Workflow

1. Locate every caller and reproduce the slow path with representative cardinality.
2. Capture EF command logs or diagnostics and identify N+1, over-fetch, tracking, cartesian expansion, premature materialization, or repeated compilation.
3. Apply the smallest query-shape fix:
   - move filters before materialization;
   - replace existence `Count` with `Any`;
   - project only required values;
   - remove lazy loading from loops;
   - add `AsNoTracking` to read-only entity loads;
   - use `AsSplitQuery` for proven collection-join explosion.
4. Inspect the resulting SQL and query count.
5. Run the caller's integration test with tenant isolation and realistic data volume.

## PostgreSQL And Provider Checks

PostgreSQL is the production reference provider. Configure diagnostics through Npgsql and log completed database commands without parameter values:

```csharp
options.UseNpgsql(connectionString)
    .LogTo(writeLog, [DbLoggerCategory.Database.Command.Name], LogLevel.Information);
```

- Use `ToQueryString()` to inspect translation, then verify actual duration and row counts from command diagnostics.
- Use PostgreSQL `EXPLAIN (ANALYZE, BUFFERS)` with representative parameters for measured hot paths; a sequential scan is not automatically wrong for small or low-selectivity sets.
- Check index use, sort/spill behavior, join cardinality, and round trips rather than applying SQL Server plan assumptions.
- For every other configured provider, run its own behavioral test because translation, collation, null semantics, and index choices can differ. Optimize the shared LINQ shape first and isolate provider-specific SQL only when measurement proves it necessary.

## Checks

- No client-side evaluation or N+1 command pattern appears in logs.
- Query count and selected columns match the intended shape.
- Read-only optimization did not break a later write path.
- Tenant isolation and authorization tests still pass.
- The measured bottleneck improved; otherwise revert the speculative optimization.

## Verification

- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- Also use [EF Core project conventions](../dotnet-efcore-guidelines/SKILL.md) when repository or DbContext behavior changes.
