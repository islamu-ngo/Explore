<!-- ABOUTME: I-VSD review for tenant configuration manifests and policy-gated event-report intake. -->
<!-- ABOUTME: Defines provider responsibility, stakeholder protections, and moral boundaries without issuing religious or legal rulings. -->

# I-VSD Consultancy Report: Tenant Configuration Manifest And Reporting-Intake Policy

Last Updated: 2026-08-21

## Scope

This report reviews provider-controlled decisions for:

- declarative Day 0 tenant configuration in self-hosted and multi-tenant deployments;
- exporting non-secret tenant configuration for portability and GitOps workflows;
- keeping event-report intake enabled by default;
- allowing a tenant to disable event-report intake only when effective event-publication policy prevents unvetted public publication;
- preserving local operation without requiring an external moderation provider;
- separating local report intake from optional Osprey, Coop, or other external synchronization.

The report covers product defaults, architecture, policy enforcement, operator behavior, tenant administration, user affordances, privacy, recovery, and accountability. It does not assess the merits of any specific external moderation provider.

## Claim Boundary

This is provider-responsibility design analysis, not a fatwa, Sharia certification, legal opinion, copyright-compliance certification, or proof that harmful content cannot be published. Selected Sunni ethical principles inform software-provider duties only within decisions controlled by ISLAMU Event and its operators.

Questions about whether a specific act is halal, haram, obligatory, or otherwise religiously classified require qualified Sunni scholarly review. Copyright-notice obligations, statutory reporting channels, and operator liability vary by jurisdiction and require qualified legal advice.

## Executive Recommendation

Approve the architecture only with the following boundaries:

1. Keep local event-report intake enabled by default and independent from external-provider routing.
2. Introduce an explicit tenant policy, provisionally keyed as `event_reporting.intake_enabled`.
3. Permit disabling intake only after backend enforcement proves that no unvetted actor can cause public publication.
4. Enforce the invariant in every mutation path, not only in UI or one command handler.
5. Preserve a separate correction, legal, and copyright contact channel even when event-report intake is disabled.
6. Ship declarative bootstrap before managed reconciliation; do not describe restart-time overwrite as GitOps reconciliation.
7. Exclude raw secrets from manifests and exports.
8. Make failure, skip, and application outcomes visible to operators without exposing report content, credentials, or tenant-private data.

## Findings By Severity

### Critical — External-provider disablement must not disable local accountability

**Provider-controlled decision:** Existing `Reporting:Mode` and `Reporting:Enabled` settings control optional external-provider behavior, while local canonical reporting remains available.

**Risk:** Reusing those settings for report intake would silently remove a user protection when an operator intended only to disable an external dependency.

**Required mitigation:** Add a separate tenant reporting-intake policy. Keep `LocalOnly` as the zero-external-dependency default. Preserve current external routing semantics.

**Stakeholders:** Reporters, event attendees, organizers, tenant administrators, self-hosting operators, and people affected by unsafe or incorrect listings.

**Principles/domains:** Amanah (trust), non-harm, justice, rights of people, architecture, UX, operations, and governance.

### Critical — Disablement must depend on effective publication capability

**Provider-controlled decision:** The platform chooses how user, organization, group, federation, import, automation, and administrative paths reach public publication.

**Risk:** Stored booleans or disabled UI controls can appear safe while another backend path still permits unvetted publication.

**Required mitigation:** Define and test an effective publication policy. Disablement is allowed only when every non-privileged path is either closed or forced through an enforced approval boundary. Direct API calls and concurrent settings changes must fail closed.

**Rejected shortcut:** Treating `RequireApproval=true` as sufficient without proving that create and publish handlers enforce it.

**Principles/domains:** Justice, non-harm, accountability, architecture, security, and evaluation.

### High — Closed publishing does not remove correction or rights channels

**Provider-controlled decision:** Event-report intake is an in-product workflow, not the only possible contact or complaint surface.

**Risk:** A vetted administrator can still publish incorrect, outdated, unsafe, defamatory, privacy-invasive, or rights-infringing material.

**Required mitigation:** Disabling event-report intake must not remove an operator contact, correction, legal, or copyright channel. Documentation must explain the difference.

**Principles/domains:** Rights of people, restitution, trust, support, and governance.

### High — Declarative configuration must not become covert control or secret replication

**Provider-controlled decision:** ISLAMU Event defines which settings may be imported, exported, inherited, or controlled by a deployment manifest.

