<!-- ABOUTME: Active working memory for the Setup Assistant security and portability workstream. -->
<!-- ABOUTME: Records review state, blockers, resume order, evidence, decisions, and validation handoffs. -->

# Setup Assistant Security And Portability — Context

Last Updated: 2026-09-01 Europe/Brussels

## Review State

- **Planning status:** Successor A foundation-offline through Phase 4,
  successor B through Phase 7, and successor C through Phase 8 are Green;
  30/47 tasks are complete. B1 owns a
  CommunityToolkit-only shared model, target-root DI, independently gated
  Avalonia/Terminal.Gui adapters, and `Active`/`ApprovedDisabled` outcomes.
  SA-510-SA-518 are Green: the exact Toolkit graph is active in the shared
  model, DI remains executable-root-only, 18 presentation tests and 14 Setup
  architecture tests pass, and the Release build is error-free. SA-520,
  SA-525, SA-530, and SA-540 resolved `ApprovedDisabled`; Phase 6 browser
  runtime/secret/release slices also resolved `ApprovedDisabled`. Phase 7
  activated package-free Unix protected transactions with 9/9 real-filesystem
  invariants while Windows ACL output and desktop UI remain fail-closed. No
  rendered target, accessibility parity, legal editor, support, release, or
  shipping claim was introduced. Phase 8 adds bounded JSON/restricted-YAML/
  Linux-directory composition plus four evidence-bound scale profiles while
  preserving canonical defaults and Wire identity. Phase 9 successor D has a
  technically accepted isolated 10/10 absence Red and D2-1 through D2-11
  staging. Corrected D2-1 package-free strict Wire contracts are approved
  Green. D2-2 Domain behavior is approved Green with 14/14 focused and
  1089/1089 full Domain tests. Corrected D2-3 Application contracts are
  approved Green at 7/7 with a fail-closed default outcome, RFC UUID variant
  checks, exact constructor/property mappings, and borrowed-memory identity.
  D2-4 Persistence is approved Green with 5/5 provider models, 6/6 real
  PostgreSQL invariants, and five generator-owned migration/snapshot sets.
  No outer owner, live enrollment, active capability, protected
  profile, provider write, support, or release authority exists yet.
- **I-VSD report:**
  [i-vsd-setup-assistant-security-and-portability.md](../../../islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md)
- **I-VSD reviewed input revision:**
  `sha256:cb2292e484bf137c5b6f4d963733fa282cc54100dc50ec06f226ca2b7261c3e9`
  for the initial D2-1 binding; corrected D2-1 revalidation is current.
- **I-VSD status / disposition:** current and plan-aligned for the isolated
  one-file Phase 9 absence Red; no product authority. `IVSD-F038`–`IVSD-F040`
  and matching mitigations bind tenant, replay/HAL, protected profile,
  provider-coordinate, and no-readback boundaries.
- **Phase 9 D2 status:** D1/D2-0b corrected Red builds cleanly and remains
  10/10 attributable failures. Exact event identity and staged ledgers are
  technically accepted. The user explicitly approved the reviewed staged D2
  sequence. Initial D2-1 review exposed default capability serialization and
  permissive generated JSON. Corrected behavioral Red then strict package-free
  Green passes 8/8 focused Wire, 35/35 full Wire, 11/11 Core architecture,
  1/1 isolated Architecture, and a warning-free product build. D2-2 Domain and
  corrected D2-3 Application contracts, D2-4 Persistence, and D2-5 API are
  approved Green. D2-5 closes with 16/16 owned scenarios, a deterministic real
  PostgreSQL issuance race, and a 100/100 weighted Tier 1 vote. D2-6 closes
  with a 3/3 cold mismatch matrix, a warm-cache authority breaker, 16/16 full
  binding scenarios, and a 100/100 weighted Tier 1 vote. D2-7 through D2-11
  remain pending in order: provider writer,
  generated OpenAPI/client, protected profile disposition, adapter TDD, and
  closure. Capability flags remain false.
- **First CTO review:** [Split before approval](setup-assistant-security-and-portability-cto-review.md), bound to the prior exact plan/tasks revisions.
- **Current correction review:**
  [Corrected D2-1 strict Wire Green approved](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-phase9-d2-1-corrected-cto-review.md),
  `sha256:cd9b41d98111914e4aba5a392ebe0fe9d981c52f4d2d376a75eae74ca68ca72c`.
- **Current user approval:**
  [D2-1 through D2-11 staged product sequence](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-phase9-d2-product-approval.md),
  `sha256:a5d01cb1d91a071c7885316edb3ec27f244d8f36e44687ebe6d4344dbeb6b97e`.
- **Current D2-1 evidence:**
  [corrected strict JSON/capability Red/Green](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-phase9-d2-1-corrected-evidence.md).
- **Current D2-2 evidence:**
  [Domain Green](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-phase9-d2-2-green-evidence.md).
- **Current D2-2 review:**
  [Domain Green approved](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-phase9-d2-2-green-cto-review.md),
  `sha256:bf46d5c7fe2e3a8c56d8e0ffb0dc72375e38b0bb2b4eb38d1508adcf97479db2`.
- **Current D2-3 evidence:**
  [corrected Application Red/Green evidence](../../../.omo/evidence/20260901-setup-assistant-security-and-portability/summary.md).
- **Current D2-3 review:**
  [weighted corrected Green approval](../../../.omo/evidence/20260901-setup-assistant-security-and-portability/phase9-d2-3-mad-review.yaml).
- **Current D2-4 evidence:**
  [Persistence Red/Green summary](../../../.omo/evidence/20260901-setup-assistant-security-and-portability/summary.md).
- **Current D2-4 review:**
  [weighted Persistence Green approval](../../../.omo/evidence/20260901-setup-assistant-security-and-portability/phase9-d2-4-green-mad-review.yaml).
- **Current D2-5 evidence:**
  [API Green summary](../../../.omo/evidence/20260901-setup-assistant-security-and-portability/summary.md).
- **Current D2-5 review:**
  [weighted API Green approval](../../../.omo/evidence/20260901-setup-assistant-security-and-portability/phase9-d2-5-green-mad-review.yaml).
- **Current D2-6 evidence:**
  [SecretResolver Green summary](../../../.omo/evidence/20260901-setup-assistant-security-and-portability/summary.md).
- **Current D2-6 review:**
  [weighted SecretResolver Green approval](../../../.omo/evidence/20260901-setup-assistant-security-and-portability/phase9-d2-6-green-mad-review.yaml).
- **Current verification exception:** full architecture has 524 pass, 1
  unrelated SetupAssistant lock-ratchet failure, and 1 pre-existing skip. The
  D2-2-relevant Domain/naming architecture slices pass 17/17.
- **Dependency evidence:**
  [setup-assistant-security-and-portability-dependency-evidence.md](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-dependency-evidence.md)
