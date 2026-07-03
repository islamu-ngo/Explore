<!-- ABOUTME: Current handoff context for the full input validation and sanitization workstream. -->
<!-- ABOUTME: Captures verified state, research evidence, decisions, next slices, and blockers. -->

# Full Input Validation & Sanitization - Context

Last Updated: 2026-07-03 Europe/Brussels
Status: Re-baselined for implementation
Primary plan: `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-plan.md`
Task list: `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-tasks.md`
Input matrix: `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-input-matrix.md`
Decision log: `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-contract-decisions.md`

---

## 1. Current Handoff

### Completed In This Planning Pass

- Applied the `$senior-cto-feedback` workflow to the active workstream.
- Rewrote the implementation plan as a source-grounded, repo-conformant contract.
- Replaced stale research notes with fresh Tavily MCP and Context7 MCP evidence.
- Corrected the task model so validation and sanitization are distinct concerns.
- Removed the bad task that told the user to regenerate the API client.
- Promoted API, BFF, and Blazor responsibilities into separate implementation lanes.
- Identified `AiAssistantRail.razor` as a new raw-markup review item because it now uses `MarkupString`.

### Completed Implementation Slice - 2026-07-03

- Hardened `CreateStorageUploadSessionDtoValidator` so upload-session reservation metadata rejects malformed or wildcard MIME hints, control characters, path separators, dot segments, reserved Windows device names, and unsafe extension tokens.
- Updated `CreateStorageUploadSessionCommandHandler` so `ContentType` is normalized before both storage policy resolution and persistence.
- Added `CreateStorageUploadSessionDtoValidatorTests` for the new validator contract.
- Added `StorageUploadSessionCommandHandlerTests.CreateHandle_NormalizesContentTypeBeforePolicyResolutionAndPersistence` to prevent policy routing from using raw MIME input.

### Current Recommended Next Slice

Start with storage and upload validation hardening, then raw-rendering review:

1. Continue Phase 2 with direct upload request validation, storage metadata key/value/count limits, object-key validation, and upload-session ownership semantics in Application/API/BFF.
2. Add or extend tests for spoofed content type/content-signature mismatch, invalid object IDs, tenant mismatch, replayed upload session, expired session, wrong user, and unsafe logs.
3. Review `AiAssistantRail.razor` and every other `MarkupString`/rich-content seam.
4. Only after API field keys are stable, continue Blazor form convergence and server-error mapping.

### Blockers

No planning blocker remains.

Known caveats:

- The worktree contains many unrelated changes outside this workstream. Do not revert them.
- `RTK.md` is referenced by the incoming agent context but was not present at repository root during the rebaseline.
- `docs/UI_GOVERNANCE.md` and `docs/DESIGN_SYSTEM.md` have wording tension around form/component wrappers. For this workstream, follow `docs/UI_GOVERNANCE.md`: `EditForm` + `EditContext` + repo form primitives, no `MudForm`.
- `Event.Architecture.Tests` currently fail on unrelated context-manifest drift: intent `update-ai-context-disclosure` references missing `dev/active/ai-context-disclosure-policy/field-classification-matrix.md` and `dev/active/ai-context-disclosure-policy/ai-context-disclosure-policy-plan.md`.

---

## 2. Verified Codebase Reality

This workstream is already partially implemented. Do not restart from older planning assumptions.

