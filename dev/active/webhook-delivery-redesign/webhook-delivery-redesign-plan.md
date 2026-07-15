<!-- ABOUTME: Approved architecture and gated implementation plan for reliable inbound and outbound webhook delivery. -->
<!-- ABOUTME: Defines transactional processing, provider publication, tenant isolation, security, operations, and release gates. -->

# Webhook Delivery Redesign Plan

Last Updated: 2026-07-14 Europe/Brussels

## Status

- Planning status: **Approved**
- Implementation status: **Not started**
- Architecture direction: **Approved**
- First implementation gate: rebaseline source and migrations, then complete Phase 0A before reliability work
- Scope of this revision: planning documents only; no production code or schema changed

This plan supersedes the earlier draft. Implementation must not revive an
alternative model where processing correctness is represented only by inbox
settlement, Svix publication state is stored on `WebhookMessage`, callers choose
App Portal authority, or workers derive tenant context from ambient state.

## Executive Decision

The platform will support two independently operable outbound modes:

1. **Local** is the default and must remain production-capable without Svix,
   Redis, Kafka, CDC, or another mandatory service.
2. **Svix** is optional. It adds provider-native delivery and management
   capabilities without becoming the authority for platform tenancy,
   authorization, event semantics, audit, or configuration governance.

Both modes share one immutable semantic event, `WebhookMessage`. Each required
external provider submission is represented by a dedicated
`WebhookProviderPublication`. Inbound processing uses a transactional inbox:
the business effect, effect receipt, durable external-operation outbox when
needed, and inbox settlement commit atomically in the local database.

Delivery and processing are at-least-once. Exactly-once transport is not
claimed. Duplicate business effects are prevented by persisted identities,
unique constraints, transactional receipts, and idempotent consumers.

## Goals

- Close the crash gap between an inbound business mutation and inbox settlement.
- Make every background operation execute inside an explicit persisted tenant boundary.
- Make Local delivery a complete, fair, bounded, observable production mode.
- Bound Svix recovery to documented and conformance-tested guarantees.
- Remove caller-controlled App Portal capabilities immediately.
- Preserve immutable routing and security decisions for already-materialized work.
- Make audit, retention, payload access, replay, and migration release requirements.
- Keep UI authority exclusively in API-produced HAL relations.

## Non-Goals

- Exactly-once network delivery.
- A new message broker, cache, CDC pipeline, or mandatory proxy tier.
- Provider feature parity where Svix has a native capability Local does not.
- Caller-selected portal capabilities or trusted legacy provider identifiers.
- ERP ownership in the mandatory delivery phases.
- CloudEvents, Ed25519, arbitrary provider plugins, or speculative abstractions.
- Compatibility shims for contracts removed by this redesign.

## Architecture Invariants

1. `WebhookMessage` is the immutable semantic event and payload envelope.
2. `WebhookProviderPublication` is the only aggregate that owns provider
   submission and reconciliation state.
3. Local endpoint HTTP attempts remain `WebhookDeliveryAttempt` records.
4. Inbox settlement alone never proves that a business effect was applied once.
5. Every processor receives the stable persisted inbound context; it cannot
   substitute provider, tenant, message, payload, or generation identifiers.
6. All background work obtains tenant identity from the claimed persisted row,
   creates a fresh execution scope, and clears that context when disposed.
7. Provider mode, target bindings, contract version, configuration version,
   endpoint version, and retention decisions are snapshots for existing work.
8. Configuration changes affect newly materialized work only unless an explicit,
   authorized, audited migration action changes pending work.
9. Local-only startup and operation are first-class release gates.
10. HAL relations are the UI's only authority for actions.
11. Secrets, signatures, payloads, portal URLs, and raw provider errors never
    enter logs, traces, safe audit metadata, or default list/detail responses.
12. Provider behavior not guaranteed by documentation is unavailable until a
    supported-version conformance test proves it.
13. `IgnoreQueryFilters` is exceptional: every use needs a written reason and an
    explicit tenant predicate before tenant-owned data is returned or mutated.
