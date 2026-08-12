<!-- ABOUTME: Working context for the approved webhook redesign before production implementation begins. -->
<!-- ABOUTME: Preserves source baseline, decisions, proof obligations, migration policy, evidence, and the exact next step. -->

# Webhook Delivery Redesign Context

Last Updated: 2026-07-15 Europe/Brussels

## Current Status

- Planning status: **Approved**
- Implementation status: **Phase 7 typed webhook ownership in progress**
- CTO disposition: architecture accepted; previous implementation contract rejected
  until the stop-ship gaps were incorporated
- Contribution intent/schema/benchmark gate: **complete with static proof**
- No product source, generated client, model snapshot, migration, or test change is
  attributed to the governance/rebaseline gates

## NEXT

1. Add failing owner-kind, tenant-containment, instance-scope, and authorization parity tests.
2. Replace ambiguous consumer ownership combinations with one normalized typed invariant.
3. Generate the ownership migration exclusively through `dotnet ef`.
4. Propagate authoritative ownership through CQRS, HAL, Cerbos, and Blazor management flows.
5. Re-run the complete webhook release gate and record Phase 7 evidence.

Do not start with UI polish, Svix feature expansion, lower ownership scopes, or
new infrastructure.

## Frozen Dirty-Worktree Rebaseline — 2026-07-13

The machine-readable source of truth is
`.omo/evidence/webhook-delivery-redesign/0.2/baseline-manifest.json`.
It records every pre-existing path, working-content SHA-256, tracked diff SHA-256,
status, classification, and owner boundary.

- Branch/HEAD: `develop` / `505602b5a69a947972ead5bf70bcd825510ecc36`.
- Original collapsed status: 131 entries.
- Original expanded status: 134 paths = 107 modified + 27 untracked.
- Classification: 85 webhook-shaped pre-existing paths, 41 protected unrelated
  paths, and 8 explicitly owned remaining paths.
- The three clean-at-start governance files changed by todo 0.1 are excluded from
  the 134-path baseline and remain separately attributable:
  `.claude/contract/intents.yaml`, `.claude/contract/schema.json`, and
  `.agents/benchmarks/cold-start-tasks.yaml`.
- The current canonical Release baseline is red with 18 unique in-scope webhook
  compiler errors: 15 in `Explore.Persistence` and 3 in
  `Explore.Infrastructure`. The older WebAssembly task-host receipt is superseded.
- The latest committed development migration is
  `20260712144721_AddManagedTenantProvisioningOperationOutboxPointer`. The
  repository has no release tag, so this document does not call it a released
  baseline.
- The committed OpenAPI copies are valid and identical but semantically stale;
  generated contracts are regenerated only after API source freezes.
- No webhook test, migration, or model-snapshot change existed in the frozen
  pre-execution baseline.

Serialized collision ownership is mandatory for both OpenAPI files,
`EventApiClient.g.cs`, `docs/API_CHANGELOG.md`, `docs/BLAZOR.md`,
`ExploreDbContext.SaveChanges.cs`, AppHost/launch settings, and both
`.design-sync` files. Each owner compares the manifest hash before and after its
edit. Any unexpected hash or path-count change stops that lane for attribution;
no reset, clean, stash, overwrite, or inferred ownership is allowed.

## Why the Previous Draft Was Insufficient

The previous plan correctly selected Local-first optional Svix architecture, but
left implementation-critical choices ambiguous:

- A processor could commit a business mutation, crash before inbox settlement,
  and replay the mutation.
- A fresh DI scope did not prove a fresh tenant security context.
- Callers could still request App Portal capabilities until a later phase.
- Provider state could live on `WebhookMessage`, `WebhookProviderLink`, or both.
- Svix recovery was not bounded by its 12-hour idempotency response window,
  credential scope, or supported-version conformance.
- Audit, retention, capability validation, signature fixtures, fairness, payload
  authorization, and migration proof were optional or deferred.

The approved plan resolves each of those points with one chosen model.

## Architecture Invariants

1. Local is the default and is independently production-capable.
2. Svix is optional and never becomes tenant, authorization, configuration, audit,
   or event-contract authority.
3. `WebhookMessage` is immutable semantic data plus delivery-plan snapshots.
4. `WebhookProviderPublication` exclusively owns provider submission state.
5. `WebhookProviderLink` is retired for message publication after data migration.
6. Local endpoint attempts remain `WebhookDeliveryAttempt` records.
7. Inbound correctness is proven by an atomic business effect/outbox, unique effect
   receipt, and inbox settlement transaction, not by settlement alone.
