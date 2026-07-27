<!-- ABOUTME: Hot handoff context for global Actor and concrete global subject architecture. -->
<!-- ABOUTME: Captures tenant participation, federation classification, moderation, migration risks, and next work. -->

# ATProto Federation Actor Lifecycle - Context

Last Updated: 2026-07-27 Europe/Brussels

## SESSION PROGRESS (2026-07-27 Europe/Brussels)

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

### IN PROGRESS

- Task 5.2: direct external-subject promotion and proof-gated same-kind consolidation. Task 5.1 intentionally rejects existing cross-kind identities rather than mutating or merging them.

### NEXT

1. Implement Task 5.2 direct external promotion and proof-gated same-kind consolidation.
2. Then complete four-level moderation, contextual Actor reads, OrganizationTenant evidence, and final contract/UI/doc convergence.

### BLOCKERS

- Full Phase 0 API suite cannot complete in this environment because Docker/Testcontainers cannot connect to `/var/run/docker.sock` or the Docker Desktop socket. The run reached 1470 passed and 517 infrastructure failures after all code-related failures were fixed.
- Architecture test baseline currently has two unrelated failures recorded below; implementation must not hide them.
- The current full Persistence run exposes 198 stale fixtures after the new required provenance FK and exact-one Actor owner constraint; focused lifecycle migration tests pass.

## Quick Resume

1. Read this context and the tasks ledger.
2. Open only the current phase and referenced decisions in the plan.
3. Do not implement tenant Actor, ActorTenantPresence, or tenant-local duplicate Organization/Group.
4. Keep tasks current and update context/plan only at defined triggers.

## Status Snapshot

| Field | Value |
|---|---|
| Overall status | Implementation in progress |
| Completed implementation tasks | 9/14 |
| Current priority | Task 5.2 |
| Runtime implementation | Phases 0-4 and Task 5.1 complete |
| Verification | Release build 0 errors; Task 5.1 focused suites 64/64 pass; EF snapshot clean and guarded provider-key migration SQL reviewed; full Persistence fixtures remain open |

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
| `BootstrapAtprotoSessionCommandHandler.cs` | Linked-account bootstrap plus explicit represented-subject classification and participation transaction | Complete for Task 5.1; Task 5.2 owns promotion/consolidation. |
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

## Risks

- False Organization/Group consolidation is an account-takeover risk.
- Globalization must move all local state before removing tenant columns.
- Actor profile media currently depends on tenant storage.
- Event authorization must not infer tenant authority from global Actor.
- External observation must not create managed participation.
- Actor/identity/participation/Event moderation must not overlap ambiguously.

## Handoff Notes

### Handoff - 2026-07-27 Europe/Brussels (updated)

- **Current state:** Phases 0-4 and Task 5.1 are implemented, including protected classification onboarding and guarded global external-login uniqueness; 9/14 tasks complete.
- **Next action:** Task 5.2 direct promotion and proof-gated same-kind consolidation. Preserve Task 5.1's `classification_conflict` behavior until that command owns the mutation.
- **Blockers:** 198 stale Persistence fixtures remain for the full database gate; later product phases remain genuinely unimplemented.
- **Modified files:** Runtime, persistence, migrations, tests, schema, and this active workstream; preserve unrelated dirty changes.
- **Validation:** Repository-wide Release build 0 errors; Task 5.1 focused suites 64/64 pass; EF snapshot clean; guarded uniqueness SQL and PostgreSQL enforcement verified.
- **Documentation impact:** Active plan/context/tasks now match the shipped classification, participation, and provider-identity behavior.
- **Notes:** Never reintroduce tenant Actor or ActorTenantPresence. Do not merge Organizations/Groups by name. Preserve predecessor federation infrastructure.
