<!-- ABOUTME: Operational context for the Keycloak bootstrap automation workstream. -->
<!-- ABOUTME: Tracks current decisions, source-grounded files, validation baseline, and handoff notes. -->

# Keycloak Bootstrap Automation — Context

Last Updated: 2026-06-01 Europe/Brussels

## SESSION PROGRESS (2026-06-01 Europe/Brussels)

### ✅ COMPLETED
- Planning docs created after verifying they were missing.
- Current-state evidence captured from Compose, realm export, onboarding controller, auth-provider service, BFF dynamic auth, setup-secret forwarding, and docs.
- Baseline build was run in this session: `dotnet build --configuration Release --verbosity quiet` completed with warnings and `0 Error(s)`.
- Phase 1 Compose-managed Keycloak init job implemented:
  - Added `docker/keycloak/keycloak-init.sh` with two-line ABOUTME header, fail-closed BFF secret validation, optional local default escape hatch, idempotent `kcadm.sh` client lookup/update, and redacted logs.
  - Added `keycloak-init` one-shot service to `docker-compose.yml` using `quay.io/keycloak/keycloak:26.1.2` with no exposed ports.
  - Updated API and Blazor Compose dependencies to require `keycloak-init` `service_completed_successfully` before startup proceeds.
  - Updated `docs/SELF_HOSTING.md`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`, and `docs/TROUBLESHOOTING.md` for the no-manual-secret-sync flow.
- Phase 1 verification updated:
  - `bash -n docker/keycloak/keycloak-init.sh` passed.
  - `docker compose config --quiet` passed.
  - `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and existing warnings.
  - Focused AgentContext schema/link tests passed with TUnit `--treenode-filter` syntax.
- Phase 2 Application-layer Keycloak bootstrap contract implemented:
  - Added `KeycloakBootstrapRequestDto`, `KeycloakBootstrapResultDto`, and `KeycloakBootstrapMode` to model setup-time external Keycloak bootstrap without Infrastructure/API details.
  - Added `IKeycloakBootstrapService` as the Application-owned contract that Infrastructure will implement.
  - Added `KeycloakBootstrapRequestDtoValidator` with manual handler usage, URL/input validation, control-character rejection, and secret length limits.
  - Added `BootstrapKeycloakRealmCommand` and handler. The handler calls the bootstrap service, persists only normal runtime Keycloak auth-provider configuration on success, reloads JWT authority, and never persists the one-time admin credential.
  - Added Application unit tests for validation failure, bootstrap failure, no admin-secret persistence, safe response messages, and successful runtime auth config persistence.
- Phase 2 verification updated:
  - LSP diagnostics passed for `Explore.Application` and the new Application unit test file.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed.
  - `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and existing warnings.
  - Focused Architecture `CleanArchitectureTests` and `NamingConventionTests` passed.
- Phase 3 Infrastructure Keycloak Admin API adapter implemented:
  - Added `Explore.Infrastructure/Services/Keycloak/KeycloakBootstrapService.cs` implementing `IKeycloakBootstrapService` with a named `HttpClient`, one-time admin token acquisition, realm exists/create handling, client lookup/create, client-secret update, safe failure categories, and redacted diagnostics.
  - Added URL safety checks for unsupported schemes, user-info/query/fragment tricks, localhost/loopback/link-local/unspecified/multicast IP literals, while preserving self-host/internal DNS hostnames.
  - Registered `KeycloakBootstrapService.HttpClientName` with a 30-second timeout and scoped `IKeycloakBootstrapService` registration in `Explore.Infrastructure/InfrastructureServicesRegistration.cs`.
  - Added `Explore.Infrastructure.Tests/Infrastructure/KeycloakBootstrapServiceTests.cs` with fake HTTP handler coverage for create-realm success, patch-existing success, missing realm in patch mode, admin auth failure, unsafe URL rejection, bearer-token use, and no secret/admin credential leakage in safe results.
- Phase 3 verification updated:
  - LSP diagnostics passed for the new Infrastructure service and tests.
  - `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed, 386/386 tests.
  - `dotnet build --configuration Release --verbosity quiet` passed, 25 projects, 0 errors, existing warnings.
  - Focused Architecture `CleanArchitectureTests` and `NamingConventionTests` passed after Phase 3.
