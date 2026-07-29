<!-- ABOUTME: Hot handoff context for global Actor and concrete global subject architecture. -->
<!-- ABOUTME: Captures tenant participation, federation classification, moderation, migration risks, and next work. -->

# ATProto Federation Actor Lifecycle - Context

Last Updated: 2026-07-29 Europe/Brussels

## SESSION PROGRESS (2026-07-29 Europe/Brussels)

### COMPLETED

- User/CTO clarified the invariant: the same real Organization or Group is one global subject across tenants.
- Rejected both tenant-scoped Actor and global Actor plus generic `ActorTenantPresence`.
- Verified current Actor, Organization, Group, membership, settings, hierarchy, Event, subscription, storage, query-filter, auth, identity, and Jetstream coupling.
- Verified global concrete subjects require `OrganizationTenant` and `GroupTenant`, equivalent to existing `TenantUser`.
- Verified current Event and ActorSubscription Actor relationships use composite tenant FKs that must become simple global Actor FKs.
- Verified Organization/Group approval, hierarchy, local media, settings, and membership state must move to concrete participation.
- Verified observation can be derived from Event and `AtprotoRecordTenantPresentation`; import needs no participation/presence row.
- Verified official ATProto identity/OAuth semantics and EF Core migration/filter/index constraints.
- Re-baselined plan/context/tasks around global Actor, global concrete subjects, global AtprotoIdentity-to-Actor ownership, concrete tenant participation, ExternalActorSubject, and four-level moderation.
- Removed every public `UserExternalLogin` and `IndexedDid` controller, DTO, CQRS, HAL, route, serializer, generated-client, client-service, and direct-linking UI surface while retaining internal verified authentication/federation entities and repository paths.
- Regenerated canonical OpenAPI, contract inventory, and NSwag client from source; added API/OpenAPI regressions and updated API, authorization, federation, and breaking-change docs.
- Phase 0 Release build passes with zero errors. Focused removed-route tests pass (contract plus 16 federation controller tests), and three internal verified identity-linking tests pass.
- Implemented global Actor, AtprotoIdentity, ExternalActorSubject, OrganizationTenant, and GroupTenant persistence plus participation-aware repositories, handlers, federation materialization, and mutable identity refresh.
- Added guarded `20260726194055_GlobalizeAtprotoActorLifecycle` and `20260726210851_RetireIndexedDidAuthority` migrations. Legacy organization/group IDs become participation IDs; exact-DID metadata and DID custody move to AtprotoIdentity; legacy Event URL data becomes EventPublicAction; IndexedDid authority is removed.
- Updated the maintained DBML schema to global subjects, exact-DID identities, tenant participations, and EventPublicAction ownership.
- Verified migration snapshot parity, byte-stable idempotent SQL generation, focused PostgreSQL lifecycle tests (3/3), and focused architecture guards (4/4).
- Completed Task 5.1 protected classification onboarding. Person, Organization, and Group intent is bound through OAuth state, signed bootstrap assertion, private bridge request, typed command, and bridge result; missing, unknown, or mismatched classification fails before OAuth/session writes.
- Extended the linked-account bootstrap transaction to preflight login, TenantUser, and classification conflicts before mutations; then resolve the global User and personal Actor, create/replay TenantUser, and create/replay OrganizationTenant or GroupTenant plus founder admin membership for represented managed subjects. User remains the audit authority, replay is idempotent, and JWT issuance remains post-commit.
- Added guarded `20260726231528_EnforceGlobalExternalLoginIdentity`, which widens provider keys to the 2,048-character DID boundary, aborts on duplicate non-null provider identities, creates the reversible filtered unique `(provider, provider_key)` index, and rejects lossy downgrade data.
- Verified Task 5.1 with a repository-wide Release build (0 errors), focused Application 9/9, BFF 18/18, API JWT 6/6, Infrastructure gateway 12/12, architecture 15/15, and PostgreSQL baseline guards 4/4. EF model parity is clean and the emitted idempotent migration SQL was reviewed.
- Completed Task 5.2 direct external promotion and proof-gated same-kind consolidation. Canonical targets require signed ID/stamp binding plus active approved current-tenant OrgAdmin/GroupAdmin authority; exact DID proves only the external source. Direct promotion preserves Actor/identity/Event IDs. Consolidation moves active operational identity/Event/EventSeries/speaker/subscription references, preserves consent and immutable evidence, records bounded DID-digest `ActorMerge` proof, and is replay-safe.
- Restored the atomic bootstrap order: OAuth session encryption prepares once before retries; one serializable transaction applies onboarding and persists the prepared session per attempt; cache invalidation and JWT issuance are post-commit.
- Corrected EF retry tracking: each serializable attempt reloads the current User and tracked personal Actor after failed-attempt tracking is cleared; a real-EF regression proves missing TenantUser creation does not reinsert either owner.
- Added `20260728143000_ClassifyExternalUnclassifiedActors`, lookup seeder parity for ID 6, DBML/domain/federation/security documentation, and an owner/type check constraint.
- Completed Task 6.1 implementation across global Actor moderation, exact identity moderation, creation eligibility, public Event and anonymous child eligibility, federated projection lifecycle, management detail/HAL behavior, HybridCache and output-cache invalidation, and fenced outbound publication compensation.
- Completed review remediation: transactional grounded `PdsSyncOutbox` Delete compensation covers settled ownership and pending/processing Event mutations; exact-identity moderation is DID-scoped; soft-deleted source Events remain selectable for cleanup; central eligibility gates public Event and child projections; local echoes require exact outbound ownership plus current local eligibility; Event-detail HybridCache and five process-local output-cache tags are invalidated without a cross-replica consistency claim; and moderation metadata plus secured management child reads are present in regenerated OpenAPI/client artifacts and adopted by MCP/Blazor.
- Completed Task 6.2 global/contextual Actor reads and tenant-local subscription discoverability. Global profiles omit tenant participation, private User identity, and tenant storage IDs; contextual collections compose approved public participation overrides or eligible federated Event evidence. Subscription create/detail/list/fanout share one fail-closed local-discoverability predicate, unsubscribe retains its durable-row path, and Actor HAL uses a non-serialized request-local marker without client role/claim inference.
- Added the exact contextual Actor resource at `GET /api/actor/by-tenant/{tenantId}/{id}`. It uses the shared local-discoverability boundary, composes only safe public participation overrides, emits contextual self/subscription HAL, and returns 404 for hidden or cross-tenant targets.
- Verified Task 6.2 with a canonical Release build (0 errors), focused Actor detail 6/6, subscribe handler 4/4, Actor subscription HAL/security 10/10, federated discoverability 1/1, and client private-identity 1/1. OpenAPI and NSwag contracts converge without private/request-local Actor fields. Direct security/diff review passed.
- Completed Task 7.1 OrganizationTenant legitimacy evidence. Organization admins use server-bound upload sessions and attach only active private Document storage owned by the exact pending participation; tenant admins review separately with optimistic concurrency. Composite tenant foreign keys, idempotent attachment, immutable audit, retention across update/delete/reconciliation, and fail-closed ownership checks are enforced.
- Completed Task 7.1 HAL/UI/BFF delivery. Authenticated evidence resources expose bounded metadata and affordances without tenant participation IDs, submitter/reviewer identities, object keys, provider metadata, or document content. The Organization details evidence panel follows HAL links, and PDF transfer stays behind the credentialed Blazor BFF proxy.
- Completed Task 7.2 contract and documentation convergence. OpenAPI/NSwag, serializers, the exact contextual Actor client, evidence string enums, UI/localization, canonical API/federation/security/domain/Blazor docs, ADR, DBML schema, contract inventory, and architecture guards now agree. Generic Actor create/update/delete operations remain absent.
- Final verification: Release build 26 projects/0 errors; Architecture 324 passed/9 unrelated failed/1 governed skip; Blazor client 2,231 passed/2 unrelated failed/1 governed skip; EF migration parity clean; focused Actor/evidence/HAL/BFF/service lanes passed; OpenAPI privacy assertions, conflict scan, and `git diff --check` passed.

