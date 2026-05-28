<!-- ABOUTME: Repository-grounded implementation plan for full user input validation and sanitization. -->
<!-- ABOUTME: Defines matrix-first release slices, security gates, and verification policy for pre-v1 validation hardening. -->

# Full User Input Validation & Sanitization Plan

Last Updated: 2026-05-28 Europe/Brussels

## 1. Executive Summary

This workstream is a **pre-v1 platform hardening milestone**, not a broad cleanup pass. Implementation must not begin as one sweeping refactor. First lock the external-input contract, build a remediation matrix, then execute release slices in dependency order: API boundary correctness, DTO/request hardening, authoritative Application validation, persistence semantic tests, BFF unsafe endpoint hardening, Blazor form convergence, raw rendering/sanitizer policy, OpenAPI/client regeneration, and CI gates.

The repository is still in development mode, so backward compatibility is not required for DTO or contract changes. We should break DTOs aggressively where needed: remove over-postable fields, split create/update/request models, stabilize field names for generated clients and Blazor `EditContext`, and document every breaking change through OpenAPI/client diffs and `docs/API_CHANGELOG.md`.

Final target: **every external input has an owner, a threat model, a validation rule, a canonicalization decision, a safe error contract, and a regression test**.

## 2. CTO Feedback Integration and Approval Bar

The CTO verdict is accepted as the readiness bar: strong strategic direction, but not implementation-ready until converted into an executable remediation matrix and contract decisions are locked. The plan now treats broad validation/sanitization as a sequenced enterprise hardening program with gates:

1. Boundary correctness before DTO/UI churn.
2. Inventory matrix before coding.
3. DTO/request cleanup before Blazor convergence.
4. Backend contract stability before UI validators.
5. BFF unsafe endpoint hardening before public release.
6. Rich text disabled by default unless a feature-specific sanitizer decision record exists.
7. No validation observability that logs raw input, secrets, tokens, provider errors, or high-cardinality user values.

## 3. Scope and Contract Classification

No exact `.claude/contract/intents.yaml` intent exists for "full input validation and sanitization." Treat this as a compound/fallback workstream spanning these existing intents:

- `add-write-endpoint` for API write request validation, authorization, idempotency, and ProblemDetails responses.
- `add-cqrs-handler` for Application command/query validators and handler-local validation.
- `openapi-contract-change` for DTO/request schema changes and client regeneration.
- `blazor-component-affordance` for Blazor form behavior, HAL affordance safety, and accessibility.
- `add-hal-link` only if validation changes expose or hide action affordances.

Authoritative rules and skills already reviewed: `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/API.md`, `docs/ARCHITECTURE.md`, `docs/SECURITY-MODEL.md`, `docs/BLAZOR.md`, `docs/UI_GOVERNANCE.md`, `docs/OPERATIONS.md`, `docs/ACCESSIBILITY.md`, `docs/DESIGN_SYSTEM.md`, `docs/CODEBASE_STRUCTURE.md`, `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md`, `.claude/rules/blazor-client.md`, and skills `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`, `blazor-ui-conventions`, `blazor-bff-patterns`, `blazor-css-isolation`, `design-system`, `dotnet-efcore-guidelines`, and `error-tracking`.

## 4. Current State Evidence

### 4.1 Backend and API

