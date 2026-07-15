<!-- ABOUTME: Ordered implementation checklist for the approved webhook delivery redesign. -->
<!-- ABOUTME: Makes security containment, transactional correctness, provider proof, operations, migration, and release gates executable. -->

# Webhook Delivery Redesign Tasks

Last Updated: 2026-07-15 Europe/Brussels

## Status

- Planning: **Approved**
- Implementation: **Phase 7 complete — final verification in progress**
- Current task: **Full API rerun, remaining canonical projects, and migration-drift gate**
- Current blocker: **None**
- Rule: complete tasks in order unless this document explicitly marks them parallel
- Migration guard: generate or remove EF Core migrations only with `dotnet ef`; never hand-edit a generated migration, designer, or model snapshot

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
- Package remediation is complete. The API now pins the vendor-patched Microsoft.OpenApi
  2.7.5 line directly, the unused Blazor reference is removed, and redundant Roslyn 5.3
  references are removed so EF Design resolves its internally consistent 5.0 toolchain
  without NU1608. The default MIT AutoMapper 14 line retains the documented self-hoster
  licensing model but now applies `MaxDepth(64)` to every registered type map, directly
  bounding the CVE-2026-32933 recursion path; a focused composition-root regression proves
  every discovered map carries the ceiling. NuGet suppresses only that exact advisory after
  mitigation, while commercial builds remain on vendor-patched AutoMapper 16.1.1. A forced
  26-project restore completes with zero warnings, Microsoft.OpenApi no longer appears in
  the vulnerable package report, and the focused AutoMapper security regression passes.
- The API-first artifact workflow has been rerun from source: the Release API build generated
  `schemas/openapi_islamu-event.json`, the governed inventory test regenerated
  `docs/API_CONTRACT_INVENTORY.md`, and the NSwag MSBuild target regenerated and post-processed
  `EventApiClient.g.cs`; no generated artifact was hand-edited. The repository tool manifest
  now pins `dotnet-ef` 10.0.8 to the EF runtime patch, and that local CLI reports no pending
  `ExploreDbContext` model changes. No migration was created or modified.
- Final-gate checkpoint: the 26-project Release build is green with zero errors. Complete
  Application, Domain, Architecture, and Secrets test projects pass individually in Release;
  the previously reported four Management convention failures are therefore fully closed,
  not merely passing under focused selectors.
- Final-gate checkpoint: the complete PostgreSQL Persistence and API Integration projects
  also pass individually in Release. The API result closes the prior 1,791-case run's eight
  failures under the full shared-fixture/lifetime surface, including lookup reseeding,
  production guardrails, public GET contracts, instance-wide Control Plane routing, Cerbos
  provider lifetime, and public resolver governance.
- Final-gate checkpoint: Blazor Integration passes. Blazor Client completed 1,647 cases with
  1,645 passing, 1 intentionally skipped, and 1 failure: the selected-reference AI assistant
  test dispatches an `onclick` handler ID that bUnit reports was removed by an intervening
  render. The failure is isolated to test/render synchronization and is being fixed before
  the full client lane is rerun.
- Final-gate completion: the reference-result click now performs lookup and dispatch inside
  one bUnit renderer turn, eliminating the stale handler race without changing component
  behavior. The exact test, complete AI rail class, and complete 1,647-case Blazor Client
  project pass; the one accessibility case remains intentionally skipped under its existing
  documented MudBlazor v9 condition. All eight canonical projects therefore pass individually
  in Release. The deprecated, redundant `Microsoft.Extensions.ApiDescription.Client` reference
  was removed; the custom NSwag generation target, Blazor Client build, and full client suite
  remain green. Final forced restore is 26 projects with zero errors and zero warnings, and the
  final canonical 26-project Release build has zero errors. The deployable commercial API also
  builds successfully against patched AutoMapper 16.1.1/MediatR 14.1.0 without a license value
  in `.env`; the workspace was restored to the default FOSS graph afterward. EF CLI finally
  reconfirmed no pending model changes, and no generated migration artifact was edited.
- Final environment audit removed the dormant managed-Svix cloud conformance block from both
  `.env` files. The remaining provider tuple is self-hosted `svix/svix-server:v1.96.1` only,
  Redis is explicit for queue and cache, and both application credential values remain empty
  as requested. `docker compose config --quiet` passes with this self-hosted-only configuration.
- Phase 7 delivery/UI propagation now preserves the source tenant separately from instance-owned
  consumer configuration, derives provider application identity from immutable instance plus
  consumer identity, and rejects tenant-owned consumer/source mismatches before event or endpoint
  resolution. Focused Infrastructure and Application publisher tests pass. The generated OpenAPI,
  contract inventory, and NSwag client are refreshed from source. Blazor exposes the same management
  panel in Instance, Tenant, Organization, Group, and User settings, forwards the exact normalized
  owner tuple through every collection read, fixes consumer creation to its host owner, and renders
  tenant-only provider/replay tabs only when collection HAL relations authorize them. Focused API,
  Blazor service/component, and five-owner Application handler suites pass; browser ownership E2E
  remains in progress.
- Phase 7 database containment is now complete in the CLI-generated
  `20260715172404_AddTypedWebhookOwnership` migration. A required normalized
  `configuration_scope_id` is derived in the domain and stored by PostgreSQL as a generated
  `COALESCE(tenant_id, instance_id)` column, so clean installs and upgrades with existing tenant
  webhook rows receive the same deterministic scope without a mutable application backfill. It
  participates in composite
  consumer/endpoint/subscription/provider-binding foreign keys. Direct PostgreSQL writes prove
  cross-tenant Organization, Group, and User owners, cross-scope endpoints/subscriptions, and
  tenant-consumer substitution into instance endpoint/binding rows are rejected. Normal atomic
  endpoint/subscription persistence and provider-binding verification/concurrency regressions pass;
  the verified binding constraint now uses normalized Verified lookup ID 3 and checks both scope
  and consumer identity. The migration was removed and regenerated only through `dotnet ef`; no
  migration, designer, or snapshot artifact was hand-edited. Focused model, lookup, tenant-user,
  tenant-bypass, binding Up/Down, and legacy backup/restore migration rehearsals pass with the
  generated-column mapping; the complete PostgreSQL persistence suite passes 355/355.
