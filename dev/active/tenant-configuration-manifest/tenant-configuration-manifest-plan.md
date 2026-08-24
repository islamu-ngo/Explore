<!-- ABOUTME: Decision-complete implementation plan for tenant configuration manifests and reporting-intake policy. -->
<!-- ABOUTME: Sequences backend policy integrity, strict bootstrap, export, self-hosting, and HAL-driven administration. -->

# Tenant Configuration Manifest And Reporting-Intake Policy — Implementation Plan

Last Updated: 2026-08-21 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Replace environment-variable business-policy sprawl with an enterprise-grade declarative tenant configuration manifest, add standalone-container manifest ingestion and export, and model report-intake disablement as an independent tenant policy guarded by effective publication safety.
- **Task directory:** `dev/active/tenant-configuration-manifest/`
- **Planning status:** Draft; awaiting user review and approval
- **Primary matched intent:** `external-infrastructure-bootstrap` as the closest operational-bootstrap contract; the intent registry has no exact internal tenant-configuration-manifest intent.
- **Additional matched intents:** `add-cqrs-handler`, `add-get-endpoint`, `add-hal-link`, `openapi-contract-change`, `blazor-component-affordance`, and `add-ef-migration`.
- **Intent-contract gap:** `external-infrastructure-bootstrap.paths_in_scope` does not currently include `src/Event.Standalone/**`, `src/Event.MigrationService/**`, schema artifacts, or the proposed tenant-manifest feature paths. The first implementation slice must extend the intent contract without weakening existing restrictions.
- **Relevant skills:** `implementation-plan`, `i-vsd`, `senior-cto-feedback`, `agentic-research`, `ip-clean-room`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `blazor-ui-conventions`, `accessibility`, and `error-tracking`.
- **Relevant rules:** `.agents/rules/application-layer.md`, `api-controllers.md`, `api-hateoas.md`, `domain.md`, `efcore-persistence.md`, `efcore-migrations.md`, `blazor-client.md`, `tests.md`, and `ip-clean-room.md`.
- **Primary layers touched:** Domain settings metadata; Application policy, commands, queries, DTOs, validators, and contracts; Persistence entities, repositories, transactions, and generated provider migrations; Infrastructure file ingestion; API controllers, ProblemDetails mapping, HAL, OpenAPI; Blazor client/BFF services and admin UI; `Event.Standalone`; `Event.MigrationService`; Compose and operator documentation.
- **Complexity:** XL. The work changes policy semantics across every settings mutation path, introduces a versioned external contract, adds transactional multi-tenant bootstrap and audit persistence, integrates two startup topologies, adds authenticated export and generated-client changes, and requires tenant-safe UI/operations behavior.
- **I-VSD Document:** [islamic-value-sensitive-design/i-vsd-tenant-configuration-manifest.md](../../../islamic-value-sensitive-design/i-vsd-tenant-configuration-manifest.md)
- **Grill-Me Intake:** Repository evidence and the prior Senior CTO review resolved the material branches. The user accepted the recommendations to use an independent reporting-intake policy, enforce publication policy first, ship JSON bootstrap before reconciliation, exclude secrets, preserve a separate correction/legal channel, and support standalone containers through an environment path plus a read-only bind mount. No unresolved user decision remains.

## 1. Executive Summary

This workstream introduces a strict, versioned **Tenant Configuration Manifest** for Day 0 bootstrap and portable configuration export while keeping Day 2 database/UI/API administration authoritative. It also introduces a tenant-scoped `event_reporting.intake_enabled` policy, defaulting to `true`, without changing the existing external-provider meanings of `Reporting:Mode`, `Reporting:Enabled`, or `LocalOnly`.

Before report intake may be disabled, backend event creation and publication must prove that no unvetted actor can cause public publication. The same invariant must guard tenant policy updates, generic Control Plane setting writes, tenant-plan application, manifest bootstrap, and direct report submission. API/HAL and Blazor must reflect the server-authored effective policy.

Manifest v1 supports strict JSON, `Off`, `ValidateOnly`, and `Bootstrap` modes. It does not implement continuous reconciliation or `AlwaysOverride`; managed desired-state behavior is deferred until field ownership, drift, conflict, deletion, and recovery semantics receive a separate approved plan.

### Intended Outcomes

- `.env` remains infrastructure/bootstrap plumbing rather than a catalog of tenant business policy.
- Self-hosters can bootstrap tenants without an external provider or Control Plane.
- `Event.Standalone` accepts a container-internal manifest path supplied through `.env`/`--env-file` and a read-only bind mount.
- Split deployments apply the manifest exactly once in `Event.MigrationService` after migrations and before API startup.
- Tenant and Control Plane administrators can export non-secret configuration for review and version control.
- Local event reporting remains available by default and is disabled only under a server-enforced safe publication policy.

### Explicit Non-Goals

