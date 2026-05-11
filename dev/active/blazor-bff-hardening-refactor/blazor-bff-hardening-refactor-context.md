<!-- ABOUTME: Operational context and evidence journal for the Blazor BFF hardening refactor workstream. -->
<!-- ABOUTME: Preserves current-state findings, research sources, decisions, and handoff notes for future implementation agents. -->

# Blazor BFF Hardening Refactor Context

Last Updated: 2026-05-07 Europe/Brussels

## Workstream

- Task name: `blazor-bff-hardening-refactor`
- Scope: `Explore.Blazor` and `Explore.Blazor.Client` only, with conditional API HAL work if Blazor affordance migration requires missing links.
- Status: Phase 1A, Phase 1B, Phase 1C, and Phase 1D completed; Phase 1E implementation in final verification.
- Backward compatibility: not required; prefer clean secure contracts.

## SESSION PROGRESS (2026-05-07)

### Completed

- Created the `dev/active/blazor-bff-hardening-refactor/` workstream.
- Wrote plan, context, and tasks files with current-state evidence anchors.
- Classified the work against repo intents and loaded relevant skills/rules.
- Used Context7 for Blazor, ASP.NET Core/YARP/antiforgery, and MudBlazor documentation.
- Used Tavily for BFF/token/cookie/CSRF best-practice research.
- Verified documentation checks through `Event.Architecture.Tests` after creating the docs.
- Applied CTO feedback by splitting Phase 1 into independently shippable 1A-1F security slices, adding a compact threat model, defining setup-secret trust rules, and tightening first-slice scope.
- Started Phase 1A implementation: added red/green integration coverage for client-controlled `X-Setup-Secret`, introduced `SetupSecretResolutionResult`, `ISetupSecretResolver`, and Data Protection-backed setup-cookie protection, updated YARP and server-side setup-secret forwarding to strip inbound headers and forward only resolver output.
- Completed Phase 1B implementation: added `ISafeAuthDiagnosticsPolicy`, removed secret-derived OIDC diagnostics from high-risk BFF auth paths, removed browser-visible `errorDetail` redirects, sanitized token-refresh error logging, and added integration/client tests for safe redirect behavior.
- Completed Phase 1C implementation: inventoried BFF antiforgery token issuance/client handlers/endpoint coverage, added red/green tests for `/bff/theme`, and applied `.ValidateAntiforgery()` to unsafe preference and appearance mutation endpoints.
- Completed Phase 1D implementation: replaced all-claim auth-state serialization with a display-safe serialization policy, removed browser admin-claim authority from instance/tenant route guards and nav helpers, updated docs/workstream context, and verified with integration/client/architecture/build gates plus Oracle review.
- Started Phase 1E implementation: introduced BFF-owned upload sessions so browser uploads send an opaque `uploadSessionId` instead of a raw presigned URL, added destination-binding tests, updated the BFF upload proxy/client flow, and removed raw storage response-body logging from the proxy path.

### In Progress

- Phase 1E final documentation, full verification, and Oracle review.

### Blockers

- None currently. Implementation is intentionally limited to Phase 1E.

### Required Session Evidence For Future Implementation Updates

- Changed files.
- Tests and diagnostics run, including `lsp_diagnostics` on modified files.
- Documentation updated or `not applicable`.
- Residual risks, deferrals, and decisions.

## User Intent

The user requested an implementation plan based on the Blazor UI/BFF audit and explicitly required Tavily MCP research, Context7 documentation, repository conventions, industry best practices, design patterns/principles, Clean Architecture, enterprise-grade maintainability, and no backward-compatibility constraints.

## Relevant Intents And Rules

- `bff-auth-bug`: primary for server-side BFF hardening. Rule: `.claude/rules/blazor-server.md`.
- `blazor-component-affordance`: primary for Blazor client affordance, render-mode, accessibility, wrapper, and component refactors. Rule: `.claude/rules/blazor-client.md`.
- `add-hal-link`: conditional only if API HAL links are missing. Rule: `.claude/rules/api-hateoas.md`.

## Loaded Skills

- `agentic-research`
- `clean-architecture-rules`
- `auth-patterns`
- `blazor-bff-patterns`
- `blazor-ui-conventions`
- `design-system`
- `blazor-css-isolation`
- `accessibility`

