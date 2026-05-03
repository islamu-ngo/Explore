<!-- ABOUTME: Implementation plan for the event creation progressive-disclosure experience. -->
<!-- ABOUTME: Aligns Blazor UX work with Application/API use-case contracts, HAL affordances, and Clean Architecture boundaries. -->

# Event Creation Progressive Disclosure Plan

Last Updated: 2026-05-03

## Executive Summary

Replace the current event creation experience with an essential-first progressive-disclosure flow that keeps the first screen focused while preserving explicit Application use cases, API contracts, and HAL affordances. The UX should stop presenting internal modeling terms such as sessions, day details, rooms, agenda items, templates, appearance, and taxonomy as first-class concepts before the user expresses intent.

The architecture scope is intentionally broader than a Blazor-only refactor:

> Implement progressive disclosure primarily in Blazor Client, but change Application/API contracts whenever the current request shape leaks internal model structure, blocks clean validation, or forces Blazor to own business composition.

The current `CreateEventRequest` and `CreateEvent.razor.cs` already show the risk: Blazor composes `Sessions`, `Days`, `Rooms`, `AgendaItems`, temp keys, room/day references, and draft-versus-publish state before calling the API. This plan therefore treats Application/API cleanup as a first-class phase, not a fallback.

## CTO Feedback Incorporation Summary

This revision replaces the prior “keep scope primarily in Blazor Client unless compile/contract verification proves otherwise” assumption with use-case-first boundaries:

- Blazor owns progressive disclosure state, form composition, summaries, drawer behavior, focus restoration, labels, live announcements, MudBlazor v9 usage, CSS isolation/design tokens, and service-layer calls.
- Application owns create draft, update draft, publish, publish readiness validation, schedule composition, tenant/owner policy, registration rules, taxonomy/custom field validation, and authorization-sensitive reference checks.
- API owns HTTP endpoints, generated contracts, HAL action affordances, creation-context/policy payloads, and server validation of actor permissions, tenant boundaries, owner scope, allowed publication contexts, visibility/format options, required fields, and publish eligibility.
- Domain remains free of UI, drawer, progressive-disclosure, or form-step concepts.
- Backward compatibility is not a constraint because the repository is in active development; prefer clean contracts over shims.
- Second-pass architecture gate: do not start the Blazor skeleton until create/update/publish contracts, `EventCreationContext`, HAL affordances, schedule composer ownership, timezone model, publish-readiness error contract, idempotency/concurrency, and legacy create endpoint deletion are settled.

## Current State Analysis

Verified files:

- `Explore.Application/DTOs/Event/CreateEventRequest.cs`
- `Explore.Application/DTOs/Event/Validators/CreateEventRequestValidator.cs`
- `Explore.Application/Features/Events/Requests/Commands/CreateEventCommand.cs`
- `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs`
- `Explore.Application/Services/EventActorResolver.cs`
- `Explore.Domain/Enums/EventStatusEnum.cs`
- `Explore.API/Controllers/EventController.cs`
- `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
- `Explore.Application/Hateoas/LinkRelations.cs`
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor`
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs`
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.css`
- `Explore.Blazor.Client/Pages/Events/Components/SessionEditorPanel.razor`
- `Explore.Blazor.Client/Pages/Events/Workflows/SessionEditorWorkflow.cs`
- `Explore.Blazor.Client/Pages/Events/Workflows/TimezoneWorkflow.cs`
- `Explore.Blazor.Client/Pages/Events/Models/SessionEditorModel.cs`
- `Explore.Blazor.Client/Services/EventService.cs`
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- `Explore.Blazor.Client.Tests/Pages/Event/SessionEditorPanelTests.cs`
- `Explore.Blazor.Client.Tests/Services/EventServiceTests.cs`

Current issues:

- `CreateEventRequest` is a graph-shaped create contract with `Sessions`, `Days`, `Rooms`, and `AgendaItems`; it exposes internal scheduling composition to Blazor.
- `CreateEvent.razor.cs` manually populates session/day/room/agenda nested collections and temp-key references in `PopulateSchedulingOnRequest()`.
- `SaveAsDraftAsync()` changes `EventStatusId` and reuses the same submit path; draft and publish are not separate use cases.
- The old graph-shaped create path must be deleted or replaced after the draft/update/publish path exists; active development means no compatibility shim should preserve the wrong abstraction.
- There is no first-class `EventCreationContext` resource for publication contexts, owner options, policy-required fields, allowed formats, default timezone, upload affordances, lookup affordances, or policy refresh.
- `CreateEventCommandHandler` is responsible for atomic persistence but still receives a UI-composed graph instead of a use-case-shaped draft/schedule/publish request.
- `CreateEventRequestValidator` validates existence and temp-key references, but the plan must require tenant/publisher ownership validation for every referenced ID, not only repository existence.
- `EventLinkPolicy` exposes create/edit/delete/register/session/taxonomy links, and `LinkRelations.Publish` exists, but the current create/publish flow is not modeled as explicit HAL-backed publish readiness and publish affordances.
- Existing API infrastructure includes `IdempotencyMiddleware` with `Idempotency-Key` header support; create draft and publish must intentionally use it instead of relying on accidental retry behavior.
- Existing domain entities implement `IConcurrencyAware` through `ConcurrencyStamp`; update draft and publish must require an expected concurrency value and return clear conflict responses.
- Existing API/Blazor ProblemDetails plumbing can carry machine-readable publish-readiness errors, but the plan needs a formal field-path contract rather than Blazor section names.
- Existing outbox/audit infrastructure is available; publish side effects must use transactional audit and outbox records rather than direct external side effects in the handler.
- Existing image/object-storage paths can fail independently of event draft persistence; the create flow needs explicit behavior for failed upload, tenant-scoped image references, replacement cleanup/tombstoning, and self-host S3/MinIO-compatible storage.
- The adaptive scheduling UI exposes multiple implementation concepts instead of one user-facing Schedule concept.
- Event Options and Event Appearance are too prominent for optional or policy-dependent details.
- Timezone is handled quietly in the UI, but the contract must preserve local date, local start/end time, timezone ID, and server-derived UTC instants to avoid DST/offset errors.
- `SessionEditorPanel` opens with image, blueprint, title, description, time, location, capacity, registration, languages, and custom properties; this is too much for adding a quick session.
- The title field and dynamic reveals need explicit accessibility review for visible labels, announcements, keyboard behavior, and focus restoration.
- A prior `DateOnly`/generated-client mismatch around day mapping was suspected; verify during Phase 0 and fix at the correct contract layer if present.

Research inputs:

- Project docs require Clean Architecture boundaries, Blazor service-layer usage, HAL links as the source of truth, MudBlazor v9 APIs, token-based CSS, BEM CSS isolation, accessible labels, focus restoration, and live announcements.
- Context7 MudBlazor documentation confirms current patterns for `MudDrawer @bind-Open`, temporary drawers, provider setup, `MudFileUpload` with `<CustomContent>`, and component state patterns.
- Microsoft Blazor forms guidance supports explicit `EditForm`, validation summaries, labels, and visible validation paths.
- Microsoft Clean Architecture guidance supports inward dependencies and keeping business/application use cases out of presentation code.
- Timezone research, including Noda Time concepts, supports modeling local date/time plus timezone ID and handling ambiguous/invalid local times server-side.
- RFC 9457 Problem Details fits publish-readiness and conflict errors because it preserves machine-readable API contracts while allowing Blazor to map use-case field paths to visible UI sections.
- Repository patterns already provide `Idempotency-Key`, `ConcurrencyStamp`, audit repositories, outbox processing, ProblemDetails parsing, object-storage services, and health checks; the event plan should reuse those patterns instead of inventing parallel mechanisms.

## Target Future State

Creation context:

- Blazor loads `EventCreationContext` before rendering the create form.
- API exposes a context endpoint such as `GET /events/creation-context` or `GET /event-creation/context`.
- `EventCreationContextResponse` includes `PublicationContexts`, `AllowedFormats`, `Defaults`, `Policy`, `Lookups`, and `HalLinks`.
- The context answers whether the actor can create personal, organization, or group events; which owners are available; whether approval is required; whether public visibility is allowed; whether categories, event type, or templates are required; whether rooms or online links are allowed; default timezone; and links for draft creation, publish readiness, upload, lookup, and policy refresh.
- HAL remains action links only; policy and field requirements live in the companion creation-context payload so Blazor does not hardcode enterprise policy.

Initial visible flow:

- Creation-context-driven publication-context selector instead of tab-like User/Organization/Group controls.
- Owner and context status, such as `can publish`, `requires approval`, or `draft only`.
- Event name with visible label
- Date, start time, end time, and quiet timezone summary
- Format
- Venue or meeting link based on format
- Registration mode when required by policy or user intent
- Capacity when relevant
- Description
- Collapsed Schedule card
- Collapsed More options card
- Save draft and Review and publish actions

Desktop layout:

- Main column: owner/context, title, date/time/timezone, format, location or meeting link, description, Schedule, More options.
- Compact secondary column: image, appearance summary, draft/publish status, and non-dominant upload affordance.
- Mobile layout: one column; image must not dominate the first viewport; mobile action bar must not obscure focused controls.

Schedule behavior:

- Replace the four scheduling toggles with one unified `Schedule` section.
- Default collapsed summary uses the main date/time.
- Expanded schedule shows a default session derived from the main event details.
- `Add another session` opens a simplified session drawer.
- `Add itinerary item` creates agenda details without requiring users to understand agenda modeling.
- `Add rooms` appears only for in-person/hybrid events with a venue and an API/HAL-backed affordance.
- Day grouping is inferred from session and itinerary local dates.
- Day labels become editable only after multiple dates exist.
- Rooms, itinerary items, day grouping, day labels, and schedule-specific capacity/time details stay inside Schedule, not More options.
- Schedule summary should read like human language, for example `1 session · May 8 · 2 itinerary items`.
- Server-side schedule composition remains authoritative.

More options behavior:

- Collapse advanced options behind `More options`.
- Organize advanced groups by outcome rather than table/model names:
  - Reach and discoverability: visibility, audience, format variants.
  - Organization: categories, tags, series, template/custom fields.
  - Registration and capacity: registration policy, external registration URL, capacity rules.
  - Schedule precision: timezone override and advanced schedule policy toggles only when policy allows them.
  - Presentation: image, background color/effect, optional appearance.
- Elevate policy-required fields out of More options. A field cannot be hidden if the selected publication context requires it to save or publish.
- Keep summaries visible so collapsed state is never opaque.

Session drawer behavior:

- Keep the existing right-side drawer pattern when appropriate for desktop.
- Use a split quick component, such as `SessionQuickEditorPanel`, rather than adding many mode flags to `SessionEditorPanel`.
- First view should show only session title, date, start time, and end time.
- Optional overrides are collapsed: description, different location, capacity/registration, languages, custom image, blueprint/custom fields.
- Sessions inherit event image, location, registration, languages, capacity, and timezone unless overridden.
- Store explicit overrides only; compute effective values for summaries, validation, and payload composition.
- Drawer close restores focus to the triggering control.

Draft and publish behavior:

- `Save draft` creates or updates a draft without requiring publish-only fields.
- `Review and publish` calls server publish-readiness validation.
- If readiness succeeds, show a review confirmation with owner, visibility, date/time/timezone, location/link, registration/capacity, schedule summary, required classification, and approval behavior.
- `Publish event` appears inside the confirmation and calls the separate publish use case.
- Approval or restricted publication contexts must be represented by server-provided affordances and validation messages, not Blazor role checks.
- Failed publish readiness should return actionable field/group errors that the UI can reveal and focus.
- Create draft and publish operations use idempotency keys through the repository `Idempotency-Key` pattern; update draft and publish include expected concurrency stamp/version.

Image and object-storage behavior:

- Draft can save without an image unless the selected policy explicitly requires one.
- Failed image upload does not corrupt or block an otherwise valid draft.
- Image references are tenant-scoped and active-state checked before assignment.
- Replacing an image cleans up or tombstones the old object consistently.
- Self-hosted S3-compatible or MinIO-backed storage paths are smoke-tested.

## Architecture Strategy

### Domain Layer

- No UI or progressive-disclosure concepts.
- Existing event/session/day/room/agenda concepts can remain domain concepts where they represent business state.
- Do not add drawer state, hidden/expanded flags, form steps, or Blazor-oriented summaries to Domain.

### Application Layer

- Delete or replace the old graph-shaped `CreateEventRequest` path after the new draft/update/publish path exists. Do not keep it as a compatibility shim.
- Introduce use-case-shaped requests/commands, for example:
  - `CreateEventDraftRequest` / `CreateEventDraftCommand`
  - `UpdateEventDraftRequest` / `UpdateEventDraftCommand`
  - `ValidateEventPublishReadinessRequest` / query or command equivalent
  - `PublishEventRequest` / `PublishEventCommand`
- Keep repo vocabulary: DTO `Request` types under Application DTOs and MediatR `Command` records under Features.
- `CreateEventDraftRequest` must require an idempotency key via the API `Idempotency-Key` path or an equivalent explicit contract.
- `UpdateEventDraftRequest` must include expected concurrency stamp/version.
- `PublishEventRequest` must include expected concurrency stamp/version and an idempotency key.
- Add `EventCreationContextResponse` and related DTOs for context/policy payloads when they belong in Application contract space.
- Model nested inputs by user intent, not persistence graph, for example:
  - `PublicationContextRequest`
  - `EventDetailsRequest`
  - `EventFormatRequest`
  - `EventScheduleRequest`
  - `EventRegistrationRequest`
  - `EventOptionsRequest`
