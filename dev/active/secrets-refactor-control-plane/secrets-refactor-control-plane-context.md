<!-- ABOUTME: Active handoff for the repository-grounded secrets authority refactor. -->
<!-- ABOUTME: Records verified reality, decisions, blockers, and the next planning gate. -->

# Secrets Authority And Control Plane Refactor — Context

Last Updated: 2026-08-30 Europe/Brussels

## Current State

- **Stage:** Product implementation authorized; Phase 0 governance is complete.
- **Current phase:** Phase 6 deployment and operator convergence.
- **Current task:** `SEC-405` — consolidated verification and Tier 1 MAD review.
- **User approval:** Full no-backward-compatibility implementation is approved against
  combined plan/tasks
  `sha256:a6255e78747ee7d85f42b27b213a5a0c3db1f250c0b24702856b4b6000445f37`.
  The user separately selected `Whole development databases`, authorizing whole
  LOCAL DEVELOPMENT database/volume disposal and recreation when needed for the
  clean migration path, subject to exact pre-execution target proof.
- **I-VSD:** `current` / `plan-aligned`; GATE-001 is complete for the revised
  plan/tasks mapping, including replica rotation, portability, and destructive
  local-development authority.
- **Current revalidated revision:** plan
  `sha256:fed15e71ffeb739aa2dd2e62ef06317fcdde060420e9a7c4c9093105e295f6c9`;
  tasks `sha256:10a49e6dcfd55e39234dba068eed6a22634f1ed06f3419f19861d7b425772f84`;
  combined plan-then-tasks
  `sha256:825b965e4937a7544ec99ea93c20456f2737c43441d45d44ac00549586805757`.
- **CTO:** Fresh revision-bound verdict `Approve`; GATE-002 is complete for the
  exact current revalidated revision. This does not grant GATE-003.
- **Working-tree baseline:** The one-time pre-product Release build passed 45
  projects with 0 errors and 343 pre-existing warnings. Product changes for Phases
  1–6 are present and Green execution remains consolidated under `SEC-405`.
- **Product verification:** Five required Red slices ran once and failed for their
  intended pre-change invariants. All Green build/test/provider/topology/browser/
  runbook/MAD execution is now ready for the single `SEC-405` wave.
- **User execution direction:** Use no more than one subagent at a time and read its
  complete output before any later delegation. Focus only on planned code and
  directly owned tests/docs. Do not investigate unrelated failures or run phase-exit
  builds, Green suites, provider matrices, app/browser/Aspire QA, MAD, or repeated
  reviews; defer them to `SEC-405`/`FINAL`.
- **Mandatory exceptions:** `GOV-002` contract authorization must pass once before
  product edits, and the five Tier 1 Red tasks must run their smallest failing slice
  once before dependent production code. Removing those would violate repository
  security rules.

## Resume Here

1. Execute `SEC-405` exactly once: generated artifacts, Release build, scoped tests,
   provider/topology evidence, browser/accessibility/manual QA, runbook checks, and
   one anonymized Tier 1 MAD.
2. Preserve the PR 0–6 intent mapping below and reclassify conditional intents from
   the final changed-file set before each phase closes.
3. Preserve the local reset boundary. Exact environment/provider/database/container/
   volume identities MUST be proven immediately before each destructive command;
   no target has yet been proven or reset.

## Phase 1–6 Implementation Evidence

- Authority is explicit `Environment` or `Infisical`; isolated bootstrap has no
  appsettings/User Secrets/lower-source fallback and diagnostics use bounded codes.
- `SecretBinding` is metadata-only. Inline ciphertext/source/protect/unprotect paths,
  the unused AES/database configuration provider, value mutation endpoints, and
  compatibility scaffolding are deleted. Five clean provider migrations were
  generated for PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL.
- `ISecretResolver` returns typed bounded outcomes, caches resolved values only for
  five minutes with tenant/scope/qualifier isolation, and emits low-cardinality
  source/status metrics. Required consumers fail closed; optional capabilities
  disable truthfully.
- Rotation uses local value-free acknowledgements plus deployment-owned replica
  convergence. HTTP/database candidates validate before activation; overlap is
  limited to key rings/HMACs and other credentials use coordinated restart.