- Phase 4 setup-gated API/BFF/Blazor service transport slice implemented:
  - Added `BootstrapInstanceOnboardingKeycloakRealm` route name and `POST /api/InstanceOnboarding/auth-provider-configuration/keycloak-bootstrap` to `InstanceOnboardingController`.
  - The API route is `[AllowAnonymous]`, `[SetupSecretRequired]`, endpoint-classified as admin, returns `BaseCommandResponse<Guid>`, dispatches `BootstrapKeycloakRealmCommand`, and logs no bootstrap credentials.
  - Added explicit Keycloak bootstrap path coverage to `SetupSecretForwardingHandler` so browser-supplied `X-Setup-Secret` is stripped and the trusted setup secret resolver value is forwarded.
  - Added `BootstrapKeycloakRealmAsync` to the Blazor Refit API and `InstanceOnboardingService`, plus a local `KeycloakBootstrapRequestModel` for the client boundary.
  - Successful Blazor service calls now refresh dynamic auth schemes through `/bff/auth/refresh-schemes`; failed calls do not refresh.
  - Added focused API integration, BFF forwarding, and Blazor client service tests for the new path.
- Phase 4 transport-slice verification updated:
  - LSP diagnostics passed for the modified API controller, API integration tests, Blazor service, Blazor client service tests, and BFF forwarding tests.
  - Focused `Event.API.IntegrationTests` for `InstanceOnboardingControllerTests` passed sequentially.
  - Focused `Explore.Blazor.IntegrationTests` for `SetupSecretForwardingHandlerTests` passed sequentially.
  - Focused `Explore.Blazor.Client.Tests` for `InstanceOnboardingServiceTests` passed.
  - `dotnet build --configuration Release --verbosity quiet` passed with 25 projects, 0 errors, and existing warnings.
  - Focused Architecture `ApiContractArchitectureTests` and `EndpointClassificationArchitectureTests` passed.
- Phase 4 onboarding UI mode implemented:
  - `AuthProviderConfiguration.razor` now offers manual OIDC configuration or Keycloak bootstrap configuration when Keycloak is enabled and not environment-managed.
  - Bootstrap mode collects Keycloak base URL, realm, Blazor BFF client ID/secret, optional API client ID/secret, and one-time admin username/password.
  - The UI labels bootstrap admin credentials as one-time/not stored, calls `InstanceOnboardingService.BootstrapKeycloakRealmAsync`, relies on the service-level auth-scheme refresh, and clears bootstrap client/admin secrets after submit whether bootstrap succeeds or fails.
  - Added focused `AuthProviderConfigurationSourceTests` to guard the visible bootstrap affordance and secret-clearing call path.

### ✅ PHASE 5 DOCUMENTATION
- Phase 5 operator documentation updated:
  - `docs/SELF_HOSTING.md` now describes Compose-managed Keycloak secret sync and the external-Keycloak setup bootstrap flow, including temporary credential use, setup-secret protection, runtime OIDC persistence, idempotent client lookup/update, and post-success credential retirement.
  - `docs/CONFIGURATION.md` now distinguishes persisted runtime Keycloak config from request-scoped external bootstrap credentials and documents URL safety behavior.
  - `docs/SECRETS.md` now states the external bootstrap admin/service-account credential is one-time operator input and must not be saved to appsettings, environment variables, Infisical, governance settings, logs, traces, screenshots, or support bundles.
  - `docs/TROUBLESHOOTING.md` now covers external bootstrap URL failures, authentication/permission failures, missing realms, client conflicts, rerunning bootstrap, `keycloak-init` recovery, and post-bootstrap login failures.
  - Reviewed `docs/OPERATIONS.md` and `docs/RELEASE_CHECKLIST.md`; no extra startup, health, backup, or release checklist update was needed beyond the existing Keycloak DB backup and Compose sequencing docs.
  - Verification after Phase 5 docs: focused AgentContext link tests passed, focused AgentContext schema tests passed, and `dotnet build --configuration Release --verbosity quiet` passed with 25 projects, 0 errors, and existing warnings.
