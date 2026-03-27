ABOUTME: Deep implementation report of current .NET Cerbos + HATEOAS authorization architecture in Clean Architecture + CQRS.
ABOUTME: Provides a direct Spring Boot implementation blueprint with a strict Cerbos-only safe-mode fallback (deny while Cerbos is unavailable).

# Cerbos + HATEOAS Authorization Implementation Report (for Spring Boot Port)

## 1. Scope and Objective

This report analyzes the current implementation in this repository for:

1. Authorization in Clean Architecture + CQRS (pipeline-level enforcement).
2. Cerbos integration (runtime provider behavior, request/decision model).
3. HATEOAS authorization (link-level filtering and batch decision flow).
4. Behavior on list/get-all endpoints and batched link authorization.
5. Existing fallback behavior and the exact change needed for your Java Spring Boot implementation:
   Cerbos is authoritative, and when Cerbos is down, system enters safe mode and denies non-emergency requests until recovery.

---

## 2. Current .NET Architecture (What Exists Today)

## 2.1 Core contracts and enforcement points

Authorization is enforced in two places:

1. CQRS pipeline behavior before handlers.
   - File: `Explore.Application/Behaviors/AuthorizationBehavior.cs`
   - Contract: `IAuthorizationProvider` (`Explore.Application/Contracts/Infrastructure/IAuthorizationProvider.cs`)
2. HATEOAS link filtering during response assembly.
   - Files:
     - `Explore.API/Hateoas/ResourceAssemblerBase.cs`
     - `Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`

This dual-layer model means:

1. Command/query execution is protected (actual business execution).
2. Hypermedia affordances are also protected (discoverability/UI guidance).

## 2.2 CQRS authorization model (MediatR behavior)

`AuthorizationBehavior<TRequest,TResponse>` applies three paths:

1. `IAuthorizedRequest` path:
   - Request carries `ResourceKind`, `ResourceId`, `Action`, optional `ResourceAttributes`.
2. `[AuthorizeResource]` attribute path:
   - Attribute supplies resource/action.
   - Optional `ISecureRequest` enriches dynamic `ResourceId`/attributes.
3. No contract/attribute:
   - Request passes through without authorization check.

Important implementation details:

1. Attribute lookup is cached via `ConcurrentDictionary<Type, AuthorizeResourceAttribute?>`.
2. Deny throws `AuthorizationException` (maps to HTTP 403 via exception handling chain).
3. OpenTelemetry activity span is emitted with tags:
   - `resource.kind`, `resource.action`, `request.type`.

Files:

1. `Explore.Application/Behaviors/AuthorizationBehavior.cs`
2. `Explore.Application/Authorization/AuthorizeResourceAttribute.cs`
3. `Explore.Application/Authorization/IAuthorizedRequest.cs`
4. `Explore.Application/Authorization/ISecureRequest.cs`

## 2.3 Resource/action normalization layer

`ResourceDescriptorRegistry` centralizes:

1. DTO type -> resource kind string.
2. `PermissionAction` enum -> action string (`read/create/update/delete/...`).

This is used by both:

1. CQRS authorization metadata.
2. HATEOAS link policy permission metadata.

Files:

1. `Explore.Application/Authorization/ResourceDescriptorRegistry.cs`
2. `Explore.Application/Authorization/PermissionAction.cs`
3. `Explore.API/Hateoas/LinkDefinitionPermissionExtensions.cs`

This avoids drift between “backend permission checks” and “link-level permission checks”.

---

## 3. Runtime authorization provider behavior (Cerbos + fallback)

## 3.1 Current runtime composition

Dependency registration:

1. `IAuthorizationProvider` -> `RuntimeAuthorizationProvider`
2. Concrete providers also registered:
   - `CerbosAuthorizationService`
   - `FallbackAuthorizationService`

File:

1. `Explore.Infrastructure/InfrastructureServicesRegistration.cs`

## 3.2 Current runtime decision routing

`RuntimeAuthorizationProvider` routing order:

1. Resolve tenant BYO Cerbos config (`ICerbosConfigResolver`).
2. If BYO configured:
   - call BYO Cerbos endpoint.
   - if unreachable:
     - `failure_mode=closed` -> activate fallback safe mode, then fallback provider evaluates (safe mode denies non-instance-admin).
     - `failure_mode=open` -> standard fallback RBAC.
3. If no BYO:
   - resolve instance provider setting `authorization.provider`.
   - if `cerbos`, try Cerbos then on exception fallback to local provider.
   - if not `cerbos`, use local provider.

File:

1. `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs`

## 3.3 Cerbos provider details

`CerbosAuthorizationService` characteristics:

1. Uses HTTP endpoint `/api/check/resources` (not gRPC).
2. Supports single and batch checks; single wraps into batch.
3. Principal built by `CerbosPrincipalBuilder`:
   - role: `authenticated_user`
   - attrs: `isInstanceAdmin`, `tenantMemberships`, `orgMemberships`.
4. Resource payload includes:
   - `kind`, `id`, `attr`.
   - auto-adds `tenantId` if missing and tenant context exists.
   - sets `scope=tenantId` for policy scoping.
5. Any non-success or missing decision -> deny.
6. On connectivity/timeouts in provider itself -> deny all for that request.

Files:

1. `Explore.Infrastructure/Services/CerbosAuthorizationService.cs`
2. `Explore.Infrastructure/Services/CerbosPrincipalBuilder.cs`
3. `Explore.Infrastructure/Services/CorrelationIdDelegatingHandler.cs`

## 3.4 Fallback provider details (local RBAC + safe mode latch)

`FallbackAuthorizationService` characteristics:

1. Role hierarchy via `IAdminContext` and resource attributes.
2. `SafeMode` is one-way latch (`ActivateSafeMode()`), logs critical once.
3. In safe mode:
   - instance admin allowed.
   - everyone else denied.
4. Has optimized batch path:
   - small batch (`<=2`) delegates to async single path.
   - larger batch pre-resolves authority profile once, then sync evaluate.

File:

1. `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`

## 3.5 Cerbos policies and parity

Cerbos policies are under `cerbos/policies/*.yaml` with derived role model.

Key files:

1. `cerbos/policies/derived_roles.yaml`
2. `cerbos/policies/event.yaml`
3. `cerbos/policies/event_registration.yaml`
4. `cerbos/policies/tenant_setting.yaml`

Architecture test enforces parity between:

1. `ResourceDescriptorRegistry` kinds.
2. fallback service handled kinds.
3. Cerbos policy files.

File:

1. `Event.Architecture.Tests/AuthorizationParityTests.cs`

---

## 4. HATEOAS authorization model

## 4.1 Link definition model

`LinkDefinition` supports:

1. static checks: `RequiresAuth`, `RequiredRoles`, `Condition`.
2. permission checks:
   - `PermissionResourceKind`
   - `PermissionAction`
   - `PermissionResourceId`
   - `PermissionResourceAttributes`

File:

1. `Explore.Application/Hateoas/LinkDefinition.cs`

## 4.2 Link policy pattern

Each resource has:

1. detail policy (`ILinkPolicy<TDto>`)
2. collection policy (`ICollectionLinkPolicy<TListDto>`)

Policies declare which links exist and which require permission checks.

Representative files:

1. `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
2. `Explore.API/Hateoas/Policies/OrganizationLinkPolicy.cs`
3. `Explore.API/Hateoas/Policies/EventRegistrationLinkPolicy.cs`
4. registrations: `Explore.API/Extensions/HateoasAssemblerRegistration.cs`

## 4.3 Batch link authorization engine

`HateoasAuthorizationEvaluator` logic:

1. Evaluate static checks first.
2. Build authorization checks only for permission-bound links.
3. Call `IAuthorizationProvider.IsAllowedBatchAsync` once per evaluated definition set.
4. On evaluator exception -> deny all permission-bound links (fail-closed for links).

File:

1. `Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`

## 4.4 Get-all/list endpoint behavior with batch processing

For list endpoints (critical to your request):

1. Controllers call `ToCollectionResource(...)`.
2. `ResourceAssemblerBase.BuildListResourcesWithBatch(...)`:
   - builds item link definitions for every item.
   - flattens all definitions into a single list.
   - runs one evaluator batch call for all links.
   - remaps decisions back to each item and link.

This is the central scalability mechanism for “get all” HATEOAS auth.

File:

1. `Explore.API/Hateoas/ResourceAssemblerBase.cs`

## 4.5 Prefer header behavior

`Prefer: return=minimal`:

1. middleware sets context flag.
2. assemblers skip generating links.
3. response includes `Preference-Applied: return=minimal`.

Files:

1. `Explore.API/Middleware/PreferHeaderMiddleware.cs`
2. `Explore.API/Extensions/HateoasServiceExtensions.cs`

---

## 5. List/Get-All + Authorization + Caching behavior

## 5.1 Event list endpoint characteristics

`GET /api/event` is `[AllowAnonymous]` and response is HAL collection.

File:

1. `Explore.API/Controllers/EventController.cs`

Implication:

1. Data query may be anonymous.
2. Hypermedia action links are still auth-filtered based on current principal and permission checks.

## 5.2 Output cache variation and auth-aware links

Output cache policies vary by `Authorization` header for list/detail data.

From `Program.cs`:

1. `ListData` varies by `Authorization`, tenant slug, host, pagination params.
2. `DetailData` varies by `Authorization`, tenant slug, host, route `id`.

This is required because HATEOAS links differ by user permissions.

File:

1. `Explore.API/Program.cs`

---

## 6. Current behavior vs your required Java behavior

## 6.1 Current .NET behavior summary

Today, .NET system can still authorize without Cerbos in several paths:

1. explicit local provider mode (`authorization.provider != cerbos`)
2. instance Cerbos failure -> local fallback
3. BYO failure with open mode -> local fallback
4. BYO failure with closed mode -> safe mode in fallback provider

## 6.2 Your required Java behavior (target)

You want:

1. Cerbos-only authorization model.
2. No runtime provider switching.
3. No normal RBAC fallback.
4. If Cerbos is unavailable:
   - enter safe mode.
   - block authorized requests until Cerbos recovers.

This is stricter than current .NET default instance behavior.

---

## 7. Spring Boot implementation blueprint (same architecture, strict Cerbos)

## 7.1 Keep the same conceptual layers

Map current design 1:1:

1. Application/CQRS pipeline authorization behavior.
2. HATEOAS link policy + batch evaluator.
3. Shared resource/action registry.
4. Cerbos provider as single authority.

Do not port runtime provider-selection logic or local RBAC fallback decision tables into runtime path.

## 7.2 Recommended Java components

1. `AuthorizationProvider` interface:
   - `isAllowed(...)`
   - `isAllowedBatch(...)`
2. `CerbosAuthorizationService` as the only implementation used by application authorization.
3. `AuthorizationBehavior` in CQRS pipeline/interceptor:
   - checks marker interface or annotation equivalent.
4. `HateoasAuthorizationEvaluator`:
   - static checks then batch permission checks.
5. `ResourceAssemblerBase` equivalent:
   - flattened item-link batch evaluation for list responses.
6. `SafeModeAuthorizationGate` (global state/service):
   - one-way latch active while Cerbos unavailable.

## 7.3 Safe mode semantics for strict Cerbos-only

Implement two states:

1. `NORMAL`
2. `SAFE_MODE`

Transitions:

1. `NORMAL -> SAFE_MODE` when Cerbos check fails due to connectivity/timeout/circuit open.
2. `SAFE_MODE -> NORMAL` after successful Cerbos health probe and explicit recovery policy.

Decision behavior:

1. In safe mode:
   - deny all permission checks by default.
   - optionally allow only minimal emergency endpoints (if you define them).
2. Do not execute local RBAC fallback decisions.
3. Do not switch to any alternate provider during runtime.

## 7.4 Circuit breaker + timeout

Mirror .NET resilience intent:

1. short timeout for Cerbos checks (2-3s).
2. circuit breaker to avoid cascading latency.
3. on breaker open / timeout / network failure:
   - activate safe mode latch.
   - deny checks.
4. recovery should be explicit:
   - periodic Cerbos health probe.
   - require N consecutive healthy probes before exiting safe mode (to avoid flapping).

## 7.5 CQRS request contracts

Mirror existing patterns:

1. annotation-driven requests (`@AuthorizeResource(kind, action)`)
2. optional dynamic context interface (`SecureRequest`) for:
   - `resourceId`
   - `resourceAttributes`
3. fallback resource id for annotated requests without dynamic id:
   - request type name (same as .NET behavior)

## 7.6 HATEOAS + batch checks in list endpoints

Preserve exactly this behavior:

1. Build all link definitions for all list items.
2. Evaluate static checks locally.
3. Batch remaining permission-bound checks to Cerbos in one request.
4. Fail-closed at link level if batch fails.
5. Reconstruct per-item authorized links.

This is essential for performance and consistency with get-all responses.

## 7.7 Required resource attributes in Java port

Cerbos derived role logic depends on attributes:

1. `tenantId` for tenant_admin derivation.
2. `organizationId` for org_admin derivation.
3. additional attributes like `isLockedByInstance` for setting policies.

If attributes are omitted, many decisions degrade to deny and links disappear unexpectedly.

## 7.8 Principal shape to preserve

Build Cerbos principal with:

1. roles: `["authenticated_user"]`
2. attrs:
   - `isInstanceAdmin`
   - `tenantMemberships` map
   - `orgMemberships` map

This must match derived role policy expectations.

---

## 8. Porting checklist (implementation-critical)

1. Build a central `ResourceDescriptorRegistry` equivalent and keep it shared between CQRS behavior and HATEOAS.
2. Implement annotation + dynamic interface authorization pipeline behavior.
3. Implement Cerbos batch API client (`/api/check/resources`) with correlation IDs.
4. Implement strict safe mode gate with no local fallback decisions and no runtime provider switching.
5. Implement HATEOAS link definition model with permission metadata.
6. Implement batch link evaluator and list flatten/remap algorithm.
7. Keep cache variance on auth identity/credentials where responses include auth-sensitive links.
8. Port Cerbos policies and derived roles unchanged (or semantically equivalent) before feature rollout.
9. Add architecture tests equivalent to `AuthorizationParityTests` to prevent registry/policy drift.
10. Add integration tests for:
    - anonymous vs authenticated links on get-all.
    - permission-bound links hidden when denied.
    - safe mode deny behavior when Cerbos is unreachable.
    - recovery from safe mode after Cerbos health returns.

---

## 9. OpenAPI + Swagger + HATEOAS schema parity (must-have for Java)

You explicitly requested full parity for OpenAPI schema support with HATEOAS. Current .NET implementation has a layered solution you should replicate conceptually.

## 9.1 Dual OpenAPI surfaces in .NET

Current API exposes both:

1. Swashbuckle (`AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI`) for interactive docs.
2. Native ASP.NET OpenAPI (`AddOpenApi`, `MapOpenApi`) for document export and downstream tooling.

File:

1. `Explore.API/Program.cs`

## 9.2 HAL wrapper schema problem being solved

Because HAL wrappers flatten DTO properties and include `_links`/`_embedded`, vanilla schema generation is insufficient.

Current .NET solution explicitly patches schema generation for HAL wrappers to:

1. include inner DTO properties.
2. include HAL `_links` and `_embedded`.
3. ensure arrays under embedded/items reference proper component schemas (avoid duplicate/incorrect generated client types).

## 9.3 Swashbuckle path (interactive docs parity)

Swashbuckle configuration includes:

1. custom schema IDs for stability.
2. `HalSchemaFilter` to enrich schemas for:
   - `HalResource<T>`
   - `HalCollectionEmbedded<T>`
3. OAuth2/Keycloak security scheme docs.

Files:

1. `Explore.API/Extensions/ServiceCollectionExtensions.cs`
2. `Explore.API/OpenApi/HalSchemaFilter.cs`

## 9.4 Native OpenAPI path (export + client generation parity)

Native OpenAPI path uses document transformation:

1. `HalDtoSchemaTransformer` registers DTO schemas used only inside HAL wrappers.
2. Populates HAL wrapper schemas with flattened DTO properties + HAL metadata.
3. Replaces inline array item schemas with `$ref` component references to fix client generation collisions.

Files:

1. `Explore.API/OpenApi/HalDtoSchemaTransformer.cs`
2. `Explore.API/Program.cs` (AddOpenApi registration)

## 9.5 OpenAPI export pipeline (for generated clients)

At startup (Development), API exports OpenAPI JSON file for client generation workflow.

File:

1. `Explore.API/Services/OpenApiExportService.cs`

Operational behavior:

1. fetches `/openapi/event-api.json`.
2. pretty-prints and writes `swagger.json` in API project root.
3. downstream clients consume this schema for code generation.

## 9.6 Java Spring Boot parity requirements (what to implement)

For equivalent Java behavior, implement all of the following:

1. HAL-aware schema customization in your OpenAPI generation pipeline (springdoc customizers/model converters).
2. Explicit schema representation of:
   - `_links` object with `href/method/title`.
   - `_embedded` object with typed `items` arrays.
   - flattened DTO properties when your runtime serialization flattens resource payloads.
3. Stable component schema references for nested list DTOs to prevent duplicate generated models.
4. OpenAPI security schemes equivalent to your auth setup (OIDC/OAuth2 bearer usage).
5. OpenAPI export artifact generation step in dev/build pipeline for client codegen parity.
6. Integration tests that assert OpenAPI document correctness for HAL resources and collections.

Without these, you may have working endpoints but broken or misleading generated clients.

---

## 10. Gaps and risks observed in current .NET codebase (useful for Java design)

1. There are strong tests for `AuthorizationBehavior` and fallback service behavior, but no dedicated test class for `RuntimeAuthorizationProvider` routing branches (BYO open/closed, instance fallback path).
2. HATEOAS authorization tests are mostly integration-level behavior checks; there is limited direct unit coverage of evaluator branch logic (especially exception path and decision index remapping).
3. `CerbosAuthorizationServiceTests` currently contains a non-complete `Dispose()` (throws `NotImplementedException`) which should be fixed in .NET, though not directly blocking your Java implementation.

These are opportunities to make the Java version stronger from day one.

---

## 11. Exact adaptation recommendation for your Spring Boot app

Implement the same architecture, but collapse provider strategy to:

1. Do not implement runtime provider selection for authorization.
2. Wire authorization directly to Cerbos provider in application pipeline and HATEOAS evaluator.
3. Keep a separate safe-mode gate for outage handling, not a fallback decision engine.
4. On Cerbos failure:
   - set safe mode active.
   - deny checks until Cerbos health indicates recovery.
5. No local RBAC fallback execution for normal authorization.
6. Replicate OpenAPI+HAL schema customizations end-to-end so Swagger UI, exported OpenAPI, and generated clients all understand:
   - flattened HAL resources,
   - typed `_embedded.items`,
   - authorization-aware link model metadata.

In short:

1. Keep CQRS + HATEOAS + batch mechanics exactly.
2. Keep Cerbos request/principal/resource contract exactly.
3. Replace fallback policy execution with strict safe-mode deny semantics.
4. Keep full OpenAPI/Swagger/HATEOAS schema pipeline parity.

That gives you behavioral parity with your existing API architecture while enforcing your stricter operational security requirement in Java.

---

## 12. Implementation-grade Java blueprint (difficult/non-intuitive parts)

This section gives concrete code patterns you can directly adapt in Spring Boot.

## 12.1 Authorization contracts (Java)

Use explicit contracts equivalent to `.NET` `IAuthorizedRequest` / `ISecureRequest` / `[AuthorizeResource]`.

```java
package com.example.auth;

