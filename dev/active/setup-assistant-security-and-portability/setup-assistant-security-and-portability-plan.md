<!-- ABOUTME: Canonical architecture plan for the cross-platform ISLAMU Event Setup Assistant. -->
<!-- ABOUTME: Defines behavior, security boundaries, phased delivery, verification, and I-VSD traceability. -->

# Setup Assistant Security And Portability — Implementation Plan

Last Updated: 2026-08-31 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Write an implementation plan for
  `islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md`
  as the next workstream after ConfigurationManifest.
- **Task directory:**
  `dev/active/setup-assistant-security-and-portability/`
- **Planning status:** Successor A foundation-offline is approved and active;
  SA-120 is Green and SA-130 is the current slice.
- **Change classification:** Behavioral Delta. This work adds a shipped
  cross-platform product, command contract, secret-handling workflows,
  generated deployment artifacts, legal-content authoring, packaging, and
  release behavior.
- **Complexity:** XL. The work crosses a package-free shared contract and
  headless core, a repository-native BCL terminal wizard, framework-neutral
  desktop/browser outcomes, deterministic CLI, security boundaries,
  accessibility/localization, six release RIDs, Linux packaging,
  signing/provenance, dependency governance, and agent context.
- **Primary intent:** `external-infrastructure-bootstrap` — Tier 1 Security,
  exhaustive threat-boundary exploration, Invariant-Breakers, fail-closed
  secret handling, and security/operations review.
- **Supporting intents:** `ci-cd-change`,
  `ip-clean-room-governance`, and `create-agent-context-skill`.
- **Inherited contracts:** `legal-identity-authority-change` and the
  active ConfigurationManifest workstream govern legal role authority, immutable
  publication/acceptance evidence, tenant isolation, server-side apply, and
  configuration migration behavior already present in the authoritative worktree. This plan
  consumes the frozen v1alpha2 wire/schema/registry/import-preview baseline
  rather than redefining those boundaries. Open ConfigurationManifest phase
  gates are upstream verification state, not Setup implementation evidence.
- **Fallback scope contract:** No current intent grants a GUI framework or
  shipped presentation target. Successor A may create the new `src/Event.Setup*`
  and corresponding `tests/Event.Setup*` boundaries as package-free shells to
  satisfy the project graph; only Core and CLI/terminal behavior is functional
  or shippable in A. Successor B owns GUI framework selection and actual
  browser/desktop presentation targets. Later scope includes package/solution/
  CI/release integration, generated environment assets, setup documentation,
  the post-CLI agent skill, live target adapters, and separately authorized
  migration orchestration. Server behavior remains authoritative and uses
  existing API/HAL/BFF, import-session, transfer, and transactional seams.
- **Relevant skills:** implementation-plan, i-vsd, grill-me,
  criticality-guardrail, clean-architecture-rules, ip-clean-room,
  agentic-research, accessibility, skill-authoring, auth-patterns,
  blazor-bff-patterns, and dotnet-efcore-guidelines.
- **Relevant rules:** `.agents/rules/application-layer.md`,
  `.agents/rules/domain.md`, `.agents/rules/tests.md`, and
  `.agents/rules/ip-clean-room.md`. Existing path rules do not cover a future
  GUI framework; successor B must add executable presentation boundaries rather
  than informally borrowing Blazor-specific rules.
- **Primary layers:** `Event.Wire.Contracts`, new `Event.Setup.Core`,
  package-free disabled presentation target shells in A, a repository-native
  CLI/terminal adapter, successor-B GUI/browser/desktop adapters, release
  engineering, and agent context.
- **I-VSD document:**
  [i-vsd-setup-assistant-security-and-portability.md](../../../islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md)
- **I-VSD reviewed input revision:**
  `sha256:d2bbba40455c013e20883ab6202f84411bb05f2c20f6060a9e73095f44a8e4b1`
- **I-VSD status / disposition:** `current` / `plan-aligned`; findings
  `IVSD-F001`–`IVSD-F046` and mitigations `IVSD-M001`–`IVSD-M046` preserve the
  BCL-only successor-A graph and framework-neutral successor-B boundary.
- **Clean-room evidence:**
  [setup-assistant-security-and-portability-clean-room-evidence.md](setup-assistant-security-and-portability-clean-room-evidence.md)
- **Dependency evidence:**
  [setup-assistant-security-and-portability-dependency-evidence.md](setup-assistant-security-and-portability-dependency-evidence.md)
- **First CTO review:** [Split before approval](setup-assistant-security-and-portability-cto-review.md), bound to the prior plan/tasks revisions.
- **Current correction review:** [Approved the BCL-only successor-A strategy](setup-assistant-security-and-portability-cto-review.md);
  successors B-G inherit no approval.
- **User approval:** [Bound to the reviewed BCL-only successor-A revision](setup-assistant-security-and-portability-approval.md).
- **Grill-Me intake:** The existing I-VSD consultation records the user’s
  material choices: web plus desktop, protective no-secret browser default,
  optional trust-based web secret mode, open/auditable browser source,
  relevant-only dotenv, instance and tenant portability, constrained legal
  Markdown, an interactive terminal workflow plus machine CLI, compatible FOSS
  only, no embedded AI, and a post-CLI agent skill. Repository evidence resolves the remaining
  architecture: reuse package-free `Event.Wire.Contracts`, add a pure
  `Event.Setup.Core`, keep all live authority server-side, promote the former
  deferred composition, live-target, secret-binding, application-data, and
  payment-operation work into explicit later phases, and ship hosted
  secret mode disabled until independent evidence approves the exact release.
  The 2026-08-31 expansion is user-directed; I-VSD and CTO revalidation remain
  mandatory before those new high-criticality phases start.

### 0.1 Umbrella Program And Successor Approval Boundaries

This directory is the canonical umbrella for the complete user-approved Setup
Assistant program. It preserves all requirements, Scenarios 3.1-3.15, 41 SA
IDs, and 12 phase gates, but it grants implementation authority to no successor.
Successor directories are intentionally not created by this correction.

