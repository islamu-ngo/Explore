<!-- ABOUTME: Evidence register for API-wide code-liability reduction decisions and phase gates. -->
<!-- ABOUTME: Separates verified repository facts from assumptions, external-tool failures, and implementation results. -->

# API-Wide Code Liability Reduction — Evidence Register

Last Updated: 2026-08-16 Europe/Brussels

## Phase 7 + 8.1 Evidence (2026-08-16)

### Contract equivalence proof for the controller partition

The five hotspot controllers were split into capability controllers. Because ASP.NET Core derives operationIds
from `[Http*(Name = ...)]` — not from the controller class — and NSwag is configured
`operationGenerationMode: SingleClientFromOperationId`, a class-level split is invisible to clients **provided
every action keeps its route name**. That was verified rather than assumed:

| Check | Result |
|---|---|
| OpenAPI paths added / removed | **0 / 0** |
| operationIds | **identical**, 756 operations |
| `components` section | **identical** |
| Per-operation differences excluding `tags` | **0** |
| Generated `EventApiClient` public methods | **756, identical** |

The sole document change is the `tags` array, which now names the real capability. That is a documentation
grouping improvement, recorded in `docs/API_CHANGELOG.md`.

### Family sizes after partition

| Family | Before | After |
|---|---|---|
| Event | 1,033 | 334 + Lifecycle 321 + Moderation 194 + ManagementRead 211 + Calendar 154 |
| RegistrationOrder | 1,146 | 78 + Guest 514 + Authenticated 457 + Base 257 |
| Webhooks | 987 | 403 + Endpoints 316 + Messages 330 + Base 77 |
| InstanceSettings | 859 | **deleted** — six capability controllers + Base |
| ControlPlane | 673 | 166 + TenantPlan 249 + TenantConfiguration 246 + TenantLifecycle 200 |

Controllers over 500 lines fell from **7 to 3**, none of them a former hotspot.

### Phase 7.1 — a domain rule that lived in the wrong layer

Event moderation reason-code normalization was implemented as four private controller helpers wrapping
`EventModerationReasonCodePolicy`. Only HTTP callers were subject to it; an MCP or internal caller of
`ModerateEventCommand` bypassed it entirely. Moving the normalization into the three command handlers makes
the rule unconditional and removed ~79 lines of controller code. The failure shape is unchanged: the handler
returns the same failure code and error text the controller previously built by hand.

### Shared behavior became explicit bases, not duplication

Splitting a controller surfaces what its actions were quietly sharing. Three of those turned out to be real
boundaries worth naming:

- `InstanceSettingsControllerBase` — the instance-admin-**or**-active-setup-secret rule. Six settings surfaces
  now inherit one implementation; a surface that re-implemented it slightly differently would have been a way
  to configure an instance without being its administrator.
- `WebhooksControllerBase` — server-resolved ownership scope for collection links, so a pagination URL can
  never be built from a caller-supplied owner id.
- `RegistrationOrderControllerBase` — the native-attempt/requirement/participant protocol shared by guest and
  authenticated checkout, so the two doors cannot drift into accepting different registrations.

### Phase 8.1 — composition root

`AddApiBackgroundProcessing` now owns the in-process worker topology, with each enablement condition stated
inline and no reflection; `AddApiHostServices` fell from ~460 to 393 lines. Extracting it also surfaced a dead
`if` block left behind when the organizer-payment worker moved to Quartz — the settings were still bound and
tested, but the body was empty.

### Test-suite repointing

The partition broke contract tests that reflect over controller types by name. They were repointed rather than
weakened, and two patterns emerged worth keeping:

- Unit-style controller tests now resolve constructor arguments **by parameter type** from a pool, so a
  capability controller that declares only the dependencies it uses no longer breaks a shared factory.
- Contract tests that assert across a whole family (`EventFamilyAction`, `ControlPlaneFamilyAction`,
  `RegistrationFamilyAction`, `WebhookFamilyAction`) look an action up across the family rather than on one
  hardcoded class, which is what the assertion actually meant all along.

