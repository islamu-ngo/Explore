<!-- ABOUTME: Corrected implementation checklist for input validation and sanitization hardening. -->
<!-- ABOUTME: Tracks API, BFF, Blazor, sanitization, generated-client, and verification tasks. -->

# Full Input Validation & Sanitization - Tasks

Last Updated: 2026-07-04 Europe/Brussels
Status: Senior CTO rebaseline updated; implementation is partially complete and must continue by matrix-backed slice
Current priority: high-risk API/Application validation gaps, then BFF trust-boundary gaps, then Blazor form/server-error/rendering convergence. Storage work is no longer the default next lane unless a new matrix row proves a concrete endpoint/display/log gap.

---

## Task Rules

- Keep this checklist aligned with the plan, context, matrix, and contract decisions.
- Do not mark a task complete unless code or documentation evidence has been verified in the repo.
- Prefer narrow vertical slices with focused tests. A slice should usually touch one matrix row or one tightly related endpoint family.
- Do not perform audit-only work. An audit task is complete only when it produces a matrix update, accepted/deferred contract decision, code/test change, or documented deferral with owner and rationale.
- Do not add broad cleanup tasks unrelated to input validation, sanitization, error safety, BFF boundaries, or Blazor form validation.
- Do not reintroduce prohibited patterns: global sanitizers, validator DI auto-registration, `MudForm` standardization, Blazor-only authority, role/claim affordance gating, or asking the user to regenerate clients.
- Do not duplicate Application command validation in BFF or Blazor. BFF validates BFF-owned seams; Blazor validates UX and maps server errors.
- Treat sanitization as seam-specific: output encoding by default, CSV/TSV formula neutralization for export artifacts, safe filename generation for downloads/uploads, and HTML sanitizers only after an accepted rich-content decision.

## Next Slice Queue

Use this queue unless fresh evidence shows a higher-risk row:

1. `Blazor form validation convergence`: target event/session/organization forms with API ProblemDetails mapping, accessible summaries/focus, duplicate-submit behavior, and HAL-only affordance gating.
2. `Email dispatch admin semantics`: add tenant-B outbox masking and raw SMTP/provider/log leakage coverage where not already proven.
3. `Idempotency and cross-cutting header residuals`: cover idempotency-key, tenant-hint, and forwarded-host validation rows.
4. `BFF boundary residuals`: continue only when the matrix identifies an uncovered browser-controlled header, cookie, continuation path, antiforgery, or diagnostics seam.

---

## Phase 0 - Rebaseline And Research

Status: Complete for the 2026-07-04 planning update.

- [x] Read `AGENTS.md` and the repo Contribution Contract.
- [x] Read senior CTO feedback skill and required resources.
- [x] Read repo governance, API, Blazor, security, authorization, testing, and operation docs.
- [x] Read relevant `.claude/rules/*.md`.
- [x] Preserve earlier Tavily MCP/OWASP/IETF research evidence and record that the latest Tavily MCP refresh attempt for this continuation returned usage-limit status `432`.
- [x] Use Context7 MCP for current ASP.NET Core validation, Blazor form/rendering, FluentValidation, and MudBlazor documentation.
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

- [ ] Before editing a boundary, update the matching matrix row with trust boundary, caller/auth level, tenant source, validation owner, canonicalization owner, sanitization/encoding owner, error contract, abuse cases, and tests.
- [ ] Add missing rows only for the slice being implemented: route IDs, headers, idempotency keys, continuation URLs, setup secrets, storage metadata, rich content, or Blazor forms.
- [ ] Mark intentional compatibility exceptions, including any request type that allows unknown JSON members, with a contract decision or explicit deferral.
- [ ] Do not start broad DTO/UI convergence until the selected row has owner, threat model, priority, decision status, and regression test target.

### 1.2 Application Validator Coverage

- [ ] For the selected matrix row, identify only the commands/queries where syntactic validation belongs in Application.
- [ ] Add or update validators without DI auto-registration.
- [ ] Use `ValidateAsync` in handlers/tests.
- [ ] Keep validators side-effect free.
- [ ] Keep canonicalization outside validators unless the validator is only comparing normalized values.
- [ ] Add unit tests for the selected row's required fields, ranges, lengths, enum allowlists, invalid identifiers, invalid dates, and cross-field rules.

### 1.3 Handler Semantic Guards

- [ ] For the selected matrix row, identify validation rules that require tenant context, repositories, authorization, state, clock, or persistence.
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

### 2026-07-04 Implemented Contact Share Export Validation/Sanitization Slice