| Successor boundary | Independently reviewable PR slices | Entry and owned outcome |
|---|---|---|
| **A. `setup-assistant-foundation-offline`** | A1 SA-110-SA-130 architecture/dependency/CI and ten package-free shells; A2 SA-210-SA-230 wire/core; A3 SA-310-SA-340 catalogue/offline; A4 SA-410-SA-430 CLI/BCL terminal wizard | Sole candidate successor, paused for changed-revision review. After planning-mode I-VSD revalidation and fresh CTO/user approval, ships the non-secret offline CLI/terminal product with no live authority. Presentation/Browser/Desktop shells are disabled and non-shipped. |
| **B. `setup-assistant-presentation-targets`** | B1 SA-510-SA-540 framework-neutral shared GUI/legal; B2 SA-610-SA-640 browser; B3 SA-710-SA-730 desktop | Owns actual GUI framework selection and runtime targets. Requires stable A public contracts, a provenance-complete graph or new authoritative Avalonia evidence, fresh I-VSD/CTO/user approval, and target security/accessibility/release evidence; each target ships or remains disabled independently. |
| **C. `setup-assistant-composition-scale`** | C1 SA-810 and SA-820 canonical composition; C2 SA-830 measured profiles | Requires stable A2/A3 contracts; canonical JSON remains unchanged and every larger profile is separately evidenced. |
| **D. `setup-assistant-live-control-plane`** | D1 SA-910 Red plus server enrollment/authorization contracts; D2 SA-920 and SA-930 server behavior/generated contract; D3 SA-1010 Red plus SA-1020 and SA-1030 Setup adapters/UI | Requires fresh Tier 1 intake, current I-VSD, fresh CTO/user approval, and green ConfigurationManifest Tier 1/tenant/atomicity evidence. Backend precedes activation; Setup owns no local authority. |
| **E. `setup-application-data-migration`** | E1 SA-1110 privacy/tenant Red; E2 SA-1120 Domain/Persistence/outbox; E3 SA-1130 API/HAL/generated client; E4 Setup UI activation | Requires D contracts plus fresh Tier 2 custody/erasure and Tier 1 tenant intake, current I-VSD, fresh CTO/user approval, and named privacy/provider evidence. |
| **F. `setup-sovereign-payment-migration`** | F1 dedicated Worst Break Red and Tier 0 decision record; F2 SA-1140 Domain/Persistence/provider reconciliation; F3 API/HAL/Setup activation | Requires D/E contracts and independent Tier 0 Grill-Me, current I-VSD, fresh CTO/user approval, and provider/legal/operator evidence. It may remain permanently disabled. |
| **G. `setup-release-and-agent-contract`** | G1 SA-1210 and SA-1220 per selected target/capability; G2 SA-1240 only after CLI schema ships; G3 SA-1250 program reconciliation | Requires each owning successor green; release evidence describes only its implemented/evidenced subset. |

Dependencies are one-way: A -> B/C -> D -> E; F depends on D/E contracts but is
independently optional; G runs for each shippable subset and again for final
program reconciliation. No later successor inherits umbrella, A, prior-user,
or prior-CTO approval. Before implementation, each successor MUST bind the
current I-VSD revision plus fresh tier-appropriate intake, CTO review, explicit
user approval, and its named evidence to its own exact plan/tasks revisions.
Missing evidence leaves that successor or capability disabled; it does not
create a compatibility shim or authorize another boundary.

Foundation A remains the only candidate successor for implementation, but its
changed revision is paused. SA-110's existing Red evidence remains recorded;
SA-120 and all implementation are blocked until planning-mode I-VSD
revalidation, fresh CTO review, and exact-revision user approval.

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
- a versioned machine CLI plus a human repository-native BCL interactive
  terminal wizard;
- shared, framework-neutral GUI outcomes for browser and desktop in successor B;
- constrained legal-document editing over the existing legal Markdown
  contract;
- deterministic release artifacts, SBOMs, checksums, signatures/provenance,
  and truthful support claims; and
- a schema-compliant agent skill created only after the CLI contract is real.
- canonical JSON compiled from bounded YAML or directory/multi-file authoring;
- target-authorized live handoff, secret-binding/provider readiness, and
  direct-transfer orchestration without portable secret values; and
- separately reviewed application-data and sovereign payment-operation
  migration with receipts, restartability, and rollback boundaries.

The architectural center is a package-free, deterministic contract and
workflow core. The successor-A terminal wizard is repository-native and BCL
only. A's presentation target projects are disabled non-shipped contract shells;
successor B selects the actual GUI framework and implements UI, filesystem, and
browser adapters around the same core. Server runtime authority for authentication,
tenancy, policy ceilings, import sessions, transactional apply, legal
publication, and acceptance remains in ISLAMU Event.

### Explicit non-goals

- No combined manifest-plus-dotenv artifact.
- No embedded AI, model SDK, prompt runtime, inference, or agent loop.
- No raw secret value in a portable artifact, process argument, machine JSON,
  support report, migration receipt, or application-data payload.
