<!-- ABOUTME: Active working memory for the Setup Assistant security and portability workstream. -->
<!-- ABOUTME: Records review state, blockers, resume order, evidence, decisions, and validation handoffs. -->

# Setup Assistant Security And Portability — Context

Last Updated: 2026-08-31 Europe/Brussels

## Review State

- **Planning status:** Umbrella correction awaiting fresh revision-bound CTO
  review and exact-revision user approval; implementation not started.
- **I-VSD report:**
  [i-vsd-setup-assistant-security-and-portability.md](../../../islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md)
- **I-VSD reviewed input revision:**
  `sha256:055fb1dd8c0dfcdbd809bbfb89cbd2660904469fd3d866d6d6349af091793d4f`
- **I-VSD status / disposition:** `current` / `plan-aligned`; expanded
  findings `IVSD-F037`–`IVSD-F046` preserve the named Tier 0/1/2 gates.
- **First CTO review:** [Split before approval](setup-assistant-security-and-portability-cto-review.md), bound to the prior exact plan/tasks revisions.
- **Correction review:** Awaiting fresh revision-bound read-only CTO review; no
  approval has been granted for this correction.
- **Corrected plan review revision:**
  `sha256:8b4d46006a99cb456afbe42efe3c4bf141c9e168babeffe18c70e63f0d19450c`
- **Corrected tasks review revision:**
  `sha256:9f2e5e9ab4e67b674f4655ea88baa3b9920ea8fa7d7daca47ac14afbea4f2480`
- **User direction:** The full Setup Assistant objective was approved on
  2026-08-31. Corrected exact-revision approval awaits binding after final
  plan/tasks hashes and fresh CTO review.

## Umbrella Program State

This directory is the umbrella for seven independently reviewable successor
boundaries; successor directories have not been created. Foundation A
`setup-assistant-foundation-offline` is the sole active successor and remains
blocked before SA-110. B `setup-assistant-presentation-targets`, C
`setup-assistant-composition-scale`, D `setup-assistant-live-control-plane`, E
`setup-application-data-migration`, F `setup-sovereign-payment-migration`, and G
`setup-release-and-agent-contract` are inactive.

PR slices are A1 SA-110-SA-130, A2 SA-210-SA-230, A3 SA-310-SA-340, A4
SA-410-SA-430; B1 SA-510-SA-540, B2 SA-610-SA-640, B3 SA-710-SA-730; C1
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
- Selected Avalonia `12.1.1` and Terminal.Gui `2.4.17` only as candidates
  subject to exact target-graph review.
- Resolved the architecture into package-free Wire Contracts, pure
  `Event.Setup.Core`, thin Avalonia/Browser/Desktop/CLI adapters, and
  server-authoritative live apply.
- Re-baselined the plan with 15 RFC 2119 requirements and 12 phases. New
  phases own YAML/directory composition and measured scale, live enrollment
  and secret-provider binding, live apply/direct transfer, and application-
  data/payment-operation migration.
- Re-baselined the hot ledger with 41 implementation tasks and separate phase
  verification checkboxes.
- Created the sanitized clean-room evidence packet and bound review state to
  its SHA-256 revision.
- Recorded the user's direction approving the complete objective; after the CTO
  split decision, corrected exact-revision approval must be rebound after final
  hashes and fresh review.
- Revalidated the expanded composition, live-authority, application-data,
  privacy, and payment scope through planning-mode I-VSD. The current report
  adds `IVSD-F037`–`IVSD-F046` with matching mitigations and maps them to
  Scenarios 3.13–3.15 and SA-810–SA-1250.
- Recorded the first revision-bound CTO decision as `Split before approval` and
  recast this directory as the seven-successor umbrella with Foundation A sole
  active.
- Established the pre-implementation Release build baseline. It currently
  fails in unrelated shared-worktree
  `tests/Event.Domain.UnitTests/ValueObjects/AtprotoDidTests.cs` because the
  concurrent strong-typing work removed or moved `AtprotoDid`; the build
  reported 10 `CS0103` errors and 1026 pre-existing analyzer warnings.

### IN PROGRESS

- Correcting the four canonical planning/evidence files in response to the
  first CTO decision `Split before approval`; fresh review has not started.

### NEXT

1. Compute final plan/tasks hashes and obtain fresh revision-bound read-only
   CTO review of the corrected umbrella and successor A.
2. Bind explicit user approval to that corrected exact revision.
3. Only then start `SA-110` Red in successor A.