- YAML support in manifest v1.
- `AlwaysOverride`, continuous reconciliation, deletion propagation, or Kubernetes-style field ownership.
- Importing raw credentials, report content, PII, users, roles, events, or external-provider resources.
- Treating the manifest as backup/restore.
- Preserving any proposed pre-implementation `SEED_MANIFEST_*` naming or behavior.
- Compatibility shims for unshipped configuration keys or API contracts.
- Replacing current tenant-plan functionality.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| The settings registry is code-defined and typed. | `src/Explore.Domain/Settings/SettingRegistry.cs`, `SettingDefinition.cs` | High | 257 definitions across 33 registered categories were counted during planning; five typed tenant settings documents also exist. |
| Event submission and approval values exist. | `src/Explore.Domain/Settings/Definitions/EventSettingDefinitions.cs`; `src/Explore.Application/Settings/Groups/EventSettingGroup.cs` | High | User, organization, group, and approval values are tenant-scoped. |
| Existing reporting mode controls optional providers, not report intake. | `src/Explore.Infrastructure/Configuration/ModerationProviderOptions.cs`; `ReportingRoutingPolicyResolver.cs` | High | `LocalCanonicalRequired` remains true while external providers may be disabled. |
| Published events currently advertise reporting. | `src/Explore.API/Hateoas/Policies/EventLinkPolicy.cs`; `GetEventReportOptionsRequestHandler.cs` | High | Report links and `IsReportable=true` are not conditioned on a tenant intake policy. |
| Direct report submission has a dedicated Application handler. | `src/Explore.Application/Features/EventReporting/Handlers/Commands/SubmitEventReportCommandHandler.cs` | High | The direct write path must enforce the new policy even when HAL is bypassed. |
| Policy-critical settings have multiple mutation paths. | `TenantPolicySettingService.Apply.cs`; `SetControlPlaneTenantSettingCommandHandler.cs`; `ApplyControlPlaneTenantPlanAssignmentCommandHandler.cs`; `UpdateReportingRoutingSettingsCommandHandler.cs` | High | A two-handler guard is bypassable. |
| Existing Control Plane plan apply already demonstrates transactional bulk setting writes and audit summaries. | `ApplyControlPlaneTenantPlanAssignmentCommandHandler.cs`; `TenantPlanApplicationLog.cs`; `ITenantSettingRepository.cs` | High | Reuse the pattern, not nested MediatR command dispatch. |
| Standalone migrates and seeds before serving. | `src/Event.Standalone/Program.cs` | High | SQLite is constrained to one replica; the host runs as non-root and persists `/app/data`. |
| Split production bootstrap is owned by a one-shot service. | `src/Event.MigrationService/Worker.cs`; `docker-compose.yml` | High | The manifest belongs in the migration-service sequence, not an API hosted service. |
| The proposed Blazor page names do not exist. | Repository inventory of `src/Explore.Blazor.Client/Pages/Admin/` | High | Current tenant policy UI is composed through `TenantAdminSettings.razor` and `TenantPoliciesSection.razor`. |
| No overlapping manifest workstream exists. | `dev/active/` and `dev/pause/` search for manifest/reporting/settings/provisioning | High | Create a new workstream. |
| Build baseline is green with an unrelated advisory. | `dotnet build --configuration Release --verbosity quiet` on 2026-08-21 | High | 0 errors; pre-existing `SSH.NET` NU1903 warnings remain out of scope. |
| Architecture baseline is green. | `Event.Architecture.Tests` on 2026-08-21 | High | 421 passed, one documented skip. |

### 2.2 Existing Implementation

#### Domain

- `SettingRegistry` owns immutable definitions, types, defaults, scopes, locks, sensitivity, and allowed values.
- Tenant setting overrides and typed JSON settings documents already exist.
- Event settings define submission and approval values.
- There is no local report-intake setting.
- No manifest boundary model exists, which is correct for Domain purity.

#### Application

- `EventSettingGroup` and `ReportingSettingGroup` batch-resolve effective settings.
- `EventLifecyclePolicyProvider` consumes event settings, but the current plan cannot rely on those values as a complete authorization/publication barrier until all create/publish paths are tested and corrected.
- Reporting option, submission, tenant policy, Control Plane setting, and plan-application handlers are separate mutation/read paths.
- `IHierarchicalSettingsResolver` provides typed resolution, locks, cache invalidation, and setting writes.

#### Persistence

- Tenant/system settings use normalized rows and hierarchical lock semantics.
- Control Plane plan application provides a repository-native bulk transaction precedent.
- No manifest operation/result audit entities exist.
- EF Core migrations are provider-specific generated artifacts and may not be hand-edited.

#### Infrastructure And Hosts

- `ModerationProviderOptions` configures external moderation providers.
- `ReportingRoutingPolicyResolver` preserves local canonical handling.
- `Event.Standalone` runs migrations/seeding in-process before serving.
- Split deployments run `Event.MigrationService` as the one-shot migration owner.
- No strict file-ingestion service or typed manifest options exist.

#### API, HAL, And Blazor

- Event HAL links advertise reporting for eligible published public events.
- Direct report options and submission endpoints do not resolve an intake policy.
- Control Plane and tenant setting APIs already provide authenticated configuration surfaces.
- Blazor gates resource actions by HAL and uses generated API clients/BFF services.
- There is no export endpoint, manifest DTO, or manifest download UI.

### 2.3 Existing Tests And Verification Coverage

Verified relevant projects/files include:

- `tests/Event.Application.UnitTests/Features/EventReporting/**`
- `tests/Event.Application.UnitTests/Features/ControlPlane/Commands/**`
- `tests/Event.Application.UnitTests/Services/TenantPolicySettingServiceTests.cs`
- event create/publish handler tests in `Event.Application.UnitTests`
- `tests/Event.API.IntegrationTests/Features/EventReportsControllerTests.cs`
- `tests/Event.API.IntegrationTests/Features/EventReportOptionsControllerTests.cs`
- `tests/Event.API.IntegrationTests/Features/EventReportHateoasTests.cs`
- `tests/Event.API.IntegrationTests/Features/ModerationReportingRoutingControllerTests.cs`
- Control Plane HATEOAS tests in `Event.API.IntegrationTests`
- `tests/Event.Persistence.IntegrationTests/**`
- `tests/Event.Standalone.IntegrationTests/StandaloneHostGraphTests.cs`
- `tests/Explore.Blazor.Client.Tests/Pages/Admin/TenantPoliciesSectionTests.cs`
- Control Plane and reporting client service tests in `Explore.Blazor.Client.Tests`
- `tests/Event.Architecture.Tests/**`

