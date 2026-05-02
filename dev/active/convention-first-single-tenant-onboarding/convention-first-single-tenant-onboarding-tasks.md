# Convention-First Single-Tenant Onboarding - Task Checklist

Last Updated: 2026-05-02

## Status Legend

- ⏳ Not started
- 🟡 In progress
- ✅ Complete
- ⚠️ Blocked / needs decision

## Phase 0: Safety Fixes and Route Hygiene ⏳ NOT STARTED

### T0.1 Fix organization create route mismatch

- [ ] Replace `/organization/create` navigation/link usages with the canonical `/organizations/create` route or deliberately rename the route everywhere.
- [ ] Add route/link regression coverage for organization create navigation.
- [ ] Verify onboarding “Formal Organization” path reaches the real route.

Acceptance criteria:

- [ ] `Explore.Blazor.Client/Routes.razor` and all navigation sources agree on the same organization-create URL.
- [ ] No user path lands on a missing route.
- [ ] Tests fail if the stale URL is reintroduced.

Dependencies: none.

Effort: S.

Skills: `blazor-ui-conventions`.

### T0.2 Hide or redirect tenant onboarding in SingleTenant

- [ ] Add SingleTenant guard for `/onboarding/tenant` in BFF/client routing.
- [ ] Ensure MultiTenant operator mode can still reach tenant onboarding.
- [ ] Add tests for SingleTenant hidden/redirect behavior and MultiTenant allowed behavior.

Acceptance criteria:

- [ ] SingleTenant users do not see “tenant onboarding” as a first-run concept.
- [ ] Direct navigation to `/onboarding/tenant` in SingleTenant redirects or returns a clear not-applicable path.
- [ ] MultiTenant behavior remains available when `DEPLOYMENT_MODE=multi_tenant`.

Dependencies: T1.1 can refine final behavior.

Effort: M.

Skills: `blazor-bff-patterns`, `auth-patterns`.

## Phase 1: Deployment Mode Policy ⏳ NOT STARTED

### T1.1 Make deployment mode operator-controlled

- [ ] Refine `DeploymentModeProvider` so absent deployment mode defaults to SingleTenant.
- [ ] Treat `DEPLOYMENT_MODE=multi_tenant` as the explicit MultiTenant opt-in.
- [ ] Define invalid-value behavior: fail fast with operator-readable diagnostics or safely resolve SingleTenant during setup.
- [ ] Remove or lock normal runtime admin deployment-mode switching unless MultiTenant operator mode explicitly enables it.
- [ ] Add tests for absent env, explicit `multi_tenant`, explicit `single_tenant`, invalid value, and normalization variants.

Acceptance criteria:

- [ ] Default launch is SingleTenant without env configuration.
- [ ] MultiTenant onboarding appears only with explicit operator config.
- [ ] Instance onboarding completion cannot be overridden by a user-selected mode.
- [ ] Configuration docs and tests match implementation.

Dependencies: none.

Effort: M.

Skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`.

### T1.2 Remove deployment-mode choice from standard onboarding UI

- [ ] Remove normal deployment-mode picker/choice from onboarding pages.
- [ ] Show configured mode as operator status only.
- [ ] Use SingleTenant “site setup” language by default.
- [ ] Show MultiTenant-specific language only when operator mode is enabled.

Acceptance criteria:

- [ ] Typical self-hoster is not asked to choose deployment mode.
- [ ] UI explains mode as preconfigured by the operator.
- [ ] SingleTenant path avoids tenant language.

Dependencies: T1.1.

Effort: M.

Skills: `blazor-ui-conventions`.

## Phase 2: Application Onboarding Model and Defaults ⏳ NOT STARTED

### T2.1 Add Site Profile / SelfHostOnboardingProfile

- [ ] Add Application-owned request/DTO/model for minimal Site Profile.
- [ ] Include site name, support/contact email, canonical URL/domain if known, locale/time zone, and optional intent/purpose.
- [ ] Add validation using project validator conventions.
- [ ] Ensure the concept does not enter Domain as a scope/entity.

Acceptance criteria:

- [ ] Site Profile exists in Application/onboarding layer.
- [ ] No `Workspace`, `SubTenant`, `OrganizationScope`, or tenant-scope drift is introduced.
- [ ] Tests cover validation and default behavior.

Dependencies: T1.1.

Effort: M.

Skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`.

