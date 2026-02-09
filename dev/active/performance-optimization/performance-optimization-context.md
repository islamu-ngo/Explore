# Performance Optimization - Context

**Last Updated: 2026-02-09**

---

## SESSION PROGRESS (2026-02-09)

### ✅ IMPLEMENTATION COMPLETE (verified by codebase audit)

**Phase 1 - Infrastructure (Program.cs + Persistence)**:
- Response Compression (Brotli + Gzip) - `Program.cs:64-72`
- Output Caching middleware (3 policies: LookupData/ListData/DetailData) - `Program.cs:75-90`
- HybridCache (L1 in-memory + L2 distributed) - `Program.cs:92-102`
- Npgsql retry/timeout/split-query config - `PersistenceServicesRegistration.cs:42-47`
- Newtonsoft.Json removed from API layer - `ExceptionMiddleware.cs` migrated

**Phase 2 - EF Core Query Optimization**:
- AsNoTracking: 157 calls across 47 repository files
- AsSplitQuery: 23 calls across 9 repositories with complex includes
- Compiled queries: 5 repositories (Event, Organization, User, Category, Location)
- Database indexes: 11 new composite indexes across 5 entity configurations
- O(n) algorithm fix in DeleteEventCommandHandler

**Phase 3 - Serialization & API Layer**:
- System.Text.Json source generator: 152 DTO types in ExploreJsonContext
- CancellationToken: 180 parameters across 43 controllers
- PerformanceBehavior pipeline: created + registered in MediatR DI

**Phase 4 - Caching Strategy**:
- FrozenDictionary lookup cache: ILookupDataCache + LookupDataCache + IHostedService
- HybridCache: 17 handler files (5 query handlers cached + 12 command handlers with invalidation)

**Phase 5-6 - Already Present**:
- Named query filters (QueryFilterNames.Tenant/SoftDelete) already in ExploreDbContext
- .NET 10 JIT passive improvements (net10.0 target confirmed)

### ⚠️ IDENTIFIED GAPS (from codebase audit)
- [OutputCache] attributes: Middleware configured but attributes NOT on controllers (being fixed)
- Task 2.4: ExecuteUpdate/ExecuteDelete only in PdsSyncOutboxRepository (pre-existing) - no new bulk ops needed
- Task 2.5: Pagination projection optimization - deferred (needs per-handler work)
- Task 5.1-5.2: C# 14 params Span / collection expressions - no candidates found
- Task 6.3: Npgsql Enlist=false - connection string config, not code
- Phase 7: Benchmarks & arch tests - new project creation, separate effort

### Previous Context
- Deep research on .NET 10 / ASP.NET Core 10 / EF Core 10 / C# 14 performance features
- Codebase exploration (controllers, repositories, DbContext, entities, DI registration)
- Created comprehensive 7-phase implementation plan
- Created task checklist with acceptance criteria

### 🟡 IN PROGRESS
- None (planning phase complete, ready for implementation)

### ⚠️ BLOCKERS
- None identified

---

## Key Findings from Research

### .NET 10 Performance Improvements (November 2025 LTS)
1. **JIT Deabstraction**: Object stack allocation via escape analysis, delegate allocation elimination, better generic specialization
2. **EF Core 10**: Faster `ExpressionVisitor` with cached traversal results, improved row materialization, non-expression lambdas in `ExecuteUpdate`
3. **Kestrel**: Up to 35% faster with smarter connection pooling, lower memory, better HTTP/3
4. **Named Query Filters**: EF Core 10 supports named filters that can be selectively disabled
5. **Complex Type JSON**: `ExecuteUpdate` now supports JSON columns mapped as complex types

### C# 14 Low-Allocation Features
1. **params ReadOnlySpan<T>**: Zero-allocation variadic methods (compiler stack-allocates)
2. **Extension Members**: New extension syntax for cleaner code
3. **field keyword**: Property backing field access without manual field declaration
4. **Null-conditional assignment**: `user?.Profile = LoadProfile();`
5. **.NET 10 JIT**: Automatic stack allocation of short-lived objects, delegate elision

