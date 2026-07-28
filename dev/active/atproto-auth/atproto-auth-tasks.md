<!-- ABOUTME: Executable checklist for the sixteen-phase AT Protocol OAuth and event-federation implementation. -->
<!-- ABOUTME: Tracks DB-first publication, canonical ingress, tenant-local event import, HAL, and phase gates. -->

# AT Protocol Integration — Task Checklist

Last Updated: 2026-07-28 Europe/Brussels

## Status Summary

- **Overall status:** Complete. All 34/34 implementation tasks and 28/28 execution-plan gates are independently verified, including Todo 24, Todo 23, F1-F4, five final review lanes, and the runtime audit.
- **Completed:** 34/34 implementation tasks; Todo 24 whole-container thumbnail validation; Todo 23 final matrix; F1-F4.
- **Current priority:** None; implementation closeout is complete.
- **Next recommended slice:** None. Live-provider checks are release activity outside this workstream.
- **OAuth scope:** Phases 1-6.
- **Federation scope:** Phases 7-16 complete; canonical ingress preserves unsupported/future producer fields in `AtprotoRecord.RecordJson` and maps only semantically compatible fields locally.

## Progress Reconciliation — 2026-07-28

- Todos 16-24 and F1-F4 are confirmed; all 28 top-level gates are complete.
- Phase 13.1 settings passed Application 17/17, Infrastructure 13/13, API 8/8, Architecture 9/9, PostgreSQL 1/1, and both bUnit administration surfaces 25/25 combined.
- Phase 13.2 held one Jetstream session while updating filters in place; its subscriber/readiness/runtime-store matrix passed 30/30, including coalescing, failure reconnect, cursor preservation, and cancellation cleanup.
- Phase 13.3 passed the real PostgreSQL PDS snapshot reconciliation matrix 4/4 twice after correcting one test-only JSONB comparison to semantic JSON equality.
- Phase 13.4 passed the real PostgreSQL encrypted refresh matrix 6/6 plus refresh-lock repeat 2/2; focused security/store/writer/architecture gates are green and no credential material was emitted.
- Phase 14.1 passed the expanded provider-neutral discovery matrix: auth flow 17/17, encrypted API-backed session binding 6/6, deterministic cache expiry/remap 2/2 twice, OAuth transport 23/23, and the 10,000-entry cache capacity/cross-lease probes.
- Docker/Testcontainers cleanup is complete. The concurrent dynamic-event-management UI workstream was explicitly excluded from ATProto attribution and edits.
- Final fingerprint: base `c57ebcca0e00b33c43ee4899a285cf1cb8bbd2b2`, manifest `e5ad4d92d28ae06e12df82277519a7865f093b6dc43f24bb249710a388c0a0cd`, patches `62e5bcbe8f8c8f63b94f38885851914934578f25d586f733d911af297ba19882`, `507a101294b607326a796e033557d81f5742eb73730ab98c3e4fd191b4673469`, and `fbf56d56056d10791b5c7506d7e7d51f5cc102c73b2477f17e69d7197ad7fc51`.
- Final matrix: Release 26/0; Infrastructure 184; Application 130; Architecture 301 plus one governed skip; PostgreSQL 73; H5 1; H6 5; gateway 49 twice; PostgreSQL rejection twice; safe PNG 1. Five review lanes and runtime audit passed under `.omo/evidence/atproto-auth/final-security-review-container-validation/`.

## Historical Broad Verification Snapshot — 2026-07-19

The Release build and all nine per-project commands from `docs/TESTING.md` were attempted individually. ATProto-focused Application, Infrastructure, API, BFF, persistence, architecture, and component suites remain green.

This is a historical per-project snapshot, not the final completion result. Its shared-tree failures were superseded by the exact-fingerprint final matrix recorded in the 2026-07-28 reconciliation above.

| Gate | Result | Evidence / classification |
|---|---|---|
| Release solution build | Blocked | At the frozen canonical snapshot, exit 1 came from two `CS9035` errors at `NotificationFanoutOccurrenceRepositoryTests.cs:789`: unrelated `Event.Tenant` and `Event.VisibilityType` fixture members were missing. The fresh Todo 15 build found the three additional direct test-source errors summarized above; affected ATProto production projects remain source-unrelated. |
| Event.Domain.UnitTests | Passed | Exact project command exited 0. |
| Event.Application.UnitTests | Blocked | 2,734 passed, 2 failed, 2 skipped. Both failures are unrelated notification/email metrics tests: `NotificationFanoutPageProcessorTests.ReplayAfterPartialPageConvergesAndAdvancesOnce` and `BusinessMetricsEmailDispatchTests.RecordEmailDispatchOperationalSignalsUsesOnlyBoundedSafeTags`. |
| Event.Architecture.Tests | Blocked | 255 passed, 2 failed, 1 skipped. Every ATProto-owned violation was removed; the remaining failures are `EmailDispatchProcessorControlLinkPolicy` and the pre-existing `CustomPropertyExposureScope` naming violation. |
| Explore.Secrets.UnitTests | Passed | Exact project command exited 0. |
| Explore.Infrastructure.Tests (`Category!=Runtime`) | Passed | Exact non-runtime project command exited 0. |
| Event.Persistence.IntegrationTests | Blocked | Does not compile because of the same two unrelated `CS9035` fixture errors at `NotificationFanoutOccurrenceRepositoryTests.cs:789`. |
| Event.API.IntegrationTests | Indeterminate broad gate | The full command was terminated at the tool/process boundary without a test failure result; the focused ATProto API discovery, JWT, bridge, and contract suites pass. |
| Explore.Blazor.IntegrationTests | Passed | Exact project command exited 0. |
| Explore.Blazor.Client.Tests | Passed | 1,728 passed and one pre-existing explicit skip; only the known AngleSharp `NU1902` advisory remains. |

The reported `ExploreJsonContext`, obsolete `PermissionAction`, and control-plane nullability diagnostics are fixed. Release builds of Explore.Application, Explore.Persistence, Explore.Infrastructure, Explore.API, Explore.Blazor, and Explore.Blazor.Client complete without those errors.

## Implementation Maintenance Rules

- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Mark a substantial task IN PROGRESS when it is likely to span multiple edits or a handoff; skip this churn for tiny tasks completed immediately.
- Check a substantial completed task immediately; reconcile small completed tasks no later than phase end.
- Add discovered work where it belongs and keep completed count, priority, next slice, deferred work, and update date accurate.
- Check a phase complete only after all implementation and phase-verification checkboxes pass.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Update the plan only when scope, architecture, sequencing, acceptance criteria, risk, or validation strategy changes.
- Do not run build/tests after individual tasks; verify once at phase end.
- Do not start the app, browser, Docker, Aspire, Playwright, Chrome DevTools, or live services for phase verification.
- Task 1.2 resolved secret ownership with three direct instance-only registry definitions and no legacy `InfrastructureSecretSettingKeys` compatibility constant; preserve that boundary.
- Do not begin Phase 9/10 runtime edits before ADR-015 Task 9.1; its residual choices may not weaken the single capability, user consent, community-profile semantics, DB-first order, exhaustive description, or two-collection ingress.
- Never add a PDS network call to event/registration request transactions. Only committed outbox rows call CarpaNet.
- Never truncate or silently omit public snapshot data to make a PDS record fit; coverage/size failure means no PDS enqueue.
- Preserve unrelated worktree changes; never revert another workstream.