- Phase 7 API finalization has corrected the normalized OpenAPI role-ID invariant, the typed-owner
  Svix portal metadata fixture, and controller tests that must model the production
  `internal_user_id` identity boundary. A TUnit security-fixture defect was also isolated:
  public nested Keycloak/Cerbos initializer properties were initialized recursively by TUnit and
  then initialized again by the composite, racing two Keycloak key sets. The composite now privately
  owns exactly one Keycloak and one Cerbos lifecycle. The focused real-JWT lane passes 14/14 and its
  TRX proves only Ryuk plus one container of each type were created. Context7 was invoked for the
  TUnit lifecycle contract but reported its monthly quota exhausted; the repository-pinned TUnit
  XML documentation independently confirms recursive nested-initializer discovery. The complete
  API suite is now being rerun before any final-gate checkbox is marked.
- PostgreSQL 18 clean, committed-baseline, 10,000-row legacy, and backup/restore
  rehearsals converge to one semantic schema; all representative legacy rows classify
  deterministically and the restored data checksum is identical.
- The latest full API regression run completed 1,791 cases with 1,780 passing, 3 skipped,
  and 8 failures. The final webhook-focused regressions were stale test authority/data:
  portal containment queried/substituted the retired generic audit repository instead of
  the append-only webhook audit writer, and the duplicate-capture logging fixture sampled
  independent UTC instants that violated retention ordering before reaching its redaction
  assertion. All 5 HTTP portal containment cases and the focused duplicate-capture logging
  case now pass without weakening audit failure containment or log-redaction assertions.
  The remaining eight failures are now resolved at their owning boundaries. Respawn resets
  explicitly reseed runtime lookup data, restoring the normalized notification category/status
  rows required by registration intent persistence. Public GET smoke probes accept only success
  or safe `403`/`404` outcomes declared by the endpoint contract, which preserves anonymous
  controller ingress plus handler-level authorization. The multi-tenant Control Plane is exempt
  from tenant resolution because it is an instance-wide surface, then reaches JWT and Cerbos
  instance-setting authorization; its real-Cerbos test override now has host lifetime instead of
  being disposed after one request. Non-testing guardrail hosts use a non-Development environment
  and early disabled scheduler settings, preventing user-secret/Infisical and TickerQ state from
  contaminating the production registration proof. The public resolver configuration is no longer
  incorrectly grouped with protected instance settings. Focused registration (3), production
  guardrail (4), authorization pipeline (12), resolver governance (1), regular-user Cerbos view
  matrix (19), public GET smoke (1), and Control Plane middleware regression (1) selectors pass.
  Full API regression remains part of the final gate.
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
  `.env` and `.env.example` retain the self-hosted Svix infrastructure settings while the
  application auth token and operational-webhook secret are intentionally blank. They have no
  cloud SaaS or Infisical owner in this development phase. Aspire can run the pinned
  `svix/svix-server:v1.96.1`; application-level Svix operations remain fail-closed until those
  local self-hosted credentials are deliberately configured.
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
  reports no pending model changes. The remaining implementation and verification results are
  recorded in the following Phase 3.2 entries.
- Phase 3.2 capability authority is now exercised end to end. CQRS regression tests prove
  unavailable modes fail before persistence, Local capability provenance remains distinct,
  verified Svix authority is the intersection of live provider proof and binding governance,
  and pure Svix cannot mutate Local endpoints. The pinned v1.96.1 Testcontainers matrix passes
  both selected methods and all eleven executed cases; resolver, option-validation, readiness,
  lookup/DTO parity, HAL authority, and Blazor component suites also pass. The canonical OpenAPI
  document and NSwag client were regenerated, and EF CLI again reports no pending model changes.
  The Blazor provider cell renders bounded capability counts and safe reason text, while portal
  actions remain HAL-only. Independent screenshot review remains open because the applicable
  visual-QA workflow requires reviewer subagents and the user explicitly prohibited subagents.
- The canonical Release build passes all 26 projects. Full Application, non-runtime
  Infrastructure, Blazor client, and 329-case PostgreSQL persistence suites pass; 20/20 stable
  OpenAPI invariants and the generated inventory pass; all eight focused HAL authority cases and
  the complete live self-hosted conformance selector pass. Readiness now publishes normalized
  lookup codes rather than C# enum names. The first full persistence run exposed two historical
  migration tests seeding the new lookup before its table existed. Their isolated setup now uses
  the current EF chain and runtime seeder first, then generated Down migrations to the historical
  boundary before replaying Up. Both 10,002-row backup/restore and binding-identity Up/Down cases
  pass again without a production seeder bypass or any migration-file edit.
- Phase 3.3 implementation now assigns authoritative positive configuration versions to webhook
  consumers and endpoints and resolves delivery plans from active tenant/consumer/event/endpoint,
  provider-capability, contract, retention, and secret-binding facts. Local target snapshots carry
  the endpoint version; provider plans carry consumer/capability and credential-reference versions;
  incomplete facts fail closed. Endpoint updates and signing-credential rotations now require the
  caller's expected configuration version, an explicit normalized preserve-or-migrate decision, a
  reason, and acknowledgement when provider publication outcomes are uncertain. Eligible migration
  is restricted to pending, unclaimed, never-attempted Local targets and is committed atomically with
  the configuration change and a credential-free audit record. Preserve leaves every existing
  snapshot untouched. Local signing snapshots now use a dedicated credential-activation timestamp
  instead of the endpoint's unrelated general update time. Materialization rejects payloads whose
  identity, schema version, occurrence time, or retention boundary differs from the database-governed
  event contract. The Blazor dialogs expose the same non-defaulted decision and remain HAL-gated.
  EF CLI removed and regenerated the pending migration as
  `20260714115533_EnforceWebhookConfigurationSnapshots`; its migration, designer, and model snapshot
  have not been hand-edited. API, generated-client, Blazor, API-test, Domain-test,
  Infrastructure-test, and Persistence-test projects build with zero errors. Focused payload,
  resolver, options-validation, handler, domain, API, HAL, Blazor, atomic PostgreSQL, lookup-parity,
  and four-case historical migration/backup/restore suites pass. The historical fixtures seed on the
  current schema before generated `Down` replay, so their setup no longer writes current columns into
  an intentionally older schema. Phase 3.3 is complete and Phase 3.4 verification is active.
