<!-- ABOUTME: Canonical architecture plan for the cross-platform ISLAMU Event Setup Assistant. -->
<!-- ABOUTME: Defines behavior, security boundaries, phased delivery, verification, and I-VSD traceability. -->

# Setup Assistant Security And Portability — Implementation Plan

Last Updated: 2026-08-30 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Write an implementation plan for
  `islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md`
  as the next workstream after ConfigurationManifest.
- **Task directory:**
  `dev/active/setup-assistant-security-and-portability/`
- **Planning status:** Draft; awaiting user review.
- **Change classification:** Behavioral Delta. This work adds a shipped
  cross-platform product, command contract, secret-handling workflows,
  generated deployment artifacts, legal-content authoring, packaging, and
  release behavior.
- **Complexity:** XL. The work crosses a package-free shared contract, headless
  core, Avalonia desktop/browser adapters, Terminal.Gui, deterministic CLI,
  security boundaries, accessibility/localization, six release RIDs, Linux
  packaging, signing/provenance, dependency governance, and agent context.
- **Primary intent:** `external-infrastructure-bootstrap` — Tier 1 Security,
  exhaustive threat-boundary exploration, Invariant-Breakers, fail-closed
  secret handling, and security/operations review.
- **Supporting intents:** `ci-cd-change`,
  `ip-clean-room-governance`, and `create-agent-context-skill`.
- **Inherited contracts:** `legal-identity-authority-change` and the
  user-closed ConfigurationManifest workstream govern legal role authority, immutable
  publication/acceptance evidence, tenant isolation, server-side apply, and
  migration behavior already present in the authoritative worktree. This plan
  consumes the frozen v1alpha2 wire/schema/registry/import-preview baseline
  rather than redefines those boundaries. Retired ConfigurationManifest
  Phases 19–23 are not implementation evidence and are not silently inherited.
- **Fallback scope contract:** No current intent names a new Avalonia shipped
  product. The inferred source scope is the new `src/Event.Setup*` projects,
  corresponding `tests/Event.Setup*` projects, package/solution/CI/release
  integration, generated environment assets, setup documentation, and the
  post-CLI agent skill. Server changes are limited to extracting pure contracts
  and registering generated catalogue checks; live API behavior remains out of
  scope.
- **Relevant skills:** implementation-plan, i-vsd, grill-me,
  criticality-guardrail, clean-architecture-rules, ip-clean-room,
  agentic-research, accessibility, skill-authoring, auth-patterns,
  blazor-bff-patterns, and dotnet-efcore-guidelines.
- **Relevant rules:** `.agents/rules/application-layer.md`,
  `.agents/rules/domain.md`, `.agents/rules/tests.md`, and
  `.agents/rules/ip-clean-room.md`. Existing path rules do not cover Avalonia;
  Phase 1 adds executable Setup Assistant boundaries rather than informally
  borrowing Blazor-specific rules.
- **Primary layers:** `Event.Wire.Contracts`, new `Event.Setup.Core`, shared
  Avalonia presentation, Browser/Desktop composition roots, CLI/TUI adapter,
  release engineering, and agent context.
- **I-VSD document:**
  [i-vsd-setup-assistant-security-and-portability.md](../../../islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md)
- **I-VSD reviewed input revision:**
  `sha256:8c86d6be6f612861bba9c4ea641a451722fe0d6b5feccad09ac310b5cdce1637`
- **I-VSD status / disposition:** `current` / `plan-aligned`.
- **Clean-room evidence:**
  [setup-assistant-security-and-portability-clean-room-evidence.md](setup-assistant-security-and-portability-clean-room-evidence.md)
- **CTO review:** Not reviewed.
- **User approval:** Awaiting approval for this exact workstream revision.
- **Grill-Me intake:** The existing I-VSD consultation records the user’s
  material choices: web plus desktop, protective no-secret browser default,
  optional trust-based web secret mode, open/auditable browser source,
  relevant-only dotenv, instance and tenant portability, constrained legal
  Markdown, Terminal.Gui plus machine CLI, compatible FOSS only, no embedded
  AI, and a post-CLI agent skill. Repository evidence resolves the remaining
  architecture: reuse package-free `Event.Wire.Contracts`, add a pure
  `Event.Setup.Core`, keep all live authority server-side, and ship hosted
  secret mode disabled until independent evidence approves the exact release.
  No material user decision remains open.

## 1. Executive Summary

ISLAMU Event will gain an offline-first **Setup Assistant** that makes
self-hosting and configuration portability usable without weakening the
separation between shareable non-secret configuration and deployment-local
secrets.

The product will provide:

- deterministic authoring, validation, formatting, diffing, coverage, and
  export for `ConfigurationManifest` and `TenantConfigurationPackage`;
- a canonical environment catalogue and relevant-only dotenv renderer;
- a safe no-secret browser workflow and a separately gated secret workflow;
- native desktop file protection on Windows, Linux, and macOS;
- a versioned machine CLI plus a human Terminal.Gui TUI;
- shared Avalonia views for browser and desktop;
- constrained legal-document editing over the existing legal Markdown
  contract;
- deterministic release artifacts, SBOMs, checksums, signatures/provenance,
  and truthful support claims; and
- a schema-compliant agent skill created only after the CLI contract is real.

The architectural center is not Avalonia. It is a package-free, deterministic
contract and workflow core. UI, terminal, filesystem, and browser concerns are
adapters around that core. Server runtime authority for authentication,
tenancy, policy ceilings, import sessions, transactional apply, legal
publication, and acceptance remains in ISLAMU Event.

### Explicit non-goals

- No live instance import/export API client, OAuth/device flow, token storage,
  Infisical read/write, provider connectivity test, or live secret retrieval.
- No combined manifest-plus-dotenv artifact.
- No embedded AI, model SDK, prompt runtime, inference, or agent loop.
- No PWA/service worker, auto-update, downloaded plugin/template pack, mobile
  target, or direct instance-to-instance transfer.
- No legal advice, auto-publication, fabricated acceptance, or migration of
  historical acceptance evidence.
- No backward-compatibility layer for Application-owned v1alpha2 contract
  locations. Callers move to the shared contract in one breaking change.

## 2. Source-Grounded Current State Report

### 2.0 Pre-Flight Structural Context

The code-review graph MCP is not registered in this harness. The following
slice is the bounded manual structural trace from owning projects, references,
contracts, and tests:

```yaml
Target: Setup Assistant offline portability and dotenv generation
Callers (Upstream):
  - new Event.SetupAssistant Avalonia workflows
  - new Event.SetupAssistant.Cli commands and Terminal.Gui TUI
  - existing ISLAMU.ConfigurationManifest.SchemaGenerator
Callees (Downstream):
  - Event.Wire.Contracts versioned codecs
  - new Event.Setup.Core catalogues, validators, renderers, and workflow states
  - desktop/browser platform adapters
Impacted Flows:
  - ConfigurationManifest and TenantConfigurationPackage authoring
  - relevant-only dotenv generation
  - legal source authoring and portability
  - browser and terminal secret completion
  - multi-platform release and agent automation
Criticality: Tier 1 Security
Test Coverage:
  - existing Event.Wire.Contracts.UnitTests
  - existing Event.Application.UnitTests ConfigurationManifest suites
  - existing Event.Architecture.Tests ConfigurationManifest suites
  - new Event.Setup.* focused test projects
```

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| No Setup Assistant project exists | `Explore.slnx`; project inventory under `src/` and `tests/` | High | New projects are explicitly marked new in Section 6. |
| ConfigurationManifest is closed for archival by explicit user decision | `configuration-manifest-context.md`; closure disposition dated 2026-08-30 | High | Setup consumes the frozen current v1alpha2/schema/registry/import-preview baseline; retired phases are not represented as implemented. |
| v1alpha2 wire contracts are Application-owned | `src/Explore.Application/Features/ConfigurationManifest/Contracts/ConfigurationManifestV1Alpha2.cs` | High | This currently forces the schema tool to reference all Application. |
| A closed portability registry exists | `ConfigurationPortabilityRegistry.cs` | High | It has 21 entries and explicit excluded authority classes. |
| Import preview and HTTP foundations exist | `src/Explore.Application/Features/ConfigurationManifest/Importing/**`; instance/tenant import controllers and generated contracts | High | Strict parser, target-bound session, coverage/diff composer, and upload/preview/refresh/cancel transport exist; atomic apply remains server-side and outside this offline Setup workstream. |
| Constrained legal Markdown exists | `src/Explore.Domain/LegalMarkdownContract.cs` | High | Pure parser/renderer; rejects HTML, resources, unsafe links, and malformed structure. |
| Secret binding has a registry | `src/Explore.Domain/Secrets/SecretDefinitionRegistry.cs` | High | It is not a complete deployment-variable catalogue. |
| Environment inputs are large and manual | `.env.example` (618 lines), `docker-compose.yml` (985 lines) | High | No machine-generated relevance/default/activation model exists. |
| A package-free shared contract project exists | `src/Event.Wire.Contracts/Event.Wire.Contracts.csproj` | High | Application already references it; it is the repository-native extraction seam. |
| Deterministic dependency policy exists | `Directory.Build.props`, `Directory.Packages.props`, 49 lock files | High | Candidate Setup packages are not yet pinned or approved. |
| Release scope lacks Setup Assistant | `eng/release/policy/scope-registry.yaml` | High | Final release integration must add public `setup`. |
| Avalonia browser output is static client-side WASM | Official Avalonia WebAssembly deployment documentation | High | Static output does not eliminate origin trust. |
| Avalonia browser accessibility is partial | Official Avalonia accessibility documentation | High | Browser accessibility claims must be narrower than desktop claims. |
| CSP can deny script connections but is not proof of safety | Microsoft .NET 10 CSP guidance | High | Hosted secret mode remains independently gated. |

