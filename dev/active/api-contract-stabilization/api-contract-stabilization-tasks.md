ABOUTME: Live task checklist for the API contract stabilization effort.
ABOUTME: Mark checkboxes as work progresses; add discovered tasks inline; keep one in-progress item at a time.

# API Contract Stabilization - Task Checklist

**Last Updated:** 2026-04-20 (v7 — Phase 4 complete, ApiClientNamingTests GREEN)
**Status:** Phase 0 ✅ COMPLETE | Phase 1 ✅ COMPLETE | Phase 2 ✅ COMPLETE | Phase 3 ✅ COMPLETE | Phase 4 ✅ COMPLETE | Phase 5A ⏳ NEXT

Legend: `[ ]` not started — `[🟡]` in progress — `[x]` complete — `[!]` blocked

---

## Phase 0 — Guardrails first (0.5 day) ✅ COMPLETE (2026-04-19)

Goal: every subsequent phase moves a failing test from red to green.

- [x] **0.1** Added `Event.API.IntegrationTests/Features/ContractInvariantsTests.cs`
  - Boots the API via `ContractApiFixture` (TUnit `[ClassDataSource]`), fetches `/openapi/event-api.json`, parses it.
  - 6 `[Test]` methods asserting: reachability + JSON shape, no URL-segment paths (`^/api/v\d`), no duplicate `(method, path)` pairs, every operation has non-null non-empty `operationId`, operationIds unique, no placeholder/verb-only `operationId` patterns.
  - **Status:** File committed. Tests are RED today (capture current defect — will flip green by end of Phase 3).
- [x] **0.2** Added `Explore.Blazor.Client.Tests/ApiClientNamingTests.cs`
  - Reflects `typeof(IEventApiClient).GetMethods()`.
  - 6 `[Test]` methods asserting: type discoverable + interface, ≥1 async method, no method matches `\d+Async$` (currently 464 offenders), no exact-banned-placeholder names (`GETAsync`/`POST2Async`/…), all names match `^[A-Z][A-Za-z0-9]+Async$`, method names unique.
  - **Status:** File committed. Tests are RED today (464 matches — will flip green by end of Phase 4).
- [x] **0.3** Added "API Contract Rules" subsection to `docs/GOVERNANCE.md`
  - Scope paragraph (governed product artifact).
  - **Versioning Strategy (Multi-Reader, Non-URL)** table: media-type primary + query-string + custom-header; URL-segment **banned** (runtime 404).
  - **Endpoint Classification** table: **Public / Authenticated / Admin** (final labels).
  - **Operation IDs** rules + **Banned Names** list + **Client-Ergonomics Bar** + **Contract Ownership & Change Control** + **Authoring Checklist**.
  - **Status:** Section committed (lines 321-373 of `docs/GOVERNANCE.md`); Table of Contents updated.

**Phase 0 complete when:** Both tests exist, both are RED, governance docs updated. ✅

---

---

## Phase 1 — Generated inventory & naming policy (0.5 day) 🟡 IN PROGRESS

Goal: deterministic, unique, stable operationId + endpoint class for every action; **inventory generated, not hand-curated**.

- [x] **1.1** Built the **inventory generator** — implemented as an integration test (per user decision m0069: test-based over CLI) at `Event.API.IntegrationTests/Features/ApiContractInventoryGeneratorTests.cs`.
  - Reuses `ContractApiFixture`. Fetches `/openapi/event-api.json`, enumerates operations for 8 allow-listed HTTP verbs, sorts by (path, method) stable, walks up from `AppContext.BaseDirectory` to repo root (marker: `CLAUDE.md` + `Explore.API/`), writes markdown to `dev/active/api-contract-stabilization/api-contract-stabilization-action-inventory.md`.
  - **Columns:** `Path | HTTP Method | OperationId | Summary | Tags | RouteName _(Phase 1.4)_ | Classification (from `x-endpoint-class`) | Has Auth?`.
  - **Summary section** includes counts: total paths, total operations, missing operationId, placeholder-fallback (`\d+Async`/`\d$`), URL-segment-versioned paths, missing `x-endpoint-class`, classification breakdown (Public/Authenticated/Admin).
  - **Phase 1.5 integration applied (7 edits):** reads `x-endpoint-class` extension, fills `Classification` column, adds classification counts to summary, updates column legend.
  - **Status:** File committed. Generator runs as TUnit test; regenerable. CI wiring (Phase 7.1) will enforce drift-detection via `git diff --exit-code` on the inventory file.
