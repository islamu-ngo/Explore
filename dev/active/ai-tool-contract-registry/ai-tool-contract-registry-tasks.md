<!-- ABOUTME: Tactical checklist for the AI Tool Contract Registry workstream. -->
<!-- ABOUTME: Tracks registry, confirmation, API/HAL, Blazor, MCP, retention, and docs tasks. -->

# AI Tool Contract Registry — Task Checklist

Last Updated: 2026-06-01 Europe/Brussels

## Status Summary

- **Overall status:** Implementation started for the Application registry foundation.
- **Completed:** Current-state investigation, old AI workstream migration analysis, new plan/context/tasks creation, Phase 1.1 registry contracts, Phase 1.2 common JSON payload guard.
- **Current priority:** Phase 1.3 registry-backed prompt/parser wiring.
- **Next recommended slice:** Phase 1.3: registry-back prompt schema and parser while preserving current `CreateEventDraft` behavior.

## Implementation Maintenance Rules

- [x] Before starting work, read plan/context/tasks.
- [x] Re-read relevant intent docs/rules/skills for the slice being implemented.
- [x] After each completed task, update this checklist immediately.
- [ ] If implementation changes architecture/scope, update the plan before continuing.
- [x] If discoveries affect future work, update the context file.
- [x] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.
- [x] Do not claim “done” unless all three dev docs reflect actual state.

## Phase 0: Plan Review And Baseline ⏳ NOT STARTED

- [x] **0.1 User reviews and approves/corrects scope**
  - **Files:** `dev/active/ai-tool-contract-registry/*`.
  - **Acceptance:** Plan status becomes User-reviewed/Approved or corrections are incorporated.
  - **Validation:** Docs updated with review outcome.
  - **Effort:** S
  - **Dependencies:** none.

- [ ] **0.2 Archive or supersede old AI integration workstream**
  - **Files:** `dev/active/ai-integration/*`, `dev/pause/ai-integration/*` or equivalent pointer.
  - **Acceptance:** Old unfinished work points here; no old Phase 4-8 task is lost.
  - **Validation:** Grep old docs for next-step pointers.
  - **Effort:** S
  - **Dependencies:** 0.1.

- [x] **0.3 Confirm current repo state before first edit**
  - **Files:** context update only unless blockers found.
  - **Acceptance:** `git status --short` and baseline build/test blockers recorded; unrelated dirty work identified and not reverted.
  - **Validation:** `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` at minimum for Phase 1.
  - **Effort:** S
  - **Dependencies:** 0.1.

## Phase 1: Application Tool Contract Registry Foundation ⏳ NOT STARTED

- [x] **1.1 Define registry contracts**
  - **Files:** new `Explore.Application/Features/AiAssistant/Tools/AiToolDefinition.cs`, `AiToolContractRegistry.cs`, `IAiToolContractRegistry.cs`, `AiToolValidationResult.cs`, `AiToolExecutionContext.cs`.
  - **Acceptance:** Registry can expose tool definitions without API/Blazor/Persistence dependency; failure codes/messages are safe.
  - **Validation:** Application build and targeted unit tests.
  - **Effort:** M
  - **Dependencies:** 0.3.

- [x] **1.2 Add common JSON payload guard**
  - **Files:** new `AiToolPayloadGuard.cs`, `Event.Application.UnitTests/Features/AiAssistant/Tools/AiToolPayloadGuardTests.cs`.
  - **Acceptance:** Invalid JSON, arrays, unknown fields, and forbidden fields fail closed without raw content echo.
  - **Validation:** Targeted Application tests.
  - **Effort:** M
  - **Dependencies:** 1.1.

- [ ] **1.3 Registry-back prompt schema and parser**
  - **Files:** `AiSystemPromptFactory.cs`, `AiStructuredActionParser.cs`, `AiPromptContextBuilder.cs`, `SendAiMessageCommandHandler.cs`, prompt/parser tests.
  - **Acceptance:** Current provider-visible behavior remains `CreateEventDraft` only; no schema when tool proposals disabled; parser uses registry allow-list.
  - **Validation:** `AiPromptContextBuilderTests`, `AiStructuredActionParserTests`, Application build.
  - **Effort:** L
  - **Dependencies:** 1.1, 1.2.

## Phase 2: Migrate CreateEventDraft Into Registry ⏳ NOT STARTED

- [ ] **2.1 Register CreateEventDraft tool definition**
  - **Files:** new `CreateEventDraftAiToolDefinition.cs`; existing payload/mapper/schema files.
  - **Acceptance:** Tool definition provides schema, allowed fields, mapper, confirmation metadata, and required authorization metadata; existing mapper safety remains.
  - **Validation:** `CreateEventDraftAiActionMapperTests` pass.
  - **Effort:** M
  - **Dependencies:** 1.3.

