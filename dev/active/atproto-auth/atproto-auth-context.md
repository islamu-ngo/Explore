<!-- ABOUTME: Hot handoff context for the AT Protocol OAuth and event-federation implementation workstream. -->
<!-- ABOUTME: Captures DB-first publication, exhaustive event projection, governed validation, ingress, blockers, and next work. -->

# AT Protocol Integration — Context

Last Updated: 2026-07-18 Europe/Brussels

## SESSION PROGRESS (2026-07-18 Europe/Brussels)

### COMPLETED

- Classified the composite work against the repository intent catalog.
- Read all required repository docs, path rules, selected skills, the complete ATProto report, current auth/federation code, existing tests, configuration/secrets paths, and overlapping workstreams.
- Verified CarpaNet from both local documentation/source at /home/amir/dev/Github/CarpaNet and Context7 package /drasticactions/carpanet.
- Confirmed the existing Release build is green: 0 errors, 326 existing warnings.
- Created the repository-grounded six-phase OAuth plan and synchronized task checklist.
- Re-baselined the workstream to twelve phases after the user's federation clarification.
- Verified both local publication seams use IUnitOfWork and that current EventPublish readiness is stricter than the community lexicon's required `name` and `createdAt`.
- Verified the full event graph spans sessions, days, agenda, locations/rooms, lookups, aspects, categories/tags, speakers/languages, and event/session custom-property EAV values.
- Re-queried Context7 /drasticactions/carpanet for RestoreSessionAsync, repository record operations, and Jetstream WantedCollections/cursor/commit behavior.
- Replaced the former F0 blocker with executable governance, projection, outbox, Jetstream, API/HAL, and UI phases. ADR-015 is now Task 9.1.
- Execution was approved by the user's persistent implementation goal; the workstream is now in progress with 5/27 product tasks complete.
- Captured a fresh protected dirty-tree fingerprint at HEAD `aefa7797054c58a1233267835417aea46830b050` and confirmed the exact Release baseline: 25 projects, 0 errors, 0 warnings.
- Reconciled architecture corrections: no direct `AtprotoRecord` mutations, a server-private bridge, the existing `IsLockable` engine, typed RSVP projection, public-location disclosure, independent source-field manifests, global canonical inbound ownership, capability-bound community readiness, and last-moment delivery gate rechecks.
- Completed and independently verified Task 1.1: stable CarpaNet 1.0.2 packages are centrally pinned with NuGet-generated lock files, BFF/Infrastructure ownership is enforced, and the exact eight-file authoritative local lexicon closure compiles without network resolution. Evidence is in `.omo/evidence/atproto-auth/task-2/README.md`.
- Completed and independently verified Task 1.2: ADR-014, canonical OAuth client metadata/JWKS publication, a strict rotation-capable ES256 key provider, typed BFF options/Infisical mapping, and three direct instance-only secret definitions are implemented. Implementation evidence is in `.omo/evidence/atproto-auth/task-3a/`; independent confirmation is in `.omo/evidence/atproto-auth/task-3a-verifier/`.
- Task 1.2 security rework now rejects ambiguous/non-local callback paths, credential-bearing or non-canonical public origins, and non-canonical base64url JWK values; public JWKS and browser contracts remain private-material free.
- Completed and independently scoped-confirmed Task 1.3: one package-free shared transport now enforces canonical metadata, strict OAuth forms, fresh issuer-bound assertions, mandatory response nonces, DNS-pinned public egress, bounded responses, and fail-closed readiness across BFF and Infrastructure. Evidence is in `.omo/evidence/atproto-auth/task-3/README.md`.

### IN PROGRESS

- Phase 1 implementation is complete. Its final root verification is pending only because unrelated concurrent email-retention changes currently break the Infrastructure test fixture contract and one email side-effect architecture rule. Phase 7 governance/validation is independently verified complete.

### NEXT

1. Re-run the Phase 1 Release build and architecture gates when the unrelated email-retention work settles.
2. Mark Phase 1 verification complete only when both root commands pass.
3. Begin Task 2.1 encrypted DID-keyed session persistence after that gate.