### IN PROGRESS

- None. The requested workstream tasks are complete.

### NEXT

1. Run the Docker-backed PostgreSQL/Testcontainers lanes when a Docker socket is available.
2. Leave the recorded unrelated architecture and Blazor failures to their owning workstreams.

### BLOCKERS

- Full Phase 0 API suite cannot complete in this environment because Docker/Testcontainers cannot connect to `/var/run/docker.sock` or the Docker Desktop socket. The run reached 1470 passed and 517 infrastructure failures after all code-related failures were fixed.
- Architecture passed 324/334 with nine failures in privacy/cache registries, naming, generated-client, authorization-parity, update-inventory, and concurrent persistence/ticketing work; implementation does not hide them or claim suite success.
- The full Blazor client run passed 2,231/2,234 with one governed skip. Its two unrelated dirty-worktree failures are the CreateEvent registration-policy accessibility source assertion and EventTicketCatalogEditor cancellation-on-dispose.
- The current full Persistence run exposes 198 stale fixtures after the new required provenance FK and exact-one Actor owner constraint; focused lifecycle migration tests pass.
- Task 5.2 PostgreSQL tests compile but Docker/Testcontainers cannot reach either configured socket.
- Concurrent registration-data work owns current EF snapshot drift; the model probe contained only its Event participation tables/column removals.
- Concurrent `AtprotoJetstreamRepository` work owns the current Persistence-to-Application.DTO architecture failure.
- A concurrent `MinorUnitMath` edit previously blocked the repository build with CS9135 before Task 5.2; the affected Application and Persistence projects compiled against the last green Domain binary. The current canonical Release build passes with 0 errors.

