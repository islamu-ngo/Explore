ABOUTME: Live context file for the API contract stabilization effort. Holds session progress, key files, decisions, and quick-resume guidance.
ABOUTME: Update the SESSION PROGRESS section every meaningful step — this is what survives context resets.

# API Contract Stabilization - Context

**Last Updated:** 2026-04-19
**Parent of:** `dev/active/hateoas-client-alignment/`
**Status:** Phase 0 ✅ COMPLETE | Phase 1 🟡 IN PROGRESS (1.5 bulk annotation of 70 controllers; 2/3 agents done, 3rd running)

---

## SESSION PROGRESS (2026-04-19)

### ✅ COMPLETED
- 5-agent parallel Blazor audit (ApiClient wiring, service layer, pages/admin, BFF/auth, controller-to-client coverage).
- Oracle strategy consultation — confirmed API/OpenAPI contract defect (not NSwag).
- Identified true root cause: dual-versioning routes (`/api/...` + `/api/v0.1/...`) both landing in `swagger.json`; versioned selector copies lose `operationId`; NSwag collides → **464 `\dAsync` methods**.
- Identified minimal Blazor blast radius: **only 5 service wrappers** call suffixed methods directly.
- **CTO review applied 2026-04-19** — 12 revisions (see plan changelog).
- **Plan v3 written** (post-user versioning decision m0044): multi-reader API versioning locked (media-type primary + query-string + custom-header; URL-segment deleted); **enum labels finalized to Public / Authenticated / Admin** (user decision m0086).
- **Phase 0 COMPLETE (all 3 sub-tasks):**
  - `Event.API.IntegrationTests/Features/ContractInvariantsTests.cs` (6 RED tests over `/openapi/event-api.json`).
  - `Explore.Blazor.Client.Tests/ApiClientNamingTests.cs` (6 RED tests reflecting over `IEventApiClient`).
  - `docs/GOVERNANCE.md` — new "API Contract Rules" section (versioning strategy, endpoint classification with final labels, operation IDs, banned names, client-ergonomics bar, contract ownership, authoring checklist).
- **Phase 1.1 COMPLETE:** inventory generator implemented as TUnit integration test (`ApiContractInventoryGeneratorTests.cs`) with `Classification` column sourced from `x-endpoint-class` OpenAPI extension.
- **Phase 1.2 COMPLETE:** `docs/NAMING_CONVENTIONS.md` — new "API Contract Naming" section with operation ID naming policy + Route Name vs Operation Id language + authoring checklist.
- **Phase 1.3 COMPLETE:** collision detection provided by Phase 0.1 + Phase 1.1 combined (no duplicate detector needed).
- **Phase 1.4 COMPLETE:** `Event.API.IntegrationTests/Features/RouteNameCoverageTests.cs` — 3 tests enforcing 1:1 coverage between `RouteNames` constants and `EndpointDataSource` named endpoints.
- **Phase 1.5 infrastructure COMPLETE:**
  - `Explore.API/Attributes/EndpointClass.cs` (enum Public/Authenticated/Admin).
  - `Explore.API/Attributes/EndpointClassificationAttribute.cs`.
  - `Explore.API/OpenApi/EndpointClassificationTransformer.cs` (`IOpenApiOperationTransformer`; emits `x-endpoint-class`).
  - `Explore.API/Program.cs` wiring.
  - `Event.Architecture.Tests/EndpointClassificationArchitectureTests.cs` (arch test).
  - `dotnet build` → 0 errors, zero warnings from new files.

### 🟡 IN PROGRESS
- **Phase 1.5 bulk annotation** — 70 non-abstract controllers split across 3 parallel `deep` agents.
  - Agent 1 `bg_d46fc264` — 25 uniform-Public class-level. ✅ COMPLETED.
  - Agent 2 `bg_68d607b1` — 19 uniform-Authenticated class-level. ✅ COMPLETED.
  - Agent 3 `bg_ef573d93` — 27 mixed-auth per-action. 🟡 RUNNING (~9m+, large per-action workload).

### ⏳ NOT STARTED (awaiting Phase 1 completion + user Phase 2 approval)
- Phase 2 — **Delete URL-segment only**, keep media-type + add query-string + custom-header readers (locked per user m0044)
- Phase 3 — Stable operationIds
- Phase 4 — Regenerate `IEventApiClient`
- Phase 5A — Contract-surface hygiene
- Phase 5B — Client-consumer hygiene + smoke test
- Phase 5C — UI cleanups
- Phase 6 — Fold `hateoas-client-alignment`
- Phase 7 — Verification + schema-diff visibility + forward standard

