<!-- ABOUTME: Implementation plan for automating Keycloak realm/client bootstrap for self-hosted deployments. -->
<!-- ABOUTME: Covers Compose init jobs, external Keycloak setup-time import, secret boundaries, tests, and docs. -->

# Keycloak Bootstrap Automation — Implementation Plan

Last Updated: 2026-06-01 Europe/Brussels

## 0. Planning Metadata
- **Request:** Plan how to automate Keycloak realm import/client secret synchronization so Docker Compose self-hosters and operators with an existing Keycloak instance do not manually edit the ISLAMU realm clients.
- **Task directory:** `dev/active/keycloak-bootstrap-automation/`
- **Planning status:** Approved for implementation and implemented through Phase 6 backend and browser integration smoke. Phase 1 Compose-managed Keycloak init job, Phase 2 Application-layer bootstrap contract, Phase 3 Infrastructure adapter, Phase 4 setup-gated API/BFF/UI wiring, Phase 5 operator docs, automated disposable-Keycloak backend integration smoke, and focused Playwright UI bootstrap smoke are complete; unrelated architecture-suite cleanup remains. Phase 7 is newly planned future work for post-onboarding Keycloak doctor/resync/rotation and is not part of the completed Phase 1-6 acceptance.
- **Matched intents:** No exact intent in `.claude/contract/intents.yaml`. This is cross-cutting DevOps/API/Blazor/security work that touches Docker Compose, setup onboarding, Keycloak/OIDC, BFF trust boundaries, and docs.
- **Fallback Contract:** `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/SECURITY-MODEL.md`, `docs/BLAZOR.md`, `docs/SELF_HOSTING.md`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, plus path-scoped rules for touched API/BFF/test/docs files.
- **Relevant skills:** `auth-patterns`, `blazor-bff-patterns`, `clean-architecture-rules`, `aspire` where orchestration is touched.
- **Relevant rules:** `.claude/rules/api-controllers.md`, `.claude/rules/blazor-server.md`, `.claude/rules/tests.md`.
- **Primary layers touched:** DevOps, API, Application, Infrastructure, Blazor/BFF, Docs, Tests.
- **Estimated complexity:** L. The core Compose sync script is small, but the secure external-Keycloak onboarding path crosses setup-secret gates, OIDC metadata, Keycloak Admin API semantics, no-secret-persistence requirements, BFF cookie/setup-secret forwarding, and integration tests.

## 1. Executive Summary

Build a two-lane Keycloak automation model:

1. **Compose-managed Keycloak lane:** keep `docker/keycloak/realm-export.json` as the base realm definition, then run a one-shot `keycloak-init` service after Keycloak is healthy. The job uses Keycloak Admin API/`kcadm.sh` to idempotently set client secrets from environment/Infisical-backed values such as `KEYCLOAK_BLAZOR_CLIENT_SECRET` and optional `KEYCLOAK_API_CLIENT_SECRET`.
2. **External existing Keycloak lane:** during setup onboarding, let the operator provide a Keycloak base URL plus a one-time bootstrap credential. The server uses that credential only for the current request to create/import/partial-import realm resources and set client secrets, then discards it. Runtime stores only the normal ISLAMU auth configuration needed for operation, not Keycloak admin credentials.
3. **Future post-onboarding maintenance lane:** after launch, expose an instance-admin-only Keycloak doctor/resync/rotation workflow. This lane diagnoses realm drift, previews additive repair plans, requires backup confirmation before mutation, uses temporary Keycloak admin/service-account credentials only for the active operation, and never deletes/reimports an existing realm.

The business outcome is less manual self-hosting friction without making the API permanently privileged inside Keycloak. The security boundary is explicit: long-lived Keycloak admin secrets must not be stored in API/Blazor config or database. Bootstrap credentials are setup-time only and are redacted in logs, diagnostics, and response bodies.

**Out of scope for completed first implementation:** full Keycloak user provisioning, Keycloak admin UI replacement, automatic production redirect URI discovery behind every reverse proxy, and broad IAM provider support beyond Keycloak. Post-onboarding realm doctor/resync/rotation is now captured as planned Phase 7 future work, not part of the completed Phase 1-6 acceptance.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Compose currently imports a static Keycloak realm file. | Verified: `docker-compose.yml` keycloak service uses `start-dev --import-realm` and mounts `./docker/keycloak/realm-export.json`. | High | Import is useful for first bootstrap but not sufficient for drift/secret sync. |
| Realm export contains hardcoded Blazor/API client secrets. | Verified: `docker/keycloak/realm-export.json` clients `islamu-event-blazor` and `islamu-event-api` include static `secret` values. | High | This creates drift when Infisical/env secrets differ. |
| Blazor consumes `KEYCLOAK_BLAZOR_CLIENT_SECRET`. | Verified: `docker-compose.yml` sets `Keycloak__ClientSecret`; `Explore.Blazor/Extensions/ConfigurationExtension.cs` maps `KEYCLOAK_BLAZOR_CLIENT_SECRET` to `Keycloak:ClientSecret`. | High | This secret is required for confidential BFF OIDC token exchange. |
| API currently does not consume `KEYCLOAK_API_CLIENT_SECRET` for normal bearer validation. | Verified by search for `KEYCLOAK_API_CLIENT_SECRET`, `Keycloak:ApiClientSecret`, and `ApiClientSecret`; only registry/docs references found. | High | `islamu-event-api` is used as audience/resource-server client, not a login client. |
| API JWT validation accepts `islamu-event-api` and `islamu-event-blazor` audiences/azp. | Verified: `Explore.API/Extensions/AuthenticationExtensions.cs`. | High | API secret likely optional until service-account/client-credentials flows are introduced. |
| Onboarding has setup-token-gated auth provider configuration. | Verified: `Explore.API/Controllers/InstanceOnboardingController.cs`, `SaveAuthProviderConfiguration`, `SetupSecretRequiredAttribute`. | High | This is the natural place to add external Keycloak bootstrap. |
| Current auth-provider config stores Keycloak client secret as an application secret setting and redacts on public read. | Verified: `Explore.Application/Services/AuthProviderConfigurationService.cs`, `InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret`. | High | Existing behavior stores runtime OIDC client secret, not Keycloak admin bootstrap secret. |
| BFF refreshes dynamic auth schemes after setup config save. | Verified: `Explore.Blazor/Services/DynamicAuthSchemeManager.cs`, `Explore.Blazor/Extensions/BffAuthEndpoints.cs`. | High | Bootstrap should trigger refresh only after persisted runtime config is valid. |
| No Keycloak Admin API bootstrap implementation exists. | Verified by search for `partialImport`, `/admin/realms`, `kcadm`, `KeycloakAdmin`. | High | New infrastructure/service code required. |
| Docs currently tell operators to verify Keycloak realm manually, not sync it. | Verified: `docs/SELF_HOSTING.md` Keycloak Realm section. | High | Docs must change with automation. |
| Existing tests cover onboarding, setup secret, Keycloak discovery, and BFF setup-secret forwarding. | Verified: `Event.API.IntegrationTests/Features/InstanceOnboardingControllerTests.cs`, `KeycloakDiscoveryTests.cs`, `Explore.Blazor.IntegrationTests/Endpoints/BffSetupSecretEndpointsTests.cs`, `SetupSecretForwardingHandlerTests.cs`. | High | Add focused tests plus automated disposable-Keycloak smoke instead of manual Compose verification. |

