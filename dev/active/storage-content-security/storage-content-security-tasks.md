<!-- ABOUTME: Live task and evidence ledger for reusable safe-raster and storage content-security implementation. -->
<!-- ABOUTME: Tracks phase ownership, test-first gates, reviews, documentation, immutable ledger, and Boulder completion. -->

# Reusable Safe-Raster and Storage Content Boundary — Tasks

Last Updated: 2026-07-29 Europe/Brussels

## Status

- Overall: planning complete; implementation not started.
- Completed: 0 of 30 implementation tasks.
- Current phase: Phase 1 — Shared safe-raster policy.
- Current task: `SCS-100`.
- Approval blocker: legacy route removal in `SCS-320` requires plan acceptance.
- Runtime blocker: unknown until the Phase 6 container-runtime preflight.
- Review status: not started; no final SHA is pinned.

## Maintenance Rules

- This is the hot execution ledger; update it after every task, gate, blocker, or review result.
- Check a task only when its code, colocated tests, docs, and evidence are complete.
- Record exact commands, nonzero test counts, exit status, full SHA, and evidence path.
- Keep `storage-content-security-context.md` current after discoveries and handoffs.
- Change the plan only for architecture/scope decisions.
- Preserve unrelated worktree changes.
- Never record uploaded bytes, base64, filenames, object keys, presigned URLs, secrets, tokens, PII, or raw provider errors.
- A repair after final SHA pinning invalidates every final review checkbox.

## Baseline Checklist

- [ ] Record branch and full starting SHA.
- [ ] Record full and scoped `git status --short`.
- [ ] Identify all pre-existing in-scope changes and their owner.
- [ ] Read plan/context/tasks and required contract/rule/skill files.
- [ ] Retry code-review graph impact/test discovery; record fallback if unavailable.
- [ ] Run the canonical root Release build before runtime edits.
- [ ] Add/update the `storage-content-security` Boulder work entry without changing unrelated `active_work_id`.

Baseline evidence:

```text
Planning session, 2026-07-29:
- No runtime implementation or implementation test suite was run.
- Code-review graph: attempted first; failed with `Transport closed`.
- Worktree: dirty with unrelated Event/Actor/Studio/API/docs changes.
- Planning artifact verification is recorded at the end of this file.
```

## Phase 1 — Shared Safe-Raster Policy

### SCS-100 — Failing-first parser and MIME matrix

- [ ] Add tests for exact parameter-free JPEG/PNG/GIF/WebP/AVIF MIME.
- [ ] Add MIME/extension match and mismatch tests.
- [ ] Add valid baseline/progressive JPEG controls.
- [ ] Add valid PNG/GIF/still-WebP/animated-WebP/AVIF controls.
- [ ] Add truncated/malformed container cases.
- [ ] Add MIME-spoofed bytes for every supported MIME.
- [ ] Add safe-prefix plus active SVG/HTML tail cases.
- [ ] Assert exact EOF and no trailing bytes.
- [ ] Confirm the red tests fail for the intended missing shared policy/weak prefix behavior.

### SCS-110 — Application-owned policy

- [ ] Add `SafeRasterContentPolicy` with two `ABOUTME:` lines.
- [ ] Move the existing container parsers inward without adding a package/interface/factory.
- [ ] Add the minimal exact MIME, extension, container, metadata, and reference APIs.
- [ ] Support valid animated WebP `ANIM`/`ANMF` framing.
- [ ] Describe the guarantee as structural framing, not decoding/sanitization.

### SCS-120 — Storage signature integration

- [ ] Make raster inspection read a bounded full container plus one overflow sentinel.
- [ ] Return a replayable bounded stream to the provider.
- [ ] Preserve non-seekable stream behavior.
- [ ] Preserve non-raster document-prefix inspection/streaming.
- [ ] Reject overflow, early EOF, mismatch, truncation, and trailing active content before provider write.
- [ ] Preserve quota/session failure and cleanup behavior.

### SCS-130 — Phase 1 docs

- [ ] Update the safe-raster guarantee in `docs/SECURITY-MODEL.md`.
- [ ] State exact MIME and structural-EOF limits accurately.
- [ ] State decoder/malware/pixel semantics are out of scope.

