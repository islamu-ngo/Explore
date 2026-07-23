<!-- ABOUTME: Repository-grounded implementation plan for AT Protocol OAuth and event federation with CarpaNet. -->
<!-- ABOUTME: Defines DB-first event publication, exhaustive community-lexicon projection, inbound fetch, and governed validation profiles. -->

# AT Protocol Integration — Implementation Plan

Last Updated: 2026-07-23 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Write an implementation plan under dev/active for the ATProto implementation, follow dev/report/atproto-report.md strictly, use CarpaNet documentation from /home/amir/dev/Github/CarpaNet/docs/docs and Context7, ignore backward compatibility because the product is still in development, and preserve repository conventions, Clean Architecture, security, and maintainability. The 2026-07-18 clarification makes ATProto Events one governed capability for both fetch and publication, requires local-DB-first publication, requires every non-native public event field in the single event record description, and adds an administrator-selectable community-lexicon validation profile.
- **Task directory:** dev/active/atproto-auth/
- **Planning status:** All 33 implementation tasks across fifteen phases are complete and evidence-backed. The user's 2026-07-23 clarification superseded ADR-015's read-model-only inbound boundary: each accepted inbound event now materializes a tenant-local `Event` and exactly one `EventSession`. The execution plan is 21/26 top-level gates complete; canonical full-project verification and F1-F4 remain open.
- **Completed implementation tasks:** 33/33. Recovery, refresh durability, universal discovery, and tenant-local inbound aggregate materialization are independently confirmed under `.omo/evidence/atproto-auth/task-16/` through `task21/`.
- **Current priority:** Run Todo 22's canonical Release build, all nine project test commands, contract/migration checks, and deterministic integration smoke.
- **Primary source:** dev/report/atproto-report.md, revision 3 dated 2026-07-18.
- **Matched intents:** bff-auth-bug, add-write-endpoint, add-get-endpoint, add-cqrs-handler, add-ef-migration, update-repository-query, openapi-contract-change, add-hal-link, blazor-component-affordance, and external-infrastructure-bootstrap.
- **Relevant skills:** implementation-plan, agentic-research, clean-architecture-rules, auth-patterns, blazor-bff-patterns, cqrs-mediatr-guidelines, dotnet-efcore-guidelines, outbox-pattern, error-tracking, blazor-ui-conventions, and lsp.
- **Relevant rules:** AGENTS.md; docs/QUICK_REFERENCE.md; docs/GOVERNANCE.md; .claude/rules/application-layer.md; api-controllers.md; api-hateoas.md; blazor-server.md; blazor-client.md; domain-layer.md; efcore-persistence.md; efcore-migrations.md; and tests.md.
- **Primary layers:** Domain, Application, Persistence, Infrastructure, API, Blazor BFF, generated Blazor client contracts, configuration/secrets, and documentation.
- **Complexity:** XL. The work crosses two authentication trust boundaries, multi-tenant BFF routing, OAuth state and DPoP credential persistence, asymmetric key rotation, EF Core schema, transactional outbox delivery, community-lexicon projection, Jetstream ingestion and snapshot recovery, tenant-local aggregate import, public HAL/OpenAPI, and nine test projects across fifteen dependency-ordered phases.
- **Compatibility posture:** Breaking cleanup is authorized for this development-stage feature. Do not add aliases, dual reads/writes, deprecated endpoints, compatibility DTOs, migration shims, or compatibility-only tests.

## 1. Executive Summary

Implement AT Protocol OAuth as a real BFF login provider and implement governed event/RSVP federation using CarpaNet, CarpaNet.OAuth, and CarpaNet.Jetstream. The BFF owns handle input, OAuth challenge/callback, client metadata/JWKS, transient state, cookie sign-in, and tenant return routing. The API independently verifies the DPoP-bound PDS session, synchronizes the already-linked platform user, persists the encrypted CarpaNet session, and issues a short-lived first-party ES256 session JWT. Existing YARP bearer forwarding then carries only that first-party JWT to the API; PDS tokens and private DPoP material never reach WebAssembly.

Event federation is DB-first. The effective `federation.atproto_events_enabled` setting activates both inbound event fetching and eligible outbound event publication. Local create/publish validation runs under either the default platform profile or the administrator-selected `community_lexicon` profile. A PDS create is never attempted inside a request transaction and never exists without a committed application event: the local publication transition and durable `PdsSyncOutbox` row commit atomically, then a worker restores the user's CarpaNet session and writes the PDS record. One `community.lexicon.calendar.event` record represents the whole event. Every public/federatable field that does not have a native lexicon property—including all sessions, days, agenda items, aspects, lookups, categories, tags, locations, and custom EAV values—is rendered deterministically into that record's description.

Inbound event federation keeps one globally canonical DID/collection/rkey record, but no longer stops at a discovery projection. For every enabled tenant presentation, the fenced Jetstream or complete PDS-snapshot transaction also creates or updates one provenance-linked `Event` and exactly one `EventSession`. A dedicated internal CQRS command and manually instantiated FluentValidation validator accept the community lexicon minimum (`name` and `createdAt`) while validating every optional supplied value. The first safe lexicon URI is the source URL and maps to `Event.EventUrl`; starts/ends map to the session schedule; mode, status, RSVP expectation, description, DID, and canonical record identity map deterministically without enqueuing outbound federation.

The implementation deliberately reuses the existing AtprotoAuthenticationHandler seam, DynamicAuthSchemeManager, LoginRedirect component, SyncUserCommand, UserExternalLogin, IndexedDid, UserAuthenticationToken, cookie/YARP pipeline, tenant context, secret registry, rate limiting, logging, and test fixtures. It does not hand-roll PAR, PKCE, DPoP, nonce retry, handle resolution, PDS discovery, or OAuth token refresh because CarpaNet already owns those protocol details.

### Intended outcomes

- A linked ATProto identity can authenticate through Bluesky, Eurosky, or another conforming PDS and receive the same platform authorization/HAL experience as a Keycloak user.
- API authorization accepts a dedicated, fully validated first-party ATProto session JWT without treating a PDS token as an API bearer token.
- OAuth sessions are tenant-scoped, DID-keyed, rotation-capable, encrypted at rest, and usable later by CarpaNet RestoreSessionAsync.
- Client metadata and public JWKS are hosted at the configured canonical ATProto public URL.
- Disabling auth.atproto_login_enabled removes the provider and makes its readiness truthful.
- Disabling federation.atproto_events_enabled stops both inbound collection ingestion and new outbound event/RSVP enqueue for that effective scope.
- Accepted inbound community events create or update one tenant-local Event and one EventSession in the same fenced transaction as canonical materialization and cursor/snapshot settlement.
- An enabled tenant can publish only with explicit user consent, a linked DID, a restorable OAuth session, successful local publication, successful exhaustive projection, and a durable post-commit outbox entry.
- Instance administrators, or tenant administrators when the existing setting lock is open, can select platform validation or the community lexicon's minimum required event fields; `community_lexicon` is effective only while ATProto Events is enabled.

### Explicit non-goals

- No FishyFlip adoption or custom implementation of AT Protocol OAuth cryptography.
- No Keycloak identity brokering.
- No PDS access or refresh token in browser state, serialized auth state, query strings, logs, traces, support bundles, or public DTOs.
- No email-based matching for ATProto identities and no synthetic email address.
- No general-purpose OAuth framework or speculative provider abstraction.
- No CarpaNet.AspNetCore package unless implementation evidence proves a concrete need; current documentation shows XRPC server generation, not OAuth client metadata hosting.
- No retroactive bulk publication merely because an administrator enables the capability; already-published events require an explicit, HAL-advertised synchronization action or a later lifecycle transition.
- No separate session records in the community event collection; all session data is embedded as readable text in the one event description.
- No second remote session record is created, but every inbound local import creates exactly one application `EventSession`.
- No reflection-based EF graph serialization, raw entity JSON, private registration answers, attendee PII, moderation evidence, audit internals, secrets, or internal-only identifiers in the PDS description.
- No public create/update/delete API, DTO, handler, generated client method, or HAL affordance may directly mutate `AtprotoRecord`; only lifecycle-owned outbound delivery and the canonical inbound subscriber own those records.

## 2. Source-Grounded Pre-Implementation State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| The ATProto BFF scheme is scaffolding, not working authentication. | src/Explore.Blazor/Authentication/AtprotoAuthenticationHandler.cs returns NoResult and a 501 challenge; AtprotoAuthenticationOptions still names FishyFlip. | High | The existing seam should be completed, not replaced with a second auth stack. |
| The login UI already supplies a handle. | src/Explore.Blazor.Client/Pages/LoginRedirect.razor calls /auth/challenge with provider=atproto and login_hint. | High | Reuse its accessible form and safe return URL behavior. |
| The BFF already owns cookies and API bearer forwarding. | Event.Web.BffHosting cookie events, EventBffBearerForwardingHandler, ExploreBffCookieSessionHandler, and CircuitAccessTokenService. | High | The first-party JWT belongs in the protected server cookie ticket. |
| The API currently routes API keys and Keycloak JWTs only. | src/Explore.API/Extensions/AuthenticationExtensions.cs and ApiAuthenticationSchemeNames. | High | A distinct AtprotoSession branch is required; selector parsing must never become trust. |
| UserAuthenticationToken responses are metadata-safe but generic writes accept raw credentials. | UserAuthenticationTokenDto and ListDto omit secrets; Create/Update DTOs contain access, refresh, ID, and DPoP material. | High | Replace public generic mutation with the purpose-built authenticated bridge; do not keep both. |
| Public AtprotoRecord writes bypass lifecycle ownership. | AtprotoRecordController exposes create/update/delete; generated client methods, command handlers, DTOs, and HAL links preserve the bypass. | High | Remove direct mutation surfaces; event/RSVP lifecycle outboxes and canonical Jetstream ingestion are the only write authorities. |
| The current token schema cannot faithfully persist a complete CarpaNet OAuthSessionData and stores secrets as short plaintext strings. | Explore.Domain/UserAuthenticationToken.cs and UserAuthenticationTokenConfiguration.cs use five max-length-500 strings and lack a DID key or encryption version. | High | Use one versioned encrypted session envelope plus safe metadata. |
| Existing ATProto sync rejects unlinked identities without email. | SyncUserCommandHandler requires an existing UserExternalLogin for ATProto when email is absent. | High | Safe default is linked-account sign-in only; the report's new-user DoD conflicts with this rule. |
| PDS publishing scaffolding exists but is not wired. | PdsSyncOutbox, PdsSyncWorker, PdsService, repository, and configurations exist; no named lifecycle handler enqueues it. | High | Reuse the outbox shape only after Task 9.1 adds ownership, idempotency, leasing, and settlement invariants. |
| PdsService currently performs unauthenticated raw HTTP and worker settlement drops returned URI/CID. | PdsService posts directly to putRecord/deleteRecord; PdsSyncOutboxRepository.MarkAsCompleted ignores uri/cid. | High | Part B must replace this with RestoreSessionAsync and transactional settlement. |
| Both local publication paths already expose a transaction seam. | CreateEventCommandHandler creates the aggregate graph and notification outbox inside IUnitOfWork when created as Published; PublishEventCommandHandler validates readiness, changes status, and writes its notification outbox inside IUnitOfWork. | High | Add PDS outbox planning inside these transactions; never call a PDS from either handler. |
| Platform publish readiness is stricter than the community event lexicon. | EventLifecyclePolicyProvider requires title, tenant, owner, status, visibility, format, and scheduled-session rollups for EventPublish; the vendored community event lexicon requires only name and createdAt. | High | Add a governed profile while preserving authorization, ownership, tenant, storage, and supplied-field integrity invariants. |
| The create request already makes optional graph data structurally explicit. | CreateEventRequest and CreateEventRequestValidator cover sessions, days, rooms, agenda, locations, lookups, aspects, categories, and tags; only Title is universally required and server defaults supply persistence invariants. | High | Community mode can reduce publication readiness without accepting invalid values when optional data is supplied. |
| The event aggregate has substantially more public data than the community record. | Event, EventSession, EventDay, EventAgendaItem, aspects, category/tag joins, location/room joins, language/speaker joins, and custom-property value entities exceed the lexicon's native fields. | High | Build a typed public publication snapshot and exhaustive coverage manifest; render every non-native field into one description. |
| CarpaNet supplies the required OAuth protocol machinery. | Local docs plus Context7 package /drasticactions/carpanet confirm OAuthClientConfig, AuthorizeAsync, CallbackAsync, RestoreSessionAsync, SignOutAsync, IOAuthStateStore, and IOAuthSessionStore. | High | Our work is storage, hosting, BFF/API trust, and policy glue. |
| CarpaNet local-source lexicon generation can be hermetic. | /home/amir/dev/Github/CarpaNet/docs/docs/project-setup.md documents LexiconFiles; network LexiconResolve and auto-resolution are optional. | High | Vendor required schemas and do not resolve them over DNS in CI builds. |
| CarpaNet supports the required outbound and inbound primitives. | Local docs/source and Context7 /drasticactions/carpanet document OAuthSession.RestoreSessionAsync, generic repository record operations, and Jetstream SubscribeAsync with WantedCollections, long cursor, commit operations, records, and deletes. | High | Keep CarpaNet behind Infrastructure adapters and filter Jetstream to the two vendored community collections. |
| CarpaNet.AspNetCore is not an OAuth metadata helper. | Local CarpaNet.AspNetCore docs/source describe generated ASP.NET XRPC controllers. | High | YAGNI: do not add it for two small metadata endpoints. |
| The report advertises Part B sections that are absent. | Header references B1-B9, but the file ends at line 321 after a summary of Part B decisions. | High | The user's 2026-07-18 clarification resolves the governing toggle, validation profile, DB/PDS ordering, and projection rules; ADR-015 is now an implementation task rather than an external blocker. |
| The report says seven publish handlers but names five event plus three registration handlers. | Part B summary in dev/report/atproto-report.md. | High | Treat the explicitly named set as eight and cover it in Tasks 9.2 and 9.3. |
| A fresh attributable baseline exists. | `rtk dotnet build --configuration Release --verbosity quiet` completed at HEAD `aefa7797054c58a1233267835417aea46830b050` with 25 projects, 0 errors, and 0 warnings on 2026-07-18 before ATProto product edits. | High | The dirty-path hashes and command result are recorded under `.omo/evidence/atproto-auth/task-1/`. |

### 2.2 Existing Implementation

#### Domain and persistence

- UserAuthenticationToken is tenant-scoped and auditable but stores incomplete plaintext session fields.
- UserExternalLogin is the identity-link authority; SyncUserCommandHandler already normalizes the atproto provider and refuses unsafe email matching.
- IndexedDid stores DID, handle, and PDS host.
- AtprotoRecord is linked from Event and EventRegistration, despite the report calling out a missing entity link; it still lacks tenant/direction/provenance, payload/correlation, and settlement state adequate for inbound and outbound records.
- PdsSyncOutbox and its worker/repository are present but unused by lifecycle handlers; the entity lacks tenant/user ownership, stable aggregate-version idempotency, recoverable leases, and complete settlement data.

#### Application and API

- UserAuthenticationToken CRUD handlers and safe response DTOs exist.
- UserController.SyncUser is the existing identity synchronization route.
- MultiAuth has a policy selector for API key versus Keycloak JWT.
- UserAuthenticationTokenController exposes authenticated metadata reads/deletion plus generic credential create/update endpoints; AtprotoRecordController separately exposes direct record mutations that must be deleted so federation records have one lifecycle-owned write authority.
- Existing rate-limit, endpoint-classification, ProblemDetails, API versioning, HAL policy, and route-name conventions are reusable.

#### BFF and client

- DynamicAuthSchemeManager conditionally registers the ATProto stub.
- BffAuthEndpoints provides challenge, signout, provider discovery, and refresh endpoints.
- BffProviderReadinessService currently reports ATProto ready without checking its required configuration.
- LoginRedirect already renders a handle-input provider and sanitizes return URLs.
- AuthenticationState serialization is display-safe; tokens remain server-side.

### 2.3 Existing Tests And Verification Coverage

- tests/Explore.Blazor.IntegrationTests/Endpoints/BffAuthEndpointValidationTests.cs protects challenge input validation.
- tests/Explore.Blazor.IntegrationTests/Services/BffProviderReadinessServiceTests.cs and BffSessionRefreshServiceTests.cs cover adjacent BFF behavior.
- tests/Event.Application.UnitTests/Features/Users/Commands/SyncUserCommandHandlerTests.cs protects explicit ATProto linking and email rules.
- tests/Event.Application.UnitTests/Features/UserAuthenticationTokens covers current token handler privacy and self-scope.
- tests/Event.API.IntegrationTests/Features/UserAuthenticationTokenControllerMetadataTests.cs protects safe token metadata.
- tests/Event.API.IntegrationTests/Features/NoKeycloakAuthenticationTests.cs and authorization fixtures cover current API auth routing.
- tests/Event.Persistence.IntegrationTests has tenant-isolation infrastructure but no ATProto OAuth session round-trip test.
- tests/Event.Architecture.Tests enforces layer boundaries but has no CarpaNet dependency allowlist test.
- Gaps: no real challenge/callback, state single-use, DID triple-match, bootstrap assertion replay, encrypted session round-trip, JWT issuer routing, refresh, revocation, or canonical-host handoff coverage.

### 2.4 Existing Documentation And Contracts

- docs/SECURITY-MODEL.md and docs/AUTHORIZATION.md define the BFF/token and authorization boundaries.
- docs/FEDERATION.md and docs/LEXICONS.md describe existing ATProto/federation intent.
- docs/CONFIGURATION.md and docs/SECRETS.md own operator settings and secret rotation.
- docs/API.md and docs/API_CHANGELOG.md own endpoint conventions and breaking changes.
- schemas/islamu-event.md owns the persisted schema narrative.
- schemas/lexicons/lexicon-community-calendar-events.json and lexicon-community-calendar-rsvp.json are the canonical repository lexicons for Phases 8-10.
- Generated API client: src/Explore.Blazor.Client/Clients/EventApiClient.g.cs with source-generation metadata in AppJsonSerializerContext.cs.
- Overlap: dev/active/secrets-refactor-control-plane owns SecretDefinitionRegistry/ISecretResolver and its future auth-secret migration; dev/pause/blazor-clean-code-refactor task 6A.5 already names the ATProto stub.

### 2.5 Current Pain Points / Improvement Areas

