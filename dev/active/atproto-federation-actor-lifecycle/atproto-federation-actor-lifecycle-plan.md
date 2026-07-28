<!-- ABOUTME: Repository-grounded implementation plan for global Actor identity and global concrete subjects. -->
<!-- ABOUTME: Defines tenant participation, ATProto classification, federation promotion, moderation, and migration. -->

# ATProto Federation Actor Lifecycle - Implementation Plan

Last Updated: 2026-07-28 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Complete the global-subject architecture consistently. `Actor`, `User`, `Organization`, `Group`, and unclassified external subjects are global. `TenantUser`, `OrganizationTenant`, and `GroupTenant` own tenant-specific participation, policy, moderation, hierarchy, settings, and profile overrides. ATProto registration classifies a verified global identity without duplicating subjects per tenant.
- **Task directory:** `dev/active/atproto-federation-actor-lifecycle/`.
- **Implementation status:** Phases 0-5 are implemented. Four-level moderation, contextual Actor reads, legitimacy evidence, and final contract/UI convergence in Phases 6-7 remain open.
- **Superseded decisions:** Both `ActorTenantPresence` and the temporary return to tenant-scoped Actor are rejected. A generic presence row would duplicate concrete participation lifecycles.
- **Predecessor:** `dev/active/atproto-auth/` remains authoritative for implemented OAuth/DPoP verification, canonical `AtprotoRecord`, Jetstream, outbox, recovery, Event/EventSession materialization, source metadata, and zero echo.
- **Matched intents:** `add-ef-migration`, `update-repository-query`, `add-cqrs-handler`, `add-get-endpoint`, `add-write-endpoint`, `openapi-contract-change`, `add-hal-link`, `blazor-component-affordance`, and `bff-auth-bug`.
- **Skills/rules:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, and all matching Domain/Application/Persistence/Migration/API/HAL/Blazor/Test rules.
- **Primary layers:** Domain, Application, Persistence, Infrastructure, API/HAL/OpenAPI, Blazor BFF/client, schema migration, tests, and canonical docs.
- **Complexity:** XL. Current Actor, Organization, Group, Event, subscriptions, settings, memberships, hierarchy, storage, filters, and authorization encode tenant ownership in different ways.
- **Compatibility:** One reviewed maintenance-window cutover. No dual reads/writes, aliases, compatibility DTOs, or mixed-version support.

## 1. Executive Summary

The same real-world person, organization, or group must have one platform-global identity regardless of tenant participation. `Actor` is that global identity/subject abstraction. Its concrete owner is also global: User, Organization, Group, ExternalActorSubject, or ServicePrincipal. Tenant policy belongs to the concrete subject's tenant participation, not Actor.

The current repository does not implement that model consistently. Actor, Organization, and Group carry `TenantId`; Event and ActorSubscription use composite `(TenantId, ActorId)` foreign keys; Organization/Group approval and hierarchy live on the concrete entity; and inbound federation creates one Bot Actor per tenant for the same DID.

The corrected model removes tenant scope from Actor and concrete subjects, introduces `OrganizationTenant` and `GroupTenant`, retains `TenantUser`, and makes Event reference global Actor by `ActorId`. `AtprotoIdentity` is global, exact-DID authoritative, and points to its global Actor. An Actor may have several identities; one identity belongs to at most one Actor. Authentication links a verified identity to User, while representation may be a Person, Organization, or Group Actor.

Unknown imported DIDs create one global Actor plus `ExternalActorSubject`. Events materialized in any tenant reference that Actor directly; observation is derived from Event and `AtprotoRecordTenantPresentation`, not a presence row. Verified classification promotes the external subject in place, preserving Actor/Event IDs. Existing same-kind global subjects normally remain canonical during explicit consolidation. Different semantic kinds never auto-merge.

### Intended outcomes

- One Actor and one concrete global subject represent one real platform subject.
- One exact DID maps to one global `AtprotoIdentity`; identities can be moderated independently from Actor.
- Organizations and Groups participate in many tenants through explicit concrete associations.
- Tenant approval, local moderation, organizer eligibility, settings, hierarchy, profile overrides, and memberships never leak into global subjects.
- Actor moderation is platform-wide; identity moderation is credential-wide; participation/federation moderation is tenant-local; Event moderation is content-local.
- Imported events from one DID use the same Actor in every tenant.
- Every successful ATProto registration resolves a User login, then classifies or associates the global represented subject.
- Public Actor URLs are global; tenant-context views compose participation and tenant-local content.

### Non-goals

- No `ActorTenantPresence`, tenant-scoped Actor, name/email/handle auto-merge, generic identity framework, ActivityPub bridge, PDS/AppView/relay implementation, or compatibility layer.
- No automatic tenant participation merely because federated content was observed.
- No global-follow subscription product in this slice; current subscriptions remain explicitly tenant-contextual.
- No network/provider call inside a business transaction.
- No general document-management subsystem.

