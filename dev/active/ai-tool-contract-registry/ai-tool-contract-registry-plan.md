<!-- ABOUTME: Strategic implementation plan for the AI Tool Contract Registry workstream. -->
<!-- ABOUTME: Reframes remaining AI assistant and future MCP work around one governed tool contract boundary. -->

# AI Tool Contract Registry — Implementation Plan

Last Updated: 2026-06-01 Europe/Brussels

## 0. Planning Metadata

- **Request:** Replace the old AI integration implementation plan with a long-term internal AI Tool Contract Registry plan that makes MCP another adapter over the same governed capabilities.
- **Task directory:** `dev/active/ai-tool-contract-registry/`
- **Planning status:** Draft, awaiting user review.
- **Matched intents:** `add-cqrs-handler`, `openapi-contract-change`, `blazor-component-affordance`, `update-repository-query`, `add-ef-migration`, `cerbos-policy-change` as later phases touch Application, API/HAL/OpenAPI, Blazor, Persistence, and authorization policy.
- **Relevant skills:** `senior-cto-feedback`, `cto-consultation`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, `dotnet-efcore-guidelines`, `error-tracking`.
- **Relevant rules:** `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/OPERATIONS.md`, `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/blazor-client.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/efcore-migrations.md`, `.claude/rules/tests.md`.
- **Primary layers touched:** Domain, Application, Persistence, Infrastructure, API, Blazor, Docs, DevOps.
- **Estimated complexity:** XL. This is a cross-layer architecture pivot for AI actions, confirmation, HAL/API contracts, Blazor UX, optional MCP transport, security, tenancy, audit, and self-hosting documentation.

## 1. Executive Summary

The current AI implementation successfully built provider-safe chat, persistence, authenticated API foundations, and the first safe `CreateEventDraft` mapper. The next phase should not continue by adding one bespoke `ActionPayload` and `ActionMapper` pair per tool forever. That would duplicate schema, validation, mapping, confirmation, HAL, audit, and future MCP exposure logic.

This workstream creates an internal AI Tool Contract Registry as the canonical platform boundary for AI and future MCP tools. The registry will own tool metadata, JSON schema, input validation, mapping, authorization/affordance requirements, confirmation mode, execution routing, and audit behavior. The in-product AI assistant and the future MCP server become adapters over the same registry rather than parallel implementations.

Explicitly out of scope for the first registry slice: direct automatic mutation from model output, direct MCP mutation bypassing confirmation, broad Blazor chat enablement without HAL-gated action cards, replacing existing CQRS commands, or creating a second event persistence path.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---|---|
| AI conversations, runs, proposed actions, and tool execution audit entities already exist. | Verified: `Explore.Domain/Ai/AiToolExecution.cs`; old workstream context lists `Explore.Domain/Ai/*`. | High | Registry should reuse this persistence/audit foundation instead of inventing separate MCP records first. |
| Provider-facing tool schema is currently a string attached to chat requests. | Verified: `Explore.Application/Contracts/Infrastructure/Ai/AiChatModels.cs::AiStructuredActionSchema`. | High | Registry can become the source that emits provider schemas. |
| Parser is hardcoded to `CreateEventDraft`. | Verified: `Explore.Application/Features/AiAssistant/Prompting/AiStructuredActionParser.cs` has `AllowedKinds = [AiProposedActionKind.CreateEventDraft]`. | High | This is the seam to replace with registry-backed allow-list and validation. |
| Prompt factory is hardcoded to `CreateEventDraftSchema`. | Verified: `Explore.Application/Features/AiAssistant/Prompting/AiSystemPromptFactory.cs`. | High | Registry should generate schemas from registered tool definitions. |
| `CreateEventDraft` has a safe strict mapper today. | Verified: `Explore.Application/Features/AiAssistant/Actions/CreateEventDraftAiActionMapper.cs`. | High | Keep behavior, migrate behind registry. |
| Unknown and privileged event draft fields are rejected today. | Verified: `CreateEventDraftAiActionMapper` `AllowedJsonPropertyNames` and failure `unsupported_payload_field`. | High | This policy must become reusable default registry behavior. |
| Organization/group scope for event draft proposals is allow-list gated. | Verified: `CreateEventDraftAiActionMappingContext` checks allowed org/group ID sets. | High | Registry execution context needs user/tenant/resource affordance data. |
| Confirm/reject is not implemented. | Verified by old tasks: Phase 5.2-5.4 unchecked; search found no `ConfirmAiProposedAction*` or `RejectAiProposedAction*`. | High | Next mutation work must start here after registry foundation. |
| MCP is not implemented. | Verified: `glob **/*Mcp*.*` found no code files. | High | MCP must be planned as a new optional adapter. |
| MCP exists only as roadmap intent today. | Verified: `docs/semantic_versioning/v1.0.0.md` Phase E mentions MCP server host/tools/auth/rate/audit tests. | High | This plan should align with roadmap but not pretend code exists. |
| Old AI integration still has unfinished reference search, Blazor UI, retention, streaming/cancellation, and docs tasks. | Verified: `dev/active/ai-integration/ai-integration-tasks.md` Phase 4-8. | High | Carry forward under registry-centered sequencing. |
| Broad AI UI enablement is blocked by HAL product-surface gating and retention posture. | Verified: `dev/active/ai-integration/ai-integration-context.md`. | High | Do not jump to UI before confirm/reject/HAL and retention decisions. |
| AI schema migration is not cleanly AI-scoped. | Verified: old context says AI tables are in `20260529173418_domainupdate.cs`; `20260531165911_domainupdate4.cs` is empty snapshot refresh. | Medium | Do not claim clean migration history; plan must include self-hoster notes. |

### 2.2 Existing Implementation

By layer:

- **Domain:** AI aggregate concepts exist under `Explore.Domain/Ai/*`, including `AiToolExecution` for execution metadata. Proposed action lifecycle is already modeled in old workstream context as proposed/confirmed/rejected/executed/failed.
- **Application:** Provider-neutral chat contracts exist under `Explore.Application/Contracts/Infrastructure/Ai/*`. `AiStructuredActionParser`, `AiSystemPromptFactory`, `AiPromptContextBuilder`, `SendAiMessageCommandHandler`, DTOs, and AI CQRS handlers already support provider proposals as persisted non-mutating actions. `CreateEventDraftAiActionMapper` is the only strict typed action mapper today.
- **Persistence:** AI conversation repository and EF mappings exist. Repository returns entities and enforces tenant filters according to old verified workstream context.
- **Infrastructure:** Runtime provider selection, fake provider, OpenAI-compatible raw HTTP adapter, health checks, and safe telemetry already exist.
- **API/HAL:** `AiAssistantController`, `AiAssistantProblemDetails`, `AiAssistantLinkPolicy`, and `AiConversationResourceAssembler` expose authenticated bootstrap/history/send/run-status and conservative conversation links. Confirm/reject links do not exist yet.
- **Blazor:** The shell dock and AI rail placeholder exist. Functional chat, reference picker, and proposal cards remain planned.
- **MCP:** No product MCP code exists today. Only semantic-versioning docs mention future MCP support.

### 2.3 Existing Tests And Verification Coverage

- `Event.Application.UnitTests` covers AI settings, bootstrap, conversation handlers, send orchestration, prompt packing, structured action parser, and `CreateEventDraftAiActionMapperTests`.
- `Explore.Infrastructure.Tests` covers provider settings, fake provider, OpenAI-compatible adapter, runtime selector, and health reporter/check behavior.
- `Event.Persistence.IntegrationTests` covers `AiConversationRepositoryTests` against PostgreSQL.
- `Event.API.IntegrationTests` covers AI controller/HAL/API flows, including host-backed and DB-backed fake-provider flows.
- Old Phase 5.1 verification: LSP clean; Application build passed; Application UnitTests build passed; `CreateEventDraftAiActionMapperTests` passed 16/16; `AiPromptContextBuilderTests` passed 4/4.
- Known gap: Architecture authorization parity has unrelated non-AI HATEOAS metadata failures. Do not treat those as AI registry failures, but do not hide them.

### 2.4 Existing Documentation And Contracts

- `dev/active/ai-integration/*` is the old active workstream and should be archived after this plan is reviewed.
- `docs/CONFIGURATION.md` and `docs/OPERATIONS.md` document current AI provider, egress, governance, and telemetry behavior.
- `docs/API_CHANGELOG.md`, `schemas/openapi.json`, and `Explore.Blazor.Client/Clients/EventApiClient.g.cs` already include current AI API foundation.
- `docs/semantic_versioning/v1.0.0.md` lists future MCP server support.
- `docs/SECURITY-MODEL.md`, `docs/MULTI_TENANCY.md`, and `docs/API.md` remain authoritative for auth, tenant, and contract behavior.
- `dev/active/ai-integration/ai-integration-plane-report.md` exists as a Plane inspiration analysis/reference artifact. It is not implementation status, but final documentation must preserve appropriate AGPL-compatible inspiration credit if the UI materially uses those ideas.

### 2.5 Current Pain Points / Improvement Areas

- **Hardcoded tool schema path:** `AiSystemPromptFactory` owns one string schema. This does not scale to multiple tools or MCP.
- **Hardcoded tool parser allow-list:** `AiStructuredActionParser` owns one static `HashSet`. Registry should own allowed tools per tenant/provider/context.
- **Mapper duplication risk:** `CreateEventDraftAiActionMapper` correctly validates one tool, but adding many sibling mappers without common policy will duplicate unknown-field, privileged-field, failure-code, schema, and scope checks.
- **No confirmation engine:** proposed actions are persisted, but there is no generic confirm/reject command path, executor contract, idempotency handling, or result-link persistence.
- **No HAL action affordance for proposals:** UI cannot safely render Confirm/Reject buttons yet because confirm/reject HAL links do not exist.
- **No MCP adapter:** MCP should not be implemented by copying AI tool logic into a second surface.
- **Retention posture incomplete:** broad history exposure and UI enablement still need cleanup/redaction jobs around `ai_assistant.retention_days`.
- **Old plan is no longer the right source of truth:** it was an AI integration plan, not a reusable tool-governance and adapter architecture plan.

### 2.6 Unknowns After Investigation

- Exact MCP hosting package/protocol implementation remains undecided. Implementation must research current .NET MCP server options and choose an optional adapter that does not raise the core self-hosting floor.
- Whether MCP should run inside `Explore.API`, a separate Aspire resource, or an optional host is undecided. The first MCP phase must decide with operator impact evidence.
- How much direct machine-to-machine MCP mutation is acceptable remains a policy decision. Default is proposal-first; direct mutation requires explicit trusted-client policy, idempotency, audit, tenant binding, and user/admin approval.
- Whether event reference search should precede confirm/reject depends on desired first UI slice. Current recommendation: registry foundation and confirm/reject first, reference search before polished Blazor UX.
- Whether existing AI migration history needs cleanup is uncertain. Since the project is pre-v1, a later migration reset may be acceptable, but it must be documented for self-hosters.

## 3. Proposed Future State

Target architecture:

```text
Provider / In-Product AI
  -> AiPromptContextBuilder
  -> AiToolContractRegistry emits provider action schema
  -> AI provider returns proposed tool call JSON
  -> AiStructuredActionParser asks registry to validate kind + raw payload boundary
  -> Proposed action persisted
  -> API/HAL exposes confirm/reject affordances when allowed
  -> User confirms
  -> AiToolConfirmationHandler asks registry for executor
  -> Executor dispatches existing MediatR command/query
  -> Domain/Application/Persistence/API policies remain authoritative

External MCP Client
  -> Optional ISLAMU Event MCP adapter
  -> Authenticated tenant-bound tool request
  -> Same AiToolContractRegistry
  -> Default path creates/returns proposed action or invokes same confirmation engine
  -> Existing CQRS command/query remains the mutation/read source of truth
```

The registry will define each tool once:

- stable tool name/kind;
- JSON schema;
- unknown/privileged field policy;
- input validator;
- typed mapper;
- confirmation mode: proposal-only, confirm-required, read-only, or trusted-direct only by explicit policy;
- required authorization action/resource kind;
- HAL relation names;
- executor that calls existing MediatR requests;
- audit and safe failure-code behavior;
- provider/MCP exposure metadata.

## 4. Non-Negotiable Constraints

