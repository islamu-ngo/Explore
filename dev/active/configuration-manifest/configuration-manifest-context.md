<!-- ABOUTME: Resumable context for the instance-wide ConfigurationManifest rebase. -->
<!-- ABOUTME: Records verified tenant-only reality, approved architecture, risks, and the first implementation task. -->

# Configuration Manifest And Reporting-Intake Policy — Context

Last Updated: 2026-08-27 Europe/Brussels

## SESSION PROGRESS (2026-08-26 Europe/Brussels)

### COMPLETED

- Stopped runtime implementation at the user's direction.
- Loaded the `senior-cto-feedback`, `implementation-plan`, `i-vsd`,
  `grill-me`, `agentic-research`, `ip-clean-room`, and
  `clean-architecture-rules` workflows and their required review guidance.
- Audited the current branch implementation and confirmed it is tenant-focused:
  `TenantConfigurationManifestV1` uses `TenantConfigurationList` and only
  `spec.tenants`; the catalog, compiler, apply, audit-result, export, schema,
  startup, routes, BFF, UI, and generated contract are tenant-named.
- Verified the reusable foundations: strict bounded ingestion, explicit catalog,
  deterministic serializer/schema generator, compiler/preflight/apply split,
  atomic transaction, named locks, audit, outbox, post-migration startup owner,
  deterministic export, BFF boundary, HAL-only actions, and paid-policy
  narrowing.
- Verified missing instance foundations: no `spec.instance`, instance catalog,
  instance document catalog, complete instance-before-tenant compiler, instance
  result/bootstrap marker, or whole-instance export. The only approved
  v1alpha1 instance document candidate with an existing owner is the instance
  paid-event-policy aggregate; no generic instance document store is planned.
- Re-baselined the plan as one `ConfigurationManifest` containing a required
  instance section and one or more tenants.
- Added Phases 9–15 with strict Red/Green sequencing for breaking contract
  identity, instance authority/persistence, unified atomic apply, startup
  cutover, whole-instance export, BFF/UI, generated artifacts/docs, and
  criticality review.
- Selected a clean pre-v1 breaking cutover: no old DTO, kind, route, media type,
  environment key, path, schema, generated method, service, or migration alias.
- Updated the mapped I-VSD report for instance authority, tenant autonomy,
  privacy, payment boundaries, self-hosting, and operator recovery.
- Completed an independent Senior CTO review. Its initial “Approve with required
  changes” findings were incorporated: phase-local rename ratchets,
  post-Day-2 rerun authority, transaction-internal lock order, executable
  reporting safeguards, exact export authorization/size behavior, a concrete
  initial allowlist/document decision, aligned v1alpha1 identities, single
  migration ownership, and expanded operator recovery.
- Completed the final CTO re-review and resolved its three residual findings:
  CM-930 now moves only contract/catalog/serialization/schema surfaces;
  CM-1310’s obsolete-route assertions remain Red until CM-1330 deletion; and
  this context no longer proposes a generic instance document store.
- Completed CM-1040 with the sole typed
  `instance.paid_event_policy` document backed by the existing immutable
  aggregate. Public input carries policy intent but no revision authority.
  Initial preflight binds the active revision, lock-time preflight fences it,
  instance mutation occurs first, and tenant narrowing targets the resulting
  effective revision inside the caller-owned serializable transaction.
- Fixed every adversarial CM-1040 finding test-first: rejected broadening no
  longer mutates tracked aggregate state, null instance/tenant documents and
  null sovereign export metadata fail closed, and `ValidateOnly` does not drain
  pending outbox effects. The final transaction, contract, and QA MAD lanes all
  passed.
- Completed CM-1050 reporting accountability. Instance configuration cannot
  own the tenant reporting-intake switch, manifest changes use publication
  safety, and general POST/options/HAL agree with the switch. Correction,
  unsafe-link, and legal/copyright remedies remain available on distinct
  authenticated local routes backed by the canonical encrypted-evidence,
  case, serializable transaction, and outbox flow with no required provider.