### BLOCKERS

- **Approval blocker:** The first CTO review decided `Split before approval`;
  fresh revision-bound CTO review and corrected exact-revision user approval
  are not yet recorded.
- **Later-phase review gates:** Phases 9–11 retain their named Tier 1,
  privacy, and Tier 0 intake/evidence gates even though the expanded I-VSD
  report is now plan-aligned.
- **Release enablement gates:** Hosted browser secret mode, legal templates,
  package formats, support tiers, and signing claims remain disabled until
  their named independent evidence exists. These gates do not block no-secret
  core planning.

## Quick Resume

1. Read this context and
   `setup-assistant-security-and-portability-tasks.md`.
2. Confirm the corrected plan/tasks hashes and whether fresh CTO and
   exact-revision user approval were recorded.
3. Confirm Foundation A remains the sole active successor.
4. If approved and unblocked, read only plan Phase 1 plus Sections 0.1, 4,
   5.1, 5.4, 5.7, and 5.9.
5. Start `SA-110`; do not scaffold packages or projects before its Red
   contracts pin the frozen ConfigurationManifest archive baseline.
6. Keep tasks current immediately; update this context only on a trigger.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Responsibility |
|---|---|---|---|
| `src/Event.Wire.Contracts/` | Existing, to extend | Inner shared contract | Versioned manifest/package codecs and constrained legal Markdown |
| `src/Event.Setup.Core/` | New | Headless core | Environment catalogue, dotenv, offline validation/diff/readiness, workflow states |
| `src/Event.SetupAssistant/` | New | Shared presentation | Avalonia views, view models, resources, accessibility/localization |
| `src/Event.SetupAssistant.Browser/` | New | Browser adapter | Static WASM startup, local download, CSP/capability and secret-state boundary |
| `src/Event.SetupAssistant.Desktop/` | New | Desktop adapter | Platform startup, native picker, Windows/Unix protected writes |
| `src/Event.SetupAssistant.Cli/` | New | CLI/TUI adapter | Stable commands, JSON schema, exit categories, Terminal.Gui |
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
6. CLI machine mode is non-secret and deterministic. Secret terminal work is
   human-only in a real TTY; agents never drive the TUI.
7. Desktop writes use target-specific permission-first handle adapters and
   fail closed rather than degrade silently.
8. Legal editing produces draft/readiness source only; no publication,
   acceptance, legal approval, raw HTML, remote resources, or unapproved
   templates.
9. Candidate packages are not approved dependencies. The complete graph,
   obligations, vulnerabilities, and outbound paths decide.
10. The first shippable successor is offline and source-free. Native Wayland,
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

## Validation Baseline

This correction is Markdown-only. No product build or implementation test was
run or claimed for the correction.

Planning verification refreshed on 2026-08-31:

- all required plan and I-VSD report headings are present;
- all four workstream Markdown files have two ABOUTME lines;
- all `IVSD-F001`-`IVSD-F046` findings map one-to-one to
  `IVSD-M001`-`IVSD-M046`; the current report is `current` / `plan-aligned`;
- all plan task IDs converge with 41 open implementation tasks;
- every implementation checkbox states a verification assertion;
- twelve phases each have one Release build and one selected project test gate;
- the plan contains no execution checkboxes;
- local Markdown links resolve;
- the clean-room handoff covers every successor without importing external
  implementation expression;
- final hashes must be computed after the last correction edit and then bound
  by fresh CTO/user review;
- `git diff --check` passes for the four-file correction bundle.

No .NET build or test was run because this workflow changed planning Markdown
only. Existing source/test/generated/doc diffs elsewhere in the shared
worktree predate and remain outside this planning task.

Each implementation phase runs exactly:

| Phase | Release build | One selected test project |
|---|---|---|
| 1 — Foundation | `dotnet build --configuration Release --verbosity quiet` | `Event.Architecture.Tests` |
| 2 — Shared contracts/core | same | `Event.Wire.Contracts.UnitTests` |
| 3 — Environment/offline workflows | same | `Event.Setup.Core.Tests` |
| 4 — CLI/TUI | same | `Event.SetupAssistant.Cli.Tests` |
| 5 — Shared Avalonia | same | `Event.SetupAssistant.Tests` |
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
- Exact Avalonia/Terminal.Gui/native/tooling graph: SA-120.
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

### Handoff — 2026-08-31 Europe/Brussels — Pre-SA-110 CTO Split Correction

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
