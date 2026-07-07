<!-- ABOUTME: Senior CTO implementation plan for completing the Control Plane Blazor v1.0 governance platform. -->
<!-- ABOUTME: Captures repository evidence, product scope, architecture decisions, delivery phases, risks, and verification gates. -->

# Control Plane Blazor v1.0 Implementation Plan

Last Updated: 2026-07-07 Europe/Brussels

## 0. Metadata

Task name: `control-plane-blazor-v1`

Request: Continue implementation for a fully working Control Plane Blazor v1.0 app. Tenant plans mean SaaS pricing tiers such as Starter, Community, Enterprise, and Self-hosted default: each tier has price metadata, defaults, locks, quotas, feature availability, and provisioning behavior. Tenant provisioning APIs must be able to provision a tenant from a selected plan/tier. Instance admins can update plans through versioned changes.

Product thesis: Control Plane v1.0 is not just an admin dashboard. It is the instance administrator's operator console and SaaS governance/product-control platform for running hosted tiers and self-hosted instances safely.

Owner context: The project is in development mode, so placeholder routes and incomplete contracts can be replaced without backward-compatibility ceremony. Persisted data still needs safe migrations, constraints, and rollback strategy.

Primary projects:

- `Event.ControlPlane.Blazor`: dedicated Interactive Server BFF host and API adapter.
- `Event.ControlPlane.Client`: host-neutral Razor class library for Control Plane pages, services, routes, and UI contracts.
- `Explore.API`: control-plane HTTP endpoints, HATEOAS assemblers, and route names.
- `Explore.Application`: CQRS queries/commands, validators, authorization metadata, and DTO mapping.
- `Explore.Domain`: new tenant-plan governance entities only if required by implementation evidence.
- `Explore.Infrastructure` and `Explore.Persistence`: settings resolution, identity/admin context, repositories, EF configuration, migrations, outbox, quotas, and storage/email provider integration.

Composite intent coverage:

- No exact `finish Control Plane app` or `tenant plan studio` intent exists in `.claude/contract/intents.yaml`.
- Use slice intents: `bff-auth-bug`, `blazor-component-affordance`, `add-get-endpoint`, `add-write-endpoint`, `add-hal-link`, `add-cqrs-handler`, `update-repository-query`, `add-ef-migration`, `cerbos-policy-change`, `openapi-contract-change`, and `external-infrastructure-bootstrap` as each PR requires.
- If this work recurs, add a dedicated Control Plane intent before broad implementation.

## 1. Executive Summary

The existing plan was correct but too narrow. It treated Control Plane v1.0 mostly as page completion. The CTO-level scope is larger: the Control Plane must let an instance administrator operate infrastructure, define SaaS pricing tiers, apply and lock tenant capabilities, manage quotas, view effective configuration, and govern self-hosted operations without touching setup secrets, direct database state, or tenant business data.

The repository already has major foundations: a dedicated BFF host, RCL pages, generated admin API client, control-plane API/Application handlers, hierarchical settings, instance/tenant lock semantics, storage quota governance, email-dispatch admin concepts, external API key quota infrastructure, admin hierarchy docs, and DB-derived instance-admin authority. The missing foundation is a canonical SaaS tenant-plan aggregate. No verified `TenantPlan`, `PlanTemplate`, `SubscriptionPlan`, or pricing-tier aggregate exists in code today.

The sequence must not jump straight into Plan Studio UI. First, make the dedicated host reliably call the admin API, because the current app still shows `Control-plane API unavailable`, `The control-plane API resource was not found.`, and adapter fail-closed messages. Second, define the governance model for tenant plans as versioned SaaS tiers over existing hierarchical settings, quotas, and locks. Third, implement API/Application/HAL contracts. Fourth, build RCL pages and workflows. Fifth, finish operational sections and harden security, accessibility, observability, and runbooks.

The senior CTO decision: split the work by risk boundary, not by navigation page. Connectivity, governance model/data, API contract, RCL UI, operations, and docs/tests should be separate PR families. A single giant Control Plane PR would be high-risk and unreviewable.

## 2. Evidence Log

Repository docs and rules read:

