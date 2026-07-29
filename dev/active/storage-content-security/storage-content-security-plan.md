<!-- ABOUTME: Canonical implementation plan for reusable safe-raster validation and storage delivery hardening. -->
<!-- ABOUTME: Covers user uploads, AI attachments, ATProto thumbnails, image references, public reads, and legacy bypass removal. -->

# Reusable Safe-Raster and Storage Content Boundary — Implementation Plan

Last Updated: 2026-07-29 Europe/Brussels

## 0. Plan Metadata

| Field | Value |
|---|---|
| Task ID | `storage-content-security` |
| Canonical intent | Fallback contract: no single intent in `.claude/contract/intents.yaml`; compose `add-write-endpoint`, `openapi-contract-change`, Application/API/Blazor path rules, and the global security invariants |
| Status | Proposed — planning complete; legacy route removal requires approval through plan acceptance before implementation |
| Owner | Unassigned |
| Change type | Cross-layer security hardening, API contract cleanup, Application policy reuse, storage delivery, federation, Blazor/BFF, tests, and documentation |
| Execution boundary | Test-first, phase ordered; runtime implementation must not begin from this planning turn |

### Scope

In scope:

- Introduce one dependency-free, server-side safe-raster policy in `Explore.Application`.
- Accept only exact, parameter-free safe-raster MIME types and MIME-matching extensions.
- Structurally validate JPEG, PNG, GIF, WebP, and AVIF through exact end-of-file before a raster becomes active or public.
- Preserve support for valid still and animated WebP; add a real animated-WebP positive control.
- Make storage upload finalization, ATProto thumbnail staging, and AI image input consume the shared policy.
- Enforce safe cross-field metadata for `public_image` and image purposes at create, update, materialization, linking, and read boundaries.
- Apply image-reference eligibility to Event featured/background/session images, Actor/profile pictures, Organization logos, and User profile pictures.
- Keep non-raster attachments private/authenticated and force download disposition instead of same-origin inline rendering.
- Make presigned downloads disposition-safe and stop using raw object keys as browser image presentation URLs.
- Remove the legacy direct presigned-upload plus caller-authored storage-metadata API/client path.
- Preserve the original ATProto `AtprotoRecord.RecordJson` when an optional thumbnail is rejected.
- Rerun the targeted Application, API, BFF, client, Infrastructure, PostgreSQL persistence, Architecture, and Release build gates.
- Rerun the five final review lanes and a runtime-debugging audit against one pinned final SHA.
- Update canonical docs, the live task/context ledgers, ATProto cross-references, `.omo/start-work/ledger.jsonl`, and the matching `.omo/boulder.json` work entry.

Out of scope:

- Malware scanning, antivirus, content moderation, or a generic file-scanning subsystem.
- Image transcoding, sanitization, or adding an image-decoder dependency.
- Claiming full codec correctness; the guarantee is bounded structural framing through exact EOF.
- Pixel-count/decompression-bomb policy beyond existing upload-size limits.
- Making arbitrary documents anonymously readable.
- Replacing current document signature rules with a full PDF/Office parser.
- Proxying or ingesting tenant branding URLs. External HTTPS logo URLs are a separate URL/privacy boundary because their bytes are not stored or served by `StorageObjectController`.
- A database schema change or validation-attestation column. New writes are guarded at ingress, metadata mutation is constrained, and legacy unsafe rows fail closed at anonymous read.
- Browser/manual/Playwright UI QA. Client behavior is covered by non-browser TUnit/bUnit tests; runtime review is limited to the explicitly requested storage/federation audit.

### Inputs and overlaps

- The completed ATProto thumbnail repair is the source of the existing exact MIME and whole-container parsers:
  `src/Explore.Infrastructure/Services/Federation/AtprotoThumbnailBlobGateway.cs`.
- Historical ATProto plan/evidence remains under `.omo/plans/atproto-auth.md`,
  `dev/active/atproto-auth/`, and `.omo/evidence/atproto-auth/`.
- This workstream supersedes only duplicated safe-raster logic and extends the trust boundary; it must not rewrite historical ATProto evidence.
- No existing `dev/active/` or `dev/pause/` workstream owns platform-wide storage content safety.
- The worktree is shared and currently contains unrelated Event/Actor/Studio changes. Implementation must preserve them and stop on an in-scope collision.
- The code-review graph was invoked first during planning but returned `Transport closed`; narrow repository searches were used as the documented fallback.

## 1. Executive Summary

The ATProto thumbnail fix correctly prevents remote SVG or MIME-spoofed active content from being stored, but the same guarantee is not yet applied to every file boundary. Browser Event images, profile pictures, and Organization logos converge on storage upload sessions, while AI images arrive as base64 and ATProto thumbnails arrive over the network. All of them can safely share one Application-layer raster policy.

The root vulnerability is broader than MIME filtering. A safe design must bind four facts:

1. the declared MIME is an exact allowlisted raster type;
2. the extension matches that MIME;
3. the complete byte container matches that MIME through exact EOF;
4. only eligible metadata can be linked or anonymously served as an image.

