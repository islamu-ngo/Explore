ABOUTME: Current decisions, files, and constraints for the MVP launch sprint.
ABOUTME: Read this before resuming implementation; pair it with mvp-launch-plan.md and mvp-launch-tasks.md.

# MVP Launch — Context

## Session Progress

### Completed 2026-05-03 Rebaseline

- Replaced stale `mvp-launch-plan.md` with a source-aligned closure plan.
- Updated `mvp-launch-tasks.md` Quick Resume and closure order so work starts with evidence/status reconciliation, not old WP-1.4.
- Reviewed repo conventions: Clean Architecture, CQRS/MediatR, EF Core 10 named query filters, Blazor BFF/InteractiveAuto, HAL-driven UI affordances, and project-level test verification.
- Used Tavily for current external research on Microsoft Blazor security/BFF guidance, EF Core 10 named filters, Playwright testing practices, and schema.org/Event structured data.
- Used Context7 for ASP.NET Core Blazor security, EF Core 10 filters, MudBlazor, and Playwright .NET tracing/isolation docs.
- Collected parallel codebase/doc-agent outputs. The external-docs subagent failed because its configured model was unavailable; direct Tavily/Context7 research replaced it.

### Completed 2026-05-03 WP-18 Core Health Checks

- Implemented readiness/liveness separation in `Explore.ServiceDefaults`: `/health` now includes checks tagged `ready`; `/alive` remains liveness-only.
- Added shared readiness checks for distributed cache and OIDC discovery.
- Added API SMTP readiness via `IEmailService.TestConnectionAsync()`.
- Added Blazor BFF downstream API readiness and EF Core database readiness.
- Tagged secret-provider health checks as readiness checks so secret backends are visible in `/health`.
- Exposed Blazor Redis fallback state through `IDistributedCacheBackendState` so configured Redis fallback is reported as degraded instead of hidden.
- Verification passed: `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` and `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet`.
- Verification passed: `dotnet run --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release -- --treenode-filter "/*/*/*NoKeycloakAuthenticationTests*/*" --minimum-expected-tests 1 --no-progress` ran 8 tests, 0 failed.
- Verification note: `dotnet test --filter` is not valid for these TUnit/Microsoft Testing Platform projects; use `dotnet run --project ... -- --treenode-filter ...` for targeted runs.

### Current Source-Aligned Findings

- WP-1 infrastructure source work is implemented; Docker/Aspire runtime evidence remains blocked locally by Docker Desktop/QEMU.
- WP-14 unsubscribe foundation exists; remaining work is branded confirmation UI, audit logging, email header injection, dispatch-time consent, and integration tests.
- WP-15 branded error pages exist; runtime route/status-code smoke remains.
- WP-16 sitemap/robots/canonical foundation exists; JSON-LD and OG gaps remain.
- WP-6 iCal endpoint/UI exists; runtime/calendar-app and generated-client drift checks remain.
- WP-4, WP-5, and WP-7 are implemented enough to shift to runtime smoke/regression evidence.
- WP-17 capacity/waitlist appears implemented in source even though old tasks said not started; audit before adding code.
- WP-18 health checks are source complete, including conditional Cerbos readiness and operator interpretation docs; remaining WP-18 work is runtime smoke with live dependencies.
- WP-19.5 BFF CSP is implemented in `Explore.Blazor/Extensions/MiddlewareExtensions.cs` and registered in `Explore.Blazor/Program.cs`; runtime browser smoke remains.
- WP-19.7 HAL legacy fallback removal is implemented in `Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`: permission-bound links without explicit actions now fail closed and no HTTP method action inference remains.
- WP-19.7 verification passed through `AuthorizationParityTests`; the targeted Application unit test project is blocked by unrelated custom-property projection test compile errors.
- WP-25.1 placeholder cleanup is source complete: `ImageHelper` generates local SVG data URI fallbacks, `MyEvents` and `MyRegistrations` use it, and no `placehold.co` source references remain outside tests/docs.
- WP-25.2 landing-page member count is source complete: `LandingPageService` now reads `GetActorsAsync(...).TotalCount` instead of hardcoded placeholder values, with targeted tests for success and API failure fallback.
- WP-25 price/currency audit is partially complete: visible event price/currency usage is display-only in list/detail/card/created pages, and `EventEdit.razor` has no visible event price/currency inputs; `CreateEvent` must be re-checked after publish-flow dirty work is reconciled.
- WP-19 audit hardening outside CSP, WP-20 JSON-LD/OG, WP-23 accessibility, WP-24 manifest, and deferred API/client-generation TODO cleanup remain active launch work.
- Some Blazor pages still use `RoleHelper` for action affordance decisions; distinguish allowed coarse navigation from forbidden per-resource mutation gating.
- Runtime visual smoke is still needed for local image fallbacks and landing-page stats after the Blazor publish-flow blocker is cleared.
- Event publish flow files are currently dirty/in-flight; reconcile before editing WP-9 source.
- `dev/active/navbar-customization/` is a stale reference; use `dev/active/sidebar-dock-layout-refactor/` only if shell/sidebar work is launch-critical.