8. Claimed persisted `TenantId` is the sole tenant source for worker execution.
9. Each work item gets a fresh DI scope, DbContext, tenant context, and machine principal.
10. Existing work retains its provider, binding, endpoint, contract, configuration,
    credential-reference, and retention snapshots.
11. HAL is the only UI action authority.
12. Provider assumptions outside documented guarantees require conformance evidence.
13. Payload and secret-bearing values are excluded from default responses, logs,
    traces, audit metadata, and metric dimensions.
14. No network I/O runs in a database transaction or migration.

## Chosen Data Contracts

### Inbound

`IncomingWebhookProcessingContext` carries:

- `IncomingWebhookMessageId`
- `TenantId`
- `Provider`
- `ProviderMessageId`
- `EventType`
- `PayloadHash`
- `ProcessingGeneration`

`IncomingWebhookEffectReceipt` is unique on
`(TenantId, IncomingWebhookMessageId, EffectKind)` and records hash, generation,
applied time, and safe result reference. A local effect or durable external-effect
outbox, receipt, and `Processed` transition commit together. A receipt-backed retry
settles without repeating the effect. Same external identity with another hash is
`PayloadConflict`.

Inbound terminal states include `Processed`, `Ignored`, `RejectedPermanent`,
`PayloadConflict`, and `DeadLettered`. Redrive creates a new generation and retains
actor/reason/time provenance; it does not rewrite the row as newly `Verified`.

### Outbound Provider Publication

`WebhookProviderPublication` is unique on
`(TenantId, WebhookMessageId, ProviderKind, ProviderBindingId)`. It owns immutable
binding/application identity, idempotency/event IDs, request hash, provider/config/
mode/contract snapshots, credential reference/version, 12-hour-bounded idempotency
validity, external ID, lease/fence, failures, counters, and next action.

States are `Prepared`, `Publishing`, `ProviderQueued`, `RetryDue`,
`PublicationUnknown`, `DeadLettered`, `ManualReconciliation`, and `Abandoned`.
Attempts are append-only. A fresh provider identity never resolves unknown state.

### Svix Security Boundary

One Svix application maps to one `WebhookConsumer`, not one tenant. The UID derives
from immutable instance identity plus `WebhookConsumerId`. Portal access is issued
only for a verified binding and server-derived minimum capabilities. Existing
externally supplied application IDs are `LegacyUnverified` until verified.

### Delivery Plan

Materialization snapshots required provider publications/Local targets, binding
IDs, endpoint config versions, mode, configuration version, event-contract version,
and retention policy. Changes affect new messages unless an explicit authorized,
audited migration changes eligible pending work.

## Tenant Execution Contract

- A privileged coordinator only claims bounded cross-tenant candidate IDs and
  persisted tenant IDs.
- Business processing runs in a fresh async scope and fresh DbContext.
- `TenantExecutionContext` is initialized from the claimed row and disposed after
  one item.
- The system principal grants only the named machine action.
- Resource lookups include persisted tenant constraints and fail closed on ID
  substitution.
- Every `IgnoreQueryFilters` use needs a reason and explicit tenant predicate.
- Local and Cerbos machine-action decisions must match.
- The concurrency proof runs tenant A and B simultaneously and checks cleanup.

## Provider Assumptions Requiring Proof

These are not accepted from intuition or a client SDK alone:

- Repeat create after the documented idempotency validity window.
- Recovery after credential rotation.
- Event-ID duplicate semantics for same and changed payload.
- Lookup/list consistency after timeout following provider acceptance.
- Behavior across every explicitly supported self-hosted Svix version.
- Capability availability on older/self-hosted versions.

The conformance suite records provider/version and covers repeat POST within the
window, expiry, event-ID collisions, acceptance timeout, credential rotation,
list/get consistency, and unsupported behavior. Unsupported proof results in
`ManualReconciliation` or a configuration validation error, not optimistic retry.

## Security and Authority Decisions

- Delete public App Portal `ReadOnly` and `FeatureFlags` inputs immediately.
- Do not add a compatibility shim.
- Derive capabilities server-side from verified binding, policy, and provider facts.
- Return no portal HAL action when binding is absent, disabled, or unverified.
- Return portal sessions with `Cache-Control: no-store`.
- Never log/cache/trace/audit portal URL/token; audit only issuance fact.
- Make payload access a separate `view-payload` authorization and HAL relation.
- Return payload only after tenant/retention checks, with no-store and audit.
- Continue project rules: anonymous controller GET plus handler authorization;
  authorized writes plus handler authorization; HAL-only Blazor action gating.

