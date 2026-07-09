<!-- ABOUTME: Executable task checklist for implementing the Notification Preference Matrix workstream. -->
<!-- ABOUTME: Splits persistence, CQRS/API/HAL, Blazor/BFF, delivery integration, and verification into maintainable PR-sized slices. -->

# Notification Preference Matrix — Task Checklist

Last Updated: 2026-07-09 Europe/Brussels

## 1. Status Summary

- Planning artifacts: updated with CTO review, Context7 evidence, source-grounded current state, PR split, risk register, and verification contract.
- Implementation code: PR 1 Data Foundation And Resolver, PR 2 CQRS/API/HAL endpoints, PR 3 Blazor/BFF matrix surfaces, and PR 4 delivery integration are implemented for current-user, organization, and group scopes.
- Recommended next PR: operations/docs cleanup and final manual QA.
- Delivery integration is implemented; canonical docs and final QA now need to record the shipped behavior and remaining unrelated blockers.

## 2. Maintenance Rules

Every implementation agent must:

1. Update `notification-preference-matrix-plan.md` when architecture, scope, risks, acceptance criteria, or PR split changes.
2. Update `notification-preference-matrix-context.md` before handoff, pause, context compaction, or agent switch.
3. Update this checklist immediately when a task is completed, deferred, split, or invalidated.
4. Keep checkbox semantics intact: Email and In-App independent; both checked allowed; both unchecked is opt-out for non-required categories.
5. Keep required semantics intact: required cells are locked on server-side and displayed as disabled checked UI cells.
6. Keep HAL affordance gating intact: Blazor actions come from `_links`, not roles/claims.
7. Keep tenant isolation intact: no runtime request path disables the Tenant query filter.
8. Keep repositories entity-returning and validators manually instantiated.
9. Add two ABOUTME comment lines to every new source file.
10. Run only per-project tests relevant to touched paths plus Release build.

## 3. PR 0 — Approval And Baseline

### 0.1 Confirm product semantics

- Status: Completed.
- Layer: Planning.
- Files:
  - `dev/active/notification-preference-matrix/notification-preference-matrix-plan.md`
  - `dev/active/notification-preference-matrix/notification-preference-matrix-context.md`
  - `dev/active/notification-preference-matrix/notification-preference-matrix-tasks.md`
- Action: Confirm category list, default channel states, required categories, global mute copy, and user/org/group scope expectations.
- Done when:
  - Product owner accepts or changes category/default/required policy.
  - Any changes are recorded in all affected dev-doc files before code starts.
- Verification:
  - Read-back docs diff only.

### 0.2 Re-classify the first code slice

- Status: Completed.
- Layer: Governance.
- Files:
  - `.claude/contract/intents.yaml`
  - `docs/QUICK_REFERENCE.md`
  - matching `.claude/rules/*.md`
- Action: Match the first production-code PR to actual repository intent(s), path scopes, docs, and minimum tests.
- Done when:
  - Matched intent(s), loaded docs, loaded skills, paths in scope, and verification commands are recorded in context.
- Verification:
  - Context file updated before edits.

## 4. PR 1 — Data Foundation And Resolver

### 1.1 Inspect exact lookup and notification persistence patterns

- Status: Completed.
- Layer: Discovery.
- Files:
  - notification lookup/domain files near `Explore.Domain/Notification*.cs`
  - existing EF configurations and seed data in `Explore.Persistence/**`
  - existing repository implementations for notification/settings patterns
- Action: Identify the smallest existing pattern for lookup metadata, tenant-scoped entities, EF filters, uniqueness constraints, and repositories.
- Done when:
  - Concrete file paths and chosen pattern are added to context.
  - No new entity/table is added before confirming no existing lookup extension fits.
- Verification:
  - Context update.

### 1.2 Add category/channel metadata

- Status: Completed.
- Layer: Domain + Persistence.
- Files:
  - new or extended lookup-like category/channel entities
  - EF configuration and seed data
  - focused migration
- Action: Add stable server-side category/channel metadata for Email and In-App matrix rendering/resolution.
- Done when:
  - Category metadata includes stable codes, names, descriptions, required flag, default Email/In-App values, and sort order.
  - Channels include only `email` and `in_app`.
  - Lookup IDs are stable `int` values.
  - Required metadata is not inferred from localized display names.
