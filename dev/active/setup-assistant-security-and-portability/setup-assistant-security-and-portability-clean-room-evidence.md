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
- **Research boundary:** Official framework, platform, standards, package
  registry, signature, vulnerability, and sanitized component/notices metadata
  only. No third-party source, snippet, AST, test, schema, prose, package
  content, license text, visual asset, or implementation structure was
  retained.
- **Dependency decision:**
  [setup-assistant-security-and-portability-dependency-evidence.md](setup-assistant-security-and-portability-dependency-evidence.md)

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

Accessed 2026-08-30 through 2026-08-31 using public official documentation,
registry, signature, vulnerability, or package metadata:

| Source | URL | Functional fact used |
|---|---|---|
| Avalonia WebAssembly deployment | https://docs.avaloniaui.net/docs/deployment/webassembly | Browser publish output is a static client-side WebAssembly site with no server-side application code. |
| Avalonia supported platforms | https://docs.avaloniaui.net/docs/supported-platforms | Windows, macOS, Linux, and WebAssembly support levels vary by OS/version; Arch and other unlisted distributions are Tier 3 rather than implied first-class support. |
| Avalonia Linux guide | https://docs.avaloniaui.net/docs/platform-specific-guides/linux | X11 is the stable default; native Wayland is an explicit experimental path; Linux accessibility uses AT-SPI2. |
| Avalonia accessibility | https://docs.avaloniaui.net/docs/app-development/accessibility | Desktop platforms expose native accessibility APIs; browser/WASM accessibility is partial and requires a separate evidence claim. |
| Avalonia licensing FAQ | https://docs.avaloniaui.net/tools/faq | The framework is MIT; professional tooling has separate terms and must not enter the FOSS product graph. |
| Avalonia package index | https://api.nuget.org/v3-flatcontainer/avalonia/index.json | `12.1.1` was the evaluated stable candidate; runtime targets are blocked and no package is selected for A. |
| Terminal.Gui documentation | https://tui-cs.github.io/Terminal.Gui/index.html | Candidate behavior informed requirements only; no implementation expression was retained. |
| Terminal.Gui package index | https://api.nuget.org/v3-flatcontainer/terminal.gui/index.json | `2.4.17` was evaluated and its complete 24-package graph is blocked. |
| Terminal.Gui registration | https://www.nuget.org/packages/Terminal.Gui/2.4.17 | Exact blocked direct package identity. |
| TextMateSharp.Grammars registration | https://www.nuget.org/packages/TextMateSharp.Grammars/2.0.4 | Exact mandatory transitive blocker identity. |
| NuGet service index | https://api.nuget.org/v3/index.json | Official registry, vulnerability, content-integrity, and signature service boundary. |
| .NET 10 Blazor CSP guidance | https://learn.microsoft.com/en-us/aspnet/core/blazor/security/content-security-policy?view=aspnetcore-10.0 | Client-side WebAssembly can use `connect-src 'none'`; CSP reduces risk but is not a complete security proof. |
| .NET cryptographic random bytes | https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator.getbytes?view=net-10.0 | `RandomNumberGenerator.GetBytes` supplies cryptographically strong random bytes; the static/BCL cryptographic RNG APIs are preferred over non-cryptographic randomness. |
| .NET strict UTF-8 decoder construction | https://learn.microsoft.com/en-us/dotnet/api/system.text.utf8encoding.-ctor?view=net-10.0 | `UTF8Encoding(false, true)` emits no BOM and throws on invalid UTF-8 decoding input. |
| .NET Console | https://learn.microsoft.com/en-us/dotnet/api/system.console?view=net-10.0 | Console exposes process composition facts; command behavior receives explicit I/O and terminal facts instead of reading it ambiently. |
| .NET redirected input fact | https://learn.microsoft.com/en-us/dotnet/api/system.console.isinputredirected?view=net-10.0 | The composition root can distinguish redirected standard input from an interactive terminal capability. |
| System.Text.Json source generation | https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation | Generated `JsonSerializerContext`/`JsonTypeInfo` metadata is the planned machine serializer seam without reflection serialization. |
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
- CLI machine mode is deterministic and non-secret. Terminal secret entry uses
  a bounded repository-native BCL wizard; it is human-only, interactive,
  masked, TTY-bound, and writes directly to a protected file. Product-owned
  adversarial tests cover redirection, echo restoration, signals, resize,
  scrollback, keyboard/non-color operation, and accessibility limitations.
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
| A foundation-offline | Reuse package-free wire contracts; build deterministic non-secret catalogue, dotenv, legal, machine CLI, and a repository-native BCL terminal wizard. Create package-free disabled presentation/Browser/Desktop contract shells only to close the tested project graph; they are non-shipped and non-functional. | Current plan-aligned I-VSD, revision-bound CTO/user approval, and exact dependency/AFC/SSO evidence. |
| B presentation-targets | Select a provenance-complete GUI graph and activate shared GUI, browser, and desktop targets over A contracts; preserve all browser secret and desktop fail-closed behavior. Avalonia may be reconsidered only with new authoritative component/build evidence. | Fresh target I-VSD/CTO/user and dependency/security/accessibility review; exact browser bundle/origin/request/storage evidence; filesystem/ACL/link/atomicity evidence. |
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
4. Successor A's terminal adapter is a bounded repository-native BCL wizard
   over `Event.Setup.Core`; it owns no validation, rendering, readiness, or
   secret classification. The deterministic machine CLI remains separate and
   non-secret.