- **B1 review binding:**
  [setup-assistant-security-and-portability-b1-review-bindings.md](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-b1-review-bindings.md)

## Umbrella Program State

This directory is the umbrella for seven independently reviewable successor
boundaries. The historical `setup-assistant-presentation-targets` B0
Razor/static-browser candidate and its I-VSD/CTO/binding artifacts are
superseded and non-executable. Foundation A
`setup-assistant-foundation-offline` is approved and Green; SA-110 through
SA-730 and Phase 1 through Phase 7 are Green. B owns the active shared
CommunityToolkit human-presentation state plus independently gated Avalonia and
Terminal.Gui adapters/runtime targets. The B0 SA-510 contract is historical;
SA-518 and Unix protected output are Green, while Avalonia, Terminal.Gui,
browser/desktop UI, rendered accessibility, Windows ACL output, and the legal
editor remain `ApprovedDisabled` or fail-closed. C
`setup-assistant-composition-scale` is Green with bounded canonical
composition and measured scale evidence. D
`setup-assistant-live-control-plane` has a technically accepted isolated D2-0b
Red/staging packet plus corrected D2-1 package-free strict Wire approved Green.
D2-2 Domain behavior, corrected D2-3 Application contracts, and D2-4
Persistence, D2-5 API, and D2-6 SecretResolver authority mismatch are approved
Green; D2-7 provider write/barrier behavior is current and no live-control
capability is active. E
`setup-application-data-migration`, F `setup-sovereign-payment-migration`, and
G `setup-release-and-agent-contract` are inactive.

PR slices are A1 SA-110-SA-130 with ten package-free project shells, A2
SA-210-SA-230, A3 SA-310-SA-340, A4 SA-410-SA-430 BCL CLI/terminal wizard;
B1 SA-510 graph decisions, SA-515 Red, and SA-518 shared model; B2 SA-520
shared Avalonia views, B3 SA-525 Terminal.Gui, B4 SA-530 accessibility/
localization, B5 SA-540 legal, B6 SA-610-SA-640, B7 SA-710-SA-730; C1
SA-810/SA-820, C2 SA-830; D1 SA-910 Red/server contracts, D2 SA-920/SA-930,
D3 SA-1010 Red plus SA-1020/SA-1030; E1 SA-1110 privacy/tenant Red, E2 SA-1120,
E3 SA-1130, E4 SA-1132 Setup UI activation; F1 SA-1135 Worst Break Red/Tier 0
record, F2 SA-1140 Domain/Persistence/provider reconciliation, F3 SA-1145
API/HAL/Setup activation;
G1 SA-1210/SA-1220 per subset, G2 SA-1240 after CLI schema, G3 SA-1250.

Dependencies are one-way A -> B and A -> C; D consumes selected stable B/C
contracts; E depends on D; F depends on D/E contracts and is independently
optional; G runs per subset and at final reconciliation. No later
successor inherits umbrella or predecessor approval. Every successor requires
the current I-VSD revision, fresh tier-appropriate intake, fresh CTO review,
exact-revision user approval, and named evidence before implementation.

## Resume From Here

1. Implement the staged D2-7 selected-authority writer, commitment,
   idempotency, cancellation, and revocation/dispatch barrier behavior.
2. Run the focused `SetupLiveAuthoritySecurityTests` selector and obtain
   exact-revision Tier 1 Green review before D2-8.
3. D2-7 continues to own executable pre-dispatch ordering, revocation race,
   selected writer/HMAC, idempotency, call-count, cancellation, and
   lease-disposal proof.
4. Do not start provider writer, OpenAPI/client generation,
   protected profile, or adapter work before its preceding stage is reviewed
   Green.
5. Keep every live-control capability flag false until D2-11 closure.

## Historical Session Progress (Superseded; Non-Executable)

Everything nested in this section records prior states only. Labels such as
complete, current, in progress, next, and handoff below are historical facts,
not executable resume directions. The sole current resume authority is
`Resume From Here` above.

### COMPLETED

- Classified the plan as a Tier 1 Security
  `external-infrastructure-bootstrap` workstream with supporting CI/CD,
  clean-room/license, and agent-skill intents.
- Loaded implementation-plan, I-VSD, Grill-Me, criticality, clean-architecture,
  dependency, accessibility, and skill-authoring governance.
- Verified there is no Setup Assistant source or test project.
- Verified the current ConfigurationManifest implementation provides strict
  v1alpha2 contracts, a closed 21-entry portability registry, legal Markdown,
  protected import sessions, semantic preview, scope-safe HTTP/HAL/BFF
  contracts, and deterministic generated clients.
- Recorded ConfigurationManifest as an active upstream dependency. Setup keeps
  the frozen v1alpha2 wire/schema/registry/no-secret extraction contract and
  does not inherit server implementation details.
- Verified the existing package-free `Event.Wire.Contracts` project is the
  repository-native shared contract seam and Application already references it.
- Verified there is no canonical full environment-variable/activation
  catalogue; `.env.example` and Compose remain separate large maintained
  surfaces.
- Refreshed official Avalonia, Terminal.Gui, CSP, platform accessibility,
  filesystem, signing/notarization, provenance, and Flatpak evidence without
  ingesting third-party source.
- Completed source-free SA-120 dependency research. Terminal.Gui `2.4.17` is
  blocked because mandatory TextMateSharp.Grammars `2.0.4` lacks complete
  component-level provenance/notices; no stable corrected or supported
  no-grammar profile exists, so none of its 24-package graph may be pinned or
  restored.
- Determined Avalonia `12.1.1` shared compile use is only conditionally
  acceptable for non-shipped scaffolding with telemetry opt-out and publish
  exclusions, while Desktop and Browser runtime targets remain blocked by
  unresolved native component mapping and unproved
  `Avalonia.Remote.Protocol` publish absence. No Avalonia package is selected
  for A.
- Selected a BCL-only successor-A product graph: package-free Wire Contracts,
  pure `Event.Setup.Core`, deterministic machine CLI, and a repository-native
  bounded terminal wizard. Presentation/Browser/Desktop boundaries are
  package-free disabled non-shipped shells; successor B owns the actual GUI
  graph and targets.
- Re-baselined the plan with 15 RFC 2119 requirements and 12 phases. New
  phases own YAML/directory composition and measured scale, live enrollment
  and secret-provider binding, live apply/direct transfer, and application-
  data/payment-operation migration.
- Re-baselined the hot ledger with 41 implementation tasks and separate phase
  verification checkboxes.
- Created the sanitized clean-room evidence packet and bound review state to
  its SHA-256 revision.
- Recorded the user's direction approving the complete objective; after the CTO
  split decision, successor-A approval was rebound after final hashes and fresh
  review.
- Revalidated the expanded composition, live-authority, application-data,
  privacy, and payment scope through planning-mode I-VSD. That prior-aligned
  report added `IVSD-F037`–`IVSD-F046` with matching mitigations and mapped
  them to Scenarios 3.13–3.15 and SA-810–SA-1250; all remain preserved while
  the report is stale for the new strategy.
