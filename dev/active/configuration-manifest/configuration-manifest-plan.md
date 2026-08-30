<!-- ABOUTME: Decision-complete plan for full instance and tenant configuration portability. -->
<!-- ABOUTME: Extends the implemented bootstrap/export foundation with import, migration, legal documents, and recovery. -->

# Configuration Manifest And Reporting-Intake Policy — Implementation Plan

Last Updated: 2026-08-30 Europe/Brussels

## 0. Planning Metadata

- **Request:** Complete configuration portability after the implemented
  instance-wide bootstrap/export foundation: whole-instance UI import,
  tenant-admin `TenantConfigurationPackage` import/export, cross-instance
  migration, preview/diff/mapping/approval, atomic apply/rollback, typed legal
  documents, extensible section coverage, and advanced operational quality.
- **Task directory:** `dev/active/configuration-manifest/`
- **Planning status:** Closed for archival by explicit user directive on
  2026-08-30. Implementation evidence reaches CM-1830; Phases 19–23 are
  retired, not represented as implemented.
- **Change classification:** Behavioral Delta. This revision adds new artifact,
  authorization, import-session, migration, legal-publication, recovery,
  extension, API/BFF, and Blazor administration behavior.
- **Current implementation reality:** strict v1alpha2
  `ConfigurationManifest` and `TenantConfigurationPackage` record/schema
  contracts plus the closed portability registry are implemented.
  Typed role-owned legal aggregates, acceptance-free portable legal source,
  append-only publication evidence, target-isolated repositories, and
  generated five-provider persistence migrations are also implemented. One
  deterministic non-fetching Markdown contract now serves preview, API, and
  role-labeled `/terms`/`/privacy` pages from last-published evidence. Bounded
  target-scoped import sessions, strict parsing, encrypted temporary bytes,
  registry-derived semantic preview/coverage, freshness binding, stable
  mapping, and expiry/cancellation cleanup are implemented behind Application
  and Persistence boundaries.
  ConfigurationManifest continues to support instance-plus-tenant startup
  bootstrap and instance-admin whole-instance export. Scope-safe instance and
  tenant upload/preview/refresh/cancel HTTP, HAL, BFF, OpenAPI, and generated
  client surfaces are implemented. Tenant-package operation/apply, cross-
  instance migration, rollback snapshots, managed drift ownership, and direct
  transfer remain retired design, not implementation evidence.
- **Primary matched intent:** `external-infrastructure-bootstrap`.
- **Criticality:** Tier 1 Security because the file crosses instance authority,
  tenant isolation, startup, filesystem, and persistence boundaries. Any
  instance paid-event policy work additionally retains Tier 0 Sovereign gates.
- **Additional matched intents:** `add-cqrs-handler`, `add-get-endpoint`,
  `add-hal-link`, `openapi-contract-change`, `blazor-component-affordance`, and
  `add-ef-migration`.
- **Complexity:** XL. The expansion crosses security-critical uploads,
  authorization, tenant isolation, concurrency, legal-publication evidence,
  generated contracts, and two administration scopes.
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
  pre-v1; v1alpha2 replaces v1alpha1 without aliases, dual reads, converters,
  deprecated routes, or migration shims.
- **I-VSD:** [i-vsd-configuration-manifest.md](../../../islamic-value-sensitive-design/i-vsd-configuration-manifest.md)
- **I-VSD reviewed input:** `sha256:b1bb05932eef7c11ec0af43b307d4afdb4eac17ac3b8d563f095cbe16c99f26d`
- **I-VSD status/disposition:** `superseded` /
  `closed-by-user-directive`. The 2026-08-30
  planning-mode review maps IVSD-F001 through IVSD-F024 to Phases 16-23 and
  confirms IVSD-F025 through IVSD-F030 are explicitly deferred. Those mappings
  are retained as historical design evidence, not completion claims.
- **CTO review:** Phases 16–23 are approved by the revision-bound
  [Senior CTO review](configuration-manifest-cto-review.md).
- **User approval:** scope boundary and implementation start approved on
  2026-08-30; the user explicitly closed the workstream for archival on
  2026-08-30 and directed Setup Assistant planning to consume the frozen
  current baseline.
- **Grill-Me intake:** resolved by direct user decisions: no backward
  compatibility; configuration-manifest work completes first; Avalonia,
  Terminal.Gui, CLI, `.env` generation, and agentic skill planning are deferred
  to a separate workstream after this Definition of Done is met.

## 1. Executive Summary

Phases 9–15 delivered the instance-wide bootstrap/export foundation. Phase 16
completed the clean v1alpha2 artifact and portability-registry cutover. The
remaining product increments complete practical configuration migration rather
than creating a separate desktop/TUI product.

The expanded target retains one strict JSON `ConfigurationManifest` and adds a
distinct tenant-scoped `TenantConfigurationPackage`:

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
- whole-instance import/export is instance-administrator-only and remains
  secret-free;
- tenant administrators can export and import only configuration they govern,
  while the authenticated target route selects tenant authority;
- every import is preview-first, revision-fenced, fully preflighted, atomic,
  receipt-backed, and recoverable through a new forward rollback operation;
- typed legal documents move as owned source/drafts without rewriting
  publication or acceptance history;
- a governed section registry makes coverage, omissions, mappings,
  dependencies, and extension compatibility machine-readable;