- Verification:
  - Added persistence coverage for seed presence and stable codes/IDs.
  - `Event.Persistence.IntegrationTests` passed 259/259 tests.

### 1.3 Add scoped preference/profile persistence

- Status: Completed.
- Layer: Domain + Persistence.
- Files:
  - new tenant-scoped preference cell entity
  - new tenant-scoped profile/global mute entity or equivalent
  - repository contract(s) in Application
  - repository implementation(s) in Persistence
  - EF configuration and focused migration
- Action: Persist explicit per-scope category/channel choices and global mute state.
- Done when:
  - User, organization, and group targets are supported.
  - Exactly one target id is allowed for target-specific scopes.
  - Unique active row exists per tenant/scope/target/category/channel.
  - Tenant and soft-delete filters follow repo conventions.
  - Repository methods return entities only.
  - Multi-row save can run inside unit-of-work transaction.
- Verification:
  - Added `Event.Persistence.IntegrationTests` coverage for tenant filtering and resolver-backed rows.
  - Migration `20260709160022_AddNotificationPreferenceMatrixFoundation` enforces target constraints and filtered unique indexes.

### 1.4 Add effective preference resolver

- Status: Completed.
- Layer: Application contract + Persistence implementation.
- Files:
  - new Application resolver contract
  - resolver implementation
  - resolver DTO/result models if needed
  - `Event.Application.UnitTests/**`
- Action: Resolve `(tenant, recipient user, optional organization/group, category, channel)` into enabled/disabled metadata.
- Done when:
  - Required categories resolve enabled even when explicit disabled rows exist.
  - Global mute disables only non-required categories.
  - All unchecked cells for a non-required category resolve as opt-out.
  - User/group/org/tenant/instance precedence and lock behavior are tested.
  - Missing rows fall back to seeded defaults.
  - Resolver batches reads enough to avoid per-cell N+1 queries.
- Verification:
  - Added persistence integration coverage for required defaults, global mute, hierarchy overrides, locks, and seeded defaults.
  - `Event.Persistence.IntegrationTests` passed 259/259 tests.

## 5. PR 2 — CQRS, API, HAL, Authorization

### 2.1 Add matrix query projections

- Status: Completed.
- Layer: Application.
- Files:
  - query records/handlers for user, organization, and group matrices
  - projection DTOs for categories, channels, cells, mute state
  - validators if needed
- Action: Return UI-ready effective matrix data.
- Done when:
  - Projection includes category/channel metadata, cell enabled/editable/locked/source/reason fields, mute state, and action metadata.
  - Handler maps entities/resolver output to DTOs.
  - No entities or `IQueryable` leak upward.
  - Validators are manually instantiated.
- Verification:
  - Added `Event.Application.UnitTests` coverage for current-user projection shape and group parent-organization resolver context.
  - Full `Event.Application.UnitTests` passed 2089/2089 tests.

### 2.2 Add save/mute/reset commands

- Status: Completed for save and mute; reset overrides remains deferred until product confirms reset UX.
- Layer: Application.
- Files:
  - command records/handlers
  - command validators
  - response contracts
- Action: Save editable cells, set global mute, and optionally reset scoped overrides.
- Done when:
  - Required/locked disable attempts fail with explicit validation semantics.
  - Saves are idempotent.
  - Multi-row writes use unit-of-work transaction.
  - Cache invalidation runs only after successful commit.
- Verification:
  - Added Application unit tests for required-category write rejection, user transaction writes, organization-scoped transaction writes, and locked broader mute rejection.
  - Full `Event.Application.UnitTests` passed 2089/2089 tests.

### 2.3 Add API endpoints

- Status: Completed for current-user, organization, and group GET/save/mute endpoints.
- Layer: API.
- Files:
  - notification preference controller or equivalent API endpoint file
  - route names/constants if used
  - request/response mappings
- Action: Expose authenticated user/org/group preference endpoints.
- Done when:
  - User/org/group GET endpoints require authenticated/resource-authorized access.
  - Writes require `[Authorize]` plus handler-level resource checks.
  - Route templates and names are explicit.
  - ProblemDetails/validation/unauthorized/forbidden/not-found outcomes are declared and tested.