- No PWA/service worker, auto-update, downloaded executable plugin, or mobile
  target in this revision.
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
  - future successor-B Event.SetupAssistant GUI workflows
  - new Event.SetupAssistant.Cli commands and BCL interactive terminal wizard
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
| ConfigurationManifest is an active upstream dependency | `configuration-manifest-context.md`; active continuation dated 2026-08-31 | High | Setup consumes only the frozen v1alpha2/schema/registry/no-secret wire baseline and does not inherit server implementation details. |
| v1alpha2 wire contracts are Application-owned | `src/Explore.Application/Features/ConfigurationManifest/Contracts/ConfigurationManifestV1Alpha2.cs` | High | This currently forces the schema tool to reference all Application. |
| A closed portability registry exists | `ConfigurationPortabilityRegistry.cs` | High | It has 21 entries and explicit excluded authority classes. |
| Import preview and HTTP foundations exist | `src/Explore.Application/Features/ConfigurationManifest/Importing/**`; instance/tenant import controllers and generated contracts | High | Strict parser, target-bound session, coverage/diff composer, and upload/preview/refresh/cancel transport exist; atomic apply remains server-side and outside this offline Setup workstream. |
| Constrained legal Markdown exists | `src/Explore.Domain/LegalMarkdownContract.cs` | High | Pure parser/renderer; rejects HTML, resources, unsafe links, and malformed structure. |
| Secret binding has a registry | `src/Explore.Domain/Secrets/SecretDefinitionRegistry.cs` | High | It is not a complete deployment-variable catalogue. |
| Environment inputs are large and manual | `.env.example` (618 lines), `docker-compose.yml` (985 lines) | High | No machine-generated relevance/default/activation model exists. |
| A package-free shared contract project exists | `src/Event.Wire.Contracts/Event.Wire.Contracts.csproj` | High | Application already references it; it is the repository-native extraction seam. |
| Deterministic dependency policy exists | `Directory.Build.props`, `Directory.Packages.props`, 49 lock files | High | Candidate Setup packages are not yet pinned or approved. |
| Release scope lacks Setup Assistant | `eng/release/policy/scope-registry.yaml` | High | Final release integration must add public `setup`. |
| Avalonia candidate browser output is static client-side WASM | Official Avalonia WebAssembly deployment documentation | High | Static output does not eliminate origin trust; the runtime graph is blocked and not selected for A. |
| Avalonia candidate browser accessibility is partial | Official Avalonia accessibility documentation | High | Browser accessibility claims must be target-evidenced; the candidate is not approved. |
| SA-120 dependency outcome is decision-complete | [Dependency evidence](setup-assistant-security-and-portability-dependency-evidence.md) | High | Terminal.Gui 2.4.17 and both Avalonia runtime graphs are blocked; A selects a BCL-only product graph. |
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
- No GUI/TUI framework package, desktop packaging, browser static release,
  Setup CLI, terminal wizard, or Setup agent skill exists.

### 2.3 Existing Tests And Verification Coverage

- `Event.Wire.Contracts.UnitTests` protects the existing admission codec.
- Application and Architecture ConfigurationManifest tests protect strict
  v1alpha2 shape, schemas, deterministic generation, registry coverage, and
  record semantics.
- Domain tests protect legal Markdown and legal lifecycle behavior.
- Persistence and API tests protect server import/session/runtime behavior.
- Existing tests do not protect a headless setup core, environment activation,
  dotenv dialect, secret state lifecycle, browser network/storage boundaries,
  desktop safe writes, CLI machine contracts, terminal leakage, target-specific
  GUI accessibility, or Setup release artifacts.

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
4. ConfigurationManifest remains active with a frozen Setup-facing wire
   baseline. Setup extraction must preserve those bytes and must not treat
   upstream server capabilities as Setup implementation evidence.
5. Browser-local execution can be overclaimed as origin-independent security.
6. Desktop/browser/terminal secret surfaces have distinct leakage paths and
   cannot share one generic “save file” adapter.
7. The repository has no Setup-specific dependency, project-reference,
   telemetry, AI, or release architecture ratchets.
8. Browser accessibility support may be weaker than desktop support; parity
   cannot be claimed from shared presentation code alone.

### 2.6 Unknowns After Investigation

These details are genuinely deferrable because they do not change the phase
structure or trust-boundary architecture:

| Unknown | Search/evidence | Resolution owner |
|---|---|---|
| Successor-B GUI framework and exact target graph | Avalonia 12.1.1 runtime targets are blocked by unresolved native component mapping and unproved publish exclusion; A selects no GUI package | Successor B selects a provenance-complete graph or obtains new authoritative evidence plus fresh approval |
| Final OS/version support floor | Official Avalonia tiers are known; release-runner and packaging evidence is absent | SA-1210 publishes only evidenced combinations |
| Which legal templates receive counsel approval | No approved template pack exists | SA-530 may ship blank/project-authored approved templates only |
| Which locally generated secret classes are provider-valid | Secret definitions exist; provider acceptance evidence is incomplete | SA-330 admits only independently documented generators |
| Whether AppImage or Flatpak clears target review | Official sandbox behavior is known; tool/license/release graph is not | SA-1210 keeps each format disabled until approved |

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

### Requirement 3.13: Composed Authoring Has One Canonical Output

The assistant **SHALL** accept bounded YAML and directory/multi-file authoring
inputs only as source composition formats and **MUST** compile them into the
single canonical v1alpha2 JSON artifact before preview, transfer, or apply.

#### Scenario 3.13A: Deterministic composition

- **GIVEN** equivalent single-file JSON, YAML, and directory sources
- **WHEN** each source is normalized and compiled
- **THEN** all produce the same canonical bytes, digest, section coverage, and
  diagnostics without embedding source paths or ordering accidents.

#### Scenario 3.13B: Composition ambiguity or oversized input

- **GIVEN** duplicate keys, conflicting fragments, links, traversal, cycles,
  unknown files, excessive depth/count/bytes, or an unmeasured larger profile
- **WHEN** composition begins
- **THEN** it fails closed before producing a partial artifact; larger limits
  remain disabled until measured resource evidence approves a named profile.

### Requirement 3.14: Live Target And Secret-Binding Authority

Live operations **MUST** use explicit target enrollment, short-lived scoped
authorization, server-provided HAL capabilities, and target-local secret
binding/provider identifiers. The assistant **MUST NOT** treat portable source
identity or a secret reference as authority and **MUST NOT** retrieve raw
provider secret values.

#### Scenario 3.14A: Authorized live handoff

- **GIVEN** a verified target, authenticated operator, fresh capability, and
  preview-ready canonical artifact
- **WHEN** the operator requests import, direct transfer, binding completion,
  or provider readiness
- **THEN** the server reauthorizes the target and tenant, advertises the exact
  allowed action, returns value-free state, and records a resumable receipt.

#### Scenario 3.14B: Stale, replayed, or cross-target authority

- **GIVEN** an expired token, stale HAL capability, mismatched tenant, replayed
  transfer, source authority claim, or unavailable provider
- **WHEN** a live action is attempted
- **THEN** the operation fails closed with RFC 7807 details, changes no target
  state, exposes no provider coordinates/value, and preserves safe retry.

### Requirement 3.15: Application-Data And Sovereign Migration

Application-data and payment-operation migration **MUST** be explicit,
category-selectable, tenant-isolated, resumable, idempotent, and independently
authorized from configuration portability. Money state **MUST NOT** be
reconstructed from configuration or silently replayed.

#### Scenario 3.15A: Resumable application-data migration

