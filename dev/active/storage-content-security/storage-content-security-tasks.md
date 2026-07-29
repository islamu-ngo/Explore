<!-- ABOUTME: Live task and evidence ledger for reusable safe-raster and storage content-security implementation. -->
<!-- ABOUTME: Tracks phase ownership, test-first gates, reviews, documentation, immutable ledger, and Boulder completion. -->

# Reusable Safe-Raster and Storage Content Boundary — Tasks

Last Updated: 2026-07-29 Europe/Brussels

## Status

- Overall: implementation active; Waves 1 through 7 are independently confirmed.
- Completed: 28 of 30 implementation tasks.
- Current phase: Final review and completion closeout.
- Current task: `SCS-730`.
- Approval blocker: none; the approved pre-v1 legacy route removal and contract regeneration are complete.
- Runtime blocker: none for the storage-security scope; the expanded Wave 6 B1
  PostgreSQL selectors pass. Broad Release/Persistence gates remain unaccepted
  because of unrelated concurrent pricing/schema failures.
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

- [x] Record branch and full starting SHA.
- [x] Record full and scoped `git status --short`.
- [x] Identify all pre-existing in-scope changes and their owner.
- [x] Read plan/context/tasks and required contract/rule/skill files.
- [x] Retry code-review graph impact/test discovery; record fallback if unavailable.
- [x] Run the canonical root Release build before runtime edits.
- [x] Add/update the `storage-content-security` Boulder work entry without changing unrelated `active_work_id`.

Baseline evidence:

```text
Planning session, 2026-07-29:
- No runtime implementation or implementation test suite was run.
- Code-review graph: attempted first; failed with `Transport closed`.
- Worktree: dirty with unrelated Event/Actor/Studio/API/docs changes.
- Planning artifact verification is recorded at the end of this file.

Implementation baseline, 2026-07-29:
- Branch/SHA: `develop` at `2f0426ed0530bfad3655715acd40e6f3d87fbe00`.
- Full and storage-scoped status: clean at capture time; no pre-existing in-scope changes required attribution.
- Graph retry: seam resolved, but the index was stale (`2de073f...`).
- `rtk dotnet build --configuration Release --verbosity quiet`: PASS, 26 projects, 0 errors, 10,608 pre-existing `NU1903` warning occurrences, 00:00:47.56.
- Evidence: `.omo/evidence/storage-content-security/baseline/verification.md`.
- Boulder: `works.storage-content-security` active; unrelated `active_work_id` preserved.
```

## Phase 1 — Shared Safe-Raster Policy

### SCS-100 — Failing-first parser and MIME matrix

- [x] Add tests for exact parameter-free JPEG/PNG/GIF/WebP/AVIF MIME.
- [x] Add MIME/extension match and mismatch tests.
- [x] Add valid baseline/progressive JPEG controls.
- [x] Add valid PNG/GIF/still-WebP/animated-WebP/AVIF controls.
- [x] Add truncated/malformed container cases.
- [x] Add MIME-spoofed bytes for every supported MIME.
- [x] Add safe-prefix plus active SVG/HTML tail cases.
- [x] Assert exact EOF and no trailing bytes.
- [x] Confirm the red tests fail for the intended missing shared policy/weak prefix behavior.

### SCS-110 — Application-owned policy

- [x] Add `SafeRasterContentPolicy` with two `ABOUTME:` lines.
- [x] Move the existing container parsers inward without adding a package/interface/factory.
- [x] Add the minimal exact MIME, extension, container, metadata, and reference APIs.
- [x] Support valid animated WebP `ANIM`/`ANMF` framing.
- [x] Describe the guarantee as structural framing, not decoding/sanitization.

### SCS-120 — Storage signature integration

- [x] Make raster inspection read a bounded full container plus one overflow sentinel.
- [x] Return a replayable bounded stream to the provider.
- [x] Preserve non-seekable stream behavior.
- [x] Preserve non-raster document-prefix inspection/streaming.
- [x] Reject overflow, early EOF, mismatch, truncation, and trailing active content before provider write.
- [x] Preserve quota/session failure and cleanup behavior.

### SCS-130 — Phase 1 docs

- [x] Update the safe-raster guarantee in `docs/SECURITY-MODEL.md`.
- [x] State exact MIME and structural-EOF limits accurately.
- [x] State decoder/malware/pixel semantics are out of scope.