- `AGENTS.md`: contribution contract, critical rules, verification baseline, final teaching-summary requirement.
- `.claude/contract/intents.yaml`: no exact Control Plane v1 or tenant-plan intent; composite slice contract required.
- `docs/QUICK_REFERENCE.md`: repositories return entities, manual validators, ID conventions, user-id fallback, HAL affordances, tenant isolation, build/test baseline.
- `docs/GOVERNANCE.md`: layer ownership, path-to-rule routing, no improvising when an intent is missing.
- `docs/ARCHITECTURE.md`: Clean Architecture, CQRS, BFF, MediatR authorization, HATEOAS batch pipeline, outbox, tenant runtime modes.
- `docs/SECURITY-MODEL.md`: dedicated Control Plane BFF with confidential Keycloak client, HttpOnly cookie, server-side token forwarding, instance-admin-only BFF policy.
- `docs/BLAZOR.md`: separate `Event.ControlPlane.Blazor` host, `Event.ControlPlane.Client` RCL, Interactive Server-only control plane, no browser tokens.
- `docs/API.md`: middleware order, API versioning, ProblemDetails, email-dispatch admin surface, rate limiting, JWT validation, HATEOAS rules.
- `docs/AUTHORIZATION.md`: endpoint plus MediatR authorization, DB-derived instance administrator authority, fail-closed authorization.
- `docs/ADMIN_HIERARCHY.md`: instance admin versus tenant admin boundaries, lock cascade, delegation, emergency access, audit requirements, tenant offboarding.
- `docs/MULTI_TENANCY.md`: deployment modes, admin-host exclusion, tenant resolution, hierarchical settings, lock cascade, single-tenant bypass caveat.
- `docs/STORAGE.md`: provider-neutral storage policy, tenant delegation lock, quota flow, redacted admin UI, storage metrics, backup/restore impact.
- `docs/OPERATIONS.md`: Aspire startup topology, Control Plane host startup, health/readiness, metrics, storage and moderation reporting operational signals.
- `docs/DESIGN_SYSTEM.md`: CSS layers, design tokens, local Control Plane primitives, MudBlazor wrapper rules.
- `docs/ACCESSIBILITY.md`: WCAG 2.2 AA, heading structure, labels, focus, live regions, RTL via logical properties.

Skills and rules read:

- `senior-cto-feedback`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `error-tracking`, `outbox-pattern`.
- `.claude/rules/blazor-server.md`, `.claude/rules/blazor-client.md`, `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/application-layer.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/tests.md`.

Context7 evidence:

- ASP.NET Core Blazor docs confirm the dedicated host shape: `AddRazorComponents().AddInteractiveServerComponents()`, `MapRazorComponents<App>().AddInteractiveServerRenderMode()`, app-wide `InteractiveServer` render mode, and routable RCL components through additional assemblies.
- YARP docs confirm route/cluster transforms for path and header manipulation. Tokens and privileged headers must stay in server-side transforms/delegating handlers.
- MudBlazor docs confirm current layout/data-display primitives. Use existing project wrappers/current MudBlazor APIs instead of a parallel UI framework.

Code evidence:

- `Event.ControlPlane.Blazor/Program.cs` already wires a dedicated Control Plane BFF host, Keycloak auth, antiforgery, YARP API proxy, bearer forwarding, `ControlPlaneBffCookieSessionHandler`, `IEventApiClient`, `ControlPlaneApiAdapter`, Interactive Server components, RCL additional assembly routing, and `ControlPlaneAccess` authorization.
- `Event.ControlPlane.Blazor/Services/ControlPlaneApiAdapter.cs` maps generated API calls into RCL service contracts and maps HTTP failures to UI problem messages. The visible `resource was not found` message comes from its 404 mapping.
- `Event.ControlPlane.Client` already has routes and real pages for overview, tenants, domains, and operations, plus placeholders for onboarding, health, storage, jobs, security, and policies.
- `Explore.API/Controllers/ControlPlaneController.cs` already exposes overview, domains, operations, tenant list/detail, tenant create, and tenant lifecycle commands.
- `Explore.API/Controllers/SettingsController.cs` exposes tenant hierarchical settings read/update/lock/unlock endpoints.
- `Explore.API/Controllers/InstanceSettingsController.cs` exposes instance governance endpoints for modules, event policy, organization policy, branding, domains, tenant delegation, AI assistant, and more.
- `Explore.Application/Features/ControlPlane/**` already contains CQRS handlers for current Control Plane reads and commands.
- `Explore.Application/Contracts/Identity/IAdminContext.cs` and `Explore.Infrastructure/Identity/AdminContext.cs` implement DB-derived admin authority.
- `Explore.Persistence` and tests show existing quota infrastructure for storage and external API keys, plus `QuotaExceededException` and quota ProblemDetails contracts.
- No verified `TenantPlan`, `PlanTemplate`, `SubscriptionPlan`, or tenant-plan aggregate exists in C# code today.