- Phase 3.4 implementation now has an authorized consumer provider-mode transition boundary with
  expected configuration-version checks, normalized preserve/migrate governance, target capability,
  Local-endpoint and verified Svix-binding prerequisites, uncertainty acknowledgement, and an atomic
  credential-free audit. Existing pending work is always preserved on its immutable Local target or
  provider-publication snapshot; cross-provider pending-work migration is rejected explicitly. The
  API exposes this action through a permission-filtered `change-provider-mode` HAL affordance. Build,
  concurrency, immutable-snapshot, generated-contract, and live migration evidence are still in progress.
- EF CLI regenerated `20260714115533_EnforceWebhookConfigurationSnapshots` after both consumer and
  endpoint configuration versions became optimistic-concurrency tokens. The generated designer and
  snapshot contain `IsConcurrencyToken` metadata; no generated migration artifact was hand-edited.
  Focused application/API mode-change tests, Local-to-Svix immutable resolution tests, and real
  PostgreSQL stale-writer tests for both aggregates now pass.
- Phase 3.4 is complete. The latest-release 10,002-row generated migration replay deterministically
  backfills delivery snapshots and normalized publication attempts, preserves queued evidence,
  routes unresolved evidence to manual reconciliation, and leaves unverifiable provider identity
  disabled as `LegacyUnverified`. Lock sampling observed zero waiting webhook-table locks; custom
  backup/clean restore preserved the publication checksum and the retired provider-link table stayed
  absent. The migration-rehearsal evidence now names the final CLI-generated migration. Phase 4.1 is active.
- Phase 4.1 is complete. Three live PostgreSQL claim tests prove the global, per-tenant, and
  per-endpoint in-flight ceilings, per-cycle tenant caps, round-robin noisy-tenant fairness, and
  lease/fence rejection across competing workers. The hierarchical settings resolver now enforces
  every registered setting's declared scope during reads as well as writes, so stale rows cannot
  override instance-only webhook limits; all 33 focused hierarchy cases pass. Phase 4.2 is active.
- Phase 4.2 is complete. A scoped governance resolver freezes lock-aware tenant policy for each
  claim/delivery while startup options remain hard safety ceilings. Focused tests prove governed
  claim limits, full-jitter retry, bounded `Retry-After`, request timeout, byte-capped response
  disposal, redirect/SSRF rejection, configured auto-pause, and authorized HAL-only resume. A
  four-case PostgreSQL suite proves sustained failures open the circuit and tenant-scoped resume
  atomically resets its delivery state. Phase 4.3 is active.
- Phase 4.3 is complete. Twelve signature fixtures include an independent canonical HMAC vector,
  exact UTF-8/raw-byte and whitespace mutation checks, altered ID/timestamp/payload rejection,
  inclusive tolerance boundaries, malformed/unknown/multiple signatures, and current/previous
  rotation expiry. Duplicate HTTP signature fields now normalize to separate values and the full
  eleven-case incoming-webhook framework suite passes. Production verification continues to use
  `CryptographicOperations.FixedTimeEquals`. Phase 4.4 is active.
- Phase 4.4 implementation now has normalized action, outcome, principal, scope, and target
  lookups plus a tenant-filtered append-only administrative ledger generated through the EF CLI.
  The repair-binding flow now records successful verification atomically with the binding write
  and persists rejected ownership proof as safe hashed metadata; both paths exclude credentials,
  raw provider responses, and caller-supplied actor metadata. The Application project builds after
  the first audit-writer retrofit, while the remaining lifecycle handlers and persistence proofs
  are still in progress.
- Mandatory audit now spans consumer and endpoint creation, endpoint update/archive/test/secret
  rotation, provider-mode changes, repair success/rejection, portal issuance/rejection, incoming
  redrive, delivery retry, automatic pause, manual resume, and explicit pending-work migration.
  Create/update flows use the retry-aware EF unit of work, and the Local worker commits failed
  attempt settlement, circuit transition, retry creation, and system audit atomically. Focused
  projects compile with domain tests for unsafe metadata rejection and PostgreSQL tests for DB time,
  normalized foreign keys, EF append-only guards, and rollback when mandatory audit validation fails.
  Focused TUnit lanes now pass for the audit domain invariants, five administrative-handler suites,
  Local delivery retry/auto-pause behavior, the portal fail-closed/no-store HTTP contract, lookup
  parity, and incoming-redrive recovery; every selected lane enforces at least one discovered test.
- Phase 4.4's audit foundation is complete and EF CLI reports no pending model changes. Five focused
  writer cases prove normalized user, machine-owner, and explicit system principals plus anonymous
  and malformed-principal rejection. The cross-phase action matrix remains open only for operations
  that do not exist yet: retention changes/cleanup land in 4.5, and operator reconcile/abandon plus
  bulk replay land in 5.1/5.3. Those dependent audit emissions are explicitly non-blocking and must
  close with their owning operation rather than being fabricated ahead of the business workflow.
- Phase 4.5 implementation is in progress. Separate validated retention horizons now resolve to an
  immutable policy version and UTC cutoffs snapshotted on outgoing plans, incoming messages, provider
  publications, and administrative audit. A normalized retention-subject lookup and tenant-filtered
  hold aggregate protect eligible evidence. The bounded cleanup repository excludes active work,
  ambiguous provider outcomes, replay/idempotency windows, and active holds; cleanup plus its system
  audit commit in one tenant-scoped unit-of-work transaction. The production API graph builds with
  zero errors. Domain, policy-validation, manual-retry, inbound-redrive, and PostgreSQL cleanup lanes
  pass, including dry-run, active holds, tenant isolation, terminal-state gating, payload redaction,
  and minimum hash/identity/outcome preservation. EF CLI removed the first unsafe generated draft,
  the model gained conservative legacy-row defaults, and EF CLI regenerated
  `20260714133943_AddWebhookRetentionGovernance`; no migration artifact was hand-edited. Cleanup
  orchestration/audit, lookup parity, and supported Svix retention mapping tests pass. Context7
  confirms `payloadRetentionPeriod` is a per-message integer-day override against Svix's 90-day
  default; provider retention is capped at 90 days without shortening authoritative local evidence.
  Phase 4.5 is complete. Canonical Release build plus full Application and Domain projects pass;
  clean-architecture, agent-context, and webhook authorization-parity lanes pass. The full architecture
  suite remains red only for unrelated committed management request/handler namespace and management
  DTO naming violations, so the canonical check stopped before its remaining projects by policy.
