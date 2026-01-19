# Pagination and Global Query Filters - Context

**Last Updated**: 2026-01-15

## SESSION PROGRESS (2026-01-15)

### ✅ COMPLETED
- Explored codebase: Found 43 controllers with GetAll endpoints
- Researched EF Core pagination (Context7): Skip/Take and Keyset patterns
- Researched Global Query Filters (Context7): Multi-tenant filtering
- Designed comprehensive implementation plan
- Identified 22 tenant-scoped entities and 21 lookup tables
- Created dev-docs structure

### 🟡 IN PROGRESS
- Phase 1: Infrastructure Foundation

### ⏳ NOT STARTED
- Phase 2: Global Query Filters
- Phase 3: Repository Updates
- Phase 4: Request/Handler Updates
- Phase 5: Controller Updates
- Phase 6: Testing & Validation

## Key Files

### Infrastructure (NEW)
- `Explore.Domain/Interfaces/ITenantEntity.cs` - NEW - Tenant entity interface
- `Explore.Application/Responses/PaginatedResult.cs` - NEW - Pagination wrapper
- `Explore.Application/Requests/PaginationParams.cs` - NEW - Input validation

### Core Files to Modify
- `Explore.Application/Contracts/Persistence/IGenericRepository.cs` - Add GetAllPaged
- `Explore.Persistence/Repositories/GenericRepository.cs` - Implement GetAllPaged
- `Explore.Persistence/ExploreDbContext.cs` - Add Global Query Filters

### Reference Patterns
- `Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs` - Request pattern
- `Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs` - Handler pattern
- `Explore.API/Controllers/EventController.cs` - Controller pattern

## Important Decisions Made

1. **Offset Pagination over Keyset**: Simpler implementation, supports random page access
2. **Global Query Filters with null check**: Allows migrations to work without tenant context
3. **ITenantEntity interface**: Clean way to identify tenant-scoped entities
4. **Lookup tables excluded**: Small datasets don't need pagination

## Technical Constraints

1. Validators use manual instantiation (NOT DI)
2. Repositories return ENTITIES, handlers map to DTOs
3. Commands return `BaseCommandResponse<Guid>`
4. GET endpoints are `[AllowAnonymous]`, write endpoints are `[Authorize]`

## Quick Resume

To continue implementation:
1. Read this context file
2. Check tasks.md for current progress
3. Start with Phase 1: Infrastructure Foundation
4. Follow patterns in reference files
