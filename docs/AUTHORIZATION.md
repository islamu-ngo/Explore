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
    -   `FallbackAuthorizationService`: Uses a local, database-backed RBAC implementation.

### 3.4. HATEOAS Link Authorization

The API uses a Hypermedia as the Engine of Application State (HATEOAS) model, and the links returned to the client are authorization-aware.

-   **Mechanism**: The `HateoasAuthorizationEvaluator` service is used by resource assemblers when building API responses.
-   **Behavior**: It evaluates the permissions required to execute the action associated with each potential link. If the current user is not authorized, the link is omitted from the response. This prevents the UI from displaying options that the user cannot act upon.
-   **Batching**: To avoid performance issues, it batches permission checks for all links in a response into a single call to the authorization provider.

## 4. Authorization Providers

### 4.1. Cerbos

-   **Description**: A powerful, open-source, stateless authorization service that allows policies to be defined in human-readable YAML files.
-   **Usage**: When configured, the `CerbosAuthorizationService` translates the application's authorization request into a Cerbos `CheckResources` API call. This allows for complex policies (e.g., resource-based, attribute-based, time-based) to be managed outside the application code.
-   **BYO (Bring Your Own) Cerbos**: The platform supports a multi-tenant model where each tenant can optionally provide their own Cerbos PDP configuration.

### 4.2. Fallback RBAC Service

-   **Description**: A local, in-database implementation of Role-Based Access Control. It serves as the default authorization provider and as a fallback if Cerbos is unavailable.
-   **Logic**: The `FallbackAuthorizationService` contains hardcoded rules for known resources (`event`, `organization`, `tenant_setting`, etc.) and denies access by default for any unknown resource type.
-   **Notable Rules**:
    -   Instance administrators bypass most checks.
    -   Users can always view/update their own `user` resource.
    -   Updates to tenant settings can be denied if the setting is locked by an instance administrator.

### 4.3. Provider Resolution Flow

The `RuntimeAuthorizationProvider` selects the authorization engine for a given check in the following order:

1.  **Tenant BYO Cerbos**: If the current tenant has a specific "Bring Your Own" Cerbos instance configured, it is used.
2.  **Instance-Level Setting**: If not, the system checks the instance-wide `AuthorizationProvider` setting (from the `SystemSetting` table).
    -   If `"cerbos"`, it uses the instance's shared `CerbosAuthorizationService`.
    -   If any other value (or null), it uses the `FallbackAuthorizationService`.

### 4.4. Failure Modes

The system is designed to fail safely.

-   **Instance Cerbos Failure**: If the connection to the instance-level Cerbos PDP fails (e.g., network error, timeout), the `RuntimeAuthorizationProvider` logs the error and automatically uses the `FallbackAuthorizationService` for that request.
-   **BYO Cerbos Failure**:
    -   If the tenant's BYO configuration has `failure_mode=closed`, the fallback provider runs in `SafeMode`, denying all requests except for those from an instance administrator.
    -   If `failure_mode=open`, the fallback provider runs its standard RBAC logic.

## 5. Roles and Permissions

### 5.1. Administrative Hierarchy

The platform defines a clear hierarchy of roles with distinct boundaries. See [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md) for a detailed breakdown.

-   **Instance Administrator**: Operates the infrastructure. Can manage tenants but cannot access tenant business data.
-   **Tenant Administrator**: Manages a specific community (tenant). Can configure the tenant, manage users and content within it, but cannot override instance-locked policies.
-   **Organization Administrator**: A user with elevated privileges within a specific organization inside a tenant.
-   **Standard User**: A regular platform user.

### 5.2. Permission Boundaries

Strict boundaries are enforced to protect tenant autonomy and platform integrity. For example, an Instance Admin cannot read tenant business data, and a Tenant Admin cannot disable globally enforced security policies.

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

-   **User ID Extraction**: The system uses a fallback chain to reliably extract the user's unique identifier from the JWT claims, checking in order: `internal_user_id` -> `sub` -> `nameidentifier` -> `sid`.
-   **Admin Claims**: A `BffAdminClaimsTransformation` service enriches the user's principal with specific `admin` claims after authentication, which can be used for UI-level authorization checks.

## 7. Related Documentation

-   [SECURITY.md](SECURITY.md): Covers the broader security model, including authentication and JWT configuration.
-   [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md): Details the roles and responsibilities of different administrative levels.
-   [API.md](API.md): Describes the MediatR pipeline and how authorization fits into the request flow.
-   [adr/ADR-001-authorization-provider-architecture.md](adr/ADR-001-authorization-provider-architecture.md): The original architectural decision record for this design.