- Add an Application-level `EventScheduleComposer` or `EventScheduleBuilder` that converts user-facing schedule input into event days, sessions, rooms, agenda items, effective values, and UTC instants.
- Keep `EventScheduleComposer` stateless and decoupled from EF tracking or persistence concerns.
- Validate required fields based on draft versus publish and selected publication context.
- Validate tenant ownership, active state, selected publication context, and actor usability for every reference ID: organization, group, location, room, image, template, series, registration policy, category, tag, language, custom field, agenda item kind, and schedule item references.
- Resolve owner, tenant, visibility, and publication capability server-side through actor/context policy. Ignore client-submitted status for draft creation and transition to published only through `PublishEventCommand`.
- Formalize readiness errors as use-case field paths, for example `PublishReadinessError { Code, FieldPath, Message, Severity }` with paths such as `details.title`, `schedule.primary.startTime`, `schedule.sessions[0].date`, `registration.capacity`, `classification.categoryIds`, and `publicationContext.ownerId`.
- Resolve local date, local start, local end, and timezone ID into derived UTC start/end server-side. Reject or disambiguate skipped and ambiguous local times consistently.
- Preserve manual validator instantiation in handlers per repo convention.
- Emit auditable outcomes for draft create/update, schedule changes, visibility/registration/image changes, publish, unpublish, cancel, and failed publish readiness where applicable.
- Publish handler must change event state, write audit, write an outbox event such as `EventPublishedIntegrationEvent`, and commit atomically. It must not send notifications, webhooks, search indexing, feed updates, calendar updates, activity updates, or analytics directly.

### API Layer

- Keep controllers thin: route, authorize/classify endpoint, dispatch MediatR, return result, assemble HAL.
- Expose Application use cases directly instead of a single overloaded create endpoint.
- Add `EventCreationContext` endpoint and HAL links for draft creation, publish readiness, publish, upload, lookup, and policy refresh.
- Add or update route names and HAL policies for draft creation, draft update, publish readiness, publish, and context-specific affordances.
- HAL links remain the source of truth for Blazor actions; Blazor must not infer capabilities from roles or claims. HAL should expose action links, not full form schema.
- Omit publish HAL link when policy forbids publish. Publish readiness can still explain missing requirements through ProblemDetails/readiness errors.
- Publish-related endpoints must validate actor permissions, tenant boundaries, owner scope, allowed contexts, visibility, formats, required fields, and publish eligibility server-side.
- Return RFC 9457-style ProblemDetails for readiness failures and concurrency conflicts, including machine-readable field paths that Blazor maps to UI sections.
- Reuse `Idempotency-Key` middleware for create draft and publish. Surface replay/conflict behavior clearly to Blazor service methods.
- Regenerate OpenAPI and the Blazor generated client after contract changes.

### Blazor Client Layer

- Main owner of progressive-disclosure behavior and visible form state.
- Use `CreateEventFormModel` for UI state: expanded sections, publication context, quick session rows, drawer state, temp client IDs, upload preview, summaries, and local visible validation state.
- Call typed service-layer methods, not generated API clients directly from components.
- Use server/HAL affordances for publication contexts and actions.
- Load and refresh `EventCreationContext` before hardcoding owner/context policy.
- Maintain local UI state for expansion, summaries, drawer flow, and focus restoration only.
- Avoid duplicating Application business rules; surface server readiness ProblemDetails by mapping use-case field paths to UI sections, expanding the relevant group, and focusing the first visible error.
- Show clear optimistic-concurrency conflict errors and reload/merge guidance when expected concurrency stamp/version is stale.
- Treat image upload as independent from draft persistence: preserve draft state on upload failure and never assign unvalidated image references.
- Keep MudBlazor v9 APIs and wrapper components where practical.

### CSS and Design System Layer

- Use `CreateEvent.razor.css` and component CSS isolation with BEM names.
- Use design tokens and logical properties.
- Do not add bare `.mud-*` selectors outside approved global MudBlazor override files.
- Use `DialogOptionsFactory`/wrappers for dialogs and standard controls where practical.
- Keep ordinary content elevation modest and consistent with the design system.

### Accessibility Layer

- Every input must have a visible or programmatically associated label.
- No placeholder-only labels.
- Dynamic reveals must update `aria-expanded`/`aria-controls` when custom controls are used.
- Use `IAccessibilityAnnouncerService` or equivalent live regions for added/removed schedule details, publish readiness errors, and drawer state changes.
- Use `IAccessibilityFocusService` or equivalent save/restore behavior around drawers/dialogs.
- Keyboard navigation must reach every reveal, action, upload, drawer field, and submit/publish action.
- Hidden required fields must never block submission without a visible, focusable explanation.
- Drawer open/close must preserve focus, support keyboard close, use an accessible title/description, and become mobile full-width when needed.
- Mobile action bar must not obscure focused controls.

### Enterprise and Self-Host Layer

- Preserve tenant isolation for all owner and reference checks.
- Keep local/self-host flows functional without external SaaS assumptions.
- Ensure generated clients, OpenAPI, migrations if any, and seed/lookups remain deterministic for self-host deployments.
- Include smoke acceptance for create draft, update draft, image upload failure, image replacement, publish readiness, publish, service restart, event reload, audit verification, and mobile-friendly creation paths.
- Feature flags for advanced scheduling or room management must gracefully degrade or default deterministically in self-hosted environments where SaaS configuration endpoints are unavailable.
- Health checks should cover database and object storage where applicable.

