<!-- ABOUTME: Atomic execution ledger for the secrets authority and control-plane refactor. -->
<!-- ABOUTME: Sequences security invariants, breaking cleanup, safe surfaces, and operator recovery. -->

# Secrets Authority And Control Plane Refactor — Tasks

Last Updated: 2026-08-30 Europe/Brussels

## Status And Rules

- **Implementation status:** User-authorized; Phase 0 governance is complete and
  `SEC-001` is the exact next task. GATE-001, GATE-002, GATE-003, `GOV-001`, and
  `GOV-002` are complete. Whole
  local-development database/volume recreation is separately authorized within the
  exact environment and target-confirmation boundary below.
- **Legend:** `[ ]` pending, `[~]` in progress, `[x]` complete, `[!]` blocked.
- **Effort:** `S` under 2h, `M` 2–6h, `L` 6–12h, `XL` over 12h.
- Check one atomic task at a time. Update this file immediately when evidence exists.
- Red tasks are reserved for source authority, confidentiality, tenant races,
  rotation rollback, and secret-free export boundaries.
- **Single-agent rule:** never have more than one subagent active or dispatched at a
  time; read its complete output before any later delegation. No parallel swarms.
- **Code-first rule:** do not perform irrelevant cleanup, unrelated failure analysis,
  repeated review, phase-exit builds, Green test runs, app startup, Aspire/browser
  QA, provider matrices, runbook execution, or MAD during implementation. Author the
  required code/tests/wiring and defer execution to `SEC-405`/`FINAL`.
- **Only early executions:** run the focused `GOV-002` contract validator once before
  product edits and the smallest named Red slice once for each mandatory Tier 1
  invariant task. Directly blocking syntax/compiler diagnosis is allowed only to
  resume the planned code edit; it is not task-completion evidence.

## Approval Gate

- [x] **GATE-001 — Revalidate I-VSD against rewritten plan/tasks** (`M`)
  - **Files:**
    `islamic-value-sensitive-design/i-vsd-secrets-refactor-control-plane.md`, this
    plan triad.
  - **Work:** Run I-VSD planning mode against exact SHA-256 digests; preserve stable
    findings or record replacements; map every material finding to a scenario and
    task.
  - **Acceptance:** Report is `current` / `plan-aligned` or names an explicit
    unresolved escalation; no stale mapping remains.
  - **Dependency:** Corrected authoritative plan/tasks/context bytes and exact
    SHA-256 digests.
  - **Current state:** Fresh planning-mode revalidation is `current` /
    `plan-aligned`; stable findings were remapped and portability/destructive
    authority findings were added without granting CTO or product approval.

- [x] **GATE-002 — Obtain revision-bound CTO review** (`S`)
  - **Files:** `secrets-refactor-control-plane-cto-review.md` and triad.
  - **Work:** Review the exact post-I-VSD revisions for completeness, correctness,
    coherence, worst-break coverage, right-sizing, and operator recovery.
  - **Acceptance:** Decision is `Approve` or all required changes are applied and
    re-reviewed; rewrite-mode self-approval is not used.
  - **Dependency:** Freshly completed `GATE-001` for the corrected bytes.
  - **Decision:** `Approve` for plan
    `sha256:fed15e71ffeb739aa2dd2e62ef06317fcdde060420e9a7c4c9093105e295f6c9`,
    tasks
    `sha256:10a49e6dcfd55e39234dba068eed6a22634f1ed06f3419f19861d7b425772f84`,
    combined
    `sha256:825b965e4937a7544ec99ea93c20456f2737c43441d45d44ac00549586805757`,
    and I-VSD
    `sha256:4ca3e870b100cbe3da05aa115f559ff68c6612c19bc33c78f838481d33f52617`.
    GATE-002 is complete; this decision does not grant GATE-003 product approval or
    destructive execution authority.

- [x] **GATE-003 — Record product approval and bind the authorized local reset** (`S`)
  - **Files:** plan metadata, context, and tasks.
  - **Work:** After GATE-002, obtain explicit product implementation approval. Carry
    forward the already granted permission to dispose/recreate whole LOCAL
    DEVELOPMENT databases and volumes only when needed for the clean migration path.
    Immediately before execution record each target's environment, provider,
    database/container identity, and volume/path, and prove it is local/non-shared.
  - **Acceptance:** Plan/context/tasks agree on the approved implementation revision
    and bounded destructive authority. Production, shared, staging, CI evidence,
    external-provider/Infisical state, deployment secret stores, and unnamed
    databases/volumes remain excluded. Before each later destructive command,
    `SEC-104` MUST confirm the exact local environment/provider/database/container/
    volume identity and stop on ambiguity. Gate completion does not claim that any
    target has been proven or reset. The permission includes all data and
    database-resident Data Protection material in a confirmed local target, not only
    `SecretBinding` rows.
  - **Dependency:** `GATE-002`.
  - **Decision:** The user explicitly approved full implementation with no backward
    compatibility against combined plan/tasks
    `sha256:a6255e78747ee7d85f42b27b213a5a0c3db1f250c0b24702856b4b6000445f37`
    and selected `Whole development databases`. GATE-003 is complete. No destructive
    target has been proven or reset; per-target confirmation remains an `SEC-104`
    execution precondition.

