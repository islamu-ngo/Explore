ABOUTME: Context file for EAV custom properties refactor — tracks progress, key files, and decisions.
ABOUTME: Read this first when resuming work on this task.

# EAV Custom Properties — Context

**Last Updated: 2026-03-04**

---

## SESSION PROGRESS (2026-03-04)

### ✅ COMPLETED
- Research: Plane custom properties architecture (EAV with 4 tables)
- Audit: Full MetadataJson usage across all layers (Domain, DTOs, Handlers, Specs, Repos, Controllers, Blazor)
- Decision: Appearance settings → dedicated columns (not EAV)
- Decision: EAV scoped by EntityTypeName + optional EventTypeId + TenantId
- Decision: Typed value columns (not single-string)
- Plan: Complete dev-docs plan created

### 🟡 IN PROGRESS
- Nothing — planning complete, implementation not started

### ⚠️ BLOCKERS
- None

---

## Quick Resume

1. Read this file → understand current state
2. Read `eav-custom-properties-tasks.md` → see what's next
3. Read `eav-custom-properties-plan.md` → understand full design if needed
4. Start with Phase 1 (Domain entities) — no dependencies
5. Build after each phase to verify compilation

---

## Key Files

### New Files to Create

| File | Purpose | Status |
|------|---------|--------|
| `Explore.Domain/Enums/PropertyType.cs` | Property data type enum | ⏳ |
| `Explore.Domain/Enums/EntityTypeName.cs` | Entity type discriminator enum | ⏳ |
| `Explore.Domain/CustomPropertyDefinition.cs` | Property definition entity | ⏳ |
| `Explore.Domain/CustomPropertyOption.cs` | Dropdown option entity | ⏳ |
| `Explore.Domain/CustomPropertyValue.cs` | Property value entity | ⏳ |
| `Explore.Persistence/Configurations/Entities/CustomPropertyDefinitionConfiguration.cs` | EF config | ⏳ |
| `Explore.Persistence/Configurations/Entities/CustomPropertyOptionConfiguration.cs` | EF config | ⏳ |
| `Explore.Persistence/Configurations/Entities/CustomPropertyValueConfiguration.cs` | EF config | ⏳ |
| `Explore.Application/Contracts/Persistence/ICustomPropertyDefinitionRepository.cs` | Repo interface | ⏳ |
| `Explore.Application/Contracts/Persistence/ICustomPropertyValueRepository.cs` | Repo interface | ⏳ |
| `Explore.Persistence/Repositories/CustomPropertyDefinitionRepository.cs` | Repo impl | ⏳ |
| `Explore.Persistence/Repositories/CustomPropertyValueRepository.cs` | Repo impl | ⏳ |

### Existing Files to Modify

| File | Change | Status |
|------|--------|--------|
| `Explore.Domain/Event.cs:109` | Remove MetadataJson, add appearance columns | ⏳ |
| `Explore.Domain/Organization.cs:60` | Remove MetadataJson, add appearance columns | ⏳ |
| `Explore.Domain/Group.cs:33` | Remove MetadataJson, add branding columns | ⏳ |
| `Explore.Persistence/.../EventConfiguration.cs:99-101` | Remove jsonb config, add appearance columns | ⏳ |
| `Explore.Persistence/.../OrganizationConfiguration.cs:31` | Remove jsonb config, add appearance columns | ⏳ |
| `Explore.Persistence/.../GroupConfiguration.cs:30` | Remove jsonb config, add branding columns | ⏳ |
| `Explore.Persistence/ExploreDbContext.cs` | Add 3 DbSets + query filters | ⏳ |
| `Explore.Persistence/PersistenceServicesRegistration.cs` | Register new repos | ⏳ |
| `Explore.Application/DTOs/Event/CreateEventDto.cs:63` | Remove MetadataJson, add appearance | ⏳ |
| `Explore.Application/DTOs/Event/UpdateEventDto.cs:51` | Remove MetadataJson, add appearance | ⏳ |
| `Explore.Application/DTOs/Event/EventDto.cs:100` | Remove MetadataJson, add appearance | ⏳ |
| `Explore.Application/DTOs/Organization/*.cs` | Remove MetadataJson (4 files) | ⏳ |
| `Explore.Application/DTOs/Group/*.cs` | Remove MetadataJson (4 files) | ⏳ |
| `Explore.Application/Specifications/Events/EventSubqueryFilter.cs:132-204` | Remove JSONB filters | ⏳ |
| `Explore.Persistence/Repositories/EventRepository.cs:279-287` | Remove JSONB cases | ⏳ |
| `Explore.Application/Features/Events/.../GetEventListRequest.cs:225-234` | Remove MetadataJson params | ⏳ |
| `Explore.Application/Features/Events/.../GetEventListRequestHandler.cs:157-161` | Remove MetadataJson dispatch | ⏳ |
| `Explore.Application/Features/Organizations/.../UpdateOrganizationDetailsCommandHandler.cs:76` | Use appearance columns | ⏳ |
| `Explore.Application/Features/Groups/.../UpdateGroupCommandHandler.cs:77` | Use branding columns | ⏳ |
| `Explore.API/Controllers/EventController.cs:151-152` | Remove MetadataJson query params | ⏳ |
| `Explore.Blazor.Client/Helpers/EventAppearanceMetadataHelper.cs` | Refactor: no more JSON parse | ⏳ |
| `Explore.Blazor.Client/Helpers/OrganizationAppearanceMetadataHelper.cs` | Refactor | ⏳ |
| `Explore.Blazor.Client/Helpers/GroupBrandingMetadataHelper.cs` | Refactor | ⏳ |
| Blazor pages (9 files) | Use appearance columns | ⏳ |
| `Explore.Application/Profiles/MappingProfile.cs` | Add EAV mappings | ⏳ |