import java.util.Map;

public interface AuthorizedRequest {
    String resourceKind();
    String resourceId();
    String action();
    default Map<String, Object> resourceAttributes() { return null; }
}
```

```java
package com.example.auth;

import java.util.Map;

public interface SecureRequest {
    default String resourceId() { return null; }
    default Map<String, Object> resourceAttributes() { return null; }
}
```

```java
package com.example.auth;

import java.lang.annotation.*;

@Target(ElementType.TYPE)
@Retention(RetentionPolicy.RUNTIME)
@Inherited
public @interface AuthorizeResource {
    String resource();
    PermissionAction action();
}
```

```java
package com.example.auth;

public enum PermissionAction {
    READ, CREATE, UPDATE, DELETE, MANAGE_MEMBERS, VIEW_SHARED_CONTACTS, EXPORT_SHARED_CONTACTS;

    public String toPolicyAction() {
        return switch (this) {
            case READ -> "read";
            case CREATE -> "create";
            case UPDATE -> "update";
            case DELETE -> "delete";
            case MANAGE_MEMBERS -> "manage_members";
            case VIEW_SHARED_CONTACTS -> "viewsharedcontacts";
            case EXPORT_SHARED_CONTACTS -> "exportsharedcontacts";
        };
    }
}
```

Why this is critical:

1. You need both static metadata (annotation) and dynamic runtime attributes (tenantId/organizationId).
2. If you rely only on static annotation, Cerbos derived roles break for org/tenant scoped resources.

## 12.2 Authorization provider contract (Cerbos-only)

```java
package com.example.auth;

import java.util.List;
import java.util.Map;

public interface AuthorizationProvider {
    boolean isAllowed(String resourceKind, String resourceId, String action, Map<String, Object> resourceAttributes);
    List<Boolean> isAllowedBatch(List<AuthorizationCheck> checks);
}
```

```java
package com.example.auth;

import java.util.Map;

public record AuthorizationCheck(
    String resourceKind,
    String resourceId,
    String action,
    Map<String, Object> resourceAttributes
) {}
```

Design decision for your target:

1. Register one bean only for authorization decisions: Cerbos-backed provider.
2. Safe-mode gate wraps that provider behavior, but never replaces it with local RBAC policy logic.

## 12.3 CQRS pipeline behavior/interceptor (hard part)

Equivalent to `.NET` `AuthorizationBehavior`.

```java
package com.example.auth;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

public final class AuthorizationPipelineBehavior implements RequestBehavior {
    private final AuthorizationProvider authorizationProvider;
    private final SafeModeAuthorizationGate safeModeGate;
    private final ConcurrentHashMap<Class<?>, AuthorizeResource> attributeCache = new ConcurrentHashMap<>();

    public AuthorizationPipelineBehavior(AuthorizationProvider authorizationProvider,
                                         SafeModeAuthorizationGate safeModeGate) {
        this.authorizationProvider = authorizationProvider;
        this.safeModeGate = safeModeGate;
    }

