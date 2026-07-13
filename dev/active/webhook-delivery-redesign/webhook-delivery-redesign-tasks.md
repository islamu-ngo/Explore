<!-- ABOUTME: Ordered implementation checklist for the approved webhook delivery redesign. -->
<!-- ABOUTME: Makes security containment, transactional correctness, provider proof, operations, migration, and release gates executable. -->

# Webhook Delivery Redesign Tasks

Last Updated: 2026-07-13 Europe/Brussels

## Status

- Planning: **Approved**
- Implementation: **Not started**
- Current task: **0.1 Rebaseline the implementation and migration source**
- Rule: complete tasks in order unless this document explicitly marks them parallel

## Definition of Ready

- [x] Local-first, optional-Svix direction accepted.
- [x] Dedicated `WebhookProviderPublication` model selected.
- [x] Transactional effect-receipt model selected.
- [x] `WebhookConsumer` selected as the Svix application security boundary.
- [x] Provider assumptions bounded by official documentation/conformance proof.
- [x] Audit, retention, fairness, signatures, migration, and release gates made mandatory.
- [ ] Source/model/migration rebaseline completed after plan approval.
- [ ] User explicitly starts implementation work.

## Phase 0A: Immediate App Portal Authority Containment

### 0.1 Rebaseline source and migrations

- [ ] Inspect the current portal request, command, handler, validator, service,
  controller, HAL policy, OpenAPI schema, generated client, UI, and tests.
- [ ] Inspect `ExploreDbContextModelSnapshot`, the latest webhook migration, and the
  latest released-schema upgrade starting point.
- [ ] Record source drift in this context file before editing if current behavior
  differs from the approved plan.
- [ ] Run the canonical Release build and existing focused webhook tests for a
  green baseline. Stop and report if the baseline is not green.

### 0.2 Lock the server authority contract with failing tests

- [ ] Add application/API tests proving callers cannot submit `ReadOnly` or
  `FeatureFlags` portal authority.
- [ ] Prove no portal session or HAL action for absent, disabled, tenant-mismatched,
  or legacy-unverified binding.
- [ ] Prove capabilities are derived as the minimum allowed by authorization,
  verified binding, provider/version capability, and instance governance.
- [ ] Prove portal responses include `Cache-Control: no-store`.
- [ ] Prove URL/token values do not enter logs, traces, caches, or audit metadata.
- [ ] Prove issuance fact is durably audited before the URL is returned.

### 0.3 Remove caller-controlled portal authority

- [ ] Delete public `ReadOnly` and `FeatureFlags` request/command/client fields.
- [ ] Derive portal capabilities server-side from the verified consumer binding.
- [ ] Fail closed when binding verification or provider capability is unavailable.
- [ ] Add safe issuance-fact audit; never persist URL/token.
- [ ] Add no-store response handling.
- [ ] Do not add compatibility parameters or a fallback endpoint.

### 0.4 Regenerate and verify contracts

- [ ] Regenerate OpenAPI output from source.
- [ ] Regenerate `EventApiClient.g.cs` through the repository workflow.
- [ ] Update Blazor calls and tests; keep action rendering HAL-only.
- [ ] Run application, API integration, infrastructure portal, client unit,
  architecture, and generated-client drift checks.

**Phase 0A exit:** caller-controlled portal authority is unreachable in the public
contract, verified binding is mandatory, sensitive responses are no-store/redacted,
and issuance audit is proven.

## Phase 0B: Architecture and Migration Baseline

### 0.5 Freeze schema decisions

- [ ] Document concrete EF mappings for `IncomingWebhookEffectReceipt`,
  `WebhookProviderPublication`, publication attempts, inbound attempts, delivery
  plan snapshots, retention holds, and audit records.
- [ ] Confirm aggregate IDs use UUIDv7 `Guid`, lookups use `int`, and cursors use `long`.
- [ ] Define the effect receipt unique key:
  `(TenantId, IncomingWebhookMessageId, EffectKind)`.
- [ ] Define the publication unique key:
  `(TenantId, WebhookMessageId, ProviderKind, ProviderBindingId)`.
- [ ] Define all state transitions and terminal/nonterminal classifications.
- [ ] Confirm repositories return entities and mapping remains in handlers.

### 0.6 Define deterministic legacy classification

- [ ] Inventory every legacy incoming row, message/provider link, binding/application
  ID, endpoint config, and unresolved delivery state.
