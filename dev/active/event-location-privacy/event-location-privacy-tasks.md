<!-- ABOUTME: Executable task checklist for the CTO-amended event-location privacy implementation. -->
<!-- ABOUTME: Sequences characterization, canonical EventLocation migration, disclosure authority, global erasure, outbox correction, UI, and verification. -->

# Event Location Privacy Tasks

**Status:** W1-W5, W7, and W8 complete; W6 partially complete (`ELP-230A`, `ELP-250`, `ELP-310` verified; `ELP-500` open); W9-W10 gates remain open
**Last Updated:** 2026-07-20 Europe/Brussels
**Plan:** `dev/active/event-location-privacy/event-location-privacy-plan.md`  
**Context:** `dev/active/event-location-privacy/event-location-privacy-context.md`

## Maintenance Rules

- Keep this file, the plan, and context synchronized after every task.
- Work in task-ID order unless the dependency table explicitly permits parallel work.
- Mark one implementation task `in progress` at a time; add evidence before checking it complete.
- Write the smallest failing test before non-trivial implementation.
- Record exact changed paths and commands/results in context.
- Never touch unrelated dirty-worktree changes.
- Stop for approval before weakening tenant isolation, public minimization, irreversible erasure, transaction atomicity, or governance floors.
- Generated clients and migrations are generated through canonical commands, never hand-edited.

## Execution Dependencies

| Phase | Entry gate | Exit gate |
|---|---|---|
| 0. Contract and characterization | Amended docs aligned | Leakage tests fail for known current behavior; Home Discovery blocker recorded |
| 1. Domain | Phase 0 complete | Kind/state/EventLocation/audience invariants pass Domain tests |
| 2. Registration access | Domain facts available | Event/Day/SessionSelection effective coverage passes exhaustive tests |
| 3. Persistence and migration | Domain stable | Expand/backfill/validate migrations and PostgreSQL tests pass |
| 4. Application disclosure | Persistence batch contracts stable | Pure evaluator and bounded batch service pass unit/integration budgets |
| 5. API/HAL/contracts | Application authority complete | Exact route/cache/auth split and generated contracts pass |
| 6. Global erasure/outbox/operations | EventLocation persistence and correction model complete | Atomic erasure, dispatch, restore replay, remediation pass |
| 7. Blazor | API/NSwag stable | HAL-gated accessible UX passes component/browser checks |
| 8. Outbound/discovery/docs | Disclosure authority stable | Every surface has evidence and operations/docs match behavior |
| 9. Final QA | All prior phases complete | Full build/tests/contracts/migrations/visual review green |

The exact execution order is the wave table in plan Section 16. Critical corrections are: `ELP-015` runs immediately after leakage/route characterization; `ELP-350` precedes `ELP-315`; historical `ELP-505`/`ELP-515` execution ownership is transferred to OREA with no ELP checkbox; `ELP-420A` precedes Blazor adoption; and `ELP-230C`, `ELP-430`, then `ELP-420B` form the final contraction lane.

## Phase 0: Contract and Characterization

- [x] **ELP-000 — Re-baseline the approved architecture across all three durable docs**
  - Paths: `dev/active/event-location-privacy/*`.
  - Result: canonical EventLocation, nullable-TBA XOR, retained internal scheduling IDs, deterministic legacy state, immediate Stage A, staged migrations, and verification owners are decision-complete. Authority topology is now owned by OREA: one-database default, explicit retained mode with no fallback, `local-full`-only provisioning, and Persistence/EF Core ownership of both ledgers and generated migrations.
  - Evidence: initial six-file protected-diff capture plus the historical 2026-07-16 two-database protocol review; OREA now supersedes that universal topology with the approved two-mode contract.

- [x] **ELP-005 — Block stale Home Discovery address/coordinate contract before product edits**
  - Paths: `dev/active/home-discovery-experience/home-discovery-experience-plan.md`, `home-discovery-experience-context.md`, `home-discovery-experience-tasks.md`.
  - Change: correct the false claim that `LocationListDto` omits private data; source currently exposes Address. Forbid browser enumeration of exact addresses/coordinates. Block current-location work on coarse `PublicDiscoveryArea` or a later governed PostGIS design.
  - Result: Home Discovery cannot reintroduce location leakage or treat `ShowCoordinates` as indexing consent.
  - Verify: documentation link/schema tests and grep for contradictory exact-coordinate/address guidance.
  - Evidence: four focused Architecture documentation/context tests passed; contradiction grep and six-file `git diff --check` passed; pre-existing Home Discovery implementation hunks remain present.

- [x] **ELP-010 — Add current-leakage characterization tests before contracts change**
  - Paths: `tests/Event.API.IntegrationTests/Features/LocationControllerTests.cs`, EventSession controller tests, `tests/Event.Application.UnitTests/Features/EventPrograms/`, Events calendar tests, and Blazor EventDetail tests.
  - Cases: anonymous Location detail/list address exposure; session/program/calendar/JSON-LD direct mapping; auth-cookie public response behavior; current private-address hint copy.
  - Result: tests document every current leak and become red/updated as authority is introduced.
  - Evidence: focused API/Application/Blazor/MCP tests characterize generic Location/room reads, public projections, filter URLs, cache headers, principal invariance, and private-address copy; the completed ELP-015 behavior makes them green.

- [x] **ELP-015 — Ship immediate Stage A fail-closed public minimization**
  - Dependencies: ELP-010 and ELP-400 characterization only; do not wait for Domain/schema work.
  - Paths: anonymous Location detail/list, public session/program/calendar projections, Blazor JSON-LD, MCP location output, and public filter URL construction plus their focused tests.
  - Change: preserve route shapes where safe, but omit every physical LocationId, address, postcode, coordinate, identifying name/city/room, and location-bearing fragment until EventLocation policy exists. If an old non-null contract cannot express a safe value, return no location/result rather than a fabricated neutral address.
  - Result: all known anonymous location surfaces are coarse or fail closed, and an auth cookie cannot enrich them.
  - Verify: ELP-010/400 tests turn green; API/Blazor/MCP contract assertions and source scan prove no compatibility fallback restores exact data.
  - Evidence: public session/group/agenda/program/calendar/JSON-LD surfaces redact physical fields and require public parent/child eligibility; public HAL is principal-invariant; generic reads and event management routes are authorized and `private, no-store`; MCP builders disclose zero physical values and the gateway requires exact ordered cardinality/identity. Managed/API ELP passed 19/19, Application ELP 18/18, API ELP 11/11, MCP ELP at least 10, authenticated MCP management at least 6, SDK at least 10, full Application 2,317/2,317, and full Blazor 1,702/1,702 executed with one not executed. Canonical OpenAPI/inventory/client hashes are recorded in context.
  - Boundary: the temporary management picker returns only locations/rooms already associated with that event. First/new venue selection remains fail-closed for non-admins until ELP-405/610; neither task is complete.

- [x] **ELP-020 — Freeze outbound surface inventory and purpose table**
  - Paths: plan Section 11, context outbound inventory, `docs/API.md`, `docs/SECURITY-MODEL.md`.
  - Inventory: sessions/program/agenda/JSON-LD, public/attendee calendars, email/reminders, tickets/QR, Svix webhooks, CSV/JSON, search, moderation, API keys, print/PDF/reports, MCP/AI, federation/PDS, discovery.
  - Result: each surface has owner, audience, purpose, allowed fields, cache policy, and target test.
  - Evidence: plan Section 11 and the context inventory record every named surface, current owner/absence proof, correction behavior, and focused test path; completeness grep passed.