- Day 2 API/UI/database administration remains authoritative after bootstrap;
- optional managed ownership, drift, takeover, and direct transfer are included
  only after their dedicated security/ownership phases prove the contracts.

This is a substantial portability and administration expansion. The strict
reader, deterministic serializer/schema generator, transaction and outbox
foundation, startup sequencing, authorization, HAL/BFF, and export patterns
remain reusable.

### 1.1 Explicit Non-Goals And Deferred Product

This workstream SHALL NOT create or plan implementation tasks for:

- Avalonia web/desktop projects;
- Terminal.Gui or any CLI/TUI executable;
- `.env` generation or secret-entry experiences;
- an agentic skill or embedded AI;
- application-data migration for events, users, registrations, orders,
  tickets, payments, or uploaded files.

Those surfaces receive a separate implementation plan only after every
ConfigurationManifest phase and gate in this workstream is complete.

## 2. Source-Grounded Current State

### 2.1 Verified Evidence

The claims below reflect the completed Phases 9–15 recorded in
`configuration-manifest-context.md`. No .NET suite was rerun for this
Markdown-only planning update; all pass counts remain historical implementation
evidence rather than fresh verification.

| Current fact | Evidence | Planning consequence |
|---|---|---|
| Strict v1alpha2 instance and tenant-package contracts plus schemas exist. | `src/Explore.Application/Features/ConfigurationManifest/Contracts/**`, `ConfigurationPortabilityRegistry`, `schemas/configuration-manifest-v1alpha2.schema.json`, and `schemas/tenant-configuration-package-v1alpha2.schema.json` | Use these sole artifact identities for remaining import, legal, migration, and UI behavior. |
| Startup bootstrap is host-local, bounded, atomic, and post-migration/pre-traffic. | `src/Explore.Infrastructure/ConfigurationManifest/**`, Standalone/MigrationService composition, Phase 12 evidence | Preserve this bootstrap lane; browser/API import is a separate authorized Day 2 lane. |
| Whole-instance export exists under Control Plane authority. | `ExportConfigurationManifestQueryHandler`, Control Plane controller/HAL/BFF/client, Phase 13–14 evidence | Reuse deterministic export and bounded download, then add preview/import/history actions. |
| Tenant-shaped manifest export was intentionally removed. | CM-1330 evidence and current route/HAL absence tests | Add a distinct `TenantConfigurationPackage`; never weaken whole-instance export authorization. |
| Apply already has ordered leases, serializable transaction, safe audit, and outbox. | ConfigurationManifest apply/preflight/persistence slices and focused PostgreSQL evidence | Generalize to selected-section import and forward rollback without nested transactions. |
| No import session or browser upload boundary exists. | Current API/BFF/UI inventory and I-VSD E001–E006 | Add bounded temporary storage, preview state, expiry, digest binding, approval, and apply. |
| No typed portable legal-document aggregate exists. | Static Terms/Privacy pages and tenant setting document taxonomy | Introduce explicit instance/tenant legal ownership; keep published history and acceptance outside portable configuration. |
| Setting definitions and existing domain aggregates expose broad candidate configuration. | `SettingRegistry`, `SettingsDocumentKeys`, footer/navigation/template/custom-property/localization domains | Classify every section through a closed portability registry; never auto-expose by discovery. |
| Current full Persistence project gate is unhealthy outside this feature. | Context Phase 11 blocker and focused provider evidence | Keep the blocker visible; do not relabel focused selectors as the full-project pass. |

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

1. No whole-instance HTTP/BFF/Blazor import.
2. No tenant-scoped configuration package or tenant-admin portability surface.
3. No bounded import-session lifecycle, value-safe preview, semantic diff,
   target mapping, approval, expiry, or stale-preview fencing.
4. No named merge/apply/replace/reconcile ownership semantics.
5. No pre-import portable snapshot, forward rollback, migration receipt,
   fidelity report, or history dashboard.
6. No complete portability section registry or machine-readable coverage
   ledger.
7. No typed role-scoped legal-document aggregate, constrained Markdown
   contract, target-review lifecycle, or public rendering cutover.
8. No cross-instance direct-transfer protocol, package signature policy,
   GitOps ownership, drift, takeover, or relinquishment contract.
9. No accessibility/usability evidence for complex import, diff, mapping,
   conflict, approval, and rollback workflows.

### 2.4 Current Strengths To Preserve

- tenant isolation is explicit and fail-closed;
- secrets and PII are excluded from manifests, exports, audit, and telemetry;
- payment provider identity, credentials, sale control, refund execution,
  liability, acceptance, and reconciliation remain sovereign;
- manifest application is atomic and effects are deferred until commit;
- runtime API/UI administration does not pretend bootstrap is reconciliation.

### 2.5 Current Improvement Areas

- export/bootstrap bytes are not yet a usable migration experience;
- instance authority currently blocks legitimate tenant-owned portability;
- operators must manually compare JSON and recover changes;
- static legal pages cannot express instance/tenant ownership or move safely;
- configuration coverage is scattered across catalogs and domain-specific
  contracts rather than one truthful portability ledger;
- current I-VSD disposition is `changes-required`.

### 2.6 Strictly Deferrable Unknowns