No manifest schema, parser, transaction, startup, export, or secret-omission tests exist.

### 2.4 Confirmed Gaps

1. Reporting intake and external provider routing are not separate policies.
2. Effective publication safety is not proven across every backend path.
3. Cross-setting invariants are not centralized across all mutation routes.
4. No versioned manifest contract, explicit setting allowlist, or strict parser exists.
5. No startup application record, digest, tenant result, or atomic apply operation exists.
6. No standalone/split manifest startup orchestration exists.
7. No authenticated export contract, HAL relation, generated client, or admin UI exists.
8. No exact internal-configuration-bootstrap intent exists for all required paths.

## 3. Proposed Future State

```text
operator file / export
        |
        v
Infrastructure strict reader
  - one bounded read
  - duplicate-key rejection
  - UTF-8 / strict JSON
  - SHA-256 provenance digest
        |
        v
Application manifest compiler
  - apiVersion/kind validation
  - setting allowlist + type validation
  - tenant identity/duplicate checks
  - complete proposed-state invariant validation
        |
        v
Persistence transactional apply
  - all manifest items prevalidated
  - existing tenants skipped in Bootstrap
  - new tenants/settings/documents created atomically
  - operation + per-tenant audit results
        |
        v
post-commit application effects
  - cache invalidation
  - setting notifications
  - structured outcome logs/metrics
```

### 3.1 Manifest Envelope

```json
{
  "$schema": "https://schemas.islamu.org/event/tenant-configuration/v1/schema.json",
  "apiVersion": "configuration.islamu.org/v1alpha1",
  "kind": "TenantConfigurationList",
  "metadata": {
    "name": "primary-deployment"
  },
  "spec": {
    "tenants": [
      {
        "metadata": {
          "name": "default"
        },
        "spec": {
          "displayName": "Primary Community",
          "settings": {
            "events.user_submission_enabled": false,
            "events.organization_submission_enabled": false,
            "events.group_submission_enabled": false,
            "events.require_approval": true,
            "event_reporting.intake_enabled": false
          },
          "documents": {}
        }
      }
    ]
  }
}
```

`metadata.name` for a tenant maps to the existing unique tenant slug. The manifest contains tenant configuration only; no user/admin credentials are bootstrapped.

### 3.2 Bootstrap Modes

| Mode | Behavior |
|---|---|
| `Off` | Do not discover, validate, or apply a manifest. |
| `ValidateOnly` | Read and validate the complete document, emit outcome diagnostics, and write no tenant configuration. |
| `Bootstrap` | Validate all items first; create and configure absent tenants; skip existing tenants wholesale; never patch an existing tenant. |

When no explicit path is set, the host may discover `/etc/islamu-event/bootstrap/tenant-configuration.json`. Absence is a no-op. An explicitly configured missing/unreadable path or any present invalid file fails before serving traffic.

### 3.3 Export Views

- **Overrides** is the default: only manifest-allowlisted tenant-owned overrides/documents are emitted.
- **Portable** is explicit: effective non-sensitive values are emitted to reproduce behavior on a new tenant, with metadata declaring that inherited/default values were flattened.
- Sensitive values are omitted in both views; exports include warnings/annotations listing omitted keys without values.
- Exported files are configuration artifacts, not backups.

## 4. Non-Negotiable Constraints

1. Keep `Reporting:Mode`, `Reporting:Enabled`, `LocalOnly`, Osprey, and Coop semantics unchanged.
2. `event_reporting.intake_enabled` defaults to `true`.
3. Disabling intake requires effective closed or enforced-approval publication policy.
4. Direct report POST, options, HAL, tenant policy, Control Plane setting, tenant-plan apply, and manifest apply must agree.
5. UI uses HAL for action authorization and server-authored capability state for whether disablement is currently valid.
6. Manifest models do not live in Domain.
7. Application handlers do not invoke other MediatR commands to compose the transaction.
8. Repositories return entities, not DTOs.
9. Validators are manually instantiated.
10. Tenant isolation is explicit on every read/write/export.
11. Raw secrets never enter exported manifests, audit records, logs, traces, or ProblemDetails.
12. EF migrations and snapshots are generated, never hand-edited.
13. Startup apply is idempotent, atomic, cancellation-aware, and owned by one startup path.
14. No YAML, compatibility alias, or `AlwaysOverride` implementation in this workstream.
15. Every new file begins with the repository-standard two-line `ABOUTME` summary.

## 5. Architecture And Design Decisions

### 5.1 Decision Matrix