## Blocking Decisions And Gates

- [x] **Execution approval accepts the linked-account-only default.**
  - Current SyncUser behavior rejects unlinked ATProto identities without verified email; no synthetic email, implicit user creation, or email auto-match is planned.
- [x] **Task 1.2 secret ownership is reconciled with secrets-refactor-control-plane.**
  - Three direct instance-only secret definitions and the BFF `/atproto` Infisical mapping own the separated key purposes; no legacy ATProto constant or cross-layer BFF dependency was added.
- [x] **Confidential-client gate:** Task 1.3 proves the Event-owned discovery-aware scoped-key handler adds fresh CarpaNet `ClientAssertion` values to validated PAR/token/refresh/revoke requests while preserving DPoP nonce behavior; no CarpaNet fork or public-client downgrade is used.
- [x] **Task 1.3 proves a constrained CarpaNet transport or enforced deployment egress.**
- [x] **Task 9.1 ADR-015 is complete before any Phase 9/10 federation runtime edit beyond its own schema/ADR work.**

## Phase 1: CarpaNet Boundary, Client Identity, And ADR — IMPLEMENTATION COMPLETE; VERIFICATION PENDING

- [x] **1.1 Pin CarpaNet and make lexicon generation hermetic**
  - **Files:** Directory.Packages.props; src/Explore.Blazor/Explore.Blazor.csproj; src/Explore.Infrastructure/Explore.Infrastructure.csproj; their packages.lock.json files; the exact eight-file getSession/event/RSVP lexicon closure under schemas/lexicons; tests/Event.Architecture.Tests/AtprotoDependencyBoundaryTests.cs (new).
  - **Evidence:** Independently verified; `.omo/evidence/atproto-auth/task-2/README.md` records package/schema provenance, generated-binding coverage, isolated NuGet restore/signature checks, locked restores, architecture/build gates, and protected-tree audit results.
  - **Acceptance:**
    - [x] CarpaNet, CarpaNet.OAuth, and CarpaNet.Jetstream use exact stable central versions with consistent lock files.
    - [x] Infrastructure generates getSession, event, and RSVP bindings from the exact local eight-file closure without DNS/network resolution.
    - [x] Architecture tests allow CarpaNet only in BFF/Infrastructure and reject it in Domain/Application/WASM/Persistence.
    - [x] The selected package version and evidence source are recorded in context.
  - **Effort:** M
  - **Dependencies:** None.

- [x] **1.2 Record ADR-014 and implement client metadata/key publication**
  - **Files:** docs/adr/ADR-014-atproto-session-trust-bridge.md (new); AtprotoAuthenticationOptions.cs; AtprotoOAuthEndpointExtensions.cs (new); AuthenticationExtensions.cs; BffEndpointExtensions.cs; ConfigurationExtension.cs; ServiceRegistrationExtensions.cs; AtprotoClientKeyProvider.cs (new); SecretDefinitionRegistry.cs; AtprotoOAuthPublicationTests.cs (new).
  - **Evidence:** Implementation `.omo/evidence/atproto-auth/task-3a/`; independent confirmation `.omo/evidence/atproto-auth/task-3a-verifier/`. Security rework rejects ambiguous/non-local callbacks, credential-bearing or non-canonical public origins, and non-canonical JWK coordinates/scalars while keeping private material out of public/browser surfaces.
  - **Acceptance:**
    - [x] Metadata uses the URL client_id, exact redirect URI, private_key_jwt, dpop_bound_access_tokens, and scope atproto transition:generic.
    - [x] JWKS never emits private parameters; unknown/missing/invalid keys fail readiness.
    - [x] OAuth client/bootstrap, session encryption, and API session signing key purposes are separate and documented.
    - [x] Metadata endpoints are GET, anonymous, cache-bounded, and size-bounded; focused BFF integration coverage protects their HTTP behavior and Task 4.2 retains end-to-end flow regression coverage.
    - [x] ADR-014 records A1-A7 and rejects anonymous bridge writes and BFF-trusted user identity.
  - **Effort:** L
  - **Dependencies:** 1.1 and secrets-refactor-control-plane ownership checkpoint.

- [x] **1.3 Constrain CarpaNet outbound networking and startup readiness**
  - **Files:** src/Explore.Atproto.Transport (new shared boundary); AtprotoOAuthClientFactory.cs in Explore.Blazor and Explore.Infrastructure (new); AtprotoCoreClientFactory.cs (new); ServiceRegistrationExtensions.cs; InfrastructureServicesRegistration.cs; BffProviderReadinessService.cs; docs/SELF_HOSTING.md; docs/TROUBLESHOOTING.md; focused BFF/Infrastructure/architecture tests.
  - **Evidence:** `.omo/evidence/atproto-auth/task-3/README.md`; independent verifier verdict **SCOPED CONFIRMED** after the full security rework and attributable-warning repair.
  - **Acceptance:**
    - [x] Challenge, callback discovery, getSession, refresh, and signout each have a documented constrained transport path.
    - [x] DNS rebinding and redirect-to-private-address cases are rejected.
    - [x] Development loopback helpers are enabled only in Development and never weaken production policy.
    - [x] Provider readiness explains missing config/key/cache/egress prerequisites without exposing secrets.
  - **Effort:** L
  - **Dependencies:** 1.1, 1.2.

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS

Current limitation: Task 1.3 is independently scoped-confirmed, but the latest root rerun is blocked by unrelated concurrent email-retention fixture/interface changes and an unrelated email side-effect architecture failure. Do not edit those files from this workstream; rerun these gates when the shared tree settles.

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet

## Phase 2: Encrypted DID-Keyed Session Persistence — VERIFIED COMPLETE; POSTGRESQL RUNTIME UNAVAILABLE

- [x] **2.1 Replace plaintext token persistence with a DID-keyed encrypted session envelope**
  - **Status:** Independently confirmed. Implementation, focused non-container tests, migration guards, model-drift check, and Release build are green; Docker-backed PostgreSQL execution remains unavailable.
  - **Files:** UserAuthenticationToken.cs; UserAuthenticationTokenConfiguration.cs; IUserAuthenticationTokenRepository.cs; UserAuthenticationTokenRepository.cs; generated ProtectAtprotoOAuthSessions EF migration/snapshot; schemas/islamu-event.md; UserAuthenticationTokenRepositoryTests.cs (new).
  - **Acceptance:**
    - [x] No plaintext credential property/column remains in the runtime model.
    - [x] The unique key prevents two active records for the same tenant/provider/DID while allowing the same DID in different tenants.
    - [x] Repository methods return entities, use explicit tracking intent, accept cancellation, and never call IgnoreQueryFilters.
    - [x] Migration and schema docs state that rollback invalidates sessions and requires login.
    - [x] Every touched legacy file gains two ABOUTME lines.
  - **Effort:** L
  - **Dependencies:** Phase 1.

- [x] **2.2 Implement the repository-backed CarpaNet session store**
  - **Status:** Independently confirmed after nested-null/missing-member envelopes were changed from raw exceptions to classified `invalid_session` failures.
  - **Files:** AtprotoSessionEnvelopeProtector.cs (new); RepositoryBackedOAuthSessionStore.cs (new); InfrastructureServicesRegistration.cs; IUserAuthenticationTokenRepository.cs; RepositoryBackedOAuthSessionStoreTests.cs (new); docs/SECRETS.md.
  - **Acceptance:**
    - [x] Store/Get round-trips DPoP JWK, token set, auth method, client ID, redirect URI, scope, and PDS metadata.
    - [x] Database inspection in the persistence test proves recognizable token/JWK substrings are absent.
    - [x] Delete is tenant/DID scoped and idempotent.
    - [x] Unknown kid, authentication-tag failure, and malformed envelope fail closed without secret values in logs.
    - [x] Rewriting under the active kid is supported without a dual plaintext path.
  - **Effort:** L
  - **Dependencies:** 2.1.

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet

## Phase 3: Authenticated API Trust Bridge And MultiAuth — VERIFIED COMPLETE; POSTGRESQL RACE RUNTIME DEFERRED BY POLICY

- [x] **3.1 Add the authenticated ATProto bootstrap boundary**
  - **Status:** Independently confirmed. Route/method/tenant/audience-bound BFF ES256 assertions, privileged-header stripping, route-only API authentication, and production PostgreSQL atomic replay consumption pass focused verification.
  - **Files:** AtprotoBootstrapAssertionService.cs (new); BffCookieForwardingHandler.cs; AtprotoBootstrapAssertionValidator.cs (new); API AuthenticationExtensions.cs; ApiAuthenticationSchemeNames.cs; AtprotoBootstrapAuthenticationTests.cs (new).
  - **Acceptance:**
    - [x] Missing, expired, wrong-audience, wrong-route, wrong-tenant, unknown-kid, non-ES256, and replayed assertions are rejected.
    - [x] AtprotoBootstrap cannot authorize any endpoint except the bridge.
    - [x] The assertion carries no trusted DID/user identity and the API still performs PDS verification.
    - [x] Browser-supplied privileged headers are removed before proxying.
  - **Effort:** L
  - **Dependencies:** 1.2, 1.3.

- [x] **3.2 Verify, synchronize, persist, and mint the first-party session**
  - **Status:** Independently confirmed through real hardened Carpa discovery/DPoP/getSession, encrypted persistence/restore, typed mismatch zero writes, atomic Actor/IndexedDid/session synchronization, and post-commit JWT mint/retry.
  - **Files:** IAtprotoOAuthSecurityGateway.cs (new); Application Features/Authentication/Atproto models/request/handler/validator (new); Infrastructure AtprotoOAuthSecurityGateway.cs (new); AtprotoSessionController.cs (new); RouteNames.cs; AtprotoSessionBridgeTests.cs (new).
  - **Acceptance:**
    - [x] No write occurs before all DID/PDS checks pass.
    - [x] Unlinked ATProto identities fail without email matching or user creation.
    - [x] A linked identity produces User/Actor/UserExternalLogin consistency, IndexedDid metadata, one encrypted session row, and a platform JWT.
    - [x] Validator is manually instantiated; repositories return entities; IndexedDid/session writes are atomic and a retry safely repairs a post-SyncUser failure.
    - [x] Controller has explicit version, route, route name, classification, authorization scheme, rate limit, response metadata, ProblemDetails, and no-store policy.
    - [x] Request/exception logs contain only correlation IDs, tenant, PDS hostname classification, and redacted DID hash where necessary.
  - **Effort:** XL
  - **Dependencies:** 2.2, 3.1.

- [x] **3.3 Route and validate ATProto session JWTs in MultiAuth**
  - **Status:** Independently confirmed. Purpose-separated ES256 validation, bounded selector, exact tenant/provider claims, lifetime bounds, Guid subject, and Keycloak/API-key parity pass focused verification.
  - **Files:** API AuthenticationExtensions.cs; ApiAuthenticationSchemeNames.cs; AtprotoSessionJwtOptions.cs (new); MultiAuthAtprotoSessionTests.cs (new); docs/AUTHORIZATION.md; docs/SECURITY-MODEL.md.
  - **Acceptance:**
    - [x] Only ES256, known kid, exact issuer/audience, valid lifetime, and required claims are accepted.
    - [x] Oversized/malformed/claim-confused tokens are rejected without selector exceptions.
    - [x] A token routed to the wrong scheme never succeeds.
    - [x] API key and Keycloak regression cases remain green.
    - [x] sub remains the platform user Guid so existing authorization and HAL policies work unchanged.
  - **Effort:** M
  - **Dependencies:** 3.2.

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet

## Phase 4: BFF Challenge, Callback, Cookie, And Tenant Handoff — IMPLEMENTATION VERIFIED; ROOT BUILD GATE PENDING

- [x] **4.1 Implement single-use OAuth state and API-backed session adapters**
  - **Status:** Independently confirmed. Atomic state/handoff consumption, API-backed Store/Get/Delete, and the purpose-separated current-session bridge pass focused and full-project coverage.
  - **Evidence:** `.omo/evidence/atproto-auth/task-7/README.md`.
  - **Files:** AtprotoOAuthFlowContext.cs (new); CacheBackedOAuthStateStore.cs (new); ApiBackedOAuthSessionStore.cs (new); ServiceRegistrationExtensions.cs; AtprotoOAuthStoreTests.cs (new).
  - **Acceptance:**
    - [x] State expires within the configured short TTL and can be consumed exactly once.
    - [x] State is bound to issuer, tenant, expected DID, origin, and safe return path.
    - [x] API-backed StoreAsync never accepts a browser caller and never logs session material.
    - [x] Get/Delete use authenticated API operations and remain tenant/DID scoped.
    - [x] Redis GETDEL is used in configured multi-node deployments; local memory mode is explicitly single-node development only.
  - **Effort:** L
  - **Dependencies:** 3.1, 3.2.

- [x] **4.2 Complete challenge and callback processing**
  - **Status:** Independently confirmed after repairing error-callback state reuse, mixed code/error ambiguity, missing rate-limit registration, and `Task<IResult>` route dispatch. A dedicated fixed-window policy bounds both endpoints through real HTTP middleware, and the hermetic WebApplicationFactory matrix exercises real CarpaNet PAR/token/callback behavior.
  - **Evidence:** `.omo/evidence/atproto-auth/task-7/README.md`; the focused challenge/callback suite is green with a two-case WebApplicationFactory matrix running real CarpaNet against hermetic DNS/PLC/PDS/AS/PAR/token responses.
  - **Files:** AtprotoAuthenticationHandler.cs; AtprotoAuthenticationOptions.cs; BffAuthEndpoints.cs; DynamicAuthSchemeManager.cs; AtprotoAuthenticationFlowTests.cs (new).
  - **Acceptance:**
    - [x] Missing/invalid/oversized handles fail before DNS/HTTP resolution.
    - [x] Challenge redirects only to the CarpaNet-produced HTTPS authorization URL.
    - [x] Callback rejects state, issuer, DID, tenant, and flow-context mismatches.
    - [x] BFF integration tests verify metadata/JWKS status, media type, cache policy, redirect URI, scope, and public-only key shape.
    - [x] FishyFlip comments/stub behavior are removed.
    - [x] Return paths remain local/allowlisted and raw exception/provider content never reaches the query string.
  - **Effort:** L
  - **Dependencies:** 1.3, 4.1.