- Phase 4.6 baseline evidence is red as expected: `aspire start --isolated` with
  `WEBHOOKS_PROVIDER=Local` still created both `svix` and `svix-postgres`, and the API resource waited
  for Svix. The detached Aspire 13.4.6 process then reproduced the documented lifecycle defect and
  disappeared from `aspire ps`; its secret-free child log is the evidence source. Context7's current
  Aspire guidance confirms configuration-driven conditional resource registration, so the AppHost
  graph, Compose/environment defaults, and operator documentation must now converge on Local without
  provisioning or referencing Svix.
- Phase 4.6 Local-only graph correction is implemented: Aspire conditionally registers and references
  self-hosted Svix only for explicit `Svix`/`Composite`, Compose defaults to Local without the optional
  `webhooks` profile, both environment files select Local with intentionally blank Svix credentials,
  and the live isolated graph contains no `svix` or `svix-postgres` resource. Live QA also exposed a
  production-only EF Core resiliency defect: all three webhook claim repositories began explicit
  transactions outside Npgsql's retrying execution strategy. Three failing-first PostgreSQL regressions
  reproduced the exact exception; the repositories now execute their complete advisory-lock transaction
  through `CreateExecutionStrategy().ExecuteAsync`, and the focused incoming, delivery, and publication
  claim suites pass. The rebuilt live API now returns 200 from `/alive`, reports both webhook checks
  Healthy for Local, and completes repeated incoming-worker polling without the prior exception.
- Phase 4.6 live lifecycle tracing exposed an unresolved Local handoff: canonical publication atomically
  creates `WebhookLocalTargetSnapshot` rows, while the production Local worker claims only
  `WebhookDeliveryAttempt` rows; no runtime caller currently promotes or claims a pending target.
  Consequently a normally materialized Local plan cannot reach HTTP delivery and its target cannot reach
  terminal retention state. A focused runtime-service regression now reproduces the defect: one due,
  claimed canonical target with no legacy scheduled attempt produces zero processed/succeeded deliveries
  and no HTTP call. The worker now uses fair, capacity-governed Local-target claims; sends retained bytes
  using the target's frozen URL, credential reference/version, and timeout; settles through the target
  lease/fence; and appends one terminal attempt-evidence row per execution. Retryable work returns to the
  target as `RetryDue`, exhausted work becomes `DeadLettered`, and non-retryable work becomes `Abandoned`.
  Manual retry atomically reopens a terminal target and appends audit without manufacturing a scheduled
  attempt. The legacy attempt-queue claim/mutable-settlement API has been removed, and endpoint-test
  scheduling now materializes the same atomic message/plan/Local-target graph. Focused Release suites are
  green for 17 drain cases, four readiness cases, five PostgreSQL target claim/recovery cases, two
  tenant-isolation bypass cases, nine materialization cases, and the webhook application handlers.
  Live Aspire QA now also proves the authenticated endpoint-test entry point atomically creates the
  consumer, endpoint, audit, message, delivery plan, and canonical Local target; the hosted worker
  advanced that target to `SUCCEEDED` with fence `1` and appended exactly one `SUCCEEDED` attempt
  evidence row with HTTP 200. A second authenticated test against a deterministic HTTP 500 sink is
  exercising the real retry scheduler and circuit produced five ordered append-only failure evidence
  rows, including bounded timeout classification, then crossed the governed threshold and atomically
  set the endpoint to `AUTO_PAUSED` with `ENDPOINT_AUTO_PAUSED` audit. The authenticated resume action
  returned success, restored `ACTIVE`, reset the failure counter, and appended `ENDPOINT_RESUMED` audit.
  Source/package scans show no webhook dependency on Redis, Kafka, CDC, YARP, or a new proxy; the live
  Local graph has no Svix, Kafka, CDC, or proxy resource. A time-shifted disposable success record is
  processed by the restarted hosted cleanup processor: its retained payload is cleared, its terminal
  attempt evidence is pruned, and `RETENTION_CLEANUP_COMPLETED` records both affected categories in a
  safe audit payload. Phase 4.6 is complete; Phase 5.1 is active.
- Phase 5.1 implementation now compiles across the Release API dependency graph with 0 errors.
  Provider publication list/detail resources map normalized status, provider, mode, and append-only
  attempt evidence without payloads or credential references. Manual reconciliation requires an exact
  provider message identifier and settles only `MANUAL_RECONCILIATION`; abandonment is restricted to
  `MANUAL_RECONCILIATION` or `DEAD_LETTERED`, so an operator cannot steal active leased work. Endpoint
  pause/resume uses the existing `DISABLED`/`AUTO_PAUSED`/`ACTIVE` lookup states, state-aware HAL links,
  optimistic repository transitions, and mandatory audit. Pause and resume now require the caller's
  observed delivery-state version and repeat that predicate inside the atomic PostgreSQL update, closing
  an ABA race across pause/resume cycles. Focused Domain, Application, API, Infrastructure, and real-
  PostgreSQL persistence tests pass, including stale-version and ABA rejection; the Local/Cerbos policy
  contracts cover the new operator actions. `dotnet ef migrations has-pending-model-changes` reports no
  model drift, so no migration was generated or edited. Live Aspire HTTP QA proves handler-authorized
  reads, controller-authorized writes, exact HAL pause/resume switching, versions `1 -> 2 -> 3`, a
  stale-version `409` after a complete pause/resume ABA cycle, and normalized success audit records.
  Provider-publication list returned `200`; authenticated missing reconcile/abandon returned `404`,
  anonymous handler-authorized read returned `403`, and anonymous write returned `401`. The temporary
  local Keycloak user, `sub` mapper, direct-grant toggle, and provider-key substitutions were removed;
  both original database identity keys are restored and the API is healthy after restart. Phase 5.1
  is complete.