## Canonical Repo Sources Read

- `CLAUDE.md`: contribution questions, verification commands, HAL source-of-truth rule, two `ABOUTME` comments rule, manual validators, layer boundaries.
- `AGENTS.md`: delegates to `CLAUDE.md`.
- `dev/active/README.md`: required three-file workstream structure.
- `.claude/contract/intents.yaml`: relevant intents listed above.
- `.claude/rules/blazor-server.md`: BFF boundary, forwarding handler separation, `UseCookies=false`, SSR safety, endpoint modularization.
- `.claude/rules/blazor-client.md`: InteractiveAuto default, MudBlazor v9, BEM/CSS isolation, `::deep` last resort, accessibility priority.
- `.claude/rules/api-hateoas.md`: yield-return links, policy separation, fail-closed HAL capability pipeline.
- `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/ARCHITECTURE.md`, `docs/BLAZOR.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/DESIGN_SYSTEM.md`, `docs/ACCESSIBILITY.md`, `docs/OPERATIONS.md`, `docs/CODEBASE_STRUCTURE.md`, `docs/API.md`.

## Official And External Research

- Context7 `/websites/learn_microsoft_en-us_aspnet_core_blazor`: Blazor Web Apps support mixed SSR/interactive SSR/CSR/InteractiveAuto. Interactive pages can prerender before event handlers attach; InteractiveAuto code must not rely on server-only request state.
- Context7 `/dotnet/aspnetcore.docs`: antiforgery examples show generating a JavaScript-readable request token cookie and sending it back in a header for unsafe AJAX requests; YARP docs show auth/authz middleware before `MapReverseProxy`.
- Context7 `/mudblazor/mudblazor`: dialogs use `IDialogService`, `MudDialogProvider`, `DialogOptions`, custom `MudDialog` components, and v9 async APIs.
- Tavily result: IETF OAuth Browser-Based Apps draft says BFF cookies must be `Secure` and `HttpOnly`, should use `SameSite=Strict`, path `/`, no `Domain`, and `__Host` prefix where feasible; BFF endpoints must implement CSRF defenses.
- Tavily BFF sources reinforce that tokens stay server-side and browser receives only secure HttpOnly session cookies.

## Current-State Evidence Anchors

### BFF Boundary

- `Explore.Blazor/Extensions/YarpProxyExtensions.cs:197-233`: `ForwardSetupSecretAsync` removes outgoing header then reads inbound `X-Setup-Secret` request header and forwards it.
- `Explore.Blazor/Services/DynamicAuthSchemeManager.cs:306-311`, `536-544`, `577-610`: secret length/prefix diagnostics, `Console.Error`, and browser redirect `errorDetail` include sensitive operational details.
- `Explore.Blazor/Program.cs:61-64`: `SerializeAllClaims = true`.
- `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs:16-64`: mutating endpoints lack `.ValidateAntiforgery()`.
- `Explore.Blazor/Extensions/BffStorageEndpoints.cs:15-18`: positive example of auth + antiforgery on upload proxy.
- `Explore.Blazor/Extensions/BffStorageEndpoints.cs:58-75`: upload URL validation checks HTTPS plus `X-Amz-*` query markers, not trusted host/bucket/session binding.
- `Explore.Blazor/Services/CircuitAccessTokenService.cs:90-163`: static token dictionary keyed by user ID.
- `Explore.Blazor/Extensions/BffAuthEndpoints.cs:330-350`: session refresh sets token service and returns `token = tokenAssessment.Reason`; lines 383-405 describe token metadata in logs.

### Phase 1A Implementation Evidence