## Local Reliability Decisions

- Bound global, per-tenant, and per-endpoint concurrency.
- Bound each tenant's share of a claim cycle to preserve fairness.
- Use exponential retry with full/equal jitter and bounded `Retry-After`.
- Bound timeout and response-body reads.
- Auto-pause/open circuit after configured sustained failure.
- Require HAL-gated authorized manual resume.
- Use instance defaults plus governed tenant overrides; no universal magic numbers.
- Conform HMAC-SHA256 signatures to Standard Webhooks exact-byte semantics.

## Capability Decisions

Provider capabilities are typed, version-aware data used by configuration writes,
startup validation, readiness, HAL, and UI explanations. The matrix covers endpoint
management, attempts, replay, payload inspection, portal, event catalog, retention,
application/endpoint throttling, transformations, ordering, and callbacks. Local
does not advertise simulated Svix features.

## Audit and Retention Decisions

Append-only audit is part of write behavior for lifecycle, secrets, provider/mode/
config/retention, binding, portal issuance fact, retry/redrive/reconcile/abandon/
bulk replay, pause/resume, and pending-work migration. Safe audit records include
principal, effective scope, action, target, safe before/after, policy/config version,
correlation, reason, outcome, and DB time. They exclude payloads, secrets,
signatures, portal values, and raw provider errors.

Separate retention policies cover inbound payload, outbound payload, attempts,
dead-letter evidence, publications/mappings, operational logs, and admin audit.
Cleanup cannot remove nonterminal, unknown/reconciliation, replay-window, or held
data. Minimum hashes/identities/outcomes survive payload deletion.

## Migration and Legacy-Data Policy

- Breaking obsolete contracts is allowed; hidden compatibility layers are not.
- Every legacy row receives a deterministic classification.
- Legacy externally supplied Svix identifiers become `LegacyUnverified`.
- Legacy message-provider links migrate to evidence-backed publication states.
- Every schema slice defines indexes/constraints and a forward-fix strategy.
- Verify upgrade from the latest committed development migration,
  representative timing/locks, and backup/restore.
- Separate resumable backfills from blocking DDL when needed.
- Do not trust legacy provider ownership until the server verifies it.
- Do not delete unresolved or unknown publication evidence.

### Phase 0B migration rehearsal contract

- The committed-development starting point is
  `20260712144721_AddManagedTenantProvisioningOperationOutboxPointer`.
- Deployment uses an inspected EF Core idempotent SQL script. Operators apply it with
  `ON_ERROR_STOP`, record migration-history before/after, and do not run application
  writers concurrently with the schema slice.
- Declare a 15-minute write-maintenance window for the measured 10,000-row shape, with
  a further 15-minute rollback/restore buffer. Before production deployment, scale the
  window from a production-like rehearsal; if the projected migration exceeds five
  minutes or the lock sample shows waiting writers, stop and split the backfill from DDL.
- Take a PostgreSQL custom-format backup before migration and restore it into a clean
  database. Compare migration history, row count, a deterministic payload checksum, and
  semantic webhook schema. Column ordinal position is excluded because PostgreSQL dump/
  restore may reorder physical columns without changing names, types, defaults,
  constraints, indexes, or EF mappings.
- Before traffic resumes, a failed validation restores the verified backup. After traffic
  resumes, keep writers paused and use a new additive forward migration; do not execute a
  destructive `Down()` or delete unresolved webhook evidence.
- The 2026-07-14 PostgreSQL 18 rehearsal upgraded 10,000 committed-baseline rows in under
  one second, canonicalized all 10,000 payloads, observed no waiting webhook-table lock at
  the 250 ms sample, and restored the same data checksum and semantic schema hash. The
  timing is release evidence for this fixture size, not a production capacity promise.

## Rename Boundary

Production source paths and namespaces remain `Explore.*`, while much of the test
and product vocabulary is `Event.*`. The webhook redesign uses current paths and
does not rename projects or the repository. A rename is a separate approved work
item because combining it with migrations would obscure review and rollback.

## Source Baseline to Recheck

### Domain and Persistence

- `src/Explore.Domain/IncomingWebhookMessage.cs`
- `src/Explore.Domain/WebhookMessage.cs`
- `src/Explore.Domain/WebhookConsumerProviderBinding.cs`
- `src/Explore.Domain/WebhookProviderPublication.cs`
- `src/Explore.Domain/WebhookConsumer.cs`
- `src/Explore.Domain/WebhookEndpoint.cs`
- `src/Explore.Persistence/Configurations/Entities/IncomingWebhookMessageConfiguration.cs`
- `src/Explore.Persistence/Repositories/IncomingWebhookMessageRepository.cs`
- `src/Explore.Persistence/Repositories/WebhookMessageRepository.cs`
- `src/Explore.Persistence/ExploreDbContext.DbSets.cs`
- `src/Explore.Persistence/ExploreDbContext.QueryFilters.cs`
- `src/Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs`
- `src/Explore.Persistence/Migrations/20260702192022_AddWebhookSubsystem.cs`

