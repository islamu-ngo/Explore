<!-- ABOUTME: Operational context for resuming the local-first file storage workstream. -->
<!-- ABOUTME: Records CTO re-baseline decisions, current risks, validation, and next implementation slice. -->

# Local-First File Storage - Context

Last Updated: 2026-05-29 Europe/Brussels

## SESSION PROGRESS (2026-05-29 Europe/Brussels)

### COMPLETED

- Re-baselined the existing implementation plan through `senior-cto-feedback`.
- Re-read AGENTS contract, quick reference, governance, operations, `/dev-docs` workflow, CTO-review resources, path rules, related skills, storage docs, security/BFF/API/multi-tenancy docs, and current storage source files.
- Verified current code reality for S3-shaped contracts, BFF upload proxy, admin UI placeholders, storage metadata, API key endpoints, tests, Compose optional MinIO profile, and generated-client/API contract references.
- Ran baseline build: `dotnet build --configuration Release --verbosity quiet` passed with existing package/deprecation warnings.
- Added required two-line `ABOUTME:` comments to all three workstream docs.
- Rewrote plan/tasks around a CTO-required PR split instead of one broad implementation stream.
- Ran focused context verification: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed after doc updates.
- Implemented PR 1 storage foundation:
  - provider constants, object visibility/purpose/lifecycle states, and upload session states;
  - local-default storage settings with `long` byte/quota values;
  - expanded `StorageObject` metadata, soft delete, quarantine/delete lifecycle, and optimistic concurrency;
  - new `StorageUploadSession` and `StorageUsageCounter` domain entities;
  - EF configurations, query filters, repositories, DI registration, migration, and schema documentation;
  - DTO/validator/mapping updates for provider-neutral storage metadata.
- Generated `AddLocalFirstStorageFoundation` migration using local environment Postgres variables after Infisical design-time bootstrap was unavailable in the restricted network.
- Added domain tests for storage object lifecycle, upload session transitions, storage usage counters, and setting registry coverage.
- Used Context7 ASP.NET Core documentation for current upload/download guidance:
  - generate trusted server-side object keys;
  - stream to storage instead of buffering entire uploads;
  - enforce size limits while reading streams;
  - never trust browser filenames;
  - keep local storage outside static web roots;
  - expose range processing at the API boundary, not by leaking filesystem paths.
- Implemented PR 2 local provider foundation:
  - provider-neutral Application contracts in `IFileStorageProvider` and `IFileStorageProviderResolver`;
  - non-CQRS storage payload models named `FileStorage*Input`, `FileStorage*Result`, and `FileStorageProviderStatus`;
  - deployment-managed `Storage:Local` infrastructure options and validator;
  - provider resolver registration;
  - local filesystem provider with tenant-scoped UUIDv7 object keys, temp-to-final writes, streaming SHA-256, byte-limit/expected-size enforcement, path traversal containment checks, range-compatible read streams, idempotent delete, and writable-root self-test;
  - focused infrastructure tests for options validation, writes, reads, deletes, traversal rejection, checksums, and provider status.
- Used Context7 AWS SDK for .NET documentation for S3 adapter guidance:
  - `PutObjectAsync` with stream input and content type;
  - `GetObjectAsync` returning `ResponseStream` and response headers;
  - `DeleteObjectAsync` for provider delete requests;
  - `HeadBucketAsync`/bucket checks for provider status.
- Implemented PR 2 optional S3 provider foundation:
  - `IS3ClientFactory` / `S3ClientFactory` centralizes S3 endpoint normalization and SDK client creation for data and presign endpoints;
  - legacy `ObjectStorageService` now uses the shared factory but remains the legacy presigned URL service;
  - `S3FileStorageProvider` implements `IFileStorageProvider` with server-generated tenant keys, streaming upload via `PutObjectAsync`, SHA-256 calculation while the SDK reads, expected/max byte enforcement, read/delete operations, and `s3_not_configured` / `s3_unavailable` status results;
  - DI registers both local and S3 providers, with a scoped provider resolver so tenant-scoped S3 resolution is safe;
  - focused S3 provider tests avoid external network access by substituting `IAmazonS3`.
