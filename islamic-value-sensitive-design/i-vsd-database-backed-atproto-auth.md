<!-- ABOUTME: Evaluates provider responsibility for Redis-independent, database-backed ATProto login. -->
<!-- ABOUTME: Maps privacy, replay protection, browser binding, and hosting claims to implementation-plan evidence. -->

# Database-Backed ATProto Authentication - I-VSD Planning Review

Last Updated: 2026-09-05 Europe/Brussels

## Review Metadata

- Mode: planning
- Subject: database-backed ATProto transient authentication
- Workstream: database-backed-atproto-auth
- Report kind: implementation-plan
- Report status: current
- Disposition: plan-aligned
- Evidence cutoff: 2026-09-05
- Reviewed input: database-backed-atproto-auth `plan-r1` triad; repository `1ca0edeac1da90d29a135c5efa3f8c6269e2574c` plus existing local planning-contract modifications
- Supersedes: none

## Scope

Replace Redis-dependent OAuth state and tenant-session handoff storage with the existing primary database, behind an authenticated BFF-to-API boundary. Include atomic consumption, initiating-browser correlation, tenant/origin checks, bounded retention, readiness, and accurate hosting guidance. Cover SQLite, PostgreSQL, SQL Server, and MySQL/MariaDB, which are supported repository providers.

The user approved database-backed storage and a clean break from obsolete development-memory configuration. This is not approval to implement or commit this plan. No product code was changed during planning.

The separate [authentication deployment documentation review](i-vsd-authentication-deployment-documentation-alignment.md) owns broader presets and deployment-documentation alignment. This report neither replaces nor edits it.

## Claim Boundary

**I-VSD design inference:** trustworthy stewardship, privacy, equitable self-hosting access, and accountable failure handling favor using an existing database without weakening authentication. The recommendation concerns software-provider choices, not a scholarly ruling, certification, legal determination, or guarantee of production security.

Repository inspection establishes current code behavior; official specifications establish protocol obligations. Neither substitutes for implementation tests, multi-provider contention evidence, or operational proof.

## Findings

### IVSD-F001 - Remove an unnecessary hosting dependency without weakening access control

- Lifecycle: open
- Severity: high
- Evidence level: repository-grounded; design inference
- Principle/domain: trust and justice; infrastructure and operations
- Affected stakeholders: self-hosting operators, attendees, organizers, tenant administrators
- Provider decision and mechanism: production OAuth currently rejects absent Redis. Removing that requirement improves access, but silently switching to local memory would make security and availability topology-dependent.
- Evidence: `src/Explore.Blazor/Services/Auth/AtprotoAtomicCache.cs`; `docs/internal/HOSTING_ARCHITECTURE.md`; user-approved database design.
- Recommendation: one relational authority for transient authentication in all environments, with no Redis or process-memory fallback.
- Validation requirement: Production-mode login without Redis, multi-instance consumption, restart, unavailable-store behavior, and all supported provider contracts.
- Owner: authentication/persistence implementation owner
- Disposition/mapping: planned; `SC-01`, `SC-02`, `SC-07`, `SC-10`; tasks `P1.1`, `P1.2`, `P3.1`, `P3.2`, `P4.1`.

### IVSD-F002 - Prevent bearer URL replay and login substitution

- Lifecycle: open
- Severity: critical
- Evidence level: repository-grounded; protocol-grounded
- Principle/domain: prevention of harm and trust; identity and user experience
- Affected stakeholders: people signing in and communities relying on account identity
- Provider decision and mechanism: signatures/encryption do not confer single-use semantics. Existing state/handoff consumption is atomic, but the inspected flow does not bind completion to a per-flow browser proof. A stateless checkout ticket is not an authentication precedent.
- Evidence: `CacheBackedOAuthStateStore`, `AtprotoTenantSessionHandoffStore`, `BffAuthEndpoints`, `AtprotoAuthenticationHandler`; RFC 9700 Sections 2.1 and 4.7.
- Recommendation: preserve exactly-one-winner consumption; bind each flow to a host-only stable browser proof at the initiating origin using a distinct per-flow HMAC; never issue an authenticated cookie at the canonical callback for a different initiating origin. Keep one bounded fifteen-minute proof cookie, require HTTPS, and never overwrite established proof during parallel flows.
- Validation requirement: races, stolen callback/handoff URLs in a second browser, absent/tampered proof, parallel logins, expiry boundaries, and interrupted consumption.
- Owner: BFF authentication implementation owner
- Disposition/mapping: planned; `SC-02`, `SC-04`, `SC-05`, `SC-06`; tasks `P1.1`, `P2.1`, `P3.1`, `P3.2`.