The smallest correct implementation reuses the already-written ATProto parsers, moves them inward to `Explore.Application`, applies them at each server trust boundary, and deletes the legacy API path that permits direct provider upload followed by caller-authored active metadata. Blazor checks remain useful for immediate feedback, but the API/Application boundary remains authoritative.

## 2. Current-State Evidence

| Evidence | Current behavior | Security consequence |
|---|---|---|
| `StorageContentSignaturePolicy` | Checks only a short magic prefix for JPEG/PNG/GIF/WebP; unknown `image/*` is rejected, but unknown non-image MIME is accepted | A prefix-valid raster with malformed/trailing content is not fully bound to its MIME |
| `FinalizeStorageUploadSessionCommandHandler` | Runs the signature policy before provider write, then creates an `Active` object using session MIME/purpose/visibility | This is the correct shared ingress seam, but its raster validation is weaker than ATProto |
| `CreateStorageUploadSessionDtoValidator` | Validates MIME syntax and enum membership independently | `text/html` plus `public_image`, or an attachment purpose plus `public_image`, can be reserved |
| `UpdateStorageObjectDtoValidator` and `UpdateStorageObjectCommandHandler` | Permit later changes to content type, extension, file type, visibility, and purpose | An object can be reclassified into an unsafe anonymous image after upload |
| `StorageObjectContentReader.CanRead(publicImagesOnly: true)` | Requires only `Active` plus `public_image` | Anonymous serving trusts mutable metadata and does not require a safe-raster MIME/image purpose |
| `StorageObjectController.ToFileResult` | Streams stored MIME inline for both authenticated and anonymous reads | Authenticated SVG/HTML/general content is not forced to attachment disposition |
| `GetPresignedDownloadUrlRequestHandler` / `ObjectStorageService` | Signs a provider GET without a download disposition override | Authenticated non-raster content can be rendered directly by the object-storage origin |
| `StoragePresentationUrlResolver` | Signs a raw object key when a projection contains one | Browser image presentation can bypass metadata-backed public-image eligibility |
| `POST /api/storageobject/generate-upload-url` | Returns a direct S3-compatible presigned PUT | Uploaded bytes never pass the provider-neutral finalizer |
| `POST /api/storageobject` | Accepts caller-authored provider key, MIME, visibility, purpose, and lifecycle metadata | A caller can create an active public object without server byte inspection |
| Event/Actor/User/Organization validators and handlers | Usually check only that an image `StorageObject` exists; some paths do not even do that | Unsafe, non-active, wrong-tenant, or non-public objects can be attached as presentation images |
| `BffStorageEndpoints` | Server-owns `legacy_image`/`public_image`, but accepts any syntactically valid MIME before forwarding to the API | The API can reject SVG later, but the BFF does not fail early on its image-only route |
| `ImageUploadClientPolicy` / `ImageFileReaderService` | UX allowlist and signature detection already cover JPEG/PNG/GIF/WebP | Useful client feedback exists, but it is not a security authority |
| `AiAssistantRail.OnFilesSelectedAsync` | Accepts any `image/*` and explicitly maps BMP/SVG | Active/non-provider-safe image content can enter the AI request |
| `SendAiMessageRequestDtoValidator` | Accepts any `image/*`, validates base64 length, but not MIME/container binding or declared-size equality | Bytes may be persisted in message JSON and sent to an AI provider before storage finalization rejects them |
| `AtprotoThumbnailBlobGateway` | Exact JPEG/PNG/GIF/WebP/AVIF MIME plus full bounded structural parsing and CID binding | Correct reusable raster behavior exists, but is private to Infrastructure |
| `AtprotoJetstreamRepository.ApplyThumbnailAsync` | Creates active `public_image` metadata directly after staging; has a local MIME-to-extension switch including SVG | Materialization should consume the shared metadata policy and must preserve `RecordJson` |
| `SecurityHeadersMiddleware` | Adds `nosniff` and `default-src 'none'` | Valuable defense in depth, but headers must not be the primary content trust decision |

### File-ingress inventory

| Ingress/consumer | Current path | Required policy application |
|---|---|---|
| Event create/edit featured image | `IImageStorageService` → BFF session/proxy → API finalizer | Shared byte policy plus safe image-reference eligibility |
| User profile picture | Shared image upload path, then `UpdateUserCommandHandler` | Shared byte policy plus safe image-reference eligibility |
| Organization logo | Shared image upload path, then `CreateOrganizationCommandHandler` | Shared byte policy plus safe image-reference eligibility |
| Actor profile image | Storage-object ID on Actor create/update | Safe image-reference eligibility |
| Event background and nested session images | Storage-object IDs in Event create/update/draft DTOs | Safe image-reference eligibility |
| AI assistant images | Browser bytes → base64 request → persisted message JSON/provider prompt | Client UX subset plus authoritative Application MIME/container/size validation before persistence |
| AI proposed Event image | Stored AI attachment → upload session/finalizer | Same Application safe-raster policy; no second parser |
| ATProto thumbnails | Hardened PDS fetch → gateway → provider → persistence materialization | Shared policy with AVIF, CID/size binding retained, optional rejection preserves canonical JSON |
| Authenticated attachments/documents | Upload session → `/content` or presigned download | Existing signature policy, private/authenticated visibility, forced attachment disposition |
| Anonymous image reads | `/api/storageobject/{id}/public` and Event OG reads | Active + public + image-purpose + exact safe-raster metadata, otherwise 404 |
| Legacy direct upload | Presigned provider PUT + caller-created metadata | Remove, do not build a second inspection/promotion pipeline |

