# MODULAR EXTENSIBILITY & GOVERNANCE REFACTOR - Task Checklist

> **Progress Tracking for Implementation**
>
> Check off tasks as they are completed. Update status indicators per phase.
>
> **Created**: 2026-01-30
> **Last Updated**: 2026-01-30

---

## AUTHORITATIVE SOURCE

**Primary Plan**: [refactor-api-high-flexibility-implementation.md](../refactor-api-high-flexibility-implementation.md)

This task checklist implements the blocks defined in the authoritative source:
- **BLOCK 1** → Phase 2 (Cascading Settings Engine)
- **BLOCK 2** → Phase 3 (Relational Aspect Architecture)
- **BLOCK 3** → Phase 5 (Request-Scoped Strategy Resolver)
- **BLOCK 4** → Phase 4 (Module Governance)
- **BLOCK 5** → Phase 7 (Virtual Tenant Masking)
- **BLOCK A** → Phase 1 (UUID v7 Infrastructure)
- **BLOCK B** → Phase 8 (HATEOAS & API Updates - Aspect Filtering)
- **BLOCK C** → Phase 6 (PDS Hosting & Synchronization)

---

## Status Legend

- ✅ COMPLETE - All tasks finished and tested
- 🟡 IN PROGRESS - Currently being worked on
- ⏳ NOT STARTED - Waiting for dependencies
- ❌ BLOCKED - Cannot proceed due to blocker

---

## Phase 0: Pre-Implementation ✅ COMPLETE

- [x] Read all documentation (ARCHITECTURE, DOMAIN, GOVERNANCE, MULTI_TENANCY, EXTENSIBILITY, MODULAR_EVENTS, API, FEDERATION)
- [x] Research UUID v7 patterns for .NET 10 / EF Core 10 / PostgreSQL
- [x] Research EF Core 10 named query filters
- [x] Research Outbox pattern for reliable messaging
- [x] Create implementation plan (plan.md)
- [x] Create context file (context.md)
- [x] Create tasks checklist (tasks.md)
- [ ] Get plan approval from stakeholder

---

## Phase 1: UUID v7 Infrastructure ⏳ NOT STARTED

**Goal**: Replace existing UUID v4 generation with UUID v7 for all primary keys

### 1.1 Create UUID v7 Value Generator
- [ ] Create folder `Explore.Infrastructure/ValueGenerators/`
- [ ] Create `UuidV7ValueGenerator.cs` implementing `ValueGenerator<Guid>`
- [ ] Use `Guid.CreateVersion7()` for generation
- [ ] Write unit tests verifying v7 format and monotonicity
- [ ] **Acceptance**: Generator produces valid, time-sortable UUID v7s

### 1.2 Update Entity Configurations
- [ ] Update `EventConfiguration.cs` to use `HasValueGenerator<UuidV7ValueGenerator>()`
- [ ] Update `ActorConfiguration.cs`
- [ ] Update `OrganizationConfiguration.cs`
- [ ] Update `LocationConfiguration.cs`
- [ ] Update `EventSessionConfiguration.cs`
- [ ] Update `CategoryConfiguration.cs`
- [ ] Update `TagConfiguration.cs`
- [ ] Update all other Guid PK entity configurations
- [ ] Remove any `HasDefaultValueSql("gen_random_uuid()")` if present
- [ ] **Acceptance**: All entities configured for UUID v7

### 1.3 Create Migration
- [ ] Run `dotnet ef migrations add UseUuidV7`
- [ ] Review generated migration (should be minimal - no data changes)
- [ ] Apply migration to development database
- [ ] Verify existing data unaffected
- [ ] **Acceptance**: Migration applies cleanly

### 1.4 Add DID Unique Index
- [ ] Add `HasIndex(a => a.Did).IsUnique()` to ActorConfiguration
- [ ] Create migration for index
- [ ] Apply migration
- [ ] **Acceptance**: Unique constraint on Did column

### 1.5 Run Tests
- [ ] Run all existing unit tests
- [ ] Run all existing integration tests
- [ ] Verify no regressions
- [ ] **Acceptance**: All tests pass

---