### Verification caveat worth recording

Mid-session the Docker daemon stopped, and every Testcontainers-backed test failed with
`DockerUnavailableException` — 538 failures that looked like a catastrophic regression and were purely
environmental. With Podman the suite needs `DOCKER_HOST`, `TESTCONTAINERS_RYUK_DISABLED`, and
`TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE` exported. Any future "mass regression" should be checked against the
container endpoint before the code.

## Delivery Evidence (2026-08-16) — Phases 1–6, 8.2/8.3

### Measured outcomes

| Liability | Before | After | Proof |
|---|---|---|---|
| Controller service location (`HttpContext.RequestServices`) | 1 | **0** | Ratchet A baseline is now empty |
| Divergent identity claim chains | 3 | **1** | `PlatformIdentityPrincipalExtensions`; `UserContext` and `GetAuthenticatedUserId` delegate |
| `ExploreControllerBase` | 147 lines | **48 lines** | Provider reconstruction and the locator removed |
| Controller files parsing identity claims | 7 | **5** | All remaining are purpose-bound or diagnostic, each with a recorded reason |
| Private controller failure mappers | 11 across 4 files | **1** | Ratchet C; survivor is feature-specific |
| `HateoasAssemblerRegistration.cs` `AddScoped` | 296 | **27** | Ratchet F; service graph diffed 293 → 293, byte-identical |
| `Task.Delay` periodic worker files | 17 | **10** | Ratchet D; 8 migrated to Quartz, remainder characterized exclusions |
| `EventManagementMcpTools.cs` | 2,516 lines | **1,463 lines** | Split into 4 focused modules; MCP protocol surface untouched |
| Release build warnings | 13,535 | **9,290** | Same canonical command |

### HAL service-graph equivalence (Phase 4.2 acceptance)

The full descriptor inventory — service type, implementation type, and lifetime for all 293 HAL registrations
— was captured before and after the helper migration and diffed. The result was **identical**, which is the
plan's stated acceptance criterion for changing registration style without changing what resolves. The
inventory is reproducible from `HateoasRegistrationGraphTests.HalRegistrationInventory_IsStableAndNonEmpty`.

### Regression attribution (Phase 2b)

The identity migration initially produced 8 apparent API-integration failures. A baseline run in an isolated
git worktree at HEAD established that **25 failures are pre-existing**. Of the 8, all traced to six
unit-style controller tests that substituted `IUserContext` through the service-location seam while leaving
the principal claim-less. They were fixed by giving those principals real `sub` claims, which makes them
exercise the genuine claim→identity path rather than mocking it away. Final position: **25 failures, equal to
baseline**, with one baseline failure (`OpenApiDocument_PublicHalDetailResourceSchemasAreNotEmpty`) *fixed* by
the Phase 0 HAL wrapper registration.

### Design decisions worth preserving

- **Identity as a pure function.** `UserContext` only ever used `IHttpContextAccessor` to reach the principal;
  the claim logic itself was pure. Recognizing that removed the need to thread an identity dependency through
  25 controller constructors — the extensions read `ControllerBase.User`, which the controller already has.
- **The pinned fallback order was deliberate.** `UserContextTests` asserts `sub` wins over `internal_user_id`.
  Consolidation preserved that ordering rather than "improving" it, because for platform-managed accounts the
  provider subject *is* the local id.
- **Composable failure policies.** `CommandFailurePolicy` is immutable, so `GuestStartFailures` is expressed as
  `OrderLifecycleFailures` plus one rule. The relationship between a capability's policies became visible
  instead of being three near-identical literals.
- **Exact allowlists, not ceilings.** A ratchet that only rejects *new* violations lets a fixed violation's
  entry rot. Making the baseline an exact set means a successful migration fails the hygiene test until the
  entry is deleted, which is what makes the ratchets monotonic.

