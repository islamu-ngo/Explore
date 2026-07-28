<!-- ABOUTME: Hot handoff context for global Actor and concrete global subject architecture. -->
<!-- ABOUTME: Captures tenant participation, federation classification, moderation, migration risks, and next work. -->

# ATProto Federation Actor Lifecycle - Context

Last Updated: 2026-07-28 Europe/Brussels

## SESSION PROGRESS (2026-07-28 Europe/Brussels)

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
- Implemented Task 6.1 runtime behavior across global Actor moderation, exact identity moderation, creation eligibility, public Event and anonymous child eligibility, federated projection lifecycle, management detail/HAL behavior, HybridCache and output-cache invalidation, and fenced outbound publication compensation. Four reason-only POST routes select the action server-side. The slice has no schema, migration, Cerbos policy, generated-client, OpenAPI, or UI change.

### IN PROGRESS

- Task 6.1 verification and canonical documentation convergence. Runtime implementation is complete; final Phase 6 verification remains for the parent.

### NEXT

1. Parent runs the remaining Task 6.1 and Phase 6 verification, including the final Release build and selected API project command.
2. Start Task 6.2 global/contextual Actor reads and tenant-local subscription discoverability only after Task 6.1 verification closes.
3. Add OrganizationTenant legitimacy evidence in Task 7.1.
4. Finish OpenAPI/client/UI/localization/doc regeneration and architecture guardrails only in the owning later task.

### BLOCKERS

- Full Phase 0 API suite cannot complete in this environment because Docker/Testcontainers cannot connect to `/var/run/docker.sock` or the Docker Desktop socket. The run reached 1470 passed and 517 infrastructure failures after all code-related failures were fixed.
- Architecture test baseline currently has two unrelated failures recorded below; implementation must not hide them.
- The current full Persistence run exposes 198 stale fixtures after the new required provenance FK and exact-one Actor owner constraint; focused lifecycle migration tests pass.
- Task 5.2 PostgreSQL tests compile but Docker/Testcontainers cannot reach either configured socket.
- Concurrent registration-data work owns current EF snapshot drift; the model probe contained only its Event participation tables/column removals.
- Concurrent `AtprotoJetstreamRepository` work owns the current Persistence-to-Application.DTO architecture failure.
- A concurrent `MinorUnitMath` edit currently blocks the repository build with CS9135 before Task 5.2; the affected Application and Persistence projects compile against the last green Domain binary.

## Quick Resume

1. Read this context and the tasks ledger.
2. Open only the current phase and referenced decisions in the plan.
3. Do not implement tenant Actor, ActorTenantPresence, or tenant-local duplicate Organization/Group.
4. Keep tasks current and update context/plan only at defined triggers.

## Status Snapshot

| Field | Value |
|---|---|
| Overall status | Implementation in progress |
| Completed implementation tasks | 11/14 |
| Current priority | Task 6.1 verification |
| Runtime implementation | Phases 0-5 and Task 6.1 complete |
| Verification | Task 6.1 focused evidence passes; final Phase 6 build/API verification remains parent-owned; PostgreSQL projection and eligibility execution is Docker-blocked |

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
| `OrganizationTenant.cs` / `GroupTenant.cs` | Tenant approval/status/moderation/settings/profile/hierarchy | Add legitimacy evidence in Task 7.1. |
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
- Task 6.1 focused evidence: Domain Actor 17/17 and identity 5/5; moderation handlers 13/13; moderation API 15/15; creation eligibility 33/33; deterministic public eligibility matrix and child tests 3/3; projection persistence 2/2 in-memory; discovery/source Application 18/18 and API discovery 12/12; detail handlers 11/11; Event HAL 22/22; planner 49/49; PDS 5/5; RSVP 10/10. PostgreSQL projection and eligibility matrices compile but cannot execute without Docker. Recent slice Release builds passed, but later concurrent ticketing drift has intermittently blocked broad builds, so final canonical verification is not claimed.

## Risks

- False Organization/Group consolidation is an account-takeover risk.
- Globalization must move all local state before removing tenant columns.
- Actor profile media currently depends on tenant storage.
- Event authorization must not infer tenant authority from global Actor.
- External observation must not create managed participation.
- Actor/identity/participation/Event moderation must not overlap ambiguously.

## Handoff Notes

### Handoff - 2026-07-28 Europe/Brussels

- **Current state:** Phases 0-5 and Task 6.1 runtime are implemented; 11/14 tasks complete. Phase 6 verification remains open.
- **Next action:** Parent runs final Task 6.1 verification, then Task 6.2 is next. Do not mark Phase 6 complete before that verification.
- **Blockers:** Docker-blocked PostgreSQL projection/eligibility execution, 198 stale Persistence fixtures, concurrent registration/ticketing model drift, and intermittent broad-build drift owned by other workstreams.
- **Modified files:** Runtime, persistence, migrations, tests, schema, and this active workstream; preserve unrelated dirty changes.
- **Validation:** Task 5.2 boundary Release build 0 errors; Application 17/17, EF retry tracking 1/1, BFF 20/20, Infrastructure 13/13, lifecycle architecture 4/4; final Oracle PASS; idempotent migration SQL reviewed.
- **Documentation impact:** Domain, Authorization, Security Model, Federation, API, API Changelog, Multi-Tenancy, Admin Hierarchy, and this active workstream describe Task 6.1. No generated inventory, schema, OpenAPI, client, or UI artifact changed.
- **Notes:** Never reintroduce tenant Actor or ActorTenantPresence. Do not merge Organizations/Groups by name. Preserve predecessor federation infrastructure.
