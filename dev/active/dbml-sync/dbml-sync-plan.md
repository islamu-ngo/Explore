# DBML Sync - Domain/Application/Persistence/API Implementation Plan

**Last Updated:** 2026-01-04
**Status:** Plan Refined with Actual Codebase Patterns

## Executive Summary

Bring the current ASP.NET Core / Clean Architecture / CQRS (MediatR) codebase into alignment with the provided DBML schema. The DBML is the source of truth. The work includes:
- Domain layer: create/update entities and relationships based on DBML
- Application layer: repository interfaces, CQRS requests/handlers, DTOs, validators, AutoMapper config
- Persistence layer: DbContext updates, entity configurations, repositories, migrations alignment
- API layer: controllers and middleware updates to expose the new/updated use cases consistently

This is a schema-driven refactor/implementation where correctness, referential integrity, and tenant isolation are primary concerns.

### ✅ Key Updates (2026-01-04)

**Analysis Completed:**
- ✅ Analyzed actual Organization entity implementation across all layers
- ✅ Identified concrete naming patterns, file structures, and conventions
- ✅ Discovered critical DBML schema errors requiring fixes
- ✅ All blocking design decisions resolved

**Critical Findings:**
1. **DBML Corrections Required** (see `dbml-corrections-required.md`):
   - atproto_record types (uuid → varchar)
   - event_session_agenda_items timestamps (timestamp → timestamptz)
   - Missing tenant_id in 3 tables
   - OrganizationReview missing from DBML

2. **Implementation Patterns Documented**:
   - DTO naming: `{Entity}Dto`, `{Entity}ListDto`, `Create{Entity}Dto`, `Update{Entity}Dto`
   - Commands return: `BaseCommandResponse<Guid>`
   - Repositories return: DTOs for queries, entities for commands
   - API routing: Must standardize to `/api/v1/[controller]`

3. **All Design Decisions Resolved** (see context.md for details):
   - ✅ atproto_record types
   - ✅ Location geo modeling
   - ✅ Tenant enforcement strategy
   - ✅ Join table modeling
   - ✅ API versioning
   - ✅ Delete behaviors
   - ✅ User ID extraction pattern
   - ✅ Repository return types

**Next Steps:**
1. Get user approval for DBML corrections
2. Update DBML schema file
3. Begin Phase 0 implementation with corrected schema

## Current State
- Existing solution uses Clean Architecture and CQRS (MediatR), FluentValidation, DTOs, repository pattern, AutoMapper.
- Current codebase is NOT in sync with DBML (entities/configurations/endpoints mismatch).
- Project documentation already defines conventions, folder structure, naming, and patterns (we will follow those; no generic examples).

## Source of Truth
- DBML provided in the prompt (latest version; includes: tenant, actor, event, event_session, registration, tags, categories, atproto_record, sync_state, etc.).

## Goals / Success Criteria
### Functional
- Domain models represent all DBML tables that are part of the business domain (and/or persistence domain) with correct relationships.
- Application layer exposes CQRS use cases for core flows (events, sessions, registrations, orgs, tags/categories, locations, actors) aligned with API needs.
- Persistence layer generates correct schema (or matches existing schema) via EF Core configurations and migrations, including constraints and indexes.
- API layer exposes consistent endpoints/controllers that map to Application CQRS without bypassing it.

### Non-functional
- Multi-tenancy: tenant boundaries are consistently enforceable (via query filters and/or repository scoping and/or controller scoping as per your conventions).
- ATProto record linkage is consistent (event.atproto_record_id, event_registration.atproto_record_id).
- Time correctness: all session time fields use timestamptz semantics and are handled consistently.

## Scope
### In Scope (DBML tables)
Core event platform:
- tenant, tenant_user, tenant_settings, user, user_role
- organization, organization_members, organization_role, organization_position, approval_status
- event, event_session, event_session_languages, event_session_speakers, event_session_agenda_items
- event_registration, registration_mode
Discovery metadata:
- category (with parent_id), event_categories
- tag, tag_type, tag_type_tags, event_tags
Lookups:
- event_type, event_status, visibility_type, event_format
- madhab, audience_age, audience_gender, language
Storage:
- storage_object, file_type
Federation/indexing:
- actor, actor_type, did_custody_type
- actor_key_store
- indexed_did, sync_state
- atproto_record
Location:
- location (coordinates + lat/long + timezone)

### Out of Scope (unless already present and required by compilation/tests)
- Full moderation system tables/workflows (not in DBML yet)
- ActivityPub gateway endpoints/objects (unless already part of API surface needing fixes)
- UI/Blazor features (unless your API project contains BFF controllers that require sync)

## Key Design Decisions (DBML → Code)
1. DBML-first alignment:
   - Update code to match schema (not schema to match code), unless a conflict is documented as intentional.
2. Explicit join entities:
   - Keep join tables as explicit entities (event_tags, event_categories, tag_type_tags, event_session_languages, etc.) rather than implicit many-to-many, unless your conventions prefer implicit and the DB schema supports it without losing tenant_id columns.
3. Lookup tables:
   - Model lookups as entities with Id + MasterCode (+ FullName/Description). Add seed strategy in Persistence if your conventions use seeding.
4. Tenant consistency:
   - For tables containing tenant_id, enforce it in Persistence (global query filters and/or composite unique constraints) according to your conventions.
   - Ensure join tables with tenant_id cannot cross-link records from other tenants.
