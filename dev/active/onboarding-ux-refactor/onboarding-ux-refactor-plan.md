<!-- ABOUTME: Repository-grounded implementation plan for coherent single-tenant and multi-tenant administrator onboarding. -->
<!-- ABOUTME: Preserves setup and authorization boundaries while replacing fragmented post-auth flows with an authoritative task list. -->

# Onboarding UX Refactor — Implementation Plan

> **Implementation update (2026-07-12):** Production Keycloak management remains reachable with deployment secrets. Explicit authorization-provider intent, bounded instance-only Local/Cerbos reconciliation, runtime precedence, server-authoritative route skipping, the single-column Local-default UI, and canonical docs are implemented with focused green coverage. Tasks 6.3, 8.1, 8.2, and 8.3 remain open for live QA, required suites, and final handoff.
> **Scope expansion recorded:** The production fixes necessarily touch shared Application/Infrastructure provider services, API configuration and background reconciliation, AppHost/Compose propagation, generated contracts, and focused tests in addition to the original Blazor-first paths. This is the smallest shared-source change that makes both detected-provider behaviors authoritative.

Last Updated: 2026-07-12 Europe/Brussels

## 0. Planning Metadata

- **Request:** Implement the user-approved coherent administrator launch journey for SingleTenant and MultiTenant deployments and keep its three persistent workstream documents synchronized.
- **Task directory:** `dev/active/onboarding-ux-refactor/`
- **Planning status:** User-approved — implementation, focused tests, and canonical docs are complete; live authorization QA, required suites, authenticated runtime verification, and final handoff remain open
- **Primary matched intent:** `external-infrastructure-bootstrap` — Automate external infrastructure bootstrap or onboarding
- **Relevant skills loaded:** `senior-cto-feedback`, `auth-patterns`, `blazor-bff-patterns`, `clean-architecture-rules`, `blazor-ui-conventions`, `accessibility`, `ponytail`
- **Relevant rules loaded:** `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/application-layer.md`, `.claude/rules/blazor-server.md`, `.claude/rules/blazor-client.md`, `.claude/rules/tests.md`
- **Layers touched:** API HAL/controllers/background services/configuration, Application, Infrastructure, Blazor BFF, Blazor Client, AppHost/Compose deployment configuration, generated OpenAPI client/contracts, Tests, Docs, and dev workstream records. Domain and Persistence are unchanged.
- **Estimated complexity:** XL. The visual refactor is moderate, but the work crosses a pre-auth setup-secret boundary, authenticated platform and tenant authority scopes, two deployment modes, Cerbos/local authorization, generated API contracts, BFF forwarding, accessibility, operator documentation, and five mandatory test projects.
- **Implementation boundary:** The user approved full implementation on 2026-07-12. The baseline Release build passed before product edits; unrelated managed-control-plane work and any `.codex` changes remain outside this workstream.

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

The following conditional intents remain deferred unless later evidence activates them:

- `add-get-endpoint` and `add-cqrs-handler`: activate only if the evidence threshold for an aggregate onboarding snapshot is met.
- `add-write-endpoint`: activates only if an existing completion/configuration endpoint cannot safely support an accepted task.
- `cerbos-policy-change`: activates only if policy semantics change; Cerbos configuration or package sync alone does not imply a policy change.

## 1. Executive Summary

The platform already has the secure and authoritative backend pieces needed for launch: a setup-secret BFF boundary, authentication and authorization provider configuration, instance status and preflight queries, idempotent completion handlers, tenant onboarding state, post-launch settings editors, RFC 7807 errors, and Cerbos/local authorization support. The main problem is orchestration and presentation: setup, provider configuration, instance onboarding, tenant onboarding, and post-launch administration appear as separate route-specific experiences whose relationship is difficult for operators to understand.

The target is one conceptual launch journey with two security contexts:

1. `/setup` remains a separate pre-auth operator gateway protected by the setup secret.
2. After authentication, administrators see a single-column conditional task list derived from server-authoritative state.