- Phase 6 automated disposable-Keycloak backend integration smoke implemented:
  - Added `KeycloakBootstrap:AllowLocalUrls` Infrastructure option so production keeps blocking loopback/localhost bootstrap targets by default while Docker/Testcontainers-backed tests can explicitly allow disposable local Keycloak URLs.
  - Updated `KeycloakBootstrapService` to use the option and to update client secrets via the Keycloak client representation endpoint, matching Keycloak 26/kcadm behavior observed in the disposable container.
  - Added `KeycloakBootstrapRealRuntimeTests` in `Event.API.IntegrationTests`, which starts disposable Keycloak, posts to the setup-gated bootstrap endpoint, exercises the real Application handler and Infrastructure Keycloak Admin API adapter, rotates the Blazor client secret, proves the rotated secret works against Keycloak's token endpoint, and verifies persisted runtime auth config does not contain the one-time admin credential.
  - Extended Keycloak test fixtures with base URL and rotated-secret token-client helpers.
  - Verification: focused `KeycloakBootstrapServiceTests`, focused `KeycloakBootstrapRealRuntimeTests`, and release build passed.
- Optional Playwright browser UI bootstrap e2e implemented:
  - Added `KeycloakBootstrapBrowserFlowTests` in `Explore.Blazor.Client.E2ETests`, driven through Aspire AppHost, Testcontainers Keycloak/Postgres, and Playwright.
  - The test persists the setup secret through `/bff/setup-secret`, opens `/onboarding/auth-provider`, enables Keycloak bootstrap mode, submits one-time Keycloak admin credentials through the setup-gated BFF/API path, reaches `/onboarding/authz-provider`, and verifies browser storage contains no tokens.
  - `AuthProviderConfiguration.razor` now uses the correct MudBlazor v9 `@bind-Expanded` API for the Keycloak panel, stable test-id wrappers for browser automation, and immediate bootstrap field binding so the save button tracks typed values.
  - Verification: focused `KeycloakBootstrapBrowserFlowTests` passed.
- Phase 7 future maintenance scope planned after CTO discussion:
  - Added a new future phase for post-onboarding Keycloak doctor, resync, and rotation.
  - The planned workflow is instance-admin gated, additive by default, backup-confirmed before mutation, and uses only temporary Keycloak admin/service-account credentials for the active operation.
  - The plan explicitly rejects destructive realm delete/reimport, permanent Keycloak admin credential storage, and silent override of deployment-managed client secrets.

### 🟡 IN PROGRESS
- No Keycloak bootstrap implementation slice is currently blocked. Phase 7 is planned future work and has not started. Full Architecture remains blocked by unrelated dirty-worktree/source issues documented below.
- A separate post-bootstrap browser login extension is not part of the passing browser UI bootstrap e2e: when the Playwright test was extended through Keycloak auth-code login, Keycloak returned `not_allowed` / `Offline tokens not allowed for the user or client` during `/signin-oidc` token redemption. The setup UI/bootstrap path passes; this login/offline-token behavior should be investigated separately if required.

### ⏭️ NEXT
1. If continuing this workstream, start Phase 7 with the read-only Keycloak realm doctor diagnostic.
2. Triage unrelated full-Architecture failures in separate workstreams if this branch needs a fully green architecture suite.
3. Investigate the separate Keycloak auth-code `offline_access` login failure if post-bootstrap browser login coverage is required.

### ⚠️ BLOCKERS
- No blocker for Compose-managed Keycloak bootstrap, the Phase 2 Application contract, or the Phase 3 Infrastructure adapter.
- Exact least-privilege Keycloak Admin API role composition is documented as required operation capabilities; the disposable-Keycloak smoke currently uses the container admin credential for deterministic test setup.
- Browser-level Playwright UI bootstrap coverage now exists and passes. Extending that same test through post-bootstrap login is blocked by the separate Keycloak `offline_access` auth-code failure described above.