- `Explore.Blazor.IntegrationTests/Handlers/SetupSecretForwardingHandlerTests.cs`: added characterization tests proving a client-controlled `X-Setup-Secret` is not forwarded without a trusted source and that a trusted session secret wins over a fake client header.
- `Explore.Blazor/Services/SetupSecretResolver.cs`: added resolver contract/result/source enum plus Data Protection-backed setup-cookie protector. Resolver source order is BFF-owned setup session, protected setup cookie, explicit local/development/bootstrap config fallback, and never inbound header.
- `Explore.Blazor/Services/SetupSecretForwardingHandler.cs`: setup forwarding now removes `X-Setup-Secret` before resolving and forwards only trusted resolver output.
- `Explore.Blazor/Extensions/YarpProxyExtensions.cs`: YARP setup-secret transform now strips client-supplied headers and forwards only `ISetupSecretResolver` output.
- `Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs`: setup cookie persistence now writes protected cookie values, and persisted-secret reads go through the resolver.
- `Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs`: registers `ISetupSecretCookieProtector` and `ISetupSecretResolver`.
- Verification so far: `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` passed 44/44 after the red tests were made green.
- Residual note: `RateLimitingExtensions.cs`, `BffAuthEndpoints.cs`, and `MiddlewareExtensions.cs` still read the `setup-secret` cookie for non-forwarding rate-limit/onboarding/status checks. They are outside the Phase 1A forwarding boundary but should be reviewed later if the protected-cookie format affects those behaviors.

### Phase 1B Implementation Evidence