- [x] **ELP-030 — Capture API/OpenAPI/generated-client and migration baselines**
  - Paths: `src/Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs`, `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`, current EF snapshot, API contract snapshots.
  - Result: known baseline and exact regeneration commands recorded in context.
  - Verify: generation commands produce no unexplained pre-change diff.
  - Evidence: context records commit, migration head, seven SHA-256 artifact hashes, dirty/clean capture state, and workflow-exact deterministic commands. Generators were deliberately not run over three protected concurrent Home Discovery artifacts; ELP-420A/B must compare against this byte baseline before accepting generated changes.

- [x] **ELP-040 — Lock instance/tenant governance source and most-restrictive merge**
  - Paths: `src/Explore.Application/Contracts/Services/ITenantPolicySettingService.cs`, `src/Explore.Application/Services/TenantPolicySettingService.cs`, `TenantPolicySettingService.Read.cs`, `TenantPolicySettingService.Apply.cs`, planned `src/Explore.Application/Contracts/Services/ILocationPrivacyGovernanceService.cs`, and `src/Explore.Application/Services/LocationPrivacyGovernanceService.cs`.
  - Keys: `allow_home_locations`, `allow_public_exact_address`, `allow_public_coordinates`, `minimum_home_audience`, `default_reveal_offset` under `location_privacy`.
  - Result: exact ownership, precedence, validation, and audit paths recorded before schema work.
  - Evidence: plan Section 9 and context lock SystemSetting/TenantSetting ownership, field-by-field merge lattice, JSON formats/ranges, fail-closed defaults, widening rejection, setting audit, correction outbox, and cache invalidation owners/tests.

- [x] **ELP-060 — Lock LocationKind, LocationPrivacyState, and audience code contracts in tests**
  - Paths: `src/Explore.Domain/LocationKind.cs`, `LocationPrivacyState.cs`, `LocationDisclosureAudience.cs`, enum companions, and `tests/Event.Domain.UnitTests/LocationPrivacyLookupContractTests.cs`.
  - Cases: exact stable IDs/master codes for Unclassified and all kind/state/audience values; kind remains descriptive and never grants disclosure.
  - Result: persisted lookup vocabulary cannot drift. Aggregate owner/PII/erasure/resurrection invariants remain owned by open task ELP-120.
  - Evidence: Domain lookup contract 2/2; Domain build 0 errors; Clean Architecture/Code Hygiene/Naming green.

- [x] **ELP-070 — Lock global-account deletion versus tenant-membership removal semantics**
  - Paths: `docs/MULTI_TENANCY.md`, `src/Explore.Application/Features/Users/Handlers/Commands/DeleteUserCommandHandler.cs`, planned `src/Explore.Application/Features/TenantUsers/Requests/Commands/RemoveTenantMembershipCommand.cs`, `src/Explore.Application/Features/TenantUsers/Handlers/Commands/RemoveTenantMembershipCommandHandler.cs`, and matching tests under `tests/Event.Application.UnitTests/Features/TenantUsers/Commands/`.
  - Result: global deletion erases every owned Home across tenants; membership removal changes TenantUser/TenantUserProfile only.
  - Verify: architecture test forbids membership handlers from invoking global privacy erasure.
  - Evidence: plan/context ownership tables name global and membership handlers, repository/transaction boundaries, negative mutation rules, unit/integration owners, and the `EventLocationPrivacyArchitectureTests` dependency prohibition.

## Phase 1: Domain Model

- [x] **ELP-100 — Add normalized LocationKind lookup**
  - Paths: `src/Explore.Domain/LocationKind.cs`, `Enums/LocationKindEnum.cs`, EF configuration/DbSet, repair seeder, and focused Domain/Persistence tests.
  - Values: `UNCLASSIFIED`, `COMMERCIAL_VENUE`, `PUBLIC_SPACE`, `COMMUNITY_VENUE`, `PRIVATE_HOME`.
  - Result: stable int lookup; no behavior grants disclosure by kind.
  - Evidence: IDs 1-5 map to `UNCLASSIFIED`, `COMMERCIAL_VENUE`, `PUBLIC_SPACE`, `COMMUNITY_VENUE`, and `PRIVATE_HOME`; Domain 2/2, Persistence 2/2, PostgreSQL startup/idempotency 1, and Domain/Persistence builds have 0 errors.

- [x] **ELP-110 — Add normalized LocationPrivacyState and audience lookups**
  - Paths: `src/Explore.Domain/LocationPrivacyState.cs`, `LocationDisclosureAudience.cs`, enum companions, EF configurations/DbSets, repair seeder, and focused Domain/Persistence tests.
  - Values: NotProvided/Active/Erased and Never/AnyCurrentRegistrant/ConfirmedParticipant.
  - Result: backend values are stable and separate from UI labels.
  - Evidence: state IDs 1-3 map to `NOT_PROVIDED`, `ACTIVE`, `ERASED`; audience IDs 1-3 map to `NEVER`, `ANY_CURRENT_REGISTRANT`, `CONFIRMED_PARTICIPANT`. The idempotent repair method is tested directly, and verified ELP-230A now creates the tables with migration-local inserts before activating the global repair seeder. Current repository-wide pending-model output is non-green only because concurrent shared model/snapshot work continues beyond the verified Expand pair.

- [x] **ELP-120 — Implement Location lifecycle and consent-backed Home ownership**
  - Paths: `src/Explore.Domain/Location.cs`, `LocationPii.cs`, `LocationRoom.cs`, `LocationOwnershipConsent.cs`, Domain tests.
  - Change: kind/state/owner/erasure fields; `EraseOwnedPii()` tombstone; reject PII recreation; current-user default owner; explicit consent transfer.
  - Result: irreversible aggregate lifecycle with optimistic concurrency.
  - Evidence: `LocationPrivacyLifecycleTests` prove optional PII, same-aggregate attachment, owner-stable Private Home classification, versioned explicit transfer consent, irreversible erasure, unique room tombstones, identifying-label restoration rejection, and replacement-by-new-Location behavior. The combined lifecycle/EventLocation Domain category passed 50/50; Domain Release build and architecture/hygiene/naming gates were green, followed by a clean independent re-review.

- [x] **ELP-125 — Add canonical first-class EventLocation and migrate aggregate references conceptually**
  - Paths: `src/Explore.Domain/EventLocation.cs`; EventSession, EventSessionGroup, EventAgendaItem, EventSessionAgendaItem, and their persistence configurations.
  - Change: field selections, audience, reveal time, review flag, policy version, concurrency/audit/soft-delete; nullable `LocationId` only for explicit TBA with a database XOR; nullable EventLocationId references prepared for migration.
  - Integrity: retain internal physical LocationId on session/group/agenda carriers where composite room-containment and GiST overlap rules require it; constrain it to match EventLocation and never expose it as public authority.
  - Lifecycle: server fail-closed creation; final detach soft-delete; reattach fresh association.
  - Result: every event-local physical place is mediated by one EventLocation.
  - Evidence: `EventLocationTests` prove UUIDv7 identity/concurrency, immutable tenant/event/physical identity, exactly-one physical-or-TBA shape, fail-closed v1 policy, independent per-event policies, terminal detach, and publication readiness. EventSession, EventSessionGroup, EventAgendaItem, and EventSessionAgendaItem carry authoritative EventLocationId and clear stale physical room keys on TBA/physical changes; verified ELP-230A now supplies database cross-table enforcement.