### 2.2 Existing Implementation

#### DevOps / Compose
- `docker-compose.yml` defines `keycloak-db`, `keycloak`, API, Blazor, and optional MinIO/Cerbos services.
- Keycloak imports `docker/keycloak/realm-export.json` at startup with `--import-realm`.
- `keycloak-init` one-shot service now runs after healthy Keycloak and before API/Blazor startup completion.

#### Keycloak Realm Files
- `docker/keycloak/realm-export.json` is the production/local Compose realm seed.
- `docker/keycloak/ISLAMU-realm.test.json` is an integration-test realm seed with test secrets/users.
- Both are static JSON files. They are not templated from Infisical/env.

#### API / Application
- `InstanceOnboardingController` exposes setup-token-gated auth-provider save/read endpoints.
- `SaveAuthProviderConfigurationCommandHandler` validates auth config and calls `IAuthProviderConfigurationService.ApplyConfigurationAsync`, then reloads JWT authority.
- `BootstrapKeycloakRealmCommandHandler` now defines the Application-layer bootstrap orchestration contract: it validates a bootstrap request, calls `IKeycloakBootstrapService`, persists only normal runtime Keycloak auth-provider configuration on success, and reloads JWT authority.
- `IKeycloakBootstrapService` is an Application contract implemented by Infrastructure through a bounded, redacted Keycloak Admin API adapter.
- `AuthProviderConfigurationService` stores provider settings in `SystemSetting`, with secrets under `InfrastructureSecretSettingKeys` and redacted on normal reads.
- `SetupSecretRequiredAttribute` checks only trusted server-forwarded `X-Setup-Secret` against `ISetupSecretProvider`.

#### Blazor / BFF
- `Explore.Blazor` owns OIDC login and cookies; browser does not own access tokens.
- `DynamicAuthSchemeManager` registers Keycloak/Google schemes from env or setup-saved DB config.
- `SetupSecretForwardingHandler` forwards setup secrets only to onboarding endpoints and strips browser-controlled values.
- `AuthProviderConfiguration.razor` currently asks for Keycloak authority/client ID/client secret, but it does not import/configure Keycloak via Admin API.

### 2.3 Existing Tests And Verification Coverage

- `Event.API.IntegrationTests/Features/InstanceOnboardingControllerTests.cs`: setup onboarding, auth-provider endpoints, admin update behavior.
- `Event.API.IntegrationTests/Features/KeycloakDiscoveryTests.cs`: Keycloak realm/metadata health for test container import.
- `Event.API.IntegrationTests/Features/SetupSecretFlowTests.cs` and `Middleware/SetupSecretRequiredFilterTests.cs`: setup-secret behavior.
- `Explore.Blazor.IntegrationTests/Endpoints/BffSetupSecretEndpointsTests.cs`: BFF setup-secret sanitization.
- `Explore.Blazor.IntegrationTests/Handlers/SetupSecretForwardingHandlerTests.cs`: setup-secret forwarding route gate.
- Added in Phase 2: Application unit tests for bootstrap request validation, safe bootstrap failure behavior, successful runtime auth-provider persistence, and no admin-secret persistence.
- Added in Phases 3-4: Infrastructure fake-HTTP tests, setup-gated API integration tests, BFF setup-secret forwarding tests, Blazor service tests, and focused UI source tests.
- Still missing: automated Compose init script smoke tests, automated onboarding UI/e2e smoke, and external-Keycloak end-to-end import/patch smoke tests.

### 2.4 Existing Documentation And Contracts

- `docs/SELF_HOSTING.md` documents Compose topology, `keycloak-init`, and the no-manual-client-secret-sync flow.
- `docs/CONFIGURATION.md` documents Infisical/env mapping for Keycloak values and the Compose bootstrap-specific secret synchronization rule.
- `docs/SECRETS.md` documents Infisical paths, secret-provider behavior, and the non-runtime nature of Keycloak admin credentials for Compose init.
- `docs/SECURITY-MODEL.md` documents BFF token boundary, safe diagnostics, and setup-secret handling.
- `docs/BLAZOR.md` documents setup-secret and BFF endpoint boundaries.
- `docs/TROUBLESHOOTING.md` includes setup/secret-provider troubleshooting and now covers `unauthorized_client` from Keycloak client-secret mismatch plus `keycloak-init` reruns.

### 2.5 Current Pain Points / Improvement Areas

1. **Manual Keycloak UI step:** Operators must open Keycloak and manually align imported client secrets with Infisical/env values.
2. **Secret drift:** Blazor may use `KEYCLOAK_BLAZOR_CLIENT_SECRET` while Keycloak still has `islamu-event-blazor-secret` from the realm export.
3. **`KEYCLOAK_API_CLIENT_SECRET` ambiguity:** Registry defines it, realm has an API client secret, but runtime code does not use it for bearer validation.
4. **External Keycloak onboarding gap:** Existing onboarding can save OIDC settings but cannot create/import/patch realm clients in a customer-owned Keycloak.
5. **Security boundary risk:** Adding Keycloak Admin API to the API runtime can over-privilege the API if admin credentials are stored or reused outside setup.
6. **Docs under-specify import semantics:** `--import-realm` is not a reliable recurring sync/rotation mechanism once the realm already exists.

