<!-- ABOUTME: Operational context for the AI Tool Contract Registry workstream. -->
<!-- ABOUTME: Captures current evidence, decisions, risks, and next actions for registry and MCP adapter planning. -->

# AI Tool Contract Registry — Context

Last Updated: 2026-06-01 Europe/Brussels

## SESSION PROGRESS (2026-06-01 Europe/Brussels)

### ✅ COMPLETED

- Created this new workstream to supersede `dev/active/ai-integration` after user review.
- Read `/dev-docs` command requirements and Senior CTO planning guardrails.
- Inspected old AI integration plan/context/tasks and carried forward unfinished Phase 4-8 work.
- Verified current implementation evidence: hardcoded `CreateEventDraft` schema/parser path, existing strict mapper, existing AI persistence/API/provider foundation, no MCP code, and roadmap-only MCP docs.
- Wrote registry-centered plan, context, and task checklist.
- Audited old Phase 4-8 tasks against this workstream and patched missing fidelity items: Plane inspiration credit, dock/accessibility docs, separate Blazor state/reference/proposal/full-panel tasks, explicit cancellation semantics, advanced dashboards/runbooks, and the full final validation matrix.
- Implemented Phase 1.1 registry contracts in `Explore.Application/Features/AiAssistant/Tools/*` with no API/Blazor/Persistence dependency.
- Implemented Phase 1.2 `AiToolPayloadGuard` plus unit coverage for malformed JSON, non-object payloads, unknown fields, forbidden fields, and registry-backed validation.
- Implemented Phase 1.3 registry-backed prompt/parser wiring: `AiSystemPromptFactory` reads tool schema/kinds from the registry, `AiStructuredActionParser` validates provider payloads through the registry, and `SendAiMessageCommandHandler` shares one default registry instance between prompt construction and parser validation.
- Implemented Phase 2.1/2.2 `CreateEventDraft` registry migration hardening: the tool definition now carries mapper metadata, authorization metadata, provider/MCP exposure flags, and schema/mapper drift tests.
- Implemented Phase 3.1/3.2 proposed-action confirmation and first executor path: confirm/reject Application commands, fail-closed tenant/user checks, duplicate-safe state handling, and `CreateEventDraft` execution through `CreateEventCommand`/MediatR.

### 🟡 IN PROGRESS

- Ready for Phase 3.3 safe execution-result metadata using `AiToolExecution` or an explicit decision that the current schema is sufficient for the first release.

### ⏭️ NEXT

1. Decide whether to add an explicit archived pointer for the already-deleted `dev/active/ai-integration` workstream or leave deletion as the archive outcome.
2. Next implementation slice: Phase 3.3, persisting/querying safe execution audit metadata without raw provider/tool payload leakage.
3. After Phase 3.3, move to Phase 4 API/HAL/OpenAPI so UI affordances can be gated by links instead of local authorization logic.

### ⚠️ BLOCKERS

- No implementation blocker for Phase 3.3 execution audit metadata.
- MCP hosting/protocol package selection is intentionally unresolved until Phase 7 research.
- Old AI migration history is mixed; avoid claiming a clean AI-scoped migration.
- Broad Blazor AI enablement remains blocked on Phase 4 confirm/reject API/HAL links and retention posture.
- Lossless handoff audit is now complete; no known old Phase 4-8 task remains unmapped after the latest patch.
- Worktree was already heavily dirty before this slice, including deleted `dev/active/ai-integration/*` files and unrelated CI/API/Blazor/Infrastructure changes. This slice did not revert or modify those unrelated changes.

## Quick Resume