- `Explore.Blazor/Services/SafeAuthDiagnosticsPolicy.cs`: added normalized safe auth diagnostics with `errorCode`, `correlationId`, and failure category; redirect URLs no longer include `errorDetail`, raw exception text, client IDs, or secret-derived metadata.
- `Explore.Blazor/Services/DynamicAuthSchemeManager.cs`: OIDC registration/token/remote-failure logs now use presence booleans, safe diagnostic codes, and correlation IDs instead of client-secret prefix/length or raw provider details. Remote failures redirect through `ISafeAuthDiagnosticsPolicy`.
- `Explore.Blazor/Extensions/BffAuthEndpoints.cs`: challenge exceptions now redirect through `ISafeAuthDiagnosticsPolicy` and no longer write raw exception details or `errorDetail` to the browser.
- `Explore.Blazor/Services/TokenRefreshCookieEvents.cs`: token refresh failures now log status, sanitized error code, and body presence only; raw token endpoint bodies are not written to logs.
- `Explore.Blazor.IntegrationTests/Services/SafeAuthDiagnosticsPolicyTests.cs`: covers safe login redirect URL generation and `OpenIdConnectEvents.OnRemoteFailure` behavior.
- `Explore.Blazor.Client.Tests/Pages/Auth/AuthRedirectPagesTests.cs`: verifies the login page ignores legacy `errorDetail` query values and renders only the generic failure message/provider choices.
- `Explore.Blazor.Client.Tests/Services/EventAgendaItemServiceTests.cs`: fixed unrelated agenda-item test fixture dates exposed by the full client suite so the suite can validate Phase 1B changes cleanly.
- Verification so far: `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` passed 46/46; `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed 1065 total, 1064 succeeded, 1 skipped.
- Residual note: development-only `/auth/debug` still exposes discovery details and raw discovery errors for local troubleshooting. It is dev-gated and should remain under review, but it is not part of the production-path Phase 1B leak fixed here.

### Phase 1C Implementation Evidence

- `Explore.Blazor/Program.cs`: BFF antiforgery is configured with request header `X-CSRF-TOKEN`; MVC controllers already use auto-validation.
- `Explore.Blazor/Extensions/MiddlewareExtensions.cs`: `UseAntiforgeryTokenMiddleware` issues readable `XSRF-TOKEN` cookies on `GET` requests via `IAntiforgery.GetAndStoreTokens`.
- `Explore.Blazor/Extensions/AntiforgeryEndpointExtensions.cs`: `.ValidateAntiforgery()` returns `400` ProblemDetails titled `Antiforgery validation failed` for missing or invalid tokens.
- `Explore.Blazor.Client/Services/Http/BrowserCredentialsMessageHandler.cs`: mutating browser requests add `X-CSRF-TOKEN` from the `XSRF-TOKEN` cookie.
- `Explore.Blazor/Services/BffCookieForwardingHandler.cs`: InteractiveServer self-calls forward captured cookies and mirror `XSRF-TOKEN` into `X-CSRF-TOKEN`.
- `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs`: unsafe preference and appearance mutation endpoints now call `.ValidateAntiforgery()`.
- `Explore.Blazor.IntegrationTests/Endpoints/BffPreferenceAntiforgeryTests.cs`: added red/green coverage for missing, invalid, and valid antiforgery headers on `/bff/theme`.
- Verification so far: `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` failed 2 new tests before validation and passed 49/49 after validation.
- Intentional exceptions remain: setup-secret bootstrap endpoints and `/bff/auth/refresh-session/internal`. They are documented bootstrap/internal server-side exceptions and were not changed in Phase 1C.

### Phase 1D Implementation Evidence

- `Explore.Blazor/Services/AuthStateSerializationPolicy.cs`: added display-safe Blazor auth-state serialization. Serialized browser claims are limited to `name`, `preferred_username`, `given_name`, and `family_name`; email, stable IDs, tenant IDs, role claims, admin claims, and tokens are excluded.
- `Explore.Blazor/Program.cs`: replaced `SerializeAllClaims = true` with `AuthStateSerializationPolicy.SerializeDisplaySafeClaimsAsync`.
- `Explore.Blazor.Client/Routing/Guards/AdminRouteGuard.cs`: instance-admin route authority now comes from the BFF onboarding status endpoint, not serialized browser admin claims.
- `Explore.Blazor.Client/Routing/Guards/TenantAdminRouteGuard.cs`: tenant-admin route authority now comes from the BFF tenant onboarding status endpoint, not serialized browser admin claims.
- `Explore.Blazor.Client/Layout/NavMenu.razor.cs`: admin navigation helpers no longer derive authority from serialized `explore:admin:*` claims; instance/tenant admin visibility uses BFF-reported state, and organization-admin claim-derived links are suppressed until a server-confirmed affordance exists.
- `Explore.Application/Authorization/AdminClaimTypes.cs`, `Explore.Infrastructure/Identity/AdminClaimsTransformation.cs`, and `Explore.Infrastructure/InfrastructureServicesRegistration.cs`: comments updated to clarify admin claims enrich the server-side principal and must not be serialized as browser authority.
- `Explore.Blazor.IntegrationTests/Services/AuthStateSerializationPolicyTests.cs`: proves only display-safe claims serialize and anonymous users serialize no auth-state data.
- `Explore.Blazor.Client.Tests/Routing/Guards/AdminRouteGuardTests.cs`, `TenantAdminRouteGuardTests.cs`, and `Explore.Blazor.Client.Tests/Layout/NavMenuAdminTests.cs`: prove browser admin claims alone no longer grant instance/tenant route access or admin navigation links.
- Residual note: `OrgAdminRouteGuard`, `GroupAdminRouteGuard`, `AuthStateService`, `TenantContextProvider`, organization/group member UI, and `ProjectionStatusSection` still contain browser claim reads for IDs/admin membership. The minimized auth-state policy no longer supplies sensitive ID/admin/tenant claims, so these paths fail closed or lose claim-derived shortcuts; proper replacements belong in Phase 4/HAL or a scoped BFF current-user/tenant-context slice.

### Phase 1E Implementation Evidence

- `Explore.Blazor/Services/StorageUploadSessionStore.cs`: added BFF-owned upload-session store backed by `IDistributedCache`. Sessions bind the exact server-issued upload URL to the authenticated user, content type, object key, view URL, and expiry; arbitrary non-presigned URLs, cross-user reuse, content-type mismatch, expired/corrupt sessions, and missing sessions fail closed with safe failure codes.
- `Explore.Blazor/Extensions/BffStorageEndpoints.cs`: added `POST /bff/storage/upload-session` and changed `/bff/storage/upload-proxy` to require `uploadSessionId` instead of trusting a caller-supplied `uploadUrl`. The proxy resolves the exact stored destination server-side, consumes the session on successful upload, and logs upstream storage failures without raw response bodies.
- `Explore.Blazor.Client/Services/ImageStorageService.cs`: browser full-flow uploads now request a BFF upload session and send only `uploadSessionId`, `contentType`, and file bytes to the proxy. Non-browser/server paths retain direct trusted presigned URL uploads.
- `Explore.Blazor.IntegrationTests/Endpoints/BffStorageUploadProxyTests.cs`: proves an arbitrary presigned-looking HTTPS URL is rejected by the proxy because a server-issued upload session is required. The test overrides antiforgery intentionally to isolate Phase 1E destination binding; Phase 1C owns antiforgery coverage.
- `Explore.Blazor.IntegrationTests/Services/StorageUploadSessionStoreTests.cs`: covers untrusted URL rejection, cross-user session rejection, content-type mismatch rejection, and successful exact server-issued URL resolution.
- Verification so far: `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` passed 56/56; `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed 1065 total, 1064 succeeded, 1 skipped.
- Residual note: `UploadUrlResponseDto` in API/Application remains unchanged and has no upload-session field because Phase 1E uses a BFF-only session wrapper to avoid broad API/Application/Infrastructure changes. Direct non-browser upload paths still accept trusted presigned URLs generated for that request. The proxy test bypasses antiforgery only to isolate destination binding, not because antiforgery is optional.

