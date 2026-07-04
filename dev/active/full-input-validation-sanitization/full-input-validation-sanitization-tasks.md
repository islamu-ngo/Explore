<!-- ABOUTME: Corrected implementation checklist for input validation and sanitization hardening. -->
<!-- ABOUTME: Tracks API, BFF, Blazor, sanitization, generated-client, and verification tasks. -->

# Full Input Validation & Sanitization - Tasks

Last Updated: 2026-07-04 Europe/Brussels
Status: Re-baselined; implementation is partially complete and must continue by slice
Current priority: remaining storage display/log safety, residual non-eval JS interop/DOM input review, then Blazor form/server-error convergence

---

## Task Rules

- Keep this checklist aligned with the plan, context, matrix, and contract decisions.
- Do not mark a task complete unless code or documentation evidence has been verified in the repo.
- Prefer narrow slices with focused tests.
- Do not add broad cleanup tasks unrelated to input validation, sanitization, error safety, BFF boundaries, or Blazor form validation.
- Do not reintroduce prohibited patterns: global sanitizers, validator DI auto-registration, `MudForm` standardization, Blazor-only authority, role/claim affordance gating, or asking the user to regenerate clients.

---

## Phase 0 - Rebaseline And Research

Status: Complete for the 2026-07-04 planning update.

- [x] Read `AGENTS.md` and the repo Contribution Contract.
- [x] Read senior CTO feedback skill and required resources.
- [x] Read repo governance, API, Blazor, security, authorization, testing, and operation docs.
- [x] Read relevant `.claude/rules/*.md`.
- [x] Use Tavily MCP for current validation, sanitization, file-upload, logging, and idempotency guidance.
- [x] Use Context7 MCP for current ASP.NET Core, FluentValidation, and MudBlazor documentation.
- [x] Reclassify the workstream as a cross-cutting validation/sanitization hardening program.
- [x] Rewrite the implementation plan with source-grounded current state and corrected future phases.
- [x] Rewrite this task checklist to remove stale and unsafe tasks.
- [x] Rewrite context with current handoff guidance.
- [x] Update the input matrix rows for new findings from this rebaseline.
- [x] Add or update contract-decision notes for implementer-owned OpenAPI/client generation and raw-rendering classification.

---

## Phase 1 - API And Application Input Contract Hardening

Goal: Make server-side validation authoritative for request DTOs, queries, route IDs, headers, and command bodies.

### 1.1 Matrix-First Audit

- [ ] Audit API request models and Application DTOs against `full-input-validation-sanitization-input-matrix.md`.
- [ ] Add missing rows for route IDs, headers, idempotency keys, continuation URLs, setup secrets, storage metadata, and rich content.
- [ ] For each row, identify syntactic validation, semantic validation, canonicalization, output encoding/sanitization, and test owner.
- [ ] Mark intentional compatibility exceptions, including any request type that allows unknown JSON members.

### 1.2 Application Validator Coverage

- [ ] Identify commands/queries without manual FluentValidation coverage where syntactic validation belongs in Application.
- [ ] Add or update validators without DI auto-registration.
- [ ] Use `ValidateAsync` in handlers/tests.
- [ ] Keep validators side-effect free.
- [ ] Keep canonicalization outside validators unless the validator is only comparing normalized values.
- [ ] Add unit tests for required fields, ranges, lengths, enum allowlists, invalid identifiers, invalid dates, and cross-field rules.

### 1.3 Handler Semantic Guards

- [ ] Identify validation rules that require tenant context, repositories, authorization, state, clock, or persistence.
- [ ] Add handler/service guards before side effects.
- [ ] Add tests for tenant mismatch, not-found masking, duplicate state, invalid state transition, idempotency conflicts, and ownership violations.
- [ ] Ensure repositories return entities and remain tenant-aware.

### 1.4 API Transport Validation

- [ ] Verify unknown JSON member rejection remains enabled by default.
- [ ] Review `UpdateTenantPolicyRequest` and any other intentional unknown-member exceptions; document or correct them.
- [ ] Confirm malformed JSON, model-binding errors, and invalid query parameters use canonical problem details.
- [ ] Add integration tests for representative bad JSON, unknown JSON members, invalid queries, invalid route IDs, and error field keys.
- [x] Add storage upload finalize controller coverage for not-found, expired, size mismatch, content-type mismatch, and provider/write-result failure ProblemDetails mappings.
- [ ] Keep controllers thin.

---

## Phase 2 - Storage, Upload, Metadata, And Object-Key Hardening

Goal: Complete the highest-risk remaining validation area.

### 2026-07-03 Completed Storage Slice

- [x] Harden `CreateStorageUploadSessionDtoValidator` for upload-session reservation metadata: malformed or wildcard MIME hints, control-character values, path separators, dot segments, reserved Windows device names, and unsafe extension tokens.
- [x] Normalize upload-session `ContentType` before storage policy resolution and persistence so route selection and stored metadata use the same canonical value.
- [x] Add validator regression coverage in `CreateStorageUploadSessionDtoValidatorTests`.
- [x] Add handler regression coverage in `StorageUploadSessionCommandHandlerTests` for content-type normalization before policy resolution.

### 2026-07-03 Implemented Storage Metadata Slice

- [x] Add shared storage metadata predicates for relative object keys, simple filenames, reserved Windows names, simple extension tokens, MIME hints, and SHA-256 hex digests.
- [x] Harden `UploadRequestDtoValidator` against reserved filenames and malformed multi-segment MIME hints.
- [x] Harden `CreateStorageObjectDtoValidator` and `UpdateStorageObjectDtoValidator` for object-key traversal, unsafe names/display names, unsafe extensions, malformed content types, non-hex checksums, and incomplete owning-resource metadata.
- [x] Add `StorageObjectMetadataDtoValidatorTests` source for create/update metadata regressions.
- [x] Execute the new storage metadata validator tests: focused `Event.Application.UnitTests` passed 37 tests for `StorageObjectMetadataDtoValidatorTests` and `UploadRequestDtoValidatorTests`.
- [x] Document that the current storage DTO/domain model has no arbitrary metadata dictionary, so metadata key/value/count validation is not a current code seam.

### 2026-07-03 Implemented Upload Session Ownership Slice