## Phase 0 — Contribution Contract Prerequisite

- [x] **GOV-001 — Add a Tier 1 secrets-authority contribution contract** (`M`)
  - **Files:** `.agents/contract/intents.yaml`, matching `.agents/rules/*.md` and
    `.omo/rules/*.md` twins only where path routing requires them, and contract
    validation tests/docs.
  - **Work:** Cover `src/Explore.Domain/Secrets/**`, `src/Explore.Secrets/**`,
    `.env.example`, directly owned tests/docs, and the seven-PR intent matrix. Inherit
    security intake, invariant tests, zero-secret outputs, generated-artifact rules,
    approval-gated development-data reset, and applicable secondary intents.
  - **Acceptance:** Every planned path has an authoritative intent with exact scope,
    minimum tests, docs, acceptance, and forbidden actions; no existing intent is
    weakened.
  - **Dependency:** `GATE-003` (complete).
  - **Evidence:** The corrected eight-file contract draft is present and the focused
    `GOV-002` validator confirmed its schema, references, governance ownership,
    routing, expected route sets, and conflict precedence.

- [x] **GOV-002 — Validate contribution-contract routing** (`S`)
  - **Files:** governance artifacts changed by `GOV-001` only.
  - **Work:** Run the focused intent/schema/reference/routing validator once and
    record the PR 0–6 mapping. Defer architecture, broad links, and all product tests
    to `SEC-405`; do not iterate unrelated baseline failures.
  - **Acceptance:** Focused contract validation is green and the changed-file-to-
    intent mapping for PRs 0–6 is recorded in context.
  - **Dependency:** `GOV-001`.
  - **Evidence:** `dotnet run eng/agent-context/validate-contract.cs -- . --intent
    secrets-authority` exited `0` on 2026-08-30 and reported all contract, benchmark,
    ownership, reachability, route-set, and precedence checks passing.

## Phase 1 — Authority Contract And Fail-Closed Bootstrap

- [x] **SEC-001 — Red: deterministic source authority invariant** (`M`)
  - **Files:**
    `tests/Explore.Secrets.UnitTests/Bootstrap/BootstrapSecretLoaderTests.cs`,
    `tests/Explore.Secrets.UnitTests/Services/SecretResolverBindingTests.cs`.
  - **Work:** Add adversarial cases where Infisical is selected, a lower-authority
    environment/config value exists, and provider resolution is absent,
    unauthorized, invalid, or unavailable.
  - **Acceptance:** Tests fail because current code falls back or collapses failure;
    assertions verify no lower source is read and no sensitive material is output.
  - **Dependency:** `GOV-002`.
  - **Evidence:** The focused `BootstrapSecretLoaderTests` slice compiled and ran
    16 tests on 2026-08-30: the 12 retained cases passed and the four new Infisical
    absence/invalid/unavailable/unauthorized cases failed because current bootstrap
    returned lower-authority environment credentials instead of failing closed.
    The typed `ISecretResolver` public-contract assertion is also authored for final
    verification. Exit `2` is the required Red evidence; this slice is not rerun
    during implementation.

- [x] **SEC-002 — Define typed secret and capability outcomes** (`M`)
  - **Files:** `src/Explore.Application/Contracts/Secrets/`,
    `src/Explore.Domain/Secrets/SecretDefinition.cs`,
    `src/Explore.Domain/Secrets/SecretDefinitionRegistry.cs`.
  - **Work:** Model resolved, unconfigured, unavailable, unauthorized, and invalid
    outcomes plus required/core versus optional capability policy. Keep Domain free
    of provider/framework dependencies.
  - **Acceptance:** Callers can distinguish policy-relevant outcomes without secret
    values in errors; definitions own required/optional classification.
  - **Dependency:** `SEC-001`.
  - **Evidence:** Added the closed `SecretResolutionResult` /
    `SecretResolutionStatus` contract and a value-free diagnostic representation.
    `SecretDefinition.Requirement` now derives `Core` versus `OptionalCapability`
    from the existing bootstrap invariant, so non-bootstrap failure remains scoped
    to its owning capability.

