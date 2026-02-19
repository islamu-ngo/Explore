# Modular Extensibility & Governance Refactor - Task Checklist

> **Purpose:** Detailed checklist for tracking implementation progress
>
> **Last Updated:** 2026-01-30
> **Estimated Duration:** 12-16 weeks
> **Risk Level:** High (Core architectural changes)

---

## STATUS LEGEND

| Symbol | Meaning |
|--------|---------|
| `[ ]` | Not Started |
| `[~]` | In Progress |
| `[x]` | Completed |
| `[!]` | Blocked |
| `[?]` | Needs Clarification |

---

## Phase 1: Foundation (UUID v7 + Named Query Filters)

**Duration:** 2 weeks | **Risk:** Medium | **Status:** NOT STARTED

### 1.1 UUID v7 Audit & Migration

- [ ] Audit all 44 entity configurations in `Explore.Persistence/Configurations/Entities/`
- [ ] Document entities with Guid PKs missing `uuidv7()` default
- [ ] Create list of entities requiring migration:
  - [ ] `ActorConfiguration.cs`
  - [ ] `ActorKeyStoreConfiguration.cs`
  - [ ] `AtprotoRecordConfiguration.cs`
  - [ ] `EventSessionConfiguration.cs`
  - [ ] `EventSessionAgendaItemConfiguration.cs`
  - [ ] `LocationConfiguration.cs`
  - [ ] `OrganizationConfiguration.cs`
  - [ ] `OrganizationMemberConfiguration.cs`
  - [ ] `OrganizationReviewConfiguration.cs`
  - [ ] `StorageObjectConfiguration.cs`
  - [ ] `TenantConfiguration.cs`
  - [ ] `TenantSettingsConfiguration.cs`
  - [ ] `TenantUserConfiguration.cs`
  - [ ] `UserConfiguration.cs`
  - [ ] `UserAuthenticationTokenConfiguration.cs`
  - [ ] `UserExternalLoginConfiguration.cs`
  - [ ] `UserRoleConfiguration.cs`

### 1.2 UUID v7 Implementation

- [ ] Verify PostgreSQL version supports `uuidv7()` (PG 18+ native, or pg_uuidv7 extension)
- [ ] Create `GuidVersion7Converter` value converter for application-side fallback
  - File: `Explore.Persistence/Converters/GuidVersion7Converter.cs`
- [ ] Update `EventConfiguration.cs` - verify existing `HasDefaultValueSql("uuidv7()")`
- [ ] Update `ActorConfiguration.cs` with UUID v7 default
- [ ] Update `OrganizationConfiguration.cs` with UUID v7 default
- [ ] Update `UserConfiguration.cs` with UUID v7 default
- [ ] Update `EventSessionConfiguration.cs` with UUID v7 default
- [ ] Update `TenantConfiguration.cs` with UUID v7 default
- [ ] Update remaining Guid PK entities (batch update)
- [ ] Generate migration for UUID v7 defaults
- [ ] Test migration on staging database
- [ ] Verify existing data remains intact (no ID changes)

### 1.3 Named Query Filters (EF Core 10)

- [ ] Verify EF Core 10 is installed and configured
- [ ] Refactor `ExploreDbContext.ApplyGlobalQueryFilters()` method
- [ ] Split combined filters for `Event` entity:
  - [ ] `HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId)`
  - [ ] `HasQueryFilter(name: "SoftDelete", predicate: e => !e.IsDeleted)`
- [ ] Split combined filters for `EventSession` entity
- [ ] Split combined filters for `Organization` entity
- [ ] Split combined filters for `OrganizationMember` entity
- [ ] Split combined filters for `Actor` entity
- [ ] Split combined filters for `User` entity
- [ ] Apply named filters to all `ITenantEntity` entities
- [ ] Apply named filters to all `ISoftDeletable` entities

### 1.4 Query Filter Extensions

- [ ] Create `QueryableExtensions.cs` in `Explore.Persistence/Extensions/`
- [ ] Implement `IncludeDeleted<T>()` extension method
- [ ] Implement `IgnoreTenantFilter<T>()` extension (admin only, with security check)
- [ ] Document extension usage in code comments

### 1.5 Phase 1 Testing

- [ ] Write unit tests for `GuidVersion7Converter`
- [ ] Write unit tests for selective filter disabling
- [ ] Write integration test: `IncludeDeleted()` returns soft-deleted entities
- [ ] Write integration test: Tenant filter cannot be bypassed without admin role
- [ ] Run all existing tests to ensure no regressions
- [ ] Performance benchmark: UUID v7 vs random GUID query times

### 1.6 Phase 1 Acceptance Criteria

