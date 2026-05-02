# Convention-First Single-Tenant Onboarding - Implementation Plan

Last Updated: 2026-05-02

## Executive Summary

Replace the current fragmented first-run experience with a convention-first onboarding path optimized for the fastest self-hosted time-to-value. The platform should launch in `SingleTenant` mode by default, hide internal tenant mechanics from normal self-hosters, and expose `MultiTenant` only when an operator explicitly sets `DEPLOYMENT_MODE=multi_tenant` through environment/secret configuration.

Backward compatibility is not required because the repository is in development mode. This plan intentionally favors clean replacement of confusing flows over compatibility shims: remove user-facing deployment-mode choice from normal onboarding, hide or redirect tenant onboarding in SingleTenant, fix route mismatches, apply smart defaults, and turn advanced configuration into optional post-launch tuning.

## Goals

1. Make SingleTenant the convention and only normal launch path.
2. Make MultiTenant an explicit operator mode activated only by `DEPLOYMENT_MODE=multi_tenant`.
3. Reduce required first-run decisions to the minimum needed to launch a useful site.
4. Keep tenant implementation internal in SingleTenant; use “site”, “instance”, and “public experience” language in UI.
5. Preserve Clean Architecture boundaries and BFF security rules.
6. Use smart defaults and preflight checks to reach a successful launch quickly.

## Non-Goals

1. No new Domain “workspace”, “organization scope”, or “subtenant” model.
2. No browser-side token handling or setup-secret exposure.
3. No user-facing deployment mode picker in the standard wizard.
4. No broad rewrite of tenancy persistence unless a focused migration becomes necessary.
5. No compatibility preservation for old onboarding route behavior beyond deliberate redirects/aliases where useful.

## Current State Analysis

### Product and architecture context

- `docs/PROJECT.md` describes ISLAMU Event as an open-source event discovery and management platform with self-hosting support and a Blazor BFF architecture.
- `docs/ARCHITECTURE.md` documents Clean Architecture boundaries: Domain, Application, Persistence/Infrastructure, API, and Blazor composition roots.
- `docs/DOMAIN.md` confirms tenant-aware entities use `TenantId` with global filters, and `Actor` represents exactly one owner shape: user, organization, or group. Onboarding intent must not become a new Domain scope.
- `docs/SECURITY.md` confirms the BFF owns OIDC/session cookies and API token forwarding; `Explore.Blazor.Client` must not manage tokens.

### Deployment mode behavior

Verified files:

- `Explore.Infrastructure/Services/DeploymentModeProvider.cs`
  - `GetCurrentModeAsync()` reads persisted bootstrap state after onboarding.
  - Before onboarding, missing/incomplete bootstrap returns `DeploymentMode.SingleTenant` so setup endpoints remain reachable.
  - `GetConfiguredOnboardingModeAsync()` resolves `Deployment:Mode` from configuration/options.
  - `TryParseDeploymentMode()` normalizes underscores/hyphens/spaces and ignores case.
- `Explore.API/Extensions/ConfigurationExtensions.cs`
  - `DEPLOYMENT_MODE` maps to `Deployment:Mode` and normalizes values such as `single_tenant` and `multi_tenant`.
- `docs/CONFIGURATION.md`
  - Documents default `Deployment:Mode` as `SingleTenant` and `DEPLOYMENT_MODE=multi_tenant` as the operator escape hatch.

Current gap: after onboarding, runtime mode can still be treated as a governance/admin setting. For the desired convention-first model, deployment mode should be operator-controlled, not a normal admin/user wizard decision.

### Instance onboarding behavior

Verified files:

- `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`
  - Forces `request.Settings.DeploymentMode` from `IDeploymentModeProvider.GetConfiguredOnboardingModeAsync()`.
  - Ensures the default tenant in SingleTenant via `EnsureDefaultTenantAsync()`.
  - Creates or uses the onboarding user and actor.
  - Persists deployment mode as a locked `SystemSetting` under `GovernanceSettingKeys.Deployment.Mode`.
  - Assigns Platform Admin and default-tenant admin in SingleTenant.
  - Creates/updates `InstanceBootstrapState`, invalidates caches, reloads JWT authority, and locks the setup secret.