- The existing authenticated control-plane overview consumes only
  `Provider`/`Status`/`RemediationCode`. No new action, secret input, HAL relation,
  BFF endpoint, CSS, or generated client hand edit was introduced.
- Compose/AppHost/Standalone schemas align canonical forwarding, explicit provider,
  replica count, setup authority, and promotion HMAC. Source files and `.env.example`
  no longer define local credential defaults for the touched services.
- SMTP, S3, analytics administration, and Cerbos Admin credentials are absent from
  hierarchical setting definitions/seeds/groups/write DTOs/UI and resolve only from
  the selected authority. Non-secret endpoint/bucket/sender policy remains governed.

## Phase 0 Validation Evidence

- **Focused command:** `dotnet run eng/agent-context/validate-contract.cs -- .
  --intent secrets-authority`
- **Result:** Exit `0` on 2026-08-30; 22 unique intents, 13 benchmark scenarios,
  contract schema/references, governance ownership, secondary reachability,
  expected route sets, and conflict precedence all passed.
- **PR 0:** agent-context/governance change.
- **PR 1:** `secrets-authority`; add `external-infrastructure-bootstrap` when
  AppHost, Compose, or Standalone files change.
- **PR 2:** `secrets-authority` plus `add-ef-migration` for generated persistence
  artifacts and the provider matrix.
- **PR 3:** `secrets-authority`.
- **PR 4:** `secrets-authority`; add `external-infrastructure-bootstrap` when
  deployment validation changes.
- **PR 5:** `secrets-authority`; conditionally add `add-get-endpoint`,
  `add-hal-link`, `openapi-contract-change`, and `blazor-component-affordance` from
  the actual surface changed.
- **PR 6:** `secrets-authority` plus `external-infrastructure-bootstrap`;
  conditionally add `ci-cd-change` when CI/Coolify files change.

## Execution Focus Override — 2026-08-30

The user stopped the prior orchestration after reading the accumulated subagent
record and directed a code-first rewrite. The record showed repeated governance
review loops and unrelated baseline analysis before product work. This workstream is
therefore paused with these binding rules:

- one active/dispatched subagent maximum; no parallel swarms;
- consume the full current subagent result before another delegation;
- no drive-by cleanup, speculative additions, or unrelated failure diagnosis;
- only `GOV-002` and mandatory failing-first Red slices execute before final QA;
- all Green tests, builds, provider/topology matrices, app/browser/Aspire/manual QA,
  accessibility checks, runbook execution, MAD, and broad independent review run once
  at `SEC-405`/`FINAL`;
- phase-closing tasks are implementation reconciliation checkpoints, not test cycles.

This changes execution cadence only. It does not alter secret authority, tenant or
diagnostic invariants, migration consent, provider support, product scope, I-VSD
findings, or the approved architecture.

## Verified Repository Reality

### Four Existing Responsibilities

1. **AppHost bootstrap:** `src/Explore.AppHost/AppHost.cs` loads `.env`, augments
   configuration with Infisical, creates local Aspire secret parameters, and
   projects explicit child environment values.
2. **Pre-DI database bootstrap:**
   `src/Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs` resolves each Postgres
   field through Infisical, environment, then `IConfiguration`.
3. **Runtime resolution:** `src/Explore.Secrets/Services/SecretResolver.cs` resolves
   tenant then instance bindings, dispatches one source, and caches in memory.
4. **Configuration manifest:** startup/import/export handles allowlisted non-secret
   configuration and is owned by `dev/active/configuration-manifest/`.

### Confirmed Security Gaps

- `SecretBinding` permits `InlineEncrypted` and stores reversible Data Protection
  ciphertext in the database.
- `InlineSecretSource` unprotects that ciphertext into plaintext at runtime.
- bootstrap accepts duplicated/legacy Infisical configuration and falls back to
  lower-authority sources.
- bootstrap stderr and Infisical source logs can include provider body/exception,
  path, key, binding, or client-identifying detail.
- provider failure is often converted into null, conflating failure with an
  unconfigured optional capability.
- local five-minute caching has no explicit rotation/freshness contract.
- no complete SecretBinding CRUD/API/UI implementation was found; old plan claims
  that assume it exists are invalid.

### Reusable Foundations