- [ ] **2.2 Add schema/mapper drift tests**
  - **Files:** new or extended `CreateEventDraftAiToolDefinitionTests.cs`.
  - **Acceptance:** Schema field set and mapper allowed field set cannot silently drift.
  - **Validation:** Targeted Application tests.
  - **Effort:** S
  - **Dependencies:** 2.1.

## Phase 3: Proposed Action Confirmation Engine ⏳ NOT STARTED

- [ ] **3.1 Add confirm/reject commands and handlers**
  - **Files:** new `ConfirmAiProposedActionCommand.cs`, `RejectAiProposedActionCommand.cs`, handlers/tests.
  - **Acceptance:** Authenticated current user only; wrong tenant/user fails closed; duplicate confirm safe; reject has no side effects.
  - **Validation:** Application command tests.
  - **Effort:** L
  - **Dependencies:** 2.1.

- [ ] **3.2 Add CreateEventDraft executor**
  - **Files:** new executor; existing `CreateEventDraftAiActionMapper.cs`, `CreateEventCommand.cs`.
  - **Acceptance:** Executor dispatches `CreateEventCommand` through MediatR; no direct event repository insert; created event is draft with empty program graph.
  - **Validation:** Application tests and later DB-backed API flow.
  - **Effort:** L
  - **Dependencies:** 3.1.

- [ ] **3.3 Persist safe execution result metadata**
  - **Files:** `AiToolExecution` usage, repository update methods, tests; migration only if current schema is insufficient.
  - **Acceptance:** Success/failure execution metadata is queryable without raw provider/tool payload leakage.
  - **Validation:** Application/Persistence tests.
  - **Effort:** M
  - **Dependencies:** 3.2.

## Phase 4: API, HAL, OpenAPI, Contract Tests ⏳ NOT STARTED

- [ ] **4.1 Add confirm/reject API endpoints**
  - **Files:** `AiAssistantController.cs`, `AiAssistantProblemDetails.cs`, `RouteNames.cs`, API tests.
  - **Acceptance:** Authorized thin endpoints; confirm propagates `Idempotency-Key`; safe ProblemDetails.
  - **Validation:** `Event.API.IntegrationTests` targeted controller tests.
  - **Effort:** M
  - **Dependencies:** 3.1.

- [ ] **4.2 Add proposed-action HAL links**
  - **Files:** `AiAssistantLinkPolicy.cs`, `AiConversationResourceAssembler.cs`, `LinkRelations.cs`, HATEOAS tests.
  - **Acceptance:** Confirm/reject links appear only for allowed proposed actions; absent for unauthorized/stale/executed/rejected actions.
  - **Validation:** AI HATEOAS tests.
  - **Effort:** M
  - **Dependencies:** 4.1.

- [ ] **4.3 Add DB-backed create-draft confirmation flow tests**
  - **Files:** new `AiAssistantCreateEventDraftFlowTests.cs` or equivalent.
  - **Acceptance:** Fake provider proposes draft; send creates no event; confirm creates exactly one draft; reject creates none; duplicate confirm creates one event.
  - **Validation:** API/Application tests.
  - **Effort:** L
  - **Dependencies:** 4.1, 4.2.

- [ ] **4.4 Regenerate OpenAPI/client/changelog**
  - **Files:** `schemas/openapi.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API_CHANGELOG.md`, `docs/API.md` if needed.
  - **Acceptance:** Generated client includes confirm/reject methods; changelog documents auth/idempotency/HAL/ProblemDetails.
  - **Validation:** API build and Blazor Client build.
  - **Effort:** M
  - **Dependencies:** 4.1, 4.2.

## Phase 5: Event Reference Search And Prompt Packing ⏳ NOT STARTED

- [ ] **5.1 Add reference DTO/query contracts**
  - **Files:** new `AiReferenceSearchResultDto.cs`, `AiSelectedReferenceDto.cs`, `SearchAiReferencesQuery.cs`.
  - **Acceptance:** Lightweight event-first reference shape; no full event content.
  - **Validation:** Application build/tests.
  - **Effort:** M
  - **Dependencies:** 1.3.

- [ ] **5.2 Add event reference repository/query path**
  - **Files:** event repository/specification files; persistence tests.
  - **Acceptance:** Repository returns entities; bounded deterministic search; tenant filters preserved; no unsafe `IgnoreQueryFilters()`.
  - **Validation:** Persistence integration tests.
  - **Effort:** M
  - **Dependencies:** 5.1.

