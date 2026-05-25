<!-- ABOUTME: Progressive analysis report comparing crmworx-api implementation patterns to ISLAMU Event opportunities. -->
<!-- ABOUTME: Focuses on crmworx-api evidence and docs-only ISLAMU Event context for future implementation planning. -->

# CRMWorx API Implementation Patterns ISLAMU Event Can Benefit From

> Progressive report started 2026-05-22. Scope is intentionally asymmetric: **crmworx-api is analyzed deeply**, while **ISLAMU Event is used only through documentation context** (`docs/`, AGENTS/skills) as requested.

## 1. Executive Snapshot

CRMWorx API is a Java/Spring Boot, Maven multi-module, enterprise SaaS CRM backend using Clean Architecture + CQRS via PipelinR, PostgreSQL/Hibernate, Keycloak, Cerbos, Redis, RabbitMQ, Mailpit/SMTP, Loki, and OpenTelemetry. Its most immediately transferable strength for ISLAMU Event is not a single library choice, but a production-grade asynchronous reliability posture: domain workflows create durable intent rows, RabbitMQ carries pointer-only dispatch events, consumers perform side effects under manual acknowledgements, and the database remains the authoritative ledger for publish, delivery, retry, receipt, and ambiguous-outcome states.

ISLAMU Event already documents a strong .NET baseline: Clean Architecture + CQRS/MediatR, PostgreSQL/EF Core, HAL/HATEOAS, multi-tenancy, OpenTelemetry/Serilog, idempotency, and a general outbox processor. The gap that crmworx most clearly illuminates is deeper operational maturity around **business-specific asynchronous workflows**: queue-first email dispatch, automation/sequence execution, RabbitMQ topology, SMTP delivery controls, tenant throttling/circuit breakers, poison-message parking/replay, and failure-matrix-driven tests.

## 2. Evidence Baseline

### 2.1 CRMWorx repository shape

Observed source paths:

- Root Maven aggregator: `/home/amir/Oppworx/Github/crmworx-api/pom.xml`.
- Modules discovered: `crmworx-domain`, `crmworx-application`, `crmworx-persistence`, `crmworx-infrastructure`, `crmworx-api`, `crmworx-architecture-tests`, `crmworx-perf`.
- Root `README.md` identifies the product as a closed-source enterprise multi-tenant SaaS, optimized for managed operations rather than broad self-hosting variability.
- Root `docker-compose.yml` provisions PostgreSQL, Keycloak, Redis, RabbitMQ, Mailpit, Cerbos PDP, Loki, OTEL Collector, and the API.

### 2.2 ISLAMU Event docs-only context used

Only Event documentation/context was read. Key constraints that frame transferability:

- Event architecture: Clean Architecture + CQRS + BFF; API host `Explore.API`, Blazor BFF/client, PostgreSQL/EF Core.
- Event request flow: middleware → controller → MediatR → handler → repository → DTO mapping → HAL assembler.
- Event outbox docs: `OutboxProcessor` polls `outbox_messages`, dispatches through `IOutboxMessageDispatcher`, retries with exponential backoff, dead-letters after max retries, and has specialized `PdsSyncOutbox` / `PolicyChangeOutbox` variants.
- Event API docs say the current general outbox dispatcher is a logging/no-op dispatcher unless replaced, and analytics reliability work explicitly defers buffered/outbox delivery.
- Event hard constraints: repositories return entities, validators are manually instantiated, tenant isolation is central, HAL links gate UI affordances, and every source/doc file starts with two `ABOUTME` lines.

## 3. First Major Transfer Area: Queue-First Email Dispatch

### 3.1 What CRMWorx implemented

CRMWorx has a completed workstream under `dev/testing/automation-sequence-rabbitmq-email/` titled **Sequence + Automation RabbitMQ Email Dispatch**. The context and plan documents describe implementation as complete and identify exact runtime responsibilities:

- Sequence and automation actions create durable email intent in `email_outbox`.
- `EmailOutboxPublishService` claims pending outbox rows and publishes pointer-only `EmailDispatchRequestedEvent v1` messages.
- `RabbitMQEmailDispatchPublisher` requires mandatory routing, correlated publisher confirms, and return handling before marking a message published.
- RabbitMQ topology includes email dispatch exchange/queues, DLQ, replay, priority, and parking routes.
- `RabbitMQEmailDispatchListener` consumes with manual acknowledgements (`basicAck`, `basicReject`) and routes poison messages to DLQ.
- `EmailDispatchConsumeService` validates tenant/event/outbox alignment, records idempotent consume receipts, persists delivery attempts, maps SMTP outcomes, and defers retryable work safely.
- `SmtpEmailOutboxRelayClient` is retained as the SMTP transport adapter, but the legacy direct DB-polling scheduler path was removed so queue-first dispatch is the single production owner.
- Runtime controls include global pause, tenant pause, per-tenant circuit-open deferral, tenant workload throttling, and deferred retry scheduling through DB state.
- Observability includes `crmworx.email.dispatch.events{stage,outcome,source_type,priority}` plus structured non-PII logs with `eventId`, `tenantId`, `emailOutboxId`, `sourceType`, `priority`, `correlationId`, and `causationId`.