## Quick Resume

1. Read this context and the tasks ledger.
2. Open only the current phase and referenced decisions in the plan.
3. Do not implement tenant Actor, ActorTenantPresence, or tenant-local duplicate Organization/Group.
4. Keep tasks current and update context/plan only at defined triggers.

## Status Snapshot

| Field | Value |
|---|---|
| Overall status | Complete within recorded environment limits |
| Completed implementation tasks | 14/14 |
| Current priority | None |
| Runtime implementation | Phases 0-7 complete |
| Verification | Release build 0 errors; focused Actor/evidence lanes pass; Architecture 324 passed/9 unrelated failed/1 skip; Blazor 2,231 passed/2 unrelated failed/1 skip; PostgreSQL is Docker-blocked |

## Core Model

- `Actor`, `User`, `Organization`, `Group`, `ExternalActorSubject`, and `AtprotoIdentity` are global.
- `TenantUser`, `OrganizationTenant`, and `GroupTenant` are tenant participation.
- AtprotoIdentity points to represented Actor; UserExternalLogin points to authenticated User and identity.
- Actor retains one authoritative concrete-owner FK and exact-one-owner XOR; concrete subjects expose inverse navigation without duplicate FK.
- Event references global Actor directly; tenant write authority is resolved through concrete participation.
- Import creates no participation row. External observation is Event/presentation evidence.

## Key Decisions

- Same real subject uses one Actor across all tenants.
- Organization/Group names are not merge evidence. Existing rows remain distinct unless global User or exact verified DID proves identity.
- Group hierarchy is tenant-local and moves to GroupTenant.
- Approval, local moderation/visibility, organizer eligibility, settings, local profile/media overrides, and memberships belong to participation.
- Tenant-owned StorageObject cannot back a global Actor FK; local media moves to participation overrides, while global profile keeps safe URI/CID metadata.
- Actor moderation is platform-wide and instance-controlled.
- AtprotoIdentity moderation blocks one external identity without necessarily suspending Actor.
- Tenant moderation targets TenantUser/OrganizationTenant/GroupTenant or tenant identity import policy.
- Event moderation remains content-specific.
- ActorSubscription remains tenant-local; global following is deferred.
- Direct external promotion preserves Actor/identity/Event IDs. Proven same-kind consolidation normally keeps existing concrete Actor canonical. Cross-kind merge is forbidden.
- Classification is explicit local intent after DID verification; ATProto profile metadata is not subject-kind authority.
- No backward compatibility or mixed-version mode.

## Key Files