## 2. Source-Grounded Current State

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Consequence |
|---|---|---:|---|
| Actor is tenant-scoped and owns User/Organization/Group FKs today. | `Actor.cs`; `ActorConfiguration.cs`; Actor query filters | High | Remove tenant key/filter; retain one authoritative subject-owner direction and extend it for external subjects. |
| User is global, but Actor ownership/preference is duplicated. | `User.cs`; `TenantUser.cs`; configurations | High | Global personal Actor belongs to User; tenant membership/moderation remains TenantUser; preferred workspace Actor is tenant-scoped. |
| Organization is tenant-scoped and carries global plus local fields together. | `Organization.cs`; `OrganizationConfiguration.cs`; `OrganizationPii.cs` | High | Canonical identity/contact fields stay global; approval/local status/profile overrides move to OrganizationTenant. |
| Group is tenant-scoped and carries approval plus tenant hierarchy. | `Group.cs`; `GroupConfiguration.cs` | High | Canonical group fields stay global; approval, local profile, parent Organization/Group participation, and status move to GroupTenant. |
| Organization/Group members are tenant-scoped but reference concrete subjects directly. | member entities/configurations | High | Memberships must target OrganizationTenant/GroupTenant; uniqueness becomes participation plus User. |
| Organization/Group settings already carry TenantId. | `OrganizationSetting.cs`; `GroupSetting.cs`; query filters | High | Repoint them to concrete tenant participation rather than global subject alone. |
| Event and tenant ActorSubscription use composite Actor FKs. | `EventConfiguration.cs`; `ActorSubscriptionConfiguration.cs` | High | Replace with simple global Actor FK; handlers enforce tenant-context validity through participation or federated presentation. |
| ActorSubscription is explicitly tenant-local. | `ActorSubscription.cs` and configuration | High | Preserve local-follow semantics; do not silently convert it into a global follow. |
| DID truth is split across IndexedDid, ActorPii, and AtprotoRecord. | corresponding entities/configurations | High | Migrate the exact-DID union into global AtprotoIdentity and remove ActorPii DID authority. |
| Provider login lookup bypasses tenant filter without uniqueness. | `UserExternalLoginRepository.cs`; configuration | High | Make provider identity global/unique before onboarding. |
| Unknown inbound DID creates a tenant Bot Actor. | `AtprotoJetstreamRepository.ApplyEventImportsAsync` | High | Replace with one global ExternalUnclassified Actor/subject shared by all tenant Events. |
| Record, Actor/Event/session materialization, cursor, and fence already commit together. | `TryApplyAndAdvanceWithResultAsync`; `ApplyEventImportsAsync` | High | Global identity/Actor/external-subject creation joins the same transaction. |
| Organization governance settings exist but create handler does not consume them. | `OrganizationSettingDefinitions`; `OrganizationSettingGroup`; create handler | High | Normal and ATProto creation share one policy-aware operation. |
| Private storage/evidence patterns exist. | upload-session handlers; `StorageObject`; `EventReportEvidence` | High | OrganizationTenant evidence reuses private Document ownership and composite tenant FKs. |
| ATProto OAuth authenticates DID, not subject kind. | official OAuth/DID/profile specifications | High | Classification is explicit local business intent after verification, never profile inference. |

### 2.2 Current Ownership And Policy Coupling

- `Actor`, `Organization`, and `Group` all receive named tenant filters and soft-delete filters.
- `Organization.ApprovalStatusId`, approval audit, TenantId, and ActorId mix global identity with local onboarding.
- `Group.ApprovalStatusId`, TenantId, parent Organization/Group, profile picture, and ActorId mix canonical identity with tenant hierarchy and local assets.
- Current Organization/Group creation performs several writes without one explicit `IUnitOfWork` boundary.
- `OrganizationMember` uniqueness is `(OrganizationId, UserId)` and `GroupMember` uniqueness is `(GroupId, UserId)`, which cannot represent the same User holding separate tenant-local memberships in one global subject.
- Actor profile media points to tenant-scoped `StorageObject`; those FKs cannot remain on a global Actor without widening storage scope unsafely.
- Current ActorSubscription targets Organization/Group Actor in one tenant; the composite FK currently relies on Actor tenancy rather than an explicit eligibility rule.

### 2.3 Existing Tests And Gaps

- Existing Organization tests protect tenant-admin Approved versus ordinary Pending behavior, membership, Actor creation, update authorization, and concurrency.
- Existing Group tests protect Pending creation, hierarchy, membership, approval updates, APIs, HAL, and services.
- Existing federation tests protect canonical records, tenant materialization, replay, tombstone, recovery, source links, and zero echo.
- Missing: global subject uniqueness, participation isolation, User Actor deduplication, DID Actor deduplication, hierarchy migration, tenant-local subscription eligibility, global/identity/local moderation separation, external classification, and cross-tenant Actor reuse.

### 2.4 Documentation And Official Constraints