| Unknown | Why deferrable | Owning task |
|---|---|---|
| Exact import-session/upload/snapshot retention durations | Does not change bounded expiring state-machine architecture | CM-1810 |
| Exact detached-signature algorithm and curated issuer set | Signature/issuer trust model is fixed; repository release primitives determine concrete profile | CM-2210 |
| Initial legal-template locales and jurisdiction variants | Typed locale/template contract and non-certifying review gate are fixed | CM-1710 |
| Exact managed-ownership lease/refresh interval | Explicit ownership/takeover/relinquishment semantics are fixed | CM-2210 |

Any discovery that changes artifact authority, import modes, transaction
boundaries, legal evidence separation, or phase sequencing is not deferrable
and requires plan/user review before implementation continues.

## 3. Current Foundation And Expanded Behavioral Contract

Sections 3.1–3.5 record the implemented v1alpha1 foundation. Sections
3.6–3.8 are the normative v1alpha2 expansion.

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
- startup bootstrap never performs reconcile/prune/takeover. Phase 22 may add
  `ReconcileManaged` only as a separate Day 2 ownership contract.

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
- tenant administrators do not receive a partial
  `ConfigurationManifest`; v1alpha2 adds the distinct
  `TenantConfigurationPackage` defined below;
- exports remain configuration artifacts, not backups or secret bundles;
- exported bytes are generated incrementally but the aggregate hard limit
  remains 4 MiB so every successful export is accepted by the import boundary;
  tenant enumeration is bounded at 256 and overflow is rejected before any
  per-tenant configuration read. Overflow fails before response bytes are sent with stable
  `configuration_manifest_export_too_large` ProblemDetails.

### 3.6 Artifact And Portability Requirements

#### Requirement CM-R1 — Distinct Authority-Bound Artifacts

The system SHALL expose `ConfigurationManifest` for instance-authorized
portability and `TenantConfigurationPackage` for tenant-authorized
portability. Artifact metadata MUST never select target authority.

**Scenario CM-S1 — Tenant package targets authenticated tenant**

- **GIVEN** an administrator authorized for tenant B uploads a package exported
  from tenant A;
- **WHEN** preview or apply is requested through tenant B’s route;
- **THEN** the target is tenant B, source identity is provenance only, and no
  package member can select tenant A, another tenant, or instance scope.

#### Requirement CM-R2 — Preview Before Mutation

Every HTTP/UI import SHALL create a bounded, expiring, digest-bound preview
before any configuration write. Preview MUST report changed, unchanged,
skipped, mapped, blocking, warning, omitted, and externally required items.

**Scenario CM-S2 — Upload is side-effect-free**

- **GIVEN** a valid artifact containing changes;
- **WHEN** the administrator uploads and previews it;
- **THEN** no setting, document, tenant, audit-success row, outbox effect, or
  provider call is created.

**Scenario CM-S3 — Stale preview fails closed**

- **GIVEN** target configuration changes after preview;
- **WHEN** apply uses the stale preview token;
- **THEN** apply returns a stable conflict, performs no selected-section write,
  and offers a fresh preview.

#### Requirement CM-R3 — Named Apply Semantics

The system SHALL distinguish `PreviewOnly`, `CreateNew`, `MergeMissing`,
`ApplySelected`, `ReplacePortableConfiguration`, and `ReconcileManaged`.
Omission MUST NOT imply deletion unless managed ownership explicitly grants it
and the preview names the deletion.

**Scenario CM-S4 — Replacement is bounded to selected portable fields**

- **GIVEN** application data, environment-bound fields, and unselected portable
  sections exist;
- **WHEN** an authorized administrator confirms replacement;
- **THEN** only selected portable configuration changes and every excluded
  category remains untouched.

#### Requirement CM-R4 — Atomic Apply And Forward Rollback

Every accepted import SHALL revalidate authority and revisions under ordered
locks, apply all selected sections in one transaction, persist a value-minimized
receipt and durable effects, and support rollback as a new authorized forward
operation from a protected pre-import snapshot.

**Scenario CM-S5 — One invalid or racing section rolls back all**

- **GIVEN** several selected sections and one invalid, stale, locked, or
  concurrently changed section;
- **WHEN** apply reaches fresh preflight;
- **THEN** no selected section, success receipt, or external effect commits.

#### Requirement CM-R5 — Explicit Portability Coverage

Every configuration section SHALL declare scope, authority, schema version,
portability class, dependencies, references, export, preview/diff, validation,
apply, verify, rollback, and documentation behavior. Unknown or absent required
extensions MUST fail according to their declared compatibility rule and MUST
never disappear silently.

**Scenario CM-S6 — Coverage is truthful**

- **GIVEN** an export omits secrets, PII, application data, operational state,
  or unsupported sections;
- **WHEN** the artifact and receipt are inspected;
- **THEN** machine-readable coverage names each omission and target setup
  requirement without exposing values.

### 3.7 Legal-Document Requirements

#### Requirement CM-R6 — Role-Scoped Portable Legal Source

Instance and tenant legal documents SHALL be typed, localized, role-owned,
bounded, and portable as constrained Markdown plus metadata. Import MUST create
target-reviewed drafts or new target versions and MUST NOT copy acceptance
history or source-instance authority.

**Scenario CM-S7 — Legal import preserves evidence**

- **GIVEN** a package containing published-looking terms and privacy text;
- **WHEN** it is imported into another instance or tenant;
- **THEN** target drafts/review requirements are created, source links and
  identity placeholders are flagged, and no user acceptance is fabricated or
  rewritten.

#### Requirement CM-R7 — Safe Deterministic Legal Rendering