- [x] Inject `ICurrentUserService` into `CancelStorageUploadSessionCommandHandler`.
- [x] Inject `ICurrentUserService` into `FinalizeStorageUploadSessionCommandHandler`.
- [x] Mask wrong-user upload sessions as `StorageUploadSessionNotFound`, matching wrong-tenant behavior.
- [x] Enforce finalize ownership before usage-counter access, provider write, storage-object creation, quota commit, and failure-state mutation.
- [x] Enforce cancel ownership before usage-counter access, quota release, and session-state mutation.
- [x] Add focused `StorageUploadSessionCommandHandlerTests` for wrong-user finalize and cancel attempts.
- [x] Verify focused `StorageUploadSessionCommandHandlerTests` pass 18 tests.

### 2026-07-04 Implemented BFF Storage Boundary Slice

- [x] Reject reserved Windows device filenames in `/bff/storage/upload-session` before forwarding the request to the API.
- [x] Reject reserved Windows device filenames in `/bff/storage/upload-proxy` before API finalization.
- [x] Reject malformed multi-segment MIME hints such as `application/pdf/extra` at the BFF boundary.
- [x] Remove raw API upload-session IDs from BFF storage proxy failure logs.
- [x] Add `BffStorageUploadProxyTests` coverage proving invalid BFF filename/MIME input fails locally without API calls or finalize side effects.

### 2026-07-04 Implemented BFF Upload Session Expiry Slice

- [x] Reject already-expired API upload-session reservation responses in `StorageUploadSessionStore.IssueAsync`.
- [x] Ensure expired cached BFF upload sessions are consumed and replay as `session_not_found`.
- [x] Add BFF endpoint coverage proving unknown opaque session IDs do not reach the downstream API finalize endpoint.
- [x] Add BFF endpoint coverage proving expired cached sessions return safe `400` ProblemDetails and do not reach the downstream API finalize endpoint.
- [x] Add BFF endpoint coverage proving expired API reservation responses return safe `502` ProblemDetails without echoing raw API upload-session IDs.

### 2026-07-04 Implemented Storage Presigned Download Slice

- [x] Sign by-ID presigned downloads with the persisted `StorageObject.ObjectKey`, not an object key parsed from `StorageObject.Uri`.
- [x] Enforce active lifecycle, visibility, and current-user ownership semantics before signing: public image, authenticated tenant, and private owner paths are handled explicitly.
- [x] Reject invalid presigned-download expiration values outside 1 through 60 minutes before metadata lookup.
- [x] Fail closed when storage metadata is missing, inaccessible, inactive, or missing a provider object key.
- [x] Suppress provider object-key response echo while preserving the existing `PresignedDownloadUrlResponseDto` shape.
- [x] Map inaccessible or invalid presigned-download handler results to the existing 404 ProblemDetails response in `StorageObjectController`.
- [x] Add focused `GetPresignedDownloadUrlRequestHandlerTests` and `StorageUploadSessionControllerTests` coverage for key signing, ownership gating, expiration bounds, missing keys, and null-to-404 behavior.

### 2026-07-04 Implemented Storage Provider Result Validation Slice

- [x] Validate provider write results before `StorageObject` metadata creation, quota commit, and upload-session finalization.
- [x] Require provider write results to match the reserved provider and current tenant object-key namespace.
- [x] Require provider write results to match the reserved byte count and content type.
- [x] Require provider write results to include a valid SHA-256 hex checksum.
- [x] Fail closed as `StorageUploadWriteFailed` when provider metadata is invalid, release quota reservation, mark the session failed, and avoid storage metadata persistence.
- [x] Add `StorageUploadSessionCommandHandlerTests` coverage for invalid provider metadata with no `StorageObject` creation and no quota commit.

### 2026-07-04 Implemented Storage API Failure Mapping Coverage Slice

- [x] Drive storage upload finalize failures through `StorageObjectController.UploadSessionContent` instead of only handler-level tests.
- [x] Verify `StorageUploadSessionNotFound` maps to 404 ProblemDetails.
- [x] Verify `StorageUploadSessionExpired` maps to 409 ProblemDetails.
- [x] Verify `StorageUploadSizeMismatch` and `StorageUploadContentTypeMismatch` map to canonical 400 validation ProblemDetails.
- [x] Verify `StorageUploadWriteFailed` maps to 503 ProblemDetails for provider/write-result failure.

### 2026-07-04 Implemented Storage Content Signature Slice

- [x] Add `StorageContentSignaturePolicy` in the Application layer so byte-level upload checks stay outside controllers and infrastructure providers.
- [x] Treat `Content-Type` as reserved metadata/hint only; known uploaded image/document types must match server-side byte signatures before provider writes.
- [x] Validate known content extensions against MIME-specific allowlists at finalize time.
- [x] Fail unsupported `image/*` uploads closed until a signature/rendering policy is explicitly added.
- [x] Preserve non-seekable request-body streams by replaying the inspected prefix to the storage provider.
- [x] Fail spoofed bytes or extension mismatches as `StorageUploadContentSignatureMismatch`, release quota reservation, mark the session failed, and avoid `StorageObject` creation.
- [x] Map `StorageUploadContentSignatureMismatch` to canonical `400` storage validation ProblemDetails.
- [x] Add `StorageUploadSessionCommandHandlerTests` coverage for spoofed bytes, extension mismatch, and non-seekable stream prefix replay.
- [x] Add and execute `StorageUploadSessionControllerTests` coverage for the new API failure-code mapping.

### 2026-07-04 Implemented Storage Runtime Route Coverage Slice

- [x] Add runtime `StorageObjectControllerTests` coverage proving upload-session create, finalize, and cancel endpoints return `401` without authentication.
- [x] Add runtime `StorageObjectControllerTests` coverage proving malformed finalize/cancel upload-session route IDs are rejected as `404` by the `{uploadSessionId:guid}` route constraint.
- [x] Add runtime `StorageObjectControllerTests` coverage proving authenticated storage object route/body ID mismatch returns safe `400` validation ProblemDetails without echoing the body ID.

### 2026-07-04 Implemented Storage Projection URL Helper Slice

- [x] Add `StoragePresentationUrlResolver` as the shared Application helper for projection image URL resolution.
- [x] Sign only validated relative object keys through `IObjectStorageService`.
- [x] Pass absolute HTTP(S) URLs through without converting their path into a provider object key.
- [x] Allow only local `/api/storageobject/...` paths for relative browser paths.
- [x] Reject unsafe relative references such as traversal paths without provider calls.
- [x] Log resolver warnings/errors with bounded image context labels, not raw object keys or URIs.
- [x] Migrate `EventDetailsProjectionService`, tag/category event projections, and group projections to the shared resolver.
- [x] Add `StoragePresentationUrlResolverTests` for safe signing, external URL pass-through, local API path pass-through, unsafe relative rejection, and provider failure behavior.
- [x] Finish migration for actor, organization, event list, managed-events, my-events, and user projection helpers still signing URI paths or logging raw references.

