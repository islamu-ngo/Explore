<!-- ABOUTME: Actionable task checklist for implementing full input validation and sanitization hardening. -->
<!-- ABOUTME: Breaks the CTO-approved pre-v1 hardening milestone into matrix-first release slices and verification gates. -->

# Full User Input Validation & Sanitization Tasks

Last Updated: 2026-05-28 Europe/Brussels

## Status Legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete
- `[!]` Blocked / needs decision

## Planning / Research Status

- [x] Create persistent dev-doc workstream under `dev/active/full-input-validation-sanitization/`.
- [x] Read repository contract, relevant intents, canonical docs, skills, and rules.
- [x] Collect external validation/sanitization research from Context7, OWASP, Microsoft/source-backed research, and librarian.
- [x] Collect initial Blazor form/input inventory.
- [x] Collect initial backend API/Application validator/request inventory.
- [x] Apply initial Oracle corrections.
- [x] Incorporate CTO feedback into plan structure, gates, release slices, and DoD.
- [x] Collect final input-surface and Blazor-form background agent findings.
- [x] Run second Oracle review on CTO-integrated docs.
- [x] Re-run docs/architecture validation after final updates (`Event.Architecture.Tests`: 179 total, 178 succeeded, 1 skipped, 0 failed).
- [x] Create Slice 1 contract decision log scaffold: `full-input-validation-sanitization-contract-decisions.md`.
- [x] Create Slice 2 input-surface matrix scaffold: `full-input-validation-sanitization-input-matrix.md`.
- [x] Re-run docs/architecture validation after adding Slice 1/2 artifacts (`Event.Architecture.Tests`: 179 total, 178 succeeded, 1 skipped, 0 failed).
- [x] Refresh session handoff in `full-input-validation-sanitization-context.md` with current state, next action, blockers, modified files, validation state, and unrelated dirty-worktree warning.
- [x] Re-run docs/architecture validation after the session-handoff refresh (`Event.Architecture.Tests`: 179 total, 178 succeeded, 1 skipped, 0 failed).
- [x] Re-run handoff validation after context/tasks refresh (`diff --check`: no whitespace errors; `Event.Architecture.Tests`: 179 total, 178 succeeded, 1 skipped, 0 failed).
- [x] Re-ran Context7 research for current ASP.NET Core API/Blazor validation behavior and FluentValidation async/manual validation guidance.
- [x] Locked Slice 1 contract decisions D-001 through D-018 as accepted implementation contracts.
- [x] Expanded Slice 2 matrix with grouped endpoint-family rows using `docs/API_CONTRACT_INVENTORY.md` and current API/BFF/Blazor inventories.
- [!] Session-start Release build is currently red before implementation edits due `Explore.Blazor.Client.Tests/Services/CustomPropertyAdminServiceTests.cs` generated-client anonymous type mismatches.
- [x] Re-ran workstream-doc verification after Slice 1/2 continuation (`git diff --check`: passed; `Event.Architecture.Tests`: 179 total, 178 succeeded, 1 skipped, 0 failed).

## Current Priority for Next Session

1. Continue converting `Pending inventory` matrix rows into concrete implementation tickets/tests, starting with API ProblemDetails normalization, unknown-property rejection, query validators, and idempotency fingerprinting.
2. Start Slice 3/4 implementation only for rows marked `Ready for slice`; leave unresolved `Pending inventory` rows as matrix work.
3. Preserve unrelated dirty files outside `dev/active/full-input-validation-sanitization/`; targeted status shows this workstream directory is untracked and the repository has many unrelated dirty entries.

## Slice 1 — Contract Lock

- [x] Decide whether automatic `[ApiController]` model-state failures are accepted as framework-shaped responses or normalized through a documented response factory.
- [x] Lock expected response shape for malformed JSON.
- [x] Lock expected response shape for missing body.
- [x] Lock expected response shape for wrong `Content-Type` / 415.
- [x] Lock expected response shape for invalid enum/model-binding failures.
- [x] Lock unknown-property behavior and tests.
- [x] Lock over-posted/server-owned field behavior and tests.
- [x] Lock oversized string behavior and max-length error format.
- [x] Lock idempotency behavior for same key + same payload.
- [x] Lock idempotency behavior for same key + different payload.
- [x] Lock idempotency behavior for validation failure replay/caching.
- [x] Lock rich-text default: plain text only unless feature-specific sanitizer decision record exists.
- [x] Lock validation telemetry policy: allowed metadata only, no raw values.

## Slice 2 — Required Input Surface Matrix

Build a remediation matrix with exactly these columns before coding:

- [ ] Route / endpoint / form
- [ ] Auth level
- [ ] Tenant context source
- [ ] DTO / UI model
- [ ] Validator owner
- [ ] Canonicalization owner
- [ ] Rich text?
- [ ] Error contract
- [ ] Tests
- [ ] Abuse cases
- [ ] Threat model
- [ ] Release slice / priority
- [ ] Decision status

Matrix gate:

- [~] Block Slice 3+ implementation until every high-risk row has owner, threat model, release slice/priority, decision status, and test ownership. Current state: individual high-risk rows and grouped endpoint-family rows exist; rows marked `Ready for slice` can start, rows still marked `Pending inventory` need concrete per-controller/per-DTO tickets before broad implementation.

Matrix must include:

- [~] All API controller body/request DTO inputs.
- [~] All API route parameters.
- [~] All public/read `[FromQuery]` inputs.
- [x] Search terms and email-search inputs such as `ContactShareConsentController` query fields.
- [~] Slugs and tenant slugs.
- [x] Pagination/page-size/cursor values.
- [x] Sort fields and sort directions.
- [x] Date ranges and timezone assumptions.
- [x] Lookup IDs and enum values.
- [x] Custom-property filters and projection-backed filters, including `CustomPropertyGovernanceController` and `CustomPropertyProjectionAdminController`.
- [x] Module-specific filters.
- [~] Application commands, queries, request DTOs, and dynamic/custom metadata models.
- [x] BFF unsafe endpoints and browser-facing request models.
- [x] Blazor write forms and UI models.
- [x] Upload/session/proxy inputs.
- [x] Setup/bootstrap/internal exception routes and compensating controls.
- [x] Idempotency-key, correlation, tenant, and cursor inputs.

## Slice 3 — DTO Hardening

- [ ] Convert backend inventory into a row-by-row matrix for every `Explore.Application` command/query/request DTO that accepts user input.
- [ ] Seed the matrix with known high-priority request models:
  - [ ] `CreateEventRequest`
  - [ ] `CreateEventDraftRequestDto`
  - [ ] `UpdateEventDraftRequestDto`
  - [ ] `PublishEventRequestDto`
  - [ ] `CreateEventSessionGroupRequestDto`
  - [ ] `CompleteInstanceOnboardingRequest`
- [ ] Remove over-postable fields from request DTOs.
- [ ] Ensure no entity-shaped write DTOs remain.
- [ ] Split create/update/request DTOs where semantics differ.
- [ ] Standardize field names for generated clients and Blazor `EditContext`.
- [ ] Standardize max lengths.
- [ ] Standardize nullable semantics.
- [ ] Standardize enum handling.
- [ ] Standardize lookup ID semantics.
- [ ] Standardize date/time/date-range semantics.
- [ ] Decide unknown-property behavior.
- [ ] Add OpenAPI examples/format hints where useful.

## Slice 4 — Application Validators and Semantic Checks

- [ ] Ensure each user-input command/query/request DTO has one of:
  - [ ] FluentValidation validator
  - [ ] documented domain-invariant-only validation
  - [ ] documented no-user-input/no-op decision
- [ ] Replace ad hoc validation strings with validators or named application/domain checks where appropriate.
- [ ] Ensure validators are manually instantiated in handlers/services, not injected as `IValidator<T>`.
- [ ] Do not add `AddValidatorsFromAssembly(...)` or a global validation pipeline unless the architecture contract is explicitly changed.
- [ ] Standardize all validation invocation to `ValidateAsync` or `ValidateAndThrowAsync`.
- [ ] Add cross-field validators for date ranges, session capacity, registration windows, enum/lookup consistency, and dependent options.
- [ ] Add public read/query validators for search, slug, pagination, cursor, sort/filter allowlists, date ranges, and custom-property filters.
- [ ] Add or explicitly document no-op validator decisions for `EventFilterRequest` and `EventSessionFilterRequest`.
- [ ] Add endpoint-level negative tests for malformed public GET query inputs beyond happy-path pagination/filter coverage.
- [ ] Ensure custom-property filters verify property/module/filterable/searchable state for the caller tenant.
- [ ] Add validator tests for happy path, edge limits, malicious payloads, Unicode/whitespace/control characters, and localization-safe messages.
- [ ] Add handler tests proving validation runs before persistence side effects.

## Slice 5 — Persistence Semantic Tests and Tenant Leakage

