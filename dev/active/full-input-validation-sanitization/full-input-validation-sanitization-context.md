<!-- ABOUTME: Session context and evidence log for the full input validation and sanitization planning workstream. -->
<!-- ABOUTME: Captures source documents, agent findings, CTO feedback, decisions, blockers, and quick-resume notes for future implementation sessions. -->

# Full User Input Validation & Sanitization Context

Last Updated: 2026-05-29 Europe/Brussels

## SESSION PROGRESS (2026-05-29 Europe/Brussels)

### ✅ COMPLETED

- Continued the input validation/sanitization implementation workstream only; did not work on TickerQ/scheduler and did not manually edit EF migration files.
- Implemented legacy direct storage presigned-upload request validation in the Application layer:
  - `UploadRequestDto` now has the required two-line ABOUTME header and remains a narrow browser-input DTO.
  - `UploadRequestDtoValidator` rejects empty/overlong filenames and content types, control characters, path separators, dot segments, malformed MIME types, and wildcard MIME types.
  - `GenerateUploadUrlCommandHandler` manually instantiates the validator, calls `ValidateAsync`, throws `FluentValidation.ValidationException` before side effects, and calls `IObjectStorageService` only with normalized values.
- Added focused unit coverage for validator edge cases and handler pre-storage side-effect behavior.
- Updated the input matrix and tasks to mark the legacy direct presign filename/content-type slice implemented while keeping storage metadata/object-key semantics pending.
- Attempted Context7 again for ASP.NET Core validation guidance; the server returned monthly quota exhaustion, so this slice continued from repository conventions and previously recorded ASP.NET Core/FluentValidation guidance.

### 🟡 IN PROGRESS

- Overall hardening remains in progress. The next executable area should stay inside `full-input-validation-sanitization-input-matrix.md` rows and should avoid scheduler/TickerQ and generated migration files.
- `StorageObjectController` upload APIs are only partially complete: direct presign request shape is covered; `CreateStorageObjectDto`, `UpdateStorageObjectDto`, object-key semantics, metadata size/shape, tenant leakage, and controller/API contract tests still need row-by-row work.

### ⏭️ NEXT

1. Continue Slice 3/4 write-request hardening from the matrix, preferably storage metadata/object-key validators or another high-risk DTO row that already has a clear owner and tests.
2. For storage metadata/object-key work, inspect `CreateStorageObjectDtoValidator`, `UpdateStorageObjectDtoValidator`, `StorageObjectController`, storage handlers, and storage repository semantics before editing.
3. Add handler/API tests proving validation occurs before persistence/storage side effects and does not leak tenant/object existence.
4. Regenerate OpenAPI/generated client only in the explicit Slice 9 step after API DTO shapes stabilize.

### ⚠️ BLOCKERS

- Context7 MCP quota is exhausted until the provider resets quota; document this if further library research is required.
- Current architecture gate is red in the dirty workspace after the handoff update: `Queries_ShouldResideIn_QueriesNamespace` fails in `Event.Architecture.Tests/CqrsPatternTests.cs`. The failure is unrelated to the markdown handoff and should be investigated before claiming a fully green architecture gate.
- The workspace contains extensive unrelated dirty changes, including TickerQ/scheduler files, regenerated migration churn, local-first storage work, actor-subscription work, CI/docs changes, and deleted unrelated active docs. Do not revert or fold those into this workstream without explicit user instruction.

## Handoff — 2026-05-29 Europe/Brussels

### Current State

- What is completed: legacy direct storage presigned-upload request validation now runs in the Application command handler before object-storage side effects. Validator and handler tests are green. Active matrix/tasks/context docs are refreshed for this slice.
- What is in progress: broader input validation/sanitization remains active. Storage upload APIs are partially complete: direct presign filename/content-type shape is implemented; storage object metadata/object-key/tenant semantic checks remain.
- What changed since the last handoff: this handoff supersedes the old planning-only handoff. Implementation has now completed several validation slices, including this session's storage direct-presign hardening.

### Next Action