- CM-1050 adversarial hardening rejects reserved-subcategory spoofing, masks
  privacy-erasure timing races, prevents reporting against non-public or
  transactionally stale event state, and makes duplicate identity
  subcategory-aware. Privacy/security, transaction, and HTTP/HAL MAD lanes all
  passed.
- Closed Phase 10 with a full Release solution build at 0 errors and
  4,457/4,457 Application tests passing through the built
  TUnit/Microsoft.Testing.Platform executable. The `dotnet test --project`
  wrapper stalled before spawning its test host, so zero-discovery and stalled
  wrapper runs were rejected as evidence rather than misreported as green.
- Began CM-1110 Red lifecycle specifications. Three initial tests now reproduce
  the missing bootstrap marker/generation, historical instance-policy
  reapplication on same-section rerun, and failure to reject a changed instance
  section.
- Completed CM-1110 Red specifications across compiler, preflight, and handler
  surfaces. The complete matrix pins proposed-instance-first and
  current-authority validation, deterministic lock-plan requirements,
  first/same/changed-section lifecycle, later tenant additions, wholesale
  existing-tenant skips, Day-2 revision binding, complete preflight,
  zero-partial-write behavior, exact authority revision use, and
  omission-as-no-reset.
- Intentional Red evidence is compiler 6/7, preflight 8/8, and handler 16/20.
  The five failures map only to the missing typed instance plan/lock identity,
  bootstrap marker/generation, same-section no-reapply behavior, later tenant
  additions, and changed-section rejection. CM-1110 changed no production code.
- Completed CM-1120 scope-aware compilation and preflight. The apply plan now
  has non-interchangeable instance/tenant types, canonical tenant-independent
  instance-section SHA-256 identity, instance-scoped changed facts, and
  deterministic instance-manifest/resource locks.
- CM-1120 preflight composes proposed instance publication state before tenant
  overrides, validates paid-policy narrowing against proposed or fresh current
  authority as appropriate, rejects changed bootstrap sections, fails closed
  on malformed persisted state, and strips all historical instance mutations
  on same-section Day-2 reruns. Compiler 8/8 and preflight 12/12 are green;
  four handler lifecycle failures remain intentionally assigned to CM-1140.
- Completed CM-1130/CM-1140 atomic apply. Real PostgreSQL tests first failed
  only at the missing hierarchy and instance persistence seams, then passed
  after the handler acquired ordered manifest, instance-resource, and
  tenant-resource session leases before opening the serializable retryable
  transaction.
- Bootstrap digest/generation and scope-qualified instance key-name facts now
  live on append-only operation evidence. Current-transaction instance,
  publication, tenant, branding, and paid-policy boundaries commit with the
  value-free outbox; failure evidence remains isolated until rollback.
- The unapplied audit heads were removed through EF tooling and regenerated as
  `AddConfigurationManifestAtomicBootstrapAudit` for PostgreSQL, SQLite, SQL
  Server, MariaDB, and MySQL. Provider parity 5/5 reports no pending changes;
  no generated artifact was hand-edited.
- Focused CM-1140 evidence is handler 22/22, instance mutation 9/9,
  publication policy 19/19, Domain audit 5/5, PostgreSQL hierarchy/state 2/2,
  audit persistence 5/5, atomic rollback 7/7, and competing writers 5/5.
- Completed CM-1210 startup-cutover Red specifications. The focused selector
  compiles and runs 4 tests without Docker, Aspire, browsers, services, sleeps,
  or polling. Existing post-migration/pre-traffic ordering passes; 3 failures
  map only to the missing canonical Infrastructure surface, old environment
  and convention-path constants, and legacy deployment artifact names.