- Canonical owners: `docs/DOMAIN.md`, `MULTI_TENANCY.md`, `AUTHORIZATION.md`, `SECURITY-MODEL.md`, `FEDERATION.md`, `API.md`, `API_CHANGELOG.md`, `BLAZOR.md`, `BACKUP_RESTORE_UPGRADE.md`, `schemas/islamu-event.md`, and a new ADR.
- Preserve ADR-015 canonical record/materialization rules.
- Official ATProto constraints: DID is permanent/global/case-sensitive with a 2048 maximum; handle/PDS/signing key are mutable; OAuth requires unique state, PKCE, PAR, DPoP, expected-DID/token-subject, and authoritative issuer/PDS verification; profile Lexicon has no subject-kind field.
- EF Core constraints: named tenant filters belong on tenant rows; unique/partial indexes enforce races; required-navigation filters can hide parents unexpectedly; data-moving migrations require reviewed SQL.

### 2.5 Unknowns Assigned To Implementation

| Unknown | Resolution |
|---|---|
| Complete disposition of every Actor/Organization/Group FK | Task 1.1 produces a manifest classifying global identity, tenant participation, current mutable ownership, immutable evidence, and tenant-observation references. |
| Global Actor uploaded media without a global storage scope | Initial cutover moves tenant StorageObject FKs to participation profile overrides; canonical global profile uses safe identity URI/CID metadata. A global uploaded-asset model requires separate approval. |
| Existing same-name Organizations across tenants | Never merge by name. Each becomes a separate global subject unless User ownership or exact verified DID proves identity. |
| Existing conflicting profiles while deduplicating User/DID Actors | Migration uses explicit precedence and preserves losing tenant-safe values as participation overrides where possible; otherwise aborts for reviewed mapping. |

## 3. Proposed Future State

### 3.1 Global identity graph

```text
AtprotoIdentity --many-to-one--> Actor --exactly-one--> User
                                      |-------------> Organization
                                      |-------------> Group
                                      |-------------> ExternalActorSubject
                                      |-------------> ServicePrincipal
```

- `Actor`: global ID, kind, canonical public profile, global status, aliases/merge evidence, audit/soft delete.
- `AtprotoIdentity`: global exact DID, mutable verified handle/PDS/key/cache state, `ActorId`, identity moderation state. Many identities may reference one Actor; one identity references at most one Actor.
- Ownership direction is single-source: Actor keeps the subject discriminator FKs and database XOR constraint; concrete entities expose inverse navigation but no duplicate stored ActorId.
- `User`, `Organization`, and `Group` have no TenantId or tenant filter.
- `ExternalActorSubject` is global and owns the temporary unclassified Actor state until promotion.

### 3.2 Concrete tenant participation

```text
User         -> TenantUser
Organization -> OrganizationTenant
Group        -> GroupTenant
```

- `OrganizationTenant`: Id, OrganizationId, TenantId, approval/status, local visibility/moderation, organizer eligibility, local profile/contact/media overrides, approval audit, concurrency, audit, soft delete; unique `(TenantId, OrganizationId)`.
- `GroupTenant`: Id, GroupId, TenantId, approval/status, local visibility/moderation, local profile/media overrides, tenant hierarchy, concurrency, audit, soft delete; unique `(TenantId, GroupId)`.
- Group parent relationships target `OrganizationTenant` or `GroupTenant`, because hierarchy is tenant context.
- OrganizationMember/GroupMember and OrganizationSetting/GroupSetting target their participation aggregate.
- Observation alone does not create participation. Imported Event plus record presentation proves tenant observation.

### 3.3 Event, authorization, and subscriptions

- Event remains tenant-scoped and references global Actor with a simple FK.
- Local Event creation resolves Actor kind to global subject, then requires active matching TenantUser/OrganizationTenant/GroupTenant plus current User authority.
- Imported events may reference ExternalUnclassified Actor without participation; federation capability/presentation policy governs them.
- Current ActorSubscription remains tenant-local. It keeps TenantId, subscriber TenantUser, and global TargetActorId; creation/read/fanout require that the Actor is discoverable in that tenant through active participation or public federated materialization.
- A future global follow is a separate product contract and table, not an implicit semantic change.

### 3.4 Registration and classification

1. BFF completes the existing verified OAuth flow and binds classification intent to authenticated session, tenant, expected DID, issuer/PDS, nonce, expiry, and antiforgery state.
2. Server resolves/creates global AtprotoIdentity, global User login, and current TenantUser idempotently.
3. Classification is explicit Person, Organization, or Group local business intent; it is not inferred from ATProto metadata.
4. Person links/promotes Actor to User and ensures current TenantUser.
5. Organization links/promotes Actor to one global Organization and creates/resolves current OrganizationTenant under existing self-registration/verification policy; registrant becomes tenant OrgAdmin.
6. Group links/promotes Actor to one global Group and creates/resolves current GroupTenant under existing self-registration/hierarchy policy; registrant becomes tenant GroupAdmin.
7. The same DID can authenticate User while representing Organization/Group because UserExternalLogin targets identity and identity targets represented Actor; audit remains User.