### 3.2 Why this matters for ISLAMU Event

ISLAMU Event already documents SMTP health checks and notification/email boundaries, but crmworx demonstrates a more advanced pattern for high-consequence email side effects:

1. **Do not treat SMTP as a simple background poll.** Model email as durable business intent first, then broker dispatch work through a queue.
2. **Keep email body/recipients out of broker messages.** Publish pointer-only events containing identifiers, schema version, tenant, correlation/causation, source, and priority; resolve body and SMTP credentials server-side.
3. **Split publish state from delivery state.** A row can be pending publish, retrying publish, broker-published, delivery retry scheduled, sent, permanently failed, or unknown.
4. **Treat ambiguous SMTP outcomes as first-class.** Timeouts after SMTP handoff may not be safe to blindly retry; crmworx models `UNKNOWN` so operators can reconcile instead of pretending exactly-once email exists.
5. **Use RabbitMQ for transport reliability, not as the business ledger.** Rabbit owns transport redelivery, DLQ, replay, and poison routing; the database owns business retry, delivery attempts, tenant controls, and audit history.

## 4. Immediate Recommendation For Event

Create an Event-specific design track for **Queue-First Notification/Email Dispatch** that adapts crmworx's design to .NET and the existing Event outbox conventions:

- Add a specialized `EmailDispatchOutbox` or extend notification/email intent rows rather than overloading the generic `OutboxMessage` blindly.
- Implement a publisher worker that persists a stable publish event ID, publishes pointer-only messages to RabbitMQ, and only transitions to broker-published after confirm/no-return.
- Implement an idempotent manual-ack consumer that validates tenant and outbox row ownership before sending SMTP.
- Add delivery attempts, consume receipts, retry/dead-letter states, and `Unknown` outcome support.
- Add tenant-level pause/throttle/circuit controls so one broken tenant SMTP configuration cannot starve the platform.

This is a stronger target than simply “add RabbitMQ” because the key value is the state machine and operational control plane around side effects.

## 5. Reliability Pattern: State Machines Before Infrastructure

CRMWorx's most transferable lesson is that RabbitMQ was introduced only after the business state model was made explicit. The implementation plan first locked the state machine, retry ownership, payload privacy, and crash semantics; only then did it add publisher and consumer adapters.

### 5.1 Evidence from crmworx

The workstream plan says the future state is:

```text
Sequence / Automation
        ↓
EmailDispatchRequestPort (enqueue intent)
        ↓
email_outbox (canonical durable source)
        ↓
EmailOutboxPublishService
        ↓
RabbitMQ pointer event (no body/PII)
        ↓
RabbitMQEmailDispatchListener (manual ack)
        ↓
DeliverEmailOutbox use case
        ↓
SMTP sender adapter
        ↓
email_outbox + attempts + receipts updated
```

That flow matters because each arrow has a different failure contract:

- Handler/application code owns creation of durable intent.
- The database owns business delivery state, retry scheduling, receipts, and auditability.
- RabbitMQ owns transport redelivery, DLQ routing, replay handoff, and queue separation.
- SMTP remains an adapter, not the orchestration owner.
- Operational controls live outside the SMTP adapter so dispatch can be paused, throttled, or circuit-opened without code redeploys.

### 5.2 Benefit for ISLAMU Event

Event already documents a general polling `OutboxProcessor` and specialized outboxes for federation and policy changes. crmworx suggests Event should avoid a single generic outbox becoming a dumping ground for every side effect. Instead, Event can keep the generic outbox for low-risk integration events while introducing specialized state machines where the business consequence is high:

| Event workflow | crmworx-inspired improvement |
|---|---|
| Notification/email sending | Dedicated email/notification dispatch intent rows with publish state, delivery state, attempt history, receipts, and `Unknown` outcome. |
| Analytics delivery | Move from deferred buffered reliability to an outbox-backed analytics delivery ledger with explicit publish and dispatch status. |
| Federation/AT Protocol sync | Apply the same confirm/receipt/failure-matrix thinking already used in crmworx email to Event's specialized `PdsSyncOutbox`. |
| Policy/settings propagation | Treat policy-change dispatch as an operable workflow with replay, parking, idempotency, and low-cardinality metrics. |

The concrete recommendation is to design every high-consequence async feature around a **domain-specific state machine first**, then choose whether a poller, RabbitMQ, or another broker should transport the work.

## 6. Testing Pattern: Crash/Fault Matrix As Acceptance Criteria

