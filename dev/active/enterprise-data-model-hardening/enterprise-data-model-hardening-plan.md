<!-- ABOUTME: Strategic implementation plan for enterprise-grade ISLAMU Event data-model hardening. -->
<!-- ABOUTME: Converts CTO data-model feedback into repo-grounded phases, constraints, risks, and verification gates. -->

# Enterprise Data Model Hardening — Implementation Plan

Last Updated: 2026-05-28 Europe/Brussels

## 0. Planning Metadata

- **Request:** Convert the CTO data-model report into a repository-grounded implementation plan for ISLAMU Event. The user explicitly allows breaking changes because the product is still in development.
- **Task directory:** `dev/active/enterprise-data-model-hardening/`
- **Planning status:** In implementation
- **Matched intents:** Multi-intent workstream. No single `.claude/contract/intents.yaml` entry covers this breadth, so implementation slices must re-classify before editing. Closest intents are:
  - `add-ef-migration` — schema/domain changes; docs `docs/QUICK_REFERENCE.md`, `docs/DOMAIN.md`; skill `dotnet-efcore-guidelines`; rule `.claude/rules/efcore-migrations.md`; paths `Explore.Persistence/Migrations/**/*.cs`, `Explore.Domain/**/*.cs`; tests `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests`; update `schemas/islamu-event.md`; acceptance: reversible/idempotent migration, lookup seed sync; forbidden: destructive `Down()` that silently loses data.
  - `update-repository-query` — repository/filter/query changes; docs `docs/QUICK_REFERENCE.md`; skill `dotnet-efcore-guidelines`; rule `.claude/rules/efcore-persistence.md`; paths `Explore.Persistence/Repositories/**/*.cs`; tests `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests`; acceptance: immutable `EventQuerySpecification`, explicit navigation loading; forbidden: `IgnoreQueryFilters()` without safety tests.
  - `add-cqrs-handler` — Application handlers for migration commands, registry enforcement, lifecycle/admin flows; docs `docs/ARCHITECTURE.md`, `docs/QUICK_REFERENCE.md`; skill `cqrs-mediatr-guidelines`; rule `.claude/rules/application-layer.md`; paths `Explore.Application/Features/**/*.cs`; tests `Event.Application.UnitTests`, `Event.Architecture.Tests`; forbidden: cross-feature coupling.
  - `add-write-endpoint` / `openapi-contract-change` — if admin/data lifecycle endpoints change; docs `docs/API.md`, `docs/SECURITY-MODEL.md`, `docs/QUICK_REFERENCE.md`; rules `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`; tests `Event.API.IntegrationTests`, `Event.Architecture.Tests`; update `docs/API_CHANGELOG.md`; breaking changes are explicitly approved by the user for this pre-release workstream.
  - `cerbos-policy-change` — if tenant membership or operational data lifecycle policies change; docs `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`; paths `cerbos/**`; tests `Event.API.IntegrationTests`, `Event.Architecture.Tests`; update `docs/AUTHORIZATION.md`; forbidden: widening permissions without explicit user approval.
- **Relevant skills loaded:** `agentic-research`, `clean-architecture-rules`, `dotnet-efcore-guidelines`, `cqrs-mediatr-guidelines`, `auth-patterns`, `outbox-pattern`, `error-tracking`.
- **Relevant rules loaded:** `.claude/rules/domain.md`, `.claude/rules/efcore-migrations.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`.
- **Primary layers touched:** Domain, Application, Persistence, Infrastructure, API, Blazor, Docs, DevOps.
- **Estimated complexity:** XL. This is a cross-cutting model refactor touching tenant identity, authorization, persistence constraints, migrations, RLS/capacity planning, public API contracts, docs, and test strategy.
- **Current implementation state:** Phase 0 is accepted by user instruction to start implementation. Phase 1.1 is implemented in `tenant-scoped-fk-inventory.md`; strict `ITenantEntity` query-filter gaps found during the inventory were corrected in `Explore.Persistence/ExploreDbContext.QueryFilters.cs`. Phase 1.2 through Phase 3.2 were implemented as EF/domain/model hardening slices, but generated migration files from those slices are not stable source-of-truth in this development branch because the user intentionally deletes and regenerates migrations. Future agents must preserve the code-owned model/configuration changes and should not add migration files unless the user explicitly asks. Phase 1.3 is implemented as a bounded PostgreSQL RLS tenant-session prototype with production table policies deferred. Phase 1.4 is implemented with fail-closed tenant query filters, explicit bypass reasons, and persistence/architecture tests. Phase 2.1 is decided in `tenant-role-grant-model-decision.md`. Phase 2.2 and 2.3 implemented the internal `TenantUserRoleGrant` model, repository/authority/provisioning updates, schema/docs, and targeted tests. Phase 2.4 is implemented as a breaking public API/HAL/Cerbos/OpenAPI/Blazor-client replacement: `/api/tenant-user-role-grants`, `TenantUserRoleGrantDto/ListDto`, create/revoke affordances, Cerbos resource `islamuevent_tenant_user_role_grant`, and regenerated OpenAPI/NSwag client contracts. Phase 3.1 is implemented with UTC instants as the schedule source of truth, domain-owned local projections, timezone normalization, schedule graph re-projection, model-owned database check constraints, and targeted unit/integration tests. Phase 3.2 is implemented with exact fixed/prayer-relative `EventSessionIslamicAspect` domain invariants, aligned FluentValidation rules, handler-owned aspect mapping, and database constraints for state shape, offset range, and reference-prayer range. Phase 3.3 is implemented with hard PostgreSQL exclusion enforcement for same-room active session overlaps, declared in `EventSessionConfiguration` and applied by `PostgresModelConstraintApplier` after EF migrations, repository translation of exclusion violations to room-conflict errors, and PostgreSQL integration tests. The 2026-05-28 startup failure was fixed by Development-only `PendingModelChangesWarning` suppression in runtime migration paths and by invoking the model-owned constraint applier from API startup migrations. Phase 4.1 is complete with `custom-property-quota-enforcement-audit.md`. Phase 4.2 is complete: template create/update and template-sync apply enforce the missing definition/option cardinality quotas, Layer 3 governance rejects known event/session Layer 2 semantic keys even under tenant namespaces before repository reads or writes, reserved namespace writes stay blocked without a privileged workflow, and purge/retire semantics are explicit with repository dependency rechecks plus restrictive value/projection delete behavior in EF configuration. Phase 4.3 is complete with bounded operator-facing projection status fields, projection quota rejection metrics, hard-purge decision metrics, and operations triage guidance that avoids raw custom-property keys in metric tags.

## Re-baseline — 2026-05-28 Europe/Brussels

- **Reason:** User clarified that this development branch intentionally deletes generated migration files and regenerates them later; data-model hardening must therefore live in EF/domain/configuration code and docs unless migration files are explicitly requested.
- **What changed:** Same-room overlap enforcement is now model-owned through `EventSessionConfiguration` plus `PostgresModelConstraintApplier`, and runtime Development migration paths ignore EF pending-model drift so Aspire/API startup can run while migrations are being regenerated.
- **Plan impact:** Treat migration filenames in older handoffs as historical evidence only. The current source of truth for new constraints is the EF model configuration, domain/application code, schema docs, and tests. Do not recreate migration files during Phase 4 unless the user asks.
- **Remaining work:** Phase 5 polymorphic registry, Phase 6 lifecycle/retention/partitioning, Phase 7 final docs/API/verification, and deferred production RLS rollout.

## 1. Executive Summary

This workstream hardens the ISLAMU Event data model for enterprise, self-hostable, multi-tenant operation. The current model has strong foundations: UUIDv7 aggregate IDs, normalized lookup tables, tenant-scoped EF Core filters, PII split tables, event/session/aspect layering, governed custom properties, transactional outboxes, and HAL/Cerbos-aware authorization.

The remaining gap is defense-in-depth and long-term operability. Tenant isolation now fails closed through EF filters and explicit bypasses, and tenant-local role authority is rooted in `TenantUserRoleGrant` across Domain, Persistence, Application, API/HAL, Cerbos, OpenAPI, and the generated Blazor client. Custom properties are powerful enough to require stricter quotas and lifecycle controls; event scheduling stores duplicated UTC/local projections that need stronger invariants; polymorphic bindings need a registry contract; and high-growth operational tables need retention/partitioning decisions.

The target state is not compatibility-preserving. It is a cleaner enterprise model: a single tenant-local user authority aggregate, tenant-safe foreign keys or equivalent guardrails, first-class lifecycle/retention policy, optional PostgreSQL RLS as defense-in-depth, bounded EAV, explicit event-time invariants, and updated API/auth/docs/tests.