## Phase 2: Cascading Settings Engine ⏳ NOT STARTED

**Goal**: Implement three-tier configuration hierarchy with locking capabilities

### 2.1 Create Domain Entities
- [ ] Create folder `Explore.Domain/Settings/`
- [ ] Create `SystemSetting.cs`:
  - [ ] Properties: Id (Guid), Key (string), Value (string), IsLocked (bool), AllowedValues (string?), Description (string?)
  - [ ] Auditing fields: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
- [ ] Create `TenantSetting.cs`:
  - [ ] Properties: Id (Guid), TenantId (Guid), Key (string), Value (string)
  - [ ] Auditing fields
  - [ ] Implement `ITenantEntity`
- [ ] **Acceptance**: Entities compile, follow domain conventions

### 2.2 Create Entity Configurations
- [ ] Create folder `Explore.Persistence/Configurations/Settings/`
- [ ] Create `SystemSettingConfiguration.cs`:
  - [ ] Unique index on Key
  - [ ] Named query filter for SoftDelete
  - [ ] UUID v7 value generator for Id
- [ ] Create `TenantSettingConfiguration.cs`:
  - [ ] Composite unique index on (TenantId, Key)
  - [ ] Named query filters for SoftDelete and TenantFilter
  - [ ] UUID v7 value generator for Id
- [ ] Add DbSets to `ExploreDbContext`
- [ ] **Acceptance**: Configurations generate correct schema

### 2.3 Create Migration
- [ ] Run `dotnet ef migrations add AddCascadingSettings`
- [ ] Review and apply migration
- [ ] **Acceptance**: Tables created correctly

### 2.4 Create Repository Interfaces
- [ ] Create `ISystemSettingRepository.cs` in Application/Contracts/Persistence:
  - [ ] `GetByKeyAsync(string key)`
  - [ ] `GetAllAsync()`
  - [ ] `UpsertAsync(SystemSetting setting)`
- [ ] Create `ITenantSettingRepository.cs`:
  - [ ] `GetByKeyAsync(Guid tenantId, string key)`
  - [ ] `GetAllForTenantAsync(Guid tenantId)`
  - [ ] `UpsertAsync(TenantSetting setting)`
- [ ] **Acceptance**: Interfaces follow existing patterns

### 2.5 Implement Repositories
- [ ] Create `SystemSettingRepository.cs` in Persistence/Repositories
- [ ] Create `TenantSettingRepository.cs`
- [ ] Register in DI container
- [ ] **Acceptance**: Repositories functional

### 2.6 Implement Settings Resolver Service
- [ ] Create `ISettingsResolver.cs` interface in Application/Contracts/Services:
  - [ ] `GetSettingAsync<T>(string key, Guid? tenantId = null)`
  - [ ] `GetSettingValueAsync(string key, Guid? tenantId = null)` (returns string)
- [ ] Create `SettingsResolver.cs` implementation:
  - [ ] Resolution logic: Locked → Tenant Override → System Default
  - [ ] Type conversion support (string → T)
  - [ ] Caching consideration (optional)
- [ ] Register in DI container
- [ ] Write unit tests for cascade logic
- [ ] **Acceptance**: Resolver correctly applies cascade

### 2.7 Create CQRS Commands
- [ ] Create `UpsertSystemSettingCommand.cs` + Handler
- [ ] Create `UpsertSystemSettingDtoValidator.cs`
- [ ] Create `UpsertTenantSettingCommand.cs` + Handler
- [ ] Create `UpsertTenantSettingDtoValidator.cs`
- [ ] Create `LockSystemSettingCommand.cs` + Handler
- [ ] **Acceptance**: Commands with validation work

### 2.8 Create CQRS Queries
- [ ] Create `GetSettingQuery.cs` + Handler
- [ ] Create `GetAllSystemSettingsQuery.cs` + Handler
- [ ] Create `GetTenantSettingsQuery.cs` + Handler
- [ ] **Acceptance**: Queries return correct data