### Phase 1 gate

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- [x] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
Confirmed 2026-07-29 at shared HEAD d1cd1ae2edfbd32322f93fa6f7a47e8365d61e44:
- RED: the new exact-container matrix failed against the legacy prefix-only behavior.
- GREEN: policy matrix 19/19; upload-finalizer class 29/29.
- Permanent controls include baseline/progressive JPEG, PNG, GIF, still and real
  two-frame animated WebP, AVIF, five MIME/container spoof cases, and five
  safe-container plus active-tail cases.
- Release build: 26 projects, 0 errors.
- Full Application project: 3323/3326 passed; three deterministic current-HEAD
  contract failures were reproduced in isolation and confirmed unrelated to
  Wave 1 (PublishEvent ordering, UpdateOrganization authorization expectation,
  and EventLocationDisclosure record-shape contract).
- Manual built-assembly probe: valid_png=True; safe_prefix_svg_tail=False.
- Executor: `.omo/evidence/storage-content-security/wave1/DONE_CLAIM.md`.
- Independent gate: `.omo/evidence/storage-content-security/wave1/ADVERSARIAL_VERIFY.md`
  with verdict `confirmed`.
- Temporary parser/debug fixtures and journal were removed.
```

## Phase 2 — Storage Metadata, Image References, and AI Ingress

### SCS-200 — Upload-session cross-field policy

- [x] Reject `public_image` unless MIME/extension is a safe raster and purpose is image-capable.
- [x] Reject image purposes with non-safe MIME/extension.
- [x] Fail before quota reservation/provider work where metadata alone is invalid.
- [x] Retain byte inspection before active object creation.
- [x] Add safe positive controls for all server-supported raster MIME types.

### SCS-210 — Storage update hardening

- [x] Remove byte-identity fields from client-editable update DTOs.
- [x] Keep display-name/ownership fields only where valid.
- [x] Validate the merged existing-plus-patch entity before visibility/purpose updates.
- [x] Reject attachment/document/general objects promoted to `public_image`.
- [x] Reject split metadata/access update bypasses.

### SCS-220 — Read and presigned eligibility

- [x] Require active/public/image-purpose/safe MIME+extension for anonymous reads.
- [x] Prove denial happens before provider open.
- [x] Return sanitized display name and attachment decision for authenticated delivery.
- [x] Reuse eligibility in presigned-download authorization/response construction.
- [x] Keep owner/tenant/authenticated visibility semantics intact.

### SCS-230 — Image-reference eligibility

- [x] Refresh graph/LSP references for every `StorageObject` image FK assignment.
- [x] Harden Event featured image.
- [x] Harden Event background image.
- [x] Harden nested EventSession image references.
- [x] Harden Event draft image references.
- [x] Harden Actor create/update profile images.
- [x] Harden User profile picture updates.
- [x] Harden Organization logo/profile picture creation/update paths.
- [x] Require active, same-tenant, public, safe-raster metadata.
- [x] Return validation failure instead of silently ignoring an invalid reference.

### SCS-240 — AI authoritative image validation

- [x] Restrict server AI image input to exact JPEG/PNG/GIF/WebP.
- [x] Decode base64 once in the authoritative validation path.
- [x] Require declared `SizeBytes` to equal decoded bytes when supplied.
- [x] Validate filename extension when supplied.
- [x] Validate complete container before serializing message JSON.
- [x] Prove rejected content causes no conversation/message persistence.
- [x] Prove rejected content is not added to provider prompt images.
- [x] Reuse the policy in proposed Event image materialization.

### SCS-250 — Phase 2 docs/tests

- [x] Add Application tests for every changed handler/validator/reader path.
- [x] Update storage, image-reference, and AI boundaries in `docs/SECURITY-MODEL.md`.

### Phase 2 gate

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- [x] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
Confirmed 2026-07-29 at shared HEAD 44804ee34709805c94d14f7be82e340297e91d60
plus the independently reviewed working-tree test/API compile-consumer fixes:
- Storage-core focused selectors: 81/81.
- AI selectors: 508/508.
- Image-reference selectors: 54/54, including 18 new invocations across
  Event/Session/Series/Day, Actor/User, Group/Organization, and AI boundaries.
- Full Application project: 3378/3381 passed; the only failures are the same
  three deterministic unrelated current-HEAD contract failures documented in
  Wave 1.
- Release build: 26 projects, 0 errors.
- Manual probe: unsafe_public_html=False; safe_public_avif=True;
  editable_byte_identity=False; valid reference/AI=True; cross-tenant and
  active-tail=False.
- Executor evidence:
  `.omo/evidence/storage-content-security/wave2/storage-core/DONE_CLAIM.md`
  and `.omo/evidence/storage-content-security/wave2/images-ai/DONE_CLAIM.md`.
- Independent gates:
  `.omo/evidence/storage-content-security/wave2/storage-core/ADVERSARIAL_VERIFY.md`
  and `.omo/evidence/storage-content-security/wave2/images-ai/ADVERSARIAL_VERIFY.md`,
  both `confirmed`.
```

