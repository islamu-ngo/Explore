ABOUTME: Strategic implementation plan for stabilizing the OpenAPI contract that feeds the NSwag-generated Blazor client.
ABOUTME: Eliminates 464 suffix-disambiguated client methods by deleting URL-segment versioning, adopting multi-reader versioning, and treating OpenAPI as a governed product artifact.

# API Contract Stabilization - Implementation Plan

**Last Updated:** 2026-04-19 (v3 — post-user versioning decision + Phase 0/1 execution)
**Parent plan for:** `dev/active/hateoas-client-alignment/` (downstream workstream)
**Owner:** Sisyphus
**Effort:** Large (3+ days)
**Risk:** Medium (API contract churn, mitigated by guardrail-first ordering)
**Review:** Approved with architectural tightening (CTO review 2026-04-19) + user versioning lock-in (m0044) + enum naming lock-in (m0086)
**Status:** Phase 0 ✅ COMPLETE | Phase 1 🟡 IN PROGRESS (bulk annotation) | Phase 2+ ⏳

---

## Executive Summary

The Blazor application functions end-to-end today (BFF wiring, service layer, pages, admin settings all verified clean via 5-agent audit), but the NSwag-generated `IEventApiClient` contains **464 suffix-disambiguated method names** (`TenantGET2Async`, `TenantDELETE2Async`, `Status8Async`, `Internal2Async`, …). This is unmaintainable, unsearchable, and hostile to HAL link-policy alignment work already in flight (`hateoas-client-alignment`).

**Root cause** is an **API/OpenAPI contract defect**, not an NSwag defect:

1. `docs/ARCHITECTURE.md` (line 57) documents the **dual API versioning**: media-type (`Accept: application/json;v=0.1`) plus URL segment (`/api/v0.1/controller`) driven by `VersionedRouteConvention`.
2. Every `[ApiVersion("0.1")]` controller therefore registers at **two routes**.
3. The versioned selector clones lose their `operationId` (action `Name = null`) and emit anonymous operations.
4. `swagger.json` contains **both** canonical and aliased operations.
5. NSwag with `SingleClientFromOperationId` collides and falls back to numeric-suffix disambiguation.
6. Result: **464 `\dAsync` methods** in `EventApiClient.g.cs` (86 632 lines).

**The fix** is not `useOperationIds=false` (symptom-rename only) and **not just hiding the aliases from OpenAPI**. The fix is to **delete the URL-segment alias strategy entirely** while **keeping media-type versioning and adding query-string + custom-header readers** (user decision m0044 — permanent multi-reader strategy, not pre-1.0 temporary). Controllers carry exactly one `[Route("api/[controller]")]` — URL-segment routes (`/api/v0.1/...`) are banned and must 404 at runtime.

Stabilize `operationId`s, install **contract governance**, regenerate once, and repair only the 5 service wrappers that called suffixed names.

We are in development; no backwards-compatibility obligations. **Delete complexity, don't hide it.**

---

## Strategic Frame (CTO-approved)

1. **Treat the OpenAPI contract as a governed product artifact**, not a generated side-effect. Checked-in `swagger.json` is first-class; changes to public routes, operationIds, schemas, or response semantics require review.
2. **Delete, don't hide.** Remove URL-segment alias routes entirely unless a real consumer proves they're needed. Undocumented runtime behaviour is worse than no behaviour.
3. **Stop publishing what we can't govern.** Every operation is either deliberately part of the public contract, deliberately part of the authenticated-admin contract, or deliberately excluded. No accidental exposure.
4. **Single versioning strategy**, single OpenAPI document, single NSwag input, single client.
5. **Forward guardrails + schema-diff visibility** — CI catches drift; diffs are surfaced even when we don't block on them.

---

## Endpoint Classification

Every controller action MUST declare which class it belongs to via `[EndpointClassification(EndpointClass.X)]` (user decision m0083: explicit attribute over convention inference; m0086: final enum labels Public / Authenticated / Admin).