Legal source SHALL use one constrained non-fetching Markdown contract shared by
validation, preview, import/export, and public rendering. Raw HTML, executable
content, remote images, tracking, unresolved required placeholders, and unsafe
links MUST fail closed.

**Scenario CM-S8 — Unsafe legal content cannot publish**

- **GIVEN** a document containing remote content, executable markup, or an
  unresolved required identity;
- **WHEN** preview, import, or publication readiness runs;
- **THEN** a stable value-safe blocker is returned and public state is
  unchanged.

### 3.8 Existing Blazor Administration And Deferred Setup Assistant

The existing Blazor administration application SHALL provide:

- whole-instance upload, preview, diff, mapping, approval, apply, history, and
  rollback under instance HAL authority;
- tenant package export/import/clone/history/rollback under tenant HAL
  authority;
- keyboard, screen-reader, reflow, localization, and RTL-complete workflows;
- raw JSON as an optional expert view, never the required interface.

The future Setup Assistant may consume the final schemas and APIs, but this
workstream MUST NOT create Avalonia, Terminal.Gui, CLI, `.env`, or agent-skill
projects or tasks.

## 4. Non-Negotiable Constraints

1. One canonical instance `ConfigurationManifest` and one distinct
   tenant-authorized `TenantConfigurationPackage`; no partial tenant manifest
   or compatibility surface.
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
16. No YAML, directory composition, compatibility alias, or hidden automatic
    reconciliation.
17. Every new file begins with two `ABOUTME:` lines.
18. Upload/preview storage is bounded, protected, value-safe, and expires
    automatically.
19. Artifact identity and source metadata never select target authority.
20. Legal source is typed, role-scoped, constrained, and separate from
    immutable publication/acceptance evidence.
21. Avalonia, Terminal.Gui, CLI/TUI, `.env`, and agent-skill work is excluded
    until this workstream is complete.

## 5. Architecture And Design Decisions

### 5.1 Decision Matrix

| Decision | Selected | Rejected | Rationale |
|---|---|---|---|
| Product contracts | Whole-instance `ConfigurationManifest` plus tenant-scoped `TenantConfigurationPackage` | Partial tenant manifest or one artifact with caller-selected scope | Distinct kinds preserve authority while enabling legitimate tenant portability. |
| Contract migration | Replace old kind, DTOs, schema, routes, env keys, and media types | Preserve aliases | Pre-v1 development permits a clean contract; duplicate semantics would become permanent debt. |
| Scope catalogs | Separate explicit instance and tenant catalogs under one feature | Auto-expose by registry scope | Instance eligibility requires stricter authority and operational classification. |
| Instance documents | `instance.paid_event_policy` only in v1alpha1, backed by its existing aggregate | Generic JSON document entity or reuse tenant rows | Do not create speculative persistence; every document needs a real authority owner. |
| Apply ordering | Compile instance, validate tenants against it, then one atomic apply | Tenant-first or independent transactions | Instance defaults, locks, and ceilings constrain tenant validity. |
| Bootstrap changes | Immutable instance-section digest after first success | Restart-time overwrite | Avoids hidden reconciliation and Day 2 changes being reverted. |
| Tenant reruns | Create absent, skip existing wholesale | Patch existing tenants | Preserves explicit Day 0 versus Day 2 ownership. |
| Export authority | Instance-admin whole-instance export plus tenant-admin package export | Tenant-shaped partial manifests | Tenant portability needs a distinct non-root artifact and trusted route-selected target. |
| Paid policy | Canonical instance revision then tenant narrowing | Direct setting/table writes | Preserves Tier 0 authority, freshness, and sovereign exclusions. |
| Effects | Transactional outbox and post-commit cache/notification dispatch | External effects inside transaction | Keeps rollback truthful and recovery idempotent. |
| Import lifecycle | Bounded upload → side-effect-free preview → approved apply → receipt/history | Upload-to-write | Prevents hidden destructive changes and stale authority. |
| Rollback | New forward import from protected portable snapshot | Database/audit history rewrites | Preserves append-only evidence and canonical mutation boundaries. |
| Legal documents | Typed role-scoped Markdown source with target review | Hard-coded pages, links-only portability, arbitrary HTML | Preserves accountable authorship, safety, localization, and migration independence. |
| Extensibility | Closed section registry with declared portability behavior | Arbitrary extension JSON/scripts/SQL | Makes coverage truthful without remote code execution. |
| Advanced automation | Explicit managed ownership and optional reviewed direct transfer | Implicit continuous reconciliation or source deletion | Keeps drift/takeover/deletion and network trust visible. |
| Setup applications | Separate later plan after ConfigurationManifest completion | Mix Avalonia/TUI/CLI/skill work into this plan | Keeps the server/domain contract stable before new clients depend on it. |

### 5.2 Clean Architecture Ownership

- **Domain:** setting definitions; existing tenant document ownership;
  paid-policy and other true domain invariants; audit/bootstrap state entities.
- **Application:** manifest contracts, catalogs, validators, compiler, apply
  plan, import-session state machine, section registry, preview/diff/mapping,
  CQRS requests/handlers, scope-qualified DTO mapping, authorization facts,
  canonical transaction-aware mutation boundaries, deterministic serializer.
- **Persistence:** EF configuration, entity repositories, transaction execution,
  protected import metadata/snapshots/receipts, indexes/constraints, generated
  provider migrations.