### IVSD-F003 - Make pre-authentication privilege explicit and narrowly bounded

- Lifecycle: open
- Severity: critical
- Evidence level: repository-grounded; design inference
- Principle/domain: accountability and trust; architecture and governance
- Affected stakeholders: all tenants sharing an instance
- Provider decision and mechanism: a canonical callback cannot infer the originating tenant before reading protected OAuth state. Existing session bootstrap requires DID/tenant context, so reusing it unchanged creates circular authentication or an unsafe tenant fallback.
- Evidence: `AtprotoAuthenticationHandler.CompleteCallbackAsync`, `AtprotoJwtService`, `ApiTenantResolutionMiddleware`, ADR-014.
- Recommendation: a dedicated machine-authenticated, instance-owned transient-auth boundary with no listing, no browser access, purpose-bound assertions, replay protection, and explicit tenant checks once the protected binding is recovered. Do not bypass tenant filters on business entities.
- Validation requirement: reject user JWTs, existing bootstrap tokens, missing/tampered/replayed service assertions, changed bodies, wrong routes/purposes, and wrong-tenant consumption. Verify exclusion from public discovery and YARP forwarding.
- Owner: API authentication implementation owner and security reviewer
- Disposition/mapping: planned; `SC-03`, `SC-04`, `SC-08`; tasks `P2.1`, `P2.2`, `P3.1`.

### IVSD-F004 - Bound credential custody and deletion claims

- Lifecycle: open
- Severity: high
- Evidence level: repository-grounded; design inference
- Principle/domain: privacy and trust; data governance
- Affected stakeholders: account holders and database/backup operators
- Provider decision and mechanism: transient payloads can contain PKCE/DPoP material and platform session data. Database storage adds backup retention and key-custody consequences even when payloads are encrypted.
- Evidence: protected payloads in the two existing BFF stores; `BffDataProtectionExtensions`; EF Core provider documentation.
- Recommendation: retain BFF encryption and purpose separation; persist only hashed opaque locators and required metadata; use short server-enforced TTLs and bounded cleanup; keep keys outside the payload store; prohibit sensitive logs and telemetry labels.
- Validation requirement: ciphertext-only persistence, expiry without dependence on cleanup timing, key loss/rotation behavior, cleanup contention, and secret-free traces. Document that database backups are not synchronously erased by row deletion.
- Owner: persistence/operations implementation owner
- Disposition/mapping: planned; `SC-06`, `SC-07`, `SC-09`, `SC-10`; tasks `P1.1`, `P1.2`, `P4.1`, `P4.2`, `P5.1`.

### IVSD-F005 - Publish operational truth rather than a stateless-authentication claim

- Lifecycle: open
- Severity: medium
- Evidence level: repository-grounded; design inference
- Principle/domain: truthfulness and accountability; communication and operations
- Affected stakeholders: adopters choosing hosting topology and incident responders
- Provider decision and mechanism: removing Redis state storage does not remove the need for persistent/shared BFF Data Protection keys, database migrations, or configured ATProto signing authorities.
- Evidence: `AtprotoOAuthClientFactory.GetReadiness`, `BffProviderReadinessService`, `BffDataProtectionExtensions`, public ATProto and troubleshooting pages.
- Recommendation: readiness must check usable storage, not just DI registration; document supported key persistence independently of Redis and fail-closed restart behavior.
- Validation requirement: healthy/unhealthy dependency transitions through the real health surface; operator instructions for persistent single-node keys and shared multi-replica keys; explicit distinction from stateless checkout.
- Owner: operations/documentation implementation owner
- Disposition/mapping: planned; `SC-01`, `SC-07`, `SC-10`; tasks `P4.1`, `P4.2`, `P5.1`.