## 3. Proposed End State

### 3.1 One reusable policy

`SafeRasterContentPolicy` lives in `Explore.Application/Services` as a static, dependency-free policy. It does not introduce an interface, factory, DI registration, or third-party decoder.

It owns:

- exact parameter-free MIME normalization;
- MIME-to-extension matching;
- server-safe raster set: JPEG, PNG, GIF, WebP, AVIF;
- browser/AI subset: JPEG, PNG, GIF, WebP;
- bounded full-container structural validation through exact EOF;
- public-image metadata eligibility;
- image-reference eligibility for active, same-tenant public raster objects.

`StorageContentSignaturePolicy` delegates raster decisions to it and retains the existing non-raster document-prefix behavior. `AtprotoThumbnailBlobGateway` delegates MIME/container validation to it while retaining SSRF controls, DID/PDS resolution, response-size, CID, timeout, cleanup, and fail-soft behavior.

### 3.2 Trust flow

```text
untrusted bytes + claimed MIME/extension
                  |
                  v
        boundary-specific size limit
                  |
                  v
   SafeRasterContentPolicy exact MIME + full container
                  |
          +-------+--------+
          |                |
       reject           provider write
          |                |
   no active object         v
                    server-owned metadata
                            |
                            v
             reference/link eligibility check
                            |
                            v
       anonymous reader rechecks safe metadata
```

For general attachments, the flow ends in authenticated download with
`Content-Disposition: attachment`. General attachments never become public through the image route.

### 3.3 Surface outcomes

- SVG, BMP, wildcard MIME, MIME parameters, extension mismatch, truncated containers, MIME-spoofed bytes, and valid-prefix active tails are rejected before raster provider write.
- A failed AI image never enters `AiMessage.ImageAttachmentsJson` and is never sent to a configured AI provider.
- An optional hostile ATProto thumbnail is dropped while the Event/Session import and canonical `RecordJson` remain unchanged.
- `public_image` can be assigned only to an active safe-raster object with an image purpose.
- Event/profile/logo/Actor references reject ineligible storage objects rather than silently linking them.
- Anonymous reads of legacy unsafe metadata return 404 without opening provider content.
- Authenticated documents/general content download as attachments.
- Raw provider keys are not converted into browser presentation URLs.
- The direct-upload and caller-authored metadata operations disappear from OpenAPI and the generated client.

## 4. Constraints and Invariants

- Clean Architecture remains inward-only: Domain → Application ← Infrastructure/Persistence → API/Blazor composition.
- Repository contracts return entities, not policy DTOs.
- Validators are manually instantiated.
- New files start with two `ABOUTME:` lines.
- GET authorization semantics remain unchanged: the public image route is `[AllowAnonymous]`; other storage reads remain `[Authorize]`.
- HAL remains the authority for storage mutation/download affordances.
- Browser MIME/extension/signature checks are UX only and cannot replace Application validation.
- Public-image rejection is fail closed and returns bounded errors/404; no provider key, raw URL, filename, byte prefix, or provider response is logged.
- ATProto caller cancellation must still propagate; optional thumbnail failures remain fail soft.
- No canonical ATProto JSON rewrite is permitted.
- No new package, decoder, scanner, interface, or database migration is introduced unless implementation evidence proves the existing dependency-free approach cannot satisfy a required container.
- Do not edit unrelated dirty-worktree files unless a direct in-scope conflict is resolved with the user.
- Route removal is a breaking pre-v1 OpenAPI change. Plan acceptance is the required approval; without it, implementation must stop before `SCS-300` rather than leave the insecure operations available.

## 5. Architecture and Design Decisions