- **Infrastructure:** bounded local-file reading, lexical strictness, options,
  digesting, startup runner, safe logs/metrics.
- **API/HAL:** instance/tenant import/export transport, ProblemDetails, rate
  limits, route names, OpenAPI, HAL capability emission.
- **Blazor BFF/client:** token-safe bounded upload/download adapters and
  HAL-driven instance/tenant administration UI; no local authorization
  calculation.
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
- bind apply to preview digest, target revisions, selected sections, mappings,
  and approvals; any drift invalidates the preview;
- treat rollback as another fresh authorized apply with its own locks,
  transaction, receipt, and outbox;
- on any stale revision or conflict, roll back instance, tenants, audit success,
  and effects together, then record only privacy-minimized failure evidence when
  the database remains available.

### 5.4 Authorization And Trust

- file application remains a trusted host bootstrap operation, never an HTTP
  request body or browser-supplied path;
- instance export requires instance-level resource authorization with Cerbos and
  local-provider parity;
- instance import requires independent preview/apply/replace/rollback facts and
  enhanced approval where policy broadens;
- tenant administrators retain Day 2 APIs and may export/import only a
  `TenantConfigurationPackage` for the trusted route-selected target;
- BFF tokens remain server-side and the browser receives only bounded
  upload/download and value-safe preview contracts;
- explicit wrong-instance, wrong-tenant, missing-context, and locked-authority
  tests fail closed.

## 6. Implementation Phases

Phases 1–8 produced the former tenant foundation. Phases 9–15 delivered the
current instance-wide bootstrap/export product. Expanded implementation resumes
at Phase 16. Granular Red/Green tasks live only in
`configuration-manifest-tasks.md`.

### 6.1 Delivery And Review Boundaries

This file is an umbrella workstream, not one omnibus change set. The expanded
implementation SHALL remain independently reviewable at these boundaries:

1. Phase 16: artifact and registry contract cutover;
2. Phase 17: legal-document aggregate and rendering boundary;
3. Phases 18-19: import-session and atomic recovery backend;
4. Phase 20: tenant package and migration API boundary;
5. Phase 21: BFF/client administration boundary;
6. Phase 22: managed ownership and direct-transfer boundary;
7. Phase 23: generated artifacts, documentation, evidence, and release.

Each boundary closes its own build and selected project gate before work moves
forward. Later boundaries may depend on earlier contracts, but an incomplete
UI or advanced-operations boundary MUST NOT obscure the independently verified
backend state. No commit is created unless the user separately authorizes one.

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

### Phase 16 — V1Alpha2 Artifact And Section Contracts

**Goal:** Replace v1alpha1 with the clean v1alpha2
`ConfigurationManifest`/`TenantConfigurationPackage` contracts and one closed
portability-section registry.

**Primary paths:** Application contracts/catalog/serialization/schema;
settings/document/module/template/footer/navigation/custom-property/localization
owners; schema generator; contract and architecture tests.

**Exit criteria:** both kinds are strict and authority-distinct; every candidate
section has one portability classification and owner; unknown sections fail
closed; machine-readable coverage and omission contracts exist; v1alpha1 has no
alias.

**Phase-end verification:** one Release build and
`Event.Application.UnitTests`.

**Rollback/failure:** contract/schema failure leaves v1alpha1 current; no
runtime consumer cuts over before the new contract and registry are green.

### Phase 17 — Typed Legal Documents And Safe Content

**Goal:** Add role-scoped instance/tenant legal-document ownership, immutable
publication history, acceptance separation, constrained Markdown, localization,
and target-review transitions.

**Primary paths:** Domain legal aggregates/value objects; Application mutation,
validation, rendering contracts; Persistence entities/configurations/generated
migrations; current legal pages/footer composition; legal tests.

**Exit criteria:** legal kinds are closed and role-owned; Markdown is bounded,
deterministic, non-fetching, and shared across validation/rendering; imports
create target drafts/new versions; publication and acceptance evidence cannot
be imported or rewritten; unsafe content fails closed.

**Phase-end verification:** one Release build and `Event.Domain.UnitTests`.

**Rollback/failure:** legal routes continue serving the last published
immutable version; failed import/publication creates no new effective document.

### Phase 18 — Import Sessions, Preview, Diff, And Mapping

**Goal:** Introduce bounded instance and tenant import sessions whose previews
are side-effect-free, digest/revision bound, section-selective, and
mapping-aware.

**Primary paths:** Application import-session contracts/state machine,
preview/diff/coverage/mapping composers; protected Persistence metadata/storage;
API upload/preview routes, rate limits, ProblemDetails, OpenAPI, and tests.

**Exit criteria:** upload never mutates configuration; sessions expire and
cannot cross authority; preview classifies all outcomes and external setup;
stable mappings use machine identities; stale revisions fail before apply;
secrets/PII/values never enter telemetry or support artifacts.

**Phase-end verification:** one Release build and
`Event.API.IntegrationTests`.

**Rollback/failure:** rejected/expired sessions delete protected temporary
bytes according to retention policy and retain only value-minimized evidence.

### Phase 19 — Atomic Apply, Receipts, Snapshots, And Forward Rollback

**Goal:** Apply selected instance/tenant sections through canonical boundaries
under ordered leases and one fresh serializable transaction, with durable
receipts and forward rollback.

**Primary paths:** Application preflight/apply/verify/rollback; Domain operation
state; Persistence transaction/leases/snapshots/receipts/outbox; real
PostgreSQL/provider tests and generated migrations.

