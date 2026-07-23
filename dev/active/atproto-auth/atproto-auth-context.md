<!-- ABOUTME: Hot handoff context for the AT Protocol OAuth and event-federation implementation workstream. -->
<!-- ABOUTME: Captures DB-first publication, exhaustive event projection, governed validation, ingress, blockers, and next work. -->

# AT Protocol Integration — Context

Last Updated: 2026-07-23 Europe/Brussels

## SESSION PROGRESS (2026-07-23 Europe/Brussels)

### COMPLETED

- Classified the composite work against the repository intent catalog.
- Read all required repository docs, path rules, selected skills, the complete ATProto report, current auth/federation code, existing tests, configuration/secrets paths, and overlapping workstreams.
- Verified CarpaNet from both local documentation/source at /home/amir/dev/Github/CarpaNet and Context7 package /drasticactions/carpanet.
- Confirmed the existing Release build is green: 0 errors, 326 existing warnings.
- Created the repository-grounded six-phase OAuth plan and synchronized task checklist.
- Re-baselined the workstream to twelve phases after the user's federation clarification.
- Re-baselined the workstream to fifteen phases after the user's event backfilling, PDS-decoupled dual identity, and tenant-local inbound-import requirements.
- Verified both local publication seams use IUnitOfWork and that current EventPublish readiness is stricter than the community lexicon's required `name` and `createdAt`.
- Verified the full event graph spans sessions, days, agenda, locations/rooms, lookups, aspects, categories/tags, speakers/languages, and event/session custom-property EAV values.
- Re-queried Context7 /drasticactions/carpanet for RestoreSessionAsync, repository record operations, and Jetstream WantedCollections/cursor/commit behavior.
- Replaced the former F0 blocker with executable governance, projection, outbox, Jetstream, API/HAL, and UI phases. ADR-015 is now Task 9.1.
- Before the 2026-07-23 clarification, all 32/32 original product tasks were implemented and independently verified; the added Phase 15 task is now also complete.
- Captured a fresh protected dirty-tree fingerprint at HEAD `aefa7797054c58a1233267835417aea46830b050` and confirmed the exact Release baseline: 25 projects, 0 errors, 0 warnings.
- Reconciled architecture corrections: no direct `AtprotoRecord` mutations, a server-private bridge, the existing `IsLockable` engine, typed RSVP projection, public-location disclosure, independent source-field manifests, global canonical inbound ownership, capability-bound community readiness, and last-moment delivery gate rechecks.
- Completed and independently verified Task 1.1: stable CarpaNet 1.0.2 packages are centrally pinned with NuGet-generated lock files, BFF/Infrastructure ownership is enforced, and the exact eight-file authoritative local lexicon closure compiles without network resolution. Evidence is in `.omo/evidence/atproto-auth/task-2/README.md`.
- Completed and independently verified Task 1.2: ADR-014, canonical OAuth client metadata/JWKS publication, a strict rotation-capable ES256 key provider, typed BFF options/Infisical mapping, and three direct instance-only secret definitions are implemented. Implementation evidence is in `.omo/evidence/atproto-auth/task-3a/`; independent confirmation is in `.omo/evidence/atproto-auth/task-3a-verifier/`.
- Task 1.2 security rework now rejects ambiguous/non-local callback paths, credential-bearing or non-canonical public origins, and non-canonical base64url JWK values; public JWKS and browser contracts remain private-material free.
- Completed and independently scoped-confirmed Task 1.3: one package-free shared transport now enforces canonical metadata, strict OAuth forms, fresh issuer-bound assertions, mandatory response nonces, DNS-pinned public egress, bounded responses, and fail-closed readiness across BFF and Infrastructure. Evidence is in `.omo/evidence/atproto-auth/task-3/README.md`.
- Completed the remaining OAuth/session, governed event federation, exhaustive projection, transactional outbound delivery, filtered Jetstream ingress, tenant-gated discovery/HAL, and administrator/user/client surfaces. Independent evidence for the final API slice is in `.omo/evidence/atproto-auth/task-13/README.md`.
- Added tenant-scoped current PDS delivery state to My Events, stable recovery guidance without raw provider text, and transactional settlement link-back from the canonical ATProto record to the committed local Event.
- The administrator components now use `IAtprotoFederationSettingsService` instead of directly injecting the generated API client; the shared test context supplies the typed service boundary. The complete Blazor client suite is green: 1,728 passed and one pre-existing explicit skip.
- Repaired the final ATProto architecture gaps: immutable atomic-cache snapshots, truthful 3xx OpenAPI success metadata, CQRS/repository naming alignment, explicitly BFF-private bridge models, and typed UI service ownership. The architecture suite improved from 9 failures to 2 unrelated baseline failures.
- Fixed the reported compilation diagnostics by removing obsolete raw `AtprotoRecordDto` serializer roots, removing the obsolete `PermissionAction` attribute constructor, and matching the `ISecureRequest.ResourceAttributes` nullability contract.
- Reconciled federation, API, configuration, self-hosting, troubleshooting, project, architecture, outbox, and operations documentation with the implemented governance, DB-first delivery, exhaustive one-description projection, Jetstream ingestion, safe HAL, client status, and recovery contracts.
- Regenerated `docs/API_CONTRACT_INVENTORY.md` from the current canonical OpenAPI schema; the obsolete raw `/api/atprotorecord` surface is absent and the typed federated-event source redirect is recorded.
- Completed governed recovery settings and administrator surfaces through the existing five-tier setting resolver, API/HAL authority, and locked seed definitions. Evidence: `.omo/evidence/atproto-auth/task-16/`.
- Completed in-place Jetstream DID-filter updates on one globally leased socket with normalized/coalesced updates, exact event/RSVP collections, durable-cursor reconnect, and bounded telemetry. Evidence: `.omo/evidence/atproto-auth/task-17/`.
- Completed bounded, signed PDS snapshot recovery through the canonical inbound record/presentation pipeline. Real PostgreSQL reconciliation passed 4/4 twice under the former read-model-only rule; Phase 15 now extends that same transaction to Event/EventSession import. Evidence: `.omo/evidence/atproto-auth/task-18/pds-recovery.md`.
- Completed atomic encrypted OAuth refresh persistence through the shared exact-scope PostgreSQL advisory lock and repository-backed CarpaNet session store. Real PostgreSQL refresh/store coverage passed 6/6 and the lock pair repeated 2/2. Evidence: `.omo/evidence/atproto-auth/task-19/`.
- Completed universal PDS and authorization-server discovery. Provider-neutral tests cover distinct PDS/issuer origins, `did:plc`, hostname-only `did:web`, strict single-service validation, deterministic cache remapping, and token substitution rejection. Evidence: `.omo/evidence/atproto-auth/task-20/`.
- Completed tenant-local inbound Event/EventSession materialization through one validated internal CQRS mapping shared by Jetstream and PDS recovery. Real PostgreSQL proved 22/22 cases twice, including exact-one concurrency, stable replay/update IDs, Unicode-safe description summary, source EventUrl/createdAt, tenant-scoped absence/tombstone handling, commit-fence rollback, and zero outbound echo. Final independent evidence: `.omo/evidence/atproto-auth/task21/final-adversarial-verify.md`.