- `SecretDefinitionRegistry` and source/scope validation;
- tenant then instance resolution shape;
- setup-secret generation, lifecycle, fail-closed lock, and BFF header stripping;
- explicit Compose environment allowlists and runtime/migrator separation;
- configuration-manifest v1alpha2 allowlist and sensitive-value omission metadata;
- existing resolver/bootstrap/domain/redaction tests.

## Decisions In Force

1. Infisical or explicit environment injection owns every secret value.
2. Database rows and manifests may contain non-secret metadata only.
3. Whole local-development databases and volumes may be disposed/recreated when
   needed for the clean baseline path, including all data and database-resident Data
   Protection material in those confirmed targets. Production, shared, staging, CI
   evidence, external provider/Infisical state, deployment secret stores, and unnamed
   targets are excluded. Record exact target identity and local/non-shared proof
   immediately before execution; ambiguity stops the command.
4. One explicit source mode is authoritative; provider failure never falls back.
5. Required secrets fail startup/activation; optional secrets affect only their
   owning capability with truthful status.
6. Provider payloads, exception details, coordinates, identifiers, and values are
   excluded from every output surface.
7. Keep `SecretBinding` only as a deep tenant/instance metadata/reference module.
8. Rotation is provider/consumer-specific and must preserve last-known-good or fail
   closed with forward recovery.
9. Configuration manifest remains secret-free and does not absorb secret lifecycle.
10. No file/Vault/cloud scaffolding, HybridCache/Redis L2, generic read audit,
    fixed Polly recipe, duplicate CRUD UI, or Kubernetes implementation.
11. All direct resolver consumers are operator-rotated and observe new values on
    next resolution/use or documented restart. Automation remains limited to the
    existing options-driven HTTP-client/database factories plus provider refresh and
    cache invalidation.
12. Runtime authority/confidentiality ships independently from activation/rotation.
13. Multi-replica rotation is deployment-coordinated, never inferred from one local
    callback: use old/new overlap until all intended replicas acknowledge, or a
    maintenance restart when overlap is unavailable. Partial activation withholds
    success/revocation; stale replicas drain/restart or fail the capability closed.
14. Authority-changing PRs ship their operator contract and executable validation;
    final convergence cannot defer the first truthful documentation.

## Worst Break

An Infisical/provider error includes credential material in diagnostics while the
system silently resolves a stale lower-authority environment value. Operators see
a healthy capability even though authority is compromised and a secret has leaked.
`SCN-SEC-001` and `SCN-OBS-001` must fail before any production change and pass
before Phase 1/3 exit.

## Implementation Sequence

| Phase | Purpose | Blocking scenarios |
|---|---|---|
| 0 | Extend the contribution contract for secret-authority paths and PR tests | Governance/schema/architecture checks |
| 1 | Deterministic authority and fail-closed bootstrap | `SCN-SEC-001`, bootstrap portions of `SCN-SEC-002` and `SCN-OBS-001` |
| 2 | Remove inline persistence and prove tenant isolation | `SCN-TEN-001` |
| 3 | Typed runtime policy, consumer activation state, cache, health, zero-secret outputs | complete `SCN-SEC-002`, `SCN-CAP-001`, `SCN-OBS-001` |
| 4 | Consumer activation, rotation, and recovery | `SCN-ROT-001`–`SCN-ROT-003` |
| 5 | Safe server-authorized status and manifest boundary | `SCN-MAN-001` |
| 6 | Deployment/operator convergence and recovery | `SCN-OPS-001` plus provider×topology artifact completeness |

## Evidence Ledger

| Path/source | Evidence retained |
|---|---|
| `.agents/contract/intents.yaml` | Tier 1 scope, mandatory acceptance, tests, docs, and forbidden actions |
| `docs/QUICK_REFERENCE.md` | Infisical/`.env` source-of-truth, generated migrations, HAL authority, greenfield posture |
| `src/Explore.Domain/Secrets/SecretBinding*.cs` | Current source/scope/ciphertext model |
| `src/Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs` | Current precedence and stderr behavior |
| `src/Explore.Secrets/Sources/*.cs` | Current environment, Infisical, and inline adapters |
| `src/Explore.Secrets/Services/SecretResolver.cs` | Current fallback scope, null semantics, and cache |
| `.env.example`, `docker-compose.yml`, `src/Explore.AppHost/AppHost.cs` | Deployment authority and projection |
| configuration-manifest handler/controller/workstream | Existing closed non-secret portability boundary |
| Microsoft, Aspire, Infisical, OWASP, Docker, Kubernetes official docs | Functional requirements only; no source or implementation expression retained |