- [x] **ELP-130 — Encode contextual field matrix including rooms and operational secrets**
  - Paths: `src/Explore.Application/DTOs/Location/EventLocationDisclosureContract.cs` and `tests/Event.Application.UnitTests/Services/EventLocationDisclosureContractTests.cs`; ELP-310 owns evaluator implementation and ELP-740 owns canonical documentation.
  - Cases: country/timezone baseline; city/name/room contextual; room description management-only; exact derivatives sensitive; access instructions never public; Private Home generic label.
  - Result: field decisions are explicit and executable, not inferred from table location.
  - Evidence: `EventLocationDisclosureContractTests` passed 17/17 across all 16 fields and Public/Attendee/Management ceilings. One contract owns derivative source authority, timezone fails closed pending explicit policy, operational secrets have no route purpose, and public Private Home output permits only `Private venue` with no city/room/address/postcode/coordinates/derivatives.

- [x] **ELP-140 — Add EventLocation policy and exact-read audit models**
  - Paths: `src/Explore.Domain/EventLocationDisclosureAudit.cs`, `EventLocationExactReadAudit.cs`, `LocationPrivacyErasureAuthorityIntent.cs`, `LocationPrivacyErasureReplayCheckpoint.cs`, typed privacy-audit enums, and tests.
  - Result: append-only old/new policy and audience/version audit, UUIDv7 idempotency/monotonic authority sequence facts, local checkpoint, plus PII-free exact-read audit; no address values.
  - Evidence: typed closed reason/purpose enums, nonempty GUID trace/correlation IDs, immutable audit/intent/checkpoint records, and negative structural tests prevent durable free-text PII fields. Initial and changed policy evidence is aggregate-derived and contiguous; the combined Domain EventLocationPrivacy lane passed 62/62 before persistence and 63/63 after it.

- [x] **ELP-150 — Add explicit Location To Be Announced remediation state**
  - Paths: `src/Explore.Domain/EventLocation.cs`, `tests/Event.Domain.UnitTests/EventLocationTests.cs`, and publication validation tests.
  - Result: `EventLocation.IsToBeAnnounced=true` is explicit, suppresses every physical-location field, permits publication without a usable physical venue, and is never inferred from erasure or missing PII; unusable required physical venues otherwise block publication.
  - Evidence: Domain tests prove explicit TBA is publishable, while physical publication requires a tenant/identity-matching Active Location with nonblank address and postcode; missing, mismatched, NotProvided, Erased, or empty PII fails closed.

## Phase 2: Registration-Intent Access

The following ELP-200/210 tables are authoritative and synchronized with plan Section 7. `Approved` is the current persisted approval term; the disclosure resolver treats it as the `Confirmed`-equivalent effective state.

| Registration mode when approval is null | Effective result | Disclosure consequence |
|---|---|---|
| `Open` | `Approved` (`Confirmed`-equivalent) | May qualify for attendee disclosure only after requested-placement scope coverage and every policy/governance/reveal gate pass. |
| `ApprovalRequired` | `Pending` | Qualifies only for organizer-selected `ANY_CURRENT_REGISTRANT`; never for `CONFIRMED_PARTICIPANT`. |
| `InviteOnly` | Deny unless a separate authority proves a valid invitation | No invitation authority exists today, so null approval denies registration and location access. |
| `Closed` | Deny | Do not create an access-bearing intent. |

| Persisted/lifecycle fact | Effective disclosure state | Location disclosure ceiling |
|---|---|---|
| `Approved` | `Confirmed` | Exact disclosure may proceed only when the intent scope covers the requested EventLocation placement and all remaining gates pass. |
| `Pending` or `Waitlisted` | Same state | `ANY_CURRENT_REGISTRANT` only, with requested-placement coverage; never confirmed-participant authority. |
| `Rejected`, `Cancelled`, or `Revoked` | Same terminal state | Deny. |
| Soft-deleted or expired intent/registration | Non-live | Deny. |

`ApprovalStatus` now persists stable `Cancelled=5`/`CANCELLED` and `Revoked=6`/`REVOKED` values. Partial cancellation removes only the cancelled child coverage while remaining live children keep the parent intent live; last-child cancellation synchronizes the parent terminal state. Pending/Approved consume capacity and terminal transitions release it. ELP-210 provides the immutable pure EventLocation entitlement result. Verified ELP-225 loads exact tenant/event/user registration entities and derives current Event/Day/SessionSelection placement coverage.

- [x] **ELP-200 — Characterize registration intent lifecycle and null approval by mode**
  - Paths: `src/Explore.Domain/EventRegistrationIntent.cs`, `EventRegistration.cs`, `ApprovalStatus.cs`, `Enums/ApprovalStatusEnum.cs`, `Enums/RegistrationModeEnum.cs`, `Enums/RegistrationScopeEnum.cs`, `Services/Registration/RegistrationPolicyRules.cs`, registration handlers/repositories and tests.
  - Result: the authoritative tables above are executable; stable persisted `Cancelled`/`Revoked` vocabulary lands before attendee authority, and null approval is never guessed.
  - Security/capacity result: PATCH cannot reassign event, intent, or user identity. Attendees may DELETE only their own registration and never PATCH; organizer/admin management remains policy-authorized. Authorization enrichment captures persisted tenant/event/session/user ownership and the serializable repository transaction revalidates that snapshot against ownership races. Pending/Approved reserve capacity; leave/move/terminal transitions release it, and a full destination waitlists atomically. Child and parent terminal evidence remains synchronized.
  - Evidence: approval rules at least 24, policy rules exactly 30, create handler at least 18, validator at least 9, update at least 11, PostgreSQL registration repository at least 16, intent repository at least 11, approval seeder at least 1, Persistence ELP 20, Domain ELP at least 30, own-cancel Application 22/22, PostgreSQL cancellation/race 8/8, real Cerbos 2/2, native Cerbos 461/461, architecture/parity 24/24, and full Release build 26 projects with 0 errors.

- [x] **ELP-210 — Add EventLocationRegistrationAccess immutable result and effective-state resolver**
  - Paths: `src/Explore.Application/Contracts/Services/EventLocationRegistrationAccess.cs`, `IEventLocationRegistrationAccessService.cs`, `src/Explore.Application/Services/EventLocationRegistrationAccessService.cs`, and `tests/Event.Application.UnitTests/Services/EventLocationRegistrationAccessServiceTests.cs`.
  - Fields: intent ID, scope, effective state, event/day/session coverage, requested EventLocation coverage.
  - Result: one fail-closed entitlement fact for the evaluator; it applies the tables above, ignores cancelled child coverage, and denies a non-live parent.
  - Evidence: `EventLocationRegistrationAccessServiceTests` passed 42/42. The sealed result has a non-public constructor; the pure resolver validates identity/coverage, maps Approved to Confirmed, resolves null by registration mode, caps Pending/Waitlisted at AnyCurrentRegistrant, and denies terminal, deleted, expired, non-live, malformed, or uncovered facts. Repositories do not return this authority fact.