| ID | Decision | Rationale |
|---|---|---|
| D1 | Put one static safe-raster policy in `Explore.Application` | Both Infrastructure and Application can consume it without reversing dependencies or adding DI boilerplate |
| D2 | Move, do not duplicate, the ATProto JPEG/PNG/GIF/WebP/AVIF structural parsers | The existing parser work is already validated; one owner prevents drift |
| D3 | Describe the guarantee as “structurally framed through exact EOF” | The hand-written parser does not decode pixels or prove CRC/codec semantics |
| D4 | Support valid still and animated WebP | Current UI advertises generic WebP support; reusing a still-only parser would create a regression |
| D5 | Keep AVIF server/federation-safe but exclude it from the current browser/AI chooser subset | ATProto already supports AVIF; current browser UX/provider assumptions advertise four formats |
| D6 | Buffer only raster content within the already-resolved upload limit; keep non-raster streaming/prefix replay | Full-container validation needs complete bounded bytes, while documents do not need a new memory-heavy parser |
| D7 | Centralize public-image and reference eligibility in the same policy | MIME, extension, lifecycle, visibility, purpose, and tenant checks must not drift across handlers/readers |
| D8 | Make content type, extension, and file type immutable after finalization | Mutating byte identity without reinspection is unsafe; display-name and ownership metadata can remain editable |
| D9 | Permit access mutation only when the resulting entity satisfies the shared cross-field policy | Prevents `attachment`/HTML/general objects from being promoted to `public_image` |
| D10 | Require eligible storage objects at every image FK/reference assignment | Safe upload alone is insufficient if arbitrary existing objects can be linked as Event/profile/logo images |
| D11 | Force non-raster API and presigned downloads to attachment disposition | Prevents same-origin/provider-origin inline active rendering while preserving authenticated file delivery |
| D12 | Stop signing raw object keys in `StoragePresentationUrlResolver` | Browser presentation must use metadata-backed IDs/public routes or an explicitly external URL |
| D13 | Remove legacy direct upload and metadata-create operations | The provider-neutral session flow already exists; building quarantine/promotion for a second path is unnecessary |
| D14 | Keep security headers as defense in depth | `nosniff` and CSP reduce impact but do not prove stored bytes are safe raster |
| D15 | Reuse existing BusinessMetrics failure-code dimensions | New metric instruments are unnecessary; bounded policy failure codes are sufficient |
| D16 | Add no validation-attestation column | New ingress is byte-bound, mutation is constrained, and anonymous reads fail closed on unsafe legacy metadata |
| D17 | Preserve ATProto staging cleanup/CID/SSRF behavior unchanged around the extracted parser | The refactor must narrow duplication without weakening the already-reviewed network boundary |

Rejected alternatives:

- A generic `IFileSecurityService` with multiple validators: rejected because there is one concrete raster policy and no second implementation.
- Server-side image transcoding: rejected because it adds a decoder/dependency and is not required to prevent active-content serving.
- A second secure direct-upload promotion workflow: rejected because provider-neutral upload sessions already solve the problem.
- MIME-only anonymous-read checks without mutation hardening: rejected because metadata is currently caller-editable.
- Client-only enforcement: rejected because browser claims are untrusted and AI/API callers can bypass the UI.

## 6. Implementation Phases and Tasks

The checkbox ledger in `storage-content-security-tasks.md` is canonical. Tests and documentation are part of the task that owns the behavior. Each phase ends with exactly one root Release build and one relevant non-browser test project.

### Phase 1 — Shared safe-raster policy

Goal: create the dependency-free policy under failing-first Application tests.

- **SCS-100:** Add failing tests for exact MIME/extension rules, the five safe raster formats, truncated containers, mismatched MIME, parameterized MIME, valid-prefix active tails, exact EOF, and real animated WebP.
- **SCS-110:** Add `SafeRasterContentPolicy` in `Explore.Application/Services` by moving the existing structural parsers inward. Expose only the minimal MIME, extension, byte-container, public-metadata, and reference-eligibility operations required by known callers.
- **SCS-120:** Refactor `StorageContentSignaturePolicy` so raster uploads read at most the reserved size plus one sentinel byte, validate the complete container, and replay a bounded `MemoryStream` to the provider. Preserve the current streaming prefix behavior for non-raster documents/general files.
- **SCS-130:** Document the exact “structurally framed through EOF, not decoded” guarantee in `docs/SECURITY-MODEL.md`.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
```

### Phase 2 — Storage metadata, image references, and AI ingress

Goal: make every Application-layer image decision consume the shared policy.

- **SCS-200:** Add cross-field validation to upload-session creation and finalization: image purposes and `public_image` require an exact safe-raster MIME/extension; non-image/public combinations fail before quota/provider work; finalization still byte-validates before creating an active object.
- **SCS-210:** Make storage byte identity immutable on update and validate the merged entity before any access/purpose change. Cover both “metadata-only” and “access-only” patches so split groups cannot bypass cross-field rules.
- **SCS-220:** Harden `StorageObjectContentReader` and presigned-download authorization helpers with the same lifecycle/visibility/purpose/MIME eligibility rules. Return the safe display name and an attachment decision needed by delivery surfaces.
- **SCS-230:** Replace existence-only image checks with active, same-tenant, public, safe-raster eligibility for Event featured/background/nested-session images, Event drafts, Actor profile images, User profile pictures, and Organization logos. Reject the command instead of silently ignoring an invalid image reference.
- **SCS-240:** Validate AI image MIME, decoded length, declared `SizeBytes`, extension/file name when present, and complete bytes before serializing `ImageAttachmentsJson`. Reuse the same policy again when an AI proposed Event image is materialized; do not persist or send rejected bytes to the AI provider.
- **SCS-250:** Add focused handler/validator/read tests beside each changed Application boundary and update the AI/storage sections of `docs/SECURITY-MODEL.md`.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
```

### Phase 3 — API delivery and legacy bypass removal

Goal: make HTTP delivery safe and remove externally callable uninspected upload operations.