- [x] **SEC-003 — Replace bootstrap precedence with explicit source mode** (`L`)
  - **Files:** `src/Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs`,
    `src/Explore.AppHost/AppHost.cs`, `.env.example`, `docker-compose.yml`,
    current Standalone composition files discovered immediately before edit.
  - **Work:** Select environment or Infisical authority explicitly; environment
    mode reads environment only; Infisical mode uses environment-injected
    secret-zero credentials and fails closed. Delete legacy aliases and appsettings
    or User Secrets origins.
  - **Acceptance:** `SCN-SEC-001` and `SCN-SEC-002` pass for bootstrap; supported
    topologies preserve SQLite/Postgres behavior without ambiguous precedence.
  - **Dependency:** `SEC-002`.
  - **Evidence:** `SecretAuthorityConfiguration` accepts only explicit
    `Environment` or `Infisical`, builds an isolated authority configuration, and
    rejects unsupported/missing modes. Bootstrap deleted its second Infisical HTTP
    client and per-field fallback chain. API, Blazor, MigrationService, AppHost,
    Standalone composition, Compose, launch profiles, and `.env.example` now carry
    the selected mode and one canonical Infisical bootstrap schema. Green execution
    remains deferred to `SEC-405`.

- [x] **SEC-004 — Redact bootstrap and provider diagnostics** (`M`)
  - **Files:** `src/Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs`, relevant
    bootstrap diagnostics tests and shared redaction helpers already in the repo.
  - **Work:** Replace stderr bodies, exception messages, paths, keys, project/client
    identifiers, and values with bounded reason codes and safe remediation text.
  - **Acceptance:** Canary provider payloads do not appear in stderr/log captures;
    operators can identify setting class and remediation without coordinates.
  - **Dependency:** `SEC-003`.
  - **Evidence:** Both API/Secrets and Blazor Infisical startup providers discard
    response bodies and exception chains, omit URLs/paths/project/client/key data,
    fail list operations closed, and emit only bounded `secret_authority_*` reason
    codes. Reload retains last-known-good data without exception output.

- [x] **SEC-005 — Ship the authority-cutover operator contract** (`M`)
  - **Files:** `docs/SECRETS.md`, `docs/CONFIGURATION.md`,
    `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`, and `.env.example` where its
    key schema changes.
  - **Work:** In this authority-changing PR, remove database/appsettings/User Secrets
    authority and fallback claims; document explicit Environment versus Infisical
    mode, secret-zero inputs, required/optional bootstrap behavior, value-free
    diagnosis, and immediate recovery/re-provision commands for Standalone, Aspire,
    and Compose.
  - **Acceptance:** Each changed behavior has same-PR operator instructions and
    executable command/key/link validation; no later phase is needed to make the
    authority contract truthful.
  - **Dependency:** `SEC-004`.
  - **Evidence:** `.env.example`, Secrets, Configuration, Self-Hosting, and
    Troubleshooting now specify explicit Environment/Infisical selection,
    secret-zero inputs, no fallback, bounded diagnostics, and Compose/Aspire/direct
    Standalone start/recovery commands.

- [x] **SEC-006 — Close Phase 1 implementation scope** (`S`)
  - **Work:** Reconcile changed files, source-mode implementation, tests, and operator
    docs against Phase 1 scope. Do not run Green tests, builds, QA, or MAD.
  - **Acceptance:** Phase 1 code and test wiring are complete; deferred commands and
    scenario owners are recorded for `SEC-405`.
  - **Dependency:** `SEC-005`.
  - **Evidence:** Phase 1 changed-file reconciliation and scoped diff whitespace
    check are clean. `SCN-SEC-001`, bootstrap `SCN-SEC-002`, and bootstrap
    `SCN-OBS-001` Green execution remain owned by `SEC-405` as required by the
    code-first cadence.

## Phase 2 — Remove Database Secret Values

- [x] **SEC-101 — Red: reject inline values and cross-tenant races** (`L`)
  - **Files:** `tests/Event.Domain.UnitTests/Entities/SecretBindingTests.cs`,
    Secrets resolver tests, and the smallest real-provider Persistence integration
    tests owning SecretBinding constraints.
  - **Work:** Add invariants that no definition/factory accepts inline values and
    hostile concurrent tenants cannot resolve/cache another tenant's metadata.
  - **Acceptance:** Tests fail against `InlineEncrypted` and prove scope-qualified
    isolation rather than mock call counts.
  - **Dependency:** `SEC-006`.
  - **Evidence:** Added domain representability, shared-cache concurrency, PostgreSQL
    repository concurrency, and provider-matrix metadata-only tests. The required
    Domain Red slice ran once: 17 tests, 16 passed, and the new invariant failed on
    the existing `InlineEncrypted` enum as expected.

- [x] **SEC-102 — Remove inline source from Domain and Application** (`L`)
  - **Files:** `src/Explore.Domain/Secrets/SecretBinding.cs`,
    `SecretBinding.Factory.cs`, source enums/definitions/events, and corresponding
    Application contracts/handlers located by graph immediately before edit.
  - **Work:** Delete `InlineEncrypted`, ciphertext/version state, Protect paths,
    source-switch behavior, and secret-bearing request/result contracts. Preserve
    only metadata that enforces source/scope/reference behavior.
  - **Acceptance:** Domain cannot represent a stored secret value; no compatibility
    enum/field/adapter remains.
  - **Dependency:** `SEC-101`.
  - **Evidence:** Domain enum/state/factories and Application inline-protection
    contracts were deleted. Browser-facing Listmonk/localization value-write
    commands and API endpoints were removed. Managed registration now requires a
    deployment-owned binding; provider webhook results expose only an external
    provisioning requirement flag.

