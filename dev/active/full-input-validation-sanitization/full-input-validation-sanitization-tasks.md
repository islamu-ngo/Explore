<!-- ABOUTME: Corrected implementation checklist for input validation and sanitization hardening. -->
<!-- ABOUTME: Tracks API, BFF, Blazor, sanitization, generated-client, and verification tasks. -->

# Full Input Validation & Sanitization - Tasks

Last Updated: 2026-07-03 Europe/Brussels
Status: Re-baselined; implementation is partially complete and must continue by slice
Current priority: storage/upload validation hardening, then raw-rendering review

---

## Task Rules

- Keep this checklist aligned with the plan, context, matrix, and contract decisions.
- Do not mark a task complete unless code or documentation evidence has been verified in the repo.
- Prefer narrow slices with focused tests.
- Do not add broad cleanup tasks unrelated to input validation, sanitization, error safety, BFF boundaries, or Blazor form validation.
- Do not reintroduce prohibited patterns: global sanitizers, validator DI auto-registration, `MudForm` standardization, Blazor-only authority, role/claim affordance gating, or asking the user to regenerate clients.

---

## Phase 0 - Rebaseline And Research

Status: Complete for the 2026-07-03 planning update.

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
- [ ] Keep controllers thin.

---

## Phase 2 - Storage, Upload, Metadata, And Object-Key Hardening

Goal: Complete the highest-risk remaining validation area.

### 2026-07-03 Completed Storage Slice

- [x] Harden `CreateStorageUploadSessionDtoValidator` for upload-session reservation metadata: malformed or wildcard MIME hints, control-character values, path separators, dot segments, reserved Windows device names, and unsafe extension tokens.
- [x] Normalize upload-session `ContentType` before storage policy resolution and persistence so route selection and stored metadata use the same canonical value.
- [x] Add validator regression coverage in `CreateStorageUploadSessionDtoValidatorTests`.
- [x] Add handler regression coverage in `StorageUploadSessionCommandHandlerTests` for content-type normalization before policy resolution.

### 2.1 Upload Request Semantics

- [ ] Review `UploadRequestDtoValidator` against OWASP file-upload guidance and current product limits.
- [ ] Validate filename display values for length, control characters, path separators, reserved names, and unsafe normalization cases.
- [ ] Validate content type as a hint only; do not trust it as proof of file content.
- [ ] Validate extension allowlist or explicitly document why extension is not part of this upload contract.
- [ ] Validate declared size/count before expensive operations.
- [ ] Validate metadata key/value length, character set, and count.
- [ ] Add tests for spoofed content type, invalid filename, oversized metadata, invalid extension, invalid size, and malformed metadata.

### 2.2 Storage Object And Session Ownership

- [ ] Ensure object keys/storage IDs are server-generated or strictly validated.
- [ ] Validate upload-session IDs before presign, complete, download, and proxy operations.
- [ ] Verify upload sessions are bound to tenant, user, and intended operation where required.
- [ ] Add tests for tenant mismatch, replayed session, wrong user, expired session, missing session, and mismatched object ID.
- [ ] Confirm failure responses do not reveal cross-tenant existence.

### 2.3 Storage Logging And Display Safety

- [ ] Audit logs for raw filenames, object keys, metadata values, and storage provider messages.
- [ ] Redact or encode sensitive values in structured logs.
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

---

## Phase 6 - Sanitization And Raw Rendering Review

Goal: Remove unnecessary raw rendering and define sanitizer ownership only where rich content is intentionally supported.

### 6.1 Raw Rendering Inventory

- [ ] Search for all `MarkupString`, raw HTML rendering, Markdown-to-HTML, email HTML body usage, `RenderFragment` composition from user input, and JS interop DOM injection.
- [ ] Add each seam to the input matrix.
- [ ] Classify each seam as controlled markup, encoded text, sanitized rich content, or remove.

### 6.2 High-Priority Component Review

- [ ] Review `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor` prompt-reference highlight markup.
- [ ] Prove every user-controlled value inserted into that markup is HTML-encoded before `MarkupString`.
- [ ] Prefer replacing the raw markup builder with component composition if feasible.
- [ ] Add `Explore.Blazor.Client.Tests` coverage with dangerous titles, reference labels, prompt excerpts, and malformed input.

### 6.3 Known Rich-Content Seams

- [ ] Re-check `CommunityGuidelines.razor` and document why it is controlled or encoded.
- [ ] Add a decision row for `PublicExperienceHomeBlockKind.RichText`.
- [ ] Add a decision row for `EmailMessage.HtmlBody`.
- [ ] Decide whether each rich-content seam is allowed, removed, stored as plain text, rendered only in trusted contexts, or sanitized.
- [ ] If sanitizer support is required, choose the owning layer and add allowlist tests for tags, attributes, URI schemes, event handlers, scripts, styles, malformed HTML, and dangerous URLs.

---

## Phase 7 - Observability, Logging, And Error Safety

Goal: Keep validation telemetry useful without leaking sensitive values.

- [ ] Audit validation failure logs for raw request bodies, tokens, cookies, setup secrets, object keys, filenames, rich text, email bodies, and provider payloads.
- [ ] Replace unsafe values with stable identifiers, lengths, hashes, or redacted placeholders where useful.
- [ ] Ensure user-facing validation errors do not echo secrets or raw rich content.
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
- Full Application unit suite passed: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet` ran 1,791 tests.
- Full Release build passed: `dotnet build --configuration Release --verbosity quiet` built 25 projects with existing warnings.
- LSP diagnostics were clean for the changed Application and test files.
- `git diff --check` was clean for the changed Application and test files.
- Architecture tests were run and are blocked by an unrelated existing context-manifest issue: intent `update-ai-context-disclosure` references missing `dev/active/ai-context-disclosure-policy/field-classification-matrix.md` and `dev/active/ai-context-disclosure-policy/ai-context-disclosure-policy-plan.md`.

---

## Deferred Or Separate Work

- [ ] Resolve the broader documentation tension between `docs/UI_GOVERNANCE.md` and `docs/DESIGN_SYSTEM.md` about wrapper wording. This is not required to continue validation hardening because the local form rule is clear: no `MudForm`.
- [ ] Decide whether a reusable sanitizer package is needed after rich-content seams are classified. Do not choose a sanitizer before the product/content decision.
- [ ] Consider architecture/static checks for raw markup after the inventory is stable.
- [ ] Consider shared test helpers for problem-details field-key assertions after several slices duplicate the same assertions.