### Key Technologies for This Plan
- **HybridCache**: L1 (memory) + L2 (Redis) with stampede protection (already in csproj!)
- **FrozenDictionary/FrozenSet**: ~2x faster lookups for static data (built into .NET 8+)
- **System.Text.Json Source Generators**: ~30-40% faster serialization, zero reflection
- **Output Caching**: Built-in middleware with tag-based invalidation
- **Compiled Queries**: `EF.CompileAsyncQuery()` eliminates LINQ translation overhead
- **ExecuteUpdate/ExecuteDelete**: Bulk operations without entity materialization

---

## Key Files

### API Layer
- **`Explore.API/Program.cs`** - Main configuration: JSON, middleware, DI. Currently missing: response compression, output caching, HybridCache wiring
- **`Explore.API/Controllers/`** - All REST controllers. Need: CancellationToken, OutputCache attributes
- **`Explore.API/Explore.API.csproj`** - Dependencies. Note: `Newtonsoft.Json` still referenced alongside System.Text.Json. `Microsoft.Extensions.Caching.Hybrid` already added.

### Persistence Layer
- **`Explore.Persistence/ExploreDbContext.cs`** - DbContext with query filters. Need: named filters (EF Core 10), pooling
- **`Explore.Persistence/PersistenceServiceRegistration.cs`** - DI registration. Need: `AddDbContextPool`, `NpgsqlDataSource`
- **`Explore.Persistence/Repositories/`** - All repository implementations. Need: `AsNoTracking`, compiled queries, N+1 fixes
- **`Explore.Persistence/Configurations/`** - EF Core entity configurations. Need: index audit

### Application Layer
- **`Explore.Application/Features/*/Handlers/Queries/`** - Read query handlers. Need: `AsNoTracking`, caching
- **`Explore.Application/Features/*/Handlers/Commands/`** - Write handlers. Need: cache invalidation, `ExecuteUpdate`
- **`Explore.Application/DTOs/`** - All DTOs. Need: Source generator registration
- **`Explore.Application/Profiles/`** - AutoMapper profiles

### Domain Layer
- **`Explore.Domain/Entities/`** - All domain entities with navigation properties

---

## Important Decisions

| Decision | Rationale |
|----------|-----------|
| HybridCache over raw IDistributedCache | Built-in stampede protection, L1+L2, already referenced |
| FrozenDictionary for lookup data | Read-optimized, thread-safe, ~2x faster than Dictionary |
| Source generators over reflection JSON | Compile-time serialization eliminates hot-path reflection |
| Output Caching over Response Caching | Server-side control, tag-based invalidation, Redis backing |
| Keep controllers (not Minimal APIs) | Existing codebase uses controllers; migration cost > perf gain |
| Named query filters (EF Core 10) | Selective filter disabling without `IgnoreQueryFilters()` for all |
| Offset pagination kept for UI | Simple, familiar; add keyset for API consumers only |

---

## Technical Constraints

1. **Multi-tenancy**: All caches MUST include `TenantId` in cache keys. Output cache MUST `VaryByHeader("X-Tenant-Id")`
2. **Soft Delete**: Named query filters must not be bypassed accidentally
3. **Auth**: JWT validation is a hot path - cannot add latency there
4. **PostGIS**: Spatial queries have different optimization strategies than regular queries
5. **Aspire**: Redis connection provided via Aspire service discovery - configure accordingly
6. **AutoMapper**: Still in use for entity-to-DTO mapping. Full removal is out of scope.

---

## Quick Resume

To continue implementation:
1. Read this file for context
2. Check `performance-optimization-tasks.md` for progress
3. Start with **Phase 1** (infrastructure/configuration changes)
4. Each phase can be implemented independently except where dependencies noted
5. Run `dotnet build --configuration Release --verbosity quiet` after each task
6. Run all test projects after each phase
