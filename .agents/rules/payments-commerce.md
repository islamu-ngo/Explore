---
name: payments-commerce
description: Apply when editing payment, checkout, pricing, stripe connect, orders, or commerce-related files.
paths:
  - "src/**/*Payment*.cs"
  - "src/**/*Stripe*.cs"
  - "src/**/*Checkout*.cs"
  - "src/**/*Refund*.cs"
  - "src/**/*Dispute*.cs"
  - "src/**/*Payout*.cs"
  - "src/**/*PlatformMonetization*.cs"
  - "src/**/*Promotion*.cs"
related_skills: [clean-architecture-rules, dotnet-efcore-guidelines, outbox-pattern, auth-patterns]
related_docs: [docs/PAYMENTS.md, docs/QUICK_REFERENCE.md, docs/SECURITY-MODEL.md, docs/CONFIGURATION.md]
minimum_tests: [Event.Domain.UnitTests, Event.Application.UnitTests, Explore.Infrastructure.Tests, Event.API.IntegrationTests, Event.Architecture.Tests]
related_intents: [registration-data-collection, webhook-delivery-redesign]
---

<!-- ABOUTME: Path-scoped rules for Tier 0 Sovereign Financial workflows, Stripe Connect, and payment contracts. -->
<!-- ABOUTME: Enforces zero cardholder data, integer minor-unit arithmetic, idempotency, outbox pairing, and secret isolation. -->

# Payments and Commerce Rules (Tier 0 — Sovereign)

## Applies To
- `src/**/*Payment*.cs`, `src/**/*Stripe*.cs`, `src/**/*Checkout*.cs`, `src/**/*Refund*.cs`, `src/**/*Dispute*.cs`, `src/**/*Payout*.cs`

## Critical Rules & Invariants

| # | Rule | Correct | Wrong |
|---|---|---|---|
| 1 | **Zero Cardholder Data (PCI-DSS)** | Delegate payment capture exclusively to provider-hosted pages (e.g. Stripe Checkout). | Handling, transmitting, or persisting raw card numbers, CVVs, or bank account credentials. |
| 2 | **Integer Minor-Unit Money** | Store and compute all amounts in checked integer minor units (cents as `long` or `int`). | Using `float`, `double`, or `decimal` in core arithmetic or currency logic. |
| 3 | **Monotonic Payment States** | State transitions move strictly forward (`Pending` $\rightarrow$ `Succeeded` / `Failed`); reconcile via provider truth. | Overwriting succeeded payments or allowing client-driven state resets. |
| 4 | **Transactional Outbox Pairing** | Pair every order mutation emitting domain events or webhook side-effects with an Outbox insert in the same DB transaction. | Direct HTTP calls, email sending, or webhook dispatch inside a DB transaction. |
| 5 | **Idempotent Webhooks** | Verify exact signed UTF-8 body bytes against server-only webhook secret (`whsec_...`); deduplicate via monotonic event IDs. | Processing webhooks without signature verification or mutating state directly in controller. |
| 6 | **Secret Isolation** | Resolve platform API keys (`sk_live_...`) server-side via `SecretDefinitionRegistry`. | Logging keys, returning secrets in browser DTOs, or embedding in OpenAPI schemas. |
| 7 | **Multi-Tenant Account Fencing** | Create ticket charges as direct charges in the organizer's connected merchant account context (`StripeAccount: acct_...`). | Mixing organizer payouts or routing payments to instance-level administrator accounts. |

## Must Read
- [docs/PAYMENTS.md](../../docs/PAYMENTS.md)
- [docs/adr/ADR-022-paid-event-commerce-and-stripe-connect.md](../../docs/adr/ADR-022-paid-event-commerce-and-stripe-connect.md)
- [docs/QUICK_REFERENCE.md#critical-rules](../../docs/QUICK_REFERENCE.md#critical-rules)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, `Event.Architecture.Tests`
- Fuzzing / Negative tests: Duplicate webhook replay, out-of-order event dispatch, and currency rounding tests must pass.

## Related
- Intents: `registration-data-collection`, `webhook-delivery-redesign`
- Agents: `security-privacy-agent.md`, `quality-verifier-agent.md`, `backend-engineer-agent.md`
- Skills: `clean-architecture-rules`, `outbox-pattern`, `auth-patterns`