| Path | Current role | Planned change |
|---|---|---|
| `src/Explore.Domain/Actor.cs` | Global subject identity with exact-one concrete owner | Add command/policy behavior for remaining moderation and promotion phases. |
| `ActorConfiguration.cs` | Global key, owner XOR/uniqueness, no tenant relation | Complete. |
| `Organization.cs` / `Group.cs` | Global canonical subjects | Complete. |
| `OrganizationTenant.cs` / `GroupTenant.cs` | Tenant approval/status/moderation/settings/profile/hierarchy | OrganizationTenant legitimacy evidence complete; GroupTenant unchanged. |
| `TenantUser.cs` | Existing user participation | Retain tenant roles/moderation/profile; preferred Actor stays local. |
| member/setting entities | Tenant rows targeting subject | Target concrete participation. |
| `EventConfiguration.cs` | Composite Actor FK | Simple global ActorId FK; business authorization checks participation. |
| `ActorSubscriptionConfiguration.cs` | Tenant-local composite target | Simple global target FK; retain tenant-local semantics. |
| `AtprotoIdentity.cs` | Sole exact-DID authority with DID custody and mutable verified metadata | Add identity moderation command/policy behavior in Task 6.1. |
| `AtprotoJetstreamRepository.cs` | One global identity/external Actor shared by tenant Events | Complete. |
| create Organization/Group handlers | Transactional global subject plus local participation creation | Complete. |
| `BootstrapAtprotoSessionCommandHandler.cs` | Prepares encryption once, reloads tracked owners per serializable attempt, persists onboarding/session atomically, and issues JWT post-commit | Complete for Task 5.2. |
| `AtprotoSubjectOnboardingOperation.cs` | Direct promotion, authorized same-kind consolidation, replay, and current-tenant participation | Complete for Task 5.2. |
| `ActorReferenceConsolidationRepository.cs` | Moves active operational references across tenants while preserving consent/history | Complete for Task 5.2. |
| `20260728143000_ClassifyExternalUnclassifiedActors` | Lookup ID 6, legacy BOT backfill, and external owner/type constraint | Complete; PostgreSQL execution awaits Docker. |
| `AtprotoAuthenticationHandler.cs` / `AtprotoBootstrapAssertionService.cs` | OAuth state validation and signed DID/classification bridge binding | Complete. |
| `20260726231528_EnforceGlobalExternalLoginIdentity` | 2,048-character provider keys plus guarded global provider/key uniqueness | Complete. |

## Validation Baseline

