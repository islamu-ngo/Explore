<!-- ABOUTME: Records the technology-neutral decision for domain-owned lifecycle behavior and contextual completeness. -->
<!-- ABOUTME: Defines nullable persistence, layered validation, orchestration boundaries, and enforcement across implementations. -->

# ADR-026: Domain-Owned Lifecycle And Contextual Completeness

| | |
|---|---|
| **Status** | Accepted for implementation |
| **Date** | 2026-08-18 |
| **Deciders** | Architecture and Domain Modeling workstreams |
| **Applies to** | Any codebase with lifecycle-driven aggregates, regardless of language, framework, or persistence library |

## Context

Many business objects are valid before they are complete. A draft, imported record,
historical record, moderated object, or archived object can legitimately omit data that
is mandatory for publication or another later operation. Different deployments,
tenants, ingestion sources, and workflows may also require different fields at the
same lifecycle transition.

Making every eventually required field non-null in storage forces one of four bad
outcomes:

1. fake placeholder values are persisted;
2. drafts are stored in a parallel model and later copied or merged;
3. valid imports and historical records are rejected; or
4. deployment-specific policy is embedded in a universal database schema.

Allowing nullable fields solves those storage problems, but it creates a different
risk when lifecycle rules live only in application handlers or use-case services.
Each handler can then maintain its own transition matrix, readiness checks, and
status assignment. Over time, handlers, background jobs, integrations, and client
affordances disagree. Adding or changing a transition requires finding every copy,
and one missed path can bypass an invariant or emit side effects for an invalid
state change.

The system therefore needs both of these properties:

- contextually incomplete objects must remain representable; and
- fixed lifecycle invariants must have one domain-owned authority.

This decision is deliberately technology-neutral. Terms such as aggregate,
application handler, repository, and transaction describe responsibilities, not a
specific language or framework.

## Definitions

| Term | Meaning |
|---|---|
| **Structural invariant** | A fact that must be true for every persisted instance in every lifecycle state, deployment, and workflow. |
| **Contextual completeness** | Data required only for a specific operation, lifecycle transition, source, tenant, or policy profile. |
| **Lifecycle invariant** | A fixed business rule defining which state changes are legal and what local aggregate facts must hold. |
| **Readiness** | A diagnostic evaluation that explains why an operation can or cannot proceed, including dynamic policy requirements. |
| **Application policy** | A rule that depends on authorization, configuration, tenant policy, deployment mode, repository facts, or external state. |

## Decision

### 1. Use one durable identity across the normal lifecycle

A draft is the real business object in an earlier state, not a different object.
Normal drafting, publication, cancellation, moderation, completion, and archival use
the same aggregate identity and persistence record.

A separate revision or change-set model is justified only when an already published
version must remain visible while a proposed revision is edited and reviewed. That
is a versioning requirement, not a reason to duplicate the initial draft model.

### 2. Base storage nullability on all valid states

A persisted field is non-null only when it is required in every valid state and
workflow. Typical examples are identity, aggregate ownership, tenancy or partition
scope, current lifecycle state, parent references, and creation metadata.

A field remains nullable when absence is valid in at least one supported state or
ingestion path. Typical examples are publication copy, media, classification,
schedule, location, pricing, review data, or source-specific metadata.

This yields four persistence rules:

1. Never store fake dates, empty identifiers, placeholder text, or sentinel values
   merely to satisfy a schema.
2. Keep unconditional foreign keys, uniqueness, ranges, and relational consistency
   in the database.
3. Add a conditional database constraint only when the condition is universal and
   cannot vary by tenant, deployment, source, or policy version.
4. Do not encode configurable readiness policy as storage nullability.

Nullability represents a valid absence of data. It does not mean that every
operation accepts the absence.

### 3. Keep validation at every boundary that owns a distinct concern

Moving lifecycle behavior into the domain does not remove application validation.
The layers validate different questions:

| Layer | Owns | Examples |
|---|---|---|
| **Request or command boundary** | Input shape and trust-boundary validation | Types, lengths, syntax, mutually exclusive inputs, required command fields, supplied identifier shape. |
| **Application layer** | Dynamic policy and orchestration prerequisites | Authorization, tenant or deployment policy, referenced-record existence, moderation records, location readiness, source rules, configuration, and cross-aggregate facts. |
| **Domain model** | Fixed business invariants and legal state mutation | Allowed transitions, terminal states, local schedule consistency, aggregate-owned relationships, and no mutation after an invalid transition. |
| **Persistence layer** | Universal data integrity | Non-null structural fields, foreign keys, uniqueness, unconditional checks, and concurrency tokens. |

