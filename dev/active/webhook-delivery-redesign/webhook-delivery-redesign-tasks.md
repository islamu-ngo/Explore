<!-- ABOUTME: Ordered implementation checklist for the approved webhook delivery redesign. -->
<!-- ABOUTME: Makes security containment, transactional correctness, provider proof, operations, migration, and release gates executable. -->

# Webhook Delivery Redesign Tasks

Last Updated: 2026-07-14 Europe/Brussels

## Status

- Planning: **Approved**
- Implementation: **In progress**
- Current task: **3.2 Add typed provider capabilities**
- Current blocker: **None**
- Rule: complete tasks in order unless this document explicitly marks them parallel

### Progress snapshot

- Phase 0A portal authority containment is implemented: callers cannot choose portal
  authority, persisted verified bindings and capability ceilings govern issuance, HAL
  is fail-closed, successful responses are `no-store`, and audit failure suppresses the URL.
- Phase 0B model/schema decisions are implemented through generated additive EF Core
  migrations, normalized lookup tables, tenant-leading keys/indexes, composite tenant
  foreign keys, named tenant filters, and repository-local bypass reasons.
- Phase 1 processing is implemented through persisted claims, lease tokens/fences,
  generation checks, per-claim tenant/machine execution scopes, transactional effect
  receipts, append-only attempts, bounded retry/dead-letter handling, lease heartbeats,
  and an authenticated/audited redrive command and API.
- Verified focused suites currently cover 5 atomic-effect cases, 7 settlement/recovery
  cases, 3 tenant-execution cases, 1 bounded-drain case, 4 redrive-handler cases, 3
  redrive API cases, the repository aggregate-evidence save-boundary regression, live
  Local/Cerbos machine-action parity, the real PostgreSQL two-tenant crash/isolation
  recovery scenario, and lookup parity/model-drift checks.
- The canonical Release build passes across 26 projects with 0 errors, and the full
  PostgreSQL persistence regression now passes 329/329 after binding-identity normalization.
- PostgreSQL 18 clean, committed-baseline, 10,000-row legacy, and backup/restore
  rehearsals converge to one semantic schema; all representative legacy rows classify
  deterministically and the restored data checksum is identical.
- The latest full API regression run completed 1,762 cases with 1,743 passing, 3 skipped,
  and 16 failures. Seven were stale webhook portal fixtures: the HTTP host used an
  unregistered provider tuple and HAL fixtures inherited the zero-evidence managed
  default. Those fixtures now use the pinned, executed self-hosted profile; all 5 HTTP
  containment and all 7 HAL authority cases pass. The remaining 9 failures are outside
  the webhook surface (public endpoint inventory, Control Plane tenant authorization,
  non-testing authorization bootstrap, instance-setting policy contracts, and two
  event-registration runtime fixtures). A broad API rerun remains required after the
  unrelated baseline is repaired; no unrelated source/test was changed to hide it.
- Wave 4.1 now has the plan-mandated `WebhookProviderPublicationStateTests`: eight
  focused cases prove immutable plan/provider identity, ordered fenced evidence,
  legal terminal and reconciliation paths, stale-lease rejection, bounded attempts,
  and the twelve-hour idempotency ceiling. The red run exposed and the domain fix now
  closes automatic publication claims at or after `IdempotencyValidUntil`.
- Wave 4.2 adds an entity-returning publication repository with atomic PostgreSQL
  due/unknown claims, explicit tenant predicates, bounded batches and attempts,
  lease tokens/fences, append-only attempt tracking, and optimistic completion guards.
  Four real-database cases pass, including concurrent claimers and a stale completion
  race normalized from the evidence uniqueness constraint to an EF concurrency failure.