**Risk:** A broad manifest can expose credentials, override tenant autonomy, flatten inherited governance into local overrides, or make UI changes appear successful when startup reconciliation later reverses them.

**Required mitigation:**

- use an explicit allowlist of manifest-configurable settings;
- exclude raw secrets from import and export;
- show when fields are deployment-managed;
- distinguish portable effective exports from tenant-owned override exports;
- defer managed reconciliation until field ownership, conflict, deletion, and drift behavior are explicit.

**Principles/domains:** Amanah, transparency, privacy, autonomy, portability, and operations.

### High — Startup failure must be atomic, legible, and recoverable

**Provider-controlled decision:** The platform determines whether invalid declarative state partially applies, starts unsafely, or fails before serving traffic.

**Risk:** Partial tenant creation or partial policy application can leave a deployment inconsistent while operators believe bootstrap succeeded.

**Required mitigation:** Read the file once, validate the complete document before writes, apply transactionally, record a non-secret digest and result, and fail readiness/startup on explicit-path or invalid-file errors. Existing tenants skipped by bootstrap must be reported clearly.

**Principles/domains:** Trust, competence, operational stewardship, and accountability.

### Moderate — Defaults and language must not overclaim moral or legal certainty

**Provider-controlled decision:** Documentation explains why reporting is available and what disabling it means.

**Risk:** Statements such as “moral liability eliminated” or “copyright enforcement guaranteed” can mislead operators and users.

**Required mitigation:** Explain the product rationale in plain operational language. Keep legal claims qualified and route disputed religious or legal conclusions to appropriate experts.

## Stakeholder Traceability

| Stakeholder | Primary interest | Provider-controlled protection |
|---|---|---|
| Event attendees and community members | Safe, accurate, correctable public listings | Default local reporting, correction channel, clear unavailability reason |
| Reporters | Privacy, acknowledgment, predictable handling | Local-first storage, bounded evidence sharing, stable API outcomes |
| Event organizers | Fair review and correction of mistakes | Explainable report categories, moderation records, correction path |
| Tenant administrators | Legitimate control without accidental unsafe combinations | Server-enforced invariant, manifest validation, clear managed-state status |
| Self-hosting operators | No mandatory external service, deterministic recovery | `LocalOnly` default, strict bootstrap, actionable startup diagnostics |
| Platform operators | Tenant isolation, auditability, supportability | Transactional apply, non-secret audit records, metrics and logs |
| Rights holders and affected third parties | Contact and correction for harmful or infringing listings | Independent legal/copyright contact surface |

## Principles And Product Domains

| Principle | Application |
|---|---|
| Amanah / trust | Configuration and UI must reflect the actual effective policy; hidden bypasses are unacceptable. |
| Justice / consistency | Equivalent publication paths must receive equivalent safety enforcement. |
| Non-harm | Local reporting remains the safe default, and invalid combinations fail closed. |
| Rights of people | Correction and legal-contact routes remain available even when event reporting is disabled. |
| Privacy and dignity | Manifests and audit logs contain no report evidence, credentials, or unnecessary personal data. |
| Autonomy and portability | Self-hosters can bootstrap and export configuration without an external control plane or provider lock-in. |

## Governance And Operational Recommendations

- Treat reporting-intake disablement as a high-impact tenant policy change and audit the actor, prior state, new state, and reason code.
- Record manifest version, mode, non-secret digest, tenant result, changed setting keys, and timestamps; never persist raw manifest secrets.
- Add metrics for manifest application outcomes and rejected reporting-policy transitions, tagged only with bounded non-sensitive dimensions.
- Return stable machine-readable failure codes and RFC 7807 responses for HTTP callers.
- Ensure UI explanations are localized, accessible, and based on server-authored state and HAL affordances.
- Review outcome data after deployment: rejected policy changes, reporting availability, correction-channel usage, and operator bootstrap failures.

## Rejected Alternatives

1. Reusing `Reporting:Mode=Disabled` for local intake — rejected because it changes established external-provider semantics.
2. UI-only disablement rules — rejected because direct API, plan, manifest, and internal writes would bypass them.
3. `AlwaysOverride` in the first release — rejected until field ownership, drift, conflict, and deletion semantics exist.
4. Exporting raw secrets — rejected because portability artifacts are not secret backups.
5. Removing every correction channel for closed publishers — rejected because vetted publication can still be wrong or harmful.
6. YAML in the first contract — rejected to keep parsing, duplicate-key behavior, schema tooling, and operational diagnostics deterministic.