### IN PROGRESS

- Todo 22 canonical full-project, generated-contract, migration, and deterministic integration verification.

### NEXT

1. Freeze one attributable ATProto-relevant source snapshot after concurrent dynamic-event UI writes settle.
2. Run Todo 22's Release build, all nine per-project commands, locked/generated contract and migration checks, and deterministic fake-service/Testcontainers smoke.
3. Complete F1-F4 against the verified snapshot.

### BLOCKERS

- No environment blocker remains. Docker/Testcontainers is available and supplied the required real PostgreSQL evidence for Todos 18 and 19.
- The shared worktree is still receiving unrelated dynamic-event-management UI changes. Phase 15 must avoid those paths, and Todo 22 must run on a frozen attributable snapshot without editing or absorbing that workstream.
- The 2026-07-19 broad matrix below is historical, not a current failure list. Its former shared-tree failures must be re-evaluated by Todo 22 rather than assumed fixed or still failing.

### ACCEPTED PRODUCT CONSTRAINT

- Linked-account-only ATProto sign-in is accepted by execution approval. Unlinked identities remain rejected; no synthetic email, implicit user creation, or email auto-match is added.

## Quick Resume

1. Read this context and atproto-auth-tasks.md.
2. Read only the current phase, constraints, or changed decisions from atproto-auth-plan.md.
3. Start from Todo 22, the first unchecked top-level ATProto execution-plan gate.
4. Keep tasks current during implementation. Update context/plan only at their defined triggers.
5. Preserve ADR-015's DB-first, one-capability, exhaustive-description, consent, canonical-ingress, exact two-collection, and atomic tenant-local import invariants through final verification.

## Current Status Snapshot

| Field | Value |
|---|---|
| Overall status | 33/33 implementation tasks complete; 21/26 execution-plan gates complete; canonical verification and F1-F4 remain |
| Completed implementation tasks | 33/33 completed tasks |
| Current priority | Todo 22 canonical verification and deterministic integration smoke |
| Next executable slice | Run Todo 22, then F1-F4 |
| OAuth implementation | Complete in Phases 1-6; live-provider release evidence remains outside automated gates |
| Federation implementation | Phases 7-15 complete, including tenant-local inbound aggregate import |
| Baseline build | Fresh green baseline at HEAD aefa7797 on 2026-07-18; 25 projects, 0 errors, 0 warnings |

## Current Focused Verification Addendum — 2026-07-22

- Recovery settings: Application 17/17, Infrastructure 13/13, API 8/8, Architecture 9/9, PostgreSQL seed 1/1, and bUnit administration surfaces 25/25 combined.
- Dynamic Jetstream filtering: subscriber/readiness/runtime-store 30/30, repeated across six runs for the affected subscriber matrix.
- Bounded PDS snapshot reconciliation: real PostgreSQL 4/4 twice; focused handler/gateway/runtime/subscriber and architecture suites are green.
- Atomic encrypted OAuth refresh: real PostgreSQL 6/6 with lock pair repeated 2/2; security gateway 12/12, encrypted store 6/6, writer 12/12, and architecture boundaries are green.
- Universal discovery: auth flow 17/17, session binding 6/6, cache 2/2 twice, transport 23/23, linked-account handler 5/5, architecture 29/29, and bounded 10,000-entry cache/cross-lease probes.

These results close implementation Tasks 13.1-14.1. They do not replace Todo 22's canonical build and all-nine-project matrix.

## Historical Canonical Verification Matrix — 2026-07-19

This is the last complete per-project matrix recorded by the ATProto workstream. It is not an all-green or release-readiness claim. A fresh Todo 15 Release build on the current shared tree also exited 1 with five direct unrelated test-source compile errors: one missing `TryClaimOccurrenceAsync` cancellation argument, two ambiguous `HybridCache.RemoveAsync` calls, and the two required-member fixture errors below; fourteen downstream `CS0006` errors were compilation fallout.

