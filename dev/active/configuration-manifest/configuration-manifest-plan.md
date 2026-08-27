<!-- ABOUTME: Decision-complete implementation plan for one instance-wide ConfigurationManifest. -->
<!-- ABOUTME: Re-baselines the tenant-only foundation into a strict instance-and-tenant bootstrap contract. -->

# Configuration Manifest And Reporting-Intake Policy — Implementation Plan

Last Updated: 2026-08-26 Europe/Brussels

## 0. Planning Metadata

- **Request:** Replace the tenant-only manifest concept with one
  `ConfigurationManifest` per ISLAMU Event instance. The same strict file must
  configure approved instance settings/documents and one or more tenant
  settings/documents in single-tenant and multi-tenant deployments.
- **Task directory:** `dev/active/configuration-manifest/`
- **Planning status:** Re-baselined and ready for implementation approval. Runtime
  implementation is paused; this update changes planning artifacts only.
- **Current implementation reality:** the completed foundation is
  tenant-focused. Its envelope is `TenantConfigurationList`, its only root
  configuration collection is `spec.tenants`, and its catalog, compiler, apply,
  audit-result, export, route, schema, startup, and UI surfaces are tenant-named.
- **Primary matched intent:** `external-infrastructure-bootstrap`.
- **Criticality:** Tier 1 Security because the file crosses instance authority,
  tenant isolation, startup, filesystem, and persistence boundaries. Any
  instance paid-event policy work additionally retains Tier 0 Sovereign gates.
- **Additional matched intents:** `add-cqrs-handler`, `add-get-endpoint`,
  `add-hal-link`, `openapi-contract-change`, `blazor-component-affordance`, and
  `add-ef-migration`.
- **Intent gap:** the intent registry currently scopes tenant-prefixed feature,
  schema, and tooling paths. Phase 9 must replace those paths with the canonical
  `ConfigurationManifest` locations before product edits.
- **Relevant skills:** `implementation-plan`, `senior-cto-feedback`, `i-vsd`,
  `grill-me`, `agentic-research`, `ip-clean-room`,
  `criticality-guardrail`, `clean-architecture-rules`,
  `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`,
  `blazor-bff-patterns`, `blazor-ui-conventions`, `accessibility`,
  `error-tracking`, and `epistemic-mad-review`.
- **Primary layers:** Domain setting/document ownership; Application contract,
  catalog, validation, compilation, commands, queries, and mutation boundaries;
  Persistence audit/bootstrap state and generated provider migrations;
  Infrastructure ingestion/startup; API/HAL/OpenAPI; Blazor BFF/client; hosts,
  deployment configuration, generated artifacts, and operator documentation.
- **Compatibility position:** clean breaking replacement. The repository is
  pre-v1; no tenant-manifest DTO, kind, route, media type, environment key,
  schema, service alias, migration alias, or generated-client shim survives.
- **I-VSD:** [i-vsd-configuration-manifest.md](../../../islamic-value-sensitive-design/i-vsd-configuration-manifest.md)

## 1. Executive Summary

The current implementation is not a whole-instance configuration manifest. It
successfully provides strict tenant bootstrap, tenant-safe export, transactional
application, audit, startup ownership, and paid-policy narrowing, but it cannot
declare general instance settings or instance documents. The name
`TenantConfigurationManifest` therefore understates the intended product and
the contract shape prevents the intended use.

The corrected target is one strict JSON `ConfigurationManifest`:

- `spec.instance` declares only explicitly allowlisted, non-secret,
  deployment-safe instance settings and typed instance documents;
- `spec.tenants` declares one or more tenants and their allowlisted settings,
  typed documents, and valid tenant policy narrowing;
- the complete document is validated before writes;
- the resulting instance state is compiled first because it defines defaults,
  locks, and policy ceilings used to validate tenant sections;
- one transaction applies the instance bootstrap state, absent tenants, audit
  evidence, and durable post-commit effects;
- single-tenant mode uses the same contract with one default tenant;
- multi-tenant mode uses the same contract with multiple tenants;
- whole-instance export is instance-administrator-only and remains secret-free;
- Day 2 API/UI/database administration remains authoritative after bootstrap;
- managed reconciliation, field takeover, deletion, and pruning remain out of
  scope until a separate ownership design is approved.

This is a substantial contract and authority refactor, not a mechanical rename.
The strict reader, deterministic serializer/schema generator, transaction and
outbox foundation, startup sequencing, and HAL/BFF patterns remain reusable.

## 2. Source-Grounded Current State

### 2.1 Verified Evidence

The structural claims below were reconfirmed from the named branch paths on
2026-08-26. Representative test locations identify existing coverage, but no
.NET suite was rerun for this Markdown-only planning update; prior pass counts
remain historical context and are not claimed as fresh verification.