| Class | Audience | In Canonical OpenAPI? | In Typed `IEventApiClient`? | Auth Pattern |
|---|---|---|---|---|
| **Public** | External consumers, unauthenticated reads, SDKs | Yes | Yes | `[AllowAnonymous]` |
| **Authenticated** | Any logged-in user; tenant- or user-scoped writes and privileged reads | Yes | Yes | `[Authorize]` (no roles required) |
| **Admin** | Operator / setup / diagnostics (tenant management, setup-secret flows, internal tooling) | No (`IgnoreApi = true` where applicable) | No | `[Authorize(Roles=...)]` or `[SetupSecretRequired]` |

**Enforcement:** `Event.Architecture.Tests/EndpointClassificationArchitectureTests.cs` asserts every non-abstract `ControllerBase` subclass (or every HTTP action method if no class-level attribute) carries an explicit `[EndpointClassification]`. Unclassified actions fail the build.

**Mechanism:** `[EndpointClassification]` metadata is read by `Explore.API/OpenApi/EndpointClassificationTransformer.cs` (`IOpenApiOperationTransformer`) and emitted as the `x-endpoint-class` operation extension in `/openapi/event-api.json`. LastOrDefault precedence — action-level attribute overrides controller-level.

**Known flags (to be resolved in future workstreams, NOT this plan):**
- `InstanceSettingsController` is declared `Authenticated` but runtime-checks `IsInstanceAdmin` — real classification is `Admin`. Strict attribute-based rules can't capture runtime checks.
- `TenantController` writes are declared `Authenticated` because zero `Roles=` attributes exist codebase-wide. Future: declarative role-based authorization pass.

---

## Current State

### Verified healthy (no change needed)
- **`IEventApiClient` wiring:** NSwag v14.6.3.0 generates `Explore.Blazor.Client/Clients/EventApiClient.g.cs`. Partials (`EventApiClient.cs`, `DtoPartials.cs`) are minimal and sound. Typed HttpClient on Server (base `https://localhost:7039/`) and WASM (self-origin via BFF).
- **Service layer:** 47 services. 34 use `IEventApiClient`, 12 use `IHttpClientFactory("BffClient")` intentionally, 3 state-only, 1 mixed (`ImageStorageService`). Zero raw-HttpClient injections. Zero orphans.
- **BFF/Auth:** YARP `/api/{**catchall}` with three server handlers (`AccessTokenForwardingHandler`, `TenantHeaderForwardingHandler`, `SetupSecretForwardingHandler`), two WASM handlers (`BrowserCredentialsMessageHandler`, `BffUnauthorizedHandler`). Cookie: HttpOnly, Secure, SameSite=Lax, 7-day sliding. Dynamic OIDC via `IDynamicAuthSchemeManager`.
- **Pages/Admin UI:** 35 routable pages, 7 admin pages under `/admin/*` all wired correctly.

### The defect
- **464 methods** in `EventApiClient.g.cs` match regex `\dAsync`.
- Verified pairs: `TenantDELETEAsync`/`TenantDELETE2Async`, `TenantGET2Async`/`TenantGET3Async`, `TenantPUT2Async`/`TenantPUT3Async`, `AuthProviderGETAsync`/`AuthProviderGET2Async`, `Status7Async`/`Status8Async`, `InternalAsync`/`Internal2Async`, `AuthProviderConfigurationAsync`/`AuthProviderConfiguration2Async`.
- **5 service wrappers** call suffixed variants directly: `UserService`, `EventRegistrationService`, `EventSeriesService`, `EventSessionAgendaItemService`, `OrganizationMemberService`.
- **Named-route controllers** (e.g. `ActorController`) produce a good canonical method _plus_ bad alias methods.
- **Unnamed-route controllers** (e.g. `TenantController`) collapse into `GET/GET2/GET3` soup even without aliases.

### Related plans
- **`dev/active/hateoas-client-alignment/`** — downstream. HAL link-policy fragility and `OrganizationDetails.razor.cs:103` pattern violation. Do not start until Phase 4 merged.

### Audit-derived backlog (rolled in, split cleanly between contract and UI)
**Contract-hygiene cleanups** (Phase 5A):
- Storage/SMTP/Localization test-connection endpoints — classify as Internal/Diagnostic or Authenticated Admin.
- Aspire plumbing / health / setup-secret controllers — classify and apply `IgnoreApi` where appropriate.

**Client-consumer cleanups** (Phase 5B):
- Confirm the regenerated client is mechanically clean (no hand edits, minimal partials, all wrapper services absorb churn).

