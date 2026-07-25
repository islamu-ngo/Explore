<!-- ABOUTME: Hot handoff context for the Actor-first ATProto federation identity lifecycle workstream. -->
<!-- ABOUTME: Captures global identity, verified claiming, tenant presence, moderation, risks, and next work. -->

# ATProto Federation Actor Lifecycle - Context

Last Updated: 2026-07-25 Europe/Brussels

## SESSION PROGRESS (2026-07-25 Europe/Brussels)

### COMPLETED

- Classified the follow-up against all relevant repository intents, rules, skills, tests, and documentation obligations.
- Read the required planning resources and the implemented `dev/active/atproto-auth` workstream, including the historical backfill report.
- Traced current Actor, ActorPii, User, UserPii, TenantUser, UserExternalLogin, Event ownership, Jetstream import, auth bootstrap, discovery projection, moderation, API, HAL, Blazor, and representative tests.
- Confirmed the root mismatch: Actor is tenant-scoped, DID is non-unique, import creates one Actor per tenant, and verified auth rejects unlinked identities.
- Confirmed the reusable foundation: OAuth verification, canonical records, global Jetstream cursor, tenant record presentations, Event/EventSession materialization, DB-first outbox, recovery, source links, and zero-echo behavior already exist.
- Established the proposed model: global Actor, separate ActorTenantPresence, optional User, `User.ActorId` as sole personal ownership link, explicit verified claim/merge, scoped moderation, and materialized-only public discovery.
- Baseline Release build passed during planning with existing package vulnerability warnings.
- Created the synchronized plan/context/tasks workstream. No runtime files were changed.
- Planning verification passed: `git diff --check -- dev/active/atproto-federation-actor-lifecycle` returned clean, and `Event.Architecture.Tests` passed 302/302 executed tests with one documented skip; existing `System.Security.Cryptography.Xml` NU1903 warnings remain.

### IN PROGRESS

- Awaiting user review and approval of the draft architecture and phase sequence.

### NEXT

1. Review Plan Decisions A1-A9, especially the Actor FK disposition rule and DID-Actor-wins merge behavior.
2. If approved, start Task 1.1 by writing ADR-016 and the complete Actor FK disposition manifest before changing schema.
3. Do not start auth/import changes before the global Actor migration contract is approved and tested.

### BLOCKERS

- User approval is required before implementation because this plan supersedes the predecessor workstream's accepted linked-account-only constraint.
- Migration must abort rather than guess if one DID group contains Actors owned by different Users.

## Quick Resume

1. Read this context and `atproto-federation-actor-lifecycle-tasks.md`.
2. Read only the current phase and referenced decisions in `atproto-federation-actor-lifecycle-plan.md`.
3. Start from the first unchecked high-priority task after user approval.
4. Keep tasks current during implementation; update context/plan only at their defined triggers.

## Current Status Snapshot

| Field | Value |
|---|---|
| Overall status | Draft, awaiting user review |
| Completed implementation tasks | 0/12 |
| Current priority | User review of A1-A9 and Phase 1 migration boundary |
| Next executable slice | Task 1.1 after approval |
| Runtime implementation | Not started |
| Planning artifacts | Created and synchronized |
| Baseline | Release build green with existing package vulnerability warnings |

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `dev/active/atproto-auth/*` | Existing | Dev docs | Implemented OAuth/federation predecessor | Reuse; linked-only constraint is the planned delta. |
| `src/Explore.Domain/Actor.cs` | Existing | Domain | Current Actor aggregate | Remove tenancy and reverse owner FKs. |
| `src/Explore.Domain/ActorPii.cs` | Existing | Domain | Public identifying/profile extension | DID becomes globally unique. |
| `src/Explore.Domain/User.cs` | Existing | Domain | Optional platform account | `ActorId` becomes sole personal ownership link. |
| `src/Explore.Domain/UserPii.cs` | Existing | Domain | Private User identity fields | Email becomes nullable for verified ATProto account. |
| `src/Explore.Domain/TenantUser.cs` | Existing | Domain | Account tenant membership/moderation | Retained; not suitable for unclaimed Actor presence. |
| `src/Explore.Domain/ActorTenantPresence.cs` | New | Domain | Actor visibility/presence per tenant | Represents claimed or unclaimed Actors. |
| `src/Explore.Domain/ActorMerge.cs` | New | Domain | Immutable merge evidence | Preserves source/canonical IDs and reason. |
| `src/Explore.Persistence/Configurations/Entities/ActorConfiguration.cs` | Existing | Persistence | Current tenant Actor keys/ownership | Replaced with global constraints. |
| `src/Explore.Persistence/Configurations/Entities/ActorPiiConfiguration.cs` | Existing | Persistence | DID/handle indexes | Add filtered unique DID index. |
| `src/Explore.Persistence/Configurations/Entities/EventConfiguration.cs` | Existing | Persistence | Composite tenant Actor FK | Replace with simple ActorId FK; Event stays tenant-scoped. |
| `src/Explore.Persistence/Repositories/ActorRepository.cs` | Existing | Persistence | Actor id/DID/tenant lookups | Split global identity from presence reads. |
| `src/Explore.Persistence/Repositories/AtprotoJetstreamRepository.cs` | Existing | Persistence | Atomic canonical import/materialization | Reuse global Actor and upsert tenant presence. |
| `src/Explore.Application/Features/Authentication/Atproto/Handlers/Commands/BootstrapAtprotoSessionCommandHandler.cs` | Existing | Application | Verified session bootstrap | Replace linked-only gate with safe claim/conflict flow. |
| `src/Explore.Application/Features/Users/Handlers/Commands/SyncUserCommandHandler.cs` | Existing | Application | Provider User synchronization | Keep no-email-match; allow verified ATProto creation through bounded path. |
| `src/Explore.Application/Features/Federation/Atproto/Handlers/Queries/GetPublicEventDiscoveryRequestHandler.cs` | Existing | Application | Merges local and projection discovery | Become materialized-only. |
| `src/Explore.API/Controllers/ActorController.cs` | Existing | API | Actor public/write routes | Serve global profile and scoped HAL actions. |
| `docs/adr/ADR-015-atproto-event-federation-ownership.md` | Existing | Docs | Current record/outbox ownership | Preserve its DB-first and canonical-ingress invariants. |
| `docs/adr/ADR-016-atproto-actor-identity-lifecycle.md` | New | Docs | Identity/claim/merge decision record | Task 1.1 hard gate. |