    @Override
    public Object handle(Object request, RequestHandler next) {
        if (request instanceof AuthorizedRequest ar) {
            enforce(ar.resourceKind(), ar.resourceId(), ar.action(), ar.resourceAttributes(), request.getClass().getSimpleName());
            return next.handle();
        }

        AuthorizeResource meta = attributeCache.computeIfAbsent(request.getClass(),
            cls -> cls.getAnnotation(AuthorizeResource.class));

        if (meta != null) {
            String resourceId = request.getClass().getSimpleName();
            Map<String, Object> attrs = null;

            if (request instanceof SecureRequest sr) {
                if (sr.resourceId() != null && !sr.resourceId().isBlank()) resourceId = sr.resourceId();
                attrs = sr.resourceAttributes();
            }

            enforce(meta.resource(), resourceId, meta.action().toPolicyAction(), attrs, request.getClass().getSimpleName());
        }

        return next.handle();
    }

    private void enforce(String resourceKind, String resourceId, String action, Map<String, Object> attrs, String requestType) {
        if (!safeModeGate.isRequestAllowed(resourceKind, action)) {
            throw new AuthorizationDeniedException(resourceKind, action, "safe_mode");
        }

        boolean allowed = authorizationProvider.isAllowed(resourceKind, resourceId, action, attrs);
        if (!allowed) {
            throw new AuthorizationDeniedException(resourceKind, action, "policy_deny");
        }
    }
}
```

Non-intuitive behavior you must preserve:

1. For annotation path + missing dynamic resource id, fallback to request class name.
2. Cache annotation lookup.
3. Throw one domain authorization exception mapped globally to HTTP 403.

## 12.4 Cerbos principal and payload mapping (hard part)

### Principal shape

```java
public record CerbosPrincipal(
    String id,
    java.util.List<String> roles,
    java.util.Map<String, Object> attr
) {}
```

```java
public final class CerbosPrincipalBuilder {
    private final AdminContext adminContext;

    public CerbosPrincipalBuilder(AdminContext adminContext) {
        this.adminContext = adminContext;
    }

    public CerbosPrincipal build(java.util.UUID userId) {
        boolean isInstanceAdmin = adminContext.isInstanceAdmin();
        var tenantMemberships = new java.util.HashMap<String, Object>();
        for (var tenantId : adminContext.getAdminTenantIds()) tenantMemberships.put(tenantId.toString(), "admin");
        var orgMemberships = new java.util.HashMap<String, Object>();
        for (var orgId : adminContext.getAdminOrganizationIds()) orgMemberships.put(orgId.toString(), "admin");

        var attr = new java.util.HashMap<String, Object>();
        attr.put("isInstanceAdmin", isInstanceAdmin);
        attr.put("tenantMemberships", tenantMemberships);
        attr.put("orgMemberships", orgMemberships);

        return new CerbosPrincipal(userId.toString(), java.util.List.of("authenticated_user"), attr);
    }
}
```

### Resource request model

```java
public record CerbosResource(
    String kind,
    String id,
    java.util.Map<String, Object> attr,
    String scope
) {}

public record CerbosResourceAction(
    CerbosResource resource,
    java.util.List<String> actions
) {}

public record CerbosCheckRequest(
    String requestId,
    CerbosPrincipal principal,
    java.util.List<CerbosResourceAction> resources
) {}
```

### Batch check call

```java
public final class CerbosAuthorizationService implements AuthorizationProvider {
    private final CerbosPrincipalBuilder principalBuilder;
    private final AdminContext adminContext;
    private final TenantContext tenantContext;
    private final CerbosHttpClient client;
    private final SafeModeAuthorizationGate safeModeGate;

    @Override
    public boolean isAllowed(String resourceKind, String resourceId, String action, java.util.Map<String, Object> attrs) {
        var results = isAllowedBatch(java.util.List.of(new AuthorizationCheck(resourceKind, resourceId, action, attrs)));
        return !results.isEmpty() && results.get(0);
    }

    @Override
    public java.util.List<Boolean> isAllowedBatch(java.util.List<AuthorizationCheck> checks) {
        if (checks.isEmpty()) return java.util.List.of();
        if (!safeModeGate.isCerbosPathEnabled()) return denyAll(checks.size());

        var userId = adminContext.getUserId();
        if (userId == null) return denyAll(checks.size());

        String requestId = java.util.UUID.randomUUID().toString();
        var principal = principalBuilder.build(userId);
        String tenantScope = tenantContext.getTenantId() != null ? tenantContext.getTenantId().toString() : null;

        var resources = new java.util.ArrayList<CerbosResourceAction>(checks.size());
        for (var c : checks) {
            var attrs = c.resourceAttributes() != null
                ? new java.util.HashMap<>(c.resourceAttributes())
                : new java.util.HashMap<String, Object>();

            if (!attrs.containsKey("tenantId") && tenantScope != null) attrs.put("tenantId", tenantScope);

            var resource = new CerbosResource(c.resourceKind(), c.resourceId(), attrs, tenantScope);
            resources.add(new CerbosResourceAction(resource, java.util.List.of(c.action())));
        }

        try {
            var response = client.checkResources(new CerbosCheckRequest(requestId, principal, resources));
            return mapDecisions(checks, response);
        } catch (Exception ex) {
            safeModeGate.activate("cerbos_unreachable", ex);
            return denyAll(checks.size());
        }
    }

    private java.util.List<Boolean> mapDecisions(java.util.List<AuthorizationCheck> checks, CerbosCheckResponse response) {
        var out = new java.util.ArrayList<Boolean>(checks.size());
        for (int i = 0; i < checks.size(); i++) {
            var check = checks.get(i);
            String effect = response.effectAt(i, check.action()); // helper: null if missing
            out.add("EFFECT_ALLOW".equals(effect));
        }
        return out;
    }