### 3.5 Federation before and after classification

- First observation of unknown DID creates one global AtprotoIdentity, Actor(kind ExternalUnclassified), and ExternalActorSubject in the existing fenced record/materialization transaction.
- Tenant A and Tenant B Events reference the same Actor; no participation row is created by observation.
- Direct classification replaces external ownership in place and preserves Actor/identity/Event IDs.
- Only the onboarding tenant receives new TenantUser/OrganizationTenant/GroupTenant. Other tenants keep federated Events without local management participation until their own authorized association flow.
- When a same-kind existing global subject is explicitly proven, that existing Actor normally remains canonical; mutable references move, identity points to canonical Actor, and ActorMerge preserves source/canonical evidence.
- Different kinds or conflicting User owners fail closed for explicit review.

### 3.6 Four-level moderation

| Level | Target | Scope | Example |
|---|---|---|---|
| Subject | Actor | Platform-wide | fraud, impersonation, legal prohibition, severe cross-tenant abuse |
| Credential | AtprotoIdentity | Global external identity | compromised DID/key/PDS while other Actor identities remain valid |
| Participation/federation | TenantUser, OrganizationTenant, GroupTenant, or tenant identity policy | One tenant | local ban, pending verification, local import block |
| Content | Event/report decision | One tenant content item | event takedown or redaction |

Actor-wide action requires instance authority. Tenant admins mutate concrete participation only. Before classification, tenant-local federation policy may target AtprotoIdentity; it is not Actor presence.

### 3.7 Profiles, URLs, and evidence

- `/actors/{actorId}` serves canonical public identity.
- `/t/{tenantSlug}/actors/{actorId}` composes canonical identity with participation, local visibility/profile overrides, and tenant-local Event counts.
- Tenant-owned StorageObject profile media moves to TenantUser/OrganizationTenant/GroupTenant overrides. Global Actor retains safe canonical URI/CID metadata until a dedicated global storage scope is approved.
- Organization legitimacy evidence targets OrganizationTenant and private tenant StorageObject, because approval is tenant-local.

## 4. Non-Negotiable Constraints

- Repositories return entities; handlers map DTOs; validators are manually instantiated.
- Actor and every concrete owner are global. No tenant filter or TenantId is allowed on Actor, User, Organization, Group, ExternalActorSubject, or AtprotoIdentity.
- Every Actor has exactly one concrete global owner; every concrete owner has exactly one Actor. One authoritative FK direction only.
- Tenant authority comes only from TenantUser, OrganizationTenant, GroupTenant, tenant federation policy, or tenant-owned content, never global Actor existence.
- No ActorTenantPresence or generic polymorphic participation row.
- No auto-merge by name, email, handle, URL, address, or profile similarity.
- Only verified OAuth/session flows create identity/login/claim links; classification is local intent, not protocol truth.
- Multi-write creation, promotion, consolidation, and evidence attachment use `IUnitOfWork`; provider/storage I/O remains outside transactions.
- HAL links are the sole UI affordance authority.
- Preserve canonical records, outbox, recovery, cursor fencing, materialization, source metadata, and zero echo.

## 5. Architecture Decisions

### A1. Global Actor and global concrete subjects
- **Decision:** Remove TenantId from Actor, Organization, and Group; add global ExternalActorSubject.
- **Why:** One real subject must retain one identity across tenants.
- **Rejected:** One Actor per tenant; global Actor with tenant-local duplicate Organizations/Groups.
- **Consequence:** Tenant policy moves to concrete participation aggregates.

### A2. No ActorTenantPresence
- **Decision:** Use TenantUser, OrganizationTenant, GroupTenant, Event, record presentation, and tenant identity policy.
- **Why:** Their lifecycles differ and a generic row creates conflicting status truth.
- **Consequence:** Observation can exist without membership or managed participation.

### A3. AtprotoIdentity points to global Actor
- **Decision:** Promote IndexedDid into AtprotoIdentity with nullable-during-migration then required ActorId; no separate tenant binding table.
- **Why:** One DID represents one global subject, while an Actor may have several identities.
- **Consequence:** UserExternalLogin authenticates User through identity; identity independently identifies represented Actor.

### A4. Actor owns the concrete-subject discriminator
- **Decision:** Retain Actor-side unique User/Organization/Group owner FKs, add ExternalActorSubject/ServicePrincipal owner, remove duplicate owner-side ActorId columns.
- **Why:** The existing PostgreSQL XOR check can enforce exactly one subject without dual writes or polymorphic FKs.
- **Consequence:** Concrete subjects expose inverse one-to-one navigation only.

### A5. OrganizationTenant and GroupTenant own local policy
- **Decision:** Move approval, status, local moderation/visibility, settings, media overrides, organizer eligibility, and Group hierarchy to participation.
- **Why:** Those values can differ across tenants while canonical identity remains shared.
- **Consequence:** Memberships/settings/hierarchy authorize through participation IDs.