### 2.2 Existing Implementation

#### Shared contracts and Domain

- `Event.Wire.Contracts` is package-free and already used for versioned
  cross-runtime codecs.
- `Explore.Domain` owns legal document aggregate behavior, legal content
  limits, and the constrained Markdown parser.
- `SecretDefinitionRegistry` owns secret-binding semantics and canonical
  environment names, but it mixes secret-binding concerns with prose
  descriptions and does not express non-secret variables, defaults,
  capability predicates, topology, generation policy, or documentation
  anchors.

#### Application and schema generation

- v1alpha2 manifest/package DTOs, import modes, portability registry,
  serialization, validation, and preview currently live under
  `Explore.Application`.
- The schema generator references the whole Application assembly.
- Application correctly retains live target authority and transactional
  orchestration; those parts must not move into the offline core.

#### Presentation and deployment

- Existing Blazor is isolated behind generated API contracts and is not a
  reusable offline UI.
- `.env.example`, Compose, configuration documentation, and self-hosting
  documentation are maintained independently.
- No Avalonia, Terminal.Gui, desktop packaging, browser static release,
  Setup CLI, or Setup agent skill exists.

### 2.3 Existing Tests And Verification Coverage

- `Event.Wire.Contracts.UnitTests` protects the existing admission codec.
- Application and Architecture ConfigurationManifest tests protect strict
  v1alpha2 shape, schemas, deterministic generation, registry coverage, and
  record semantics.
- Domain tests protect legal Markdown and legal lifecycle behavior.
- Persistence and API tests protect server import/session/runtime behavior.
- Existing tests do not protect a headless setup core, environment activation,
  dotenv dialect, secret state lifecycle, browser network/storage boundaries,
  desktop safe writes, CLI machine contracts, TUI leakage, Avalonia
  accessibility, or Setup release artifacts.

### 2.4 Existing Documentation And Contracts

- Canonical: `docs/CONFIGURATION.md`, `docs/SECRETS.md`,
  `docs/SELF_HOSTING.md`, `docs/SECURITY-MODEL.md`,
  `docs/ACCESSIBILITY.md`, `docs/LOCALIZATION.md`,
  `docs/CI_CD_GOVERNANCE.md`, `docs/RELEASE_CHECKLIST.md`, and
  `docs/legal/IP_GOVERNANCE.md`.
- Generated: `schemas/configuration-manifest-v1alpha2.schema.json` and
  `schemas/tenant-configuration-package-v1alpha2.schema.json`.
- Release: central package pins, package lock files, release scope registry,
  append-only change fragments, dependency license/vulnerability scanners,
  checksums, SBOM/provenance, and GitHub workflow governance.
- I-VSD: the linked report owns provider-responsibility findings
  `IVSD-F001` through `IVSD-F036`.

### 2.5 Current Pain Points / Improvement Areas

1. A small offline client cannot safely reference all of Application.
2. Versioned portability contracts and legal Markdown are in different
   assemblies despite needing byte-identical desktop/browser/CLI/server
   behavior.
3. Environment relevance and defaults are implicit across prose, Compose, host
   configuration, and secret registry.
4. ConfigurationManifest is closed with a frozen current baseline, but its
   retired atomic-apply, live migration UI, managed-ownership, and transfer
   phases are not implementation evidence. Setup extraction must preserve the
   current bytes and must not imply those retired capabilities exist.
5. Browser-local execution can be overclaimed as origin-independent security.
6. Desktop/browser/terminal secret surfaces have distinct leakage paths and
   cannot share one generic “save file” adapter.
7. The repository has no Setup-specific dependency, project-reference,
   telemetry, AI, or release architecture ratchets.
8. Browser accessibility support is weaker than desktop support; parity cannot
   be claimed from shared XAML alone.

### 2.6 Unknowns After Investigation

These details are genuinely deferrable because they do not change the phase
structure or trust-boundary architecture:

| Unknown | Search/evidence | Resolution owner |
|---|---|---|
| Exact approved Avalonia package graph | Official metadata identifies `12.1.1` as current stable candidate; no restore/license scan performed | SA-120 either approves the exact graph or stops Phase 1 |
| Exact approved Terminal.Gui package graph | Official metadata identifies `2.4.17` as current stable candidate | SA-120 |
| Final OS/version support floor | Official Avalonia tiers are known; release-runner and packaging evidence is absent | SA-810 publishes only evidenced combinations |
| Which legal templates receive counsel approval | No approved template pack exists | SA-530 may ship blank/project-authored approved templates only |
| Which locally generated secret classes are provider-valid | Secret definitions exist; provider acceptance evidence is incomplete | SA-330 admits only independently documented generators |
| Whether AppImage or Flatpak clears target review | Official sandbox behavior is known; tool/license/release graph is not | SA-810 keeps each format disabled until approved |

Hosted web secret-mode enablement is not an open question: the capability is
implemented behind a release gate and remains disabled until the named
independent evidence exists.

## 3. Proposed Future State: Behavioral Contract & Scenarios

### Requirement 3.1: One Headless Source Of Truth

The system **SHALL** produce identical portable artifacts, diagnostics,
coverage, and readiness classifications from desktop, browser, CLI, TUI, schema
generation, and server static validation for the same inputs.

#### Scenario 3.1A: Cross-adapter parity

- **GIVEN** the same valid setup profile and source artifact
- **WHEN** desktop, browser, CLI, and TUI request formatting or validation
- **THEN** they produce byte-identical artifacts, the same stable diagnostic
  codes, and the same digest without network or persistence access.

#### Scenario 3.1B: Stale server contract

- **GIVEN** the active ConfigurationManifest contract or registry differs from
  the shared offline contract
- **WHEN** build-time convergence checks run
- **THEN** the build fails before a Setup release can be produced.

### Requirement 3.2: Strict Non-Secret Portability

The system **MUST** keep `ConfigurationManifest` and
`TenantConfigurationPackage` non-secret, scope-correct, bounded, deterministic,
and separate from deployment environment output.

#### Scenario 3.2A: Valid offline authoring

- **GIVEN** an operator selects permitted instance or tenant sections
- **WHEN** the operator creates, imports, edits, diffs, validates, or exports
  an artifact
- **THEN** only registered portable sections appear and the result carries the
  canonical schema identity, coverage, digest, and source-independent stable
  identities.

#### Scenario 3.2B: Secret or authority smuggling

- **GIVEN** an artifact contains a secret, PII, provider binding, topology,
  operational state, source database ID as authority, or wrong-scope legal text
- **WHEN** it is parsed or exported
- **THEN** processing fails closed with bounded codes and emits no sensitive
  value or partial artifact.

### Requirement 3.3: Relevant-Only Dotenv

The system **SHALL** render only variables relevant to selected topology and
capabilities, omit unchanged canonical defaults, and classify incomplete
requirements truthfully.

#### Scenario 3.3A: No-secret generation

- **GIVEN** a selected deployment profile with required secret variables
- **WHEN** the operator generates in no-secret mode
- **THEN** relevant required secret keys render with empty values, irrelevant
  keys are absent, defaulted values are omitted, and readiness states
  `Incomplete` with key names but no fabricated values.

#### Scenario 3.3B: Dotenv injection edge cases

- **GIVEN** values containing whitespace, quotes, `#`, backslashes, dollar
  signs, Unicode, line breaks, or command-like text