- Repositories return entities, never DTOs.
- Validators are manually instantiated; do not rely on DI for validators.
- Use `Guid` for aggregate IDs, `int` for lookup IDs, and `long` for cursors/message sequence.
- GET endpoints are usually `[AllowAnonymous]`, but private AI/MCP tenant/user surfaces must be authenticated and authorized.
- Write endpoints are `[Authorize]` and must enforce MediatR authorization/resource behavior.
- HAL links are the single source of truth for Blazor action affordances. No local role checks for Confirm/Reject buttons.
- Browser never sees provider credentials, MCP credentials, bearer tokens, provider endpoints, raw provider request IDs, prompts, or raw provider errors.
- AI/MCP mutating tools must resolve to existing Application commands, not direct repositories or controller logic.
- Model output is untrusted. Unknown, privileged, or out-of-scope tool fields fail closed.
- Tenant isolation is API-authoritative and persistence must preserve EF tenant filters.
- No external HTTP/provider/tool call inside a DB transaction.
- No compatibility shim unless the user explicitly approves it. The repo is pre-v1, so cleaner contracts are preferred.
- Every new C# file must begin with two `ABOUTME:` comments and use file-scoped namespaces.

## 5. Architecture And Design Decisions

### Decision 1: Registry Is The Core Boundary

- **Decision:** Build an Application-layer `AiToolContractRegistry` before adding more concrete AI tools.
- **Why:** It prevents schema/parser/mapper/executor duplication and gives MCP a shared contract surface.
- **Alternatives considered:** Continue one mapper per action; build MCP first; use provider SDK function-calling as source of truth.
- **Consequences:** Early refactor work is required, but later tools become cheaper and safer.
- **Files/layers affected:** Application prompting/actions/contracts/tests first; API/Blazor/MCP later.

### Decision 2: MCP Is An Adapter, Not Authority

- **Decision:** MCP will sit over the registry/API/Application boundary and cannot bypass authorization, tenant isolation, confirmation defaults, or audit.
- **Why:** MCP is transport/protocol. It is not the product authorization model.
- **Alternatives considered:** Let MCP call repositories or execute provider tools directly.
- **Consequences:** MCP implementation may be slightly slower, but remains enterprise-safe and self-hostable.
- **Files/layers affected:** Future optional MCP host/API adapter/Infrastructure registration/docs.

### Decision 3: Mutating Tools Confirm By Default

- **Decision:** Mutating tools produce proposed actions first and require human confirmation unless an explicit trusted machine-to-machine policy is later approved.
- **Why:** Current product assistant runs in user context and model output is untrusted.
- **Alternatives considered:** Automatic tool invocation from provider tool calls.
- **Consequences:** UI and API need Confirm/Reject flows before broad product enablement.
- **Files/layers affected:** Application confirm/reject commands, API/HAL, Blazor action cards, MCP policy.

### Decision 4: Existing CQRS Commands Stay Authoritative

- **Decision:** Tool executors dispatch existing MediatR commands/queries such as `CreateEventCommand`.
- **Why:** Existing handlers own validation, authorization behavior, actor resolution, transactions, metrics, cache invalidation, and future outbox hooks.
- **Alternatives considered:** Tool executors write repositories directly.
- **Consequences:** Some mapping contexts are needed, but business logic stays centralized.
- **Files/layers affected:** Application registry/executors; no direct Persistence dependency from Application executors.

### Decision 5: One Schema Source For AI And MCP

- **Decision:** Tool JSON schemas are emitted from registry definitions for provider tool calls and MCP tool definitions.
- **Why:** Prevents drift between product AI, API docs, and external agent contracts.
- **Alternatives considered:** Separate AI schema strings and MCP schema definitions.
- **Consequences:** Registry schema tests become important quality gates.
- **Files/layers affected:** `AiSystemPromptFactory`, `AiStructuredActionParser`, future MCP adapter.

## 6. Implementation Phases

### Phase 0: Plan Review, Baseline, And Old Workstream Archive

- **Goal:** Make this workstream the source of truth and archive old AI integration after approval.
- **Depends on:** User review.
- **Relevant files:** `dev/active/ai-integration/*`, `dev/active/ai-tool-contract-registry/*`, `dev/pause/` archive path.
- **Related skills/rules:** `senior-cto-feedback`, `clean-architecture-rules`.
- **Acceptance criteria:** User approves or corrects this plan; old AI unfinished tasks are represented here; old plan is moved to pause/archive only after approval.
- **Verification:** Docs review, no build required for planning-only archive step.
- **Rollback / failure handling:** If user rejects the plan, keep `dev/active/ai-integration` active and update this draft with corrections.

#### Task 0.1: User Review And Scope Correction

- **Type:** docs
- **Layer:** Docs
- **Files:** `dev/active/ai-tool-contract-registry/*` existing after this plan.
- **Description:** User reviews architecture direction, especially MCP-as-adapter and confirm-by-default policy.
- **Acceptance Criteria:** Plan status is updated to User-reviewed or corrections are incorporated.
- **Dependencies:** none.
- **Effort:** S
- **Validation:** Docs updated with review outcome.

#### Task 0.2: Archive Old AI Integration Workstream

- **Type:** docs
- **Layer:** Docs
- **Files:** `dev/active/ai-integration/*` existing; `dev/pause/ai-integration/*` new or equivalent archive location.
- **Description:** Move or mark the old AI integration workstream as superseded after user approval. Preserve history; do not delete evidence.
- **Acceptance Criteria:** Old workstream clearly points to this new plan; no unfinished old task is lost.
- **Dependencies:** 0.1.
- **Effort:** S
- **Validation:** Grep for old “next recommended slice” confirms pointer to this workstream.

### Phase 1: Application Tool Contract Registry Foundation

- **Goal:** Introduce the reusable Application-layer registry and shared validation model without changing runtime behavior.
- **Depends on:** Phase 0 approval.
- **Relevant files:** new `Explore.Application/Features/AiAssistant/Tools/*`; existing `AiStructuredActionParser.cs`, `AiSystemPromptFactory.cs`, `AiPromptContextBuilder.cs`, `AiChatModels.cs`; tests under `Event.Application.UnitTests/Features/AiAssistant/Tools/*`.
- **Related skills/rules:** `cqrs-mediatr-guidelines`, `clean-architecture-rules`, `.claude/rules/application-layer.md`.
- **Acceptance criteria:** Registry can list enabled tool definitions, emit provider schema, validate raw payload object shape, reject unknown/privileged fields through common policy, and return stable safe failure codes.
- **Verification:** `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false`; targeted Application unit tests.
- **Rollback / failure handling:** Revert registry wiring before changing `SendAiMessageCommandHandler` if behavior diverges.

