<!-- ABOUTME: Tactical task checklist for the AI Context Disclosure Policy workstream.
     ABOUTME: CTO-reviewed; PII disclosure gated on Phase 4; MCP audit in Phase 1. -->

# AI Context Disclosure Policy — Task Checklist

Last Updated: 2026-06-28 Europe/Brussels

## Status Summary
- **Overall status:** User-reviewed (Senior CTO approved with 8 mandatory corrections — all incorporated)
- **Completed:** 0/27
- **Current priority:** Begin Phase 1
- **Next recommended slice:** Phase 1 (Foundations)

## CTO Mandatory Corrections (all incorporated)
1. Instance-admin AI = aggregate/redacted only (separate workstream). 2. No broad instance-admin AI. 3. MCP audit Phase 1. 4. PII classification machine-enforced. 5. PII gated on Phase 4. 6. Stricter provider trust (evidence). 7. Policy hierarchy instance>tenant>user. 8. Bypass-prevention architecture test.

## Implementation Maintenance Rules
- [ ] Before starting work, read plan/context/tasks.
- [ ] After each completed task, update this checklist immediately.
- [ ] If implementation changes scope/architecture, update the plan before continuing.
- [ ] If discoveries affect future work, update the context file.
- [ ] Final summary: Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Baseline
- [x] **0.1 Senior CTO reviews plan and approves/corrects scope.** ✅ (8 corrections incorporated)
- [ ] **0.2 Implementation agent confirms current repo state before first edit.**
  - Acceptance: no stale assumptions; PII entities re-verified.

## Phase 1: Foundations — Classification + Registry + MCP Audit + Docs ⏳ NOT STARTED
- [ ] **1.1 Read PII entities + security docs → field-classification matrix**
  - Files: `Explore.Domain/{UserPii,OrganizationPii,ActorPii,LocationPii}.cs`; `docs/SECURITY-MODEL.md`; `docs/AUTHORIZATION.md`.
  - Acceptance: `field-classification-matrix.md` covers ALL PII properties; admin-boundary quotes; EVERY PII property classified (default Deny).
  - Effort: M | Deps: 0.2
- [ ] **1.2 Create enums (sensitivity, disclosure rule, provider trust tier, administrative scope)**
  - Files (new): `Explore.Domain/Enums/AiContextSensitivityEnum.cs`, `AiContextDisclosureRuleEnum.cs`, `AiProviderTrustTierEnum.cs` (stricter: `LocalInProcessOrSameNetworkModel`, `TenantControlledPrivateEndpoint`, `TenantConfiguredExternalProcessor`, `PlatformConfiguredExternalProcessor`, `Unknown`=most restrictive), `AiAdministrativeContextScopeEnum.cs`.
  - Acceptance: compile; values match plan; Unknown documented restrictive; ABOUTME headers.
  - Effort: S | Deps: 1.1
- [ ] **1.3 Create disclosure registry + seed (machine-readable)**
  - Files (new): `Explore.Application/Features/AiAssistant/Disclosure/AiContextDisclosureRegistry.cs`, `AiContextDisclosureEntry.cs`.
  - Acceptance: compile; fail-closed (unknown→Deny); seeded from matrix.
  - Effort: M | Deps: 1.2
- [ ] **1.4 Write canonical docs (policy + ADR)**
  - Files (new): `docs/AI_CONTEXT_SECURITY.md`, `docs/adr/ADR-006-ai-context-disclosure-policy.md`.
  - Acceptance: render; cross-linked from `docs/index.md`; includes policy hierarchy + provider-evidence + admin modes.
  - Effort: M | Deps: 1.1
