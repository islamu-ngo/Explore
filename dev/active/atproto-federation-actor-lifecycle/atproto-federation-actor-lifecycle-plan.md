<!-- ABOUTME: Repository-grounded implementation plan for Actor-first ATProto federation identity and lifecycle. -->
<!-- ABOUTME: Defines global Actor identity, verified claiming, tenant presence, moderation, and materialized discovery. -->

# ATProto Federation Actor Lifecycle - Implementation Plan

Last Updated: 2026-07-25 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Correct the ATProto federation architecture so Actor is the durable identity/profile and ownership subject, User is optional, imported identities can later be claimed by verified login, no email matching occurs, and an existing federated Actor wins any identity merge without rewriting imported Events.
- **Task directory:** `dev/active/atproto-federation-actor-lifecycle/`
- **Planning status:** Draft, awaiting user review.
- **Related completed workstream:** `dev/active/atproto-auth/`. Its OAuth, canonical-record, Jetstream, outbox, projection, and tenant-local Event materialization implementation is reused rather than rebuilt. Its linked-account-only product constraint is superseded only by this workstream after approval.
- **Matched intents:** `add-ef-migration`, `update-repository-query`, `add-cqrs-handler`, `add-get-endpoint`, `add-write-endpoint`, `openapi-contract-change`, `add-hal-link`, `blazor-component-affordance`, `bff-auth-bug`, and `external-infrastructure-bootstrap` where self-hosting/configuration documentation changes.
- **Relevant skills:** `implementation-plan`, `clean-architecture-rules`, `auth-patterns`, `blazor-bff-patterns`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `blazor-ui-conventions`, and `outbox-pattern` for any durable profile-refresh work discovered during implementation.
- **Relevant rules:** `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `.claude/rules/domain.md`, `.claude/rules/application-layer.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/efcore-migrations.md`, `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/blazor-server.md`, `.claude/rules/blazor-client.md`, and `.claude/rules/tests.md`.
- **Primary layers:** Domain, Application, Persistence, Infrastructure, API/HAL/OpenAPI, Blazor client, migrations, and product/operator documentation.
- **Complexity:** XL. `Actor` is currently tenant-scoped and participates in at least 21 persistence configurations, imported publishers are duplicated per tenant, User and Actor contain competing ownership links, and the corrected claim/merge flow crosses verified OAuth, tenant membership, global identity, moderation, public discovery, and destructive-schema migration boundaries.
- **Compatibility posture:** The product is pre-1.0, so no compatibility DTOs, dual reads/writes, aliases, or deprecated routes are added merely to preserve the current incorrect model. Data migration must still preserve durable identities, imported Event ownership, audit evidence, and externally meaningful DIDs.

## 1. Executive Summary

ATProto federation already imports canonical records and materializes a tenant-local `Event` and `EventSession`, but it creates a separate tenant-scoped `Actor` for the same DID in every tenant. Authentication separately requires an existing `UserExternalLogin`, which prevents a verified ATProto identity from claiming the Actor that federation already created. `Actor.UserId`, `User.ActorId`, and `User.DefaultActorId` also leave ownership ambiguous.

This workstream makes `Actor` global and durable. A DID identifies at most one Actor across the installation. Tenant visibility and participation move to a separate `ActorTenantPresence`; account membership remains in `TenantUser`. `User.ActorId` becomes the only personal-Actor ownership link and remains nullable because Actors may be unclaimed. `DefaultActorId` remains a UI/workspace preference, not ownership. Public Actor profile fields remain on the Actor/ActorPii aggregate, while User-only identity data stays private and email becomes optional.

Inbound federation first resolves or creates the global Actor by DID, records tenant presence, and materializes Events directly against that Actor. A later cryptographically verified ATProto login atomically creates or links a User to the existing Actor. If an explicitly linked Keycloak account already has a local personal Actor, the DID-bearing federated Actor is canonical; mutable ownership references are moved to it, immutable evidence remains historically attributable through a merge record, and already-imported Events need no rewrite.

Public discovery becomes materialized-only: `AtprotoEventProjection` remains canonical ingestion state, source metadata, and recovery evidence, but no projection-only item is returned as a public Event. Actor profile, tenant visibility, global moderation, and public counts therefore use the same materialized Event path.

### Intended outcomes

- One global Actor exists per exact verified DID, regardless of how many tenants present that Actor's records.
- Federation can create an unclaimed Actor without creating a User.
- Verified ATProto login can create an email-less User and claim the existing Actor idempotently.
- Explicit provider linking never auto-matches by email and never transfers an Actor already owned by another User.
- A federated DID Actor is canonical when an authenticated User explicitly links ATProto after receiving a local Keycloak Actor.
- Tenant admins may hide an Actor only in their tenant; only instance admins may impose Actor-wide suspension.
- Public profile statistics count current-tenant, public, materialized Events only.
- Existing canonical record, outbox, Jetstream, recovery, consent, and zero-echo behavior remains intact.

### Explicit non-goals

- No ActivityPub bridge, first-party PDS, AppView, relay, or general ATProto server implementation.
- No email auto-match, synthetic email, provider-key inference, or unauthenticated account merge.
- No generic identity-resolution framework or provider-agnostic merge engine.
- No second federation capability switch; the existing governed ATProto Events capability remains authoritative.
- No direct public CRUD for `AtprotoRecord`, `ActorTenantPresence`, or merge records.
- No projection-only Event card or public profile count.
- No cross-tenant private analytics in anonymous Actor profile responses.
- No network call inside an EF Core transaction.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Actor is tenant-scoped today. | `src/Explore.Domain/Actor.cs` implements `ITenantEntity` and requires `TenantId`; `ActorConfiguration` defines alternate key `(TenantId, Id)`. | High | This is the root cause of one DID becoming several Actors. |
| Event enforces tenant-coupled Actor ownership. | `EventConfiguration.Configure` maps `(Event.TenantId, Event.ActorId)` to `(Actor.TenantId, Actor.Id)`. | High | Global Actor requires a simple ActorId FK while Event remains tenant-scoped. |
| DID is indexed but not unique. | `ActorPiiConfiguration` creates non-unique `ix_actor_pii_did`. | High | Concurrent imports or auth can duplicate identity. |
| User/Actor ownership is duplicated. | `Actor.UserId`, `User.ActorId`, `User.DefaultActorId`, `ActorConfiguration`, and `UserConfiguration`. | High | `User.ActorId` will be authoritative; `DefaultActorId` remains preference only. |
| Imported publishers are duplicated per tenant. | `AtprotoJetstreamRepository.ApplyEventImportsAsync` searches by `(TenantId, Did)` and creates a tenant-local Bot Actor for a miss. | High | Replace with one global DID lookup plus tenant presence upsert. |
| Canonical ingestion and tenant presentation already exist. | `AtprotoRecord`, `AtprotoEventProjection`, `AtprotoRecordTenantPresentation`, `AtprotoJetstreamSubscriber`, and `AtprotoJetstreamRepository`. | High | Reuse these components; do not rebuild federation. |
| Imported Events are already materialized. | `ApplyEventImportsAsync` creates/updates tenant-local `Event` and one `EventSession` tied to `AtprotoRecordId`. | High | The Event can point directly to the global Actor. |
| Public discovery still merges projection-only results. | `GetPublicEventDiscoveryRequestHandler.Handle` concatenates local Event results with `AtprotoEventProjection` results. | High | Corrected discovery removes the projection branch, not canonical storage. |
| ATProto login is linked-account-only. | `BootstrapAtprotoSessionCommandHandler.Handle` returns `account_not_linked`; `SyncUserCommandHandler.Handle` rejects an unlinked email-less ATProto identity. | High | This is the behavior this approved workstream changes. |
| User email cannot currently be absent. | `UserPii.Email` is `required string`; `SyncUserCommandHandler` requires email for User creation. | High | Nullable email changes must stay behind provider-specific validation. |
| Tenant account moderation already exists. | `TenantUser` stores status, suspension/ban/removal fields, notes, profile, and ActorId. | High | Keep it for User membership; do not overload it for unclaimed Actors. |
| Actor has no global moderation state. | `Actor` has soft-delete/audit fields only; current moderation entities target Events or TenantUsers. | High | Add explicit global Actor state and tenant-local presence hiding. |
| Actor references are broad. | `ActorId` appears in 21 persistence configuration files and 20 Domain files. | High | Migration must classify mutable ownership versus immutable evidence before rewriting. |
| Baseline build is green. | `dotnet build --configuration Release --verbosity quiet` completed successfully during planning with existing package vulnerability warnings. | High | Planning changed no runtime files. |

### 2.2 Existing Implementation

#### Domain and persistence

- `Actor` currently combines public profile data with tenant ownership and reverse ownership FKs to User, Organization, and Group.
- `ActorPii` is a required one-to-one extension containing display name, DID, handle, and profile URI.
- `User` owns `ActorId`, `DefaultActorId`, provider metadata, and required `UserPii`; `TenantUser` owns tenant membership and tenant moderation.
- `Organization` and `Group` already point to Actor, so the reverse `Actor.OrganizationId` and `Actor.GroupId` links are unnecessary for ownership.
- `ActorRepository` exposes tenant-oriented and DID-oriented reads, but tenant query filters currently shape all Actor access.
- `AtprotoRecordTenantPresentation` already separates canonical record identity from tenant visibility at the record level.

#### Federation and authentication

- One global Jetstream consumer parses exact event/RSVP collections, stores canonical records, projects discovery fields, and materializes enabled tenant Event aggregates.
- `AtprotoSessionController` and `BootstrapAtprotoSessionCommandHandler` independently verify the DPoP-bound PDS session before issuing the first-party JWT.
- Verified sessions currently require a tenant-specific `UserExternalLogin` and a tenant-specific personal Actor.
- `SyncUserCommandHandler` permits email matching for supported providers but intentionally denies it for ATProto.

#### API, HAL, and Blazor

- `ActorController` has anonymous GET by id, DID, and tenant plus authorized create/update/delete routes.
- `EventController` exposes public discovery and the governed ATProto source route.
- Blazor has `ActorService`, actor subscription UI, and workspace Actor switching, but no coherent global federated Actor profile flow.

### 2.3 Existing Tests And Verification Coverage

- `tests/Event.Persistence.IntegrationTests/Federation/AtprotoInboundEventImportPersistenceTests.cs` protects tenant-local materialization, replay, tombstone, and zero-echo behavior.
- `tests/Event.Persistence.IntegrationTests/Federation/AtprotoFederationPersistenceTests.cs` protects canonical record/presentation persistence.
- `tests/Event.Application.UnitTests/Features/Users/Commands/SyncUserCommandHandlerTests.cs` protects the current linked-only and email rules.
- ATProto bootstrap, encrypted session, discovery, source, and architecture tests exist across `Event.Application.UnitTests`, `Event.API.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.Persistence.IntegrationTests`, and `Event.Architecture.Tests`.
- Gaps: global DID uniqueness, cross-tenant Actor reuse, unclaimed Actor claim, email-less User persistence, merge concurrency, tenant-local Actor hiding, Actor-wide moderation, and materialized-only discovery are not covered.

### 2.4 Existing Documentation And Contracts

- `dev/active/atproto-auth/*` is the implemented predecessor and must remain the source for OAuth, outbox, lexicon, Jetstream, and recovery details.
- `docs/adr/ADR-015-atproto-event-federation-ownership.md` owns current DB-first federation record ownership.
- `docs/FEDERATION.md`, `docs/DOMAIN.md`, `docs/SECURITY-MODEL.md`, `docs/MULTI_TENANCY.md`, and `docs/OUTBOX_PATTERN.md` document the relevant boundaries.
- `docs/API.md`, `docs/API_CHANGELOG.md`, generated OpenAPI/client artifacts, and `docs/API_CONTRACT_INVENTORY.md` own public contract changes.
- `schemas/islamu-event.md` owns the database narrative.
- `.claude/contract/intents.yaml` has no dedicated federation-identity intent; this workstream carries the combined contracts listed in planning metadata.

### 2.5 Current Pain Points / Improvement Areas

- The same DID has different Actor IDs across tenants, so ownership, profile, moderation, subscriptions, and analytics cannot converge on one identity.
- A federated Actor cannot be claimed through the already-verified auth bridge unless a User and login link pre-exist.
- Actor type `Bot` is used for an external person only because the current Actor ownership check treats an Actor without User/Organization/Group as a bot.
- User/Actor ownership is circular and represented twice.
- Projection-only discovery bypasses materialized Event policy, Actor moderation, and a single profile/statistics model.
- A global Actor migration can accidentally collapse historical audit evidence or cross tenant boundaries unless every Actor FK is classified.

### 2.6 Unknowns After Investigation

| Unknown | Search performed | Owning task / resolution |
|---|---|---|
| Which Actor FKs are mutable current ownership versus immutable historical evidence? | Counted Domain and EF configuration references; inspected Event, User, Organization, Group, TenantUser, subscriptions, reports, notifications, audit, storage, and AI reference paths. | Task 1.1 records a complete FK disposition manifest before Task 1.2 migration SQL is written. |
| Which current call sites assume `User.Email` is non-null? | Verified Domain and sync handler requirements; broad DTO/client impact remains distributed. | Task 5.1 uses compiler diagnostics and bounded search to update only account identity/display paths that require nullable handling. |
| Whether a public Actor profile page already exists under another route? | Actor components/services and generated client were searched; only workspace switcher and subscription surfaces were found. | Task 7.1 adds the smallest route/component only if no equivalent appears before implementation. |
| Whether profile refresh needs a durable queue? | Current import fetches optional thumbnails outside the EF transaction; no Actor profile refresh owner exists. | Task 4.1 starts with bounded cached fetch in the existing inbound prefetch boundary and adds no outbox unless measured failure recovery cannot be achieved on later observations. |

## 3. Proposed Future State

### 3.1 Identity and tenancy model

1. `Actor` no longer implements `ITenantEntity` and has no `TenantId`, `UserId`, `OrganizationId`, or `GroupId`.
2. Actor type value 1 is renamed from User to Person. A Person Actor may be unclaimed or linked to one User.
3. `User.ActorId` is the nullable, unique ownership link for a personal Actor. `Organization.ActorId` and `Group.ActorId` remain their respective ownership links. `User.DefaultActorId` is explicitly a workspace preference and may reference an Actor the User is authorized to operate.
4. `ActorPii.Did` has a filtered global unique index. DID comparisons remain exact ordinal. Handle is mutable profile metadata and never an identity/merge key.
5. New `ActorTenantPresence` records `(TenantId, ActorId)`, first/last seen timestamps, source, and tenant visibility state. It permits an unclaimed Actor to be presented or hidden without a User.
6. `TenantUser` remains the account membership, role, and account-moderation record. When a User claims an Actor in a tenant, `TenantUser.ActorId` points to the same global Actor.
7. Actor global status is Active or Suspended. Only instance-admin policy may change it. Tenant admins may hide `ActorTenantPresence` locally without changing global identity.

### 3.2 Inbound federation flow

1. Jetstream/PDS recovery continues to establish canonical `AtprotoRecord` and tenant record presentations.
2. Before the EF transaction, the existing constrained PDS boundary may fetch a bounded public Actor profile for a new or stale DID; failure does not reject the event.
3. The transaction resolves the one global Actor by DID under a unique constraint, creates a Person Actor when absent, and updates safe public profile fields when verified data is available.
4. The transaction upserts `ActorTenantPresence` for every enabled tenant presentation.
5. Tenant-local Event/EventSession materialization points to the same global Actor ID. Replay, update, tombstone, thumbnail, cursor, and zero-echo behavior remains unchanged.

### 3.3 Verified claim and merge flow

1. The BFF/API security bridge verifies expected DID, token subject, PDS session DID, PDS origin, and session binding exactly as today.
2. The Application handler loads the global Actor by verified DID.
3. If the Actor is unclaimed and no provider login exists, it creates an email-less User/UserPii, creates the tenant `UserExternalLogin` and `TenantUser`, and sets `User.ActorId` to that Actor in one transaction.
4. If the login already maps to that User/Actor, the operation is idempotent and refreshes profile/session metadata.
5. If an authenticated User explicitly links ATProto and has a different local Person Actor, the DID Actor is canonical. Mutable ownership references move to it, the source Actor is soft-deleted, and an `ActorMerge` record preserves source/canonical identity and reason.
6. Imported Events already owned by the DID Actor are untouched. Local mutable ownership records attached to the source Actor are reassigned with collision-specific handling.
7. If the DID Actor is owned by another User, the operation fails with `identity_conflict`; no email or handle fallback is attempted.

### 3.4 Public profile and discovery flow

1. Actor GET by id/DID resolves the global Actor, exposes public profile fields only, and reports current-tenant presence.
2. Actor-by-tenant reads join `ActorTenantPresence`; they no longer rely on an Actor tenant filter.
3. Public Actor statistics count only non-deleted, public, materialized Events in the current tenant. Instance-wide operational counts remain admin-only.
4. Public discovery queries materialized Events only. `AtprotoEventProjection` remains ingestion/recovery/source metadata and is never mapped directly to a public Event card.
5. A globally suspended Actor or tenant-hidden presence removes its Events/profile from public tenant reads without deleting canonical records.

## 4. Non-Negotiable Constraints

- Repositories return entities, never DTOs; handlers own mapping.
- Validators are manually instantiated.
- Aggregates use UUIDv7 `Guid`; lookups use `int`; cursors use `long`.
- GET endpoints remain anonymous; all moderation, merge, and write endpoints require authorization.
- UI action visibility is controlled by HAL links, never local role/claim checks.
- Every new source file starts with two `ABOUTME:` lines.
- Domain remains independent of Application, Persistence, Infrastructure, API, and Blazor.
- Tenant-scoped dependents remain fail-closed under tenant query filters even though Actor is global.
- A global Actor lookup must never grant tenant participation, authorization, or profile-edit authority.
- ATProto identity is established only from the existing verified session boundary; DID, handle, email, headers, or client DTOs alone are not trusted.
- No PDS network call occurs inside an EF transaction.
- No destructive migration silently drops Actor references or User identity data.
- Existing outbox, canonical record, cursor fencing, recovery, and zero-echo invariants remain unchanged.
- No backward-compatibility shim is added without a concrete persisted/external consumer need.

## 5. Architecture And Design Decisions

### A1. Actor is global; tenant participation is separate

- **Decision:** Remove Actor tenancy and add `ActorTenantPresence` keyed by `(TenantId, ActorId)`.
- **Why:** DID identity is global, while visibility and participation are tenant policy.
- **Alternatives considered:** Keep one Actor per tenant; make `TenantUser` represent every Actor. The first preserves the bug, and the second cannot represent unclaimed Actors.
- **Consequences:** Actor repositories become explicit global reads; every tenant-facing query must join a tenant-scoped dependent or presence row.
- **Files/layers affected:** Domain Actor/presence, DbContext/configurations/migration, repositories, API queries, federation import, and tests.

### A2. Ownership points toward Actor from the owner

- **Decision:** `User.ActorId`, `Organization.ActorId`, and `Group.ActorId` own their Actor relationships; remove reverse owner FKs from Actor. Keep `User.DefaultActorId` only as preference.
- **Why:** One FK per ownership relationship removes circular creation and conflicting truth.
- **Alternatives considered:** Keep both directions and synchronize them. Rejected because every write would need dual-write repair logic.
- **Consequences:** Creation handlers create Actor first or in the same transaction, then set the owning entity FK.
- **Files/layers affected:** Domain entities, EF configurations, sync/onboarding/organization/group handlers, fixtures, and schema docs.

### A3. DID is the only federated Actor identity key

- **Decision:** Enforce one non-null DID globally with a filtered unique database index; do not merge by handle or email.
- **Why:** Handles change and email is neither guaranteed nor an ATProto identity proof.
- **Alternatives considered:** Application-only uniqueness or normalized-handle fallback. Both race and can misidentify users.
- **Consequences:** Concurrent import/auth converges by database uniqueness and deterministic retry.
- **Files/layers affected:** ActorPii configuration/migration, ActorRepository, import, auth, and persistence tests.

### A4. User is optional and email may be absent

- **Decision:** Permit nullable `UserPii.Email`; verified ATProto first login may create User without email. Keycloak/OIDC creation retains its provider-specific email requirements.
- **Why:** ATProto verification proves control of a DID, not ownership of an email address.
- **Alternatives considered:** Synthetic email or no User creation. Synthetic data is unsafe; linked-only login prevents claiming imported identity.
- **Consequences:** Display, DTO, validation, and privacy-erasure paths must handle null explicitly.
- **Files/layers affected:** Domain/UserPii, sync/bootstrap handlers, mappings/contracts, persistence, and tests.

### A5. Claim is link-only; merge is explicit and DID Actor wins

- **Decision:** A verified login claims an unowned Actor. An authenticated explicit provider-link operation may merge its existing local Actor into the DID Actor. No passive sign-in or email match triggers merge.
- **Why:** This preserves imported ownership and prevents account takeover.
- **Alternatives considered:** Keep local Actor canonical or rewrite imported Events. Both split the DID identity or destroy stable imported ownership.
- **Consequences:** Merge needs transaction fencing, collision rules, audit evidence, and conflict failure when another User owns the DID Actor.
- **Files/layers affected:** Application auth/link handlers, Actor repository, migration model, authorization, and tests.

### A6. Mutable references move; immutable evidence remains attributable

- **Decision:** Reassign current ownership/participation references during merge, preserve immutable historical evidence, and record `ActorMerge(SourceActorId, CanonicalActorId, reason, actor, timestamp)`.
- **Why:** Current behavior must converge without falsifying audit history.
- **Alternatives considered:** Rewrite every Actor FK or resolve every read through aliases. Both are broader and more error-prone.
- **Consequences:** Task 1.1 must maintain an explicit FK disposition manifest and Task 5.2 must implement collision handling per mutable relation.
- **Files/layers affected:** Domain merge record, EF migration, merge repository/service, audit/report readers, and tests.

### A7. Moderation has global and tenant scopes

- **Decision:** Instance admins may suspend Actor globally; tenant admins may hide ActorTenantPresence locally. `TenantUser` moderation continues to govern account membership.
- **Why:** A tenant must not globally censor an identity, while the instance needs abuse control that also covers unclaimed actors.
- **Alternatives considered:** Tenant-only or global-only moderation. Each leaves an abuse or governance gap.
- **Consequences:** Public queries apply both global and current-tenant state; authorization and HAL distinguish the two actions.
- **Files/layers affected:** Domain, CQRS, policy/HAL, API, discovery/profile queries, and tests.

### A8. Public discovery is materialized-only

- **Decision:** Remove projection-only result mapping from `GetPublicEventDiscoveryRequestHandler`; retain projection persistence for ingestion, source, and recovery.
- **Why:** One public Event model ensures Actor, moderation, tenant, lifecycle, and analytics consistency.
- **Alternatives considered:** Continue merging projection DTOs or duplicate all policy on projections. Both preserve two public truth models.
- **Consequences:** A record is not publicly discoverable until tenant Event materialization commits successfully.
- **Files/layers affected:** Application discovery handler, projection repository usage, API/HAL tests, docs, and client contracts.

### A9. Profile hydration is opportunistic and bounded

- **Decision:** Fetch verified public profile data outside the transaction through the existing constrained ATProto transport, cache it, and let later observations retry failures.
- **Why:** Event ingestion must not fail or hold database locks because a profile endpoint is unavailable.
- **Alternatives considered:** Fetch inside transaction or add a new durable outbox immediately. The first is unsafe; the second is unproven complexity.
- **Consequences:** DID-only fallback profiles are valid, and no profile value can override DID identity.
- **Files/layers affected:** Infrastructure ATProto gateway/options, import plan, Actor updates, lexicon closure if required, and tests.

## 6. Implementation Phases

### Phase 1: Global Actor Schema And Safe Data Migration

- **Goal:** Establish the global identity model and migrate current data without losing references or collapsing unrelated identities.
- **Depends on:** User approval of this plan.
- **Relevant files:** `Actor.cs`, `ActorPii.cs`, `User.cs`, `UserPii.cs`, `Organization.cs`, `Group.cs` (existing); `ActorTenantPresence.cs`, `ActorMerge.cs` (new); configurations, DbContext, migration, model snapshot, ADR/schema/docs/tests (existing/new).
- **Related skills/rules:** `clean-architecture-rules`, `dotnet-efcore-guidelines`, `.claude/rules/domain.md`, `.claude/rules/efcore-migrations.md`, `.claude/rules/tests.md`.
- **Acceptance criteria:** Global Actor constraints compile; all current Actor FKs have an explicit disposition; duplicate DIDs migrate deterministically; tenant presence and merge audit are preserved; nullable email does not weaken non-ATProto validation.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Migration `Down()` may restore schema only where data-preserving; any duplicate-DID ambiguity or FK collision aborts migration with a diagnostic instead of guessing.

#### Task 1.1: Define global Actor, presence, merge, and ownership contracts

- **Type:** modify/create
- **Layer:** Domain/Persistence/Docs
- **Files:** `src/Explore.Domain/Actor.cs`, `ActorPii.cs`, `User.cs`, `Organization.cs`, `Group.cs`, `Enums/ActorTypeEnum.cs` (existing); `ActorTenantPresence.cs`, `ActorMerge.cs`, status/source enums (new); relevant configurations and `docs/adr/ADR-016-atproto-actor-identity-lifecycle.md` (new).
- **Description:** Remove tenant and reverse-owner state from Actor, rename personal Actor type semantics to Person, make owner-side links authoritative, define tenant presence and scoped moderation fields, define merge evidence, and record every Actor FK as mutable ownership, current participation, or immutable evidence.
- **Acceptance Criteria:**
  - [ ] Domain model permits Person Actor without User and has no dependency on a tenant.
  - [ ] `User.ActorId` is unique when non-null; `DefaultActorId` is documented as preference only.
  - [ ] Tenant presence supports unclaimed Actor visibility and tenant-local hiding.
  - [ ] Global Actor status transitions are explicit domain methods, not free-form controller mutation.
  - [ ] ADR contains the complete Actor FK disposition manifest and merge collision policy.
- **Dependencies:** None.
- **Effort:** XL.
- **Required Skills/Rules:** `clean-architecture-rules`, `dotnet-efcore-guidelines`, domain and migration rules.

#### Task 1.2: Implement deterministic Actor/email migration and constraints

- **Type:** create/modify
- **Layer:** Persistence/Domain/Docs
- **Files:** new EF migration and designer; `ExploreDbContextModelSnapshot.cs`, `ActorConfiguration.cs`, `ActorPiiConfiguration.cs`, `UserConfiguration.cs`, `UserPiiConfiguration.cs`, all Actor-FK configurations, `schemas/islamu-event.md`, and focused migration/persistence tests.
- **Description:** Backfill one canonical Actor per DID, create presence rows from existing Actor tenants and record presentations, redirect only mutable/current references, preserve immutable evidence, make email nullable, replace composite tenant Actor FKs, and add filtered DID/User ownership uniqueness.
- **Acceptance Criteria:**
  - [ ] Same-DID tenant duplicates choose one deterministic canonical Actor and retain all tenant presences.
  - [ ] Imported Events already on the canonical DID Actor remain unchanged; duplicate-owned imported Events are redirected only as required by deduplication.
  - [ ] Conflicting non-DID Actors are never merged automatically.
  - [ ] Migration aborts on ambiguous ownership instead of silently discarding data.
  - [ ] PostgreSQL tests prove uniqueness, FK integrity, tenant presence, nullable email, and migration idempotence from the current schema.
- **Dependencies:** 1.1.
- **Effort:** XL.
- **Required Skills/Rules:** `dotnet-efcore-guidelines`, EF migration/persistence/test rules.

### Phase 2: Global Actor Repositories And Tenant-Safe Consumers

- **Goal:** Make all Actor access explicit about global identity versus tenant participation and remove assumptions that Actor query filters provide authorization.
- **Depends on:** Phase 1.
- **Relevant files:** `ActorRepository.cs`, repository interfaces, Actor specifications, organization/group/user creation handlers, tenant/user/AI/shell/event consumers, and architecture/persistence tests (existing).
- **Related skills/rules:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, application/persistence/test rules.
- **Acceptance criteria:** Global DID/id reads are deterministic; tenant listings require presence or tenant-owned dependent rows; no authorization path treats global Actor existence as tenant access.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep repository changes in one slice; a tenant-boundary failure blocks the phase and is fixed at the repository/query owner rather than patched in each controller.

#### Task 2.1: Replace tenant-filtered Actor repository contracts

- **Type:** modify
- **Layer:** Application/Persistence
- **Files:** `IActorRepository.cs`, `ActorRepository.cs`, Actor specifications/query handlers, query-filter configuration, and repository tests (existing).
- **Description:** Add explicit global id/DID reads, tenant-presence reads, unclaimed/owned state checks, and merge-safe lookup methods. Remove misleading `GetActorByUserIdAndTenantId` ownership semantics.
- **Acceptance Criteria:**
  - [ ] Global DID lookup returns at most one Actor and handles uniqueness races.
  - [ ] Tenant Actor lists require active visible `ActorTenantPresence` or another explicitly authorized tenant relation.
  - [ ] Repository methods return entities and do not bypass filters without a documented reason and safety test.
  - [ ] Merge lookup can distinguish unclaimed, owned-by-current-user, and owned-by-other-user states.
- **Dependencies:** 1.2.
- **Effort:** L.
- **Required Skills/Rules:** `dotnet-efcore-guidelines`, repository and application rules.

#### Task 2.2: Update Actor creators and cross-layer consumers

- **Type:** modify
- **Layer:** Application/Persistence
- **Files:** `SyncUserCommandHandler.cs`, `CreateOrganizationCommandHandler.cs`, `CreateGroupCommandHandler.cs`, onboarding, UI shell, AI Actor context, events, subscriptions, notifications, and affected tests/fixtures (existing).
- **Description:** Replace Actor tenant/reverse-owner initialization, set owner-side links, require tenant authorization independently, and preserve workspace selection through `DefaultActorId` without treating it as ownership.
- **Acceptance Criteria:**
  - [ ] Person, Organization, and Group Actor creation uses the new one-direction ownership model.
  - [ ] Tenant authorization still comes from TenantUser/organization/group policy, not Actor lookup.
  - [ ] Actor subscriptions and notification fanout resolve canonical Actor identity without cross-tenant data leakage.
  - [ ] Architecture tests prevent reintroduction of `Actor.TenantId` and reverse owner FKs.
- **Dependencies:** 2.1.
- **Effort:** XL.
- **Required Skills/Rules:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, application/test rules.

### Phase 3: Canonical Inbound Actor Materialization

- **Goal:** Make Jetstream and snapshot recovery reuse one Actor per DID while preserving tenant-local materialization.
- **Depends on:** Phase 2.
- **Relevant files:** `AtprotoJetstreamRepository.cs`, import plan/factory, federation docs, and focused persistence tests (existing/new).
- **Related skills/rules:** `clean-architecture-rules`, `dotnet-efcore-guidelines`, application/persistence/test rules.
- **Acceptance criteria:** Concurrent tenants and replay converge on one Actor; tenant presence is idempotent; existing materialization, tombstone, recovery, and zero-echo behavior remains intact.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep the prior canonical record/presentation transaction intact until global Actor persistence passes real PostgreSQL replay, recovery, concurrency, and tombstone tests.

#### Task 3.1: Rework inbound materialization around global Actor identity

- **Type:** modify
- **Layer:** Application/Persistence
- **Files:** `AtprotoJetstreamRepository.ApplyEventImportsAsync`, `AtprotoFederatedEventImportPlan`, factory/handlers, DbContext, `AtprotoInboundEventImportPersistenceTests.cs`, `AtprotoFederationPersistenceTests.cs`, and `docs/FEDERATION.md` (existing).
- **Description:** Resolve/create Actor globally by DID, set Person type, upsert each tenant presence, and attach tenant Event/EventSession aggregates to the shared Actor while retaining record/presentation fences, thumbnails, tombstones, and zero echo.
- **Acceptance Criteria:**
  - [ ] Two enabled tenants importing the same DID create one Actor and two presence rows.
  - [ ] Replay, update, recovery, and concurrent import are idempotent.
  - [ ] Existing tenant Event IDs and canonical `AtprotoRecordId` links remain stable.
  - [ ] Tombstone removes materialized Events as today but does not delete the global Actor while other presence/history remains.
- **Dependencies:** 2.1, 2.2.
- **Effort:** L.
- **Required Skills/Rules:** `dotnet-efcore-guidelines`, application/persistence/test rules.

### Phase 4: Bounded Public Actor Profile Hydration

- **Goal:** Safely enrich global Actors with public ATProto profile data without making Event import depend on remote profile availability.
- **Depends on:** Phase 3.
- **Relevant files:** constrained PDS gateway/transport, import prefetch boundary, local profile lexicons if needed, cache/options, Actor mapping, operator docs, and focused Infrastructure tests (existing/new).
- **Related skills/rules:** `auth-patterns`, `clean-architecture-rules`, `agentic-research` only if current CarpaNet APIs need verification, infrastructure/test rules.
- **Acceptance criteria:** Profile retrieval is constrained, cached, optional, and outside transactions; failure retains a DID-only Actor and later observations can retry.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Disable only profile hydration on transport failures; never disable canonical Event import or delete canonical records.

#### Task 4.1: Hydrate bounded public Actor profile data

- **Type:** modify/create
- **Layer:** Infrastructure/Application/Persistence/Docs
- **Files:** existing constrained ATProto PDS gateway and import prefetch boundary; profile lexicon files/generated bindings if required; options/cache registration; Actor profile mapping; `docs/FEDERATION.md`, `docs/LEXICONS.md`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`; focused gateway/import tests.
- **Description:** Fetch public handle/display name/description/avatar/PDS metadata for new or stale DIDs outside the transaction, enforce response/redirect/content bounds, cache results, and pass an optional verified snapshot into import. Reuse later observations as retry; add no durable queue unless evidence proves it necessary.
- **Acceptance Criteria:**
  - [ ] DID remains the immutable key; profile fields cannot replace or merge identity.
  - [ ] SSRF, redirect, payload-size, media-type, and timeout policy matches the existing constrained transport.
  - [ ] Profile failure produces a DID-only Actor and does not fail Event import.
  - [ ] Single-tenant and multi-tenant self-hosting behavior is documented without adding a second federation mode.
- **Dependencies:** 3.1.
- **Effort:** L.
- **Required Skills/Rules:** `auth-patterns`, `clean-architecture-rules`, infrastructure/test rules.

### Phase 5: Verified Actor Claim And Explicit Account Merge

- **Goal:** Allow a verified ATProto identity to create/claim its User safely and merge only an explicitly linked local Actor into the DID Actor.
- **Depends on:** Phase 3.
- **Relevant files:** bootstrap/session commands, sync/link handlers, User/UserPii DTOs and mappings, repositories, token/session persistence, API/BFF contracts, security docs, and tests (existing/new).
- **Related skills/rules:** `auth-patterns`, `blazor-bff-patterns`, `cqrs-mediatr-guidelines`, application/API/test rules.
- **Acceptance criteria:** First verified ATProto login can claim; repeat login is idempotent; no email matching occurs; owned-DID conflict fails closed; merge prefers federated Actor and preserves imported Events.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Any failure rolls back User/login/membership/Actor linkage together; security gateway persistence and token issuance occur only for the committed identity result.

#### Task 5.1: Claim or create User from verified ATProto session

- **Type:** modify
- **Layer:** Application/Persistence/API
- **Files:** `BootstrapAtprotoSessionCommandHandler.cs`, `SyncUserCommandHandler.cs` or a narrower ATProto claim command, User/UserPii mappings/DTOs, external-login and TenantUser repositories, focused unit/persistence/API tests, `docs/SECURITY-MODEL.md`, and `docs/AUTHORIZATION.md`.
- **Description:** After existing cryptographic verification, atomically resolve the global DID Actor, create an email-less User when unclaimed, create tenant login/membership, link `User.ActorId`, and preserve session/token persistence. Keep non-ATProto provider creation validation unchanged.
- **Acceptance Criteria:**
  - [ ] No User is created before full PDS/session verification succeeds.
  - [ ] Unclaimed Actor claim preserves Actor ID and all imported Event.ActorId values.
  - [ ] Repeat login repairs missing tenant membership/link metadata idempotently without duplicating User or Actor.
  - [ ] Email and handle are never used to locate an existing User.
  - [ ] Actor owned by another User returns stable `identity_conflict` without session issuance.
- **Dependencies:** 3.1, 1.2.
- **Effort:** L.
- **Required Skills/Rules:** `auth-patterns`, `cqrs-mediatr-guidelines`, application/API/test rules.

#### Task 5.2: Merge explicit local personal Actor into federated Actor

- **Type:** create/modify
- **Layer:** Domain/Application/Persistence/API
- **Files:** new Actor merge command/handler/validator and repository operation; explicit provider-link owner; `ActorMerge` configuration; mutable dependent repositories; API ProblemDetails/HAL contract if an endpoint changes; focused concurrency/conflict tests; `docs/API_CHANGELOG.md`.
- **Description:** Under explicit authenticated provider linking, lock source and canonical Actors deterministically, verify current User ownership and DID control, move only the Task 1.1 mutable-reference manifest with collision handling, set User.ActorId to canonical, preserve imported Event references already on canonical, record merge, and soft-delete source.
- **Acceptance Criteria:**
  - [ ] Canonical Actor is always the verified DID Actor.
  - [ ] Imported Events already on canonical Actor are not updated.
  - [ ] Mutable source-owned records converge without duplicate subscription/membership uniqueness violations.
  - [ ] Immutable audit/evidence rows remain unchanged and can identify the merge record.
  - [ ] Concurrent claim/link attempts produce one committed result or a deterministic conflict.
- **Dependencies:** 5.1, 2.1, 1.1.
- **Effort:** XL.
- **Required Skills/Rules:** `auth-patterns`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, application/persistence/API/test rules.

### Phase 6: Actor Profile, Scoped Moderation, Analytics, And Materialized Discovery

- **Goal:** Expose one safe Actor profile/read model and enforce global/local moderation through materialized public Event queries only.
- **Depends on:** Phase 5.
- **Relevant files:** Actor queries/controller/HAL/policies, Event discovery handler/repositories, moderation commands, DTOs/OpenAPI/generated contracts, API/federation docs, and integration tests (existing/new).
- **Related skills/rules:** `cqrs-mediatr-guidelines`, `auth-patterns`, API/HAL/application/test rules.
- **Acceptance criteria:** Public reads expose no User PII; tenant presence and global moderation are enforced server-side; discovery no longer maps projections directly; HAL is the action authority.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** If a materialized Event or Actor policy cannot be evaluated, fail closed from public discovery; canonical records/projections remain for recovery and retry.

#### Task 6.1: Replace Actor API reads with global profile plus tenant presence

- **Type:** modify
- **Layer:** Application/API/Docs
- **Files:** Actor query requests/handlers/DTOs/mappings, `ActorController.cs`, `ActorLinkPolicy.cs`, RouteNames/OpenAPI/generated contracts, API integration tests, `docs/API.md`, and `docs/API_CHANGELOG.md`.
- **Description:** Return global public profile by id/DID, resolve tenant listings through presence, include current-tenant public materialized Event counts, and expose write/moderation links only when server policy authorizes them.
- **Acceptance Criteria:**
  - [ ] Anonymous GET exposes public Actor fields and no User email/provider/session/merge internals.
  - [ ] Actor-by-tenant returns only visible active presence rows.
  - [ ] Public counts are current-tenant, public, non-deleted, materialized Events only.
  - [ ] Authorized HAL links distinguish profile edit, tenant hide/unhide, and instance suspend/restore.
- **Dependencies:** 2.1, 5.2.
- **Effort:** L.
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`, `auth-patterns`, API/HAL rules.

#### Task 6.2: Add Actor-wide suspension and tenant-local presence hiding

- **Type:** create/modify
- **Layer:** Domain/Application/API/Persistence
- **Files:** Actor/presence state methods, commands/validators/handlers, authorization descriptors/policies, ActorController/HAL, repositories, audit logs, tests, `docs/SECURITY-MODEL.md`, and `docs/API_CHANGELOG.md`.
- **Description:** Implement separate instance-admin global status and tenant-admin local presence visibility transitions with concurrency, reason bounds, audit attribution, idempotency, and safe ProblemDetails.
- **Acceptance Criteria:**
  - [ ] Tenant admin cannot change global Actor status or another tenant's presence.
  - [ ] Instance suspension hides Actor and owned Events across public tenants without deleting canonical data.
  - [ ] Tenant hide affects only that tenant and also works for unclaimed Actors.
  - [ ] Repeat transitions are idempotent and concurrent conflicting transitions use `ConcurrencyStamp`.
- **Dependencies:** 6.1, 1.1.
- **Effort:** L.
- **Required Skills/Rules:** `auth-patterns`, `cqrs-mediatr-guidelines`, domain/application/API/test rules.

#### Task 6.3: Make public discovery materialized-only

- **Type:** modify
- **Layer:** Application/Persistence/API/Docs
- **Files:** `GetPublicEventDiscoveryRequestHandler.cs`, Event list specification/repository, projection repository consumers, source-link policy, API integration tests, `docs/FEDERATION.md`, `docs/API.md`, and contract inventory.
- **Description:** Remove projection-only item mapping/counting, query the materialized Event path with Actor global/local moderation predicates, and retain projection lookups only for governed source metadata where required.
- **Acceptance Criteria:**
  - [ ] No `AtprotoEventProjection` is returned directly as an Event discovery item.
  - [ ] Imported materialized Events remain discoverable with stable pagination and source links.
  - [ ] Suspended or tenant-hidden Actor Events are absent.
  - [ ] Materialization failure leaves canonical/projection evidence but no public Event card.
- **Dependencies:** 6.2, 3.1.
- **Effort:** L.
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, application/persistence/API/test rules.

### Phase 7: Blazor Actor Experience And Contract Reconciliation

- **Goal:** Consume the corrected global Actor contract without client-side identity or authorization inference.
- **Depends on:** Phase 6.
- **Relevant files:** `IActorService`, `ActorService`, generated API client/serializer roots, Event organizer/profile links, actor subscription UI, optional Actor profile page, Blazor docs, and bUnit/service tests (existing/new).
- **Related skills/rules:** `blazor-ui-conventions`, `blazor-css-isolation` if a new component is needed, Blazor client/API HAL/test rules.
- **Acceptance criteria:** Users can navigate from materialized Event ownership to one Actor profile; all actions are HAL-gated; nullable User email never leaks into Actor display fallback.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep Actor profile rendering read-only when links are absent; do not add client role checks or hide API errors behind fabricated local state.

#### Task 7.1: Consume global Actor profiles and HAL actions in Blazor

- **Type:** modify/create
- **Layer:** Blazor/Docs
- **Files:** `IActorService.cs`, `ActorService.cs`, generated client and `AppJsonSerializerContext.cs`, Event/profile/subscription components, optional minimal Actor profile route/component, isolated CSS if created, bUnit/service tests, `docs/BLAZOR.md`, and `docs/API_CONTRACT_INVENTORY.md`.
- **Description:** Regenerate contracts, route Actor links to the global profile, render current-tenant public materialized counts, and show subscribe/edit/hide/suspend actions only from HAL relations.
- **Acceptance Criteria:**
  - [ ] One Actor URL represents the same DID across tenant Event pages.
  - [ ] Profile renders DID-only fallback safely when remote profile hydration failed.
  - [ ] No action checks local claims/roles; missing HAL links remove the affordance.
  - [ ] Mobile/desktop component tests cover loading, not found/hidden, suspended, and normal profile states without browser automation.
- **Dependencies:** 6.1, 6.2, 6.3.
- **Effort:** L.
- **Required Skills/Rules:** `blazor-ui-conventions`, Blazor client/HAL/test rules.

## 7. Testing Strategy

- Phase 1 uses PostgreSQL persistence integration tests because schema, data migration, filtered uniqueness, and FK integrity are the highest-risk behavior.
- Phase 2 uses architecture tests to enforce the new global/tenant boundary and prevent `Actor.TenantId` or reverse owner FKs from returning.
- Phase 3 uses PostgreSQL persistence integration tests for canonical Actor reuse, tenant presence, replay, recovery, tombstone, and zero echo.
- Phase 4 uses Infrastructure tests for constrained public profile retrieval, caching, and failure fallback.
- Phase 5 uses Application unit tests for verified claim, no-email/no-auto-match, conflict, idempotency, and merge orchestration.
- Phase 6 uses API integration tests for public profile, HAL authorization, moderation, and the distinct materialized-only discovery contract.
- Phase 7 uses Blazor client tests for HAL-gated rendering and global profile navigation.
- Intent-mandated projects not selected as a phase gate are still updated by their owning task; no extra command is run inside that phase. The canonical full project matrix remains the predecessor workstream's Todo 23 responsibility until that workstream closes.

## 8. Documentation, Configuration, And Operations Impact

- Add `docs/adr/ADR-016-atproto-actor-identity-lifecycle.md` for global identity, tenancy, claiming, merge, moderation, and materialized discovery decisions.
- Update `docs/DOMAIN.md`, `docs/MULTI_TENANCY.md`, and `schemas/islamu-event.md` with global Actor, optional User, ActorTenantPresence, ActorMerge, and nullable email.
- Update `docs/FEDERATION.md` and `docs/LEXICONS.md` with one-Actor-per-DID import and bounded profile hydration.
- Update `docs/SECURITY-MODEL.md` and `docs/AUTHORIZATION.md` with claim/merge trust and global-versus-tenant moderation authority.
- Update `docs/API.md`, `docs/API_CHANGELOG.md`, generated OpenAPI/client artifacts, and `docs/API_CONTRACT_INVENTORY.md` for Actor contracts and materialized-only discovery.
- Update `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, and `docs/TROUBLESHOOTING.md` only to clarify single-/multi-tenant behavior and profile refresh failure; add no new capability switch unless implementation evidence requires a bounded cache/timeout option.
- No Compose, Aspire, secret, or deployment topology change is expected.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Only the existing verified ATProto security gateway may establish DID control.
- Claim/merge endpoints require authenticated, single-use, tenant-bound flow state and server-side ownership checks.
- No email matching or handle matching occurs, even when values coincide.
- User email is private and nullable; Actor public DTOs never include User, login, token, session, or merge reason data.
- Global Actor moderation is instance-admin only. Tenant admins operate only current-tenant presence.
- Public queries fail closed on global suspension, local hide, missing presence, tenant mismatch, or unresolved authorization.
- Merge uses deterministic locking and optimistic concurrency to prevent double claims and cross-account takeover.
- Audit records contain bounded reason codes/text and IDs, not OAuth tokens, DPoP material, private profile payloads, or raw provider errors.
- Profile hydration reuses SSRF-safe transport and bounded response handling.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

- **Multi-tenancy - Applicable:** Actor is global, but Event, TenantUser, ActorTenantPresence, authorization, public statistics, and visibility remain tenant-scoped.
- **Federation - Applicable:** DID uniqueness, record provenance, replay, recovery, source links, and zero echo are preserved; public projection-only results are removed.
- **Localization - Applicable:** Profile display name/description are remote Unicode content and must render safely; no translation layer is introduced.
- **Accessibility - Applicable:** Any new Blazor profile route uses semantic headings, labeled actions, keyboard access, loading/error status, and existing design-system components.
- **Product - Applicable:** ATProto-first users can sign in without email; explicit linking is required to combine providers; federated identity remains useful before account claim.

## 11. Observability And Operations

- Add bounded counters for global Actor created/reused, tenant presence created/reactivated/hidden, claim succeeded/conflicted, merge succeeded/conflicted, and profile hydration succeeded/failed.
- Log ActorId, tenant ID, bounded reason code, and hashed/partially redacted DID where existing logging policy requires; never log User email, OAuth payload, or profile JSON.
- Preserve Jetstream readiness. Profile hydration failure is degraded enrichment, not ingestion unready.
- Migration emits deterministic counts for tenant Actor rows, canonical Actors, duplicate DID groups, presence rows, reassigned mutable references, and aborted ambiguity.
- Troubleshooting identifies duplicate-DID constraint failures, owned-DID conflicts, stale profile fallback, and hidden/suspended public results.

## 12. Migration And Compatibility Plan

1. Add new presence/merge/global-status schema while current Actor tenancy still exists.
2. Build the FK disposition manifest and abort if any Actor FK is unclassified.
3. Group non-null exact DIDs, select canonical Actor deterministically, backfill presence from Actor tenants and record presentations, and reassign mutable/current references according to manifest.
4. Preserve immutable evidence and record deduplication merges.
5. Backfill owner-side links and validate one personal owner per Actor before removing reverse-owner FKs.
6. Replace composite tenant Actor FKs with simple ActorId FKs while retaining tenant checks on dependents and presence/authorization queries.
7. Make User email nullable, rename personal Actor type semantics, add unique filtered DID/User ownership indexes, then remove Actor.TenantId and obsolete columns.
8. Deploy code and schema as one pre-1.0 coordinated release; do not support mixed old/new application versions.
9. `Down()` must not claim to reconstruct duplicate tenant Actors after consolidation. If reversal cannot preserve data, fail explicitly and document restore-from-backup as rollback.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Actor FK migration rewrites audit evidence or misses mutable ownership. | High | Critical | Mandatory complete disposition manifest; abort unclassified FKs; PostgreSQL migration tests. | Count mismatch, FK violation, audit identity drift. | 1.1, 1.2 |
| Duplicate DID rows have conflicting User owners. | Medium | Critical | Abort migration; require explicit operator resolution rather than guessing. | More than one non-null owner in DID group. | 1.2 |
| Global Actor lookup leaks cross-tenant access. | Medium | Critical | Separate global identity repository methods from presence/authorization queries; architecture and API tests. | Actor appears without current tenant presence/policy. | 2.1, 5.1 |
| Nullable email breaks display or provider validation. | High | High | Provider-specific creation rules; compiler-guided updates; no empty-string substitution. | Nullability diagnostics or Keycloak regression test. | 1.2, 4.1 |
| Concurrent import and login create duplicate Actor/User. | Medium | Critical | Database DID/User uniqueness, deterministic locks, retry/idempotency tests. | Unique constraint conflict not converging on reread. | 3.1, 5.1 |
| Merge collides on subscriptions/memberships. | High | High | Per-relation collision policy in ADR; transactional deduplication. | Unique violation during merge. | 1.1, 5.2 |
| Global moderation is accidentally delegated to tenant admins. | Low | Critical | Separate commands/policies/HAL links; instance-admin-only integration tests. | Tenant admin receives global action link or 2xx. | 6.2 |
| Materialized-only discovery lowers visible count after failed imports. | Medium | Medium | Treat as truthful degraded state; preserve projection evidence and retry import; observe gap metric. | Projection count exceeds materialized visible count. | 6.3 |
| Profile endpoint causes ingestion latency/SSRF. | Medium | High | Fetch outside transaction, bounded constrained transport, cache, optional fallback. | Import latency/profile-failure counters. | 4.1 |

## 14. Success Metrics And Definition Of Done

- Database contains at most one active Actor per non-null DID and one optional User owner per personal Actor.
- Importing the same DID into multiple tenants creates one Actor and one presence per tenant.
- Verified ATProto first login claims the preexisting Actor with no email and no imported Event rewrite.
- Explicitly linking an existing Keycloak User selects the DID Actor as canonical and records one audited merge.
- Cross-account DID conflict, email coincidence, and handle coincidence never merge identities.
- Public Actor/profile/discovery endpoints respect global suspension and current-tenant visibility.
- Public discovery contains only materialized Events and retains governed source links.
- Every phase has one green Release build and its one selected project test before the phase is marked complete.
- Plan, context, and task ledger remain synchronized; implementation tasks and phase verification are tracked separately.

## 15. Implementation Agent Contract - KEEP DEV DOCS CURRENT

1. At initial implementation start, read all three files once. On resume, read context/tasks first and only the current plan phase plus referenced decisions.
2. Start from the highest-priority unchecked task unless the user overrides it.
3. Use `tasks.md` as the hot ledger. Check a substantial task immediately after its acceptance criteria pass; reconcile small tasks no later than phase end.
4. Keep task and phase-verification checkboxes separate. A phase is complete only after all tasks and both phase gates pass.
5. Update status summary, completed count, current priority, next slice, discovered work, deferred work, and date whenever task state changes.
6. Update context after a phase, decision, blocker, validation failure, material discovery, or handoff. Update this plan only for strategy/scope/sequence/risk/acceptance changes.
7. Record failed validation and the next recovery action without marking the phase complete.
8. Preserve unrelated dirty worktree changes and identify them in handoff notes.
9. Run phase verification once after all phase tasks: one Release build and at most one selected non-browser project test. Do not start the application, browser, Docker, Aspire, or live services for routine phase verification.
10. Never report implementation complete when repository reality and `tasks.md` disagree.
11. Every implementation summary must teach what changed, the architecture and trust boundaries used, important files/classes, data/control flow, security/reliability practices, verification, remaining work, and dev-doc status.

## 16. Progress Reporting Contract

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: tasks reconciled yes/no; context and plan updated or unchanged with reason
```

## 17. Potential Risks & Unknowns

The highest-risk work is not ATProto protocol handling; it is consolidating a tenant-scoped Actor referenced across at least 21 EF configurations without weakening tenant authorization or rewriting immutable evidence. Task 1.1's FK disposition manifest and Task 1.2's real PostgreSQL migration tests are hard gates. The second risk is nullable User email: it must be an ATProto-specific account capability, not an accidental relaxation of Keycloak/OIDC identity requirements.
