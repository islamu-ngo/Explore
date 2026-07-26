<!-- ABOUTME: Hot handoff context for global Actor and concrete global subject architecture. -->
<!-- ABOUTME: Captures tenant participation, federation classification, moderation, migration risks, and next work. -->

# ATProto Federation Actor Lifecycle - Context

Last Updated: 2026-07-26 Europe/Brussels

## SESSION PROGRESS (2026-07-26 Europe/Brussels)

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

### IN PROGRESS

- Task 1.1: defining the global Actor/AtprotoIdentity/concrete-subject owner graph and exhaustive FK disposition manifest.

### NEXT

1. Complete Task 1.1 global owner contracts, tests, ADR, and FK manifest.
2. Complete Task 1.2 concrete participation contracts and field manifest.
3. Do not scaffold Task 2.1 until both manifests are exhaustive.

### BLOCKERS

- Full Phase 0 API suite cannot complete in this environment because Docker/Testcontainers cannot connect to `/var/run/docker.sock` or the Docker Desktop socket. The run reached 1470 passed and 517 infrastructure failures after all code-related failures were fixed.
- Architecture test baseline currently has two unrelated failures recorded below; implementation must not hide them.

## Quick Resume

1. Read this context and the tasks ledger.
2. Open only the current phase and referenced decisions in the plan.
3. Do not implement tenant Actor, ActorTenantPresence, or tenant-local duplicate Organization/Group.
4. Keep tasks current and update context/plan only at defined triggers.

## Status Snapshot

| Field | Value |
|---|---|
| Overall status | Implementation in progress |
| Completed implementation tasks | 1/14 |
| Current priority | Task 1.1 |
| Runtime implementation | Phase 0 complete |
| Verification | Release build green; Phase 0 full API suite blocked by Docker availability |

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
| `src/Explore.Domain/Actor.cs` | Tenant Actor with owner FKs/profile | Global subject identity; remove TenantId; add external owner/global moderation. |
| `ActorConfiguration.cs` | Composite tenant key and owner XOR | Global key, owner XOR/uniqueness, no tenant relation. |
| `Organization.cs` / `Group.cs` | Tenant concrete subjects | Global canonical subjects. |
| `OrganizationTenant.cs` / `GroupTenant.cs` | New | Tenant approval/status/moderation/settings/profile/hierarchy. |
| `TenantUser.cs` | Existing user participation | Retain tenant roles/moderation/profile; preferred Actor stays local. |
| member/setting entities | Tenant rows targeting subject | Target concrete participation. |
| `EventConfiguration.cs` | Composite Actor FK | Simple global ActorId FK; business authorization checks participation. |
| `ActorSubscriptionConfiguration.cs` | Tenant-local composite target | Simple global target FK; retain tenant-local semantics. |
| `IndexedDid.cs` / ActorPii DID | Split DID stores | Promote union into global AtprotoIdentity; remove duplicate authority. |
| `AtprotoJetstreamRepository.cs` | Tenant Bot creation | One global identity/external Actor shared by tenant Events. |
| create Organization/Group handlers | Multi-write tenant subject creation | Shared transactional global subject plus local participation creation. |

## Validation Baseline

- Corrected re-baseline Release build: passed with 0 errors and 835 existing warnings.
- Corrected re-baseline architecture run: 301 passed, 2 unrelated failures, 1 skipped.
- Failure 1: consent architecture source assertion expects `reporterUserId.ToString()` while handler uses `resolvedReporterUserId.ToString()`.
- Failure 2: PII inventory lacks `Event.SourcePublisherName`, `Event.SubmittedByUserId`, `EventOrganizerClaim.ReviewerUserId`, and `EventPublicAction.Url`.
- Task/phase parity, stale-assumption search, AFT planning diagnostics, and `git diff --check` passed. The unrelated architecture failures were not changed or bypassed.

## Risks

- False Organization/Group consolidation is an account-takeover risk.
- Globalization must move all local state before removing tenant columns.
- Actor profile media currently depends on tenant storage.
- Event authorization must not infer tenant authority from global Actor.
- External observation must not create managed participation.
- Actor/identity/participation/Event moderation must not overlap ambiguously.

## Handoff Notes

### Handoff - 2026-07-26 Europe/Brussels

- **Current state:** Correct global-subject architecture written across all three planning files; no runtime implementation.
- **Next action:** Review, then Task 0.1.
- **Blockers:** None in planning; two unrelated architecture baseline failures remain.
- **Modified files:** Three files in this workstream only.
- **Validation:** Release build passed with 0 errors/835 warnings. Architecture tests: 301 passed, 2 unrelated baseline failures, 1 skipped. Parity, stale-assumption, diagnostics, and diff checks passed.
- **Documentation impact:** Planning only; canonical runtime docs are assigned to implementation tasks.
- **Notes:** Never reintroduce tenant Actor or ActorTenantPresence. Do not merge Organizations/Groups by name. Preserve predecessor federation infrastructure.
