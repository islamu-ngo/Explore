<!-- ABOUTME: Working context for the event creation progressive-disclosure implementation plan. -->
<!-- ABOUTME: Captures findings, decisions, constraints, and resume notes for future development sessions. -->

# Event Creation Progressive Disclosure Context

Last Updated: 2026-05-03

## Session Progress

Completed:

- Reviewed active plan directory `dev/active/event-creation-progressive-disclosure/`.
- Re-read the saved plan, context, and task files after the user confirmed they had been saved.
- Reviewed existing `dev/active` documentation conventions and documentation style guidance.
- Loaded relevant skills: Clean Architecture, Blazor UI conventions, and Design System.
- Reviewed project docs for architecture, Blazor, design system, accessibility, and quick-reference rules.
- Researched MudBlazor v9 behavior with Context7.
- Researched Blazor forms/validation, Clean Architecture, and timezone modeling with Tavily/web sources.
- Collected external-docs research confirming MudBlazor expansion/drawer/dialog patterns, WCAG focus guidance, HAL affordance rules, and timezone modeling guidance.
- Explored current Application/API/Blazor event creation contracts and HAL policies.
- Updated the implementation plan away from Blazor-only scope and toward use-case-first Application/API contracts.
- Updated the task checklist to include Phase 0 contract review, Application/API cleanup, security, audit, accessibility, mobile, and self-host gates.
- Incorporated second CTO review amendments: creation context, strict legacy create path deletion, idempotency, optimistic concurrency, publish-readiness ProblemDetails, DST/timezone edge cases, review-and-publish confirmation, object-storage failure behavior, transactional outbox, observability, and stronger security wording.
- Collected planning-doc consistency, architecture review, official docs research, and code-pattern exploration results for the second-pass update.
- Implemented the first Blazor progressive-disclosure slice in `CreateEvent.razor`, `CreateEvent.razor.cs`, and `CreateEvent.razor.css`.
- Added a visible `Event name` label to the title input.
- Renamed the primary create action to `Review and publish` and the draft menu action to `Save draft` while keeping the existing single-submit backend path for this slice.
- Wrapped schedule details in a single collapsed `Schedule` disclosure with summary text and `aria-controls` on the existing reveal buttons.
- Wrapped taxonomy-heavy Event Options in a collapsed `More options` disclosure with summary text.
- Added mobile stacking for inline schedule add rows.
- Updated `CreateEventTests` coverage for the visible review action and progressive-disclosure sections.
- Verified `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet --no-restore` passes with 970 succeeded and 1 pre-existing skipped.
- Verified `dotnet build --configuration Release --verbosity quiet` passes with warnings only.
- Added first-class Application DTOs for server-owned event creation context: `EventCreationContextDto` and `EventCreationPublisherOptionDto`.
- Added `GetEventCreationContextRequest` and `GetEventCreationContextRequestHandler` to resolve personal, organization, and group publisher options from tenant policy plus `event:create` permission checks.
- Added authenticated API endpoint `GET /api/event/creation-context` named `RouteNames.GetEventCreationContext`.
- Added creation context DTOs to `ExploreJsonContext` for API source-generated serialization.
- Added Application unit tests for personal default context, permission-backed organization/group options, and unavailable context.
- Verified `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-restore` passes with 1092 succeeded.
- Verified `dotnet build --configuration Release --verbosity quiet` passes with warnings only after the creation context endpoint slice.
- Verified `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --no-restore` passes with 142 succeeded after Application/API changes.
- Regenerated `Explore.API/swagger.json` by running the API with the HTTPS launch profile so the development OpenAPI export can follow the configured HTTPS redirect.
- Regenerated `Explore.Blazor.Client/Clients/EventApiClient.g.cs` from the updated OpenAPI document via the Blazor client build.
- Added `IEventService.GetEventCreationContextAsync(CancellationToken)` and the `EventService` generated-client wrapper.
- Added generated creation-context DTOs to `AppJsonSerializerContext`.
- Added Blazor service tests for creation-context success, API failure fallback, and cancellation-token forwarding.
- Verified `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet --no-restore` passes with 973 succeeded and 1 pre-existing skipped.
- Verified `dotnet build --configuration Release --verbosity quiet` passes with warnings only after OpenAPI/client/service wiring.
- Attempted `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore`; it did not complete before the 180s command timeout and produced no final test summary, so API integration coverage remains inconclusive for this slice.
- Loaded `EventCreationContext` in `CreateEvent.razor.cs` through `IEventService.GetEventCreationContextAsync()` during form initialization.
- Updated create-page publisher mode gating so personal, organization, and group availability comes from server-owned creation context when present, with local role checks only as a fallback if the context cannot be loaded.
- Applied creation-context defaults to the selected publisher mode and first publishable organization/group option.
- Added create-page alerts for unavailable creation context and blocked creation access.
- Updated organization/group dropdown validation to use creation-context publisher option reasons instead of local role text when server context is available.
- Added `CreateEventTests` coverage that the page loads creation context and blocks submit when the server reports no available publisher.
- Verified `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet --no-restore` passes with 974 succeeded and 1 pre-existing skipped after create-page context wiring.
- Verified `dotnet build --configuration Release --verbosity quiet` passes with warnings only after create-page context wiring.
- Replaced the tab-like User/Organization/Group publisher buttons with one `Publishing as` selector driven by creation-context publisher options.
- Kept legacy organization/group dropdowns only as a fallback when creation context cannot be loaded.
- Added selector option rendering with publisher icons, display names, server-provided disabled reasons, and publishable-option gating.
- Added `CreateEventTests` coverage that the publication-context selector renders and the legacy publisher buttons do not.
- Verified `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet --no-restore` passes with 975 succeeded and 1 pre-existing skipped after the selector replacement.
- Verified `dotnet build --configuration Release --verbosity quiet` passes with warnings only after the selector replacement.