## CTO Finding Disposition

| Finding | Disposition in corrected authority |
|---|---|
| 1. Oversized Phase 3 | Resolved in planning: Phase/PR 3 now owns runtime policy, consumers, confidentiality, cache, health, and docs; Phase/PR 4 independently owns activation, rotation, replica recovery, and runbooks. Each has its own exit gate. |
| 2. Replica rotation contract absent | Resolved in planning: `SCN-ROT-002` and `SCN-ROT-003` normatively define overlap, partial activation, delayed revocation, maintenance restart, stale-replica drain/restart/fail-closed behavior; `SEC-221`–`SEC-226` own executable evidence. |
| 3. Destructive target ambiguous | Resolved in planning and user decision: whole local-development databases/volumes may be recreated when needed, including their data/Data Protection material. Production/shared/staging/CI/external-provider/Infisical/deployment stores and unnamed targets are excluded; immediate target proof is mandatory. |
| 4. Operator docs deferred | Resolved in planning: Phase 1 ships authority/bootstrap docs, Phase 3 ships runtime failure/health docs, Phase 4 ships rotation/recovery docs, and Phase 6 only converges/validates them. |
| 5. Provider/topology evidence implicit | Resolved in planning: the plan names Environment and Infisical across Standalone, Aspire, Compose, and multi-replica split rows, rejects unimplemented Vault/Azure/AWS rows, and names executable PostgreSQL/SQLite/SQL Server/MariaDB/MySQL artifacts. |

## Validation Baseline And Constraints

- The one-time pre-product Release baseline completed on 2026-08-30: 45 projects,
  0 errors, and 343 pre-existing warnings.
- `SEC-001` Red evidence completed on 2026-08-30: the focused
  `BootstrapSecretLoaderTests` slice ran 16 tests, with 12 retained passes and four
  expected failures proving current lower-authority fallback for Infisical absence,
  invalidity, unavailability, and unauthorized response.
- Planning verification is ABOUTME/header, internal-path/link, revision consistency,
  and scoped `git diff --check`.
- During implementation, use the fastest relevant TUnit slice for active work. Each
  product PR runs exactly one Release build plus every applicable intent-minimum
  test project once at exit; PR0 uses governance/schema/link/architecture checks.
- EF migration files and model snapshots are generated, never hand-edited.

## Blockers

1. Immediately-before-execution identity proof for each authorized local-development
   database/volume target remains a later destructive-operation precondition, not a
   completed fact. Destructive permission exists only inside that boundary.

## Dated Handoff

### 2026-08-30 Europe/Brussels — Full Re-baseline

Replaced the 2026-04 speculative plan with repository-grounded source authority,
removed obsolete Phase 3 execution authority, and split work into five reviewable
product phases behind one governance prerequisite. No runtime code changed. Next
action is I-VSD revalidation, not governance or product implementation.

### 2026-08-30 Europe/Brussels — I-VSD Revalidation Corrected

Adversarial verification rejected the first completion claim because
multi-replica setup-secret authority lacked explicit scenario/task acceptance and
the triad still recorded stale gate state. The plan now extends `SCN-OPS-001`,
`SEC-401`, and `SEC-404` with one-authority, fail-closed, cleanup, and recovery
requirements; Plan Section 9 and the I-VSD report use the same `IVSD-F003` mapping.
At that historical revision, `GATE-001` was complete for the recorded digest and no
product work, implementation approval, or disposal permission had occurred. The
later correction handoff below supersedes that gate/permission state.

### 2026-08-30 Europe/Brussels — Independent GATE-002 Review

The independent review bound the exact current plan, tasks, and I-VSD revisions and
returned `Split before approval`. The target architecture remains endorsed, but
Phase 3 must split, runtime rotation needs an explicit multi-replica support matrix,
GATE-003 must name the exact destructive target and data scope, behavior-owning
operator docs must move into their PRs, and provider/topology evidence must be
enumerated. This status-only synchronization does not change provider responsibility
or the then-current I-VSD disposition. At that time GATE-002 and GATE-003 remained
pending and no product implementation or disposal/reset permission existed; the
later correction handoff below supersedes that permission state.

