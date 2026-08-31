<!-- ABOUTME: Active working memory for the Setup Assistant security and portability workstream. -->
<!-- ABOUTME: Records review state, blockers, resume order, evidence, decisions, and validation handoffs. -->

# Setup Assistant Security And Portability — Context

Last Updated: 2026-08-31 Europe/Brussels

## Review State

- **Planning status:** Successor A foundation-offline approved and active;
  SA-420 deterministic CLI is Green; SA-430 human terminal workflows are
  current.
- **I-VSD report:**
  [i-vsd-setup-assistant-security-and-portability.md](../../../islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md)
- **I-VSD reviewed input revision:**
  `sha256:d2bbba40455c013e20883ab6202f84411bb05f2c20f6060a9e73095f44a8e4b1`
- **I-VSD status / disposition:** `current` / `plan-aligned`; mappings
  `IVSD-F001`–`IVSD-F046` and `IVSD-M001`–`IVSD-M046` are preserved.
- **First CTO review:** [Split before approval](setup-assistant-security-and-portability-cto-review.md), bound to the prior exact plan/tasks revisions.
- **Current correction review:** [Approved the BCL-only successor-A strategy](setup-assistant-security-and-portability-cto-review.md);
  later successors inherit no approval.
- **Corrected plan review revision:**
  `sha256:8b4d46006a99cb456afbe42efe3c4bf141c9e168babeffe18c70e63f0d19450c`
- **Corrected tasks review revision:**
  `sha256:9f2e5e9ab4e67b674f4655ea88baa3b9920ea8fa7d7daca47ac14afbea4f2480`
- **User approval:** [Bound to the reviewed BCL-only successor-A revision](setup-assistant-security-and-portability-approval.md).
- **Dependency evidence:**
  [setup-assistant-security-and-portability-dependency-evidence.md](setup-assistant-security-and-portability-dependency-evidence.md)

## Umbrella Program State

This directory is the umbrella for seven independently reviewable successor
boundaries; successor directories have not been created. Foundation A
`setup-assistant-foundation-offline` is the sole candidate successor but is
approved and active; SA-110 through SA-420 are complete. Phase 1 through
Phase 3 verification are Green; SA-430 is current. B
`setup-assistant-presentation-targets`
now owns actual GUI framework selection and runtime targets. B, C
`setup-assistant-composition-scale`, D `setup-assistant-live-control-plane`, E
`setup-application-data-migration`, F `setup-sovereign-payment-migration`, and G
`setup-release-and-agent-contract` are inactive.

PR slices are A1 SA-110-SA-130 with ten package-free project shells, A2
SA-210-SA-230, A3 SA-310-SA-340, A4 SA-410-SA-430 BCL CLI/terminal wizard;
B1 SA-510-SA-540 framework-neutral GUI selection/workspaces, B2 SA-610-SA-640,
B3 SA-710-SA-730; C1
SA-810/SA-820, C2 SA-830; D1 SA-910 Red/server contracts, D2 SA-920/SA-930,
D3 SA-1010 Red plus SA-1020/SA-1030; E1 SA-1110 privacy/tenant Red, E2 SA-1120,
E3 SA-1130, E4 Setup UI activation; F1 Worst Break Red/Tier 0 record, F2
SA-1140 Domain/Persistence/provider reconciliation, F3 API/HAL/Setup activation;
G1 SA-1210/SA-1220 per subset, G2 SA-1240 after CLI schema, G3 SA-1250.

Dependencies are one-way A -> B/C -> D -> E; F depends on D/E contracts and is
independently optional; G runs per subset and at final reconciliation. No later
successor inherits umbrella or predecessor approval. Every successor requires
the current I-VSD revision, fresh tier-appropriate intake, fresh CTO review,
exact-revision user approval, and named evidence before implementation.

## SESSION PROGRESS (2026-08-31 Europe/Brussels)

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

### IN PROGRESS

- Phase 4 SA-430 repository-native human terminal workflows.

### NEXT

1. Author and implement SA-430 TTY-only human terminal secret workflows while
   preserving the existing machine CLI as the automation surface.

### BLOCKERS

- **SA-120 package stop condition:** Any Terminal.Gui or Avalonia pin/reference/
  lock entry in A, any replacement TUI/GUI package, any package exception, or
  any activation/shipping of the presentation shells stops the task. Unknown,
  incompatible, proprietary, commercial, source-available, or outbound-path-
  blocking material has no waiver path.