- **WHEN** the explicit supported dotenv dialect parses or renders them
- **THEN** output round-trips deterministically or fails closed without
  executing, interpolating, or silently changing the value.

### Requirement 3.4: Minimum Secret Exposure

The system **MUST NOT** persist, transmit, log, trace, announce, diagnose,
restore, or include a secret in any artifact other than the explicitly
requested protected dotenv output.

#### Scenario 3.4A: Local secret completion

- **GIVEN** a human explicitly enters or locally generates approved secret
  values
- **WHEN** output succeeds, is cancelled, expires, navigation changes, or the
  workflow faults
- **THEN** application state and rendered values are cleared as far as the
  platform permits, no support data contains values, and the UI states that
  deterministic memory erasure cannot be promised.

#### Scenario 3.4B: Exception and support capture

- **GIVEN** a secret-bearing operation throws
- **WHEN** diagnostics or an optional support report is generated
- **THEN** only release identity, platform, selected capability keys, and
  closed error codes appear; values, usernames in paths, raw exceptions,
  environment content, and clipboard data do not.

### Requirement 3.5: Protective Browser Default And Origin Truth

Every browser session **SHALL** start in no-secret mode. Secret entry **MUST**
require a fresh explicit trust decision bound to the displayed official origin
and release identity.

#### Scenario 3.5A: Safe default

- **GIVEN** a new, refreshed, restored, deep-linked, or back-forward browser
  session
- **WHEN** the Setup Assistant opens
- **THEN** no-secret mode is active and no remembered setting, URL state,
  cookie, or browser store activates secret entry.

#### Scenario 3.5B: Unapproved hosted release

- **GIVEN** the exact browser bundle lacks approved security/origin evidence
- **WHEN** the release capability manifest is generated
- **THEN** hosted secret entry is absent or disabled and no copy claims the
  origin cannot obtain secrets.

### Requirement 3.6: Browser Network And Storage Denial

After secret mode begins, the browser build **MUST** issue no request, allow no
form/navigation/resource channel, and write no secret-bearing browser state.

#### Scenario 3.6A: Enter secret mode after preload

- **GIVEN** every required code, font, icon, template, localization, and help
  resource is loaded locally
- **WHEN** the operator enters secret mode
- **THEN** network capability transitions to denied, external navigation is
  unavailable until secrets are cleared, and generation remains local.

#### Scenario 3.6B: Hidden channel attempt

- **GIVEN** code attempts fetch, XHR, WebSocket, EventSource, beacon, form,
  remote image/font/media, worker, frame, CSP report, or external navigation
- **WHEN** secret mode is active
- **THEN** the reviewed policy and application boundary deny the channel,
  record no value, and fail closed. Public enablement still requires
  independent evidence against the exact bundle.

### Requirement 3.7: Desktop Protected Writes

Desktop secret output **MUST** use target-specific protected creation and
atomic replacement, and **MUST** refuse unsafe targets by default.

#### Scenario 3.7A: Protected new file

- **GIVEN** a user-selected regular target on a filesystem that supports the
  required protection
- **WHEN** a secret-bearing dotenv is written
- **THEN** the temporary file is created in the destination directory with
  owner-only access, flushed, atomically installed, rechecked, and leaves no
  plaintext backup.

#### Scenario 3.7B: Link/race/permission failure

- **GIVEN** a symlink, reparse point, directory, special file, changed target,
  unsupported permission model, or overwrite race
- **WHEN** the write is attempted
- **THEN** the operation refuses or rolls back, removes incomplete bytes, and
  returns a closed value-free error. An advanced override is explicit and is
  never labelled safe.

### Requirement 3.8: Legal Content Is Draft Authority, Not Evidence

The assistant **SHALL** author constrained, portable legal source and readiness
metadata, but **MUST NOT** publish, fabricate historical versions, create
acceptance, or claim legal approval.

#### Scenario 3.8A: Role-correct legal draft

- **GIVEN** an instance or tenant authority, kind, audience, locale, typed
  placeholders, and approved template or blank source
- **WHEN** the operator validates and exports the draft
- **THEN** the correct scope receives bounded source/provenance/readiness
  metadata and the target is instructed to review it as a new draft/version.

#### Scenario 3.8B: Unsafe or misleading content

- **GIVEN** raw HTML, remote resources, executable content, unsafe links,
  unresolved placeholders, inaccessible headings, unapproved template
  provenance, or wrong-role claims
- **WHEN** preview or export is requested
- **THEN** the operation fails closed or remains explicitly not ready without
  altering published or acceptance evidence.

### Requirement 3.9: Stable CLI And Human TUI

The CLI **SHALL** expose deterministic, versioned, noninteractive commands and
one JSON object for machine mode. Secret completion **MUST** remain human-only
in an interactive TTY workflow.

#### Scenario 3.9A: Machine automation

- **GIVEN** a supported command and valid non-secret input
- **WHEN** an agent or script requests machine output and dry-run
- **THEN** one versioned JSON object contains stable status, diagnostics,
  artifact digests, sensitivity, coverage, and readiness with no terminal
  escapes or localized authority.

#### Scenario 3.9B: Secret supplied through process surfaces

- **GIVEN** a caller tries to pass a secret through arguments, options,
  environment, captured stdin, stdout, or non-TTY execution
- **WHEN** the command runs
- **THEN** it rejects the operation with a stable exit category and emits no
  supplied value.

### Requirement 3.10: Accessible And Localized Parity

The product **SHALL** provide keyboard-complete, non-color-only, scalable,
localized, and RTL-safe workflows, while publishing target-specific
accessibility limitations truthfully.

#### Scenario 3.10A: Shared accessible workflow

- **GIVEN** a keyboard-only, screen-reader, high-contrast, reduced-motion, 200%
  scale, or RTL user
- **WHEN** the user completes selection, validation, review, and save/download
- **THEN** controls expose stable roles/names/states, focus remains visible and
  logical, errors are associated and summarized once, and secret values never
  enter accessible names or announcements.

#### Scenario 3.10B: Unsupported parity claim

- **GIVEN** a target lacks equivalent browser or terminal assistive-technology
  evidence
- **WHEN** support metadata is rendered
- **THEN** the product labels the limitation and preserves an evidenced
  alternative rather than claiming universal parity.

### Requirement 3.11: FOSS And Verifiable Releases

Every shipped target **MUST** have an approved FOSS dependency graph, exact
identity, lock digest, SBOM, notices, checksums, provenance, signing status, and
truthful support scope.

#### Scenario 3.11A: Approved target release

- **GIVEN** a specific RID/package/browser bundle
- **WHEN** release evidence is assembled
- **THEN** artifact, source commit, locks, SBOM, build manifest, signatures,
  checksums, support tier, and reproducibility status converge on the same
  immutable release.

#### Scenario 3.11B: Incompatible dependency or packaging tool

- **GIVEN** an unknown, commercial, proprietary, source-available,
  field-of-use, reciprocal-obligation-incompatible, or unscanned component
- **WHEN** dependency or release validation runs
- **THEN** that target is blocked until replaced or separately approved; a
  scanner pass alone never grants legal compatibility.

### Requirement 3.12: Agent Safety And Human Approval

The external skill **MUST** use only the implemented versioned CLI, default to
no-secret dry-run operation, and require explicit human approval before writes,
legal publication handoff, live apply, or authority broadening.

#### Scenario 3.12A: Safe agent draft

- **GIVEN** a compatible installed CLI and non-secret source artifact
- **WHEN** an agent validates, explains, diffs, or drafts configuration
- **THEN** it uses machine JSON, never reads a secret-bearing file, presents
  bounded diagnostics, and waits for approval before the final non-secret
  write.

#### Scenario 3.12B: Skill/CLI drift or secret request

- **GIVEN** an incompatible CLI version, missing command/schema, secret-bearing
  input, or request to drive the TUI
- **WHEN** the skill routes the operation
- **THEN** it stops safely, directs secret completion to the local human UI,
  and never invents commands or handles values.

## 4. Non-Negotiable Constraints

1. Every source file starts with two `ABOUTME:` lines.
2. No backward-compatibility alias, duplicate DTO, deprecated route, old
   namespace adapter, dual read, or dual serializer survives the contract move.
3. `Event.Wire.Contracts` and `Event.Setup.Core` remain package-minimal,
   deterministic, trim/AOT-compatible, network-free, persistence-free, and
   provider-free.
4. The Setup projects never reference Application, Domain, Persistence,
   Infrastructure, API, Blazor, MediatR, EF Core, secret providers, or provider
   SDKs.