#### Task 1.1: Define Registry Contracts

- **Type:** create
- **Layer:** Application
- **Files:** new `Explore.Application/Features/AiAssistant/Tools/AiToolDefinition.cs`, `AiToolContractRegistry.cs`, `IAiToolContractRegistry.cs`, `AiToolValidationResult.cs`, `AiToolExecutionContext.cs`.
- **Description:** Create typed contracts for tool kind/name, JSON schema, confirmation mode, validation, mapping, executor metadata, required auth resource/action, and exposure flags for provider/MCP.
- **Acceptance Criteria:** Contracts reference Application/Domain types only; no API/Blazor/Persistence dependency; failure codes are stable and safe.
- **Dependencies:** 0.1.
- **Effort:** M
- **Validation:** Application build and architecture tests where available.

#### Task 1.2: Add Common JSON Boundary Validation

- **Type:** create/test
- **Layer:** Application
- **Files:** new `AiToolPayloadGuard.cs`; tests `AiToolPayloadGuardTests.cs`.
- **Description:** Extract reusable JSON object parsing, allowed-field validation, privileged-field rejection, and safe error messages.
- **Acceptance Criteria:** Invalid JSON, arrays, unknown fields, and forbidden fields fail closed without echoing raw model content.
- **Dependencies:** 1.1.
- **Effort:** M
- **Validation:** Targeted Application tests.

#### Task 1.3: Registry-Back Prompt Schema And Parser

- **Type:** modify/test
- **Layer:** Application
- **Files:** existing `AiSystemPromptFactory.cs`, `AiStructuredActionParser.cs`, `AiPromptContextBuilder.cs`, `SendAiMessageCommandHandler.cs`; tests under prompt/parser suites.
- **Description:** Replace hardcoded `CreateEventDraftSchema` and parser allow-list with registry-backed schema and kind validation while preserving current external behavior.
- **Acceptance Criteria:** Existing prompt/parser tests still pass; schema still exposes only `CreateEventDraft`; disabled `ToolProposalsEnabled` emits no schema.
- **Dependencies:** 1.1, 1.2.
- **Effort:** L
- **Validation:** `AiPromptContextBuilderTests`, `AiStructuredActionParserTests`, full Application build.

### Phase 2: Migrate CreateEventDraft Into The Registry

- **Goal:** Keep Phase 5.1 safety behavior while making `CreateEventDraft` the first registered tool.
- **Depends on:** Phase 1.
- **Relevant files:** existing `CreateEventDraftAiActionPayload.cs`, `CreateEventDraftAiActionMapper.cs`, `AiSystemPromptFactory.cs`; new `CreateEventDraftAiToolDefinition.cs`.
- **Acceptance criteria:** All current mapper tests pass; schema and mapper field sets cannot drift; org/group allow-list behavior remains explicit.
- **Verification:** `CreateEventDraftAiActionMapperTests` and schema tests.
- **Rollback / failure handling:** Keep old mapper intact and only remove hardcoded prompt/schema after parity tests pass.

#### Task 2.1: Register CreateEventDraft Tool Definition

- **Type:** create/modify/test
- **Layer:** Application
- **Files:** new `CreateEventDraftAiToolDefinition.cs`; modify registry composition.
- **Description:** Wrap existing payload/mapper/schema metadata as a registry tool definition.
- **Acceptance Criteria:** One source defines allowed JSON property names and schema field names; privileged fields remain excluded.
- **Dependencies:** 1.1.
- **Effort:** M
- **Validation:** Mapper/schema parity tests.

#### Task 2.2: Add Schema/Mapper Drift Tests

- **Type:** test
- **Layer:** Application Tests
- **Files:** new or extended `CreateEventDraftAiToolDefinitionTests.cs`.
- **Description:** Prove every schema field is accepted by the mapper and every mapper field appears in schema, excluding internal mapping context.
- **Acceptance Criteria:** No silent schema/mapper drift.
- **Dependencies:** 2.1.
- **Effort:** S
- **Validation:** Targeted Application tests.

### Phase 3: Proposed Action Confirmation Engine

- **Goal:** Add generic confirm/reject commands and the first executor path, dispatching `CreateEventCommand` for event draft creation.
- **Depends on:** Phase 2.
- **Relevant files:** new `ConfirmAiProposedActionCommand.cs`, `RejectAiProposedActionCommand.cs`, handlers, tool executor contracts; existing `IAiConversationRepository`, `CreateEventCommand`, `CreateEventCommandHandler`, `AiToolExecution`.
- **Acceptance criteria:** Confirm is idempotent/duplicate-safe; reject has no event side effect; execution failures are safely persisted; event creation uses MediatR `CreateEventCommand`; no direct event repository insert.
- **Verification:** Application unit tests; targeted build.
- **Rollback / failure handling:** If executor fails, proposed action remains failed with safe code; no partial event creation outside existing transaction path.

#### Task 3.1: Add Confirm/Reject Commands And Handlers

- **Type:** create/test
- **Layer:** Application
- **Files:** new command/request/handler files under `Explore.Application/Features/AiAssistant`.
- **Description:** Load proposed action for update, enforce ownership/tenant/state, apply idempotency key, and update proposed action status.
- **Acceptance Criteria:** Confirm/reject require authenticated current user; wrong user/tenant fails closed; stale state returns safe failure.
- **Dependencies:** 2.1.
- **Effort:** L
- **Validation:** `Event.Application.UnitTests` targeted command tests.

#### Task 3.2: Add CreateEventDraft Executor

- **Type:** create/test
- **Layer:** Application
- **Files:** new executor; existing `CreateEventDraftAiActionMapper.cs`, `CreateEventCommand.cs`.
- **Description:** Rehydrate payload, validate org/group context, call existing `CreateEventCommand` through MediatR, and persist result metadata.
- **Acceptance Criteria:** Created event is draft status through existing DTO/command path; no session/day/room/agenda graph creation; no event repository direct insert.
- **Dependencies:** 3.1.
- **Effort:** L
- **Validation:** Application tests with fake mediator/repository; later API DB-backed flow.