## Phase 3 — API Delivery and Legacy Bypass Removal

### SCS-300 — Safe file response behavior

- [x] Serve eligible safe public raster inline.
- [x] Serve authenticated non-raster content with sanitized attachment disposition.
- [x] Preserve range processing, Last-Modified, ETag/checksum, CSP, and `nosniff`.
- [x] Prove SVG/HTML/general content cannot use the anonymous route.

### SCS-310 — Presigned and presentation URL hardening

- [x] Add provider response content-disposition override for ID-based presigned downloads.
- [x] Keep presigned responses no-store and secret-safe.
- [x] Remove raw-object-key signing from `StoragePresentationUrlResolver`.
- [x] Preserve metadata-backed `/api/storageobject/{id}/public` image URLs.
- [x] If the provider cannot enforce disposition, remove non-raster presigned affordance instead of returning inline content.

### SCS-320 — Remove legacy write operations

- [x] Confirm plan acceptance authorizes the breaking removal.
- [x] Remove `GenerateStorageObjectUploadUrl` controller operation and route.
- [x] Remove caller-authored `CreateStorageObject` controller operation and route.
- [x] Remove their HAL affordances.
- [x] Keep provider-neutral upload-session operations.
- [x] Prove removed operations are absent from endpoint inventory/OpenAPI.

### SCS-330 — API integration coverage

- [x] Unsafe public metadata returns 404.
- [x] Provider is not opened for unsafe public metadata.
- [x] Safe raster remains inline with the correct MIME.
- [x] Authenticated SVG/HTML/document/general content is attachment.
- [x] Presigned download carries disposition override/no-store behavior.
- [x] Removed legacy operations are not callable.

### SCS-340 — Contract/docs regeneration

- [x] Regenerate the canonical OpenAPI schema.
- [x] Regenerate `EventApiClient.g.cs`.
- [x] Update `docs/API.md`.
- [x] Update `docs/API_CHANGELOG.md`.
- [x] Update `docs/API_CONTRACT_INVENTORY.md`.
- [x] Confirm no stale legacy operation/client methods remain.

### Phase 3 gate

- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [x] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
SCS-340 is independently confirmed at reviewer-observed SHA
`bb84ad7a128668570655c88c9d28ea7cdb9fd833`: Release build 26 projects,
0 errors; focused API/HAL/upload-session selectors 27/27, 8/8, and 16/16;
497 paths, 644 unique operations, 644 inventory rows; zero legacy storage
operations, DTOs, client methods, or fallbacks. The cumulative full API gate
remains not accepted: 1,570/2,113 passed and 543 pre-existing/shared failures
remain, with zero new failure names and zero storage-focused failures.

Evidence:
`.omo/evidence/storage-content-security/wave3/contracts/DONE_CLAIM.md` and
`.omo/evidence/storage-content-security/wave3/contracts/ADVERSARIAL_VERIFY.md`.
```

## Phase 4 — BFF and Browser Image UX

### SCS-400 — BFF image-only contract

- [x] Restrict session requests to exact JPEG/PNG/GIF/WebP.
- [x] Require matching simple extension.
- [x] Recheck proxy form MIME against session/file declarations.
- [x] Preserve auth, antiforgery, opaque session, user, size, and consumed-session binding.
- [x] Preserve rejection of raw destinations/object keys/paths.

### SCS-410 — Persistent UI upload convergence

- [x] Confirm Event images use `ImageUploadClientPolicy` and BFF sessions only.
- [x] Confirm User profile pictures use the same path.
- [x] Confirm Organization logos use the same path.
- [x] Remove reachable direct-upload URL fallback.
- [x] Remove reachable caller-authored storage-record fallback.
- [x] Preserve preview, size, cancellation, and user-safe errors.

### SCS-420 — AI composer UX

- [x] Restrict accept list to JPEG/PNG/GIF/WebP.
- [x] Remove BMP/SVG inference.
- [x] Compare detected byte signature with declared/extension MIME after reading.
- [x] Preserve max count and max bytes.
- [x] Add bUnit negative and positive controls.

### SCS-430 — BFF/client docs and tests

- [x] Update BFF integration tests.
- [x] Update affected Blazor client/bUnit tests.
- [x] Update `docs/BLAZOR.md`.

### Phase 4 gate

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [x] Record nonzero counts, SHA, warnings/failures, and evidence.

### Phase 4 cumulative client gate

- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- [x] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
SCS-410, SCS-420, and SCS-430 are independently confirmed at SHA
`d6ac21497d104b3b7842c8bc09f181166f9cbfc3`. The final residual
`DirectStorageUploadMessageHandler`/`StorageHttpClientNames.DirectUpload`
transport and both DI registrations were removed; exact legacy browser-path
scans return zero. Release build passed with 0 errors; reader 20/20, AI 32/32,
upload 4/4, storage 12/12, focused BFF 37/37, and full BFF 398/398 passed.
The full client gate remains unaccepted with 2 named unrelated failures:
2,231 passed, 2 failed, and 1 governed skip. Canonical browser evidence
contains 15 PNGs, 15 accessibility snapshots, and 15 zero-error console logs;
both independent final visual reviews passed.

Evidence:
`.omo/evidence/storage-content-security/wave4/third-stop-verification/VERIFICATION.md`,
`.omo/evidence/storage-content-security/wave4/ADVERSARIAL_VERIFY.md`,
`.omo/evidence/storage-content-security/wave4/VISUAL_PASS_A_FINAL.md`, and
`.omo/evidence/storage-content-security/wave4/VISUAL_PASS_B_FINAL.md`.
```