## Implementation Phases

### Phase 0: Documentation, Contract, And Architecture Review

Tasks:

- Run baseline build to know whether current generated contracts compile.
- Verify `CreateEventRequest`, generated `EventApiClient.g.cs`, and `PopulateSchedulingOnRequest()` mapping, including the suspected day `DateOnly`/client type mismatch.
- Document exactly where Blazor currently owns business composition.
- Decide final request/command names and endpoint/HAL route names.
- Define `EventCreationContextResponse`, route, policy payload shape, HAL action links, and refresh semantics.
- Decide publication-context UI behavior from creation-context options, including approval, draft-only, and can-publish states.
- Decide the fate of the old `CreateEventRequest` endpoint and generated client path; default is delete/replace, not preserve.
- Decide idempotency strategy for create draft and publish, aligning with existing `Idempotency-Key` middleware.
- Decide optimistic concurrency strategy for update draft and publish, aligning with `ConcurrencyStamp`/expected version patterns.
- Decide ProblemDetails/readiness error contract, including `PublishReadinessError` field paths and Blazor mapping rules.
- Decide IANA timezone strategy and Windows mapping if required by deployment/runtime constraints.
- Identify policy-required fields by publication context and define which fields cannot remain hidden.
- Identify required audit events and test coverage.
- Identify object-storage/image failure, replacement, tombstone/cleanup, and self-host storage smoke behavior.

Acceptance criteria:

- Contract leakage is explicitly confirmed or dismissed with file evidence.
- Draft, update draft, publish readiness, and publish use cases are named.
- Schedule composition ownership is assigned to Application.
- Domain remains unchanged unless a true domain rule gap is discovered.
- Phase 0 is a real go/no-go gate: Blazor skeleton work does not start until creation context, lifecycle contracts, schedule ownership, timezone model, readiness error contract, security rule, idempotency/concurrency, HAL action authority, old endpoint deletion, and accessibility automation scope are settled.

### Phase 1: Application/API Shape Cleanup

Tasks:

- Add use-case-shaped Application requests/commands for create draft, update draft, publish readiness, and publish.
- Add `EventCreationContext` Application/API contract and endpoint.
- Add Application schedule composer/builder for local date/time/timezone-to-domain graph composition.
- Split draft validation from publish validation.
- Add tenant/owner/reference validation beyond existence checks.
- Delete or replace the old graph-shaped `CreateEventRequest` path after the new path exists. Remove legacy `EventStatusId` mutation as a draft/publish mechanism.
- Require idempotency keys for create draft and publish through the repo-supported `Idempotency-Key` path or an explicitly approved equivalent.
- Require expected concurrency stamp/version for update draft and publish.
- Add structured publish-readiness ProblemDetails with machine-readable use-case field paths.
- Add explicit local-time handling for skipped time, ambiguous time, crossing midnight, multi-day across DST, timezone changes after sessions exist, and viewer timezone display.
- Add image reference validation and object-storage failure behavior.
- Add transactional audit and outbox records for publish side effects.
- Add trace spans and structured logs around draft create, draft update, readiness validation, and publish.
- Update API endpoints, route names, response metadata, OpenAPI, generated Blazor client, HAL policies, and service methods.

Acceptance criteria:

- Blazor no longer needs to build internal day/room/session/agenda temp-key graphs for the primary create path.
- Save draft and publish are separate Application/API use cases.
- Legacy graph-shaped create contract is gone from the primary create flow.
- Publish affordances and readiness are HAL/server-backed, and HAL remains action links only.
- Readiness and conflict failures return ProblemDetails that Blazor can map without server returning Blazor section names.
- Application tests cover draft validation, publish validation, schedule composition, timezone conversion, reference authorization, tenant isolation, idempotency, concurrency, audit, and outbox behavior.

### Phase 2: Progressive Create Page Skeleton

Tasks:

- Restructure `CreateEvent.razor` so the default visible form contains only essential fields.
- Add `CreateEventFormModel` to own UI-only state such as expanded sections, selected publication context, quick session rows, drawer state, temp client IDs, upload preview, and summaries.
- Load `EventCreationContext` before rendering policy-dependent owner/context/format/default fields.
- Replace tab-like User/Organization/Group patterns with a creation-context-driven publication-context selector.
- Add quiet timezone summary/edit affordance near date/time.
- Move optional appearance and taxonomy-heavy fields out of the first visible flow.
- Move image/appearance into a compact secondary column on desktop and keep the mobile first viewport focused on essential fields.
- Add separate `Save draft` and `Review and publish` actions wired through the Blazor service layer.
- Surface server validation/readiness errors by expanding and focusing the relevant section.
- Display optimistic-concurrency conflict errors with reload/merge guidance.