- The advertised provider returns 501.
- The API cannot validate a PDS DPoP token as a platform bearer token.
- Generic raw-token mutation contracts are broader than the required BFF-only bootstrap.
- Current token columns are too small, incomplete for OAuthSessionData, and unencrypted.
- ATProto readiness is optimistic and can advertise a broken provider.
- Multi-node OAuth state needs an atomic consume operation; ordinary IDistributedCache get/remove is insufficient.
- Canonical callback hosting needs a one-time tenant handoff for custom domains.
- CarpaNet accepts supplied HTTP clients for core flows, but local source inspection found ATProtoOAuthClient construction paths that may instantiate a client internally; the implementation must prove or contain that egress.
- At the planning baseline, federation governance exposed only the legacy decentralization capability; the ATProto Events capability, validation profile, administrator locks, and user publication consent had not yet been implemented.
- Existing event detail/list projections do not guarantee a complete, deterministic, public event snapshot for PDS publication.
- A worker row can remain Processing after a crash and the completion path discards URI/CID, so idempotent retry and RSVP strongRef ordering are incomplete.

### 2.6 Unknowns After Investigation

| Unknown | Search performed | Owning task / resolution |
|---|---|---|
| Exact released CarpaNet version matching the verified docs/API. | NuGet stable package inventory, local source at commit `a24d54bf6a9ce3bbf7c1961d37ab099abe1d1a65`, local docs, generated-binding compilation, and Context7 were compared. | Resolved by Task 1.1: pin `CarpaNet`, `CarpaNet.OAuth`, and `CarpaNet.Jetstream` 1.0.2 with lock-file content hashes. |
| Whether every CarpaNet resolver/session call can use the hardened HttpClient. | Local docs/source and Context7 inspected; OAuth config is injectable, but an authenticated client path creates a client internally. | Task 1.3 proves the boundary. If not injectable, readiness remains blocked unless deployment egress policy is documented and enforced. |
| New ATProto user creation versus explicit linking. | SyncUser handler and tests inspected; the report both mandates no auto-match and claims new-user first-login success. | Safe default in Decision A7: linked accounts only. User approval is required to add account-linking UX or an email-less user model. |
| Exact Part B B1-B9 prose. | Full report read; sections are absent. | The user clarification is authoritative for enablement, validation, projection, and ordering. Tasks 9.1, 10.1, and 11.1 record remaining persistence/moderation/HAL choices in ADR-015 without weakening those requirements. |
| ADR-015 payload/entity/correlation shape. | AtprotoRecord, Event/EventRegistration FKs, outbox, worker, repository, lifecycle handlers, and lexicons inspected. | Task 9.1 resolves it before any outbound/inbound runtime edit. |
| Maximum acceptable encoded repository-record size for the pinned CarpaNet/PDS path. | The vendored community lexicon has no description maxLength; repository record limits are a protocol/server constraint. | Task 8.2 records the pinned limit/evidence and makes overflow a permanent no-PDS projection result—never truncation. |
| LSP symbol index. | Roslyn status was healthy, but two document/workspace symbol requests timed out during solution load. | Text/source evidence is sufficient for planning; implementation must retry LSP after the workspace is warm before renaming or moving symbols. |

## 3. Proposed Future State

### 3.1 End-to-end authentication flow

1. LoginRedirect sends provider, normalized handle, and safe return URL to the BFF challenge endpoint.
2. The BFF resolves the handle through the hardened CarpaNet client, captures expected DID, tenant, origin, and return path in a protected flow envelope, then calls OAuthSession.AuthorizeAsync with the DID.
3. CarpaNet performs authorization-server discovery, PAR, PKCE, DPoP, and redirect generation. The BFF state store persists CarpaNet state with a short TTL and atomic consume.
4. On /signin-atproto, the state store consumes the entry and populates a scoped flow context before CarpaNet calls the API-backed IOAuthSessionStore.
5. The API-backed store posts OAuthSessionData to POST /api/auth/atproto/session with a one-minute, audience-bound, single-use BFF assertion signed by the OAuth client key. The endpoint remains authorized; it never trusts browser headers or the BFF's identity claim.
6. The API places the submitted session in a temporary CarpaNet store, restores it, calls com.atproto.server.getSession through the hardened client, and requires equality of expected DID, token subject, authenticated CarpaNet DID, and PDS response DID.
7. The Application handler reuses SyncUserCommand for an already-linked UserExternalLogin, then atomically upserts IndexedDid and the encrypted UserAuthenticationToken session envelope, and requests a first-party session JWT. If the downstream upsert fails, the next verified login repairs it idempotently.
8. The API returns only the platform JWT and display-safe identity metadata. The BFF stores that JWT in the protected cookie authentication ticket and signs in the user.
9. For a different tenant host, the canonical callback stores the result behind a one-time opaque handoff code; the destination host atomically consumes it and creates its own cookie. The platform JWT itself is never put in the URL.
10. YARP forwards the first-party JWT. MultiAuth selects AtprotoSession only from a bounded unvalidated issuer hint, then JwtBearer performs complete issuer, audience, signature, lifetime, algorithm, and key-id validation.
11. Refresh restores the encrypted CarpaNet session, lets CarpaNet rotate PDS tokens, persists the rotated envelope, and issues a new platform JWT. Signout attempts remote revocation and deletes the stored session, while local cookie deletion remains authoritative.

### 3.2 End-to-end outbound event flow

1. Resolve the effective ATProto Events governance for the event tenant. The master capability is enabled only when `federation.atproto_events_enabled` resolves true; instance locks determine whether tenant administrators may override that value and the validation profile.
2. Resolve `platform` or `community_lexicon`. Platform mode preserves current event publish requirements. Community mode is eligible only while the effective ATProto Events capability is enabled; otherwise publication readiness resolves to platform. When eligible, it requires the lexicon's user-facing minimum—`name`, mapped from Title, while `createdAt` is server generated—plus non-relaxable application invariants for tenant, owner, authorization, status, concurrency, persistence shape, and every optional value actually supplied.
3. Run this profile in both direct create-as-published and later PublishEvent. A draft create never enqueues a PDS create. If local readiness fails, the application event is not published and no PDS outbox row exists.
4. Require explicit User-tier publication consent, an owner-linked DID, and an encrypted restorable CarpaNet session. The auth login toggle does not substitute for federation consent.
5. Inside the existing `IUnitOfWork` transaction, persist the local event/complete child graph or publish transition first in the unit of work, load/map the canonical public publication snapshot, validate the generated community record, and add a PDS outbox row. There is no network call in the transaction.
6. The snapshot first applies the existing `EventLocationDisclosureEvaluator` with `EventLocationDisclosurePurpose.Public`. The event mapper writes native fields to `name`, `createdAt`, `startsAt`, `endsAt`, `mode`, `status`, disclosed `locations`, `uris`, and `rsvpExpected` when representable. It renders every remaining public/federatable snapshot field into deterministic human-readable sections in the same record's `description`; an independently maintained source-field manifest proves coverage rather than deriving coverage from the projection it audits.
7. If snapshot coverage is incomplete, a value cannot be rendered safely, or the complete encoded record exceeds the verified limit, local publication can succeed but no PDS outbox is inserted. Emit a bounded structured reason and expose federation status; never omit or truncate data to force a PDS write.
8. Only after the database transaction commits can `PdsSyncWorker` claim the row. Immediately before remote I/O it rechecks effective capability, the owner's current publication consent, and public-location disclosure, then restores the owner's session by DID and writes a deterministic preallocated record key. A rolled-back application event therefore cannot become a PDS event, and revocation after enqueue prevents an unstarted remote write.
9. The worker settles URI/CID and the Event/AtprotoRecord link transactionally. A crash after remote success retries the same record key and reconciles instead of creating a duplicate.
10. Update/cancel/delete/heavy-redact operations enqueue only when an outbound AtprotoRecord already exists. RSVP writes wait for the event URI/CID and use that strongRef. Remote failure never rolls back or deletes the committed application event.

### 3.3 End-to-end inbound event flow

1. One leased multi-node CarpaNet.Jetstream consumer runs only while at least one effective instance/tenant ATProto Events capability is enabled and subscribes with `WantedCollections` restricted to `community.lexicon.calendar.event` and `community.lexicon.calendar.rsvp`.
2. Persist the long cursor and process create/update/delete commits idempotently by DID, collection, and record key. Apply the report's curated-allowlist moderation policy before a record can enter a public read model.
3. Validate records against the vendored lexicons and persist each DID/collection/rkey version once as a global canonical inbound record, independent of tenant visibility. Preserve provenance, never route an inbound record through local publish handlers or the outbound outbox, and match locally-owned URI/CID records to prevent echo duplicates.
4. Deletes/tombstones purge or suppress the inbound projection and dependent RSVP state. Cursor advancement and record persistence follow the chosen atomic checkpoint policy recorded in ADR-015.
5. Tenant presentation/visibility joins decide which global canonical records appear in home and event-list queries for an enabled tenant. API responses own tenant filtering and HAL links; the client renders source/federation state and gates actions solely from links. No tenant gets a duplicate record copy or its own Jetstream socket.

### 3.4 Ownership

- **Blazor BFF:** user input, OAuth protocol orchestration, client metadata/JWKS, state, client assertion, cookie, canonical-host handoff, safe redirects, provider readiness.
- **Application:** establish/refresh/revoke use cases, manual validation, identity orchestration, effective federation governance, validation-profile selection, public snapshot mapping/formatting, outbox planning, tenant/user invariants, and inbound read-model use cases.
- **Infrastructure:** CarpaNet adapters, constrained networking, OAuth session encryption, PDS verification, session JWT signing, authenticated repository writes, and filtered Jetstream subscription/parsing.
- **Persistence:** tenant/DID-scoped entity queries, full event aggregate loading, transactional outbox/AtprotoRecord settlement, cursor/read-model persistence, and EF schema only; repositories continue returning entities.
- **API:** authenticated bootstrap/session endpoints, MultiAuth schemes, federation/admin/user-setting endpoints, public HAL resources, rate limiting, ProblemDetails, and OpenAPI.
- **WASM:** display-safe provider/session/federation metadata, settings, consent, and HAL-driven event presentation only; no OAuth/PDS credential material and no local claim/role authorization.

## 4. Non-Negotiable Constraints

1. Repositories return entities, never DTOs.
2. Validators are manually instantiated in handlers.
3. int remains for lookup IDs, UUIDv7 Guid for aggregates, and long for cursors.
4. Every write endpoint remains authorized. The pre-session bridge uses a dedicated AtprotoBootstrap authentication scheme, not AllowAnonymous.
5. Every created or touched file gains the required two ABOUTME lines.
6. UI mutation affordances remain HAL-driven; no local role/claim gating.
7. The BFF does not reference Application or Persistence; mirrored constants/server-only clients stay within its allowed references.
8. Browser-controlled privileged headers are stripped; the server injects the bootstrap assertion.
9. PDS access/refresh tokens, ID token, private DPoP JWK, OAuth client private JWK, encryption keys, and session-signing keys are never logged or serialized to WASM.
10. DID equality is checked in both BFF and API, and the API independently calls getSession.
11. State is short-lived, single-use, tenant-bound, and issuer-bound.
12. Resolver and PDS traffic is HTTPS-only, bounded, redirect-disabled, private-address-blocked, and timeout/size-limited; production readiness fails closed if the CarpaNet call path cannot be constrained.
13. The first-party session JWT uses a dedicated issuer, audience, ES256 key ring, kid, short TTL, and narrow clock skew; it is never accepted as an ATProto bootstrap assertion.
14. auth.atproto_login_enabled and federation.atproto_events_enabled remain independent. Federation also requires explicit user publication consent and a usable linked session before outbound work.
15. One effective ATProto Events capability governs both inbound fetching and outbound event/RSVP publication; do not reintroduce separate administrator fetch/publish toggles.
16. A PDS event can be created only from a committed application event publication. Local readiness failure means neither local publication nor outbox; database rollback means no remotely visible PDS event.
17. The worker never creates an event from update/delete/redaction paths when no outbound AtprotoRecord exists. Initial remote creation belongs only to direct create-as-published, later PublishEvent, or a future explicit HAL sync action.
18. `community_lexicon` relaxes required business fields, not authorization, ownership, tenant isolation, concurrency, server-generated identifiers/timestamps, storage constraints, reference integrity, privacy, rate limits, or validation of optional values supplied.
19. The single event description contains every non-native field from the typed public/federatable snapshot, including all sessions, EAV/custom properties, aspects, and resolved lookups. Coverage gaps and size overflow fail closed for PDS publication; truncation and silent omission are forbidden.
20. “All event information” means all fields approved for the canonical public/federatable snapshot. Secrets, private registration/attendee data, moderation/report evidence, audit/concurrency/soft-delete internals, and internal-only identifiers remain excluded by explicit policy.
21. No backward-compatibility code. Replace obsolete raw-token writes and prior split federation-toggle assumptions rather than wrapping or retaining them.
22. Phase verification runs once after all tasks: one Release build and at most one selected non-browser project test.
23. No browser, Aspire, Docker, live-PDS, Playwright, manual-QA, or migration-command verification is scheduled in these phase gates.
24. The bootstrap/session bridge is a server-private BFF-to-API contract and is excluded from public OpenAPI generation, generated WASM clients, HAL, and browser serializers.
25. `AtprotoRecord` has no public direct mutation authority. Delete its create/update/delete controller actions, DTOs, commands, handlers, generated methods, and mutation links; lifecycle outboxes and canonical ingress own writes.
26. ATProto capability/profile locking uses `SettingDefinition.IsLockable`, the existing lock/unlock commands, persisted lock state, and effective-setting metadata. Do not create parallel `lock_tenant_*` setting keys.
27. `community_lexicon` can relax publication readiness only when the effective ATProto Events capability is enabled; disabled/unknown state uses platform readiness.
28. Event and RSVP projections have independent typed contracts and independently maintained source-field manifests. Public location values pass `EventLocationDisclosurePurpose.Public` at snapshot time and again before delivery.
29. The worker rechecks effective capability and current self-consent immediately before every remote write; a stale claim cannot bypass revocation.
30. Organizer `ApprovalStatus` is workflow state, never ATProto RSVP intent. A successfully committed active `EventRegistrationIntent`/registration lifecycle projects only `community.lexicon.calendar.rsvp#going`; user cancellation/deletion deletes that remote RSVP. Do not emit `interested` or `notgoing` until an explicit local user-intent model exists.

## 5. Architecture And Design Decisions

### A1 — CarpaNet is the sole OAuth protocol implementation

- **Decision:** Use CarpaNet and CarpaNet.OAuth. Use CarpaNet-generated getSession bindings from vendored lexicons. Do not add FishyFlip or CarpaNet.AspNetCore.
- **Why:** Local docs, source, and Context7 confirm that CarpaNet already implements identity resolution, PAR, PKCE, DPoP, nonce retry, callback validation, refresh, restore, and signout.
- **Alternatives considered:** FishyFlip; hand-written OAuth; Keycloak broker; CarpaNet.AspNetCore.
- **Consequences:** Pinning and an egress compatibility proof are mandatory because the library is still evolving.
- **Affected layers:** BFF, Infrastructure, package management, schema assets.

### A2 — Preserve the authorized-write invariant with a server-private ATProto bootstrap assertion

- **Decision:** Protect POST /api/auth/atproto/session with a dedicated JwtBearer scheme. The BFF signs a short-lived assertion using its OAuth client ES256 key; it carries client identity, tenant, audience, jti, method, and route, but no user identity authority. The API consumes jti once and still verifies the PDS session independently. The bridge is server-private and excluded from browser OpenAPI/client generation, HAL, and public serializers.
- **Why:** The report needs an API call before a platform JWT exists, while the repository forbids anonymous writes and rejects BFF-trusted identity headers.
- **Alternatives considered:** AllowAnonymous bridge; trusted identity headers; accepting the PDS token directly; mTLS-only trust.
- **Consequences:** The OAuth client key also authenticates this BFF instance; the first-party session signing key remains separate.
- **Affected layers:** BFF, API auth, secrets, distributed cache, integration tests.

### A3 — One CarpaNet session-store contract, two adapters, one table

- **Decision:** Implement IOAuthSessionStore in the BFF as a dedicated server-private API adapter and in Infrastructure as a repository-backed adapter. UserAuthenticationToken is the only durable ATProto session store; no bridge model enters the general generated `IEventApiClient` surface.
- **Why:** This matches report D2/D5, respects Blazor isolation, and enables future RestoreSessionAsync worker use.
- **Alternatives considered:** BFF database reference; BFF-only cache; a second OAuth-session table.
- **Consequences:** A scoped callback context bridges CarpaNet's state consume to StoreAsync so the API receives the expected DID and tenant before persistence.
- **Affected layers:** BFF, Application contracts/models, Infrastructure, Persistence.

### A4 — Persist a versioned encrypted OAuthSessionData envelope

- **Decision:** Replace individual plaintext credential columns with SubjectDid, SessionCiphertext, EncryptionKeyId, plus safe PdsHost/ExpiresAt metadata. Serialize the complete CarpaNet OAuthSessionData, then encrypt it using AES-GCM and an instance key ring resolved by Explore.Secrets.
- **Why:** The current columns are incomplete and size-limited; a single authenticated envelope round-trips the library contract without leaking Carpa types into Domain.
- **Alternatives considered:** Per-column encryption; API Data Protection cookie key ring; storing only a reference.
- **Consequences:** Existing ATProto sessions are invalidated during this development migration; there is no dual reader or plaintext fallback. Key rotation can decrypt by kid and rewrite under the active key.
- **Affected layers:** Domain, Application repository contract, Infrastructure, Persistence, migration, secrets docs.

### A5 — Three explicit cryptographic purposes

- **Decision:** Use separate key material for OAuth client/bootstrap signing, OAuth-session envelope encryption, and API session-JWT signing. Each is instance-scoped and rotation-capable. Canonical keys are auth.atproto.oauth_client_private_jwks, auth.atproto.session_encryption_keyring, and auth.atproto.session_jwt_private_jwks.
- **Why:** Purpose separation limits blast radius and prevents a metadata/client key from signing API session tokens.
- **Alternatives considered:** One shared key; BFF cookie Data Protection ring; ephemeral process keys.
- **Consequences:** SecretDefinitionRegistry and BFF Infisical mapping must be coordinated with secrets-refactor-control-plane before implementation.
- **Affected layers:** Domain secret registry, Explore.Secrets consumers, BFF configuration, API/Infrastructure, operator docs.

### A6 — Atomic transient state and opaque cross-host handoff

