<!-- ABOUTME: Current evidence and implementation handoff for the reusable storage content-security workstream. -->
<!-- ABOUTME: Records verified flows, decisions, affected surfaces, blockers, and the next executable task. -->

# Reusable Safe-Raster and Storage Content Boundary — Context

Last Updated: 2026-07-29 Europe/Brussels

## Current Status

- Planning is complete; no runtime source was changed in this planning turn.
- Next task: `SCS-100` failing-first Application tests for the reusable safe-raster policy.
- Approval checkpoint: accepting the plan authorizes the pre-v1 removal of
  `POST /api/storageobject/generate-upload-url` and `POST /api/storageobject`.
- No matching active/paused workstream currently owns platform-wide storage content safety.
- The repository has unrelated dirty Event/Actor/Studio/API/docs changes. Implementation must record and preserve them.
- Code-review graph discovery was attempted first and failed with `Transport closed`; narrow repository inspection was used as fallback.

## Contract Classification

No single intent in `.claude/contract/intents.yaml` covers this cross-layer security hardening. Use the documented fallback contract:

- `add-write-endpoint` rules for authenticated storage mutations and CQRS behavior;
- `openapi-contract-change` for breaking operation/DTO removal and generated client convergence;
- `.claude/rules/application-layer.md`;
- `.claude/rules/api-controllers.md`;
- `.claude/rules/blazor-server.md`;
- `.claude/rules/blazor-client.md`;
- `.claude/rules/tests.md`;
- `clean-architecture-rules` for the Application-owned shared policy.

Authoritative docs already inspected during planning:

- `AGENTS.md`
- `docs/QUICK_REFERENCE.md`
- `docs/GOVERNANCE.md`
- `docs/DOCUMENTATION_STYLE_GUIDE.md`
- `docs/OPERATIONS.md`
- `docs/TESTING.md`
- `docs/ARCHITECTURE.md`
- `docs/CODEBASE_STRUCTURE.md`
- `docs/API.md`
- `docs/SECURITY-MODEL.md`
- `docs/BLAZOR.md`
- `docs/API_CHANGELOG.md`
- `dev/active/README.md`
- `.claude/contract/intents.yaml`

## Verified End-to-End Flows

### Persistent browser images

Event create/edit, User profile picture, and Organization logo upload through:

```text
IBrowserFile
  -> ImageUploadClientPolicy / ImageFileReaderService
  -> IImageStorageService
  -> POST /bff/storage/upload-session
  -> POST /bff/storage/upload-proxy
  -> POST /api/storageobject/upload-sessions
  -> PUT /api/storageobject/upload-sessions/{id}/content
  -> FinalizeStorageUploadSessionCommandHandler
  -> IFileStorageProvider.WriteAsync
  -> active StorageObject
```

The browser policy currently accepts JPEG/PNG/GIF/WebP and performs signature detection. It is useful UX but not an authority.

### AI images

```text
IBrowserFile
  -> AiAssistantRail (currently any image/*, including BMP/SVG)
  -> base64 AiMessageImageInputDto
  -> SendAiMessageRequestDtoValidator (currently image/* + base64 size only)
  -> AiMessage.ImageAttachmentsJson
  -> AI provider prompt
  -> optional confirmed Event image
  -> storage upload session/finalizer
```

The authoritative validation must occur before `ImageAttachmentsJson` persistence and provider construction, not only when a later proposed Event image is finalized.

### ATProto thumbnails

```text
record JSON thumbnail metadata
  -> AtprotoThumbnailBlobGateway
  -> hardened DID/PDS fetch
  -> size + CID + exact MIME + full structural container validation
  -> provider staging
  -> AtprotoJetstreamRepository.ApplyThumbnailAsync
  -> active public StorageObject + Event.FeaturedImageId
```

The gateway already has the correct parser behavior, but it is private to Infrastructure. The repository has a separate MIME-to-extension mapping that still includes SVG and should consume the shared policy.

### Public and authenticated delivery

```text
/api/storageobject/{id}/public
  -> GetPublicImageRequest
  -> StorageObjectContentReader.OpenAsync(publicImagesOnly: true)
  -> provider stream
  -> inline FileStreamResult

/api/storageobject/{id}/content
  -> authenticated/authorized reader
  -> provider stream
  -> inline FileStreamResult
```

The public reader currently checks only active lifecycle plus `public_image`.
The same `ToFileResult` path is used for both inline public images and general authenticated content.