**UI cleanups** (Phase 5C):
- `InstanceSettings.razor` — legacy redirect stub, delete candidate.
- `TenantPolicySettings.razor` — marked replaced, verify and delete if confirmed.
- `ImageStorageService` — SRP nit; split into `ImageApiService` + `ImageUploadService` if approved.

---

## Target (Future) State

1. **One canonical `operationId` per action** — unique, stable, human-readable (e.g. `Tenant_GetById`, `Tenant_Update`, `Tenant_Delete`).
2. **One operation per action in the exported OpenAPI document.**
3. **Zero URL-segment alias routes.** `/api/v0.1/...` is gone from the application (runtime and documentation). Runtime returns 404.
4. **Multi-reader versioning** — media-type (primary, REST-pure, HATEOAS-compatible) + query-string (`?api-version=0.1`, for webhooks) + custom-header (`X-Api-Version: 0.1`, for service-to-service). URL-segment banned. Permanent strategy (survives 1.0+). (See D2/D15 in context.md.)
5. **`EventApiClient.g.cs` contains zero methods matching `\dAsync`** — CI-asserted (Phase 0.2).
6. **Three-way consistency** between ASP.NET action metadata, canonical OpenAPI operations, and `IEventApiClient` — integration-tested.
7. **Every action classified** as Public / Authenticated / Admin — architecture-tested (`EndpointClassificationArchitectureTests`).
8. **Service wrappers use canonical names only** — 5 services updated.
9. **`hateoas-client-alignment`** work proceeds on top of the stable client.
10. **Schema-diff visibility in CI** — breaking schema changes surfaced even if not blocked.

---

## Guiding Language: Route Name vs Operation Id (CTO clarification)

These are **intentionally aligned, not inherently identical**:

- **Route Name** — routing/HAL identity. The thing `RouteNames.X` refers to. The thing `LinkGenerator` resolves.
- **Operation Id** — OpenAPI/client-contract identity. The thing NSwag turns into a method name.

Today they align via `[HttpGet(Name = "Tenant_GetById")]`. We codify this as **policy**, not **physics** — in a future where one must diverge from the other, the language is already in place.

---

## Implementation Phases

Ordered by **risk-down-first**: guardrails before surgery, contract before codegen, codegen before Blazor churn, client hygiene before UI hygiene.

### Phase 0 — Guardrails first (0.5 day)

**Goal:** Fail the build loudly if anyone reintroduces the defect, _before_ we start changing anything.

- **0.1** Add `Event.API.IntegrationTests/ContractInvariantsTests.cs`. Boots the API, requests `/openapi/event-api.json`, parses it, and asserts:
  - No duplicate `(method, path)` pairs.
  - Every operation has a non-null, non-empty `operationId`.
  - No two operations share the same `operationId`.
  - **No `operationId` matches placeholder/verb-only patterns** `^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)\d*$` or `^[A-Z][a-zA-Z]+(GET|POST|PUT|PATCH|DELETE)\d*$` (new, per CTO "ban placeholder operation names").
  - No path matches `^/api/v\d` (we are removing URL-segment aliases entirely).
  - **Acceptance:** All assertions RED today; turn green by end of Phase 3.
- **0.2** Add `Explore.Blazor.Client.Tests/ApiClientNamingTests.cs`. Reflects `IEventApiClient`'s public method list and asserts:
  - No method name matches `\dAsync$`.
  - No method name matches raw-verb patterns `^(Get|Post|Put|Patch|Delete)\d*Async$` or `^[A-Z][a-zA-Z]+(GET|POST|PUT|PATCH|DELETE)\d*Async$`.
  - **Acceptance:** RED today (464 matches); green by end of Phase 4.
- **0.3** Document the invariants in `docs/GOVERNANCE.md` under a new subsection "API Contract Rules" covering:
  - One canonical operation per action.
  - Unique explicit `operationId` everywhere.
  - No placeholder/verb-only operation names.
  - Runtime alias routes do not exist (deleted, not hidden).
  - Every action carries an endpoint class (Public / Authenticated Admin / Internal).
  - `swagger.json` is a governed artifact — changes require PR review.

### Phase 1 — Inventory & name every action (0.5 day)

**Goal:** Deterministic, unique, stable operationId for every action; **inventory generated, not hand-curated**.

