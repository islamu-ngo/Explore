<!-- ABOUTME: Current handoff context for the full input validation and sanitization workstream. -->
<!-- ABOUTME: Captures verified state, research evidence, decisions, next slices, and blockers. -->

# Full Input Validation & Sanitization - Context

Last Updated: 2026-07-04 Europe/Brussels
Status: Senior CTO rebaseline updated; implementation continues by matrix-backed slice
Primary plan: `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-plan.md`
Task list: `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-tasks.md`
Input matrix: `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-input-matrix.md`
Decision log: `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-contract-decisions.md`

---

## 1. Current Handoff

### Completed In This Planning Pass

- Applied the `$senior-cto-feedback` workflow to the active workstream.
- Rewrote the implementation plan as a source-grounded, repo-conformant contract.
- Refreshed Context7 MCP evidence for ASP.NET Core `[ApiController]` automatic validation failures, Blazor `EditContext`/`ValidationMessageStore`, antiforgery, and encoded rendering versus raw `MarkupString`/`AddMarkupContent` seams.
- Retained earlier OWASP/IETF research notes, but recorded that the latest Tavily MCP call is blocked by usage-limit status `432`; no new Tavily result from this continuation should be treated as evidence.
- Corrected the task model so validation and sanitization are distinct concerns.
- Removed the bad task that told the user to regenerate the API client.
- Promoted API, BFF, and Blazor responsibilities into separate implementation lanes.
- Corrected the remaining task model so audit-only work is not enough: every slice must update a matrix row, contract decision, code/test, or explicit deferral.
- Shifted the next priority away from indefinite storage cleanup and toward high-risk API/Application validation, BFF trust boundaries, and Blazor form/server-error convergence.
- Identified and then removed the `AiAssistantRail.razor` prompt-reference `MarkupString` seam.

### Corrected Next Slice Queue

1. `Blazor form validation convergence`: continue remaining session create/edit forms with API ProblemDetails mapping, accessible summaries/focus, duplicate-submit behavior, and HAL-only affordance gating. Organization create/details/settings and event create/edit have completed convergence slices; do not mark the whole Blazor family complete until session composers are covered.
2. `Email dispatch admin semantics`: add tenant-B outbox masking and raw SMTP/provider/log leakage coverage where not already proven.
3. `Idempotency and cross-cutting header residuals`: cover idempotency-key, tenant-hint, and forwarded-host validation rows.
4. `BFF boundary residuals`: continue only when the matrix identifies an uncovered browser-controlled header, cookie, continuation path, antiforgery, or diagnostics seam.

Do not start another broad audit. Pick the row first, update the matrix, then implement the smallest validating slice and record focused verification.

### Implemented Blazor Organization Create Validation Convergence Slice - 2026-07-04

- `CreateOrganization.razor` now uses the repository form primitives: `EditForm`, `EditContext`, `FormSubmissionGuard`, `FormSubmitState`, `ServerValidationErrorStore`, and `AppValidationSummary`.
- The create wizard now performs UI-local syntactic checks for required organization fields, basic email shape, HTTP(S) website URLs, positive postal code, accepted terms, and confirmation. These are UX checks only; API/Application validation remains authoritative.
- API validation failures are now caught as generated-client `ApiException` instances and mapped into the form `EditContext`; unexpected exceptions use a generic user-facing message instead of echoing raw exception text.
- `ServerValidationErrorStore` now handles generated `ValidationProblemDetails` directly, not only `ProblemDetails` extension-data errors, and resolves field keys case-insensitively with support for nested keys such as `Email.Value`.
- Added `CreateOrganizationTests` coverage for local invalid-email blocking, generated `ValidationProblemDetails` field-error mapping, and unexpected exception text not being rendered to the user.
- Context7 MCP evidence used: official ASP.NET Core Blazor docs for `EditForm`, `EditContext`, `ValidationMessageStore`, custom validation, and server validation display through the form validation pipeline.
- Tavily MCP evidence status: fresh Tavily search for current OWASP/client-server validation guidance failed with usage-limit status `432`; no Tavily output from this slice is treated as evidence.
- Verification: `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet` passed with 0 errors and the existing ASPDEPR001 warning. `dotnet format whitespace` and `git diff --check` passed for touched files.
- Repaired stale generated-client `_links` anonymous-type test fixtures in `EventTemplateHalResourceExtensionsTests`, moderation/reporting tests, and `SupportAccessClientServiceTests` so `Explore.Blazor.Client.Tests` compiles again after client regeneration.
- Verification now passes for the Blazor client boundary: `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` completed with 0 errors; `CreateOrganizationTests` passed 3 tests; `OrganizationDetailsHateoasTests` passed 3 tests; full `Explore.Blazor.Client.Tests` passed with 1490 succeeded and 1 existing documented skip.
- Canonical full Release build status: `dotnet build --configuration Release --verbosity quiet` is not green in the current worktree. After the Blazor test fixture repair, remaining failures are outside this slice in existing warning-as-error/analyzer/package issues across other projects.
- Architecture verification limitation: `Event.Architecture.Tests` ran 259 tests; 256 passed, 1 skipped, and 2 failed on an unrelated migrated `text-to-lottie` skill frontmatter/section schema issue.
- Visual QA limitation: no browser screenshot QA was completed for the authenticated organization routes. The Blazor client/test projects build and bUnit exercises the form states, but no fresh authenticated browser session with route data was available for screenshot-based visual QA.

### Implemented Blazor Organization Details/Settings Validation Convergence Slice - 2026-07-04