SingleTenant launch completes the instance and default-tenant setup, then hands off to events or existing settings. MultiTenant launch completes the platform first and hands off to the control plane; creating or onboarding a first tenant is a separate optional task with tenant-scoped authority. `DEPLOYMENT_MODE` and the dedicated admin host remain operator-owned deployment configuration, not onboarding choices.

The implementation should compose existing endpoints initially. A new aggregate snapshot endpoint is deliberately deferred unless tests or traces demonstrate inconsistent multi-call state, a reproducible race, or unacceptable request amplification. Cerbos inventory and policy decision-test endpoints are also deferred because no current operator contract or implementation supports them.

### 1.1 Implemented Outcome — 2026-07-12

- Instance and tenant onboarding now use one semantic, responsive, display-only task-list component while parent pages retain state and workflow ownership.
- Existing instance and tenant status endpoints are HAL resources. Server policies emit permission/setup-secret-checked `complete` and management relations; Blazor exposes actions only when those relations are present.
- A configured Keycloak task stays complete and nonblocking while retaining **Manage authentication**. It opens `/onboarding/auth-provider` before launch and `/admin/instance/settings?section=auth-providers` after launch so operators can create, diagnose, repair, reconcile, or rotate the realm/client configuration.
- Deployment-detected Keycloak retains an explicit **Configure Authentication Providers** action instead of forcing login. The focused editor exposes manual realm values plus additive patch-existing/create-if-missing bootstrap actions, reads only a redacted contract, and never prefills stored secrets into browser controls.
- `AUTHORIZATION_PROVIDER=local|cerbos` is a validated deployment-owned selector shared by onboarding and runtime authorization. Local skips the choice page without contacting Cerbos. Cerbos uses bounded single-flight background work to verify the instance PDP and publish only to the instance Admin API, stays fail-closed until ready, skips the chooser while pending/ready, and exposes locked remediation after final failure. Blank/unset keeps manual onboarding with Local selected and Cerbos behind native progressive disclosure.
- Completion remains server-authoritative: both pages submit through existing commands, re-fetch status, and reject unconfirmed completion or tenant drift.
- The BFF always removes browser-controlled setup-secret headers and forwards the trusted server/session secret only to exact or slash-delimited instance-onboarding endpoint paths; query-string and near-route lookalikes fail closed.
- Endpoint composition was retained after request-count and overlapping-refresh tests reproduced no snapshot escalation trigger.

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
| Setup, startup, provider, instance, tenant, login, logout, and settings routes already exist. | Verified: `src/Explore.Blazor.Client/Routes.razor` | High | Route orchestration should be simplified, not rebuilt. |
| Setup secret is resolved and forwarded by trusted server code rather than browser code. | Verified: `src/Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs`; `src/Explore.Blazor/Services/SetupSecretResolver.cs`; `src/Explore.Blazor/Services/SetupSecretForwardingHandler.cs` | High | Preserve this trust boundary. |
| Current post-auth onboarding is split across instance, auth provider, authz provider, and tenant pages. | Verified: `src/Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor`; `AuthProviderConfiguration.razor`; `AuthorizationProviderConfiguration.razor`; `TenantOnboarding.razor` | High | These become task destinations or focused task pages. |
| Startup routing already distinguishes incomplete and completed onboarding. | Verified: `src/Explore.Blazor.Client/Pages/Onboarding/StartupGate.razor`; `src/Explore.Blazor.Client/Services/StartupRoutingService.cs` | High | Preserve server-derived routing behavior. |
| Standard application chrome is hidden for setup/startup/onboarding routes. | Verified: `src/Explore.Blazor.Client/Layout/MainLayout.razor.cs`; `src/Explore.Blazor.Client/Layout/SetupLayout.razor` | High | New routes must remain in the same shell policy. |
| Deployment mode is operator-controlled and persisted at completion; the client value is not authoritative. | Verified: `src/Explore.Infrastructure/Services/DeploymentModeProvider.cs`; `src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`; `docs/DEPLOYMENT_MODES.md` | High | Never render it as a chooser. |
| Instance preflight already distinguishes blockers and operational warnings. | Verified: `src/Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetOnboardingPreflightQueryHandler.cs` | High | Map blockers to required tasks and warnings to remediation/optional tasks. |
| Instance completion creates required bootstrap state, users/admin grants, and default tenant behavior. | Verified: `src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs` | High | Do not duplicate orchestration in the client. |
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

