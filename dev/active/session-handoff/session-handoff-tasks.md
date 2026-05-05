<!-- ABOUTME: Checklist for continuing Event Program Management after session handoff. -->
<!-- ABOUTME: Tracks remaining work by architecture layer with acceptance criteria. -->

# Session Handoff - Tasks

Last Updated: 2026-05-05

## Phase 0: Resume And Guardrails

- [x] Verify canonical docs exist (`CLAUDE.md`, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/SECURITY.md`, `docs/API.md`, `dev/active/README.md`).
- [x] Verify corrected session-group implementation files exist.
- [x] Verify rejected child-event hierarchy artifacts are removed from server source and generated contracts.
- [ ] Add automated regression test for rejected artifact absence.
  - **Acceptance Criteria**:
    - [ ] Test fails if `Event.ParentEventId` returns.
    - [ ] Test fails if rejected parent-candidate/subevents/program route names return.
    - [ ] Test is included in architecture or application unit test suite.
  - **Effort**: S
  - **Skills**: `clean-architecture-rules`

## Phase 1: Application Write Contracts

- [ ] Create session-group write request DTOs.
  - **Files**:
    - [ ] `Explore.Application/DTOs/EventSessionGroup/CreateEventSessionGroupRequest.cs`
    - [ ] `Explore.Application/DTOs/EventSessionGroup/UpdateEventSessionGroupRequest.cs`
    - [ ] `Explore.Application/DTOs/EventSessionGroup/AssignSessionToGroupRequest.cs`
  - **Acceptance Criteria**:
    - [ ] No client-controlled `TenantId`.
    - [ ] Assignment DTO includes `EventId`, `EventSessionGroupId`, `EventSessionId`, `IsPrimary`, `SortOrder`.
  - **Effort**: M
  - **Skills**: `cqrs-mediatr-guidelines`

- [ ] Add validators for create/update/assignment.
  - **Acceptance Criteria**:
    - [ ] Validators are manually instantiated in handlers.
    - [ ] Assignment validator enforces group/session/event consistency.
    - [ ] Room/location validation uses repositories and tenant filters.
  - **Effort**: M
  - **Skills**: `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`

- [ ] Add command requests and handlers.
  - **Acceptance Criteria**:
    - [ ] Create/update return project-standard command response.
    - [ ] Delete/unassign soft-delete rows rather than hard deleting at runtime.
    - [ ] Handlers pass cancellation tokens end-to-end.
  - **Effort**: L
  - **Skills**: `cqrs-mediatr-guidelines`, `clean-architecture-rules`

## Phase 2: Authorization And HAL

- [ ] Extend authorization catalogs for session-group/session-assignment writes.
  - **Files To Verify/Update**:
    - [ ] `Explore.Application/Authorization/AuthorizationActions.cs`
    - [ ] `Explore.Application/Authorization/ResourceKinds.cs`
    - [ ] `Explore.Application/Authorization/ResourceDescriptors.cs`
    - [ ] `cerbos/policies/`
  - **Acceptance Criteria**:
    - [ ] Writes are denied by default unless authorized.
    - [ ] Resource descriptors include event/session/group context needed by Cerbos/fallback provider.
  - **Effort**: M
  - **Skills**: `auth-patterns`

- [ ] Add HAL write links.
  - **Acceptance Criteria**:
    - [ ] Event detail exposes create session-group affordance only when authorized.
    - [ ] Group detail exposes edit/delete/reorder only when authorized.
    - [ ] Session detail exposes assign/unassign group only when authorized.
  - **Effort**: M
  - **Skills**: `auth-patterns`, `blazor-ui-conventions`

## Phase 3: API Write Endpoints

- [ ] Add write endpoints to `Explore.API/Controllers/EventSessionGroupController.cs`.
  - **Acceptance Criteria**:
    - [ ] POST create uses `[Authorize]`, named route, and `CreatedAtRoute`.
    - [ ] PUT update includes concurrency behavior if request supports it.
    - [ ] DELETE uses soft-delete behavior.
    - [ ] Assignment endpoints call Application handlers and include response metadata.
  - **Effort**: M
  - **Skills**: `auth-patterns`, `cqrs-mediatr-guidelines`

- [ ] Regenerate OpenAPI and client after API writes.
  - **Acceptance Criteria**:
    - [ ] `Explore.API/swagger.json` includes write operations.
    - [ ] `Explore.Blazor.Client/Clients/EventApiClient.g.cs` compiles.
    - [ ] Rejected child-event endpoints remain absent.
  - **Effort**: S
  - **Skills**: `blazor-ui-conventions`

## Phase 4: Tests

- [ ] Application unit tests for validators and handlers.
  - **Acceptance Criteria**:
    - [ ] Assignment rejects cross-event group/session pairs.
    - [ ] Assignment rejects tenant mismatch.
    - [ ] Create/update validate required names and max lengths.
  - **Effort**: M

- [ ] Persistence integration tests.
  - **Acceptance Criteria**:
    - [ ] Unique group slug per tenant/event enforced for non-deleted groups.
    - [ ] Unique membership enforced per tenant/event/group/session.
    - [ ] Only one primary group per event/session is allowed.
    - [ ] Soft-deleted join rows are hidden by filters.
  - **Effort**: M

- [ ] API/HAL tests.
  - **Acceptance Criteria**:
    - [ ] Public reads only return published groups.
    - [ ] HAL links include session groups and sessions routes.
    - [ ] Write links appear only when authorized.
  - **Effort**: M

## Phase 5: Blazor UX

- [ ] Add session-group service wrapper if project pattern requires it.
  - **Acceptance Criteria**:
    - [ ] UI does not call raw HTTP directly.
    - [ ] Service preserves HAL links.
  - **Effort**: M

- [ ] Add Program Summary and Add Session flow.
  - **Acceptance Criteria**:
    - [ ] Event composer shows read-only program summary from groups/sessions/agenda items.
    - [ ] Add session saves/updates event draft before navigating to dedicated session creation.
    - [ ] No giant nested session form appears in `CreateEvent.razor`.
  - **Effort**: XL

- [ ] Add session-group picker/section components.
  - **Acceptance Criteria**:
    - [ ] UI labels use Track/Devroom/Stage/Program section.
    - [ ] Components use MudBlazor v9 and CSS isolation/BEM conventions.
    - [ ] Actions are gated by HAL links.
  - **Effort**: L

## Phase 6: Documentation And Handoff Updates

- [ ] Update `docs/DOMAIN.md` with `EventSessionGroup` and `EventSessionGroupSession`.
- [ ] Update `docs/API.md` after write endpoints are added.
- [ ] Update `dev/active/event-creation-progressive-disclosure/` as implementation phases complete.
- [ ] Update this handoff context after each major step.

## Verification Checklist For Every Future Slice

- [ ] `lsp_diagnostics` on touched files/directories reports no errors.
- [ ] `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet` when generated client/UI changes.
- [ ] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] OpenAPI/client regenerated after API surface changes.
- [ ] Rejected child-event artifact grep remains clean.