## Phase 5 — ATProto Gateway Reuse

### SCS-500 — Shared policy integration

- [x] Make gateway MIME checks call the Application policy.
- [x] Make gateway container checks call the Application policy.
- [x] Retain AVIF for ATProto.
- [x] Retain candidate, response, CID, and size binding.

### SCS-510 — Remove parser duplication safely

- [x] Delete moved private MIME/container parser code.
- [x] Preserve DID/PDS resolution and SSRF protections.
- [x] Preserve redirect/DNS pinning behavior.
- [x] Preserve bounded response and timeout behavior.
- [x] Preserve cancellation propagation and staged cleanup.
- [x] Preserve optional-thumbnail fail-soft behavior.

### SCS-520 — Federation tests/docs

- [x] Add valid animated WebP.
- [x] Rerun safe five-format matrix.
- [x] Rerun MIME parameter/mismatch/truncation/active-tail matrix.
- [x] Prove zero writes on rejection.
- [x] Update `docs/FEDERATION.md`.

### Phase 5 gate

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- [x] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
SCS-500, SCS-510, and SCS-520 are independently confirmed at SHA
`d6ac21497d104b3b7842c8bc09f181166f9cbfc3`. The gateway contains
exactly three shared-policy calls and zero private raster parser hits.
Gateway tests passed 50/50; full fast Infrastructure passed 1,151/1,151;
the Application-to-Infrastructure architecture selector passed 1/1; Release
build passed with 0 errors. The safe matrix includes progressive JPEG, PNG,
GIF, still and independently decoded two-frame animated WebP, and AVIF.
Parameterized/mismatched MIME, relabeled SVG, truncation, per-format active
tails, CID/size mismatch, and provider failure all retain zero-write/cleanup
proof while SSRF, redirect/DNS, bounded-read, timeout, cancellation, and DID/PDS
binding remain Infrastructure-owned.

Evidence:
`.omo/evidence/storage-content-security/wave5/DONE_CLAIM.md` and
`.omo/evidence/storage-content-security/wave5/ADVERSARIAL_VERIFY.md`.
```

## Phase 6 — PostgreSQL Materialization and Record Preservation

### SCS-600 — Persistence materialization policy

- [x] Reuse shared MIME-to-extension mapping.
- [x] Remove SVG/unknown image extension fallback.
- [x] Recheck staged provider result against thumbnail metadata.
- [x] Require safe public-image metadata before adding `StorageObject`.
- [x] Leave canonical import successful when optional image is rejected.

### SCS-610 — PostgreSQL matrix

- [x] Safe JPEG materializes and links.
- [x] Safe PNG materializes and links.
- [x] Safe GIF materializes and links.
- [x] Safe still/animated WebP materializes and links.
- [x] Safe AVIF materializes and links.
- [x] Relabeled SVG is not stored/linked.
- [x] Truncated/active-tail content is not stored/linked.
- [x] Original `AtprotoRecord.RecordJson` deep-equals producer JSON.
- [x] Imported Event/EventSession relationships remain correct.
- [x] Rejected thumbnail leaves zero unsafe `StorageObject` rows.

### SCS-620 — Container runtime evidence

- [x] Preflight the supported Docker/Podman runtime.
- [x] Run the project gate through existing Testcontainers fixtures.
- [x] Record nonzero counts and TRX/log evidence.
- [x] Confirm no test container remains.
- [x] Do not remove shared containers, databases, volumes, or backups.

### Phase 6 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [x] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
SCS-600, SCS-610, and SCS-620 are independently confirmed at SHA
`d6ac21497d104b3b7842c8bc09f181166f9cbfc3`. Fresh PostgreSQL results:
staged adversarial 10/10, cleanup 5/5, handler surface 9/9, safe metadata
5/5, replacement/replay 1/1, and architecture 7/7. Accepted JPEG, PNG,
GIF, still/animated WebP, and AVIF materialize and link; SVG, parameterized
or mismatched MIME, truncation/active tails, size/checksum/CID mismatch, and
blank/whitespace provider/object key preserve exact `RecordJson` and the
Event/EventSession graph with zero unsafe row/link/outbox effects. Rejected
stages are cleaned exactly once. No owned container/process/temp directory
remains. Broad Release and full Persistence gates remain explicitly
unaccepted due unrelated concurrent pricing/schema failures.

Evidence:
`.omo/evidence/storage-content-security/wave6/DONE_CLAIM.md`,
`.omo/evidence/storage-content-security/wave6/ADVERSARIAL_VERIFY.md`, and
`.omo/evidence/storage-content-security/wave6/ADVERSARIAL_VERIFY_FINAL.md`.
```