- Wave 4.3 now materializes each exact-byte `WebhookMessage`, immutable delivery-plan
  snapshot, Local target set, and provider publication set through one application
  contract and one EF transaction. Semantic replay returns the frozen existing plan;
  changed immutable data fails explicitly; unique-key races recover only a matching
  winner; and target failure rolls the entire graph back. The active publisher now
  resolves governance first, builds bytes once, and never performs synchronous network
  dispatch. Six application cases, five infrastructure routing cases, and four real
  PostgreSQL atomicity cases pass; `TimeProvider` supplies the materialization clock.
- Wave 4.4 adds a disabled-by-default asynchronous provider-publication processor,
  bounded claims, fenced dispatch, snapshot-only Svix requests, deterministic retry
  jitter, and stable provider event/idempotency identities. Provider acceptance updates
  only the publication; definitely-not-accepted failures may retry; timeout or malformed
  acceptance becomes `PublicationUnknown`; and lease loss cannot write stale success.
  Five focused dispatch identity cases pass, with no active provider-link dual write.
- Wave 4.5 adds conformance-gated, lookup-only unknown reconciliation. One exact
  event/hash evidence-tag match queues; proven absence schedules the unchanged identity;
  conflicts, multiple matches, unsupported profiles, expiry, and exhausted attempts
  require manual reconciliation. Lookup is bounded by pages and time window, the default
  capability policy is fail-closed, and expired unleased rows use a separate repository
  recovery path. Seven infrastructure cases pass. Publication-attempt outcomes are now
  enum-backed normalized lookup rows with a restrictive FK, runtime/literal seeds, a
  generated migration, and tenant-leading evidence indexing; lookup parity and the four
  real PostgreSQL publication claim/concurrency cases pass after the schema change.
- Wave 4.6 now has a live, pinned self-hosted Svix v1.96.1 Testcontainers matrix
  backed by PostgreSQL and shared Redis queue/cache state. Two selected test methods
  execute seven provider cases: version pinning, idempotent replay inside the twelve-hour
  window, simulated window expiry, duplicate event identity with same/changed payload,
  response loss after acceptance, real credential rotation, and list/get consistency.
  The exact matrix command passes and writes secret-free JSON/TRX evidence. The live
  profile proved that self-hosted v1.96.1 does not return message tags, so request-hash
  exact lookup is deliberately disabled and unknown publications route to manual
  reconciliation. Unsupported and zero-evidence profiles fail startup/readiness.
  Managed Svix SaaS is outside the supported deployment matrix. Its zero-evidence profile
  remains fail-closed and unselectable; the dormant manual harness does not count as
  release evidence. Compose, Aspire, E2E infrastructure, runtime defaults, and operator
  docs use the same pinned
  tuple; Redis is explicit for both Svix queue and cache. Six readiness and eight option-
  validation cases pass, the AppHost Release build is green, and Compose validates cleanly.
  The canonical Release build was rerun across all 26 projects with 0 errors, and EF Core
  reports no model changes since the latest generated migration; existing warnings remain.
  The non-runtime infrastructure lane passes 773/773 after aligning two portal service
  fixtures with their verified binding tuple. Application, Domain, Secrets, Persistence,
  Blazor integration, and Blazor client projects pass their canonical Release test runs.
  Architecture currently has four committed Management-surface convention failures
  outside this redesign (command/query namespace/public-handler rules and seven DTO names).
  The exact self-hosted conformance selector was rerun after this scope decision and passed.
  `.env` and `.env.example` contain the matching local self-hosted JWT/signing-secret pair;
  managed token placeholders remain blank and have no Infisical or cloud SaaS owner. A live
  Aspire restart now runs healthy `svix/svix-server:v1.96.1`; the bundled JWT generator
  returned a valid bearer-token shape without exposing it, and the `.env` JWT authenticated
  successfully against the live self-hosted application-list endpoint.
- Phase 2.6 now uses three ordered generated migrations: widen nullable legacy evidence
  columns, perform a set-based evidence backfill with fail-fast ambiguity validation, then
  drop the retired table. The backfill creates immutable plans, `LegacyUnverified` bindings,
  provider publications, and normalized attempts; trims and normalizes provider identities;
  copies endpoint identity; preserves the source link ID as publication provenance; and
  never guesses provider success. Synced rows with one provider message ID become
  `ProviderQueued`; unresolved rows require manual reconciliation and disabled rows become
  terminal. Conflicting ownership or identifiers aborts the transaction before evidence loss.