| Command | Result |
|---|---|
| `dotnet build --configuration Release --verbosity quiet` | At the frozen canonical snapshot, blocked by two unrelated `CS9035` errors in `NotificationFanoutOccurrenceRepositoryTests.cs:789`; the fresh Todo 15 build found the additional current errors summarized above. |
| Event.Domain.UnitTests | Passed. |
| Event.Application.UnitTests | 2,734 passed, 2 unrelated failures, 2 skipped. |
| Event.Architecture.Tests | 255 passed, 2 unrelated failures, 1 skipped; all ATProto-owned failures are green. |
| Explore.Secrets.UnitTests | Passed. |
| Explore.Infrastructure.Tests `Category!=Runtime` | Passed. |
| Event.Persistence.IntegrationTests | Compile-blocked by the same unrelated notification fixture. |
| Event.API.IntegrationTests | Broad command indeterminate at the process boundary; focused ATProto API suites passed. |
| Explore.Blazor.IntegrationTests | Passed. |
| Explore.Blazor.Client.Tests | 1,728 passed, one pre-existing explicit skip. |

## Evidence Sources Read

### Repository governance

- AGENTS.md
- /home/amir/.codex/RTK.md
- .github/copilot-instructions.md
- .claude/contract/intents.yaml
- .claude/rules/application-layer.md
- .claude/rules/api-controllers.md
- .claude/rules/api-hateoas.md
- .claude/rules/blazor-server.md
- .claude/rules/blazor-client.md
- .claude/rules/domain-layer.md
- .claude/rules/efcore-persistence.md
- .claude/rules/efcore-migrations.md
- .claude/rules/tests.md

### Repository docs

- docs/QUICK_REFERENCE.md
- docs/GOVERNANCE.md
- docs/ARCHITECTURE.md
- docs/AUTHORIZATION.md
- docs/SECURITY-MODEL.md
- docs/API.md
- docs/BLAZOR.md
- docs/DOMAIN.md
- docs/FEDERATION.md
- docs/LEXICONS.md
- docs/OUTBOX_PATTERN.md
- docs/CONFIGURATION.md
- docs/SECRETS.md
- docs/OPERATIONS.md
- docs/TESTING.md
- docs/DOCUMENTATION_STYLE_GUIDE.md
- docs/CODEBASE_STRUCTURE.md
- docs/CODEBASE_INSIGHTS.md
- docs/DESIGN_SYSTEM.md
- docs/ACCESSIBILITY.md
- dev/active/README.md

### Selected skills

- implementation-plan and all directly referenced resources
- agentic-research and directly referenced resources
- clean-architecture-rules
- auth-patterns
- blazor-bff-patterns
- cqrs-mediatr-guidelines
- dotnet-efcore-guidelines
- outbox-pattern
- error-tracking
- blazor-ui-conventions
- lsp

### ATProto and CarpaNet