- **Later-phase review gates:** Phases 9–11 retain their named Tier 1,
  privacy, and Tier 0 intake/evidence gates even though the expanded I-VSD
  report is now plan-aligned.
- **Release enablement gates:** Hosted browser secret mode, legal templates,
  package formats, support tiers, and signing claims remain disabled until
  their named independent evidence exists. These gates do not block no-secret
  core planning.

## Quick Resume

1. Read this context, the
   [tasks](setup-assistant-security-and-portability-tasks.md), and the
   [dependency evidence](setup-assistant-security-and-portability-dependency-evidence.md).
2. Confirm successor A remains the sole approved active successor.
3. Resume at SA-430 from the Green deterministic CLI evidence.
4. Keep tasks current immediately; update this context only on a trigger.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Responsibility |
|---|---|---|---|
| `src/Event.Wire.Contracts/` | Existing, to extend | Inner shared contract | Versioned manifest/package codecs and constrained legal Markdown |
| `src/Event.Setup.Core/` | New | Headless core | Environment catalogue, dotenv, offline validation/diff/readiness, workflow states |
| `src/Event.SetupAssistant/` | New A shell; B activation | Shared presentation boundary | Package-free disabled non-shipped contract shell in A; successor B selects and implements the GUI framework |
| `src/Event.SetupAssistant.Browser/` | New A shell; B activation | Browser boundary | Package-free disabled non-shipped shell in A; B owns static runtime, local download, CSP/capability and secret-state adapter |
| `src/Event.SetupAssistant.Desktop/` | New A shell; B activation | Desktop boundary | Package-free disabled non-shipped shell in A; B owns platform startup, native picker, and protected writes |
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
   [dependency evidence](setup-assistant-security-and-portability-dependency-evidence.md)
   is the canonical decision record.
10. Successor A's presentation/Browser/Desktop projects are package-free,
   disabled, non-shipped contract shells only. Successor B owns actual GUI
   framework selection and must obtain a provenance-complete graph plus fresh
   I-VSD/CTO/user/target approvals.
11. The first shippable successor is offline and source-free. Native Wayland,
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
- the ledger retains exactly 41 unique SA implementation IDs and 41
  implementation checkboxes; SA-110 and SA-120 are complete;
- twelve phases retain exactly one Release build and one selected project test
  checkbox, for 24 phase verification checkboxes total;
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
| 5 — Shared GUI | same | `Event.SetupAssistant.Tests` |
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
- Successor-B GUI graph: Avalonia runtime targets are currently blocked;
  successor B must choose a provenance-complete graph or obtain new
  authoritative component/build evidence and fresh approvals.
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

## Handoff Notes

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
  [Canonical dependency decision](setup-assistant-security-and-portability-dependency-evidence.md).
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

### Current Handoff — 2026-08-31 — SA-220 Package-Free Ownership Cutover

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

### Current Handoff — 2026-08-31 — SA-230 Deterministic Core

- **Current state:** 6/41 tasks complete; SA-230 is Green and the Phase 2 Release build is next.
- **Product boundary:** `Event.Setup.Core` references Wire plus BCL only and owns immutable setup metadata, value-safe diagnostics, readiness, artifact digests, section diff/coverage facts, and explicit transitions. It owns no environment/dotenv, file, CLI, UI, host, provider, persistence, telemetry, localization, or live behavior.
- **Verification:** Seven focused tests pass in 242 ms in the final direct executable run. Final Core and test Release builds have zero warnings/errors; seven new C# files have clean diagnostics.
- **Verifier proof:** Synthetic ambient time, mutable collection, and supplied-value diagnostic fixtures are rejected by compiled metadata/IL verification.
- **Dependency proof:** Product inventory is empty; tests retain only existing TUnit 1.63.0. Vulnerability/deprecation checks are clean and locks are unchanged.
- **Execution note:** The first post-compile selector found two verifier/test defects (5/7); the corrected selector passed 7/7 in 237 ms. This history is retained in cumulative evidence rather than hidden.
- **Next action:** Run only the Phase 2 Release build, then the full Wire project test gate. Do not mark either gate complete before its command passes.

### Current Handoff — 2026-08-31 — SA-320 Environment Catalogue

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

### Current Handoff — 2026-08-31 — SA-410 CLI Contract Red

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