    private java.util.List<Boolean> denyAll(int count) {
        var out = new java.util.ArrayList<Boolean>(count);
        for (int i = 0; i < count; i++) out.add(false);
        return out;
    }
}
```

Important:

1. Missing decision must be deny.
2. Transport failure must trigger safe mode and deny.
3. Keep positional mapping (request i -> result i).

## 12.5 Safe mode gate (hard part)

Do not model safe mode as a boolean only. Use state + metadata for operability.

```java
package com.example.auth;

import java.time.Instant;
import java.util.concurrent.atomic.AtomicReference;

public final class SafeModeAuthorizationGate {
    public enum Mode { NORMAL, SAFE_MODE }

    public record State(Mode mode, Instant since, String reason) {
        public static State normal() { return new State(Mode.NORMAL, null, null); }
    }

    private final AtomicReference<State> state = new AtomicReference<>(State.normal());
    private volatile int healthyProbeStreak = 0;
    private final int recoveryThreshold;

    public SafeModeAuthorizationGate(int recoveryThreshold) {
        this.recoveryThreshold = recoveryThreshold;
    }

    public void activate(String reason, Throwable ex) {
        state.updateAndGet(cur -> cur.mode == Mode.SAFE_MODE
            ? cur
            : new State(Mode.SAFE_MODE, Instant.now(), reason));
        healthyProbeStreak = 0;
    }

    public boolean isCerbosPathEnabled() {
        return state.get().mode == Mode.NORMAL;
    }

    public boolean isRequestAllowed(String resourceKind, String action) {
        if (state.get().mode == Mode.NORMAL) return true;
        return false;
    }

    public void onHealthProbeSuccess() {
        if (state.get().mode == Mode.NORMAL) return;
        healthyProbeStreak++;
        if (healthyProbeStreak >= recoveryThreshold) {
            state.set(State.normal());
            healthyProbeStreak = 0;
        }
    }

    public void onHealthProbeFailure() {
        healthyProbeStreak = 0;
    }

    public State currentState() {
        return state.get();
    }
}
```

Operational recommendation:

1. Expose `/internal/auth/safe-mode` endpoint for observability.
2. Emit structured logs and metrics on state transitions.

## 12.6 HATEOAS link model + evaluator (hard part)

### Link definition

```java
package com.example.hateoas;

import java.util.Map;
import java.util.function.BooleanSupplier;

public record LinkDefinition(
    String rel,
    String routeName,
    Object routeValues,
    String method,
    String title,
    boolean requiresAuth,
    String[] requiredRoles,
    BooleanSupplier condition,
    String permissionResourceKind,
    String permissionAction,
    String permissionResourceId,
    Map<String, Object> permissionResourceAttributes
) {}
```

### Evaluator

```java
public final class HateoasAuthorizationEvaluator {
    private final AuthorizationProvider authorizationProvider;

    public java.util.List<Boolean> areLinksAllowed(java.util.List<LinkDefinition> defs, PrincipalView user) {
        if (defs.isEmpty()) return java.util.List.of();

        var results = new boolean[defs.size()];
        var pending = new java.util.ArrayList<IndexedCheck>();

        for (int i = 0; i < defs.size(); i++) {
            var d = defs.get(i);
            if (!passesStaticChecks(d, user)) {
                results[i] = false;
                continue;
            }

            var check = buildCheck(d);
            if (check == null) {
                results[i] = true;
                continue;
            }
            pending.add(new IndexedCheck(i, check));
        }

        if (pending.isEmpty()) return toList(results);

        try {
            var batch = pending.stream().map(IndexedCheck::check).toList();
            var allowed = authorizationProvider.isAllowedBatch(batch);
            for (int i = 0; i < pending.size(); i++) {
                int index = pending.get(i).index();
                results[index] = i < allowed.size() && Boolean.TRUE.equals(allowed.get(i));
            }
        } catch (Exception ex) {
            for (var p : pending) results[p.index()] = false;
        }

        return toList(results);
    }

    private boolean passesStaticChecks(LinkDefinition d, PrincipalView user) {
        if (d.condition() != null && !d.condition().getAsBoolean()) return false;
        if (d.requiresAuth() && (user == null || !user.isAuthenticated())) return false;
        if (d.requiredRoles() != null && d.requiredRoles().length > 0) {
            if (user == null || !user.isAuthenticated()) return false;
            boolean hasRole = false;
            for (String role : d.requiredRoles()) {
                if (user.hasRole(role)) { hasRole = true; break; }
            }
            if (!hasRole) return false;
        }
        return true;
    }

    private AuthorizationCheck buildCheck(LinkDefinition d) {
        if (d.permissionResourceKind() == null || d.permissionResourceKind().isBlank()) return null;
        String action = d.permissionAction() != null && !d.permissionAction().isBlank()
            ? d.permissionAction()
            : mapMethodToAction(d.method());
        if (action == null) return null;
        String resourceId = d.permissionResourceId() != null
            ? d.permissionResourceId()
            : extractResourceId(d.routeValues(), d.routeName());
        return new AuthorizationCheck(d.permissionResourceKind(), resourceId, action, d.permissionResourceAttributes());
    }

    private String mapMethodToAction(String method) {
        if (method == null) return null;
        return switch (method.toUpperCase()) {
            case "GET" -> "read";
            case "POST" -> "create";
            case "PUT", "PATCH" -> "update";
            case "DELETE" -> "delete";
            default -> null;
        };
    }