- Recorded the first revision-bound CTO decision as `Split before approval` and
  recast this directory as the seven-successor umbrella with Foundation A then
  sole active.
- Obtained prior revision-bound CTO approval for successor A only and bound the
  user's continuing implementation instruction to those reviewed plan/tasks
  hashes; those approvals are now historical and successors B-G inherit none.
- Completed SA-110 Red in
  `tests/Event.Architecture.Tests/SetupAssistantArchitectureTests.cs`.
  The focused Release selector compiled and ran six tests: three prerequisite/
  verifier tests passed and three failed only for the absent ten Setup
  projects, browser capability JSON, and frozen-contract baseline JSON.
- Revalidated the BCL-only dependency strategy through I-VSD and obtained fresh
  revision-bound CTO and user approval for successor A only.
- Completed SA-120 Green: ten package-free project boundaries, ten committed
  locks, exact inward references, two generator-owned disabled ratchets, and
  zero blocked package references. Ten force-evaluated restores, ten locked
  restores, ten Release project builds, package inventory/vulnerability/
  deprecation checks, the 654-pair license policy, generator check, and the
  focused architecture selector all passed; the selector was Green 6/6.
- Established the pre-implementation Release build baseline. It currently
  fails in unrelated shared-worktree
  `tests/Event.Domain.UnitTests/ValueObjects/AtprotoDidTests.cs` because the
  concurrent strong-typing work removed or moved `AtprotoDid`; the build
  reported 10 `CS0103` errors and 1026 pre-existing analyzer warnings.
- Completed SA-130: Setup source/tests/locks/generator/ratchets route the
  always-present Architecture and Setup lanes; locked restore is followed by a
  non-mutating ratchet check; all five empty focused test projects build in
  Release without pretending zero tests are product coverage. Recursive license
  discovery, blocked-package absence, read-only/no-secret authority, and real
  Git tracked/ignored semantics are covered by focused architecture contracts.
- Phase 1 Release build passed with zero errors on 2026-08-31. It emitted
  10,911 pre-existing shared-worktree analyzer warnings; the ten new Setup
  projects had already built independently with zero warnings/errors.
- Phase 1 full `Event.Architecture.Tests` gate passed: 521 total, 520
  succeeded, zero failed, and one pre-existing explicitly governed skip in
  31.538 seconds.
- Completed SA-210 intentional Red in the package-free Wire test project. Four
  new C# files have clean LSP diagnostics and the project compiles in Release
  with zero warnings/errors. The exact focused selector ran once: 6 total, 5
  passed, 1 failed, 0 skipped. Its sole bounded failure is the missing
  `ISLAMU.Wire.Contracts.ConfigurationPortability` owner/API while v1alpha2 and
  legal Markdown remain in old Application/Domain owners. Literal golden bytes,
  digests, checked-schema limits, strict breakers, record/collection semantics,
  legal safety, synthetic verifier failures, and no-value-leak scans are pinned
  under `.omo/evidence/20260831-setup-assistant-security-and-portability/`.
  Production contracts/code, schemas, generators, project graph, solution,
  dependencies, and current Application/Domain tests were not moved or edited.

- Completed SA-230 with package-free immutable Core contracts for profiles,
  selections, diagnostics, readiness, SHA-256 digests, section diff/coverage,
  and workflow transitions. Seven focused architecture/behavior tests are
  Green; compiled IL/public-surface breakers detect ambient calls, mutable
  collections, and diagnostic value leakage. Locked builds and package audits
  are clean and no lock changed.
- Phase 2 Release build passed with zero errors in 44.72 seconds. It emitted
  6,703 pre-existing shared-worktree analyzer warnings; focused Wire and Setup
  Core product builds remain clean.
- Phase 2 full `Event.Wire.Contracts.UnitTests` gate passed: 27/27 succeeded,
  zero failed/skipped, in 575 ms.

- Completed SA-310 intentional Red in the package-free Core test project. Six new
  Environment test files have clean LSP diagnostics and the project compiles in
  Release with zero warnings/errors. The prescribed combined selector ran once:
  22 total, 21 passed, 1 failed, 0 skipped, 295 ms. Its sole bounded failure is
  the aggregate prerequisite for the absent final
  `ISLAMU.Event.Setup.Core.Environment` owners and absent generator-owned
  `eng/setup-assistant/generated/environment-catalogue.json`. Independent cycle,
  collision, unsafe-default, irrelevant-key, fake-secret, dotenv injection,
  bounds, readiness, and diagnostic-leak breakers passed. Production catalogue,
  dotenv, generator, and machine-catalogue behavior is not implemented.
- Completed SA-320 at 8/41 tasks. Package-free Core now owns 346 immutable
  environment definitions and a closed activation graph. The compiled generator
  compares 52 Domain secret environment authorities, 310 preserved template
  keys, and 290 nested Compose interpolation keys, then emits deterministic
  value-free machine JSON and bounded documentation. The one noncanonical mixed-
  case template alias was removed; Compose topology and existing template values
  otherwise remain machine-owned and unchanged. The focused catalogue selector
  is Green at 11/11 after semantic audit repair; SA-330 dotenv owners remain
  absent and isolated.
- Completed SA-330 at 9/41 tasks. Core now owns the strict bounded UTF-8
  dotenv codec, catalogue-driven no-secret/secret composition, definition-
  authoritative readiness, and the sole approved repository-owned
  `SETUP_SECRET` opaque 256-bit generator. The acceptance audit made
  duplicate/case-colliding/null inputs fail closed, prevents unclassified
  values satisfying protected readiness, and converts invalid UTF-16/null
  document entries into empty-byte diagnostics. The final dotenv selector
  passed 15/15 in 246 ms and Core architecture passed 7/7 in 216 ms.
- Completed SA-340 at 10/41 tasks. Core now composes the frozen Wire codecs
  into immutable create/open/edit/validate/format/diff/coverage/export
  workflows for the exact six registered sections. Legal source remains a
  typed, role-correct, provenance-preserving review draft with no publication,
  acceptance, target, apply, or dotenv authority. The acceptance audit made
  public string projections value-safe, normalized section digests away from
  source identity, closed locale/scope validation, and moved Wire bounds checks
  into creation. Final workflow tests passed 10/10 in 321 ms and Core
  architecture passed 8/8 in 230 ms.
- Phase 3 Release build passed with zero errors in 38.01 seconds. It emitted
  812 pre-existing shared-worktree analyzer warnings; focused Core product and
  test builds remain clean apart from the two pinned namespace warnings.
- Phase 3 full `Event.Setup.Core.Tests` gate passed: 44/44 succeeded, zero
  failed/skipped, in 672 ms.