- **Decision:** Use Redis GETDEL for state/handoff in configured multi-node deployments and a process-local atomic store only for explicit single-node development. Never use non-atomic IDistributedCache get-then-remove. Handoff URLs carry only a random one-time code.
- **Why:** Replay resistance and custom-domain cookies require atomic server-side transfer.
- **Alternatives considered:** JWT in query string; broad cookie domain; ordinary distributed cache; disabling custom domains.
- **Consequences:** ATProto readiness is false for multi-node production when Redis atomic consume is unavailable.
- **Affected layers:** BFF services/endpoints, readiness, configuration, tests.

### A7 — Linked-account sign-in is the safe default

- **Decision:** Preserve SyncUserCommandHandler's rule: ATProto has no verified email, so sign-in succeeds only for a pre-existing UserExternalLogin. Do not auto-match email, invent email, or silently create an incomplete User.
- **Why:** The report explicitly rejects email auto-match, and current domain creation requires email.
- **Alternatives considered:** Synthetic email; making User email nullable; implicit DID-only account creation; account-link UI in this scope.
- **Consequences:** The report's DoD statement that first ATProto login creates a new user is not currently achievable. A separate approved account-linking/onboarding decision is required.
- **Affected layers:** Application behavior, BFF error mapping, docs, acceptance criteria.

### A8 — One governed ATProto Events capability controls fetch and publication

- **Decision:** Replace the report summary's split administrator fetch/publish controls with the single lockable `federation.atproto_events_enabled` definition. Default is disabled and instance-locked through the existing `IsLockable` lock engine. Enabling the effective capability makes both inbound fetch and outbound publication available; outbound still requires User-tier consent, linked DID/session, and an eligible lifecycle transition.
- **Why:** This is the user's explicit 2026-07-18 clarification and avoids configurations where an administrator believes “ATProto Events” is on while half the feature silently remains off.
- **Alternatives considered:** Separate fetch/publish toggles; auth.atproto_login_enabled as an implicit federation toggle; automatic user publication without consent.
- **Consequences:** Instance administrators can unlock tenant control, but cannot use the master switch to bypass user consent. Disabling stops new fetch/publication work without deleting local business data.
- **Affected layers:** Domain settings, Application policy, workers, API, admin/user UI.

### A9 — Validation strictness is an administrator-governed profile

- **Decision:** Add the lockable `federation.atproto_event_validation_profile` definition with `platform` (default) and `community_lexicon`, reusing the existing lock engine rather than a second lock key. Apply community readiness to direct create-as-published and PublishEvent only while the effective ATProto Events capability is enabled; otherwise use platform readiness. Community mode requires the lexicon's `name` and server-generated `createdAt`, while retaining non-relaxable platform safety/persistence invariants and validation for every optional supplied value.
- **Why:** The community lexicon deliberately permits events without a schedule, while the current platform EventPublish profile requires scheduled sessions and rollups.
- **Alternatives considered:** Always require the stricter platform profile; bypass all application validation; make the profile a per-user choice.
- **Consequences:** In community mode, a title-only event can be locally published and federated. Tenant administrators can select it only when the instance lock is open. Existing databases remain feasible because optional event data is nullable/defaulted and strictness is primarily policy-driven, but Task 7.2 must prove schema compatibility.
- **Affected layers:** Domain settings, lifecycle policy/evaluator, create/publish handlers, admin API/UI.

### A10 — Local commit and transactional outbox precede every PDS event

- **Decision:** Persist the local publication transition and immutable PDS outbox command in the same `IUnitOfWork` transaction. Call CarpaNet only after commit. Use a stable preallocated TID/record key and idempotent/reconcilable write so retry after a settlement crash cannot create a duplicate.
- **Why:** This is the only ordering that guarantees no event can exist in a user's PDS without a corresponding committed application event.
- **Alternatives considered:** PDS-first then local save; remote call inside the database transaction; compensating delete; fire-and-forget after the response.
- **Consequences:** A PDS outage leaves the application event authoritative and the outbox retryable/dead-lettered. A database rollback leaves no claimable outbox row. Remote settlement updates AtprotoRecord separately after successful delivery.
- **Affected layers:** Application lifecycle handlers, Domain federation entities, Persistence transaction/repositories, API worker, Infrastructure CarpaNet gateway.

### A11 — One typed snapshot, one community event record, exhaustive description

- **Decision:** Map one typed, public/federatable `AtprotoEventPublicationSnapshot` to one `community.lexicon.calendar.event`, and map a successfully committed active `EventRegistrationIntent`/registration lifecycle through a separate typed RSVP projection as `community.lexicon.calendar.rsvp#going`. Organizer `ApprovalStatus` never determines user RSVP intent. User cancellation/deletion deletes the remote RSVP; `interested` and `notgoing` remain unsupported until an explicit local user-intent model exists. Native lexicon fields receive direct values. Every event snapshot field without a native property is rendered into deterministic, readable description sections; every session is embedded in that same description. Independently maintained event and RSVP source-field manifests require every source member to be mapped natively, rendered, or explicitly privacy-excluded before code can pass. Location values are admitted only through `EventLocationDisclosurePurpose.Public`.
- **Why:** The community lexicon has no native representation for most of the platform's event graph, and the user explicitly requires complete preservation in the description.
- **Alternatives considered:** Multiple event/session records; raw JSON dump; reflection over EF entities; best-effort/truncated summaries.
- **Consequences:** Projection is explicit, reviewable, localized/display-friendly, and safe. Unknown/new fields, unsupported values, or size overflow prevent PDS enqueue while leaving the committed local event intact and observable.
- **Affected layers:** Application snapshot/mapper/validator, Persistence aggregate loading, Infrastructure generated lexicon types, tests.

### A12 — Filtered Jetstream creates an inbound read model, never an outbound echo

- **Decision:** Use one leased multi-node CarpaNet.Jetstream consumer with only the community event and RSVP collections, a durable long cursor, curated allowlist moderation, idempotent DID/collection/rkey upserts into global canonical records, and tombstone deletion. Tenant presentation/visibility joins are separate from canonical ownership. Inbound records retain provenance and never invoke local publication/outbox logic.
- **Why:** A dedicated ingress path prevents echo loops and lets tenant settings/HAL safely control presentation.
- **Alternatives considered:** Poll every PDS; subscribe to all collections; materialize inbound records by calling local create handlers.
- **Consequences:** API queries can combine local and allowed federated events without confusing ownership. Tenants do not own duplicate inbound copies or sockets. Enabling/disabling affects tenant presentation and whether the shared consumer is needed, not deletion of unrelated local events.
- **Affected layers:** Infrastructure, Persistence, Application queries, API HAL, Blazor Client.

## 6. Implementation Phases

### Phase 1: CarpaNet Boundary, Client Identity, And ADR

- **Goal:** Adopt the exact CarpaNet packages, make source generation hermetic, publish a rotation-capable OAuth client identity, and prove the outbound-network boundary before authentication code depends on it.
- **Depends on:** Execution is approved; the Task 1.2 secrets ownership checkpoint is complete.
- **Related skills/rules:** clean-architecture-rules, auth-patterns, blazor-bff-patterns, agentic-research, blazor-server.md, tests.md.
- **Acceptance criteria:**
  - Exact package versions are pinned and lock files agree.
  - Required lexicons are repository-local; no CI build uses LexiconResolve or auto-resolve.
  - Public metadata/JWKS contain only public material and reflect the configured canonical URL.
  - ADR-014 records trust boundaries, key purposes, linked-account limitation, and selector safety.
  - CarpaNet traffic is proven constrainable or the provider fails readiness with a documented deployment egress requirement.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** Remove the package references/endpoints and leave the existing disabled stub. Do not continue to Phase 2 when exact package API, lexicon generation, or egress constraints are unresolved.

#### Task 1.1: Pin CarpaNet and make lexicon generation hermetic

- **Status:** Complete; independently verified on 2026-07-18.
- **Evidence:** `.omo/evidence/atproto-auth/task-2/README.md` records failing-first coverage, package/schema provenance, generated bindings, isolated NuGet restore/signature verification, locked restores, architecture/build gates, and protected-tree audit results.
- **Type:** modify / create / investigate
- **Layer:** Package management / Infrastructure / Blazor
- **Files:**
  - Directory.Packages.props (existing)
  - src/Explore.Blazor/Explore.Blazor.csproj (existing)
  - src/Explore.Infrastructure/Explore.Infrastructure.csproj (existing)
  - src/Explore.Blazor/packages.lock.json (existing, generated)
  - src/Explore.Infrastructure/packages.lock.json (existing, generated)
  - schemas/lexicons/com.atproto.server.getSession.json and the seven required event/RSVP transitive lexicons (new/existing)
  - tests/Event.Architecture.Tests/AtprotoDependencyBoundaryTests.cs (new)
- **Description:** Pin the stable 1.0.2 `CarpaNet`, `CarpaNet.OAuth`, and `CarpaNet.Jetstream` packages whose public APIs match local docs/source and Context7 evidence. Reference OAuth only in the BFF and Infrastructure, core only in Infrastructure, and Jetstream only in Infrastructure. Vendor the authoritative getSession, strongRef, event, RSVP, and four referenced location lexicons as the minimum closed eight-file dependency set; configure Infrastructure with only explicit `LexiconFiles` and keep all network/automatic resolution disabled. Do not add CarpaNet.AspNetCore.
- **Acceptance Criteria:**
  - [x] CarpaNet, CarpaNet.OAuth, and CarpaNet.Jetstream use exact central versions with consistent lock files.
  - [x] Infrastructure generates getSession, event, and RSVP bindings from the exact eight-file local schema closure without DNS/network resolution.
  - [x] Architecture tests allow CarpaNet only in BFF/Infrastructure and reject it in Domain/Application/WASM/Persistence.
  - [x] The selected package version and evidence source are recorded in this workstream context.
- **Dependencies:** None.
- **Effort:** M
- **Required Skills/Rules:** agentic-research, clean-architecture-rules, tests.md.

#### Task 1.2: Record ADR-014 and implement client metadata/key publication

- **Status:** Complete; independently verified on 2026-07-18.
- **Evidence:** Implementation evidence is in `.omo/evidence/atproto-auth/task-3a/`; independent verification is in `.omo/evidence/atproto-auth/task-3a-verifier/`. The verifier confirmed the security rework for canonical callback/public-origin validation, strict canonical base64url JWK parsing, public-only JWKS, key-ring failure modes, and server-only secret ownership.
- **Type:** create / modify
- **Layer:** Blazor / Domain secrets / Docs
- **Files:**
  - docs/adr/ADR-014-atproto-session-trust-bridge.md (new)
  - src/Explore.Blazor/Authentication/AtprotoAuthenticationOptions.cs (existing)
  - src/Explore.Blazor/Extensions/AtprotoOAuthEndpointExtensions.cs (new)
  - src/Explore.Blazor/Extensions/AuthenticationExtensions.cs (existing)
  - src/Explore.Blazor/Extensions/BffEndpointExtensions.cs (existing)
  - src/Explore.Blazor/Extensions/ConfigurationExtension.cs (existing)
  - src/Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs (existing)
  - src/Explore.Blazor/Services/Auth/AtprotoClientKeyProvider.cs (new)
  - src/Explore.Domain/Secrets/SecretDefinitionRegistry.cs (existing)
  - tests/Explore.Blazor.IntegrationTests/Endpoints/AtprotoOAuthPublicationTests.cs (new)
- **Description:** Add canonical public URL/client ID/redirect/scope options; publish /oauth/client-metadata.json and /oauth/jwks.json; resolve the private ES256 JWK set server-side; expose active and retired public keys by kid during rotation. Register the three direct instance-only ATProto secret purposes without a legacy `InfrastructureSecretSettingKeys` compatibility constant or Domain/Application reference from the BFF. ADR-014 records the resulting trust model. Operator documentation remains assigned to the later owning configuration/operations task.
- **Acceptance Criteria:**
  - [x] Metadata uses the URL client_id, exact redirect URI, private_key_jwt, dpop_bound_access_tokens, and scope atproto transition:generic.
  - [x] JWKS never emits private parameters; unknown/missing/invalid keys fail readiness.
  - [x] OAuth client/bootstrap, session encryption, and API session signing key purposes are separate and documented.
  - [x] Metadata endpoints are GET, anonymous, cache-bounded, and size-bounded; focused BFF integration coverage now protects their HTTP behavior and Task 4.2 retains end-to-end flow regression coverage.
  - [x] ADR-014 records A1-A7 and rejects anonymous bridge writes and BFF-trusted user identity.
- **Dependencies:** 1.1 and secrets-refactor-control-plane ownership checkpoint.
- **Effort:** L
- **Required Skills/Rules:** auth-patterns, blazor-bff-patterns, blazor-server.md, docs/DOCUMENTATION_STYLE_GUIDE.md.

#### Task 1.3: Constrain CarpaNet outbound networking and startup readiness

- **Status:** Complete; independently scoped-confirmed on 2026-07-18.
- **Evidence:** `.omo/evidence/atproto-auth/task-3/README.md` records the shared package-free transport, CarpaNet real-flow regressions, operator docs, green scoped suites, attributable-warning repair, and unrelated latest root-lane limitation.
- **Type:** investigate / create / modify
- **Layer:** Blazor / Infrastructure
- **Files:**
  - src/Explore.Blazor/Services/Auth/AtprotoOAuthClientFactory.cs (new)
  - src/Explore.Infrastructure/Services/Federation/AtprotoOAuthClientFactory.cs (new)
  - src/Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs (existing)
  - src/Explore.Infrastructure/InfrastructureServicesRegistration.cs (existing)
  - src/Explore.Blazor/Services/Auth/BffProviderReadinessService.cs (existing)
  - docs/SELF_HOSTING.md (existing)
  - docs/TROUBLESHOOTING.md (existing)
- **Description:** Supply named clients/handlers to all supported CarpaNet entry points. Enforce HTTPS, no redirects, DNS/IP checks against private/link-local/loopback ranges, bounded connect/request timeouts, response limits, and cancellation. Verify the local-source path that constructs an internal client. If it cannot accept the policy, require an enforced deployment egress boundary and make readiness fail closed until it is configured.
- **Acceptance Criteria:**
  - [x] Challenge, callback discovery, getSession, refresh, and signout each have a documented constrained transport path.
  - [x] DNS rebinding and redirect-to-private-address cases are rejected.
  - [x] Development loopback helpers are enabled only in Development and never weaken production policy.
  - [x] Provider readiness explains missing config/key/cache/egress prerequisites without exposing secrets.
- **Dependencies:** 1.1, 1.2.
- **Effort:** L
- **Required Skills/Rules:** auth-patterns, error-tracking, external-infrastructure-bootstrap intent.

### Phase 2: Encrypted DID-Keyed Session Persistence

- **Goal:** Make UserAuthenticationToken a faithful, tenant-safe, encrypted durable backing for CarpaNet OAuthSessionData.
- **Depends on:** Phase 1.
- **Related skills/rules:** dotnet-efcore-guidelines, clean-architecture-rules, efcore-persistence.md, efcore-migrations.md, domain-layer.md.
- **Acceptance criteria:**
  - A complete OAuthSessionData round-trips without plaintext persistence.
  - DID/provider/tenant uniqueness and query filters prevent cross-tenant access.
  - Rotation can read by kid and rewrite with the active encryption key.
  - Existing safe metadata reads still omit credential material.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** Rolling back invalidates ATProto sessions and requires reauthentication; no business entity is deleted. There is no plaintext fallback.

#### Task 2.1: Replace plaintext token persistence with a DID-keyed encrypted session envelope

- **Type:** modify / create
- **Layer:** Domain / Persistence / Docs
- **Files:**
  - src/Explore.Domain/UserAuthenticationToken.cs (existing)
  - src/Explore.Persistence/Configurations/Entities/UserAuthenticationTokenConfiguration.cs (existing)
  - src/Explore.Application/Contracts/Persistence/IUserAuthenticationTokenRepository.cs (existing)
  - src/Explore.Persistence/Repositories/UserAuthenticationTokenRepository.cs (existing)
  - src/Explore.Persistence/Migrations/<generated>_ProtectAtprotoOAuthSessions.cs (new, EF-generated timestamp)
  - src/Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs (existing, generated)
  - schemas/islamu-event.md (existing)
  - tests/Event.Persistence.IntegrationTests/Repositories/UserAuthenticationTokenRepositoryTests.cs (new)
- **Description:** Replace AccessToken/RefreshToken/DpopKey/IdToken persistence with required SubjectDid, SessionCiphertext, and EncryptionKeyId while retaining Provider, PdsHost, and ExpiresAt as safe metadata. Add tenant/provider/DID uniqueness and repository queries that always include tenant scope. Because the existing handler never created ATProto sessions, fail the migration if legacy ATProto rows exist rather than guessing a backfill.
- **Acceptance Criteria:**
  - [ ] No plaintext credential property/column remains in the runtime model.
  - [ ] The unique key prevents two active records for the same tenant/provider/DID while allowing the same DID in different tenants.
  - [ ] Repository methods return entities, use explicit tracking intent, accept cancellation, and never call IgnoreQueryFilters.
  - [ ] Migration and schema docs state that rollback invalidates sessions and requires login.
  - [ ] Every touched legacy file gains two ABOUTME lines.
- **Dependencies:** Phase 1.
- **Effort:** L
- **Required Skills/Rules:** dotnet-efcore-guidelines, efcore-persistence.md, efcore-migrations.md, domain-layer.md.

#### Task 2.2: Implement the repository-backed CarpaNet session store

- **Type:** create / modify
- **Layer:** Infrastructure / Persistence / Secrets
- **Files:**
  - src/Explore.Infrastructure/Services/Federation/AtprotoSessionEnvelopeProtector.cs (new)
  - src/Explore.Infrastructure/Services/Federation/RepositoryBackedOAuthSessionStore.cs (new)
  - src/Explore.Infrastructure/InfrastructureServicesRegistration.cs (existing)
  - src/Explore.Application/Contracts/Persistence/IUserAuthenticationTokenRepository.cs (existing)
  - tests/Event.Persistence.IntegrationTests/Federation/RepositoryBackedOAuthSessionStoreTests.cs (new)
  - docs/SECRETS.md (existing)
- **Description:** Serialize OAuthSessionData with a bounded source-generated JSON contract, encrypt/decrypt with AES-GCM using the Explore.Secrets key ring, and implement CarpaNet StoreAsync/GetAsync/DeleteAsync over the tenant-scoped repository. Zero temporary key/plaintext buffers where practical and classify corruption/unknown kid as reauthentication, not silent fallback.
- **Acceptance Criteria:**
  - [ ] Store/Get round-trips DPoP JWK, token set, auth method, client ID, redirect URI, scope, and PDS metadata.
  - [ ] Database inspection in the persistence test proves recognizable token/JWK substrings are absent.
  - [ ] Delete is tenant/DID scoped and idempotent.
  - [ ] Unknown kid, authentication-tag failure, and malformed envelope fail closed without secret values in logs.
  - [ ] Rewriting under the active kid is supported without a dual plaintext path.