- [x] Add `ContactShareConsentExportQueryRequest` so `ContactShareConsentController.ExportSharedContacts` binds export query inputs through the API query-validation model pattern instead of a raw `string`.
- [x] Add `QueryValidationRules.ValidateContactShareExportFormat` with a fixed `csv`/`tsv` allowlist, blank rejection, control-character rejection, and normalized lowercase command value.
- [x] Keep `ExportSharedContactsCommandHandler` defensive by rejecting null/blank/control-character/unsupported formats before repository reads or export side effects.
- [x] Sanitize generated shared-contact export artifacts: neutralize spreadsheet-formula cells before CSV/TSV escaping and derive the server filename from a bounded safe organization slug.
- [x] Harden `Explore.Blazor.Client.Services.ContactShareConsentService` so Blazor normalizes supported export formats and returns a safe `null` result without calling the generated API client for unsupported format values.
- [x] Regenerate the Blazor API client through the documented NSwag target after the API contract change; verify no accidental `NormalizedFormat` query parameter is exposed.
- [x] Add/extend tests in `PublicQueryValidationTests`, `ExportSharedContactsCommandHandlerTests`, and `ContactShareConsentServiceTests`.
- [x] Record Context7 evidence: ASP.NET Core `[ApiController]` automatically returns 400 `ValidationProblemDetails` for model-validation failures from query-bound models. Record Tavily limitation: fresh Tavily search/research attempts for OWASP API input validation returned usage-limit status `432`.

### 2026-07-04 Implemented Public Event Query Temporal View Validation Slice

- [x] Add `QueryValidationRules.ValidateTemporalView` so public event-list `view` query input is an explicit allowlist instead of a silent best-effort enum parse.
- [x] Validate `EventFilterRequest.View` before `EventController.GetAll` dispatches MediatR, preserving thin controller behavior and relying on `[ApiController]` automatic 400 handling.
- [x] Reject unknown and undefined numeric temporal-view values; accept known values such as `upcomingAndOngoing`.
- [x] Add model-level coverage for invalid, undefined numeric, and valid `view` values.
- [x] Add runtime API coverage proving `GET /api/Event?view=sideways` returns canonical `400 application/problem+json` with the stable `view` field key and without echoing the submitted value.
- [x] Add `EventSessionFilterRequest` pagination coverage while staying inside the existing shared query-validation rules.
- [x] Record Context7 evidence: ASP.NET Core `[ApiController]` automatically returns HTTP 400 for model-validation failures before action execution, so query-model validation is the correct boundary and no controller `ModelState` branch is needed.

### 2026-07-04 Implemented External API Key Usage Report Query Validation Slice

- [x] Add `ExternalApiKeyUsageReportQueryRequest` for `from`, `to`, and optional `tenantId` instead of binding raw scalar query parameters in `ExternalApiKeyController.GetUsageReport`.
- [x] Add shared `QueryValidationRules.ValidateRequiredDateRange` for required date pairs, inverted ranges, and bounded range length.
- [x] Bound usage-report queries to `366` days to prevent unbounded expensive reporting windows.
- [x] Validate optional `tenantId` shape and reject `Guid.Empty` before the Application handler performs tenant/admin authorization.
- [x] Keep the controller thin: model validation happens through `[ApiController]`; the action only maps the validated query model into `GetExternalApiKeyUsageReportRequest`.
- [x] Add query-model tests for missing dates, inverted date range, oversized range, and empty tenant ID.
- [x] Add runtime API coverage proving invalid date ranges return canonical `400` validation ProblemDetails before handler admin authorization.
- [x] Record Context7 evidence: ASP.NET Core `[ApiController]` automatically returns HTTP 400 for model-validation failures. Record Tavily limitation: fresh OWASP query-validation search returned usage-limit status `432`.

### 2026-07-04 Implemented External API Key Usage Report Semantic Authorization Slice

- [x] Add `ExternalApiKeyUsageReportRequestHandlerTests` for valid usage-report inputs after API query-shape validation.
- [x] Prove a tenant-scoped usage-report request from a caller with no tenant-admin or instance-admin authority throws `AuthorizationException` before tenant or platform-wide quota repository reads.
- [x] Prove a tenant admin request reads only the requested tenant through `GetUsageByTenant` and does not call `GetUsagePlatformWide`.
- [x] Prove a platform-wide usage-report request from a non-instance admin throws `AuthorizationException` before quota repository reads.
- [x] Prove an instance admin platform-wide request uses `GetUsagePlatformWide` and does not call the tenant-scoped read path.
- [x] Record the evidence boundary: this is Application handler semantic coverage; add HTTP runtime tenant-admin coverage only if a future matrix row identifies a cheap fixture path.

### 2026-07-04 Implemented External API Key Create/Update Input Hardening Slice