| Current fact | Evidence | Planning consequence |
|---|---|---|
| The root contract is tenant-only. | `src/Explore.Application/Features/TenantConfigurationManifest/Contracts/TenantConfigurationManifestV1.cs` | Replace it with `ConfigurationManifestV1Alpha1`; do not add a second manifest type beside it. |
| The public kind is `TenantConfigurationList`. | `src/Explore.Application/Features/TenantConfigurationManifest/Contracts/TenantConfigurationManifestV1.cs` — `TenantConfigurationManifestV1.Kind` | Replace it with `ConfigurationManifest`; no alias. |
| The catalog accepts only tenant-compatible entries. | `src/Explore.Application/Features/TenantConfigurationManifest/Catalog/TenantConfigurationManifestCatalog.cs`; representative coverage under `tests/Event.Application.UnitTests/Features/TenantConfigurationManifest/` | Add independent explicit instance and tenant catalogs with separate authority rules. |
| Compiler, validator, and apply plans contain tenant plans only. | `src/Explore.Application/Features/TenantConfigurationManifest/Validation/TenantConfigurationManifestValidator.cs`, `Compilation/TenantConfigurationManifestCompiler.cs`, `Application/ApplyTenantConfigurationManifestCommandHandler.cs` | Introduce a complete instance-and-tenant proposed-state compiler and ordered apply plan. |
| Export is tenant-scoped. | `src/Explore.Application/Features/TenantConfigurationManifest/Application/ExportTenantConfigurationManifestQueryHandler.cs`; representative export tests under `tests/Event.Application.UnitTests/Features/TenantConfigurationManifest/` | Replace tenant-self manifest export with instance-admin whole-instance export. |
| Startup, schema, options, routes, media type, and UI are tenant-named. | `src/Explore.Infrastructure/TenantConfigurationManifest/**`, schema generator, API/BFF/Blazor manifest surfaces | Rename the complete vertical slice and delete old public names. |
| Setting definitions already describe scope, sensitivity, defaults, locks, and coordinated mutation metadata. | `SettingDefinition.cs`, `SettingRegistry.cs` | Reuse definitions as metadata, but never auto-expose registry entries. |
| Canonical instance scalar writes already exist. | `src/Explore.Application/Settings/SettingUpsertService.cs` and coordinated mutation boundaries | Add transaction-aware manifest entry points rather than writing rows directly. |
| Settings documents are tenant-owned only. | `src/Explore.Domain/Settings/Documents/TenantSettingsDocument.cs`, `SettingsDocumentTaxonomy.cs`, `SettingsDocumentKeys.cs` | Do not invent a generic instance document store; v1alpha1 admits only the existing instance paid-policy aggregate as an instance document. |
| Paid policy already separates instance authority and tenant narrowing. | `src/Explore.Application/Features/PaidEventPolicies/PaidEventPolicyMutationBoundary.cs`, `src/Explore.Domain/Services/Registration/PaidEventPolicyRules.cs` | Preserve the authority chain and bind tenant narrowing to the manifest-compiled current instance revision. |
| Strict parsing, digesting, schema drift, atomicity, audit, outbox, and startup ordering are implemented. | Existing manifest Application, Infrastructure, Persistence, host, and test slices | Generalize these foundations instead of replacing their behavior. |

### 2.2 What Is Reusable

- bounded one-read JSON ingestion, strict UTF-8 and duplicate-member rejection;
- source-generated serialization and manual Application validation;
- explicit allowlist catalog pattern;
- deterministic Draft 2020-12 schema generation and byte-drift gate;
- compiler/preflight/apply separation;
- canonical named locks, transaction boundary, safe audit, and transactional
  outbox;
- post-migration/pre-traffic startup ownership in Standalone and
  MigrationService;
- deterministic export serialization and bounded BFF download;
- HAL-only UI action gating;
- tenant paid-policy narrowing and sovereign-field exclusions.

### 2.3 Missing Capabilities

1. No `spec.instance`.
2. No explicit instance-safe setting catalog.
3. No instance document catalog; the only approved v1alpha1 candidate with an
   existing owner is the instance paid-event-policy aggregate.
4. No complete proposed-state model that validates tenant values against the
   instance values declared in the same file.
5. No transaction-aware instance setting/paid-policy manifest boundary.
6. No instance result or scope-qualified audit facts.
7. No instance-admin whole-instance export contract.
8. No instance-level HAL/BFF/UI authority model.
9. No unified schema, media type, environment keys, startup path, generated
   client, or operator vocabulary.

### 2.4 Current Strengths To Preserve

- tenant isolation is explicit and fail-closed;
- secrets and PII are excluded from manifests, exports, audit, and telemetry;
- payment provider identity, credentials, sale control, refund execution,
  liability, acceptance, and reconciliation remain sovereign;
- manifest application is atomic and effects are deferred until commit;
- runtime API/UI administration does not pretend bootstrap is reconciliation.

## 3. Proposed Future State

### 3.1 Canonical Envelope

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

The envelope is one instance configuration artifact. `metadata.name` is an
operator-facing stable manifest identity, not a tenant identifier and not a
secret. The instance object is required, even when its two closed maps are
empty. The tenant array is required and contains at least one tenant.

### 3.1.1 Contract Table

| Member | Contract |
|---|---|
| `$schema` | Required exact URI ending in `/v1alpha1/schema.json`. |
| `apiVersion` | Required exact value `configuration.islamu.org/v1alpha1`. |
| `kind` | Required exact value `ConfigurationManifest`. |
| `metadata` | Required closed object. |
| `metadata.name` | Required lowercase DNS label, 1–63 characters; operator identity only. |
| `spec` | Required closed object. |
| `spec.instance` | Required closed object with required `settings` and `documents` maps. |
| `spec.instance.settings` | Closed explicit catalog; values retain canonical Domain JSON types. |
| `spec.instance.documents` | Closed catalog; v1alpha1 initially admits only `instance.paid_event_policy`. |
| `spec.tenants` | Required array with 1–256 entries, additionally bounded by the 4 MiB file limit. |
| tenant `metadata.name` | Required canonical slug, lowercase DNS label, 1–63 characters, unique by ordinal comparison. |
| tenant `spec.displayName` | Required nonblank string, maximum 200 Unicode scalar values. |
| tenant `spec.settings` / `documents` | Required closed catalog maps with strict canonical types. |
| unknown/duplicate members | Rejected at every depth before semantic validation. |

