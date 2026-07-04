<!-- ABOUTME: Implementation plan for the Event Instance Console and multi-tenant control-plane capability. -->
<!-- ABOUTME: Defines current evidence, architecture decisions, phases, validation, and future-agent contract. -->

# Event Instance Console And Multi-Tenant Control Plane - Implementation Plan

Last Updated: 2026-07-04 Europe/Brussels

## 0. Planning Metadata

- **Request:** Write and CTO-strengthen an implementation plan for the Event Instance Console and the multi-tenant control-plane capabilities that appear only when ISLAMU Event runs in multi-tenant mode. Single-tenant mode keeps the existing administration settings page as its current instance-console abstraction. The target must include a required shared BFF hosting security library, a shared control-plane Razor class library, and a self-hostable separate Blazor control-plane app, all named with the current `Event.*` namespace. The separate control-plane app must authenticate through Keycloak OIDC using the BFF pattern.
- **Task directory:** `dev/active/multi-tenant-control-plane/`
- **Planning status:** In implementation. Phase 1 shared BFF hosting foundation has been accepted for the current scope: proxy/header primitives, safe auth diagnostics, OIDC options, and token-refresh cookie events now live in `Event.Web.BffHosting`, and `Explore.Blazor` consumes them. Phase 2 shared control-plane client work has not started.
- **Matched intents:** No exact intent in `.claude/contract/intents.yaml` matched "multi-tenant control-plane implementation plan". Use the Fallback Contract from `AGENTS.md`, `docs/QUICK_REFERENCE.md`, and `docs/GOVERNANCE.md`. Related implementation intents likely to apply later: `external-infrastructure-bootstrap`, `add-get-endpoint`, `add-write-endpoint`, `add-cqrs-handler`, `add-ef-migration`, `update-repository-query`, `openapi-contract-change`, `blazor-component-affordance`, `bff-auth-bug`, `cerbos-policy-change`, and `ci-cd-change`.
- **Relevant skills:** `senior-cto-feedback`, `clean-architecture-rules`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `dotnet-efcore-guidelines`, `outbox-pattern`, `error-tracking`, `aspire`.
- **Relevant rules:** `.claude/rules/blazor-client.md`, `.claude/rules/blazor-server.md`, `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/application-layer.md`, `.claude/rules/domain.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/efcore-migrations.md`, `.claude/rules/tests.md`.
- **Primary layers touched:** Application, Persistence, Infrastructure, API, Blazor, Docs, DevOps. Domain may be touched only if tenant lifecycle, audit, or operation status entities are missing.
- **Estimated complexity:** XL. This crosses deployment-mode rules, BFF host routing, Keycloak OIDC confidential-client setup, shared ASP.NET Core/YARP/cookie/header security extraction, Blazor UI composition, API authorization/HAL contracts, tenant isolation, operations views, Docker/Aspire topology, and self-hosting documentation. The highest-risk foundation is extracting shared BFF hosting correctly before `Event.ControlPlane.Blazor` exists, then proving the existing `Explore.Blazor` host still behaves the same.

## 1. Executive Summary

Build one Event Instance Console capability with multi-tenant control-plane capabilities enabled only in multi-tenant mode. The implementation has three required new projects:

1. **`Event.Web.BffHosting`**: shared ASP.NET Core browser-BFF hosting library for Keycloak OIDC, cookies, YARP proxying, privileged header stripping, token forwarding, safe diagnostics, and health checks.
2. **`Event.ControlPlane.Client`**: host-neutral Razor class library for control-plane pages, components, route constants, and service contracts.
3. **`Event.ControlPlane.Blazor`**: separate self-hostable Blazor/BFF control-plane app that consumes both shared libraries.

The control-plane capability still has multiple deployment shapes:

1. **Embedded Instance Console** in the existing `Explore.Blazor` / `Explore.Blazor.Client` experience. Single-tenant deployments keep the existing settings page; multi-tenant deployments add the tenant/platform control-plane sections for instance administrators.
2. **Dedicated control-plane hostname** using the same existing Blazor/BFF image, with host-based shell separation for operators who want `admin.example.org`.
3. **Separate self-hostable Control Plane Blazor/BFF app** named `Event.ControlPlane.Blazor` that reuses `Event.ControlPlane.Client` and can be deployed as its own Docker image/profile.

CTO naming correction: new control-plane projects must use `Event.*` names because `Explore` is the older project prefix. Existing checked-in projects such as `Explore.Blazor`, `Explore.API`, and `Explore.AppHost` remain as current codebase reality until a broader repository rename is planned separately.

The separate control-plane app must authenticate operators through Keycloak OIDC as a confidential BFF client. Browser code receives only HttpOnly cookies; bearer/refresh tokens, Keycloak client secrets, setup secrets, tenant hints, and support-access authority stay server-side. The target is a strong, explicit security boundary, not a second browser-token app.

The first implementation should extract `Event.Web.BffHosting` from the existing `Explore.Blazor` host and make `Explore.Blazor` consume it before adding the separate app. This proves the security-sensitive BFF boundary once, then lets `Event.ControlPlane.Blazor` use the same OIDC/YARP/cookie/header machinery instead of reimplementing it.

Single-tenant tenant/fleet/platform controls are explicitly out of scope. In single-tenant mode, the existing single administration settings page remains the intended instance-console abstraction for the instance administrator. The plan must not add a casual single-tenant to multi-tenant toggle. Any future mode conversion must be a deliberate migration wizard/runbook with backups, DNS/reverse-proxy checks, isolation preflight, maintenance mode, execution, and post-migration verification.

Future Blazor/native app projects are intentionally out of scope for this workstream. The shared BFF hosting boundary should be maintainable and extensible enough for future browser hosts, but this plan must not add or enumerate those future applications as implementation tasks.

Also out of scope for the first implementation: a fully independent management API with reserved database/API resources. A separate Blazor UI improves operator ergonomics and security separation, but it does not guarantee rescue access if the shared API or database is saturated. A true management plane with reserved resources can follow after the shared control-plane model is stable.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Deployment mode is already modeled as configuration and persisted after onboarding. | Verified: `Explore.Infrastructure/DeploymentSettings.cs`, `Explore.Infrastructure/Services/DeploymentModeProvider.cs`, `Explore.Application/Contracts/Services/IDeploymentModeProvider.cs`. | High | `Deployment:Mode` / `DEPLOYMENT_MODE` controls first-run onboarding mode; persisted mode is authoritative after onboarding. |
| Single-tenant onboarding is the convention-first path when `DEPLOYMENT_MODE` is absent. | Verified: `docs/DEPLOYMENT_MODES.md`, `docs/CONFIGURATION.md`, `Explore.Infrastructure/Services/DeploymentModeProvider.cs`. | High | Pre-onboarding fallback resolves single tenant so setup can run without platform complexity. |
| Multi-tenant onboarding appears when `DEPLOYMENT_MODE=multi_tenant`. | Verified: `docs/DEPLOYMENT_MODES.md`, `Explore.Blazor.Client/Routing/Guards/MultiTenantOnboardingRouteGuard.cs`, `Explore.Application/Features/InstanceOnboarding/Commands/CompleteInstanceOnboardingCommandHandler.cs` by source/search. | High | The guard allows the multi-tenant onboarding route when configured or required. |
| Runtime mode switching is intentionally not a normal admin setting. | Verified: `docs/DEPLOYMENT_MODES.md`, `docs/CONFIGURATION.md`. | High | Docs say mode changes require an explicit operator migration/runbook. |
| API tenant resolution is API-authoritative and fails closed in multi-tenant mode. | Verified: `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`, `Explore.API/Middleware/ApiTenantPostAuthenticationMiddleware.cs`, `docs/MULTI_TENANCY.md`. | High | Resolution order is trusted BFF header, custom domain, subdomain; unresolved multi-tenant API requests return 404. |
| Single-tenant endpoints can hide platform/admin concepts. | Verified: `Explore.API/Filters/BlockInSingleTenantAttribute.cs`, `docs/DEPLOYMENT_MODES.md`. | High | `BlockInSingleTenant` returns 404; `RequireMultiTenant` returns 403. |
| Existing Blazor app is the BFF host and proxies API calls through YARP. | Verified: `Explore.Blazor/Extensions/YarpProxyExtensions.cs`, `Explore.Blazor/Services/TenantHeaderForwardingHandler.cs`, `docs/BLAZOR.md`, `docs/ARCHITECTURE.md`. | High | Browser tokens are not exposed; BFF forwards server-held tokens and trusted tenant headers. |
| Keycloak OIDC is the established BFF authentication model. | Verified by search/read: `Explore.Blazor/Extensions/AuthenticationExtensions.cs`, `Explore.Blazor/Services/DynamicAuthSchemeManager.cs`, `Explore.Blazor/Services/TokenRefreshCookieEvents.cs`, `docs/CONFIGURATION.md`, `docs/BLAZOR.md`, `docs/SECURITY-MODEL.md`, `docker/keycloak/realm-export.json`, `docker/keycloak/keycloak-init.sh`. | High | Separate control-plane app should extend this model with its own confidential Keycloak client instead of introducing a different auth path. |
| `Event.Web.BffHosting` now exists as the accepted shared BFF hosting foundation. | Verified: `Event.Web.BffHosting/Event.Web.BffHosting.csproj`, `Event.Web.BffHosting/Proxy/EventApiProxyExtensions.cs`, `Event.Web.BffHosting/Security/BffProxyHeaderSanitizer.cs`, `Event.Web.BffHosting/Authentication/EventBffOidcOptionsFactory.cs`, `Event.Web.BffHosting/Authentication/EventBffTokenRefreshCookieEvents.cs`, `Event.Web.BffHosting/Authentication/SafeAuthDiagnosticsPolicy.cs`, `Explore.Blazor/Extensions/YarpProxyExtensions.cs`, `Explore.Blazor/Extensions/AuthenticationExtensions.cs`, `Explore.Blazor/Services/DynamicAuthSchemeManager.cs`, `Explore.Blazor/Services/EventBffHostingAdapters.cs`, `Explore.Blazor/Services/ExploreBffCookieSessionHandler.cs`, `Event.Architecture.Tests/EventWebBffHostingArchitectureTests.cs`; builds and focused tests passed on 2026-07-04. | High | The accepted Phase 1 slice covers YARP proxy registration, API base resolution, development TLS trust policy, privileged-header stripping, token/tenant/setup/support adapters, reusable OIDC options, safe remote-failure diagnostics, shared token-refresh cookie events, and `Explore.Blazor` consumption. |
| Existing instance settings UI already exists in the Blazor client. | Verified by file structure: `Explore.Blazor.Client/Pages/Admin/Instance/InstanceAdminSettings.razor` and `Explore.Blazor.Client/Pages/Admin/Instance/Components/*`. | High | The current page has settings sections such as tenants, SMTP, storage, auth, localization, modules, and governance. |
| Current admin routes include `/admin/instance/settings`. | Verified: `Explore.Blazor.Client/Routes.razor`, `docs/ADMIN_GUIDE.md`. | High | This route should remain the single-tenant administration abstraction. |
| HAL links are the source of truth for UI action affordances. | Verified: `docs/QUICK_REFERENCE.md`, `docs/BLAZOR.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/blazor-client.md`. | High | Control-plane resource actions must not be enabled from local role checks alone. |
| No control-plane-specific shared class library or separate control-plane Blazor app currently exists. | Verified by search: no `ControlPlane`, `Control.Plane`, `control-plane`, `Event.ControlPlane.Blazor`, or `Explore.Control*` project found except unrelated docs references. | High | New projects/files must be treated as new work. |
| Related active work exists for secrets/control-plane separation. | Verified: `dev/active/secrets-refactor-control-plane/*`. | Medium | That workstream is about secret binding separation, not this UI/control-plane product surface. Coordinate but do not merge plans. |
| Related paused work exists for tenant onboarding/lifecycle. | Verified: `dev/pause/tenant-onboarding-enterprise/*`. | Medium | Tenant lifecycle tasks may provide prerequisites or overlap. Re-baseline before implementation because the worktree has many unrelated changes. |
| Baseline build was green before this plan was written. | Verified by command: `dotnet build --configuration Release --verbosity quiet`. | High | Build passed with 25 projects, 0 errors, and existing warnings, including package warnings. |

