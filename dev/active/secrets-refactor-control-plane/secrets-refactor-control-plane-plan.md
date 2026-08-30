<!-- ABOUTME: Repository-grounded plan for deterministic, deployment-owned secret authority. -->
<!-- ABOUTME: Removes database ciphertext and separates bootstrap, runtime references, rotation, and portability. -->

# Secrets Authority And Control Plane Refactor — Implementation Plan

Last Updated: 2026-08-30 Europe/Brussels

## 0. Planning Metadata

- **Request:** Re-baseline the stale secrets refactor from current code and current
  official guidance; do not treat the previous plan as authoritative.
- **Task directory:** `dev/active/secrets-refactor-control-plane/`
- **Planning status:** GATE-001 I-VSD revalidation, GATE-002 technical approval, and
  GATE-003 user implementation approval are complete. Product edits remain blocked
  until the Phase 0 governance prerequisite (`GOV-001`, then `GOV-002`) completes.
  The user separately selected `Whole development databases`; Section 13 remains the
  exact destructive boundary and no execution-time target has yet been proven.
- **Change classification:** Behavioral Delta. Source authority, startup failure,
  runtime capability state, secret persistence, diagnostics, rotation, and
  operator recovery behavior all change.
- **Primary matched intent:** `external-infrastructure-bootstrap`, supplemented by
  the phase-specific intent matrix in Section 0.1. Phase 0 MUST extend the intent
  registry before product edits because current scopes do not cover
  `src/Explore.Secrets/**`, `src/Explore.Domain/Secrets/**`, or `.env.example`.
- **Criticality:** Tier 1 Security. Secret-zero handling, tenant isolation,
  provider failure, deployment authority, and diagnostic leakage are security
  boundaries.
- **Complexity:** XL, split into one governance prerequisite and six independently
  reviewable product phases/PRs.
- **Relevant skills:** `implementation-plan`, `senior-cto-feedback`, `i-vsd`,
  `grill-me`, `agentic-research`, `ip-clean-room`, `criticality-guardrail`,
  `clean-architecture-rules`, `dotnet-efcore-guidelines`, `auth-patterns`,
  `blazor-bff-patterns`, `blazor-ui-conventions`, `error-tracking`, and
  `epistemic-mad-review`.
- **Primary layers:** Domain secret definitions and metadata; Application
  resolution contracts and capability policy; Persistence metadata and generated
  migrations; `Explore.Secrets` provider adapters; API/HAL/BFF status surfaces;
  AppHost, Standalone, Compose, CI, schemas, and operator documentation.
- **Compatibility position:** Clean breaking replacement. No legacy source aliases,
  dual reads, database-ciphertext compatibility, deprecated routes, or migration
  shims.
- **Execution posture:** Code-first and strictly scoped. Use at most one subagent at
  a time, never dispatch parallel review/implementation swarms, and read the complete
  subagent result before any later delegation. Do not perform drive-by cleanup,
  unrelated baseline diagnosis, repeated reviews, broad test runs, app startup,
  browser/Aspire QA, provider-matrix execution, or phase-exit builds during active
  implementation. Defer those checks to the final verification wave in `SEC-405`
  and `FINAL`.
- **Non-deferrable exceptions:** `GOV-002` MUST validate the contribution contract
  once before product edits, and each Tier 1 Red task MUST run the smallest named
  invariant slice once to prove the current security defect before its production
  fix. These are authorization and failing-first safety gates, not general testing.
- **I-VSD:**
  [i-vsd-secrets-refactor-control-plane.md](../../../islamic-value-sensitive-design/i-vsd-secrets-refactor-control-plane.md)
- **I-VSD status/disposition:** `current` / `plan-aligned` for the exact plan/tasks
  revision recorded in the I-VSD report. This does not grant CTO technical readiness
  or user product implementation approval.
- **CTO review:** The fresh revision-bound re-review records `Approve`; GATE-002 is
  complete for the exact reviewed revisions.
- **User approval:** Full implementation of this no-backward-compatibility workstream
  is approved against pre-GATE-003 combined plan/tasks
  `sha256:a6255e78747ee7d85f42b27b213a5a0c3db1f250c0b24702856b4b6000445f37`.
  Separately, whole local-development databases and volumes may be recreated when
  required by the clean migration path. Production, shared, staging, CI evidence,
  external-provider/Infisical state, deployment secret stores, unnamed targets, and
  every ambiguous target remain excluded; immediate identity proof is still required
  before each destructive command.
- **Grill-Me decisions resolved from repository evidence:** Secret values remain
  deployment-owned; Infisical and environment modes are explicit and do not
  silently fall back; configuration manifests remain secret-free; bootstrap and
  runtime authority remain separate; purpose-specific rotation replaces a
  universal rotation state machine.

### 0.1 Contribution Contract And PR Intent Matrix

Phase 0 SHALL add or extend a secrets-authority intent so every planned product
path, required test project, documentation obligation, acceptance criterion, and
forbidden action is explicit before implementation. The new contract inherits Tier
1 security rigor and MUST NOT weaken existing intents.

| PR | Applicable intent(s) | Contract consequence |
|---|---|---|
| 0 — governance prerequisite | Agent-context/governance change | Add exact scopes for Domain secrets, `Explore.Secrets`, `.env.example`, and their tests/docs; validate the intent schema and twin rules before product edits. |
| 1 — authority/bootstrap | New secrets-authority intent; `external-infrastructure-bootstrap` where AppHost/Compose/Standalone change | Carry deterministic authority, secret-zero, deployment, docs, and full applicable minimum verification. |
| 2 — persistence removal | New secrets-authority intent; `add-ef-migration` or an explicitly approved greenfield baseline-reset contract | Generate artifacts only, update `schemas/islamu-event.md`, prove provider/tenant behavior, and never hand-edit or silently destroy data. |
| 3 — runtime authority/confidentiality | New secrets-authority intent | Land typed provider outcomes, consumer policy, bounded freshness, health, zero-secret diagnostics, and the matching operator failure contract without rotation activation. |
| 4 — rotation/recovery | New secrets-authority intent; `external-infrastructure-bootstrap` where deployment validation changes | Land consumer support classification, single-replica automation, deployment-owned multi-replica overlap/restart coordination, stale-replica behavior, recovery, and matching runbooks. |
| 5 — safe visibility | New secrets-authority intent; conditional `add-get-endpoint`, `add-hal-link`, `openapi-contract-change`, and `blazor-component-affordance` only if those surfaces actually change | Reuse existing surfaces first; apply API/HAL/generated-client/Blazor minimums only to created or changed contracts. The repository greenfield rule overrides compatibility-shim requirements. |
| 6 — deployment/operators | New secrets-authority intent; `external-infrastructure-bootstrap`; conditional `ci-cd-change` if CI/Coolify files change | Carry final topology convergence, rerun, release, cross-doc validation, and provider-safety obligations without deferring the first truthful operator contract. |

