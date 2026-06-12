<!-- ABOUTME: Executable task checklist for implementing full event-management MCP capabilities. -->
<!-- ABOUTME: Breaks the work into reviewable slices with acceptance criteria and verification commands. -->

# Full Event-Management MCP — Tasks

Last Updated: 2026-06-08 Europe/Brussels

## Status

- **Overall:** Draft plan created; implementation not started.
- **Current recommended next task:** Review Phase 0 decisions, then begin Phase 1 capability matrix or Phase 2 public read slice.
- **Related workstream:** `dev/active/ai-tool-contract-registry/` Phase 13.5 overlaps with public MCP read resources.

## Phase 0: Review Scope And Safety Boundary

- [x] **0.1 Create planning docs**
  - **Files:** `dev/active/full-event-management-mcp/full-event-management-mcp-plan.md`, `full-event-management-mcp-context.md`, `full-event-management-mcp-tasks.md`.
  - **Acceptance:** Plan records current implementation, gaps, decisions, phases, risks, and validation.
  - **Validation:** `git diff --check -- dev/active/full-event-management-mcp/full-event-management-mcp-plan.md dev/active/full-event-management-mcp/full-event-management-mcp-context.md dev/active/full-event-management-mcp/full-event-management-mcp-tasks.md`.
  - **Progress:** Completed as a docs-only planning task on 2026-06-08.

- [ ] **0.2 Confirm write semantics with user**
  - **Files:** Active docs only unless ADR requested.
  - **Decision required:** Does “full event-management MCP” mean proposal-first mutations, or should direct writes be supported?
  - **Recommended default:** Proposal-first mutations only.
  - **Acceptance:** User-approved decision is recorded in plan/context/tasks.
  - **Validation:** Docs review.

- [ ] **0.3 Add ADR if direct MCP writes are requested**
  - **Files:** `docs/adr/ADR-0xx-*.md`, `docs/OPERATIONS.md`, `docs/MCP_DEBUGGING.md`, active docs.
  - **Acceptance:** ADR defines direct-write approval model, scopes, idempotency, audit, rate limits, HAL relationship, rollback, and tests before code changes.
  - **Validation:** Docs review and architecture tests if ADR/schema rules apply.
  - **Dependency:** 0.2 direct-write approval.

## Phase 1: Capability Matrix And Contract Baseline

- [ ] **1.1 Generate event API-to-MCP capability matrix**
  - **Files:** `dev/active/full-event-management-mcp/full-event-management-mcp-context.md` and/or a new matrix section in the plan.
  - **Inputs:** `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, event-related controllers.
  - **Acceptance:** Each event-adjacent REST operation is classified as public read, authenticated read, proposal mutation, direct mutation deferred, not suitable for MCP, or needs ADR.
  - **Validation:** Spot-check matrix against `schemas/openapi.json` event paths.

- [ ] **1.2 Define MCP naming and response conventions**
  - **Files:** Active plan/context, later `Explore.API/Mcp/EventManagementMcpDescriptors.cs`.
  - **Acceptance:** Tool/resource names are stable, lower snake case, bounded, and mapped to REST/CQRS sources; response descriptors have safe field lists and max sizes.
  - **Validation:** Review against current MCP SDK tests/conventions.

- [ ] **1.3 Decide public read response shape**
  - **Files:** Active docs; later MCP descriptor files.
  - **Options:** Return bounded descriptors with HAL action summary, or full HAL JSON where appropriate.
  - **Acceptance:** Decision preserves HAL authority without oversized payloads.
  - **Validation:** Prototype tests in Phase 2.

## Phase 2: Public Event Read MCP Surface

- [ ] **2.1 Add public event search MCP tool**
  - **Files:** `Explore.API/Mcp/EventManagementMcpTools.cs`, `Explore.API/Mcp/EventManagementMcpDescriptors.cs`, `Explore.API/Mcp/EventManagementMcpJsonContext.cs`, `Explore.API/Program.cs`.
  - **Description:** Add `search_public_events` using MediatR `GetEventListRequest` with bounded pagination/filter inputs.
  - **Acceptance:** Anonymous/no-key/blank-key/invalid-key clients can call it; result matches public REST visibility; page size is capped; no private/draft/archived leakage.
  - **Validation:** New `Event.API.IntegrationTests/Features/EventManagementMcpPublicReadTests.cs` REST-vs-MCP list parity tests.

- [ ] **2.2 Add public event detail MCP resource/tool**
  - **Files:** `Explore.API/Mcp/EventManagementMcpResources.cs` or `EventManagementMcpTools.cs`, descriptors/json context, `Program.cs`.
  - **Description:** Add `get_public_event` or resource template `islamu-event://events/{eventId}` using `GetEventDetailsRequest`.
  - **Acceptance:** Published public events are readable; draft/archived/private/unknown events return a safe not-found descriptor; no raw exceptions.
  - **Validation:** REST-vs-MCP detail parity tests.