- [ ] Add persistence-aware transactional rechecks for uniqueness, overlap, quota, capacity, tenant/resource constraints, and dynamic custom-property constraints.
- [ ] Add transaction/race-condition tests for high-risk semantic validation.
- [ ] Validate lookup IDs inside caller tenant scope; never disable the `Tenant` filter for runtime validation.
- [ ] Test Tenant B submitting Tenant A IDs for event/session/org/lookups/custom-property references.
- [ ] Ensure invalid cross-tenant IDs return “not found or not accessible” style safe failures.
- [ ] Ensure validation messages do not reveal privileged resource existence.
- [ ] Ensure HAL fail-closed posture is mirrored by validation messages.
- [ ] Run `Event.Persistence.IntegrationTests` when validation touches EF-backed uniqueness, capacity, tenant, or repository behavior.

## Slice 6 — BFF Unsafe Endpoint Audit

- [ ] Inventory and matrix `BffPreferenceEndpoints.cs`.
- [ ] Inventory and matrix `BffSetupSecretEndpoints.cs`.
- [ ] Inventory and matrix `BffStorageEndpoints.cs`.
- [ ] Inventory and matrix `BffAuthEndpoints.cs`.
- [ ] Test unsafe preferences/theme/language/appearance writes without CSRF token.
- [ ] Test setup-secret header spoofing: browser `X-Setup-Secret` stripped, trusted secret resolved server-side.
- [ ] Test setup-secret diagnostics do not leak raw provider/API errors.
- [ ] Test browser-visible state, responses, logs, and storage never contain access tokens, refresh tokens, identity tokens, setup secrets, provider diagnostic payloads, or trusted presigned-upload secrets.
- [ ] Test browser-supplied `Authorization`, token-like, and setup-secret headers are ignored or stripped where applicable.
- [ ] Test server-side token forwarding remains BFF-owned and is not delegated to Blazor client code.
- [ ] Add endpoint-level negative tests for invalid BFF preference values (`theme`, `language`, `direction`), not only normalization service tests.
- [ ] Test upload session reuse / consume-once semantics.
- [ ] Test upload session used by different user.
- [ ] Test content type mismatch.
- [ ] Test arbitrary presigned URL injection is rejected.
- [ ] Test upload destination is never browser-controlled.
- [ ] Validate bootstrap/internal routes without `[Authorize]` have documented compensating controls: setup secret, antiforgery, rate limit, short-lived opaque session, or internal trust boundary.
- [ ] Ensure BFF validation remains local/manual in `Explore.Blazor`; do not couple BFF to Application validators or DI `IValidator<T>`.

## Slice 7 — Blazor Form Convergence

- [ ] Resolve the `docs/UI_GOVERNANCE.md` versus `docs/DESIGN_SYSTEM.md` wrapper-component conflict before broad UI form edits.
- [ ] Confirm client-side validation ownership. UI-local validators may cover only stable syntactic checks: required, max length, simple date ordering, basic URL/email shape.
- [ ] Do not duplicate auth/tenant/capacity/quota/uniqueness/cross-resource/dynamic custom-property/module enablement/persistence rules in Blazor.
- [ ] Do not reference `Explore.Application` directly from `Explore.Blazor.Client` unless creating a shared validation package with explicit dependency/version/bundle-size rules.
- [ ] Align `docs/UI_GOVERNANCE.md` if server-only validation is used for complex cross-resource/persistence/authorization rules.
- [ ] Standardize `CreateEvent.razor(.cs)` on canonical form validation and server error mapping.
- [ ] Standardize `EventEdit.razor(.cs)` on canonical form validation and server error mapping.
- [ ] Standardize `CreateSession.razor` and `EditSession.razor` on canonical form validation and server error mapping.
- [ ] Standardize `CreateOrganization.razor(.cs)` wizard validation and final submit errors.
- [ ] Standardize `OrganizationDetails.razor(.cs)` profile/contact/address edit validation.
- [ ] Standardize `SettingsPersonalInfo.razor` and `SettingsSecurity.razor` validation and status/error announcements.
- [ ] Standardize `CreateApiKeyDialog.razor` validation for name/scopes/expiration and safe secret display.
- [ ] Standardize `EventTemplateEditor.razor` validation for dynamic template fields.
- [ ] Standardize room/day/agenda/aspect editor dialogs on shared form primitives.
- [ ] Standardize `EventReviewDialog.razor` validation for rating/comment fields.
- [ ] Keep `AppTextField` styling-only unless a documented validation abstraction is deliberately introduced.
- [ ] Add bUnit tests for server validation mapping, invalid submit focus behavior, disabled submit state, `role="alert"`, `role="status"`, and dialog focus restore.

## Slice 8 — Canonicalization, Raw Rendering, and Sanitizer Policy