- [ ] All Guid PK entities have `HasDefaultValueSql("uuidv7()")`
- [ ] Migration generated and applied successfully
- [ ] Existing data remains intact
- [ ] Named filters can be selectively disabled
- [ ] All existing tests pass
- [ ] Performance benchmarks acceptable

---

## Phase 2: Cascading Settings Engine

**Duration:** 2 weeks | **Risk:** Low | **Status:** NOT STARTED

### 2.1 Domain Entities

- [ ] Create `SystemSetting` entity in `Explore.Domain/SystemSetting.cs`
  - Properties: Id, Key, Value, IsLocked, AllowedValues, Description, Category
- [ ] Refactor `TenantSettings` to `TenantSetting` (Key-Value structure)
  - File: `Explore.Domain/TenantSetting.cs`
  - Properties: Id, TenantId, Key, Value
- [ ] Add navigation property to `Tenant.cs` if needed
- [ ] Document breaking change in migration notes

### 2.2 Persistence Layer

- [ ] Create `SystemSettingConfiguration.cs` in `Explore.Persistence/Configurations/Entities/`
  - UUID v7 default
  - Unique index on Key
  - Index on Category
- [ ] Create/Update `TenantSettingConfiguration.cs`
  - UUID v7 default
  - Unique composite index on (TenantId, Key)
  - Foreign key to Tenant with CASCADE delete
- [ ] Add `DbSet<SystemSetting>` to `ExploreDbContext.cs`
- [ ] Add/Update `DbSet<TenantSetting>` to `ExploreDbContext.cs`
- [ ] Generate migration for SystemSettings table
- [ ] Generate migration for refactored TenantSettings table
- [ ] Create data migration script for existing TenantSettings data

### 2.3 Application Layer - Settings Resolver

- [ ] Create `ISettingsResolver` interface in `Explore.Application/Contracts/Infrastructure/`
  - `GetSettingAsync<T>(string key)`
  - `GetSettingAsync<T>(string key, Guid tenantId)`
  - `CanOverrideAsync(string key)`
  - `GetAllSettingsAsync()`
- [ ] Create `SettingsResolver` in `Explore.Infrastructure/Services/`
- [ ] Implement resolution logic:
  1. Get SystemSetting by key
  2. If IsLocked, return system value
  3. Check TenantSetting for override
  4. Fall back to SystemSetting default
- [ ] Add `IMemoryCache` caching layer (5-minute TTL)
- [ ] Register `ISettingsResolver` in DI container

### 2.4 CQRS Handlers for Settings

- [ ] Create `GetSettingQuery` and `GetSettingQueryHandler`
- [ ] Create `GetAllSettingsQuery` and handler
- [ ] Create `UpdateSystemSettingCommand` and handler (Admin only)
- [ ] Create `UpdateTenantSettingCommand` and handler
- [ ] Create `CreateSystemSettingCommand` and handler (SuperAdmin only)
- [ ] Create `DeleteSystemSettingCommand` and handler (SuperAdmin only)

### 2.5 Settings Seed Data

- [ ] Create `SystemSettingSeedData.cs` in `Explore.Persistence/Seed/`
- [ ] Seed initial settings:
  - [ ] `Events.MaxAttendeesPerEvent` (unlocked, default: 1000)
  - [ ] `Federation.PdsUrl` (locked, default: "https://pds.islamu.io")
  - [ ] `Modules.EnabledByDefault` (unlocked, default: ["Core"])
  - [ ] `Deployment.Mode` (locked, default: "MultiTenant")
- [ ] Add seed data to migration or seeder class

### 2.6 Phase 2 Testing

- [ ] Write unit tests for `SettingsResolver`
  - Test locked setting returns system value
  - Test unlocked setting returns tenant override
  - Test fallback to system default
  - Test caching behavior
- [ ] Write unit tests for settings CQRS handlers
- [ ] Write integration tests for settings CRUD
- [ ] Write integration test: Tenant cannot override locked setting

### 2.7 Phase 2 Acceptance Criteria

- [ ] `SystemSetting` entity created with all fields
- [ ] `TenantSetting` entity refactored
- [ ] Migrations generated and applied
- [ ] `ISettingsResolver` working with caching
- [ ] Seed data for default settings
- [ ] All tests passing

---

## Phase 3: Relational Aspect Architecture

**Duration:** 3 weeks | **Risk:** High | **Status:** NOT STARTED

### 3.1 Domain Entities - Aspect Tables

- [ ] Create `Explore.Domain/Aspects/` directory
- [ ] Create `EventIslamicAspect.cs`:
  - `Guid EventId` (PK and FK)
  - `int? MadhabId`
  - `int? PrayerTimeOffsetMinutes`
  - `string? PrayerTimeReference`
  - `string? GenderMode`
  - `int? InstructionLanguageId`