### 2.9 Create Settings Controller
- [ ] Create `SettingsController.cs` in API/Controllers:
  - [ ] `GET /api/v1/settings/{key}` [AllowAnonymous]
  - [ ] `GET /api/v1/settings/system` [Authorize(Roles = "Admin")]
  - [ ] `PUT /api/v1/settings/system/{key}` [Authorize(Roles = "Admin")]
  - [ ] `PUT /api/v1/settings/tenant/{key}` [Authorize]
  - [ ] `POST /api/v1/settings/system/{key}/lock` [Authorize(Roles = "Admin")]
- [ ] Add OpenAPI documentation (EndpointSummary, ProducesResponseType)
- [ ] **Acceptance**: All endpoints functional with authorization

### 2.10 Seed Default Settings
- [ ] Create `SettingsSeeder.cs` or add to existing seeder
- [ ] Define default system settings (e.g., "MaxEventsPerTenant", "AllowPublicRegistration")
- [ ] **Acceptance**: Default settings seeded

### 2.11 Run Tests
- [ ] Write integration tests for settings endpoints
- [ ] Write unit tests for cascade logic
- [ ] Run all tests
- [ ] **Acceptance**: All tests pass

---

## Phase 3: Relational Aspect Architecture ⏳ NOT STARTED

**Goal**: Strip domain-specific fields from Event and move to optional 1:1 Aspect tables

### 3.1 Refactor Event Core Entity
- [ ] Open `Explore.Domain/Event.cs`
- [ ] Remove `MadhabId` property (if exists)
- [ ] Remove any other Islamic-specific properties
- [ ] Add `string? MetadataJson` for dynamic fields
- [ ] Add navigation properties:
  ```csharp
  public virtual EventIslamicAspect? IslamicAspect { get; set; }
  public virtual EventTechAspect? TechAspect { get; set; }
  ```
- [ ] **Acceptance**: Event contains only universal properties

### 3.2 Create Aspect Entities
- [ ] Create folder `Explore.Domain/Aspects/`
- [ ] Create `EventIslamicAspect.cs`:
  - [ ] PK: `Guid EventId` (shared key)
  - [ ] Properties: MadhabId (int?), PrayerTimeOffset (int?), GenderMode (enum), LanguageMode (string?)
  - [ ] Navigation: `Event Event`
  - [ ] Auditing fields
- [ ] Create `EventTechAspect.cs`:
  - [ ] PK: `Guid EventId` (shared key)
  - [ ] Properties: GithubRepoUrl (string?), TechStack (string? JSON), HackathonTrack (string?), SkillLevel (enum)
  - [ ] Navigation: `Event Event`
  - [ ] Auditing fields
- [ ] Create enums: `GenderMode`, `SkillLevel` if not existing
- [ ] **Acceptance**: Aspect entities use shared PK pattern

### 3.3 Create Aspect Entity Configurations
- [ ] Create folder `Explore.Persistence/Configurations/Aspects/`
- [ ] Create `EventIslamicAspectConfiguration.cs`:
  - [ ] Table name: `event_islamic_aspects`
  - [ ] Shared PK configuration
  - [ ] 1:1 relationship with Event
  - [ ] Named query filters
- [ ] Create `EventTechAspectConfiguration.cs`:
  - [ ] Table name: `event_tech_aspects`
  - [ ] Shared PK configuration
  - [ ] 1:1 relationship with Event
  - [ ] Named query filters
- [ ] Add DbSets to ExploreDbContext
- [ ] **Acceptance**: 1:1 relationships configured correctly

### 3.4 Create EF Core Migration
- [ ] Run `dotnet ef migrations add AddEventAspects`
- [ ] Review migration:
  - [ ] New aspect tables created
  - [ ] FK constraints correct
  - [ ] MadhabId removed from Events (if was there)
- [ ] Write data migration script if needed (move existing data)
- [ ] Apply migration
- [ ] **Acceptance**: Migration applies, data preserved

### 3.5 Create Aspect DTOs
- [ ] Create folder `Explore.Application/DTOs/Aspects/`
- [ ] Create `EventIslamicAspectDto.cs`
- [ ] Create `CreateEventIslamicAspectDto.cs`
- [ ] Create `UpdateEventIslamicAspectDto.cs`
- [ ] Create `EventTechAspectDto.cs`
- [ ] Create `CreateEventTechAspectDto.cs`
- [ ] Create `UpdateEventTechAspectDto.cs`
- [ ] **Acceptance**: DTOs follow existing patterns

