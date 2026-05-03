<!-- ABOUTME: Phase checklist for event creation progressive disclosure implementation. -->
<!-- ABOUTME: Tracks contract, UI, accessibility, security, audit, and verification work. -->

# Event Creation Progressive Disclosure Tasks

Last Updated: 2026-05-03

## Phase 0: Documentation, Contract, And Architecture Review

- [x] Run release build baseline: `dotnet build --configuration Release --verbosity quiet`.
- [x] Verify `CreateEventRequest`, generated `EventApiClient.g.cs`, and `PopulateSchedulingOnRequest()` mapping.
- [x] Confirm or dismiss the suspected day `DateOnly`/generated-client mismatch.
- [x] Identify every place Blazor currently composes sessions, days, rooms, agenda items, temp keys, draft/publish state, and schedule references.
- [ ] Decide final request/command names for create draft, update draft, publish readiness, and publish.
- [ ] Decide API route names and HAL relation/policy changes for draft/update/publish affordances.
- [x] Define first `EventCreationContextResponse` shape for publisher context policy.
- [x] Decide `EventCreationContext` route: `GET /api/event/creation-context`.
- [x] Decide publication-context selector behavior for personal, organization, and group contexts from server context payload, not hardcoded tabs.
- [ ] Decide old graph-shaped `CreateEventRequest` endpoint fate; default is delete/replace after draft/update/publish exists.
- [ ] Decide idempotency strategy for create draft and publish, aligned with existing `Idempotency-Key` middleware.
- [ ] Decide optimistic concurrency strategy for update draft and publish, aligned with `ConcurrencyStamp`/expected version.
- [ ] Decide RFC 9457 ProblemDetails/readiness error contract with `PublishReadinessError { Code, FieldPath, Message, Severity }`.
- [ ] Decide IANA timezone strategy and Windows timezone mapping if runtime/deployment requires it.
- [ ] Identify policy-required fields by publication context.
- [ ] Identify audit events required for draft/update/schedule/visibility/registration/image/publish/unpublish/cancel.
- [ ] Identify outbox/domain events required for publish side effects.
- [ ] Identify object-storage/image failure behavior, replacement cleanup/tombstoning, and self-host S3/MinIO-compatible storage smoke coverage.
- [x] Confirm Domain requires no UI/progressive-disclosure concepts.

Acceptance criteria:

- [x] Contract leakage is documented with file evidence.
- [ ] Draft and publish split is explicitly designed.
- [ ] Schedule composition ownership is assigned to Application.
- [ ] Required fields that cannot stay hidden are identified.
- [x] No implementation proceeds on a Blazor-only assumption.
- [ ] Blazor skeleton work is blocked until creation context, lifecycle contracts, HAL action authority, schedule composer ownership, timezone model, readiness error contract, security rule, idempotency/concurrency, accessibility automation, and old endpoint deletion are settled.

## Phase 1: Application/API Shape Cleanup

- [ ] Add or reshape `CreateEventDraftRequest` / command equivalent.
- [ ] Add or reshape `UpdateEventDraftRequest` / command equivalent.
- [ ] Add publish readiness validation request/query/command equivalent.
- [ ] Add `PublishEventRequest` / command equivalent.
- [x] Add `EventCreationContextResponse` and context endpoint.
- [ ] Add HAL links for draft creation, publish readiness, publish, upload, lookup, and policy refresh.
- [ ] Keep HAL as action links only; keep field requirements and allowed values in the creation-context payload.
- [ ] Add Application `EventScheduleComposer` or `EventScheduleBuilder`.
- [ ] Convert local date, local start/end, and timezone ID into server-derived UTC instants.
- [ ] Validate ambiguous/invalid local times and return actionable errors.
- [ ] Add timezone coverage for spring-forward skipped time, fall-back ambiguous time, crossing midnight, multi-day across DST, timezone changes after sessions exist, and viewer timezone display.
- [ ] Split draft validation from publish validation.
- [ ] Delete/replace old graph-shaped `CreateEventRequest` primary create path after new endpoints exist.
- [ ] Remove client-controlled `EventStatusId` mutation as a draft/publish mechanism.
- [ ] Require idempotency key support for create draft and publish.
- [ ] Require expected concurrency stamp/version for update draft and publish.
- [ ] Return clear conflict ProblemDetails for stale expected concurrency values.
- [ ] Return structured publish-readiness ProblemDetails with use-case field paths, not Blazor section names.
- [ ] Validate tenant/owner scope for organization, group, location, room, image, template, series, registration policy, category, tag, language, custom field, and schedule item kind references.
- [ ] Reject referenced IDs unless they belong to the current tenant, are active, are allowed for selected publication context, and are usable by the actor for the requested operation.
- [ ] Resolve client owner IDs against authorized publication contexts server-side.
- [ ] Ignore client status on draft create; publish state transition only through `PublishEventCommand`.
- [ ] Ensure draft can save without image unless policy requires it.
- [ ] Ensure failed image upload does not corrupt draft state.
- [ ] Validate image references are tenant-scoped and active.
- [ ] Define replacement image cleanup or tombstone behavior.
- [ ] Write audit record and outbox event in the same transaction for publish.
- [ ] Prevent direct external side effects in publish handler; use outbox/background processing for notifications, search, feeds, calendar, activity, and analytics.
- [ ] Add trace spans and structured logs around draft create/update/readiness/publish.
- [x] Update API endpoints, route names, response metadata, and controller actions for creation context.
- [ ] Update HAL policies/assemblers for create draft, update draft, publish readiness, publish, and publication contexts.
- [x] Regenerate OpenAPI and Blazor generated client.
- [x] Update Blazor service layer methods after OpenAPI and generated client regeneration.
- [ ] Add Application tests for draft validation, publish validation, schedule composition, timezone handling, tenant/reference validation, and audit acceptance.

