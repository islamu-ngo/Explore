<!-- ABOUTME: Implementation plan for hardening the Blazor BFF boundary and refactoring Blazor client maintainability hotspots. -->
<!-- ABOUTME: Coordinates security, architecture, UI, accessibility, and verification work for the Blazor server/client surface only. -->

# Blazor BFF Hardening Refactor Plan

Last Updated: 2026-05-07 Europe/Brussels

## 0. Purpose And Current Status

Create an enterprise-grade implementation path for hardening `Explore.Blazor` and refactoring `Explore.Blazor.Client` after the Blazor/BFF audit. This plan is documentation-only; no implementation has started. Backward compatibility is not a constraint because the repository is in development mode, so implementation agents should prefer clean, secure, maintainable designs over preserving current accidental contracts.

## 1. Scope

### In Scope

- `Explore.Blazor/**/*.cs` server/BFF endpoints, auth/session services, YARP transforms, antiforgery, storage proxy, diagnostics, and BFF-side docs.
- `Explore.Blazor.Client/**/*.cs`, `*.razor`, and `*.razor.css` service wrappers, HTTP/result handling, component decomposition, dialog workflows, HAL affordance consumption, render-mode alignment, design-system usage, and accessibility fixes.
- Tests in `Explore.Blazor.IntegrationTests`, `Explore.Blazor.Client.Tests`, `Explore.Blazor.Client.E2ETests`, and relevant architecture tests.
- Documentation updates in `docs/BLAZOR.md`, `docs/SECURITY-MODEL.md`, `docs/DESIGN_SYSTEM.md`, and `docs/ACCESSIBILITY.md` when implementation changes their stated contracts.

### Out Of Scope Unless Required By HAL Contract Work

- API controller, HATEOAS policy, application, persistence, migration, or domain refactors. If a Blazor UI action cannot become HAL-driven because the API does not expose the needed `_links`, create a small follow-up slice under the `add-hal-link` intent rather than expanding this refactor indiscriminately.

## 2. Intent Classification

- Primary intent: `bff-auth-bug` for BFF auth/session/proxy hardening under `.claude/rules/blazor-server.md`.
- Secondary intent: `blazor-component-affordance` for client HAL affordance, render mode, MudBlazor, and accessibility work under `.claude/rules/blazor-client.md`.
- Conditional intent: `add-hal-link` only if API HAL policies must expose missing action affordances; then load `.claude/rules/api-hateoas.md`, keep policies fail-closed, and use yield-return link emission.
- Fallback: broad maintainability/security refactor governed by `CLAUDE.md`, `docs/GOVERNANCE.md`, `docs/QUICK_REFERENCE.md`, and loaded skills.

## 3. Canonical Rules And Skills

- Skills: `agentic-research`, `clean-architecture-rules`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, `design-system`, `blazor-css-isolation`, `accessibility`.
- BFF invariants: browser never receives raw bearer tokens; BFF stores auth state in HttpOnly cookies and forwards Bearer server-side; privileged forwarding headers are BFF-controlled; state-changing cookie-auth endpoints validate antiforgery; outbound pooled server clients keep `UseCookies=false`.
- Client invariants: default render mode is InteractiveAuto unless server-only execution is intentionally documented; no `HttpContext` assumptions in `.Client`; MudBlazor v9 async APIs; HAL `_links` are the UI action source of truth; wrappers/tokens over ad hoc styling; WCAG 2.2 AA semantics and keyboard/focus behavior.
- Architecture invariants: API/Blazor are composition roots; do not push BFF/UI concerns inward; new C# files use file-scoped namespaces and two `ABOUTME` comments; tests must prove behavior before and after refactors.

## 3A. Execution Principle

This workstream is an umbrella. Implementation must proceed in small, independently verified slices. A slice changes one security or architecture boundary at a time, includes tests proving the old unsafe behavior is blocked, updates context/tasks before moving on, and remains independently rollbackable. Do not batch multiple Phase 1 security domains into one implementation PR or agent session.

Recommended implementation streams:

- Stream A — BFF security hardening.
- Stream B — BFF endpoint decomposition.
- Stream C — client HTTP/result pipeline.
- Stream D — image storage service split.
- Stream E — HAL affordance migration.
- Stream F — UI component decomposition.
- Stream G — accessibility/dialog/CSS cleanup.

## 3B. Compact Threat Model

