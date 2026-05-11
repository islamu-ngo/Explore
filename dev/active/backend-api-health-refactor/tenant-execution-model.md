<!-- ABOUTME: Tenant execution model artifact for backend/API health refactor Phase 0. -->
<!-- ABOUTME: Defines runtime, host-admin, background, migration, and design-time tenant behavior. -->

# Tenant Execution Model

Last Updated: 2026-05-07 Europe/Brussels

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
| `RuntimeTenantOptionalPublicRead` | Public reads where tenant is resolved by host/domain/path, global platform-level public data, or explicit public read model. | Never query all tenant rows silently. Return no rows or public platform data only. | Only through explicit public/cross-tenant read model. | Yes for explicit cross-tenant read model. |
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
| First platform administrator | Created through the setup/bootstrap flow only while instance onboarding is incomplete. Completion requires setup-secret validation plus authenticated identity when an auth provider is configured. | Verify whether setup supports a local/self-host fallback identity when Keycloak is not configured. |
| First tenant administrator | In `SingleTenant` mode, the first platform administrator may also seed/claim the default tenant administrator role during onboarding. In `MultiTenant` mode, tenant administrator creation must be explicit per tenant and auditable. | Define exact command/API for default tenant claim before Phase 1E code changes. |
| Setup secret authority | `X-Setup-Secret` is a bootstrap credential, not an ongoing admin credential. It must be disabled or return `410 Gone` after onboarding completion except explicitly documented preflight/status endpoints. | Confirm all setup-secret endpoints are inventoried in `endpoint-inventory.md`. |
| Keycloak/group authority | After auth provider configuration is active, admin capability comes from configured identity/provider claims plus resource authorization, not from setup secret. | Verify missing/misconfigured provider behavior returns typed ProblemDetails rather than broad fallback access. |
| Bootstrap disablement | Operators must be able to disable bootstrap/setup endpoints after completion. Disabled endpoints return `410 Gone` or `404 Not Found` per endpoint classification, with no side effects. | Decide final status code in Phase 1E tests and document it in `api-error-catalog.md` if new code is needed. |
| Auditability | Every bootstrap completion, first-admin creation, provider configuration write/test, and bootstrap disablement emits an audit event with actor/setup principal, tenant/host scope, operation, outcome, and correlation id. | Add audit taxonomy rows before implementing setup changes. |
| Missing auth provider | Read-only onboarding/status endpoints may expose safe setup state; privileged writes require setup-secret authority until provider is configured, then provider-backed admin authority. | Verify SingleTenant/MultiTenant differences in API integration tests. |

Open blockers: exact local fallback identity behavior, exact default-tenant admin claim command, and final disabled-endpoint status code. These are Phase 1E blockers and must not be deferred into controller decomposition.

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