### Phase 4: API, HAL, OpenAPI, And Contract Tests

- **Goal:** Expose confirm/reject safely over API and HAL so Blazor can render proposal actions without local role checks.
- **Depends on:** Phase 3.
- **Relevant files:** `Explore.API/Controllers/AiAssistantController.cs`, `AiAssistantProblemDetails.cs`, `RouteNames.cs`, `AiAssistantLinkPolicy.cs`, `AiConversationResourceAssembler.cs`, `schemas/openapi.json`, generated client, `docs/API_CHANGELOG.md`.
- **Acceptance criteria:** Authorized endpoints; stable route names/operation IDs; confirm accepts `Idempotency-Key`; HAL links appear only when action state and permissions allow; ProblemDetails are safe.
- **Verification:** API integration tests, HATEOAS tests, OpenAPI/client generation, API changelog.
- **Rollback / failure handling:** Do not regenerate client until API tests and route names are stable.

#### Task 4.1: Add Confirm/Reject API Endpoints

- **Type:** modify/test
- **Layer:** API
- **Files:** existing `AiAssistantController.cs`, `AiAssistantProblemDetails.cs`, `RouteNames.cs`; API tests.
- **Description:** Add thin endpoints dispatching confirm/reject commands.
- **Acceptance Criteria:** `[Authorize]`; idempotency header propagated for confirm; no prompt/payload leaks in errors.
- **Dependencies:** 3.1.
- **Effort:** M
- **Validation:** `Event.API.IntegrationTests` controller tests.

#### Task 4.2: Add Proposed Action HAL Links

- **Type:** modify/test
- **Layer:** API/HATEOAS
- **Files:** existing `AiAssistantLinkPolicy.cs`, `AiConversationResourceAssembler.cs`, `LinkRelations.cs`; tests.
- **Description:** Emit `confirm`/`reject` links only for proposed actions the current user can act on.
- **Acceptance Criteria:** Links absent for executed/rejected/failed actions and unauthorized contexts.
- **Dependencies:** 4.1.
- **Effort:** M
- **Validation:** AI HATEOAS tests and architecture authorization parity where unrelated blockers permit.

#### Task 4.3: Regenerate OpenAPI/Client/Changelog

- **Type:** docs/API contract
- **Layer:** API/Blazor Docs
- **Files:** `schemas/openapi.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API_CHANGELOG.md`, `docs/API.md` if needed.
- **Description:** Regenerate client after contract stabilization and document confirm/reject semantics.
- **Acceptance Criteria:** Generated client has confirm/reject methods; changelog documents auth/idempotency/HAL/ProblemDetails.
- **Dependencies:** 4.1, 4.2.
- **Effort:** M
- **Validation:** API build, Blazor Client build, OpenAPI diff review.

### Phase 5: Event Reference Search And Prompt Reference Packing

- **Goal:** Carry forward old Phase 4 so AI can ground proposals in bounded event references before polished UX.
- **Depends on:** Phase 4 or can run partly in parallel after Phase 1.
- **Relevant files:** new reference DTO/query/handler/packer; existing event repositories and API controller/HAL policy.
- **Acceptance criteria:** Event-first reference search returns lightweight authorized results; tenant filters preserved; prompt packer applies per-reference/total budgets and safe quoting.
- **Verification:** Application, Persistence, and API tests.
- **Rollback / failure handling:** Keep send-message reference selection disabled until reference search is tested.

#### Task 5.1: Add Reference DTOs And Query Contracts

- **Type:** create/test
- **Layer:** Application
- **Files:** new `AiReferenceSearchResultDto.cs`, `AiSelectedReferenceDto.cs`, `SearchAiReferencesQuery.cs`.
- **Acceptance Criteria:** Shape includes kind, resource ID, title, snippet, metadata, and links only; no full event content.
- **Dependencies:** Phase 1.
- **Effort:** M
- **Validation:** Application build/tests.

#### Task 5.2: Add Event Reference Query And Prompt Packer

- **Type:** modify/create/test
- **Layer:** Persistence/Application
- **Files:** event repository/specification, new `AiReferencePromptPacker.cs`, tests.
- **Description:** Search authorized tenant events and pack selected summaries into bounded prompt context.
- **Acceptance Criteria:** Deterministic sorting, cross-tenant absence, no sensitive/internal fields, prompt-injection boundaries.
- **Dependencies:** 5.1.
- **Effort:** L
- **Validation:** Persistence integration tests and Application packer tests.

### Phase 6: Blazor Product Assistant UX

- **Goal:** Build the product assistant UI only after registry, confirmation, API/HAL, and client contracts are stable.
- **Depends on:** Phases 4-5.
- **Relevant files:** `AiAssistantRail.razor*`, `AiAssistantState.cs`, new `Explore.Blazor.Client/Components/AiAssistant/*`, generated client wrapper service.
- **Acceptance criteria:** User can bootstrap, list/resume conversations, send prompts, select references, view proposal cards, confirm/reject via HAL links, and open result links; buttons are HAL-gated only.
- **Verification:** `Explore.Blazor.Client.Tests`, manual desktop/mobile smoke if app can run.
- **Rollback / failure handling:** Keep AI rail disabled/placeholder if API contracts are incomplete.

#### Task 6.1: Add Blazor AI Client Service

- **Type:** create/test
- **Layer:** Blazor Client
- **Files:** new `IAiAssistantClientService.cs`, `AiAssistantClientService.cs`, service registration.
- **Description:** Wrap generated API client methods for bootstrap/history/send/reference/confirm/reject with error handling and idempotency generation.
- **Acceptance Criteria:** Components do not call raw generated client directly; no provider secrets; API errors map to user-safe state.
- **Dependencies:** 4.3.
- **Effort:** M
- **Validation:** Blazor service tests.

#### Task 6.2: Extend AI Assistant State For Conversations

- **Type:** modify/test
- **Layer:** Blazor Client
- **Files:** `AiAssistantState.cs` and/or new `AiAssistantConversationState.cs`.
- **Description:** Track selected conversation, selected model, selected references, loading/error state, and disabled/availability state without turning client state into an authorization authority.
- **Acceptance Criteria:** Existing open/availability tests pass; state never makes local per-action authorization decisions; HAL links remain the only Confirm/Reject affordance source.
- **Dependencies:** 6.1.
- **Effort:** M
- **Validation:** Blazor state tests.

