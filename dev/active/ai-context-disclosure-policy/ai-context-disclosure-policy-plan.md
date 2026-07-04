<!-- ABOUTME: Active implementation plan for the AI Context Disclosure Policy workstream. -->
<!-- ABOUTME: Keeps ADR-012, AI_CONTEXT_SECURITY, registry tests, and field matrix aligned. -->

# AI Context Disclosure Policy - Implementation Plan

Last Updated: 2026-07-04 Europe/Brussels

## 0. Planning Metadata

- **Request:** Maintain a source-backed policy for what platform data may be disclosed to AI prompts, transcripts, and MCP tool responses.
- **Task directory:** `dev/active/ai-context-disclosure-policy/`
- **Planning status:** Active support workstream; restored because `.claude/contract/intents.yaml`, `docs/AI_CONTEXT_SECURITY.md`, and ADR-012 reference these files.
- **Matched intent:** `update-ai-context-disclosure`.
- **Primary layers touched:** Domain enums, Application disclosure registry/gateway, API MCP tools, Architecture tests, security docs.
- **Current source of truth:** `docs/AI_CONTEXT_SECURITY.md`, `docs/adr/ADR-012-ai-context-disclosure-policy.md`, and `Explore.Application/Features/AiAssistant/Disclosure/AiContextDisclosureRegistry.cs`.

## 1. Executive Summary

The AI Context Disclosure Policy prevents accidental disclosure of regulated PII to model providers or MCP tool consumers. It does this through a classified registry, a gateway boundary, provider trust tiers, phase gating, and architecture tests. The policy is intentionally restrictive: unregistered fields deny by default, confidential/restricted fields deny outside local-model execution, and PII disclosure remains disabled until the Phase 4 prerequisites are complete.

## 2. Source-Grounded Current State

| Claim | Evidence | Status |
|---|---|---|
| Every current `*Pii` entity property is represented by the registry. | `AiContextDisclosureSchemaTests` reflects `UserPii`, `OrganizationPii`, `ActorPii`, and `LocationPii` against `AiContextDisclosureRegistry.CreateDefault()`. | Guarded |
| AI flows should use a single gateway boundary. | `IAiContextGateway` is registered in `Explore.Application/ApplicationServicesRegistration.cs`; bypass tests inspect AI assistant and MCP types. | Guarded |
| The field matrix is required governance input. | `docs/AI_CONTEXT_SECURITY.md`, ADR-012, and the `update-ai-context-disclosure` intent all reference this active workstream. | Restored |
| PII disclosure is off by default. | `AiAssistantSettingGroup.PiiDisclosureEnabled` defaults to `false`; `MaxAiContextSensitivity` defaults to `1` (`Internal`). | Guarded |

## 3. Target Architecture

The durable pattern is:

1. Domain defines only neutral enums and PII entities.
2. Application owns `AiContextDisclosureRegistry`, `IAiContextGateway`, redaction, consent, and transcript hygiene.
3. API MCP tools and AI assistant handlers request sanitized context envelopes instead of reading PII entities directly.
4. Tests enforce completeness and bypass prevention.
5. Docs and the field matrix explain why each field classification exists.

## 4. Non-Negotiable Constraints

- `IAiContextGateway` is the disclosure boundary for AI prompt and MCP context.
- Unregistered PII fields fail closed.
- `PiiDisclosureEnabled` defaults to `false`.
- `MaxAiContextSensitivity` defaults no higher than `Internal`.
- User consent cannot override instance or tenant deny.
- Provider trust evidence must be explicit; `Unknown` is most restrictive.
- No row-level user PII is exposed through a generic instance-admin AI context.

## 5. Implementation Phases

### Phase 1 - Registry And Matrix Integrity

- Keep `field-classification-matrix.md` synchronized with `AiContextDisclosureRegistry`.
- Keep `AiContextDisclosureSchemaTests` green.
- Update `docs/AI_CONTEXT_SECURITY.md` summary counts whenever the matrix changes.

### Phase 2 - Gateway And Consent Evaluation

- Route AI assistant context construction and MCP tool context through `IAiContextGateway`.
- Model effective disclosure as base classification plus provider trust plus tenant policy plus user consent plus phase flags.
- Preserve fail-closed behavior on missing evidence or gateway errors.

### Phase 3 - Bypass Prevention

- Keep architecture tests that block direct `*Pii` entity dependencies from AI assistant and MCP code.
- Prefer sanitized envelopes over raw entities, repositories, or DTO widening.

### Phase 4 - PII Disclosure Enablement Gate

- Add persistence max-sensitivity metadata, log redaction verification, transcript deletion propagation, and operator docs before enabling PII disclosure.
- Only after those checks pass may an implementation set `PiiDisclosureEnabled` to `true`.

### Phase 5 - MCP Tool Narrowing

- Audit every AI/MCP tool for fields disclosed, provider trust tier, consent status, and denial metadata.
- Ensure MCP responses never include raw PII outside the gateway envelope.

## 6. Verification

- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- Targeted checks: `AiContextDisclosureSchemaTests`, `AiContextGatewayBypassTests`, `GovernanceSettingKeysTests`.
- Readback checks: `rg -n "field-classification-matrix|ai-context-disclosure-policy-plan" docs .claude dev/active`.

## 7. Open Risks

| Risk | Mitigation |
|---|---|
| Matrix and registry drift. | Architecture tests plus this active matrix. |
| AI/MCP code bypasses the gateway. | `AiContextGatewayBypassTests`. |
| Operator enables PII disclosure before hygiene work is complete. | Restrictive defaults, Phase 4 gate, docs, and settings tests. |
| Provider trust is guessed from provider name. | Evidence-based trust tier requirement in ADR-012 and policy docs. |

## 8. Handoff

This file was restored on 2026-07-04 because architecture tests failed when `.claude/contract/intents.yaml` referenced missing active workstream docs. Future changes to AI context disclosure should update this plan, `field-classification-matrix.md`, `mcp-tool-audit.md`, `docs/AI_CONTEXT_SECURITY.md`, and ADR-012 as appropriate.
