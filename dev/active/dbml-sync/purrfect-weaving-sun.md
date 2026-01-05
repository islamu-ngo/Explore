# DBML Sync - Comprehensive Analysis & Refactored Implementation Plan

**Last Updated:** 2026-01-04
**Status:** Analysis Complete - Ready for User Review
**Task:** Verify and refactor DBML sync plan based on actual codebase patterns

---

## Executive Summary

This document presents a comprehensive analysis comparing the existing DBML sync plan against the **actual implementation patterns** used in the ISLAMU Event codebase (specifically the Organization entity implementation). Based on this analysis, I've identified **critical gaps, inconsistencies, and areas requiring refinement** in the current plan.

### Key Findings

✅ **What the Plan Got Right:**
- DBML as source of truth approach
- Phase structure (Discovery → Domain → Application → Persistence → API)
- Clean Architecture layer separation
- Multi-tenancy considerations

❌ **Critical Gaps Identified:**
1. **Missing Codebase Conventions**: Plan lacks specific file structure patterns actually used
2. **DTO Naming Patterns**: Plan doesn't specify actual DTO naming used (CreateXDto, XListDto, etc.)
3. **Response Patterns**: Missing BaseCommandResponse<T> pattern actually used
4. **Validation Pipeline**: Plan assumes inline validation, but codebase uses FluentValidation separately
5. **Repository Return Types**: Actual repos return DTOs, not entities
6. **API Versioning**: Inconsistency in routing (/api/v1 vs /api/)
7. **User ID Extraction**: Pattern needs centralization
8. **Missing OrganizationReview Entity**: DBML doesn't include this existing entity

---

## Part 1: Analysis of Current Implementation Patterns

### 1.1 Domain Layer Patterns (from Organization Example)

**File Location Pattern:**
```
Explore.Domain/
├── {Entity}.cs                    // e.g., Organization.cs
├── {SubEntity}.cs                 // e.g., OrganizationMember.cs
├── Enums/
│   └── {Entity}Enum.cs           // e.g., OrganizationRoleEnum.cs
```

**Entity Conventions Observed:**
- ✅ **Primary Key**: `public Guid Id { get; set; }` for entities
- ✅ **Primary Key**: `public int Id { get; set; }` for lookup tables
- ✅ **Navigation Properties**: PascalCase with virtual keyword optional
- ✅ **Foreign Keys**: Explicit properties with `[ForeignKey]` annotation IN DOMAIN (contrary to docs)
- ⚠️ **Issue**: Some entities use data annotations despite "no EF in Domain" guideline

**Enum Pattern:**
```csharp
public enum OrganizationRoleEnum
{
    Founder = 1,
    Director = 2,
    Member = 3
    // Explicit integer values
}
```

**Naming Standard:**
- Entity: `Organization`
- Enum: `OrganizationRoleEnum`
- Sub-entity: `OrganizationMember`

---

### 1.2 Application Layer Patterns

**File Structure:**
```
Explore.Application/
├── DTOs/
│   └── {Entity}/
│       ├── {Entity}Dto.cs                    // Detail view
│       ├── {Entity}ListDto.cs                // List view
│       ├── Create{Entity}Dto.cs              // Create request
│       ├── Update{Entity}Dto.cs              // Update request
│       ├── Update{Entity}{Feature}Dto.cs     // Specific updates
│       └── Validators/
│           ├── Create{Entity}DtoValidator.cs
│           └── Update{Entity}DtoValidator.cs
├── Features/
│   └── {EntityPlural}/                        // e.g., Organizations
│       ├── Requests/
│       │   ├── Commands/
│       │   │   ├── Create{Entity}Command.cs
│       │   │   └── Update{Entity}Command.cs
│       │   └── Queries/
│       │       ├── Get{Entity}DetailsRequest.cs
│       │       ├── Get{Entity}ListRequest.cs
│       │       └── GetMy{EntityPlural}Request.cs
│       └── Handlers/
│           ├── Commands/
│           │   ├── Create{Entity}CommandHandler.cs
│           │   └── Update{Entity}CommandHandler.cs
│           └── Queries/
│               ├── Get{Entity}DetailsRequestHandler.cs
│               └── Get{Entity}ListRequestHandler.cs
├── Contracts/
│   └── Persistence/
│       └── I{Entity}Repository.cs
└── Profiles/
    └── MappingProfile.cs                      // Centralized AutoMapper
```

**DTO Naming Patterns:**
```csharp
// Detail view DTO
public class OrganizationDto { ... }

// List view DTO (lighter weight)
public class OrganizationListDto { ... }

// Create DTO (input)
public class CreateOrganizationDto { ... }

// Update DTO (input)
public class UpdateOrganizationDto { ... }

// Specialized update DTO
public class UpdateOrganizationApprovalStatusDto { ... }
```

**Command Pattern:**
```csharp
public class CreateOrganizationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateOrganizationDto OrganizationDto { get; set; }
    public string? UserId { get; set; }
}
```

**Query Pattern:**
```csharp
public class GetOrganizationDetailsRequest : IRequest<OrganizationDto>
{
    public Guid Id { get; set; }
}

public class GetOrganizationListRequest : IRequest<List<OrganizationListDto>>
{
    public Guid Id { get; set; }
}
```

**Response Pattern:**
```csharp
// Commands return structured response
BaseCommandResponse<Guid> with:
  - Id
  - Success
  - Message
  - Errors (List<string>)

// Queries return DTOs directly
OrganizationDto
List<OrganizationListDto>

// Some updates return Unit (fire-and-forget)
```

**Validation Pattern:**
```csharp
public class CreateOrganizationDtoValidator : AbstractValidator<CreateOrganizationDto>
{
    private readonly IApprovalStatusRepository _repository;

    public CreateOrganizationDtoValidator(IApprovalStatusRepository repository)
    {
        _repository = repository;

        RuleFor(p => p.FullName)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(100).WithMessage("{PropertyName} must not exceed 100 characters.");

        RuleFor(p => p.Email)
            .EmailAddress().WithMessage("{PropertyName} must be a valid email address.");

        RuleFor(p => p.WebsiteUrl)
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            .When(p => p.WebsiteUrl.Length > 0)
            .WithMessage("{PropertyName} must be a valid Uri.");
    }
}
```

**Repository Interface Pattern:**
```csharp
public interface IOrganizationRepository : IGenericRepository<Organization, Guid>
{
    // IMPORTANT: Methods return DTOs, not entities
    Task<OrganizationDto> GetOrganizationWithDetails(Guid id);
    Task<List<OrganizationListDto>> GetOrganizationsWithDetails();
    Task<List<OrganizationListDto>> GetMyOrganizations(string userId);
}

public interface IGenericRepository<T, TKey> where T : class
{
    Task<T?> GetById(TKey id);
    Task<IReadOnlyList<T>> GetAll();
    Task<bool> Exists(TKey id);
    Task<T> Create(T entity);
    Task Update(T entity);
    Task Delete(T entity);
}
```

