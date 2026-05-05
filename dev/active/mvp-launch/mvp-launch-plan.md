ABOUTME: Current enterprise-grade implementation plan for closing MVP launch gaps.
ABOUTME: Rebaselined against source, repo conventions, Tavily research, and Context7 documentation.

# MVP Launch — Implementation Plan

> **Created:** 2026-03-28 | **Rebaselined:** 2026-05-03
> **Branch:** `develop`
> **Goal:** finish the smallest correct launch path for real organizers and event seekers while removing stale plan debt, architecture drift, and partially implemented work.

---

## Executive Summary

The old plan is no longer reliable as an execution document. Several former launch blockers have already landed in source or are mostly complete, while the remaining MVP risk is concentrated in runtime verification, registration integrity, email dispatch, security/audit readiness, public-surface quality, and test evidence.

Current launch posture:

| Area | Status | Required Action |
|------|--------|-----------------|
| Docker/.NET 10/DataProtection/Redis fallback | Implemented; Docker runtime smoke blocked locally | Verify in healthy Docker/Aspire runtime |
| Broken registration email promise | Fixed | Keep as regression gate only |
| Unsubscribe foundation | Implemented; email integration incomplete | Finish headers, branded page, consent checks, audit |
| iCal/calendar download | Implemented; runtime/client evidence incomplete | Verify `.ics`, OpenAPI/NSwag drift, calendar app import |
| Branded error pages | Implemented | Runtime smoke and accessibility check |
| Sitemap/robots/canonicals | Implemented foundation | Runtime smoke; add JSON-LD and OG gaps |
| Capacity/waitlist | Source indicates implementation exists | Audit concurrency, duplicate guard, tests, runtime flow |
| Registration confirmation email | Not complete | Add outbox-backed, idempotent dispatcher |
| Health checks | Source complete for API/BFF dependencies including conditional Cerbos readiness and operator docs | Runtime-smoke `/health` in healthy Docker/Aspire |
| Security audit/CSP/HAL cleanup | Partially open | Remove dev-mode compatibility fallback and close audit gaps |
| E2E/snapshots/coverage | Scaffolded or partial | Turn into release evidence |

This plan replaces the old 25-work-package backlog with a closure plan. Historical discoveries belong in `mvp-launch-context.md`; executable checklists belong in `mvp-launch-tasks.md`; this file is the strategic dependency order and definition of done.

---

## Non-Negotiable Principles

1. No backward-compatibility shims for development-only behavior. Remove stale paths, obsolete HAL fallbacks, and role-gated UI affordance workarounds instead of preserving them.
2. Clean Architecture remains strict: Domain stays dependency-free; Application owns CQRS, validation, specifications, and DTO mapping; Persistence owns EF Core/repositories; API/Blazor are composition roots.
3. Repositories return entities. Handlers map to DTOs. Handlers instantiate validators manually and pass `CancellationToken` end to end.
4. Blazor uses the BFF boundary. Browser code never stores access tokens, API calls go through the BFF, and `HttpContext` is not used in InteractiveAuto/WASM execution paths.
5. HAL `_links` are the sole source of truth for per-resource UI action affordances. Role/claim checks may support coarse navigation only, not mutation buttons.
6. EF Core 10 named query filters must preserve tenant isolation. Use targeted soft-delete filter disabling only, never blunt runtime `IgnoreQueryFilters()`.
7. Runtime verification is required for every implemented foundation. Source existence is not launch readiness.
8. Every API surface change must refresh OpenAPI/NSwag and update consuming Blazor code before the work package can close.
9. E2E failures must leave trace artifacts. Playwright flows should use isolated browser contexts, resilient locators, and trace capture for diagnosis.
10. No new hardcoded user-facing strings in touched Blazor surfaces should bypass the existing localization/translation approach. Do not introduce a parallel `.resx`/`IStringLocalizer` migration in this sprint.

---

## Release Gates

All gates must pass before MVP release.

### Gate A — Deployability

- [ ] `dotnet build --configuration Release --verbosity quiet` succeeds.
- [ ] Docker/Aspire runtime starts API, Blazor BFF, PostgreSQL, Redis when configured, Keycloak, Cerbos, and migration service.
- [ ] DB migrations apply cleanly, including `DataProtectionKeyContext` migrations.
- [x] API and Blazor `/alive` endpoints are liveness-only in source; API integration coverage confirms `/alive` returns 200 without OIDC.
- [x] API and Blazor `/health` endpoints are readiness-only and report tagged dependency checks without hiding degraded dependencies.
- [ ] Redis absence follows the intended fallback behavior and logs the effective cache backend.

### Gate B — Session Integrity