1. Read `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-plan.md`.
2. Read `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-tasks.md`.
3. Do not continue old `dev/active/ai-integration` tasks directly unless user rejects this plan.
4. Continue with Phase 3.3: safe execution-result metadata for confirmed tools, then Phase 4 API/HAL endpoints.
5. Keep all three dev docs updated after every meaningful implementation slice.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `dev/active/ai-integration/*` | Existing | Docs | Old AI implementation workstream. | Superseded after user approval; unfinished tasks migrated here. |
| `dev/active/ai-integration/ai-integration-plane-report.md` | Existing | Docs | Plane inspiration analysis/reference artifact. | Not implementation status; final docs should credit inspiration if UI materially uses those ideas. |
| `Explore.Application/Features/AiAssistant/Prompting/AiStructuredActionParser.cs` | Existing | Application | Validates provider proposed actions before persistence. | Registry-backed; rejects unknown kinds and invalid/forbidden payload fields before persistence. |
| `Explore.Application/Features/AiAssistant/Prompting/AiSystemPromptFactory.cs` | Existing | Application | Produces system prompt and action schema. | Reads provider-visible tool kinds/schema from the registry. |
| `Explore.Application/Features/AiAssistant/Actions/CreateEventDraftAiActionPayload.cs` | Existing | Application | Safe event draft payload shape. | Keep as first registered tool payload. |
| `Explore.Application/Features/AiAssistant/Tools/CreateEventDraftAiToolDefinition.cs` | New | Application | First registry-backed tool definition for provider schema, allowed fields, forbidden fields, confirmation posture, mapper metadata, authorization metadata, and exposure flags. | Added in Phase 1.3 and hardened in Phase 2. |
| `Explore.Application/Features/AiAssistant/Actions/CreateEventDraftAiActionMapper.cs` | Existing | Application | Strict mapper from untrusted JSON to `CreateEventDraftRequestDto`. | Uses the registry definition's allowed field set so schema/mapper fields do not drift. |
| `Explore.Application/Features/AiAssistant/Actions/CreateEventDraftAiToolExecutor.cs` | New | Application | Executes confirmed `CreateEventDraft` proposals. | Maps untrusted payload, dispatches `CreateEventCommand` through MediatR, and never writes event repositories directly. |
| `Explore.Application/Contracts/Infrastructure/Ai/AiChatModels.cs` | Existing | Application | Provider-neutral chat contracts and `AiStructuredActionSchema`. | Registry should feed `AiStructuredActionSchema`. |
| `Explore.Application/Features/AiAssistant/Handlers/Commands/SendAiMessageCommandHandler.cs` | Existing | Application | Orchestrates send-message and persists proposed actions. | Shares one registry instance between prompt schema and parser validation; still does not execute tools during send. |
| `Explore.Application/Features/AiAssistant/Requests/Commands/ConfirmAiProposedActionCommand.cs` | New | Application | Authenticated command for confirming a proposed action. | Uses AI-conversation authorization metadata and action ID as the secure resource identifier; handler enforces tenant/user ownership. |
| `Explore.Application/Features/AiAssistant/Requests/Commands/RejectAiProposedActionCommand.cs` | New | Application | Authenticated command for rejecting a proposed action. | Reject path has no tool side effects and is duplicate-safe. |
| `Explore.Application/Features/AiAssistant/Handlers/Commands/ConfirmAiProposedActionCommandHandler.cs` | New | Application | Confirms, executes, and persists proposed-action state. | Fail-closed tenant/user checks; duplicate executed actions do not re-run tools. |
| `Explore.Application/Features/AiAssistant/Handlers/Commands/RejectAiProposedActionCommandHandler.cs` | New | Application | Rejects proposed actions without execution. | Fails closed for wrong tenant/user and invalid states. |
| `Explore.Domain/Ai/AiToolExecution.cs` | Existing | Domain | Execution audit metadata for confirmed tools. | Reuse for confirmation engine; extend only if necessary. |
| `Explore.Application/Contracts/Persistence/IAiConversationRepository.cs` | Existing | Application | AI aggregate repository contract. | Now exposes `GetProposedActionForUpdateAsync` and `UpdateProposedActionAsync` for confirmation transitions. |
| `Explore.Application/Features/Events/Requests/Commands/CreateEventCommand.cs` | Existing | Application | Authorized event create command. | `CreateEventDraft` executor must dispatch this, not write Event repository directly. |
| `Explore.API/Controllers/AiAssistantController.cs` | Existing | API | Authenticated AI API routes. | Add confirm/reject endpoints in Phase 4. |
| `Explore.API/Hateoas/Policies/AiAssistantLinkPolicy.cs` | Existing | API/HAL | AI conversation HAL affordances. | Add proposal confirm/reject links after Application commands exist. |
| `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor` | Existing | Blazor | Placeholder assistant rail. | Functional UI comes after API/HAL/client stability. |
| `docs/semantic_versioning/v1.0.0.md` | Existing | Docs | Roadmap mentions MCP server support. | No current MCP implementation exists. |
| `dev/active/ai-tool-contract-registry/*` | New | Docs | New source of truth for registry/MCP-adapter implementation. | Created in this planning slice. |