- [ ] **2.3 Add public program/session read slice if approved**
  - **Files:** Event MCP tools/resources; tests.
  - **Description:** Add `get_event_program_summary` and optionally `list_event_sessions` using existing queries.
  - **Acceptance:** Public visibility matches REST; responses are bounded.
  - **Validation:** Program/session REST-vs-MCP tests.

- [ ] **2.4 Update MCP SDK contract tests for event read primitives**
  - **Files:** `Event.API.IntegrationTests/Features/McpSdkContractTests.cs`, `McpProtocolContractTests.cs`.
  - **Acceptance:** New MCP methods/classes have SDK attributes, descriptions, parameter descriptions, cancellation tokens, and explicit registration.
  - **Validation:** Focused MCP API integration tests.

- [ ] **2.5 Update docs for public event MCP smoke**
  - **Files:** `docs/MCP_DEBUGGING.md`, `docs/OPERATIONS.md`, `docs/API.md`, active docs.
  - **Acceptance:** Docs state anonymous event MCP reads are public-only and parity-tested; curl/Inspector smoke examples remain redacted.
  - **Validation:** `git diff --check`.

## Phase 3: Authenticated Event Management Read Context

- [ ] **3.1 Add my-events MCP read capability**
  - **Files:** Event MCP tools/resources; tests.
  - **Description:** Add `list_my_events` using `GetMyEventsRequest`.
  - **Acceptance:** Requires valid scoped principal; no anonymous/invalid-key access; tenant mismatch fails closed.
  - **Validation:** MCP authorization tests for bearer user and API key scope combinations.

- [ ] **3.2 Add event creation context MCP capability**
  - **Files:** Event MCP tools/resources; tests.
  - **Description:** Add `get_event_creation_context` using `GetEventCreationContextRequest`.
  - **Acceptance:** Exposes publisher options and tenant policy safely; no role/claim inference in MCP.
  - **Validation:** Application handler tests already exist; add MCP protocol/auth tests.

- [ ] **3.3 Add event management context resource**
  - **Files:** Event MCP resources/descriptors; may use `IResourceAssembler<EventDto, EventListDto>` and `IHttpContextAccessor`.
  - **Description:** For an event id, return bounded event state, concurrency stamp, publish readiness summary/link, and action descriptors derived from HAL `_links`.
  - **Acceptance:** MCP action list matches HAL links for edit/delete/publish/add-session/session-create-context; absent links mean unavailable actions.
  - **Validation:** HAL-vs-MCP parity tests.

- [ ] **3.4 Add publish-readiness MCP read capability**
  - **Files:** Event MCP tools/resources; tests.
  - **Description:** Add `get_event_publish_readiness` using `GetEventPublishReadinessRequest`.
  - **Acceptance:** Requires management authority; response is bounded and safe.
  - **Validation:** MCP auth and handler parity tests.

- [ ] **3.5 Harden scopes for private event MCP reads**
  - **Files:** `Explore.API/Extensions/AuthenticationExtensions.cs`, external API-key scope tests, docs, possibly `ExternalApiKeyScopes.cs` if new scopes are needed.
  - **Acceptance:** API keys require MCP read authority and event/domain read authority; `mcp:read` alone does not grant private event management reads.
  - **Validation:** External API Key tests and MCP authorization tests.