- [x] Add shared Application input normalization helpers for external API key name/description limits aligned to EF constraints.
- [x] Reject control characters in external API key create/update names before repository create/update side effects.
- [x] Bound create descriptions to 1000 characters and reject description control characters before persistence.
- [x] Compare external API key names after trimming so `"Bot"` and `" Bot "` cannot bypass owner-scoped uniqueness checks.
- [x] Normalize create descriptions to trimmed text or `null` before storing.
- [x] Add Blazor-side `CreateExternalApiKeyDtoValidator` so the API key dialog gives immediate UX feedback for name, description, and scope errors while keeping the API/Application authoritative.
- [x] Wire `CreateApiKeyDialog.razor` to `FluentValidationValidator`, add name/description max counters, and trim submitted name/description before calling the service.
- [x] Add Application unit tests for create/update control-character rejection, overlong descriptions, normalized duplicate names, and no repository side effects on invalid input.
- [x] Add Blazor client validator tests for control-character name, overlong description, and empty scopes.
- [x] Add API integration tests for invalid create/update inputs.
- [x] Add Application unit coverage proving successful API key creation returns the raw key once while persisting only the hash and omitting the raw key/secret fragment from handler logs.
- [x] Add API integration coverage for `/api/externalapikey` tenant mismatch with a persisted API key and for non-create detail responses omitting `apiKey`, `secretHash`, the raw key, and the secret fragment.
- [x] Record Context7 evidence: ASP.NET Core `[ApiController]` automatic model-validation behavior remains the API boundary. Record Tavily limitation: fresh OWASP input-validation search returned usage-limit status `432`.

### 2026-07-04 Implemented Email Dispatch Admin Query Validation Slice

- [x] Add `EmailDispatchStatusQueryRequest` so `/api/admin/email-dispatch/status` validates required `tenantId` and bounds `limit` to 1 through 200 before MediatR dispatch.
- [x] Add `EmailDispatchPauseTenantQueryRequest` so pause `reason` remains optional but rejects control characters and values over 500 characters at the API model-validation boundary.
- [x] Add `EmailDispatchParkQueryRequest` so park `reason` is required, bounded to 500 characters, and checked for unsupported control characters before command dispatch.
- [x] Update `EmailDispatchAdminController` to bind the query models, keep controller logic thin, and pass normalized reason text into existing Application commands.
- [x] Keep existing Application command validators as defense-in-depth for `TenantId`, `OutboxId`, and reason constraints.
- [x] Add `EmailDispatchAdminControllerTests` model and runtime coverage proving invalid status limit, missing park reason, and overlong pause reason return canonical `400` validation ProblemDetails before the MediatR pipeline runs.
- [x] Record Context7 evidence: ASP.NET Core `[ApiController]` automatically returns HTTP 400 for model-validation failures. Record Tavily limitation: fresh OWASP input-validation search returned usage-limit status `432`.

### 2026-07-04 Implemented Public Detail Route-ID Cross-Tenant Masking Slice

- [x] Reuse existing malformed route-ID coverage for event, event-session, organization, location, actor, category, tag, and storage routes instead of duplicating `not-a-guid` tests.
- [x] Add `EventVisibilityContractTests.GetByIdForCrossTenantEventReturnsSafeNotFound` so a published public event from another tenant returns `404` on `/api/event/{id}` and does not echo the event title or tenant ID.
- [x] Add `EventSessionVisibilityContractTests.GetByIdReturnsSafeNotFoundForCrossTenantSession` so a published session under another tenant returns `404` on `/api/eventsession/{id}` and does not echo the session title or tenant ID.
- [x] Keep this as a representative route-family slice; remaining event graph route families need new tests only when the matrix identifies an uncovered route or resource-oracle risk.

### 2026-07-04 Implemented Correlation Header Validation Slice

- [x] Bound API `X-Correlation-ID` and `X-Request-ID` values to one non-blank visible-ASCII value no longer than 128 characters before storing them in `HttpContext.Items`, Serilog context, and response headers.
- [x] Generate the normal server correlation ID when browser-supplied correlation metadata is unsafe instead of echoing huge or control-character values.
- [x] Preserve safe `X-Request-ID` fallback behavior when `X-Correlation-ID` is unsafe.
- [x] Harden the shared BFF proxy header sanitizer so unsafe correlation/request metadata is stripped before YARP forwards to the API, while safe metadata such as `Accept`, `Accept-Language`, and bounded correlation IDs survive.
- [x] Add focused API and BFF integration tests for unsafe overlong/control-character correlation metadata.
- [x] Record Context7 evidence for ASP.NET Core BFF/minimal API antiforgery and middleware trust-boundary behavior. Tavily MCP still returns usage-limit status `432`, so this slice adds no fresh Tavily source evidence.

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