### 2026-08-30 Europe/Brussels — CTO Blocker Correction Rewrite

Resolved the delivery-shape blockers without changing product code or the immutable
CTO verdict. The prior Phase 3 is split into independently reviewable runtime
authority/confidentiality and consumer activation/rotation/recovery PRs. New
normative scenarios own overlap, coordinated restart, partial activation, delayed
revocation, and stale-replica fail-closed behavior. Authority, runtime, and rotation
operator docs now ship in their behavior-changing PRs. The plan names executable
Environment/Infisical × Standalone/Aspire/Compose/multi-replica evidence and the
five-engine PostgreSQL/SQLite/SQL Server/MariaDB/MySQL clean-baseline matrix.

The user explicitly authorized disposal/recreation of whole local-development
databases and volumes when needed for the clean baseline path; production, shared,
staging, CI evidence, external-provider/Infisical state, deployment secret stores,
and unnamed targets remain excluded, and every target still requires immediate
pre-execution identity proof. Because task ownership and provider/rotation
responsibility changed materially, the prior I-VSD report is stale, GATE-001 is
pending, GATE-002 remains blocked, GATE-003 lacks product implementation approval,
and product work remains prohibited. GATE-001 must evaluate corrected combined
`sha256:d280829bf7e8e0f086f7423ba55f9f516d0165625f375a8c94396c654031c895`.

**DoneClaim:** The authoritative plan/tasks/context correction is complete for
planning handoff only. The I-VSD report is intentionally stale, GATE-001/GATE-002/
GATE-003 remain blocked as stated, no CTO or product approval is claimed, and no
product implementation may start from these bytes.

### 2026-08-30 Europe/Brussels — Fresh Planning-Mode I-VSD Revalidation

Revalidated the stable provider-responsibility findings against the corrected
runtime/rotation split, `SCN-ROT-002`/`SCN-ROT-003`, provider×topology evidence, and
five-database matrix. Added explicit I-VSD traceability for secret-free portability
and the user-owned Section 13 local-development disposal boundary. Plan Section 9
and the report now match exactly. `GATE-001` is complete for combined
`sha256:825b965e4937a7544ec99ea93c20456f2737c43441d45d44ac00549586805757`.
`GATE-002`, `GATE-003`, and `GOV-001` remain blocked; no product implementation or
technical/product approval occurred.

### 2026-08-30 Europe/Brussels — Fresh GATE-002 CTO Approval

The independent Senior CTO re-review bound the exact corrected plan, tasks, context,
and current/plan-aligned I-VSD revisions and returned `Approve`. All five former
findings are resolved in the planning authority; no new technical blocker was found.
GATE-002 is complete. GATE-003 remains pending because this review neither records
nor infers product implementation approval. The existing local-development disposal
boundary is unchanged, and every destructive target still requires immediate
environment/provider/database/container/volume identity and local/non-shared proof.
No product implementation or destructive command has started.

### 2026-08-30 Europe/Brussels — GATE-003 User Authority Recorded

The user explicitly approved full implementation of the GATE-002-approved
no-backward-compatibility workstream against pre-GATE-003 combined plan/tasks
`sha256:a6255e78747ee7d85f42b27b213a5a0c3db1f250c0b24702856b4b6000445f37`.
Separately, the mandatory Tier 1 Grill-Me decision selected `Whole development
databases`: whole LOCAL DEVELOPMENT databases and volumes may be disposed/recreated
when required by the clean migration path. This does not authorize production,
shared, staging, CI evidence, external-provider/Infisical state, deployment stores,
unnamed targets, or ambiguous targets. No target is yet proven and no reset occurred;
each destructive command still requires immediate environment/provider/database/
container/volume identity and local/non-shared proof.

GATE-003 is complete. GOV-001 is now ready but remains pending because no matching
secrets-authority intent exists. GOV-002 remains pending behind GOV-001, and SEC-001
remains blocked behind GOV-002. No product file was edited.

**DoneClaim:** All three approval gates are complete and the user's two authorities
are recorded without broadening destructive scope. Phase 0 governance is not yet
complete: `GOV-001` is the exact next executable task, followed by `GOV-002`; product
implementation begins only at `SEC-001` after both and after downstream `.omo`
execution state/todos are created.