### 2026-07-04 Implemented Storage Log Failure-Bucketing Slice

- [x] Remove raw exception-object logging from `StoragePresentationUrlResolver` presigned URL signing failures.
- [x] Remove raw exception-object logging from `GetPresignedDownloadUrlRequestHandler` by-ID presigned-download provider signing failures.
- [x] Remove raw exception-object logging from `StorageObjectContentReader` provider-not-found and provider-unavailable paths.
- [x] Remove raw exception-object logging from the BFF upload proxy catch path.
- [x] Replace those logs with bounded failure-type fields plus already-approved storage object ID/provider/context labels.
- [x] Verify the touched storage paths no longer contain `LogError(ex, ...)` or `LogWarning(ex, ...)`.

### 2026-07-04 Implemented Storage Cross-Tenant Runtime Masking Slice

- [x] Seed a real secondary-tenant `StorageObject` through `TenantScenarioSeed.SeedSecondaryTenantWithUserAsync`.
- [x] Request the secondary-tenant storage object from the default tenant through authenticated metadata detail, content, and presigned-download routes.
- [x] Prove all three routes return the canonical safe `404 Storage object not found` ProblemDetails response.
- [x] Prove those responses do not echo the secondary tenant ID, provider name, object key prefix, file name, or authenticated-tenant visibility value.
- [x] Confirm the current storage runtime lookup path already fails closed for this cross-tenant read scenario; no production code patch was required in this slice.

### 2026-07-04 Implemented Storage Upload-Session Tenant/Replay/State Guard Coverage Slice

- [x] Add Application handler coverage proving same-user wrong-tenant finalize attempts return `StorageUploadSessionNotFound` before usage-counter lookup, provider writes, storage metadata creation, or session updates.
- [x] Add Application handler coverage proving same-user wrong-tenant cancel attempts return `StorageUploadSessionNotFound` before quota lookup, quota release, or session updates.
- [x] Add Application handler coverage proving replayed finalized upload sessions return idempotent success without provider replay, storage metadata creation, quota finalization, or session mutation.
- [x] Add Application handler coverage proving canceled upload sessions cannot be finalized and do not reach provider writes, storage metadata creation, quota updates, or session mutation.
- [x] Confirm no production code patch was required in this slice; the inspected handlers already failed closed and the work was to lock the contract with tests.

### 2026-07-04 Implemented Storage API Upload-Session Semantic Runtime Coverage Slice

- [x] Drive missing upload-session finalize and cancel requests through the real authenticated API host and verify both return safe `404 Storage upload session not found` ProblemDetails.
- [x] Drive canceled upload-session finalization through the real authenticated API host and verify it returns safe `409 Storage upload session conflict` ProblemDetails with the `storage_upload_session_invalid_state` code.
- [x] Drive finalized upload-session cancellation through the real authenticated API host and verify it returns safe `409 Storage upload session conflict` ProblemDetails with the `storage_upload_session_finalized` code.
- [x] Assert upload-session semantic failure responses do not echo tenant IDs, provider names, storage object keys, raw file names, checksums, or private visibility metadata.

### 2.1 Upload Request Semantics

- [ ] Review `UploadRequestDtoValidator` against OWASP file-upload guidance and current product limits.
- [ ] Validate filename display values for length, control characters, path separators, reserved names, and unsafe normalization cases.
- [x] Validate content type as a hint only for finalized uploaded bytes; do not trust it as proof of file content.
- [x] Validate extension allowlist for known finalized image/document upload content types.
- [ ] Validate declared size/count before expensive operations.
- [x] Validate provider-reported byte count, content type, SHA-256 digest, and object-key namespace after provider write before metadata persistence.
- [x] Confirm the current storage DTO/domain model has no arbitrary metadata dictionary, so metadata key/value/count validation is not a current code seam.
- [x] Add tests for spoofed finalized bytes and invalid known-content extension.
- [ ] Add remaining tests for invalid filename, oversized file metadata fields, invalid size, and malformed metadata at the endpoint/runtime surfaces not already covered by validator tests.

### 2.2 Storage Object And Session Ownership

- [ ] Ensure object keys/storage IDs are server-generated or strictly validated.
- [ ] Validate upload-session IDs before presign, complete, download, and proxy operations. API finalize/cancel route-shape and missing/canceled/finalized semantic runtime coverage is complete; remaining work must be tied to newly discovered endpoint or BFF seams in the matrix.
- [ ] Verify upload sessions are bound to tenant, user, and intended operation where required beyond the completed finalize/cancel Application handler checks.
- [ ] Add tests for tenant mismatch, replayed session, expired session, and mismatched object ID across remaining API/BFF storage endpoints not already covered by Application, API runtime, or BFF opaque-session tests.
- [x] Add BFF upload-proxy tests for unknown opaque sessions, expired cached sessions, and expired API reservation responses with no downstream finalization.
- [x] Add API runtime test for storage object route/body ID mismatch with safe validation ProblemDetails.
- [x] Add Application unit tests for wrong-user finalize/cancel masking and no side effects.
- [x] Add Application unit tests for wrong-tenant finalize/cancel masking, finalized replay idempotency, and canceled finalize invalid-state no-side-effect behavior.
- [x] Add API runtime tests for missing upload-session finalize/cancel, canceled finalize invalid-state, and finalized cancel conflict with safe ProblemDetails and no storage metadata echo.
- [x] Preserve non-seekable finalize upload streams when byte-signature inspection consumes a prefix before provider writes.
- [x] Validate presigned-download storage object IDs through metadata lookup, lifecycle/visibility checks, and current-user owner checks before signing by-ID downloads.
- [x] Validate finalized provider object keys are safe relative keys under `tenants/{tenantId:N}/` before storage metadata becomes visible.
- [x] Add runtime API tests for upload-session create/finalize/cancel authentication and malformed finalize/cancel route IDs.
- [x] Add runtime API tests proving private-owner storage content and presigned read responses are safe 404s for different authenticated users without owner/object-key/provider metadata echo.
- [x] Confirm storage detail/content/presigned failure responses do not reveal cross-tenant existence.

### 2.3 Storage Logging And Display Safety