- **GIVEN** approved events, users, registrations, orders, tickets, uploaded
  files, or other selected application-data categories
- **WHEN** migration is interrupted and resumed
- **THEN** stable source identities, target mappings, checkpoints, integrity
  digests, and receipts prevent duplicates and cross-tenant writes.

#### Scenario 3.15B: Payment and refund authority conflict

- **GIVEN** sale-control, review, handoff, reconciliation, refund, or payment
  state whose provider/ledger authority is incomplete, stale, or conflicting
- **WHEN** migration or operational handoff is requested
- **THEN** the sovereign operation pauses without money mutation, requires
  target/provider reconciliation and explicit approval, and records zero-PII,
  value-safe evidence for recovery.

#### Scenario 3.15C: Worst Break Red — Replayed cross-tenant sovereign race

- **Owner:** Successor F, slice F1, before SA-1140 production code.
- **GIVEN** a stale or replayed capability and tenant mismatch racing payment
  finalization/refund through the public server seam
- **WHEN** deterministic coordination releases both operations against the real
  owning database and provider contract under a bounded timeout
- **THEN** the Red test MUST assert zero cross-tenant rows, zero provider/outbox
  money intent, unchanged checked ledger balances, exactly one durable
  value-free conflict receipt, and zero PII or secret logs.
- **Test constraint:** Subscribe to exact coordination/state signals before the
  race. Fixed sleeps, polling luck, internal mocks, and fake ownership
  boundaries are forbidden.

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
6. Manifests/packages contain no secret value, PII, provider credential,
   operational state, or deployment topology. Opaque secret-binding/provider
   identifiers live only in a separate target-local deployment plan and never
   grant authority.
7. `.env` and portable configuration remain separate artifacts with different
   sensitivity labels and no combined archive by default.
8. Secret values originate only from explicit local human entry, approved
   local cryptographic generation, or target-authorized provider writes. The
   assistant never reads raw values back from Infisical or a live instance.
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

### 5.4 Presentation architecture and successor boundary

- **Decision:** Successor A creates `Event.SetupAssistant`, Browser, and Desktop
  only as package-free disabled contract shells when required by the SA-110
  project graph. They are non-shipped and contain no functional UI. Successor B
  selects a provenance-complete GUI framework and implements shared views,
  resources, view models, accessibility/localization, and thin browser/desktop
  composition roots over stable Core contracts.
- **Why:** Terminal.Gui 2.4.17 is blocked by incomplete mandatory grammar
  provenance/notices, while Avalonia 12.1.1 runtime targets are blocked by
  unresolved native component mapping and unproved publish exclusion. Empty
  package-free boundaries preserve the intended architecture without silently
  approving or shipping either graph.
- **Alternatives:** Pinning a blocked graph, adding a package exception,
  treating compile-only conditional acceptability as runtime approval, or
  duplicating browser/desktop business behavior are rejected.
- **Consequences:** SA-510 and later remain GUI-framework-neutral until
  successor B receives fresh I-VSD, CTO, user, dependency, security, and
  accessibility approval. B may use Avalonia only after new authoritative
  evidence closes the exact runtime graph, or may choose another
  provenance-complete framework. No target/package is inherited from A.

### 5.5 Browser secret capability gate

- **Decision:** Build secret-mode behavior behind an immutable generated
  release capability manifest whose public default is disabled. Enabling
  requires exact-bundle CSP/request/storage evidence and independent security
  and legal approval.
- **Why:** A static client-side browser bundle and `connect-src 'none'` do not
  remove origin control, extension, browser, or device risk.
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

### 5.7 CLI and terminal wizard boundary

- **Decision:** Handwritten deterministic command parsing follows existing
  repository CLIs. The human workflow is a bounded repository-native BCL
  interactive terminal wizard with no new TUI package. Machine mode remains a
  separate non-secret surface using stable JSON schemas.
- **Why:** Terminal.Gui 2.4.17 is blocked by its mandatory 24-package graph, and
  no command or TUI framework is needed to preserve the observable workflow.
  Product-owned code and adversarial tests must own TTY detection, redirection,
  echo suppression/restoration, signals, resize, scrollback, and accessibility
  limitations.
- **Alternatives:** Terminal.Gui pinning, another external TUI dependency,
  agent-driven terminal UI, secret options, captured stdin, or localized prose
  parsing are rejected.
- **Consequences:** `event-setup` owns stable command/exit categories; secret
  completion is available only through the interactive TTY wizard. Its bounded
  linear prompts are not an automation contract and never share the machine
  CLI's process surfaces.

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

### 5.10 Source composition and scale profiles

- **Decision:** YAML and directory inputs are authoring adapters that compile
  through one normalized composition model into canonical v1alpha2 JSON.
- **Why:** Multiple wire formats would multiply validation, signature, and
  compatibility authority. One output keeps server/import contracts frozen.
- **Alternatives:** YAML as a second wire contract, implicit file merges, and
  unbounded directory discovery are rejected.
- **Consequences:** source paths never enter canonical bytes; conflicts fail
  closed; larger count/size profiles require measured memory/time evidence and
  explicit generated limits.

### 5.11 Live target and secret-provider adapters

- **Decision:** Add a networked outer adapter around the pure Core using
  short-lived target enrollment, generated wire contracts, HAL affordances,
  and server-authoritative import/transfer/provider operations.
- **Why:** Setup may guide and orchestrate live work but cannot own tenant,
  target, credential, provider, or transaction authority.
- **Alternatives:** long-lived bearer storage, direct database access, direct
  provider SDK use from Core/UI, and trusting source instance identity are
  rejected.
- **Consequences:** encrypted saved profiles are optional and contain only
  target identity plus revocable credential handles; secret values are write-
  only and provider coordinates remain server-side.

### 5.12 Application-data and sovereign migration engine

- **Decision:** Treat application data and payment operations as separate
  server-owned migration plans with category checkpoints, idempotency keys,
  mapping ledgers, receipts, and an outbox-backed commit boundary.
- **Why:** Configuration portability cannot safely represent aggregates,
  files, money, provider state, or reconciliation authority.
- **Alternatives:** database copying, configuration-embedded payloads,
  best-effort batch replay, and silent payment reconstruction are rejected.