- New legacy-link writes are structurally retired: the synchronous provider contract,
  runtime router, Local/DryRun/Disabled adapters, old Svix publisher, provider-link
  repository, and all corresponding DI registrations are removed. Delivery now has one
  authoritative route through immutable plan materialization plus asynchronous Local/Svix
  drains. After the database rehearsal passed, the legacy entity, EF configuration, DbSet,
  query filter, migration-service backfill adapter, and obsolete repository/provider tests
  were removed from the runtime model.
- The real PostgreSQL 18 retirement rehearsal upgrades 10,002 legacy links in an isolated
  database, observes zero waiting locks at 250 ms, creates 10,002 plans/publications/attempts,
  removes the legacy table, and verifies a custom-format backup/restore with identical
  publication ID/status checksum and no restored legacy table. The complete case passes in
  2 minutes 31.566 seconds, below the five-minute split threshold.
- Post-retirement verification is green for the 26-project Release build and all 329
  persistence tests. Architecture remains at the same unrelated Management baseline:
  270 passed, 1 skipped, and 4 failures for command/handler namespace and visibility plus
  seven Management DTO names; no webhook model, migration, dependency, or naming rule fails.
- Phase 3.1 now has one authoritative consumer security boundary. Application UIDs are
  recomputed as `islamu-{instance:N}-consumer-{consumer:N}` from the completed bootstrap
  identity, the tenant-only mapper fallback is deleted, and same-tenant consumers receive
  distinct provider identities. A generated forward migration normalizes placeholder legacy
  instance IDs, invalidates stale ownership proof, records reversible audit evidence, and has
  a passing 5.696-second PostgreSQL Up/Down rehearsal. The current 10,002-row upgrade,
  backup, restore, and checksum rehearsal passes in 2 minutes 34.211 seconds, and the full
  real-PostgreSQL persistence lane passes 329/329.
- The authenticated repair endpoint verifies the exact self-hosted conformance profile, Svix
  application ID/UID, and tenant/consumer metadata before opening a database transaction.
  It then creates or rebinds the normalized entity under optimistic concurrency/fence guards
  and commits a hashed, credential-free audit record atomically. `webhook:manage-provider`
  authorization and a HAL `repair-provider-binding` relation keep this operation server-owned;
  managed Svix SaaS and unsupported profiles fail closed.
- Phase 3.2 implementation is in progress with a normalized
  `webhook_provider_capabilities` lookup, an enum-backed twelve-capability bitmask, bounded
  database constraints, and a provider/version resolver. Local deliberately exposes only
  endpoint management and event-catalog authority; self-hosted Svix v1.96.1 exposes only the
  four capabilities proven by the live matrix. Configuration validation, readiness metadata,
  CQRS write boundaries, and consumer read models now fail closed through the same resolver.
  EF CLI generated `20260714095353_NormalizeWebhookProviderCapabilities`; the removed empty
  predecessor and model snapshot were handled exclusively by `dotnet ef`, and EF currently
  reports no pending model changes. HAL/UI explanations, contract regeneration, and the full
  focused verification matrix remain before the four 3.2 checklist items can be closed.

## Definition of Ready

- [x] Local-first, optional-Svix direction accepted.
- [x] Dedicated `WebhookProviderPublication` model selected.
- [x] Transactional effect-receipt model selected.
- [x] `WebhookConsumer` selected as the Svix application security boundary.
- [x] Provider assumptions bounded by official documentation/conformance proof.
- [x] Audit, retention, fairness, signatures, migration, and release gates made mandatory.
- [x] Source/model/migration rebaseline completed after plan approval.
- [x] User explicitly starts implementation work.

## Phase 0A: Immediate App Portal Authority Containment

### 0.1 Rebaseline source and migrations