Explicitly out of scope for the first implementation pass:
- Building a generic runtime schema engine.
- Replacing PostgreSQL or EF Core.
- Implementing tenant-per-database hosting.
- Rewriting all controllers/UI flows unless required by the model changes.
- Treating partitioning/RLS as already implemented before migrations and operations runbooks prove it.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| The repo uses .NET 10, EF Core 10, Npgsql, Aspire, TUnit, and Testcontainers. | `global.json`; `Directory.Packages.props`; `Explore.Persistence/Explore.Persistence.csproj`; `Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj` | High | Verified package versions include EF Core `10.0.7`, Npgsql EF provider `10.0.1`, Npgsql `10.0.2`, Aspire `13.2.3`, Testcontainers PostgreSQL `4.10.0`, TUnit `1.33.0`. |
| Clean Architecture is canonical. | `docs/ARCHITECTURE.md`; `.claude/skills/clean-architecture-rules/SKILL.md` | High | Domain inward dependency boundary is non-negotiable. |
| Tenant isolation is application/EF-filter centered today. | `docs/MULTI_TENANCY.md`; `Explore.Persistence/ExploreDbContext.QueryFilters.cs`; `docs/SECURITY-MODEL.md` | High | Named `Tenant` and `SoftDelete` filters exist. RLS tenant-session prototype support exists, but production table policies are not enabled. |
| Tenant query filters now fail closed when `TenantContext` is missing. | `Explore.Persistence/ExploreDbContext.cs`; `Explore.Persistence/ExploreDbContext.QueryFilters.cs`; `TenantQueryFilterFailClosedTests` | High | System/admin paths require `EnableTenantFilterBypass(reason)` or `IgnoreTenantFilter(reason)`. |
| `TenantUser` stores tenant-local user lifecycle/moderation state. | `Explore.Domain/TenantUser.cs`; `Explore.Persistence/Configurations/Entities/TenantUserConfiguration.cs`; `docs/MULTI_TENANCY.md` | High | Includes status, joined/suspended/ban/remove fields, actor/profile, soft delete. |
| Tenant role authority is now rooted in `TenantUserRoleGrant`, an auditable child of `TenantUser`. | `Explore.Domain/TenantUserRoleGrant.cs`; `Explore.Domain/TenantUser.cs`; `Explore.Persistence/Configurations/Entities/TenantUserRoleGrantConfiguration.cs`; `Explore.API/Controllers/TenantUserRoleGrantController.cs`; `schemas/openapi.json`; `schemas/islamu-event.md` | High | The former tenant-member public contract was replaced by explicit tenant role grant DTOs, route names, HAL policies, Cerbos policy/schema, and generated client methods. Historical migration names may not exist in the current development worktree because migrations are regenerated. |
| Admin authority now relies on active `TenantUser` state plus active `TenantUserRoleGrant`. | `Explore.Infrastructure/Identity/AdminContext.cs`; `Explore.Persistence/Repositories/TenantUserRoleGrantRepository.cs`; `Explore.Application/Contracts/Persistence/ITenantUserRoleGrantRepository.cs` | High | `IsTenantAdmin` and active membership checks query active grants and active tenant-local users. |
| Event-scoped roles already exist in the domain and authorization layer. | `Explore.Domain/EventRoleAssignment.cs`; `Explore.Persistence/Configurations/Entities/EventRoleAssignmentConfiguration.cs`; `Explore.Persistence/Services/EventAuthoritySnapshotService.cs`; `dev/active/event-scoped-operational-roles/event-scoped-operational-roles-plan.md` | High | This plan must not regress that workstream. |
| Event/session schedule model stores UTC times and cached local projections. | `Explore.Domain/Event.cs`; `Explore.Domain/EventSession.cs`; `Explore.Domain/Services/Scheduling/**`; `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs`; `schemas/islamu-event.md` | High | Phase 3.1 makes UTC instants authoritative, keeps timezone validation in domain/application services, reprojects the loaded schedule graph when event timezone changes, and models DB constraints for rollup ranges and local minute projection consistency. |
| Same-room session overlap is a hard persistence constraint. | `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs`; `Explore.Persistence/Schema/PostgresModelConstraintApplier.cs`; `Explore.Persistence/Repositories/EventSessionRepository.cs`; `Event.Persistence.IntegrationTests/Repositories/SchedulingConstraintTests.cs`; `schemas/islamu-event.md` | High | Active room-bound sessions cannot overlap within the same tenant/location/room. Adjacent sessions and different rooms are allowed; soft delete releases the room. The PostgreSQL exclusion constraint is model-owned because development migrations are regenerated frequently. |
| Islamic session aspect has exact fixed/prayer-relative invariants. | `Explore.Domain/EventSessionIslamicAspect.cs`; `EventSessionIslamicAspectValidationRules`; `Explore.Persistence/Configurations/Entities/EventSessionIslamicAspectConfiguration.cs`; `schemas/islamu-event.md` | High | `Fixed` requires null prayer fields; `RelativeToPrayer` requires location at validation time plus `reference_prayer` and `offset_minutes`; EF configuration/schema docs describe DB constraints for field shape, offset range, and prayer range. |
| Layer 3 custom properties are governed, projected, and bounded by docs. | `docs/CUSTOM_PROPERTIES.md`; `docs/adr/ADR-006-custom-properties-runtime-boundary.md`; `Explore.Domain/EventCustomPropertyDefinition.cs`; `Explore.Persistence/Configurations/Entities/EventCustomPropertyProjectionConfiguration.cs` | High | Existing boundary says custom properties must not become a runtime schema engine. |
| Custom property quotas exist in code. | `Explore.Persistence/Services/CustomPropertyQuotaResolver.cs`; `Explore.Application/Features/EventCustomProperties/**`; `Explore.Application/Features/EventSessionCustomProperties/**` | High | Need hardening around enforcement coverage, operator visibility, and DB growth. |
| External bindings are provider-neutral but type strings are only constrained as non-blank text. | `Explore.Domain/ExternalBinding.cs`; `Explore.Persistence/Configurations/Entities/ExternalBindingConfiguration.cs`; `Explore.Domain/Constants/ExternalBindingTypes.cs` | High | A registry contract should constrain allowed internal/external type pairs. |
| Specialized email dispatch outbox is now part of the model. | `Explore.Domain/EmailDispatchOutbox.cs`; `Explore.Persistence/Configurations/Entities/EmailDispatchOutboxConfiguration.cs`; `dev/active/crmworx-event-api-adaptation/crmworx-event-api-adaptation-context.md` | High | Must coordinate with active durable side-effect work. |
| Audit, notification, outbox, email dispatch, contact export, and projection backlog tables are growth-sensitive. | `Explore.Domain/AuditLog.cs`; `Explore.Domain/Notification.cs`; `Explore.Domain/EmailDispatchOutbox.cs`; `docs/OPERATIONS.md`; `docs/OUTBOX_PATTERN.md` | High | `docs/OPERATIONS.md` states partitioning is not implemented. |
| Current canonical schema document was recently synchronized to EF snapshot. | `schemas/islamu-event.md`; prior schema-drift verification in this session | High | Future implementation must keep this file in sync with migrations. |
| All strict `ITenantEntity` domain classes are now registered in named tenant filters. | `Explore.Domain/**/*.cs`; `Explore.Persistence/ExploreDbContext.QueryFilters.cs`; `dev/active/enterprise-data-model-hardening/tenant-scoped-fk-inventory.md` | High | Phase 1.1 found 69 strict tenant entities and closed seven missing filter registrations. Phase 1.4 removed null-tenant broad reads. |
| The first composite-FK hardening scope is the event graph and is now implemented in model code. | `dev/active/enterprise-data-model-hardening/tenant-scoped-fk-inventory.md`; event EF configuration files; `EventGraphTenantForeignKeyTests`; `schemas/islamu-event.md` | High | The implemented scope includes event/session/day/agenda/group/registration/contact-share relations before EAV and tenant-role consolidation. Generated migration filenames from this work are historical only in the current development workflow. |
| RLS tenant-session behavior is now proven in a bounded prototype. | `Explore.Persistence/Security/PostgresTenantSessionInterceptor.cs`; `Event.Persistence.IntegrationTests/TenantIsolation/PostgresTenantSessionRlsPrototypeTests.cs`; `docs/SECURITY-MODEL.md` | High | The prototype uses EF Core connection interception, `app.current_tenant_id`, a synthetic forced-RLS table, and a generated non-superuser role. Production table policies are deferred. |
| Current API inventory is generated and large. | `docs/API_CONTRACT_INVENTORY.md` | High | Any endpoint rename/removal must update generated inventory and API changelog. |
| Context7 docs confirm EF query filters, named filters, and concurrency-token patterns. | Context7 `/dotnet/entityframework.docs` query on 2026-05-26 | Medium | Used for framework behavior, not repo behavior. |
| Context7 docs confirm PostgreSQL RLS policy syntax and composite FK/check/exclusion constraint support. | Context7 `/websites/postgresql_current` query on 2026-05-26 | Medium | Used for DB feature feasibility. |
| External industry research supports treating tenant isolation as a spectrum and using DB-level isolation/RLS as defense-in-depth. | OWASP Multi Tenant Security Cheat Sheet; Microsoft Azure Architecture Center multitenancy storage/data articles | Medium | Tavily MCP was requested by the user but was not exposed by the tool registry; see unknowns. |

### 2.2 Existing Implementation

**Domain**