### 2.2 Existing Implementation

**Deployment mode and onboarding**

- `DeploymentSettings` models `Mode`, `DefaultTenantId`, `HidePlatformAdminInSingleTenant`, and `DefaultTenantSubdomain`.
- `DeploymentModeProvider` resolves configured onboarding mode from `Deployment:Mode` / `DEPLOYMENT_MODE`, then uses persisted onboarding state as the runtime authority after setup.
- `CompleteInstanceOnboardingCommandHandler` persists the configured onboarding mode when instance setup completes.
- `MultiTenantOnboardingRouteGuard` protects the multi-tenant onboarding route in the Blazor client.

**Tenant resolution and isolation**

- `ApiTenantResolutionMiddleware` resolves tenants for API traffic and intentionally fails closed in unresolved multi-tenant requests.
- `ApiTenantPostAuthenticationMiddleware` reconciles API key tenant binding after authentication. Instance-admin API keys can operate without a tenant binding where permitted.
- `docs/MULTI_TENANCY.md` describes the authoritative resolver order and EF Core global filters.

**Blazor/BFF**

- `Explore.Blazor` is the BFF host and maps `/api/{**catchall}` through YARP.
- `YarpProxyExtensions` removes browser-controlled privileged headers and forwards server-held access tokens and trusted tenant hints.
- `TenantHeaderForwardingHandler` adds `X-Tenant-Slug` only from BFF route context.
- `Explore.Blazor.Client` owns the current UI pages and services.
- `Event.Web.BffHosting` now owns the shared `/api/*` YARP route/cluster setup, proxy request transform, privileged-header sanitizer, provider-neutral OIDC options factory, safe auth diagnostics policy, token-refresh cookie events, and named token-refresh backchannel client. `Explore.Blazor/Extensions/YarpProxyExtensions.cs` registers host-specific proxy adapters and delegates to `AddEventApiProxy(...)`.
- `Explore.Blazor` still owns host-specific dynamic auth orchestration through `DynamicAuthSchemeManager`, but reusable OIDC option construction now comes from `IEventBffOidcOptionsFactory`. Host-specific principal enrichment, circuit token cleanup, auth cookie cleanup, and setup-aware session-expiry redirects live in `ExploreBffCookieSessionHandler`, not in the shared library.

**Existing administration UI**