## Phase 0 Closure (2026-08-16) — verification baseline established

### 0.1 Toolchain and build baseline

The SDK workload blocker recorded on 2026-08-15 is **resolved** (repo commit `68b6f7848`). Verified 2026-08-16 on SDK `10.0.302`:

| Command | Result |
|---|---|
| `dotnet build --configuration Release --verbosity quiet` (solution) | **Build succeeded — 0 errors, 13,535 warnings** (pre-Phase-0 measurement) |
| Same, after Phase 0 fixes | **Build succeeded — 0 errors, 12,917 warnings** |
| `dotnet build src/Explore.API/Explore.API.csproj` | 0 errors, 999 warnings |

The plan's long-standing `758 warnings` figure is **wrong and is retracted**; the real solution-wide count is an order of magnitude higher. `12,917` is the authoritative baseline for "warning-neutral" claims from this point. The contradictory `0 warnings / exited 1 during restore` record is also retracted.

### 0.2 Known architecture failures — all four closed

Baseline run: **383 total, 378 passed, 4 failed, 1 skipped.** Final run after fixes: **383 total, 382 passed, 0 failed, 1 skipped.**

| # | Failing test | Root cause | Fix |
|---|---|---|---|
| 1 | `Runtime tenant-filter bypasses must use approved reason constants` | `RegistrationFormAuthoringRepository.GetTemplateSourceVersionAsync` passed a raw string `"registration-form-template-source-version"` to `IgnoreTenantFilter` | Added `TenantFilterBypassReasons.RegistrationFormTemplateSourceVersion` and referenced it. The bypass itself is legitimate — bounded by an exact event/form/version id tuple. |
| 2 | `DTOs_ShouldEndWith_Dto` | `RegistrationFormTemplateInput` and `InstantiateRegistrationFormTemplateInput` violated the `Dto` suffix convention | Renamed both to `…InputDto` across Application, API, Blazor, and tests, rather than growing the exemption list. OpenAPI and the NSwag client were regenerated through the documented workflow. |
| 3 | `BlazorProductionBackendContracts_ShouldComeFromGeneratedApiClient` | `HalResourceOfRegistrationAnswerAnalyticsDto` was **never registered** in `HalOpenApiSchemaCatalog`, so OpenAPI emitted `"HalResourceOfRegistrationAnswerAnalyticsDto": { }` — an empty wrapper — and a hand-written Blazor mirror was added to compensate | Registered the wrapper and its three nested analytics DTOs in the catalog. The schema now emits full properties and three named components instead of inline anonymous shapes; NSwag now generates `RegistrationAnswerFieldAggregateDto`, `RegistrationAnswerAggregateCellDto`, and `RegistrationAnswerNumericAggregateDto` by name. Deleted `src/Explore.Blazor.Client/Clients/RegistrationAnswerAnalyticsClientDtos.cs`. Anonymous generated classes dropped 18 → 16. |
| 4 | `InventoryCoversCurrentEfAndDesignatedProviderSurfaces` | The PII inventory named properties that do not exist on the EF model, which **masked nine further real gaps** behind an early assertion | See below. |

### 0.2a Privacy findings surfaced by fixing failure #4

This was not a naming nit. Two inventory rows pointed at non-existent columns, so the columns that actually hold the data were uncovered, and the early assertion prevented the rest of the test from ever running:

- `EventContactShareConsent.UserId` → corrected to `UserSubjectId` (the real column).
- `EventContactShareExportItem.EmailSnapshot` → corrected to `ExportedFieldSnapshot` (the real column, which carries the exported contact payload). Also corrected in the test's required-coverage list.

With the early assertion passing, EF-model traversal then revealed six uncovered user-linked columns and three uncovered external provider surfaces, all now inventoried:

| Copy / surface | Disposition |
|---|---|
| `EventContactShareConsentHistory.UserId` | HardDelete |
| `EventContactShareConsentHistory.EmailSnapshot` | HardDelete |
| `EventContactShareConsentHistory.EmailNormalizedSnapshot` | HardDelete |
| `EventContactShareExport.ContentHash` | Anonymize — export integrity verification |
| `RegistrationAmendment.ActorUserId` | Anonymize — amendment accountability |
| `RegistrationOrder.AppliedPromotionDisplayLabelSnapshot` | BoundedRetain — financial reconciliation |
| `provider:stripe:connect-account` (`StripeConnectAccountAdapter`) | ExternalAction — RevokeOrUnlinkExternalIdentity |
| `provider:google-sheets:submission-row` (`GoogleSheetsRegistrationProviderSubmissionSink`) | ExternalAction — ExpireLocalMetadataWithoutRecall (organizer-owned document; delivered rows cannot be recalled) |
| `provider:registration-webhook:submission-payload` (`WebhookRegistrationProviderSubmissionSink`) | ExternalAction — CorrectOrDeleteProviderCopy |

### 0.3 Concurrent-workstream collision status

The user confirmed on 2026-08-16 that no other agent is working in this repository. The collision matrix therefore no longer gates phase ordering, and Design Rule 14 is satisfied by definition for this execution. The matrix is retained in `context.md` as documentation of which workstreams own which surfaces, but Phase 7 is no longer blocked on owner idleness.

### 0.4 Scheduling authority — decided

TickerQ has been **removed from the repository**. The platform now uses **Quartz.NET 3.19.1** (`Quartz`, `Quartz.AspNetCore`, `Quartz.Extensions.Hosting`, `Quartz.Serialization.SystemTextJson`) with an ADO job store that works across PostgreSQL, SQLite, SQL Server, MySQL, and MariaDB, optional clustering, an instance-admin status endpoint, and an embedded-SQL schema initializer. This settles plan §2.5 as **option A**: Quartz.NET is the periodic scheduling authority, and Phase 5 migrates qualifying `Task.Delay` workers onto it rather than introducing any bespoke lifecycle.

Established pattern to follow: `QuartzSchedulerExtensions.AddCronJob<TJob>` (durable job + cron trigger + `WithMisfireHandlingInstructionDoNothing`), job keys derived from `ScheduledJobNames` in `Explore.Application.Contracts.Scheduling`, `[DisallowConcurrentExecution]` on jobs, pointer-only `JobDataMap` payloads, and scheduler disabled in the `Testing` environment.

## 2026-08-15 Re-Verification (Senior CTO review)

### Toolchain — supersedes the 2026-08-14 build diagnosis below

The earlier record attributed the build failure to the `Explore.Blazor.Client` WebAssembly SDK task host (`MSB4216`/`MSB4027`). That diagnosis is **wrong and is retracted**. The actual failure is repository-wide and occurs at SDK resolution, before any compilation:

```
error MSB4242: SDK Resolver Failure: "Microsoft.DotNet.MSBuildWorkloadSdkResolver" failed while
attempting to resolve the SDK "Microsoft.NET.SDK.WorkloadAutoImportPropsLocator".
System.InvalidOperationException: Workload set version 10.0.301.1 has missing manifests
likely removed by package management. Run "dotnet workload repair" to fix this.
```

Reproduced 2026-08-15 for both `src/Explore.API/Explore.API.csproj` and `tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj` (Release, `-m:1`, `--no-restore`), each failing in ~0.1s. `dotnet workload list` throws the same exception. `global.json` pins `sdk.version` `10.0.301`; the installed SDK is `10.0.302`.

Consequence: the claim below that the API, Application, and architecture projects "each compile successfully" is **not currently reproducible** and must be re-established in Phase 0.1 rather than carried forward. The `0 errors, 758 warnings` baseline is likewise unverified, and contradicts the entry below recording the same command as exiting 1 during restore with 0 warnings and 0 errors. Phase 0.1 must record the real numbers with SDK version and date, and delete whichever record is wrong.

