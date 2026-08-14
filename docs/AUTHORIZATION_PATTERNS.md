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
2. Handler-owned local check bypasses (`GetHandlerOwnedLocalCheckIndexes`): self-service `user:update`, pre-create `event:create`, `organization:create`, `event_session:create`, `ai_conversation` route directly to `FallbackAuthorizationService` to ensure stale PDP policy packages cannot block self-service or pre-create handlers.
3. Instance-level mode from `SystemSetting` key `AuthorizationProvider` (cached for 1 minute):
   - `"cerbos"` -> `CerbosAuthorizationService`
   - any other value / `"local"` -> `FallbackAuthorizationService`
4. If the instance provider setting cannot be read, the runtime uses the Cerbos fail-closed path and logs safe `FailureType` metadata only.

Instance Cerbos failures are fail-closed. Network, timeout, or PDP-unavailable failures deny rather than falling back to `FallbackAuthorizationService`; switching back to local RBAC requires an explicit provider configuration change.

BYO Cerbos failure handling:

- Any BYO PDP failure activates fallback provider `SafeMode` (one-way latch: non-instance-admin traffic denied).
- `failure_mode=open` is still parsed as a deprecated configuration value, but runtime authorization treats it as fail-closed and does not run standard local RBAC.

BYO config resolver failures activate provider-instance `SafeMode` instead of silently using local RBAC. A tenant configured with `cerbos.mode=custom_endpoint` but no custom PDP endpoint remains in BYO mode: runtime authorization activates safe mode, while explicit BYO Admin API configuration remains available for package sync/status operations.

## Fallback RBAC Facts

`FallbackAuthorizationService` is a 4-part partial class (`.cs`, `.Evaluators.cs`, `.Batch.cs`, `.MachineCaller.cs`) that is deny-by-default for unknown resource kinds and includes explicit rules across all 40 domain resource kinds (`tenant_setting`, `organization`, `event`, `event_registration`, `storage_object`, `user`, `webhook`, `support_access_session`, etc.).

Notable behavior:

- Instance admins bypass normal checks except for direct event authority rules (`event:manage-tickets` requires explicit event authority).
- Tenant-setting updates are denied when `isLockedByInstance=true` (unless document is `tenant.branding`).
- `user` resource supports self-service `view`/`update` when `targetUserId == current user`.
- Machine callers (API keys) evaluate via `EvaluateMachineCallerAccessAsync`: barred from registration workflows, gated by `MachineScopeMapping` scope ceilings, and scoped by `ExternalApiKeyOwnerType`.
- Batch evaluation (`IsAllowedBatchAsync`) pre-resolves an `AuthorityProfile` and fetches active event role snapshots via `IEventAuthoritySnapshotService` in **a single pass**, executing batch link checks in $O(1)$ database queries.

## Related

- [SECURITY.md](SECURITY.md)
- [AUTHORIZATION.md](AUTHORIZATION.md)
- [adr/ADR-001-authorization-provider-architecture.md](adr/ADR-001-authorization-provider-architecture.md)
