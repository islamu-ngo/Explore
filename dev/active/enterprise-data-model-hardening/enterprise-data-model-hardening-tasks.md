<!-- ABOUTME: Tactical checklist for enterprise-grade ISLAMU Event data-model hardening. -->
<!-- ABOUTME: Tracks phased implementation tasks, acceptance criteria, validation, and dev-doc maintenance. -->

# Enterprise Data Model Hardening — Task Checklist

Last Updated: 2026-05-28 Europe/Brussels

## Status Summary

- **Overall status:** In implementation
- **Completed:** 16/25 implementation tasks
- **Current priority:** Phase 5.1 polymorphic reference registry; Phase 4 custom-property governance hardening is complete.
- **Next recommended slice:** Start Phase 5.1 with the reference type registry and tenant-scope/delete-behavior rules.
- **Handoff status:** Refreshed on 2026-05-28 after the Phase 4.3 operator-signal slice. Generated migration files are intentionally out of scope unless the user asks for regeneration.

## Implementation Maintenance Rules

- [x] Before starting work, read plan/context/tasks.
- [x] Re-classify the slice against `.claude/contract/intents.yaml`.
- [x] Load matching docs, rules, and skills before editing.
- [x] After each completed task, update this checklist immediately.
- [x] If implementation changes scope or architecture, update the plan before continuing.
- [x] If discoveries affect future work, update the context file.
- [x] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Research Closure

- [x] **0.1 User reviews and approves/corrects plan**
  - **Files:** `dev/active/enterprise-data-model-hardening/*`
  - **Acceptance:** plan status moves from Draft to User-reviewed/Approved; rejected recommendations are deferred with reason.
  - **Validation:** user instructed implementation to start on 2026-05-27; plan status moved to In implementation.
  - **Effort:** S
  - **Dependencies:** none

- [x] **0.2 Rerun Tavily MCP research if available**
  - **Files:** `enterprise-data-model-hardening-context.md`
  - **Acceptance:** Tavily findings are recorded, or unavailability is explicitly recorded with tool evidence.
  - **Validation:** tool discovery rechecked on 2026-05-27; Context7 available, Tavily still not exposed; context updated.
  - **Effort:** S
  - **Dependencies:** none

- [x] **0.3 Confirm active workstream coordination**
  - **Files:** `dev/active/event-scoped-operational-roles/**`, `dev/active/backend-api-health-refactor/**`, `dev/active/crmworx-event-api-adaptation/**`, `dev/active/rabbitmq-messaging/**`
  - **Acceptance:** overlaps and conflicts are listed in context before implementation.
  - **Validation:** active event-role, backend-health, CRMWorx/email-dispatch, and RabbitMQ workstream contexts reviewed; context updated.
  - **Effort:** S
  - **Dependencies:** 0.1

## Phase 1: Tenant Isolation Guardrails

- [x] **1.1 Generate tenant-scoped entity and FK inventory**
  - **Files:** `Explore.Domain/**/*.cs`, `Explore.Persistence/Configurations/Entities/**/*.cs`, `Explore.Persistence/ExploreDbContext.QueryFilters.cs`, `dev/active/enterprise-data-model-hardening/tenant-scoped-fk-inventory.md`, context file.
  - **Acceptance:** every strict tenant-scoped entity family and FK category is classified; first migration scope selected; seven missing strict tenant query filters corrected.
  - **Validation:** inventory reviewed against EF snapshot/configuration sources; strict `ITenantEntity` filter check now returns no missing classes.
  - **Effort:** M
  - **Dependencies:** 0.1