## 3. Current State Report

Working foundations:

- Dedicated Control Plane BFF host exists and is correctly separated from the public Blazor host.
- RCL route catalog, service interfaces, and fail-closed fallback clients exist.
- Overview, tenants, domains, and operations pages exist as RCL pages.
- API/Application backend already provides substantial read models and commands.
- BFF-level instance-admin shell authorization works and denies non-admins before shell rendering.
- Hierarchical settings and lock semantics already exist across instance, tenant, organization, group, and user scopes.
- Storage quotas and tenant delegation locks already exist.
- Email-dispatch admin controls and external API key quota infrastructure already exist.
- Admin hierarchy docs already define instance admin boundaries, tenant admin delegation, locks, audit, and emergency access.

Broken user-visible states:

- Existing pages can render `Control-plane API unavailable` and `The control-plane API resource was not found.`
- Overview still has adapter-not-configured copy that is wrong for the dedicated host.
- Operations can render `Deployment-mode runbook unavailable` independently from API availability.
- Tenants renders `Create tenant` disabled even when HAL create affordance exists.
- Domains renders Verify/Test/Retry buttons disabled because the RCL service contract has no command methods.
- Onboarding, health, storage, jobs, security, and policies are placeholders.

Strategic product gaps:

- There is no tenant-plan/pricing-tier model even though the requested product needs one.
- There is no Plan Studio for instance admins to define priced SaaS tiers, capability bundles, locks, and quota defaults.
- There is no Per-Tenant Configuration Center showing effective values, inheritance source, lock source, and plan assignment.
- There is no first-class plan assignment, preview, diff, publish, apply, rollback, version update, or audit workflow.
- There is no single operator view combining tenant plan, tenant settings, quotas, lifecycle, domains, storage, email, API keys, AI/MCP/render policy/footer/branding governance.

## 4. Future State

Control Plane v1.0 must let an authenticated instance administrator run the instance safely from `/admin/instance` without setup secrets, API keys, direct DB access, tenant impersonation, or browser role checks.

The v1.0 shell must provide:

- Overview: instance mode, public origins, provider health, tenant counts, warnings, and remediation links.
- Tenant Plan Studio: create, draft, validate, publish, archive, clone, and version SaaS tenant plans with price, currency, billing period, and provisioning status.
- Tenant Configuration Center: view a tenant's effective configuration, plan assignment, inherited settings, lock source, quotas, usage, and overrides.
- Tenants: list/detail, create tenant from a selected plan/tier, assign plan during provisioning, activate/suspend/archive/reactivate/schedule purge, audit visibility, and HAL-gated lifecycle actions.
- Domains: required DNS records, platform/admin/tenant domain status, verify/test/retry commands, and safe remediation guidance.
- Operations: deployment-mode runbook, mode transition, outbox/email/moderation/storage status, job status, retry controls where safe, and warning drill-down.
- Onboarding: first-run/admin checklist using overview, domain, health, storage, security, policy, and plan readiness signals.
- Health: API/BFF/database/storage/email/auth/authorization/federation status with redacted diagnostics.
- Storage: provider status, quotas, usage, delegation lock, test/recalculate actions where HAL allows.
- Jobs: outbox/background worker status, dead-letter counts, retryable failures, and safe retry/drain actions.
- Security: auth provider, admin authority source, authorization mode, CORS/origin posture, security headers, secret/config status, and remediation warnings.
- Policies: authorization/Cerbos/local fallback mode, policy sync state, tenant lock policy, policy-change outbox state, and safe resync/reload actions if supported.

