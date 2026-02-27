ABOUTME: Defines the three authorization request patterns used by the MediatR pipeline.
ABOUTME: Documents provider selection and failure behavior (Cerbos, BYO Cerbos, fallback RBAC).

# Authorization Patterns

This project enforces fine-grained authorization in `Explore.Application.Behaviors.AuthorizationBehavior<TRequest,TResponse>`.

## Enforcement Point

- Requests are checked before handlers execute.
- Denials throw `AuthorizationException`.
- `Explore.API.ExceptionHandling.GlobalExceptionHandler` maps `AuthorizationException` to HTTP `403 Forbidden`.
- Registered pipeline order is:
  - `PerformanceBehavior`
  - `AuthorizationBehavior`
- There is no global validation pipeline behavior in current registration; validators are used from handlers/services.

## Request Patterns

1. `IAuthorizedRequest`
   - use when resource kind/action/id are request-dependent.
   - required fields: `ResourceKind`, `ResourceId`, `Action` (optional attributes).
   - behavior reads all values directly from request instance.
2. `[AuthorizeResource]`
   - use when resource kind/action are static.
   - required data: attribute values only.
   - behavior defaults resource ID to request type name.
3. `[AuthorizeResource]` + `ISecureRequest`
   - use when kind/action are static but ID or attributes are runtime values.
   - required data: attribute + optional `ResourceId`/`ResourceAttributes`.
   - behavior prefers dynamic values from `ISecureRequest`; falls back when missing.

## How To Choose

1. Fixed resource kind and no instance-specific context: `[AuthorizeResource]`
2. Fixed kind but policy depends on entity ID/attributes: `[AuthorizeResource]` + `ISecureRequest`
3. Fully dynamic kind/action/id from request state: `IAuthorizedRequest`

## Provider Resolution (Runtime)

`RuntimeAuthorizationProvider` routes checks in this order:

1. Tenant BYO Cerbos config (if configured through `ICerbosConfigResolver`).
2. Otherwise, instance-level mode from `SystemSetting` key `AuthorizationProvider` (cached for 1 minute):
   - `"cerbos"` -> `CerbosAuthorizationService`
   - any other value -> `FallbackAuthorizationService`
3. If instance Cerbos fails (network/timeout), it falls back to `FallbackAuthorizationService`.

BYO Cerbos failure handling:

- `failure_mode=closed`: fallback runs in `SafeMode` (non-instance-admin traffic denied).
- `failure_mode=open`: fallback runs in standard RBAC mode.

## Fallback RBAC Facts

`FallbackAuthorizationService` is deny-by-default for unknown resource kinds and includes explicit rules for known kinds (`tenant_setting`, `organization`, `event`, `event_registration`, `storage_object`, `user`, etc.).

Notable behavior:

- Instance admins bypass normal checks.
- Tenant-setting updates can be denied when `isLockedByInstance=true`.
- `user` resource supports self-service `view`/`update` when `resourceId == current user`.

## Related

- [SECURITY.md](SECURITY.md)
- [adr/ADR-001-authorization-provider-architecture.md](adr/ADR-001-authorization-provider-architecture.md)