## Verified Security Gaps

1. `StorageContentSignaturePolicy` validates raster prefixes only.
2. Upload-session DTO validation allows unsafe purpose/visibility/MIME combinations.
3. `UpdateStorageObject` can mutate byte identity and promote objects after inspection.
4. Anonymous read trusts lifecycle/visibility without MIME/purpose eligibility.
5. Authenticated SVG/HTML/general content is streamed inline.
6. Presigned downloads do not force attachment disposition.
7. `StoragePresentationUrlResolver` can sign a raw object key for browser presentation.
8. Direct presigned upload plus caller-created metadata bypasses all byte inspection.
9. Event/Actor/User/Organization image references mostly validate existence only.
10. AI accepts and persists/sends arbitrary `image/*`.
11. ATProto materialization duplicates MIME/extension policy and includes an SVG fallback.

## Shared Policy Shape

Recommended concrete type:

```text
Explore.Application.Services.SafeRasterContentPolicy
```

Keep it static and dependency-free. Required operations only:

- exact MIME normalization/allowlist;
- browser/AI four-format subset;
- MIME-to-extension compatibility;
- full byte-container structural validation;
- safe public-image metadata check;
- active same-tenant image-reference check.

Do not add:

- an interface;
- DI registration;
- a factory/strategy system;
- a decoder/transcoder package;
- a generic malware-scanning abstraction;
- a schema attestation column.

## Important Existing Files

Application:

- `src/Explore.Application/Services/StorageContentSignaturePolicy.cs`
- `src/Explore.Application/Services/StorageObjectContentReader.cs`
- `src/Explore.Application/Services/StoragePresentationUrlResolver.cs`
- `src/Explore.Application/Features/StorageObjects/Handlers/Commands/FinalizeStorageUploadSessionCommandHandler.cs`
- `src/Explore.Application/Features/StorageObjects/Handlers/Commands/CreateStorageObjectCommandHandler.cs`
- `src/Explore.Application/Features/StorageObjects/Handlers/Commands/UpdateStorageObjectCommandHandler.cs`
- `src/Explore.Application/DTOs/StorageObject/Validators/CreateStorageUploadSessionDtoValidator.cs`
- `src/Explore.Application/DTOs/StorageObject/Validators/CreateStorageObjectDtoValidator.cs`
- `src/Explore.Application/DTOs/StorageObject/Validators/UpdateStorageObjectDtoValidator.cs`
- `src/Explore.Application/DTOs/Ai/Validators/SendAiMessageRequestDtoValidator.cs`
- `src/Explore.Application/DTOs/Ai/AiMessageImageAttachmentSerializer.cs`
- `src/Explore.Application/Features/AiAssistant/Handlers/Commands/SendAiMessageCommandHandler.cs`
- `src/Explore.Application/Features/AiAssistant/Handlers/Commands/ConfirmAiProposedActionCommandHandler.cs`

Image-reference consumers:

- Event create/update/draft validators and handlers;
- nested EventSession/background image fields in Event DTO validation;
- Actor create/update validators and handlers;
- `UpdateUserCommandHandler`;
- `CreateOrganizationCommandHandler`.

API/BFF/client:

- `src/Explore.API/Controllers/StorageObjectController.cs`
- `src/Explore.API/Hateoas/Policies/StorageObjectLinkPolicy.cs`
- `src/Explore.API/Hateoas/RouteNames.cs`
- `src/Explore.Blazor/Extensions/BffStorageEndpoints.cs`
- `src/Explore.Blazor.Client/Services/ImageUploadClientPolicy.cs`
- `src/Explore.Blazor.Client/Services/ImageUploadClient.cs`
- `src/Explore.Blazor.Client/Services/ImageStorageService.cs`
- `src/Explore.Blazor.Client/Services/ImageStorageRecordClient.cs`
- `src/Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor`

Federation/persistence:

- `src/Explore.Infrastructure/Services/Federation/AtprotoThumbnailBlobGateway.cs`
- `src/Explore.Persistence/Repositories/AtprotoJetstreamRepository.cs`

## Existing Test Seams