| Decision | Selected approach | Rejected approach | Rationale |
|---|---|---|---|
| Report intake | New `event_reporting.intake_enabled` tenant policy | Reuse external `Reporting:Mode`/`Enabled` | Preserves established provider semantics and local-first safety. |
| Policy enforcement | Application-level effective-state evaluator used by every mutation path | Guard only two handlers or UI | Prevents direct API, plan, manifest, and internal bypasses. |
| Manifest contract | Strict JSON with `apiVersion`, `kind`, metadata, and spec | YAML-first or unversioned DTO | Deterministic parsing, editor schema support, explicit evolution. |
| Setting shape | Flat canonical setting keys plus separate typed documents | Hand-maintained nested model for 257 settings | Reuses registry identity and avoids a second naming taxonomy. |
| Exposure control | Explicit Application manifest setting catalog referencing Domain registry definitions | Automatically expose every non-sensitive tenant setting | New settings must not become externally configurable by accident. |
| Runtime validation | Strict `System.Text.Json` reader plus manual Application validators | New runtime JSON Schema package | No new runtime dependency; repository-native validation remains authoritative. |
| Schema generation | Deterministic BCL-based generator tool and checked-in Draft 2020-12 artifact | Hand-edited schema or runtime reflection-only schema | Prevents contract drift without adding a runtime dependency. |
| Initial lifecycle | `Off`, `ValidateOnly`, `Bootstrap` | `SeedIfEmpty` per setting group or `AlwaysOverride` | Whole-tenant bootstrap is predictable and preserves Day 2 changes. |
| Apply semantics | Validate whole document, then one transaction | Dispatch existing commands sequentially | Avoids partial tenants and fragmented authorization/UoW behavior. |
| Startup owner | Standalone after migrations; split `Event.MigrationService` after migrations | API hosted service in every replica | Matches existing topology ownership and avoids replica races. |
| Audit | Manifest operation plus per-tenant result, digest, changed key names, status, timestamps | Store raw manifest or values | Supports diagnosis without retaining secrets or private content. |
| Export | Authenticated override/portable views with secret omission | “Round-trip fidelity” including secrets | Separates portability from backup and inheritance semantics. |
| UI | Existing tenant admin settings architecture, generated client, HAL-gated actions | New disconnected `ReportingSettings.razor` page | Matches repository structure and authorization source of truth. |
| Compatibility | Clean break for unshipped manifest names/contracts | Aliases for `SEED_MANIFEST_*` | Development-mode project; compatibility adds unjustified surface. |

### 5.2 Clean Architecture Ownership

- **Domain:** setting definitions, tenant/audit entities, explicit state methods where entity invariants exist.
- **Application:** manifest contracts, setting catalog, policy evaluator, complete-state validator, commands/queries, DTO mapping, authorization facts, and repository interfaces.
- **Persistence:** EF configurations, repositories, transaction execution, provider-generated migrations, and audit persistence.
- **Infrastructure:** bounded file reading, duplicate-key detection, strict deserialization, options validation, hashing, and startup-facing bootstrap service.
- **API:** authenticated export/controller contracts, ProblemDetails mapping, route names, HAL policies, OpenAPI schemas.
- **Blazor/BFF:** generated client adapter, HAL/capability consumption, accessible controls, download behavior.
- **Hosts:** ordering after database migration and before traffic.

### 5.3 Manifest Parsing Contract

- Read the selected regular file once into a bounded byte buffer; initial fixed maximum is 4 MiB.
- Reject relative paths, directories, symlinks, non-UTF-8 content, duplicate object properties, unknown properties, unsupported versions/kinds, duplicate tenant slugs, and keys absent from the manifest catalog.
- Parse property names case-sensitively.
- Compute SHA-256 from the exact bytes read and never log content.
- Validate setting value type and allowed values from `SettingRegistry`.
- Reject sensitive keys in v1; secret references are deferred until an approved secret-provider design exists.
- Validate every tenant and cross-setting invariant before opening the apply transaction.

### 5.4 Reporting-Intake Invariant

The evaluator returns an explicit result with `Allowed`, stable reason code, and operator-safe message.

```text
MayDisableReportingIntake =
  all non-privileged submission paths are closed
  OR every such path is forced through an enforced admin approval boundary
```

The evaluator consumes the effective publication policy, not raw request fields. Federation, imports, automation, and any new publication path must be classified before they can claim safety.

When intake is disabled:

- event detail HAL omits report/correction links governed by event reporting;
- report options return `IsReportable=false` with `event_reporting_disabled`;
- direct report submission fails with a stable Application failure code mapped to RFC 7807;
- tenant settings expose server-authored `CanDisable`/reason state;
- a separate general correction/legal/copyright contact surface remains documented and available.

## 6. Implementation Phases

## Phase 1 — Publication And Reporting Policy Integrity

**Goal:** Make event publication safety enforceable before introducing report-intake disablement.

### TCM-110 — Enforce effective submission and approval policy

- Verify and correct create/publish paths for user, organization, group, import, federation, automation, and administrator actors.
- Extend or split `EventLifecyclePolicyProvider` only where needed; keep authorization and lifecycle validation responsibilities explicit.
- Add regression tests proving non-privileged actors cannot directly publish when approval is required and cannot submit through disabled paths.
- Record newly discovered publication paths in context before expanding implementation scope.

### TCM-120 — Add reporting-intake setting and invariant evaluator

- Add `event_reporting.intake_enabled` with default `true` and tenant maximum scope.
- Add a dedicated strongly typed reporting-intake policy group rather than extending external-provider runtime options.
- Add an Application evaluator for effective publication safety and a complete proposed-state invariant validator.
- Integrate the validator with tenant policy apply, generic Control Plane setting writes, tenant-plan preflight, and any direct hierarchical write path for the guarded keys.
- Add truth-table, stale-state, concurrent-change, lock, and bypass tests.

### TCM-130 — Enforce intake policy on report reads, writes, and HAL

- Gate event report options, direct report submission, and all report/correction HAL relations.
- Return stable failure/reason codes without exposing authorization internals.
- Preserve local canonical reporting whenever intake is enabled regardless of external-provider mode.
- Extend Application/API tests in the same task.