### Next Work

- Phase 0 evidence baseline: reconcile source/task status, run build and available project tests, and record Docker blocker separately from product work.
- Phase 1 runtime verification for already implemented foundations: WP-1, WP-6, WP-14, WP-15, WP-16, WP-17, and WP-18 runtime dependency states.
- Next implementation slice should be WP-19 audit hardening, WP-25 placeholder/TODO cleanup, or WP-3 registration email/consent, depending on whether publish-flow dirty files are still in-flight.

### Blockers

- Docker/Testcontainers runtime verification is blocked in the current local environment by Docker Desktop/QEMU startup failure.
- `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet` is currently blocked by unrelated dirty publish-flow/client work, including `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs` using `??` between `List<EventPublishReadinessErrorDto>` and an array. Do not treat this as CSP-slice failure.
- `dotnet run --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release -- --treenode-filter "/*/*/*HateoasAuthorizationEvaluatorTests*/*" --minimum-expected-tests 1 --no-progress` is currently blocked before execution by unrelated custom-property projection test compile errors.
- API build verification passes after conditional Cerbos readiness; the targeted Application unit project remains blocked by unrelated custom-property projection test signature drift.
- Do not treat that local infrastructure blocker as proof the product code is launch-ready; collect evidence in a healthy runtime.

---

## Key Decisions

### D1: Email Verification Ownership

- Email verification is Keycloak's responsibility.
- AT Protocol handle verification is the PDS responsibility.
- The application does not implement account email verification.

### D2: DataProtection Strategy

- DataProtection persistence is Blazor BFF only. Do not register the BFF key ring in API.
- Use separate `Explore.Persistence/DataProtectionKeyContext.cs`; do not attach global keys to tenant-scoped `ExploreDbContext`.
- Current package version is `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` 10.0.7 via central package management.
- `Event.MigrationService` applies DataProtection migrations.

### D3: Outbox Payload

- Registration email outbox payloads should be reference payloads with IDs only.
- Dispatch handlers fetch fresh data from repositories at send time.
- Consumers must be idempotent because the outbox is at-least-once.

### D4: Email Idempotency

- Key idempotency by event type plus registration/outbox identity.
- Prefer durable send markers or send-log checks over in-memory suppression.
- Structured logs must include registration, event, user, tenant, outbox message, and correlation IDs.

### D5: Outbox Dispatch Strategy

- Replace logging-only dispatch for known launch event types with routing to explicit handlers.
- Unknown event types may fall back to logging, but launch-critical events cannot be logging-only.

### D6: Email Template Approach

- Keep MVP templates simple and maintainable; do not introduce a full templating engine unless needed.
- Email builder must support unsubscribe URL/header data and tenant-safe public URLs.

### D7: iCal Library

- `Ical.Net` 5.2.1 is installed for single-event `.ics` export.
- Stable UID should derive from event identity.
- Timestamps must be normalized and validated by runtime/calendar-app smoke tests.

### D8: Redis Degradation

- The app must run without Redis for self-hostable MVP deployments.
- If Redis is configured but unavailable, log degradation clearly and expose readiness status according to environment expectations.
- Always log the effective cache backend.

### D9: External API Key Scope

- Disable external API key functionality for MVP unless rate limiting, auditing, documentation, and tests are complete.
- A safe disabled state is preferable to partial public API security.

### D10: Publish UX Scope

- MVP needs clear Save Draft versus Publish behavior.
- Publish-flow source files are currently in-flight; inspect before changing WP-9 code.
- Advanced undo/beforeunload/status workflows are post-MVP unless already present.

### D11: Localization Scope

- Do not start a parallel `IStringLocalizer`/`.resx` migration during MVP launch.
- Avoid adding new hardcoded user-facing strings in touched files when the existing translation/service pattern is available.
- If translation integration is outside the work package, document the waiver instead of inventing a second localization architecture.

### D12: Capacity Scope

- MVP must prevent over-registration and represent waitlist state clearly.
- Auto-promotion, organizer capacity alerts, bulk approval, and CSV export are post-MVP.
- Because source appears to include capacity/waitlist support now, audit before implementing.

### D13: Unsubscribe Mechanism

- Use per-category unsubscribe tokens, not a global kill switch.
- Support RFC 8058 `List-Unsubscribe` and `List-Unsubscribe-Post` for one-click unsubscribe.
- Tokens use `ITimeLimitedDataProtector` and fail safely for invalid/expired input.