### Phase 1 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- [ ] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
Pending.
```

## Phase 2 — Storage Metadata, Image References, and AI Ingress

### SCS-200 — Upload-session cross-field policy

- [ ] Reject `public_image` unless MIME/extension is a safe raster and purpose is image-capable.
- [ ] Reject image purposes with non-safe MIME/extension.
- [ ] Fail before quota reservation/provider work where metadata alone is invalid.
- [ ] Retain byte inspection before active object creation.
- [ ] Add safe positive controls for all server-supported raster MIME types.

### SCS-210 — Storage update hardening

- [ ] Remove byte-identity fields from client-editable update DTOs.
- [ ] Keep display-name/ownership fields only where valid.
- [ ] Validate the merged existing-plus-patch entity before visibility/purpose updates.
- [ ] Reject attachment/document/general objects promoted to `public_image`.
- [ ] Reject split metadata/access update bypasses.

### SCS-220 — Read and presigned eligibility

- [ ] Require active/public/image-purpose/safe MIME+extension for anonymous reads.
- [ ] Prove denial happens before provider open.
- [ ] Return sanitized display name and attachment decision for authenticated delivery.
- [ ] Reuse eligibility in presigned-download authorization/response construction.
- [ ] Keep owner/tenant/authenticated visibility semantics intact.

### SCS-230 — Image-reference eligibility

- [ ] Refresh graph/LSP references for every `StorageObject` image FK assignment.
- [ ] Harden Event featured image.
- [ ] Harden Event background image.
- [ ] Harden nested EventSession image references.
- [ ] Harden Event draft image references.
- [ ] Harden Actor create/update profile images.
- [ ] Harden User profile picture updates.
- [ ] Harden Organization logo/profile picture creation/update paths.
- [ ] Require active, same-tenant, public, safe-raster metadata.
- [ ] Return validation failure instead of silently ignoring an invalid reference.

### SCS-240 — AI authoritative image validation

- [ ] Restrict server AI image input to exact JPEG/PNG/GIF/WebP.
- [ ] Decode base64 once in the authoritative validation path.
- [ ] Require declared `SizeBytes` to equal decoded bytes when supplied.
- [ ] Validate filename extension when supplied.
- [ ] Validate complete container before serializing message JSON.
- [ ] Prove rejected content causes no conversation/message persistence.
- [ ] Prove rejected content is not added to provider prompt images.
- [ ] Reuse the policy in proposed Event image materialization.

### SCS-250 — Phase 2 docs/tests

- [ ] Add Application tests for every changed handler/validator/reader path.
- [ ] Update storage, image-reference, and AI boundaries in `docs/SECURITY-MODEL.md`.

### Phase 2 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- [ ] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
Pending.
```

## Phase 3 — API Delivery and Legacy Bypass Removal

### SCS-300 — Safe file response behavior

- [ ] Serve eligible safe public raster inline.
- [ ] Serve authenticated non-raster content with sanitized attachment disposition.
- [ ] Preserve range processing, Last-Modified, ETag/checksum, CSP, and `nosniff`.
- [ ] Prove SVG/HTML/general content cannot use the anonymous route.

### SCS-310 — Presigned and presentation URL hardening

- [ ] Add provider response content-disposition override for ID-based presigned downloads.
- [ ] Keep presigned responses no-store and secret-safe.
- [ ] Remove raw-object-key signing from `StoragePresentationUrlResolver`.
- [ ] Preserve metadata-backed `/api/storageobject/{id}/public` image URLs.
- [ ] If the provider cannot enforce disposition, remove non-raster presigned affordance instead of returning inline content.

### SCS-320 — Remove legacy write operations

- [ ] Confirm plan acceptance authorizes the breaking removal.
- [ ] Remove `GenerateStorageObjectUploadUrl` controller operation and route.
- [ ] Remove caller-authored `CreateStorageObject` controller operation and route.
- [ ] Remove their HAL affordances.
- [ ] Keep provider-neutral upload-session operations.
- [ ] Prove removed operations are absent from endpoint inventory/OpenAPI.

### SCS-330 — API integration coverage

- [ ] Unsafe public metadata returns 404.
- [ ] Provider is not opened for unsafe public metadata.
- [ ] Safe raster remains inline with the correct MIME.
- [ ] Authenticated SVG/HTML/document/general content is attachment.
- [ ] Presigned download carries disposition override/no-store behavior.
- [ ] Removed legacy operations are not callable.