**Phase verification:**

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
```

## Phase 2 — Reporting Policy API Contract

**Goal:** Provide an authenticated, server-authored policy resource suitable for HAL-driven administration.

### TCM-210 — Add tenant reporting-intake policy query and mutation contract

- Add query/update requests, DTOs, manual validators, authorization facts, and handlers.
- Return effective enabled state, source/lock metadata, `CanDisable`, and stable unavailability reason.
- Keep controllers thin; use route constants, immutable failure policies, explicit ProblemDetails response metadata, rate limits, and request timeouts.
- Add HAL relations for viewing/updating and omit mutation affordances when authorization or lock state forbids them.
- Update OpenAPI catalog coverage and API integration tests.

### TCM-220 — Update tenant policy composition without compatibility shims

- Integrate the policy into current tenant settings reads/writes where required.
- Remove any now-duplicated local policy calculation.
- Regenerate OpenAPI and the NSwag client after the server contract stabilizes; do not hand-edit generated code.
- Document the breaking development contract in `docs/API_CHANGELOG.md`.

**Phase verification:**

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
```

## Phase 3 — Manifest Contract, Catalog, And Schema

**Goal:** Establish one deterministic, versioned, editor-friendly contract before persistence or startup orchestration.

### TCM-310 — Add strict manifest contracts and setting catalog

- Add Application contract types under a tenant-configuration-manifest feature, not Domain.
- Add an explicit allowlist catalog whose entries resolve to `SettingRegistry` definitions.
- Include only tenant-safe, non-sensitive settings and approved typed documents.
- Add manual structural, semantic, and cross-reference validators.
- Add source-generated JSON serialization metadata where the feature participates in AOT/source-generated serialization.

### TCM-320 — Add bounded strict JSON ingestion

- Add Infrastructure options and validator for `TENANT_MANIFEST_PATH` and `TENANT_MANIFEST_MODE`.
- Add one-read bounded file ingestion, duplicate-property detection, strict case-sensitive deserialization, digest calculation, and safe diagnostics.
- Support `/etc/islamu-event/bootstrap/tenant-configuration.json` convention discovery.
- Add tests for missing explicit paths, absent convention paths, permissions, oversize files, symlinks, duplicates, unknown properties, unsupported versions, invalid values, and secret keys.

### TCM-330 — Generate and govern JSON Schema Draft 2020-12

- Add a deterministic BCL-only schema generator tool referencing the Application manifest contract/catalog.
- Add `schemas/tenant-configuration-manifest-v1.schema.json` with immutable `$id`, explicit required properties, constraints, enums, and `additionalProperties: false`.
- Add an architecture/contract test that fails when generated output differs from the checked-in artifact.
- Package the schema with releases and document the canonical URL; public schema hosting must serve the exact checked-in bytes before documentation advertises the URL.
- Record provenance; no third-party schema source or copied representation enters the repository.

**Phase verification:**

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## Phase 4 — Transactional Tenant Bootstrap And Audit

**Goal:** Apply absent tenants and their declarative configuration atomically and audibly.

### TCM-410 — Add manifest operation and tenant-result persistence

- Add operation/result entities with UUIDv7 IDs, mode, apiVersion, kind, manifest name, digest, bounded status/reason data, timestamps, and changed key names.
- Never persist raw manifest bytes, secret values, exported values, or report content.
- Add entity configurations, repository contracts/implementations, tenant-safe indexes, and a transaction boundary.
- Generate provider migrations through the canonical EF tooling for every supported provider; never patch generated files.

### TCM-420 — Add compile, preflight, and atomic bootstrap command

- Add one Application command/service that compiles the validated contract into an apply plan.
- Resolve tenant slugs and system locks, validate all items and effective policy combinations, then apply the document in one transaction.
- Create absent tenants using repository-native shared services extracted from current tenant creation logic; do not dispatch nested commands.
- Skip existing tenants wholesale with `SkippedExisting`; do not fill missing setting groups.
- On write failure, roll back all tenant configuration and persist only a safe failure operation when the database remains available.
- Invalidate caches and publish setting notifications only after commit.
- Add idempotency, rollback, race, wrong-tenant, multi-item, and provider persistence tests.

**Phase verification:**

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
```

## Phase 5 — Standalone And Split Startup Integration

**Goal:** Make bootstrap behave identically in supported self-hosting topologies without replica races.

### TCM-510 — Integrate post-migration pre-traffic bootstrap

- In `Event.Standalone`, run manifest validation/application after `ExploreDatabaseMigrator.MigrateAndSeedAsync` and before API/Blazor startup.
- In split deployments, register the narrow manifest Application/Infrastructure dependencies in `Event.MigrationService` and run bootstrap after migration before the one-shot service exits.
- Keep the API runtime out of split-topology reconciliation.
- Development API startup may use the same post-migration bootstrap sequence only when it owns migrations.
- Explicit invalid paths or invalid manifests fail startup with non-secret structured diagnostics and non-zero migration-service exit.

### TCM-520 — Add deployment and standalone container support

- Extend the contribution intent paths before editing host files.
- Wire Compose environment and a read-only manifest bind mount to `event-migrationservice`, not only the API.
- Document standalone `docker run --env-file` plus a read-only bind mount at `/etc/islamu-event/bootstrap/tenant-configuration.json`.
- Keep `/app/data` for mutable application/SQLite state; do not auto-discover configuration there.
- Document non-root file permissions and a digest-pinned derived-image pattern using `COPY --chown`.
- Add deterministic options, service-composition, host-order contract, rerun, absent-file, and invalid-file coverage without starting Docker, a browser, or external services.

**Phase verification:**

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
```

## Phase 6 — Authenticated Export, HAL, And BFF Integration