Schema, media type, and API version all use `v1alpha1`; no ambiguous `v1`
alias exists.

### 3.2 Scope And Authority Model

- **Instance settings:** only entries explicitly admitted by the instance
  catalog. Eligibility requires database-backed runtime ownership, a canonical
  transaction-aware mutation path, no secret value, and no deployment/runtime
  topology ambiguity.
- **Instance documents:** v1alpha1 initially admits only
  `instance.paid_event_policy`, backed by the existing paid-policy aggregate and
  canonical mutation boundary. No generic `InstanceSettingsDocument` entity is
  added. Other instance document identities remain closed until a concrete
  Domain owner, validator, persistence model, and safe export descriptor are
  separately approved. Arbitrary JSON is forbidden.
- **Tenant settings/documents:** preserve the explicit tenant catalog and
  tenant-isolation rules.
- **Instance paid-event policy:** may be included only through the canonical
  Tier 0 mutation boundary after an explicit field-by-field authority review.
- **Tenant paid-event policy:** remains a narrowing of the effective instance
  policy. The compiler binds it to the instance revision selected or created by
  the same apply plan; callers cannot supply a cross-instance revision.
- **Never manifest-owned:** credentials, secret references, connection strings,
  signing/encryption material, provider accounts, operator identity, official
  status/origin, buyer/acceptance PII, sale-control/review/handoff state,
  liability, disputes, negative balances, refund execution, reconciliation,
  infrastructure topology, and deployment-managed secret bindings.

### 3.2.1 Initial Instance Authority Matrix

| Decision | Exact keys / group | Reason |
|---|---|---|
| Admit scalar instance defaults | `branding.display_name`, `branding.logo_url`, `branding.favicon_url`, `branding.custom_css_url` | Portable non-secret brand defaults with existing hierarchical ownership. |
| Admit tenant-governance defaults | `tenants.self_service_registration`, `tenants.white_labeling_enabled`, `organizations.tenant_can_omit_verification` | Explicit instance governance already represented as settings. |
| Admit module defaults | `modules.islamic_enabled`, `modules.tech_enabled` | Non-secret product defaults; tenant narrowing remains explicit. |
| Admit coordinated publication defaults | `events.user_submission_enabled`, `events.organization_submission_enabled`, `events.group_submission_enabled`, `events.require_approval` | Only through the canonical publication-policy mutation boundary. |
| Admit organization/group defaults | `organizations.verification_required`, `organizations.self_registration_enabled`, `groups.self_registration_enabled` | Non-secret policy defaults with established hierarchical ownership. |
| Admit presentation defaults | `routing.default_public_home_page`, `appearance.default_theme_mode`, `public_experience.mode`, `public_experience.event_catalog_label` | Portable non-secret defaults; reference-bearing IDs and free-form JSON remain excluded. |
| Admit footer governance locks | `footer.lock_tenant_template`, `footer.lock_tenant_link_groups`, `footer.lock_tenant_social_links`, `footer.lock_tenant_description`, `footer.lock_tenant_copyright` | Existing instance-only tenant-governance flags. |
| Admit typed instance document | `instance.paid_event_policy` | Existing Tier 0 aggregate/boundary; field-level sovereign exclusions still apply. |
| Keep tenant-only | `event_reporting.intake_enabled`, tenant branding, `tenant.paid_event_policy`, tenant default organization/group IDs | Reporting disablement and tenant-owned values require tenant-specific authority and safety checks. |
| Exclude v1alpha1 | deployment, routing resolver/topology, domains, security, Cerbos, email, storage, integrations, webhooks, AI/MCP governance, support access, secret/provider fields, reference-bearing preset/organization IDs, free-form JSON settings | Deployment/security/secret/external-resource or non-portable authority is not bootstrap business configuration. |

CM-1010 must verify every listed definition still has the expected scope,
sensitivity, mutation boundary, and exportability before coding the catalog.
Any mismatch removes the key from v1alpha1; it never weakens the guard.

### 3.3 Compile And Apply Flow

```text
one bounded file read
  -> strict envelope/schema/duplicate validation
  -> explicit instance and tenant catalog lookup
  -> compile complete proposed instance state
  -> compile every tenant against that proposed instance state
  -> acquire canonical instance-manifest lease
  -> acquire sorted canonical instance resource leases
  -> acquire sorted canonical tenant/resource leases
  -> begin one serializable transaction while those leases remain held
  -> replay all freshness, authority, uniqueness, and current-state checks
  -> inside that same transaction:
       apply instance settings/documents/policy through canonical boundaries
       create/configure absent tenants through canonical boundaries
       persist scope-qualified operation/results and outbox effects
  -> commit
  -> dispatch cache invalidation/notifications
```

No handler writes settings, documents, or payment/governance tables directly.
Application orchestrates; Domain owns invariants; Persistence implements entity
storage; Infrastructure owns file I/O; hosts own startup ordering.

### 3.4 Bootstrap Lifecycle

Supported modes remain:

| Mode | Behavior |
|---|---|
| `Off` | Do not discover, validate, or apply a file. |
| `ValidateOnly` | Validate the complete instance-and-tenant document and write nothing. |
| `Bootstrap` | Apply the instance bootstrap section and create absent tenants atomically. |

Bootstrap is not continuous desired-state reconciliation:

- the first successful instance section records an immutable normalized
  instance-section digest and bootstrap generation;
- rerunning the same instance section is idempotent;
- a changed instance section after successful bootstrap fails with a stable
  `configuration_manifest_instance_already_bootstrapped` result and directs the
  operator to Day 2 administration;
- absent tenants in a later file may be created only when the instance section
  still matches the recorded digest;
- existing tenants remain whole-tenant skips;
- no omitted field is deleted, reset, or taken over;
- `Reconcile`, prune, takeover, and field ownership remain separate future work.

#### Bootstrap State Matrix

| Situation | Authoritative instance state for tenant validation | Outcome |
|---|---|---|
| First successful bootstrap | Complete proposed instance state compiled from current state plus the manifest section | Apply approved instance state, then tenants, atomically; record normalized section digest and resulting resource revisions. |
| Same section, no Day 2 changes | Fresh current effective instance state, which must match recorded post-bootstrap revisions | Instance no-op; create absent tenants; skip existing tenants. |
| Same section after Day 2 instance changes | Fresh current effective instance state and active policy revisions, never historical manifest values | Do not reapply instance values. Validate new tenants against current authority; succeed only if valid and record the exact current revisions used. |
| Changed instance section after bootstrap | Current state is authoritative | Fail `configuration_manifest_instance_already_bootstrapped`; no instance or tenant write. |
| Current instance policy changes while applying | Lock-time revision reread | Serialize before/after the competing writer or fail stale; never validate against an obsolete revision. |
| Current state no longer permits a new tenant section | Fresh current state | Fail complete preflight with a stable reason; no partial write. |

Canonical mutation boundaries participate in the outer transaction and declared
lock hierarchy through in-transaction methods; they must not reacquire a
lower-order lock or start a nested transaction.

### 3.5 Export Model

The canonical export is a whole-instance artifact:

- only an instance administrator or equivalent Control Plane authority may
  export it;
- the route is
  `GET /api/control-plane/configuration-manifest/export?view=Overrides|Portable`
  with operation ID `ExportConfigurationManifest`;
- there is no caller-supplied instance identifier. The API resolves the one
  current deployment instance from trusted server context;
- the Application query is an `IAuthorizedRequest` over the current
  `InstanceSettings` resource with `View` action, plus explicit
  configuration-manifest export facts used identically by Cerbos and local
  authorization;
- unauthenticated callers receive 401, denied callers receive 403, and an
  unavailable configured Cerbos provider fails closed through the repository
  service-unavailable ProblemDetails policy;
- cross-tenant reads use a dedicated entity-returning repository operation whose
  filter bypass is named for instance-authorized manifest export and constrained
  to active tenants in the current instance;
- single-tenant and multi-tenant installations use the same endpoint and file;
- `Overrides` emits declared instance overrides and tenant-owned overrides;
- `Portable` emits approved effective non-sensitive values with explicit
  flattening and sovereign-omission metadata;
- tenant administrators no longer receive a manifest-shaped partial export;
- current tenant-self manifest routes, HAL relations, BFF methods, and UI
  controls are deleted rather than aliased;
- exports remain configuration artifacts, not backups or secret bundles;
- exported bytes are generated incrementally but the aggregate hard limit
  remains 4 MiB so every successful export is accepted by the import boundary;
  tenant enumeration is bounded at 256 and overflow is rejected before any
  per-tenant configuration read. Overflow fails before response bytes are sent with stable
  `configuration_manifest_export_too_large` ProblemDetails.

## 4. Non-Negotiable Constraints

1. One canonical `ConfigurationManifest` contract; no parallel
   `TenantConfigurationManifest` compatibility surface.
2. One file may represent exactly one instance and one or more tenants.
3. Single-tenant and multi-tenant deployments use the same schema and pipeline.
4. All instance and tenant entries are explicit allowlists; registry membership
   never creates manifest exposure.
5. Complete-document validation occurs before writes.
6. Proposed instance state is compiled before tenant validation.
7. Instance locks are acquired before tenant locks using deterministic ordering.
8. One transaction owns instance changes, tenant creation/configuration, audit,
   and outbox.
9. Canonical mutation boundaries retain Domain validation, authorization facts,
   lock/freshness semantics, and post-commit effects.
10. Tenant-originated fields can never select instance keys or instance
    mutation APIs.
11. No secrets, PII, credentials, provider state, operational payment state, or
    topology ownership enters manifests, exports, audit, logs, metrics, traces,
    or ProblemDetails.
12. HAL is the sole UI action-affordance authority.
13. Validators remain manually instantiated.
14. Repositories return entities, never DTOs.
15. EF migrations/snapshots, OpenAPI, NSwag, and JSON Schema are generated from
    source and never hand-edited.
16. No YAML, directory composition, remote URL ingestion, compatibility alias,
    or managed reconcile mode.
17. Every new file begins with two `ABOUTME:` lines.

## 5. Architecture And Design Decisions

### 5.1 Decision Matrix