- [ ] Login survives Blazor container restart.
- [ ] Auth cookie remains valid after recycle because DataProtection keys persist.
- [ ] No token is exposed to WASM/browser storage.
- [ ] BFF token forwarding works only server-side and preserves tenant/setup-secret forwarding where required.

### Gate C — Registration Truthfulness & Email

- [ ] Registration succeeds without false UI promises.
- [ ] Registration creates an outbox row atomically with the registration intent.
- [ ] `RegistrationConfirmed` dispatch sends an email or retries/dead-letters with structured logs.
- [ ] Handler is idempotent under outbox replay.
- [ ] Email includes visible unsubscribe link and RFC 8058 headers.
- [ ] Dispatch checks notification preferences and skips opted-out categories with metrics/logging.

### Gate D — Public Event Completion Loop

- [ ] Anonymous users can discover and open published event details.
- [ ] Draft events are hidden from anonymous users; archived events return not found.
- [ ] User can register, see next steps, view My Registrations, download `.ics`, and share the event.
- [ ] Capacity-full sessions waitlist safely without over-registration.
- [ ] Duplicate registration is prevented at the application and persistence boundary.

### Gate E — Legal & Compliance

- [ ] Privacy Policy, Terms, Community Guidelines, Accessibility Statement, and License decision are reachable or explicitly waived.
- [ ] Cookie consent remains functional and persists preference.
- [ ] One-click unsubscribe endpoint works anonymously and safely for invalid/expired tokens.
- [ ] Preference changes produce audit entries with correlation IDs.

### Gate F — SEO & Public Surface

- [ ] `/sitemap.xml` returns tenant-correct URLs for public static pages and published events.
- [ ] `/robots.txt` allows indexing only in production-like environments and disallows non-production.
- [ ] Public pages have canonical URLs and required OG/Twitter tags.
- [ ] Event details include valid `schema.org/Event` JSON-LD generated from real event data.
- [ ] Organization profile includes valid `schema.org/Organization` JSON-LD where data exists.
- [ ] Branded 404/403/500 pages render in real browser navigation and expose correlation ID on 500.
- [x] Production source no longer depends on `placehold.co`; runtime visual smoke for image fallbacks remains.

### Gate G — Security, Authorization, And Audit

- [ ] Setup-secret endpoints use the setup-secret rate-limit policy and have tests proving metadata.
- [ ] PII reads and admin/tenant/instance setting changes write audit entries.
- [ ] Authorization denials are structured-logged with principal, resource, action, tenant, and correlation ID.
- [x] BFF responses include a conservative CSP header from the Blazor host boundary.
- [ ] Blazor/MudBlazor assets are runtime-smoked under the CSP in a healthy app host.
- [ ] Audit log reads are tenant/instance scoped correctly.
- [x] `HateoasAuthorizationEvaluator.MapMethodToAction()` fallback is removed; permission-bound links without explicit actions fail closed.
- [ ] UI mutation affordances are HAL-link driven.

### Gate H — Test Evidence

- [ ] Architecture tests pass for Clean Architecture, CQRS, API contracts, and Blazor client rules.
- [ ] Application handler tests pass and handler coverage target is met or explicitly waived with risk.
- [ ] Persistence integration tests cover DataProtection and registration capacity/duplicates.
- [ ] API integration tests cover unsubscribe, health, authz, sitemap/robots, and ProblemDetails shape.
- [ ] Blazor client/integration tests cover My Registrations, post-registration UX, error pages, and HAL affordances.
- [ ] E2E critical flows run in CI/runtime with Playwright trace artifacts on failure.
- [ ] Snapshot tests cover HATEOAS links and stable ProblemDetails/DTO contracts.

---

## Execution Phases

### Phase 0 — Evidence Baseline

Purpose: stop implementing against stale assumptions.

Actions:

- [ ] Run `git status --short` and identify unrelated in-flight changes before touching source.
- [ ] Reconcile `mvp-launch-tasks.md` statuses with current source for WP-1, WP-6, WP-14, WP-15, WP-16, and WP-17.
- [ ] Confirm publish-flow files are in-flight before planning WP-9 source edits.
- [ ] Run the canonical build.
- [ ] Run non-Docker architecture/unit tests that are not blocked by local Docker.
- [ ] Record Docker/Aspire blocker separately from product readiness.

Exit criteria:

- Current source status is documented.
- No work package says “not started” when implementation exists.
- Stale references to `dev/active/navbar-customization/` are removed or replaced with `dev/active/sidebar-dock-layout-refactor/` only if shell/layout work is actually launch-critical.

### Phase 1 — Close Implemented Foundations

Purpose: convert landed source into release evidence.

Work:

- WP-1 runtime smoke: Docker/Aspire start, migrations, DataProtection restart, Redis fallback.
- WP-6 runtime smoke: `.ics` endpoint content type, UID stability, UTC timestamps, import into a calendar app, OpenAPI/NSwag refresh decision.
- WP-14 foundation closure: branded unsubscribe confirmation UI, audit entry, integration tests.
- WP-15 runtime smoke: 404/403/500 routes and status-code re-execution.
- WP-16 runtime smoke: sitemap, robots, canonical tags.
- WP-17 source audit: verify capacity/waitlist implementation, duplicate guard, transaction/concurrency safety, lookup seed, UI states, tests.

Exit criteria:

- Gates A, B, D partial, E partial, and F partial have evidence.
- Runtime-only gaps are not mixed with implementation gaps.

### Phase 2 — Registration Integrity And Email

Purpose: make the registration loop truthful, useful, and compliant.

Work:

- Implement or finish registration confirmation outbox flow.
- Add `RegistrationConfirmed` handler routing instead of logging-only dispatch.
- Use reference payload IDs and fetch fresh data at dispatch time.
- Enforce idempotency with a durable marker or send log keyed by registration/outbox event.
- Inject unsubscribe URL and RFC 8058 headers.
- Respect user notification preferences at dispatch.
- Ensure duplicate-registration guard is enforced in handler/repository/database.
- Ensure waitlist state is clear in event list, registration modal, post-registration success, and My Registrations.

Exit criteria:

- Gates C and D pass in runtime.
- Email send/retry/dead-letter behavior has tests and logs.

### Phase 3 — Operations, Security, And Authorization Cleanup

Purpose: remove enterprise launch risk and development-mode compatibility debt.

Work:

- [x] Add readiness health checks for PostgreSQL, distributed cache/Redis fallback state, Keycloak/OIDC discovery, SMTP, API downstream readiness, and secret-provider checks.
- [x] Preserve `/alive` as liveness and keep `/health` readiness dependency-sensitive.
- [x] Add conditional Cerbos readiness when the instance authorization provider is configured as Cerbos.
- [x] Add operator-facing health interpretation docs for Healthy/Degraded/Unhealthy dependency states.
- Add audit log writes for PII reads, admin changes, preference changes, and authorization denials.
- Ensure setup-secret rate limiting is applied everywhere the secret can be validated.
- [x] Add CSP at the Blazor BFF boundary.
- [ ] Verify MudBlazor/Blazor assets still load under the CSP in a healthy runtime.
- [x] Remove `MapMethodToAction()` HAL fallback by requiring explicit policy action metadata.
- Replace remaining per-resource `RoleHelper` mutation affordance gating with HAL-link checks.
- Disable external API key functionality for MVP unless rate limits, auditing, and docs are complete.

Exit criteria:

- Gate G passes.
- No compatibility fallback remains without an explicit tracked post-MVP justification.

### Phase 4 — Public Surface And Product Polish

Purpose: make the public MVP credible without starting a large redesign.

Work:

- Add Event JSON-LD from actual event data: name, description, start/end, location/online URL, image, organizer, status, offer only if pricing exists.
- Add Organization JSON-LD only from trustworthy org fields.
- Fill OG/Twitter gaps on Home, Landing, EventDetail, OrganizationProfile, and OrganizationDetail.
- Add or explicitly waive Accessibility Statement and License pages.
- Add manifest-only PWA support if not present; defer service worker/offline mode.
- [x] Replace `placehold.co` source references with local SVG fallbacks through `ImageHelper`.
- Verify public/registration/event cards render acceptable fallback imagery in browser smoke.
- [x] Replace landing-page hardcoded member-count TODO with actor API total-count retrieval.
- Defer API/client-generation-dependent TODOs with explicit tracking instead of blocking MVP closure.
- Re-check `CreateEvent` price/currency and TODO state after publish-flow dirty work is reconciled.
- Run accessibility checks on registration, event detail, error pages, unsubscribe, and My Registrations.

Exit criteria:

- Gate F passes.
- Public pages do not look unfinished under anonymous browsing.

### Phase 5 — Quality Gate Burn-Down

Purpose: transform partial test scaffolding into release confidence.

Work:

- Finish E2E critical flows: anonymous discovery, login/BFF token chain, registration, waitlist, unsubscribe, tenant isolation, authorization denial.
- Configure Playwright traces/screenshots/snapshots for CI failure artifacts.
- Finish snapshot coverage for HATEOAS link matrices, DTO shapes, and ProblemDetails 400/401/403/404/500.
- Raise handler coverage for launch-critical command/query handlers or document targeted waivers.
- Run architecture tests for Clean Architecture, CQRS, API contracts, Blazor client architecture, and naming.