**Exit criteria:** all selected sections preflight before writes; preview digest,
mappings, revisions, approvals, and target authority are replayed; one failure
rolls back every section/effect; receipts are value-minimized; rollback is a new
authorized operation; provider models are generated/current.

**Phase-end verification:** one Release build and the complete
`Event.Persistence.IntegrationTests` project. Focused selectors guide active
development but cannot close the phase; the existing unrelated project
baseline must be repaired or explicitly waived.

**Rollback/failure:** transaction rollback preserves current state; snapshot
creation failure prevents apply; post-commit effect retry uses the outbox.

### Phase 20 — Tenant Portability And Cross-Instance Migration

**Goal:** Deliver tenant-admin package export/import, clone, target mapping,
history, rollback, fidelity, and optional source/target transfer prerequisites
without exposing whole-instance authority.

**Primary paths:** Application tenant package CQRS; tenant-safe repositories;
authorization facts/providers; API/HAL/OpenAPI/BFF contracts; migration
receipts and tests.

**Exit criteria:** authenticated route context selects the target; tenant
packages never include instance/other-tenant values; clone requires delegated
creation authority; source IDs are provenance only; external setup and fidelity
are explicit; whole-instance export remains instance-only.

**Phase-end verification:** one Release build and
`Event.API.IntegrationTests`.

**Rollback/failure:** source state is never deleted automatically; failed target
apply leaves source and target configuration unchanged.

### Phase 21 — Blazor Instance And Tenant Administration

**Goal:** Add preview-first instance and tenant import/export/history/rollback
workspaces to the existing Blazor administration product.

**Primary paths:** Blazor BFF upload/download services/endpoints; generated
client adapter; instance/tenant pages/components; localization/scoped CSS;
accessibility and component tests.

**Exit criteria:** HAL alone gates every action; tokens stay server-side;
instance and tenant authority remain visually and technically distinct; dense
diff/mapping/approval flows are keyboard/screen-reader/reflow/RTL capable; raw
JSON is optional; stale capability/preview recovery preserves focus.

**Phase-end verification:** one Release build and
`Explore.Blazor.Client.Tests`.

**Rollback/failure:** client failure never implies apply success; operation
history/receipt remains the authoritative result.

### Phase 22 — Extensibility, Managed Ownership, And Direct Transfer

**Goal:** Complete governed extension sections, signatures, GitOps drift and
field ownership, approval separation, and optional mutually authenticated
direct transfer.

**Primary paths:** section descriptors/source generation; ownership/drift
contracts; signature/trust policy; transfer session/protocol; authorization,
outbox, security, privacy, and integration tests.

**Exit criteria:** extensions execute no code/migration; signed packs identify
issuer and provenance; managed deletion/takeover/relinquishment is explicit;
drift never overwrites automatically; direct transfer is opt-in, SSRF-safe,
replay-protected, mutually approved, bounded, resumable, and never deletes
source state.

**Phase-end verification:** one Release build and
`Event.Application.UnitTests`.

**Rollback/failure:** unmanaged fields remain untouched; transfer can resume or
expire safely before commit; signature/issuer failure blocks preview/apply.

### Phase 23 — Generated Contracts, Operations, Evidence, And Release

**Goal:** Regenerate and prove all schemas/contracts/models, teach migration and
recovery, refresh I-VSD/CTO evidence, close criticality review, and publish the
breaking change fragment.

**Primary paths:** JSON Schema/OpenAPI/inventory/NSwag/provider models;
configuration/self-hosting/operations/security/legal/accessibility/API
documentation; release fragment; I-VSD; evidence and triad.

**Exit criteria:** generated artifacts are stable on a second run; all
configuration domains have truthful coverage; docs distinguish configuration,
application data, secrets, operational state, and backup; migration/rollback
drills and criticality review have no surviving blocker; I-VSD is plan-aligned;
the final change fragment/commit composition follows release policy.

**Phase-end verification:** one Release build and
`Event.Architecture.Tests`.

**Rollback/failure:** no completion claim while any generated drift, unwaived
phase gate, critical finding, or I-VSD/triad mismatch remains.

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
- tenant package attempts to select another target or instance scope;
- upload-to-write, expired session, replayed token, stale preview, changed
  mapping, and approval bypass;
- one invalid selected section among otherwise valid sections;
- forward rollback racing ordinary Day 2 writers;
- legal import attempting to copy publication/acceptance history;
- raw HTML, remote resources, unsafe links, oversized localized Markdown, and
  unresolved required legal placeholders;
- extension code/migration payloads, unknown compatibility, drift takeover,
  deletion without ownership, package signature failure, SSRF, and transfer
  replay;
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
| 16 | `Event.Application.UnitTests` |
| 17 | `Event.Domain.UnitTests` |
| 18 | `Event.API.IntegrationTests` |
| 19 | `Event.Persistence.IntegrationTests` |
| 20 | `Event.API.IntegrationTests` |
| 21 | `Explore.Blazor.Client.Tests` |
| 22 | `Event.Application.UnitTests` |
| 23 | `Event.Architecture.Tests` |

Each phase also runs one canonical Release build. BFF and other touched-layer
tests use focused TUnit selectors during their owning task; full unrelated
projects are not added to phase exits.

## 8. Documentation, Configuration, And Operations

Canonical operator contract after cutover:

- `CONFIGURATION_MANIFEST_PATH`
- `CONFIGURATION_MANIFEST_MODE`
- `/etc/islamu-event/bootstrap/configuration-manifest.json`
- `schemas/configuration-manifest-v1alpha2.schema.json`
- `schemas/tenant-configuration-package-v1alpha2.schema.json`
- `application/vnd.islamu.configuration-manifest.v1alpha2+json`
- `application/vnd.islamu.tenant-configuration-package.v1alpha2+json`

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
It must also teach import modes, preview expiry/staleness, mappings, approvals,
receipts, rollback, tenant migration, legal target review, section coverage,
managed ownership, transfer trust, retention, and support-safe diagnostics.

### 8.1 Release And Changelog Strategy

This is Tier 2 breaking/operator-impact work. The final Phase 23 task SHALL:

- create an append-only `docs/releases/changes/CHG-*.yaml` fragment;
- identify v1alpha1 removal, v1alpha2 artifact/media/schema changes, new
  instance/tenant administration behavior, migration/reset actions, and legal
  review boundaries;
- validate through `ReleaseInputPolicy`;
- compose a conventional `feat(configuration)!:` commit subject only when the
  user authorizes a commit;
- include `Change-Id: CHG-...` and a `BREAKING CHANGE:` footer.

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
- **Imports:** bounded temporary bytes, digest-bound previews, automatic expiry,
  rate/size limits, stale-revision fencing, and value-safe ProblemDetails.
- **Legal:** role-scoped source only; publication/acceptance history is
  immutable nonportable evidence; constrained Markdown performs no fetch.
- **Extensions/transfer:** no executable payloads; signatures do not grant
  authority; transfer is SSRF-safe, replay-protected, and mutually approved.

### 9.1 I-VSD Mapping

Report:
[i-vsd-configuration-manifest.md](../../../islamic-value-sensitive-design/i-vsd-configuration-manifest.md),
reviewed input
`sha256:b1bb05932eef7c11ec0af43b307d4afdb4eac17ac3b8d563f095cbe16c99f26d`,
status `current`, disposition `plan-aligned`.

| I-VSD finding/mitigation | Scenario/task mapping | Disposition |
|---|---|---|
| F001/M001 | CM-S2; CM-1810–1830 and CM-2110 | Implement whole-instance preview/import/history/rollback. |
| F002/M002 | CM-S1; CM-2010–2030 and CM-2120 | Implement tenant-admin package portability. |
| F003/M003 | CM-S1; CM-1610 and CM-2010 | Target authority comes only from authenticated route/context. |
| F004/M004 | CM-S2/CM-S3; CM-1810–1830 | Enforce preview-first, side-effect-free upload. |
| F005/M005 | CM-S4; CM-1610/CM-1620/CM-1920 | Implement named apply modes and explicit ownership. |
| F006/M006 | CM-S5; CM-1910–1930 | Implement atomic apply, snapshots, receipts, forward rollback. |
| F007/M007 | CM-S6; CM-1820/CM-2010 | Map only stable machine identities and surface blockers. |
| F008/M008 | CM-S6; CM-1610/CM-1810/CM-2310 | Exclude and scan secrets, PII, application/operational data. |
| F009/M009 | CM-S7; CM-1710–1730 | Rebind accountable target identity; never copy authority blindly. |
| F010/M010 | CM-S5; CM-1910/CM-1920 | Fence paid policy and require enhanced approval for broadening. |
| F011/M011 | CM-S1/CM-S3; CM-2220 | Implement optional mutually approved secure transfer. |
| F012/M012 | CM-S6; CM-1610/CM-2210 | Use a declarative non-executable extension registry. |
| F013/M013 | CM-S6; CM-1620/CM-2310 | Generate truthful coverage, omission, dependency, fidelity ledgers. |
| F014/M014 | Section 3.8; CM-2110–2130 | Implement accessible/localized instance and tenant workflows. |
| F015/M015 | CM-S1/CM-S5; CM-1810/CM-1910/CM-2010 | Reauthorize and append value-minimized audit evidence. |
| F016/M016 | This re-baseline; CM-2310 | Resolve stale-plan integrity and keep triad/report synchronized. |
| F017/M017 | CM-S6; CM-2320 | Document configuration versus data, secrets, operations, and backup. |
| F018/M018 | CM-1930/CM-2030/CM-2330 | Prove fidelity, recovery, accessibility, and migration outcomes. |
| F019/M019 | CM-S7; CM-1710/CM-1720 | Implement typed role-scoped legal bundles. |
| F020/M020 | CM-S7; CM-1710/CM-1920 | Preserve immutable publication/acceptance evidence. |
| F021/M021 | CM-S7; CM-1710/CM-1730 | Govern templates as non-certifying reviewed starting points. |
| F022/M022 | CM-S8; CM-1710–1730 | Share one constrained safe Markdown contract. |
| F023/M023 | CM-S7; CM-1720/CM-2010 | Export owned source and create target-reviewed versions. |
| F024/M024 | CM-S8; CM-1610/CM-1710 | Re-baseline legal count/locale/byte/link/package limits. |
| F025–F030/M025–M030 | Section 1.1 and 3.8 | Deferred in full to the later Setup Assistant/Avalonia/TUI/CLI/skill workstream; no task here. |

## 10. Single-Tenancy, Multi-Tenancy, Federation, Localization, Accessibility

