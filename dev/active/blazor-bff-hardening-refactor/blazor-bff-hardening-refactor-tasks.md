<!-- ABOUTME: Task checklist for implementing the Blazor BFF hardening and client refactor workstream. -->
<!-- ABOUTME: Tracks phase-by-phase implementation, verification commands, and documentation updates. -->

# Blazor BFF Hardening Refactor Tasks

Last Updated: 2026-05-07 Europe/Brussels

## Status Legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Completed
- `[!]` Blocked or needs decision

## Phase 0 — Planning And Setup

- [x] Create `dev/active/blazor-bff-hardening-refactor/` workstream docs.
- [x] Read repo contracts, rules, canonical docs, official docs, and Tavily research sources.
- [x] Verify current-state evidence anchors in BFF/client files.
- [x] Add compact threat model to the plan/context before first security code change.
- [x] Split security hardening into independently verifiable Phase 1A-1F slices.
- [ ] Before first code edit, create a git/status baseline and inspect current branch changes without committing.

## Phase 1A — Setup-Secret Trust Boundary

- [x] Define trusted setup-secret source order before implementing `ISetupSecretResolver`.
  - Acceptance: source order is BFF-owned setup handshake/session state, protected BFF-issued setup cookie, explicitly gated local/dev/bootstrap configuration fallback, and never inbound request header. BFF-owned setup state does not imply normal application-user authentication before first-run setup.
- [x] Define `SetupSecretResolutionResult` with `Found`, `SetupSecretSource`, `Secret`, and safe failure/diagnostic code.
  - Acceptance: logging/tests can assert source/failure behavior without secret value, prefix, or length.
- [x] Add failing integration test proving inbound `X-Setup-Secret` is currently forwarded or would be forwarded by the legacy path.
- [x] Add `ISetupSecretResolver` under `Explore.Blazor/Services` or a more specific BFF forwarding namespace.
  - Acceptance: resolver has one responsibility, uses BFF-controlled sources only, and has tests for source priority, missing-secret behavior, setup-cookie protection expectations, expiry, and completion invalidation.
- [x] Update `YarpProxyExtensions.ForwardSetupSecretAsync` so it never reads `httpContext.Request.Headers["X-Setup-Secret"]`.
  - Acceptance: integration tests prove inbound `X-Setup-Secret` is stripped, ignored, and not forwarded downstream; trusted resolver output wins over fake inbound header.
- [x] Update `docs/BLAZOR.md`, `docs/SECURITY-MODEL.md`, and `docs/TROUBLESHOOTING.md` to remove request-header setup-secret fallback as a trusted source.

## Phase 1B — Safe Auth/OIDC Diagnostics

- [x] Introduce `ISafeDiagnosticsPolicy` for auth/OIDC failures.
- [x] Replace OIDC secret-derived diagnostics with safe correlation-code diagnostics.
  - Acceptance: browser redirects contain only safe error codes/correlation IDs, and logs contain no client-secret prefix/length or raw secret-derived metadata.
- [x] Remove `Console.Error` auth diagnostics and browser `errorDetail` leakage.
- [x] Add tests for OIDC failure redirect safety, correlation ID logging, no secret metadata in logs, and no production-path `Console.Error` usage.

## Phase 1C — BFF CSRF/Antiforgery Contract

- [x] Define BFF antiforgery token issuance and header contract before adding `.ValidateAntiforgery()` broadly.
  - Acceptance: contract names token issuance endpoint/middleware, request-token cookie name, header name, client handler behavior, SSR/WASM/prerender behavior, and bootstrap exceptions.
- [x] Inventory unsafe BFF mutations before editing endpoints.
  - Acceptance: inventory includes preference/appearance/storage/auth/setup endpoint families, identifies which endpoints are changed in this slice, and explicitly defers unrelated auth refactors.
- [x] Verify `BffClient`, generated clients, and preference endpoint callers send antiforgery tokens in legitimate paths.
- [x] Add antiforgery validation to unsafe preference/appearance BFF endpoints.
  - Acceptance: unsafe endpoints reject missing/invalid antiforgery tokens, while any bootstrap exception is explicitly documented and tested.

## Phase 1D — Client Auth-State Claim Minimization

- [x] Inventory current serialized auth-state claim consumers before removing `SerializeAllClaims`.
  - Acceptance: inventory covers display name, email, user id, roles, tenant id, provider, setup/onboarding claims, culture/language/direction, feature flags, and other claim reads.
- [x] Define display-safe serialized-claim allow-list.
  - Acceptance: authorization, tenancy authority, feature access, and action affordances are explicitly excluded from serialized auth state and must come from BFF/API/HAL.
- [x] Restrict `.AddAuthenticationStateSerialization` to minimal display-safe claims.
  - Acceptance: exact serialized-claim allow-list is documented and tested before `SerializeAllClaims` is removed.
- [x] Add tests proving role/permission claims are not used for UI action authority.

## Phase 1E — Storage Upload Destination Binding

- [x] Choose storage upload binding strategy before implementation.
  - Acceptance: upload-session binding is preferred; strict allowlist is allowed only if session binding is not yet available and the decision is documented in context.
- [x] Add storage upload host/bucket/path/session validation.
  - Acceptance: implementation identifies the trusted storage configuration/session source; if `Explore.Blazor` does not already own trusted storage policy configuration, BFF-issued or API-issued upload-session IDs are introduced before proxying uploads instead of duplicating storage policy in the UI layer.
- [x] Add negative storage upload tests for untrusted host, wrong bucket, wrong tenant/user/session prefix, expired upload session, content type mismatch, and arbitrary valid-looking S3 URLs.
- [x] Add positive storage upload test for a valid server-approved upload-session path.

