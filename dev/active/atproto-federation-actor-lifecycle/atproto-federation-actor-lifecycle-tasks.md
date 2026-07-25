<!-- ABOUTME: Executable checklist for the Actor-first ATProto federation identity lifecycle workstream. -->
<!-- ABOUTME: Tracks global Actor migration, verified claim/merge, scoped moderation, discovery, and phase gates. -->

# ATProto Federation Actor Lifecycle - Task Checklist

Last Updated: 2026-07-25 Europe/Brussels

## Status Summary

- **Overall status:** Draft, awaiting user review. Runtime implementation has not started.
- **Completed:** 0/12 implementation tasks. Phase verification is tracked separately.
- **Current priority:** Review architecture Decisions A1-A9 and Phase 1 migration boundary.
- **Next recommended slice:** After approval, Task 1.1 - define ADR-016 and the complete Actor FK disposition manifest.

## Implementation Maintenance Rules

- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Mark a substantial task IN PROGRESS when it will span meaningful work or a handoff.
- Check a substantial task immediately after its acceptance criteria pass; reconcile small tasks no later than phase end.
- Keep completed count, current priority, next slice, discovered work, deferred work, and update date accurate.
- Check a phase complete only after all implementation and phase-verification checkboxes pass.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Update the plan only when scope, architecture, sequencing, acceptance criteria, risk, or validation strategy changes.
- Do not run build/tests after individual tasks; verify once at phase end.
- Do not start the app, browser, Docker, Aspire, Playwright, Chrome DevTools, or live services for routine phase verification.
- Preserve unrelated worktree changes and never absorb predecessor Todo 23 or another active workstream.

## Blocking Decisions And Gates

- [ ] **User approves replacing linked-account-only ATProto login with verified Actor claim.**
  - No email matching, synthetic email, passive cross-provider merge, or handle matching is introduced.
- [ ] **User approves global Actor plus ActorTenantPresence.**
  - Tenant authorization remains on tenant membership/presence/dependent policy, never global Actor existence.
- [ ] **User approves DID-Actor-wins explicit merge.**
  - Imported Events already owned by the DID Actor are not rewritten; mutable local references move and immutable evidence remains.
- [ ] **Task 1.1 FK disposition manifest is complete before Task 1.2 migration code.**
  - Every Actor FK is categorized; unclassified references block migration.

## Phase 1: Global Actor Schema And Safe Data Migration - NOT STARTED

- [ ] **1.1 Define global Actor, presence, merge, and ownership contracts**
  - **Files:** `Actor.cs`, `ActorPii.cs`, `User.cs`, `Organization.cs`, `Group.cs`, `ActorTypeEnum.cs` (existing); `ActorTenantPresence.cs`, `ActorMerge.cs`, related enums/configurations, and `docs/adr/ADR-016-atproto-actor-identity-lifecycle.md` (new).
  - **Acceptance:** Actor has no tenancy/reverse owners; Person may be unclaimed; User.ActorId is unique ownership; presence/moderation/merge contracts exist; every Actor FK has a mutable/current/evidence disposition.
  - **Effort:** XL.
  - **Dependencies:** User approval gates.

- [ ] **1.2 Implement deterministic Actor/email migration and constraints**
  - **Files:** new EF migration/designer; model snapshot; Actor/User/ActorPii/UserPii and all Actor-FK configurations; `schemas/islamu-event.md`; focused PostgreSQL migration tests.
  - **Acceptance:** Same-DID Actors consolidate deterministically with all presences/references intact; ambiguous ownership aborts; email is nullable; DID and personal owner uniqueness are database-enforced.
  - **Effort:** XL.
  - **Dependencies:** 1.1.

### Phase 1 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 2: Global Actor Repositories And Tenant-Safe Consumers - NOT STARTED

- [ ] **2.1 Replace tenant-filtered Actor repository contracts**
  - **Files:** `IActorRepository.cs`, `ActorRepository.cs`, Actor specifications/query handlers, query-filter configuration, and repository tests.
  - **Acceptance:** Explicit global id/DID and tenant-presence methods exist; lookup distinguishes unclaimed/current-owner/other-owner; bypasses are bounded and tested.
  - **Effort:** L.
  - **Dependencies:** 1.2.

- [ ] **2.2 Update Actor creators and cross-layer consumers**
  - **Files:** User/organization/group/onboarding handlers, UI shell, AI Actor context, events, subscriptions, notifications, affected tests/fixtures.
  - **Acceptance:** Owner-side links are authoritative; tenant authorization is independent of Actor; workspace preference remains distinct; architecture prevents old tenant/reverse-owner model.
  - **Effort:** XL.
  - **Dependencies:** 2.1.

### Phase 2 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 3: Canonical Inbound Actor Materialization - NOT STARTED

