<!-- ABOUTME: Resumable context for tenant manifest bootstrap and reporting-intake policy work. -->
<!-- ABOUTME: Records verified current behavior, approved decisions, risks, validation, and the next implementation slice. -->

# Tenant Configuration Manifest And Reporting-Intake Policy — Context

Last Updated: 2026-08-21 Europe/Brussels

## SESSION PROGRESS (2026-08-21 Europe/Brussels)

### ✅ COMPLETED

- Completed repository-grounded Senior CTO review of the proposed seed-manifest and reporting-disablement plan.
- Verified that existing `Reporting:Mode`/`Enabled` settings control external providers while local canonical reporting remains required.
- Verified the current event settings, policy resolution, report options/submission/HAL, Control Plane setting/plan writes, tenant creation, startup migration ownership, standalone image, and test seams.
- Compared source-free functional behavior from official Keycloak, Grafana, JSON Schema, Kubernetes, Terraform, GitLab, and Docker documentation.
- Resolved the manifest lifecycle, schema, secret, export, startup, and standalone-container design.
- Created the required I-VSD report: [i-vsd-tenant-configuration-manifest.md](../../../islamic-value-sensitive-design/i-vsd-tenant-configuration-manifest.md).
- Created the synchronized implementation plan and task checklist.

### 🟡 IN PROGRESS

- Awaiting user review and approval of the planning workstream.

### ⏭️ NEXT

1. User reviews the decisions and phase ordering in plan Sections 3–6.
2. After approval, begin `TCM-110 — Enforce effective submission and approval policy`.
3. Do not implement manifest parsing, persistence, export, or UI before Phase 1 policy integrity passes.

### ⚠️ BLOCKERS

- Implementation is blocked on user approval of this draft plan.
- The shared workspace already contains unrelated modifications to `.agents/contract/intents.yaml`, `.env.example`, README, and many source/dev-doc files. This is not a planning blocker, but implementation agents must re-read and merge target files surgically; ask the user if a direct conflict cannot be resolved without overwriting unrelated work.

## Quick Resume

- **Status:** Draft planning complete; implementation not started.
- **Current phase:** Awaiting approval, then Phase 1 — Publication And Reporting Policy Integrity.
- **Current task:** `TCM-110`.
- **Read first:** this context file, then `TCM-110` in `tenant-configuration-manifest-tasks.md`, then plan Section 5.4 and Phase 1 only.
- **Hard gate:** No report-intake disablement or manifest feature ships until all effective event-publication paths enforce submission and approval policy.
- **I-VSD:** [islamic-value-sensitive-design/i-vsd-tenant-configuration-manifest.md](../../../islamic-value-sensitive-design/i-vsd-tenant-configuration-manifest.md)

## Key Files And Responsibilities

### Canonical Planning And Governance

| Path | Responsibility |
|---|---|
| `AGENTS.md` | Contribution contract and critical invariants |
| `.agents/contract/intents.yaml` | Intent scope, required docs/tests, forbidden operations |
| `.agents/skills/implementation-plan/` | Planning artifact and maintenance contract |
| `dev/active/tenant-configuration-manifest/tenant-configuration-manifest-plan.md` | Architecture, phase order, acceptance, risks |
| `dev/active/tenant-configuration-manifest/tenant-configuration-manifest-tasks.md` | Hot implementation ledger |
| `islamic-value-sensitive-design/i-vsd-tenant-configuration-manifest.md` | Provider-responsibility and moral boundary analysis |

### Settings And Policy