### Client Services

- `Explore.Blazor.Client/Extensions/HttpResponseExtensions.cs:10-13`: all API-calling code should use safe response helpers.
- Service grep found raw HTTP handling in 12 service files, including `AppearanceThemeService.cs`, `FooterAdminService.cs`, `InstanceOnboardingService.cs`, `ImageStorageService.cs`, `TenantNavigationService.cs`, `TenantOnboardingService.cs`, `PublicExperienceService.cs`, template sync services, `BffClient.cs`, and `FeatureFlagClientService.cs`.
- `Explore.Blazor.Client/Services/ImageStorageService.cs:27-88`: broad interface spans file reading, upload URLs, uploads, records, delete, previews.
- `Explore.Blazor.Client/Services/ImageStorageService.cs:600-760`: orchestration, generated client call, raw BFF HttpClient, raw deserialization, and error mapping in one service.

### UI Components

- `Explore.Blazor.Client/Pages/Events/EventList.razor.cs:27-44`: many injected services.
- `Explore.Blazor.Client/Pages/Events/EventList.razor.cs:1668-1818`: inline registration workflow embedded in page.
- `Explore.Blazor.Client/Pages/Events/Sessions/CreateSession.razor:1-130` and `EditSession.razor:1-130`: near-duplicate session forms.
- `Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantLookupTablesSection.razor:415-667`: repeated category/tag/location create/edit/delete workflows.
- `Explore.Blazor.Client/Pages/Organizations/OrganizationMembers.razor:22`, `62`, `87`: role-helper action gating.
- `Explore.Blazor.Client/Pages/Organizations/OrganizationMembers.razor:64`, `89`: physical `text-align:right` usage.
- `Explore.Blazor.Client/Services/DialogOptionsFactory.cs:1-60`: standard dialog options exist.
- `TenantLookupTablesSection.razor:417`, `448`, `497`, `535`, `584`, `622`: manual `new DialogOptions` still used.
- `Explore.Blazor.Client/Pages/Admin/Components/ApiKeysSection.razor:108-115`: icon-only delete button lacks `aria-label`; uses physical `text-align:right`.
- `Explore.Blazor.Client/Pages/Admin/Components/UiThemeCatalogSection.razor:85-93`: icon-only edit/delete buttons lack `aria-label`.
- `Explore.Blazor.Client/Pages/Events/EventList.razor.css:67-163`: broad `::deep .mud-grid` overrides and `!important`; positive use of container queries/logical properties also present.

## Active Workstream Check

No existing `dev/active/blazor-bff-hardening-refactor/` directory existed before this planning work. Existing active files were standalone reports/notes: `README.md`, `onboarding-challenging-bug.md`, `mvp-report.md`, `prd.md`, `multi-tenancy-vs-single-tenant-support.md`, `modularity-event-aspects-note.md`, and `infisical-report.md`.

## Decisions