### Re-measured scale (working-tree HEAD, 2026-08-15)

| Metric | 2026-08-13 audit | 2026-08-15 verified | Delta |
|---|---|---|---|
| `Explore.API` files / C# lines | — / 62,201 | 480 / 62,881 | +680 |
| Controller files / lines | 119 / 24,882 | 121 / 25,326 | +2 files, +444 |
| `HateoasAssemblerRegistration.cs` lines / `AddScoped` | — / 278 | 456 / 293 | +15 registrations |
| `RegistrationOrderController.cs` | 1,061 | 1,142 | +81 |
| `EventController.cs` | 1,033 | 1,033 | 0 |
| `WebhooksController.cs` | 1,025 | 1,025 | 0 |
| `InstanceSettingsController.cs` | 854 | 858 | +4 |
| `ControlPlaneController.cs` | 672 | 672 | 0 |
| `BackgroundServices` files / lines | 33 / 2,046 | 34 / 2,110 | +1 file, +64 |
| `ApiHostServiceCollectionExtensions.cs` | 509 | 518 | +9 |
| `EventManagementMcpTools.cs` | 2,516 | 2,516 | 0 |
| `CommandResponseResultMapper.cs` | 643 | 643 | 0 |

Confirmed unchanged: `MapCommandResponse` has 15 controller call sites (16 API-wide); controllers contain 42 `FindFirst` and 44 `User.Find*` occurrences; 12 controllers retain private `Map*Failure`/`To*Problem` members; `ExploreControllerBase` is 147 lines and still service-locates `IUserContext` on line 17.

This is the evidence for moving the architecture ratchets from Phase 8 to Phase 1: over two days in which this workstream removed 174 lines, the API grew by 680.

### Newly surfaced facts

- **`Explore.API/Hateoas` is 170 files / 14,360 lines.** `HateoasAssemblerRegistration.cs` is 3.2% of it. `RouteNames.cs` (1,052) and `EventLinkPolicy.cs` (762) are the two largest HAL files and are out of scope. Phase 4 must not imply HAL is solved.
- **TickerQ 10.4.0 is already a dependency.** `Directory.Packages.props` pins `TickerQ`, `TickerQ.Dashboard`, `TickerQ.EntityFrameworkCore`, and `TickerQ.Instrumentation.OpenTelemetry`; all four are referenced by `Explore.API.csproj`. `ApiHostServiceCollectionExtensions.cs:177` calls `AddApiTickerQScheduler`, `ApiHostStartupExtensions.cs:97` applies its migrations, `ApiHostApplicationExtensions.cs:108` mounts it — gated to `EmailDispatchProcessorMode.TickerQ`. A bespoke periodic-worker base class would be a second permanent scheduling concept. Phase 5.1 must decide the authority before any worker code moves.
- **Controller partitioning is client-safe — verified, not assumed.** Every action declares `Name = RouteNames.*`; `OperationIdInvariantTransformer` (registered at `ApiHostServiceCollectionExtensions.cs:266`) rejects placeholder operationIds; `ContractInvariantsTests.OpenApiDocument_OperationIdsAreUnique` enforces uniqueness; `src/Explore.Blazor.Client/nswag.json` sets `operationGenerationMode: SingleClientFromOperationId`. OperationIds and generated `EventApiClient` method names are therefore independent of controller class names. `schemas/openapi_islamu-event.json` contains 756 operations.
- **`EventApiClient.g.cs` is 152,132 lines** of generated client. Repository-wide LOC is not a maintainability signal; the plan's rejection of LOC-percentage targets is correct.
- **17 files in `BackgroundServices` use `Task.Delay`.** `OutboxProcessor.cs` (243 lines) is the largest and is excluded from consolidation as the durable side-effect authority.
- **Test-project ownership gap.** `tests/Explore.Infrastructure.Tests/Identity/UserContextTests.cs` is named in the Phase 2 characterization as authoritative, but no phase in the previous plan ever ran `Explore.Infrastructure.Tests`. Likewise `Event.Persistence.IntegrationTests` owns outbox/retention semantics that Phase 5 changes and was never run. Both are now phase gates (plan §7).
- **Concurrent-workstream collision.** `dev/active/` holds 15 workstreams and `dev/pause/` holds 9. Six actively own the five hotspot controllers and eight background workers this plan targets. The previous artifacts recorded only that unrelated files were dirty; no overlap analysis existed. The binding matrix is now in `context.md`.

