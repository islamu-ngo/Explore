<!-- ABOUTME: Records Phase 19 refund and dispute research provenance without retaining external source expression. -->
<!-- ABOUTME: Attests that the implementation remains independently designed from repository-native payment authority. -->

# Phase 19 Clean-Room Evidence

Date: 2026-08-25 Europe/Brussels

## Source register

- Stripe, “The Refund object,” https://docs.stripe.com/api/refunds/object, accessed 2026-08-25 through public official documentation.
- Stripe, “Create a refund,” https://docs.stripe.com/api/refunds/create, accessed 2026-08-25 through public official documentation.
- Stripe, “Idempotent requests,” https://docs.stripe.com/api/idempotent_requests, accessed 2026-08-25 through public official documentation.
- Stripe, “Create direct charges,” https://docs.stripe.com/connect/direct-charges, accessed 2026-08-25 through public official documentation.
- Stripe, “The Dispute object,” https://docs.stripe.com/api/disputes/object, accessed 2026-08-25 through public official documentation.
- Context7 `/stripe/stripe-dotnet` and `/dotnet/entityframework.docs`, accessed 2026-08-25 for public API and framework-behavior confirmation only.

## Sanitized functional facts

- A direct-charge refund and later retrieval remain scoped to the connected account that owns the original payment.
- A retryable refund mutation uses one stable, non-sensitive idempotency key; retrieval is inherently idempotent and receives no mutation key.
- Refund truth distinguishes pending, requires-action, succeeded, failed, and canceled observations. Insufficient connected-account balance can leave a refund pending.
- Refunding platform-directed application fees is explicit. A partial charge refund returns the application fee proportionally when that option is selected.
- Dispute observations include inquiry and formal stages plus open, won, lost, closed-without-formal-dispute, and prevented outcomes.
- EF Core user-controlled transactions must execute as one replayable unit when an execution strategy is enabled. Application-managed GUID concurrency tokens remain provider-neutral.

## Clean-room attestation

No third-party source, snippet, AST, decompiled artifact, SQL, migration, test, comment, documentation prose, or asset was copied into the implementation. External material supplied only interoperability names and observable behavior. No dependency was added or updated.

## Independent design and AFC/SSO review

- Repository anchors: ADR-022/024, the paid-event I-VSD consultation, `PaymentAttempt`, `PaidOrderAcceptanceSnapshot`, the durable incoming-webhook framework, tenant query filters, and existing Stripe Infrastructure boundaries.
- Independent design: `RefundAttempt` remains the provider-neutral capacity reservation; `PaymentDispute` remains a separate projection; Application owns dispatch/reconciliation orchestration; Infrastructure alone maps Stripe requests and normalized signed observations; Persistence linearizes reservations on the tenant-qualified payment authority.
- Constrained elements: Stripe wire status values, connected-account request scope, and the application-fee refund flag are interoperability facts. EF transaction and concurrency APIs are framework constraints.
- Discretionary structure, naming, state ownership, transaction sequence, normalized envelopes, tests, and error taxonomy are repository-native and were not derived from external implementation structure.
- AFC/SSO decision: pass. Reviewer: implementation agent, 2026-08-25.

## Evidence

- Focused Domain refund invariants: 4 passed.
- Focused Application dispatch/reconciliation invariants: 9 passed.
- Focused Infrastructure Stripe refund/dispute invariants: 9 passed.
- Focused SQLite refund/dispute persistence invariants: 5 passed.
- PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL migration models report no pending changes after generated migration creation.
