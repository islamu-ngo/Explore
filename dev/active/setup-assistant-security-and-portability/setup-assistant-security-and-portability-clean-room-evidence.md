<!-- ABOUTME: Source-free research handoff for the Setup Assistant planning workstream. -->
<!-- ABOUTME: Records official facts, repository anchors, independent decisions, and provenance boundaries. -->

# Setup Assistant Security And Portability — Clean-Room Evidence

Last Updated: 2026-08-31 Europe/Brussels

## Identity

- **Workstream:** `setup-assistant-security-and-portability`
- **Intent:** Preserve the complete Setup Assistant umbrella while handing off
  seven separately reviewed successors: offline foundation, presentation
  targets, composition scale, live control-plane, application-data migration,
  sovereign payment migration, and release/agent contracts.
- **Research boundary:** Official framework, platform, standards, and package
  metadata only. No third-party source, snippet, AST, test, schema, prose,
  visual asset, or implementation structure was retained.

## Repository Evidence Packet

| Locator | Verified fact |
|---|---|
| `dev/active/configuration-manifest/configuration-manifest-context.md` | ConfigurationManifest is active; Setup consumes only its frozen v1alpha2/no-secret/legal Markdown extraction contract and does not inherit server implementation details. |
| `src/Explore.Application/Features/ConfigurationManifest/Contracts/ConfigurationManifestV1Alpha2.cs` | Current versioned manifest and tenant-package wire contracts are Application-owned and depend on Domain legal limits. |
| `src/Explore.Application/Features/ConfigurationManifest/Catalog/ConfigurationPortabilityRegistry.cs` | A closed 21-entry registry classifies portable and excluded sections, including explicit secret, PII, application-data, operational-state, provider-binding, and topology exclusions. |
| `src/Explore.Domain/LegalMarkdownContract.cs` | A deterministic, network-free constrained legal Markdown parser/renderer already rejects raw HTML, remote resources, unsafe links, malformed placeholders, and inaccessible heading order. |
| `src/Explore.Domain/Secrets/SecretDefinitionRegistry.cs` | Secret-binding semantics and canonical environment-variable names exist, but there is no complete non-secret deployment catalogue or declarative activation graph. |
| `src/Event.Wire.Contracts/Event.Wire.Contracts.csproj` | A package-free inner project already owns versioned codecs and values shared across server and isolated clients. |
| `eng/configuration-manifest-schema/src/ISLAMU.ConfigurationManifest.SchemaGenerator/ISLAMU.ConfigurationManifest.SchemaGenerator.csproj` | The current schema generator references the whole Application assembly, which is too broad for the Setup Assistant. |
| `.env.example` and `docker-compose.yml` | The current operator inputs are large hand-maintained documents (618 and 985 lines respectively), not one machine-generated catalogue. |
| `Directory.Build.props` and `Directory.Packages.props` | Central package management, committed lock files, deterministic builds, and FOSS/commercial dependency boundaries are repository-wide. |
| `docs/CI_CD_GOVERNANCE.md` | Product package graphs must pass locked restore, vulnerability audit, and repository-owned license policy; unknown or denied licenses fail closed. |
| `eng/release/policy/scope-registry.yaml` | No public `setup` release scope exists yet. |
| `Explore.slnx` | No Setup Assistant source or test project exists. |

## Official Source Register

Accessed 2026-08-30 through public official documentation or package metadata:

| Source | URL | Functional fact used |
|---|---|---|
| Avalonia WebAssembly deployment | https://docs.avaloniaui.net/docs/deployment/webassembly | Browser publish output is a static client-side WebAssembly site with no server-side application code. |
| Avalonia supported platforms | https://docs.avaloniaui.net/docs/supported-platforms | Windows, macOS, Linux, and WebAssembly support levels vary by OS/version; Arch and other unlisted distributions are Tier 3 rather than implied first-class support. |
| Avalonia Linux guide | https://docs.avaloniaui.net/docs/platform-specific-guides/linux | X11 is the stable default; native Wayland is an explicit experimental path; Linux accessibility uses AT-SPI2. |
| Avalonia accessibility | https://docs.avaloniaui.net/docs/app-development/accessibility | Desktop platforms expose native accessibility APIs; browser/WASM accessibility is partial and requires a separate evidence claim. |
| Avalonia licensing FAQ | https://docs.avaloniaui.net/tools/faq | The framework is MIT; professional tooling has separate terms and must not enter the FOSS product graph. |
| Avalonia package index | https://api.nuget.org/v3-flatcontainer/avalonia/index.json | `12.1.1` is the current stable candidate, subject to complete target-graph review. |
| Terminal.Gui documentation | https://tui-cs.github.io/Terminal.Gui/index.html | Terminal.Gui v2 supports Windows, macOS, Linux, keyboard/mouse, Unicode, editors, wizards, and inline/full-screen operation. |
| Terminal.Gui package index | https://api.nuget.org/v3-flatcontainer/terminal.gui/index.json | `2.4.17` is the current stable candidate; later `2.4.18` entries are prerelease builds. |
| .NET 10 Blazor CSP guidance | https://learn.microsoft.com/en-us/aspnet/core/blazor/security/content-security-policy?view=aspnetcore-10.0 | Client-side WebAssembly can use `connect-src 'none'`; CSP reduces risk but is not a complete security proof. |
| .NET Unix file-mode API | https://learn.microsoft.com/en-us/dotnet/api/system.io.file.setunixfilemode?view=net-10.0 | .NET 10 can apply Unix modes through a file handle or path; handle-first use supports safer creation. |
| Windows SignTool | https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool | Windows SDK tooling signs, timestamps, and verifies artifacts, with SHA-256 digest selection. |
| Apple notarization | https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution | Developer ID distribution requires signing/notarization evidence and supports ticket revocation/audit. |
| SLSA provenance | https://slsa.dev/spec/v1.2/provenance | Provenance binds an artifact to where, when, and how it was produced. |
| Flatpak sandbox permissions | https://docs.flatpak.org/en/latest/sandbox-permissions.html | Sandboxed applications default to no network and limited filesystem access; portals should replace blanket host access. |