| Decision | Selected | Rejected | Rationale |
|---|---|---|---|
| Product contract | One `ConfigurationManifest` with instance and tenants | Separate instance and tenant manifest families | One deployment artifact matches the product concept and avoids ambiguous composition. |
| Contract migration | Replace old kind, DTOs, schema, routes, env keys, and media types | Preserve aliases | Pre-v1 development permits a clean contract; duplicate semantics would become permanent debt. |
| Scope catalogs | Separate explicit instance and tenant catalogs under one feature | Auto-expose by registry scope | Instance eligibility requires stricter authority and operational classification. |
| Instance documents | `instance.paid_event_policy` only in v1alpha1, backed by its existing aggregate | Generic JSON document entity or reuse tenant rows | Do not create speculative persistence; every document needs a real authority owner. |
| Apply ordering | Compile instance, validate tenants against it, then one atomic apply | Tenant-first or independent transactions | Instance defaults, locks, and ceilings constrain tenant validity. |
| Bootstrap changes | Immutable instance-section digest after first success | Restart-time overwrite | Avoids hidden reconciliation and Day 2 changes being reverted. |
| Tenant reruns | Create absent, skip existing wholesale | Patch existing tenants | Preserves explicit Day 0 versus Day 2 ownership. |
| Export authority | Instance-admin whole-instance export | Tenant-shaped partial manifests | A canonical instance file must not leak other tenants or imply partial files are deployable roots. |
| Paid policy | Canonical instance revision then tenant narrowing | Direct setting/table writes | Preserves Tier 0 authority, freshness, and sovereign exclusions. |
| Effects | Transactional outbox and post-commit cache/notification dispatch | External effects inside transaction | Keeps rollback truthful and recovery idempotent. |

### 5.2 Clean Architecture Ownership

- **Domain:** setting definitions; existing tenant document ownership;
  paid-policy and other true domain invariants; audit/bootstrap state entities.
- **Application:** manifest contracts, catalogs, validators, compiler, apply
  plan, CQRS requests/handlers, scope-qualified DTO mapping, authorization facts,
  canonical transaction-aware mutation boundaries, deterministic serializer.
- **Persistence:** EF configuration, entity repositories, transaction execution,
  indexes/constraints, generated provider migrations.
- **Infrastructure:** bounded local-file reading, lexical strictness, options,
  digesting, startup runner, safe logs/metrics.
- **API/HAL:** instance-admin transport, ProblemDetails, route names, OpenAPI,
  HAL capability emission.
- **Blazor BFF/client:** token-safe download adapter and HAL-driven
  instance-administration UI; no local authorization calculation.
- **Hosts/deployment:** post-migration/pre-traffic ownership and read-only file
  mounting.

### 5.3 Concurrency And Atomicity

- use serializable execution for the whole apply;
- acquire one canonical instance-manifest lease, then sorted canonical instance
  setting and policy leases, then sorted tenant/resource leases before opening
  the serializable transaction; PostgreSQL uses session leases and SQLite uses
  process leases so waiting cannot fix a stale transactional snapshot;
- re-read the bootstrap marker, active instance policy, setting locks, tenant
  existence, and applicable resource revisions inside the fresh transaction
  while every ordered lease remains held;
- prove both valid serial orders against competing instance setting, instance
  paid-policy, tenant creation, branding, and tenant setting writers;
- on any stale revision or conflict, roll back instance, tenants, audit success,
  and effects together, then record only privacy-minimized failure evidence when
  the database remains available.

### 5.4 Authorization And Trust

- file application remains a trusted host bootstrap operation, never an HTTP
  request body or browser-supplied path;
- instance export requires instance-level resource authorization with Cerbos and
  local-provider parity;
- tenant administrators retain Day 2 tenant settings APIs but cannot obtain the
  whole-instance manifest;
- BFF tokens remain server-side and the browser receives only the bounded
  download;
- explicit wrong-instance, wrong-tenant, missing-context, and locked-authority
  tests fail closed.

## 6. Implementation Phases

Phases 1–8 of the former tenant-focused workstream produced the reusable
foundation. The corrected implementation resumes at Phase 9. Granular Red/Green
tasks live only in `configuration-manifest-tasks.md`.

### Phase 9 — Breaking Contract And Namespace Rebase

**Goal:** Establish the sole public identity and closed instance-and-tenant
envelope before adding new writes.

**Primary paths:** intent registry; Application manifest contracts/catalog
metadata; JSON context; schema generator; schema artifact; architecture and
contract tests.

**Exit criteria:** tests first prove the new kind/root shape and absence of old
Application contract/schema identities; `ConfigurationManifest` types and paths
compile; the old kind and schema have no alias; deterministic schema drift
passes. Environment/path ratchets belong to Phase 12,
API/media/HAL/generated-contract ratchets to Phase 13, BFF/UI ratchets to Phase
14, and the repository-wide zero-old-runtime-name ratchet to Phase 15.

### Phase 10 — Instance Configuration Authority And Canonical Mutation

**Goal:** Add the exact initial instance-safe scalar catalog, the typed
`instance.paid_event_policy` document strategy, and canonical
transaction-aware mutation seams without a generic instance document store.

**Primary paths:** Domain setting and paid-policy ownership; Application instance
catalog and mutation boundaries; existing paid-policy persistence; focused
Domain/Application tests.

**Exit criteria:** every v1alpha1 field has an authority decision;
sensitive/topology/sovereign fields fail closed; approved instance settings and
the paid-policy document use canonical boundaries; tenant inputs cannot reach
instance storage; reporting-intake/publication/correction safeguards remain
green.

### Phase 11 — Unified Compiler, Atomic Apply, Audit, And Concurrency

