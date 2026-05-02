# Convention-First Single-Tenant Onboarding - Context

Last Updated: 2026-05-02

## SESSION PROGRESS (2026-05-02)

### ✅ COMPLETED

- User requested a `/dev-docs` implementation plan for convention-first single-tenant onboarding.
- Loaded relevant skills: Clean Architecture, CQRS/MediatR, Blazor UI conventions, auth patterns, and Blazor BFF patterns.
- Completed required Tavily research.
- Completed required Context7 documentation lookup.
- Verified repo docs before citing them.
- Verified current source files/classes before citing them.
- Created the plan/context/tasks files under `dev/active/convention-first-single-tenant-onboarding/`.

### 🟡 IN PROGRESS

- No code implementation has started for this plan.
- Next implementation should start at Phase 0 route and SingleTenant tenant-onboarding safety.

### ⚠️ BLOCKERS

- None for planning.
- Implementation must account for a dirty worktree with many protected/user changes.

## User Intent

The user wants an implementation plan, not code changes, with these explicit requirements:

- No backward compatibility constraints; repository is in development mode.
- Use Tavily MCP for research.
- Use Context7 MCP for documentation.
- Follow repo conventions, industry best practices, design patterns/principles, Clean Architecture, enterprise-grade maintainability.
- Golden rule: Convention over Configuration.
- Fastest, smoothest time-to-value for most self-hosters.
- Platform launches with SingleTenant only by default.
- MultiTenant appears only when the operator explicitly configures `DEPLOYMENT_MODE=multi_tenant` via environment/secret.

## Research Evidence

### Tavily

Search used:

```text
2026 self hosted software onboarding first run setup wizard convention over configuration bootstrap admin setup secret deployment defaults best practices
```

Relevant takeaways:

- Minimize signup/setup friction.
- Use smart defaults and templates.
- Drive users to a first meaningful win quickly.
- Onboarding flows should remain versioned, tested, and secure.

### Context7

Libraries/docs consulted:

- ASP.NET Core docs: `/websites/learn_microsoft_en-us_aspnet_core`.
- Blazor docs: `/dotnet/blazor` and ASP.NET Core Blazor forms documentation surfaced through ASP.NET Core docs.

Relevant takeaways:

- ASP.NET Core default configuration includes appsettings, environment variables, and command line.
- Environment variables override appsettings by default.
- Blazor validated forms use `EditForm`, `DataAnnotationsValidator`, `ValidationSummary`, `ValidationMessage`, and `OnValidSubmit`.

Repo interpretation:

- `DEPLOYMENT_MODE=multi_tenant` is a correct explicit operator escape hatch.
- Normal onboarding should not ask for deployment mode.
- Wizard forms should use standard Blazor validation patterns.

## Verified Repo Documentation

### `CLAUDE.md`

Key rules:

- Clean Architecture: Domain → Application → Infrastructure/Persistence → API/Blazor.
- Repositories return entities.
- Validators are manually instantiated.
- Use `Guid` for aggregates.
- GET endpoints are usually `[AllowAnonymous]`; writes use `[Authorize]`.
- Each C# file starts with two ABOUTME comments.
- HAL links are UI action source of truth.
- Build verification: `dotnet build --configuration Release --verbosity quiet`.

### `docs/ARCHITECTURE.md`

Key facts:

- .NET 10 Clean Architecture + CQRS + BFF.
- `Explore.API` is the API host.
- `Explore.Blazor` is the BFF host.
- `Explore.Blazor.Client` is the UI client.
- Multi-tenancy supports SingleTenant and MultiTenant.
- SingleTenant uses a default tenant internally.
- MultiTenant resolves by trusted header/domain/subdomain.
- EF filters enforce tenant isolation.

### `docs/CONFIGURATION.md`

Key facts:

- `DEPLOYMENT_MODE` maps to `Deployment:Mode`.
- Values like `single_tenant`/`multi_tenant` are normalized to `SingleTenant`/`MultiTenant`.
- Default deployment mode is SingleTenant.
- First-run onboarding mode is controlled by API configuration.
- Current docs mention persisted runtime setting and admin mode switching; plan should change/lock this to operator-driven mode only unless explicit MultiTenant is enabled.

