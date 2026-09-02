---
description: >-
  Understand the BFF, API, MediatR, authority, tenancy, and durable effect
  paths.
---

# Architecture & Request Flows

ISLAMU Event follows Clean Architecture. The Domain and Application layers own business rules; Persistence and Infrastructure implement data/provider concerns; the API composes the runtime; the Blazor BFF owns the browser session.

## Browser request

1. The browser connects to the Blazor BFF.
2. The BFF owns the cookie session and obtains or refreshes access tokens.
3. The BFF forwards a bearer token and trusted tenant context to the API.
4. The API resolves tenant and caller authority, dispatches through MediatR, and uses repositories for entities.
5. The response includes HAL links for actions allowed now.

The browser does not inspect roles or claims to invent resource actions.

## Write request

1. Authentication and endpoint authorization run first.
2. MediatR resource authorization evaluates the actual target and tenant.
3. The application command validates domain invariants.
4. One authoritative state transition is persisted.
5. Reliable external effects are represented as durable outbox work.
6. The response communicates current state and allowed follow-up actions through HAL.

Caller and tenant identity come from authenticated and resolved runtime context, never request-body identity fields.

## External callback

Provider callbacks use provider-specific authentication, replay, idempotency, and correlation contracts. Intake is recorded before effects are applied. A browser return URL, callback correlation token, or advisory moderation signal is not terminal business authority unless the integration contract explicitly says so.

## Operational request

`/alive` answers whether the process runs. `/health` answers whether required dependencies are ready. `/metrics` provides bounded measurements. Keep these endpoints private or deliberately exposed and ensure their payloads contain no credentials, private paths, object keys, connection strings, or PII.

## Durable authority examples

* The notification inbox remains authoritative when SSE or Web Push only asks the client to refresh.
* Signed payment events and reconciliation establish payment/refund state, not a success page.
* Local lifecycle state governs outbound federation; inbound materialization and cursor settlement commit atomically.
* Privacy erasure establishes an anti-resurrection fence before asynchronous provider work.

Continue with [Self-Hosting](../self-hosting/) to choose a topology.