- [ ] Map externally supplied Svix identifiers to `LegacyUnverified`.
- [ ] Map message/provider links to evidence-backed publication states; never guess success.
- [ ] Define handling for orphaned, duplicate, conflicting, and unknown rows.
- [ ] Define the provider-binding verification/repair flow before portal/publication use.

### 0.7 Prepare migration proof

- [ ] Split focused schema DDL from any resumable data backfill where lock duration requires it.
- [ ] Specify indexes, foreign keys, unique constraints, and online/concurrent options
  supported by the target PostgreSQL deployment.
- [ ] Prepare latest-release migration smoke data including legacy and conflict cases.
- [ ] Define backup/restore rehearsal, maintenance window, and forward-fix procedure.
- [ ] Add representative data-volume timing and lock observation to the release evidence.

**Phase 0B exit:** there is one schema contract, every legacy class has a deterministic
destination, and migration verification is executable before reliability code lands.

## Phase 1: Transactional Inbox and Durable Processing

### 1.1 Add failing duplicate/effect crash tests first

- [ ] Same provider ID and same payload hash returns duplicate success.
- [ ] Same provider ID and changed payload hash persists `PayloadConflict`.
- [ ] Crash after claim and before processing leaves retryable work.
- [ ] Crash after mutation staging and before transaction commit leaves no mutation/receipt.
- [ ] Simulate post-business-commit/pre-inbox-settlement failure and prove the chosen
  transaction/receipt design cannot replay the committed effect.
- [ ] Existing receipt with matching hash settles retry without applying effect again.
- [ ] Existing receipt with mismatched hash fails closed.
- [ ] Slow processing beyond lease expiry cannot commit a second effect.
- [ ] Concurrent workers preserve one effect and append evidence for each execution.

### 1.2 Introduce stable processing context

- [ ] Add `IncomingWebhookProcessingContext` with incoming ID, tenant, provider,
  provider message ID, event type, payload hash, and generation.
- [ ] Populate it only from the claimed persisted inbox row.
- [ ] Update processor contracts to require it.
- [ ] Reject attempts to substitute tenant/message/provider identity.

### 1.3 Add transactional effect receipts

- [ ] Add `IncomingWebhookEffectReceipt` entity and EF configuration.
- [ ] Add the tenant-scoped unique constraint and payload-hash invariant.
- [ ] Make processors declare stable `EffectKind` values.
- [ ] Commit local mutation, receipt, and inbox `Processed` transition in one UoW.
- [ ] For external effects, commit durable outbox/operation, receipt, and settlement
  in one UoW; dispatch network work later.
- [ ] Implement receipt-backed settlement without effect replay.
- [ ] Translate unique-race outcomes to deterministic success/conflict behavior.

### 1.4 Complete inbound states and evidence

- [ ] Add `Ignored`, `RejectedPermanent`, and `PayloadConflict` outcomes.
- [ ] Preserve `DeadLettered -> RetryDue` redrive with new processing generation.
- [ ] Record actor, reason, time, source generation, and result for redrive.
- [ ] Add append-only processing attempts with bounded failure details.
- [ ] Ensure automatic retry never processes conflict/permanent-rejection states.

### 1.5 Add explicit worker tenant execution

- [ ] Keep the cross-tenant coordinator limited to bounded candidate claim.
- [ ] Return persisted tenant ID with each claim.
- [ ] Create a fresh async DI scope, DbContext, `TenantExecutionContext`, and narrowly
  scoped system principal per work item.
- [ ] Constrain every resource lookup to persisted tenant and fail closed.
- [ ] Document every necessary `IgnoreQueryFilters` use and add tenant predicates.
- [ ] Dispose/clear tenant and principal context after every item, including failure.
- [ ] Add Local/Cerbos parity tests for machine actions.

### 1.6 Prove tenant isolation under concurrency

- [ ] Process tenant A and B concurrently and prove no context bleed.
- [ ] Attempt cross-tenant resource identifier substitution and prove no lookup/mutation.
- [ ] Prove stale ambient HTTP/browser tenant data is ignored.
- [ ] Prove context cleanup after success, cancellation, exception, and lease loss.
- [ ] Run multi-worker PostgreSQL tests, not only in-memory/unit tests.

### 1.7 Add and verify focused migration