### 2.6 Unknowns After Investigation

| Unknown | Search/Reason | Resolution Task |
|---|---|---|
| Exact minimum Keycloak roles needed for external bootstrap in customer Keycloak. | Not in repo; depends on Keycloak Admin API permissions. | Add docs and validation recommending a temporary service account scoped to `realm-management` roles such as manage-clients/manage-realm as narrowly as Keycloak allows. |
| Whether API client should stay confidential/bearerOnly or become public/no-secret. | Runtime does not consume API secret, but realm export includes one. | Decide in implementation whether to keep optional API secret sync or simplify docs to make only BFF secret mandatory. |
| Whether Keycloak realm import should use full import or partial import for existing realms. | No existing code; Keycloak supports realm create and partial import semantics. | Implement idempotent adapter with explicit `create realm` vs `partial import clients/scopes/mappers` paths. |
| Whether bootstrap should live in API setup endpoint or external CLI/job only. | User expressed concern about API holding admin secrets. | Prefer separate Compose job for managed Keycloak and setup-time ephemeral API/BFF endpoint for external Keycloak; do not persist admin credentials. |

## 3. Proposed Future State

### Compose-managed flow

```text
Infisical/.env
  ├─ KEYCLOAK_ADMIN / KEYCLOAK_ADMIN_PASSWORD
  ├─ KEYCLOAK_BLAZOR_CLIENT_SECRET
  └─ KEYCLOAK_API_CLIENT_SECRET (optional)
        ↓
docker compose up
        ↓
keycloak imports base realm-export.json
        ↓
keycloak-init waits for Keycloak health
        ↓
kcadm/Admin API logs into master realm
        ↓
set/update client secrets and required redirect/web origins
        ↓
API/BFF start with matching runtime secrets
```

### External existing Keycloak setup flow

```text
Operator validates setup secret in /setup
        ↓
Auth provider page selects Keycloak bootstrap mode
        ↓
Operator enters Keycloak URL + realm choice + one-time bootstrap credential
        ↓
BFF sends request through setup-secret-protected API path
        ↓
API validates URL/realm/client values, calls Keycloak Admin API once
        ↓
API creates/imports/partial-imports clients/scopes/mappers and sets client secrets
        ↓
API persists only runtime OIDC settings/client secret needed for ISLAMU login
        ↓
Bootstrap admin credential is discarded and never returned/logged/stored
        ↓
BFF refreshes dynamic auth schemes; operator logs in to continue onboarding
```

## 4. Non-Negotiable Constraints

- Repositories return entities, never DTOs.
- Validators are manually instantiated in handlers/services; do not inject `IValidator<T>`.
- GET endpoints are `[AllowAnonymous]`; write/setup mutation endpoints are protected by setup secret and/or `[Authorize]` as appropriate.
- Browser never sees bearer tokens; BFF stores tokens server-side in HttpOnly cookies.
- Browser-controlled `X-Setup-Secret` is not trusted. BFF/API must only use resolver/forwarded trusted setup secret.
- Do not store Keycloak admin/bootstrap credentials in `SystemSetting`, secret binding, appsettings, logs, telemetry, response bodies, traces, or browser state.
- New C# and markdown files must start with two `ABOUTME:` summary lines.
- No Clean Architecture dependency inversion violations: Keycloak HTTP adapter belongs in Infrastructure; Application owns contracts/commands/DTOs; API/Blazor expose transport/UI.
- Tenant isolation remains API-authoritative; bootstrap endpoints are instance-level setup/admin only.
- No compatibility shims unless explicitly approved; project is pre-v1.

## 5. Architecture And Design Decisions

### Decision 1: Use a separate `keycloak-init` service for Compose-managed Keycloak
- **Why:** The API should not need Keycloak admin credentials during normal runtime just to correct Compose realm secrets.
- **Alternatives:** Templating `realm-export.json`; API startup sync; manual Keycloak UI.
- **Consequences:** Clear secret isolation, rerunnable init job, smaller API blast radius. Requires script and docs.
- **Files/layers affected:** `docker-compose.yml`, new `docker/keycloak/keycloak-init.sh`, docs.

### Decision 2: External-Keycloak bootstrap is setup-time and non-persistent
- **Why:** Storing Keycloak admin credentials in API/DB means API compromise becomes Keycloak compromise.
- **Alternatives:** Store admin client secret for later rotation; ask users to do manual import; require separate CLI.
- **Consequences:** Safer default; operators must re-enter/bootstrap via CLI for future realm drift unless follow-up rotation tooling is built.
- **Files/layers affected:** Application DTO/contracts, Infrastructure Keycloak Admin client, API setup endpoint, Blazor onboarding UI.

### Decision 3: Infrastructure owns Keycloak Admin API client
- **Why:** HTTP protocol details and Keycloak endpoints are external infrastructure concerns.
- **Alternatives:** Put HTTP calls in controller/handler; shell out to `kcadm.sh` from API.
- **Consequences:** Testable adapter, Clean Architecture compliance. Application defines `IKeycloakBootstrapService` contract and request/response DTOs.
- **Files/layers affected:** `Explore.Application/Contracts/Services`, `Explore.Infrastructure/Services/Keycloak`, DI registration.

### Decision 4: API client secret is optional until runtime needs it
- **Why:** Current API validates JWTs and does not use client credentials; only Blazor BFF confidential client secret is runtime-critical.
- **Alternatives:** Force both secrets everywhere; remove API secret from realm.
- **Consequences:** Less self-hoster friction while preserving optional sync if realm keeps a confidential API client.
- **Files/layers affected:** `docker-compose.yml`, `realm-export.json` docs, `SecretDefinitionRegistry` follow-up if deprecating API secret.

### Decision 5: Import strategy is idempotent patch, not blind full overwrite
- **Why:** Existing customer Keycloak realms may contain users, roles, custom clients, and policies. Full overwrite is dangerous.
- **Alternatives:** Full realm import only; manual export/download instructions only.
- **Consequences:** Need careful partial import/create/update logic and diagnostics. Safer for existing Keycloak.
- **Files/layers affected:** Keycloak bootstrap service and tests.

