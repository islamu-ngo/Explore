<!-- ABOUTME: Clean-room research record for the Phase 22+ registration-data-collection planning rebaseline. -->
<!-- ABOUTME: Preserves source provenance while handing implementers only repository-native functional constraints. -->

# Phase 22+ Clean-Room Evidence

Date: 2026-08-27 Europe/Brussels

Scope: planning only; no runtime source, dependency, package lock, configuration, migration, generated contract, test, or external asset changed.

## Governing Internal Inputs

- `islamic-value-sensitive-design/i-vsd-registration-data-collection.md` (2026-08-26, SHA-256 `d7723403e6d8b1a70854599a3c4812091290cf505bea7ea0a4558a5e6532d237`) is primary.
- `islamic-value-sensitive-design/i-vsd-paid-event-payments-consultation.md` (2026-08-13, SHA-256 `44e90e5ccb88ba7e98503f0f1b98c00b7bdfaf85d623aff8f7ff882a2a90cb36`) is secondary and yields on conflict.
- ADR-016 through ADR-018 and ADR-022 through ADR-024 define repository-native context, authority, aggregate, commerce, admission, transfer, recovery, and product boundaries.
- The `registration-data-collection` intent and canonical/path-scoped rules define allowed paths, verification, criticality, and forbidden actions.

## Public Source Register

Only official/public behavioral documentation was consulted. No external repository, source code, SDK implementation, snippet, AST, SQL, schema, migration, test, comment, asset, or copied expressive organization was ingested.

| Source | Accessed | Permitted source-free observation |
|---|---:|---|
| [Microsoft EF Core concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency) via Context7 `/dotnet/entityframework.docs` | 2026-08-27 | Optimistic concurrency tokens create an explicit conflict that application code must resolve; use for one-winner state transitions, not blind non-idempotent retries. |
| [ASP.NET Core Data Protection limited-lifetime payloads](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/limited-lifetime-payloads) via Context7 `/dotnet/aspnetcore.docs` | 2026-08-27 | Purpose isolation and bounded expiry are native capability primitives; the key ring is restore-critical operational state. |
| [ASP.NET Core rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit) via Context7 `/dotnet/aspnetcore.docs` | 2026-08-27 | Endpoint/partition policies, bounded queues, `429`, and `Retry-After` support abuse controls without replacing authorization/idempotency. |
| [OWASP Transaction Authorization](https://cheatsheetseries.owasp.org/cheatsheets/Transaction_Authorization_Cheat_Sheet.html) | 2026-08-27 | Sensitive operation authority is server-side, operation-specific, short-lived, and enforced at every state transition. |
| [OWASP Forgot Password](https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html) | 2026-08-27 | Public capability flows need cryptographic randomness, secure storage, single use, expiry, fixed trusted origins, HTTPS, generic responses, and rate limiting. |
| [GDPR](https://eur-lex.europa.eu/legal-content/EN/TXT/?uri=CELEX%3A32016R0679) and [EDPB design/default summary](https://www.edpb.europa.eu/system/files/2026-02/edpb-summary-gdpr-data-protection-design-default_en.pdf) | 2026-08-27 | Plan for explicit purpose, minimization, storage limitation, privacy by default, and risk-appropriate protection; do not claim legal compliance. |
| [WCAG 2.2](https://www.w3.org/TR/WCAG22/) and [status messages](https://www.w3.org/WAI/WCAG22/Understanding/status-messages) | 2026-08-27 | Use labelled/native controls, stable focus/order, keyboard access, and programmatically determinable status/error changes; do not claim certification. |
| [Stripe charge types](https://docs.stripe.com/connect/charges), [connected-account payouts](https://docs.stripe.com/connect/payouts-connected-accounts), and [platform controls](https://docs.stripe.com/connect/platform-controls-for-stripe-dashboard-accounts) | 2026-08-27 | Direct charges remain connected-account transactions; payout/manual controls depend on the exact account configuration and cannot be assumed. |
| [Stripe connected-account reserves](https://docs.stripe.com/connect/connected-account-reserves) | 2026-08-27 | The documented reserve capability is Private preview and time-bounded; it is not an acceptable stable typed public API for this repository. |

## Sanitized Functional Handoff

1. Enforce access modes, accepted terms, purchaser actor/context, and strict event/tenant/instance purchase ceilings before introducing transfer claims.
2. Pin order-vs-participant form scope, purpose/visibility/retention lineage, participant-owned consent, and approval; active admission authority cannot precede required completion.
3. Treat transfer as future-holder authority rotation, not a commercial rewrite: preserve purchaser/payment/refund truth, recollect recipient facts, and revoke the old credential atomically.
4. Keep waitlist order deterministic and explainable. A released exact-type ticket has priority before new stock; an original-holder refund is requested only after replacement payment is reconciled.
5. Keep add-ons optional, event-bound, independently inventoried/fulfilled, and excluded from admission authority and specialist business systems.
6. Restore application state and capability/key/scheduler/outbox/provider-cursor state together, proving no duplication or authority resurrection.
7. Keep `ProtectedDelayedPayout` unavailable. Do not create runtime code, configuration, migration, API/HAL/client surface, scheduler job, preview/raw provider call, or escrow language.

## Independent Design / SSO Record

- **AFC filtering:** only abstract behaviors, risks, provider contract facts, and standards obligations were retained; external expression was excluded.
- **Repository-native anchors:** existing CQRS/MediatR handlers, Domain aggregates/services, EF configurations/repositories, tenant filters, idempotency middleware, transactional outbox, Quartz helpers, Data Protection/keyed-digest capabilities, HAL policies, BFF forwarding, versioned forms/typed answers, generated NSwag client, and admission/check-in fences.
- **Independent structure:** the Phase 22–27 sequence follows current ISLAMU aggregate dependencies and failure boundaries, not an external product's module/schema/UI order.
- **Dependency decision:** none. Existing framework/repository primitives cover the plan; no new package is justified.
- **Outbound-license decision:** no third-party implementation material or dependency entered the workstream; intended ISLAMU outbound licensing paths are unchanged.
- **Implementation boundary:** a future implementer must use this sanitized handoff plus repository source/ADRs and official public API documentation only. If restricted-source contamination is suspected, discard that output and restart cleanly.

## Evidence Links

- Authoritative plan: `registration-data-collection-plan.md` §19
- Executable tasks: `registration-data-collection-tasks.md` Phases 22–27
- Review: `registration-data-collection-cto-review.md`
- Live state: `registration-data-collection-context.md`