**AutoMapper Profile Pattern:**
```csharp
// In MappingProfile.cs
CreateMap<Organization, OrganizationDto>().ReverseMap();
CreateMap<Organization, OrganizationListDto>();  // One-way
CreateMap<Organization, CreateOrganizationDto>().ReverseMap();
CreateMap<Organization, UpdateOrganizationApprovalStatusDto>().ReverseMap();
```

---

### 1.3 Persistence Layer Patterns

**File Structure:**
```
Explore.Persistence/
├── ExploreDbContext.cs
├── Configurations/
│   └── Entities/
│       ├── {Entity}Configuration.cs
│       └── {SubEntity}Configuration.cs
└── Repositories/
    ├── GenericRepository.cs
    └── {Entity}Repository.cs
```

**Entity Configuration Pattern:**
```csharp
public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        // Table name (usually matches entity name)
        builder.ToTable("Organizations"); // or implicit

        // UUID v7 generation for primary key
        builder.Property(e => e.Id)
            .HasDefaultValueSql("uuidv7()");

        // Default values for enums
        builder.Property(e => e.ApprovalStatusId)
            .HasDefaultValue((int)ApprovalStatusEnum.Pending);

        // Required fields and max lengths
        builder.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(500);

        // Foreign key relationships
        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .IsRequired(false);

        // Seed data
        builder.HasData(
            new Organization
            {
                Id = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
                FullName = "ISLAMU",
                ...
            });
    }
}
```

**Repository Implementation Pattern:**
```csharp
public class OrganizationRepository : GenericRepository<Organization, Guid>, IOrganizationRepository
{
    public OrganizationRepository(ExploreDbContext context) : base(context) { }

    // CRITICAL: Returns DTOs, not entities
    public async Task<OrganizationDto> GetOrganizationWithDetails(Guid id)
    {
        var organization = await _context.Organizations
            .Include(o => o.ApprovalStatus)
            .FirstOrDefaultAsync(o => o.Id == id);

        // Manual mapping or projection
        return new OrganizationDto
        {
            Id = organization.Id,
            FullName = organization.FullName,
            ApprovalStatusFullName = organization.ApprovalStatus.FullName,
            ...
        };
    }

    public async Task<List<OrganizationListDto>> GetOrganizationsWithDetails()
    {
        return await _context.Organizations
            .Include(o => o.ApprovalStatus)
            .Select(o => new OrganizationListDto
            {
                Id = o.Id,
                FullName = o.FullName,
                ApprovalStatusFullName = o.ApprovalStatus.FullName,
                ...
            })
            .ToListAsync();
    }
}
```

**DbContext Pattern:**
```csharp
public class ExploreDbContext : DbContext
{
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExploreDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

---

### 1.4 API Layer Patterns

**File Structure:**
```
Explore.API/
├── Controllers/
│   └── {Entity}Controller.cs
└── Middleware/
    └── ...