- [ ] Define normalizer ownership outside validators for:
  - [ ] `EmailCanonicalizer`
  - [ ] `SlugNormalizer`
  - [ ] `PhoneNormalizer`
  - [ ] `UrlNormalizer`
  - [ ] `SearchTermNormalizer`
  - [ ] `TenantSlugNormalizer`
  - [ ] `TagNameNormalizer`
- [ ] Add deterministic/idempotent tests for each normalizer.
- [ ] Test Unicode normalization, whitespace, control characters, punctuation, and casing rules.
- [ ] Decide which values preserve original display text alongside canonical comparison keys.
- [ ] Define free-form Unicode text rules for names, titles, descriptions, address lines, organization descriptions, and review/comments.
- [ ] Audit all raw rendering sinks:
  - [ ] `MarkupString`
  - [ ] JS interop that could set `innerHTML`
  - [ ] URL construction
  - [ ] style/CSS injection
  - [ ] markdown/rich text rendering
  - [ ] raw HTML helpers
- [ ] Preserve or test `CommunityGuidelines.razor` escaping behavior.
- [ ] Classify `PublicExperienceHomeBlockKind.RichText`, `PublicExperienceShellDto` home blocks, and `EmailMessage.HtmlBody` as trusted/system-authored, sanitized user-authored, or disallowed.
- [ ] Document `ProjectionStatusSection.razor` as controlled system markup with encoded error text, not user-authored rich text.
- [ ] Default user-authored text to plain text.
- [ ] If user-authored rich HTML is required, create a sanitizer decision record with threat model, allowlist, server-side implementation, and update policy.
- [ ] Add sanitizer regression tests for dangerous tags, attributes, protocols, SVG/math payloads, markdown/raw HTML transforms, and modified sanitized content.

## Slice 9 — OpenAPI and Generated Client Regeneration

- [ ] Regenerate `schemas/openapi.json` after API contract changes.
- [ ] Regenerate `Explore.Blazor.Client/Clients/EventApiClient.g.cs` in a discrete step.
- [ ] Review OpenAPI/generated-client diffs for every breaking DTO change.
- [ ] Update Blazor services to map new generated DTOs without leaking generated clients into Razor components.
- [ ] Update `docs/API_CHANGELOG.md` with breaking validation/DTO changes.

## Slice 10 — CI Gates and Observability Safety

- [ ] Audit validation failure logging for raw user input and sensitive fields.
- [ ] Replace unsafe log values with field names, validator codes, length buckets, categories, tenant/resource identifiers, endpoints, statuses, trace IDs, and correlation IDs.
- [ ] Confirm validation errors preserve traceability through correlation IDs without leaking payloads.
- [ ] Add safe metrics dimensions only; avoid high-cardinality raw input tags.
- [ ] Confirm rate limits and request timeouts protect expensive validation paths.
- [ ] Run `dotnet build --configuration Release --verbosity quiet`.
- [ ] Run `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`.
- [ ] Run `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`.
- [ ] Run `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- [ ] Run `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.
- [ ] Run `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- [ ] Run `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` when semantic validation changes persistence-backed uniqueness, capacity, tenant, or repository behavior.
- [ ] Verify generated OpenAPI/client diffs are intentional and committed separately if required by project workflow.

## Enterprise Acceptance Checklist

- [ ] Every external input has an owner, threat model, validation rule, canonicalization decision, safe error contract, and regression test.
- [ ] No unbounded string on endpoints.
- [ ] No unbounded paging or unvalidated sort/filter.
- [ ] No tenant existence leakage.
- [ ] No entity-shaped write DTOs.
- [ ] No UI local role/claim action availability checks.
- [ ] No `MarkupString`/raw HTML for user-authored content without sanitizer decision record.
- [ ] No validation logs with raw input, secrets, tokens, upload destinations, provider errors, or high-cardinality arbitrary values.
- [ ] Every breaking DTO has OpenAPI/generated-client diff review.
- [ ] `UploadRequestDtoValidator` has dedicated unit coverage or documented equivalent coverage.
- [ ] High-risk validators have malicious payload tests.
- [ ] Persistence-dependent validation has transaction/race-condition tests.
- [ ] Every unsafe BFF endpoint has antiforgery or documented compensating control.
- [ ] BFF token boundary is explicit: no browser-visible tokens, no trusted browser-supplied auth/token headers, and server-side forwarding remains BFF-owned.
- [ ] All validation failures use safe, stable API/BFF error contracts.
- [ ] Blazor forms consistently map server validation errors into `EditContext`.
- [ ] Blazor validation failures are accessible and focus-managed.
- [ ] HAL remains the UI action-affordance source of truth.