- [ ] Create `EventTechAspect.cs`:
  - `Guid EventId` (PK and FK)
  - `string? GithubRepoUrl`
  - `string? TechStack` (JSON array)
  - `string? SkillLevel`
  - `bool IsHandsOn`
  - `string? LiveCodingUrl`
- [ ] Create `IAspect` marker interface (optional, for generic handling)

### 3.2 Modify Event Entity

- [ ] Add navigation property `EventIslamicAspect? IslamicAspect` to `Event.cs`
- [ ] Add navigation property `EventTechAspect? TechAspect` to `Event.cs`
- [ ] Add `string? MetadataJson` for dynamic/rare fields
- [ ] Mark `MadhabId` and `Madhab` for removal (DO NOT remove yet)
- [ ] Document breaking change in migration notes

### 3.3 Persistence Layer - Aspect Configurations

- [ ] Create `Explore.Persistence/Configurations/Entities/Aspects/` directory
- [ ] Create `EventIslamicAspectConfiguration.cs`:
  - Table name: "EventIslamicAspects"
  - Shared PK pattern: `HasKey(a => a.EventId)`
  - 1:1 relationship with `HasOne(a => a.Event).WithOne(e => e.IslamicAspect)`
  - FK to Madhab with RESTRICT delete
  - FK to Language with RESTRICT delete
- [ ] Create `EventTechAspectConfiguration.cs`:
  - Table name: "EventTechAspects"
  - Shared PK pattern
  - 1:1 relationship
- [ ] Update `EventConfiguration.cs`:
  - Remove Madhab relationship (after data migration)
  - Add MetadataJson column with `jsonb` type
- [ ] Add `DbSet<EventIslamicAspect>` to `ExploreDbContext.cs`
- [ ] Add `DbSet<EventTechAspect>` to `ExploreDbContext.cs`

### 3.4 Data Migration

- [ ] Generate migration for aspect tables
- [ ] Create data migration script:
  ```sql
  -- 1. Create aspect tables
  -- 2. Migrate MadhabId data to EventIslamicAspects
  -- 3. Drop MadhabId from Events (after verification)
  ```
- [ ] Test migration on staging with production data copy
- [ ] Create rollback script
- [ ] Document migration steps for operations team

### 3.5 Application Layer - DTOs

- [ ] Create `Explore.Application/DTOs/Event/Aspects/` directory
- [ ] Create `EventIslamicAspectDto.cs`:
  - MadhabId, MadhabName
  - PrayerTimeOffsetMinutes, PrayerTimeReference
  - GenderMode
  - InstructionLanguageId, InstructionLanguageName
- [ ] Create `CreateEventIslamicAspectDto.cs`
- [ ] Create `UpdateEventIslamicAspectDto.cs`
- [ ] Create `EventTechAspectDto.cs`:
  - GithubRepoUrl
  - TechStack (List<string>)
  - SkillLevel
  - IsHandsOn
  - LiveCodingUrl
- [ ] Create `CreateEventTechAspectDto.cs`
- [ ] Create `UpdateEventTechAspectDto.cs`

### 3.6 Update Event DTOs

- [ ] Update `EventDto.cs`:
  - Remove MadhabId, MadhabName
  - Add `List<string> AvailableAspects`
  - Add `EventIslamicAspectDto? IslamicAspect`
  - Add `EventTechAspectDto? TechAspect`
  - Add `Dictionary<string, object>? Metadata`
- [ ] Update `EventListDto.cs`:
  - Add `List<string> AvailableAspects`
- [ ] Update `CreateEventDto.cs`:
  - Remove MadhabId
  - Add `CreateEventIslamicAspectDto? IslamicAspect`
  - Add `CreateEventTechAspectDto? TechAspect`
  - Add `Dictionary<string, object>? Metadata`
- [ ] Update `UpdateEventDto.cs` similarly

### 3.7 AutoMapper Profiles

- [ ] Update `MappingProfile.cs` with aspect mappings:
  - [ ] `EventIslamicAspect` -> `EventIslamicAspectDto`
  - [ ] `CreateEventIslamicAspectDto` -> `EventIslamicAspect`
  - [ ] `EventTechAspect` -> `EventTechAspectDto` (with TechStack JSON deserialization)
  - [ ] `CreateEventTechAspectDto` -> `EventTechAspect` (with TechStack JSON serialization)
- [ ] Update `Event` -> `EventDto` mapping:
  - Add `AvailableAspects` calculation
  - Add `Metadata` JSON deserialization
- [ ] Create helper method `GetAvailableAspects(Event e)`

### 3.8 Repositories