### `docs/SECURITY.md`

Key facts:

- Browser never owns tokens.
- BFF stores session/OIDC token state in HttpOnly cookies.
- BFF proxies API calls through YARP and attaches bearer token server-side.
- Incoming `X-Setup-Secret` is stripped and replaced by trusted BFF-held setup secret state.
- Forwarded host trust requires normalized `Request.Host` after trusted forwarded-header middleware.

### `docs/API.md`

Key facts:

- Middleware order includes tenant resolution before authentication and API-key tenant finalization after authentication.
- Setup secret validation has rate limiting.
- Controller and HATEOAS conventions apply to new endpoints.

### `docs/DOMAIN.md`

Key facts:

- `ITenantEntity` uses `TenantId` and global filters.
- `Actor` represents user/organization/group ownership with exactly one nullable owner FK.
- Onboarding intent must not create a new Domain scope concept.

### `docs/OPERATIONS.md`

Key facts:

- Aspire topology uses migration service, API, and Blazor readiness dependencies.
- Setup secret is generated and logged if no env secret exists and setup is active.
- API health endpoints include `/health`, `/alive`, `/metrics`.

### `docs/TROUBLESHOOTING.md`

Key facts:

- Setup secret checks include `/setup`, BFF setup endpoints, stripped direct client header, and 60-minute generated secret timeout.
- Tenant resolution order: trusted `X-Tenant-Slug`, custom domain, subdomain.
- Unresolved MultiTenant request returns 404.

### `docs/PROJECT.md`

Key facts:

- ISLAMU Event is an open-source event discovery and management platform with self-hosting support.
- Current implemented scope includes event lifecycle, organizations, lookup-driven filtering, multi-tenant runtime support, Blazor BFF, runtime authorization provider, HAL/HATEOAS, and OpenAPI client generation.
- Deployment mode is currently runtime-governed.

## Verified Source Files and Behaviors

### `Explore.Infrastructure/Services/DeploymentModeProvider.cs`

- Class: `DeploymentModeProvider : IDeploymentModeProvider`.
- `GetCurrentModeAsync()` reads cached/persisted bootstrap state.
- Pre-onboarding null/incomplete bootstrap returns `DeploymentMode.SingleTenant`.
- Post-onboarding parses `bootstrap.SelectedDeploymentMode`.
- Corrupted persisted value falls back to `MultiTenant`.
- `GetConfiguredOnboardingModeAsync()` calls `ResolveConfiguredMode()`.
- `ResolveConfiguredMode()` checks `_configuration["Deployment:Mode"]`; otherwise uses options.
- `TryParseDeploymentMode()` normalizes underscores/hyphens/spaces and ignores case.

### `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`

- Forces `request.Settings.DeploymentMode` from configured onboarding mode.
- Ensures default tenant in SingleTenant.
- Creates/uses onboarding user and actor.
- Persists deployment mode setting.
- Persists instance name.
- Assigns Platform Admin.
- Assigns default tenant admin in SingleTenant.
- Creates/updates `InstanceBootstrapState`.
- Invalidates caches.
- Reloads JWT authority.
- Locks setup secret.

### `Explore.Infrastructure/Services/SetupSecretProvider.cs`

- Reads `SETUP_SECRET` or generates a 32-character crypto-random token.
- Generated setup token times out after 60 minutes.
- `ValidateSecret()` rejects locked/timed-out/missing state and uses fixed-time comparison.
- `Lock()` marks setup complete.
- `GetSecretForLogging()` returns generated secret but not env-provided secret.
- `InitializeAsync()` reads bootstrap state and fails open to setup-incomplete on repository errors.

### `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`

- Exempts `/api/InstanceOnboarding` and `/api/System`.
- SingleTenant sets default tenant from `DeploymentSettings.DefaultTenantId` or fallback GUID.
- MultiTenant resolves by trusted `X-Tenant-Slug`, then host/domain/subdomain.
- Unresolved non-API-key request returns 404 ProblemDetails.

### `Explore.Blazor/Extensions/MiddlewareExtensions.cs`