| Path | Verified responsibility |
|---|---|
| `src/Explore.Domain/Settings/SettingDefinition.cs` | Setting type/default/scope/lock/sensitivity metadata |
| `src/Explore.Domain/Settings/SettingRegistry.cs` | Immutable registry of code-defined setting definitions |
| `src/Explore.Domain/Settings/Definitions/EventSettingDefinitions.cs` | Tenant event submission and approval definitions |
| `src/Explore.Domain/Settings/Definitions/ReportingSettingDefinitions.cs` | Tenant external reporting-provider settings |
| `src/Explore.Application/Settings/Groups/EventSettingGroup.cs` | Effective typed event settings |
| `src/Explore.Application/Settings/Groups/ReportingSettingGroup.cs` | Effective typed external-provider settings |
| `src/Explore.Application/Services/Lifecycle/EventLifecyclePolicyProvider.cs` | Policy-aware event lifecycle validation |
| `src/Explore.Application/Services/TenantPolicySettingService.Apply.cs` | Tenant policy setting mutation path |
| `src/Explore.Application/Features/ControlPlane/Handlers/Commands/SetControlPlaneTenantSettingCommandHandler.cs` | Generic Control Plane setting write |
| `src/Explore.Application/Features/ControlPlane/Handlers/Commands/ApplyControlPlaneTenantPlanAssignmentCommandHandler.cs` | Bulk tenant-plan setting/quota apply |
| `src/Explore.Application/Features/EventReporting/Handlers/Commands/UpdateReportingRoutingSettingsCommandHandler.cs` | External reporting-provider tenant routing mutation |

### Reporting Runtime And HAL

| Path | Verified responsibility |
|---|---|
| `src/Explore.Infrastructure/Configuration/ModerationProviderOptions.cs` | Runtime external-provider mode; not report intake |
| `src/Explore.Infrastructure/Services/Moderation/ReportingRoutingPolicyResolver.cs` | External targets with mandatory local canonical handling |
| `src/Explore.Application/Features/EventReporting/Handlers/Queries/GetEventReportOptionsRequestHandler.cs` | Public event reportability/options |
| `src/Explore.Application/Features/EventReporting/Handlers/Commands/SubmitEventReportCommandHandler.cs` | Direct authenticated report submission |
| `src/Explore.API/Controllers/EventReportsController.cs` | Reporter-facing HTTP contract |
| `src/Explore.API/Hateoas/Policies/EventLinkPolicy.cs` | Event reporting/correction affordances |

### Bootstrap, Persistence, And Hosts

| Path | Verified responsibility |
|---|---|
| `src/Explore.Application/Features/Tenants/Handlers/Commands/CreateTenant/CreateTenantCommandHandler.cs` | Current atomic tenant creation behavior to extract/reuse without nested dispatch |
| `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs` | Shared migration and runtime seed entry point |
| `src/Explore.Persistence/Seed/DatabaseSeeder.cs` | Lookup/development seeding; not tenant manifest ingestion |
| `src/Event.Standalone/Program.cs` | In-process migration before standalone traffic |
| `src/Event.Standalone/Dockerfile` | Non-root standalone runtime and `/app/data` mutable state |
| `src/Event.MigrationService/Worker.cs` | Split topology one-shot migration owner |
| `docker-compose.yml` | Split service ordering and volume/env wiring |

### API And UI

| Path | Verified responsibility |
|---|---|
| `src/Explore.API/Controllers/ControlPlaneTenantConfigurationController.cs` | Arbitrary-tenant Control Plane configuration surface |
| `src/Explore.API/Hateoas/RouteNames.cs` | Stable route/operation identifiers |
| `src/Explore.Blazor.Client/Pages/Admin/Tenant/TenantAdminSettings.razor` | Current tenant admin settings entry |
| `src/Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantPoliciesSection.razor` | Existing event/organization policy controls |
| `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` | Generated API client; never hand-edit |

## Key Decisions