### 🚩 KNOWN FLAGS (surface in Phase 0+1 report)
- **`InstanceSettingsController`** classified `Authenticated` (class `[Authorize]`) but every action runtime-checks `IsInstanceAdmin` → real classification is **Admin**. Strict attribute-based rules can't capture runtime checks. Future: add `[Authorize(Roles=...)]` or policy attribute.
- **`TenantController` writes** classified `Authenticated` (no `Roles=` attribute) — arguably should be **Admin**. Flag for future role-based authorization pass.
- **Zero `Roles=` attributes codebase-wide.** Current auth policy is inline runtime checks, not declarative. Consider declarative roles/policies in a future workstream.

### ⚠️ BLOCKERS / DECISIONS NEEDED (remaining for Phase 2+)
- ~~**Decision (Phase 2.4):** URL-segment alias consumers?~~ **RESOLVED (m0044):** Delete URL-segment entirely.
- ~~**Decision (Phase 2.5):** Media-type versioning consumers?~~ **RESOLVED (m0044):** KEEP media-type versioning. ADD `QueryStringApiVersionReader("api-version")` + `HeaderApiVersionReader("X-Api-Version")` via `ApiVersionReader.Combine`. No URL-segment.
- **Decision (Phase 2.1):** Confirm native OpenAPI (.NET 10) vs Swashbuckle pipeline under `OpenApiExportService`. One-line finding.
- **Decision (Phase 5A.3):** Storage/SMTP/Localization test-connection endpoints — Authenticated Admin (expose through typed client, default) vs Internal (BFF-only).
- **Decision (Phase 5C.3):** `ImageStorageService` SRP split approval — nice-to-have, not strictly required.
- **Decision (pre-Phase 2):** User approval for `VersionedRouteConvention.cs` deletion candidate (per shell rules).

---

## Key Files

### The root-cause evidence
- **`Explore.Blazor.Client/Clients/EventApiClient.g.cs`** (86 632 lines)
  - Generated NSwag client. 464 methods match `\dAsync`.
  - Verified pairs: `TenantDELETEAsync` (65032) / `TenantDELETE2Async` (65316); `TenantGET2Async`/`TenantGET3Async`; `AuthProviderGETAsync`/`AuthProviderGET2Async`; `Status7Async`/`Status8Async` (TenantOnboarding); `InternalAsync`/`Internal2Async`; `AuthProviderConfigurationAsync`/`AuthProviderConfiguration2Async`.
- **`Explore.API/swagger.json`** (checked-in)
  - NSwag input. Contains both `/api/tenant/{id}` and `/api/v0.1/tenant/{id}` entries; versioned entries have no `operationId`.
- **`Explore.Blazor.Client/nswag.json`** (93 lines)
  - Uses `operationGenerationMode: "SingleClientFromOperationId"`. Correct setting — problem is upstream.

### Controllers to study for naming policy
- **`Explore.API/Controllers/TenantController.cs`**
  - `[ApiVersion("0.1")]` + `[Route("api/[controller]")]`. Explicit `GetById`/`Update`/`Delete` actions (lines 62-141).
  - **No explicit action names.** Generates `TenantGET/TenantGET2` soup. **First controller fixed in Phase 3.**
- **`Explore.API/Controllers/ActorController.cs`**
  - `[Route("api/actor")]` (explicit, non-template). Explicit action names → clean generation. **Baseline to copy.**

### The versioning plumbing (Phase 2 surgery)
- **`Explore.API/Extensions/ApiVersioningExtensions.cs`** — ASP.NET versioning setup. Primary Phase 2 touch point.
- **`Explore.API/VersionedRouteConvention.cs`** — selector-cloning convention; creates `/api/v0.1/...` aliases. **Likely deleted in Phase 2.2.**
- **`Explore.API/Services/OpenApiExportService.cs`** — writes `/openapi/event-api.json`. Phase 3.2 invariant check hooks here.

### The 5 service wrappers that break on regeneration (Phase 4.5)
- `Explore.Blazor.Client/Services/UserService.cs`
- `Explore.Blazor.Client/Services/EventRegistrationService.cs`
- `Explore.Blazor.Client/Services/EventSeriesService.cs`
- `Explore.Blazor.Client/Services/EventSessionAgendaItemService.cs`
- `Explore.Blazor.Client/Services/OrganizationMemberService.cs`

### Audit cleanup targets (Phase 5C — lowest strategic priority)
- `Explore.Blazor/Components/Pages/Admin/InstanceSettings.razor` — legacy redirect (delete candidate).
- `Explore.Blazor/Components/Pages/Admin/TenantPolicySettings.razor` — check replacement status.
- `Explore.Blazor.Client/Services/ImageStorageService.cs` — SRP split candidate.