- **Consequences:** Tier 0 payment actions receive their own approval and
  reconciliation state machine; failure pauses or compensates without
  deleting source state.

## 6. Implementation Phases

### Phase 1: Contract Freeze, Dependency Gate, And Project Boundaries

- **Goal:** Establish a green, license-approved, executable architecture before
  feature code.
- **Depends on:** ConfigurationManifest's frozen v1alpha2 wire contract,
  schema/registry/import-preview outputs and no-secret boundary as the
  extraction baseline, current plan-aligned I-VSD, and revision-bound CTO plus
  exact-revision user approval for successor A.
- **Relevant files:**
  - Existing: `Explore.slnx`, `Directory.Packages.props`,
    `Directory.Build.props`, `.github/workflows/test.yml`,
    `.ci/scripts/validate-dependency-license-policy.cs`,
    `tests/Event.Architecture.Tests/**`.
  - New: five package-free Setup source shells, five focused test shells, one
    lock file per project, the two generated SA-110 fail-closed ratchets, Setup
    architecture tests, and dependency evidence under this workstream. The
    presentation/Browser/Desktop shells are disabled and non-shipped.
- **Related skills/rules:** criticality-guardrail, ip-clean-room,
  agentic-research, clean-architecture-rules, tests rule.
- **Acceptance criteria:**
  - The [dependency decision](setup-assistant-security-and-portability-dependency-evidence.md)
    is enforced: Terminal.Gui 2.4.17 and its complete 24-package graph are not
    pinned/restored; no Avalonia package is pinned/restored in A; no package
    exception or replacement TUI/GUI dependency exists.
  - The selected A product graph is BCL plus package-free
    `Event.Wire.Contracts`; Setup project references form an acyclic inward
    graph and cannot reference server, network, persistence, provider,
    telemetry, AI, commercial tooling, or blocked UI packages.
  - All five source and five test projects are in the solution with committed
    lock files; package-free disabled presentation/Browser/Desktop shells are
    not shipped, functional UI, or support evidence.
  - Browser secret capability and presentation target activation default
    disabled; both generated SA-110 ratchets are present.
  - Clean-room source register, dependency decision, vulnerability/signature
    record, and SSO separation are reviewer-ready.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Remove only the new unshipped shell slice if
  a boundary fails review. Do not weaken scanners, add a blocked pin, activate
  a target, or keep an unapproved optional package.

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

### Phase 4: Versioned CLI And BCL Interactive Terminal Wizard

- **Goal:** Ship deterministic machine commands and a functionally equivalent
  repository-native human terminal workflow without a TUI package.
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
  - Interactive wizard secret entry requires a real TTY, is masked/non-echoing,
    disables redirected/output artifact surfaces, restores echo on every exit,
    and clears state on cancellation, completion, signal, suspension, resize
    failure, or navigation.
  - Product-owned adversarial tests cover TTY/redirection, echo restoration,
    signals, resize, scrollback leakage, keyboard completion, non-color output,
    bounded text, and truthful terminal accessibility limitations.
  - Core parity tests prove equivalent artifact bytes and diagnostics.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.SetupAssistant.Cli.Tests/Event.SetupAssistant.Cli.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** An unstable command remains unshipped rather
  than receiving aliases. Secret terminal-wizard mode remains disabled if TTY,
  echo, signal, resize, scrollback, or accessibility invariants cannot be
  proven.

### Phase 5: Shared GUI Workspaces, Accessibility, And Localization

- **Goal:** In independently approved successor B, select the GUI framework and
  implement shared GUI outcomes over the headless workflows, including
  manifest, environment, legal, review, and readiness experiences.
- **Depends on:** Phases 3 and 4 command contract.
- **Relevant files:**
  - Existing from A: package-free disabled `src/Event.SetupAssistant/**` and
    `tests/Event.SetupAssistant.Tests/**` contract shells.
  - New in B: selected framework integration, shared presentation resources,
    views, accessibility/localization adapters, and target evidence.
  - Existing: `docs/ACCESSIBILITY.md`, `docs/LOCALIZATION.md`.
- **Related skills/rules:** accessibility, criticality-guardrail,
  ip-clean-room.
- **Acceptance criteria:**
  - A provenance-complete exact GUI graph and target set receive fresh I-VSD,
    CTO, user, dependency, security, and accessibility approval before a shell
    becomes functional or shipped; no Avalonia package/target is presumed.
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
    permitting only the pinned selected-runtime requirements.
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

### Phase 8: YAML, Directory Composition, And Measured Scale

- **Goal:** Add ergonomic source composition without creating a second wire
  contract or weakening canonical limits.
- **Depends on:** Phases 2–5.
- **Relevant files:** new Setup Core composition adapters and tests, CLI/TUI/UI
  source pickers, generated composition schema/coverage, and existing
  v1alpha2 codecs/schemas.
- **Related skills/rules:** criticality-guardrail, clean-architecture-rules,
  accessibility, tests rule.
- **Acceptance criteria:**
  - JSON, YAML, and directory/multi-file inputs converge to identical canonical
    v1alpha2 JSON, digests, coverage, and diagnostics.
  - Duplicate/conflicting fragments, links, traversal, cycles, unknown files,
    and partial output fail closed.
  - Canonical limits remain the default; any larger named profile is generated
    only from measured memory/time evidence and stays compatible with target
    server limits.
  - Secret references, provider identifiers, and application data cannot be
    smuggled through composition metadata.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Disable the source adapter or larger profile
  without changing canonical JSON parsing or existing artifacts.

### Phase 9: Live Target Enrollment And Secret-Provider Binding

- **Goal:** Connect Setup to a live target through explicit short-lived
  authority while keeping raw secrets and provider coordinates out of portable
  artifacts and machine surfaces.
- **Depends on:** Stable A/B/C contracts; fresh Tier 1 intake, current I-VSD,
  fresh CTO and exact-revision user approval for successor D; and its named
  tenant/authorization/replay/provider evidence.
- **Relevant files:** new networked Setup adapter and tests; generated live
  wire contracts; existing authentication, API/HAL/BFF, secret-provider,
  import-session, and operator documentation surfaces.
- **Related skills/rules:** auth-patterns, blazor-bff-patterns,
  criticality-guardrail, ip-clean-room, secret isolation.