14. Migrations may break obsolete contracts but may not lose data, trust
    unverified legacy identifiers, take unbounded locks, or omit rollback recovery.

## Ownership Boundary

Phase 7 makes webhook configuration ownership explicit for Instance, Tenant,
Organization, Group, and User scopes. Every consumer has one normalized owner
kind and one matching typed owner reference. Organization, Group, and User
owners must resolve inside the consumer tenant; instance-owned consumers have no
tenant and resolve to the completed immutable instance identity. Database check
constraints reject missing, mixed, or contradictory owner references.

Tenant query filters hide instance-owned configuration by default. Privileged
instance operations must disable only the named tenant filter and apply an
explicit instance/consumer predicate. Event messages and delivery evidence keep
their source `TenantId`; an explicitly targeted instance consumer may receive
events from any tenant without changing the source tenant or weakening tenant
isolation. Authorization resource descriptors carry the canonical owner kind and
owner ID, and Local/Cerbos decisions must remain equivalent for every scope.

ERP integrations remain a separate admission-gated project because they
introduce connector credentials, schemas, scheduling, and data-governance
concerns beyond webhook transport.

## Core Domain Model

### `WebhookMessage`

`WebhookMessage` stores an immutable semantic event:

- `Id` and `TenantId`
- canonical event type and stable event identity
- exact payload bytes or durable payload reference plus `PayloadHash`
- occurred/materialized timestamps
- event-contract version
- resolved retention-policy snapshot
- delivery-plan/configuration version
- required target snapshots

It does not own mutable Svix publish/reconcile state. Materialization creates the
message and all required Local targets/provider publications in one database
transaction. A partially-created delivery plan is invalid.

### `IncomingWebhookMessage`

The inbox stores exact verified input and processing state. It includes:

- `TenantId`, `Provider`, `ProviderMessageId`, and internal message ID
- exact payload bytes/reference and `PayloadHash`
- event type and verification evidence
- state, lease owner, lease expiry, fence/generation, and next attempt
- bounded failure code/detail and timestamps
- redrive generation and provenance

Required terminal outcomes include `Processed`, `Ignored`,
`RejectedPermanent`, and `PayloadConflict`. A duplicate provider identity with
the same hash returns duplicate success. The same identity with another hash is
`PayloadConflict` and is never processed automatically.

Redrive does not rewrite history. `DeadLettered -> RetryDue` creates a new
processing generation and records actor, reason, time, and prior generation.

### `IncomingWebhookEffectReceipt`

Each business effect is protected by a durable receipt containing:

- `TenantId`
- `IncomingWebhookMessageId`
- stable `EffectKind`
- `PayloadHash`
- `ProcessingGeneration`
- applied timestamp
- optional safe result reference

The database enforces uniqueness on
`(TenantId, IncomingWebhookMessageId, EffectKind)`. A local processor commits
its business mutation, receipt, and inbox transition to `Processed` in one
transaction. A processor requiring an external effect commits the durable
outbound operation/outbox, receipt, and inbox settlement in that transaction;
network I/O occurs later. If a receipt already exists with the expected hash, a
retry settles the inbox without replaying the effect. A hash mismatch fails
closed as conflict.

Every processor receives `IncomingWebhookProcessingContext` with at least:

- `IncomingWebhookMessageId`
- `TenantId`
- `Provider`
- `ProviderMessageId`
- `EventType`
- `PayloadHash`
- `ProcessingGeneration`

### `WebhookProviderPublication`

This is one row per message, provider, and immutable provider binding. It owns:

- `TenantId`, `WebhookMessageId`, `ProviderKind`, `ProviderBindingId`
- stable provider event ID and idempotency key
- payload/request hash
- provider/configuration/mode/contract version snapshots
- provider environment and immutable application identity
- external message ID when known
- lease owner, expiry, fence/generation, and state timestamps
- `IdempotencyCredentialVersion` and `IdempotencyValidUntil`
- credential reference/version, never credential value
- bounded failure code/detail
- retry/reconciliation count and next action time
- `LastAutomaticReconciliationAt`

