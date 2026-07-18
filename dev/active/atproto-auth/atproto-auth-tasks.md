<!-- ABOUTME: Executable checklist for the twelve-phase AT Protocol OAuth and event-federation implementation. -->
<!-- ABOUTME: Tracks DB-first publication, exhaustive event projection, governed validation, Jetstream ingress, HAL, and phase gates. -->

# AT Protocol Integration — Task Checklist

Last Updated: 2026-07-18 Europe/Brussels

## Status Summary

- **Overall status:** Approved for execution; implementation in progress.
- **Completed:** 5/27 implementation tasks; phase verification tracked separately.
- **Current priority:** Task 2.1 encrypted DID-keyed session persistence and Phase 8 exhaustive event/RSVP projection are active in parallel.
- **Next recommended slice:** Complete and independently verify Tasks 2.1-2.2/9.1 persistence plus Tasks 8.1-8.2 projection while preserving the confirmed Phase 1 and Phase 7 boundaries.
- **OAuth scope:** Phases 1-6.
- **Federation scope:** Executable Phases 7-12; ADR-015 is Task 9.1.

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
- [ ] **Task 9.1 ADR-015 is complete before any Phase 9/10 federation runtime edit beyond its own schema/ADR work.**

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

## Phase 2: Encrypted DID-Keyed Session Persistence — NOT STARTED

- [ ] **2.1 Replace plaintext token persistence with a DID-keyed encrypted session envelope**
  - **Files:** UserAuthenticationToken.cs; UserAuthenticationTokenConfiguration.cs; IUserAuthenticationTokenRepository.cs; UserAuthenticationTokenRepository.cs; generated ProtectAtprotoOAuthSessions EF migration/snapshot; schemas/islamu-event.md; UserAuthenticationTokenRepositoryTests.cs (new).
  - **Acceptance:**
    - [ ] No plaintext credential property/column remains in the runtime model.
    - [ ] The unique key prevents two active records for the same tenant/provider/DID while allowing the same DID in different tenants.
    - [ ] Repository methods return entities, use explicit tracking intent, accept cancellation, and never call IgnoreQueryFilters.
    - [ ] Migration and schema docs state that rollback invalidates sessions and requires login.
    - [ ] Every touched legacy file gains two ABOUTME lines.
  - **Effort:** L
  - **Dependencies:** Phase 1.

- [ ] **2.2 Implement the repository-backed CarpaNet session store**
  - **Files:** AtprotoSessionEnvelopeProtector.cs (new); RepositoryBackedOAuthSessionStore.cs (new); InfrastructureServicesRegistration.cs; IUserAuthenticationTokenRepository.cs; RepositoryBackedOAuthSessionStoreTests.cs (new); docs/SECRETS.md.
  - **Acceptance:**
    - [ ] Store/Get round-trips DPoP JWK, token set, auth method, client ID, redirect URI, scope, and PDS metadata.
    - [ ] Database inspection in the persistence test proves recognizable token/JWK substrings are absent.
    - [ ] Delete is tenant/DID scoped and idempotent.
    - [ ] Unknown kid, authentication-tag failure, and malformed envelope fail closed without secret values in logs.
    - [ ] Rewriting under the active kid is supported without a dual plaintext path.
  - **Effort:** L
  - **Dependencies:** 2.1.

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet

## Phase 3: Authenticated API Trust Bridge And MultiAuth — NOT STARTED

- [ ] **3.1 Add the authenticated ATProto bootstrap boundary**
  - **Files:** AtprotoBootstrapAssertionService.cs (new); BffCookieForwardingHandler.cs; AtprotoBootstrapAssertionValidator.cs (new); API AuthenticationExtensions.cs; ApiAuthenticationSchemeNames.cs; AtprotoBootstrapAuthenticationTests.cs (new).
  - **Acceptance:**
    - [ ] Missing, expired, wrong-audience, wrong-route, wrong-tenant, unknown-kid, non-ES256, and replayed assertions are rejected.
    - [ ] AtprotoBootstrap cannot authorize any endpoint except the bridge.
    - [ ] The assertion carries no trusted DID/user identity and the API still performs PDS verification.
    - [ ] Browser-supplied privileged headers are removed before proxying.
  - **Effort:** L
  - **Dependencies:** 1.2, 1.3.