- Phase 5.2 is complete. The dedicated `GetWebhookMessagePayloadQuery` requires
  `webhook:view-payload`, uses the tenant-leading message repository boundary, rejects invalid IDs
  before persistence, rechecks the UTC retention cutoff before reading, and returns exact bytes only
  through a purpose-specific base64 DTO. Missing/cross-tenant IDs share generic `404`; known expired
  or cleared tenant-local payloads return `410`. Every reached read outcome appends normalized
  `PAYLOAD_VIEWED` audit evidence, and successful bytes are not returned if audit persistence fails.
  The API action is anonymous at the controller and handler-authorized per project convention, emits
  `Cache-Control: no-store,no-cache` plus `Pragma: no-cache`, and its `payload` HAL relation exists
  only while retained and after the separate permission check. Tenant/instance admins are allowed;
  delegated organization admins and the dedicated incoming worker are denied in both Local and
  Cerbos policy contracts. The dedicated handler, controller/HAL, real HTTP cache-header, fallback
  authorization, live Cerbos decision, and all 410/404/default-DTO containment tests pass. Native
  Cerbos compilation executes 410/410 policy cases, including 64/64 webhook cases. Release builds
  for Application, API, generated Blazor client, and affected test projects have 0 errors; OpenAPI,
  NSwag client, and API inventory were regenerated through their normal build/test workflows.
  `dotnet ef migrations has-pending-model-changes` reports no model drift, so no migration was
  generated or edited. Phase 5.3 is active.
- Phase 5.3 implementation design is fixed: bulk replay will persist a tenant-scoped queued
  operation with a normalized status lookup, immutable filter snapshot, stable operation key plus
  request hash, preview/exclusion evidence, optimistic version, and pre-start cancellation. The
  worker transaction will re-evaluate eligibility and only reopen bounded terminal Local targets;
  ordinary Local delivery claims remain responsible for tenant/endpoint fairness and in-flight
  ceilings. Provider conflict, unknown, manual-reconciliation, active retention holds, unavailable
  payloads, inactive endpoints, and nonterminal/succeeded work remain excluded rather than being
  guessed or directly dispatched. This keeps controller requests short and makes cancellation,
  idempotency, and crash rollback durable.
- Phase 5.3 Domain and Application foundations are implemented and compile in Release with 0
  errors. The normalized `WebhookBulkReplayStatusLookup`, durable operation aggregate, immutable
  preview snapshot, repository/service boundaries, bounded management DTOs, manual validators,
  tenant-scoped authorized queries/commands, stable SHA-256 request identity, schedule-time
  advisory-lock contract, tenant reserved-capacity check, mandatory normalized audit, and
  optimistic queued-only cancellation are in place. Persistence, worker execution, API/HAL,
  generated migration, tests, and live QA remain active work before any 5.3 checkbox is closed.
- Phase 5.3 persistence, worker, and API foundations are now implemented and compile in Release
  with 0 errors. EF maps tenant-leading optional consumer/endpoint foreign keys, the normalized
  status table, lifecycle/count/hash constraints, operation-key uniqueness, queue/filter indexes,
  and an application-managed concurrency token. Preview classifications are disjoint and set-based;
  schedule/execution use tenant advisory locks; execution rechecks holds, payload retention, endpoint
  activity, and terminal Local state before reopening at most the operation limit. A fresh-scope
  worker completes or safely fails each operation with normalized system audit. The management API
  exposes preview, `202` schedule, list/detail polling, and queued-only optimistic cancellation with
  HAL and tenant-admin-only Cerbos authority. Migration
  `20260714171512_AddWebhookBulkReplayOperations` was generated exclusively by `dotnet ef`; no
  generated migration, designer, or snapshot file was hand edited. Tests and live QA remain active.
- Phase 5.3 focused verification is green across Domain, Application, Infrastructure, API, and
  real-PostgreSQL Persistence coverage. The tests prove aggregate lifecycle and optimistic
  cancellation, stable request identity and capacity rejection, transactional execution/failure
  settlement, tenant-scoped preview classifications, bounded eligible-target reopening, HAL and
  authorization behavior, lookup parity, and machine-scope parity. Native Cerbos compilation passes
  all 416 policy cases, including all 70 webhook cases. `dotnet ef migrations
  has-pending-model-changes` reports no model drift. The broader Cerbos contract run retains one
  pre-existing unrelated instance-setting denial; the replay-focused contract is green.
- Phase 5.3 operator documentation now covers the preview/list/detail/schedule/cancel routes,
  `MaterializedAt` half-open window semantics, terminal-Local-only eligibility, disjoint exclusion
  evidence, stable operation-key behavior, tenant capacity/advisory locks, ordinary delivery-worker
  fairness, queued-only cancellation, settings validation, and the tenant-admin/HAL authorization
  boundary. OpenAPI/client regeneration exposed that the provider-publication and bulk-replay HAL
  detail schemas were empty and their collection members were untyped. The canonical HAL schema
  catalog now registers the four concrete detail/filter/attempt DTOs and both collection mappings;
  the API build regenerated OpenAPI, the Blazor build regenerated its NSwag client, all five bulk
  replay routes appear in the generated inventory, and all 20 stable OpenAPI invariants pass.
  Live Aspire HTTP QA initially exposed stale mutable Cerbos state: the PDP derived the tenant-admin
  role but its persisted webhook policy predated `webhook:bulk-replay`. Context7-backed direct
  decision probes isolated the drift; publishing the current resource policy through the documented
  self-hosted Admin API and reloading the PostgreSQL store changed that exact decision from deny to
  allow without weakening the policy. Live API QA then proved `400` invalid-window validation,
  a `200` preview with one eligible and one disjointly excluded Local target, anonymous read `403`,
  anonymous write `401`, and HAL collection/detail affordances. A queued operation cancelled before
  execution moved from version 1 to normalized `CANCELLED` version 2 and lost its cancel link. A
  second stable key returned the same operation on exact replay, rejected changed parameters with
  `409`, completed through the hosted processor with one scheduled target and version 3, then
  rejected late cancellation with `409`. The ordinary Local worker reprocessed that target through
  its existing retry/fairness path and returned it to `DEAD_LETTERED`; normalized audit rows record
  scheduled, cancelled, completed, and rejected outcomes with bounded safe metadata. Phase 5.3 is
  complete; the disposable local Keycloak QA identity remains only for Phase 5.4 browser QA and will
  be removed with its temporary client mappers/direct-grant toggle afterward.