5. Server runtime validation and authorization remain authoritative for target
   state, tenant identity, policy ceilings, reference mappings, locks,
   transactional apply, legal publication, and acceptance.
6. Manifests/packages contain no secret, PII, provider credential, operational
   state, deployment topology, or secret reference.
7. `.env` and portable configuration remain separate artifacts with different
   sensitivity labels and no combined archive by default.
8. Secret values originate only from explicit local human entry or approved
   local cryptographic generation. The assistant never reads Infisical or live
   instances.
9. Secret workflows have no analytics, telemetry, remote logs, crash upload,
   CSP report, update call, source map value, or developer-tools package in
   production.
10. Browser secret mode is no-secret by default, per-session opt-in, preload
    first, no network/storage afterward, and release-disabled until independent
    approval.
11. Desktop writes are link-safe, permission-first, atomic, value-free in
    diagnostics, and leave no automatic plaintext backup.
12. CLI machine mode is non-secret. TUI secret mode requires a real TTY and
    never writes secrets to process arguments, environment, stdin capture,
    stdout, stderr, history, scrollback, title, or clipboard by default.
13. No dependency enters source, build, assets, fonts, templates, native
    runtime, packaging, or tests before clean-room and outbound-license review.
14. Generated schemas, catalogues, environment templates, release manifests,
    and lock files are generator-owned and never hand-edited.
15. No fixed sleeps, timing-luck polling, source/prose pinning tests,
    mock-mirroring, weakened assertions, skipped failures, or suppressed
    diagnostics.
16. Planning verification does not start apps, browsers, Docker, Aspire,
    Playwright, or live services. External platform/security/accessibility
    evidence remains a release gate, not a fabricated automated claim.

## 5. Architecture And Design Decisions

### 5.1 Shared contract boundary

- **Decision:** Move versioned manifest/package wire types, codecs, constrained
  legal Markdown, and portable content limits into the existing package-free
  `Event.Wire.Contracts`. Create `Event.Setup.Core` for environment catalogue,
  dotenv, offline validation/diff/readiness, and workflow state.
- **Why:** Application already references Wire Contracts. This removes the
  schema tool’s broad Application dependency and gives Setup adapters one small
  shared source without making server layers depend on a UI product.
- **Alternatives considered:**
  - Reference `Explore.Application` from the assistant: rejected as broad,
    framework-heavy, and a Clean Architecture inversion.
  - Put everything in `Explore.Domain`: rejected because the offline client
    would pull the complete business assembly.
  - Duplicate generated DTOs/parsers in Setup: rejected because behavior and
    security rules would drift.
  - Add a sixth new portability-contract project: viable but unnecessary while
    `Event.Wire.Contracts` already has the exact package-free role.
- **Consequences:** Domain gains one inward reference to package-free Wire
  Contracts if legal Markdown moves; architecture docs/tests explicitly model
  this shared-kernel exception. Application runtime orchestration stays put.
- **Affected:** `Event.Wire.Contracts`, Domain legal types, Application
  ConfigurationManifest contracts, schema generator, tests, architecture docs.

### 5.2 Headless workflow state machines

- **Decision:** Use immutable inputs/results and explicit state machines in
  `Event.Setup.Core`; UI state is mutable adapter state only.
- **Why:** Every adapter must share validation, relevance, readiness,
  sensitivity, and digest behavior without a shallow forwarding service layer.
- **Alternatives:** UI-owned view-model rules and a generic service bus are
  rejected as drift-prone and shallow.
- **Consequences:** Core methods are synchronous/pure unless cryptographic
  randomness is injected through a narrow platform port. Collections snapshot
  caller input and diagnostics contain stable codes/paths, never values.

### 5.3 Canonical environment catalogue

- **Decision:** One closed catalogue in `Event.Setup.Core` owns public variable
  metadata, topology/capability predicates, defaults, safe validators, secret
  classification, generation policy, restart behavior, and documentation
  anchors. Build tools generate/validate `.env.example` and Compose coverage.
- **Why:** Human prose and Compose interpolation are not safe executable
  product logic.
- **Alternatives:** Parse `.env.example` or copy `SecretDefinitionRegistry`;
  both are rejected.
- **Consequences:** `SecretDefinitionRegistry` remains binding authority;
  convergence tests require every secret-backed environment key to map to the
  catalogue without exposing Infisical coordinates or values.

### 5.4 Avalonia architecture

- **Decision:** `Event.SetupAssistant` contains shared Avalonia views, resources,
  view models, and workflows; Browser and Desktop are thin platform
  composition roots. Use built-in binding/notification primitives rather than
  adding an MVVM framework by default.
- **Why:** It minimizes the shipped graph and keeps behavioral authority in the
  core.
- **Alternatives:** Separate browser/desktop UIs lose parity; a third-party MVVM
  framework adds obligations without proven need.
- **Consequences:** Avalonia `12.1.1` is only a candidate until Phase 1 clears
  the full graph. Production excludes professional tooling and diagnostics.

### 5.5 Browser secret capability gate

- **Decision:** Build secret-mode behavior behind an immutable generated
  release capability manifest whose public default is disabled. Enabling
  requires exact-bundle CSP/request/storage evidence and independent security
  and legal approval.
- **Why:** Static WASM and `connect-src 'none'` do not remove origin control,
  extension, browser, or device risk.
- **Alternatives:** Always enable, feature flag from remote configuration, or
  claim “ISLAMU cannot access secrets” are rejected.
- **Consequences:** No network-loaded flags exist. The app preloads bundled
  locale/help/template assets before transition, then enters a terminal
  network-denied state.

### 5.6 Desktop safe-write ports

- **Decision:** Define one core write intent and separate Windows/Unix platform
  adapters that open handles safely, establish restrictive access before
  content exposure, flush, atomically replace, verify, and clean up.
- **Why:** ACLs, Unix modes, links/reparse points, and atomic replacement differ
  materially.
- **Alternatives:** `File.WriteAllText`, write-then-chmod, generic file-picker
  success, and plaintext backup are rejected.
- **Consequences:** unsupported filesystems fail closed by default; any
  override is advanced, explicit, and visibly lower assurance.

### 5.7 CLI and TUI boundary

- **Decision:** Handwritten deterministic command parsing follows existing
  repository CLIs; Terminal.Gui `2.4.17` is a candidate solely for the human
  TUI. Machine mode uses stable JSON schemas and never accepts secret values.
- **Why:** No additional command framework is required, and TUI screen state is
  not an automation contract.
- **Alternatives:** Agent-driven TUI, secret options, captured stdin, or
  localized prose parsing are rejected.
- **Consequences:** `event-setup` owns stable command/exit categories; secret
  completion is available only through an interactive TTY state machine.

### 5.8 Legal workspace

- **Decision:** Reuse the shared constrained legal Markdown codec and typed
  v1alpha2 legal records. The assistant edits source/readiness only and hands
  target publication to server authority.
- **Why:** It preserves XSS/resource denial, role boundaries, and immutable
  publication/acceptance evidence.
- **Alternatives:** raw HTML, embedded browser preview, external spellcheck,
  copied legal templates, and auto-publication are rejected.
- **Consequences:** the first release may ship blank editing and only approved,
  attributed, immutable local templates.

### 5.9 Packaging and release

- **Decision:** Release per RID/format from governed `eng/` tooling and CI
  adapters, with one identity joining commit, lock digest, SBOM, build manifest,
  checksums, signature/notarization, source, and reproducibility.
- **Why:** Secret-handling binaries require stronger substitution resistance
  than an unsigned archive.
- **Alternatives:** one generic Linux claim, mutable downloads, commercial
  packaging tooling, and scanner-only license approval are rejected.
- **Consequences:** initial stable matrix is Windows/macOS/Linux desktop,
  Browser static bundle, and six CLI RIDs. Wayland-native, AppImage, Flatpak,
  global tool, and reciprocal-license boundaries remain target gates.

## 6. Implementation Phases

### Phase 1: Contract Freeze, Dependency Gate, And Project Boundaries

- **Goal:** Establish a green, license-approved, executable architecture before
  feature code.
- **Depends on:** the 2026-08-30 user closure of ConfigurationManifest, the
  current v1alpha2 contracts/schema/registry/import-preview outputs frozen as
  the extraction baseline, user approval of this Setup revision, and
  revision-bound CTO review.