- **Dependencies:** 2.1.
- **Effort:** L
- **Required Skills/Rules:** auth-patterns, dotnet-efcore-guidelines, error-tracking.

### Phase 3: Authenticated API Trust Bridge And MultiAuth

- **Goal:** Independently validate the ATProto session, reuse platform identity synchronization, persist the verified session, and mint/validate a first-party JWT.
- **Depends on:** Phase 2.
- **Related skills/rules:** cqrs-mediatr-guidelines, auth-patterns, clean-architecture-rules, api-controllers.md, application-layer.md.
- **Acceptance criteria:**
  - The bridge is authorized by AtprotoBootstrap and rate-limited.
  - DID equality and PDS getSession validation occur before any durable identity/session write.
  - Linked users receive a short-lived first-party JWT; unlinked users receive a safe actionable rejection.
  - MultiAuth validates ATProto and Keycloak tokens with separate complete validators.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** Disable the ATProto scheme/provider and delete incomplete session rows. Keycloak/API-key branches remain unchanged.

#### Task 3.1: Add the authenticated ATProto bootstrap boundary

- **Type:** create / modify
- **Layer:** Blazor / API / Security
- **Files:**
  - src/Explore.Blazor/Services/Auth/AtprotoBootstrapAssertionService.cs (new)
  - src/Explore.Blazor/Services/BffCookieForwardingHandler.cs (existing)
  - src/Explore.API/Authentication/AtprotoBootstrapAssertionValidator.cs (new)
  - src/Explore.API/Extensions/AuthenticationExtensions.cs (existing)
  - src/Explore.Application/Constants/ApiAuthenticationSchemeNames.cs (existing)
  - tests/Event.API.IntegrationTests/Authentication/AtprotoBootstrapAuthenticationTests.cs (new)
- **Description:** Sign a one-minute ES256 client assertion with exact issuer/client_id, API audience, tenant, HTTP method/path, issued/expiry times, and random jti. Strip any browser-provided assertion header, inject it only in the server HTTP client, validate it under a dedicated scheme, and atomically consume jti.
- **Acceptance Criteria:**
  - [ ] Missing, expired, wrong-audience, wrong-route, wrong-tenant, unknown-kid, non-ES256, and replayed assertions are rejected.
  - [ ] AtprotoBootstrap cannot authorize any endpoint except the bridge.
  - [ ] The assertion carries no trusted DID/user identity and the API still performs PDS verification.
  - [ ] Browser-supplied privileged headers are removed before proxying.
- **Dependencies:** 1.2, 1.3.
- **Effort:** L
- **Required Skills/Rules:** auth-patterns, blazor-bff-patterns, api-controllers.md.

#### Task 3.2: Verify, synchronize, persist, and mint the first-party session

- **Type:** create / modify
- **Layer:** Application / Infrastructure / API
- **Files:**
  - src/Explore.Application/Contracts/Identity/IAtprotoOAuthSecurityGateway.cs (new)
  - src/Explore.Application/Features/Authentication/Atproto/Models/AtprotoOAuthSessionMaterial.cs (new)
  - src/Explore.Application/Features/Authentication/Atproto/Requests/Commands/EstablishAtprotoSessionCommand.cs (new)
  - src/Explore.Application/Features/Authentication/Atproto/Handlers/Commands/EstablishAtprotoSessionCommandHandler.cs (new)
  - src/Explore.Application/Features/Authentication/Atproto/Validators/EstablishAtprotoSessionCommandValidator.cs (new)
  - src/Explore.Infrastructure/Services/Federation/AtprotoOAuthSecurityGateway.cs (new)
  - src/Explore.API/Controllers/AtprotoSessionController.cs (new)
  - src/Explore.API/Hateoas/RouteNames.cs (existing)
  - tests/Event.API.IntegrationTests/Features/AtprotoSessionBridgeTests.cs (new)
- **Description:** Accept a bounded server-only session material contract. Use a temporary CarpaNet session store and RestoreSessionAsync/getSession to verify expected DID, OAuth subject, authenticated client DID, and PDS DID before writes. Manually validate the request, call the public SyncUserCommand boundary for the already-linked identity, atomically upsert IndexedDid plus the encrypted session, then issue an ES256 platform JWT with sub=user Guid, did, handle, tenant, provider, issuer, audience, kid, jti, and short expiry. A verified retry repairs a failed downstream upsert idempotently.
- **Acceptance Criteria:**
  - [ ] No write occurs before all DID/PDS checks pass.
  - [ ] Unlinked ATProto identities fail without email matching or user creation.
  - [ ] A linked identity produces User/Actor/UserExternalLogin consistency, IndexedDid metadata, one encrypted session row, and a platform JWT.
  - [ ] Validator is manually instantiated; repositories return entities; IndexedDid/session writes are atomic and a retry safely repairs a post-SyncUser failure.
  - [ ] Controller has explicit version, route, route name, classification, authorization scheme, rate limit, response metadata, ProblemDetails, and no-store policy.
  - [ ] Request/exception logs contain only correlation IDs, tenant, PDS hostname classification, and redacted DID hash where necessary.
- **Dependencies:** 2.2, 3.1.
- **Effort:** XL
- **Required Skills/Rules:** cqrs-mediatr-guidelines, auth-patterns, clean-architecture-rules, api-controllers.md, application-layer.md.

#### Task 3.3: Route and validate ATProto session JWTs in MultiAuth

- **Type:** modify / create
- **Layer:** API / Security
- **Files:**
  - src/Explore.API/Extensions/AuthenticationExtensions.cs (existing)
  - src/Explore.Application/Constants/ApiAuthenticationSchemeNames.cs (existing)
  - src/Explore.API/Authentication/AtprotoSessionJwtOptions.cs (new)
  - tests/Event.API.IntegrationTests/Authentication/MultiAuthAtprotoSessionTests.cs (new)
  - docs/AUTHORIZATION.md (existing)
  - docs/SECURITY-MODEL.md (existing)
- **Description:** Add AtprotoSession JwtBearer with a distinct issuer/audience/key ring. Keep selector parsing bounded to issuer routing only; the selected handler performs full validation. Preserve API key and Keycloak fallback behavior and existing user-ID extraction order.
- **Acceptance Criteria:**
  - [ ] Only ES256, known kid, exact issuer/audience, valid lifetime, and required claims are accepted.
  - [ ] Oversized/malformed/claim-confused tokens are rejected without selector exceptions.
  - [ ] A token routed to the wrong scheme never succeeds.
  - [ ] API key and Keycloak regression cases remain green.
  - [ ] sub remains the platform user Guid so existing authorization and HAL policies work unchanged.
- **Dependencies:** 3.2.
- **Effort:** M
- **Required Skills/Rules:** auth-patterns, AUTHORIZATION.md, SECURITY-MODEL.md.

### Phase 4: BFF Challenge, Callback, Cookie, And Tenant Handoff

- **Goal:** Replace the 501 stub with the complete CarpaNet web flow and establish a protected cookie session on the correct tenant host.
- **Depends on:** Phase 3.
- **Related skills/rules:** blazor-bff-patterns, auth-patterns, blazor-ui-conventions, blazor-server.md.
- **Acceptance criteria:**
  - Valid handle input reaches CarpaNet authorization and callback.
  - State and handoff codes are single-use and tenant-bound.
  - BFF verifies the callback DID and consumes only a verified API result.
  - The protected cookie carries the platform JWT; serialized WASM state does not.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** Disable the provider and clear pending state/handoff entries. Existing cookies/providers remain unaffected.

#### Task 4.1: Implement single-use OAuth state and API-backed session adapters

- **Type:** create / modify
- **Layer:** Blazor
- **Files:**
  - src/Explore.Blazor/Services/Auth/AtprotoOAuthFlowContext.cs (new)
  - src/Explore.Blazor/Services/Auth/CacheBackedOAuthStateStore.cs (new)
  - src/Explore.Blazor/Services/Auth/ApiBackedOAuthSessionStore.cs (new)
  - src/Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs (existing)
  - tests/Explore.Blazor.IntegrationTests/Services/AtprotoOAuthStoreTests.cs (new)
- **Description:** Implement CarpaNet IOAuthStateStore with protected, bounded state and atomic consume; populate a scoped flow context from AppState during consume. Implement IOAuthSessionStore as the API bridge adapter; its StoreAsync reads expected DID/tenant from the scoped context, sends the bootstrap assertion, and captures the verified platform result for callback completion.
- **Acceptance Criteria:**
  - [ ] State expires within the configured short TTL and can be consumed exactly once.
  - [ ] State is bound to issuer, tenant, expected DID, origin, and safe return path.
  - [ ] API-backed StoreAsync never accepts a browser caller and never logs session material.
  - [ ] Get/Delete use authenticated API operations and remain tenant/DID scoped.
  - [ ] Redis GETDEL is used in configured multi-node deployments; local memory mode is explicitly single-node development only.
- **Dependencies:** 3.1, 3.2.
- **Effort:** L
- **Required Skills/Rules:** blazor-bff-patterns, auth-patterns, blazor-server.md.

#### Task 4.2: Complete challenge and callback processing

- **Type:** modify
- **Layer:** Blazor
- **Files:**
  - src/Explore.Blazor/Authentication/AtprotoAuthenticationHandler.cs (existing)
  - src/Explore.Blazor/Authentication/AtprotoAuthenticationOptions.cs (existing)
  - src/Explore.Blazor/Extensions/BffAuthEndpoints.cs (existing)
  - src/Explore.Blazor/Services/DynamicAuthSchemeManager.cs (existing)
  - tests/Explore.Blazor.IntegrationTests/Endpoints/AtprotoAuthenticationFlowTests.cs (new)
- **Description:** Pass login_hint and safe return metadata into AuthenticationProperties; normalize and bound the handle; resolve expected DID through the hardened client; call AuthorizeAsync; implement /signin-atproto with CallbackAsync; compare callback DID to expected DID; consume the bridge result; map failures to stable safe error codes.
- **Acceptance Criteria:**
  - [ ] Missing/invalid/oversized handles fail before DNS/HTTP resolution.
  - [ ] Challenge redirects only to the CarpaNet-produced HTTPS authorization URL.
  - [ ] Callback rejects state, issuer, DID, tenant, and flow-context mismatches.
  - [ ] BFF integration tests verify metadata/JWKS status, media type, cache policy, redirect URI, scope, and public-only key shape.
  - [ ] FishyFlip comments/stub behavior are removed.
  - [ ] Return paths remain local/allowlisted and raw exception/provider content never reaches the query string.
- **Dependencies:** 1.3, 4.1.
- **Effort:** L
- **Required Skills/Rules:** blazor-bff-patterns, auth-patterns, blazor-server.md.

#### Task 4.3: Complete cookie sign-in and canonical-host tenant handoff

- **Type:** create / modify
- **Layer:** Blazor
- **Files:**
  - src/Explore.Blazor/Services/Auth/AtprotoTenantSessionHandoffStore.cs (new)
  - src/Explore.Blazor/Extensions/BffAuthEndpoints.cs (existing)
  - src/Explore.Blazor/Services/ExploreBffCookieSessionHandler.cs (existing)
  - src/Explore.Blazor/Services/CircuitAccessTokenService.cs (existing)
  - tests/Explore.Blazor.IntegrationTests/Endpoints/AtprotoTenantHandoffTests.cs (new)
- **Description:** Build the cookie principal from verified API output, preserve sub/nameidentifier/sid fallback semantics, and save the first-party JWT in protected authentication properties. On a different allowed tenant host, store the result server-side and redirect with only a random one-time code; atomically consume it on the destination before SignInAsync.
- **Acceptance Criteria:**
  - [ ] Same-host callback signs in directly; cross-host callback uses one-time opaque handoff.
  - [ ] Handoff is origin/tenant/expiry bound and rejects replay or host substitution.
  - [ ] No JWT or PDS credential appears in URLs, browser storage, WASM auth state, or response bodies.
  - [ ] Cookie HTTPS, SameSite, antiforgery, and existing BFF token-forwarding behavior remain intact.
- **Dependencies:** 4.2.
- **Effort:** L
- **Required Skills/Rules:** blazor-bff-patterns, auth-patterns, SECURITY-MODEL.md.

### Phase 5: Session Refresh, Revocation, Readiness, And Operations

- **Goal:** Keep PDS and platform sessions coherent, make signout safe, and provide truthful bounded operational signals.
- **Depends on:** Phase 4.
- **Related skills/rules:** cqrs-mediatr-guidelines, auth-patterns, error-tracking, blazor-bff-patterns.
- **Acceptance criteria:**
  - Refresh persists rotated CarpaNet session data before issuing a replacement platform JWT.
  - Signout revokes best effort, deletes durable session state, and always clears the local cookie.
  - Readiness, metrics, and logs distinguish configuration, dependency, protocol, replay, validation, and revocation outcomes without secrets.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** Expire/clear the local cookie and require full reauthentication. Never keep a cookie alive because remote revocation failed.

#### Task 5.1: Refresh PDS and first-party sessions coherently

- **Type:** create / modify
- **Layer:** Application / Infrastructure / API / Blazor
- **Files:**
  - src/Explore.Application/Features/Authentication/Atproto/Requests/Commands/RefreshAtprotoSessionCommand.cs (new)
  - src/Explore.Application/Features/Authentication/Atproto/Handlers/Commands/RefreshAtprotoSessionCommandHandler.cs (new)
  - src/Explore.Infrastructure/Services/Federation/AtprotoOAuthSecurityGateway.cs (existing from 3.2)
  - src/Explore.API/Controllers/AtprotoSessionController.cs (existing from 3.2)
  - src/Explore.Blazor/Services/Auth/BffSessionRefreshService.cs (existing)
  - tests/Event.Application.UnitTests/Features/Authentication/Atproto/RefreshAtprotoSessionCommandHandlerTests.cs (new)
- **Description:** Add an AtprotoSession-authorized refresh command. Restore by tenant/DID, allow CarpaNet refresh/token events to update the durable encrypted envelope, verify the current PDS session, and then mint the replacement platform JWT. Integrate the BFF refresh service without applying Keycloak token-endpoint assumptions.
- **Acceptance Criteria:**
  - [ ] Only the authenticated user's tenant/DID session can refresh.
  - [ ] Rotated OAuthSessionData is durably stored before the new platform JWT is returned.
  - [ ] Missing/corrupt/revoked PDS session fails as reauthentication, not an infinite retry.
  - [ ] Concurrent refresh has one authoritative persisted result and does not regress token rotation.
  - [ ] Existing Keycloak refresh tests/behavior are preserved.
- **Dependencies:** Phase 4.
- **Effort:** L
- **Required Skills/Rules:** cqrs-mediatr-guidelines, auth-patterns, blazor-bff-patterns.

#### Task 5.2: Revoke remotely and clear locally on sign-out

- **Type:** create / modify
- **Layer:** Application / Infrastructure / API / Blazor
- **Files:**
  - src/Explore.Application/Features/Authentication/Atproto/Requests/Commands/RevokeAtprotoSessionCommand.cs (new)
  - src/Explore.Application/Features/Authentication/Atproto/Handlers/Commands/RevokeAtprotoSessionCommandHandler.cs (new)
  - src/Explore.API/Controllers/AtprotoSessionController.cs (existing from 3.2)
  - src/Explore.Blazor/Extensions/BffAuthEndpoints.cs (existing)
  - tests/Event.Application.UnitTests/Features/Authentication/Atproto/RevokeAtprotoSessionCommandHandlerTests.cs (new)
- **Description:** Restore the CarpaNet session and call SignOutAsync best effort, then delete the tenant/DID session idempotently. BFF signout invokes it for ATProto and clears the cookie regardless of the remote outcome.
- **Acceptance Criteria:**
  - [ ] Remote success and already-revoked cases delete the local durable session.
  - [ ] Remote outage is logged/metriced without exposing tokens and never prevents cookie deletion.
  - [ ] Cross-user/cross-tenant revoke is rejected.
  - [ ] Repeat signout is safe and returns the existing local signout behavior.
- **Dependencies:** 5.1.
- **Effort:** M
- **Required Skills/Rules:** cqrs-mediatr-guidelines, auth-patterns, error-tracking.

#### Task 5.3: Make provider readiness and telemetry truthful

- **Type:** modify
- **Layer:** Blazor / API / Infrastructure / Docs
- **Files:**
  - src/Explore.Blazor/Services/Auth/BffProviderReadinessService.cs (existing)
  - src/Explore.Blazor/HealthChecks/AtprotoAuthenticationHealthCheck.cs (new)
  - src/Explore.Infrastructure/Services/Federation/AtprotoAuthenticationMetrics.cs (new)
  - src/Explore.API/Program.cs (existing)
  - docs/CONFIGURATION.md (existing)
  - docs/SECRETS.md (existing)
  - docs/SELF_HOSTING.md (existing)
  - docs/TROUBLESHOOTING.md (existing)
  - tests/Event.Application.UnitTests/Features/Authentication/Atproto/AtprotoObservabilityPolicyTests.cs (new)
- **Description:** Report ATProto ready only when enabled and canonical URL, keys, atomic state backend, API bridge, and egress requirements are present. Add low-cardinality counters/durations for challenge, callback, verification, refresh, and revoke outcomes; document rotation, rerun, backup, and recovery.
- **Acceptance Criteria:**
  - [ ] Disabled provider is omitted; misconfigured provider is unavailable with a safe reason.
  - [ ] Metrics have bounded labels and no full DID, handle, URL query, token, JWK, or exception body.
  - [ ] Health checks do not perform per-probe live PDS login or leak configuration values.
  - [ ] Operator docs cover key rotation overlap, session invalidation, cache loss, PDS outage, and recovery.
- **Dependencies:** 5.1, 5.2.
- **Effort:** M
- **Required Skills/Rules:** error-tracking, external-infrastructure-bootstrap intent.

### Phase 6: Public Contract Cleanup And Safe Client Surface

