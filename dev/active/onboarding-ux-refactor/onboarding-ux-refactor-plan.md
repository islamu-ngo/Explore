<!-- ABOUTME: Repository-grounded implementation plan for a unified single-tenant and multi-tenant onboarding workspace. -->
<!-- ABOUTME: Preserves setup and authorization boundaries while unifying fragmented route-specific pages into one guided experience. -->

# Onboarding UX Refactor — Implementation Plan

> **Implemented foundation (2026-07-12):** Production Keycloak management, Local/Cerbos reconciliation, server-authoritative route skipping, HAL-gated task actions, and mode-specific launch handoffs exist with focused coverage. This foundation is behaviorally valuable but does not provide the unified onboarding workspace shown in the user prototype.
> **Scope expansion recorded:** The production fixes necessarily touch shared Application/Infrastructure provider services, API configuration and background reconciliation, AppHost/Compose propagation, generated contracts, and focused tests in addition to the original Blazor-first paths. This is the smallest shared-source change that makes both detected-provider behaviors authoritative.
> **Corrected planning re-baseline (2026-07-29):** The supplied current-state screenshots prove the presentation remains fragmented: pages share only a minimal layout, not a persistent journey model. The prototype establishes the missing target: one route-aware workspace with progress, focused step content, setup summary/help, and consistent navigation. Phase 9 plans that implementation; no runtime code changed during this planning pass.

Last Updated: 2026-07-29 Europe/Brussels

## 0. Planning Metadata

- **Request:** Refactor the current route-specific instance onboarding pages into the unified onboarding experience demonstrated by the supplied prototype while preserving SingleTenant/MultiTenant behavior and security boundaries.
- **Task directory:** `dev/active/onboarding-ux-refactor/`
- **Planning status:** Corrected and re-baselined — prototype direction is user-confirmed; unified workspace implementation has not started
- **Primary matched intent:** `external-infrastructure-bootstrap` — Automate external infrastructure bootstrap or onboarding
- **Relevant skills loaded:** `implementation-plan`, `shared/frontend` (`design/README.md`, `image-to-code-skill.md`, `redesign-skill.md`), `design-system`, `blazor-ui-conventions`, `blazor-css-isolation`, `auth-patterns`, `blazor-bff-patterns`, `clean-architecture-rules`, `accessibility`, `visual-qa`, `ponytail`
- **Relevant rules loaded:** `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/application-layer.md`, `.claude/rules/blazor-server.md`, `.claude/rules/blazor-client.md`, `.claude/rules/tests.md`
- **Layers touched by the new slice:** Application onboarding DTO/command/validator, API onboarding controller/route/HAL policy, generated client, Blazor Client layout/components/pages/services, Blazor BFF navigation boundaries, focused tests, `docs/DESIGN.md`, canonical Blazor/operator docs, and dev workstream records. Infrastructure/Persistence remain unchanged unless implementation proves the existing instance-settings repository cannot persist the non-secret profile draft.
- **Estimated complexity:** XL. The visual work is substantial and the prototype's honest save/resume contract requires one narrow server write across the pre-auth setup-secret → OIDC → authenticated administrator boundaries.
- **Implementation boundary:** Preserve all July behavior and tests. Do not redesign deployment-mode ownership, provider reconciliation, completion handlers, or tenant authority. Unrelated working-tree changes remain outside this workstream.

### 0.1 Primary Intent Contract

| Field | Contract |
|---|---|
| Intent | `external-infrastructure-bootstrap` — Automate external infrastructure bootstrap or onboarding |
| `must_read_docs` | `docs/QUICK_REFERENCE.md`; `docs/GOVERNANCE.md`; `docs/SECURITY-MODEL.md`; `docs/SECRETS.md`; `docs/CONFIGURATION.md`; `docs/SELF_HOSTING.md`; `docs/TESTING.md` |
| `load_skills` | `auth-patterns`; `blazor-bff-patterns`; `clean-architecture-rules` |
| `load_rules` | `.claude/rules/api-controllers.md`; `.claude/rules/application-layer.md`; `.claude/rules/blazor-server.md`; `.claude/rules/blazor-client.md`; `.claude/rules/tests.md` |
| `paths_in_scope` | `docker-compose.yml`; `docker/**`; `src/Explore.Application/Contracts/Services/**/*.cs`; `src/Explore.Application/DTOs/Onboarding/**/*.cs`; `src/Explore.Application/Features/InstanceOnboarding/**/*.cs`; `src/Explore.Infrastructure/Services/**/*.cs`; `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`; `src/Explore.API/Controllers/**/*Settings*.cs`; `src/Explore.API/Controllers/**/*Onboarding*.cs`; `src/Explore.API/Hateoas/RouteNames.cs`; `src/Explore.Blazor/**/*.cs`; `src/Explore.Blazor.Client/**/*.razor`; `src/Explore.Blazor.Client/Services/**/*.cs`; `Event.*Tests/**/*.cs`; `Explore.*Tests/**/*.cs`; `docs/CONFIGURATION.md`; `docs/SECRETS.md`; `docs/SELF_HOSTING.md`; `docs/TROUBLESHOOTING.md`; `dev/active/**` |
| `minimum_tests` | `Event.Application.UnitTests`; `Event.API.IntegrationTests`; `Explore.Blazor.IntegrationTests`; `Explore.Blazor.Client.Tests`; `Event.Architecture.Tests` |
| `docs_to_update` | `docs/CONFIGURATION.md`; `docs/SECRETS.md`; `docs/SELF_HOSTING.md`; `docs/TROUBLESHOOTING.md` |
| `unique_acceptance` | Bootstrap credentials are request/job scoped and never persisted; privileged browser headers are stripped and replaced only by the trusted BFF/server; provider admin responses, tokens, passwords, and secrets never enter logs, DTOs, traces, or support output; existing provider resources are repaired additively and destructive delete/reimport requires explicit approval; operator docs cover rerun, rotation, backup, failure recovery, and partial completion. |
| `forbidden_without_approval` | Persisting provider admin/service-account secrets for reuse; deleting/reimporting an existing realm/resource as repair; trusting browser-provided setup/admin headers; silently overriding deployment-managed secrets. |

### 0.2 Conditional Intent Reclassification

The implementation audit activated the following conditional contracts on 2026-07-12:

- `blazor-component-affordance`: active because onboarding action visibility is implemented in Blazor. Scope remains `src/Explore.Blazor.Client/**/*.razor`; minimum test project is `Explore.Blazor.Client.Tests`; mutation affordances must be gated by HAL link presence.
- `add-hal-link`: active because the existing instance and tenant onboarding status responses do not expose the accepted management/completion affordances. Scope is API HATEOAS policy/assembler code plus Blazor consumers; minimum test projects are `Event.API.IntegrationTests` and `Explore.Blazor.Client.Tests`; policies remain separate and use `yield return`, permission checks, and fail-closed omission.
- `openapi-contract-change`: active because the existing status responses became explicit HAL resources and the generated client preserves `_links`. Scope is the affected controllers, `docs/API_CHANGELOG.md`, generated OpenAPI/client artifacts, and contract tests; the user explicitly approved breaking pre-v1 development changes.
- `add-write-endpoint`: active for `PATCH /api/instance-onboarding/profile`; requires `[Authorize]`, setup-secret rate limiting, HAL exposure, idempotency consideration, `Event.API.IntegrationTests`, `Event.Architecture.Tests`, and `docs/API_CHANGELOG.md`.
- `add-cqrs-handler`: active for `SaveInstanceOnboardingProfileCommand`; scope stays in `Features/InstanceOnboarding`, returns `BaseCommandResponse<Guid>`, manually instantiates the profile validator, passes cancellation, avoids cross-feature internals, and requires Application plus Architecture tests.

The following conditional intents remain deferred unless later evidence activates them:

- `add-get-endpoint`: activates only if the evidence threshold for an aggregate onboarding snapshot is met.
- `add-write-endpoint` and `add-cqrs-handler`: activated for the accepted Save and exit/resume contract. Current profile fields persist only during final completion, while the post-launch branding write requires instance-admin authority that does not exist before launch. Add one onboarding-scoped, non-secret profile-draft write protected by `[Authorize]`, `[SetupSecretRequired]`, setup rate limiting, validation, and audit.
- `cerbos-policy-change`: activates only if policy semantics change; Cerbos configuration or package sync alone does not imply a policy change.

### 0.3 Re-baseline Classification — 2026-07-29

The corrected request activates a new `blazor-component-affordance` presentation slice plus one `add-write-endpoint`/`add-cqrs-handler` profile-draft slice under the existing `external-infrastructure-bootstrap` workstream. Existing backend authority is otherwise sufficient; the primary missing behavior is a cohesive journey shell and route-aware step navigation. The planning pass edits only `dev/active/onboarding-ux-refactor/`.

Before implementation, approval of this corrected plan must explicitly widen the current intent allow-list to the required visual paths: `docs/DESIGN.md`, existing/new `*.razor.css`, and focused Blazor test files. No existing intent fully describes a broad onboarding redesign; do not silently treat `blazor-component-affordance`'s `.razor` allow-list as CSS/design-doc authorization.

## 1. Executive Summary

The platform already has the secure and authoritative backend pieces needed for launch: a setup-secret BFF boundary, authentication and authorization provider configuration, instance status and preflight queries, idempotent completion handlers, tenant onboarding state, post-launch settings editors, RFC 7807 errors, and Cerbos/local authorization support. The screenshots show the remaining product problem: each route renders as an isolated page with different density and hierarchy, large unused canvas, no persistent progress, no stable Back/Continue model, and no contextual summary explaining where the operator is in the overall launch. The repository trace also found one functional gap implied by the prototype: site-profile input is persisted only by final completion, so pre-launch Save and exit/resume needs a narrow onboarding draft write.

The target is one visually continuous workspace spanning two security contexts without merging them:

1. `/setup` remains the pre-auth access gate protected by the setup secret.
2. After validation, authentication-provider setup enters the shared workspace under setup-secret authority.
3. OIDC performs the existing hard navigation and creates the BFF-authenticated session.
4. The same workspace presentation resumes post-auth for site profile, authorization, readiness review, and launch.

SingleTenant launch completes the instance and default-tenant setup, then hands off to events or existing settings. MultiTenant launch completes the platform first and hands off to the control plane; creating or onboarding a first tenant is a separate optional task with tenant-scoped authority. `DEPLOYMENT_MODE` and the dedicated admin host remain operator-owned deployment configuration, not onboarding choices.

The workspace composes existing status/provider/preflight endpoints and adds only `PATCH /api/instance-onboarding/profile` for non-secret profile draft persistence. A new aggregate snapshot endpoint remains deferred unless tests or traces demonstrate inconsistent multi-call state, a reproducible race, or unacceptable request amplification. Cerbos inventory and policy decision-test endpoints remain deferred.

### 1.1 Implemented Foundation — 2026-07-12

- Instance and tenant onboarding now use one semantic, responsive, display-only task-list component while parent pages retain state and workflow ownership.
- Existing instance and tenant status endpoints are HAL resources. Server policies emit permission/setup-secret-checked `complete` and management relations; Blazor exposes actions only when those relations are present.
- A configured Keycloak task stays complete and nonblocking while retaining **Manage authentication**. It opens `/onboarding/auth-provider` before launch and `/admin/instance/settings?section=auth-providers` after launch so operators can create, diagnose, repair, reconcile, or rotate the realm/client configuration.
- Deployment-detected Keycloak retains an explicit **Configure Authentication Providers** action instead of forcing login. The focused editor exposes manual realm values plus additive patch-existing/create-if-missing bootstrap actions, reads only a redacted contract, and never prefills stored secrets into browser controls.
- `AUTHORIZATION_PROVIDER=local|cerbos` is a validated deployment-owned selector shared by onboarding and runtime authorization. Local skips the choice page without contacting Cerbos. Cerbos uses bounded single-flight background work to verify the instance PDP and publish only to the instance Admin API, stays fail-closed until ready, skips the chooser while pending/ready, and exposes locked remediation after final failure. Blank/unset keeps manual onboarding with Local selected and Cerbos behind native progressive disclosure.
- Completion remains server-authoritative: both pages submit through existing commands, re-fetch status, and reject unconfirmed completion or tenant drift.
- The BFF always removes browser-controlled setup-secret headers and forwards the trusted server/session secret only to exact or slash-delimited instance-onboarding endpoint paths; query-string and near-route lookalikes fail closed.
- Endpoint composition was retained after request-count and overlapping-refresh tests reproduced no snapshot escalation trigger.
- `SetupLayout` supplies language/theme controls and a main landmark, but it does not supply the prototype's journey header, step progress, summary/help rail, or shared navigation footer.
- Provider, authorization, overview, and readiness content still render as separate page compositions; `OnboardingTaskList` is an overview, not the persistent cross-route workspace shown in the prototype.

### 1.2 Prototype Reconciliation — 2026-07-29

| Proposal | Decision | Repository-grounded reason |
|---|---|---|
| Present one coherent, recoverable launch journey with clear progress and task status. | Implement | Current pages expose authoritative state but do not present a persistent journey across routes. |
| Give SingleTenant and MultiTenant distinct completion handoffs. | Retain | `StartupGate.razor` and completion routing already send SingleTenant to events/settings and MultiTenant to the control plane. |
| Keep provider setup manageable after initial completion. | Retain | HAL-gated authentication and authorization management routes remain available before and after launch. |
| Use one persistent workspace with header, segmented progress, focused main step, setup summary/help rail, and footer navigation. | Implement | `SetupLayout` is only minimal chrome. Sharing a layout type does not create the prototype's information architecture or continuity. |
| Let the browser choose deployment mode during onboarding. | Reject | `DeploymentModeProvider` and canonical deployment docs make mode operator-controlled configuration; the UI may display it only as read-only context. |
| Require a first tenant before a MultiTenant instance can launch. | Reject | Platform readiness and tenant readiness use different authority scopes; zero-tenant MultiTenant launch is intentionally supported. |
| Add provider-intent or Local/Cerbos bootstrap machinery as part of this refactor. | Reject as already implemented | Validated deployment intent, bounded reconciliation, runtime precedence, safe remediation, and server-authoritative route skipping already exist. |
| Use guided step navigation while preserving recovery and direct revisits. | Implement with constraints | The progress/summary UI is a server-derived navigation projection, not a second completion store. Existing focused routes remain addressable, completed steps remain revisitable when HAL permits, and failures do not reset prior work. |
| Add onboarding-only settings editors or an aggregate snapshot pre-emptively. | Reject | Existing editors and endpoints are reused; request-count and refresh tests did not meet the snapshot escalation threshold. |
| Show “Draft saved / Save and exit / resumes here.” | Implement with one narrow write | Current final completion is the only pre-launch profile persistence path. Reuse `SelfHostOnboardingProfileDto` and existing instance-setting storage through an onboarding-scoped command; never persist secrets or generic client wizard state. |

Result: the visual/navigation proposal is valid and missing. Phase 9 implements it on top of the existing route and authority contracts; provider, deployment-mode, tenant, and completion behavior remain unchanged.

### Explicitly Out Of Scope

- Tenant invitations, tenant lifecycle transitions, self-service registration, and public tenant creation.
- A user-editable deployment mode or admin-host selector.
- Cerbos policy inventory and arbitrary decision-test APIs.
- Replacement of Keycloak, Cerbos, local RBAC, or current BFF token handling.
- A second set of onboarding-only post-launch editors.
- Destructive external-provider repair, compatibility shims, or schema changes without new evidence and approval.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| The current experience is not unified despite sharing `SetupLayout`. | User-supplied current screenshots of setup access, authentication configuration/detected state, setup overview/profile/readiness, and authorization | High | Pages have inconsistent geometry and action placement with no persistent progress or summary/help rail. |
| The desired workspace geometry is explicit. | User-supplied prototype screenshot | High | Use header + saved/resume action, segmented progress, focused main step, setup summary/about rail, and stable footer navigation as layout grammar; do not copy branding or fixed domain steps. |
| Setup, startup, provider, instance, tenant, login, logout, and settings routes already exist. | Verified: `src/Explore.Blazor.Client/Routes.razor` | High | Route orchestration should be simplified, not rebuilt. |
| Setup secret is resolved and forwarded by trusted server code rather than browser code. | Verified: `src/Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs`; `src/Explore.Blazor/Services/SetupSecretResolver.cs`; `src/Explore.Blazor/Services/SetupSecretForwardingHandler.cs` | High | Preserve this trust boundary. |
| Current post-auth onboarding is split across instance, auth provider, authz provider, and tenant pages. | Verified: `src/Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor`; `AuthProviderConfiguration.razor`; `AuthorizationProviderConfiguration.razor`; `TenantOnboarding.razor` | High | These become task destinations or focused task pages. |
| Startup routing already distinguishes incomplete and completed onboarding. | Verified: `src/Explore.Blazor.Client/Pages/Onboarding/StartupGate.razor`; `src/Explore.Blazor.Client/Services/StartupRoutingService.cs` | High | Preserve server-derived routing behavior. |
| Standard application chrome is hidden for setup/startup/onboarding routes. | Verified: `src/Explore.Blazor.Client/Layout/MainLayout.razor.cs`; `src/Explore.Blazor.Client/Layout/SetupLayout.razor` | High | New routes must remain in the same shell policy. |
| Deployment mode is operator-controlled and persisted at completion; the client value is not authoritative. | Verified: `src/Explore.Infrastructure/Services/DeploymentModeProvider.cs`; `src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`; `docs/DEPLOYMENT_MODES.md` | High | Never render it as a chooser. |
| Instance preflight already distinguishes blockers and operational warnings. | Verified: `src/Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetOnboardingPreflightQueryHandler.cs` | High | Map blockers to required tasks and warnings to remediation/optional tasks. |
| Instance completion creates required bootstrap state, users/admin grants, and default tenant behavior. | Verified: `src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs` | High | Do not duplicate orchestration in the client. |
| Pre-launch site-profile edits have no independent save contract. | Verified: `InstanceOnboarding.razor` submits its `EditForm` only through `CompleteOnboardingAsync`; `InstanceSettingsController.UpdateBrandingSettings` requires the instance-admin authority granted only at completion. | High | Activates the narrow profile-draft write required for truthful Save and exit/resume. |
| Tenant onboarding has status, settings, progress, and completion operations. | Verified: `src/Explore.API/Controllers/TenantOnboardingController.cs`; tenant onboarding command/query handlers | High | UI should reuse these contracts. |
| Existing post-launch settings pages can be task destinations. | Verified: `src/Explore.Blazor.Client/Pages/Admin/Instance/InstanceAdminSettings.razor`; `Pages/Admin/Tenant/TenantAdminSettings.razor`; `Pages/Admin/Organization/OrganizationAdminSettings.razor` | High | Avoid parallel editors. |
| Cerbos/local selection, reachability, readiness, package download, and sync exist. | Verified: `src/Explore.Infrastructure/Services/AuthorizationProviderConfigurationService.cs`; `CerbosConfigResolver.cs`; `RuntimeAuthorizationProvider.cs`; `CerbosPolicyPackageService.cs`; `src/Explore.API/Controllers/InstanceSettingsController.cs` | High | Keep scope to existing operator workflows. |
| No Cerbos inventory or decision-check endpoint exists. | Not found: searches for `Cerbos inventory`, `policy inventory`, and `decision check` under `src/Explore.*` | High | Treat both as deferred hypotheses, not requirements. |
| RFC 7807 and server-validation client handling already exist. | Verified: `src/Explore.API/ExceptionHandling/ApiProblemFactory.cs`; `GlobalExceptionHandler.cs`; `src/Explore.Blazor.Client/Exceptions/ApiProblemException.cs`; `Components/Forms/ServerValidationErrorStore.cs` | High | Task failures should reuse these contracts. |
| Write idempotency and bounded setup audit logging already exist. | Verified: `src/Explore.API/Middleware/IdempotencyMiddleware.cs`; `InstanceBootstrapAuditLogger`; setup-secret filter/attribute | High | Completion and retry UX must preserve these semantics. |
| MultiTenant tenant resolution fails closed; SingleTenant uses the fixed default tenant. | Verified: `docs/MULTI_TENANCY.md`; `docs/DEPLOYMENT_MODES.md`; `docs/QUICK_REFERENCE.md` | High | Never infer or fabricate tenant context in the client. |
| Existing paused tenant-onboarding plan is broader and stale. | Verified: `dev/pause/tenant-onboarding-enterprise/tenant-onboarding-enterprise-plan.md`, context, tasks | High | This workstream supersedes only route/wizard UX assumptions. |
| Deployment-detected Keycloak must remain sanitized and manageable after configuration. | Implemented: `AuthProviderConfigurationService`, API compatibility mapping, Compose/Aspire propagation, server-derived secret ownership, and focused service/TestServer tests | High | Complete authority/client-ID tuples report detected/configured state without returning the secret; HAL navigation still reaches the provider page for create, repair, and reconcile actions. Real deployed login/realm-repair manual verification remains open. |