- the schema and apply pipeline are identical in both tenancy modes;
- single-tenant mode requires exactly the canonical default tenant identity
  selected by repository tenancy rules;
- multi-tenant mode accepts multiple unique slugs and validates all before
  writes;
- instance settings/defaults/locks/policy ceilings constrain every tenant;
- tenant package target authority comes from authenticated route context, never
  package metadata;
- federation publication/reporting invariants remain unchanged;
- configuration migration remains distinct from federation/PDS publication and
  application-data migration;
- export and administration copy is localizable and never renders raw authority
  codes as user-facing prose;
- BFF/client import/diff/mapping/approval/history changes retain keyboard,
  focus, reflow, contrast, RTL, non-color state, and live announcements.

## 11. Observability And Recovery

Record structured, bounded facts:

- mode, apiVersion, kind, operation id, digest prefix;
- instance validation/apply result;
- tenant counts by created/skipped/failed;
- stable failure category and duration;
- outbox dispatch state.
- import-session status/expiry, artifact kind, selected section count, mapping
  blocker count, approval state, snapshot/rollback operation relation, and
  fidelity status.

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
- stale/expired preview: create a new preview; never force the old token;
- failed Day 2 apply: inspect the value-safe receipt; no selected state changed;
- applied migration needing reversal: authorize a forward rollback from the
  protected pre-import snapshot;
- transfer interruption: resume or expire before target commit; never delete
  source state.

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

Phases 9–15 intentionally deleted the old tenant-manifest contract. Phases
16–23 intentionally replace v1alpha1 with v1alpha2 and add a distinct tenant
package.

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

The v1alpha2 cutover also removes v1alpha1 schema/media/generated-client
surfaces in one source-driven regeneration. No aliases, converters, dual reads,
redirects, or deprecated endpoints remain. Development databases use generated
corrective/reset strategy selected before migration edits; persisted external
use requires an explicit data-migration decision.

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
| Package metadata selects target authority | Blocker | Trusted route/context target, distinct artifact kinds, wrong-scope tests | 16, 18, 20 |
| Upload mutates before approval | Blocker | Side-effect-free session/preview state machine and public-state tests | 18 |
| Stale preview applies over Day 2 changes | Blocker | Digest/revision/mapping/approval replay under ordered locks | 18–19 |
| Rollback rewrites audit or partially applies | Blocker | Protected snapshot and new forward operation in one transaction | 19 |
| Tenant package leaks other scopes | Blocker | Tenant-filtered repositories, HAL/API parity, output scans | 20 |
| Legal import fabricates acceptance | Blocker | Separate immutable evidence and target-draft semantics | 17, 19 |
| Markdown executes or tracks | Blocker | Shared constrained non-fetching parser/sanitizer and adversarial tests | 17 |
| Extension smuggles executable behavior | Blocker | Declarative registry; no scripts/SQL/migrations/plugins | 16, 22 |
| Managed ownership deletes unmanaged fields | Critical | Explicit field ownership, previewed deletion, takeover/relinquishment | 22 |
| Direct transfer creates SSRF/replay/source loss | Blocker | Allowlisted target proof, mutual approval, nonce/digest binding, no source deletion | 22 |
| Setup Assistant scope contaminates server plan | Major | Explicit deferment and zero Avalonia/TUI/CLI/skill tasks | All |

## 14. Definition Of Done

The expanded workstream is complete only when:

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
12. whole-instance import is available through API/BFF/Blazor with preview,
    mapping, approval, history, and rollback;
13. tenant administrators can export/import/clone only their governed
    `TenantConfigurationPackage`;
14. typed role-scoped legal documents migrate as safe source/drafts without
    changing publication or acceptance history;
15. section coverage, omissions, mappings, dependencies, fidelity, and target
    setup are machine-readable;
16. managed ownership/direct transfer satisfy their explicit safety contracts;
17. all phase builds/tests and required Tier 1/Tier 0 evidence/review gates
    pass, including the previously unsatisfied full Persistence project unless
    explicitly waived;
18. I-VSD is refreshed to `plan-aligned`, the triad is reconciled, and the
    breaking release fragment validates;
19. no Avalonia, Terminal.Gui, CLI/TUI, `.env`, or agent-skill implementation
    has been mixed into this workstream.

## 15. Implementation Agent Contract

1. Resume from `configuration-manifest-context.md`, then the first unchecked
   task in `configuration-manifest-tasks.md`, then only the referenced plan phase.
2. Treat Phases 9–15 as historical completed foundation and start expanded work
   at CM-1610; do not reopen completed tasks without new contrary evidence.
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
11. Do not create Avalonia, Terminal.Gui, CLI/TUI, `.env`, or agent-skill work
    under this task; stop and route such work to the later Setup Assistant plan.

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

**Decision: Approve Phases 16–23 for implementation at the independently gated
delivery boundaries in Section 6.1.**

The implemented bootstrap/export foundation remains sound. The expanded plan
now addresses the I-VSD gaps in whole-instance UI import, legitimate tenant
portability, migration recovery, legal-document ownership, extensibility, and
advanced governance without mixing in the deferred Setup Assistant clients.

The revision-bound review is recorded in
`configuration-manifest-cto-review.md`. No unresolved user decision changes the
first task’s scope. Legal document authority and direct-transfer controls still
require their named legal, security, privacy, accessibility, and scholarly
release gates. Time estimates are intentionally omitted.