The same business rule must not be independently reimplemented in several layers.
Defense in depth is achieved by calling the same domain authority from readiness,
mutation, and capability evaluation, while each outer layer retains its own
validation responsibilities.

### 4. Make the aggregate the only normal lifecycle mutation gate

Lifecycle state is not publicly assignable. The aggregate exposes semantic methods
such as `publish`, `cancel`, `archive`, `moderate`, `restore`, `schedule`, or
`complete`. It does not expose a generic `setStatus` method.

Each lifecycle method must:

1. validate the current state through domain-owned rules;
2. validate fixed local facts required by that transition;
3. make no changes when validation fails;
4. update the state and domain-owned timestamps atomically; and
5. report whether a transition changed state when callers need idempotent behavior.

The domain method must not load repositories, read configuration, authorize users,
send messages, call external systems, or invalidate caches.

### 5. Keep one reusable domain decision surface

When code outside the aggregate must ask whether an action is legal before invoking
it, use a pure domain-owned rule or specification alongside the aggregate. The
aggregate method invokes that same rule before mutation.

This rule surface may expose operations such as:

- `canTransition(current, target)`;
- `canPublish(current, localFacts)`;
- `canEditDraft(current)`; or
- `requireTransition(current, target)`.

It must remain small, deterministic, and free of I/O. A switch expression, explicit
transition table, or equivalent closed representation is preferred over a generic
state-machine framework. The rule is an implementation aid inside the domain, not
a second owner of mutation.

Application handlers, readiness evaluators, background jobs, and presentation
capability builders must consume this authority instead of maintaining their own
transition matrices.

### 6. Keep dynamic completeness policy in the application layer

Fields required by a tenant, deployment, ingestion source, or operation profile are
resolved by an application policy provider or readiness service. The policy uses a
controlled vocabulary of supported requirements rather than reflection over
arbitrary entity properties or an executable rules engine.

The application composes:

1. the fixed domain decision;
2. the effective policy for the operation;
3. repository-backed and cross-aggregate facts; and
4. authorization and concurrency prerequisites.

Readiness returns structured diagnostics suitable for APIs and user interfaces.
Executing the transition still calls the aggregate method, so a missed or bypassed
readiness call cannot perform an illegal fixed transition.

### 7. Let application handlers orchestrate, not decide lifecycle legality

A command handler or use-case service follows this order:

1. validate the request or command;
2. authorize the actor and resolve scope;
3. load the aggregate and required related facts;
4. verify optimistic concurrency where applicable;
5. resolve dynamic policy and evaluate readiness;
6. invoke the semantic aggregate method;
7. persist the aggregate and transactional side-effect intents;
8. commit the transaction; and
9. perform post-commit cache invalidation or other best-effort projection work.

The handler maps domain failures to stable application or API error codes. It does
not assign lifecycle state directly and does not duplicate the domain transition
table to produce those errors.

### 8. Separate initialization and synchronization from ordinary transitions

New objects start through an explicit constructor or named factory. The normal
creation path should select a deliberate initial state, commonly `Draft`.

Trusted import, migration, seed, or historical reconstruction paths may require a
different initial state. They use explicit, narrowly named initialization or
synchronization seams and remain distinguishable from user-driven transitions.
They do not reopen a public status setter.

Persistence hydration may use a non-public constructor, backing field, or equivalent
mapping mechanism. Persistence convenience must not make lifecycle mutation public
to application code.

### 9. Define idempotency, time, concurrency, and side effects explicitly

Externally retryable transitions should normally treat an already-achieved target
state as an idempotent no-op. A duplicate command returns the established result and
does not repeat notifications, integration work, audit entries, or other side
effects. If a product needs same-state requests to fail, that behavior is explicit
and tested rather than accidental.

The application supplies a trusted timestamp to the aggregate. Domain behavior does
not read a global wall clock directly. Concurrent writes use the persistence model's
optimistic concurrency mechanism and surface a conflict rather than silently
overwriting a newer state.

Business state and durable side-effect intents are committed atomically when both
must succeed together. External calls occur after commit through an outbox or an
equivalent durable delivery mechanism. Cache invalidation never substitutes for
durable state and occurs only after a successful commit.

### 10. Keep client affordances aligned with server authority

Clients do not recreate lifecycle matrices from local status checks. The server
publishes allowed actions through hypermedia links, a capability document, or an
equivalent server-authored contract that combines authorization, application policy,
and the domain decision.

The API remains the hard enforcement boundary. Server-authored affordances improve
the user experience but never replace validation when the command executes.

## Reference Flow

The complete decision flow is:

1. Storage permits every valid incomplete state.
2. Boundary validation rejects malformed or untrusted input.
3. Application readiness explains dynamic missing requirements.
4. The aggregate rejects illegal fixed transitions.
5. Persistence protects universal integrity and concurrent updates.
6. Durable side effects are recorded only after a successful domain transition.
7. Presentation surfaces actions from the same server-owned decision path.

Language-neutral pseudocode:

```text
publish(at):
    if state is PUBLISHED:
        return UNCHANGED

    lifecycleRules.requireTransition(state, PUBLISHED)
    requireLocalPublicationInvariants()

    state = PUBLISHED
    updatedAt = requireTrustedTimestamp(at)
    return CHANGED
```

The application calls this method only after command validation, authorization,
policy readiness, repository-backed checks, and concurrency validation have passed.

## Required Enforcement

Every implementation of this decision must include:

1. exhaustive domain tests for the lifecycle transition matrix;
2. tests proving invalid transitions do not mutate the aggregate;
3. tests proving idempotent retries do not repeat side effects;
4. tests for each dynamic policy profile and readiness diagnostic;
5. handler or use-case tests proving boundary validators still execute;
6. persistence tests proving non-public lifecycle state can be hydrated and saved;
7. an architecture or static-analysis check that prevents direct lifecycle-state
   assignment outside approved domain and initialization seams; and
8. contract tests proving server-authored actions agree with domain rules and
   application policy.

## Consequences

### Benefits

- A lifecycle rule changes in one domain authority and applies to every caller.
- Handlers become smaller orchestration units without losing validation.
- Drafts, imports, archives, and tenant-specific requirements share one durable
  aggregate identity.
- Storage reflects legitimate absence instead of fake completeness.
- The API, background processing, and user interface cannot silently drift into
  competing transition models.
- The design is portable across languages, dependency-injection styles, command
  buses, persistence libraries, and web frameworks.

### Costs

- Domain entities contain nullable properties, so consumers must handle absence
  deliberately.
- Validation is intentionally layered; developers must understand ownership rather
  than trying to collapse every rule into one validator.
- Existing direct assignments, fixtures, imports, and seed paths must migrate to
  constructors, factories, or semantic methods.
- Object-relational mapping may need explicit access configuration for non-public
  state.
- Exhaustive transition and architecture tests add maintenance work, but they replace
  a larger and less visible drift risk.

## Rejected Alternatives

### Make every eventual field non-null

Rejected because it confuses publication completeness with universal validity and
forces placeholders or rejects legitimate drafts, imports, and historical data.

### Validate only in application handlers

Rejected because every handler, worker, integration, and repair path can implement a
different transition matrix or forget a guard.

### Validate only in the domain entity

Rejected because authorization, configuration, tenant policy, repository facts,
input shape, and cross-aggregate readiness do not belong inside an I/O-free
aggregate.

### Create parallel draft tables or draft entities

Rejected for ordinary drafting because it duplicates identity, relationships,
authorization, mapping, merge logic, and validation. Use a revision/change-set model
only for simultaneous published and proposed versions.

### Expose a generic status setter

Rejected because it preserves an anemic domain model and lets callers bypass
transition-specific invariants and side-effect expectations.

### Adopt a generic state-machine or rules-engine dependency

Rejected until demonstrated complexity exceeds a closed, explicit domain rule.
Adding a framework does not solve ownership and makes simple lifecycle behavior
harder to inspect.

### Encode dynamic policy in database constraints

Rejected because configuration and policy can vary by deployment, tenant, source,
operation, and time while a shared schema must represent all valid states.

## Adoption Sequence

1. Inventory every production lifecycle-state assignment, including handlers,
   workers, imports, repair tools, seeds, and moderation paths.
2. Write the desired transition matrix and classify each rule as structural,
   domain-fixed, application-dynamic, or persistence-owned.
3. Add the pure domain decision surface and exhaustive matrix tests.
4. Add semantic aggregate methods and prove invalid calls do not mutate state.
5. Refactor application paths to keep their validators and readiness checks, then
   call the aggregate method instead of assigning status.
6. Make lifecycle setters non-public and migrate approved initialization and
   synchronization seams.
7. Make readiness diagnostics and server-authored capabilities consume the same
   domain decision.
8. Add persistence, architecture, concurrency, idempotency, and side-effect tests.
9. Remove obsolete transition helpers and compatibility shims in pre-release
   systems. Established public contracts require a separate compatibility and
   migration decision.

## Related

- [Event Draft Lifecycle Architecture Consultation](../../dev/report/Event%20Draft%20Lifecycle%20Architecture%20Consultation.md)
- [Domain Model](../DOMAIN.md)
- [Technical Architecture](../ARCHITECTURE.md)
- [ADR-002: Outbox Pattern](ADR-002-outbox-pattern.md)