### Assets

- Setup secret and setup-session state.
- Access/refresh tokens and token metadata.
- Authenticated BFF session cookie.
- Antiforgery request token.
- Presigned upload destination or upload-session descriptor.
- Serialized Blazor auth-state claims.

### Attackers

- Unauthenticated browser user.
- Authenticated low-privilege user.
- Malicious tenant user.
- Compromised frontend JavaScript dependency.
- Misconfigured reverse proxy or self-hosting environment.
- Log reader with partial operational access.

### Primary Threats

- Browser-controlled privileged header injection.
- CSRF against cookie-auth BFF endpoints.
- Token/session leakage through diagnostics or over-serialization.
- Arbitrary presigned-looking upload URL proxying.
- Overexposed client claims used as authority.
- Cross-user or stale circuit token bleed.
- Development/bootstrap convenience accidentally becoming production fallback.

## 4. Current-State Implementation Report

### 4.1 BFF Security Boundary

- `Explore.Blazor/Extensions/YarpProxyExtensions.cs:197-233` says it strips incoming `X-Setup-Secret`, but then reads `httpContext.Request.Headers["X-Setup-Secret"]` at line 208 and forwards it at lines 230-233. This violates the BFF rule that privileged proxy headers must be replaced from trusted BFF-controlled state, not re-trusted from the browser.
- `Explore.Blazor/Services/DynamicAuthSchemeManager.cs:306-311`, `536-544`, and `577-610` log or redirect secret-derived diagnostics (`secretLength`, `secretPrefix`, `clientId`, detailed error text) and write to `Console.Error`. This needs a safe diagnostics policy with opaque browser errors and correlation IDs.
- `Explore.Blazor/Program.cs:61-64` enables `.AddAuthenticationStateSerialization(options => options.SerializeAllClaims = true)`, exposing more claims than the client needs. The client should receive minimal display-safe claims while authority stays in HAL/BFF/API checks.
- `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs:16-64` maps mutating `/bff/theme`, `/bff/language`, `/bff/direction`, and appearance profile endpoints without `.ValidateAntiforgery()`. `Explore.Blazor/Extensions/BffStorageEndpoints.cs:15-18` shows the intended pattern: `.RequireAuthorization().ValidateAntiforgery()` for unsafe BFF operations.
- `Explore.Blazor/Extensions/BffStorageEndpoints.cs:58-75` validates only HTTPS plus `X-Amz-Algorithm`/`X-Amz-Signature` query markers for upload URLs. Authenticated users can still submit arbitrary HTTPS URLs shaped like a presigned S3 URL unless host/bucket/path/session binding is added.
- `Explore.Blazor/Services/CircuitAccessTokenService.cs:90-163` stores tokens in a process-wide static dictionary keyed by user id and logs token length/user id metadata. This is not browser token storage, but it is a lifecycle/isolation/testability risk.

### 4.2 BFF Endpoint And Diagnostics Structure

- `Explore.Blazor/Extensions/BffAuthEndpoints.cs` combines routing, challenge, provider readiness, OIDC metadata checks, session refresh, token assessment, debug endpoints, safe return URL validation, and direct response writing. It also returns `token = tokenAssessment.Reason` at line 350, which is semantically misleading even though it is not the raw token.
- `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs` repeats authenticated checks, `IHttpClientFactory.CreateClient("BffClient")`, proxy calls, cookie persistence, and default fallbacks. Theme validation differs between resolved appearance and cookie reads, which can drop high-contrast/custom modes.

### 4.3 Client HTTP/Result Handling

- `Explore.Blazor.Client/Extensions/HttpResponseExtensions.cs:10-13` already declares the repo standard: all API-calling code should use its status-code-first helpers instead of raw `ReadFromJsonAsync` or `GetFromJsonAsync`.
- Current service grep shows raw HTTP/result handling remains concentrated in `AppearanceThemeService.cs`, `FooterAdminService.cs`, `InstanceOnboardingService.cs`, `ImageStorageService.cs`, `TenantNavigationService.cs`, `TenantOnboardingService.cs`, `PublicExperienceService.cs`, template sync services, and others.
- `Explore.Blazor.Client/Services/ImageStorageService.cs:27-88` exposes one broad interface for file reading, upload URL creation, upload execution, record creation, presigned download URL retrieval, delete, and previews. Lines 600-760 mix multi-step orchestration, generated client calls, raw `CreateClient("BffClient")`, `ReadFromJsonAsync`, and exception-to-message mapping.