- [ ] Create `IEventIslamicAspectRepository` interface
- [ ] Create `EventIslamicAspectRepository` implementation
- [ ] Create `IEventTechAspectRepository` interface
- [ ] Create `EventTechAspectRepository` implementation
- [ ] Register repositories in DI container

### 3.9 Update Query Handlers

- [ ] Update `GetEventDetailsRequestHandler.cs`:
  - Add `.Include(x => x.IslamicAspect).ThenInclude(a => a.Madhab)`
  - Add `.Include(x => x.IslamicAspect).ThenInclude(a => a.InstructionLanguage)`
  - Add `.Include(x => x.TechAspect)`
- [ ] Update `GetEventListRequestHandler.cs`:
  - Add minimal aspect includes for `AvailableAspects` calculation
- [ ] Update other Event query handlers as needed

### 3.10 Update Command Handlers

- [ ] Update `CreateEventCommandHandler.cs`:
  - Create `EventIslamicAspect` if `IslamicAspect` provided in DTO
  - Create `EventTechAspect` if `TechAspect` provided in DTO
  - Set aspect `EventId` to created event's ID
- [ ] Update `UpdateEventCommandHandler.cs`:
  - Handle aspect CRUD (create if new, update if exists, delete if removed)
- [ ] Verify `DeleteEventCommandHandler.cs` - cascade delete handles aspects

### 3.11 Aspect Filtering

- [ ] Create `EventFilterSpecification.cs` in `Explore.Application/Features/Events/Specifications/`
- [ ] Implement filter mapping:
  - `madhab` -> `IslamicAspect.Madhab.Name`
  - `gendermode` -> `IslamicAspect.GenderMode`
  - `skilllevel` -> `TechAspect.SkillLevel`
  - `ishandson` -> `TechAspect.IsHandsOn`
  - `hasaspect=islamic` -> `IslamicAspect != null`
  - `hasaspect=tech` -> `TechAspect != null`
- [ ] Update `GetEventListRequestHandler` to use specification
- [ ] Document filter query parameters in API docs

### 3.12 Validators

- [ ] Create `CreateEventIslamicAspectDtoValidator.cs`
  - Validate MadhabId exists
  - Validate PrayerTimeReference is valid value
  - Validate GenderMode is valid value
- [ ] Create `CreateEventTechAspectDtoValidator.cs`
  - Validate GithubRepoUrl format
  - Validate SkillLevel is valid value
- [ ] Update `CreateEventDtoValidator.cs` to validate aspects

### 3.13 Phase 3 Testing

- [ ] Write unit tests for aspect mappings
- [ ] Write unit tests for aspect validators
- [ ] Write unit tests for `EventFilterSpecification`
- [ ] Write integration tests for aspect CRUD:
  - [ ] Create event with Islamic aspect
  - [ ] Create event with Tech aspect
  - [ ] Create event with both aspects
  - [ ] Update event to add aspect
  - [ ] Update event to remove aspect
  - [ ] Delete event cascades to aspects
- [ ] Write integration tests for aspect filtering
- [ ] Verify all existing Event tests still pass
- [ ] Performance test: Event queries with aspect joins

### 3.14 Phase 3 Acceptance Criteria

- [ ] `EventIslamicAspect` and `EventTechAspect` entities created
- [ ] Entity configurations with 1:1 shared PK pattern
- [ ] Migration with data migration from `MadhabId`
- [ ] `MadhabId` removed from Event entity
- [ ] DTOs updated with aspect support
- [ ] AutoMapper profiles updated
- [ ] Query handlers include aspects
- [ ] Command handlers create/update/delete aspects
- [ ] Aspect filtering working
- [ ] All tests passing

---

## Phase 4: Module Governance

**Duration:** 2 weeks | **Risk:** Low | **Status:** NOT STARTED

### 4.1 Domain Entities

- [ ] Create `Explore.Domain/Modules/` directory
- [ ] Create `ModuleDefinition.cs`:
  - Guid Id
  - string Key (e.g., "Mod_Islamic", "Mod_Tech")
  - string Name
  - string? Description
  - string? WizardSchemaUrl
  - string? IconName
  - int DisplayOrder
  - bool IsActive
- [ ] Create `TenantCapability.cs`:
  - Guid Id
  - Guid TenantId
  - Guid ModuleId
  - bool IsEnabled
  - DateTime EnabledAt
  - Guid? EnabledBy

### 4.2 Persistence Layer

- [ ] Create `Explore.Persistence/Configurations/Entities/Modules/` directory
- [ ] Create `ModuleDefinitionConfiguration.cs`:
  - UUID v7 default
  - Unique index on Key
  - Seed default modules (Core, Islamic, Tech)