### 3.6 Update Event DTOs
- [ ] Update `EventDto.cs`:
  - [ ] Add `List<string> AvailableAspects`
  - [ ] Add `EventIslamicAspectDto? IslamicAspect`
  - [ ] Add `EventTechAspectDto? TechAspect`
- [ ] Update `EventListDto.cs` (add AvailableAspects for list view)
- [ ] Update `CreateEventDto.cs`:
  - [ ] Add `CreateEventIslamicAspectDto? IslamicAspect`
  - [ ] Add `CreateEventTechAspectDto? TechAspect`
- [ ] Update `UpdateEventDto.cs` similarly
- [ ] **Acceptance**: Polymorphic response structure

### 3.7 Update Event Repository
- [ ] Add to `IEventRepository`:
  - [ ] `GetEventWithAspectsAsync(Guid id)`
- [ ] Implement in `EventRepository`:
  - [ ] Use `.Include(e => e.IslamicAspect).Include(e => e.TechAspect)`
- [ ] **Acceptance**: Eager loading works

### 3.8 Create Aspect-Specific Validators
- [ ] Create folder `Explore.Application/DTOs/Aspects/Validators/`
- [ ] Create `EventIslamicAspectValidator.cs`:
  - [ ] Validate MadhabId exists (if provided)
  - [ ] Validate GenderMode is valid enum
- [ ] Create `EventTechAspectValidator.cs`:
  - [ ] Validate GithubRepoUrl format
  - [ ] Validate SkillLevel enum
- [ ] **Acceptance**: Validation errors for invalid aspects

### 3.9 Update AutoMapper Profiles
- [ ] Update `EventMappingProfile.cs`:
  - [ ] Map EventIslamicAspect ↔ EventIslamicAspectDto
  - [ ] Map EventTechAspect ↔ EventTechAspectDto
  - [ ] Configure AvailableAspects calculation
- [ ] **Acceptance**: All mappings work

### 3.10 Update Event Handlers
- [ ] Update `GetEventDetailsRequestHandler`:
  - [ ] Use `GetEventWithAspectsAsync`
  - [ ] Build `AvailableAspects` list from non-null aspects
  - [ ] Include aspect DTOs in response
- [ ] Update `CreateEventCommandHandler`:
  - [ ] Accept optional aspect DTOs
  - [ ] Create aspect records in same transaction
  - [ ] Validate aspects using manual validator instantiation
- [ ] Update `UpdateEventCommandHandler`:
  - [ ] Handle aspect updates (create/update/delete)
- [ ] Update `DeleteEventCommandHandler`:
  - [ ] Cascade delete handles aspects (via FK)
- [ ] **Acceptance**: Full CRUD for events with aspects

### 3.11 Run Tests
- [ ] Write unit tests for aspect handlers
- [ ] Write integration tests for aspect CRUD
- [ ] Run all existing tests (check for regressions)
- [ ] **Acceptance**: All tests pass

---

## Phase 4: Module Governance & Discovery ⏳ NOT STARTED

**Goal**: Control module visibility per tenant with dynamic API discovery

### 4.1 Create Module Definition Entities
- [ ] Create folder `Explore.Domain/Modules/`
- [ ] Create `ModuleDefinition.cs`:
  - [ ] Key (string, PK), Name, Description
  - [ ] WizardSchemaUrl (string?)
  - [ ] IsEnabled (bool) - instance level
  - [ ] Auditing fields
- [ ] Create `TenantCapability.cs`:
  - [ ] Id (Guid), TenantId, ModuleKey
  - [ ] IsEnabled (bool) - tenant level
  - [ ] Implement `ITenantEntity`
  - [ ] Auditing fields
- [ ] **Acceptance**: Module entities created

### 4.2 Create Module Entity Configurations
- [ ] Create folder `Explore.Persistence/Configurations/Modules/`
- [ ] Create `ModuleDefinitionConfiguration.cs`
- [ ] Create `TenantCapabilityConfiguration.cs`:
  - [ ] Composite unique index on (TenantId, ModuleKey)