- **Goal:** Remove obsolete credential-bearing generic mutation APIs, regenerate the client, and leave only safe session metadata/revocation behavior in browser-visible contracts.
- **Depends on:** Phase 5.
- **Related skills/rules:** openapi-contract-change intent, blazor-ui-conventions, api-controllers.md, blazor-client.md, api-hateoas.md.
- **Acceptance criteria:**
  - Browser-visible generated contracts contain no PDS token, ID token, or DPoP private-key properties.
  - Generic create/update token endpoints and handlers are removed rather than deprecated.
  - Direct `AtprotoRecord` create/update/delete contracts, handlers, generated methods, and HAL mutation links are removed so lifecycle/ingress ownership cannot be bypassed.
  - The server-private ATProto bridge is absent from browser OpenAPI/client/serializer surfaces.
  - Safe session metadata and revoke affordances remain authorized and HAL-driven where rendered.
  - API changelog clearly records the intentional development-stage breaking change.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** Regenerate from the last safe OpenAPI surface; never restore the raw credential DTOs as a compatibility measure.

#### Task 6.1: Remove secret-bearing generic token mutation contracts

- **Type:** delete / modify
- **Layer:** Application / API
- **Files:**
  - src/Explore.API/Controllers/UserAuthenticationTokenController.cs (existing)
  - src/Explore.API/Controllers/AtprotoRecordController.cs (existing; remove direct mutation actions)
  - src/Explore.API/Hateoas/Policies/AtprotoRecordLinkPolicy.cs (existing; remove mutation links)
  - src/Explore.Application/DTOs/UserAuthenticationToken/CreateUserAuthenticationTokenDto.cs (delete)
  - src/Explore.Application/DTOs/UserAuthenticationToken/UpdateUserAuthenticationTokenDto.cs (delete)
  - src/Explore.Application/DTOs/UserAuthenticationToken/Validators/CreateUserAuthenticationTokenDtoValidator.cs (delete)
  - src/Explore.Application/DTOs/UserAuthenticationToken/Validators/UpdateUserAuthenticationTokenDtoValidator.cs (delete)
  - src/Explore.Application/Features/UserAuthenticationTokens/Requests/Commands/CreateUserAuthenticationTokenCommand.cs (delete)
  - src/Explore.Application/Features/UserAuthenticationTokens/Requests/Commands/UpdateUserAuthenticationTokenCommand.cs (delete)
  - src/Explore.Application/Features/UserAuthenticationTokens/Handlers/Commands/CreateUserAuthenticationTokenCommandHandler.cs (delete)
  - src/Explore.Application/Features/UserAuthenticationTokens/Handlers/Commands/UpdateUserAuthenticationTokenCommandHandler.cs (delete)
  - tests/Event.Application.UnitTests/Features/UserAuthenticationTokens/Commands/UserAuthenticationTokenCommandHandlerTests.cs (modify)
  - tests/Event.Application.UnitTests/Features/UserAuthenticationTokens/Queries/UserAuthenticationTokenDtoPrivacyTests.cs (modify)
  - src/Explore.Application/DTOs/AtprotoRecord/CreateAtprotoRecordDto.cs and UpdateAtprotoRecordDto.cs (delete)
  - src/Explore.Application/Features/AtprotoRecords/Requests/Commands and Handlers/Commands (delete direct mutation types)
  - tests/Event.API.IntegrationTests/Features/AtprotoRecordControllerTests.cs (replace direct-write coverage with read-only/route-absence coverage)
- **Description:** Delete generic credential create/update contracts and controller actions. Delete public direct `AtprotoRecord` create/update/delete actions, DTOs, commands, handlers, serializer roots, generated methods, and mutation HAL links; retain only governed read discovery where required. Keep self-scoped token metadata list/detail and route deletion through the ATProto revoke use case when Provider=atproto. Do not add obsolete aliases or replacement public mutation DTOs.
- **Acceptance Criteria:**
  - [ ] OpenAPI has no generic raw-token create/update operation.
  - [ ] Safe DTOs expose only ID, provider, PDS host, and expiry.
  - [ ] Delete/revoke remains authorized, self/tenant scoped, and idempotent.
  - [ ] No compatibility route, command, DTO, mapper, serializer entry, or test remains.
  - [ ] Static/OpenAPI/generated-client checks prove no caller can directly mutate `AtprotoRecord`; lifecycle outboxes and canonical Jetstream ingestion are the only write authorities.
- **Dependencies:** Phase 5.
- **Effort:** M
- **Required Skills/Rules:** cqrs-mediatr-guidelines, api-controllers.md, no-backward-compatibility instruction.

#### Task 6.2: Regenerate clients and align safe account-session UX/docs

- **Type:** modify / create
- **Layer:** Blazor Client / API Docs
- **Files:**
  - src/Explore.Blazor.Client/Clients/EventApiClient.g.cs (existing, generated)
  - src/Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs (existing)
  - src/Explore.Blazor.Client/Pages/LoginRedirect.razor (existing)
  - tests/Explore.Blazor.Client.Tests/Pages/LoginRedirectAtprotoTests.cs (new)
  - tests/Explore.Blazor.Client.Tests/Security/AtprotoCredentialIsolationTests.cs (new)
  - docs/API_CHANGELOG.md (existing)
  - docs/FEDERATION.md (existing)
  - docs/AUTHORIZATION.md (existing)
- **Description:** Regenerate the client after the OpenAPI change, remove serializer roots for deleted secret DTOs, keep handle input accessible, and map only stable BFF error codes to user guidance. If an account-session revoke affordance is rendered, gate it from the returned HAL relation rather than claims. Update docs to describe completed OAuth Part A and explicitly retain Part B as not implemented.
- **Acceptance Criteria:**
  - [ ] Generated client/JSON context contains no deleted credential types or bridge session material.
  - [ ] The server-private bridge operation/models and removed direct `AtprotoRecord` mutations are absent from the generated browser client and serializer context.
  - [ ] Login handle label, validation, focus, keyboard submission, and error announcement remain accessible.
  - [ ] UI never gates per-resource actions from roles/claims.
  - [ ] API_CHANGELOG records removed endpoints and new bridge/refresh/revoke operations.
  - [ ] FEDERATION distinguishes implemented OAuth authentication from the still-pending event/RSVP phases in this workstream.
- **Dependencies:** 6.1.
- **Effort:** M
- **Required Skills/Rules:** blazor-ui-conventions, blazor-client.md, api-hateoas.md, openapi-contract-change intent.

### Phase 7: ATProto Events Governance And Validation Profiles

- **Goal:** Introduce the single effective capability, administrator locks, User-tier publication consent, and the platform/community validation-profile resolver before any federation side effect is wired.
- **Depends on:** Phase 6.
- **Related skills/rules:** cqrs-mediatr-guidelines, clean-architecture-rules, application-layer.md, domain-layer.md.
- **Acceptance criteria:**
  - One effective setting enables/disables both fetch and outbound capability.
  - Instance administrators can lock both capability and validation profile; unlocked tenant administrators can override only their tenant.
  - User publication consent remains independent and cannot be granted by an administrator on the user's behalf.
  - Platform mode preserves current readiness; community mode removes schedule/format/visibility as user-required publication data while retaining internal and supplied-value invariants.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** Defaults remain disabled, platform, and instance-locked. Unknown/corrupt profile values resolve fail-closed to platform and emit bounded diagnostics.

#### Task 7.1: Add the ATProto Events capability, locks, and user consent

- **Type:** create / modify
- **Layer:** Domain / Application / Persistence / API
- **Files:**
  - src/Explore.Domain/Constants/GovernanceSettingKeys.cs (existing)
  - src/Explore.Domain/Settings/Definitions/AtprotoFederationSettingDefinitions.cs (new)
  - src/Explore.Application/Settings/Groups/AtprotoFederationSettingGroup.cs (new)
  - src/Explore.Application/Features/Settings/Requests and Handlers for instance defaults, tenant override, and user publish consent (existing)
  - src/Explore.Persistence/Seed/SeedIds.cs (existing)
  - src/Explore.Persistence/Seed/LookupTableSeeder.cs (existing)
  - src/Explore.API/Controllers/SettingsController.cs (existing)
  - src/Explore.API/Hateoas/Policies/InstanceSettingGroupLinkPolicy.cs (new; ATProto allowlisted policy)
  - src/Explore.API/Hateoas/Assemblers/InstanceSettingGroupResourceAssembler.cs (new; ATProto allowlisted assembler)
  - tests/Event.Application.UnitTests/Settings/AtprotoFederationGovernanceTests.cs (new)
  - tests/Event.API.IntegrationTests/Features/InstanceSettingGroupApiTests.cs (new)
- **Description:** Define exactly `federation.atproto_events_enabled`, `federation.atproto_event_validation_profile`, and User-tier `federation.atproto_publish_my_events`. Mark capability/profile `IsLockable` and reuse the existing lock/unlock commands, persisted lock state, five-tier resolver, and `CanEdit/Reason` metadata; do not create parallel `lock_tenant_*` setting keys. Seed disabled/platform values with instance lock state. Expose instance defaults and locks through the generic instance-scope settings API and authorization-aware HAL metadata; keep personal publication consent on the existing User-scope surface. Replace, rather than preserve, the report summary's split administrator fetch/publish keys.
- **Acceptance Criteria:**
  - [x] Effective false gates both fetch and new outbound enqueue; there is no independent administrator fetch/publish toggle.
  - [x] Instance lock state makes tenant writes reject safely and is reflected in effective-setting edit metadata.
  - [x] Exactly three new setting definitions exist; capability/profile locking is performed by the existing lock engine rather than duplicate lock settings.
  - [x] Validation profile accepts only `platform` and `community_lexicon`; unknown values fail closed to platform.
  - [x] User consent defaults false, is self-scoped, auditable, revocable, and cannot be changed by tenant/instance administrators.
  - [x] Instance administrators can read, update, lock, and unlock capability/profile through the ATProto instance-setting routes; every other registry key is rejected before command dispatch, and clients gate allowed actions from HAL links.
  - [x] `auth.atproto_login_enabled` does not implicitly enable/disable event federation.
- **Dependencies:** Phase 6.
- **Effort:** L
- **Required Skills/Rules:** cqrs-mediatr-guidelines, clean-architecture-rules, application-layer.md, domain-layer.md.

#### Task 7.2: Make create/publish readiness profile-aware

- **Type:** create / modify
- **Layer:** Application
- **Files:**
  - src/Explore.Application/Services/Lifecycle/ValidationProfile.cs (existing)
  - src/Explore.Application/Services/Lifecycle/EventLifecyclePolicyProvider.cs (existing)
  - src/Explore.Application/Services/Lifecycle/EventLifecycleReadinessEvaluator.cs (existing)
  - src/Explore.Application/Features/Federation/Atproto/Services/AtprotoEventGovernanceResolver.cs (new)
  - src/Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs (existing)
  - src/Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs (existing)
  - tests/Event.Application.UnitTests/Services/Lifecycle/AtprotoEventValidationProfileTests.cs (new)
- **Description:** Resolve the effective capability/profile once per command and manually apply community readiness to direct create-as-published and later PublishEvent only while ATProto Events is enabled; disabled or unknown capability uses platform readiness. Keep `CreateEventRequestValidator` for shape, length, reference, schedule-consistency, and supplied optional values. Add a community publication profile whose only community-required data is Title/name and server-generated CreatedAt, layered with tenant/owner/status/authorization/concurrency/storage invariants that no administrator can loosen.
- **Acceptance Criteria:**
  - [x] Platform profile retains the current scheduled-session/first-start/visibility/format publication behavior.
  - [x] Community profile accepts a title-only public event using server defaults and generated CreatedAt; it does not require sessions, startsAt, endsAt, event type, or audience lookups.
  - [x] Disabled or unresolved ATProto Events capability cannot activate community readiness and falls back to platform publication requirements.
  - [x] Invalid optional sessions, references, prices, time ranges, enum values, or cross-tenant IDs still fail in either profile.
  - [x] Cancelled/archived/moderated/unauthorized/cross-tenant/concurrency-conflicted events cannot use the relaxed profile.
  - [x] Tests prove current EF non-null/check constraints can persist the community-minimum event without a schema-relaxation migration.
- **Dependencies:** 7.1.
- **Effort:** L
- **Required Skills/Rules:** cqrs-mediatr-guidelines, application-layer.md, tests.md.

### Phase 8: Canonical Publication Snapshot And Exhaustive Description

- **Goal:** Build one complete public event snapshot and a generated community record whose native fields plus description account for every federatable value.
- **Depends on:** Phase 7.
- **Related skills/rules:** clean-architecture-rules, cqrs-mediatr-guidelines, update-repository-query intent, application-layer.md, efcore-persistence.md.
- **Acceptance criteria:**
  - The repository returns a tenant-filtered entity graph; Application owns the snapshot mapping.
  - All event sessions and all non-native public graph values appear in the one description.
  - Lookup IDs become display names/codes, EAVs become labeled typed display values, and times/currency/booleans/URLs are readable and unambiguous.
  - Coverage gaps, unsafe content, and oversized records prevent PDS enqueue without truncation.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** This phase has no remote side effects. A projection error returns a typed permanent no-PDS result and leaves local publication behavior unchanged.

#### Task 8.1: Load and build the canonical public/federatable event snapshot

- **Type:** create / modify
- **Layer:** Application / Persistence
- **Files:**
  - src/Explore.Application/Contracts/Persistence/IEventRepository.cs (existing)
  - src/Explore.Persistence/Repositories/EventRepository.cs (existing)
  - src/Explore.Application/Features/Federation/Atproto/Models/AtprotoEventPublicationSnapshot.cs (new)
  - src/Explore.Application/Features/Federation/Atproto/Services/AtprotoEventPublicationSnapshotFactory.cs (new)
  - src/Explore.Application/Services/EventLocationDisclosureEvaluator.cs (existing; reuse)
  - tests/Event.Application.UnitTests/Features/Federation/Atproto/AtprotoEventPublicationSnapshotFactoryTests.cs (new)
- **Description:** Add a no-tracking read for delivery/retry and a tracked in-transaction path where required, both returning the Event entity graph. Include every publicly federatable scalar/navigation: descriptions/content/subtitle, status/format/visibility/type/audience/madhab, price/currency/registration, actor/organization/group display data, series, categories/tags, event locations/rooms, days, agenda, every session with status/kind/schedule/registration/capacity/pricing/languages/speakers/location/room, Islamic/tech/session aspects, and event/session custom-property definitions/options/values. Evaluate event/session locations through `EventLocationDisclosureEvaluator` with `EventLocationDisclosurePurpose.Public`, then map entities to an immutable Application snapshot; never return a DTO from the repository or serialize the EF graph.
- **Acceptance Criteria:**
  - [ ] Snapshot tests include multiple days, rooms, agenda items, every session, lookup display values, both aspect families, and event/session EAV values.
  - [ ] Soft-deleted/private/internal-only data is excluded by explicit policy, not accidental missing Includes.
  - [ ] Every included event/session location is the `EventLocationDisclosurePurpose.Public` result; private-home/delayed/erased address canaries never enter the snapshot.
  - [ ] No moderation/report evidence, attendee identity/answers, audit/concurrency fields, tenant/user IDs, secrets, or private storage metadata enters the snapshot.
  - [ ] Lookup values have human-facing name/code fallbacks; no description section exposes a raw lookup ID as its only representation.
  - [ ] Query count/shape is bounded and avoids N+1 loading.
- **Dependencies:** 7.2.
- **Effort:** XL
- **Required Skills/Rules:** dotnet-efcore-guidelines, optimizing-ef-core-queries, clean-architecture-rules.

#### Task 8.2: Map the community record and render every additional field

- **Type:** create / modify
- **Layer:** Application / Infrastructure
- **Files:**
  - src/Explore.Infrastructure/Explore.Infrastructure.csproj (existing)
  - schemas/lexicons/lexicon-community-calendar-events.json (existing)
  - schemas/lexicons/lexicon-community-calendar-rsvp.json (existing)
  - src/Explore.Application/Features/Federation/Atproto/Models/AtprotoCalendarEventRecordData.cs (new)
  - src/Explore.Application/Features/Federation/Atproto/Models/AtprotoCalendarRsvpRecordData.cs (new)
  - src/Explore.Application/Features/Federation/Atproto/Services/AtprotoCalendarEventRecordMapper.cs (new)
  - src/Explore.Application/Features/Federation/Atproto/Services/AtprotoCalendarRsvpRecordMapper.cs (new)
  - src/Explore.Application/Features/Federation/Atproto/Services/AtprotoEventDescriptionFormatter.cs (new)
  - src/Explore.Application/Features/Federation/Atproto/Validators/AtprotoCalendarEventRecordValidator.cs (new)
  - src/Explore.Application/Features/Federation/Atproto/Validators/AtprotoCalendarRsvpRecordValidator.cs (new)
  - tests/Event.Application.UnitTests/Features/Federation/Atproto/AtprotoEventDescriptionFormatterTests.cs (new)
- **Description:** Generate CarpaNet bindings hermetically from the vendored community lexicons. Map representable event values to native properties. Map only a successfully committed active `EventRegistrationIntent`/registration lifecycle to a typed `community.lexicon.calendar.rsvp#going` record with event strongRef inputs; never translate organizer `ApprovalStatus`, and do not emit `interested` or `notgoing` without a future explicit local user-intent model. Build a deterministic plain-text/Markdown event description with stable sections and ordering for every other snapshot value, including every session in the same record. Strip/convert markup safely, use the tenant/public culture with invariant ISO timestamps as an unambiguous fallback, and include display labels for EAV type/value and lookup name/code. Maintain independently authored event and RSVP source-field manifests rather than deriving them from mapper output; tests compare them to their source contracts. Verify the encoded record limit for the pinned stack and never truncate.
- **Acceptance Criteria:**
  - [ ] Native mapping covers name, base description, createdAt, available aggregate startsAt/endsAt, mode, status, locations, uris, and rsvpExpected.
  - [ ] The final description contains base description/content plus sections for every non-native scalar, all days/agenda, all sessions and their full public metadata, actors/groups/organizations, registration/pricing, categories/tags, locations/rooms, all aspects, and all event/session EAV values.
  - [ ] Stable ordering makes the same snapshot byte-for-byte deterministic and suitable for payload hashing/idempotency.
  - [ ] Adding an uncovered snapshot field fails a coverage test; null/empty values follow an explicit documented omission rule and are never confused with an unmapped field.
  - [ ] RSVP projection maps a successfully committed active `EventRegistrationIntent`/registration lifecycle only to `#going` plus the settled event URI/CID strongRef, has its own source-field manifest/validator, and excludes organizer `ApprovalStatus`, attendee answers, and identity/private registration data. Cancellation/deletion plans a remote delete; `interested`/`notgoing` are not emitted.
  - [ ] Unsafe/private values never enter output; invalid lexicon shape or encoded-size overflow returns permanent no-PDS and never a shortened record.
- **Dependencies:** 8.1.
- **Effort:** XL
- **Required Skills/Rules:** cqrs-mediatr-guidelines, clean-architecture-rules, error-tracking.