Tenant plans must be product-level SaaS tiers, not a second settings system. A plan should compile to existing typed settings, tenant settings, quotas, locks, module settings, and governance policies. Updating a published plan creates a new version; existing tenant assignments move only through an explicit preview/apply decision.

## 5. Non-Negotiable Constraints

- Browser code never sees access tokens, setup secrets, API keys, admin authority headers, privileged transport headers, raw connection strings, object storage keys, provider credentials, or policy package secrets.
- BFF cookie authentication and server-side bearer forwarding remain the only browser-to-API path.
- User ID extraction must preserve `sub -> nameidentifier -> sid` wherever identity is resolved.
- DB-derived instance administrator authority is authoritative. Do not trust Keycloak roles as instance-admin source of truth.
- HAL `_links` is the only source of truth for UI action affordances.
- Instance admins configure infrastructure and tenant governance; they do not access tenant business data or impersonate tenant users.
- Tenant admins can only manage delegated, unlocked settings within instance limits.
- Single-tenant mode bypasses some instance locks for the default tenant as documented; multi-tenant mode must fail closed on unresolved tenant routing.
- Controllers stay thin and use `RouteNames` constants.
- CQRS handlers own DTO mapping and manually instantiate validators.
- Repositories return entities, not DTOs or `IQueryable`.
- Tenant isolation filters cannot be disabled in runtime request paths.
- Plan application must not bypass existing hierarchical settings, lock semantics, quota enforcement, rate limiting, or authorization checks.
- Control Plane RCL remains host-neutral and must not depend on host-specific BFF security primitives.
- The dedicated host remains Interactive Server-only. Do not add WASM or InteractiveAuto to Control Plane.
- Use project-level tests only. Never run solution-level `dotnet test`.
- Every edited file keeps or adds two-line `ABOUTME:` comments.

## 6. Architecture Decisions

Decision 1: Complete existing architecture before adding product breadth.

Keep `Event.ControlPlane.Client` RCL pages and service interfaces, `Event.ControlPlane.Blazor` host adapters, generated API clients, `Explore.API` controllers, and `Explore.Application` CQRS handlers. Add missing contracts only where evidence shows a gap.

Decision 2: Adapter/API connectivity is phase one.

No Plan Studio or new page work should start until overview, tenants, domains, operations, and runbook calls work through the dedicated host with authenticated server-side API calls.

Decision 3: Tenant plans are a governance aggregate over existing settings, not a parallel stack.

The tenant-plan model should reference or emit existing setting keys, typed setting documents, quota defaults, locks, and module policies. It must reuse `SystemSetting`, `TenantSetting`, hierarchical resolution, storage quotas, quota ProblemDetails, and admin hierarchy semantics.

Decision 4: Add a canonical plan model deliberately.

Because no verified plan aggregate exists, introduce a small explicit model such as `TenantPlan`, `TenantPlanVersion`, and `TenantPlanAssignment` only after a short technical design slice. Keep the model versioned, auditable, and migration-safe.

Decision 5: Plan application must be previewable and idempotent.

Before applying a plan, show a diff of setting writes, lock changes, quota changes, and unsupported/deprecated keys. Applying a plan must be repeatable, auditable, and safe on partial failure. Bulk apply should use outbox/idempotency if it touches many tenants.

Decision 6: Read-only diagnostics precede mutation controls.

For health, storage, jobs, security, and policies, ship redacted read models first. Add probes, retries, resync, and reload commands only where infrastructure supports safe, bounded, authorized mutation.

Decision 7: Split by risk boundary.

Use separate PR families for connectivity, domain/persistence model, API/Application/HAL contract, RCL UI, operations/docs, and security test hardening.

## 7. Product Capability Model

### Tenant Plan Studio

Tenant Plan Studio lets an instance admin define SaaS tiers such as `Starter`, `Community`, `Enterprise`, or `Self-hosted default`. A plan version can define:

- Stable plan key, display name, public/private visibility, and whether the plan is available for tenant provisioning.
- Price amount, currency, billing period, and operator-facing billing notes. Billing collection/provider integration is out of scope until a payment provider is selected.

- Enabled/disabled modules and feature flags.
- Feature locks and delegation flags.
- Storage provider policy, default tenant quota, upload ceiling, and tenant storage delegation lock.
- Email sending quota, dispatch controls, replay/park permissions, and provider policy where supported.
- External API key quota defaults and limits.
- AI assistant governance defaults and tenant override locks where existing settings support it.
- MCP runtime settings and locks where existing settings support it.
- Render policy delegation, footer locks, branding, domain policy, moderation reporting delegation, auth provider constraints, authorization provider constraints, and onboarding defaults.
- Required setup checklist items and warnings.

No secrets belong in a plan. Plans may reference secret-backed providers by policy name or setting key, but never store secret values. Updating a plan means creating and publishing a new version; assigned tenants keep their current version until an instance admin previews and applies an upgrade/downgrade.

### Tenant Configuration Center

Per tenant, an instance admin must be able to:

- See assigned plan, plan version, assignment date, and assignment actor.
- See effective settings grouped by domain: modules, storage, email, API keys, AI, MCP, rendering, footer, branding, domains, moderation/reporting, auth, authorization.
- See each effective value's source: plan, instance default, tenant override, organization/group/user override, or runtime fallback.
- See lock source and whether tenant admins can change it.
- Apply a plan, preview a plan diff, override selected values, lock/unlock settings, and rollback to a prior plan assignment.
- See quota usage versus quota limits.
- See audit history for plan and lock changes.

### Tenant Provisioning Wizard

Tenant creation should become a safe wizard:

- Choose tenant identity, slug, display name, admin contact, default domain mode, and SaaS plan key/version.
- Preview resulting settings, locks, quotas, and warnings before create.
- Create the tenant transactionally through Application commands.
- Schedule non-transactional side effects through outbox where needed.
- Show post-create remediation links to domains, storage, email, and plan details.

## 8. Data, Persistence, and Migration Plan

Foundation design slice:

- Confirm whether tenant plans should live in Domain entities or typed settings documents. Default decision: use Domain entities for pricing-tier metadata/versioning/assignment, and compile plan contents into typed settings/quotas.
- Proposed entities if design confirms need: `TenantPlan`, `TenantPlanVersion`, `TenantPlanAssignment`, and `TenantPlanApplicationLog`.
- Plan metadata must include stable key, display name, price amount, currency, billing period, active-for-provisioning flag, and version status.
- Use `Guid` UUIDv7 for aggregates. Use `int` only for lookups and `long` for cursors.
- Add unique constraints for plan key/version and one active plan assignment per tenant.
- Store plan payload as typed, validated documents or strongly typed child records. Avoid arbitrary unvalidated JSON blobs unless existing typed-settings infrastructure already validates the document.
- Add indexes for tenant assignment lookup, active published plans, and audit log query.
- Do not edit applied migrations. Add focused migrations only.

Migration posture:

- Development mode allows replacing placeholder routes and incomplete contracts.
- Persisted plan/assignment data still needs forward migrations and rollback notes.
- Seed a minimal `Default` or `SelfHostedDefault` plan only if implementation needs a safe baseline; do not seed opinionated SaaS pricing tiers as business truth.
- Existing tenants should show `No assigned plan` until a plan is explicitly applied or a migration intentionally assigns the default.
- Existing tenant assignments must not silently follow plan edits. Upgrade/downgrade uses explicit preview, typed confirmation, and audit.

## 9. API and Contract Completion Plan

Connectivity first:

- Add an integration test proving the dedicated host uses real `ControlPlaneApiAdapter` registrations and can call generated `IEventApiClient` methods for overview, tenants, domains, operations, and deployment-mode runbook.
- Verify generated paths, base address, route prefix, `api-version`, `X-Api-Version`, media type, and proxy behavior against `Explore.API/Controllers/ControlPlaneController.cs`.
- Fix only the smallest proven mismatch: generated client invocation parameters, base address resolution, route constants, OpenAPI generation, or BFF proxy configuration.