In progress:

- Next Phase 1 slice: draft/update/readiness/publish contracts and Application-owned schedule composition.

Blockers:

- API integration test project did not complete within the local command timeout; add a targeted creation-context endpoint integration test or rerun with a longer timeout before PR handoff.

## Key Decisions

- No tabs.
- No stepper.
- Use one continuous progressive-disclosure flow.
- Blazor remains responsible for UI progressive disclosure, summaries, drawer state, focus, labels, announcements, MudBlazor v9 usage, CSS isolation, and service-layer calls.
- Application/API changes are first-class when current request shapes leak internal model structure or force Blazor to own business composition.
- Replace the old scope statement with: “Implement progressive disclosure primarily in Blazor Client, but change Application/API contracts whenever the current request shape leaks internal model structure, blocks clean validation, or forces Blazor to own business composition.”
- Draft and publish must be separate use cases.
- Creation context must be a first-class Application/API resource loaded before the create page.
- HAL exposes action links only; publication policy, allowed values, field requirements, defaults, and lookups belong in `EventCreationContextResponse` or equivalent context payload.
- Delete or replace the old graph-shaped `CreateEventRequest` path after draft/update/publish exists. Do not keep it as a compatibility shim.
- Schedule is one user-facing concept, but Application owns authoritative schedule composition.
- Rooms, itinerary items, day grouping, day labels, and schedule-specific capacity/time details stay inside Schedule, not More options.
- Store explicit session overrides only; compute effective values for summaries, validation, and submission.
- Publication context and actions must be HAL/server-backed, not Blazor role-checked.
- Save flow labels are `Save draft`, `Review and publish`, then `Publish event` inside confirmation.
- Create draft and publish must use idempotency keys through the repo-supported `Idempotency-Key` path or approved equivalent.
- Update draft and publish must include expected concurrency stamp/version and surface conflict ProblemDetails.
- Publish-readiness errors must use machine-readable use-case field paths, not Blazor section names.
- Local date, local start/end, timezone ID, and server-derived UTC instants are required; server owns skipped/ambiguous local time resolution.
- Publish side effects must use transactional audit and outbox records; handlers must not run direct external side effects.
- Image upload failure must not corrupt draft state; image references must be tenant-scoped and active.
- Policy-required fields cannot remain hidden inside More options.
- Do not add backward-compatibility shims; project is in development mode.

## Key Files

Planning docs:

- `dev/active/event-creation-progressive-disclosure/event-creation-progressive-disclosure-plan.md`
- `dev/active/event-creation-progressive-disclosure/event-creation-progressive-disclosure-context.md`
- `dev/active/event-creation-progressive-disclosure/event-creation-progressive-disclosure-tasks.md`

Application/API contracts:

- `Explore.Application/DTOs/Event/CreateEventRequest.cs`
- `Explore.Application/DTOs/Event/Validators/CreateEventRequestValidator.cs`
- `Explore.Application/Features/Events/Requests/Commands/CreateEventCommand.cs`
- `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs`
- `Explore.Application/DTOs/Event/EventCreationContextDto.cs`
- `Explore.Application/DTOs/Event/EventCreationPublisherOptionDto.cs`
- `Explore.Application/Features/Events/Requests/Queries/GetEventCreationContextRequest.cs`
- `Explore.Application/Features/Events/Handlers/Queries/GetEventCreationContextRequestHandler.cs`
- `Explore.API/swagger.json`
- `Explore.Application/Services/EventActorResolver.cs`
- `Explore.Domain/Enums/EventStatusEnum.cs`
- `Explore.API/Controllers/EventController.cs`
- `Explore.API/Middleware/IdempotencyMiddleware.cs`
- `Explore.Domain/Interfaces/IConcurrencyAware.cs`
- `Explore.Persistence/ExploreDbContext.SaveChanges.cs`
- `Explore.Persistence/EfCoreUnitOfWork.cs`
- `Explore.Domain/OutboxMessage.cs`
- `Explore.API/BackgroundServices/OutboxProcessor.cs`
- `Explore.Infrastructure/Messaging/MqContractOutboxMessageDispatcher.cs`
- `Explore.Application/Hateoas/LinkRelations.cs`
- `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
- `Explore.API/ExceptionHandling/GlobalExceptionHandler.cs`
- `Explore.API/ExceptionHandling/ValidationExceptionHandler.cs`
- `Explore.Blazor.Client/Exceptions/ApiProblemException.cs`
- `Explore.API/Controllers/StorageObjectController.cs`
- `Explore.Application/Contracts/Infrastructure/IObjectStorageService.cs`
- `Explore.Blazor.Client/Services/ImageStorageService.cs`
- `Explore.Blazor/Extensions/BffStorageEndpoints.cs`

Blazor implementation:

- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor`
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs`
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.css`
- `Explore.Blazor.Client/Pages/Events/Components/SessionEditorPanel.razor`
- `Explore.Blazor.Client/Pages/Events/Workflows/SessionEditorWorkflow.cs`
- `Explore.Blazor.Client/Pages/Events/Workflows/TimezoneWorkflow.cs`
- `Explore.Blazor.Client/Pages/Events/Models/SessionEditorModel.cs`
- `Explore.Blazor.Client/Services/EventService.cs`
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- `Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs`

Tests to revisit:

- `Explore.Blazor.Client.Tests/Pages/Event/SessionEditorPanelTests.cs`
- `Explore.Blazor.Client.Tests/Services/EventServiceTests.cs`
- Application tests for event creation/publishing once new use cases exist.
- Architecture tests if Application/API contracts change.

## Current Implementation Notes

- `CreateEventRequest` currently exposes nested `Sessions`, `Days`, `Rooms`, and `AgendaItems` collections.
- This graph-shaped request is now treated as proven leakage; future implementation should delete/replace it after the new draft/update/publish path exists.
- `CreateEventRequestValidator` validates temp keys and many reference existence checks, but the plan must require tenant/publisher ownership validation for referenced IDs.
- `CreateEventCommand` carries a `CreateEventRequest` and only provides organization ID as a resource attribute when present.
- `CreateEventCommandHandler` persists event, days, rooms, sessions, agenda, taxonomy, template properties, image actor assignment, metrics, and cache invalidation in one path.
- `EventActorResolver` enforces publishing policy for personal/org/group publishing.
- `EventStatusEnum` includes `Draft=1` and `Published=2`.
- `EventController.Create` accepts `[FromBody] CreateEventRequest` and dispatches `CreateEventCommand`; no separate create draft/publish endpoints were observed in the current create path.
- `LinkRelations.Publish` exists, but current event HAL policies need review/extension for explicit publish readiness and publish affordances.
- `CreateEvent.razor.cs` sets draft by mutating `EventStatusId` and reusing `HandleSubmit()`.
- `PopulateSchedulingOnRequest()` manually builds sessions, days, rooms, agenda items, temp keys, and references in Blazor.
- The first implemented UI slice intentionally did not change Application/API contracts, so the old graph-shaped create path, draft status mutation, and Blazor schedule graph composition remain known debt for Phase 1.
- `CreateEvent.razor` now hides schedule details and More options behind MudBlazor expansion panels without introducing tabs or steppers.
- `CreateEvent.razor.cs` now computes `ScheduleSummary` and `MoreOptionsSummary` for the collapsed sections.
- `GET /api/event/creation-context` now returns server-owned creation context for personal, organization, and group publisher options.
- `GetEventCreationContextRequestHandler` uses `ITenantPolicySettingService` plus `IOrganizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(..., PermissionCodes.EventCreate)` and `IGroupMemberRepository.GetGroupIdsWhereUserHasPermission(..., PermissionCodes.EventCreate)` so context options align with `EventActorResolver` create authorization.
- The generated NSwag client now includes `GetEventCreationContextAsync(...)` from `GET /api/event/creation-context`.
- `EventService.GetEventCreationContextAsync(...)` wraps the generated client so Blazor pages can load server-owned publisher context through the service layer.
- `CreateEvent.razor.cs` now loads creation context during initialization, applies the default publisher mode, disables unavailable publisher modes, blocks submit when `CanCreate` is false, and uses publisher option reasons for organization/group validation.
- `CreateEvent.razor` now uses a single creation-context-first `Publishing as` selector instead of tab-like User/Organization/Group buttons.
- Local publisher role checks and organization/group dropdowns remain only as fallback behavior when context-load failure leaves `_creationContext` null.
- OpenAPI export requires the `https` launch profile locally because Development HTTPS redirection targets port `7039`; HTTP-only launch attempts redirect and fail the exporter.
- `SessionEditorPanel` currently exposes many optional fields on first open.
- `IdempotencyMiddleware` already supports optional `Idempotency-Key` replay for write requests; create draft and publish should intentionally require/use this pattern.
- Domain entities use `IConcurrencyAware` and persistence updates `ConcurrencyStamp`; update draft and publish should require expected concurrency stamp/version.
- Global exception and validation handlers already produce ProblemDetails, and Blazor parses ProblemDetails via `ApiProblemException`; readiness errors should extend this contract with use-case field paths.
- Generic `OutboxMessage`, `OutboxProcessor`, and `MqContractOutboxMessageDispatcher` already support at-least-once integration events including `EventPublished`-style payloads.
- Storage upload uses server storage endpoints plus Blazor BFF upload proxy; create docs must account for upload failure and tenant-scoped image references.