- [ ] Audit logs for raw filenames, object keys, metadata values, and storage provider messages.
- [ ] Redact or encode sensitive values in structured logs.
- [x] Remove raw API upload-session IDs from BFF storage proxy failure logs.
- [x] Remove raw URI/object-key extraction and response echo from the by-ID presigned-download handler.
- [x] Fail provider-result mismatches without echoing provider-returned object keys, content types, or checksums in user-facing errors.
- [x] Return storage content/download responses with persisted validated content types instead of provider-returned MIME hints.
- [x] Add runtime response-redaction coverage for private-owner storage read masking.
- [x] Centralize projection image URL signing for event detail, tag/category event projections, and group projections through `StoragePresentationUrlResolver`.
- [x] Replace remaining actor, organization, event-list, managed-events, my-events, and user projection resolver copies with `StoragePresentationUrlResolver`.
- [x] Bucket storage provider/proxy exception logs without raw exception objects in storage presentation URL signing, content read, and BFF upload proxy paths.
- [ ] Ensure Blazor displays filenames/metadata through normal encoded text, not raw markup.
- [ ] Add regression tests where existing helpers can assert no sensitive response echo.

---

## Phase 3 - Tenant-Aware Semantic Validation And Persistence Backstops

Goal: Prove invalid or cross-tenant data cannot bypass UI/API validation into persistence.

- [ ] List write handlers that accept client-supplied aggregate IDs or tenant-bound child IDs.
- [ ] For each high-risk handler, add tenant-mismatch tests.
- [ ] Verify repository queries include tenant predicates where tenant isolation applies.
- [ ] Add persistence/integration tests for race-sensitive uniqueness constraints.
- [ ] Verify handlers do not map untrusted DTOs into persisted entities before validation/canonicalization.
- [ ] Confirm validation failures happen before external side effects.

---

## Phase 4 - BFF Boundary Residual Audit

Goal: Validate BFF-only seams without duplicating Application command validation.

### 4.1 BFF Route Matrix

- [ ] Inventory Blazor Server/BFF endpoints, setup routes, preference routes, auth diagnostics, YARP proxy paths, upload proxy paths, and internal endpoints.
- [ ] Classify each unsafe route as antiforgery-protected or documented exception with compensating controls.
- [ ] Add missing matrix rows for BFF route inputs, headers, cookies, and continuation paths.

### 4.2 Token And Proxy Safety

- [ ] Verify bearer tokens never reach `Explore.Blazor.Client`, browser-visible markup, logs, or responses.
- [ ] Validate proxy target paths and prevent path traversal or route confusion.
- [ ] Validate required upload/session headers and reject spoofed or missing values.
- [x] Validate BFF upload-session/proxy filename and MIME hints for reserved device names and malformed multi-segment MIME values.
- [x] Verify invalid BFF storage upload-session/proxy input is rejected before downstream API calls/finalization.
- [x] Verify unknown or expired BFF storage upload sessions are rejected before downstream API finalization.
- [ ] Add or update `Explore.Blazor.IntegrationTests` for token isolation, proxy path validation, antiforgery/exception behavior, and safe error responses.

### 4.3 BFF Error Mapping

- [ ] Ensure BFF validation failures return canonical problem details or safe Blazor errors.
- [ ] Ensure setup/auth diagnostic failures do not leak setup secrets, tokens, cookies, or internal provider errors.

---

## Phase 5 - Blazor Form Validation Convergence

Goal: Make Blazor forms consistent, accessible, and aligned with API problem-details keys.

### 5.1 Form Pattern Convergence

- [ ] Inventory forms that still do not use `EditForm` + `EditContext`.
- [ ] Convert selected forms to existing primitives: `FormSubmissionGuard`, `FormSubmitState`, `ServerValidationErrorStore`, and `AppValidationSummary`.
- [ ] Keep client rules limited to immediate UX checks such as required values, ranges, basic formats, and duplicate-submit protection.
- [ ] Do not introduce `MudForm`.
- [ ] Do not call Application validators from `Explore.Blazor.Client`.

### 5.2 Server Error Mapping

- [ ] Verify API/BFF problem-details field keys match form field names or documented mapping.
- [ ] Map server errors through `ServerValidationErrorStore`.
- [ ] Clear server errors on field edit according to existing component behavior.
- [ ] Add component tests for API validation errors, non-field errors, repeated submissions, and successful clear/reset.

### 5.3 Accessibility And HAL Affordances

- [ ] Verify validation summaries are reachable and announced by existing accessibility patterns.
- [ ] Verify error focus behavior.
- [ ] Verify actions remain gated by HAL `_links`, not local roles/claims.
- [ ] Add component tests for link-present and link-absent action states where forms expose edit/delete/submit affordances.

### 5.4 Blazor Upload UX And Error Safety

- [x] Inventory Blazor file upload controls and handlers: `ImageUpload.razor`, event create/edit image upload handlers, organization logo upload, `ImageFileReaderService`, `ImageUploadClient`, `ImageStorageService`, and `ImageStorageRecordClient`.
- [x] Centralize or align browser-side upload UX rules for allowed image types, max size, upload-in-progress state, preview state, and accessible error display through `ImageUploadClientPolicy`.
- [x] Keep browser `IBrowserFile.ContentType`, `Name`, and `Size` checks as UX-only hints; BFF/API/Application validation remains authoritative.
- [x] Replace raw `Exception.Message` upload errors with safe generic text or allowlisted upload service messages.
- [x] Remove raw `IBrowserFile.Name`, raw upload URLs, raw BFF/provider response bodies, raw ProblemDetails text, and raw exception messages from Blazor upload-path logs; use failure types, size buckets, content-type buckets, status codes, and booleans.
- [x] Add component/service tests with dangerous filenames, multipart filename sanitization, upload service failures, metadata failure mapping, and safe error/log expectations.

---

## Phase 6 - Sanitization And Raw Rendering Review

Goal: Remove unnecessary raw rendering and define sanitizer ownership only where rich content is intentionally supported.

### 6.1 Raw Rendering Inventory

- [x] Search for current `MarkupString`, raw HTML rendering, Markdown-to-HTML, email HTML body usage, `RenderFragment` composition from user input, and direct JS eval/DOM injection.
- [ ] Add each seam to the input matrix.
- [ ] Classify each seam as controlled markup, encoded text, sanitized rich content, or remove.
- [x] Remove all currently discovered `AddMarkupContent` usage under `Explore.Blazor` and `Explore.Blazor.Client`.
- [x] Re-run the Blazor raw-rendering scan and confirm the only remaining raw-markup match is the classified `CommunityGuidelines.razor` `MarkupString` seam.
- [x] Remove all discovered Blazor `eval` JS interop calls under `Explore.Blazor` and `Explore.Blazor.Client`.