### Decision 6: Existing realms need typed additive maintenance, not destructive reimport
- **Why:** Long-lived self-hosted realms will accumulate users, roles, client customizations, and additional ISLAMU project clients. Requiring delete/reimport for every new realm requirement is operationally unsafe.
- **Alternatives:** Manual Keycloak UI instructions; reapply `realm-export.json`; store permanent Keycloak admin credentials for automatic background sync.
- **Consequences:** Future post-onboarding maintenance must compute a typed desired-state diff, preview additive operations, require explicit instance-admin authorization and backup confirmation, and use only request/job-scoped Keycloak admin credentials.
- **Files/layers affected:** Future Application doctor/sync contracts, Infrastructure Keycloak desired-state adapter, API instance-admin endpoints, Blazor admin UI, docs, tests.

## 6. Implementation Phases

### Phase 1: Compose-managed Keycloak init job
- **Goal:** Automate client secret sync for repository Compose deployments.
- **Depends on:** None.
- **Relevant files:** `docker-compose.yml` existing, `docker/keycloak/keycloak-init.sh` new, `docs/SELF_HOSTING.md` existing, `docs/TROUBLESHOOTING.md` existing.
- **Related skills/rules:** auth-patterns, docs rules from AGENTS.
- **Acceptance criteria:** `docker compose up` can create/import realm and idempotently set Blazor/API client secrets from env; job can run repeatedly without rotating secrets unexpectedly.
- **Verification:** `docker compose config`; local smoke with Keycloak; `dotnet build --configuration Release --verbosity quiet`.
- **Rollback / failure handling:** Remove `keycloak-init` service; static realm import remains.
- **Implementation status:** Implemented for Compose. `KEYCLOAK_BLAZOR_CLIENT_SECRET` is required unless `KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET=true`; API/Blazor wait for successful init through `service_completed_successfully`.

#### Task 1.1: Add Keycloak init script
- **Type:** create
- **Layer:** DevOps
- **Files:** `docker/keycloak/keycloak-init.sh` (new)
- **Description:** Use Keycloak container tooling (`kcadm.sh`) or bounded curl calls to authenticate with `KEYCLOAK_ADMIN`/`KEYCLOAK_ADMIN_PASSWORD`, locate clients by `clientId`, and set `secret` values from `KEYCLOAK_BLAZOR_CLIENT_SECRET` and optional `KEYCLOAK_API_CLIENT_SECRET`.
- **Acceptance Criteria:** script fails closed on missing required Blazor secret unless default local dev fallback is explicitly enabled; logs only client IDs and boolean secret presence; supports idempotent reruns.
- **Dependencies:** None
- **Effort:** M
- **Validation:** shell syntax check if available; manual `docker compose run --rm keycloak-init`.

#### Task 1.2: Add Compose one-shot service
- **Type:** modify
- **Layer:** DevOps
- **Files:** `docker-compose.yml` (existing)
- **Description:** Add `keycloak-init` service using the same Keycloak image, depending on healthy `keycloak`, with admin/client secret env mappings and script mount.
- **Acceptance Criteria:** API/Blazor depend on successful init or docs explain failure mode; init does not expose additional public ports.
- **Dependencies:** Task 1.1
- **Effort:** S
- **Validation:** `docker compose config` and local smoke.

### Phase 2: External Keycloak bootstrap contract
- **Goal:** Define Application-layer DTOs/contracts for one-time Keycloak bootstrap without committing HTTP details to Application.
- **Depends on:** User approval to start implementation was given. The setup-time API/BFF/Blazor service transport and UI exposure slice are implemented.
- **Relevant files:** `Explore.Application/DTOs/Onboarding/KeycloakBootstrap*.cs`, `Explore.Application/Onboarding/KeycloakBootstrapMode.cs`, `Explore.Application/Contracts/Services/IKeycloakBootstrapService.cs`, new command/handler/validator/tests.
- **Related skills/rules:** clean-architecture-rules, auth-patterns, application-layer conventions.
- **Acceptance criteria:** request model separates runtime OIDC values from bootstrap credentials; validators reject unsafe URLs/blank realm/client values/control chars/oversized secrets.
- **Verification:** `Event.Application.UnitTests` passed; focused Clean Architecture and Naming architecture tests passed. Focused CqrsPatternTests and full architecture suite remain blocked by unrelated existing source/worktree failures.
- **Rollback:** Remove new command/DTOs before API endpoint is wired.
- **Implementation status:** Implemented for Application orchestration. Infrastructure Keycloak Admin API calls and setup-gated API/BFF/UI wiring are now implemented.

#### Task 2.1: Define DTOs and service contract
- **Type:** create
- **Layer:** Application
- **Files:** `Explore.Application/DTOs/Onboarding/KeycloakBootstrapRequestDto.cs` (new), `KeycloakBootstrapResultDto.cs` (new), `Explore.Application/Contracts/Services/IKeycloakBootstrapService.cs` (new)
- **Description:** Capture Keycloak base URL, target realm, client IDs, runtime client secret, optional API client secret, mode (`CreateRealm`/`PatchExistingRealm`), and one-time bootstrap credential. Result includes success flags and safe diagnostics only.
- **Acceptance Criteria:** no admin secret appears on response DTO; DTO names make one-time credential semantics obvious.
- **Dependencies:** None
- **Effort:** M
- **Validation:** Build and architecture tests.
- **Implementation status:** Complete. DTO/result/enum/service contract exist, and result DTO contains only safe diagnostics with no admin credential or token fields.

#### Task 2.2: Add command handler with manual validator
- **Type:** create
- **Layer:** Application
- **Files:** `Explore.Application/Features/InstanceOnboarding/Requests/Commands/BootstrapKeycloakRealmCommand.cs` (new), handler (new), validator (new)
- **Description:** Manually instantiate validator, call `IKeycloakBootstrapService`, persist normal auth-provider configuration only on successful bootstrap, then reload JWT authority.
- **Acceptance Criteria:** no admin credential persistence; failures return safe `BaseCommandResponse<Guid>` errors; cancellation token is passed.
- **Dependencies:** Task 2.1
- **Effort:** M
- **Validation:** `Event.Application.UnitTests`.
- **Implementation status:** Complete. Handler manually instantiates the validator, calls `IKeycloakBootstrapService`, persists only `AuthProviderConfigurationDto` runtime Keycloak fields on success, reloads JWT authority, and keeps one-time admin credentials out of storage/logs/responses.

