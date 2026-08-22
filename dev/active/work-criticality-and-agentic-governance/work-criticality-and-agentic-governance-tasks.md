<!-- ABOUTME: Hot task execution ledger for Work Criticality and Agentic Governance. -->
<!-- ABOUTME: Tracks task checkboxes, phase verifications, dependencies, effort, and completion state. -->

# Work Criticality and Agentic Governance — Task Ledger

**Last Updated:** 2026-08-22 Europe/Brussels  
**Completed Tasks:** 18 / 18  
**Current Phase:** Complete (Phases 1–5 Delivered)  
**I-VSD Reference:** [islamic-value-sensitive-design/i-vsd-work-criticality-and-agentic-governance.md](../../../islamic-value-sensitive-design/i-vsd-work-criticality-and-agentic-governance.md)

---

## Phase 1: Contribution Contract & Schema Hardening

- [x] Task 1.1: Extend `.agents/contract/schema.json` with the hardened `criticality` object.
- [x] Task 1.2: Backfill `.agents/contract/intents.yaml` for all 19+ intents with complete criticality metadata, elevating migrations and privacy erasure.
- [x] Task 1.3: Add `AgentContextCriticalityTests.cs` to `tests/Event.Architecture.Tests/` to enforce criticality validation in CI.
- [x] **Phase 1 Verification Gate:** Validated all 19 intents and JSON schema against criticality invariants.

---

## Phase 2: Path-Scoped Criticality Rules & Guardrail Skills

- [x] Task 2.1: Author `.agents/rules/payments-commerce.md` with rules for zero-cardholder-data, integer arithmetic, and outbox coupling.
- [x] Task 2.2: Author `.agents/rules/privacy-and-pii.md` with rules for framework log redaction, authority-first commit, and anti-resurrection fences.
- [x] Task 2.3: Author `.agents/rules/auth-trust-boundaries.md` with rules for fail-closed auth and single user ID extraction authority.
- [x] Task 2.4: Author `.agents/skills/criticality-guardrail/SKILL.md` and `.agents/skills/epistemic-mad-review/SKILL.md`.
- [x] **Phase 2 Verification Gate:** Path-scoped rules and skills satisfy repository schemas and metadata constraints.

---

## Phase 3: Telemetry PII Redaction & .NET Compliance

- [x] Task 3.1: Add `Microsoft.Extensions.Compliance.Abstractions`, `Microsoft.Extensions.Compliance.Redaction`, and `Microsoft.Extensions.Telemetry` to `Directory.Packages.props`.
- [x] Task 3.2: Implement `DataTaxonomy`, `[PiiDataAttribute]`, and `[SensitiveDataAttribute]` in `src/Explore.ServiceDefaults/Compliance/`.
- [x] Task 3.3: Implement zero-allocation `Span<char>` `StarRedactor` and cryptographic `HmacRedactor`.
- [x] Task 3.4: Wire up redaction into `AddServiceDefaults` in `src/Explore.ServiceDefaults/Extensions.cs`.
- [x] Task 3.5: Add unit tests in `tests/Explore.Diagnostic.UnitTests/Compliance/` verifying zero-leak log redaction and HMAC determinism.
- [x] **Phase 3 Verification Gate:** Redaction pipeline and unit tests implemented.

---

## Phase 4: Mutation Testing & Epistemic MAD Protocol Integration

- [x] Task 4.1: Create `stryker-config.json` with dynamic thresholds (>85% Domain/App, >60% Infra) and AST mutation exclusions.
- [x] Task 4.2: Update reviewer subagent profiles (`security-privacy-agent.md`, `quality-verifier-agent.md`, `change-reviewer-agent.md`) with Response Anonymization and Invariant-Breaker test generation.
- [x] **Phase 4 Verification Gate:** Stryker configuration and agent prompt contracts verified.

---

## Phase 5: EU AI Act Governance, AI BOM & Final Commit Composition

- [x] Task 5.1: Author `docs/legal/EU_AI_ACT_CONFORMITY.md` and `ai-bom.v1.json` with AGPL-3.0-only license and provider-agnostic runtime adapters.
- [x] Task 5.2: Implement validation script in `.ci/scripts/validate-ai-bom-conformity.cs`.
- [x] Task 5.3: Add adversarial AI benchmark suite in `tests/Explore.Diagnostic.UnitTests/AiEvaluation/CriticalityBenchmarkEvaluationTests.cs`.
- [x] **Phase 5 Verification Gate:** EU AI Act technical docs and CycloneDX 1.6 AI-BOM validation verified.

---

## Phase 6: Dynamic Tier Execution Matrix (DTEM) & Proportional Rigor

- [x] Task 6.1: Extend `.agents/contract/schema.json` with `intake_clarification_mode`, `exploration_protocol`, `testing_strategy`, and `review_protocol`.
- [x] Task 6.2: Backfill all 19 intents in `.agents/contract/intents.yaml` with dynamic instruction properties.
- [x] Task 6.3: Update `AGENTS.md` Cold-Start Flow with Step 2 Dynamic Alignment & Intake.
- [x] Task 6.4: Update `.agents/CONTEXT_ENGINEERING.md` with Dynamic Exploration Budget & Criticality Matrix table.
- [x] Task 6.5: Upgrade `.agents/skills/criticality-guardrail/SKILL.md` to be the master DTEM operational router.
- [x] Task 6.6: Author `.agents/rules/work-criticality-matrix.md` path-scoped rule file.
- [x] Task 6.7: Update `.agents/skills/grill-me/SKILL.md` with high-criticality intake decision trees.
- [x] Task 6.8: Update `tests/Event.Architecture.Tests/AgentContextCriticalityTests.cs` enforcing dynamic execution properties and high-criticality rigor in CI.
- [x] **Phase 6 Verification Gate:** DTEM architecture assertions and policy tests verified.
