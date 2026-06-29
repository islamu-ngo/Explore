<!-- ABOUTME: Pre-Phase-5 audit of every MCP surface in Explore.API/Mcp/ for unsafe disclosures. -->
<!-- ABOUTME: Documents current data exposure per tool/resource/prompt and the Phase 5 narrowing plan. -->

# MCP Tool/Resource/Prompt Disclosure Audit

**Status:** Pre-Phase-5 baseline (after Phase 1 foundations land, before the `IAiContextGateway` exists).
**Last Updated:** 2026-06-28
**Scope:** `Explore.API/Mcp/AiAssistantMcpTools.cs`, `AiAssistantMcpResources.cs`, `AiAssistantMcpPrompts.cs` (361 lines total).
**Audited by:** AI Context Disclosure workstream (Task 1.6).

---

## 1. Summary

The MCP adapter surface for the AI assistant is **already narrow by design** — it never returns raw message content, raw proposed-action payloads, PII entity rows, or broad repository projections. There is exactly **one** Phase-4-sensitive field that the Phase 5 gateway must sanitize before emission: `AiMcpReferenceDescriptor.Summary`.

| Surface | Count | Current Disclosure Risk | Phase 5 Action |
|---|---|---|---|
| Tools | 1 (`propose_ai_tool_action`) | **None** — opaque payload, MediatR-gated. | None required (architecture test Task 3.3 will lock this in). |
| Resources | 2 (`ai_conversations`, `ai_conversation_detail`) | **Low** — one Phase-4-sensitive field (`References.Summary`). | Add `IAiContextGateway.SanitizeReferences(...)` call in `MapDetail`. |
| Prompts | 2 (`create_event_draft_with_confirmation`, `manage_event_with_confirmation`) | **None** — pure static instruction strings. | None required. |

**Verdict:** No unsafe disclosures today. No urgent remediation. Phase 5 (Task 5.1) implements the single narrowing item before Task 4.4 flips PII disclosure on.

---

## 2. Tool — `propose_ai_tool_action`

**File:** `Explore.API/Mcp/AiAssistantMcpTools.cs` (108 lines).
**Authorization:** `[Authorize(Policy = McpAuthorizationPolicies.Propose)]`.
**Character:** Write, non-destructive, non-idempotent, `OpenWorld = false`.

### Parameters
| Name | Type | Disclosure Treatment |
|---|---|---|
| `conversationId` | `Guid` | Opaque identifier; no PII. |
| `toolName` | `string` | AI-supplied schema name; not a data sink. |
| `payloadJson` | `string` | **Opaque to the MCP layer** — passed straight to MediatR (`ProposeAiToolActionCommand`). |
| `summary` | `string?` | Caller-supplied human hint; not persisted as PII-bearing context. |
| `cancellationToken` | `CancellationToken` | — |

### Data flow
MCP handler → `IMediator.Send(ProposeAiToolActionCommand)` → application-layer handler applies HAL/tool-contract gating → returns `AiMcpCommandResultDescriptor`.

### Result shape (`AiMcpCommandResultDescriptor`)
`Success`, `Id?`, `Message?`, `FailureCode?`, `Errors?` — all status metadata. No entity body. Serialized via `AiToolRegistryMcpJsonContext.Default.AiMcpCommandResultDescriptor` (source-generated `JsonSerializerContext` — type-safe, no reflection leak).

### Telemetry
`McpAdapterTelemetry.StartToolCall / MarkSuccess / MarkFailure / MarkCancelled / RecordToolCall` with `projected: false`. Standard observability; no payload logged.

### Disclosure verdict
**SAFE.** The tool never reads PII. All write semantics are funneled through the existing application-layer MediatR gate (`ProposeAiToolActionCommandHandler`), which already enforces tool-contract validation, conversation ownership, tenant scoping, and idempotency.

---

## 3. Resource — `ai_conversations`

**File:** `Explore.API/Mcp/AiAssistantMcpResources.cs` (lines ~30-110).
**Authorization:** `[Authorize(Policy = McpAuthorizationPolicies.Read)]`.
**UriTemplate:** `islamu-event://ai/conversations`.
**Query:** `IMediator.Send(GetAiConversationListQuery { Limit = 10 })`.

### Result shape (`AiMcpConversationSummaryDescriptor`)
| Field | Disclosure Treatment |
|---|---|
| `Id` | Conversation GUID; not PII. |
| `Status` | Enum string. |
| `Title?` | User-set conversation title. Bounded; treated as user-authored content (subject to Phase-4 transcript hygiene, but not PII itself). |
| `Provider?` | Provider name. |
| `ModelId?` | Model identifier string. |
| `LastMessageSequence` | Integer. |
| `CreatedAt`, `UpdatedAt?` | Timestamps. |

