<!-- ABOUTME: Operational context memory for the AI Context Disclosure Policy workstream.
     ABOUTME: Tracks session progress, key files, decisions, blockers, and handoffs. -->

# AI Context Disclosure Policy — Context

Last Updated: 2026-06-28 Europe/Brussels

## SESSION PROGRESS (2026-06-28 Europe/Brussels)

### ✅ COMPLETED
- Planning docs created (plan/context/tasks).
- Current-state report completed with evidence (plan §2).
- Codebase verification: PII entities (`UserPii`, `OrganizationPii`, `ActorPii`, `LocationPii`) confirmed in `Explore.Domain/`.
- Existing AI flow mapped (compressed blocks b1–b12): `AiSafeDataContextRegistry`, `AiReferencePromptPacker`, `AiPromptContextBuilder`, `ProcessAiRunCommandHandler.BuildSelectedReferenceContextAsync`, `AiAssistantActorContextService` (no instance-admin concept).
- No existing `ai-context` workstream in `dev/active/`.
- **Senior CTO review received — direction APPROVED with 8 mandatory corrections. All incorporated into plan + tasks.**

### 🟡 IN PROGRESS
- Ready for Phase 1 implementation (plan status: User-reviewed).

### ⏭️ NEXT
1. Implementation agent starts **Task 1.1** (read PII entities + security docs → field-classification matrix).
2. Then Task 1.2 (enums with stricter provider trust), 1.3 (registry), 1.4 (docs), 1.5 (PII-completeness reflection test), 1.6 (MCP audit).
3. Update this context file after each slice.

### ⚠️ BLOCKERS
- **None.** CTO resolved the instance-admin question (see CTO Feedback below): aggregate/redacted only, separate workstream, NOT in general assistant.

## CTO Feedback (2026-06-28) — 8 Mandatory Corrections (all incorporated)

1. **Instance-admin AI = aggregate/redacted only** — confirmed; separate `AiAdministrativeContextScope` workstream later (not general assistant).
2. **No broad instance-admin AI resolution** in the general assistant.
3. **MCP audit moved earlier** — Phase 1 Task 1.6 (inventory + classify); Phase 2 Task 2.5 (block unsafe behind flags); Phase 5 (refactor).
4. **PII field classification machine-enforced** — reflection test Task 1.5: every `*Pii` property MUST be in registry; no unclassified fields.
5. **PII disclosure disabled until transcript hygiene** — Phase 3 = public/internal only; PII enabled only after Phase 4 (Task 4.4 gated flip).
6. **Stricter provider trust semantics** — replaced vague `SelfHosted` with evidence-based tiers: `LocalInProcessOrSameNetworkModel`, `TenantControlledPrivateEndpoint`, `TenantConfiguredExternalProcessor`, `PlatformConfiguredExternalProcessor`, `Unknown` (=most restrictive). Requires config evidence, not naming.
7. **Policy hierarchy explicit: instance > tenant > user consent** — final decision = intersection of all three (most restrictive wins). User consent never overrides instance/tenant.
8. **Architecture tests prevent gateway bypass** — Task 3.3: `Features/AiAssistant/**` + `Explore.API/Mcp/**` may not directly depend on PII/broad repos except via `IAiContextGateway`.

### Additional CTO refinements by phase
- **Phase 2:** Gateway returns sanitized disclosure envelope (`AllowedFields, DeniedFields, SanitizedPayload, MaxSensitivity, ConsentIds, ProviderTrustTier, AuditId, PolicyVersion`) — never raw objects. Consent record precisely scoped (user, tenant, field/field-group, purpose, provider trust tier, concrete provider id, expiry, one-time vs persistent, conversation/run id, revoked timestamp, policy version).
- **Phase 5:** Tool metadata expanded: `purpose`, `allowedProviderTiers`, `auditCategory`, `allowedActorScopes`, `returnsAggregateOnly` (in addition to `maxSensitivity`, `requiresCurrentUser`, `requiresConsent`, `returnsPii`).
- **Phase 6:** UI must show external-provider data-leave warning; admin deployment modes (AI disabled / public-internal only / own-PII consent / local-model-only sensitive / external-PII disabled).