- [ ] Add effect receipt, inbound state/evidence, generation, and required indexes.
- [ ] Migrate legacy inbox rows according to Phase 0B classifications.
- [ ] Run latest-release migration smoke, representative timing, backup/restore,
  Release build, and focused application/persistence/API tests.

**Phase 1 exit:** a committed inbound effect cannot be committed twice, conflict is
explicit, every worker runs under the persisted tenant, and failure injection passes.

## Phase 2: Provider Publication and Bounded Reconciliation

### 2.1 Add failing aggregate/state tests

- [ ] One publication per message/provider/binding unique key.
- [ ] Immutable binding/application, event ID, idempotency key, request hash,
  credential version, mode/config/contract versions, and validity window.
- [ ] Legal transitions among `Prepared`, `Publishing`, `ProviderQueued`,
  `RetryDue`, `PublicationUnknown`, `DeadLettered`, `ManualReconciliation`, `Abandoned`.
- [ ] Timeout after acceptance produces unknown state, not false failure/success.
- [ ] Fresh identity cannot be used to escape unknown state.

### 2.2 Add `WebhookProviderPublication`

- [ ] Add aggregate, EF configuration, repository contract/implementation, DbSet,
  query filter, unique/index constraints, and DTO mapping in handlers.
- [ ] Add append-only publication/reconciliation attempts.
- [ ] Move mutable provider submission state off `WebhookMessage`.
- [ ] Store external provider message mapping on the publication.
- [ ] Stop new `WebhookProviderLink` publication writes.

### 2.3 Materialize publication atomically

- [ ] Create `WebhookMessage`, delivery-plan snapshot, and all required publications/
  Local target snapshots in one transaction.
- [ ] Derive stable provider event and idempotency identities deterministically.
- [ ] Snapshot provider binding/application/environment, configuration, mode,
  contract, retention, credential reference/version, and validity window.
- [ ] Make materialization idempotent under outbox redelivery.

### 2.4 Bound automatic Svix recovery

- [ ] Retry create only before `IdempotencyValidUntil` with unchanged token scope,
  environment, application, credential version, request hash, and idempotency key.
- [ ] Persist `LastAutomaticReconciliationAt` and bounded counters.
- [ ] After expiry/rotation, use only conformance-proven lookup.
- [ ] Otherwise transition to `ManualReconciliation` and expose safe HAL action.
- [ ] Never make a fresh create call with a new identity for unknown work.

### 2.5 Build provider conformance suite

- [ ] Run against managed Svix and every supported self-hosted version.
- [ ] Cover repeat create inside the window.
- [ ] Cover window expiry.
- [ ] Cover duplicate event ID with same and changed payload.
- [ ] Cover timeout after acceptance.
- [ ] Cover credential rotation.
- [ ] Cover list/get consistency.
- [ ] Record capability/version results and reject unsupported configurations.

### 2.6 Migrate and retire legacy links

- [ ] Create provider publication rows from legacy links using deterministic evidence.
- [ ] Preserve unknown/unresolved evidence and require reconciliation.
- [ ] Verify legacy application IDs before enabling publish/portal actions.
- [ ] Remove retired message-publication link model only after migration verification.
- [ ] Run migration smoke, timing/lock checks, backup/restore, and focused tests.

**Phase 2 exit:** provider submissions have one authoritative aggregate, unknown state
cannot be hidden, and automatic recovery never exceeds proven provider guarantees.

## Phase 3: Binding, Capabilities, and Configuration Authority

### 3.1 Make consumer the provider security boundary

- [ ] Derive Svix application UID from immutable instance identity plus consumer ID.
- [ ] Verify each binding belongs to the persisted tenant and consumer.
- [ ] Keep separate consumers/apps when portal administration or message visibility differs.
- [ ] Add repair/rebind workflow with authorization and audit.
- [ ] Prevent tenant ID alone from serving as the application UID.

### 3.2 Add typed provider capabilities

- [ ] Model endpoint management, attempts, replay, payload inspection, portal,
  catalog, retention, app/endpoint throttling, transformations, ordering, callbacks.
- [ ] Resolve capabilities by provider and supported version.
- [ ] Validate configuration writes and startup against capabilities.
- [ ] Drive readiness, HAL, and UI explanations from capabilities.
- [ ] Do not claim Local parity for provider-native features.

### 3.3 Enforce immutable configuration snapshots

- [ ] Persist delivery plan, provider mode, bindings, endpoint config version,
  event-contract version, and retention snapshot when materializing.