| Area | Verified Evidence | Meaning For Future Work |
|---|---|---|
| API validation problem details | `Explore.API/Program.cs` and `Explore.API/ExceptionHandling/ApiValidationProblemDetailsFactory.cs` implement canonical model-state/problem-details behavior. | Extend through tests and request model coverage. Do not duplicate validation response logic in controllers. |
| Unknown JSON member rejection | `Explore.API/Program.cs` configures unmapped JSON member handling. | Keep this strict by default. Any exception must be intentional and documented. |
| Public query validation | `Explore.API/Models/EventFilterRequest.cs`, `EventSessionFilterRequest.cs`, `PaginatedQueryRequests.cs`, and `QueryValidationRules.cs` contain query validation logic. | Audit remaining query surfaces before marking the query lane complete. |
| Idempotency validation | `Explore.API/Middleware/IdempotencyRequestIdentity.cs` and `IdempotencyMiddleware.cs` exist with tests for fingerprint mismatch behavior. | Preserve fingerprint semantics when changing write request contracts. |
| Upload request validation | `Explore.Application/DTOs/StorageObject/Validators/UploadRequestDtoValidator.cs` exists with storage validation tests. `CreateStorageUploadSessionDtoValidator.cs` now covers upload-session MIME syntax, unsafe file/display names, and unsafe extension tokens. `CreateStorageUploadSessionCommandHandler.cs` normalizes MIME before policy resolution and persistence. | Continue into direct upload request review, metadata key/value/count limits, object-key validation, tenant/session ownership, content-signature semantics, and logging safety. |
| Blazor server validation mapping | `Explore.Blazor.Client/Components/Forms/ServerValidationErrorStore.cs` exists. | Use it for API problem-details mapping instead of one-off component code. |
| Blazor form primitives | Multiple forms already use `EditContext`, `FormSubmissionGuard`, `AppValidationSummary`, and server validation stores. | Continue convergence with existing primitives. Do not introduce `MudForm`. |
| BFF boundary tests | Blazor integration tests exist around auth setup, preferences, storage proxy, and YARP security. | Treat BFF work as a residual audit, not a full rewrite. |
| Raw markup | `CommunityGuidelines.razor` was previously reviewed as controlled markup. `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor` now builds prompt-reference highlight markup with `MarkupString`. | Raw rendering classification is not complete. Review and test `AiAssistantRail.razor` first. |
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

Tavily was used successfully on 2026-07-03. Use the following as current external guidance:

- OWASP Input Validation Cheat Sheet: validate all untrusted input with syntactic and semantic checks; prefer allowlists.
- OWASP XSS Prevention Cheat Sheet: output encoding is the primary defense; sanitization is specific to safe HTML handling; global response-side filters are fragile.
- OWASP File Upload Cheat Sheet: treat filenames and `Content-Type` as untrusted; validate extension, MIME, signature, size, storage location, and authorization.
- OWASP Logging Cheat Sheet: validate, sanitize, or encode event data before logging; do not log sensitive raw inputs.
- IETF HTTPAPI Idempotency-Key draft: fingerprint mismatch and concurrent same-key requests require explicit conflict semantics.

### Context7 MCP

Context7 was used successfully on 2026-07-03.

Use these current framework references:

- `/dotnet/aspnetcore.docs` for ASP.NET Core problem details, `InvalidModelStateResponseFactory`, Blazor `EditContext`, `ValidationMessageStore`, antiforgery, and file-upload practices.
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

- `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor`
- `CommunityGuidelines.razor`
- `PublicExperienceHomeBlockKind.RichText`
- `EmailMessage.HtmlBody`

---

## 7. Verification Notes

Use project-scoped tests. Do not run solution-level `dotnet test`.

Latest verified slice on 2026-07-03:

- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/CreateStorageUploadSessionDtoValidatorTests/*" --minimum-expected-tests 1` passed 26 tests.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/StorageUploadSessionCommandHandlerTests/CreateHandle_NormalizesContentTypeBeforePolicyResolutionAndPersistence" --minimum-expected-tests 1` passed 1 test.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet` passed 1,791 tests.
- `dotnet build --configuration Release --verbosity quiet` passed for 25 projects.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet` failed only on the unrelated missing-doc manifest entries listed in Blockers.
- LSP diagnostics and `git diff --check` were clean for the changed Application and test files.

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
- "Strip unknown characters from input without a product contract."
- "Require antiforgery on every unsafe BFF route without acknowledging documented exceptions and compensating controls."

---

## 9. Final Handoff Summary

The active workstream is now suitable for implementation. The plan no longer treats validation and sanitization as one generic activity. It separates:

- API/Application authoritative validation.
- BFF boundary validation and token/antiforgery controls.
- Blazor client UX validation and server-error mapping.
- Sanitization only for approved renderable content.

The next implementation agent should begin with a narrow high-risk slice: storage/upload metadata validation or raw-rendering review. Both have clear security value and concrete test targets.