- Phase 5.4 generated-contract UI implementation is complete and browser verification is active.
  A dedicated `IWebhookOperationsService` keeps sensitive payload reads, optimistic endpoint
  pause/resume, provider reconciliation/abandonment, and replay preview/schedule/cancel calls behind
  the NSwag client; Razor never calls HTTP directly. The existing management panel now exposes
  audited decoded/exact-base64 payload views and Local delivery controls only when message/endpoint
  HAL relations exist. Separate provider-publication and bulk-replay components render collection
  and item actions exclusively from HAL, retain observed concurrency versions, require normalized
  reason evidence, invalidate stale previews after filter changes, and explain unavailable controls
  using provider capability and normalized lifecycle state rather than role guesses. The generated
  client service tests and the full 1,647-case Blazor client suite pass, including responsive
  MudTable/date-filter component coverage for Svix and Local modes. Live Aspire browser QA now
  proves all six webhook tabs render, endpoint pause/resume follows HAL changes, retained payloads
  open in decoded and exact-base64 no-cache views while cleared payloads expose no action, and Local
  mode exposes no provider-publication action. The generated client serializes `DateTimeOffset`
  query/body values without an offset, so the API boundary now normalizes offsetless replay dates to
  UTC before CQRS validation; focused API tests cover both preview and schedule. Live replay preview
  then returned one eligible and one disjointly excluded target, scheduling returned `202`, and the
  hosted processor completed it through the ordinary Local queue. A browser cancellation attempt
  correctly reached the queued-only optimistic action but lost the race with the processor and
  returned `409`; the earlier live API proof remains the successful queued-cancellation evidence.
  Final browser QA at 1280, 768, and 375 CSS pixels now proves zero page-level horizontal overflow,
  compact labeled-row tables inside the nested settings layout, one-column mobile replay controls,
  visible tab-scroll affordances, light/dark token contrast, dialog focus trapping, Escape dismissal,
  and focus restoration. Webhook dialogs now use the repository `DialogOptionsFactory` presets and
  multi-value table cells use explicit content wrappers so labels stay aligned in compact mode. The
  browser console emitted only pre-existing shell CSP/notification-stream errors, with no webhook
  component exception. Fresh device-scale screenshots were manually inspected; the independent
  reviewer gate is unavailable because the user explicitly prohibited subagents. The full Blazor
  client project test run passes again after replacing four expression-tree-incompatible `is not
  null` test predicates with equivalent null comparisons. Phase 5 is complete.
- Phase 6.1 discovery confirms the existing safe webhook metrics and separate Local/Svix
  health checks are a partial base, not the exit state: claim lag, provider-publication
  retry/dead-letter/unknown/manual-reconciliation outcomes, and provider-health signals
  still need bounded instruments. Current .NET guidance was revalidated through Context7:
  `System.Diagnostics.Metrics` remains the OpenTelemetry-compatible instrumentation API,
  while health-check tags/predicates are the supported way to expose independent
  readiness views for optional capabilities.
- Phase 6.1 implementation adds an enum-backed closed telemetry vocabulary, claim-lag and
  unknown-publication-age histograms, bounded processing/retry/dead-letter/manual-
  reconciliation/auto-pause/provider-health counters, and durable-boundary instrumentation in
  Local and self-hosted Svix workers. Auto-pause counts only the state transition. Cleanup mode
  and data-kind inputs now collapse outside the allowlist, and a 1,000-measurement load test proves
  unsafe unique values form one `unknown` series. Webhook worker/provider catch paths log only
  exception types, not exception objects or raw provider data. Focused Application metrics and all
  Local/Svix readiness, provider dispatch-identity, and unknown-reconciliation tests pass.
- Phase 6.2 exposes `/health/webhooks/local` and `/health/webhooks/svix` as tag-filtered independent
  views while preserving aggregate `/health`; Svix readiness now fails when its publication
  processor is disabled and continues to fail closed on unsupported/zero-evidence capability
  tuples before secret resolution. The managed-Svix-SaaS harness is now a TUnit `Explicit`
  manual lane: it remains directly selectable when cloud evidence is intentionally requested,
  but its deliberately empty cloud credentials no longer contaminate normal self-hosted runs.
  The full Infrastructure suite passes 824/824.
- Phase 6.3 adds `docs/WEBHOOK_OPERATIONS_RUNBOOK.md` with bounded cardinality rules, initial SLOs,
  PromQL alert templates, independent readiness, Local-only and self-hosted Svix startup/outage,
  unknown/manual reconciliation, inside/outside-window credential rotation, auto-pause/resume,
  retention hold/cleanup recovery, and backup/restore/forward-fix procedures. It reiterates that
  migrations are generated only with `dotnet ef` and generated artifacts are never hand edited.
- Phase 6.2 live Local-mode QA now proves the independent tag-filtered surfaces: the Local endpoint
  returns HTTP 200 with only `webhook-local-delivery`, zero due/stale targets, and no sensitive
  values; the Svix endpoint independently returns HTTP 200 with only `webhook-svix-provider` and
  `svixProviderSelected=false`. A live `/metrics` scrape exposes only the expected bounded
  `provider=local,outcome=healthy` and `provider=svix,outcome=not_selected` health series. The API
  Debug build completed with 0 errors before the Aspire resource restart.
- Final verification evidence now includes a 26-project Release build with 0 errors; passing full
  Application, Domain, Secrets, Persistence, Infrastructure (824/824), Blazor integration, and
  Blazor client projects; 270 passing/1 skipped/4 failing Architecture cases; and the 1,780
  passing/3 skipped/8 failing API result above. The webhook HAL permission architecture test,
  portal containment class, duplicate-capture redaction case, bounded-metric tests, independent
  readiness tests, and supported self-hosted conformance selector pass. `dotnet ef migrations
  has-pending-model-changes` reports no model drift. Two complete OpenAPI/inventory/NSwag
  regeneration cycles produced identical hashes. The final source audit found and removed the
  last exception-object log from the Svix event-type sync worker; webhook logs now retain only
  bounded exception-type metadata. Aspire and all disposable Keycloak/database QA state were
  removed after the live verification.