- Completed SA-420 at 12/41 tasks. The package-free BCL CLI now owns the closed
  parser, immutable explicit I/O/terminal/environment-presence seams,
  source-generated machine envelope, deterministic schema generator/check,
  real Core-backed diff/catalogue/environment/portability/legal/doctor
  behavior, and the explicit SA-430 TUI block. The acceptance audit added real
  baseline/candidate diffing, useful bounded catalogue outputs, validated
  activation selection, canonical 4 MiB input bounds, one-object pre-dispatch
  machine failures, truthful readiness/paths, broader operation/process tests,
  and cohesive modules. Final focused gates passed 8/8 application, 2/2
  program, 10/10 contract, and 10/10 architecture tests.
- Completed SA-430 at 13/41 tasks. The package-free human terminal workflow
  now requires the exact interactive TTY predicate, filename-first Unix
  create-new mode-0600 output, intercepted key input, key-or-signal wake
  coordination, Core-backed manual/generated `SETUP_SECRET` composition,
  deterministic restoration/clearing, and truthful incomplete readiness. The
  acceptance audit removed fabricated public configuration and metadata-only
  completion, blocked unsupported platforms before secret reads, and added
  real Unix permission/overwrite tests. The final lead selector passed 13/13
  in 271 ms after correcting the CLI exit to `Incomplete` when the protected
  artifact still has missing public requirements.
- Phase 4 Release build passed with zero errors in 45.29 seconds. It emitted
  11,228 pre-existing shared-worktree analyzer warnings; focused CLI and
  terminal builds remain clean apart from the two pinned namespace warnings.
- Phase 4 full `Event.SetupAssistant.Cli.Tests` gate passed: 33/33 succeeded,
  zero failed/skipped, in 628 ms.
- Completed a source-free analysis of the user-authorized WPF architecture
  documentation and current official CommunityToolkit/Avalonia/Terminal.Gui
  behavior. The plan now transfers constructor DI, generated observable
  properties/commands, typed weak messaging, transient workspace graphs, and
  minimal code-behind while preserving Event-native Core authority and secret
  boundaries. No external implementation source was accessed or copied.
- Historical then-current re-baseline moved the hot ledger from 41 to 43 tasks:
  SA-515 owned the
  framework-neutral CommunityToolkit presentation model, SA-520 owns shared
  Avalonia views/lifetimes, and SA-525 owns the explicit Terminal.Gui
  event/command adapter. Machine CLI/Core remain presentation-free. This is
  architecture direction only; no package, project, lock, shell, or approval
  was activated.
- Detected a concurrently created successor-B B0 Razor/browser workstream
  during verification. It was never approved and is now explicitly superseded,
  historical, and non-executable.

### Historical In Progress (Superseded)

- Phase 7 is Green. The first Phase 8 CTO review returned `Split before
  approval`; no Core product preimage changed. The task ledger now separates
  C1 Red, C1 Green Core, and C2 scale closures with fourteen matrices, exact
  numeric ceilings, a mutation/parser-bomb Worst Break, exact owned paths,
  verification dispositions, and per-slice planned commit contracts.

### Historical Next (Superseded)

1. Recompute a corrected Phase 8 binding over the revised task ledger and
   unchanged Core/test preimages.
2. Revalidate I-VSD and obtain a fresh CTO verdict, then author only C1 Red.

### BLOCKERS

- **SA-120 package stop condition:** Any Terminal.Gui or Avalonia pin/reference/
  lock entry in A, any replacement TUI/GUI package, any package exception, or
  any activation/shipping of the presentation shells stops the task. Unknown,
  incompatible, proprietary, commercial, source-available, or outbound-path-
  blocking material has no waiver path.
- **Successor-B package stop condition:** SA-510 approved only two roles, the
  CommunityToolkit.Mvvm `8.4.2` shared-presentation role and the Microsoft DI
  `10.0.10` plus Abstractions `10.0.10` executable-root role. Avalonia and
  Terminal.Gui are not approved, stay `ApprovedDisabled`, and are absent from
  every product project, pin, and lock. Each remaining graph needs its own
  pin/lock/generator/native/publish/license/notice/vulnerability evidence and
  fresh approvals. A blocked target stays disabled;
  no process-global messenger, Generic Host, ReactiveUI, custom binding
  framework, or silent fallback enters the graph.
- **B0 lifecycle:** B0 is superseded and non-executable. Its hashes remain
  provenance only and no review or conditional permission transfers to B1.
- **Later-phase review gates:** Phases 9–11 retain their named Tier 1,
  privacy, and Tier 0 intake/evidence gates even though the expanded I-VSD
  report is now plan-aligned.
- **Release enablement gates:** Hosted browser secret mode, legal templates,
  package formats, support tiers, and signing claims remain disabled until
  their named independent evidence exists. These gates do not block no-secret
  core planning.

## Historical Quick Resume (Superseded; Non-Executable)

Do not use the steps below to resume work. They preserve the earlier B1 state
only; `Resume From Here` is the sole current instruction.

1. Read this context, the
   [tasks](setup-assistant-security-and-portability-tasks.md), and the
   [dependency evidence](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-dependency-evidence.md).
2. Confirm successor A and the Phase 5 shared Toolkit model are Green while all
   richer targets remain disabled.
3. Resume at SA-610 browser secret-boundary Red. Run every B1
   restore/build/test/publish evidence command with
   `DOTNET_CLI_TELEMETRY_OPTOUT=1`, `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1`,
   `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1`, `DOTNET_NOLOGO=1`, and
   isolated package caches; the SDK advertises workloads over the network
   without them, which failed one traced probe build.
4. Keep tasks current immediately; update this context only on a trigger.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Responsibility |
|---|---|---|---|
| `src/Event.Wire.Contracts/` | Existing, to extend | Inner shared contract | Versioned manifest/package codecs and constrained legal Markdown |
| `src/Event.Setup.Core/` | New | Headless core | Environment catalogue, dotenv, offline validation/diff/readiness, workflow states |
| `src/Event.SetupAssistant/` | Existing A shell; B activation | Framework-neutral human presentation | Package-free disabled non-shipped shell in A; B adds CommunityToolkit-only ViewModels, commands, value-free messages, edit buffers, and Core projections, with no GUI types or I/O. DI registration stays executable-root-owned and is forbidden inside `Event.SetupAssistant` |
| `src/Event.SetupAssistant.Avalonia/` | New in B | Shared Avalonia view adapter | Typed compiled AXAML `UserControl` views, styles/resources, and no business authority; exact package graph remains blocked |
| `src/Event.SetupAssistant.Terminal/` | New in B | Terminal.Gui adapter | Explicit disposable event/command/property/collection/focus projection over shared ViewModels; exact package graph remains blocked |
| `src/Event.SetupAssistant.Browser/` | Existing A shell; B activation | Browser lifetime boundary | Thin single-view host over shared Avalonia views plus local download, CSP/capability, and secret-state adapter |
| `src/Event.SetupAssistant.Desktop/` | Existing A shell; B activation | Desktop lifetime boundary | Thin classic-desktop host over shared Avalonia views plus platform startup, native picker intent, and protected writes |
| `src/Event.SetupAssistant.Cli/` | New | CLI/terminal adapter | Stable commands, JSON schema, exit categories, repository-native BCL interactive wizard |
| `src/Event.Setup.Live/` | New | Outer live adapter | Target enrollment, HAL-gated operations, protected credential handles |
| `src/Explore.*/Features/ApplicationMigration/` | New | Server Clean Architecture | Application-data/payment migration plans, checkpoints, mappings, receipts, outbox |
| `eng/setup-assistant/` | New | Engineering/release | Environment generation, packaging, manifests, checksums/SBOM/provenance |
| `.agents/skills/setup-assistant-cli/` | New, final phase | Agent context | Version-gated no-secret machine workflows after CLI exists |
| `dev/active/setup-assistant-security-and-portability/` | New | Dev docs/evidence | Canonical plan, ledger, context, and source-free handoff |
| `islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md` | Existing, revalidated | I-VSD | Provider-responsibility findings, mitigations, escalation, planning handoff |

