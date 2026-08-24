<!-- ABOUTME: Domain journal for authentication, authorization, multi-tenancy, and security trust boundaries. -->
<!-- ABOUTME: Captures durable findings on Cerbos policies, Keycloak, BFF tokens, and tenant isolation. -->

# Auth & Multi-Tenancy Knowledge Ledger

> **Scope**: Authentication, Cerbos policies, Keycloak, BFF token forwarding, tenant resolution, and security boundaries.

---

## 1. Architectural Decisions

- **Single User ID Authority**: Derive user identity exclusively via `Explore.Application.Authentication.PlatformIdentityPrincipalExtensions` (`sub` $\rightarrow$ `nameidentifier` $\rightarrow$ `sid` $\rightarrow$ `internal_user_id`). Never parse raw claims or re-derive identity ad-hoc in controllers.
- **Central Tenant Isolation**: Enforce tenant isolation centrally via EF Core global query filters in `ExploreDbContext`. Do not bypass query filters without documented architectural review.
- **BFF Anti-Spoofing**: Trust tenant and user headers only from the verified BFF gateway; strip incoming client `X-Tenant-Slug` on public edge.
- **Write-Only Secrets in Dashboards**: Tenant provider secrets must be accepted only at write boundaries. Read DTOs, HAL responses, and metrics must remain redacted and expose only boolean configuration status or aggregate counts.

---

## 2. Technical Insights & Patterns

- **Consent Audit FKs Must Match Aggregate Roots**: When an operation creates a parent and child rows atomically (e.g. `EventRegistrationIntent` parent + multiple child `EventRegistration` rows), audit foreign keys (like `EventContactShareConsent`) must target the parent aggregate root (`SourceEventRegistrationIntentId`), not an arbitrary child row.
- **Cerbos Authorization Attributes Use Organization ID, Not Actor ID**: Cerbos `org_admin` derived roles check `resource.attr.organizationId` against the user's organization membership. Because `Actor.Id` and `Organization.Id` are distinct GUIDs, controllers/handlers must resolve `recipientActorId → Actor.OrganizationId` server-side before evaluating authorization.
- **`ISecureRequest.ResourceAttributes` Resolution Precedes Handler Execution**: `AuthorizationBehavior<TRequest, TResponse>` pulls `ResourceAttributes` synchronously before MediatR handler invocation. Any contextual lookup required for authorization must be resolved before `_mediator.Send`.
- **Event-Child Fallback Authorization Must Validate Resource Tenant**: Optimized batch fallback authorization must resolve event context from resource attributes and verify `resourceTenantId == profile.TenantId` before allowing access, failing closed on tenant mismatch.
- **Event-Scoped Operational Roles**: `EventRoleAssignment` serves as the persisted event-instance grant using canonical effective predicate `Status == Active && StartsAtUtc <= now && (ExpiresAtUtc IS NULL OR ExpiresAtUtc > now)`.

---

## 3. Failed Approaches & Lessons

- **Mocking `IUserContext` in Controller Tests**: Mocking `IUserContext` through the container prevents testing the real claim chain. Controller tests must set real claims on `ControllerContext.HttpContext.User`.
- **Client-Side Role Checks for UI Affordances**: Checking user roles/claims in Blazor components to gate actions is forbidden. Gating must rely solely on the presence of HAL `_links` returned by the server.