## Validation And Evaluation Plan

Implementation evidence must demonstrate:

- every publication path obeys the effective submission and approval policy;
- all setting mutation paths reject an unsafe transition;
- report HAL links, options, direct submission, and UI agree on intake availability;
- invalid manifests produce no partial writes;
- restart and rerun behavior is idempotent;
- exported manifests omit sensitive values;
- wrong-tenant and unauthorized export attempts fail;
- standalone and split migration-service topologies apply the same bootstrap contract;
- operator logs identify the failing tenant and stable error code without exposing secrets.

Operational validation after release should examine rejection rates, startup failures, support cases, correction-channel accessibility, and whether operators misunderstand external-provider disablement as report-intake disablement.

## Validation Gaps

- No stakeholder interviews or tenant-operator usability studies were reviewed.
- No production moderation outcome data exists for the proposed policy.
- Jurisdiction-specific legal and copyright-channel requirements were not assessed.
- The repository does not yet prove that every event-publication path enforces the submission and approval settings.
- Managed reconciliation behavior is intentionally deferred and therefore not evaluated as an implemented capability.

## Escalation Needed

- Qualified legal review for jurisdiction-specific complaint, copyright, privacy, and retention obligations.
- Qualified Sunni scholarly review if future policy claims classify reporting, moderation, publication, or operator duties in religious-legal terms.
- Security review before allowing secret references beyond environment or approved secret-provider identifiers.
- Product/stakeholder review before broadening disablement to additional content types or community workflows.

## Evidence Reviewed

### Repository Evidence

- `src/Explore.Infrastructure/Configuration/ModerationProviderOptions.cs`
- `src/Explore.Infrastructure/Services/Moderation/ReportingRoutingPolicyResolver.cs`
- `src/Explore.Application/Settings/Groups/EventSettingGroup.cs`
- `src/Explore.Application/Settings/Groups/ReportingSettingGroup.cs`
- `src/Explore.Application/Services/Lifecycle/EventLifecyclePolicyProvider.cs`
- `src/Explore.Application/Features/EventReporting/Handlers/Commands/SubmitEventReportCommandHandler.cs`
- `src/Explore.Application/Features/EventReporting/Handlers/Queries/GetEventReportOptionsRequestHandler.cs`
- `src/Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
- `src/Explore.Application/Features/ControlPlane/Handlers/Commands/SetControlPlaneTenantSettingCommandHandler.cs`
- `src/Explore.Application/Features/ControlPlane/Handlers/Commands/ApplyControlPlaneTenantPlanAssignmentCommandHandler.cs`
- `src/Event.Standalone/Program.cs`
- `src/Event.MigrationService/Worker.cs`
- `src/Explore.Domain/Settings/SettingRegistry.cs`
- `docs/CONFIGURATION.md`
- `docs/SELF_HOSTING.md`
- `docs/SECRETS.md`

### External Functional References

Only source-free behavioral facts were used:

- Keycloak import/export: <https://www.keycloak.org/server/importExport>
- Keycloak Operator realm import: <https://www.keycloak.org/operator/realm-import>
- Grafana provisioning: <https://grafana.com/docs/grafana/latest/administration/provisioning/>
- JSON Schema Draft 2020-12: <https://json-schema.org/draft/2020-12>
- Kubernetes Server-Side Apply: <https://kubernetes.io/docs/reference/using-api/server-side-apply/>
- Docker bind mounts: <https://docs.docker.com/engine/storage/bind-mounts/>
- Docker environment files: <https://docs.docker.com/engine/containers/run/#env-files>

## Missing Evidence

- Confirmed stakeholder acceptance of the disabled-intake wording and correction-channel UX.
- A complete caller graph proving every policy-critical setting mutation route.
- A formal legal opinion for copyright or jurisdiction-specific reporting duties.
- Performance measurements for large multi-tenant bootstrap manifests.

## Context Inventory

- Code-defined hierarchical settings registry with tenant locks and sensitive-setting metadata.
- Existing tenant policy, control-plane plan, and reporting-provider settings surfaces.
- Local canonical report case and optional external provider routing.
- Standalone in-process migration and split one-shot migration-service topologies.
- HAL-driven Blazor affordance requirements.
- Existing provider-specific EF Core migration projects and architecture tests.