- **Acceptance criteria:**
  - Target enrollment uses a bounded device/interactive authorization flow,
    explicit tenant selection, short-lived scopes, revocation, and no token in
    logs, arguments, machine JSON, or portable artifacts.
  - Optional saved profiles are platform-protected and contain only target
    identity plus revocable credential handles.
  - Secret bindings/provider identifiers are target-local, allowlisted,
    value-free, and never treated as authority.
  - Infisical/provider operations are server-authorized write/readiness flows;
    Setup never reads raw secret values or exposes provider coordinates.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Revoke enrollment and delete the local
  protected handle. Offline/no-secret authoring remains fully usable.

### Phase 10: Live Apply And Direct-Transfer Orchestration

- **Goal:** Adapt the frozen configuration import, managed-apply, rollback, and
  mutually approved direct-transfer protocols into Setup workflows.
- **Depends on:** Phase 9 and green upstream ConfigurationManifest Tier 1,
  tenant-isolation, replay, and atomicity gates. Missing evidence disables live
  apply/direct transfer; the gate cannot be bypassed.
- **Relevant files:** new Setup live-operation adapters/workspaces/tests;
  existing generated API client, HAL relations, import-session/direct-transfer
  contracts, receipts, and operations documentation.
- **Related skills/rules:** auth-patterns, blazor-bff-patterns,
  criticality-guardrail, accessibility, error-tracking.
- **Acceptance criteria:**
  - Setup follows server HAL affordances for preview, apply, managed approval,
    transfer, history, cancellation, and forward rollback.
  - Capabilities remain header-only, expiring, target-qualified, replay-fenced,
    and absent from saved profiles/support evidence.
  - Interrupted transfer resumes from verified chunks; promotion is atomic and
    never deletes source state.
  - Setup displays committed configuration separately from pending effects and
    never claims local completion before the server receipt does.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Cancel or expire the target session and use
  server forward rollback where advertised; never synthesize local rollback.

### Phase 11: Application-Data And Sovereign Operations Migration

- **Goal:** Add independently authorized migration for application aggregates,
  files, and payment operational handoff without conflating it with
  configuration portability.
- **Depends on:** D contracts. Successor E requires fresh Tier 2 custody/erasure
  and Tier 1 tenant intake plus current I-VSD, fresh CTO/user approval, and
  named evidence. Successor F independently requires D/E contracts, Tier 0
  Grill-Me, current I-VSD, fresh CTO/user approval, explicit payment/provider/
  legal/operator decisions, and the F1 Worst Break Red before SA-1140.
- **Relevant files:** new server-side migration Domain/Application/Persistence/
  API contracts and tests; Setup migration adapters/workspaces; outbox,
  protected staging, mapping/checkpoint/receipt, payment, privacy, and operator
  documentation surfaces.
- **Related skills/rules:** criticality-guardrail, grill-me, auth-patterns,
  cqrs-mediatr-guidelines, dotnet-efcore-guidelines, outbox-pattern,
  error-tracking, accessibility.
- **Acceptance criteria:**
  - Events, users, registrations, orders, tickets, uploaded files, and other
    application-data categories are explicit, tenant-isolated, resumable,
    idempotent, integrity-checked, and source-retaining.
  - Identity/reference mappings and checkpoints are durable; interruption or
    replay cannot duplicate aggregates or cross tenant boundaries.
  - Sale-control, review, handoff, reconciliation, refund, and payment
    operations use a separate sovereign state machine with explicit provider
    reconciliation and approval before any money mutation.
  - Before SA-1140 production code, successor F owns the Scenario 3.15C Worst
    Break Red against the real owning database/provider contract with
    deterministic coordination and a bounded timeout; all exact zero-mutation,
    ledger, receipt, and log assertions MUST fail for the intended reason.
  - Receipts and telemetry are zero-PII/value-safe; secrets and provider
    credentials never enter migration payloads.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Pause at the last durable checkpoint,
  compensate only through domain-approved operations, retain source state, and
  require reconciliation before retrying sovereign actions.

### Phase 12: Packaging, Provenance, Documentation, And Agent Skill

- **Goal:** Produce governed multi-target release outputs, operator/security
  documentation, and the post-CLI skill for the exact implemented capability
  set.
- **Depends on:** Each selected owning successor and its phase gate being
  green. G runs per independently shippable subset and again for final program
  reconciliation; unevidenced later successors do not block an offline release.
- **Relevant files:** new `eng/setup-assistant/**`, Setup CI/release workflow
  paths, package manifests, SBOM/notices/checksum/provenance evidence, Setup
  docs, `.agents/skills/setup-assistant-cli/**`, and
  `docs/releases/changes/CHG-*.yaml`; existing release and governance surfaces.
- **Related skills/rules:** ci-cd intent, ip-clean-room, skill-authoring,
  conventional-commit, criticality-guardrail.
- **Acceptance criteria:**
  - Required archives/packages cover only evidenced targets and implemented
    capabilities; experimental formats remain absent until independently gated.
  - Release identity converges across commit, version, RID/format, locks, SBOM,
    build manifest, checksums, signatures/notarization, source, and
    reproducibility.
  - Docs cover offline, live, composition, secret-binding, transfer,
    application-data, sovereign recovery, accessibility, and support boundaries.
  - Agent skill routes only implemented compatible CLI commands, defaults to
    no-secret dry-run machine mode, rejects secret inputs/TUI automation, and
    requires human approval for every live or mutating operation.
  - I-VSD, CTO/MAD reviews, threat models, dependency evidence, and the Tier 2
    change fragment match the shipped subset.
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
- `Event.Setup.Core.Tests`: YAML/directory composition convergence, conflict
  rejection, canonical limits, and measured scale profiles.
- `Event.API.IntegrationTests`: live enrollment, scoped capability, tenant
  isolation, secret-provider readiness, and RFC 7807 fail-closed behavior.
- `Event.Persistence.IntegrationTests`: application-data checkpoints,
  idempotency, mapping integrity, outbox atomicity, and sovereign-operation
  concurrency on real providers.
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
  authority-broadening writes without approval;