#### Task 6.3: Build Conversation Components

- **Type:** create/modify/test
- **Layer:** Blazor UI
- **Files:** `AiAssistantRail.razor*`, `AiAssistantHeader.razor`, `AiConversationList.razor`, `AiMessageList.razor`, `AiPromptComposer.razor`, CSS/test files.
- **Description:** Replace the placeholder rail with the functional assistant layout while preserving existing shell dock/fixed behavior.
- **Acceptance Criteria:** MudBlazor UI, BEM-like CSS isolation, keyboard/ARIA support, loading/error/empty/disabled states, and desktop/mobile smoke are covered.
- **Dependencies:** 6.1, 6.2.
- **Effort:** L
- **Validation:** bUnit tests and manual smoke.

#### Task 6.4: Build Reference Picker

- **Type:** create/modify/test
- **Layer:** Blazor UI
- **Files:** `AiReferencePicker.razor`, `AiReferenceChip.razor`, CSS/test files.
- **Description:** Add event reference selection UI after reference API/HAL/client contracts are stable.
- **Acceptance Criteria:** Debounced reference search, selectable/removable chips, loading/empty/error states, and keyboard-removable chips.
- **Dependencies:** 5.3, 6.1, 6.2.
- **Effort:** M
- **Validation:** bUnit reference-picker tests.

#### Task 6.5: Build Proposed Action And Result Cards

- **Type:** create/modify/test
- **Layer:** Blazor UI
- **Files:** `AiProposedActionCard.razor`, `CreateEventDraftActionPreview.razor`, `AiActionResultCard.razor`, CSS/test files.
- **Description:** Add proposed-action preview and result cards after confirm/reject API/HAL/client contracts are stable.
- **Acceptance Criteria:** Confirm/Reject buttons only render from HAL links; double-submit prevented; result links use API links; proposal visuals remain distinct from committed event state.
- **Dependencies:** Phase 4, 6.1, 6.2.
- **Effort:** L
- **Validation:** bUnit HAL-gating tests.

#### Task 6.6: Add Full Panel Test Coverage

- **Type:** test
- **Layer:** Blazor Tests
- **Files:** `Explore.Blazor.Client.Tests/Components/AiAssistant/*`, existing layout/dock bridge tests as needed.
- **Description:** Preserve the old AI plan’s full panel coverage gate.
- **Acceptance Criteria:** Tests cover bootstrap, history, model selection, references, send, proposal cards, confirm/reject, disabled/error states, and existing dock bridge behavior.
- **Dependencies:** 6.1-6.5.
- **Effort:** L
- **Validation:** `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.

### Phase 7: Optional ISLAMU Event MCP Adapter

- **Goal:** Expose selected registry tools/resources/prompts to trusted external agents through MCP without making MCP the authority.
- **Depends on:** Registry and confirmation engine; API/HAL stability strongly preferred.
- **Relevant files:** new optional MCP host/adapter files TBD; `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, Aspire/Docker profile docs if touched.
- **Acceptance criteria:** MCP is optional; disabled by default or profile-gated; authenticated; tenant-bound; rate-limited; audited; protocol conformance tests exist; mutating tools default to proposal creation or confirmation flow.
- **Verification:** MCP protocol conformance tests, API/Application tests, authz/tenant isolation tests, docs review.
- **Rollback / failure handling:** MCP adapter can be disabled without affecting in-product AI.

#### Task 7.1: Research And Select MCP Hosting Strategy

- **Type:** investigate/docs
- **Layer:** API/Infrastructure/DevOps
- **Files:** plan update; optional ADR under docs if needed.
- **Description:** Evaluate .NET MCP server libraries/transport, hosting inside API vs separate Aspire resource, OAuth/machine auth, and self-hosting impact.
- **Acceptance Criteria:** Decision records required services, optional dependencies, auth mode, failure behavior, and operator docs impact.
- **Dependencies:** Phase 1.
- **Effort:** M
- **Validation:** Source-backed decision in plan/context.

#### Task 7.2: Implement MCP Adapter Over Registry

- **Type:** create/test
- **Layer:** API/Infrastructure/Application adapter
- **Files:** TBD after 7.1.
- **Description:** Expose registry tool definitions as MCP tools/resources/prompts. Default mutating path creates proposed actions or invokes confirmed/trusted-direct path only under explicit policy.
- **Acceptance Criteria:** No direct repository mutation; tenant-bound auth context required; audit entries created; rate budgets enforced.
- **Dependencies:** 7.1, Phase 3.
- **Effort:** XL
- **Validation:** MCP conformance, authz, tenant isolation, rate/audit tests.

### Phase 8: Retention, Redaction, Streaming/Cancellation, Operations, And Final Docs

- **Goal:** Complete operational hardening and release documentation.
- **Depends on:** Core AI registry/API/UI/MCP slices as applicable.
- **Relevant files:** AI settings, cleanup jobs, docs, dashboards/runbooks, tests.
- **Acceptance criteria:** Retention cleanup is tenant-safe and observable; cancellation/streaming decision documented and tested; ops docs cover provider/MCP disabled/misconfigured/rate-limited/failure states.
- **Verification:** Application/API/Infrastructure/Blazor tests plus docs checks.
- **Rollback / failure handling:** Keep streaming and MCP disabled if operational readiness is incomplete.

#### Task 8.1: Add Retention Cleanup And Redaction Jobs