## Phase 4: Proposal-First Core Event Mutation Tools

- [ ] **4.1 Add `UpdateEventDraft` ATCR definition and mapper**
  - **Files:** `Explore.Application/Features/AiAssistant/Tools/*`, `Actions/*`, tests.
  - **Description:** Add schema and mapper for proposed event draft updates.
  - **Acceptance:** Forbids tenant, actor, status, server-owned projection fields; requires expected concurrency stamp.
  - **Validation:** Unit tests for schema fields, hidden-field rejection, mapper output.

- [ ] **4.2 Add projected `propose_update_event_draft` MCP tool**
  - **Files:** Existing projection factory may auto-project after 4.1; tests.
  - **Acceptance:** Tool appears only for `mcp:propose` scoped principals; persists proposed action; does not update event directly.
  - **Validation:** MCP protocol tests count unchanged event state and one proposed action.

- [ ] **4.3 Add publish proposal support**
  - **Files:** ATCR definition/action mapper/tests.
  - **Description:** Add `propose_publish_event` with expected concurrency stamp and readiness context.
  - **Acceptance:** Proposal can be made only when HAL/action authority allows publish; no direct status change until confirmation.
  - **Validation:** MCP/AI proposed-action tests; publish handler tests remain source of direct execution behavior.

- [ ] **4.4 Add delete proposal support**
  - **Files:** ATCR definition/action mapper/tests.
  - **Description:** Add `propose_delete_event` with explicit destructive summary/confirmation metadata.
  - **Acceptance:** Tool is marked destructive in metadata/hints but authorization remains product-side; no delete until confirmation.
  - **Validation:** Redaction/proposal/no-side-effect tests.

- [ ] **4.5 Add aspect proposal support**
  - **Files:** ATCR definitions/action mappers/tests for Islamic and Tech aspects.
  - **Acceptance:** Upsert/delete aspect proposals validate allowed fields and module/permission context; no direct aspect mutation.
  - **Validation:** Unit and MCP protocol tests.

- [ ] **4.6 Update MCP prompt guidance for event management**
  - **Files:** `Explore.API/Mcp/EventManagementMcpPrompts.cs` or `AiAssistantMcpPrompts.cs`, docs/tests.
  - **Acceptance:** Prompt teaches agents to read management context, check HAL-derived actions, propose changes, and wait for confirmation.
  - **Validation:** Prompt listed for scoped clients only; SDK contract tests.

## Phase 5: Event Sub-Resource Management Slices

- [ ] **5.1 Event session read/proposal slice**
  - **Files:** Event MCP tools/resources, ATCR definitions/action mappers, tests.
  - **Acceptance:** Read sessions by event/session; propose create/update/delete session; preserve schedule conflict validation through existing command path after confirmation.
  - **Validation:** REST/MCP read parity and no-direct-side-effect proposal tests.

- [ ] **5.2 Event session group/program-section slice**
  - **Files:** Event MCP tools/resources, ATCR definitions/action mappers, tests.
  - **Acceptance:** Read groups/program sections; propose create/update/delete/assign/unassign.
  - **Validation:** REST/MCP parity and proposal tests.

- [ ] **5.3 Event day and agenda item slice**
  - **Files:** Event MCP tools/resources, ATCR definitions/action mappers, tests.
  - **Acceptance:** Read agenda/day projections; propose day/agenda item changes.
  - **Validation:** REST/MCP parity and proposal tests.

- [ ] **5.4 Custom property definition/value slice**
  - **Files:** Event MCP tools/resources, ATCR definitions/action mappers, tests.
  - **Acceptance:** Respect projection feature flags, quota settings, tenant governance, and authenticated-only custom-property APIs.
  - **Validation:** Projection/gating parity tests.

- [ ] **5.5 Registration and team management slice**
  - **Files:** Event MCP tools/resources, ATCR definitions/action mappers, tests.
  - **Acceptance:** Registration/team operations remain authorization-sensitive and proposal-first where mutating.
  - **Validation:** REST/MCP auth parity and proposal tests.