### BLOCKERS

- **Shared-tree verification limitation:** the latest Phase 1 root rerun is unrelatedly blocked by `InMemoryEmailDispatchOutboxRepository` missing three new retention interface members; architecture additionally reports an unrelated `InstanceSettingsController` email-transport boundary violation. Task 1.3 itself is independently scoped-confirmed. Do not fix those email files from this workstream.

### ACCEPTED PRODUCT CONSTRAINT

- Linked-account-only ATProto sign-in is accepted by execution approval. Unlinked identities remain rejected; no synthetic email, implicit user creation, or email auto-match is added.

## Quick Resume

1. Read this context and atproto-auth-tasks.md.
2. Read only the current phase, constraints, or changed decisions from atproto-auth-plan.md.
3. Start from the first unchecked high-priority task unless the user overrides it.
4. Keep tasks current during implementation. Update context/plan only at their defined triggers.
5. Before Phase 9/10 federation runtime work, complete ADR-015 Task 9.1; do not weaken the DB-first, one-capability, exhaustive-description, consent, or two-collection invariants.

## Current Status Snapshot

| Field | Value |
|---|---|
| Overall status | Approved; implementation in progress |
| Completed implementation tasks | 5/27 |
| Current priority | Implement encrypted DID-keyed persistence and exhaustive event/RSVP projection |
| Next executable slice | Complete Tasks 2.1-2.2/9.1 and 8.1-8.2, then independently verify both lanes |
| OAuth release | Fully planned in Phases 1-6 |
| Federation release | Fully planned in Phases 7-12; ADR-015 is the first persistence task, not an external blocker |
| Baseline build | Fresh green baseline at HEAD aefa7797 on 2026-07-18; 25 projects, 0 errors, 0 warnings |

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
| src/Explore.Application/Contracts/Identity/IAtprotoOAuthSecurityGateway.cs | New | Application | External OAuth verification/crypto boundary | No Carpa types leak into Application. |
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

## Verified Current Behavior

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

These are binding requirements assigned to Phases 7-12:

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

### Handoff — 2026-07-18 Europe/Brussels

- **Current state:** Tasks 1.1-1.3 are complete and independently verified. Task 1.3 adds the shared constrained OAuth/PDS transport, BFF and Infrastructure factories, strict confidential assertions/nonces, fail-closed readiness, and operator guidance.
- **Next action:** Re-run the two Phase 1 root gates after the unrelated email-retention changes settle, then start Task 2.1.
- **Blockers:** No Task 1.3 product blocker remains. Root verification is temporarily limited by unrelated email fixture/interface and email side-effect architecture changes. ADR-015 remains a planned prerequisite task, not an external blocker.
- **Modified files:** Exact Task 1.3 production, test, and documentation paths are recorded in `.omo/evidence/atproto-auth/task-3/README.md`.
- **Validation:** Focused and full BFF suites are green after the warning repair; the Infrastructure production project builds with 0 errors/0 warnings; prior full Infrastructure/architecture/root gates were green; the independent verifier issued **SCOPED CONFIRMED**. Latest root limitations are explicitly recorded in Task 1.3 evidence.
- **Documentation impact:** ADR-014, self-hosting transport/key-rotation guidance, and fail-closed troubleshooting guidance are complete for Task 1.3. Later operational telemetry/session details remain assigned to Task 5.3.
- **Risks:** CarpaNet internal HttpClient behavior, CallbackAsync/StoreAsync ordering, DB/PDS commit ordering, exhaustive-yet-private description projection, stable-rkey settlement recovery, and Jetstream trust/cursor handling.
- **Notes for next contributor/agent:** Preserve SyncUser's no-email/no-auto-match invariant and Task 1.2's direct instance-only secret ownership. Before Phase 9/10 runtime work, finish ADR-015 without weakening A8-A12. The paused blazor-clean-code-refactor task 6A.5 overlaps the handler and should be marked absorbed only under that workstream's maintenance rules.
