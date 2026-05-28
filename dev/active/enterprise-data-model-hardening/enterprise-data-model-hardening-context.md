<!-- ABOUTME: Operational context for enterprise-grade ISLAMU Event data-model hardening. -->
<!-- ABOUTME: Preserves evidence, decisions, risks, validation baseline, and handoff notes for future agents. -->

# Enterprise Data Model Hardening — Context

Last Updated: 2026-05-28 Europe/Brussels

## SESSION PROGRESS (2026-05-28 Europe/Brussels)

### COMPLETED

- Reconciled Phase 3.3 with the user's development migration workflow: generated migration files are not part of the current source of truth for this slice.
- Confirmed the same-room session overlap rule remains code-owned in EF configuration:
  - `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs` declares `EX_EventSession_RoomNoOverlap`.
  - `Explore.Persistence/Schema/PostgresExclusionConstraintExtensions.cs` stores the model annotation.
  - `Explore.Persistence/Schema/PostgresModelConstraintApplier.cs` applies `btree_gist` plus the partial GiST exclusion constraint after EF migrations.
- Wired the model-owned constraint applier into runtime migration paths:
  - `Event.MigrationService/Worker.cs`
  - `Explore.API/Program.cs` startup migration block
  - `Explore.API/Program.cs` development `/admin/migrate` endpoint
  - `Event.Persistence.IntegrationTests/Fixtures/PostgreSqlContainerFixture.cs`
- Fixed the logged startup failure caused by EF Core raising `RelationalEventId.PendingModelChangesWarning` during runtime `MigrateAsync()` while migrations are intentionally out of sync in development:
  - `Event.MigrationService/Program.cs` now ignores the warning only when the migration service runs in Development.
  - `Explore.Persistence/PersistenceServicesRegistration.cs` now accepts the host environment name and ignores the warning only in Development runtime DbContext registration.
  - `Explore.API/Program.cs` passes `builder.Environment.EnvironmentName` into persistence registration.
- Removed leftover untracked generated migration files from `Explore.Persistence/Migrations`; no migration file or model snapshot change remains as part of the Phase 3.3 follow-up.
- Rechecked the original workstream state. Phases 0-3 remain complete; the next implementation slice is Phase 4.1 custom-property quota enforcement audit.
- Completed Phase 4.1 custom-property quota enforcement audit:
  - Added `custom-property-quota-enforcement-audit.md` with the quota registry, enforcement matrix, exact gaps, and hardening order.
  - Confirmed direct shared, event runtime, session runtime, multi-value, projection rebuild, dirty-scope, and governance-report paths already use the expected quota controls.
  - Identified gaps in event template option quotas, event-session template definition/option quotas, and template-sync resulting runtime cardinality checks.
  - Updated `docs/CUSTOM_PROPERTIES.md` with the current covered paths and known hardening gaps.
  - Used Context7 for current EF Core named query filter / `AsNoTracking()` guidance and FluentValidation `ValidateAsync` guidance.
- Completed the Phase 4.2 quota hardening sub-slice:
  - `CreateEventTemplateCommandHandler` and `UpdateEventTemplateCommandHandler` now reject definitions whose nested options exceed `max_options_per_definition`.
  - `CreateEventSessionTemplateCommandHandler` and `UpdateEventSessionTemplateCommandHandler` now enforce `max_definitions_per_template` and `max_options_per_definition`.
  - `EventTemplateSyncService` and `EventSessionTemplateSyncService` now preflight the resulting runtime definition count and option count before applying selected added definitions/options.
  - Sync quota failures now escape as `QuotaExceededException`, preserving the canonical `quota_exceeded` API shape instead of becoming generic `apply_failed` conflicts.
  - Added focused Application unit tests for event template option quotas, session-template definition/option quotas, and event/session template-sync resulting cardinality quotas.
  - Updated `docs/CUSTOM_PROPERTIES.md` and `custom-property-quota-enforcement-audit.md` to mark the quota gaps closed.
  - Verification: `Event.Application.UnitTests` passed 1057/1057; `Event.Architecture.Tests` passed 178/179 with one known skipped response-metadata test.
- Completed the Phase 4.2 semantic-reservation sub-slice:
  - Expanded `CustomPropertySemanticReservations` to include event and session Layer 2 semantic aliases for Islamic and tech aspects.
  - Hardened Layer 3 collision detection so reserved Layer 2 keys are rejected even under tenant namespaces, not only under exact `sector.*` namespace/key pairs.
  - Added application policy tests for tenant namespace collisions and a shared definition handler regression proving governance fails before duplicate repository reads or writes.
  - Updated the domain reservation test to encode semantic-key reservation as namespace-independent.
  - Verification: `Event.Application.UnitTests` passed 1062/1062 and `Event.Domain.UnitTests` passed 247/247. `Event.Domain.UnitTests` initially exposed the old exact-namespace assertion, and the assertion was updated to encode namespace-independent semantic key reservation.
- Completed the Phase 4.2 purge/retire lifecycle sub-slice:
  - Normal delete is documented as retirement: definitions are deactivated/soft-deleted, options are retired, values remain historical, event/session projections are removed, and the machine key remains reserved until an audited hard purge.
  - Hard purge remains an explicit operator action with a non-blank reason and audit log. Handlers now reuse a shared blocked-response helper and convert stale-preflight repository failures back into structured dependency errors.
  - Shared, event, and session repositories now re-check purge dependencies inside `PurgeDefinition` before executing physical deletes.
  - EF model configuration now uses restrictive delete behavior from definitions to historical values and projection rows, so regenerated migrations should not cascade hard-purge value/projection history.
  - Verification: focused `PurgeCustomPropertyDefinitionCommandHandlerTests` passed 3/3; focused shared PostgreSQL purge guard passed 1/1.
- Completed the Phase 4.3 operator-signal slice:
  - Projection status admin responses now expose `PendingDirtyScopeCount`, `OperationalState`, `RequiresOperatorAction`, and `RecommendedAction`.
  - Projection quota rejections emit `explore.projections.quota_exceeded_total`.
  - Hard-purge decisions emit `explore.custom_properties.purge_decisions` with bounded scope/outcome/blocker tags.
  - Operations docs list projection triage states and explicitly forbid raw namespace/key, display names, resource IDs, and purge reasons as metric dimensions.
  - Verification: Release build passed; full `Event.Application.UnitTests` passed 1065/1065; `Event.Architecture.Tests` passed 178/179 with the known skipped response-metadata test; scoped `git diff --check` passed.

### IN PROGRESS

- No implementation task is active. Phase 4 custom-property governance hardening is complete.

### NEXT

1. Start Phase 5.1 with the polymorphic reference registry and define allowed target kinds, ID type, tenant scope, delete behavior, and type pairings before persistence changes.
2. Keep generated migration files out of scope unless the user explicitly asks for migration regeneration.
3. Keep production RLS rollout deferred until app-role/migration-role separation and admin/system read paths are designed and tested.
4. Before changing the reference registry code, re-classify the slice against `.claude/contract/intents.yaml` and load the matching docs/rules.

### BLOCKERS

- Tavily MCP remains unavailable from tool discovery; do not claim Tavily research was performed.
- Full `Event.Persistence.IntegrationTests` is not currently green: the latest run reached 132/134 and failed two unrelated email-dispatch transition tests (`TryReplayForOperatorResetsDeferredRowToPending`, `TryParkForOperatorMarksEligibleRowAsParked`).
- Focused event/session PostgreSQL custom-property lifecycle tests currently fail before custom-property assertions because the dirty migration database is missing `events.content`; the new shared PostgreSQL purge guard passes.
- The worktree contains many unrelated dirty files from other workstreams and generated outputs. Future agents must scope status/diff checks to the active slice and must not revert unrelated changes.

## SESSION PROGRESS (2026-05-27 Europe/Brussels)

### COMPLETED