- dev/report/atproto-report.md, complete file
- /home/amir/dev/Github/CarpaNet/docs/docs, complete documentation set
- Relevant CarpaNet OAuth/store/client/source-generator source
- Context7 package /drasticactions/carpanet:
  - OAuthClientConfig
  - OAuthSession.AuthorizeAsync
  - OAuthSession.CallbackAsync
  - OAuthSession.RestoreSessionAsync
  - IOAuthSessionStore
  - IOAuthStateStore
  - SignOutAsync
  - LexiconFiles
  - Jetstream scope

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| dev/report/atproto-report.md | Existing | Architecture | Primary requested source | Part A detailed; Part B body absent. |
| Directory.Packages.props | Existing | Build | Central package versions | Pin CarpaNet exactly. |
| schemas/lexicons/com.atproto.server.getSession.json | New | Schema | Hermetic getSession binding | No network LexiconResolve. |
| src/Explore.Blazor/Authentication/AtprotoAuthenticationHandler.cs | Existing stub | BFF | Challenge seam | Replace 501; preserve scheme. |
| src/Explore.Blazor/Authentication/AtprotoAuthenticationOptions.cs | Existing/implemented | BFF | Canonical public URL, callback, scope, and private-key-ring options | Task 1.2 complete; Task 1.3 consumes the key binding. |
| src/Explore.Blazor/Extensions/BffAuthEndpoints.cs | Existing | BFF | Challenge/callback/signout/refresh endpoints | Add callback and safe errors. |
| src/Explore.Blazor/Services/DynamicAuthSchemeManager.cs | Existing | BFF | Runtime scheme registration | Bind real ATProto options. |
| src/Explore.Blazor/Services/Auth/CacheBackedOAuthStateStore.cs | New | BFF | CarpaNet state persistence | Redis atomic consume; dev memory only. |
| src/Explore.Blazor/Services/Auth/ApiBackedOAuthSessionStore.cs | New | BFF | CarpaNet session adapter | Uses a dedicated server-private API bridge client; never enters generated WASM contracts. |
| src/Explore.Blazor/Services/Auth/AtprotoOAuthFlowContext.cs | New | BFF | Callback-scoped expected DID/tenant | Populated during state consume. |
| src/Explore.Blazor/Services/Auth/AtprotoTenantSessionHandoffStore.cs | New | BFF | Cross-host cookie handoff | Opaque code only; atomic consume. |
| src/Explore.Blazor/Services/Auth/AtprotoClientKeyProvider.cs | New/implemented | BFF | Strict rotation-capable OAuth client ES256 key provider | Public-only deterministic JWKS plus disposable private signer copies by kid. |
| src/Explore.Blazor/Services/Auth/BffProviderReadinessService.cs | Existing | BFF | Provider availability | Currently reports ATProto ready too eagerly. |
| src/Explore.API/Controllers/AtprotoSessionController.cs | New | API | Establish/refresh/revoke boundary | Explicit auth schemes and rate limits. |
| src/Explore.API/Extensions/AuthenticationExtensions.cs | Existing | API | MultiAuth registration/selector | Add Bootstrap and Session validators. |
| src/Explore.Application/Features/Authentication/Atproto/ | New | Application | CQRS establish/refresh/revoke use cases | Manual validators, public SyncUser request only. |
| src/Explore.Application/Contracts/Infrastructure/IAtprotoOAuthSecurityGateway.cs | New/implemented | Application | External OAuth verification/crypto boundary | No Carpa types leak into Application. |
| src/Explore.Infrastructure/Services/Federation/AtprotoOAuthSecurityGateway.cs | New | Infrastructure | CarpaNet verify/restore/JWT operations | Uses constrained transport. |
| src/Explore.Infrastructure/Services/Federation/RepositoryBackedOAuthSessionStore.cs | New | Infrastructure | Durable CarpaNet store | Same table as BFF API adapter. |
| src/Explore.Infrastructure/Services/Federation/AtprotoSessionEnvelopeProtector.cs | New | Infrastructure | AES-GCM OAuthSessionData envelope | Key ring via Explore.Secrets. |
| src/Explore.Domain/UserAuthenticationToken.cs | Existing | Domain | Durable tenant user session metadata | Replace plaintext fields with DID/ciphertext/kid. |
| src/Explore.Persistence/Repositories/UserAuthenticationTokenRepository.cs | Existing | Persistence | Tenant/DID session queries | Repositories return entities. |
| src/Explore.Persistence/Configurations/Entities/UserAuthenticationTokenConfiguration.cs | Existing | Persistence | Session schema/indexes | Add tenant/provider/DID unique index. |
| src/Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs | Existing/overlap | Domain | Canonical secret keys | Coordinate with secrets workstream. |
| src/Explore.Domain/Secrets/SecretDefinitionRegistry.cs | Existing/overlap | Domain | Allowed secret definitions | Coordinate with secrets workstream. |
| src/Explore.Blazor.Client/Clients/EventApiClient.g.cs | Existing/generated | Blazor Client | API client surface | Must contain no secret-bearing DTOs. |
| src/Explore.Blazor.Client/Pages/LoginRedirect.razor | Existing | Blazor Client | Accessible handle input | Reuse; only stable error mapping. |
| docs/adr/ADR-014-atproto-session-trust-bridge.md | New/implemented | Docs | Trust/key/library decision | Accepted in Task 1.2; records A1-A7 and rejects anonymous/BFF-trusted bridge identity. |
| src/Explore.Domain/Constants/GovernanceSettingKeys.cs | Existing | Domain | Canonical ATProto Events capability/profile/consent keys | Capability/profile reuse `IsLockable` and existing lock commands; no duplicate lock keys. |
| src/Explore.Application/Services/Lifecycle/EventLifecyclePolicyProvider.cs | Existing | Application | Platform and community publication required-field policies | Community relaxes business requirements, never safety invariants. |
| src/Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs | Existing | Application | Direct create-as-published transaction seam | Local graph plus PDS outbox must commit atomically; no network call. |
| src/Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs | Existing | Application | Draft-to-published transaction seam | Local status plus PDS outbox must commit atomically; no network call. |
| src/Explore.Application/Features/Federation/Atproto/Models/AtprotoEventPublicationSnapshot.cs | New | Application | Exhaustive public/federatable event graph contract | Every field covered natively, in description, or privacy-excluded. |
| src/Explore.Application/Features/Federation/Atproto/Services/AtprotoEventDescriptionFormatter.cs | New | Application | Deterministic readable description builder | Includes every session, EAV, aspect, and resolved lookup in one record. |
| src/Explore.Application/Features/Federation/Atproto/Models/AtprotoCalendarRsvpRecordData.cs | New | Application | Typed privacy-safe RSVP projection | Maps public status plus settled event URI/CID strongRef inputs. |
| src/Explore.Application/Services/EventLocationDisclosureEvaluator.cs | Existing | Application | Public-location privacy gate | Evaluate `EventLocationDisclosurePurpose.Public` at snapshot and again before delivery. |
| src/Explore.Domain/Federation/PdsSyncOutbox.cs | Existing | Domain | Durable post-commit PDS command | Add ownership, version/idempotency, lease, payload/hash, settlement. |
| src/Explore.Domain/AtprotoRecord.cs | Existing | Domain | Inbound/outbound AT record identity | Add tenant, direction/provenance, correlation, URI/CID state. |
| src/Explore.API/Controllers/AtprotoRecordController.cs | Existing/remove mutations | API | Current direct record bypass | Delete create/update/delete and matching DTO/handler/HAL/generated surfaces; retain governed discovery only. |
| docs/adr/ADR-015-atproto-event-record-and-outbox.md | New | Docs | Federation persistence/order/cursor/settlement decision | Mandatory Task 9.1 before runtime ingress/egress edits. |
| src/Explore.API/BackgroundServices/PdsSyncWorker.cs | Existing | API host | Post-commit outbound delivery | Restore CarpaNet session; stable rkey; settle URI/CID before RSVP. |
| src/Explore.Infrastructure/Services/Federation/AtprotoJetstreamSubscriber.cs | New | Infrastructure | Filtered inbound event/RSVP stream | Exactly two WantedCollections plus long cursor/tombstones. |

## Verified Baseline Behavior Before This Implementation

### BFF

- Cookie authentication is default and YARP forwards only a server-held bearer token.
- ATProto is dynamically registered when enabled, but its handler returns 501.
- LoginRedirect sends login_hint and a safe return path.
- ATProto readiness currently returns true without required key/cache/network evidence.
- BFF Infisical configuration is loaded through ConfigurationExtension; importing Application/Persistence is forbidden.
- An existing BffSelfCallToken proves same-process BFF calls, but it requires an already-authenticated user and cannot solve initial ATProto login.

### API and Application

- MultiAuth chooses API key or Keycloak JWT only.
- SyncUserCommandHandler does not email-auto-match ATProto and rejects an unlinked identity with no email.
- UserAuthenticationToken metadata DTOs are safe, but generic create/update request DTOs carry raw credentials.
- UserAuthenticationTokenController is authorized, consistent with the write-endpoint invariant.
- Existing user-ID resolution order is sub, nameidentifier, then sid and must remain unchanged.