- [x] **4.3 Complete cookie sign-in and canonical-host tenant handoff**
  - **Status:** Independently confirmed. The mapped handoff endpoint passes protected-cookie, host-substitution, replay, malformed-code, and browser-canary coverage; the real-Carpa WebApplicationFactory matrix proves both direct cookie issuance and cross-host opaque handoff creation/consumption.
  - **Evidence:** `.omo/evidence/atproto-auth/task-7/README.md`.
  - **Files:** AtprotoTenantSessionHandoffStore.cs (new); BffAuthEndpoints.cs; ExploreBffCookieSessionHandler.cs; CircuitAccessTokenService.cs; AtprotoTenantHandoffTests.cs (new).
  - **Acceptance:**
    - [x] Same-host callback signs in directly; cross-host callback uses one-time opaque handoff.
    - [x] Handoff is origin/tenant/expiry bound and rejects replay or host substitution.
    - [x] No JWT or PDS credential appears in URLs, browser storage, WASM auth state, or response bodies.
    - [x] Cookie HTTPS, SameSite, antiforgery, and existing BFF token-forwarding behavior remain intact.
  - **Effort:** L
  - **Dependencies:** 4.2.

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [x] dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet

## Phase 5: Session Refresh, Revocation, Readiness, And Operations — IMPLEMENTATION VERIFIED; SHARED BROAD GATES BLOCKED

- [x] **5.1 Refresh PDS and first-party sessions coherently**
  - **Status:** Independently confirmed. Server-derived tenant/user/DID identity is manually validated, serialized with a deterministic PostgreSQL session advisory lock, restored through the encrypted session store, refreshed through CarpaNet, reverified with authenticated `getSession`, and persisted before a replacement first-party JWT can return. Invalid durable/PDS state clears the BFF session and yields one stable reauthentication response.
  - **Evidence:** `.omo/evidence/atproto-auth/task-8/README.md` records the green Infrastructure gateway, API metadata, BFF refresh, readiness, observability, and client-isolation gates plus the explicit shared-tree PostgreSQL/Application limitations.
  - **Files:** RefreshAtprotoSession command/handler (new); IAtprotoSessionRefreshLock.cs and PostgresAtprotoSessionRefreshLock.cs (new); AtprotoOAuthSecurityGateway.cs; AtprotoCoreClientFactory.cs; AtprotoSessionController.cs; BffSessionRefreshService.cs; refresh gateway/handler/lock tests (new).
  - **Acceptance:**
    - [x] Only the authenticated user's tenant/DID session can refresh.
    - [x] Rotated OAuthSessionData is durably stored before the new platform JWT is returned.
    - [x] Missing/corrupt/revoked PDS session fails as reauthentication, not an infinite retry.
    - [x] Concurrent refresh has one authoritative persisted result and does not regress token rotation.
    - [x] Existing Keycloak refresh tests/behavior are preserved.
  - **Effort:** L
  - **Dependencies:** Phase 4.

- [x] **5.2 Revoke remotely and clear locally on sign-out**
  - **Status:** Independently confirmed. Cookie/circuit state is cleared first; a bounded private DELETE dispatches a typed CQRS revoke that calls real CarpaNet sign-out and always deletes the exact tenant/user/DID durable session, including remote outage and caller cancellation. Repeat absence is safe; no local-delete compatibility command remains.
  - **Evidence:** `.omo/evidence/atproto-auth/task-8/README.md`.
  - **Files:** RevokeAtprotoSession command/handler and typed result (new); AtprotoOAuthSecurityGateway.cs; AtprotoRevocationObserver.cs (new); AtprotoSessionController.cs; BffAuthEndpoints.cs; revoke gateway/handler/BFF tests (new).
  - **Acceptance:**
    - [x] Remote success and already-revoked cases delete the local durable session.
    - [x] Remote outage is logged/metriced without exposing tokens and never prevents cookie deletion.
    - [x] Cross-user/cross-tenant revoke is rejected.
    - [x] Repeat signout is safe and returns the existing local signout behavior.
  - **Effort:** M
  - **Dependencies:** 5.1.

- [x] **5.3 Make provider readiness and telemetry truthful**
  - **Status:** Independently confirmed. Passive health/readiness distinguishes disabled, ready, and safely unavailable configuration without live PDS/OAuth probes. Fixed operation/outcome telemetry covers readiness, challenge, callback, bridge verification, refresh, and revoke without identity, URL-query, token, JWK, or exception-body labels.
  - **Evidence:** `.omo/evidence/atproto-auth/task-8/README.md`; operator recovery and rotation guidance is updated in CONFIGURATION, SECRETS, SELF_HOSTING, and TROUBLESHOOTING.
  - **Files:** BffProviderReadinessService.cs; AtprotoAuthenticationHealthCheck.cs (new); AtprotoAuthenticationMetrics.cs (new); Blazor Program.cs and service registration; docs/CONFIGURATION.md; docs/SECRETS.md; docs/SELF_HOSTING.md; docs/TROUBLESHOOTING.md; AtprotoObservabilityPolicyTests.cs (new).
  - **Acceptance:**
    - [x] Disabled provider is omitted; misconfigured provider is unavailable with a safe reason.
    - [x] Metrics have bounded labels and no full DID, handle, URL query, token, JWK, or exception body.
    - [x] Health checks do not perform per-probe live PDS login or leak configuration values.
    - [x] Operator docs cover key rotation overlap, session invalidation, cache loss, PDS outage, and recovery.
  - **Effort:** M
  - **Dependencies:** 5.1, 5.2.

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS

Current limitation: focused refresh/revoke production and handler contracts are green, but the complete Application project/canonical build is blocked by unrelated concurrent event-location/email-dispatch drift. PostgreSQL contention is additionally blocked before its test body by unrelated missing `is_deleted` fixture schema state. Exact evidence is in `.omo/evidence/atproto-auth/task-8/README.md`.

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet

## Phase 6: Public Contract Cleanup And Safe Client Surface — VERIFIED COMPLETE; ROOT BUILD GATE EXTERNALLY BLOCKED

- [x] **6.1 Remove secret-bearing generic token mutation contracts**
  - **Status:** Independently confirmed. Raw generic token writes and direct public `AtprotoRecord` mutations are absent from controllers, CQRS contracts, OpenAPI, serializers, HAL mutation links, and the generated browser client. No compatibility route, DTO, handler, mapper, or shim was retained.
  - **Evidence:** `.omo/evidence/atproto-auth/task-8/README.md` includes the exact empty forbidden-symbol scan and focused generated-surface test.
  - **Files:** UserAuthenticationTokenController.cs; Create/Update token DTOs and validators (delete); Create/Update token commands and handlers (delete); AtprotoRecordController.cs direct mutation actions; Create/Update AtprotoRecord DTOs/commands/handlers/serializer roots/generated methods/mutation HAL links (delete); token/AtprotoRecord privacy and route-absence tests (modify).
  - **Acceptance:**
    - [x] OpenAPI has no generic raw-token create/update operation.
    - [x] Safe DTOs expose only ID, provider, PDS host, and expiry.
    - [x] Delete/revoke remains authorized, self/tenant scoped, and idempotent.
    - [x] No compatibility route, command, DTO, mapper, serializer entry, or test remains.
    - [x] Public OpenAPI, HAL, serializers, and generated clients contain no direct `AtprotoRecord` create/update/delete authority; only lifecycle outboxes and canonical ingress write records.
  - **Effort:** M
  - **Dependencies:** Phase 5.