## Key Decisions

1. Reuse `Event.Wire.Contracts`; do not reference all Application from the
   offline product and do not duplicate v1alpha2/legal codecs.
2. `Event.Setup.Core` references only Wire Contracts and BCL APIs; all I/O,
   terminal, browser, desktop, and UI behavior is adapter-owned.
3. Live target identity, tenant authority, policy ceilings, imports,
   transactions, legal publication, and acceptance remain server-side.
4. Environment catalogue and secret-binding registry have distinct authority:
   the catalogue owns deployment relevance; the registry owns binding
   semantics; executable parity prevents drift.
5. No-secret browser mode is complete and primary. Hosted secret mode is a
   generated release capability that defaults disabled until exact-bundle
   independent evidence passes.
6. CLI machine mode is non-secret and deterministic. Secret terminal work uses
   a repository-native bounded BCL wizard, is human-only in a real TTY, and is
   product-tested for redirection, echo, signals, resize, scrollback, keyboard,
   non-color, and accessibility boundaries; agents never drive it.
7. Desktop writes use target-specific permission-first handle adapters and
   fail closed rather than degrade silently.
8. Legal editing produces draft/readiness source only; no publication,
   acceptance, legal approval, raw HTML, remote resources, or unapproved
   templates.
9. Terminal.Gui `2.4.17` and its complete 24-package graph are blocked; no pin,
   restore, partial profile, or exception is allowed. Avalonia `12.1.1` runtime
   targets are blocked and no Avalonia package is selected for A. The
   [dependency evidence](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-dependency-evidence.md)
   is the canonical decision record.
10. Successor A's presentation/Browser/Desktop projects are package-free,
    disabled, non-shipped contract shells only. Successor B owns actual GUI
    framework selection and must obtain a provenance-complete graph plus fresh
    I-VSD/CTO/user/target approvals.
11. Successor B's desired presentation geometry is now explicit:
    `Event.SetupAssistant` owns CommunityToolkit-only human ViewModels/messages;
    a separate shared Avalonia project owns AXAML; a separate Terminal.Gui
    project owns manual event/command projection; Browser/Desktop are thin
    Avalonia lifetime roots; machine CLI and Core reference none of them.
12. ViewModels project immutable Core state rather than reimplementing
    validation/readiness/sensitivity. Messenger instances are session-injected,
    recipients activate/deactivate explicitly, and no secret value may enter an
    observable property, message, command parameter, validation string, or
    automation property.
13. The first shippable successor is offline and source-free. Native Wayland,
    AppImage, Flatpak, global tool, PWA, auto-update, plugins, mobile, and AI
    remain outside it. Live control-plane, application-data, and payment are
    separate successor programs using generated server APIs and repository-native
    authority; Setup Core remains offline/pure and performs no direct database,
    provider, secret readback, or local authority operation.

## Constraints And Rules To Remember

- Tier 1 security requires failing adversarial boundary tests first,
  value-safe evidence, exhaustive dependency/trust review, and final
  independent security debate/review.
- Every new file has two ABOUTME lines and uses repository naming/record
  conventions.
- No secret, credential, connection string, signing key, or hard-coded test
  value enters source. External secrets remain in Infisical or `.env`.
- No backward-compatibility shim or parallel contract owner.
- No third-party source/code/prose/assets entered planning context; use the
  sanitized evidence packet for implementation.
- No dependency before locked graph, scanner, and target-specific
  license/outbound decision.
- No remote telemetry, analytics, crash upload, CSP reports, update checks, or
  production diagnostics.
- No browser/app/service startup, Playwright, Docker, Aspire, fixed sleeps, or
  manual runtime walkthrough in planned automated verification.
- Generated assets and lock files are generator-owned and never hand-edited.
- Tasks are the hot ledger; plan changes only for strategy; context changes only
  on meaningful state.

## Validation Baseline And SA-120 Evidence

The planning correction was Markdown-only. Subsequent approved SA-120
implementation and verification produced:

- all five changed/new workstream Markdown files have exactly two ABOUTME lines;
- all `IVSD-F001`-`IVSD-F046` findings and `IVSD-M001`-`IVSD-M046` mitigations
  remain preserved in the current plan-aligned report;
- the current re-baselined ledger retains exactly 47 unique SA implementation
  IDs and 47 implementation checkboxes; 13 tasks through SA-430 are complete;
- twelve phases retain explicit Release and selected/disposition test gates;
  Phase 5 and Phase 12 add target-specific gates so a blocked target can be
  proved `ApprovedDisabled` without certifying another target;
- ten force-evaluated restores, ten locked restores, and ten Release project
  builds passed with zero warnings/errors;
- all new package inventories and vulnerability/deprecation audits passed;
- the repository license policy passed 654 package/version pairs without a new
  exception;
- the generator check passed 2/2 and the focused SA-120 Setup architecture
  selector passed 6/6 in 258 ms;
- SA-130 workflow/action/cache/contract validation and local actionlint passed;
  the focused Setup selector passed 10/10 in 453 ms; five focused Setup project
  builds passed with zero warnings/errors; generator check passed 2/2; license
  policy passed 654 pairs; and Git semantics proved 21 trackable inputs plus
  seven ignored outputs;
- C# LSP diagnostics were clean. YAML LSP and local zizmor were unavailable;
  the repository Workflow Security lane installs pinned actionlint/zizmor, and
  no validator was weakened or installed ad hoc;
- the plan contains no checkboxes and context remains the sole session state;
- local Markdown links resolve, including the new dependency evidence;
- successor A contains no Terminal.Gui or Avalonia approval/pin path and no
  stale waiver; successor B remains framework-neutral and independently gated;