- **1.1** **Generate** the inventory via an `ApiExplorer`-based generator target (not by hand). Add a `msbuild` target or a CLI entry point in `Explore.API` (e.g. `dotnet run --project Explore.API -- export-action-inventory`) that walks `IApiDescriptionGroupCollectionProvider` and writes `api-contract-stabilization-action-inventory.md` with columns: `Controller`, `ActionName`, `HttpVerb`, `RouteTemplate`, `CurrentName`, `ProposedOperationId`, `EndpointClass`, `RouteNamesConstant`.
  - **Acceptance:** Generator committed; inventory file produced by generator, not by hand; regenerable.
- **1.2** Define the naming policy in `docs/NAMING_CONVENTIONS.md`:
  - Format: `{ControllerShortName}_{ActionName}` PascalCase (e.g. `Tenant_GetById`).
  - Semantic distinguishers only: `_ByTenant`, `_ByUser`, `_Legacy`.
  - Ban: generic verbs (`Get`, `List`), numeric suffixes, controller-less names.
  - Collections vs single resources must be distinguishable (e.g. `Tenant_List` vs `Tenant_GetById`).
  - Mutation names reflect business action where meaningful (e.g. `Registration_Cancel` preferred over `Registration_Delete` if semantically different).
- **1.3** Detect policy-produced collisions in the inventory; resolve by semantic suffix.
  - **Acceptance:** Generator fails if duplicate proposed IDs appear.