- [ ] **3.1 Rework inbound materialization around global Actor identity**
  - **Files:** `AtprotoJetstreamRepository.ApplyEventImportsAsync`, import plan/factory/handlers, DbContext, inbound federation persistence tests, and `docs/FEDERATION.md`.
  - **Acceptance:** Same DID across tenants creates one Actor and multiple presence rows; replay/recovery/concurrency remain idempotent; Event IDs/record links remain stable; tombstones do not incorrectly delete global identity.
  - **Effort:** L.
  - **Dependencies:** 2.1, 2.2.

### Phase 3 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 4: Bounded Public Actor Profile Hydration - NOT STARTED

- [ ] **4.1 Hydrate bounded public Actor profile data**
  - **Files:** constrained PDS gateway/import prefetch; profile lexicons/bindings if needed; cache/options; Actor mapping; federation/lexicon/configuration/self-hosting/troubleshooting docs; focused tests.
  - **Acceptance:** Verified public profile fetch is SSRF/size/timeout bounded and outside transactions; DID remains identity; failure uses DID-only fallback and never rejects Event import; no second federation switch is added.
  - **Effort:** L.
  - **Dependencies:** 3.1.

### Phase 4 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`

## Phase 5: Verified Actor Claim And Explicit Account Merge - NOT STARTED

- [ ] **5.1 Claim or create User from verified ATProto session**
  - **Files:** bootstrap/session and sync/claim handlers; User/UserPii mappings/contracts; external-login/TenantUser repositories; focused tests; security/authorization docs.
  - **Acceptance:** Only full verification can create User; unclaimed Actor ID/imported ownership remains stable; repeat login repairs idempotently; email/handle never match; other-owner conflict fails without session issuance.
  - **Effort:** L.
  - **Dependencies:** 3.1, 1.2.

- [ ] **5.2 Merge explicit local personal Actor into federated Actor**
  - **Files:** new merge command/handler/validator/repository operation; explicit provider-link owner; merge configuration; mutable dependent repositories; API/HAL/ProblemDetails if changed; tests and API changelog.
  - **Acceptance:** Verified DID Actor is canonical; imported canonical Events are untouched; mutable source relations converge with collision policy; immutable evidence remains; concurrent attempts resolve once or conflict.
  - **Effort:** XL.
  - **Dependencies:** 5.1, 2.1, 1.1.

### Phase 5 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 6: Actor Profile, Scoped Moderation, Analytics, And Materialized Discovery - NOT STARTED

- [ ] **6.1 Replace Actor API reads with global profile plus tenant presence**
  - **Files:** Actor queries/DTOs/mappings/controller/HAL/routes/OpenAPI/generated contracts; API tests; API docs/changelog.
  - **Acceptance:** Public global profile exposes no User PII; tenant listing requires visible presence; counts use current-tenant public materialized Events; HAL distinguishes allowed actions.
  - **Effort:** L.
  - **Dependencies:** 2.1, 5.2.

- [ ] **6.2 Add Actor-wide suspension and tenant-local presence hiding**
  - **Files:** Actor/presence domain methods; CQRS validators/handlers; authorization/HAL/controller/repository/audit; tests; security/API docs.
  - **Acceptance:** Tenant admin is local-only; instance admin can suspend globally; unclaimed Actors can be locally hidden; transitions are audited, idempotent, and concurrency-safe.
  - **Effort:** L.
  - **Dependencies:** 6.1, 1.1.

- [ ] **6.3 Make public discovery materialized-only**
  - **Files:** discovery handler, Event specifications/repositories, projection consumers, source-link policy, API tests, federation/API docs, contract inventory.
  - **Acceptance:** Projection is never returned directly; materialized imports retain pagination/source; hidden/suspended Actor Events are absent; failed materialization has evidence but no public card.
  - **Effort:** L.
  - **Dependencies:** 6.2, 3.1.

### Phase 6 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 7: Blazor Actor Experience And Contract Reconciliation - NOT STARTED

- [ ] **7.1 Consume global Actor profiles and HAL actions in Blazor**
  - **Files:** `IActorService.cs`, `ActorService.cs`, generated client/serializer roots, Event/profile/subscription components, optional minimal Actor profile route/component/CSS, bUnit/service tests, Blazor docs, contract inventory.
  - **Acceptance:** One Actor URL follows the DID across tenants; DID-only fallback renders safely; all actions depend on HAL; loading/not-found/hidden/suspended/normal states have non-browser tests.
  - **Effort:** L.
  - **Dependencies:** 6.1, 6.2, 6.3.

### Phase 7 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Remaining / Deferred Work

- ActivityPub bridging and first-party PDS/AppView/relay hosting remain roadmap work and are not activated by this plan.
- A durable Actor profile refresh outbox is deferred unless Task 4.1 proves bounded cached retry on later observations is insufficient.
- Cross-tenant private Actor analytics are deferred; anonymous profile counts remain current-tenant/public/materialized only.
- The predecessor `dev/active/atproto-auth` Todo 23 and F1-F4 remain owned by that workstream and must not be absorbed here.