## Phase 1 Baseline

### Repository state

- The workspace contained unrelated modified and untracked files before implementation began. This workstream must not revert, stage, or rewrite them.
- Code-review graph architecture discovery succeeded, but its index reports `head_matches_build=false`; it is high-level discovery evidence only.

### External research

- Tavily MCP was attempted for source-free official/industry context and returned status `432` (plan usage limit exceeded).
- Context7 MCP was searched in the active tool inventory and was not available.
- No external content influenced implementation structure. Official URLs supplied for later verification:
  - <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0>
  - <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0>
  - <https://learn.microsoft.com/en-us/dotnet/core/extensions/timer-service>
  - <https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection>

### Automated baseline

- `dotnet build --configuration Release --verbosity quiet` exited 1 before this workstream changed runtime code, during restore, with 0 warnings and 0 errors.
- The post-change serial solution build reached `Explore.Blazor.Client` and failed in the installed WebAssembly SDK task host (`MSB4216`/`MSB4027`), outside the API/Application changes. Disabling build servers did not repair the SDK host.
- `src/Explore.API/Explore.API.csproj`, `src/Explore.Application/Explore.Application.csproj`, and `tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj` each compile successfully with `--no-restore`, Release, and serial MSBuild.
- The architecture executable runs, but the repository-wide suite currently has four unrelated dirty-worktree failures: registration-form input naming, an unapproved registration-form tenant-filter bypass, Blazor-owned registration-answer analytics DTOs, and two missing privacy inventory properties.

## Phase 1 Documentation Audit

- `docs/CODEBASE_STRUCTURE.md` and `docs/API.md` contain stale references to `schemas/openapi.json`; the current checked-in artifact and generated inventory use `schemas/openapi_islamu-event.json`.
- `docs/API_CONTRACT_INVENTORY.md` is generated, current, and must not be hand-edited.
- `docs/QUICK_REFERENCE.md` and `docs/index.md` already own adequate controller and navigation guidance; duplicating Phase 1 rules there would create documentation debt.

## Phase 1 Contract Pins

The five hotspot families already have layered contract authorities. Phase 1 adds no duplicate style test:

| Controller family | Current surface | Existing contract authorities |
|---|---|---|
| `EventController` | 26 actions covering discovery, management reads, calendar/OpenGraph, lifecycle, moderation, update, and delete | `ApiConventionTests.cs`, `ApiContractArchitectureTests.cs`, `EndpointClassificationArchitectureTests.cs`, `EventAuthorityControllerContractTests.cs`, `EventVisibilityContractTests.cs`, `EventControllerCalendarTests.cs`, `HateoasContractTests.cs` |
| `RegistrationOrderController` | 34 actions covering checkout, guest/authenticated attempts, participants, tickets, lifecycle, claim, and event management | `RegistrationOrderControllerTests.cs`, `NativeRegistrationOpenApiContractTests.cs`, `RegistrationOrderLinkPolicyTests.cs`, `EndpointAuthorizationMatrixTests.cs`, `ContractInvariantsTests.cs` |
| `WebhooksController` | 21 actions covering event types, consumers, endpoints, portal, messages/payloads, attempts, retry, and redrive | `WebhooksControllerTests.cs`, `WebhookPayloadNoStoreTests.cs`, `WebhookPortalHalAuthorityTests.cs`, `WebhookPortalHttpContainmentTests.cs`, `WebhookSensitiveSurfaceAbsenceTests.cs`, `EndpointClassificationArchitectureTests.cs` |
| `InstanceSettingsController` | 47 actions covering governance groups, storage/SMTP/provider tests, authentication/authorization, and settings operations | `InstanceOnboardingOpenApiContractTests.cs`, `InstanceSettingGroupApiTests.cs`, `InstanceStorageAdminControllerTests.cs`, `SetupSecretRateLimitMetadataTests.cs`, `EndpointAuthorizationMatrixTests.cs` |
| `ControlPlaneController` | 29 actions covering overview, plan/version management, tenant settings, assignments, tenant creation/lifecycle, and operations | `ControlPlaneControllerPolicyTests.cs`, `ControlPlaneSingleTenantSuppressionTests.cs`, `ManagedControlPlaneAuthenticationRoutingTests.cs`, `OpenApiParityTests.cs`, and `Hateoas/ControlPlane*Tests.cs` |