- Implemented PR 2 effective storage policy resolver:
  - `IStoragePolicyResolver` exposes effective policy resolution and provider lookup;
  - `ResolvedStoragePolicy` carries tenant id, normalized provider, effective max upload bytes, tenant quota bytes, instance max upload ceiling, delegation lock state, override allowance, and setting sources;
  - `StoragePolicyResolver` resolves instance policy first, then applies tenant overrides only when single-tenant mode or unlocked multi-tenant storage delegation allows it;
  - unsupported runtime providers such as `legacy_external` normalize back to local for new writes;
  - tenant max-upload settings are capped by the instance ceiling before upload flows can use them;
  - unit tests cover local/S3, locked and unlocked multi-tenant, single-tenant override, invalid provider fallback, and provider lookup.
- Implemented PR 3.1 Application upload-session commands and validators:
  - `CreateStorageUploadSessionDto` and `StorageUploadSessionDto` expose provider-neutral upload-session inputs and policy/usage context without provider-specific destination data;
  - `CreateStorageUploadSessionDtoValidator` is manually instantiated and validates expected byte count, content type shape, safe filename/display/extension metadata, purpose/visibility values, owning resource pairing, and required idempotency key;
  - `CreateStorageUploadSessionCommand` and `CancelStorageUploadSessionCommand` use resource authorization attributes with tenant/resource attributes for the API layer to populate;
  - `CreateStorageUploadSessionCommandHandler` resolves tenant and effective storage policy, rejects uploads above the effective max before opening a transaction, enforces tenant quota reservations inside `IUnitOfWork.ExecuteInTransactionAsync`, creates upload sessions with a 15-minute expiry, and returns an existing tenant/idempotency session without reserving bytes twice;
  - `CancelStorageUploadSessionCommandHandler` releases reserved bytes for active or expired sessions, marks sessions canceled/expired, treats already terminal canceled/expired/failed states as idempotent no-ops, and rejects finalized sessions without releasing quota;
  - `IStorageUploadSessionRepository` now has a tenant/idempotency tracked lookup for retry-safe command handling.
- Implemented PR 3.2 API upload-session and byte-stream endpoints:
  - `StorageObjectController` now exposes authenticated provider-neutral upload-session routes for create, content upload/finalization, and cancel, all using stable `RouteNames`, endpoint classification, write rate limiting, request timeout metadata, and explicit response metadata;
  - the create/cancel endpoints set `TenantId` from API tenant context before dispatching the Application command so MediatR authorization and handlers stay tenant-bound;
  - the content endpoint streams `Request.Body` into `FinalizeStorageUploadSessionCommand` with request content type and content length instead of accepting browser-selected provider destinations, object keys, or local paths;
  - `FinalizeStorageUploadSessionCommandHandler` validates tenant/session state, expiry, expected size, and content type, marks the reserved session uploading before provider IO, writes via the server-selected `IFileStorageProvider`, creates `StorageObject` metadata, finalizes reserved usage, and maps provider failures to failed-session plus reservation release;
  - `StorageUploadProblemDetails` maps storage failure codes to stable RFC 7807 responses including `413`, `422`, `409`, `404`, `400`, and `503`;
  - `RequestTimeoutExtensions` now registers the named default timeout policy as well as `options.DefaultPolicy`, fixing anonymous smoke 500s for endpoints using `RequestTimeoutExtensions.DefaultPolicy`;
  - API tests cover controller dispatch, route metadata, timeout/rate-limit policy metadata, and ProblemDetails mapping.