Exit criteria:

- Gate H passes.
- Remaining defects are triaged as launch-blocking, launch-waived, or post-MVP.

---

## Work Package Disposition

| Old WP | New Status | Disposition |
|--------|------------|-------------|
| WP-1 Infrastructure | Implemented; needs runtime evidence | Phase 1 |
| WP-2 Navbar customization | Stale reference | Replace with sidebar/dock handoff only if launch-critical; otherwise remove from MVP |
| WP-3 Registration email | Open | Phase 2 |
| WP-4 My Registrations | Implemented/enhanced; verify | Phase 1/2 smoke |
| WP-5 Post-registration UX | Implemented; verify | Phase 1/2 smoke |
| WP-6 iCal | Implemented; verify | Phase 1 |
| WP-7 Share verification | Implemented; verify list/detail | Phase 1/4 smoke |
| WP-8 HATEOAS cleanup | Open debt remains | Phase 3 |
| WP-9 Save Draft vs Publish | In-flight source changes detected | Phase 3 after current branch state is reconciled |
| WP-10 Onboarding | Existing; decide waiver only | Phase 4 if launch-critical |
| WP-11 Test gap sweep | Partial | Phase 5 |
| WP-12 External API key | Risk | Disable for MVP unless fully hardened |
| WP-14 Unsubscribe | Partial | Phase 1/2 |
| WP-15 Error pages | Implemented; verify | Phase 1 |
| WP-16 SEO foundation | Foundation implemented; harden | Phase 1/4 |
| WP-17 Capacity/waitlist | Source indicates implemented; audit | Phase 1/2 |
| WP-18 Health checks | Core implementation complete; Cerbos/docs/runtime smoke remain | Phase 3 |
| WP-19 Audit hardening | Open/partial | Phase 3 |
| WP-20 JSON-LD/OG | Open/partial | Phase 4 |
| WP-21 E2E | Scaffolded/partial | Phase 5 |
| WP-22 Snapshots | Partial | Phase 5 |
| WP-23 Accessibility | Open/partial | Phase 4/5 |
| WP-24 PWA manifest | Open unless source proves otherwise | Phase 4 |
| WP-25 Placeholder/TODO cleanup | Open | Phase 4 |

---

## Technical Debt Burn-Down

These debts must be removed, explicitly waived, or ticketed before launch sign-off.

| Debt | Required Outcome |
|------|------------------|
| Stale dev docs and Quick Resume | Plan/context/tasks describe the same current state |
| Dead `dev/active/navbar-customization` reference | Removed or replaced with actual active track |
| Docker verification blocked locally | Runtime evidence collected in a healthy environment |
| Generated client drift | OpenAPI/NSwag refreshed after API changes |
| `MapMethodToAction()` fallback | Removed or unreachable through explicit HAL policy actions |
| Per-resource RoleHelper mutation gating | Replaced with HAL-link checks |
| External placeholder image service | Replaced with local/tenant-branded fallbacks |
| Product TODOs in touched files | Resolved or converted into tracked non-MVP tickets |
| Incomplete audit writes | PII/admin/preference/authz events logged |
| E2E scaffolds not green | CI/runtime E2E evidence with traces |
| Snapshot gaps | HATEOAS/ProblemDetails/DTO contracts covered |

---

## External Guidance Applied

- Microsoft Blazor security guidance: keep tokens and sensitive security data on the server; use a BFF pattern for InteractiveAuto/client interactions; avoid client token handling.
- Microsoft EF Core 10 guidance: use named query filters for soft delete and tenant filters, and disable only the named filter required by a query.
- Playwright guidance: tests should use isolated browser contexts, resilient locators, auto-waiting/web assertions, and trace capture for debugging.
- Schema.org/Google structured-data guidance: Event JSON-LD should describe real event fields and be validated as release evidence, not just string-injected markup.
- MudBlazor guidance and repo conventions: use current v9 APIs and shared project wrappers/design tokens rather than ad hoc component styling.

---

## Canonical Verification

Do not run solution-level `dotnet test`. Use project-level commands.

Minimum verification set:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

Add integration/runtime suites when Docker/Aspire is healthy:

```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
```

Use the repo `/check` command when preparing final sign-off because it mirrors the repository verification policy.

---

## Quick Resume

1. Read `mvp-launch-context.md` for current decisions and status.
2. Use `mvp-launch-tasks.md` as the executable checklist.
3. Start with Phase 0 evidence and status reconciliation, not WP-1.4.
4. Close implemented foundations with runtime evidence before writing new feature code.
5. Then execute Phase 2 registration/email, Phase 3 security/ops, Phase 4 public surface, and Phase 5 test evidence.