### 2026-07-04 Implemented Storage Infrastructure Health Log Redaction Slice

- [x] Remove raw exception-object logging from `S3FileStorageProvider.TestAsync` health probes.
- [x] Remove raw exception-object logging from `LocalFileStorageProvider.TestAsync` health probes.
- [x] Remove raw endpoint logging and raw exception-object logging from legacy `ObjectStorageService.TestConnectionAsync`.
- [x] Replace storage provider health/probe failure logs with bounded `FailureType` categories.
- [x] Add `Explore.Infrastructure.Tests` log-capture coverage proving S3 endpoints, bucket names, local filesystem paths, provider exception text, and storage secrets are not present in formatted failure logs.

### 2026-07-04 Implemented Storage Upload Failure Response Redaction Slice

- [x] Canonicalize API `ProblemDetails.detail` for known storage upload failure codes in `CommandResponseResultMapper`.
- [x] Canonicalize `ValidationProblemDetails.errors["storageUpload"]` for known storage upload validation failure codes instead of echoing lower-layer `BaseCommandResponse.Errors`.
- [x] Preserve existing status, title, type, and `code` mappings for upload-session not-found, conflict, payload-too-large, quota, validation, and provider-unavailable responses.
- [x] Add controller contract coverage proving provider failure details do not echo S3 status text, internal endpoints, object keys, presigned query values, or secret markers.
- [x] Add controller contract coverage proving validation failure errors do not echo internal policy/content-type details or object-key values.

### 2026-07-04 Implemented Storage Object Metadata Create Failure Mapping Slice

- [x] Add an API-owned create-validation descriptor for `StorageObjectController.Create`.
- [x] Map failed `CreateStorageObjectCommand` responses to canonical `400` validation ProblemDetails instead of returning `200 OK` with `Success=false`.
- [x] Preserve successful create behavior as `200 OK` with the command response.
- [x] Add controller contract coverage proving create validation failures use the stable `storageObject` error key, safe detail, and `validation_failed` code.

### 2026-07-04 Implemented Storage Metadata Update Tenant-Authority Slice

- [x] Remove client-supplied DTO `TenantId` from storage metadata create/update command authorization context.
- [x] Keep storage metadata create authorization collection-scoped; persistence tenant authority remains server-owned through `ITenantContext`.
- [x] Authorize storage metadata update against the persisted `StorageObject`, not against tenant values supplied by the update DTO.
- [x] Update `UpdateStorageObjectCommandHandler` so it loads the existing current-tenant entity, preserves persisted `TenantId`, and patches only allowed metadata fields.
- [x] Return a safe not-found-style command failure for missing or wrong-tenant storage metadata updates with no repository update side effect.
- [x] Add Application unit coverage for command authorization metadata, persisted-resource authorization enrichment, DTO tenant tampering, allowed-field update patching, and wrong-tenant no-update behavior.

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

### 2026-07-04 Implemented Blazor Storage Display Safety Slice

- [x] Re-audit current Blazor storage display paths and confirm `StorageImage.razor`, `ImageUpload.razor`, and instance/tenant storage settings render dynamic text through normal Razor/MudBlazor bindings rather than raw markup.
- [x] Add `StorageImage` bUnit coverage proving untrusted storage-adjacent metadata in `Alt` remains an attribute value and does not create an injected `onerror` attribute.
- [x] Add `StorageImage` bUnit coverage proving dangerous error/display text is rendered as encoded text and does not create attacker-controlled `img` or `script` DOM nodes.
- [x] Extend `BrowserInteropSafetyTests` so any new `MarkupString` or `AddMarkupContent` use under `Explore.Blazor` or `Explore.Blazor.Client` must be deliberately reviewed and allowlisted.
- [x] Keep `CommunityGuidelines.razor` as the only current raw-markup allowlist entry because its tenant content is already escaped before controlled structural markup is emitted.
- [x] Record Context7 evidence: ASP.NET Core Blazor renders strings as plain text by default, `MarkupString`/`AddMarkupContent` are raw-rendering seams, and user-controlled strings should flow through normal Razor text/attribute bindings or `RenderTreeBuilder.AddContent`.
- [x] Record Tavily limitation: this continuation's OWASP output-encoding search failed with usage-limit status `432`; no new Tavily result from this slice is treated as evidence.

### 2.1 Upload Request Semantics