- `tests/Event.Application.UnitTests/Features/StorageObjects/Commands/StorageUploadSessionCommandHandlerTests.cs`
- `tests/Event.Application.UnitTests/Features/StorageObjects/Queries/StorageObjectContentReaderTests.cs`
- `tests/Event.Application.UnitTests/Features/StorageObjects/Validators/StorageObjectMetadataDtoValidatorTests.cs`
- `tests/Event.Application.UnitTests/Features/StorageObjects/Commands/StorageObjectCommandHandlerTests.cs`
- `tests/Event.Application.UnitTests/Features/AiAssistant/Commands/SendAiMessageCommandHandlerTests.cs`
- `tests/Event.Application.UnitTests/Features/AiAssistant/Commands/AiProposedActionCommandHandlerTests.cs`
- `tests/Event.API.IntegrationTests/Features/StorageUploadSessionControllerTests.cs`
- `tests/Event.API.IntegrationTests/Features/StorageObjectControllerTests.cs`
- `tests/Explore.Blazor.IntegrationTests/Endpoints/BffStorageUploadProxyTests.cs`
- `tests/Explore.Blazor.Client.Tests/Components/Shell/AiAssistantRailTests.cs`
- `tests/Explore.Blazor.Client.Tests/Services/ImageUploadClientTests.cs`
- `tests/Explore.Blazor.Client.Tests/Services/ImageStorageServiceTests.cs`
- `tests/Explore.Infrastructure.Tests/Federation/AtprotoThumbnailBlobGatewayContractTests.cs`
- `tests/Event.Persistence.IntegrationTests/Federation/AtprotoInboundEventImportPersistenceTests.cs`
- `tests/Event.Architecture.Tests/`

## Behavioral Details to Preserve

- Upload-session quota reservations are released on inspection/provider failure.
- Provider write receives the exact reserved MIME and size.
- Non-seekable streams remain usable after inspection.
- ATProto invalid optional thumbnails return `null`; caller cancellation is rethrown.
- ATProto staged bytes are cleaned up on cancellation/unconsumed results.
- DID/PDS SSRF and CID/size binding stay in Infrastructure.
- `AtprotoRecord.RecordJson` is semantically unchanged.
- `nosniff`, CSP, ETag/checksum, range processing, authorization, response caching, and no-store rules remain.
- BFF upload session/proxy keeps authorization, antiforgery, user binding, content-type binding, expected-size binding, opaque IDs, and no raw destination.
- HAL remains the UI authority for storage actions.

## Parser Precision

Use the existing ATProto parsers as the starting implementation, with one planned correction:

- valid animated WebP must pass. The previous final code-quality review found the current gateway rejects standard `ANIM`/`ANMF` chunks while advertising generic `image/webp`.

Documentation must say:

> structurally framed through exact EOF

It must not say:

> decoded, sanitized, fully valid, or malware-free

## Legacy Compatibility Decision

Recommended and planned:

- remove the direct presigned-upload endpoint;
- remove caller-authored `StorageObject` creation;
- remove generated/client fallback paths;
- keep provider-neutral upload sessions;
- keep ID-based authenticated presigned downloads, but force attachment disposition;
- keep server-owned direct provider operations used internally, provided their caller performs the relevant shared validation before public materialization.

Reason: maintaining a second public upload path would require quarantine, inspection, promotion, provenance, cleanup, and new lifecycle tests. The existing upload-session path already provides the smaller secure solution.

## Documentation and Completion State

The historical ATProto work is marked completed in `.omo/boulder.json`. The current global `active_work_id` belongs to another workstream. Execution must:

- add/update a separate `storage-content-security` work entry;
- never replace unrelated `active_work_id` merely to report this task;
- append, not rewrite, `.omo/start-work/ledger.jsonl`;
- add cross-references to the ATProto plan/context/tasks after completion;
- store fresh final reports under `.omo/evidence/storage-content-security/`.

## Verification Policy

Each phase runs one root Release build and one project test as defined in the plan. The explicit user-required PostgreSQL lane is the only container runtime phase. Do not start Aspire or a browser.

Final reviews are invalidated by any later code/doc commit. Every lane must record the same full SHA:

1. goal/constraint verification;
2. context/history/scope mining;
3. code-quality review;
4. hands-on QA evidence review;
5. security/threat audit;
6. runtime-debugging audit.

## Handoff

Start with `SCS-100`:

1. record baseline branch/SHA/status and root Release build;
2. retry graph impact discovery;
3. add failing Application tests for the policy matrix;
4. add the smallest static Application policy by moving existing parser code;
5. do not touch ATProto gateway production code until Phase 5;
6. update the task ledger immediately after the Phase 1 gate.