Acceptance criteria:

- [ ] Blazor no longer owns internal schedule graph/temp-key composition for the primary create path.
- [ ] Save draft and publish are separate Application/API use cases.
- [ ] Old graph create path is not preserved as a compatibility shim.
- [ ] Publish actions and context options are HAL/server-backed.
- [ ] Application tests prove reference IDs are tenant/owner-scoped, not merely existent.
- [ ] ProblemDetails contract covers readiness errors and concurrency conflicts.
- [ ] Publish handler writes state, audit, and outbox atomically.

## Phase 2: Progressive Create Page Skeleton

- [x] Reduce initial visible form to essential fields.
- [ ] Add `CreateEventFormModel` for UI-only state: expanded sections, publication context, quick session rows, drawer state, temp client IDs, upload preview, and summaries.
- [x] Load `EventCreationContext` before rendering policy-dependent owner/context/format/default fields.
- [x] Replace User/Organization/Group tab-like selector with a redesigned creation-context-first publication-context selector.
- [x] Show context status such as approval required, can publish, or draft only.
- [x] Add visible label for event name and any fields currently relying on placeholders.
- [ ] Add quiet timezone summary and edit affordance near date/time.
- [ ] Move Event Appearance into More options.
- [x] Move taxonomy-heavy options out of the initial visible flow.
- [ ] Move image/appearance to a compact desktop secondary column.
- [ ] Keep mobile first viewport focused on essential fields, not image upload.
- [ ] Wire Save draft to the draft use case through the Blazor service layer.
- [ ] Wire `Review and publish` to publish readiness through the Blazor service layer.
- [ ] Show review confirmation before final publish.
- [ ] Wire `Publish event` inside confirmation to publish use case.
- [ ] Expand and focus relevant sections when server validation/readiness errors return.
- [ ] Map use-case field paths such as `schedule.sessions[0].date` to visible UI sections.
- [ ] Display optimistic-concurrency conflict errors with reload/merge guidance.

Acceptance criteria:

- [ ] Simple draft save works without publish-only fields.
- [x] No tabs or steppers introduced.
- [x] Essential fields remain visible without expansion.
- [ ] Policy-required fields are elevated when required.
- [ ] UI actions are gated by service/HAL affordances, not local role checks.
- [x] Creation-context policy drives owner/context/format/default UI.

## Phase 3: Unified Schedule

- [ ] Replace scheduling toggles with one Schedule section.
- [x] Add schedule summary text.
- [ ] Add `Add schedule details`.
- [ ] Add `Add another session`.
- [ ] Add `Add itinerary item`.
- [ ] Add conditional `Add rooms` only when format/context supports rooms.
- [ ] Infer day groups from local session and itinerary dates.
- [ ] Allow day label editing only after days exist.
- [ ] Send user-facing schedule input to Application schedule composition.
- [ ] Keep rooms, itinerary items, day grouping, day labels, and schedule-specific capacity/time details inside Schedule.
- [ ] Keep timezone summary near date/time and editable through schedule precision.
- [ ] Announce schedule additions/removals.

Acceptance criteria:

- [ ] One-session events do not expose day/room/agenda controls.
- [ ] Online events do not expose rooms.
- [ ] Multi-date schedules infer days deterministically.
- [ ] Ambiguous/invalid local times are handled with actionable errors.
- [ ] DTO/internal graph mapping is owned by Application, not Blazor.
- [ ] Rooms and itinerary are not moved into More options.

## Phase 4: Session Drawer Simplification

- [ ] Introduce a quick session editor component or split component path.
- [ ] Avoid mode-flag explosion in `SessionEditorPanel`.
- [ ] Show only title/date/start/end initially.
- [ ] Collapse Description.
- [ ] Collapse Different location.
- [ ] Collapse Capacity and registration.
- [ ] Collapse Languages.
- [ ] Collapse Custom image.
- [ ] Collapse Blueprint/custom fields.
- [ ] Add inheritance summaries.
- [ ] Store explicit overrides only.
- [ ] Compute effective values for summaries, validation, and submission.
- [ ] Restore focus on drawer close.
- [ ] Support keyboard close.
- [ ] Add accessible drawer title and description.
- [ ] Use mobile full-width drawer behavior where needed.
- [ ] Announce drawer open/close and major changes.
- [ ] Verify `EventEdit.razor` behavior and adjust intentionally if needed.

