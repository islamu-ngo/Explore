---
name: criticality-guardrail
description: Load when working on Tier 0 Sovereign (payments, money, orders), Tier 1 Security (auth, tenancy, migrations), or Tier 2 Privacy (PII, erasure) tasks to enforce advanced model tier, Invariant-Breaker adversarial tests, and zero-PII telemetry validation.
type: guardrail
enforcement: block
priority: critical
---
<!-- ABOUTME: Master dynamic instruction router enforcing proportional rigor and safety gates based on task criticality tier. -->
<!-- ABOUTME: Enforces proactive /grill-me intake, deep graph exploration, Invariant-Breaker failing tests, and Epistemic MAD review. -->

# Criticality Guardrail & Dynamic Tier Execution

## Rules

1. **Model Capability Escalation**: Any task touching Tier 0 (`sovereign`), Tier 1 (`security`), or Tier 2 (`privacy`) MUST execute using an `advanced` capability model tier.
2. **Proactive Intake Clarification Gate**: When a task resolves to Tier 0, 1, or 2, the agent is strictly forbidden from writing code immediately. If requirements, edge cases, or threat boundaries are underspecified, the agent MUST run a proactive `/grill-me` alignment interview first.
3. **Invariant-Breakers First**: Author failing adversarial/exploit tests FIRST (concurrency races, double-spending, hold expiration races, cross-tenant header spoofing, PII log leakage).
4. **Exhaustive Knowledge Graph Exploration**: High-criticality tasks must trace full caller/callee trees, outbox linkages, database lock contention, and ADR/I-VSD invariants.
5. **Zero Log-PII Invariant**: Never pass entities implementing `IContainsPii` or sensitive properties to `ILogger` without `[PiiData]` redaction attributes.
6. **Expand/Contract Persistence Invariant**: All migrations touching tenancy or money/user tables MUST use Expand/Contract to guarantee zero-downtime rolling updates.
7. **Transactional Outbox Mandate**: Any domain event, webhook, or external notification resulting from a state transition MUST be written to the local Outbox table in the same transaction.

## Dynamic Tier Execution Matrix (DTEM)

| Lifecycle Stage | Tier 0: Sovereign (Payments) | Tier 1: Security (Auth/Tenancy) | Tier 2: Privacy (PII/Erasure) | Tier 3: Domain State (CQRS) | Tier 4: Standard (UI/Docs) |
|---|---|---|---|---|---|
| **1. Intake & Grill-Me** | Mandatory `/grill-me` on money flows, hold expiration | Mandatory `/grill-me` on threat models, fail-closed auth | Mandatory `/grill-me` on erasure authority, receipt tokens | Standard Q&A (only if ambiguous) | Autonomous defaults |
| **2. Exploration Depth** | Exhaustive graph (callers, callees, outbox, DB locks) | Exhaustive graph + policy (Cerbos, BFF, global filters) | Exhaustive data flow (`*Pii` fields, log sinks) | Bounded caller/callee tracing | Local surface reading only |
| **3. Invariant Alignment** | Strict ADR-022/024, integer checked arithmetic | Strict `SECURITY-MODEL.md`, `AUTHORIZATION.md` | Strict `PRIVACY_ERASURE.md`, zero-PII logging | Clean Architecture (Domain $\rightarrow$ App $\rightarrow$ Infra) | UI system: HAL links, BEM CSS |
| **4. Invariant-Breaker Tests** | Concurrency races, double capture, currency overflow | Cross-tenant data leaks, forged headers, expired JWT | Unmasked PII log injection, user resurrection races | Behavioral CQRS unit & integration tests | Affordance & render tests (`_links`) |
| **5. Testing Rigor** | Real PostgreSQL tests, row locking, Stryker >85% | Real multi-provider DB tests, Cerbos parity, Stryker >85% | Full erasure lifecycle test, test log PII scan pass | Targeted project unit & integration tests | Fast verification + Release build |
| **6. Multi-Agent Review** | Epistemic MAD (anonymized debate & weighted vote) | Epistemic MAD (anonymized debate & weighted vote) | Epistemic MAD (anonymized debate & weighted vote) | Peer Review (`backend-engineer-agent`) | Lightweight Self-Check |
| **7. Teaching Summary** | Architecture, state transitions, recovery runbook | Threat model resolution, tenant isolation proof | Data retention impact, erasure auditability proof | Summary of CQRS handlers & mappings | Concise UI walkthrough |

## High-Leverage Testing Done Right

High-criticality tasks explicitly reject shallow unit tests and mock assertions. Consult [resources/adversarial-archetypes.md](resources/adversarial-archetypes.md) for concrete recipes:
1. **Concurrency Race Tests**: Simulate simultaneous requests (e.g. hold expiration racing against payment capture).
2. **Real Database Integration Tests**: Run against real PostgreSQL / multi-provider test containers to verify transaction boundaries, row-level locking, and idempotency.
3. **Exploit Invariant Tests**: Verify that tampering with tenant headers, negative amounts, or expired tokens fails closed with RFC 7807 ProblemDetails.
4. **Log Sink PII Scans**: Assert that execution traces and logs contain zero plaintext emails, cards, or tokens.

## Workflow

1. **Classify & Check Criticality**: Read intent from `.agents/contract/intents.yaml` and resolve DTEM tier.
2. **Direction Alignment**: If Tier 0, 1, or 2, trigger proactive `/grill-me` interview with the user.
3. **Deep Graph Exploration**: Use `code-review-graph` MCP tools to map callers, callees, dependent flows, and DB lock contention.
4. **Draft Failing Invariant-Breakers**: Write adversarial concurrency and exploit tests first.
5. **Implement Fail-Closed Solution**: Author clean, transactional, PII-safe code.
6. **Verify Proportional Rigor**: Run real DB integration tests, Stryker mutation checks (>85%), and log sink PII scans.
7. **Epistemic MAD Review**: Execute anonymized multi-agent debate and post-hoc voting.
8. **Comprehensive Technical Teaching**: Provide a deep architectural teaching summary before completing the task.

## Verification

- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