- **Relevant files:**
  - Existing: `Explore.slnx`, `Directory.Packages.props`,
    `Directory.Build.props`, `.github/workflows/test.yml`,
    `.ci/scripts/validate-dependency-license-policy.cs`,
    `tests/Event.Architecture.Tests/**`.
  - New: five Setup source projects, focused test projects, package locks,
    Setup architecture tests, and clean-room dependency evidence under this
    workstream.
- **Related skills/rules:** criticality-guardrail, ip-clean-room,
  agentic-research, clean-architecture-rules, tests rule.
- **Acceptance criteria:**
  - Candidate versions are accepted or replaced only after complete
    direct/transitive/native/tooling/asset/license/vulnerability review.
  - Setup project references form an acyclic inward graph and cannot reference
    server, network, persistence, provider, telemetry, AI, or commercial
    tooling assemblies.
  - Every product/test project is in the solution with a committed lock file.
  - Browser secret capability defaults disabled.
  - Clean-room source register, dependency decision, and SSO separation are
    reviewer-ready.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Remove only the new unshipped project/pin
  slice if a dependency fails review. Do not weaken scanners or keep an
  unapproved optional target.

### Phase 2: Shared Wire Contracts And Headless Core

- **Goal:** Move all offline-portable contract and codec authority into
  package-free shared assemblies without changing wire bytes.
- **Depends on:** Phase 1.
- **Relevant files:**
  - Existing: `src/Event.Wire.Contracts/**`,
    `src/Explore.Domain/LegalMarkdownContract.cs`,
    Application ConfigurationManifest contract/serialization/catalog files,
    schema generator, schema artifacts, and owning tests.
  - New: `src/Event.Setup.Core/**`,
    `tests/Event.Setup.Core.Tests/**`, Setup contract architecture tests.
- **Related skills/rules:** clean-architecture-rules,
  criticality-guardrail, record contracts, application/domain/tests rules.
- **Acceptance criteria:**
  - v1alpha2 JSON, legal Markdown HTML, diagnostics, limits, schemas, and
    digests remain byte/semantic equivalent through the new owner.
  - Obsolete Application/Domain definitions are deleted with no aliases.
  - Setup Core exposes strict parse/format/diff/coverage/readiness workflows
    and has no I/O or ambient time.
  - Application continues to own trusted target binding, live validation,
    authorization, transactions, and apply.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Wire.Contracts.UnitTests/Event.Wire.Contracts.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep the old owner until failing-first
  equivalence tests pass; perform one breaking cutover only after every caller
  is migrated.

### Phase 3: Environment Catalogue And Offline Workflows

- **Goal:** Deliver the canonical environment graph, explicit dotenv dialect,
  relevant-only planning/rendering, approved secret generation, and complete
  offline manifest/legal workflows.
- **Depends on:** Phase 2.
- **Relevant files:**
  - Existing: `.env.example`, `docker-compose.yml`,
    `SecretDefinitionRegistry.cs`, configuration/self-hosting docs.
  - New: Setup Core catalogue, predicate graph, dotenv codec, readiness model,
    generator/check tool under `eng/setup-assistant/`, and focused tests.
- **Related skills/rules:** criticality-guardrail, ip-clean-room, tests rule.
- **Acceptance criteria:**
  - Catalogue activation is closed, acyclic, deterministic, and covers every
    documented Compose/startup input.
  - `.env.example`, docs tables, and drift reports are generated or validated
    from the catalogue; hand-authored explanatory prose is not pinned by tests.
  - No-secret rendering produces only relevant empty secret placeholders and
    an incomplete readiness result.
  - Secret generation is limited to exact approved classes using platform
    cryptographic randomness.
  - Manifest/package/legal workflows produce stable digests, diagnostics,
    diffs, coverage, and readiness without target authority claims.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Generator/check mode must report drift before
  rewriting. Unknown keys or cyclic predicates block generation; they never
  fall through as optional.

### Phase 4: Versioned CLI And Terminal.Gui TUI

- **Goal:** Ship deterministic machine commands and a functionally equivalent
  human terminal workflow.
- **Depends on:** Phase 3.
- **Relevant files:**
  - New: `src/Event.SetupAssistant.Cli/**`,
    `tests/Event.SetupAssistant.Cli.Tests/**`,
    `schemas/event-setup-command-v1.schema.json`.
  - Existing patterns: release/schema generator CLI programs and skill schema.
- **Related skills/rules:** criticality-guardrail, accessibility,
  ip-clean-room, tests rule.
- **Acceptance criteria:**
  - Command families cover catalogue, manifest, tenant package, env, legal,
    doctor, and TUI workflows with help, dry-run, explicit paths, stable exit
    categories, and one JSON machine object.
  - Machine mode has no secret-input path and emits no localized prose,
    terminal escape, raw exception, or value.
  - TUI secret entry requires a real interactive TTY, is masked/non-echoing,
    disables stdout output, and clears state on every terminal transition.
  - Core parity tests prove equivalent artifact bytes and diagnostics.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.SetupAssistant.Cli.Tests/Event.SetupAssistant.Cli.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** An unstable command remains unshipped rather
  than receiving aliases. Secret TUI mode remains disabled if terminal
  capability or leakage invariants cannot be proven.

### Phase 5: Shared Avalonia Workspaces, Accessibility, And Localization

- **Goal:** Implement the shared GUI over the headless workflows, including
  manifest, environment, legal, review, and readiness experiences.
- **Depends on:** Phases 3 and 4 command contract.
- **Relevant files:**
  - New: `src/Event.SetupAssistant/**`,
    `tests/Event.SetupAssistant.Tests/**`, bundled resources/locales.
  - Existing: `docs/ACCESSIBILITY.md`, `docs/LOCALIZATION.md`.
- **Related skills/rules:** accessibility, criticality-guardrail,
  ip-clean-room.
- **Acceptance criteria:**
  - Shared views contain no validation, sensitivity, serialization, or
    portability authority.
  - Workspaces use semantic controls, stable automation metadata, keyboard
    completion, visible focus, error summary/field association, non-color
    status, reflow/scaling, reduced motion, bundled localization, and RTL.
  - Secret values never enter automation names, help text, announcements,
    clipboard, validation messages, or persistent edit history.
  - Legal editor uses constrained source/outline/sanitized preview and cannot
    load remote content or publish.
  - Browser, desktop, TUI, and CLI support metadata state their evidenced
    accessibility differences.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** A custom control without correct automation
  semantics is replaced by a native control. Unsupported locale or
  accessibility capability is labelled unavailable, not silently degraded.

### Phase 6: Browser Locality And Secret Boundary

- **Goal:** Produce the static browser target with a useful no-secret default
  and a release-gated secret state machine.
- **Depends on:** Phase 5.
- **Relevant files:**
  - New: `src/Event.SetupAssistant.Browser/**`,
    `tests/Event.SetupAssistant.Browser.Tests/**`, generated CSP/release
    capability manifest, static hosting/release configuration.
- **Related skills/rules:** criticality-guardrail, accessibility,
  ip-clean-room, agentic-research.
- **Acceptance criteria:**
  - No-secret mode works without secret input and never persists form state.
  - Secret mode cannot activate from URL, storage, cookie, history, or a
    remembered preference.
  - Production bundle contains no third-party remote asset, analytics,
    telemetry, crash upload, update check, service worker, CSP reporter, or
    diagnostics package.
  - Exact CSP intent denies connections, forms, framing, objects, remote
    resources/workers, and external navigation during secret state while
    permitting only the pinned WASM runtime requirements.
  - Public release capability remains disabled until independent exact-bundle
    request/storage/origin/security/legal evidence is recorded.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.SetupAssistant.Browser.Tests/Event.SetupAssistant.Browser.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Disable or remove hosted secret mode without
  affecting no-secret operation. A CSP relaxation requires a fresh threat
  model and I-VSD refresh.

### Phase 7: Desktop Protected Output

- **Goal:** Implement Windows, Linux, and macOS desktop composition and
  platform-specific protected file output.
- **Depends on:** Phase 5.
- **Relevant files:**
  - New: `src/Event.SetupAssistant.Desktop/**`,
    platform adapters, `tests/Event.SetupAssistant.Desktop.Tests/**`.
- **Related skills/rules:** criticality-guardrail, accessibility,
  ip-clean-room.
- **Acceptance criteria:**
  - File selection and write intent remain UI/platform concerns; secret bytes
    pass directly to the protected writer without preview-string copies.
  - Windows verifies current-user ACL/reparse safety; Unix verifies owner
    read/write mode and link/file identity; all platforms use same-directory
    temporary files, flush, atomic install, and cleanup.
  - Existing-target redacted diff and overwrite approval precede replacement.
  - Browser download limitations and unsupported desktop filesystems receive
    truthful lower-assurance messaging.
  - No recent-secret file, autosave, restore state, plaintext backup, or
    automatic clipboard copy exists.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.SetupAssistant.Desktop.Tests/Event.SetupAssistant.Desktop.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** A failing target adapter is omitted from the
  support matrix; it does not fall back to unprotected writes.

