---
name: dotnet-efcore-guidelines
description: "Load for EF Core entities/configurations, DbContext/query filters, repositories/specifications, migrations, model snapshots, transactions, or seeded lookup tables; use query-optimization skill specifically for N+1 or slow SQL diagnosis."
type: pattern
enforcement: suggest
priority: high
---
<!-- ABOUTME: EF Core guidance for Explore.Persistence repositories, DbContext configuration, migrations, query filters, and PostgreSQL-aligned data access. -->
<!-- ABOUTME: Keeps persistence entity-first, tenant-safe, soft-delete-aware, and consistent with lookup seeding and migration discipline. -->

## Must-Read Docs
- [../../../docs/ARCHITECTURE.md](../../../docs/ARCHITECTURE.md)
- [../../../docs/CODEBASE_INSIGHTS.md](../../../docs/CODEBASE_INSIGHTS.md)
- [../../../docs/DOMAIN.md](../../../docs/DOMAIN.md)
- [../../../docs/QUICK_REFERENCE.md](../../../docs/QUICK_REFERENCE.md)
- [../../../src/Explore.Persistence/Seed/LookupTableSeeder.cs](../../../src/Explore.Persistence/Seed/LookupTableSeeder.cs)

## Top 5 Invariants
1. Repositories return entities rather than DTOs, and read-only paths should prefer `AsNoTracking()`.
2. Named `SoftDelete` and `Tenant` query filters belong in `OnModelCreating`, and deleted-row inclusion should prefer `IgnoreQueryFilters([QueryFilterNames.SoftDelete])` so tenant isolation stays active.
3. The pooled DbContext factory sets scoped services such as `TenantContext` and `CurrentUserService` through property injection, and both can be `null` during migrations or seeding.
4. Migration and model-snapshot files are never hand-edited. Fix the EF model/configuration or migration-generation extension, then regenerate an unapplied development migration; applied migrations remain immutable and require a generated corrective migration.
5. Lookup enum IDs and stable codes stay in parity with idempotent `LookupTableSeeder` missing-row repair, and any migration-dependent lookup row is inserted locally before its backfill; model `HasData()` stays forbidden while EF Core #36682 applies.

## Top 5 Anti-Patterns
1. A repository returns a DTO or `IQueryable`, which leaks mapping or EF internals beyond Persistence and weakens Application ownership.
2. Domain entities carry default values for persistence behavior, which hides business intent that belongs in handlers or EF configuration.
3. Runtime request paths disable the `Tenant` filter, which introduces tenant-isolation bugs that are hard to detect.
4. Hand-editing generated migration or snapshot output hides an incorrect EF model and can produce an unreproducible production schema.
5. Model `HasData()` seeds lookup rows while EF Core #36682 applies, which can break migration generation and leaves dependent backfills ordered after unavailable runtime seed data.

## Minimal Examples
```csharp
public sealed class EventStatusConfiguration : IEntityTypeConfiguration<EventStatus>
{
    public void Configure(EntityTypeBuilder<EventStatus> builder)
    {
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);
        builder.HasQueryFilter("Tenant", x => x.TenantId == TenantContext.CurrentTenantId);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => x.MasterCode).IsUnique();
    }
}
```

Generate migrations only after the entity/configuration model is correct:

```bash
dotnet ef migrations add AddEventStatus --context ExploreDbContext --project Explore.Persistence --startup-project Explore.API
```

If the generated output is wrong, fix its model/configuration source, remove the unapplied migration with `dotnet ef migrations remove`, and generate it again. Never patch the generated migration or snapshot.

```csharp
public sealed class EventRepository(ExploreDbContext dbContext) : IEventRepository
{
    public async Task<IReadOnlyList<Event>> ListAsync(
        IQuerySpecification<Event> specification,
        CancellationToken cancellationToken)
    {
        IQueryable<Event> query = dbContext.Events.AsNoTracking();
        query = specification.Apply(query);
        return await query.ToListAsync(cancellationToken);
    }
}
```

## Verification Hooks
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/CleanArchitectureTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
- `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- `dotnet build --configuration Release --verbosity quiet`

## Related Skills
- [../clean-architecture-rules/SKILL.md](../clean-architecture-rules/SKILL.md)
- [../cqrs-mediatr-guidelines/SKILL.md](../cqrs-mediatr-guidelines/SKILL.md)