1. Security boundary fixes precede UI cleanup.
2. Do not cite `EventList.razor` as missing `h1`; it has `PageTitle` and an `sr-only` h1 from the audit.
3. Do not cite `DockResizeHandle` focus as a violation; it has a replacement `:focus-visible` outline.
4. Treat explicit `InteractiveServer` as an undocumented deviation requiring audit, not an automatic defect.
5. Treat `::deep` as technical debt only where broad/brittle; some third-party MudBlazor internals legitimately require it.
6. Keep API HAL changes conditional and scoped if Blazor cannot consume missing `_links`.
7. Treat this workstream as an umbrella; implementation must proceed through small independently verified slices, not a single broad refactor.
8. Phase 1 is split into 1A setup-secret trust boundary, 1B safe OIDC diagnostics, 1C CSRF/antiforgery contract, 1D auth-state claim minimization, 1E storage upload destination binding, and 1F circuit token lifecycle hardening.
9. `ISetupSecretResolver` must never trust inbound `X-Setup-Secret`. Trusted source order is BFF-owned setup handshake/session state, protected BFF-issued setup cookie, explicitly gated local/dev/bootstrap config fallback, then no header source ever. “BFF-owned setup state” does not mean normal application-user authentication before first-run setup.
10. `SetupSecretResolutionResult` should expose source/failure metadata without secret value, prefix, or length so tests/logs can verify behavior safely.
11. Storage upload hardening should prefer server-issued upload-session IDs or signed descriptors; strict allowlist is only a documented fallback.
12. Antiforgery work must define the full token issuance/cookie/header/client-handler/SSR/WASM/prerender/bootstrap contract and inventory unsafe BFF mutations before adding validation. Phase 1C must only edit identified endpoints selected for that slice and must not fold in unrelated auth refactors.
13. Auth-state minimization requires a compatibility inventory before changing `SerializeAllClaims`; serialized auth state may contain only display-safe identity hints, not authority.
14. Circuit token lifecycle changes require Oracle/design review before coding because scope must account for tabs, circuits, logout, refresh, multi-instance deployment, sticky sessions, restarts, and Data Protection.
15. Do not synthesize missing HAL links client-side from roles, route names, guessed permissions, or cached claims. Missing-but-expected affordances become minimal conditional API HAL tasks.
16. Raw HTTP enforcement must allow documented low-level exceptions for central executors, upload/streaming clients, generated-client adapters, test fakes, and health/debug endpoints before architecture tests ban patterns.

## Compact Threat Model Summary

- Assets: setup secret/session state, access/refresh tokens, BFF session cookie, antiforgery token, upload destination/session descriptor, serialized auth claims.
- Attackers: unauthenticated browser user, authenticated low-privilege user, malicious tenant user, compromised frontend JavaScript dependency, misconfigured reverse proxy/self-hosting environment, partial log reader.
- Primary threats: privileged header injection, CSRF, diagnostic leakage, arbitrary upload proxying, overexposed claims used as authority, cross-user/stale circuit token bleed, dev/bootstrap fallback reaching production.

## Risk Watchlist

| Risk | Mitigation |
|---|---|
| Dev setup breaks when request-header setup-secret trust is removed | Implement replacement trusted source order first; production has no request-header trust; local/dev fallback must be explicit and environment-gated; tests can use resolver doubles. |
| Cookie hardening breaks OIDC or setup flows | Document cookie purpose separately; BFF session, OIDC correlation, nonce, antiforgery, and setup cookies can require different SameSite/Secure/HttpOnly settings. |
| HAL coverage gaps block client role-gate removal | Record missing affordance in the tracking table and create the smallest conditional `add-hal-link` task. |
| UI decomposition becomes abstraction theater | Extract shared workflow mechanics, not business meaning; do not force divergent validation/HAL/reload/dialog copy into fake generic abstractions. |

## Future Tracking Tables

### Render-Mode Audit Table

Future agents must append rows when auditing explicit `@rendermode InteractiveServer` usage.

| File | Current Mode | Decision | Reason | Verification |
|---|---|---|---|---|
| _TBD_ | _TBD_ | _Keep / remove / defer_ | _TBD_ | _TBD_ |

### Missing HAL Affordance Table

Future agents must append rows before creating conditional API HAL work.

| UI File | Resource/Route | Current UI Gate | Expected `_links` Rel | Minimal API Policy/DTO Change | Decision |
|---|---|---|---|---|---|
| _TBD_ | _TBD_ | _TBD_ | _TBD_ | _TBD_ | _TBD_ |

### Allowed Low-Level HTTP Executor Exceptions

Future agents must list approved locations before adding architecture tests that block raw HTTP helpers.

| File/Type | Allowed Raw Operation | Reason | Test/Guard |
|---|---|---|---|
| _TBD_ | _TBD_ | _TBD_ | _TBD_ |

## Recommended Next Session Start

If Phase 1B verification and Oracle review pass, the next implementation slice is Phase 1C only: BFF CSRF/antiforgery contract. Do not begin Phase 1C until Phase 1B has completed build, architecture/docs tests, Oracle review, and context/task updates.