### 4.4 UI Component Size And Duplication

- `Explore.Blazor.Client/Pages/Events/EventList.razor.cs:27-44` injects many unrelated services and line range `1668-1818` embeds inline registration auth prompts, focus management, consent checks, DTO creation, API calls, snackbar handling, and exception text display inside the page.
- `Explore.Blazor.Client/Pages/Events/Sessions/CreateSession.razor:1-130` and `EditSession.razor:1-130` are near-duplicate program-item forms with matching layout, loading/error/success shells, fields, and save structure.
- `Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantLookupTablesSection.razor:415-667` repeats category/tag/location create/edit/delete dialog, snackbar, and reload flows with manual `new DialogOptions` and repeated `ShowMessageBoxAsync` calls.

### 4.5 Authorization And HAL Drift

- `Explore.Blazor.Client/Pages/Organizations/OrganizationMembers.razor:22`, `62`, and `87` gate actions via `RoleHelper.CanManage(currentUserRole)`, while project rules require HAL `_links` as the action affordance source. Route guards may remain coarse navigation protection, but button/action visibility must come from server-provided affordances.

### 4.6 Render Mode, Dialog, Accessibility, And CSS Drift

- A repo search found 36 `.razor` files under `Explore.Blazor.Client` with explicit `@rendermode InteractiveServer`. This may be legitimate for some flows, but each deviation from the default InteractiveAuto needs an explicit reason or removal.
- `Explore.Blazor.Client/Services/DialogOptionsFactory.cs:1-60` defines required presets, but `TenantLookupTablesSection.razor:417`, `448`, `497`, `535`, `584`, and `622` still manually constructs `DialogOptions`.
- `Explore.Blazor.Client/Pages/Admin/Components/ApiKeysSection.razor:108-115` and `UiThemeCatalogSection.razor:85-93` contain icon-only `MudIconButton` controls without action-specific `aria-label` values. `OrganizationMembers.razor:64` and `89` use physical `text-align:right`.
- `Explore.Blazor.Client/Pages/Events/EventList.razor.css:67-163` uses `::deep .event-grid.mud-grid` and many `!important` rules. It also uses positive patterns such as container queries and logical properties, so the target is to reduce brittle MudBlazor overrides, not to discard the whole file.

## 5. External And Official Research Summary

- Context7/Microsoft Blazor docs: Blazor Web Apps mix static SSR, interactive SSR, CSR, and InteractiveAuto; prerendered interactive components can execute before event handlers attach and later become interactive. `HttpContext` assumptions are unsafe outside static SSR/root request scenarios.
- Context7/ASP.NET Core docs: antiforgery tokens are specifically required for unsafe cookie-auth AJAX/form interactions; client-side code can read an antiforgery request token from a non-HttpOnly token cookie and send it in a header.
- Context7/YARP docs: authentication and authorization middleware should run before `MapReverseProxy`; proxy transforms must set downstream credentials deliberately.
- Context7/MudBlazor docs: dialog workflows should use `IDialogService`, `MudDialogProvider`, custom dialog components, `DialogOptions`, and the v9 async APIs (`ShowAsync`, `ShowMessageBoxAsync`).
- Tavily/IETF OAuth browser-based apps draft: BFF cookies must be `Secure` and `HttpOnly`, should use `SameSite=Strict`, path `/`, no `Domain`, and `__Host` prefix where possible; BFF endpoints must implement CSRF defenses because browser-to-BFF calls rely on cookies.
- Tavily BFF security sources: the central security advantage of BFF is keeping access/refresh tokens off the browser and attaching tokens only in the server/BFF-to-resource-server hop.

## 6. Target Architecture

### 6.1 BFF Security Services

- `ISetupSecretResolver`: returns a trusted setup secret only from documented BFF-controlled sources. Never reads inbound request headers. Required trusted source order:
  1. BFF-owned setup handshake/session state. This does not require normal application-user authentication before first-run setup; it means server-side state established by the setup handshake and controlled by the BFF.
  2. Protected setup cookie issued by the BFF during the setup handshake. The cookie must be Data Protection-backed or equivalent, `Secure` outside local development, `HttpOnly`, scoped to setup behavior, short-lived, invalidated after setup completion, and not treated as trusted merely because it is browser-sent.
  3. Secure server configuration fallback, only for explicitly gated local/development/bootstrap mode.
  4. No inbound request header source, ever.