- `OrganizationDetails.razor.cs` now performs UI-local syntactic validation before update dispatch: required organization fields, basic email shape, HTTP(S) website URL, and positive postal code. The API/Application validators remain authoritative for business rules and canonicalization.
- `OrganizationDetails.razor.cs` no longer renders raw generated-client, provider, or database exception messages for load/update failures. Unexpected failures use bounded generic UI text while logs keep structured exception detail.
- `OrganizationProfileSection.razor` now uses the same Blazor form primitives as the route-level form: `EditForm`, `EditContext`, `FormSubmissionGuard`, `FormSubmitState`, `ServerValidationErrorStore`, and `AppValidationSummary`.
- Organization settings/profile updates now map generated `ValidationProblemDetails` into the `EditContext` and use generic user-facing text for unexpected update failures.
- Added `OrganizationDetailsHateoasTests` coverage for invalid-email client blocking, server `ValidationProblemDetails` mapping from nested `Email.Value`, and unexpected exception non-echo while preserving existing HAL affordance tests.
- Added `OrganizationProfileSectionTests` coverage for invalid-email client blocking, server `ValidationProblemDetails` mapping, and unexpected exception non-echo on the settings/profile form.
- Context7 MCP evidence used: official ASP.NET Core Blazor docs for `EditForm`, `EditContext`, `ValidationMessageStore`, custom validation, and server validation display. Tavily MCP refresh still fails with usage-limit status `432`.
- Verification: `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed with 0 errors; focused `OrganizationDetailsHateoasTests` passed 6 tests; focused `OrganizationProfileSectionTests` passed 3 tests; full `Explore.Blazor.Client.Tests` passed with 1496 succeeded and 1 existing documented skip after one transient `AiAssistantRailTests.ReferenceHighlights_EncodeDangerousReferenceDisplayNames` failure passed in isolation and on full rerun.
- Visual QA limitation: visual-qa instructions were loaded after the UI changes, but the full screenshot/oracle gate was not run because this authenticated component surface was verified through bUnit rather than a live seeded browser session.

### Implemented Blazor Event Create/Edit Server-Error Safety Slice - 2026-07-04

- `CreateEvent.razor.cs` and `EventEdit.razor.cs` already used `EditForm`, `EditContext`, `FormSubmissionGuard`, `FormSubmitState`, `ServerValidationErrorStore`, and `AppValidationSummary`; this slice tightened their unsafe failure paths rather than introducing another form abstraction.
- Generated-client `ValidationProblemDetails` for create/update event failures now continue to map into the form `EditContext` through `ServerValidationErrorStore`, preserving API/Application validation as authoritative and Blazor validation as UX only.
- Unexpected create/update failures no longer render raw generated-client, provider, database, or transport exception text. The UI now uses fixed generic messages for create/update, while structured logs retain exception detail.
- Event create/edit inline session deletion failures now use a fixed generic UI message and structured logging instead of rendering `Exception.Message`.
- `EventEdit.razor.cs` load failures now use a fixed generic refresh message instead of rendering the raw load exception.
- Added `CreateEventTests` coverage for generated `ValidationProblemDetails` mapping into `EditContext` and unexpected exception non-echo on event creation.
- Added `EventEditTests` coverage for generated `ValidationProblemDetails` mapping into `EditContext` and unexpected exception non-echo on event update.
- Repaired stale generated-client `_links` anonymous-type test fixtures in event-template, moderation/reporting, and support-access tests so `Explore.Blazor.Client.Tests` compiles against the current regenerated client.
- Context7 MCP evidence used: official ASP.NET Core Blazor docs for `EditForm`, `EditContext`, `ValidationMessageStore`, custom validation, and server validation display. Tavily MCP refresh for current OWASP validation/output-encoding guidance still fails with usage-limit status `432`, so no fresh Tavily result from this slice is treated as evidence.
- Verification: `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet /clp:ErrorsOnly` passed with 0 errors; focused `CreateEventTests` passed 32 tests; focused `EventEditTests` passed 15 tests; full `Explore.Blazor.Client.Tests` passed with 1500 succeeded and 1 existing documented skip.
- Visual QA limitation: no live browser screenshot QA was run for the authenticated event forms. This slice changed validation/error behavior only and was verified through bUnit component state plus full Blazor client tests.

### Implemented Email Dispatch Admin Query Validation Slice - 2026-07-04

- Added `EmailDispatchStatusQueryRequest`, `EmailDispatchPauseTenantQueryRequest`, and `EmailDispatchParkQueryRequest` in the existing API query-model file.
- `EmailDispatchAdminController.GetStatus` now validates `tenantId` and `limit` through `[ApiController]` model validation before dispatching `GetEmailDispatchStatusQuery`.
- `PauseTenant` now accepts an API query model, keeps pause `reason` optional, rejects control characters/over-500-character values early, and passes trimmed reason text or `null` into `SetEmailDispatchTenantPauseStateCommand`.
- `ParkDispatch` now accepts an API query model, requires non-blank `reason`, rejects control characters/over-500-character values early, and passes trimmed reason text into `ParkEmailDispatchCommand`.
- Existing Application command validators remain in place for defense-in-depth over tenant IDs, outbox IDs, and command reason bounds.
- Added focused model and runtime coverage in `EmailDispatchAdminControllerTests`: invalid status `limit`, blank park reason, control-character pause reason, invalid runtime status limit, missing runtime park reason, and overlong runtime pause reason all fail as canonical `400` validation ProblemDetails before MediatR dispatch.
- Context7 MCP confirmed `[ApiController]` automatic 400 model-validation behavior for query-bound request models. Tavily MCP still returns usage-limit status `432`, so no fresh Tavily evidence was added.
- Remaining email-dispatch row work: tenant-B outbox semantic masking and raw SMTP/provider/log leakage coverage where not already proven by existing command/provider tests.

### Implemented Public Detail Route-ID Cross-Tenant Masking Slice - 2026-07-04

- Added representative event graph route-ID runtime coverage without duplicating every controller's existing malformed-ID tests.
- `EventVisibilityContractTests.GetByIdForCrossTenantEventReturnsSafeNotFound` seeds a published public event under a non-default tenant, calls `/api/event/{id}` through the default tenant context, and proves the response is `404` without the hidden event title or tenant ID.
- `EventSessionVisibilityContractTests.GetByIdReturnsSafeNotFoundForCrossTenantSession` does the same for `/api/eventsession/{id}` with a published session under a non-default tenant.
- Existing scattered tests already cover malformed route IDs such as `/not-a-guid` for event, event-session, organization, location, actor, category, tag, and storage routes, so this slice focused on the missing cross-tenant not-accessible case.
- Verification passed: Event.API.IntegrationTests build had 0 errors, and the focused `EventVisibilityContractTests` plus `EventSessionVisibilityContractTests` lane passed 10 tests.

### Implemented Correlation Header Validation Slice - 2026-07-04

- `CorrelationIdMiddleware` now accepts only one non-blank visible-ASCII `X-Correlation-ID` or `X-Request-ID` value up to 128 characters before pushing it into `HttpContext.Items`, Serilog context, and the response header.
- Unsafe correlation metadata now falls back to the server-generated request trace identifier, and a safe `X-Request-ID` can still be used when `X-Correlation-ID` is unsafe.
- `BffProxyHeaderSanitizer` now strips unsafe `X-Correlation-ID` and `X-Request-ID` values before YARP forwarding, while preserving safe correlation/request metadata and ordinary request metadata such as `Accept` and `Accept-Language`.
- Verification passed: BFF integration build and API integration build completed with 0 errors; focused `BffProxyHeaderSanitizerTests` passed 3 tests and focused `CorrelationIdTests` passed 6 tests.
- Remaining header work: idempotency-key conflict coverage, tenant-hint validation, and trusted forwarded-host spoofing residuals.

### Completed Implementation Slice - 2026-07-03

- Hardened `CreateStorageUploadSessionDtoValidator` so upload-session reservation metadata rejects malformed or wildcard MIME hints, control characters, path separators, dot segments, reserved Windows device names, and unsafe extension tokens.
- Updated `CreateStorageUploadSessionCommandHandler` so `ContentType` is normalized before both storage policy resolution and persistence.
- Added `CreateStorageUploadSessionDtoValidatorTests` for the new validator contract.
- Added `StorageUploadSessionCommandHandlerTests.CreateHandle_NormalizesContentTypeBeforePolicyResolutionAndPersistence` to prevent policy routing from using raw MIME input.

### Implemented Storage Metadata Slice - 2026-07-03

- Added `StorageObjectMetadataValidation` as the shared Application-layer predicate owner for relative object keys, simple file names, reserved Windows names, simple extension tokens, optional/required MIME hints, and SHA-256 hex digests.
- Hardened `UploadRequestDtoValidator`, `CreateStorageObjectDtoValidator`, and `UpdateStorageObjectDtoValidator` against reserved file names, object-key traversal, unsafe metadata file names/display names/extensions, malformed MIME hints, non-hex checksums, and incomplete owning-resource metadata.
- Added `StorageObjectMetadataDtoValidatorTests` and expanded `UploadRequestDtoValidatorTests`.
- Verified `Explore.Application` builds and LSP diagnostics are clean for the changed source/test files.
- Verified focused `Event.Application.UnitTests` storage metadata execution passes 37 tests for `StorageObjectMetadataDtoValidatorTests` and `UploadRequestDtoValidatorTests`.

### Implemented Upload Session Ownership Slice - 2026-07-03

- Added `ICurrentUserService` ownership checks to `CancelStorageUploadSessionCommandHandler` and `FinalizeStorageUploadSessionCommandHandler`.
- Wrong-user upload sessions now fail closed as `StorageUploadSessionNotFound`, matching wrong-tenant masking and avoiding cross-user existence disclosure.
- Finalize checks ownership before the first transaction returns an uploadable session, before provider I/O uses the reloaded session, and again in finalize/failure transaction paths.
- Added focused `StorageUploadSessionCommandHandlerTests` coverage proving wrong-user finalize/cancel attempts do not access usage counters, write provider bytes, create `StorageObject` metadata, release quota, or mutate session state.

### Implemented BFF Storage Boundary Slice - 2026-07-04

- Hardened `Explore.Blazor/Extensions/BffStorageEndpoints.cs` so `/bff/storage/upload-session` and `/bff/storage/upload-proxy` reject reserved Windows device filenames before forwarding or finalizing upload work.
- Tightened BFF MIME hint parsing so multi-segment values such as `application/pdf/extra` are rejected as invalid instead of being accepted by tolerant media-type parsing.
- Removed raw API upload-session IDs from BFF storage proxy failure logs; logs now describe a resolved API upload session without recording the raw session identifier.
- Expanded `BffStorageUploadProxyTests` to prove reserved filenames and malformed multi-segment content types fail locally and do not call the downstream API/finalize path.

### Implemented Storage Presigned Download Slice - 2026-07-04

- Hardened `GetPresignedDownloadUrlRequestHandler` so provider signing uses the persisted `StorageObject.ObjectKey`, never an object key parsed back out of `StorageObject.Uri`.
- Added Application-layer read gating before signing: storage metadata must be active; `PublicImage` is public; `AuthenticatedTenant` requires an authenticated current user; `PrivateOwner` requires `CreatedBy` to match `ICurrentUserService.UserId`.
- Added fail-closed expiration bounds of 1 through 60 minutes before metadata lookup, preventing unbounded or nonsensical presigned URL lifetimes.
- Stopped returning provider object keys from the by-ID presigned-download response by keeping the existing DTO shape but setting `ObjectKey` to `string.Empty`.
- Kept storage logs identifier-based: failures now log storage object IDs and visibility only, not raw URIs or provider object keys.
- Updated `StorageObjectController.GetPresignedDownloadUrl` so a null handler result maps to the existing 404 ProblemDetails response instead of `200 null`.
- Added focused Application handler tests and API controller contract tests for persisted-key signing, owner gating, expiration rejection, missing object keys, no object-key response echo, and null-to-404 behavior.

### Implemented Storage Provider Result Validation Slice - 2026-07-04

- Hardened `FinalizeStorageUploadSessionCommandHandler` so provider write results are validated before storage metadata creation, quota commit, and upload-session finalization.
- Provider results must now match the reserved provider, return a safe relative object key under the current tenant prefix, report the reserved byte count, preserve the reserved content type, and include a valid SHA-256 hex checksum.
- Invalid provider metadata now fails closed as `StorageUploadWriteFailed`, releases the quota reservation, marks the upload session failed, and does not create a `StorageObject`.
- Finalized storage metadata now persists the canonical session provider/content type after provider-result validation instead of trusting provider-returned values for those fields.
- Expanded `StorageUploadSessionCommandHandlerTests` so the happy path uses realistic tenant-scoped object keys and SHA-256 digests, and a malicious/buggy provider result proves no metadata persistence or quota commit occurs.

### Implemented Storage API Failure Mapping Coverage Slice - 2026-07-04

- Expanded `StorageUploadSessionControllerTests` so upload finalize failures are driven through `StorageObjectController.UploadSessionContent`.
- Covered storage upload not-found, expired-session conflict, size mismatch, content-type mismatch, and write/provider-result failure mappings.
- Verified the controller returns the canonical ProblemDetails status/title/code/detail for each failure code, including `StorageUploadWriteFailed` mapping to `503 Storage provider unavailable`.

### Implemented Storage Runtime Route Coverage Slice - 2026-07-04

- Expanded runtime `StorageObjectControllerTests` so upload-session create, finalize, and cancel endpoints prove unauthenticated requests return `401` at the API host boundary.
- Added malformed upload-session route-ID coverage for finalize and cancel so `/upload-sessions/not-a-guid/...` is rejected by the `{uploadSessionId:guid}` route constraint as `404` before controller dispatch.
- Verified the API integration project builds and the focused storage object runtime suite passes 13 tests.

### Implemented Storage Content Signature Slice - 2026-07-04

- Added `StorageContentSignaturePolicy` as an Application-layer storage finalization policy for known uploaded image/document types.
- The policy validates content type as a reserved-session hint, not as proof: known MIME types must match an extension allowlist and byte signature before provider writes.
- Covered image signatures for JPEG, PNG, GIF, and WebP; document signatures for PDF, RTF, OLE compound Office files, and ZIP-container Office/OpenDocument formats.
- Unsupported `image/*` content types now fail closed until a product/security decision adds a safe signature/rendering policy.
- `FinalizeStorageUploadSessionCommandHandler` runs the policy after the session is marked `Uploading` and before provider I/O. Failures use `StorageUploadContentSignatureMismatch`, release the quota reservation, mark the session failed, and do not create `StorageObject` metadata.
- Non-seekable request streams are handled by replaying the inspected prefix to the storage provider, preserving the bytes the validator consumed.
- `CommandResponseResultMapper` maps `StorageUploadContentSignatureMismatch` to canonical `400` validation ProblemDetails.
- Added command-handler tests for spoofed bytes, mismatched extension, and non-seekable prefix replay.
- Added API controller failure-mapping coverage for the new failure code and verified it through focused `StorageUploadSessionControllerTests`.

### Implemented Storage Projection URL Helper Slice - 2026-07-04

- Added `StoragePresentationUrlResolver` as the shared Application helper for read projections that turn stored image references into presentation URLs.
- The resolver signs only validated relative object keys, passes through absolute HTTP(S) URLs without converting their path into a provider object key, allows local `/api/storageobject/...` paths, rejects unsafe relative references, and logs failures without raw object-key/URI values.
- Migrated `EventDetailsProjectionService`, `GetEventsByTagRequestHandler`, `GetEventsByCategoryRequestHandler`, `GetGroupListRequestHandler`, `GetMyGroupsRequestHandler`, and `GetGroupDetailsRequestHandler` to the shared resolver.
- Added `StoragePresentationUrlResolverTests` proving safe object keys are signed, external URLs are not signed, local storage API paths pass through, unsafe relative references are rejected, and provider failures return null.

### Implemented Storage Projection URL Helper Completion Slice - 2026-07-04

- Migrated the remaining actor, organization, event-list, managed-events, my-events, and user profile projection handlers to `StoragePresentationUrlResolver`.
- Removed duplicated handler-local resolver logic that parsed provider object keys out of absolute URL paths.
- Removed duplicated raw object-key/URI logging templates from those projection handlers; resolver failures now log bounded image-context labels only.
- Confirmed no remaining Application feature handler matches the old `AbsolutePath.TrimStart('/')`, `ObjectKeyOrUri`, raw object-key log, or private async `ResolveImageUrl` copy patterns.

### Implemented AI Rail Raw Rendering Removal Slice - 2026-07-04

- Replaced the `AiAssistantRail.razor` prompt-reference highlight `MarkupString` builder with a `RenderFragment` backed by `RenderTreeBuilder.AddContent`.
- Dynamic prompt/reference display names now stay in Blazor's normal renderer encoding path; the highlight span remains component markup, but user-controlled text is never concatenated into raw HTML.
- Added `AiAssistantRailTests.ReferenceHighlights_EncodeDangerousReferenceDisplayNames` with malicious `<img onerror>` and `<script>` display names, proving the highlight text is encoded and no `img` or `script` DOM nodes are created.
- Confirmed `rg "MarkupString" Explore.Blazor Explore.Blazor.Client` now finds only the previously classified static `CommunityGuidelines.razor` seam.

### Implemented Community Guidelines Raw Rendering Coverage Slice - 2026-07-04

- Re-checked `CommunityGuidelines.razor`, which still uses `MarkupString` after converting tenant-configured markdown-like plain text into a constrained HTML subset.
- The renderer escapes tenant content before adding the allowed structural tags and inline `strong`/`em` formatting, so tenant-provided `<script>`, `<img>`, event handlers, and dangerous URL text remain text rather than DOM.
- Added `CommunityGuidelinesTests.Render_WhenGuidelinesContainDangerousHtml_EscapesTenantContent` for malicious tenant-customized guidelines content.

### Implemented Public Rich Text And Email HTML Encoding Slice - 2026-07-04

- Re-checked `PublicExperienceHomeBlockKind.RichText` through `Explore.Blazor.Client/Pages/Home.razor`; organization home content blocks render block `Title`, `Subtitle`, and `Body` through normal Razor/MudText content, so tenant-configured rich text is treated as encoded text rather than raw HTML.
- Added `HomeTests.Home_OrganizationRichTextBlock_RendersTenantContentAsEncodedText` with malicious `<script>`, `<img>`, and event-handler payloads, proving the rendered rich-text block preserves text content but creates no `script` or `img` DOM nodes.
- Hardened `EventLifecycleEmailOutboxFactory` so fixed lifecycle email template snippets remain trusted template HTML while organizer notification body text is HTML-encoded before it is inserted into `EmailDispatchOutbox.HtmlBody`.
- Added `EventLifecycleEmailOutboxFactoryTests` coverage proving malicious event titles and organizer notification bodies are encoded in HTML email bodies while plain-text bodies remain plain text.

### Implemented Admin Status Raw Markup Removal Slice - 2026-07-04

- Replaced `ProjectionStatusSection.razor` status-row `AddMarkupContent` calls with renderer element/content composition.
- `ProjectionStatusModel.LastErrorMessage` now flows through Blazor `RenderTreeBuilder.AddContent`, so system error text is encoded by the renderer rather than manually encoded and then inserted into raw markup.
- Replaced the static `ExposureGovernanceSection.razor` flag-header `AddMarkupContent` span with renderer element/content calls.
- Added `CustomPropertyGovernanceTests.ProjectionStatus_EncodesDangerousLastErrorText`, covering malicious `<img onerror>` and `<script>` error text and proving no `img` or `script` DOM nodes are created.
- Re-ran the Blazor raw-rendering scan; the only remaining match is the classified `CommunityGuidelines.razor` `MarkupString` seam.

### Implemented Browser Action JS Interop Slice - 2026-07-04

- Added `IBrowserActionInterop` and `BrowserActionInterop` as the typed Blazor-client boundary for browser share, clipboard, smooth-scroll, and base64 file download actions.
- Added `/js/browser-actions.js` as a JS-isolated ES module. Blazor now imports and invokes named module functions instead of evaluating string-built JavaScript.
- Replaced the discovered event preview, event detail, event list, event registration, and anonymous landing page `eval` calls with the typed interop boundary.
- Moved event ICS download from string-built `eval` into the module's `Blob`/object-URL download path with a JS-side filename safety fallback.
- Preserved UX behavior while reducing execution risk: native share still falls back to clipboard, clipboard/download failures show generic messages, and bounded service logs avoid raw titles, URLs, filenames, and base64 payloads.
- Added `BrowserActionInteropTests` for structured module arguments, fail-closed JS unavailability, and blank required value handling.
- Added `BrowserInteropSafetyTests` so Blazor source fails if `InvokeAsync("eval")`, `InvokeVoidAsync("eval")`, or direct `eval(...)` is reintroduced.
- Context7 MCP ASP.NET Core Blazor JS interop documentation was used for the JS module/import pattern. The Tavily MCP OWASP eval/DOM-sink query for this continuation returned usage-limit status `432`, so no new Tavily result from this slice is treated as source evidence.

### Implemented Legacy Download Helper Removal Slice - 2026-07-04

- Migrated `OrganizationSharedContacts` shared-contact export downloads and `InstanceAuthProviderSection` authorization policy package downloads to `IBrowserActionInterop.DownloadBase64FileAsync`.
- Removed the unused global `/js/file-download.js` helper and the `downloadFileFromBase64` global interop identifier.
- Download failures now return safe generic UI messages instead of reporting success after a failed browser action. Shared-contact export success also avoids echoing raw server-provided filenames in snackbar text.
- Extended `BrowserInteropSafetyTests` so Blazor source fails on DOM HTML-injection sinks (`innerHTML`, `outerHTML`, `insertAdjacentHTML`, `document.write`, `setHTML`) and on the legacy global download identifier.

### Implemented Contact Share Export Validation/Sanitization Slice - 2026-07-04

- Added `ContactShareConsentExportQueryRequest` and bound `ContactShareConsentController.ExportSharedContacts` through the same `IValidatableObject` API query model pattern used by paginated queries.
- Added `QueryValidationRules.ValidateContactShareExportFormat`: export format is required, cannot contain control characters, and must be one of `csv` or `tsv`. The controller passes only the normalized lowercase format to `ExportSharedContactsCommand`.
- Kept the Application handler defensive by rejecting null/blank/control-character/unsupported formats before actor/org lookup, consent reads, export audit persistence, or file generation.
- Sanitized export output: CSV/TSV cells starting with spreadsheet formula trigger characters are prefixed before separator escaping, and the server-provided filename uses a bounded slug derived from organization name instead of raw organization text.
- Hardened `Explore.Blazor.Client.Services.ContactShareConsentService`: the Blazor service normalizes `csv`/`tsv` and returns a safe null result without calling `IEventApiClient` for unsupported export formats.
- Regenerated `Explore.Blazor.Client/Clients/EventApiClient.g.cs` through the documented NSwag target after the API contract change. The generated `ExportOrganizationSharedContactsAsync` signature now exposes only `format`, `eventId`, API-version parameters, and cancellation token; no accidental normalized-format query parameter remains.
- Context7 MCP ASP.NET Core docs were used for `[ApiController]` query-model validation behavior and automatic 400 `ValidationProblemDetails`. Fresh Tavily MCP search/research attempts for OWASP API input-validation guidance returned usage-limit status `432`, so this slice did not add new Tavily evidence beyond the workstream's earlier OWASP/IETF research notes.

### Implemented Public Event Query Temporal View Validation Slice - 2026-07-04

- `EventFilterRequest.View` now validates against an explicit `TemporalView` allowlist through `QueryValidationRules.ValidateTemporalView` before `EventController.GetAll` dispatches the query.
- Unknown values such as `sideways` and undefined numeric enum values such as `999` now fail model validation instead of being silently parsed to `null` and treated as an unfiltered event list.
- The controller stays thin: `[ApiController]` model-validation behavior returns the canonical API 400 response before action execution; `ParseTemporalView` still only maps already-validated values into the Application query.
- `EventSessionFilterRequest` gained focused pagination validation coverage using the existing shared `ValidatePagination` rule; no new session-specific validator was needed.
- Added model tests in `PublicQueryValidationTests` and runtime coverage in `PublicQueryRuntimeValidationTests.EventList_WhenViewIsUnknown_ReturnsValidationProblemDetails`, proving the public API returns `400 application/problem+json`, uses the stable `view` field key, and does not echo the raw submitted query value in the body.
- Context7 MCP `/dotnet/aspnetcore.docs` was refreshed for `[ApiController]` automatic 400 behavior on model-validation failures. Fresh Tavily MCP research was not retried for this narrow continuation because the previous workstream attempts are currently blocked by usage-limit status `432`; earlier OWASP input-validation guidance remains the external security basis.

### Implemented External API Key Usage Report Query Validation Slice - 2026-07-04

- `ExternalApiKeyController.GetUsageReport` now binds `ExternalApiKeyUsageReportQueryRequest` instead of raw scalar `from`, `to`, and `tenantId` query parameters.
- `ExternalApiKeyUsageReportQueryRequest` requires both dates, rejects inverted ranges, bounds reports to `366` days, and rejects `Guid.Empty` tenant IDs through the shared query-model validation path.
- `QueryValidationRules.ValidateRequiredDateRange` is the shared helper for required bounded date pairs. Existing optional event-list date validation remains unchanged.
- The controller stays thin: `[ApiController]` returns the canonical 400 response for invalid query input before action execution, while the Application handler still owns tenant/admin authorization for valid inputs.
- Added `PublicQueryValidationTests` coverage for missing dates, inverted range, oversized range, and empty tenant ID. Added `ExternalApiKeyIntegrationTests.GetUsageReport_WithInvalidDateRange_ShouldReturnValidationProblemBeforeAdminAuthorization` so an authenticated non-admin with invalid dates receives validation ProblemDetails rather than a misleading handler-level forbidden response.
- Context7 MCP `/dotnet/aspnetcore.docs` was refreshed for `[ApiController]` automatic 400 model-validation behavior. A fresh Tavily MCP OWASP input-validation search returned usage-limit status `432`, so this slice relies on the already-recorded OWASP input-validation guidance plus the current official ASP.NET docs.

### Implemented External API Key Usage Report Semantic Authorization Slice - 2026-07-04

- Added `ExternalApiKeyUsageReportRequestHandlerTests` to lock the Application handler authorization boundary for valid usage-report query inputs.
- Tenant-scoped usage reports now have focused tests proving a caller who is neither tenant admin nor instance admin throws `AuthorizationException` before `IExternalApiKeyQuotaRepository.GetUsageByTenant` or platform-wide reads occur.
- Tenant admins are covered for the positive path: the handler reads only the requested tenant via `GetUsageByTenant` and does not fall through to `GetUsagePlatformWide`.
- Platform-wide reports are covered separately: non-instance admins fail before repository reads, while instance admins use only `GetUsagePlatformWide`.
- This is handler-level semantic proof, not HTTP runtime tenant-leak coverage. Add an API integration test later only if the matrix identifies a cheap fixture path for tenant-admin/API-key usage-report scenarios.
- Verification passed: `dotnet build Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -clp:ErrorsOnly` completed with 0 errors and existing warning debt; focused `ExternalApiKeyUsageReportRequestHandlerTests` passed 4 tests with `-- --treenode-filter`.

### Implemented External API Key Create/Update Input Hardening Slice - 2026-07-04

- `CreateExternalApiKeyDtoValidator` and `UpdateExternalApiKeyPolicyDtoValidator` now reject control characters in API key names and compare owner-scoped name uniqueness against trimmed names.
- `CreateExternalApiKeyDtoValidator` now bounds descriptions to the EF 1000-character limit and rejects description control characters before create side effects.
- `CreateExternalApiKeyCommandHandler` stores descriptions as trimmed text or `null`, matching the form/API canonicalization contract.
- Added `ExternalApiKeyInputValidation` as the Application-side helper for the shared name/description limits and normalization.
- Added Blazor-side `CreateExternalApiKeyDtoValidator` and wired `CreateApiKeyDialog.razor` to `FluentValidationValidator`, name/description counters, and trim-before-submit behavior. This is UX feedback only; Application/API validation remains authoritative.
- Added Application unit coverage in `ExternalApiKeyObservabilityTests` for create/update name control characters, overlong descriptions, normalized duplicate names, and no repository side effects for invalid input.
- Added Blazor client validator coverage in `CreateExternalApiKeyDtoValidatorTests` for control-character name, overlong description, and empty scopes.
- Added and verified API integration tests for invalid create/update input ProblemDetails, persisted-key tenant mismatch on `/api/externalapikey`, and non-create response secret absence.
- Focused API integration verification now passes 16 tests for `ExternalApiKeyIntegrationTests` plus `ExternalApiPhase0IntegrationTests`.
- Focused Application and Blazor client test lanes passed. Focused architecture tests compiled and ran, but `CleanArchitectureTests.Persistence_ShouldNotHaveDependencyOn_ApplicationDtos` fails from an unrelated Persistence/Application DTO dependency; this slice did not touch Persistence.
- Context7 MCP `/dotnet/aspnetcore.docs` was refreshed for `[ApiController]` model-validation behavior and validation ProblemDetails. A fresh Tavily MCP OWASP input-validation search returned usage-limit status `432`.

### Implemented Blazor Upload UX/Error/Log Safety Slice - 2026-07-04

- Added `ImageUploadClientPolicy` as the Blazor-client upload UX policy for accepted image formats, UX-only MIME checks, max-size messages, safe user-facing upload errors, size/content-type log buckets, and safe browser filename generation.
- Updated `ImageFileReaderService` so `FileUploadData.FileName` is sanitized before downstream BFF/API/upload clients see browser-provided filename metadata. Logs now record size/content-type buckets and failure types instead of raw browser filenames.
- Updated `ImageUploadClient`, `ImageStorageService`, and `ImageStorageRecordClient` so BFF/direct upload logs no longer record raw filenames, raw upload URLs, raw BFF/provider response bodies, raw ProblemDetails text, or raw exception messages. Upload service failures are mapped through safe allowlisted messages before page components display them.
- Updated `ImageUpload.razor`, `CreateEvent.razor(.cs)`, `EventEdit.razor(.cs)`, and `CreateOrganization.razor.cs` to share the upload policy, keep browser checks as UX only, and display generic safe upload errors instead of `Exception.Message`.
- Added focused Blazor client tests for dangerous filename sanitization, multipart upload filename sanitization, no raw BFF body/filename logging, safe metadata failure messages, safe lower-level upload-error mapping, and Create Event generic upload error display.
- Verified `dotnet build --configuration Release --verbosity quiet` passes: 25 projects, 0 errors, existing package/deprecation warnings only.
- Verified `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passes: 1453 succeeded, 1 skipped.

### Implemented Blazor Storage Display Safety Slice - 2026-07-04

- Re-audited current Blazor storage display paths. `StorageImage.razor`, `ImageUpload.razor`, and the instance/tenant storage settings sections render dynamic values through normal Razor/MudBlazor text or attribute bindings rather than `MarkupString`/`AddMarkupContent`.
- Added `SharedComponentAccessibilityTests.StorageImage_RendersCustomAltAsAttributeOnly_WhenMetadataContainsMarkup`, proving dangerous storage-adjacent alt text remains the `alt` attribute value and does not create an injected `onerror` attribute.
- Added `SharedComponentAccessibilityTests.StorageImage_RendersErrorTextAsEncodedText_WhenMetadataContainsMarkup`, proving dangerous display/error text is encoded text and does not create attacker-controlled `img` or `script` DOM nodes.
- Extended `BrowserInteropSafetyTests` with a raw-rendering allowlist guard. Any new `MarkupString` or `AddMarkupContent` use under `Explore.Blazor` or `Explore.Blazor.Client` now fails the Blazor client test suite unless it is deliberately reviewed and added to the allowlist.
- Kept `CommunityGuidelines.razor` as the only current allowlist entry because its tenant content is escaped before controlled structural markup is emitted.
- Context7 MCP ASP.NET Core Blazor docs were used for the rendering rule: normal string output is text/encoded, while `MarkupString` and `AddMarkupContent` are raw-rendering seams that must not receive untrusted values. The Tavily MCP OWASP output-encoding search for this continuation failed with usage-limit status `432`, so no new Tavily result from this slice is treated as evidence.
- Verification passed for the slice boundary: focused `SharedComponentAccessibilityTests`/`BrowserInteropSafetyTests` lane ran 13 tests with 12 passed and 1 existing skip; full `Explore.Blazor.Client.Tests` passed with 1484 succeeded and 1 existing skip; `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed with 5 projects and 0 errors; the raw-rendering scan reports only `CommunityGuidelines.razor`; `git diff --check` is clean. The solution build initially passed with 27 projects and 0 errors earlier in the slice, but a later final rerun in the dirty 28-project worktree failed outside this slice on existing warning/analyzer debt and missing `Explore.Application` reference output for unrelated projects.

### Implemented BFF Upload Session Expiry Slice - 2026-07-04

- Hardened `StorageUploadSessionStore.IssueAsync` so an already-expired API upload-session reservation fails closed as `upload_session_expired` instead of returning a browser-visible opaque session that is immediately unusable.
- Added store-level coverage for expired API reservation responses and expired cached sessions; expired cached sessions are consumed so replay attempts become `session_not_found`.
- Expanded `BffStorageUploadProxyTests` so unknown opaque session IDs and expired cached sessions return safe `400` ProblemDetails without reaching the downstream API finalize endpoint.
- Expanded BFF upload-session endpoint coverage so an expired API reservation response maps to safe `502` ProblemDetails without echoing the raw API upload-session ID.
- Verified `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet` passes: 183 succeeded.

### Implemented Storage Object ID Mismatch Runtime Slice - 2026-07-04

- Added runtime `StorageObjectControllerTests.Update_WithRouteBodyIdMismatch_WhenAuthenticated_ShouldReturnSafeValidationProblem`.
- The test proves authenticated `PUT /api/storageobject/{id}` with a different body `Id` returns canonical `400` validation ProblemDetails with `Storage object ID mismatch.` and does not echo the untrusted body ID.
- Verified the focused storage object controller runtime suite passes: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/StorageObjectControllerTests/*" --minimum-expected-tests 1` ran 18 tests.

### Implemented Storage Log Failure-Bucketing Slice - 2026-07-04

- Updated `StoragePresentationUrlResolver` so presigned URL signing failures log bounded `FailureType` values instead of raw provider exception objects.
- Updated `GetPresignedDownloadUrlRequestHandler` so by-ID presigned-download provider signing failures log bounded `FailureType` values instead of raw provider exception objects or provider messages.
- Updated `StorageObjectContentReader` so provider object-not-found and provider-unavailable paths log storage object IDs, provider labels where already part of the contract, and bounded failure types without raw exception payloads.
- Updated `BffStorageEndpoints` so upload proxy exceptions log bounded proxy failure types (`api_unavailable`, `stream_io`, etc.) instead of raw exception objects that could contain provider URLs, filenames, object keys, or upstream details.
- Verified targeted storage log scan finds no `LogError(ex, ...)` / `LogWarning(ex, ...)` calls in those storage paths.

### Implemented Storage Content Response Metadata Authority Slice - 2026-07-04

- Updated `StorageObjectContentReader` so download/content responses return the persisted `StorageObject.ContentType` that already passed reservation/finalization validation, not a provider-returned MIME hint from the object read.
- Added regression coverage proving a provider response that reports `image/svg+xml` for an object persisted as `image/png` still returns `image/png` to API/BFF callers.
- Verified focused `StorageObjectContentReaderTests` pass 4 tests, the focused architecture hygiene slice passes 13 tests, and `Explore.Application` builds successfully.

### Implemented Storage Private-Owner Runtime Masking Coverage Slice - 2026-07-04

- Added runtime API coverage in `StorageObjectControllerTests` for an active `PrivateOwner` storage object requested by a different authenticated user through both `/content` and `/presigned-url`.
- The test proves both read surfaces return the canonical safe `404 Storage object not found` ProblemDetails response and do not echo the owner user ID, provider name, object key, file name, tenant object-key prefix, or private visibility value.
- Kept RFC7807 `instance` path behavior intact: the requested route can appear in `instance`, but sensitive storage metadata must not appear in title/detail/extensions.
- Verified the focused `StorageObjectControllerTests` lane now passes 19 tests and the focused architecture hygiene slice still passes 13 tests.

### Implemented Storage Cross-Tenant Runtime Masking Coverage Slice - 2026-07-04

- Added runtime API coverage in `StorageObjectControllerTests` for an active `AuthenticatedTenant` storage object seeded under a secondary tenant and requested from the default tenant.
- The test drives the metadata detail route, `/content`, and `/presigned-url` through the real API host with endpoint authorization allowed, proving the Application/persistence lookup path fails closed before storage metadata or provider operations become browser-visible.
- All three surfaces return the canonical safe `404 Storage object not found` ProblemDetails response and do not echo the secondary tenant ID, provider name, object key prefix, file name, or authenticated-tenant visibility value.
- The focused cross-tenant test passed after rebuilding the API integration test project, the full focused `StorageObjectControllerTests` lane now passes 20 tests, LSP diagnostics are clean for the changed test file, architecture hygiene passed 13 tests, and `git diff --check` is clean.

### Implemented Storage Upload-Session Tenant/Replay/State Guard Coverage Slice - 2026-07-04

- Expanded `StorageUploadSessionCommandHandlerTests` with Application-level guard coverage for same-user wrong-tenant finalize and cancel attempts.
- Wrong-tenant finalize now has explicit no-side-effect coverage: no usage-counter lookup, no provider write, no `StorageObject` creation, and no upload-session update after the handler returns `StorageUploadSessionNotFound`.
- Wrong-tenant cancel now has explicit no-side-effect coverage: no usage-counter lookup, no quota update, and no upload-session update after the handler returns `StorageUploadSessionNotFound`.
- Added replay/state coverage proving an already-finalized upload session returns idempotent success without replaying provider writes, creating storage metadata, finalizing quota again, or mutating the session.
- Added canceled-session finalize coverage proving a canceled upload session returns `StorageUploadSessionInvalidState` without provider writes, storage metadata creation, quota updates, or session mutation.
- No production code patch was needed in this slice; `CancelStorageUploadSessionCommandHandler` and `FinalizeStorageUploadSessionCommandHandler` already enforce tenant/current-user accessibility before side effects and already short-circuit finalized/canceled states.
- Verification passed for the focused `StorageUploadSessionCommandHandlerTests` lane with 26 tests, LSP diagnostics for the touched test file, separate focused architecture hygiene class filters, and `git diff --check`. Current test/build output still includes existing NuGet audit/package warnings.

### Implemented Storage API Upload-Session Semantic Runtime Coverage Slice - 2026-07-04

- Expanded runtime `StorageObjectControllerTests` so authenticated API requests now cover upload-session semantic failures at the HTTP boundary, not only handler-level outcomes.
- Missing upload-session finalize and cancel requests now prove `StorageObjectController.UploadSessionContent` and `CancelUploadSession` return safe `404 Storage upload session not found` ProblemDetails.
- Canceled upload-session finalization now proves the API returns safe `409 Storage upload session conflict` ProblemDetails with `storage_upload_session_invalid_state`.
- Finalized upload-session cancellation now proves the API returns safe `409 Storage upload session conflict` ProblemDetails with `storage_upload_session_finalized`.
- The new response checks prove failure bodies do not echo tenant IDs, provider names, tenant object-key prefixes, raw file names, checksums, or private visibility metadata.
- No production code patch was needed in this slice; `CommandResponseResultMapper` and the upload-session handlers already produced the correct safe ProblemDetails mapping.
- Verification passed for the three focused new API integration cases individually, the full focused `StorageObjectControllerTests` lane with 23 tests, LSP diagnostics for the touched test file, focused architecture hygiene filters, and `git diff --check` for the touched test/docs files. Current output still includes existing NuGet audit/package and analyzer warnings.

### Implemented Storage Infrastructure Health Log Redaction Slice - 2026-07-04

- `S3FileStorageProvider.TestAsync` now logs a bounded `FailureType` on failed S3-compatible bucket probes instead of passing the raw exception object into `ILogger`.
- `LocalFileStorageProvider.TestAsync` now logs a bounded `FailureType` on failed local filesystem probes instead of passing the raw exception object into `ILogger`.
- Legacy `ObjectStorageService.TestConnectionAsync` no longer logs the configured S3 endpoint or raw exception object when a connection probe fails.
- `Explore.Infrastructure.Tests/Infrastructure/TestListLogger.cs` captures rendered structured log messages and exception payloads for storage diagnostic assertions.
- `S3FileStorageProviderTests.TestAsync_WhenBucketProbeFails_LogsFailureTypeWithoutRawProviderPayload` proves formatted logs omit provider exception text, endpoint, bucket name, and secret key.
- `LocalFileStorageProviderTests.TestAsync_WithUnwritableRoot_LogsFailureTypeWithoutRawFilesystemPath` proves formatted logs omit local filesystem paths and raw exception text.
- `ObjectStorageServiceTests.TestConnectionAsync_WhenProbeFails_LogsFailureTypeWithoutEndpointOrProviderPayload` proves legacy S3 connection probes omit endpoint, bucket name, secret key, and provider exception text.
- Context7 evidence used for this slice: ASP.NET Core logging docs support structured message templates and show exception-object logging includes exception message/stack trace; the storage contract therefore uses bounded `FailureType` fields for sensitive provider failures. Tavily MCP search/extract attempts for OWASP logging/file-upload guidance returned usage-limit status `432`, so this slice relies on previously incorporated OWASP guidance plus repo security docs.
- Verification passed: `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/S3FileStorageProviderTests/*|/*/*/LocalFileStorageProviderTests/*|/*/*/ObjectStorageServiceTests/*" --minimum-expected-tests 1 --no-progress --no-ansi --output Normal` passed 6 tests; `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed 4 projects with existing AutoMapper NU1903 warnings; `git diff --check` passed for the touched infrastructure files.

### Implemented Storage Upload Failure Response Redaction Slice - 2026-07-04

- `CommandResponseResultMapper.ToStorageUploadProblem` now treats known storage upload failure codes as an API response contract, not a pass-through for `BaseCommandResponse.Message` or `Errors`.
- Known storage upload failures keep their existing HTTP status, title, problem type, and `code` extension, but `detail` now comes from API-owned canonical text keyed by `FailureCode`.
- Known storage upload validation failures now build `ValidationProblemDetails.errors["storageUpload"]` from the same canonical text, so lower-layer error collections cannot echo provider diagnostics, object keys, policy internals, tenant-scoped paths, or unsafe content-type details.
- Unknown storage validation failures still use the generic command validation mapper until they are classified in the matrix; this keeps the slice narrow and avoids broad response-shape churn.
- Added `StorageUploadSessionControllerTests.UploadSessionContent_WhenProviderFailureContainsInternalData_ReturnsCanonicalProblemDetails` for the 503 provider/write-failure path.
- Added `StorageUploadSessionControllerTests.UploadSessionContent_WhenValidationFailureContainsInternalData_ReturnsCanonicalValidationProblem` for the 400 validation path.
- Verification passed: `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed 8 projects with existing warnings; focused `StorageUploadSessionControllerTests` passed 14 tests; focused `StorageObjectControllerTests` passed 23 tests.

### Implemented Storage Object Metadata Create Failure Mapping Slice - 2026-07-04

- `StorageObjectController.Create` now uses a create-specific `ApiValidationProblemDescriptor` and maps failed `CreateStorageObjectCommand` responses through the canonical command validation ProblemDetails mapper.
- Failed metadata create commands now return `400 Storage object validation failed` with the stable `storageObject` error key and `validation_failed` code instead of returning `200 OK` with `Success=false`.
- Successful metadata create commands still return the existing `200 OK` command response shape.
- Added `StorageUploadSessionControllerTests.Create_WhenHandlerValidationFails_ReturnsValidationProblemDetails` to lock the create failure HTTP contract.
- Verification passed: `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed 8 projects with existing warnings; focused `StorageUploadSessionControllerTests` passed 15 tests; focused `StorageObjectControllerTests` passed 23 tests.

### Implemented Storage Metadata Update Tenant-Authority Slice - 2026-07-04

- `CreateStorageObjectCommand` no longer forwards `StorageObjectDto.TenantId` as authorization metadata. Create authorization is collection-scoped; persisted tenant ownership stays server-owned through `ITenantContext`.
- `UpdateStorageObjectCommand` no longer forwards `StorageObjectDto.TenantId` as authorization metadata. `AuthorizationBehavior` enriches storage-object authorization from the persisted `StorageObject` loaded by id, so Cerbos/fallback authorization sees persisted tenant authority rather than DTO tenant claims.
- `UpdateStorageObjectCommandHandler` no longer maps an update DTO into a fresh `StorageObject`. It loads the existing storage object, verifies it belongs to the current tenant, patches allowed metadata fields onto that entity, preserves persisted `TenantId`, and falls back blank `SafeDisplayName` to `FullName`.
- Missing or wrong-tenant storage metadata updates return a safe failed command response with `Storage object not found.` and do not call repository update. This keeps tenant/resource existence masking aligned with the storage read runtime masking slices.
- Added `StorageObjectCommandHandlerTests` for create/update authorization metadata, DTO tenant tampering, persisted tenant preservation, allowed-field patching, and wrong-tenant no-update behavior. Added `AuthorizationBehaviorTests.Handle_WithStorageObjectResource_EnrichesPersistedStorageAuthorizationContext` to prove persisted storage authorization enrichment.
- Context7 MCP ASP.NET Core docs were refreshed for this continuation and reinforce the same implementation contract: validate/process untrusted request data through server-owned model/application paths, use ProblemDetails-style validation responses, treat uploaded/file metadata as untrusted input, and keep Blazor rendering on encoded text/attribute paths unless a raw-rendering seam is explicitly reviewed. A fresh Tavily MCP OWASP search for input-validation/file-upload/logging guidance returned usage-limit status `432`; no new Tavily result from this continuation is treated as evidence beyond the workstream's earlier OWASP/IETF notes.
- Verification passed: `dotnet build Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -clp:ErrorsOnly` passed 3 projects with 0 errors and existing AutoMapper NU1903 warnings; focused `StorageObjectCommandHandlerTests` passed 4 tests; focused `AuthorizationBehaviorTests` passed 18 tests. An initial VSTest-style `--filter` run failed because this TUnit/Microsoft.Testing.Platform project requires `-- --treenode-filter`; the corrected focused commands passed.

### Current Recommended Next Slice

Continue with the highest-risk rows that are not already covered by the completed external API key and storage slices:

1. BFF boundary residual audit: target setup/auth/preference/proxy routes where browser-controlled headers, cookies, continuation paths, antiforgery, or diagnostics can cross trust boundaries.
2. Blazor form convergence: event/session/organization forms should map API ProblemDetails into `EditContext`, keep accessible validation summaries/focus behavior, prevent duplicate submits, and gate actions by HAL links.
3. Email dispatch admin semantics: cover tenant-B outbox masking and raw SMTP/provider/log leakage where not already proven.
4. Idempotency and cross-cutting headers: cover idempotency-key, correlation/request-id, and tenant-hint validation rows.

Storage and raw-rendering work should continue only from a new matrix-backed endpoint/display/log row. Do not repeat the completed storage upload-session, presigned-download, provider-result, content-signature, log-redaction, current Blazor storage display, or raw-markup/eval/global-download slices.

### Blockers

No planning blocker remains.

Known caveats:

- The worktree contains many unrelated changes outside this workstream. Do not revert them.
- `RTK.md` is referenced by the incoming agent context but was not present at repository root during the rebaseline.
- `docs/UI_GOVERNANCE.md` and `docs/DESIGN_SYSTEM.md` have wording tension around form/component wrappers. For this workstream, follow `docs/UI_GOVERNANCE.md`: `EditForm` + `EditContext` + repo form primitives, no `MudForm`.
- Focused `Event.Application.UnitTests` storage lanes currently compile and pass. The broader worktree still contains unrelated dirty Webhooks, settings, infrastructure, and docs changes; verify full-suite status before broad completion claims.
- Full `Event.Application.UnitTests` currently fails on unrelated dirty settings test code: `SettingHandlerTests.Batch_CerbosEndpoints_NormalizesBareHostsBeforePersisting` throws NSubstitute `AmbiguousArgumentsException` for `SetValueAsync(String, String, SettingScope, Guid, Guid, CancellationToken)`.
- Focused agent-context architecture tests now pass in the current worktree. The full `Event.Architecture.Tests` suite currently fails on unrelated support-access authorization parity drift: `islamuevent_support_access_session` has no Cerbos policy file and no `FallbackAuthorizationService` case.

---

## 2. Verified Codebase Reality

This workstream is already partially implemented. Do not restart from older planning assumptions.

| Area | Verified Evidence | Meaning For Future Work |
|---|---|---|
| API validation problem details | `Explore.API/Program.cs` and `Explore.API/ExceptionHandling/ApiValidationProblemDetailsFactory.cs` implement canonical model-state/problem-details behavior. | Extend through tests and request model coverage. Do not duplicate validation response logic in controllers. |
| Unknown JSON member rejection | `Explore.API/Program.cs` configures unmapped JSON member handling. | Keep this strict by default. Any exception must be intentional and documented. |
| Public query validation | `Explore.API/Models/EventFilterRequest.cs`, `EventSessionFilterRequest.cs`, `PaginatedQueryRequests.cs`, and `QueryValidationRules.cs` contain query validation logic. Public event-list temporal `view` values are now allowlisted before controller dispatch instead of silently ignored when unknown. External API key usage reports now use a query model that validates required bounded dates and optional tenant ID shape before handler authorization. | Audit remaining query surfaces before marking the query lane complete. Event and event-session syntactic/semantic runtime coverage is still matrix-backed work; external API key usage-report shape validation is implemented. |
| External API key management and usage report | `CreateExternalApiKeyDtoValidator`, `UpdateExternalApiKeyPolicyDtoValidator`, `CreateExternalApiKeyCommandHandler`, `UpdateExternalApiKeyPolicyCommandHandler`, and `GetExternalApiKeyUsageReportRequestHandler` own external API key validation and reporting semantics. Names are trimmed before uniqueness checks and persistence, control characters are rejected, descriptions are bounded/rejected for controls and normalized to trimmed/null, owner scope ceilings remain Application-side, and owner authority remains handler-side. `CreateApiKeyDialog.razor` now has a Blazor UX validator for basic name/description/scope errors. Successful create issuance is covered so the raw key is returned once, only the secret hash is persisted, and handler logs omit the raw key and secret fragment. API integration coverage verifies persisted-key tenant mismatch on `/api/externalapikey` and non-create detail response secret absence. Usage-report handler tests prove tenant-scoped and platform-wide report reads fail closed before repository access when the caller lacks tenant/instance admin authority. | Implemented for create/update validation, Blazor UX validation, response/log/persistence safety, usage-report query shape validation, and usage-report handler semantic authorization. Optional future HTTP runtime tenant-admin coverage should be added only if a matrix row identifies a cheap fixture path. Do not treat Blazor validation as an authority source. |
| Idempotency validation | `Explore.API/Middleware/IdempotencyRequestIdentity.cs` and `IdempotencyMiddleware.cs` exist with tests for fingerprint mismatch behavior. | Preserve fingerprint semantics when changing write request contracts. |
| Upload request validation | `Explore.Application/DTOs/StorageObject/Validators/UploadRequestDtoValidator.cs` exists with storage validation tests and now rejects reserved filenames and malformed multi-segment MIME hints. `CreateStorageUploadSessionDtoValidator.cs` covers upload-session MIME syntax, unsafe file/display names, and unsafe extension tokens. `CreateStorageUploadSessionCommandHandler.cs` normalizes MIME before policy resolution and persistence. `CreateStorageObjectDtoValidator.cs` and `UpdateStorageObjectDtoValidator.cs` now validate object keys, filename/display metadata, extension tokens, content-type syntax, SHA-256 hex shape, and owning-resource pair consistency. `CancelStorageUploadSessionCommandHandler.cs` and `FinalizeStorageUploadSessionCommandHandler.cs` now enforce tenant/current-user ownership before side effects; focused tests cover wrong-user and same-user wrong-tenant finalize/cancel masking, finalized replay idempotency without provider replay, and canceled finalize invalid-state behavior without side effects. Finalize now validates known image/document byte signatures and extension allowlists before provider writes, replays inspected prefixes for non-seekable streams, and validates provider write metadata before storage object creation: provider, tenant-scoped object key, byte count, content type, and SHA-256 digest must match the reserved session contract. `CommandResponseResultMapper` now canonicalizes known storage upload failure `detail` and validation `errors` from `FailureCode` instead of echoing lower-layer `BaseCommandResponse.Message` or `Errors`. `StorageObjectController.Create` now maps failed metadata create command responses to canonical `400` validation ProblemDetails instead of returning `200 OK` with `Success=false`. Storage metadata create/update commands no longer forward DTO `TenantId` into authorization context: create is collection-scoped, update is authorized against the persisted storage object, and `UpdateStorageObjectCommandHandler` loads the current-tenant entity before patching allowed metadata fields while preserving persisted `TenantId`. `GetPresignedDownloadUrlRequestHandler.cs` now validates expiration bounds, reads metadata by ID, enforces lifecycle/visibility/current-user read semantics, signs the persisted provider object key, suppresses provider object-key response echo, maps inaccessible results to a 404 through `StorageObjectController`, and buckets provider signing failures without raw exception payloads. `StorageObjectContentReader.cs` now returns the persisted validated content type for content/download responses instead of provider-returned MIME hints. `S3FileStorageProvider`, `LocalFileStorageProvider`, and legacy `ObjectStorageService` now bucket health/connection probe failures without logging raw exception payloads, configured endpoints, bucket names, local filesystem paths, or storage secrets. `StorageObjectControllerTests` now cover runtime upload-session authentication requirements, malformed upload-session route-ID rejection, authenticated upload-session missing/canceled/finalized semantic failures, authenticated route/body storage object ID mismatch responses, private-owner content/presigned read masking for different authenticated users, and cross-tenant metadata/content/presigned read masking for secondary-tenant storage objects. Current Blazor storage display components render storage-adjacent text through encoded bindings and are protected by the raw-rendering allowlist guard. | Continue into residual storage work beyond auth/route constraints, upload-session missing/canceled/finalized API semantics, known storage upload failure response-redaction, storage object create failure-to-400 mapping, storage metadata update tenant-authority, by-ID presigned download, BFF expiry, ID-mismatch contracts, content-response content-type authority, private-owner same-tenant masking, cross-tenant read masking, storage infrastructure health log-redaction, current Blazor storage display safety, and Application handler tenant/replay/state guards: response echo safety outside the known upload-session failure mapper and any newly discovered matrix-backed endpoint seams. Future storage object list/detail UI must add a matrix row before rendering additional metadata fields. The current DTO/domain model has no arbitrary metadata dictionary, so metadata key/value/count validation is not an active code seam. |
| Blazor server validation mapping | `Explore.Blazor.Client/Components/Forms/ServerValidationErrorStore.cs` exists. | Use it for API problem-details mapping instead of one-off component code. |
| Blazor form primitives | Multiple forms already use `EditContext`, `FormSubmissionGuard`, `AppValidationSummary`, and server validation stores. | Continue convergence with existing primitives. Do not introduce `MudForm`. |
| Blazor image upload UX | `ImageUploadClientPolicy` centralizes Blazor-side upload UX policy, safe browser filename generation, safe user-facing upload errors, and safe log buckets. `ImageFileReaderService` sanitizes browser-provided filenames into `FileUploadData.FileName`. `ImageUploadClient`, `ImageStorageService`, and `ImageStorageRecordClient` avoid raw filename, upload URL, BFF/provider body, ProblemDetails, and exception-message logs in upload paths. `ImageUpload.razor`, event create/edit uploads, and organization logo upload use shared policy and safe generic errors. Focused `Explore.Blazor.Client.Tests` coverage proves dangerous filename sanitization, multipart filename safety, no raw BFF body/filename logging, safe metadata failure mapping, safe service failure mapping, and Create Event generic upload errors. | Implemented for current image upload surfaces. Continue only residual UI form server-error mapping and any future upload surfaces discovered by the matrix. |
| BFF boundary tests | Blazor integration tests exist around auth setup, preferences, storage proxy, and YARP security. `BffStorageUploadProxyTests` now cover reserved device filenames, malformed multi-segment MIME hints, content-type mismatch, different-user sessions, raw upload URL rejection, consume-once behavior, unknown opaque session IDs, expired cached sessions, and expired API reservation responses. | Treat BFF work as a residual audit, not a full rewrite. Continue only with uncovered BFF-only seams and storage edge cases beyond the now-covered upload-session expiry/missing-session behavior. |
| Raw markup | `CommunityGuidelines.razor` remains the only `MarkupString` found under `Explore.Blazor`/`Explore.Blazor.Client`; it renders tenant-configured markdown-like text through an escaping renderer and now has malicious-content coverage. `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor` no longer uses `MarkupString`; prompt-reference highlights render through a `RenderFragment` and `RenderTreeBuilder.AddContent`, with malicious display-name coverage in `AiAssistantRailTests`. `ProjectionStatusSection.razor` now renders admin projection status rows through `RenderTreeBuilder.AddContent`, with malicious `LastErrorMessage` coverage in `CustomPropertyGovernanceTests`. `ExposureGovernanceSection.razor` no longer uses raw markup for the static flag-header span. Public home `RichText` blocks render as encoded text through normal Razor/MudText content and now have malicious-content bUnit coverage. `EventLifecycleEmailOutboxFactory` treats lifecycle HTML as system-template HTML and encodes organizer/body interpolation before writing `EmailDispatchOutbox.HtmlBody`. `StorageImage` storage-adjacent alt/error display has malicious metadata coverage. | Current Blazor raw-markup inventory has no unclassified `MarkupString` or `AddMarkupContent` matches, and `BrowserInteropSafetyTests` now fails on unreviewed raw rendering outside the allowlist. Continue residual review for JS interop/eval and any future sanitizer decision for intentionally rich HTML. |
| API client generation | `docs/API.md` defines generation commands for OpenAPI/client artifacts. | Implementation agents own generation when contracts change. |

---

## 3. Active Architecture Decisions

Use these decisions unless a new source-grounded decision record supersedes them.

1. Server-side validation is authoritative. Blazor validation is UX, accessibility, and error mapping.
2. Application validators are manually instantiated and use `ValidateAsync`.
3. Controllers bind and dispatch only; no business validation in controllers.
4. BFF validation protects BFF-only seams and token boundaries; it does not duplicate Application command validators.
5. Default XSS defense is framework output encoding and context-aware encoding.
6. Sanitization is only for approved renderable content seams with allowlists and tests.
7. Canonicalization is explicit and auditable; validators should not silently mutate persisted commands.
8. HAL links drive UI action affordances.
9. OpenAPI/client generation is implementation-owned.

---

## 4. Fresh Research Evidence

### Tavily MCP

Tavily was used successfully during the 2026-07-04 planning pass. Later Tavily calls during implementation continuations, including the storage metadata tenant-authority continuation, hit usage-limit status `432`, so the bullets below come from the earlier successful planning evidence, not from the later blocked calls. Use the following as current external guidance:

- OWASP Input Validation Cheat Sheet: validate all untrusted input with syntactic and semantic checks; prefer allowlists.
- OWASP XSS Prevention Cheat Sheet: output encoding is the primary defense; sanitization is specific to safe HTML handling; global response-side filters are fragile.
- OWASP File Upload Cheat Sheet: treat filenames and `Content-Type` as untrusted; validate extension, MIME, signature, size, storage location, and authorization.
- OWASP Logging Cheat Sheet: validate, sanitize, or encode event data before logging; do not log sensitive raw inputs.
- IETF HTTPAPI Idempotency-Key draft: same-key/same-fingerprint retries replay the completed result, concurrent in-progress retries are conflicts, and the draft examples use 422 for same-key/different-payload reuse. This repo currently preserves `409 idempotency_key_reuse` for different-payload reuse as an accepted local API contract.

### Context7 MCP

Context7 was used successfully on 2026-07-04.

Use these current framework references:

- `/dotnet/aspnetcore.docs` for ASP.NET Core problem details, `InvalidModelStateResponseFactory`, Blazor `EditContext`, `ValidationMessageStore`, antiforgery, and file-upload practices.
- 2026-07-04 storage metadata tenant-authority continuation refreshed `/dotnet/aspnetcore.docs` for server-owned validation, ProblemDetails-style validation responses, file/upload metadata distrust, and encoded Blazor rendering versus reviewed raw-rendering seams.
- `/fluentvalidation/fluentvalidation` for manual validation, `ValidateAsync`, and async-rule limitations in automatic ASP.NET validation.
- `/websites/mudblazor` for generic MudBlazor form API awareness only. Do not copy generic `MudForm` guidance into this repo because `docs/UI_GOVERNANCE.md` is authoritative for the local pattern.

---

## 5. Files Future Implementers Should Read First

Always read:

- `AGENTS.md`
- `docs/QUICK_REFERENCE.md`
- `docs/GOVERNANCE.md`
- `docs/OPERATIONS.md`
- `docs/API.md`
- `docs/BLAZOR.md`
- `docs/SECURITY-MODEL.md`
- `docs/TESTING.md`
- `.claude/contract/intents.yaml`
- This workstream's plan, tasks, matrix, and contract decisions.

Read by slice:

| Slice | Additional Required Reads |
|---|---|
| Application/API validation | `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md`, `.agents/skills/cqrs-mediatr-guidelines/SKILL.md`, `.agents/skills/clean-architecture-rules/SKILL.md` |
| HAL/API affordances | `.claude/rules/api-hateoas.md`, `docs/AUTHORIZATION.md` |
| BFF | `.claude/rules/blazor-server.md`, `.agents/skills/auth-patterns/SKILL.md`, `.agents/skills/blazor-bff-patterns/SKILL.md` |
| Blazor forms | `.claude/rules/blazor-client.md`, `.agents/skills/blazor-ui-conventions/SKILL.md`, `.agents/skills/blazor-css-isolation/SKILL.md`, `docs/UI_GOVERNANCE.md`, `docs/ACCESSIBILITY.md` |
| Tests | `.claude/rules/tests.md`, `.agents/skills/source-command-check/SKILL.md` |

---

## 6. Slice Guidance

### API And Application

Use this flow:

1. Identify the request DTO/query/header/route seam in the matrix.
2. Decide whether the rule is syntactic, semantic, authorization, tenancy, idempotency, or persistence uniqueness.
3. Add syntactic/cross-field FluentValidation validators in Application when the request maps to an Application command/query.
4. Add handler semantic checks when the rule needs tenant context, repositories, clock, state, idempotency, or side-effect ordering.
5. Add API model/request validation only for transport and binding concerns.
6. Verify problem-details field keys and messages are safe.

Do not:

- Register validators globally in DI.
- Put repository-backed validation in controllers.
- Return DTOs from repositories.

### BFF

Use this flow:

1. Identify whether the input is BFF-only or a normal API command.
2. For BFF-only input, validate route, query, headers, setup secret shape, antiforgery/exception controls, proxy target, and upload/session binding.
3. For normal API commands, rely on generated clients/services and map API problem details into Blazor.
4. Add tests for token isolation and browser-visible response safety.

Do not expose bearer tokens to `Explore.Blazor.Client`.

### Blazor Client

Use this flow:

1. Use `EditForm` + `EditContext`.
2. Use existing form primitives and validation summary components.
3. Add local validation for immediate feedback only.
4. Map server-side problem details through `ServerValidationErrorStore`.
5. Gate affordances by HAL links.
6. Add bUnit/component tests for invalid local input, server errors, duplicate submit, and link-gated actions.

Do not introduce `MudForm` or direct Application validator references.

### Sanitization And Raw Rendering

Use this flow:

1. Inventory raw-rendering seams.
2. Classify each seam as controlled markup, encoded text, sanitized rich content, or remove.
3. Prefer component composition over `MarkupString`.
4. If a sanitizer is needed, put it in the owning server/Application seam unless there is a clear display-only reason.
5. Add malicious-payload tests.

Prioritize:

- Completed current behavior: `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor`, `CommunityGuidelines.razor`, `PublicExperienceHomeBlockKind.RichText`, and current `EmailMessage.HtmlBody` lifecycle/organizer factory composition.
- Completed current behavior: `ProjectionStatusSection.razor` and `ExposureGovernanceSection.razor` no longer use `AddMarkupContent`.
- Remaining: residual JS interop/eval review and any future product decision to support sanitized rich HTML.

---

## 7. Verification Notes

Use project-scoped tests. Do not run solution-level `dotnet test`.

Earlier storage reservation verification on 2026-07-03:

- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/CreateStorageUploadSessionDtoValidatorTests/*" --minimum-expected-tests 1` passed 26 tests.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/StorageUploadSessionCommandHandlerTests/CreateHandle_NormalizesContentTypeBeforePolicyResolutionAndPersistence" --minimum-expected-tests 1` passed 1 test.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet` passed 1,791 tests at that point; current full-suite status is listed under the upload-session ownership slice below.
- `dotnet build --configuration Release --verbosity quiet` passed for 25 projects.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet` now passes in the current worktree.
- LSP diagnostics and `git diff --check` were clean for the changed Application and test files.

Latest implemented storage metadata slice on 2026-07-03:

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed.
- LSP diagnostics and `git diff --check` were clean for `StorageObjectMetadataValidation.cs`, `UploadRequestDtoValidator.cs`, `CreateStorageObjectDtoValidator.cs`, `UpdateStorageObjectDtoValidator.cs`, `StorageObjectMetadataDtoValidatorTests.cs`, and `UploadRequestDtoValidatorTests.cs`.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/StorageObjectMetadataDtoValidatorTests/*|/*/*/UploadRequestDtoValidatorTests/*" --minimum-expected-tests 1` passed 37 tests.

Latest implemented upload-session ownership slice on 2026-07-03:

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed for the Application handler changes.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/StorageUploadSessionCommandHandlerTests/*" --minimum-expected-tests 1` passed 18 tests.
- LSP diagnostics were clean for `CancelStorageUploadSessionCommandHandler.cs`, `FinalizeStorageUploadSessionCommandHandler.cs`, and `StorageUploadSessionCommandHandlerTests.cs`.
- `git diff --check` was clean for the changed storage handler, validator, test, and workstream files.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet` failed 1 unrelated dirty settings test: `SettingHandlerTests.Batch_CerbosEndpoints_NormalizesBareHostsBeforePersisting`.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet` passed 240 tests with 1 documented skip.

Latest implemented upload-session tenant/replay/state guard coverage slice on 2026-07-04:

- `dotnet format whitespace --include Event.Application.UnitTests/Features/StorageObjects/Commands/StorageUploadSessionCommandHandlerTests.cs --verbosity quiet` completed successfully.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/StorageUploadSessionCommandHandlerTests/*" --minimum-expected-tests 1` passed 26 tests, including same-user wrong-tenant finalize/cancel masking, finalized replay idempotency, and canceled finalize invalid-state coverage.
- LSP diagnostics found no issues in `Event.Application.UnitTests/Features/StorageObjects/Commands/StorageUploadSessionCommandHandlerTests.cs`.
- Separate focused architecture hygiene filters passed: `CleanArchitectureTests` ran 13 tests, `CodeHygieneTests` ran 4 tests, and `NamingConventionTests` ran 10 tests.
- `git diff --check` completed successfully.
- `dotnet build --configuration Release --verbosity quiet` passed for 25 projects with 0 errors and 1,666 existing warnings.
- The focused test/build commands still emit existing NuGet audit/package warnings before execution, including `AutoMapper` NU1903 and, in architecture runs, `Microsoft.OpenApi` NU1903 plus `Microsoft.CodeAnalysis.Workspaces.MSBuild` NU1608.

Latest implemented BFF storage boundary slice on 2026-07-04:

- `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet` passed for the BFF endpoint changes, with existing package vulnerability/deprecation warnings.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/BffStorageUploadProxyTests/*" --minimum-expected-tests 1` passed 10 tests.
- LSP diagnostics were clean for `Explore.Blazor/Extensions/BffStorageEndpoints.cs` and `Explore.Blazor.IntegrationTests/Endpoints/BffStorageUploadProxyTests.cs`.
- `git diff --check` was clean for the changed BFF endpoint and BFF integration test files.

Latest implemented storage presigned-download slice on 2026-07-04:

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed with existing dependency warnings.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed with existing dependency warnings.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/GetPresignedDownloadUrlRequestHandlerTests/*" --minimum-expected-tests 1` passed 5 tests.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/StorageUploadSessionControllerTests/*" --minimum-expected-tests 1` passed 10 tests.
- LSP diagnostics were clean for `GetPresignedDownloadUrlRequestHandler.cs`, `GetPresignedDownloadUrlRequestHandlerTests.cs`, `StorageObjectController.cs`, and `StorageUploadSessionControllerTests.cs`.
- `git diff --check` was clean for the changed Application handler, API controller, focused tests, and workstream files.

Latest implemented storage provider-result validation slice on 2026-07-04:

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed for the Application handler change, with existing dependency/analyzer warnings.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/StorageUploadSessionCommandHandlerTests/*" --minimum-expected-tests 1` passed 19 tests after rebuilding the test project.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/StorageUploadSessionCommandHandlerTests/*" --minimum-expected-tests 1` passed 19 tests on the rebuilt artifacts.
- LSP diagnostics were clean for `FinalizeStorageUploadSessionCommandHandler.cs` and `StorageUploadSessionCommandHandlerTests.cs`.
- `git diff --check` was clean for the changed Application handler and focused test file.

Latest implemented storage API failure-mapping coverage slice on 2026-07-04:

- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/StorageUploadSessionControllerTests/*" --minimum-expected-tests 1` passed 11 tests after rebuilding the API integration test project.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/StorageUploadSessionControllerTests/*" --minimum-expected-tests 1` passed 11 tests on the rebuilt artifacts.
- LSP diagnostics were clean for `StorageUploadSessionControllerTests.cs`.
- `git diff --check` was clean for the changed API integration test file.

Latest implemented storage content-signature slice on 2026-07-04:

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed for `StorageContentSignaturePolicy` and the finalize handler integration, with existing warnings.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed for the API failure-code mapping, with existing warnings.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/StorageUploadSessionCommandHandlerTests/*" --minimum-expected-tests 1` passed 22 tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/CleanArchitectureTests/*|/*/*/CodeHygieneTests/*|/*/*/NamingConventionTests/*" --minimum-expected-tests 1` passed 13 tests.
- `git diff --check` was clean for `StorageContentSignaturePolicy.cs`, `FinalizeStorageUploadSessionCommandHandler.cs`, `FailureCodes.cs`, `CommandResponseResultMapper.cs`, `StorageUploadSessionCommandHandlerTests.cs`, and `StorageUploadSessionControllerTests.cs`.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passed with existing warnings.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/StorageUploadSessionControllerTests/*" --minimum-expected-tests 1` passed 11 tests, covering the updated controller mapping case.

Latest implemented Blazor storage display safety slice on 2026-07-04:

- `StorageImage.razor`, `ImageUpload.razor`, and instance/tenant storage settings were re-audited for dynamic storage-adjacent display paths. They use normal Razor/MudBlazor text and attribute bindings, not raw `MarkupString`/`AddMarkupContent`.
- `SharedComponentAccessibilityTests.StorageImage_RendersCustomAltAsAttributeOnly_WhenMetadataContainsMarkup` proves dangerous alt metadata remains an attribute value and does not create an injected `onerror` attribute.
- `SharedComponentAccessibilityTests.StorageImage_RendersErrorTextAsEncodedText_WhenMetadataContainsMarkup` proves dangerous display/error text is encoded and does not create attacker-controlled `img` or `script` DOM nodes.
- `BrowserInteropSafetyTests.BlazorSource_UsesRawHtmlRenderingOnlyInReviewedAllowlist` now fails on any new `MarkupString` or `AddMarkupContent` under `Explore.Blazor` or `Explore.Blazor.Client` unless deliberately allowlisted.
- Context7 MCP ASP.NET Core Blazor docs were used for the normal string rendering versus raw-rendering seam contract. Tavily MCP OWASP output-encoding search failed with usage-limit status `432`, so no new Tavily result from this slice is evidence.
- Verification passed for the slice boundary: focused storage-display/raw-rendering lane ran 13 tests with 12 succeeded and 1 existing skip; full `Explore.Blazor.Client.Tests` passed with 1484 succeeded and 1 existing skip; `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed with 5 projects and 0 errors; the raw-rendering scan reports only `CommunityGuidelines.razor`; `git diff --check` is clean. The solution build initially passed with 27 projects and 0 errors earlier in the slice, but a later final rerun in the dirty 28-project worktree failed outside this slice on existing warning/analyzer debt and missing `Explore.Application` reference output for unrelated projects.

Latest implemented storage projection URL helper slice on 2026-07-04:

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed for the shared resolver and projection handler changes, with existing dependency/analyzer warnings.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/StoragePresentationUrlResolverTests/*" --minimum-expected-tests 1` passed 5 tests after rebuilding the test project.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/StoragePresentationUrlResolverTests/*" --minimum-expected-tests 1` passed 5 tests on the rebuilt artifacts.
- LSP diagnostics were clean for `StoragePresentationUrlResolver.cs`, `StoragePresentationUrlResolverTests.cs`, `EventDetailsProjectionService.cs`, `GetEventsByTagRequestHandler.cs`, `GetEventsByCategoryRequestHandler.cs`, and the three touched Group query handlers.
- `git diff --check` was clean for the shared resolver, resolver tests, and touched projection handlers.

Latest implemented projection-helper completion slice on 2026-07-04:

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed for the remaining actor, organization, event-list, managed-events, my-events, and user projection handler migration, with the existing AutoMapper package advisory warning.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/StoragePresentationUrlResolverTests/*" --minimum-expected-tests 1` passed 5 tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/AgentContextSchemaTests/*|/*/*/AgentContextIntentManifestTests/*|/*/*/AgentContextDuplicationTests/*" --minimum-expected-tests 1` passed 9 tests.
- LSP diagnostics were clean for all 11 newly migrated projection handlers.
- `git diff --check` was clean for all 11 newly migrated projection handlers.
- `rg` found no remaining old private async `ResolveImageUrl` copies, URI-path object-key extraction, `ObjectKeyOrUri` log templates, or raw object-key presign log templates under `Explore.Application/Features` and `Explore.Application/Services`.
- Full `Event.Architecture.Tests` was attempted and failed on unrelated support-access authorization parity drift: `ResourceKindConstants_ShouldHave_CerbosPolicy` and `ResourceKindConstants_ShouldHave_FallbackCase` both reported missing coverage for `islamuevent_support_access_session`.

Latest implemented admin status raw-markup removal slice on 2026-07-04:

- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/CustomPropertyGovernanceTests/*" --minimum-expected-tests 1` passed 13 tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/BlazorClientArchitectureTests/*" --minimum-expected-tests 1` passed 17 tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/AccessibilityConventionTests/*" --minimum-expected-tests 1` passed 8 tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/CodeHygieneTests/*" --minimum-expected-tests 1` passed 4 tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/AgentContextSchemaTests/*|/*/*/AgentContextIntentManifestTests/*|/*/*/AgentContextDuplicationTests/*" --minimum-expected-tests 1` passed 9 tests.
- `dotnet format whitespace --include Explore.Blazor.Client/Pages/Admin/CustomProperties/Components/ProjectionStatusSection.razor Explore.Blazor.Client/Pages/Admin/CustomProperties/Components/ExposureGovernanceSection.razor Explore.Blazor.Client.Tests/Pages/Admin/CustomPropertyGovernanceTests.cs --verbosity quiet` completed successfully.
- `rg -n 'MarkupString|AddMarkupContent|innerHTML|outerHTML|insertAdjacentHTML|document\\.write|setHTML\\(|eval\\(' Explore.Blazor Explore.Blazor.Client` reports only `Explore.Blazor.Client/Pages/Legal/CommunityGuidelines.razor`.
- LSP diagnostics are clean for `CustomPropertyGovernanceTests.cs`; Razor LSP is not installed and was previously declined, so Razor components are covered by the successful build/test execution.
- `git diff --check` is clean for the touched Blazor component, test, and workstream files.

Latest implemented browser action JS interop slice on 2026-07-04:

- `dotnet format whitespace --include Explore.Blazor.Client/Contracts/Interop/IBrowserActionInterop.cs Explore.Blazor.Client/Services/BrowserActionInterop.cs Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs Explore.Blazor.Client/Components/Events/EventPreviewWorkspace.razor.cs Explore.Blazor.Client/Pages/Landing/LandingPageForNonUsers.razor.cs Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs Explore.Blazor.Client/Pages/Events/EventList.razor.cs Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor Explore.Blazor.Client.Tests/Common/BlazorTestContext.cs Explore.Blazor.Client.Tests/Services/BrowserActionInteropTests.cs Explore.Blazor.Client.Tests/Security/BrowserInteropSafetyTests.cs --verbosity quiet` completed successfully.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/BrowserActionInteropTests/*" --minimum-expected-tests 1` passed 5 tests.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BrowserInteropSafetyTests/*" --minimum-expected-tests 1` passed 1 test.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventDetailTests/*|/*/*/EventListTests/*" --minimum-expected-tests 1` passed 19 tests.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventRegistrationTests/*" --minimum-expected-tests 1` passed 4 tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/BlazorClientArchitectureTests/*|/*/*/CodeHygieneTests/*|/*/*/AccessibilityConventionTests/*" --minimum-expected-tests 1` passed 17 tests.
- `rg -n 'Invoke(?:Void)?Async(?:<[^>]+>)?\\s*\\(\\s*"eval"|\\beval\\s*\\(|innerHTML|outerHTML|insertAdjacentHTML|document\\.write|setHTML\\(' Explore.Blazor.Client Explore.Blazor -g '*.cs' -g '*.razor' -g '*.js' -g '!bin/**' -g '!obj/**' -g '!node_modules/**'` returned no matches.
- `dotnet build --configuration Release --verbosity quiet` passed: 26 projects, 0 errors, existing package/dependency warnings only.

Latest implemented legacy download helper removal slice on 2026-07-04:

- `OrganizationSharedContacts` and `InstanceAuthProviderSection` now use `IBrowserActionInterop.DownloadBase64FileAsync` instead of the global `downloadFileFromBase64` helper.
- `/js/file-download.js` was removed because no app host referenced it after the migration.
- `BrowserInteropSafetyTests` now covers no eval, no DOM HTML-injection sinks, and no legacy global download helper.
- `dotnet format whitespace --include Explore.Blazor.Client/Pages/Organizations/OrganizationSharedContacts.razor.cs Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAuthProviderSection.razor Explore.Blazor.Client.Tests/Security/BrowserInteropSafetyTests.cs --verbosity quiet` completed successfully.
- Initial parallel `dotnet test` execution hit a Blazor build artifact file lock on `blazor.build.boot-extension.json`; rerunning the affected test sequentially from built artifacts passed.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BrowserInteropSafetyTests/*" --minimum-expected-tests 3` passed 3 tests.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BrowserActionInteropTests/*" --minimum-expected-tests 1` passed 5 tests.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/KeycloakRealmDoctorSourceTests/*" --minimum-expected-tests 1` passed 8 tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BlazorClientArchitectureTests/*|/*/*/CodeHygieneTests/*|/*/*/AccessibilityConventionTests/*" --minimum-expected-tests 1` passed 17 tests.
- `dotnet build --configuration Release --verbosity quiet` passed: 26 projects, 0 errors, existing package/dependency warnings only.

Latest implemented contact share export validation/sanitization slice on 2026-07-04:

- `dotnet format whitespace --include Explore.API/Models/QueryValidationRules.cs Explore.API/Models/PaginatedQueryRequests.cs Explore.API/Controllers/ContactShareConsentController.cs Explore.Application/Features/ContactShareConsents/Handlers/Commands/ExportSharedContactsCommandHandler.cs Event.API.IntegrationTests/Features/PublicQueryValidationTests.cs Event.Application.UnitTests/Features/ContactShareConsents/Commands/ExportSharedContactsCommandHandlerTests.cs Explore.Blazor.Client/Services/ContactShareConsentService.cs Explore.Blazor.Client.Tests/Services/ContactShareConsentServiceTests.cs --verbosity quiet` completed successfully.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity minimal --no-restore -maxcpucount:1` passed with existing warnings.
- `dotnet msbuild Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1 /v:minimal` regenerated `EventApiClient.g.cs` successfully.
- `rg -n "NormalizedFormat|normalizedFormat|ExportOrganizationSharedContactsAsync" Explore.Blazor.Client/Clients/EventApiClient.g.cs` confirms the generated export client has no normalized-format query parameter and still exposes the intended export method.
- `dotnet build --configuration Release --verbosity quiet` passed: 26 projects, 0 errors, existing warnings.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet` passed 1956 tests.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/PublicQueryValidationTests/*" --minimum-expected-tests 1` passed 22 tests.
- A full `Event.API.IntegrationTests` run was started before the focused filter was corrected and then cancelled after unrelated failures in custom-property projection, storage/HAL, and external API key governance tests; those failures predate this contact-share slice and were not in the touched surface.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet` passed 1481 tests with 1 documented skip.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet` passed 242 tests with 1 documented skip.

Common commands:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

OpenAPI/client generation when API contracts change:

```bash
dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity minimal --no-restore -maxcpucount:1
dotnet msbuild Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1 /v:minimal
```

---

## 8. Do Not Reintroduce These Tasks

- "Delegate API client regeneration to the user."
- "Add a global sanitizer middleware."
- "Register all FluentValidation validators in DI."
- "Move validation into controllers for convenience."
- "Use `MudForm` as the standard validation pattern."
- "Make Blazor role/claim checks decide edit/delete affordances."
- "Reuse Application validators directly in `Explore.Blazor.Client`."
- "Sanitize all strings before saving."
- "Display raw upload exception messages or log raw browser filenames."
- "Strip unknown characters from input without a product contract."
- "Require antiforgery on every unsafe BFF route without acknowledging documented exceptions and compensating controls."

---

## 9. Final Handoff Summary

The active workstream is now suitable for implementation. The plan no longer treats validation and sanitization as one generic activity. It separates:

- API/Application authoritative validation.
- BFF boundary validation and token/antiforgery controls.
- Blazor client UX validation and server-error mapping.
- Sanitization only for approved renderable content.

The next implementation agent should begin with a narrow high-risk slice that is still genuinely pending: residual storage response echo safety or newly discovered endpoint seams, then any remaining non-eval JS module/DOM input review, then Blazor form/server-error convergence after API field keys are stable. Earlier storage metadata, storage infrastructure health logging, current Blazor image upload and storage display surfaces, known rich-text/email seams, AI rail markup, admin custom-property `AddMarkupContent` seams, browser action `eval` interop, and legacy global download interop are already classified or implemented and should not be restarted without fresh source evidence.