### 2.2 Existing Implementation By Layer

#### Blazor Client

- `Pages/Setup.razor` is the setup-secret bootstrap entry and authentication handoff.
- `Pages/Onboarding/StartupGate.razor` selects the next route from current server state.
- `InstanceOnboarding.razor`, `AuthProviderConfiguration.razor`, and `AuthorizationProviderConfiguration.razor` implement instance/provider steps.
- `TenantOnboarding.razor` implements tenant-scoped completion and redirection.
- Existing instance, tenant, and organization settings pages provide reusable post-launch editors.
- `MainLayout.razor.cs` owns onboarding chrome suppression and accessibility focus behavior.
- `Shared/LanguagePicker.razor` exposes culture selection.

#### Blazor BFF

- `BffAuthEndpoints.cs` owns browser authentication/session endpoints.
- `BffSetupSecretEndpoints.cs` owns setup-secret session lifecycle.
- `SetupSecretResolver.cs` resolves trusted secret sources.
- `SetupSecretForwardingHandler.cs` replaces untrusted browser input with trusted server-resolved authority.
- `CircuitUserContext.cs` bridges authenticated circuit identity.

#### Application/API

- `InstanceOnboardingController.cs`, `SystemController.cs`, `TenantOnboardingController.cs`, and `InstanceSettingsController.cs` expose status, preflight, provider configuration, completion, tenant onboarding, and post-launch operations.
- Status handlers combine bootstrap state, configured/persisted deployment mode, setup-secret state, and admin membership.
- The preflight handler checks setup-secret state, database reachability, deployment mode, default tenant, auth configuration, canonical host, DNS guidance, and operational warnings.
- Instance completion is the authoritative bootstrap commit and locks setup mode after success.
- Tenant completion/progress handlers persist tenant-scoped state and enforce settings locks.

#### Infrastructure/Operations

- `DeploymentModeProvider` owns configured onboarding mode and persisted runtime mode.
- Authorization-provider services verify and persist local/Cerbos configuration without returning raw credentials.
- Runtime authorization selects tenant BYO Cerbos, then instance Cerbos, then local fallback according to configured failure semantics.
- Aspire and Compose already model external/local infrastructure; Compose includes Cerbos policy sync support.
- Shared health, liveness, metrics, and OpenTelemetry exist in `src/Explore.ServiceDefaults/Extensions.cs`.

### 2.3 Existing Tests And Verification Coverage

Verified examples include:

- `tests/Explore.Blazor.Client.Tests/Pages/SetupTests.cs`
- `tests/Explore.Blazor.Client.Tests/Pages/Onboarding/InstanceOnboardingTests.cs`
- `tests/Explore.Blazor.Client.Tests/Pages/Onboarding/TenantOnboardingTests.cs`
- `tests/Explore.Blazor.Client.Tests/Pages/Onboarding/StartupGateTests.cs`
- `tests/Explore.Blazor.Client.Tests/Layout/MainLayoutTests.cs`
- `tests/Explore.Blazor.IntegrationTests/Endpoints/BffSetupSecretEndpointsTests.cs`
- `tests/Event.API.IntegrationTests/Features/InstanceOnboardingControllerTests.cs`
- `tests/Event.API.IntegrationTests/Features/ProblemDetailsContractTests.cs`
- `tests/Event.API.IntegrationTests/Features/CerbosPolicyBootSyncRunnerTests.cs`
- `tests/Event.Application.UnitTests/Features/InstanceOnboarding/Commands/CompleteInstanceOnboardingCommandHandlerTests.cs`
- `tests/Event.Application.UnitTests/Features/InstanceOnboarding/Queries/GetOnboardingPreflightQueryHandlerTests.cs`
- Tenant onboarding, tenant settings, tenant creation, deployment-mode, setup-secret, and authorization-provider service tests in the corresponding Application and Infrastructure test projects.

Gaps:

- No dedicated `TenantOnboardingControllerTests` integration file was found.
- Existing UI tests protect pages individually but do not yet prove one end-to-end conceptual task-list journey across both deployment modes.
- Authority-denial tests must explicitly cover platform-admin versus tenant-admin task execution.

### 2.4 Existing Documentation And Contracts

Relevant current sources are `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/BLAZOR.md`, `docs/DEPLOYMENT_MODES.md`, `docs/MULTI_TENANCY.md`, `docs/TESTING.md`, and `docs/OPERATIONS.md`. API behavior is also represented in OpenAPI/generated `IEventApiClient`; generated clients must never be hand-edited.

No dedicated onboarding, bootstrap, authorization-provider onboarding, Cerbos inventory, or decision-test document was found. This plan therefore updates canonical existing operator docs rather than adding a new permanent product doc unless implementation reveals a durable information architecture need.

### 2.5 Current Pain Points / Improvement Areas

1. **Fragmented journey:** Existing routes are valid individually but render as disconnected pages with no persistent progress, summary, or navigation model.
2. **Weak visual continuity:** Sharing `SetupLayout` provides theme/language controls, not the prototype's workspace. Page width, density, hierarchy, action placement, and contextual guidance change substantially between setup, provider, authorization, overview, and readiness screens.
3. **Poor use of viewport:** The provider pages occupy a narrow card or form inside a mostly empty desktop canvas, while overview/readiness pages become long undifferentiated documents.
4. **No durable orientation:** Operators cannot see completed/current/upcoming work, step count, why the current step matters, or where Back/Continue will lead.
5. **Inconsistent exit/resume semantics:** Existing route navigation can resume from authoritative status, but the UI does not expose a clear Save and exit/Exit contract or warn about unsaved sensitive form input.
6. **Mode ambiguity:** A UI that treats deployment mode as form input would conflict with the operator-owned source of truth and produce misleading state.
7. **Scope ambiguity in MultiTenant:** Platform readiness and first-tenant readiness are separate outcomes; coupling them would block the control plane and blur authorities.
8. **Duplicate editor risk:** Building onboarding-only settings forms would drift from post-launch editors and validation contracts.
9. **Local authority risk:** Deriving steps or actions from route history, claims, or browser state would violate HAL and tenant fail-closed rules.
10. **Recovery discoverability:** Existing backend retry/idempotency support is stronger than the current UX explanation of failures, reruns, and partially completed provider setup.
11. **Test gap:** Page tests protect individual routes but do not prove a responsive cross-route workspace, summary state, focus flow, or secure exit/resume behavior.

### 2.6 Unknowns After Investigation

| Unknown | Search/Evidence | Resolution During Implementation |
|---|---|---|
| Can existing endpoint composition provide a stable task list without visible intermediate inconsistency? | Status, provider, preflight, and progress endpoints exist; no aggregate contract exists. | Implement composition first; instrument/test refresh behavior. Escalate only on reproduced races or unacceptable amplification. |
| Can post-launch editors render safely inside an onboarding shell, or should tasks link to them? | Editors exist, but setup-context compatibility was not proven. | Prefer links/focused task pages. Reuse embedded editor components only after auth/context/accessibility tests. |
| Which warnings are actionable before launch versus informational after launch? | Preflight exposes warnings, but product categorization is not fully specified. | Define a deterministic mapping table during Phase 1 with product-owner review; blockers remain server-authoritative. |
| Are all task labels localized today? | Language picker/localization infrastructure exists; exact onboarding resource coverage was not audited. | Inventory strings before component work; add resource keys rather than hard-coded English. |
| Which HAL links are already available for every task action? | HAL is verified in admin UI, not exhaustively for every onboarding response. | Audit generated DTO links before implementation; add a HAL link only under the conditional intent if a mutation affordance truly lacks one. |

## 3. Proposed Future State

### 3.1 Conceptual Journey

```text
Operator deployment configuration
  DEPLOYMENT_MODE + admin host + secrets
                    |
                    v
/setup access gate (pre-auth, setup-secret authority)
  validate setup secret
                    |
                    v
Unified onboarding workspace
  authentication provider -> OIDC handoff
                    |
                    v
BFF-authenticated workspace resumes from authoritative state
  site profile -> authorization -> readiness review -> launch
                    |
          +---------+----------+
          |                    |
   SingleTenant           MultiTenant
   events/settings        platform/control-plane settings
                               |
                               +-- optional first-tenant task
                                   (new tenant context and tenant authority)
```

### 3.2 Unified Workspace Contract

Desktop layout follows the prototype's information architecture while using ISLAMU Event tokens, typography, wrappers, and copy:

- **Outer layout:** `SetupLayout` remains the trust-neutral page layout and owns theme/language controls plus the main landmark.
- **Workspace header:** product identity at inline-start; authoritative saved/resumable status and Save and exit/Exit action at inline-end.
- **Progress header:** current step label, `Step n of m`, and a segmented progress indicator. The visible step set is derived from deployment mode, provider ownership, current authority, and server status; it is not hard-coded to the prototype's eight steps.
- **Main step:** one structural `h1`, concise explanation, the existing focused form/status content, inline validation, and no full-page card around the entire form.
- **Summary rail:** a complementary desktop `aside` with completed/current/upcoming steps and an About this step explanation. Step links render only when revisiting/navigating is safe and authorized.
- **Footer navigation:** stable Back and primary Continue/Review/Launch positions. Actions remain real buttons/links and use existing page commands and HAL relations.

Responsive behavior:

- At wide desktop, main content and summary rail form a two-region CSS Grid; the rail uses tonal separation rather than heavy elevation.
- At tablet/mobile, the rail becomes an in-flow `details`/drawer-like summary after the progress header, the main step becomes full width, and footer actions remain reachable without covering validation or the on-screen keyboard.
- The segmented indicator may compress visually, but current step text and `n of m` remain visible and no horizontal page overflow is allowed.
- Logical CSS properties, RTL order, 24px minimum targets, visible focus, forced-colors, long translations, and reduced motion are mandatory.

State semantics:

- Step status is derived from API/HAL state, never route history or a browser-only completion store.
- The access gate is visually related but outside the numbered journey until the setup secret is valid.
- OIDC and setup-secret cookie transitions may hard-reload; visual continuity must not turn them into client-only navigation.
- Save and exit never stores setup secrets, provider secrets, or unsaved form fields in local/session storage. A page may save through its existing server endpoint before exit; otherwise dirty input requires explicit discard confirmation.
- Resume uses `StartupGate`/`StartupRoutingService` plus authoritative status to choose the earliest incomplete or failed-remediation step.
- Deployment-managed skipped steps are shown as completed/configured or omitted according to the journey definition; they are never marked complete from route visits.
- A blocking preflight result prevents launch; warnings remain visible and nonblocking. Retry re-fetches authoritative state.
- Errors reuse RFC 7807 and accessibility announcements without exposing credentials.
- Completion status does not remove independently authorized provider-management actions.

### 3.3 Instance Journey Step Projection

| Surface | Counted step | Existing route/source | Authority and projection rule |
|---|---:|---|---|
| Setup access | No | `/setup`; BFF setup-secret session | Pre-auth gate only. After validation, enter the workspace; never show the secret or count the gate as completed journey work. |
| Authentication provider | 1 | `/onboarding/auth-provider`; auth-provider status/configuration | Setup-secret authority before OIDC. Deployment-detected configuration may render complete with a manage/reconcile path. |
| OIDC handoff | No | `/auth/login` → `/startup` | Hard browser/session transition. Resume the same visual journey post-auth; do not persist client wizard state through it. |
| Site profile | 2 | `/onboarding/instance`; existing branding read plus planned `PATCH /api/instance-onboarding/profile` | `[Authorize]` + active setup-secret authority. Persist only `SelfHostOnboardingProfileDto` through the onboarding command; show read-only deployment context nearby, never as input. |
| Authorization | 3 | `/onboarding/authz-provider`; authz status/configuration | Explicit Local/Cerbos deployment state may auto-complete/skip. Failed managed Cerbos remains a reachable remediation state. |
| Readiness review and launch | 4 | `/onboarding/instance`; preflight plus `complete` HAL action | Required checks block; warnings do not. Launch is the primary action inside the final step and is confirmed by a fresh status read. |
| Tenant onboarding | Separate journey | `/onboarding/tenant`; tenant status/settings/progress | Never part of instance `n of m`. Available after MultiTenant platform launch only under trusted tenant authority. |

The summary rail may display completed and upcoming steps, but only the four counted instance steps contribute to progress. If a deployment-owned step is omitted rather than displayed as configured, `n of m` is recomputed from the visible journey definition and tested for consistency.

### 3.4 Authority Matrix

| Task family | Endpoint family | Authority | Tenant context | Modes | HAL/RFC 7807 expectations |
|---|---|---|---|---|---|
| Setup-secret validation/session | BFF setup endpoints and setup-secret-gated API endpoints | Pre-auth setup secret resolved by trusted server | None | Both | Browser header stripped/replaced; RFC 7807/controlled status for invalid, inactive, rate-limited secret. |
| Authentication provider and first admin | Instance onboarding/setup endpoints | Setup secret until OIDC handoff; authenticated platform administrator for ongoing provider management | None | Both | No provider token/secret materialized to client; safe problem details; completed configuration retains a HAL-authorized **Manage authentication** affordance for realm creation, repair, or reconciliation. |
| Setup Overview read state | System/instance onboarding status, provider status, preflight | Authenticated platform administrator after login; public status only where current endpoint permits | No arbitrary tenant context | Both | Read-only state; absent affordance/state fails closed. |
| Site profile and instance configuration | Existing onboarding completion/settings operations | Platform administrator | Instance; SingleTenant handler may bind default tenant internally | Both | Write requires authorized action; validation uses RFC 7807. |
| Authorization provider | Instance onboarding/settings authz endpoints plus startup reconciliation | Deployment intent is server-owned; setup secret before auth where currently supported; platform administrator after auth | Instance | Both | Explicit Local/Cerbos skips choice; Cerbos runtime stays fail-closed until endpoint verification and policy sync succeed; credentials remain write-only/redacted; failures expose safe task remediation. |
| Platform review and launch | Existing preflight and instance completion | Platform administrator plus any current setup constraints | Instance | Both | Required checks block; retry is idempotent; launch affordance server-authoritative. |
| First-tenant handoff | Existing tenant creation/onboarding routes only when server authorizes | Platform administrator may initiate allowed platform action; tenant tasks require tenant administrator authority | Explicit resolved tenant | MultiTenant only | No role-derived local elevation; missing tenant/link fails closed. |
| Tenant profile/policy/branding completion | Tenant onboarding/status/settings endpoints | Tenant administrator or explicitly supported instance-admin operation in handler | Required, trusted tenant resolution | MultiTenant and default-tenant paths where current API permits | Locked settings remain server-enforced; errors RFC 7807; actions HAL-gated. |
| Post-launch editors | Existing instance/tenant/organization settings APIs | Existing resource authority | Existing route context | As currently supported | Reuse current HAL action rules and validation. |

## 4. Non-Negotiable Constraints

- `/setup` remains a separate setup-secret trust boundary; post-auth roles do not replace it.
- Browser-supplied privileged headers are never trusted.
- Tokens, setup secrets, provider credentials, and raw admin responses never reach browser storage, logs, DTOs, traces, or support output.
- `DEPLOYMENT_MODE` and dedicated admin hosts are deployment configuration, not runtime onboarding choices.
- MultiTenant platform launch never depends on first-tenant onboarding.
- Tenant resolution and authorization remain API-authoritative and fail closed.
- HAL `_links` is the only source of UI action affordances; do not inspect local roles/claims to reveal actions.
- Repositories return entities, never DTOs; handlers map application DTOs.
- Validators are manually instantiated.
- GET/write authorization, controller metadata, RouteNames, RFC 7807, and generated-client rules remain intact.
- Blazor consumes the generated `IEventApiClient` through the BFF and does not reference backend layers.
- InteractiveAuto/WASM code never assumes `HttpContext`.
- All new files receive the required two-line `ABOUTME:` header.
- WCAG 2.2 AA, localization, logical CSS properties, RTL, and existing design wrappers are required.
- No compatibility shim, destructive provider repair, or silent override of deployment secrets without explicit approval.

## 5. Architecture And Design Decisions

### D1 — Preserve Security Boundaries Inside One Visual Workspace

- **Decision:** Keep `/setup` pre-auth and setup-secret protected, then use the same workspace presentation for provider setup and post-auth administrator steps while preserving the existing hard OIDC/cookie transitions.
- **Why:** The user needs continuity, not merged credentials. Visual structure can persist across routes even when authority and browser sessions change.
- **Alternatives:** Keep the current split-screen access page plus isolated centered forms; rejected because the screenshots show that shared theme controls alone do not communicate one journey. A client-only wizard carrying setup state across OIDC is also rejected because it expands browser ownership and breaks the BFF boundary.
- **Consequences:** The access gate remains outside numbered progress. The provider step can render inside the workspace under setup-secret authority; post-auth steps rebuild the same journey model from server state.
- **Files/layers:** Setup page, BFF setup endpoints/services, startup gate, onboarding client pages, tests.

### D2 — Guided Workspace, Not A Monolithic Client Wizard

- **Decision:** Add a persistent route-aware progress header, focused step body, summary/help rail, and stable footer around the existing focused routes.
- **Why:** The prototype provides orientation and consistent action placement without requiring one giant form. Existing routes remain the recovery and deep-link boundaries.
- **Alternatives:** The current overview plus disconnected forms is rejected as visually fragmented. One monolithic component with local step state is rejected because provider, OIDC, platform, and tenant authorities differ.
- **Consequences:** A shared display component receives a server-derived journey projection; pages retain business logic. `OnboardingTaskList` may be adapted for the summary projection or retired if the new shell makes it redundant, but there is only one visible progress model.

### D3 — Deployment Mode Is Read-Only Context

- **Decision:** Display resolved mode for explanation only; never offer a chooser.
- **Why:** The configured onboarding mode is operator-owned and the completion handler ignores client authority.
- **Alternatives:** Persist a form selection; rejected as misleading and unsafe.

### D4 — Platform Launch Precedes Optional Tenant Launch

- **Decision:** MultiTenant platform completion is sufficient for launch. First-tenant onboarding is a separate optional task and authority context.
- **Why:** Platform administrators and tenant administrators have distinct responsibilities, and the control plane must operate without an application tenant.
- **Alternatives:** Force tenant creation before completion; rejected because it couples unrelated readiness boundaries.