### Phase 8: Packaging, Provenance, Documentation, And Agent Skill

- **Goal:** Produce governed multi-target release outputs, operator/security
  documentation, and the post-CLI skill.
- **Depends on:** Phases 1–7 and the exact public capabilities selected for the
  release.
- **Relevant files:**
  - New: `eng/setup-assistant/**`, Setup CI/release workflow paths, package
    manifests, SBOM/notices/checksum/provenance evidence, Setup docs,
    `.agents/skills/setup-assistant-cli/**`,
    `docs/releases/changes/CHG-*.yaml`.
  - Existing: release policy/runbook/checklist, scope registry, CI governance,
    security/configuration/secrets/self-hosting/troubleshooting docs,
    `.agents/contract/intents.yaml`, skill schema, architecture tests.
- **Related skills/rules:** ci-cd intent, ip-clean-room, skill-authoring,
  conventional-commit, criticality-guardrail.
- **Acceptance criteria:**
  - Required archives/packages cover evidenced Windows, Linux, macOS, browser,
    and CLI targets; experimental/optional formats remain absent until gated.
  - Release identity converges across commit, version, RID/format, locks, SBOM,
    build manifest, checksums, signatures/notarization, source, and
    reproducibility.
  - Docs state origin trust, browser/desktop/terminal limitations, secret
    completion, recovery, incident, support, accessibility, localization,
    package support, and no-live-authority boundaries.
  - Public release scope `setup` is governed.
  - Agent skill routes only implemented compatible CLI commands, defaults to
    no-secret dry-run machine mode, rejects secret inputs/TUI automation, and
    requires human approval.
  - I-VSD, threat model, dependency evidence, accessibility/security/legal
    escalations, and Tier 2 change fragment match shipped capabilities.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Release only the target/capability subset
  with complete evidence. Never retain mutable, unsigned, unscanned, or
  falsely supported artifacts as a fallback.

## 7. Testing Strategy

### 7.1 Invariant anchors

- `Event.Wire.Contracts.UnitTests`: exact v1alpha2 bytes, strict lexical
  rejection, legal Markdown safety, collection ownership, and schema identity.
- `Event.Setup.Core.Tests`: environment predicate graph, dotenv dialect,
  relevance/defaults, separation, readiness, secret generation, diff/coverage,
  and value-safe diagnostics.
- `Event.SetupAssistant.Cli.Tests`: command schema, stable exit categories,
  noninteractive determinism, TTY secret boundary, and parity.
- `Event.SetupAssistant.Tests`: view-model state, semantic accessibility
  metadata, localization/RTL resource completeness, and no-secret leakage.
- `Event.SetupAssistant.Browser.Tests`: release capability state, network/store
  denial adapters, navigation clearing, static asset/CSP contract, and no
  remote/telemetry dependencies.
- `Event.SetupAssistant.Desktop.Tests`: real temporary-filesystem link,
  overwrite, permission, atomicity, cleanup, and race invariants on supported
  runners.
- `Event.Architecture.Tests`: project-reference boundaries, dependency/license
  coverage, no AI/provider/telemetry/network leakage, generator convergence,
  source/lock/release/skill contracts.

### 7.2 High-leverage adversarial scenarios

- secrets smuggled into manifest, package, diagnostics, machine JSON, support
  report, automation properties, arguments/environment/stdin, logs, or output;
- environment predicate cycles, default drift, duplicate keys, Compose
  interpolation, multiline/Unicode/newline/quoting attacks;
- browser state restoration, deep-link activation, remote resource channels,
  CSP reporting, external navigation, and stale release capability;
- symlink/reparse swaps, special files, permission failure, partial writes,
  atomic replacement races, and overwrite confusion;
- wrong-role legal text, unresolved placeholders, unsafe links/HTML/resources,
  unapproved templates, and attempts to migrate publication/acceptance history;
- incompatible package/license/native/tooling graphs and mismatched
  artifact/SBOM/checksum/signature/source identities;
- skill/CLI version drift, invented commands, secret-bearing files, and
  authority-broadening writes without approval.

Tests assert public codecs, workflow results, command JSON, file state, package
manifests, and closed error codes. They do not assert internal call counts,
framework cancellation mechanics, raw source/CSS/prose, or duplicated
production calculations.

### 7.3 Verification lane

Each phase runs one Release build and at most the single test project named in
Section 6 after all phase tasks. Focused iteration uses one TUnit class selector
with `--treenode-filter`. No plan task starts the application, browser, Docker,
Aspire, Playwright, Chrome DevTools, or a live service.

Independent browser request/storage review, platform assistive-technology
audits, package install/launch evidence, signing/notarization, and legal review
are explicit enablement/release gates. Absence of that evidence disables the
claim or target; this plan does not counterfeit it with unit tests.

## 8. Documentation, Configuration, And Operations Impact

### Documentation

- Update `docs/ARCHITECTURE.md` and `docs/CODEBASE_STRUCTURE.md` for the
  package-free shared-kernel and Setup project graph.
- Update `docs/CONFIGURATION.md`, `docs/SECRETS.md`, and
  `docs/SELF_HOSTING.md` for catalogue-generated relevant-only dotenv,
  no-secret placeholders, separation, and Setup CLI/GUI.
- Update `docs/SECURITY-MODEL.md` for browser origin/network/storage,
  terminal, desktop file, observability, and live-authority boundaries.
- Update `docs/ACCESSIBILITY.md` and `docs/LOCALIZATION.md` with Avalonia and
  Terminal target-specific contracts rather than Blazor-only assumptions.
- Update `docs/OPERATIONS.md`, `docs/TROUBLESHOOTING.md`,
  `docs/CI_CD_GOVERNANCE.md`, `docs/RELEASE_CHECKLIST.md`,
  `docs/RELEASE_POLICY.md`, and `docs/RELEASE_RUNBOOK.md` for packaging,
  signing, incident, provenance, support matrix, and recovery.
- Update `docs/legal/IP_GOVERNANCE.md` only if the implementation discovers a
  reusable new target-license rule; ordinary dependency decisions remain in
  workstream/release evidence.

### Configuration and generated artifacts

- Add central package pins and lock files only after approval.
- Add generated environment catalogue outputs and a check mode; `.env.example`
  remains the template, never a secret source file committed with values.
- Add command JSON schema, Setup release capability/support manifests,
  checksums, SBOMs, notices, and provenance metadata.
- Keep browser source tracked; ignore only generated publish/build/release
  output.
- Add no secret, credential, signing key, certificate password, connection
  string, or encryption key to source. CI signing/notarization secrets remain
  environment-scoped external secrets.

### Operations

- No runtime health endpoint or remote telemetry is added to the offline
  product.
- A local value-safe `doctor` command reports release/platform/capability
  status and closed codes.
- Hosted browser deployment uses immutable static bundles and explicit
  security headers; secret mode enablement is a governed release decision.
- Support and incident workflows never request generated dotenv or secret
  values.

### 8.1 Release & Changelog Strategy

This is **Tier 2**: a new shipped security-sensitive/operator-facing product
with configuration, secret, packaging, and release impact.

- Add `setup` to the public release scope registry.
- Use `feat(setup): ...` for the terminal Conventional Commit subject.
- The final task creates an append-only
  `docs/releases/changes/CHG-YYYY-NNNN.yaml` through the repository release
  engine, validates it with `ReleaseInputPolicy`, and uses the exact terminal
  `Change-Id: CHG-YYYY-NNNN` footer.
- Include `BREAKING CHANGE:` only if the shared contract move changes a
  documented public integration beyond the pre-release internal namespace
  relocation.
- No `Changelog: skip` is permitted for the product release.

## 9. Islamic Value-Sensitive Design & Moral Boundaries

The linked I-VSD report is provider-responsibility design reasoning, not a
fatwa, Sharia certification, security certification, legal advice,
accessibility certification, or proof of zero disclosure. Its reviewed input
revision is
`sha256:8c86d6be6f612861bba9c4ea641a451722fe0d6b5feccad09ac310b5cdce1637`;
status is `current`; disposition is `plan-aligned`.