- User instructed implementation to start; workstream status moved to In implementation.
- Re-classified the first implementation slice as multi-intent persistence/data-model hardening with `add-ef-migration`, `update-repository-query`, and dev-doc maintenance implications.
- Rechecked required tooling: Context7 is available and was used for EF Core composite relationships/alternate keys and PostgreSQL composite FK/RLS primitives; Tavily MCP is still not exposed by tool discovery.
- Baseline build passed before edits: `dotnet build --configuration Release --verbosity quiet` completed with existing package/analyzer warnings and 0 errors.
- Reviewed active overlap workstreams:
  - `event-scoped-operational-roles`: event-child tenant/event context and event role authorization must not regress.
  - `backend-api-health-refactor`: previously flagged null-tenant broad-read filters; Phase 1.4 has now closed that runtime gap with explicit bypass reasons.
  - `crmworx-event-api-adaptation` and `rabbitmq-messaging`: email-dispatch state must stay PostgreSQL-source-of-truth; RabbitMQ remains optional transport.
- Implemented Phase 1.1 inventory in `tenant-scoped-fk-inventory.md`.
- Found 69 strict `ITenantEntity` domain classes. Seven were missing named tenant filters: `EventSeries`, `EventContactShareConsent`, `EventContactShareExport`, `OrganizationSetting`, `GroupSetting`, `UserPreference`, and `UserNotificationPreference`.
- Updated `Explore.Persistence/ExploreDbContext.QueryFilters.cs` so all strict `ITenantEntity` classes are now registered in named tenant filters.
- Implemented Phase 1.2 first migration scope: event graph composite tenant-safe FKs.
- Added tenant-scoped alternate keys and composite FK guardrails across the event graph, registration, taxonomy junctions, role assignments, and contact-share relationships.
- Tightened room-aware scheduling rows so `EventSession`, `EventAgendaItem`, and `EventSessionGroup` require `LocationId` when `RoomId` is present and constrain rooms through the same tenant/location pair.
- Added `EventRegistration.EventId` so registration access rows can be constrained to the same tenant and event as both their intent and selected session.
- Post-edit verification passed:
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - `git diff --check`
- Implemented Phase 1.3 RLS tenant-session prototype.
- Added disabled-by-default `PostgresTenantSessionInterceptor`, guarded by `Persistence:EnableRlsTenantSession`, to bind `app.current_tenant_id` on EF Core connection open.
- Added a PostgreSQL integration test that creates a synthetic forced-RLS table, queries through a generated non-superuser role, and proves tenant A, tenant B, and missing-tenant behavior.
- Updated security, multi-tenancy, and configuration docs to state that production tenant-table RLS is still deferred until app-role/migration-role/admin-path rollout is designed.
- Implemented Phase 1.4 runtime tenant-filter bypass hardening.
- Replaced permissive `TenantContext == null || ...` query filters with fail-closed `TenantFilterTenantId` / `IsTenantFilterBypassed` semantics.
- Added explicit bypass reasons for tenant-filter bypass helpers, production repositories, tenant lookup cache warmup, database seeding, email dispatch worker operations, API-key authentication/management, and legacy tenant-resolution lookup.
- Added architecture guardrails preventing permissive null-tenant filters and direct runtime `.IgnoreQueryFilters()` full-filter bypasses from returning.
- Added persistence integration tests proving missing tenant context hides tenant rows, tenant context scopes rows, and explicit system bypass returns bounded rows.
- Implemented Phase 2.1 tenant role grant model decision.
- Chose `TenantUserRoleGrant` as an auditable child of `TenantUser`, replacing `TenantMember` in the next breaking schema/API slice.
- Recorded the target entity shape, composite tenant FK, tenant-role-scope FK, active-grant uniqueness, backfill/fail-fast migration path, and API contract direction in `tenant-role-grant-model-decision.md`.
- Implemented Phase 2.2 tenant role grant domain/persistence model.
- Added `TenantUserRoleGrant` as an auditable `TenantUser` child and removed the old `TenantMember` domain/config/table from the active model.
- Added EF alternate keys and composite FKs so grants reference `(tenant_id, tenant_user_id)` on `tenant_users` and `(role_id, role_scope_id)` on tenant-scoped roles, with a check constraint keeping `role_scope_id = Tenant`.
- Added migration `20260527132704_ReplaceTenantMemberWithTenantUserRoleGrants`, with fail-fast preflight checks for orphan old memberships and non-tenant-scoped roles, backfill from `tenant_members`, and a downgrade that refuses silent data loss when revoke history or multiple active roles cannot be represented by the old table.
- Updated `schemas/islamu-event.md`, `docs/DOMAIN.md`, `docs/MULTI_TENANCY.md`, `docs/AUTHORIZATION.md`, `docs/SECURITY-MODEL.md`, `docs/CODEBASE_STRUCTURE.md`, and `docs/CODEBASE_INSIGHTS.md` for the internal grant model.
- Implemented Phase 2.3 internal authority/provisioning updates.
- Rewired `AdminContext`, tenant role repository, tenant onboarding, managed-provider provisioning, tenant role-grant CQRS handlers, validators, seed data, and tests to use `TenantUserRoleGrant`.
- Implemented Phase 2.4 public API/HAL/Cerbos/OpenAPI/Blazor-client contract replacement.
- Replaced the former public tenant-member DTOs, commands, handlers, controller, HAL assembler/policy, route names, Cerbos resource kind/policy/schema, and generated client operations with `TenantUserRoleGrant` contracts.
- Public route surface is now `/api/tenant-user-role-grants`; create accepts `TenantUserId` plus tenant-scoped `RoleId`, tenant is derived from `ITenantContext`, and revoke is modeled as `DELETE` with audit fields instead of update-in-place.
- HAL now exposes collection `create` and detail `revoke` affordances through `_links`; no edit/update affordance is emitted for role grants.
- OpenAPI schemas were regenerated in both `schemas/openapi.json` and `Explore.API/@schemas/openapi.json`, then the NSwag Blazor client was regenerated.
- Implemented Phase 3.1 event/session time invariant hardening.
- Chose UTC instants as the authoritative schedule write model and kept local date/time/minute fields as domain-owned projections.
- Added `ScheduleTimeZoneResolver` and tightened `EventScheduleProjectionCalculator` so blank timezone input normalizes to UTC, invalid non-blank timezone IDs fail, and zero-length/inverted schedule ranges are rejected.
- Added `Event.ApplyScheduleTimeZone(...)` and `Event.RecalculateScheduleSummaryFromSessions()` so full schedule graph updates reproject sessions/agenda items, relink day IDs by local date, and refresh event rollups through one approved aggregate method.
- Updated AutoMapper profiles and event update handlers so DTO mapping cannot write scheduled child UTC/local projection fields or event schedule rollups directly.
- Added migration `20260527204604_FormalizeScheduleProjectionInvariants` to normalize existing projection data, narrow `events.event_time_zone_id` to `varchar(100)`, and add schedule rollup/local-minute check constraints.
- Updated `schemas/islamu-event.md` and `docs/DOMAIN.md` to document UTC source-of-truth scheduling, timezone validation, and DB guardrails.
- Implemented Phase 3.2 prayer-relative session scheduling hardening.
- Added exact `EventSessionIslamicAspect.ApplyScheduling(...)` invariants for `Fixed` vs `RelativeToPrayer` state, including `-180..180` offset bounds.
- Replaced permissive Islamic aspect DTO validation with shared FluentValidation rules used by standalone session create/update and event graph creation.
- Removed write-side AutoMapper reverse mapping for `EventSessionIslamicAspect` and routed create/update handlers through the domain method.
- Added migration `20260527213951_HardenPrayerRelativeSessionScheduling` to replace the old relative-start constraint with exact state, offset-range, and prayer-range checks.
- Updated `schemas/islamu-event.md` and `docs/DOMAIN.md` to document the fixed vs prayer-relative session aspect contract.
- Implemented Phase 3.3 same-room session overlap hard enforcement.
- Moved the same-room exclusion constraint source of truth into `EventSessionConfiguration` and `PostgresModelConstraintApplier`; generated migration files are intentionally not part of this slice because development migrations are regenerated frequently.
- Kept the existing validator/repository friendly checks, and mapped PostgreSQL exclusion violations (`23P01`) back to `RoomScheduleConflictException`.
- Added PostgreSQL integration tests proving same-room overlaps fail, adjacent sessions pass, different-room overlaps pass, and soft-deleted sessions release the room.

### COMPLETED THIS SLICE