Acceptance criteria:

- No tabs or steppers are introduced.
- Essential fields remain visible without requiring expansion.
- Policy-required fields are visible when required.
- Simple draft save works without publish-only fields.
- Creation-context policy drives owner/context/format/default UI, not hardcoded enterprise rules.

### Phase 3: Unified Schedule

Tasks:

- Replace separate scheduling toggles with one `Schedule` card.
- Add user-facing actions: `Add schedule details`, `Add another session`, `Add itinerary item`, `Add rooms`, `Customize day names`.
- Show `Add rooms` only when format/context supports rooms.
- Infer days from local schedule dates.
- Send user-facing schedule input to Application schedule composition instead of building internal temp-key graphs in Blazor.
- Add visible timezone summary and edit path.
- Keep rooms, itinerary, day grouping, day labels, and schedule-specific capacity/time details inside Schedule.
- Add explicit UI mapping for readiness errors under `schedule.*` field paths.

Acceptance criteria:

- A simple one-session event does not expose day/room/agenda controls.
- Multi-date schedules infer days deterministically.
- Online-only events do not expose rooms.
- Ambiguous/invalid local times are handled by server validation with actionable UI feedback.
- Rooms and itinerary are not moved into More options.

### Phase 4: Session Drawer Simplification

Tasks:

- Introduce a quick session editor component or split component path instead of adding mode-flag complexity to `SessionEditorPanel`.
- Show only title/date/start/end initially.
- Collapse optional overrides.
- Add inheritance summaries such as `Uses event location` and `Uses event image`.
- Store overrides only and compute effective values for summaries and submission.
- Verify `EventEdit.razor` usage and adjust intentionally if the shared editor changes.
- Restore focus on drawer close.
- Support keyboard close, accessible drawer title/description, mobile full-width behavior, and live announcements.

Acceptance criteria:

- Adding a session is fast and minimal.
- Optional overrides still work.
- Inheritance semantics are explicit and test-covered.
- Event edit behavior is not accidentally regressed.

### Phase 5: More Options And Policy-Aware Fields

Tasks:

- Convert visible Event Options into collapsed `More options`.
- Reorganize groups by outcomes: reach/discoverability, organization, registration/capacity, schedule precision, presentation.
- Add summaries for each group.
- Elevate policy-required fields when selected context requires them.
- Keep template/custom field path hidden until explicitly opened or required by policy/template.
- Keep categories/tags available but not prominent on initial load.
- Keep timezone override/edit path in schedule precision, but keep schedule structure, rooms, itinerary, day grouping, day labels, and schedule-specific capacity/time details in Schedule.

Acceptance criteria:

- Initial page is not taxonomy-heavy.
- Summaries update as selections change.
- Required fields are never hidden in a way that blocks save/publish invisibly.
- HAL/context policy drives available options and actions.

### Phase 6: Accessibility, Mobile, Tests, And Verification

Tasks:

- Add visible labels where placeholders are currently doing label work.
- Add `aria-expanded`/`aria-controls` to custom reveal controls.
- Announce dynamic reveals and schedule changes.
- Validate keyboard navigation through schedule, More options, drawer, upload, save, review, and publish confirmation.
- Polish mobile single-column layout and action bar behavior.
- Add review confirmation tests for owner, visibility, date/time/timezone, location/link, registration/capacity, schedule summary, required classification, approval behavior, and final `Publish event` action.
- Add/update Blazor component tests for progressive disclosure, drawer focus, hidden required fields, publish-readiness ProblemDetails mapping, publication context, schedule summary, mobile action bar, image upload failure, and session inheritance.
- Add/update Application tests for draft/publish split, schedule composition, timezone conversion, policy-required fields, reference tenant validation, idempotency, optimistic concurrency, audit events, and outbox writes.
- Add API contract tests for `EventCreationContext`, draft endpoints, publish endpoints, HAL affordances, ProblemDetails/readiness errors, and conflict errors.
- Add timezone tests for DST spring-forward skipped time, DST fall-back ambiguous time, event crossing midnight, multi-day event across DST boundary, user timezone changes after sessions exist, and online event viewed in another timezone.
- Add self-host smoke for fresh deployment, load creation context, create draft, update draft, upload image, handle image upload failure, validate readiness, publish, restart services, load event, and verify audit.
- Run architecture/API/Blazor tests and release build.

Acceptance criteria:

- WCAG 2.2 AA expectations are met for labels, focus, keyboard, and dynamic content.
- Mobile creation flow is usable without horizontal compression.
- Mobile action bar does not obscure focused controls.
- Build and relevant tests pass.
- No Clean Architecture violations.
- No MudBlazor v8 APIs or design-system CSS violations are introduced.
- Self-host smoke acceptance covers context load, draft create/update, image upload/failure, readiness, publish, restart, event load, and audit verification.