- The repository already uses FluentValidation extensively. Backend inventory found 115 `AbstractValidator` files in `Explore.Application`, plus Blazor client validators and startup/options validators in Infrastructure/Secrets.
- The project convention is manual validation, not DI-injected `IValidator<T>` and not a MediatR validation pipeline. `ValidationBehavior` was intentionally removed and `dev/_journal/MAJOR_DECISIONS.md` records "Option A — Manual Validation."
- API error handling already uses chained `IExceptionHandler` implementations: `ValidationExceptionHandler` for validation failures and `GlobalExceptionHandler` for known application/global failures.
- Controllers are `[ApiController]` based and bind request DTOs with `[FromBody]`. No API-layer `AbstractValidator` implementations, custom `ApiBehaviorOptions`, `SuppressModelStateInvalidFilter`, or `InvalidModelStateResponseFactory` were found.
- Direct `ast-grep` found 104 `[FromQuery]` parameters across API/Application surfaces, so public/read query validation is a first-class scope, not an afterthought.
- Final input-surface mapping found no validator files for `Explore.API/Models/EventFilterRequest.cs` or `Explore.API/Models/EventSessionFilterRequest.cs`; public GET filter validation is currently a concrete gap.
- Matrix seed route/query surfaces include `ExternalApiKeyController.GetUsageReport(from,to,tenantId)`, `CustomPropertyGovernanceController` query filters, `CustomPropertyProjectionAdminController` projection/tenant/page inputs, `ContactShareConsentController` email/search/export inputs, `EmailUnsubscribeController` token, `ModuleController` module keys, `FooterController` public/write config inputs, and `TenantController` navigation inputs.
- `IdempotencyMiddleware` validates `Idempotency-Key` length and whitespace, scopes lookup by `(key, tenantId)`, replays stored 200-499 JSON/problem responses, avoids 5xx and oversized-body caching, and logs key hashes. It does not appear to enforce same-key/same-payload fingerprinting yet; that must be explicitly decided and tested.

### 4.2 Blazor UI

The Blazor inventory found three patterns:

1. `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor` uses `DataAnnotationsValidator`, `ValidationSummary`, and `ValidationMessage`.
2. Admin dialogs often use `FluentValidationValidator`.
3. Most event, organization, profile, and review forms use `EditContext`, `AppValidationSummary`, manual checks, and server error mapping.

Shared UI seams already exist and should be reused:

- `Explore.Blazor.Client/Components/Forms/AppValidationSummary.razor`
- `Explore.Blazor.Client/Components/Forms/FormSubmitState.cs`
- `Explore.Blazor.Client/Components/Forms/FormSubmissionGuard.razor`
- `Explore.Blazor.Client/Components/Forms/ServerValidationErrorStore.cs`

No `MudForm`, `InputText`, `HtmlString`, `IHtmlContent`, or `innerHTML` usage was found in the Blazor client inventory. `MarkupString` was found only in `Explore.Blazor.Client/Pages/Legal/CommunityGuidelines.razor`, where HTML is escaped before output. A second controlled markup sink exists in `Explore.Blazor.Client/Pages/Admin/CustomProperties/Components/ProjectionStatusSection.razor`, where system status markup HTML-encodes error text and is not a user-authored rich-text feature.

### 4.3 BFF, Upload, and Idempotency Seams

Known BFF endpoint files to include in the matrix:

- `Explore.Blazor/Endpoints/BffPreferenceEndpoints.cs`
- `Explore.Blazor/Endpoints/BffSetupSecretEndpoints.cs`
- `Explore.Blazor/Endpoints/BffStorageEndpoints.cs`
- `Explore.Blazor/Endpoints/BffAuthEndpoints.cs`

Important existing protections:

- `Explore.Blazor/Program.cs` configures antiforgery header `X-CSRF-TOKEN`, MVC auto antiforgery, `UseAntiforgeryTokenMiddleware`, and `UseAntiforgery`.
- `BffStorageEndpoints.cs` maps `/bff/storage/upload-session` and `/bff/storage/upload-proxy` with authorization.
- `StorageUploadSessionStore` binds upload sessions to owner user id, upload URL, object key, view URL, content type, expiry, and opaque random session id; it validates trusted HTTPS presigned URL shape, content type, owner mismatch, expiry, corrupt payloads, and content-type mismatch.
- `YarpProxyExtensions` strips browser-supplied `X-Setup-Secret` and forwards only the BFF-resolved trusted setup secret.
- Existing tests include `BffPreferenceAntiforgeryTests`, `BffStorageUploadProxyTests`, `StorageUploadSessionStoreTests`, `BffCookieForwardingHandlerTests`, and `BffSecurityTests`.