### D14: PWA Scope

- MVP is manifest-only if not already present.
- Service worker, offline caching, background sync, and push are post-MVP.

### D15: Health Check Strategy

- Keep liveness separate from readiness.
- Readiness includes database, distributed cache/Redis fallback state, Keycloak/OIDC discovery, SMTP, conditional Cerbos PDP readiness, downstream API readiness for the BFF, and secret-provider checks.
- Degraded dependencies should be explicit, not hidden.
- Missing SMTP configuration is degraded; configured-but-failing SMTP is unhealthy.
- Missing OIDC configuration is healthy/skipped so fresh self-hosted deployments still start; configured-but-invalid OIDC metadata is unhealthy.
- Local authorization provider mode skips Cerbos readiness; configured instance Cerbos mode is unhealthy if the PDP cannot pass gRPC health.

### D16: Audit Log Access Control

- Instance admins can read all audit entries.
- Tenant admins can read tenant-scoped entries.
- Regular users can read their own audit entries only.
- PII access, admin setting changes, preference changes, and authorization denials need audit/structured-log evidence.

### D17: Setup-Secret Rate Limiting

- Reuse the existing setup-secret rate-limit policy.
- Tests should verify endpoint metadata for every setup-secret validation path.

### D18: Error Page Strategy

- Branded 404/403/500 pages already exist.
- Remaining work is runtime status-code re-execution, browser smoke, accessibility, and 500 correlation display.

### D19: Feeds Deferred

- MVP is single-event `.ics` download.
- Organization/tenant calendar feeds and RSS/Atom are post-MVP.

### D20: Snapshot Testing

- `Verify.TUnit` 31.9.4 is in use.
- Snapshot diffs must be reviewed intentionally.
- Prioritize HATEOAS links, DTO shapes, and ProblemDetails contracts.

### D21: Placeholder Asset Policy

- Production pages must not depend on `placehold.co`.
- Missing logos/images should use local or tenant-branded fallbacks; current source fallback is `ImageHelper` SVG data URIs.

### D22: Accessibility Scope

- MVP accessibility work should focus on launch-critical flows: discovery, event detail, registration, My Registrations, error pages, unsubscribe, and onboarding.
- Use existing design system wrappers and token patterns.

---

## Key Files

| Area | Files |
|------|-------|
| Plan docs | `dev/active/mvp-launch/mvp-launch-plan.md`, `mvp-launch-tasks.md`, `mvp-launch-context.md` |
| DataProtection | `Explore.Persistence/DataProtectionKeyContext.cs`, `Explore.Persistence/Extensions/DataProtectionServiceCollectionExtensions.cs`, `Event.MigrationService/Worker.cs` |
| Unsubscribe | `Explore.API/Controllers/EmailUnsubscribeController.cs`, `Explore.Infrastructure/Mail/Unsubscribe/EmailUnsubscribeTokenService.cs` |
| Calendar | `Explore.API/Controllers/EventController.cs`, `Explore.API/Hateoas/RouteNames.cs`, Blazor event detail/My Registrations pages |
| SEO/errors | `Explore.API/Controllers/SitemapController.cs`, `Explore.Blazor/Controllers/RobotsController.cs`, `Explore.Blazor.Client/Pages/Errors/` |
| Capacity/waitlist | `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs`, `Explore.Persistence/Repositories/EventRegistrationIntentRepository.cs`, `ApprovalStatusEnum`, lookup seed |
| HAL/security debt | `Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`, Blazor pages using `RoleHelper` for action affordances |
| Placeholder debt | `Explore.Blazor.Client/Helpers/ImageHelper.cs`, `Explore.Blazor.Client/Pages/User/MyRegistrations.razor`, `Explore.Blazor.Client/Pages/Events/MyEvents.razor.cs` |
| Publish flow in-flight | Event publish command/request/validator/handler/controller/HATEOAS files shown dirty in current worktree |

---

## Quick Resume

1. Open `mvp-launch-plan.md` and follow Phase 0.
2. Use `mvp-launch-tasks.md` only after reconciling old task statuses with source.
3. Do not restart old WP-1.4; it is already done.
4. Close runtime evidence for implemented foundations first.
5. Then complete registration email/consent, health/audit/CSP/HAL cleanup, public SEO polish, and test evidence.

## Session Handoff — 2026-05-03 Europe/Brussels

No implementation work was performed for this active task during the sidebar dock refactor handoff session. Existing context, plan, and task files remain the authoritative state for this workstream. Do not infer progress or blockers here from the sidebar/dock-specific changes unless a future session explicitly broadens scope.