## Source-Free Functional Specification

- The product creates and validates non-secret manifest/package artifacts and a
  separate relevant-only dotenv artifact.
- Browser sessions start in no-secret mode. Optional secret entry is a
  per-session trust decision and is release-disabled until independent security
  evidence approves the exact build and origin.
- No secret value enters portable configuration, command arguments, process
  environment, captured stdin, machine JSON, logs, telemetry, support bundles,
  browser storage, route state, or remote requests.
- Desktop secret output uses user-selected paths, link/special-file refusal,
  restrictive access, same-directory temporary creation, atomic replacement,
  explicit overwrite, permission verification, and partial-file cleanup.
- CLI machine mode is deterministic and non-secret. Terminal secret entry is
  human-only, interactive, masked, TTY-bound, and writes directly to a
  protected file.
- Legal source uses the existing constrained Markdown behavior and remains
  draft/review content; the assistant never publishes, fabricates acceptance,
  or claims legal approval.
- Every shipped target has an exact lock graph, SBOM, notices, checksum,
  signature/provenance status, support tier, and truthful release identity.
- Offline foundation is the first independently shippable delivery. Setup Core
  remains pure and offline: it has no network, database, provider, server,
  secret-readback, persistence, or local-authority behavior.
- Live control-plane work uses generated server HTTP/HAL contracts. Target,
  tenant, authorization, provider, import, transfer, and secret-binding
  authority remains in repository-native server layers; Setup receives only
  scoped capability and value-free state and never reads raw secrets or
  provider coordinates.
- Application-data migration is a separate server-owned custody program using
  repository-native privacy/tenant authority, durable mappings/checkpoints,
  protected staging, generated provider migrations, transactional outbox, and
  source retention. Portable configuration carries no application data or PII.
- Sovereign payment migration is a separate optional Tier 0 program using the
  repository's `OrganizerDirect`, immutable recipient/currency facts,
  deterministic refund allocation, checked ledger, provider reconciliation,
  and unknown-outcome authority. Configuration and Setup Core derive no money
  truth.
- Release and agent contracts describe only independently implemented and
  evidenced subsets. Missing evidence disables the target/capability and never
  enables a fallback or compatibility shim.

## Successor Functional Handoffs

| Boundary | Source-free implementation handoff | Required fresh evidence before implementation |
|---|---|---|
| A foundation-offline | Reuse package-free wire contracts; build deterministic non-secret catalogue, dotenv, legal, CLI/TUI workflows in pure Setup Core and outer local adapters. | Current I-VSD, corrected CTO/user approval, exact dependency/AFC/SSO evidence. |
| B presentation-targets | Adapt A contracts to shared Avalonia, browser, and desktop; browser secret capability remains release-disabled and desktop writes fail closed. | Target dependency/security/accessibility review; exact browser bundle/origin/request/storage evidence; filesystem/ACL/link/atomicity evidence. |
| C composition-scale | Treat YAML/directory as bounded inputs that converge on unchanged canonical v1alpha2 JSON; profiles exist only from measured evidence. | Stable A2/A3 contracts and named cardinality/resource evidence. |
| D live-control-plane | Consume generated server APIs/HAL; server reauthorizes target, tenant, replay, provider and transfer state; no local authority or secret readback. | Fresh Tier 1/I-VSD/CTO/user approval and green ConfigurationManifest Tier 1/tenant/replay/atomicity evidence; the gate cannot be bypassed. |
| E application-data-migration | Use server Domain/Application/Persistence/API authority for category custody, tenant-qualified mappings, checkpoints, idempotency, privacy/erasure, outbox, receipts, and source retention; Setup is an adapter. | Fresh Tier 2 custody/erasure and Tier 1 tenant review, I-VSD/CTO/user approval, generated provider/database evidence. |
| F sovereign-payment-migration | Use server-owned payment/refund state and real provider/ledger contracts. Before SA-1140, deterministically race stale/replayed capability plus tenant mismatch at the public seam and prove zero cross-tenant rows, zero provider/outbox money intent, unchanged checked balances, one durable value-free conflict receipt, and zero PII/secret logs. | Fresh Tier 0 Grill-Me/I-VSD/CTO/user approval, real database/provider contract, provider/legal/operator decision evidence; no sleeps or internal mocks. |
| G release-and-agent-contract | Package and document only selected green subsets; teach only implemented versioned no-secret CLI behavior and require human approval for mutation. | Each owning successor green plus exact locks/SBOM/provenance/signing/support and applicable legal/security/privacy/payment evidence. |