### 6.2 High-Priority Component Review

- [x] Review `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor` prompt-reference highlight markup.
- [x] Remove the `MarkupString` builder and render prompt-reference highlights through component composition.
- [x] Prove dynamic prompt/reference display names flow through Blazor renderer encoding instead of raw HTML concatenation.
- [x] Add `Explore.Blazor.Client.Tests` coverage with dangerous reference display names containing event-handler and script payloads.

### 6.3 Known Rich-Content Seams

- [x] Re-check `CommunityGuidelines.razor` and document why it is controlled or encoded.
- [x] Add a decision row for `PublicExperienceHomeBlockKind.RichText`.
- [x] Add a decision row for `EmailMessage.HtmlBody`.
- [x] Decide whether each known rich-content seam is allowed, removed, stored as plain text, rendered only in trusted contexts, or sanitized.
- [x] Prove public home `RichText` block content renders as encoded text, not DOM, with malicious bUnit coverage.
- [x] Prove lifecycle/organizer email HTML body interpolation is context-encoded before dispatch outbox persistence.
- [ ] If sanitizer support is required, choose the owning layer and add allowlist tests for tags, attributes, URI schemes, event handlers, scripts, styles, malformed HTML, and dangerous URLs.

### 2026-07-04 Implemented Admin Status Raw Markup Removal Slice

- [x] Replace `ProjectionStatusSection.razor` status-row `AddMarkupContent` calls with `RenderTreeBuilder.OpenElement`, `AddAttribute`, and `AddContent`.
- [x] Let Blazor renderer encoding handle dynamic projection status values, especially `ProjectionStatusModel.LastErrorMessage`, instead of manually encoding and then injecting raw markup.
- [x] Replace the static `ExposureGovernanceSection.razor` flag-header `AddMarkupContent` span with component-renderer element/content calls.
- [x] Add `ProjectionStatus_EncodesDangerousLastErrorText` to prove malicious `<img onerror>` and `<script>` status text remains encoded and creates no DOM nodes.
- [x] Update the input matrix and contract decision for the projection-status seam.

### 2026-07-04 Implemented Browser Action JS Interop Slice

- [x] Add `IBrowserActionInterop` and `BrowserActionInterop` as the typed Blazor boundary for browser share, clipboard, smooth-scroll, and base64 file download actions.
- [x] Add `/js/browser-actions.js` as an ES module loaded through JS isolation instead of global `eval` calls.
- [x] Replace event preview, event list, event detail, event registration, and anonymous landing page direct `eval`, `navigator.share`, and `navigator.clipboard` component calls with the typed interop service.
- [x] Move ICS download anchor creation out of string-built `eval` and into a JS module function that creates a `Blob`, assigns a sanitized download filename, and revokes the object URL.
- [x] Keep Blazor component fallbacks safe: share falls back to clipboard, clipboard/download failures show generic UI messages, and service logs do not include raw titles, URLs, filenames, or base64 content.
- [x] Add `BrowserActionInteropTests` for structured module arguments, fail-closed JS unavailability, and blank required values.
- [x] Add `BrowserInteropSafetyTests` so Blazor source fails if `InvokeAsync("eval")`, `InvokeVoidAsync("eval")`, or direct `eval(...)` is reintroduced.
- [x] Record Tavily MCP limitation for this slice: the OWASP eval/DOM-sink query returned usage-limit status `432`; Context7 ASP.NET Core Blazor JS interop docs were used for the JS module/import pattern.

### 2026-07-04 Implemented Legacy Download Helper Removal Slice

- [x] Migrate shared-contact CSV/TSV export downloads from the legacy global `downloadFileFromBase64` function to `IBrowserActionInterop.DownloadBase64FileAsync`.
- [x] Migrate instance authorization policy package downloads from the legacy global helper to `IBrowserActionInterop.DownloadBase64FileAsync`.
- [x] Remove the unused `/js/file-download.js` global helper.
- [x] Keep download success/failure user messages generic so raw server-provided filenames are not echoed in snackbar/status text.
- [x] Extend `BrowserInteropSafetyTests` to block DOM HTML-injection sinks and the legacy `downloadFileFromBase64` identifier from returning.

---

## Phase 7 - Observability, Logging, And Error Safety

Goal: Keep validation telemetry useful without leaking sensitive values.

- [ ] Audit validation failure logs for raw request bodies, tokens, cookies, setup secrets, object keys, filenames, rich text, email bodies, and provider payloads.
- [ ] Replace unsafe values with stable identifiers, lengths, hashes, or redacted placeholders where useful.
- [ ] Ensure user-facing validation errors do not echo secrets, raw rich content, raw upload filenames, provider details, or raw exception messages.
- [ ] Keep metrics low-cardinality and free of PII.
- [ ] Add regression tests or focused code review notes for high-risk failure paths.

---

## Phase 8 - OpenAPI, Generated Clients, And Docs

Goal: Keep contracts and client code aligned with implementation.

- [ ] When API contracts change, run:

```bash
dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity minimal --no-restore -maxcpucount:1
dotnet msbuild Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1 /v:minimal
```

- [ ] Review generated OpenAPI/API client diffs.
- [ ] Verify generated clients compile.
- [ ] Update `docs/API.md` for durable API contract changes.
- [ ] Update `docs/BLAZOR.md` for durable BFF/form workflow changes.
- [ ] Update `docs/SECURITY-MODEL.md` for sanitizer, upload, token, antiforgery, or logging security changes.
- [ ] Update `docs/TESTING.md` only if required verification commands change.
- [ ] Update this workstream's context, matrix, and decision log.

Important correction: do not add a task that tells the user to regenerate the API client. The implementation agent owns generation and verification when contracts change.

---

## Phase 9 - Verification Gate

Goal: Verify each slice through the relevant project tests and final build.

### Per-Slice Minimums

- [ ] Application validator/handler changes: run `Event.Application.UnitTests`.
- [ ] API contract/middleware/controller changes: run relevant `Event.API.IntegrationTests`.
- [ ] Persistence/tenant uniqueness changes: run relevant `Event.Persistence.IntegrationTests`.
- [ ] BFF changes: run `Explore.Blazor.IntegrationTests`.
- [ ] Blazor client form/rendering changes: run `Explore.Blazor.Client.Tests`.
- [ ] Context/rule/skill/docs-structure changes: run relevant `Event.Architecture.Tests` where practical.
- [ ] Broad completion claim: run `dotnet build --configuration Release --verbosity quiet`.

### Commands

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Do not run solution-level `dotnet test`.

