<!-- ABOUTME: Strategic implementation plan for the AI Context Disclosure Policy.
     ABOUTME: Treats app-authorization and AI-disclosure as distinct permissions with field-level sensitivity. -->

# AI Context Disclosure Policy — Implementation Plan

Last Updated: 2026-06-28 Europe/Brussels

## 0. Planning Metadata

- **Request:** Implement an enterprise-grade AI Context Disclosure Policy: app-authorization and AI-disclosure are distinct permissions. Default-deny PII to AI; field-level sensitivity; AI Context Gateway; consent engine; transcript redaction; narrowed MCP tools.
- **Task directory:** `dev/active/ai-context-disclosure-policy/`
- **Planning status:** **User-reviewed** (Senior CTO approved direction with 8 mandatory corrections — all incorporated).
- **Matched intents:** None. **Fallback Contract** applies (see §4). Task 7.1 proposes `ai-context-disclosure` intent.
- **Skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`, `dotnet-efcore-guidelines`, `error-tracking`, `outbox-pattern`.
- **Layers:** Domain, Application, Persistence, API, Blazor, Docs.
- **Complexity:** **XL**. PII disclosure gated behind Phase 4 (CTO #5).

### CTO Mandatory Corrections (incorporated)
1. Instance-admin AI = aggregate/redacted only; separate later workstream.
2. No broad instance-admin AI in general assistant.
3. MCP audit moved to Phase 1 (Task 1.6); unsafe blocked Phase 2 (Task 2.5).
4. PII classification machine-enforced (reflection test Task 1.5).
5. PII disclosure disabled until Phase 4 (gated flip Task 4.4).
6. Stricter provider trust (evidence-based, not naming); `Unknown`=most restrictive.
7. Policy hierarchy: instance > tenant > user consent (intersection).
8. Architecture test prevents gateway bypass (Task 3.3).

---

## 1. Executive Summary

Server-authoritative AI Context Disclosure Policy between AI orchestrator and data layer. Principle: **normal app authorization ≠ AI disclosure authorization.** PII-split entities (`User/UserPii`, `Organization/OrganizationPii`, `Actor/ActorPii`, `Location/LocationPii`) provide the foundation.

### In scope
1. Field-level sensitivity (`AiContextSensitivity`) + disclosure rules (`AiContextDisclosureRule`), **machine-enforced** registry (CTO #4).
2. AI Context Gateway returning **sanitized disclosure envelope** (never raw objects).
3. Policy hierarchy instance ∩ tenant ∩ user consent (CTO #7).
4. Consent engine, precisely scoped (CTO #5).
5. Transcript hygiene as part of disclosure boundary; PII gated on Phase 4 (CTO #5).
6. Narrowed MCP tools with full metadata; audited early (CTO #3).
7. Privacy settings + JIT consent + admin deployment modes.
8. Docs (`AI_CONTEXT_SECURITY.md`, ADR-006).
9. Architecture tests: PII-completeness (1.5) + bypass-prevention (3.3).

### Out of scope
- Replacing Keycloak/Cerbos. Building local LLM runtime.
- **Broad instance/tenant admin AI in general assistant** (CTO #1/#2) — separate `AiAdministrativeContextScope` workstream.
- Event-context rich retrieval (downstream consumer; see b1–b12).
- Federation cross-tenant AI. Full DSAR automation.

---

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log
| Claim | Evidence | Confidence |
|---|---|---|
| PII entities exist | `Verified: Explore.Domain/{UserPii,OrganizationPii,ActorPii,LocationPii}.cs` | High |
| AI flow = flat summary only | `(b7)` `ProcessAiRunCommandHandler.BuildSelectedReferenceContextAsync` → `GetEventDetailsRequest` | High |
| `AiSafeDataContextRegistry` = flat allow-list (seed) | `(b2)` 1 context kind, 8 fields, fail-closed | High |
| Minimal DTOs intentional | `(b3)` `AiSelectedReferenceDto` 4 fields | High |
| No gateway/consent/sensitivity enum | `Not found` in `Features/AiAssistant/**` | High |
| No instance-admin in actor service | `(b12)` user/org/group actors only | High |
| MCP tools unaudited | `Explore.API/Mcp/AiAssistantMcpTools.cs` exists | Medium |

### 2.2 Existing Implementation
- **Domain:** PII-split aggregates; rich event domain (`Event`, `EventSession`, `EventSessionSpeaker`, `EventSessionLanguage`, `EventRegistration`); enums (`EventStatusEnum`, `VisibilityTypeEnum`, `EventSessionStatusEnum`).
- **Application (`Features/AiAssistant/`):** `Context/AiSafeDataContextRegistry*` (seed); `Prompting/AiReferencePromptPacker` (500 chars/ref, 2k total); `Prompting/AiPromptContextBuilder`; `Handlers/Commands/{SendAiMessage,ProcessAiRun}CommandHandler`; `Actors/AiAssistantActorContextService` (no instance-admin).
- **API:** `Mcp/AiAssistantMcpTools.cs` (unaudited); `Controllers/AiAssistantController.cs`.
- **Blazor:** `Components/Shell/AiAssistantRail.razor`; no privacy UI.
- **Persistence:** no consent/audit tables.

### 2.3 Tests
`Event.Architecture.Tests/` (agent schemas); `Event.Application.UnitTests/`. No AI disclosure/consent/redaction tests. Greenfield.

### 2.4 Docs
`docs/SECURITY-MODEL.md`, `AUTHORIZATION.md`, `AI_RAG_FOUNDATION.md`, `AI_AGENT_CONTRACT_INVENTORY.md`, `AGENTS.md`. Proposed new: `AI_CONTEXT_SECURITY.md`, ADR-006.

### 2.5 Pain Points
No AI disclosure boundary; flat allow-list too coarse; no consent; PII leakage in transcripts; admin boundary unenforced; small token budgets; MCP unaudited; no audit trail.

### 2.6 Unknowns
Exact PII fields (Task 1.1); provider trust metadata (Task 1.3/3.1); conversation persistence shape (Phase 4); Cerbos wiring (Phase 1); MCP tool breadth (Task 1.6).

---

## 3. Proposed Future State

### 3.1 Architecture
```
User prompt → AI Orchestrator → AI Context Gateway → Sanitized Envelope → LLM provider → Tagged conversation store
Gateway checks: (1) app-authz, (2) subject/consent + policy hierarchy, (3) disclosure policy + provider trust
```

### 3.2 Classification Model
```csharp
enum AiContextSensitivity { Public, InternalOperational, PersonalNonPii, Pii, SensitivePersonal, Secret, Credential, CrossUserPersonal, TenantBusinessConfidential }
enum AiContextDisclosureRule { Allow, AllowIfCurrentUser, RequireJustInTimeConsent, RequireTenantPolicyAndConsent, AggregateOnly, LocalModelOnly, Deny }
```
**Provider Trust Tiers (CTO #6 — evidence-based):**
```csharp
enum AiProviderTrustTier { LocalInProcessOrSameNetworkModel, TenantControlledPrivateEndpoint, TenantConfiguredExternalProcessor, PlatformConfiguredExternalProcessor, Unknown }
```
`Unknown` = most restrictive. `LocalModelOnly` requires config evidence, not naming.

Representative policies: `UserPii.Email`=Pii/RequireJustInTimeConsent; `AccessToken`=Credential/Deny; other users' `UserPii.*`=CrossUserPersonal/Deny; public event title=Public/Allow; counts=InternalOperational/AggregateOnly.

### 3.3 Policy Hierarchy (CTO #7)
Final decision = instance policy ∩ tenant policy ∩ user consent (most restrictive wins). User consent never overrides instance/tenant.

### 3.4 Disclosure Flow
App-authorized? → subject check (cross-user=Deny; own-PII=consent+hierarchy; aggregate=counts only) → provider tier allows? → hierarchy intersection = allow? → disclose + audit.

### 3.5 Consent Record (CTO #5)
user, tenant, field/field-group, purpose, provider trust tier, concrete provider id, expiry, one-time vs persistent, conversation/run id, revoked timestamp, policy version.

### 3.6 Transcript Hygiene (CTO #5 — part of disclosure boundary)
`MaxSensitivity` column; redacted logging; deletion propagation; references over values. **PII disclosure disabled until this exists.**

### 3.7 MCP Narrowing (CTO #3)
Each tool declares: `maxSensitivity`, `requiresCurrentUser`, `requiresConsent`, `returnsPii`, `purpose`, `allowedProviderTiers`, `auditCategory`, `allowedActorScopes`, `returnsAggregateOnly`. Routes through gateway.

### 3.8 Administrative AI (separate workstream — CTO #1/#2)
`AiAdministrativeContextScope { InstanceAggregate, TenantAggregate, OperationalDiagnostics }` — aggregate/redacted only, never row-level PII. NOT in general assistant.

### 3.9 Admin Deployment Modes (CTO #6)
AI disabled / public-internal only / own-PII consent / local-model-only sensitive / external-PII disabled.

---

## 4. Non-Negotiable Constraints (Fallback Contract)
- Repos return entities, never DTOs. Validators manual. IDs int/Guid/long. GET AllowAnonymous, write Authorize. ABOUTME headers. HAL links = affordance source. Clean Architecture. Tenant isolation API-authoritative. File-scoped namespaces.
- **Server policy = boundary; UI = convenience.**
- **Default-deny PII.** Disclosure needs rule + hierarchy intersection + (own PII) consent.
- **No PII field without explicit classification** (CTO #4).
- **No PII disclosure until transcript hygiene** (CTO #5).
- **Provider trust requires config evidence** (CTO #6).
- **Policy hierarchy: instance > tenant > user** (CTO #7).
- Pre-v1: no compat shims.

---

## 5. Architecture Decisions
- **5.1** Promote `AiSafeDataContextRegistry` → field-level disclosure registry (reuse seed).
- **5.2** Gateway returns sanitized envelope (`AllowedFields, DeniedFields, SanitizedPayload, MaxSensitivity, ConsentIds, ProviderTrustTier, AuditId, PolicyVersion`) — never raw objects. Packer only renders approved fields.
- **5.3** Consent = first-class Domain entity, precise scoping.
- **5.4** Provider trust = evidence-based (CTO #6).
- **5.5** Policy hierarchy in domain model (instance ∩ tenant ∩ user).
- **5.6** Transcript hygiene = part of disclosure boundary (CTO #5).
- **5.7** Machine-enforced PII classification via reflection test (CTO #4).
- **5.8** Gateway-bypass prevention architecture test (CTO #8).
- **5.9** Admin AI = aggregate/redacted, separate workstream (CTO #1/#2).
- **5.10** MCP audit early (CTO #3).

---

## 6. Implementation Phases

> **Gating rule (CTO #5):** Phase 3 = public/internal ONLY. PII disclosure disabled until Phase 4 complete.

### Phase 1: Foundations — Classification + Registry + MCP Audit + Docs
- **Goal:** Declarative policy, machine-enforced classification, MCP inventory, docs.
- **Acceptance:** enums compile; registry seeded; **reflection test: every `*Pii` property classified**; MCP tools inventoried; docs published.
- **Verification:** build; `dotnet test --project Event.Architecture.Tests`; docs-lint.
- **Tasks:**
  - **1.1** Read PII entities + security docs → `field-classification-matrix.md` (EVERY PII property classified, default Deny). Effort M.
  - **1.2** Create enums (`AiContextSensitivity`, `AiContextDisclosureRule`, `AiProviderTrustTier` stricter, `AiAdministrativeContextScope`). Effort S.
  - **1.3** Create `AiContextDisclosureRegistry` + seed (fail-closed). Effort M.
  - **1.4** Write `docs/AI_CONTEXT_SECURITY.md` + ADR-006 (incl. hierarchy, provider-evidence, admin modes). Effort M.
  - **1.5** Architecture test: registry schema + **PII-completeness reflection test** (CTO #4). Effort M.
  - **1.6** MCP tool audit → `mcp-tool-audit.md` (safe/unsafe/unknown) (CTO #3). Effort M.

### Phase 2: Gateway + Consent Domain + Block Unsafe MCP
- **Goal:** Gateway chokepoint (sanitized envelope), consent entity/repo/handlers, block unsafe MCP.
- **Acceptance:** gateway returns envelope; three checks; policy hierarchy enforced; consent precise; unsafe MCP blocked.
- **Tasks:**
  - **2.1** `AiDataConsent` entity (precise scoping §3.5) + repo + migration. Effort L.
  - **2.2** `IAiContextGateway` + impl returning `AiDisclosureEnvelope` (policy hierarchy intersection). Effort L.
  - **2.3** Consent handlers (grant/revoke/evaluate) + manual validators. Effort M.
  - **2.4** `AiDisclosureAudit` + repo; gateway writes per decision. Effort M.
  - **2.5** Block unsafe MCP tools behind feature flags (CTO #3). Effort M.

### Phase 3: Wire Gateway + Bypass-Prevention (public/internal ONLY)
- **Goal:** Route AI context through gateway; bypass-prevention test. **PII DISABLED.**
- **Tasks:**
  - **3.1** Provider trust tier from config evidence (CTO #6). Effort M.
  - **3.2** Route `BuildSelectedReferenceContextAsync` through gateway (PII denied). Effort M.
  - **3.3** Bypass-prevention architecture test (CTO #8): `Features/AiAssistant/**` + `Explore.API/Mcp/**` no direct PII/broad repo deps except via gateway. Effort M.
  - **3.4** Extend `AiReferencePromptPacker` for envelope-approved rich context. Effort M.

### Phase 4: Transcript Hygiene + Deletion Propagation (PREREQUISITE for PII)
- **Goal:** `MaxSensitivity` tagging, redaction, deletion propagation. **PII enabled only after this.**
- **Tasks:**
  - **4.1** `MaxSensitivity` column on conversations/messages. Effort M.
  - **4.2** Logging redaction middleware. Effort M.
  - **4.3** Deletion propagation hook (user-delete → redact AI traces). Effort L.
  - **4.4** **Enable PII disclosure (gated flip)** — only after 4.1–4.3 verified (CTO #5). Effort S.

### Phase 5: MCP Narrowing + Full Metadata
- **Tasks:**
  - **5.1** Tool metadata declaration (`maxSensitivity`, `requiresCurrentUser`, `requiresConsent`, `returnsPii`, `purpose`, `allowedProviderTiers`, `auditCategory`, `allowedActorScopes`, `returnsAggregateOnly`) + gateway routing. Effort L.
  - **5.2** Replace broad tools with narrow ones. Effort L.

### Phase 6: UI + JIT Consent + Admin Modes
- **Tasks:**
  - **6.1** Settings panel + admin deployment modes (Blazor). Effort M.
  - **6.2** JIT consent modal with external-provider data-leave warning. Effort M.

### Phase 7: Hardening + Docs + Intent
- **Tasks:**
  - **7.1** Propose `ai-context-disclosure` intent. Effort S.
  - **7.2** E2E disclosure suite (5 scenarios + policy-hierarchy intersection). Effort L.
  - **7.3** Finalize docs + cross-links. Effort S.

---

## 7. Testing Strategy
- Registry fail-closed + seed (Unit).
- **PII-completeness reflection** (Architecture, Task 1.5).
- **Gateway-bypass prevention** (Architecture, Task 3.3).
- Gateway three-check + envelope (Unit+Integration).
- **Policy hierarchy intersection** (Unit).
- **Provider trust evidence** (Unit).
- Consent grant/revoke/evaluate (Unit).
- Transcript redaction (Unit). Deletion propagation (Integration).
- MCP tool metadata + routing (Unit). Settings/consent UI (bUnit).
- E2E disclosure scenarios (Integration).

## 8. Documentation/Config/Ops Impact
New: `AI_CONTEXT_SECURITY.md`, ADR-006. Updated: `docs/index.md`, `AGENTS.md`, `AI_RAG_FOUNDATION.md`, `OPERATIONS.md`. Config: provider trust evidence, admin modes. Ops: audit table, retention, deletion runbook.

## 9. Security/Authz/Privacy
Gateway reuses Keycloak/Cerbos. Tenant-scoped audit. Default-deny PII. Policy hierarchy. Admin escalation blocked. HAL-gated UI.

## 10. Multi-Tenancy/Federation/L10n/A11y/Product
Multi-Tenancy: Applicable (consent/audit tenant-scoped; policy hierarchy tenant layer). Federation: Needs Investigation (out of scope). Localization: Applicable. Accessibility: Applicable (WCAG consent modal). Product: Applicable (self-hostable provider trust + admin modes).

## 11. Observability
Redacted logging; disclosure metrics (allow/deny by sensitivity, hierarchy denials); gateway trace span; health checks.

## 12. Migration/Compatibility
Migrations: `AiDataConsent`, `AiDisclosureAudit`, `MaxSensitivity` columns. Seed: registry in code. Sequence: Phase 1→2→3 (public/internal)→**4 (transcript; then PII enabled)**→5→6→7. No compat shims.

## 13. Risk Register
| Risk | Mitigation | Detection | Owner |
|---|---|---|---|
| Unclassified PII leaks | Reflection test 1.5; fail-closed | Build failure | 1.1, 1.5 |
| Gateway bypass | Bypass-prevention test 3.3 | Arch test failure | 3.3 |
| PII before transcript hygiene | Gated flip 4.4 | Flag check | 4.4 |
| Provider trust misconfigured | Evidence-based 3.1; Unknown=restrictive | Config audit | 3.1 |
| Policy hierarchy bypass | Intersection logic 5.5 + tests | Unit failure | 2.2, 7.2 |
| Transcript PII persists | Redaction + deletion 4.2/4.3 | Log inspection | 4.2, 4.3 |
| Consent UX overpromises | External warning; safe defaults | UX feedback | 6.2 |

## 14. Success Metrics / DoD
Functional: rich policy-approved context; no cross-user PII (even admins); credentials denied; admin=aggregates; hierarchy enforced. Quality: build + Application/Architecture tests (incl. PII-completeness + bypass-prevention) + integration suite + bUnit. Docs: docs-lint + published. Validation: manual smoke (5 scenarios + hierarchy deny).

## 15. Implementation Agent Contract
Read plan+context+tasks before slices. Update all three after meaningful work. Developer-teaching summaries. On failure: update context with root cause. Refresh docs before handoff/PR.

## 16. Progress Reporting Contract
Implemented (teaching summary) / Verified / Remaining / Next / Docs updated.

## 17. Potential Risks & Unknowns
Three load-bearing guarantees: (1) PII-completeness reflection test (1.5) — one unclassified property = leak; (2) bypass-prevention test (3.3) — one direct repo call defeats boundary; (3) PII gating on Phase 4 (4.4) — enabling PII before transcript hygiene creates second PII store. Secondary: provider trust evidence — weak validation lets tenants label external as "local". Policy-hierarchy intersection must be tested with "instance denies despite tenant+user allow" cases.