### A6. Event references global Actor directly
- **Decision:** Replace composite `(TenantId, ActorId)` FK with simple ActorId FK.
- **Why:** Actor is global; tenant authorization is a business rule over concrete participation, not identity storage.
- **Consequence:** Local writes prove participation; imports prove tenant presentation/materialization policy.

### A7. Imported external subject is global and unclassified
- **Decision:** One DID creates one global ExternalUnclassified Actor/subject; no tenant row is created merely for observation.
- **Why:** The same remote publisher can appear in many tenants before local classification.
- **Consequence:** Direct promotion preserves Actor/Event IDs.

### A8. Merge only with proof
- **Decision:** Existing global User ownership or exact verified DID may establish identity; same name/profile never does.
- **Why:** Global deduplication is an account-takeover boundary.
- **Consequence:** Unproven current tenant Organizations/Groups migrate as distinct global subjects.

### A9. Subscription semantics stay tenant-local
- **Decision:** Preserve current ActorSubscription product meaning and replace only its composite Actor FK.
- **Why:** Converting to global follow would be an unrequested behavior change.
- **Consequence:** Handler requires local discoverability/participation; global follow is deferred.

### A10. Storage remains tenant-scoped
- **Decision:** Move uploaded Actor/Organization/Group media to participation overrides; keep canonical URI/CID fields global.
- **Why:** Reusing tenant StorageObject globally would leak tenant ownership and authorization.
- **Consequence:** Global uploaded assets require a separate reviewed storage-scope design.

## 6. Implementation Phases

### Phase 0: Close Public Identity CRUD
- **Goal:** Remove client-asserted identity ownership before changing claim semantics.
- **Depends on:** None.
- **Verification:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- **Rollback:** Do not restore generic CRUD; add only purpose-built verified/self-scoped contracts.

#### Task 0.1: Remove public UserExternalLogin and IndexedDid CRUD
- **Type/Layer:** delete/modify; Application/API/Blazor/Docs.
- **Files:** existing controllers, DTO/CQRS/HAL/routes/OpenAPI/generated client/tests/docs; internal persistence retained until migration.
- **Acceptance:** No public request can assert UserId/DID/provider/PDS/key ownership; verified auth/import internals compile; breaking deletion is documented.
- **Dependencies/Effort:** None; M.

### Phase 1: Define Global Subjects And Concrete Participation
- **Goal:** Establish exact domain ownership and field/FK disposition before schema work.
- **Depends on:** Phase 0.
- **Verification:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`.
- **Rollback:** Any unclassified FK or dual source of truth blocks migration scaffolding.

#### Task 1.1: Define global Actor/identity/concrete-subject contracts and ADR
- **Type/Layer:** modify/create; Domain/Docs/Tests.
- **Files:** Actor/User/Organization/Group/ActorPii/IndexedDid; new AtprotoIdentity, ExternalActorSubject, ActorMerge, Actor/identity moderation records; ADR and architecture tests.
- **Acceptance:** Global entities have no TenantId/filter; Actor owner XOR is exact and single-direction; identity-to-Actor cardinality supports many credentials; DID/handle/PDS semantics match protocol; complete Actor FK disposition manifest exists.
- **Dependencies/Effort:** 0.1; XL.

#### Task 1.2: Define OrganizationTenant/GroupTenant and local field ownership
- **Type/Layer:** create/modify; Domain/Docs/Tests.
- **Files:** new participation entities/status/profile overrides; Organization/Group/member/settings/hierarchy/subscription/storage models; ADR manifest.
- **Acceptance:** Approval/moderation/visibility/settings/hierarchy/local media belong to participation; memberships target participation; observation requires no participation; no generic presence model appears.
- **Dependencies/Effort:** 1.1; XL.

### Phase 2: Deterministic Globalization Migration
- **Goal:** Move current tenant rows into global subject plus participation shape without guessed identity merges.
- **Depends on:** Phase 1.
- **Verification:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- **Rollback:** Mandatory backup/restore; Down refuses lossy de-globalization.

#### Task 2.1: Implement reviewed migration, preflight, filters, and FK conversion
- **Type/Layer:** create/modify; Persistence/Docs.
- **Files:** configurations, DbContext sets/filters, all Actor/Organization/Group FKs, guarded `GlobalizeAtprotoActorLifecycle` and `RetireIndexedDidAuthority` migrations/designers/snapshot, schema/upgrade docs, PostgreSQL tests.
- **Acceptance:** User actors deduplicate by global User proof; exact-DID actors deduplicate only under compatible owner-kind rules; unproven Organizations/Groups remain distinct; current org/group rows create participation; hierarchy/members/settings/media move correctly; Event/subscription Actor FKs become simple; identity union migrates; row counts/FKs/audit preserved or migration aborts.
- **Dependencies/Effort:** 1.1, 1.2; XL.

### Phase 3: Participation-Aware Repositories And Creation
- **Goal:** Make normal product flows obey global subject plus local participation semantics.
- **Depends on:** Phase 2.
- **Verification:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`.
- **Rollback:** Tenant-boundary failures are fixed in shared repositories/policy operations, not patched in controllers.