- [x] **SEC-103 — Remove inline infrastructure and DI** (`M`)
  - **Files:** `src/Explore.Secrets/Sources/InlineSecretSource.cs`,
    `src/Explore.Secrets/Extensions/SecretResolutionServiceCollectionExtensions.cs`,
    affected tests and Data Protection registration only where exclusively owned by
    inline secret storage.
  - **Work:** Delete inline adapter and registrations. Retain Data Protection used
    by unrelated platform features.
  - **Acceptance:** No runtime path can unprotect database ciphertext; unrelated key
    rings remain intact.
  - **Dependency:** `SEC-102`.
  - **Evidence:** Deleted `InlineSecretSource` and `InlineSecretProtector`, removed
    both DI registrations, and retained unrelated Data Protection key-ring paths.

- [x] **SEC-104 — Update persistence model and generate migrations** (`L`)
  - **Files:**
    `src/Explore.Persistence/Configurations/Entities/SecretBindingConfiguration.cs`,
    `ExploreDbContext`, provider migration projects/snapshots generated by current
    repository tooling, and `schemas/islamu-event.md`.
  - **Work:** After GATE-002 and product implementation approval, remove inline
    columns/constraints/index assumptions, enforce allowed opaque metadata groups
    and tenant/instance uniqueness, confirm exact local-development target identities,
    reset only authorized local databases/volumes, and regenerate clean provider
    baselines through repository tooling. Stop if any target is production, shared,
    staging, CI evidence, external-provider/Infisical state, a deployment secret
    store, unnamed, or ambiguous.
  - **Acceptance:** Every supported provider initializes from generated artifacts;
    the canonical schema agrees; no migration/snapshot is hand-edited; no destructive
    compatibility migration or fictitiously reversible `Down` path is introduced.
  - **Dependency:** `SEC-103`.
  - **Evidence:** With explicit user approval, EF removed and regenerated the five
    uncommitted `AddConfigurationImportSessions` artifacts against the pre-contraction
    model, then generated five isolated `RemoveInlineSecretValues` artifacts against
    the metadata-only model. The latter contain only the source-metadata CHECK
    replacement and two ciphertext-column drops. Server removal checks targeted
    unreachable `127.0.0.1:1`; SQLite used a repository-local throwaway file, so no
    retained database or volume changed. Canonical schema and lookup IDs agree.

- [x] **SEC-105 — Implement the five-provider clean-baseline matrix** (`L`)
  - **Files:** `.github/workflows/_build-test.yml`,
    `tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseProviderBehaviorContractTests.cs`,
    a new SecretBinding provider contract in the same test project, generated
    provider artifacts, and `schemas/islamu-event.md`.
  - **Work:** Extend the existing `database-provider-matrix` so PostgreSQL, SQLite,
    SQL Server, MariaDB, and MySQL each execute clean and idempotent MigrationService
    runs, `PrimaryDatabaseRuntimeSmokeTests`,
    `PrimaryDatabaseProviderBehaviorContractTests`, and hostile concurrent
    SecretBinding tenant/instance constraint cases. Upload one evidence artifact per
    provider. Do not execute the five-engine matrix until `SEC-405`.
  - **Acceptance:** All five named rows and artifact uploads are implemented and
    mapped to `SCN-TEN-001`; execution and generated-output inspection are deferred
    to `SEC-405`.
  - **Dependency:** `SEC-104`.
  - **Evidence:** Added `SecretBindingProviderContractTests` and a dedicated matrix
    step for PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL with provider-specific
    TRX evidence included in the existing per-provider artifact upload.

- [x] **SEC-106 — Close Phase 2 implementation scope** (`S`)
  - **Work:** Reconcile inline-source deletion, generated migrations, schema, tests,
    and CI matrix wiring. Do not run builds, Green suites, provider matrices, or MAD.
  - **Acceptance:** Phase 2 implementation is complete and all deferred migration,
    tenancy, provider, and architecture commands are recorded for `SEC-405`.
  - **Dependency:** `SEC-105`.
  - **Evidence:** Scoped whitespace validation is clean; non-generated source and
    schema contain no inline-secret representation. Migration/provider execution
    remains deferred to `SEC-405` as required by the code-first cadence.

## Phase 3 — Runtime Authority And Confidentiality

- [x] **SEC-201 — Red: zero-secret outputs across runtime surfaces** (`L`)
  - **Files:** Secrets tests plus existing Doctor/auth/database/audit redaction tests
    selected through graph relationships.
  - **Work:** Inject canary values into provider response bodies, exception messages,
    paths, keys, tokens, credentials, and IDs across resolver, validation, health,
    metrics, traces, ProblemDetails, and support diagnostics.
  - **Acceptance:** Tests expose current leaks and scan user-observable outputs,
    not raw source text.
  - **Dependency:** `SEC-106`.
  - **Evidence:** `SecretRuntimeRedactionTests` ran once: 2 executed, 2 failed on
    provider-body leakage/missing bounded reason codes before production changes.