- `git diff --check` passes for the five-file planning/evidence bundle.

Each implementation phase runs exactly:

| Phase | Release build | One selected test project |
|---|---|---|
| 1 — Foundation | `dotnet build --configuration Release --verbosity quiet` | `Event.Architecture.Tests` |
| 2 — Shared contracts/core | same | `Event.Wire.Contracts.UnitTests` |
| 3 — Environment/offline workflows | same | `Event.Setup.Core.Tests` |
| 4 — CLI/TUI | same | `Event.SetupAssistant.Cli.Tests` |
| 5 — Shared MVVM/human adapters | same | `Event.SetupAssistant.Tests` |
| 6 — Browser | same | `Event.SetupAssistant.Browser.Tests` |
| 7 — Desktop | same | `Event.SetupAssistant.Desktop.Tests` |
| 8 — Composition/scale | same | `Event.Setup.Core.Tests` |
| 9 — Live enrollment/provider | same | `Event.API.IntegrationTests` |
| 10 — Live apply/transfer | same | `Event.SetupAssistant.Tests` |
| 11 — Data/payment migration | same | `Event.Persistence.IntegrationTests` |
| 12 — Release/skill | same | `Event.Architecture.Tests` |

Known repository baseline:

- The previous ConfigurationManifest session recorded a Release build with
  zero errors and unrelated analyzer warnings.
- ConfigurationManifest verification remains active; its gate results are not
  Setup implementation evidence.
- Do not rerun or reinterpret historical ConfigurationManifest results as
  Setup evidence.

## Current Known Risks / Unknowns

- Frozen ConfigurationManifest archive drift or capability overclaim: SA-110.
- Successor-B human-presentation graphs: Avalonia runtime targets are currently blocked;
  Terminal.Gui is separately blocked; CommunityToolkit/DI exact graphs are
  unselected. Successor B must prove and approve each graph independently.
- Presentation drift: SA-510/SA-515 must prove one Core-backed ViewModel state
  graph without secret-bearing messages or duplicated validation; SA-520 and
  SA-525 must prove Avalonia compiled-binding and Terminal.Gui teardown parity
  without contaminating machine CLI/Core.
- Environment catalogue/default/activation completeness: SA-310/SA-320.
- Browser exact-bundle network/storage and origin review: SA-610–SA-640.
- Desktop filesystem/ACL support matrix: SA-710–SA-730.
- Composition ambiguity and scale evidence: SA-810–SA-830.
- Live authority/provider secret boundary: SA-910–SA-1030.
- Application-data tenant/replay/privacy risk: SA-1110–SA-1130.
- Payment reconciliation/refund authority: SA-1110/SA-1140.
- Legal template/counsel approval: SA-540/SA-1220.
- OS/RID/package support and signatures: SA-1210/SA-1220.
- Agent skill/CLI drift: SA-1240.

## Historical Handoff Notes (Superseded; Non-Executable)

All nested handoffs and their `Current state`/`Next action` fields preserve
prior evidence only. They cannot override `Resume From Here`.

### Handoff — 2026-08-31 Europe/Brussels — SA-130 CI Governance

- **Current state:** SA-130 is complete at 3/41 implementation tasks. Setup
  paths route Architecture plus the Release Setup project gate; all ten locks
  are recursively audited; both generated ratchets are checked without writes;
  source/future browser `wwwroot` remains trackable while generated outputs are
  ignored. Phase 1 build and full Architecture gates remain unchecked.
- **Authority:** Both workflows retain top-level `contents: read`, every
  checkout disables persisted credentials, the caller passes no integration
  activation, and no PR secret, write permission, OIDC, signing, publish,
  deployment, or release authority was introduced.
- **Validation:** Repository action-pin, Dependabot, workflow-cache, deploy
  contract, and intent validators passed; local actionlint passed. Focused
  `SetupAssistantArchitectureTests` passed 10/10 once in 453 ms. Five focused
  Setup project builds passed with zero warnings/errors; ratchet check passed
  2/2; license policy passed 654 pairs; Git semantics passed for 21 tracked
  inputs and seven ignored outputs; C# diagnostics were clean. YAML LSP and
  local zizmor were unavailable and no Node/Python tool was installed.
- **Safety:** No secret, credential, token, connection string, personal data,
  or other PII was supplied or emitted. SA-120 Green evidence remains intact;
  no dependency, exception, generated write, runtime UI, or feature behavior
  was added.
- **Next action:** Run the full selected Architecture project gate once.

### Historical Handoff — 2026-08-31 Europe/Brussels — SA-120 Dependency Decision

- **Current state:** Dependency research is decision-complete but SA-120 remains
  open. Terminal.Gui's 24-package graph and Avalonia runtime targets are
  blocked; A selects a package-free BCL product graph. No source scaffolding,
  lock, package pin, restore, or generated ratchet is claimed.
- **Review truth:** The package/implementation strategy materially changed.
  Treat the unchanged I-VSD as `stale` / `changes-required`; preserve all
  F001-F046/M001-M046 and run planning-mode revalidation immediately. Existing
  CTO/user artifacts bind only prior hashes and do not approve this revision.
- **Next action:** Revalidate I-VSD, obtain fresh revision-bound CTO and user
  approval, then resume open SA-120 by creating ten package-free project
  shells/locks and both fail-closed ratchets.
- **Successor boundary:** A's presentation/Browser/Desktop projects are disabled
  non-shipped contract shells only. B owns actual GUI selection and all runtime
  targets with no inherited package approval.
- **Evidence:**
  [Canonical dependency decision](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-dependency-evidence.md).
- **Validation:** Markdown-only ABOUTME, link, SA-ID, checkbox, blocked-pin,
  stale-waiver, and whitespace checks; no .NET command.

### Historical Handoff — 2026-08-31 Europe/Brussels — Pre-SA-110 CTO Split Correction (Superseded)

- **Current state:** Full objective preserved as an umbrella with seven
  independently approved successors; Foundation A is sole active; 0/41 tasks
  complete and all 12 phase gates remain open.
- **Review truth:** Current I-VSD revision
  `sha256:055fb1dd8c0dfcdbd809bbfb89cbd2660904469fd3d866d6d6349af091793d4f`
  is `current` / `plan-aligned`. The first CTO review decided `Split before
  approval`; corrected fresh CTO and exact-revision user approvals are pending.
- **Next action:** Compute final plan/tasks hashes, obtain fresh read-only CTO
  review, bind user approval, then begin SA-110 Red only in successor A.
- **Later boundaries:** D requires green ConfigurationManifest Tier 1/tenant/
  replay/atomicity evidence with no bypass. F owns the named deterministic
  real-database/provider Worst Break Red before SA-1140.
- **Validation:** Markdown-only structural, link, ID, phase-gate, contradiction,
  and whitespace checks; no .NET command.

### Historical Handoff — 2026-08-31 — Deferred Scope Promotion (Superseded)