- [x] Review `UploadRequestDtoValidator` and storage metadata validators against file-upload guidance and current product limits for existing storage seams.
- [x] Validate filename display values for control characters, path separators, reserved names, and unsafe normalization cases in the current storage validator layer.
- [x] Validate content type as a hint only for finalized uploaded bytes; do not trust it as proof of file content.
- [x] Validate extension allowlist for known finalized image/document upload content types.
- [ ] Validate declared size/count before expensive operations only for newly discovered storage/API upload rows not already covered by upload-session reservation and finalization tests.
- [x] Validate provider-reported byte count, content type, SHA-256 digest, and object-key namespace after provider write before metadata persistence.
- [x] Confirm the current storage DTO/domain model has no arbitrary metadata dictionary, so metadata key/value/count validation is not a current code seam.
- [x] Add tests for spoofed finalized bytes and invalid known-content extension.
- [ ] Add remaining endpoint/runtime tests only when a matrix row identifies a storage surface not already covered by validator, Application handler, API runtime, BFF, or Blazor display tests.

### 2.2 Storage Object And Session Ownership

- [x] Ensure current object keys/storage IDs are server-generated or strictly validated before storage metadata becomes authoritative.
- [x] Validate upload-session IDs before finalize/cancel/proxy operations currently covered by API/BFF route-shape, missing/canceled/finalized semantic runtime, and opaque-session tests.
- [x] Verify current upload sessions are bound to tenant, user, and intended operation for finalize/cancel/proxy paths covered in this workstream.
- [ ] Add more tenant mismatch, replay, expired session, or mismatched object ID tests only for remaining API/BFF storage endpoints that a matrix row identifies as uncovered.
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
- [x] Ensure storage metadata create/update authorization does not trust client-supplied `TenantId`; create is collection-scoped and update authorization is enriched from the persisted storage object.
- [x] Ensure storage metadata update preserves the persisted tenant and fails missing or wrong-tenant updates before mutation.

### 2.3 Storage Logging And Display Safety

- [ ] Audit logs for raw filenames, object keys, metadata values, and storage provider messages.
- [ ] Redact or encode sensitive values in structured logs.
- [x] Remove raw API upload-session IDs from BFF storage proxy failure logs.
- [x] Remove raw endpoint/path and raw exception payload logging from storage provider health and legacy S3 connection probes.
- [x] Remove raw URI/object-key extraction and response echo from the by-ID presigned-download handler.
- [x] Fail provider-result mismatches without echoing provider-returned object keys, content types, or checksums in user-facing errors.
- [x] Canonicalize known storage upload failure `detail` and validation `errors` at the API mapper boundary so lower-layer messages cannot leak into RFC 7807 responses.
- [x] Map legacy storage object metadata create command failures to validation ProblemDetails instead of success-status command envelopes.
- [x] Return storage content/download responses with persisted validated content types instead of provider-returned MIME hints.
- [x] Add runtime response-redaction coverage for private-owner storage read masking.
- [x] Centralize projection image URL signing for event detail, tag/category event projections, and group projections through `StoragePresentationUrlResolver`.
- [x] Replace remaining actor, organization, event-list, managed-events, my-events, and user projection resolver copies with `StoragePresentationUrlResolver`.
- [x] Bucket storage provider/proxy exception logs without raw exception objects in storage presentation URL signing, content read, and BFF upload proxy paths.
- [x] Ensure current Blazor storage display paths render storage-adjacent filenames/metadata/error text through normal encoded text or encoded attributes, not raw markup.
- [ ] Add regression tests where existing helpers can assert no sensitive response echo.

---

## Phase 3 - Tenant-Aware Semantic Validation And Persistence Backstops

Goal: Prove invalid or cross-tenant data cannot bypass UI/API validation into persistence.

- [ ] Start with the matrix queue: event/session create-update-publish, custom-property runtime APIs, external API key create/update, email dispatch admin APIs, settings/governance writes, and public/detail route-ID families.
- [ ] For the selected row, list write handlers that accept client-supplied aggregate IDs or tenant-bound child IDs.
- [ ] For the selected high-risk handler, add tenant-mismatch tests.
- [ ] Verify repository queries include tenant predicates where tenant isolation applies.
- [ ] Add persistence/integration tests for race-sensitive uniqueness constraints.
- [ ] Verify handlers do not map untrusted DTOs into persisted entities before validation/canonicalization.
- [ ] Confirm validation failures happen before external side effects.

---

## Phase 4 - BFF Boundary Residual Audit

Goal: Validate BFF-only seams without duplicating Application command validation.

### 4.1 BFF Route Matrix

- [ ] For the selected BFF route family, update the matrix row for route inputs, headers, cookies, continuation paths, antiforgery posture, and safe error contract.
- [ ] Classify each selected unsafe route as antiforgery-protected or documented exception with compensating controls.
- [ ] Do not copy Application command validators into BFF. Only validate BFF-owned inputs such as opaque session IDs, setup-secret source, proxy path/header shape, cookies, and browser-controlled forwarding headers.

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