1. **Fragmented journey:** Existing routes are valid individually but do not explain the complete launch sequence or what remains.
2. **Mode ambiguity:** A UI that treats deployment mode as form input would conflict with the operator-owned source of truth and produce misleading state.
3. **Scope ambiguity in MultiTenant:** Platform readiness and first-tenant readiness are separate outcomes; coupling them would block the control plane and blur authorities.
4. **Duplicate editor risk:** Building onboarding-only settings forms would drift from post-launch editors and validation contracts.
5. **Local authority risk:** Deriving tasks from claims or client assumptions would violate HAL and tenant fail-closed rules.
6. **Recovery discoverability:** Existing backend retry/idempotency support is stronger than the current UX explanation of failures, reruns, and partially completed provider setup.
7. **Test gap:** Page tests do not yet encode the target cross-route journey and authority matrix.
8. **Stale planning overlap:** The paused enterprise tenant workstream still describes a wizard and stale backend gaps; an implementation agent could accidentally absorb that scope.

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
/setup (pre-auth, setup-secret authority, separate shell)
  validate setup -> configure/verify auth provider -> create first administrator
                    |
                    v
OIDC login (BFF cookie; tokens never reach browser)
                    |
                    v
Post-auth Setup Overview (server-derived conditional task list)
  site profile -> authorization provider -> preflight -> review/launch
                    |
          +---------+----------+
          |                    |
   SingleTenant           MultiTenant
   events/settings        platform/control-plane settings
                               |
                               +-- optional first-tenant task
                                   (new tenant context and tenant authority)