- Implemented PR 3.3 metadata-driven download/public image handlers:
  - added `IStorageObjectContentReader` and `StorageObjectContentReader` to centralize lifecycle, visibility, and owner checks before any provider stream is opened;
  - added `GetStorageObjectContentRequest` and handler so API downloads resolve by stable `StorageObject.Id`, not browser-supplied provider object keys;
  - updated `GetPublicImageRequestHandler` to use the same content reader with `publicImagesOnly: true`, preventing private/authenticated tenant files from being served by the public image URL;
  - added `GET /api/storageobject/{id:guid}/content` with `RouteNames.GetStorageObjectContent`, `FileStreamResult.EnableRangeProcessing`, `LastModified`, and checksum-backed ETag metadata;
  - retained arbitrary-key legacy endpoints for PR 3.4 hardening/removal rather than mixing route removal into PR 3.3.
- Implemented PR 3.4 arbitrary-key route removal:
  - removed public `GET /api/storageobject/file/{*fileKey}` and `GET /api/storageobject/presigned-url-by-key/{*objectKey}` from `StorageObjectController`;
  - removed their route-name constants and MediatR request/handler pairs so local storage cannot be read or presigned from browser-supplied provider keys;
  - added a focused controller route-surface test that fails if those templates or route names are reintroduced;
  - updated `docs/API_CHANGELOG.md` and `docs/STORAGE.md` to make the breaking storage contract change explicit.
- Implemented PR 3.5 BFF storage upload/proxy refactor:
  - `/bff/storage/upload-session` now accepts provider-neutral file metadata plus expected byte count, calls `api/storageobject/upload-sessions`, and returns only an opaque BFF upload session id;
  - `StorageUploadSessionStore` now persists owner, API upload-session id, content type, expected size, and expiry instead of presigned upload URLs, object keys, or view URLs;
  - `/bff/storage/upload-proxy` rejects raw upload destinations, validates the caller-owned BFF session, requires exact file size/content type match, and streams bytes to `api/storageobject/upload-sessions/{id}/content`;
  - successful browser proxy uploads now return metadata-backed `/api/storageobject/{id}/public` and `/content` URLs from the finalized API session response;
  - Blazor image upload clients pass expected file size into the BFF session request, use BFF proxy upload results directly in browser flows, and no longer call removed arbitrary-key presigned download routes.

### IN PROGRESS

- OpenAPI/client regeneration is still not implemented.

### NEXT

1. Continue with PR 3.6 OpenAPI and NSwag generated-client regeneration after the storage contract is stable.
2. Re-read target source files before editing because this repo has an active dirty worktree.
3. Resolve or work around unrelated generated-client test compile errors before running the Blazor client test project.
4. After each slice, update plan/context/tasks before reporting completion.

### BLOCKERS

- Persistence integration verification currently fails in two email-dispatch transition tests unrelated to this storage slice:
  - `TryParkForOperatorMarksEligibleRowAsParked` expected `Parked`, got `DeadLettered`.
  - `TryReplayForOperatorResetsDeferredRowToPending` expected `Pending`, got `DeadLettered`.
- The repo had unrelated email-dispatch worktree changes before this storage implementation; do not patch that area as part of storage unless explicitly requested.
- Full API integration suite is currently not a clean PR 3.3 signal because real-Postgres lanes fail on pending EF model changes/migration drift in the active worktree and one authorization matrix path returned `504 GatewayTimeout`. Focused storage API tests and anonymous-protected smoke passed after rebuilding the test host.
- Focused PR 3.4 storage route tests passed after removing arbitrary-key API routes; full API integration suite was not rerun because the broader suite remains noisy for the unrelated reasons above.
- Full Architecture tests are currently blocked by unrelated active-worktree issues:
  - `AuthorizationParityTests.AllLinkPoliciesHaveExplicitPermissionActions` reports multiline `RequirePermission(...)` calls in `ActorLinkPolicy.cs` and `EventLinkPolicy.cs`.
  - `CqrsPatternTests.Queries_ShouldResideIn_QueriesNamespace` reports a non-query `*Request` type from the active AI integration work (`AiChatRequest`) outside a Queries namespace.
- Full root Release build is currently blocked by unrelated active-worktree compile errors:
  - `Event.Application.UnitTests/Features/AiAssistant/Queries/GetAiAssistantBootstrapQueryHandlerTests.cs` references `SettingSource.System` and an old `ResolvedSetting` constructor.
  - `Explore.Blazor.Client.Tests/Services/CustomPropertyAdminServiceTests.cs` has generated-client anonymous type mismatches.