- [ ] **3.2 Verify, synchronize, persist, and mint the first-party session**
  - **Files:** IAtprotoOAuthSecurityGateway.cs (new); Application Features/Authentication/Atproto models/request/handler/validator (new); Infrastructure AtprotoOAuthSecurityGateway.cs (new); AtprotoSessionController.cs (new); RouteNames.cs; AtprotoSessionBridgeTests.cs (new).
  - **Acceptance:**
    - [ ] No write occurs before all DID/PDS checks pass.
    - [ ] Unlinked ATProto identities fail without email matching or user creation.
    - [ ] A linked identity produces User/Actor/UserExternalLogin consistency, IndexedDid metadata, one encrypted session row, and a platform JWT.
    - [ ] Validator is manually instantiated; repositories return entities; IndexedDid/session writes are atomic and a retry safely repairs a post-SyncUser failure.
    - [ ] Controller has explicit version, route, route name, classification, authorization scheme, rate limit, response metadata, ProblemDetails, and no-store policy.
    - [ ] Request/exception logs contain only correlation IDs, tenant, PDS hostname classification, and redacted DID hash where necessary.
  - **Effort:** XL
  - **Dependencies:** 2.2, 3.1.

- [ ] **3.3 Route and validate ATProto session JWTs in MultiAuth**
  - **Files:** API AuthenticationExtensions.cs; ApiAuthenticationSchemeNames.cs; AtprotoSessionJwtOptions.cs (new); MultiAuthAtprotoSessionTests.cs (new); docs/AUTHORIZATION.md; docs/SECURITY-MODEL.md.
  - **Acceptance:**
    - [ ] Only ES256, known kid, exact issuer/audience, valid lifetime, and required claims are accepted.
    - [ ] Oversized/malformed/claim-confused tokens are rejected without selector exceptions.
    - [ ] A token routed to the wrong scheme never succeeds.
    - [ ] API key and Keycloak regression cases remain green.
    - [ ] sub remains the platform user Guid so existing authorization and HAL policies work unchanged.
  - **Effort:** M
  - **Dependencies:** 3.2.

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet

## Phase 4: BFF Challenge, Callback, Cookie, And Tenant Handoff — NOT STARTED

- [ ] **4.1 Implement single-use OAuth state and API-backed session adapters**
  - **Files:** AtprotoOAuthFlowContext.cs (new); CacheBackedOAuthStateStore.cs (new); ApiBackedOAuthSessionStore.cs (new); ServiceRegistrationExtensions.cs; AtprotoOAuthStoreTests.cs (new).
  - **Acceptance:**
    - [ ] State expires within the configured short TTL and can be consumed exactly once.
    - [ ] State is bound to issuer, tenant, expected DID, origin, and safe return path.
    - [ ] API-backed StoreAsync never accepts a browser caller and never logs session material.
    - [ ] Get/Delete use authenticated API operations and remain tenant/DID scoped.
    - [ ] Redis GETDEL is used in configured multi-node deployments; local memory mode is explicitly single-node development only.
  - **Effort:** L
  - **Dependencies:** 3.1, 3.2.

- [ ] **4.2 Complete challenge and callback processing**
  - **Files:** AtprotoAuthenticationHandler.cs; AtprotoAuthenticationOptions.cs; BffAuthEndpoints.cs; DynamicAuthSchemeManager.cs; AtprotoAuthenticationFlowTests.cs (new).
  - **Acceptance:**
    - [ ] Missing/invalid/oversized handles fail before DNS/HTTP resolution.
    - [ ] Challenge redirects only to the CarpaNet-produced HTTPS authorization URL.
    - [ ] Callback rejects state, issuer, DID, tenant, and flow-context mismatches.
    - [ ] BFF integration tests verify metadata/JWKS status, media type, cache policy, redirect URI, scope, and public-only key shape.
    - [ ] FishyFlip comments/stub behavior are removed.
    - [ ] Return paths remain local/allowlisted and raw exception/provider content never reaches the query string.
  - **Effort:** L
  - **Dependencies:** 1.3, 4.1.