- Phase 3.3 decision is now implemented as hard DB enforcement: active sessions cannot overlap in the same tenant/location/room.
- Constraint semantics are `[start_time, end_time)`, so back-to-back sessions where one ends exactly when the next starts are valid.
- Soft delete intentionally removes the row from the partial exclusion predicate, allowing room reuse after cancellation/removal while preserving audit history.
- Context7 official PostgreSQL docs were queried for GiST exclusion constraints, `tstzrange`, and `btree_gist`; EF Core/Npgsql docs were queried for model configuration and PostgreSQL extension metadata.
- Verification for Phase 3.3:
  - `dotnet build --configuration Release --verbosity quiet` passed.
  - `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build` passed 1039/1039.
  - `dotnet test Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build` passed 131/131.
  - `dotnet test Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build` passed 178/179 with the known skipped response-metadata test.

- Migration `20260527132704_ReplaceTenantMemberWithTenantUserRoleGrants` applies through the PostgreSQL integration-test path and the model snapshot now contains `tenant_user_role_grants`, not active `tenant_members`.
- New persistence tests prove PostgreSQL rejects a tenant grant with an instance/platform role and rejects a cross-tenant `TenantUser` reference.
- Application validators now distinguish `Role does not exist` from `Role must be a tenant-scoped role`, preserving previous API validation semantics while enforcing the new tenant-role scope rule.
- Onboarding and managed-provider provisioning now create/reuse `TenantUser` and create `TenantUserRoleGrant` records instead of writing role authority directly against global `User`.
- Public contract search shows no removed `TenantMemberDto`, `CreateTenantMemberDto`, `UpdateTenantMemberDto`, `/api/tenant-members`, or `islamuevent_tenant_member` surface remains. Remaining `TenantMember` hits are the intentional `RoleEnum.TenantMember` lookup value and Cerbos principal attribute name `tenantMemberships`.
- Verification for Phase 2.4:
  - `dotnet build --configuration Release --verbosity quiet` passed.
  - `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet` passed 1028/1028.
  - `dotnet test Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet` passed 295/295.
  - `dotnet test Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet` passed 178/179 with one known skipped response-metadata test.
  - `dotnet test Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet` passed 121/121.
  - `dotnet test Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/*/OpenApiDocument_*" --minimum-expected-tests 5 --no-progress` passed 17/17, including HAL embedded-item and non-empty detail schema invariants.
  - `dotnet test Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet` passed 1209/1210 with one known skipped component-accessibility test.
- Full `Event.API.IntegrationTests` was attempted and failed 38/1084 on unrelated existing seeded-data/auth/external-key paths; the Phase 2.4 OpenAPI invariant subset is green.

### NEXT

1. Start Phase 4.1: audit custom-property quota enforcement coverage across definitions, options, values, templates, projections, and rebuild paths.
2. Keep full production RLS rollout deferred until app-role/migration-role separation and admin/system reads are explicitly designed.
3. Continue to require explicit bypass reasons for any new cross-tenant persistence path.
4. Continue to use focused TUnit `--treenode-filter` slices for API integration evidence until the unrelated full-suite failures are fixed.

### BLOCKERS

- Tavily MCP remains unavailable. This is still not a code blocker, but final research claims must not state Tavily was used unless the connector appears and is actually queried.

## SESSION PROGRESS (2026-05-26 Europe/Brussels)

### COMPLETED

- Planning created.
- Current-state report completed from repository evidence.
- Context7 official documentation research completed for EF Core and PostgreSQL.
- External web research completed for multi-tenant data isolation context using OWASP and Microsoft Azure Architecture Center sources.
- Tavily MCP availability checked through tool discovery; not exposed in this session.

### IN PROGRESS

- Awaiting user review of implementation plan.

### NEXT

1. User reviews `enterprise-data-model-hardening-plan.md`, especially sections 2.5, 5, 6, and 13.
2. If Tavily MCP becomes available, rerun external research and update this context before implementation.
3. First implementation agent starts with Phase 0, then Phase 1 tenant isolation inventory.

### BLOCKERS

- Tavily MCP was requested by the user but was not available from the tool registry in this session. This is not a code blocker if the user accepts the available Context7 + external research baseline, but it should be resolved before claiming the research requirement is fully satisfied.

## Quick Resume

1. Read `enterprise-data-model-hardening-plan.md`.
2. Read `enterprise-data-model-hardening-tasks.md`.
3. Re-classify the implementation slice against `.claude/contract/intents.yaml`.
4. Load matching docs/rules/skills before editing.
5. Start from Phase 4.2 unless user instruction overrides it.
6. Do not add or regenerate migration files unless the user explicitly asks; this branch currently treats EF configuration/domain code plus schema docs as the data-model source of truth while migrations are regenerated during development.
7. Keep all three dev docs updated after each meaningful implementation slice.

## Research Notes

### Repository-First Findings

- `docs/ARCHITECTURE.md` confirms .NET 10, Clean Architecture, CQRS/MediatR, BFF, PostgreSQL/EF Core, named query filters, HAL authorization, and outbox background services.
- `docs/DOMAIN.md` confirms core aggregates, normalized lookup pattern, PII split tables, Layer 2 event aspects, governed custom properties, tenant/soft-delete interfaces, and outbox variants.
- `docs/MULTI_TENANCY.md` confirms tenant resolution order, single/multi-tenant modes, EF query filters, and tenant-local `TenantUser` / `TenantUserProfile` state.
- `docs/SECURITY-MODEL.md` now documents RLS as prototype-supported but not enabled on production tenant tables.
- `docs/OPERATIONS.md` confirms partitioning is not implemented and must not be documented as current behavior.
- `docs/CUSTOM_PROPERTIES.md` and ADR-006 confirm Layer 3 custom properties must not become a runtime schema engine.

### Context7 Findings

- EF Core docs confirm model-level query filters for soft delete and multi-tenancy, EF Core 10 named query filters, selective `IgnoreQueryFilters`, and optimistic concurrency tokens.
- PostgreSQL docs confirm `CREATE POLICY` / row-level security support, `USING` and `WITH CHECK` expressions, composite foreign keys, table constraints, check constraints, unique constraints, and exclusion constraints.
- Npgsql docs confirm pooled connections are reset on close by default; the RLS prototype uses EF Core `PooledDbContextFactory` and binds tenant state on each EF connection open instead of assuming a pooled session keeps state.
- 2026-05-27 recheck: Context7 confirmed EF Core composite relationships use `HasPrincipalKey` plus composite `HasForeignKey`, composite alternate keys use `HasAlternateKey`, and EF Core 10 named filters can be selectively disabled. PostgreSQL docs confirmed composite foreign keys, table constraints, and RLS `CREATE POLICY`/`WITH CHECK` syntax.
- 2026-05-27 RLS recheck: Context7 confirmed EF Core singleton interceptors should be stateless and can use event data context for scoped state. PostgreSQL docs confirmed superusers and `BYPASSRLS` roles bypass row security; table owners only become subject to RLS with `FORCE ROW LEVEL SECURITY`.

### External Research Findings

- OWASP Multi Tenant Security guidance supports database-level isolation such as RLS/schemas as defense-in-depth for multi-tenant applications.
- Microsoft Azure Architecture Center describes tenant isolation as a spectrum and notes shared databases rely on tenant identifiers and application logic, while RLS can add database-level tenant isolation.
- Microsoft Azure multitenant storage/data guidance recommends considering storage/data patterns and avoiding unbounded custom data modeling patterns that hurt tenant operability.

### Tavily MCP Status