**Goal:** Export safe configuration through tenant and Control Plane surfaces.

### TCM-610 — Add override and portable export queries

- Add one tenant-scoped Application query with `Overrides` default and explicit `Portable` view.
- Resolve only manifest-catalog settings/documents.
- Omit sensitive values and include non-secret omission metadata.
- Preserve deterministic property/key ordering and line endings so version-control diffs are stable.
- Add wrong-tenant, inherited-value, locked-value, document, and secret-omission tests.

### TCM-620 — Add tenant and Control Plane export endpoints

- Add authenticated tenant-self and multi-tenant Control Plane GET endpoints over the same query.
- Use route constants, rate limits, request timeouts, explicit authorization, attachment content disposition, and a versioned media type.
- Add HAL export relations only for authorized callers.
- Update OpenAPI, schema catalogs, generated client, API changelog, and integration tests.

### TCM-630 — Add server/BFF download integration

- Add BFF-safe service methods; components must not bypass the BFF.
- Stream or buffer only within the bounded manifest size and preserve the server filename/content type.
- Add authentication, failure mapping, and download integration coverage.

**Phase verification:**

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
```

## Phase 7 — Tenant Administration, Accessibility, And Operator Documentation

**Goal:** Make policy and export understandable and operable through current admin UX and canonical docs.

### TCM-710 — Add reporting-intake administration

- Integrate the policy into the existing tenant admin settings composition.
- Gate mutation by HAL; use server-authored `CanDisable` and reason state rather than local role/claim checks.
- Explain that external-provider disablement differs from report-intake disablement.
- Preserve keyboard operation, focus, semantic labels, validation summary, live-region announcements, RTL-safe layout, and localization.
- Add component tests for enabled, safely disableable, blocked, locked, unauthorized, error, and successful states.
- Reconcile policy wording with the I-VSD report and canonical configuration/security documentation.

### TCM-720 — Add tenant manifest export interaction

- Add a HAL-gated export control using the generated client/BFF service.
- Let authorized users select Overrides or Portable with clear secret-omission and backup disclaimers.
- Use deterministic accessible filenames and announce download failures/success without exposing filesystem details.
- Add Blazor client service/component tests.
- Add final API/configuration/self-hosting links where users encounter export and record `Reconcile`/field ownership as deferred with no dormant production enum or stub.

**Phase verification:**

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

## 7. Testing Strategy

### Test-First Boundaries

- Write failing regression tests before changing publication-policy enforcement.
- Lock the current external-provider semantics before adding the intake policy.
- Prove every unsafe settings mutation path fails before implementing manifest application.
- For manifest parsing, each rejection test must fail for its named malformed property, not a prior unrelated error.
- Use event/state signals for asynchronous startup tests; fixed sleeps and polling delays are forbidden.

### Required Coverage

| Concern | Primary project |
|---|---|
| Publication policy and invariant truth table | `Event.Application.UnitTests` |
| Reporting options, submission, HAL, ProblemDetails, auth | `Event.API.IntegrationTests` |
| Manifest catalog/schema drift and architecture boundaries | `Event.Architecture.Tests` |
| Transaction, rollback, audit, provider migrations | `Event.Persistence.IntegrationTests` |
| Startup options, composition, ordering contracts, and idempotency | `Explore.Infrastructure.Tests` |
| BFF download and authenticated server integration | `Explore.Blazor.IntegrationTests` |
| Tenant policy/export components and HAL behavior | `Explore.Blazor.Client.Tests` |

Architecture, generated migration, generated client, and schema artifacts may not be weakened, manually patched, or excluded to make tests pass.

## 8. Documentation, Configuration, And Operations Impact

### Configuration

- New canonical operator keys: `TENANT_MANIFEST_PATH` and `TENANT_MANIFEST_MODE`.
- Modes: `Off`, `ValidateOnly`, `Bootstrap`.
- Convention path: `/etc/islamu-event/bootstrap/tenant-configuration.json`.
- No `SEED_MANIFEST_*` aliases and no YAML extension in v1.

### Documentation

Update:

- `.env.example`
- `docs/CONFIGURATION.md`
- `docs/SECRETS.md`
- `docs/SELF_HOSTING.md`
- `docs/TROUBLESHOOTING.md`
- `docs/API_CHANGELOG.md`
- schema documentation/indexes
- README links only where the operator entry point requires it

### Release And Changelog Strategy

- Treat the manifest envelope and export media type as a new versioned public contract.
- Record API additions and generated-client changes in `docs/API_CHANGELOG.md`.
- Ship the exact schema artifact with releases and container documentation.
- Pin example container images by release version/digest; never recommend `latest` for enterprise or air-gapped evidence.
- Add a release note stating that report intake remains enabled by default and external reporting-provider configuration is unchanged.
- No compatibility/migration notice is needed for unshipped `SEED_MANIFEST_*` names.

### Operator Recovery

- Invalid explicit path: correct/mount the file and rerun the one-shot migration service or restart standalone.
- Invalid manifest: use `ValidateOnly`, correct all reported stable error codes, rerun; no partial tenant writes exist.
- Existing tenant skipped: use Admin UI/API or export/recreate a new tenant; Bootstrap never patches it.
- Failed transaction: inspect operation id, structured logs, and tenant result; fix the cause and rerun.
- Disable manifest processing: set mode `Off` or remove the convention file.

## 9. Islamic Value-Sensitive Design (I-VSD) & Moral Boundaries

The canonical analysis is [i-vsd-tenant-configuration-manifest.md](../../../islamic-value-sensitive-design/i-vsd-tenant-configuration-manifest.md).

Implementation must preserve:

- local-first report intake as the safe default;
- equivalent enforcement across every publication path;
- an independent correction/legal/copyright contact channel;
- no religious or legal overclaims in UI/docs;
- privacy-minimized manifest, export, audit, log, and metric data;
- operator autonomy without hiding responsibility or effective policy.

## 10. Security, Authorization, Privacy, And Abuse Considerations

- **Authorization — Applicable:** tenant-self export and policy mutation require tenant-setting permissions; arbitrary-tenant export requires Control Plane authorization and multi-tenant mode.
- **Tenant isolation — Applicable:** every query/result/repository operation carries explicit tenant identity; wrong-tenant export and bootstrap resolution fail closed.
- **Secrets — Applicable:** sensitive registry keys are rejected from v1 manifests and omitted from exports.
- **Filesystem trust — Applicable:** absolute bounded regular-file reads, symlink rejection, one-read parsing, non-root permissions, and safe diagnostics.
- **Abuse — Applicable:** direct report submission cannot bypass disabled intake, and unsafe disablement cannot bypass publication policy.
- **Privacy — Applicable:** audit stores digests, key names, statuses, and timestamps only; no manifest values, report evidence, credentials, or PII.
- **Rate limiting — Applicable:** authenticated export endpoints use the repository-standard admin/control-plane policies.
- **CSRF/BFF — Applicable:** Blazor calls use generated clients through BFF services; no browser token exposure or direct API bypass.

## 11. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

- **Multi-tenancy — Applicable:** manifests may contain multiple tenants; all items prevalidate before one atomic apply; existing tenants skip wholesale.
- **Federation — Applicable:** federated/import publication paths must be classified by the effective publication policy before intake can be disabled.
- **Localization — Applicable:** policy descriptions, failure messages, and export guidance are localizable; stable reason codes remain language-neutral.
- **Accessibility — Applicable:** native semantics, keyboard access, focus handling, live announcements, zoom/reflow, contrast, and RTL behavior follow WCAG 2.2 AA project rules.
- **White-label/self-hosting — Applicable:** no ISLAMU-instance-only assumption, external provider, or hosted control plane is required.
- **Paid plans/quotas — Not directly applicable:** the manifest does not alter plan entitlements; existing Control Plane locks/plan settings still constrain effective state.

## 12. Observability And Operations

Add structured logs and bounded metrics for:

- manifest discovery outcome;
- validation/apply duration;
- mode and apiVersion;
- digest prefix or operation id, never content;
- tenant counts by created/skipped/failed;
- stable failure category;
- rejected reporting-policy transitions.

Use OTEL/Prometheus/Loki conventions. Do not tag metrics with high-cardinality manifest paths, tenant slugs, raw errors, or values. Startup errors must identify the operation and stable code, while HTTP errors use RFC 7807 with trace/correlation metadata.

## 13. Migration And Compatibility Plan

- Add the new setting definition with default `true`; existing tenants inherit enabled behavior with no data backfill.
- Add manifest operation/result tables through generated provider migrations.
- No existing reporting-provider value changes meaning.
- No unshipped seed-manifest names or shapes are preserved.
- OpenAPI and generated client may break freely within the development `0.1` contract, but operation ids and HAL relations must be intentional and documented.
- `Down` migrations remove only new audit tables/indexes; tenant business configuration written by a manifest is not silently deleted by schema rollback.
- Managed reconciliation requires a future explicit migration/ownership plan and is not pre-seeded as dormant behavior.

## 14. Risk Register

| Risk | Severity | Mitigation | Owner/phase |
|---|---|---|---|
| An unclassified publication path bypasses approval | Blocker | Phase 1 caller inventory, regression tests, fail-closed evaluator | TCM-110 |
| Generic setting or plan application bypasses the invariant | Blocker | Complete proposed-state validator in every mutation path plus ratchet tests | TCM-120 |
| Manifest models leak into Domain or handlers depend on Persistence | Critical | Layer ownership and architecture tests | Phase 3 |
| Partial multi-tenant bootstrap | Critical | Full preflight and one execution-strategy transaction | Phase 4 |
| Replica race or API applies twice | Critical | One-shot migration owner; standalone pre-traffic ordering; idempotency | Phase 5 |
| Export exposes secrets or inherited state ambiguously | Critical | Catalog, omission metadata, explicit Overrides/Portable views | Phase 6 |
| Public schema URL drifts from checked artifact | Major | Deterministic generator, equality test, release-byte publication requirement | TCM-330 |
| Shared dirty `.env.example`, README, or intent edits are overwritten | Critical | Re-read before patching; preserve unrelated changes; ask on direct conflict | Every affected phase |
| 4 MiB limit is insufficient for unusually large bundles | Major | Stable oversize diagnostic; document scope; revisit only with measured need | Phase 3/operations |
| Operator mistakes Bootstrap for reconciliation | Major | Naming, skip result, docs, no `AlwaysOverride` enum/stub | Phase 5/7 |
| Correction channel becomes unreachable when reporting is disabled | Critical | Independent product/documentation acceptance criterion | Phase 1/7 |

## 15. Success Metrics And Definition Of Done

The workstream is complete when:

1. All publication paths obey the effective submission/approval policy.
2. Unsafe reporting-intake disablement fails through every settings mutation route.
3. External-provider configuration retains existing behavior.
4. Strict JSON manifests validate deterministically and schema drift fails CI.
5. Invalid multi-tenant manifests produce zero partial configuration writes.
6. Bootstrap creates absent tenants, skips existing tenants wholesale, and reruns idempotently.
7. Standalone and split migration-service topologies share the same post-migration contract.
8. Override and portable exports are tenant-safe, deterministic, authenticated, and secret-free.
9. HAL, API, generated client, BFF, and Blazor show the same effective capability.
10. Required build and one project test per phase pass without weakening tests.
11. Canonical configuration, secret, self-hosting, troubleshooting, API, schema, and I-VSD documentation agree.
12. No runtime code, docs, or release examples describe v1 bootstrap as continuous GitOps reconciliation.

## 16. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. Start/resume by reading `tenant-configuration-manifest-context.md` and the current unchecked task in `tenant-configuration-manifest-tasks.md`.
2. Retrieve only the plan section named by that task; do not reread the whole workstream on every step.
3. Update `tasks.md` immediately after a substantial task meets acceptance criteria; small related items may be reconciled together no later than phase end.
4. Run phase verification only after all implementation tasks in that phase, with one Release build and at most the one selected project test.
5. Mark a phase complete only when implementation and both verification checkboxes are complete.
6. Refresh `context.md` after a phase, meaningful decision, blocker, validation failure, scope discovery, or handoff.
7. Update `plan.md` only when scope, architecture, phase order, acceptance criteria, risk, or verification strategy changes.
8. Record deviations with rationale; never silently substitute a weaker design.
9. Preserve unrelated shared-workspace changes. Re-read dirty target files immediately before patching and ask one precise question if edits directly conflict.
10. Do not hand-edit EF migrations, model snapshots, generated API clients, or generated schema artifacts; fix source inputs and regenerate.
11. Do not weaken tests, ratchets, authorization, tenant filters, secret handling, or HAL gating.
12. Before handoff/PR, reconcile task status, add a dated context handoff, and name deferred work explicitly.

## 17. Progress Reporting Contract

Implementation reports must use:

```text
Phase: <name>
Task: <id and title>
Status: completed / blocked / in progress
Changed: <behavior and key files>
Evidence: <build and selected project test>
Risks: <new or changed>
Plan updated: yes/no with reason
Context updated: yes/no with reason
Tasks reconciled: yes/no
Docs updated: yes/no with reason
Next: <next unchecked task>
```

Planning status after this artifact set: **Draft; awaiting user review and approval.** Implementation has not started.

## 18. Potential Risks & Unknowns

The most likely failure is not JSON parsing; it is incomplete policy ownership. The repository has multiple settings mutation and event-publication paths, and a missed path would make the safety invariant misleading. Phase 1 therefore blocks every manifest or UI feature. The other material operational risk is shared-workspace overlap: `.env.example`, README, and `.agents/contract/intents.yaml` already contain unrelated modifications, so implementation must merge surgically rather than overwrite them. Public hosting for the canonical schema URI must serve the exact generated artifact before documentation presents it as resolvable; runtime validation does not depend on that network endpoint.

## 19. Paid Checkout Policy And Governance Integration Follow-up

This is an append-only follow-up discovered by registration-data-collection Phase 18C. It does not change the current manifest starting point (`TCM-110`) or weaken the Phase 1 publication/reporting-policy blocker.

### 19.1 Scope and authority boundary

- Integrate only non-secret tenant policy **narrowing** that the existing `PaidEventPolicyRules` can prove does not broaden instance authority.
- Keep instance operator identity, official status/origin, provider profile, provider credential ownership, charge type, liability allocation, and any future refund execution authority outside tenant-manifest ownership.
- Treat tenant/event stop-sale as persisted operational governance with reviewer/audit semantics, not as a browser-written setting or an unaudited manifest boolean.
- Do not place provider keys, connected-account IDs, buyer contacts, acceptance text, or reconciliation payloads in manifests, exports, audit payloads, logs, metrics, or traces.

### 19.2 Integration design

- Extend the manifest catalog with an explicit paid-checkout namespace only after Phase 1 caller inventory and Phase 3 schema/catalog foundations are complete.
- Parse manifest values into typed Application commands; reuse domain validation and policy composition rather than writing settings or payment tables directly.
- Apply paid-policy changes and any declared initial sale-control posture inside the existing transactional manifest operation, with tenant-qualified reads, serializable governance transitions where required, deterministic audit facts, and full rollback on failure.
- Preserve the Phase 18C authority chain: effective instance policy → valid tenant narrowing → persisted sale control/review → server-authored acceptance → freshness check → provider handoff.
- Expose effective and locked status through the existing generated API/BFF/HAL administration flow. Blazor must render mutation affordances only from HAL.
- Export only non-sensitive manifest-owned policy values. Portable export must identify flattened values and sovereign locks without claiming they are tenant-overridable.

### 19.3 Verification additions

- Add RED tests for attempted instance-policy broadening, operator/credential override, stop-sale bypass, cross-tenant application, stale revision, partial transaction, secret/PII export, and HAL/direct-authorization disagreement.
- Run real PostgreSQL collision tests for manifest apply versus stop/resume/review and provider handoff.
- Regenerate any affected schema, API inventory, OpenAPI, NSwag, and provider migrations from source.
- Require Phase 18C payment authority, freshness, mutation, and disclosure suites to remain green.

### 19.4 Dependencies and sequencing

- Registration-data-collection Phase 18C is complete and supplies the canonical paid-checkout authority model.
- Runtime implementation follows tenant-manifest Phases 1, 3, and 4; administration/export work follows Phases 6 and 7.
- Phase 19 refund work may add new policy facts later, but no manifest key may predeclare or imply refund execution, reserve ownership, liability, or consumer protection before that authority is implemented.