### Persistence and federation scaffolding

- UserAuthenticationToken secret fields are plaintext strings with length 500 and no DID key/unique index.
- IndexedDid and UserExternalLogin provide reusable identity mapping.
- Event and EventRegistration already have optional AtprotoRecord FKs.
- PdsSyncOutbox repository saves on Create, no named lifecycle handler uses it, worker completion discards URI/CID, and raw PdsService is unauthenticated.
- The report summary names eight publish handlers despite saying seven.
- CreateEventCommandHandler and PublishEventCommandHandler already use IUnitOfWork. This is the required seam for atomically committing local publication plus PdsSyncOutbox while keeping remote calls outside the transaction.
- Current EventPublish readiness requires visibility, format, and a scheduled session/first start; the community event lexicon requires only name and createdAt.
- CreateEventRequestValidator requires Title and validates all optional graph values/references when supplied, so the community profile can reduce readiness without accepting malformed optional data.
- PdsSyncOutbox has no tenant/user key, aggregate version/idempotency key, or recoverable lease; Processing can become stuck after a worker crash.
- The vendored community event record has native name/description/createdAt/startsAt/endsAt/mode/status/locations/uris/rsvpExpected only. All other public event graph values require description rendering.

## Key Decisions

| ID | Decision | Consequence |
|---|---|---|
| A1 | CarpaNet/CarpaNet.OAuth only; no FishyFlip or CarpaNet.AspNetCore. | Pin exact versions and keep adapters thin. |
| A2 | Bridge write uses a dedicated signed AtprotoBootstrap scheme. | No AllowAnonymous exception and no BFF user-identity trust. |
| A3 | BFF API-backed and Infrastructure repository-backed IOAuthSessionStore share UserAuthenticationToken. | One durable session authority, no BFF DB reference. |
| A4 | Persist complete OAuthSessionData as one AES-GCM envelope keyed by DID/tenant. | Remove incomplete plaintext secret properties and no dual reader. |
| A5 | Separate auth.atproto.oauth_client_private_jwks, auth.atproto.session_encryption_keyring, and auth.atproto.session_jwt_private_jwks. | Three rotation-capable secret purposes. |
| A6 | Redis atomic consume in multi-node; process-local only for explicit single-node dev. | Readiness fails closed when replay safety is unavailable. |
| A7 | Linked-account-only ATProto sign-in. | No synthetic email, auto-match, or implicit user creation. |
| A8 | One lockable `federation.atproto_events_enabled` capability governs both fetch and eligible outbound publication; default disabled/instance-locked through the existing lock engine. | No split administrator fetch/publish toggles or duplicate lock keys; User-tier consent remains mandatory. |
| A9 | Lockable `federation.atproto_event_validation_profile` is `platform` by default or `community_lexicon`; community is eligible only while capability is enabled. | Existing `IsLockable`/lock commands apply; disabled/unknown capability uses platform readiness and safety invariants never relax. |
| A10 | Local publication and immutable PDS outbox commit in the same transaction; CarpaNet runs only after commit. | No PDS event can precede or survive rollback of the application event. |
| A11 | Typed event and RSVP projections have independently maintained source-field manifests; every non-native event field, including every session/EAV/aspect/lookup, is rendered in one description after public-location disclosure. Active registration maps only to `#going`; organizer `ApprovalStatus` is never user intent. | Coverage/privacy/size failure skips PDS enqueue; cancellation/deletion deletes the remote RSVP; `interested`/`notgoing` await an explicit local intent model. |
| A12 | One leased CarpaNet.Jetstream consumer writes global canonical event/RSVP records with cursor, allowlist, tombstones, and echo prevention; tenant presentation is separate. | No per-tenant sockets/copies; inbound records never invoke outbound lifecycle handlers. |
| A13 | No backward compatibility. | Delete obsolete raw-token writes, split-toggle assumptions, and incomplete federation semantics. |
| A14 | Bridge models are server-private and `AtprotoRecord` has no public mutation surface. | Browser OpenAPI/client/HAL cannot carry bridge secrets or bypass lifecycle/ingress ownership. |

## Constraints And Rules To Remember

- Every touched file must start with two ABOUTME lines.
- Repositories return entities, map in handlers.
- Validators are manually instantiated.
- BFF cannot reference Application/Persistence.
- All write endpoints remain authorized.
- Browser/headers never carry trusted bootstrap authority; server strips/injects privileged assertion.
- PDS/OAuth credentials and private keys never reach WASM, URLs, logs, traces, metrics, public DTOs, or support artifacts.
- Selector parsing chooses a scheme only; complete JWT validation remains mandatory.
- Tenant, DID, state, assertion, session, and handoff invariants are enforced server-side.
- HAL remains the sole UI mutation-affordance source.
- Effective ATProto Events enablement controls both fetch and new outbound enqueue; auth login does not substitute for it.
- User publication consent is self-scoped, defaults false, and remains mandatory even when administrators enable federation.
- Community validation relaxes required event business fields only; internal invariants and every supplied optional value still validate.
- Community validation applies only while the effective ATProto Events capability is enabled; disabled/unknown capability uses platform readiness.
- Request handlers never call a PDS. Local publish plus outbox is atomic; only committed rows may reach CarpaNet.
- One remote event record contains the entire event. All non-native public snapshot fields, including every session/EAV/aspect/lookup, go into description.
- Coverage gaps and record-size overflow prevent PDS enqueue. Never truncate, silently omit, or dump raw entity JSON.
- Event/RSVP source-field manifests are independently maintained; event/session locations must pass `EventLocationDisclosurePurpose.Public` at snapshot and pre-delivery time.
- A successfully committed active `EventRegistrationIntent`/registration lifecycle maps only to `community.lexicon.calendar.rsvp#going`. Organizer `ApprovalStatus` never maps to RSVP intent; user cancellation/deletion deletes the remote RSVP; `interested`/`notgoing` are not emitted without a real local user-intent model.
- Public/federatable completeness never includes attendee/private registration data, moderation/report evidence, audit/concurrency/soft-delete internals, secrets, or internal-only IDs.
- Inbound subscription accepts only the community event and RSVP collections and never echoes through outbound handlers.
- Inbound ownership is global canonical with one leased consumer; tenant visibility/presentation joins are separate and no tenant owns a duplicate socket/record copy.
- Capability and self-consent are rechecked after claim immediately before remote I/O; stale work cannot bypass revocation.
- The bridge is server-private, and public direct `AtprotoRecord` create/update/delete APIs, DTOs, handlers, generated methods, and HAL mutation links are removed.
- No compatibility aliases, dual reads/writes, obsolete routes, or compatibility tests.
- No phase-end browser, Aspire, Docker, live-PDS, or manual validation.
- Preserve unrelated dirty worktree changes.