---

## Important Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | **EAV, not JSONB** | Plane uses normalized tables. Enables typed queries, validation, schema evolution |
| 2 | **3 entities** (Definition, Option, Value) | Minimal viable model; Options are separate for dropdown/select property types |
| 3 | **EntityTypeName enum** as discriminator | Allows same EAV system for Event, Organization, Group without separate tables |
| 4 | **Optional EventTypeId scoping** | Conference events can have different custom props than Workshops (like Plane's IssueType) |
| 5 | **Typed value columns** | TextValue/NumberValue/BooleanValue/DateTimeValue/OptionId — better indexing and querying vs string |
| 6 | **Polymorphic EntityId** (no DB FK) | One value table for all entity types; discriminated by definition's EntityTypeName |
| 7 | **Appearance → dedicated columns** | System-defined visual settings don't belong in user-defined custom fields |
| 8 | **No feature toggle** | Per user: EAV is always active, no settings to enable |
| 9 | **No data migration** | Project is in development; old MetadataJson data is discarded |

---

## Technical Constraints

- **Repositories return entities, never DTOs** — mapping in handlers
- **Validators manually instantiated** — no DI
- **Commands return `BaseCommandResponse<Guid>`**
- **File-scoped namespaces** for all new files
- **ABOUTME headers** on all new files
- **Audit fields** (CreatedAt/By, UpdatedAt/By) via `IAuditableEntity`
- **Soft delete** (IsDeleted, DeletedAt/By) via `ISoftDeletable`
- **Named query filter**: `.HasQueryFilter(name: "SoftDelete", predicate: e => !e.IsDeleted)`
- **Guid PK** for new entities (core aggregates)
- **int PK** for lookups only (EventType already exists as int)
- **ConcurrencyStamp** not required for EAV entities (high-write, last-write-wins is acceptable)

---

## Entity Interface Signatures (For Reference)

```csharp
public interface ITenantEntity { Guid TenantId { get; set; } }
public interface IAuditableEntity {
    DateTime CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}
public interface ISoftDeletable {
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}
```

```csharp
public interface IGenericRepository<T, TKey> where T : class {
    Task<T?> GetById(TKey id);
    Task<IReadOnlyList<T>> GetAll();
    Task<(IReadOnlyList<T> Items, int TotalCount)> GetAllPaged(int pageNumber, int pageSize);
    Task<bool> Exists(TKey id);
    Task<T> Create(T entity);
    Task Update(T entity);
    Task Delete(T entity);
    Task HardDelete(T entity);
}
```

```csharp
public class BaseCommandResponse<TKey> {
    public TKey? Id { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}
```

---

## Related Docs

- `CLAUDE.md` — Project rules
- `docs/DOMAIN.md` — Domain model (must update after implementation)
- `docs/ARCHITECTURE.md` — Architecture (must update)
- `docs/EXTENSIBILITY.md` — Extensibility model (must update — currently references MetadataJson)
- `docs/GOVERNANCE.md` — Naming conventions and patterns
- `.claude/skills/clean-architecture-rules/SKILL.md`
- `.claude/skills/cqrs-mediatr-guidelines/SKILL.md`
- `.claude/skills/dotnet-efcore-guidelines/SKILL.md`