- Completed CM-1220 startup/deployment cutover. Infrastructure concrete types,
  DI, logs, exception category, and post-migration sequence now use
  ConfigurationManifest naming; startup and result codes use the
  `configuration_manifest_*` prefix. MigrationService is the split owner,
  Standalone is the combined owner, and API replicas default to Off. Compose
  gates API traffic on successful one-shot completion and mounts the canonical
  file read-only; images retain non-root execution and ship the canonical
  v1alpha1 schema. The focused startup suite passes 37/37. A lean-host DI gap
  exposed by the new instance mutation boundary was fixed by registering
  `SettingUpsertService` plus the minimal existing MediatR dispatcher without
  loading the full runtime Application graph.
- Completed CM-1230 operator contracts across configuration, secrets,
  self-hosting, operations, troubleshooting, `.env.example`, and the bootstrap
  mount README. They now describe one instance-wide file, canonical keys and
  paths, no-secret/read-only/non-root handling, immutable instance-section
  digest semantics, fresh Day 2 authority on same-section reruns, wholesale
  tenant skips, and rollback-safe versus durable post-commit recovery. The
  recovery matrix covers every approved state/action case. Canonical schema
  drift check, `docker compose config --quiet`, link-target checks, legacy-term
  scans, and diff integrity pass.
- Phase 12 Release build passes with 0 warnings and 0 errors. The first run
  exposed four stale Standalone integration fixture references to the deleted
  startup interface; the fixture and probe were cut over to
  `IConfigurationManifestStartupRunner`, after which the no-restore rerun was
  clean.
- Phase 12 Infrastructure phase-exit verification passes 1554/1554. The
  startup cutover, host ownership/order, deployment artifact, file-boundary,
  durable deferred-effect composition, and all unrelated Infrastructure
  behavior remain green.
- Completed CM-1310 whole-instance export Red specifications. Both owning test
  projects compile. The focused Application class fails 5/5 on missing
  canonical query/serializer/contract/result and the dedicated all-active-
  tenant entity read. The focused API class fails 7/7 on the missing canonical
  controller/route, current 404 behavior, remaining tenant aliases, missing
  explicit shared export fact, and unavailable provider/overflow mappings.
  Tests cover deterministic single-/multi-tenant Overrides/Portable output,
  typed documents, sovereign omission, 4 MiB buffered preflight, instance
  authority, trusted context, tenant/wrong-instance denial, no-store,
  media/filename/operation ID, stable ProblemDetails, and provider parity
  without sleeps or polling.

### IN PROGRESS

- Phases 9–15 and CM-1530 product implementation are complete. The
  objective-level completion audit found two unwaived verification
  prerequisites that prevent literal closure.
  The unified contract, explicit instance-setting and paid-policy authority,
  current-transaction mutation seams, revision fencing, same-manifest
  narrowing, schema, reporting accountability, and adversarial
  null/state/privacy hardening are green. Scope-aware compilation and preflight
  are green. Serializable lock ordering, bootstrap lifecycle persistence,
  all-scope rollback, scope-safe audit, and durable effects are also green.

### NEXT

1. Repair or rebase the unrelated Persistence integration baseline, then pass
   the complete Phase 11 project gate.
2. Register and use Context7 MCP for the requested documentation research, or
   obtain an explicit waiver for that method-specific requirement.
3. Re-run the completion audit and close the persistent goal only after both
   unwaived requirements have evidence.

### BLOCKERS

- No ConfigurationManifest product defect or decision blocker remains.
- The required full `Event.Persistence.IntegrationTests` phase-exit project
  again hit a 1,200-second deadline with broad unrelated baseline failures. The
  focused real-provider ConfigurationManifest selectors remain green, but no
  approval exists to replace the full-project gate.
- Tavily MCP and Context7 MCP were explicitly requested, but neither tool is
  registered in this session. The available web search and web fetch tools were
  used against official sources; this limitation must not be represented as
  successful Tavily/Context7 usage.
- The repository knowledge-graph MCP described in `AGENTS.md` is also not
  registered. Read-only repository scouts and bounded direct evidence replaced
  it for this planning update.

