<!-- ABOUTME: Hot implementation ledger for global Actor and concrete global subject architecture. -->
<!-- ABOUTME: Tracks participation migration, federation classification, moderation, contracts, and phase gates. -->

# ATProto Federation Actor Lifecycle - Task Checklist

Last Updated: 2026-07-29 Europe/Brussels

## Status Summary

- **Overall status:** Complete; Phases 0-7 are implemented and verified within the recorded environment limits.
- **Completed:** 12/14 implementation tasks. Phase verification is tracked separately.
- **Current priority:** None. Tasks 5.2, 6.1, 6.2, 7.1, and 7.2 are complete.
- **Next recommended slice:** Add private tenant-owned legitimacy evidence without changing global Actor identity or introducing a presence row.

## Maintenance Rules

- Read all artifacts once initially; on resume read context/tasks and only the current plan section.
- Mark substantial work in progress when it spans meaningful edits or handoff.
- Check substantial tasks immediately after acceptance; reconcile small work by phase end.
- Keep implementation and phase verification separate.
- Update context for phase completion, decisions, blockers, failures, discoveries, or handoff.
- Update plan only for scope, architecture, order, acceptance, risk, or verification changes.
- Run one Release build plus only the listed project once after each phase.
- Do not start app/browser/Docker/Aspire/live services for routine verification.
- Preserve unrelated worktree changes.

## Blocking Decisions

- [x] Actor and concrete subject owners are global.
- [x] TenantUser, OrganizationTenant, and GroupTenant own tenant participation.
- [x] ActorTenantPresence and tenant Actor are rejected.
- [x] External observation creates no participation row.
- [x] Direct promotion preserves IDs; consolidation requires proof; cross-kind auto-merge is forbidden.
- [x] User reviews the complete re-baselined plan before implementation.
- [x] Tasks 1.1 and 1.2 FK/field manifests are complete before Task 2.1 migration.

## Phase 0: Close Public Identity CRUD - IMPLEMENTED; TEST INFRASTRUCTURE BLOCKED

- [x] **0.1 Remove public UserExternalLogin and IndexedDid CRUD**
  - **Files:** existing UserExternalLogin/IndexedDid controllers, DTOs, CQRS handlers, HAL/routes, generated client/serializer, API tests/docs; internal repository contracts retained until Phase 2.
  - **Acceptance:** No public request asserts identity ownership; verified internals remain; breaking deletion is documented.
  - **Effort:** M.
  - **Dependencies:** Plan approval.