## Phase 1F — Circuit Token Lifecycle Hardening

- [ ] Complete Oracle/design review before changing `CircuitAccessTokenService` or token resolver architecture.
  - Acceptance: design decides per-circuit/per-tab/per-user-session/per-authentication-session scope; logout, refresh, expiry, multiple tabs, multi-instance deployment, sticky sessions, restarts, and Data Protection assumptions are documented.
- [ ] Redesign or bound circuit token bridging to avoid static process-wide user token cache risks.
  - Acceptance: token lifecycle has deterministic cleanup, bounded scope, safe structured logging, and tests covering isolation/expiry behavior.
- [ ] Add tests for logout clearing, expired token rejection, User A/User B isolation, multiple tab/circuit behavior, token refresh behavior, and documented multi-instance behavior.

## Phase 2 — BFF Endpoint Decomposition

- [ ] Extract auth challenge, provider readiness, session refresh, safe return URL, token assessment, and diagnostics services from `BffAuthEndpoints.cs`.
  - Acceptance: endpoint mapper stays thin, extracted services have focused tests, and behavior remains covered by integration tests.
- [ ] Rename misleading refresh response property currently returned as `token = tokenAssessment.Reason`.
- [ ] Extract preference forwarding, validation, and cookie persistence from `BffPreferenceEndpoints.cs`.
- [ ] Centralize BFF-to-API result mapping in `BffForwardingResults` or equivalent.
  - Acceptance: repeated proxy response translation is removed from endpoint handlers and error details are safe by default.
- [ ] Add tests for each extracted service and endpoint behavior.

## Phase 3 — Client HTTP/Result Pipeline

- [ ] Introduce `ApiClientExecutor`/`HttpCommandClient` using `HttpResponseExtensions`.
  - Acceptance: allowed low-level raw HTTP/deserialization locations are listed in the context file before adding enforcement tests.
- [ ] Define explicit `OperationResult<T>`/`ApiResult<T>` for UI-visible service failures.
- [ ] Migrate `ImageStorageService` first and split into file reader, upload client, storage record client, preview service, and content-type classifier.
- [ ] Migrate `FooterAdminService`, `TenantNavigationService`, and `AppearanceThemeService` to the central executor.
- [ ] Migrate onboarding/public-experience/template-sync services or document deferrals.
- [ ] Add tests to prevent new raw `ReadFromJsonAsync`, `GetFromJsonAsync`, and `EnsureSuccessStatusCode` patterns outside low-level executors.

## Phase 4 — UI Decomposition And HAL Affordances

- [ ] Extract `EventListRegistrationWorkflow` from `EventList.razor.cs`.
- [ ] Extract `EventListFilterState`, `EventListDockingController`, and `EventListSelectionController` or equivalent seams.
- [ ] Remove raw exception-detail snackbar messages from registration and similar flows.
- [ ] Create shared `EventSessionForm` used by create/edit session pages.
- [ ] Create `EventSessionFormModelMapper` and `EventSessionSaveCoordinator`.
- [ ] Replace lookup CRUD duplication in `TenantLookupTablesSection.razor` with a generic workflow/component.
- [ ] Replace role-helper action gating in organization/group/admin UI with HAL/action view models.
  - Acceptance: each missing HAL affordance is recorded in the context table with route/resource/action, current gate, expected `_links` rel, and minimal conditional API work if needed.
- [ ] If missing links block HAL migration, create a scoped API HAL task under `add-hal-link`.
  - Acceptance: API scope remains limited to the minimal missing `_links` contract; do not expand this workstream into general API authorization, controller, application, or persistence refactors.

## Phase 5 — Render Mode, Dialog, Accessibility, CSS

- [ ] Audit all explicit `@rendermode InteractiveServer` occurrences and document or remove each deviation.
  - Acceptance: context file contains a render-mode audit table with file, current mode, decision, reason, and verification result.
- [ ] Introduce `DialogWorkflowService` for open/confirm/result/focus patterns.
- [ ] Replace manual `new DialogOptions` with `DialogOptionsFactory` presets.
- [ ] Replace raw icon-only `MudIconButton` usages with `AppIconButton` or add action-specific `aria-label`.
- [ ] Convert physical CSS (`text-align:right`, left/right/margin-left/right/padding-left/right/border-left/right) to logical properties.
- [ ] Reduce broad `::deep .mud-*` and `!important` usage through wrappers, component params, tokens, or approved global overrides.
- [ ] Add live-region announcements and focus restore where dynamic content/dialog workflows require it.

## Verification Checklist

- [ ] Run `dotnet build --configuration Release --verbosity quiet` after each implementation slice.
- [ ] Run `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` after BFF changes.
- [ ] Run `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` after client service/component changes.
- [ ] Run targeted `Explore.Blazor.Client.E2ETests` flows after UI route/dialog/auth changes.
- [ ] Run `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` for architecture/docs-rule impacts.
- [ ] Run `/docs-lint` after documentation changes.
- [ ] Run `lsp_diagnostics` on all modified files before finalizing each slice.
- [ ] Record changed files, tests run, diagnostics result, docs update status, residual risks, and deferrals in `blazor-bff-hardening-refactor-context.md` after each implementation slice.

## Documentation Checklist

- [ ] Keep this tasks file updated after each implementation session.
- [ ] Update `blazor-bff-hardening-refactor-context.md` with decisions, new evidence, and verification results.
- [ ] Update `blazor-bff-hardening-refactor-plan.md` if scope or sequencing changes.
- [ ] Update `docs/BLAZOR.md` and `docs/SECURITY-MODEL.md` when setup-secret, diagnostics, CSRF, or auth-state contracts change.
- [ ] Update design/accessibility docs if reusable dialog/component patterns are introduced.