- [x] **1.2 Add composite tenant-safe keys/FKs for the event graph**
  - **Files:** event/session/day/agenda/group/registration/contact-share EF configs, `Explore.Domain/EventRegistration.cs`, registration handlers/repositories/profile, generated migration only when requested, `schemas/islamu-event.md`, `EventGraphTenantForeignKeyTests`.
  - **Acceptance:** DB rejects cross-tenant, cross-event, and cross-location room parent-child mismatches; valid graphs still persist; schema DBML updated.
  - **Validation:** build passed; persistence integration tests passed 116/116; architecture tests passed 176/177 with 1 skipped; `git diff --check` passed. No automated DBML parser was found, so schema verification is manual against the generated EF migration/snapshot.
  - **Effort:** XL
  - **Dependencies:** 1.1

- [x] **1.3 Decide and prototype PostgreSQL RLS**
  - **Files:** `Explore.Persistence/Security/PostgresTenantSessionInterceptor.cs`, `Explore.Persistence/PersistenceServicesRegistration.cs`, `Event.Persistence.IntegrationTests/TenantIsolation/PostgresTenantSessionRlsPrototypeTests.cs`, `docs/SECURITY-MODEL.md`, `docs/MULTI_TENANCY.md`, `docs/CONFIGURATION.md`.
  - **Acceptance:** pooled EF Core/Npgsql connection session-variable behavior proven; production table RLS explicitly deferred until app-role/migration-role/admin-path design is complete.
  - **Validation:** `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed 117/117.
  - **Effort:** L
  - **Dependencies:** 1.1

- [x] **1.4 Replace undocumented runtime tenant-filter bypasses**
  - **Files:** `Explore.Persistence/ExploreDbContext.cs`, `Explore.Persistence/ExploreDbContext.QueryFilters.cs`, `Explore.Persistence/QueryFilters/**`, tenant-aware repositories/services, `Explore.Persistence/Seed/DatabaseSeeder.cs`, persistence fixtures, `TenantQueryFilterFailClosedTests`, `PersistenceTenantFilterArchitectureTests`, multi-tenancy/security docs.
  - **Acceptance:** runtime bypasses require explicit reason/path/test; missing tenant context cannot return all tenant rows.
  - **Validation:** build passed; persistence integration tests passed 119/119; architecture tests passed 178/179 with 1 skipped.
  - **Effort:** L
  - **Dependencies:** 1.1

## Phase 2: Tenant User / Membership Consolidation

- [x] **2.1 Choose final tenant role grant model**
  - **Files:** `dev/active/enterprise-data-model-hardening/tenant-role-grant-model-decision.md`, plan/context/tasks, `TenantUser.cs`, former `TenantMember.cs`, role docs/code references.
  - **Acceptance:** decision records `TenantUserRoleGrant` entity shape, keys, lifecycle, role-scope validation, migration path, and API replacement direction.
  - **Validation:** repository-grounded decision documented; Context7 EF Core alternate-key guidance confirmed; implementation review still required in Phase 2.2.
  - **Effort:** M
  - **Dependencies:** 1.1

- [x] **2.2 Implement domain and persistence model**
  - **Files:** tenant role grant entity/config/repository, `ExploreDbContext.DbSets.cs`, generated migration only when requested, schema DBML.
  - **Acceptance:** role grants reference tenant-local user; wrong tenant/role scope rejected; old `tenant_members` data is backfilled into `tenant_user_role_grants`; old domain/persistence contract is removed; current public DTO/API names remain an intentional bridge until Phase 2.4.
  - **Validation:** `dotnet build --configuration Release --verbosity quiet` passed; `Event.Persistence.IntegrationTests` passed 121/121; `Event.Architecture.Tests` passed 178/179 with 1 skipped; `git diff --check` passed; `schemas/islamu-event.md` updated and manually reviewed against the EF model/migration.
  - **Effort:** XL
  - **Dependencies:** 2.1

- [x] **2.3 Update admin authority and provisioning flows**
  - **Files:** `AdminContext.cs`, tenant role repository, onboarding handler, managed-provider provisioning handler.
  - **Acceptance:** active tenant-local user status gates tenant authority; provisioning creates user/profile/role grant transactionally.
  - **Validation:** `Event.Application.UnitTests` passed 1036/1036; `Explore.Infrastructure.Tests` passed 295/295; API integration tests are deferred to Phase 2.4 because the public API/HAL contract was intentionally left bridged.
  - **Effort:** L
  - **Dependencies:** 2.2

- [x] **2.4 Update tenant role API/HAL/Cerbos/Blazor contracts**
  - **Files:** tenant member/role features, controllers, HAL policies, Cerbos policies, Blazor clients, API docs/changelog.
  - **Acceptance:** OpenAPI/HAL/auth parity updated; Blazor gates actions through `_links`; breaking changes documented.
  - **Validation:** Release build passed; `Event.Application.UnitTests` passed 1028/1028; `Explore.Infrastructure.Tests` passed 295/295; `Event.Architecture.Tests` passed 178/179 with 1 known skipped metadata test; `Event.Persistence.IntegrationTests` passed 121/121; focused API OpenAPI invariants passed 17/17; `Explore.Blazor.Client.Tests` passed 1209/1210 with 1 known skipped component-accessibility test. A full API integration run still fails on unrelated seeded-data/auth/external-key paths and is not counted as Phase 2.4 evidence.
  - **Effort:** XL
  - **Dependencies:** 2.3

## Phase 3: Event Time And Schedule Invariants

- [x] **3.1 Formalize UTC/local schedule source of truth**
  - **Files:** `Event.cs`, `EventSession.cs`, `EventSessionConfiguration.cs`, schedule projection services, docs.
  - **Acceptance:** one approved write path for local projections; timezone/reschedule tests pass; DB rejects invalid ranges.
  - **Validation:** Release build passed; domain unit tests passed 242/242; application unit tests passed 1029/1029; persistence integration tests passed 123/123 and cover invalid UTC ranges plus local minute projection constraints; architecture tests passed 178/179 with the known skipped response-metadata test; scoped `git diff --check` for Phase 3.1 files passed.
  - **Effort:** L
  - **Dependencies:** 1.2

- [x] **3.2 Harden prayer-relative scheduling**
  - **Files:** `EventSessionIslamicAspect.cs`, `EventSessionIslamicAspectConfiguration.cs`, related DTOs/handlers.
  - **Acceptance:** fixed/relative states are consistently validated in handlers and DB constraints.
  - **Validation:** Release build passed; domain unit tests passed 247/247; application unit tests passed 1039/1039; persistence integration tests passed 127/127; architecture tests passed 178/179 with the known skipped response-metadata test.
  - **Effort:** M
  - **Dependencies:** 3.1

- [x] **3.3 Decide session overlap/conflict constraints**
  - **Files:** `EventSessionConfiguration.cs`, model-owned PostgreSQL constraint applier, docs.
  - **Acceptance:** overlap behavior is DB-enforced: active sessions cannot overlap in the same tenant/location/room; adjacent sessions and different-room overlaps are valid; soft-deleted sessions release the room.
  - **Validation:** Release build passed; application unit tests passed 1039/1039; persistence integration tests passed 131/131; architecture tests passed 178/179 with the known skipped response-metadata test. Follow-up on 2026-05-28 moved the exclusion constraint source of truth from generated migration files into EF configuration plus `PostgresModelConstraintApplier`; API startup and migration service runtime paths now apply model-owned constraints after EF migrations, Development runtime contexts ignore `PendingModelChangesWarning`, Release build passes, architecture tests pass, and scoped `git diff --check` passes. The latest full persistence suite reached 132/134 and failed two unrelated email-dispatch transition tests.
  - **Effort:** M
  - **Dependencies:** 3.1

## Phase 4: Custom Property Governance Hardening

- [x] **4.1 Audit custom-property quota enforcement coverage**
  - **Files:** `CustomPropertyQuotaResolver.cs`, `Explore.Application/Features/*CustomPropert*/**`, `docs/CUSTOM_PROPERTIES.md`.
  - **Acceptance:** matrix maps every definition/option/value/projection path to quota checks.
  - **Validation:** `custom-property-quota-enforcement-audit.md` maps shared, event, session, template, sync, projection, dirty-scope, governance-report, and query/filter paths. Baseline Release build passed before edits; focused architecture tests passed after docs update.
  - **Effort:** M
  - **Dependencies:** 0.1

- [x] **4.2 Harden EAV lifecycle and promotion rules**
  - **Files:** custom-property handlers/services, docs, authorization policies if needed.
  - **Acceptance:** Layer 2 semantics cannot be recreated as Layer 3; namespace/collision/purge rules are centralized and tested; template and template-sync quota gaps from Phase 4.1 are closed before adding new lifecycle controls.
  - **Validation:** quota sub-slice passed `Event.Application.UnitTests` 1057/1057 and `Event.Architecture.Tests` 178/179 with one known skipped response-metadata test. Semantic-reservation sub-slice passed `Event.Application.UnitTests` 1062/1062 and `Event.Domain.UnitTests` 247/247. Purge/retire lifecycle sub-slice passed focused `PurgeCustomPropertyDefinitionCommandHandlerTests` 3/3, focused shared PostgreSQL purge guard 1/1, full `Event.Architecture.Tests` 178/179 with the known skipped response-metadata test, and scoped `git diff --check`. Later Phase 4.3 verification restored the full Release build and full `Event.Application.UnitTests` baseline; event-scoped PostgreSQL custom-property tests remain blocked by current migration drift (`events.content` missing).
  - **Progress:** Template and template-sync quota gaps are closed: event templates enforce option caps, session templates enforce definition and option caps, and event/session sync apply preflights resulting runtime definition and option cardinality before writes. Semantic key reservations now reject known event/session Layer 2 concepts even under tenant namespaces before duplicate repository reads or writes. Normal delete is documented as retirement that keeps machine keys reserved, and audited hard purge now has handler-level blocked responses, repository-level dependency rechecks, and restrictive EF model relationships for value/projection dependents.
  - **Effort:** L
  - **Dependencies:** 4.1

- [x] **4.3 Add operator-facing projection/quota signals**
  - **Files:** custom-property governance/projection admin handlers/controllers, `BusinessMetrics.cs`, operations docs.
  - **Acceptance:** safe metrics/logs/responses expose quota usage and projection backlog without sensitive/high-cardinality tags.
  - **Validation:** focused projection status query tests passed 2/2; focused event projection rebuild tests passed 7/7; focused session projection rebuild tests passed 3/3; full `Event.Application.UnitTests` passed 1065/1065; Release build passed; architecture tests passed 178/179 with the known skipped response-metadata test; scoped docs updated with bounded metric tags and triage states.
  - **Effort:** M
  - **Dependencies:** 4.1

## Phase 5: Polymorphic Reference Registry

- [ ] **5.1 Define reference type registry**
  - **Files:** `ExternalBindingTypes.cs`, new registry/service files if needed, docs.
  - **Acceptance:** allowed target kinds, ID type, tenant-scope, delete behavior, and type pairings are documented.
  - **Validation:** registry unit tests.
  - **Effort:** M
  - **Dependencies:** 0.1

- [ ] **5.2 Enforce registry in handlers and persistence**
  - **Files:** `ExternalBindingConfiguration.cs`, `ExternalBindingRepository.cs`, provisioning handlers, notification/custom-property handlers.
  - **Acceptance:** invalid type/pair writes fail predictably; existing managed-provider bindings remain valid.
  - **Validation:** application and persistence tests.
  - **Effort:** L
  - **Dependencies:** 5.1

## Phase 6: Lifecycle, Retention, Partitioning, And Operational Data

- [ ] **6.1 Build table lifecycle matrix**
  - **Files:** context, `docs/OPERATIONS.md`.
  - **Acceptance:** high-growth tables have lifecycle class, owner, default retention, and partitioning threshold.
  - **Validation:** docs review.
  - **Effort:** M
  - **Dependencies:** 0.1

- [ ] **6.2 Implement safe cleanup for eligible tables**
  - **Files:** repositories/background services/options/health checks, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`.
  - **Acceptance:** cleanup is tenant-aware or explicit system-scoped; dry-run/logging/metrics exist; protected rows are not deleted.
  - **Validation:** application/integration tests and operations docs.
  - **Effort:** L
  - **Dependencies:** 6.1