- [x] Pick one form family from the matrix before editing: organization create/details/settings.
- [x] Convert the selected create form to existing primitives where needed: `EditForm`, `EditContext`, `FormSubmissionGuard`, `FormSubmitState`, `ServerValidationErrorStore`, and `AppValidationSummary`.
- [x] Keep client rules limited to immediate UX checks such as required values, ranges, basic formats, and duplicate-submit protection.
- [x] Do not introduce `MudForm`.
- [x] Do not call Application validators from `Explore.Blazor.Client`.
- [x] Continue the same convergence for organization details/settings.
- [x] Continue the same convergence for event create/edit server-error mapping and raw unexpected-exception non-echo.
- [ ] Continue the same convergence for session create/edit.

### 5.2 Server Error Mapping

- [x] Extend `ServerValidationErrorStore` to handle generated-client `ValidationProblemDetails`.
- [x] Map case-insensitive and nested server field keys such as `Email.Value` back to the Blazor model field where possible.
- [x] Map create-organization server errors through `ServerValidationErrorStore`.
- [x] Clear server errors on field edit according to existing component behavior.
- [x] Add `CreateOrganizationTests` for generated API validation errors and raw unexpected-exception non-echo.
- [x] Add organization-details/settings coverage for client validation, generated API validation errors, and raw unexpected-exception non-echo.
- [x] Add event create/edit coverage for generated API validation errors and raw unexpected-exception non-echo.
- [ ] Add non-field, repeated-submission, and clear/reset coverage where the next selected Blazor form row exposes those states.

### 5.3 Accessibility And HAL Affordances

- [ ] Verify validation summaries are reachable and announced by existing accessibility patterns.
- [ ] Verify error focus behavior.
- [ ] Verify actions remain gated by HAL `_links`, not local roles/claims.
- [ ] Add component tests for link-present and link-absent action states where forms expose edit/delete/submit affordances.
- [ ] Complete fresh browser visual/accessibility QA for the changed organization create route once an authenticated Blazor surface can be rendered.

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
- [x] Add a source-level raw-rendering guard so future `MarkupString`/`AddMarkupContent` use outside the reviewed allowlist fails the Blazor client test suite.
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
- [x] Blazor client form/rendering changes: run `Explore.Blazor.Client.Tests`.
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

### 2026-07-04 Public Event Query Temporal View Verification Evidence

- Context7 MCP `/dotnet/aspnetcore.docs` confirmed `[ApiController]` automatically returns HTTP 400 for model-validation failures, so query-model validation is the correct boundary and no controller `ModelState` branch is needed.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -clp:ErrorsOnly` passed: 8 projects, 0 errors, existing warning debt.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/PublicQueryValidationTests/*|/*/*/PublicQueryRuntimeValidationTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passed: 26 tests.
- The runtime test proves `GET /api/Event?view=sideways` returns `400 application/problem+json`, keeps the stable `view` field key, and does not echo the submitted value in the response body.
- Focused `Event.Architecture.Tests` passed for `CleanArchitectureTests` (13), `CodeHygieneTests` (4), `NamingConventionTests` (10), and agent-context schema/intent/duplication tests (9).
- `git diff --check` passed for the touched API model, API integration test, and workstream documentation files.

### 2026-07-04 Public Detail Route-ID Cross-Tenant Verification Evidence

- `dotnet format whitespace Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --include Event.API.IntegrationTests/Features/EventVisibilityContractTests.cs Event.API.IntegrationTests/Features/EventSessionVisibilityContractTests.cs --verbosity quiet` passed.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -clp:ErrorsOnly` passed: 8 projects, 0 errors, existing warning debt.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventVisibilityContractTests/*|/*/*/EventSessionVisibilityContractTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passed: 10 tests.
- The new tests prove cross-tenant event and event-session detail IDs return safe `404` responses and do not echo hidden titles or tenant IDs.

### 2026-07-04 Correlation Header Validation Verification Evidence