## Quick Resume
1. Read `keycloak-bootstrap-automation-plan.md`.
2. Read `keycloak-bootstrap-automation-tasks.md`.
3. Compose-managed Phase 1, Application-layer Phase 2, Infrastructure Phase 3, Phase 4 API/BFF/UI wiring, Phase 5 operator docs, Phase 6 disposable-Keycloak backend integration smoke, and focused Playwright UI bootstrap e2e are complete; do not rework them unless validation exposes a defect.
4. Phase 7 post-onboarding Keycloak doctor/resync/rotation is planned but not started. Begin with the read-only doctor before any mutation workflow.
5. Keep plan/context/tasks updated after each meaningful implementation slice.
6. Never store Keycloak admin/bootstrap credentials.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `docker-compose.yml` | Existing | DevOps | Compose topology for Postgres, Redis, Keycloak, API, Blazor, optional services. | Now includes `keycloak-init` service. |
| `docker/keycloak/realm-export.json` | Existing | DevOps/Auth | Base ISLAMU realm seed for Compose. | Contains static default client secrets today. |
| `docker/keycloak/ISLAMU-realm.test.json` | Existing | Test/Auth | Test realm seed with users and test secrets. | Keep separate from production/local defaults. |
| `docker/keycloak/keycloak-init.sh` | New | DevOps/Auth | One-shot script to set/sync client secrets after realm import. | Should be idempotent and redacted. |
| `Explore.API/Controllers/InstanceOnboardingController.cs` | Existing | API | Setup-token-gated onboarding endpoints. | Now exposes setup-gated Keycloak bootstrap transport endpoint. |
| `Explore.API/Hateoas/RouteNames.cs` | Existing | API | Route-name constants for endpoint contracts. | Now includes `BootstrapInstanceOnboardingKeycloakRealm`. |
| `Explore.API/Filters/SetupSecretRequiredAttribute.cs` | Existing | API | Gates setup endpoints by trusted `X-Setup-Secret`. | The setup bootstrap route uses it. |
| `Explore.Application/DTOs/Onboarding/KeycloakBootstrapRequestDto.cs` | New | Application | Request model for external Keycloak setup-time bootstrap. | Separates runtime OIDC fields from one-time admin credential. |
| `Explore.Application/DTOs/Onboarding/KeycloakBootstrapResultDto.cs` | New | Application | Safe result model for bootstrap service/handler. | Contains no credentials, tokens, client secrets, or raw provider responses. |
| `Explore.Application/Onboarding/KeycloakBootstrapMode.cs` | New | Application | Bootstrap mode enum. | Lives outside DTO namespace to satisfy DTO naming architecture rules. |
| `Explore.Application/Contracts/Services/IKeycloakBootstrapService.cs` | New | Application | Application contract implemented later by Infrastructure. | Accepts request plus cancellation token and returns safe result. |
| `Explore.Application/DTOs/Onboarding/Validators/KeycloakBootstrapRequestDtoValidator.cs` | New | Application | Manual validator for bootstrap requests. | Rejects blank, invalid, oversized, and control-character inputs. |
| `Explore.Application/Features/InstanceOnboarding/Requests/Commands/BootstrapKeycloakRealmCommand.cs` | New | Application | MediatR command for setup-time bootstrap orchestration. | Now dispatched by the setup-gated API endpoint. |
| `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/BootstrapKeycloakRealmCommandHandler.cs` | New | Application | Validates, calls bootstrap service, persists runtime auth config, reloads JWT authority. | Never persists one-time admin credentials. |
| `Explore.Application/Services/AuthProviderConfigurationService.cs` | Existing | Application | Persists runtime auth-provider config and redacts secrets on reads. | Must not store Keycloak admin/bootstrap credential. |
| `Explore.Application/DTOs/Onboarding/AuthProviderConfigurationDto.cs` | Existing | Application | Current Keycloak/Google/ATProto auth-provider config DTO. | May need companion bootstrap DTO. |
| `Explore.Application/DTOs/Onboarding/Validators/AuthProviderConfigurationDtoValidator.cs` | Existing | Application | Manual validator for auth-provider config. | New bootstrap validator should follow manual pattern. |
| `Explore.Infrastructure/Services/Keycloak/KeycloakBootstrapService.cs` | New | Infrastructure | Implements Keycloak Admin API bootstrap protocol behind the Application contract. | Uses named `HttpClient`, one-time admin token, safe failures, and no raw response-body logging. |
| `Explore.Infrastructure/InfrastructureServicesRegistration.cs` | Existing | Infrastructure | Registers Infrastructure services and named HTTP clients. | Now registers scoped `IKeycloakBootstrapService` and bounded `KeycloakBootstrapClient`. |
| `Explore.Blazor/Services/DynamicAuthSchemeManager.cs` | Existing | BFF | Registers OIDC/OAuth schemes dynamically from env/DB config. | Refresh after successful setup bootstrap. |
| `Explore.Blazor/Extensions/BffAuthEndpoints.cs` | Existing | BFF | Provides `/auth/providers` and `/bff/auth/refresh-schemes`. | Used after auth-provider save/bootstrap. |
| `Explore.Blazor/Services/SetupSecretForwardingHandler.cs` | Existing | BFF | Forwards trusted setup secret to setup endpoints only. | Now explicitly covers the Keycloak bootstrap setup path. |
| `Explore.Blazor.Client/Pages/Onboarding/AuthProviderConfiguration.razor` | Existing | Blazor UI | First-run auth-provider setup page. | Now offers manual OIDC vs Keycloak bootstrap mode and clears one-time bootstrap secrets after submit. |
| `Explore.Blazor.Client/Services/IInstanceOnboardingApi.cs` | Existing | Blazor Client | Refit interface for onboarding/settings endpoints. | Now includes Keycloak bootstrap setup method. |
| `Explore.Blazor.Client/Services/InstanceOnboardingService.cs` | Existing | Blazor Client | UI-friendly service wrapper around onboarding API. | Now includes bootstrap method, client request model, and success-only auth-scheme refresh. |
| `docs/SELF_HOSTING.md` | Existing | Docs | Compose/self-hosting runbook. | Document no manual Keycloak UI secret step. |
| `docs/CONFIGURATION.md` | Existing | Docs | Config/secret mapping. | Clarify BFF secret mandatory/API secret optional/bootstrap credential not stored. |
| `docs/TROUBLESHOOTING.md` | Existing | Docs | Operator troubleshooting. | Add Keycloak bootstrap/unauthorized_client diagnostics. |
| `docs/SECRETS.md` | Existing | Docs | Secret provider and ownership model. | Documents one-time external bootstrap credential non-persistence. |