## Key Decisions

1. Build an internal AI Tool Contract Registry before adding more tools.
2. Keep `CreateEventDraft` as the first registered tool and preserve Phase 5.1 safety behavior.
3. MCP is an optional adapter over the registry/API/Application boundary, not the core authority.
4. Mutating tools require human confirmation by default.
5. Existing CQRS commands remain authoritative for mutations.
6. Registry emits the same schemas for provider tool calls and future MCP tools to avoid drift.
7. Blazor proposal actions must be HAL-gated; no local role/claim checks for Confirm/Reject.
8. Retention/redaction must be addressed before broad AI history/UI enablement.

## Constraints And Rules To Remember

- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- Domain remains pure; Application cannot depend on API/Blazor/Persistence implementations.
- Tool output from providers and MCP clients is untrusted.
- Confirmed mutations dispatch existing MediatR commands.
- Tenant isolation must fail closed.
- Private AI/MCP surfaces are authenticated even if general GET convention allows anonymous reads elsewhere.
- HAL links are the UI affordance authority.
- New C# files need two `ABOUTME:` comments.
- Use per-project test commands, not solution-level `dotnet test`.

## Validation Baseline

Planning-only docs were created in this slice. Implementation slices should use targeted commands:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --treenode-filter "/*/*/*AiTool*/*|/*/*/*CreateEventDraftAiAction*/*|/*/*/*AiPromptContextBuilderTests*/*" --no-progress --maximum-parallel-tests 1
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

API/Blazor/Persistence/MCP phases add their own project-level verification from the plan and tasks file.

## Current Known Risks / Unknowns

- Registry can become too abstract. Keep Phase 1 minimal and prove it with `CreateEventDraft` only.
- MCP hosting and authentication strategy require current .NET/MCP research in Phase 7.
- Confirm/reject idempotency and event creation are covered at the Application handler level; Phase 4 still needs DB-backed API flow tests to prove duplicate confirm creates exactly one draft.
- Existing unrelated architecture parity failures may obscure registry validation; record separately.
- Old mixed AI migration history may need self-hoster notes or later migration cleanup.
- Old AI task mapping after audit: old Phase 4 reference search maps to new Phase 5; old Phase 5 confirm/create-draft maps to new Phases 2-4; old Phase 6 Blazor panel maps to new Phase 6; old Phase 7 cancellation/streaming/retention/dashboards maps to new Phase 8; old Phase 8 docs/credit/final validation maps to new Phase 8.5 and verification checklist.

## Handoff Notes

### Handoff — 2026-06-01 Europe/Brussels

- **Current state:** New planning workstream created for AI Tool Contract Registry and future MCP adapter. No production code changed in this planning slice.
- **Next action:** User review. If approved, mark/archive old `dev/active/ai-integration` and begin Phase 1 registry contracts.
- **Blockers:** None for Phase 1; MCP implementation details intentionally deferred to Phase 7 research.
- **Modified files:** `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-plan.md`, `ai-tool-contract-registry-context.md`, `ai-tool-contract-registry-tasks.md`.
- **Validation:** Docs consistency checks should confirm required headers and no placeholder tokens remain.
- **Documentation impact:** Old AI integration docs must later point to this workstream.
- **Risks:** Do not start MCP first; that would duplicate the tool contract boundary this plan is designed to centralize.
- **Notes for next contributor/agent:** Read current `CreateEventDraftAiActionMapper` and parser/prompt factory before editing; preserve Phase 5.1 tests as regression coverage.

### Handoff Audit — 2026-06-01 Europe/Brussels

- **Question answered:** The old AI integration workstream was not archived until verifying that its remaining work was represented here.
- **Result:** Initial registry docs carried the major phases but compressed some old tasks too much. The plan/tasks now explicitly include Plane inspiration credit, Blazor dock/accessibility docs, AI conversation state tests, separate reference picker/proposal card tasks, full panel bUnit tests, cancellation, streaming/polling, advanced provider/run dashboards, and the old final validation matrix.
- **Remaining archive action:** After user approval, update old `dev/active/ai-integration/*` to point here or move it per the project’s active-doc archival convention.

