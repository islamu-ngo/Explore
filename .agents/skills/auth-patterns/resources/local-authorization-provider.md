<!-- ABOUTME: Deep-dive reference guide for local authorization provider mechanics, batch evaluation, and machine caller scoping. -->
<!-- ABOUTME: Covers FallbackAuthorizationService partial class structure, AuthorityProfile pre-resolution, and SafeMode latch rules. -->

# Local Authorization Provider (`FallbackAuthorizationService`) Reference

This resource documents the internal mechanics of the database-driven local authorization provider used when running without Cerbos or in local/BYOC/ATProto deployment modes.

---

## 1. Class Architecture

`FallbackAuthorizationService` is implemented as four partial classes in `src/Explore.Infrastructure/Services/`:

```
Explore.Infrastructure/Services/
├── FallbackAuthorizationService.cs          # Main entry point, switch dispatching, safe-mode latch
├── FallbackAuthorizationService.Evaluators.cs      # Resource-specific evaluator logic (40 kinds)
├── FallbackAuthorizationService.Batch.cs          # O(1) single-pass AuthorityProfile batch engine
└── FallbackAuthorizationService.MachineCaller.cs   # API key scope ceiling & owner-type scoping
```

---

## 2. Dispatch Resolution Order

Every resource authorization check in `FallbackAuthorizationService` follows this evaluation order:

1. **Event Action Validation**: Screen out unsupported action strings (`IsSupportedEventResourceAction`).
2. **Instance Admin Direct Event Gate**: Instance Admins bypass normal checks EXCEPT for direct event authority actions (e.g. `event:manage-tickets` requires explicit event authority).
3. **Safe-Mode Latch Gate**: If `SafeMode == true` (activated when BYO Cerbos with `failure_mode=closed` fails), deny all non-Instance Admin traffic.
4. **Machine Principal Gate**: If caller is an API key (`_machinePrincipalAccessor.IsMachineCaller`), execute `EvaluateMachineCallerAccessAsync`.
5. **Resource Kind Switch**: Route to domain evaluators (`EvaluateTenantSettingAccessAsync`, `EvaluateOrganizationAccessAsync`, `EvaluateEventScopedAccessAsync`, `EvaluateStorageObjectAccessAsync`, etc.).

---

## 3. Batch Evaluation Optimization Engine

When evaluating HATEOAS link visibility or multi-resource checks (`IsAllowedBatchAsync`), `FallbackAuthorizationService.Batch.cs` avoids $N+1$ database queries:

```
                          IsAllowedBatchAsync(checks)
                                     |
                                     v
                       Is checks.Count <= 2 or Machine?
                                    / \
                              Yes  /   \ No
                                  v     v
              Sequential IsAllowedAsync  Pre-Resolve AuthorityProfile (1 DB Pass)
                                         Pre-Fetch Event Snapshots (1 SQL Query)
                                         Synchronous In-Memory Loop (EvaluateWithProfile)
```

### Authority Profile Pre-Resolution

```csharp
private sealed record AuthorityProfile(
    bool IsInstanceAdmin,
    bool IsTenantAdmin,
    Guid TenantId,
    IReadOnlySet<Guid> AdminOrgIds,
    IReadOnlySet<Guid> AdminGroupIds,
    IReadOnlySet<Guid> EventCreateOrgIds,
    IReadOnlySet<Guid> EventCreateGroupIds,
    Guid? UserId);
```

Resolves all caller administrative memberships once per batch. Event role assignments across all batch event IDs are fetched in **one query** via `IEventAuthoritySnapshotService.GetForUserAndEventsAsync()`.

---

## 4. Machine Principal (API Key) Security Rules

Machine callers evaluate access in `FallbackAuthorizationService.MachineCaller.cs`:

1. **Registration Form Bar**: Machine callers are strictly prohibited from modifying registration forms or managing registration channels/tickets.
2. **Scope Ceiling (`MachineScopeMapping`)**: External API key scopes (`events:write`, `organizations:read`, `admin:tenant`, `mcp:propose`) establish an absolute capability ceiling.
3. **Owner Scoping (`ExternalApiKeyOwnerType`)**:
   - `InstanceAdmin`: Unrestricted access.
   - `Tenant`: Restricted to resource `tenantId == context.TenantId`. Cannot access instance settings or platform namespaces.
   - `Organization` / `Group`: Restricted to resources owned by `context.OwnerId`.
   - `User`: Restricted to user-owned resources or tenant resources where user is Tenant/Org Admin.

---

## 5. Failure Modes & One-Way Safe-Mode Latch

- **BYO Cerbos Failure (`failure_mode=closed`)**: Triggers `ActivateSafeMode()`.
- **Behavior**: Sets `SafeMode = true`. Logs a critical alert. Denies all non-Instance Admin traffic.
- **Latch Property**: Safe mode is a **one-way latch** for the lifetime of that provider instance; it cannot be programmatically deactivated without recreating the provider instance.

---

## 6. Key Resource Evaluator Facts

- **Tenant Settings**: Updates check `isLockedByInstance == true`. Locked settings deny non-instance admins unless the document key is `tenant.branding`.
- **User Profiles**: Self-service `view`/`update` requires `targetUserId == currentUserId`. Other targets require Tenant Admin or Instance Admin.
- **Storage Objects**: Downloads are allowed if active AND (`PublicImage` | `AuthenticatedTenant` | `createdBy == currentUserId`).
- **Support Access Sessions**: Active support sessions tag OpenTelemetry activities, enforce tenant matching (`support_access_target_tenant_mismatch`), and deny mutations when in read-only mode (`support_access_read_only`).