- [ ] Make later configuration changes affect new messages only.
- [ ] Add explicit authorized/audited migration for eligible pending work.
- [ ] Warn on unknown/reconciliation publications during config change.
- [ ] Require an explicit pending-work decision; never silently reroute.

### 3.4 Verify binding/capability/snapshot migration

- [ ] Backfill deterministic snapshots for legacy pending work.
- [ ] Mark unverifiable provider identity as `LegacyUnverified`.
- [ ] Run Local/Svix mode-change and endpoint-version concurrency tests.
- [ ] Run latest-release migration smoke and representative lock timing.

**Phase 3 exit:** each consumer has a verified provider boundary, capabilities are
typed/versioned authority, and queued work cannot silently change routing semantics.

## Phase 4: Local Enterprise Operations

### 4.1 Add fairness and concurrency tests first

- [ ] Enforce global in-flight maximum.
- [ ] Enforce per-tenant and per-endpoint maximums.
- [ ] Enforce max items per tenant per claim cycle.
- [ ] Prove a noisy tenant cannot starve another tenant.
- [ ] Prove lease/fence behavior under multiple workers.
- [ ] Prove instance locks prevent unauthorized tenant overrides.

### 4.2 Implement bounded delivery behavior

- [ ] Add configurable instance defaults and governed tenant overrides.
- [ ] Use exponential backoff with full/equal jitter.
- [ ] Honor only bounded valid `Retry-After`.
- [ ] Bound endpoint timeout and response-body read.
- [ ] Reject redirects and apply existing outbound URL/SSRF validation.
- [ ] Add configured sustained-failure circuit/auto-pause.
- [ ] Add authorized HAL-gated manual resume.

### 4.3 Complete Standard Webhooks conformance fixtures

- [ ] Sign `message-id.timestamp.raw-payload` with HMAC-SHA256.
- [ ] Verify exact bytes without JSON reserialization.
- [ ] Support current/previous/multiple signatures during rotation.
- [ ] Use constant-time HMAC comparison.
- [ ] Cover UTF-8/non-ASCII, whitespace/newline mutation, stale/future timestamps,
  altered fields/payload, malformed base64, duplicate headers, tolerance boundaries.
- [ ] Share fixtures across signer/verifier tests where applicable.

### 4.4 Implement mandatory append-only audit

- [ ] Add audit entity/storage with principal, effective scope, action, target,
  safe before/after, config/policy version, correlation, reason, outcome, DB time.
- [ ] Audit consumer/endpoint lifecycle, secret rotation, mode/config/retention,
  binding, portal issuance fact, retry/redrive/reconcile/abandon/bulk replay,
  auto-pause/resume, and pending-work migration.
- [ ] Commit audit with business write where possible.
- [ ] Prevent security-sensitive success response when required audit persistence fails.
- [ ] Prove payloads, secrets, signatures, portal values, and raw provider errors are absent.

### 4.5 Implement retention and cleanup

- [ ] Add separate policies for inbound/outbound payload, attempts, dead-letter
  evidence, publications/mappings, logs, and admin audit.
- [ ] Snapshot resolved policy on materialized work.
- [ ] Add retention hold.
- [ ] Make cleanup tenant-scoped, bounded, idempotent, and observable.
- [ ] Exclude nonterminal, unknown/reconciliation, replay-window, and held data.
- [ ] Preserve minimum identities, hashes, outcomes, and audit after payload deletion.
- [ ] Map supported per-message Svix retention while keeping local minimum evidence.

### 4.6 Verify Local-only production profile

- [ ] Run Docker/Aspire with Svix absent and Local configured.
- [ ] Prove startup, readiness, materialization, delivery, retry, auto-pause/resume,
  retention cleanup, and audit.
- [ ] Prove no Redis/Kafka/CDC/new proxy dependency is required.

**Phase 4 exit:** Local is fair, bounded, signature-conformant, audited, retention-
governed, and independently production-operable.

## Phase 5: Operations API and Blazor UI

### 5.1 Add HAL-governed operational resources

- [ ] Add authorized handlers/endpoints for retry/redrive, reconcile, abandon,
  pause/resume, and eligible pending-work migration.
- [ ] Add state/capability/authorization-aware HAL policies.
- [ ] Keep controller GETs anonymous and handler-authorized per project rules.
- [ ] Keep writes controller-authorized and handler-authorized.
- [ ] Add Local/Cerbos parity tests for every action.