- [ ] Add DbSets to ExploreDbContext
- [ ] Create migration
- [ ] **Acceptance**: Tables created

### 4.3 Create Module Repository and Service
- [ ] Create `IModuleRepository.cs`:
  - [ ] `GetAllModulesAsync()`
  - [ ] `GetModuleAsync(string key)`
- [ ] Create `ITenantCapabilityRepository.cs`:
  - [ ] `GetCapabilitiesForTenantAsync(Guid tenantId)`
  - [ ] `IsModuleEnabledAsync(Guid tenantId, string moduleKey)`
- [ ] Create `IModuleService.cs`:
  - [ ] `GetAvailableModulesAsync(Guid tenantId)`
  - [ ] `IsModuleEnabledAsync(Guid tenantId, string moduleKey)`
- [ ] Implement services
- [ ] **Acceptance**: Module availability respects 3-tier hierarchy

### 4.4 Create Module Discovery Endpoint
- [ ] Create `ModulesController.cs`:
  - [ ] `GET /api/v1/modules/available` [AllowAnonymous]
- [ ] Response includes module key, name, description, wizard schema URL
- [ ] Add OpenAPI documentation
- [ ] **Acceptance**: Endpoint returns correct modules per tenant

### 4.5 Integrate Module Check with Event Creation
- [ ] Update `CreateEventCommandHandler`:
  - [ ] Check if provided aspects match enabled modules
  - [ ] Reject Islamic aspect if "Mod_Islamic" not enabled
  - [ ] Return validation error with helpful message
- [ ] **Acceptance**: Module governance enforced

### 4.6 Seed Default Modules
- [ ] Create `ModuleSeeder.cs`:
  - [ ] Mod_Islamic: Islamic event features
  - [ ] Mod_Tech: Tech event features
  - [ ] Mod_Educational: Educational features (disabled by default)
- [ ] Seed default tenant capabilities for default tenant
- [ ] **Acceptance**: Modules seeded

### 4.7 Run Tests
- [ ] Write tests for module service
- [ ] Write integration tests for module endpoint
- [ ] Write tests for module enforcement
- [ ] **Acceptance**: All tests pass

---

## Phase 5: Request-Scoped Strategy Resolver ⏳ NOT STARTED

**Goal**: Enable modular business logic that adapts at runtime

### 5.1 Define Strategy Interfaces
- [ ] Create folder `Explore.Application/Contracts/Strategies/`
- [ ] Create `IEventStrategy.cs` (base interface)
- [ ] Create `ISchedulingStrategy.cs`:
  - [ ] `CalculateEventTimeAsync(EventScheduleRequest request)`
- [ ] Create `IValidationStrategy.cs`:
  - [ ] `ValidateAsync(object dto, string moduleKey)`
- [ ] **Acceptance**: Strategy interfaces defined

### 5.2 Implement Islamic Scheduling Strategy
- [ ] Create folder `Explore.Infrastructure/Strategies/`
- [ ] Create `IslamicSchedulingStrategy.cs`:
  - [ ] Inject prayer time service/library
  - [ ] Calculate actual times from prayer offsets
  - [ ] Handle "30 minutes after Maghrib" → actual DateTime
- [ ] Create `DefaultSchedulingStrategy.cs`:
  - [ ] Pass through absolute times unchanged
- [ ] **Acceptance**: Prayer-based scheduling works

### 5.3 Create Strategy Resolver
- [ ] Create `IStrategyResolver.cs` in Application/Contracts/Services
- [ ] Create `StrategyResolver.cs`:
  - [ ] `GetSchedulingStrategy(Guid tenantId)` → returns appropriate strategy
  - [ ] Uses IModuleService to check enabled modules
- [ ] Register strategies in DI
- [ ] **Acceptance**: Correct strategy resolved per tenant

### 5.4 Integrate with MediatR Handlers
- [ ] Inject `IStrategyResolver` into event handlers
- [ ] Apply scheduling strategy during event creation
- [ ] Apply validation strategy as needed
- [ ] **Acceptance**: Strategies applied transparently