The unique key is
`(TenantId, WebhookMessageId, ProviderKind, ProviderBindingId)`.

States are `Prepared`, `Publishing`, `ProviderQueued`, `RetryDue`,
`PublicationUnknown`, `DeadLettered`, `ManualReconciliation`, and `Abandoned`.
Append-only publication attempts record every submit/retry/reconcile decision.
`WebhookProviderLink` is retired for message publication; the publication row
contains the final external provider mapping. Legacy link data is migrated, not
silently discarded.

### Local Delivery Records

Local delivery has immutable target snapshots plus append-only
`WebhookDeliveryAttempt` evidence. A target records endpoint ID and endpoint
configuration version, exact destination snapshot/reference, signature key
version, payload hash, next action, lease/fence, terminal state, and safe failure
classification. Secret values and response bodies are not retained.

Queued work continues with its endpoint configuration snapshot. A later endpoint
edit applies to new messages. An explicit audited pending-work migration may move
eligible queued work to another version; it never mutates attempts already sent.

## Tenant Execution Boundary

Background workers use two separate authority levels:

1. A privileged coordinator claims bounded cross-tenant candidates. Its query is
   narrowly scoped, returns persisted `TenantId`, and does no tenant business work.
2. Each claimed item runs in a fresh asynchronous DI scope with a fresh DbContext,
   explicit `TenantExecutionContext`, and narrowly scoped machine/system principal.

The item processor verifies that all resource identifiers resolve under the
persisted tenant before mutation. It never reads tenant identity from HTTP,
browser state, claims left by another item, or mutable ambient globals. Disposal
clears the tenant/principal context. Local and Cerbos authorization providers
must return equivalent decisions for these machine actions.

The required concurrency test processes tenant A and tenant B concurrently and
proves no context bleed, no cross-tenant identifier substitution, and no resource
lookup outside the claimed tenant.

## Inbound Processing Flow

1. Authenticate provider and verify the signature over exact request bytes.
2. Derive provider message identity, event type, payload hash, and persisted tenant.
3. Insert or classify the inbox row under a uniqueness constraint.
4. Return duplicate success for the same provider identity and payload hash.
5. Persist `PayloadConflict` for the same identity and a different hash.
6. The coordinator claims eligible work with a lease and generation fence.
7. A tenant-scoped processor starts a local database transaction.
8. It checks the effect receipt and either applies the effect or takes the
   receipt-proven settlement path.
9. It commits business mutation or durable outbox, effect receipt, and inbox
   settlement atomically.
10. It appends bounded attempt/audit evidence and emits low-cardinality telemetry.

A lease expiry may cause concurrent execution, but the effect-receipt unique
constraint and optimistic fence prevent a second committed effect.

## Local Outbound Flow

1. The application transaction writes domain changes and transactional outbox.
2. An idempotent dispatcher materializes `WebhookMessage` and its immutable plan.
3. The Local worker claims a fair, bounded set of targets.
4. It enforces global, per-tenant, and per-endpoint in-flight limits.
5. It signs exact payload bytes and sends one bounded HTTP request.
6. It classifies response/timeout, appends an attempt, and transitions the target.
7. Retry uses exponential backoff with full or equal jitter and a bounded
   `Retry-After` value.
8. Sustained failure opens the endpoint circuit/auto-pauses it according to the
   resolved configuration snapshot. Authorized manual resume is HAL-gated.

Limits are configurable instance defaults with tenant overrides only where
instance governance permits. Required controls include maximum items per tenant
per claim cycle, endpoint timeout, response-body read limit, retry budget, and
auto-pause policy. No generic threshold is hardcoded as universal policy.

## Svix Publication and Reconciliation

`WebhookConsumer` is the Svix application security boundary. The Svix application
UID derives from immutable instance identity plus `WebhookConsumerId`. Multiple
consumers in one tenant receive separate applications whenever portal authority,
endpoint administration, or message visibility differs. Tenant ID alone is not
the application UID.

Publishing uses a stable provider event ID and idempotency key. Automatic retries
are allowed only while all of these remain true:

- current time is before `IdempotencyValidUntil`
- the documented/conformance-proven window still applies
- auth environment, application, and credential scope/version are unchanged
- payload and request hash are unchanged
- the same idempotency key is reused

Svix documents retention of a POST response for the same auth token and
idempotency key for up to 12 hours. After that window or credential rotation,
automatic create retry is forbidden. Lookup-based reconciliation is allowed only
when the supported self-hosted version's conformance suite proves the
lookup semantics. Otherwise the publication becomes `ManualReconciliation`.
The system never creates a fresh provider identity to conceal unknown state.

The conformance suite covers repeat create within the window, expiry, duplicate
event ID with same and changed payload, timeout after provider acceptance,
credential rotation, list/get consistency, and older/unsupported behavior.

## App Portal Authority Containment

Phase 0A removes public `ReadOnly` and `FeatureFlags` inputs from the App Portal
command/API. The server derives the minimum safe capabilities from the verified
`WebhookConsumer` binding and caller authorization. There is no compatibility
shim. A missing, legacy-unverified, disabled, or mismatched binding produces no
portal HAL action and no session.

The response uses `Cache-Control: no-store`. Portal URL/token values are not
logged, traced, cached, or written to audit. Audit records only the issuance fact,
principal, consumer/binding target, capability policy version, result, and time.
The audit fact must be durable before the URL is returned.

## Provider Capability Matrix

Capabilities are typed server-side facts per provider and supported version:

- endpoint management
- provider attempt visibility
- replay
- payload inspection
- App Portal
- event catalog
- provider retention control
- application throttling
- endpoint throttling
- transformations
- ordering
- operational callbacks

They drive startup/config validation, readiness, API HAL actions, UI explanations,
and supported self-hosted Svix checks. Local is not made to claim synthetic parity.
If a configured feature is unsupported, startup or the configuration write fails
with a typed problem; it does not degrade silently.

## Signature Contract

Local outbound signatures conform to the Standard Webhooks baseline using
HMAC-SHA256 over `message-id.timestamp.raw-payload`. Verification/signing uses
exact bytes, supports multiple signature values during rotation, rejects stale or
unreasonably future timestamps, and uses constant-time comparison.

Shared fixtures cover exact bytes, UTF-8/non-ASCII, whitespace/newline mutation,
current/previous/multiple signatures, stale/future time, altered ID/timestamp/
payload, malformed base64, duplicate headers, and tolerance boundaries. Ed25519
is later work and cannot alter the HMAC baseline.

## Authorization and HAL

- GET endpoints remain `[AllowAnonymous]`; handlers perform resource-level checks.
- Write endpoints remain `[Authorize]`; handlers enforce action authorization.
- Machine workers use explicit principal/resource/action checks.
- Local and Cerbos providers must have parity tests.
- Blazor renders actions only when the API supplies the corresponding HAL relation.
- The API omits impossible provider actions based on binding verification,
  capability, state, retention, tenant, and authorization.
- Payload access uses a separate `view-payload` action and HAL relation.

Payload is never included by default. An authorized payload read is tenant-checked,
audited, `no-store`, and returns retention-aware `404` or `410` when unavailable.

## Audit Contract

Append-only write-side audit is mandatory for:

- consumer and endpoint lifecycle
- secret rotation
- provider, mode, configuration, and retention changes
- binding creation, verification, repair, and retirement
- portal session issuance fact
- retry, redrive, reconcile, abandon, and bulk replay
- automatic pause and manual resume
- explicit pending-work migration

Each record contains principal, effective scope, action, target, safe before/after
metadata, policy/configuration version, correlation ID, reason, outcome, and
database timestamp. It excludes payload, secret values, signatures, portal data,
and raw provider errors. The audited business write and audit record commit in the
same transaction where possible. A security-sensitive response is not returned
until its required audit write succeeds.

## Retention Contract

Policies are explicit and separately configurable for:

- inbound raw payload
- outbound payload
- processing and delivery attempts
- dead-letter evidence
- provider publications/external mappings
- operational logs
- administrative audit