CRMWorx's email workstream explicitly lists mandatory integration/container scenarios rather than relying on ordinary unit tests. The matrix covers the edges that usually cause duplicate side effects or invisible message loss:

1. outbox row created but publisher not yet run;
2. publisher claims row then crashes before publish;
3. publish confirm received, then crash before database update;
4. unroutable publish through return handling;
5. broker nack;
6. duplicate consume event;
7. tenant mismatch between message and outbox row;
8. missing referenced outbox row;
9. consumer crash before SMTP;
10. SMTP success then crash before DB update;
11. successful DB update but ack failure/redelivery;
12. permanent SMTP failure;
13. transient SMTP failure;
14. ambiguous SMTP timeout mapped to `UNKNOWN`;
15. tenant SMTP disabled or missing;
16. tenant throttle exceeded;
17. DLQ replay must not resend an already-sent message.

### 6.1 Benefit for ISLAMU Event

This matrix is directly portable as a design-review checklist for Event's outbox and background-service work. Event's architecture tests already enforce layer boundaries and conventions; crmworx shows the next layer of maturity: **failure-mode tests that prove operational semantics**.

For Event, this means future work should add test cases that prove:

- idempotent dispatch under at-least-once execution;
- optimistic claim behavior under concurrent processors;
- retry delay and max-retry transitions;
- dead-letter rows remain inspectable and replayable;
- tenant isolation is revalidated at consume/dispatch time, not trusted from message headers;
- ambiguous external side effects have explicit states and operator procedures;
- Rabbit/broker failures do not mark business work complete prematurely.

The practical report recommendation is to create an Event `AsyncWorkflowFailureMatrix` template under dev docs or testing docs and require each new specialized outbox/background workflow to fill it before implementation.

## 7. Operations Pattern: Local Stack Mirrors Production Concerns

CRMWorx's `docker-compose.yml` is not only a convenience file. It encodes the product's operational architecture: PostgreSQL, Keycloak, Redis, RabbitMQ management, Mailpit, Cerbos, Loki, OTEL Collector, and the API all run together with health checks and dependency ordering. The API container gets explicit environment wiring for SMTP, automation execution, RabbitMQ publisher confirms/returns, email dispatch exchange/queue/DLQ names, consumer enablement, and OpenTelemetry export.

### 7.1 Benefit for ISLAMU Event

Event already documents Aspire, OpenTelemetry, Prometheus/Loki/Serilog, SMTP health checks, and request/tenant/correlation observability. crmworx reinforces the value of an environment where reliability features can be exercised locally as a system, not only as isolated unit tests.

Actionable Event takeaways:

- Keep local orchestration close to production topology for high-risk workflows: database, broker, SMTP sink, identity, authorization, logs, and traces should be available together.
- Make queue and worker toggles explicit so rollout can stage publisher and consumer independently.
- Add broker health and publisher-confirm settings to operational docs before implementing queue-first workflows.
- Treat Mailpit-like SMTP sinks as required test infrastructure for email/notification flows.
- Ensure metrics/logs are available during local failure-matrix testing, not added only after production issues.

The strongest adaptation is not copying Docker Compose directly into Event; Event uses .NET/Aspire patterns. The benefit is the operational philosophy: **developer environments should let engineers rehearse real failure, replay, and observability workflows before production**.

## 8. Architecture Pattern: Ordered CQRS Pipeline As A Tested Contract

CRMWorx uses PipelinR middleware as a first-class application pipeline, not merely a handler dispatch mechanism. Representative evidence:

- `crmworx-infrastructure/.../pipeline/IdempotencyMiddleware.java` runs at `@Order(4)`, uses Redis `SETNX`-style `setIfAbsent`, stores a 24-hour in-progress/completed marker, scopes keys by tenant when the command implements `TenantScopedCommand`, replays completed responses, rejects duplicates, and clears the marker on command failure.
- `crmworx-infrastructure/.../pipeline/AuthorizationMiddleware.java` runs at `@Order(6)`, supports explicit `AuthorizedRequest`, annotation-based `@AuthorizeResource`, and `SecureRequest`, calls the canonical authorization service, records authorization metrics, and propagates Cerbos degraded-mode exceptions rather than silently allowing access.
- `crmworx-api/src/test/java/.../pipeline/MiddlewareOrderTest.java` asserts every middleware has an order, no two middleware share an order, idempotency runs before validation, validation runs before authorization, authorization runs before tenant context, and tenant context runs before timeout.

The effective pipeline contract is:

```text
Performance → Correlation → Tracing → Logging → Idempotency → Validation → Authorization → TenantContext → Timeout → Handler
```

### 8.1 Benefit for ISLAMU Event

Event already uses CQRS/MediatR and documents middleware/controller/handler flow. crmworx shows how to make the MediatR pipeline itself an enforceable contract:

- Add or strengthen tests that assert pipeline behavior ordering, especially validation before authorization, idempotency before side effects, tenant context before persistence, and timeout/performance instrumentation around handlers.
- Treat idempotency as a command contract, not a controller convention. In .NET terms, commands that need deduplication can carry an idempotency key and tenant identifier, while a MediatR pipeline behavior performs `IDistributedCache`/Redis-backed claim, replay, reject, and cleanup semantics.
- Record business metrics inside pipeline behaviors so every command/query gets consistent duration, success/failure, and authorization decision telemetry.
- Preserve Event's rule that validators are manually instantiated where required by the repository contract. crmworx's validation pipeline is conceptually useful, but Event must adapt it without violating the documented validator-instantiation rule.

The main lesson is that cross-cutting behavior should be **ordered, tested, and observable**, not spread across controllers or handlers.

## 9. Tenant And Authorization Pattern: Fail Closed, Bind Context, Gate Links

CRMWorx has a layered tenant/security posture that aligns strongly with Event's HAL-affordance rule while adding useful operational details.

### 9.1 Tenant context binding

`TenantContextRequestFilter.java` resolves tenant context from canonical `/t/{slug}/api/**` routes or JWT tenant claims before business handling starts. It rejects malformed/untrusted hints, unknown tenants, forbidden bindings, and tenant-scoped requests without context. When resolution succeeds, it writes tenant identifiers into MDC and runs the remaining request inside a tenant context scope.

Transferable Event ideas:

- Keep tenant resolution as an explicit early request-stage concern rather than rediscovering tenant context inside handlers.
- Emit tenant-resolution metrics with source/outcome tags, but avoid high-cardinality tenant IDs in metric labels.
- Include tenant identifiers in structured logs/correlation context for diagnostics, while keeping sensitive or unbounded data out of metrics.
- For async consumers, repeat tenant validation against the authoritative database row; do not trust broker message tenant fields alone.

### 9.2 Authorization and HAL link gating

`HateoasAuthorizationEvaluator.java` evaluates links in three gates: static rule, business-state rule, then permission requirement. It batches authorization checks for collections, memoizes decisions in a request cache, records metrics, and fails closed when the principal is missing, batch evaluation throws, or a decision is missing. Suppressed links are logged with resource/action/rel/outcome context.

This is directly relevant to Event because Event docs say HAL links are the single source of truth for UI action affordances. crmworx strengthens that rule with two concrete practices:

1. **Permission checks should be batchable and cached per request.** This avoids N+1 policy calls when rendering collection resources with affordance links.
2. **Authorization uncertainty should suppress links, not emit them.** Missing context, evaluator exceptions, and missing decisions all result in denied/suppressed affordances.

Event should preserve its own policy stack and HAL conventions, but can borrow the evaluator shape: static predicates + business predicates + policy decision + request-scoped cache + suppression metrics.

## 10. API Error Pattern: Problem Details With Safe Validation Shape

`GlobalExceptionHandler.java` maps application/domain/security failures to Spring `ProblemDetail` responses. It includes specific handlers for not found, business rule conflict, duplicate command, authorization unavailable with `Retry-After`, forbidden, validation exceptions, type mismatch, missing parameters, and malformed JSON. Validation errors become a `violations[]` array with `field`, `message`, stable code, and redacted rejected value.

### 10.1 Benefit for ISLAMU Event

Event already documents RFC 7807 ProblemDetails with trace/correlation metadata. crmworx adds useful refinements:

- Duplicate/idempotency failures should have a stable problem type/title and include the idempotency key only when safe.
- Authorization-provider outage should map to `503` with a retry hint instead of collapsing into a generic `500` or accidental allow.
- Validation details should use stable machine-readable codes derived from constraint names, while rejected values are redacted by field sensitivity.
- Malformed JSON, type mismatch, and missing parameter errors should all share one predictable `violations[]` structure.

For Event, this is a design-quality opportunity: make API errors not just standards-compliant, but operationally and client-usefully consistent across validation, idempotency, authorization outage, and malformed request cases.

## 11. Persistence Pattern: Explicit Tenant/Soft-Delete Query Composition

CRMWorx persistence is JPA/Hibernate, not EF Core. The useful pattern is still portable: query safety is expressed in reusable composition primitives and guarded base entities.

Evidence:

- `BaseTenantJpaEntity.java` is a mapped superclass with a required immutable `tenant_id` column and a `@PrePersist` guard that throws if a tenant-scoped entity is persisted without `tenantId`.
- `BaseSpecificationBuilder.java` composes JPA `Specification<T>` filters for equality, inequality, case-insensitive contains, `IN`, date ranges, soft-delete (`isDeleted = false`), and tenant scoping (`tenantId = ...`) before combining all specs with `AND`.
- `TenantReadPortAdapter.java` demonstrates cacheable read-port projection (`@Cacheable` on tenant ID and slug lookups) returning `TenantDto` from JPA entities.