- `Tenant`, `TenantUser`, `TenantUserProfile`, `TenantUserRoleGrant`, `TenantSetting`, `TenantSettingsDocument`, and related entities model tenant boundaries, tenant-local user state, and tenant role authority.
- `User` is global and soft-deletable; PII is split into `UserPii`.
- `Actor`, `Organization`, `Group`, `Event`, `EventSession`, `Location`, `StorageObject`, `AuditLog`, `Notification`, registration, outbox, and custom-property rows carry tenant scope where appropriate.
- Event data uses a three-layer model:
  - Layer 1: `Event`, `EventSession`, agenda/session-group/day/registration entities.
  - Layer 2: typed aspects such as `EventIslamicAspect`, `EventTechAspect`, `EventSessionIslamicAspect`.
  - Layer 3: custom-property definitions/values/projections.

**Application**

- CQRS/MediatR is the orchestration layer.
- Tenant role/admin flows depend on `ITenantUserRoleGrantRepository` and `ITenantUserRepository`; public DTOs and requests now use `TenantUserRoleGrant` names and tenant-local user identifiers.
- Custom-property handlers use `ICustomPropertyQuotaResolver`.
- Event-role authority uses `IEventAuthoritySnapshotService`.
- Authorization metadata lives in `Explore.Application/Authorization/**`.

**Persistence**

- `ExploreDbContext` is split into `DbSets`, `QueryFilters`, and `SaveChanges`.
- Named EF Core filters enforce `Tenant` and `SoftDelete`.
- EF configurations are mostly one file per entity and define UUIDv7, constraints, indexes, and relationships.
- `GenericRepository<T,TKey>` returns entities and centralizes basic CRUD, soft delete, hard delete, and unique-key error conversion.
- Current repositories use `IgnoreTenantFilter(reason)` in selected authority/tenant-resolution paths. These must stay explicit, bounded, and tested.

**Infrastructure/API**

- `AdminContext` resolves instance/tenant/org/group authority from database state.
- `FallbackAuthorizationService` and Cerbos-backed provider route resource authorization.
- API controllers are thin MediatR + HAL response shells by convention, but the active API contract inventory still shows many endpoints with route-name metadata marked as pending phase work.
- HAL `_links` are the client source of truth for mutation affordances.

**Docs/Operations**

- `docs/SECURITY-MODEL.md` documents RLS as prototype-supported, with production tenant-table policies deferred.
- `docs/OPERATIONS.md` documents partitioning as planned capacity work, not current.
- `docs/CUSTOM_PROPERTIES.md` and ADR-006 define EAV boundaries.
- `docs/OUTBOX_PATTERN.md` documents general and specialized outbox behavior.

### 2.3 Existing Tests And Verification Coverage

Verified test projects and current coverage signals:

- `Event.Architecture.Tests` — architecture/context/contract invariants; prior run in this session passed.
- `Event.Persistence.IntegrationTests` — PostgreSQL-backed persistence tests; current run passed 121/121 including event graph composite-FK guardrails, RLS tenant-session prototype coverage, and tenant role grant FK/scope rejection tests.
- `Event.Application.UnitTests` — CQRS/domain-service behavior; includes onboarding, event-role, custom-property, email-dispatch metric tests.
- `Event.API.IntegrationTests` — API contract, auth, OpenAPI, and integration behavior.
- `Explore.Infrastructure.Tests` — infrastructure services including settings, email dispatch RabbitMQ settings/health tests in current dirty worktree.
- `Explore.Blazor.Client.Tests` — UI service/component tests where HAL affordance changes appear.

Important test gaps for this workstream:

- No complete DB-level tenant-safe composite FK suite across all tenant families; the event graph slice is covered.
- RLS production rollout is not implemented; only the tenant-session prototype is tested.
- No retention/partitioning integration tests for append-only/operational tables.
- No single test suite proves every tenant-scoped FK includes tenant identity or a documented exception.
- Event-time overlap/exclusion constraints are not currently proven.
- External binding allowed-type registry parity is not enforced by tests.

### 2.4 Existing Documentation And Contracts

- Agent/task contracts: `AGENTS.md`, `.claude/contract/intents.yaml`, `.claude/commands/dev-docs.md`, `dev/active/README.md`.
- Architecture: `docs/ARCHITECTURE.md`, `docs/GOVERNANCE.md`, `docs/CODEBASE_STRUCTURE.md`.
- Domain/data model: `docs/DOMAIN.md`, `schemas/islamu-event.md`.
- Tenancy/security/authz: `docs/MULTI_TENANCY.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/ADMIN_HIERARCHY.md`.
- API/HAL: `docs/API.md`, `docs/API_CHANGELOG.md`, generated `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`.
- Operational docs: `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`, `docs/CONFIGURATION.md`, `docs/TROUBLESHOOTING.md`, `docs/BACKUP_RESTORE_UPGRADE.md`.
- EAV/extensibility: `docs/CUSTOM_PROPERTIES.md`, `docs/EXTENSIBILITY.md`, `docs/MODULAR_EVENTS.md`, ADR-006.
- Reliability: `docs/OUTBOX_PATTERN.md`, ADR-002, ADR-008.
- Active overlapping workstreams:
  - `dev/active/event-scoped-operational-roles/`
  - `dev/active/backend-api-health-refactor/`
  - `dev/active/api-contract-stabilization/`
  - `dev/active/crmworx-event-api-adaptation/`
  - `dev/active/rabbitmq-messaging/`
  - `dev/active/enterprise-ci-cd-hardening/`

### 2.5 Current Pain Points / Improvement Areas

1. **Tenant isolation still needs defense-in-depth beyond EF filters.**
   - Evidence: `ExploreDbContext.QueryFilters.cs` now fails closed for missing tenant context and `PersistenceTenantFilterArchitectureTests` guards this behavior; `docs/SECURITY-MODEL.md` still marks production RLS table policies as deferred.
   - Why it matters: shared-database multi-tenancy needs layered protection. Direct SQL, reporting, migrations, or future host-admin APIs still need explicit role/session and authorization design.

2. **Tenant role grants are now clear end-to-end, but downstream consumers must adopt the breaking contract.**
   - Evidence: `TenantUserRoleGrant` replaced the old tenant-member aggregate in Domain/Persistence/Application and now owns public DTOs, controller routes, HAL policies, Cerbos resource policy/schema, OpenAPI schemas, and generated Blazor client methods.
   - Why it matters: enterprise-grade clarity is now in place, but API consumers must move to `/api/tenant-user-role-grants`, `TenantUserRoleGrantDto/ListDto`, create/revoke semantics, and HAL `_links` affordance gating.

3. **Tenant-safe FK relationships are still incomplete outside the completed event graph slice.**
   - Evidence: Phase 1.2 now protects the high-risk event graph, but custom-property/template/storage/operational families still include child rows with `TenantId` plus single-column parent references.
   - Why it matters: outside the completed event graph scope, a row can still carry tenant A and point at parent tenant B unless application code prevents it.

4. **Custom-property EAV is powerful and must stay bounded.**
   - Evidence: docs define boundary and quotas exist, but projections, governance, quota reporting, purge, and template sync are spread across many handlers/repositories.
   - Why it matters: unmanaged EAV becomes a hidden parallel schema engine, which hurts indexing, interoperability, exports, policy, and maintainability.

5. **Event-time invariants need stronger persistence enforcement.**
   - Evidence: `EventSession` stores UTC times and cached local projections; DB only checks `end_time > start_time`.
   - Why it matters: discovery, schedule rendering, prayer-relative timing, capacity, and conflict checks depend on stable temporal truth.

6. **Polymorphic references need a formal registry.**
   - Evidence: `ExternalBinding` stores `ExternalType` and `InternalType` as strings with non-blank checks; `Notification` has optional entity-type/id; custom properties target entity type names.
   - Why it matters: stringly typed polymorphism can accumulate invalid target types, unmanaged cleanup semantics, and unbounded integration contracts.

7. **Soft delete and hard purge policy is inconsistent by entity family.**
   - Evidence: `GenericRepository.Delete` soft-deletes `ISoftDeletable`, hard-deletes otherwise; custom-property purge has special audited flows; event roles are lifecycle evidence; outbox dead letters remain indefinitely.
   - Why it matters: enterprise self-hosting needs predictable restore/purge/retention/export behavior by table family.

8. **Operational tables lack retention/partitioning implementation.**
   - Evidence: `docs/OPERATIONS.md` explicitly states partitioning is not implemented; audit/outbox/notifications/email/contact-export/projection backlog tables are growth-sensitive.
   - Why it matters: self-hosters need operational knobs before tables grow without bounds.

9. **Some existing domain files touched by future work do not currently start with two `ABOUTME:` lines.**
   - Evidence: `Explore.Domain/Event.cs`, `Explore.Domain/EventSession.cs`, and `Explore.Domain/EventRegistration.cs` start with `using`.
   - Why it matters: AGENTS requires new/modified files to comply; implementation slices touching those files should repair headers.

10. **Research tooling gap: Tavily MCP was requested but unavailable.**
    - Evidence: `tool_search` exposed Context7/GitHub/Jean tools, but no Tavily namespace.
    - Why it matters: user requested Tavily specifically. Implementation agents should rerun Tavily research if the connector becomes available before final approval.

### 2.6 Unknowns After Investigation