- **Worst Break Red (successor F):** stale/replayed capability plus tenant
  mismatch racing finalization/refund through the public seam, deterministically
  coordinated under a bounded timeout against the real owning database and
  provider contract, asserting zero cross-tenant rows, zero provider/outbox
  money intent, unchanged checked ledger balances, one durable value-free
  conflict receipt, and zero PII/secret logs.

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
- Update `docs/ACCESSIBILITY.md` and `docs/LOCALIZATION.md` with selected GUI,
  browser, desktop, and terminal target-specific contracts rather than
  Blazor-only assumptions.
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
`sha256:d2bbba40455c013e20883ab6202f84411bb05f2c20f6060a9e73095f44a8e4b1`;
status is `current`; disposition is `plan-aligned`. Findings
`IVSD-F001` through `IVSD-F046` and mitigations `IVSD-M001` through
`IVSD-M046` remain preserved without remapping or deletion. Replacing the
approved package/implementation strategy with a BCL terminal wizard and
framework-neutral successor-B boundary was revalidated without weakening any
provider-controlled behavior or later gate.

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
| `IVSD-F013` / `IVSD-M013` | 3.11; SA-110, SA-120, SA-1210, SA-1220 | Target-specific dependency approval |
| `IVSD-F014` / `IVSD-M014` | 3.11; SA-1210, SA-1220 | Signed, identified, verifiable releases |
| `IVSD-F015` / `IVSD-M015` | 3.10, 3.11; SA-510, SA-1210, SA-1220 | Evidence-backed support matrix |
| `IVSD-F016` / `IVSD-M016` | 3.11; SA-120, SA-130, SA-640, SA-1210 | Track browser source; ignore generated output |
| `IVSD-F017` / `IVSD-M017` | 3.5, 3.11; SA-630, SA-640, SA-1220 | Official identity and fork disclosure |
| `IVSD-F018` / `IVSD-M018` | 3.10; SA-510, SA-520, SA-530 | Accessible GUI behavior |
| `IVSD-F019` / `IVSD-M019` | 3.10; SA-510, SA-520 | Bundled localization and RTL |
| `IVSD-F020` / `IVSD-M020` | 3.2, 3.3, 3.4; SA-320, SA-330, SA-340 | No live secret authority |
| `IVSD-F021` / `IVSD-M021` | 3.5, 3.11; SA-630, SA-640, SA-1220 | Truthful security claims |
| `IVSD-F022` / `IVSD-M022` | 3.4–3.7, 3.11; SA-610, SA-710, SA-1210, SA-1220 | Adversarial evidence and external release gates |
| `IVSD-F023` / `IVSD-M023` | 3.8; SA-210, SA-340, SA-530, SA-540 | Typed role-scoped legal source |
| `IVSD-F024` / `IVSD-M024` | 3.8; SA-210, SA-340, SA-530, SA-540 | Never rewrite publication/acceptance evidence |
| `IVSD-F025` / `IVSD-M025` | 3.8; SA-540, SA-1220 | Approved local non-certifying templates only |
| `IVSD-F026` / `IVSD-M026` | 3.8; SA-210, SA-220, SA-540 | One constrained Markdown codec |
| `IVSD-F027` / `IVSD-M027` | 3.2, 3.8; SA-210, SA-340, SA-540 | Portable legal source and metadata |
| `IVSD-F028` / `IVSD-M028` | 3.2, 3.8; SA-210, SA-340, SA-540 | Bounded localized content and usable diff |
| `IVSD-F029` / `IVSD-M029` | 3.9, 3.10; SA-410, SA-420, SA-430 | First-class CLI/TUI |
| `IVSD-F030` / `IVSD-M030` | 3.9; SA-410, SA-420 | Versioned JSON, exits, help, dry-run, digests |
| `IVSD-F031` / `IVSD-M031` | 3.4, 3.9; SA-410, SA-420, SA-430 | Terminal secret boundary |
| `IVSD-F032` / `IVSD-M032` | 3.12; SA-1240 | Skill rejects secret access |
| `IVSD-F033` / `IVSD-M033` | 3.12; SA-110, SA-1240 | No embedded AI/provider dependency |
| `IVSD-F034` / `IVSD-M034` | 3.8, 3.12; SA-420, SA-530, SA-540, SA-1240 | Human approval gates |
| `IVSD-F035` / `IVSD-M035` | 3.10; SA-430, SA-520, SA-1220 | Truthful TUI/browser accessibility evidence |
| `IVSD-F036` / `IVSD-M036` | 3.12; SA-410, SA-420, SA-1240 | Skill only after verified CLI contract |
| `IVSD-F037` / `IVSD-M037` | Scenario 3.13; SA-810, SA-820, SA-830, SA-1220, SA-1250 | Canonical composition and evidence-bound scale |
| `IVSD-F038` / `IVSD-M038` | Scenarios 3.14 and 3.15; SA-910, SA-920, SA-1010, SA-1030, SA-1110, SA-1120, SA-1130, SA-1250 | Tenant authority across live and migrated records |
| `IVSD-F039` / `IVSD-M039` | Scenario 3.14; SA-910, SA-920, SA-1010, SA-1020, SA-1030, SA-1250 | Fresh scoped replay-fenced authorization |
| `IVSD-F040` / `IVSD-M040` | Scenario 3.14; SA-910, SA-930, SA-1220, SA-1250 | Write-only coordinate-free provider binding |
| `IVSD-F041` / `IVSD-M041` | Scenario 3.15A; Tier 2 custody/erasure gate before SA-1110; SA-1110, SA-1120, SA-1130, SA-1220, SA-1250 | Separate application-data custody contract |
| `IVSD-F042` / `IVSD-M042` | Scenario 3.15A; SA-1110, SA-1120, SA-1130, SA-1250 | Durable idempotent migration continuity |
| `IVSD-F043` / `IVSD-M043` | Scenario 3.15B; Tier 0 decision gate before SA-1110 and provider/ledger reconciliation gate before SA-1140; SA-1110, SA-1140, SA-1220, SA-1250 | Sovereign-money reconciliation before mutation |
| `IVSD-F044` / `IVSD-M044` | Scenarios 3.14A and 3.15A; SA-1030, SA-1120, SA-1130, SA-1220, SA-1250 | Source retention and operator autonomy |
| `IVSD-F045` / `IVSD-M045` | Scenarios 3.14A-B and 3.15A-B; SA-1010, SA-1020, SA-1030, SA-1110, SA-1120, SA-1130, SA-1140, SA-1220, SA-1250 | Truthful pending/unknown/recovery state |
| `IVSD-F046` / `IVSD-M046` | Scenarios 3.12, 3.14, and 3.15; SA-1020, SA-1030, SA-1110, SA-1130, SA-1140, SA-1240, SA-1250 | Category-level human agency |