- [x] **SEC-202 — Return typed provider failures without sensitive details** (`L`)
  - **Files:** `src/Explore.Secrets/Sources/InfisicalSecretSource.cs`,
    `EnvironmentSecretSource.cs`, `Infrastructure/InfisicalClientFactory.cs`,
    `Services/SecretResolver.cs`, and relevant Application contracts.
  - **Work:** Stop logging exception objects/provider coordinates; preserve
    cancellation; map failures to bounded reason codes; apply required/optional
    policy outside adapters.
  - **Acceptance:** Adapters return bounded unavailable/unauthorized/invalid outcomes
    distinct from unconfigured; consumer behavior remains owned by `SEC-203`.
  - **Dependency:** `SEC-201`.
  - **Evidence:** Sources, client factory, legacy provider boundary, and resolver
    now preserve cancellation and return/log only bounded typed outcomes.

- [x] **SEC-203 — Migrate resolver consumers to explicit policy** (`L`)
  - **Files:** production `ISecretResolver` callers in admission/promotion/recovery,
    registration providers, Listmonk, Stripe, Svix, SMTP, S3, analytics,
    localization, ATProto, Cerbos, ticketing recovery, and managed control-plane
    registration, plus their behavior-owning tests.
  - **Work:** Replace shared null semantics at every production boundary. Required
    admission/promotion/recovery material fails closed when its workflow is enabled;
    optional integrations expose an explicit unconfigured/degraded state confined
    to that capability. Preserve server-derived tenant authority.
  - **Acceptance:** `SCN-SEC-002` and `SCN-CAP-001` pass across the bounded caller
    inventory; no consumer guesses whether null means absent, failed, or forbidden.
  - **Dependency:** `SEC-202`.
  - **Evidence:** Production resolver callers use `SecretResolutionResult`; required
    cryptographic/payment/recovery flows fail closed and optional health surfaces
    expose bounded capability-local states.

- [x] **SEC-204 — Define bounded freshness and invalidation** (`M`)
  - **Files:** `src/Explore.Secrets/Services/SecretResolver.cs`, existing metrics and
    tests.
  - **Work:** Make cache key include tenant/instance/source-reference identity;
    document bounded freshness; invalidate on safe metadata changes; never cache
    authorization/failure as a successful absence. Use existing memory cache unless
    measured multi-replica behavior requires a separately approved design.
  - **Acceptance:** Concurrent tenant tests and freshness tests pass without Redis
    or speculative HybridCache.
  - **Dependency:** `SEC-203`.
  - **Evidence:** Successful values alone use the five-minute process-local cache;
    keys include scope/qualifier/source/binding identity and mutation invalidation
    removes all matching qualified entries.

- [x] **SEC-205 — Complete runtime health and observability** (`M`)
  - **Files:** existing Secrets health/metrics/diagnostic classes and tests.
  - **Work:** Expose low-cardinality unconfigured/degraded/failed-closed states and
    safe remediation links; audit mutations only; never persist reads or values.
  - **Acceptance:** full `SCN-OBS-001` passes and health distinguishes provider
    failure from unconfigured without coordinates.
  - **Dependency:** `SEC-204`.
  - **Evidence:** Metrics use only source/status tags, health separates
    unconfigured/available/unavailable, exception objects and coordinates are
    absent, and provider read-audit persistence was removed.

- [x] **SEC-206 — Ship the runtime failure and recovery operator contract** (`M`)
  - **Files:** `docs/SECRETS.md`, `docs/CONFIGURATION.md`,
    `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`, and health/operations docs
    directly changed by runtime outcomes.
  - **Work:** In this authority-changing PR, document typed outcomes,
    required/optional activation, process-local cache freshness/invalidation,
    capability health, and immediate value-free diagnosis/recovery. Do not describe
    rotation activation; Phase 4 owns it.
  - **Acceptance:** Runtime behavior and operator documentation ship together and
    command/key/link validation is executable; no source failure is documented as a
    clean miss.
  - **Dependency:** `SEC-205`.
  - **Evidence:** Secrets/configuration/self-hosting/troubleshooting docs now define
    typed outcomes, capability policy, bounded freshness, and value-free recovery.

- [x] **SEC-207 — Close Phase 3 implementation scope** (`S`)
  - **Work:** Reconcile typed outcomes, consumers, cache, health, canary tests, and
    operator docs. Do not execute Green suites, scans, builds, or MAD.
  - **Acceptance:** Phase 3 code/test wiring is complete, contains no rotation
    activation, and records `SCN-SEC-002`, `SCN-CAP-001`, and `SCN-OBS-001` final
    verification owners.
  - **Dependency:** `SEC-206`.
  - **Evidence:** Scoped whitespace validation is clean; Green `SCN-SEC-002`,
    `SCN-CAP-001`, and `SCN-OBS-001` execution remains owned by `SEC-405`.