### D5 — Compose Existing Endpoints Initially

- **Decision:** Retain composition; do not create an onboarding snapshot endpoint for this workstream.
- **Why:** Existing status, provider, preflight, settings, and progress contracts already own authoritative state. A snapshot would add a new consistency contract.
- **Measured evidence (2026-07-12):** instance refresh uses one status read plus one parallel set of five existing reads; tenant refresh uses status plus settings only while incomplete. Focused tests prove exactly one call set per load/refresh, overlapping refresh deduplication, and authoritative post-mutation re-fetch. No D5 escalation trigger was reproduced.
- **Escalation triggers:** (1) tests reproduce contradictory state across calls that client refresh cannot resolve; (2) traces show an unacceptable request storm after simple deduplication/caching; (3) identical task derivation is duplicated across more than two consumers; or (4) server-side atomicity is required to express a launch invariant.
- **If escalated:** Reclassify under `add-get-endpoint`, `add-cqrs-handler`, and `openapi-contract-change`; design a secret-free read DTO, named route, response metadata, generated client, cache semantics, and integration tests.

### D6 — Reuse Existing Editors And Provider Operations

- **Decision:** Task actions link to or compose existing focused pages/editors; no onboarding-specific duplicate forms by default.
- **Why:** Shared validation, HAL, localization, and post-launch maintenance remain consistent.
- **Consequence:** Embed only components proven safe across setup and post-launch contexts. Treat completion and ongoing manageability as separate server-authoritative properties: a completed task may retain a HAL-authorized management link, including **Manage authentication** for Keycloak realm repair/reconciliation.

### D7 — Authorization Intent Is Explicit; Cerbos Scope Is Health And Package Sync

- **Decision:** Treat `AUTHORIZATION_PROVIDER=local|cerbos` as deployment-owned intent and blank/unset as manual onboarding. Endpoint or credential presence is a prerequisite, never provider intent. Reuse existing endpoint verification, readiness, package download, and sync; do not add inventory or arbitrary decision testing.
- **Why:** Compose and Aspire can supply Cerbos endpoints even when Local is desired, so inference is ambiguous. The explicit selector gives runtime and onboarding one source of truth without expanding Cerbos product scope.
- **Consequence:** Local performs no Cerbos call. Cerbos is selected at runtime immediately and therefore fails closed; bounded single-flight background work verifies the instance PDP then publishes to the instance Admin API before configured status becomes ready. Automatic navigation skips the choice page while pending/ready, and final failure is repaired through a locked remediation view. Manual blank/unset onboarding defaults to Local and reveals Cerbos progressively.

### D8 — Recovery Is A First-Class UX State

- **Decision:** Every task can reload authoritative status, show safe RFC 7807 failures, and offer retry/remediation without resetting completed work.
- **Why:** External provider and DNS/configuration steps fail independently; restarting an entire wizard is unsafe.

### D9 — Save And Exit Means Server Persistence Or Explicit Discard

- **Decision:** Do not add browser draft persistence. Add `PATCH /api/instance-onboarding/profile` (`RouteNames.SaveInstanceOnboardingProfile`) and a manually validated `SaveInstanceOnboardingProfileCommand` to persist only `SelfHostOnboardingProfileDto` under `[Authorize]` + `[SetupSecretRequired]`. `InstanceOnboardingStatusLinkPolicy` emits `save-profile` only while setup is active and the caller is authenticated. Other steps use their existing server commands; otherwise Exit confirms discarding dirty input.
- **Why:** The prototype's resume affordance is valuable, but setup/provider secrets and stale deployment data must not be stored in browser storage.
- **Consequences:** The command reuses current instance-setting keys and the existing profile DTO/validator; it adds no schema or generic wizard-state table. Repeating the same PATCH converges to the same values, and successful writes invalidate the existing public-experience shell cache. Completion remains authoritative and receives/finalizes the profile again. Resume reads current branding/profile values plus status. Tests pin profile-field parity between draft save and completion. The header may say progress is saved only after a confirmed server write.

### D10 — Prototype Layout Grammar, ISLAMU Design System

- **Decision:** Follow the prototype's workspace geometry and hierarchy, not its Oppworx branding, fixed eight-step copy, colors, or domain-specific controls.
- **Why:** The reference demonstrates the missing experience; `docs/DESIGN.md`, MudBlazor v9 wrappers, and `--isl-*` tokens remain the implementation source of truth.
- **Consequences:** Update `docs/DESIGN.md` with the `OnboardingWorkspace` primitive and all states before component implementation. Reference-fidelity QA judges structure, proportions, hierarchy, and responsive intent alongside project theming.

## 6. Implementation Phases

### Phase 0: User Review And Baseline

- **Goal:** Approve scope, freeze authority boundaries, and confirm a green baseline without touching unrelated working-tree changes.
- **Depends on:** None.
- **Relevant files:** These three dev docs; existing tests and canonical docs.
- **Acceptance:** User approves/corrects journeys, authority matrix, deferred scope, and snapshot threshold.
- **Verification:** Capture `git status`; run the canonical build only when the implementation session can isolate existing user changes.
- **Rollback:** Documentation-only; revert only this workstream if rejected.

#### Task 0.1 — Review Plan
- **Type:** investigate/docs
- **Layer:** Docs/Product/Security
- **Files:** these three new planning files
- **Description:** User and implementation lead review Sections 3–6 and the paused-work overlap.
- **Acceptance Criteria:** approval state recorded; open decisions converted to tasks.
- **Dependencies:** none
- **Effort:** S
- **Validation:** plan/context/tasks agree.

### Phase 1: Contract And TDD Baseline

- **Goal:** Encode the two deployment-mode journeys and three authority contexts before UI refactoring.
- **Depends on:** Phase 0 approval.
- **Relevant files (existing):** onboarding page tests, startup routing tests, setup tests, BFF setup-secret tests, API onboarding tests.
- **Acceptance:** Tests describe SingleTenant launch, MultiTenant platform launch, optional tenant handoff, no mode chooser, blocker/warning behavior, and denial cases.
- **Verification:** Focused Blazor/BFF/API tests.
- **Rollback:** Test-only commit can be reverted without product impact.