## Quick Resume

- **Status:** Phases 9–14 complete.
- **Current phase:** Phase 15 — Generated Artifacts, Documentation, Cutover,
  And Review.
- **Current task:** `CM-1530`.
- **Read first:** this file, then CM-1530 in
  `configuration-manifest-tasks.md`, then plan Sections 13–14 and 17.
- **Hard gate:** do not mechanically rename. Instance authority, explicit
  catalogs, instance documents, instance-before-tenant validation, transaction
  order, export authorization, and bootstrap digest semantics are mandatory.
- **I-VSD:** [i-vsd-configuration-manifest.md](../../../islamic-value-sensitive-design/i-vsd-configuration-manifest.md)

## User-Approved Direction

- One file configures one instance.
- The same file supports single-tenant and multi-tenant deployments.
- The root name and public product contract are `ConfigurationManifest`.
- The file includes approved instance settings/documents and tenant
  settings/documents.
- Add implementation phases rather than continuing the tenant-only plan.
- Do not preserve backward compatibility; this is development mode.
- Do not include task time estimates.
- Update planning artifacts only for this request.

## Verified Current Implementation

| Path/symbol | Current responsibility | Rebase consequence |
|---|---|---|
| `TenantConfigurationManifestV1.cs` | Tenant-only envelope and kind | Replace with required instance + tenants root. |
| `TenantConfigurationManifestCatalog.cs` | Explicit tenant setting/document catalog | Split explicit instance and tenant authority catalogs. |
| `TenantConfigurationManifestValidator.cs` | Tenant structural/semantic policy validation | Add complete proposed instance state and wrong-scope rejection. |
| `TenantConfigurationManifestCompiler.cs` | Tenant plans only | Produce typed instance and tenant plans in authority order. |
| `ApplyTenantConfigurationManifestCommandHandler.cs` | Atomic absent-tenant bootstrap | Generalize to atomic instance plus absent tenants. |
| `ExportTenantConfigurationManifestQueryHandler.cs` | Tenant Overrides/Portable export | Replace with instance-admin whole-instance export. |
| `TenantConfigurationManifestStartupRunner.cs` | Tenant-manifest startup discovery/apply | Rename and preserve post-migration/pre-traffic ownership. |
| `SettingDefinition.cs` / `SettingRegistry.cs` | Scope, sensitivity, defaults, locks | Reuse metadata; never auto-expose instance settings. |
| `SettingUpsertService.cs` | Canonical instance scalar mutation | Add transaction-aware manifest entry points. |
| `TenantSettingsDocument.cs` / `SettingsDocumentTaxonomy.cs` | Tenant document ownership only | Do not generalize it. v1alpha1 reuses the existing instance paid-policy aggregate and rejects every generic instance document key/store. |
| `PaidEventPolicyMutationBoundary.cs` | Canonical instance and tenant policy revisions | Preserve Tier 0 authority and bind tenant narrowing internally. |

## Key Architecture Decisions

1. **One root contract:** `apiVersion`,
   `kind: ConfigurationManifest`, `metadata`, and `spec`.
2. **Required scope shape:** `spec.instance` and `spec.tenants`; tenants contain
   at least one item.
3. **One schema for all tenancy modes:** single-tenant mode uses the canonical
   default tenant; multi-tenant mode supplies multiple unique tenants.
4. **Explicit independent catalogs:** instance and tenant eligibility are
   separately admitted and tested.
5. **Typed instance documents:** v1alpha1 admits only
   `instance.paid_event_policy`, backed by the existing aggregate and mutation
   boundary. No generic instance document entity/repository is introduced;
   arbitrary JSON is forbidden.
6. **Complete-state compile:** derive proposed instance settings/documents/policy
   first, then validate every tenant against them before opening the transaction.
7. **Canonical boundaries:** manifest handlers never write setting, document,
   policy, sale-control, or audit tables directly.