Global reflection tests pin API versioning, controller inheritance, `[ApiController]`, route presence, repository isolation, DTO-only responses, tenant-filter safety, unique route names, endpoint classification, and selected response metadata. Feature integration tests pin behavior-, security-, caching/no-store-, and HAL-specific contracts. A future controller-family partition must run both layers; source-level LOC or constructor syntax is deliberately not a contract.

## Phase 1 Dead-Path Disposition

- Bounded source/reference searches found no production caller or active DI registration for `Explore.API.Services.HeaderTenantResolver` or `Explore.API.Services.TenantContext`; the active composition root registers `Explore.Infrastructure.Services.TenantContext` and API tenant-resolution middleware.
- The only `HeaderTenantResolver` reference outside its file was a source-path guardrail. The API `TenantContext` had no type reference outside its file; same-name results resolved to infrastructure, Blazor, test, or `ExploreDbContext.TenantContext` symbols.
- `PermissionAction` typed `RequirePermission` overloads had zero call sites. The obsolete enum survived only through two bridge overloads, `ResourceDescriptorRegistry.ToActionString`, and its bridge-only architecture test; current link policies already use `AuthorizationActions` strings.
- Deleted both unwired API tenant services, the obsolete permission enum, its two overloads, mapper, and bridge-only test. Updated the organization guardrail and canonical tenant/auth test documentation atomically.
- Route constants, controller helpers, and generic HAL registrations were retained because the bounded audit did not prove them dead. In particular, repeated generic registration source lines belong to different helper paths and are not evidence of duplicate runtime registrations.

## Phase 1 Mechanical Adapter Reduction

- Twenty-eight controllers with a single assignment-only `IMediator` field and constructor were converted to primary constructors.
- Action signatures, attributes, request construction, return paths, base classes, and authorization/HAL metadata were not intentionally changed.
- The scoped controller diff is net `174` lines removed (`94` insertions, `268` deletions) because the field, constructor, and `_mediator` member accesses were replaced by the captured constructor parameter.
- Scoped `git diff --check` passed and the assigned cohort contains zero `private readonly IMediator _mediator` declarations.
- Independent controller-diff review approved the cohort with high confidence; a serial Release API build passed with zero errors.

## Phase 1 Targeted Verification

- The affected `TenantResolversMustNotUse_OrganizationIdentifiersAsInputs` and `AllLinkPoliciesHaveExplicitPermissionActions` architecture tests passed (2/2) from the compiled TUnit executable.
- Scoped `git diff --check` passed for API, Application authorization, architecture tests, documentation, and workstream artifacts.
- The two canonical repository-wide phase gates remain unchecked because of the pre-existing SDK-host and unrelated architecture-suite failures recorded above; no test was weakened to hide them.

## Phase 2 Identity Characterization