### Phase 3: Infrastructure Keycloak Admin API adapter
- **Goal:** Implement bounded, redacted Keycloak Admin API calls.
- **Depends on:** Phase 2.
- **Relevant files:** new `Explore.Infrastructure/Services/Keycloak/*`, `InfrastructureServicesRegistration.cs` existing.
- **Acceptance criteria:** adapter can authenticate, create/check realm, locate/create clients, update secrets; all logs redact credentials and tokens.
- **Verification:** `Explore.Infrastructure.Tests`, release build, focused Clean Architecture and Naming architecture tests passed. Optional Testcontainers/manual external-Keycloak smoke remains future work.
- **Rollback:** Leave Application contract unregistered; endpoint returns service unavailable until implementation exists.
- **Implementation status:** Complete for Infrastructure. API/BFF/UI wiring is now implemented.

#### Task 3.1: Implement Keycloak Admin client
- **Type:** create
- **Layer:** Infrastructure
- **Files:** `Explore.Infrastructure/Services/Keycloak/KeycloakBootstrapService.cs` (new), local protocol models as needed.
- **Description:** Use `HttpClientFactory` with bounded timeout. Implement token acquisition from one-time credential, realm exists check, realm create, client lookup/create, and secret update.
- **Acceptance Criteria:** no shelling out from API; no raw response body logging; safe exception categories.
- **Dependencies:** Task 2.1
- **Effort:** L
- **Validation:** `Explore.Infrastructure.Tests` passed with fake HTTP handler coverage; build passed.
- **Implementation status:** Complete. The adapter keeps admin credentials and tokens method-scoped, returns categorized safe failures, and omits raw Keycloak response bodies from logs/results.

#### Task 3.2: Register service and options
- **Type:** modify
- **Layer:** Infrastructure/API composition
- **Files:** `Explore.Infrastructure/InfrastructureServicesRegistration.cs` (existing), API program/DI if required.
- **Description:** Register `IKeycloakBootstrapService` and named HttpClient with timeout and safe dev certificate behavior only where intentional.
- **Acceptance Criteria:** no singleton captures per-request admin secret; secrets flow only as method parameters.
- **Dependencies:** Task 3.1
- **Effort:** S
- **Validation:** Build, focused Clean Architecture, and focused Naming architecture tests passed.
- **Implementation status:** Complete. Infrastructure registers a scoped `IKeycloakBootstrapService` and named `KeycloakBootstrapClient` with a 30-second timeout.

### Phase 4: API endpoint and Blazor onboarding UI
- **Goal:** Expose setup-time bootstrap and let operators choose managed/imported external Keycloak.
- **Depends on:** Phases 2-3.
- **Relevant files:** `Explore.API/Controllers/InstanceOnboardingController.cs`, `RouteNames.cs`, `Explore.Blazor.Client/Pages/Onboarding/AuthProviderConfiguration.razor`, `IInstanceOnboardingApi.cs`, `InstanceOnboardingService.cs`.
- **Acceptance criteria:** endpoint is setup-secret protected; UI labels admin credential as one-time/not stored; BFF setup-secret forwarding includes the new endpoint path; auth schemes refresh after success.
- **Verification:** API integration tests, BFF forwarding tests, UI/service tests.
- **Rollback:** Hide UI mode and remove endpoint route.
- **Implementation status:** Complete. The setup-secret-gated API route, BFF trusted setup-secret forwarding, Blazor service method/model, success-only auth-scheme refresh, visible onboarding UI mode, and focused tests are implemented.

#### Task 4.1: Add setup-token-gated API route
- **Type:** modify/create
- **Layer:** API
- **Files:** `InstanceOnboardingController.cs` (existing), `RouteNames.cs` (existing)
- **Description:** Add `POST /api/InstanceOnboarding/auth-provider-configuration/keycloak-bootstrap` or similar explicit route, `[AllowAnonymous]`, `[SetupSecretRequired]`, `[EndpointClassification(EndpointClass.Admin)]`, response types, and MediatR dispatch.
- **Acceptance Criteria:** route name explicit; ProblemDetails/BaseCommandResponse errors safe; operation is not available after setup completion.
- **Dependencies:** Phase 2
- **Effort:** S
- **Validation:** `Event.API.IntegrationTests`, `Event.Architecture.Tests`.
- **Implementation status:** Complete. The route dispatches `BootstrapKeycloakRealmCommand` and is setup-secret gated through `SetupSecretRequiredAttribute`.

#### Task 4.2: Update BFF forwarding and client service
- **Type:** modify
- **Layer:** Blazor/BFF
- **Files:** `SetupSecretForwardingHandler.cs` (existing), `CircuitAccessTokenService.cs` if route allow-list exists, `IInstanceOnboardingApi.cs`, `InstanceOnboardingService.cs`.
- **Description:** Add new endpoint to setup-secret forwarding/allow-list and expose a client method.
- **Acceptance Criteria:** browser-supplied setup header is still stripped; trusted resolver output reaches API.
- **Dependencies:** Task 4.1
- **Effort:** M
- **Validation:** `Explore.Blazor.IntegrationTests`.
- **Implementation status:** Complete for BFF and Blazor service transport. `SetupSecretForwardingHandler` explicitly covers the new route, `IInstanceOnboardingApi` and `InstanceOnboardingService` expose bootstrap calls, and successful service calls refresh auth schemes. The visible UI caller is implemented in Task 4.3.

#### Task 4.3: Add UI mode for external Keycloak bootstrap
- **Type:** modify
- **Layer:** Blazor Client
- **Files:** `AuthProviderConfiguration.razor` (existing)
- **Description:** Add a clear choice: “Use already configured Keycloak” vs “Let ISLAMU configure clients now”. Collect one-time admin/service-account credential only for bootstrap mode and clear it after submit.
- **Acceptance Criteria:** UI warns credential is not stored; model clears secret fields after failure/success; no local role/claim authorization assumptions.
- **Dependencies:** Task 4.2
- **Effort:** M
- **Validation:** focused `AuthProviderConfigurationSourceTests` passed; automated UI/e2e smoke remains as the next test slice.
- **Implementation status:** Complete. Service transport exists, the Razor UI mode is wired, bootstrap credentials are labeled one-time/not stored, and bootstrap secret fields are cleared after submit.