### SCS-340 — Contract/docs regeneration

- [ ] Regenerate the canonical OpenAPI schema.
- [ ] Regenerate `EventApiClient.g.cs`.
- [ ] Update `docs/API.md`.
- [ ] Update `docs/API_CHANGELOG.md`.
- [ ] Update `docs/API_CONTRACT_INVENTORY.md`.
- [ ] Confirm no stale legacy operation/client methods remain.

### Phase 3 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
Pending.
```

## Phase 4 — BFF and Browser Image UX

### SCS-400 — BFF image-only contract

- [ ] Restrict session requests to exact JPEG/PNG/GIF/WebP.
- [ ] Require matching simple extension.
- [ ] Recheck proxy form MIME against session/file declarations.
- [ ] Preserve auth, antiforgery, opaque session, user, size, and consumed-session binding.
- [ ] Preserve rejection of raw destinations/object keys/paths.

### SCS-410 — Persistent UI upload convergence

- [ ] Confirm Event images use `ImageUploadClientPolicy` and BFF sessions only.
- [ ] Confirm User profile pictures use the same path.
- [ ] Confirm Organization logos use the same path.
- [ ] Remove reachable direct-upload URL fallback.
- [ ] Remove reachable caller-authored storage-record fallback.
- [ ] Preserve preview, size, cancellation, and user-safe errors.

### SCS-420 — AI composer UX

- [ ] Restrict accept list to JPEG/PNG/GIF/WebP.
- [ ] Remove BMP/SVG inference.
- [ ] Compare detected byte signature with declared/extension MIME after reading.
- [ ] Preserve max count and max bytes.
- [ ] Add bUnit negative and positive controls.

### SCS-430 — BFF/client docs and tests

- [ ] Update BFF integration tests.
- [ ] Update affected Blazor client/bUnit tests.
- [ ] Update `docs/BLAZOR.md`.

### Phase 4 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] Record nonzero counts, SHA, warnings/failures, and evidence.

### Phase 4 cumulative client gate

- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
Pending.
```

## Phase 5 — ATProto Gateway Reuse

### SCS-500 — Shared policy integration

- [ ] Make gateway MIME checks call the Application policy.
- [ ] Make gateway container checks call the Application policy.
- [ ] Retain AVIF for ATProto.
- [ ] Retain candidate, response, CID, and size binding.

### SCS-510 — Remove parser duplication safely

- [ ] Delete moved private MIME/container parser code.
- [ ] Preserve DID/PDS resolution and SSRF protections.
- [ ] Preserve redirect/DNS pinning behavior.
- [ ] Preserve bounded response and timeout behavior.
- [ ] Preserve cancellation propagation and staged cleanup.
- [ ] Preserve optional-thumbnail fail-soft behavior.

### SCS-520 — Federation tests/docs

- [ ] Add valid animated WebP.
- [ ] Rerun safe five-format matrix.
- [ ] Rerun MIME parameter/mismatch/truncation/active-tail matrix.
- [ ] Prove zero writes on rejection.
- [ ] Update `docs/FEDERATION.md`.

### Phase 5 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
Pending.
```

## Phase 6 — PostgreSQL Materialization and Record Preservation

### SCS-600 — Persistence materialization policy

- [ ] Reuse shared MIME-to-extension mapping.
- [ ] Remove SVG/unknown image extension fallback.
- [ ] Recheck staged provider result against thumbnail metadata.
- [ ] Require safe public-image metadata before adding `StorageObject`.
- [ ] Leave canonical import successful when optional image is rejected.

### SCS-610 — PostgreSQL matrix

- [ ] Safe JPEG materializes and links.
- [ ] Safe PNG materializes and links.
- [ ] Safe GIF materializes and links.
- [ ] Safe still/animated WebP materializes and links.
- [ ] Safe AVIF materializes and links.
- [ ] Relabeled SVG is not stored/linked.
- [ ] Truncated/active-tail content is not stored/linked.
- [ ] Original `AtprotoRecord.RecordJson` deep-equals producer JSON.
- [ ] Imported Event/EventSession relationships remain correct.
- [ ] Rejected thumbnail leaves zero unsafe `StorageObject` rows.

### SCS-620 — Container runtime evidence

- [ ] Preflight the supported Docker/Podman runtime.
- [ ] Run the project gate through existing Testcontainers fixtures.
- [ ] Record nonzero counts and TRX/log evidence.
- [ ] Confirm no test container remains.
- [ ] Do not remove shared containers, databases, volumes, or backups.

### Phase 6 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
Pending.
```