## Detailed Task List

1. Run Phase 0 as a real architecture gate.
   Acceptance criteria: baseline known; old create endpoint fate, `EventCreationContext`, draft/update/publish contracts, HAL action affordances, schedule composer ownership, timezone model, readiness error contract, security rule, idempotency/concurrency, and accessibility automation are settled before Blazor skeleton work begins.

2. Define `EventCreationContextResponse` and publication-context UI behavior.
   Acceptance criteria: Blazor can load policy, defaults, lookups, allowed values, owner options, approval behavior, and action links without hardcoding enterprise policy.

3. Delete or replace the old graph-shaped `CreateEventRequest` path after the new draft/update/publish path exists.
   Acceptance criteria: no compatibility shim preserves the old `Sessions`/`Days`/`Rooms`/`AgendaItems` temp-key create path, and client `EventStatusId` mutation disappears.

4. Add use-case-shaped Application/API draft, update, publish readiness, and publish contracts.
   Acceptance criteria: `CreateEventDraftRequest`, `UpdateEventDraftRequest`, readiness validation, and `PublishEventRequest` or command equivalents align with repo CQRS vocabulary and manual validators.

5. Add idempotency and optimistic concurrency.
   Acceptance criteria: create draft and publish use `Idempotency-Key` or approved equivalent; update draft and publish require expected concurrency stamp/version; conflict ProblemDetails are UI-mappable.

6. Add structured publish-readiness ProblemDetails.
   Acceptance criteria: readiness failures return machine-readable `PublishReadinessError` entries with use-case field paths such as `details.title`, `schedule.sessions[0].date`, and `publicationContext.ownerId`.

7. Add Application schedule composition and timezone correctness.
   Acceptance criteria: local date/start/end/timezone input produces deterministic days/sessions/rooms/agenda/UTC instants server-side, with explicit DST skipped/ambiguous, crossing-midnight, multi-day DST, timezone-change, and viewer-timezone tests.

8. Refactor default create page hierarchy and publication selector.
   Acceptance criteria: main column owns owner/context/title/date-time/format/location-description/Schedule/More options; compact secondary column owns image/appearance/status; mobile is one column and image does not dominate the first viewport.

9. Build unified Schedule and keep schedule structure out of More options.
   Acceptance criteria: sessions, itinerary items, rooms, day grouping, day labels, and schedule-specific capacity/time details stay in Schedule; More options contains only timezone override, visibility/audience, classification, registration rules, template/custom fields, and presentation.

10. Simplify session drawer.
    Acceptance criteria: first-open session editing is title/date/start/end only, optional inherited overrides are collapsed, focus is restored, keyboard close works, and mobile drawer is full-width.

11. Add review-and-publish confirmation.
    Acceptance criteria: `Review and publish` runs readiness, valid drafts show owner/visibility/timezone/location/registration/schedule/classification/approval summary, and final confirmation action is `Publish event`.

12. Add image/object-storage failure behavior.
    Acceptance criteria: draft can save without image unless policy requires it; failed upload does not corrupt draft; image refs are tenant-scoped/active; replacement cleans up or tombstones old image; S3/MinIO-compatible self-host path is tested.

13. Add security, audit, outbox, observability, accessibility, mobile, and self-host tests.
    Acceptance criteria: reference tenant checks, overposting protections, audit records, transactional outbox publish events, trace/log spans, focus/keyboard behavior, mobile layout, and self-host smoke paths are covered.

14. Run verification.
    Acceptance criteria: release build and relevant tests pass.

## Risks And Mitigations

Risk: Existing create page has tightly coupled markup, UI state, and request composition.

Mitigation: Move business composition to Application first, then simplify Blazor against cleaner service methods.

Risk: Hidden fields may still participate in validation.

Mitigation: Split draft and publish validation; elevate policy-required fields; focus server readiness errors.

Risk: Schedule inference may create duplicate or unstable day ordering.

Mitigation: Centralize schedule composition in Application and sort by local date/time with timezone-aware rules.

Risk: Session drawer is shared by create and edit flows.

Mitigation: Prefer a split quick-create component and verify edit behavior explicitly.

Risk: HAL affordances may lag new use cases.

Mitigation: Treat route names, policies, assemblers, OpenAPI, generated client, and Blazor service methods as one contract change.

Risk: Timezone handling may silently accept invalid or ambiguous local times.

Mitigation: Validate local date/time/timezone server-side and return actionable correction messages.

## Success Metrics