5. Terminal.Gui `2.4.17` and its complete 24-package graph are blocked because
   mandatory TextMateSharp.Grammars `2.0.4` lacks complete component-level
   provenance/notices. No stable corrected package or supported no-grammar
   profile exists; no graph member is pinned/restored for A.
6. Avalonia `12.1.1` shared compile use is only conditionally acceptable for
   non-shipped scaffolding with `AVALONIA_TELEMETRY_OPTOUT=1` and publish
   exclusions, but every Desktop/Browser runtime target is blocked. A selects
   no Avalonia package. Its presentation target boundaries are package-free,
   disabled, non-shipped shells; B owns actual GUI framework selection.
7. Successor B retains all GUI/browser/desktop outcomes. It must select a
   provenance-complete graph or obtain new authoritative Avalonia component/
   build evidence, then receive fresh I-VSD, CTO, user, dependency, security,
   and accessibility approval. No package or target is inherited.
8. The public browser secret capability ships disabled until the exact static
   bundle, CSP, origin disclosure, storage/request behavior, and independent
   security review are approved. No wording may convert client-side execution
   into a claim that the hosting origin cannot obtain secrets.
9. Any selected GUI's initial Linux runtime uses an evidenced stable backend.
   Native Wayland, AppImage, and Flatpak remain gated additions after target-specific
   compatibility, portal, packaging, and license evidence.
10. No embedded AI, model SDK, prompt runtime, live secret retrieval, PWA,
   service worker, auto-update, plugin, or token persistence enters scope.
11. The agent skill is created only after the versioned CLI machine contract is
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

- **Canonical record:**
  [setup-assistant-security-and-portability-dependency-evidence.md](setup-assistant-security-and-portability-dependency-evidence.md).
- **Current decision:** planning evidence only; no dependency, project shell,
  lock, pin, restore, or generated ratchet is claimed.
- **Selected A graph:** BCL plus existing package-free
  `Event.Wire.Contracts`; repository-native deterministic machine CLI and
  bounded terminal wizard. Existing approved test infrastructure may be used
  only in the five matching test shells.
- **Blocked Terminal graph:** Terminal.Gui `2.4.17`, mandatory
  TextMateSharp.Grammars `2.0.4`, and the complete 24-package resolved graph.
  Missing grammar component provenance/notices has no stable corrected or
  supported no-grammar path.
- **Blocked Avalonia targets:** Desktop and Browser runtime graphs. Native
  SkiaSharp/HarfBuzz alternatives lack authoritative binary/component mapping,
  and exact publish absence of `Avalonia.Remote.Protocol` is unproved. ANGLE's
  BSD-3-Clause resolution, build-only BuildServices, telemetry opt-out, and
  signed integrity do not approve a runtime target or any A package pin.
- **Conditional fact, not selection:** shared compile scaffolding could be
  reconsidered only for non-shipped use with
  `AVALONIA_TELEMETRY_OPTOUT=1` and proved publish exclusions; A needs no such
  package and must not pin/restore it.