- Startup redirect/auth gate sends auth entry paths to `/setup` while onboarding incomplete and no trusted setup-secret cookie exists.
- `OnboardingProtectedPaths` currently includes `/onboarding/instance` and `/onboarding/tenant`.
- `EnforceOnboardingAuthGateAsync()` protects onboarding GET paths.

### `Explore.Blazor.Client/Routes.razor`

- Routes include `/startup`, `/setup`, `/onboarding/auth-provider`, `/onboarding/authz-provider`, `/onboarding/instance`, `/onboarding/tenant`, `/organizations/create`, `/organization/success`.

### Route mismatch evidence

- `Explore.Blazor.Client/Routes.razor` defines `/organizations/create`.
- `Explore.Blazor.Client/Pages/Organizations/MyOrganizations.razor` links to `/organization/create`.
- `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor` navigates to `/organization/create`.
- Other files already use `/organizations/create`.

### `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor`

- Route: `/onboarding/instance`.
- `[Authorize]` and `InteractiveServer`.
- Uses `SetupLayout`.
- Uses `MudStepper` with “Instance Identity”, conditional “First Host Setup”, and “Review & Complete”.
- Shows first host choices: Personal, Quick Group, Formal Organization, Do Later.
- In SingleTenant, completion is disabled until a first-host choice exists.
- This conflicts with the requested convention-first onboarding behavior.

### `Explore.Blazor.Client/Services/InstanceOnboardingService.cs`

- `GetSystemOnboardingStatusAsync()` calls `api/system/onboarding-status`.
- `GetStatusAsync()` calls `api/InstanceOnboarding/status`.
- `ValidateSecretAsync()` calls `api/InstanceOnboarding/validate-secret` and maps 429/Gone/etc.
- `CompleteAsync()` calls `api/InstanceOnboarding/complete`.
- Service also contains broader governance operations, including deployment mode update and storage/SMTP tests.

### Verified controller/application files

- `Explore.API/Controllers/InstanceOnboardingController.cs` exists.
- `Explore.API/Controllers/TenantOnboardingController.cs` exists.
- `Explore.API/Controllers/SystemController.cs` exists in the worktree, but may be untracked/protected.
- `Explore.Application/Features/InstanceOnboarding/**` contains handlers/requests including system status and governance update work.
- `Explore.Application/Features/TenantOnboarding/**` contains tenant onboarding handlers/requests.
- `Explore.Application/DTOs/Onboarding/**` contains onboarding DTOs including `SystemOnboardingStatusDto.cs`, `InstanceOnboardingStatusDto.cs`, `CompleteInstanceOnboardingRequest.cs`, `AuthProviderConfigurationDto.cs`, `ResolverConfigurationDto.cs`, `PublicExperienceSettingsDto.cs`.

## Key Decisions

1. Default mode is SingleTenant with hidden internal default tenant.
2. MultiTenant is advanced operator mode only, enabled by `DEPLOYMENT_MODE=multi_tenant`.
3. Normal onboarding will not ask the user to select deployment mode.
4. Normal onboarding will not require first host selection before launch.
5. SingleTenant UI should not use tenant language.
6. Site Profile is an Application/onboarding model, not a Domain scope.
7. Preflight checks should be severity-based: block critical setup issues, warn for optional operational maturity.
8. Setup secret recovery improves operator guidance without weakening secret handling.

## Protected Worktree Warning

The worktree is dirty and includes many protected/user changes. Implementation must run `git status --short` before editing and avoid touching unrelated files.

Known protected/user changes include, but are not limited to:

- `Explore.API/swagger.json`.
- `docs/CONFIGURATION.md` and other deployment/self-hosting docs.
- Many deleted and replacement migration files.
- Onboarding/deployment files already modified in current worktree.
- Untracked `SystemController`, `SystemOnboardingStatusDto`, and system onboarding query/handler files.
- Previous T2.3/T1.1/T1.2 implementation files.

## Quick Resume

1. Read this context file.
2. Read `convention-first-single-tenant-onboarding-plan.md`.
3. Start with Phase 0 tasks:
   - fix `/organization/create` route mismatch;
   - hide/redirect `/onboarding/tenant` in SingleTenant.
4. Before editing, run `git status --short` and identify protected user changes.
5. Keep implementation surgical and verify with relevant tests.