Cleanup is tenant-scoped, bounded, observable, and idempotent. It cannot delete
nonterminal work, unknown/reconciliation work, evidence still inside the replay
window, or data under retention hold. Payload cleanup preserves minimum hashes,
identities, outcomes, and audit evidence needed to prove processing.

Svix defaults payload retention to 90 days and supports per-message retention.
The resolved local policy is mapped to the provider option when supported, while
minimal local evidence remains authoritative. Provider delete-on-success is not a
default because it can remove recovery evidence.

## Operations Contract

Mandatory operations include list/detail, retry/redrive, reconcile, abandon,
pause/resume, payload access, and health/readiness. Every affordance is HAL-gated.

Bulk replay is later in the mandatory operations phase and must have preview,
filters, maximum batch, stable idempotency identity, per-tenant limits, reason,
audit, cancellation before start, and exclusions for conflict/unknown work. It
must not bypass endpoint or tenant fairness controls.

Metrics use bounded dimensions such as provider, operation, and outcome. Tenant,
message, endpoint, event ID, and URL are not metric labels. Traces correlate
internal IDs but redact payloads, secrets, signatures, portal values, and raw
provider errors. Readiness reports Local and optional Svix health independently.

## Migration and Legacy-Data Policy

Each schema slice requires:

- deterministic classification of every legacy row
- explicit indexes, foreign keys, and unique constraints
- migration from the latest released schema
- representative-data timing and lock observation
- backup/restore rehearsal
- maintenance-window declaration when needed
- forward-fix strategy; destructive rollback is not assumed

Externally supplied legacy Svix application/binding IDs migrate to
`LegacyUnverified`. Portal and publication actions remain unavailable until the
server verifies ownership and provider identity. Existing message links migrate
to provider publications with a deterministic state (`ProviderQueued`,
`PublicationUnknown`, or terminal evidence-backed state). No guessed success.

Migrations are short and focused. Large data backfills are resumable and separated
from blocking DDL when necessary. No network calls occur inside migrations or
database transactions.

The repository still uses `Explore.*` source project names while tests and product
language increasingly use `Event.*`. This redesign does not perform that rename.
Any project/repository rename is a separate approved change to avoid mixing broad
path churn with reliability and migration work.

## Delivery Sequence

### Phase 0A: Immediate Portal Authority Containment

- Remove caller-supplied App Portal authority fields.
- Derive minimum capabilities from verified binding and authorization.
- Remove portal HAL action for invalid/unverified binding.
- Add no-store and secret-redaction/audit tests.
- Regenerate OpenAPI and the Blazor client from source.

### Phase 0B: Architecture and Migration Baseline

- Rebaseline source, current model snapshot, and latest released migration path.
- Record final schema/state/unique-key decisions from this plan.
- Define legacy classification and binding verification workflow.
- Add migration smoke/timing/backup-forward-fix evidence.

### Phase 1: Transactional Inbox and Durable Processing

- Add explicit tenant worker execution boundary.
- Add effect receipts and transaction-owned processor contract.
- Add complete inbox states, redrive provenance, and append-only attempts.
- Prove duplicate, conflict, crash, lease-expiry, and tenant-isolation behavior.

### Phase 2: Provider Publication and Bounded Reconciliation

- Introduce `WebhookProviderPublication` and attempts.
- Migrate/retire message publication links.
- Implement immutable Svix identity and bounded retry/reconciliation.
- Run every supported self-hosted conformance suite; managed SaaS is outside scope.

### Phase 3: Binding, Capabilities, and Configuration Authority

- Make `WebhookConsumer` the verified provider application boundary.
- Add typed provider capabilities and startup/write validation.
- Snapshot delivery plan, configuration, endpoint, contract, and retention versions.
- Add explicit audited pending-work migration.

### Phase 4: Local Enterprise Operations

- Add fair claiming, concurrency caps, jitter, bounded Retry-After, timeout/body limits.
- Add auto-pause/circuit and HAL-gated manual resume.
- Complete Standard Webhooks fixtures.
- Complete mandatory audit and retention cleanup.

### Phase 5: Operations API and UI