- The four Management architecture failures are resolved at their source. Seven management
  commands and seven command handlers now use the canonical `Requests.Commands` and
  `Handlers.Commands` namespaces; management queries and query handlers use their matching query
  namespaces; and seven management transport types now carry the required `Dto` suffix with no
  compatibility aliases. The public-handler and handler-naming fitness functions now select actual
  `*Handler` types before asserting visibility/naming, so private nested value records are no longer
  misclassified as handlers. The focused CQRS and naming convention suites pass after a fresh
  Release architecture build. Full architecture verification remains part of the final gate.

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
- [x] Add publication DTO mapping in the management/read handlers introduced in Phase 5.
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
- [x] Otherwise transition to `ManualReconciliation` and expose safe HAL action.
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

- [x] Model endpoint management, attempts, replay, payload inspection, portal,
  catalog, retention, app/endpoint throttling, transformations, ordering, callbacks.
- [x] Resolve capabilities by provider and supported version.
- [x] Validate configuration writes and startup against capabilities.
- [x] Drive readiness, HAL, and UI explanations from capabilities.
- [x] Do not claim Local parity for provider-native features.

### 3.3 Enforce immutable configuration snapshots

- [x] Persist delivery plan, provider mode, bindings, endpoint config version,
  event-contract version, and retention snapshot when materializing.
- [x] Make later configuration changes affect new messages only.
- [x] Add explicit authorized/audited migration for eligible pending work.
- [x] Warn on unknown/reconciliation publications during config change.
- [x] Require an explicit pending-work decision; never silently reroute.

### 3.4 Verify binding/capability/snapshot migration

- [x] Backfill deterministic snapshots for legacy pending work.
- [x] Mark unverifiable provider identity as `LegacyUnverified`.
- [x] Run Local/Svix mode-change and endpoint-version concurrency tests.
- [x] Run latest-release migration smoke and representative lock timing.

**Phase 3 exit:** each consumer has a verified provider boundary, capabilities are
typed/versioned authority, and queued work cannot silently change routing semantics.

## Phase 4: Local Enterprise Operations

### 4.1 Add fairness and concurrency tests first

- [x] Enforce global in-flight maximum.
- [x] Enforce per-tenant and per-endpoint maximums.
- [x] Enforce max items per tenant per claim cycle.
- [x] Prove a noisy tenant cannot starve another tenant.
- [x] Prove lease/fence behavior under multiple workers.
- [x] Prove instance locks prevent unauthorized tenant overrides.

### 4.2 Implement bounded delivery behavior

- [x] Add configurable instance defaults and governed tenant overrides.
- [x] Use exponential backoff with full/equal jitter.
- [x] Honor only bounded valid `Retry-After`.
- [x] Bound endpoint timeout and response-body read.
- [x] Reject redirects and apply existing outbound URL/SSRF validation.
- [x] Add configured sustained-failure circuit/auto-pause.
- [x] Add authorized HAL-gated manual resume.

### 4.3 Complete Standard Webhooks conformance fixtures

- [x] Sign `message-id.timestamp.raw-payload` with HMAC-SHA256.
- [x] Verify exact bytes without JSON reserialization.
- [x] Support current/previous/multiple signatures during rotation.
- [x] Use constant-time HMAC comparison.
- [x] Cover UTF-8/non-ASCII, whitespace/newline mutation, stale/future timestamps,
  altered fields/payload, malformed base64, duplicate headers, tolerance boundaries.
- [x] Share fixtures across signer/verifier tests where applicable.

### 4.4 Implement mandatory append-only audit

- [x] Add audit entity/storage with principal, effective scope, action, target,
  safe before/after, config/policy version, correlation, reason, outcome, DB time.
- [x] Audit consumer/endpoint lifecycle, secret rotation, mode/config/retention,
  binding, portal issuance fact, retry/redrive/reconcile/abandon/bulk replay,
  auto-pause/resume, and pending-work migration.
  - [x] Existing lifecycle, provider configuration, portal, retry/redrive, circuit, resume,
    binding, and pending-work migration operations emit normalized mandatory audit.
  - [x] Retention-policy/cleanup audit closes with 4.5.
  - [x] Operator reconcile/abandon audit closes with 5.1.
  - [x] Bulk-replay audit closes with 5.3.
- [x] Commit audit with business write where possible.
- [x] Prevent security-sensitive success response when required audit persistence fails.
- [x] Prove payloads, secrets, signatures, portal values, and raw provider errors are absent.

The remaining action-matrix children depend on later business operations and are explicitly
parallel with their owning tasks; they do not block starting 4.5. Phase 4.4 itself remains open
until those children are closed.

### 4.5 Implement retention and cleanup

- [x] Add separate policies for inbound/outbound payload, attempts, dead-letter
  evidence, publications/mappings, logs, and admin audit.
- [x] Snapshot resolved policy on materialized work.
- [x] Add retention hold.
- [x] Make cleanup tenant-scoped, bounded, idempotent, and observable.
- [x] Exclude nonterminal, unknown/reconciliation, replay-window, and held data.
- [x] Preserve minimum identities, hashes, outcomes, and audit after payload deletion.
- [x] Map supported per-message Svix retention while keeping local minimum evidence.

### 4.6 Verify Local-only production profile

- [x] Run Docker/Aspire with Svix absent and Local configured.
- [x] Prove startup, readiness, materialization, delivery, retry, auto-pause/resume,
  retention cleanup, and audit.
- [x] Prove no Redis/Kafka/CDC/new proxy dependency is required.

**Phase 4 exit:** Local is fair, bounded, signature-conformant, audited, retention-
governed, and independently production-operable.

## Phase 5: Operations API and Blazor UI

### 5.1 Add HAL-governed operational resources

- [x] Add authorized handlers/endpoints for retry/redrive, reconcile, abandon,
  pause/resume, and eligible pending-work migration.
- [x] Add state/capability/authorization-aware HAL policies.
- [x] Keep controller GETs anonymous and handler-authorized per project rules.
- [x] Keep writes controller-authorized and handler-authorized.
- [x] Add Local/Cerbos parity tests for every action.

### 5.2 Add separately authorized payload access