- The user explicitly requested Tavily MCP.
- Tool discovery for `context7` succeeded.
- Tool discovery for `tavily`, `Tavily Search API MCP`, and `tavily-search` exposed no Tavily namespace; only Context7/GitHub/Jean tools were available.
- 2026-05-27 tool discovery was repeated; no Tavily namespace was exposed.
- Action: rerun Task 0.2 if Tavily becomes available.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `Explore.Domain/TenantUser.cs` | Existing | Domain | Tenant-local user lifecycle, status, moderation, actor/profile link | Aggregate root for tenant-local authority. |
| `Explore.Domain/TenantUserRoleGrant.cs` | New | Domain | Auditable tenant role grant rooted in `TenantUser` lifecycle state | Replaces the old tenant-member aggregate in both internal and public contracts. |
| `dev/active/enterprise-data-model-hardening/tenant-role-grant-model-decision.md` | New | Docs / architecture decision | Phase 2.1 tenant role grant model decision | Chooses `TenantUserRoleGrant`, with composite tenant FK, tenant-role-scope FK, revoke lifecycle, migration path, and API replacement direction. |
| `Explore.Persistence/Configurations/Entities/TenantUserConfiguration.cs` | Existing | Persistence | Tenant user table, unique tenant/user and tenant/actor indexes, status check | Adds alternate key `(TenantId, Id)` for grant ownership guardrails. |
| `Explore.Persistence/Configurations/Entities/TenantUserRoleGrantConfiguration.cs` | New | Persistence | Tenant role grant table/config with active uniqueness, role-scope check, composite FKs | Enforces tenant-local user ownership and tenant-only role assignments. |
| `Explore.Persistence/Repositories/TenantUserRoleGrantRepository.cs` | New/Modified | Persistence | Implements `ITenantUserRoleGrantRepository` | Uses reasoned tenant-filter bypass for user membership enumeration and active `TenantUser` subquery for authority safety. |
| `Explore.API/Controllers/TenantUserRoleGrantController.cs` | New/Modified | API | Public tenant role-grant endpoint surface | Exposes `/api/tenant-user-role-grants` list/detail/create/revoke with HAL resources. |
| `Explore.Infrastructure/Identity/AdminContext.cs` | Existing/Modified | Infrastructure | Resolves current admin authority | Depends on tenant role grant repository. |
| `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs` | Existing/Modified | Application | Creates default tenant administrator, active tenant user, and role grant | Uses `TenantUserRoleGrant` internally. |
| `Explore.Application/Features/ManagedProviderProvisioning/Handlers/Commands/EnsureManagedProviderClientProvisionedCommandHandler.cs` | Existing/Modified | Application | Creates tenant, user, actor, tenant user/profile/grant, external bindings | Keeps provider customer as tenant admin, not instance admin. |
| `Explore.Persistence/Migrations/20260527132704_ReplaceTenantMemberWithTenantUserRoleGrants.cs` | New | Persistence migration | Backfills and replaces `tenant_members` with `tenant_user_role_grants` | Fails fast on orphan memberships and non-tenant roles; downgrade refuses silent loss of revoked/multiple active grant state. |
| `Explore.Domain/Event.cs` | Existing | Domain | Event/program container | If modified, add required two-line ABOUTME header. |
| `Explore.Domain/EventSession.cs` | Existing | Domain | Scheduled child content with UTC/local projection fields | If modified, add required two-line ABOUTME header. |
| `Explore.Domain/EventSessionIslamicAspect.cs` | Existing | Domain | Prayer-relative Islamic session aspect | Has one ABOUTME line and needs two if touched. |
| `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs` | Existing | Persistence | Session time indexes and basic range constraints | Candidate for composite tenant FK and stronger temporal constraints. |
| `Explore.Domain/EventRoleAssignment.cs` | Existing | Domain | Event-scoped role assignment evidence | Coordinate with `event-scoped-operational-roles` workstream. |
| `Explore.Persistence/Services/EventAuthoritySnapshotService.cs` | Existing | Persistence | Batch effective event-role authority lookup | Must remain tenant-safe. |
| `Explore.Domain/EventCustomPropertyDefinition.cs` | Existing | Domain | Event-local Layer 3 definition | EAV governance hardening target. |
| `Explore.Domain/EventCustomPropertyValue.cs` | Existing | Domain | Event-local Layer 3 value | EAV lifecycle/retention target. |
| `Explore.Domain/EventCustomPropertyProjection.cs` | Existing | Domain | Derived query projection | Not source of truth. |
| `Explore.Persistence/Services/CustomPropertyQuotaResolver.cs` | Existing | Persistence | Resolves custom-property quotas from tenant/system/default settings | Enforcement coverage needs audit. |
| `Explore.Domain/ExternalBinding.cs` | Existing | Domain | Provider-neutral external/internal identity binding | Needs allowed type registry. |
| `Explore.Persistence/Configurations/Entities/ExternalBindingConfiguration.cs` | Existing | Persistence | External binding indexes and non-blank/status constraints | Strings are currently loosely governed. |
| `Explore.Domain/EmailDispatchOutbox.cs` | Existing | Domain | Specialized durable email-dispatch state | Coordinate with CRMWorx adaptation workstream. |
| `Explore.Domain/AuditLog.cs` | Existing | Domain | Tenant-scoped audit trail | Retention/partitioning/lifecycle classification target. |
| `Explore.Domain/Notification.cs` | Existing | Domain | Tenant-scoped user notification/deep-link record | Retention and polymorphic reference target. |
| `Explore.Persistence/ExploreDbContext.QueryFilters.cs` | Existing/Modified | Persistence | Named tenant and soft-delete query filters | Strict tenant filters now fail closed unless `TenantContext` is bound or an explicit bypass is enabled. |
| `Explore.Persistence/QueryFilters/TenantFilterBypassReasons.cs` | New | Persistence | Approved reason catalog for tenant-filter bypass calls | Used by repositories, workers, seeding, and tenant lookup paths to make cross-tenant reads reviewable. |
| `Event.Persistence.IntegrationTests/TenantIsolation/TenantQueryFilterFailClosedTests.cs` | New | Persistence tests | Missing-tenant fail-closed certification | Proves absent tenant returns no tenant rows, bound tenant returns only that tenant, and explicit bypass is required for system reads. |
| `Event.Architecture.Tests/PersistenceTenantFilterArchitectureTests.cs` | New | Architecture tests | Guardrails for query-filter bypass conventions | Blocks reintroducing `TenantContext == null ||` and direct runtime full-filter bypasses. |
| `Explore.Persistence/Security/PostgresTenantSessionInterceptor.cs` | New | Persistence | Sets PostgreSQL `app.current_tenant_id` when EF Core opens a connection | Prototype support only; runtime registration is disabled by default. |
| `Explore.Persistence/PersistenceServicesRegistration.cs` | Existing/Modified | Persistence DI | Registers pooled DbContext factory and optional tenant-session interceptor | `Persistence:EnableRlsTenantSession=true` opt-in adds the interceptor; no production RLS policies are enabled by this flag. |
| `Event.Persistence.IntegrationTests/TenantIsolation/PostgresTenantSessionRlsPrototypeTests.cs` | New | Persistence tests | Synthetic forced-RLS proof through non-superuser role | Proves tenant A, tenant B, and missing-tenant behavior using `app.current_tenant_id`. |
| Event graph EF configurations | Existing/Modified | Persistence | Tenant-scoped alternate keys and composite FKs | Phase 1.2 uses the existing `Group` composite-FK pattern for event/session/day/group/registration/taxonomy/contact-share relationships. |
| `Explore.Domain/EventRegistration.cs` | Existing/Modified | Domain | Registration access row now carries `EventId` | Enables DB enforcement that registration, intent, and session belong to the same tenant/event boundary. |
| `Explore.Persistence/Migrations/20260527092407_AddEventGraphTenantForeignKeys.cs` | New | Persistence migration | Adds event graph composite FK guardrails and backfills `event_registrations.event_id` | Applies cleanly in the PostgreSQL integration test container. |
| `Event.Persistence.IntegrationTests/Repositories/EventGraphTenantForeignKeyTests.cs` | New | Persistence tests | Negative tests for cross-tenant/cross-event FK violations | Proves PostgreSQL rejects representative invalid graph writes. |
| `dev/active/enterprise-data-model-hardening/tenant-scoped-fk-inventory.md` | New/Modified | Docs/Persistence planning | Classifies 69 strict tenant entities, tenant-keyed exceptions, FK risk categories, and completed event graph composite-FK scope | Source of truth for remaining FK-hardening boundaries. |
| `schemas/islamu-event.md` | Existing/Modified | Docs | DBML schema reference | Updated for event graph alternate keys, composite FKs, `event_registrations.event_id`, and `tenant_user_role_grants`. |

## Key Decisions

