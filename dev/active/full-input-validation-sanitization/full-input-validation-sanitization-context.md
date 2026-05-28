<!-- ABOUTME: Session context and evidence log for the full input validation and sanitization planning workstream. -->
<!-- ABOUTME: Captures source documents, agent findings, CTO feedback, decisions, blockers, and quick-resume notes for future implementation sessions. -->

# Full User Input Validation & Sanitization Context

Last Updated: 2026-05-28 Europe/Brussels

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

1. Continue filling `full-input-validation-sanitization-contract-decisions.md` until all Slice 1 decisions are `Accepted` or explicitly deferred.
2. Continue expanding `full-input-validation-sanitization-input-matrix.md` until every API, BFF, Blazor, upload, query, idempotency, and raw-rendering input has a row.
3. Use the release slices in `full-input-validation-sanitization-tasks.md` as the execution order once the contract log and matrix gates are satisfied.
4. No implementation code has been changed yet; this workstream is ready for a future implementation session.