### 11.1 Benefit and caution for ISLAMU Event

Event's documented rule is stricter than crmworx in one important area: **Event repositories return entities, never DTOs**. Therefore, Event should not directly copy crmworx read adapters that return DTO projections from persistence ports.

The portable parts are:

- reusable, explicit specification composition for tenant, soft-delete, search, range, and status filters;
- persistence-time guards that prevent tenant-scoped rows being saved without tenant identity;
- cacheable hot-path lookups, adapted so Event keeps DTO mapping in handlers/Application as required;
- tests that prove tenant filters cannot be accidentally omitted.

The recommendation is to translate crmworx's `BaseSpecificationBuilder` concept into Event's existing Application-level `IQuerySpecification<T>` model, while preserving Event's entity-returning repository boundary.

## 12. Observability Pattern: Business Metrics For Policy And Async Lifecycle

`BusinessMetrics.java` centralizes Micrometer counters/timers for command duration/outcome, authorization decisions, authorization decision outcomes, suppressed links, denied-on-error, tenant resolution outcomes, degraded authorization requests, sequence progression, automation execution, email delivery, RabbitMQ publish/consume, and queue-first email dispatch lifecycle metrics.

CRMWorx is careful in the email dispatch plan to keep high-cardinality identifiers out of metric labels; IDs such as `eventId`, `tenantId`, `emailOutboxId`, `correlationId`, and `causationId` are structured-log fields instead.

### 12.1 Benefit for ISLAMU Event

Event already documents OpenTelemetry, Serilog, Prometheus, Loki, correlation IDs, request logging, and business metrics. crmworx adds a useful taxonomy for what to measure in complex workflows:

- policy decisions: allowed/denied/outcome/error/degraded;
- HAL link suppression: resource/action/rel/outcome;
- tenant resolution: source/outcome;
- command/query execution: command name/outcome/duration;
- async lifecycle: publish/consume/delivery stage/outcome/source/priority;
- replay and parking events as operationally visible lifecycle states.

For Event, the actionable rule is: every specialized outbox or background workflow should define its metric dimensions during design, with low cardinality enforced up front and correlation IDs reserved for logs/traces.

## 13. Security Pattern: Authentication-Only IdP, Policy-Owned Authorization

CRMWorx draws a sharp line between authentication and authorization:

- Keycloak/OIDC validates identity and token shape.
- JWT principal resolution uses a stable fallback order (`sub` → `nameidentifier` → `sid`).
- Keycloak role claims are intentionally ignored for resource authorization.
- DB-sourced role context is assembled into a Cerbos principal.
- Cerbos is the sole resource-authorization provider.
- When Cerbos is unavailable, degraded mode is explicit: production defaults to `READ_ONLY`, writes fail with `503`, `DENY_ALL` is available, and `ALLOW_ALL` exists only for test/OpenAPI profiles.

### 13.1 Benefit for ISLAMU Event

Event already documents strong handler-level authorization and HAL affordance gating. crmworx adds useful operational strictness:

- Avoid using identity-provider roles as business authorization truth. Treat OIDC/JWT as authentication and source business roles from Event's own tenant/domain model or policy system.
- Define explicit degraded-mode behavior for any external authorization dependency: deny all, read-only, or fail closed by resource class. Do not silently fall back to stale local role checks unless that is a deliberate, tested preservation mode.
- Surface authorization-provider outage as a distinct ProblemDetails response with retry guidance, not as generic failure.
- Keep policy action vocabulary documented and tested against link/handler requirements, so UI affordances, API handlers, and policy files cannot drift.

The key Event takeaway is governance: security behavior should be traceable from route policy → handler/pipeline requirement → policy decision → HAL link emission → tests.

## 14. Configuration Pattern: Document Only What Is Actually Wired

`docs/CONFIGURATION.md` is valuable because it distinguishes current runtime configuration from older or planned settings. It lists static Spring/management/security sections, then enumerates active `crmworx.*` custom properties consumed by code: Cerbos target/health/TLS, pipeline timeout, sequence/automation processing, tenant/workload throttles, queue-first email publish controls, SMTP controls, dispatch-processing pause/circuit settings, RabbitMQ queue/DLQ/replay settings, rate limits, CORS/CSP, platform-admin bootstrap, JWT audiences, route policies, tenant resolution, degraded authorization, SLO thresholds, and integration outbox relay controls.

It also explicitly marks themes **not wired in the current Java runtime**: deployment mode switching, secret-provider prefix, Infisical integration, and database-backed cascading settings.

### 14.1 Benefit for ISLAMU Event

This is a strong documentation hygiene practice for Event:

- Separate **implemented runtime configuration** from planned architecture and legacy names.
- For every config property in docs, identify the owning code path and operational purpose.
- Mark removed/legacy settings as removed so operators do not tune dead switches.
- Keep local-development defaults, test-profile overrides, and production defaults visibly separate.
- For future queue-first workflows, document publisher, consumer, retry, DLQ replay, parking, pause, and circuit settings before rollout.

This matters because configuration drift is an operational bug: a documented toggle that is not wired is worse than no toggle, because it gives operators false confidence during incidents.

## 15. Governance Pattern: Context Engineering As CI-Enforced Architecture

CRMWorx mirrors Event's own agentic documentation culture. Its architecture docs describe a contract-first context system:

- `AGENTS.md` is the canonical entry contract.
- `.claude/contract/intents.yaml` maps task types to must-read docs, rules, skills, in-scope paths, tests, and docs updates.
- `.claude/rules/*.md` constrain behavior by edited module.
- `.claude/skills/*/SKILL.md` provide focused workflows.
- `crmworx-architecture-tests` validates schema, links, manifest integrity, duplication drift, and benchmark coverage.

### 15.1 Benefit for ISLAMU Event

Event already has a very similar contract system. The benefit here is reinforcement rather than novelty:

- Keep architecture/context tests as mandatory CI gates, not optional docs checks.
- Enforce link/action/policy parity with tests where possible, especially for HAL affordance gating.
- Treat dev docs and active workstream context as first-class implementation artifacts for complex async/security work.
- Use intent-specific minimum tests plus a broader context gate when rules/skills/docs change.

The crmworx lesson is that agentic engineering rules only stay useful when they are backed by automated drift detection.

## 16. Transferability Matrix

| CRMWorx pattern | Event benefit | Porting difficulty | Event-specific caution |
|---|---|---:|---|
| Queue-first email dispatch with pointer events | Strong notification/email reliability, replay, and tenant controls | High | Must adapt to .NET hosted services, EF Core, MediatR, and Event's existing outbox variants. |
| Explicit publish/delivery/receipt state | Better auditability and recovery than generic `OutboxMessage` only | Medium | Keep specialized state machines for high-consequence workflows; do not overcomplicate simple events. |
| Crash/failure matrix tests | Prevent duplicate sends and invisible message loss | Medium | Add proportional tests; not every workflow needs all 17 scenarios. |
| Ordered CQRS pipeline tests | Prevent cross-cutting behavior drift | Medium | Respect Event's manual validator rule. |
| Redis idempotency middleware | Tenant-scoped duplicate protection for write commands | Medium | Pair cache dedupe with durable DB constraints where side effects are irreversible. |
| Cerbos-style fail-closed HAL gating | Strong UI affordance correctness | Medium | Event should use its own policy stack, but keep request-scoped batching/cache and fail-closed behavior. |
| Tenant context request binding | Cleaner tenant diagnostics and less handler boilerplate | Medium | Event already uses EF tenant filters; async consumers must still revalidate tenant/outbox ownership. |
| ProblemDetails validation shape | Better client ergonomics and supportability | Low | Preserve Event's existing RFC 7807 extensions (`traceId`, `timestamp`, `correlationId`). |
| Low-cardinality business metrics | More useful Prometheus/Loki dashboards | Low | Keep tenant/resource IDs in logs/traces, not metric labels. |
| Wired-vs-planned configuration docs | Less incident confusion | Low | Update docs whenever toggles are added, removed, or renamed. |

## 17. Recommended Event Roadmap From This Analysis

1. **Design a Queue-First Notification/Email Dispatch PRD/tech spec.** Scope state machine, DB schema, RabbitMQ topology, SMTP adapter, tenant controls, and failure matrix before coding.
2. **Create an Async Workflow Failure Matrix template.** Use crmworx's 17 cases as the seed, then tailor per workflow.
3. **Harden MediatR pipeline tests.** Assert ordering and invariants for correlation, performance, validation, authorization, idempotency, tenant context, and timeout behavior.
4. **Add request-scoped HAL authorization batching/cache guidance.** This directly supports Event's documented “HAL links are the single source of truth” rule.
5. **Audit configuration docs against wired code paths.** Separate implemented toggles from planned or removed settings, especially around outbox/SMTP/analytics reliability.
6. **Define authorization degraded-mode semantics.** If any external policy provider is introduced or hardened, decide fail-closed/read-only behavior and test it.
7. **Expand business metrics by workflow lifecycle.** For each high-consequence background workflow, define low-cardinality stage/outcome metrics plus structured log fields.

## 18. Messaging Deep Dive: Mature Email Dispatch Versus Generic Integration Relay

The messaging deep dive shows that crmworx has two different reliability maturity levels. The **email dispatch path** is production-shaped and carefully modeled. The **generic integration outbox relay** exists and has useful pieces, but is less complete and should be treated as a cautionary pattern rather than copied wholesale.