- **Exact PostgreSQL RLS rollout strategy.** Tenant-session behavior is proven and full production rollout is deferred; remaining rollout work needs app-role/migration-role separation, admin/system-path design, and table-family migrations.
- **Remaining composite FK blast radius.** Event graph scope is implemented; custom-property/template/storage/operational families still need bounded follow-up scopes before constraints are added.
- **Tenant role-grant consumer migration.** Phase 2.4 implemented the breaking public contract replacement; downstream clients now need to adopt tenant-user-role-grant routes, DTOs, and revoke semantics.
- **RLS/session-variable interaction with pooled DbContexts.** The connection-open interceptor approach is proven for tenant/session binding; migration and host-admin/system bypass paths still need explicit production design before table policies are enabled.
- **Partitioning threshold and table list.** Need choose implementation now or create documented capacity knobs only. Resolve in Task 6.1.
- **API breaking changes.** Phase 2.4 replaced the former tenant-member API surface. Future contract work should update generated inventory and changelog whenever additional DTO/controller changes land.
- **Tavily research.** Tavily MCP was not available in this session. Resolve by rerunning external research through Tavily if available before implementation approval.

## 3. Proposed Future State

Target design:

```text
Tenant
  └─ TenantUser (one row per global User per Tenant, tenant-local lifecycle truth)
       ├─ TenantUserProfile
       └─ TenantUserRoleGrant (auditable tenant role grants reference TenantUserId)

Tenant-scoped aggregate rows
  ├─ Carry TenantId
  ├─ Use EF named Tenant + SoftDelete filters where applicable
  ├─ Use tenant-safe composite FK/alternate-key guardrails for parent-child tenant consistency
  └─ Optionally protected by PostgreSQL RLS policy using app.current_tenant_id

Events
  ├─ Event as program/container
  ├─ EventSession as scheduled child item
  ├─ EventDay/EventAgendaItem/EventSessionGroup as schedule structure
  ├─ Layer 2 aspects for sector-standard semantics
  └─ Layer 3 custom properties only for governed local extension

Operational tables
  ├─ Explicit lifecycle class: source of truth / append-only evidence / retry state / derived projection / cache
  ├─ Retention and cleanup policy per class
  ├─ Partitioning decision recorded before implementation claims operator support
  └─ Metrics/health/logging for growth and cleanup outcomes
```

Developer/operator outcomes:

- Developers have one tenant-local user authority model.
- Persistence constraints prevent cross-tenant parent-child mismatches even when a repository bug slips through.
- RLS is either implemented behind a tested connection/session strategy or explicitly kept as a documented post-plan task.
- Custom properties remain a governed extension layer, not a shadow product model.
- Operational tables have lifecycle and retention policies operators can understand.
- API, HAL, Cerbos/local auth, OpenAPI, and Blazor affordances remain aligned.

## 4. Non-Negotiable Constraints

- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- Use `Guid` for aggregates, `int` for lookups, `long` for cursors/concurrency/cursors.
- GET endpoints remain `[AllowAnonymous]`; write endpoints remain `[Authorize]`.
- HAL `_links` are the single source of truth for UI action affordances.
- Domain cannot reference EF Core, MediatR, ASP.NET Core, Persistence, API, or Blazor.
- Application cannot reference `ExploreDbContext` or infrastructure implementations.
- `IgnoreTenantFilter(reason)` / `IgnoreQueryFilters()` requires an explicit reason, constrained predicate, and tests.
- Tenant isolation is API-authoritative and must not silently return all rows when tenant context is absent.
- All new/modified files must start with two `ABOUTME:` lines.
- Applied migrations are not rewritten; use corrective migrations.
- Breaking API/schema changes are allowed by user instruction, but they must be documented and tested.
- Outbox/dispatch state remains PostgreSQL-source-of-truth; broker transports cannot own business state.
- Custom properties must not become a runtime schema engine.

## 5. Architecture And Design Decisions

### Decision 1: Consolidate Tenant Authority Around Tenant-Local User

- **Decision:** Replace `TenantMember` with `TenantUserRoleGrant`, an auditable child of `TenantUser`.
- **Why:** Tenant lifecycle/moderation and tenant role authority must have one database anchor. `TenantUser` already owns tenant-local participation state; role grants should reference that row, not re-link directly to global `User`.
- **Alternatives considered:** Keep current split and rely on repository joins; put one `RoleId` on `TenantUser`; generic `UserRoleAssignment`; rename `TenantMember` without changing constraints.
- **Consequences:** Domain, persistence, admin context, onboarding, managed-provider provisioning, API DTOs/controllers, HAL policies, Cerbos attributes, Blazor generated client contracts, and API docs now use `TenantUserRoleGrant`. Role updates become create/revoke flows so grant evidence remains auditable.
- **Files/layers affected:** `Explore.Domain/TenantUser.cs`, `Explore.Domain/TenantUserRoleGrant.cs`, `Explore.Persistence/Configurations/Entities/TenantUserRoleGrantConfiguration.cs`, `Explore.Persistence/Repositories/TenantUserRoleGrantRepository.cs`, `Explore.Infrastructure/Identity/AdminContext.cs`, `Explore.Application/Features/TenantUserRoleGrants/**`, `Explore.API/Controllers/TenantUserRoleGrantController.cs`, API/HAL/Cerbos docs.
- **Decision record:** `dev/active/enterprise-data-model-hardening/tenant-role-grant-model-decision.md`.

### Decision 2: Add Tenant-Safe Database Guardrails

- **Decision:** Add composite tenant guardrails for tenant-owned parent-child relationships where both sides are tenant-scoped.
- **Why:** EF filters protect reads, but DB constraints should prevent invalid writes.
- **Alternatives considered:** EF-only invariants; RLS only; trigger-only tenant checks.
- **Consequences:** Requires alternate keys/indexes and careful migration sequencing; some relationships need documented exceptions for global lookup/shared rows.
- **Files/layers affected:** EF configurations, migrations, integration tests, `schemas/islamu-event.md`.

### Decision 3: Treat RLS As Defense-In-Depth, Not As Application Authorization

- **Decision:** Keep PostgreSQL RLS as defense-in-depth. Implement only tenant-session prototype support now; defer production table policies until runtime app-role, migration-role, host-admin/system-path, and table-family rollout design is complete.
- **Why:** RLS is valuable, but a broken pooled-session or role design can cause outages or leaks. The Phase 1.3 prototype proved tenant-session binding and also proved that superuser connections bypass RLS, so production role design is mandatory before rollout.
- **Alternatives considered:** No RLS; RLS-first migration across all tables; schema-per-tenant; shipping only an ADR with no code.
- **Consequences:** `PostgresTenantSessionInterceptor` exists behind `Persistence:EnableRlsTenantSession`, but production tenant tables remain governed by EF filters and database FKs until a later RLS migration slice.
- **Files/layers affected:** `Explore.Persistence`, API/Infrastructure connection setup, docs/security/operations.

### Decision 4: Keep Custom Properties Bounded And Observable

- **Decision:** Preserve the Layer 1/2/3 model, add stronger quota enforcement/reporting and promote standard semantics out of EAV.
- **Why:** EAV is useful for long-tail fields but dangerous as hidden core schema.
- **Alternatives considered:** Remove custom properties; expand into full runtime schema engine.
- **Consequences:** More governance checks, docs, operator reports, and tests around projections/quotas/purge.
- **Files/layers affected:** `Explore.Application/Features/*CustomProperties/**`, projection services, docs.

### Decision 5: Classify Operational Tables By Lifecycle

- **Decision:** Add a table lifecycle matrix and implement cleanup/retention/partitioning only from that matrix.
- **Why:** Audit evidence, retry state, projections, caches, and user content have different retention semantics.
- **Alternatives considered:** One generic cleanup job; indefinite retention for all operational tables.
- **Consequences:** Operators get explicit knobs; migrations may add indexes/partitions later; tests must prove cleanup never deletes protected evidence.
- **Files/layers affected:** `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`, `Explore.API` background services, Persistence repositories/migrations.

## 6. Implementation Phases

### Phase 0: Review, Baseline, And Research Closure

- **Goal:** Confirm scope, rerun missing research, freeze first PR boundaries, and prevent conflict with active workstreams.
- **Depends on:** User review of this draft.
- **Relevant files:** this workstream docs; `dev/active/event-scoped-operational-roles/**`; `dev/active/backend-api-health-refactor/**`; `dev/active/crmworx-event-api-adaptation/**`.
- **Related skills/rules:** `agentic-research`, `clean-architecture-rules`.
- **Acceptance criteria:** User approves or corrects plan; Tavily status is resolved; implementation PR sequence is finalized.
- **Verification:** No code verification beyond doc review.
- **Rollback / failure handling:** If Tavily remains unavailable, record this and continue only if user accepts the available research baseline.

#### Task 0.1: User Review And Scope Approval

- **Type:** investigate / docs
- **Layer:** Docs
- **Files:** existing `dev/active/enterprise-data-model-hardening/*`
- **Description:** User reviews sections 2.5, 3, 5, 6, and 13 for correctness and priority.
- **Acceptance Criteria:**
  - [ ] Plan status changed from Draft to User-reviewed or Approved.
  - [ ] Any rejected recommendation is moved to Remaining/Deferred Work with reason.
- **Dependencies:** none
- **Effort:** S
- **Required Skills/Rules:** `agentic-research`
- **Validation:** review only.

#### Task 0.2: Rerun External Research With Tavily If Available