5. atproto_record modeling:
   - Treat atproto_record as a persistence-backed concept used by multiple aggregates; ensure uniqueness constraints as per your requirements (typically did+collection+record_key).
   - Note: DBML currently uses uuid for did/record_key/cid; verify intended types during implementation.

## Implementation Phases

### Phase 0: Discovery & Alignment Spec (1–2 sessions)
Purpose: produce a “DBML → Code Map” so implementation is systematic, not ad-hoc.
Deliverables:
- Entity inventory: which tables map to which entities (Domain) and which are purely persistence/indexing concerns.
- Relationship map: cardinalities and navigation decisions.
- Constraints/index plan: uniques, required fields, cascade behaviors.
- API surface plan: which controllers/actions must exist or be updated.

Acceptance:
- A written mapping document exists (in context.md and/or an additional dev note).
- Open questions are resolved as implementation decisions (recorded in context.md).

### Phase 1: Domain Layer Implementation (2–4 sessions)
Tasks:
- Create/update Domain entities to match DBML tables and relationships.
- Introduce Value Objects / Enums only if your conventions do so while keeping DBML fidelity.
- Ensure domain invariants where applicable (e.g., session start < end, required fields not null).
- Align naming and folder placement with your repo docs.

Acceptance:
- Domain compiles.
- All entities needed by Application/Persistence exist with correct properties and relationships.
- No EF Core attributes/DbContext references in Domain.

### Phase 2: Application Layer Implementation (2–5 sessions)
Tasks:
- Repository interfaces in Application for aggregates (event, session, registration, org, tags/categories, location, actor, atproto).
- CQRS Requests/Handlers:
  - Create/update flows for core CRUD + discovery queries your API expects.
  - Ensure tenant scoping is respected.
- DTOs and AutoMapper profiles:
  - DTOs match API contract (or are updated consistently).
- FluentValidation validators:
  - Validate commands/requests using your pipeline conventions.

Acceptance:
- Application compiles and tests (if present) pass.
- CQRS handlers cover all endpoints currently expected by API.
- Mapping + validation wired consistently.

### Phase 3: Persistence Layer Implementation (2–5 sessions)
Tasks:
- Update DbContext:
  - DbSet entries for all relevant entities
  - Query filters/tenant scoping strategy
- EntityTypeConfiguration:
  - Table names, keys, required columns, max lengths
  - FK relationships and delete behaviors
  - Indexes and unique constraints (including composite uniqueness where required)
  - PostGIS/geo choices for location (DBML has point + lat/long; decide canonical mapping)
- Repositories:
  - Implement interfaces from Application
  - Ensure includes/queries align with query handlers
- Migrations:
  - Generate/adjust migrations to reach DBML schema
  - If DB already exists, create a safe migration strategy (baseline vs incremental)

Acceptance:
- dotnet build succeeds.
- Database can be created/updated to match DBML constraints.
- Repositories work with handlers (integration tests if available).

### Phase 4: API Layer Implementation (1–3 sessions)
Tasks:
- Update/create Controllers to call MediatR requests.
- Ensure consistent:
  - routing conventions (/api/v1/… or your standard)
  - error handling middleware behavior
  - auth policies and tenant resolution (if applicable)
- Add/update middleware if needed for:
  - tenant resolution
  - correlation IDs
  - exception mapping
  - validation problem details
- Ensure endpoints reflect the new schema realities (e.g., event sessions are first-class; join tables shape queries).

Acceptance:
- API project compiles and starts.
- Existing API tests pass (or are updated).
- Core endpoints function end-to-end against DB.

### Phase 5: Verification, Cleanup, Documentation (1–2 sessions)
Tasks:
- Remove dead code that references old schema.
- Add missing indexes/constraints discovered during testing.
- Update internal docs (if required by your repo conventions).

Acceptance:
- “Schema mismatch” issues are eliminated.
- dev docs updated with final decisions and any deviations from DBML.

## Risks & Mitigations
- Risk: Hidden schema expectations in code not represented in DBML.
  - Mitigation: Phase 0 mapping + explicit “deviations” log in context.md.
- Risk: Multi-tenancy bugs (cross-tenant reads/writes).
  - Mitigation: enforce scoping at repository + persistence filter level; add tests.
- Risk: atproto_record type mismatch (did/cid/rkey as uuid vs string).
  - Mitigation: validate intended types early in Phase 0; document decision.
- Risk: Join table modeling (explicit entities vs EF many-to-many) conflicting with tenant_id presence.
  - Mitigation: prefer explicit entities for join tables that carry tenant_id.
- Risk: Breaking API clients due to contract changes.
  - Mitigation: keep DTOs backward-compatible when possible; version endpoints if needed.

## Timeline Estimate
- Phase 0: 0.5–1 day
- Phase 1: 1–2 days
- Phase 2: 1–3 days
- Phase 3: 1–3 days
- Phase 4: 0.5–1.5 days
- Phase 5: 0.5–1 day

Total: ~4–10 working days depending on how far the current code diverges.

## Deliverables
- Updated Domain entities
- Updated Application CQRS + DTOs + validators + mapping
- Updated Persistence DbContext + configurations + repositories + migrations
- Updated API controllers + middleware
- Updated dev docs (context/tasks reflect reality)