#### Task 1.1 — Add Journey Tests
- **Type:** test
- **Layer:** Blazor Client
- **Files:** existing `tests/Explore.Blazor.Client.Tests/Pages/Onboarding/InstanceOnboardingTests.cs`, `TenantOnboardingTests.cs`, `StartupGateTests.cs`, `tests/Explore.Blazor.Client.Tests/Pages/SetupTests.cs`; route-guard tests discovered during implementation
- **Description:** Write failing/updated tests for the target routing and task-state model.
- **Acceptance Criteria:** SingleTenant never enters tenant onboarding after platform completion; MultiTenant lands in control-plane/instance administration; first-tenant task is optional; deployment mode is not editable; completed authentication-provider state still exposes **Manage authentication** when the authoritative HAL affordance is present.
- **Dependencies:** 0.1
- **Effort:** M
- **Required Skills/Rules:** Blazor UI, accessibility, auth, tests
- **Validation:** `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

#### Task 1.2 — Add Authority And Trust-Boundary Tests
- **Type:** test
- **Layer:** BFF/API
- **Files:** existing `tests/Explore.Blazor.IntegrationTests/Endpoints/BffSetupSecretEndpointsTests.cs`; existing API onboarding/setup-secret integration tests; **new only if absent:** `tests/Event.API.IntegrationTests/Features/TenantOnboardingControllerTests.cs`
- **Description:** Prove browser headers are ignored, setup credentials remain server-owned, platform/tenant denial cases fail closed, and RFC 7807 remains safe.
- **Acceptance Criteria:** No secret echo/log assertion; trusted forwarding only; unauthorized cross-scope actions denied; invalid/inactive/rate-limited setup behavior preserved.
- **Dependencies:** 0.1
- **Effort:** M
- **Validation:** targeted BFF and API integration tests.

### Phase 2: Minimal Shared Task-List Presentation

- **Goal:** Create the smallest accessible display primitive needed by both journeys.
- **Depends on:** Phase 1 tests.
- **Relevant files:** **new** `src/Explore.Blazor.Client/Pages/Onboarding/Components/OnboardingTaskList.razor`; **new** isolated CSS; optional item/model file only if one component becomes unwieldy.
- **Acceptance:** Required/optional/status/remediation semantics are accessible, responsive, localized, and authority-neutral.
- **Verification:** bUnit/component tests, keyboard review, dark/light and RTL visual QA.
- **Rollback:** Remove the new component and retain current pages.

#### Task 2.1 — Inventory Localization And Design Reuse
- **Type:** investigate
- **Layer:** Blazor Client
- **Files:** existing onboarding pages, localization resources, common wrapper components, design tokens
- **Description:** Find reusable wrappers and resource keys before adding markup.
- **Acceptance Criteria:** No duplicate wrapper or hard-coded English workflow text is introduced.
- **Dependencies:** 1.1
- **Effort:** S
- **Validation:** evidence recorded in context.

#### Task 2.2 — Implement Task List Component
- **Type:** create/test
- **Layer:** Blazor Client
- **Files:** new component/CSS and corresponding test file
- **Description:** Render a semantic list of display-only items with title, description, required/optional text, status, action/link, and disabled reason.
- **Acceptance Criteria:** keyboard-operable; status not color-only; logical CSS only; ≥24px targets; no role/claim logic; no backend dependencies.
- **Dependencies:** 2.1
- **Effort:** M
- **Validation:** Blazor client tests and accessibility checks.

### Phase 3: Instance Setup Overview And Launch

- **Goal:** Convert post-auth instance onboarding into authoritative conditional tasks while preserving focused provider pages and completion handlers.
- **Depends on:** Phase 2.
- **Relevant files (existing):** `InstanceOnboarding.razor`, its CSS, `AuthProviderConfiguration.razor`, `AuthorizationProviderConfiguration.razor`, `StartupGate.razor`, `InstanceOnboardingService.cs`, post-launch instance settings.
- **Acceptance:** Both modes share the overview, but mode-specific launch outcomes are explicit; required preflight checks block; warnings do not; retries are safe.
- **Verification:** Blazor client tests, Application unit tests, API integration tests, manual refresh/failure recovery.
- **Rollback:** Revert route/page refactor; backend contracts remain unchanged.

#### Task 3.1 — Derive Instance Tasks From Existing Contracts
- **Type:** modify/test
- **Layer:** Blazor Client
- **Files:** existing instance onboarding page/service; new local typed view state only if necessary
- **Description:** Compose system/instance status, provider status, and preflight into deterministic task items. Deduplicate concurrent loads and cancel stale requests.
- **Acceptance Criteria:** no snapshot endpoint; no local role authority; absent/error state fails closed; secrets absent; authentication-provider completion changes task status without removing a HAL-authorized **Manage authentication** action, using the setup provider page before launch and the admin provider editor after launch.
- **Dependencies:** 2.2
- **Effort:** L
- **Validation:** state permutation tests and request-count assertions where practical.

#### Task 3.2 — Add Review/Launch State
- **Type:** modify/test
- **Layer:** Blazor Client
- **Files:** existing `InstanceOnboarding.razor` and service
- **Description:** Present resolved deployment context, completed tasks, blockers, warnings, and the existing completion action.
- **Acceptance Criteria:** one h1/PageTitle; blockers prevent launch; warnings remain actionable; success announced; SingleTenant and MultiTenant handoffs match Section 3.
- **Dependencies:** 3.1
- **Effort:** L
- **Validation:** bUnit, keyboard/live-region checks, completion handler/API tests.

### Phase 4: Tenant-Scoped Optional Onboarding

- **Goal:** Use the same task-list language for tenant onboarding without making it a platform-launch prerequisite.
- **Depends on:** Phase 2 and MultiTenant handoff from Phase 3.
- **Relevant files (existing):** `TenantOnboarding.razor`, `TenantOnboardingService.cs`, tenant admin settings components, tenant route guards.
- **Acceptance:** Explicit tenant context, tenant-authorized actions, server-enforced locks, and separate platform-admin handoff.
- **Verification:** Blazor tests, tenant controller integration coverage, Application handler tests.
- **Rollback:** Revert tenant UI; platform launch remains functional.

#### Task 4.1 — Refactor Tenant Page To Tasks
- **Type:** modify/test
- **Layer:** Blazor Client
- **Files:** existing tenant onboarding page/service and tests
- **Description:** Map existing tenant status/settings/progress to profile, policy, branding, storage, and completion tasks only where current contracts support them.
- **Acceptance Criteria:** explicit trusted tenant context; no invitation/lifecycle/self-service scope; locked settings rendered from server state; completion calls existing service.
- **Dependencies:** 2.2, 3.2
- **Effort:** L
- **Validation:** tenant UI and API integration tests.

### Phase 5: Trust Boundary And Recovery Hardening

- **Goal:** Prove refresh, retry, expiry, partial provider failure, and post-completion locking behavior.
- **Depends on:** Phases 3 and 4.
- **Relevant files:** existing BFF setup-secret services/endpoints, onboarding services/pages, setup-secret and provider tests.
- **Acceptance:** No reset-all requirement; safe recovery instructions; no secret leakage; setup mode locks after successful completion.
- **Verification:** integration tests plus manual failure injection in local Aspire/Compose environment.
- **Rollback:** Test and UX messaging changes can be reverted independently; do not weaken server checks.

#### Task 5.1 — Document And Test Recovery Matrix
- **Type:** test/docs
- **Layer:** BFF/API/Blazor/Operations
- **Files:** existing tests and context; later canonical docs
- **Description:** Cover invalid/expired secret, interrupted provider verification, authz endpoint unavailable, preflight blocker, repeated completion, refresh after partial success, and post-lock rerun.
- **Acceptance Criteria:** each failure has detection, safe user message, retry/remediation, and operator recovery; raw provider response omitted.
- **Dependencies:** 3.2, 4.1
- **Effort:** L
- **Validation:** targeted test names and manual evidence recorded in context.

### Phase 6: Backend Composition Decision Gate

- **Goal:** Validate D5 with implementation measurements rather than preference.
- **Depends on:** Instance and tenant task composition.
- **Relevant files:** current client services; only conditional new Application/API DTO/query/controller paths.
- **Acceptance:** Written evidence supports composition or activates full endpoint intents.
- **Verification:** request traces, race tests, duplicated-derivation review.
- **Rollback:** Default is no backend change.

#### Task 6.1 — Measure Composition
- **Type:** investigate/test
- **Layer:** Blazor/BFF/API
- **Files:** existing services/tests; no new endpoint initially
- **Description:** Count initial/refresh requests, test cancellation/deduplication, and reproduce any inconsistent state.
- **Acceptance Criteria:** Keep composition unless an escalation trigger in D5 is demonstrated.
- **Dependencies:** 3.2, 4.1
- **Effort:** S/M
- **Validation:** decision and evidence entered in all three dev docs.

#### Task 6.2 — Add Aggregate Snapshot Only If Evidence Requires It
- **Type:** conditional modify/test
- **Layer:** Application/API/Blazor
- **Files:** new snapshot DTO/query/controller/client paths only after D5 is triggered
- **Description:** Introduce one secret-free aggregate read only if contradictory state, unacceptable amplification, duplicated derivation, or required server atomicity is reproduced.
- **Acceptance Criteria:** full endpoint/CQRS/HAL/OpenAPI contracts are reclassified and verified; otherwise this task remains deferred.
- **Dependencies:** 6.1 and a demonstrated escalation trigger
- **Effort:** L
- **Validation:** intent-specific tests and generated-client verification.

#### Task 6.3 — Reconcile Deployment Authorization And Simplify Its UI
- **Type:** modify/test/docs/ops
- **Layer:** API/Application/Infrastructure/Blazor/Deployment
- **Files:** authorization provider options/state/services/handlers; Cerbos boot runner/package service; AppHost/Compose/env; onboarding/admin components; generated contracts; focused tests and canonical docs
- **Description:** Use explicit blank/Local/Cerbos intent, reconcile deployment Cerbos in the background, and replace the three-column chooser with a Local-first single-column disclosure.
- **Acceptance Criteria:** Local makes zero Cerbos calls; Cerbos verifies and publishes to the same instance target with bounded single-flight retries; pending/ready skips the chooser; final failure is safe remediation; blank defaults Local; Keycloak management remains independently reachable.
- **Dependencies:** 3.1, 5.1, 7.1
- **Effort:** L
- **Validation:** focused cross-layer tests, contract verification, Compose/Aspire smoke, and desktop/mobile visual QA.

### Phase 7: Documentation And Operations

- **Goal:** Make deployment, rerun, recovery, and authority boundaries clear to self-hosters.
- **Depends on:** Final composition decision.
- **Relevant files:** required intent docs plus `docs/DEPLOYMENT_MODES.md`, `docs/BLAZOR.md`, and `docs/API_CHANGELOG.md` only if API changes.
- **Acceptance:** Operator can configure mode/admin host/secrets, complete either journey, diagnose failures, rotate credentials, back up state, and rerun safely.
- **Verification:** docs link/context tests and command/config examples.
- **Rollback:** Docs track implemented behavior; never document an unshipped endpoint.

#### Task 7.1 — Update Canonical Docs
- **Type:** docs
- **Layer:** Docs/DevOps
- **Files:** existing `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`, `docs/DEPLOYMENT_MODES.md`, `docs/BLAZOR.md`; conditional `docs/API_CHANGELOG.md`
- **Description:** Document mode selection, setup-secret ownership, authority matrix, required/warning checks, recovery, Cerbos scope, admin host, and both launch outcomes.
- **Acceptance Criteria:** no mode chooser or browser secret guidance; commands are reproducible; first tenant remains optional.
- **Dependencies:** 6.1
- **Effort:** M
- **Validation:** architecture agent-context link/schema tests.

### Phase 8: Foundation Verification And Re-baseline

- **Goal:** Preserve and finish verification of the implemented backend/provider/task-list foundation before final workspace handoff.
- **Depends on:** All prior phases.
- **Acceptance:** Definition of Done passes; all three dev docs reflect actual implementation.
- **Rollback:** Revert by atomic phase while preserving tested backend authority.

#### Task 8.1 — Run Required Gates
- **Type:** test/ops/docs
- **Layer:** All touched layers
- **Files:** all modified files
- **Description:** Diagnostics, individual test projects, Release build, manual journeys, accessibility/localization, Compose config, and Aspire smoke where configuration changed.
- **Acceptance Criteria:** Section 14 gates pass or blockers are recorded without false completion.
- **Dependencies:** 5.1, 7.1
- **Effort:** L
- **Validation:** exact command output summarized in context.

### Phase 9: Unified Onboarding Workspace

- **Goal:** Replace the disconnected route-specific presentation shown in the current screenshots with the prototype-informed workspace while retaining existing routes, commands, BFF transitions, and HAL authority.
- **Depends on:** Corrected plan approval and the implemented Phase 1-7 behavioral foundation. Phase 8 runtime blockers do not prevent TDD/component work, but must close before final handoff.
- **Relevant files:** `docs/DESIGN.md`; `SetupLayout.razor` and CSS; existing onboarding pages and CSS; `OnboardingTaskList`; new shared workspace component/model only where reuse is proven; existing client services and focused tests.
- **Acceptance:** Desktop and mobile render one coherent journey with authoritative progress, focused step content, summary/help, stable navigation, secure exit/resume, and mode-specific launch outcomes.
- **Rollback:** Shared workspace integration can be reverted route by route while preserving all server/provider behavior.

#### Task 9.1 — Freeze The Visual And State Contract
- **Type:** design/test/docs
- **Layer:** Blazor Client/Design System
- **Files:** `docs/DESIGN.md`; new component test/state harness; current/prototype screenshot evidence
- **Description:** Document `OnboardingWorkspace` geometry, tokens, responsive states, step projection, status vocabulary, dirty/exit behavior, and reference-fidelity expectations before implementation.
- **Acceptance Criteria:** desktop, tablet, mobile, LTR, RTL, light, dark, long-copy, loading, error, locked, skipped, complete, and dirty states are named; setup access is outside numbered progress; visible steps are conditional, not fixed at eight.
- **Dependencies:** corrected plan approval
- **Effort:** M
- **Validation:** design-system review plus failing component/source tests for the new contract.

#### Task 9.2 — Build The Shared Workspace Primitive
- **Type:** create/test
- **Layer:** Blazor Client
- **Files:** likely new `Pages/Onboarding/Components/OnboardingWorkspace.razor` and isolated CSS; minimal step descriptor/model; `SetupLayout` only for outer-chrome changes; reuse `OnboardingTaskList` where its semantics fit
- **Description:** Implement the header, progress, main slot, summary/help rail, responsive disclosure, and footer slots as a display/navigation component with no API, role, or provider business logic.
- **Acceptance Criteria:** semantic header/nav/section/aside/footer inside `SetupLayout`'s existing `main#main-content` landmark; one page `h1`; current step uses `aria-current="step"`; status is not color-only; footer actions are native controls; focus order matches visual order; no nested `main` or full-page card shell; project tokens/wrappers only.
- **Dependencies:** 9.1
- **Effort:** L
- **Validation:** bUnit semantics/state matrix and component visual harness at 375/768/1280px.