| I-VSD mapping | Scenario and task mapping | Disposition |
|---|---|---|
| `IVSD-F001` / `IVSD-M001` | 3.1, 3.2, 3.3; SA-230, SA-340, SA-520 | Implement focused product workspaces |
| `IVSD-F002` / `IVSD-M002` | 3.1; SA-110, SA-210, SA-220, SA-230 | Shared Wire Contracts plus headless Core |
| `IVSD-F003` / `IVSD-M003` | 3.5, 3.6; SA-610, SA-620, SA-630, SA-640 | Implement; independent review gates enablement |
| `IVSD-F004` / `IVSD-M004` | 3.5; SA-610, SA-620, SA-630 | Protective browser default |
| `IVSD-F005` / `IVSD-M005` | 3.6; SA-610, SA-630, SA-640 | Implement denial; exact-bundle evidence required |
| `IVSD-F006` / `IVSD-M006` | 3.4, 3.5, 3.6; SA-610, SA-620, SA-630 | No browser persistence and bounded lifetime |
| `IVSD-F007` / `IVSD-M007` | 3.3; SA-310, SA-320, SA-330 | Relevant empty placeholders and incomplete readiness |
| `IVSD-F008` / `IVSD-M008` | 3.1, 3.3; SA-310, SA-320 | Canonical environment catalogue |
| `IVSD-F009` / `IVSD-M009` | 3.2, 3.3; SA-210, SA-310, SA-330, SA-340 | Artifact and sensitivity separation |
| `IVSD-F010` / `IVSD-M010` | 3.7; SA-710, SA-720, SA-730 | Platform-safe desktop writes |
| `IVSD-F011` / `IVSD-M011` | 3.4, 3.7; SA-330, SA-630, SA-720, SA-730 | Minimize lifetime; no erasure claim |
| `IVSD-F012` / `IVSD-M012` | 3.4, 3.6; SA-110, SA-610, SA-640, SA-730 | Observability-free secret paths |
| `IVSD-F013` / `IVSD-M013` | 3.11; SA-110, SA-120, SA-810, SA-820 | Target-specific dependency approval |
| `IVSD-F014` / `IVSD-M014` | 3.11; SA-810, SA-820 | Signed, identified, verifiable releases |
| `IVSD-F015` / `IVSD-M015` | 3.10, 3.11; SA-510, SA-810, SA-820 | Evidence-backed support matrix |
| `IVSD-F016` / `IVSD-M016` | 3.11; SA-120, SA-130, SA-640, SA-810 | Track browser source; ignore generated output |
| `IVSD-F017` / `IVSD-M017` | 3.5, 3.11; SA-630, SA-640, SA-820 | Official identity and fork disclosure |
| `IVSD-F018` / `IVSD-M018` | 3.10; SA-510, SA-520, SA-530 | Accessible GUI behavior |
| `IVSD-F019` / `IVSD-M019` | 3.10; SA-510, SA-520 | Bundled localization and RTL |
| `IVSD-F020` / `IVSD-M020` | 3.2, 3.3, 3.4; SA-320, SA-330, SA-340 | No live secret authority |
| `IVSD-F021` / `IVSD-M021` | 3.5, 3.11; SA-630, SA-640, SA-820 | Truthful security claims |
| `IVSD-F022` / `IVSD-M022` | 3.4–3.7, 3.11; SA-610, SA-710, SA-810, SA-820 | Adversarial evidence and external release gates |
| `IVSD-F023` / `IVSD-M023` | 3.8; SA-210, SA-340, SA-530, SA-540 | Typed role-scoped legal source |
| `IVSD-F024` / `IVSD-M024` | 3.8; SA-210, SA-340, SA-530, SA-540 | Never rewrite publication/acceptance evidence |
| `IVSD-F025` / `IVSD-M025` | 3.8; SA-540, SA-820 | Approved local non-certifying templates only |
| `IVSD-F026` / `IVSD-M026` | 3.8; SA-210, SA-220, SA-540 | One constrained Markdown codec |
| `IVSD-F027` / `IVSD-M027` | 3.2, 3.8; SA-210, SA-340, SA-540 | Portable legal source and metadata |
| `IVSD-F028` / `IVSD-M028` | 3.2, 3.8; SA-210, SA-340, SA-540 | Bounded localized content and usable diff |
| `IVSD-F029` / `IVSD-M029` | 3.9, 3.10; SA-410, SA-420, SA-430 | First-class CLI/TUI |
| `IVSD-F030` / `IVSD-M030` | 3.9; SA-410, SA-420 | Versioned JSON, exits, help, dry-run, digests |
| `IVSD-F031` / `IVSD-M031` | 3.4, 3.9; SA-410, SA-420, SA-430 | Terminal secret boundary |
| `IVSD-F032` / `IVSD-M032` | 3.12; SA-840 | Skill rejects secret access |
| `IVSD-F033` / `IVSD-M033` | 3.12; SA-110, SA-840 | No embedded AI/provider dependency |
| `IVSD-F034` / `IVSD-M034` | 3.8, 3.12; SA-420, SA-530, SA-540, SA-840 | Human approval gates |
| `IVSD-F035` / `IVSD-M035` | 3.10; SA-430, SA-520, SA-820 | Truthful TUI/browser accessibility evidence |
| `IVSD-F036` / `IVSD-M036` | 3.12; SA-410, SA-420, SA-840 | Skill only after verified CLI contract |

### Escalation gates

- Independent security and qualified legal review before hosted secret mode is
  enabled.
- Qualified dependency/outbound-license review for every target graph and any
  reciprocal component.
- Qualified legal review for templates, role claims, origin/privacy/trademark
  wording, and source-offer obligations.
- Platform accessibility review before claiming target conformance.
- Release-engineering approval before signing/notarization/package support
  claims.
- Qualified Sunni scholarly review only if future marketing, contracts,
  payments, or product claims introduce a religious-legal conclusion. None is
  made here.

## 10. Security, Authorization, Privacy, And Abuse Considerations

### Threat model

| Asset / promise | Adversary or failure | Trust boundary | Fail-closed control |
|---|---|---|---|
| Entered/generated secrets | Malicious or compromised hosting origin, extension, browser, local process, recorder, support capture | Browser delivery/runtime and terminal process | No-secret default, exact-release disclosure, release-disabled hosted secret mode, preload/network/storage denial, TTY-only human path, value-free support |
| Protected dotenv bytes | Symlink/reparse swap, weak filesystem, concurrent overwrite, backup/sync exposure | Desktop picker and destination filesystem | Handle-first inspection, permission-first same-directory temp, explicit overwrite, atomic install, post-check, cleanup, no safe claim on override |
| Portable manifest/package | Malicious artifact, secret/PII smuggling, wrong authority, oversized content, source IDs | Shared codec and offline workflow | Closed schema/registry, exact limits, strict lexical parser, stable identities, secret/PII/topology exclusions, server authority for live target |
| Legal source and role claims | Unsafe Markdown, copied template, wrong operator role, auto-publication, fabricated acceptance | Legal editor to portable draft and later server publication | Constrained codec, typed role/provenance, approved local templates only, readiness gate, draft/new-version handoff, no acceptance migration |
| Command automation | Untrusted arguments/files, unstable prose, skill drift, agent secret request | CLI process and external agent | Versioned machine JSON, stable exits, non-secret machine mode, explicit paths/dry-run, skill version gate, human approval |
| Release identity | Dependency substitution, incompatible license, unsigned package, stale web bundle, false support claim | Restore/build/package/sign/host chain | Locked graph, vulnerability/license gates, SBOM/checksum/provenance, signatures/notarization, immutable capability/support manifests |

- **Authentication/authorization:** Not applicable to the offline first
  release. No token or live API path exists. Future live operations require a
  separate auth/BFF plan and HAL authority.
- **Tenant isolation:** Applicable to artifact scope. Tenant packages cannot
  contain instance authority; source names/IDs never become trusted target
  authority.
- **Secrets:** Critical. Values remain local, ephemeral where practical,
  non-observable, and confined to protected output.
- **Browser origin:** Critical. The origin controls delivered code; disclosure,
  reproducibility, source, digests, CSP, and review provide evidence but not
  technical impossibility.
- **Rate limiting:** Not applicable without a server endpoint.
- **Idempotency:** File writes use digest/target identity and explicit overwrite;
  commands are deterministic and dry-run capable.
- **Auditability:** No secret activity audit captures values. Release and
  dependency decisions are audited; user-created support reports are explicit
  and value-safe.
- **Abuse:** Reject artifact bombs over canonical size/depth/count limits,
  unsafe Markdown, path/link tricks, malicious dotenv syntax, command
  injection assumptions, and unofficial-origin overclaims.