These F037-F046 mappings reproduce the current I-VSD Planning Handoff without
changing them. Their successor gates remain blocking: current I-VSD is necessary
but no later successor may begin without its fresh tier-appropriate intake,
CTO review, exact-revision user approval, and named evidence.

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

- **Authentication/authorization:** Applicable to Phases 9–11. Enrollment,
  tokens, target/tenant authority, HAL affordances, and every live mutation
  fail closed server-side; offline authoring remains unauthenticated.
- **Tenant isolation:** Applicable to artifact scope. Tenant packages cannot
  contain instance authority; source names/IDs never become trusted target
  authority.
- **Secrets:** Critical. Values remain local, ephemeral where practical,
  non-observable, and confined to protected output.
- **Browser origin:** Critical. The origin controls delivered code; disclosure,
  reproducibility, source, digests, CSP, and review provide evidence but not
  technical impossibility.
- **Rate limiting:** Applicable to enrollment, provider readiness, transfer,
  live apply, and migration endpoints.
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
| Accessibility | Applicable | The BCL terminal wizard and successor-B selected GUI/browser/desktop targets require distinct evidence and honest alternatives. |
| Product autonomy | Applicable | No-secret/default offline paths reduce dependence on ISLAMU hosting and Infisical. |
| Payments | Applicable in Phase 11 | Sovereign sale-control, reconciliation, handoff, and refund operations remain server/provider-authoritative and independently approved. |
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
- Phases 9–11 may add server persistence for enrollment, protected credential
  handles, migration plans, checkpoints, mappings, receipts, and sovereign
  operation state. EF migrations are generated for every provider and are
  never hand-edited.
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
| Hidden package/native/license incompatibility | Medium | Critical | Full target graph, scanner, legal decision, target exclusion | Unknown/denied metadata or lock/SBOM mismatch | SA-120, SA-1210 |
| Hosted build exfiltrates secrets | Low after controls | Critical | Disabled-by-default capability, preload, deny policy, independent review | Request/storage evidence or origin mismatch | SA-610–SA-640 |
| Managed memory retains secrets | High | High | Minimize copies/lifetime; clear state; no erasure claim | Heap/DOM/state review findings | SA-330, SA-630, SA-730 |
| Unsafe desktop overwrite/link race | Medium | Critical | Handle-first platform adapters and real filesystem invariants | Permission/link/identity mismatch | SA-710–SA-730 |
| Environment catalogue drifts from runtime/Compose | Medium | High | One generator/check graph and closed convergence tests | Generated artifact diff or unknown key | SA-310, SA-320 |
| Cross-platform claim exceeds evidence | Medium | High | Release support manifest per exact OS/RID/format | Missing package/sign/accessibility evidence | SA-1210, SA-1220 |
| Browser accessibility is materially incomplete | High | High | Honest limitation, semantic controls, desktop/CLI alternative | Missing platform accessibility evidence | SA-520, SA-1220 |
| TUI leaks through terminal/process surfaces | Medium | Critical | TTY-only secret state; no args/env/stdin/stdout/history | Captured output/process contract failure | SA-410–SA-430 |
| Legal template or role claim misleads | Medium | Critical | Blank/approved templates only, provenance and legal gate | Missing approval or wrong-scope validation | SA-530, SA-1220 |
| Composition ambiguity changes canonical meaning | Medium | High | One normalized merge model, closed conflicts, canonical byte convergence | Cross-format digest or diagnostic mismatch | SA-810–SA-830 |
| Live credential or tenant authority is replayed | Medium | Critical | Short-lived scoped enrollment, HAL reauthorization, protected handles, revocation | Replay/cross-target invariant failure | SA-910–SA-1030 |
| Application migration duplicates or crosses tenants | Medium | Critical | Durable mappings/checkpoints/idempotency and real-provider races | Duplicate aggregate or tenant-isolation failure | SA-1110–SA-1130 |
| Payment handoff mutates money without reconciliation | Medium | Critical | Separate sovereign state machine, provider reconciliation, explicit approval | Ledger/provider/receipt mismatch | SA-1110, SA-1140 |
| Skill invents commands or handles secrets | Medium | High | Create after CLI; version gate; machine-only no-secret workflow | Schema/version mismatch or secret input | SA-1240 |
| Release docs expose overconfident claims | Medium | High | I-VSD/claim review and identified-release wording | Claim inventory/review failure | SA-1220, SA-1250 |

## 15. Success Metrics And Definition Of Done

- All five Setup source and five focused test projects exist with committed
  locks and clean architecture; A's product graph is BCL-only, and its three
  presentation target shells remain disabled, package-free, and non-shipped.
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
- YAML and directory inputs converge to the canonical JSON artifact, and
  larger profiles exist only with measured evidence.
- Live target enrollment, secret bindings, import/transfer, and saved profiles
  are scoped, revocable, tenant-safe, and value-free outside human/provider
  write boundaries.
- Application-data migration is resumable and idempotent; sovereign payment
  handoff cannot mutate money without reconciliation and explicit approval.
- Every phase’s single build/test gate is green, the Tier 2 change fragment is
  valid, I-VSD has been revalidated as current/plan-aligned, and CTO/user
  approvals bind the final revision.

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

The hardest risk is proving a coherent security and release promise across
fundamentally unequal targets. A shared future GUI layer does not make browser
accessibility equal to desktop, client-side execution does not remove origin
trust, and a successful local file write does not prove safe permissions on every
filesystem. The plan therefore treats support,
secret-mode enablement, and package formats as evidence-backed release
capabilities rather than aspirations. The second risk is dependency-boundary
confusion: ConfigurationManifest is an active server workstream while Setup is
still planning-only. Setup must preserve the frozen wire/schema/registry
behavior and must never market upstream server work as an existing Setup
capability.