- [x] **ELP-225 — Implement Event, Day, and SessionSelection EventLocation coverage**
  - Paths: planned `src/Explore.Application/Services/EventLocationRegistrationAccessService.cs`, `src/Explore.Application/Contracts/Persistence/IEventRegistrationRepository.cs`, `src/Explore.Persistence/Repositories/EventRegistrationRepository.cs`, `tests/Event.Application.UnitTests/Services/EventLocationRegistrationAccessServiceTests.cs`, and Persistence integration tests.
  - Rules: Event covers all eligible; Day covers eligible items on selected day; SessionSelection covers selected sessions only; no active intent denies.
  - States: Pending/Waitlisted broad only; Confirmed both; Rejected/Cancelled/Revoked/deleted deny.
  - Result: cross-day/session/location over-grant is impossible.
  - Evidence: one tenant-filter-preserving `AsSingleQuery`/`AsNoTrackingWithIdentityResolution` read returns `EventRegistration` entities and bypasses only the named soft-delete filter. Application revalidates every tenant/event/user/navigation identity, excludes deleted or expired facts, derives Event/Day coverage from the current placement graph, keeps SessionSelection child-bound, and chooses the strongest live intent with a stable tie-break. The 62-case Application matrix passed 62/62; disposable PostgreSQL coverage passed 3/3 on current owned binaries; Clean Architecture passed 15/15, tenant-filter architecture 4/4, and Code Hygiene 4/4. The provider probe observed one reader for 300 requested IDs, three deduplicated results, zero tracked entries, and end-to-end cancellation-token forwarding. A fresh Persistence-test rebuild remained externally blocked by two concurrent `NotificationFanoutOccurrenceRepositoryTests` required-member compile errors; no repository-wide green claim is made. Independent verdict: `confirmed` at 0.96 confidence in `.omo/evidence/task-9-registration-coverage-adversarial-verify.md`.

## Phase 3: Persistence and Migration

- [x] **ELP-230A — Generate focused expand migration**
  - Paths: planned configurations `LocationKindConfiguration.cs`, `LocationPrivacyStateConfiguration.cs`, `LocationDisclosureAudienceConfiguration.cs`, `EventLocationConfiguration.cs`, `EventLocationDisclosureAuditConfiguration.cs`; `ExploreDbContext.cs`, `ExploreDbContext.QueryFilters.cs`, generated migration/snapshot.
  - Change: lookup seeds, Location lifecycle columns, optional PII, EventLocation/audit/local replay-checkpoint tables, nullable EventLocationId references, TBA/location XOR, filtered uniqueness, indexes/checks/tenant-safe consistency FKs. Optional authority provisioning and its dedicated EF migration are outside ELP-230A and owned by OREA-120/140/300.
  - Result: additive schema supports fail-closed dual-write with valid Down before irreversible activation.
  - Verify: generated SQL reviewed for locks, defaults, tenant keys, UUIDv7, concurrency, local checkpoint, and rollback; `Database:Migrations:EventLocationPrivacyStage=Expand|Backfill|Contract` is required while ELP migrations are pending, and a missing/invalid selector cannot auto-apply backfill/contract.
  - Evidence: `20260716132239_AddEventLocationPrivacyExpand`, its generated Designer/snapshot state, `EventLocationPrivacyMigrationStage`, `ExploreDatabaseMigrator`, API startup, MigrationService startup, lookup-seeder activation, and `EventLocationMigrationStageTests` are present. Independent PostgreSQL 18 verification passed the original five migration-stage tests and the Persistence `EventLocationPrivacy` category 40/40; exact 783-line idempotent Expand SQL applied twice, fresh/legacy upgrade and pre-activation Down preserved legacy rows, and raw SQL rejected append-only audit deletion, erased-PII reattachment/resurrection, tombstone restoration, carrier mismatches, and referenced-association detach. The later stage-gate repair makes already-applied Expand/Backfill targets idempotent before later-pending-stage checks and forwards a configured nonblank stage from AppHost to both MigrationService and API; focused applied-stage retry passed 2/2, the full PostgreSQL stage suite passed 7/7, and AppHost architecture passed 7/7. Missing/blank/invalid/unavailable stages still fail closed. Live Aspire proved stage forwarding but stopped on persisted PostgreSQL/RabbitMQ credential mismatch, so migration-service exit 0 and healthy API are not claimed. Current `has-pending-model-changes` is separately non-green because concurrent shared model/snapshot work exists, so no repository-wide model-parity claim is made. Migration implication: Expand and the separately verified Backfill are complete; Contract and disclosure activation remain gated. Cache implication: none. Security implication: selector ceilings and PostgreSQL guards fail closed. Risk/next: preserve the externally invalid shared snapshot/current-head state until the authorized migration lane reconciles it.

- [x] **ELP-230B — Implement idempotent Unclassified and EventLocation backfill**
  - Paths: generated `src/Explore.Persistence/Migrations/*_BackfillUnclassifiedEventLocations.cs`, EF snapshot, and planned `tests/Event.Persistence.IntegrationTests/Migrations/EventLocationBackfillTests.cs`.
  - Rules: every legacy Location => Unclassified; PII present => Active, PII absent => NotProvided, never legacy Erased; unique tenant/event/location EventLocation; country only; city only with recorded continuity exception; all other fields false; audience Never; NeedsPrivacyReview true; never infer Home/owner.
  - Result: repeat-safe backfill plus unresolved review metrics.
  - Gate: operator-selected `Backfill` target; zero-gap verification succeeds before policy activation.
  - Evidence: `20260718215537_BackfillUnclassifiedEventLocations` conservatively covers all four real carrier tables, preserves event-local pair identity, derives lifecycle only from `LocationPii`, writes country-only/audience-Never/review-required UUIDv7 associations plus PII-free typed audits, and fills only missing authority. Fresh PostgreSQL acceptance passed 3/3 twice with stable replay hashes/IDs, later-row repair, atomic malformed failure, zero carrier gaps, guarded/safe `Down`, forward convergence, and existing dual-writes preserved; migration stages passed 7/7, Persistence privacy 54/54, and Clean Architecture 15/15. The Backfill source and Designer remain byte-identical to HEAD, and the repaired acceptance test hash is `b9223483e750ce075bbe5242fa0cc3cd425f3b5789cc3d308ce33d2eb74ad649`. The earlier 18,594-line pre-concurrency SQL artifact applied twice at its then-current 67-migration head, but a concurrent Reconcile migration plus externally emptied shared snapshot invalidated later current-head/model-parity proof; neither is claimed. Independent verdict: `confirmed` at 0.98 confidence in `.omo/evidence/task-9-backfill-adversarial-verify.md`. Disclosure remains inactive until the remaining contraction and consumer gates pass.

- [ ] **ELP-230C — Validate zero-gap data and contract old references**
  - Paths: focused contract migration, verification SQL in `docs/OPERATIONS.md`, integration tests.
  - Gate: zero missing EventLocationId, orphan, duplicate active pair, tenant mismatch, invalid Home state, resurrected Erased PII.
  - Result: required EventLocation references and obsolete public/contract references removed only after all consumers migrate; internal physical scheduling IDs required for composite room/GiST integrity remain consistency-constrained.
  - Gate: operator-selected `Contract` target runs only in W18 after ELP-420A client adoption and all consumers; automatic startup migration cannot collapse A/B/C. ELP-420B regenerates the final client afterward.

- [x] **ELP-240 — Add EventLocation repositories and bounded batch loading**
  - Paths: `src/Explore.Application/Contracts/Persistence/IEventLocationRepository.cs`, `src/Explore.Persistence/Repositories/EventLocationRepository.cs`, related audit/checkpoint contracts and repositories, registration, and tests.
  - Result: entity-returning, AsNoTracking read batches; tracked mutation; tenant-safe unique active association; no DTO projection.
  - Evidence: Application persistence contracts and repositories cover EventLocation, disclosure audit, exact-read audit, and replay checkpoint without DTO, `IQueryable`, or DbContext leakage. Reads are bounded/ordered/no-tracking, mutations tracked, and missing tenant context fails closed. Repository integration passed 12/12 and relational model verification passed 1/1; SaveChanges rejects tenant/event/location/room/parent-session mismatches across all four carriers, and verified ELP-230A now backs those checks with database triggers.