**Goal:** Compile and apply one complete proposed instance-and-tenant state in
one serializable transaction.

**Primary paths:** Application validator/compiler/preflight/apply plan and
handler; audit/bootstrap state; Persistence transaction/locks/outbox; real
PostgreSQL tests.

**Exit criteria:** instance-before-tenant validation is proven; same-digest
reruns are idempotent; changed post-bootstrap instance sections fail safely;
absent tenants may be added only under the recorded instance section; all
conflicts roll back every scope; effects are durable and post-commit.

### Phase 12 — Startup, Deployment, And Recovery Rename

**Goal:** Make every supported topology consume the canonical file and naming.

**Primary paths:** Infrastructure reader/options/runner; Standalone;
MigrationService; development migration owner; Aspire/Compose; Dockerfiles;
`.env.example`; operator docs and startup tests.

**Exit criteria:** only `CONFIGURATION_MANIFEST_PATH`,
`CONFIGURATION_MANIFEST_MODE`, and
`/etc/islamu-event/bootstrap/configuration-manifest.json` remain; one owner
applies before traffic; read-only mount, non-root permissions, failure exit, and
recovery behavior are documented and tested without live Docker/browser work.

### Phase 13 — Whole-Instance Export, API, HAL, And Generated Contract

**Goal:** Replace tenant-shaped export with one authorized instance export.

**Primary paths:** Application export query/serializer; API controller, route
names, HAL policies, OpenAPI transformers/catalog; API inventory; NSwag;
authorization providers and API tests.

**Exit criteria:** instance-admin export emits one deterministic secret-free
file for single- or multi-tenant mode; unauthorized and tenant-only callers fail;
the canonical route/resource/failure/4 MiB contract is covered; tenant manifest
export routes/relations are deleted before OpenAPI/client regeneration; OpenAPI,
API inventory, and generated client contain only canonical names.

### Phase 14 — BFF And Administration Cutover

**Goal:** Expose the whole-instance export safely through the existing
instance/Control Plane administration surface.

**Primary paths:** Blazor BFF service/endpoint; generated client adapter;
instance administration components; localization; scoped CSS; BFF/client tests.

**Exit criteria:** the browser never receives tokens or raw API authority;
download remains bounded; instance-admin affordances are HAL-only; tenant
settings pages no longer advertise tenant manifest exports; focus,
localization, RTL, and WCAG 2.2 AA behavior remain covered.

### Phase 15 — Generated Artifacts, Documentation, Cutover, And Review

**Goal:** Remove stale tenant-manifest artifacts and prove the full authority,
privacy, migration, and operational contract.

**Primary paths:** generated JSON Schema/OpenAPI/NSwag/API inventory/provider
migrations; configuration, secrets, self-hosting, operations, security,
payments, troubleshooting, API changelog, release fragment, I-VSD, and evidence
artifacts.

**Exit criteria:** the repository-wide zero-old-runtime-name ratchet passes; no
tracked runtime/documentation contract advertises the old name; generated
artifacts are stable on a second run; reset/cutover guidance is explicit; Tier 1
and Tier 0 evidence gates pass; anonymized MAD review has no surviving blocker;
all triad artifacts are reconciled.

## 7. Testing Strategy

### Test-First Invariant Order

Every behavioral task follows:

1. author failing public-contract or invariant-breaker tests;
2. observe the expected failure;
3. implement the smallest owning-layer change;
4. run the focused TUnit selector;
5. refactor without changing the public invariant;
6. run one Release build and at most one selected project test at phase exit.

Required adversarial coverage includes:

- tenant payload attempts to select instance keys or documents;
- unknown, sensitive, secret, topology, provider, PII, and sovereign fields;
- duplicate keys, wrong kind/version, oversize input, symlink, and unsafe path;
- instance setting/document/policy writers racing manifest apply;
- tenant creation/settings/branding/policy writers racing the same apply;
- instance section changed after bootstrap;
- partial instance success followed by tenant failure;
- stale instance paid-policy revision and tenant broadening;
- wrong-tenant and non-instance-admin export;
- HAL/direct authorization disagreement;
- secret/PII scans across export, audit, ProblemDetails, logs, metrics, traces,
  and evidence.

### Phase Exit Projects

| Phase | One selected project |
|---|---|
| 9 | `Event.Architecture.Tests` |
| 10 | `Event.Application.UnitTests` |
| 11 | `Event.Persistence.IntegrationTests` |
| 12 | `Explore.Infrastructure.Tests` |
| 13 | `Event.API.IntegrationTests` |
| 14 | `Explore.Blazor.Client.Tests` |
| 15 | `Event.Architecture.Tests` |

Each phase also runs one canonical Release build. BFF and other touched-layer
tests use focused TUnit selectors during their owning task; full unrelated
projects are not added to phase exits.

## 8. Documentation, Configuration, And Operations

Canonical operator contract after cutover:

- `CONFIGURATION_MANIFEST_PATH`
- `CONFIGURATION_MANIFEST_MODE`
- `/etc/islamu-event/bootstrap/configuration-manifest.json`
- `schemas/configuration-manifest-v1alpha1.schema.json`
- `application/vnd.islamu.configuration-manifest.v1alpha1+json`

Update:

- `.env.example`
- `docs/CONFIGURATION.md`
- `docs/SECRETS.md`
- `docs/SELF_HOSTING.md`
- `docs/OPERATIONS.md`
- `docs/SECURITY-MODEL.md`
- `docs/PAYMENTS.md`
- `docs/TROUBLESHOOTING.md`
- `docs/API_CHANGELOG.md`
- release/schema indexes and deployment examples