- Context7 MCP `/dotnet/aspnetcore.docs` confirmed ASP.NET Core antiforgery and middleware placement guidance for BFF/minimal API trust-boundary handling. Tavily MCP OWASP CSRF/input-boundary search returned usage-limit status `432`.
- `dotnet format whitespace` passed for `Event.Web.BffHosting/Security/BffProxyHeaderSanitizer.cs`, `Explore.API/Middleware/CorrelationIdMiddleware.cs`, `Explore.Blazor.IntegrationTests/Services/BffProxyHeaderSanitizerTests.cs`, and `Event.API.IntegrationTests/Features/Middleware/CorrelationIdTests.cs`.
- `dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -clp:ErrorsOnly` passed: 10 projects, 0 errors, existing warning debt.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -clp:ErrorsOnly` passed: 8 projects, 0 errors, existing warning debt.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffProxyHeaderSanitizerTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passed: 3 tests.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/CorrelationIdTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passed: 6 tests.

### 2026-07-04 External API Key Usage Report Query Verification Evidence

- Context7 MCP `/dotnet/aspnetcore.docs` confirmed `[ApiController]` automatic HTTP 400 behavior for model-validation failures. Tavily MCP OWASP input-validation search returned usage-limit status `432`.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -clp:ErrorsOnly` passed: 8 projects, 0 errors, existing warning debt.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/PublicQueryValidationTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passed: 30 tests.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ExternalApiKeyIntegrationTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passed: 12 tests.
- The runtime test proves an authenticated non-admin with an invalid usage-report date range receives canonical `400` validation ProblemDetails before the Application handler's admin authorization path.
- Focused `Event.Architecture.Tests` passed for `CleanArchitectureTests` (13), `CodeHygieneTests` (4), `NamingConventionTests` (10), and agent-context schema/intent/duplication tests (9).
- `git diff --check` passed for the touched API model, controller, API integration test, and workstream documentation files.

### 2026-07-04 External API Key Usage Report Semantic Verification Evidence

- `dotnet format whitespace Event.Application.UnitTests/Event.Application.UnitTests.csproj --include Event.Application.UnitTests/Features/ExternalApiKeys/Queries/ExternalApiKeyUsageReportRequestHandlerTests.cs --verbosity quiet` passed.
- `dotnet build Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -clp:ErrorsOnly` passed: 3 projects, 0 errors, existing warning debt.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ExternalApiKeyUsageReportRequestHandlerTests/*" --minimum-expected-tests 4 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passed: 4 tests.
- The handler tests prove unauthorized tenant-scoped and platform-wide usage-report reads fail before quota repository access, tenant admins read only the requested tenant path, and instance admins read only the platform-wide path.
- An initial combined test command used an overly high `--minimum-expected-tests` value for the selected filter; the corrected focused command above is the accepted evidence.

### 2026-07-04 External API Key Create/Update Input-Hardening Verification Evidence

- Context7 MCP `/dotnet/aspnetcore.docs` confirmed `[ApiController]` automatic validation behavior and validation ProblemDetails. Tavily MCP OWASP input-validation search returned usage-limit status `432`, so this slice adds no fresh Tavily source evidence.
- `dotnet build Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -clp:ErrorsOnly` passed with existing warning debt.
- `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -clp:ErrorsOnly` passed with existing warning debt.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ExternalApiKeyObservabilityTests/*|/*/*/ExternalApiKeyScopeCeilingTests/*" --minimum-expected-tests 12 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passed: 12 tests.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/CreateExternalApiKeyDtoValidatorTests/*" --minimum-expected-tests 3 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passed: 3 tests.
- `dotnet format whitespace --include ... --verbosity quiet` passed for the touched Application, Blazor, test, and active workstream documentation files.
- `dotnet format whitespace Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --include Event.API.IntegrationTests/Features/ExternalApiKeyIntegrationTests.cs Event.API.IntegrationTests/Features/ExternalApiPhase0IntegrationTests.cs --verbosity quiet` passed.
- `git diff --check -- Event.API.IntegrationTests/Features/ExternalApiKeyIntegrationTests.cs Event.API.IntegrationTests/Features/ExternalApiPhase0IntegrationTests.cs` passed.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -clp:ErrorsOnly` passed: 8 projects, 0 errors, existing warning debt.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ExternalApiKeyIntegrationTests/*|/*/*/ExternalApiPhase0IntegrationTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passed: 16 tests.
- Focused `Event.Architecture.Tests` compiled and ran 14 tests, but failed unrelated existing `CleanArchitectureTests.Persistence_ShouldNotHaveDependencyOn_ApplicationDtos`; this slice did not touch Persistence.

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

### 2026-07-04 Blazor Storage Display Safety Verification Evidence