#### Task 9.3 — Integrate Setup Access, Authentication, And OIDC Handoff
- **Type:** modify/test
- **Layer:** Blazor Client/BFF boundary
- **Files:** `Setup.razor`, `AuthProviderConfiguration.razor`, their CSS/tests, `Routes.razor`, `SetupLayout`, existing BFF setup-secret tests
- **Description:** Keep the access gate secure, then render provider configuration as the first workspace step and preserve hard reloads where HttpOnly setup-secret/OIDC state changes.
- **Acceptance Criteria:** setup secret never enters journey state or browser storage; detected/manual provider paths share the workspace; Save and exit never persists secrets locally; login returns through `StartupGate`; invalid/expired secret and provider failure retain focused remediation.
- **Dependencies:** 9.2
- **Effort:** L
- **Validation:** existing setup/provider/BFF tests plus new route, focus, dirty-exit, and resume tests.

#### Task 9.4 — Add Profile Draft Persistence And Integrate Post-Auth Steps
- **Type:** modify/test
- **Layer:** Blazor Client
- **Files:** `SelfHostOnboardingProfileDto` and validator reuse; new `SaveInstanceOnboardingProfileCommand`/handler; `InstanceOnboardingController`, `RouteNames`, status HAL policy, generated client; `InstanceOnboarding.razor`, `AuthorizationProviderConfiguration.razor`, `StartupGate.razor`, `InstanceOnboardingService.cs`, CSS/tests
- **Description:** Add the narrow profile-draft write and project site profile, authorization, readiness, warnings, and launch into focused workspace steps with one authoritative summary and stable Back/Continue/Review/Launch positions.
- **Acceptance Criteria:** `PATCH /api/instance-onboarding/profile` requires authentication, active setup-secret authority, setup rate limiting, manual validation, safe RFC 7807, audit, and `save-profile` HAL; it persists no secret or generic route history. UI saves only when the HAL relation exists; status/HAL drives step completion/navigation; deployment mode is read-only; Local/Cerbos skip/remediation survives; warnings remain nonblocking; completion re-fetches server state before handoff.
- **Dependencies:** 9.3
- **Effort:** XL
- **Validation:** bUnit state permutations, request-count/deduplication tests, Application/API completion tests, SingleTenant and MultiTenant-zero-tenant journeys.

#### Task 9.5 — Reuse The Workspace For Optional Tenant Onboarding
- **Type:** modify/test
- **Layer:** Blazor Client
- **Files:** `TenantOnboarding.razor`, CSS/service/tests, shared workspace step definition
- **Description:** Apply the same workspace grammar to the separate tenant-scoped journey after MultiTenant platform launch without adding it to instance-launch progress.
- **Acceptance Criteria:** trusted tenant context is always visible; platform and tenant summaries are not mixed; tenant drift fails closed; locked settings and HAL actions remain authoritative; first tenant stays optional.
- **Dependencies:** 9.4
- **Effort:** L
- **Validation:** tenant page/service/API tests and desktop/mobile/RTL visual states.

### Phase 10: Reference-Fidelity Verification And Handoff

- **Goal:** Prove the unified experience against the supplied prototype and repository quality gates.
- **Depends on:** Phase 9.
- **Acceptance:** `/visual-qa` passes in reference-fidelity mode at 375/768/1280px for representative access, provider, instance, readiness, authorization-remediation, and tenant states; required tests/build/docs pass or unrelated blockers are attributed.

#### Task 10.1 — Run Final UX, Security, Test, And Documentation Gates
- **Type:** test/ops/docs/review
- **Layer:** All touched layers
- **Files:** all Phase 9 files plus `docs/DESIGN.md`, `docs/BLAZOR.md`, operator docs only where visible behavior changes, and all three workstream docs
- **Description:** Run component/BFF/API/Application/Architecture gates, real-stack mode journeys, assisted accessibility where available, and dual-review visual QA; then refresh the handoff.
- **Acceptance Criteria:** no reference-fidelity, responsive, focus, secret, HAL, tenant, completion, or recovery regression remains; visual evidence is fresh and not inherited from the pre-workspace UI.
- **Dependencies:** 9.5
- **Effort:** L
- **Validation:** Section 14 plus `/visual-qa` and final structured review.

## 7. Testing Strategy

| Requirement | Test level | Project/files |
|---|---|---|
| Setup secret stays server-owned | BFF/API integration | `Explore.Blazor.IntegrationTests`; `Event.API.IntegrationTests` |
| SingleTenant and MultiTenant routing | bUnit/service tests | `Explore.Blazor.Client.Tests` startup/onboarding/route-guard tests |
| Required blockers vs warnings | Application unit + bUnit | `Event.Application.UnitTests`; onboarding page tests |
| Instance completion/idempotent retry | Application/API integration | complete-handler and controller tests |
| Platform vs tenant authority denial | API integration | onboarding controller tests, including new tenant controller file if needed |
| Tenant locks/isolation | Application/API integration | tenant settings/onboarding tests |
| RFC 7807 rendering | API integration + bUnit | ProblemDetails contract and validation UI tests |
| HAL affordance behavior | architecture/API/client tests | authorization parity and component tests, including a completed authentication-provider task that retains **Manage authentication** only when its authoritative affordance is present |
| Cerbos/local behavior | Infrastructure/Application/API tests | current provider/config/sync/readiness tests |
| Workspace information architecture | bUnit + reference-fidelity visual QA | header/progress/main/aside/footer structure, current-step state, conditional visible-step count, desktop/mobile geometry |
| Cross-route resume/exit | Application/API/BFF/client tests | setup-secret handoff, OIDC return, profile draft write and `save-profile` HAL, authoritative earliest-incomplete routing, dirty discard, no browser secret persistence |
| Accessibility/localization | bUnit + manual | headings, landmarks, `aria-current`, live regions, keyboard, screen reader, RTL, dark/light, forced colors, long translations |
| Architecture and docs contracts | architecture tests | `Event.Architecture.Tests` |

No test may be deleted to pass. Integration tests use real infrastructure according to repository policy. If the API contract changes, regenerate NSwag as a discrete step and test the generated client; never hand-edit it.

## 8. Documentation, Configuration, And Operations Impact

- Required design contract: update `docs/DESIGN.md` with the `OnboardingWorkspace` primitive, states, geometry, responsive behavior, motion, and accessibility rules before UI code.
- Required intent docs: `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md` only where operator-visible workflow text changes.
- Also expected: `docs/DEPLOYMENT_MODES.md`, `docs/BLAZOR.md`; `docs/API_CHANGELOG.md` only for a contract change.
- `docker-compose.yml`, `docker/**`, and `src/Explore.AppHost/AppHost.cs` should remain unchanged unless the final UX exposes a verified configuration omission. If changed, validate `docker compose config` and Aspire topology.
- Document `DEPLOYMENT_MODE`, `CONTROL_PLANE_PUBLIC_ORIGIN`/admin host behavior, setup-secret source/rotation, Cerbos endpoints and sync, backup of persisted bootstrap/settings state, and failure recovery.
- Source-aware metadata should be reused if current DTOs expose ownership. A general configuration-source registry is not planned.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Setup-secret requests remain rate-limited/audited and secrets remain redacted.
- OIDC tokens remain in HttpOnly BFF cookies and are forwarded as bearer tokens only by server code.
- JWT validation continues to enforce `aud` and `azp`; user ID extraction remains `sub` → name identifier → `sid`.
- Platform and tenant authority are enforced server-side; client affordances use HAL.
- Tenant context comes only from trusted BFF header, custom domain, or subdomain resolution; unresolved MultiTenant requests fail closed.
- Completion writes retain idempotency, transactional behavior, bounded audit logging, and safe retries.
- Provider endpoint validation retains SSRF protections and write-only/redacted credentials.
- Error details must not include secrets, raw upstream payloads, DNS tokens, or internal stack traces.
- No new public endpoint, policy bypass, impersonation mechanism, or destructive repair is part of this plan.

## 10. Product Considerations