- [ ] **4.3 Complete cookie sign-in and canonical-host tenant handoff**
  - **Files:** AtprotoTenantSessionHandoffStore.cs (new); BffAuthEndpoints.cs; ExploreBffCookieSessionHandler.cs; CircuitAccessTokenService.cs; AtprotoTenantHandoffTests.cs (new).
  - **Acceptance:**
    - [ ] Same-host callback signs in directly; cross-host callback uses one-time opaque handoff.
    - [ ] Handoff is origin/tenant/expiry bound and rejects replay or host substitution.
    - [ ] No JWT or PDS credential appears in URLs, browser storage, WASM auth state, or response bodies.
    - [ ] Cookie HTTPS, SameSite, antiforgery, and existing BFF token-forwarding behavior remain intact.
  - **Effort:** L
  - **Dependencies:** 4.2.

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet

## Phase 5: Session Refresh, Revocation, Readiness, And Operations — NOT STARTED

- [ ] **5.1 Refresh PDS and first-party sessions coherently**
  - **Files:** RefreshAtprotoSession command/handler (new); AtprotoOAuthSecurityGateway.cs; AtprotoSessionController.cs; BffSessionRefreshService.cs; RefreshAtprotoSessionCommandHandlerTests.cs (new).
  - **Acceptance:**
    - [ ] Only the authenticated user's tenant/DID session can refresh.
    - [ ] Rotated OAuthSessionData is durably stored before the new platform JWT is returned.
    - [ ] Missing/corrupt/revoked PDS session fails as reauthentication, not an infinite retry.
    - [ ] Concurrent refresh has one authoritative persisted result and does not regress token rotation.
    - [ ] Existing Keycloak refresh tests/behavior are preserved.
  - **Effort:** L
  - **Dependencies:** Phase 4.

- [ ] **5.2 Revoke remotely and clear locally on sign-out**
  - **Files:** RevokeAtprotoSession command/handler (new); AtprotoSessionController.cs; BffAuthEndpoints.cs; RevokeAtprotoSessionCommandHandlerTests.cs (new).
  - **Acceptance:**
    - [ ] Remote success and already-revoked cases delete the local durable session.
    - [ ] Remote outage is logged/metriced without exposing tokens and never prevents cookie deletion.
    - [ ] Cross-user/cross-tenant revoke is rejected.
    - [ ] Repeat signout is safe and returns the existing local signout behavior.
  - **Effort:** M
  - **Dependencies:** 5.1.

- [ ] **5.3 Make provider readiness and telemetry truthful**
  - **Files:** BffProviderReadinessService.cs; AtprotoAuthenticationHealthCheck.cs (new); AtprotoAuthenticationMetrics.cs (new); API Program.cs; docs/CONFIGURATION.md; docs/SECRETS.md; docs/SELF_HOSTING.md; docs/TROUBLESHOOTING.md; AtprotoObservabilityPolicyTests.cs (new).
  - **Acceptance:**
    - [ ] Disabled provider is omitted; misconfigured provider is unavailable with a safe reason.
    - [ ] Metrics have bounded labels and no full DID, handle, URL query, token, JWK, or exception body.
    - [ ] Health checks do not perform per-probe live PDS login or leak configuration values.
    - [ ] Operator docs cover key rotation overlap, session invalidation, cache loss, PDS outage, and recovery.
  - **Effort:** M
  - **Dependencies:** 5.1, 5.2.

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet

## Phase 6: Public Contract Cleanup And Safe Client Surface — NOT STARTED

- [ ] **6.1 Remove secret-bearing generic token mutation contracts**
  - **Files:** UserAuthenticationTokenController.cs; Create/Update token DTOs and validators (delete); Create/Update token commands and handlers (delete); AtprotoRecordController.cs direct mutation actions; Create/Update AtprotoRecord DTOs/commands/handlers/serializer roots/generated methods/mutation HAL links (delete); token/AtprotoRecord privacy and route-absence tests (modify).
  - **Acceptance:**
    - [ ] OpenAPI has no generic raw-token create/update operation.
    - [ ] Safe DTOs expose only ID, provider, PDS host, and expiry.
    - [ ] Delete/revoke remains authorized, self/tenant scoped, and idempotent.
    - [ ] No compatibility route, command, DTO, mapper, serializer entry, or test remains.
    - [ ] Public OpenAPI, HAL, serializers, and generated clients contain no direct `AtprotoRecord` create/update/delete authority; only lifecycle outboxes and canonical ingress write records.
  - **Effort:** M
  - **Dependencies:** Phase 5.