### 2026-07-03 Verification Evidence

- Focused upload-session validator tests passed: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/CreateStorageUploadSessionDtoValidatorTests/*" --minimum-expected-tests 1` ran 26 tests.
- Focused handler regression passed: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/StorageUploadSessionCommandHandlerTests/CreateHandle_NormalizesContentTypeBeforePolicyResolutionAndPersistence" --minimum-expected-tests 1` ran 1 test.
- Full Application unit suite passed at that earlier checkpoint: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet` ran 1,791 tests. Current full-suite status is listed in the upload-session ownership evidence below.
- Full Release build passed: `dotnet build --configuration Release --verbosity quiet` built 25 projects with existing warnings.
- LSP diagnostics were clean for the changed Application and test files.
- `git diff --check` was clean for the changed Application and test files.
- Architecture tests were rerun after unrelated `dev/active/ai-context-disclosure-policy/*` files appeared in the worktree and now pass in the current worktree.

### 2026-07-03 Storage Metadata Verification Evidence

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed for the storage metadata validator changes.
- LSP diagnostics were clean for `StorageObjectMetadataValidation.cs`, `UploadRequestDtoValidator.cs`, `CreateStorageObjectDtoValidator.cs`, `UpdateStorageObjectDtoValidator.cs`, `StorageObjectMetadataDtoValidatorTests.cs`, and `UploadRequestDtoValidatorTests.cs`.
- `git diff --check` was clean for the changed storage metadata validator files and tests.
- Focused `Event.Application.UnitTests` storage metadata execution passed 37 tests with `--treenode-filter "/*/*/StorageObjectMetadataDtoValidatorTests/*|/*/*/UploadRequestDtoValidatorTests/*"`.

### 2026-07-03 Upload Session Ownership Verification Evidence

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed for the handler constructor and ownership guard changes.
- Focused `Event.Application.UnitTests` upload-session handler execution passed 18 tests with `--treenode-filter "/*/*/StorageUploadSessionCommandHandlerTests/*"`.
- LSP diagnostics were clean for `CancelStorageUploadSessionCommandHandler.cs`, `FinalizeStorageUploadSessionCommandHandler.cs`, and `StorageUploadSessionCommandHandlerTests.cs`.
- `git diff --check` was clean for the changed storage handler, validator, test, and workstream files.
- Full `Event.Application.UnitTests` execution currently fails one unrelated dirty settings test: `SettingHandlerTests.Batch_CerbosEndpoints_NormalizesBareHostsBeforePersisting` throws NSubstitute `AmbiguousArgumentsException` for `SetValueAsync(String, String, SettingScope, Guid, Guid, CancellationToken)`.
- `Event.Architecture.Tests` passed with `--no-build`: 240 total, 239 succeeded, 1 documented skip.

### 2026-07-04 BFF Storage Boundary Verification Evidence

- `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet` passed for the BFF endpoint changes, with existing package vulnerability/deprecation warnings.
- Focused `Explore.Blazor.IntegrationTests` BFF storage execution passed 10 tests with `--treenode-filter "/*/*/BffStorageUploadProxyTests/*"`.
- LSP diagnostics were clean for `Explore.Blazor/Extensions/BffStorageEndpoints.cs` and `Explore.Blazor.IntegrationTests/Endpoints/BffStorageUploadProxyTests.cs`.
- `git diff --check` was clean for the changed BFF endpoint and BFF integration test files.

### 2026-07-04 BFF Upload Session Expiry Verification Evidence

- `dotnet build --configuration Release --verbosity quiet` passed: 25 projects, 0 errors, existing package/deprecation warnings only.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet` passed: 183 succeeded.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet` passed: 183 succeeded.
- `dotnet format whitespace --include Explore.Blazor/Services/StorageUploadSessionStore.cs Explore.Blazor.IntegrationTests/Services/StorageUploadSessionStoreTests.cs Explore.Blazor.IntegrationTests/Endpoints/BffStorageUploadProxyTests.cs --verbosity quiet` completed successfully.
- `git diff --check` was clean for the changed BFF session store and BFF integration test files.

### 2026-07-04 Storage Object ID Mismatch Runtime Verification Evidence

- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/StorageObjectControllerTests/*" --minimum-expected-tests 1` passed 18 tests.
- `dotnet format whitespace --include Event.API.IntegrationTests/Features/StorageObjectControllerTests.cs --verbosity quiet` completed successfully.

### 2026-07-04 Storage Private-Owner Runtime Masking Verification Evidence

- `dotnet format whitespace --include Event.API.IntegrationTests/Features/StorageObjectControllerTests.cs --verbosity quiet` completed successfully.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/StorageObjectControllerTests/StorageReadEndpoints_WithPrivateOwnerObjectForDifferentUser_ShouldReturnSafeNotFound" --minimum-expected-tests 1` passed 1 test after rebuilding the focused test project.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/StorageObjectControllerTests/*" --minimum-expected-tests 1` passed the full focused storage object runtime lane: 19 tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/CleanArchitectureTests/*|/*/*/CodeHygieneTests/*|/*/*/NamingConventionTests/*" --minimum-expected-tests 1` passed 13 tests.

### 2026-07-04 Storage Cross-Tenant Runtime Masking Verification Evidence

- `dotnet format whitespace --include Event.API.IntegrationTests/Features/StorageObjectControllerTests.cs --verbosity quiet` completed successfully.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/StorageObjectControllerTests/StorageReadEndpoints_WithCrossTenantObject_ShouldReturnSafeNotFound" --minimum-expected-tests 1` passed 1 test after rebuilding the focused test project, with existing package/analyzer warnings only.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/StorageObjectControllerTests/*" --minimum-expected-tests 1` passed the full focused storage object runtime lane: 20 tests.
- LSP diagnostics found no issues in `Event.API.IntegrationTests/Features/StorageObjectControllerTests.cs`.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/CleanArchitectureTests/*|/*/*/CodeHygieneTests/*|/*/*/NamingConventionTests/*" --minimum-expected-tests 1` passed 13 tests.
- `git diff --check` completed successfully.

### 2026-07-04 Storage Upload-Session Tenant/Replay/State Guard Verification Evidence

- `dotnet format whitespace --include Event.Application.UnitTests/Features/StorageObjects/Commands/StorageUploadSessionCommandHandlerTests.cs --verbosity quiet` completed successfully.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/StorageUploadSessionCommandHandlerTests/*" --minimum-expected-tests 1` passed 26 tests, including wrong-tenant finalize/cancel masking, finalized replay, and canceled finalize invalid-state coverage. Existing `AutoMapper` NU1903 audit warnings were still emitted before test execution.
- LSP diagnostics found no issues in `Event.Application.UnitTests/Features/StorageObjects/Commands/StorageUploadSessionCommandHandlerTests.cs`.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/CleanArchitectureTests/*" --minimum-expected-tests 1` passed 13 tests with existing NuGet audit/package warnings.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/CodeHygieneTests/*" --minimum-expected-tests 1` passed 4 tests with existing NuGet audit/package warnings.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/NamingConventionTests/*" --minimum-expected-tests 1` passed 10 tests with existing NuGet audit/package warnings.
- `dotnet build --configuration Release --verbosity quiet` passed for 25 projects with 0 errors and 1,666 existing warnings, including NuGet audit advisories and package/deprecation warnings.
- `git diff --check` completed successfully.

### 2026-07-04 Storage Log Failure-Bucketing Verification Evidence

- `dotnet build --configuration Release --verbosity quiet` passed: 25 projects, 0 errors, existing package/deprecation warnings only.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/StoragePresentationUrlResolverTests/*|/*/*/StorageObjectContentReaderTests/*" --minimum-expected-tests 1` passed 5 tests for the resolver filter.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/StorageObjectContentReaderTests/*" --minimum-expected-tests 1` passed 3 tests.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffStorageUploadProxyTests/*" --minimum-expected-tests 1` passed 13 tests.
- Targeted `rg` scan found no `LogError(ex, ...)` / `LogWarning(ex, ...)` calls in `BffStorageEndpoints.cs`, `StoragePresentationUrlResolver.cs`, or `StorageObjectContentReader.cs`.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/GetPresignedDownloadUrlRequestHandlerTests/*" --minimum-expected-tests 1` passed 6 tests, including provider signing failure log bucketing without raw exception payloads.
- Focused `Event.Architecture.Tests` clean architecture/code hygiene/naming execution passed 13 tests with `--treenode-filter "/*/*/CleanArchitectureTests/*|/*/*/CodeHygieneTests/*|/*/*/NamingConventionTests/*"`.
- Targeted `rg` scan found no `LogError(ex, ...)` / `LogWarning(ex, ...)` calls in `GetPresignedDownloadUrlRequestHandler.cs`, `BffStorageEndpoints.cs`, `StoragePresentationUrlResolver.cs`, or `StorageObjectContentReader.cs`.

### 2026-07-04 Storage Content Response Metadata Authority Verification Evidence

- `dotnet format whitespace --include Explore.Application/Services/StorageObjectContentReader.cs Event.Application.UnitTests/Features/StorageObjects/Queries/StorageObjectContentReaderTests.cs --verbosity quiet` completed successfully.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/StorageObjectContentReaderTests/*" --minimum-expected-tests 1` passed 4 tests, including provider/content-type mismatch coverage.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/CleanArchitectureTests/*|/*/*/CodeHygieneTests/*|/*/*/NamingConventionTests/*" --minimum-expected-tests 1` passed 13 tests.
- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed: 2 projects, 0 errors, existing `AutoMapper` NU1903 warnings only.
- `git diff --check` passed for tracked touched storage code and workstream docs; `git diff --check --no-index /dev/null Event.Application.UnitTests/Features/StorageObjects/Queries/GetPresignedDownloadUrlRequestHandlerTests.cs` produced no whitespace errors for the untracked regression file.

### 2026-07-04 Storage Presigned Download Verification Evidence

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed for the Application handler change, with existing dependency warnings.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed for the API controller change, with existing dependency warnings.
- Focused `Event.Application.UnitTests` presigned-download execution passed 5 tests with `--treenode-filter "/*/*/GetPresignedDownloadUrlRequestHandlerTests/*"`.
- Focused `Event.API.IntegrationTests` storage controller execution passed 10 tests with `--treenode-filter "/*/*/StorageUploadSessionControllerTests/*"`.
- LSP diagnostics were clean for `GetPresignedDownloadUrlRequestHandler.cs`, `GetPresignedDownloadUrlRequestHandlerTests.cs`, `StorageObjectController.cs`, and `StorageUploadSessionControllerTests.cs`.
- `git diff --check` was clean for the changed Application handler, API controller, focused tests, and workstream files.

### 2026-07-04 Blazor Upload UX/Error/Log Safety Verification Evidence

- `dotnet build --configuration Release --verbosity quiet` passed: 25 projects, 0 errors, existing package/deprecation warnings only.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed: 1454 total, 1453 succeeded, 1 skipped.
- Earlier VSTest-style filtering failed because the project uses Microsoft.Testing.Platform/TUnit and does not accept `--filter` or `--logger`; the canonical unfiltered per-project command above was used instead.

### 2026-07-04 Storage Provider Result Verification Evidence

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed for the Application handler change, with existing dependency/analyzer warnings.
- Focused `Event.Application.UnitTests` storage upload-session execution passed 19 tests after rebuilding with `--treenode-filter "/*/*/StorageUploadSessionCommandHandlerTests/*"`.
- Focused `Event.Application.UnitTests` storage upload-session execution then passed 19 tests again with `--no-build` on the rebuilt artifacts.
- LSP diagnostics were clean for `FinalizeStorageUploadSessionCommandHandler.cs` and `StorageUploadSessionCommandHandlerTests.cs`.
- `git diff --check` was clean for the changed Application handler and focused test file.

### 2026-07-04 Storage API Failure Mapping Verification Evidence

- Focused `Event.API.IntegrationTests` storage controller execution passed 11 tests after rebuilding with `--treenode-filter "/*/*/StorageUploadSessionControllerTests/*"`.
- Focused `Event.API.IntegrationTests` storage controller execution then passed 11 tests again with `--no-build` on the rebuilt artifacts.
- LSP diagnostics were clean for `StorageUploadSessionControllerTests.cs`.
- `git diff --check` was clean for the changed API integration test file.

### 2026-07-04 Storage Content Signature Verification Evidence

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed for the new content-signature policy and finalize handler integration, with existing warnings.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed for the storage failure-code API mapping, with existing warnings.
- Focused `Event.Application.UnitTests` storage upload-session execution passed 22 tests with `--treenode-filter "/*/*/StorageUploadSessionCommandHandlerTests/*"`.
- Focused `Event.Architecture.Tests` clean-layer/hygiene/naming execution passed 13 tests with `--treenode-filter "/*/*/CleanArchitectureTests/*|/*/*/CodeHygieneTests/*|/*/*/NamingConventionTests/*"`.
- `git diff --check` was clean for `StorageContentSignaturePolicy.cs`, `FinalizeStorageUploadSessionCommandHandler.cs`, `FailureCodes.cs`, `CommandResponseResultMapper.cs`, `StorageUploadSessionCommandHandlerTests.cs`, and `StorageUploadSessionControllerTests.cs`.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passed with existing warnings.
- Focused `Event.API.IntegrationTests` storage controller execution passed 11 tests with `--treenode-filter "/*/*/StorageUploadSessionControllerTests/*"`, including the new content-signature failure mapping.

### 2026-07-04 Storage Runtime Route Coverage Verification Evidence

- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passed with existing warnings.
- Focused `Event.API.IntegrationTests` storage object runtime execution passed 13 tests with `--treenode-filter "/*/*/StorageObjectControllerTests/*"`.

### 2026-07-04 AI Rail Raw Rendering Removal Verification Evidence

- `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed with existing warnings.
- Focused `Explore.Blazor.Client.Tests` AI rail execution passed 21 tests with `--treenode-filter "/*/*/AiAssistantRailTests/*"`.
- `rg "MarkupString" Explore.Blazor Explore.Blazor.Client` now reports only `Explore.Blazor.Client/Pages/Legal/CommunityGuidelines.razor`.
- Browser screenshot visual QA was not run for this slice because there was no CSS/layout/interaction change; verification used rendered-DOM bUnit coverage for the security-sensitive output path.

### 2026-07-04 Community Guidelines Raw Rendering Verification Evidence

- `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed with existing warnings.
- Focused `Explore.Blazor.Client.Tests` community guidelines execution passed 1 test with `--treenode-filter "/*/*/CommunityGuidelinesTests/*"`.

### 2026-07-04 Public Rich Text And Email HTML Encoding Verification Evidence

- `dotnet build Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed with existing warnings.
- Focused `Event.Application.UnitTests` email factory execution passed 5 tests with `--treenode-filter "/*/*/EventLifecycleEmailOutboxFactoryTests/*"`.
- `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed with existing warnings.
- Focused `Explore.Blazor.Client.Tests` home page execution passed 8 tests with `--treenode-filter "/*/*/HomeTests/*"`.

### 2026-07-04 Admin Status Raw Markup Removal Verification Evidence

- Focused `Explore.Blazor.Client.Tests` projection status execution passed 1 test with `--treenode-filter "/*/*/CustomPropertyGovernanceTests/ProjectionStatus_EncodesDangerousLastErrorText"`.
- Focused `Explore.Blazor.Client.Tests` custom property governance execution passed 13 tests with `--treenode-filter "/*/*/CustomPropertyGovernanceTests/*"`.
- Sequential architecture verification passed `BlazorClientArchitectureTests` (17 tests), `AccessibilityConventionTests` (8 tests), and `CodeHygieneTests` (4 tests).
- Focused agent-context architecture verification passed `AgentContextSchemaTests`, `AgentContextIntentManifestTests`, and `AgentContextDuplicationTests` (9 tests).
- `dotnet format whitespace --include Explore.Blazor.Client/Pages/Admin/CustomProperties/Components/ProjectionStatusSection.razor Explore.Blazor.Client/Pages/Admin/CustomProperties/Components/ExposureGovernanceSection.razor Explore.Blazor.Client.Tests/Pages/Admin/CustomPropertyGovernanceTests.cs --verbosity quiet` completed successfully.
- `rg -n "MarkupString|AddMarkupContent|innerHTML|outerHTML|insertAdjacentHTML|document\\.write|setHTML\\(|eval\\(" Explore.Blazor Explore.Blazor.Client` reports only `Explore.Blazor.Client/Pages/Legal/CommunityGuidelines.razor`.
- LSP diagnostics for `CustomPropertyGovernanceTests.cs` are clean; Razor LSP is not installed, so Razor component validation relies on build/test execution.
- `git diff --check` is clean for the touched Blazor component, test, and workstream files.