- [ ] **6.3 Decide partitioning implementation**
  - **Files:** `docs/OPERATIONS.md`, migrations if implemented.
  - **Acceptance:** partitioning is implemented with tests/runbooks or explicitly deferred without claiming current support.
  - **Validation:** persistence tests if implemented.
  - **Effort:** M/XL
  - **Dependencies:** 6.1

## Phase 7: API, Authorization, Documentation, And Final Verification

- [ ] **7.1 Update canonical docs and schema**
  - **Files:** `schemas/islamu-event.md`, `docs/DOMAIN.md`, `docs/MULTI_TENANCY.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`, `docs/CONFIGURATION.md`.
  - **Acceptance:** docs reflect implemented behavior; RLS/partitioning current vs planned status is accurate.
  - **Validation:** schema drift check; architecture/context tests.
  - **Effort:** L
  - **Dependencies:** implementation phases

- [ ] **7.2 Regenerate API inventory and verify HAL/auth parity**
  - **Files:** generated API inventory, HAL policies, Cerbos policies/tests, Blazor HAL consumers.
  - **Acceptance:** operation IDs/classifications valid; Cerbos/local parity passes; UI action gates use HAL links.
  - **Validation:** `Event.API.IntegrationTests`, authorization parity tests, Blazor client tests if touched.
  - **Effort:** M
  - **Dependencies:** 2.4 and all API changes