- [ ] **6.2 Regenerate clients and align safe account-session UX/docs**
  - **Files:** EventApiClient.g.cs; AppJsonSerializerContext.cs; LoginRedirect.razor; LoginRedirectAtprotoTests.cs (new); AtprotoCredentialIsolationTests.cs (new); docs/API_CHANGELOG.md; docs/FEDERATION.md; docs/AUTHORIZATION.md.
  - **Acceptance:**
    - [ ] Generated client/JSON context contains no deleted credential types or bridge session material.
    - [ ] The server-private bridge and removed direct `AtprotoRecord` mutations are absent from browser OpenAPI/client/serializer surfaces.
    - [ ] Login handle label, validation, focus, keyboard submission, and error announcement remain accessible.
    - [ ] UI never gates per-resource actions from roles/claims.
    - [ ] API_CHANGELOG records removed endpoints and new bridge/refresh/revoke operations.
    - [ ] FEDERATION distinguishes implemented OAuth authentication from the still-pending event/RSVP phases in this workstream.
  - **Effort:** M
  - **Dependencies:** 6.1.

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet

## Phase 7: ATProto Events Governance And Validation Profiles — VERIFIED COMPLETE

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
- [x] dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet

## Phase 8: Canonical Publication Snapshot And Exhaustive Description — NOT STARTED

- [ ] **8.1 Load and build the canonical public/federatable event snapshot**
  - **Files:** IEventRepository.cs; EventRepository.cs; AtprotoEventPublicationSnapshot.cs (new); AtprotoEventPublicationSnapshotFactory.cs (new); snapshot factory tests (new).
  - **Acceptance:**
    - [ ] Tenant-filtered entity loading covers all public scalars, every session/day/agenda/location/room, actors/groups/organizations, categories/tags, lookups, aspects, speakers/languages, and event/session EAV values without N+1.
    - [ ] Event/session locations are projected only from `EventLocationDisclosureEvaluator` results for `EventLocationDisclosurePurpose.Public`; private-home, delayed, and erased address canaries are absent.
    - [ ] Application maps the entity graph to one immutable snapshot; repositories still return entities.
    - [ ] Soft-deleted/private/internal data is excluded explicitly.
    - [ ] Attendee/private registration data, moderation/report evidence, audit/concurrency/soft-delete internals, secrets, and internal IDs never enter the snapshot.
  - **Effort:** XL
  - **Dependencies:** 7.2.

- [ ] **8.2 Map the community record and render every additional field**
  - **Files:** Infrastructure csproj; existing community event/RSVP lexicons; AtprotoCalendarEventRecordData.cs and AtprotoCalendarRsvpRecordData.cs (new); event/RSVP mappers, validators, independently maintained source-field manifests; description formatter tests (new).
  - **Acceptance:**
    - [ ] Native name/description/createdAt/startsAt/endsAt/mode/status/locations/uris/rsvpExpected fields are mapped when available.
    - [ ] One deterministic description contains base content and every non-native public field, including all sessions, EAVs, aspects, resolved lookups, days, agenda, locations, registration, pricing, categories, and tags.
    - [ ] Independently maintained event and RSVP source-field manifests fail when any source field is neither native, rendered, nor explicitly privacy-excluded; manifests are not derived from mapper output.
    - [ ] Typed RSVP projection maps a successfully committed active `EventRegistrationIntent`/registration lifecycle only to `community.lexicon.calendar.rsvp#going` plus settled event URI/CID; organizer `ApprovalStatus`, attendee identity/answers, and private registration data are excluded. User cancellation/deletion plans remote delete; `interested`/`notgoing` are not emitted.
    - [ ] Stable ordering/display formatting is byte-deterministic; no raw-ID-only lookup output or raw EF/HTML dump.
    - [ ] Invalid shape, unsafe value, coverage gap, or encoded-size overflow returns permanent no-PDS; never truncate.
  - **Effort:** XL
  - **Dependencies:** 8.1.

### Phase 8 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet

## Phase 9: Transactional Outbound Event And RSVP Publication — NOT STARTED