1. **Separate policy:** use `event_reporting.intake_enabled`, default `true`; do not alter external-provider semantics.
2. **Backend safety first:** effective publication enforcement across all actors/paths precedes intake disablement.
3. **Central invariant:** every policy-critical mutation path validates the complete proposed effective state.
4. **Boundary ownership:** manifest contracts/validators live in Application; file I/O lives in Infrastructure; persistence remains entity/repository based; hosts own ordering.
5. **Contract:** strict JSON with `$schema`, `apiVersion`, `kind`, metadata, and spec; flat canonical setting keys plus separate typed documents.
6. **Exposure:** an explicit Application catalog opts settings/documents into the manifest; registry membership alone is insufficient.
7. **No new runtime parser dependency:** `System.Text.Json`, manual validators, and a BCL-only deterministic schema generator.
8. **Lifecycle:** v1 implements `Off`, `ValidateOnly`, and whole-tenant `Bootstrap`; existing tenants skip wholesale.
9. **No reconciliation:** `AlwaysOverride`/managed drift is deferred to a separate approved workstream.
10. **Atomicity:** prevalidate the complete document and apply through one transaction; never dispatch existing commands sequentially.
11. **Secrets:** reject sensitive setting keys in v1; exports omit secret values and declare omissions.
12. **Audit:** persist manifest identity/version/digest/status/result/key names, never raw manifest/value content.
13. **Startup:** standalone and development migration owners run post-migration/pre-traffic; split production runs in `Event.MigrationService`, not every API replica.
14. **Container path:** `/etc/islamu-event/bootstrap/tenant-configuration.json`; `/app/data` remains mutable application data.
15. **Export:** Overrides by default; explicit Portable view flattens effective non-sensitive values.
16. **UI:** use existing tenant settings composition, generated client/BFF, HAL authorization, and server-authored disablement capability.
17. **Compatibility:** no `SEED_MANIFEST_*`, YAML, aliases, or unshipped compatibility shims.

## Constraints And Rules To Remember

- Every file needs two `ABOUTME` lines.
- Domain remains dependency-free; manifest format models do not belong there.
- Application handlers use repository contracts, manual validators, and cancellation tokens.
- Repositories return entities, not DTOs.
- Controllers remain thin and use route names, ProblemDetails metadata, rate limits, request timeouts, and immutable failure policies.
- HAL is the UI authorization source of truth.
- Blazor uses InteractiveAuto-compatible, BFF-safe, MudBlazor v9, accessible, RTL-safe patterns.
- Sensitive settings and secret values never appear in manifests, exports, logs, metrics, traces, audit payloads, or support artifacts.
- Generated EF migrations/snapshots, OpenAPI clients, and schema artifacts are regenerated from source; never hand-edited.
- Do not weaken tests, ratchets, tenant filters, locks, or authorization to make the work pass.
- Fixed sleeps and timing-based polling are forbidden in tests.
- Re-read dirty target files before editing and preserve unrelated changes.
- Run exactly one Release build and at most the selected project test at phase end.

## Validation Baseline

Planning-time baseline captured on 2026-08-21:

- `dotnet build --configuration Release --verbosity quiet` — passed with 0 errors.
- Pre-existing `SSH.NET` 2025.1.0 NU1903 high-severity advisory warnings remain unrelated.
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed; 421 succeeded and one documented API metadata test skipped.
- `git diff --check` passed for the newly created I-VSD report and plan at creation time.

Implementation phase commands are defined once in the plan/tasks and must not be run during planning.

## Current Known Risks / Unknowns

- The complete publication caller graph is the highest-risk implementation unknown; Phase 1 owns discovery and must update this context if another path is found.
- Public hosting for the versioned schema URI must serve the exact checked-in artifact before docs advertise it as resolvable.
- A fixed 4 MiB v1 file limit is an explicit operational boundary; measured enterprise demand may justify a later revision.
- Existing dirty changes in `.agents/contract/intents.yaml`, `.env.example`, and README may directly overlap later tasks.
- No production stakeholder/usability evidence exists for policy wording or export-mode terminology.
- Legal/copyright obligations vary by jurisdiction and remain outside software design authority.

## Handoff Notes

### Handoff — 2026-08-21 Europe/Brussels

- **Completed:** repository analysis, official functional research, CTO decisions, I-VSD report, plan, context, and executable task checklist.
- **Not started:** runtime implementation.
- **Next task:** `TCM-110`.
- **Blocking rule:** do not add manifest ingestion or report-disable UI before Phase 1 backend policy integrity passes.
- **Scope warning:** extend the intent contract before editing current out-of-scope host/schema paths.
- **Workspace warning:** preserve unrelated dirty files; no cleanup or reset is authorized.
- **Deferred:** YAML, secret references, and managed reconcile/field ownership.

