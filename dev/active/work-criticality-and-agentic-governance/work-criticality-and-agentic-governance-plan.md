<!-- ABOUTME: Executable implementation plan for Enterprise-Grade Work Criticality, Epistemic Multi-Agent Validation, and Telemetry Governance. -->
<!-- ABOUTME: Replaces uniform agent trust with a 5-tier risk taxonomy, Expand/Contract migrations, MAD Response Anonymization, and .NET PII redaction. -->

# Work Criticality and Agentic Governance — Implementation Plan

Last Updated: 2026-08-22 Europe/Brussels

## 0. Planning Metadata

- **Original Request:** Architect and implement an enterprise-grade, self-hostable Work Criticality Classification system for AI agentic engineering, integrating hybrid agentless/SWE-agent workflows, epistemic validation (MAD response anonymization and weighted voting), framework-level telemetry PII sanitization (.NET `Microsoft.Extensions.Compliance.Redaction`), AST mutation testing (Stryker.NET), and regulatory compliance (EU AI Act August 2026 mandates).
- **Task Directory:** `dev/active/work-criticality-and-agentic-governance/`
- **Planning Status:** Approved (In Implementation)
- **Primary Intents:** `create-agent-context-skill`, `ci-cd-change`, `external-infrastructure-bootstrap`, `platform-privacy-erasure` from `.agents/contract/intents.yaml`.
- **Cross-Cutting Guardrails:** `clean-architecture-rules`, `auth-patterns`, `dotnet-efcore-guidelines`, `error-tracking`, `ip-clean-room`.
- **I-VSD Document:** [islamic-value-sensitive-design/i-vsd-work-criticality-and-agentic-governance.md](../../../islamic-value-sensitive-design/i-vsd-work-criticality-and-agentic-governance.md)
- **Primary Layers Touched:** `.agents/` (contracts, rules, skills, agents, benchmarks), `Explore.ServiceDefaults` & `Explore.Infrastructure` (telemetry, compliance redactors), `Explore.Persistence` (migration safety, multi-database engine verification), `Event.Architecture.Tests` (CI assertions, contract tests), and `docs/` (governance, EU AI Act compliance runbooks).
- **Complexity:** XL (Spans schema evolution, CI architecture assertions, framework-level telemetry redaction, AST mutation testing, multi-agent debate protocols, and legal AI Act compliance).

---

## 1. Executive Summary

Autonomous Large Language Model (LLM) agents operating within enterprise, self-hosted software ecosystems present an asymmetric risk profile: a subtle failure in a UI component causes minor aesthetic drift, whereas a flaw in a payment webhook, multi-tenant global query filter, or privacy erasure routine causes severe financial loss, tenant data leakage, or catastrophic legal liability.

This implementation plan transitions ISLAMU Event from **uniform agent trust** to a **deterministic, 5-Tier Work Criticality & Epistemic Governance Architecture**:

1. **5-Tier Work Criticality Taxonomy (Tiers 0–4)**: Explicitly classifies work from Tier 0 (Sovereign/Financial) to Tier 4 (Standard UI/Docs), dynamically elevating model capability (`advanced`), verification depth (`exhaustive`), and mandatory safety gates.
2. **Self-Hosted Enterprise Invariants**: Enforces **Expand/Contract database migrations** for zero-downtime rolling updates, **multi-database engine parity** (PostgreSQL, SQLite, SQL Server, MySQL, MariaDB), **offline air-gapped topology** (circuit breakers with zero external phone-home dependencies), and **non-destructive cryptographic key rotation** ($K_n \rightarrow K_{n+1}$).
3. **Epistemic Validation via Anonymized Multi-Agent Debate (MAD)**: Eliminates sycophancy (85.5% modal error adoption) and self-bias by stripping identity markers from debate transcripts (Response Anonymization), structuring review into asymmetric expert personas (Security, DB Architecture, Performance, Compliance), deploying the **Invariant-Breaker Pattern** (writing failing exploit/bypass tests), and aggregating via weighted voting over Directed Acyclic Graphs (DAGs).
4. **Framework-Level Telemetry PII Redaction**: Integrates `Microsoft.Extensions.Compliance.Redaction` in .NET 10 to automatically redact `[PiiData]` via zero-allocation `Span<char>` `StarRedactor` and traceable `HmacRedactor`, intercepting all `ILogger` streams to guarantee zero PII enters developer/agent context windows.
5. **Mutation Testing & Non-Deterministic QA**: Deploys Stryker.NET in CI with dynamic mutation score gates (>85% for Domain/Application, >60% for Infrastructure) and the Durable Task Roslyn Analyzer to prove test assertions catch real semantic regressions.
6. **EU AI Act Reality (August 2026 Compliance)**: Institutionalizes Article 50 transparency (automated Git commit signatures `Authorship: AI-Agentic`, UI watermarks), Annex IV dynamic AI Bill of Materials (AI BOM), Article 10 10-year data governance logging, machine-readable `robots.txt` respect, and human-in-the-loop hard kill-switches.