- **1.4** Coordinate route names with `RouteNames` constants (non-inferable rule #20) — document the **Route Name vs Operation Id** distinction from the "Guiding Language" section.
  - Today: they align. Tomorrow: they may not. Policy allows both.
- **1.5** Assign an **endpoint class** to every action (Public / Authenticated Admin / Internal). Unclassified = architecture test fails.

### Phase 2 — Delete URL-segment routes, install multi-reader versioning (1 day)

**Goal:** Remove the `/api/v0.1/...` surface entirely, not just from OpenAPI. Install the locked multi-reader versioning strategy.

**Locked per user m0044:** Keep media-type versioning (primary, REST-pure, HATEOAS-compatible). Add `QueryStringApiVersionReader("api-version")` and `HeaderApiVersionReader("X-Api-Version")`. Delete URL-segment. This is a **permanent** strategy, not pre-1.0 temporary.

**Target `Program.cs` configuration:**
```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(0, 1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new MediaTypeApiVersionReader("v"),            // Accept: application/json;v=0.1
        new QueryStringApiVersionReader("api-version"),// ?api-version=0.1
        new HeaderApiVersionReader("X-Api-Version")    // X-Api-Version: 0.1
    );
});
```

Controllers carry single `[Route("api/[controller]")]`. No `[Route("api/v{version:apiVersion}/[controller]")]`. No dual-route decoration.

- **2.1** Confirm the active OpenAPI pipeline. **RESOLVED during Phase 1 prep:** native .NET 10 OpenAPI (`builder.Services.AddOpenApi("event-api", …)` in `Program.cs:159`). NOT Swashbuckle (incompatible with .NET 10's Microsoft.OpenApi 2.x per `OpenApiExportService.cs:69` comment). Endpoint: `/openapi/event-api.json`.
- **2.2** **Kill the URL-segment alias at source.** Remove the cloning behaviour in `VersionedRouteConvention` / `ApiVersioningExtensions` so `/api/v0.1/...` routes are no longer registered at all.
  - **Acceptance:** `GET /api/v0.1/tenant/{id}` returns 404. `GET /api/tenant/{id}` returns 200. Exported OpenAPI contains no `^/api/v` paths. `ContractInvariantsTests` turns green on URL-segment assertion.
- **2.3** Remove `VersionedRouteConvention.cs` from application startup. If no other caller uses it, delete the class entirely (**report as deletion candidate first per shell rules — awaiting explicit user approval**).
- **2.4** Wire the multi-reader versioning configuration above in `Program.cs` via `ApiVersioningExtensions`.
  - **Acceptance:** `curl -H "Accept: application/json;v=0.1" /api/tenant` returns 200. `curl "/api/tenant?api-version=0.1"` returns 200. `curl -H "X-Api-Version: 0.1" /api/tenant` returns 200. `curl /api/v0.1/tenant` returns 404.
- **2.5** Rewrite `docs/ARCHITECTURE.md` line 57 to reflect the multi-reader strategy (not the old dual-versioning decision).
- **2.6** Fallback path (if 2.2 cascades unexpectedly): set `ApiExplorerSettings.IgnoreApi = true` on the clones. Use only if deletion proves infeasible.
- **2.7** Tertiary fallback (if both 2.2 and 2.6 blocked): add `IDocumentTransformer` in `OpenApiExportService` filtering `^/api/v\d` paths before emit.

### Phase 3 — Stable operationIds on every action (1 day)

**Goal:** Every operation in the canonical document has a unique, stable, explicit `operationId`.

- **3.1** Apply proposed operationIds per Phase 1 inventory. Preferred: `[HttpGet("{id:guid}", Name = "Tenant_GetById")]` — doubles as the `RouteNames` constant target. Fallback if Name is already taken by another mechanism: `[EndpointName("Tenant_GetById")]`.
- **3.2** Startup-time invariant check via `IOperationTransformer` / document transformer — throws in development if any operation is emitted without an `operationId` or matches a banned placeholder pattern.
- **3.3** Update / create matching `RouteNames.*` constants.
- **3.4** Apply endpoint-class metadata. Internal/diagnostic controllers get `[ApiExplorerSettings(IgnoreApi = true)]`. Public/Authenticated Admin controllers get explicit metadata.
- **3.5** Regenerate the canonical OpenAPI document locally; visually diff for sanity.

### Phase 4 — Regenerate `IEventApiClient` once (0.5 day)

**Goal:** Kill the 464 suffixed methods in one regeneration.

- **4.1** Refresh `Explore.API/swagger.json` from the now-clean runtime endpoint.
- **4.2** Run NSwag regeneration for `Explore.Blazor.Client/nswag.json`. Keep `operationGenerationMode: "SingleClientFromOperationId"` — it will now work correctly.
- **4.3** Inspect `EventApiClient.g.cs` diff. Expected: large rename block, zero structural logic change.
- **4.4** Run `dotnet build --configuration Release --verbosity quiet`. Expect compile failures in the 5 identified service wrappers.
- **4.5** Fix compile errors by **renaming calls only** in:
  - `UserService`, `EventRegistrationService`, `EventSeriesService`, `EventSessionAgendaItemService`, `OrganizationMemberService`.
- **4.6** `lsp_diagnostics` clean on each edited service file.

### Phase 5A — Contract-surface hygiene (0.25 day, strategic priority)

**Goal:** Every remaining API action deliberately classified and visible/invisible accordingly. No accidental surface.

- **5A.1** Classify every action per Phase 1.5. Apply `IgnoreApi` where Internal/Diagnostic. Confirm Authenticated Admin actions carry `[Authorize(Roles=...)]`.
- **5A.2** Audit the surviving OpenAPI document — manual or diff-based review. Every listed endpoint is deliberately public or admin; no bootstrap, probe, or test-connection endpoint leaks through.
- **5A.3** Decision: Storage/SMTP/Localization **test-connection** endpoints. Default: classify as Authenticated Admin (admin UI buttons light up) since they legitimately serve the admin workflow. Alternative: Internal if we decide the admin UI should trigger them via a separate BFF endpoint. User decision required.

### Phase 5B — Client-consumer hygiene (0.25 day)

**Goal:** Regenerated client is mechanically replaceable and developer-ergonomic.

- **5B.1** Confirm no hand edits in `EventApiClient.g.cs`. Confirm partials (`EventApiClient.cs`, `DtoPartials.cs`) are minimal.
- **5B.2** Confirm every wrapper service absorbs rename churn — no raw client calls in pages/components.
- **5B.3** Add the **generated-client ergonomics bar** to `docs/GOVERNANCE.md`:
  - No verb-only method names (`GETAsync`, `POST2Async`).
  - Collection vs single-resource distinguishable (`Tenant_List` → `TenantListAsync`, `Tenant_GetById` → `TenantGetByIdAsync`).
  - Mutation names reflect business action where business action ≠ HTTP verb.
- **5B.4** Add one **generated-client smoke test** in `Explore.Blazor.Client.Tests` that instantiates `IEventApiClient` against a test server (using the existing `Event.API.IntegrationTests` infrastructure or a new minimal host) and calls a representative subset (e.g. 1 GET collection, 1 GET by id, 1 POST, 1 PUT, 1 DELETE) to prove runtime compatibility, not just naming.

### Phase 5C — UI/Blazor cleanups (0.25 day)

**Goal:** Finish the Blazor audit backlog. Lower strategic priority than 5A/5B.

- **5C.1** Delete `InstanceSettings.razor` (legacy redirect). Check for inbound links. **Report as deletion candidate before actual removal** per shell rules.
- **5C.2** Investigate `TenantPolicySettings.razor`. If replaced, report for deletion. If kept, add ABOUTME comment explaining why.
- **5C.3** Decide on `ImageStorageService` SRP split. If approved: split into `ImageApiService` (typed-client) + `ImageUploadService` (BffClient multipart); retire mixed service. If deferred: add a citation comment.

### Phase 6 — Fold `hateoas-client-alignment` in (0.5 day)

**Goal:** Downstream HAL client-consumption work ships on the stable client.

- **6.1** Read `dev/active/hateoas-client-alignment/hateoas-client-alignment-context.md`; confirm scope unchanged.
- **6.2** Add a header to `dev/active/hateoas-client-alignment/hateoas-client-alignment-plan.md`: "**Parent plan:** `dev/active/api-contract-stabilization/`. **Do not start before:** Phase 4 merged."
- **6.3** Execute the HAL plan's tasks there (`OrganizationDetails.razor.cs:103` fix, per-type `HasHalLink()` extensions, missing collection-link policies). Tracked there, not here.

### Phase 7 — Verification & contract governance (0.75 day)

**Goal:** Prove it stays fixed; install schema-diff visibility; formalize forward policy.

- **7.1** CI wiring — Phase 0 guardrail tests enforced on every push. Generator from 1.1 runs in CI; inventory file drift (regenerated vs committed) fails the build.
- **7.2** Run the full `CLAUDE.md` test matrix individually (all 8 projects).
- **7.3** Manual smoke via Aspire AppHost — every admin page under `/admin/*`, save/load confirmed.
- **7.4** Add `Event.Architecture.Tests/ApiContractArchitectureTests.cs`. Asserts:
  - Every controller carrying `[ApiVersion]` (if any survive Phase 2.5) has explicit route names on all actions OR documented exemption.
  - Every action is reachable with a unique operation identity.
  - Every action is classified (Public / Authenticated Admin / Internal).
  - Every new controller follows the forward policy: explicit route template, explicit name, explicit response typing, explicit OpenAPI inclusion/exclusion.
- **7.5** Add **schema-diff visibility** in CI. Compare the OpenAPI document from HEAD against the OpenAPI document from `main` (or previous commit). Emit a visible job summary with added/removed/changed operations and schemas. **Do not block** the build today — we are pre-1.0 and break freely. When we hit 1.0, flip to blocking.
- **7.6** Publish the **forward controller-authoring standard** in `docs/QUICK_REFERENCE.md`:
  - Every new controller action must have an explicit route template.
  - Every new controller action must have an explicit route name or endpoint name.
  - Every new controller action must declare its endpoint class (Public / Authenticated Admin / Internal).
  - Every new controller action must have an explicit response type.
  - No overloaded semantic ambiguity — one action, one responsibility, one name.

---

## Success Metrics

| Metric | Baseline (today) | Target |
|---|---|---|
| `EventApiClient.g.cs` methods matching `\dAsync` | 464 | 0 |
| `EventApiClient.g.cs` methods matching verb-only patterns | Many | 0 |
| Canonical OpenAPI duplicate `(method,path)` pairs | ≥71 | 0 |
| Operations missing explicit `operationId` | Many | 0 |
| Operations matching placeholder `operationId` pattern | Many | 0 |
| URL-segment alias routes at runtime | ~71 | 0 |
| Service wrappers calling suffixed client methods | 5 | 0 |
| Unclassified API actions | All | 0 |
| `hateoas-client-alignment` ship-blocked | Yes | No |
| Architecture tests enforcing contract rules | 0 | 2 new + 1 schema-diff visibility job |
| Generated-client runtime smoke coverage | None | ≥1 representative call per HTTP verb |

---

## Timeline

- Phase 0: 0.5d
- Phase 1: 0.5d
- Phase 2: 1.0d (increased scope — deletion not just hiding)
- Phase 3: 1.0d
- Phase 4: 0.5d
- Phase 5A: 0.25d
- Phase 5B: 0.25d
- Phase 5C: 0.25d
- Phase 6: 0.5d
- Phase 7: 0.75d
- **Total: ~5.5 days of focused work, 3+ days minimum.**

---

## Potential Risks & Unknowns

**Risk: removing `/api/v0.1/...` breaks an unknown consumer.** Pre-1.0, no confirmed external consumers, but sleeper scripts or bookmarked integrations may exist. _Mitigation:_ Phase 2.4 escalation gate. If discovered, fall back to time-boxed transitional alias (one release) or separate OpenAPI document — never steady-state.

**Risk: removing media-type versioning (Phase 2.5) breaks clients pinning `Accept: application/json;v=0.1`.** _Mitigation:_ grep repo for any such usage (Blazor, tests, docs). If absent and no external consumer, remove. Escalate before executing.

**Risk: `VersionedRouteConvention` deletion cascades into unexpected breakage.** Other `[ApiVersion]` consumers, middleware assumptions, tests depending on alias paths. _Mitigation:_ Phase 2.2 build-and-test after each deletion step. Full test matrix before merging.

**Risk: route-name collisions inside `RouteNames`.** Some controllers already have explicit route names. _Mitigation:_ Phase 1 inventory generator detects collisions up front; semantic suffix resolves them.

**Risk: inventory generator becomes a maintenance burden.** Writing a small reflection tool is cheap; keeping it compiling over time requires discipline. _Mitigation:_ it is a single CLI entry point in `Explore.API`; covered by the Phase 7.1 CI wiring so drift is detected immediately.

**Risk: Swagger UI regression.** Removing routes may confuse users with cached bookmarks. _Mitigation:_ we are pre-1.0; document the change in `docs/GOVERNANCE.md`; the new Swagger UI is objectively cleaner.

**Risk: HAL alignment fix dependency on named routes.** `hateoas-client-alignment` may prefer specific route names. _Mitigation:_ Phase 1 naming policy coordinated with the HAL plan's expectations.

**Risk: checked-in `swagger.json` drift between developers.** Today it drifts silently. _Mitigation:_ Phase 0.1 + Phase 7.5 schema-diff visibility job surface any drift at PR time.

**Risk: missing operationIds on endpoints introduced by Aspire / health / setup-secret controllers.** These may not follow our policy. _Mitigation:_ Phase 1 inventory is exhaustive; these get either Internal classification with `IgnoreApi` or canonical IDs.

**Unknown: whether native OpenAPI (.NET 10) or Swashbuckle is the active pipeline.** _Mitigation:_ Phase 2.1 confirms before choosing implementation path.

**Unknown: whether external consumers (PWA, mobile, partner) depend on `/api/v0.1/...` URLs or the media-type version.** _Mitigation:_ Phase 2.4 + 2.5 escalation gates; user consultation before execution.

---

## Out of scope (explicit non-goals)

- Any Domain or Application layer change — this plan is pure API contract + Blazor client fallout.
- New features, new endpoints, new DTOs.
- Performance work.
- Authentication/authorization changes (beyond classification metadata).
- Test coverage beyond contract invariants, the architecture tests, and one client smoke test specified.
- Blocking schema-diff gate (pre-1.0; visibility only).

---

## Files most likely to change (non-exhaustive)

**API layer:**
- `Explore.API/Controllers/*.cs` (Phase 1, 3 — route names, operation IDs, endpoint class metadata)
- `Explore.API/Extensions/ApiVersioningExtensions.cs` (Phase 2.2, 2.5)
- `Explore.API/VersionedRouteConvention.cs` (Phase 2.2 — likely deleted)
- `Explore.API/Services/OpenApiExportService.cs` (Phase 3.2 invariant check)
- `Explore.API/swagger.json` (Phase 4.1 — refreshed)
- New: `Explore.API/Tools/ActionInventoryExporter.cs` (Phase 1.1 — generator)

**Blazor client:**
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs` (Phase 4.2 — regenerated)
- `Explore.Blazor.Client/Services/{UserService,EventRegistrationService,EventSeriesService,EventSessionAgendaItemService,OrganizationMemberService}.cs` (Phase 4.5)

**Tests:**
- `Event.API.IntegrationTests/ContractInvariantsTests.cs` (Phase 0.1 — new)
- `Explore.Blazor.Client.Tests/ApiClientNamingTests.cs` (Phase 0.2 — new)
- `Explore.Blazor.Client.Tests/GeneratedClientSmokeTests.cs` (Phase 5B.4 — new)
- `Event.Architecture.Tests/ApiContractArchitectureTests.cs` (Phase 7.4 — new)

**Docs:**
- `docs/GOVERNANCE.md` (Phase 0.3, 5B.3 — API Contract Rules + client ergonomics bar)
- `docs/NAMING_CONVENTIONS.md` (Phase 1.2 — operationId naming policy + route-name vs operation-id language)
- `docs/QUICK_REFERENCE.md` (Phase 7.6 — forward controller-authoring standard)
- `docs/ARCHITECTURE.md` line 57 (Phase 2.5 — revise dual-versioning entry; likely to "single unversioned contract, pre-1.0")

**UI:**
- `Explore.Blazor/Components/Pages/Admin/InstanceSettings.razor` (Phase 5C.1 — delete candidate)
- `Explore.Blazor/Components/Pages/Admin/TenantPolicySettings.razor` (Phase 5C.2 — investigate/delete)

---

## Files to delete (candidates — awaiting explicit user approval per shell rules)

- `Explore.Blazor/Components/Pages/Admin/InstanceSettings.razor` — legacy redirect only.
- `Explore.Blazor/Components/Pages/Admin/TenantPolicySettings.razor` — if Phase 5C.2 confirms replacement.
- `Explore.API/VersionedRouteConvention.cs` — if Phase 2.2 removes the alias strategy entirely (no other callers).

---

## CTO Review Changes (Changelog)

Revisions applied 2026-04-19 based on senior review:

1. **D2 → D2-revised.** Removed the preference for retaining media-type versioning. New default: strip to a single unversioned contract pre-1.0 unless a consumer is proven.
2. **Phase 2 → deletion not hiding.** URL-segment aliases removed at source (`VersionedRouteConvention` change or deletion), not filtered from the OpenAPI document. Phase 2.5 added to consider removing media-type versioning entirely.
3. **Endpoint Classification** added as first-class concept. Every action declares Public / Authenticated Admin / Internal. Enforced by architecture test.
4. **Schema-diff visibility** added (Phase 7.5). CI surfaces additions/removals/changes without blocking pre-1.0.
5. **Generated-client ergonomics bar** added (Phase 5B.3). Bans verb-only names, mandates collection vs single-resource distinguishability, mandates business-action mutation names.
6. **Banned placeholder operation names** added to Phase 0.1 assertions and Phase 3.2 startup check (`GET`, `POST`, `TenantGET2`, etc.).
7. **Action inventory generated, not hand-curated** (Phase 1.1). Added CLI generator target; CI detects drift.
8. **Phase 5 split** into 5A (contract hygiene, highest strategic priority), 5B (client hygiene, mid-strategic), 5C (UI hygiene, lowest-strategic).
9. **Generated-client smoke test** added (Phase 5B.4). One real runtime call per HTTP verb against a test server.
10. **Forward controller-authoring standard** added (Phase 7.6). Written policy in `docs/QUICK_REFERENCE.md` for all new controllers.
11. **Route Name vs Operation Id** framed as intentionally aligned, not inherently identical (new "Guiding Language" section + D6-revised in context.md).
12. **Contract ownership language** added to Strategic Frame: OpenAPI is a governed product artifact, not a generated side-effect.

---

## Resume instructions for future sessions

1. Read this plan end-to-end.
2. Read `api-contract-stabilization-context.md` for current progress.
3. Read `api-contract-stabilization-tasks.md` for the live checklist.
4. Re-read `docs/ARCHITECTURE.md` line 57 (dual-versioning decision — note: subject to Phase 2.5 rewrite) and `docs/QUICK_REFERENCE.md` rule 20 (named routes must match route constants).
5. Do NOT start work until Phase 0 guardrail tests exist — they are the safety net.
6. If Phase 2 surfaces an unknown consumer, **escalate before deleting**.