#### Task 3.1: Replace global-subject and participation repository/query contracts
- **Type/Layer:** modify; Application/Persistence.
- **Files:** Actor/User/Organization/Group repositories, new participation repositories, specifications, membership/settings/hierarchy/subscription queries, filters/tests.
- **Acceptance:** Global reads are explicit; tenant listings start from participation or tenant content; membership uniqueness includes participation; no tenant filter bypass substitutes for authorization.
- **Dependencies/Effort:** 2.1; XL.

#### Task 3.2: Refactor normal Organization/Group creation and updates
- **Type/Layer:** modify; Application/API/Docs.
- **Files:** create/update/approval handlers, shared policy-aware operations, memberships, storage ownership, tests/docs.
- **Acceptance:** Creation makes one global subject/Actor plus current participation transactionally; existing self-registration/verification policy is authoritative; founder admin is tenant-local; updates distinguish canonical versus tenant override fields; no name auto-merge.
- **Dependencies/Effort:** 3.1; XL.

### Phase 4: Global Federated Identity And External Materialization
- **Goal:** Reuse one global Actor for a DID across all tenant Event materializations.
- **Depends on:** Phase 3.
- **Verification:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- **Rollback:** Fence loss or uniqueness conflict rolls back identity, Actor, external subject, Event/session, and cursor together.

#### Task 4.1: Materialize unknown DID as one global external subject
- **Type/Layer:** modify; Persistence/Application/Docs.
- **Files:** Jetstream repository/import plans, AtprotoIdentity repository, Actor/external creation, Event FK, federation tests/docs.
- **Acceptance:** Same DID in multiple tenants creates one identity/Actor/external subject; tenant Events remain distinct and stable; no participation or Bot fallback; replay/recovery/tombstone/zero-echo remain idempotent.
- **Dependencies/Effort:** 3.1; XL.

#### Task 4.2: Refresh mutable identity/profile metadata safely
- **Type/Layer:** modify; Infrastructure/Application/Persistence.
- **Files:** constrained identity/PDS gateway, cache/options, AtprotoIdentity/Actor profile mapping, tests/config/docs.
- **Acceptance:** Auth resolution cache is under ten minutes; non-auth refresh is bounded; handle is bidirectionally verified/lowercase; PDS/key migration does not change Actor; optional profile failure never rejects Event; SSRF/size/time limits remain.
- **Dependencies/Effort:** 4.1; L.

### Phase 5: Verified Registration, Promotion, And Consolidation
- **Goal:** Authenticate User and classify/associate the global represented subject without tenant duplication.
- **Depends on:** Phase 4.
- **Verification:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- **Rollback:** Verification/classification conflict creates no partial account, subject, participation, or membership.

#### Task 5.1: Add protected classification onboarding
- **Status:** Complete as of 2026-07-27 Europe/Brussels.
- **Type/Layer:** create/modify; BFF/Application/API.
- **Files:** auth state/assertion, bootstrap command/result, classification contracts, User/login/TenantUser/global subject/participation operations, tests/security docs.
- **Acceptance:** OAuth controls remain; every success resolves User/login/TenantUser; Person/Organization/Group is explicit intent; managed subject creates/resolves current participation under policy; same DID may authenticate User and represent managed Actor; audit remains User; replay is safe.
- **Implemented:** Classification is signed and validated end-to-end; the linked-account bootstrap transaction preflights all no-write conflicts before resolving the User, personal Actor, represented global subject, current participation, and founder membership; cross-kind identities return `classification_conflict`; provider/key uniqueness is database-enforced across the full 2,048-character DID boundary; session JWT issuance remains post-commit.
- **Evidence:** Release build 0 errors; focused Application 9/9, BFF 18/18, API JWT 6/6, Infrastructure gateway 12/12, architecture 15/15, and PostgreSQL baseline guards 4/4 passed; EF model parity is clean and guarded migration SQL was reviewed.
- **Dependencies/Effort:** 3.2, 4.2; XL.

#### Task 5.2: Promote external subject or consolidate proven same-kind subject
- **Type/Layer:** create/modify; Domain/Application/Persistence.
- **Files:** internal promotion/consolidation command, owner/FK operations, ActorMerge, memberships/participation, tests/docs.
- **Acceptance:** Direct promotion preserves Actor/identity/Event IDs; only onboarding tenant gains participation; existing proven same-kind Actor normally remains canonical; mutable references move; immutable evidence remains; different-kind/User conflict fails closed.
- **Evidence:** `AtprotoSubjectOnboardingOperation` promotes external Actors in place or consolidates only into an explicit same-kind canonical Actor after concurrency, active-state, approved-participation, and current-tenant OrgAdmin/GroupAdmin checks. The handler prepares encryption once, reloads the current User and tracked personal Actor inside each retry, commits onboarding plus prepared OAuth-session persistence atomically, and issues JWTs post-commit. Active identity/Event/EventSeries/speaker/subscription references move; consent and historical evidence remain; `ActorMerge` stores identity ID plus bounded DID digest. Migration `20260728143000_ClassifyExternalUnclassifiedActors` inserts lookup ID 6, backfills legacy external BOT Actors, and enforces owner/type alignment.
- **Verification:** Task 5.2 boundary Release build 0 errors; focused Application 17/17, EF retry tracking 1/1, BFF 20/20, Infrastructure 13/13, actor-lifecycle architecture 4/4, migration-test compilation, and final Oracle review passed. Idempotent SQL was reviewed. PostgreSQL execution remains environment-blocked by unavailable Docker sockets; unrelated concurrent `AtprotoJetstreamRepository` DTO dependency, registration-model snapshot drift, and later `MinorUnitMath` compile failure remain owned elsewhere.
- **Dependencies/Effort:** 5.1; XL.