- `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor`
  - Route: `/onboarding/instance`.
  - Uses `MudStepper` with “Instance Identity”, conditional “First Host Setup”, and “Review & Complete”.
  - Requires a first-host choice in SingleTenant: Personal, Quick Group, Formal Organization, or Do Later.
  - Shows deployment mode notice and calls system/status/branding APIs through `IInstanceOnboardingService`.
- `Explore.Blazor.Client/Services/InstanceOnboardingService.cs`
  - Calls `api/system/onboarding-status`, `api/InstanceOnboarding/status`, `api/InstanceOnboarding/validate-secret`, and `api/InstanceOnboarding/complete`.
  - Also contains broader governance/admin operations such as deployment mode update, storage/SMTP tests, analytics/footer governance, and auth provider setup.

Current gap: the UI asks a typical self-hoster to make first-host decisions before launch. This conflicts with convention-over-configuration. The first run should collect a minimal Site Profile and apply conventions automatically, with “customize later” as the default.

### Setup secret and startup gate

Verified files:

- `Explore.Infrastructure/Services/SetupSecretProvider.cs`
  - Reads `SETUP_SECRET` or generates a random setup token.
  - Generated setup tokens expire after 60 minutes.
  - `ValidateSecret()` fails closed for locked/timed-out/missing secrets and uses fixed-time comparison.
  - `Lock()` marks setup complete.
  - `GetSecretForLogging()` returns generated secrets for operator visibility but not env-provided secrets.
- `Explore.Blazor/Extensions/MiddlewareExtensions.cs`
  - Redirects startup/auth entry paths to `/setup` while onboarding is incomplete.
  - Protects onboarding paths with the setup/auth gate.
  - Current protected paths include `/onboarding/instance` and `/onboarding/tenant`.
- `docs/TROUBLESHOOTING.md`
  - Documents setup secret checks and the 60-minute auto-generated secret timeout.

Current gap: the security mechanics are strong, but expired/locked/recovery states are not a productized self-hoster recovery journey. The setup screen should explain how to rotate/reissue a setup secret and how to continue safely.

### Tenant resolution behavior

Verified files:

- `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`
  - Exempts `/api/InstanceOnboarding` and `/api/System` from normal tenant resolution.
  - In SingleTenant, sets the default tenant from `DeploymentSettings.DefaultTenantId` or fallback `018e4e5c-7f00-7000-8000-000000000001`.
  - In MultiTenant, resolves by trusted `X-Tenant-Slug`, then custom domain/subdomain.
  - Returns 404 ProblemDetails when MultiTenant tenant cannot be resolved.
- `docs/API.md`
  - Documents middleware order with tenant resolution before authentication and final API-key binding after authentication.

Current gap: SingleTenant correctly uses an internal default tenant, but UI/routes still expose tenant-onboarding language and paths. This should be hidden or redirected in SingleTenant.

### Routing and UX issues

Verified files:

- `Explore.Blazor.Client/Routes.razor`
  - Defines `/setup`, `/onboarding/instance`, `/onboarding/tenant`, and `/organizations/create`.
- `Explore.Blazor.Client/Pages/Organizations/MyOrganizations.razor`
  - Links to `/organization/create` in at least two places.
- `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor`
  - Navigates to `/organization/create` after Formal Organization choice.
- Other verified routes use `/organizations/create` correctly.

P0 gap: `/organization/create` and `/organizations/create` are inconsistent. In development mode, fix the route and add guard tests instead of preserving both as permanent behavior.

## Research Findings

### Tavily research

Search: “2026 self hosted software onboarding first run setup wizard convention over configuration bootstrap admin setup secret deployment defaults best practices”.

Relevant findings:

- Value-first onboarding should minimize signup/setup friction.
- Smart defaults, templates, and pre-populated content reduce time-to-first-success.
- First-run flows should guide users to the first meaningful win, then celebrate/confirm success.
- Onboarding content and setup flows must remain tested, secured, and versioned.