Documentation must distinguish bootstrap from reconciliation, explain one-file
single-/multi-tenant behavior, list excluded authority, teach same-digest reruns
and changed-instance failure recovery, and state that export is not backup.

## 9. Security, Authorization, Privacy, And Abuse

- **Filesystem:** absolute bounded regular local file, symlink rejection,
  one-read digest, non-root read-only mount, no remote URL or directory merge.
- **Secrets:** no raw or indirect secret bindings in the contract. Secrets
  remain Infisical or `.env` sourced and deployment-owned.
- **Tenant isolation:** tenant identity is explicit in every compile/apply/export
  path; tenant payload cannot select scope; instance operations cannot leak one
  tenant through another result.
- **Authorization:** startup apply is host-owned; export is instance-authorized;
  HAL mirrors server authorization; Cerbos/local parity is mandatory.
- **Privacy:** audit and telemetry contain bounded identifiers, digest/status,
  counts, and key codes only.
- **Abuse:** no browser-controlled bootstrap path, instance key, tenant list, or
  privileged downstream header is trusted.
- **Payments:** provider credentials, operational governance, handoff,
  reconciliation, acceptance, liability, and refund execution remain outside
  manifest authority.

## 10. Single-Tenancy, Multi-Tenancy, Federation, Localization, Accessibility

- the schema and apply pipeline are identical in both tenancy modes;
- single-tenant mode requires exactly the canonical default tenant identity
  selected by repository tenancy rules;
- multi-tenant mode accepts multiple unique slugs and validates all before
  writes;
- instance settings/defaults/locks/policy ceilings constrain every tenant;
- federation publication/reporting invariants remain unchanged;
- export and administration copy is localizable and never renders raw authority
  codes as user-facing prose;
- BFF/client changes retain keyboard, focus, reflow, contrast, RTL, and live
  announcement behavior.

## 11. Observability And Recovery

Record structured, bounded facts:

- mode, apiVersion, kind, operation id, digest prefix;
- instance validation/apply result;
- tenant counts by created/skipped/failed;
- stable failure category and duration;
- outbox dispatch state.

Never record paths as metric labels, raw values, secret names/presence, tenant
private content, provider state, or PII.

Recovery:

- invalid file: run `ValidateOnly`, correct stable errors, restart owner;
- same digest: safe idempotent no-op;
- changed instance section after bootstrap: use Day 2 administration or reset a
  development instance; do not force overwrite;
- tenant conflict: preserve recorded instance section, correct tenant input, rerun;
- transaction failure: no partial state; inspect operation id and stable code;
- disable processing: set mode `Off` or remove the convention file.

### Operator State/Action Matrix

| Operator state | Safe action |
|---|---|
| File missing at explicit path | Restore/mount the original file or correct the path; startup remains failed. |
| Convention file absent | No-op unless deployment policy requires an explicit path. |
| File invalid | Run `ValidateOnly`, correct every stable error, rerun; no state was applied. |
| Original instance section lost | Inspect operation id, manifest name, digest, and changed-key codes; restore the original source from operator version control or backup. Values are intentionally not recoverable from audit. |
| Same section after Day 2 changes | Keep the original instance section unchanged; new tenants validate against current effective instance authority. |
| Changed section after bootstrap | Use Day 2 APIs/UI for instance changes or reset a development instance; do not force bootstrap overwrite. |
| Tenant already exists | Manage it through Day 2 APIs/UI; bootstrap skips it wholesale. |
| New tenant conflicts with current instance policy | Correct the tenant section or current policy through its authoritative Day 2 workflow, then rerun. |
| Failure audit cannot be persisted | Use startup correlation/trace logs and database health evidence; fix database availability and rerun. No success is assumed. |
| Database restored | Restore matching manifest source and bootstrap/audit data together; validate the digest before traffic. |
| Export exceeds 4 MiB | Use supported Day 2 APIs or database backup for recovery; manifest export emits no partial bytes and is not a backup. |

## 12. Migration And Compatibility

This workstream intentionally deletes the old tenant-manifest contract.

Remove or replace:

- `TenantConfigurationManifest*` public/internal feature names where they denote
  the whole manifest;
- `TenantConfigurationList`;
- `TENANT_MANIFEST_PATH` and `TENANT_MANIFEST_MODE`;
- `/etc/islamu-event/bootstrap/tenant-configuration.json`;
- tenant-manifest schema file and canonical URI;
- tenant-manifest routes, HAL relations, BFF methods, media type, generated
  methods, UI labels, and documentation;
- obsolete compatibility tests.

Because the project is in development mode:

- change source entities/configurations first;
- generate the renamed audit/bootstrap model migration once in Phase 11 through
  canonical EF tooling; delete/replace the current unapplied development
  migration rather than layering a compatibility rename;
- development databases with the old audit/bootstrap model are reset rather
  than supported by compatibility migrations;
- Phase 15 verifies provider-model drift/currentness unless a later approved
  source-model change genuinely requires regeneration;
- regenerate OpenAPI, API inventory, NSwag, and schema only from source;
- document the breaking cutover in API changelog and release fragment.

No destructive production-data claim is made. If deployment evidence later
proves the old migration shipped to a persistent external environment, stop and
replace the reset assumption with an explicit migration plan before editing
generated migrations.

