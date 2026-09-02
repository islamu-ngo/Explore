---
description: >-
  Understand layer ownership, CQRS request flow, repository boundaries, and HAL
  response assembly.
---

# Clean Architecture

ISLAMU Event keeps business rules independent from delivery and infrastructure concerns. Dependencies point inward:

```
Domain -> Application -> Persistence / Infrastructure -> API composition
```

The arrows describe allowed dependency direction, not runtime call order.

## Domain

The Domain layer owns aggregates, value objects, lifecycle rules, and domain events. It does not depend on EF Core, HTTP, UI frameworks, or provider SDKs.

## Application

Application owns CQRS commands and queries, MediatR handlers, specifications, authorization requirements, and immutable request/result contracts. Validators are instantiated explicitly rather than discovered through dependency injection.

## Persistence and Infrastructure

Persistence implements entity storage, specifications, query filters, transactions, and generated migrations. Repositories return entities, never API DTOs. Infrastructure implements external providers such as SMTP, storage, payment, authorization, and protocol adapters behind application-owned contracts.

## API and BFF

Controllers stay thin: validate the transport boundary, dispatch through MediatR, map results, and assemble HAL links. Route names and server-issued `_links` are the action authority for clients. The browser communicates through the BFF so session cookies and downstream tokens remain server-side concerns.

## Cross-cutting invariants

* Aggregates use UUIDv7 `Guid` identifiers; lookups use `int`; cursors use `long`.
* GET endpoints are anonymous unless the resource contract requires private access; writes require authorization.
* Tenant identity comes from trusted request and persistence context, never from a request-body user or tenant field.
* External side effects that must survive failure use durable outbox or equivalent replayable state.
* Breaking pre-1.0 changes remove obsolete shapes instead of adding compatibility shims.

Put a change in the lowest layer that can own its invariant without depending on an outer layer.