## Constraints

- Follow Clean Architecture boundaries.
- Domain must not reference UI/progressive-disclosure concepts.
- Application owns use cases, validators, schedule composition, publish readiness, and reference/policy validation.
- API owns route/HAL affordances and server-side validation of actor permissions, tenant boundaries, allowed contexts, formats, visibility, required fields, and publish eligibility.
- API must expose creation context/policy payloads separately from HAL action links.
- Blazor Client must call service layer, not generated API clients directly from components.
- HAL links remain the source of truth for UI action affordances.
- Blazor must not trust local role checks, owner IDs, visibility, client status, or referenced IDs.
- Use MudBlazor v9 APIs.
- Use design tokens and CSS isolation conventions.
- Do not add bare `.mud-*` selectors outside approved global overrides.
- Add or preserve two-line ABOUTME headers in edited docs/code where practical.
- Every input needs accessible labeling.
- Dynamic reveals need announcements/focus management.
- Hidden required fields must never block submission without visible explanation.
- Enterprise/self-host paths must remain deterministic and tenant-safe.
- Publish side effects must be idempotent and outbox-driven.
- Object storage health/failure behavior must be covered for self-host.

## Research Notes

- MudBlazor: use current v9 patterns such as `MudDrawer @bind-Open`, `<CustomContent>` for `MudFileUpload`, provider setup, and documented component state flows.
- Blazor forms: use visible labels, `EditForm`, validation summaries/messages, and predictable validation paths.
- Clean Architecture: presentation depends inward; Application coordinates use cases and contracts; Domain stays independent of presentation/infrastructure.
- Timezone: model local date, local start/end, and timezone ID; derive UTC on the server and validate ambiguous/invalid local times.
- HAL/RFC 8288: HAL should remain links/actions; field requirements and policy data belong in a companion context payload.
- RFC 9457/ASP.NET ProblemDetails: use stable machine-readable problem types and field paths for readiness/conflict responses.
- EF concurrency: repo uses concurrency stamps rather than SQL rowversion-only assumptions.
- OpenTelemetry/health checks: add spans/structured logs for draft/update/readiness/publish and health coverage for database/object storage where applicable.

## Quick Resume

Next action after the current implementation slices:

1. Add draft/update/readiness/publish Application/API contracts and decide the replacement point for the graph-shaped create request.
2. Move authoritative schedule composition out of Blazor and replace the graph-shaped create request path.
3. Add HAL-backed action affordances for the creation context and publish lifecycle.
4. Wire Blazor `Save draft`, `Review and publish`, and final `Publish event` to service-layer methods backed by HAL/server policy.