- [ ] **5.3 Add reference API/HAL and prompt packer**
  - **Files:** AI reference handler/API endpoint/HAL policy, `AiReferencePromptPacker.cs`, tests.
  - **Acceptance:** Authorized results with links when allowed; per-reference/total prompt budgets; safe quoted boundaries.
  - **Validation:** API and Application tests.
  - **Effort:** L
  - **Dependencies:** 5.2.

## Phase 6: Blazor Product Assistant UX ⏳ NOT STARTED

- [ ] **6.1 Add Blazor AI client service**
  - **Files:** new `IAiAssistantClientService.cs`, `AiAssistantClientService.cs`, service registration/tests.
  - **Acceptance:** Components use service, not ad-hoc raw HTTP; service handles bootstrap/history/send/reference/confirm/reject and idempotency keys.
  - **Validation:** Blazor service tests.
  - **Effort:** M
  - **Dependencies:** 4.4.

- [ ] **6.2 Extend AI state for conversation UI**
  - **Files:** `AiAssistantState.cs` and/or `AiAssistantConversationState.cs`.
  - **Acceptance:** Tracks selected conversation/model/references/loading/errors; existing open/availability tests pass; no local authz decisions.
  - **Validation:** Blazor state tests.
  - **Effort:** M
  - **Dependencies:** 6.1.

- [ ] **6.3 Replace rail placeholder with assistant layout**
  - **Files:** `AiAssistantRail.razor*`, `AiAssistantHeader.razor`, `AiConversationList.razor`, `AiMessageList.razor`, `AiPromptComposer.razor`.
  - **Acceptance:** MudBlazor UI, BEM-like CSS isolation, keyboard/ARIA support, docked/fixed behavior preserved, loading/error/empty/disabled states covered.
  - **Validation:** bUnit tests and manual smoke.
  - **Effort:** L
  - **Dependencies:** 6.1, 6.2.

- [ ] **6.4 Add event reference picker UI**
  - **Files:** `AiReferencePicker.razor`, `AiReferenceChip.razor`, CSS/tests.
  - **Acceptance:** Debounced search, select/remove chips, loading/empty/error states, keyboard-removable chips.
  - **Validation:** bUnit tests.
  - **Effort:** M
  - **Dependencies:** Phase 5, 6.1.

- [ ] **6.5 Add proposed action/result cards**
  - **Files:** `AiProposedActionCard.razor`, `CreateEventDraftActionPreview.razor`, `AiActionResultCard.razor`.
  - **Acceptance:** Confirm/Reject only render when HAL links exist; double-submit prevented; result links use API links; proposal distinct from committed data.
  - **Validation:** bUnit HAL-gating tests.
  - **Effort:** L
  - **Dependencies:** Phase 4, 6.1, 6.2.

- [ ] **6.6 Add full panel bUnit tests**
  - **Files:** `Explore.Blazor.Client.Tests/Components/AiAssistant/*`, existing layout tests as needed.
  - **Acceptance:** Bootstrap, history, model selection, references, send, action cards, confirm/reject, disabled/error states; existing dock bridge tests pass.
  - **Validation:** `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** L
  - **Dependencies:** 6.1-6.5.

## Phase 7: Optional ISLAMU Event MCP Adapter ⏳ NOT STARTED

- [ ] **7.1 Research and select MCP hosting strategy**
  - **Files:** plan/context update, optional ADR/docs.
  - **Acceptance:** Decision covers .NET MCP library/transport, API vs separate host, auth, tenancy, config, health, self-hosting impact, and disable path.
  - **Validation:** Source-backed decision recorded.
  - **Effort:** M
  - **Dependencies:** Phase 1.

- [ ] **7.2 Implement MCP adapter over registry**
  - **Files:** TBD after 7.1.
  - **Acceptance:** MCP tools/resources/prompts are registry-backed; mutating tools default to proposal/confirmation path; no direct repository mutation.
  - **Validation:** MCP conformance, authz, tenant isolation, rate/audit tests.
  - **Effort:** XL
  - **Dependencies:** 7.1, Phase 3.

- [ ] **7.3 Document MCP self-hosting and operations**
  - **Files:** `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, Aspire/Docker docs if touched.
  - **Acceptance:** Self-hosters know required/optional services, env vars, secrets, health, recovery, and disable behavior.
  - **Validation:** Docs review/docs lint where available.
  - **Effort:** M
  - **Dependencies:** 7.2.