### 4.4 Sanitization and Encoding

- Existing broad sanitization is limited and targeted. Known examples include analytics payload sanitization in `AnalyticsGovernanceService`, calendar filename sanitization in `EventController`, DOM ID sanitization in `DockElementIds`, SVG label encoding in `ImageHelper`, and email content encoding in registration flows.
- Rich text is already present as explicit data shape, not merely a future possibility: `Explore.Application/Models/PublicExperience/PublicExperienceHomeBlocksConfig.cs` includes `PublicExperienceHomeBlockKind.RichText`, `Explore.Application/DTOs/PublicExperience/PublicExperienceShellDto.cs` exposes home block kinds that default to rich text, and `Explore.Application/Models/EmailMessage.cs` has `HtmlBody`. These must receive sanitizer decision records or be constrained to trusted/system-authored content before public release.
- There is no evidence of a broad HTML sanitizer package or global request sanitizer. This is good: do not introduce a global sanitizer/interceptor because encoding is context-specific and sanitization is only appropriate for explicitly accepted rich HTML.
- OWASP guidance distinguishes validation from canonicalization, output encoding, and rich-HTML sanitization. Input validation is necessary but not the primary XSS defense.

## 5. Required Input Surface Matrix

Implementation may not begin until this matrix exists and high-risk rows are prioritized. The working matrix lives in [`full-input-validation-sanitization-input-matrix.md`](full-input-validation-sanitization-input-matrix.md), and Slice 1 decisions live in [`full-input-validation-sanitization-contract-decisions.md`](full-input-validation-sanitization-contract-decisions.md). Columns are mandatory; the extra `Threat model`, `Release slice / priority`, and `Decision status` columns make the CTO gate enforceable rather than hidden inside prose:

| Route / endpoint / form | Auth level | Tenant context source | DTO / UI model | Validator owner | Canonicalization owner | Rich text? | Error contract | Tests | Abuse cases | Threat model | Release slice / priority | Decision status |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Example only: `POST /api/events` | `[Authorize]` | `ITenantContext` after tenant middleware | `CreateEventRequest` | Application validator in handler | Event normalizer / handler | No | RFC7807 validation ProblemDetails | Unit + API negative contract | overlong title, tenant B lookup id, malformed date | over-posting, tenant oracle, expensive validation | Slice 3/4 — High | Pending contract lock |

The matrix must cover:

- API controllers and route/query/body inputs, seeded from `ApiContractInventoryGeneratorTests` and `EndpointAuthorizationMatrixTests`.
- Public anonymous GET/query/filter inputs, especially `EventFilterRequest` and `EventSessionFilterRequest` because no validators were found for them.
- Application commands, queries, request DTOs, and dynamic/custom-property filter models.
- BFF endpoints and browser-facing state-changing routes.
- Blazor write forms and UI-local models.
- Upload/session/proxy inputs.
- Setup/bootstrap/internal exception routes with compensating controls.
- Idempotency-key, correlation, tenant, and cursor inputs.

## 6. Target State Policies

### 6.1 Validation Ownership

- API controllers stay thin: model binding, route metadata, MediatR dispatch, HAL assembly, and `ActionResult` only.
- Application handlers/services perform authoritative validation by manually instantiating FluentValidation validators and calling async validation APIs.
- Domain entities enforce domain invariants that must never depend on UI/API validation.
- Blazor client validation improves UX and accessibility but is never trusted as an authorization, tenant, quota, capacity, uniqueness, or persistence boundary.
- BFF endpoints validate BFF-only request models locally in `Explore.Blazor` endpoint/service seams. Do not couple BFF to `Explore.Application` validators or DI-injected `IValidator<T>` registration.

### 6.2 Public Read / Query Validation

Public or anonymous GET input can still be an attack surface: expensive queries, data leakage, telemetry cardinality explosions, confusing cache behavior, and inconsistent sorting/filtering. The matrix and implementation must cover:

- Search terms and global text filters.
- Slugs and tenant slugs.
- Pagination, page size, and cursor values.
- Sort fields and sort direction allowlists.
- Date ranges and timezone assumptions.
- Lookup IDs and enum values.
- Custom-property filters, module-specific filters, and projection-backed filters.
- Output-cache and ETag interactions for rejected query values.

Rules:

- Page size must be bounded.
- Sort/filter fields must be allowlisted.
- Search terms must be normalized and length-bucketed for telemetry.
- Custom-property filters must verify the property is enabled/filterable/searchable for the caller tenant and module.
- Invalid filters should return safe field-level errors without revealing cross-tenant existence.

### 6.3 Tenant-Leakage Prevention

Validation must not become an oracle for cross-tenant or unauthorized resource existence.

- Validate lookup IDs inside caller tenant scope using tenant filters; do not disable the `Tenant` filter for runtime validation.
- Prefer “not found or not accessible” for cross-tenant IDs and unauthorized resources.
- Do not reveal that Tenant A’s ID exists when submitted by Tenant B.
- Extend fail-closed HAL posture to validation messages: if the user cannot act on or see a resource, validation should not reveal privileged detail about it.
- Add tests where Tenant B submits Tenant A IDs for lookups, event/session references, organization references, custom-property references, and idempotency keys.

### 6.4 Validation API Standard

- Prefer `ValidateAsync(...)` everywhere because FluentValidation async rules require it and synchronous invocation can throw when async rules are later added.
- Validation failures flow through `ValidationExceptionHandler` into RFC 7807 ProblemDetails with stable field keys and safe messages.
- Automatic `[ApiController]` model-state failures, malformed JSON, missing bodies, content-type mismatches, and invalid enum/model-binding failures must be tested and either normalized through a documented `InvalidModelStateResponseFactory` decision or accepted with a documented framework response shape.
- Negative contract tests must cover invalid enum, unknown property behavior, missing required property, malformed JSON, missing body, wrong content type, max length, invalid date range, invalid lookup ID with no cross-tenant leak, over-posted fields, and oversized strings.
- Validators use allowlists, type/range/length checks, enum checks, anchored regexes, and semantic validation where needed.
- Validators avoid ReDoS-prone regexes and avoid over-validating free-form Unicode text in ways that exclude legitimate names/languages.
- Persistence-dependent rules are rechecked transactionally in handlers/repositories, not only in validators.

### 6.5 DTO and Over-Posting Policy

Pre-v1 DTO changes should favor explicitness over compatibility:

- Do not use entity-shaped write DTOs.
- Split create/update/request DTOs where semantics differ.
- Remove server-owned fields from request bodies.
- Standardize max lengths, nullability, enum handling, lookup ID semantics, date/time semantics, and field names.
- Keep generated client and Blazor `EditContext` field names stable after contract lock.
- Review OpenAPI and generated-client diffs for every breaking DTO change.

### 6.6 Blazor Form Standard

All user-input forms should converge only **after backend contract lock**:

- Use `EditForm` + `EditContext`.
- Use `AppValidationSummary`, `ServerValidationErrorStore`, `FormSubmitState`, and `FormSubmissionGuard`.
- Use UI-local validators only for stable syntactic checks: required, max length, simple date ordering, basic URL/email shape.
- Do not duplicate complex Application rules in Blazor: auth, tenant scope, capacity, quota, uniqueness, cross-resource checks, dynamic custom-property rules, module enablement, and persistence-dependent rules remain server-only.
- Do not reference `Explore.Application` validators from `Explore.Blazor.Client` unless a deliberate shared validation package is created with dependency, version, and WASM bundle-size rules.
- Server validation errors must map into `EditContext`.
- Accessibility behavior from `docs/ACCESSIBILITY.md` is required: labels, `role="alert"`, `role="status"`, focus first invalid field or summary, and dialog focus restore.
- Resolve `docs/UI_GOVERNANCE.md` versus `docs/DESIGN_SYSTEM.md` wrapper-component conflict before broad form refactors.

