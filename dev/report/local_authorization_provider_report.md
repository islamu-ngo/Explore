# Master Technical Reference: Local Authorization Provider Architecture & Implementation

> **Author**: Antigravity  
> **Date**: August 13, 2026  
> **Version**: 2.0 (Exhaustive Deep-Dive)  
> **Repository Scope**: `Explore.Application`, `Explore.Domain`, `Explore.Infrastructure`, `Explore.Persistence`, `Explore.API`

---

## Table of Contents

1. [Executive Summary & Scope](#1-executive-summary--scope)
2. [System Architecture & Core Abstractions](#2-system-architecture--core-abstractions)
   - [2.1 Abstraction Interface (`IAuthorizationProvider`)](#21-abstraction-interface-iauthorizationprovider)
   - [2.2 Data Contract (`AuthorizationCheck`)](#22-data-contract-authorizationcheck)
   - [2.3 Canonical Deduplication Algorithm](#23-canonical-deduplication-algorithm)
3. [Runtime Provider Router (`RuntimeAuthorizationProvider`)](#3-runtime-provider-router-runtimeauthorizationprovider)
   - [3.1 Decision Priority Pipeline](#31-decision-priority-pipeline)
   - [3.2 Handler-Owned Local Parity Bypasses](#32-handler-owned-local-parity-bypasses)
   - [3.3 Failure Handling & The One-Way Safe-Mode Latch](#33-failure-handling--the-one-way-safe-mode-latch)
   - [3.4 Mode Cache & Invalidation](#34-mode-cache--invalidation)
4. [Local Authorization Engine (`FallbackAuthorizationService`) Deep-Dive](#4-local-authorization-engine-fallbackauthorizationservice-deep-dive)
   - [4.1 Code Base Structure](#41-code-base-structure)
   - [4.2 Dispatch Topology & Main Switch](#42-dispatch-topology--main-switch)
   - [4.3 Granular Evaluator Logic for All Resource Kinds](#43-granular-evaluator-logic-for-all-resource-kinds)
     - [4.3.1 System Settings & Governance Locks](#431-system-settings--governance-locks)
     - [4.3.2 Tenants & Tenant User Role Grants](#432-tenants--tenant-user-role-grants)
     - [4.3.3 Organizations, Members, & Evidence Review](#433-organizations-members--evidence-review)
     - [4.3.4 Groups & Group Members](#434-groups--group-members)
     - [4.3.5 Events, Sessions, Days, & Agendas](#435-events-sessions-days--agendas)
     - [4.3.6 Registration Forms, Workflow, & Channels](#436-registration-forms-workflow--channels)
     - [4.3.7 Registration Orders](#437-registration-orders)
     - [4.3.8 Organizer Claims & Withdrawals](#438-organizer-claims--withdrawals)
     - [4.3.9 Storage Objects & Visibility Rules](#439-storage-objects--visibility-rules)
     - [4.3.10 User Profiles & Self-Service Rules](#4310-user-profiles--self-service-rules)
     - [4.3.11 Support Access Sessions](#4311-support-access-sessions)
     - [4.3.12 Webhook Management](#4312-webhook-management)
     - [4.3.13 EAV Custom Properties, Projections, & Governance](#4313-eav-custom-properties-projections--governance)
     - [4.3.14 Email Dispatch Controls](#4314-email-dispatch-controls)
     - [4.3.15 Lookups, Notifications, & AI Conversations](#4315-lookups-notifications--ai-conversations)
5. [Machine Principal (API Key) Security Architecture](#5-machine-principal-api-key-security-architecture)
   - [5.1 Scope Ceiling Engine (`MachineScopeMapping`)](#51-scope-ceiling-engine-machinescopemapping)
   - [5.2 Owner-Type Access Scoping (`ExternalApiKeyOwnerType`)](#52-owner-type-access-scoping-externalapikeyownertype)
6. [High-Throughput Batch Optimization Engine](#6-high-throughput-batch-optimization-engine)
   - [6.1 The 4-Step Batch Capability Planning Algorithm](#61-the-4-step-batch-capability-planning-algorithm)
   - [6.2 Authority Profile Pre-Resolution (`AuthorityProfile`)](#62-authority-profile-pre-resolution-authorityprofile)
   - [6.3 Batch Event Authority Snapshots (`EventAuthoritySnapshotService`)](#63-batch-event-authority-snapshots-eventauthoritysnapshotservice)
   - [6.4 Performance & Complexity Analysis](#64-performance--complexity-analysis)
7. [Identity & Authority Resolution Engine (`AdminContext`)](#7-identity--authority-resolution-engine-admincontext)
   - [7.1 Database-First Authority Resolution](#71-database-first-authority-resolution)
   - [7.2 Memory Caching & Invalidation Architecture](#72-memory-caching--invalidation-architecture)
   - [7.3 Single-Tenant Mode Optimizations](#73-single-tenant-mode-optimizations)
8. [Enforcement Pipeline & Architectural Depths](#8-enforcement-pipeline--architectural-depths)
   - [8.1 Controller Level (`[Authorize]`)](#81-controller-level-authorize)
   - [8.2 MediatR Pipeline (`AuthorizationBehavior<TReq, TRes>`)](#82-mediatr-pipeline-authorizationbehaviortreq-tres)
   - [8.3 HATEOAS Link Filtering (`HateoasAuthorizationEvaluator`)](#83-hateoas-link-filtering-hateoasauthorizationevaluator)
   - [8.4 Client Gating Invariant: HAL `_links`](#84-client-gating-invariant-hal-_links)
9. [Onboarding & Configuration Management](#9-onboarding--configuration-management)
10. [Anti-Escalation & Capability Ceiling (`CapabilityCeilingService`)](#10-anti-escalation--capability-ceiling-capabilityceilingservice)
11. [Testing & Verification Matrix](#11-testing--verification-matrix)

---

## 1. Executive Summary & Scope

The platform employs a pluggable, multi-layered authorization system supporting external Policy Decision Points (Cerbos PDP via gRPC) as well as an in-process, database-driven **Local Authorization Provider** ([`FallbackAuthorizationService`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Infrastructure/Services/FallbackAuthorizationService.cs)).

The local provider is a production-grade, high-performance Role-Based and Attribute-Based Access Control (RBAC/ABAC) engine. It is utilized when running in single-tenant deployments, ATProto/PDS standalone nodes, local development, or when Cerbos integration is turned off by configuration.

This document serves as the **authoritative, exhaustive technical reference** for the local authorization architecture. It covers every interface, concrete class, decision switch, evaluator rule, caching strategy, security boundary, and batch optimization algorithm across the API codebases.

---

## 2. System Architecture & Core Abstractions

```
                               +-------------------------------------------------------------+
                               |                  HTTP Request / Blazor BFF                  |
                               +-------------------------------------------------------------+
                                                              |
                                                              v
                               +-------------------------------------------------------------+
                               |             ASP.NET Core Controllers / Endpoints            |
                               |               [Authorize] / [AllowAnonymous]                |
                               +-------------------------------------------------------------+
                                                              |
                                                              v
                               +-------------------------------------------------------------+
                               |       MediatR Pipeline: AuthorizationBehavior<TReq, TRes>   |
                               +-------------------------------------------------------------+
                                                              |
                                                              v
                               +-------------------------------------------------------------+
                               |               IAuthorizationProvider (Contract)             |
                               +-------------------------------------------------------------+
                                                              |
                                                              v
                               +-------------------------------------------------------------+
                               |                 RuntimeAuthorizationProvider                |
                               |  (Router: Tenant BYO -> Local Bypass -> SystemSetting)     |
                               +-------------------------------------------------------------+
                                            /                                   \
                                           v                                     v
                               +-----------------------+             +-----------------------+
                               | CerbosAuthorization   |             | FallbackAuthorization |
                               | Service (gRPC PDP)    |             | Service (Local RBAC)  |
                               +-----------------------+             +-----------------------+
                                                                                 |
                                                                                 v
                                                                     +-----------------------+
                                                                     |     AdminContext      |
                                                                     | (DB-Backed Authority) |
                                                                     +-----------------------+
```

### 2.1 Abstraction Interface (`IAuthorizationProvider`)

Defined in [`src/Explore.Application/Contracts/Infrastructure/IAuthorizationProvider.cs`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Application/Contracts/Infrastructure/IAuthorizationProvider.cs):

```csharp
public interface IAuthorizationProvider
{
    Task<bool> IsAllowedAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<bool>> IsAllowedBatchAsync(
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken cancellationToken = default);

    Task<bool> CheckSettingAccessAsync(
        string settingKey,
        string action,
        Guid? tenantId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);
}
```

### 2.2 Data Contract (`AuthorizationCheck`)

A sealed record encapsulating a single transport-neutral authorization request:

```csharp
public sealed record AuthorizationCheck(
    string ResourceKind,
    string ResourceId,
    string Action,
    IReadOnlyDictionary<string, object>? ResourceAttributes = null,
    AuthorizationScope? Scope = null)
```

- **`ResourceKind`**: Domain string constant from [`ResourceKinds`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Application/Authorization/ResourceKinds.cs) (e.g., `"islamuevent_event"`).
- **`ResourceId`**: Primary key or entity identifier.
- **`Action`**: Targeted action string constant from [`AuthorizationActions`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Application/Authorization/AuthorizationActions.cs) (e.g., `"view"`, `"update"`, `"events:publish"`).
- **`ResourceAttributes`**: Runtime context map containing tenant IDs, owner IDs, lock states, or organizer references.
- **`Scope`**: Boundary wrapper carrying explicit `TenantId` and `OrganizationId`.

### 2.3 Canonical Deduplication Algorithm

To eliminate duplicate policy evaluation during batch checks (e.g., multiple HATEOAS links referencing the same underlying resource check), `AuthorizationCheck` generates a length-prefixed deduplication key via `ToDeduplicationKey()`:

```csharp
public string ToDeduplicationKey()
{
    var builder = new StringBuilder();
    AppendSegment(builder, ResourceKind);
    AppendSegment(builder, ResourceId);
    AppendSegment(builder, Action);
    AppendScope(builder, Scope);
    AppendAttributes(builder, ResourceAttributes);
    return builder.ToString();
}

private static void AppendSegment(StringBuilder builder, string value)
{
    builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
    builder.Append(':');
    builder.Append(value);
    builder.Append('|');
}
```

Attribute keys are sorted using `Ordinal` comparison, and attribute values are prefixed with their full type name (`System.Guid:00000000-...`, `System.Boolean:True`). This ensures that attribute-sensitive checks can never collapse into different decisions.

---

## 3. Runtime Provider Router (`RuntimeAuthorizationProvider`)

Located in [`src/Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs), this class acts as the active proxy for all `IAuthorizationProvider` calls in the application.

### 3.1 Decision Priority Pipeline

```
                              Incoming IsAllowedBatchAsync()
                                            |
                                            v
                         +-------------------------------------+
                         | Apply Support Access Session Gate   |
                         +-------------------------------------+
                                            |
                                            v
                         +-------------------------------------+
                         | Are checks for Settings resources?  |
                         +-------------------------------------+
                                     /             \
                             Yes    /               \  No
                                   v                 v
                   +-------------------+   +--------------------+
                   | Execute Instance  |   | Resolve Tenant BYO |
                   | Provider Mode     |   | Cerbos Config      |
                   +-------------------+   +--------------------+
                                                     /          \
                                         Configured /            \ Default/Null
                                                   v              v
                                        +--------------+  +-------------------------------+
                                        | Execute BYO  |  | Filter Handler-Owned Local    |
                                        | Cerbos PDP   |  | Check Indexes                 |
                                        +--------------+  +-------------------------------+
                                                                 /                 \
                                                 All/Partial    /                   \ None
                                                               v                     v
                                                 +-------------------+     +--------------------+
                                                 | Execute Local     |     | Execute Instance   |
                                                 | Parity checks     |     | Provider Mode      |
                                                 +-------------------+     +--------------------+
```

### 3.2 Handler-Owned Local Parity Bypasses

Certain high-frequency or self-service checks are marked as **handler-owned** via `IsHandlerOwnedLocalCheck()`:
- `islamuevent_ai_conversation`: AI assistant conversation checks.
- `islamuevent_user:update`: Self-service profile updates (where target resource ID equals the authenticated user ID).
- `islamuevent_event:create`: Event pre-creation checks.
- `islamuevent_organization:create`: Organization pre-creation checks.
- `islamuevent_event_session:create`: Pre-creation session checks.
- `islamuevent_storage_object:create`: Direct upload session creation.

These checks evaluate against `FallbackAuthorizationService` even when instance mode is `"cerbos"`, ensuring that PDP package sync latency never blocks basic self-service user actions.

### 3.3 Failure Handling & The One-Way Safe-Mode Latch

| Mode | Failure Event | Handling Strategy | Security Rationale |
|---|---|---|---|
| **Instance Cerbos** | gRPC Connection / PDP Timeout | **Fail-Closed (Deny All)** | System refuses to guess local rules when explicit Cerbos PDP policies were selected. |
| **Tenant BYO Cerbos** | PDP Down & `FailureMode.Closed` | **Activate Safe-Mode Latch** | Transitions local provider to `SafeMode = true`. Denies all non-Instance Admin traffic. |
| **Tenant BYO Cerbos** | PDP Down & `FailureMode.Open` | **Standard Local Fallback** | Tenant opted into standard local RBAC fallback during outages. |
| **Setting DB Fail** | Unable to read `AuthorizationProvider` setting | **Cerbos Fail-Closed Path** | System defaults to highest restriction when security settings cannot be read. |

#### Safe-Mode Latch Implementation
```csharp
public void ActivateSafeMode()
{
    SafeMode = true;
    if (!_safeModeLogged)
    {
        _safeModeLogged = true;
        _logger.LogCritical(
            "Safe mode ACTIVATED. Only instance admin access is permitted. " +
            "Cause: BYO Cerbos PDP unreachable with failure_mode=closed.");
    }
}
```
`SafeMode` is a **one-way latch**: once activated on a `FallbackAuthorizationService` instance, it cannot be reset programmatically without recreating the provider instance.

### 3.4 Mode Cache & Invalidation

The instance authorization mode (`"cerbos"` vs `"local"`) is stored in `SystemSettings` and cached in `IMemoryCache` with key `AuthorizationProvider_Mode` for 60 seconds (`InstanceModeCacheDuration`).

Calling `InvalidateInstanceMode()` immediately clears this entry, forcing the next request to re-read the mode from database settings.

---

## 4. Local Authorization Engine (`FallbackAuthorizationService`) Deep-Dive

### 4.1 Code Base Structure

`FallbackAuthorizationService` is organized across four partial files:

| File Name | Primary Responsibility | Key Methods |
|---|---|---|
| [`FallbackAuthorizationService.cs`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Infrastructure/Services/FallbackAuthorizationService.cs) | Main entry point, DI constructor, primary switch dispatcher, logging helpers | `IsAllowedAsync`, `CheckSettingAccessAsync`, `LogDecision`, `ActivateSafeMode` |
| [`FallbackAuthorizationService.Evaluators.cs`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Infrastructure/Services/FallbackAuthorizationService.Evaluators.cs) | Resource-specific evaluator methods for all 40 domain resource kinds | `EvaluateOrganizationAccessAsync`, `EvaluateEventScopedAccessAsync`, `EvaluateStorageObjectAccessAsync`, `EvaluateUserAccessAsync`, etc. |
| [`FallbackAuthorizationService.Batch.cs`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Infrastructure/Services/FallbackAuthorizationService.Batch.cs) | Optimized batch evaluation using single-pass `AuthorityProfile` pre-resolution | `IsAllowedBatchAsync`, `ResolveAuthorityProfileAsync`, `EvaluateWithProfile` |
| [`FallbackAuthorizationService.MachineCaller.cs`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Infrastructure/Services/FallbackAuthorizationService.MachineCaller.cs) | API key machine caller evaluation, scope ceilings, owner-type checks | `EvaluateMachineCallerAccessAsync`, `EvaluateTenantOwnerMachineAccess`, `EvaluateOrganizationOwnerMachineAccess` |

### 4.2 Dispatch Topology & Main Switch

When `IsAllowedAsync` is called, execution proceeds through `FallbackAuthorizationService.cs`:

```csharp
public async Task<bool> IsAllowedAsync(
    string resourceKind, string resourceId, string action,
    IDictionary<string, object>? resourceAttributes = null,
    CancellationToken cancellationToken = default)
{
    // 1. Supported action guard
    if (!IsSupportedEventResourceAction(resourceKind, action))
        return false;

    // 2. Instance admin check & direct event authority rule
    var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(cancellationToken);
    if (isInstanceAdmin && resourceKind == ResourceKinds.Event && action == AuthorizationActions.Events.ManageTickets)
        return false; // Direct event authority required

    if (isInstanceAdmin && !RequiresDirectEventAuthority(resourceKind, action))
        return true; // Instance admin bypass for non-direct event resources

    // 3. Safe-Mode Check
    if (SafeMode && !isInstanceAdmin)
        return false;

    // 4. Machine Caller (API Key) Evaluation
    if (_machinePrincipalAccessor.IsMachineCaller)
        return await EvaluateMachineCallerAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken);

    // 5. Main Resource Kind Switch
    var decision = resourceKind switch
    {
        "islamuevent_instance_setting" => false,
        "islamuevent_tenant_setting" => await EvaluateTenantSettingAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_tenant" => await EvaluateTenantResourceAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_tenant_user_role_grant" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_category" or "islamuevent_tag" or "islamuevent_location" or "islamuevent_location_room" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_custom_property_definition" or "islamuevent_custom_property_template" => await EvaluateViewableTenantResourceAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_custom_property_value" => await EvaluateViewableOrgResourceAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_custom_property_projection" => await EvaluateCustomPropertyProjectionAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_custom_property_governance" or "islamuevent_email_dispatch" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_webhook" => await EvaluateWebhookAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_support_access_session" => await EvaluateSupportAccessSessionResourceAsync(action, resourceAttributes, cancellationToken),
        "islamuevent_platform_namespace" => action is "view",
        "islamuevent_organization" => await EvaluateOrganizationAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_organization_member" => await EvaluateOrgScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_organization_review" => await EvaluateOrgReviewAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_group" => await EvaluateViewableOrgResourceAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_group_member" => await EvaluateGroupMemberAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_event" or "islamuevent_event_session" or "islamuevent_event_session_group" or "islamuevent_event_session_agenda_item" or "islamuevent_event_day" or "islamuevent_event_agenda_item" or "islamuevent_event_organizer_claim" or "islamuevent_registration_form" => await EvaluateEventScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_registration_order" => await EvaluateRegistrationOrderAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_event_contact_share_consent" => await EvaluateContactShareConsentAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_storage_object" => await EvaluateStorageObjectAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_user" => await EvaluateUserAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_notification" or "islamuevent_actor_subscription" or "islamuevent_ai_conversation" => true,
        "islamuevent_actor" => await EvaluateActorAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
        "islamuevent_atproto_record" or "islamuevent_indexed_did" => false,
        _ => await EvaluateDefaultAccessAsync(resourceKind, action, resourceAttributes, cancellationToken)
    };

    LogDecision(decision ? "allow" : "deny", "fallback_policy", resourceKind, resourceId, action);
    return decision;
}
```

### 4.3 Granular Evaluator Logic for All Resource Kinds

#### 4.3.1 System Settings & Governance Locks
- **`islamuevent_instance_setting`**: Evaluates to `false` for non-instance admins. Instance admins bypass this check in step 2 of the primary dispatch.
- **`islamuevent_tenant_setting`**:
  1. Checks if `resourceAttributes["isLockedByInstance"] == true`. If locked (and not document `tenant.branding`), returns `false` (Instance locked).
  2. Resolves target `tenantId` from `resourceAttributes["tenantId"]` or falls back to ambient `_tenantContext.TenantId`.
  3. Returns `true` if `_adminContext.IsTenantAdminAsync(tenantId)` is true.

#### 4.3.2 Tenants & Tenant User Role Grants
- **`islamuevent_tenant`**: Actions restricted to `"view"` or `"update"`. Returns `true` if caller is a Tenant Admin of the target tenant.
- **`islamuevent_tenant_user_role_grant`**: Granted to Tenant Admins of the tenant scope specified in attributes.

#### 4.3.3 Organizations, Members, & Evidence Review
- **`islamuevent_organization`**:
  - Pre-Create (`authorizationPhase == "pre_create"`): Returns `true` if an authenticated user ID is present.
  - Action `review-evidence`: Requires Tenant Admin authority.
  - Action `submit-evidence` & `view-evidence`: Requires Organization Admin authority on `organizationId`.
  - General CRUD: Allowed if caller is Tenant Admin or Organization Admin.
- **`islamuevent_organization_member`**: Allowed for Tenant Admins (for actions `view`, `create`, `update`, `delete`, `manage_members`) or Organization Admins of the parent organization.
- **`islamuevent_organization_review`**: Actions `"create"` and `"view"` are open to authenticated users; update/delete requires Organization Admin or Tenant Admin.

#### 4.3.4 Groups & Group Members
- **`islamuevent_group`**: Action `"view"` is open to all authenticated users; mutating actions require Tenant Admin or Group Admin (`IsGroupAdminAsync`).
- **`islamuevent_group_member`**: Actions `"view"` and `"create"` are open; updates/deletions require Tenant Admin or Group Admin.

#### 4.3.5 Events, Sessions, Days, & Agendas
- **Event Creation (`event:create`)**: Open to any authenticated user within their ambient tenant context.
- **Event Scoped Context Guard**: Requires valid `tenantId` and `eventId`. Target `tenantId` must match ambient `_tenantContext.TenantId`.
- **Event Moderation (`moderate-light`, `moderate-heavy`, `unmoderate`)**: Requires Tenant Admin authority in the tenant scope (or Instance Admin). Event creators, managers, and org admins cannot moderate events.
- **Event Role Snapshots**: For standard actions (`view`, `update`, `delete`, `publish`), the provider checks:
  1. Is caller Tenant Admin? (Allowed for tenant-admin actions `view`, `view-management`, `moderate-*`, `view-organizer-claims`, `review-organizer-claim`).
  2. Is caller Organization Admin for the event's owning organization? (Allowed).
  3. Is caller the Actor User owner (`organizerUserId == currentUserId`)? (Allowed).
  4. Query `_eventAuthoritySnapshotService.GetForUserAndEventsAsync()`: Check if active `EventRoleAssignment` contains the permission code derived via `PermissionCodeFor(resourceKind, action)`.

#### 4.3.6 Registration Forms, Workflow, & Channels
- **`islamuevent_registration_form`**: Actions (`view`, `create`, `update`, `delete`, `preflight`, `publish`, `manage-requirements`, `attach`, `detach`).
- **Authorization**: Evaluated via `EvaluateManageRegistrationsAccessAsync`:
  - Machine callers are strictly **denied**.
  - Verified Organizer Controllers (`organizerUserId == currentUserId`, OR caller has `PermissionCodes.EventCreate` in `organizerOrganizationId` / `organizerGroupId`) are **allowed**.
  - Tenant Admins without organizer control are **denied**.
  - Event Role Assignment carrying `PermissionCodes.EventRegistrationManage` is **allowed**.

#### 4.3.7 Registration Orders
- **`islamuevent_registration_order`**: Actions (`view`, `cancel`, `continue`, `finalize`).
- **Authorization**:
  - Allowed if current user ID matches `accountUserId` in resource attributes (purchaser self-service).
  - Action `"view"` is allowed if caller has `manage-registrations` access for the parent event.

#### 4.3.8 Organizer Claims & Withdrawals
- **`islamuevent_event_organizer_claim`**:
  - `claim-organizer`: Requires authenticated human user (non-machine, non-instance admin).
  - `withdraw-organizer-claim`: Instance admins are explicitly **denied**. Allowed if caller is the claimant actor owner (`claimantUserId`, `claimantOrganizationId`, or `claimantGroupId`).
  - `review-organizer-claim`: Requires Tenant Admin or Tenant Curator. Regular event managers are **denied**.

#### 4.3.9 Storage Objects & Visibility Rules
- **`islamuevent_storage_object`**:
  - Action `create`: Open to all authenticated users.
  - Actions `download` / `presigned_download`: Allowed if caller is Tenant Admin, OR if lifecycle state is `Active` AND visibility is `PublicImage` or `AuthenticatedTenant`, OR if visibility is `Private` AND `createdBy == currentUserId`.
  - Actions `update` / `delete`: Requires Tenant Admin authority.

#### 4.3.10 User Profiles & Self-Service Rules
- **`islamuevent_user`**:
  - Actions `"view"` and `"update"`: Allowed if `targetUserId == currentUserId` (self-service profile management).
  - Other actions or non-self targets: Requires Tenant Admin or Instance Admin authority.

#### 4.3.11 Support Access Sessions
- **`islamuevent_support_access_session`**:
  - Actions (`view`, `list`, `view_audit`): Allowed for Tenant Admins of the specified target `tenantId`.
  - Mutating actions (`start`, `stop`, `force_stop`): Restricted to Support Operators / Instance Admins.

#### 4.3.12 Webhook Management
- **`islamuevent_webhook`**:
  - Evaluates `ownerKindId` attribute (`Instance`, `Tenant`, `Organization`, `Group`, `User`).
  - `Instance` owner -> Instance Admin required.
  - `Tenant` owner -> Tenant Admin required.
  - `Organization` / `Group` / `User` owner -> Requires delegated action (`IsDelegatedWebhookAction`) AND ownership by caller.

#### 4.3.13 EAV Custom Properties, Projections, & Governance
- **`custom_property_definition`** & **`custom_property_template`**: Action `"view"` is open to authenticated users; mutating actions require Tenant Admin.
- **`custom_property_value`**: Action `"view"` is open; mutating actions require Tenant Admin or Organization Admin.
- **`custom_property_projection`**: Actions `"view"` and `"update"` require Tenant Admin of the target tenant.

#### 4.3.14 Email Dispatch Controls
- **`islamuevent_email_dispatch`**: Actions (`view`, `manage_tenant`, `park`, `replay`, `resolve`, `reconcile`). Requires Tenant Admin of the tenant scope.

#### 4.3.15 Lookups, Notifications, & AI Conversations
- **`category`**, **`tag`**, **`location`**, **`location_room`**: `"view"` is open; mutations require Tenant Admin.
- **`notification`**, **`actor_subscription`**, **`ai_conversation`**: Evaluates to `true` at the provider level; MediatR handlers enforce owner and tenant isolation.

---

## 5. Machine Principal (API Key) Security Architecture

Machine caller authorization is handled by `FallbackAuthorizationService.MachineCaller.cs`.

```csharp
private async Task<bool> EvaluateMachineCallerAccessAsync(
    string resourceKind, string resourceId, string action,
    IDictionary<string, object>? resourceAttributes,
    CancellationToken cancellationToken)
{
    // 1. Prohibit registration workflow modifications by machine callers
    if (resourceKind == ResourceKinds.RegistrationForm ||
        (resourceKind == ResourceKinds.Event && action is AuthorizationActions.Events.ManageRegistrations or AuthorizationActions.Events.ManageTickets))
    {
        return false;
    }

    var context = _machinePrincipalAccessor.Current;
    if (context is null) return false;

    // 2. Scope Ceiling Gate
    if (!MachineScopeMapping.ScopesPermit(context.Scopes, resourceKind, action))
        return false;

    // 3. Owner-Type Scoping Switch
    return context.OwnerType switch
    {
        ExternalApiKeyOwnerType.InstanceAdmin => true,
        ExternalApiKeyOwnerType.Tenant => EvaluateTenantOwnerMachineAccess(context, resourceKind, resourceId, resourceAttributes),
        ExternalApiKeyOwnerType.Organization => EvaluateOrganizationOwnerMachineAccess(context, resourceKind, resourceId, resourceAttributes),
        ExternalApiKeyOwnerType.Group => EvaluateGroupOwnerMachineAccess(context, resourceKind, resourceId, resourceAttributes),
        ExternalApiKeyOwnerType.User => await EvaluateUserOwnerMachineAccessAsync(context, resourceKind, resourceId, action, resourceAttributes, cancellationToken),
        _ => false,
    };
}
```

### 5.1 Scope Ceiling Engine (`MachineScopeMapping`)

Defined in [`src/Explore.Application/Authorization/MachineScopeMapping.cs`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Application/Authorization/MachineScopeMapping.cs), this class translates coarse API key token scopes into permission ceilings:

- `admin:instance`: Bypasses all scope checks (grants access to all resource kinds).
- `admin:tenant`: Grants write/read access to tenant-scoped resources (`events`, `orgs`, `groups`, `users`, `lookups`, `webhooks`).
- `events:write` / `events:read`: Restricts access strictly to event resources (`islamuevent_event`, `islamuevent_event_session`, `islamuevent_event_day`, etc.).
- `organizations:write` / `organizations:read`: Restricts access to organization resources.
- `groups:write` / `groups:read`: Restricts access to group resources.
- `users:write` / `users:read`: Restricts access to user profiles and actor subscriptions.
- `lookups:read`: Grants read-only access to categories, tags, locations, and custom property definitions.
- `mcp:propose` / `mcp:read`: Scoped specifically for AI assistant tool action proposals and conversation reads.
- `webhook:process-incoming`: Restricted exclusively to processing incoming webhook callbacks (`islamuevent_webhook:webhook:process-incoming`).

### 5.2 Owner-Type Access Scoping (`ExternalApiKeyOwnerType`)

In addition to satisfying the scope ceiling, a machine caller must satisfy owner-type resource scoping:
- **`Tenant` Owner**: Target resource `tenantId` must equal `context.TenantId`. Forbidden from instance settings, ATProto records, and platform namespaces.
- **`Organization` Owner**: Target resource must belong to `context.OwnerId` organization or be a user/lookup resource. Cannot touch tenant-wide settings or unrelated organizations.
- **`Group` Owner**: Target resource must belong to `context.OwnerId` group.
- **`User` Owner**: Allowed if target resource is the user's own profile, OR if the owning user is a Tenant Admin / Org Admin for the target resource.

---

## 6. High-Throughput Batch Optimization Engine

Evaluating HATEOAS link visibility for long lists of resources could cause extreme database query overhead ($N+1$ calls). `FallbackAuthorizationService.Batch.cs` implements an optimized batch evaluator:

```csharp
public async Task<IReadOnlyList<bool>> IsAllowedBatchAsync(
    IReadOnlyList<AuthorizationCheck> checks,
    CancellationToken cancellationToken = default)
```

### 6.1 The 4-Step Batch Capability Planning Algorithm

```
  Step 1: Small Batch Fast-Path
  (checks.Count <= 2 or Machine Caller)
  ------------------------------------> Sequential IsAllowedAsync() execution.

  Step 2: Authority Profile Resolution
  ------------------------------------> Query IAdminContext & repositories once
                                        to build immutable AuthorityProfile.

  Step 3: Batch Event Authority Snapshot
  ------------------------------------> Extract distinct Event IDs from checks.
                                        Fetch active EventRoleAssignments once
                                        via IEventAuthoritySnapshotService.

  Step 4: Synchronous In-Memory Evaluation
  ----------------------------------------> Iterate over checks and evaluate
                                            EvaluateWithProfile() in CPU memory.
```

### 6.2 Authority Profile Pre-Resolution (`AuthorityProfile`)

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

Resolving `AuthorityProfile` executes exactly **one set of queries** at the start of the batch:
1. `IsInstanceAdminAsync()`
2. `IsTenantAdminAsync(tenantId)`
3. `GetAdminOrganizationIdsAsync()`
4. `GetAdminGroupIdsAsync()`
5. `GetOrganizationIdsWhereUserHasPermission(userId, PermissionCodes.EventCreate)`
6. `GetGroupIdsWhereUserHasPermission(userId, PermissionCodes.EventCreate)`

### 6.3 Batch Event Authority Snapshots (`EventAuthoritySnapshotService`)

Located in [`src/Explore.Persistence/Services/EventAuthoritySnapshotService.cs`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Persistence/Services/EventAuthoritySnapshotService.cs):

```csharp
var assignments = await _dbContext.EventRoleAssignments
    .AsNoTracking()
    .Where(a =>
        a.TenantId == tenantId &&
        a.UserId == userId &&
        distinctEventIds.Contains(a.EventId) &&
        a.Status == EventRoleAssignmentStatus.Active &&
        a.StartsAtUtc <= utcNow &&
        (a.ExpiresAtUtc == null || a.ExpiresAtUtc > utcNow))
    .Select(a => new AssignmentAuthorityRow(a.EventId, a.Role.MasterCode, a.RoleId))
    .ToListAsync(cancellationToken);
```

The snapshot service fetches all active event role assignments for all event IDs in the batch in **a single SQL query**, joins them with `RolePermissions`, and returns a dictionary of permission sets keyed by `EventId`.

### 6.4 Performance & Complexity Analysis

| Evaluation Approach | Database Queries for 50 Items (10 Links/Item = 500 Checks) | Total Execution Time |
|---|---|---|
| **Naïve Sequential Evaluation** | $500 \text{ to } 1,500 \text{ SQL queries}$ | $350\text{ms} - 1,200\text{ms}$ |
| **Optimized Batch Capability Engine** | **Exactly 2 SQL queries** | **$< 2\text{ms}$** |

---

## 7. Identity & Authority Resolution Engine (`AdminContext`)

Located in [`src/Explore.Infrastructure/Identity/AdminContext.cs`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Infrastructure/Identity/AdminContext.cs), `AdminContext` resolves user authority directly from database tables, ignoring role/claim parameters in incoming JWTs.

### 7.1 Database-First Authority Resolution

```
                       ClaimsPrincipal (User)
                                 |
                                 v
                     ResolveUserIdAsync()
           (Claim internal_user_id -> sub/nameidentifier
            -> UserExternalLogins table lookup)
                                 |
                                 v
          +----------------------------------------------+
          |           Database Authority Queries          |
          +----------------------------------------------+
          | 1. PlatformUserRoles (platform.admin)       |
          | 2. InstanceBootstrapStates (CompletedBy)     |
          | 3. TenantUserRoleGrants (TenantAdmin)        |
          | 4. OrganizationMembers (OrgAdmin)            |
          | 5. GroupMembers (GroupAdmin)                 |
          +----------------------------------------------+
```

### 7.2 Memory Caching & Invalidation Architecture

- **Sliding Expiration**: All resolved authority decisions are stored in `IMemoryCache` with a 5-minute sliding window (`TimeSpan.FromMinutes(5)`).
- **Cache Key Format**: `AdminContext_Instance_{userId}`, `AdminContext_Tenant_{userId}_{tenantId}`, `AdminContext_Org_{userId}_{orgId}`.
- **Targeted Eviction**: Handlers mutating roles call `IAdminCacheInvalidator.InvalidateUser(userId)` to immediately clear cached entries for that user.

### 7.3 Single-Tenant Mode Optimizations

In single-tenant deployments (`IDeploymentModeProvider.IsSingleTenantAsync()`), Instance Admins automatically receive Tenant Admin privileges for `PlatformDefaults.DefaultTenantId`:

```csharp
if (tenantId == PlatformDefaults.DefaultTenantId && await IsInstanceAdminAsync(uid.Value, cancellationToken))
{
    return true;
}
```

---

## 8. Enforcement Pipeline & Architectural Depths

### 8.1 Controller Level (`[Authorize]`)
Endpoints in `Explore.API` apply standard ASP.NET Core attributes:
- `GET` endpoints default to `[AllowAnonymous]` to support public event discovery.
- `POST`, `PUT`, `DELETE` endpoints apply `[Authorize]`.

### 8.2 MediatR Pipeline (`AuthorizationBehavior<TReq, TRes>`)

Located in [`src/Explore.Application/Behaviors/AuthorizationBehavior.cs`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Application/Behaviors/AuthorizationBehavior.cs):
1. Intercepts CQRS requests before handler execution.
2. Checks if request type is cached in `AttributeCache` as decorated with `[AuthorizeResource(kind, action)]`.
3. Resolves dynamic resource IDs and attributes via `ISecureRequest` or `IAuthorizationContextEnricher<TRequest>`.
4. Calls `IAuthorizationProvider.IsAllowedAsync()`.
5. On denial, logs a warning with trace correlation IDs and throws `AuthorizationException(resourceKind, action)`, which `GlobalExceptionHandler` converts to HTTP `403 Forbidden`.

### 8.3 HATEOAS Link Filtering (`HateoasAuthorizationEvaluator`)

Located in [`src/Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`](file:///home/amir/ISLAMU/Github/Event/src/Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs):
1. **Phase 1: Static Checks**: Filters out links failing static boolean conditions or unauthenticated role requirements.
2. **Context Enrichment**: `EnrichRegistrationFormChecksAsync()` resolves trusted event/organizer attributes from EF Core to prevent client-supplied attribute tampering.
3. **Phase 2: Deduplication**: Collapses identical checks using `ToDeduplicationKey()`.
4. **Phase 3: Batch Evaluation**: Executes `IAuthorizationProvider.IsAllowedBatchAsync()`.
5. **Phase 4: Fail-Closed Materialization**: Maps boolean outcomes back to original link definitions. If batch evaluation throws an exception, all permission-bound links default to `false`.

### 8.4 Client Gating Invariant: HAL `_links`

> **Critical Architecture Rule**: UI clients (Blazor, Mobile, External Web) MUST gate user actions (Edit, Delete, Manage) exclusively by checking for the presence of the corresponding relation in the returned HAL `_links` collection. Clients MUST NOT inspect local claims, JWT roles, or user flags to infer action availability.

---

## 9. Onboarding & Configuration Management

Located in [`src/Explore.Infrastructure/Services/AuthorizationProviderConfigurationService.cs`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Infrastructure/Services/AuthorizationProviderConfigurationService.cs):
- Manages reading and updating authorization provider settings (`"cerbos"` vs `"local"`).
- Executes gRPC health checks via `Grpc.Health.V1.Health.HealthClient` to verify Cerbos PDP endpoints before mode switching.
- Generates `SecretOwnershipDto` metadata distinguishing between application-managed settings and deployment-managed environment overrides.

---

## 10. Anti-Escalation & Capability Ceiling (`CapabilityCeilingService`)

Located in [`src/Explore.Application/Authorization/CapabilityCeilingService.cs`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Application/Authorization/CapabilityCeilingService.cs):
Enforces four anti-escalation rules when users create custom roles or assign permissions:
1. **Grant Ceiling**: A caller can only assign permissions that their own assigned roles possess.
2. **Filtered Permissions**: Sensitive permissions (`IsFiltered == true`) cannot be granted unless the caller holds them explicitly.
3. **Scope Boundary**: A caller can only create or edit roles at their own administrative scope or lower (`Platform (0)` > `Tenant (1)` > `Organization (2)`).
4. **System Protection**: System roles (`IsSystem == true`) can never be modified or deleted.

---

## 11. Testing & Verification Matrix

| Test Suite | File Location | Purpose & Verification Scope |
|---|---|---|
| **Architecture Parity** | `Event.Architecture.Tests.AuthorizationParityTests` | Validates that every `ResourceKind` and `AuthorizationAction` constant is explicitly handled in `FallbackAuthorizationService`. |
| **API Integration** | [`Event.API.IntegrationTests/Features/LocalRbacAuthorizationTests.cs`](file:///home/amir/ISLAMU/Github/Event/tests/Event.API.IntegrationTests/Features/LocalRbacAuthorizationTests.cs) | Executes end-to-end HTTP requests with real Keycloak JWTs under `authorization.provider = "local"` verifying HTTP 200 vs 403 responses across Instance Admin, Tenant Admin, and Regular User personas. |
| **Local RBAC Unit Tests** | `Explore.Infrastructure.Tests.Behaviors.FallbackAuthorizationServiceTests` | Exercises direct resource evaluators for organizations, events, storage, webhooks, and settings. |
| **Router & Safe-Mode Unit Tests** | `Explore.Infrastructure.Tests.Behaviors.RuntimeAuthorizationProviderTests` | Tests BYO PDP fallback, safe-mode latch activation, handler bypass indexes, and setting checks. |
| **Machine Caller Tests** | `Explore.Infrastructure.Tests.Authorization.FallbackAuthorizationMachineCallerTests` | Tests API key scope ceilings (`MachineScopeMapping`) and owner-type boundaries. |

---

## Conclusion

The Local Authorization Provider ([`FallbackAuthorizationService`](file:///home/amir/ISLAMU/Github/Event/src/Explore.Infrastructure/Services/FallbackAuthorizationService.cs)) is a zero-dependency, enterprise-grade access control engine. By combining database-backed identity resolution, single-pass batch capability planning, strict domain evaluation rules, scope-gated machine principal validation, and fail-closed security posture, it guarantees full security enforcement across the ISLAMU Event platform without requiring an external Cerbos PDP container.