### Guardrail test locations (Phase 0, 5B, 7)
- `Event.API.IntegrationTests/ContractInvariantsTests.cs` (Phase 0.1 — new)
- `Explore.Blazor.Client.Tests/ApiClientNamingTests.cs` (Phase 0.2 — new)
- `Explore.Blazor.Client.Tests/GeneratedClientSmokeTests.cs` (Phase 5B.4 — new)
- `Event.Architecture.Tests/ApiContractArchitectureTests.cs` (Phase 7.4 — new)

### Tooling (Phase 1.1 — new)
- `Explore.API/Tools/ActionInventoryExporter.cs` (or equivalent) — CLI generator walking `IApiDescriptionGroupCollectionProvider` to emit `api-contract-stabilization-action-inventory.md`. **Generator, not hand-curation.**

### Documentation
- `docs/ARCHITECTURE.md` — line 57 (dual-versioning decision; **subject to Phase 2.5 rewrite** if media-type versioning is also dropped).
- `docs/QUICK_REFERENCE.md` — rule 20 (named routes in `RouteNames` must match `[HttpGet(Name=...)]`). **Phase 7.6 adds** the forward controller-authoring standard.
- `docs/NAMING_CONVENTIONS.md` — Phase 1.2 adds operationId naming policy + route-name vs operation-id language.
- `docs/GOVERNANCE.md` — Phase 0.3 adds "API Contract Rules" subsection; Phase 5B.3 adds "Generated-Client Ergonomics Bar".

### Related plan
- **`dev/active/hateoas-client-alignment/`** — downstream. Phase 6 adds parent-pointer header.

---

## Important Decisions

### D1 — Do NOT set `useOperationIds=false`
Oracle recommendation. That would only rename symptoms. **Confirmed CTO-approved.**

### D2 (revised again per user m0044) — Maximum-flexibility versioning: media-type + query-string + custom-header, NO URL-segment
**Original D2:** keep media-type canonical, strip URL-segment from OpenAPI.
**CTO revision:** delete URL-segment AND media-type entirely, single unversioned contract.
**User override (m0044):** keep media-type versioning (architecturally correct, REST-pure with HATEOAS). **Delete URL-segment** (duplicates all routes, clutters HATEOAS link generation, makes OpenAPI messy → root cause of 464 duplicates). **Additionally add** `QueryStringApiVersionReader("api-version")` + `HeaderApiVersionReader("X-Api-Version")` as flexibility fallbacks for webhooks / browser scripts / clients that can't set `Accept`.

**Target configuration:**
```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(0, 1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new MediaTypeApiVersionReader("v"),           // Accept: application/json;v=0.1
        new QueryStringApiVersionReader("api-version"),// ?api-version=0.1
        new HeaderApiVersionReader("X-Api-Version")    // X-Api-Version: 0.1
    );
});
```

**Controllers:** single `[Route("api/[controller]")]` only. No `[Route("api/v{version:apiVersion}/[controller]")]`. No dual-route decoration.

**No env-var toggle.** Versioning strategy is contract-level, identical across environments.

Rationale (user): ASP.NET Core framework handles conflict detection (400 Bad Request on ambiguous versioning). Clean routing, predictable HATEOAS link generation, flexibility for basic consumers without abandoning REST purity.

### D3 — One OpenAPI document, not two
Single canonical document exported from `OpenApiExportService`. Second document only if a late-discovered external consumer requires it — NSwag points only at the primary.

### D4 — Guardrails first, surgery second
Phase 0 is non-negotiable. Failing tests define the invariant. **Confirmed CTO-approved (strongest part of the plan).**

### D5 — `hateoas-client-alignment` is downstream
Waits until Phase 4 merged. Avoids regenerating the client twice.

### D6 (revised per CTO) — Route Name and Operation Id are intentionally aligned, not inherently identical
**Original D6:** prefer `[HttpGet(Name = "...")]` so route name = operation id = `RouteNames` constant.
**Revised D6:** today they align via `[HttpGet(Name = "...")]`. Conceptually they are distinct: route name = routing/HAL identity; operation id = client contract identity. Policy documents the alignment as deliberate; allows divergence when future need justifies it.

### D7 (revised per CTO) — Preferred Phase-2 mechanism: delete the alias strategy
**Original D7:** `ApiExplorerSettings.IgnoreApi = true` on versioned selector clones.
**Revised D7:** delete the alias cloning entirely in `VersionedRouteConvention` (or remove the class). Runtime returns 404 for `/api/v0.1/...` which is correct for a deleted surface. `IgnoreApi` on clones is a fallback only if deletion cascades into unexpected breakage. Document-transform filtering is a tertiary fallback.