8. **Atomicity:** acquire the instance-manifest lease, sorted instance-resource
   leases, and sorted tenant/resource leases before opening one serializable
   transaction and replaying preflight. PostgreSQL session leases and SQLite
   process leases prevent a wait from fixing a stale transaction snapshot; the
   transaction owns all writes, audit, and outbox.
9. **Bootstrap, not reconcile:** first instance section records an immutable
   normalized digest; same instance section reruns never reapply historical
   values and validate new tenants against fresh current Day 2 authority;
   changed instance section fails and directs the operator to Day 2
   administration.
10. **Tenant reruns:** absent tenants may be added only under the unchanged
    instance section; existing tenants remain wholesale skips.
11. **Whole-instance export:** only instance/Control Plane authority may export
    the canonical file at
    `GET /api/control-plane/configuration-manifest/export`; trusted server
    context resolves the current instance, the aggregate limit is 4 MiB, and
    tenant-shaped manifest export is removed.
12. **Secrets and sovereign state stay out:** credentials, secret bindings,
    topology, provider accounts, PII, payment operational state, liability,
    refund execution, handoff, and reconciliation are excluded.
13. **Clean break:** old names/contracts/artifacts are deleted, not aliased.
14. **Generated authority:** migrations/snapshots/schema/OpenAPI/API
    inventory/NSwag are regenerated from source.
15. **HAL/BFF:** UI action capability remains HAL-authored; browser tokens and
    privileged authority remain server-side.

## Contract Sketch

```json
{
  "$schema": "https://schemas.islamu.org/event/configuration-manifest/v1alpha1/schema.json",
  "apiVersion": "configuration.islamu.org/v1alpha1",
  "kind": "ConfigurationManifest",
  "metadata": {
    "name": "primary-instance"
  },
  "spec": {
    "instance": {
      "settings": {},
      "documents": {}
    },
    "tenants": [
      {
        "metadata": {
          "name": "default"
        },
        "spec": {
          "displayName": "Primary Community",
          "settings": {},
          "documents": {}
        }
      }
    ]
  }
}
```

## Canonical Cutover Names

- `ConfigurationManifest`
- `CONFIGURATION_MANIFEST_PATH`
- `CONFIGURATION_MANIFEST_MODE`
- `/etc/islamu-event/bootstrap/configuration-manifest.json`
- `schemas/configuration-manifest-v1alpha1.schema.json`
- `application/vnd.islamu.configuration-manifest.v1alpha1+json`

No tenant-manifest compatibility spelling survives implementation.

## Constraints And Rules To Remember

- Domain stays framework-free.
- Application owns contracts, validation, compilation, orchestration, DTO
  mapping, and mutation boundaries.
- Persistence returns entities and owns EF configuration/transactions.
- Infrastructure owns file I/O and startup options.
- API/HAL owns transport and action capability.
- Blazor uses generated-client adapters through the BFF.
- Validators are manually instantiated.
- Every file begins with two `ABOUTME` lines.
- Tenant context and instance authority fail closed.
- Secrets originate from Infisical or `.env`, never the manifest.
- Generated artifacts are never hand-edited.
- No compatibility shims, fixed sleeps, polling tests, suppressed diagnostics,
  or weakened ratchets.
- Planning-only changes do not run .NET build/test suites.

## Research And Provenance

### Repository evidence

- contract/catalog/validator/compiler/apply/export/startup symbols listed above;
- settings scope/sensitivity metadata and canonical mutation boundaries;
- tenant-only document taxonomy;
- existing paid-policy authority chain and sovereign exclusions;
- current tests under `Event.Application.UnitTests`,
  `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`,
  `Event.API.IntegrationTests`, `Explore.Blazor.IntegrationTests`,
  `Explore.Blazor.Client.Tests`, and `Event.Architecture.Tests`.

### Official source-free functional references