| Concern | Applicability | Plan |
|---|---|---|
| Multi-tenancy | Applicable, central | Separate platform launch from tenant launch; explicit tenant context; fail closed. |
| Federation | Needs investigation only | Do not add federation onboarding. Ensure route/API changes do not block future federation contracts. |
| Localization | Applicable | Resource-backed task text, locale-safe errors, long-string and RTL QA. |
| Accessibility | Applicable, mandatory | WCAG 2.2 AA, one h1/PageTitle, semantic task list, keyboard, focus/live regions, non-color status, logical CSS. |
| White-label/product | Applicable | Site profile/branding tasks use existing settings and hierarchical locks; no duplicate editor. |
| Cultural filtering/prayer/spatial features | Not directly applicable | They remain post-launch product capabilities, not launch blockers. |
| Self-hosting | Applicable, central | Operator-owned mode, secret, host, provider setup, recovery, backup, and rerun docs. |

## 11. Observability And Operations

- Reuse existing structured setup audit events and OpenTelemetry; never log DTOs containing credential fields.
- Record task-load and completion failure categories, not secrets or raw provider responses.
- Existing health endpoints remain the operator source for database, Cerbos, SMTP, storage, and dependent-service readiness. The UI may link to remediation but should not duplicate health logic.
- Measure request count and latency during the composition decision gate.
- Troubleshooting must map each blocker to a health/configuration check, safe recovery action, and log/metric name where available.
- Partial completion must remain visible after refresh; setup lock state and provider ownership source must be explicit.

## 12. Migration And Compatibility Plan

- No EF Core migration, seed change, or backfill is currently planned.
- Existing route URLs should remain as focused task destinations unless user review approves route removal. Avoid compatibility shims; update route callers atomically if routes are consolidated.
- Existing persisted onboarding/bootstrap/tenant progress remains authoritative and needs no reset.
- Deploy backend-compatible UI first where possible. If an API snapshot becomes justified, deploy the additive endpoint/generated client before switching the UI, then remove obsolete composition only in the same pre-v1 workstream.
- Compose/Aspire configuration changes, if any, deploy before onboarding UI that depends on them.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Setup, platform, and tenant authority collapse in generic task code | Medium | Critical | Authority matrix; display-only component; server/HAL gating | Cross-scope action appears or denial test fails | 1.2, 2.2, 4.1 |
| First tenant accidentally blocks MultiTenant platform launch | Medium | High | Separate completion states and routes | Platform cannot reach control plane with zero tenants | 1.1, 3.2 |
| Secret/provider response leaks through problem details or logs | Low | Critical | Existing redaction; explicit tests; bounded logging | Secret-like value in DTO/log/trace snapshot | 1.2, 5.1 |
| Client-composed state flickers or contradicts during refresh | Medium | Medium | Cancellation/deduplication; decision gate | Reproduced inconsistent task status or excessive calls | 3.1, 6.1 |
| Local claims replace HAL/API authority | Medium | High | Ban role-derived affordances; parity tests | Button visible without link or API denies expected action | 1.2, 3.1, 4.1 |
| Duplicate onboarding and post-launch forms drift | Medium | Medium | Link/reuse existing editors | Same setting has two validators or contracts | 2.1, 3.1, 4.1 |
| Accessibility/RTL regresses in custom task list | Medium | High | Semantic component and manual QA | Failed keyboard, screen reader, contrast, or RTL check | 2.2, 8.2 |
| Workspace becomes a client-side source of truth | Medium | Critical | Display-only journey projection; route/API status and HAL remain authoritative | Step marked complete after visit or action shown without link | 9.1-9.5 |
| Fixed prototype step count breaks deployment-managed skips | Medium | High | Compute visible steps and `n of m` from current mode/state | Progress count disagrees with reachable steps | 9.1, 9.4 |
| Save and exit leaks secrets or implies unsaved data was persisted | Medium | Critical | Server-save only; no local/session storage; dirty discard confirmation | Secret/draft key in browser storage or false saved message | 9.1, 9.3 |
| Desktop summary rail overwhelms mobile or RTL | Medium | High | Responsive in-flow disclosure, logical CSS, 375/768/1280 visual QA | overflow, obscured footer, wrong reading/focus order | 9.2, 10.1 |
| Prototype is copied literally and conflicts with ISLAMU tokens/brand | Low | Medium | Use geometry/hierarchy only; update `docs/DESIGN.md`; token audit | Oppworx copy/colors/fixed eight steps appear | 9.1, 9.2 |
| Cerbos scope expands into unsupported diagnostics | Low | Medium | Explicit deferred list and decision gate | New inventory/decision route appears without intent | 7.1 |
| Stale paused plan redirects implementation | Medium | Medium | Cross-reference and supersession statement | Agent starts invitation/lifecycle/wizard task | 0.1, all docs |
| Unrelated dirty working-tree changes contaminate verification | High currently | High | Isolate paths, capture status, never revert user work | Diff includes managed-control-plane or `.codex` files | 0.1, 8.1 |

## 14. Success Metrics And Definition Of Done

Functional:

- SingleTenant and MultiTenant administrators experience one visually continuous, route-aware workspace after setup-secret validation.
- Every step shows persistent journey orientation: current step, conditional progress, completed/upcoming summary, contextual explanation, and stable navigation.
- Desktop uses the prototype-informed main/summary layout; tablet/mobile use an accessible in-flow summary without viewport overflow or hidden actions.
- `/setup` remains separately protected and no privileged credential reaches browser ownership.
- SingleTenant launches to events/settings.
- MultiTenant launches the platform/control plane without requiring a tenant; first-tenant onboarding is optional and tenant-scoped.
- All task status and actions are server-authoritative; blockers, warnings, retries, and partial progress survive refresh.
- Completed authentication-provider setup remains manageable through the focused provider page when authorized, so operators can create, repair, or reconcile the Keycloak realm without making the task appear incomplete.
- Deployment-only Keycloak produces sanitized detected/enabled/authority/client-ID state and configured status without returning its client secret; the operator can still enter the full provider editor.
- Existing Cerbos/local configuration and post-launch editors are reused.
- Explicit Local skips authorization setup with zero Cerbos calls; explicit Cerbos is ready only after instance PDP verification and instance Admin API policy publication, while Keycloak management remains independently reachable.
- Exit/resume is honest and secure: only confirmed server writes are called saved, dirty unsaved input is confirmed before discard, and secrets never enter browser draft storage.

Quality gates:

```bash
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet build --configuration Release --verbosity quiet
```

Also required: clean diagnostics on modified files; docs/context tests; manual keyboard, screen-reader, RTL, dark/light, forced-colors, long-copy, refresh/retry, dirty-exit, SingleTenant, MultiTenant-zero-tenant, first-tenant, and provider-failure smoke tests. Run `/visual-qa` in reference-fidelity mode at 375/768/1280px. Run `docker compose config` and an Aspire smoke only if deployment configuration changes.

Current evidence applies only to the implemented foundation, not the missing unified workspace. The latest broad Release build passed with zero errors; Application passed 2,205/2,205; serialized Client passed 1,618 with one governed skip; and Blazor Integration passed 241/241. API passed 1,722/1,733 with eight failures and three skips; Architecture passed 263/268 with four failures and one governed skip. Existing provider/security coverage remains valuable. Previous browser screenshots and visual `PASS` describe the fragmented pre-Phase-9 UI and cannot satisfy the new reference-fidelity gate.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. Before any slice, read this plan, `onboarding-ux-refactor-context.md`, and `onboarding-ux-refactor-tasks.md`.
2. Start with the highest-priority incomplete task unless the user overrides it.
3. Before editing, re-check repository status and preserve all unrelated user changes.
4. After every meaningful task or discovery, update this plan if decisions/scope/risks changed, update context with current state/files/validation/blockers/next action, and update the checklist immediately.
5. Reclassify conditional intents before introducing an endpoint, HAL link, API contract, CQRS handler, or Cerbos policy change.
6. Do not report completion unless the three docs match actual code and validation.
7. If validation fails, record the failure, root cause if known, and recovery action.
8. Before pause, handoff, context reset, or PR creation, refresh all three files and add a dated handoff.
9. Every user summary must teach what changed: patterns, libraries/infrastructure, important files/classes, control flow, security/tenant/idempotency/error handling conventions, verification, remaining work, and next action.

## 16. Progress Reporting Contract

- **Implemented:** A medium-sized developer teaching summary naming the task-list/BFF/CQRS/HAL patterns, important files/components/handlers, authority and data flow, and why the design follows repository conventions.
- **Verified:** Exact diagnostics, tests, build, and manual checks run with results.
- **Remaining:** Unfinished tasks, risks, or deferred decisions.
- **Next:** One concrete next slice.
- **Docs updated:** Whether plan/context/tasks reflect reality, with reason if not.

## 17. Potential Risks & Unknowns

The most likely hard part is not rendering a task list; it is deriving a stable, comprehensible view from several authoritative contracts without creating a second source of truth. The initial no-snapshot decision keeps the architecture smaller, but implementation must measure request behavior and test refresh races rather than defend that decision dogmatically. The second hard part is MultiTenant authority: a platform administrator may be allowed to initiate tenant creation while tenant onboarding actions remain tenant-scoped. Every task must state which authority and tenant context it requires, and missing context or HAL affordance must fail closed.

The current working tree contains unrelated managed-control-plane changes. They remain preserved and outside this workstream; `.codex/config.toml` currently has no diff. Final verification evidence must continue to distinguish unrelated baseline failures from onboarding behavior.

The production authentication detection path is implemented in `AuthProviderConfigurationService`, while authorization intent/reconciliation is implemented across `AuthorizationProviderConfigurationService`, `AuthorizationProviderBootstrapState`, `CerbosPolicyBootSyncRunner`, and `CerbosPolicyPackageService`. API compatibility mapping, Compose/Aspire propagation, generated contracts, server-derived ownership, and focused regression coverage make both environment-driven journeys authoritative. The service boundaries are resolved; real deployed Keycloak/browser realm management and fresh authorization bootstrap/browser journeys remain open runtime gates.