- `Explore.Blazor.Client.Tests` project build is currently blocked by the same unrelated `CustomPropertyAdminServiceTests` generated-client anonymous type mismatches, so focused client service tests for the BFF upload client could not be executed through that test project in this slice.
- `git diff --check` currently fails in unrelated generated client whitespace at `Explore.Blazor.Client/Clients/EventApiClient.g.cs` lines 5610, 6944, 7941, 8024, 8111, 8197, 37567, 37653, and 38734. That generated file was not part of the PR 3.5 BFF upload proxy changes.

## Quick Resume

1. Read `dev/active/local-first-file-storage/local-first-file-storage-plan.md`.
2. Read `dev/active/local-first-file-storage/local-first-file-storage-tasks.md`.
3. Review the PR 1 files/migration if continuing the same slice.
4. Start with PR 3.5 BFF storage upload/proxy refactor.
5. Keep all three dev docs current after every meaningful implementation slice.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `Explore.Domain/StorageObject.cs` | Existing | Domain | Tenant-scoped storage metadata. | Needs provider, object key, content type, checksum, visibility/purpose, lifecycle/delete state, soft-delete/quarantine decision. |
| `Explore.Persistence/Configurations/Entities/StorageObjectConfiguration.cs` | Existing | Persistence | EF mapping for storage metadata. | Needs indexes/constraints for tenant/provider/visibility/lifecycle queries. |
| `Explore.Application/Contracts/Infrastructure/IObjectStorageService.cs` | Existing | Application | Current S3-shaped contract. | Replace or isolate behind S3-only adapter. |
| `Explore.Application/Contracts/Infrastructure/IFileStorageProvider.cs` | New | Application | Provider-neutral storage contract. | Uses `FileStorage*Input` payloads to avoid CQRS `*Request` convention collisions. |
| `Explore.Application/Contracts/Infrastructure/IFileStorageProviderResolver.cs` | New | Application | Resolves registered providers by provider id. | This is not yet the effective tenant policy resolver. |
| `Explore.Application/Contracts/Infrastructure/IStoragePolicyResolver.cs` | New | Application | Resolves effective storage policy and selected provider. | Applies storage delegation lock and instance upload ceiling before provider use. |
| `Explore.Application/Models/Storage/FileStorage*` | New | Application | Provider-neutral storage IO/status payloads. | No AWS/S3 concrete types. |
| `Explore.Application/Models/Storage/ResolvedStoragePolicy.cs` | New | Application | Effective provider/quota/upload policy. | Used by future upload-session commands. |
| `Explore.Application/Services/StoragePolicyResolver.cs` | New | Application | Storage policy resolution service. | Local fallback for unsupported write providers; tenant overrides only when allowed. |
| `Explore.Application/Features/StorageObjects/Handlers/Commands/CreateStorageUploadSessionCommandHandler.cs` | New | Application | Creates upload reservations and sessions. | Resolves policy, enforces max upload/quota, and uses tenant/idempotency to avoid double reservation under transaction retries. |
| `Explore.Application/Features/StorageObjects/Handlers/Commands/CancelStorageUploadSessionCommandHandler.cs` | New | Application | Cancels or expires reserved upload sessions. | Releases quota reservations for non-finalized sessions and rejects finalized sessions. |
| `Explore.Application/Features/StorageObjects/Handlers/Commands/FinalizeStorageUploadSessionCommandHandler.cs` | New | Application | Streams reserved upload-session bytes to the selected provider and finalizes metadata. | Keeps provider IO out of long DB transactions while releasing reservations on provider failure. |
| `Explore.Application/DTOs/StorageObject/Validators/CreateStorageUploadSessionDtoValidator.cs` | New | Application | Validates upload session intent. | Manual validator; rejects unsafe metadata and missing idempotency keys before handler state changes. |
| `Explore.API/Controllers/StorageUploadProblemDetails.cs` | New | API | Maps upload-session command failures to RFC 7807. | Keeps HTTP status/problem mapping out of Application handlers. |
| `Explore.Infrastructure/Storage/LocalFileStorageProvider.cs` | New | Infrastructure | Local filesystem storage provider. | Streams bytes, generates object keys, hashes content, and blocks path traversal. |
| `Explore.Infrastructure/Storage/LocalFileStorageOptions.cs` | New | Infrastructure | Deployment-managed local root configuration. | Bound from `Storage:Local`, not tenant/admin settings. |
| `Explore.Infrastructure/Storage/S3FileStorageProvider.cs` | New | Infrastructure | Optional S3-compatible provider-neutral adapter. | Streams via AWS SDK and reports unavailable when S3 config is missing/broken. |
| `Explore.Infrastructure/Storage/S3ClientFactory.cs` | New | Infrastructure | S3 SDK client factory. | Shared by legacy presigned service and provider-neutral S3 adapter. |
| `Explore.Infrastructure/Services/ObjectStorageService.cs` | Existing | Infrastructure | Current AWS SDK S3 provider. | Must become optional S3 provider, not default storage abstraction. |
| `Explore.Infrastructure/Storage/S3ConfigResolver.cs` | Existing | Infrastructure | S3 config resolver. | Keep for S3 only; add provider-neutral policy resolver separately. |
| `Explore.API/Controllers/StorageObjectController.cs` | Existing | API | Current storage object API. | Must add provider-neutral upload/download and remove/constrain arbitrary-key local access. |
| `Explore.Blazor/Extensions/BffStorageEndpoints.cs` | Existing | BFF | Browser upload session/proxy. | Currently presigned-S3 URL bound and 10 MB hard-coded. |
| `Explore.Blazor.Client/Services/ImageStorageService.cs` | Existing | Blazor Client | Image/S3 upload orchestration. | Reads small images into memory; delete returns false. |
| `Explore.Blazor.Client/Services/ImageUploadClient.cs` | Existing | Blazor Client | Upload transport seam. | Needs provider-neutral BFF session flow. |
| `Explore.Blazor.Client/Shared/ImageUpload.razor` | Existing | Blazor Client | Image picker/upload component. | Has component-level 5 MB default. |
| `Explore.Blazor.Client/Shared/S3Image.razor` | Existing | Blazor Client | S3 image display. | Rename/wrap into provider-neutral stored image component. |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceStorageSection.razor` | Existing | Blazor Client | Instance S3 settings UI. | Must become local-first provider/usage/limits dashboard. |
| `Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantAdminSettingsLayout.razor` | Existing | Blazor Client | Tenant settings. | Storage section is placeholder. |
| `docker-compose.yml` | Existing | DevOps | Self-host Compose topology. | Injects S3 defaults while MinIO is optional; needs local data volume. |
| `Explore.AppHost/AppHost.cs` | Existing | DevOps | Aspire local topology. | Needs local storage data directory/resource; S3 not required. |
| `docs/STORAGE.md` | Existing | Docs | Storage operator doc. | Currently S3-centered. |
| `docs/BACKUP_RESTORE_UPGRADE.md` | Existing | Docs | Backup/restore runbook. | Must include local data root by default. |

## Key Decisions

- Local filesystem is the default provider for new installs.
- Local data root is deployment-managed and validated; admin UI does not write arbitrary filesystem paths.
- S3 is optional and selected only through explicit provider policy.
- Metadata ID and visibility, not raw object key, are the canonical public addressing model.
- Upload session/reservation is required before accepting bytes.
- Storage visibility/access model is required before generic document upload.
- Quarantine/dry-run comes before destructive cleanup.
- Admin UI action affordances are HAL/status driven.

## Constraints And Rules To Remember

- Repositories return entities only; mapping belongs in handlers.
- Validators are manually instantiated.
- Use `long` for bytes, quotas, sizes, and capacity counters.
- Tenant isolation is API/Persistence authoritative; do not bypass filters casually.
- Browser never controls upload destination, provider, tenant, local path, or privileged headers.
- New C# files need two `ABOUTME:` comments.
- Local files are not static web assets and must not be served from webroot.
- Logs/metrics must not expose paths, raw object keys, presigned URLs, credentials, or filenames where avoidable.

## Validation Baseline

- Baseline build passed on 2026-05-29:
  - `dotnet build --configuration Release --verbosity quiet`
- PR 1 build passed after implementation on 2026-05-29:
  - `dotnet build --configuration Release --verbosity quiet`
- Domain tests passed on 2026-05-29:
  - `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`
  - Result: 273 passed, 0 failed.
- Application unit tests passed on 2026-05-29:
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - Result: 1079 passed, 0 failed.
- Application unit tests passed on 2026-05-29 after PR 2 policy resolver:
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - Result: 1085 passed, 0 failed.
- Application project build passed on 2026-05-29 after PR 2 policy resolver:
  - `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet`
  - Result: 0 warnings, 0 errors.
- Infrastructure tests passed on 2026-05-29 after PR 2 local provider foundation:
  - `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
  - Result: 336 passed, 0 failed.