## Quick Resume
1. Read `ai-context-disclosure-policy-plan.md` (§3 future state, §6 phases, §17 risks).
2. Read `ai-context-disclosure-policy-tasks.md`.
3. Start from Task 1.1 unless user overrides.
4. Keep all three dev docs updated after each meaningful slice.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `Explore.Domain/UserPii.cs` | Existing | Domain | User PII fields (email, name, etc.) | Read in Task 1.1 to build matrix. |
| `Explore.Domain/OrganizationPii.cs` | Existing | Domain | Org PII | Read in Task 1.1. |
| `Explore.Domain/ActorPii.cs` | Existing | Domain | Actor PII | Read in Task 1.1. |
| `Explore.Domain/LocationPii.cs` | Existing | Domain | Location PII | Read in Task 1.1. |
| `Explore.Application/Features/AiAssistant/Context/AiSafeDataContextRegistry.cs` | Existing | Application | Flat allow-list — **seed** for disclosure registry | Extend or supersede in Phase 1.3. |
| `Explore.Application/Features/AiAssistant/Context/AiSafeDataContextSummaryPolicy.cs` | Existing | Application | Fail-closed validation | Reuse pattern. |
| `Explore.Application/Features/AiAssistant/Prompting/AiReferencePromptPacker.cs` | Existing | Application | Prompt rendering + token budgets | Extend in Task 3.3. |
| `Explore.Application/Features/AiAssistant/Prompting/AiPromptContextBuilder.cs` | Existing | Application | Prompt assembly chokepoint | `PackSelectedReferences` hook. |
| `Explore.Application/Features/AiAssistant/Handlers/Commands/ProcessAiRunCommandHandler.cs` | Existing | Application | `BuildSelectedReferenceContextAsync` enrichment hook | Route through gateway in Task 3.2. |
| `Explore.Application/Features/AiAssistant/Handlers/Commands/SendAiMessageCommandHandler.cs` | Existing | Application | Resolves acting actor | Unchanged (gateway reuses). |
| `Explore.Application/Features/AiAssistant/Actors/AiAssistantActorContextService.cs` | Existing | Application | Actor resolution (no instance-admin concept) | See blocker note. |
| `Explore.Application/Contracts/Persistence/IEventRepository.cs` | Existing | Application | Event repo (returns entities) | Downstream consumer ref. |
| `Explore.API/Mcp/AiAssistantMcpTools.cs` | Existing | API | MCP tool surface | Audit + narrow in Phase 5. |
| `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor` | Existing | Blazor | Assistant UI | Add settings/consent UI in Phase 6. |
| `Explore.Domain/Enums/AiContextSensitivityEnum.cs` | New | Domain | Sensitivity classification | Task 1.2. |
| `Explore.Domain/Enums/AiContextDisclosureRuleEnum.cs` | New | Domain | Disclosure rules | Task 1.2. |
| `Explore.Domain/Enums/AiProviderTrustTierEnum.cs` | New | Domain | Provider trust tiers | Task 1.2. |
| `Explore.Application/Features/AiAssistant/Disclosure/AiContextDisclosureRegistry.cs` | New | Application | Field-level registry | Task 1.3. |
| `Explore.Application/Features/AiAssistant/Disclosure/IAiContextGateway.cs` | New | Application | Gateway chokepoint | Task 2.2. |
| `Explore.Domain/AiDataConsent.cs` | New | Domain | Consent entity | Task 2.1. |
| `Explore.Domain/AiDisclosureAudit.cs` | New | Domain | Audit trail | Task 2.4. |
| `docs/AI_CONTEXT_SECURITY.md` | New | Docs | Canonical policy doc | Task 1.4. |
| `docs/adr/ADR-006-ai-context-disclosure-policy.md` | New | Docs | ADR | Task 1.4. |

## Key Decisions

1. **Extend `AiSafeDataContextRegistry` into the field-level disclosure registry** (Decision 5.1) — reuse the existing seed rather than building parallel.
2. **AI Context Gateway as Application-layer chokepoint** (5.2) — all AI context fetches route through it.
3. **Consent as a first-class Domain entity** (5.3) — auditable, revocable, deletion-aware.
4. **Provider trust tier as explicit metadata** (5.4) — enables `LocalModelOnly` rules.
5. **Transcript sensitivity tagging + redaction** (5.5) — prevents second PII copies.
6. **Admin AI = aggregate/redacted only** (5.6) — no cross-user PII via general assistant.
7. **Fallback Contract** (no intent matched) — task 7.1 proposes a new `ai-context-disclosure` intent.

## Constraints And Rules To Remember

- Repositories return entities, never DTOs (map in handlers/projections).
- Validators manually instantiated in handlers.
- IDs: `int` lookups, `Guid` (UUIDv7) aggregates, `long` cursors.
- GET `[AllowAnonymous]`, write `[Authorize]`.
- Two-line `ABOUTME:` headers on every new file.
- HAL links = UI affordance source of truth.
- Clean Architecture: Domain → Application → Persistence/Infra → API/Blazor.
- Tenant isolation API-authoritative (global query filters); only `[QueryFilterNames.SoftDelete]` may be ignored when justified.
- **Server-side policy is the security boundary — UI toggles are convenience.**
- **Default-deny for PII.** Disclosure needs explicit rule + (own PII) consent.
- Pre-v1: no backward-compat shims unless explicitly approved.
- File-scoped namespaces for new C# files.

## Validation Baseline

Before any phase is considered complete:
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project Event.Application.UnitTests`
- `dotnet test --project Event.Architecture.Tests`
- Relevant integration test project (per phase)
- `/docs-lint` if docs changed

## Current Known Risks / Unknowns

- **Field-classification correctness** — matrix must be peer-reviewed against every PII entity (Task 1.1). Owner: Task 1.1 + 7.2.
- **Gateway completeness** — every AI context fetch must route through it; architecture test is load-bearing (Task 1.5/3.2).
- **Provider trust tier misconfiguration** — safe default mitigates (Task 3.1).
- **Instance-admin semantics** — no concept in current actor service; user confirmation needed (blocker above).
- **Federation cross-tenant AI disclosure** — out of scope; flagged for future.

## Handoff Notes

### Handoff — 2026-06-28 Europe/Brussels
- **Current state:** Planning complete. Three dev docs created. Awaiting user review.
- **Next action:** User reviews plan; then Task 1.1 (read PII entities + security docs → field-classification matrix).
- **Blockers:** Instance-admin AI semantics confirmation (see Blockers).
- **Modified files:** `dev/active/ai-context-disclosure-policy/{plan,context,tasks}.md` (new).
- **Validation:** No code changes yet; no build/test needed.
- **Documentation impact:** Plan proposes `docs/AI_CONTEXT_SECURITY.md` + ADR-006 (to be written in Task 1.4).
- **Risks:** See §17 of the plan and risks list above.
- **Notes for next contributor/agent:** Start by reading the plan §2 (current state) and §6 (phases). The compressed blocks b1–b12 contain the detailed prior research of the AI flow and domain entities — they are the evidence base for the current-state report.