- **Type:** create/modify/test/docs
- **Layer:** Application/Persistence/Infrastructure/Operations
- **Files:** cleanup handler/job files TBD, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`.
- **Description:** Enforce `ai_assistant.retention_days` with tenant-scoped cleanup/redaction and safe metrics.
- **Acceptance Criteria:** No cross-tenant deletes; dry-run/operator visibility considered; no prompt/content in logs.
- **Dependencies:** Phase 3.
- **Effort:** L
- **Validation:** Persistence/API/Application tests.

#### Task 8.2: Add Cancellation Semantics

- **Type:** create/modify/test/docs
- **Layer:** Application/API/Infrastructure/Blazor
- **Files:** AI run handlers/controller/provider/client components TBD.
- **Description:** Carry forward the old explicit cancellation task as a separate decision/implementation gate rather than hiding it inside streaming.
- **Acceptance Criteria:** Cancellation endpoint/API/UI exists if implemented; cancel HAL link appears only for cancellable runs; provider cancellation token is honored; cancelled runs produce no proposed actions.
- **Dependencies:** Core send/run flow and API/HAL stability.
- **Effort:** M
- **Validation:** Application/API/Blazor tests.

#### Task 8.3: Decide And Implement Streaming Or Polling

- **Type:** investigate/create/modify/test/docs
- **Layer:** Application/API/Infrastructure/Blazor
- **Files:** TBD after decision.
- **Description:** Decide whether the first advanced UX should use SSE, SignalR, polling, or stay non-streaming until later.
- **Acceptance Criteria:** Decision is documented; auth, tenant isolation, BFF boundary, cancellation, and non-streaming fallback are preserved.
- **Dependencies:** Core confirmable non-streaming flow.
- **Effort:** M-XL
- **Validation:** API/Blazor tests and manual smoke if implemented.

#### Task 8.4: Add Provider/Run Dashboards And Runbook Polish

- **Type:** docs/ops/test
- **Layer:** Operations/Infrastructure/API
- **Files:** metrics dashboards/runbook docs, `docs/OPERATIONS.md`, troubleshooting docs if present.
- **Description:** Preserve the old advanced provider/run operations task.
- **Acceptance Criteria:** Builds on Phase 2 health/metrics/logging; no secrets/content in logs; dashboards use low-cardinality dimensions; runbooks cover disabled, misconfigured, unavailable, rate-limited, failed-confirmation, stuck-action, and MCP states.
- **Dependencies:** Provider health/telemetry and confirmation engine.
- **Effort:** M
- **Validation:** Infrastructure/API tests or docs checks as applicable.

#### Task 8.5: Final Validation And Documentation Refresh

- **Type:** docs/test
- **Layer:** All
- **Files:** docs and dev docs touched by completed slices, including `docs/API.md`, `docs/API_CHANGELOG.md`, `schemas/openapi.json`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, `docs/BLAZOR.md`, `docs/DOCK_LAYOUT.md`, `docs/ACCESSIBILITY.md` if needed, `README.md` if Plane inspiration credit applies.
- **Description:** Bring API/config/self-hosting/operations/Blazor docs and dev docs to release quality.
- **Acceptance Criteria:** All changed behavior documented; generated clients current; old AI plan archived; dock behavior, keyboard/focus/accessibility, HAL-gated action workflow, provider setup, secrets, model allow-list, limits, retention, health, disable behavior, troubleshooting, and Plane inspiration credit are documented where applicable.
- **Dependencies:** all implemented phases.
- **Effort:** L
- **Validation:** Per-project builds/tests, architecture/context tests, docs lint if applicable.

## 7. Testing Strategy

- **Application unit tests:** registry contracts, payload guard, parser/schema registry behavior, `CreateEventDraft` schema/mapper parity, confirm/reject handlers, executor dispatch and failure handling. Command: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` with targeted `--treenode-filter` during slices.
- **Domain unit tests:** only needed if proposed-action/tool-execution lifecycle changes. Command: `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`.
- **Persistence integration tests:** needed for retention cleanup, proposed-action lookup/update, execution audit, reference search. Command: `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` targeted by class.
- **API integration tests:** confirm/reject endpoints, HAL links, ProblemDetails, idempotency, OpenAPI route shape, DB-backed create-draft flow. Command: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` targeted by class.
- **Blazor tests:** client service/state/components/HAL-gated proposal buttons. Command: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.
- **Infrastructure tests:** MCP adapter hosting/config/health/rate/audit if implemented there. Command depends on selected host, likely `Explore.Infrastructure.Tests` and/or `Event.API.IntegrationTests`.
- **Architecture/context tests:** run when new files/rules/docs change or before PR. Command: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`. If unrelated known HATEOAS parity failures remain, record them explicitly.
- **Full final validation:** before closing the workstream, attempt the canonical build plus per-project test list from the tasks file, including Domain, Application, Infrastructure, Persistence, API integration, Blazor Client, Blazor Integration when UI flow changes require it, and Architecture tests. Document unrelated pre-existing failures instead of hiding them.

## 8. Documentation, Configuration, And Operations Impact

- Update `dev/active/ai-integration/*` to point to this workstream after approval, then archive it.
- Update `docs/API.md` and `docs/API_CHANGELOG.md` when confirm/reject or MCP API contracts land.
- Update `schemas/openapi.json` and generated Blazor client after API stabilization.
- Update `docs/CONFIGURATION.md` for any new `ai_assistant.*`, `AiProvider:*`, or MCP adapter keys.
- Update `docs/SELF_HOSTING.md` when MCP/retention jobs add optional services, env vars, profiles, or recovery steps.
- Update `docs/OPERATIONS.md` for runbooks, metrics, health, rate limits, retention cleanup, and MCP troubleshooting.
- Update `docs/BLAZOR.md`, `docs/DOCK_LAYOUT.md`, and `docs/ACCESSIBILITY.md` when the assistant rail becomes functional or keyboard/focus/dock behavior changes.
- Update `README.md` or an appropriate AI/UI doc with Plane inspiration credit if the final UI materially uses the existing Plane analysis; credit inspiration only, and do not claim copied code unless code is actually ported.
- Do not require MCP infrastructure for basic self-hosted AI chat unless user explicitly chooses that product posture.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- In-product AI and MCP both treat tool input as untrusted.
- Mutating tools require confirmation by default and dispatch existing authorized CQRS commands.
- Confirm/reject commands must enforce current-user ownership, tenant isolation, action state, idempotency, and tool enablement.
- HAL links are the only UI affordance source; Blazor cannot inspect local roles for per-proposal buttons.
- MCP authentication must distinguish user-delegated context from machine-to-machine context. Machine principals need explicit scopes, tenant binding, rate budgets, and audit.
- Provider/MCP telemetry must exclude prompts, responses, selected reference content, provider endpoints, API keys, model raw errors, and credentials.
- Quotas and API rate limits from Phase 3.9 remain in force before provider or tool side effects.
- Retention cleanup must handle prompts/proposals/tool results according to `ai_assistant.retention_days` and avoid cross-tenant deletion.
- ProblemDetails must use safe failure codes/messages and never echo raw payloads.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