- Kubernetes objects:
  <https://kubernetes.io/docs/concepts/overview/working-with-objects/kubernetes-objects/>
  — explicit version/kind/metadata/spec conventions and strict unknown-field
  validation support a clear versioned envelope.
- Kubernetes declarative files:
  <https://kubernetes.io/docs/tasks/manage-kubernetes-objects/declarative-config/>
  — field ownership, mixed writers, deletion, and pruning are separate concerns;
  this plan intentionally does not claim them.
- .NET options:
  <https://learn.microsoft.com/en-us/dotnet/core/extensions/options>
  — validate startup options before dependent services operate.
- JSON Schema Draft 2020-12:
  <https://json-schema.org/draft/2020-12>
  — retain deterministic closed schema generation.
- Docker bind mounts:
  <https://docs.docker.com/engine/storage/bind-mounts/>
  — preserve a read-only host/file boundary.
- PostgreSQL transaction isolation:
  <https://www.postgresql.org/docs/current/transaction-iso.html>
  — real serializable concurrency tests must prove valid serial outcomes.
- PostgreSQL explicit locking:
  <https://www.postgresql.org/docs/current/explicit-locking.html>
  — canonical transaction-level lock identities must align with competing writers.
- ASP.NET Core OpenAPI metadata:
  <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/include-metadata?view=aspnetcore-10.0>
  — controller file downloads should declare `FileContentResult` plus the
  canonical media type so build-time OpenAPI emits `type: string`,
  `format: binary` rather than a JSON/base64 byte-array contract.
- ASP.NET Core OpenAPI generation:
  <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0>
  — retain the repository’s build-time document generation and transformer
  pipeline before deterministic inventory and NSwag regeneration.
- ASP.NET Core Blazor file downloads:
  <https://learn.microsoft.com/en-us/aspnet/core/blazor/file-downloads?view=aspnetcore-10.0>
  — use a same-origin URL download boundary, repeat security checks on the
  server, and keep the browser independent from privileged API destinations.

Only behavioral facts and standards constraints were retained. No external code,
schema expression, tests, prose, migrations, or assets were copied. No
dependency change is planned.

## Validation Baseline

This request changed planning Markdown only. Per repository Tier 4 scope:

- do not run `dotnet build`;
- do not run .NET test projects;
- run `git diff --check -- dev/active/configuration-manifest
  islamic-value-sensitive-design/i-vsd-configuration-manifest.md`;
- verify relative links and plan/context/tasks/I-VSD terminology manually;
- verify old planning artifacts are removed after the new triad is complete.

The prior runtime branch evidence remains historical context, not proof that the
new instance-wide work is implemented.

## Current Risks And Unknowns

- The initial v1alpha1 authority matrix is fixed in plan Section 3.2.1.
  CM-1010 must reconfirm every definition’s current metadata and remove any
  mismatched key rather than weakening the guard.
- The repository has no current generic instance document entity, and this plan
  intentionally does not add one. Only `instance.paid_event_policy` is admitted
  through its existing aggregate; all other instance document keys remain
  closed.
- The old manifest audit migration may have been exercised in development
  databases. The approved default is a development reset and regenerated
  unapplied migration; evidence of external persistent use would require
  stopping for a migration decision.
- Instance paid-event policy is Tier 0. Manifest ownership is allowed only for
  fields that survive the explicit authority review and canonical mutation
  boundary.
- Whole-instance export carries cross-tenant configuration and therefore cannot
  reuse tenant-self authorization.
- If Day 2 instance changes make a later tenant invalid, fresh current authority
  wins and the complete rerun fails without writes; historical manifest values
  are never used as a hidden validation ceiling.
- Managed reconciliation remains a separate workstream because ownership,
  deletion, takeover, and drift are not bootstrap semantics.
- Tavily, Context7, and code-review-graph tooling gaps reduce tool-specific
  evidence but do not change the source-grounded architecture decision.

## Handoff — 2026-08-26 Europe/Brussels