- [x] Add `view-payload` action/resource authorization.
- [x] Emit payload HAL relation only when authorized, retained, and state-appropriate.
- [x] Return no-store response and write access audit.
- [x] Return retention-aware `404`/`410` without leaking cross-tenant existence.
- [x] Keep payload out of all default list/detail DTOs.

### 5.3 Add bounded bulk replay

- [x] Add preview with explicit filters and estimated eligible/excluded counts.
- [x] Add maximum batch and per-tenant limits.
- [x] Require reason, stable idempotency identity, and audit.
- [x] Allow cancellation before execution starts.
- [x] Exclude conflict, unknown, manual-reconciliation, held, and ineligible work.
- [x] Preserve fairness and endpoint protections.

### 5.4 Update Blazor through generated contracts

- [x] Regenerate OpenAPI/client after API changes.
- [x] Render all actions from HAL relation presence only.
- [x] Explain unavailable actions from safe capability/state metadata, not role guesses.
- [x] Add responsive/accessibility and component tests for Local and Svix modes.
- [x] Run browser E2E for portal, payload access, replay, pause/resume, and denial paths.
  - Live complete: pause/resume, retained/cleared payload gating, Local portal/provider-action
    absence, replay preview/schedule, optimistic late-cancel conflict, cross-breakpoint containment,
    keyboard/focus, light/dark theme, and fresh screenshot review.

**Phase 5 exit:** operators can safely inspect and act without bypassing tenant,
authorization, capability, retention, or HAL boundaries.

## Phase 6: Observability, SLOs, and Runbooks

### 6.1 Add bounded telemetry

- [x] Add metrics for claim lag, processing/delivery outcome, retries, dead letters,
  unknown/manual reconciliation, auto-pause, cleanup, and provider health.
- [x] Use only bounded labels such as provider, operation, and outcome.
- [x] Exclude tenant/message/endpoint/event IDs and URLs from metric labels.
- [x] Redact payloads, secrets, signatures, portal values, and raw provider errors
  from logs/traces.

### 6.2 Add independent readiness and SLOs

- [x] Report Local readiness independently of optional Svix readiness.
- [x] Validate provider/version capability at startup/readiness.
- [x] Define SLOs and alerts for lag, retry rate, dead-letter rate, unknown age,
  paused endpoints, and cleanup backlog.
- [x] Verify cardinality and redaction under load.

### 6.3 Write operational runbooks

- [x] Local-only startup and incident recovery.
- [x] Svix-enabled startup and provider outage.
- [x] Unknown publication and manual reconciliation.
- [x] Credential rotation inside/outside idempotency window.
- [x] Tenant endpoint auto-pause/resume.
- [x] Retention hold and cleanup failure.
- [x] Migration forward-fix and backup restore.

**Phase 6 exit:** operators can detect, diagnose, and recover each supported mode
without exposing sensitive data or relying on undocumented provider behavior.

## Phase 7: Typed Instance/Tenant/Organization/Group/User Ownership

### 7.1 Normalize the ownership aggregate

- [x] Replace the ambiguous system-integration kind with an explicit Instance lookup/enum value.
- [x] Enforce exactly one Instance, Tenant, Organization, Group, or User owner per consumer.
- [x] Resolve Organization, Group, and User owners inside the declared tenant.
- [x] Require completed immutable instance identity for instance-owned consumers.
- [x] Propagate configuration scope to endpoints, subscriptions, and provider bindings.
- [x] Add PostgreSQL check constraints, typed foreign keys, and scope-leading indexes.
- [x] Generate the migration only with `dotnet ef`; do not hand-edit generated artifacts.

### 7.2 Implement authoritative CQRS ownership resolution

- [x] Add owner-scoped create, list, detail, update, endpoint, portal, and delivery operations.
- [x] Resolve persisted owner attributes before authorization; never trust caller-supplied ownership.
- [x] Keep tenant-owned records behind named tenant filters and instance rows hidden by default.
- [x] Permit explicit instance operations only with bounded instance/consumer predicates.
- [x] Write mandatory audit evidence at the actual effective owner scope.

### 7.3 Enforce authorization and HAL parity

- [x] Grant instance administrators instance-wide ownership operations.
- [x] Grant tenant administrators tenant-wide ownership operations.
- [x] Grant organization and group administrators only their owned webhook resources.
- [x] Grant users only their own user-owned webhook resources.
- [x] Keep provider repair, payload, reconciliation, abandon, and bulk replay at explicitly approved scopes.
- [x] Add Local/Cerbos parity and unrelated-owner denial tests for all five owner kinds.
- [x] Emit every management affordance exclusively through owner-aware HAL policies.

### 7.4 Complete delivery and UI scope propagation

- [x] Preserve source tenant identity when an explicitly targeted instance consumer receives a tenant event.
- [x] Reject cross-tenant Organization, Group, User, endpoint, subscription, and binding substitution.
- [x] Keep provider application identity isolated per consumer and immutable instance identity.
- [x] Regenerate OpenAPI and the NSwag client from source.
- [x] Add owner-scoped webhook panels for instance, tenant, organization, group, and user settings.
- [x] Gate every Blazor action by HAL relation presence and add responsive/accessibility tests.
- [x] Run browser E2E for allowed and denied ownership paths.
  - **Completed 2026-07-15:** the Playwright/Aspire scenario provisions a real
    multi-tenant customer administrator through the production managed-provider workflow,
    creates Instance-, Tenant-, Organization-, Group-, and User-owned consumers through
    generated API contracts, and exercises all five settings panels. It verifies tenant-only
    sensitive tabs; asserts `403` for instance, unrelated-organization, unrelated-group, and
    unrelated-user selection; and captures visually reviewed desktop, tablet, and mobile
    evidence for every owner. Browser navigation now waits on an `OnAfterRender` readiness
    marker so prerendered HTML cannot accept inert clicks before the Blazor circuit hydrates.

**Phase 7 exit:** every webhook configuration has one database-enforced typed owner;
instance, tenant, organization, group, and user administrators see and operate only
their authorized resources; Local/Cerbos and API/HAL/Blazor behavior are equivalent.

## Conditional Later Tasks

These require a new approved plan and are not implementation shortcuts:

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
- [ ] Every supported self-hosted Svix conformance profile has executed evidence
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