- **Multi-tenancy:** Applicable. Registry execution context must include tenant and current user/machine principal; repository queries keep tenant filters; MCP must be tenant-bound.
- **Federation:** Needs investigation. Do not expose federated event mutation tools until federation authority and remote side effects are explicitly modeled.
- **Localization:** Applicable for UI and user-facing errors. Tool schemas/failure codes remain stable English/internal; UI can localize display text later.
- **Accessibility:** Applicable for Blazor rail and proposal cards. Use keyboard-accessible buttons, ARIA labels, focus management, and bUnit accessibility checks where available.
- **Product:** Registry enables a safer product assistant, external developer/admin automation, future bulk import, and self-hosted power-user workflows without splitting authority across transports.

## 11. Observability And Operations

- Add structured logs for registry validation failures by code/tool kind only, never raw payload/content.
- Add metrics for tool proposals, validation failures, confirmation attempts, confirmation successes/failures, duplicate confirms, rejects, MCP requests, MCP auth failures, and retention cleanup results.
- Keep dimensions low-cardinality: tool kind, outcome, failure category, provider/adapter, not tenant/user IDs unless already allowed by metric policy.
- Add health/readiness checks for optional MCP adapter only when enabled.
- Add operator runbooks for disabled AI, invalid provider config, rate-limited sends, failed confirmations, stuck proposed actions, retention cleanup failures, and MCP disabled/misconfigured/auth failures.

## 12. Migration And Compatibility Plan

- The project is pre-v1, so prefer clean registry contracts over compatibility shims for old internal AI schema code.
- Phase 1-2 should preserve current external behavior while changing internals. No data migration expected.
- Phase 3 may require new proposed-action execution metadata or additional columns only if current `AiToolExecution` is insufficient; if so, add EF migration, indexes, schema docs, self-hoster notes, and rollback caveat.
- Phase 4 changes public API/OpenAPI. Regenerate `schemas/openapi.json`, generated Blazor client, and changelog in the same slice.
- Phase 7 MCP is optional and should be disabled/profile-gated by default. Self-hosters must be able to run without MCP services.
- Old `dev/active/ai-integration` is documentation history only after archive; no runtime compatibility impact.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---|---|---|---|---|
| Registry becomes over-generic and hard to use. | Medium | High | Keep first implementation scoped to `CreateEventDraft`; add abstractions only when used. | Complex generic types, weak tests, unclear failure codes. | Phase 1-2 |
| MCP bypasses product authorization. | Medium | Critical | MCP adapter calls registry/API/Application only; no repository writes; authz/tenant tests required. | MCP tests can mutate without command auth. | Phase 7 |
| Schema and mapper drift. | High | High | Add schema/mapper parity tests for each tool. | Schema includes fields mapper rejects or vice versa. | Task 2.2 |
| Confirm duplicate creates multiple events. | Medium | Critical | Idempotency key and proposed-action state transition tests. | Duplicate confirm creates >1 event in DB-backed flow. | Phase 3-4 |
| Blazor renders buttons by role instead of HAL. | Medium | High | Component tests assert absence of HAL link hides buttons. | Components inspect claims/roles for proposal actions. | Phase 6 |
| Retention cleanup deletes cross-tenant data. | Low | Critical | Tenant-scoped repository queries and integration tests. | Cleanup test deletes another tenant’s conversation. | Phase 8 |
| Optional MCP raises self-hosting floor. | Medium | Medium | Disabled/profile-gated default; no required new service for basic deployment. | Basic compose/Aspire profile needs MCP config. | Phase 7 |
| Existing unrelated architecture failures obscure registry validation. | High | Medium | Record unrelated failures separately and run targeted tests. | Architecture tests fail in non-AI HATEOAS policies. | All phases |

## 14. Success Metrics And Definition Of Done

- Registry owns tool schema, allowed kinds, validation policy, and execution metadata.
- `CreateEventDraft` is registered through the registry with no regression from Phase 5.1 safety tests.
- Confirm/reject commands exist and dispatch `CreateEventCommand` for confirmed draft creation.
- Confirm/reject API endpoints and HAL links are stable, documented, and covered by integration tests.
- Blazor proposal buttons render only from HAL links.
- Optional MCP adapter, if implemented, uses the same registry and does not bypass CQRS/auth/tenant/audit boundaries.
- Retention/redaction posture is implemented before broad history/UI enablement.
- Required validation passes for changed slices, using project-level commands rather than solution-level `dotnet test`.
- Dev docs stay current: this plan, context, and tasks reflect completed and remaining work before any handoff.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

Future agents implementing this plan MUST follow this contract:

1. Before starting any implementation slice, read this plan, `ai-tool-contract-registry-context.md`, and `ai-tool-contract-registry-tasks.md`.
2. Start from the highest-priority incomplete task unless user instruction overrides it.
3. Re-read the matched intent docs/rules/skills for the slice being implemented.
4. After completing each meaningful task or discovering new scope, update this plan if architecture/scope changed, update context with current state/validation/blockers, and check off tasks.
5. Do not report “done” unless docs reflect the actual current state.
6. Every implementation summary must teach the user what changed, name important files/classes, describe control/data flow, state what was verified, and list what remains.
7. If validation fails, update context/tasks with failure, root cause if known, and next recovery action.
8. Before pausing, context reset, handoff, or PR creation, refresh all three dev docs and add/refresh a handoff section.

## 16. Progress Reporting Contract

When an implementation agent finishes a slice, final response should use:

- **Implemented:** Developer teaching summary naming patterns, libraries/infrastructure, important files/classes, and data/control flow.
- **Verified:** Exact commands/tests and outcomes.
- **Remaining:** Concrete unfinished tasks and blockers.
- **Next:** Recommended next slice.
- **Docs updated:** Whether plan/context/tasks were refreshed.

## 17. Potential Risks & Unknowns

The hardest part is keeping the registry small enough to remain understandable while strong enough to prevent MCP and in-product AI from drifting apart. If Phase 1 over-engineers generic abstractions before `CreateEventDraft` is migrated, it will slow delivery. If Phase 7 builds MCP as a separate product surface rather than a registry adapter, the platform will split authorization, tenancy, confirmation, and audit semantics. The first implementation slice must therefore prove the registry with exactly one existing tool before expanding the tool catalog.