- [ ] Create `TenantCapabilityConfiguration.cs`:
  - UUID v7 default
  - Unique composite index on (TenantId, ModuleId)
  - FK to Tenant with CASCADE
  - FK to ModuleDefinition with CASCADE
- [ ] Add `DbSet<ModuleDefinition>` to ExploreDbContext
- [ ] Add `DbSet<TenantCapability>` to ExploreDbContext
- [ ] Generate migrations

### 4.3 Seed Data

- [ ] Add seed data for `ModuleDefinition`:
  - [ ] Mod_Core (Core Events) - always active
  - [ ] Mod_Islamic (Islamic Events) - optional
  - [ ] Mod_Tech (Tech Events) - optional
- [ ] Document how to add new modules

### 4.4 Application Layer - Module Service

- [ ] Create `IModuleService` interface in `Explore.Application/Contracts/Infrastructure/`:
  - `GetAvailableModulesAsync()` - modules for current tenant
  - `IsModuleEnabledAsync(string moduleKey)` - check if enabled
  - `GetModuleWizardSchemaAsync(string moduleKey)` - get form schema
- [ ] Create `ModuleService` in `Explore.Infrastructure/Services/`
- [ ] Add caching for module queries
- [ ] Register in DI container

### 4.5 Module DTOs

- [ ] Create `ModuleDefinitionDto.cs`
- [ ] Create `TenantCapabilityDto.cs`

### 4.6 Module CQRS Handlers

- [ ] Create `GetAvailableModulesQuery` and handler
- [ ] Create `GetModuleSchemaQuery` and handler
- [ ] Create `EnableModuleCommand` and handler (Admin only)
- [ ] Create `DisableModuleCommand` and handler (Admin only)
- [ ] Create `GetTenantCapabilitiesQuery` and handler

### 4.7 API Controller

- [ ] Create `ModuleController.cs` in `Explore.API/Controllers/`:
  - [ ] `GET /api/modules/available` - list available modules
  - [ ] `GET /api/modules/{moduleKey}/schema` - get wizard schema
  - [ ] `POST /api/admin/modules/{moduleKey}/enable` - enable for tenant (Admin)
  - [ ] `POST /api/admin/modules/{moduleKey}/disable` - disable for tenant (Admin)

### 4.8 Integration with Aspects

- [ ] Update `CreateEventDtoValidator` to check module availability:
  - If `IslamicAspect` provided, check `Mod_Islamic` is enabled
  - If `TechAspect` provided, check `Mod_Tech` is enabled
- [ ] Return clear error message if module not enabled
- [ ] Update `EventController` to include module context in responses

### 4.9 Phase 4 Testing

- [ ] Write unit tests for `ModuleService`
- [ ] Write unit tests for module CQRS handlers
- [ ] Write integration tests for module discovery endpoint
- [ ] Write integration test: Cannot use aspect if module disabled
- [ ] Write integration test: Enable/disable module for tenant

### 4.10 Phase 4 Acceptance Criteria

- [ ] `ModuleDefinition` entity with seed data
- [ ] `TenantCapability` entity
- [ ] `IModuleService` interface and implementation
- [ ] Module discovery endpoints working
- [ ] Aspect validation respects module availability
- [ ] Admin can enable/disable modules
- [ ] All tests passing

---

## Phase 5: Strategy Pattern Implementation

**Duration:** 2 weeks | **Risk:** Low | **Status:** NOT STARTED

### 5.1 Strategy Interface

- [ ] Create `Explore.Application/Contracts/Strategies/` directory
- [ ] Create `IEventStrategy` interface:
  - `string ModuleKey { get; }`
  - `Task<ValidationResult> ValidateAsync(CreateEventDto dto, CancellationToken ct)`
  - `Task PostCreateAsync(Event @event, CancellationToken ct)`
  - `Task PostUpdateAsync(Event @event, CancellationToken ct)`
  - `IEnumerable<LinkDto> GetLinks(Event @event)`
- [ ] Create `IStrategyResolver` interface:
  - `Task<IEnumerable<IEventStrategy>> GetApplicableStrategiesAsync(CreateEventDto dto, CancellationToken ct)`
  - `Task<ValidationResult> ValidateWithStrategiesAsync(CreateEventDto dto, CancellationToken ct)`

### 5.2 Base Strategy

- [ ] Create `BaseEventStrategy` abstract class (optional, for shared logic)

### 5.3 Islamic Strategy

- [ ] Create `Explore.Infrastructure/Strategies/` directory
- [ ] Create `IslamicEventStrategy.cs`:
  - `ModuleKey = "Mod_Islamic"`
  - Validate PrayerTimeReference values
  - Validate GenderMode values
  - PostCreate: trigger prayer time calculation (if configured)
  - GetLinks: return islamic-details link