- Infrastructure tests passed on 2026-05-29 after PR 2 S3 provider adapter:
  - `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
  - Result: 341 passed, 0 failed.
- Infrastructure tests passed on 2026-05-29 after PR 2 policy resolver:
  - `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
  - Result: 343 passed, 0 failed.
- Focused architecture/context verification passed on 2026-05-29:
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - Result: 180 passed, 1 existing skipped, 0 failed.
- Application project build passed on 2026-05-29 after PR 3.1 upload-session commands:
  - `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet`
  - Result: 0 errors with existing warnings.
- Application unit tests passed on 2026-05-29 after PR 3.1 upload-session commands:
  - `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - Result: 1094 passed, 0 failed.
- Infrastructure tests passed on 2026-05-29 after PR 3.1 upload-session commands:
  - `dotnet test Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
  - Result: 343 passed, 0 failed.
- Architecture tests passed on 2026-05-29 after PR 3.1 upload-session commands:
  - `dotnet test Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - Result: 180 passed, 1 existing skipped, 0 failed.
- API project build passed on 2026-05-29 after PR 3.2 upload-session endpoints:
  - `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`
  - Result: 0 errors with existing warnings.
- Application unit tests passed on 2026-05-29 after PR 3.2 upload finalization handler:
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - Result: 1100 passed, 0 failed.
- Application unit tests passed on 2026-05-29 after PR 3.3 metadata-driven content reader:
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - Result: 1117 passed, 0 failed.
- Focused storage content reader tests passed on 2026-05-29:
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/*StorageObjectContentReaderTests*/*" --minimum-expected-tests 3 --no-progress --maximum-parallel-tests 1`
  - Result: 3 passed, 0 failed.