- [ ] **9.1 Record ADR-015 and harden federation persistence**
  - **Files:** ADR-015 (new); AtprotoRecord.cs/config; PdsSyncOutbox.cs/config/repository/contract; generated migration/snapshot; schemas/islamu-event.md.
  - **Acceptance:**
    - [ ] ADR/schema define outbound tenant/user ownership, global canonical inbound ownership, tenant presentation/visibility joins, direction/provenance, DID/collection/rkey identity, source entity/version, immutable payload/hash, stable idempotency, CID expectations, URI/CID settlement, cursor/checkpoint policy, and user consent.
    - [ ] Unique constraints prevent duplicate record identity/logical operation while allowing later aggregate versions.
    - [ ] Claims have owner/expiry and crashed Processing leases are reclaimable.
    - [ ] Completion settles AtprotoRecord URI/CID and outbox status in one transaction; no result is discarded.
    - [ ] Existing Event/EventRegistration FKs and event-before-RSVP dependency are explicitly reconciled.
    - [ ] One leased multi-node consumer owns the global canonical cursor/materialization; no per-tenant socket or duplicate inbound record ownership exists.
  - **Effort:** XL
  - **Dependencies:** Phase 8.

- [ ] **9.2 Enqueue event publication only from successful local lifecycle transitions**
  - **Files:** AtprotoEventPublicationPlanner.cs (new); Create/Publish/Update/Cancel/Delete/HeavyRedact Event handlers; lifecycle outbox tests (new).
  - **Acceptance:**
    - [ ] Draft create, local readiness failure, capability/consent/link/session failure, mapping failure, and size overflow create no PDS row and make no network call.
    - [ ] Local create-as-published/PublishEvent and immutable create outbox commit or roll back together inside IUnitOfWork.
    - [ ] Stable rkey/idempotency values are allocated outside retryable delegate or otherwise deterministic across execution-strategy retry.
    - [ ] Update/cancel/delete/redact target only an existing outbound AtprotoRecord and never synthesize a remote create.
    - [ ] Lexicon/projection failure leaves valid local publication authoritative and exposes a bounded federation status.
  - **Effort:** XL
  - **Dependencies:** 9.1.

- [ ] **9.3 Deliver event records, settle URI/CID, then publish RSVP strongRefs**
  - **Files:** IPdsService.cs; PdsService.cs; PdsSyncWorker.cs; Create/Update/Delete EventRegistration handlers; settlement integration tests (new).
  - **Acceptance:**
    - [ ] Worker restores only the owner DID's CarpaNet OAuth session and never processes an uncommitted/disabled/cross-tenant row.
    - [ ] After claim and immediately before remote I/O, worker rechecks effective capability, current self-consent, and `EventLocationDisclosurePurpose.Public`; stale work cannot bypass revocation or privacy changes.
    - [ ] Stable-record-key retry/reconciliation prevents duplicate event creation after remote success and settlement crash.
    - [ ] Event URI/CID settles before an RSVP strongRef becomes claimable; missing CID defers safely.
    - [ ] Active registration emits only `#going`; organizer approval changes never map to RSVP intent; user cancellation/deletion deletes the existing remote RSVP; `interested`/`notgoing` remain unsupported until a real local user-intent model exists.
    - [ ] Remote failure never rolls back/deletes the application event and is retry/dead-letter observable without secret/provider-body leakage.
    - [ ] Revoked consent/disabled capability stops eligible unclaimed delivery according to ADR-015 without silently deleting completed remote records.
  - **Effort:** XL
  - **Dependencies:** 9.2 and Phase 5.

### Phase 9 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet

## Phase 10: Filtered Inbound Jetstream Federation — NOT STARTED