    private String extractResourceId(Object routeValues, String fallback) {
        if (routeValues instanceof Map<?, ?> m) {
            for (String k : java.util.List.of("id", "tenantId", "organizationId", "did", "userId")) {
                for (var e : m.entrySet()) {
                    if (k.equalsIgnoreCase(String.valueOf(e.getKey())) && e.getValue() != null) {
                        return String.valueOf(e.getValue());
                    }
                }
            }
        }
        return fallback;
    }

    private java.util.List<Boolean> toList(boolean[] arr) {
        var out = new java.util.ArrayList<Boolean>(arr.length);
        for (boolean b : arr) out.add(b);
        return out;
    }

    private record IndexedCheck(int index, AuthorizationCheck check) {}
}
```

Non-intuitive but essential:

1. Static checks must happen before Cerbos batch to reduce payload/latency.
2. Exception in batch evaluation must fail-closed for permission-bound links.
3. Index remapping logic must be exact.

## 12.7 Batch remap algorithm in collection assembly (hard part)

Equivalent to `.NET` `BuildListResourcesWithBatch`.

```java
public final class ResourceAssemblerBase<TDetail, TList> {
    private final LinkGenerator linkGenerator;
    private final HateoasAuthorizationEvaluator evaluator;

    public HalCollectionResource<TList> toCollectionResource(PaginatedResult<TList> page, LinkPolicy<TList> itemPolicy, CollectionPolicy collectionPolicy, PrincipalView user) {
        var itemDefs = new java.util.ArrayList<java.util.List<LinkDefinition>>(page.items().size());
        for (var item : page.items()) itemDefs.add(itemPolicy.itemLinks(item, user));

        var flattened = new java.util.ArrayList<LinkDefinition>();
        for (var defs : itemDefs) flattened.addAll(defs);

        var decisions = evaluator.areLinksAllowed(flattened, user);

        var cursor = 0;
        var halItems = new java.util.ArrayList<HalResource<TList>>(page.items().size());
        for (int itemIndex = 0; itemIndex < page.items().size(); itemIndex++) {
            var defs = itemDefs.get(itemIndex);
            var links = new java.util.LinkedHashMap<String, HalLink>();

            for (int defIndex = 0; defIndex < defs.size(); defIndex++) {
                int globalIndex = cursor + defIndex;
                if (globalIndex >= decisions.size() || !Boolean.TRUE.equals(decisions.get(globalIndex))) continue;
                var d = defs.get(defIndex);
                var link = linkGenerator.generate(d);
                if (link != null) links.put(d.rel(), link);
            }

            cursor += defs.size();
            halItems.add(new HalResource<>(page.items().get(itemIndex), links, null));
        }

        var rootLinks = linkGenerator.paginationLinks(page);
        var collectionDefs = collectionPolicy.collectionLinks(user);
        var collectionAllowed = evaluator.areLinksAllowed(collectionDefs, user);
        for (int i = 0; i < collectionDefs.size(); i++) {
            if (!Boolean.TRUE.equals(collectionAllowed.get(i))) continue;
            var d = collectionDefs.get(i);
            var link = linkGenerator.generate(d);
            if (link != null) rootLinks.put(d.rel(), link);
        }

        return HalCollectionResource.from(page, halItems, rootLinks);
    }
}
```

Failure mode to avoid:

1. Evaluating links item-by-item with one Cerbos call per item. This will explode latency on big lists.

## 12.8 Output cache variation (auth-aware HAL)

For list/detail HAL endpoints, vary cache at least by:

1. tenant identifier.
2. authorization identity context (or token hash/role signature).
3. query params for lists.

Reason:

1. Same data can have different `_links` depending on permissions.

## 12.9 OpenAPI/HAL schema customization in Spring Boot (hard part)

You need springdoc customizers/model converters to produce correct schemas for HAL wrappers.

### Model converter outline

```java
@Component
public final class HalModelConverter implements io.swagger.v3.core.converter.ModelConverter {
    @Override
    public io.swagger.v3.oas.models.media.Schema<?> resolve(
        io.swagger.v3.core.converter.AnnotatedType type,
        io.swagger.v3.core.converter.ModelConverterContext context,
        java.util.Iterator<io.swagger.v3.core.converter.ModelConverter> chain
    ) {
        Class<?> raw = type.getRawClass();
        if (raw == null) return chain.hasNext() ? chain.next().resolve(type, context, chain) : null;

        if (isHalResource(raw, type)) {
            Class<?> dtoType = resolveGeneric(type, 0);
            var dtoSchema = context.resolve(new io.swagger.v3.core.converter.AnnotatedType(dtoType));
            var out = new io.swagger.v3.oas.models.media.ObjectSchema();
            copyProperties(out, dtoSchema);
            out.addProperties("_links", halLinksSchema());
            out.addProperties("_embedded", new io.swagger.v3.oas.models.media.ObjectSchema().nullable(true));
            return out;
        }

        if (isHalCollectionResource(raw, type)) {
            Class<?> itemType = resolveGeneric(type, 0);
            var halItemSchema = context.resolve(new io.swagger.v3.core.converter.AnnotatedType(makeHalResourceType(itemType)));
            var out = new io.swagger.v3.oas.models.media.ObjectSchema();
            out.addProperties("_links", halLinksSchema());
            out.addProperties("_embedded", new io.swagger.v3.oas.models.media.ObjectSchema()
                .addProperties("items", new io.swagger.v3.oas.models.media.ArraySchema().items(halItemSchema)));
            out.addProperties("pageNumber", new io.swagger.v3.oas.models.media.IntegerSchema());
            out.addProperties("pageSize", new io.swagger.v3.oas.models.media.IntegerSchema());
            out.addProperties("totalCount", new io.swagger.v3.oas.models.media.IntegerSchema());
            out.addProperties("totalPages", new io.swagger.v3.oas.models.media.IntegerSchema());
            return out;
        }

        return chain.hasNext() ? chain.next().resolve(type, context, chain) : null;
    }