### Phase 5: Tests, docs, and operational runbooks
- **Goal:** Make behavior supportable for self-hosters and future agents.
- **Depends on:** Phases 1-4.
- **Relevant files:** `docs/SELF_HOSTING.md`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/TROUBLESHOOTING.md`, `docs/RELEASE_CHECKLIST.md`, test files.
- **Acceptance criteria:** docs state exactly which secrets are required, what is automated, what external-Keycloak permissions are needed, and how to recover when bootstrap fails.
- **Verification:** docs architecture tests; build; affected test projects.

### Phase 6: Automated disposable-Keycloak integration/e2e smoke
- **Goal:** Replace manual smoke with repeatable automated coverage that proves the real Keycloak bootstrap path works against disposable infrastructure.
- **Depends on:** Phases 1-5.
- **Relevant files:** new or expanded tests under `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, `Explore.Blazor.IntegrationTests`, `Explore.Blazor.Client.Tests`, and `Explore.Blazor.Client.E2ETests`.
- **Acceptance criteria:** tests start disposable Keycloak (Testcontainers or bounded Compose harness), verify Compose-style `keycloak-init` secret sync or equivalent real Keycloak Admin API calls, exercise external bootstrap create/patch behavior, and prove the Blazor setup path can call bootstrap without leaking setup/admin/client secrets. Browser coverage should prove the onboarding UI can submit bootstrap through the BFF setup-secret boundary without storing tokens in browser storage. No human-only manual smoke is part of acceptance.
- **Verification:** automated integration/e2e test command documented in this workstream and runnable project-by-project under the repository test policy.
- **Implementation status:** Complete for backend integration smoke and focused browser UI bootstrap smoke. `KeycloakBootstrapRealRuntimeTests` starts disposable Keycloak, calls the setup-gated bootstrap API route, exercises the real Application handler and Infrastructure Keycloak Admin API adapter, rotates the Blazor client secret in Keycloak, verifies the rotated secret through the Keycloak token endpoint, and confirms persisted runtime config excludes the one-time admin credential. `KeycloakBootstrapBrowserFlowTests` starts Aspire AppHost/Testcontainers infrastructure, persists setup secret through the BFF, drives the visible Keycloak bootstrap onboarding UI, reaches the next onboarding step, and verifies browser storage contains no tokens. Extending that browser test through post-bootstrap login currently exposes a separate Keycloak `offline_access` auth-code failure and is deferred as a distinct investigation.

### Phase 7: Post-onboarding Keycloak doctor, resync, and rotation (future)
- **Goal:** Let launched self-hosted instances safely diagnose and additively repair Keycloak realm drift without deleting/reimporting the realm or permanently storing Keycloak admin credentials.
- **Depends on:** Phases 1-6, especially the existing bounded Keycloak Admin API adapter, runtime auth-provider configuration, secret ownership model, and setup-time bootstrap lessons.
- **Relevant files:** Future Application DTOs/contracts/commands for doctor/sync plans, future Infrastructure Keycloak desired-state/diff services, `InstanceOnboardingController` or a new instance-admin infrastructure controller, Blazor admin infrastructure UI, `docs/SELF_HOSTING.md`, `docs/SECRETS.md`, `docs/TROUBLESHOOTING.md`, `docs/BACKUP_RESTORE_UPGRADE.md`.
- **Acceptance criteria:** doctor can report missing/mismatched Keycloak realm requirements without mutation; resync preview shows additive operations before apply; apply is instance-admin gated; operator confirms Keycloak backup before mutation; temporary Keycloak admin/service-account credentials are never stored; destructive operations are forbidden by default; client-secret rotation respects application-managed vs deployment-managed ownership.
- **Verification:** Application unit tests for plan generation and credential non-persistence, Infrastructure fake-HTTP tests for Keycloak Admin API diffs/mutations/redaction, API integration tests for instance-admin authorization and safe responses, Blazor UI tests for preview/confirmation/secret clearing, disposable-Keycloak runtime tests for additive resync and rotation.
- **Implementation status:** Planned / not started. This phase is intentionally separated from first-run bootstrap acceptance.

#### Task 7.1: Add read-only Keycloak realm doctor
- **Type:** create
- **Layer:** Application/API/Infrastructure/Blazor admin UI
- **Files:** Future doctor DTOs/contracts/queries, Keycloak Infrastructure inspection service, instance-admin API endpoint, admin UI surface.
- **Description:** Diagnose realm compatibility without mutation. Check reachability, realm existence, OIDC discovery, Blazor/API clients, redirect URIs, web origins, `standardFlowEnabled`, offline-access role/composite/client-scope/scope-mapping requirements, refresh-token settings, audience mappings, and future ISLAMU project clients.
- **Acceptance Criteria:** returns structured statuses such as healthy/needs-repair/blocked; exposes no secrets, tokens, raw provider bodies, or secret-derived details; supports a basic non-admin mode and an optional temporary-admin read-only mode.
- **Dependencies:** Phase 6 Keycloak runtime proof and existing secret safety docs.
- **Effort:** L
- **Validation:** Application unit tests, Infrastructure fake-HTTP tests, API authorization tests, Blazor admin UI tests.

#### Task 7.2: Define typed Keycloak desired-state and sync-plan model
- **Type:** create
- **Layer:** Application
- **Files:** Future `KeycloakRealmDesiredState`, `KeycloakRealmSyncPlan`, operation DTOs, validators.
- **Description:** Represent ISLAMU-owned realm requirements as typed additive contracts instead of treating `realm-export.json` as a recurring update mechanism.
- **Acceptance Criteria:** plan can express add/update operations for ISLAMU-owned clients, redirect URIs, web origins, optional scopes, scope mappings, protocol/audience mappers, default-role composites, and future project client contracts; plan explicitly marks destructive operations unsupported.
- **Dependencies:** 7.1.
- **Effort:** L
- **Validation:** deterministic diff unit tests and architecture tests.