```

### 3.2 Task-List Semantics

- One responsive column; no competing stepper, side panel, or progress model.
- Every item has title, concise description, required/optional label, status, action/remediation, and authority scope.
- Status is derived from API state, not route history or local completion flags.
- A blocking preflight result prevents launch; warnings remain visible but do not masquerade as blockers.
- Refresh/retry always re-fetches authoritative state.
- Completion is safe to retry through existing idempotency and handler behavior.
- Errors render RFC 7807 detail and field validation without exposing credentials.
- Actions appear only when the server contract/HAL affordance permits them.
- Completion status does not remove an independently authorized ongoing-management action. In particular, a configured authentication-provider task keeps a HAL-authorized **Manage authentication** affordance to the focused setup page before launch and the admin provider editor after launch so operators can diagnose, repair, or reconcile the Keycloak realm; missing or failed authoritative state still fails closed.
- Authentication configuration or secret presence never removes the provider-management surface. A detected Keycloak fast path retains the full setup editor, and postlaunch management remains the HAL-authorized admin editor.
- Authorization uses separate semantics: explicit deployment intent bypasses the provider-choice page, while blank/unset intent renders the Local default and advanced Cerbos disclosure. Failed deployment-managed Cerbos remains reachable as locked remediation from the authoritative instance task.

### 3.3 Authority Matrix

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

### D1 — Preserve Two Security Contexts In One Conceptual Journey

- **Decision:** Keep `/setup` pre-auth and setup-secret protected; begin the unified task-list shell only after OIDC authentication.
- **Why:** A visually unified flow must not collapse operator bootstrap and authenticated administration authorities.
- **Alternatives:** A single route/shell carrying the setup secret; rejected because it expands browser exposure and confuses authority.
- **Consequences:** Shared visual language is allowed, but session state and actions remain separate.
- **Files/layers:** Setup page, BFF setup endpoints/services, startup gate, onboarding client pages, tests.

### D2 — Conditional Task List, Not A Stepper

- **Decision:** Use a single-column task list with focused task routes/pages.
- **Why:** Existing tasks can be completed or revisited independently, warnings are not linear steps, and MultiTenant adds conditional tasks.
- **Alternatives:** One giant wizard or competing side navigation; rejected due to duplicated state and poor recovery.
- **Consequences:** The task component is display-only; backend state determines status.

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

### Phase 8: Final Verification And Handoff

- **Goal:** Prove the refactor, docs, boundaries, and operational path are complete.
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
| Accessibility/localization | bUnit + manual | headings, roles, live regions, keyboard, screen reader, RTL, dark/light, long translations |
| Architecture and docs contracts | architecture tests | `Event.Architecture.Tests` |

No test may be deleted to pass. Integration tests use real infrastructure according to repository policy. If the API contract changes, regenerate NSwag as a discrete step and test the generated client; never hand-edit it.

## 8. Documentation, Configuration, And Operations Impact

- Required intent docs: `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`.
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
| Cerbos scope expands into unsupported diagnostics | Low | Medium | Explicit deferred list and decision gate | New inventory/decision route appears without intent | 7.1 |
| Stale paused plan redirects implementation | Medium | Medium | Cross-reference and supersession statement | Agent starts invitation/lifecycle/wizard task | 0.1, all docs |
| Unrelated dirty working-tree changes contaminate verification | High currently | High | Isolate paths, capture status, never revert user work | Diff includes managed-control-plane or `.codex` files | 0.1, 8.1 |

## 14. Success Metrics And Definition Of Done

Functional:

- SingleTenant and MultiTenant administrators can identify all required launch work from one post-auth task list.
- `/setup` remains separately protected and no privileged credential reaches browser ownership.
- SingleTenant launches to events/settings.
- MultiTenant launches the platform/control plane without requiring a tenant; first-tenant onboarding is optional and tenant-scoped.
- All task status and actions are server-authoritative; blockers, warnings, retries, and partial progress survive refresh.
- Completed authentication-provider setup remains manageable through the focused provider page when authorized, so operators can create, repair, or reconcile the Keycloak realm without making the task appear incomplete.
- Deployment-only Keycloak produces sanitized detected/enabled/authority/client-ID state and configured status without returning its client secret; the operator can still enter the full provider editor.
- Existing Cerbos/local configuration and post-launch editors are reused.
- Explicit Local skips authorization setup with zero Cerbos calls; explicit Cerbos is ready only after instance PDP verification and instance Admin API policy publication, while Keycloak management remains independently reachable.

Quality gates:

```bash
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet build --configuration Release --verbosity quiet
```

Also required: clean diagnostics on modified files; docs/context tests; manual keyboard, screen-reader, RTL, dark/light, refresh/retry, SingleTenant, MultiTenant-zero-tenant, first-tenant, and provider-failure smoke tests. Run `docker compose config` and an Aspire smoke only if deployment configuration changes.

Current evidence: the latest broad Release build passed with zero errors; Application passed 2,205/2,205; serialized Client passed 1,618 with one governed skip; and Blazor Integration passed 241/241. API passed 1,722/1,733 with eight failures and three skips; Architecture passed 263/268 with four failures and one governed skip. Current authorization-focused coverage passes 13 configuration/options, 19 provider/single-flight, 22 policy-package target-isolation, 23 runtime-provider, four boot-runner, 13 page, 34 client-service, ten admin-layout, nine Setup, and ten authentication-source tests; Client/API/Infrastructure Release builds have zero errors. Keycloak producer/service/TestServer coverage also remains green and proves secret-free reads plus persistent management access. Previous browser-focused desktop/mobile/RTL/dark/long-text/focus/disclosure checks passed with an independent `PASS`, but fresh authorization-page real-stack QA is still required. The prior `EMFILE`, `.slnx`, and migration blockers are resolved; remaining runtime issues are S3 readiness, unavailable assisted screen-reader tooling, and incomplete authenticated journeys.

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