## Recommendations

Proceed with the database-backed design under the scenarios and phase gates above. Keep authentication failures closed; restart a login after an uncertain consume response rather than replaying a potentially successful consume. Do not expand this work into generic cache replacement, payment changes, secret-authority migration, or unrelated hosting presets.

## Stakeholders

Operators own deployment keys, backups, and retention policies. The platform owns safe defaults, correct tenant/browser boundaries, understandable errors, and truthful deployment requirements. Attendees and organizers must not need to understand Redis or recover from infrastructure details themselves.

## I-VSD Principles And Domains

Trust/Amanah maps to credential custody and exactly-one-winner state transitions. Privacy maps to encrypted short-lived payloads and no sensitive logs. Justice maps to viable lightweight hosting without a weaker authentication contract. Prevention of harm maps to browser/session substitution defenses. Truthfulness and accountability map to readiness, evidence, and explicit operational limits.

These are engineering interpretations using the repository I-VSD framework; no religious-legal conclusion is asserted.

## Validation Gaps

Implementation does not yet exist. Database races, complete browser flows, schema generation, key persistence across replicas, and API assertion rejection remain implementation gates. Graph MCP is unavailable and LSP symbol lookup timed out; bounded source tracing was used instead.

## Escalation Needed

No scholarly/legal escalation is identified for this narrow infrastructure decision. Any proposal to accept replay, remove browser binding, expose the private store publicly, or retain payloads beyond documented TTLs requires renewed security review and user scope alignment.

## Evidence Reviewed

- Repository revision named in metadata; existing local planning-contract changes were read and preserved.
- `src/Explore.Blazor/Services/Auth/AtprotoAtomicCache.cs`, `CacheBackedOAuthStateStore.cs`, `AtprotoTenantSessionHandoffStore.cs`, `ApiBackedOAuthSessionStore.cs`.
- `src/Explore.Blazor/Authentication/AtprotoAuthenticationHandler.cs`, `src/Explore.Blazor/Extensions/BffAuthEndpoints.cs`.
- `src/Explore.API/Authentication/AtprotoJwtService.cs`, `AtprotoAuthenticationHandlers.cs`, `src/Explore.API/Controllers/AtprotoSessionController.cs`.
- `src/Explore.Persistence/Repositories/IdempotencyRepository.cs`, `AtprotoBootstrapReplayRepository.cs`, and PostgreSQL/SQLite integration tests.
- `docs/internal/adr/ADR-014-atproto-session-trust-bridge.md`, `docs/internal/HOSTING_ARCHITECTURE.md`.
- [AT Protocol OAuth specification](https://atproto.com/specs/auth).
- [RFC 9700 OAuth Security Best Current Practice](https://www.rfc-editor.org/rfc/rfc9700.html).
- [EF Core set-based writes and concurrency](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete).
- [EF Core SQLite limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations).
- [ASP.NET Core Data Protection configuration](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0).

## Missing Evidence

Context7 MCP was explicitly requested but is not available in this session's tool catalog; searches for both `context7` and `resolve-library-id` returned no tool. Official documentation was retrieved directly after web search. No external implementation source or dependency was imported.

## Context Inventory

The canonical local workstream is `dev/active/database-backed-atproto-auth/`, containing `database-backed-atproto-auth-plan.md`, `database-backed-atproto-auth-tasks.md`, and `database-backed-atproto-auth-context.md`. These gitignored files are local working memory, not a durable dependency of this report. Durable implementation decisions graduate to ADR-014 and operator documentation before workstream closure.

## Review Lifecycle

Initial evidence review: 2026-09-05. The user selected relational storage and rejected compatibility baggage. No unresolved user-choice fork remains; cryptographic, transaction, and browser-proof details are engineering constraints.

Revalidated on 2026-09-05 against the authored `plan-r1` plan/context/tasks triad: all five finding IDs map to existing behavioral scenarios and executable task IDs; the report and plan agree on trust boundaries, browser-proof lifetime, expiry and hosting claims. Disposition is `plan-aligned`, not implementation approval. Findings stay open until implementation evidence resolves them. Changes to API authority, callback routing, key storage, payload retention, or provider support make this review stale.