### D8 — No domain changes
Pure API-contract + Blazor-client work. Do not touch `Explore.Domain` or `Explore.Application`.

### D9 — No backwards compatibility
Development mode. Break freely. Delete duplicates. **Confirmed CTO-approved — "perfect moment to make contract-breaking cleanup moves".**

### D10 (new) — OpenAPI is a governed product artifact
Checked-in `swagger.json` is first-class. Changes to public routes, operationIds, schemas, or response semantics require PR review. Governance language added to `docs/GOVERNANCE.md` in Phase 0.3.

### D11 (new) — Every action has an endpoint class
Public / Authenticated Admin / Internal. Unclassified = architecture test fails. Drives OpenAPI inclusion, role gating, and client-generation eligibility.

### D12 (new) — Schema-diff visible in CI, not blocking (pre-1.0)
Phase 7.5 surfaces added/removed/changed operations and schemas at PR time. No block today. Flip to blocking at 1.0.

### D13 (new) — Generated-client ergonomics bar
No verb-only names. Collection vs single distinguishable. Mutation names reflect business action where meaningful. Asserted via Phase 0.2 expanded assertions.

### D14 (new) — Action inventory generated, not hand-curated
Phase 1.1 adds a CLI generator that walks ApiExplorer. Phase 7.1 CI wiring detects inventory drift.

### D15 (new, user m0044) — Multi-reader API versioning is the permanent strategy
Media-type is primary (REST-pure, preferred in Blazor frontend). Query-string and custom header are secondary flexibility readers. The framework resolves conflicts (400 on ambiguity). URL-segment is explicitly NOT a reader — runtime returns 404 for `/api/v0.1/...` after Phase 2. This policy is permanent, not pre-1.0 temporary; it survives 1.0+ without redesign.

---

## Technical Constraints (Non-Inferable — from `CLAUDE.md` + `docs/QUICK_REFERENCE.md`)

1. Repositories return **entities**, never DTOs; mapping in handlers.
2. Validators manually instantiated (no DI).
3. Commands return `BaseCommandResponse<TId>`.
4. GET = `[AllowAnonymous]`, write = `[Authorize]`, admin = roles.
5. UserId fallback: `sub` → `nameidentifier` → `sid`.
6. File-scoped namespaces for new C# files.
7. Named route constants in `RouteNames` must match `[HttpGet(Name = "...")]` values.
8. All files start with `ABOUTME:` two-line summary.
9. Auditing fields on entities: `CreatedAt/By`, `UpdatedAt/By`, `IsDeleted`.
10. EF soft-delete filter named `SoftDelete`.

---

## Build / Test Baseline

**Build:**
```
dotnet build --configuration Release --verbosity quiet
```

**Test projects run individually** (NOT solution-level):
- `Event.Application.UnitTests`
- `Event.Domain.UnitTests`
- `Event.Architecture.Tests`
- `Explore.Secrets.UnitTests`
- `Event.Persistence.IntegrationTests`
- `Event.API.IntegrationTests`
- `Explore.Blazor.IntegrationTests`
- `Explore.Blazor.Client.Tests`

`Explore.Blazor.Client.E2ETests` requires Aspire AppHost running; not part of standard run.

---

## Quick Resume

To continue from any future session:

1. **Read** this file first. SESSION PROGRESS tells you current state.
2. **Read** `api-contract-stabilization-plan.md` for strategy and phase definitions (v2 post-CTO review).
3. **Read** `api-contract-stabilization-tasks.md` for live checklist.
4. **Verify** nothing has drifted: `grep -c "\dAsync" Explore.Blazor.Client/Clients/EventApiClient.g.cs` should still be ~464 if Phase 4 hasn't run.
5. **If Phase 0 isn't done yet, START THERE.** Guardrails before surgery.
6. **Do not regenerate the client** until Phases 2 and 3 are complete.
7. **Before Phase 2 execution**, confirm the Phase 2.4 and 2.5 escalation gates with the user (URL-segment consumers? media-type consumers?). Default: delete both.

---

## Session IDs (for deeper-dive continuation)

- ApiClient audit: `ses_25e130f03ffeDihkcd5maMz6AO`
- Services map: `ses_25e12def5ffeOYPGSjO79aOl8w`
- Pages/admin: `ses_25e12aca8ffejWZhSWfxR7hvIQ`
- BFF/auth: `ses_25e127029ffeGJyd6Qwqzd7R6J`
- Controllers/coverage: `ses_25e124572ffebG5WiOkvvAlAGh`
- Oracle strategy: `ses_25d39daafffeI8vNfafL8RSPdR`