---

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| `.agents/contract/schema.json` enforces intent schema | `Event.Architecture.Tests.AgentContextPolicyTests` | High | Currently lacks `criticality` object. |
| `intents.yaml` covers 19+ change categories | `.agents/contract/intents.yaml` | High | Uniform contract fields; no risk distinction. |
| Subagents declare model tiers | `.agents/agents/*-agent.md` | High | Enforced by `AgentProfiles_ShouldDeclareAllowedModelTier`. |
| PII redaction mentioned in docs | `docs/SECRETS.md`, `docs/PRIVACY_ERASURE.md` | High | Lacks framework-level .NET `ILogger` interceptors. |
| Multi-provider DB support | `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs` | High | PostgreSQL, SQLite, SQL Server, MariaDB, MySQL supported. |

---

## 3. Non-Negotiable Constraints

1. **Clean Architecture Purity**: `Explore.Domain` has zero dependencies; `Explore.Application` depends only on Domain; `Explore.Persistence` and `Explore.Infrastructure` implement interfaces; `Explore.Blazor` is fully isolated.
2. **Deterministic Context Budgets**: Follows `.agents/CONTEXT_ENGINEERING.md` (12 KB bootstrap cap, 0 duplicate bytes).
3. **No Hand-Edited Migrations**: EF Core migrations are strictly generated.
4. **EU AI Act Article 50**: All non-human AI commits and UI-facing AI features must carry irremovable disclosure.
5. **Zero PII Leakage**: `[PiiData]` must never be written in plaintext to structured logs or OpenTelemetry tags.

---

## 4. Implementation Phases

### Phase 1: Contribution Contract & Schema Hardening
- **Goal:** Update `.agents/contract/schema.json` and backfill `intents.yaml` with the complete 5-tier criticality taxonomy and enterprise failure mode declarations.
- **Tasks:**
  - `Task 1.1`: Extend `schema.json` with the hardened `criticality` object.
  - `Task 1.2`: Backfill `intents.yaml` for all 19+ intents, elevating migrations and privacy erasure.
  - `Task 1.3`: Add `AgentContextCriticalityTests.cs` to `Event.Architecture.Tests`.
- **Phase-End Verification:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 2: Path-Scoped Criticality Rules & Guardrail Skills
- **Goal:** Author dedicated path-scoped rules and guardrail skills to enforce invariants during code authoring.
- **Tasks:**
  - `Task 2.1`: Author `.agents/rules/payments-commerce.md`.
  - `Task 2.2`: Author `.agents/rules/privacy-and-pii.md`.
  - `Task 2.3`: Author `.agents/rules/auth-trust-boundaries.md`.
  - `Task 2.4`: Author `.agents/skills/criticality-guardrail/SKILL.md` and `.agents/skills/epistemic-mad-review/SKILL.md`.
- **Phase-End Verification:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 3: Telemetry PII Redaction & .NET Compliance
- **Goal:** Integrate `Microsoft.Extensions.Compliance.Redaction` across `Explore.ServiceDefaults` and `Explore.Infrastructure`.
- **Tasks:**
  - `Task 3.1`: Add compliance NuGet packages to `Directory.Packages.props`.
  - `Task 3.2`: Implement `DataTaxonomy`, `[PiiData]`, and `[SensitiveData]` in `Explore.ServiceDefaults`.
  - `Task 3.3`: Implement zero-allocation `StarRedactor` and cryptographic `HmacRedactor`.
  - `Task 3.4`: Wire up redaction into `AddServiceDefaults` and `ILogger` pipeline; add unit tests for zero-leak log redaction.
- **Phase-End Verification:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 4: Mutation Testing & Epistemic MAD Protocol Integration
- **Goal:** Integrate Stryker.NET AST mutation testing configuration and the Invariant-Breaker adversarial review protocol.
- **Tasks:**
  - `Task 4.1`: Configure `stryker-config.json` with dynamic thresholds (>85% Domain/App, >60% Infra).
  - `Task 4.2`: Update reviewer subagent profiles (`security-privacy-agent.md`, `quality-verifier-agent.md`) with Response Anonymization and Invariant-Breaker test generation.
  - `Task 4.3`: Integrate Stryker.NET and Roslyn analyzers into `_build-test.yml` workflow.
- **Phase-End Verification:** `dotnet build --configuration Release --verbosity quiet`

### Phase 5: EU AI Act Governance, AI BOM & Final Commit Composition
- **Goal:** Implement legal compliance assets, AI BOM, commit disclosures, and release fragment.
- **Tasks:**
  - `Task 5.1`: Author `docs/compliance/EU_AI_ACT_CONFORMITY.md` and `docs/compliance/ai-bom.v1.json`.
  - `Task 5.2`: Implement automated Git commit disclosure hook in `.agents/hooks/commit-ai-disclosure.sh`.
  - `Task 5.3`: Expand `.agents/benchmarks/cold-start-tasks.yaml` with adversarial Tier 0/1/2 evaluation scenarios.
  - `Task 5.4`: Create release change fragment `docs/releases/changes/CHG-2026-0012.yaml` and compose final Conventional Commit.
- **Phase-End Verification:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