- [ ] **1.5 Architecture test — registry schema + PII-classification completeness (CTO #4)**
  - Files (new): `Event.Architecture.Tests/AiContextDisclosureSchemaTests.cs`.
  - Acceptance: (a) registry non-empty/valid/no-dup; (b) reflection: every public persisted property on every `*Pii` entity in registry — unclassified PII fails build.
  - Effort: M | Deps: 1.3
- [ ] **1.6 MCP tool audit (CTO #3 — moved earlier)**
  - Files (existing): `Explore.API/Mcp/AiAssistantMcpTools.cs`, `AiAssistantMcpResources.cs`, `AiAssistantMcpPrompts.cs`.
  - Acceptance: `mcp-tool-audit.md` catalogs every tool; each classified safe/unsafe/unknown; broad tools flagged.
  - Effort: M | Deps: 0.2

## Phase 2: Gateway + Consent Domain + Block Unsafe MCP ⏳ NOT STARTED
- [ ] **2.1 Consent domain entity + repo + migration (precise scoping CTO #5)**
  - Files (new): `Explore.Domain/AiDataConsent.cs` (user, tenant, field/field-group, purpose, provider trust tier, concrete provider id, expiry, one-time vs persistent, conversation/run id, revoked timestamp, policy version); `IAiDataConsentRepository.cs`; EF config + migration.
  - Acceptance: compile; migration applies; tenant filter.
  - Effort: L | Deps: 1.2
- [ ] **2.2 Gateway interface + implementation (sanitized envelope — CTO Phase 2)**
  - Files (new): `IAiContextGateway.cs`, `AiContextGateway.cs`, `AiDisclosureRequest.cs`, `AiDisclosureEnvelope.cs` (`AllowedFields, DeniedFields, SanitizedPayload, MaxSensitivity, ConsentIds, ProviderTrustTier, AuditId, PolicyVersion`).
  - Acceptance: three checks; policy-hierarchy intersection (instance ∩ tenant ∩ user); fail-closed; returns envelope never raw objects.
  - Effort: L | Deps: 1.3, 2.1
- [ ] **2.3 Consent command/query handlers**
  - Files (new): `GrantAiConsentCommand`, `RevokeAiConsentCommand`, `EvaluateAiConsentQuery` + handlers + manual validators.
  - Acceptance: work end-to-end; validators manual; cancellation tokens.
  - Effort: M | Deps: 2.1
- [ ] **2.4 Audit trail for disclosure decisions**
  - Files (new): `Explore.Domain/AiDisclosureAudit.cs`; repo; gateway writes per decision.
  - Acceptance: every decision audited; tenant-isolated.
  - Effort: M | Deps: 2.2
- [ ] **2.5 Block unsafe MCP tools behind feature flags (CTO #3)**
  - Acceptance: unsafe/unknown tools disabled by default; flag for testing.
  - Effort: M | Deps: 1.6

## Phase 3: Wire Gateway + Bypass-Prevention (public/internal ONLY — CTO #5) ⏳ NOT STARTED
> **PII disclosure DISABLED.** Public/internal context only.
- [ ] **3.1 Provider trust tier from config evidence (CTO #6)**
  - Files (existing): `AiAssistantSettingGroup`; provider config.
  - Acceptance: evidence-based (not naming); Unknown=most restrictive; safe default.
  - Effort: M | Deps: 1.2
- [ ] **3.2 Route `BuildSelectedReferenceContextAsync` through gateway (PII disabled)**
  - Files (existing): `ProcessAiRunCommandHandler.cs`.
  - Acceptance: no bypass; public context resolves; PII denied.
  - Effort: M | Deps: 2.2, 3.1
- [ ] **3.3 Bypass-prevention architecture test (CTO #8)**
  - Files (new): `Event.Architecture.Tests/AiGatewayBypassPreventionTests.cs`.
  - Acceptance: `Features/AiAssistant/**` + `Explore.API/Mcp/**` no direct PII/broad repo deps except via `IAiContextGateway`; adding direct dep fails build.
  - Effort: M | Deps: 2.2
- [ ] **3.4 Extend `AiReferencePromptPacker` for envelope-approved rich context**
  - Files (existing): `AiReferencePromptPacker.cs`.
  - Acceptance: only envelope AllowedFields rendered; tunable budgets.
  - Effort: M | Deps: 3.2

## Phase 4: Transcript Hygiene + Deletion Propagation (PREREQUISITE for PII — CTO #5) ⏳ NOT STARTED
> **PII disclosure enabled ONLY after this phase (Task 4.4 gated flip).**
- [ ] **4.1 `MaxSensitivity` on conversation persistence**
  - Files (existing): `AiConversation`/`AiMessage` entities + persistence + migration.
  - Acceptance: column present; gateway sets it.
  - Effort: M | Deps: 2.2
- [ ] **4.2 Logging redaction middleware**
  - Files (new): redaction middleware in logging pipeline.
  - Acceptance: PII not in logs; non-PII retained.
  - Effort: M | Deps: 1.3
- [ ] **4.3 Deletion propagation hook**
  - Files (existing/new): user-deletion handler hook + AI trace cleanup.
  - Acceptance: deletion cleans AI traces; auditable.
  - Effort: L | Deps: 4.1
- [ ] **4.4 Enable PII disclosure (gated flip — CTO #5)**
  - Acceptance: flag gated on 4.1–4.3; PII flows only after flip.
  - Effort: S | Deps: 4.1, 4.2, 4.3

## Phase 5: MCP Narrowing + Full Metadata ⏳ NOT STARTED
- [ ] **5.1 Tool metadata declaration + gateway routing**
  - Files (new/existing): tool base/attribute (API) carrying `maxSensitivity`, `requiresCurrentUser`, `requiresConsent`, `returnsPii`, `purpose`, `allowedProviderTiers`, `auditCategory`, `allowedActorScopes`, `returnsAggregateOnly`; enforce gateway routing.
  - Acceptance: all tools declare full metadata; all route via gateway.
  - Effort: L | Deps: 2.2, 1.6
- [ ] **5.2 Replace broad tools with narrow ones**
  - Acceptance: no broad tools; narrow tools tested.
  - Effort: L | Deps: 5.1

## Phase 6: UI + JIT Consent + Admin Modes ⏳ NOT STARTED
- [ ] **6.1 Settings panel + admin deployment modes (Blazor)**
  - Files (new): MudBlazor panel (five toggles + admin modes).
  - Acceptance: five toggles + admin modes; safe defaults; server-validated.
  - Effort: M | Deps: 2.3
- [ ] **6.2 JIT consent modal with external-provider warning (Blazor)**
  - Acceptance: appears on PII request; external warning shown; writes consent.
  - Effort: M | Deps: 6.1, 2.3, 4.4

## Phase 7: Hardening + Docs + Intent ⏳ NOT STARTED
- [ ] **7.1 Propose `ai-context-disclosure` intent**
  - Files (existing): `.claude/contract/intents.yaml`.
  - Acceptance: intent added; architecture tests pass.
  - Effort: S | Deps: all
- [ ] **7.2 E2E disclosure suite (5 scenarios + policy-hierarchy intersection)**
  - Acceptance: public allow / own-PII consent / cross-user deny / admin aggregate / credential deny + instance-denies-despite-tenant+user-allow all pass.
  - Effort: L | Deps: all
- [ ] **7.3 Finalize docs + cross-links**
  - Files (existing): `docs/index.md`, `AGENTS.md`, `docs/OPERATIONS.md`.
  - Acceptance: docs-lint; cross-links valid.
  - Effort: S | Deps: all

## Verification Checklist
- [ ] LSP diagnostics clean for modified files.
- [ ] `dotnet build --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.Application.UnitTests` passes.
- [ ] `dotnet test --project Event.Architecture.Tests` passes (incl. PII-completeness 1.5 + bypass-prevention 3.3).
- [ ] Integration disclosure suite passes (incl. policy-hierarchy intersection 7.2).
- [ ] `/docs-lint` passes (docs changed).
- [ ] **PII-disclosure flag (4.4) only enabled after 4.1–4.3 verified.**
- [ ] Dev docs refreshed with final state.

## Remaining / Deferred Work
- **Administrative AI Context (CTO #1, #2):** separate later workstream using `AiAdministrativeContextScope` (`InstanceAggregate`, `TenantAggregate`, `OperationalDiagnostics`) — aggregate/redacted only, never row-level user PII.
- **Event-context rich retrieval (original bug):** downstream consumer; separate workstream after Phase 3+4. See compressed blocks b1–b12.
- **Federation cross-tenant AI disclosure:** out of scope.
- **Full DSAR automation:** GDPR-aligned but not built here.
