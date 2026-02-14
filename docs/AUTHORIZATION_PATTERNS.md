# Authorization Patterns

> Guide for choosing the correct authorization pattern for MediatR commands and queries.

---

## Overview

The platform uses three authorization patterns, all enforced server-side through the MediatR pipeline. Client-side auth checks (Blazor) are UX-only — see [SECURITY.md](SECURITY.md#client-side-authorization-ux-only).

| Pattern | Mechanism | Use Case |
|---------|-----------|----------|
| `[AuthorizeResource]` | Attribute on MediatR request class | Static resource/action authorization |
| `IAuthorizedRequest` | Interface on MediatR request class | Dynamic resource context from request properties |
| `ISecureRequest` | Companion to `[AuthorizeResource]` | Static resource kind + dynamic resource ID/attributes |

All three are evaluated by `AuthorizationBehavior<TRequest, TResponse>` in the MediatR pipeline, before the handler executes.

---

## Pattern 1: `[AuthorizeResource]` Attribute

**When to use**: The resource kind and action are known at compile time and don't depend on request data.

```csharp
[AuthorizeResource("instance_setting", PermissionAction.Update)]
public class UpdateInstanceSettingCommand : IRequest<BaseCommandResponse>
{
    public Guid Id { get; set; }
    public string Value { get; set; }
}
```

**How it works**:
1. `AuthorizationBehavior` detects the `[AuthorizeResource]` attribute via reflection
2. Extracts `Resource = "instance_setting"`, `Action = "update"`
3. Uses `typeof(TRequest).Name` as ResourceId (static fallback)
4. Calls `IAuthorizationProvider.IsAuthorizedAsync(principal, resource, action)`
5. Throws `ForbiddenException` if denied

**Best for**: Simple commands where the resource type is always the same (settings, lookup tables).

---

## Pattern 2: `IAuthorizedRequest` Interface

**When to use**: The request itself carries the full authorization context — resource kind, resource ID, action, and optional attributes are all dynamic.

```csharp
public class UpdateOrganizationCommand : IRequest<BaseCommandResponse>, IAuthorizedRequest
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; }

    // IAuthorizedRequest implementation
    public string ResourceKind => "organization";
    public string ResourceId => OrganizationId.ToString();
    public string Action => "update";
    public IDictionary<string, object>? ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString(),
        ["organizationId"] = OrganizationId.ToString()
    };
}
```

**How it works**:
1. `AuthorizationBehavior` checks if request implements `IAuthorizedRequest`
2. Reads `ResourceKind`, `ResourceId`, `Action`, and `ResourceAttributes` from the request instance
3. Passes full context to `IAuthorizationProvider` — enables Cerbos conditions like "is user admin of THIS organization?"
4. Throws `ForbiddenException` if denied

**Best for**: Commands where the resource ID matters for authorization (organization-specific, tenant-specific operations).

---

## Pattern 3: `ISecureRequest` (Companion to `[AuthorizeResource]`)

**When to use**: The resource kind and action are static (use `[AuthorizeResource]`), but the resource ID or attributes come from the request instance.

```csharp
[AuthorizeResource("event", PermissionAction.Delete)]
public class DeleteEventCommand : IRequest<BaseCommandResponse>, ISecureRequest
{
    public Guid EventId { get; set; }
    public Guid OrganizationId { get; set; }

    // ISecureRequest implementation — enhances the attribute with dynamic context
    public string? ResourceId => EventId.ToString();
    public IDictionary<string, object>? ResourceAttributes => new Dictionary<string, object>
    {
        ["organizationId"] = OrganizationId.ToString()
    };
}
```

**How it works**:
1. `AuthorizationBehavior` detects `[AuthorizeResource]` for resource kind + action
2. Also checks if request implements `ISecureRequest`
3. If yes: uses `ISecureRequest.ResourceId` and `ResourceAttributes` instead of static defaults
4. Combines static metadata (attribute) with dynamic context (interface)

**Best for**: Commands that always operate on the same resource type but need instance-specific IDs for policy evaluation.

---

## Decision Tree

```
Does the command need authorization?
│
├── NO → Don't add any authorization pattern
│        (public reads, system commands)
│
└── YES → Is the resource kind always the same?
          │
          ├── YES → Does the authorization check need the specific resource ID?
          │         │
          │         ├── NO → Use [AuthorizeResource] alone
          │         │        (simplest: attribute-only)
          │         │
          │         └── YES → Use [AuthorizeResource] + ISecureRequest
          │                   (attribute for kind/action, interface for ID/attrs)
          │
          └── NO → Use IAuthorizedRequest
                   (full dynamic control over resource kind, ID, action, attrs)
```

---

## Quick Reference

| Scenario | Pattern | Example |
|----------|---------|---------|
| Update instance settings | `[AuthorizeResource]` | Static resource, no ID needed |
| Delete a specific event | `[AuthorizeResource]` + `ISecureRequest` | Static kind, dynamic event ID |
| Update organization details | `IAuthorizedRequest` | Dynamic org ID + tenant context |
| Create event for org | `IAuthorizedRequest` | Org context determines permission |
| Public query (GET) | None | `[AllowAnonymous]` at controller level |

---

## Pipeline Order

```
MediatR Pipeline:
  1. ValidationBehavior     ← FluentValidation (input validation)
  2. AuthorizationBehavior  ← Checks [AuthorizeResource] / IAuthorizedRequest
  3. Handler                ← Business logic executes only if authorized
```

Authorization runs AFTER validation (no point checking auth for invalid requests) and BEFORE the handler (fail-fast on unauthorized access).

---

## Related

- [SECURITY.md](SECURITY.md) — Security architecture overview
- [ADR-001](adr/ADR-001-authorization-provider-architecture.md) — Why HTTP + dual-provider architecture
- [DEPLOYMENT_TIERS.md](DEPLOYMENT_TIERS.md) — How authorization scales across tiers
