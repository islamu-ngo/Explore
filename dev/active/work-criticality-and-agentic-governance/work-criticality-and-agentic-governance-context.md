<!-- ABOUTME: Active context and handoff ledger for Work Criticality and Agentic Governance. -->
<!-- ABOUTME: Records decisions, evidence locations, active workstream state, and verification baseline. -->

# Work Criticality and Agentic Governance — Active Context

**Last Updated:** 2026-08-22 Europe/Brussels  
**Status:** In Progress (Phase 1 Execution)  
**I-VSD Reference:** [islamic-value-sensitive-design/i-vsd-work-criticality-and-agentic-governance.md](../../../islamic-value-sensitive-design/i-vsd-work-criticality-and-agentic-governance.md)

---

## 1. Current State & Handoff Summary
- **Current Phase:** Phase 1 (Contribution Contract & Schema Hardening).
- **Goal:** Harden `.agents/contract/schema.json` and backfill `.agents/contract/intents.yaml` with the 5-tier criticality taxonomy and Expand/Contract rollback definitions, then create and verify `AgentContextCriticalityTests.cs`.
- **Next Action:** Execute Task 1.1 (schema.json update) and Task 1.2 (intents.yaml backfill).

---

## 2. Key Architecture Decisions & Invariants
1. **Criticality Tiers (5-Tier Model)**:
   - Tier 0: `sovereign` (Payments, Commerce, Stripe, Orders, Payouts)
   - Tier 1: `security` (Keycloak, Cerbos, AuthN/AuthZ, Tenant Isolation, Elevated Migrations)
   - Tier 2: `privacy` (PII, GDPR Erasure Authority, Anti-Resurrection Fencing)
   - Tier 3: `domain_state` (Domain Aggregates, Normal Migrations, CQRS Handlers)
   - Tier 4: `standard` (Public GET API, Blazor UI, CSS, Docs)
2. **Expand/Contract Migrations**: Required for Tier 0 and Tier 1 persistence changes to guarantee zero-downtime rolling updates.
3. **Epistemic Validation (MAD)**: Response Anonymization in Multi-Agent Debate to eliminate sycophancy (85.5% error adoption) and Invariant-Breaker pattern for adversarial test generation.
4. **Framework Telemetry Redaction**: .NET 10 `Microsoft.Extensions.Compliance.Redaction` with `StarRedactor` (`Span<char>`) and `HmacRedactor` (cryptographic non-PII correlation).
5. **EU AI Act (August 2026)**: Article 50 non-human authorship disclosures (automated commit trailers), Annex IV dynamic AI BOM, and 10-year data governance logs.

---

## 3. Evidence Locations
- Intent Schema: `.agents/contract/schema.json`
- Intent Catalog: `.agents/contract/intents.yaml`
- Architecture Tests: `tests/Event.Architecture.Tests/AgentContextPolicyTests.cs`, `tests/Event.Architecture.Tests/AgentContextCriticalityTests.cs`
- Compliance Library Root: `src/Explore.ServiceDefaults/Compliance/`
- EU AI Act Runbook: `docs/compliance/EU_AI_ACT_CONFORMITY.md`
- AI BOM: `docs/compliance/ai-bom.v1.json`