### Phase 9: Transactional Outbound Event And RSVP Publication

- **Goal:** Guarantee local commit before remote visibility, make delivery idempotent/recoverable, and settle event URI/CID before dependent RSVP records.
- **Depends on:** Phase 8 and the encrypted session work from Phase 2.
- **Related skills/rules:** outbox-pattern, dotnet-efcore-guidelines, cqrs-mediatr-guidelines, efcore-migrations.md.
- **Acceptance criteria:**
  - No PDS network call occurs in a request transaction.
  - A claimable PDS create row and a locally published Event always commit or roll back together.
  - Stable record identity plus transactional settlement make crash retry duplicate-safe.
  - Event URI/CID exists before any RSVP strongRef is enqueued or delivered.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** Disabling the capability stops new enqueue and worker claims but does not roll back local events. Pending rows remain auditable; permanent errors dead-letter with safe reasons.

#### Task 9.1: Record ADR-015 and harden federation persistence

- **Type:** create / modify
- **Layer:** Domain / Persistence / Docs
- **Files:**
  - docs/adr/ADR-015-atproto-event-record-and-outbox.md (new)
  - src/Explore.Domain/AtprotoRecord.cs (existing)
  - src/Explore.Domain/Federation/PdsSyncOutbox.cs (existing)
  - src/Explore.Persistence/Configurations/Entities/AtprotoRecordConfiguration.cs (existing)
  - src/Explore.Persistence/Configurations/Entities/Federation/PdsSyncOutboxConfiguration.cs (existing)
  - src/Explore.Application/Contracts/Persistence/IPdsSyncOutboxRepository.cs (existing)
  - src/Explore.Persistence/Repositories/PdsSyncOutboxRepository.cs (existing)
  - src/Explore.Persistence/Migrations/<generated>_HardenAtprotoEventFederation.cs (new)
  - src/Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs (existing, generated)
  - schemas/islamu-event.md (existing)
- **Description:** Define tenant/user ownership for outbound records, global canonical ownership for inbound DID/collection/rkey versions, separate tenant presentation/visibility joins, direction/provenance, source event/registration/version correlation, immutable payload/hash, stable idempotency key, expected/current CID, URI/CID settlement, failure classification, timestamps, one leased consumer/cursor, and recoverable claim leases. Preserve Event/EventRegistration FKs and resolve them explicitly in ADR-015. Use UUIDv7 identities and long cursors. Remove obsolete fields/semantics instead of compatibility shims.
- **Acceptance Criteria:**
  - [ ] Unique constraints prevent duplicate remote identity and duplicate logical outbox operation while allowing sequential aggregate versions.
  - [ ] Processing claims have owner/expiry and crashed leases are reclaimable without double completion.
  - [ ] Completion settles AtprotoRecord URI/CID and outbox status in one transaction; MarkAsCompleted no longer discards results.
  - [ ] Payload, error, and identifier lengths are bounded; query filters and explicit tenant predicates prevent cross-tenant access.
  - [ ] ADR-015 records DB-first ordering, echo prevention, cursor/checkpoint policy, user consent, and event-before-RSVP dependency.
  - [ ] ADR-015 records one global canonical inbound record per DID/collection/rkey version, tenant visibility/presentation separately, and no per-tenant Jetstream consumer.
- **Dependencies:** Phase 8.
- **Effort:** XL
- **Required Skills/Rules:** outbox-pattern, dotnet-efcore-guidelines, efcore-persistence.md, efcore-migrations.md.

#### Task 9.2: Enqueue event publication only from successful local lifecycle transitions

- **Type:** create / modify
- **Layer:** Application
- **Files:**
  - src/Explore.Application/Features/Federation/Atproto/Services/AtprotoEventPublicationPlanner.cs (new)
  - src/Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs (existing)
  - src/Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs (existing)
  - src/Explore.Application/Features/Events/Handlers/Commands/UpdateEventCommandHandler.cs (existing)
  - src/Explore.Application/Features/Events/Handlers/Commands/CancelEventCommandHandler.cs (existing)
  - src/Explore.Application/Features/Events/Handlers/Commands/DeleteEventCommandHandler.cs (existing)
  - src/Explore.Application/Features/Events/Handlers/Commands/HeavyRedactEventCommandHandler.cs (existing)
  - tests/Event.Application.UnitTests/Features/Federation/Atproto/AtprotoEventLifecycleOutboxTests.cs (new)
- **Description:** After primary lifecycle validation and inside the same `IUnitOfWork`, apply capability/consent/session guards, create the full snapshot, map and manually validate the record, then add an immutable outbox row. Direct draft creation never enqueues. Create-as-published and PublishEvent can create the initial remote record; later handlers may update/delete only an already-linked outbound AtprotoRecord. Lexicon/projection failure skips federation, records a bounded warning/status, and does not undo otherwise-valid local publication.
- **Acceptance Criteria:**
  - [ ] Local readiness failure leaves status unchanged and creates no PDS outbox row.
  - [ ] Database rollback removes both local changes and the PDS outbox row; tests prove no claimable orphan exists.
  - [ ] Capability off, consent off, unlinked DID, missing session, incomplete coverage, invalid lexicon, or oversized payload creates no PDS row and performs no network call.
  - [ ] Successful local publication and create outbox persist atomically; the preallocated rkey/idempotency key is stable across execution-strategy retry.
  - [ ] Update/cancel/delete/redact never synthesize a remote create when AtprotoRecord is absent.
- **Dependencies:** 9.1.
- **Effort:** XL
- **Required Skills/Rules:** outbox-pattern, cqrs-mediatr-guidelines, application-layer.md.

#### Task 9.3: Deliver event records, settle URI/CID, then publish RSVP strongRefs

- **Type:** create / modify
- **Layer:** Application / Infrastructure / API
- **Files:**
  - src/Explore.Application/Contracts/Infrastructure/IAtprotoPdsDeliveryGateway.cs (new)
  - src/Explore.Application/Features/Federation/Atproto/Services/AtprotoPdsDeliveryProcessor.cs (new)
  - src/Explore.Infrastructure/Services/Federation/AtprotoPdsDeliveryGateway.cs (new)
  - src/Explore.API/BackgroundServices/PdsSyncWorker.cs (new)
  - src/Explore.API/BackgroundServices/PdsSyncWorkerOptions.cs (new)
  - src/Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs (existing)
  - src/Explore.Application/Features/EventRegistrations/Handlers/Commands/DeleteEventRegistrationCommandHandler.cs (existing)
  - src/Explore.Application/Features/EventRegistrations/Handlers/Commands/UpdateEventRegistrationCommandHandler.cs (existing; intentionally no RSVP publication call)
  - focused Application/Infrastructure delivery, RSVP, and enabled-handler atomicity tests (new/modified)
- **Description:** Replace unauthenticated raw HTTP with the repository-backed CarpaNet OAuth session restored by user DID. Immediately before each remote call, re-resolve the effective capability, current self-consent, and `EventLocationDisclosurePurpose.Public`; release/dead-letter the claim without remote I/O when any gate no longer permits delivery. Write the event at the stable record key with retry reconciliation and optional swap/CID protection supported by the pinned binding; classify 429/5xx/timeouts as retryable and auth/validation/ownership errors as permanent/reauth-required. Settle URI/CID transactionally. A committed active registration enqueues typed `#going` only after the event AtprotoRecord has URI/CID; user cancellation/deletion enqueues delete for the existing remote RSVP. Organizer approval changes never synthesize RSVP intent.
- **Acceptance Criteria:**
  - [ ] Worker cannot process an uncommitted/disabled/cross-tenant row and restores only the row owner's session.
  - [ ] Crash after remote success but before local settlement retries/reconciles the same rkey without a duplicate event.
  - [ ] Event completion stores URI/CID before dependent RSVP is claimable; missing CID defers rather than fabricates a strongRef.
  - [ ] Active registration emits only `#going`; organizer `ApprovalStatus` changes emit no RSVP intent; user cancellation/deletion deletes the existing remote RSVP; `interested`/`notgoing` remain unsupported.
  - [ ] Remote failure never deletes/rolls back the application event; retry/dead-letter status is queryable without secret/provider-body leakage.
  - [ ] Disabling or revoking user consent stops unclaimed new delivery safely; already-completed remote records are changed only by an explicit authorized lifecycle/consent policy.
  - [ ] Capability, current self-consent, and public-location disclosure are rechecked after claim and immediately before remote I/O; stale claimed work cannot bypass revocation.
- **Dependencies:** 9.2 and Phase 5 refresh/revocation.
- **Effort:** XL
- **Required Skills/Rules:** outbox-pattern, auth-patterns, error-tracking.

### Phase 10: Filtered Inbound Jetstream Federation

- **Goal:** Fetch only community event/RSVP records when the effective capability is enabled and maintain an idempotent moderated inbound read model with tombstone handling.
- **Depends on:** Phase 9.
- **Related skills/rules:** clean-architecture-rules, error-tracking, dotnet-efcore-guidelines.
- **Acceptance criteria:**
  - CarpaNet.Jetstream uses exactly two WantedCollections and a durable long cursor.
  - Capability disablement stops subscription/ingestion for the effective scope.
  - Allowlist, lexicon validation, provenance, de-duplication, echo prevention, and tombstone behavior are explicit.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** Stop the subscriber and retain the last safe cursor/read model for diagnosis. Never widen collections or skip validation to recover throughput.

#### Task 10.1: Implement capability-aware Jetstream ingestion and tombstones

- **Type:** create / modify
- **Layer:** Infrastructure / Persistence / API host
- **Files:**
  - Directory.Packages.props (existing)
  - src/Explore.Infrastructure/Explore.Infrastructure.csproj (existing)
  - src/Explore.Infrastructure/Services/Federation/AtprotoJetstreamSubscriber.cs (new)
  - src/Explore.Application/Contracts/Persistence/IAtprotoRecordRepository.cs (existing)
  - src/Explore.Persistence/Repositories/AtprotoRecordRepository.cs (existing)
  - src/Explore.API/Program.cs (existing)
  - tests/Explore.Infrastructure.Tests/Federation/AtprotoJetstreamSubscriberTests.cs (new)
- **Description:** Pin CarpaNet.Jetstream consistently and run one leased multi-node consumer subscribed only to event/RSVP collections. Persist/reuse the long microsecond cursor and dispatch bounded commit envelopes. Upsert validated inbound versions idempotently as global canonical records by DID/collection/rkey, retain source URI/CID/provenance, and maintain tenant presentation/visibility separately; detect locally-owned records and process deletes as tombstone purge/suppression including dependent RSVPs. Re-evaluate whether any effective scope needs the shared consumer without opening a per-event unbounded settings query.
- **Acceptance Criteria:**
  - [ ] WantedCollections contains exactly the two vendored NSIDs; no wildcard or unrelated collection is accepted.
  - [ ] Disabled capability causes no new inbound materialization; enabling starts/resumes from the durable cursor.
  - [ ] Malformed, oversized, unallowlisted, wrong-type, or unsupported records are quarantined/ignored with bounded metrics and do not advance state contrary to ADR-015.
  - [ ] Duplicate/replayed commits are idempotent, local outbound records do not appear twice, and deletes remove/suppress dependent inbound state.
  - [ ] A multi-node lease permits one canonical consumer, tenants never open their own sockets or own duplicate inbound rows, and presentation joins control tenant visibility.
  - [ ] Reconnect/backoff/cancellation are bounded and contain no DID/record payload high-cardinality labels.
- **Dependencies:** 9.1, 9.3.
- **Effort:** XL
- **Required Skills/Rules:** error-tracking, dotnet-efcore-guidelines, external-infrastructure-bootstrap intent.

### Phase 11: Tenant-Gated Federation API And HAL

- **Goal:** Expose allowed inbound/local federation state through existing home/event-list APIs with tenant gating, provenance, and authoritative HAL affordances.
- **Depends on:** Phase 10.
- **Related skills/rules:** api-controllers.md, api-hateoas.md, cqrs-mediatr-guidelines, add-get-endpoint intent.
- **Acceptance criteria:**
  - Disabled tenants receive no inbound ATProto events.
  - Local and federated entries are distinguishable and de-duplicated.
  - API owns authorization and action links; browser code never infers actions from claims or source type.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** Remove federated embedded items/links while preserving local event responses and route compatibility within the still-development contract.

#### Task 11.1: Extend home/event-list queries and HAL for federated events

- **Type:** create / modify
- **Layer:** Application / API
- **Files:**
  - src/Explore.Application/Features/PublicExperience/Handlers/Queries/GetHomeDiscoveryQueryHandler.cs (existing)
  - src/Explore.Application/DTOs/PublicExperience/HomeDiscoveryDto.cs (existing)
  - src/Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs (existing)
  - src/Explore.API/Controllers/PublicExperienceController.cs (existing)
  - src/Explore.API/Controllers/EventController.cs (existing)
  - src/Explore.API/Hateoas/Policies/EventLinkPolicy.cs (existing)
  - src/Explore.API/Hateoas/RouteNames.cs (existing)
  - tests/Event.API.IntegrationTests/Features/AtprotoFederatedEventPresentationTests.cs (new)
- **Description:** Merge allowed inbound projections into home and event-list results only after effective tenant capability checks. De-duplicate against local outbound URI/CID, expose bounded provenance/source metadata and safe external links, and add only policy-authorized HAL actions such as view-source or RSVP/sync where applicable. Keep GET anonymous and all writes authorized.
- **Acceptance Criteria:**
  - [ ] Disabled/locked-off tenant results contain no inbound ATProto items or federation action links.
  - [ ] Enabled results include only allowlisted, valid, non-tombstoned records and preserve stable pagination/sort semantics.
  - [ ] A locally-owned event observed through Jetstream appears once as the local event with federation metadata.
  - [ ] HAL relations, not DTO booleans/roles/claims, are the sole mutation/source-action authority.
  - [ ] OpenAPI classifications, response metadata, cache tags, rate limits, ProblemDetails, and route names remain explicit.
- **Dependencies:** Phase 10.
- **Effort:** L
- **Required Skills/Rules:** api-hateoas.md, api-controllers.md, cqrs-mediatr-guidelines.

### Phase 12: Administrator, User-Consent, And Event Client Surfaces

- **Goal:** Let authorized administrators govern capability/profile/locks, let users control PDS publication consent, and render federated events safely from API/HAL contracts.
- **Depends on:** Phase 11.
- **Related skills/rules:** blazor-ui-conventions, blazor-client.md, design-system, accessibility docs.
- **Acceptance criteria:**
  - Instance and tenant controls explain that one switch activates both fetch and publication capability.
  - Locked controls are disabled with the server-provided reason; community mode explains its reduced required fields without implying reduced security.
  - Users can grant/revoke their own publication consent and see federation delivery/failure state without credentials.
  - Home/list cards distinguish federated provenance and use HAL for every action.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** Hide unavailable federation controls/cards from server capability/HAL state; do not add client-side policy fallbacks.

#### Task 12.1: Add instance, tenant, and user federation controls

- **Type:** create / modify
- **Layer:** Blazor Client / generated contracts
- **Files:**
  - src/Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceGovernanceSection.razor (existing)
  - src/Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantPoliciesSection.razor (existing)
  - src/Explore.Blazor.Client/Pages/User/UserProfile.razor and UserProfile.razor.cs (existing)
  - src/Explore.Blazor.Client/Contracts/Services/Federation/IAtprotoFederationSettingsService.cs (new)
  - src/Explore.Blazor.Client/Services/AtprotoFederationSettingsService.cs (new)
  - src/Explore.Blazor.Client/Clients/EventApiClient.g.cs (existing, generated)
  - src/Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs (existing)
  - tests/Explore.Blazor.Client.Tests/Pages/Admin/InstanceGovernanceSectionTests.cs (existing)
  - tests/Explore.Blazor.Client.Tests/Pages/Admin/TenantPoliciesSectionTests.cs (existing)
  - tests/Explore.Blazor.Client.Tests/Pages/User/UserProfileTests.cs (existing)
- **Description:** Render the master capability, two instance lock controls, platform/community profile selector, effective-source/reason metadata, and self-scoped publication consent. Regenerate contracts from OpenAPI. Explain that enabling makes both fetching and eligible publication possible, but a PDS write still requires consent, a linked session, successful local publication, and complete projection.
- **Acceptance Criteria:**
  - [ ] Instance admin can set defaults/locks; tenant admin can edit only when API metadata permits; user can change only personal consent.
  - [ ] Locked/unavailable controls expose an accessible explanation and cannot be bypassed by forged client state.
  - [ ] Community profile copy states that only lexicon-required business fields are required while security, ownership, tenant, reference, and supplied-value validation remain enforced.
  - [ ] No token, DPoP key, full DID in telemetry, or private record payload enters generated/browser state.
  - [ ] Labels, descriptions, keyboard interaction, focus, validation summary, and error announcement follow repository accessibility conventions.
- **Dependencies:** 7.1, 7.2, 11.1.
- **Effort:** L
- **Required Skills/Rules:** blazor-ui-conventions, blazor-client.md, accessibility docs.

#### Task 12.2: Render federated events and delivery status from HAL

- **Type:** create / modify
- **Layer:** Blazor Client
- **Files:**
  - src/Explore.Application/DTOs/Event/EventListDto.cs (existing)
  - src/Explore.Application/Features/Events/Handlers/Queries/GetMyEventsRequestHandler.cs (existing)
  - src/Explore.Application/Contracts/Persistence/IPdsSyncOutboxRepository.cs (existing)
  - src/Explore.Persistence/Repositories/PdsSyncOutboxRepository.cs (existing)
  - src/Explore.Blazor.Client/Components/Discovery/HomeDiscoveryExperience.razor (existing)
  - src/Explore.Blazor.Client/Components/Discovery/UpcomingEventList.razor (existing)
  - src/Explore.Blazor.Client/Pages/Events/Components/EventCard.razor and EventCard.razor.cs (existing)
  - tests/Event.Application.UnitTests/Features/Events/Queries/GetMyEventsRequestHandlerTests.cs (new)
  - tests/Event.Persistence.IntegrationTests/Federation/AtprotoFederationPersistenceTests.cs (existing)
  - tests/Explore.Blazor.Client.Tests/Components/Event/EventCardTests.cs (existing)
  - tests/Explore.Blazor.Client.Tests/Components/Discovery/HomeDiscoveryExperienceTests.cs (existing)
  - tests/Explore.Blazor.Client.Tests/Components/Discovery/UpcomingEventListTests.cs (existing)
  - tests/Explore.Blazor.Client.Tests/Services/EventServiceTests.cs (existing)