- [x] **ELP-250 — Add named global privacy-erasure repository query**
  - Paths: `ILocationRepository.cs`, `LocationRepository.cs`, `ExploreDbContext.QueryFilters.cs`, architecture and PostgreSQL tests.
  - Rule: explicit tenant-filter bypass strictly bounded by OwnerUserId and PrivateHome; no general unrestricted query.
  - Result: all current/former-tenant owned Homes found without cross-user leakage.
  - Evidence: `ILocationRepository.GetOwnedPrivateHomesForGlobalErasureAsync` returns tracked entities, rejects `Guid.Empty` before EF access, disables only the named tenant filter for `GlobalLocationPrivacyErasure`, and immediately bounds by exact `OwnerUserId` plus `PrivateHome` with tenant/id ordering. Two fresh PostgreSQL 18 runs passed 2/2 each using two owned Homes across tenants, an unrelated-owner Home, and a transactionally injected same-owner commercial counterexample; both `Location` and AutoIncluded `LocationPii` were tracked and the DDL/DML mutation rolled back. Application contract passed 1/1, tenant-filter architecture 4/4, Clean Architecture 15/15, Code Hygiene 4/4, and the root Release build completed 26 projects with 0 errors. Migration/cache implications: none. Security implication: no unrestricted cross-tenant enumeration or DTO/`IQueryable` escape. Risk/next: the query is only the read seam; ELP-500 retains characterization ownership while OREA owns erasure orchestration.

- [x] **ELP-260 — Persist policy audit, authority client/checkpoint, and concurrency**
  - Paths: EventLocation audit/exact-read/checkpoint repositories and configurations; `src/Explore.Application/Contracts/LocationPrivacy/`; `src/Explore.Infrastructure/Privacy/ErasureAuthority/`; two-database integration tests.
  - Result: concurrency-token conflicts produce stable API errors; UUIDv7 append is idempotent, authority sequence/checkpoint are monotonic, app restore cannot overwrite independently retained authority facts, and payloads are PII-free.
  - Persistence evidence: EventLocation creation atomically writes truthful aggregate-derived `0→1` `AssociationCreated` audit; later writes require contiguous versions matching the aggregate and competing writers yield one winner plus stable `concurrent_update`. Disclosure/exact-read audits and checkpoints are tenant-filtered and append-oriented. Persistence repository passed 12/12, model 1/1, Domain EventLocationPrivacy 63/63, and final independent persistence review returned PASS with no high/medium finding.
  - Separate-authority evidence: `ILocationPrivacyErasureAuthority` and `PostgreSqlLocationPrivacyErasureAuthority` use typed PII-free facts and an independently configured PostgreSQL database. A dedicated NOLOGIN owner exposes fixed-search-path `SECURITY DEFINER` append/read functions to execute-only runtime; transactional counter allocation is globally serialized, UUIDv7 RFC variant is checked in client/Domain/SQL, normalized duplicates are idempotent, mismatched duplicates reject, server owns UTC metadata, and reads are ordered/bounded to 500. PostgreSQL authority tests passed 16/16 across concurrency, rollback/failed insert, ambiguous acknowledgement, cancellation, runtime table/counter denial, and application-database recreation; final authority re-review returned PASS.

## Phase 4: Application Disclosure Authority

- [x] **ELP-300 — Add purpose-specific EventLocation DTOs and requests**
  - Paths: `src/Explore.Application/DTOs/Location/EventLocationDisclosureContract.cs`, `EventLocationDisclosureRequest.cs`, `EventLocationDisclosureResult.cs`, and `EventLocationDtos.cs`.
  - Result: public DTO exposes EventLocationId only; attendee/management shapes are separate; no generic exact LocationDto reuse.
  - Evidence: separate public, attendee, management, policy-update, internal request, and constrained result contracts use closed purpose-specific factories. Public exposes EventLocationId but no physical LocationId; suppressed Hidden/TBA/unavailable/review states cannot carry values. Disclosure contract tests passed 17/17 and final security/code-quality reviews found no high/medium issue.

- [x] **ELP-310 — Implement pure EventLocationDisclosureEvaluator with exhaustive tests**
  - Paths: `src/Explore.Application/Services/EventLocationDisclosureEvaluator.cs`, unit tests.
  - Order: tenant/association, privacy state, purpose ceiling, governance, authorization/entitlement, server time, field policy, contextual redaction.
  - Result: deterministic fail-closed matrix including Private Home and TBA.
  - Evidence: `EventLocationDisclosureEvaluator`, `EventLocationDisclosureResult`, and their focused tests now distinguish malformed facts as `Hidden` from valid `NotProvided`/`Erased` as `Unavailable`, always with null values, empty fields, and no physical `LocationId`. Management disclosure no longer accepts or materializes physical identity. Failing-first evidence recorded 67/72 evaluator and 16/17 contract before repair; the final evaluator command passed 72/72 twice, with contract 17/17, registration access 42/42, governance 15/15, Clean Architecture 15/15, and Code Hygiene 4/4. Direct runtime probes confirmed invalid identity/enums/cross-location PII/stale lifecycle fail closed, valid lifecycle remains unavailable, Private Home stays generic until authorized, and reveal uses only supplied UTC server time. Migration/cache implications: none; the evaluator is synchronous, dependency-free, and I/O-free. Security implication: no purpose returns physical `LocationId`. Risk/next: service/query budgets and route activation remain open under `ELP-315`/`ELP-405`; W8 is now verified and W9 owns the next dependency gates.

- [ ] **ELP-315 — Implement batched EventLocationDisclosureService and enforce query/auth budgets**
  - Paths: `src/Explore.Application/Contracts/Services/IEventLocationDisclosureService.cs`, `Services/EventLocationDisclosureService.cs`, request/result records, unit/integration tests.
  - API: `ResolveManyAsync(IReadOnlyCollection<EventLocationDisclosureRequest>, CancellationToken)`.
  - Result: deduplicated immutable EventLocationId-keyed result; bounded association/location+PII/room/registration/governance queries; one batched manager authorization; no N+1.
  - Dependencies: ELP-340 governance and ELP-350 batched manager authorization are complete.
  - Implementation evidence: `EventLocationDisclosureService` accepts at most 256 requests, normalizes public identity away, derives private identity from `ICurrentUserService`, permits a public batch to span events within one tenant/purpose, and requires private batches to remain within one event/requester. Duplicate EventLocation requests with conflicting room contexts are conservatively normalized to no room disclosure. The service returns an immutable EventLocationId-keyed dictionary after one bounded EventLocation read with Location/PII, one bounded room read, one governance resolution, and then either one tenant/event/user registration-coverage read or one ELP-350 manager authorization/audit batch. All I/O completes before the result loop, which only constructs immutable authority facts and calls the pure evaluator; missing associations, rooms, entitlement, governance, or authorization therefore materialize fail closed without per-row database or provider calls. Application Release compiled 2 projects with 0 errors; Persistence Release compiled 4 projects with 0 errors; `git diff --check` passed. Tests remain deferred, so the task stays unchecked.