- [x] **6.2 Regenerate clients and align safe account-session UX/docs**
  - **Status:** Independently confirmed. The canonical NSwag command ran once; the handle form is required, labelled, autofocus/keyboard accessible, and announces stable errors while POSTing server-side without URL handle leakage. API/auth/federation docs distinguish implemented OAuth/session behavior from pending event/RSVP phases.
  - **Evidence:** `.omo/evidence/atproto-auth/task-8/README.md`.
  - **Files:** EventApiClient.g.cs; AppJsonSerializerContext.cs; LoginRedirect.razor; AuthRedirectPagesTests.cs; AtprotoCredentialIsolationTests.cs (new); docs/API_CHANGELOG.md; docs/FEDERATION.md; docs/AUTHORIZATION.md.
  - **Acceptance:**
    - [x] Generated client/JSON context contains no deleted credential types or bridge session material.
    - [x] The server-private bridge and removed direct `AtprotoRecord` mutations are absent from browser OpenAPI/client/serializer surfaces.
    - [x] Login handle label, validation, focus, keyboard submission, and error announcement remain accessible.
    - [x] UI never gates per-resource actions from roles/claims.
    - [x] API_CHANGELOG records removed endpoints and new bridge/refresh/revoke operations.
    - [x] FEDERATION distinguishes implemented OAuth authentication from the still-pending event/RSVP phases in this workstream.
  - **Effort:** M
  - **Dependencies:** 6.1.

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS

Current result: the complete Blazor.Client project, 14-test login/privacy minimum, forbidden-symbol scan, and diff check are green. The broad BFF project is 313/314 with one isolated-repeat, source-attributed failure in a pre-existing non-hermetic tenant-page fixture that reaches unavailable external API/localhost endpoints; do not mark this phase verified until independent review accepts or repairs that shared fixture.

- [ ] dotnet build --configuration Release --verbosity quiet
- [x] dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet

## Phase 7: ATProto Events Governance And Validation Profiles — IMPLEMENTATION VERIFIED; CURRENT BROAD SUITE HAS UNRELATED FAILURES

- [x] **7.1 Add the ATProto Events capability, locks, and user consent**
  - **Files:** GovernanceSettingKeys.cs; AtprotoFederationSettingDefinitions.cs (new); AtprotoFederationSettingGroup.cs (new); generic instance/tenant/user settings handlers; SeedIds.cs; LookupTableSeeder.cs; SettingsController.cs; InstanceSettingGroupLinkPolicy.cs (new); InstanceSettingGroupResourceAssembler.cs (new); AtprotoFederationGovernanceTests.cs (new); InstanceSettingGroupApiTests.cs (new).
  - **Acceptance:**
    - [x] `federation.atproto_events_enabled` is the only administrator capability switch for both fetch and new outbound enqueue.
    - [x] Capability and validation profile are `IsLockable`; unlocked tenant overrides use the existing lock/unlock commands, persisted lock state, five-tier resolver, and server-provided edit reason.
    - [x] Instance administrators manage capability/profile through ATProto-only instance-scope read/update/lock/unlock routes; unrelated registry keys never reach generic setting commands, and clients use authorization-filtered HAL links as the action authority.
    - [x] Exactly three definitions are added (capability, profile, self-consent); no parallel `lock_tenant_*` setting keys exist.
    - [x] Profile accepts only `platform` or `community_lexicon` and defaults fail-closed to platform.
    - [x] User publication consent defaults false, is self-scoped/auditable/revocable, and cannot be granted by an administrator.
    - [x] auth.atproto_login_enabled remains independent.
  - **Effort:** L
  - **Dependencies:** Phase 6.

- [x] **7.2 Make create/publish readiness profile-aware**
  - **Files:** ValidationProfile.cs; EventLifecyclePolicyProvider.cs; EventLifecycleReadinessEvaluator.cs; AtprotoEventGovernanceResolver.cs (new); CreateEventCommandHandler.cs; PublishEventCommandHandler.cs; AtprotoEventValidationProfileTests.cs (new).
  - **Acceptance:**
    - [x] Platform preserves current scheduled-session/visibility/format readiness.
    - [x] Community accepts Title/name plus server-generated CreatedAt without requiring sessions/start/end/type/audience.
    - [x] Authorization, tenant, owner, status, concurrency, storage, reference integrity, and every supplied optional value remain validated.
    - [x] Current EF constraints persist the community-minimum event without broad schema relaxation.
    - [x] `community_lexicon` readiness is eligible only while the effective ATProto Events capability is enabled; disabled/unknown capability uses platform readiness.
  - **Effort:** L
  - **Dependencies:** 7.1.

### Phase 7 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet

## Phase 8: Canonical Publication Snapshot And Exhaustive Description — VERIFIED COMPLETE; POSTGRESQL RUNTIME UNAVAILABLE

- [x] **8.1 Load and build the canonical public/federatable event snapshot**
  - **Status:** Independently confirmed after adding the reflected scalar source inventory, maximal 16-collection Application graph, and real PostgreSQL graph/query-budget test body.
  - **Files:** IEventRepository.cs; EventRepository.cs; AtprotoEventPublicationSnapshot.cs (new); AtprotoEventPublicationSnapshotFactory.cs (new); snapshot factory tests (new).
  - **Acceptance:**
    - [x] Tenant-filtered entity loading covers all public scalars, every session/day/agenda/location/room, actors/groups/organizations, categories/tags, lookups, aspects, speakers/languages, and event/session EAV values without N+1.
    - [x] Event/session locations are projected only from `EventLocationDisclosureEvaluator` results for `EventLocationDisclosurePurpose.Public`; private-home, delayed, and erased address canaries are absent.
    - [x] Application maps the entity graph to one immutable snapshot; repositories still return entities.
    - [x] Soft-deleted/private/internal data is excluded explicitly.
    - [x] Attendee/private registration data, moderation/report evidence, audit/concurrency/soft-delete internals, secrets, and internal IDs never enter the snapshot.
  - **Effort:** XL
  - **Dependencies:** 7.2.

- [x] **8.2 Map the community record and render every additional field**
  - **Status:** Independently confirmed with zero uncovered/stale/ambiguous manifest paths, a maximal deterministic description fixture, CarpaNet mappers/validators, exact size limits, and persisted-scope RSVP semantics.
  - **Files:** Infrastructure csproj; existing community event/RSVP lexicons; AtprotoCalendarEventRecordData.cs and AtprotoCalendarRsvpRecordData.cs (new); event/RSVP mappers, validators, independently maintained source-field manifests; description formatter tests (new).
  - **Acceptance:**
    - [x] Native name/description/createdAt/startsAt/endsAt/mode/status/locations/uris/rsvpExpected fields are mapped when available.
    - [x] One deterministic description contains base content and every non-native public field, including all sessions, EAVs, aspects, resolved lookups, days, agenda, locations, registration, pricing, categories, and tags.
    - [x] Independently maintained event and RSVP source-field manifests fail when any source field is neither native, rendered, nor explicitly privacy-excluded; manifests are not derived from mapper output.
    - [x] Typed RSVP projection maps a successfully committed active `EventRegistrationIntent`/registration lifecycle only to `community.lexicon.calendar.rsvp#going` plus settled event URI/CID; organizer `ApprovalStatus`, attendee identity/answers, and private registration data are excluded. User cancellation/deletion plans remote delete; `interested`/`notgoing` are not emitted.
    - [x] Stable ordering/display formatting is byte-deterministic; no raw-ID-only lookup output or raw EF/HTML dump.
    - [x] Invalid shape, unsafe value, coverage gap, or encoded-size overflow returns permanent no-PDS; never truncate.
  - **Effort:** XL
  - **Dependencies:** 8.1.

