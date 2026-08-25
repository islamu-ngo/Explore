<!-- ABOUTME: Records Phase 20 QR dependency provenance and the source-free representation contract. -->
<!-- ABOUTME: Preserves independent ISLAMU design while approving one minimal MIT encoder dependency. -->

# Phase 20 QR Clean-Room Evidence

Date: 2026-08-25 Europe/Brussels
Intent: Task 20.3 QR representation and clean-room encoder/decoder gate

## Source register

- NuGet package metadata and manifest for `Net.Codecrete.QrCodeGenerator` 3.1.0, https://www.nuget.org/packages/Net.Codecrete.QrCodeGenerator/3.1.0 and https://api.nuget.org/v3-flatcontainer/net.codecrete.qrcodegenerator/3.1.0/net.codecrete.qrcodegenerator.nuspec, accessed 2026-08-25.
- NuGet package metadata and manifest for comparison candidate `ZXing.Net` 0.16.11, https://www.nuget.org/packages/ZXing.Net/0.16.11 and https://api.nuget.org/v3-flatcontainer/zxing.net/0.16.11/zxing.net.nuspec, accessed 2026-08-25.
- MDN, “BarcodeDetector,” https://developer.mozilla.org/en-US/docs/Web/API/BarcodeDetector, accessed 2026-08-25.
- MDN, “BarcodeDetector.getSupportedFormats(),” https://developer.mozilla.org/en-US/docs/Web/API/BarcodeDetector/getSupportedFormats_static, accessed 2026-08-25.
- WICG, “Accelerated Shape Detection in Images,” https://wicg.github.io/shape-detection-api/, accessed 2026-08-25.
- Repository authorities: ADR-023, Task 20.3, the dependency-license validator, Blazor typed-interop conventions, and the Task 20.2 credential contract.
- Context7 MCP was unavailable in this environment. No Context7 result is claimed.

Only public package metadata, license expressions, declared dependency groups, and platform behavior were retained. Third-party source, snippets, ASTs, tests, comments, assets, and implementation structure were not supplied to implementation.

## Sanitized functional specification

### Representation

- The machine payload is exactly `islamu-admission:v1:` followed by the unpadded 43-character Base64url encoding of the 32-byte opaque admission bearer.
- The payload is ASCII, bounded to 63 characters, contains no URL, PII, tenant, ticket/order/participant identifier, amount, entitlement, role, or authorization claim.
- Parsing is ordinal and fail-closed: unknown version, wrong prefix, whitespace, non-Base64url alphabet, padding, or a decoded length other than 32 bytes is rejected without echoing the candidate.
- The payload is a credential, not a locator. It must never be placed in a URL, query string, fragment, referrer, log, exception, telemetry dimension, browser storage, persistent DOM attribute, or diagnostic string.

### Encoding

- Infrastructure renders QR Model 2 through the approved package using quartile error correction, a four-module quiet zone, opaque black foreground, and opaque white background.
- SVG output contains only the QR geometry and presentation metadata required to render it; it does not include the payload as text, comment, title, description, identifier, or data attribute.
- Repeated encoding of the same payload produces deterministic module geometry.
- The later UI must render at least 256 by 256 CSS pixels, preserve the quiet zone, avoid interpolation blur, and retain black/white 21:1 contrast in every theme and print mode.

### Decoding and fallback

- Browser camera decoding uses the native `BarcodeDetector` API only when the page is a secure context, the constructor exists, `getSupportedFormats()` succeeds, and `qr_code` is explicitly supported.
- Native detection is experimental and not Baseline; absence, rejection, disconnect, or unsupported QR format returns a typed unavailable/failure result. It never silently claims camera support.
- The ES module accepts a caller-owned image source, returns transient raw detection values, and does not write them to the DOM, storage, network, console, or telemetry.
- The typed Blazor boundary validates detected text through the repository-owned payload parser before exposing a bearer to the caller.
- Camera support is optional. Phase 21 HID keyboard and labeled manual entry paths remain first-class and invoke the same server command.
- Deterministic fixtures cover valid v1 payloads, malformed/unknown payloads, no detection, multiple detection ambiguity, insecure/unsupported browser state, and JS disconnect/cancellation.

### Accessible alternative

- QR is never the only representation. The later ticket surface presents a keyboard-selectable manual bearer alternative labeled as sensitive and provides non-color status/instructions.
- Unsupported camera behavior directs users to HID/manual paths without trapping focus or relying on sound, color, or toast-only feedback.

## Dependency decision

### Approved product dependency

- Component: `Net.Codecrete.QrCodeGenerator`
- Version: `3.1.0`, centrally pinned
- Role: direct runtime dependency of `Explore.Infrastructure`; QR encoding only
- License: SPDX `MIT`, from the authoritative NuGet manifest
- Declared dependencies: none for the supplied `net6.0` and `.NETStandard2.0` groups
- Outbound decision: approved at the repository engineering-policy level for the public
  AGPLv3 and intended alternative/commercial paths while the component remains under MIT.
  This is not legal certification of an assembled distribution; notice and SBOM inclusion
  remain release obligations.
- Obligations: retain package/license metadata in dependency notices and SBOM; do not copy package source or examples into ISLAMU code
- NuGet lock evidence: `src/Explore.Infrastructure/packages.lock.json` records direct requested/resolved version `3.1.0`.
- NuGet content hash: `2b9mz9C5vNdpwFT1o4j/1DijuNAzP0wHZo3EqJe7zIQDO3/e38NNJuQ3+kJ22oJakJtieRI0wYCwbBVUbesVTg==`.
- Policy outcome: `dotnet run .ci/scripts/validate-dependency-license-policy.cs -- .` passed for 653 unique package/version pairs; only the six pre-existing visible exceptions were reported.