- [ ] **ELP-320 — Migrate public session/program/agenda backend projections**
  - Paths: public EventSession query handlers, `GetEventProgramSummaryRequestHandler.cs`, agenda handlers, `EventSessionMappingProfile.cs`; ELP-440 owns calendar builders/routes and ELP-650 owns JSON-LD.
  - Result: no direct Location/PII/room mapping; batch disclosure used once per response.
  - Implementation evidence: public session, session-group, event-agenda, session-agenda, merged-agenda, and program handlers now build event-scoped placements and resolve one public `IEventLocationDisclosureService` batch per response. A shared projection seam maps only `EventLocationDisclosureResult` into nested `EventLocationPublicDto`; legacy physical Location/room fields remain explicit null compatibility seams. AutoMapper ignores those fields, including the duplicate EventSessionGroup maps, so it cannot traverse `Location`, `LocationPii`, or `LocationRoom` directly. Conflicting room contexts for a shared EventLocation omit the room rather than choosing one. Application Release compiled 2 projects with 0 errors and 273 warnings; Persistence Release compiled 4 projects with 0 errors and 789 warnings; the focused physical-field source scan and `git diff --check` passed. Tests remain deferred, so the task stays unchecked.

- [x] **ELP-330 — Migrate event/location creation and attachment commands to server-created fail-closed EventLocation**
  - Paths: Event create/update/import/draft handlers and every `*CommandHandler.cs` under EventSessions, EventSessionGroups, EventAgendaItems, EventSessionAgendaItems, and LocationRooms that attaches/detaches LocationId.
  - Result: dual-write during migration; final detach soft-deletes; reattach fresh association; clients cannot omit policy creation.
  - Evidence: `EventLocationAttachmentService` resolves the unique active physical/TBA association or creates a fail-closed audited one; all four carrier families assign it on create/draft/update and detach it after the final live delete or reassignment inside the same application transaction. Event moves create event-scoped associations, clear operations become explicit TBA, reattachment never resurrects a soft-deleted association, and referenced rooms cannot be moved across locations. Development seeding is now an in-scope writer: its 36 carriers map to 8 distinct active authorities with one initial audit each, with authority/carrier IDs stable after the second seed. Independent verification passed the real-PostgreSQL seeder 6/6, dual-write 8/8 twice, attachment service 9/9, session-agenda handlers 6/6, strict fixture-dependent handlers 42/42, Clean Architecture 15/15, Code Hygiene 4/4, and the root Release build with 0 errors; the verifier returned `confirmed` at 0.99 confidence. Migration/cache implications: none beyond the already-expanded authority schema. Security implication: every production and development carrier writer now fails closed through the server-owned authority. Risk/next: disclosure remains inactive; W8 backfill, registration coverage, and governance are now verified, and W9 owns the next dependency gates.

- [x] **ELP-340 — Implement governance composition, server-time reveal, and policy-version invalidation**
  - Paths: governance contract/implementation, EventLocation update handler, `src/Explore.Application/Caching/CacheTags.cs`, tests.
  - Result: most restrictive rule wins; reveal uses server UTC plus entitlement; tightening invalidates all affected projections.
  - Evidence: the five typed location-privacy keys merge through the most restrictive lattice, malformed values fail closed, tenant writes cannot widen instance ceilings, and reveal decisions use controlled server UTC. Setting rows, EventLocation policy-version/review corrections, disclosure audits, and PII-free `location.privacy.corrected` outbox rows stay inside the shared transaction; the five typed setting notifications and deduplicated global/tenant/event/association invalidation run only after the outer commit, while rollback emits neither. Governance services passed 15/15, API governance 2/2, Domain privacy audit 8/8, selected cache/architecture 5/5, PostgreSQL EventLocation repository 12/12, handler commit/rollback ordering 10/10, and the single-key post-commit path 1/1; a fresh Explore.API Release build had 0 errors and 0 warnings. The root Release build remained externally blocked by concurrent notification-test compile errors, and one unrelated EmailDispatch authorization-parity failure remains; neither is counted as ELP-340. Independent verdict: `confirmed` at 0.99 confidence in `.omo/evidence/task-9-governance-adversarial-verify.md`.

- [ ] **ELP-350 — Add management authorization and PII-free exact-read security audit**
  - Paths: authorization descriptors/handlers following `IAuthorizedRequest` / resource policy conventions; exact-read audit service; tests.
  - Result: manager exact reads fail closed and are audited without values; UI gets HAL affordances only.
  - Implementation evidence: `EventLocationManagementAuthorizationService` normalizes a tenant-bounded batch, loads all parent `Event` authorization targets once, evaluates the existing `event:view-management` action in one `IAuthorizationProvider.IsAllowedBatchAsync` call, and maps missing targets, missing decisions, or provider failures to deny. Every returned allow/deny decision is persisted first through `EventLocationExactReadAuditService`; its contract accepts only identities, typed purpose, decision, server UTC, and correlation/trace metadata, never physical-location values. Audit persistence revalidates all active current-tenant `EventLocation` IDs in one bounded query and appends the batch in one save, so audit failure prevents exact-read decisions from being returned. The focused Application Release build compiled 2 projects with 0 errors and `git diff --check` passed. The Persistence build is temporarily blocked by unrelated in-progress email-outbox interface changes; tests remain deferred, so the task stays unchecked.

- [ ] **ELP-360 — Implement EventLocation policy concurrency and append-only audit**
  - Paths: policy update command/handler/validator, audit repository, Application and Persistence tests.
  - Result: expected concurrency token and PolicyVersion required; old/new selection/audience/reveal metadata recorded; addresses absent.

## Phase 5: API, HAL, and Contracts

- [x] **ELP-400 — Add route-level authorization/cache characterization tests**
  - Paths: Event/Location API integration tests.
  - Cases: anonymous/auth-cookie equivalence, unauthorized attendee/manager, no-store headers, tenant mismatch, stale policy version.
  - Result: Stage-A public responses are identical across anonymous/authenticated principals and physical values are absent; generic Location/room and temporary managed event routes require resource authorization, reject cross-tenant/cross-event enumeration, and send `private, no-store`. Final EventLocation stale-policy-version behavior remains with ELP-405/810.
  - Evidence: managed/API ELP 19/19, API public ELP 11/11, public eligibility 2, native Cerbos 461/461, language security 3/3 plus handler 5/5/controller 2/2, and API Release build 7 projects with 0 errors/0 warnings.

- [ ] **ELP-405 — Implement exact public/attendee/management route split**
  - Paths: planned `src/Explore.API/Controllers/EventLocationController.cs` and `src/Explore.API/Hateoas/RouteNames.cs`.
  - Routes: public `/api/events/{eventId}/locations`; attendee `/my-access`; management `/{eventLocationId}/management`; disclosure PUT.
  - Result: public always public-only/no shared cache v1; attendee/management authorized/private/no-store.

- [ ] **ELP-410 — Add EventLocation HAL policies and assemblers**
  - Paths: planned `Hateoas/Policies/EventLocationLinkPolicy.cs`, `Hateoas/Assemblers/EventLocationResourceAssembler.cs`, `Extensions/HateoasAssemblerRegistration.cs`, tests.
  - Result: server-authorized edit/disclosure/owner-transfer/remediation links; no client role logic.