- **SCS-300:** Split storage file results by purpose: safe public raster stays inline; authenticated non-raster content includes a sanitized `Content-Disposition: attachment`; all responses retain range, checksum/ETag, `nosniff`, and existing authorization.
- **SCS-310:** Add attachment response overrides to ID-based presigned downloads and remove raw-object-key signing from browser image presentation. Keep provider keys and signed URLs secret/no-store.
- **SCS-320:** Remove `POST /api/storageobject/generate-upload-url` and `POST /api/storageobject` from `StorageObjectController`, route names, HAL policies, and public OpenAPI. Keep provider-neutral upload sessions as the only external byte-ingress contract.
- **SCS-330:** Add API integration tests proving unsafe public metadata returns 404 without provider open, documents/SVG/general content download as attachments, safe raster remains inline, and removed legacy operations are absent.
- **SCS-340:** Regenerate `schemas/openapi_islamu-event.json` and the generated client using the documented workflow; update `docs/API.md`, `docs/API_CHANGELOG.md`, and `docs/API_CONTRACT_INVENTORY.md` with the approved pre-v1 breaking change.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
```

### Phase 4 — BFF and browser image UX

Goal: fail early in browser-only image paths without treating client/BFF checks as authoritative.

- **SCS-400:** Restrict `/bff/storage/upload-session` and `/bff/storage/upload-proxy` to exact JPEG/PNG/GIF/WebP declarations and matching simple extensions while preserving authorization, antiforgery, user/session/size binding, and provider-neutral forwarding.
- **SCS-410:** Route Event images, User profile pictures, and Organization logos through the existing `ImageUploadClientPolicy`/`ImageFileReaderService` and BFF session flow only. Remove reachable fallback calls that request direct provider URLs or create storage metadata records.
- **SCS-420:** Restrict the AI composer accept list and file-selection logic to the same four browser types, compare detected bytes with declared MIME, and remove BMP/SVG mappings. Keep user-safe errors and size/count limits.
- **SCS-430:** Update BFF integration coverage and `docs/BLAZOR.md`; update client/bUnit tests in the same task and include the client project in final cumulative verification.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
```

### Phase 5 — ATProto gateway reuse

Goal: remove the Infrastructure-private raster policy without changing the reviewed network/CID/fail-soft behavior.

- **SCS-500:** Make `AtprotoThumbnailBlobGateway` call the Application safe-raster policy for exact JPEG/PNG/GIF/WebP/AVIF MIME and complete container validation.
- **SCS-510:** Delete the moved private parser/MIME code from the gateway. Preserve hardened DID/PDS resolution, redirect/DNS/SSRF controls, bounded response reads, CID/size binding, cancellation, cleanup, and optional-thumbnail failure behavior.
- **SCS-520:** Extend gateway contract tests with real animated WebP, parameter/mismatch/truncation/active-tail negatives, and a safe positive matrix. Update `docs/FEDERATION.md` with the shared policy and exact guarantee wording.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
```

### Phase 6 — PostgreSQL materialization and canonical-record preservation

Goal: enforce the same metadata policy when a validated ATProto blob becomes a persisted public image.

- **SCS-600:** Make `AtprotoJetstreamRepository.ApplyThumbnailAsync` use the shared MIME-to-extension and public-image metadata policy before creating `StorageObject`. Remove its SVG fallback and refuse inconsistent staged metadata without changing the canonical import.
- **SCS-610:** Extend `AtprotoInboundEventImportPersistenceTests` with safe JPEG/PNG/GIF/WebP/AVIF materialization, relabeled/truncated/active-tail rejection, zero unsafe `StorageObject` rows/links, and deep-equal original `AtprotoRecord.RecordJson`.
- **SCS-620:** Run the explicit PostgreSQL/Testcontainers lane against the configured Docker/Podman runtime. Record container cleanup and do not delete shared databases, volumes, or user data.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
```

### Phase 7 — Dead-path cleanup, architecture guard, and completion evidence

Goal: remove obsolete direct-upload code and make the boundary hard to reintroduce.

- **SCS-700:** Delete now-unreachable direct-upload commands/handlers/DTOs, legacy image upload/record client methods or classes, mapping/serialization registrations, generated-client remnants, and obsolete tests. Do not delete any provider interface still used for authenticated presigned downloads or server-owned storage operations.
- **SCS-710:** Add an Architecture regression proving the API exposes no direct provider-upload URL or caller-authored active `StorageObject` creation operation, Infrastructure uses the Application raster policy, and browser-facing storage mutations remain upload-session based.
- **SCS-720:** Reconcile all canonical runtime/contract docs, this plan/context/task ledger, ATProto plan/context/task cross-references, and the API schema/client. Append phase-verification records to `.omo/start-work/ledger.jsonl` and leave completion status pending review.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Post-phase review and closeout:

- **SCS-730:** Pin the final runtime/contract full SHA and run five independent final lanes: goal/constraint verification, context/history/scope mining, code-quality review, hands-on QA evidence review, and security/threat audit. Then run the runtime-debugging audit against the same SHA. Store reports under `.omo/evidence/storage-content-security/final-review/`.
- **SCS-740:** After all lanes approve, append completion/review records to `.omo/start-work/ledger.jsonl`, mark the synchronized progress docs complete, and register/update only the `storage-content-security` entry in `.omo/boulder.json`. Preserve unrelated work entries and `active_work_id`. A final metadata-only scope check must prove this closeout did not alter reviewed runtime/contract files; otherwise pin a new SHA and rerun applicable lanes.

## 7. Testing Strategy

### 7.1 Failing-first matrix

At minimum, the shared policy tests must prove:

| Class | Cases |
|---|---|
| MIME | exact lowercase/normalizable safe MIME; reject parameters, wildcard, SVG, BMP, HTML, empty, malformed |
| Extension | JPEG aliases accepted; PNG/GIF/WebP/AVIF exact mapping; reject missing/mismatch for public images |
| JPEG | valid baseline/progressive; reject truncation, missing EOI/scan/frame, appended active tail |
| PNG | valid framed PNG; reject missing IHDR/IDAT/IEND, duplicate/invalid critical chunks, appended active tail |
| GIF | GIF87a/GIF89a including animation; reject empty/truncated block chains and bytes after trailer |
| WebP | valid VP8/VP8L/VP8X still and valid animation; reject RIFF length mismatch, malformed ANIM/ANMF, trailing bytes |
| AVIF | valid `avif`/`avis` file type and required metadata/media boxes; reject malformed box sizes/truncation/trailing bytes |
| Metadata | only active, public, image-purpose, same-tenant, safe MIME/extension objects qualify |
| AI | reject SVG/BMP/spoofed/truncated/size mismatch before repository/provider calls; accept valid four-format subset |
| Storage | no provider write or active object on failure; quota/session failure remains consistent |
| Delivery | unsafe public rows return 404 without provider open; non-raster authenticated content is attachment |
| ATProto | invalid optional thumbnail does not fail import, stage bytes, link image, or change canonical JSON |

### 7.2 Phase allocation

| Test project | Primary coverage |
|---|---|
| `Event.Application.UnitTests` | Shared parser, storage finalizer, metadata/reference eligibility, AI ingress, content-reader decisions |
| `Event.API.IntegrationTests` | HTTP status/header/body behavior, anonymous read denial, legacy route absence |
| `Explore.Blazor.IntegrationTests` | BFF MIME/extension/session/proxy/antiforgery behavior |
| `Explore.Blazor.Client.Tests` | Event/profile/logo/AI chooser UX and no direct-upload fallback |
| `Explore.Infrastructure.Tests` | ATProto network/CID/container/cancellation/cleanup contract |
| `Event.Persistence.IntegrationTests` | PostgreSQL thumbnail materialization and `RecordJson` preservation |
| `Event.Architecture.Tests` | No direct-upload API, no parser duplication, dependency direction |

`Explore.Blazor.Client.Tests` is changed during Phase 4 but is not a second phase gate. Run it once after Phase 4 changes settle and before the Phase 7 final SHA is pinned:

```bash
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

Targeted selectors may be used during red/green work. The project-level phase command remains the acceptance command and must have nonzero test execution.

### 7.3 Runtime/PostgreSQL exception

The implementation-plan default avoids Docker/manual runtime work, but the user explicitly requires the ATProto PostgreSQL persistence lane and runtime-debugging audit. Phase 6 therefore runs the existing Testcontainers project only; it does not start the full Aspire application, a browser, or a manual UI surface. If no supported container runtime is reachable, record the exact blocker and do not mark Phase 6 or Boulder complete.

## 8. Documentation, Configuration, and Operations

Update in the phase that owns the behavior:

- `docs/SECURITY-MODEL.md`: safe-raster trust boundary, AI/storage/reference/public-read rules, attachment delivery, exact guarantee wording.
- `docs/API.md`: public image eligibility, authenticated attachment behavior, upload-session-only write contract.
- `docs/API_CHANGELOG.md`: breaking removal of legacy upload URL and caller-authored metadata operations.
- `docs/API_CONTRACT_INVENTORY.md`: regenerated storage operations and authorization/tenant classifications.
- `docs/BLAZOR.md`: browser four-format subset and BFF session/proxy enforcement.
- `docs/FEDERATION.md`: shared safe-raster policy, AVIF support, optional-thumbnail behavior, canonical JSON preservation.
- `schemas/openapi_islamu-event.json` and generated Blazor client: regenerate after controller changes.
- `dev/active/storage-content-security/*`: keep plan/context/tasks synchronized.
- `dev/active/atproto-auth/*` and `.omo/plans/atproto-auth.md`: add a completion cross-reference only; do not revise historical evidence claims.
- `.omo/start-work/ledger.jsonl`: append start, task, verification, review, and completion records; never rewrite existing lines.
- `.omo/boulder.json`: update only the matching work entry and preserve unrelated active state.

No new environment variable, provider configuration, secret, database migration, or deployment topology is planned.

Deployment order:

1. deploy Application/API public-read fail-closed logic and legacy route removal with the shared policy;
2. deploy BFF/client contract regeneration in the same release;
3. existing unsafe `public_image` rows become non-readable when MIME/purpose metadata is ineligible;
4. operators may identify affected rows through bounded metadata queries, but the application does not auto-delete provider objects;
5. re-upload is the supported repair for an ineligible legacy image.

## 9. Security and Privacy

Threats addressed:

- stored XSS/active content via SVG/HTML served from the anonymous storage origin;
- MIME spoofing and safe-prefix plus active-tail polyglots;
- post-upload metadata promotion to public image;
- direct provider upload bypassing server inspection;
- caller-authored provider keys/lifecycle/content metadata;
- image FK/reference substitution with wrong-purpose/wrong-tenant/non-active objects;
- AI provider exposure and database persistence of rejected active image input;
- raw object-key presentation/presigned inline rendering;
- drift between ATProto, normal uploads, and AI validation.

Defense layers:

- bounded input size;
- exact MIME/extension allowlist;
- complete structural container validation;
- cross-field metadata eligibility;
- safe reference eligibility;
- anonymous-read recheck;
- attachment disposition for non-raster downloads;
- CSP and `nosniff`;
- bounded, secret-safe failures and metrics.

Privacy/logging:

- Never log uploaded bytes, base64, full filenames, provider object keys, presigned URLs, CIDs beyond existing bounded federation identifiers, or raw provider responses.
- AI validation failures return a generic image-policy message and do not echo content.
- Review/evidence artifacts contain counts, MIME buckets, hashes, and test outcomes only.

Residual risk:

- Structural parsing does not detect malware or corrupted pixel streams.
- External HTTPS image URLs remain controlled by their separate URL/CSP/privacy policy.
- A user may download and locally open an authenticated active document; this plan prevents inline anonymous/same-origin rendering, not user-initiated local execution.

## 10. Cross-Cutting Classifications

| Concern | Classification |
|---|---|
| API compatibility | Breaking pre-v1 removal of two write operations and storage update byte-identity fields |
| Authorization | Existing `[Authorize]` write/download and `[AllowAnonymous]` public-image route retained |
| Tenancy | Image references require same-tenant storage metadata; anonymous ATProto materialization remains tenant-owned |
| Idempotency | Upload-session and AI idempotency behavior preserved; failure occurs before provider/repository mutation |
| Transactions | No network work inside database transactions; storage session failure/quota release semantics preserved |
| Persistence | No schema change; PostgreSQL tests cover materialization behavior |
| Caching | Public image 7-day response cache remains only for eligible safe raster; unsafe rows return 404 |
| HAL | Removed operations disappear from links; clients continue using server-authored affordances |
| Generated contracts | OpenAPI and `EventApiClient.g.cs` regenerated after route/DTO removal |
| Accessibility | File chooser labels/errors remain; accepted-format text is updated consistently |
| Localization | Reuse existing user-safe messages or add resource-backed strings if the touched surface already uses localization |
| Federation | CID/size/SSRF/fail-soft behavior retained; only parser ownership changes |

## 11. Observability

- Reuse `BusinessMetrics.RecordStorageUploadSession`, `RecordStorageUploadBytes`, `RecordStorageRead`, and existing failure-code dimensions.
- Add bounded failure codes only where current metrics need to distinguish:
  `unsafe_raster_metadata`, `unsafe_raster_container`, and `unsafe_public_image`.
- Do not add high-cardinality labels such as MIME strings outside an approved small bucket set, filenames, tenant IDs, object IDs, CIDs, or provider errors.
- BFF warnings continue to record status/failure category only.
- Runtime audit must observe:
  - zero provider writes for rejected content;
  - zero unsafe storage rows/links;
  - successful safe-raster write/read;
  - canonical `RecordJson` unchanged;
  - attachment/public response headers;
  - no orphan test containers after the PostgreSQL lane.

## 12. Migration and Compatibility

- No EF migration is planned.
- Existing eligible JPEG/PNG/GIF/WebP/AVIF rows continue to serve when metadata is coherent.
- Existing SVG/BMP/HTML/general `public_image` rows fail closed at `/public`.
- Existing raw object-key image references no longer receive a presigned browser URL; rebind them to a metadata-backed storage object.
- Existing direct-upload API consumers must migrate to:
  1. `POST /api/storageobject/upload-sessions`;
  2. `PUT /api/storageobject/upload-sessions/{id}/content`;
  3. the returned storage-object ID/public URL.
- Existing storage update clients may update display/ownership/access fields only; byte identity is immutable.
- The project is pre-v1, but route/shape removal still requires plan approval, changelog documentation, schema regeneration, and generated-client convergence.
- Historical ATProto `RecordJson` remains untouched. Rejected legacy thumbnails are not rewritten into producer records.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Shared parser rejects valid raster variants | Medium | Medium | Positive fixtures for progressive JPEG, animated GIF/WebP, AVIF sequence brand, and existing gateway corpus |
| Full raster buffering increases memory pressure | Medium | High | Bound by reserved size/route policy, read one sentinel byte, reject overflow, retain streaming for non-raster |
| Route removal breaks unknown external clients | Medium | Medium | Explicit approval, pre-v1 changelog, OpenAPI regeneration, migration instructions |
| Image-reference hardening reveals existing invalid rows | Medium | Medium | Fail closed, document re-upload/rebind repair, no destructive cleanup |
| Metadata update split-group bypass | Medium | High | Validate merged entity state in handler, not DTO groups independently |
| Presigned storage origin ignores disposition | Low | High | Set provider response header override and integration/unit-test request construction |
| ATProto extraction weakens cancellation/cleanup/SSRF | Low | High | Limit gateway diff to parser calls; retain existing full gateway contract suite |
| Persistence materializer accepts staged/declared mismatch | Low | High | Recheck shared metadata policy before `StorageObject` creation and cover PostgreSQL negative cases |
| Dirty worktree conflict | High | Medium | Record scoped status before each phase, preserve unrelated edits, stop on direct overlap |
| Container runtime unavailable | Medium | Medium | Record blocker; do not claim persistence/final completion or mutate shared runtime state |
| Graph remains unavailable | Medium | Low | Record failure and use narrow `rg`/LSP fallback; rerun impact detection before final review if restored |

## 14. Success Criteria and Definition of Done

The work is complete only when:

- One Application safe-raster policy owns exact MIME, extension, container, metadata, and reference eligibility.
- JPEG/PNG/GIF/WebP/AVIF structural tests include positive, mismatch, truncation, and active-tail cases.
- Valid animated WebP passes.
- Storage finalization performs complete raster validation before provider write/active metadata.
- `public_image` and image-purpose cross-field violations fail at create/update/materialization.
- Every known Event/Actor/User/Organization image reference requires an eligible same-tenant safe public image.
- AI SVG/BMP/spoofed/truncated/mismatched content is rejected before persistence/provider use.
- ATProto uses the shared policy and preserves optional-thumbnail fail-soft behavior and original `RecordJson`.
- Anonymous storage reads serve only eligible safe raster.
- Authenticated non-raster content and presigned downloads use attachment disposition.
- Raw object keys are not signed as browser image presentation URLs.
- Legacy direct-upload and caller-authored metadata operations are absent from controllers, HAL, OpenAPI, generated client, and Architecture tests.
- All seven phase builds/tests pass with nonzero test execution.
- `Explore.Blazor.Client.Tests` passes after Phase 4.
- The PostgreSQL/Testcontainers lane passes and leaves no test containers.
- Five final review lanes plus runtime-debugging audit approve the same pinned final SHA.
- Canonical docs, the three workstream artifacts, ATProto cross-references, immutable ledger, and Boulder entry are synchronized.
- No unrelated worktree changes are modified or claimed.

## 15. Implementation Agent Contract

Before runtime edits:

1. Read this plan, context, and tasks file fully.
2. Re-read `AGENTS.md`, the fallback intent entries, required docs, matching rules, and relevant skills.
3. Record branch, full SHA, full/scoped `git status --short`, and in-scope pre-existing changes.
4. Invoke the code-review graph first; if unavailable, record the failure and use narrow LSP/`rg` fallback.
5. Run the canonical Release build once as the implementation baseline.
6. Add or update the `storage-content-security` Boulder work entry without changing unrelated `active_work_id`.

During implementation:

- Follow task order unless new evidence requires a documented plan decision.
- Write failing tests before each security behavior.
- Update `storage-content-security-tasks.md` immediately after task/gate completion.
- Update context after any changed decision, blocker, surprising behavior, or handoff.
- Update the stable plan only when scope/architecture changes.
- Keep tests/docs in the same task as their behavior.
- Use `apply_patch` for manual file edits.
- Do not weaken or delete a failing security test.
- Do not introduce compatibility shims, new packages, or abstractions without recording why the approved design cannot work.
- Do not run provider/network work inside a database transaction.
- Do not expose secrets or user content in logs/evidence.

Before completion:

1. Ensure the last phase performed the final root Release build.
2. Run the cumulative Blazor client project once.
3. Verify all phase commands executed nonzero tests.
4. Pin the final full SHA and do not mutate it during review.
5. Run the five final review lanes plus runtime-debugging audit against that SHA.
6. Repair findings test-first; any repair creates a new SHA and invalidates all final-lane approvals.
7. Synchronize docs, ledgers, ATProto cross-references, evidence, and Boulder.
8. Mark complete only when no required checkbox remains.

## 16. Progress Reporting

- `storage-content-security-tasks.md` is the hot execution ledger.
- `storage-content-security-context.md` is the current evidence/handoff source.
- This plan is stable and changes only for approved architecture/scope decisions.
- Each ledger entry records date/timezone, task ID, exact command, outcome/count, SHA, and artifact path when applicable.
- `.omo/start-work/ledger.jsonl` is append-only. Never rewrite prior ATProto or other workstream events.
- Final review reports live under `.omo/evidence/storage-content-security/final-review/` and identify the exact full SHA.
- `.omo/boulder.json` tracks only coarse work status; it is not a substitute for the task/evidence ledger.

## 17. Risks and Open Unknowns

- Confirm during `SCS-310` that the configured S3-compatible SDK/provider supports response content-disposition overrides on presigned GETs. If not, remove the presigned download affordance for non-raster content rather than returning an inline-active URL.
- Confirm every image FK assignment with graph/LSP references before `SCS-230`; the planning inventory found Event, nested EventSession, Actor, User, and Organization paths, but future/concurrent code may add another consumer.
- Confirm generated OpenAPI source of truth (`schemas/openapi_islamu-event.json` versus any transitional `schemas/openapi.json`) using the documented generation command before editing generated files.
- Confirm whether external consumers use the two legacy write operations. This does not change the recommended removal, but it affects release notes and migration communication.
- Confirm real animated WebP fixture licensing/source; use a tiny repository-owned generated fixture or an existing permissively licensed corpus and record provenance.
- If legitimate safe-raster inputs exceed the current image route limit, change the existing storage route policy through its normal governance path; do not add an unbounded parser exception.