- **SSO decision:** `pass` for a repository-native linear BCL wizard derived
  from ISLAMU requirements and existing CLI conventions; no third-party names,
  hierarchy, UI composition, tests, or prose are reproduced.
- **Stop condition:** any blocked pin/restore/lock, replacement GUI/TUI package,
  package exception, activated shell, missing graph/provenance/notice/
  vulnerability/signature evidence, or stale approval stops implementation.
  There is no waiver path.

## Review And Evidence State

- Current I-VSD reviewed input revision:
  `sha256:d2bbba40455c013e20883ab6202f84411bb05f2c20f6060a9e73095f44a8e4b1`;
  status `current`, disposition `plan-aligned`, with all
  F001-F046/M001-M046 mappings preserved.
- Current CTO and user approval artifacts authorize only the BCL-only
  successor-A revision; no later successor inherits them.
- SA-110 Red and SA-120 Green are recorded. Ten package-free project
  boundaries/locks and two generator-owned disabled ratchets exist; no blocked
  package or presentation capability is approved.

## Evidence Limits

No project scaffold, package pin, lock, restore, target publish, runtime
request capture, filesystem test, assistive-technology audit, notarization,
SBOM, reproducibility check, or independent security/legal approval occurred in
this planning update. Sanitized package signature and point-in-time
vulnerability findings are recorded only in the dependency evidence. The
evidence supports a planning decision, not implementation or release claims.

## SA-220 Cumulative Cutover Evidence — 2026-08-31

This entry adds repository-source implementation evidence without replacing the
planning/dependency history above.

- Package-free owner: `src/Event.Wire.Contracts/Event.Wire.Contracts.csproj`
  contains zero `PackageReference` and zero `ProjectReference` entries. Its
  final public root is `ISLAMU.Wire.Contracts.ConfigurationPortability`.
- Required SA-210 selector passed 6/6:
  `dotnet run --project tests/Event.Wire.Contracts.UnitTests/Event.Wire.Contracts.UnitTests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupContractExtractionTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`.
- Focused behavior passed: Wire legal 4/4; Wire v1alpha2 closure 7/7;
  Application parser 4/4, serialization 3/3, validator 26/26; Domain legal
  aggregate 13/13; schema generation 9/9 and artifact routing 2/2; SA-110
  frozen-boundary architecture 10/10; API tenant-package authority 2/2.
- Canonical generator `--check` passed for both artifacts. Checked files had no
  git diff. SHA-256 identities are
  `25b6ea15b13367e714c97717a82f96c0a05df337d5540e3c3e69a56110984597`
  (manifest) and
  `2d7832a5b4d36e052168717d0c737ab7332e52c6ff353bd8e5212ec3c224d75a`
  (tenant package).
- Final sequential Release builds passed for Wire, Domain, Application,
  Infrastructure, API, schema generator, Wire tests, Domain tests, Application
  tests, Architecture tests, and API integration tests: 11 projects, zero
  errors. Incremental warning counts were 0/0/0/0/0/0/11/0/1468/316/0;
  warnings are the repository's pre-existing analyzer inventory, not suppressed.
- LSP error diagnostics were clean for Wire (12 files), Infrastructure
  ConfigurationManifest (5 files), Wire portability tests (6 files), and
  Application ConfigurationManifest tests (17 files). Larger Domain,
  Application, and API directory requests timed out; compile verification above
  is authoritative. The schema directory additionally reported the pre-existing
  unavailable Biome executable; no package was installed.
- Searches across `src`, `eng`, and `tests` returned zero old Application
  contract namespace, old serialization namespace/context, Domain legal owner,
  aliases, or type forwards. Wire dependency scans returned zero references.
- Diagnostic and exception vectors disclose only stable code/path metadata;
  supplied secret/PII sentinel values were absent. The SA-210 authority closure,
  smuggling, and value-leak assertions passed.
- `git diff --check` passed. Initial concurrent transitive builds encountered
  only shared output-file locks; the final sequential build set above passed.
- No external source, package, migration, generated API client, UI runtime, live
  endpoint, commit, compatibility shim, or Phase 2 Setup Core workflow was
  introduced by SA-220.