Repo interpretation: ISLAMU Event should not ask normal self-hosters to understand tenancy, deployment mode, or first-host ownership before launch. It should infer sensible defaults, apply them, and provide a handoff checklist.

### Context7 documentation

ASP.NET Core docs (`/websites/learn_microsoft_en-us_aspnet_core`) confirmed:

- Default configuration includes appsettings, environment variables, and command line.
- Environment variables override appsettings by default.
- Blazor forms use `EditForm`, `DataAnnotationsValidator`, `ValidationSummary`, `ValidationMessage`, and `OnValidSubmit` patterns for validated forms.

Repo interpretation: `DEPLOYMENT_MODE=multi_tenant` is an appropriate explicit operator escape hatch. The wizard should use standard validated form patterns instead of ad hoc validation logic where possible.

## Proposed Future State

### First-run product journey

Normal SingleTenant journey:

```text
Setup Secret → Admin Auth → Site Profile → Smart Defaults Applied → Preflight → Launch/Handoff
```

The self-hoster should experience the platform as “set up my site”, not “configure tenancy”. The platform still uses the internal default tenant for data isolation, but SingleTenant UI should use “Site”, “Instance”, and “Public Experience”.

### SingleTenant default

- If `DEPLOYMENT_MODE` is absent, invalid, or not explicitly `multi_tenant`, the first-run path is SingleTenant.
- The normal wizard never asks the user to choose SingleTenant or MultiTenant.
- The default internal tenant is created/ensured automatically.
- The default tenant is not described as “tenant” in SingleTenant UI.

### MultiTenant operator mode

- MultiTenant is enabled only by explicit operator configuration: `DEPLOYMENT_MODE=multi_tenant`.
- When enabled, the operator sees advanced multi-tenant onboarding and tenant onboarding routes.
- If not enabled, `/onboarding/tenant` is hidden and redirected/blocked.
- Deployment mode switching from runtime admin UI is removed or locked unless MultiTenant operator mode is explicitly enabled.

### Site Profile

Replace required first-host decisions with a minimal Site Profile:

- Site/instance display name.
- Public contact/support email if available.
- Canonical public URL/domain when known.
- Locale/time zone defaults.
- Optional site purpose selector with safe default.

Recommended Application-level concept: `SelfHostOnboardingProfile` or `SiteProfile`. This is an Application/config/onboarding concept, not a Domain entity and not a tenant scope.

### Smart defaults

Based on Site Profile and SingleTenant convention:

- Set default public experience mode to discovery-centric unless an advanced user opts into organization-centric setup.
- Use “Events” as the public catalog label.
- Allow `/events` or the public home page to work immediately.
- Defer organization/group creation unless clearly needed.
- Use existing public-experience settings and governance settings rather than adding new Domain ownership concepts.

### Preflight

Add a preflight read model with severity:

Blocking checks:

- Setup secret valid and setup mode active.
- Database reachable and migrations applied.
- Deployment mode resolved and consistent with operator config.
- Default tenant exists in SingleTenant.
- Required auth provider/runtime config is valid enough to create/sign in the admin.
- Canonical host/domain exists if required by configured runtime.

Warning checks:

- SMTP not configured.
- Object storage not configured.
- Backups not configured/verified.
- Logs/metrics/health endpoint visibility not checked.
- Public exposure/search/signups settings may need review.

### Launch and handoff

After completion:

- Show what was created: admin, site profile, default tenant internally, public URL, public experience defaults.
- Provide next recommended actions: create first event, invite teammate, configure SMTP, set domain/TLS, verify backups.
- Link to admin settings with HAL/API-backed affordances where available.

## Implementation Phases

### Phase 0 - P0 Safety and Route Corrections

Layer focus: Blazor Client, Blazor BFF, tests.

1. Fix `/organization/create` vs `/organizations/create` route mismatch.
2. Add route/link tests so onboarding and organization pages only generate valid routes.
3. Add SingleTenant guard for `/onboarding/tenant` in the BFF startup/onboarding gate.
4. Ensure MultiTenant operator mode still exposes tenant onboarding.

Acceptance:

- No navigation/link points to a non-existent organization create route.
- In SingleTenant, `/onboarding/tenant` redirects to `/onboarding/instance`, `/setup`, or a clear not-applicable page.
- In MultiTenant, tenant onboarding remains reachable when configured.

### Phase 1 - Deployment Mode Policy Baseline

Layer focus: Infrastructure, Application contracts, API tests.

1. Refine `DeploymentModeProvider` semantics:
   - `DEPLOYMENT_MODE=multi_tenant` explicitly enables MultiTenant.
   - absent config resolves to SingleTenant.
   - invalid values fail with clear diagnostics or safely resolve to SingleTenant during setup, depending on chosen operator policy.
2. Remove normal UI/admin runtime switching for deployment mode.
3. Keep persisted deployment mode as bootstrap/runtime state, but treat it as operator-governed.
4. Add tests for env precedence, absent env default, invalid env behavior, and persisted-state behavior.

Acceptance:

- SingleTenant is default with no environment variable.
- `DEPLOYMENT_MODE=multi_tenant` is the only normal way to enter MultiTenant onboarding.
- Admin UI cannot casually flip deployment mode in standard SingleTenant operation.

### Phase 2 - Application Onboarding Models and Defaults

Layer focus: Application.

1. Add Application-owned `SiteProfile`/`SelfHostOnboardingProfile` DTO/model.
2. Add validated command/request shape for completing convention-first onboarding.
3. Refactor `CompleteInstanceOnboardingCommandHandler` so it:
   - reads configured deployment mode from `IDeploymentModeProvider`, not user choice;
   - ensures default tenant in SingleTenant;
   - creates/uses admin user and actor;
   - applies site profile and public-experience defaults;
   - optionally creates first organization/group only from advanced/explicit intent;
   - locks setup secret after successful completion.
4. Add preflight read model and query handler.
5. Keep validators manually instantiated per repo convention.

Acceptance:

- Normal completion needs only setup/auth context and minimal Site Profile.
- Default tenant creation remains internal and tenant-safe.
- No new Domain scope concept is introduced.
- Public-experience defaults use settings/config records, not display DTOs or raw query strings.

### Phase 3 - Infrastructure and Configuration

Layer focus: Infrastructure, configuration extensions, operations.

1. Ensure environment mapping for `DEPLOYMENT_MODE` is explicit and documented in code/tests.
2. Add setup-secret recovery/reissue guidance mechanics:
   - no secret leakage to browser;
   - operator-safe instructions for env secret rotation or generated-secret restart behavior;
   - clear expired/locked states.
3. Implement preflight infrastructure probes where needed:
   - database/migration state;
   - auth provider/runtime config;
   - canonical host/domain check;
   - optional SMTP/storage/backups/logging/health warnings.
4. Keep API/BFF setup-secret forwarding rules intact.

Acceptance:

- Preflight checks are deterministic and severity-tagged.
- Setup secret recovery UX explains operator action without weakening security.
- No direct browser token or setup-secret handling is introduced.

### Phase 4 - API Surface

Layer focus: API controllers, route names, HATEOAS.

1. Stabilize `SystemController` and `InstanceOnboardingController` around convention-first status/preflight/complete flows.
2. Add or refine preflight endpoint.
3. Ensure setup endpoints keep setup-secret rate limiting and `[AllowAnonymous]` only where intended.
4. Hide/redirect/disable tenant onboarding endpoint behavior in SingleTenant where route-level UI protection is insufficient.
5. Update route names and HAL policies if new onboarding actions are exposed.

Acceptance:

- Public setup/status endpoints expose only safe setup state.
- Write operations require setup secret and/or authenticated admin as appropriate.
- SingleTenant tenant-onboarding API cannot be used to create a confusing second onboarding flow.

### Phase 5 - Blazor/BFF Onboarding UX

Layer focus: Blazor BFF, Blazor Client, design system.

1. Replace fragmented wizard steps with:
   - Setup Secret;
   - Admin Auth;
   - Site Profile;
   - Preflight;
   - Launch/Handoff.