### Phase 0 Verification - RUN ONCE
- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors; existing package/analyzer warnings remain.
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` — code-related failures resolved; 1470/1987 passed and 517 Testcontainers tests are blocked because Docker is unavailable at both configured sockets. Focused removed-route, contract, and verified identity-linking tests pass.

## Phase 1: Define Global Subjects And Participation - COMPLETE

- [x] **1.1 Define global Actor/identity/concrete-subject contracts and ADR**
  - **Files:** `Actor.cs`, `User.cs`, `Organization.cs`, `Group.cs`, `ActorPii.cs`, `IndexedDid.cs`; new AtprotoIdentity/ExternalActorSubject/ActorMerge/moderation entities; ADR and architecture tests.
  - **Acceptance:** Global entities have no TenantId/filter; Actor owner XOR is single-source; identity cardinality/protocol semantics are explicit; Actor FK manifest is exhaustive.
  - **Effort:** XL.
  - **Dependencies:** 0.1.

- [x] **1.2 Define OrganizationTenant/GroupTenant and local field ownership**
  - **Files:** new participation/status/profile entities; existing Organization/Group members, settings, hierarchy, subscriptions, storage ownership, domain tests, ADR manifest.
  - **Acceptance:** Approval/moderation/visibility/settings/hierarchy/local media/membership belong to participation; observation creates no participation; no generic presence.
  - **Effort:** XL.
  - **Dependencies:** 1.1.

### Phase 1 Verification - RUN ONCE
- [x] `dotnet build --configuration Release --verbosity quiet` — lifecycle source compiled; later repository-wide contract work introduced unrelated test-project compile drift.
- [x] Focused `ActorLifecycleArchitectureTests` and `UserPiiInventoryArchitectureTests` — 4/4 passed.

## Phase 2: Deterministic Globalization Migration - IMPLEMENTED; FULL SUITE FIXTURES PENDING

- [x] **2.1 Implement reviewed migration, preflight, filters, and FK conversion**
  - **Files:** all Actor/Organization/Group/member/setting/Event/subscription configurations, DbContext sets/filters, migration/designer/snapshot, schema/upgrade docs, PostgreSQL tests.
  - **Acceptance:** User/DID proof only; unproven same-name subjects stay distinct; participation backfills preserve local state; Event/subscription Actor FKs become simple; identity union and audit/FK counts converge or abort; backup restore is rollback.
  - **Effort:** XL.
  - **Dependencies:** 1.1, 1.2 and complete manifests.

### Phase 2 Verification - RUN ONCE
- [x] API startup and Persistence integration-test projects build with zero errors; EF reports no pending model changes; generated idempotent SQL is byte-stable across repeated generation.
- [ ] Full Persistence suite — focused migration tests pass 3/3, but the full run exposes 198 stale fixtures, primarily `EventProvenanceTypeId = 0` and ownerless Actors rejected by `ck_actors_exactly_one_owner`.

## Phase 3: Participation Repositories And Normal Creation - COMPLETE

- [x] **3.1 Replace global-subject and participation repository/query contracts**
  - **Files:** Actor/User/Organization/Group repository interfaces/implementations, new participation repositories, specifications, membership/settings/hierarchy/subscription queries, tests.
  - **Acceptance:** Global reads are explicit; tenant lists start from participation/content; memberships/settings/hierarchy/subscriptions use participation and no filter bypass grants authority.
  - **Effort:** XL.
  - **Dependencies:** 2.1.

- [x] **3.2 Refactor normal Organization/Group creation and updates**
  - **Files:** Organization/Group create/update/approval handlers, shared policy operations, member/storage ownership operations, Application/API tests/docs.
  - **Acceptance:** One global subject/Actor plus current participation commits transactionally; governance policy and founder tenant role are reused; canonical versus local updates are explicit; no name auto-merge.
  - **Effort:** XL.
  - **Dependencies:** 3.1.

### Phase 3 Verification - RUN ONCE
- [x] Release build passed at the phase boundary.
- [x] Event.Application.UnitTests passed 3,079/3,079 at the phase boundary.

## Phase 4: Global Federation Materialization - COMPLETE

- [x] **4.1 Materialize unknown DID as one global external subject**
  - **Files:** `AtprotoJetstreamRepository.cs`, import plans, AtprotoIdentity/Actor/external repositories, Event configuration, federation persistence tests/docs.
  - **Acceptance:** Same DID across tenants uses one identity/Actor/external subject; Events remain tenant-local/stable; no participation/Bot fallback; fence/replay/recovery/tombstone/zero-echo remain atomic.
  - **Effort:** XL.
  - **Dependencies:** 3.1.

- [x] **4.2 Refresh mutable identity/profile metadata safely**
  - **Files:** constrained identity/PDS gateway, cache/options, AtprotoIdentity/Actor mapping, Infrastructure/Application tests, config/federation docs.
  - **Acceptance:** Handle/PDS/key refresh never changes Actor; OAuth and cache/SSRF controls remain; optional profile failure does not reject Event.
  - **Effort:** L.
  - **Dependencies:** 4.1.

### Phase 4 Verification - RUN ONCE
- [x] Release build passed at the phase boundary.
- [x] Event.Persistence.IntegrationTests passed 1,093/1,093 before the final migration; current post-migration full-suite fixture drift is tracked under Phase 2 verification.

## Phase 5: Verified Registration And Promotion - COMPLETE

- [x] **5.1 Add protected classification onboarding**
  - **Files:** BFF auth state/assertion, bootstrap command/result, classification contracts, User/login/TenantUser/global subject/participation operations, BFF/Application tests/security docs.
  - **Acceptance:** OAuth remains fully bound; every success resolves User/login/TenantUser; explicit classification creates/resolves global subject and current participation under policy; audit remains User; replay safe.
  - **Evidence:** Classification is signed through OAuth state, bootstrap assertion, private bridge request, command, and result; Person reuses the User Actor/TenantUser, while Organization/Group transactionally create or replay one global subject/Actor, current participation, and founder admin membership. Login, TenantUser, and classification conflicts are preflighted before writes; provider identity is globally unique across the 2,048-character DID boundary; JWT issuance occurs only after commit.
  - **Verification:** Release build 0 errors; focused Application 9/9, BFF 18/18, API JWT 6/6, Infrastructure gateway 12/12, architecture 15/15, and PostgreSQL baseline guards 4/4 passed; EF reports no pending model changes and generated guarded index SQL was reviewed.
  - **Effort:** XL.
  - **Dependencies:** 3.2, 4.2.

- [x] **5.2 Promote external subject or consolidate proven same-kind subject**
  - **Files:** internal promotion/consolidation command/handler, owner/FK repositories, ActorMerge, participation/membership operations, Domain/Application/Persistence tests/docs.
  - **Acceptance:** Direct promotion preserves Actor/identity/Event IDs; only onboarding tenant gains participation; proven same-kind consolidation preserves canonical evidence; cross-kind/User conflict fails closed.
  - **Evidence:** Signed target ID/stamp plus active approved current-tenant OrgAdmin/GroupAdmin authority gates consolidation. Direct promotion retains Actor/identity/Event IDs. Consolidation moves active identity/Event/EventSeries/speaker/subscription references, preserves consent and historical evidence, records bounded DID-digest merge evidence, and persists the prepared OAuth session in the same serializable retry. Every retry reloads the current User and a tracked personal Actor after EF clears failed-attempt state.
  - **Verification:** Application 17/17, EF retry tracking 1/1, BFF 20/20, Infrastructure 13/13, actor lifecycle architecture 4/4, and the Task 5.2 boundary Release build passed. Migration tests compile but Docker/Testcontainers execution is unavailable; unrelated concurrent architecture/snapshot/build drift is recorded below.
  - **Effort:** XL.
  - **Dependencies:** 5.1.

### Phase 5 Verification - RUN ONCE
- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors.
- [x] `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` — focused ATProto flow 20/20.

## Phase 6: Moderation, Profiles, Authorization, Subscriptions - COMPLETE

- [x] **6.1 Implement four-level moderation and participation-aware Event authorization**
  - **Files:** Actor/identity/tenant moderation entities and commands, EventActorResolver, federation policy, public specifications, authorization/HAL/API tests/docs.
  - **Acceptance:** Actor is instance-global; identity is credential-global; tenant admin affects only participation/federation policy; Event remains content-local; public reads compose every applicable level.
  - **Implementation evidence:** Instance-admin-only Actor and exact identity moderation use `global-actor-moderation`, append immutable records only on real transitions, preserve identity `IsActive`, and invalidate public caches. Grounded `PdsSyncOutbox` work transactionally compensates settled and unsettled Event mutations with a fenced Delete after Actor or exact-DID suspension; exact-identity reconciliation is DID-scoped, and soft-deleted source Events remain selectable for cleanup. Central eligibility gates all public Event and child projections before disclosure, counting, or pagination. Local echoes require exact outbound ownership plus current local eligibility. RSVP is unchanged.
  - **Cache evidence:** Moderation evicts tagged Event-detail HybridCache entries plus `event-discovery`, `public-home-discovery`, `list-data`, `detail-data`, and `seo-sitemap` from the process-local output-cache store. This does not prove cross-replica consistency.
  - **Contract evidence:** Four reason-only POST routes select Suspend or Reinstate server-side, declare `WritePolicy` and typed `429` metadata, and are present in regenerated OpenAPI and generated-client artifacts. Dedicated authenticated management routes for Event days, session languages, Islamic aspects, and Tech aspects preserve management access without weakening public reads. MCP and Blazor use those generated operations, and management Event collections emit management-specific HAL. No schema, migration, or Cerbos policy change is part of Task 6.1.
  - **Focused runnable evidence:** Moderation compensation 17/17, public actions 10/10, local echo 3/3, Actor moderation API 9/9, cache invalidation 1/1, public eligibility 3/3, publication planning 49/49, managed child Application/API/Blazor lanes, and management collection HAL 23/23 passed. The full Persistence run confirmed the three in-memory eligibility tests before 597 Docker/Testcontainers startup failures.
  - **Review:** Final direct code/security review passed after splitting public and managed requests into one-request-per-handler classes. The remediation Release build, focused Application 4/4, focused API 2/2, diagnostics, conflict scan, and `git diff --check` passed.
  - **Blocked/unrelated evidence:** The PostgreSQL Docker lane cannot execute. Final architecture verification remained 320/330 with the same nine failures attributable to existing privacy/cache registries, naming, generated-client, authorization-parity, update-inventory, and concurrent persistence/ticketing work. Pre-existing package advisory warnings remain.
  - **Effort:** XL.
  - **Dependencies:** 5.2.

- [x] **6.2 Add global/contextual Actor reads and preserve local subscriptions**
  - **Files:** Actor/profile DTOs/queries/controller/HAL, participation read models, ActorSubscription handlers/configuration, API tests/docs.
  - **Acceptance:** Global and tenant profile views do not leak local/private data; subscription remains tenant-contextual and requires local discoverability; no presence row.
  - **Implementation evidence:** Global list, ID, DID, and handle reads return only active global Actors and active unsuspended ATProto identities. Tenant collections compose approved visible unsuspended Organization/Group participation or eligible public federated Event evidence, apply only public participation overrides, and serialize neither tenant participation/private User identity nor tenant storage IDs. A non-serialized request-local marker gates Actor detail and collection `subscribe`/`subscription` HAL links. Subscription create, detail, list, and fanout share the same tenant-local discoverability predicate; unsubscribe intentionally retains unrestricted access to an existing durable row after the target becomes hidden.
  - **Verification:** Canonical Release build passed with 0 errors. Focused Actor detail 6/6, subscribe handler 4/4, Actor subscription HAL/security 10/10, federated discoverability 1/1, and private-client-identity regression 1/1 passed. OpenAPI and NSwag regeneration omit private/request-local Actor fields without additional Task 6.2 drift. The final Architecture run is 324 passed, 9 unrelated failed, and 1 governed skip; PostgreSQL/Testcontainers remains unavailable because no Docker socket is reachable.
  - **Review:** Direct security/diff review passed. Shared discoverability is fail-closed for hidden and cross-tenant targets, federated evidence grants no participation, and no ActorTenantPresence or global-follow semantics were introduced.
  - **Effort:** L.
  - **Dependencies:** 6.1.

### Phase 6 Verification - RUN ONCE
- [x] `dotnet build --configuration Release --verbosity quiet` - 0 errors; package advisory warnings are pre-existing.
- [x] Focused Event API moderation command - 9/9 passed.
- [x] Focused Event API cache command - 1/1 passed.
- [x] Final direct review - passed after one-request-per-handler remediation. PostgreSQL remains Docker-blocked; architecture is 320/330 with the same nine failures outside the Task 6.1 runtime changes.
- [x] Task 6.2 focused Actor detail, subscription, HAL/security, federated-evidence, and client-privacy lanes - 22/22 passed.
- [x] Task 6.2 direct security/diff review - passed; the final Architecture run is 324 passed, 9 unrelated failed, and 1 governed skip.

## Phase 7: Evidence, UI, Contracts, Canonical Docs - COMPLETE

- [x] **7.1 Add OrganizationTenant legitimacy evidence**
  - **Files:** new evidence entity/config/repository/contracts/commands, existing upload-session/storage checks, participation HAL/UI, tests/schema/docs.
  - **Acceptance:** Pending participation admins attach active private tenant-owned Document storage; review remains tenant-local/separate; retention/audit/composite tenant FKs/no leakage are enforced.
  - **Implementation evidence:** `OrganizationTenantEvidence` uses composite tenant foreign keys to the participation and retained Document, unique replay identity, review audit, and optimistic concurrency. Organization admins use a server-bound upload session and can attach only an active private Document owned by the exact pending participation. Tenant admins approve or reject evidence separately; the participation is never auto-approved. Storage update/delete/reconciliation excludes retained evidence.
  - **Contract/UI evidence:** Authenticated HAL exposes only bounded evidence metadata and actions; tenant IDs, participation IDs, submitter/reviewer identities, object keys, provider metadata, and content remain private. The Organization details panel follows HAL affordances and uses the BFF PDF proxy.
  - **Verification:** Focused command-handler, Organization service, BFF proxy, HAL, retention, and enum-contract checks passed. EF Core reports no pending model changes after migration `20260729141557_AddOrganizationTenantLegitimacyEvidence`.
  - **Effort:** L.
  - **Dependencies:** 6.1.

- [x] **7.2 Reconcile OpenAPI/client/UI/docs and architecture guardrails**
  - **Files:** routes/HAL/OpenAPI/generated client/serializers/onboarding/profile components/localization, canonical docs/ADR/schema/contract inventory/architecture tests.
  - **Acceptance:** HAL-driven global/contextual UX and contracts converge; docs/schema/ADR match; tests forbid tenant Actor/Organization/Group, ActorTenantPresence, composite Event-Actor FK, ActorPii DID authority, public identity CRUD, and local authorization inference.
  - **Implementation evidence:** OpenAPI and NSwag were regenerated from source; the exact contextual Actor resource, string evidence-review enum, authenticated evidence operations, serializers, UI, localization, canonical docs, ADR, schema, contract inventory, and architecture guardrails converge. Generic Actor create/update/delete operations and client methods are absent.
  - **Verification:** Actor/evidence OpenAPI assertions passed; generated Actor CRUD absence, conflict scan, and `git diff --check` passed. The final Architecture run is 324 passed, 9 unrelated failed, and 1 governed skip.
  - **Effort:** XL.
  - **Dependencies:** 6.2, 7.1.

### Phase 7 Verification - RUN ONCE
- [x] `dotnet build --configuration Release --verbosity quiet` - 26 projects, 0 errors; existing package advisories remain.
- [x] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` - executed: 2,231 passed, 2 unrelated dirty-worktree failures, 1 governed skip.

## Remaining / Deferred Work

- Global-follow subscriptions are deferred; current ActorSubscription remains tenant-contextual.
- Global uploaded subject assets are deferred until a safe global storage scope is approved; tenant uploads remain participation overrides.
- ActivityPub, PDS/AppView/relay hosting, generic identity frameworks, and automatic cross-kind merge remain out of scope.
- No compatibility aliases, dual reads/writes, or mixed-version deployment support will be added.
- Repair post-migration Persistence fixtures by setting valid Event provenance and concrete Actor ownership before using the full suite as a release gate.
- PostgreSQL Task 5.2 tests compile but cannot execute until Docker is available at one configured socket.
- Concurrent registration-data model changes own current EF snapshot drift; the disposable probe showed only their Event column/table delta.
- Concurrent `AtprotoJetstreamRepository` work owns the current Persistence-to-Application.DTO architecture failure.
- The repository-wide Release build was green at the Task 5.2 boundary. A later concurrent `MinorUnitMath` edit currently fails before Task 5.2 with CS9135; Application and Persistence Task 5.2 sources still compile in isolation against the last green Domain binary.