- Focused API anonymous protected smoke passed on 2026-05-29 after registering the named default request-timeout policy:
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/Protected_Endpoints_ReturnUnauthorized_Or_Forbidden" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - Result: 1 passed, 0 failed.
- Focused API upload-session endpoint tests passed on 2026-05-29:
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/*StorageUploadSessionControllerTests*/*" --minimum-expected-tests 5 --no-progress --maximum-parallel-tests 1`
  - Result: 5 passed, 0 failed.
- Focused storage API controller tests passed on 2026-05-29 after PR 3.3:
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*StorageUploadSessionControllerTests*/*" --minimum-expected-tests 7 --no-progress --maximum-parallel-tests 1`
  - Result: 7 passed, 0 failed.
- Architecture tests passed on 2026-05-29 after PR 3.2:
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - Result: 180 passed, 1 existing skipped, 0 failed.
- Architecture tests passed on 2026-05-29 after PR 3.3:
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - Result: 180 passed, 1 existing skipped, 0 failed.
- PR 2 full solution build passed on 2026-05-29:
  - `dotnet build --configuration Release --verbosity quiet`
  - Result: 0 errors with existing warnings.
- PR 2 S3 adapter full solution build attempt on 2026-05-29:
  - `dotnet build --configuration Release --verbosity quiet`
  - Result: blocked outside storage by five `CS1503` errors in `Explore.Blazor.IntegrationTests/Endpoints/BffSetupSecretEndpointsTests.cs`.
- PR 3.1 full solution build attempt on 2026-05-29:
  - `dotnet build --configuration Release --verbosity quiet`
  - Result: blocked outside storage by unrelated `Notification.DeduplicationKey` required-member test compile errors and duplicate actor subscription persistence members.
- Persistence integration tests ran on 2026-05-29 and failed outside storage:
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - Result: 139 passed, 2 failed, both in `EmailDispatchOutboxTransitionRepositoryTests`.
- Existing warnings observed:
  - `MailKit` moderate vulnerability warning (`NU1902`) across multiple projects.
  - `NU1510` package pruning suggestions in Infrastructure/Blazor.
  - `NU1608` Roslyn package constraint warning in API.
  - `ASPDEPR001` for `Microsoft.Extensions.ApiDescription.Client`.

Full implementation validation remains the plan section 7 command list.

## Current Known Risks / Unknowns

- Public arbitrary-key endpoints are the highest-risk contract to carry forward.
- Existing public storage list/detail endpoints may be wrong for generic private files.
- Exact visibility/purpose/lifecycle enum names need implementation design in PR 1.
- Local filesystem capacity reporting in containers is best effort; docs must avoid over-promising per-tenant physical free space.
- S3 secret ownership/redaction should align with the current `SECRETS.md` model before the admin read DTOs are finalized.

## Handoff Notes

### Handoff - 2026-05-29 Europe/Brussels

- **Current state:** PR 1 foundation, PR 2 local/S3 provider + effective policy resolver foundations, PR 3.1 Application upload-session commands/validators, PR 3.2 API upload-session/finalization endpoints, PR 3.3 metadata-driven download/public-image runtime, PR 3.4 arbitrary-key route removal, and PR 3.5 BFF upload-session/proxy refactor are implemented.
- **Next action:** Start PR 3.6 by regenerating OpenAPI/NSwag client artifacts and fixing generated-client contract drift.
- **Blockers:** Full API integration suite is still noisy from unrelated active-worktree database drift and one authorization matrix timeout; full Architecture is blocked by unrelated HATEOAS/AI-contract active-worktree issues; root Release build is blocked by unrelated AI assistant and generated-client test compile errors; `git diff --check` is blocked by unrelated generated-client trailing whitespace.
- **Modified storage files:** Domain storage entities/constants/settings, Application storage DTOs/validators/contracts/mapping/settings serialization, upload-session commands/handlers/failure codes, provider-neutral storage contracts/models, effective storage policy resolver, local filesystem provider/options/resolver, S3 provider/client factory, legacy S3 service factory usage, Persistence storage configs/repositories/migration/query filters/seeds, domain/application/infrastructure tests, schema docs, and workstream docs.
- **Validation:** Domain unit tests passed earlier. Current Application/API project builds, Application unit tests, focused storage content/API tests, focused protected-endpoint API smoke, Infrastructure tests, and Architecture tests passed before later active-worktree drift. After PR 3.5, `Explore.Blazor` and `Explore.Blazor.Client` builds passed; full `Explore.Blazor.IntegrationTests` passed; focused BFF upload proxy and session-store tests passed.
- **Documentation impact:** Runtime/operator docs (`docs/STORAGE.md`, `CONFIGURATION.md`, `SELF_HOSTING.md`, backup/restore, API changelog) still need updates when provider/API behavior changes in later PRs.
- **Risks:** OpenAPI/NSwag generated client drift still blocks clean Blazor client test-project verification; PR 3.6 should regenerate and stabilize those artifacts.
- **Notes for next contributor/agent:** Keep local filesystem first-class and provider-neutral. Provider implementations, policy resolver, Application upload-session commands, API upload/finalization endpoints, metadata-driven content reads, and arbitrary-key API removal now exist; remaining BFF/client flows must not use `IObjectStorageService` or caller-supplied object keys for local storage.