### 5.4 Tech Strategy

- [ ] Create `TechEventStrategy.cs`:
  - `ModuleKey = "Mod_Tech"`
  - Validate GitHub URL format
  - Validate SkillLevel values
  - GetLinks: return tech-details link

### 5.5 Strategy Resolver

- [ ] Create `StrategyResolver.cs`:
  - Inject `IEnumerable<IEventStrategy>` and `IModuleService`
  - Filter strategies by enabled modules
  - Match strategies to DTO aspect data
- [ ] Implement `ValidateWithStrategiesAsync` to aggregate validation errors

### 5.6 DI Registration

- [ ] Register `IEventStrategy` implementations
- [ ] Register `IStrategyResolver`
- [ ] Ensure strategies are scoped appropriately

### 5.7 Integrate with Handlers

- [ ] Update `CreateEventCommandHandler`:
  - Inject `IStrategyResolver`
  - Call `ValidateWithStrategiesAsync` before creating
  - Call `PostCreateAsync` for each applicable strategy
- [ ] Update `UpdateEventCommandHandler`:
  - Call `PostUpdateAsync` for each applicable strategy

### 5.8 Integrate with HATEOAS

- [ ] Update `EventLinkPolicy`:
  - Inject `IStrategyResolver`
  - Aggregate links from all applicable strategies

### 5.9 Prayer Time Integration (Optional/Future)

- [ ] Research prayer time APIs/libraries
- [ ] Create `IPrayerTimeService` interface
- [ ] Implement prayer time calculation in `IslamicEventStrategy`
- [ ] Create `GetPrayerTimesQuery` for Islamic events

### 5.10 Phase 5 Testing

- [ ] Write unit tests for `IslamicEventStrategy`
- [ ] Write unit tests for `TechEventStrategy`
- [ ] Write unit tests for `StrategyResolver`
- [ ] Write integration test: Strategy validation prevents invalid data
- [ ] Write integration test: Strategies add correct HATEOAS links

### 5.11 Phase 5 Acceptance Criteria

- [ ] `IEventStrategy` interface defined
- [ ] `IslamicEventStrategy` implemented
- [ ] `TechEventStrategy` implemented
- [ ] `StrategyResolver` working with module service
- [ ] Strategies registered in DI
- [ ] Command handlers use strategy validation
- [ ] HATEOAS includes strategy links
- [ ] All tests passing

---

## Phase 6: PDS Synchronization (Outbox Pattern)

**Duration:** 3 weeks | **Risk:** Medium | **Status:** NOT STARTED

### 6.1 Domain Entity

- [ ] Create `Explore.Domain/Federation/` directory (if not exists)
- [ ] Create `PdsSyncOutbox.cs`:
  - Guid Id
  - string Did
  - string Collection
  - string RecordKey
  - string Operation (create/update/delete)
  - string? Payload (JSON)
  - DateTime CreatedAt
  - DateTime? ProcessedAt
  - int RetryCount
  - string? LastError
  - string Status (pending/processing/completed/failed)

### 6.2 Persistence Layer

- [ ] Create `PdsSyncOutboxConfiguration.cs`:
  - UUID v7 default
  - Index on Status for worker queries
  - Index on CreatedAt for ordering
- [ ] Add `DbSet<PdsSyncOutbox>` to ExploreDbContext
- [ ] Generate migration

### 6.3 PDS Service Interface

- [ ] Create `IPdsService` interface in `Explore.Application/Contracts/Infrastructure/`:
  - `HostRecordAsync` - write to Islamu PDS
  - `ProxyRecordAsync` - write to external PDS
  - `DeleteRecordAsync` - delete from PDS
  - `ResolvePdsAsync` - determine which PDS for an actor
- [ ] Create `PdsWriteResult` record
- [ ] Create `PdsResolution` record

### 6.4 SaveChanges Interceptor

- [ ] Create `Explore.Persistence/Interceptors/` directory
- [ ] Create `PdsSyncInterceptor.cs`:
  - Inherit from `SaveChangesInterceptor`
  - Override `SavingChangesAsync`
  - Detect Event entity changes (Added/Modified/Deleted)
  - Create outbox entries for federated events
  - Map entity to ATProto record format
- [ ] Register interceptor in ExploreDbContext configuration

### 6.5 PDS Service Implementation

- [ ] Create `PdsService.cs` in `Explore.Infrastructure/Services/`:
  - Implement `HostRecordAsync` using ATProto client
  - Implement `ProxyRecordAsync` for external PDS
  - Implement `DeleteRecordAsync`
  - Implement `ResolvePdsAsync` using Actor.PdsHost