- Context7 MCP ASP.NET Core Blazor docs confirmed the current rendering contract: normal string output is text/encoded, while `MarkupString` and `RenderTreeBuilder.AddMarkupContent` are raw-rendering seams.
- Tavily MCP OWASP output-encoding search failed with usage-limit status `432`; no new Tavily result from this continuation is treated as evidence.
- Initial VSTest-style focused execution failed because the TUnit/Microsoft.Testing.Platform runner does not accept `--filter` or `--logger`; the corrected command used `-- --treenode-filter`.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build -- --treenode-filter "/*/*/SharedComponentAccessibilityTests/*|/*/*/BrowserInteropSafetyTests/*" --minimum-expected-tests 1 --no-progress --no-ansi --output Normal` passed: 13 total, 12 succeeded, 1 existing skip.
- `dotnet build --configuration Release --verbosity quiet` initially passed: 27 projects, 0 errors, existing warning debt only.
- A later final solution-build rerun in the current dirty 28-project worktree failed outside this slice on existing warning/analyzer debt and missing `Explore.Application` reference output for unrelated projects; `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed for the touched Blazor boundary: 5 projects, 0 errors.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build -- --no-progress --no-ansi --output Normal` passed: 1485 total, 1484 succeeded, 1 existing skip.
- `rg -n "MarkupString|AddMarkupContent|innerHTML|outerHTML|insertAdjacentHTML|document\\.write|setHTML\\(|eval\\(" Explore.Blazor Explore.Blazor.Client -g '*.cs' -g '*.razor' -g '*.js' -g '!bin/**' -g '!obj/**' -g '!node_modules/**'` reports only `Explore.Blazor.Client/Pages/Legal/CommunityGuidelines.razor`.
- `git diff --check` passed after the test and workstream documentation updates.

### 2026-07-04 Storage Metadata Update Tenant-Authority Verification Evidence

- Context7 MCP `/dotnet/aspnetcore.docs` was refreshed for server-owned validation, ProblemDetails-style validation responses, untrusted file/upload metadata, and encoded Blazor rendering versus reviewed raw-rendering seams.
- Tavily MCP OWASP input-validation/file-upload/logging search returned usage-limit status `432`; no new Tavily result from this continuation is treated as evidence beyond the earlier OWASP/IETF research already recorded in the workstream.
- `dotnet build Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -clp:ErrorsOnly` passed: 3 projects, 0 errors, existing AutoMapper NU1903 warnings.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/StorageObjectCommandHandlerTests/*" --minimum-expected-tests 4 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passed: 4 tests.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/AuthorizationBehaviorTests/*" --minimum-expected-tests 18 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passed: 18 tests.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet -clp:ErrorsOnly` passed: 7 projects, 0 errors, existing package warnings.
- Focused `Event.Architecture.Tests` passed separately for `CleanArchitectureTests` (13), `CodeHygieneTests` (4), `NamingConventionTests` (10), and agent-context schema/intent/duplication tests (9).
- `git diff --check` passed for tracked touched code/docs; `git diff --check --no-index /dev/null Event.Application.UnitTests/Features/StorageObjects/Commands/StorageObjectCommandHandlerTests.cs` emitted no whitespace errors for the new untracked test file.
- An initial VSTest-style `--filter` attempt failed because this TUnit/Microsoft.Testing.Platform project requires `-- --treenode-filter`; the corrected focused commands above are the accepted evidence.

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

### 2026-07-04 Blazor Organization Validation Verification Evidence

- `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed after repairing stale generated-client `_links` anonymous-type test fixtures.
- Focused `CreateOrganizationTests` execution passed 3 tests with `--no-build` and `--treenode-filter "/*/*/CreateOrganizationTests/*"`.
- Focused `OrganizationDetailsHateoasTests` execution passed 6 tests with `--no-build` and `--treenode-filter "/*/*/OrganizationDetailsHateoasTests/*"`.
- Focused `OrganizationProfileSectionTests` execution passed 3 tests with `--no-build` and `--treenode-filter "/*/*/OrganizationProfileSectionTests/*"`.
- Full `Explore.Blazor.Client.Tests` execution passed with 1496 succeeded and 1 existing documented skip after one transient `AiAssistantRailTests.ReferenceHighlights_EncodeDangerousReferenceDisplayNames` failure passed in isolation and on full rerun.
- `git diff --check` was clean for the touched Blazor client, test, and workstream files.
- Full solution build remains outside this slice: the current worktree still fails `dotnet build --configuration Release --verbosity quiet` on existing warning-as-error/analyzer/package issues outside the Blazor organization validation changes.

---

## Deferred Or Separate Work

- [ ] Resolve the broader documentation tension between `docs/UI_GOVERNANCE.md` and `docs/DESIGN_SYSTEM.md` about wrapper wording. This is not required to continue validation hardening because the local form rule is clear: no `MudForm`.
- [ ] Decide whether a reusable sanitizer package is needed after rich-content seams are classified. Do not choose a sanitizer before the product/content decision.
- [ ] Consider architecture/static checks for raw markup after the inventory is stable.
- [ ] Consider shared test helpers for problem-details field-key assertions after several slices duplicate the same assertions.