Conditional intents are resolved from the final changed-file set before each PR
starts. Missing scope is a blocker, not permission to edit outside the contract.

## 1. Executive Summary

The old plan proposed a database control plane containing reversible ciphertext,
generic versioned rotation, persistent read auditing, HybridCache, Polly recipes,
file/Vault-style sources, and a large new CRUD surface. Current code and repository
rules do not justify that design. More importantly, storing inline ciphertext in
`SecretBinding` conflicts with the repository invariant that secrets originate
only from Infisical or `.env`/explicit environment injection.

The replacement establishes four explicit responsibilities:

1. **Deployment/bootstrap authority** chooses one source mode and provides
   secret-zero inputs before normal dependency injection.
2. **Runtime reference resolution** uses tenant/instance-scoped, non-secret source
   metadata and resolves exactly one authoritative source.
3. **Purpose-specific rotation** coordinates provider and consumer behavior with
   validation and rollback appropriate to each credential type.
4. **Configuration portability** remains a separate, non-secret manifest concern.

Database rows may describe opaque source references and safe operational status;
they SHALL NOT contain secret values or reversible ciphertext. Required secrets
fail closed at startup or capability activation. Optional secrets disable only
their owning capability and expose truthful, non-sensitive status. Provider
responses, exception details, coordinates, and credentials never enter logs,
traces, health payloads, metrics, ProblemDetails, or support artifacts.

## 2. Source-Grounded Current State Report

### 2.0 Pre-Flight Structural Context (Blast Radius)

The fresh code graph at `HEAD 7ad222c285e5e0e8ff8e8c9f12bf7acb29fca6ba`
contains 62,446 nodes and 2,012,194 edges. The affected execution surfaces are:

- AppHost environment composition and Infisical augmentation;
- pre-DI PostgreSQL bootstrap;
- runtime `SecretResolver` and its three source adapters;
- `SecretBinding` domain/persistence ownership and tenant fallback;
- setup-secret and external-provider bootstrap flows;
- configuration-manifest export and BFF download boundaries;
- Standalone SQLite, split Compose, CI/Coolify, `.env.example`, and operator docs;
- Domain, Secrets, Persistence, Application, API, Blazor, and Architecture tests.

### 2.1 Evidence Log

| Evidence | Verified current fact |
|---|---|
| `src/Explore.AppHost/AppHost.cs` | Loads `.env`, augments configuration with Infisical, projects explicit child environment variables, and owns local-development parameter/default behavior. |
| `src/Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs` | Resolves PostgreSQL fields through Infisical, environment, then `IConfiguration`; writes provider diagnostics directly to stderr. |
| `src/Explore.Domain/Secrets/SecretBinding.cs` and `.Factory.cs` | Model instance/tenant source metadata and permit `InlineEncrypted` database ciphertext. |
| `src/Explore.Secrets/Services/SecretResolver.cs` | Chooses tenant then instance binding, dispatches one source, caches for five minutes, and often turns source failure into null. |
| `src/Explore.Secrets/Sources/*.cs` | Environment reads process environment; Infisical catches/logs provider exceptions; inline source unprotects database ciphertext. |
| `src/Explore.Application/Features/ConfigurationManifest/Handlers/Queries/ExportConfigurationManifestQueryHandler.cs` | Exports allowlisted non-secret settings/documents and marks sensitive/sovereign values omitted. It does not query secret bindings. |
| `.env.example` and `docker-compose.yml` | `.env` is the documented schema; Compose forwards explicit allowlists and separates runtime/migrator credentials. |
| `src/Event.Standalone` and `README.md` | Standalone SQLite is the minimum topology; Postgres and external services are not universal prerequisites. |
| `docs/SECRETS.md` and `docs/CONFIGURATION.md` | Still describe duplicated providers, appsettings/User Secrets/database authority, automatic refresh, and stale manifest/CLI guidance. |
| Existing tests | Cover resolver tenant fallback, bootstrap precedence, setup-secret lifecycle/header stripping, and selected redaction paths; they do not prove deterministic fail-closed authority or repository-wide zero-secret diagnostics. |

External functional guidance was consulted under the clean-room policy. Only
behavioral constraints are retained:

- [Microsoft ASP.NET Core configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/): providers are ordered and later values override earlier values; environment values are process-visible inputs, not a managed vault.
- [Microsoft Aspire external parameters](https://aspire.dev/fundamentals/external-parameters/): secret parameters protect deployment input handling but do not replace a lifecycle secret manager.
- [Infisical machine identities](https://infisical.com/docs/documentation/platform/identities/machine-identities): machine authentication has a secret-zero boundary, short-lived access, expiry, and revocation considerations.
- [Infisical secret rotation](https://infisical.com/docs/documentation/platform/secret-rotation/overview): rotation semantics depend on provider support; overlap is not universally available.
- [OWASP Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html): lifecycle ownership, least privilege, rotation, audit metadata, recovery, and high availability must be explicit.
- [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html): tokens, credentials, and secret values must not be recorded.
- [Docker build secrets](https://docs.docker.com/build/building/secrets/): sensitive build inputs use ephemeral secret mounts rather than `ARG`/`ENV` persistence.
- [Kubernetes Secrets](https://kubernetes.io/docs/concepts/configuration/secret/): base64 is not encryption and consumers may require restart/reload coordination. Kubernetes implementation remains out of scope.

### 2.2 Existing Implementation

Reusable foundations are `SecretDefinitionRegistry`, tenant/instance binding
lookup, environment and Infisical adapters, setup-secret fail-closed handling,
explicit Compose allowlists, configuration-manifest omission metadata, and BFF
header stripping. `SecretBinding` and `SecretResolver` are not discarded; they are
narrowed to non-secret references and explicit result semantics.

The incompatible implementation is `InlineEncrypted`: Data Protection ciphertext
and version metadata are persisted in the application database and later
unprotected into plaintext. The bootstrap loader also contains duplicated source
precedence and a legacy Infisical prefix. Infisical source failures and bootstrap
HTTP failures can expose provider detail and are frequently reduced to a clean
miss, obscuring degraded or compromised authority.

### 2.3 Existing Tests And Verification Coverage

Useful tests exist in:

- `tests/Explore.Secrets.UnitTests/Bootstrap/BootstrapSecretLoaderTests.cs`;
- `tests/Explore.Secrets.UnitTests/Services/SecretResolverBindingTests.cs`;
- `tests/Event.Domain.UnitTests/Entities/SecretBindingTests.cs`;
- setup-secret provider/API/BFF integration tests;
- current Doctor, authentication, database-context, and structured-audit
  redaction tests.

Missing invariant coverage includes hostile concurrent tenant access, explicit
source-mode failure without fallback, required-versus-optional capability state,
provider-response/exception redaction across every output channel, and
consumer-specific rotation rollback.

### 2.4 Existing Documentation And Contracts

`.env.example` is the environment schema. `docs/SECRETS.md`,
`docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/SECURITY-MODEL.md`, and
deployment bootstrap docs are operator contracts. Current inconsistencies include
database-held application secrets, duplicated Infisical prefixes, stale
configuration-manifest v1alpha1 references, stale `aspire run`, and a bootstrap
README that disagrees with the v1alpha2 schema in the repository.

### 2.5 Current Pain Points / Improvement Areas

- More than one source can appear authoritative during bootstrap.
- Inline database ciphertext violates the repository secret source-of-truth rule.
- Null conflates unconfigured, unavailable, unauthorized, and invalid states.
- Sensitive provider diagnostics can escape through stderr/logging.
- Five-minute local caching has no explicit freshness or rotation contract.
- Operator docs do not provide one coherent rerun, rotation, backup, and recovery
  procedure for all supported topologies.
- The old plan duplicates the configuration-manifest workstream and invents
  infrastructure not present in the current dependency graph.

### 2.6 Unknowns After Investigation (Strict Deferrable Open Questions Rule)

No scope- or architecture-changing questions are deferred. Current production
consumers are partitioned as follows:

- admission, recovery, promotion-digest, and ticketing-recovery keys are
  required/core when their owning security workflow is enabled;
- tenant registration providers, Stripe, Svix, SMTP, S3, analytics, localization,
  ATProto, Cerbos, Listmonk, webhooks, and managed control-plane registration are
  optional capabilities whose own operation fails or disables truthfully;
- all direct `ISecretResolver` consumers are operator-owned rotation boundaries;
  they re-resolve on the next supported operation/restart and receive explicit
  value-free validation and recovery instructions;
- automated candidate/rollback orchestration is limited to the already existing
  options-driven `RotationAwareHttpClientFactory` and
  `RotationAwareDbContextFactory`; current evidence shows no direct resolver
  consumer wired into those factories.

Per-provider overlap windows and exact restart timing remain implementation values,
but they cannot expand automation beyond those two proven boundaries without a new
plan/approval revision.

## 3. Proposed Future State: Behavioral Contract & Scenarios

### 3.1 Normative Requirements

- Secret values **MUST** originate from Infisical or explicit environment
  injection described by `.env.example`.
- Application databases, manifests, API contracts, browser state, AppHost source,
  appsettings, and User Secrets **MUST NOT** originate or persist secret values.
- Each deployment and secret class **MUST** select one source authority.
- Supported multi-replica deployments **MUST** use one deployment-owned setup-secret
  authority across replicas; replicas **MUST NOT** generate or accept divergent
  setup secrets, and inconsistency **MUST** fail closed with value-free recovery.
- Infisical-selected resolution failure **MUST NOT** fall back to environment or
  configuration values.
- Required bootstrap/core secrets **MUST** fail startup or activation closed.
- Optional capability secrets **MUST** affect only their owning capability and
  **MUST** expose a truthful non-sensitive state.
- Tenant authority **MUST** come from authenticated server context and repository
  isolation, never request headers/body or UI claims.
- Control-plane APIs, HAL, BFF, and UI **MUST NOT** expose values, reversible
  ciphertext, provider payloads, or sensitive coordinates.
- Configuration manifest import/export **MUST** remain secret-free.
- Rotation **MUST** preserve last-known-good service or fail closed, with an
  operator-visible rollback path and no universal provider-independent promise of
  zero downtime.
- Runtime rotation **MUST NOT** claim deployment-wide success from one replica's
  process-local activation. Multi-replica rotation SHALL be deployment-coordinated:
  use provider overlap while the old credential remains valid, or use a documented
  maintenance-window stop/restart when overlap is unavailable. Live automatic
  cross-replica orchestration is unsupported in this revision.
- During an overlap rollout, revocation of the old credential **MUST** wait for
  value-free evidence that every intended replica has activated and verified the
  candidate. Partial activation keeps the old credential valid and rolls the
  affected replica back or removes it from service; it is never reported as success.
- A stale replica **MUST NOT** serve dependent work after the declared overlap
  deadline. It SHALL be drained/restarted or fail the affected capability closed
  until it verifies the active credential generation.
- Direct resolver consumers **MUST** migrate to explicit required/core or optional
  capability handling; `null` **MUST NOT** remain a shared policy contract.
- Logs, stderr, traces, metrics, health payloads, ProblemDetails, and support
  artifacts **MUST NOT** contain credentials, tokens, values, provider response
  bodies, exception messages, paths, keys, or client identifiers.

### 3.2 Security And Operator Scenarios

**SCN-SEC-001 — Deterministic authority**

- GIVEN Infisical mode is selected and a lower-authority environment value exists
- WHEN Infisical is unavailable, unauthorized, or returns no value
- THEN resolution fails with a typed non-sensitive state and never reads the
  environment value.

**SCN-SEC-002 — Required secret fails closed**

- GIVEN a required database or authentication secret is absent or invalid
- WHEN its host or capability starts
- THEN startup/activation fails before accepting dependent work and diagnostics
  identify only the safe setting classification and remediation route.

**SCN-CAP-001 — Optional capability isolation**

- GIVEN an optional SMTP/analytics/AI credential is unconfigured
- WHEN unrelated platform capabilities start
- THEN only the owning capability is unavailable and status distinguishes
  unconfigured from provider failure without revealing coordinates.

**SCN-TEN-001 — Concurrent tenant isolation**

- GIVEN two tenants have bindings for the same definition
- WHEN hostile concurrent requests resolve and mutate status
- THEN each request can observe only its authenticated tenant or permitted instance
  fallback and no cache/result crosses tenant boundaries.

**SCN-OBS-001 — Zero-secret diagnostics**

- GIVEN provider responses/exceptions contain canary credentials and coordinates
- WHEN bootstrap, resolution, validation, health, API errors, and support diagnostics
  fail
- THEN no canary material appears in any observable output channel.

**SCN-ROT-001 — Rotation rollback**

- GIVEN a candidate credential has been created at the source
- WHEN dependent-resource validation or controlled reload fails
- THEN the last-known-good credential remains authoritative or the capability fails
  closed, the candidate is revoked/rolled back, and operators receive a value-free
  recovery receipt.

**SCN-ROT-002 — Multi-replica partial activation**

- GIVEN a deployment-coordinated overlap rollout and at least two replicas
- WHEN one replica validates the candidate and another rejects, times out, or does
  not acknowledge activation
- THEN deployment success is withheld, the old credential remains valid, the failed
  replica rolls back or leaves service, and no provider revocation occurs until all
  intended replicas produce value-free convergence evidence.

**SCN-ROT-003 — Restart and stale-replica boundary**

- GIVEN a consumer/provider pair has no safe overlap or a replica misses the overlap
  deadline
- WHEN rotation is attempted
- THEN the deployment uses a documented maintenance-window stop/restart, or drains
  and restarts the stale replica; dependent work remains closed until candidate
  validation succeeds, and unsupported live rotation is never presented as healthy.

**SCN-MAN-001 — Secret-free portability**

- GIVEN bindings and active credentials exist
- WHEN an instance or tenant configuration artifact is exported
- THEN the artifact contains no values, ciphertext, tokens, source credentials, or
  sensitive provider coordinates and reports that sensitive values were omitted.

**SCN-OPS-001 — Safe rerun and recovery**

- GIVEN bootstrap is rerun after partial external-resource creation or credential
  rotation, including a supported multi-replica deployment sharing setup authority
- WHEN existing resources are discovered or replicas concurrently initialize,
  clean up, or recover setup-secret state
- THEN operations are additive/idempotent, deployment-managed secrets are not
  silently overwritten, every replica observes one deployment-owned setup-secret
  authority, divergent or unavailable authority fails closed, obsolete setup
  material is cleaned up, and documented value-free forward recovery restores
  consistent service.

## 4. Non-Negotiable Constraints

- Preserve Clean Architecture dependency direction and repository-returned
  entities.
- Generate EF migrations; never hand-edit migration or model-snapshot files.
- Preserve Standalone SQLite as the minimum topology and split deployment support.
- Preserve BFF token/header boundaries and HAL as the UI affordance authority.
- No source-code-derived external implementation material or incompatible
  dependency additions.
- No backward compatibility for obsolete source types, aliases, columns, or docs.
- No persisted provider admin credentials or browser-supplied setup authority.

## 5. Architecture And Design Decisions

### AD-001 — Deployment Owns Values

Infisical or explicit environment injection owns secret values. Application state
may own definitions, opaque references, safe validation status, timestamps, and
audit metadata, but never values or reversible material.

### AD-002 — Explicit Source Mode, No Fallback

Bootstrap and runtime selection use an explicit source mode. The adapter for that
mode returns typed outcomes: resolved, unconfigured, unavailable, unauthorized,
or invalid. Callers apply required/optional policy; adapters do not silently
convert provider failure into absence.

### AD-003 — Keep `SecretBinding` Only As A Deep Metadata Module

`SecretBinding` earns its place by concentrating definition/scope/source-reference
invariants and tenant/instance override policy. `InlineEncrypted` and its
ciphertext/version fields disappear. If a remaining metadata field does not drive
resolution, authorization, safe status, or recovery, delete it.

### AD-004 — Purpose-Specific Rotation

Rotation belongs to the provider/consumer pair. Database credentials, OAuth client
secrets, signing/encryption key rings, SMTP credentials, and external admin
credentials have different overlap and reload capabilities. Shared orchestration
defines candidate, validate, activate/reload, verify, rollback, and revoke stages;
it does not persist a fictional universal `Pending/Active/Previous` value model.
Automation is confined to the existing options-driven HTTP-client and database
factories. Direct resolver consumers remain operator-owned unless a future,
separately approved plan proves an independent automated workflow is necessary.
Those factories coordinate only process-local replacement. In multi-replica
deployments they participate in an operator/deployment-owned overlap rollout or
maintenance restart; they do not provide a distributed commit protocol. Each
consumer family MUST be classified before implementation as `overlap-rollout`,
`coordinated-restart`, or `unsupported-live`, with partial activation, stale-replica,
rollback, revocation, and recovery evidence owned by Phase 4.

### AD-005 — Separate Non-Secret Manifest

`ConfigurationManifest` remains closed, allowlisted, and secret-free. This
workstream adds omission/boundary tests only; broad import/export/UI ownership stays
with `dev/active/configuration-manifest/`.

### AD-006 — Minimal Infrastructure

Continue with existing SDKs and memory caching only where bounded freshness is
explicitly safe. Do not add file sources, Vault/cloud-provider scaffolding,
HybridCache/Redis L2, generic read-audit storage, or fixed Polly recipes without a
measured requirement and separate approval.

## 6. Implementation Phases

Phases 1–6 are code-delivery checkpoints, not test/review cycles. Unless a task is a
Tier 1 Red invariant task, implementation work SHALL author the required production
code, test code, CI wiring, and operator contracts without executing builds, Green
test suites, provider matrices, app/browser/Aspire QA, MAD review, or repeated
independent review. Every phase exit below describes the behavior that the final
`SEC-405`/`FINAL` wave must prove, not a command that is run at that phase boundary.
Unrelated failures or cleanup discovered along the way are recorded and left alone
unless they directly block the next planned code edit.

### Phase 0 — Contribution Contract Prerequisite

Extend `.agents/contract/intents.yaml` and any required twin path rules with a
Tier 1 secrets-authority intent covering verified paths, test projects, docs,
acceptance criteria, and forbidden actions. Record conditional secondary-intent
routing and validate the contract before touching product code.

**Exit:** every planned path is authorized by an exact intent and the focused
`GOV-002` contract validator passes once; no product file has changed. Product builds
and architecture suites are deferred to final verification.

### Phase 1 — Authority Contract And Fail-Closed Bootstrap

Define typed resolution/capability outcomes and required/optional classifications.
Add Red invariant-breaker coverage for unauthorized fallback and zero-secret
bootstrap diagnostics. Replace legacy/double Infisical configuration paths with one
explicit source mode in AppHost, BootstrapSecretLoader, Standalone, and Compose.
Ship the source-mode, no-fallback, secret-zero, required/optional bootstrap, and
immediate recovery operator contract in `docs/SECRETS.md`, `docs/CONFIGURATION.md`,
`docs/SELF_HOSTING.md`, and `docs/TROUBLESHOOTING.md` in this same PR.

**Exit:** `SCN-SEC-001` plus the bootstrap portions of `SCN-SEC-002` and
`SCN-OBS-001` pass; no appsettings/User Secrets/runtime fallback remains an
origin for secret values; executable source-mode checks and operator docs agree.
Complete required-consumer activation remains owned by Phase 3.

### Phase 2 — Remove Database Secret Values

Remove `InlineEncrypted`, Data Protection Protect/Unprotect flows, ciphertext and
version fields, commands/contracts/DI/tests that support inline values, and reduce
bindings to opaque non-secret metadata. This pre-release workstream chooses a clean
local-development database/volume reset and generated migration-baseline
regeneration rather than a compatibility migration. The user's authorization covers
whole LOCAL DEVELOPMENT databases and volumes only when needed for this clean path;
target identities MUST still be confirmed immediately before execution and the
exclusions in Section 13 are absolute. Update `schemas/islamu-event.md` and add
concurrent tenant-isolation/provider constraints.

**Exit:** `SCN-TEN-001` passes across supported database providers; no application
table or DTO can persist/return a secret value or reversible ciphertext.

### Phase 3 — Runtime Authority And Confidentiality

Make Infisical/provider outcomes typed and value-free, remove exception/response
logging, define bounded process-local cache freshness and invalidation, migrate every
direct resolver consumer to explicit required/core or optional capability behavior,
and expose value-free health. Ship the matching runtime failure, cache freshness,
capability-state, and immediate recovery documentation in this PR. Do not activate
or automate credential rotation in this slice.

**Exit:** complete `SCN-SEC-002`, `SCN-OBS-001`, and `SCN-CAP-001` pass; no source
failure is silently reported as unconfigured; every direct consumer has an explicit
policy owner; runtime docs and executable checks agree.

### Phase 4 — Consumer Activation, Rotation, And Recovery

Classify each automated category and direct-consumer family as
`overlap-rollout`, `coordinated-restart`, or `unsupported-live`. Harden only the
existing options-driven HTTP-client and database factories for process-local
candidate/validate/activate/verify/rollback behavior. Multi-replica coordination
remains deployment/operator owned and MUST implement the normative overlap,
partial-activation, restart, stale-replica, revocation, and fail-closed contracts in
`SCN-ROT-001`–`SCN-ROT-003`. Ship provider/consumer-specific rotation, reload,
rollback, recovery, and break-glass runbooks and executable validation in this PR.

**Exit:** `SCN-ROT-001`–`SCN-ROT-003` pass for every claimed mode; unsupported live
rotation is explicitly rejected; no old credential is revoked before deployment-wide
convergence evidence; operator docs identify owner, overlap/restart mode, and exact
recovery command path.

### Phase 5 — Safe Control-Plane Visibility

Expose server-authorized status and safe reference metadata through the smallest
existing settings/control-plane surface. Use API/HAL/BFF authority; add Blazor
affordances only when a supported operator action exists. Prove existing manifest
exports and any changed API/OpenAPI/generated-client/UI or support outputs omit
values and sensitive coordinates. This workstream changes the manifest handler or
schema only if a concrete defect is first recorded and coordinated with
`dev/active/configuration-manifest/`. Do not create a duplicate generic secret CRUD
product.

**Exit:** `SCN-MAN-001` passes and a hostile browser cannot supply authority or
recover sensitive material from any contract.

### Phase 6 — Deployment And Operator Convergence

Align AppHost, Compose, Standalone, `.env.example`, CI/Coolify, bootstrap schemas,
and operator docs. Fix v1alpha1/v1alpha2 and promotion-HMAC forwarding drift.
Validate the already-shipped authority/runtime/rotation docs as one coherent
install, rerun, backup, restore, setup-secret authority/cleanup, and forward-recovery
contract for each supported topology. This PR may correct cross-document drift but
MUST NOT be the first delivery of behavior-owning operator instructions. Complete
each PR's applicable intent test matrix, MAD review, Tier 2 change
fragment, and final atomic commits.

**Exit:** `SCN-OPS-001` passes through operator-level validation; all supported
topologies have one source authority and a tested recovery path, and supported
multi-replica deployments prove consistent setup-secret authority or fail closed.

### 6.1 Provider × Topology Evidence Matrix

Only `None`/explicit environment injection and `Infisical` are supported secret
authorities. `Vault`, `AzureKeyVault`, and `AwsSecretsManager` are declared but
unimplemented and MUST fail closed; they have no supported topology row. Every row
below requires executable evidence; documentary inspection supplements but never
replaces startup, divergence, partial-activation, or recovery assertions.

| Runtime topology | Environment authority | Infisical authority | Executable owner | Documentary owner |
|---|---|---|---|---|
| Direct `Event.Standalone`, one process/container, SQLite default | Supported | Supported | Extend `StandaloneProviderCompositionTests` plus `EnvironmentSecretProviderTests` / `InfisicalSecretProviderTests` to compose the selected mode and reject fallback | `.env.example`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md` |
| Aspire `Hosting:Topology=Standalone` | Supported | Supported in explicitly selected maintainer mode | Add public composition assertions to the AppHost topology contract and run provider tests; do not use source-text scraping as product evidence | `docs/OPERATIONS.md`, `docs/CONFIGURATION.md` |
| Aspire `Hosting:Topology=Split` (`DefaultLocal`, `FullLocal`, `LocalDataExternalPlatform`, `ExternalInfra`) | Supported | Supported where profile inputs select it; `FullLocal` remains explicit environment mode | Add profile/source-mode composition cases and provider tests proving one selected authority and value-free failure | `launchSettings.json`, `docs/OPERATIONS.md`, `.env.example` |
| Split Docker Compose, one API replica | Supported | Supported | Extend `DockerComposeTopologyDoctorCheckTests`, bootstrap/resolver tests, and `docker compose config` validation for explicit allowlists and no fallback | `docker-compose.yml`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md` |
| Split deployment, two or more API replicas | Supported only when deployment injects one consistent authority | Supported only when replicas share the same project/environment/path authority | New `SecretRotationReplicaConvergenceTests`: execute `SCN-ROT-002` and `SCN-ROT-003` for overlap, partial activation, stale replica, restart, and fail-closed recovery; setup-secret divergence remains executable under `SCN-OPS-001` | rotation/recovery runbook shipped in Phase 4; final cross-doc validation in Phase 6 |

The primary database clean-baseline matrix is independently closed and executable:

| Provider | Required executable evidence |
|---|---|
| PostgreSQL | CI `database-provider-matrix`: clean/idempotent MigrationService run, `PrimaryDatabaseRuntimeSmokeTests`, `PrimaryDatabaseProviderBehaviorContractTests`, and the new SecretBinding tenant/constraint contract |
| SQLite | Same CI lane with isolated file creation plus the same smoke, behavior, and SecretBinding contract |
| SQL Server | Same CI lane against the pinned SQL Server service plus the same smoke, behavior, and SecretBinding contract |
| MariaDB | Same CI lane against the pinned MariaDB service plus the same smoke, behavior, and SecretBinding contract |
| MySQL | Same CI lane against the pinned MySQL service plus the same smoke, behavior, and SecretBinding contract |

Phase 2 MUST extend the existing five-engine `.github/workflows/_build-test.yml`
`database-provider-matrix`; “supported providers” without one artifact per row is not
completion evidence. Phase 4 MUST execute replica behavior for every live claim;
runbook review alone cannot prove convergence.

Matrix rows are exercised only in the final verification wave by exact class slices
(after the relevant project build):

- provider authority: `dotnet run --project tests/Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --no-build -- --treenode-filter "/*/*/*SecretProviderFactoryTests/*"`, then the equivalent `EnvironmentSecretProviderTests` and `InfisicalSecretProviderTests` slices;
- direct Standalone: `dotnet run --project tests/Event.Standalone.IntegrationTests/Event.Standalone.IntegrationTests.csproj --no-build -- --treenode-filter "/*/*/*StandaloneProviderCompositionTests/*"`;
- Aspire source/topology composition: new public-seam `SecretAuthorityAppHostCompositionTests` slice in `Event.Architecture.Tests` (raw AppHost source scraping is forbidden);
- Compose: `dotnet run --project tests/Explore.Diagnostic.UnitTests/Explore.Diagnostic.UnitTests.csproj --no-build -- --treenode-filter "/*/*/*DockerComposeTopologyDoctorCheckTests/*"` plus `docker compose config --quiet`;
- multi-replica runtime: new `SecretRotationReplicaConvergenceTests` slice in `Explore.Secrets.UnitTests`;
- database providers: the existing CI `database-provider-matrix` command at `.github/workflows/_build-test.yml`, extended with a new SecretBinding contract slice and one uploaded artifact for each of the five rows.

### Behavioral Slice Rule: Invariant-First Slicing (in `tasks.md`)

Red tasks precede production changes only for source authority, tenant isolation,
diagnostic confidentiality, rotation rollback, and secret-free export boundaries.
Run only the smallest named Red slice once to demonstrate the current defect.
Ordinary handlers/UI/docs use direct implementation and author their contract tests,
but all Green execution is deferred to `SEC-405`/`FINAL`.

### Atomic Task Verification Rule (in `tasks.md`)

Each task names exact owning files, dependencies, acceptance evidence, and the final
verification owner. During implementation, do not run builds, Green tests, broad
suites, app startup, browser/Aspire QA, provider matrices, or MAD review after tasks
or phases. Read/compile diagnostics may be used only when required to keep editing
the next planned code task; they are not completion gates.

### Final Phase Closing Rule: Changelog & Commit as the Final Task (in `tasks.md`)

After implementation and review are green, create the required Tier 2 change
  fragment and compose focused commits following repository history. Do not mix the
  governance prerequisite or six product phases into one commit.

## 7. Testing Strategy

- Domain tests protect source/scope/reference invariants and removal of inline
  values.
- Secrets tests protect deterministic authority, typed outcomes, zero-secret
  diagnostics, cache boundaries, and rotation rollback.
- Persistence tests use supported providers for constraints, generated migrations,
  and hostile concurrent tenant isolation.
- Application/API/BFF tests protect required/optional behavior, server authority,
  HAL, and value-free contracts.
- Configuration-manifest tests protect closed omission boundaries.
- Architecture tests prevent forbidden dependencies and secret-bearing contracts.
- Tier 1 MAD review independently challenges fallback, diagnostics, tenancy,
  migration, rollback, and operator recovery during the final verification wave.

### 7.1 Code-First Execution Policy

During active implementation, execute only:

1. the focused `GOV-002` contribution-contract validator once before product edits;
2. the smallest named Red invariant slice once for `SEC-001`, `SEC-101`, `SEC-201`,
   `SEC-221`, and `SEC-301`, before their dependent production change.

Everything else is deferred to `SEC-405` and `FINAL`: Release builds, Green test
slices, full/minimum projects, migration/provider matrices, Docker/Aspire/Standalone
startup, HTTP/browser/manual QA, accessibility checks, canary scans, runbook
execution, MAD review, and repeated independent reviews. Test and CI code is still
implemented alongside its owning behavior so the final wave can run once against
the complete system. A directly blocking compiler/syntax error may be diagnosed
only enough to continue the planned code path; unrelated failures are logged without
investigation.

### 7.2 Final Verification Matrix

| Implemented slice | Verification deferred to `SEC-405` / `FINAL` |
|---|---|
| 0 | Contract/schema, route, twin, link, whitespace, and architecture ratchet. Only the focused contract validator runs before implementation. |
| 1 | Bootstrap/resolver Green suites, Release build, source-mode topology checks, and operator-link/command validation. |
| 2 | Domain/Secrets/Persistence/Architecture suites, generated-artifact inspection, and the five-provider clean/idempotent baseline matrix. |
| 3 | Runtime consumer suites, zero-secret canary scan, health/failure behavior, and operator-document validation. |
| 4 | `SCN-ROT-001`–`SCN-ROT-003`, replica convergence, provider/consumer matrix, runbook execution, and rotation MAD. |
| 5 | API/HAL/OpenAPI/generated-client/BFF/Blazor, accessibility, and hostile-browser behavior for changed surfaces only. |
| 6 | Standalone/Aspire/Compose/multi-replica operator paths, rerun/backup/restore/break-glass, all intent minimums, final Release build, and final Tier 1 MAD. |

## 8. Documentation, Configuration, And Operations Impact

Implementation updates include `.env.example`, `docs/SECRETS.md`,
`docs/CONFIGURATION.md`, `docs/SECURITY-MODEL.md`, `docs/SELF_HOSTING.md`,
`docs/OPERATIONS.md`, backup/recovery/troubleshooting docs, deployment bootstrap
README/schema references, AppHost composition, Compose allowlists, Standalone
guidance, and CI/Coolify secret inventory documentation.

Authority-changing documentation ships with its owning PR: Phase 1 owns source
mode/no-fallback/bootstrap recovery; Phase 3 owns typed runtime outcomes, cache
freshness, health, and capability recovery; Phase 4 owns overlap/restart,
partial-activation, stale-replica, rollback/revoke, and break-glass rotation. Phase 6
only converges and validates those already truthful contracts.

Operator docs must distinguish secret-zero inputs, required/core values, optional
capability values, externally managed rotation, reload/restart behavior, backup
scope, break-glass authority, and value-free diagnostics.

### 8.1 Release & Changelog Strategy (Procedural Contribution)

This is a Tier 2 security/operator breaking change. The final phase creates a
change fragment that names removed inline storage and legacy aliases, explicit
source-mode migration, required operator action, downtime/reload expectations,
and rollback/recovery. Generated baseline artifacts and docs ship in the same release as
the breaking runtime change; no compatibility window is planned.

## 9. Islamic Value-Sensitive Design (I-VSD) & Moral Boundaries

| Finding | Required mitigation | Scenario | Task ownership |
|---|---|---|---|
| `IVSD-F001` ambiguous authority can conceal unsafe fallback | `IVSD-M001` explicit source and fail-closed outcomes | `SCN-SEC-001` | `SEC-001`–`SEC-003` |
| `IVSD-F002` diagnostics can expose entrusted secrets | `IVSD-M002` zero-secret output boundary | `SCN-OBS-001` | `SEC-004`, `SEC-201`, `SEC-202`, `SEC-205`, `SEC-207` |
| `IVSD-F003` weak recovery burdens self-hosters | `IVSD-M003` topology-specific rerun/rotation/recovery | `SCN-OPS-001`, `SCN-ROT-001`–`SCN-ROT-003` | `SEC-221`–`SEC-226`, `SEC-401`, `SEC-402`, `SEC-404` |
| `IVSD-F004` tenant crossover violates entrusted authority | `IVSD-M004` server-derived tenant isolation and races | `SCN-TEN-001` | `SEC-101`, `SEC-104`, `SEC-105` |
| `IVSD-F005` silent degradation misrepresents capability state | `IVSD-M005` required/optional typed status | `SCN-SEC-002`, `SCN-CAP-001` | `SEC-002`, `SEC-202`, `SEC-203`, `SEC-205`, `SEC-207` |
| `IVSD-F006` secret-bearing portability transfers entrusted material | `IVSD-M006` preserve a closed, value-free portability boundary | `SCN-MAN-001` | `SEC-301`–`SEC-305` |
| `IVSD-F007` destructive migration authority can exceed consent | `IVSD-M007` constrain disposal to proven local-development targets and fail on ambiguity | Section 13 authority boundary; no product scenario | `GATE-003`, `SEC-104`–`SEC-106` |

These mappings are confirmed by the current revision-bound planning-mode I-VSD
report. The Section 13 destructive authority is deliberately a user-owned execution
gate rather than a product behavior scenario. I-VSD alignment does not grant CTO
technical readiness, user product implementation approval, or religious-legal
approval.

## 10. Security, Authorization, Privacy, And Abuse Considerations

Secrets, credentials, provider coordinates, and exception payloads are sensitive
even when not personal data. API authority remains server-side; protected admin
GET endpoints are explicit exceptions to the generic anonymous-GET convention.
Tenant filters/repositories remain active; no runtime `IgnoreQueryFilters()` escape
is planned. BFF strips browser-supplied privileged headers. Provider admin
credentials remain request/job scoped and never become runtime secret records.

## 11. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

Tenant overrides are permitted only where `SecretDefinitionRegistry` allows them.
Instance locks and authenticated target context govern inheritance. No federation
or locale-specific secret semantics are introduced. Any operator UI uses HAL
affordances, value-free accessible labels/status, keyboard/focus conventions, and
RTL-safe styling; a UI is not required for actions that remain deployment-owned.

## 12. Observability And Operations

Emit low-cardinality state/reason codes and timing without exception messages,
values, coordinates, binding IDs that become sensitive, or provider bodies.
Health differentiates unconfigured, degraded, and failed-closed capabilities but
never provides discovery material. Audit only security-relevant mutations and
rotation/recovery receipts, not secret reads or values. Alerting must direct
operators to runbooks, not reproduce provider diagnostics.

## 13. Migration And Compatibility Plan

The user has authorized disposal and recreation of whole **LOCAL DEVELOPMENT**
databases and volumes when needed for the clean migration path. This includes all
data and database-resident Data Protection material inside the specifically named
local targets; it is broader than deleting only `SecretBinding` rows. It does not
authorize destruction of production, shared, staging, CI evidence, external
provider resources, Infisical state, deployment secret stores, or any unnamed
database/volume. Immediately before any destructive command, the implementation
agent MUST print and record each target's environment, provider, database/container
identity, and volume/path, prove it is local and non-shared, and stop on ambiguity.

After GATE-002 passes and GATE-003 records separate product implementation approval,
delete inline source enums/factories/contracts, update the EF model, reset only those
confirmed local-development targets, and regenerate provider migration baselines
through repository tooling. Do not ship a destructive compatibility migration or a
`Down` path pretending deleted ciphertext can be restored. Update
`schemas/islamu-event.md`, execute every provider row in Section 6.1, and require
operators to re-provision values in the selected deployment source. Rollback is
forward-fix from external source/configuration plus a regenerated/corrected local
development baseline; generated artifacts are never hand-edited.

## 14. Risk Register

| Risk | Severity | Mitigation |
|---|---|---|
| Provider failure silently falls back and appears healthy | Blocker | Typed outcomes plus `SCN-SEC-001` Red test |
| Provider exception/response leaks credentials | Blocker | Central zero-secret diagnostics contract plus canary scans |
| Tenant cache/reference crosses scope | Blocker | Scope-qualified keys, repository isolation, hostile concurrency tests |
| Rotation revokes old credential before every replica converges | Critical | `SCN-ROT-002`, deployment-wide value-free acknowledgements, old-credential overlap, and delayed revocation |
| Stale replica serves after overlap deadline | Critical | `SCN-ROT-003`, drain/restart or capability fail-closed behavior |
| Local reset command reaches shared/staging/CI/production/external/deployment state | Blocker | Exact pre-execution target identity and environment proof; authorization is local-development-only and ambiguity stops execution |
| Removing inline values strands a local development deployment | Major | Authorized local database/volume recreation plus reprovision runbook; no compatibility migration |
| AppHost/Compose/docs disagree on authority | Major | Phase 6 provider×topology matrix and contract validation |
| Control-plane UI duplicates configuration ownership | Major | Reuse existing settings/HAL surface or omit UI |

## 15. Success Metrics And Definition Of Done

- No database, manifest, API, UI, source file, appsettings, or User Secrets path
  can originate or persist secret values.
- Every secret class has one documented source authority and required/optional
  policy.
- All ten named scenarios pass with value-free evidence.
- PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL each produce clean/idempotent
  baseline plus SecretBinding tenant/constraint artifacts; every supported runtime
  topology executes its Environment and/or Infisical authority row.
- Logs, traces, stderr, health, metrics, ProblemDetails, and support artifacts pass
  canary secret scans.
- Standalone, Compose, and Aspire operator paths document install, rerun, rotation,
  backup, restore, and forward recovery.
- Configuration manifests remain secret-free and the secrets workstream does not
  duplicate configuration-manifest ownership.
- I-VSD is fresh/plan-aligned, revision-bound CTO review approves, and user approval
  is recorded before implementation begins.

## 16. Implementation Agent Contract — KEEP DEV DOCS CURRENT

The implementation agent SHALL:

- read this plan and the context file before selecting the next task;
- update only `tasks.md` for granular execution status;
- update `context.md` immediately for decisions, blockers, baseline failures, and
  dated handoffs;
- update this plan only when behavior, architecture, phase scope, or risk changes;
- verify paths/symbols immediately before editing because the worktree is shared;
- regenerate migration baselines through EF tooling after explicit disposal
  approval, local target-identity proof, and environment exclusions, then inspect
  them without hand edits;
- keep exactly one task in progress and check it off when its acceptance evidence
  exists;
- use no more than one subagent at a time, consume its complete output, and never
  launch parallel implementation/review agents;
- refuse irrelevant cleanup, speculative additions, unrelated baseline diagnosis,
  and repeated review loops; record non-blocking discoveries for later instead;
- defer all product Green tests, builds, app/browser/Aspire/manual QA, provider
  matrices, and MAD review to `SEC-405`/`FINAL`, except the focused `GOV-002`
  authorization validator and mandatory failing-first Red slices;
- stop and refresh I-VSD/CTO/user approval if a material provider/deployment
  responsibility changes;
- never reinterpret a source failure as absence merely to preserve availability;
- never add a dependency or external implementation pattern without clean-room and
  licensing review.

## 17. Progress Reporting Contract

At phase start, record the selected code task in `context.md`. At task completion,
record the changed paths and deferred final-verification owner, then immediately
update `tasks.md`. Do not run or report phase-exit test cycles. `SEC-405` records the
single consolidated Release build, every applicable intent-minimum project,
generated/provider/topology evidence, real-surface QA, and MAD review. A handoff
states current phase/task, changed paths, next code action, blockers, and deferred
verification without duplicating this plan.

## 18. Potential Risks & Unknowns

Runtime bindings carry provider coordinates that are secret-equivalent outside
trusted server persistence/adapters; keep them server-only. Exact overlap, reload,
and restart timing remains purpose-specific and must be documented per consumer,
but direct resolver consumers remain operator-owned under this revision.