- [x] Inspect the current portal request, command, handler, validator, service,
  controller, HAL policy, OpenAPI schema, generated client, UI, and tests.
- [x] Inspect `ExploreDbContextModelSnapshot`, the latest webhook migration, and the
  latest released-schema upgrade starting point.
- [x] Record source drift in this context file before editing if current behavior
  differs from the approved plan.
- [x] Run the canonical Release build and existing focused webhook tests for a
  green baseline. Stop and report if the baseline is not green.

### 0.2 Lock the server authority contract with failing tests

- [x] Add application/API tests proving callers cannot submit `ReadOnly` or
  `FeatureFlags` portal authority.
- [x] Prove no portal session or HAL action for absent, disabled, tenant-mismatched,
  or legacy-unverified binding.
- [x] Prove capabilities are derived as the minimum allowed by authorization,
  verified binding, provider/version capability, and instance governance.
- [x] Prove portal responses include `Cache-Control: no-store`.
- [x] Prove URL/token values do not enter logs, traces, caches, or audit metadata.
- [x] Prove issuance fact is durably audited before the URL is returned.

### 0.3 Remove caller-controlled portal authority

- [x] Delete public `ReadOnly` and `FeatureFlags` request/command/client fields.
- [x] Derive portal capabilities server-side from the verified consumer binding.
- [x] Fail closed when binding verification or provider capability is unavailable.
- [x] Add safe issuance-fact audit; never persist URL/token.
- [x] Add no-store response handling.
- [x] Do not add compatibility parameters or a fallback endpoint.

### 0.4 Regenerate and verify contracts

- [x] Regenerate OpenAPI output from source.
- [x] Regenerate `EventApiClient.g.cs` through the repository workflow.
- [x] Update Blazor calls and tests; keep action rendering HAL-only.
- [x] Run application, API integration, infrastructure portal, client unit,
  architecture, and generated-client drift checks.

**Phase 0A exit:** caller-controlled portal authority is unreachable in the public
contract, verified binding is mandatory, sensitive responses are no-store/redacted,
and issuance audit is proven.

## Phase 0B: Architecture and Migration Baseline

### 0.5 Freeze schema decisions

- [x] Document concrete EF mappings for `IncomingWebhookEffectReceipt`,
  `WebhookProviderPublication`, publication attempts, inbound attempts, delivery
  plan snapshots, retention holds, and audit records.
- [x] Confirm aggregate IDs use UUIDv7 `Guid`, lookups use `int`, and cursors use `long`.
- [x] Define the effect receipt unique key:
  `(TenantId, IncomingWebhookMessageId, EffectKind)`.
- [x] Define the publication unique key:
  `(TenantId, WebhookMessageId, ProviderKind, ProviderBindingId)`.
- [x] Define all state transitions and terminal/nonterminal classifications.
- [x] Confirm repositories return entities and mapping remains in handlers.

### 0.6 Define deterministic legacy classification

- [x] Inventory every legacy incoming row, message/provider link, binding/application
  ID, endpoint config, and unresolved delivery state.
- [x] Map externally supplied Svix identifiers to `LegacyUnverified`.
- [x] Map message/provider links to evidence-backed publication states; never guess success.
- [x] Define handling for orphaned, duplicate, conflicting, and unknown rows.
- [x] Define the provider-binding verification/repair flow before portal/publication use.

### 0.7 Prepare migration proof

- [x] Split focused schema DDL from any resumable data backfill where lock duration requires it.
- [x] Specify indexes, foreign keys, unique constraints, and online/concurrent options
  supported by the target PostgreSQL deployment.
- [x] Prepare latest-release migration smoke data including legacy and conflict cases.
- [x] Define backup/restore rehearsal, maintenance window, and forward-fix procedure.
- [x] Add representative data-volume timing and lock observation to the release evidence.

**Phase 0B exit:** there is one schema contract, every legacy class has a deterministic
destination, and migration verification is executable before reliability code lands.