### 6.7 Canonicalization Policy

Validators validate; they do not mutate DTOs. Canonicalization belongs in handlers, domain services, factories, or dedicated normalizers with idempotency tests.

Candidate normalizers:

- `EmailCanonicalizer`
- `SlugNormalizer`
- `PhoneNormalizer`
- `UrlNormalizer`
- `SearchTermNormalizer`
- `TenantSlugNormalizer`
- `TagNameNormalizer`

Rules:

- Normalization must be deterministic and idempotent.
- Tests must cover Unicode normalization, whitespace, control characters, punctuation, and case where applicable.
- Preserve display values separately from canonical keys when business semantics require fidelity. Example: store `DisplayName` after safe trimming, but use a separate normalized comparison/search key.
- Idempotency-key logic must define whether canonicalized payload fingerprints are required for same-key/same-payload replay and same-key/different-payload rejection.

### 6.8 Rich Text and Raw Rendering Policy

Default all user-authored text to plain text. Rich HTML is unsupported unless a feature explicitly opts in with:

- Approved sanitizer profile.
- Threat model.
- Server-side sanitizer implementation and patch/update policy.
- Regression tests for dangerous tags, attributes, protocols, SVG/math payloads, markdown/raw HTML transforms, and modified sanitized content.
- Release gate and documentation update.

No casual `MarkupString`, markdown rendering, JavaScript `innerHTML`, raw attributes, or raw HTML helpers for user-authored content. Existing `PublicExperienceHomeBlockKind.RichText`, `PublicExperienceShellDto` home blocks, and `EmailMessage.HtmlBody` must be classified as trusted/system-authored, sanitized user-authored, or disallowed before release.

### 6.9 BFF Hard Gates

BFF validation moves earlier in the release order because browser-facing cookie-auth routes are security boundaries.

Required tests/decisions:

- Browser never receives access, refresh, identity, setup-secret, presigned-upload, or provider diagnostic tokens in serialized state, logs, responses, generated client payloads, or Blazor storage.
- Browser-supplied `Authorization`, token-like, and setup-secret headers are ignored or stripped where applicable; server-side token forwarding remains BFF-owned.
- Unsafe preferences/theme/language/appearance writes without CSRF token.
- Setup-secret header spoofing: browser `X-Setup-Secret` stripped and trusted secret resolved server-side.
- Upload session reuse / consume-once semantics.
- Upload session used by different user.
- Content type mismatch.
- Arbitrary presigned URL injection.
- Upload destination not browser-controlled.
- Raw IdP/provider error leakage prevention.
- Bootstrap/internal routes without `[Authorize]` have documented compensating controls such as setup secret, antiforgery, rate limiting, short-lived opaque session, or internal trust boundary.

### 6.10 Idempotency as Validation Hardening

`Idempotency-Key` is part of external input validation:

- Same key + same payload: replay cached response where policy allows.
- Same key + different payload: reject or otherwise explicitly document behavior.
- Validation failure replay: decide whether 400 validation failures are cached/replayed; current middleware can persist 200-499 JSON/problem responses under size/content-type constraints, so tests must lock expected behavior.
- Tenant boundary: idempotency cache remains scoped by tenant and must not replay across tenants.
- Oversized ProblemDetails are not cached.
- 5xx responses are not cached.
- Keys must not contain sensitive values; logs use hash/category only.

### 6.11 Observability Safe by Design

Validation telemetry may include:

- Field name.
- Validator code/category.
- Length bucket.
- Endpoint/route.
- Status code.
- Tenant/resource identifier when already authorized and safe.
- Trace ID and correlation ID.

Validation telemetry must not include raw input values, request bodies, secrets, tokens, setup secrets, raw provider/IdP errors, upload destinations, raw search terms, raw custom-property values, or high-cardinality arbitrary labels unless an audited safe constant.

## 7. Enterprise Release Slices

### Slice 1 — Contract Lock

Lock the intended API/BFF validation error contract before DTO or UI churn.