1. **Plan name:** `enterprise-data-model-hardening`.
2. **Implementation posture:** breaking changes are allowed, but still require migrations, tests, docs, and controlled rollout.
3. **Primary first slice:** tenant isolation inventory before writing migrations.
4. **Tenant authority target:** replace `TenantMember` with `TenantUserRoleGrant`, an auditable child of `TenantUser`.
5. **RLS posture:** prototype-supported defense-in-depth candidate, not a replacement for application authorization; production table policies remain deferred.
6. **Partitioning posture:** not current behavior; implement only with runbooks/tests or keep as threshold-based capacity plan.
7. **EAV posture:** custom properties remain Layer 3 governed extensions, not runtime entities.
8. **First schema-hardening scope:** event graph composite tenant FKs before tenant-role consolidation, EAV guardrails, or production RLS rollout.
9. **RLS role requirement:** runtime RLS must use a non-superuser, non-`BYPASSRLS` app role; migration/maintenance roles stay separate.
10. **Query-filter gap policy:** strict `ITenantEntity` classes must be registered in named tenant filters; nullable/global hybrid tenant rows need explicit exception decisions rather than automatic promotion.
11. **Tenant-filter execution policy:** missing `TenantContext` fails closed; system/admin paths must opt in through explicit bypass reasons and bounded predicates.

## Constraints And Rules To Remember

- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- Domain stays dependency-free.
- Application cannot use `ExploreDbContext`.
- `Guid` for aggregates, `int` for lookups, `long` for cursors.
- GET endpoints are anonymous; writes authorized.
- HAL `_links` gate UI action affordances.
- `IgnoreTenantFilter(reason)` / `IgnoreQueryFilters()` must be reasoned, constrained, and tested.
- Applied migrations are not rewritten.
- Every new/modified file starts with two `ABOUTME:` lines.

## Validation Baseline

Before implementation completion, expect at minimum:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Add these when touched:

```bash
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

## Current Known Risks / Unknowns

- RLS tenant-session behavior is proven in a bounded prototype, but production rollout still needs app-role/migration-role separation and admin/system-path tests.
- Composite tenant FKs may reveal existing invalid data.
- Tenant role public contract replacement is now complete and intentionally breaking; downstream API clients must migrate to tenant-user-role-grant route/DTO names.
- Future API changes may require Blazor/client regeneration and HAL policy updates.
- Tavily MCP research remains unresolved.
- Event graph constraints now fail fast if registration rows cannot backfill `event_id`; other future composite-FK families can still reveal existing invalid data.
- Tenant-filter bypasses are now explicit in code, but future host-admin/API paths still need endpoint-level authorization and HAL contract review as they are implemented.
- Full API integration is not green due unrelated existing seeded-data/auth/external-key failures; use focused TUnit slices as implementation evidence until that suite is repaired.

## Handoff Notes

### Handoff — 2026-05-26 Europe/Brussels

- **Current state:** Planning docs created; no implementation code changed by this workstream.
- **Next action:** User reviews plan. If approved, start Task 1.1 tenant-scoped entity/FK inventory.
- **Blockers:** Tavily MCP unavailable in this session.
- **Modified files:** `dev/active/enterprise-data-model-hardening/enterprise-data-model-hardening-plan.md`, `enterprise-data-model-hardening-context.md`, `enterprise-data-model-hardening-tasks.md`.
- **Validation:** Docs-only change. No build/test run for this planning step.
- **Documentation impact:** New dev-doc workstream only.
- **Risks:** Tenant authority consolidation and RLS are the highest-risk slices.
- **Notes for next contributor/agent:** Do not start migrations before the FK inventory and tenant role model decision are documented.

### Handoff — 2026-05-27 Europe/Brussels

- **Current state:** Phase 1.1 is complete. The inventory file exists, strict tenant filter gaps are corrected, and Phase 1.2 event graph composite FK scope is selected.
- **Next action:** Implement event graph composite tenant-safe keys/FKs with integration tests and update `schemas/islamu-event.md`.
- **Blockers:** Tavily MCP unavailable; RLS/session-variable design still needs a later prototype.
- **Modified files:** `Explore.Persistence/ExploreDbContext.QueryFilters.cs`, `dev/active/enterprise-data-model-hardening/tenant-scoped-fk-inventory.md`, `enterprise-data-model-hardening-plan.md`, `enterprise-data-model-hardening-context.md`, `enterprise-data-model-hardening-tasks.md`.
- **Validation:** Baseline and post-edit build passed. Architecture tests passed 176 succeeded / 1 skipped. Persistence integration tests passed 110 succeeded. `git diff --check` passed.
- **Documentation impact:** Dev docs now reflect active implementation and first migration boundaries.
- **Risks:** Composite FKs may reveal bad existing data; `EventRegistration` has convention-mapped FKs that should be made explicit before tenant-safe conversion.

### Handoff — 2026-05-27 Europe/Brussels — Phase 1.2

- **Current state:** Phase 1.2 is complete. The event graph now has database-enforced tenant/event consistency through EF Core alternate keys and composite foreign keys.
- **Next action:** Start Phase 1.3 RLS decision/prototype, or explicitly defer RLS with ADR-quality reasoning before moving to Phase 1.4 runtime tenant-filter bypass hardening.
- **Blockers:** Tavily MCP remains unavailable in this environment. No code blocker remains for the completed event-graph guardrail slice.
- **Modified files:** event graph EF configuration files, `Explore.Domain/EventRegistration.cs`, registration command/repository/profile/test fixtures, `20260527092407_AddEventGraphTenantForeignKeys`, `ExploreDbContextModelSnapshot.cs`, `EventGraphTenantForeignKeyTests.cs`, `schemas/islamu-event.md`, and this workstream's dev docs.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed with existing warnings. `Event.Persistence.IntegrationTests` passed 116/116. `Event.Architecture.Tests` passed 176/177 with 1 skipped. `git diff --check` passed.
- **Documentation impact:** `schemas/islamu-event.md`, plan, context, tasks, and FK inventory now reflect the implemented Phase 1.2 model.
- **Risks:** DBML syntax was manually reviewed against the generated migration/snapshot; no automated schema parser was found. Optional composite relationships now use `Restrict` instead of `SetNull`, which is intentional but can affect delete workflows.

### Handoff — 2026-05-27 Europe/Brussels — Phase 1.3

- **Current state:** Phase 1.3 is complete as a bounded RLS prototype. A stateless EF Core `DbConnectionInterceptor` can set PostgreSQL `app.current_tenant_id` on connection open, and production table policies remain disabled/deferred.
- **Next action:** Start Phase 1.4 runtime tenant-filter bypass hardening.
- **Blockers:** Tavily MCP remains unavailable. Production RLS rollout is intentionally blocked on app-role/migration-role separation and explicit host-admin/system read design.
- **Modified files:** `Explore.Persistence/Security/PostgresTenantSessionInterceptor.cs`, `Explore.Persistence/PersistenceServicesRegistration.cs`, `Event.Persistence.IntegrationTests/TenantIsolation/PostgresTenantSessionRlsPrototypeTests.cs`, `docs/SECURITY-MODEL.md`, `docs/MULTI_TENANCY.md`, `docs/CONFIGURATION.md`, and this workstream's dev docs.
- **Validation:** `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed 117/117 after the RLS prototype test was added. `dotnet build --configuration Release --verbosity quiet` passed with existing warnings. `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed 176/177 with 1 skipped. `git diff --check` passed.
- **Documentation impact:** Security, multi-tenancy, and configuration docs now distinguish prototype support from production RLS enablement.
- **Risks:** The prototype test showed why runtime role design matters: PostgreSQL superusers bypass RLS even when a table uses `FORCE ROW LEVEL SECURITY`; production runtime must use a non-superuser/non-`BYPASSRLS` app role.

### Handoff — 2026-05-27 Europe/Brussels — Phase 1.4

- **Current state:** Phase 1.4 is complete. Tenant query filters now fail closed without an ambient tenant, and cross-tenant persistence paths must use explicit bypass reasons.
- **Next action:** Start Phase 2.1 tenant role grant model decision; do not change tenant membership APIs until the target role-grant shape is documented.
- **Blockers:** Tavily MCP remains unavailable. No code blocker remains for tenant-filter bypass hardening.
- **Modified files:** `Explore.Persistence/ExploreDbContext.cs`, `Explore.Persistence/ExploreDbContext.QueryFilters.cs`, `Explore.Persistence/QueryFilters/QueryFilterExtensions.cs`, `Explore.Persistence/QueryFilters/TenantFilterBypassReasons.cs`, tenant-aware repositories/services, `Explore.Persistence/Seed/DatabaseSeeder.cs`, persistence fixtures, `TenantQueryFilterFailClosedTests.cs`, `PersistenceTenantFilterArchitectureTests.cs`, `docs/MULTI_TENANCY.md`, `docs/SECURITY-MODEL.md`, `docs/CODEBASE_INSIGHTS.md`, and this workstream's dev docs.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed with existing warnings. `Event.Persistence.IntegrationTests` passed 119/119. `Event.Architecture.Tests` passed 178/179 with 1 skipped.
- **Documentation impact:** Multi-tenancy, security, and codebase-insight docs now describe fail-closed tenant filters and explicit bypass reasons.
- **Risks:** Future host-admin/API surfaces still need endpoint authorization, HAL affordance, and API-contract work when those flows become user-facing. Test fixtures use explicit system bypasses for repository-level setup; tenant-isolation tests must use tenant-filtered contexts.

### Handoff — 2026-05-27 Europe/Brussels — Phase 2.1

- **Current state:** Phase 2.1 is complete. The target tenant role model is `TenantUserRoleGrant`, replacing `TenantMember` with auditable create/revoke grants rooted in `TenantUser`.
- **Next action:** Start Phase 2.2: add the domain entity/config/DbSet/repository contract, create the backfill/drop migration, update `schemas/islamu-event.md`, and add persistence tests for cross-tenant and wrong-role-scope rejection.
- **Blockers:** Tavily MCP remains unavailable. No design blocker remains for the tenant role grant shape.
- **Modified files:** `dev/active/enterprise-data-model-hardening/tenant-role-grant-model-decision.md`, `enterprise-data-model-hardening-plan.md`, `enterprise-data-model-hardening-context.md`, and `enterprise-data-model-hardening-tasks.md`.
- **Validation:** Docs/decision slice only. Context7 official EF Core docs were queried for alternate keys, composite FKs, and unique-index distinction. Code build/tests from Phase 1.4 remain the latest compiled verification.
- **Documentation impact:** Dev docs now point the next agent to Phase 2.2 rather than asking them to re-decide the role grant model.
- **Risks:** The migration should fail fast on orphan tenant memberships or non-tenant-scoped tenant-member roles; do not silently create authority rows unless the data issue is explicit and tested.

### Handoff — 2026-05-27 Europe/Brussels — Phase 2.2/2.3

- **Current state:** Phase 2.2 and 2.3 are complete internally. `TenantUserRoleGrant` replaces `TenantMember` in Domain/Persistence, admin authority checks, tenant onboarding, managed-provider provisioning, seed data, repository contracts, mapping, validators, and unit/integration tests.
- **Next action:** Start Phase 2.4. Replace the public `TenantMember` API/HAL/Cerbos/Blazor contract with explicit tenant role grant names and semantics, regenerate/verify OpenAPI, update API docs/changelog, and run API/authorization/UI tests.
- **Blockers:** Tavily MCP remains unavailable. No internal domain/persistence blocker remains for tenant role grants.
- **Modified files:** `Explore.Domain/TenantUserRoleGrant.cs`, `Explore.Domain/TenantUser.cs`, deleted `Explore.Domain/TenantMember.cs`, `Explore.Persistence/Configurations/Entities/TenantUserRoleGrantConfiguration.cs`, deleted `TenantMemberConfiguration.cs`, `Explore.Persistence/ExploreDbContext.DbSets.cs`, `Explore.Persistence/ExploreDbContext.QueryFilters.cs`, `Explore.Persistence/Repositories/TenantUserRoleGrantRepository.cs`, `Explore.Persistence/Migrations/20260527132704_ReplaceTenantMemberWithTenantUserRoleGrants.cs`, seed files, tenant onboarding/provisioning handlers, tenant role-grant CQRS bridge files, `AdminContext.cs`, tests, `schemas/islamu-event.md`, canonical docs, and this workstream's dev docs.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed with existing warnings. `Event.Persistence.IntegrationTests` passed 121/121. `Event.Application.UnitTests` passed 1036/1036. `Explore.Infrastructure.Tests` passed 295/295. `Event.Architecture.Tests` passed 178/179 with 1 skipped. `git diff --check` passed.
- **Documentation impact:** Schema and canonical docs now describe `TenantUserRoleGrant` as the internal tenant authority model. Dev docs now point the next agent to Phase 2.4 public contract replacement.
- **Risks:** Superseded by the Phase 2.4 handoff below: the public API/OpenAPI/HAL bridge was removed in the next slice.

### Handoff — 2026-05-27 Europe/Brussels — Phase 2.4

- **Current state:** Phase 2.4 is complete. Public API/HAL/Cerbos/OpenAPI/Blazor-client contracts now expose tenant role authority as `TenantUserRoleGrant`.
- **Next action:** Start Phase 3.1 event/session time invariant hardening.
- **Blockers:** Tavily MCP remains unavailable. Full API integration remains noisy because of unrelated existing seeded-data/auth/external-key failures; focused OpenAPI invariants passed.
- **Modified files:** tenant role grant DTOs, validators, CQRS handlers/requests, `TenantUserRoleGrantController`, HAL route names/assembler/policy/registration, Cerbos policy/schema/resource descriptors/actions, fallback authorization mappings, managed-provider provisioning result DTOs, `ExploreJsonContext`, generated OpenAPI schemas, generated NSwag client, canonical API docs/changelog, tests, and this workstream's dev docs.
- **Validation:** Release build passed. Application unit tests passed 1028/1028. Infrastructure tests passed 295/295. Architecture tests passed 178/179 with 1 known skip. Persistence integration tests passed 121/121. Focused API OpenAPI invariants passed 17/17. Blazor client tests passed 1209/1210 with 1 known skip.
- **Documentation impact:** Plan/context/tasks, `docs/API.md`, and `docs/API_CHANGELOG.md` now describe the breaking tenant role grant API contract.
- **Risks:** API consumers using `/api/tenant-members` or tenant-member DTO names must migrate. The remaining `TenantMember` string in OpenAPI/client is the role enum value, not the removed aggregate/API contract.

### Handoff — 2026-05-27 Europe/Brussels — Phase 3.1

- **Current state:** Phase 3.1 is complete. UTC start/end instants are the schedule source of truth; local schedule columns are server-owned projections generated through domain scheduling methods.
- **Next action:** Superseded by the Phase 3.2 handoff below.
- **Blockers:** Tavily MCP remains unavailable. No schedule-source-of-truth blocker remains.
- **Modified files:** event aggregate/scheduled child domain methods, schedule projection services, event update handlers, event/session/agenda mapping profiles, event repository schedule graph loading, event/session/agenda EF configurations, migration `20260527204604_FormalizeScheduleProjectionInvariants`, schedule tests, `docs/DOMAIN.md`, `schemas/islamu-event.md`, and this workstream's dev docs.
- **Validation:** Release build passed. Domain unit tests passed 242/242. Application unit tests passed 1029/1029. Persistence integration tests passed 123/123. Architecture tests passed 178/179 with 1 known skipped response-metadata test. Scoped `git diff --check` for Phase 3.1 files passed.
- **Documentation impact:** Canonical domain and DBML schema docs now state the UTC-source/local-projection rule, timezone normalization behavior, and database check constraints.
- **Risks:** Full `git diff --check` still reports trailing whitespace in a pre-existing generated Blazor client diff outside Phase 3.1. Timezone aliases canonicalize according to .NET/system timezone data, so self-hosting environments should run with ICU/globalization support enabled.

### Handoff — 2026-05-27 Europe/Brussels — Phase 3.2

- **Current state:** Phase 3.2 is complete. `EventSessionIslamicAspect` now has domain-owned scheduling invariants for exact fixed/prayer-relative states, app validation rejects invalid DTO graphs before handlers run, and EF/PostgreSQL constraints enforce the same state shape and ranges.
- **Next action:** Start Phase 3.3. Decide whether same-room/resource session overlaps are hard persistence constraints or business-warning semantics, then document and test the selected behavior.
- **Blockers:** Tavily MCP remains unavailable in the tool registry. No prayer-relative scheduling blocker remains.
- **Modified files:** `Explore.Domain/EventSessionIslamicAspect.cs`, event-session Islamic aspect validators, event/session create/update handlers, `EventSessionMappingProfile`, `EventSessionIslamicAspectConfiguration`, migration `20260527213951_HardenPrayerRelativeSessionScheduling`, migration snapshot, domain/application/persistence tests, `docs/DOMAIN.md`, `schemas/islamu-event.md`, and this workstream's dev docs.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed with existing warnings. `Event.Domain.UnitTests` passed 247/247. `Event.Application.UnitTests` passed 1039/1039. `Event.Persistence.IntegrationTests` passed 127/127. `Event.Architecture.Tests` passed 178/179 with the known skipped response-metadata test.
- **Documentation impact:** Canonical domain and DBML schema docs now state the fixed vs prayer-relative session aspect contract and list the replacement DB constraints.
- **Risks:** The migration clears stale fixed-session prayer fields because fixed sessions use UTC schedule fields as authoritative. Existing relative rows with invalid offset or prayer enum values fail migration preflight instead of being silently repaired.

### Handoff — 2026-05-27 Europe/Brussels — Phase 3.3

- **Current state:** Phase 3.3 is complete. Same-room session overlaps are hard database conflicts through `EX_EventSession_RoomNoOverlap`, scoped to active rows in the same tenant/location/room.
- **Next action:** Start Phase 4.1 custom-property quota enforcement audit.
- **Blockers:** Tavily MCP remains unavailable in the tool registry. No schedule overlap blocker remains.
- **Modified files:** event-session EF configuration, model-owned PostgreSQL constraint applier, migration service worker, test fixture, event-session repository contract/implementation, generic repository virtual hooks, room conflict exception, session validators, `SchedulingConstraintTests`, `docs/DOMAIN.md`, `schemas/islamu-event.md`, and this workstream's dev docs.
- **Validation:** Release build passed. Application unit tests passed 1039/1039. Persistence integration tests passed 131/131. Architecture tests passed 178/179 with the known skipped response-metadata test.
- **Follow-up validation 2026-05-28:** Release build passed after moving the exclusion constraint into EF model metadata. Architecture tests passed 178/179 with the known skipped response-metadata test. Full persistence integration tests reached 132/134 succeeded and failed only `EmailDispatchOutboxTransitionRepositoryTests.TryReplayForOperatorResetsDeferredRowToPending` and `TryParkForOperatorMarksEligibleRowAsParked`, which are outside the scheduling constraint path.
- **Documentation impact:** Canonical domain and DBML schema docs now state that active same-room session overlaps are DB-enforced; adjacent ranges and soft-delete release semantics are explicit.
- **Risks:** Existing production/self-host data with overlapping active room assignments will fail the model-owned preflight and must be corrected intentionally before applying the constraint.

### Handoff — 2026-05-28 Europe/Brussels — Phase 3.3 Runtime Fix And Session Handoff

#### Current State

- What is completed: Phase 3.3 remains complete as code-owned same-room overlap enforcement. Runtime Development startup no longer fails on EF pending-model drift while migration files are intentionally absent/regenerated.
- What is in progress: No implementation task is active. This is a handoff checkpoint before Phase 4.
- What changed since the last handoff: `PendingModelChangesWarning` was handled in Development runtime DbContext configuration, API startup migrations now invoke `PostgresModelConstraintApplier`, and leftover untracked migration files were removed.

#### Next Action

1. Start Phase 4.1 custom-property quota enforcement audit.
2. Build an audit matrix for definitions, options, values, projections, dirty scopes, templates, rebuilds, and admin/API paths.
3. Convert each discovered gap into exact Phase 4.2/4.3 implementation tasks before editing handlers broadly.

#### Blockers

- Tavily MCP remains unavailable; do not claim Tavily research was performed.
- Full `Event.Persistence.IntegrationTests` currently has two unrelated email-dispatch transition failures. They are not blockers for Phase 4.1 audit work, but they block claiming the full persistence suite is green.

#### Modified Files

- `Event.MigrationService/Program.cs` — Development-only suppression of EF pending-model warning for runtime migrations.
- `Event.MigrationService/Worker.cs` — applies model-owned PostgreSQL constraints after EF migrations.
- `Explore.API/Program.cs` — passes host environment to persistence registration and applies model-owned PostgreSQL constraints after API startup/admin migrations.
- `Explore.Persistence/PersistenceServicesRegistration.cs` — Development-only runtime pending-model warning suppression and environment-name parameter.
- `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs` — model-owned same-room overlap exclusion metadata.
- `Explore.Persistence/Schema/PostgresExclusionConstraintExtensions.cs` — EF model annotation API for PostgreSQL exclusion constraints.
- `Explore.Persistence/Schema/PostgresModelConstraintApplier.cs` — idempotent PostgreSQL extension/constraint applier with preflight conflict detection.
- `Event.Persistence.IntegrationTests/Fixtures/PostgreSqlContainerFixture.cs` — applies model-owned constraints after migrations.
- `Event.Persistence.IntegrationTests/Repositories/SchedulingConstraintTests.cs` — same-room overlap, adjacency, different-room, and soft-delete release coverage.
- `docs/DOMAIN.md`, `schemas/islamu-event.md` — document same-room overlap enforcement and model-owned constraint application.
- `dev/active/enterprise-data-model-hardening/*` — refreshed handoff, plan baseline, and task status.

#### Validation

- Commands run:
  - `dotnet build --configuration Release --verbosity quiet` — passed on 2026-05-28 with existing warning volume.
  - `dotnet test Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build` — passed 178/179 with the known skipped response-metadata test.
  - `git diff --check -- <scoped Phase 3.3/data-model files>` — passed.
- Commands still needed:
  - Phase 4.1 audit is docs/matrix-first; run targeted build/tests once it moves from audit to code changes.
  - Full `Event.Persistence.IntegrationTests` should be rerun after unrelated email-dispatch transition failures are fixed.

#### Documentation Impact

- Updated this workstream's plan/context/tasks to reflect the migration-file policy, current validation, and next Phase 4.1 slice.
- Canonical domain/schema docs were already updated for Phase 3.3 behavior.

#### Risks

- Source-grounding risks: Older handoff notes mention generated migration filenames that are no longer current files; treat them as historical evidence only.

### Handoff — 2026-05-28 Europe/Brussels — Phase 4.1 Quota Audit

#### Current State

- What is completed: Phase 4.1 is complete as a documentation and code-path audit. The quota registry, enforcement matrix, exact gaps, and recommended hardening order are recorded in `custom-property-quota-enforcement-audit.md`.
- What is in progress: No implementation task is active. Phase 4.2 should start from the gaps below.
- What changed since the last handoff: `docs/CUSTOM_PROPERTIES.md` now states which quota paths are covered and which template/template-sync paths remain weaker than direct runtime writes.

#### Next Action

1. Patch event template create/update to enforce `max_options_per_definition`.
2. Patch event-session template create/update to enforce `max_definitions_per_template` and `max_options_per_definition`.
3. Patch event/session template sync apply to preflight resulting runtime definition counts and per-definition option counts before writes.
4. Add focused Application unit tests for all quota failures before moving to broader EAV lifecycle/promotion controls.

#### Blockers

- Tavily MCP remains unavailable; do not claim Tavily research was performed.
- Full `Event.Persistence.IntegrationTests` still has unrelated email-dispatch transition failures from the previous handoff and should not be claimed green until rerun/fixed.

#### Modified Files

- `dev/active/enterprise-data-model-hardening/custom-property-quota-enforcement-audit.md` — Phase 4.1 audit matrix updated with the Phase 4.2 quota closure notes.
- `docs/CUSTOM_PROPERTIES.md` — canonical quota enforcement summary updated to list template and template-sync coverage.
- `dev/active/enterprise-data-model-hardening/enterprise-data-model-hardening-plan.md` — current state updated to show the Phase 4.2 quota sub-slice is complete.
- `dev/active/enterprise-data-model-hardening/enterprise-data-model-hardening-tasks.md` — next recommended slice moved to semantic lifecycle/promotion hardening.
- `dev/active/enterprise-data-model-hardening/enterprise-data-model-hardening-context.md` — current handoff updated.

#### Validation

- Commands run:
  - `dotnet build --configuration Release --verbosity quiet` — passed before Phase 4.1 edits with existing warnings.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed 178/179 with the known skipped response-metadata test.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` — passed 1057/1057 after the Phase 4.2 quota sub-slice.
  - `git diff --check -- dev/active/enterprise-data-model-hardening docs/CUSTOM_PROPERTIES.md` — passed.
- Commands still needed:
  - Semantic Phase 4.2 lifecycle changes should run focused application/API tests once implemented.

#### Documentation Impact

- The active audit file is the source of truth for completed Phase 4.2 quota hardening.
- Canonical custom-property docs now state that direct runtime, template, and template-sync cardinality quotas are aligned.

#### Risks

- Superseded by the Phase 4.3 handoff: operator-facing projection/quota/purge signals are complete, and the full Release build plus full application-unit baseline are green again.
- Test risk that remains: event/session PostgreSQL custom-property tests are blocked by current migration drift (`events.content` missing). Avoid reporting full persistence-suite success until that is fixed and rerun.
- Operator/release risks: Existing overlapping active room assignments will fail the constraint applier preflight and require intentional data cleanup.

#### Notes For Next Contributor Or Agent

- Required docs/rules to read: `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/CUSTOM_PROPERTIES.md`, ADR-006, `docs/DOMAIN.md`, `.claude/rules/application-layer.md`, `.claude/rules/efcore-persistence.md`.
- Assumptions made: Development-mode runtime migrations may tolerate model drift because the user regenerates migrations; non-Development environments should remain stricter.
- Do not touch / unrelated dirty files: the worktree has many unrelated modified/deleted/untracked files, including generated Blazor client whitespace and broad API/auth/email-dispatch work. Scope future diffs and cleanup to the active slice.

### Handoff — 2026-05-28 Europe/Brussels — Phase 4.2 Purge/Retire Lifecycle

#### Current State

- What is completed: Phase 4.2 is complete across quota, semantic reservation, and purge/retire lifecycle controls. Normal delete is retirement and keeps the machine key reserved while historical rows exist; audited hard purge is dependency-free only and releases the key only after repository-level rechecks.
- What is in progress: No implementation task is active. Phase 4.3 is the next slice.
- What changed since the last handoff: purge command handlers now return the same structured blocked response when a preflight becomes stale, repositories re-check dependencies inside `PurgeDefinition`, EF model relationships from definitions to values/projections are restrictive, and `docs/CUSTOM_PROPERTIES.md` documents the lifecycle contract.

#### Next Action

1. Start Phase 4.3 operator-facing projection/quota/purge signals.
2. Add safe metrics/logging/report fields without raw property-name cardinality.
3. Document operator triage paths in `docs/OPERATIONS.md`.

#### Blockers

- Superseded by the Phase 4.3 handoff: full Release build and full `Event.Application.UnitTests` now pass.
- Event/session PostgreSQL custom-property tests are blocked by migration drift: PostgreSQL reports `column "content" of relation "events" does not exist` during seed.

#### Modified Files

- `Explore.Application/Features/CustomProperties/CustomPropertyPurgeResponseFactory.cs` — shared blocked-response helper for purge races.
- `Explore.Application/Features/*CustomPropertyDefinitions/Handlers/Commands/Purge*DefinitionCommandHandler.cs` — stale-preflight handling before audit emission.
- `Explore.Persistence/Repositories/*CustomProperty*Repository.cs` — dependency rechecks inside hard purge.
- `Explore.Persistence/Configurations/Entities/*CustomProperty*Configuration.cs` — restrictive value/projection delete behavior for definition relationships.
- `Event.Application.UnitTests/Features/CustomPropertyDefinitions/Commands/PurgeCustomPropertyDefinitionCommandHandlerTests.cs` and `Event.Persistence.IntegrationTests/Repositories/CustomPropertyOptionLifecyclePostgreSqlTests.cs` — focused race/guard coverage.
- `docs/CUSTOM_PROPERTIES.md` and active workstream docs — lifecycle and validation status.

#### Validation

- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-progress --treenode-filter "/*/*/PurgeCustomPropertyDefinitionCommandHandlerTests/*" --minimum-expected-tests 3` — passed 3/3.
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet --no-progress --treenode-filter "/*/*/*/SharedPurgeDefinition_OnPostgreSql_ReturnsFalseWhenValuesExistAndKeepsRows" --minimum-expected-tests 1` — passed 1/1.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --no-progress` — passed 178/179 with the known skipped response-metadata test.
- `git diff --check -- <scoped Phase 4.2 files>` — passed.
- `dotnet build --configuration Release --verbosity quiet` — initially failed on unrelated generated Blazor client test anonymous type mismatches in `CustomPropertyAdminServiceTests`; later Phase 4.3 verification passed the full Release build.

#### Documentation Impact

- Canonical custom-property docs now define retirement vs hard purge, repository and handler dependency checks, restrictive EF delete behavior, and the decision that tenant-local-to-sector promotion remains a typed Layer 2 schema change rather than a runtime privileged mutation.

#### Risks

- No migration was generated for the EF delete-behavior changes by design; this branch treats migrations as regenerated artifacts unless explicitly requested.
- The next contributor should avoid coupling Phase 4.3 telemetry to raw namespace/key values because that would create high-cardinality metrics.

### Handoff — 2026-05-28 Europe/Brussels — Phase 4.3 Operator Signals

#### Current State

- What is completed: Phase 4.3 is complete. Projection status responses now include bounded operator fields (`PendingDirtyScopeCount`, `OperationalState`, `RequiresOperatorAction`, `RecommendedAction`), projection rebuild quota rejections emit metrics, hard-purge decisions emit bounded business metrics, and operations docs list triage states plus metric dimensions.
- What is in progress: No implementation task is active. Phase 5.1 is the next slice.
- What changed since the last handoff: status query handlers count pending dirty scopes and derive actionable states; projection metrics gained `explore.projections.quota_exceeded_total`; business metrics gained `explore.custom_properties.purge_decisions`; purge handlers record purged/blocked/failed decisions without recording namespace/key or purge reasons.

#### Next Action

1. Start Phase 5.1 polymorphic reference registry.
2. Define allowed target kinds, ID type, tenant scope, delete behavior, and type-pairing rules before adding persistence changes.
3. Keep migration files out of scope unless explicitly requested.

#### Blockers

- No build or application-unit blocker remains for this slice.
- Event/session PostgreSQL custom-property tests remain blocked by migration drift: PostgreSQL reports `column "content" of relation "events" does not exist` during seed.

#### Modified Files

- `Explore.Application/DTOs/CustomPropertyProjection/ProjectionStatusDto.cs` — added bounded operator-facing status fields.
- `Explore.Application/Features/CustomProperties/CustomPropertyProjectionStatusSignals.cs` — centralizes status-to-action mapping.
- `Explore.Application/Features/EventCustomPropertyProjections/Handlers/Queries/GetEventCustomPropertyProjectionStatusQueryHandler.cs` and session equivalent — count pending dirty scopes and apply signals.
- `Explore.Application/Telemetry/ProjectionMetrics.cs` — projection quota rejection counter.
- `Explore.Application/Telemetry/BusinessMetrics.cs` plus shared/event/session purge handlers — hard-purge decision counter with bounded blocker category.
- `Event.Application.UnitTests/Features/EventCustomPropertyProjections/Queries/GetCustomPropertyProjectionStatusQueryHandlerTests.cs` — focused operator-signal coverage.
- `docs/OPERATIONS.md`, `docs/CUSTOM_PROPERTIES.md`, and active workstream docs — metric and triage documentation.

#### Validation

- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-progress --treenode-filter "/*/*/GetCustomPropertyProjectionStatusQueryHandlerTests/*" --minimum-expected-tests 2` — passed 2/2.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-progress --treenode-filter "/*/*/RebuildEventCustomPropertyProjectionCommandHandlerTests/*" --minimum-expected-tests 7` — passed 7/7.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-progress --treenode-filter "/*/*/RebuildEventSessionCustomPropertyProjectionCommandHandlerTests/*" --minimum-expected-tests 3` — passed 3/3.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-progress` — passed 1065/1065.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --no-progress` — passed 178/179 with the known skipped response-metadata test.
- `dotnet build --configuration Release --verbosity quiet` — passed with existing warnings.
- `git diff --check -- <scoped Phase 4.3 files>` — passed.

#### Documentation Impact

- Operations docs now tell operators how to interpret `healthy`, `dirty_backlog_pending`, `rebuilding`, `rebuild_stale`, and `failed`.
- Metric documentation explicitly forbids raw custom-property namespace/key, display names, resource IDs, and purge reasons as metric dimensions.

#### Risks

- Phase 4.3 deliberately does not add a runtime promotion workflow. The existing governance report remains the operator surface for promotion candidates; actual promotion remains a typed Layer 2 schema change.