### 2026-07-04 Storage Projection URL Helper Verification Evidence

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed for the shared resolver and projection handler changes.
- Focused `Event.Application.UnitTests` resolver execution passed 5 tests after rebuilding with `--treenode-filter "/*/*/StoragePresentationUrlResolverTests/*"`.
- Focused `Event.Application.UnitTests` resolver execution then passed 5 tests again with `--no-build` on the rebuilt artifacts.
- LSP diagnostics were clean for `StoragePresentationUrlResolver.cs`, `StoragePresentationUrlResolverTests.cs`, `EventDetailsProjectionService.cs`, tag/category projection handlers, and the touched group query handlers.
- `git diff --check` was clean for the shared resolver, resolver tests, and touched projection handlers.

### 2026-07-04 Storage Projection URL Helper Completion Verification Evidence

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed for the remaining projection handler migrations.
- Focused `Event.Application.UnitTests` resolver execution passed 5 tests with `--no-build` and `--treenode-filter "/*/*/StoragePresentationUrlResolverTests/*"`.
- Focused `Event.Architecture.Tests` agent-context execution passed 9 tests with `--no-build` and `--treenode-filter "/*/*/AgentContextSchemaTests/*|/*/*/AgentContextIntentManifestTests/*|/*/*/AgentContextDuplicationTests/*"`.
- LSP diagnostics were clean for the migrated actor, organization, event-list, managed-events, my-events, and user projection handlers.
- `git diff --check` was clean for the migrated actor, organization, event-list, managed-events, my-events, and user projection handlers.
- `rg` found no remaining old private async `ResolveImageUrl` copies, URI-path object-key extraction, `ObjectKeyOrUri` log templates, or raw object-key presign log templates under `Explore.Application/Features` and `Explore.Application/Services`.
- Full `Event.Architecture.Tests` was attempted and currently fails on unrelated support-access authorization parity drift: missing Cerbos policy and fallback authorization case for `islamuevent_support_access_session`.

---

## Deferred Or Separate Work

- [ ] Resolve the broader documentation tension between `docs/UI_GOVERNANCE.md` and `docs/DESIGN_SYSTEM.md` about wrapper wording. This is not required to continue validation hardening because the local form rule is clear: no `MudForm`.
- [ ] Decide whether a reusable sanitizer package is needed after rich-content seams are classified. Do not choose a sanitizer before the product/content decision.
- [ ] Consider architecture/static checks for raw markup after the inventory is stable.
- [ ] Consider shared test helpers for problem-details field-key assertions after several slices duplicate the same assertions.