#### Task 7.3: Add instance-admin resync preview workflow
- **Type:** create
- **Layer:** API/Blazor admin UI
- **Files:** Future instance-admin infrastructure controller/route names, admin UI page/component, service models.
- **Description:** Let an authenticated instance admin run doctor and preview the computed additive `RealmSyncPlan` before providing mutation credentials.
- **Acceptance Criteria:** endpoint is `[Authorize]` with instance-admin authorization; UI uses server-confirmed affordances; preview is read-only; raw Keycloak errors are categorized safely; no Keycloak admin credential is required for the basic preview unless deeper Admin API reads are selected.
- **Dependencies:** 7.1-7.2.
- **Effort:** L
- **Validation:** API integration tests, Blazor UI/source tests, authorization tests.

#### Task 7.4: Add additive resync apply with backup confirmation
- **Type:** create
- **Layer:** Application/API/Infrastructure/Blazor admin UI
- **Files:** Future command/handler/validator, Infrastructure apply service, admin UI confirmation flow, docs.
- **Description:** Apply only approved additive repairs after the operator confirms Keycloak backup and submits a temporary Keycloak admin/service-account credential for the active operation.
- **Acceptance Criteria:** may create/update ISLAMU-owned clients and add missing scopes/mappers/redirects/origins/composites; must not delete realm/users/groups/unrelated clients/unowned roles; must not remove operator-added redirect URIs without a future explicit destructive-operation design; clears temporary credential after submit and never stores it.
- **Dependencies:** 7.2-7.3.
- **Effort:** XL
- **Validation:** Infrastructure fake-HTTP tests, disposable-Keycloak integration tests, secret scanning/redaction checks, docs tests.

#### Task 7.5: Add explicit client-secret rotation workflow
- **Type:** create
- **Layer:** Application/API/Infrastructure/Blazor admin UI/Secrets
- **Files:** Future rotation command/service/UI plus docs.
- **Description:** Allow instance admins to rotate ISLAMU-owned Keycloak client secrets deliberately, coordinating Keycloak Admin API update with runtime secret ownership.
- **Acceptance Criteria:** application-managed secrets can be updated by ISLAMU; deployment-managed secrets produce operator instructions to update env/Infisical instead of silently overriding; audit logs record actor/time/client ID/result but never secret values; auth schemes refresh or restart guidance is shown.
- **Dependencies:** 7.4 and secret ownership model.
- **Effort:** L
- **Validation:** Application unit tests, API integration tests, Infrastructure fake-HTTP tests, Blazor UI tests, disposable-Keycloak rotation proof.

#### Task 7.6: Add multi-project identity contract registry and drift detection
- **Type:** create
- **Layer:** Application/Infrastructure/Ops
- **Files:** Future identity contract registry, module/project contributors, doctor extensions, docs.
- **Description:** Let future ISLAMU projects or optional services contribute their own Keycloak client/scope/mapper requirements to the desired-state model.
- **Acceptance Criteria:** Event, future identity service, admin portal, mobile client, and other project contracts can be composed without one project owning the entire realm; optional scheduled drift detection is read-only and never auto-mutates; findings are safe for support bundles.
- **Dependencies:** 7.2.
- **Effort:** XL
- **Validation:** registry composition tests, doctor tests, documentation checks.

## 7. Testing Strategy

| Requirement | Test Project / Check |
|---|---|
| Compose init script references correct env and client IDs | Shell/static check plus `docker compose config`; optional local smoke. |
| Setup endpoint requires setup secret and closes after onboarding | `Event.API.IntegrationTests`. |
| Application command never persists admin bootstrap credential | `Event.Application.UnitTests` with fake service/repository. |
| Keycloak Admin API adapter redacts failures and handles success/conflict | `Explore.Infrastructure.Tests` with fake `HttpMessageHandler`; optional Testcontainers. |
| BFF forwards setup secret to new endpoint only from trusted sources | `Explore.Blazor.IntegrationTests/Handlers/SetupSecretForwardingHandlerTests.cs`. |
| Blazor UI/service calls new route and clears one-time secret | `Explore.Blazor.Client.Tests` or focused bUnit/service tests. |
| Operator docs cover external Keycloak bootstrap and recovery | Focused AgentContext schema/link tests plus release build. |
| Real Keycloak bootstrap works end to end | New automated disposable-Keycloak integration/e2e tests; no manual smoke acceptance. |
| Browser UI submits setup-gated bootstrap | `Explore.Blazor.Client.E2ETests` focused `KeycloakBootstrapBrowserFlowTests`; no browser token storage. |
| Architecture invariants | `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`. |
| Baseline build | `dotnet build --configuration Release --verbosity quiet`. |

Phase 6 observed verification:

```bash
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/KeycloakBootstrapServiceTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Passed.

dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/KeycloakBootstrapRealRuntimeTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Passed. Starts disposable Keycloak and verifies setup-gated bootstrap plus rotated client-secret token flow.

dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/KeycloakBootstrapBrowserFlowTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Passed. Starts Aspire AppHost/Testcontainers infrastructure and verifies BFF setup-secret persistence plus Keycloak bootstrap UI submission without browser token storage.

dotnet build --configuration Release --verbosity quiet
# Passed with 25 projects, 0 errors, and existing warnings.
```

Minimum expected test commands before completion:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Phase 2 observed verification:

```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
# Passed.

dotnet build --configuration Release --verbosity quiet
# Passed with 0 errors and existing warnings.

dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/CleanArchitectureTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Passed.

dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/NamingConventionTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
# Passed.
```

Known verification limitations after Phase 2:

- Full `Event.Architecture.Tests` still fails on unrelated dirty-worktree rules already observed before Phase 2.
- Focused `CqrsPatternTests` fails on an existing tracked `AiChatRequest` location/naming issue in `Explore.Application/Contracts/Infrastructure/Ai/AiChatModels.cs`, which was not modified by the Keycloak work.

## 8. Documentation, Configuration, And Operations Impact