- **Type:** investigate
- **Layer:** Docs
- **Files:** existing `enterprise-data-model-hardening-context.md`
- **Description:** Use Tavily MCP for multi-tenant database isolation, PostgreSQL RLS, EAV governance, and operational retention research if the connector becomes available. Keep private repo details out of external prompts.
- **Acceptance Criteria:**
  - [ ] Tavily research findings are appended to context, or unavailability is recorded with tool evidence.
  - [ ] Context distinguishes local repo facts, official docs, and external research.
- **Dependencies:** none
- **Effort:** S
- **Required Skills/Rules:** `agentic-research`
- **Validation:** context updated.

### Phase 1: Tenant Isolation Guardrails

- **Goal:** Move from EF-filter-only tenant safety toward database-enforced tenant consistency.
- **Depends on:** Phase 0.
- **Relevant files:** `Explore.Persistence/ExploreDbContext.QueryFilters.cs`, `Explore.Persistence/QueryFilters/QueryFilterNames.cs`, `Explore.Persistence/Configurations/Entities/*.cs`, migration files only when explicitly requested/regenerated, `schemas/islamu-event.md`, `docs/SECURITY-MODEL.md`, `docs/MULTI_TENANCY.md`.
- **Related skills/rules:** `dotnet-efcore-guidelines`, `.claude/rules/efcore-persistence.md`, `.claude/rules/efcore-migrations.md`.
- **Acceptance criteria:** Tenant-scoped FK inventory exists; implementation adds tested guardrails for selected high-risk relationships; no runtime broad all-tenant read remains undocumented.
- **Verification:** `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`; architecture tests.
- **Rollback / failure handling:** In this development branch, guardrails live in EF configuration/domain code while migrations are regenerated. When migration files are explicitly generated for release, ship them in small relationship-family slices and never rewrite applied migrations after release.

#### Task 1.1: Generate Tenant-Scoped Entity And FK Inventory

- **Type:** investigate / docs
- **Layer:** Persistence / Docs
- **Files:** existing `Explore.Domain/**/*.cs`, `Explore.Persistence/Configurations/Entities/**/*.cs`, `schemas/islamu-event.md`; new or updated analysis section in context.
- **Status:** Complete on 2026-05-27. Inventory is `dev/active/enterprise-data-model-hardening/tenant-scoped-fk-inventory.md`.
- **Description:** Inventory every `ITenantEntity`, every FK from tenant-scoped child to tenant-scoped parent, and every intentional exception such as global lookups or nullable instance rows.
- **Acceptance Criteria:**
  - [ ] Each tenant-scoped entity is classified: root, child, lookup/global, operational, derived projection, or exception.
  - [ ] Each FK is classified as tenant-safe, application-only tenant-safe, global/shared, or unsafe.
  - [ ] First guardrail migration scope is selected.
- **Dependencies:** 0.1
- **Effort:** M
- **Required Skills/Rules:** `dotnet-efcore-guidelines`
- **Validation:** inventory reviewed against EF model snapshot and schema docs.

#### Task 1.2: Add Composite Tenant-Safe Keys/FKs For High-Risk Event Graph

- **Type:** modify / model-owned schema / test / docs
- **Layer:** Domain / Persistence / Docs
- **Files:** existing `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs`, `EventDayConfiguration.cs`, `EventAgendaItemConfiguration.cs`, `EventSessionGroupConfiguration.cs`, `EventRegistrationIntentConfiguration.cs`, `EventRegistrationConfiguration.cs`; generated migration only when requested; existing `schemas/islamu-event.md`.
- **Status:** Complete on 2026-05-27 in model/configuration code. Historical migration `20260527092407_AddEventGraphTenantForeignKeys` is not a current source-of-truth file in this development worktree; `EventGraphTenantForeignKeyTests` verify the event graph scope.
- **Description:** Add alternate keys/indexes and composite FKs so event-child rows cannot point across tenant boundaries. Start with event/session/registration graph because it is the highest-value tenant data.
- **Acceptance Criteria:**
  - [x] EF model/configuration adds composite guardrails without weakening required query paths.
  - [x] Integration tests fail on cross-tenant, cross-event, and cross-location room parent-child mismatch.
  - [x] Existing valid seed/test graphs still persist.
  - [x] Schema DBML reflects constraints.
- **Dependencies:** 1.1
- **Effort:** XL
- **Required Skills/Rules:** `dotnet-efcore-guidelines`, `efcore-migrations`, `efcore-persistence`
- **Validation:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`; scoped `git diff --check`. DBML was manually reviewed against EF model/schema evidence because no automated parser was found.

#### Task 1.3: Decide And Prototype RLS

- **Type:** investigate / spike / docs
- **Layer:** Persistence / Infrastructure / DevOps / Docs
- **Files:** existing `docs/SECURITY-MODEL.md`, `docs/MULTI_TENANCY.md`, `docs/CONFIGURATION.md`, `Explore.Persistence/PersistenceServicesRegistration.cs`; new `Explore.Persistence/Security/PostgresTenantSessionInterceptor.cs`, `Event.Persistence.IntegrationTests/TenantIsolation/PostgresTenantSessionRlsPrototypeTests.cs`.
- **Status:** Complete on 2026-05-27. Tenant-session infrastructure is implemented behind `Persistence:EnableRlsTenantSession`; production table RLS policies are deferred.
- **Description:** Build a small, reversible RLS prototype using `app.current_tenant_id` session variable and EF Core/Npgsql connection-open behavior. Decide whether to implement full production RLS now or defer.
- **Acceptance Criteria:**
  - [x] Connection/session variable behavior is proven under pooled EF Core DbContext and Npgsql connection semantics.
  - [x] Migration/design-time/seeding bypass behavior is documented as a production rollout prerequisite.
  - [x] Integration test proves normal tenant A/B query behavior and absent-tenant denial through a non-superuser forced-RLS role.
  - [x] Decision recorded: defer full production RLS table policies until app/migration role design and admin/system paths are explicit.
- **Dependencies:** 1.1
- **Effort:** L
- **Required Skills/Rules:** `agentic-research`, `dotnet-efcore-guidelines`, `auth-patterns`
- **Validation:** `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed 117/117.

### Phase 2: Tenant User / Membership Consolidation

- **Goal:** Eliminate ambiguous tenant-local identity and role authority split.
- **Depends on:** Phase 1 inventory.
- **Relevant files:** `Explore.Domain/TenantUser.cs`, `Explore.Domain/TenantUserRoleGrant.cs`, `Explore.Persistence/Configurations/Entities/TenantUserConfiguration.cs`, `TenantUserRoleGrantConfiguration.cs`, `Explore.Persistence/Repositories/TenantUserRoleGrantRepository.cs`, `Explore.Infrastructure/Identity/AdminContext.cs`, `Explore.Application/Features/TenantUserRoleGrants/**`, managed provider/onboarding handlers, API controllers/HAL/Cerbos.
- **Related skills/rules:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`, EF rules.
- **Acceptance criteria:** Database has one tenant-local user root; tenant roles reference it; all authority checks and API contracts use the new model.
- **Verification:** Application unit tests, persistence integration tests, API integration tests, authorization parity tests.
- **Rollback / failure handling:** Development mode allows breaking change. Preserve the domain/EF model backfill assumptions in docs/tests; generate migration/backfill files only when the user requests migration regeneration.

#### Task 2.1: Choose Final Tenant Role Grant Model

- **Type:** investigate / docs
- **Layer:** Domain / Application / Persistence
- **Status:** Complete on 2026-05-27. Decision record is `tenant-role-grant-model-decision.md`.
- **Files:** existing `TenantUser.cs`, former `TenantMember.cs`, `Role.cs`, `RoleScope.cs`, tenant role-grant repository, `AdminContext.cs`; update context decision log.
- **Description:** Chose `TenantUserRoleGrant`, a many-to-one child of `TenantUser`, with composite tenant FK, tenant-role-scope FK, explicit grant/revoke lifecycle, and breaking API replacement direction.
- **Acceptance Criteria:**
  - [x] Decision names entity, keys, uniqueness, lifecycle, and role-scope validation.
  - [x] Decision explains migration/backfill from `TenantMember`.
  - [x] Decision lists API DTO names to rename/remove.
- **Dependencies:** 1.1
- **Effort:** M
- **Required Skills/Rules:** `clean-architecture-rules`
- **Validation:** repository-grounded decision documented; Context7 EF Core alternate-key guidance confirmed. Implementation review occurs in Task 2.2.

#### Task 2.2: Implement Domain And Persistence Model

- **Type:** create / modify / model-owned schema / test
- **Layer:** Domain / Persistence
- **Status:** Complete on 2026-05-27 in domain/EF/configuration/API code. Historical migration `20260527132704_ReplaceTenantMemberWithTenantUserRoleGrants` documented the intended backfill, but generated migration files are not stable source-of-truth in the current development worktree.
- **Files:** new `TenantUserRoleGrant` entity/config/repository; existing `Explore.Persistence/ExploreDbContext.DbSets.cs`; generated migration only when requested; `schemas/islamu-event.md`; remove old `TenantMember` domain/persistence contract in the same breaking slice.
- **Description:** Add `TenantUserRoleGrant`, migrate `TenantMember` data through matching `TenantUser` rows, enforce tenant role scope through composite FK to `Role`, enforce tenant ownership through composite FK to `TenantUser`, and remove the old `TenantMember` schema per the Phase 2.1 decision.
- **Acceptance Criteria:**
  - [x] Role grant references tenant-local user by key and tenant.
  - [x] DB rejects cross-tenant or wrong-role-scope grants.
  - [x] Backfill migrates existing tenant admin data and fails fast on orphan/non-tenant-scoped source rows.
  - [x] Old domain/persistence table names are removed; public DTO/API replacement was completed in Phase 2.4.
- **Dependencies:** 2.1
- **Effort:** XL
- **Required Skills/Rules:** `domain`, `efcore-migrations`, `efcore-persistence`
- **Validation:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed 121/121; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed 178/179 with 1 skipped; `git diff --check` passed; schema DBML updated and manually reviewed against migration/snapshot.

#### Task 2.3: Update Authority Resolution And Provisioning

- **Type:** modify / test
- **Layer:** Application / Infrastructure / Persistence
- **Status:** Complete on 2026-05-27 for internal authority/provisioning. Public API/HAL/Cerbos/Blazor contract parity was completed in Task 2.4.
- **Files:** existing `Explore.Infrastructure/Identity/AdminContext.cs`, `Explore.Persistence/Repositories/TenantUserRoleGrantRepository.cs`, `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`, `Explore.Application/Features/ManagedProviderProvisioning/Handlers/Commands/EnsureManagedProviderClientProvisionedCommandHandler.cs`.
- **Description:** Rewrite admin/membership checks and provisioning flows to use the new tenant-local role model.
- **Acceptance Criteria:**
  - [x] Active/banned/suspended/removed tenant users cannot hold effective tenant authority.
  - [x] Onboarding and managed-provider provisioning create or reuse tenant-local user and role grant in one transaction.
  - [x] Admin context uses the new grant repository; role updates revoke old grants and create active grants.
- **Dependencies:** 2.2
- **Effort:** L
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`, `auth-patterns`
- **Validation:** `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed 1036/1036; `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed 295/295.

