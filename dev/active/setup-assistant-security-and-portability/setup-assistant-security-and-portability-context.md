<!-- ABOUTME: Active working memory for the Setup Assistant security and portability workstream. -->
<!-- ABOUTME: Records review state, blockers, resume order, evidence, decisions, and validation handoffs. -->

# Setup Assistant Security And Portability — Context

Last Updated: 2026-08-30 Europe/Brussels

## Review State

- **Planning status:** Draft; awaiting user review.
- **I-VSD report:**
  [i-vsd-setup-assistant-security-and-portability.md](../../../islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md)
- **I-VSD reviewed input revision:**
  `sha256:8c86d6be6f612861bba9c4ea641a451722fe0d6b5feccad09ac310b5cdce1637`
- **I-VSD status / disposition:** `current` / `plan-aligned`.
- **CTO review:** Not reviewed.
- **User approval:** Awaiting approval for this exact workstream revision.

## SESSION PROGRESS (2026-08-30 Europe/Brussels)

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
- Recorded the user’s 2026-08-30 decision to close ConfigurationManifest for
  archival. Its current v1alpha2/schema/registry/import-preview behavior is the
  frozen Setup extraction baseline; its retired Phases 19–23 are not
  represented as implemented or inherited.
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
- Drafted the plan with 12 RFC 2119 requirements, adversarial scenarios, eight
  phases, exact phase gates, release strategy, risk register, and all 36 I-VSD
  mappings.
- Drafted the hot ledger with 28 implementation tasks and separate phase
  verification checkboxes.
- Created the sanitized clean-room evidence packet and bound review state to
  its SHA-256 revision.

### IN PROGRESS

- Awaiting user review of the implementation plan.

### NEXT

1. User corrects or approves the plan.
2. Obtain revision-bound Senior CTO review because the workstream is XL and
   security-sensitive.
3. Start `SA-110` with failing Setup architecture/security-boundary and
   frozen-baseline convergence tests.

### BLOCKERS

- **Approval blocker:** User approval and revision-bound CTO review are not yet
  recorded.
- **Release enablement gates:** Hosted browser secret mode, legal templates,
  package formats, support tiers, and signing claims remain disabled until
  their named independent evidence exists. These gates do not block no-secret
  core planning.

## Quick Resume

1. Read this context and
   `setup-assistant-security-and-portability-tasks.md`.
2. Confirm whether user and revision-bound CTO approval changed.
3. If approved and unblocked, read only plan Phase 1 plus Sections 4, 5.1,
   5.4, 5.7, and 5.9.
4. Start `SA-110`; do not scaffold packages or projects before its Red
   contracts pin the frozen ConfigurationManifest archive baseline.
5. Keep tasks current immediately; update this context only on a trigger.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Responsibility |
|---|---|---|---|
| `src/Event.Wire.Contracts/` | Existing, to extend | Inner shared contract | Versioned manifest/package codecs and constrained legal Markdown |
| `src/Event.Setup.Core/` | New | Headless core | Environment catalogue, dotenv, offline validation/diff/readiness, workflow states |
| `src/Event.SetupAssistant/` | New | Shared presentation | Avalonia views, view models, resources, accessibility/localization |
| `src/Event.SetupAssistant.Browser/` | New | Browser adapter | Static WASM startup, local download, CSP/capability and secret-state boundary |
| `src/Event.SetupAssistant.Desktop/` | New | Desktop adapter | Platform startup, native picker, Windows/Unix protected writes |
| `src/Event.SetupAssistant.Cli/` | New | CLI/TUI adapter | Stable commands, JSON schema, exit categories, Terminal.Gui |
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
10. Native Wayland, AppImage, Flatpak, global tool, live APIs, PWA, auto-update,
    plugins, mobile, and AI are outside the approved first workstream.

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

Planning is Markdown-only. No product build or implementation test was run or
claimed during plan authoring.

Planning verification completed on 2026-08-30:

- all required plan and I-VSD report headings are present;
- all four workstream Markdown files have two ABOUTME lines;
- all 36 findings and 36 mitigations map to scenarios/tasks;
- all plan task IDs converge with 28 open implementation tasks;
- every implementation checkbox states a verification assertion;
- eight phases each have one Release build and one selected project test gate;
- the plan contains no execution checkboxes;
- local Markdown links resolve;
- the clean-room evidence digest matches the review state;
- `git diff --check` passes for the workstream and I-VSD report.

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
| 8 — Release/skill | same | `Event.Architecture.Tests` |

Known repository baseline:

- The previous ConfigurationManifest session recorded a Release build with
  zero errors and unrelated analyzer warnings.
- ConfigurationManifest closure explicitly retired its remaining phase gates;
  that waiver is not a green test result and is not Setup implementation
  evidence.
- Do not rerun or reinterpret historical ConfigurationManifest results as
  Setup evidence.

## Current Known Risks / Unknowns

- Frozen ConfigurationManifest archive drift or capability overclaim: SA-110.
- Exact Avalonia/Terminal.Gui/native/tooling graph: SA-120.
- Environment catalogue/default/activation completeness: SA-310/SA-320.
- Browser exact-bundle network/storage and origin review: SA-610–SA-640.
- Desktop filesystem/ACL support matrix: SA-710–SA-730.
- Legal template/counsel approval: SA-540/SA-820.
- OS/RID/package support and signatures: SA-810/SA-820.
- Agent skill/CLI drift: SA-840.

## Handoff Notes

### Handoff — 2026-08-30 Europe/Brussels — ConfigurationManifest Archive Transfer

- **Current state:** The user closed ConfigurationManifest for archival. The
  Setup plan, tasks, context, I-VSD handoff, and clean-room evidence now pin
  the frozen implemented v1alpha2/schema/registry/import-preview baseline.
- **Next action:** User review, revision-bound Senior CTO review, then SA-110.
- **Blockers:** Setup user approval and CTO review only.
- **Modified files:** Setup plan/context/tasks/clean-room evidence and Setup
  I-VSD report; ConfigurationManifest plan/context/tasks/I-VSD contain the
  matching archive disposition.
- **Validation:** Evidence digest refreshed to
  `sha256:8c86d6be6f612861bba9c4ea641a451722fe0d6b5feccad09ac310b5cdce1637`;
  final link/state/whitespace checks remain.
- **Documentation impact:** No runtime or operator documentation changed.
- **Risks:** Archive closure is not proof that retired server atomic-apply,
  migration UI, managed-ownership, or direct-transfer capabilities exist.
- **Notes for next contributor/agent:** Do not reopen ConfigurationManifest
  tasks from the archived ledger. SA-110 must fail on frozen-baseline drift or
  capability overclaim before contract extraction.

### Handoff — 2026-08-30 Europe/Brussels

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