- Verification:
  - `Explore.API` no-dependencies Release build passed after adding endpoints.
  - API integration suite is currently blocked from a clean full run by unrelated event-registration `notification_intents` foreign-key failures and a separate unrelated dirty Infrastructure localization compile blocker when building dependencies.
  - Current-user API/HAL/OpenAPI failures introduced during implementation were fixed before this status update.

### 2.4 Add authorization and HAL parity

- Status: Completed for server-authored HAL links on implemented preference resources; dedicated permission-bound resource descriptors remain deferred for broader policy hardening.
- Layer: API/Auth.
- Files:
  - authorization resource/action definitions
  - local fallback/Cerbos policy files as applicable
  - HAL link policy/assembler
  - authorization parity tests
- Action: Add `save`, `set-mute`, and `reset-overrides` links only when authorized.
- Done when:
  - HAL evaluator fails closed for preference actions.
  - Blazor can rely entirely on `_links` for affordances.
  - Local fallback and external policy behavior match.
- Verification:
  - Added `NotificationPreferenceMatrixLinkPolicy`, `NotificationPreferenceMatrixResourceAssembler`, route names, HAL assembler registration, and OpenAPI HAL schema catalog entry.
  - Full `Event.Architecture.Tests` passed 262/263 tests with one pre-existing skip before org/group endpoint expansion; rerun after UI/delivery cleanup when unrelated full-build blocker is cleared.

## 6. PR 3 — Blazor / BFF Matrix Surface

### 3.1 Inspect existing settings/navigation surfaces

- Status: Completed.
- Layer: Blazor Discovery.
- Files:
  - existing user settings pages/components
  - existing organization/group settings/navigation pages
  - existing client services wrapping generated API clients
- Action: Find the smallest existing place to host user preferences first, then org/group if surfaces exist.
- Done when:
  - Concrete paths are recorded in context.
  - No new settings shell is created if an existing one fits.
- Verification:
  - Recorded existing user settings, organization profile, group profile, generated client, notification service, and HAL helper paths in context.

### 3.2 Add reusable matrix component

- Status: Completed.
- Layer: Blazor Client.
- Files:
  - reusable `.razor` component
  - optional `.razor.css` isolation file
  - component/service tests
- Action: Render category rows and Email/In-App checkbox columns from API projection.
- Done when:
  - Accessible checkbox labels use category/channel names.
  - Required cells render disabled checked with lock/reason copy.
  - Global mute helper copy explains required notifications still send.
  - Save/reset/mute buttons are gated by HAL links.
  - No role/claim checks appear in component code.
  - Styling uses existing design tokens/wrappers and BEM classes if custom CSS is needed.