### 5.5 Run Tests
- [ ] Write unit tests for strategies
- [ ] Write tests for strategy resolver
- [ ] **Acceptance**: All tests pass

---

## Phase 6: PDS Hosting & Synchronization ⏳ NOT STARTED

**Goal**: Implement Outbox pattern for reliable AT Protocol record synchronization

### 6.1 Create Outbox Entity
- [ ] Create folder `Explore.Domain/Federation/`
- [ ] Create `PdsSyncOutbox.cs`:
  - [ ] Id (Guid), ActorDid (string), RecordType (string)
  - [ ] RecordUri (string?), Payload (string JSON)
  - [ ] OccurredAt (DateTime), ProcessedAt (DateTime?)
  - [ ] Error (string?), RetryCount (int)
- [ ] **Acceptance**: Outbox entity created

### 6.2 Create Outbox Entity Configuration
- [ ] Create `PdsSyncOutboxConfiguration.cs`:
  - [ ] Index on ProcessedAt (NULL filter)
  - [ ] Index on RetryCount
  - [ ] UUID v7 for Id
- [ ] Add DbSet to ExploreDbContext
- [ ] Create migration
- [ ] **Acceptance**: Efficient querying of unprocessed entries

### 6.3 Create Domain Event Interface
- [ ] Create `IHasDomainEvents.cs` interface:
  - [ ] `IReadOnlyList<IDomainEvent> DomainEvents`
  - [ ] `void ClearDomainEvents()`
- [ ] Create `IDomainEvent.cs` marker interface
- [ ] Update relevant entities to implement `IHasDomainEvents`
- [ ] **Acceptance**: Domain event pattern implemented

### 6.4 Create Domain Events
- [ ] Create folder `Explore.Domain/Events/` (domain events, not the Event entity)
- [ ] Create `EventCreatedDomainEvent.cs`:
  - [ ] Contains event data for AT Protocol record
- [ ] Create `EventUpdatedDomainEvent.cs`
- [ ] Create `EventDeletedDomainEvent.cs`
- [ ] **Acceptance**: Domain events defined

### 6.5 Create PDS Adapter Service
- [ ] Create folder `Explore.Infrastructure/Services/Federation/`
- [ ] Create `IPdsService.cs`:
  - [ ] `HostRecordAsync(string did, object record)` - For Islamu-hosted
  - [ ] `ProxyRecordAsync(string remotePds, object record)` - For external PDS
- [ ] Create `PdsService.cs` implementation (placeholder for AT Protocol)
- [ ] **Acceptance**: PDS interface defined

### 6.6 Implement Outbox Interceptor
- [ ] Create folder `Explore.Persistence/Interceptors/`
- [ ] Create `OutboxInterceptor.cs : SaveChangesInterceptor`:
  - [ ] Override `SavingChangesAsync`
  - [ ] Collect domain events from tracked entities
  - [ ] Create `PdsSyncOutbox` entry for each event
  - [ ] Add to same transaction
  - [ ] Clear domain events from entities
- [ ] Register interceptor in DbContext
- [ ] **Acceptance**: Events captured atomically

### 6.7 Create Background Worker
- [ ] Create folder `Explore.API/BackgroundServices/`
- [ ] Create `OutboxProcessorService.cs : BackgroundService`:
  - [ ] Polling loop (configurable interval)
  - [ ] Query unprocessed outbox entries (batch)
  - [ ] For each: call PDS service, mark processed or retry
  - [ ] Error handling with retry count increment
- [ ] Register in Program.cs
- [ ] Add configuration for interval, batch size, max retries
- [ ] **Acceptance**: Outbox processed reliably

### 6.8 Handle DID Status State Machine
- [ ] Update Actor entity if needed for DID status
- [ ] Add `DidStatus` enum: Pending, Active, Failed
- [ ] Block write operations until DID is Active
- [ ] **Acceptance**: DID lifecycle managed

### 6.9 Run Tests
- [ ] Write unit tests for outbox interceptor
- [ ] Write integration tests for outbox processing
- [ ] Test retry logic
- [ ] **Acceptance**: All tests pass

