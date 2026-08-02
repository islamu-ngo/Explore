<!-- ABOUTME: Decision-complete implementation plan for the optional single-process Event.Standalone host. -->
<!-- ABOUTME: Combines API, Blazor, and SQLite default persistence on one Kestrel port with Docker packaging. -->

# Event Standalone Combined Host — Implementation Plan

Last Updated: 2026-08-02 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Add an optional `Event.Standalone` startup project in which one ASP.NET Core/Kestrel process and one port serve the existing REST API, Razor Components/Blazor Server UI, SignalR, and external/mobile API consumers.
- **Task directory:** `dev/active/event-standalone-combined-host/`
- **Planning status:** Complete; implementation not started.
- **Intent contract:** No registered intent directly covers a new combined composition root. Use the governance fallback contract, bounded to host composition, integration tests, solution/orchestration wiring, and topology documentation. Do not infer endpoint, persistence, migration, or UI-feature scope.
- **Relevant skills:** `implementation-plan`, `explore-codebase`, `clean-architecture-rules`, `blazor-bff-patterns`, `blazor-ui-conventions`.
- **Relevant rules:** `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `.claude/rules/api-controllers.md`, `.claude/rules/blazor-server.md`, `.claude/rules/tests.md`.
- **Primary layers:** API composition, Blazor/BFF composition, shared BFF hosting, Aspire orchestration, integration/architecture tests, operations documentation.
- **Complexity:** XL. Two independently configured web hosts must share DI, authentication schemes, middleware, endpoints, startup gates, static assets, health, and shutdown behavior without weakening the browser BFF trust boundary or duplicating hosted services. Additionally includes SQLite default persistence, provider override, and Docker packaging.

## 1. Executive Summary

Create `src/Event.Standalone` as an optional composition root. It will reference the existing `Explore.API` and `Explore.Blazor` host assemblies, call reusable host-composition modules extracted from their `Program.cs` files, and expose all existing HTTP surfaces through one Kestrel listener. The split `Explore.API` plus `Explore.Blazor` topology remains the default and must retain its current behavior.

The combined host will not self-proxy `/api/*` through YARP. Browser API requests will pass through an in-process BFF bridge that authenticates the HttpOnly session cookie, enforces antiforgery for mutations, obtains the server-held access token, strips browser-controlled privileged headers, reconstructs trusted headers, and injects the bearer token before the existing API authentication and endpoint pipeline runs. Requests without an authenticated BFF session continue through the existing external API bearer/API-key path. Controllers, CQRS handlers, HAL behavior, and generated clients remain unchanged.

### Explicit Non-Goals

- Direct Blazor-to-MediatR or Blazor-to-persistence calls.
- Replacing the default split-host topology.
- New REST endpoints, `/api/v1` routes, controller behavior, generated API contracts, or UI features.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| API and Blazor are separate executable composition roots. | `src/Explore.API/Program.cs`, `src/Explore.Blazor/Program.cs`, their `.csproj` files | High | API owns lower-layer registration; Blazor owns BFF/UI registration. |
| The API pipeline has security- and tenancy-sensitive ordering. | `src/Explore.API/Program.cs`, `docs/ARCHITECTURE.md`, `docs/QUICK_REFERENCE.md` | High | Extraction must preserve order, not merely registrations. |
| Browser API traffic currently uses cookie-authenticated BFF → YARP → bearer-authenticated API. | `src/Explore.Blazor/Program.cs`, `docs/BLAZOR.md`, `docs/SECURITY-MODEL.md`, `.agents/skills/blazor-bff-patterns/` | High | Tokens and trusted headers must remain server-owned. |
| API routes are `/api/...`; version selection is media type, query, or header based. | API versioning registration and controller conventions | High | Do not introduce `/api/v1/...`. |
| API controllers reside in the `Explore.API` assembly. | `src/Explore.API/Controllers/`, `src/Explore.API/Program.cs` | High | Standalone MVC must add the API assembly as an application part. |
| Razor root components and static assets reside in the Blazor host/client assemblies. | `src/Explore.Blazor/Program.cs`, `src/Explore.Blazor/Explore.Blazor.csproj` | High | Standalone must explicitly root Razor/static-web-asset discovery in these assemblies. |
| Aspire currently starts migration service → API → Blazor. | `src/Explore.AppHost/AppHost.cs`, `docs/OPERATIONS.md` | High | Standalone must be opt-in and wait directly for the same prerequisites. |
| Existing named development HTTPS ports are API 7039 and Blazor 7177. | both `Properties/launchSettings.json` files | High | Reserve standalone HTTPS 7180 and HTTP 5180. |
| The baseline build is already red before plan edits. | `dotnet build --configuration Release --verbosity quiet` on 2026-08-02 | High | 13 errors and 905 warnings; representative failures are listed below. |

### 2.2 Existing Implementation

#### API host

`src/Explore.API/Program.cs` is the only current composition root allowed to reference Application, Infrastructure, and Persistence. It registers controllers, API versioning, HATEOAS, tenant resolution, authentication/authorization, rate limiting, idempotency, output caching, OpenAPI/MCP, workers, migration/seeding startup, the privacy-erasure startup gate, setup-secret initialization, health, and graceful shutdown. Its middleware order is part of the API contract.

#### Blazor/BFF host

`src/Explore.Blazor/Program.cs` owns OIDC and HttpOnly cookie sessions, dynamic schemes, antiforgery, BFF endpoints, trusted token/header forwarding, YARP `/api/*`, Razor Components, InteractiveServer and InteractiveWASM render modes, SignalR, localization, static assets, and the `Explore.Blazor.Client` additional assembly. UI code communicates through generated API clients and does not reference lower application layers.

#### Shared hosting code

Both hosts already expose focused extension methods, and `src/Event.Web.BffHosting` owns reusable BFF primitives. The remaining problem is top-level orchestration: large `Program.cs` files still contain registration, pipeline, startup, and endpoint composition that cannot be safely reused by a third host.

#### Orchestration

`src/Explore.AppHost/AppHost.cs` models migration, API, and Blazor as separate Aspire resources. Keycloak callbacks target the Blazor endpoint. Split mode uses API discovery via Aspire metadata or `ExploreApi:BaseUrl`/`API_ENDPOINT`.

### 2.3 Existing Tests And Verification Coverage

- `tests/Event.Architecture.Tests` protects Clean Architecture and host/client reference boundaries.
- `tests/Event.API.IntegrationTests` protects API middleware, controllers, authentication, tenancy, and HTTP behavior.
- `tests/Explore.Blazor.IntegrationTests` protects BFF/auth/proxy and host behavior.
- `tests/Explore.Blazor.Client.Tests` protects client behavior but is not a primary host-composition gate.
- There is no standalone-host integration project and no current test proving cookie and external API authentication can coexist on one `/api/*` endpoint graph.

### 2.4 Existing Documentation And Contracts

- `docs/ARCHITECTURE.md` and `docs/CODEBASE_STRUCTURE.md` define composition roots and dependency direction.
- `docs/BLAZOR.md` and `docs/SECURITY-MODEL.md` define the browser BFF trust boundary.
- `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, and `docs/TROUBLESHOOTING.md` describe startup topology, settings, ports, and recovery.
- `Explore.slnx` is the canonical solution manifest.
- Existing API/OpenAPI output and generated `IEventApiClient` contracts must not change.

### 2.5 Current Pain Points / Improvement Areas

- Host composition is embedded in two top-level programs, so a third host would otherwise duplicate roughly 900 lines and immediately drift.
- A naïve same-origin YARP destination pointing to the standalone host would recurse or require a second listener, defeating the requested topology.
- Combining cookie/OIDC and API bearer/API-key registration creates default-scheme and middleware-order collision risk.
- API workers, migration/seeding, startup gates, health, service defaults, and shutdown can be accidentally registered or executed twice.
- Referencing a Razor host assembly does not by itself prove correct static web asset and root-component discovery.

### 2.6 Unknowns After Investigation

| Unknown | Investigation performed | Owning task |
|---|---|---|
| Exact static-web-asset base-path behavior when a Web SDK project references another Web SDK project | Existing project/startup files and docs establish ownership but not runtime output of the future project | Tasks 3.3 and 3.4 resolve it without speculative copying. |
| Whether every API authorization policy accepts the claims produced after local bearer validation | Existing auth registrations show the shared API path, but only combined-host execution proves policy selection | Tasks 3.2 and 3.4. |
| Whether current AppHost configuration has a reusable topology selector | Current AppHost has only split registration | Task 4.1 introduces one explicit selector with split as default. |

## 3. Proposed Future State

`Explore.API` and `Explore.Blazor` expose public, owning-assembly host modules for services, startup actions, middleware, and endpoints. Their existing `Program.cs` files become thin callers of those modules and preserve split-host behavior. `Event.Standalone` references both assemblies and composes the same modules once in a unified, explicitly ordered pipeline.

One listener serves:

- UI, static assets, SignalR, `/auth/*`, `/bff/*`, `/oauth/*`, and manifest endpoints through Blazor/BFF composition;
- `/api/*` controllers, API health/OpenAPI/MCP surfaces, and external/mobile consumers through API composition;
- browser `/api/*` calls through a pre-authentication BFF bridge, then the same API authentication/authorization/controller endpoint graph used by external clients.

The bridge flow is:

1. Match `/api/*` before API authentication.
2. Classify with an explicit cookie-scheme `AuthenticateAsync`; do not install the cookie principal as the API request principal. If no valid BFF session cookie exists, do not alter the request; normal bearer/API-key processing continues.
3. If a valid BFF session exists, enforce the existing proxy antiforgery contract for unsafe methods.
4. Retrieve a usable access token from the server-side authentication ticket. If it is missing, expired, unavailable, or cannot be refreshed, strip privileged inbound headers and fail `401/403` before controller execution; never fall through to external authentication.
5. Remove inbound `Authorization`, support-access, setup-secret, and tenant headers that browsers must not control.
6. Apply the same trusted tenant/setup/support enrichment used by YARP and place the server-held bearer token on the in-process request.
7. Clear/replace the classification principal and continue into existing API authentication so the injected bearer token is validated and becomes the only principal used by controller authorization; then continue through tenant, authorization, idempotency, caching, and controllers.

### Unified Pipeline Ownership

| Surface | Owning pipeline/endpoints | Must not run |
|---|---|---|
| `/api/*` | Browser bridge classification, API tenant/auth/rate-limit/authorization/idempotency/cache, API controllers | BFF endpoint antiforgery for external clients; UI/static middleware |
| API-owned `/mcp` and OpenAPI/Swagger/Scalar routes | Existing API gate/auth and endpoint modules, scoped exactly as in the API host | Cookie classification unless the existing API contract explicitly routes through it |
| `/auth/*`, `/bff/*`, `/oauth/*`, manifest | BFF cookie/auth, antiforgery where currently required, BFF rate limits, existing endpoint modules | API tenant/idempotency/output-cache pipeline |
| Static assets, Razor Components, SignalR | Blazor security/default middleware and Razor/static endpoint modules | API auth/rate/idempotency; mutation antiforgery intended for BFF/API calls |
| Health/liveness/readiness | One Standalone health endpoint set with local API/startup readiness | Remote self-readiness polling or duplicate API/Blazor health mapping |

## 4. Non-Negotiable Constraints

1. Preserve the Clean Architecture boundary: Blazor projects do not reference Domain/Application/Persistence/Infrastructure; only composition roots may wire lower layers.
2. Keep browser tokens server-side in HttpOnly session state. Never return them to components, JavaScript, WASM, logs, or response headers.
3. Keep `/api/*` as one controller endpoint graph. Do not duplicate routes for browser and external callers.
4. Preserve API pipeline ordering and BFF route-before-catch-all ordering.
5. Preserve HAL as the UI affordance source of truth and leave controllers/generated contracts unchanged.
6. Preserve trusted tenant resolution and strip/reconstruct privileged browser headers before API processing.
7. Register and execute API workers, startup migration/seeding, privacy gate, setup-secret initialization, service defaults, health, and shutdown exactly once.
8. Preserve `RuntimeRenderPolicyService`: the fallback is `InteractiveServer`, tenant policy may select `InteractiveAuto` or `InteractiveWebAssembly`, onboarding remains forced to `InteractiveServer`, and components may not depend on `HttpContext`.
9. Keep split hosting as the default configuration and regression-test both profiles.
10. SQLite is the default provider for Event.Standalone; operators may override via `DATABASE_PROVIDER` environment variable or Infisical secret to select PostgreSQL, SQL Server, MariaDB, or MySQL. Provider selection follows the `multi-database-support` workstream contract.
11. Every new source or documentation file begins with two `ABOUTME:` lines.
12. Run only project-scoped Release tests, never solution-level `dotnet test`.

## 5. Architecture And Design Decisions

### Decision A: Extract host modules; do not duplicate programs

- **Decision:** Move reusable service/startup/pipeline/endpoint composition into public extensions in the existing owning host assemblies. Existing programs and Standalone call them.
- **Why:** One implementation preserves behavior across profiles and minimizes drift.
- **Alternatives considered:** Copy both programs into Standalone; make one executable launch the other; move all host code to a new generic framework project.
- **Consequences:** Existing host programs change structurally, so integration and architecture tests must lock their behavior. No speculative generic hosting framework is introduced.
- **Files/layers affected:** `Explore.API`, `Explore.Blazor`, their tests.

### Decision B: Keep one `/api/*` endpoint graph and bridge browser credentials in-process

- **Decision:** A BFF bridge middleware handles authenticated browser requests before the existing API authentication pipeline. It reuses shared trusted-request enrichment and injects the server-held bearer token; it does not proxy over HTTP.
- **Why:** This preserves server-side tokens and API policy evaluation without loopback, recursion, a second port, duplicate controllers, or direct UI-to-application calls.
- **Alternatives considered:** YARP self-proxy; direct MediatR calls; cookie-authenticate API controllers; duplicate browser-only controller routes.
- **Consequences:** Middleware classification and order are security-sensitive. Unsafe cookie-backed API calls require antiforgery. External clients remain unchanged.
- **Files/layers affected:** `Event.Web.BffHosting`, `Explore.Blazor` YARP transforms, `Event.Standalone`, integration tests.

### Decision C: Use an explicit host profile for Blazor composition

- **Decision:** Add a small `BlazorHostProfile` (`Split` or `Combined`) consumed by registration/pipeline modules. Split registers YARP and remote API readiness; Combined registers the local bridge and local API readiness.
- **Why:** The transport difference is real and must be explicit; scattered booleans or environment checks invite mixed profiles.
- **Alternatives considered:** Runtime detection from URLs; duplicate registration methods; always register YARP but leave it unused.
- **Consequences:** One public profile type is added. All profile-specific services must be covered by integration tests.
- **Files/layers affected:** `Explore.Blazor`, `Event.Standalone`.

### Decision D: Standalone is an independent composition root

- **Decision:** `Event.Standalone` is a Web SDK executable referencing `Explore.API` and `Explore.Blazor`, with API MVC application parts and Blazor Razor/static asset assemblies explicitly registered. It owns one shutdown state and startup sequence.
- **Why:** It fulfills the one-process/one-port requirement while retaining each assembly's ownership.
- **Alternatives considered:** Convert API or Blazor into the combined default; create a generic host outside both assemblies.
- **Consequences:** Solution and architecture rules must recognize three valid composition roots, while split remains default.
- **Files/layers affected:** new standalone project, `Explore.slnx`, architecture tests.

### Decision E: Aspire topology selection is explicit and defaults to Split

- **Decision:** `Explore.AppHost` reads `Hosting:Topology` (`Split` default, `Standalone` opt-in; environment form `Hosting__Topology=Standalone`). Standalone receives one endpoint and waits directly for migrations and shared infrastructure. Keycloak callbacks target the selected UI endpoint.
- **Why:** Operators need deterministic topology without replacing current deployment behavior.
- **Alternatives considered:** Always start all three resources; infer topology from missing URLs; replace split mode.
- **Consequences:** AppHost registration becomes conditional, but infrastructure parameters remain shared.
- **Files/layers affected:** `Explore.AppHost`, configuration and operations docs.

### Decision F: Reserve development ports 5180/7180

- **Decision:** Standalone launch settings use HTTP 5180 and HTTPS 7180; launch settings are the source of truth.
- **Why:** They are stable and do not conflict with API 5035/7039 or Blazor 5144/7177.
- **Alternatives considered:** Reuse an existing port; dynamic-only ports.
- **Consequences:** Documentation and Keycloak callback generation must use the selected endpoint.
- **Files/layers affected:** standalone launch settings, AppHost, configuration/operations docs.

### Decision G: Middleware branch strategy uses explicit UseWhen path-prefix isolation

- **Decision:** Use `UseWhen` middleware branching with path-prefix predicates to isolate API and Blazor/BFF pipelines. API pipeline (`/api/*`, `/openapi/*`, `/mcp`) gets bridge → bearer auth → tenant → authz → rate-limit → idempotency → output-cache. Blazor pipeline (everything else) gets static files → antiforgery → cookie auth → Razor.
- **Why:** Global middleware ordering in a combined host is fragile and security-sensitive. Explicit `UseWhen` branching makes pipeline ownership visible in code and prevents API rate-limiting from executing on static asset requests or BFF antiforgery from rejecting external API clients.
- **Alternatives considered:** Global middleware ordering with route-aware conditional logic; `MapWhen` with terminal branches; separate `IApplicationBuilder` pipelines.
- **Consequences:** Middleware registration is more verbose but pipeline isolation is explicit and testable. Each branch's middleware order is independently verifiable.
- **Files/layers affected:** `Event.Standalone/Program.cs`, standalone integration tests.

### Decision H: SQLite as default persistence with provider override

- **Decision:** Event.Standalone defaults to SQLite with a local persistent file (`/app/data/event.db`). Operators may override by setting `DATABASE_PROVIDER=PostgreSQL` (or `SqlServer`, `MariaDb`, `MySql`) plus structured connection fields via environment variables or Infisical secrets. Provider selection uses the same `DatabaseOptions` contract defined by the `multi-database-support` workstream.
- **Why:** SQLite eliminates the database container for small self-hosters, achieving the single-container deployment goal. Provider override preserves the path to production-grade databases for larger operators.
- **Alternatives considered:** Require PostgreSQL always; auto-detect based on connection string presence; separate standalone images per provider.
- **Consequences:** The standalone image must include SQLite native binaries. WAL mode and busy-timeout PRAGMAs must be set at startup. Single-replica constraint applies when using SQLite. Docker volume mount is required for data persistence.
- **Files/layers affected:** `Event.Standalone/Program.cs`, `Event.Standalone/appsettings.json`, `Dockerfile`, Docker Compose, `multi-database-support` workstream Phase 4.

### Decision I: Single-container Docker image with volume mount

- **Decision:** Ship a multi-stage `Dockerfile` producing a single container image (`islamu/event-standalone`) that includes the .NET runtime, Event.Standalone binary, and SQLite native libraries. Data is persisted via a Docker volume mounted at `/app/data`. The Dockerfile exposes one port (8080) and accepts all configuration via environment variables.
- **Why:** A `docker run -v data:/app/data -p 8080:8080 islamu/event-standalone` one-liner is the target operator experience for minimal self-hosting.
- **Alternatives considered:** Separate API and Blazor images; require external database always; use Docker Compose only.
- **Consequences:** The image is larger than API-only (~200MB vs ~150MB) but eliminates all external dependencies except DNS/TLS. Compose files become optional convenience rather than requirements.
- **Files/layers affected:** `src/Event.Standalone/Dockerfile`, `docker-compose.standalone.yml`, `docs/SELF_HOSTING.md`.

## 6. Implementation Phases

### Phase 1: Reusable API Host Composition

- **Goal:** Make API composition reusable without changing split-host behavior.
- **Depends on:** Pre-existing baseline build errors are resolved by their owning workstream or explicitly waived by the user; do not absorb those fixes here.
- **Relevant files:** existing `src/Explore.API/Program.cs`; new files under `src/Explore.API/Hosting/`; existing/new API integration tests.
- **Related skills/rules:** `clean-architecture-rules`, API controller and test rules.
- **Acceptance criteria:** The existing API program is a thin composition caller; API pipeline order and startup gates remain intact; no worker/default/health registration is duplicated.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep extracted code in owning assemblies and revert the caller to the last behaviorally equivalent module boundary; do not copy code into Standalone to bypass a failed extraction.

#### Task 1.1: Extract API host composition

- **Type:** modify/create
- **Layer:** API
- **Files:** `src/Explore.API/Program.cs` (existing); `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` (new); `src/Explore.API/Hosting/ApiHostStartupExtensions.cs` (new); `src/Explore.API/Hosting/ApiHostApplicationExtensions.cs` (new); affected `tests/Event.API.IntegrationTests/**` (existing/new).
- **Description:** Extract the current registrations, pre-run startup gates, ordered middleware, and endpoints without changing their behavior. Expose the controller assembly marker/application-part registration needed by another composition root. Keep the shutdown token and 25-second API grace behavior injectable from the caller so Standalone owns only one shutdown state.
- **Acceptance Criteria:**
  - [ ] Existing API startup, middleware, controllers, workers, health, OpenAPI/MCP, migration/seeding, privacy gate, and setup-secret initialization are invoked once through the module.
  - [ ] API middleware order is protected by focused integration assertions where existing coverage is insufficient.
  - [ ] `Explore.API/Program.cs` contains composition and run only.
- **Dependencies:** None.
- **Effort:** XL
- **Required Skills/Rules:** `clean-architecture-rules`, `.claude/rules/api-controllers.md`, `.claude/rules/tests.md`.

### Phase 2: Reusable Blazor/BFF Host Composition

- **Goal:** Make Blazor/BFF composition reusable through explicit Split and Combined profiles without changing split-host behavior or architecture boundaries.
- **Depends on:** Phase 1 complete.
- **Relevant files:** existing `src/Explore.Blazor/Program.cs`; new files under `src/Explore.Blazor/Hosting/`; existing/new Blazor integration and architecture tests.
- **Related skills/rules:** `clean-architecture-rules`, `blazor-bff-patterns`, Blazor server and test rules.
- **Acceptance criteria:** The existing Blazor program is a thin Split-profile caller; Split retains YARP and remote readiness; Combined exposes local bridge/readiness hooks; root components/static assets/render modes remain reusable; architecture boundaries remain green.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep extracted code in the Blazor assembly and restore the Split caller to the last behaviorally equivalent boundary; do not duplicate it in Standalone.

#### Task 2.1: Extract profile-aware Blazor/BFF composition

- **Type:** modify/create
- **Layer:** Blazor
- **Files:** `src/Explore.Blazor/Program.cs` (existing); `src/Explore.Blazor/Hosting/BlazorHostProfile.cs` (new); `src/Explore.Blazor/Hosting/BlazorHostServiceCollectionExtensions.cs` (new); `src/Explore.Blazor/Hosting/BlazorHostApplicationExtensions.cs` (new); affected `tests/Explore.Blazor.IntegrationTests/**` (existing/new).
- **Description:** Extract Blazor/BFF services, dynamic-scheme initialization, ordered middleware, BFF endpoints, static assets, and Razor component mapping. `Split` must retain YARP and `ApiReadinessHealthCheck`; `Combined` must omit both and expose hooks for the local API bridge/readiness.
- **Acceptance Criteria:**
  - [ ] Split profile behavior and endpoint ordering remain unchanged.
  - [ ] InteractiveServer, InteractiveWASM, SignalR, root `App`, and `Explore.Blazor.Client` additional assembly registrations are reusable, while `RuntimeRenderPolicyService` retains its current route-aware fallback and override behavior.
  - [ ] Profile-specific services cannot be accidentally enabled together.
- **Dependencies:** 1.1.
- **Effort:** XL
- **Required Skills/Rules:** `blazor-bff-patterns`, `blazor-ui-conventions`, `.claude/rules/blazor-server.md`, `.claude/rules/tests.md`.

#### Task 2.2: Extend architecture coverage for reusable hosts

- **Type:** modify
- **Layer:** Tests
- **Files:** affected `tests/Event.Architecture.Tests/**` (existing); `Explore.slnx` only if test discovery metadata requires it.
- **Description:** Codify allowed composition-root references and prevent Blazor/Client from gaining lower-layer references during extraction. Keep controllers in API and shared BFF primitives free of API/Application dependencies.
- **Acceptance Criteria:**
  - [ ] Existing split host boundaries remain enforced.
  - [ ] Public host modules are callable by a composition root without exposing lower layers to Blazor.Client.
  - [ ] No new circular project reference exists.
- **Dependencies:** 1.1, 2.1.
- **Effort:** M
- **Required Skills/Rules:** `clean-architecture-rules`, `.claude/rules/tests.md`.

### Phase 3: Single-Process Host And Security Bridge

- **Goal:** Add the one-process/one-port host with both browser and external API authentication paths.
- **Depends on:** Phase 2 complete.
- **Relevant files:** new `src/Event.Standalone/**`; existing `src/Event.Web.BffHosting/**`, YARP transform files in `src/Explore.Blazor/**`, `Explore.slnx`; new `tests/Event.Standalone.IntegrationTests/**`.
- **Related skills/rules:** `clean-architecture-rules`, `blazor-bff-patterns`, API controller, Blazor server, and test rules.
- **Acceptance criteria:** HTTPS 7180/HTTP 5180 serve UI/static assets/SignalR/BFF/API from one application; browser API mutations enforce antiforgery and use server-held tokens; external bearer/API-key clients retain existing behavior; workers/startup/health are singletons; no self-proxy exists.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Standalone.IntegrationTests/Event.Standalone.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Disable/remove the optional standalone project and leave split callers on the extracted modules; never weaken authentication or bypass API policy evaluation to make combined tests pass.

#### Task 3.1: Create the standalone composition root

- **Type:** create/modify
- **Layer:** API/Blazor composition
- **Files:** `src/Event.Standalone/Event.Standalone.csproj` (new); `src/Event.Standalone/Program.cs` (new); `src/Event.Standalone/appsettings.json` (new); `src/Event.Standalone/appsettings.Development.json` (new if environment-specific values are required); `src/Event.Standalone/Properties/launchSettings.json` (new); `Explore.slnx` (existing).
- **Description:** Add a .NET 10 Web SDK project referencing both host assemblies and invoke the extracted modules once. Configure one shutdown state, one service-default/health set, one startup sequence, and launch ports 5180/7180. Do not add database-provider settings.
- **Acceptance Criteria:**
  - [ ] Project builds as an independent startup project and appears in the solution's API/UI hosting group.
  - [ ] API and Blazor registrations coexist without duplicate service defaults, health endpoints, workers, migrations, or startup gates.
  - [ ] No YARP destination targets the Standalone listener.
- **Dependencies:** 1.1, 2.1, 2.2.
- **Effort:** L
- **Required Skills/Rules:** `clean-architecture-rules`, `.claude/rules/blazor-server.md`, `.claude/rules/api-controllers.md`.

#### Task 3.2: Share trusted BFF request enrichment and add the in-process bridge

- **Type:** modify/create
- **Layer:** Blazor/BFF
- **Files:** owning trusted-header/token-forwarding services under `src/Event.Web.BffHosting/**` (existing/new); affected YARP transform/registration files under `src/Explore.Blazor/**` (existing); `src/Event.Standalone/Middleware/CombinedApiBridgeMiddleware.cs` (new); affected standalone integration tests (new).
- **Description:** Extract the transport-independent setup-secret, tenant, support-access, and inbound-header sanitization from YARP-specific transforms. Reuse it from both YARP and the combined bridge. For `/api/*`, explicitly authenticate the BFF cookie; on success enforce antiforgery for unsafe methods, retrieve the server-side token, sanitize/reconstruct headers, set the local Authorization header, and continue into existing API tenant/auth/authorization middleware. On no valid cookie, leave the external-client request on the existing API path.
- **Acceptance Criteria:**
  - [ ] Browser-controlled privileged headers never survive either transport.
- [ ] Tokens remain server-side and are not logged or returned.
- [ ] Cookie-backed unsafe requests without valid antiforgery fail before controller execution.
- [ ] A valid cookie session with no usable/refreshed access token strips privileged headers and fails `401/403` before controller execution; it never falls through to bearer/API-key classification.
- [ ] Cookie authentication is used only to classify/enrich the request; existing API bearer authentication validates the injected token and supplies the sole controller authorization principal.
- [ ] External bearer/API-key requests are not forced through cookie authentication.
  - [ ] Existing split-host YARP tests continue to exercise the same trusted enrichment.
- **Dependencies:** 3.1.
- **Effort:** XL
- **Required Skills/Rules:** `blazor-bff-patterns`, `.claude/rules/blazor-server.md`, `.claude/rules/tests.md`.

#### Task 3.3: Compose the unified middleware and endpoint graph

- **Type:** modify
- **Layer:** API/Blazor composition
- **Files:** `src/Event.Standalone/Program.cs` (new); API host application extensions from Phase 1 (new); Blazor host application extensions from Phase 2 (new); affected integration tests (new).
- **Description:** Implement the exact surface ownership table in §3: common forwarded headers/shutdown/error/security handling first; branch `/api/*`, API-owned MCP/OpenAPI routes, BFF endpoint routes, UI/static/Razor/SignalR, and health explicitly; then map each owning endpoint module. Add API MVC application parts and Blazor root/additional assemblies explicitly. Use Web SDK project-reference static-web-asset discovery and explicit Razor component assemblies; do not copy `wwwroot` or generated static assets into Standalone.
- **Acceptance Criteria:**
  - [ ] `/api/...` maps once and preserves existing versioning/HAL/controller behavior.
  - [ ] `/auth/*`, `/bff/*`, `/oauth/*`, manifest, static assets, Razor Components, and SignalR are reachable in the endpoint data source.
- [ ] API middleware does not process static assets/UI callbacks, and BFF antiforgery does not reject external API clients.
- [ ] Static assets come from referenced Web SDK projects and Razor roots/additional assemblies are explicit; no asset duplication is introduced.
  - [ ] API startup work and hosted workers execute once.
- **Dependencies:** 3.2.
- **Effort:** XL
- **Required Skills/Rules:** `clean-architecture-rules`, `blazor-bff-patterns`, API controller and Blazor server rules.

#### Task 3.4: Add standalone-host integration coverage

- **Type:** create
- **Layer:** Tests
- **Files:** `tests/Event.Standalone.IntegrationTests/Event.Standalone.IntegrationTests.csproj` (new); focused factory/fixture and test files under that directory (new); `Explore.slnx` (existing).
- **Description:** Use the repository's TUnit/WebApplicationFactory patterns to validate endpoint discovery, application parts/static assets, auth selection, antiforgery, header sanitization, readiness, and singleton startup/hosted-service ownership. Reuse test infrastructure where it is already shared; do not duplicate full API or Blazor suites.
- **Acceptance Criteria:**
  - [ ] Tests cover UI/Razor/SignalR endpoint registration and API controller discovery.
- [ ] Tests cover authenticated browser GET, authenticated browser mutation with/without antiforgery, external bearer/API-key request, and unauthenticated request.
- [ ] Tests cover a valid cookie session with missing/expired/unrefreshable token and prove fail-closed behavior plus cookie-principal isolation.
  - [ ] Tests prove privileged inbound browser headers are stripped/replaced.
  - [ ] Tests prove no loopback proxy destination and no duplicate worker/startup registration.
- **Dependencies:** 3.1, 3.2, 3.3.
- **Effort:** XL
- **Required Skills/Rules:** `.claude/rules/tests.md`, `blazor-bff-patterns`.

### Phase 4: Optional Aspire Topology And Operator Contract

- **Goal:** Make Standalone selectable without changing the default topology, and document the supported operator/developer path.
- **Depends on:** Phase 3 complete.
- **Relevant files:** `src/Explore.AppHost/AppHost.cs`, AppHost configuration files if present/required, `Explore.slnx`, architecture tests, and exact runtime topology docs.
- **Related skills/rules:** `clean-architecture-rules`, documentation style guide, tests rule.
- **Acceptance criteria:** Split remains the default; `Hosting__Topology=Standalone` registers migration service plus one standalone web resource; selected callbacks/dependencies/ports are correct; docs consistently distinguish split and standalone; SQLite and Compose remain excluded.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Retain Split as unconditional default and remove only the optional branch if orchestration cannot satisfy dependency/callback invariants; do not run split and standalone resources together as a fallback.

#### Task 4.1: Add explicit Aspire topology selection

- **Type:** modify
- **Layer:** DevOps
- **Files:** `src/Explore.AppHost/AppHost.cs` (existing); exact AppHost settings file only if repository convention already uses one (existing/new); affected architecture/configuration tests (existing/new).
- **Description:** Parse `Hosting:Topology` with supported values `Split` and `Standalone`, defaulting to Split and failing fast on unknown values. Inventory every environment/configuration input currently forwarded to API and Blazor, then forward each required key exactly once to Standalone. Register the migration service and one `Event.Standalone` resource, wait directly for migrations/infrastructure, and generate Keycloak callbacks from the standalone endpoint. Do not register API/Blazor resources in that branch.
- **Acceptance Criteria:**
  - [ ] No setting preserves today's split resource graph.
  - [ ] Standalone selection creates exactly one web resource after migration prerequisites.
- [ ] Unknown topology values produce an actionable startup error.
- [ ] Keycloak callbacks and service references use the selected UI/API endpoint.
- [ ] The existing API/Blazor AppHost input inventory is recorded in code/tests or the owning docs, and each Standalone input is forwarded exactly once.
- **Dependencies:** 3.4.
- **Effort:** L
- **Required Skills/Rules:** `clean-architecture-rules`, `.claude/rules/tests.md`.

#### Task 4.2: Update architecture and operations documentation

- **Type:** modify
- **Layer:** Docs
- **Files:** `docs/ARCHITECTURE.md`, `docs/CODEBASE_STRUCTURE.md`, `docs/BLAZOR.md`, `docs/SECURITY-MODEL.md`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md` (all existing).
- **Description:** Document three composition roots, the unchanged default split topology, standalone ports/startup command/configuration, one-process request flows, browser bridge trust rules, health/startup ownership, callback behavior, and recovery to Split. Link drift-prone claims to code/config/tests. Explicitly state that Standalone does not alter persistence provider behavior and that Compose packaging is not yet supplied.
- **Acceptance Criteria:**
  - [ ] Developer and operator docs use `/api/...`, never `/api/v1/...` as the canonical route.
  - [ ] Browser and external API flows are distinguished, including antiforgery and privileged-header handling.
  - [ ] Port/configuration/topology instructions agree across all documents.
  - [ ] Limitations and rollback to Split are explicit.
- **Dependencies:** 4.1.
- **Effort:** L
- **Required Skills/Rules:** `docs/DOCUMENTATION_STYLE_GUIDE.md`, `AGENTS.md`.

#### Task 4.3: Lock composition-root and topology invariants

- **Type:** modify
- **Layer:** Tests
- **Files:** affected `tests/Event.Architecture.Tests/**` (existing); affected AppHost test/config inspection files if they exist (existing/new).
- **Description:** Extend architecture assertions to allow Standalone as a composition root, ensure Blazor and Client boundaries remain intact, require all three projects in `Explore.slnx`, and protect Split as the default topology without launching Aspire.
- **Acceptance Criteria:**
  - [ ] Architecture tests recognize Standalone but do not broaden lower-layer access for Blazor or Client.
  - [ ] Solution membership and project-reference direction are asserted.
  - [ ] Static inspection/config tests prove Split default and Standalone opt-in registration are mutually exclusive.
- **Dependencies:** 4.1, 4.2.
- **Effort:** M
- **Required Skills/Rules:** `clean-architecture-rules`, `.claude/rules/tests.md`.

### Phase 5: SQLite Default Persistence And Provider Override

- **Goal:** Make Event.Standalone default to SQLite and allow operators to override the provider via environment variables or Infisical secrets.
- **Depends on:** Phase 4 complete. The `multi-database-support` workstream Phase 1 (structured `DatabaseOptions` contract) must be landed or co-developed.
- **Relevant files:** `src/Event.Standalone/Program.cs`; `src/Event.Standalone/appsettings.json`; `multi-database-support` Phase 4 SQLite provider registration; existing/new standalone integration tests.
- **Related skills/rules:** `dotnet-efcore-guidelines`, `clean-architecture-rules`.
- **Acceptance criteria:** Default startup with no database configuration uses SQLite at `/app/data/event.db` with WAL mode enabled; `DATABASE_PROVIDER=PostgreSQL` plus structured fields switches to PostgreSQL; invalid provider/missing required fields fail-fast at startup; SQLite data survives container restart via volume mount; single-replica constraint is enforced for SQLite.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Standalone.IntegrationTests/Event.Standalone.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Revert to requiring explicit database configuration; do not silently fall back to in-memory or create unpersistedfiles.

#### Task 5.1: Integrate SQLite default provider with standalone composition

- **Type:** modify
- **Layer:** Persistence/Composition
- **Files:** `src/Event.Standalone/Program.cs` (existing); `src/Event.Standalone/appsettings.json` (existing); persistence registration extensions from `multi-database-support` workstream.
- **Description:** When no `DATABASE_PROVIDER` is configured, default to SQLite with connection string `Data Source=/app/data/event.db;Cache=Shared`. Execute `PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;` via a connection interceptor or startup hook. When `DATABASE_PROVIDER` is set, delegate to the structured `DatabaseOptions` provider-selection logic from the `multi-database-support` workstream.
- **Acceptance Criteria:**
  - [ ] Default standalone startup creates and uses `/app/data/event.db` without any database configuration.
  - [ ] WAL mode is enabled and verified at startup.
  - [ ] `DATABASE_PROVIDER=PostgreSQL` with structured fields switches to PostgreSQL.
  - [ ] Missing required fields for a non-SQLite provider produce actionable startup errors.
  - [ ] SQLite busy-timeout prevents `SQLITE_BUSY` under concurrent API requests.
- **Dependencies:** 4.3, multi-database-support Phase 1.
- **Effort:** L
- **Required Skills/Rules:** `dotnet-efcore-guidelines`, `clean-architecture-rules`.

#### Task 5.2: Add provider-override integration tests

- **Type:** modify
- **Layer:** Tests
- **Files:** affected `tests/Event.Standalone.IntegrationTests/**`.
- **Description:** Add tests proving SQLite default behavior, WAL mode activation, provider override to PostgreSQL, fail-fast on invalid configuration, and single-replica SQLite enforcement.
- **Acceptance Criteria:**
  - [ ] Tests prove SQLite is used when no provider is configured.
  - [ ] Tests prove PostgreSQL is used when `DATABASE_PROVIDER=PostgreSQL` is set with valid structured fields.
  - [ ] Tests prove startup failure with actionable diagnostics when required fields are missing.
- **Dependencies:** 5.1.
- **Effort:** M
- **Required Skills/Rules:** `.claude/rules/tests.md`.

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS

- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Standalone.IntegrationTests/Event.Standalone.IntegrationTests.csproj --configuration Release --verbosity quiet`

### Phase 6: Docker Packaging

- **Goal:** Produce a single Docker image for Event.Standalone with SQLite default, volume mount, and environment-variable configuration.
- **Depends on:** Phase 5 complete.
- **Relevant files:** new `src/Event.Standalone/Dockerfile`; new `docker-compose.standalone.yml`; `docs/SELF_HOSTING.md`.
- **Related skills/rules:** `clean-architecture-rules`, documentation style guide.
- **Acceptance criteria:** `docker build` produces a working image; `docker run -v data:/app/data -p 8080:8080 islamu/event-standalone` starts successfully with SQLite default; environment variables override provider and all configuration; health endpoint responds; data persists across container restarts.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `docker build -t islamu/event-standalone -f src/Event.Standalone/Dockerfile .` (manual build verification)
- **Rollback / failure handling:** Remove Dockerfile and Compose artifacts; standalone remains usable via `dotnet run` without packaging.

#### Task 6.1: Create multi-stage Dockerfile

- **Type:** create
- **Layer:** DevOps
- **Files:** `src/Event.Standalone/Dockerfile` (new).
- **Description:** Multi-stage build: SDK stage restores and publishes Event.Standalone as a self-contained or framework-dependent deployment. Runtime stage uses `mcr.microsoft.com/dotnet/aspnet:10.0` base, copies published output, creates `/app/data` directory with appropriate permissions, sets `ASPNETCORE_URLS=http://+:8080`, and configures `ENTRYPOINT`. Include SQLite native binaries in the runtime image.
- **Acceptance Criteria:**
  - [ ] `docker build` completes without errors.
  - [ ] Runtime image exposes port 8080.
  - [ ] `/app/data` directory exists with write permissions for the container user.
  - [ ] SQLite native binaries are present in the runtime image.
  - [ ] Image size is under 250MB.
- **Dependencies:** 5.2.
- **Effort:** M
- **Required Skills/Rules:** None specific.

#### Task 6.2: Create standalone Docker Compose file

- **Type:** create
- **Layer:** DevOps
- **Files:** `docker-compose.standalone.yml` (new).
- **Description:** Minimal Compose file with one `event-standalone` service, a named volume for `/app/data`, port mapping `8080:8080`, and example environment variables for provider override. Include comments explaining each configuration option.
- **Acceptance Criteria:**
  - [ ] `docker compose -f docker-compose.standalone.yml up` starts Event.Standalone with SQLite default.
  - [ ] Data volume persists across `docker compose down` and `up`.
  - [ ] Commented examples show PostgreSQL override configuration.
- **Dependencies:** 6.1.
- **Effort:** S
- **Required Skills/Rules:** None specific.

#### Task 6.3: Update self-hosting documentation

- **Type:** modify
- **Layer:** Docs
- **Files:** `docs/SELF_HOSTING.md` (existing); `docs/CONFIGURATION.md` (existing).
- **Description:** Add a "Quick Start" section with the `docker run` one-liner. Document the standalone deployment profile, SQLite default behavior, provider override via environment variables / Infisical, volume mount requirements, single-replica constraint, and upgrade/backup procedures for SQLite.
- **Acceptance Criteria:**
  - [ ] `docker run -v data:/app/data -p 8080:8080 islamu/event-standalone` is documented as the minimal deployment.
  - [ ] Provider override examples cover PostgreSQL, SQL Server, MariaDB.
  - [ ] SQLite backup (`cp /app/data/event.db`) and restore procedures are documented.
  - [ ] Single-replica constraint for SQLite is explicit.
- **Dependencies:** 6.1, 6.2.
- **Effort:** M
- **Required Skills/Rules:** `docs/DOCUMENTATION_STYLE_GUIDE.md`.

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS

- `dotnet build --configuration Release --verbosity quiet`
- Manual: `docker build -t islamu/event-standalone -f src/Event.Standalone/Dockerfile .` completes successfully.

## 7. Testing Strategy

- **Phase 1:** `Event.API.IntegrationTests` owns behavior-preserving API host extraction.
- **Phase 2:** `Explore.Blazor.IntegrationTests` owns Split/Combined profile and BFF/UI host extraction; architecture assertions are folded into Task 2.2 and run in eventual full PR verification.
- **Phase 3:** new `Event.Standalone.IntegrationTests` owns combined-host endpoint/auth/antiforgery/static-asset behavior.
- **Phase 4:** `Event.Architecture.Tests` owns final solution membership, composition-root permissions, and mutually exclusive topology configuration.
- **Phase 5:** `Event.Standalone.IntegrationTests` extends with SQLite default, provider override, and fail-fast configuration tests.
- **Phase 6:** Manual Docker build verification; no new test project.
- Test approach is tests-after extraction for behavior-preserving modules and focused regression-first tests for the security-sensitive browser bridge where practical.

## 8. Documentation, Configuration, And Operations Impact

- Add `Event.Standalone` to `Explore.slnx` and reserve launch ports 5180/7180.
- Add AppHost `Hosting:Topology` with environment form `Hosting__Topology`; default `Split`.
- Standalone consumes the union of existing API/BFF settings. Do not create renamed duplicate settings unless a collision is proven during Task 2.1.
- Update the eight exact docs in Task 4.2. No generated API client/OpenAPI contract update is expected because controllers and routes do not change.
- Add `src/Event.Standalone/Dockerfile` and `docker-compose.standalone.yml` in Phase 6.
- Document `docker run -v data:/app/data -p 8080:8080 islamu/event-standalone` as the minimal self-hosting deployment.
- Document SQLite default behavior, provider override via `DATABASE_PROVIDER` environment variable or Infisical secret, volume mount requirements, and single-replica constraint.
- Document direct `dotnet run --project src/Event.Standalone/Event.Standalone.csproj` and Aspire opt-in as alternative development paths.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- **Trust boundary:** Applicable. BFF cookies and server-held tokens remain distinct from external bearer/API-key credentials.
- **Authentication/authorization:** Applicable. Local token injection must still invoke existing API bearer validation and policies; cookie claims alone do not replace API authorization.
- **Antiforgery:** Applicable. Every unsafe cookie-backed `/api/*` request must satisfy the existing XSRF cookie/header contract before token injection.
- **Tenant isolation:** Applicable. Strip inbound tenant headers from browser-classified traffic and apply only authoritative BFF tenant state; external API tenant resolution remains unchanged.
- **Privileged headers:** Applicable. Setup secret and support access are stripped and reconstructed through one shared enrichment service used by YARP and Standalone.
- **Rate limiting/idempotency:** Applicable. Requests continue through existing API rate limiting and idempotency; BFF endpoint limits remain scoped to BFF routes.
- **Audit/privacy:** Applicable. Existing support audit and privacy-erasure startup gate run once. Tokens/secrets must never enter logs, traces, metrics, or error bodies.
- **HAL:** Applicable but unchanged. API policies/assemblers continue to produce UI affordances.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Reason |
|---|---|---|
| Multi-tenancy | Applicable | Same host must preserve trusted tenant header/host resolution for browser and external clients. |
| Federation/AT Protocol | Applicable, unchanged | Blazor OAuth/federation endpoints remain mapped; no protocol behavior changes. |
| Localization | Applicable, unchanged | Both localization registrations/middleware must compose once with existing UI/API behavior. |
| Accessibility | Not directly applicable | No UI markup or interaction changes; Razor assets/components must remain reachable. |
| Product behavior | Applicable, topology-only | Users should observe no feature difference between split and standalone profiles. |

## 11. Observability And Operations

- Use one Serilog/telemetry/service-default registration and preserve correlation across BFF bridge and API processing.
- Add a bounded host-topology attribute/log at startup; never log tokens, cookies, setup secrets, support tokens, or privileged header values.
- Preserve liveness/readiness and graceful-shutdown semantics. Combined readiness reports local API/startup readiness rather than polling itself through HTTP.
- Ensure worker/startup failures remain operator-visible and fail startup consistently with the API host.
- Troubleshooting must distinguish auth-classification, antiforgery, static-asset discovery, startup gate, and topology configuration failures.

## 12. Migration And Compatibility Plan

- **Database/schema/data:** Event.Standalone defaults to SQLite with `/app/data/event.db`. Operators may override to PostgreSQL or other providers. Provider selection and migration assembly routing follow the `multi-database-support` workstream contract. SQLite migrations are generated separately from PostgreSQL migrations.
- **Startup migration:** Existing API migration/seeding orchestration is reused once by Standalone; `Event.MigrationService` ordering remains an Aspire prerequisite.
- **API compatibility:** Routes, version negotiation, controllers, HAL, ProblemDetails, and generated clients remain unchanged.
- **Deployment compatibility:** Split remains default and is the immediate rollback. Standalone is additive and opt-in.
- **Configuration compatibility:** Existing API/BFF keys are consumed as-is. Only `Hosting:Topology` and standalone launch ports are new.
- **Rollback:** Select `Hosting__Topology=Split` or use the existing API and Blazor startup projects; no data rollback is required.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Middleware order weakens auth/tenant/antiforgery behavior | Medium | Critical | Explicit branches, shared enrichment, focused integration tests | Browser request reaches controller without expected auth/XSRF/tenant | 3.2, 3.3, 3.4 |
| Cookie and API schemes select the wrong principal | Medium | Critical | Explicit bridge authentication followed by existing API bearer validation | External client challenged by OIDC or browser skips API policies | 3.2, 3.4 |
| Hosted services/startup gates run twice | Medium | High | Single Standalone owner and registration-count tests | Duplicate jobs, migrations, callbacks, or startup logs | 3.1, 3.3, 3.4 |
| Static assets/Razor roots are not discoverable transitively | Medium | High | Explicit application/additional assemblies and integration assertions | UI 404, missing asset, absent hub/component endpoint | 3.3, 3.4 |
| Extraction changes split-host behavior | Medium | High | Thin callers and existing host integration coverage | Existing API/Blazor integration regression | 1.1, 2.1 |
| AppHost registers mixed topology or wrong callbacks | Low | High | Fail-fast enum/config and static topology assertions | Both split and standalone resources or callback mismatch | 4.1, 4.3 |
| Current unrelated build failures block phase gates | High until fixed | High | Record baseline; require owning workstream to restore green before implementation | Same 13 baseline errors remain | Implementation prerequisite |
| Scope expands into persistence or packaging | Medium | Medium | Explicit non-goals and separate follow-up approval | Migration/provider/Docker files appear in diff | All tasks |
| SQLite default creates data on unexpected path without volume | Medium | High | Fail-fast if `/app/data` is not writable or not a mount; startup warning in logs | Data loss after container restart without volume | 5.1, 6.1 |
| Provider override misconfiguration silently uses wrong provider | Low | High | Structured validation with fail-fast and credential-safe diagnostics | Startup error or wrong database engine | 5.1, 5.2 |
| Docker image missing SQLite native binaries on target architecture | Low | High | Multi-arch build verification; explicit native-library inclusion | `DllNotFoundException` at runtime | 6.1 |

## 14. Success Metrics And Definition Of Done

- `Event.Standalone` is selectable as a startup project and as an opt-in Aspire topology with one web resource/port.
- Existing split API/Blazor behavior remains the default and passes its owning regression coverage.
- The combined endpoint graph contains existing API controllers, BFF/auth endpoints, Razor Components, static assets, and SignalR.
- Browser API calls use server-held tokens, trusted header reconstruction, and antiforgery; external/mobile bearer/API-key calls remain unchanged.
- API startup gates, workers, health, observability, and shutdown register/execute once.
- No UI-to-MediatR/lower-layer dependency, loopback proxy, duplicate API route, migration/provider change, or `/api/v1` contract is introduced.
- Every phase's single Release build and selected project test pass after the unrelated baseline failures are resolved.
- Plan/context/tasks and the exact architecture/operations docs are synchronized with implementation reality.
- `docker run -v data:/app/data -p 8080:8080 islamu/event-standalone` starts successfully with SQLite default, serves UI and API on one port, and persists data across restarts.
- Operators can override to PostgreSQL via `DATABASE_PROVIDER=PostgreSQL` plus structured connection fields.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. At first implementation start, read all three workstream files once. On cold resume, read context/tasks first and only the current plan sections/decisions.
2. Start with the highest-priority unchecked task unless the user overrides it.
3. Use `event-standalone-combined-host-tasks.md` as the hot ledger; check substantial work immediately and reconcile small work by phase end.
4. Keep implementation-task and phase-verification checkboxes separate. A phase is complete only after its one build and one selected test pass.
5. Update task status/count/priority/next slice/date whenever task state changes.
6. Update context only after a phase, meaningful decision, blocker, failed validation, material discovery, or handoff.
7. Update this plan only when scope, architecture, phase order, acceptance, risk, or validation strategy changes.
8. Record failed validation and recovery without marking work complete. Never absorb unrelated baseline fixes into this workstream without approval.
9. Before pause/compaction/transfer/PR, reconcile tasks and add a dated context handoff, naming unrelated dirty files to avoid.
10. Run phase verification only after all phase tasks. Do not repeat successful commands or start browser/Aspire/Docker/live services as a plan phase gate.
11. Never report completion when repository reality and the ledger disagree.
12. Every implementation summary must teach the architecture, important files/types, request/control flow, security/reliability conventions, exact verification, remaining work, and dev-doc status.

## 16. Progress Reporting Contract

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: tasks yes/no; context/plan updated or unchanged with reason
```

## 17. Potential Risks & Unknowns

The most failure-prone slice is Task 3.3: ASP.NET Core middleware that is harmless in separate hosts can become globally active and conflict when combined. The implementation must route-group or branch API- and BFF-specific behavior explicitly instead of concatenating both programs. The static-web-asset behavior of referenced Web SDK projects also remains an evidence gap until the new WebApplicationFactory coverage executes. If the current baseline build is still red, implementation must stop at the first phase gate and wait for the owning fixes rather than broadening this plan. The SQLite integration depends on the `multi-database-support` workstream Phase 1 (structured `DatabaseOptions` contract) and Phase 4 (SQLite provider registration). If those phases are not landed, Phase 5 of this workstream must co-develop the minimal SQLite provider registration independently. The privacy-erasure authority SQLite file (`privacy_erasure_authority.db`) must remain separate from the primary application SQLite file (`event.db`) to preserve independent restore lifecycles.