    private io.swagger.v3.oas.models.media.Schema<?> halLinksSchema() {
        var linkObj = new io.swagger.v3.oas.models.media.ObjectSchema()
            .addProperties("href", new io.swagger.v3.oas.models.media.StringSchema())
            .addProperties("method", new io.swagger.v3.oas.models.media.StringSchema())
            .addProperties("title", new io.swagger.v3.oas.models.media.StringSchema().nullable(true));
        return new io.swagger.v3.oas.models.media.ObjectSchema().additionalProperties(linkObj);
    }
}
```

### OpenAPI customizer for security and consistency

```java
@Bean
public OpenApiCustomiser halAndSecurityCustomiser() {
    return openApi -> {
        if (openApi.getComponents() == null) openApi.setComponents(new Components());

        openApi.getComponents().addSecuritySchemes("oidc", new SecurityScheme()
            .type(SecurityScheme.Type.OAUTH2)
            .flows(new OAuthFlows().implicit(new OAuthFlow().authorizationUrl("https://<auth>/authorize"))));

        openApi.addSecurityItem(new SecurityRequirement().addList("oidc"));
    };
}
```

Critical:

1. Flattened runtime JSON and schema must match.
2. If schema keeps `data` but runtime flattens fields, generated clients will break.

## 12.10 Prefer header middleware in Java

```java
@Component
public final class PreferHeaderFilter extends OncePerRequestFilter {
    public static final String MINIMAL_ATTR = "hateoas.minimal";

    @Override
    protected void doFilterInternal(HttpServletRequest req, HttpServletResponse res, FilterChain chain)
        throws ServletException, IOException {
        boolean minimal = false;
        String prefer = req.getHeader("Prefer");
        if (prefer != null) {
            for (String token : prefer.split(",")) {
                String t = token.trim();
                if (t.equalsIgnoreCase("return=minimal")) {
                    minimal = true;
                    break;
                }
            }
        }

        req.setAttribute(MINIMAL_ATTR, minimal);
        if (minimal) res.setHeader("Preference-Applied", "return=minimal");
        chain.doFilter(req, res);
    }
}
```

Assembler logic must short-circuit link generation when minimal is true.

---

## 13. Minimal end-to-end flow to replicate (sequence)

1. HTTP request enters API.
2. CQRS request dispatched.
3. Authorization pipeline checks request contracts/annotation.
4. Pipeline calls Cerbos batch/single through Cerbos provider.
5. If Cerbos error:
   - safe mode activates.
   - deny decision returned.
6. Handler executes only if authorized.
7. Controller builds HAL response using resource assembler.
8. Assembler computes link definitions.
9. Evaluator runs static checks then one Cerbos batch call for all permission-bound links.
10. Links are filtered by decisions.
11. OpenAPI document exposes matching HAL structure for generated clients.

---

## 14. What to intentionally not port from current .NET

Because your target is strict Cerbos-only:

1. Do not port `FallbackAuthorizationService` authorization decisions as a runtime fallback path.
2. Do not port instance setting `authorization.provider` switching.
3. Do not keep open fallback mode for Cerbos outage.

You can still keep:

1. parity tests against Cerbos policy resources/actions.
2. safe-mode gate and observability.

---

## 15. Suggested Java test matrix (must-have)

### Unit tests

1. Authorization pipeline:
   - annotation path, interface path, secure request dynamic id path.
2. Cerbos provider:
   - missing user id -> deny.
   - missing action decision -> deny.
   - transport failure -> safe mode activation + deny.
3. HATEOAS evaluator:
   - static checks short-circuit.
   - batch index remap correctness.
   - exception path fail-closed.

### Integration tests

1. `GET` list anonymous -> no auth-required create/edit/delete links.
2. authenticated list -> expected create link appears, permission-bound links filtered by decision.
3. `Prefer: return=minimal` -> item/root links removed as designed, metadata retained.
4. Cerbos outage simulation -> API enters safe mode and denies protected operations.
5. Cerbos recovery simulation -> exits safe mode only after probe threshold.
6. OpenAPI snapshot test:
   - HAL resources include `_links`/`_embedded`.
   - flattened DTO fields present.
   - no duplicate conflicting schemas for nested lists.

---

## 16. Direct mapping table (.NET -> Java)

1. `AuthorizationBehavior<TReq,TRes>` -> CQRS interceptor/behavior in command bus.
2. `[AuthorizeResource]` -> `@AuthorizeResource`.
3. `ISecureRequest` -> `SecureRequest` interface.
4. `IAuthorizationProvider` -> `AuthorizationProvider`.
5. `CerbosAuthorizationService` -> same name/service in Java.
6. `HateoasAuthorizationEvaluator` -> same component in Java.
7. `ResourceAssemblerBase` batch flatten/remap -> same algorithm in Java assembler base.
8. `PreferHeaderMiddleware` -> servlet filter.
9. `HalSchemaFilter` + `HalDtoSchemaTransformer` -> springdoc model converter/customizers.
10. `AuthorizationParityTests` -> architecture tests validating resource/action/policy parity.

---

## 17. Final implementation note

If you build only endpoint authorization and skip HATEOAS/OpenAPI parity, your Java API will be functionally secure but operationally incomplete:

1. client navigation will drift from permissions.
2. generated API clients will not correctly represent HAL payloads.
3. get-all endpoints will degrade under per-item permission calls.

The critical “hard-mode” pieces are:

1. batch permission remap algorithm for list links.
2. strict safe-mode state machine with recovery strategy.
3. OpenAPI HAL schema customizations matching runtime JSON shape.