- Decide whether automatic `[ApiController]` failures are normalized or documented as framework-shaped responses.
- Define accepted behavior for malformed JSON, invalid enum, missing body, wrong content type, unknown property, missing required property, oversized strings, and over-posted fields.
- Define idempotency mismatch/replay policy.
- Define rich text as disabled by default.

### Slice 2 — Input Surface Matrix

Build the required matrix and block implementation until high-risk rows have owners, threat model, tests, and release slice assignment.

Gate: Slice 3 cannot begin until every high-risk matrix row has a decision status (`accepted`, `needs design`, `blocked`, or `deferred with rationale`) and an owner for validation, canonicalization, error contract, and tests.

### Slice 3 — DTO Hardening

Break DTOs pre-v1, remove over-posting, split create/update/request models, stabilize field keys, and update OpenAPI expectations.

### Slice 4 — Application Validators

Complete authoritative manual FluentValidation and named semantic checks in handlers/services. Use `ValidateAsync`, keep repos entity-first, keep validators out of DI, and test before persistence side effects.

### Slice 5 — Persistence Semantic Tests

Add persistence integration and race/concurrency tests for tenant scope, uniqueness, capacity, overlap, quota, dynamic custom properties, and repository-backed semantic validation.

### Slice 6 — BFF Unsafe Endpoint Audit

Harden preferences, setup-secret, storage/upload, auth diagnostics, token forwarding, antiforgery, setup-secret header stripping, and upload-session contracts.

### Slice 7 — Blazor Form Convergence

After backend contract lock, converge forms on `EditForm`/`EditContext`/shared primitives and map server validation errors into `EditContext`. Keep UI-local validators syntactic only.

### Slice 8 — Raw Rendering / Sanitizer Policy

Audit raw rendering sinks, default to plain text, and require opt-in sanitizer decision records for any rich text.

### Slice 9 — OpenAPI and Client Regeneration

Regenerate `schemas/openapi.json`, regenerate `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, review diffs, and update `docs/API_CHANGELOG.md`.

### Slice 10 — CI Gates

Run the full implementation verification suite, docs/architecture gates, and targeted negative contract/security tests.

## 8. Validation and Test Strategy

For this planning-only output, validate documentation structure and links with:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Implementation verification should include:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
```

Add targeted suites for:

- API negative contract behavior: invalid enum, unknown property behavior, missing required property, malformed JSON, wrong content type, overlong strings, invalid lookup IDs, and over-posting.
- Public read/query validation: pagination bounds, sort/filter allowlists, date ranges, cursor format, search term normalization, custom-property filters.
- Tenant-leakage prevention: Tenant B submitting Tenant A IDs should not reveal existence.
- Idempotency: same key/same payload, same key/different payload, validation failure replay, tenant boundary, oversized ProblemDetails not cached, 5xx not cached.
- BFF hard gates: CSRF, setup-secret spoofing, invalid preference values at endpoint level, upload session reuse, owner mismatch, content-type mismatch, arbitrary presigned URL injection, raw IdP error leakage.
- BFF token-boundary gates: browser receives no tokens, browser-supplied auth/token headers are stripped or ignored, server-side token forwarding stays BFF-owned.
- Blazor: server validation mapping into `EditContext`, invalid submit focus, live region/status behavior, and no role/claim local action gating.
- Raw rendering/rich HTML/XSS payload regressions.
- Observability: no raw values in validation logs/metrics.

## 9. Research Evidence