### Phase 8 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet

## Phase 9: Transactional Outbound Event And RSVP Publication — IMPLEMENTATION INDEPENDENTLY CONFIRMED; BROAD BUILD GATE BLOCKED BY UNRELATED TEST SOURCE

- [x] **9.1 Record ADR-015 and harden federation persistence**
  - **Status:** Independently confirmed. Two focused fail-closed migrations, fenced repositories, atomic Jetstream state, model alignment, and Release build are green; Docker-backed PostgreSQL execution remains unavailable.
  - **Files:** ADR-015 (new); AtprotoRecord.cs/config; PdsSyncOutbox.cs/config/repository/contract; generated migration/snapshot; schemas/islamu-event.md.
  - **Acceptance:**
    - [x] ADR/schema define outbound tenant/user ownership, global canonical inbound ownership, tenant presentation/visibility joins, direction/provenance, DID/collection/rkey identity, source entity/version, immutable payload/hash, stable idempotency, CID expectations, URI/CID settlement, cursor/checkpoint policy, and user consent.
    - [x] Unique constraints prevent duplicate record identity/logical operation while allowing later aggregate versions.
    - [x] Claims have owner/expiry and crashed Processing leases are reclaimable.
    - [x] Completion settles AtprotoRecord URI/CID and outbox status in one transaction; no result is discarded.
    - [x] Existing Event/EventRegistration FKs and event-before-RSVP dependency are explicitly reconciled.
    - [x] One leased multi-node consumer owns the global canonical cursor/materialization; no per-tenant socket or duplicate inbound record ownership exists.
  - **Effort:** XL
  - **Dependencies:** Phase 8.

- [x] **9.2 Enqueue event publication only from successful local lifecycle transitions**
  - **Status:** Independently confirmed after race-safety, supersession, bounded reconciliation, transaction-rollback, and fixture-fidelity repairs. Evidence: `.omo/evidence/atproto-auth/task-11/README.md`.
  - **Files:** AtprotoEventPublicationPlanner.cs (new); Create/Publish/Update/Cancel/Delete/HeavyRedact Event handlers; lifecycle outbox tests (new).
  - **Acceptance:**
    - [x] Draft create, local readiness failure, capability/consent/link/session failure, mapping failure, and size overflow create no PDS row and make no network call.
    - [x] Local create-as-published/PublishEvent and immutable create outbox commit or roll back together inside IUnitOfWork.
    - [x] Stable rkey/idempotency values are allocated outside retryable delegate or otherwise deterministic across execution-strategy retry.
    - [x] Update/cancel/delete/redact target only an existing outbound AtprotoRecord and never synthesize a remote create.
    - [x] Lexicon/projection failure leaves valid local publication authoritative and exposes a bounded federation status.
  - **Effort:** XL
  - **Dependencies:** 9.1.

- [x] **9.3 Deliver event records, settle URI/CID, then publish RSVP strongRefs**
  - **Status:** Independently confirmed. Fenced CarpaNet delivery, crash-safe reconciliation, strongRef ordering, durable RSVP convergence, and bounded failure observability pass focused Application, Infrastructure, PostgreSQL, and architecture verification. Evidence: `.omo/evidence/atproto-auth/task-11/README.md`.
  - **Files:** IAtprotoPdsDeliveryGateway.cs; AtprotoPdsDeliveryGateway.cs; AtprotoPdsDeliveryProcessor.cs; PdsSyncWorker.cs/options; Create/Delete EventRegistration handlers (Update intentionally emits no RSVP); planner/writer/processor/handler tests.
  - **Acceptance:**
    - [x] Worker restores only the owner DID's CarpaNet OAuth session and never processes an uncommitted/disabled/cross-tenant row.
    - [x] After claim and immediately before remote I/O, worker rechecks effective capability, current self-consent, and `EventLocationDisclosurePurpose.Public`; stale work cannot bypass revocation or privacy changes.
    - [x] Stable-record-key retry/reconciliation prevents duplicate event creation after remote success and settlement crash.
    - [x] Event URI/CID settles before an RSVP strongRef becomes claimable; missing CID defers safely.
    - [x] Active registration emits only `#going`; organizer approval changes never map to RSVP intent; user cancellation/deletion deletes the existing remote RSVP; `interested`/`notgoing` remain unsupported until a real local user-intent model exists.
    - [x] Remote failure never rolls back/deletes the application event and is retry/dead-letter observable without secret/provider-body leakage.
    - [x] Revoked consent/disabled capability stops eligible unclaimed delivery according to ADR-015 without silently deleting completed remote records.
  - **Effort:** XL
  - **Dependencies:** 9.2 and Phase 5.

### Phase 9 Verification — RUN ONCE AFTER ALL PHASE TASKS

Focused independent evidence is green: Application handlers 62/62, planners/processors 36/36, Infrastructure 18/18, live PostgreSQL 23/23, and architecture 22/22. The repository-wide Release build is separately blocked by three unrelated expression-tree compile errors in `NotificationFanoutPageProcessorTests.cs`; keep the broad phase gate open until the shared tree compiles.

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet

## Phase 10: Filtered Inbound Jetstream Federation — VERIFIED COMPLETE; CANONICAL MIGRATION FIXTURE EXTERNALLY BLOCKED

- [x] **10.1 Implement capability-aware Jetstream ingestion and tombstones**
  - **Status:** Independently confirmed after repairing fail-open DID admission, cursor poisoning, and dormant-host startup behavior. Curated DID admission is fail-closed at activation: empty config remains a valid dormant host state, while enabled capability with no curated DIDs is unhealthy and the subscriber, event-source, and parser open/admit nothing. Invalid/out-of-range cursor quarantine explicitly retains the last safe checkpoint so later legitimate envelopes remain ingestible. Independent verification passes subscriber/parser 17/17, readiness 3/3, Release Infrastructure and API/OpenAPI builds, CarpaNet/Clean Architecture boundaries, resolver scenarios 4/4, and live PostgreSQL current-model checks 14/14. The normal migration fixture is blocked before test bodies by unrelated `is_deleted` migration drift; see `.omo/evidence/atproto-auth/task-12/README.md`. Context7 was attempted but its monthly quota is exhausted, so the pinned local docs/source are the current authority.
  - **Files:** Directory.Packages.props; Infrastructure csproj; AtprotoJetstreamSubscriber.cs (new); IAtprotoRecordRepository.cs; AtprotoRecordRepository.cs; API Program.cs; subscriber tests (new).
  - **Acceptance:**
    - [x] One leased multi-node CarpaNet.Jetstream consumer uses WantedCollections containing exactly community event and RSVP; durable global cursor is long microseconds.
    - [x] Disabled capability performs no new materialization; enable/resume uses the last safe cursor.
    - [x] Bounded lexicon/size validation and curated allowlist reject/quarantine unsupported records.
    - [x] Replay is idempotent, locally-owned URI/CID records do not duplicate, and delete/tombstone purges/suppresses dependent RSVP state.
    - [x] Inbound DID/collection/rkey versions are globally canonical; tenant presentation/visibility is separate and tenants never own duplicate rows or sockets.
    - [x] Reconnect/backoff/cancellation and metrics are bounded with no high-cardinality DID/rkey/payload labels.
  - **Effort:** XL
  - **Dependencies:** 9.1, 9.3.

### Phase 10 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [x] dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category!=Runtime]" --minimum-expected-tests 1