- Verification:
  - `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet` passed.
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet --no-build -- --no-progress --maximum-failed-tests 1` passed 1564/1565 tests with one pre-existing skip.

### 3.3 Wire service/BFF calls and hosting pages

- Status: Completed.
- Layer: Blazor/BFF.
- Files:
  - existing service layer wrappers
  - user settings host page
  - organization/group host pages if existing navigation supports them
- Action: Load, edit, save, mute, and reset via service layer/BFF-safe clients.
- Done when:
  - Browser never receives tokens.
  - State-changing calls are antiforgery-safe where applicable.
  - Server validation errors render in the form.
  - User surface works before org/group expansion.
- Verification:
  - `INotificationService` and `NotificationService` now wrap generated current-user, organization, and group preference matrix endpoints.
  - `/settings?section=notifications`, organization profiles, and group profiles host the reusable HAL-gated matrix component.
  - `Explore.Blazor.Client.Tests` no-build run passed 1564/1565 tests with one pre-existing skip.
  - Full dependency test/build remains blocked by unrelated localization compile errors that the user instructed not to fix.

## 7. PR 4 — Delivery Integrations

### 4.1 Gate in-app notification fanout

- Status: Completed.
- Layer: Application.
- Files:
  - existing notification fanout/producers/handlers found during slice discovery
  - resolver integration tests
- Action: Use the effective resolver before creating non-required in-app notifications.
- Done when:
  - Opted-out non-required categories do not create `Notification` rows.
  - Required categories still create rows.
  - Deduplication behavior remains unchanged for created rows.
  - SSE behavior remains a refresh hint only.
- Verification:
  - Added Application unit coverage for event-published fanout suppression when matrix disables `event-updates` / `in_app`.
  - Moderation fanout now resolves `trust-safety` / `in_app` through the required-category path.
  - Registration fallback notifications now resolve `registration-status` / `in_app` before creating rows.
  - Full `Event.Application.UnitTests` passed 2091/2091 tests.

### 4.2 Gate email dispatch paths

- Status: Completed.
- Layer: Application + Infrastructure.
- Files:
  - existing email dispatch enqueue/factory/drain paths found during discovery
  - email dispatch tests
- Action: Apply Email channel preferences to non-required product emails through current durable dispatch boundaries.
- Done when:
  - Opted-out non-required email categories do not enqueue/send or are recorded as skipped by existing semantics.
  - Required/security/legal emails remain enabled where applicable.
  - Existing unsubscribe/preference checks remain at least as restrictive.
  - Tenant pause/operator park/replay/health behavior remains unchanged.
- Verification:
  - `EmailDispatchDrainService` keeps tenant pause, processing claim, receipt claim, legacy unsubscribe, retry, dead-letter, operator park/replay, and SMTP handoff semantics intact.
  - Matrix-disabled emails are marked skipped with failure category `recipient_notification_preference_disabled` before SMTP provider handoff.
  - Added Infrastructure test coverage for matrix-disabled direct email dispatch skip behavior.
  - Full `Explore.Infrastructure.Tests` passed 710/710 tests.

## 8. PR 5 — Operations, Docs, Cleanup

### 5.1 Add observability without leaking sensitive data

- Status: Pending.
- Layer: Operations.
- Files:
  - metrics/logging code touched by implementation
  - docs if new metrics are public/operator-facing
- Action: Add low-cardinality metrics/logs for preference writes, suppressions, and required/locked rejections.
- Done when:
  - Logs/metrics include tenant/request correlation where appropriate.
  - Logs do not include raw email addresses, notification bodies, or full preference payloads.
- Verification:
  - Unit/integration tests if metrics/logging helpers are covered.
  - Manual log review during QA.

### 5.2 Update canonical docs

- Status: Pending.
- Layer: Documentation.
- Files:
  - `docs/API.md`
  - `docs/DOMAIN.md`
  - `docs/BLAZOR.md`
  - `docs/NOTIFICATIONS.md`
  - `docs/EMAIL_NOTIFICATIONS.md` if Email channel integration ships
  - `docs/SECURITY-MODEL.md` or `docs/AUTHORIZATION.md` if auth semantics change
- Action: Move implemented behavior from active plan into durable docs.
- Done when:
  - API endpoints and HAL behavior are documented.
  - Domain/persistence model is documented.
  - UI matrix behavior and required/global mute semantics are documented.
  - Delivery integration boundaries are documented accurately.
- Verification:
  - Docs read-back.
  - Docs/architecture tests if applicable.

### 5.3 Run final verification

- Status: Pending.
- Layer: Repository-wide.
- Files:
  - all touched code and docs
- Action: Run Release build and per-project tests required by touched paths.
- Done when:
  - Build passes or pre-existing failures are explicitly recorded.
  - Relevant test projects pass.
  - Architecture tests pass.
- Verification commands:
  - `dotnet build --configuration Release --verbosity quiet`
  - relevant per-project `dotnet test --project ... --configuration Release --verbosity quiet`

### 5.4 Manual QA through real surfaces

- Status: Pending.
- Layer: Blazor/API/runtime.
- Files:
  - implemented UI/API/delivery surfaces
- Action: Use the implemented feature as a real user/operator.
- Done when:
  - Email-only, In-App-only, both, and neither choices can be saved and reloaded.
  - Global mute suppresses non-essential categories and preserves choices.
  - Required rows cannot be changed through UI or direct API calls.
  - Authorized org/group user can manage preferences; forbidden user cannot and lacks HAL links.
  - At least one in-app path and one email dispatch path respect preferences.
- Verification:
  - Browser/API/manual QA notes recorded in context.

## 9. Deferred / Explicitly Out Of Scope Until Requested

- Push, SMS, webhook, or mobile notification channels.
- Notification-to-email fanout for all in-app notifications.
- Billing workflow implementation.
- Marketing consent model changes beyond default-off preference metadata.
- Admin tenant/instance preference UI unless product explicitly requests operator-managed defaults.
- Conflict-resolution UX for concurrent matrix edits unless product requires it.