## Phase 1: Transactional Inbox and Durable Processing

### 1.1 Add failing duplicate/effect crash tests first

- [x] Same provider ID and same payload hash returns duplicate success.
- [x] Same provider ID and changed payload hash persists `PayloadConflict`.
- [x] Crash after claim and before processing leaves retryable work.
- [x] Crash after mutation staging and before transaction commit leaves no mutation/receipt.
- [x] Simulate post-business-commit/pre-inbox-settlement failure and prove the chosen
  transaction/receipt design cannot replay the committed effect.
- [x] Existing receipt with matching hash settles retry without applying effect again.
- [x] Existing receipt with mismatched hash fails closed.
- [x] Slow processing beyond lease expiry cannot commit a second effect.
- [x] Concurrent workers preserve one effect and append evidence for each execution.

### 1.2 Introduce stable processing context

- [x] Add `IncomingWebhookProcessingContext` with incoming ID, tenant, provider,
  provider message ID, event type, payload hash, and generation.
- [x] Populate it only from the claimed persisted inbox row.
- [x] Update processor contracts to require it.
- [x] Reject attempts to substitute tenant/message/provider identity.

### 1.3 Add transactional effect receipts

- [x] Add `IncomingWebhookEffectReceipt` entity and EF configuration.
- [x] Add the tenant-scoped unique constraint and payload-hash invariant.
- [x] Make processors declare stable `EffectKind` values.
- [x] Commit local mutation, receipt, and inbox `Processed` transition in one UoW.
- [x] For external effects, commit durable outbox/operation, receipt, and settlement
  in one UoW; dispatch network work later.
- [x] Implement receipt-backed settlement without effect replay.
- [x] Translate unique-race outcomes to deterministic success/conflict behavior.

### 1.4 Complete inbound states and evidence

- [x] Add `Ignored`, `RejectedPermanent`, and `PayloadConflict` outcomes.
- [x] Preserve `DeadLettered -> RetryDue` redrive with new processing generation.
- [x] Record actor, reason, time, source generation, and result for redrive.
- [x] Add append-only processing attempts with bounded failure details.
- [x] Ensure automatic retry never processes conflict/permanent-rejection states.

### 1.5 Add explicit worker tenant execution

- [x] Keep the cross-tenant coordinator limited to bounded candidate claim.
- [x] Return persisted tenant ID with each claim.
- [x] Create a fresh async DI scope, DbContext, `TenantExecutionContext`, and narrowly
  scoped system principal per work item.
- [x] Constrain every resource lookup to persisted tenant and fail closed.
- [x] Document every necessary `IgnoreQueryFilters` use and add tenant predicates.
- [x] Dispose/clear tenant and principal context after every item, including failure.
- [x] Add Local/Cerbos parity tests for machine actions.

### 1.6 Prove tenant isolation under concurrency

- [x] Process tenant A and B concurrently and prove no context bleed.
- [x] Attempt cross-tenant resource identifier substitution and prove no lookup/mutation.
- [x] Prove stale ambient HTTP/browser tenant data is ignored.
- [x] Prove context cleanup after success, cancellation, exception, and lease loss.
- [x] Run multi-worker PostgreSQL tests, not only in-memory/unit tests.

### 1.7 Add and verify focused migration

- [x] Add effect receipt, inbound state/evidence, generation, and required indexes.
- [x] Migrate legacy inbox rows according to Phase 0B classifications.
- [x] Prove repository saves attach newly appended processing evidence before EF change
  detection; the isolated tenant-bypass regression and atomic/recovery suites pass.
- [x] Complete the full persistence regression rerun after the save-boundary fix.
- [x] Complete the canonical Release build after all Phase 1 fixes.
- [x] Add and pass the plan-mandated `IncomingWebhookCrashIsolationTests` fresh-state
  recovery scenario with one effect, exact evidence, and no cross-tenant mutation.
- [x] Reconcile or baseline the 8 non-webhook failures from the full API regression run.
- [x] Run latest-release migration smoke, representative timing, backup/restore,
  Release build, and focused application/persistence/API tests.