- `SetupSecretResolutionResult`: resolver output should be a structured result, not a nullable string. It should include `Found`, `SetupSecretSource`, `Secret`, and safe `FailureCode`/diagnostic metadata so tests and logs can assert source behavior without leaking secret values.
- `IAccessTokenResolver`: abstracts current request/circuit token resolution with deterministic scope and cleanup; avoids process-wide token dictionaries unless explicitly bounded and encrypted/protected.
- `ISafeDiagnosticsPolicy`: maps internal exceptions to structured logs with correlation IDs and safe browser-facing problem codes.
- `IStorageUploadPolicy`: validates upload destinations before the BFF performs a server-side upload. Preferred enterprise design is upload-session binding: client requests an upload intent, BFF/API issues an upload session or signed descriptor, and the BFF validates session id, user, tenant, expected host, bucket, key prefix, content type, size, and expiry before upload. A strict configured allowlist is acceptable only if session binding is not yet available, and must include allowed host, bucket, key prefix, tenant/user/session path, content type, max size, and expiry window.

### 6.2 BFF Endpoint Modules

- Keep endpoint mapping thin. Move behavior into services: `AuthChallengeService`, `AuthProviderReadinessService`, `SessionRefreshService`, `SafeReturnUrlValidator`, `TokenAssessmentService`, `PreferenceBffService`, `PreferenceCookieWriter`, and `BffForwardingResults`.
- Centralize BFF-to-API proxy result translation so endpoint handlers do not repeat raw `HttpClient` forwarding logic.

### 6.3 Client Service Layer

- Introduce `ApiClientExecutor`/`HttpCommandClient` around `HttpResponseExtensions` and generated NSwag clients.
- Return explicit `OperationResult<T>`/`ApiResult<T>` shapes for UI flows that need user-visible errors; do not conflate empty data with failed requests.
- Split `ImageStorageService` into `ImageFileReader`, `ImageUploadClient`, `ImageStorageRecordClient`, `ImagePreviewService`, and `ContentTypeClassifier`.

### 6.4 UI Orchestration And Components

- Split page code-behind orchestration into small state/controller classes: `EventListFilterState`, `EventListRegistrationWorkflow`, `EventListDockingController`, `EventListSelectionController`, and `DialogWorkflowService`.
- Extract shared components/models: `EventSessionForm`, `EventSessionFormModelMapper`, `EventSessionSaveCoordinator`, `LookupTableCrudSection<T>`, `EventFilterFields`, `ProgramSectionForm`, and `ProgramSectionList`.
- Enforce `DialogOptionsFactory` and wrapper components (`AppButton`, `AppIconButton`, `AppDialogShell`, `AppTextField`, `AppCard`) where project standards require them.

## 7. Implementation Phases

### Phase 1A — Setup-Secret Trust Boundary

1. Add a failing characterization/integration test proving browser-supplied `X-Setup-Secret` is currently forwarded or would be forwarded by the old path.
2. Define the trusted setup-secret source order and `SetupSecretResolutionResult` contract before writing production implementation code.
3. Introduce `ISetupSecretResolver` in `Explore.Blazor` and update `YarpProxyExtensions.ForwardSetupSecretAsync` to use resolver output only.
4. Add negative and positive tests: inbound fake header ignored, no resolver secret means no downstream setup header, trusted resolver secret forwarded, inbound fake plus trusted resolver secret forwards only trusted value, and logs do not include secret value/prefix/length.
5. Update `docs/BLAZOR.md`, `docs/SECURITY-MODEL.md`, `docs/TROUBLESHOOTING.md`, and any setup-secret references that still describe request-header trust.

### Phase 1B — Safe Auth/OIDC Diagnostics

1. Introduce `ISafeDiagnosticsPolicy`.
2. Remove secret prefix/length/client-secret diagnostics, raw `Console.Error` auth diagnostics, and browser `errorDetail` leakage.
3. Use opaque error codes and correlation IDs for browser-visible failures.
4. Add tests proving OIDC failure redirects contain only safe code/correlation ID, logs contain correlation ID, logs do not contain client-secret metadata, and production auth paths no longer write to `Console.Error`.