## Phase 4 — Consumer Activation, Rotation, And Recovery

- [x] **SEC-221 — Red: replica-safe rotation invariants** (`L`)
  - **Files:** `RotationAwareHttpClientFactoryTests.cs`,
    `RotationAwareDbContextFactoryTests.cs`, provider refresh/cache tests, and new
    `SecretRotationReplicaConvergenceTests.cs` in `Explore.Secrets.UnitTests`.
  - **Work:** Add failing `SCN-ROT-001`–`SCN-ROT-003` cases for invalid candidates,
    failed local activation, one-of-two replica activation, missing acknowledgement,
    premature old-credential revocation, overlap expiry, stale replicas, and
    no-overlap maintenance restart. Tests MUST reject deployment success inferred
    from one process.
  - **Acceptance:** Red evidence fails current process-local behavior and proves
    partial activation/stale replicas cannot be called converged or healthy.
  - **Dependency:** `SEC-207`.
  - **Evidence:** `SecretRotationReplicaConvergenceTests` ran once: 3 executed,
    3 failed because HTTP returned no acknowledgement, database returned `void`,
    and no convergence guard existed.

- [x] **SEC-222 — Classify every consumer's supported rotation mode** (`L`)
  - **Files:** `src/Explore.Domain/Secrets/SecretDefinitionRegistry.cs`, direct
    resolver consumer inventory, and the provider×topology support contract.
  - **Work:** For HTTP options, database options, admission/promotion/recovery,
    tenant registration, Listmonk, Stripe, Svix, SMTP, S3, analytics, localization,
    ATProto, Cerbos, ticketing, and control-plane consumers, record owner and exactly
    one mode: `overlap-rollout`, `coordinated-restart`, or `unsupported-live`, plus
    candidate validation, acknowledgement, stale deadline, rollback, revoke, and
    break-glass behavior. Do not add distributed infrastructure.
  - **Acceptance:** Every family has one normative mode and executable evidence
    owner; unsupported live/zero-downtime claims are absent.
  - **Dependency:** `SEC-221`.
  - **Evidence:** `SecretDefinitionRegistry.GetRotationProfile` gives every known
    family one owner/mode and candidate, acknowledgement, stale, rollback,
    revocation, and break-glass contract.

- [x] **SEC-223 — Harden process-local automated rotation boundaries** (`L`)
  - **Files:** `src/Explore.Secrets/Services/RotationAwareHttpClientFactory.cs`,
    `RotationAwareDbContextFactory.cs`, `SecretRefreshService.cs`, resolver cache
    invalidation, and their tests.
  - **Work:** Implement candidate/validate/activate/verify/rollback only for the
    existing options-driven HTTP and database factories. Emit value-free local
    acknowledgement suitable for deployment coordination; never revoke provider
    credentials or claim cross-replica commit from a local callback.
  - **Acceptance:** `SCN-ROT-001` passes for both categories; an unverified candidate
    never becomes active and local evidence cannot be mistaken for deployment
    convergence.
  - **Dependency:** `SEC-222`.
  - **Evidence:** HTTP and database candidates validate before activation and emit
    value-free local acknowledgements; refresh exposes the same local-only evidence.

- [x] **SEC-224 — Implement overlap, restart, partial activation, and stale-replica coverage** (`L`)
  - **Files:** `SecretRotationReplicaConvergenceTests.cs`, existing deployment
    validation seams, and provider refresh/cache tests.
  - **Work:** Implement two-replica overlap coverage where old remains valid until all
    acknowledgements, one-replica failure/rollback, stale-replica drain/fail-closed
    at deadline, and no-overlap coordinated maintenance restart. Exercise every
    consumer mode that claims support; `unsupported-live` must reject activation.
    Defer Green execution to `SEC-405`.
  - **Acceptance:** `SCN-ROT-002` and `SCN-ROT-003` coverage is complete and wired for
    value-free evidence; concurrency/convergence execution is owned by `SEC-405`.
  - **Dependency:** `SEC-223`.
  - **Evidence:** Convergence tests cover all-replica same-attempt success, partial
    pending, deadline fail-closed, rejected candidates, and no-overlap restart.

- [x] **SEC-225 — Ship and validate rotation/recovery runbooks** (`L`)
  - **Files:** `docs/SECRETS.md`, `docs/SELF_HOSTING.md`,
    `docs/BACKUP_RESTORE_UPGRADE.md`, `docs/TROUBLESHOOTING.md`, and existing
    deployment validation tools/tests.
  - **Work:** In this authority-changing PR, document each consumer mode, provider
    overlap prerequisite, restart/maintenance sequence, acknowledgement threshold,
    partial-activation rollback, stale-replica drain, delayed revocation, recovery,
    and break-glass owner. Validate exact commands/links without secrets.
  - **Acceptance:** Every matrix row has same-PR executable evidence plus truthful
    operator instructions; no universal refresh or zero-downtime promise remains.
  - **Dependency:** `SEC-224`.
  - **Evidence:** Secrets/self-hosting/backup/troubleshooting docs now contain the
    mode matrix, acknowledgement/revocation gates, rollback, stale drain, and break glass.