### 18.1 Email dispatch path: strongest implementation

Representative files:

- `crmworx-domain/src/main/java/com/oppworx/crmworx/domain/entity/EmailOutbox.java` models email intent and lifecycle state.
- `crmworx-persistence/src/main/resources/db/migration/V3_47__create_email_outbox.sql`, `V3_51__add_email_outbox_routing_and_attempt_history.sql`, and `V3_52__add_email_dispatch_publish_and_receipt_contracts.sql` define the durable schema for publish state, routing, attempts, receipts, idempotency, and tenant isolation.
- `crmworx-application/src/main/java/com/oppworx/crmworx/application/features/email/publish/EmailOutboxPublishService.java` owns polling, claiming, stable publish-event IDs, publish retries, and publish-state transitions.
- `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchPublisher.java` publishes pointer-only events with correlated publisher confirms and mandatory return checks.
- `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchListener.java` consumes the standard dispatch queue with manual acknowledgement semantics.
- `crmworx-application/src/main/java/com/oppworx/crmworx/application/features/email/consume/EmailDispatchConsumeService.java` performs idempotent receipt claim, tenant/event/outbox validation, SMTP delivery, retry scheduling, dead-letter transitions, and ambiguous-outcome handling.
- `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchDeadLetterReplayListener.java` handles guarded DLQ replay and parking.

`RabbitMQEmailDispatchPublisher` is the key proof point for publish safety. It creates `CorrelationData` from the event ID, calls `rabbitTemplate.send(...)`, waits for the confirm future using `email-dispatch-publish-confirm-timeout-ms`, rejects missing/nacked confirms, and rejects returned unroutable messages before the application can mark an outbox row published. That is the right boundary: broker-accepted transport state becomes a prerequisite for database publish-state transition.

`RabbitMQEmailDispatchListener` is the key proof point for consume safety. It consumes only after `crmworx.messaging.rabbitmq.enabled=true` and `email-dispatch-consumer-enabled=true`, uses the manual-ack listener factory, parses `EmailDispatchRequestedEvent`, runs the consume service inside an internal-worker tenant context, `basicAck`s successful or safely terminal work, `basicReject`s poison/DLQ dispositions, and `basicNack`s unexpected transient failures for requeue.

### 18.2 Queue topology: explicit, but verify every declared route is consumed

`RabbitMQConfig.java` declares:

- `crmworx.email.dispatch.v1.exchange` topic exchange;
- standard queue bound to `email.dispatch.standard`;
- priority queue bound to `email.dispatch.priority`;
- dead-letter exchange and DLQ bound to `email.dispatch.standard.dlq`;
- parking queue bound to `email.dispatch.parking`;
- manual-ack listener container factory for normal dispatch;
- manual-ack listener container factory for DLQ replay.

The verified caution is that `RabbitMQEmailDispatchPublisher.resolveRoutingKey(...)` routes `CRITICAL`, `HIGH`, and `PRIORITY` messages to the priority routing key, while `RabbitMQEmailDispatchListener` is annotated with only:

```java
@RabbitListener(
        queues = "${crmworx.messaging.rabbitmq.email-dispatch-queue:crmworx.email.dispatch.standard.q}",
        containerFactory = "crmworxEmailDispatchListenerContainerFactory"
)
```

I found no equivalent listener for `email-dispatch-priority-queue` in the verified source search. That is a valuable lesson for Event: declaring priority topology is not enough. A queue-first design must prove that every route has an enabled consumer, test coverage, observability, and an operator story.

### 18.3 Generic integration outbox: useful shape, weaker delivery guarantees

CRMWorx also has a generic integration outbox:

- `crmworx-domain/src/main/java/com/oppworx/crmworx/domain/entity/IntegrationOutbox.java` models idempotency, retry, and dead-letter state.
- `crmworx-persistence/src/main/resources/db/migration/V3_27__create_integration_outbox.sql` creates the generic integration outbox table.
- `crmworx-application/src/main/java/com/oppworx/crmworx/application/features/integration/support/IntegrationOutboxPublisher.java` enqueues tenant-scoped integration work.
- `crmworx-application/src/main/java/com/oppworx/crmworx/application/features/integration/relay/IntegrationOutboxRelayService.java` polls, dispatches, retries, and dead-letters rows.
- `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/integration/OutboxRelayScheduler.java` drives relay processing.
- `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/integration/HttpOutboxRelayClient.java` dispatches over HTTP with HMAC signing.
- `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQOutboxRelayClient.java` dispatches over RabbitMQ when configured.

