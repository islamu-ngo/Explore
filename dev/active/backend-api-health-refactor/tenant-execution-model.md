<!-- ABOUTME: Tenant execution model artifact for backend/API health refactor Phase 0. -->
<!-- ABOUTME: Defines runtime, host-admin, background, migration, and design-time tenant behavior. -->

# Tenant Execution Model

Last Updated: 2026-06-14 Europe/Brussels

## Purpose

This artifact defines how tenant context is interpreted by runtime requests, host administration, background jobs, migrations, seeding, and design-time operations. It prevents missing tenant context from becoming an implicit all-tenant query.

## Execution Modes

```csharp
public enum TenantExecutionMode
{
    RuntimeTenantRequired,
    RuntimeTenantOptionalPublicRead,
    HostAdministration,
    BackgroundSystem,
    MigrationOrSeeding,
    DesignTime
}
```

| Mode | Intended Call Sites | Missing Tenant Behavior | Can Cross Tenant Boundaries? | Audit Required? |
|---|---|---|---|---|
| `RuntimeTenantRequired` | Normal authenticated tenant-scoped API requests and writes. | Fail closed: 400/401/403/no rows depending endpoint contract. | No. | For writes/security-sensitive reads. |
| `RuntimeTenantOptionalPublicRead` | Public reads where tenant is resolved by host/domain/path, global platform-level public data, or explicit public read model. | Never query all tenant rows silently. Return no rows or public platform data only; never return identity/membership/role/grant data merely because the endpoint is a GET. | Only through explicit public/cross-tenant read model. | Yes for explicit cross-tenant read model. |
| `HostAdministration` | Platform/host admin services, diagnostics, tenant summaries. | Requires host-admin policy and reason. | Yes, through explicit APIs only. | Always. |
| `BackgroundSystem` | Background processors, outbox dispatchers, federation sync, scheduled jobs. | Requires operation name and reason. | Yes, only through explicit system service APIs. | Always for tenant bypass/cross-tenant work. |
| `MigrationOrSeeding` | Startup migration, lookup seeding, tenant bootstrap seeding. | Allowed by migration/seeding path only. | Yes, if migration/seed requires. | Log operation; audit if security-sensitive data changes. |
| `DesignTime` | EF tooling/design-time model creation. | No runtime data access expected. | No runtime queries. | No. |

## Existing Intentional Exceptions to Preserve Explicitly

- Instance/global default rows currently exist for lookup-like data such as `EventType` and footer link groups; these must be modeled as global/platform defaults rather than accidental all-tenant reads.
- API-key tenant resolution currently defers tenant binding until authentication; mismatched API-key tenant and requested tenant should continue to fail closed.
- Instance-admin API keys that proceed without a bound tenant must be represented as `HostAdministration`, require explicit capability/reason semantics, and emit audit/structured logs.
- Tenant resolution paths such as host/subdomain/domain lookup may need constrained tenant-filter bypasses, but only through named tenant-resolution services with explicit predicates and tests.

## Self-Hosted Bootstrap Decision

Phase 1E must implement the following target behavior before privileged policy hardening is considered complete:

| Decision Point | Target Behavior | Blocking Question / Verification |
|---|---|---|
| First platform administrator | Created only by `POST /api/instanceonboarding/complete` while onboarding is incomplete. Completion requires both an authenticated identity and a valid `X-Setup-Secret`; the setup secret alone is never a platform-admin identity. | Locked by API tests: valid setup secret without authentication returns `401 Unauthorized`; authenticated request without setup secret returns `403 Forbidden`. |
| First tenant administrator | In `SingleTenant` mode, onboarding also grants the first platform administrator the default tenant administrator role and provisions default tenant branding. In `MultiTenant` mode, onboarding grants platform administration only; tenant administrator creation is explicit per tenant after bootstrap. | Locked by Application unit tests for SingleTenant and MultiTenant role-grant differences. |
| Setup secret authority | `X-Setup-Secret` is a bootstrap credential for setup-only endpoints before completion. It is disabled after onboarding completion; setup-secret-protected endpoints return `410 Gone` once setup mode is inactive. | `validate-secret` already has 410 coverage; Phase 1E adds 410 coverage for an internal setup endpoint. |
| Keycloak/group authority | After auth provider configuration is active, admin capability comes from configured identity/provider claims plus resource authorization. Keycloak bootstrap may be setup-secret-gated before completion; post-bootstrap rotation/sync remain provider-admin/resource-authorized operations. | R-004 tests now prove sync apply blocks without backup confirmation and rotation persists/reloads only after Keycloak accepts the application-managed secret update. |
| Bootstrap disablement | Setup-secret-protected endpoints return `410 Gone` after completion, with no side effects. Public status/sanitized provider reads remain explicitly safe read-only endpoints. | Final status-code decision is `410 Gone` for setup-protected endpoints after completion. |
| Auditability | Required event names are `bootstrap.completion.started`, `bootstrap.completion.succeeded`, `bootstrap.completion.failed`, `bootstrap.first_platform_admin_granted`, `bootstrap.first_tenant_admin_granted`, `bootstrap.setup_secret.disabled`, `bootstrap.auth_provider_configured`, and `bootstrap.auth_provider_configuration_failed`. Each event records actor/setup principal, tenant/host scope, operation, outcome, and correlation id. | Structured bootstrap audit emission now exists for setup-secret accepted/rejected/inactive checks, Keycloak bootstrap validation/start/failure/success, and setup-mode disablement. If exact dotted event-name taxonomy becomes a public/operator contract, reconcile the implemented event enum names in a dedicated follow-up. |
| Missing auth provider / provider failure | Read-only onboarding/status endpoints may expose safe setup state. Privileged writes require setup-secret authority plus authenticated identity where the endpoint mutates instance state; after provider activation, provider-backed admin/resource authorization is authoritative. | Keycloak bootstrap provider timeout/unreachable/invalid/upstream failures now return typed `502`/`503` ProblemDetails and stay redacted. Any separate post-activation missing-auth-provider scenario must be verified from source before becoming a task. |

Open follow-ups: reconcile exact audit event-name taxonomy if it becomes a stable operator contract, and define any local-only emergency recovery identity separately from setup-secret authority.

## Required System Scope API Shape

Tenant-bypass APIs should require a reason and operation name:

```csharp
BeginSystemTenantScope(SystemTenantScopeReason reason, string operationName, Guid? actorUserId = null)
```

Use-case-shaped APIs are preferred over LINQ helpers:

```csharp
await crossTenantEventReadStore.GetTenantSummariesAsync(reason, cancellationToken);
await tenantScope.RunAsHostAdministratorAsync(reason, operationName, action, cancellationToken);
```

## Forbidden Runtime Patterns

- Controller actions directly calling `IgnoreTenantFilter()` or `IgnoreAllFilters()`.
- Runtime tenant filters shaped as `TenantContext == null || row.TenantId == ...`.
- “System context” without reason enum, operation name, structured logging, and tests.
- Cross-tenant reads that return tenant-scoped rows without authorization and audit logging.
- Treating `RuntimeTenantOptionalPublicRead` as permission to expose user identity, registration, organization membership, tenant role grant, invitation, or revocation metadata anonymously.
- Navigation-dependent tenant isolation for child entities without tests proving parent-scoped filtering remains tenant-safe.

## Structured Logging Fields

- `tenant_execution_mode`
- `tenant_id`
- `actor_user_id`
- `operation_name`
- `system_scope_reason`
- `resource_type`
- `resource_id`
- `correlation_id`
- `outcome`

## Phase 1 Test Requirements

- Runtime tenant present returns only matching tenant rows.
- Runtime tenant absent fails closed or returns no tenant-scoped rows.
- Public reads do not return all tenant-scoped rows when tenant is absent.
- Host admin cross-tenant read requires host-admin policy and reason.
- Background system cross-tenant work requires operation name and reason.
- Migration/seeding/design-time modes do not leak into runtime request handling.