- [x] **SEC-226 — Close Phase 4 implementation scope** (`S`)
  - **Work:** Reconcile rotation modes, factories, replica coverage, and runbooks.
    Do not execute Green suites, deployment QA, runbooks, builds, or MAD.
  - **Acceptance:** Phase 4 code/test/runbook wiring is complete and all deferred
    `SCN-ROT-001`–`SCN-ROT-003` commands are recorded for `SEC-405`.
  - **Dependency:** `SEC-225`.
  - **Evidence:** Phase 4 code/test/runbook wiring is complete; all Green rotation
    and replica convergence execution remains deferred to `SEC-405`.

## Phase 5 — Safe Control-Plane Visibility

- [x] **SEC-301 — Red: secret-free contract boundary** (`L`)
  - **Files:** configuration-manifest handler tests, API integration tests, generated
    contract/client tests, and BFF tests selected through graph relationships.
  - **Work:** Seed values/references/ciphertext-shaped canaries and assert export,
    API/OpenAPI/generated clients/BFF never return them or sensitive coordinates.
  - **Acceptance:** `SCN-MAN-001` fails on any recoverable value or source discovery
    material and does not scrape source code.
  - **Dependency:** `SEC-226`.
  - **Evidence:** `SecretFreeControlPlaneContractTests` ran once: 2 executed,
    2 failed because the existing overview had no server-side bounded status port.

- [x] **SEC-302 — Design the minimum server-authorized status surface** (`M`)
  - **Files:** existing settings/control-plane Application and API/HAL owners found
    through graph; no new route until reuse is evaluated.
  - **Work:** Apply the deletion test: reuse current settings status when it can
    express configured/degraded/required state; otherwise add the smallest CQRS/API
    slice. Authority comes from authenticated server context.
  - **Acceptance:** No generic secret CRUD, value input, provider admin credential,
    or browser-supplied tenant/admin authority is introduced.
  - **Dependency:** `SEC-301`.
  - **Evidence:** Reused `GetControlPlaneOverviewQuery`; no route, secret CRUD,
    value input, provider credential, or browser authority surface was added.

- [x] **SEC-303 — Preserve manifest omission and API/HAL contracts** (`L`)
  - **Files:** configuration-manifest boundary tests and only the existing
    settings/API/HAL/OpenAPI/generated-client owners changed by `SEC-302`.
  - **Work:** Prove the existing manifest remains closed and value-free; expose safe
    capability state and supported actions through server-authorized HAL only. Do
    not modify the manifest handler/schema unless a concrete defect is coordinated
    with `dev/active/configuration-manifest/`.
  - **Acceptance:** `SCN-MAN-001` passes; protected admin GET is documented as an
    explicit control-plane exception; clients receive no secret-bearing records.
  - **Dependency:** `SEC-302`.
  - **Evidence:** The snapshot contract has exactly provider/status/remediation code;
    manifest omission canaries include ciphertext and both supported source references.

- [x] **SEC-304 — Add BFF/UI affordances only for supported actions** (`L`)
  - **Files:** current Blazor settings/BFF components and tests discovered after
    `SEC-303` contract generation.
  - **Work:** Gate by HAL links, render value-free status/recovery guidance, strip
    privileged browser headers, and meet focus/keyboard/RTL/WCAG conventions. Omit
    UI for deployment-only actions.
  - **Acceptance:** No local claim/role gating or secret entry/storage exists;
    accessibility and BFF tests protect the actual behavior.
  - **Dependency:** `SEC-303`.
  - **Evidence:** Existing accessible provider cards render the new server status.
    No deployment-only action or secret input was added, so no new HAL relation,
    BFF endpoint, component, CSS, focus, keyboard, or RTL behavior was necessary.

- [x] **SEC-305 — Close Phase 5 implementation scope** (`S`)
  - **Work:** Reconcile API/HAL/OpenAPI/generated-client/BFF/UI implementation and
    tests. Do not run builds, Green suites, browser/accessibility QA, or MAD.
  - **Acceptance:** Phase 5 code/test wiring is complete and value-free/HAL/browser
    verification is recorded for `SEC-405`.
  - **Dependency:** `SEC-304`.
  - **Evidence:** Phase 5 reuses the authenticated overview/HAL/client path and adds
    no generated shape; deferred contract/UI/accessibility execution remains in `SEC-405`.

## Phase 6 — Deployment And Operator Convergence