Plan/governance API slices:

- Plan list/detail/read effective configuration.
- Plan create/update draft/publish/archive/clone with price, currency, billing period, and active-for-provisioning metadata.
- Plan diff/validate against current settings registry and tenant limits.
- Assign plan to tenant with preview and typed confirmation, including provisioning-from-plan for new tenants.
- Roll back plan assignment to prior version where safe.
- Per-tenant effective configuration read with value source and lock source.
- Per-tenant override/lock/unlock commands reusing existing settings endpoints where possible.
- Quota read/update commands for storage, email dispatch, external API keys, AI/MCP if existing quota infrastructure supports those domains.

Endpoint rules:

- Add `RouteNames` constants and use them in controllers and HATEOAS policies.
- Add CQRS request/handler in `Explore.Application`.
- Add validators manually in handlers.
- Add `[AuthorizeResource]` metadata and policy tests.
- Add HATEOAS links and fail closed.
- Add OpenAPI/generated client updates through the established workflow.
- Map generated client calls in `ControlPlaneApiAdapter` to RCL service contracts.

## 10. UI and RCL Completion Plan

Shared UI rules:

- RCL pages call RCL service interfaces only.
- Pages never inspect roles or claims to decide actions.
- Pages render action controls only when HAL `_links` expose those actions.
- MudBlazor usage follows current APIs and local Control Plane primitives.
- Component CSS uses colocated `.razor.css`, BEM, logical properties, and no unscoped `.mud-*` overrides.
- Every mutation has visible labels, accessible validation, command result announcement, and focused remediation.

Page targets:

- `Plans`: Plan Studio list/detail/editor, version history, publish/archive/clone, validation results.
- `Tenant Configuration`: effective setting explorer, plan assignment, diff preview, override/lock/unlock actions, quota usage.
- `Tenants`: create tenant wizard with plan selection, lifecycle, audit, and plan summary.
- `Domains`: verify/test/retry commands, DNS checks, timestamps, redacted guidance.
- `Operations`: deployment-mode runbook, job/outbox/email/moderation/storage status, safe retry actions.
- `Health`: redacted service health dashboard.
- `Storage`: provider diagnostics, quota usage, delegation lock, safe test/recalculate controls.
- `Jobs`: background-processing dashboard with guarded retry/drain affordances.
- `Security`: auth/admin-authority/authorization/origin/security-header posture.
- `Policies`: policy provider status, tenant lock posture, policy sync/outbox state.
- `Onboarding`: aggregate readiness checklist after the underlying sections exist.

## 11. BFF and Host Integration Plan

Host responsibilities:

- Keep `Event.ControlPlane.Blazor/Program.cs` as composition root.
- Keep `IEventApiClient` configured with `UseCookies=false` and `EventBffBearerForwardingHandler`.
- Keep `ControlPlaneBffCookieSessionHandler` as DB-derived admin-claim enrichment for the BFF shell.
- Keep `MapRazorComponents<App>().AddInteractiveServerRenderMode().AddAdditionalAssemblies(ControlPlaneClientAssembly.Value)`.
- Keep `RequireAuthorization(EventBffAuthorizationPolicies.ControlPlaneAccess)` on Control Plane components and protected proxy routes.
- Strip browser-supplied privileged headers and set server-authoritative headers/tokens only server-side.

Integration tests must prove:

- Non-admins receive BFF 403 and cannot render the shell.
- Instance admins can render the shell and call Control Plane adapter services.
- Browser requests cannot spoof privileged headers.
- API calls receive server-side bearer tokens and no browser-visible tokens.
- Unconfigured fallback cannot leak into the dedicated host when real adapters are registered.
- Plan and tenant-configuration actions remain HAL-gated.

## 12. Section-by-Section Delivery Plan

Phase 1: Make existing pages real.

- Fix overview/tenants/domains/operations/runbook adapter/API connectivity.
- Remove misleading dedicated-host fail-closed copy.
- Add tests around the current four pages and host adapter.

Phase 2: Design and prove governance foundation.