### Application, API, and Infrastructure

- `src/Explore.Application/Webhooks/DefaultWebhookEventPublisher.cs`
- `src/Explore.Application/Contracts/Webhooks/IncomingWebhookContracts.cs`
- `src/Explore.API/Services/IncomingWebhookIntakeService.cs`
- `src/Explore.API/Controllers/IncomingWebhooksController.cs`
- `src/Explore.API/Controllers/WebhooksController.cs`
- `src/Explore.API/Hateoas/Policies/WebhookConsumerLinkPolicy.cs`
- `src/Explore.API/Hateoas/Policies/WebhookDeliveryLinkPolicy.cs`
- `src/Explore.Infrastructure/Webhooks/WebhookProviderPublicationDrainService.cs`
- `src/Explore.Infrastructure/Webhooks/WebhookProviderPublicationDispatcher.cs`
- `src/Explore.Infrastructure/Webhooks/SvixWebhookProviderBindingAuthorityService.cs`
- `src/Explore.Infrastructure/Webhooks/SvixAppPortalService.cs`
- `src/Explore.Infrastructure/Webhooks/WebhookSignatureService.cs`
- `src/Explore.Infrastructure/Configuration/WebhookOptions.cs`
- `src/Explore.Infrastructure/Configuration/WebhookOptionsValidator.cs`

### UI and Contracts

- `src/Explore.Blazor.Client/Components/Webhooks/WebhookManagementPanel.razor`
- `src/Explore.Blazor.Client/Services/Webhooks/WebhookManagementService.cs`
- `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- `src/Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs`

### Existing Tests

- `tests/Event.API.IntegrationTests/Features/IncomingWebhookFrameworkTests.cs`
- `tests/Event.API.IntegrationTests/Features/WebhooksControllerTests.cs`
- `tests/Event.Persistence.IntegrationTests/Repositories/WebhookPersistenceTests.cs`
- `tests/Event.Persistence.IntegrationTests/TenantIsolation/WebhookRepositoryBypassTests.cs`
- `tests/Explore.Infrastructure.Tests/Infrastructure/Webhooks/WebhookDeliveryDrainServiceTests.cs`
- `tests/Explore.Infrastructure.Tests/Infrastructure/Webhooks/SvixWebhookDeliveryProviderTests.cs`
- `tests/Explore.Infrastructure.Tests/Infrastructure/Webhooks/SvixAppPortalServiceTests.cs`
- `tests/Explore.Infrastructure.Tests/Infrastructure/Webhooks/WebhookSignatureServiceTests.cs`
- `tests/Explore.Blazor.Client.Tests/Components/Webhooks/WebhookManagementPanelTests.cs`

The rebaseline must inspect actual symbols and the current migration chain before
the tasks are translated into edits. These paths are leads, not permission to
assume the code has remained unchanged.

## Evidence Record

- .NET scoped worker guidance: hosted service is singleton; create an async scope
  for scoped work. <https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service>
- Svix App Portal: backend creates app-scoped capability-bearing access.
  <https://docs.svix.com/app-portal>
- Svix idempotency: same auth token/key POST response retained up to 12 hours.
  <https://docs.svix.com/idempotency>
- Svix quickstart: application UID may use internal ID; prefer idempotency over
  event ID for request recovery. <https://docs.svix.com/quickstart>
- Svix security: each application is an independent security context.
  <https://docs.svix.com/overview>
- Svix retention: default 90 days with per-message override.
  <https://docs.svix.com/retention>
- Svix throttling: application and endpoint controls are provider capabilities.
  <https://docs.svix.com/throttling>
- Standard Webhooks: exact raw-byte signing, multiple signatures, timestamp
  validation, and constant-time HMAC comparison.
  <https://github.com/standard-webhooks/standard-webhooks/blob/main/spec/standard-webhooks.md>

## Release Posture

No phase is complete because its happy path works. Release requires every mandatory
webhook test, Local-only and Svix-enabled profiles, latest-schema migration smoke,
multi-worker PostgreSQL failure tests, provider conformance evidence, generated-
client drift checks, and no unwaived Critical/High finding.