- **Description:** Display source/provenance, safe external source navigation, and local outbound delivery state using generated DTOs and HAL relations. My Events performs one bounded tenant-scoped repository read that returns only the latest unsuperseded PDS delivery row per local event; successful settlement links the canonical AtprotoRecord back to the already-committed Event. Keep local and inbound event presentation coherent and accessible. Never display raw outbox/provider error bodies or machine codes; map stable failure codes to guidance.
- **Acceptance Criteria:**
  - [ ] Federated source/status is understandable without relying only on color and links have descriptive accessible names.
  - [ ] Source/RSVP/retry/sync actions render only when their HAL relations exist.
  - [ ] No role/claim/source-type inference gates actions.
  - [ ] Disabled tenant results render no stale federated cards after refresh/cache invalidation.
  - [ ] Local events remain unchanged when no federation metadata is present.
- **Dependencies:** 11.1, 12.1.
- **Effort:** M
- **Required Skills/Rules:** blazor-ui-conventions, blazor-client.md, api-hateoas.md.

### Phase 13: Inbound Event Recovery and Backfill Configuration

- **Goal:** Implement governed downtime recovery, in-place Jetstream filter updates, bounded current PDS snapshot reconciliation, and atomic encrypted OAuth refresh persistence.
- **Depends on:** Phase 12.
- **Progress:** Implementation complete and independently verified in Todos 16-19. The Phase 13 broad build/project gate remains part of Todo 22.
- **Related skills/rules:** clean-architecture-rules, cqrs-mediatr-guidelines, dotnet-efcore-guidelines.
- **Acceptance criteria:**
  - Tenant admin can toggle backfill enabled and select between downtime-only and full modes.
  - Ingestion gap recovery dynamically requests missing records from PDS repositories without starting a socket per tenant.
  - Jetstream dynamically updates allowed DIDs and collections without restarting the background service.
  - Token refresh automatically propagates back to encrypted database sessions.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** Disable backfill settings and fall back to the live-only Jetstream cursor.

#### Task 13.1: Add Tenant Settings and Rules for Backfilling

- **Status:** Complete; independently verified in `.omo/evidence/atproto-auth/task-16/`.
- **Type:** create / modify
- **Layer:** Domain / Application / API / Blazor Client
- **Files:**
  - src/Explore.Domain/Constants/GovernanceSettingKeys.cs (existing)
  - src/Explore.Domain/Settings/Definitions/AtprotoFederationSettingDefinitions.cs (existing)
  - src/Explore.Application/Settings/Groups/AtprotoFederationSettingGroup.cs (existing)
- **Description:** Introduce `federation.atproto_events_backfill_enabled` (Boolean) and `federation.atproto_events_backfill_mode` (Enum: platform-defined platform-neutral codes for `DowntimeOnly` and `Full`) into the setting registry. Ensure they are lockable at the instance tier and cascade through the tenant settings stack. Seed them as disabled/downtime-only by default and instance-locked.
- **Acceptance Criteria:**
  - [x] Both settings can be read, overridden, locked, and unlocked via standard settings API and UI.
  - [x] Standard five-tier settings resolution cascade applies.
- **Dependencies:** Phase 12.
- **Effort:** L
- **Required Skills/Rules:** cqrs-mediatr-guidelines, domain-layer.md.

#### Task 13.2: Implement Jetstream Dynamic Filter Updates

- **Status:** Complete; independently verified in `.omo/evidence/atproto-auth/task-17/`.
- **Type:** modify
- **Layer:** Infrastructure
- **Files:**
  - src/Explore.Infrastructure/Services/Federation/AtprotoJetstreamSubscriber.cs (existing)
- **Description:** Implement dynamic subscription option updates in the leased Jetstream worker. When allowed DIDs or collections are updated in the configuration or tenant parameters, invoke `client.SendOptionsUpdateAsync` with the updated list of `WantedDids` or `WantedCollections` rather than severing and restarting the WebSocket connection.
- **Acceptance Criteria:**
  - [x] Dynamic updates occur on the owned connection without restarting the background service task.
  - [x] Capacity-one coalescing, normalized comparisons, and the existing poll interval prevent update floods; failure reconnects from the unchanged durable cursor with the latest desired filter.
- **Dependencies:** 13.1.
- **Effort:** M
- **Required Skills/Rules:** error-tracking.

#### Task 13.3: Implement Inbound Event Backfill Engine

- **Status:** Complete; independently verified against real PostgreSQL in `.omo/evidence/atproto-auth/task-18/pds-recovery.md`.
- **Type:** create
- **Layer:** Application / Infrastructure / Persistence
- **Files:**
  - src/Explore.Application/Features/Federation/Atproto/Requests/Commands/ReconcileAtprotoPdsSnapshotsCommand.cs (new)
  - src/Explore.Application/Features/Federation/Atproto/Handlers/Commands/ReconcileAtprotoPdsSnapshotsCommandHandler.cs (new)
  - src/Explore.Application/Features/Federation/Atproto/Validators/ReconcileAtprotoPdsSnapshotsCommandValidator.cs (new)
  - src/Explore.Infrastructure/Services/Federation/AtprotoPdsSnapshotGateway.cs (new)
  - src/Explore.Persistence/Repositories/AtprotoJetstreamRepository.cs (existing)
- **Description:** Reconcile bounded current PDS snapshots through the global canonical DID/collection/rkey ingestion and tenant-presentation pipeline. The original read-model-only implementation is complete; Phase 15 now extends its existing fenced transaction with tenant-local Event/EventSession materialization. `DowntimeOnly` resumes the durable Jetstream cursor without PDS I/O. Governed `Full` recovery fetches a bounded `com.atproto.sync.getRepo` CAR for a bounded known-DID set, verifies DID/PDS/repository binding, commit signatures, canonical CBOR/CAR/MST structure, exact event/RSVP collections, and record limits, then atomically reconciles accepted keys. Only a complete successful snapshot may tombstone older absent canonical records; cancellation, invalid data, partial failure, and stale fences preserve prior state.
- **Acceptance Criteria:**
  - [x] Downtime recovery resumes from the saved Unix-microsecond cursor and performs no PDS snapshot I/O.
  - [x] Full recovery is bounded by known DIDs, response/CAR/block/MST/record limits, exact collections, cancellation, and a single leased consumer.
  - [x] Recovered records reuse canonical inbound records and tenant presentations; Phase 15 owns the later Event/EventSession materialization amendment.
  - [x] DID/collection/rkey deduplication, idempotent replay, atomic settlement, and complete-snapshot tombstoning pass real PostgreSQL coverage.
- **Dependencies:** 13.2.
- **Effort:** XL
- **Required Skills/Rules:** clean-architecture-rules, dotnet-efcore-guidelines, cqrs-mediatr-guidelines.

#### Task 13.4: Automate Ingest Token Refresh Hook

- **Status:** Complete; independently verified against real PostgreSQL in `.omo/evidence/atproto-auth/task-19/`.
- **Type:** modify
- **Layer:** Infrastructure / Application
- **Files:**
  - src/Explore.Infrastructure/Services/Federation/AtprotoPdsDeliveryGateway.cs (existing)
  - src/Explore.Infrastructure/Services/Federation/AtprotoOAuthSecurityGateway.cs (existing)
  - src/Explore.Infrastructure/Services/Federation/RepositoryBackedOAuthSessionStore.cs (existing)
- **Description:** Use the existing repository-backed CarpaNet `IOAuthSessionStore` as the sole refresh-durability hook. Every refresh trigger acquires the exact tenant/user/provider/DID PostgreSQL advisory lock, restores the authoritative encrypted envelope, performs the PDS operation and any DPoP refresh, then atomically re-encrypts and stores the complete `OAuthSessionData` before success. The EF concurrency token remains the stale-writer fence; `TokenRefreshed` is not a second persistence path.
- **Acceptance Criteria:**
  - [x] Rotated access/refresh tokens and private DPoP material are stored as one authenticated encrypted envelope before refreshed state is considered usable.
  - [x] Persistence failure is fatal without a PDS write or stale success; concurrent refreshes serialize and reread authoritative durable state.
- **Dependencies:** 13.3.
- **Effort:** L
- **Required Skills/Rules:** auth-patterns.

### Phase 14: Decoupled PDS Identity and Extensibility

- **Goal:** Ensure PDS independence (Bluesky, Eurosky, self-hosted) and maintain clean abstractions to support future Keycloak + local PDS integration without breaking changes.
- **Depends on:** Phase 13.
- **Progress:** Implementation complete and independently verified in Todo 20. The Phase 14 broad build/project gate remains part of Todo 22.
- **Related skills/rules:** clean-architecture-rules, auth-patterns.
- **Acceptance criteria:**
  - Authentication works with accounts in Eurosky, self-hosted, or Bluesky PDS endpoints.
  - Extensible registration boundaries exist so future PDS custodial registration can be added without architectural rewrites.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** Revert to default/Bluesky configurations.

#### Task 14.1: Verify Universal PDS OAuth Compatibility

- **Status:** Complete; independently verified in `.omo/evidence/atproto-auth/task-20/`.
- **Type:** modify / verify
- **Layer:** Blazor / Infrastructure
- **Files:**
  - src/Explore.Blazor/Services/Auth/AtprotoIdentityCache.cs (existing)
  - src/Explore.Blazor/Services/Auth/AtprotoOAuthClientFactory.cs (existing)
  - src/Explore.Blazor/Authentication/AtprotoAuthenticationHandler.cs (existing)
- **Description:** Resolve normalized handles through the hardened bounded identity cache, verify bidirectional handle/DID binding, require exactly one correctly typed HTTPS `#atproto_pds` service, then follow protected-resource and authorization-server metadata to a possibly distinct compliant issuer. Bind callback subject, PDS, issuer, client key, and tenant flow state. Keep future custodial registration at an interface boundary only; add no account creation, provider branch, synthetic email, or Bluesky fallback.
- **Acceptance Criteria:**
  - [x] Authentication challenges discover distinct compliant PDS and authorization-server endpoints for `did:plc` and hostname-only `did:web` identities without provider-specific branches.
  - [x] Handle/DID documents use short bounded cache entries with deterministic expiry/remapping; conflicts and absent, malformed, duplicate, or non-HTTPS PDS services fail before PAR.
  - [x] Callback token subject/PDS-audience substitutions fail before the private bridge, and linked-account-only onboarding remains unchanged.
- **Dependencies:** Phase 13.
- **Effort:** M
- **Required Skills/Rules:** auth-patterns.

### Phase 15: Tenant-Local Inbound Event Import

- **Goal:** Materialize every accepted tenant-visible ATProto event into the normal Event model with exactly one EventSession while retaining the global canonical record as protocol authority.
- **Depends on:** Phases 10, 11, 13.3, and 14.
- **Progress:** Implementation complete and independently confirmed in Todo 21. Canonical all-project verification remains owned by Todo 22.
- **Related skills/rules:** clean-architecture-rules, cqrs-mediatr-guidelines, dotnet-efcore-guidelines, application-layer.md, efcore-persistence.md, tests.md.
- **Acceptance criteria:**
  - A dedicated internal command/handler manually instantiates a validator whose only required lexicon fields are name and createdAt; every optional supplied field remains validated.
  - Jetstream create/update/tombstone and complete PDS snapshot reconciliation atomically synchronize the canonical record, tenant presentation, one Event, and one EventSession under the existing fence and advisory lock.
  - Replays preserve aggregate identities, newer source versions update source-owned fields, and tombstones soft-delete imported aggregates.
  - First safe source URI maps to EventUrl; startsAt/endsAt map to unscheduled, open-ended, or fixed session timing; mode/status/RSVP expectation and provenance map deterministically.
  - Inbound import never plans outbound work, assigns a local owner role, exposes a public import contract, or changes unrelated dynamic-event UI files.
- **Phase-end verification (run once after all tasks):**
  - dotnet build --configuration Release --verbosity quiet
  - dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
  - dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category!=Runtime]" --minimum-expected-tests 1
  - dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
- **Rollback / failure handling:** The canonical record/projection/presentation transaction remains authoritative; any aggregate-import failure rolls the whole fenced apply back so replay can retry.

#### Task 15.1: Materialize accepted ATProto events as Event aggregates with one EventSession

- **Status:** Complete and independently confirmed. Root-cause/mapping evidence is under `.omo/evidence/atproto-auth/import-investigation/`; implementation and final adversarial evidence are under `.omo/evidence/atproto-auth/task21/`.
- **Type:** create / modify
- **Layer:** Application / Infrastructure / Persistence
- **Files:**
  - dedicated ATProto import request, validator, command, handler, and mapper under `src/Explore.Application/Features/Federation/Atproto`
  - `src/Explore.Infrastructure/Services/Federation/AtprotoJetstreamRuntimeStore.cs`
  - `src/Explore.Persistence/Repositories/AtprotoJetstreamRepository.cs`
  - PDS reconciliation request/handler
  - focused Application, Infrastructure, and PostgreSQL tests
- **Description:** Map lexicon `name` to Event/Session title, `createdAt` to preserved source creation time, description to bounded content, the first policy-approved `uris[].uri` to EventUrl, starts/ends to the single session, mode to Local/Digital/Hybrid, status to Draft/Published/Cancelled, RSVP expectation to registration-required metadata, and DID/AT URI to provenance and a source-managed tenant actor. Apply the same mapping in live Jetstream and complete PDS snapshot recovery.
- **Acceptance Criteria:**
  - [x] Missing name or createdAt fails before persistence; malformed optional fields cannot create a partial aggregate.
  - [x] One visible tenant receives one Event and one EventSession linked by AtprotoRecordId and provenance.
  - [x] Duplicate, update, tombstone, cancellation, partial snapshot, and transaction-failure cases converge without duplicate rows or outbound outbox work.
  - [x] Scheduled imported events de-duplicate to the local Event in discovery; planned/draft records retain the federated projection until locally publishable.
  - [x] Source createdAt and EventUrl survive a real PostgreSQL round trip.
- **Dependencies:** 10.1, 11.1, 13.3, 14.1.
- **Effort:** XL
- **Required Skills/Rules:** clean-architecture-rules, cqrs-mediatr-guidelines, dotnet-efcore-guidelines.

## 7. Testing Strategy

Each phase owns focused tests with its implementation tasks, then runs one Release build and one fastest relevant non-browser project once.

| Phase | Selected project | Why |
|---|---|---|
| 1 | Event.Architecture.Tests | Proves CarpaNet/layer/reference/package boundaries and required file conventions. |
| 2 | Event.Persistence.IntegrationTests | Proves real PostgreSQL tenant uniqueness and encrypted session round-trip. |
| 3 | Event.API.IntegrationTests | Proves bootstrap auth, bridge authorization, DID rejection, JWT validation, and Keycloak/API-key regression. |
| 4 | Explore.Blazor.IntegrationTests | Proves challenge/callback/store/cookie/handoff behavior in the BFF host. |
| 5 | Event.Application.UnitTests | Proves refresh/revoke orchestration and failure classification without live providers. |
| 6 | Explore.Blazor.Client.Tests | Proves generated/browser contract isolation, accessible login behavior, and HAL affordance use. |
| 7 | Event.Application.UnitTests | Proves the effective setting cascade, lock semantics, user consent, and both lifecycle validation profiles. This repeats Phase 5 because it validates a separate event-governance slice. |
| 8 | Event.Application.UnitTests | Proves complete snapshot mapping, deterministic description rendering, coverage failure, lexicon validation, and no truncation. This repeats by design because projection is pure Application behavior. |
| 9 | Event.Persistence.IntegrationTests | Proves local-event/outbox atomicity, unique idempotency, recoverable claims, URI/CID settlement, and event-before-RSVP ordering against PostgreSQL. |
| 10 | Explore.Infrastructure.Tests | Proves exact Jetstream collection filters, cursor/replay behavior, parsing, allowlist, echo prevention, reconnect, and tombstones. |
| 11 | Event.API.IntegrationTests | Proves tenant gating, de-duplication, public API/OpenAPI behavior, and HAL relations for inbound/local federation state. |
| 12 | Explore.Blazor.Client.Tests | Proves lock-aware admin/user controls, accessible consent/profile UX, federated rendering, and HAL-only action gating. |
| 13 | Explore.Infrastructure.Tests | Proves Jetstream dynamic updates, backfill logic, CAR reading, and token refresh hooks. |
| 14 | Explore.Blazor.IntegrationTests | Proves universal PDS OAuth routing and dynamic provider discovery. |
| 15 | Event.Persistence.IntegrationTests | Proves atomic canonical-to-Event/EventSession import, mapping, replay, update, tombstone, tenant isolation, and rollback against PostgreSQL. |

Intent-mandated projects are distributed across the fifteen phases. Repeated test projects have distinct bounded purposes recorded above. The report's Bluesky/Eurosky/self-hosted-PDS matrix is release evidence outside these implementation phase gates; it is not scheduled as browser/manual/live-service verification in this plan.

## 8. Documentation, Configuration, And Operations Impact