- Inventory setting keys, typed setting documents, quota domains, lock keys, and existing admin endpoints.
- Decide and document the canonical SaaS tier model, including pricing metadata and versioned update semantics.
- Add failing tests for plan validation, price metadata, sensitive/unsupported settings, quota domains, diff, assignment, and lock-source behavior before implementation.

Phase 3: Implement tenant plan data and Application/API contracts.

- Add entities/repositories/migrations only after Phase 2 design is accepted.
- Add CQRS commands/queries, validation, HATEOAS, OpenAPI, generated client, and adapter mappings.
- Add plan assignment, provisioning-from-plan, rollback, and effective configuration endpoints.

Phase 4: Build Plan Studio and Tenant Configuration Center.

- Implement RCL services/pages and dedicated-host adapter methods.
- Add accessible forms, diff previews, typed confirmations, and audit views.

Phase 5: Finish tenant/domain/operations workflows.

- Implement tenant create wizard with plan selection, price/tier summary, and plan assignment.
- Complete domain verify/test/retry backend and UI.
- Improve operations/runbook/job status and safe actions.

Phase 6: Replace operational placeholders.

- Health, storage, jobs, security, and policies pages.

Phase 7: Add onboarding.

- Build the first-run/remediation checklist last because it aggregates completed section signals.

Phase 8: Product hardening.

- Accessibility, responsive design, operator copy, observability, docs, runbooks, and full verification matrix.

## 13. Testing and Verification Matrix

Run only project-level tests.

Baseline after any non-trivial slice:

```bash
dotnet build --configuration Release --verbosity quiet
```

BFF/Control Plane host:

```bash
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
```

RCL/component behavior:

```bash
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

API/HATEOAS/security:

```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
```

Application handlers:

```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
```

Persistence/repository changes:

```bash
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
```

Architecture/rule enforcement:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Manual QA gate:

- Start the Aspire/local app stack used by this repo.
- Sign in as an instance administrator through Keycloak.
- Navigate every route under `/admin/instance`.
- Confirm no placeholder/fallback panels remain for shipped v1.0 sections.
- Confirm non-admin receives the BFF 403.
- Confirm HAL-gated actions appear/disappear based on API links.
- Confirm plan apply/diff/rollback and tenant locks never expose secrets or tenant business data.

## 14. Docs, Configuration, and Operations

Docs to update during implementation:

- `docs/BLAZOR.md`: final Control Plane host/RCL responsibilities and route map.
- `docs/SECURITY-MODEL.md`: final Control Plane auth, BFF, token forwarding, admin authority, and no-secret boundaries.
- `docs/API.md`: new Control Plane endpoint groups, API versioning, HATEOAS affordances, quota ProblemDetails.
- `docs/AUTHORIZATION.md`: new resource/action policies and Cerbos/local fallback updates.
- `docs/ADMIN_HIERARCHY.md`: any new delegation or plan-assignment authority rules.
- `docs/MULTI_TENANCY.md`: plan assignment, effective config, lock-source behavior, single-tenant caveats.
- `docs/STORAGE.md`: storage plan/quota integration and backup/restore notes.
- `docs/OPERATIONS.md`: operator runbooks for plans, health, storage, jobs, security, policies, onboarding, deployment-mode changes.
- `docs/DESIGN_SYSTEM.md` only if new reusable Control Plane primitives become canonical.
- `dev/_journal/journal.md` for durable non-obvious findings.

Configuration to verify:

- `ControlPlane:PublicOrigin`, `Bff:PublicOrigin`, `PublicBaseUrl`, `App:PublicBaseUrl`, admin host settings, Keycloak client ID, API base address, storage settings, SMTP settings, email dispatch settings, external API key quota settings, AI/MCP governance settings, authorization provider settings, Cerbos settings, outbox settings, and health check endpoints.

Operational posture:

- Every status card must say whether the issue is API unavailable, unauthorized, not configured, degraded, locked by higher scope, quota exceeded, or action required.
- Operator guidance must be redacted. Never reveal secrets, tokens, setup secret values, provider credentials, raw policy package data, bucket names where sensitive, or connection strings.

## 15. Security, Auth, Privacy, and Abuse

Security requirements:

- Preserve `ControlPlaneAccess` BFF policy.
- Preserve API/Application authorization as authoritative for every action.
- Use DB-derived admin authority through existing identity/admin-context flows.
- Strip browser-supplied privileged headers.
- Do not expose tokens or secrets to RCL pages, serialized auth state, logs, ProblemDetails, or plan payloads.
- Mutating actions need typed confirmations when destructive, quota-changing, lock-changing, or mode-changing.
- Add rate limiting or reuse existing policies for expensive diagnostic/probe/retry endpoints.
- Redact provider details that could help attackers enumerate infrastructure.
- Do not let instance admins read tenant business content through Control Plane diagnostics.

Abuse scenarios to test:

- Non-admin tries to access shell.
- Tenant admin tries to access instance-only Plan Studio.
- Admin without a specific HAL link invokes a hidden route.
- Browser attempts to spoof setup secret, tenant, support, or admin headers.
- Expired token reaches typed API client.
- Plan apply attempts to unlock security settings that instance policy must keep locked.
- Tenant override attempts to exceed instance quota ceilings.
- Domain verify/test endpoint is spammed.
- Retry/drain job command is repeated.

## 16. Multi-Tenancy, Observability, Migration, and Success Metrics

Multi-tenancy:

- Single-tenant and multi-tenant mode must show different warnings and allowed actions.
- Never disable tenant filters in runtime request paths.
- Instance admin workflows may operate across tenants only through bounded, audited, non-business-data queries.
- Tenant lifecycle commands and plan assignments must keep audit logs and safe state transitions.

Observability:

- Add structured logs and metrics for plan create/update/publish/archive/assign/apply/rollback, lock changes, quota changes, quota denials, plan validation failures, and plan apply failures.
- Use bounded labels only: status, domain, action, plan state. Avoid tenant IDs and high-cardinality values in metric labels unless the existing metrics policy explicitly allows them.
- If plan application performs multiple writes or external side effects, use outbox/idempotency and expose retry/dead-letter status in Operations.

Migration and compatibility:

- No public backward compatibility requirement for placeholder UI routes.
- Persisted plan entities, assignments, quotas, and settings require migrations, constraints, and rollback notes.
- Existing tenants should not silently receive restrictive plan locks unless a migration explicitly states and tests that behavior.

Success metrics and definition of done:

- All `/admin/instance` routes render real data or precise remediation, never generic placeholder copy.
- Instance admin can create/publish a plan, preview and apply it to a tenant, view effective configuration and lock source, and roll back safely where supported.
- Tenant admin capabilities respect plan locks and instance ceilings.
- Quota exceeded paths return stable ProblemDetails and are visible in Control Plane without leaking secrets.
- All actions are HAL-gated and server-authorized.
- Relevant project-level tests pass.
- Manual browser QA passes for instance admin and non-admin flows.

## 17. Risk Register and Implementation-Agent Contract

Top risks:

- Connectivity work is skipped and new UI is built on broken adapter/API assumptions.
- Tenant plans duplicate or bypass existing hierarchical settings and lock semantics.
- Plan application becomes a dangerous bulk writer without preview, audit, idempotency, or rollback.
- Instance admin tools accidentally expose tenant business data or secrets.
- Quota and lock enforcement drift between UI, API, Application, and Persistence.
- One huge PR mixes schema, API, UI, and operational changes and becomes unreviewable.

Implementation-agent contract:

- Start every slice by reading this plan, context, tasks, and matching rules/skills.
- Work only one risk boundary at a time.
- Write the smallest failing test that proves the current gap before changing product code.
- Preserve Clean Architecture dependency direction.
- Update generated clients only through the established OpenAPI workflow.
- Update `control-plane-blazor-v1-context.md` after every session with files changed, tests run, decisions, and blockers.
- Add durable findings to `dev/_journal/journal.md` when discovering non-obvious route, auth, settings, lock, quota, or migration behavior.

Progress-reporting contract:

- Report by phase and risk boundary, not by vague percent complete.
- Every report must list completed files, verification commands/results, open risks, and next smallest slice.
