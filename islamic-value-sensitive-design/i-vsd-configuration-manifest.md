<!-- ABOUTME: I-VSD review for one instance-wide ConfigurationManifest and its authority boundaries. -->
<!-- ABOUTME: Protects tenant autonomy, privacy, payment governance, portability, and operator accountability. -->

# I-VSD Consultancy Report: ConfigurationManifest And Reporting-Intake Policy

Last Updated: 2026-08-26

## Claim Boundary

This report evaluates provider-controlled software decisions for one
`ConfigurationManifest` that can configure approved instance and tenant state in
single-tenant and multi-tenant ISLAMU Event deployments.

It is provider-responsibility design analysis, not a fatwa, Sharia
certification, legal opinion, privacy/copyright certification, or proof that
configuration choices produce morally correct outcomes. Questions classifying a
specific act as halal, haram, obligatory, prohibited, or otherwise
religiously/legal significant require qualified Sunni scholarly or legal review.

## Scope

The review covers:

- instance and tenant configuration authority;
- single-/multi-tenant defaults and isolation;
- Day 0 bootstrap versus Day 2 administration;
- export, portability, self-hosting, and operator recovery;
- secrets, PII, audit, logs, metrics, and generated artifacts;
- reporting-intake safety and correction channels;
- instance paid-event policy versus tenant narrowing;
- HAL/BFF administration and accessibility.

It does not approve managed reconciliation, secret replication, external
provider credentials, operational payment governance, liability allocation,
refund execution, legal compliance, or religious claims.

## Executive Recommendation

Approve one instance-wide `ConfigurationManifest` only with these boundaries:

1. Use one explicit contract containing a required instance section and one or
   more tenant sections.
2. Keep independent explicit instance and tenant allowlists; registry membership
   alone never grants manifest ownership.
3. Validate the complete proposed instance state before validating tenant
   narrowing, defaults, locks, or policy ceilings.
4. Apply all approved state atomically through canonical mutation boundaries.
5. Keep secrets, PII, provider identity/credentials, operational payment state,
   liability, and refund execution outside manifests and exports.
6. Restrict whole-instance export to instance authority; tenant administrators
   must not receive cross-tenant configuration.
7. Preserve local reporting by default, publication-safety enforcement, and an
   independent correction/legal/copyright route.
8. Keep bootstrap distinct from reconciliation. A changed instance section
   after bootstrap must fail visibly rather than silently overwrite Day 2 state.
9. Use the same file/schema in single-tenant and multi-tenant deployments.
10. Remove the narrower tenant-only compatibility surface rather than
    maintaining two competing product concepts.

## Findings By Severity

### Blocker — Tenant input must never select instance authority

**Provider-controlled decision:** The contract and compiler determine whether a
tenant section can name or reach instance settings, documents, policies, locks,
or mutation paths.

**Risk:** A wrong-scope key could let a tenant broaden platform policy, weaken
another tenant's protections, alter global defaults, or expose cross-tenant
configuration.

**Required mitigation:** Use independent typed catalogs and apply-plan types;
reject wrong-scope keys before repository work; preserve instance-first lock
ordering and tenant filters; add wrong-scope and cross-tenant invariant-breaker
tests.

**Principles/domains:** amanah, justice, non-harm, security, architecture, and
governance.

### Blocker — Instance policy must constrain tenant policy in the same plan

**Provider-controlled decision:** The manifest compiler decides which effective
instance defaults, locks, and paid-policy ceilings tenants are validated
against.

**Risk:** Validating tenants against stale pre-manifest state and then changing
instance state can create a combination that no canonical policy path would
permit.

**Required mitigation:** Compile the complete proposed instance state first,
bind tenant validation to it, replay freshness under canonical locks, and commit
the complete plan atomically.

**Principles/domains:** trust, consistency, financial responsibility,
concurrency, and evaluation.

### Critical — Whole-instance export creates cross-tenant disclosure power

**Provider-controlled decision:** The platform chooses who may export instance
and tenant configuration in one file.

**Risk:** Reusing tenant-self export authority could expose other tenants'
configuration, inherited governance, business policy, or operational metadata.

**Required mitigation:** Make canonical export instance-administrator-only;
remove tenant-shaped deployable manifest exports; omit secrets/PII/provider
state; explain Overrides versus Portable flattening before download; retain
HAL-only affordances and BFF token secrecy.

**Principles/domains:** privacy, dignity, justice, transparency, authorization,
and UX.

### Critical — Declarative bootstrap must not become covert reconciliation

**Provider-controlled decision:** Restart behavior determines whether the file
silently reclaims settings changed through Day 2 administration.

**Risk:** Hidden restart-time overwrite undermines tenant/operator autonomy and
makes the UI/API appear dishonest.

**Required mitigation:** Record an immutable normalized instance-section digest;
allow same-section idempotency; reject changed instance state after bootstrap;
create only absent tenants and skip existing tenants; defer ownership, deletion,
takeover, prune, and drift semantics.