## Phase 8: Retention, Redaction, Streaming/Cancellation, Advanced Ops, Final Docs ⏳ NOT STARTED

- [ ] **8.1 Add retention cleanup/redaction jobs**
  - **Files:** cleanup handler/job files TBD, docs/tests.
  - **Acceptance:** Enforces `ai_assistant.retention_days`; tenant-scoped; observable; no prompt/content logs.
  - **Validation:** Application/Persistence/API tests.
  - **Effort:** L
  - **Dependencies:** Phase 3.

- [ ] **8.2 Add cancellation semantics**
  - **Files:** API/Application/Blazor provider files TBD.
  - **Acceptance:** Endpoint/API/UI cancellation is added if implemented; cancel link appears for cancellable runs only; provider cancellation token honored; cancelled runs produce no actions.
  - **Validation:** Application/API/Blazor tests.
  - **Effort:** M
  - **Dependencies:** Phase 3.

- [ ] **8.3 Decide and implement streaming or polling**
  - **Files:** TBD after decision.
  - **Acceptance:** Decision documented; auth/tenant isolation preserved; non-streaming fallback remains.
  - **Validation:** API/Blazor tests + manual smoke if implemented.
  - **Effort:** M-XL
  - **Dependencies:** Core flow complete.

- [ ] **8.4 Add advanced provider/run dashboards and runbook polish**
  - **Files:** metrics dashboards/runbook docs, `docs/OPERATIONS.md`, troubleshooting docs if present.
  - **Acceptance:** Builds on provider health/metrics/logging; no secrets/content in logs; dashboards use low-cardinality dimensions; runbooks cover disabled/misconfigured/unavailable/rate-limited/failed-confirmation/stuck-action/MCP states.
  - **Validation:** Infrastructure/API tests or docs checks as applicable.
  - **Effort:** M
  - **Dependencies:** Provider health/telemetry and confirmation engine.

- [ ] **8.5 Final docs, credit, runbooks, and validation refresh**
  - **Files:** `docs/API.md`, `docs/API_CHANGELOG.md`, `schemas/openapi.json`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, `docs/BLAZOR.md`, `docs/DOCK_LAYOUT.md`, `docs/ACCESSIBILITY.md` if needed, `README.md` if Plane inspiration credit applies, dev docs.
  - **Acceptance:** AI endpoints/auth/idempotency/HAL/ProblemDetails, provider setup, secrets, model allow-list, limits, retention, health, disable behavior, troubleshooting, dock behavior, keyboard/focus/accessibility, HAL-gated action workflow, and Plane inspiration credit are documented where applicable. Old AI workstream archived and final validation recorded.
  - **Validation:** Project-level builds/tests plus docs/context checks.
  - **Effort:** L
  - **Dependencies:** Implemented phases.

## Verification Checklist

- [ ] LSP diagnostics clean for modified files.
- [ ] `dotnet build --configuration Release --verbosity quiet` passes before final closure or failures are documented as unrelated/pre-existing.
- [ ] `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passes when Domain lifecycle changes are included.
- [ ] `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passes for Application slices.
- [ ] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` targeted tests pass for registry/action slices.
- [ ] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passes for provider/MCP infrastructure slices.
- [ ] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` targeted tests pass for API/HAL slices.
- [ ] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` targeted tests pass for persistence/reference/retention slices.
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passes for Blazor slices.
- [ ] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` passes when UI flow changes require it.
- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` attempted before PR; unrelated pre-existing failures recorded separately if present.
- [ ] OpenAPI/client/changelog regenerated when API changes land.
- [ ] HAL link tests prove UI affordance gating.
- [ ] AI authorization/resource parity tests pass for local and Cerbos modes when affected.
- [ ] Send-message and confirm idempotency tests pass.
- [ ] Provider endpoint validation, safe health, and no-content logging tests pass when provider/MCP slices are affected.
- [ ] MVP quota/prompt/reference/model/tool limits and retention/redaction gate tests pass.
- [ ] No real AI provider calls are required for tests.
- [ ] Docs updated where behavior/config/operations/API changed.
- [ ] Dev docs refreshed with final state and remaining work before handoff.

## Remaining / Deferred Work

- Old `dev/active/ai-integration` archive is deferred until user approves this new workstream.
- Direct MCP mutation without human confirmation is deferred and requires explicit policy approval.
- Additional tools beyond `CreateEventDraft` are deferred until registry proves schema/mapper/executor parity with one tool.
- Streaming/cancellation remains deferred until non-streaming confirmable MVP is stable.
- Federation-aware AI references are deferred; first release should use local tenant events only.