- `Explore.Blazor.Client/Pages/Admin/Instance/InstanceAdminSettings.razor` is the existing instance administration page.
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/*` contains settings sections for tenants, domains, SMTP, storage, auth providers, localization, modules, footer governance, and other instance-level settings.
- `docs/ADMIN_GUIDE.md` documents `/admin/instance/settings`.

**Authorization and action affordances**

- Instance and tenant authority are separated in `docs/ADMIN_HIERARCHY.md`.
- HAL action links must drive UI affordances for resource operations.
- API controllers should stay thin; CQRS handlers own business flow; repositories return entities and handlers map to DTOs.

### 2.3 Existing Tests And Verification Coverage

Verified test projects and likely coverage areas:

- `Event.Architecture.Tests`: context/rule/intent shape, architecture boundaries, ABOUTME enforcement where configured.
- `Event.Application.UnitTests`: CQRS handlers, authorization metadata, settings behavior, onboarding command behavior.
- `Event.Persistence.IntegrationTests`: EF Core repositories, migrations, query filters, seeded lookup data.
- `Event.API.IntegrationTests`: API endpoint behavior, middleware, auth, HAL link contracts, onboarding, tenant controller behavior.
- `Explore.Blazor.IntegrationTests`: BFF host and handler behavior.
- `Explore.Blazor.Client.Tests`: bUnit tests for route guards, pages, components, HAL gating.
- `Explore.Blazor.Client.E2ETests`: browser-level flows where enabled.
- `Explore.Infrastructure.Tests`: infrastructure/provider behavior.

Known planning gap: no tests can currently protect a separate control-plane Blazor/BFF app or shared control-plane class library because those projects do not yet exist.

### 2.4 Existing Documentation And Contracts

Relevant docs and contracts already read:

- `docs/DEPLOYMENT_MODES.md`: single vs multi-tenant mode, first-run behavior, mode migration constraints.
- `docs/MULTI_TENANCY.md`: tenant resolver order, fail-closed behavior, filters, settings hierarchy.
- `docs/ADMIN_HIERARCHY.md`: instance vs tenant administrator responsibilities.
- `docs/ADMIN_GUIDE.md`: existing instance and tenant admin routes.
- `docs/ARCHITECTURE.md`: Clean Architecture, CQRS, BFF, middleware order, outbox.
- `docs/BLAZOR.md`: Blazor project boundaries, BFF token/header forwarding, generated client pattern, HAL UI rules.
- `docs/API.md`: API controller conventions, rate limits, API key model, admin endpoint examples.
- `docs/SECURITY-MODEL.md`: BFF OIDC/JWT model, setup secret/header hardening, authorization layers.
- `docs/AUTHORIZATION.md`: HAL authorization evaluator, Cerbos/local provider model.
- `docs/CONFIGURATION.md`: static config vs runtime settings, deployment-mode configuration, reverse proxy trust.
- `docs/SELF_HOSTING.md`: Docker Compose topology, `DEPLOYMENT_MODE`, reverse-proxy expectations.
- `docs/SECRETS.md`: provider and ownership model for secret configuration.
- `docs/CODEBASE_STRUCTURE.md`: current project structure and admin paths.
- `docs/ACCESSIBILITY.md`, `docs/DESIGN_SYSTEM.md`, `docs/OUTBOX_PATTERN.md`: UI, design, and async-operation constraints.

OpenAPI contract impact is likely if new endpoints are added, requiring `schemas/openapi.json` and `docs/API_CHANGELOG.md` updates through the repo's established workflow.

### 2.5 Current Pain Points / Improvement Areas

- **The shared BFF library is accepted for the current Phase 1 scope.** `Event.Web.BffHosting` now centralizes YARP route/cluster setup, API base-address resolution, development certificate trust policy, browser-controlled privileged-header stripping, host-provided token/tenant/setup/support forwarding adapters, reusable Keycloak/Google OIDC options, safe auth diagnostics, and token-refresh cookie events. Host-specific dynamic scheme orchestration remains in `Explore.Blazor`.
- **No shared control-plane UI library exists.** The current instance admin components live inside `Explore.Blazor.Client`, so a separate self-hostable control-plane app would either duplicate UI/client code or require extraction.
- **No separate control-plane Blazor/BFF app exists.** The repository has only the main `Explore.Blazor` host today, so `Event.ControlPlane.Blazor` and its Keycloak OIDC client/bootstrap path are new work.
- **The existing instance settings page mixes concerns that are acceptable for single-tenant administration but too flat for a multi-tenant operator console.** Multi-tenant operators need operational pages for tenants, DNS, health, jobs/outbox, storage, backups, and policy/provider status.
- **Dedicated admin host routing is not yet modeled as a first-class UI shell.** The BFF already understands host and tenant headers, but there is no verified control-plane host shell that hides public discovery and enforces instance-admin-only navigation.
- **Multi-tenant onboarding needs operator-facing DNS and control-plane access guidance.** Existing deployment docs describe the behavior, but onboarding should make public host, wildcard tenant subdomain, admin host, and custom-domain strategy visible.
- **A separate UI alone is not a true rescue plane.** If API, database, or reverse proxy capacity is saturated, a separate Blazor host may load but still fail to execute operations. This must be documented and deferred to a later management API/worker design if needed.
- **Worktree state is already heavily dirty.** Future implementation agents must re-baseline before editing and avoid reverting unrelated work.

### 2.6 Unknowns After Investigation

- **Exact profile/config boundary for later `Event.Web.BffHosting` expansion.** The current BFF foundation is accepted, but the separate control-plane host still needs explicit `ControlPlane` profile defaults for cookie names/domains, stricter CSP, optional IP allowlist, Keycloak client config, and readiness behavior. The library must remain a hosting/security helper, not a UI framework, generated client package, or business service library.
- **Exact Blazor project shape for `Event.ControlPlane.Client`.** Implementation must verify the best .NET 10 Razor class library shape for InteractiveAuto components and static assets. The library must not create a dependency cycle with `Explore.Blazor.Client`.
- **Exact Keycloak client configuration shape for the separate app.** Existing docs map `Keycloak:*` and Compose syncs the `islamu-event-blazor` client secret. Implementation must decide whether `Event.ControlPlane.Blazor` uses the same config section with a dedicated client id/secret or a separate `ControlPlane:Keycloak:*` section, then document/env-map it.
- **Whether tenant lifecycle/domain/operation endpoints already exist in the current dirty worktree.** The planning pass verified concepts and related docs, but future agents must re-read current code before implementing those endpoints.
- **Exact instance-admin authority source.** Implementation must verify current Keycloak claim/group mapping, local/Cerbos authorization metadata, and API policy names before wiring route guards and endpoint policies.
- **Backups and migration readiness data sources.** Docs mention operational needs, but the current verified codebase may not have backup status or migration preflight services yet.
- **Dedicated admin hostname security defaults.** The final implementation must decide cookie names/domains, CSP, optional IP allowlist config, and reverse-proxy examples based on the current hosting model.

## 3. Proposed Future State

### 3.1 Product Model

The product surface is the **Event Instance Console**. Single-tenant deployments already have the current instance administration settings page as their instance-console abstraction. Multi-tenant deployments add the **Control Plane** feature set inside that console for instance owners:

- tenant provisioning, suspension, archiving, purging, and lifecycle visibility;
- platform routing, DNS guidance, custom-domain status, and control-plane host configuration;
- deployment health, version, warnings, storage usage, queue/outbox status, failed jobs, and provider status;
- security provider status, authorization provider status, setup secret state, support-access state, audit events, and policy locks;
- operational tasks such as maintenance mode, cache clear, dead-letter review, backup readiness, and migration readiness.

In single-tenant mode, users continue to see the existing single administration settings page. Tenant provisioning, tenant lifecycle, wildcard tenant DNS, quotas, and cross-tenant operations remain hidden unless an explicit future migration wizard is approved.

### 3.2 Deployment Shapes

```text
Shape A: Embedded Instance Console
Browser -> Explore.Blazor BFF -> Event.Web.BffHosting -> Explore.API -> Application -> Persistence
Routes: /admin/instance/*

Shape B: Dedicated admin hostname, same app image
Operator -> admin.example.org -> Explore.Blazor BFF -> Event.Web.BffHosting -> Explore.API -> Application -> Persistence
Host shell: control-plane only

Shape C: Separate self-hostable app
Operator -> Event.ControlPlane.Blazor BFF -> Event.Web.BffHosting -> Explore.API -> Application -> Persistence
Shared UI: Event.ControlPlane.Client
Auth: Keycloak OIDC confidential client -> HttpOnly control-plane cookie -> server-side token forwarding
```

The BFF hosting library must be proven through the existing `Explore.Blazor` host before Shape C is added. Shape A should be the first control-plane UI integration because it proves the shared UI library against the existing host. Shape C is still part of the target plan, not a separate product. It should consume the same BFF hosting library, shared UI library, and API authorization/HAL contracts.

### 3.3 Proposed Project Structure

```text
Event.Web.BffHosting                    new ASP.NET Core BFF hosting library
  Authentication/
  Cookies/
  Proxy/
  Security/
  Diagnostics/
  Extensions/

Event.ControlPlane.Client                 new Razor class library
  Pages/
  Components/
  Contracts/
  Services/
  Routing/
  Styles/
  _Imports.razor

Explore.Blazor.Client                       existing embedded app
  references Event.ControlPlane.Client
  maps control-plane routes only for multi-tenant instance admins

Explore.Blazor                              existing BFF host
  references Event.Web.BffHosting
  keeps public/community web host responsibilities

Event.ControlPlane.Blazor                 new self-hostable BFF host
  references Event.Web.BffHosting
  references Event.ControlPlane.Client
  uses shared Keycloak OIDC/BFF/YARP/API proxy services
  uses a dedicated confidential OIDC client such as islamu-event-control-plane
  ships as its own Docker image/profile
```

`Event.Web.BffHosting` must be a server-side hosting helper library:

- owns ASP.NET Core/Keycloak OIDC setup, cookie policy, token refresh events, YARP API proxy setup, server-side access-token forwarding, privileged-header stripping, trusted tenant-hint forwarding, safe auth diagnostics, options validation, and BFF health checks;
- exposes profile-based host APIs such as `AddEventBffHosting(configuration, EventBffHostProfile.PublicWeb)` and `AddEventBffHosting(configuration, EventBffHostProfile.ControlPlane)`;
- supports at least `PublicWeb` and `ControlPlane` profiles in this workstream;
- does not contain pages, MudBlazor layout components, control-plane components, organizer/public UI, generated NSwag client types, Application handlers, Domain entities, Persistence repositories, Keycloak realm provisioning scripts, Docker Compose definitions, or tenant lifecycle business logic;
- remains generic enough for future browser hosts without adding any future app projects to this workstream.

`Event.ControlPlane.Client` must be host-neutral:

- no dependency on `Explore.Blazor.Client`;
- no dependency on API, Infrastructure, Persistence, or Domain projects;
- no token storage or local authorization decisions;
- service abstractions for control-plane pages, with concrete adapters registered by each host;
- HAL links drive resource action buttons.

`Event.ControlPlane.Blazor` must be security-opinionated:

- Keycloak is the only planned operator authentication provider for the separate control-plane app in this workstream.
- Use Authorization Code flow with PKCE through ASP.NET Core OpenIdConnect middleware, a confidential client secret stored only in server-side configuration/secret provider, and HttpOnly secure cookies.
- Use a dedicated Keycloak client id such as `islamu-event-control-plane`, not the public API audience and not a browser-exposed secret.
- Configure explicit redirect and sign-out callback URIs for the control-plane host, for example `/signin-oidc` and `/signout-callback-oidc`.
- Require instance-admin authority after authentication; tenant-admin authority alone must not enter the control-plane shell.
- Reuse safe auth diagnostics, OIDC discovery readiness, token refresh, cookie, and backchannel hardening patterns from the existing BFF where possible.
- Use `Event.Web.BffHosting` instead of reimplementing OIDC/YARP/cookie/header behavior locally in the separate app.

If existing app-level design wrappers such as `AppButton`, `AppCard`, or dialog shell components are needed, implementation should either move the reusable primitives into a neutral UI shared library or expose host-provided wrappers without creating a dependency cycle. Do not copy-paste the design system into a second app.

### 3.4 Route Model

Embedded multi-tenant routes:

```text
/admin/instance
/admin/instance/tenants
/admin/instance/domains
/admin/instance/onboarding
/admin/instance/health
/admin/instance/usage
/admin/instance/storage
/admin/instance/jobs
/admin/instance/security
/admin/instance/policies
/admin/instance/backups
/admin/instance/settings
```

Dedicated-host routes can use the same route set, with the shell treating `/` as the control-plane overview when the request host is an admin host.

Single-tenant mode:

- keep the existing single administration settings page abstraction;
- do not show multi-tenant control-plane navigation;
- block or hide multi-tenant-only routes through server-side/API checks and Blazor route guards;
- do not add a normal settings toggle for deployment mode.

### 3.5 Data And Control Flow

```text
Operator clicks a control-plane action
  -> Blazor component checks HAL _links for that action
  -> Control-plane service calls BFF/API through server-configured HttpClient/YARP path
  -> API endpoint enforces instance-admin authority and multi-tenant-only constraints
  -> MediatR command/query executes Application logic
  -> Repositories return entities; handlers map to DTO/HAL resources
  -> Destructive or long-running operations create audited jobs/outbox entries where appropriate
  -> UI refreshes resource state and action links from the server response
```

## 4. Non-Negotiable Constraints

- Repositories return entities, never DTOs.
- Validators are manually instantiated where the repo pattern requires it.
- Use `int` for lookups, `Guid` UUIDv7 for aggregates, and `long` for cursors.
- GET endpoints are `[AllowAnonymous]` only where public; write endpoints are `[Authorize]`. Control-plane endpoints must require instance-admin authority even if some repo-wide docs generalize GET behavior.
- UI action affordances are gated by HAL `_links`, not local role, claim, or route checks.
- Tenant isolation is API-authoritative. The browser and client-side components are never the tenant authority.
- BFF tokens stay server-side. Browser code must not receive access tokens or privileged tenant/setup/support headers.
- `Event.ControlPlane.Blazor` must authenticate with Keycloak OIDC as a confidential BFF client; it must not use API keys, setup secrets, or browser-stored bearer tokens for operator login.
- Browser-supplied privileged headers must be stripped by the BFF/proxy.
- `Event.Web.BffHosting` is required before `Event.ControlPlane.Blazor` is scaffolded. It owns security-sensitive browser-hosting primitives only and must not absorb UI, business, generated-client, domain, persistence, or provisioning-script responsibilities.
- Control-plane routes and endpoints are multi-tenant-only unless explicitly needed for the existing single-tenant settings page.
- Single-tenant mode must keep the existing administration abstraction and must not expose tenant SaaS concepts by default.
- No casual runtime single-tenant to multi-tenant toggle. Mode conversion requires a future explicit migration workflow.
- No Clean Architecture dependency violations. Shared Blazor/control-plane libraries must not reference inward-forbidden projects.
- Every new file starts with two `ABOUTME:` lines where project rules require them.
- No compatibility shims unless explicitly approved. Preserve existing documented routes where they remain product-correct; do not create artificial legacy paths.
- New control-plane/BFF projects use the current `Event.*` prefix. Do not create new `Explore.ControlPlane.*` projects.

## 5. Architecture And Design Decisions

### Decision 1: One Control-Plane Capability, Multiple Shells

- **Decision:** Build one Event Instance Console model with one shared control-plane API/UI capability consumed by embedded, dedicated-host, and separate-app shells. Multi-tenant-only features appear only when deployment mode is multi-tenant.
- **Why:** Prevents duplicated auth, generated clients, layouts, admin components, security decisions, and documentation.
- **Alternatives considered:** Start with a completely separate Blazor app. Rejected for v1 because it would force duplication before the feature shape is proven.
- **Consequences:** The shared library and API contracts must be designed before the separate app is scaffolded.
- **Files/layers affected:** Blazor client, BFF host(s), API, Application, Docs, DevOps.

### Decision 2: Instance Console Exists In Both Modes, Multi-Tenant Control-Plane Features Do Not

- **Decision:** Treat the existing single-tenant administration settings page as the current single-tenant Instance Console, while adding tenant/platform control-plane features only for multi-tenant deployments.
- **Why:** This avoids a future terminology refactor while preserving the user's original requirement that single-tenant administrators are not burdened with tenant SaaS controls.
- **Alternatives considered:** Call the whole product "multi-tenant control plane" only. Rejected because operational instance administration exists in both deployment modes. One full dashboard for both modes is also rejected because tenant lifecycle, wildcard DNS, quotas, and cross-tenant operations belong only to multi-tenant mode.
- **Consequences:** Route guards, API filters, navigation, onboarding, docs, and tests must assert single-tenant suppression for multi-tenant capabilities while preserving the existing single-tenant settings page.
- **Files/layers affected:** `Explore.Blazor.Client`, `Event.ControlPlane.Client`, API filters/controllers, tests, docs.

### Decision 3: Shared BFF Hosting Library Is Required First

- **Decision:** Create `Event.Web.BffHosting` before `Event.ControlPlane.Blazor`, and make `Explore.Blazor` consume it first.
- **Why:** Once public web and separate control-plane hosts both exist, duplicated Keycloak OIDC, cookie, token-forwarding, YARP, and privileged-header logic becomes a security consistency risk. The extra project is justified as a security boundary, not as speculative architecture.
- **Alternatives considered:** Keep BFF hosting logic duplicated and rely on tests; create `Event.Blazor.BffHosting`. Rejected because duplication invites drift, and the BFF boundary is ASP.NET Core/browser-hosting oriented rather than strictly Blazor-specific. `Event.Blazor.BffHosting` remains an acceptable fallback name only if implementation proves the library is materially Blazor-specific and the decision is recorded before scaffolding.
- **Consequences:** Phase 1 extracts hosting primitives from `Explore.Blazor`, architecture tests must enforce the dependency boundary, and BFF security integration tests must run against `Explore.Blazor` before the separate control-plane app is added.
- **Files/layers affected:** New `Event.Web.BffHosting`, `Explore.Blazor`, `Explore.Blazor.IntegrationTests`, docs/configuration.

### Decision 4: Shared Razor Class Library

- **Decision:** Create `Event.ControlPlane.Client` as a host-neutral Razor class library for control-plane pages/components/services/contracts.
- **Why:** This directly satisfies the requirement that both the separate Blazor app and embedded control plane use the same implementation.
- **Alternatives considered:** Put components in `Explore.Blazor.Client` and copy them later. Rejected because it blocks the self-hostable separate app and invites divergent behavior.
- **Consequences:** The library must not depend on `Explore.Blazor.Client`; shared UI primitives or service adapters may need extraction.
- **Files/layers affected:** New project, solution file, build/test configuration, Blazor hosts.

### Decision 5: Separate Self-Hostable App Uses Keycloak OIDC BFF Pattern

- **Decision:** Create `Event.ControlPlane.Blazor` as a BFF host authenticated through Keycloak OIDC using a dedicated confidential control-plane client and the shared `Event.Web.BffHosting` library.
- **Why:** Existing architecture requires tokens and privileged headers to stay server-side, and Keycloak is the platform identity provider. A separate app that weakens this boundary would be worse than no separate app.
- **Alternatives considered:** Standalone WebAssembly app calling API directly, shared setup-secret login, or API-key-backed operator login. Rejected because they bypass or weaken the established Keycloak/BFF trust boundary.
- **Consequences:** Docker Compose, Keycloak realm export/init scripts, Aspire AppHost, `.env.example`, and self-hosting docs must include the dedicated control-plane OIDC client and secret handling. The separate app must not reimplement BFF security primitives outside `Event.Web.BffHosting`.
- **Files/layers affected:** New BFF host, Docker Compose, Aspire AppHost, configuration docs, integration tests.

### Decision 6: Dedicated Hostname Is Host-Based Shell Separation

- **Decision:** `admin.example.org` should use host classification to show the control-plane shell and hide public/tenant discovery.
- **Why:** It gives multi-tenant operators a professional and more securable operator surface without requiring a separate app.
- **Alternatives considered:** Route-only `/admin/instance` in all deployments. Kept as the simple default, but insufficient for advanced operators.
- **Consequences:** Reverse-proxy docs, forwarded host trust, cookie settings, CSP, and optional IP allowlist settings must be designed.
- **Files/layers affected:** Blazor BFF, Blazor client routing/shell, configuration, docs, integration tests.

### Decision 7: HAL Controls Resource Actions

- **Decision:** Control-plane UI may use route guards to protect pages, but action buttons for tenant/domain/job resources must come from HAL `_links`.
- **Why:** Project rules make server-emitted HATEOAS links the UI authority for actions.
- **Alternatives considered:** Enable/disable buttons from local role claims. Rejected by project invariant.
- **Consequences:** New API resources need link assemblers/policies and bUnit tests must assert links drive affordances.
- **Files/layers affected:** API HATEOAS policies, Application authorization metadata, Blazor components, tests.

### Decision 8: Destructive Operations Are Audited And Prefer Async Execution

- **Decision:** Tenant purge, data deletion, backup/restore, dead-letter replay, and similar operations require strong confirmation, audit trails, and where possible background/outbox jobs.
- **Why:** Control-plane actions can affect all tenants and infrastructure-level data.
- **Alternatives considered:** Direct request-time deletion. Rejected for high-risk operations because it is brittle and hard to retry/audit.
- **Consequences:** Some operations may require new domain/application records for operation status, outbox entries, idempotency keys, and audit events.
- **Files/layers affected:** Domain, Application, Persistence, API, Operations docs, tests.

### Decision 9: New Projects Use `Event.*`, Existing Projects Stay As Verified

- **Decision:** The planned new projects for this workstream are `Event.Web.BffHosting`, `Event.ControlPlane.Client`, and `Event.ControlPlane.Blazor`.
- **Why:** `Explore` is the older project prefix. Creating new `Explore.ControlPlane.*` projects would bake the old name into new architecture.
- **Alternatives considered:** Continue `Explore.*` for consistency with existing projects; use `Event.Blazor.BffHosting` for the BFF library. Rejected because the user explicitly corrected the naming direction, and `Event.Web.BffHosting` better describes the ASP.NET Core/browser-BFF boundary while preserving future compatibility without listing future apps in this plan.
- **Consequences:** Implementation agents must be careful not to rename existing projects opportunistically. Existing `Explore.*` projects remain verified dependencies unless a separate repository-wide rename workstream is approved.
- **Files/layers affected:** New project files, solution file, docs, architecture tests, Docker/Aspire configuration.

## 6. Implementation Phases

### Phase 0: Plan Review And Implementation Re-Baseline

- **Goal:** Confirm scope, re-check the dirty worktree, and avoid stale assumptions before code changes.
- **Depends on:** User review of this plan.
- **Relevant files:** `dev/active/multi-tenant-control-plane/*`, `.claude/contract/intents.yaml`, current git status, current solution/project files.
- **Related skills/rules:** `clean-architecture-rules`, `tests`.
- **Acceptance criteria:** User approves or corrects scope; implementation agent records current branch/worktree state; no unrelated dirty files are reverted.
- **Verification:** `git status --short`; `dotnet build --configuration Release --verbosity quiet`.
- **Rollback / failure handling:** If baseline build fails, stop implementation and document the failure in context/tasks before deciding whether it is pre-existing.

#### Task 0.1: Confirm Fallback Contract Or Add A New Intent

- **Type:** investigate/docs
- **Layer:** Docs
- **Files:** `.claude/contract/intents.yaml` existing, `dev/active/multi-tenant-control-plane/*` existing
- **Description:** Decide whether this recurring control-plane workstream should get a dedicated intent such as `multi-tenant-control-plane`.
- **Acceptance Criteria:** Intent decision is documented; if added, architecture tests pass.
- **Dependencies:** None
- **Effort:** S
- **Required Skills/Rules:** AGENTS Contribution Contract, tests rule.
- **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`

#### Task 0.2: Re-Baseline Current Implementation

- **Type:** investigate/test
- **Layer:** Cross-layer
- **Files:** all files touched by future tasks
- **Description:** Re-read relevant code because the worktree already contains many unrelated modifications and untracked files, including an in-progress `Event.Web.BffHosting/` candidate, modified `Explore.Blazor` BFF files, and a related architecture-test candidate.
- **Acceptance Criteria:** Context file lists current branch status, known unrelated changes, whether the existing BFF extraction candidate is accepted/refined/replaced, and any existing control-plane/lifecycle code that appeared after this plan.
- **Dependencies:** None
- **Effort:** M
- **Required Skills/Rules:** AGENTS dirty-worktree rule.
- **Validation:** `git status --short`; targeted file reads/searches; baseline build command.

### Phase 1: Shared BFF Hosting Foundation

- **Goal:** Extract the security-sensitive browser-BFF hosting primitives from `Explore.Blazor` into `Event.Web.BffHosting`, then make `Explore.Blazor` consume the new library before any separate BFF host is added.
- **Depends on:** Phase 0.
- **Relevant files:** `Event.Web.BffHosting/` new, `Explore.Blazor/Extensions/AuthenticationExtensions.cs` existing, `Explore.Blazor/Extensions/YarpProxyExtensions.cs` existing, `Explore.Blazor/Services/DynamicAuthSchemeManager.cs` existing, `Explore.Blazor/Services/TokenRefreshCookieEvents.cs` existing, `Explore.Blazor/Services/TenantHeaderForwardingHandler.cs` existing, `Explore.Blazor.IntegrationTests/` existing.
- **Related skills/rules:** `blazor-bff-patterns`, `auth-patterns`, `clean-architecture-rules`, `tests`.
- **Acceptance criteria:** `Event.Web.BffHosting` contains only server-side BFF hosting primitives; `Explore.Blazor` uses it without behavior regression; privileged browser headers are stripped; server-held tokens are forwarded only by trusted transforms; safe diagnostics remain redacted; shared token refresh stays server-side; architecture tests enforce allowed references.
- **Verification:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`; `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj`.
- **Rollback / failure handling:** If extraction changes public-host behavior, stop before adding control-plane UI or the separate app, document the failing BFF parity test, and repair the shared hosting boundary first.

#### Task 1.1: Create Or Complete `Event.Web.BffHosting`

- **Type:** create
- **Layer:** Blazor/BFF
- **Files:** `Event.Web.BffHosting/Event.Web.BffHosting.csproj` new/in-progress, `Event.Web.BffHosting/Authentication/*` new/in-progress, `Event.Web.BffHosting/Cookies/*` new, `Event.Web.BffHosting/Proxy/*` new/in-progress, `Event.Web.BffHosting/Security/*` new/in-progress, `Event.Web.BffHosting/Diagnostics/*` new, `Event.Web.BffHosting/Extensions/*` new/in-progress
- **Description:** Scaffold or reconcile the current dirty-worktree candidate into a class library for ASP.NET Core BFF hosting primitives. Include options/profile types such as `EventBffHostProfile`, proxy options, privileged-header names/policies, token safety, API base-address resolution, and service registration extensions.
- **Acceptance Criteria:** Project builds; every new file has two ABOUTME lines; generated `bin/` and `obj/` outputs are not treated as source; the library has no dependency on Blazor UI/client projects, API, Application, Domain, Persistence, generated clients, or Docker/provisioning assets.
- **Dependencies:** Task 0.2
- **Effort:** M
- **Required Skills/Rules:** blazor-bff-patterns, auth-patterns, Clean Architecture.
- **Validation:** Build plus architecture tests.

#### Task 1.2: Move Shared OIDC/Cookie/Token Refresh Primitives

- **Type:** modify
- **Layer:** Blazor/BFF
- **Files:** `Explore.Blazor/Extensions/AuthenticationExtensions.cs` existing, `Explore.Blazor/Services/DynamicAuthSchemeManager.cs` existing, `Explore.Blazor/Services/TokenRefreshCookieEvents.cs` existing, `Event.Web.BffHosting/Authentication/*` new, `Event.Web.BffHosting/Cookies/*` new
- **Description:** Move or wrap reusable Keycloak/Google OIDC, cookie-event, safe remote failure, token refresh, and diagnostics behavior behind shared BFF hosting APIs. Keep dynamic provider orchestration, admin-claim enrichment, circuit/session cleanup, and setup-aware redirects host-specific.
- **Acceptance Criteria:** `Explore.Blazor` keeps current login/logout/token-refresh behavior through `Event.Web.BffHosting`; browser-visible OIDC failures remain redacted; client secrets and tokens are never exposed to browser config or logs; token-refresh HTTP backchannel lifetime is container-managed.
- **Dependencies:** Task 1.1
- **Effort:** L
- **Required Skills/Rules:** auth-patterns, blazor-bff-patterns.
- **Validation:** `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj`.

#### Task 1.3: Move Shared YARP Proxy And Header Security Primitives

- **Type:** modify
- **Layer:** Blazor/BFF
- **Files:** `Explore.Blazor/Extensions/YarpProxyExtensions.cs` existing, `Explore.Blazor/Services/TenantHeaderForwardingHandler.cs` existing, `Event.Web.BffHosting/Proxy/*` new, `Event.Web.BffHosting/Security/*` new
- **Description:** Move or wrap reusable YARP API proxy registration, access-token forwarding, privileged-header stripping, trusted tenant-hint forwarding, setup/support header protection, and forwarded-header trust options.
- **Acceptance Criteria:** Raw browser `X-Tenant-Slug`, `X-Setup-Secret`, support/break-glass headers, and authorization tokens cannot become trusted downstream state; trusted tenant hints are added only from server-side context; proxy errors do not leak internal URLs or secrets.
- **Dependencies:** Task 1.1
- **Effort:** L
- **Required Skills/Rules:** blazor-bff-patterns, security rules.
- **Validation:** Shared or parameterized BFF integration tests for header stripping and token forwarding.

#### Task 1.4: Make `Explore.Blazor` Consume `Event.Web.BffHosting`

- **Type:** modify/test
- **Layer:** Blazor/BFF
- **Files:** `Explore.Blazor/Program.cs` existing, `Explore.Blazor/Extensions/*` existing, `Explore.Blazor/appsettings*.json` existing, `Explore.Blazor.IntegrationTests/*` existing
- **Description:** Replace local BFF setup with `AddEventBffHosting(..., EventBffHostProfile.PublicWeb)` and related app/proxy endpoint mapping. Keep public web app behavior stable.
- **Acceptance Criteria:** Existing public/tenant/admin web flows still build and pass focused integration tests; no behavior depends on copied local proxy/header/safe-diagnostics/token-refresh setup; configuration remains compatible or docs name the breaking config change.
- **Dependencies:** Tasks 1.2, 1.3
- **Effort:** L
- **Required Skills/Rules:** blazor-bff-patterns, operations docs.
- **Validation:** Build, architecture tests, Blazor integration tests.

#### Task 1.5: Add BFF Hosting Architecture And Security Test Coverage

- **Type:** test
- **Layer:** Tests
- **Files:** `Event.Architecture.Tests/*` existing/new, `Explore.Blazor.IntegrationTests/*` existing/new
- **Description:** Add rules/tests proving `Event.Web.BffHosting` has no UI/business dependencies and proving BFF security behavior remains intact for the public web host before adding the separate control-plane host.
- **Acceptance Criteria:** Tests fail if `Event.Web.BffHosting` references control-plane UI, generated clients, Application/Domain/Persistence, or provisioning scripts; tests cover OIDC failure redaction, privileged-header stripping, shared BFF project boundaries, and server-side token forwarding. Separate-host token/client-secret browser-state assertions remain part of the `Event.ControlPlane.Blazor` test matrix.
- **Dependencies:** Tasks 1.1-1.4
- **Effort:** M
- **Required Skills/Rules:** tests, auth-patterns, blazor-bff-patterns.
- **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`; `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj`.

### Phase 2: Shared Control-Plane Client Library

- **Goal:** Create the reusable class library that both embedded and separate control-plane surfaces consume.
- **Depends on:** Phase 0. Can proceed after BFF hosting interfaces are clear, but does not need to wait for every BFF test if the work is split cleanly.
- **Relevant files:** `Event.ControlPlane.Client/` new, solution file existing, `Directory.Packages.props` existing, `Explore.Blazor.Client/` existing.
- **Related skills/rules:** `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `auth-patterns`, `clean-architecture-rules`.
- **Acceptance criteria:** The new library builds, has no forbidden project references, contains route/service/component boundaries, and can be referenced by `Explore.Blazor.Client` without dependency cycles.
- **Verification:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`.
- **Rollback / failure handling:** If RCL/static asset integration is blocked, document the exact project-system issue and choose a host-neutral class library shape before adding UI surface.

#### Task 2.1: Create `Event.ControlPlane.Client`

- **Type:** create
- **Layer:** Blazor
- **Files:** `Event.ControlPlane.Client/Event.ControlPlane.Client.csproj` new, `Event.ControlPlane.Client/_Imports.razor` new, `Event.ControlPlane.Client/Routing/ControlPlaneRoutes.cs` new, `Event.ControlPlane.Client/Extensions/ControlPlaneClientServiceCollectionExtensions.cs` new
- **Description:** Add a Razor class library for control-plane pages, components, route constants, service contracts, and DI registration. The project must not reference `Explore.Blazor.Client`, `Explore.API`, `Explore.Infrastructure`, `Explore.Persistence`, or `Explore.Domain`.
- **Acceptance Criteria:** Library compiles; all new files have two ABOUTME lines; project can be referenced by Blazor hosts; no forbidden dependencies.
- **Dependencies:** Task 0.2
- **Effort:** M
- **Required Skills/Rules:** Blazor UI conventions, Clean Architecture.
- **Validation:** Build plus architecture tests.

#### Task 2.2: Add Route Constants And Service Registration Extension

- **Type:** create
- **Layer:** Blazor
- **Files:** `Event.ControlPlane.Client/Routing/ControlPlaneRoutes.cs` new, `Event.ControlPlane.Client/Extensions/*` new
- **Description:** Add route constants and DI registration extensions so embedded and separate hosts register the same control-plane routes and services without duplicating route strings.
- **Acceptance Criteria:** Embedded and separate hosts can register shared routes/services from the library; route constants stay host-neutral and contain no auth/token behavior.
- **Dependencies:** Task 2.1
- **Effort:** S
- **Required Skills/Rules:** Blazor UI conventions.
- **Validation:** Build.

#### Task 2.3: Define Host-Neutral Control-Plane Service Contracts

- **Type:** create
- **Layer:** Blazor
- **Files:** `Event.ControlPlane.Client/Contracts/*` new, `Event.ControlPlane.Client/Services/*` new
- **Description:** Define service abstractions for overview, tenants, domains, operations, security, policies, storage, and backups. Components depend on these abstractions, not directly on generated clients.
- **Acceptance Criteria:** Contracts express page data and operations without token handling; action-capable resources expose HAL links; components can be unit-tested with fake services.
- **Dependencies:** Task 2.1
- **Effort:** M
- **Required Skills/Rules:** BFF patterns, HAL affordance gating.
- **Validation:** Build plus initial bUnit smoke tests once components exist.

#### Task 2.4: Resolve Shared Design-System Dependency

- **Type:** investigate/modify
- **Layer:** Blazor
- **Files:** `Explore.Blazor.Client/Components/` existing, shared component locations existing/new only if explicitly approved
- **Description:** Determine whether control-plane components can consume existing App* wrapper components without a dependency cycle. Prefer host-provided wrappers or a narrowly approved shared UI extraction over copy-pasting design-system components.
- **Acceptance Criteria:** No duplicated design-system components; no circular project references; control-plane components follow wrapper/design-token conventions.
- **Dependencies:** Task 2.1
- **Effort:** M
- **Required Skills/Rules:** design-system, blazor-css-isolation.
- **Validation:** Build, architecture tests, visual/component smoke tests.

#### Task 2.5: Add Architecture Coverage For The New Shared UI Library

- **Type:** test
- **Layer:** Tests
- **Files:** `Event.Architecture.Tests/*` existing/new
- **Description:** Add or extend architecture rules so the new shared control-plane UI library cannot take dependencies on forbidden projects and all new files follow context rules.
- **Acceptance Criteria:** Tests catch forbidden references and missing ABOUTME headers for new projects/files.
- **Dependencies:** Task 2.1
- **Effort:** M
- **Required Skills/Rules:** tests, Clean Architecture.
- **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`

### Phase 3: Control-Plane API And Application Capabilities

- **Goal:** Provide server-authoritative, instance-admin-only control-plane read models and mutations needed by the UI.
- **Depends on:** Phase 2 can proceed in parallel for contracts, but API implementation requires current endpoint inventory.
- **Relevant files:** `Explore.Application/Features/*` existing/new, `Explore.API/Controllers/*` existing/new, `Explore.API/Hateoas/*` existing/new, `Explore.Persistence/*` existing/new.
- **Related skills/rules:** `cqrs-mediatr-guidelines` by implication, `api-controllers`, `api-hateoas`, `application-layer`, `efcore-persistence`, `outbox-pattern`, `auth-patterns`.
- **Acceptance criteria:** Endpoints enforce instance-admin authority, multi-tenant-only behavior, tenant isolation, HAL actions, idempotency/audit for writes, and no repository DTO leakage.
- **Verification:** Application unit tests, API integration tests, persistence integration tests where schema/query changes exist.
- **Rollback / failure handling:** If an endpoint risks leaking tenant business data, block the task and redesign the read model with explicit bounded predicates and audit requirements.

#### Task 3.1: Inventory Existing Admin Endpoints And DTOs

- **Type:** investigate
- **Layer:** API/Application
- **Files:** `Explore.API/Controllers/InstanceSettingsController.cs` existing, `Explore.API/Controllers/TenantController.cs` existing, `Explore.API/Controllers/InstanceOnboardingController.cs` existing, related handlers existing
- **Description:** Map which existing endpoints can back control-plane pages and which new endpoints/read models are required.
- **Acceptance Criteria:** Context file includes endpoint inventory, gaps, and proposed reuse/new decisions.
- **Dependencies:** Task 0.2
- **Effort:** M
- **Required Skills/Rules:** API controllers, Application layer.
- **Validation:** Targeted searches and direct reads recorded in context.

#### Task 3.2: Add Control-Plane Overview Read Model

- **Type:** create
- **Layer:** Application/API
- **Files:** `Explore.Application/Features/ControlPlane/Queries/*` new, `Explore.API/Controllers/ControlPlaneController.cs` new or existing admin controller extension, `Explore.API/Hateoas/*` new if HAL resource is needed
- **Description:** Return deployment mode, app version, configured hosts, tenant counts by status, provider status summaries, outstanding warnings, queue/outbox summary, and links to deeper resources.
- **Acceptance Criteria:** Instance admins can read overview in multi-tenant mode; single-tenant mode is hidden/blocked as designed; no tenant business data is exposed.
- **Dependencies:** Task 3.1
- **Effort:** L
- **Required Skills/Rules:** CQRS, API, HATEOAS, auth patterns.
- **Validation:** `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj`; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj`.

#### Task 3.3: Tenant Lifecycle Control-Plane Surface

- **Type:** create/modify
- **Layer:** Domain/Application/Persistence/API
- **Files:** tenant lifecycle handlers/controllers/repositories existing or new after inventory
- **Description:** Expose tenant list/detail/actions for create/provision, suspend, archive, purge scheduling, restore/reactivate where supported, domain status, and audit trail. Use existing lifecycle model if present.
- **Acceptance Criteria:** Actions are instance-admin-only; HAL links reflect allowed transitions; destructive actions require confirmation and audit; purge is delayed/async unless explicitly designed otherwise.
- **Dependencies:** Task 3.1
- **Effort:** XL
- **Required Skills/Rules:** Domain, EF Core, outbox, HATEOAS.
- **Validation:** Domain/Application unit tests, persistence integration tests if schema changes, API integration tests for state transitions and authorization.

#### Task 3.4: Domains And DNS Verification Surface

- **Type:** create/modify
- **Layer:** Application/API/Infrastructure
- **Files:** domain/routing settings handlers existing or new, DNS verification service new if needed
- **Description:** Provide root domain, wildcard domain, control-plane host, custom tenant domain, and DNS verification status to onboarding and control-plane pages.
- **Acceptance Criteria:** UI receives structured DNS checklist rows; reverse-proxy/forwarded-host trust rules are respected; unresolved tenant hosts still fail closed.
- **Dependencies:** Task 3.1
- **Effort:** L
- **Required Skills/Rules:** multi-tenancy docs, BFF patterns, configuration.
- **Validation:** API integration tests for host/domain cases; unit tests for DNS checklist generation.

#### Task 3.5: Operations, Jobs, Storage, Email, And Provider Status

- **Type:** create/modify
- **Layer:** Application/API/Persistence/Infrastructure
- **Files:** outbox/email/storage/provider handlers and controllers existing or new
- **Description:** Add read-only summaries first for outbox/dead-letter, background jobs, SMTP/email dispatch health, storage status/usage, auth provider status, and authorization provider status. Add safe mutation endpoints only where backed by audit/idempotency.
- **Acceptance Criteria:** Operators can see operational warnings without tenant data leakage; failed/retryable actions are HAL-gated; provider health failures are observable and do not crash the UI.
- **Dependencies:** Task 3.1
- **Effort:** XL
- **Required Skills/Rules:** outbox-pattern, error-tracking, API HATEOAS.
- **Validation:** Application/API tests plus infrastructure tests where provider adapters are touched.

#### Task 3.6: Regenerate Or Update API Contract Artifacts

- **Type:** docs/test
- **Layer:** API/Docs
- **Files:** `schemas/openapi.json` existing, `docs/API_CHANGELOG.md` existing
- **Description:** Regenerate or update API contract artifacts for new or changed control-plane endpoints.
- **Acceptance Criteria:** OpenAPI schema and API changelog match implemented endpoint routes, request/response shapes, auth requirements, and HAL links.
- **Dependencies:** Phase 3 endpoint tasks
- **Effort:** M
- **Required Skills/Rules:** API contract, docs contract.
- **Validation:** API contract/inventory tests and docs review.

### Phase 4: Embedded Instance Console And Multi-Tenant Control-Plane UI

- **Goal:** Surface the shared control-plane library inside the existing Blazor app for multi-tenant deployments.
- **Depends on:** Phase 2 and enough Phase 3 APIs for the first screens.
- **Relevant files:** `Explore.Blazor.Client/Routes.razor` existing, `Explore.Blazor.Client/Pages/Admin/Instance/*` existing, `Event.ControlPlane.Client/*` new.
- **Related skills/rules:** `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `auth-patterns`, `api-hateoas`.
- **Acceptance criteria:** Multi-tenant instance admins can access the embedded console; single-tenant admins keep the existing settings page; resource actions are HAL-gated.
- **Verification:** `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj`; `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj`.
- **Rollback / failure handling:** If route integration breaks existing admin pages, revert the route registration slice and keep shared library/API changes isolated.

#### Task 4.1: Reference `Event.ControlPlane.Client` From `Explore.Blazor.Client`

- **Type:** modify
- **Layer:** Blazor
- **Files:** `Explore.Blazor.Client/Explore.Blazor.Client.csproj` existing, solution/project references
- **Description:** Reference the shared control-plane client library from the existing Blazor client so embedded routes/components can be discovered without copying implementation.
- **Acceptance Criteria:** Embedded client builds and can discover the control-plane component assembly without a dependency cycle.
- **Dependencies:** Task 2.1
- **Effort:** S
- **Required Skills/Rules:** Blazor project structure, Clean Architecture.
- **Validation:** Build and architecture tests.

#### Task 4.2: Register Embedded Control-Plane Routes

- **Type:** modify
- **Layer:** Blazor
- **Files:** `Explore.Blazor.Client/Routes.razor` existing, `Event.ControlPlane.Client/Routing/*` new
- **Description:** Map shared control-plane routes under `/admin/instance/*` in the existing app.
- **Acceptance Criteria:** Multi-tenant instance admins can route to the control-plane overview; single-tenant route behavior remains correct.
- **Dependencies:** Task 4.1
- **Effort:** M
- **Required Skills/Rules:** Blazor routing, auth patterns.
- **Validation:** bUnit route guard/navigation tests.

#### Task 4.3: Add Embedded Control-Plane Navigation And Shell Behavior

- **Type:** modify
- **Layer:** Blazor
- **Files:** shell/navigation components existing, control-plane shell components new
- **Description:** Update navigation and shell behavior so the multi-tenant control-plane surface appears only for instance administrators in multi-tenant mode.
- **Acceptance Criteria:** Control-plane nav appears only in multi-tenant mode for instance admins; public/tenant nav remains unchanged; action buttons remain HAL-gated.
- **Dependencies:** Task 4.2
- **Effort:** M
- **Required Skills/Rules:** Blazor UI, auth patterns, HAL UI.
- **Validation:** bUnit navigation/shell tests.

#### Task 4.4: Build Overview, Tenants, And Domains First Slice

- **Type:** create
- **Layer:** Blazor
- **Files:** `Event.ControlPlane.Client/Pages/Overview/*` new, `Event.ControlPlane.Client/Pages/Tenants/*` new, `Event.ControlPlane.Client/Pages/Domains/*` new, CSS isolation files new
- **Description:** Implement overview, tenants, and domains as the first useful vertical slice, backed by Phase 3 services.
- **Acceptance Criteria:** Pages have `PageTitle` and `h1`; controls use design-system wrappers; icon buttons have labels; mobile/desktop text does not overlap; actions are HAL-gated.
- **Dependencies:** Tasks 3.2, 3.3, 3.4
- **Effort:** L
- **Required Skills/Rules:** accessibility, design system, HAL UI.
- **Validation:** bUnit tests and Playwright/manual visual smoke if E2E infrastructure is available.

#### Task 4.5: Add Single-Tenant Suppression Regression Tests

- **Type:** test/modify
- **Layer:** Blazor/API
- **Files:** existing route guards, `BlockInSingleTenantAttribute.cs` existing, relevant tests existing/new
- **Description:** Add regression coverage proving single-tenant mode does not show the multi-tenant control-plane shell or navigation.
- **Acceptance Criteria:** Existing `/admin/instance/settings` remains the single-tenant abstraction; multi-tenant-only routes are hidden or blocked; tests cover both modes.
- **Dependencies:** Tasks 4.2, 4.3
- **Effort:** M
- **Required Skills/Rules:** deployment modes, tests.
- **Validation:** Blazor client tests and API integration tests.

### Phase 5: Multi-Tenant Onboarding Control-Plane And DNS Guidance

- **Goal:** Make multi-tenant onboarding ask how platform administration should be accessed and show DNS/reverse-proxy guidance.
- **Depends on:** Phase 3 domain/checklist support and Phase 2 shared UI.
- **Relevant files:** `Explore.Blazor.Client` onboarding pages existing, `Explore.Application/Features/InstanceOnboarding/*` existing, control-plane onboarding components new.
- **Related skills/rules:** Blazor UI, Application layer, configuration, self-hosting docs.
- **Acceptance criteria:** Onboarding offers embedded admin area, dedicated admin hostname, and separate control-plane app options with truthful availability; DNS checklist is visible and actionable.
- **Verification:** Application unit tests, Blazor component tests, API integration tests for persisted onboarding settings.
- **Rollback / failure handling:** If separate app is not implemented yet, option C must be shown as future/disabled or omitted; do not advertise unavailable deployment shapes as available.

#### Task 5.1: Model Administration Access Shape

- **Type:** create/modify
- **Layer:** Application/Persistence/API
- **Files:** onboarding DTOs/settings handlers existing/new
- **Description:** Persist or derive the chosen administration access shape for multi-tenant onboarding: embedded, dedicated admin hostname, separate app. Only persist values that affect runtime behavior.
- **Acceptance Criteria:** Single-tenant onboarding does not ask this question; multi-tenant onboarding has deterministic defaults; invalid combinations produce validation errors.
- **Dependencies:** Task 3.4
- **Effort:** M
- **Required Skills/Rules:** Application layer, validators manually instantiated.
- **Validation:** Unit and API integration tests.

#### Task 5.2: Add DNS Checklist And Preflight Results

- **Type:** create/modify
- **Layer:** Blazor/Application/API
- **Files:** onboarding components existing/new, DNS checklist read model new
- **Description:** Show public platform host, wildcard tenant host, control-plane host, and custom-domain CNAME guidance. Make clear that onboarding can finish before DNS is fully configured, but affected features stay inactive.
- **Acceptance Criteria:** Checklist entries are structured; failed/missing checks are warnings, not silent failures; copy is specific to multi-tenant setup.
- **Dependencies:** Task 3.4
- **Effort:** L
- **Required Skills/Rules:** accessibility, self-hosting docs.
- **Validation:** Component tests and API/read-model tests.

#### Task 5.3: Persist Only Runtime-Relevant Onboarding Settings

- **Type:** modify/test
- **Layer:** Application/Persistence
- **Files:** onboarding command handlers existing, settings services existing
- **Description:** Persist only the onboarding choices that affect runtime behavior, such as admin host or deployment-shape configuration. Keep purely informational choices out of durable settings.
- **Acceptance Criteria:** Persisted values affect host/control-plane behavior; informational choices are not over-modeled; validation prevents incomplete runtime configuration.
- **Dependencies:** Task 5.1
- **Effort:** M
- **Required Skills/Rules:** Application layer, EF Core if storage changes.
- **Validation:** Application and persistence tests if storage changes.

### Phase 6: Dedicated Control-Plane Hostname Using Existing App Image

- **Goal:** Support `admin.example.org` with the existing Blazor/BFF image before requiring a separate app.
- **Depends on:** Embedded control-plane routes and host classification.
- **Relevant files:** `Explore.Blazor/*` existing, `Explore.Blazor.Client/*` existing, configuration docs, Docker/proxy docs.
- **Related skills/rules:** `blazor-bff-patterns`, `auth-patterns`, `error-tracking`, configuration docs.
- **Acceptance criteria:** Admin host shows control-plane shell; public/tenant host does not show control-plane shell except embedded admin routes; instance-admin auth is enforced server-side.
- **Verification:** Blazor integration tests for host classification and header behavior; docs smoke review.
- **Rollback / failure handling:** If host classification conflicts with tenant resolution, disable dedicated-host shell and keep embedded routes until the host model is fixed.

#### Task 6.1: Add Control-Plane Host Configuration

- **Type:** create/modify
- **Layer:** Infrastructure/Blazor/Configuration
- **Files:** configuration settings classes existing/new, `docs/CONFIGURATION.md` existing
- **Description:** Add static configuration for admin hosts, optional cookie/security settings, and host classification. Do not store deployment-critical host routing only in tenant DB settings.
- **Acceptance Criteria:** Admin hosts are recognized after trusted forwarded-header processing; invalid config fails clearly; config is documented.
- **Dependencies:** Task 3.4
- **Effort:** M
- **Required Skills/Rules:** BFF patterns, configuration.
- **Validation:** Unit/integration tests for host classification.

#### Task 6.2: Implement Host-Based Shell Separation

- **Type:** modify
- **Layer:** Blazor
- **Files:** `Explore.Blazor` existing, `Explore.Blazor.Client` shell/routing existing, `Event.ControlPlane.Client` new
- **Description:** When request host is an admin host, render the control-plane shell and hide public discovery/tenant navigation. Enforce instance-admin route guard before showing content.
- **Acceptance Criteria:** Admin host root routes to overview; public host keeps public shell; tenant hosts keep tenant shell; no action affordance is local-role based.
- **Dependencies:** Tasks 4.1, 6.1
- **Effort:** L
- **Required Skills/Rules:** Blazor UI, auth patterns, BFF patterns.
- **Validation:** Blazor integration tests and component tests.

#### Task 6.3: Add Dedicated-Host Security Options

- **Type:** create/modify
- **Layer:** Blazor/API/Configuration
- **Files:** BFF auth configuration existing, rate limiting config existing, security docs
- **Description:** Add or document optional stricter settings for admin host: separate cookie name/domain where supported, stricter CSP, optional IP allowlist, tighter mutation rate limits, and MFA-ready guidance.
- **Acceptance Criteria:** Defaults are secure; misconfigured allowlists fail closed; docs explain what is supported now vs future.
- **Dependencies:** Task 6.1
- **Effort:** L
- **Required Skills/Rules:** auth-patterns, error-tracking.
- **Validation:** Integration tests for protected host behavior and docs review.

#### Task 6.4: Update Reverse-Proxy And Self-Hosting Docs For Dedicated Host

- **Type:** docs
- **Layer:** Docs/DevOps
- **Files:** `docs/SELF_HOSTING.md` existing, `docs/CONFIGURATION.md` existing, `docs/DEPLOYMENT_MODES.md` existing
- **Description:** Document public host, wildcard tenant host, admin host, trusted forwarded-header requirements, TLS expectations, and reverse-proxy examples for the dedicated-host shape.
- **Acceptance Criteria:** Self-hosters can configure dedicated admin hostname routing without weakening tenant host resolution or browser-BFF header trust boundaries.
- **Dependencies:** Tasks 6.1, 6.2
- **Effort:** M
- **Required Skills/Rules:** operations docs, deployment modes.
- **Validation:** Docs review and build.

### Phase 7: Separate Self-Hostable Control Plane Blazor/BFF App

- **Goal:** Create a deployable `Event.ControlPlane.Blazor` app that consumes the shared control-plane library.
- **Depends on:** Stable shared BFF hosting library, stable shared control-plane client library, and enough control-plane API coverage.
- **Relevant files:** `Event.ControlPlane.Blazor/` new, existing Blazor integration/client test projects, Dockerfile/compose/AppHost files existing/new, `docker/keycloak/realm-export.json` existing, `docker/keycloak/keycloak-init.sh` existing, `.env.example` existing, solution file existing.
- **Related skills/rules:** `blazor-bff-patterns`, `auth-patterns`, `aspire`, `design-system`, `tests`.
- **Acceptance criteria:** Separate app builds, authenticates through Keycloak OIDC as a confidential BFF client, calls the same API, uses `Event.ControlPlane.Client`, and can be self-hosted through documented Docker Compose/Aspire configuration.
- **Verification:** Build, Blazor integration tests, Docker Compose profile smoke, Aspire AppHost smoke where applicable.
- **Rollback / failure handling:** If reusable BFF extraction is too risky, ship separate app behind a feature branch/profile with documented limitations, but do not duplicate security-sensitive transforms silently.

#### Task 7.1: Scaffold `Event.ControlPlane.Blazor`

- **Type:** create
- **Layer:** Blazor/DevOps
- **Files:** `Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj` new, `Event.ControlPlane.Blazor/Program.cs` new, `Event.ControlPlane.Blazor/appsettings.json` new, solution file existing
- **Description:** Add a BFF host that references `Event.Web.BffHosting` and `Event.ControlPlane.Client`, configures the `ControlPlane` BFF host profile, static assets, safe auth diagnostics, and health endpoints as appropriate.
- **Acceptance Criteria:** App builds; no browser token exposure; no control-plane shell without authenticated Keycloak session plus instance-admin authority; route root renders the control-plane overview after auth; all files have ABOUTME headers.
- **Dependencies:** Tasks 1.4, 2.1, 3.2, 4.2
- **Effort:** L
- **Required Skills/Rules:** BFF patterns, auth patterns.
- **Validation:** Build and Blazor integration tests.

#### Task 7.2: Define Dedicated Keycloak OIDC Client And Secret Boundary

- **Type:** create/modify/docs
- **Layer:** Blazor/DevOps/Docs
- **Files:** `docker/keycloak/realm-export.json` existing, `docker/keycloak/keycloak-init.sh` existing, `.env.example` existing, `Explore.AppHost/AppHost.cs` existing, `docs/CONFIGURATION.md` existing, `docs/SELF_HOSTING.md` existing, `docs/SECURITY-MODEL.md` existing
- **Description:** Add or document a dedicated confidential Keycloak client for the separate control-plane app, for example `islamu-event-control-plane`. Define redirect URI, post-logout redirect URI, client-secret source, local Compose/Aspire secret injection, and external-Keycloak onboarding guidance.
- **Acceptance Criteria:** Control-plane OIDC client secret is server-only; browser config never contains it; self-hosters know which env vars/secrets to set; local Keycloak import/init can provision or sync the client; missing/invalid OIDC config fails with safe diagnostics and unhealthy readiness where appropriate.
- **Dependencies:** Task 7.1
- **Effort:** L
- **Required Skills/Rules:** auth-patterns, blazor-bff-patterns, operations docs.
- **Validation:** Keycloak realm/config review; Blazor integration tests for challenge/callback; self-hosting docs review.

#### Task 7.3: Consume Shared BFF Hosting With Control-Plane Profile

- **Type:** investigate/modify
- **Layer:** Blazor
- **Files:** `Event.Web.BffHosting/*` new, `Event.ControlPlane.Blazor/*` new, `Explore.Blazor.IntegrationTests/*` existing/new
- **Description:** Configure `Event.ControlPlane.Blazor` through `Event.Web.BffHosting` using the `ControlPlane` profile. Avoid local reimplementation of token forwarding, tenant header stripping, setup/support header stripping, YARP transforms, Keycloak OIDC configuration, safe auth diagnostics, token refresh, and backchannel hardening.
- **Acceptance Criteria:** Existing and separate BFF hosts use the same shared security-sensitive library with different explicit profiles; tests prove privileged headers are stripped in both hosts; OIDC challenge/callback/remote-failure handling stays safe and does not leak client-secret or provider diagnostics to the browser.
- **Dependencies:** Tasks 1.5, 7.1, 7.2
- **Effort:** L
- **Required Skills/Rules:** blazor-bff-patterns, auth-patterns.
- **Validation:** `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj` plus new control-plane host tests.

#### Task 7.4: Add Self-Hosting Deployment Shape

- **Type:** create/modify/docs
- **Layer:** DevOps/Docs
- **Files:** `docker-compose.yml` existing, control-plane Dockerfile new if project-specific, `Explore.AppHost/AppHost.cs` existing, `docs/SELF_HOSTING.md` existing, `docs/OPERATIONS.md` existing, `.env.example` existing
- **Description:** Add a Docker Compose profile and image configuration for the separate control-plane host. Document environment variables, Keycloak client secret handling, reverse-proxy routing, admin host DNS, TLS expectations, and when this shape should be used.
- **Acceptance Criteria:** Self-hosters can run public app and control-plane app separately; docs distinguish separate UI host from true management plane; no required secrets are exposed in browser config; OIDC discovery, callback, logout, and API proxy URLs are documented for same-server and separate-server deployments.
- **Dependencies:** Tasks 7.1, 7.2
- **Effort:** L
- **Required Skills/Rules:** aspire, operations docs.
- **Validation:** Compose smoke commands documented and run where available.

#### Task 7.5: Add Aspire AppHost Resource

- **Type:** create/modify
- **Layer:** DevOps
- **Files:** `Explore.AppHost/AppHost.cs` existing, `Explore.AppHost/Properties/launchSettings.json` existing, AppHost settings existing
- **Description:** Add the separate control-plane app as an Aspire resource without breaking existing local topologies. In full-local mode, provide local Keycloak control-plane client settings and wait-for relationships consistent with the existing API/Blazor startup sequence.
- **Acceptance Criteria:** Aspire can start and describe the control-plane app resource; local Keycloak, API readiness, and control-plane host config are wired without hardcoding Compose host ports; existing Aspire profiles keep working.
- **Dependencies:** Tasks 7.1, 7.2
- **Effort:** M
- **Required Skills/Rules:** aspire, operations docs.
- **Validation:** Aspire smoke commands per the `aspire` skill where available.

#### Task 7.6: Add Separate-App UI And BFF Tests

- **Type:** test
- **Layer:** Blazor/DevOps
- **Files:** `Explore.Blazor.IntegrationTests` existing/new fixtures, `Explore.Blazor.Client.E2ETests` existing/new flows
- **Description:** Cover Keycloak OIDC challenge redirects, callback failure handling, correlation state, root overview render, API proxy behavior, privileged header stripping, single public shell absence, and failed API/provider states.
- **Acceptance Criteria:** Tests fail if separate app exposes public/tenant shell, accepts non-instance-admin users, bypasses Keycloak OIDC, leaks privileged headers, stores tokens in browser-visible state, or leaks OIDC/client-secret diagnostics; visual smoke covers desktop/mobile layout.
- **Dependencies:** Tasks 7.1, 7.2, 7.3
- **Effort:** L
- **Required Skills/Rules:** tests, visual QA expectations for UI work.
- **Validation:** Project-specific test commands and optional E2E smoke.

### Phase 8: Hardening, Docs, And Release Readiness

- **Goal:** Make the feature operable, auditable, documented, and safe for self-hosters.
- **Depends on:** Phases 1-7.
- **Relevant files:** docs under `docs/`, `schemas/openapi.json`, tests, operations configs.
- **Related skills/rules:** error-tracking, outbox-pattern, docs/operations, tests.
- **Acceptance criteria:** Docs match implemented behavior; full validation passes; known deferred work is explicit.
- **Verification:** Full build plus per-project tests listed in Section 14.
- **Rollback / failure handling:** If full validation fails, update context/tasks with failure and root cause before claiming completion.

#### Task 8.1: Audit And Operation Safety Review

- **Type:** investigate/modify/test
- **Layer:** Application/API/Persistence
- **Files:** audit/outbox/control-plane operation files existing/new
- **Description:** Review destructive and high-impact control-plane mutations for audit records, idempotency, retry behavior, and failure visibility.
- **Acceptance Criteria:** Every destructive operation has confirmation, authorization, audit, and recovery behavior documented and tested.
- **Dependencies:** Phase 3 mutations
- **Effort:** L
- **Required Skills/Rules:** outbox-pattern, auth patterns, error-tracking.
- **Validation:** Unit/integration tests for operation safety.

#### Task 8.2: Observability And Health Surface

- **Type:** create/modify
- **Layer:** Infrastructure/API/Blazor/Docs
- **Files:** health/metrics/logging files existing/new, `docs/TROUBLESHOOTING.md` existing
- **Description:** Add structured logs, metrics, health summaries, and operator-visible warnings for control-plane failures.
- **Acceptance Criteria:** Logs include tenant/user/operator context as fields, not high-cardinality labels; UI shows actionable failure states; troubleshooting docs describe common failures.
- **Dependencies:** Phase 3 operations
- **Effort:** M
- **Required Skills/Rules:** error-tracking.
- **Validation:** Unit/integration tests where feasible plus manual smoke.

#### Task 8.3: Update Product And Architecture Docs

- **Type:** docs
- **Layer:** Docs
- **Files:** `docs/ADMIN_GUIDE.md`, `docs/DEPLOYMENT_MODES.md`, `docs/MULTI_TENANCY.md`, `docs/SELF_HOSTING.md`, `docs/CONFIGURATION.md`, `docs/BLAZOR.md`, `docs/SECURITY-MODEL.md`, `docs/OPERATIONS.md`, `docs/CODEBASE_STRUCTURE.md`
- **Description:** Document `Event.Web.BffHosting`, Instance Console language, embedded, dedicated-host, and separate-app deployment shapes; route model; DNS guidance; config keys; security model; and self-hosting steps.
- **Acceptance Criteria:** Docs describe implemented behavior, do not imply multi-tenant tenant/platform controls in single-tenant mode, and do not list future app projects as part of this workstream.
- **Dependencies:** Phases 1-7
- **Effort:** L
- **Required Skills/Rules:** docs contract, operations docs.
- **Validation:** Architecture/context tests, docs review, build.

#### Task 8.4: Update API Changelog And OpenAPI Schema

- **Type:** docs/test
- **Layer:** API/Docs
- **Files:** `docs/API_CHANGELOG.md` existing, `schemas/openapi.json` existing
- **Description:** Update API changelog and OpenAPI schema for new/changed control-plane endpoints.
- **Acceptance Criteria:** New/changed endpoints are reflected in API docs/contracts with accurate auth and response metadata.
- **Dependencies:** Phase 3 endpoints
- **Effort:** M
- **Required Skills/Rules:** API contract, docs contract.
- **Validation:** API contract/inventory tests.

#### Task 8.5: Final Verification And Dev Docs Refresh

- **Type:** test/docs
- **Layer:** Cross-layer
- **Files:** all modified files, `dev/active/multi-tenant-control-plane/*`
- **Description:** Run required validation, update plan/context/tasks to final state, and produce a teaching summary.
- **Acceptance Criteria:** Build and relevant tests pass or failures are documented with root cause; dev docs reflect final implementation; remaining work is explicit.
- **Dependencies:** All implementation phases
- **Effort:** M
- **Required Skills/Rules:** tests, implementation agent contract.
- **Validation:** Section 14 commands.

## 7. Testing Strategy

| Requirement | Test project/files | Expected coverage |
|---|---|---|
| `Event.Web.BffHosting` has no forbidden dependencies and preserves browser-BFF trust boundaries. | `Event.Architecture.Tests`, `Explore.Blazor.IntegrationTests` | Project references, ABOUTME headers, no UI/business/generated-client references, OIDC failure redaction, no browser-visible tokens/secrets, privileged-header stripping, server-side token forwarding. |
| Shared control-plane client library has no forbidden dependencies and all files meet context rules. | `Event.Architecture.Tests` | Project references, ABOUTME headers, rule/context shape. |
| Control-plane read models and commands obey CQRS/auth rules. | `Event.Application.UnitTests` | Handler behavior, validation, authorization metadata, idempotency decisions. |
| Tenant lifecycle/domain/operation persistence is isolated and query filters remain safe. | `Event.Persistence.IntegrationTests` | EF mappings, migrations, tenant filters, repository behavior. |
| API endpoints require instance-admin authority and multi-tenant mode. | `Event.API.IntegrationTests` | 401/403/404 behavior, `BlockInSingleTenant`/`RequireMultiTenant`, rate limits, HAL links. |
| HAL links drive control-plane UI actions. | `Explore.Blazor.Client.Tests` and control-plane client tests | Buttons/actions appear only when `_links` contain allowed actions. |
| Embedded routes do not expose control plane in single-tenant mode. | `Explore.Blazor.Client.Tests` | Route guard/navigation tests for single vs multi-tenant mode. |
| Existing BFF and separate BFF consume shared BFF hosting and strip privileged headers consistently. | `Explore.Blazor.IntegrationTests` plus separate-host fixtures | `X-Tenant-Slug`, setup/support headers, token forwarding, host classification, profile-specific cookie/OIDC settings. |
| Separate app authenticates through Keycloak OIDC confidential-client BFF flow. | Focused fixtures in existing Blazor integration/client test projects | Challenge redirect, callback path, remote failure safety, cookie issuance, token refresh, logout, missing config readiness, no browser-visible tokens/client secrets, non-instance-admin denial. |
| Dedicated admin hostname selects control-plane shell. | Blazor integration tests and E2E smoke | Admin host root shows control plane; public/tenant host shells remain correct. |
| Separate app is self-hostable. | Build, Compose/Aspire smoke, E2E smoke where available | Docker profile starts; app authenticates; API calls work through BFF. |
| Visual/accessibility baseline is acceptable. | bUnit accessibility assertions plus Playwright/manual screenshots if E2E available | Page titles, h1, labels, aria labels, mobile/desktop layout, no text overlap. |

Do not run solution-level `dotnet test`. Use project-level test commands per repo policy.

## 8. Documentation, Configuration, And Operations Impact

Docs likely to update during implementation:

- `docs/ADMIN_GUIDE.md`: control-plane routes and operator workflows.
- `docs/DEPLOYMENT_MODES.md`: Event Instance Console language, multi-tenant-only tenant/platform control-plane behavior, and no casual mode toggle.
- `docs/MULTI_TENANCY.md`: host routing, admin host, tenant resolver implications.
- `docs/SELF_HOSTING.md`: embedded, dedicated-host, and separate-app deployment instructions.
- `docs/CONFIGURATION.md`: admin host config, cookie/security options, reverse-proxy trust.
- `docs/BLAZOR.md`: shared control-plane library and shared BFF hosting boundaries.
- `docs/SECURITY-MODEL.md`: control-plane route/auth/header/security model.
- `docs/OPERATIONS.md`: validation, health, outbox/job/backups guidance.
- `docs/CODEBASE_STRUCTURE.md`: new projects.
- `docs/API_CHANGELOG.md`, `schemas/openapi.json`: new or changed API contracts.

Config/deployment impact:

- New `Bff:*` or equivalent configuration shape for shared BFF host profiles, including `HostProfile`, public origin, API base address, OIDC authority/client/callback settings, cookie settings, proxy settings, and safe diagnostics.
- Possible static config for `ControlPlane:AdminHosts`, control-plane host mode, cookie name/domain, optional IP allowlist, and CSP profile.
- Docker Compose profile/image for `Event.ControlPlane.Blazor`.
- Aspire AppHost resource for the separate app.
- Dedicated Keycloak OIDC client config for the separate app, including client id, server-side client secret, authority/metadata address, callback path, sign-out callback path, and local/external Keycloak provisioning guidance.
- Updates to `docker/keycloak/realm-export.json`, `docker/keycloak/keycloak-init.sh`, `.env.example`, and local Aspire configuration if the implementation provisions a local control-plane client.
- Reverse-proxy examples preserving Host and `X-Forwarded-*` headers.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Control-plane pages require authenticated instance administrators. Client route guards improve UX only; API authorization is authoritative.
- `Event.Web.BffHosting` is the shared browser-BFF trust boundary for `Explore.Blazor` and `Event.ControlPlane.Blazor`. It owns Keycloak OIDC setup, cookie policy, server-side token forwarding, YARP proxy setup, privileged-header stripping, safe diagnostics, options validation, and health checks.
- `Event.Web.BffHosting` must not contain UI pages/components, business/domain logic, generated NSwag clients, persistence repositories, Keycloak provisioning scripts, Docker Compose definitions, or tenant lifecycle services.
- `Event.ControlPlane.Blazor` must authenticate through Keycloak OIDC using Authorization Code flow plus PKCE, a confidential client, and server-side secret storage. It must not support setup-secret, API-key, or browser-token operator login.
- The control-plane app should use a dedicated Keycloak client such as `islamu-event-control-plane` with narrowly scoped redirect/logout URIs for the admin/control-plane host.
- OIDC callback and remote-failure handling must use safe diagnostics: browser-visible errors stay generic, while logs/traces carry only redacted correlation handles and bounded failure categories.
- HttpOnly, Secure, SameSite-appropriate cookies are the browser session boundary; access and refresh tokens remain server-side and follow the existing BFF token-refresh hygiene.
- Keycloak MFA or required action enforcement should be documented as a Keycloak realm/client policy expectation for instance administrators. If not enforced in-app, docs must not imply the app can guarantee MFA alone.
- API writes require `[Authorize]` plus CQRS/runtime authorization metadata where applicable.
- HAL `_links` gate UI affordances for tenant/domain/job resources.
- Instance administrators must not get arbitrary tenant business data access through broad control-plane dashboards. Summary counts and operational status should use bounded projections.
- Tenant-specific inspection must require explicit tenant selection, audit context, and policy checks.
- Dedicated admin host should support stricter controls: optional IP allowlist, separate cookie settings where supported, stricter CSP, tighter mutation rate limits, and MFA-ready documentation.
- Browser-supplied tenant/setup/support headers must be stripped in both the main and separate BFF hosts.
- Destructive operations need strong confirmation, idempotency, audit, and preferably asynchronous execution through jobs/outbox.
- Provider health/status failures should fail closed for privileged actions and fail soft for read-only status cards.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

- **Multi-tenancy:** Applicable and central. Event Instance Console exists in both modes through the existing administration surface, while tenant/platform control-plane behavior is multi-tenant-only, uses API-authoritative tenant resolution, and must preserve tenant filters and fail-closed host behavior.
- **Federation:** Needs investigation. The control plane may later expose federation/provider status, but this plan does not design federation management unless current docs/code require it.
- **Localization:** Applicable. UI copy for onboarding, DNS warnings, confirmations, and status labels should use existing localization conventions if implemented for admin pages.
- **Accessibility:** Applicable. Every routable page needs `PageTitle` and `h1`; controls need labels; icon buttons need accessible names; focus/error handling must follow `docs/ACCESSIBILITY.md`.
- **Product:** Applicable. The operator experience should be utilitarian and information-dense, not a marketing dashboard. It should separate instance-owner duties from tenant-admin duties and keep single-tenant admins in the simpler settings abstraction.

## 11. Observability And Operations

- Add structured logs for control-plane actions with operator id, target tenant id when applicable, operation id, correlation id, and outcome.
- Do not put tenant/user IDs into high-cardinality Prometheus labels.
- Expose operator-visible status for API health, database reachability, cache, storage provider, SMTP/email dispatch, authorization provider, auth provider, outbox/dead-letter, and background jobs.
- Expose separate app readiness for OIDC discovery, API reachability, distributed cache/session support where used, and shutdown. Missing or invalid required Keycloak configuration should be visible as an operator-safe readiness failure.
- `Event.Web.BffHosting` should expose reusable health checks and bounded diagnostics for OIDC discovery, API proxy reachability, cookie/session dependencies, and safe remote-failure categories.
- Link UI warnings to troubleshooting docs or runbook steps.
- Track long-running operations with status and retry/dead-letter visibility.
- A separate UI host should have its own health endpoint, but docs must clarify that shared API/DB saturation still affects operation execution.

## 12. Migration And Compatibility Plan

- No ordinary settings toggle from single-tenant to multi-tenant.
- If mode conversion is requested later, design it as a migration wizard/runbook with backup confirmation, canonical public host, tenant base domain, default tenant review, data isolation preflight, reverse-proxy/DNS preflight, maintenance mode, execution, and verification.
- EF Core migrations are required only if new persistent entities/settings/audit records are introduced.
- Existing documented `/admin/instance/settings` should remain valid for current administration settings. In multi-tenant mode it can be part of the control-plane shell; in single-tenant mode it remains the simple instance administration page.
- Because the repo is pre-v1, do not add compatibility shims unless the user explicitly approves them.
- Deployment sequencing should ship in this order: shared BFF hosting extraction and `Explore.Blazor` adoption, shared control-plane UI library, API/read models, embedded console, dedicated host, separate app, full docs/hardening.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| `Event.Web.BffHosting` becomes a catch-all hosting framework. | Medium | High | Restrict it to auth, cookies, proxy, header security, diagnostics, health, and options validation. Architecture tests reject UI/business/generated-client dependencies. | Project references or namespaces pull in UI/Application/Domain/Persistence/generated clients. | Tasks 1.1, 1.5 |
| BFF extraction regresses existing public web auth/proxy behavior. | Medium | High | Make `Explore.Blazor` consume `Event.Web.BffHosting` first and run BFF integration tests before building `Event.ControlPlane.Blazor`. | Public web OIDC/proxy/header tests fail after extraction. | Tasks 1.2-1.5 |
| Shared control-plane library accidentally depends on `Explore.Blazor.Client`, blocking reuse. | Medium | High | Enforce host-neutral references and architecture tests. | Build cycle or architecture test failure. | Tasks 2.1-2.3 |
| New control-plane/BFF projects are created with the old `Explore.*` prefix. | Medium | Medium | Architecture/task review requires `Event.Web.BffHosting`, `Event.ControlPlane.Client`, and `Event.ControlPlane.Blazor`. | Solution/project list contains new `Explore.ControlPlane.*` or `Explore.*BffHosting` entries. | Tasks 1.1, 2.1, 7.1, 8.3 |
| Separate app weakens authentication by using a non-Keycloak or browser-token flow. | Low | High | Require Keycloak OIDC confidential-client BFF pattern, dedicated client, server-side token storage, and integration tests through `Event.Web.BffHosting`. | OIDC challenge/callback tests fail; browser-visible token/client secret found; app supports API-key/setup-secret login. | Tasks 7.1, 7.2, 7.6 |
| Separate BFF bypasses shared BFF hosting and drifts. | Medium | High | Require `Event.ControlPlane.Blazor` to use `Event.Web.BffHosting` `ControlPlane` profile and shared security assertions. | Local duplicate OIDC/YARP/header setup appears in `Event.ControlPlane.Blazor`; header stripping tests fail in one host. | Task 7.3 |
| Control-plane pages leak tenant business data to instance admins. | Medium | High | Use bounded operational projections, explicit tenant selection, audit, and policy checks. | API integration/security review finds broad data reads. | Tasks 3.2-3.5, 8.1 |
| Single-tenant admins see multi-tenant control-plane concepts. | Medium | Medium | Route guards, API filters, and regression tests for single mode. | bUnit/API tests fail; UI nav shows routes in single mode. | Task 4.3 |
| Dedicated admin host conflicts with tenant/domain resolution. | Medium | High | Classify admin hosts before tenant shell selection; respect trusted forwarded headers. | Host routing tests fail or tenant unresolved incorrectly. | Tasks 6.1-6.2 |
| DNS/preflight UI overpromises actual verification. | Medium | Medium | Distinguish configured, reachable, and unverified states; warn but allow onboarding where safe. | Manual self-hosting smoke finds misleading status. | Task 5.2 |
| Separate UI is mistaken for true operational rescue plane. | High | Medium | Document limitation; defer reserved-resource management API as future work. | Docs/product review catches ambiguous claims. | Tasks 7.4, 8.3 |
| Dirty worktree causes accidental overwrite of unrelated changes. | High | High | Re-baseline and isolate changes; never revert unrelated files. | Git diff includes unrelated edits. | Task 0.2 |

## 14. Success Metrics And Definition Of Done

Functional success:

- `Event.Web.BffHosting` exists, is consumed by `Explore.Blazor`, and keeps existing public web BFF behavior intact.
- Multi-tenant instance admins can use an embedded control-plane console.
- The embedded console and separate app consume `Event.ControlPlane.Client`.
- A separate `Event.ControlPlane.Blazor` app can be built and self-hosted.
- `Event.ControlPlane.Blazor` consumes `Event.Web.BffHosting` with a `ControlPlane` host profile rather than reimplementing BFF security primitives.
- `Event.ControlPlane.Blazor` authenticates through Keycloak OIDC as a confidential BFF client and denies non-instance-admin users before rendering the control-plane shell.
- Dedicated admin hostname behavior is implemented or explicitly deferred with docs if scope changes.
- Single-tenant mode keeps the existing single administration settings page and hides multi-tenant control-plane concepts.
- Control-plane actions are server-authorized and HAL-gated.
- High-risk operations are audited and safe.

Quality gates:

- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj`
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj` when persistence/migrations change
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj`
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj`
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj`
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj` when infrastructure/config/provider code changes
- E2E/Playwright or manual browser smoke for embedded, dedicated-host, and separate-app UI where feasible

Docs gates:

- Plan/context/tasks reflect actual state after every meaningful slice.
- Docs listed in Section 8 are updated for any implemented behavior/config/API/deployment changes.
- API changelog and OpenAPI schema are updated if API contracts change.

## 15. Implementation Agent Contract - KEEP DEV DOCS CURRENT

Future agents implementing this plan MUST follow this contract:

1. Before starting any implementation slice, read this plan, `multi-tenant-control-plane-context.md`, and `multi-tenant-control-plane-tasks.md`.
2. Start from the highest-priority incomplete task unless user instruction overrides it.
3. After completing each meaningful task or discovering new scope, update:
   - this plan if architecture/scope/phases/risks changed;
   - `multi-tenant-control-plane-context.md` with current state, decisions, files changed, blockers, validation, and next step;
   - `multi-tenant-control-plane-tasks.md` by checking completed items and adding discovered tasks.
4. Do not report "done" unless docs reflect the actual current state.
5. Every implementation summary to the user must include:
   - what was implemented, explained as a developer teaching summary rather than an abstract status line;
   - which architecture/design patterns, libraries, infrastructure components, protocols, and project abstractions were used;
   - which important files/classes/interfaces/handlers/components changed and what each is responsible for;
   - the relevant data/control flow through the implementation;
   - which project conventions or industry best practices were followed and why;
   - what was verified;
   - what remains;
   - what should be worked on next.
6. If validation fails, update context/tasks with the failure, root cause if known, and next recovery action.
7. Before pausing, context reset, handoff, or PR creation, refresh all three dev docs and add/refresh a handoff section.

## 16. Progress Reporting Contract

When an implementation agent finishes a slice, its final response should use this concise structure:

- **Implemented:** Medium-sized developer teaching summary of what changed, naming the patterns, libraries/infrastructure, important files/classes, and data/control flow. Do not collapse this to a single abstract sentence.
- **Verified:** Exact commands/checks run and their result.
- **Remaining:** Incomplete tasks, deferred decisions, known risks.
- **Next:** The next recommended implementation slice.
- **Docs updated:** Whether plan/context/tasks were updated, with reason if not.

The `Implemented` section must leave the user with the same high-level technical understanding they would have if they had implemented the slice themselves.

## 17. Potential Risks & Unknowns

The hardest part is not drawing the control-plane pages. Phase 1 has now centralized the browser-BFF foundation in `Event.Web.BffHosting` without turning it into a generic application framework or breaking the existing public web host. The next hard boundary is keeping `Event.ControlPlane.Client` host-neutral, then ensuring `Event.ControlPlane.Blazor` consumes the shared BFF profile instead of quietly reimplementing its own security boundary.