2. Use `EditForm`, `DataAnnotationsValidator`, `ValidationSummary`, `ValidationMessage`, and `OnValidSubmit` for validated form steps.
3. Remove normal deployment-mode choice from UI.
4. Replace “tenant” language with “site/instance” in SingleTenant paths.
5. Move first-host choices behind an optional “Advanced: create organization/group now” panel or remove from first-run entirely.
6. Improve setup-secret expired/locked/recovery UI.
7. Ensure BFF continues to strip inbound `X-Setup-Secret` and forward only trusted setup secret context.

Acceptance:

- A typical self-hoster can complete onboarding with minimal decisions.
- No SingleTenant page requires understanding tenant concepts.
- Launch page directs to first useful action: create first event, configure domain, invite collaborator, or view public site.

### Phase 6 - Persistence and Migrations

Layer focus: Persistence.

1. Prefer existing `InstanceBootstrapState`, governance settings, and public-experience settings for persisted onboarding profile data.
2. If new persisted fields are needed, add a focused migration only after Application/API shape is final.
3. Do not store raw query strings or UI display DTOs as durable public-experience config.
4. Preserve tenant query filters and setup/bootstrap safety.

Acceptance:

- No unnecessary schema change.
- Any required migration is small, focused, and covered by tests.
- Tenant isolation remains enforced by existing filters.

### Phase 7 - Tests, Documentation, and Operations

Layer focus: all layers.

1. Update tests across Application, API integration, Blazor client/integration, and architecture projects.
2. Update docs:
   - `docs/CONFIGURATION.md`;
   - `docs/DEPLOYMENT_MODES.md`;
   - `docs/SELF_HOSTING.md`;
   - `docs/OPERATIONS.md`;
   - `docs/TROUBLESHOOTING.md`.
3. Add quick-start operator guidance for SingleTenant default and MultiTenant escape hatch.
4. Add verification commands to the implementation PR notes.

Acceptance:

- Docs match actual configuration semantics.
- Tests prove SingleTenant default, MultiTenant env opt-in, route correctness, setup-secret states, and preflight severity.

## Detailed Task Breakdown

### T0.1 - Fix organization create route mismatch

- Layer: Blazor Client.
- Files: `Explore.Blazor.Client/Pages/Organizations/MyOrganizations.razor`, `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor`, `Explore.Blazor.Client/Routes.razor`.
- Acceptance:
  - All links/navigation use `/organizations/create` or a deliberately renamed single route.
  - Tests fail if `/organization/create` is reintroduced accidentally.
- Dependencies: none.
- Effort: S.
- Skills: `blazor-ui-conventions`.

### T0.2 - Hide or redirect tenant onboarding in SingleTenant

- Layer: Blazor BFF, Blazor Client, API if needed.
- Files: `Explore.Blazor/Extensions/MiddlewareExtensions.cs`, `Explore.Blazor.Client/Routes.razor`, `Explore.Blazor.Client/Pages/Onboarding/TenantOnboarding.razor`, `Explore.API/Controllers/TenantOnboardingController.cs`.
- Acceptance:
  - SingleTenant users cannot enter a confusing tenant onboarding path.
  - MultiTenant operator mode keeps tenant onboarding reachable.
  - Integration tests cover both modes.
- Dependencies: T1.1 deployment-mode policy may refine the exact mode check.
- Effort: M.
- Skills: `blazor-bff-patterns`, `auth-patterns`.

### T1.1 - Make deployment mode operator-controlled

- Layer: Infrastructure/Application.
- Files: `Explore.Infrastructure/Services/DeploymentModeProvider.cs`, `Explore.Application/Contracts/Services/IDeploymentModeProvider.cs`, `Explore.API/Extensions/ConfigurationExtensions.cs`, relevant tests.
- Acceptance:
  - absent `DEPLOYMENT_MODE` means SingleTenant;
  - explicit `DEPLOYMENT_MODE=multi_tenant` means MultiTenant;
  - normal onboarding request cannot override configured mode;
  - runtime admin UI cannot switch mode unless advanced operator mode explicitly permits it.