## Validation Baseline

Fresh attributable baseline observed on 2026-07-18 at HEAD `aefa7797054c58a1233267835417aea46830b050`:

    rtk dotnet build --configuration Release --verbosity quiet

- Result: exit 0.
- Projects: 25 built.
- Errors: 0.
- Warnings: 0.
- Protected dirty-path hashes and the collision negative probe are recorded under `.omo/evidence/atproto-auth/task-1/`.

Per-phase gates:

| Phase | Build | One selected test project |
|---|---|---|
| 1 | Release build | Event.Architecture.Tests |
| 2 | Release build | Event.Persistence.IntegrationTests |
| 3 | Release build | Event.API.IntegrationTests |
| 4 | Release build | Explore.Blazor.IntegrationTests |
| 5 | Release build | Event.Application.UnitTests |
| 6 | Release build | Explore.Blazor.Client.Tests |
| 7 | Release build | Event.Application.UnitTests |
| 8 | Release build | Event.Application.UnitTests |
| 9 | Release build | Event.Persistence.IntegrationTests |
| 10 | Release build | Explore.Infrastructure.Tests |
| 11 | Release build | Event.API.IntegrationTests |
| 12 | Release build | Explore.Blazor.Client.Tests |
| 13 | Release build | Explore.Infrastructure.Tests |
| 14 | Release build | Explore.Blazor.IntegrationTests |

Run each gate once after all phase tasks. Do not repeat a green command unless its inputs changed.

## Current Known Risks / Unknowns

1. **CarpaNet exact version:** Resolved to stable 1.0.2 for core, OAuth, and Jetstream, with NuGet content hashes, local-source commit `a24d54bf6a9ce3bbf7c1961d37ab099abe1d1a65`, and generated bindings recorded in Task 1.1 evidence.
2. **CarpaNet confidential client and transport:** released 1.0.2 does not emit `private_key_jwt`; Task 1.3 owns the discovery-aware scoped-key delegating handler and must prove assertions plus constrained OAuth/PDS egress without changing CarpaNet source.
3. **Callback ordering:** CarpaNet StoreAsync occurs during CallbackAsync. Task 4.1's scoped flow context is required to attach expected DID/tenant before API persistence.
4. **Secret overlap:** Resolved for Task 1.2 with direct instance-only definitions and no legacy compatibility constant; later secret work must preserve the three independent key purposes.
5. **Account onboarding:** Unlinked identities remain rejected until explicit product approval.
6. **Custom domains:** Tenant cookies require opaque one-time handoff; token-in-URL is forbidden.
7. **DB/PDS ordering:** Phase 9 must prove rollback leaves no claimable row and stable rkey reconciliation prevents duplicates after a settlement crash.
8. **Snapshot completeness/privacy:** Every public field must be covered, but private/internal fields must remain excluded. Runtime entity reflection is forbidden.
9. **Record size:** Task 8.2 must verify the encoded limit for the pinned path; overflow is permanent no-PDS, never truncation.
10. **Community profile semantics:** UI/tests must make clear it relaxes only required business fields, not security, ownership, tenant, reference, storage, or supplied-value rules.
11. **Jetstream trust:** Exactly two collections, curated allowlist, bounded parsing, echo prevention, cursor recovery, and tombstones are mandatory.
12. **Missing Part B prose:** User clarification is binding; Task 9.1 records residual choices. Any later report conflict requires re-baselining rather than compatibility branches.
13. **LSP warm-up:** Roslyn initialized but symbol requests timed out. Retry before rename/move operations during implementation.

## Executable Federation Requirements

These are binding requirements assigned to Phases 7-14:

- One effective `federation.atproto_events_enabled` capability for both fetch and outbound availability, with an instance lock and five-tier resolution.
- `platform` versus `community_lexicon` validation profile using existing `IsLockable` state/commands; instance admin owns defaults and unlocked tenant admin may override. Community applies only while capability is enabled.
- Instance capability/profile administration uses the ATProto-only `/api/settings/instance/atproto-federation` read/update/lock/unlock contract and permission-filtered HAL actions; unrelated registry keys are rejected before CQRS dispatch, and no parallel lock engine or administrator consent route exists.
- User-tier publication consent independent of auth.atproto_login_enabled and administrator enablement.
- Primary lifecycle/profile validation first; mapped lexicon validator after governance/consent/link guards; mapped failure skips PDS enqueue and emits a bounded status without undoing valid local publication.
- Local Event publication and PdsSyncOutbox insertion in the same IUnitOfWork transaction; no PDS network call until commit.
- One event record with all sessions and every other non-native public snapshot field rendered into description; independently maintained source-field manifests, public-location disclosure, coverage/size failure, and no truncation.
- CarpaNet RestoreSessionAsync per user, last-moment capability/consent/location recheck, stable record key/retry reconciliation, and typed RSVP `#going` projection only after URI/CID settlement; cancellation/deletion removes the remote RSVP and organizer approval is never treated as intent.
- One leased CarpaNet.Jetstream consumer with WantedCollections restricted to community.lexicon.calendar.event and community.lexicon.calendar.rsvp; inbound records are globally canonical and tenant presentation is separate.
- Curated allowlist moderation, echo de-duplication, tombstone purge, tenant-gated home/event-list display, and HAL-driven affordances.
- ADR-015 for payload, tenant/user ownership, direction/provenance, aggregate-version idempotency, leases, cursor/checkpoint policy, entity correlation, and settlement.

## Handoff Notes

### Phases 13-15 completion handoff — 2026-07-23 Europe/Brussels

- **Current state:** All 33 implementation tasks are complete and independently verified. `.omo/plans/atproto-auth.md` has Todos 1-21 checked; canonical Todo 22 and F1-F4 remain open.
- **Implemented architecture:** Recovery settings reuse the five-tier governance engine; Jetstream updates one globally leased socket in place; Full recovery validates bounded signed PDS repository snapshots into canonical inbound records/presentations; OAuth rotations persist one complete encrypted CarpaNet session under the existing PostgreSQL lock; OAuth discovery follows verified handle/DID/PDS/protected-resource/issuer metadata without provider branches. Validated Jetstream and PDS event plans now atomically materialize one tenant Event and exactly one EventSession without outbound echo.
- **Verification:** Real PostgreSQL recovery passed 4/4 twice, refresh persistence passed 6/6, and inbound aggregate materialization passed 22/22 twice under independent verification. Provider-neutral discovery, cache, transport, session binding, discovery de-duplication, and architecture matrices are green. Exact evidence lives under `.omo/evidence/atproto-auth/task-16/` through `task21/`.
- **Next action:** Execute Todo 22's canonical build, all nine test projects, generated-contract/migration checks, and deterministic integration smoke before F1-F4.
- **Blockers:** No environment blocker. Preserve and exclude the concurrent dynamic-event-management UI workstream when attributing changes or failures.
- **Cleanup:** No ATProto-owned process, test container, PostgreSQL/Ryuk resource, port, browser, PTY, temporary report, or credential remains; Docker itself was left available as user-provided infrastructure.

### Phase 9 implementation handoff — 2026-07-19 Europe/Brussels

- **Current state:** Tasks 9.2-9.3 are implemented but deliberately unchecked until independent verification. Exact red/green history, commands, static scans, attributed files, and the PostgreSQL environment limitation are recorded in `.omo/evidence/atproto-auth/task-11/README.md`.
- **Local-first transaction:** Event create/publish/update/cancel/delete/heavy-redact handlers plan immutable PDS work only after the local lifecycle write and inside the same `IUnitOfWork`. Stable outbox IDs/rkeys/timestamps are allocated before retryable delegates. Direct enabled tests prove local Event and registration writes precede outbox insertion while the transaction delegate is active, with no request-path gateway call.
- **Delivery boundary:** `AtprotoPdsDeliveryProcessor` accepts only an active fenced claim, gates after claim, renews the exact lease, gates immediately before I/O, then delegates to the repository-backed CarpaNet gateway. Success fence-settles URI/CID; bounded permanent/transient failures fence-fail with capped retry and dead-letter semantics.
- **CarpaNet repository writes:** The Infrastructure gateway restores only the exact tenant/user/DID/PDS OAuth session and verifies authenticated DID/PDS binding. The writer uses generic `getRecord`/`putRecord`/`deleteRecord`, a stable rkey, identical-payload reconciliation, and CID compare-and-swap so a crash after remote success does not create a duplicate.
- **RSVP rule:** Only a committed active `EventRegistrationIntent` emits typed `#going`, only with a settled event URI/CID dependency. Approval workflow values are ignored. Cancellation deletes only the existing owned RSVP after the last active intent is gone; missing ownership never synthesizes a record. UpdateEventRegistration intentionally has no publication call.
- **Verification:** Focused Application and Infrastructure suites, Persistence/API builds, the canonical 26-project Release build, and the ATProto architecture boundary suite are green. No Docker/live PDS was used; independent verification should run the PostgreSQL Phase 9 suite when available.
- **Documentation source limitation:** Context7 quota remained exhausted, so exact transport behavior was verified against pinned local CarpaNet 1.0.2 docs/source under `/home/amir/dev/Github/CarpaNet`.

### Phases 5-6 implementation checkpoint — 2026-07-19 Europe/Brussels