- Expose HAL-governed list/detail/retry/redrive/reconcile/abandon/pause/resume.
- Add separately authorized payload view.
- Add bounded bulk replay.
- Update Blazor without local role/provider authority checks.

### Phase 6: Observability, SLOs, and Runbooks

- Add bounded metrics, redacted traces/logs, and independent readiness.
- Define SLOs and alerts for lag, retry, dead-letter, unknown publication, and pause.
- Write Local-only and Svix-enabled incident/recovery runbooks.

### Phase 7: Typed Webhook Ownership

- Normalize Instance, Tenant, Organization, Group, and User consumer ownership.
- Enforce owner-kind/reference consistency, tenant containment, and instance identity in the domain and PostgreSQL schema.
- Propagate configuration scope through endpoints, subscriptions, provider bindings, audit, and explicitly targeted delivery plans.
- Resolve authoritative owner attributes before resource authorization; never trust caller-supplied ownership.
- Add Local/Cerbos parity and HAL-gated management for every owner scope.
- Expose scope-specific Blazor management surfaces through generated API contracts.
- Prove tenant users cannot discover or mutate instance or unrelated owner resources.

### Conditional Later Work

- ERP connector project.
- CloudEvents envelope.
- Ed25519 signatures.
- Additional provider-native features.
- New infrastructure only after measurements prove the current design insufficient.

## Failure-Injection Minimums

- Crash after claim and before transaction.
- Crash after business mutation staging but before commit.
- Simulated post-business-commit/pre-inbox-settlement boundary, proving the single
  transaction or receipt recovery prevents a duplicate effect.
- Same provider ID/same hash duplicate success.
- Same provider ID/different hash `PayloadConflict`.
- Existing receipt retry settles without effect replay.
- Lease expiry during slow execution cannot commit a second effect.
- Tenant A/B concurrent processing and identifier substitution fail closed.
- Svix timeout after acceptance enters unknown state and follows bounded recovery.
- Idempotency-window expiry and credential rotation require manual reconciliation
  unless a conformance-proven lookup resolves the publication.
- Endpoint config change does not mutate queued snapshot behavior.
- Cleanup skips nonterminal, replayable, unknown, and held records.

## Verification and Release Gates

Implementation cannot ship until all of these are green:

1. Canonical Release build.
2. All project test suites required by `docs/OPERATIONS.md`.
3. All mandatory webhook unit, persistence, integration, architecture, and E2E tests.
4. Local-only Docker/Aspire profile with Svix absent.
5. Svix-enabled profile.
6. Latest-released-schema migration smoke with representative data and lock timing.
7. Multi-worker PostgreSQL concurrency and failure-injection suite.
8. Every supported self-hosted Svix conformance profile has executed evidence.
9. OpenAPI regeneration and generated-client drift check.
10. No unresolved Critical or High finding without an explicit approved release waiver.

Unrelated failures are not accepted as a green gate. They must be fixed or covered
by an explicit release waiver naming owner, risk, expiry, and remediation.

## Evidence Boundaries

- .NET hosted services are singletons and do not receive a scope automatically;
  workers must create a scope per operation. The tenant boundary in this plan is a
  project security requirement in addition to DI scoping.
- Svix App Portal tokens are application-scoped and capability-bearing, so the
  backend alone derives capabilities and targets a verified consumer binding.
- Svix idempotency response retention is documented for same token/key POSTs for
  up to 12 hours; behavior outside that boundary requires conformance proof.
- A Svix application is an independent security context with no finer isolation.
- Svix payload retention defaults to 90 days and supports per-message overrides.
- Standard Webhooks signs exact raw bytes and supports multiple signatures; JSON
  reserialization is not a valid verification path.

Primary evidence:

- <https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service>
- <https://docs.svix.com/app-portal>
- <https://docs.svix.com/idempotency>
- <https://docs.svix.com/quickstart>
- <https://docs.svix.com/overview>
- <https://docs.svix.com/retention>
- <https://docs.svix.com/throttling>
- <https://github.com/standard-webhooks/standard-webhooks/blob/main/spec/standard-webhooks.md>