**Principles/domains:** transparency, autonomy, trust, operations, and recovery.

### Critical — Instance documents require typed ownership

**Provider-controlled decision:** The platform defines whether instance
configuration can store arbitrary JSON.

**Risk:** A generic document bag can become an unreviewed channel for secrets,
PII, security policy, provider state, or values without validation/migration
ownership.

**Required mitigation:** v1alpha1 admits only
`instance.paid_event_policy`, which already has a Domain aggregate,
Application validation, canonical mutation boundary, persistence owner,
concurrency, and safe export metadata. Do not create a generic instance
document table; every other instance document key remains closed.

**Principles/domains:** data minimization, competence, maintainability, and
privacy.

### Critical — Payment configuration must not imply operational or moral authority

**Provider-controlled decision:** The manifest may configure approved
instance-paid policy and tenant narrowing.

**Risk:** A broad field could imply who operates payments, which provider acts,
who bears liability, whether a refund occurred, or whether sale-control/review
state can be bypassed.

**Required mitigation:** Use the canonical Tier 0 policy boundary and
`PaidEventPolicyRules`; keep operator/provider identity, credentials, charge
type, sale control, review, handoff, acceptance, PII, disputes, negative
balances, liability, reconciliation, and refund execution outside the contract.
Tenant policy remains narrowing only.

**Principles/domains:** justice, financial responsibility, non-harm,
accountability, and truthful claims.

### High — Secrets and deployment topology remain separate authority

**Provider-controlled decision:** The platform chooses whether the manifest may
carry credentials, secret-provider references, connection strings, signing
material, or infrastructure ownership.

**Risk:** A portable/exportable file can become a secret backup, leak through
version control, or let business configuration override deployment-managed
security.

**Required mitigation:** Secrets remain Infisical or `.env` sourced and are
never manifest fields. The manifest path/mode are deployment plumbing only.
Reject direct and indirect secret/topology keys; scan export, audit,
ProblemDetails, logs, metrics, traces, and evidence.

**Principles/domains:** privacy, amanah, security, self-hosting, and operations.

### High — Reporting accountability must survive the rename

**Provider-controlled decision:** The existing reporting-intake policy and
publication-safety gate are part of the configuration foundation.

**Risk:** Instance expansion could bypass the effective publication invariant or
conflate external provider routing with local report intake.

**Required mitigation:** Preserve `event_reporting.intake_enabled`, local-first
reporting, canonical publication-policy mutation, direct POST/options/HAL
agreement, and an independent correction/legal/copyright channel.

**Principles/domains:** non-harm, justice, rights of people, support, and
governance.

### High — Startup failure must be atomic, visible, and recoverable

**Provider-controlled decision:** The host chooses whether invalid mixed-scope
state partially applies or traffic starts.

**Risk:** Partial instance success with tenant failure can leave an operator
believing one coherent configuration was applied.

**Required mitigation:** Validate all scopes first; begin one serializable
transaction; acquire the instance-manifest lock, sorted instance-resource locks,
and sorted tenant/resource locks; replay fresh state; persist privacy-minimized
operation/results; enqueue effects atomically; fail pre-traffic; and document
same-digest, Day 2 divergence, changed-instance, tenant-conflict, reset, and
disablement recovery.

**Principles/domains:** competence, stewardship, transparency, and operational
accountability.

## Stakeholder Traceability

| Stakeholder | Primary interest | Provider-controlled protection |
|---|---|---|
| Event attendees/community members | Accurate, safe, correctable listings | Reporting defaults, publication safety, correction channel |
| Tenant administrators | Local autonomy without cross-tenant exposure | Tenant isolation, Day 2 APIs, no instance export authority |
| Instance administrators | Deterministic whole-instance bootstrap/export | Explicit catalogs, atomic apply, recovery, HAL-authorized export |
| Self-hosters | No provider lock-in or hidden startup overwrite | Local file, read-only mount, no external control plane, bootstrap-only semantics |
| Organizers | Fair policy and payment boundaries | Tenant narrowing, transparent sovereign exclusions |
| Reporters/affected third parties | Privacy and remedy | Local-first report intake, data minimization, independent contact routes |
| Platform operators | Security, auditability, supportability | Instance-first authority, safe audit, bounded telemetry, generated contracts |

## Principle And Domain Traceability

| Principle | Configuration implication |
|---|---|
| Amanah / trust | File, effective state, API/HAL, and UI must describe the same authority. |
| Justice / consistency | Equivalent instance/tenant and mutation paths receive equivalent validation. |
| Non-harm | Unsafe scope broadening, publication bypass, secret exposure, and partial writes fail closed. |
| Rights of people | Reporting/correction channels and privacy protections survive configuration changes. |
| Autonomy | Self-hosters and tenants retain explicit Day 0/Day 2 ownership without hidden restart takeover. |
| Privacy and dignity | No secrets, PII, provider state, or private values enter portable artifacts or telemetry. |
| Accountability | Actor/operation/result facts are auditable without retaining configuration values. |