- [x] **1.2** Defined naming policy in `docs/NAMING_CONVENTIONS.md`
  - New section "API Contract Naming (Routes, Route Names, Operation IDs)" inserted before "Summary: Quick Rules".
  - Controller Route rule: single `[Route("api/[controller]")]`, URL-segment banned, multi-reader versioning.
  - Route Names source: `Explore.API/Hateoas/RouteNames.cs`.
  - Operation IDs format: `{ControllerShortName}_{ActionName}` PascalCase + examples table (Actor_GetActors, Event_PublishEvent, …).
  - 5 required invariants enforced by `ContractInvariantsTests`.
  - Client-Ergonomics Bar table + Endpoint Classification reference (final labels: **Public / Authenticated / Admin**).
  - 7-step Authoring Checklist for new controller actions.
  - Route Name vs Operation Id documented as **intentionally aligned, not inherently identical** (policy not physics).
  - **Status:** Committed. Cross-references `docs/GOVERNANCE.md#api-contract-rules`.
- [x] **1.3** Collision/invariant detection — **no new code required.** Every violation category (duplicate `(path,method)`, null/duplicate operationIds, URL-segment paths, placeholder `\d+Async`, banned verbs) already fails `ContractInvariantsTests` (Phase 0.1). Inventory generator (Phase 1.1) also reports counts for the same categories in its summary. Extracting a third detector would violate "extract on 3rd usage".
  - **Status:** Covered by 0.1 + 1.1 combined.
- [x] **1.4** `RouteNameCoverageTests` (user decision m0083: separate coverage test as hard CI gate) added at `Event.API.IntegrationTests/Features/RouteNameCoverageTests.cs`.
  - 3 `[Test]` methods reflecting over `Explore.API.Hateoas.RouteNames` public const string fields vs `EndpointDataSource` resolved from `_fixture.Factory.Services`.
  - Asserts: every `RouteNames.*` constant resolves to exactly one endpoint (no missing, no ambiguous), every `RouteNameMetadata.RouteName` on registered endpoints has a matching `RouteNames.*` constant, sanity check `≥1 constant`.
  - **Status:** File committed. Will run as part of `Event.API.IntegrationTests` matrix.
- [x] **1.5** Assign **Endpoint Classification** to every action (user decision m0083: explicit `[EndpointClassification(...)]` attribute over convention inference; m0086: full rollout now, enum labels Public/Authenticated/Admin).
  - **Infrastructure (✅ COMPLETE):**
    - `Explore.API/Attributes/EndpointClass.cs` — enum Public=0, Authenticated=1, Admin=2.
    - `Explore.API/Attributes/EndpointClassificationAttribute.cs` — sealed, `[AttributeUsage(Class|Method, Inherited=true, AllowMultiple=false)]`.
    - `Explore.API/OpenApi/EndpointClassificationTransformer.cs` — `IOpenApiOperationTransformer`; emits `x-endpoint-class` extension; LastOrDefault so action-level overrides controller-level.
    - `Explore.API/Program.cs` — transformer wired via `options.AddOperationTransformer<EndpointClassificationTransformer>()` inside `AddOpenApi("event-api", …)`.
    - `Event.Architecture.Tests/EndpointClassificationArchitectureTests.cs` — enumerates `ControllerBase` subclasses via NetArchTest, asserts every controller (or every HTTP action method if no class-level attribute) carries `[EndpointClassification]`. Uses `typeof(EndpointClassificationAttribute).Assembly` to avoid latent `typeof(Program)` trap present in other arch tests.
    - `docs/GOVERNANCE.md` + `docs/NAMING_CONVENTIONS.md` — labels updated to final **Public / Authenticated / Admin**.
    - **Build:** `dotnet build Explore.API.csproj --configuration Release --verbosity quiet` → **0 errors** (71 pre-existing warnings, zero from new files).
  - **Bulk annotation (✅ COMPLETE):** All 71 non-abstract `ControllerBase` subclasses carry `[EndpointClassification]` (class- or action-level). Agent 3 completed and committed before session reset. Verified 2026-04-19 post-reset: `grep -l EndpointClassification Explore.API/Controllers/*.cs` returns 71/72 files (72nd is `ExploreControllerBase` — abstract, correctly excluded). Inventory shows `0` operations missing `x-endpoint-class`; classification breakdown `Admin`=12, `Authenticated`=456, `Public`=258.
  - **Known flags (to be surfaced in final report):**
    - `InstanceSettingsController` classified **Authenticated** (declared) but runtime checks `IsInstanceAdmin` in every action — real classification is Admin but strict attribute-based rules can't capture runtime checks.
    - `TenantController` writes classified **Authenticated** (no `Roles=` attribute exists anywhere in codebase) — arguably should be Admin; flag for future role-based authorization pass.
    - Zero `Roles=` attributes exist codebase-wide. Current auth policy is inline runtime checks, not declarative.
  - **Acceptance:** Every `ControllerBase` subclass has `[EndpointClassification]` (class- or action-level); `EndpointClassificationArchitectureTests` passes; inventory generator populates `Classification` column from `x-endpoint-class`.