- `docs/SELF_HOSTING.md`: documents `keycloak-init`, required/optional secrets, external-Keycloak bootstrap flow, temporary credential handling, and no manual UI secret step for Compose.
- `docs/CONFIGURATION.md`: clarifies BFF secret vs optional API client secret, persisted runtime auth config, request-scoped bootstrap credential non-persistence, and external bootstrap URL safety.
- `docs/SECRETS.md`: lists `/keycloak` keys and the ownership model, including one-time external bootstrap credential non-persistence.
- `docs/TROUBLESHOOTING.md`: covers Keycloak bootstrap failure symptoms and fixes (`unauthorized_client`, unsafe/bad URL, missing permissions, missing realms, client conflicts, and bad redirect URI/post-bootstrap login recovery).
- `docs/OPERATIONS.md`: mention automation in operational runbooks if behavior changes startup readiness.
- `docker-compose.yml`: new one-shot init service and env mapping.
- `docker/keycloak/realm-export.json`: may remain static, but docs should warn static secrets are local defaults and `keycloak-init` is the source of truth for Compose secret alignment.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- **Keycloak admin blast radius:** never store bootstrap admin/service-account credential; require setup-secret or instance-admin gate; recommend temporary credentials.
- **SSRF:** validate Keycloak base URL. Reject private/internal addresses for managed/cloud mode unless explicit self-host/local allow-list is configured. Compose/local may need `http://keycloak:8080` and localhost allowances.
- **Redaction:** logs/metrics must include only booleans/correlation IDs/client IDs/realm names, not admin secrets, access tokens, client secrets, or Keycloak raw response bodies.
- **Rate limiting:** setup-secret endpoints are already rate-limited; apply or preserve rate limiting for bootstrap attempts.
- **Idempotency:** bootstrap should be rerunnable; external bootstrap should avoid duplicate clients/scopes.
- **Authorization:** setup path is protected by setup secret before first admin exists; post-onboarding path, if added, must require instance admin.
- **HAL/UI:** not directly applicable unless exposing post-onboarding affordances; any admin UI action should be server-confirmed, not role-only.
- **Tenant isolation:** instance-level setup only; no tenant-scoped Keycloak admin credential storage.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Applicability | Notes |
|---|---|---|
| Multi-tenancy | Applicable | Keycloak realm is instance-wide by current auth-provider model; tenant-specific auth providers are not part of this slice. |
| Federation | Not directly applicable | ATProto login remains separate. |
| Localization | Needs investigation | UI text should use existing localization patterns if page is localized; otherwise add clear English copy for now. |
| Accessibility | Applicable | New UI fields need labels, helper text, validation summaries, keyboard flow, and alert roles. |
| Product/self-hosting | Highly applicable | Main value is reducing manual setup steps for self-hosters and external-Keycloak operators. |

## 11. Observability And Operations

- Log bootstrap attempts with safe fields: realm, client IDs, mode, success/failure category, correlation ID.
- Emit no secret-derived dimensions.
- Return categorized user-safe errors: unreachable Keycloak, invalid credential, missing permission, import conflict, invalid realm/client config.
- Add troubleshooting docs for retrying `keycloak-init` and external bootstrap.
- Consider a read-only doctor check later that detects Keycloak/client secret mismatch without fixing it.

## 12. Migration And Compatibility Plan

- No EF migration is required for the Compose init job.
- External bootstrap may not require schema changes if it reuses existing auth-provider config storage and does not persist bootstrap credentials.
- If a bootstrap audit table is desired later, that should be a separate EF migration and plan update.
- Existing `realm-export.json` can remain compatible; new init job overlays secret values after import.
- Pre-v1 stance: no legacy compatibility shim for old secret names unless explicitly approved.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| API accidentally stores Keycloak admin credential | Medium | Critical | One-time DTO only, no persistence path, tests assert secret not saved, redacted logging. | Secret scanning/test inspection/log review. | Phase 2-4 |
| Keycloak Admin API roles too broad | Medium | High | Document minimum permissions and recommend temporary service account; fail closed on insufficient role. | 403 from Keycloak; troubleshooting category. | Phase 3/Docs |
| Full realm import overwrites customer config | Low if plan followed | Critical | Use patch/partial import/update clients rather than overwrite existing realm. | Diff before/after external realm; test conflicts. | Phase 3 |
| Compose API starts before init finishes | Medium | Medium | Depend API/UI on successful init or document manual rerun and readiness. | OIDC unauthorized_client on login. | Phase 1 |
| API client secret remains confusing | High | Low/Medium | Document BFF secret mandatory, API secret optional; consider follow-up removal/deprecation. | Repeated support questions. | Docs |
| SSRF through external Keycloak URL | Medium | High | URL validation, local/private allow-list only for local setup modes, no arbitrary redirects. | Security tests. | Phase 2/3 |

## 14. Success Metrics And Definition Of Done

Functional success:
- Docker Compose deployment no longer requires manual Keycloak UI secret edits.
- External Keycloak onboarding can import/patch ISLAMU clients with a one-time bootstrap credential.
- BFF login succeeds because Keycloak and Blazor agree on the confidential client secret.

Quality gates:
- Build passes.
- Affected unit/integration/architecture tests pass individually.
- Docs explain required secrets, bootstrap modes, security boundary, and recovery steps.
- No raw secrets are logged or returned.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

Future agents implementing this plan MUST:

1. Read this plan, `keycloak-bootstrap-automation-context.md`, and `keycloak-bootstrap-automation-tasks.md` before editing.
2. Start from the highest-priority incomplete task unless user instruction overrides it.
3. Update the plan when architecture/scope changes.
4. Update the context file after each meaningful implementation slice with changed files, validation, blockers, and next step.
5. Update the tasks checklist immediately after completing or discovering tasks.
6. Do not report “done” unless all three dev docs reflect reality.
7. Final summaries must teach the implementation: design pattern, Keycloak/Admin API protocol, files changed, control flow, secret boundaries, tests, remaining work, and next step.

## 16. Progress Reporting Contract

Use this structure after each implementation slice:

- **Implemented:** medium-sized developer teaching summary naming patterns, protocols, infrastructure, files/classes, and data/control flow.
- **Verified:** exact build/test/manual commands run and results.
- **Remaining:** incomplete tasks and known risks.
- **Next:** recommended next slice.
- **Docs updated:** yes/no and which dev docs changed.

## 17. Potential Risks & Unknowns

The most likely hard part is not the Keycloak API call itself; it is preserving the security boundary while still making onboarding easy. A permanent API-held Keycloak admin credential would make setup convenient but dangerously expands blast radius. The implementation should bias toward one-shot setup credentials, separate Compose init jobs, redacted diagnostics, and clear operator docs even if that means future realm re-sync requires a deliberate action.