#### Task 2.4: Update API/HAL/Cerbos/Blazor Contracts

- **Type:** modify / docs / test
- **Layer:** API / Blazor / Authorization / Docs
- **Status:** Complete on 2026-05-27. The public contract now uses tenant-user-role-grant names, routes, DTOs, HAL policies, Cerbos resource kind, OpenAPI schemas, and generated Blazor client methods.
- **Files:** `Explore.Application/Features/TenantUserRoleGrants/**`, `Explore.Application/DTOs/TenantUserRoleGrant/**`, `Explore.API/Controllers/TenantUserRoleGrantController.cs`, `Explore.API/Hateoas/**TenantUserRoleGrant**`, `cerbos/policies/islamuevent_tenant_user_role_grant.yaml`, `cerbos/policies/_schemas/islamuevent_tenant_user_role_grant.json`, `schemas/openapi.json`, `Explore.API/@schemas/openapi.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/AUTHORIZATION.md`.
- **Description:** Replaced tenant-member endpoints/DTOs with explicit tenant user role grant contracts. UI action availability remains HAL `_links` driven.
- **Acceptance Criteria:**
  - [x] OpenAPI operation IDs are stable and intentional.
  - [x] HAL policies expose only executable role grant actions.
  - [x] Cerbos/local fallback evaluate the same tenant role attributes.
  - [x] API changelog documents breaking contract changes.
- **Dependencies:** 2.3
- **Effort:** XL
- **Validation:** Release build passed; application, infrastructure, architecture, persistence, focused OpenAPI invariant, and Blazor client tests passed. Full API integration remains blocked by unrelated existing seeded-data/auth/external-key failures.
- **Required Skills/Rules:** `api-controllers`, `api-hateoas`, `auth-patterns`
- **Validation:** `Event.API.IntegrationTests`; authorization parity tests; Blazor client tests where UI changes.

### Phase 3: Event Time And Schedule Invariants

- **Goal:** Make event/session temporal data deterministic and DB-protected.
- **Depends on:** Phase 1 event graph guardrails.
- **Relevant files:** `Explore.Domain/Event.cs`, `Explore.Domain/EventSession.cs`, `Explore.Domain/EventSessionIslamicAspect.cs`, schedule calculator service, EF configurations, registration/capacity handlers.
- **Related skills/rules:** `domain`, `dotnet-efcore-guidelines`, `cqrs-mediatr-guidelines`.
- **Acceptance criteria:** Event time fields have single write path, local projections are generated/verified, relative-time semantics are constrained, optional overlap prevention decision is recorded.
- **Verification:** Domain/application unit tests, persistence integration tests.
- **Rollback / failure handling:** Keep generated/projection changes in separate migration from overlap constraints.

#### Task 3.1: Formalize Event/Session Time Source Of Truth

- **Type:** modify / docs / test
- **Layer:** Domain / Application / Persistence / Docs
- **Files:** existing `Event.cs`, `EventSession.cs`, `EventSessionConfiguration.cs`, schedule projection services, docs.
- **Description:** Define whether UTC start/end are authoritative with app-calculated local projections, or PostgreSQL generated columns own projections. Implement one approach consistently.
- **Acceptance Criteria:**
  - [x] No handler/mapper/seed path writes local projection fields directly except approved method/generation.
  - [x] Tests prove projection updates on reschedule and timezone changes.
  - [x] DB checks reject invalid ranges.
- **Dependencies:** 1.2
- **Effort:** L
- **Required Skills/Rules:** `domain`, `application-layer`, `efcore-persistence`
- **Validation:** Release build, domain/application unit tests, persistence integration tests, architecture tests, and scoped whitespace checks passed for the Phase 3.1 files.

#### Task 3.2: Harden Prayer-Relative Scheduling

- **Type:** modify / migration / test
- **Layer:** Domain / Persistence / Application
- **Files:** existing `EventSessionIslamicAspect.cs`, `EventSessionIslamicAspectConfiguration.cs`, DTOs/handlers for Islamic aspect.
- **Description:** Ensure `StartTimeType`, `ReferencePrayer`, and `OffsetMinutes` express exact valid states and cannot drift from session start fields.
- **Acceptance Criteria:**
  - [x] Fixed sessions cannot require prayer reference/offset unless intentionally allowed.
  - [x] Relative sessions require prayer reference/offset.
  - [x] Handler validation and DB constraints agree.
- **Dependencies:** 3.1
- **Effort:** M
- **Required Skills/Rules:** `domain`, `efcore-migrations`
- **Validation:** Release build, domain/application unit tests, persistence integration tests, and architecture tests passed for this slice.

#### Task 3.3: Decide Session Conflict/Overlap Constraints

- **Type:** investigate / migration / test
- **Layer:** Domain / Persistence
- **Files:** existing `EventSessionConfiguration.cs`, docs.
- **Description:** Decide whether PostgreSQL exclusion constraints should prevent overlapping sessions by room/resource/event scope, or whether overlap is a business warning only.
- **Acceptance Criteria:**
  - [x] Decision records product behavior by event format and room/resource.
  - [x] If enforced, EF model metadata plus the PostgreSQL constraint applier adds the tested exclusion constraint/index.
  - [x] Warning-only behavior was rejected; docs and tests prove hard enforcement behavior instead.
- **Dependencies:** 3.1
- **Effort:** M
- **Required Skills/Rules:** `dotnet-efcore-guidelines`
- **Validation:** Release build, application unit tests, persistence integration tests, and architecture tests passed for this slice.

### Phase 4: Custom Property Governance Hardening

- **Goal:** Keep Layer 3 extensibility enterprise-safe, observable, and bounded.
- **Depends on:** Phase 0 approval.
- **Relevant files:** `docs/CUSTOM_PROPERTIES.md`, ADR-006, custom-property entities/configs/handlers/projection services, quota resolver.
- **Related skills/rules:** `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`.
- **Acceptance criteria:** Quotas and lifecycle are consistent across shared/event/session/template scopes; EAV promotion rule is enforced; projection lifecycle is operator-visible.
- **Verification:** Application unit tests, persistence integration tests, API integration tests for governance endpoints.
- **Rollback / failure handling:** EAV changes must be small and reversible; avoid broad projection rebuild changes in same PR as contract changes.

#### Task 4.1: Audit Quota Enforcement Coverage

- **Type:** investigate / test / docs
- **Layer:** Application / Persistence / Docs
- **Files:** existing `CustomPropertyQuotaResolver.cs`, `Explore.Application/Features/*CustomPropert*/**`, `docs/CUSTOM_PROPERTIES.md`.
- **Description:** Map every creation/update/value/projection path to quota checks and identify gaps.
- **Acceptance Criteria:**
  - [x] Matrix covers definitions, options, values, multi-values, templates, projections, and rebuild batch sizes.
  - [x] Missing quota enforcement is converted into tasks with exact handlers.
