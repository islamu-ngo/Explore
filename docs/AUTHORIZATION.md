ABOUTME: Consolidated authorization architecture, provider routing, and CQRS request patterns.
ABOUTME: Covers server-side enforcement, Cerbos/fallback behavior, and claim-related authorization notes.

# Authorization

This document consolidates all authorization-related knowledge for the platform.

## Table of Contents

1.  [Overview](#1-overview)
2.  [Authentication vs. Authorization](#2-authentication-vs-authorization)
3.  [Core Authorization Components](#3-core-authorization-components)
    *   [Endpoint-Level Authorization](#31-endpoint-level-authorization)
    *   [Resource-Level Authorization (MediatR)](#32-resource-level-authorization-mediatr)
    *   [Runtime Authorization Provider](#33-runtime-authorization-provider)
    *   [HATEOAS Link Authorization](#34-hateoas-link-authorization)
4.  [Authorization Providers](#4-authorization-providers)
    *   [Cerbos](#41-cerbos)
    *   [Fallback RBAC Service](#42-fallback-rbac-service)
    *   [Provider Resolution Flow](#43-provider-resolution-flow)
    *   [Failure Modes](#44-failure-modes)
5.  [Roles and Permissions](#5-roles-and-permissions)
    *   [Administrative Hierarchy](#51-administrative-hierarchy)
    *   [Permission Boundaries](#52-permission-boundaries)
6.  [Implementation Patterns](#6-implementation-patterns)
    *   [CQRS Authorization Patterns](#61-cqrs-authorization-patterns)
    *   [Claim-Based Authorization](#62-claim-based-authorization)
7.  [Related Documentation](#7-related-documentation)

---

## 1. Overview

The platform employs a multi-layered authorization strategy to ensure robust and flexible access control. It combines endpoint-level checks, fine-grained resource-level authorization within the application's business logic, and a runtime-pluggable provider model that supports both a sophisticated policy engine (Cerbos) and a local role-based access control (RBAC) fallback. This ensures security is enforced at multiple depths, from the web request down to individual data access.

## 2. Authentication vs. Authorization

-   **Authentication** is the process of verifying who a user is. In this platform, it is handled by Keycloak via an OIDC flow, managed by the Blazor BFF. The result is a JWT Bearer Token.
-   **Authorization** is the process of determining whether an authenticated user has the permission to perform a specific action on a specific resource. This is the primary focus of this document.

## 3. Core Authorization Components

For authentication, JWT validation, and security-header behavior, see [SECURITY.md](SECURITY.md).

### 3.1. Endpoint-Level Authorization

-   **Mechanism**: Standard ASP.NET Core `[Authorize]` and `[AllowAnonymous]` attributes on API controllers.
-   **Convention**:
    -   `GET` requests are generally `[AllowAnonymous]` to support public discovery.
    -   `POST`, `PUT`, `DELETE`, and `PATCH` requests are `[Authorize]` by default, requiring an authenticated user.
-   **Purpose**: A coarse, first-line defense at the entry point of the API.

### 3.2. Resource-Level Authorization (MediatR)

This is the core of the fine-grained authorization system, enforced within the MediatR request pipeline.

-   **Enforcement Point**: `AuthorizationBehavior<TRequest, TResponse>`. This pipeline behavior intercepts CQRS requests before they reach their handlers.
-   **Denial Behavior**: If authorization fails, the behavior throws an `AuthorizationException`. This is caught by the `GlobalExceptionHandler`, which returns an HTTP `403 Forbidden` response.
-   **Trigger Patterns**: The behavior is triggered by decorating CQRS request objects with specific interfaces or attributes. (See Implementation Patterns section below).

### 3.3. Runtime Authorization Provider

The actual logic of "is this user allowed to do this?" is delegated to a runtime provider. This allows the authorization engine to be swappable.

-   **Wrapper**: `RuntimeAuthorizationProvider` is injected into the `AuthorizationBehavior` and decides which concrete provider to use.
-   **Providers**:
    -   `CerbosAuthorizationService`: Offloads decision-making to an external Cerbos Policy Decision Point (PDP).
    -   `FallbackAuthorizationService`: Uses a local, database-backed RBAC implementation when local authorization is selected.

### 3.4. HATEOAS Link Authorization

The API uses a Hypermedia as the Engine of Application State (HATEOAS) model. HAL `_links` are the browser/client source of truth for action availability; Blazor and other clients must not recreate action gates from roles, claims, or cached local state.

-   **Mechanism**: `HateoasAuthorizationEvaluator` is used by resource assemblers and manual sync controllers before links are materialized.
-   **Behavior**: It evaluates the permissions required to execute each potential link. If the current user is not authorized, the link is omitted from the response. Permission-bound links fail closed when authorization evaluation fails; non-permission navigation links may remain when they only require authentication or static conditions.
-   **Metadata**: Link permission metadata includes resource kind, resource id, action, optional `AuthorizationScope`, and resource attributes. Descriptor-based links propagate scope and attributes from `ResourceDescriptors`; API-only links use explicit `AuthorizationActions` + `ResourceKinds` constants.
-   **Batching & Performance**: To avoid $N+1$ performance issues, the evaluator implements a **4-Phase Capability Planning Pipeline** (Candidate → Normalize → Batch Decision → Materialize). Deduplication includes resource kind/id/action, scope, and canonicalized attributes so scoped or attribute-sensitive links do not collapse into the wrong decision.
-   **Provider Optimizations**:
    -   **Cerbos**: Uses the official gRPC SDK to send deduplicated checks in a **single batch request** (`CheckResourcesAsync`).
    -   **Fallback (Local)**: Resolves the user's **Authority Profile** (admin status, tenant membership) **exactly once** per batch to eliminate redundant database/async overhead during individual link evaluation.
-   **Collection Support**: For "Get All" endpoints, all link definitions for all items in the paginated result are flattened into a single massive batch, ensuring high-scale efficiency.

## 4. Authorization Providers

### 4.1. Cerbos

-   **Description**: A powerful, open-source, stateless authorization service that allows policies to be defined in human-readable YAML files.
-   **Layering**: Application owns provider-neutral catalogs and checks (`AuthorizationActions`, `ResourceKinds`, `AuthorizationCheck`, `ResourceDescriptors`). Infrastructure owns Cerbos gRPC, Admin API, ZIP package export, client caching, and package publishing details.
-   **Usage**: When configured, the `CerbosAuthorizationService` translates the application's authorization request into a Cerbos `CheckResources` API call. Cerbos policy resource kinds are namespaced, for example `islamuevent_custom_property_template` and `islamuevent_custom_property_projection`.
-   **BYO (Bring Your Own) Cerbos**: The platform supports a multi-tenant model where each tenant can optionally provide their own Cerbos PDP and Admin API configuration.

### 4.1.1 Manual Cerbos Package Upload

When Admin API sync is unavailable, operators can push local policy and schema files with `cerbosctl` directly:

```bash
docker run --rm -it -v "/home/{user}/ISLAMU/Github/Event/cerbos/policies/_schemas:/schemas:ro" ghcr.io/cerbos/cerbosctl:0.53.0 --server={cerbos.example.com:443} --username={username} --password={password} put schema -R /schemas

docker run --rm -it -v "/home/{user}/ISLAMU/Github/Event/cerbos/policies:/policies:ro" ghcr.io/cerbos/cerbosctl:0.53.0 --server={cerbos.example.com:443} --username={username} --password={password} put policy -R /policies
```

If the password contains spaces, wrap it in quotes:

```bash
--password="password with spaces"
```

### 4.2. Fallback RBAC Service

-   **Description**: A local, in-database implementation of Role-Based Access Control. It serves as the default authorization provider. It is not used as an automatic fallback when the instance-level Cerbos provider is selected and unavailable.
-   **Logic**: The `FallbackAuthorizationService` contains hardcoded rules for known resources (`event`, `organization`, `tenant_setting`, etc.) and denies access by default for any unknown resource type.
-   **Notable Rules**:
    -   Instance administrators bypass most checks.
    -   Users can always view/update their own `user` resource.
    -   Updates to tenant settings can be denied if the setting is locked by an instance administrator.

### 4.3. Provider Resolution Flow

The `RuntimeAuthorizationProvider` selects the authorization engine for a given check in the following order:

1.  **Tenant BYO Cerbos**: If the current tenant has a specific "Bring Your Own" Cerbos instance configured, it is used.
2.  **Instance-Level Setting**: If not, the system checks the instance-wide `AuthorizationProvider` setting (from the `SystemSetting` table).
    -   If `"cerbos"`, it uses the instance's shared `CerbosAuthorizationService` and fails closed if the PDP is unavailable.
    -   If any other value (or null), it uses the local `FallbackAuthorizationService`.

If reading the instance provider setting fails, runtime authorization uses the Cerbos fail-closed path and logs only safe `FailureType` metadata. It does not default open to local RBAC.

### 4.4. Failure Modes

The system is designed to fail safely — deny by default when the configured provider is unavailable.

-   **Instance Cerbos Failure**: If the connection to the instance-level Cerbos PDP fails (e.g., network error, timeout), all authorization checks are denied. The operator explicitly chose Cerbos; falling back to a potentially more permissive local RBAC would silently bypass intended policies. Restore Cerbos connectivity or explicitly switch the authorization provider setting to local RBAC through instance administration to recover without Cerbos.
-   **BYO Cerbos Failure**:
    -   If the tenant's BYO configuration has `failure_mode=closed`, the fallback provider runs in provider-instance-scoped `SafeMode`, denying all requests except for those from an instance administrator.
    -   If `failure_mode=open`, the fallback provider runs its standard RBAC logic.
-   **BYO Configuration Failure**: If tenant BYO configuration cannot be resolved, runtime authorization activates provider-instance safe mode instead of silently using local RBAC.
-   **Blank BYO PDP Endpoint**: If a tenant explicitly sets `cerbos.mode=custom_endpoint` but leaves the custom PDP endpoint blank, the resolver preserves BYO mode, failure mode, and explicit BYO Admin API config. Runtime authorization then applies the configured `failure_mode`; it does not fall back to the instance PDP.
-   **Safe Logging**: Runtime failure logs avoid raw endpoints, Admin API credentials, JWTs/tokens, response bodies, and exception objects/messages. They keep safe operational metadata such as failure type, action, mode, counts, request id, and correlation id.

## 5. Roles and Permissions

### 5.1. Administrative Hierarchy

The platform defines a clear hierarchy of roles with distinct boundaries. See [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md) for a detailed breakdown.

-   **Instance Administrator**: Operates the infrastructure. Can manage tenants but cannot access tenant business data.
-   **Tenant Administrator**: Manages a specific community (tenant). Can configure the tenant, manage users and content within it, but cannot override instance-locked policies.
-   **Organization Administrator**: A user with elevated privileges within a specific organization inside a tenant.
-   **Standard User**: A regular platform user.

### 5.2. Permission Boundaries

Strict boundaries are enforced to protect tenant autonomy and platform integrity. For example, an Instance Admin cannot read tenant business data, and a Tenant Admin cannot disable globally enforced security policies.

Tenant user participation is tenant-local. A global `User` authenticates the person or external identity, but tenant-admin-controlled lifecycle and moderation state lives in `TenantUser`/`TenantUserProfile`. Tenant role authority lives in `TenantUserRoleGrant`, an auditable child of `TenantUser`. Local membership checks require an active tenant-local user record plus an unrevoked tenant-scoped grant, so a suspension, ban, removal, or profile moderation action in one tenant does not affect the same external identity in another tenant.

Managed-provider provisioning follows the same boundary. Provider/operator automation must authenticate through instance-admin authority before it can create customer tenants. The provisioned ERP customer/admin receives tenant-local `TenantUser`, `TenantUserProfile`, user actor, external-login binding, and `TenantUserRoleGrant` tenant-admin authority for that tenant only; this flow must not create `PlatformUserRole` rows or `InstanceAdmin` API keys for customer/admin identities.

## 6. Implementation Patterns

### 6.1. CQRS Authorization Patterns

Authorization is triggered in the MediatR pipeline based on one of three patterns applied to a command or query request class.

1.  **`IAuthorizedRequest` Interface**:
    -   **Use When**: The resource kind, ID, and action are dynamic and depend on the request's properties.
    -   **Implementation**: The request class implements `IAuthorizedRequest` and provides the `ResourceKind`, `ResourceId`, and `Action`.

2.  **`[AuthorizeResource]` Attribute**:
    -   **Use When**: The resource kind and action are static for all requests of this type.
    -   **Implementation**: The request class is decorated with `[AuthorizeResource(ResourceKind, Action)]`.

3.  **`[AuthorizeResource]` Attribute + `ISecureRequest` Interface**:
    -   **Use When**: The resource kind and action are static, but the resource ID or other attributes needed for the policy are determined at runtime.
    -   **Implementation**: A combination of the attribute and the interface. The behavior prefers the dynamic values from `ISecureRequest` at runtime.

### 6.2. Claim-Based Authorization

-   **User ID Extraction**: The standard fallback chain for user identity extraction is `sub` -> `nameidentifier` -> `sid`.
-   **`internal_user_id`**: This is a separate BFF-enriched local-user claim used by some UI/admin helpers after external identity resolution. It is not part of the general fallback chain.
-   **Admin Claims**: A `BffAdminClaimsTransformation` service enriches the user's principal with specific `admin` claims after authentication, which can be used for UI-level authorization checks.

## 7. Related Documentation

-   [SECURITY.md](SECURITY.md): Covers the broader security model, including authentication and JWT configuration.
-   [AUTHORIZATION_PATTERNS.md](AUTHORIZATION_PATTERNS.md): Quick reference for MediatR request-shape choices and provider fallback rules.
-   [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md): Details the roles and responsibilities of different administrative levels.
-   [API.md](API.md): Describes the MediatR pipeline and how authorization fits into the request flow.
-   [adr/ADR-001-authorization-provider-architecture.md](adr/ADR-001-authorization-provider-architecture.md): The original architectural decision record for this design.