### Disclosure verdict
**SAFE.** The descriptor is intentionally metadata-only — no message bodies, no PII entity rows, no transcript content. The application-layer handler (`GetAiConversationListQueryHandler`) already filters by `UserId` from the principal; the MCP layer adds no further widening.

---

## 4. Resource — `ai_conversation_detail`

**File:** `Explore.API/Mcp/AiAssistantMcpResources.cs` (lines ~110-195).
**Authorization:** `[Authorize(Policy = McpAuthorizationPolicies.Read)]`.
**UriTemplate:** `islamu-event://ai/conversations/{conversationId}`.
**Query:** `IMediator.Send(GetAiConversationDetailQuery { ConversationId })`.

### Top-level shape (`AiMcpConversationDetailDescriptor`)
`Found` + the same metadata as the summary descriptor + four child arrays: `Messages`, `Runs`, `References`, `ProposedActions`.

### 4.1 `Messages` — `AiMcpMessageDescriptor`
| Field | Disclosure Treatment |
|---|---|
| `Id`, `Sequence` | Identifiers. |
| `Role` | Enum string. |
| `CreatedAt` | Timestamp. |
| `HasContent` | **Boolean only** — message `Content` is NEVER exposed. |

**Verdict:** SAFE. The author of this descriptor deliberately replaced the content with a presence flag so the MCP surface cannot leak user/assistant prompts. This is a model pattern for any future MCP resource.

### 4.2 `Runs` — `AiMcpRunDescriptor`
| Field | Disclosure Treatment |
|---|---|
| `Id`, `Status`, `Provider`, `ModelId` | Metadata. |
| `QueuedAt`, `StartedAt?`, `CompletedAt?` | Timestamps. |
| `FailureCode?` | Enum string. |

**Verdict:** SAFE. Operational metadata only.