---

## Phase 7: Virtual Tenant Masking ⏳ NOT STARTED

**Goal**: Support Single-Tenant deployment while keeping codebase Multi-Tenant

### 7.1 Add Deployment Mode Configuration
- [ ] Add to `appsettings.json`:
  ```json
  {
    "DeploymentMode": "MultiTenant"
  }
  ```
- [ ] Create `DeploymentMode.cs` enum: SingleTenant, MultiTenant
- [ ] Create `DeploymentOptions.cs` for binding
- [ ] Register in DI
- [ ] **Acceptance**: Configuration parsed correctly

### 7.2 Modify Tenant Context Middleware
- [ ] Update `TenantContextMiddleware.cs`:
  - [ ] If SingleTenant: set TenantId to DefaultTenantId
  - [ ] If MultiTenant: existing resolution logic
- [ ] **Acceptance**: Tenant resolved correctly per mode

### 7.3 Block SuperAdmin Controllers in Single-Tenant
- [ ] Create `RequiresMultiTenantAttribute.cs`
- [ ] Create action filter that returns 404 in SingleTenant
- [ ] Apply to SuperAdmin endpoints
- [ ] **Acceptance**: SuperAdmin hidden in single-tenant

### 7.4 Update Seed Data Logic
- [ ] Modify seeder for deployment mode:
  - [ ] SingleTenant: Only default tenant, no examples
  - [ ] MultiTenant: Full seed data
- [ ] **Acceptance**: Appropriate seed per mode

### 7.5 Run Tests
- [ ] Write tests for both deployment modes
- [ ] Verify tenant resolution
- [ ] Verify SuperAdmin visibility
- [ ] **Acceptance**: All tests pass

---

## Phase 8: HATEOAS & API Updates ⏳ NOT STARTED

**Goal**: Update API responses with aspect-aware links and improved discovery

### 8.1 Update Event Link Policy
- [ ] Update `EventLinkPolicy.cs`:
  - [ ] Add conditional links based on aspects:
    - [ ] `islamic-details` if IslamicAspect exists
    - [ ] `tech-details` if TechAspect exists
- [ ] **Acceptance**: Links generated per aspect

### 8.2 Create Aspect Detail Endpoints
- [ ] Add to `EventController.cs`:
  - [ ] `GET /api/v1/events/{id}/islamic` [AllowAnonymous]
  - [ ] `PUT /api/v1/events/{id}/islamic` [Authorize]
  - [ ] `DELETE /api/v1/events/{id}/islamic` [Authorize]
  - [ ] Same for tech aspect
- [ ] Create MediatR queries/commands for aspect-only operations
- [ ] **Acceptance**: Dedicated aspect endpoints work

### 8.3 Update OpenAPI Documentation
- [ ] Add EndpointSummary/Description to all new endpoints
- [ ] Document polymorphic response schemas
- [ ] Add request/response examples
- [ ] **Acceptance**: Scalar docs complete

### 8.4 Add Query Filtering by Aspects
- [ ] Update `GetEventsQuery`:
  - [ ] Add `string? Aspect` filter parameter
  - [ ] Add aspect-specific filters (e.g., `madhab=Hanafi`)
- [ ] Implement Query Specification for dynamic filtering
- [ ] Use relational tables for efficient filtering
- [ ] **Acceptance**: Filtering works efficiently

### 8.5 Run Full Test Suite
- [ ] Run all unit tests
- [ ] Run all integration tests
- [ ] Manual API testing via Scalar
- [ ] Performance testing for filtered queries
- [ ] **Acceptance**: All tests pass, performance acceptable

---

## Final Checklist ⏳ NOT STARTED

- [ ] All phases complete
- [ ] All tests passing
- [ ] Documentation updated (if needed)
- [ ] Code review completed
- [ ] Deployed to staging
- [ ] Smoke tested on staging
- [ ] Ready for production

---

## Notes

### Discovered During Implementation
_(Add notes here as issues are discovered)_

### Deferred Items
_(Add items here that are out of scope but worth tracking)_

### Questions for Stakeholder
_(Add questions that need clarification)_

---

**End of Tasks**