- [ ] Handle authentication tokens for PDS writes
- [ ] Implement error handling and logging

### 6.6 Background Worker

- [ ] Create `Explore.Infrastructure/BackgroundServices/` directory
- [ ] Create `PdsSyncWorker.cs`:
  - Inherit from `BackgroundService`
  - Poll outbox for pending entries (every 5 seconds)
  - Process entries with retry logic
  - Update status after processing
  - Log errors and metrics
- [ ] Implement exponential backoff for retries
- [ ] Implement dead letter handling (after N retries)
- [ ] Register worker in DI container

### 6.7 Configuration

- [ ] Add PDS sync settings to `appsettings.json`:
  - PollingIntervalSeconds
  - MaxRetryCount
  - RetryDelaySeconds
  - BatchSize
- [ ] Create `PdsSyncSettings` configuration class
- [ ] Register in DI

### 6.8 Phase 6 Testing

- [ ] Write unit tests for `PdsSyncInterceptor`
- [ ] Write unit tests for entity-to-ATProto mapping
- [ ] Write unit tests for `PdsService` (with mock HTTP client)
- [ ] Write integration test: Create event generates outbox entry
- [ ] Write integration test: Worker processes pending entries
- [ ] Write integration test: Failed entries are retried
- [ ] Write integration test: Dead letters after max retries

### 6.9 Phase 6 Acceptance Criteria

- [ ] `PdsSyncOutbox` entity created
- [ ] `IPdsService` interface and implementation
- [ ] SaveChanges interceptor creates outbox entries
- [ ] Background worker processes outbox
- [ ] Retry logic with exponential backoff
- [ ] Error logging and monitoring
- [ ] Support for hosted PDS
- [ ] Support for external PDS proxy
- [ ] All tests passing

---

## Phase 7: Virtual Tenant Masking

**Duration:** 1 week | **Risk:** Low | **Status:** NOT STARTED

### 7.1 Configuration

- [ ] Create `DeploymentSettings.cs` in `Explore.Application/Settings/`:
  - `string Mode` ("SingleTenant" or "MultiTenant")
  - `Guid DefaultTenantId`
  - `bool HideSuperAdminInSingleTenant`
- [ ] Add `DeploymentMode` enum
- [ ] Update `appsettings.json` with Deployment section
- [ ] Register configuration in DI

### 7.2 Tenant Context Modification

- [ ] Modify `TenantContext.cs` in `Explore.Infrastructure/Services/`:
  - Inject `IOptions<DeploymentSettings>`
  - In SingleTenant mode: always return DefaultTenantId
  - In MultiTenant mode: resolve from request (header/subdomain)
- [ ] Implement subdomain resolution logic
- [ ] Implement header resolution logic (X-Tenant-Id)

### 7.3 SingleTenant Guard

- [ ] Create `BlockInSingleTenantAttribute.cs` in `Explore.API/Filters/`:
  - Implement `IAuthorizationFilter`
  - Check deployment mode
  - Return 404 if SingleTenant and HideSuperAdminInSingleTenant
- [ ] Apply attribute to SuperAdmin controllers:
  - [ ] TenantAdminController (if exists)
  - [ ] SystemSettingsController (if admin-only)
  - [ ] Other SuperAdmin endpoints

### 7.4 UI Adjustments

- [ ] Update Blazor navigation to hide SuperAdmin menu in SingleTenant
- [ ] Update admin dashboard for deployment mode

### 7.5 Phase 7 Testing

- [ ] Write unit tests for `TenantContext` in SingleTenant mode
- [ ] Write unit tests for `TenantContext` in MultiTenant mode
- [ ] Write unit tests for `BlockInSingleTenantAttribute`
- [ ] Write integration test: SingleTenant always uses default tenant
- [ ] Write integration test: SuperAdmin endpoints hidden in SingleTenant
- [ ] Write integration test: MultiTenant resolves from subdomain

### 7.6 Phase 7 Acceptance Criteria

- [ ] `DeploymentSettings` configuration class
- [ ] `TenantContext` respects deployment mode
- [ ] `BlockInSingleTenant` attribute working
- [ ] SuperAdmin controllers blocked in single-tenant
- [ ] Configuration documented
- [ ] All tests passing

---

## Phase 8: Integration & Testing

**Duration:** 2 weeks | **Risk:** Medium | **Status:** NOT STARTED

### 8.1 Handler Updates

- [ ] Verify all Event handlers work with new architecture
- [ ] Update any remaining handlers not covered in earlier phases
- [ ] Ensure consistent error handling across all handlers

### 8.2 HATEOAS Updates