### Rejected comparison

`ZXing.Net` 0.16.11 is Apache-2.0 and policy-compatible, but it adds a broad multi-format encoder/decoder surface that Task 20.3 does not need. The browser platform supplies the optional camera decoder; HID/manual input needs no image-decoder package. Rejecting ZXing.Net keeps the product dependency and attack surface smaller.

## Implementation separation, independent design, and AFC/SSO review

- Research context: the OmO lead for goal `01a0386c-ab3c-7051-9d65-168a7532dfe3`
  performed public-metadata/platform research and completed this sanitized handoff before
  starting implementation child `st_01a039ec`.
- Fresh-context boundary: the research context ended at the source-free handoff. Child
  `st_01a039ec` received the handoff, repository-owned authorities, and explicit
  no-third-party-source instructions; it did not receive research transcripts, excerpts,
  downloaded artifacts, source, examples, tests, ASTs, assets, or implementation structure.
- Implementer identity: OmO implementation child `st_01a039ec`.
- Implementer attestation: implementation used only this sanitized handoff,
  repository-native contracts, compiler feedback, and NuGet-generated metadata/locks.

- Repository anchors: ADR-023’s opaque rotatable bearer, Task 20.2’s 32-byte Base64url credential, Clean Architecture boundaries, typed Blazor module interop, HAL-only authority, and WCAG 2.2 AA rules.
- Constrained/commonplace elements: QR Model 2 geometry, error-correction levels, four-module quiet zone, the platform-defined `qr_code` format identifier, and secure-context feature detection.
- Independent choices: the `islamu-admission:v1:` envelope, strict 63-character grammar, typed failure taxonomy, module/service split, no-URL/no-persistent-DOM rule, receipt of transient detections, and explicit HID/manual continuity derive from ISLAMU requirements rather than external implementation structure.
- Discretionary naming, file layout, sequencing, contracts, errors, and tests remain repository-native.
- Implementer AFC/SSO self-check: pass; this self-check is not represented as independent approval.
- Initial independent AFC/SSO audit: `st_01a03a04`, 2026-08-25, rejected the evidence
  record because context identities, reviewer independence, and a journal link were missing;
  it found no incompatible dependency or externally derived expressive structure.
- Independent AFC/SSO re-audit: pass, reviewer `st_01a03a04`, 2026-08-25,
  confidence 0.99. The reviewer confirmed that every prior evidence blocker is closed
  and found no incompatible dependency or externally derived expressive structure.

## Provenance links

- Sanitized handoff and source register: this file.
- Intent: `.agents/contract/intents.yaml` entry `ip-clean-room-governance`.
- Workstream and task: `dev/active/registration-data-collection/registration-data-collection-plan.md`,
  Phase 20 Task 20.3.
- Architecture decision: `docs/adr/ADR-023-admission-credential-check-in-transfer-recovery.md`.
- RED evidence: `.omo/evidence/20260825-phase20/20.3-qr-red.md` and
  `.omo/evidence/20260825-phase20/20.3-qr-independent-red.md`.
- GREEN evidence: `.omo/evidence/20260825-phase20/20.3-qr-green.md`.
- Durable journal conclusion: `dev/_journal/journal.md`, heading
  `Phase 20 QR clean-room evidence needs explicit context identities`.
- PR/commit: pending; no PR or commit was created because this workstream has not requested
  git publication. The eventual PR must link this record and the journal entry.

## Verification evidence

- Pre-edit baseline: Infrastructure and Blazor Client Release builds passed with 0 warnings and 0 errors.
- Pre-edit baseline: focused `BrowserActionInteropTests` passed 6/6; focused `BrowserInteropSafetyTests` passed 5/5.
- Pre-edit baseline: dependency-license policy passed for 652 unique package/version pairs.
- RED commands and exact compiler failures: `.omo/evidence/20260825-phase20/20.3-qr-red.md` (SHA-256 `557642132c6923f127172f301af6b8ef3c79cb4abc89329447964cc51f3f67fc`).
- Focused payload codec command passed 10/10.
- Focused renderer command passed 3/3, including exact 41-module geometry, deterministic SVG, secret-free output, and production DI.
- Focused Blazor scanner interop and safety command passed 9/9 after the independent
  malformed/null-result invariant-breaker exposed and repaired fail-closed behavior.
- Focused renderer command passed 3/3 after binding the deterministic fixture to an exact
  SHA-256 geometry digest and a distinct second credential.
- Clean Architecture tests passed 15/15; naming-convention tests passed 11/11 with exact commands retained in the GREEN evidence.
- Targeted locked restore passed; dependency-license policy passed for 653 unique package/version pairs.
- Task-owned source scans found no URL, storage, DOM-write, network, console, telemetry, referrer, raw-eval, ZXing, QRCoder, or polyfill sink.
- `git diff --check` passed for all Task 20.3 implementation, tests, evidence, and project inputs.
- Fresh final Application, Infrastructure, and Blazor Client Release builds passed with 0 errors. Infrastructure reported 496 pre-existing analyzer warnings; the isolated Application and Blazor Client builds reported 0 warnings.
- Exact commands, counts, generated lock paths, and transient shared-tree/concurrent-output observations are retained in `.omo/evidence/20260825-phase20/20.3-qr-green.md`.