- Initial create form has materially fewer visible fields.
- Users can save a simple draft without touching advanced sections.
- Publish is a distinct, server-validated action.
- Blazor no longer composes internal scheduling graph/temp-key details for primary create.
- Multi-session, itinerary, room, and day-label workflows remain possible.
- Session drawer first-open complexity is reduced.
- Mobile flow is single-column and usable.
- Accessibility checks pass for labels, focus, live updates, and keyboard navigation.
- Application tests cover schedule composition, timezone handling, reference authorization, draft/publish validation, and audit acceptance.
- Build and relevant tests pass.

## Resources And Dependencies

Internal docs:

- `CLAUDE.md`
- `docs/ARCHITECTURE.md`
- `docs/BLAZOR.md`
- `docs/DESIGN_SYSTEM.md`
- `docs/ACCESSIBILITY.md`
- `docs/DOMAIN.md`
- `docs/GOVERNANCE.md`
- `docs/QUICK_REFERENCE.md`
- `dev/active/README.md`
- `docs/DOCUMENTATION_STYLE_GUIDE.md`

Research:

- Context7 MudBlazor docs: drawer binding, file upload `CustomContent`, providers, component state.
- Microsoft Blazor docs: forms, validation, labels, validation summary.
- Microsoft Clean Architecture guidance: inward dependencies and application/use-case ownership.
- Timezone research: local date/time plus timezone ID, ambiguous/invalid local times, server-derived UTC.
- RFC 8288/HAL guidance: links describe affordances; field requirements and policy data belong in context payloads, not HAL links.
- RFC 9457/ASP.NET ProblemDetails guidance: use stable problem types and machine-readable fields for readiness and conflict responses.
- Idempotency-Key guidance and repo `IdempotencyMiddleware`: create/publish retry safety should align with the existing header path.
- EF Core concurrency guidance and repo `IConcurrencyAware`: update/publish should use expected concurrency stamps or an approved version token.
- Noda Time/TZDB guidance: prefer IANA timezone IDs, define Windows mapping if needed, and explicitly test skipped/ambiguous local times.
- OpenTelemetry and health-check guidance: add spans/logs and database/object-storage health coverage for the create/publish flow.

Key implementation files:

- `Explore.Application/DTOs/Event/CreateEventRequest.cs`
- `Explore.Application/DTOs/Event/Validators/CreateEventRequestValidator.cs`
- `Explore.Application/Features/Events/Requests/Commands/CreateEventCommand.cs`
- `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs`
- `Explore.Application/Services/EventActorResolver.cs`
- `Explore.API/Middleware/IdempotencyMiddleware.cs`
- `Explore.Domain/Interfaces/IConcurrencyAware.cs`
- `Explore.Persistence/ExploreDbContext.SaveChanges.cs`
- `Explore.Persistence/EfCoreUnitOfWork.cs`
- `Explore.Domain/OutboxMessage.cs`
- `Explore.API/BackgroundServices/OutboxProcessor.cs`
- `Explore.Infrastructure/Messaging/MqContractOutboxMessageDispatcher.cs`
- `Explore.Application/Models/IntegrationEvents/EventPublishedIntegrationEvent.cs`
- `Explore.API/ExceptionHandling/GlobalExceptionHandler.cs`
- `Explore.API/ExceptionHandling/ValidationExceptionHandler.cs`
- `Explore.API/Controllers/EventController.cs`
- `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
- `Explore.Application/Hateoas/LinkRelations.cs`
- `Explore.API/Controllers/StorageObjectController.cs`
- `Explore.Application/Contracts/Infrastructure/IObjectStorageService.cs`
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor`
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs`
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.css`
- `Explore.Blazor.Client/Pages/Events/Components/SessionEditorPanel.razor`
- `Explore.Blazor.Client/Services/EventService.cs`
- `Explore.Blazor.Client/Services/ImageStorageService.cs`
- `Explore.Blazor.Client/Exceptions/ApiProblemException.cs`
- `Explore.Blazor/Extensions/BffStorageEndpoints.cs`
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs`

## Effort Estimate

- Phase 0: 0.5 to 1 day
- Phase 1: 1.5 to 2 days
- Phase 2: 1 day
- Phase 3: 1 to 1.5 days
- Phase 4: 1 day
- Phase 5: 0.75 to 1 day
- Phase 6: 1 to 1.5 days

Total: 7 to 9 days depending on contract-generation friction, test harness coverage, and timezone/policy validation depth.

## Potential Risks & Unknowns

- The suspected `DateOnly`/generated-client mismatch must be verified against the current build and generated client.
- Existing tests may not have sufficient component coverage for `CreateEvent.razor`; new tests may require harness setup.
- Final request names may change during Phase 0 after route/HAL review.
- Publishing policy and approval workflows may require additional affordance modeling beyond the current event links.
- Self-host smoke coverage may need new test infrastructure if no existing smoke harness fits.