- [ ] Update `EventLinkPolicy.cs`:
  - Add aspect-specific links based on `AvailableAspects`
  - Add islamic-aspect link if Islamic aspect present
  - Add tech-aspect link if Tech aspect present
- [ ] Update other link policies as needed
- [ ] Verify HATEOAS responses in integration tests

### 8.3 Blazor Updates

- [ ] Update Event list component for new DTO structure
- [ ] Update Event detail component for aspects
- [ ] Create/update Event form for aspect input
- [ ] Add module-aware conditional rendering
- [ ] Update Event filtering UI for aspect filters

### 8.4 Comprehensive Integration Tests

- [ ] Create `Event.API.IntegrationTests/Features/Aspects/` directory
- [ ] Write `EventAspectTests.cs`:
  - [ ] Create event with Islamic aspect - verify in response
  - [ ] Create event with Tech aspect - verify in response
  - [ ] Create event with multiple aspects
  - [ ] Filter events by madhab
  - [ ] Filter events by skill level
  - [ ] Filter events by aspect existence
- [ ] Write `ModuleGovernanceTests.cs`:
  - [ ] Module discovery returns enabled modules
  - [ ] Cannot use disabled module's aspect
  - [ ] Admin can enable/disable modules
- [ ] Write `SettingsResolutionTests.cs`:
  - [ ] Locked setting returns system value
  - [ ] Unlocked setting returns tenant override
  - [ ] Setting update propagates correctly

### 8.5 Multi-Tenant Testing

- [ ] Test tenant isolation with aspects
- [ ] Test module governance per tenant
- [ ] Test settings per tenant
- [ ] Test cross-tenant data access prevention

### 8.6 Single-Tenant Testing

- [ ] Test single-tenant mode end-to-end
- [ ] Verify SuperAdmin endpoints hidden
- [ ] Verify default tenant always used

### 8.7 Performance Testing

- [ ] Benchmark Event queries with aspect joins
- [ ] Benchmark UUID v7 vs random GUID queries
- [ ] Benchmark settings resolution with caching
- [ ] Benchmark module service with caching
- [ ] Identify and address any performance regressions

### 8.8 Documentation Updates

- [ ] Update `docs/ARCHITECTURE.md` with aspect pattern
- [ ] Update `docs/API.md` with new endpoints
- [ ] Update `docs/DOMAIN.md` with new entities
- [ ] Create/update `docs/MODULAR_EXTENSIBILITY.md`
- [ ] Update OpenAPI documentation (Swagger)
- [ ] Create migration guide for existing deployments
- [ ] Document breaking changes and deprecations

### 8.9 Phase 8 Acceptance Criteria

- [ ] All existing tests passing
- [ ] New integration tests for aspects
- [ ] New integration tests for modules
- [ ] New integration tests for settings
- [ ] HATEOAS includes aspect links
- [ ] Blazor components updated
- [ ] Documentation complete
- [ ] Performance benchmarks acceptable
- [ ] No unexpected breaking changes

---

## Post-Implementation Tasks

### Deployment Preparation

- [ ] Create database migration rollback scripts
- [ ] Document deployment sequence
- [ ] Prepare staging environment
- [ ] Test migrations on staging with production data
- [ ] Create monitoring dashboards for new features
- [ ] Set up alerts for PDS sync failures

### Cleanup

- [ ] Remove deprecated code marked for deletion
- [ ] Remove TODO comments for completed items
- [ ] Archive this task file to `dev/archive/`
- [ ] Update `dev/active/journal.md` with lessons learned

### Future Enhancements (Out of Scope)

- [ ] Additional aspect types (Medical, Sports, etc.)
- [ ] Dynamic aspect schema (JSON Schema-based)
- [ ] Aspect inheritance/composition
- [ ] Advanced strategy patterns (chains, decorators)
- [ ] Real-time PDS sync (webhooks)

---

## QUICK RESUME

To continue work on this refactor:

1. **Find current phase:** Look for first phase with NOT STARTED or IN PROGRESS status
2. **Find current task:** Look for first unchecked `[ ]` item in that phase
3. **Mark in progress:** Change `[ ]` to `[~]` when starting
4. **Mark complete:** Change `[~]` to `[x]` when done
5. **Update context.md:** Update SESSION PROGRESS section after significant work

---

## Related Files

- **[Implementation Plan](./modular-extensibility-refactor-plan.md)** - Detailed technical plan
- **[Context](./modular-extensibility-refactor-context.md)** - Key decisions and session progress
- **[Architecture](../../docs/ARCHITECTURE.md)** - System architecture
- **[Extensibility](../../docs/EXTENSIBILITY.md)** - Aspect architecture docs

---

**Document Version:** 1.0
**Created:** 2026-01-30
**Last Updated:** 2026-01-30