- **Outcome:** the tenant-only public contract is rejected as the final product
  architecture; its implementation remains a reusable foundation.
- **New plan:** Phases 9–15 implement one instance-wide
  `ConfigurationManifest`.
- **First task:** `CM-910` failing contract/naming tests.
- **No runtime edits:** this session changed planning/I-VSD artifacts only.
- **No compatibility:** old tenant-manifest public/runtime surfaces will be
  deleted during implementation.
- **No time estimates:** none are present in the revised artifacts.
- **Approval state:** Senior CTO review findings are fully incorporated and the
  plan verdict is “Approve with the required phases”; implementation remains
  paused until the user resumes/approves.

## SESSION PROGRESS (2026-08-27 Europe/Brussels)

### COMPLETED

- Phases 9–15 are implemented. The instance-wide `ConfigurationManifest` is the
  only manifest contract; no tenant-shaped manifest identity, route, schema, or
  alias remains anywhere under `.agents`, `.ci`, `deploy`, `docs`, `eng`,
  `islamic-value-sensitive-design`, `schemas`, `src`, or `tests`.
- Five provider migration families were deleted and regenerated with
  `dotnet ef`; no migration or model snapshot was hand-edited.
- Canonical generated artifacts (JSON Schema, OpenAPI, API contract inventory,
  NSwag client) were produced twice and compared byte-for-byte.
- An anonymized five-lane MAD review admitted eleven findings. Each was
  reproduced by a failing test and then fixed; the weighted post-hoc vote is
  unanimous `pass`. The record is
  `.omo/evidence/2026-08-27-configuration-manifest/mad-review.md`.
- The first valid Stryker pass established a 74.87% critical-handler baseline.
  Tests were hardened at public behavior seams, then Stryker 4.16.0 reran
  exactly `ApplyConfigurationManifestCommandHandler` and
  `ExportConfigurationManifestQueryHandler`: 176 mutants killed, 5 survived,
  no timeouts or errors, and a 94.12% score above the required 85% threshold.
  The report is
  `.omo/evidence/2026-08-27-configuration-manifest/mutation-critical-handlers-second/reports/mutation-report.json`.
- Final bUnit HTML embeds the production MudTheme palette and both the
  manifest section and nested action-button scoped styles. Chromium captures
  at 1440x1000 LTR and 390x844 RTL passed two independent fresh visual reviews
  with no product or evidence blocker.
- The whole Persistence integration project was explicitly abandoned after a
  bounded run timed out with 147 unrelated failures and no
  ConfigurationManifest-specific failure. Focused real-provider manifest
  gates remain green and are the accepted layer-bounded evidence.

### KEY DECISIONS

- Ordered manifest locks are acquired as session/process leases **before** the
  caller-owned serializable transaction opens, so a fresh preflight can never
  validate a snapshot taken before a later authority wait.
- The contract, generated schema, validator, and whole-instance export share a
  256-tenant ceiling. Export detects overflow from a bounded `limit + 1` query
  before reading any tenant configuration, while the aggregate 4 MiB file bound
  remains independently enforced.
- `ValidateOnly` is write-free: it returns an in-memory validation result and
  touches no transaction, lock, audit row, or outbox.
- Export authority scope is `InstanceAndTenants` everywhere; the obsolete
  narrowing scope is rejected by validation and pinned by a rejection test.
- `ApplyConfigurationManifestCommand` is classified `host-local-bootstrap` in
  the authorization guardrail ledger: it is dispatched only by
  `ConfigurationManifestStartupRunner`, never from an HTTP surface, and has no
  user principal to authorize.

### BASELINE FAILURES NOT OWNED BY THIS WORKSTREAM

- Architecture suite: skill-router size and `.claude/rules` routing both fail
  identically on mainline `develop`; the Phase 0 disposition artifact row count
  and the `StripeRefundAdapter` PII inventory entry are branch-behind-`develop`
  skews. None touches a ConfigurationManifest file.