## Key Decisions

1. **Compose-managed Keycloak uses an out-of-process init job.** This keeps Keycloak admin credentials out of normal API runtime and makes secret sync rerunnable.
2. **External Keycloak bootstrap is one-time and non-persistent.** The runtime may store the Blazor OIDC client secret, but not Keycloak admin/service-account credentials.
3. **Infrastructure owns Keycloak Admin API details.** Application defines contracts/commands; Infrastructure implements HTTP protocol; API/Blazor expose transport/UI.
4. **`KEYCLOAK_BLAZOR_CLIENT_SECRET` is mandatory for confidential BFF Keycloak login.** `KEYCLOAK_API_CLIENT_SECRET` is optional unless a future runtime API client-credentials flow uses it.
5. **Patch existing realms rather than overwrite.** Existing customer Keycloak realms should not be destroyed or blindly replaced.
6. **Future post-onboarding maintenance uses typed additive diffs.** Phase 7 should compute and preview a `RealmSyncPlan`, require instance-admin authorization and backup confirmation before mutation, and use temporary Keycloak credentials only for the active operation.

## Constraints And Rules To Remember

- Every new file starts with two `ABOUTME:` lines.
- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- BFF boundary: browser never sees tokens; setup secret is forwarded only from trusted BFF resolver/cookie/session sources.
- Setup bootstrap endpoints must be `[AllowAnonymous]` plus `[SetupSecretRequired]` before onboarding, or `[Authorize]` and instance-admin-gated after onboarding.
- Do not log or persist raw admin credentials, Keycloak access tokens, client secrets, setup secrets, or raw provider response bodies.
- Preserve Clean Architecture dependencies: Domain/Application cannot depend on Infrastructure/API/Blazor.
- UI auth/action affordances remain server-confirmed; do not introduce local role/claim authority checks.