- **Current state:** Former ConfigurationManifest deferred work is now owned by
  Setup Phases 8–11; existing Avalonia/CLI/TUI/dotenv/skill work remains in its
  original phases and was not duplicated.
- **Next action:** Revalidate I-VSD, then obtain user/CTO approval before SA-110.
- **Blockers:** Expanded live/application-data/payment phases are deliberately
  blocked on fresh I-VSD and Tier 0/1 review.
- **Modified files:** Setup plan/context/tasks and Setup I-VSD status metadata.
- **Validation:** Markdown-only consistency and whitespace checks; no product
  build or tests.
- **Documentation impact:** Planning only; no runtime/operator claim changed.

### Historical Handoff — 2026-08-31 — ConfigurationManifest Dependency Reconciliation (Superseded)

- **Current state:** ConfigurationManifest is active. Setup remains planning-
  only and consumes its frozen v1alpha2/no-secret/legal Markdown contract.
- **Next action:** User review, revision-bound Senior CTO review, then SA-110.
- **Blockers:** Setup user approval and CTO review only.
- **Modified files:** Setup plan/context/tasks/clean-room evidence and Setup
  I-VSD report; ConfigurationManifest plan/context/tasks/I-VSD contain the
  matching archive disposition.
- **Validation:** Evidence digest refreshed to
  `sha256:8c86d6be6f612861bba9c4ea641a451722fe0d6b5feccad09ac310b5cdce1637`;
  final link/state/whitespace checks remain.
- **Documentation impact:** No runtime or operator documentation changed.
- **Risks:** Upstream server implementation does not prove Setup extraction or
  portability behavior.
- **Notes for next contributor/agent:** SA-110 must fail on frozen-baseline
  drift or capability overclaim before contract extraction.

### Historical Handoff — 2026-08-30 (Superseded)

- **Current state:** Planning triad and clean-room evidence created; I-VSD
  revalidated to the evidence revision; no implementation started.
- **Next action:** User review and revision-bound CTO review, then SA-110.
- **Blockers:** User approval and CTO review only.
- **Modified files:** Only the new Setup workstream planning/evidence files and
  the existing Setup I-VSD report belong to this planning task.
- **Validation:** ABOUTME, required headings, 36 finding/mitigation mappings,
  28 task IDs, eight phase gates, local links, and SHA-256 review binding were
  revalidated after the upstream archive handoff. Final whitespace/link
  validation is pending this update. No .NET build/test is appropriate for
  planning-only docs.
- **Documentation impact:** Runtime docs are planned but unchanged.
- **Risks:** Hosted secret mode and cross-platform claims remain evidence-gated.
- **Notes for next contributor/agent:** The shared worktree contains extensive
  unrelated modified/deleted/untracked ConfigurationManifest, ticketing,
  secrets-control-plane, webhook, schema, API, generated-client, Domain,
  Persistence, and test files. Do not revert, delete, format, regenerate, or
  include them in Setup work without explicit ownership.

### Historical Handoff — 2026-08-31 — SA-220 Package-Free Ownership Cutover

- **Current state:** 5/41 tasks complete; SA-220 is Green and SA-230 is next.
  `ISLAMU.Wire.Contracts.ConfigurationPortability` solely owns the v1alpha2
  record closure, source-generated JSON context, strict compact codec,
  diagnostics/limits, and constrained legal Markdown codec. Application keeps
  orchestration/validation and Domain keeps aggregate publication, locale,
  provenance, and UTF-8 invariants while depending inward on Wire syntax.
- **Deleted owners:** Application contract/context and
  `Explore.Domain.LegalMarkdownContract`; production/test searches return zero
  old namespace, old context, old owner, alias, and type-forward references.
- **Compatibility:** None. No forwarding namespace, duplicate DTO, obsolete
  shim, reflection serializer, package, or dual context was introduced.
- **Stable artifacts:** Both canonical schema checks pass byte-for-byte.
  Manifest schema SHA-256 is
  `25b6ea15b13367e714c97717a82f96c0a05df337d5540e3c3e69a56110984597`;
  tenant-package schema SHA-256 is
  `2d7832a5b4d36e052168717d0c737ab7332e52c6ff353bd8e5212ec3c224d75a`.
- **Verification:** SA-210 6/6, Wire legal 4/4, Wire contract 7/7,
  Application parser 4/4, serialization 3/3, validator 26/26, Domain legal
  aggregate 13/13, schema generation 9/9, schema artifact 2/2, SA-110 10/10,
  and API tenant package 2/2 passed. Eleven impacted Release builds completed
  with zero errors; focused LSP scans were clean where the server responded.
- **Execution notes:** Initial concurrent builds exposed only output-file lock
  contention in shared transitive projects; the required final sequential
  builds passed. Existing analyzer-warning volume remains unrelated. The JSON/
  reflection Red fixtures had two internally contradictory helpers (nullable
  property access and inherited `MemberwiseClone` selection); both were made
  precise without weakening behavior.
- **Next action:** SA-230 was the next bounded slice and is now complete. Do not
  mark Phase 2 complete until both named Phase 2 gates pass.

### Historical Handoff — 2026-08-31 — SA-230 Deterministic Core

- **Current state:** 6/41 tasks complete; SA-230 is Green and the Phase 2 Release build is next.
- **Product boundary:** `Event.Setup.Core` references Wire plus BCL only and owns immutable setup metadata, value-safe diagnostics, readiness, artifact digests, section diff/coverage facts, and explicit transitions. It owns no environment/dotenv, file, CLI, UI, host, provider, persistence, telemetry, localization, or live behavior.
- **Verification:** Seven focused tests pass in 242 ms in the final direct executable run. Final Core and test Release builds have zero warnings/errors; seven new C# files have clean diagnostics.
- **Verifier proof:** Synthetic ambient time, mutable collection, and supplied-value diagnostic fixtures are rejected by compiled metadata/IL verification.
- **Dependency proof:** Product inventory is empty; tests retain only existing TUnit 1.63.0. Vulnerability/deprecation checks are clean and locks are unchanged.
- **Execution note:** The first post-compile selector found two verifier/test defects (5/7); the corrected selector passed 7/7 in 237 ms. This history is retained in cumulative evidence rather than hidden.
- **Next action:** Run only the Phase 2 Release build, then the full Wire project test gate. Do not mark either gate complete before its command passes.

### Historical Handoff — 2026-08-31 — SA-320 Environment Catalogue

- **Current state:** 8/41 tasks complete; SA-320 is Green and SA-330 is current.
- **Product boundary:** Core owns 346 package-free immutable definitions, closed
  identifier activation, generation/restart/docs metadata, ordinal lookup and
  relevance, and fixed value-free diagnostics. It performs no file, JSON,
  process, network, provider-SDK, reflection, telemetry, or environment access.