### Phase 6: Moderation, Authorization, Profiles, And Subscriptions
- **Goal:** Enforce global versus tenant-local effects and expose coherent identity views.
- **Depends on:** Phase 5.
- **Verification:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- **Rollback:** Any widening of tenant authority over global Actor/identity blocks the phase.

#### Task 6.1: Implement four-level moderation and participation-aware Event authorization
- **Type/Layer:** create/modify; Domain/Application/Persistence/API/HAL.
- **Files:** moderation entities/commands/policies, Event actor resolver, federation policy, public specifications, audit/tests/docs.
- **Acceptance:** Actor decision is platform-wide/instance-only; identity decision blocks that credential; tenant admins affect only participation or tenant identity import policy; Event decisions remain content-local; public reads apply all relevant levels.
- **Dependencies/Effort:** 5.2; XL.

#### Task 6.2: Add global/contextual Actor reads and preserve local subscription semantics
- **Type/Layer:** modify; Application/API/HAL.
- **Files:** Actor/profile DTOs/queries/controller/HAL, organization/group tenant views, ActorSubscription handlers/config, tests/docs.
- **Acceptance:** Global URL exposes canonical safe data; tenant view composes local participation/content; private cross-tenant data never leaks; subscription remains tenant-contextual and requires local discoverability; no Actor presence row.
- **Dependencies/Effort:** 6.1; L.

### Phase 7: Evidence, Blazor, Contracts, And Canonical Docs
- **Goal:** Finish pending Organization proof and converge every consumer/guardrail.
- **Depends on:** Phase 6.
- **Verification:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.
- **Rollback:** Generated artifacts are regenerated, never hand-edited; evidence does not auto-approve.

#### Task 7.1: Add OrganizationTenant legitimacy evidence
- **Type/Layer:** create/modify; Domain/Application/Persistence/API/HAL/Blazor.
- **Files:** evidence entity/config/repository/contracts/commands, existing upload sessions/storage checks, participation HAL/UI/tests/schema/docs.
- **Acceptance:** Pending OrganizationTenant admins can update local/canonical fields according to authority and attach active private Document storage owned by that participation; tenant admin reviews separately; composite tenant FKs, retention, audit, and no content/key leakage are enforced.
- **Dependencies/Effort:** 6.1; L.

#### Task 7.2: Reconcile OpenAPI/client/UI/docs and architecture guardrails
- **Type/Layer:** modify; API/Blazor/Docs/Tests.
- **Files:** routes/HAL/OpenAPI/generated client/serializers/onboarding/profile components/localization, canonical docs/ADR/schema/contract inventory/architecture tests.
- **Acceptance:** UI is HAL-driven; global and tenant URLs/states render; removed CRUD is absent; generated client converges; docs distinguish global subject from participation; tests forbid tenant Actor, tenant Organization/Group, ActorTenantPresence, composite Event-Actor FK, ActorPii DID authority, and client-side authorization inference.
- **Dependencies/Effort:** 6.2, 7.1; XL.

## 7. Testing Strategy

Each phase runs exactly one Release build plus its selected fastest relevant non-browser test project after all phase tasks. Persistence tests prove PostgreSQL migration, global identity races, tenant isolation, FK conversion, and federation replay. Application tests prove policy, authorization, and creation/promotion decisions. BFF tests prove OAuth/classification state binding. API/client tests prove moderation, HAL, contracts, and rendering. Existing unrelated architecture baseline failures must be resolved by their owning workstream, not bypassed here.

## 8. Documentation, Configuration, And Operations

- Update all canonical docs listed in current state, new ADR, schema, OpenAPI/client artifacts, and API changelog in owning tasks.
- No new broker, cache provider, service, environment variable, or deployment resource is required.
- Cutover requires database backup, reviewed preflight report and SQL, maintenance window, forward-only startup, and restore rollback.

## 9. Security, Authorization, Privacy, And Abuse