### Implementation Slice — 2026-06-01 Europe/Brussels

- **Current state:** Phase 1.1/1.2 code exists. The registry can expose definitions, find a definition by `AiProposedActionKind`, and validate payload JSON through a shared guard. The guard accepts only JSON objects and uses case-insensitive allow/deny field policies without echoing raw payload content in failure messages.
- **Files added:** `AiToolDefinition.cs`, `AiToolConfirmationMode.cs`, `IAiToolContractRegistry.cs`, `AiToolContractRegistry.cs`, `AiToolValidationResult.cs`, `AiToolExecutionContext.cs`, `AiToolPayloadGuard.cs`, and `Event.Application.UnitTests/Features/AiAssistant/Tools/AiToolPayloadGuardTests.cs`.
- **Behavior preserved:** Existing prompt factory, parser, send-message handler, and `CreateEventDraftAiActionMapper` are not wired to the registry yet; current provider-visible behavior remains unchanged until Phase 1.3.
- **Validation:** AFT diagnostics reported no errors for the new files, but C# LSP is not installed. `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passed with existing warnings. `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --no-progress --maximum-parallel-tests 1` passed: 1197 succeeded. `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 180 succeeded, 1 skipped.
- **Risks:** The old AI integration files were already deleted in the dirty worktree before this slice. If archival pointers are required, add them explicitly in a separate docs-only slice.

### Implementation Slice — 2026-06-01 Europe/Brussels — Phase 1.3

- **Current state:** Prompt schema generation and parser validation now consume the Application-layer AI tool registry. `CreateEventDraft` remains the only default tool definition and the only provider-visible action kind.
- **Files changed:** `AiSystemPromptFactory.cs`, `AiStructuredActionParser.cs`, `SendAiMessageCommandHandler.cs`, `AiToolContractRegistry.cs`, new `CreateEventDraftAiToolDefinition.cs`, plus `AiPromptContextBuilderTests.cs` and `AiStructuredActionParserTests.cs`.
- **Behavior preserved:** Tool proposals still produce the same `CreateEventDraft` schema shape when enabled, no action schema is emitted when proposals are disabled, and send-message still persists proposed actions only without executing tools or creating events.
- **Security/control-flow change:** Untrusted provider payloads now go through `IAiToolContractRegistry.ValidatePayload(...)`, which uses the shared JSON guard to reject unknown/forbidden fields before persistence. The send handler constructs one registry and shares it between prompt construction and parsing to avoid prompt/parser drift in the default path.
- **Tests added/updated:** Prompt tests now cover an empty registry producing no action schema. Parser tests now cover registry-driven validation, forbidden field rejection without raw field echo, and unknown-kind behavior through an empty registry.
- **Validation:** Targeted Application AI tests passed with existing warnings: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity normal -p:RunAnalyzers=false -- --treenode-filter "/*/*/*AiStructuredActionParserTests*/*|/*/*/*AiPromptContextBuilderTests*/*|/*/*/*AiToolPayloadGuardTests*/*|/*/*/*SendAiMessageCommandHandlerTests*/*" --no-progress --maximum-parallel-tests 1`. Full serial Application unit tests passed: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --no-progress --maximum-parallel-tests 1` (1200/1200). `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passed. A later architecture verification rerun during Phase 2 passed: 180 succeeded, 1 skipped.
- **Remaining:** Phase 2.1 remains unchecked because the new definition still needs explicit mapper/authorization metadata decisions and schema/mapper drift tests before it is considered fully migrated.

### Implementation Slice — 2026-06-01 Europe/Brussels — Phase 2.1/2.2

- **Current state:** `CreateEventDraft` is now fully registered as the first governed registry tool. The definition carries provider schema, allowed fields, forbidden fields, confirmation mode, payload mapper type, required authorization resource/action, and provider/MCP exposure flags.
- **Files changed:** `AiToolDefinition.cs`, new `AiToolAuthorizationRequirement.cs`, `CreateEventDraftAiToolDefinition.cs`, `CreateEventDraftAiActionMapper.cs`, `AiSystemPromptFactory.cs`, and new `CreateEventDraftAiToolDefinitionTests.cs`.
- **Behavior preserved:** Existing `CreateEventDraftAiActionMapper` validation behavior remains strict. The send-message flow still only persists proposed actions and does not execute mutations.
- **Safety/control-flow change:** `CreateEventDraftAiActionMapper` now uses `CreateEventDraftAiToolDefinition.AllowedPayloadFields` instead of a private duplicate allow-list, making the registry the single source for accepted provider JSON fields. The tool definition records `ResourceKinds.Event` plus `AuthorizationActions.Create` for the future confirmation/executor boundary.
- **Tests added/updated:** New drift tests parse the JSON schema properties, compare them to the registry allowed fields, verify every allowed field is accepted by the mapper, verify every forbidden field is rejected by the registry guard, and assert mapper/auth/exposure metadata.
- **Validation:** Targeted AI tests passed: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity normal -p:RunAnalyzers=false -- --treenode-filter "/*/*/*CreateEventDraftAiToolDefinitionTests*/*|/*/*/*CreateEventDraftAiActionMapperTests*/*|/*/*/*AiStructuredActionParserTests*/*|/*/*/*AiPromptContextBuilderTests*/*|/*/*/*AiToolPayloadGuardTests*/*|/*/*/*SendAiMessageCommandHandlerTests*/*" --no-progress --maximum-parallel-tests 1`. Application build passed: `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false`. Full serial Application tests passed: 1208/1208. Architecture tests passed: 180 succeeded, 1 skipped.
- **Remaining:** Phase 3 must add confirm/reject Application commands and the first executor path. The executor must dispatch existing `CreateEventCommand` through MediatR and must not write event repositories directly.