- **Generator boundary:** The package-free executable references compiled Core
  and Domain symbols, never registry source. It checks 52 secret authorities,
  310 template keys, 290 nested Compose keys, requiredness, and classifications;
  it writes deterministic machine JSON and bounded docs only. Existing template
  values and Compose topology remain preserved; one obsolete mixed-case alias
  was removed to enforce canonical naming.
- **Verification:** Final generator write/check passed after the audit repair;
  machine SHA-256 is
  `e228416527b22fdaefa1ffa199b146a748f485e0f92ef169a009e02968b54a90`.
  Catalogue tests passed 11/11 in 269 ms, including explicit semantic metadata,
  repeated-feature, duplicate-feature, relevance, and nested-AST regressions.
  Core architecture passed 7/7; Setup architecture/CI passed 10/10.
  The focused test build is warning-free; the generator build retained two
  existing pinned-namespace CA1716 warnings and zero errors. The broader
  Architecture build retained 496 pre-existing analyzer warnings and zero errors.
- **Diagnostics/tools:** C# LSP requests timed out; YAML LSP and actionlint were
  unavailable. Successful builds/tests are recorded instead, without claiming
  unavailable diagnostics passed.
- **Next action:** Implement only SA-330 dotenv codec/readiness/approved local
  generation. Do not mark SA-330 or run the Phase 3 gates early.

### Historical Handoff — 2026-08-31 — SA-410 CLI Contract Red

- **Current state:** 11/41 tasks complete; SA-410 is an accepted bounded Red and
  SA-420 is current.
- **Contract:** The checked v1 command schema and package-free source-free tests
  pin exact command families/operations, explicit modes and I/O, closed exits,
  one bounded canonical machine object, stable diagnostics/artifact/coverage/
  readiness projections, no control or localized authority, and fail-closed
  argument, option, environment-name, captured-input, output, and TTY vectors.
- **Observed selector:** The prescribed selector ran exactly once: 10 total,
  8 passed, 2 failed, 0 skipped in 276 ms. One failure is the intended aggregate
  missing-owner/marker prerequisite. The other was a test-only Linux separator
  defect in project-reference discovery; it was corrected after the run and the
  test project recompiles with zero warnings/errors, but the selector was not
  rerun under the run-once rule; the lead confirmation below closed that
  corrected independent assembly fact before SA-420 Green.
- **Lead confirmation:** The corrected selector then observed 10 total, 9
  passed, 1 failed, 0 skipped in 237 ms. Its sole failure is the intended
  aggregate missing final executable-owner/marker prerequisite.
- **Unimplemented:** Production command parsing/execution, source-generated
  machine serialization, command schema generation/checking, and human TTY
  secret behavior remain absent. SA-420 owns the first three; SA-430 owns actual
  human TTY secret behavior.

### Historical Handoff — 2026-08-31 — B1 Presentation Re-baseline

Superseded by the later historical B1 handoff below. Its counts and Red status describe
the state before SA-510 probing and the SA-515 Red.

- **State at the time:** 13/47 tasks complete; successor A and Phase 4 Green.
  The B0 Red is historical, no corrected B1 Red has run, and no successor-B
  package or shell is active.
- **Decision:** Transfer framework-common WPF conventions into a new
  CommunityToolkit-only human-presentation owner, then adapt it independently
  through compiled Avalonia views and an explicit disposable Terminal.Gui
  event/command seam. Keep machine CLI and Core presentation-free.
- **Security boundary:** ViewModels project immutable Core results; they do not
  own readiness, sensitivity, serialization, or business validation. Messenger
  instances are session-injected and no secret value may enter observable
  state, messages, commands, validation, automation, clipboard, or history.
- **Clean-room evidence:** Only user-authorized WPF architecture documentation
  and package identities plus official Microsoft/Avalonia/Terminal.Gui docs
  were used. No external implementation source, XAML, tests, assets, or
  expressive application structure entered the handoff.
- **Modified files:** plan, tasks, context, clean-room evidence, and dependency
  evidence in this workstream only; unrelated active product-code changes were
  preserved.
- **Validation:** Markdown/link/ledger verification remains to run. No build or
  .NET test is permitted for this documentation-only re-baseline.
- **Next action:** Bind the corrected B1 packet and obtain fresh
  I-VSD/CTO/dependency/security/accessibility/user approval before any package
  probe or implementation.
- **Historical note:** The B0 triad/I-VSD/CTO/binding record is superseded and
  non-executable.

### Historical Handoff — 2026-08-31 — B1 SA-510 Probes And SA-515 Red Complete

- **Current state:** 15/47 tasks complete. SA-510 and SA-515 are checked;
  SA-518 is the next unchecked task and product activation has not started.
- **Graph decisions:** the exact isolated Toolkit `8.4.2` and Microsoft DI
  `10.0.10` plus Abstractions `10.0.10` probes passed, and dependency/IP,
  security, and accessibility each issued `Approve` for the Toolkit
  shared-presentation role and the DI executable-root role. Avalonia
  shared/browser/desktop `12.1.1` and Terminal.Gui `2.4.17` stay
  `ApprovedDisabled`, absent, and unresolvable. Evidence:
  [B1 probe evidence](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-b1-probe-evidence.md),
  `sha256:424ef6b6e3b7b7700b4d26b11149545b0f97fe0165a22890d35a67f9e8e14be8`.
- **Environment finding:** the first traced no-restore build showed .NET SDK
  workload-advertising egress and was failed. Corrected builds with
  `DOTNET_CLI_TELEMETRY_OPTOUT=1`, `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1`,
  `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1`, `DOTNET_NOLOGO=1`, and
  isolated package caches saw zero `AF_INET`/`AF_INET6` `connect`/`sendto`.
  That environment is mandatory for future B1 evidence runs.
- **Untouched product surface:** product projects, product and test locks,
  central pins, the solution, shipping CI, generated capabilities, and
  support/release/shipping flags stayed byte-identical and false.
- **Red state:** the obsolete untracked B0 `SetupAssistantWorkspace*` files were
  removed and replaced with `SetupPresentationModelContract.cs` and
  `SetupPresentationModelTests.cs`. Ten focused tests compile and all ten fail
  only with
  `missing-approved-owner:ISLAMU.Event.SetupAssistant.Presentation.SetupPresentationSession`.
  No silent early return, synthetic transition table, class-name inventory,
  fixed sleep, timing poll, secret fixture, or product/package reference was
  added. The only warning is the pre-existing CA1716 namespace warning from the
  test `Program.cs`.
- **Clean-room:** `pass`. No external source, code, tests, or assets entered the
  implementation context; the new tests and the planned model are independently
  repository-native.
- **Next action:** rebind the exact post-Red reviews, then begin SA-518. The
  latest user goal asks for full implementation without backward compatibility,
  and this rebinding precedes any product edit.
- **Not claimed:** no rendered accessibility result, target activation, support
  tier, release, or shipping claim.