- Corrected re-baseline Release build: passed with 0 errors and 835 existing warnings.
- Corrected re-baseline architecture run: 301 passed, 2 unrelated failures, 1 skipped.
- Failure 1: consent architecture source assertion expects `reporterUserId.ToString()` while handler uses `resolvedReporterUserId.ToString()`.
- Failure 2: PII inventory lacks `Event.SourcePublisherName`, `Event.SubmittedByUserId`, `EventOrganizerClaim.ReviewerUserId`, and `EventPublicAction.Url`.
- Task/phase parity, stale-assumption search, AFT planning diagnostics, and `git diff --check` passed. The unrelated architecture failures were not changed or bypassed.
- Current migration verification: API build 0 errors; Persistence integration-test build 0 errors; EF pending-model check clean; repeated idempotent SQL generation byte-identical; focused PostgreSQL migration tests 3/3; focused lifecycle/privacy architecture tests 4/4.
- Current full Persistence attempt timed out after reporting 198 fixture failures. Dominant causes are required Event provenance rows not supplied by fixtures and ownerless test Actors rejected by the new database invariant.
- Task 5.1 validation: repository-wide Release build passed with 0 errors; focused Application classification tests 9/9, BFF binding/bridge tests 18/18, API JWT tests 6/6, Infrastructure OAuth gateway tests 12/12, Clean Architecture tests 15/15, and PostgreSQL baseline guards 4/4 passed.
- Task 5.1 migration validation: `dotnet ef migrations has-pending-model-changes` reported no changes; the idempotent SQL widens provider keys, checks duplicates, creates the filtered unique index, and writes migration history in one transaction. The PostgreSQL test accepts a 2,048-character key and rejects the same provider/key in another tenant.
- Task 5.2 validation: Application 17/17, EF retry tracking 1/1, BFF 20/20, Infrastructure 13/13, lifecycle architecture 4/4, diagnostics and diff checks clean, boundary API/Persistence/canonical Release builds 0 errors, migration discovered, idempotent SQL reviewed, and final Oracle review passed. The focused PostgreSQL suite compiled but its two tests could not start without Docker; a later concurrent `MinorUnitMath` edit now blocks a fresh repository build before Task 5.2.
- Task 6.1 validation: canonical Release build passed with 0 errors; pre-existing package advisory warnings remain. Moderation compensation passed 17/17, public actions 10/10, local echo 3/3, Actor moderation API 9/9, cache invalidation 1/1, public eligibility 3/3, publication planning 49/49, managed child Application/API/Blazor lanes, and management collection HAL 23/23. The full Persistence run confirmed all three in-memory eligibility tests before 597 Docker/Testcontainers failures. Final architecture verification remained 320/330 with the same nine recorded non-runtime failures. Direct review passed after separating managed and public requests into one-request-per-handler classes; the remediation build, Application 4/4, API 2/2, diagnostics, conflict scan, and diff check passed.
- Task 6.2 validation: canonical Release build passed with 0 errors; Actor detail 6/6, subscribe handler 4/4, Actor subscription HAL/security 10/10, eligible federated evidence 1/1, and client private-identity 1/1 passed. OpenAPI and the generated NSwag client omit tenant/private/request-local Actor fields. Direct review confirmed fail-closed local discoverability across create/detail/list/fanout and unrestricted durable-row access only for unsubscribe. Final Architecture is 324 passed, 9 unrelated failed, and 1 governed skip; PostgreSQL/Testcontainers remains Docker-blocked.
- Task 7.1 validation: focused evidence command-handler, Organization service, BFF proxy, HAL, retention, and string-enum contract checks passed. EF Core reports no pending model changes after `20260729141557_AddOrganizationTenantLegitimacyEvidence`. Direct review confirmed exact tenant/participation/document ownership, separate tenant-admin review, optimistic concurrency, idempotent attachment, retention, and no document/provider/private-identity leakage.
- Task 7.2 validation: canonical Release build passed for 26 projects with 0 errors. OpenAPI privacy assertions, exact contextual Actor operation, generated Actor CRUD absence, conflict scan, and `git diff --check` passed. The full Blazor run is 2,231 passed, 2 unrelated failed, and 1 governed skip. Runtime browser visual QA was not started because the active workstream contract prohibits starting live services/browser automation in this repository state.

## Risks

- False Organization/Group consolidation is an account-takeover risk.
- Globalization must move all local state before removing tenant columns.
- Actor profile media currently depends on tenant storage.
- Event authorization must not infer tenant authority from global Actor.
- External observation must not create managed participation.
- Actor/identity/participation/Event moderation must not overlap ambiguously.

## Handoff Notes

### Handoff - 2026-07-29 Europe/Brussels

- **Current state:** Phases 0-7 and all 14 implementation tasks are complete.
- **Next action:** Run Docker-backed PostgreSQL/Testcontainers verification when infrastructure is available; no implementation action remains in this workstream.
- **Blockers:** Docker-blocked PostgreSQL execution, Architecture 324/334 with nine unrelated failures, Blazor 2,231/2,234 with two unrelated failures and one skip, 198 stale Persistence fixtures, and concurrent registration/ticketing model drift owned by other workstreams.
- **Modified files:** Runtime, persistence, migrations, tests, schema, and this active workstream; preserve unrelated dirty changes.
- **Validation:** Task 5.2 and 6.1 evidence remains unchanged. Task 6.2 has 22 focused checks green. Task 7.1 focused handler/service/BFF/HAL/retention checks and EF parity pass. Release build is green; Architecture is 324 passed/9 unrelated failed/1 skip; Blazor is 2,231 passed/2 unrelated failed/1 skip; direct security/diff review passed.
- **Documentation impact:** Federation, API, API Changelog, security/domain/Blazor docs, ADR, DBML schema, contract inventory, OpenAPI/NSwag, and this active workstream record the completed global/contextual Actor and tenant-local evidence contracts without exposing private/request-local fields.
- **Notes:** Never reintroduce tenant Actor or ActorTenantPresence. Do not merge Organizations/Groups by name. Preserve predecessor federation infrastructure.