1. Read `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `.claude/contract/intents.yaml`, `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md`, `docs/API.md`, `docs/SECURITY-MODEL.md`, and this workstream's plan/context/tasks/matrix.
2. Continue from `full-input-validation-sanitization-input-matrix.md`, choosing a row marked ready/partially implemented rather than restarting broad planning.
3. Recommended next slice: finish storage metadata/object-key validation by inspecting `CreateStorageObjectDtoValidator`, `UpdateStorageObjectDtoValidator`, storage command handlers, `StorageObjectController`, and tenant/object-key repository semantics.

### Blockers

- Context7 MCP returned monthly quota exhaustion during this session.
- No code blocker remains for the direct presign slice.
- Full-workspace ownership is mixed; do not take over TickerQ/scheduler, unrelated generated migrations, or unrelated active-doc deletions.

### Modified Files

- `Explore.Application/DTOs/StorageObject/UploadRequestDto.cs` — added required ABOUTME header and kept the direct presign request DTO narrow.
- `Explore.Application/DTOs/StorageObject/Validators/UploadRequestDtoValidator.cs` — hardened filename/content-type validation and added normalization helpers.
- `Explore.Application/Features/StorageObjects/Handlers/Commands/GenerateUploadUrlCommandHandler.cs` — manually validates normalized input before calling `IObjectStorageService`.
- `Event.Application.UnitTests/Features/StorageObjects/Validators/UploadRequestDtoValidatorTests.cs` — new validator tests for unsafe filenames and MIME types.
- `Event.Application.UnitTests/Features/StorageObjects/Commands/GenerateUploadUrlCommandHandlerTests.cs` — new handler tests proving validation runs before storage side effects.
- `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-plan.md` — re-baselined from planning-only to active implementation state.
- `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-context.md` — refreshed current state, validation evidence, and this handoff.
- `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-tasks.md` — marked direct presign validation and verification complete; kept remaining storage/API work visible.
- `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-input-matrix.md` — marked the `StorageObjectController` upload row partially implemented for direct presign request validation.

### Validation

- Commands run:
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/UploadRequestDtoValidatorTests/*' --maximum-parallel-tests 1` — passed, 11 total, 11 succeeded.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/GenerateUploadUrlCommandHandlerTests/*' --maximum-parallel-tests 1` — passed, 3 total, 3 succeeded.
  - `git diff --check -- <storage validation files and active docs>` — passed.
  - `dotnet build Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --property:WarningLevel=0 /clp:ErrorsOnly` — passed, 0 errors.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed earlier for the storage validation slice with 181 total, 180 succeeded, 1 skipped.
  - `dotnet build --configuration Release --verbosity quiet --property:WarningLevel=0 /clp:ErrorsOnly` — first failed on a transient WebAssembly Webcil file lock.
  - `dotnet build --configuration Release --verbosity quiet --property:WarningLevel=0 --property:BuildInParallel=false --maxcpucount:1 /clp:ErrorsOnly` — passed, 0 errors.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — latest rerun after handoff markdown edits failed: 181 total, 179 succeeded, 1 skipped, 1 failed. Failing test: `Queries_ShouldResideIn_QueriesNamespace`.
- Commands still needed:
  - Investigate and fix or isolate the current architecture failure before claiming the workspace is fully green.
  - Add API/controller-level validation tests when the next storage metadata/object-key slice touches the controller/API surface.
  - Add persistence integration tests when validation touches tenant/object-key lookup, uniqueness, quota, or repository behavior.

### Documentation Impact

- Updated active dev docs only. No canonical docs or journal entry were needed for this slice.
- Future DTO/API contract changes may require `docs/API_CHANGELOG.md`, `schemas/openapi.json`, and regenerated `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.

### Risks

- Source-grounding risks: storage metadata/object-key semantics are not complete; next agent must inspect current storage model/handler/repository code before editing.
- Test or build risks: repo-wide dirty state includes unrelated generated-client whitespace and migration churn; use scoped checks for owned files and full build only after understanding concurrent changes.
- Operator/release risks: direct presigned upload remains a legacy path; BFF upload-session/proxy is more hardened, but API storage metadata semantics still need tenant/object-key leak tests.

### Notes For Next Contributor Or Agent

- Required docs/rules to read: `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/API.md`, `docs/SECURITY-MODEL.md`, `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md`, and this workstream's plan/context/tasks/matrix.
- Assumptions made: development mode allows breaking DTO/API changes; validators stay manually instantiated; canonicalization stays outside validators except pure normalization helpers used before validation in the handler.
- Do not touch / unrelated dirty files: do not edit generated EF migration files manually; do not work on TickerQ/scheduler in this validation workstream; preserve unrelated active-doc deletions and generated-client changes unless the user explicitly assigns them.

## SESSION PROGRESS (2026-05-28 Europe/Brussels)

### CONTINUATION UPDATE (2026-05-28 Europe/Brussels)

- Loaded the active workstream docs plus canonical API, architecture, security, Blazor, governance, operations, quick-reference, application-layer, and API-controller rules before editing.
- Used Context7 as requested:
  - `/dotnet/aspnetcore.docs`: `[ApiController]` automatically returns HTTP 400 `ValidationProblemDetails` for model-state failures and can be customized with `InvalidModelStateResponseFactory`.
  - `/fluentvalidation/fluentvalidation`: async rules require `ValidateAsync`; synchronous validation throws when async rules exist in FluentValidation 11+; ASP.NET automatic validation is not suitable for async rules.
  - `/dotnet/aspnetcore.docs`: Blazor server-validation errors can be mapped back into `EditContext`/validation stores; unsafe cookie-auth form/BFF submissions need antiforgery; uploads require size/count/type/path validation and safe file-name handling.
- Ran the required session-start build: `dotnet build --configuration Release --verbosity quiet` failed before implementation edits with two generated-client anonymous type mismatches in `Explore.Blazor.Client.Tests/Services/CustomPropertyAdminServiceTests.cs`; many pre-existing warnings were also emitted. Treat this as pre-existing until touched.
- Locked Slice 1 decisions D-001 through D-018 as accepted contracts:
  - normalize automatic API model-state errors into the repository ProblemDetails shape;
  - reject malformed JSON/missing bodies/wrong content type safely;
  - reject unknown write-body properties pre-v1 and remove server-owned fields from request DTOs;
  - require explicit string bounds;
  - validate public GET/query inputs;
  - add idempotency request fingerprinting and reject same-key/different-payload reuse;
  - do not cache validation/model-binding failures for idempotency;
  - keep BFF tokens/setup secrets browser-invisible;
  - bind upload proxy destinations to server-issued opaque sessions;
  - disable user-authored rich HTML unless a sanitizer decision record exists;
  - restrict validation telemetry to safe metadata; and
  - keep Blazor validation UX-only with server errors mapped into `EditContext`.
- Expanded the Slice 2 matrix with grouped endpoint-family rows from `docs/API_CONTRACT_INVENTORY.md` and current repo searches:
  - public lookup/detail routes;
  - public and authenticated pagination/query patterns;
  - custom-property runtime/projection/governance surfaces;
  - template sync;
  - settings/governance/onboarding;
  - footer/navigation;
  - actor/org/group/user/member/role;
  - external API keys;
  - analytics relay;
  - email dispatch admin;
  - localization admin;
  - UI theme/appearance;
  - storage presigned download/upload;
  - idempotency/correlation/tenant headers.
- Updated `full-input-validation-sanitization-tasks.md`: Slice 1 is complete; Slice 2 is partially complete with `Ready for slice` and `Pending inventory` rows clearly separated.
- Verified the continuation changes:
  - `git diff --check -- dev/active/full-input-validation-sanitization/...` passed with no whitespace errors.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 179 total, 178 succeeded, 1 skipped, 0 failed.

### CONTINUATION NEXT

1. Start implementation only from matrix rows marked `Ready for slice`, especially API ProblemDetails normalization, unknown-property rejection, public query validators for `EventFilterRequest` / `EventSessionFilterRequest`, shared pagination bounds, and idempotency fingerprinting.
2. Keep `Pending inventory` rows as matrix refinement work before broad DTO/UI churn.

### IMPLEMENTATION UPDATE (2026-05-28 Europe/Brussels)

- Implemented the first API input-error contract slice from the `Ready for slice` rows:
  - added `Explore.API/ExceptionHandling/ApiValidationProblemDetailsFactory.cs` to normalize automatic `[ApiController]` model-state failures into the repository RFC7807 shape;
  - configured MVC `ApiBehaviorOptions.InvalidModelStateResponseFactory` in `Explore.API/Program.cs`;
  - configured `System.Text.Json` unknown-property rejection with `JsonUnmappedMemberHandling.Disallow`;
  - added a 415 status-code bridge in `Explore.API/Extensions/ExceptionHandlingExtensions.cs` so unsupported content types receive the same safe ProblemDetails envelope;
  - added API integration contract tests in `Event.API.IntegrationTests/Features/ProblemDetailsContractTests.cs` for malformed JSON, missing body, unsupported content type, unknown properties, and over-posted HAL `_links`.
- The model-state factory intentionally redacts parser/exception-backed messages into a generic body-level error: `Request body is invalid or contains unsupported fields.` This prevents raw JSON snippets, rejected property values, parser internals, or over-posted field contents from becoming response data.
- Fixed the pre-existing Blazor generated-client test compile mismatch in `Explore.Blazor.Client.Tests/Services/CustomPropertyAdminServiceTests.cs` by aligning generated HAL `_links` anonymous types with the current generated client.
- Verification completed before the unrelated scheduler lane changed:
  - `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and pre-existing warnings.
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed: 1210 total, 1209 succeeded, 1 skipped.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 179 total, 178 succeeded, 1 skipped.
- API integration verification is not green yet:
  - full `Event.API.IntegrationTests` failed before reaching this contract slice because startup guardrail tests hit `ExecuteDeleteAsync` in `DatabaseSeeder.RemovePreviousDevelopmentEventCatalogAsync`, which the in-memory provider does not support;
  - a later targeted TUnit run needed `--treenode-filter` rather than VSTest `--filter`, but targeted API verification was then blocked by concurrent scheduler package/API work outside this validation lane.
- Do not take over scheduler/TickerQ work in this validation thread. Continue input-validation work by either adding pure unit tests around the new factory or moving to public query validators once the separate scheduler lane restores API build stability.

### PUBLIC QUERY VALIDATION UPDATE (2026-05-28 Europe/Brussels)

- Continued Slice 4 on public discovery query hardening only; no scheduler/TickerQ files were touched.
- Added `Explore.API/Models/QueryValidationRules.cs` as the shared API transport validation helper for query-bound DTOs. It validates:
  - page number and page size against `PaginatedResult<T>.MaxPageSize`;
  - bounded text/search fields and control-character rejection;
  - sort allowlist: `date`, `title`, `views`, `createdAt`;
  - filter mode allowlist: `and`, `or`;
  - `DateFrom <= DateTo`;
  - list cardinality ceilings;
  - positive `int` lookup IDs;
  - non-empty GUID IDs and GUID lists;
  - custom-property filter count, namespace/key/value bounds, required operands by operator, option GUID integrity, and numeric/date range ordering.
- `EventFilterRequest` and `EventSessionFilterRequest` now implement `IValidatableObject`, letting ASP.NET Core query model validation feed the existing `[ApiController]` automatic 400 path and the custom ProblemDetails response factory. This keeps controllers thin and avoids introducing API-layer FluentValidation/DI validator registration.
- Added `ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)` metadata to the public event and event-session list endpoints.
- Added `Event.API.IntegrationTests/Features/PublicQueryValidationTests.cs` with focused DTO-level regression tests for invalid pagination, unknown sort/mode values, inverted date ranges, invalid lookup IDs, empty GUIDs, overlong search terms, and malformed custom-property filter operands.
- Context7 confirmation used during this slice: `/dotnet/aspnetcore.docs` documents that `[ApiController]` automatically returns HTTP 400 for model validation errors and that `ApiBehaviorOptions.InvalidModelStateResponseFactory` customizes that response path.
- Verification status:
  - `git diff --check -- Explore.API/Models/QueryValidationRules.cs Explore.API/Models/EventFilterRequest.cs Explore.API/Models/EventSessionFilterRequest.cs Explore.API/Controllers/EventController.cs Explore.API/Controllers/EventSessionController.cs Event.API.IntegrationTests/Features/PublicQueryValidationTests.cs` passed.
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/PublicQueryValidationTests/*' --maximum-parallel-tests 1` passed with 10 total, 10 succeeded, 0 failed.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 179 total, 178 succeeded, 1 skipped.
  - `dotnet build --configuration Release --verbosity quiet` failed outside this validation slice because `Event.Persistence.IntegrationTests/Repositories/EmailDispatchOutboxTransitionRepositoryTests.cs` references `EmailDispatchReceipt.StartedAt`, which is not present on the current domain model.

### IDEMPOTENCY FINGERPRINTING UPDATE (2026-05-29 Europe/Brussels)

- Continued Slice 4/5 on the matrix row `Idempotency and cross-cutting headers`; no TickerQ or scheduler files were edited.
- Used Context7 `/dotnet/aspnetcore.docs` to re-confirm the ASP.NET Core middleware patterns relevant to this slice:
  - request bodies are streams and must be handled carefully when middleware reads them before MVC model binding;
  - middleware can write RFC7807 responses through `IProblemDetailsService`.
- Implemented request identity metadata for replay validation:
  - `Explore.API/Middleware/IdempotencyRequestIdentity.cs` computes method, route pattern/path plus query, normalized content type, SHA-256 body hash, authenticated/anonymous principal fingerprint, and user id;
  - JSON bodies are canonicalized with `JsonDocument`/`Utf8JsonWriter` and ordinally sorted object properties before hashing, so formatting and property order do not break legitimate retries;
  - malformed JSON falls back to raw-body hashing and still lets downstream model binding produce the safe validation response.
- Updated `Explore.API/Middleware/IdempotencyMiddleware.cs`:
  - computes the request identity before repository lookup;
  - persists identity metadata on eligible idempotency records;
  - replays only when method, target, content type, body hash, and principal fingerprint match;
  - returns HTTP 409 ProblemDetails with extension `code = idempotency_key_reuse` for same-key reuse with a different request;
  - no longer persists validation/model-binding-style `400` responses or unsupported-content-type `415` responses.
- Updated EF model ownership without manually editing generated migrations:
  - `Explore.Domain/IdempotencyRecord.cs` owns the new persisted fields;
  - `Explore.Persistence/Configurations/Entities/IdempotencyRecordConfiguration.cs` configures the new columns through EF Core fluent configuration;
  - generated migration files and snapshots are intentionally left to EF Core regeneration.
- Expanded `Event.API.IntegrationTests/Features/IdempotencyMiddlewareTests.cs`:
  - equivalent JSON payloads replay with `X-Idempotency-Replay: true`;
  - same key with different body, content type, route, method, or authenticated principal returns `409` and does not invoke the next delegate;
  - same key across different tenants does not replay across tenant scope;
  - validation failure responses are not persisted and a corrected retry can proceed.
- Verification:
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/IdempotencyMiddlewareTests/*' --maximum-parallel-tests 1` passed with 13 total, 13 succeeded, 0 failed.
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/IdempotencyRepositoryTests/*' --maximum-parallel-tests 1` passed with 1 total, 1 succeeded, 0 failed.
  - `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed with 0 errors and existing warnings.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 181 total, 180 succeeded, 1 skipped.
  - `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and existing warnings.

### SHARED PAGINATED QUERY VALIDATION UPDATE (2026-05-29 Europe/Brussels)

- Continued Slice 4 on the matrix row for standard paginated list/query pairs and adjacent high-risk query surfaces; no TickerQ/scheduler files or generated EF migration files were edited.
- Used Context7 `/dotnet/aspnetcore.docs` again for this slice. Relevant confirmation: `[ApiController]` automatically returns HTTP 400 for model-validation failures, and `IValidatableObject` supports class-level validation on bound models. The implementation intentionally uses query-bound API DTOs so invalid query input fails before MediatR handlers/repositories run.
- Added `Explore.API/Models/PaginatedQueryRequests.cs` with reusable and endpoint-specific query models:
  - `PaginationQueryRequest` for page/page-size bounds;
  - endpoint-specific wrappers for event series, templates, event/session custom-property definitions, notification filters, contact shared-contact search, custom-property governance reports, projection dirty scopes, and template sync history;
  - scalar query validation for required/non-empty GUIDs, optional positive lookup IDs, bounded search/email/projection text, enum support, and template-sync `page`/`pageSize`.
- Extended `Explore.API/Models/QueryValidationRules.cs` with shared scalar helpers for required GUIDs and optional positive integer filters.
- Converted raw `pageNumber`/`pageSize` controller parameters to validated query DTOs across the standard list surfaces:
  - actor, category, event/my, event registration, event series, agenda items, group/my groups, location, notification, organization/my organizations, storage object, tag;
  - custom-property definition, event custom-property definition, event-session custom-property definition, event template, event-session template;
  - contact shared contacts, custom-property governance report, projection dirty scopes, event template sync history, and event-session template sync history.
- Added `400` response metadata where the query-bound validation path now exists.
- Expanded `Event.API.IntegrationTests/Features/PublicQueryValidationTests.cs` from 10 to 19 validator contract tests, covering shared pagination bounds, event-series actor id, template lookup id, required parent/template GUIDs, notification lookup filters, contact email-search length, governance tenant/recommendation enum values, projection name presence, and template-sync history page/page-size.
- OpenAPI generation changed generated client parameter ordering for some query models. Production Blazor code was adjusted defensively with named generated-client arguments in `Explore.Blazor.Client/Services/EventSeriesService.cs`; `Explore.Blazor.Client.Tests/Services/NotificationServiceTests.cs` was updated to the regenerated notification client signature.
- Verification:
  - `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed with 0 errors and existing warnings.
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/PublicQueryValidationTests/*' --maximum-parallel-tests 1` passed with 19 total, 19 succeeded, 0 failed.
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/NotificationServiceTests/*' --maximum-parallel-tests 1` passed with 40 total, 40 succeeded, 0 failed.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 181 total, 180 succeeded, 1 skipped.
  - `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed with 0 errors and existing warnings.
  - `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and existing warnings.

### LEGACY STORAGE DIRECT PRESIGN VALIDATION UPDATE (2026-05-29 Europe/Brussels)

- Continued the input validation/sanitization workstream on the matrix row `StorageObjectController upload APIs`; no TickerQ/scheduler files and no generated EF migration files were edited.
- Context7 was attempted again for ASP.NET Core validation guidance, but the server returned monthly quota exhaustion. The implementation therefore followed repository conventions plus the previously recorded Context7 findings for manual FluentValidation and ASP.NET Core ProblemDetails behavior.
- Hardened the legacy direct presigned upload request path:
  - `Explore.Application/DTOs/StorageObject/UploadRequestDto.cs` now has the required ABOUTME header and remains a narrow browser-input DTO with only `FileName` and `ContentType`;
  - `Explore.Application/DTOs/StorageObject/Validators/UploadRequestDtoValidator.cs` now rejects empty/overlong values, control characters, path separators, dot-segments, malformed media types, and wildcard media types;
  - the validator exposes deterministic normalization helpers for trimming file names/content types and using `MediaTypeHeaderValue` parsing for content-type canonicalization;
  - `Explore.Application/Features/StorageObjects/Handlers/Commands/GenerateUploadUrlCommandHandler.cs` manually instantiates the validator, uses `ValidateAsync`, throws `FluentValidation.ValidationException` on invalid input, and calls `IObjectStorageService` only after normalization and validation pass.
- Added focused regression tests:
  - `Event.Application.UnitTests/Features/StorageObjects/Validators/UploadRequestDtoValidatorTests.cs` covers valid input, path traversal/path-style filenames, dot segments, control characters, malformed content types, and wildcard content types;
  - `Event.Application.UnitTests/Features/StorageObjects/Commands/GenerateUploadUrlCommandHandlerTests.cs` proves valid input is normalized before storage use and invalid input fails before the object storage service is called.
- The current workspace also contains the minimal actor-subscription DTO alignment in `Explore.Application/DTOs/ActorSubscription/ActorSubscriptionListDto.cs`, keeping the list DTO compatible with `ResourceDescriptors.ActorSubscriptionList` tenant-scope metadata. This is unrelated to storage validation behavior but required for the current Application test project to compile.
- Targeted verification:
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/UploadRequestDtoValidatorTests/*' --maximum-parallel-tests 1` passed with 11 total, 11 succeeded, 0 failed.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/GenerateUploadUrlCommandHandlerTests/*' --maximum-parallel-tests 1` passed with 3 total, 3 succeeded, 0 failed.
  - `git diff --check -- ...` over the storage validation files, new tests, and active workstream docs passed with no whitespace errors.
  - `dotnet build Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --property:WarningLevel=0 /clp:ErrorsOnly` passed with 0 errors.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 181 total, 180 succeeded, 1 skipped.
  - `dotnet build --configuration Release --verbosity quiet --property:WarningLevel=0 /clp:ErrorsOnly` first failed on a transient WebAssembly Webcil file lock for `Explore.Blazor.Client.dll`; rerun with `--property:BuildInParallel=false --maxcpucount:1` passed with 0 errors.

### ✅ COMPLETED

- Classified the task as `/dev-docs` search/analyze mode: create persistent implementation planning docs only; do not implement code changes.
- Confirmed target directory: `dev/active/full-input-validation-sanitization/`.
- Reviewed repository agent contract, relevant intents, canonical docs, path rules, and project skills.
- Attempted Tavily MCP research as requested; Tavily failed due quota/usage limit (`status:432`). Compensated with Context7, OWASP official cheat sheets, Microsoft/source-backed research from librarian, and repository docs.
- Queried Context7 for FluentValidation and MudBlazor.
- Collected Blazor input/form inventory from the first explore agent.
- Collected backend validation/sanitization inventory from the first explore agent.
- Collected external validation/sanitization recommendations from the librarian agent.
- Drafted initial plan/tasks/context docs for this workstream.
- Consulted Oracle on the initial plan and applied corrections for model-state responses, Blazor validator ownership, BFF validation seams, authorization exceptions, canonicalization ownership, and verification split.
- Incorporated CTO feedback: converted the plan into a pre-v1 hardening milestone with contract lock, required input surface matrix, release slices, public read/query validation, tenant-leakage prevention, BFF hard gates, canonicalization policy, idempotency tests, rich-text ban, observability-safe telemetry, and enterprise DoD gates.
- Collected final input-surface and Blazor-form background agent findings.
- Completed the second Oracle review on the CTO-integrated docs and applied required cleanup.
- Re-ran the authoritative docs/architecture gate: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 179 total, 178 succeeded, 1 skipped, 0 failed.
- Continued the workstream by creating Slice 1/2 starter artifacts: `full-input-validation-sanitization-contract-decisions.md` and `full-input-validation-sanitization-input-matrix.md`.
- Re-ran the authoritative docs/architecture gate after adding Slice 1/2 artifacts: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 179 total, 178 succeeded, 1 skipped, 0 failed.
- Refreshed this context for session handoff. Targeted git status shows `dev/active/full-input-validation-sanitization/` is currently untracked as a workstream directory, while the repository has many unrelated dirty files outside this workstream.
- Re-ran handoff validation after the context/tasks refresh: markdown diff check passed with no whitespace errors, and `Event.Architecture.Tests` passed with 179 total, 178 succeeded, 1 skipped, 0 failed.
- Re-ran the authoritative docs/architecture gate after the handoff refresh: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 179 total, 178 succeeded, 1 skipped, 0 failed.

### 🟡 IN PROGRESS

- None. This is a handoff-only update; no implementation code has been changed in this workstream.

### ⏭️ NEXT

1. Resolve Slice 1 contract decisions in `full-input-validation-sanitization-contract-decisions.md` until each row is `Accepted` or explicitly deferred with rationale and tests.
2. Expand `full-input-validation-sanitization-input-matrix.md` until every API, BFF, Blazor, upload, query, idempotency, and raw-rendering input has a row with owner, threat model, priority, decision status, and tests.
3. Only after the contract log and matrix gates are satisfied, begin Slice 3 DTO hardening and subsequent implementation slices in `full-input-validation-sanitization-tasks.md`.

### ⚠️ BLOCKERS / WATCH ITEMS

- `docs/UI_GOVERNANCE.md` and `docs/DESIGN_SYSTEM.md` conflict around primitive wrapper components. Resolve before broad form refactoring.
- `Explore.Blazor.Client` cannot blindly reference `Explore.Application` validators due a previously documented FluentValidation version/reference conflict and WASM bundle-size concern.
- `docs/UI_GOVERNANCE.md` expects FluentValidation-backed forms. If implementation chooses documented server-only validation for complex cross-resource rules, update UI governance explicitly.
- Automatic `[ApiController]` model-state/malformed JSON/content-type response shape is not normalized today; must test or deliberately customize.
- Idempotency middleware currently validates key length/whitespace and tenant-scoped replay, but same-key/different-payload behavior needs a policy decision and tests.
- Final Oracle review found the core architecture direction sound but required stale status cleanup, explicit matrix columns for threat model/release slice/status, explicit BFF token-boundary gates, and a recorded final validation run.
- The workstream directory is untracked in git at handoff time. Do not assume unrelated dirty files elsewhere belong to this validation workstream.

## Key Contract Rules

- Every file starts with two `ABOUTME:` lines.
- Repositories return entities, never DTOs.
- Validators are manually instantiated; do not inject `IValidator<T>` through DI.
- Use `Guid` for aggregates, `int` for lookups, `long` for cursors.
- GET endpoints are generally `[AllowAnonymous]`; write endpoints are `[Authorize]` unless documented bootstrap/internal exceptions have compensating controls.
- HAL `_links` are the only Blazor UI action-affordance source of truth.
- API errors use RFC 7807 ProblemDetails via chained `IExceptionHandler` implementations where applicable.
- Browser never sees tokens; BFF owns OIDC cookies and server-side token forwarding.
- Blazor forms should use `EditForm`/`EditContext` and the shared form submission/error primitives; `MudForm` is banned by current UI governance.
- Tenant filters must not be bypassed for runtime validation; validation messages must not leak cross-tenant existence.

## Evidence Base

### Repository Docs

- `AGENTS.md`: contribution contract, critical rules, baseline build command, final teaching summary requirement.
- `.claude/contract/intents.yaml`: no exact validation/sanitization intent; compound mapping to `add-write-endpoint`, `add-cqrs-handler`, `openapi-contract-change`, `blazor-component-affordance`, and optionally `add-hal-link`.
- `docs/QUICK_REFERENCE.md`: manual validators, ProblemDetails, middleware order, controller authoring standard, route names, HAL links, individual test policy.
- `docs/GOVERNANCE.md`: Clean Architecture ownership, validation as FluentValidation in Application, API contract rules, OpenAPI regeneration.
- `docs/API.md`: fixed middleware order, thin controllers, `[Authorize]` writes with documented exceptions, MediatR authorization, HAL affordances, chained exception handlers, idempotency, OpenAPI/client regeneration.
- `docs/ARCHITECTURE.md`: Clean Architecture + CQRS + BFF, manual handler validation, repositories return entities, MediatR `PerformanceBehavior`/`AuthorizationBehavior` but no validation pipeline.
- `docs/SECURITY-MODEL.md`: BFF trust boundary, safe diagnostics, antiforgery, upload proxy SSRF protections, safe serialized auth state, API-key scope validation, fail-closed HAL.
- `docs/BLAZOR.md`: BFF/client boundaries, YARP proxy/token forwarding, services over generated clients, common pitfalls.
- `docs/UI_GOVERNANCE.md`: canonical form architecture, `FormSubmitState`, `FormSubmissionGuard`, `ServerValidationErrorStore`, `AppValidationSummary`, banned `_isSaving`/`MudForm` patterns.
- `docs/OPERATIONS.md`: rate limits, request timeouts, correlation ID, security headers, safe operational logging.
- `docs/ACCESSIBILITY.md`: labels, `role="alert"`, `role="status"`, focus services, form/dialog accessibility tests.
- `docs/DESIGN_SYSTEM.md`: CSS layers, error/success tokens, MudBlazor override policy, wrapper-doc conflict with UI governance.
- `docs/CODEBASE_STRUCTURE.md`: project graph and validation/error-handling locations.
- `dev/_journal/MAJOR_DECISIONS.md`: `ValidationBehavior` deleted; manual validation is the repository decision.

### Skills and Rules

- `clean-architecture-rules`: Application references Domain only; validators manual; HATEOAS in API; repos return entities.
- `cqrs-mediatr-guidelines`: validators manually instantiated in handlers; commands/queries stay separate; cancellation tokens end-to-end.
- `auth-patterns`: BFF token secrecy, claim fallback, endpoint auth defaults, handler resource authorization, HAL fail-closed gating.
- `blazor-ui-conventions`: InteractiveAuto, MudBlazor v9 APIs, HAL action gating, services/components boundaries.
- `blazor-bff-patterns`: YARP proxy, server-side token forwarding, CSRF on state-changing routes, trusted tenant/setup-secret forwarding.
- `error-tracking`: chained exception handlers, ProblemDetails extensions, no swallowed exceptions, safe logging/metrics.
- `dotnet-efcore-guidelines`: tenant/soft-delete filters, repo boundaries, migration discipline.

### External Research

- Context7 `/fluentvalidation/fluentvalidation`: manual validation; `ValidateAndThrow`/`ValidateAndThrowAsync`; async rules require `ValidateAsync`.
- Context7 `/mudblazor/mudblazor`: weak/noisy for forms; repository docs are more authoritative and currently ban `MudForm`.
- OWASP Input Validation Cheat Sheet: validate all untrusted input early/server-side, allowlists over denylists, syntactic and semantic validation, safe regex, Unicode normalization, upload validation.
- OWASP XSS Prevention Cheat Sheet: context-aware output encoding, sanitize only user-authored rich HTML, safe sinks, avoid global interceptors and dangerous contexts.
- Librarian research: Microsoft `ApiController`/ProblemDetails guidance, FluentValidation manual/async guidance, Blazor validation as UX/server authority, antiforgery for unsafe browser routes, file-upload hardening, HTTP logging redaction, idempotency guidance from IETF/Stripe.

## CTO Feedback Integrated

The CTO feedback reframed the plan from “full validation cleanup” to an executable hardening milestone. Required changes applied:

- Added hard gate that implementation starts only after contract lock and a required input surface matrix.
- Reordered phases into enterprise slices: contract lock, inventory matrix, DTO hardening, Application validators, persistence semantic tests, BFF unsafe endpoint audit, Blazor form convergence, raw rendering/sanitizer policy, OpenAPI/client regeneration, CI gates.
- Added exact matrix columns: Route/endpoint/form; Auth level; Tenant context source; DTO/UI model; Validator owner; Canonicalization owner; Rich text?; Error contract; Tests; Abuse cases.
- Added public read/query validation for search terms, slugs, pagination, filters, sort fields, date ranges, custom-property filters, module-specific filters, and cursor values.
- Added tenant-leakage prevention: validate IDs inside caller tenant scope, prefer “not found or not accessible,” never bypass tenant filters, test Tenant A IDs submitted by Tenant B.
- Moved BFF validation earlier with explicit hard gates for CSRF, setup-secret spoofing, upload session replay/owner/content-type/presigned URL issues, and raw IdP error leakage.
- Restricted Blazor validation to UI-local syntactic checks; complex Application rules remain server-only and map back into `EditContext`.
- Added canonicalization normalizer policy and idempotency tests.
- Strengthened rich-text policy: plain text by default, rich HTML unsupported without sanitizer decision record and regression tests.
- Added negative API contract suite and idempotency replay/mismatch tests.
- Added observability-safe telemetry constraints.
- Added enterprise DoD gates.

## Direct Evidence from CTO Update Session

- `ast_grep_search` for `[FromQuery] $TYPE $NAME` found 104 matches across API/Application, confirming public/read/query inputs are numerous.
- Representative `[FromQuery]` surfaces include `EventController.GetAll([FromQuery] EventFilterRequest filter)`, `EventSessionController.GetAll([FromQuery] EventSessionFilterRequest filter)`, email dispatch/admin limits, external API key usage date ranges, contact share export/search, custom-property governance scopes, projection admin page/exposure ceiling, notification filters, user appearance palette generation, and storage presigned URL expiration/object-key inputs.
- `Explore.API/Middleware/IdempotencyMiddleware.cs` handles POST/PUT/PATCH/DELETE only; validates `Idempotency-Key` length <=128 and no whitespace; scopes lookup by tenant; replays existing records; persists 200-499 JSON/problem responses under 1MB; avoids 5xx caching; logs key hashes. Same-key/different-payload behavior needs explicit policy/tests.
- `Explore.Persistence/Configurations/Entities/IdempotencyRecordConfiguration.cs` has unique composite index on `(Key,TenantId)` and max key length 128.
- `Explore.Blazor/Services/StorageUploadSessionStore.cs` validates trusted HTTPS presigned URL shape, object key, content type, owner binding, expiry, corrupt payload, and content-type mismatch.
- BFF endpoint files to include in the matrix: `BffPreferenceEndpoints.cs`, `BffSetupSecretEndpoints.cs`, `BffStorageEndpoints.cs`, `BffAuthEndpoints.cs`.
- Existing BFF/security tests include `BffPreferenceAntiforgeryTests.cs`, `BffStorageUploadProxyTests.cs`, `StorageUploadSessionStoreTests.cs`, `BffCookieForwardingHandlerTests.cs`, and `BffSecurityTests.cs`.
- `rg` confirmed raw rendering search currently points to `CommunityGuidelines.razor` plus generated/bin/obj false positives.

## Final Oracle Review of CTO-Integrated Docs

Oracle returned **CONCERNS**, not FAIL. The review accepted the core direction—Clean Architecture ownership, manual FluentValidation, BFF token/CSRF boundary, tenant-leak prevention, public GET/query validation, DTO hardening, canonicalization outside validators, rich-text default deny, observability redaction, OpenAPI/client regeneration, and verification gates—but required four document fixes before finalization:

- Reconcile stale context/task status now that final mapping agents were collected and the second Oracle review completed.
- Make the input-surface matrix enforce threat model, release slice/priority, and decision status explicitly instead of hiding those gates in prose.
- Add an explicit BFF token-boundary gate: browser never receives tokens, browser-supplied auth/token headers are ignored or stripped where applicable, and server-side token forwarding remains BFF-owned.
- Record the final docs/architecture validation run before marking planning complete.


## Final Background Agent Findings

### Input Surface Mapping

- `Explore.API/Controllers/EventController.cs` is a major public/write surface: `GetAll([FromQuery] EventFilterRequest)`, `Create([FromBody] CreateEventDraftRequestDto)`, `Update([FromBody] UpdateEventDraftRequestDto)`, `Publish`, and aspect endpoints.
- `Explore.API/Models/EventFilterRequest.cs` and `Explore.API/Models/EventSessionFilterRequest.cs` are public GET query binders with no validator files found; this is a concrete public-read validation gap.
- Additional query/body surfaces for the matrix: `ExternalApiKeyController`, `CustomPropertyGovernanceController`, `CustomPropertyProjectionAdminController`, `ContactShareConsentController`, `EmailUnsubscribeController`, `PublicExperienceController`, `ModuleController`, `FooterController`, and `TenantController`.
- Storage/upload surfaces: `StorageObjectController`, `UploadRequestDto`, `UploadRequestDtoValidator`, `CreateStorageObjectDtoValidator`, `UpdateStorageObjectDtoValidator`, `BffStorageEndpoints`, and `StorageUploadSessionStore`. No dedicated unit tests were found for `UploadRequestDtoValidator`.
- Existing matrix/test seed files: `ApiContractInventoryGeneratorTests`, `EndpointAuthorizationMatrixTests`, `IdempotencyMiddlewareTests`, `ProblemDetailsContractTests`, `ExceptionHandlingIntegrationTests`, `EventVisibilityContractTests`, `EventMultiTagFilterTests`, `EventControllerRealRuntimeTests`, `StorageObjectControllerTests`, `BffPreferenceAntiforgeryTests`, `BffStorageUploadProxyTests`, and `StorageUploadSessionStoreTests`.
- Rich-text surfaces exist in Application models: `PublicExperienceHomeBlockKind.RichText`, `PublicExperienceShellDto` home blocks defaulting to `RichText`, and `EmailMessage.HtmlBody`. These must be classified as trusted/system-authored, sanitized user-authored, or disallowed.
- Existing canonicalizers/normalizers to reference before creating new ones: `CustomPropertyIdentity`, `GrpcEndpointNormalizer`, `TenantPolicySettingService`, `TenantBrandingSettingsDocumentProvisioningService`, `AppearanceResolutionService`, and `SlugGenerator`.
- Existing broad tenant-leak tests exist, but no per-route negative leak tests were found for main tenant-sensitive query surfaces such as event filters, external API key usage reports, custom-property projection admin, and contact-share consent queries.

### Blazor Form Mapping

- Shared primitives confirmed: `AppValidationSummary`, `FormSubmissionGuard`, `FormSubmitState`, and `ServerValidationErrorStore`.
- UI-local validators confirmed: `CreateEventRequestValidator`, `CreateEventSessionRequestValidator`, `UpdateEventSessionRequestValidator`, `CreateTagDtoValidator`, `CreateLocationDtoValidator`, `CreateCategoryDtoValidator`, and related client validators under `Explore.Blazor.Client/Validators`.
- Matrix-now stable/local surfaces include admin CRUD dialogs, `OrganizationDetails`, `SettingsPersonalInfo`, `SettingsSecurity`, `Setup`, `InstanceOnboarding`, `TenantOnboarding`, and upload/image components.
- Backend-contract-sensitive surfaces to defer until field keys settle: `CreateEvent`, `EventEdit`, `CreateSession`, `EditSession`, and `EventSessionForm`.
- Controlled raw rendering sinks: `CommunityGuidelines.razor` escapes before `MarkupString`; `ProjectionStatusSection.razor` uses controlled markup and encoded error text.
- Tests to anchor UI work: `SharedComponentAccessibilityTests`, `OrganizationDetailsHateoasTests`, `AppSideNavTests`, `MainLayoutTests`, `CreateEventTests`, `EventEditTests`, `CreateSessionTests`, `EditSessionTests`, `ImageUploadClientTests`, `ImageStorageServiceTests`, `ImageStorageRecordClientTests`, `ImageStorageSupportServiceTests`, `HandlerValidatorPairingTests`, and `BlazorClientArchitectureTests`.

## Blazor Inventory Summary

### Shared Form Infrastructure

- `Explore.Blazor.Client/Components/Forms/AppValidationSummary.razor`
- `Explore.Blazor.Client/Components/Forms/FormSubmissionGuard.razor`
- `Explore.Blazor.Client/Components/Forms/ServerValidationErrorStore.cs`
- `Explore.Blazor.Client/Components/Forms/FormSubmitState.cs`

### Current Patterns

- `InstanceOnboarding.razor` uses `DataAnnotationsValidator`/`ValidationSummary`/`ValidationMessage`.
- Admin dialogs for tags, locations, categories, footer links/groups, tenant navigation, custom properties, and event templates often use `FluentValidationValidator`.
- Main event/org/profile forms generally use `EditContext` + `AppValidationSummary` + manual checks/server mapping.
- `AppTextField` is styling only, not a validation abstraction.

### Important Write Surfaces to Remediate

- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor(.cs)`
- `Explore.Blazor.Client/Pages/Events/EventEdit.razor(.cs)`
- `Explore.Blazor.Client/Pages/Events/Sessions/CreateSession.razor`
- `Explore.Blazor.Client/Pages/Events/Sessions/EditSession.razor`
- `Explore.Blazor.Client/Pages/Organizations/CreateOrganization.razor(.cs)`
- `Explore.Blazor.Client/Pages/Organizations/OrganizationDetails.razor(.cs)`
- `Explore.Blazor.Client/Pages/User/Components/SettingsPersonalInfo.razor`
- `Explore.Blazor.Client/Pages/User/Components/SettingsSecurity.razor`
- `Explore.Blazor.Client/Pages/Admin/Dialogs/CreateApiKeyDialog.razor`
- `Explore.Blazor.Client/Pages/Admin/EventTemplates/Components/EventTemplateEditor.razor`
- `Explore.Blazor.Client/Pages/Events/Components/LocationRoomEditorDialog.razor`
- `Explore.Blazor.Client/Pages/Events/Dialogs/IslamicAspectEditDialog.razor`
- `Explore.Blazor.Client/Pages/Events/Dialogs/TechAspectEditDialog.razor`
- `Explore.Blazor.Client/Pages/Events/Components/EventDayEditorDialog.razor`
- `Explore.Blazor.Client/Pages/Events/Components/EventAgendaItemEditorDialog.razor`
- `Explore.Blazor.Client/Pages/Events/Dialogs/EventReviewDialog.razor`

### Raw Rendering Findings

- No `MudForm`, `InputText`, `HtmlString`, `IHtmlContent`, or `innerHTML` usage was found by the Blazor inventory.
- `MarkupString` appears only in `Explore.Blazor.Client/Pages/Legal/CommunityGuidelines.razor`; it escapes HTML before output.

## Backend Inventory Summary

### Existing Validation Architecture

- Application validation is widespread: backend inventory found 115 `AbstractValidator` files in `Explore.Application`.
- No global MediatR validation pipeline exists; no `Explore.Application/Behaviors/ValidationBehavior.cs` was found.
- No `AddValidatorsFromAssembly(...)` or similar validator auto-registration was found.
- No `AbstractValidator` implementations were found in `Explore.API`; API validation is not API-validator-driven.
- No custom `ApiBehaviorOptions`, `SuppressModelStateInvalidFilter`, or `InvalidModelStateResponseFactory` was found.

### Representative Backend Files

- `Explore.Application/DTOs/Event/CreateEventRequest.cs`
- `Explore.Application/DTOs/Event/CreateEventDraftRequestDto.cs`
- `Explore.Application/DTOs/Event/UpdateEventDraftRequestDto.cs`
- `Explore.Application/DTOs/Event/PublishEventRequestDto.cs`
- `Explore.Application/DTOs/Event/Validators/CreateEventRequestValidator.cs`
- `Explore.Application/DTOs/Event/Validators/UpdateEventDraftRequestDtoValidator.cs`
- `Explore.Application/DTOs/Event/Validators/PublishEventRequestDtoValidator.cs`
- `Explore.Application/DTOs/EventSessionGroup/CreateEventSessionGroupRequestDto.cs`
- `Explore.Application/DTOs/EventSessionGroup/Validators/CreateEventSessionGroupRequestDtoValidator.cs`
- `Explore.Application/DTOs/Onboarding/CompleteInstanceOnboardingRequest.cs`
- `Explore.Application/DTOs/Onboarding/Validators/CompleteInstanceOnboardingRequestValidator.cs`
- `Explore.Application/Exceptions/ValidationException.cs`
- `Explore.Application/ApplicationServicesRegistration.cs`
- `Explore.API/ExceptionHandling/ValidationExceptionHandler.cs`
- `Explore.API/Extensions/ExceptionHandlingExtensions.cs`
- `Explore.API/Controllers/ProgramValidationProblemDetails.cs`
- `Explore.API/Controllers/EventSessionController.cs`
- `Explore.API/Controllers/EventSessionGroupController.cs`
- `Explore.API/Controllers/EventController.cs`
- `Explore.API/Filters/SetupSecretRequiredAttribute.cs`
- `Explore.API/Middleware/IdempotencyMiddleware.cs`
- `Event.Application.UnitTests/Features/Events/Validators/CreateEventRequestValidatorTests.cs`
- `Event.Application.UnitTests/DTOs/EventRegistration/Validators/CreateEventRegistrationDtoValidatorTests.cs`
- `Event.API.IntegrationTests/Features/ProblemDetailsContractTests.cs`
- `Event.API.IntegrationTests/Features/ExceptionHandlingIntegrationTests.cs`
- `Event.API.IntegrationTests/Features/ProblemDetailsRealRuntimeTests.cs`

### Existing Sanitization / Encoding

- `CreateEventRegistrationCommandHandler` HTML-encodes event title values for email body content.
- `EventController` sanitizes calendar export filenames.
- `AnalyticsGovernanceService` strips disallowed/sensitive analytics properties and normalizes analytics values.
- `DockElementIds` sanitizes DOM IDs.
- `ImageHelper` HTML-encodes SVG label text.
- No dedicated reusable HTML sanitizer library/wrapper was found.

## Decisions for Implementation

1. Treat server-side Application/BFF validation as authoritative; Blazor validation is UX/a11y only.
2. Prefer async FluentValidation invocation across the board.
3. Do not introduce a global input sanitizer.
4. Do not introduce raw HTML/rich text rendering without a sanitizer allowlist, threat model, decision record, and tests.
5. Keep ProblemDetails as the API validation-error contract where applicable.
6. Keep OpenAPI and generated client regeneration as explicit work items after DTO changes.
7. Keep Application validators out of `Explore.Blazor.Client` unless package/version/bundle implications are resolved.
8. Treat automatic `[ApiController]` model-state and malformed JSON responses as separate from Application/FluentValidation exceptions until their exact response shape is tested or normalized.
9. Keep canonicalization outside validators: validators reject invalid input; handlers/domain services/factories/normalizers produce canonical storage/comparison forms.
10. Treat public read/query validation as part of the hardening scope.
11. Treat idempotency-key policy as external input validation, including mismatch/replay/cache decisions.
12. Treat validation telemetry as redacted structured metadata only.

## Oracle Review Summary

Initial Oracle review found the plan broadly aligned with project rules but required these clarifications, which were applied before the CTO update:

- Automatic `[ApiController]` model-state failures, malformed JSON, invalid enum/model-binding values, and content-type mismatches need explicit testing/normalization decisions.
- Server-only Blazor validation can conflict with `docs/UI_GOVERNANCE.md` unless UI governance is amended or lightweight UI-local validators cover syntactic checks.
- Blazor client validators should be UI-local or server-mapped; direct `Explore.Application` validator reuse remains blocked unless version/bundle concerns are resolved.
- BFF validation must live locally in `Explore.Blazor` endpoint/service seams and not introduce DI `IValidator<T>` or Application validator coupling.
- The statement that every write endpoint is `[Authorize]` was too absolute; bootstrap/internal exceptions need compensating controls.
- Validators should not mutate DTOs; canonicalization belongs in explicit handlers/domain services/factories/normalizers.
- Verification should distinguish docs-only validation from full implementation verification and include persistence tests when semantic validation touches transactional uniqueness/capacity/tenant/repository behavior.

A second Oracle review was completed after CTO feedback integration. It returned CONCERNS, and the required document corrections were applied: stale statuses reconciled, matrix schema expanded with threat/release/status columns, BFF token-boundary gates added, and final validation left as the remaining gate.

## Handoff — 2026-05-28 Europe/Brussels

### Current State

- What is completed: repository-grounded validation/sanitization plan, CTO feedback integration, Oracle review/corrections, Slice 1 contract decision log scaffold, and Slice 2 input-surface matrix scaffold.
- What is in progress: no active implementation; planning is paused at the contract-lock and matrix-expansion gates.
- What changed since the last handoff: added the decision log and input matrix artifacts, linked them from the plan, recorded validation results, and refreshed this context for session handoff.

### Next Action

1. Read `full-input-validation-sanitization-contract-decisions.md` and resolve `Pending` / `Needs design` decisions, starting with `[ApiController]` response shape, unknown JSON properties, over-posting, idempotency mismatch/replay, rich text classification, and validation telemetry.
2. Expand `full-input-validation-sanitization-input-matrix.md` from seeded high-risk rows to every API controller route/query/body input, BFF unsafe endpoint, Blazor write form, upload path, idempotency input, and raw-rendering/rich-text surface.
3. Update `full-input-validation-sanitization-tasks.md` after each resolved decision or completed matrix section; do not start Slice 3 implementation until Slice 1/2 gates are satisfied.

### Blockers

- Contract decisions are intentionally unresolved; implementation is blocked until Slice 1 decisions are accepted/deferred and Slice 2 high-risk rows have owners, threat models, priorities, decision statuses, and tests.
- Known decision blockers include automatic model-state response shape, malformed JSON/missing body/wrong content type behavior, unknown JSON property policy, over-posting policy, idempotency payload-fingerprint policy, validation failure caching, rich-text classification, UI governance/design-system conflict, and Blazor/Application validator ownership.

### Modified Files

- `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-plan.md` — CTO/matrix-first strategy, enterprise release slices, and links to Slice 1/2 artifacts.
- `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-context.md` — evidence log, current state, validation results, and this handoff snapshot.
- `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-tasks.md` — tactical checklist, completed planning/scaffold tasks, and remaining release-slice work.
- `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-contract-decisions.md` — new Slice 1 decision log for boundary/error/idempotency/BFF/rich-text/telemetry decisions.
- `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-input-matrix.md` — new Slice 2 matrix scaffold seeded with high-risk API, BFF, Blazor, upload, query, idempotency, and rich-text surfaces.

### Validation

- Commands run:
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed after CTO/Oracle corrections: 179 total, 178 succeeded, 1 skipped, 0 failed.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed again after adding Slice 1/2 artifacts: 179 total, 178 succeeded, 1 skipped, 0 failed.
  - `rtk git diff --check -- <workstream docs>` with `git diff --check` fallback logic — passed after the handoff refresh with no whitespace errors.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed after the handoff refresh: 179 total, 178 succeeded, 1 skipped, 0 failed.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed after the session-handoff refresh: 179 total, 178 succeeded, 1 skipped, 0 failed.
- Commands still needed before implementation completion:
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` when validation touches EF-backed uniqueness, capacity, tenant, or repository behavior.

### Documentation Impact

- Updated active dev docs only. No canonical docs, journal, implementation files, OpenAPI schema, or generated client files were intentionally changed for this handoff.
- Future implementation may need `docs/API_CHANGELOG.md`, `docs/UI_GOVERNANCE.md`, `docs/API.md`, OpenAPI schema regeneration, and Blazor generated-client updates depending on contract decisions.

### Risks

- Source-grounding risks: the matrix is a scaffold, not complete inventory; next agent must continue from repository searches and test inventories before coding.
- Test or build risks: only architecture/docs-context tests have passed for planning artifacts; no application/API/Blazor/persistence implementation tests have been run for actual validation changes because no implementation has started.
- Operator/release risks: broad validation changes can alter generated clients, public read query behavior, BFF bootstrap/setup behavior, idempotency replay semantics, and rich-text rendering; these are intentionally gated by Slice 1 decisions and Slice 2 matrix ownership.

### Notes For Next Contributor Or Agent

- Required docs/rules to read: `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/API.md`, `docs/ARCHITECTURE.md`, `docs/SECURITY-MODEL.md`, `docs/BLAZOR.md`, `docs/UI_GOVERNANCE.md`, `docs/OPERATIONS.md`, `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md`, `.claude/rules/blazor-client.md`, plus relevant skills for Clean Architecture, CQRS/MediatR, auth, Blazor BFF, Blazor UI, EF Core, and error tracking.
- Assumptions made: this workstream remains planning/documentation-only until the user explicitly asks for implementation; no backward compatibility constraints apply because the repo is pre-v1/development mode.
- Do not touch / unrelated dirty files: targeted git status shows `dev/active/full-input-validation-sanitization/` is untracked, while overall `git status --short | wc -l` reports 320 dirty entries across the repository. Treat dirty files outside this workstream as unrelated unless a separate intent establishes ownership.

## Quick Resume

1. Start by reading the 2026-05-29 handoff above, then `full-input-validation-sanitization-input-matrix.md` and `full-input-validation-sanitization-tasks.md`.
2. Continue with a concrete matrix row; recommended next slice is storage metadata/object-key validation beyond the already implemented legacy direct presign filename/content-type checks.
3. Before editing storage metadata/API code, inspect `CreateStorageObjectDtoValidator`, `UpdateStorageObjectDtoValidator`, storage command handlers, `StorageObjectController`, `StorageObjectRepository`, and tenant/object-key semantics.
4. Do not touch TickerQ/scheduler files or generated EF migrations in this workstream.

## Implementation Update — 2026-05-29 Europe/Brussels

### Completed Slice

- Implemented the Slice 6 `BffSetupSecretEndpoints` local validation/leakage hardening.
- `Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs` now validates setup-secret request bodies before API forwarding:
  - malformed JSON returns safe `400` ProblemDetails;
  - missing secrets still return safe `400`;
  - setup secrets are trimmed, capped at 512 characters, and rejected when they contain control characters;
  - invalid local requests do not call the API.
- Upstream setup-secret validation responses are now translated through a local safe-message allowlist. Browser-facing responses no longer echo upstream `Error` values from the API/provider. The endpoint preserves status meaning for `403`, `410`, `429`, `5xx`, and unexpected failures without leaking provider details.
- Setup-secret gateway logging no longer logs raw exception messages/objects for upstream validation failures; it records safe status/exception-type metadata only.
- Added `Explore.Blazor.IntegrationTests/Endpoints/BffSetupSecretEndpointsTests.cs` covering:
  - raw upstream forbidden errors are not leaked;
  - raw upstream `valid=false` errors are not leaked;
  - malformed JSON returns safe `400` and does not call the API;
  - overlong secrets return safe `400` and do not call the API;
  - browser-supplied `X-Setup-Secret` is not used as the validation secret.

### Validation

- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/BffSetupSecretEndpointsTests/*' --maximum-parallel-tests 1` — passed, 5/5.
- `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet` — passed.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed, 181 total, 180 succeeded, 1 skipped.
- `dotnet build --configuration Release --verbosity quiet` — passed with existing package/analyzer warnings.

### Workstream Notes

- Updated `full-input-validation-sanitization-input-matrix.md` to mark `BffSetupSecretEndpoints` as implemented for local validation/leakage, with remaining brute-force/rate-limit stress as follow-up under the existing setup-secret policy.
- Updated `full-input-validation-sanitization-tasks.md` to close the setup-secret inventory, spoofing, and raw-provider leakage items.
- No EF migration files were edited for this slice.

## Implementation Update — 2026-05-29 Europe/Brussels — BFF Preferences

### Completed Slice

- Implemented the Slice 6 `BffPreferenceEndpoints` simple-value validation hardening.
- `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs` now validates `PUT /bff/appearance/mode` through `IBffPreferenceValidationService` before checking authenticated/anonymous persistence paths. Invalid authenticated requests are rejected locally before API forwarding, and valid authenticated requests forward a normalized `SetThemeModeRequestDto`.
- Existing anonymous `POST /bff/theme`, `POST /bff/language`, and `POST /bff/direction` local validation remains authoritative for cookies; the new tests lock those endpoint-level failures rather than relying only on service unit tests.
- Added `Explore.Blazor.IntegrationTests/Endpoints/BffPreferenceValidationEndpointsTests.cs` covering:
  - invalid `theme` returns safe `400` and does not set the `theme` cookie;
  - invalid `lang` returns safe `400` and does not set `lang` or `.AspNetCore.Culture`;
  - invalid `dir` returns safe `400` and does not set `direction`;
  - authenticated invalid appearance mode returns safe `400` without invoking `IBffPreferenceForwardingService`.

### Validation

- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/BffPreferenceValidationEndpointsTests/*' --maximum-parallel-tests 1` — passed, 4/4.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/BffPreferenceAntiforgeryTests/*' --maximum-parallel-tests 1` — passed, 7/7.
- `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet` — passed with existing warnings.

### Workstream Notes

- Updated `full-input-validation-sanitization-input-matrix.md` to mark `BffPreferenceEndpoints` as implemented for antiforgery and simple preference/mode values; profile payload validation remains API-owned and still pending inventory.
- Updated `full-input-validation-sanitization-tasks.md` to close the BFF preference inventory, CSRF coverage, and endpoint-level invalid-value tests.
- No EF migration files were edited for this slice.

## Implementation Update — 2026-05-29 Europe/Brussels — BFF Storage

### Completed Slice

- Implemented the Slice 6 `BffStorageEndpoints` validation and upload-session binding hardening.
- `Explore.Blazor/Extensions/BffStorageEndpoints.cs` now validates upload-session JSON locally before API forwarding:
  - request bodies are required;
  - file names are trimmed, capped, and rejected when they contain path segments or control characters;
  - content types are trimmed, capped, parsed as MIME media types, and rejected when invalid or wildcarded;
  - only normalized `UploadRequestDto` values are forwarded to `api/storageobject/generate-upload-url`.
- The multipart upload proxy now rejects malformed form bodies, raw browser-supplied `uploadUrl` fields, invalid opaque session ids, unsafe file names, invalid content types, and mismatched declared/file content types before resolving the server-issued upload session or calling the S3 upload client.
- The storage BFF validation remains local/manual in `Explore.Blazor`; it does not use DI validators or Application-layer validator coupling. `StorageUploadSessionStore` remains the trust boundary for user-bound, content-type-bound, consume-once upload sessions.
- Attempted Context7 lookup for current ASP.NET Core upload/antiforgery guidance, but the Context7 quota was exhausted. This slice used the already-loaded repo security model and existing ASP.NET Core antiforgery/minimal API guidance from the prior BFF preference slice.
- Added/expanded `Explore.Blazor.IntegrationTests/Endpoints/BffStorageUploadProxyTests.cs` covering:
  - raw presigned-looking browser upload URLs are rejected without S3 calls;
  - upload-session requests with path-style filenames do not call the API;
  - upload-session requests with invalid content types do not call the API;
  - upload proxy content-type mismatch does not upload;
  - another authenticated user cannot use someone else's upload session;
  - successful upload consumes the session and reuse is rejected without a second S3 upload.

### Validation

- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/BffStorageUploadProxyTests/*' --maximum-parallel-tests 1` — passed, 6/6.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/StorageUploadSessionStoreTests/*' --maximum-parallel-tests 1` — passed, 4/4.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed, 181 total, 180 succeeded, 1 skipped.
- `dotnet build --configuration Release --verbosity quiet` — passed with existing package/analyzer warnings.

### Workstream Notes

- Updated `full-input-validation-sanitization-input-matrix.md` to mark BFF storage upload-session and upload-proxy validation as implemented for syntactic validation, destination binding, content-type mismatch, cross-user rejection, and consume-once replay behavior.
- Updated `full-input-validation-sanitization-tasks.md` to close the BFF storage inventory and upload-session proxy abuse cases.
- No EF migration files were edited for this slice.

## Implementation Update — 2026-05-29 Europe/Brussels — BFF Auth

### Completed Slice

- Implemented the Slice 6 `BffAuthEndpoints` provider-diagnostic leakage hardening.
- `Explore.Blazor/Extensions/BffAuthEndpoints.cs` now handles `/auth/providers` failures through `ISafeAuthDiagnosticsPolicy`:
  - browser-visible `ProblemDetails` use a generic detail instead of `ex.Message`;
  - responses include safe `code` and `correlationId` extensions;
  - logs record safe error code, correlation id, and exception category without logging the exception object/message.
- Added `Explore.Blazor.IntegrationTests/Endpoints/BffAuthEndpointValidationTests.cs` covering:
  - provider-resolution exceptions containing raw provider text, `refresh_token`, secret length, and client id are not echoed in the browser response;
  - browser-supplied `Authorization: Bearer ...` does not authenticate `/auth/status` and is not echoed;
  - `POST /bff/auth/refresh-schemes` without antiforgery is rejected;
  - authenticated `POST /bff/auth/refresh-session` without antiforgery is rejected.
- Attempted Context7 lookup for current ASP.NET Core BFF auth/antiforgery guidance, but the Context7 quota was exhausted. This slice used the loaded repo security model and prior ASP.NET Core antiforgery guidance from earlier BFF slices.

### Validation

- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/BffAuthEndpointValidationTests/*' --maximum-parallel-tests 1` — passed, 4/4.

### Workstream Notes

- Updated `full-input-validation-sanitization-input-matrix.md` to mark `BffAuthEndpoints` implemented for provider failure sanitization, auth-status header spoofing, and auth refresh antiforgery checks.
- Updated `full-input-validation-sanitization-tasks.md` to close BFF auth inventory and bootstrap/internal compensating-control validation; broader proxy header-stripping tests remain pending under the YARP row.
- No EF migration files were edited for this slice.

## Implementation Update — 2026-05-29 Europe/Brussels — BFF YARP Proxy Header Boundary

### Completed Slice

- Implemented the Slice 6 YARP/BFF proxy header hardening row.
- `Explore.Blazor/Services/BffProxyHeaderSanitizer.cs` now strips browser-originated credential and tenant-authority headers from the YARP `HttpRequestMessage` before any trusted BFF transform runs:
  - `Authorization` and `Proxy-Authorization`;
  - BFF/browser cookies;
  - `X-Setup-Secret`;
  - direct API `X-API-Key`;
  - access/refresh/identity/id token-style headers; and
  - browser-supplied tenant id/slug headers.
- `Explore.Blazor/Extensions/YarpProxyExtensions.cs` invokes the sanitizer as the first request transform, then the existing transforms add only server-owned values: BFF access token from the authenticated session, tenant slug from `ITenantRouteContextAccessor`, and setup secret from `ISetupSecretResolver`.
- `Explore.Application/Features/StorageObjects/Requests/Commands/FinalizeStorageUploadSessionCommand.cs` was corrected to match the current `ISecureRequest.ResourceAttributes` contract (`IDictionary<string, object>?`) so the storage/input-validation build compiles.
- The untracked `Explore.Persistence/Repositories/ActorSubscriptionRepository.cs` had a compile-only `IQueryable`/`IOrderedQueryable` fix because it blocked all Blazor integration tests; no migration files were edited.
- Attempted Context7 lookup for YARP request transform documentation, but the Context7 quota was exhausted. This slice used the repo security model and existing BFF/YARP transform implementation.

### Validation

- `dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet --property:WarningLevel=0 /clp:ErrorsOnly` — passed, 0 errors.
- `dotnet test --no-build --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/BffProxyHeaderSanitizerTests/*' --maximum-parallel-tests 1` — passed, 2/2.
- `dotnet test --no-build --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/AccessTokenForwardingHandlerTests/*' --maximum-parallel-tests 1` — passed, 5/5.
- `dotnet test --no-build --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter '/*/*/SetupSecretForwardingHandlerTests/*' --maximum-parallel-tests 1` — passed, 8/8.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed, 181 total, 180 succeeded, 1 skipped.
- `dotnet build --configuration Release --verbosity quiet --property:WarningLevel=0 /clp:ErrorsOnly` — passed, 0 errors, existing warnings.
- Scoped `git diff --check` over the files touched in this slice passed. Repository-wide `git diff --check` still reports unrelated generated-client trailing whitespace in `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.

### Workstream Notes

- Updated `full-input-validation-sanitization-input-matrix.md` to mark the YARP proxy row implemented for browser credential/header stripping.
- Updated `full-input-validation-sanitization-tasks.md` to close the Slice 6 browser-supplied header stripping task.
- No EF migration files were edited for this slice.