- [ ] **SEC-401 — Align environment and topology contracts** (`L`)
  - **Files:** `.env.example`, `docker-compose.yml`,
    `src/Explore.AppHost/AppHost.cs`, Standalone composition, CI/Coolify deployment
    files, and current configuration schemas.
  - **Work:** Make source mode, secret-zero keys, required/optional values, explicit
    forwarding, runtime/migrator separation, and the plan's provider×topology matrix
    agree. Implement Environment and Infisical evidence rows for direct Standalone, Aspire
    Standalone, Aspire Split profiles, single-replica Compose, and supported
    multi-replica split deployment; Vault/Azure/AWS remain unsupported and fail
    closed.
    For supported multi-replica deployments, define one deployment-owned
    setup-secret authority and fail closed on divergent or unavailable authority.
    Verify promotion HMAC forwarding and avoid broad `env_file` injection.
  - **Acceptance:** Every supported provider×topology row has named executable
    ownership and complete code/test wiring; actual startup/topology execution is
    deferred to `SEC-405`; no source defines secrets.
  - **Dependency:** `SEC-305`.

- [ ] **SEC-402 — Align bootstrap/schema versions and idempotency** (`L`)
  - **Files:** `deploy/bootstrap/README.md`,
    bootstrap tooling/tests.
  - **Work:** Correct stale v1alpha1 references; prove rerun patches existing
    external resources additively and never overwrites deployment-managed secrets or
    persists provider admin credentials. Escalate any concrete manifest schema defect
    to the active configuration-manifest workstream instead of editing its schema here.
  - **Acceptance:** `SCN-OPS-001` passes through the supported non-destructive
    operator path.
  - **Dependency:** `SEC-401`.

- [ ] **SEC-403 — Converge and cross-validate operator documentation** (`L`)
  - **Files:** `docs/SECRETS.md`, `docs/CONFIGURATION.md`,
    `docs/SECURITY-MODEL.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`,
    backup/restore/upgrade and troubleshooting docs, README only where directly
    stale.
  - **Work:** Cross-validate the authority contract shipped in Phase 1, runtime
    failure contract shipped in Phase 3, and rotation/recovery contract shipped in
    Phase 4; remove remaining stale aliases/manifest/Aspire drift and close topology,
    backup, restore, and release navigation gaps. Do not defer first truthful behavior
    documentation to this PR.
  - **Acceptance:** Operator docs contain no contradictory authority/fallback/
    rotation claim and link exact current commands/contracts; earlier PR histories
    already contain their behavior-owning instructions.
  - **Dependency:** `SEC-402`.

- [ ] **SEC-404 — Implement rerun, rotation, backup, restore, and break-glass validation** (`L`)
  - **Files:** operator docs and existing deployment validation tests/tools.
  - **Work:** Implement coverage for partial bootstrap rerun, setup-secret divergence/concurrent
    initialization, cleanup, and forward-recovery convergence for each supported
    topology. Reuse Phase 4 executable rotation evidence; inspect documents only for
    command syntax, ownership, backup scope, restore/reprovision, and break-glass
    navigation. Defer runbook and convergence execution to `SEC-405`.
  - **Acceptance:** Evidence ownership labels each claim `executable` or
    `documentary`; `SCN-OPS-001` and all concurrency/rotation commands are ready for
    one final execution; runbooks state restart/maintenance or external-provider
    requirements.
  - **Dependency:** `SEC-403`.

- [ ] **SEC-405 — Run final intent-mandated verification and MAD review** (`L`)
  - **Work:** Run the single consolidated verification wave for the complete
    workstream: Release build; all applicable intent-minimum projects; every deferred
    Green scenario slice; generated migration inspection; five-provider matrix;
    Environment/Infisical × Standalone/Aspire/Compose/multi-replica execution;
    zero-secret scans; API/BFF/browser/accessibility/manual QA for changed surfaces;
    runbook recovery paths; and one anonymized Tier 1 epistemic MAD on source
    authority, leakage, tenancy, migration, recovery, and self-hosting.
  - **Acceptance:** Required suites are green or pre-existing failures are proven
    unrelated; all MAD blockers are resolved and evidence is recorded in context.
  - **Dependency:** `SEC-404`.

- [ ] **SEC-406 — Create Tier 2 change fragment and focused commits** (`M`)
  - **Files:** repository change-fragment location resolved from current governance,
    plan/context/tasks final state.
  - **Work:** Document removed inline storage/aliases, explicit source migration,
    operator action, reload/downtime, and recovery. Compose atomic commits by phase
    and implementation/test ownership; do not mix generated artifacts by hand.
  - **Acceptance:** Changelog and commits tell the breaking/security/operator story;
    working tree contains no forgotten implementation changes.
  - **Dependency:** `SEC-405`.

## Explicit Non-Tasks

- No file-mounted source implementation.
- No Vault, Azure Key Vault, AWS Secrets Manager, or other provider scaffolding.
- No HybridCache/Redis L2 or distributed invalidation until measured need proves it.
- No universal secret version/status aggregate.
- No persistent audit trail for reads or secret values.
- No exact retry/circuit constants without a measured provider requirement.
- No duplicate secret CRUD UI or configuration-manifest expansion.
- No Kubernetes/Helm implementation in this workstream.