**Phase 1 exit:** a committed inbound effect cannot be committed twice, conflict is
explicit, every worker runs under the persisted tenant, and failure injection passes.

## Phase 2: Provider Publication and Bounded Reconciliation

### 2.1 Add failing aggregate/state tests

- [x] One publication per message/provider/binding unique key.
- [x] Immutable binding/application, event ID, idempotency key, request hash,
  credential version, mode/config/contract versions, and validity window.
- [x] Legal transitions among `Prepared`, `Publishing`, `ProviderQueued`,
  `RetryDue`, `PublicationUnknown`, `DeadLettered`, `ManualReconciliation`, `Abandoned`.
- [x] Timeout after acceptance produces unknown state, not false failure/success.
- [x] Fresh identity cannot be used to escape unknown state.

### 2.2 Add `WebhookProviderPublication`

- [x] Add aggregate, EF configuration, repository contract/implementation, DbSet,
  query filter, and unique/index constraints.
- [ ] Add publication DTO mapping in the management/read handlers introduced in Phase 6.
- [x] Add append-only publication/reconciliation attempts.
- [x] Move mutable provider submission state off `WebhookMessage`.
- [x] Store external provider message mapping on the publication.
- [x] Stop new `WebhookProviderLink` publication writes.

### 2.3 Materialize publication atomically

- [x] Create `WebhookMessage`, delivery-plan snapshot, and all required publications/
  Local target snapshots in one transaction.
- [x] Derive stable provider event and idempotency identities deterministically.
- [x] Snapshot provider binding/application/environment, configuration, mode,
  contract, retention, credential reference/version, and validity window.
- [x] Make materialization idempotent under outbox redelivery.

### 2.4 Bound automatic Svix recovery

- [x] Retry create only before `IdempotencyValidUntil` with unchanged token scope,
  environment, application, credential version, request hash, and idempotency key.
- [x] Persist `LastAutomaticReconciliationAt` and bounded counters.
- [x] After expiry/rotation, use only conformance-proven lookup.
- [ ] Otherwise transition to `ManualReconciliation` and expose safe HAL action.
- [x] Never make a fresh create call with a new identity for unknown work.

### 2.5 Build provider conformance suite

- [x] Run against every supported self-hosted version (currently pinned v1.96.1).
- [x] Keep managed SaaS outside the supported matrix; its zero-evidence profile remains
  fail-closed and its token placeholders remain empty.
- [x] Cover repeat create inside the window.
- [x] Cover window expiry.
- [x] Cover duplicate event ID with same and changed payload.
- [x] Cover timeout after acceptance.
- [x] Cover credential rotation.
- [x] Cover list/get consistency.
- [x] Record capability/version results and reject unsupported configurations.

### 2.6 Migrate and retire legacy links

- [x] Create provider publication rows from legacy links using deterministic evidence.
- [x] Preserve unknown/unresolved evidence and require reconciliation.
- [x] Verify legacy application IDs before enabling publish/portal actions.
- [x] Remove retired message-publication link model only after migration verification.
- [x] Run migration smoke, timing/lock checks, backup/restore, and focused tests.

**Phase 2 exit:** provider submissions have one authoritative aggregate, unknown state
cannot be hidden, and automatic recovery never exceeds proven provider guarantees.

## Phase 3: Binding, Capabilities, and Configuration Authority

### 3.1 Make consumer the provider security boundary

- [x] Derive Svix application UID from immutable instance identity plus consumer ID.
- [x] Verify each binding belongs to the persisted tenant and consumer.
- [x] Keep separate consumers/apps when portal administration or message visibility differs.
- [x] Add repair/rebind workflow with authorization and audit.
- [x] Prevent tenant ID alone from serving as the application UID.

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
- [ ] Managed Svix SaaS conformance and a selectable managed profile.
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
- [x] Every supported self-hosted Svix conformance profile has executed evidence
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