- **Current state:** Tasks 5.1-6.2 are implemented but deliberately unchecked until independent verification. Exact commands, trust-boundary proof, static scans, shared-tree limitations, and the Context7 fallback are recorded in `.omo/evidence/atproto-auth/task-8/README.md`.
- **Refresh decision:** Use a PostgreSQL session advisory lock derived deterministically from tenant/user/DID on the existing scoped EF connection. This serializes refresh across nodes without holding an EF execution-strategy transaction across CarpaNet HTTP. CarpaNet's `IOAuthSessionStore` persists rotated `OAuthSessionData` during explicit `DPoPTokenProvider.RefreshAsync`; only after authenticated `getSession` confirms the expected active DID/PDS does the API mint the replacement platform JWT.
- **Revocation decision:** Browser cookie and circuit token are cleared before bounded remote work. The private DELETE dispatches typed CQRS revocation; real CarpaNet `SignOutAsync` is attempted and the exact local encrypted session is removed even on PDS outage, prior absence, or caller cancellation. The obsolete local-delete command/gateway path was removed rather than retained as a compatibility shim.
- **BFF/API flow:** ATProto refresh is selected only by an unambiguous protected-cookie provider claim and server-validated platform user, tenant, DID, origin, and token properties. The BFF calls the hidden current-session POST/DELETE route with both bearer and one-use method/path-bound ES256 assertion. Refresh success replaces only protected cookie/circuit state; any rejected or invalid response signs out and returns stable `reauthentication_required` without retry loops or token-bearing bodies.
- **Operations:** Passive readiness distinguishes disabled, ready, and safely unavailable configuration without a PDS/OAuth probe. Fixed-cardinality metrics cover readiness, challenge, callback, bridge verification, refresh, and revoke; no DID, handle, query, token, JWK, exception body, or arbitrary URL is a label.
- **Public surface:** Raw token writes and direct public `AtprotoRecord` mutations have no controller/CQRS/OpenAPI/HAL/serializer/generated-client authority. The canonical NSwag command ran once. The handle UX remains labelled, required, autofocus/keyboard accessible, and posts server-side without URL handle leakage.
- **Verification:** Focused Infrastructure gateway, API bridge metadata, BFF refresh/revoke, readiness, observability, login UX, generated-surface, and the complete Blazor.Client test project are green. The broad BFF project is 313/314 with one isolated-repeat failure in a pre-existing non-hermetic tenant-page fixture that reaches unavailable API/localhost endpoints; no Todo 8 diff touches its test/factory/forwarding/page stack. The canonical build and ATProto architecture build are currently stopped by unrelated concurrent email-dispatch interface drift (`CS1061`), the full Application project was also previously blocked by unrelated stale event-location constructors, and real PostgreSQL lock contention is blocked before its body by unrelated missing `is_deleted` schema state. No Docker, browser, Aspire, or live PDS was started.
- **Documentation source limitation:** Context7 was invoked as required but its monthly quota was exhausted. Pinned local CarpaNet 1.0.2 docs/source under `/home/amir/dev/Github/CarpaNet/docs/docs` and the restored package were used as the recorded authoritative fallback.
- **Next action:** Independent verifier reruns Todo 8 gates and audits ordering, identity binding, cancellation-safe cleanup, bounded operations, and browser isolation before checking any Phase 5/6 task or acceptance box.

### Phase 3 implementation handoff — 2026-07-18 Europe/Brussels

- **Current state:** Tasks 3.1-3.3 are implemented but deliberately unchecked until independent verification. The BFF assertion, API bootstrap/session schemes, replay ledger, linked-account-only CQRS transaction, real Carpa PDS verification, encrypted session persistence, first-party JWT, private controller, and MultiAuth routing are present.
- **Security regression found and fixed:** deterministic real-Carpa coverage showed CarpaNet 1.0.2's default JSON resolver cannot deserialize its public `GetSessionResponse`. Infrastructure now adds its own source-generated response model to a cloned Carpa options resolver chain; the suite proves repeated restore from encrypted repository state.
- **Validation:** focused BFF, API, Application, and Infrastructure tests pass; API/BFF/Infrastructure production builds report zero errors/warnings; persistence replay tests compile. Exact commands and red-green evidence are in `.omo/evidence/atproto-auth/task-6/README.md`.
- **Independent gates:** run the PostgreSQL replay race with Testcontainers in an approved environment, inspect the private/OpenAPI/WASM boundary, and run full Phase 3 gates after unrelated shared test constructors settle.
- **Non-negotiable boundaries:** bootstrap assertions carry no user/DID authority; PDS verification precedes all writes; only exact pre-linked identities may sign in; Actor/IndexedDid/encrypted session commit before JWT minting; OAuth-client, encryption, and session-JWT keys stay purpose-separated; Keycloak/API-key routing remains unchanged.

### Handoff — 2026-07-18 Europe/Brussels

- **Current state:** Tasks 1.1-1.3 are complete and independently verified. Task 1.3 adds the shared constrained OAuth/PDS transport, BFF and Infrastructure factories, strict confidential assertions/nonces, fail-closed readiness, and operator guidance.
- **Next action:** Re-run the two Phase 1 root gates after the unrelated email-retention changes settle, then start Task 2.1.
- **Blockers:** No Task 1.3 product blocker remains. Root verification is temporarily limited by unrelated email fixture/interface and email side-effect architecture changes. ADR-015 remains a planned prerequisite task, not an external blocker.
- **Modified files:** Exact Task 1.3 production, test, and documentation paths are recorded in `.omo/evidence/atproto-auth/task-3/README.md`.
- **Validation:** Focused and full BFF suites are green after the warning repair; the Infrastructure production project builds with 0 errors/0 warnings; prior full Infrastructure/architecture/root gates were green; the independent verifier issued **SCOPED CONFIRMED**. Latest root limitations are explicitly recorded in Task 1.3 evidence.
- **Documentation impact:** ADR-014, self-hosting transport/key-rotation guidance, and fail-closed troubleshooting guidance are complete for Task 1.3. Later operational telemetry/session details remain assigned to Task 5.3.
- **Risks:** CarpaNet internal HttpClient behavior, CallbackAsync/StoreAsync ordering, DB/PDS commit ordering, exhaustive-yet-private description projection, stable-rkey settlement recovery, and Jetstream trust/cursor handling.
- **Notes for next contributor/agent:** Preserve SyncUser's no-email/no-auto-match invariant and Task 1.2's direct instance-only secret ownership. Before Phase 9/10 runtime work, finish ADR-015 without weakening A8-A12. The paused blazor-clean-code-refactor task 6A.5 overlaps the handler and should be marked absorbed only under that workstream's maintenance rules.