## Validation Baseline

Baseline observed this session:

```bash
dotnet build --configuration Release --verbosity quiet
# Result: completed with warnings, 0 errors.
```

Expected validation for implementation completion:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

For Phase 1 only, add:

```bash
docker compose config
docker compose up -d keycloak-db keycloak keycloak-init
```

Phase 1 validation observed:

```bash
bash -n docker/keycloak/keycloak-init.sh
docker compose config --quiet
# Result: both passed.
```

Phase 2 validation observed:

```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
# Result: passed, 1163/1163 tests.

dotnet build --configuration Release --verbosity quiet
# Result: passed, 25 projects, 0 errors, existing warnings.

dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/CleanArchitectureTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Result: passed, 13 tests.

dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/NamingConventionTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Result: passed, 10 tests.
```

Phase 3 validation observed:

```bash
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
# Result: passed, 386/386 tests.

dotnet build --configuration Release --verbosity quiet
# Result: passed, 25 projects, 0 errors, existing warnings.

dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/CleanArchitectureTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Result: passed, 13 tests.

dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/NamingConventionTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Result: passed, 10 tests.
```

Phase 4 transport-slice validation observed:

```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/InstanceOnboardingControllerTests/*"
# Result: passed sequentially. Earlier parallel run failed from shared bin/obj copy contention, not assertions.

dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/SetupSecretForwardingHandlerTests/*"
# Result: passed sequentially. Earlier parallel run failed from shared bin/obj copy contention, not assertions.

dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/InstanceOnboardingServiceTests/*"
# Result: passed.

dotnet build --configuration Release --verbosity quiet
# Result: passed, 25 projects, 0 errors, existing warnings.

dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiContractArchitectureTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Result: passed, 6 succeeded, 1 skipped.

dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EndpointClassificationArchitectureTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Result: passed, 3 tests.
```

Additional focused verification observed after correcting the repository's TUnit filter syntax:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/AgentContextSchemaTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Result: passed, 9 tests.

dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/AgentContextLinkTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Result: passed, 8 tests.
```

Phase 5 docs validation observed:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/AgentContextSchemaTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Result: passed, 9 tests.

dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/AgentContextLinkTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Result: passed, 8 tests.

dotnet build --configuration Release --verbosity quiet
# Result: passed, 25 projects, 0 errors, existing warnings.
```

Invalid verification attempts to ignore:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AgentContextSchemaTests
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AgentContextLinkTests
# Result: TUnit exit 5, zero tests ran. These were VSTest-style filters and are not meaningful failures.
```

Full architecture suite status:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
# Result: failed with unrelated dirty-worktree source rules, including authorization parity for ActorLinkPolicy/EventLinkPolicy and a raw HTTP JSON helper rule.

dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/CqrsPatternTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Result: failed on an existing request naming/location rule. Local search identified `Explore.Application/Contracts/Infrastructure/Ai/AiChatModels.cs` containing `AiChatRequest`; that tracked file was not modified for this Keycloak work.

dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/BlazorClientArchitectureTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Result: failed on existing unrelated notification service architecture violations in `IActorSubscriptionService.cs`, `INotificationRefreshStreamClient.cs`, and `NotificationRefreshStreamClient.cs`; those files were not modified for this Keycloak work.
```

## Current Known Risks / Unknowns

- Exact least-privilege Keycloak Admin API role composition should be confirmed in automated disposable-Keycloak coverage for the target Keycloak version; docs now state the required capabilities: realm read/create when selected, client list/create, and client-secret update.
- Need decide whether API client secret remains optional sync or is removed/de-emphasized in docs.
- External Keycloak URL validation currently blocks dangerous URL/IP-literal patterns while allowing self-host/internal DNS hostnames; Phase 4 should decide whether an explicit local/private-network opt-in is needed for browser-facing setup.
- Infrastructure adapter converts Keycloak Admin API failures into safe, categorized diagnostics without logging raw response bodies.
- Phase 7 must preserve the same credential boundary after onboarding: no permanent Keycloak admin/service-account credential storage, no raw provider bodies, no tokens in browser storage, and no destructive realm reimport path.