### Phase 1C — BFF CSRF/Antiforgery Contract

1. Define the BFF antiforgery contract before adding validation broadly: token issuance endpoint or middleware, readable request-token cookie name, expected header name, client handler behavior, SSR/WASM/pre-render behavior, and bootstrap exceptions.
2. Inventory unsafe BFF mutations before editing endpoints, including preference/appearance/storage/auth/setup endpoints, and record which endpoints are modified in this slice versus deferred.
3. Confirm whether `BffClient`, generated clients, and preference endpoints send the token consistently from SSR and WASM paths.
4. Add `.ValidateAntiforgery()` or equivalent validation only to the identified unsafe BFF mutations selected for this slice after the client/server contract is proven; do not fold unrelated auth refactors into Phase 1C.
5. Add tests for missing, invalid, and valid token behavior; safe GET endpoints should remain unaffected unless intentionally configured otherwise.

### Phase 1D — Client Auth-State Claim Minimization

1. Inventory current Blazor client auth-state consumers before removing `SerializeAllClaims = true`: display name, email, user id, roles, tenant id, provider, setup/onboarding claims, culture/language/direction, feature flags, and any other claim reads.
2. Define the serialized claim allow-list. Policy: serialized auth state contains only display-safe identity hints; authorization, tenancy authority, feature access, and action affordances come from BFF/API/HAL.
3. Remove all-claim serialization only after tests prove the minimal claim contract supports current display needs.
4. Add tests proving role/permission claims are not used for UI action authority.

### Phase 1E — Storage Upload Destination Binding

1. Choose the upload binding strategy before changing proxy behavior. Preferred: server-issued upload session IDs or signed upload descriptors. Acceptable fallback: strict configured allowlist if session binding is not yet available.
2. For upload-session binding, validate uploadSessionId, user, tenant, expected host, bucket, key prefix, content type, max size, and expiry.
3. Reject arbitrary presigned-looking URLs even when they use HTTPS and valid-looking `X-Amz-*` query parameters.
4. Add negative tests for untrusted host, wrong bucket, wrong tenant/user/session prefix, expired session, content type mismatch, and a positive valid upload-session path.

### Phase 1F — Circuit Token Lifecycle Hardening

1. Complete Oracle/design review before changing `CircuitAccessTokenService` or token resolver architecture.
2. Decide whether the token bridge is per circuit, per browser tab, per user session, or per authentication session; document logout, refresh, expiry, multiple tabs, multi-instance deployment, sticky sessions, restarts, and Data Protection assumptions.
3. Replace or strictly bound the static process-wide token dictionary only after the design is approved.
4. Add tests for logout clearing, expired token rejection, cross-user isolation, multiple tab/circuit behavior, refresh behavior, and documented multi-instance behavior.

### Phase 2 — BFF Endpoint Decomposition

1. Split `BffAuthEndpoints.cs` into thin mapper plus services.
2. Split `BffPreferenceEndpoints.cs` into forwarding, cookie, validation, and current-user concerns.
3. Add integration/unit tests for each extracted seam. Phase 1 behavior tests must remain in the same security slice as the behavior change; do not defer hardening tests to decomposition-only work.

### Phase 3 — Client HTTP And Result Pipeline

1. Build `ApiClientExecutor`/`HttpCommandClient` on top of `HttpResponseExtensions`.
2. Migrate high-churn services: `ImageStorageService`, `FooterAdminService`, `TenantNavigationService`, `AppearanceThemeService`, and onboarding/public-experience services.
3. Split image storage responsibilities and add unit tests for each seam.

### Phase 4 — UI Decomposition And HAL Affordances

1. Extract `EventList` workflows and remove exception-detail snackbar leakage.
2. Replace Create/Edit session duplication with shared form/model/save coordinator.
3. Replace lookup CRUD duplication with generic section/workflow helpers.
4. Move organization/group/admin action gating to HAL/action view models; create conditional API HAL tasks only where links are missing.
5. For every missing HAL affordance, record route/resource/action, current UI gate, expected `_links` relation, and the minimal API policy file required before expanding scope.
6. Do not synthesize missing HAL links client-side from roles, route names, guessed permissions, or cached claim state. If a link exists, show the action; if it is missing, hide the action; if it should exist but does not, record the missing affordance and create the smallest conditional API HAL task.

### Phase 5 — Render Mode, Dialog, Accessibility, CSS