### T2.2 Apply convention defaults during instance completion

- [ ] Update completion flow to apply SingleTenant defaults automatically.
- [ ] Ensure default tenant creation remains internal.
- [ ] Apply public-experience defaults: discovery-centric mode, event catalog label, safe CTAs/home defaults.
- [ ] Defer first organization/group creation unless advanced/explicit.
- [ ] Lock setup secret only after all required writes succeed.

Acceptance criteria:

- [ ] Completion requires minimal profile input.
- [ ] New site is usable immediately after launch.
- [ ] Admin receives Platform Admin and default-tenant admin roles as appropriate.
- [ ] Public experience is configured through settings/config records, not display DTO persistence.

Dependencies: T2.1.

Effort: L.

Skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`.

### T2.3 Add onboarding preflight read model

- [ ] Add Application query/read model for preflight.
- [ ] Return blocking checks and warning checks separately.
- [ ] Cover setup secret, database/migrations, deployment mode, default tenant, auth config, canonical host/domain.
- [ ] Warn on SMTP, object storage, backups, logs/metrics/health, public exposure/search/signups.
- [ ] Add API endpoint and tests.

Acceptance criteria:

- [ ] Preflight provides clear, actionable status.
- [ ] Critical launch blockers are distinguished from operational warnings.
- [ ] Checks are deterministic in tests.

Dependencies: T1.1, T2.1.

Effort: L.

Skills: `clean-architecture-rules`, `dotnet-efcore-guidelines`, `auth-patterns`.

## Phase 3: Setup Secret and Operator Recovery ⏳ NOT STARTED

### T3.1 Improve setup secret expired/locked/recovery UX

- [ ] Add clearer setup-secret status to setup/onboarding status APIs if needed.
- [ ] Improve UI for expired generated secret, env-provided secret, locked/completed setup, and invalid secret states.
- [ ] Document how to rotate/reissue setup secrets safely.
- [ ] Preserve BFF header stripping and trusted forwarding.

Acceptance criteria:

- [ ] Expired setup mode tells the operator what to do next.
- [ ] Locked setup state does not look like a generic failure.
- [ ] No setup secret is leaked through client-controlled headers or browser storage.

Dependencies: none.

Effort: M.

Skills: `auth-patterns`, `blazor-bff-patterns`.

## Phase 4: API Surface and HATEOAS ⏳ NOT STARTED

### T4.1 Stabilize convention-first onboarding endpoints

- [ ] Review `SystemController`, `InstanceOnboardingController`, and onboarding route names.
- [ ] Add/refine preflight endpoint.
- [ ] Ensure setup endpoints are `[AllowAnonymous]` only where intended and still setup-secret/rate-limit protected.
- [ ] Ensure tenant onboarding API is not confusingly usable in SingleTenant.
- [ ] Add HATEOAS links for onboarding actions if the UI needs affordance-driven behavior.

Acceptance criteria:

- [ ] API exposes safe setup status and preflight data.
- [ ] Completion write path remains protected.
- [ ] SingleTenant API behavior matches SingleTenant UI behavior.

Dependencies: T2.3.

Effort: M.

Skills: `auth-patterns`, `clean-architecture-rules`.

## Phase 5: Blazor Convention-First Wizard ⏳ NOT STARTED

### T5.1 Rebuild first-run flow

- [ ] Reshape wizard to: Setup Secret → Admin Auth → Site Profile → Preflight → Launch/Handoff.
- [ ] Use `EditForm`, `DataAnnotationsValidator`, `ValidationSummary`, `ValidationMessage`, and `OnValidSubmit` for validated steps.
- [ ] Remove mandatory first-host selection from the normal path.
- [ ] Move organization/group creation to optional advanced or post-launch action.
- [ ] Replace SingleTenant “tenant” labels with “site” or “instance”.
- [ ] Add launch/handoff page with next actions.

Acceptance criteria:

- [ ] A typical self-hoster can complete setup without understanding tenancy.
- [ ] Wizard prioritizes first successful launch over full configuration.
- [ ] UI follows Blazor/MudBlazor project conventions.

Dependencies: T1.2, T2.1, T2.2, T2.3, T3.1.

Effort: XL.

Skills: `blazor-ui-conventions`, `blazor-bff-patterns`, `design-system`.

## Phase 6: Persistence and Migrations ⏳ NOT STARTED

### T6.1 Decide persistence shape after Application/API design

- [ ] Prefer existing `InstanceBootstrapState`, governance settings, and public-experience settings.
- [ ] Add migration only if durable Site Profile/preflight/handoff state cannot fit existing structures.
- [ ] Keep migration small and focused if needed.
- [ ] Preserve tenant filters and setup/bootstrap safety.

Acceptance criteria:

- [ ] No unnecessary schema change.
- [ ] Any required migration is focused and tested.
- [ ] Persistence does not store UI display DTOs or raw query strings.

Dependencies: T2.1, T2.2, T2.3.

Effort: XS-L depending on persistence decision.

Skills: `dotnet-efcore-guidelines`.

## Phase 7: Tests, Docs, and Operations ⏳ NOT STARTED

### T7.1 Add full regression coverage

- [ ] Application unit tests for deployment policy, completion defaults, preflight.
- [ ] API integration tests for SingleTenant default, MultiTenant env opt-in, setup-secret states, tenant onboarding guard.
- [ ] Blazor tests for wizard steps, route mismatch, setup-secret recovery states, launch handoff.
- [ ] Architecture tests for no scope/model drift.

Acceptance criteria:

- [ ] Relevant test projects pass with TUnit-compatible invocation.
- [ ] Release build passes.
- [ ] Known unrelated failures are documented if encountered.

Dependencies: implementation phases.

Effort: L.

Skills: `clean-architecture-rules`, `auth-patterns`, `blazor-ui-conventions`.

### T7.2 Update operator and self-hosting docs

- [ ] Update `docs/CONFIGURATION.md`.
- [ ] Update `docs/DEPLOYMENT_MODES.md`.
- [ ] Update `docs/SELF_HOSTING.md`.
- [ ] Update `docs/OPERATIONS.md`.
- [ ] Update `docs/TROUBLESHOOTING.md`.

Acceptance criteria:

- [ ] Docs say SingleTenant is default.
- [ ] Docs say `DEPLOYMENT_MODE=multi_tenant` is the explicit MultiTenant opt-in.
- [ ] Docs explain setup-secret recovery, preflight blockers/warnings, and launch handoff.
- [ ] Docs do not ask normal self-hosters to pick a tenant mode in the wizard.

Dependencies: implementation phases.

Effort: M.

Skills: `agentic-research`, `auth-patterns`.

## Verification Checklist

Run after implementation slices as applicable:

- [ ] `lsp_diagnostics` on modified C# and Razor files.
- [ ] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal`
- [ ] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal`
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal`
- [ ] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal`
- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal`
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `git diff --check`

## Quick Resume

1. Read `convention-first-single-tenant-onboarding-context.md`.
2. Read `convention-first-single-tenant-onboarding-plan.md`.
3. Run `git status --short` and protect unrelated user changes.
4. Start with T0.1 and T0.2.
5. Keep implementation aligned to convention-over-configuration: SingleTenant by default, MultiTenant only by explicit environment/secret opt-in.