The generic relay should not be treated as equally mature as the email dispatch path. `HttpOutboxRelayClient` returns silently when `endpointUrl` is blank, and HTTP is the default transport when `crmworx.integrations.outbox-relay.transport` is not set. `RabbitMQOutboxRelayClient` publishes with `rabbitTemplate.send(...)` and metrics, but does not perform the correlated confirm/return checks used by `RabbitMQEmailDispatchPublisher`. `IntegrationEventRelay.java` is an empty retained cleanup shell with an ABOUTME note saying it was superseded by `OutboxRelayScheduler + HttpOutboxRelayClient` and will be removed once references are confirmed gone.

### 18.4 Benefit for ISLAMU Event

Event should borrow the email pipeline's rigor, not the generic relay's weaker defaults:

- Queue-first workflows should fail fast on missing endpoints, missing exchanges, unroutable messages, or unconsumed queues.
- If RabbitMQ is used for a specialized outbox, publisher confirms and mandatory return handling should be required for publish-state transitions.
- If HTTP relay is used, blank endpoints should be configuration errors or disabled states with explicit health indicators, not silent success.
- Empty retained integration classes should be cleaned up promptly because stale placeholders confuse implementation agents and operators.
- Declared priority/DLQ/parking routes need tests proving end-to-end behavior, not only bean declarations.

## 19. Implementation Checklist For Event Queue-First Workflows

Based on the crmworx messaging deep dive, an Event implementation plan for a high-consequence async workflow should include this checklist before coding:

1. **Durable intent table:** specialized rows for the workflow, using Event's UUIDv7/Guid conventions and tenant isolation rules.
2. **Publish state:** pending, claimed/processing, publish retry scheduled, broker published, publish failed/dead-lettered.
3. **Delivery state:** pending delivery, retry scheduled, sent/completed, permanent failure, `Unknown`/ambiguous external outcome.
4. **Stable message identity:** event ID, outbox row ID, tenant ID, idempotency key, schema version, correlation ID, causation ID.
5. **Pointer-only broker payload:** no email body, secrets, recipient PII, or large business snapshots in Rabbit messages.
6. **Confirm-gated publisher:** broker confirm/no-return required before marking published.
7. **Manual-ack consumer:** ack/reject/nack behavior mapped explicitly to success, poison, terminal failure, and transient failure.
8. **Receipt/idempotency ledger:** consumer claim table or equivalent uniqueness guard so redelivery cannot duplicate external side effects.
9. **Tenant revalidation:** consumer rechecks tenant/outbox ownership from the database; message headers are hints, not authority.
10. **Replay and parking:** DLQ replay must avoid resending already-completed work and must have a parking route for unsafe messages.
11. **Operational controls:** global pause, tenant pause, tenant throttle, circuit-open deferral, and controlled replay.
12. **Metrics and logs:** low-cardinality stage/outcome/source/priority metrics; IDs in structured logs/traces only.
13. **Misconfiguration behavior:** disabled feature flags and missing endpoints must be visible in health/config docs.
14. **Failure matrix:** crash windows, duplicate delivery, broker failure, unroutable publish, SMTP/API ambiguity, tenant mismatch, missing row, and replay cases tested.

This checklist translates crmworx's Java/Spring implementation into Event's .NET/Clean Architecture terms without copying framework-specific boundaries or violating Event's repository and validator rules.

---

## Progressive Analysis Log

- **2026-05-22 initial pass:** Read Event docs-only context (`docs/index.md`, `docs/QUICK_REFERENCE.md`, `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/OPERATIONS.md`) and crmworx root/project/config/planning files. Launched parallel crmworx-focused explore agents for messaging and architecture deep dives.
- **2026-05-22 queue-first deepening:** Expanded report with crmworx's state-machine-first design, explicit crash/failure matrix, and local operations topology from `docker-compose.yml` and `dev/testing/automation-sequence-rabbitmq-email/*`.
- **2026-05-22 architecture-agent synthesis:** Collected architecture deep-dive findings and verified representative crmworx files for PipelinR middleware order, Redis idempotency, tenant context binding, Cerbos/HAL link gating, ProblemDetail errors, JPA specification composition, tenant entity guards, cacheable read adapters, and business metrics.
- **2026-05-22 docs/config synthesis:** Read crmworx `docs/ARCHITECTURE.md`, `docs/SECURITY.md`, `docs/MULTI_TENANCY.md`, `docs/OPERATIONS.md`, and `docs/CONFIGURATION.md`; added security, configuration, governance, transferability, and roadmap sections.
- **2026-05-22 messaging-agent synthesis:** Collected messaging deep-dive findings and verified `RabbitMQConfig.java`, `RabbitMQEmailDispatchPublisher.java`, `RabbitMQEmailDispatchListener.java`, `RabbitMQOutboxRelayClient.java`, `HttpOutboxRelayClient.java`, `IntegrationEventRelay.java`, and `application.yml`; added sections distinguishing mature queue-first email dispatch from weaker generic integration relay patterns.