### Implementation Slice — 2026-06-01 Europe/Brussels — Phase 3.1/3.2

- **Current state:** Application-level proposed-action confirmation is implemented for the first registry tool. Authenticated users can confirm or reject proposed actions belonging to their own tenant/conversation; not-found, wrong-tenant, and wrong-user paths fail closed as `proposed_action_not_found`.
- **Files changed:** `AuthorizationActions.cs`, `IAiConversationRepository.cs`, `AiConversationRepository.cs`, new confirm/reject commands, new confirm/reject handlers, new `CreateEventDraftAiToolExecutor.cs`, `AiAssistantAuthorizationMetadataTests.cs`, and new `AiProposedActionCommandHandlerTests.cs`.
- **Control flow:** `ConfirmAiProposedActionCommandHandler` loads the proposed action for update, checks tenant and conversation user ownership, treats already executed actions as duplicate-safe success, confirms proposed actions, maps `CreateEventDraft` payloads, and dispatches existing `CreateEventCommand` through MediatR. `RejectAiProposedActionCommandHandler` marks proposed actions rejected without invoking any executor.
- **Safety notes:** The executor does not write event repositories directly. It reuses `CreateEventDraftAiActionMapper` and `CreateEventCommand`, so existing validation, authorization metadata, transaction behavior, cache invalidation, metrics, and future outbox hooks stay on the canonical command path. Organization/group-scoped AI payloads currently fail closed because confirmation uses an empty mapping allow-list until scoped context is wired.
- **Tests added/updated:** Application tests cover unauthenticated confirm, wrong-user fail-closed behavior, confirm dispatching `CreateEventCommand`, duplicate confirm without re-execution, mapping failure without dispatch, reject success, duplicate reject, invalid reject states, and authorization metadata for new commands.
- **Validation:** AFT diagnostics reported no scoped diagnostics/TODOs, with C# LSP unavailable. Application build passed: `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false`. Focused AI/Application tests passed. Full serial Application unit tests passed: 1218/1218. Architecture tests passed: 180 succeeded, 1 skipped.
- **Remaining:** Phase 3.3 still needs safe execution-result metadata/queryability via `AiToolExecution` or a documented decision that current `AiProposedAction` status/result/failure fields are sufficient. Phase 4 must expose confirm/reject through API/HAL/OpenAPI and add DB-backed duplicate-confirm flow tests.