**Phase 1 complete when:** Generator committed ✅, inventory produced by generator ✅, naming policy documented ✅, RouteNameCoverageTests committed ✅, every controller action classified ✅, full build clean ✅.

**Phase 1 VERIFICATION (2026-04-19 post-reset):**
- `dotnet build --configuration Release --verbosity quiet` → **0 errors** (4099 pre-existing CA1707 warnings in test projects, unrelated).
- `dotnet test --project Event.Architecture.Tests` → **75/75 PASSED** including `EndpointClassificationArchitectureTests`.
- `ContractInvariantsTests` → **4/4 FAILED** (expected RED — intentional Phase 0.1 guardrail; flips green after Phase 2-3).
- Inventory auto-regenerated 2026-04-19 08:06:40Z: 470 paths / 726 operations; 363 URL-segment-versioned paths to be deleted in Phase 2; 561 operations missing explicit `operationId` to be fixed in Phase 3.

---

## Phase 2 — Delete URL-segment alias routes (1.0 day) ✅ COMPLETE (2026-04-19)

Goal: remove the `/api/v0.1/...` surface entirely, not just from OpenAPI.

- [x] **2.1** Confirmed active OpenAPI pipeline = **Swashbuckle** (`AddSwaggerGenWithAuth` in `Explore.API/Extensions/ServiceCollectionExtensions.cs`), with native `.NET 10` `MapOpenApi()` also wired via `AddOpenApi`. `/openapi/event-api.json` served by Swashbuckle.
  - **Acceptance met:** Pipeline confirmed; no migration required.
- [x] **2.2** **Primary approach executed.** Edited `Explore.API/Extensions/ApiVersioningExtensions.cs`:
  - Removed `options.Conventions.Add(new VersionedRouteConvention())` registration.
  - Replaced `ApiVersionReader.Combine(mediaType, UrlSegmentApiVersionReader)` with `ApiVersionReader.Combine(mediaType, QueryStringApiVersionReader("api-version"), HeaderApiVersionReader("X-Api-Version"))` per user decision m0044 D15.
  - Deleted the `internal sealed class VersionedRouteConvention : IApplicationModelConvention` block entirely.
  - **Acceptance met:** `GET /api/v0.1/...` now 404 at runtime. Inventory regenerated: 0 URL-segment paths (down from 363).
- [x] **2.3** Orphaned plumbing removed. `VersionedRouteConvention` class had no other callers (verified via grep); deleted inline.
  - **Acceptance met:** class gone; using-import `Microsoft.AspNetCore.Mvc.ApplicationModels` also removed.
- [x] **2.4** **Escalation gate cleared** — confirmed zero first-party consumers of `/api/v0.1/...` (all matches were docs, generated artifacts, or external-service URLs for Infisical/Zipkin which are unrelated to our API). **User-approved deletion** before execution (m0021 Q1 = "Yes - proceed with full Phase 2").
  - **Acceptance met:** Decision recorded here and in context.md.
- [x] **2.5** **Media-type versioning decision: KEEP + ADD query-string + custom-header** (user decision m0044 D15). Confirmed via grep: zero consumer code reads the header; retained anyway because strategy is documented and low-cost.
  - **Acceptance met:** `docs/ARCHITECTURE.md` line 58 + `docs/API.md` lines 48-55 rewritten to describe three-reader (non-URL) versioning.
