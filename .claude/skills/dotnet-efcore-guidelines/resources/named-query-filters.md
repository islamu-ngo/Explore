ABOUTME: Named query filter pattern for EF Core in this project.
ABOUTME: Highlights multi-filter usage and safe disabling.

# Named Query Filters (EF Core 10+)

Named filters allow multiple global filters on the same entity and selective disabling in specific queries.

## Why Named Filters

- Separate concerns: soft delete, tenancy, lifecycle visibility.
- Disable one filter without disabling all filters.
- Improve readability and auditability of model configuration.

## Configuration Pattern

```csharp
var currentTenantId = tenantContext.TenantId;

modelBuilder.Entity<Event>()
    .HasQueryFilter(name: "SoftDelete", predicate: e => !e.IsDeleted)
    .HasQueryFilter(name: "Tenant", predicate: e => e.TenantId == currentTenantId);
```

## Selective Disable Pattern

Use selective disable only in explicit administrative, migration, or audit flows.

```csharp
var includingDeleted = await db.Events
    .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
    .ToListAsync(cancellationToken);
```

Project helper equivalent:

```csharp
var includingDeleted = await db.Events
    .IgnoreSoftDeleteFilter()
    .ToListAsync(cancellationToken);
```

## Guardrails

- Default application flows should keep filters enabled.
- Disabling tenant filter in runtime request paths is usually a security bug.
- Keep filter names stable and centrally documented.
- `IgnoreQueryFilters()` disables **all** filters and should be reserved for tightly-scoped admin or maintenance flows.