- Tavily MCP research was attempted as requested but failed with usage limit `status:432`. This plan compensates with repository docs, Context7 FluentValidation/MudBlazor docs, official OWASP cheat sheets, Microsoft guidance, and librarian-agent research.
- Context7 FluentValidation evidence: manual validation is the recommended path for async rules; `ValidateAsync` is required when async rules exist; automatic validation packages do not fit this repository's manual validator rule.
- Microsoft ASP.NET Core Web API guidance: `[ApiController]` automatic model validation uses `ValidationProblemDetails`; unsupported content type and malformed input need distinct contract tests.
- Microsoft Blazor forms validation guidance: Blazor validation is user feedback; server validation remains authoritative and field errors can be mapped back into form state.
- Microsoft antiforgery and file-upload guidance: browser cookie-auth unsafe routes need antiforgery; uploads require server-side size/type/name/path validation.
- IETF Idempotency-Key draft and Stripe docs support same-key replay semantics, payload mismatch decisions, random keys, and avoiding validation-only failure caching unless explicitly designed.
- OWASP Input Validation Cheat Sheet: server-side allowlist validation, syntactic plus semantic checks, anchored safe regex, Unicode normalization, upload validation, client-side validation as UX only.
- OWASP XSS Prevention Cheat Sheet and Microsoft XSS guidance: validation is not primary XSS defense; use context-aware output encoding; sanitize only user-authored rich HTML; avoid raw HTML escape hatches for untrusted content.
- Microsoft HTTP logging guidance: request/response logging can capture PII; validation telemetry should use redaction and structured metadata only.

## 10. Risks and Open Questions

- Backend inventory found widespread Application validators but did not produce a complete row-by-row remediation matrix. Slice 2 must produce it before implementation starts.
- There is no global API model-state response customization. If field-key normalization requires changing automatic `[ApiController]` 400s, design that deliberately rather than accidentally suppressing framework behavior.
- Public read/query validation is broad: direct `ast-grep` found 104 `[FromQuery]` parameters, query/filter specifications include custom-property, projection-backed, sort, date, search, and pagination inputs, and final mapping found no validators for `EventFilterRequest` or `EventSessionFilterRequest`.
- Server-only Blazor validation needs canonical doc alignment because `docs/UI_GOVERNANCE.md` expects `EditForm` + `EditContext` + FluentValidation. Either keep lightweight UI-local validators for syntax checks or update UI governance to explicitly allow server-only complex rules.
- `Explore.Blazor.Client` previously had a FluentValidation version/reference conflict with `Explore.Application`; do not reference Application validators directly from WASM without resolving package version and bundle-size concerns.
- Some validations are business decisions: Unicode categories, name punctuation, free-form text length, phone normalization, rich HTML acceptance, unknown-property behavior, idempotency mismatch behavior, and validation-failure replay policy.
- Sanitization library selection is intentionally undecided. If rich HTML is required, choose a maintained sanitizer and document its update/patch policy.
- Doc conflict exists between `docs/UI_GOVERNANCE.md` and `docs/DESIGN_SYSTEM.md` around primitive wrappers. Resolve before broad form refactors.

## 11. Enterprise Definition of Done

- No unbounded strings on external endpoints.
- No unbounded paging, unvalidated sort fields, or unvalidated filters.
- No tenant existence leakage through validation messages, lookup checks, HAL affordances, idempotency replay, or query filters.
- No entity-shaped write DTOs or server-owned fields in request bodies.
- No UI local role/claim action availability checks; HAL `_links` and server authorization remain authoritative.
- No `MarkupString`, markdown rendering, raw HTML helper, or `innerHTML` path for user-authored content without sanitizer decision record and regression tests.
- No validation logs/metrics with raw input, secrets, tokens, setup secrets, upload destinations, provider errors, or high-cardinality arbitrary values.
- Every breaking DTO change has OpenAPI/generated-client diff review and `docs/API_CHANGELOG.md` coverage.
- High-risk validators have malicious payload tests.
- Persistence-dependent validation has transaction/race-condition tests.
- Every unsafe BFF endpoint has antiforgery or documented compensating control.
- Browser-visible state, responses, logs, and storage contain no access/refresh/identity/setup-secret/provider diagnostic tokens; browser-supplied auth/token headers are stripped or ignored at BFF/proxy seams.
- API error payloads are safe, RFC 7807-shaped where applicable, correlated, documented, and tested.
- Architecture, Application, API integration, Blazor client/integration, persistence integration where applicable, and docs/architecture gates pass.