Resolved so far:

- API/BFF startup ordering now waits for `keycloak-init` to complete successfully in Compose.
- Compose `keycloak-init` fails closed when `KEYCLOAK_BLAZOR_CLIENT_SECRET` is missing unless `KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET=true` is explicitly set for disposable local development.
- Application Phase 2 now guarantees the command handler stores only normal runtime Keycloak auth configuration and not the one-time bootstrap admin credential.
- Infrastructure Phase 3 now keeps Keycloak admin credentials/tokens method-scoped and never stores them in DI, app configuration, response DTOs, or logs.
- Phase 4 transport now keeps setup-secret handling server-side: the BFF strips browser-provided setup headers and forwards only the trusted resolver value to the new bootstrap API path.
- Phase 5 docs now make the external-Keycloak operator flow supportable without requiring manual Keycloak UI secret edits or persistent admin credential storage.
- Phase 6 now includes focused Playwright browser coverage for the setup-secret-protected Keycloak bootstrap UI flow; it reaches the next onboarding step and confirms browser storage still has no tokens.
- Phase 7 is now documented as planned future work: read-only doctor, typed desired-state sync plan, instance-admin preview, additive resync with backup confirmation, explicit client-secret rotation, and multi-project identity contract registry.

## Handoff Notes

### Handoff — 2026-05-30 Europe/Brussels
- **Current state:** Phase 1 Compose-managed Keycloak bootstrap, Phase 2 Application-layer external Keycloak bootstrap contract, Phase 3 Infrastructure Keycloak Admin API adapter, Phase 4 API/BFF/UI wiring, Phase 5 operator docs, Phase 6 backend disposable-Keycloak smoke, and focused Playwright browser UI bootstrap coverage are implemented and documented. Phase 7 post-onboarding Keycloak doctor/resync/rotation is planned but not started.
- **Next action:** Start Phase 7 with the read-only Keycloak realm doctor, separately clean unrelated architecture-suite failures, or open a separate investigation for post-bootstrap Keycloak auth-code login with `offline_access` if that flow must be covered end to end.
- **Blockers:** No code or docs blocker for the completed bootstrap path. Extending the Playwright test through login currently hits Keycloak `not_allowed` / `Offline tokens not allowed for the user or client` during `/signin-oidc`; the setup UI/bootstrap path itself passes.
- **Modified files:** `docker/keycloak/keycloak-init.sh`, `docker-compose.yml`, Phase 2 Application DTO/contract/validator/command/handler files, Phase 3 Infrastructure Keycloak bootstrap service/DI/test files, Phase 4 API route/BFF forwarding/Blazor service/UI/test files, docs, and all three active dev docs.
- **Validation:** `bash -n docker/keycloak/keycloak-init.sh`, `docker compose config --quiet`, `dotnet build --configuration Release --verbosity quiet`, `Event.Application.UnitTests`, `Explore.Infrastructure.Tests`, focused Phase 4 API integration/BFF forwarding/Blazor client service/UI source tests, focused backend disposable-Keycloak `KeycloakBootstrapRealRuntimeTests`, focused Playwright `KeycloakBootstrapBrowserFlowTests`, focused AgentContext schema/link tests, focused CleanArchitecture, focused Naming, focused API contract, and focused endpoint-classification tests passed. Full architecture suite still fails on unrelated dirty-worktree rules; focused CqrsPatternTests and focused BlazorClientArchitectureTests also fail on existing unrelated violations.
- **Documentation impact:** Phase 1 and Phase 5 operator docs plus Phase 2/3/4/5 dev docs updated.
- **Risks:** Do not allow convenience to become permanent Keycloak admin credential storage. Phase 7 must stay additive by default and must not become a destructive `realm-export.json` reapply path.
- **Notes for next contributor/agent:** Keep implementation slices small and update all three dev docs after each slice.