| Identity path | Current callers | Required authority and disposition |
|---|---|---|
| Authenticated platform user | `CurrentUserId`, `RequiredUserId`, and `UserContext` are used by 17 controllers; `ResolveCurrentUserIdAsync` is used by 11 controller families when provider subjects are not internal GUIDs | `IUserContext` remains the request-scoped claim authority. Inject it explicitly; preserve the documented `sub → nameidentifier → sid` fallback and the local `internal_user_id` claim without service location. Provider-to-local lookup remains the `ResolveCurrentUserIdByIdentityRequest` Application query. |
| Provider bootstrap/sync | `InstanceOnboardingController`, `UserController`, and `ExploreControllerBase` duplicate subject, provider, provider-id, email, name, and email-verification parsing | Move this parsing behind one trusted identity contract/query. Preserve ATProto DID selection, Google/Keycloak detection, verified-email defaults, UUIDv7 allocation during first instance claim, and `401` when no provider identity exists. |
| Machine/API-key principal | `ApiKeyAuthenticationHandler`, `ApiAuthenticationPrincipalExtensions`, `IMachinePrincipalAccessor`, tenant middleware, and authorization providers | Keep separate from human `IUserContext`. API-key owner/tenant/scope claims are purpose-built machine authority; do not coerce them into user IDs or provider bootstrap identity. |
| Purpose-bound bootstrap/session principals | `AtprotoSessionController`, ATProto authentication handlers/JWT service, setup-secret handler, and managed-control-plane controller | Keep their exact claim parsing at the authentication boundary. DID, method/path, tenant, managed-instance, and replay claims are protocol validation, not ordinary user-context fallbacks. |
| Privacy-erasure receipt | `PrivacyErasureController` reads the receipt intent claim | Keep isolated in the receipt authentication scheme; it must never become ambient user identity or reveal subject existence. |
| Diagnostic-only display | `AdminCacheDiagnosticsController` exposes bounded claim slots only when diagnostics are enabled in Development/Testing | Explicit display-safe claim reads may remain, but local-user resolution and provider classification must use the trusted identity authority. No tokens or arbitrary claim dump. |
| Controller service location | `ExploreControllerBase` resolves `IUserContext`; `InstanceOnboardingController` and `InstanceSettingsController` resolve auth/authz configuration services | Replace with constructor injection. Middleware/handlers may resolve scoped services from the request/scope where the framework boundary owns the scope; Phase 2 prohibition is controller-specific. |
| Footer private parsing | `FooterController.TryGetCurrentUserId` repeats unauthorized mapping around the base identity | Replace with the explicit user context and the existing ProblemDetails helper; delete the private parser after all six callers migrate. |

Contract tests that must remain authoritative include `Explore.Infrastructure.Tests/Identity/UserContextTests.cs`, `Event.API.IntegrationTests/Features/UserControllerTests.cs`, `InstanceOnboardingControllerTests.cs`, `UserExternalLoginIntegrationTests.cs`, `TenantStorageSettingsControllerTests.cs`, `ManagedControlPlaneAuthenticationRoutingTests.cs`, and ATProto authentication/session tests. Ordinary-identity changes must not rewrite the purpose-bound API-key, setup-secret, managed-control-plane, ATProto, or receipt schemes.

### Phase 2.2 first migration slice

- `InstanceOnboardingController` now receives `IAuthProviderConfigurationService` through its constructor instead of resolving it from `HttpContext.RequestServices` for public and setup-secret configuration reads.
- `InstanceSettingsController` now receives both auth and authorization provider configuration services through its constructor instead of service-locating them for readiness endpoints.
- Direct API and API-integration-test project builds pass in Release with serial MSBuild; the eight affected controller tests pass.
- The only remaining controller `HttpContext.RequestServices` use is the base `IUserContext` locator. Phase 2.2 remains open until ordinary identity injection/provider parsing is centralized and the base locator is deleted.
- Independent review approved the dead-path removals and constructor-injection slice with no findings; active tenant and configuration-service DI registrations remain intact.