1. Audit all 36 explicit `@rendermode InteractiveServer` pages and either document server-only reasons or return to default InteractiveAuto.
2. Replace manual `DialogOptions` and raw confirmation flows with `DialogWorkflowService` and `DialogOptionsFactory` presets.
3. Add missing `aria-label`s, live announcements, focus restoration, and physical-to-logical CSS conversions.
4. Reduce broad `::deep .mud-*`/`!important` overrides through wrapper components and approved global override layer.

## 8. File-Level Change Map

- BFF: `Explore.Blazor/Extensions/YarpProxyExtensions.cs`, `BffAuthEndpoints.cs`, `BffPreferenceEndpoints.cs`, `BffStorageEndpoints.cs`, `Explore.Blazor/Services/DynamicAuthSchemeManager.cs`, `CircuitAccessTokenService.cs`, forwarding handlers/resolvers under `Explore.Blazor/Services`.
- Client services: `Explore.Blazor.Client/Extensions/HttpResponseExtensions.cs`, `Services/ImageStorageService.cs`, `FooterAdminService.cs`, `TenantNavigationService.cs`, `AppearanceThemeService.cs`, onboarding and public-experience services.
- UI: `Pages/Events/EventList.razor(.cs/.css)`, `Pages/Events/Sessions/CreateSession.razor`, `EditSession.razor`, `Pages/Admin/Tenant/Components/TenantLookupTablesSection.razor`, organization/group member pages, admin component dialogs, `Services/DialogOptionsFactory.cs` consumers.
- Docs: `docs/BLAZOR.md`, `docs/SECURITY-MODEL.md`, `docs/DESIGN_SYSTEM.md`, `docs/ACCESSIBILITY.md` when behavior changes.

## 9. Test Strategy

- Every implementation slice must record changed files, tests run, `lsp_diagnostics` result for modified files, docs updated or `not applicable`, and residual risks/deferrals in `blazor-bff-hardening-refactor-context.md`.
- BFF integration: `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- Client unit/component: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.
- Critical E2E after UI slices: `dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet` or targeted Playwright flows.
- Architecture/docs: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` and `/docs-lint` where docs links change.
- Build: `dotnet build --configuration Release --verbosity quiet` after each phase.
- Negative security tests are mandatory for Phase 1A-1F; each slice must prove the old unsafe behavior is blocked before the task is marked complete.

## 10. Acceptance Criteria

- Browser-supplied `X-Setup-Secret` is never forwarded downstream; tests prove only trusted BFF-controlled sources are used.
- OIDC/auth errors expose only safe user-facing codes plus correlation IDs; no secret prefix/length/client ID appears in browser redirects or high-level logs.
- Client auth state contains only minimal display-safe claims; UI action authority comes from HAL links or BFF/API-backed checks.
- Every unsafe `/bff/*` preference/storage/auth mutation either validates antiforgery or has a documented, tested bootstrap exception.
- Storage upload proxy only accepts server-approved upload destinations.
- Client services use central result handling; no new raw `ReadFromJsonAsync`/`GetFromJsonAsync` or `EnsureSuccessStatusCode` patterns outside low-level executors.
- Allowed low-level HTTP/deserialization exceptions are explicitly documented before enforcement tests are added.
- Refactored UI components have smaller, testable workflows and preserve visible behavior while improving separation of concerns.
- Accessibility fixes satisfy PageTitle/h1, icon labels, live status/error announcement, focus restoration, target size, and logical CSS rules.
- Phase 1A-1F each land as independently verified, rollbackable slices; no implementation slice mixes multiple Phase 1 security domains.

## 11. Non-Goals And Guardrails

- Do not move BFF/UI logic into Domain/Application/Persistence.
- Do not suppress type errors with `as any`, `@ts-ignore`, or equivalent shortcuts.
- Do not delete failing tests to pass verification.
- Do not introduce token localStorage/sessionStorage.
- Do not preserve current insecure diagnostics or setup-secret header behavior for backward compatibility.
- Do not globally override bare `.mud-*` outside `Explore.Blazor/wwwroot/css/mudblazor-overrides.css`.
- Do not synthesize client-side HAL affordances from roles, route names, guessed permissions, or serialized claims.
- Do not add regex-only raw HTTP bans without documented low-level exceptions for central executors, upload/streaming clients, generated-client adapters, test fakes, and intentional health/debug clients.