- Dependencies: none.
- Effort: M.
- Skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`.

### T1.2 - Remove deployment-mode choice from onboarding UI

- Layer: Blazor Client.
- Files: `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor`, `Explore.Blazor.Client/Services/InstanceOnboardingService.cs`.
- Acceptance:
  - wizard shows “Single site setup” language by default;
  - MultiTenant mode is displayed only as operator-enabled advanced mode;
  - no required user choice for deployment mode.
- Dependencies: T1.1.
- Effort: M.
- Skills: `blazor-ui-conventions`.

### T2.1 - Add Site Profile request/read model

- Layer: Application.
- Files: new or existing `Explore.Application/DTOs/Onboarding/*`, `Explore.Application/Features/InstanceOnboarding/*`.
- Acceptance:
  - Site Profile is Application-owned;
  - it captures site name, contact/support email, canonical URL/domain if known, locale/time zone, and optional intent;
  - no Domain entity or new scope concept is introduced.
- Dependencies: T1.1.
- Effort: M.
- Skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`.

### T2.2 - Apply convention defaults during completion

- Layer: Application.
- Files: `CompleteInstanceOnboardingCommandHandler.cs`, public-experience/governance setting services.
- Acceptance:
  - completion applies site profile, default public experience, catalog label, and safe governance settings;
  - first organization/group creation is optional/advanced, not required;
  - setup secret locks only after all writes succeed.
- Dependencies: T2.1.
- Effort: L.
- Skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`.

### T2.3 - Add onboarding preflight read model

- Layer: Application/Infrastructure/API.
- Files: new preflight query/handler/DTO, infrastructure probes, API controller endpoint.
- Acceptance:
  - returns blocking and warning checks separately;
  - covers setup secret, DB/migrations, deployment mode, default tenant, auth config, canonical host;
  - warns on SMTP, object storage, backups, logs/metrics/health, public exposure.
- Dependencies: T1.1, T2.1.
- Effort: L.
- Skills: `clean-architecture-rules`, `dotnet-efcore-guidelines`, `auth-patterns`.

### T3.1 - Improve setup-secret recovery UX

- Layer: Infrastructure/API/Blazor.
- Files: `SetupSecretProvider.cs`, `InstanceOnboardingController.cs`, setup pages/services.
- Acceptance:
  - expired generated-secret state gives clear operator instructions;
  - env-provided secret rotation guidance is clear;
  - locked/completed setup state is explicit;
  - no secret is exposed to the browser beyond trusted setup flow.
- Dependencies: none.
- Effort: M.
- Skills: `auth-patterns`, `blazor-bff-patterns`.

### T4.1 - Rebuild the Blazor wizard around time-to-value

- Layer: Blazor Client/BFF.
- Files: `Setup.razor`, `StartupGate.razor`, `InstanceOnboarding.razor`, services, CSS as needed.
- Acceptance:
  - flow is Setup Secret → Admin Auth → Site Profile → Preflight → Launch;
  - form validation uses standard Blazor patterns;
  - optional advanced settings are collapsed/deferred;
  - launch page links to first useful actions.
- Dependencies: T2.1, T2.2, T2.3, T3.1.
- Effort: XL.
- Skills: `blazor-ui-conventions`, `blazor-bff-patterns`, `design-system` if styling changes.

### T5.1 - Update docs and operator guidance

- Layer: documentation/operations.
- Files: `docs/CONFIGURATION.md`, `docs/DEPLOYMENT_MODES.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, `docs/TROUBLESHOOTING.md`.
- Acceptance:
  - docs say SingleTenant is default;
  - docs say MultiTenant requires explicit `DEPLOYMENT_MODE=multi_tenant`;
  - setup-secret recovery and preflight outcomes are documented;
  - no docs tell normal self-hosters to pick a tenant mode in the wizard.
- Dependencies: implementation phases.
- Effort: M.
- Skills: `agentic-research`, `auth-patterns`.

## Testing Strategy

### Unit tests

- `Event.Application.UnitTests`
  - deployment-mode policy;
  - convention default application;
  - preflight severity logic;
  - command validation.
- `Event.Domain.UnitTests`
  - no new Domain scope/entity concepts if guardrails are needed.

### API integration tests

- `Event.API.IntegrationTests`
  - setup secret validation and rate limiting;
  - SingleTenant default completion;
  - MultiTenant env opt-in;
  - `/onboarding/tenant` API behavior in SingleTenant vs MultiTenant;
  - preflight endpoint.

### Blazor tests

- `Explore.Blazor.Client.Tests`
  - wizard renders correct steps;
  - route mismatch cannot regress;
  - setup-secret expired/locked states render;
  - Site Profile validation and launch handoff.
- `Explore.Blazor.IntegrationTests`
  - startup redirect and onboarding gate behavior;
  - BFF setup-secret forwarding behavior.

### Architecture tests

- `Event.Architecture.Tests`
  - no Domain reference leakage;
  - onboarding DTO naming and layer boundaries;
  - no new `Workspace`, `SubTenant`, `OrganizationScope`, or `ScopeId`-based ownership model.

### Verification commands

Use TUnit-compatible invocation where applicable:

```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal
dotnet build --configuration Release --verbosity quiet
```

## Risk Assessment and Mitigations

### Risk: MultiTenant path becomes accidentally broken

- Mitigation: keep explicit MultiTenant operator-mode tests and route/API integration coverage.
- Mitigation: treat MultiTenant as advanced but supported, not deleted.

### Risk: setup-secret recovery weakens security

- Mitigation: provide guidance and state transitions, not browser-exposed secrets.
- Mitigation: keep fixed-time comparison, rate limits, BFF header stripping, and lock-after-bootstrap.

### Risk: SingleTenant UI hides internal tenant mechanics but code still needs tenant context

- Mitigation: keep default tenant creation and API middleware behavior unchanged internally.
- Mitigation: only rename user-facing language, not persistence concepts.

### Risk: preflight false positives block launch

- Mitigation: severity-based checks; block only critical setup requirements, warn for optional operational maturity items.

### Risk: deployment-mode config ambiguity

- Mitigation: make env mapping tests explicit for absent, `single_tenant`, `multi_tenant`, invalid, and casing/underscore variants.

### Risk: dirty worktree hides unrelated changes

- Mitigation: implementation must inspect `git status` before each phase and touch only task-owned files.

## Success Metrics

1. A normal self-hoster can complete onboarding without choosing deployment mode or tenant concepts.
2. Required first-run decisions are reduced to setup secret/auth and minimal Site Profile.
3. SingleTenant launch creates/uses the internal default tenant automatically.
4. `DEPLOYMENT_MODE=multi_tenant` is the only documented operator opt-in to MultiTenant onboarding.
5. `/organization/create` route mismatch is eliminated and guarded by tests.
6. `/onboarding/tenant` is not visible/confusing in SingleTenant.
7. Preflight returns actionable blockers/warnings.
8. Release build and relevant tests pass.

## Resources and Dependencies

- Existing setup secret lifecycle: `SetupSecretProvider`.
- Existing deployment mode provider: `DeploymentModeProvider`.
- Existing onboarding command/status endpoints: `InstanceOnboardingController`, `CompleteInstanceOnboardingCommandHandler`.
- Existing system status endpoint worktree files: `SystemController`, `GetSystemOnboardingStatusQueryHandler`, `SystemOnboardingStatusDto`.
- Existing public-experience settings and governance setting infrastructure.
- OIDC/Keycloak or configured auth provider for admin authentication.
- PostgreSQL migrations and health checks.
- Blazor BFF/YARP token and setup-secret forwarding.

## Effort Estimate

- Phase 0: 0.5-1 day.
- Phase 1: 1-2 days.
- Phase 2: 2-4 days.
- Phase 3: 1-3 days.
- Phase 4: 1-2 days.
- Phase 5: 3-6 days.
- Phase 6: 0-2 days depending on persistence need.
- Phase 7: 1-2 days.

Total expected effort: 9-20 engineering days, depending on how much of preflight and Blazor wizard polish is included in the first slice.

## Recommended Implementation Order

1. Fix route mismatch and SingleTenant `/onboarding/tenant` confusion.
2. Lock deployment mode policy to env-driven operator mode.
3. Introduce Site Profile and convention defaults.
4. Add preflight read model.
5. Rebuild the wizard around the new flow.
6. Update docs and operations guidance.