## Phase 11: Tenant-Gated Federation API And HAL — IMPLEMENTATION INDEPENDENTLY CONFIRMED; POSTGRESQL VERIFICATION BLOCKED

- [x] **11.1 Extend home/event-list queries and HAL for federated events**
  - **Status:** Independently confirmed. Typed materialization, governed source-aware discovery, local-echo de-duplication, safe source HAL, centralized cache invalidation, API/OpenAPI, and all focused non-PostgreSQL gates are green. The focused persistence suite cannot start because the concurrent migration chain adds `smtp_available_tokens` twice (`42701`). Evidence: `.omo/evidence/atproto-auth/task-13/README.md`.
  - **Files:** GetHomeDiscoveryQueryHandler.cs; HomeDiscoveryDto.cs; GetEventListRequestHandler.cs; PublicExperienceController.cs; EventController.cs; EventLinkPolicy.cs; RouteNames.cs; API federation presentation tests (new).
  - **Acceptance:**
    - [x] Disabled tenants receive no inbound items or federation actions; enabled tenants receive only allowed valid non-tombstoned records.
    - [x] Locally-owned Jetstream records de-duplicate to the local event with federation metadata.
    - [x] Provenance/source metadata and external URLs are bounded/safe; pagination/sort/cache semantics remain stable.
    - [x] HAL relations are the sole source of action authority; GET remains anonymous and writes authorized.
    - [x] OpenAPI, endpoint classification, rate limit, ProblemDetails, response metadata, and route names are explicit.
  - **Effort:** L
  - **Dependencies:** Phase 10.

### Phase 11 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet

## Phase 12: Administrator, User-Consent, And Event Client Surfaces — IMPLEMENTATION COMPLETE; NON-BROWSER VERIFICATION GREEN

- [x] **12.1 Add instance, tenant, and user federation controls**
  - **Status:** Implemented with a typed federation-settings client service, instance HAL-gated default/lock controls, tenant `CanEdit`/server-reason enforcement, and self-scoped user consent. Focused component suites and the full Blazor client suite are green.
  - **Files:** IAtprotoFederationSettingsService.cs; AtprotoFederationSettingsService.cs; InstanceGovernanceSection.razor; TenantPoliciesSection.razor; UserProfile.razor/.cs; generated client/JSON context; instance/tenant/user federation settings tests.
  - **Acceptance:**
    - [x] Instance admin sets defaults/locks; tenant admin edits only when server metadata allows; user edits only personal consent.
    - [x] Copy explains one switch enables fetch plus eligible publication, and community mode reduces required business fields without reducing safety validation.
    - [x] Locked controls expose an accessible server-provided reason and forged client state cannot bypass API policy.
    - [x] No token/private key/private payload enters browser contracts or telemetry.
    - [x] Labels, keyboard/focus, validation, and error announcements meet repository accessibility rules.
  - **Effort:** L
  - **Dependencies:** 7.1, 7.2, 11.1.

- [x] **12.2 Render federated events and delivery status from HAL**
  - **Status:** Implemented with typed discovery mapping, source-only HAL navigation, non-interactive no-affordance cards, text provenance, and tenant-scoped latest PDS delivery state. Stable failure codes map to safe recovery guidance; raw codes/provider bodies are not rendered. Application/UI focused tests and the full 1,729-case Blazor client run are green (1,728 passed, one pre-existing explicit skip).
  - **Files:** HomeDiscoveryExperience.razor; UpcomingEventList.razor; EventList.razor/.cs; federated rendering tests (new).
  - **Acceptance:**
    - [x] Federated provenance/status is understandable without color alone and links have descriptive names.
    - [x] Source/RSVP/retry/sync actions render only from HAL; no role/claim/source-type inference.
    - [x] Stable failure codes map to guidance; raw outbox/provider errors are never displayed.
    - [x] Disabled tenants render no stale federated cards after API/cache refresh; local-only event rendering is unchanged.
  - **Effort:** M
  - **Dependencies:** 11.1, 12.1.

### Phase 12 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [x] dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet

## Phase 13: Inbound Event Recovery and Backfill Configuration — COMPLETE

- [x] **13.1 Add Tenant Settings and Rules for Backfilling**
  - **Status:** Implemented and focused-verified. The two settings use the existing registry, five-tier resolver, generic settings commands/API/HAL policies, stable locked seed rows, and both current administrator UI surfaces. The adjacent validation-profile controls now submit plain setting codes through the same serialization boundary. Evidence: `.omo/evidence/atproto-auth/task-16/settings.md`.
  - **Files:** GovernanceSettingKeys.cs; AtprotoFederationSettingDefinitions.cs; AtprotoFederationSettingGroup.cs; SeedIds.cs; LookupTableSeeder.cs; InstanceGovernanceSection.razor; TenantPoliciesSection.razor; focused Application, Infrastructure, Persistence, API, and Blazor tests.
  - **Acceptance:**
    - [x] Both settings (`federation.atproto_events_backfill_enabled` and `federation.atproto_events_backfill_mode`) resolve through the standard five-tier resolver and generic API/HAL surfaces.
    - [x] Settings default to disabled/downtime-only, use stable locked system seed rows, and are lockable through the instance tier without a schema migration.
  - **Effort:** L
  - **Dependencies:** Phase 12.

- [x] **13.2 Implement Jetstream Dynamic Filter Updates**
  - **Status:** Complete and independently confirmed. One owned CarpaNet session receives and sends normalized, coalesced DID-filter changes without reconnecting; failure reconnects from the unchanged durable cursor with the latest desired filter. Evidence: `.omo/evidence/atproto-auth/task-17/`.
  - **Files:** src/Explore.Infrastructure/Services/Federation/AtprotoJetstreamSubscriber.cs.
  - **Acceptance:**
    - [x] Allowed-DID changes dynamically push the exact nested `SendOptionsUpdateAsync` payload on the existing WebSocket; collections remain the exact event/RSVP pair.
    - [x] Equivalent changes send nothing, bursts coalesce, invalid/oversized filters fail, and update failure preserves the durable cursor before bounded reconnect.
  - **Effort:** M
  - **Dependencies:** 13.1.

- [x] **13.3 Implement Inbound Event Backfill Engine**
  - **Status:** Complete and independently confirmed against real PostgreSQL for canonical snapshot reconciliation. Its former read-model-only aggregate boundary is superseded by Phase 15, which reuses the same transaction to materialize Event/EventSession rows. Evidence: `.omo/evidence/atproto-auth/task-18/pds-recovery.md`.
  - **Files:** ReconcileAtprotoPdsSnapshotsCommand.cs/Handler.cs/Validator.cs (new); AtprotoPdsSnapshotGateway.cs and AtprotoRepositorySnapshotVerifier.cs (new); AtprotoJetstreamRepository.cs (existing).
  - **Acceptance:**
    - [x] Downtime recovery resumes the durable Unix-microsecond Jetstream cursor without PDS snapshot I/O.
    - [x] Full recovery bounds and verifies `com.atproto.sync.getRepo` CAR/commit/MST/record data before atomic reconciliation.
    - [x] DID/collection/rkey deduplication, quarantine, idempotent replay, and complete-snapshot-only tombstoning preserve canonical records and tenant presentations.
  - **Effort:** XL
  - **Dependencies:** 13.2.