- **Dependencies:** 0.1
- **Effort:** M
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`
- **Validation:** `custom-property-quota-enforcement-audit.md` reviewed shared, event, session, template, template-sync, projection, dirty-scope, governance-report, and query/filter paths. Release build was green before docs edits; architecture tests passed after docs updates.

#### Task 4.2: Harden EAV Lifecycle And Promotion Rules

- **Type:** modify / test / docs
- **Layer:** Domain / Application / Persistence / API
- **Files:** existing custom-property definition/value/projection handlers; `docs/CUSTOM_PROPERTIES.md`; `docs/EXTENSIBILITY.md`.
- **Description:** Enforce reserved namespace/collision/promotion rules in one shared application service used by shared/event/session scopes.
- **Acceptance Criteria:**
  - [x] A standard semantic cannot be created as Layer 3 if mapped to Layer 2.
  - [x] Platform/sector namespaces are protected in handlers and authorization.
  - [x] Purge/retire semantics are consistent and documented.
- **Dependencies:** 4.1
- **Effort:** L
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`, `auth-patterns`
- **Validation:** application unit tests; API integration tests for create/update/purge.

#### Task 4.3: Add Operator-Facing Projection/Quota Signals

- **Type:** modify / test / docs
- **Layer:** Application / API / Observability / Docs
- **Files:** existing custom-property governance/projection admin handlers/controllers; `Explore.Application/Telemetry/BusinessMetrics.cs`; `docs/OPERATIONS.md`.
- **Description:** Add safe metrics/logging/report fields for quota usage, dirty-scope backlog, projection rebuild state, and blocked promotion/purge decisions.
- **Acceptance Criteria:**
  - [x] Metrics avoid high-cardinality raw property names unless explicitly bounded.
  - [x] Admin responses explain quota/purge/projection blockers.
  - [x] Operations docs include triage actions.
- **Dependencies:** 4.1
- **Effort:** M
- **Required Skills/Rules:** `error-tracking`, `api-controllers`
- **Validation:** unit tests for metrics tags; API tests for admin responses.

### Phase 5: Polymorphic Reference Registry

- **Goal:** Replace stringly typed polymorphism with governed registries and cleanup contracts.
- **Depends on:** Phase 0.
- **Relevant files:** `ExternalBinding`, `Notification`, custom-property entity type names, constants, repositories, docs.
- **Related skills/rules:** `domain`, `cqrs-mediatr-guidelines`, EF rules.
- **Acceptance criteria:** Allowed external/internal binding types and notification target types are centralized, validated, tested, and documented.
- **Verification:** Unit + persistence tests.
- **Rollback / failure handling:** Introduce registry validation before adding stricter DB constraints if data cleanup is needed.

#### Task 5.1: Define Reference Type Registry

- **Type:** create / modify / docs
- **Layer:** Domain / Application / Docs
- **Files:** existing `Explore.Domain/Constants/ExternalBindingTypes.cs`, possible new registry/service files, docs.
- **Description:** Define allowed target kinds, ID type, tenant-scope rules, cleanup behavior, and allowed external/internal type pairings.
- **Acceptance Criteria:**
  - [ ] Registry covers `ExternalBinding`, `Notification`, and custom-property entity type names or documents why not.
  - [ ] Each target type has ownership, tenant-scope, delete behavior, and validation rules.
- **Dependencies:** 0.1
- **Effort:** M
- **Required Skills/Rules:** `clean-architecture-rules`
- **Validation:** unit tests for registry.

#### Task 5.2: Enforce Registry In Handlers And Persistence

- **Type:** modify / migration / test
- **Layer:** Application / Persistence
- **Files:** `ExternalBindingConfiguration.cs`, `ExternalBindingRepository.cs`, provisioning handlers, notification handlers, custom-property handlers.
- **Description:** Validate polymorphic references at write time and add DB constraints where low-risk.
- **Acceptance Criteria:**
  - [ ] Invalid type/pair writes fail with predictable validation errors.
  - [ ] Existing managed-provider provisioning bindings remain valid.
  - [ ] Cleanup/delete semantics are documented for every target type.