## Phase 7 — Cleanup, Architecture, and Completion

### SCS-700 — Delete dead legacy code

- [x] Delete unused direct-upload commands/handlers/DTOs.
- [x] Delete obsolete mapping/serialization registrations.
- [x] Delete obsolete direct image upload/record client methods/classes.
- [x] Delete stale generated-client operations through regeneration, not manual generated-file surgery.
- [x] Delete obsolete tests only when their production surface is removed; retain/replace security assertions.
- [x] Confirm provider services still used by safe server-owned operations remain.

### SCS-710 — Architecture regression

- [x] Assert no direct provider-upload URL API operation exists.
- [x] Assert no caller-authored active storage-metadata create API exists.
- [x] Assert Infrastructure consumes the Application raster policy.
- [x] Assert browser storage writes remain upload-session based.
- [x] Assert no second production container parser remains.

### SCS-720 — Docs and ledgers

- [x] Reconcile plan/context/tasks.
- [x] Add completion cross-reference to the logical archived ATProto plan/context/tasks.
- [x] Reconcile API/BLAZOR/FEDERATION/SECURITY docs.
- [x] Reconcile OpenAPI/generated client.
- [x] Append task/gate events to `.omo/start-work/ledger.jsonl`.
- [x] Leave completion status pending final review.
- [x] Do not rewrite historical ledger lines/evidence.

### Phase 7 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- [x] Record nonzero counts, SHA, warnings/failures, and evidence.

Evidence:

```text
SCS-700 and SCS-710 are independently confirmed at SHA
`d6ac21497d104b3b7842c8bc09f181166f9cbfc3`. Deletion audit found no
reachable legacy production artifact. Six bounded architecture checks passed
6/6 twice and cover OpenAPI/generated/source absence, shared policy ownership
with no Infrastructure parser, upload-session-only browser mutation, static
SVG presentation outside upload allowlists, and retained safe server-owned
download/finalize/delete/reconcile operations. Full Architecture remains
unaccepted at 329 passed, 9 unrelated failed, 1 governed skip; Release build
remains unaccepted with 8 unrelated pricing/federation errors.

Evidence:
`.omo/evidence/storage-content-security/wave7/cleanup-architecture/DONE_CLAIM.md`
and
`.omo/evidence/storage-content-security/wave7/cleanup-architecture/ADVERSARIAL_VERIFY.md`.

SCS-720 is independently confirmed at the same observed SHA. OpenAPI and
inventory contain the same 644 operation IDs, zero legacy storage operations,
and all ten retained storage operations; the generated client remains
deterministic. API, Blazor, Federation, Security, and logical archived ATProto
progress docs agree on the single policy, browser/server format split,
exact-EOF guarantee, static SVG distinction, delivery modes, staged CID
validation, and exact `RecordJson` preservation. Focused architecture passed
6/6. The append-only ledger contains a reviewed SCS-720 gate-ready event;
completion remains pending SCS-730/SCS-740.

Evidence:
`.omo/evidence/storage-content-security/wave7/docs/DONE_CLAIM.md`,
`.omo/evidence/storage-content-security/wave7/docs/ADVERSARIAL_VERIFY.md`, and
`.omo/evidence/storage-content-security/wave7/docs/ADVERSARIAL_VERIFY_FINAL.md`.
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