- Global subject discovery grants no tenant authority.
- OAuth retains unique state, PKCE, PAR, DPoP, expected DID/token subject, authoritative issuer/PDS checks, antiforgery, nonce/expiry/single use, and SSRF hardening.
- No name/email/handle/profile matching may merge global subjects.
- Tenant admins cannot globally suspend Actor or identity.
- Global canonical PII and tenant overrides follow separate access, erasure, and disclosure policies.
- Evidence uses private storage and emits no content/provider keys in logs, metrics, ProblemDetails, or support artifacts.

## 10. Cross-Cutting Classification

| Concern | Status | Reason |
|---|---|---|
| Multi-tenancy | Applicable | Concrete participation, content, storage, settings, and authorization remain tenant-scoped. |
| Federation | Applicable | DID-to-global-Actor identity and materialization are central. |
| Security/privacy | Applicable | Global merge and OAuth are takeover boundaries; canonical/local PII split changes erasure. |
| Authorization/HAL | Applicable | Global versus tenant authority must be server-authored and HAL-driven. |
| Localization/accessibility | Applicable | Classification, profile context, moderation, evidence, and conflicts add UI states. |
| Observability | Applicable | Bounded identity/merge/moderation/migration outcomes need diagnostics. |
| New infrastructure | Not applicable | PostgreSQL, current storage, BFF, and ATProto gateways suffice. |

## 11. Observability And Recovery

- Emit bounded outcomes for identity resolution, deduplication, participation creation, external promotion, merge conflict, moderation level, and evidence attachment.
- Never emit DID documents, handles, subject IDs, tenant IDs, provider bodies, evidence names/content, or storage keys unless an existing bounded policy explicitly permits the field.
- Preflight outputs counts and conflict categories only; sensitive row-level mapping remains protected operator evidence.
- Existing ATProto readiness remains. No worker or new health check is introduced.

## 12. Migration And Compatibility

- Build identity groups from global UserId and exact DID only. Names and profiles are not identity evidence. The cutover uses two sequential migrations so legacy IndexedDid metadata is promoted only after the global identity table exists.
- Each current Organization/Group row becomes a global subject plus one tenant participation unless a compatible exact DID proves consolidation.
- Deduplicate User Actors across tenants by UserId; prefer valid current User ownership, then deterministic oldest ID, preserving tenant overrides.
- Convert Event and ActorSubscription Actor FKs from composite to simple; retain tenant indexes for queries.
- Move approval/hierarchy/settings/membership/local media state to participation before removing TenantId.
- Down cannot reconstruct duplicated tenant subjects after consolidation; restore backup is the rollback.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Signal | Owner |
|---|---:|---:|---|---|---|
| False global Organization/Group merge | Medium | Critical | Exact DID/User proof only; no name match | Preflight conflict | 2.1 |
| Tenant policy remains on global entity | Medium | High | Field/FK manifest plus guardrails | Architecture/persistence tests | 1.2, 2.1 |
| Event write bypasses participation | Medium | Critical | Shared EventActorResolver policy | Negative Application/API tests | 6.1 |
| Global profile media leaks tenant storage | Medium | High | Move local FKs to participation; URI/CID global only | FK/storage tests | 1.2, 2.1 |
| External observation grants membership | Medium | High | No participation on import | Federation persistence tests | 4.1 |
| Actor suspension available to tenant admin | Low | Critical | Instance-only policy and HAL | Authorization tests | 6.1 |
| Subscription semantics silently become global | Medium | Medium | Preserve tenant contract explicitly | API tests | 6.2 |

## 14. Definition Of Done

- Actor and every concrete subject are global; tenant participation is explicit and type-specific.
- Exact-one Actor ownership and exact DID identity are database-enforced.
- Organization/Group policy, hierarchy, membership, settings, local profile/media, and moderation live on participation.
- Events across tenants from one DID reuse one Actor without creating membership.
- Registration, promotion, same-kind consolidation, conflict, four-level moderation, evidence, global/contextual profiles, and local subscription semantics have automated coverage.
- No ActorTenantPresence, tenant Actor, tenant Organization/Group, public identity CRUD, or compatibility semantics remain.

## 15. Implementation Agent Contract

1. Read all three files once initially; on resume read context/tasks then only the current plan section.
2. Start from the highest-priority unchecked task unless the user overrides it.
3. Treat tasks as the hot ledger; check substantial work immediately and small work by phase end.
4. Keep implementation and verification checkboxes separate.
5. Update context for decisions, blockers, failures, completed phases, discoveries, or handoff; update plan only for strategy changes.
6. Run only one Release build and the listed test project once after each phase; do not start app/browser/live services.
7. Preserve unrelated changes and never claim completion when repository state and ledger differ.
8. Final summaries teach architecture, files, data flow, security/reliability, verification, and remaining work.

## 16. Progress Reporting Contract

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: tasks/context/plan status
```

## 17. Potential Risks & Unknowns

The highest-risk work is the one-time globalization migration. It must distinguish proven identity from coincidental similarity, split mixed global/local fields before dropping tenant columns, and preserve every Event/audit reference. The implementation must not proceed from Task 1.2 to migration until the FK/field disposition manifest is exhaustive and reviewed.