Dependencies are one-way A -> B/C -> D -> E; F depends on D/E contracts and is
independently optional; G runs per shipped subset and at final reconciliation.
No handoff inherits umbrella or predecessor approval.

## Resolved Architecture Decisions

1. `Event.Wire.Contracts` becomes the package-free source of truth for the
   versioned manifest/package contract and constrained legal Markdown codec.
   This reuses an existing inner shared-project pattern instead of making the
   offline product reference `Explore.Application` or duplicating contracts.
2. New `Event.Setup.Core` owns the environment catalogue, dotenv
   parse/render/readiness logic, offline validation, diffs, workflow states,
   secret generation policies, and value-safe diagnostics. It references only
   `Event.Wire.Contracts` and BCL APIs.
3. Existing repository-native server layers retain live target/tenant,
   authorization, import, transfer, provider, persistence, privacy, and payment
   authority. Offline successor A does not call live APIs. Successors D/E/F
   consume generated server APIs and HAL only after independent approval; Setup
   Core remains offline/pure with no direct database/provider access or secret
   readback and never claims local atomic-apply, ownership, or money authority.
4. Avalonia shared UI and Terminal.Gui TUI are adapters over `Event.Setup.Core`;
   neither owns validation, rendering, readiness, or secret classification.
5. Avalonia `12.1.1` and Terminal.Gui `2.4.17` are candidates, not approvals.
   Implementation first resolves complete direct/transitive/native/tooling
   graphs and blocks on any incompatible or unknown obligation.
6. The public browser secret capability ships disabled until the exact static
   bundle, CSP, origin disclosure, storage/request behavior, and independent
   security review are approved. No wording may convert client-side execution
   into a claim that the hosting origin cannot obtain secrets.
7. The initial Linux runtime uses stable X11/XWayland. Native Wayland,
   AppImage, and Flatpak remain gated additions after target-specific
   compatibility, portal, packaging, and license evidence.
8. No embedded AI, model SDK, prompt runtime, live secret retrieval, PWA,
   service worker, auto-update, plugin, or token persistence enters scope.
9. The agent skill is created only after the versioned CLI machine contract is
   implemented and verified; agents never receive or operate on secret values.

## Clean-Room Attestation

- No third-party implementation source, snippets, ASTs, decompiled artifacts,
  SQL, migrations, tests, comments, prose, images, fonts, or assets are
  included in this handoff.
- Official facts above constrain behavior only. Naming, decomposition, state
  machines, diagnostics, test arrangement, UI composition, and release
  integration are independently derived from ISLAMU repository conventions
  and the I-VSD report.
- Implementation must begin from this handoff and repository-native sources,
  not from the external research context.

## Dependency And SSO Decision

- **Current decision:** planning evidence only; no dependency was added.
- **Candidate dependencies:** Avalonia `12.1.1` and Terminal.Gui `2.4.17`.
- **Required implementation evidence:** exact role, lock graph, license
  expression, native/tooling/assets, vulnerabilities, outbound-path impact,
  notices/source obligations, repository scanner result, and independent
  AFC/SSO review.
- **Stop condition:** unknown, commercial, proprietary, source-available,
  field-of-use, or outbound-incompatible material blocks the target until a
  compatible replacement or documented steward/legal approval exists.

## Review And Evidence State

- Current I-VSD reviewed input revision:
  `sha256:055fb1dd8c0dfcdbd809bbfb89cbd2660904469fd3d866d6d6349af091793d4f`;
  status `current`, disposition `plan-aligned`, mappings F001-F046/M001-M046.
- The first CTO review decision is `Split before approval`; it grants no
  approval to this corrected handoff.
- The user approved the complete objective, while corrected exact-revision
  approval awaits final hashes and fresh CTO review.
- Foundation A is the sole active successor and remains blocked before SA-110.

## Evidence Limits

No package restore, target publish, runtime request capture, filesystem test,
assistive-technology audit, signing, notarization, SBOM, reproducibility check,
or independent security/legal review occurred during planning. The evidence
supports the plan, not implementation or release claims.