Acceptance criteria:

- [ ] Adding a session is fast and minimal.
- [ ] Optional overrides still work.
- [ ] Event/session inheritance semantics are explicit and test-covered.
- [ ] EventEdit usage is not accidentally broken.

## Phase 5: More Options And Policy-Aware Fields

- [x] Convert Event Options into collapsed More options.
- [ ] Group options by outcome: reach/discoverability, organization, registration/capacity, schedule precision, presentation.
- [ ] Add Visibility/audience/format summary.
- [ ] Add Categories/tags/series summary.
- [ ] Add Template/custom fields summary.
- [ ] Add Registration rules summary.
- [ ] Add Appearance summary.
- [ ] Add Timezone summary/edit path.
- [ ] Keep rooms and itinerary out of More options.
- [ ] Keep schedule-specific capacity/time details in Schedule.
- [ ] Elevate policy-required fields out of More options.
- [ ] Keep template/custom field path hidden until explicitly opened or required.
- [x] Keep categories/tags available but not prominent initially.

Acceptance criteria:

- [x] Initial page is not taxonomy-heavy.
- [ ] Summaries update when settings change.
- [x] Advanced options remain accessible.
- [ ] Required fields are never hidden in a way that invisibly blocks save/publish.
- [ ] HAL/context policy drives available actions and publication options.
- [ ] More options contains timezone override, visibility/audience, classification, registration rules, template/custom fields, and presentation.

## Phase 6: Accessibility, Mobile, Tests, And Verification

- [x] Add visible labels where missing.
- [x] Remove placeholder-only label patterns.
- [x] Add `aria-expanded`/`aria-controls` to custom reveal controls.
- [x] Announce dynamic reveals and schedule changes.
- [ ] Save and restore focus around drawer/dialog open-close paths.
- [ ] Verify publish-readiness errors focus the first relevant visible error.
- [ ] Ensure hidden inactive fields do not validate.
- [ ] Verify keyboard users can complete the whole flow.
- [ ] Verify keyboard navigation through schedule, More options, drawer, upload, save, review, confirmation, and publish.
- [ ] Polish mobile single-column layout.
- [ ] Add sticky or otherwise reachable mobile action bar.
- [ ] Verify mobile action bar does not obscure focused controls.
- [x] Avoid cramped side-by-side date/time grids on small screens.
- [x] Add/update `CreateEventProgressiveDisclosureTests`.
- [ ] Add/update `CreateEventPublishReadinessErrorTests`.
- [x] Add/update `CreateEventPublicationContextTests`.
- [ ] Add/update `CreateEventScheduleSummaryTests`.
- [ ] Add/update `SessionQuickEditorPanelTests`.
- [ ] Add/update `CreateEventFocusManagementTests`.
- [ ] Add/update `CreateEventDraftCommandHandlerTests`.
- [ ] Add/update `UpdateEventDraftCommandHandlerTests`.
- [ ] Add/update `PublishEventCommandHandlerTests`.
- [ ] Add/update `PublishReadinessValidatorTests`.
- [ ] Add/update `EventScheduleComposerTests`.
- [x] Add/update `EventCreationPolicyTests`.
- [ ] Add/update `EventReferenceAuthorizationTests`.
- [ ] Add/update `EventAuditTests`.
- [ ] Add/update `EventCreationContextEndpointTests`.
- [ ] Add/update `EventDraftEndpointTests`.
- [ ] Add/update `EventPublishEndpointTests`.
- [ ] Add/update `EventHalAffordanceTests`.
- [ ] Add/update `ProblemDetailsContractTests`.
- [ ] Add schedule inference/composition tests where practical.
- [ ] Add publish readiness error reveal/focus tests.
- [ ] Add security tests for referenced ID tenant/owner scope.
- [ ] Add audit logging acceptance tests.
- [ ] Add outbox/domain-event acceptance tests for publish side effects.
- [ ] Add object-storage failure behavior tests.
- [ ] Add self-host smoke coverage for fresh deployment, creation context load, draft create, draft update, image upload, failed image upload, readiness validation, publish, service restart, event load, and audit verification.
- [x] Run `dotnet build --configuration Release --verbosity quiet`.
- [x] Run `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.
- [x] Run `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` if Application/API code changes.
- [x] Run relevant Application/API tests when contract or handler code changes.
- [ ] Manually verify desktop and mobile behavior.

Acceptance criteria:

- [ ] WCAG 2.2 AA expectations are met for labels, focus, keyboard, and dynamic content.
- [ ] No hidden required fields block submission without visible explanation.
- [ ] Mobile layout is usable.
- [ ] Mobile action bar does not obscure focused controls.
- [x] Build passes.
- [x] Relevant tests pass.
- [x] No Clean Architecture violations.
- [ ] No MudBlazor v8 APIs introduced.
- [ ] No design-system CSS violations.
- [ ] HAL, ProblemDetails, idempotency, concurrency, outbox, audit, object-storage, and self-host smoke paths pass.