## Phase 7 — Cleanup, Architecture, and Completion

### SCS-700 — Delete dead legacy code

- [ ] Delete unused direct-upload commands/handlers/DTOs.
- [ ] Delete obsolete mapping/serialization registrations.
- [ ] Delete obsolete direct image upload/record client methods/classes.
- [ ] Delete stale generated-client operations through regeneration, not manual generated-file surgery.
- [ ] Delete obsolete tests only when their production surface is removed; retain/replace security assertions.
- [ ] Confirm provider services still used by safe server-owned operations remain.

### SCS-710 — Architecture regression

- [ ] Assert no direct provider-upload URL API operation exists.
- [ ] Assert no caller-authored active storage-metadata create API exists.
- [ ] Assert Infrastructure consumes the Application raster policy.
- [ ] Assert browser storage writes remain upload-session based.
- [ ] Assert no second production container parser remains.

### SCS-720 — Docs and ledgers

- [ ] Reconcile plan/context/tasks.
- [ ] Add completion cross-reference to ATProto plan/context/tasks.
- [ ] Reconcile API/BLAZOR/FEDERATION/SECURITY docs.
- [ ] Reconcile OpenAPI/generated client.
- [ ] Append task/gate events to `.omo/start-work/ledger.jsonl`.
- [ ] Leave completion status pending final review.
- [ ] Do not rewrite historical ledger lines/evidence.

### Phase 7 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
Pending.
```

## Final Review and Completion Closeout

### SCS-730 — Final review wave

- [ ] Pin and record the final full SHA.
- [ ] Goal/constraint verification approves that SHA.
- [ ] Context/history/scope mining approves that SHA.
- [ ] Code-quality review approves that SHA.
- [ ] Hands-on QA evidence review approves that SHA.
- [ ] Security/threat audit approves that SHA.
- [ ] Runtime-debugging audit approves that SHA.
- [ ] Store all reports under `.omo/evidence/storage-content-security/final-review/`.
- [ ] Confirm no mutation occurred after SHA pinning.
- [ ] If repaired, pin a new SHA and rerun all six lanes.

### SCS-740 — Completion ledgers and Boulder status

- [ ] Append final review/completion events to `.omo/start-work/ledger.jsonl`.
- [ ] Mark plan/context/tasks complete with report paths and reviewed SHA.
- [ ] Add/update only `works.storage-content-security`.
- [ ] Preserve all unrelated work entries.
- [ ] Preserve unrelated `active_work_id`.
- [ ] Mark Boulder completed only after all required gates/reviews pass.
- [ ] Run a metadata-only scope check proving no reviewed runtime/contract file changed after SHA pinning.
- [ ] If runtime/contract content changed, pin a new SHA and rerun applicable lanes.

## Definition of Done

- [ ] All 30 implementation tasks are complete.
- [ ] All seven phase Release builds pass.
- [ ] All phase project tests pass with nonzero execution.
- [ ] Blazor client cumulative test passes.
- [ ] PostgreSQL/Testcontainers evidence passes and is cleaned up.
- [ ] One reusable Application safe-raster policy remains.
- [ ] Every known upload/reference/public-delivery path is covered.
- [ ] Legacy direct-upload/caller-authored metadata surface is absent.
- [ ] ATProto `RecordJson` is preserved.
- [ ] Five final review lanes plus runtime audit approve one final SHA.
- [ ] Docs, context, task ledger, append-only ledger, evidence, and Boulder are synchronized.
- [ ] No unrelated user changes were modified or claimed.

## Planning Verification

```text
2026-07-29 Europe/Brussels:
- PASS: `git diff --check -- dev/active/storage-content-security`.
- PASS: no trailing whitespace in the three new artifacts.
- PASS: plan contains all 18 required numbered sections (0-17).
- PASS: plan and task ledger each contain the same 30 unique SCS task IDs.
- PASS: scoped status contains only the new `dev/active/storage-content-security/` artifacts.
- NOT RUN by design: implementation build/test commands are excluded from this planning workflow.
```