- [ ] **ELP-420A — Generate additive OpenAPI/HAL schema and NSwag client**
  - Paths: `OpenApi/HalOpenApiSchemaCatalog.cs`, API changelog, generated `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, serializer context.
  - Result: purpose-specific additive contracts and EventLocationId are available for ELP-600 adoption; generated artifacts clean and never hand-edited.

- [ ] **ELP-420B — Regenerate and prove final OpenAPI/HAL contract**
  - Dependencies: every backend and Blazor consumer uses purpose-specific contracts, ELP-230C contracted persistence, and ELP-430 removed obsolete anonymous exact routes/contracts.
  - Paths: the ELP-420A artifacts, API inventory/snapshots, and generated-client cleanliness checks.
  - Result: obsolete generic event-location schemas have zero consumers and final generation is clean; generated artifacts match the contracted runtime.

- [ ] **ELP-430 — Remove generic anonymous exact Location detail and obsolete contracts**
  - Paths: `LocationController.cs`, old Location DTO endpoints/assemblers/policies after consumer migration.
  - Result: no anonymous physical exact dereference; coarse non-Home discovery remains explicitly governed.

- [ ] **ELP-440 — Split public and attendee calendar routes/contracts**
  - Paths: Event calendar controller/handlers/builders and tests.
  - Result: public ICS uses public-only disclosure; attendee ICS authorized/private/no-store; no Private Home data in public subscription URL; warning about third-party retention.

## Phase 6: Global Erasure, Outbox, and Operations

- [ ] **ELP-500 — Add adversarial transaction and cross-tenant erasure tests first**
  - Paths: `tests/Event.Application.UnitTests/Features/Users/Commands/DeleteUserCommandHandlerTests.cs`, Persistence integration tests.
  - Cases: default local-ledger atomicity/rollback; retained authority unavailable; duplicate/ambiguous UUIDv7 append; crash after retained append before app commit; retained app rollback leaves authority intent pending; crash after app commit; retained sequence-zero fresh-DB replay; two tenants/former memberships; room/name tombstone; membership removal; discovery derivative; no PII in either ledger/outbox.
  - Confirmed RED only: `AuthorityUnavailable_FailsClosedBeforeDeletingUser` executes the real current `DeleteUserCommandHandler` and failed because no exception was thrown; `TwoTenantOwnedHomes_AreTombstonedBeforeUserDeletion` failed because both Homes remained `Active` (`2`) instead of `Erased` (`3`). The preserved RED receipt is 2 executed, 0 passed, 2 failed, exit 2. Both bodies are now governed pending under `EventLocationPrivacyPending`; the fresh skipped-only receipt is 2 not executed, exit 8 and is not green.
  - Separate retained-authority evidence: duplicate/ambiguous UUIDv7 append already has PostgreSQL coverage, but command orchestration is not claimed.
  - Deferred inventory is re-baselined to OREA-040/100/110/120/130/140: default local-ledger atomicity; retained append/application crash boundaries; rollback/checkpoint/outbox; retained sequence-zero replay; former-membership/room integration; membership separation; discovery cleanup; and PII-free ledger/outbox shape. Keep this task open until both startup-selected workflows and their generated migrations have dedicated evidence.

**ELP-505 ownership transfer — non-executable; implementation completion is not claimed here**

- OREA-040 owns the two real workflows and shared erasure applier; OREA-100/110 own the local ledger and atomic application-database persistence; OREA-120/140 own retained EF persistence and migration adoption.
- The original acceptance remains binding under those OREA tasks: both modes erase all `OwnerUserId` Homes across tenants, default commits ledger/erasure/checkpoint/outbox atomically, and retained mode never falls back.

- [ ] **ELP-510 — Separate tenant membership removal from global deletion**
  - Paths: planned `src/Explore.Application/Features/TenantUsers/Requests/Commands/RemoveTenantMembershipCommand.cs`, `src/Explore.Application/Features/TenantUsers/Handlers/Commands/RemoveTenantMembershipCommandHandler.cs`, authorization descriptor/policy beside that feature, and `tests/Event.Application.UnitTests/Features/TenantUsers/Commands/RemoveTenantMembershipCommandHandlerTests.cs`.
  - Result: removing TenantUser/TenantUserProfile does not invoke global erasure or alter other-tenant Homes.

**ELP-515 ownership transfer — non-executable; implementation completion is not claimed here**

- OREA-040 owns correction-outbox creation inside the shared application transaction; OREA-110/120 own the mode-specific ledger persistence needed to prove rollback and replay behavior.
- The original acceptance remains binding under OREA: correction payloads contain IDs/versions only; rollback preserves PII/ledger/checkpoint/outbox together; a committed erasure has durable local checkpoint/outbox state before cache eviction.

- [ ] **ELP-520 — Verify concrete correction dispatch, idempotency, retry, and dead-letter recovery**
  - Paths: `src/Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs`, planned concrete location-privacy correction dispatcher/service, `InfrastructureServicesRegistration.cs`, `tests/Explore.Infrastructure.Tests/Infrastructure/CompositeOutboxMessageDispatcherTests.cs`, API outbox dead-letter tests.
  - Result: every new event type has a concrete route; duplicate delivery is safe; unknown/no-op routing cannot pass; dead letters are visible/reconcilable.

**ELP-525 ownership transfer — no implementation task remains in this workstream**

- [`optional-retained-erasure-authority-plan.md`](../optional-retained-erasure-authority/optional-retained-erasure-authority-plan.md) owns the two-mode storage, migration, startup, provisioning, and operator contract.
- `OREA-210` owns conditional pre-traffic replay; `OREA-320` owns the backup/restore and operator runbooks. Do not implement this topology under an ELP task.

- [ ] **ELP-530 — Implement post-erasure EventLocation remediation workflow**
  - Paths: EventLocation review query/commands, notification outbox, publication validator, admin dashboard API, tests.
  - Result: affected associations need review; managers notified; unusable physical location blocks publication; explicit TBA allowed.

- [ ] **ELP-540 — Add privacy metrics and alerts**
  - Metrics: Unclassified locations, NeedsPrivacyReview EventLocations, missing-policy fail-closed hits, erasure duration/count, correction retries/dead letters, restore replay failures, exceptional exact reads.
  - Rule: labels contain IDs/counts/categories only, never address/user-entered location text.
  - Result: self-hosters can operate and diagnose the feature safely.

## Phase 7: Blazor UX

- [ ] **ELP-600 — Migrate Blazor services and JSON serialization to generated purpose-specific contracts**
  - Paths: `Services/LocationService.cs`, `AdminService.cs`, `LookupCacheService.cs`, `Helpers/HalResourceExtensions.cs`, `Serialization/AppJsonSerializerContext.cs`.
  - Result: no component consumes unrestricted generic Location DTO for event display.

- [ ] **ELP-610 — Add EventLocation management editor with governance-aware controls**
  - Paths: existing create/edit Location dialogs, Event create/edit/session composer, new EventLocation disclosure component using existing wrappers/CSS isolation.
  - Controls: kind/state, field selections, audience, reveal time, governance-denied states, concurrency errors.
  - Result: mutations shown only from HAL links; safe defaults visible, especially Private Home.

- [ ] **ELP-620 — Implement Home owner consent and transfer UX**
  - Paths: management component/dialog and API-generated methods, component tests.
  - Result: current user default; selecting/transferring another owner requires explicit consent, confirmation, audit, and authorization.

- [ ] **ELP-630 — Implement public and attendee disclosure states**
  - Paths: EventDetail, EventDetailsSidebar, session/program components, component tests.
  - States: public-only, eligible delayed reveal, attendee exact, Private venue, TBA, erased/unavailable, policy review.
  - Result: no client clock or role/claim decisions; server DTO/HAL authoritative.

- [ ] **ELP-640 — Add manager privacy-review dashboard and remediation actions**
  - Paths: admin/event management pages and tests.
  - Result: Unclassified/NeedsPrivacyReview queue, replacement association, TBA action, publication blocker explanation.

- [ ] **ELP-650 — Remove overpromising private-address copy and sanitize JSON-LD**
  - Paths: `Pages/Events/EventDetail.razor`, `EventDetail.razor.cs`, localization bundles.
  - Result: “Register to see private address” appears only when server exposes a real eligible affordance; JSON-LD uses public disclosure only.

- [ ] **ELP-660 — Complete localization, accessibility, responsive, RTL, and visual QA**
  - Paths: `src/Explore.Infrastructure/Localization/Bundles/en.json`, `fr.json`, `ar.json`, Razor/CSS isolation, Blazor tests, and manual browser QA.
  - Result: WCAG 2.2 AA labels/focus/live announcements/no color-only state/24px targets; 375/768/1280 layouts and RTL pass real-browser QA.

## Phase 8: Outbound Surfaces, Discovery, and Documentation

- [ ] **ELP-700 — Prove shared projection convergence and remove remaining bypasses**
  - Paths: negative source scan and focused regression tests across ELP-320-owned session/program/agenda handlers, ELP-440-owned calendars, and ELP-650-owned JSON-LD/copy.
  - Result: no direct physical Location/PII/room mapping remains and public vs attendee purpose cannot drift; this task verifies owners rather than duplicating their implementation.

- [ ] **ELP-715 — Audit email, notification, webhook, export, ticket, search, API-key, print, and report surfaces**
  - Paths: every concrete producer/serializer discovered from ELP-020; add tests beside each owner.
  - Result: each surface consumes purpose-limited EventLocation results or proves no location payload; Svix/email/export cannot bypass policy.

- [ ] **ELP-720 — Migrate MCP/AI/federation/PDS surfaces and correction behavior**
  - Paths: `src/Explore.API/Mcp/EventManagementMcp*.cs`, `src/Explore.API/BackgroundServices/PdsSyncWorker.cs`, `src/Explore.Infrastructure/Services/Federation/PdsService.cs`, AI disclosure matrix/registry docs/tests.
  - Result: sanitized location still passes through `IAiContextGateway`; policy tightening/erasure emits PII-free idempotent correction.

- [ ] **ELP-730 — Enforce PostGIS/discovery separation and erasure behavior**
  - Paths: Home Discovery docs and future discovery entity/service only if already in implementation scope; tests.
  - Result: current implementation records architecture/source absence proof because no `LocationDiscoveryPoint` store exists; if one enters scope later, prove no PII auto-copy, Private Home no point by default, transactional erasure cleanup, EventLocation/occurrence server-side joins, and no exact client catalog.

- [ ] **ELP-740 — Update canonical architecture/security/API/domain/privacy/federation/testing docs**
  - Paths: docs listed in plan Section 3, API changelog, AI matrix/registry, Home Discovery overlap.
  - Result: shipped behavior, threat model, operator limits, route contracts, and recovery semantics are discoverable and non-contradictory.

## Phase 9: Final QA and Cleanup

- [ ] **ELP-800 — Verify migration/backfill/rollback and production-like PostgreSQL data shapes**
  - Evidence: generated SQL review, idempotent rerun, zero-gap queries, pre-activation Down, post-erasure forward repair, large-owner volume measurement.

- [ ] **ELP-810 — Run adversarial privacy, auth, tenant, cache, and outbox matrix**
  - Evidence: every acceptance row below linked to a test and result; no skipped critical case.

- [ ] **ELP-820 — Run per-project automated suites and Release build**
  - Commands: canonical commands from plan Section 18; never solution-level `dotnet test`.
  - Result: no failures and no new attributable warnings.

- [ ] **ELP-830 — Run OpenAPI/NSwag/contract cleanliness and browser visual/accessibility QA**
  - Result: generated artifacts clean; routes/headers/schemas stable; desktop/mobile/RTL/focus/error/loading/review flows pass.

- [ ] **ELP-840 — Final repository and dev-doc review**
  - Checks: `git diff --check`, diagnostics, architecture suite, no obsolete `EventLocationDisclosurePolicy`/Public legacy defaults/session-row entitlement, all three docs synchronized, unrelated work untouched.
  - Result: implementation-ready or implementation-complete status stated truthfully with remaining risks.

## Mandatory Acceptance Matrix

| Acceptance case | Primary task | Primary automated owner |
|---|---|---|
| Unknown legacy becomes Unclassified, never Public; PII presence maps only to Active/NotProvided | ELP-230B | `EventLocationBackfillTests` in Persistence Integration |
| Active Home owner valid; non-erased ownerless invalid; Erased ownerless/PII-less valid; resurrection rejected | ELP-120 | `LocationPrivacyLifecycleTests` in Domain Unit |
| Person/household venue and room labels/descriptions tombstoned and never public | ELP-130 / OREA-040 | `GlobalLocationPrivacyErasureTests` in Persistence Integration |
| Same physical Location has independent per-event policies; TBA/location XOR holds | ELP-125 / ELP-150 | `EventLocationTests` in Domain Unit |
| Public contract exposes EventLocationId, not unrestricted LocationId | ELP-300 / ELP-405 | `EventLocationControllerTests` in API Integration |
| Event/Day/SessionSelection coverage is exact | ELP-225 | `EventLocationRegistrationAccessServiceTests` in Application Unit |
| Pending/waitlisted broad only; cancelled/revoked/deleted deny; null resolved by mode | ELP-200 / ELP-225 | `EventLocationRegistrationAccessServiceTests` in Application Unit |
| Homes in two tenants are both globally erased | ELP-500 / OREA-040 | `GlobalLocationPrivacyErasureTests` in Persistence Integration |
| Membership removal does not erase global/other-tenant data | ELP-070 / ELP-510 | `RemoveTenantMembershipCommandHandlerTests` in Application Unit |
| Mode-selected ledger intent remains immutable across app rollback; retained external intent stays replayable; PII/checkpoint/outbox roll back; post-commit crash finds checkpoint/outbox | OREA-040 / OREA-110 / OREA-120 | `GlobalLocationPrivacyErasureTests` in Persistence Integration |
| Public endpoint remains public-only with auth cookie | ELP-400 / ELP-405 | `EventLocationControllerTests` in API Integration |
| Attendee/management are private/no-store | ELP-400 / ELP-405 | `EventLocationControllerTests` in API Integration |
| Tightened policy/governance defeats stale cache | ELP-340 / ELP-810 | `EventLocationGovernanceTests` in API Integration |
| Public calendar public-only; attendee calendar authorized/no-store | ELP-440 | `EventCalendarPrivacyTests` in API Integration |
| Email/webhook/export/ticket/search/report cannot bypass authority | ELP-715 | Focused test beside every ELP-020 inventory owner |
| Concrete correction dispatcher is idempotent/retryable/dead-letter visible | ELP-520 | `CompositeOutboxMessageDispatcherTests` in Infrastructure plus API dead-letter tests |
| Default never resolves authority; explicit retained mode survives app restore, replays before traffic/workers, and fails closed without fallback | OREA-210 | `LocationPrivacyStartupGateTests` in API Integration |
| Discovery data is absent today or never auto-created from PII and erases transactionally | ELP-730 | Architecture absence proof or `LocationDiscoveryPrivacyTests` in Persistence Integration |
| Batch projection stays within query/auth count budget | ELP-315 | `EventLocationDisclosureBatchTests` in Persistence Integration |
| Server time controls reveal and cannot bypass entitlement | ELP-130 / ELP-310 / ELP-340 | `EventLocationDisclosureEvaluatorTests` in Application Unit |
| Private Home safe default is generic public label plus ConfirmedParticipant | ELP-110 / ELP-310 | `EventLocationDisclosureEvaluatorTests` in Application Unit |

## Implementation Completion Evidence

For every checked task, append to context:

- exact files changed;
- failing test added first;
- command and result;
- migration/contract/cache/security implications;
- unresolved risk or `none`;
- next task ID.

Do not check a task because code was written. Check it only when its result and verification evidence exist.