```

**Controller Pattern:**
```csharp
[Route("api/v1/[controller]")]  // ⚠️ Should be standardized
[ApiController]
public class OrganizationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrganizationController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    [EndpointSummary("Get all Organizations")]
    [EndpointDescription("Get A List of all the Organizations")]
    [AllowAnonymous]
    public async Task<ActionResult<List<OrganizationListDto>>> GetAll()
    {
        var organizations = await _mediator.Send(new GetOrganizationListRequest());
        return Ok(organizations);
    }

    [HttpGet("my")]
    [EndpointSummary("Get my Organizations")]
    [Authorize]
    public async Task<ActionResult<List<OrganizationListDto>>> GetMyOrganizations()
    {
        // ⚠️ User ID extraction pattern needs centralization
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var organizations = await _mediator.Send(new GetMyOrganizationsRequest { UserId = userId });
        return Ok(organizations);
    }

    [HttpPost]
    [EndpointSummary("Create Organization")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateOrganizationDto dto)
    {
        var userId = ExtractUserId(); // Should be helper method

        var command = new CreateOrganizationCommand
        {
            OrganizationDto = dto,
            UserId = userId
        };

        var response = await _mediator.Send(command);
        return Ok(response);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Put(Guid id, [FromBody] UpdateOrganizationDto dto)
    {
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var command = new UpdateOrganizationDetailsCommand
        {
            Id = id,
            UserId = userId,
            OrganizationDto = dto
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
```

**Endpoint Patterns Observed:**
- `GET /api/v1/organization` - List all
- `GET /api/v1/organization/my` - User-specific list
- `GET /api/v1/organization/{id}` - Get details
- `POST /api/v1/organization` - Create
- `PUT /api/v1/organization/{id}` - Update details
- `PUT /api/v1/organization/updatestatustype/{id}` - Update specific field

---

## Part 2: Critical Issues in DBML Schema

### 2.1 Missing Entities in DBML

**OrganizationReview** exists in codebase but NOT in DBML:
```csharp
public class OrganizationReview
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string ReviewerName { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Action Required:** Add to DBML or mark for removal.

---

### 2.2 Type Mismatches

**atproto_record types in DBML:**
```dbml
Table "atproto_record" {
  "id" uuid [pk, not null]
  "did" uuid [not null]              // ❌ Should be varchar
  "collection" varchar(500) [not null]
  "record_key" uuid [not null]       // ❌ Should be varchar
  "cid" uuid                          // ❌ Should be varchar
  "uri" varchar(500)
  "indexed_at" timestamp
}
```

**Actual ATProto standards:**
- `did`: String (e.g., "did:plc:xyz123")
- `record_key`: String (rkey)
- `cid`: String (Content Identifier hash)

**Action Required:** Update DBML types to varchar.

---

### 2.3 Field Name Inconsistencies

**DBML uses:**
- `full_name`
- `master_code`
- `tenant_id`

**Codebase uses:**
- `FullName` (PascalCase in entities)
- `MasterCode`
- `TenantId`

**Resolution:** This is acceptable - EF Core configurations map between conventions.

---

### 2.4 Missing Tenant ID in Some Tables

**Tables missing tenant_id that should have it:**
- `event_session_agenda_items` ✅ Has it
- `actor_key_store` ❌ Missing
- `user_authentication_token` ❌ Missing
- `user_external_login` ❌ Missing

**Action Required:** Verify multi-tenancy scope and add tenant_id where needed.

---

## Part 3: Routing Inconsistencies

**Current State:**
- `OrganizationController`: `[Route("api/[controller]")]` → `/api/organization`
- `EventController`: `[Route("api/v1/[controller]")]` → `/api/v1/event`

**Decision Required:** Standardize on `/api/v1/[controller]` pattern.

---

## Part 4: Refactored Implementation Plan

### Phase 0: Discovery & Alignment Spec ✅ ENHANCED

**Deliverables:**

#### 0.1 Entity Mapping Table

| DBML Table | Domain Entity | Aggregate Root | Notes |
|------------|---------------|----------------|-------|
| tenant | Tenant | ✅ Yes | Core multi-tenancy |
| tenant_user | TenantUser | No (under Tenant) | Junction table |
| tenant_settings | TenantSettings | No (under Tenant) | Owned entity |
| user | User | ✅ Yes | Identity aggregate |
| user_role | UserRole | No | Lookup table |
| user_authentication_token | UserAuthenticationToken | No (under User) | Sub-entity |
| user_external_login | UserExternalLogin | No (under User) | Sub-entity |
| actor | Actor | ✅ Yes | Federation aggregate |
| actor_type | ActorType | No | Lookup table (enum) |
| actor_key_store | ActorKeyStore | No (under Actor) | Owned/sub-entity |
| did_custody_type | DidCustodyType | No | Lookup table (enum) |
| organization | Organization | ✅ Yes | Core aggregate |
| organization_members | OrganizationMember | No (under Organization) | Junction entity |
| organization_role | OrganizationRole | No | Lookup table (enum) |
| organization_position | OrganizationPosition | No | Lookup table (enum) |
| approval_status | ApprovalStatus | No | Lookup table (enum) |
| event | Event | ✅ Yes | Core aggregate |
| event_session | EventSession | No (under Event) | Sub-entity |
| event_session_agenda_items | EventSessionAgendaItem | No (under EventSession) | Sub-entity |
| event_session_languages | EventSessionLanguage | No (under EventSession) | Junction entity |
| event_session_speakers | EventSessionSpeaker | No (under EventSession) | Junction entity |
| event_registration | EventRegistration | ✅ Yes | Separate aggregate |
| event_categories | EventCategory | No (under Event) | Junction entity |
| event_tags | EventTag | No (under Event) | Junction entity |
| event_type | EventType | No | Lookup table (enum) |
| event_status | EventStatus | No | Lookup table (enum) |
| event_format | EventFormat | No | Lookup table (enum) |
| visibility_type | VisibilityType | No | Lookup table (enum) |
| registration_mode | RegistrationMode | No | Lookup table (enum) |
| category | Category | ✅ Yes | Hierarchical aggregate |
| tag | Tag | ✅ Yes | Discovery aggregate |
| tag_type | TagType | No | Lookup table (enum) |
| tag_type_tags | TagTypeTag | No (under Tag) | Junction entity |
| madhab | Madhab | No | Lookup table (enum) |
| audience_age | AudienceAge | No | Lookup table (enum) |
| audience_gender | AudienceGender | No | Lookup table (enum) |
| language | Language | No | Lookup table (enum) |
| location | Location | ✅ Yes | Venue aggregate |
| storage_object | StorageObject | ✅ Yes | File management |
| file_type | FileType | No | Lookup table (enum) |
| indexed_did | IndexedDid | ✅ Yes | ATProto indexer |
| sync_state | SyncState | ✅ Yes | System state |
| atproto_record | AtProtoRecord | No | Shared value object |

#### 0.2 CQRS Use Case Mapping

**Events:**
- Create Event Command
- Update Event Command
- Get Event Details Query
- List Events Query (with filters: visibility, status, gender, age, madhab, tags, categories, format, dates)
- Search Events Query

**Event Sessions:**
- Create Event Session Command
- Update Event Session Command
- Get Event Session Details Query
- List Event Sessions Query (by event)
- Add Agenda Item Command
- Update Agenda Item Command
- Add Speaker Command
- Remove Speaker Command
- Set Session Languages Command

**Event Registrations:**
- Register for Session Command
- Approve Registration Command
- Reject Registration Command
- Cancel Registration Command
- List Registrations Query (by session, by user)

**Organizations:**
- Apply for Organization Command (creates with Pending status)
- Update Organization Command
- Approve Organization Command
- Reject Organization Command
- Add Member Command
- Update Member Role Command
- Remove Member Command
- Get Organization Details Query
- List Organizations Query
- Get My Organizations Query

**Tags & Categories:**
- List Tags Query
- Search Tags Query
- List Categories Query (with tree structure)
- Search Categories Query

**Locations:**
- Create Location Command
- Update Location Command
- Get Location Details Query
- Search Locations Query (by city/country/geo)

**Actors (Federation):**
- Resolve Actor Query
- Index Actor Command (internal)
- Update Actor Command

**ATProto Records (Internal):**
- Upsert ATProto Record Command
- Link Record to Event Command
- Link Record to Registration Command

#### 0.3 Critical Design Decisions

**Decision 1: atproto_record Types**
- ✅ **DECISION**: Change DBML from uuid to varchar
  - `did`: varchar(255)
  - `record_key`: varchar(500)
  - `cid`: varchar(255)
  - **Rationale**: ATProto DIDs and CIDs are strings, not UUIDs

**Decision 2: Location Geo Modeling**
- ✅ **DECISION**: Use PostGIS geometry + lat/long for compatibility
  - Primary: `coordinates` (PostGIS point type for spatial queries)
  - Auxiliary: `latitude`/`longitude` (for simple distance calculations)
  - Index: GiST index on coordinates column
  - **Rationale**: Best of both worlds - spatial queries + simple calculations

**Decision 3: Tenant Enforcement Strategy**
- ✅ **DECISION**: Multi-layered approach
  1. **Persistence Layer**: Global query filters on `tenant_id`
  2. **Repository Layer**: Tenant-scoped method signatures where applicable
  3. **Middleware**: Tenant resolution from subdomain/header
  4. **Application Layer**: Commands include tenant context
  - **Rationale**: Defense in depth, prevents cross-tenant leaks

**Decision 4: Join Table Modeling**
- ✅ **DECISION**: Explicit entities for all join tables with tenant_id
  - EventCategory (explicit entity)
  - EventTag (explicit entity)
  - EventSessionLanguage (explicit entity)
  - EventSessionSpeaker (explicit entity)
  - TagTypeTag (explicit entity)
  - OrganizationMember (explicit entity)
  - TenantUser (explicit entity)
  - **Rationale**: Preserves tenant_id, enables constraints, clearer intent

**Decision 5: API Contract Compatibility**
- ✅ **DECISION**: Versioned endpoints (/api/v1/)
  - New DTOs follow existing patterns
  - Breaking changes require new version (/api/v2/)
  - Maintain backward compatibility where possible
  - **Rationale**: Production API stability

**Decision 6: Delete Behaviors**
- ✅ **DECISION**: Explicit cascade behaviors
  - Event → EventSession: Cascade
  - EventSession → AgendaItem: Cascade
  - Event → Registration: Restrict (orphan check)
  - Organization → OrganizationMember: Cascade
  - Category → Category (parent): Restrict
  - **Rationale**: Data integrity, prevent orphans

**Decision 7: User ID Extraction**
- ✅ **DECISION**: Centralize in extension method
  ```csharp
  public static class ClaimsPrincipalExtensions
  {
      public static string? GetUserId(this ClaimsPrincipal principal)
      {
          return principal.FindFirst("sub")?.Value
              ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
              ?? principal.FindFirst("sid")?.Value;
      }
  }
  ```

**Decision 8: Repository Return Types**
- ✅ **DECISION**: Repositories return DTOs for queries, entities for commands
  - Query methods: Return DTOs (GetOrganizationWithDetails → OrganizationDto)
  - Command methods: Work with entities (Create/Update/Delete)
  - **Rationale**: Matches current pattern, reduces mapping in handlers

---

### Phase 1: Domain Layer Implementation

**Acceptance Criteria:**
- [ ] All entities compile without errors
- [ ] No EF Core references in Domain (except [ForeignKey] if pattern continues)
- [ ] All relationships defined correctly
- [ ] Enums follow {Entity}Enum naming pattern
- [ ] UUIDs for entities, int for lookups

**Estimated Effort:** 2-3 days

#### 1.1 Core Aggregates (Priority 1)

**Tenant System:**
- [ ] Create `Tenant` entity
  - [ ] Properties: Id (Guid), FullName, Slug, IsActive
  - [ ] Navigation: TenantUsers, TenantSettings
- [ ] Create `TenantUser` entity
  - [ ] Properties: Id (int), UserId (Guid), TenantId (Guid), UserRoleId (int)
  - [ ] FK: User, Tenant, UserRole
- [ ] Create `TenantSettings` entity
  - [ ] Properties: Id (int), TenantId (Guid), [JSON config blob]
  - [ ] FK: Tenant

**User System:**
- [ ] Update/Create `User` entity
  - [ ] Properties: Id (Guid), Email, FirstName, LastName, ActorId?, AuthProvider, AuthProviderId, DefaultActorId?, EmailVerified
  - [ ] Navigation: UserAuthenticationTokens, UserExternalLogins, TenantUsers
- [ ] Create `UserAuthenticationToken` entity
  - [ ] Properties: Id (Guid), UserId, Provider, AccessToken, RefreshToken, PdsHost, DpopKey, IdToken, ExpiresAt
- [ ] Create `UserExternalLogin` entity
  - [ ] Properties: Id (Guid), UserId, Provider, ProviderKey, ProviderDisplayName

**Actor System (Federation):**
- [ ] Create `Actor` entity
  - [ ] Properties: Id (Guid), ActorTypeId (int), TenantId, DisplayName, ProfilePicture?, Did?, Handle?, DidCustodyTypeId?, PdsHost?, Description, IndexedAt?, ProfilePictureCid?, ProfilePictureUri?
  - [ ] Navigation: ActorType, StorageObject (profile picture), ActorKeyStores
- [ ] Create `ActorKeyStore` entity
  - [ ] Properties: Id (Guid), ActorId, KeyPurpose, PrivateKeyEncrypted, PublicKey, IsActive, CreatedAt

**Organization System:**
- [ ] Update `Organization` entity to match DBML
  - [ ] Add missing properties: TenantId, ActorId?
  - [ ] Ensure: Id (Guid), FullName, Email, Country, City, Address, Postcode, WebsiteUrl?, ApprovalStatusId, TenantId, ActorId?
- [ ] Update `OrganizationMember` entity
  - [ ] Add: OrganizationPosition?
  - [ ] Change: Role → OrganizationRoleId (int)
- [ ] Verify `OrganizationReview` (NOT in DBML - document decision)

**Event System:**
- [ ] Update/Create `Event` entity
  - [ ] Properties: Id (Guid), EventTypeId, Title, Description, AudienceGenderId, AudienceAgeId, ActorId, Price?, CurrencyCode?, FeaturedImage, TotalViews, IsRegistrationRequired, EventUrl?, MadhabId?, TenantId, Slug?, VisibilityTypeId, SessionCount?, EventStatusId, ExternalRegistrationUrl?, FirstSessionDate?, LastSessionDate?, Timezone?, EventFormatId, AtProtoRecordId?
  - [ ] Navigation: EventSessions, EventCategories, EventTags, Actor, FeaturedImageStorage
- [ ] Create `EventSession` entity
  - [ ] Properties: Id (Guid), EventId, StartTime (timestamptz), EndTime (timestamptz), LocationId?, Title?, TenantId, Slug?, MaxAudienceAttendees?, CurrentAudienceAttendees?, RegistrationModeId?, Description?
  - [ ] Navigation: Event, Location, EventSessionAgendaItems, EventSessionLanguages, EventSessionSpeakers
- [ ] Create `EventSessionAgendaItem` entity
  - [ ] Properties: Id (Guid), EventSessionId, StartTime (timestamp), EndTime, Title, Description?, LocationId?, TenantId
- [ ] Create `EventSessionLanguage` entity (junction)
  - [ ] Properties: Id (int), EventSessionId, LanguageId, TenantId
- [ ] Create `EventSessionSpeaker` entity (junction)
  - [ ] Properties: Id (int), ActorId, EventSessionId, TenantId
- [ ] Create `EventCategory` entity (junction)
  - [ ] Properties: Id (int), EventId, CategoryId, TenantId
- [ ] Create `EventTag` entity (junction)
  - [ ] Properties: Id (int), EventId, TagId, TenantId
- [ ] Create `EventRegistration` entity
  - [ ] Properties: Id (Guid), UserId, EventSessionId, ApprovalStatusId?, TenantId, AtProtoRecordId?

**Discovery System:**
- [ ] Create `Category` entity (hierarchical)
  - [ ] Properties: Id (Guid), MasterCode, FullName, ParentId?, TenantId
  - [ ] Navigation: Parent (self-referential), Children
- [ ] Create `Tag` entity
  - [ ] Properties: Id (Guid), MasterCode, FullName, TenantId
  - [ ] Navigation: TagTypeTags
- [ ] Create `TagTypeTag` entity (junction)
  - [ ] Properties: Id (int), TagId, TagTypeId, TenantId

**Location:**
- [ ] Create `Location` entity
  - [ ] Properties: Id (Guid), FullName, Address, Postcode, Country, City, TenantId, Coordinates? (PostGIS point), Latitude?, Longitude?, Timezone?
  - [ ] Note: PostGIS handled in Persistence configuration

**Storage:**
- [ ] Create `StorageObject` entity
  - [ ] Properties: Id (Guid), FileTypeId, Uri, FullName, Extension, Size, TenantId, ActorId?

**Federation/Indexing:**
- [ ] Create `IndexedDid` entity
  - [ ] Properties: Did (string PK), Handle?, PdsHost, SigningKey?, IsActive, LastIndexedAt, LastSeenAt?
- [ ] Create `SyncState` entity
  - [ ] Properties: Id (int), Service (unique), Cursor, LastSeqTime?, UpdatedAt
- [ ] Create `AtProtoRecord` entity
  - [ ] Properties: Id (Guid), Did (varchar), Collection, RecordKey (varchar), Cid? (varchar), Uri?, IndexedAt?

#### 1.2 Lookup Tables / Enums (Priority 2)

**Convert to Enums where appropriate:**
- [ ] `EventTypeEnum`
- [ ] `EventStatusEnum`
- [ ] `EventFormatEnum`
- [ ] `VisibilityTypeEnum`
- [ ] `RegistrationModeEnum`
- [ ] `ApprovalStatusEnum`
- [ ] `OrganizationRoleEnum` ✅ (already exists)
- [ ] `OrganizationPositionEnum`
- [ ] `UserRoleEnum`
- [ ] `ActorTypeEnum`
- [ ] `DidCustodyTypeEnum`
- [ ] `FileTypeEnum`
- [ ] `MadhabEnum`
- [ ] `AudienceAgeEnum`
- [ ] `AudienceGenderEnum`
- [ ] `TagTypeEnum`

**OR keep as lookup entities (decision in Phase 0):**
- [ ] If keeping as entities, create: EventType, EventStatus, EventFormat, VisibilityType, etc.

**Language entity:**
- [ ] Create `Language` entity (likely entity, not enum due to extensibility)

---

### Phase 2: Application Layer Implementation

**Acceptance Criteria:**
- [ ] All CQRS commands/queries exist
- [ ] DTOs follow naming patterns
- [ ] Validators use FluentValidation
- [ ] AutoMapper profiles configured
- [ ] Repository interfaces defined
- [ ] Application layer compiles

**Estimated Effort:** 3-4 days

#### 2.1 Repository Interfaces

**Location:** `Explore.Application/Contracts/Persistence/`

```csharp
// Example pattern
public interface IEventRepository : IGenericRepository<Event, Guid>
{
    Task<EventDto> GetEventWithDetails(Guid id);
    Task<List<EventListDto>> GetEventsWithDetails(EventFilterDto filters);
    Task<List<EventListDto>> GetMyEvents(string userId);
}
```

**Create interfaces for:**
- [ ] `IEventRepository`
- [ ] `IEventSessionRepository`
- [ ] `IEventRegistrationRepository`
- [ ] `IOrganizationRepository` (update existing)
- [ ] `ICategoryRepository`
- [ ] `ITagRepository`
- [ ] `ILocationRepository`
- [ ] `IActorRepository`
- [ ] `IUserRepository`
- [ ] `ITenantRepository`
- [ ] `IStorageObjectRepository`
- [ ] `IAtProtoRecordRepository`
- [ ] `ISyncStateRepository`
- [ ] Lookup repositories: `IEventTypeRepository`, `IApprovalStatusRepository`, etc.

#### 2.2 DTOs

**Location:** `Explore.Application/DTOs/{Entity}/`

**For each aggregate, create:**
- `{Entity}Dto` - Detail view
- `{Entity}ListDto` - List view
- `Create{Entity}Dto` - Create input
- `Update{Entity}Dto` - Update input
- Specialized DTOs as needed

**Event DTOs:**
- [ ] `EventDto`
- [ ] `EventListDto`
- [ ] `CreateEventDto`
- [ ] `UpdateEventDto`
- [ ] `EventFilterDto` (query parameters)

**Event Session DTOs:**
- [ ] `EventSessionDto`
- [ ] `EventSessionListDto`
- [ ] `CreateEventSessionDto`
- [ ] `UpdateEventSessionDto`
- [ ] `EventSessionAgendaItemDto`
- [ ] `CreateAgendaItemDto`

**Registration DTOs:**
- [ ] `EventRegistrationDto`
- [ ] `RegisterForSessionDto`
- [ ] `ApproveRegistrationDto`

**Organization DTOs:** ✅ (mostly exist, verify completeness)
- [ ] Verify existing DTOs match DBML
- [ ] Add `OrganizationMemberDto` updates for new fields

**Category/Tag DTOs:**
- [ ] `CategoryDto` (with parent/children for tree)
- [ ] `CategoryListDto`
- [ ] `TagDto`
- [ ] `TagListDto`

**Location DTOs:**
- [ ] `LocationDto`
- [ ] `CreateLocationDto`
- [ ] `UpdateLocationDto`
- [ ] `LocationSearchDto`

**Actor DTOs:**
- [ ] `ActorDto`
- [ ] `CreateActorDto` (likely internal)

**User/Tenant DTOs:**
- [ ] `UserDto`
- [ ] `TenantDto`

#### 2.3 Validators

**Location:** `Explore.Application/DTOs/{Entity}/Validators/`

**Pattern:**
```csharp
public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    private readonly IEventTypeRepository _eventTypeRepo;

    public CreateEventDtoValidator(IEventTypeRepository eventTypeRepo)
    {
        _eventTypeRepo = eventTypeRepo;

        RuleFor(e => e.Title)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(e => e.EventTypeId)
            .MustAsync(async (id, cancellation) => await _eventTypeRepo.Exists(id))
            .WithMessage("Event type does not exist.");
    }
}
```

**Create validators for:**
- [ ] `CreateEventDtoValidator`
- [ ] `UpdateEventDtoValidator`
- [ ] `CreateEventSessionDtoValidator`
- [ ] `RegisterForSessionDtoValidator`
- [ ] Organization validators (update existing)
- [ ] Category/Tag validators
- [ ] Location validators

#### 2.4 CQRS Commands

**Location:** `Explore.Application/Features/{EntityPlural}/Requests/Commands/`

**Pattern:**
```csharp
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateEventDto EventDto { get; set; }
    public string UserId { get; set; }
    public Guid TenantId { get; set; }
}
```

**Create commands:**
- [ ] `CreateEventCommand`
- [ ] `UpdateEventCommand`
- [ ] `PublishEventCommand`
- [ ] `CancelEventCommand`
- [ ] `CreateEventSessionCommand`
- [ ] `UpdateEventSessionCommand`
- [ ] `AddAgendaItemCommand`
- [ ] `RegisterForSessionCommand`
- [ ] `ApproveRegistrationCommand`
- [ ] `RejectRegistrationCommand`
- [ ] Organization commands (update existing)
- [ ] `CreateLocationCommand`
- [ ] `UpdateLocationCommand`

#### 2.5 CQRS Queries

**Location:** `Explore.Application/Features/{EntityPlural}/Requests/Queries/`

**Pattern:**
```csharp
public class GetEventDetailsRequest : IRequest<EventDto>
{
    public Guid Id { get; set; }
}

public class GetEventListRequest : IRequest<List<EventListDto>>
{
    public EventFilterDto Filters { get; set; }
}
```

**Create queries:**
- [ ] `GetEventDetailsRequest`
- [ ] `GetEventListRequest`
- [ ] `SearchEventsRequest`
- [ ] `GetEventSessionDetailsRequest`
- [ ] `GetEventSessionsByEventRequest`
- [ ] `GetRegistrationsBySessionRequest`
- [ ] `GetMyRegistrationsRequest`
- [ ] Organization queries (update existing)
- [ ] `GetCategoryTreeRequest`
- [ ] `SearchCategoriesRequest`
- [ ] `GetTagsRequest`
- [ ] `SearchLocationsRequest`

#### 2.6 Command Handlers

**Location:** `Explore.Application/Features/{EntityPlural}/Handlers/Commands/`

**Pattern:**
```csharp
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public CreateEventCommandHandler(IEventRepository eventRepository, IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var eventEntity = _mapper.Map<Event>(request.EventDto);
        eventEntity.TenantId = request.TenantId;
        eventEntity.ActorId = request.ActorId;
        eventEntity.EventStatusId = (int)EventStatusEnum.Draft;

        var createdEvent = await _eventRepository.Create(eventEntity);

        response.Success = true;
        response.Id = createdEvent.Id;
        response.Message = "Event created successfully";

        return response;
    }
}
```

**Create handlers for all commands above.**

#### 2.7 Query Handlers

**Location:** `Explore.Application/Features/{EntityPlural}/Handlers/Queries/`

**Pattern:**
```csharp
public class GetEventDetailsRequestHandler : IRequestHandler<GetEventDetailsRequest, EventDto>
{
    private readonly IEventRepository _eventRepository;

    public GetEventDetailsRequestHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<EventDto> Handle(GetEventDetailsRequest request, CancellationToken cancellationToken)
    {
        // Repository returns DTO directly
        return await _eventRepository.GetEventWithDetails(request.Id);
    }
}
```

**Create handlers for all queries above.**

#### 2.8 AutoMapper Profiles

**Location:** `Explore.Application/Profiles/MappingProfile.cs`

```csharp
// Add to existing MappingProfile.cs
CreateMap<Event, EventDto>().ReverseMap();
CreateMap<Event, EventListDto>();
CreateMap<Event, CreateEventDto>().ReverseMap();
CreateMap<Event, UpdateEventDto>().ReverseMap();

CreateMap<EventSession, EventSessionDto>().ReverseMap();
CreateMap<EventSession, EventSessionListDto>();
CreateMap<EventSession, CreateEventSessionDto>().ReverseMap();

CreateMap<EventRegistration, EventRegistrationDto>().ReverseMap();
CreateMap<EventRegistration, RegisterForSessionDto>().ReverseMap();

CreateMap<Category, CategoryDto>().ReverseMap();
CreateMap<Tag, TagDto>().ReverseMap();
CreateMap<Location, LocationDto>().ReverseMap();
// ... etc for all entities
```

---

### Phase 3: Persistence Layer Implementation

**Acceptance Criteria:**
- [ ] DbContext includes all DbSets
- [ ] Entity configurations complete
- [ ] Migrations align with DBML
- [ ] Repositories implemented
- [ ] Tenant scoping configured
- [ ] Persistence layer compiles

**Estimated Effort:** 3-4 days

#### 3.1 DbContext Updates

**Location:** `Explore.Persistence/ExploreDbContext.cs`

```csharp
public class ExploreDbContext : DbContext
{
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantUser> TenantUsers { get; set; }
    public DbSet<TenantSettings> TenantSettings { get; set; }

    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserAuthenticationToken> UserAuthenticationTokens { get; set; }
    public DbSet<UserExternalLogin> UserExternalLogins { get; set; }

    public DbSet<Actor> Actors { get; set; }
    public DbSet<ActorType> ActorTypes { get; set; }
    public DbSet<ActorKeyStore> ActorKeyStores { get; set; }
    public DbSet<DidCustodyType> DidCustodyTypes { get; set; }

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    public DbSet<OrganizationRole> OrganizationRoles { get; set; }
    public DbSet<OrganizationPosition> OrganizationPositions { get; set; }

    public DbSet<Event> Events { get; set; }
    public DbSet<EventSession> EventSessions { get; set; }
    public DbSet<EventSessionAgendaItem> EventSessionAgendaItems { get; set; }
    public DbSet<EventSessionLanguage> EventSessionLanguages { get; set; }
    public DbSet<EventSessionSpeaker> EventSessionSpeakers { get; set; }
    public DbSet<EventCategory> EventCategories { get; set; }
    public DbSet<EventTag> EventTags { get; set; }
    public DbSet<EventRegistration> EventRegistrations { get; set; }

    public DbSet<EventType> EventTypes { get; set; }
    public DbSet<EventStatus> EventStatuses { get; set; }
    public DbSet<EventFormat> EventFormats { get; set; }
    public DbSet<VisibilityType> VisibilityTypes { get; set; }
    public DbSet<RegistrationMode> RegistrationModes { get; set; }
    public DbSet<ApprovalStatus> ApprovalStatuses { get; set; }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<TagType> TagTypes { get; set; }
    public DbSet<TagTypeTag> TagTypeTags { get; set; }

    public DbSet<Madhab> Madhabs { get; set; }
    public DbSet<AudienceAge> AudienceAges { get; set; }
    public DbSet<AudienceGender> AudienceGenders { get; set; }
    public DbSet<Language> Languages { get; set; }

    public DbSet<Location> Locations { get; set; }
    public DbSet<StorageObject> StorageObjects { get; set; }
    public DbSet<FileType> FileTypes { get; set; }

    public DbSet<IndexedDid> IndexedDids { get; set; }
    public DbSet<SyncState> SyncStates { get; set; }
    public DbSet<AtProtoRecord> AtProtoRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExploreDbContext).Assembly);

        // Global query filters for multi-tenancy
        modelBuilder.Entity<Event>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Organization>().HasQueryFilter(o => o.TenantId == CurrentTenantId);
        // ... apply to all tenant-scoped entities

        base.OnModelCreating(modelBuilder);
    }

    private Guid CurrentTenantId => /* Resolve from scoped service */;
}
```

#### 3.2 Entity Configurations

**Location:** `Explore.Persistence/Configurations/Entities/`

**For each entity, create configuration following this pattern:**

**Example: EventConfiguration.cs**
```csharp
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        // Table name
        builder.ToTable("event");

        // Primary key with UUIDv7
        builder.Property(e => e.Id)
            .HasDefaultValueSql("uuidv7()");

        // Required fields with max lengths
        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        // Foreign keys
        builder.HasOne(e => e.Actor)
            .WithMany()
            .HasForeignKey(e => e.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.FeaturedImageStorage)
            .WithMany()
            .HasForeignKey(e => e.FeaturedImage)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationships
        builder.HasMany(e => e.EventSessions)
            .WithOne(s => s.Event)
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.Slug);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();

        // Default values
        builder.Property(e => e.TotalViews)
            .HasDefaultValue(0);

        builder.Property(e => e.EventStatusId)
            .HasDefaultValue((int)EventStatusEnum.Draft);
    }
}
```

**Create configurations for ALL entities:**
- [ ] TenantConfiguration
- [ ] UserConfiguration
- [ ] ActorConfiguration
- [ ] OrganizationConfiguration (update existing)
- [ ] EventConfiguration
- [ ] EventSessionConfiguration
- [ ] EventSessionAgendaItemConfiguration
- [ ] EventRegistrationConfiguration
- [ ] CategoryConfiguration (self-referential parent)
- [ ] TagConfiguration
- [ ] LocationConfiguration (PostGIS point type)
- [ ] StorageObjectConfiguration
- [ ] Junction entity configurations
- [ ] Lookup table configurations

**Special configurations:**

**LocationConfiguration with PostGIS:**
```csharp
public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.Property(l => l.Coordinates)
            .HasColumnType("geometry(Point, 4326)");

        builder.HasIndex(l => l.Coordinates)
            .HasMethod("GIST");

        // ... other config
    }
}
```

**CategoryConfiguration (hierarchical):**
```csharp
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TenantId, c.MasterCode }).IsUnique();
    }
}
```

#### 3.3 Repository Implementations

**Location:** `Explore.Persistence/Repositories/`

**Pattern:**
```csharp
public class EventRepository : GenericRepository<Event, Guid>, IEventRepository
{
    public EventRepository(ExploreDbContext context) : base(context) { }

    public async Task<EventDto> GetEventWithDetails(Guid id)
    {
        return await _context.Events
            .Include(e => e.Actor)
            .Include(e => e.EventSessions)
            .Include(e => e.EventCategories)
                .ThenInclude(ec => ec.Category)
            .Include(e => e.EventTags)
                .ThenInclude(et => et.Tag)
            .Include(e => e.EventStatus)
            .Include(e => e.EventType)
            .Where(e => e.Id == id)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                ActorName = e.Actor.DisplayName,
                EventStatusName = e.EventStatus.FullName,
                Categories = e.EventCategories.Select(ec => new CategoryDto
                {
                    Id = ec.Category.Id,
                    FullName = ec.Category.FullName
                }).ToList(),
                // ... map all properties
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<EventListDto>> GetEventsWithDetails(EventFilterDto filters)
    {
        var query = _context.Events
            .Include(e => e.Actor)
            .Include(e => e.EventStatus)
            .AsQueryable();

        // Apply filters
        if (filters.VisibilityTypeId.HasValue)
            query = query.Where(e => e.VisibilityTypeId == filters.VisibilityTypeId);

        if (filters.EventStatusId.HasValue)
            query = query.Where(e => e.EventStatusId == filters.EventStatusId);

        if (filters.AudienceGenderId.HasValue)
            query = query.Where(e => e.AudienceGenderId == filters.AudienceGenderId);

        if (filters.MadhabId.HasValue)
            query = query.Where(e => e.MadhabId == filters.MadhabId);

        if (filters.CategoryIds != null && filters.CategoryIds.Any())
            query = query.Where(e => e.EventCategories.Any(ec => filters.CategoryIds.Contains(ec.CategoryId)));

        if (filters.TagIds != null && filters.TagIds.Any())
            query = query.Where(e => e.EventTags.Any(et => filters.TagIds.Contains(et.TagId)));

        if (filters.StartDate.HasValue)
            query = query.Where(e => e.FirstSessionDate >= filters.StartDate);

        if (filters.EndDate.HasValue)
            query = query.Where(e => e.LastSessionDate <= filters.EndDate);

        return await query
            .Select(e => new EventListDto
            {
                Id = e.Id,
                Title = e.Title,
                FirstSessionDate = e.FirstSessionDate,
                EventStatusName = e.EventStatus.FullName,
                // ... map list properties
            })
            .ToListAsync();
    }
}
```

**Implement repositories for:**
- [ ] EventRepository
- [ ] EventSessionRepository
- [ ] EventRegistrationRepository
- [ ] OrganizationRepository (update existing)
- [ ] CategoryRepository (tree queries)
- [ ] TagRepository
- [ ] LocationRepository (geo queries)
- [ ] ActorRepository
- [ ] UserRepository
- [ ] TenantRepository
- [ ] StorageObjectRepository
- [ ] AtProtoRecordRepository
- [ ] SyncStateRepository

#### 3.4 Migrations

**Generate migration:**
```bash
dotnet ef migrations add DbmlSync --project Explore.Persistence --startup-project Explore.API
```

**Review migration:**
- [ ] Verify all tables created
- [ ] Verify constraints (FK, unique)
- [ ] Verify indexes
- [ ] Verify default values
- [ ] Verify data types match DBML

**If existing database:**
- [ ] Create baseline strategy
- [ ] Test migration on dev database
- [ ] Document any manual steps needed

---

### Phase 4: API Layer Implementation

**Acceptance Criteria:**
- [ ] All controllers follow `/api/v1/[controller]` pattern
- [ ] User ID extraction centralized
- [ ] All endpoints call MediatR
- [ ] OpenAPI documentation complete
- [ ] Authorization applied correctly
- [ ] API compiles and runs

**Estimated Effort:** 2-3 days

#### 4.1 User ID Extraction Utility

**Location:** `Explore.API/Extensions/ClaimsPrincipalExtensions.cs`

```csharp
public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sid")?.Value;
    }

    public static Guid? GetUserIdAsGuid(this ClaimsPrincipal principal)
    {
        var userId = principal.GetUserId();
        return Guid.TryParse(userId, out var guid) ? guid : null;
    }
}
```

#### 4.2 Controllers

**Update Routing Convention:**
```csharp
[Route("api/v1/[controller]")]
[ApiController]
public class {Entity}Controller : ControllerBase
```

**EventController:**
```csharp
[Route("api/v1/[controller]")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [EndpointSummary("Get all events")]
    [AllowAnonymous]
    public async Task<ActionResult<List<EventListDto>>> GetAll([FromQuery] EventFilterDto filters)
    {
        var request = new GetEventListRequest { Filters = filters };
        var events = await _mediator.Send(request);
        return Ok(events);
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get event details")]
    [AllowAnonymous]
    public async Task<ActionResult<EventDto>> GetById(Guid id)
    {
        var request = new GetEventDetailsRequest { Id = id };
        var eventDto = await _mediator.Send(request);

        if (eventDto == null)
            return NotFound();

        return Ok(eventDto);
    }

    [HttpPost]
    [EndpointSummary("Create event")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto dto)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token");

        var command = new CreateEventCommand
        {
            EventDto = dto,
            UserId = userId,
            TenantId = /* resolve tenant */
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
            return BadRequest(response);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id}")]
    [EndpointSummary("Update event")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventDto dto)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new UpdateEventCommand
        {
            Id = id,
            EventDto = dto,
            UserId = userId
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }
}
```

**Create/Update Controllers:**
- [ ] EventController
- [ ] EventSessionController
- [ ] EventRegistrationController
- [ ] OrganizationController (update existing - fix routing)
- [ ] CategoryController
- [ ] TagController
- [ ] LocationController

#### 4.3 Middleware Updates

**Tenant Resolution Middleware:**
```csharp
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        // Resolve tenant from subdomain, header, or claim
        var tenantId = ResolveTenantId(context);
        tenantService.SetCurrentTenant(tenantId);

        await _next(context);
    }

    private Guid ResolveTenantId(HttpContext context)
    {
        // Implementation: subdomain, header, JWT claim
    }
}
```

---

### Phase 5: Verification, Cleanup, Documentation

**Acceptance Criteria:**
- [ ] All obsolete code removed
- [ ] Tests updated/added
- [ ] Documentation updated
- [ ] No DBML mismatches remain

**Estimated Effort:** 1-2 days

#### 5.1 Cleanup Tasks

- [ ] Remove obsolete entities not in DBML
- [ ] Remove dead code in handlers
- [ ] Standardize all API routes to `/api/v1/`
- [ ] Remove commented-out validation code
- [ ] Fix typos in endpoint summaries
- [ ] Remove temporary `[AllowAnonymous]` attributes

#### 5.2 Testing

- [ ] Unit tests for validators
- [ ] Unit tests for handlers
- [ ] Integration tests for repositories
- [ ] API endpoint tests
- [ ] Multi-tenancy isolation tests

#### 5.3 Documentation

- [ ] Update `dbml-sync-context.md` with final decisions
- [ ] Update `dbml-sync-tasks.md` with completion status
- [ ] Document any DBML deviations
- [ ] Update API documentation
- [ ] Update `CLAUDE.md` if patterns changed

---

## Part 5: Critical Issues to Address

### Issue 1: DBML Type Corrections Needed

**atproto_record table:**
```dbml
Table "atproto_record" {
  "id" uuid [pk, not null]
  "did" varchar(255) [not null]           // ✅ CHANGE from uuid
  "collection" varchar(500) [not null]
  "record_key" varchar(500) [not null]    // ✅ CHANGE from uuid
  "cid" varchar(255)                      // ✅ CHANGE from uuid
  "uri" varchar(500)
  "indexed_at" timestamp
}
```

### Issue 2: Missing OrganizationReview in DBML

**Decision Required:** Add to DBML or remove from codebase?

If keeping, add to DBML:
```dbml
Table "organization_review" {
  "id" uuid [pk, not null]
  "organization_id" uuid [not null, ref: < "organization"."id"]
  "event_id" uuid [not null, ref: < "event"."id"]
  "user_id" uuid [not null, ref: < "user"."id"]
  "reviewer_name" varchar(500) [not null]
  "rating" integer [not null]
  "comment" varchar(500)
  "created_at" timestamptz [not null]
  "updated_at" timestamptz [not null]
  "tenant_id" uuid [not null]
}
```

### Issue 3: Timestamp vs Timestamptz Consistency

**DBML uses:**
- `timestamptz` for event_session start/end times ✅
- `timestamp` for event_session_agenda_items ❌

**Recommendation:** Change to `timestamptz` for consistency.

### Issue 4: API Routing Standardization

**Current inconsistency:**
- Some controllers: `/api/organization`
- Some controllers: `/api/v1/event`

**Required:** Standardize all to `/api/v1/[controller]`

---

## Part 6: Timeline Summary

| Phase | Tasks | Estimated Effort |
|-------|-------|------------------|
| Phase 0 | Discovery & Decisions | 0.5-1 day |
| Phase 1 | Domain Layer | 2-3 days |
| Phase 2 | Application Layer | 3-4 days |
| Phase 3 | Persistence Layer | 3-4 days |
| Phase 4 | API Layer | 2-3 days |
| Phase 5 | Verification & Cleanup | 1-2 days |
| **Total** | **Full Implementation** | **12-17 days** |

---

## Part 7: Next Steps

### Immediate Actions Required:

1. **Review This Analysis**
   - Verify findings against actual codebase
   - Confirm design decisions
   - Approve/reject recommendations

2. **Update DBML**
   - Fix atproto_record types (uuid → varchar)
   - Add OrganizationReview (or mark for removal)
   - Fix timestamp → timestamptz for agenda items
   - Add missing tenant_id columns

3. **Begin Phase 0**
   - Finalize all design decisions
   - Create detailed entity mapping
   - Document any deviations from DBML

4. **Execute Phases 1-5**
   - Follow implementation plan
   - Update context.md with progress
   - Mark tasks complete in tasks.md

---

## Conclusion

This analysis reveals that the **original DBML sync plan was conceptually sound** but lacked the **specific implementation patterns** actually used in the ISLAMU Event codebase. The refactored plan now includes:

✅ **Concrete file paths and naming conventions**
✅ **Actual DTO patterns (CreateXDto, XListDto)**
✅ **BaseCommandResponse pattern**
✅ **Repository return types (DTOs for queries)**
✅ **User ID extraction centralization**
✅ **API routing standardization**
✅ **Critical DBML fixes needed**

The plan is now **actionable and aligned with the real codebase**, ready for systematic implementation once design decisions are finalized.