- [ ] **7.3 Run final full verification**
  - **Files:** entire repo.
  - **Acceptance:** required build/tests pass; failures are documented with root cause and recovery action.
  - **Validation:** full command list below.
  - **Effort:** M
  - **Dependencies:** all implementation tasks

## Verification Checklist

- [x] `dotnet build --configuration Release --verbosity quiet` passes. Latest Phase 4.3 operator-signal run passed on 2026-05-28 with existing warnings.
- [x] `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passes. Latest semantic-reservation-slice run passed 247/247.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passes. Latest Phase 4.3 operator-signal run passed 1065/1065.
- [ ] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passes. Earlier Phase 3.3 slice passed 131/131; latest 2026-05-28 full run reached 132/134 and failed two unrelated email-dispatch transition tests.
- [ ] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passes.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/*/OpenApiDocument_*" --minimum-expected-tests 5 --no-progress` passes.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passes. Latest Phase 4.3 operator-signal run passed 178/179 with the known skipped response-metadata test.
- [x] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passes when infrastructure/auth/RLS/cleanup changed.
- [x] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passes when Blazor/HAL client changed.
- [x] Schema DBML matches EF model for the implemented tenant role grant slice, manually reviewed against the EF migration/snapshot.
- [x] OpenAPI/API inventory regenerated when contracts change.
- [x] Docs updated where behavior/config/operations/API changed.
- [x] Dev docs refreshed with current Phase 3.3 implementation, 2026-05-28 runtime migration-startup fix, and remaining Phase 4+ work.

## Remaining / Deferred Work

- Tavily MCP research rerun if connector becomes available.
- Generated migration files remain intentionally out of scope in this development branch unless the user asks for migration regeneration.
- Tenant-per-database or schema-per-tenant hosting remains out of scope unless user starts a separate workstream.
- Generic runtime schema engine remains out of scope; custom properties stay bounded to existing resources.