## Key Decisions

- Actor is global and one non-null DID maps to at most one Actor.
- Person replaces User as the personal Actor type semantic; a Person Actor may be unclaimed.
- ActorTenantPresence owns tenant visibility; TenantUser continues to own account membership and roles.
- User.ActorId is the only personal ownership link. Organization.ActorId and Group.ActorId remain owner-side links. User.DefaultActorId is preference only.
- Verified ATProto login can create an email-less User and claim an unowned Actor.
- Email and handle never locate or merge identities.
- Explicit account linking may merge a local Actor into the DID Actor; the DID Actor is always canonical.
- Mutable/current references move; immutable evidence remains and is correlated by ActorMerge.
- Imported Events already pointing at the DID Actor are never rewritten by claim.
- Instance admins own global Actor suspension; tenant admins own local presence hide/unhide.
- Public Actor counts and Event discovery use materialized current-tenant public Events only.
- Profile hydration is bounded, cached, outside transactions, and optional to Event import.
- Existing federation capability, canonical record, outbox, cursor, recovery, and zero-echo behavior remains authoritative.

## Constraints And Rules To Remember

- Repositories return entities; handlers map DTOs.
- Manually instantiate validators.
- Keep Clean Architecture dependencies inward.
- GET is anonymous; moderation/merge writes are authorized.
- HAL links are the only UI affordance authority.
- Global Actor lookup never implies tenant authorization.
- Do not use `IgnoreQueryFilters()` without a bounded reason and safety test.
- No PDS network call inside an EF transaction.
- No synthetic email, email auto-match, handle match, implicit cross-provider merge, or compatibility shim.
- Preserve unrelated worktree changes.
- Phase verification is one Release build plus at most one selected non-browser test project, run once after phase tasks.

## Validation Baseline

| Phase | Selected test project |
|---|---|
| 1 - Global schema/migration | `Event.Persistence.IntegrationTests` |
| 2 - Repositories/consumers | `Event.Architecture.Tests` |
| 3 - Canonical inbound Actor | `Event.Persistence.IntegrationTests` |
| 4 - Public profile hydration | `Explore.Infrastructure.Tests` |
| 5 - Claim/merge | `Event.Application.UnitTests` |
| 6 - API/moderation/discovery | `Event.API.IntegrationTests` |
| 7 - Blazor contract | `Explore.Blazor.Client.Tests` |

Every phase first runs `dotnet build --configuration Release --verbosity quiet`, then its one selected project command. Do not add app/browser/Docker/Aspire/manual smoke gates to routine phase verification.

## Current Known Risks / Unknowns

- Task 1.1 must classify Actor references in at least 21 EF configurations. Rewriting all or preserving all would both be wrong.
- Task 1.2 must fail on duplicate DID groups owned by different Users; no automatic resolution is safe.
- Task 5.1 must make email optional only where provider verification supports it, without weakening Keycloak/OIDC creation.
- Task 5.2 needs relation-specific collision behavior for subscriptions, memberships, and other unique Actor references.
- Task 4.1 must prove cached retry is sufficient before adding any durable profile-refresh queue.
- Task 6.3 intentionally allows canonical projection evidence to exist without a public card when materialization fails.

## Handoff Notes

### Handoff - 2026-07-25 Europe/Brussels

- **Current state:** Draft planning workstream created; no runtime implementation started.
- **Next action:** User reviews A1-A9 and approves or corrects the global Actor/migration model.
- **Blockers:** Approval required because linked-account-only behavior is being superseded.
- **Modified files:** The three files under `dev/active/atproto-federation-actor-lifecycle/` only.
- **Validation:** Baseline Release build was green; planning diff check is clean; Event.Architecture.Tests passed 302, failed 0, skipped 1 with existing package vulnerability warnings.
- **Documentation impact:** Planning docs only. Runtime/API/schema docs are assigned to owning implementation tasks.
- **Risks:** Actor FK classification, duplicate DID ownership conflict, nullable email breadth, merge collisions, and tenant-boundary leakage.
- **Notes for next contributor/agent:** Do not rebuild OAuth, Jetstream, canonical records, outbox, recovery, or Event materialization. Begin with ADR-016/FK disposition, not auth code.