## 13. Risk Register

| Risk | Severity | Mitigation | Phase |
|---|---|---|---|
| Tenant input reaches instance authority | Blocker | Separate catalogs, scope-tagged plans, canonical boundaries, wrong-scope invariant tests | 9–11 |
| Instance values are applied after tenant validation | Blocker | Complete proposed-state compiler and instance-first lock/apply order | 11 |
| Secrets/topology/provider credentials become exportable | Blocker | Explicit catalogs, hard exclusions, zero-value telemetry/export scans | 10, 13, 15 |
| Instance paid policy broadens or races tenant policy | Blocker | Tier 0 boundary, revision fencing, real PostgreSQL serial-order tests | 10–11 |
| Partial instance/tenant bootstrap | Critical | One transaction, lock-time preflight replay, rollback/failure audit | 11 |
| Bootstrap silently becomes reconciliation | Critical | Immutable instance-section digest, stable changed-section failure, no reconcile enum | 11–12 |
| Whole-instance export leaks other tenants | Critical | Instance-admin-only authorization and tenant-caller denial | 13–14 |
| Old and new public names coexist | Critical | Breaking-name ratchets and final tracked-reference inventory | 9, 15 |
| Instance documents become arbitrary JSON | Critical | v1alpha1 admits only the existing paid-policy aggregate; no generic document store | 10 |
| Self-hoster cannot recover from cutover | Major | Explicit reset, startup, validation, and recovery docs | 12, 15 |
| Large cross-layer change becomes unreviewable | Major | Phase boundaries above; no mixed compatibility layer | All |

## 14. Definition Of Done

The corrected workstream is complete only when:

1. one strict `ConfigurationManifest` contract contains a required instance
   section and one or more tenants;
2. single-tenant and multi-tenant deployments use the same file and pipeline;
3. only explicitly approved non-secret instance and tenant fields are exposed;
4. complete proposed instance state constrains tenant validation;
5. one transaction atomically applies instance configuration, absent tenants,
   audit, and durable effects;
6. rerun, changed-instance, conflict, rollback, and concurrency semantics are
   deterministic and covered against real PostgreSQL;
7. whole-instance export is authenticated, instance-authorized,
   deterministic, bounded, and secret-/PII-free;
8. HAL, OpenAPI, generated client, BFF, and UI agree;
9. no runtime or documentation compatibility surface exposes the old name;
10. generated schema, OpenAPI, API inventory, NSwag, and provider models are
    current and stable;
11. configuration, security, payment, operations, self-hosting,
    troubleshooting, release, and I-VSD docs agree;
12. all phase builds/tests and required Tier 1/Tier 0 evidence/review gates pass.

## 15. Implementation Agent Contract

1. Resume from `configuration-manifest-context.md`, then the first unchecked
   task in `configuration-manifest-tasks.md`, then only the referenced plan phase.
2. Do not implement from the former tenant-manifest plan.
3. Write failing invariant/contract tests before every behavioral change.
4. Change the innermost owning layer first and migrate outward without shims.
5. Use canonical mutation boundaries; never write settings, documents, paid
   policy, sale control, or audit tables directly from the handler.
6. Preserve one deterministic lock order and one transaction.
7. Regenerate, never hand-edit, migrations/snapshots/schema/OpenAPI/NSwag.
8. Keep task status current immediately; update context on each phase,
   decision, failure, or handoff; update this plan only for strategy changes.
9. Preserve unrelated shared-workspace changes and ask only on a direct conflict.
10. Do not run implementation builds/tests during planning.

## 16. Progress Reporting Contract

```text
Phase: <number and name>
Task: <id and title>
Status: completed / blocked / in progress
Changed: <behavior and key files>
Evidence: <focused selector, phase build, selected project>
Authority: <instance/tenant/security/payment decision>
Risks: <new or changed>
Plan updated: yes/no
Context updated: yes/no
Tasks reconciled: yes/no
Docs/artifacts updated: yes/no
Next: <first unchecked task>
```

## 17. Research And Provenance

Repository evidence is authoritative. External research contributed only
source-free functional constraints:

- Kubernetes object conventions support an explicit `apiVersion`, `kind`,
  `metadata`, and `spec`, while its desired-state controller behavior is
  intentionally not copied into this bootstrap-only design.
- Kubernetes declarative-management documentation demonstrates why field
  ownership, deletion, pruning, and mixed writers are separate concerns; those
  behaviors remain deferred rather than implied.
- .NET options guidance supports validation before dependent startup work.
- JSON Schema Draft 2020-12 supports a deterministic closed editor contract.
- Docker read-only bind mounts preserve a clear host/file trust boundary.
- PostgreSQL serializable isolation and explicit locking guide concurrency
  proofs, while repository-native transaction/lock abstractions remain
  authoritative.

No external source code, schema expression, test, migration, prose, or asset is
copied. No new dependency is planned.

## 18. Senior CTO Verdict

**Decision: Approve with the required phases above.**

The tenant-only implementation was a strong reusable foundation but the public
contract was too narrow for the intended platform capability. A mechanical
rename would be rejected: it would leave instance authority, document
ownership, bootstrap idempotency, export authorization, and transaction order
undefined. The corrected plan resolves those blockers, uses a clean pre-v1
breaking cutover, and preserves the security, tenant-isolation, payment,
self-hosting, and maintainability boundaries needed for implementation.

No unresolved user decision blocks the first task. Time estimates are
intentionally omitted.
