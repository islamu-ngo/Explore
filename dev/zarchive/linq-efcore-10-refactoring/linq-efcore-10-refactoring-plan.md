# Plan: Modernize Explore.Persistence & Implement GDPR PII Deletion

**Last Updated**: 2026-03-26

## Executive Summary
This plan addresses two major architectural requirements:
1. **Modernization**: Adopting .NET 10 and EF Core 10 features (`Chunk`, `ToLookup`, `ExecuteDeleteAsync`, `ExecuteUpdateAsync`).
2. **GDPR Compliance & Schema Refactoring**: 
   - **Total Removal of JSONB**: Reverting all `JSONB` columns (primarily in Policy Sets) to traditional relational columns via Owned Types.
   - **GDPR PII Deletion**: Implementing a targeted "Hard Delete" strategy that removes records from PII extension tables (`UserPii`, `ActorPii`, etc.) while preserving non-identifying operational records (e.g., the `User` record itself for historical integrity, or as specified by domain rules).

## Current State Analysis
- **LINQ**: Over-reliance on `GroupBy` for in-memory mappings and manual paging for large collections.
- **JSONB Usage**: `InstancePolicySet`, `TenantPolicySet`, and `OrganizationPolicySet` use `.ToJson()` mapping (JSONB in PostgreSQL) for policy slots.
- **PII Storage**: PII is correctly isolated in extension tables (`user_pii`, `actor_pii`, `organization_pii`, `location_pii`) using 1:1 relationships.
- **Deletion**: Account deletion logic is not yet centralized or optimized via `ExecuteDeleteAsync`.

## Proposed Future State
- **Relational Policies**: Policy Sets will be mapped as flattened columns in their respective tables, removing all JSONB dependency.
- **GDPR Deletion**: `IUserRepository` and `IActorRepository` will feature specific methods to "Forget" a user/actor by deleting their PII records using `ExecuteDeleteAsync`.
- **Modern LINQ**: High-performance data shaping via `ToLookup` and `Chunk`.

## Implementation Phases

### Phase 1: Database Refactoring - JSONB Removal
Revert all JSONB columns to standard relational columns.

### Phase 2: GDPR & Hard Delete Strategy
Implement the PII deletion logic and optimize generic deletions.

### Phase 3: LINQ Modernization (.NET 10)
Apply performance-oriented LINQ refactoring across the repository layer.

## Detailed Tasks

### Phase 1: JSONB Removal (Relational Policies)
#### Task 1.1: Refactor `InstancePolicySetConfiguration`
- **File**: `Explore.Persistence/Configurations/Entities/InstancePolicySetConfiguration.cs`
- **Action**: Remove all `.ToJson(...)` calls. This will cause EF Core to map the owned types as flattened columns (e.g., `modules_policy_enable_islamic_module_local_value`).
- **Effort**: M

#### Task 1.2: Refactor `TenantPolicySetConfiguration` & `OrganizationPolicySetConfiguration`
- **Files**: 
  - `Explore.Persistence/Configurations/Entities/TenantPolicySetConfiguration.cs`
  - `Explore.Persistence/Configurations/Entities/OrganizationPolicySetConfiguration.cs`
- **Action**: Remove `.ToJson(...)` calls.
- **Effort**: M

#### Task 1.3: Migration Generation
- **Action**: Run `dotnet ef migrations add RemoveJsonbPolicies` and verify the generated SQL uses `ALTER TABLE ... ADD COLUMN` and `DROP COLUMN`.
- **Effort**: S

### Phase 2: GDPR PII Deletion
#### Task 2.1: Implement `ForgetPiiAsync` in `UserRepository`
- **Files**: `IUserRepository.cs`, `UserRepository.cs`
- **Logic**: Use `_dbContext.Set<UserPii>().Where(p => p.UserId == userId).ExecuteDeleteAsync()`.
- **Acceptance Criteria**: PII record is deleted; core `User` record remains (or is handled separately based on business logic).
- **Effort**: S

#### Task 2.2: Implement `ForgetPiiAsync` in `ActorRepository`
- **Files**: `IActorRepository.cs`, `ActorRepository.cs`
- **Logic**: Use `ExecuteDeleteAsync` on `ActorPii`.
- **Effort**: S

#### Task 2.3: Generic `HardDelete` Optimization
- **File**: `GenericRepository.cs`
- **Action**: Refactor `HardDelete` to use `ExecuteDeleteAsync` using the entity's primary key to avoid loading the entity.
- **Effort**: S

### Phase 3: LINQ Modernization
#### Task 3.1: Shaping & Indexing
- **Files**: `CategoryTypeCategoriesRepository.cs`, `TagTypeTagsRepository.cs`
- **Action**: Use `.ToLookup()` for many-to-many grouping. Use `.Index()` in paged queries requiring rank.
- **Effort**: S

#### Task 3.2: Batching & Parameters
- **Files**: `UserRepository.cs`, `AppSettingRepository.cs`
- **Action**: Apply `.Chunk(100)` to all methods receiving large collections for `Contains` filters.
- **Effort**: S

## Risk Assessment and Mitigation Strategies
- **Risk**: Removal of JSONB will significantly change the table schema for Policy Sets.
  - **Mitigation**: Existing data in JSONB columns must be migrated to the new columns via the migration script if production data exists.
- **Risk**: `ExecuteDeleteAsync` bypasses navigation property cascades if not configured at the DB level.
  - **Mitigation**: Ensure `OnDelete(DeleteBehavior.Cascade)` is properly configured in the PII configurations (already present in many).
- **Risk**: GDPR compliance requires ensuring *no* identifying data remains in logs or other tables.
  - **Mitigation**: The `ForgetPiiAsync` should be the first step in a larger "Account Deletion" orchestration.

## Success Metrics
- Zero JSONB columns in the final PostgreSQL schema.
- Verified deletion of PII records upon request without breaking database referential integrity.
- Successful batching of large ID sets in repository methods.

## Effort Estimates
Total estimated effort: **3-4 Days**
- Phase 1: 1 Day (Configuration + Migration Verification)
- Phase 2: 1 Day (GDPR logic + Generic Repo)
- Phase 3: 1 Day (LINQ Refactoring)
- Testing: 1 Day