## Rejected Alternatives

1. Keep separate tenant and instance manifests — rejected because it creates
   composition/order ambiguity and contradicts one instance artifact.
2. Mechanically rename the tenant-only envelope — rejected because it leaves
   instance authority, documents, transaction order, and export authorization
   undefined.
3. Auto-expose every instance-scoped registry key — rejected because scope does
   not prove secret, topology, policy, or operational safety. The initial
   allowlist is fixed in the implementation plan's authority matrix.
4. Reuse tenant document storage for instance documents — rejected because
   ownership and tenant isolation become ambiguous.
5. Allow tenant-self manifest export — rejected because the canonical file is
   instance-wide.
6. Restart-time overwrite or reconciliation — rejected until ownership,
   deletion, takeover, drift, and conflict semantics are approved.
7. Preserve old aliases — rejected because the project is pre-v1 and two public
   concepts would create permanent operator and maintenance debt.
8. Export raw secrets or secret references — rejected because configuration
   portability is not secret backup.

## Validation And Evaluation Plan

Implementation evidence must prove:

- wrong-scope tenant input cannot reach instance state;
- complete proposed instance state constrains every tenant;
- all approved writes share canonical mutation/lock/freshness rules;
- invalid or racing mixed-scope operations produce no partial writes;
- instance export denies tenant-only and wrong-instance callers;
- secret/PII/provider/operational values are absent from every output/sink;
- single-tenant and multi-tenant deployments use the same contract;
- reporting/publication safety and payment sovereign boundaries remain intact;
- operator diagnostics and recovery are understandable and non-sensitive;
- HAL/BFF/UI behavior mirrors server authorization.

Operational evaluation after release should review startup failures, changed
instance-section attempts, wrong-scope rejections, export authorization denials,
tenant skip/add outcomes, correction-channel accessibility, and whether
operators mistake bootstrap for reconciliation.

## Evidence Reviewed

### Repository Evidence

- `ConfigurationManifestV1Alpha1.cs`
- `ConfigurationManifestCatalog.cs`
- `ConfigurationManifestValidator.cs`
- `ConfigurationManifestCompiler.cs`
- `ApplyConfigurationManifestCommandHandler.cs`
- `ExportConfigurationManifestQueryHandler.cs`
- `ConfigurationManifestStartupRunner.cs`
- `SettingDefinition.cs`
- `SettingRegistry.cs`
- `SettingUpsertService.cs`
- `TenantSettingsDocument.cs`
- `SettingsDocumentTaxonomy.cs`
- `PaidEventPolicyMutationBoundary.cs`
- `PaidEventPolicyRules.cs`
- current manifest Application, Persistence, Infrastructure, API, BFF, Blazor,
  schema, and architecture test slices

### Official Functional References

- Kubernetes objects:
  <https://kubernetes.io/docs/concepts/overview/working-with-objects/kubernetes-objects/>
- Kubernetes declarative configuration:
  <https://kubernetes.io/docs/tasks/manage-kubernetes-objects/declarative-config/>
- .NET options:
  <https://learn.microsoft.com/en-us/dotnet/core/extensions/options>
- JSON Schema Draft 2020-12:
  <https://json-schema.org/draft/2020-12>
- Docker bind mounts:
  <https://docs.docker.com/engine/storage/bind-mounts/>
- PostgreSQL transaction isolation:
  <https://www.postgresql.org/docs/current/transaction-iso.html>
- PostgreSQL explicit locking:
  <https://www.postgresql.org/docs/current/explicit-locking.html>

Only source-free functional facts were retained. No external source code,
schema, tests, migrations, prose, or assets were used.

## Missing Evidence And Uncertainty

- The plan records the exact v1alpha1 candidate setting/document allowlist;
  implementation must reconfirm each current registry definition and remove any
  mismatch rather than broadening authority.
- No stakeholder or self-hoster usability study validates the one-file wording
  or recovery guidance.
- No production evidence establishes demand for managed reconciliation.
- No current generic instance document entity exists, and none is planned for
  v1alpha1. Only the existing instance paid-policy aggregate is admitted.
- Jurisdiction-specific reporting, copyright, privacy, payment, refund, and
  liability duties were not assessed.
- Tavily MCP and Context7 MCP were requested but unavailable in the planning
  session; official documentation was retrieved through available web tools.

## Escalation

- Qualified Sunni scholarly review is required before future copy makes a
  religious-legal claim about configuration, moderation, payment fairness,
  organizer duty, or refund entitlement.
- Qualified legal review is required for jurisdiction-specific reporting,
  copyright, privacy, payment, liability, and retention claims.
- Security review is required before any future secret-reference or remote
  manifest design.
- A fresh I-VSD review is required before managed reconciliation, field
  takeover/deletion, automated pruning, operational payment state, or
  tenant-shaped partial manifests are introduced.