### 4.3 `References` — `AiMcpReferenceDescriptor` ⚠️ **PHASE-4-SENSITIVE**
| Field | Disclosure Treatment |
|---|---|
| `Id`, `Kind`, `ReferenceId` | Identifiers. `Kind` is the reference category (Event/Actor/Organization); `ReferenceId` is the target GUID. |
| `DisplayName` | Bound to `AiSelectedReferenceDto.DisplayName` (typically `Event.Title`). Not PII. |
| `Summary?` | **RISK.** Bound to `AiSelectedReferenceDto.Summary`, which today is built by `ProcessAiRunCommandHandler.BuildEventReferenceSummary` from `EventDto` (status/format/visibility/host/dates/timezone/sessionCount — all `Public`/`Internal` per the field-classification-matrix). Safe **today**. **Becomes risky at Task 4.4** once PII disclosure is enabled, because the persisted `Summary` could then legally contain `Restricted` data per the matrix (e.g. event owner's contact email in local-model mode). The MCP layer currently has no redaction pass. |
| `CreatedAt` | Timestamp. |

**Verdict:** **Phase-5 mitigation required.** Before Task 4.4 flips `PiiDisclosureEnabled`, the MCP `MapDetail` mapper must invoke `IAiContextGateway.SanitizeReferences(references, viewerPrincipal, providerTrustTier)` so that any persisted `Summary` content is downgraded to what the MCP-level principal is permitted to see. This is recorded as Phase 5 Task 5.1.

### 4.4 `ProposedActions` — `AiMcpProposedActionDescriptor`
| Field | Disclosure Treatment |
|---|---|
| `Id`, `Kind`, `Status`, `ResultResourceId?`, `FailureCode?`, `CreatedAt` | Metadata only. |
| `PayloadJson` | **Intentionally omitted** by the descriptor. The full payload lives only in the application/UI confirmation flow (`ConfirmAiProposedActionCommandHandler`). |

**Verdict:** SAFE. The omission is deliberate and documented in the file's ABOUTME comment.

### 4.5 NotFound path
When the underlying query returns null, the mapper returns a descriptor with `Found = false` and **empty arrays** for all four child collections. No PII, no surrogate data.

---

## 5. Prompts

**File:** `Explore.API/Mcp/AiAssistantMcpPrompts.cs` (58 lines).
**Authorization:** Both prompts `[Authorize(Policy = McpAuthorizationPolicies.Propose)]`.

### 5.1 `create_event_draft_with_confirmation`
Static instruction string describing the workflow:
`list_ai_tool_contracts` → projected `propose_create_event_draft` (or fallback `propose_ai_tool_action`) → treat returned `Id` as pending → wait for user/UI confirmation.

**Explicit safety constraints baked into the prompt text:**
- Do NOT request or emit tenant id, provider, api key, secrets, raw provider responses, or transcripts.

**Verdict:** SAFE. The prompt is pure guidance — it never embeds runtime data.

### 5.2 `manage_event_with_confirmation`
Static instruction string describing the workflow:
read `event_management_context` resource → use HAL actions + `concurrencyStamp` → aspect-specific proposal tools → treat all as pending.

**Explicit safety constraints baked into the prompt text:**
- Do NOT request or emit tenant/actor/provider, secrets, transcripts, outbox, audit, raw `concurrencyStamp`.
- No role/claim-based permission inference (clients must gate affordances by HAL `_links` only).
- Proposal tools only.

**Verdict:** SAFE.

---

## 6. Authorization Survey

All five MCP surfaces are gated by `McpAuthorizationPolicies`:
| Surface | Policy |
|---|---|
| `propose_ai_tool_action` | `Propose` |
| `ai_conversations` | `Read` |
| `ai_conversation_detail` | `Read` |
| `create_event_draft_with_confirmation` | `Propose` |
| `manage_event_with_confirmation` | `Propose` |

The `Read` and `Propose` policies enforce an authenticated tenant principal with the appropriate scope. No anonymous surfaces. The application-layer MediatR handlers add user/conversation ownership checks on top.

---

## 7. Bypass-Prevention (Phase 3, Task 3.3)

Per CTO correction #8, the architecture test `AiContextGatewayBypassTests` will assert that `Explore.API/Mcp/**` and `Explore.Application/Features/AiAssistant/**` have **no direct dependencies** on:
- `Explore.Domain` PII entities (`UserPii`, `OrganizationPii`, `ActorPii`, `LocationPii`).
- Broad repository interfaces (`IUserRepository`, `IOrganizationRepository`, etc.) except via `IAiContextGateway`.

Current state: **No bypass paths exist today.** The MCP layer depends only on MediatR queries/commands and source-generated JSON serializers. The architecture test will lock this in.

---

## 8. Phase 5 Narrowing Plan

| Item | Source | Action | Task |
|---|---|---|---|
| 8.1 Sanitize `References.Summary` | §4.3 | Add `IAiContextGateway.SanitizeReferences(...)` invocation in `AiAssistantMcpResources.MapDetail` before emitting `AiMcpReferenceDescriptor`. | 5.1 |
| 8.2 Gateway telemetry | §2 telemetry | Add `gateway_invocation` events to `McpAdapterTelemetry` so disclosure decisions are observable. | 5.2 |
| 8.3 Optional: `HasProposedActionPayload` flag | §4.4 | No change required today. If downstream tooling wants a presence flag without the payload, add `bool HasPayload` to `AiMcpProposedActionDescriptor`. | 5.3 (optional) |
| 8.4 Lock bypass prevention | §7 | Implement `AiContextGatewayBypassTests` (Phase 3 Task 3.3) covering MCP + AI-assistant namespaces. | 3.3 |

---

## 9. Risks if PII Disclosure Is Enabled Without Phase 5

If Task 4.4 (`PiiDisclosureEnabled = true`) is flipped before Task 5.1 lands, a **single regression path** opens:

1. User asks AI assistant about an event they own in local-model mode.
2. `ProcessAiRunCommandHandler.BuildSelectedReferenceContextAsync` queries the rich context and (post-Phase-4) may embed `Restricted`/`Confidential` data in the reference `Summary` per the matrix.
3. That `Summary` is persisted on `conversation.References`.
4. A subsequent MCP `ai_conversation_detail` resource read emits the persisted `Summary` verbatim through `AiMcpReferenceDescriptor.Summary` — **with no provider-trust-tier or principal re-evaluation**.

Mitigation ordering is enforced by the task graph: **Task 5.1 is a hard prerequisite of Task 4.4.** The architecture test added in Phase 3 (Task 3.3) plus the gateway will close this path before the flip.

---

## 10. Cross-References

- `Explore.API/Mcp/AiAssistantMcpTools.cs`, `AiAssistantMcpResources.cs`, `AiAssistantMcpPrompts.cs` — source.
- `dev/active/ai-context-disclosure-policy/field-classification-matrix.md` — sensitivity source of truth.
- `dev/active/ai-context-disclosure-policy/ai-context-disclosure-policy-tasks.md` — Phase 5 task graph.
- `docs/AI_CONTEXT_SECURITY.md` — gateway contract preview (§9).
- `docs/adr/ADR-012-ai-context-disclosure-policy.md` — ADR with bypass-prevention rules.
- Compressed block b37 — raw file reads this audit was derived from.