- [ ] **10.1 Implement capability-aware Jetstream ingestion and tombstones**
  - **Files:** Directory.Packages.props; Infrastructure csproj; AtprotoJetstreamSubscriber.cs (new); IAtprotoRecordRepository.cs; AtprotoRecordRepository.cs; API Program.cs; subscriber tests (new).
  - **Acceptance:**
    - [ ] One leased multi-node CarpaNet.Jetstream consumer uses WantedCollections containing exactly community event and RSVP; durable global cursor is long microseconds.
    - [ ] Disabled capability performs no new materialization; enable/resume uses the last safe cursor.
    - [ ] Bounded lexicon/size validation and curated allowlist reject/quarantine unsupported records.
    - [ ] Replay is idempotent, locally-owned URI/CID records do not duplicate, and delete/tombstone purges/suppresses dependent RSVP state.
    - [ ] Inbound DID/collection/rkey versions are globally canonical; tenant presentation/visibility is separate and tenants never own duplicate rows or sockets.
    - [ ] Reconnect/backoff/cancellation and metrics are bounded with no high-cardinality DID/rkey/payload labels.
  - **Effort:** XL
  - **Dependencies:** 9.1, 9.3.

### Phase 10 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet

## Phase 11: Tenant-Gated Federation API And HAL — NOT STARTED

- [ ] **11.1 Extend home/event-list queries and HAL for federated events**
  - **Files:** GetHomeDiscoveryQueryHandler.cs; HomeDiscoveryDto.cs; GetEventListRequestHandler.cs; PublicExperienceController.cs; EventController.cs; EventLinkPolicy.cs; RouteNames.cs; API federation presentation tests (new).
  - **Acceptance:**
    - [ ] Disabled tenants receive no inbound items or federation actions; enabled tenants receive only allowed valid non-tombstoned records.
    - [ ] Locally-owned Jetstream records de-duplicate to the local event with federation metadata.
    - [ ] Provenance/source metadata and external URLs are bounded/safe; pagination/sort/cache semantics remain stable.
    - [ ] HAL relations are the sole source of action authority; GET remains anonymous and writes authorized.
    - [ ] OpenAPI, endpoint classification, rate limit, ProblemDetails, response metadata, and route names are explicit.
  - **Effort:** L
  - **Dependencies:** Phase 10.

### Phase 11 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet

## Phase 12: Administrator, User-Consent, And Event Client Surfaces — NOT STARTED

- [ ] **12.1 Add instance, tenant, and user federation controls**
  - **Files:** InstanceGovernanceSection.razor; TenantPoliciesSection.razor; UserProfile.razor/.cs; generated client/JSON context; federation settings tests (new).
  - **Acceptance:**
    - [ ] Instance admin sets defaults/locks; tenant admin edits only when server metadata allows; user edits only personal consent.
    - [ ] Copy explains one switch enables fetch plus eligible publication, and community mode reduces required business fields without reducing safety validation.
    - [ ] Locked controls expose an accessible server-provided reason and forged client state cannot bypass API policy.
    - [ ] No token/private key/private payload enters browser contracts or telemetry.
    - [ ] Labels, keyboard/focus, validation, and error announcements meet repository accessibility rules.
  - **Effort:** L
  - **Dependencies:** 7.1, 7.2, 11.1.

- [ ] **12.2 Render federated events and delivery status from HAL**
  - **Files:** HomeDiscoveryExperience.razor; UpcomingEventList.razor; EventList.razor/.cs; federated rendering tests (new).
  - **Acceptance:**
    - [ ] Federated provenance/status is understandable without color alone and links have descriptive names.
    - [ ] Source/RSVP/retry/sync actions render only from HAL; no role/claim/source-type inference.
    - [ ] Stable failure codes map to guidance; raw outbox/provider errors are never displayed.
    - [ ] Disabled tenants render no stale federated cards after API/cache refresh; local-only event rendering is unchanged.
  - **Effort:** M
  - **Dependencies:** 11.1, 12.1.

### Phase 12 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] dotnet build --configuration Release --verbosity quiet
- [ ] dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet

## Remaining / Deferred Work

### New ATProto account onboarding — NEEDS PRODUCT DECISION

- **Reason:** Current User creation requires email; ATProto provides no verified email; SyncUser safely requires an existing link.
- **Default:** Linked-account sign-in only.
- **Trigger:** Explicit approval for account-linking UX or an email-less user domain change.
- **Forbidden shortcut:** Synthetic email, unverified email matching, or silent implicit linking.

### Report release matrix — OUTSIDE PHASE GATES

- Bluesky, Eurosky, and self-hosted PDS live-provider evidence remains a release activity outside this plan's non-browser automated phase gates.
- Do not add browser/manual/Aspire/live-PDS tasks to these phases.