## 12. Risks And Unknowns

- Claim minimization may reveal UI assumptions on all-claim serialization; use tests to define the minimal claim contract.
- Setup-secret docs currently mention request header fallback; implementation and docs must change together to avoid future regression.
- Storage upload host/bucket validation needs configuration source confirmation; if unavailable, introduce a server-side upload-session token rather than trusting client-provided URL shape.
- HAL migration may require API link policy work. Keep that work separate and minimal.
- Render-mode changes can alter prerender and lifecycle timing; validate with focused component tests and critical E2E flows.
- Removing request-header setup-secret trust can break legitimate first-run development/setup flows if the replacement source order is not implemented first. Production must have no request-header trust; development may use explicit local-only setup sources gated by environment; tests should use resolver doubles.
- Cookie hardening is not one-size-fits-all. Main BFF session, OIDC correlation, nonce, antiforgery, and setup cookies can require different SameSite/Secure/HttpOnly settings; document each instead of applying a blanket cookie rule.
- Generic UI abstractions can become abstraction theater. Extract shared workflow mechanics, not business meaning, especially for lookup CRUD flows with divergent validation, HAL links, reload behavior, or dialog copy.

## 13. Migration And Sequencing Notes

- Because backward compatibility is intentionally not required, rename misleading contracts such as `token = tokenAssessment.Reason` to explicit status fields immediately.
- Prefer adding narrow services and tests first, then deleting old logic in the same slice.
- Keep each pull-request-sized implementation slice independently buildable and testable.
- Update `dev/active/blazor-bff-hardening-refactor/blazor-bff-hardening-refactor-context.md` after every implementation session.
- Maintain durable audit tables in the context file for render-mode decisions and missing HAL affordances.

## 14. Verification Gates Per Phase

- Phase 1A-1F: BFF integration/unit tests specific to the slice + architecture tests + build.
- Phase 2: BFF integration tests with endpoint behavior coverage + build.
- Phase 3: client service unit tests + build.
- Phase 4: client component tests + targeted E2E flows + build.
- Phase 5: accessibility/component tests + docs lint + build.

## 15. Documentation Updates Required

- `docs/BLAZOR.md`: remove request-header setup-secret trust language; document resolver source order and InteractiveAuto render policy.
- `docs/SECURITY-MODEL.md`: update setup-secret hardening, safe diagnostics, auth-state claim minimization, and CSRF rules for BFF endpoints.
- `docs/TROUBLESHOOTING.md`: remove setup-secret request-header fallback guidance and replace it with resolver/source-order diagnostics.
- `docs/DESIGN_SYSTEM.md`: reinforce `DialogOptionsFactory`/wrapper enforcement after migration.
- `docs/ACCESSIBILITY.md`: add any new reusable dialog/workflow accessibility patterns discovered during implementation.

## 16. Recommended First Slice

Implement Phase 1A only. Keep the slice extremely narrow:

1. Add a failing characterization/integration test proving inbound `X-Setup-Secret` is currently forwarded or would be forwarded by the legacy path.
2. Define trusted setup-secret source order and `SetupSecretResolutionResult` before production implementation code.
3. Add `ISetupSecretResolver`.
4. Make `YarpProxyExtensions.ForwardSetupSecretAsync` use only resolver output.
5. Prove inbound browser header is stripped and ignored, no secret means no forwarded setup header, trusted resolver output is forwarded, fake header plus trusted resolver output forwards only the trusted value, and logs do not reveal the secret value/prefix/length.
6. Update `docs/BLAZOR.md`, `docs/SECURITY-MODEL.md`, and `docs/TROUBLESHOOTING.md` setup-secret source-order language.
7. Run Blazor integration tests, architecture/docs tests, `lsp_diagnostics` on modified files, and build.

Do not touch OIDC diagnostics, antiforgery, storage, auth-state serialization, or circuit token storage in the same slice.

## 17. Handoff Notes

- Start every implementation session by reading this plan, the context file, the tasks file, `CLAUDE.md`, `.claude/rules/blazor-server.md`, `.claude/rules/blazor-client.md`, and relevant skills.
- Do not implement multiple phases at once unless each phase remains independently verified.
- Consult Oracle before changing token/session architecture or HAL/API boundaries.
- Maintain `Last Updated` stamps in all three workstream files.