- **Dependencies:** 5.1
- **Effort:** L
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`, `efcore-migrations`
- **Validation:** application + persistence tests.

### Phase 6: Lifecycle, Retention, Partitioning, And Operational Data

- **Goal:** Make data growth, soft delete, hard purge, and retention enterprise-operable.
- **Depends on:** Phase 1 and active email-dispatch workstream coordination.
- **Relevant files:** `AuditLog`, `Notification`, outboxes, email dispatch, contact share export, projection dirty scopes/status, idempotency, docs/operations.
- **Related skills/rules:** `outbox-pattern`, `error-tracking`, EF rules.
- **Acceptance criteria:** Every high-growth table has lifecycle class, retention policy, cleanup job or explicit no-cleanup rule, and tests/ops docs.
- **Verification:** Unit/integration tests for cleanup safety; docs tests if applicable.
- **Rollback / failure handling:** Cleanup jobs must default safe/off or dry-run until tested.

#### Task 6.1: Build Table Lifecycle Matrix

- **Type:** investigate / docs
- **Layer:** Docs / Persistence
- **Files:** new section in context; update `docs/OPERATIONS.md`.
- **Description:** Classify tables as core source-of-truth, PII, append-only audit/evidence, operational retry state, derived projection, cache/idempotency, or export artifact.
- **Acceptance Criteria:**
  - [ ] Matrix lists retention owner and default policy for each high-growth table.
  - [ ] Protected evidence tables are explicitly no-delete or archive-only.
  - [ ] Partitioning candidates are named with threshold.
- **Dependencies:** 0.1
- **Effort:** M
- **Required Skills/Rules:** `outbox-pattern`, `error-tracking`
- **Validation:** docs review.

#### Task 6.2: Implement Safe Cleanup For Eligible Tables

- **Type:** create / modify / test / docs
- **Layer:** Application / Persistence / API / DevOps
- **Files:** repositories/background services/options/health checks; `docs/OPERATIONS.md`; `docs/CONFIGURATION.md`.
- **Description:** Implement cleanup only for eligible cache/derived/completed operational data. Keep dead-letter/audit/evidence retention protected.
- **Acceptance Criteria:**
  - [ ] Cleanup is tenant-aware or explicitly host-admin/system-scoped with reason.
  - [ ] Dry-run/logging/metrics exist for destructive cleanup.
  - [ ] Tests prove dead-letter/audit/protected rows are not deleted.
- **Dependencies:** 6.1
- **Effort:** L
- **Required Skills/Rules:** `error-tracking`, `dotnet-efcore-guidelines`
- **Validation:** application/integration tests; operations docs.

#### Task 6.3: Decide Partitioning Implementation

- **Type:** investigate / migration / docs
- **Layer:** Persistence / DevOps / Docs
- **Files:** `docs/OPERATIONS.md`, migrations if implemented.
- **Description:** Decide whether to implement PostgreSQL partitioning for `audit_logs`, `notifications`, outboxes, dispatch attempts, projection dirty scopes, and exports now or leave threshold-based capacity plan.
- **Acceptance Criteria:**
  - [ ] Decision reflects self-host operational complexity.
  - [ ] If implemented, migrations/tests/runbooks prove partition create/attach/prune behavior.
  - [ ] If deferred, docs do not imply partitioning is current behavior.
- **Dependencies:** 6.1
- **Effort:** M/XL depending decision
- **Required Skills/Rules:** `dotnet-efcore-guidelines`, `agentic-research`
- **Validation:** persistence tests if implemented.

### Phase 7: API, Authorization, Documentation, And Final Verification

- **Goal:** Align contracts, HAL, policies, docs, schema, and full validation with the new data model.
- **Depends on:** Phases 1-6.
- **Relevant files:** `docs/**`, `schemas/islamu-event.md`, `Explore.API/**`, `Explore.Blazor.Client/**`, `cerbos/**`, generated API inventory.
- **Related skills/rules:** `api-controllers`, `api-hateoas`, `auth-patterns`, `error-tracking`.
- **Acceptance criteria:** Full build/tests pass; schema docs and API docs reflect actual model; auth and HAL affordances match.
- **Verification:** full test/build commands in section 14.
- **Rollback / failure handling:** If API breaks unexpectedly, update OpenAPI changelog and migration notes rather than adding compatibility shims unless user asks.

#### Task 7.1: Update Canonical Docs And Schema

- **Type:** docs
- **Layer:** Docs
- **Files:** `schemas/islamu-event.md`, `docs/DOMAIN.md`, `docs/MULTI_TENANCY.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`, `docs/CONFIGURATION.md`.
- **Description:** Bring docs in line with final implemented model and remove claims that are no longer true.
- **Acceptance Criteria:**
  - [ ] Schema DBML matches EF snapshot.
  - [ ] RLS/partitioning docs distinguish current behavior from planned behavior.
  - [ ] Breaking API/data model changes are documented.
- **Dependencies:** implementation phases
- **Effort:** L
- **Required Skills/Rules:** all loaded rules as applicable
- **Validation:** schema drift script; architecture/context tests.

#### Task 7.2: Regenerate API Inventory And Verify HAL/Auth Parity

- **Type:** test / docs
- **Layer:** API / Authorization / Blazor
- **Files:** generated API inventory; HAL policies; Cerbos policies/tests; Blazor HAL consumers.
- **Description:** Regenerate OpenAPI/inventory and ensure affordances come from HAL links only.
- **Acceptance Criteria:**
  - [ ] Operation IDs and endpoint classifications are valid.
  - [ ] Cerbos/local authorization parity tests pass.
  - [ ] Blazor does not inspect roles/claims for per-resource actions.
- **Dependencies:** 2.4 and other API changes
- **Effort:** M
- **Required Skills/Rules:** `api-hateoas`, `auth-patterns`
- **Validation:** API integration tests; Blazor client tests if touched.

## 7. Testing Strategy

Minimum by intent:

- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` when auth/RLS/cleanup/infrastructure is touched.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` when HAL-visible UI contracts change.

Specific new tests:

- Event graph tenant-safe FK tests reject representative cross-tenant and cross-event child/parent relationships.
- Missing tenant context does not return all tenant rows on runtime paths.
- RLS prototype proves tenant session variable behavior under context pooling.
- Tenant user role grant cannot exist without matching tenant-local user.
- Suspended/removed tenant user cannot retain effective admin authority.
- Event schedule projection recalculates and DB rejects invalid ranges.
- Prayer-relative session aspect constraints match handler validation.
- Custom-property quota/promotion/namespace/purge coverage across shared/event/session/template scopes.
- External binding registry rejects invalid type pairs.
- Cleanup job dry-run and destructive mode protect audit/dead-letter/evidence rows.

## 8. Documentation, Configuration, And Operations Impact

Docs likely updated:

- `schemas/islamu-event.md`
- `docs/DOMAIN.md`
- `docs/MULTI_TENANCY.md`
- `docs/SECURITY-MODEL.md`
- `docs/AUTHORIZATION.md`
- `docs/API.md`
- `docs/API_CHANGELOG.md`
- `docs/OPERATIONS.md`
- `docs/SELF_HOSTING.md`
- `docs/CONFIGURATION.md`
- `docs/BACKUP_RESTORE_UPGRADE.md`
- `docs/TROUBLESHOOTING.md`
- ADR if RLS/partitioning/tenant membership consolidation needs durable decision record.

Configuration impact:

- Possible RLS enablement option and migration role configuration.
- Cleanup/retention options per lifecycle class.
- Metrics/health options for cleanup and operational table growth.
- Potential API route/contract changes for tenant role grants.

Operations impact:

- Migration sequencing and backfill plan required.
- Rollback is corrective-migration based.
- Self-host runbooks must distinguish DB owners/app roles/migration roles if RLS lands.
- Backup/restore and tenant export/delete docs must reflect retention policy.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Tenant isolation is the core security concern. EF filters stay, but guardrails should add composite tenant FKs and possibly RLS.
- Authorization changes must keep Cerbos/local parity and fail closed.
- Tenant admin authority must depend on active tenant-local user state.
- UI cannot infer edit/delete/role actions from role claims; it must check HAL links.
- PII split tables remain; cleanup/export/delete policy must preserve audit requirements while handling PII erasure.
- External bindings and notifications must not leak cross-tenant IDs or invalid deep links.
- Retention/cleanup jobs must never delete protected audit/dead-letter/evidence rows without explicit operator policy.
- Logs/metrics must not include raw email addresses, tokens, provider secrets, raw JWTs, or high-cardinality property values.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

- **Multi-tenancy:** Applicable and primary. The workstream exists to harden shared-database tenant safety.
- **Federation:** Applicable to `ExternalBinding`, `Actor`, ATProto records, and PDS outbox. Do not break federation identity bindings; registry must include federation/internal binding types.
- **Localization:** Needs investigation only if API/Blazor text changes for admin lifecycle pages. Data model changes should keep localization neutral.
- **Accessibility:** Needs investigation only if Blazor admin UI changes are required. Any new controls must follow existing MudBlazor/design/accessibility rules.
- **Product:** Applicable. Tenant roles, event scheduling, custom properties, and retention directly affect enterprise self-host user trust and operations.

## 11. Observability And Operations

Add or verify:

- Metrics for tenant-role grant changes, cleanup outcomes, RLS/tenant-scope failures, custom-property quota usage, projection backlog, and operational table growth.
- Structured logs with `tenant_id`, `operation_name`, `system_scope_reason`, `actor_user_id`, `resource_type`, `resource_id`, `correlation_id`, `outcome`.
- Health checks for cleanup worker status and optional RLS/session-variable readiness if implemented.
- ProblemDetails for validation/authorization failures without sensitive details.
- Troubleshooting docs for tenant isolation failures, cleanup dry-run, and migration/RLS role issues.

## 12. Migration And Compatibility Plan

- Breaking changes are allowed by user instruction.
- Still use reversible EF migrations and clear backfills.
- Do not edit applied migrations.
- Avoid compatibility shims unless the user explicitly asks.
- Tenant membership consolidation status:
  1. The implemented migration creates `tenant_user_role_grants`, backfills from `tenant_members`, enforces tenant-local user and tenant-role-scope constraints, and drops `tenant_members` in the same development-mode breaking slice.
  2. Domain, persistence, admin context, onboarding, managed-provider provisioning, API/HAL, Cerbos, OpenAPI, and the generated Blazor client now use `TenantUserRoleGrant`.
  3. Public API consumers must migrate to `/api/tenant-user-role-grants`, `TenantUserRoleGrantDto/ListDto`, and create/revoke role-grant semantics.
- Composite tenant FKs should land by bounded graph, not one mega-migration.
- RLS should start with a prototype and operator role design before full enablement.
- Partitioning should not be documented as implemented until migrations/runbooks/tests exist.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Composite FKs reveal existing cross-tenant data inconsistencies | Medium | High | Inventory and backfill/cleanup before constraints | Migration failure or FK violation tests | 1.1, 1.2 |
| RLS misconfigured with pooled connections | Medium | Critical | Prototype first; interceptor tests; migration bypass role | Cross-tenant denial/allow test failures, production 403/empty data | 1.3 |
| Tenant membership consolidation breaks admin access | Medium | Critical | Transactional backfill; admin integration tests; bootstrap fallback review | Onboarding/admin tests fail | 2.2, 2.3 |
| API contract changes break Blazor/client generation | High | Medium | Regenerate OpenAPI, update client/services, API changelog | API integration/client tests fail | 2.4, 7.2 |
| Custom-property rules become duplicated across scopes | Medium | Medium | Shared service; matrix tests | Divergent handler behavior | 4.2 |
| Cleanup deletes protected evidence | Low | Critical | Dry-run first; protected lifecycle matrix; tests | Cleanup tests fail, audit gaps | 6.1, 6.2 |
| Work conflicts with active email-dispatch/event-role streams | Medium | Medium | Coordinate through existing dev docs; update context | Merge conflicts, duplicate models | 0.1, 6.1 |
| Tavily research requirement remains unresolved | Medium | Low/Medium | Retry when connector available; record fallback research | Missing Tavily evidence in context | 0.2 |

## 14. Success Metrics And Definition Of Done

Functional success:

- Tenant-local authority model has one clear root and one role grant path.
- Database constraints reject cross-tenant graph corruption.
- Custom-property governance is centrally enforced and observable.
- Event-time invariants are explicit and tested.
- Operational data lifecycle policy is implemented or explicitly deferred with thresholds.

Quality gates:

- Full Release build passes.
- Intent-derived unit/integration/API/architecture tests pass.
- Schema DBML matches EF model.
- OpenAPI contract and API changelog are updated for breaking changes.
- No new Clean Architecture violations.
- No repository returns DTOs.
- No unsafe `IgnoreTenantFilter(reason)` or `IgnoreQueryFilters()` without tests.

Definition of done:

- All completed phases update plan/context/tasks.
- Docs distinguish current behavior from planned behavior.
- User receives developer teaching summary for each implementation slice.
- Final context handoff lists changed files, validation, risks, and remaining work.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

Future agents implementing this plan MUST follow this contract:

1. Before starting any implementation slice, read this plan, `enterprise-data-model-hardening-context.md`, and `enterprise-data-model-hardening-tasks.md`.
2. Re-classify the slice against `.claude/contract/intents.yaml`.
3. Load the matching docs, skills, and rules before editing.
4. Start from the highest-priority incomplete task unless user instruction overrides it.
5. After completing each meaningful task or discovering new scope, update:
   - this plan if architecture/scope/phases/risks changed;
   - context with current state, decisions, files changed, blockers, validation, and next step;
   - tasks by checking completed items and adding discovered tasks.
6. Do not report done unless docs reflect actual current state.
7. If validation fails, update context/tasks with failure, root cause if known, and next recovery action.
8. Before pausing, context reset, handoff, or PR creation, refresh all three dev docs and add/refresh a handoff section.

## 16. Progress Reporting Contract

Implementation summaries should use:

- **Implemented:** Developer teaching summary naming patterns, libraries/infrastructure, important files/classes, and data/control flow.
- **Verified:** Exact commands and outcomes.
- **Remaining:** Known gaps and deferred tasks.
- **Next:** Highest-value next task.
- **Docs updated:** Whether plan/context/tasks and canonical docs were updated.

## 17. Potential Risks & Unknowns

The hardest part is not writing migrations; it is preserving tenant/admin access while tightening database constraints. `TenantUserRoleGrant` now gives tenant authority one database anchor under `TenantUser` and the public API/HAL/Cerbos/Blazor-client contract has been moved to that model. The safer enterprise path remains staged: continue with event-time invariants, EAV governance, polymorphic reference registry work, production RLS rollout design, and retention/partitioning. Tavily MCP research remains an explicit unknown because the requested MCP was not available in this session.