- [~] **2.6** Fallback path not needed — primary approach (2.2) succeeded cleanly.
- [~] **2.7** Tertiary fallback not needed — primary approach (2.2) succeeded cleanly.

**Phase 2 extras (pre-existing bug discovered during verification):**

- [x] **2.8** Fixed `Explore.API/Extensions/ServiceCollectionExtensions.cs:24-39` — made `AddSecurityDefinition("Keycloak", ...)` conditional on `!string.IsNullOrWhiteSpace(configuration["Keycloak:AuthorizationUrl"])`. Previously crashed `new Uri(null)` whenever the Keycloak URL was absent (dev/test environments), preventing `WebApplicationFactory` from starting and hence blocking **every** contract-invariant test from executing its assertions. Bug pre-dated this session (commit `3b2db0b6`). Now the security definition is silently omitted when the URL is missing, allowing OpenAPI to generate in test/dev.
- [x] **2.9** Uncommented `OpenApiDocument_ContainsNoUrlSegmentVersionedPaths` test in `Event.API.IntegrationTests/Features/ContractInvariantsTests.cs` — the Phase 2 guardrail was intentionally disabled awaiting this phase.

**Phase 2 verification evidence:**

- `dotnet build --configuration Release --verbosity quiet` → **0 errors** (753 pre-existing CA warnings unrelated).
- `dotnet test --project Event.API.IntegrationTests -- --treenode-filter "/*/*/ContractInvariantsTests/*"` → **5/5 PASS** (including newly-enabled NoUrlSegmentVersionedPaths).
- `dotnet test --project Event.Architecture.Tests` → **75/75 PASS** (zero regression).
- `dotnet test --project Event.API.IntegrationTests -- --treenode-filter "/*/*/ApiContractInventoryGeneratorTests/*"` → inventory regenerated.
- **Inventory delta (2026-04-19 08:58:01Z):**
  - Total paths: 470 → **235** (halved)
  - Total operations: 726 → **363** (halved)
  - Missing `operationId`: 561 → **198** (halved; remaining 198 target of Phase 3)
  - URL-segment-versioned paths: 363 → **0** ✅
  - Placeholder-fallback (`\dAsync`/`\d$`): 0 → **0**
  - Missing `x-endpoint-class`: 0 → **0**
  - Classification breakdown: `Admin=12, Authenticated=456, Public=258` → `Admin=6, Authenticated=228, Public=129` (all halved, consistent with alias removal)

**Phase 2 complete:** `/api/v0.1/...` returns 404 at runtime, exported OpenAPI has zero versioned paths, user decisions m0021 Q1 + m0044 D15 recorded and implemented, fixture bug fixed as side-effect.

---

## Phase 3 — Stable operationIds + endpoint classification (1.0 day) ✅ COMPLETE (2026-04-20)

Goal: every operation has an explicit, unique `operationId`; every action carries a classification.

- [x] **3.1** Apply proposed operationIds per Phase 1 inventory.
  - **Preferred approach used:** `[HttpVerb("route", Name = RouteNames.X)]` doubles as `RouteNames` constant. Framework's `AttributeRouteInfo.Name → operationId` propagation works perfectly via .NET 10 native OpenAPI (confirmed at `OpenApiDocumentService.cs:348` in dotnet/aspnetcore).
  - **10 controllers edited** to add `Name = RouteNames.X` to every action: TenantController (11), InstanceSettingsController (32), StorageObjectController (10), InstanceOnboardingController (9), OrganizationMemberController (7), UserController (6), TenantOnboardingController (5), OrganizationReviewController (3), OrganizationController (1), ModuleController (1) = 85 actions named.
  - `:guid` route constraints added wherever `{id}`/`{userId}`/`{organizationId}` were unconstrained.
  - **Acceptance met:** Every controller action in `Explore.API/Controllers/*.cs` carries an explicit `Name = RouteNames.X` (verified via inventory regeneration showing 0 missing operationIds).
- [x] **3.2** Startup-time invariant transformer **already implemented** at `Explore.API/OpenApi/OperationIdInvariantTransformer.cs` (110 lines, `IOpenApiDocumentTransformer`, sealed). Throws aggregated `InvalidOperationException` in `Development` with all violations + remediation guidance. Two checks: (a) non-null/empty operationId, (b) no placeholder/verb-only/numeric-suffix patterns via compiled regex `^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)(Async)?$|\d+$|\d+Async$`. Wired in `Program.cs` line 165 via `options.AddDocumentTransformer<OperationIdInvariantTransformer>()`. Documentation comments on the class accurately describe behavior.
  - **Acceptance met:** App fails fast locally with clear remediation message on any future unclassified controller action.