- **Privacy:** No analytics, tracking, remote logs, user accounts, or cloud
  storage. Non-secret profiles persist only after explicit opt-in.

## 11. Multi-Tenancy, Federation, Localization, Accessibility, And Product

| Concern | Classification | Reason |
|---|---|---|
| Multi-tenancy | Applicable | Tenant package scope and legal authority must not broaden to instance scope. |
| Federation | Not applicable | ATProto provider credentials/live behavior are excluded; catalogue may explain required keys without values. |
| Localization | Applicable | All security consequences, diagnostics explanations, help, and templates are bundled before secret mode. |
| Accessibility | Applicable | Avalonia desktop, browser partial support, and Terminal.Gui require distinct evidence and honest alternatives. |
| Product autonomy | Applicable | No-secret/default offline paths reduce dependence on ISLAMU hosting and Infisical. |
| Payments | Not applicable to Setup authority | Portable paid-policy intent may be displayed, but sovereign payment execution/credentials remain excluded and server-authoritative. |
| Legal authority | Applicable | Instance/tenant/organizer roles, templates, publication, and acceptance remain explicit and separate. |

## 12. Observability And Operations

- No remote telemetry, analytics, crash upload, update check, session replay, or
  CSP report endpoint.
- Local diagnostics use closed codes, release identity, platform, and
  capability keys only.
- `event-setup doctor` reports package/runtime/file capability readiness
  without paths/usernames unless explicitly consented and without environment
  values.
- Release metrics are build/package evidence, not user behavior.
- Incident runbooks cover compromised origin/artifact/signing key, incorrect
  package, secret exposure report, dependency advisory, and capability
  disablement.
- Hosted secret mode can be disabled by publishing a new immutable capability
  manifest/bundle; no remote runtime toggle weakens the offline boundary.

## 13. Migration And Compatibility Plan

- This is pre-v1 greenfield. Move v1alpha2 contracts once; delete old
  Application/Domain owners and migrate all callers with no compatibility
  namespace, type forwarding, serializer alias, or duplicate schema.
- No database migration is planned for Setup Assistant. Existing server
  legal/import persistence remains authoritative and outside this offline
  product.
- Existing generated schema bytes are the frozen extraction baseline. A future
  intentional contract change requires a new approved workstream and a Setup
  plan/I-VSD refresh before extraction continues.
- `.env.example` becomes generator-owned in one cutover; handwritten sections
  are represented as catalogue metadata or adjacent maintained prose.
- Release artifacts are new and need no upgrade compatibility. Once a CLI
  schema ships, changes follow its explicit version policy; pre-release
  commands may still break cleanly with coordinated skill/schema updates.
- Rollback removes the new target/capability or restores the last signed
  artifact; it never falls back to unprotected writes, remote secret handling,
  or stale command aliases.

## 14. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection signal | Owner/task |
|---|---:|---:|---|---|---|
| Frozen manifest contract drifts during extraction | Low | High | Pin the archive disposition and fail byte/schema/registry convergence before moving owners | Any diff from the recorded v1alpha2/schema/registry baseline | SA-110, SA-210 |
| Hidden package/native/license incompatibility | Medium | Critical | Full target graph, scanner, legal decision, target exclusion | Unknown/denied metadata or lock/SBOM mismatch | SA-120, SA-810 |
| Hosted build exfiltrates secrets | Low after controls | Critical | Disabled-by-default capability, preload, deny policy, independent review | Request/storage evidence or origin mismatch | SA-610–SA-640 |
| Managed memory retains secrets | High | High | Minimize copies/lifetime; clear state; no erasure claim | Heap/DOM/state review findings | SA-330, SA-630, SA-730 |
| Unsafe desktop overwrite/link race | Medium | Critical | Handle-first platform adapters and real filesystem invariants | Permission/link/identity mismatch | SA-710–SA-730 |
| Environment catalogue drifts from runtime/Compose | Medium | High | One generator/check graph and closed convergence tests | Generated artifact diff or unknown key | SA-310, SA-320 |
| Cross-platform claim exceeds evidence | Medium | High | Release support manifest per exact OS/RID/format | Missing package/sign/accessibility evidence | SA-810, SA-820 |
| Browser accessibility is materially incomplete | High | High | Honest limitation, semantic controls, desktop/CLI alternative | Missing platform accessibility evidence | SA-520, SA-820 |
| TUI leaks through terminal/process surfaces | Medium | Critical | TTY-only secret state; no args/env/stdin/stdout/history | Captured output/process contract failure | SA-410–SA-430 |
| Legal template or role claim misleads | Medium | Critical | Blank/approved templates only, provenance and legal gate | Missing approval or wrong-scope validation | SA-530, SA-820 |
| Skill invents commands or handles secrets | Medium | High | Create after CLI; version gate; machine-only no-secret workflow | Schema/version mismatch or secret input | SA-840 |
| Release docs expose overconfident claims | Medium | High | I-VSD/claim review and identified-release wording | Claim inventory/review failure | SA-820, SA-850 |

## 15. Success Metrics And Definition Of Done

- All five Setup product projects and focused tests exist with approved locked
  graphs and clean architecture.
- Same core input yields byte-identical artifacts and equivalent closed
  diagnostics across server static validation, schema tool, CLI, TUI, desktop,
  and browser.
- Environment catalogue covers every supported startup/Compose key and
  generates/validates relevant operator artifacts without drift.
- No-secret mode produces useful incomplete output without values; secret mode
  never crosses forbidden process/browser/observability boundaries.
- Unsafe desktop targets fail closed and leave no partial plaintext.
- CLI machine schema, exit categories, help, digests, and dry-run are stable;
  TUI secret completion is human/TTY-only.
- Legal source is typed, bounded, safe, portable, and never mutates publication
  or acceptance evidence.
- Every advertised target has matching lock/SBOM/checksum/signature/provenance,
  support, accessibility, and packaging evidence.
- Hosted secret mode is enabled only if exact-bundle independent evidence
  passes; otherwise the complete no-secret product ships without it.
- Agent skill matches the implemented CLI and cannot ingest secrets.
- Every phase’s single build/test gate is green, the Tier 2 change fragment is
  valid, I-VSD is current/plan-aligned, and CTO/user approvals bind the final
  revision.

## 16. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. On first start or cold resume, read task-owned context and the current task,
   then only the referenced plan phase/decision. Do not reload the full triad.
2. Keep a `path + heading/symbol + revision` ledger and reopen only invalidated
   evidence.
3. Start from the highest-priority unchecked task unless the user overrides it.
4. Treat `tasks.md` as the sole hot ledger. Check a substantial task
   immediately after acceptance and reconcile small related tasks no later than
   phase end.
5. Keep implementation and phase-verification checkboxes separate. A phase is
   complete only after its build and selected test pass.
6. Update counts, current priority, next slice, discovered tasks, deferred work,
   and `Last Updated` when task state changes.
7. Update context after a phase, material decision, blocker, failed validation,
   scope discovery, or handoff.
8. Update this plan only when scope, architecture, phase order, acceptance,
   risk, or validation strategy changes.
9. Record a failed gate with cause and recovery; never mark it complete.
10. Before pause/compaction/transfer/PR, reconcile tasks and add a concise dated
    context handoff naming unrelated dirty files.
11. Run phase verification only after all phase tasks: one Release build and at
    most the selected project test. Do not repeat successful commands or start
    apps/browsers/services.
12. Never report implementation complete when repository reality, external
    enablement gates, and the ledger disagree.

Every implementation summary teaches what changed, architecture and patterns,
libraries/infrastructure/protocols, key files/types and responsibilities,
data/control flow, security/reliability conventions, exact verification,
remaining work, next slice, and dev-doc status.

## 17. Progress Reporting Contract

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete, gated, or deferred work
Next: recommended next slice
Docs updated: tasks reconciled; context/plan updated or unchanged with reason
```

## 18. Potential Risks & Unknowns

The hardest risk is not Avalonia implementation; it is proving a coherent
security and release promise across fundamentally unequal targets. Shared XAML
does not make browser accessibility equal to desktop, client-side WASM does not
remove origin trust, and a successful local file write does not prove safe
permissions on every filesystem. The plan therefore treats support,
secret-mode enablement, and package formats as evidence-backed release
capabilities rather than aspirations. The second risk is archive-boundary
confusion: ConfigurationManifest was closed by explicit user decision with
later planned phases retired rather than implemented. Setup must preserve the
frozen current wire/schema/registry behavior and must never market retired
atomic-apply, live migration, managed ownership, or direct-transfer work as an
existing capability.