### 5.2 Add separately authorized payload access

- [ ] Add `view-payload` action/resource authorization.
- [ ] Emit payload HAL relation only when authorized, retained, and state-appropriate.
- [ ] Return no-store response and write access audit.
- [ ] Return retention-aware `404`/`410` without leaking cross-tenant existence.
- [ ] Keep payload out of all default list/detail DTOs.

### 5.3 Add bounded bulk replay

- [ ] Add preview with explicit filters and estimated eligible/excluded counts.
- [ ] Add maximum batch and per-tenant limits.
- [ ] Require reason, stable idempotency identity, and audit.
- [ ] Allow cancellation before execution starts.
- [ ] Exclude conflict, unknown, manual-reconciliation, held, and ineligible work.
- [ ] Preserve fairness and endpoint protections.

### 5.4 Update Blazor through generated contracts

- [ ] Regenerate OpenAPI/client after API changes.
- [ ] Render all actions from HAL relation presence only.
- [ ] Explain unavailable actions from safe capability/state metadata, not role guesses.
- [ ] Add responsive/accessibility and component tests for Local and Svix modes.
- [ ] Run browser E2E for portal, payload access, replay, pause/resume, and denial paths.

**Phase 5 exit:** operators can safely inspect and act without bypassing tenant,
authorization, capability, retention, or HAL boundaries.

## Phase 6: Observability, SLOs, and Runbooks

### 6.1 Add bounded telemetry

- [ ] Add metrics for claim lag, processing/delivery outcome, retries, dead letters,
  unknown/manual reconciliation, auto-pause, cleanup, and provider health.
- [ ] Use only bounded labels such as provider, operation, and outcome.
- [ ] Exclude tenant/message/endpoint/event IDs and URLs from metric labels.
- [ ] Redact payloads, secrets, signatures, portal values, and raw provider errors
  from logs/traces.

### 6.2 Add independent readiness and SLOs

- [ ] Report Local readiness independently of optional Svix readiness.
- [ ] Validate provider/version capability at startup/readiness.
- [ ] Define SLOs and alerts for lag, retry rate, dead-letter rate, unknown age,
  paused endpoints, and cleanup backlog.
- [ ] Verify cardinality and redaction under load.

### 6.3 Write operational runbooks

- [ ] Local-only startup and incident recovery.
- [ ] Svix-enabled startup and provider outage.
- [ ] Unknown publication and manual reconciliation.
- [ ] Credential rotation inside/outside idempotency window.
- [ ] Tenant endpoint auto-pause/resume.
- [ ] Retention hold and cleanup failure.
- [ ] Migration forward-fix and backup restore.

**Phase 6 exit:** operators can detect, diagnose, and recover each supported mode
without exposing sensitive data or relying on undocumented provider behavior.

## Conditional Later Tasks

These require a new approved plan and are not implementation shortcuts:

- [ ] Typed Organization/Group/User webhook ownership.
- [ ] ERP connector architecture and admission controls.
- [ ] CloudEvents envelope.
- [ ] Ed25519 signature mode.
- [ ] Additional provider-native features.
- [ ] Kafka/Redis/CDC/proxy changes justified by measured bottlenecks.
- [ ] `Explore.*` to `Event.*` project/repository rename.

## Final Verification Gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] Every project test suite required by `docs/OPERATIONS.md`
- [ ] All mandatory webhook unit, persistence, integration, architecture, and E2E tests
- [ ] Local-only Docker/Aspire profile
- [ ] Svix-enabled profile
- [ ] Latest-released-schema migration smoke with representative timing/lock evidence
- [ ] Backup/restore and forward-fix rehearsal
- [ ] Multi-worker PostgreSQL concurrency/failure-injection suite
- [ ] Managed and supported self-hosted Svix conformance evidence
- [ ] OpenAPI/generated-client drift check
- [ ] Security and privacy redaction review
- [ ] No unresolved Critical/High finding without approved waiver naming owner,
  risk, expiry, and remediation

No gate may be marked green by attributing a failure as unrelated. Fix it or record
the explicit release waiver.

## Completion Record

For each completed phase, append:

- commit(s)
- migrations and classification evidence
- commands/tests run
- failure-injection/conformance profile and version
- unresolved risks or approved waivers
- documentation/runbook updates