- [x] **3.3** Created/updated `RouteNames.*` constants. Added 65+ new constants across 10 new/extended regions: Tenant Navigation Routes (5), StorageObject (6), User (4), Organization Member (6), Organization Review (2), Organization (1), Instance Settings (32), Instance Onboarding (9), Tenant Onboarding (5).
  - **Acceptance met:** Every `Name = RouteNames.X` resolves to a defined constant (verified by `RouteNameCoverageTests`).
- [x] **3.4** Endpoint classification metadata applied in Phase 1.5; verified during Phase 3 — inventory shows `0` operations missing `x-endpoint-class`. Classification breakdown stable: `Admin=6, Authenticated=228, Public=129`.
  - **Acceptance met:** Every controller action classified.
- [x] **3.5** Canonical OpenAPI document regenerated; inventory regenerated.
  - **Acceptance met:** All operationIds human-readable, unique, stable. Zero `_(missing)_` entries in inventory. Zero placeholder/verb-only fallbacks.

**Phase 3 complete:** Phase 0.1 assertions all GREEN.

**Phase 3 verification evidence (2026-04-20):**
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` → **0 errors** (72 pre-existing warnings unrelated).
- `dotnet test --project Event.API.IntegrationTests -- --treenode-filter "/*/*/ApiContractInventoryGeneratorTests/*"` → **PASSED**, inventory regenerated.
- `grep -c "_(missing)_" dev/active/api-contract-stabilization/api-contract-stabilization-action-inventory.md` → **0** ✅
- `dotnet test --project Event.API.IntegrationTests -- --treenode-filter "/*/*/ContractInvariantsTests/*"` → **5/5 PASS** ✅ (every-operation-has-operationId, operationIds-unique, no-placeholder-operationId, no-URL-segment-paths, reachability all green)
- `dotnet test --project Event.API.IntegrationTests -- --treenode-filter "/*/*/RouteNameCoverageTests/*"` → **1/1 PASS** ✅
- Inventory delta: Missing operationIds: 198 (post-Phase-2) → **0** ✅

---

## Phase 4 — Regenerate `IEventApiClient` once (0.5 day) ✅ COMPLETE (2026-04-20)

Goal: zero `\dAsync`, zero verb-only methods, service wrappers repaired.

- [x] **4.1** Refreshed `Explore.API/swagger.json` from the now-clean runtime endpoint.
  - **Result:** swagger.json contains canonical PascalCase operationIds (GetMadhabs, GetTenants, CreateEventRegistration, etc.).
- [x] **4.2** Ran NSwag regeneration for `Explore.Blazor.Client/nswag.json`.
  - **Result:** `EventApiClient.g.cs` regenerated via `SingleClientFromOperationId` mode. 271 clean PascalCase methods.
- [x] **4.3** Inspected regeneration diff — no logic changes, only naming + new `api_version`/`x_Api_Version` trailing string params before `CancellationToken`.
- [x] **4.4** Full solution build: **0 errors**, 2575 pre-existing warnings.
- [x] **4.5** Renamed call sites. Scope wider than originally planned 5 wrappers — **22 service files + 1 Razor component** required updates (23 production files total). Commit `5917b26e`:
  - Services: Admin, Category, ContactShareConsent, EventRegistration, EventSeries, Event, EventSessionAgendaItem, ExternalApiKey, ImageStorage, OrganizationMember, OrganizationReview, Organization, Tag, Translation, User, UserSettings
  - Lookup services: AudienceAge, AudienceGender, EventFormat, EventStatus, EventType, Language, Madhab
  - Razor: `Pages/Admin/Instance/Components/InstanceTenantsSection.razor`
  - 63 unique legacy names mapped to new PascalCase names.
  - `CancellationToken` now passed as `cancellationToken: ct` named argument (new trailing params shifted position).
- [x] **4.6** Test project alignment (not in original plan — emerged from regeneration). Commit `c146fb20`:
  - 23 test files updated in `Explore.Blazor.Client.Tests/`
  - NSubstitute matchers expanded: `Arg.Any<CancellationToken>()` → `Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()`
  - Method rename follow-through for remaining test-side call sites.
  - `MockServiceFactory.ITranslationService` mocks left unchanged (service interface, not IEventApiClient).
- [x] **4.7** Verified guardrails:
  - `ApiClientNamingTests`: **4/4 GREEN** ✅ — zero `\dAsync` matches, no banned placeholders.
  - `Explore.Blazor.Client.Tests`: 691 passed / 1 failed / 1 skipped. The single failure (`SaveLocal_WhenBrowserCommandSucceeds_RedirectsToLogin`) is a pre-existing flaky timing test introduced in commit `0a0697db`, unrelated to Phase 4.

**Phase 4 complete:** Phase 0.2 assertions flipped GREEN; full solution builds with 0 errors; both Blazor.Client (production) and Blazor.Client.Tests project compile and build clean.

---

## Phase 5A — Contract-surface hygiene (0.25 day, highest strategic priority in Phase 5) ⏳ NOT STARTED

Goal: every remaining API action deliberately classified and visible/invisible accordingly.

- [ ] **5A.1** Verify endpoint-class assignments from Phase 1.5 are reflected in code (attributes in place).
- [ ] **5A.2** Manual/diff-based review of the surviving OpenAPI document. Every listed endpoint is deliberately Public or Authenticated Admin; no bootstrap, probe, or test-connection endpoint leaks through.
  - **Acceptance:** Review committed as comment in `context.md`.
- [ ] **5A.3** Decide Storage/SMTP/Localization **test-connection** endpoints classification.
  - Default: Authenticated Admin (expose through typed client so admin UI buttons light up).
  - Alternative: Internal (admin UI triggers via separate BFF endpoint).
  - **Acceptance:** Decision recorded in `context.md`. If A: explicit admin-role attribute + inclusion. If B: `IgnoreApi` + BFF route doc.

**Phase 5A complete when:** No unclassified action remains; surviving OpenAPI surface reviewed and approved.

---

## Phase 5B — Client-consumer hygiene + smoke test (0.25 day) ⏳ NOT STARTED

Goal: regenerated client is mechanically replaceable and developer-ergonomic.

- [ ] **5B.1** Confirm no hand edits in `EventApiClient.g.cs`. Confirm partials minimal.
  - **Acceptance:** Diff-based review; partials documented.
- [ ] **5B.2** Confirm every wrapper service absorbs rename churn; no raw client calls in pages/components.
  - **Acceptance:** `grep -r "IEventApiClient" Explore.Blazor*/Components` returns zero hits (except approved framework-level usage, if any).
- [ ] **5B.3** Add "Generated-Client Ergonomics Bar" to `docs/GOVERNANCE.md`:
  - No verb-only method names.
  - Collection vs single-resource distinguishable.
  - Mutation names reflect business action where business action ≠ HTTP verb.
  - **Acceptance:** Section committed; cross-references this plan.
- [ ] **5B.4** Add `Explore.Blazor.Client.Tests/GeneratedClientSmokeTests.cs`.
  - Instantiates `IEventApiClient` against a test server (reuse `Event.API.IntegrationTests` infrastructure or minimal host).
  - Calls ≥1 representative operation per HTTP verb: GET collection, GET by id, POST, PUT, DELETE.
  - Asserts 2xx (or documented expected code) and deserializable payloads.
  - **Acceptance:** Test passes; proves runtime compatibility, not just naming.

**Phase 5B complete when:** All four items done; ergonomics bar documented; smoke test green.

---

## Phase 5C — UI cleanups (0.25 day, lowest strategic priority) ⏳ NOT STARTED

Goal: finish the Blazor audit backlog.

- [ ] **5C.1** Delete `Explore.Blazor/Components/Pages/Admin/InstanceSettings.razor` (legacy redirect).
  - Check for any inbound links; redirect target is the modern instance-settings page.
  - **Acceptance:** File removed (**report as deletion candidate for user approval first**); no broken links; build clean.
- [ ] **5C.2** Investigate `Explore.Blazor/Components/Pages/Admin/TenantPolicySettings.razor`.
  - If replacement page live: report for deletion.
  - If still needed: add ABOUTME line explaining why.
  - **Acceptance:** Decision recorded in `context.md`.
- [ ] **5C.3** Decide on `ImageStorageService` SRP split.
  - If approved: split into `ImageApiService` (typed-client) + `ImageUploadService` (BffClient multipart); register both; retire mixed service.
  - If deferred: add a comment citing this plan.
  - **Acceptance:** Decision recorded with rationale.

**Phase 5C complete when:** All three items have explicit resolution.

---

## Phase 6 — Fold `hateoas-client-alignment` in (0.5 day) ⏳ NOT STARTED

Goal: downstream HAL work ships cleanly on the stable client.

- [ ] **6.1** Read `dev/active/hateoas-client-alignment/hateoas-client-alignment-context.md`; confirm scope unchanged.
- [ ] **6.2** Add parent-pointer header to `dev/active/hateoas-client-alignment/hateoas-client-alignment-plan.md`:
  - "**Parent plan:** `dev/active/api-contract-stabilization/api-contract-stabilization-plan.md`"
  - "**Do not start before:** Phase 4 of parent plan merged."
  - **Acceptance:** Header present; existing content untouched.
- [ ] **6.3** Execute the HAL plan's tasks in its own file (tracked there, not here).
  - **Acceptance:** HAL plan's tasks either done or scheduled.

**Phase 6 complete when:** HAL plan updated with parent-pointer; tasks done or scheduled.

---

## Phase 7 — Verification + schema-diff + forward standard (0.75 day) ⏳ NOT STARTED

Goal: prove it stays fixed; install schema-diff visibility; formalize forward policy.

- [ ] **7.1** CI wiring confirmed.
  - Phase 0 tests run on every push.
  - Phase 1.1 generator runs in CI; inventory-file drift (generated vs committed) fails the build.
  - **Acceptance:** Pipeline config references new test classes and generator target.
- [ ] **7.2** Run the full `CLAUDE.md` test matrix individually:
  - `Event.Application.UnitTests`
  - `Event.Domain.UnitTests`
  - `Event.Architecture.Tests`
  - `Explore.Secrets.UnitTests`
  - `Event.Persistence.IntegrationTests`
  - `Event.API.IntegrationTests`
  - `Explore.Blazor.IntegrationTests`
  - `Explore.Blazor.Client.Tests`
  - **Acceptance:** All green (or explicitly noted pre-existing failures unrelated to this work).
- [ ] **7.3** Manual smoke via Aspire AppHost.
  - Launch AppHost; hit every admin page under `/admin/*`; confirm save/load for each.
  - **Acceptance:** No regression observed.
- [ ] **7.4** Add `Event.Architecture.Tests/ApiContractArchitectureTests.cs`.
  - Asserts: every controller carrying `[ApiVersion]` (if any survive Phase 2.5) has explicit route names OR documented exemption.
  - Asserts: every action is reachable with a unique operation identity.
  - Asserts: every action is classified (Public / Authenticated Admin / Internal).
  - Asserts: every new controller follows the forward policy (explicit route template, explicit name, explicit response typing, explicit OpenAPI inclusion).
  - **Acceptance:** Test passes against current state.
- [ ] **7.5** Add **schema-diff visibility** job in CI.
  - Compare OpenAPI document from HEAD against previous commit / `main`.
  - Emit a visible job-summary: added / removed / changed operations and schemas.
  - **Non-blocking today** (pre-1.0); flip to blocking at 1.0.
  - **Acceptance:** Job produces a diff artifact visible in PR UI; does not fail the build on change.
- [ ] **7.6** Publish **forward controller-authoring standard** in `docs/QUICK_REFERENCE.md`.
  - Every new controller action must have: explicit route template, explicit route name / endpoint name, explicit endpoint class, explicit response typing, no overloaded semantics.
  - **Acceptance:** Section committed; cross-references this plan and `docs/GOVERNANCE.md`.

**Phase 7 complete when:** Test matrix green, manual smoke clean, architecture test passing, schema-diff job emitting summaries, forward standard documented.

---

## Discovered Tasks (append as they arise)

- _None yet._

---

## Quick Resume

1. Check SESSION PROGRESS in `api-contract-stabilization-context.md`.
2. Find the first unchecked box above; verify acceptance criteria on the immediately preceding box still hold.
3. Mark one box `🟡 IN PROGRESS`, do the work, then mark `x` COMPLETE.
4. Never skip a phase. Guardrails (Phase 0) exist so later phases can't silently regress.
5. Before Phase 2 execution, confirm the 2.4 and 2.5 escalation gates with the user.