- **ADRs:** Create docs/adr/ADR-014-atproto-session-trust-bridge.md and docs/adr/ADR-015-atproto-event-record-and-outbox.md.
- **Configuration:** Document canonical ATProto public URL, callback, issuer/audience/TTL, Redis atomic-state requirement, loopback-only development behavior, egress policy, the single ATProto Events capability, two administrator locks, validation profile, user consent, Jetstream endpoint/cursor, allowlist, retry/lease policy, and backfill settings (`federation.atproto_events_backfill_enabled`, `federation.atproto_events_backfill_mode`).
- **Secrets:** Register/document OAuth client private JWKS, OAuth-session AES-GCM key ring, and API session-signing private JWKS; describe rotation overlap and recovery.
- **Schema:** Update schemas/islamu-event.md for DID-keyed encrypted session persistence, hardened AtprotoRecord/PdsSyncOutbox, inbound record/cursor state, and local-event/outbox/settlement ordering.
- **Lexicons:** Add vendored getSession for OAuth and generate event/RSVP bindings hermetically from the two existing community lexicons.
- **API:** Add bridge/refresh/revoke operations and remove generic raw-token create/update. Update docs/API_CHANGELOG.md and generated client.
- **Security/authorization:** Update docs/SECURITY-MODEL.md and docs/AUTHORIZATION.md for AtprotoBootstrap versus AtprotoSession.
- **Federation:** Update docs/FEDERATION.md and docs/LEXICONS.md with capability/lock/consent semantics, both validation profiles, exhaustive single-description mapping, DB-first outbox flow, CarpaNet RestoreSessionAsync delivery, Jetstream ingress, moderation, tombstones, and HAL presentation.
- **Self-hosting/troubleshooting:** Add key/cache/egress/PDS outage diagnostics, rotation, backup, and session invalidation.
- **Aspire/Compose:** No change is planned unless Task 1.3 or 10.1 proves an egress, Redis, Jetstream endpoint, or worker topology setting is missing; that discovery re-baselines this plan before editing deployment files.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- **Trust boundaries:** BFF client assertion authenticates the calling workload only; PDS getSession authenticates the ATProto identity; the platform JWT authenticates subsequent API calls.
- **Authorization:** Bridge, refresh, revoke, and metadata writes/reads use explicit schemes/policies and tenant/user checks. Bootstrap is not a platform user principal.
- **Tenant isolation:** Tenant is signed into bootstrap/state/handoff, resolved server-side, and included in repository predicates and JWT claims.
- **DID binding:** Expected handle resolution, OAuth sub, Carpa authenticated DID, and PDS getSession DID must all match.
- **SSRF:** HTTPS, no redirects, DNS/IP filtering, time/size bounds, and egress readiness apply to handle, DID document, protected-resource, AS, and PDS endpoints.
- **Replay:** OAuth state, bootstrap jti, and tenant handoff code are atomic single-use values with short TTLs.
- **Secrets:** Only encrypted OAuthSessionData is durable. All private keys are secret-resolved and kid-versioned.
- **JWT confusion:** Bootstrap and session JWTs have distinct issuer/audience/purpose/key validators. Selector parsing is not authorization.
- **Rate limiting:** Challenge, callback, bridge, refresh, and revoke receive bounded policies; attacker-controlled handle resolution has stricter limits.
- **Idempotency:** Session establish is an upsert by tenant/provider/DID; refresh serializes rotation; revoke/delete is idempotent.
- **Privacy:** No unverified email match, no synthetic email, no raw handle/DID in metrics. Logs use bounded correlation and safe classifications.
- **HAL:** Any browser mutation affordance uses returned links, never claims or roles.
- **PDS ordering:** Request handlers make no PDS call. The database publication and outbox insert are atomic; only a committed claim can reach CarpaNet.
- **Consent:** Effective administrator enablement is capability only. Each outbound user's explicit self-scoped consent, linked DID, and restorable session are mandatory.
- **Publication privacy:** “All” applies to the explicit public/federatable snapshot. Private registration/attendee data, moderation/report evidence, internal audit/concurrency/soft-delete state, secrets, and internal IDs are never projected.
- **Description integrity:** Every allowed non-native value is rendered; incomplete coverage and size overflow prevent enqueue. Truncation and best-effort omission are forbidden.
- **Ingress abuse:** Jetstream accepts only two NSIDs, validates bounded records, applies a curated DID/PDS allowlist, de-duplicates local echoes, and uses bounded reconnect/backoff/quarantine signals.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Plan |
|---|---|---|
| Multi-tenancy | Applicable | Tenant-bound state/assertion/storage/JWT/handoff; canonical callback with one-time cross-host transfer. |
| Federation | Applicable | OAuth is implemented first; governed event/RSVP publish/fetch and recovery follow in Phases 7-14 with DB-first ordering. |
| Localization | Applicable | Stable BFF error codes are mapped through existing UI localization patterns; provider error text is not displayed raw. |
| Accessibility | Applicable | Preserve semantic label, keyboard submission, focus, error announcement, and one-page-heading rules in LoginRedirect. |
| Authorization/HAL | Applicable | Existing resource authorization remains unchanged because sub is the platform user Guid; rendered actions stay HAL-gated. |
| Account onboarding | Needs decision | Safe plan supports already-linked identities only. New DID-only accounts require a separate approved domain/product change. |
| Custom domains | Applicable | Canonical callback uses opaque atomic handoff; no JWT in URL. |
| Self-hosting | Applicable | Operator must configure URL, three key purposes, atomic state backend for multi-node, and egress constraints. |
| Validation governance | Applicable | Instance default/locks plus unlocked tenant override choose platform or community minimum; security/persistence invariants never relax. |
| User publication consent | Applicable | Self-scoped opt-in independent of auth login and administrator enablement; revocation stops new unclaimed publication. |
| Event data completeness | Applicable | One typed public snapshot; native fields plus exhaustive description; no separate session records or truncation. |

## 11. Observability And Operations

- Add counters and duration histograms for challenge, callback, bridge verification, refresh, revoke, and readiness outcomes.
- Use bounded outcome labels such as success, validation_failed, state_replay, did_mismatch, pds_unavailable, token_invalid, key_missing, and reauth_required.
- Preserve trace/correlation IDs across BFF-to-API calls without baggage containing handles, DIDs, tokens, or query strings.
- Health checks validate local configuration/key/cache/egress readiness, not live user PDS credentials.
- Remote PDS 429/5xx/timeouts are retryable only where CarpaNet/session semantics permit; invalid_grant, DID mismatch, corrupt envelope, and unknown key are reauthentication failures.
- Outbound metrics distinguish gated, consent_missing, mapping_failed, payload_too_large, pending, retrying, completed, permanent_failed, and reauth_required with bounded labels.
- A lease age gauge/counter exposes stuck/reclaimed outbox claims; URI/CID settlement and RSVP dependency failures are separately observable.
- Jetstream metrics cover connection state, lag/cursor age, accepted/ignored/quarantined/tombstoned records, and reconnect outcomes without full DID/rkey/payload labels.
- Key rotation retains previous public/decryption keys for a bounded overlap, then invalidates/re-encrypts remaining sessions.
- Operator recovery is explicit: disable provider, rotate/recover key, clear transient state, invalidate OAuth sessions, and require login. Never silently fall back to plaintext or weaker validation.

## 12. Migration And Compatibility Plan

- The EF migration replaces incomplete plaintext credential columns with one encrypted session envelope, DID, and key ID.
- Migration fails if an unexpected legacy ATProto session exists; implementation does not guess how to encrypt an unverified old row.
- Development databases may be reset or ATProto sessions invalidated. No user/event business data is intentionally deleted.
- Down migration can restore the old schema shape but cannot reconstruct invalidated credentials; rollback forces reauthentication and is documented.
- Generic create/update token endpoints, DTOs, handlers, serializer roots, and generated methods are deleted in the same workstream.
- No aliases, deprecated route forwarding, dual schema, compatibility mapper, or old/new tests.
- Deployment order: secrets and public URL available; Phase 1 metadata/readiness; Phase 2 migration; API bridge/JWT; BFF flow; lifecycle; contract cleanup; then enable auth.atproto_login_enabled.
- Federation migration adds hard outbox/record/cursor constraints and seeded governance definitions. Because compatibility is not required, obsolete split-toggle or incomplete federation columns are removed in the same migration rather than dual-written.
- Deployment order after OAuth: governance defaults locked/disabled; projection and persistence migration; outbound worker; inbound worker; API/HAL; client controls; then instance administrator explicitly enables/unlocks as desired.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| CarpaNet version/API drift | Medium | High | Exact pin, lock file, adapter seam, package/API evidence. | Restore/build compile break or contract test failure. | 1.1 |
| Internal CarpaNet client bypasses SSRF handler | Medium | Critical | Prove injection; otherwise fail readiness and require enforced egress. | Policy test cannot observe constrained handler. | 1.3 |
| Key ownership conflicts with active secrets refactor | High | High | Ownership checkpoint; one canonical registry/key naming change. | Overlapping dirty files or duplicate secret keys. | 1.2 |
| OAuth StoreAsync occurs before callback returns | High | High | State ConsumeAsync populates scoped flow context consumed by API-backed StoreAsync. | Store called without expected DID/tenant. | 4.1 |
| Bootstrap endpoint violates write authorization | Low after design | Critical | Dedicated AtprotoBootstrap scheme, jti replay cache, narrow policy. | Anonymous or wrong-scheme request succeeds. | 3.1 |
| JWT issuer selector becomes a trust decision | Medium | Critical | Bound parse, select only, full JwtBearer validation. | Forged issuer token reaches authorized endpoint. | 3.3 |
| Custom-domain cookie cannot cross hosts | High | High | Atomic opaque canonical-host handoff. | Callback succeeds but destination remains anonymous. | 4.3 |
| Redis loss weakens state single-use | Medium | Critical | Fail closed in multi-node; process-local mode only for explicit single-node dev. | Readiness degraded/unavailable. | 4.1, 5.3 |
| New-user report DoD conflicts with explicit linking | High | High | Preserve no-auto-match; expose safe linked-account requirement; request product decision separately. | Unlinked login rejected in bridge test. | A7, 3.2 |
| Session key rotation makes sessions unreadable | Medium | High | kid key ring, overlap, bounded rewrite, recovery docs. | unknown_kid/corrupt_envelope metrics. | 2.2, 5.3 |
| Secret-bearing DTO survives generation | Medium | High | Delete source DTOs/actions and assert generated client isolation. | Credential property found in WASM assembly/client. | 6.1, 6.2 |
| PDS event becomes visible before local commit | Low after design | Critical | Same-transaction local publication/outbox; worker-only network after commit; integration rollback test. | Remote create observed without committed Event/outbox. | A10, 9.2, 9.3 |
| Remote success followed by settlement crash duplicates an event | Medium | High | Stable preallocated rkey, idempotent/reconcilable write, unique constraints, lease recovery. | Two URIs/rkeys for one source event/version. | 9.1, 9.3 |
| Community profile is mistaken for “no validation” | Medium | Critical | Explicit hard-invariant layer, supplied-value validation, locked admin copy, regression tests. | Cross-tenant/invalid optional data publishes. | A9, 7.2, 12.1 |
| An event field or session is silently absent from description | Medium | High | Typed snapshot, coverage manifest, all-session fixtures, deterministic golden/structural tests, fail closed. | Coverage test or payload manifest mismatch. | A11, 8.1, 8.2 |
| Complete description exceeds repository limit | Medium | Medium | Measure encoded payload, no truncation, permanent no-PDS result with safe status. | payload_too_large metric/status. | 8.2, 9.2 |
| “All data” leaks private/internal information | Medium | Critical | Explicit public/federatable allowlist and privacy exclusions; no EF reflection/raw graph serialization. | Privacy fixture appears in generated description. | 8.1, 8.2 |
| Split toggle semantics reappear | Low | High | One canonical capability key; delete old assumptions; effective-setting tests. | Fetch and publish resolve differently for same scope. | A8, 7.1 |
| Jetstream creates echo duplicates or ingests abuse | Medium | High | Two-collection filter, allowlist, lexicon/size validation, URI/CID de-duplication, tombstones. | Duplicate local/federated card or quarantined spike. | A12, 10.1, 11.1 |
| Missing Part B prose hides a persistence/moderation choice | Medium | High | User clarification is binding; Task 9.1 records residual choices before runtime federation edits and cannot weaken A8-A12. | ADR unresolved or implementation contradicts plan invariant. | 9.1 |
| Jetstream buffer expiry during long downtime | Low | Medium | Detect cursor age, fallback to listRecords downtime-restricted backfill, log critical gap warnings. | critical_ingest_gap log warning / alert | 13.3 |
| PDS API rate limiting during full backfill | Medium | Medium | Implement standard rate-limiting, retry-after support, and paging limits in CarpaNet sync gateway. | pds_rate_limit_exceeded metrics | 13.3 |


## 14. Success Metrics And Definition Of Done

### Functional

1. A pre-linked ATProto identity can complete handle challenge, PDS consent, callback, cookie sign-in, and an authorized API request.
2. BFF and API independently validate DID binding.
3. Database contains one tenant/provider/DID encrypted session plus correct IndexedDid and existing identity linkage; no plaintext token/JWK is discoverable.
4. First-party JWT authorization produces the same user ID, permissions, and HAL links as the linked platform account.
5. Refresh rotates/persists PDS material before replacing the platform JWT.
6. Signout clears the cookie even if PDS revocation is unavailable and removes the durable session idempotently.
7. Keycloak and API-key authentication remain unchanged.
8. Disabled/misconfigured ATProto is absent or unavailable with a truthful safe reason.
9. Generated WASM-visible contracts contain no secret credential models.
10. Client metadata/JWKS and hermetic generated bindings match the pinned CarpaNet package.
11. Effective ATProto Events false stops both inbound ingestion/presentation and new outbound enqueue; true makes both available subject to consent/link/session and moderation.
12. Platform validation preserves existing publication requirements; community validation permits a lexicon-minimum title/name event with server-created timestamp while retaining every non-relaxable invariant.
13. A failed local create/publish validation or rolled-back transaction never yields a PDS event or claimable outbox row.
14. A successful eligible publication commits the application event and PDS outbox atomically; CarpaNet writes only after commit and settlement stores URI/CID.
15. One PDS event record contains every session and every other non-native public snapshot value in its description; coverage/size failure creates no PDS record and never truncates.
16. Event URI/CID settles before RSVP strongRef publication; crash retry does not duplicate event records.
17. Jetstream ingests only the two community collections, de-duplicates local echoes, enforces allowlist/validation, persists cursor, and purges tombstones.
18. Tenant home/event-list APIs and clients show allowed federation state only when enabled and gate actions solely by HAL.
19. Tenant admin can configure and trigger a backfill, recovering missing events from downtime or sync all history from CAR files.
20. Ingested events are created in the database, validating against validation profiles and deduplicated.
21. OAuth works across Bluesky, Eurosky, and self-hosted PDS hosts; registration abstractions support future integration of PDS custodial registration without architectural rewrites.

### Security and operations

- Replay, DID mismatch, wrong issuer/audience/kid/algorithm, cross-tenant access, private-network resolution, and corrupt encryption envelope tests reject safely.
- Logs, metrics, traces, URLs, ProblemDetails, OpenAPI examples, and support docs contain no secret values.
- Rotation, invalidation, cache failure, and PDS outage recovery are documented.

### Automated phase gates

Each of the fifteen phases is complete only when all its tasks are checked and its declared Release build/test gates pass. No separate manual/browser/live-provider gate is part of this plan.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. At first implementation start, read plan, context, and tasks once. On cold resume, read context/tasks first and only the needed plan sections.
2. Start from the highest-priority unchecked task unless the user overrides it.
3. Treat atproto-auth-tasks.md as the hot ledger. Mark substantial tasks in progress and check them immediately when their acceptance criteria are met.
4. Keep task and phase verification checkboxes separate. A phase is not complete until its build and selected test pass.
5. Update status summary, completed count, priority, next slice, deferred work, and Last Updated whenever task state changes.
6. Update context after a phase, material decision/discovery, blocker, failed validation, or handoff. Update this plan only when scope, architecture, phase order, acceptance, risk, or validation changes.
7. Record validation failure cause and recovery action without checking the affected task/phase.
8. Before pause, compaction, transfer, or PR creation, reconcile affected tasks and add a dated context handoff including unrelated dirty files to avoid.
9. Run phase verification only after all phase tasks, once: one Release build and at most one selected test project. Do not start the app, browser, Docker, Aspire, or live PDS for phase verification.
10. Before editing SecretDefinitionRegistry, InfrastructureSecretSettingKeys, ISecretResolver, or BFF Infisical mapping, reconcile ownership with secrets-refactor-control-plane; never overwrite its changes.
11. Before modifying the ATProto handler, mark dev/pause/blazor-clean-code-refactor task 6A.5 as superseded/absorbed only if that workstream's maintenance rules allow it.
12. Before any Phase 9/10 runtime edit, complete ADR-015 Task 9.1. Its residual choices may not weaken the single capability, community-profile semantics, DB-first ordering, exhaustive snapshot/description, user consent, or two-collection ingress requirements.
13. Never introduce a PDS network call into CreateEvent, PublishEvent, update/cancel/delete/redact, or registration request transactions; only committed outbox rows may call CarpaNet.
14. Never report completion when repository reality and the ledger disagree.

Every implementation summary must teach:

- what changed and why;
- architecture/design patterns, CarpaNet APIs, cryptographic purposes, infrastructure, protocols, and repository abstractions used;
- important files/classes/handlers/services/components and their responsibilities;
- the end-to-end data/control flow;
- Clean Architecture, CQRS/MediatR, manual validation, tenant isolation, BFF token containment, JWT validation, transactional outbox ordering, exhaustive projection, idempotency, encryption, retry/error handling, Jetstream filtering, and HAL conventions;
- exact verification, remaining work, next slice, and dev-doc status.

## 16. Progress Reporting Contract

After each implementation slice, report:

    Implemented: developer teaching summary
    Verified: exact evidence
    Remaining: incomplete or deferred work
    Next: recommended next slice
    Docs updated: yes/no with reason

For completed work, Docs updated must confirm that atproto-auth-tasks.md was reconciled. Report context and plan separately as updated or unchanged because no trigger occurred.

## 17. Potential Risks & Unknowns

The most likely failure is not CarpaNet's OAuth mechanics; it is the glue ordering around CallbackAsync. CarpaNet stores the session before returning the authenticated client, while this platform must attach expected DID and tenant to an authorized API persistence call. Task 4.1 therefore owns a narrow, testable state-consume-to-scoped-context bridge. If the pinned CarpaNet version changes that call order or bypasses the configured HTTP transport, stop and re-baseline instead of adding hidden fallback paths.

The largest product gap is account onboarding. The report simultaneously requires no ATProto email auto-match and claims a new user exists after first login, while current User creation requires email and deliberately rejects unlinked ATProto identities. This plan preserves the safer linked-account rule. New-user support requires an explicit product/domain decision, not a synthetic address or silent relaxation.

The largest federation risk is completeness at the privacy boundary. The user requires absolutely all additional event information in one description, but the domain also contains non-public state. The implementation therefore must treat `AtprotoEventPublicationSnapshot` as the exhaustive contract: every approved public field is present and covered, while every excluded field has a reviewed privacy/internal rationale. Runtime reflection over entities would make that boundary unreviewable and is forbidden.

The missing Part B prose remains an evidence gap, but it is no longer a blocker. The user's 2026-07-18 clarification supersedes the report summary's split fetch/publish controls and supplies the binding publication/validation/projection behavior. Task 9.1 must record residual persistence, cursor, moderation-storage, and settlement choices in ADR-015 before federation runtime code. If the eventual report body conflicts, stop and re-baseline rather than adding compatibility branches.

The strongest invariant is testable at transaction boundaries: no local validation success means no local publication and no outbox; no database commit means no claimable outbox; no claimable outbox means no CarpaNet repository write. Once a remote write succeeds, the local event remains authoritative even if settlement or later remote operations fail. Recovery reconciles the stable record key and URI/CID; it never deletes the local event to simulate distributed rollback.