- [ ] **5.6 Template and template-sync slice**
  - **Files:** Event MCP tools/resources, ATCR definitions/action mappers, tests.
  - **Acceptance:** Diff/history reads are bounded; apply sync remains proposal-first or ADR-gated due to complex mutation/concurrency.
  - **Validation:** Stale-base/concurrency parity tests.

## Phase 6: Cross-Cutting Hardening

- [ ] **6.1 Extend MCP telemetry allow-list**
  - **Files:** `Explore.API/Mcp/McpAdapterTelemetry.cs`, tests.
  - **Acceptance:** New event tool/resource names normalize to bounded known labels; unknown/sensitive labels normalize to `unknown`.
  - **Validation:** Telemetry normalization unit/integration tests.

- [ ] **6.2 Add event MCP redaction tests**
  - **Files:** `Event.API.IntegrationTests/Features/EventManagementMcpRedactionTests.cs`.
  - **Acceptance:** Errors do not echo prompts, payload secrets, API keys, bearer tokens, endpoint URLs, raw exceptions, tenant/user ids, or stack traces.
  - **Validation:** Focused redaction test class.

- [ ] **6.3 Add tenant/rate-limit matrix tests for event MCP**
  - **Files:** MCP authorization/rate-limit tests.
  - **Acceptance:** Anonymous/invalid-key traffic is IP-partitioned; valid API keys are key-id partitioned; multi-tenant requests fail closed without trusted tenant binding.
  - **Validation:** Focused MCP tests with rate limiting enabled.

- [ ] **6.4 Add architecture dependency assertions**
  - **Files:** `Event.Architecture.Tests/**` if needed.
  - **Acceptance:** Domain/Application have no `ModelContextProtocol` references; MCP classes remain in API; no repository injection into MCP tools/resources.
  - **Validation:** Architecture tests.

## Phase 7: Docs, Runbooks, And Production Readiness

- [ ] **7.1 Update MCP debugging runbook**
  - **Files:** `docs/MCP_DEBUGGING.md`.
  - **Acceptance:** Lists event read/management/proposal tools, safe curl/Inspector examples, expected anonymous/scoped surfaces, and forbidden artifacts.
  - **Validation:** Docs review and `git diff --check`.

- [ ] **7.2 Update operations/configuration/API docs**
  - **Files:** `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/API.md`, `docs/AI_AGENT_EXPERIENCE_HARDENING.md`.
  - **Acceptance:** Production posture, scopes, rollback, public TLS expectation, and proposal-first event-management workflow are documented.
  - **Validation:** Docs review and `git diff --check`.

- [ ] **7.3 Update API changelog only if REST contract changes**
  - **Files:** `docs/API_CHANGELOG.md`, `schemas/openapi.json`, generated clients if REST changes.
  - **Acceptance:** Pure MCP additions do not pretend to be REST/OpenAPI changes; any REST change is logged.
  - **Validation:** OpenAPI parity tests if REST changed.

- [ ] **7.4 Add deterministic replay/eval coverage if agent workflow changes**
  - **Files:** `Explore.Diagnostic/AiReplay/*`, `Explore.Diagnostic/AiEvaluation/*`, tests/docs.
  - **Acceptance:** Fake/replay checks cover event MCP proposal-first behavior without live provider credentials.
  - **Validation:** Diagnostic tests/reports if modified.

## Final Verification Before Claiming Implementation Complete

Run the focused tests for touched slices, then:

```bash
dotnet test Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*Mcp*/*|/*/*/*Event*/*" --no-progress --maximum-parallel-tests 1
dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*AiAssistant*/*|/*/*/*Events*/*" --no-progress --maximum-parallel-tests 1
dotnet test Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --no-restore -- --no-progress --maximum-parallel-tests 1
dotnet build --configuration Release --verbosity quiet --no-restore
git diff --check
```

If the worktree contains unrelated dirty changes, record that in the context doc and do not revert them.