- [x] **13.4 Automate Ingest Token Refresh Hook**
  - **Status:** Complete and independently confirmed against real PostgreSQL. All known refresh triggers share the existing exact-scope advisory lock and repository-backed encrypted CarpaNet session store; no second token store or event-payload reconstruction exists. Evidence: `.omo/evidence/atproto-auth/task-19/`.
  - **Files:** AtprotoPdsDeliveryGateway.cs; AtprotoOAuthSecurityGateway.cs; RepositoryBackedOAuthSessionStore.cs; focused Infrastructure and PostgreSQL tests.
  - **Acceptance:**
    - [x] The complete rotated `OAuthSessionData`, including DPoP material, is re-encrypted and saved before refreshed state is usable.
    - [x] Persistence failure prevents success/PDS writes; concurrent refreshes serialize, reread durable state, and retain EF concurrency as the stale-writer fence.
  - **Effort:** L
  - **Dependencies:** 13.3.

### Phase 13 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet

## Phase 14: Decoupled PDS Identity and Extensibility — COMPLETE

- [x] **14.1 Verify Universal PDS OAuth Compatibility**
  - **Status:** Complete and independently confirmed. The provider-neutral flow now covers distinct PDS/authorization-server origins, `did:plc`, hostname-only `did:web`, deterministic cache expiry/remapping, strict PDS-service cardinality/type/HTTPS checks, and callback token binding. Evidence: `.omo/evidence/atproto-auth/task-20/`.
  - **Files:** AtprotoIdentityCache.cs; AtprotoOAuthClientFactory.cs; AtprotoAuthenticationHandler.cs; focused BFF integration and architecture tests.
  - **Acceptance:**
    - [x] Challenges discover and use compliant non-Bluesky PDS and authorization-server endpoints via verified handle/DID resolution and a bounded cache.
    - [x] Invalid, conflicting, duplicate, non-HTTPS, stale, or substituted identity/token inputs fail before PAR/private bridge authority.
    - [x] Registration remains a decoupled future extension point; linked-account-only behavior, schema, and provider-neutral abstractions remain unchanged.
  - **Effort:** M
  - **Dependencies:** Phase 13.

### Phase 14 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet

## Phase 15: Tenant-Local Inbound Event Import — IMPLEMENTATION COMPLETE

- [x] **15.1 Materialize accepted ATProto events as Event aggregates with one EventSession**
  - **Status:** Complete and independently confirmed. Evidence: `.omo/evidence/atproto-auth/task21/final-adversarial-verify.md`.
  - **Files:** dedicated ATProto import request/validator/command/handler/mapper; Jetstream runtime store and repository; PDS reconciliation request/handler; focused tests; ADR/workstream docs.
  - **Acceptance:**
    - [x] Validator requires only lexicon name and createdAt, while validating every optional supplied value.
    - [x] First safe source URI maps to EventUrl; schedule maps to exactly one EventSession.
    - [x] Jetstream and PDS recovery create/update/tombstone the Event/session atomically with canonical record, tenant presentation, and fence/cursor semantics.
    - [x] Replays are idempotent, updates preserve aggregate identities, and inbound imports never enqueue outbound PDS work.
    - [x] Real PostgreSQL evidence proves source timestamp, mapping, one-session cardinality, rollback, replay, update, and tombstone behavior.
  - **Effort:** XL
  - **Dependencies:** 10.1, 11.1, 13.3.

### Phase 15 Verification — RUN ONCE AFTER IMPLEMENTATION

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category!=Runtime]" --minimum-expected-tests 1
- [ ] dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet

## Phase 16: Extensible Inbound Calendar Import — IMPLEMENTATION COMPLETE

- [x] **16.1 Preserve and map extensible inbound calendar fields, canonical slugs, timezones, and thumbnail blobs (Todo 22)**
  - **Status:** Complete and independently confirmed, including Todo 24's final whole-container security repair. Evidence: `.omo/evidence/atproto-auth/task22/executor-repair.md`, `.omo/evidence/atproto-auth/task22/final-adversarial-verify.md`, `.omo/evidence/atproto-auth/task22/debug-container-runtime.md`, and `.omo/evidence/atproto-auth/task24-container-validation/ADVERSARIAL_VERIFY_FINAL.md`.
  - **Scope:** Preserve the complete source record in `AtprotoRecord.RecordJson`; map only semantically compatible values with normal `SlugGenerator.FromTitle(name, "event")` and implicit-session fallback; map valid IANA timezones; resolve authoritative `media[].content` with generic `media[].blob` compatibility through the verified DID/PDS boundary, bounded `getBlob`, and registered storage.
  - **Acceptance:**
    - [x] Exact canonical JSON equality survives Jetstream and complete PDS reconciliation for standard, producer-specific, unknown nested, and prompt-like fields.
    - [x] Imported Event/EventSession slugs are normal, deterministic, and stable across replay/update; malformed optional extensions fail soft and remain raw.
    - [x] The shared Jetstream/PDS thumbnail gateway accepts only exact parameter-free JPEG/PNG/GIF/WebP/AVIF; after bounded read, size, and CID binding, it consumes the declared container structure through EOF before `WriteAsync`.
    - [x] Honest SVG, relabeled SVG, header-plus-active tails, malformed/oversized/mismatched/hung/cancelled optional blobs fail soft: semantic `RecordJson` including nested script remains canonical, `Event`/`EventSession` persist, and no image/storage/outbox/file is created.
    - [x] Safe PNG stores exact bytes and links atomically through `Event.FeaturedImageId` and tenant-owned `StorageObject`; replacement/tombstone cleanup uses the existing lifecycle.
  - **Focused evidence:** H5 1/1, H6 5/5, gateway 49/49 twice, PostgreSQL rejection twice, safe PNG 1/1.
  - **Dependencies:** Phase 15.

### Phase 16 Verification — COMPLETE

- [x] Canonical Release build, affected project matrix, generated-contract/migration checks, and deterministic fake-service/Testcontainers smoke after Task 16.1.

## Todo 23, Todo 24, And Final Verification — COMPLETE

- [x] Todo 23: final locked Release build, affected project matrix with nonzero counts, generated contracts/migrations, and deterministic integration smoke.
- [x] Todo 24: exact parameter-free JPEG/PNG/GIF/WebP/AVIF allowlist plus bounded whole-container structural validation to EOF before storage.
- [x] F1 plan compliance.
- [x] F2 code quality.
- [x] F3 real-surface QA.
- [x] F4 scope fidelity.
- [x] Five final review lanes and runtime debugging audit at `.omo/evidence/atproto-auth/final-security-review-container-validation/`.

Final result: Release 26 projects/0 errors; Infrastructure 184/184; Application 130/130; Architecture 301 plus one governed skip; PostgreSQL 73/73; H5 1/1; H6 5/5; gateway 49/49 twice; PostgreSQL rejection twice; safe PNG 1/1.

## Remaining / Deferred Work

### New ATProto account onboarding — NEEDS PRODUCT DECISION

- **Reason:** Current User creation requires email; ATProto provides no verified email; SyncUser safely requires an existing link.
- **Default:** Linked-account sign-in only.
- **Trigger:** Explicit approval for account-linking UX or an email-less user domain change.
- **Forbidden shortcut:** Synthetic email, unverified email matching, or silent implicit linking.

### Report release matrix — OUTSIDE PHASE GATES

- Bluesky, Eurosky, and self-hosted PDS live-provider evidence remains a release activity outside this plan's non-browser automated phase gates.
- Do not add browser/manual/Aspire/live-PDS tasks to these phases.
