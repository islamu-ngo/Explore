<!-- ABOUTME: Hot execution ledger for the Setup Assistant security and portability workstream. -->
<!-- ABOUTME: Sequences invariant-first implementation, phase gates, release evidence, and final changelog composition. -->

# Setup Assistant Security And Portability — Task Checklist

Last Updated: 2026-09-01 Europe/Brussels

## Status Summary

- **Overall status:** Successors A through C/Phase 8 were Green under the prior
  presentation disposition. The Project Steward has re-opened successor B for
  a Terminal.Gui-only replacement with no console fallback. Successor D
  D2-0b has a clean isolated 10/10 attributable API Red; corrected D2-1
  package-free strict Wire contracts and D2-2 Domain behavior are approved
  Green. Corrected D2-3 Application contracts are approved Green at 7/7, and
  D2-4 Persistence is approved Green at 5/5 provider models plus 6/6 real
  PostgreSQL invariants. D2-5 API is approved Green at 16/16 owned scenarios,
  a deterministic real-PostgreSQL issuance race, and a 100/100 weighted Tier 1
  vote. D2-6 SecretResolver authority mismatch is approved Green at 16/16
  focused class scenarios and a 100/100 weighted Tier 1 vote; no live-control
  capability is active. D2-7 selected-authority writes and D2-8 public/generated
  contract closure are approved Green at weighted 100/100 votes. Browser/desktop secret
  surfaces remain `ApprovedDisabled`; the package-free Unix protected writer
  is active.
- **Completed:** 35/52 implementation tasks; phase verification is tracked
  separately.
- **Current priority:** Resume Phase 9 D2-11 with every SetupLive capability
  still false until its own closure gates pass.
- **Next recommended slice:** Re-run the D2-11 closure checklist against the
  current Green Terminal.Gui boundary and unchanged fail-closed capability
  manifest.
- **Upstream disposition:** ConfigurationManifest is active. SA-110 consumes
  only its frozen v1alpha2/schema/registry/import-preview/no-secret contract;
  upstream server work is not Setup implementation evidence.
- **Resolved graphs:** The isolated CommunityToolkit.Mvvm `8.4.2` probe and the
  Microsoft DI `10.0.10` plus Abstractions `10.0.10` probe passed, and the
  post-probe dependency/IP, security, and accessibility reviews each issued
  `Approve` for the Toolkit shared-presentation role and the DI executable-root
  role. Evidence:
  [B1 probe evidence](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-b1-probe-evidence.md),
  `sha256:424ef6b6e3b7b7700b4d26b11149545b0f97fe0165a22890d35a67f9e8e14be8`.
- **Resolved Terminal decision:** Avalonia remains `ApprovedDisabled`.
  Terminal.Gui is required through the separately named, minimally patched
  package authorized in
  [setup-assistant-terminal-gui-steward-approval.md](setup-assistant-terminal-gui-steward-approval.md).
  The prior BCL/custom terminal path is removal-only and cannot remain as a
  fallback.
- **Mandatory build environment:** Every future restore, build, test, or
  publish evidence run sets `DOTNET_CLI_TELEMETRY_OPTOUT=1`,
  `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1`,
  `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1`, and `DOTNET_NOLOGO=1` with
  isolated package caches. The first traced no-restore build without them
  showed .NET SDK workload-advertising egress and was failed for that reason.
- **Historical workstream:** `dev/active/setup-assistant-presentation-targets/`
  and its B0 I-VSD/CTO/binding artifacts are superseded and non-executable.
- **Plan:**
  [setup-assistant-security-and-portability-plan.md](setup-assistant-security-and-portability-plan.md)
- **Context:**
  [setup-assistant-security-and-portability-context.md](setup-assistant-security-and-portability-context.md)
- **Clean-room evidence:**
  [setup-assistant-security-and-portability-clean-room-evidence.md](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-clean-room-evidence.md)
- **Dependency evidence:**
  [setup-assistant-security-and-portability-dependency-evidence.md](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-dependency-evidence.md)
- **I-VSD report:**
  [i-vsd-setup-assistant-security-and-portability.md](../../../islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md)
- **I-VSD review:** Current Terminal.Gui-only Steward revalidation; the report
  binds this re-baselined execution ledger and preserves all accepted finding
  and mitigation IDs.
- **I-VSD status / disposition:** `current` / `plan-aligned`; all
  `IVSD-F001`–`IVSD-F046` and `IVSD-M001`–`IVSD-M046` mappings are preserved.
- **First CTO review:** [Split before approval](setup-assistant-security-and-portability-cto-review.md), bound to prior plan/tasks hashes.
- **Current correction review:**
  [Corrected D2-1 strict Wire Green approved](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-phase9-d2-1-corrected-cto-review.md),
  `sha256:cd9b41d98111914e4aba5a392ebe0fe9d981c52f4d2d376a75eae74ca68ca72c`.
- **Current D2-3 review:**
  [Corrected Application Green MAD review](../../../.omo/evidence/20260901-setup-assistant-security-and-portability/phase9-d2-3-mad-review.yaml).
- **Current D2-4 review:**
  [Persistence Green MAD review](../../../.omo/evidence/20260901-setup-assistant-security-and-portability/phase9-d2-4-green-mad-review.yaml).
- **Current D2-5 review:**
  [API Green MAD review](../../../.omo/evidence/20260901-setup-assistant-security-and-portability/phase9-d2-5-green-mad-review.yaml).
- **Current D2-6 review:**
  [SecretResolver Green MAD review](../../../.omo/evidence/20260901-setup-assistant-security-and-portability/phase9-d2-6-green-mad-review.yaml).
- **Current D2-9 review:**
  [Unix CLI protected-profile disposition MAD review](../../../.omo/evidence/20260901-setup-assistant-security-and-portability/phase9-d2-9-protected-profile-mad-review.yaml).
- **Current user approval:**
  [D2-1 through D2-11 staged product sequence](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-phase9-d2-product-approval.md),
  `sha256:a5d01cb1d91a071c7885316edb3ec27f244d8f36e44687ebe6d4344dbeb6b97e`.

## Resume From Here

1. Execute Phase 5R and keep the D2-11 capability manifest fail-closed.
2. D2-10 remains approved at a weighted 100/100 Tier 1 vote with 34/34 focused
   adapter tests; generated client methods remain the sole canonical route
   owner while HAL relation/method presence gates actions.
3. After the sole Terminal.Gui target is Green and independently reviewed,
   resume D2-11's canonical capability generation, docs, full relevant tests,
   Release build, and weighted review.

## Successor Ownership Ledger

This remains the sole checkbox ledger for the umbrella program. The historical
B0 ledger is non-executable and cannot complete or authorize an umbrella SA
checkbox.

| Successor | PR slices and SA ownership | State / entry gate |
|---|---|---|
| A foundation-offline | A1 SA-110-SA-130 and ten package-free project shells; A2 SA-210-SA-230; A3 SA-310-SA-340; A4 SA-410-SA-430 BCL CLI/terminal wizard | SA-110-SA-430 and Phase 1-4 verification Green |
| B presentation-targets | B1 SA-510/SA-515/SA-518 shared presentation; B2 SA-520 shared Avalonia; B3 SA-525R-SA-527 patched Terminal.Gui/sole target; B4 SA-530R accessibility/localization/security; B5 SA-540R file-based legal; B6 SA-610-SA-640 browser; B7 SA-710-SA-730 desktop | B0 superseded; prior Phases 5-7 are historical Green. Phase 5R is open. Toolkit shared presentation and Unix protected output remain active; DI remains executable-root-only; Avalonia/browser/desktop stay `ApprovedDisabled`; Terminal.Gui becomes active only after the exact internal package and sole target pass every Phase 5R gate |
| C composition-scale | C1 SA-810/SA-820; C2 SA-830 | SA-810-SA-830 and Phase 8 Green with bounded composition and measured-profile evidence |
| D live-control-plane | D1 SA-910 Red/server contracts; D2 SA-920/SA-930 server/generated contracts; D3 SA-1010 Red plus SA-1020/SA-1030 adapters/UI | D2-1 through D2-10 are approved; the separately compiled ephemeral authenticated outer adapter passes 34/34 and no capability is active while D2-11 closure runs |
| E application-data-migration | E1 SA-1110 privacy/tenant Red; E2 SA-1120; E3 SA-1130; E4 SA-1132 Setup UI activation | Inactive; D contracts plus fresh Tier 2 custody/erasure, Tier 1 tenant, I-VSD/CTO/user approval, and named privacy/provider evidence |
| F sovereign-payment-migration | F1 SA-1135 Worst Break Red/Tier 0 record; F2 SA-1140 Domain/Persistence/provider reconciliation; F3 SA-1145 API/HAL/Setup activation | Inactive and optional; D/E contracts plus fresh Tier 0/I-VSD/CTO/user and provider/legal/operator approvals; may resolve `ApprovedDisabled` |
| G release-and-agent-contract | G1 SA-1210/SA-1220 per subset; G2 SA-1240 after CLI schema; G3 SA-1250 reconciliation | Inactive; each owning successor green; evidence describes only the selected subset |

One-way dependencies are A -> B and A -> C; D consumes selected stable B/C
contracts; E depends on D; F depends on D/E contracts and is independently
optional; G runs per shippable subset and at final reconciliation.
No successor inherits umbrella or predecessor approval. Each must receive the
current I-VSD plus fresh tier-appropriate intake, CTO review, exact-revision
user approval, and named evidence before any owned checkbox starts.

## Implementation Maintenance Rules

1. This file is the sole hot execution ledger. At initial implementation start,
   read the full triad once; on cold resume read context/tasks first and only
   the current plan phase or changed decision.
2. Do not reread unchanged artifacts after each task. Keep a
   `path + heading/symbol + revision` ledger.
3. Mark a substantial task `IN PROGRESS` only when it will span meaningful
   work or a handoff. Check it immediately after its acceptance criteria pass;
   reconcile small related tasks no later than phase end.
4. Keep implementation and phase-verification checkboxes separate. A phase is
   complete only after every task and its named Release build/test gate pass.
5. High-criticality Red tasks run the named focused selector and record the
   intended failure before production code. Standard adapters are implemented
   directly and asserted through public contracts.
6. Active iteration uses one focused TUnit class selector. Full project tests
   run only once at phase exit.
7. Tests assert public codecs, workflow results, command JSON, file state, and
   machine manifests. No mock call-count mirrors, framework-mechanics tests,
   raw source/CSS/prose pinning, fixed sleeps, timing polls, skips, or weakened
   assertions.
8. Do not start the application, browser, Docker, Aspire, Playwright, Chrome
   DevTools, or live services for workstream verification.
9. `Event.Wire.Contracts` and `Event.Setup.Core` remain network-, persistence-,
   provider-, AI-, telemetry-, and server-layer-free.
10. Secret values never enter portable artifacts, tests, source, arguments,
    environment, captured stdin, machine JSON, logs, diagnostics, support
    evidence, browser stores, accessibility text, or remote requests.
11. Regenerate schemas, environment templates, locks, SBOMs, manifests, and
    release artifacts from source. Never hand-edit generated output.
12. Do not add a dependency before exact target-graph license/vulnerability
    evidence. An unknown or incompatible component blocks the target.
13. No backward-compatibility namespaces, aliases, type forwards, duplicate
    codecs, route aliases, or stale command shims.
14. Update context after a phase, material decision, blocker, validation
    failure, scope discovery, or handoff. Update the plan only for strategy
    changes.
15. Before pause, compaction, transfer, or PR, reconcile affected tasks and
    name unrelated shared-worktree changes in context.

## Phase 1: Contract Freeze, Dependency Gate, And Project Boundaries

Plan reference: Phase 1 and Sections 4, 5.1, 5.4, 5.7, and 5.9.

- [x] **SA-110 — Author failing Setup architecture and security-boundary contracts and verify `SetupAssistantArchitectureTests` fails only because the new projects and ratchets do not exist**
  - **Files:** new
    `tests/Event.Architecture.Tests/SetupAssistantArchitectureTests.cs`;
    existing `Explore.slnx`, project files, package policy, and configuration
    manifest contract locators.
  - **Acceptance:** Tests require the five planned Setup projects and focused
    tests; enforce the exact inward reference graph; reject references to
    Domain/Application/Persistence/Infrastructure/API/Blazor, networking,
    persistence, provider SDKs, telemetry, analytics, AI/model packages,
    professional/commercial Avalonia tooling, and production diagnostics;
    require browser secret capability disabled; require the archived
    ConfigurationManifest closure marker; and fail on any drift from the
    frozen v1alpha2/schema/registry/import-preview baseline.
  - **Focused Red selector:**
    `dotnet run --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupAssistantArchitectureTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** M
  - **Dependencies:** Satisfied for this recorded Red by the then-current/
    plan-aligned I-VSD and then-bound CTO/user approvals. They do not authorize
    further implementation after the SA-120 strategy change.
  - **Guidance:** criticality-guardrail, clean-architecture-rules,
    ip-clean-room, tests rule.

- [x] **SA-120 — Enforce the approved BCL-only successor-A graph, scaffold its ten project boundaries/locks/ratchets, and verify locked restore, vulnerability audit, license policy, and focused architecture contracts pass without blocked pins or exceptions**
  - **Files:** existing `Directory.Packages.props`,
    `Directory.Build.props`, `Explore.slnx`,
    `tests/Event.Architecture.Tests/SetupAssistantArchitectureTests.cs`; new
    five Setup source shells, five focused test shells, one
    `packages.lock.json` per project, the two generated SA-110 fail-closed
    ratchets, and the
    [dependency evidence](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-dependency-evidence.md).
  - **Acceptance:** Treat Terminal.Gui `2.4.17` and its complete 24-package
    graph as blocked because mandatory TextMateSharp.Grammars `2.0.4` lacks
    complete component provenance/notices. Treat Avalonia `12.1.1` Desktop and
    Browser runtime graphs as blocked; compile-only scaffolding conditionality,
    BuildServices telemetry opt-out, resolved ANGLE licensing, and signed
    integrity do not approve a package or target. Add no Terminal.Gui,
    Avalonia, replacement TUI/GUI, or exception pin/reference/lock entry in A.
    Select BCL plus package-free `Event.Wire.Contracts` for the product graph.
    Create all five source and five matching test projects, locks, the disabled
    browser capability ratchet, and frozen-contract ratchet. Keep
    `Event.SetupAssistant`, Browser, and Desktop package-free, disabled,
    non-shipped contract shells: they are not functional UI, runtime targets,
    or support evidence. Test projects use only repository-approved existing
    test infrastructure. Locked restore, point-in-time vulnerability audit,
    license policy, and `SetupAssistantArchitectureTests` must pass without an
    exception; do not mark complete until those implementation artifacts and
    checks exist.
  - **Effort:** XL
  - **Dependencies:** SA-110; planning-mode I-VSD revalidation, fresh CTO
    review, and exact-revision user approval for this changed strategy.
  - **Guidance:** ip-clean-room, agentic-research, CI/CD governance.

- [x] **SA-130 — Wire Setup source, lock, and generated-output governance into CI and verify source is tracked while only build/publish/release output is ignored**
  - **Files:** existing `.gitignore`, `.github/workflows/test.yml`,
    `.ci/scripts/validate-dependency-license-policy.cs`,
    `docs/CI_CD_GOVERNANCE.md`; new Setup architecture checks and clean-room
    provenance records.
  - **Acceptance:** Path discovery includes all new projects and lock files;
    browser source/CSP/tests remain tracked; `bin/`, `obj/`, publish
    `wwwroot`, package staging, and release artifacts remain generated;
    workflow permissions stay read-only for untrusted PRs; no write token or
    signing secret reaches PR code.
  - **Effort:** L
  - **Dependencies:** SA-120.
  - **Guidance:** `ci-cd-change` intent, ip-clean-room.

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 2: Shared Wire Contracts And Headless Core

Plan reference: Phase 2 and Sections 3.1, 3.2, 3.8, 5.1, and 5.2.

- [x] **SA-210 — Author failing v1alpha2 and legal-codec extraction invariants and verify `SetupContractExtractionTests` detects every byte, diagnostic, limit, schema, and collection-ownership drift**
  - **Files:** existing/new
    `tests/Event.Wire.Contracts.UnitTests/ConfigurationPortability/**`,
    existing Application/Domain/Architecture ConfigurationManifest and legal
    Markdown tests.
  - **Acceptance:** Red tests pin consumed contract bytes and schema identity,
    strict unknown/duplicate/member handling, exact content limits,
    deterministic serialization, legal HTML/diagnostic behavior, immutable
    collection snapshots, and absence of secret/PII authority. Failures name
    only the old contract owners.
  - **Focused Red selector:**
    `dotnet run --project tests/Event.Wire.Contracts.UnitTests/Event.Wire.Contracts.UnitTests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupContractExtractionTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** L
  - **Dependencies:** Phase 1.
  - **Guidance:** record contracts, criticality-guardrail, tests rule.

- [x] **SA-220 — Move v1alpha2 wire contracts and constrained legal Markdown into `Event.Wire.Contracts` and verify old owners are deleted with all schema/server callers migrated**
  - **Files:** existing `src/Event.Wire.Contracts/**`,
    `src/Explore.Domain/LegalMarkdownContract.cs`,
    `src/Explore.Application/Features/ConfigurationManifest/Contracts/**`,
    serialization/catalog/schema generator projects and schemas.
  - **Acceptance:** One package-free owner produces byte-equivalent artifacts;
    Domain/Application reference the shared assembly only where needed; schema
    generation no longer references all Application for wire types; no alias,
    duplicate DTO, type forward, or compatibility serializer exists; SA-210
    selector is Green.
  - **Effort:** XL
  - **Dependencies:** SA-210.
  - **Guidance:** clean-architecture-rules, application/domain rules,
    ip-clean-room.

- [x] **SA-230 — Implement package-free `Event.Setup.Core` workflow contracts and verify `SetupCoreArchitectureTests` proves pure deterministic behavior with no I/O or ambient authority**
  - **Files:** new `src/Event.Setup.Core/**`,
    `tests/Event.Setup.Core.Tests/**`; existing solution and architecture docs.
  - **Acceptance:** Core references only BCL and `Event.Wire.Contracts`; owns
    immutable setup profiles, selections, diagnostics, readiness, artifact
    digests, diff/coverage inputs, and state transitions; snapshots collections;
    uses injected randomness/clock only where required; has no filesystem,
    network, host configuration, environment reads, persistence, provider,
    telemetry, localization prose authority, or live-target claims.
  - **Focused selector:**
    `dotnet run --project tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupCoreArchitectureTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** L
  - **Dependencies:** SA-220.
  - **Guidance:** clean-architecture-rules, record contracts.

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Wire.Contracts.UnitTests/Event.Wire.Contracts.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Environment Catalogue And Offline Workflows

Plan reference: Phase 3 and Sections 3.1–3.4, 3.8, 5.2, and 5.3.

- [x] **SA-310 — Author failing catalogue and dotenv Invariant-Breakers and verify `EnvironmentCatalogueInvariantTests` rejects cycles, drift, fake secrets, irrelevant keys, defaults, duplicates, injection syntax, and value-bearing diagnostics**
  - **Files:** new
    `tests/Event.Setup.Core.Tests/Environment/EnvironmentCatalogueInvariantTests.cs`,
    `DotenvContractTests.cs`; existing `.env.example`, Compose, secret registry
    as public parity seams.
  - **Acceptance:** Red scenarios cover closed activation predicates,
    topology/capability relevance, required/optional/defaulted classification,
    secret-binding parity, deterministic ordering, explicit newline/Unicode
    dialect, round-trip parse/render, empty relevant placeholders, readiness,
    and complete value-safe diagnostic scans.
  - **Focused Red selector:**
    `dotnet run --project tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*EnvironmentCatalogueInvariantTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** L
  - **Dependencies:** SA-230.
  - **Guidance:** criticality-guardrail, tests rule.

- [x] **SA-320 — Implement the canonical environment catalogue and generator/check tool and verify `.env.example`, Compose, startup, secret registry, and documentation anchors converge without scraping prose**
  - **Files:** new `src/Event.Setup.Core/Environment/**`,
    `eng/setup-assistant/**`; existing `.env.example`, `docker-compose.yml`,
    `SecretDefinitionRegistry.cs`, configuration docs, solution/CI.
  - **Acceptance:** One closed acyclic graph owns key metadata, safe validators,
    relevance/defaults, sensitivity, generation policy, restart behavior, and
    docs anchors; generator writes deterministic template sections and
    machine coverage; check mode reports drift without mutation; every secret
    environment key maps to binding authority without exposing values or
    Infisical coordinates.
  - **Effort:** XL
  - **Dependencies:** SA-310.
  - **Guidance:** clean-architecture-rules, ip-clean-room.

- [x] **SA-330 — Implement the explicit dotenv codec, readiness, and approved local secret generation and verify no-secret and secret outputs remain separate, deterministic, and value-safe**
  - **Files:** new `src/Event.Setup.Core/Dotenv/**`,
    `src/Event.Setup.Core/Secrets/**`, focused Core tests.
  - **Acceptance:** Parser/renderer handles the declared syntax or fails
    closed; no shell execution/interpolation occurs; no-secret output uses
    empty relevant placeholders and `Incomplete`; secret mode accepts only
    local human values or approved cryptographic generators with independent
    provider evidence; generated values are never reused; diagnostics,
    `ToString`, and readiness expose no values.
  - **Focused selector:**
    `dotnet run --project tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*DotenvContractTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** SA-320.
  - **Guidance:** criticality-guardrail, secret isolation.

- [x] **SA-340 — Implement offline manifest, tenant-package, legal, diff, coverage, and readiness workflows and verify `OfflinePortabilityWorkflowTests` produces stable non-secret artifacts without live-target authority**
  - **Files:** new `src/Event.Setup.Core/Portability/**`,
    `src/Event.Setup.Core/Legal/**`, focused Core tests.
  - **Acceptance:** Create/open/edit/validate/format/diff/coverage/export support
    only registered sections and stable identities; legal drafts preserve typed
    role/provenance/locales and cannot migrate publication/acceptance evidence;
    artifacts obey canonical limits and never combine with dotenv; all target
    mappings and live apply remain server-side.
  - **Focused selector:**
    `dotnet run --project tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*OfflinePortabilityWorkflowTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** SA-320, SA-330.
  - **Guidance:** criticality-guardrail, i-vsd.

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj --configuration Release --verbosity quiet`

## Phase 4: Versioned CLI And BCL Interactive Terminal Wizard

Plan reference: Phase 4 and Sections 3.4, 3.9, 5.2, and 5.7.

- [x] **SA-410 — Author failing command, machine-schema, and terminal-secret contracts and verify `SetupCliContractTests` fails on the missing executable while pinning help, dry-run, JSON, exits, TTY, and leakage boundaries**
  - **Files:** new `tests/Event.SetupAssistant.Cli.Tests/**`,
    `schemas/event-setup-command-v1.schema.json` generated expectation.
  - **Acceptance:** Red tests specify command families, one machine JSON object,
    stable codes/paths/digests/sensitivity/coverage/readiness, bounded text,
    explicit input/output, no control sequences/localized authority, and
    rejection of secrets in arguments, options, environment, captured stdin,
    stdout, or non-TTY mode.
  - **Focused Red selector:**
    `dotnet run --project tests/Event.SetupAssistant.Cli.Tests/Event.SetupAssistant.Cli.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupCliContractTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** L
  - **Dependencies:** Phase 3.
  - **Guidance:** criticality-guardrail, tests rule.

- [x] **SA-420 — Implement deterministic `event-setup` commands and verify machine JSON, text output, exit categories, digests, help, dry-run, and no-secret writes satisfy the public command contract**
  - **Files:** new `src/Event.SetupAssistant.Cli/Commands/**`,
    serialization context, generated command schema, tests.
  - **Acceptance:** Catalogue, manifest, tenant-package, env, legal, doctor,
    and `tui` command families use Setup Core; command parsing and the bounded
    interactive wizard are repository-native and BCL-only; machine output is
    one versioned object; write operations require
    explicit paths/approval semantics; machine mode cannot enter secret mode;
    unknown/removed commands fail without aliases.
  - **Effort:** XL
  - **Dependencies:** SA-410.
  - **Guidance:** clean architecture, record contracts.

- [x] **SA-430 — Implement repository-native BCL human terminal workflows and verify `SetupTerminalSecretBoundaryTests` proves TTY-only masked entry, protected output, state clearing, and byte parity with Core**
  - **Files:** new `src/Event.SetupAssistant.Cli/Tui/**`, focused CLI tests; no
    external TUI package.
  - **Acceptance:** The bounded linear terminal wizard supports the same
    workspaces and Core outputs; secret mode requires an interactive TTY,
    rejects redirection/captured input, suppresses and restores echo on every
    exit, disables stdout/stderr artifact output, retains no history/autosave/
    clipboard by default, and clears on cancel, completion, suspension, signal,
    resize failure, or navigation. Product-owned adversarial tests cover
    TTY/redirection, echo restoration, signals, resize, scrollback leakage,
    keyboard completion, non-color status, bounded output, and truthful
    accessibility limitations. Agent automation remains machine CLI only.
  - **Focused selector:**
    `dotnet run --project tests/Event.SetupAssistant.Cli.Tests/Event.SetupAssistant.Cli.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupTerminalSecretBoundaryTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** SA-420.
  - **Guidance:** accessibility, criticality-guardrail.

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.SetupAssistant.Cli.Tests/Event.SetupAssistant.Cli.Tests.csproj --configuration Release --verbosity quiet`

## Phase 5: Shared MVVM Workspaces And Human Presentation Adapters

Plan reference: Phase 5 and Sections 3.1, 3.4, 3.8, 3.10, 3.16, 5.4, and 5.7.

- [x] **SA-510 — Bind and evaluate independent B1 package graphs through the restricted non-shipping probe protocol and record an exact verdict for every presentation decision unit**
  - **Files:** B1 binding/intake/dependency evidence; isolated
    `eng/setup-assistant/probes/**`; machine-consumed adapter dispositions.
  - **Acceptance:** B0 is superseded. CommunityToolkit/DI, shared Avalonia,
    Avalonia Browser, Avalonia Desktop, and Terminal.Gui are independently
    bound and reviewed through plan Section 5.4.1. Every node records exact
    identity/role/lock/hash/signature/vulnerability/license/NOTICE/SBOM/
    telemetry/publish evidence. Each candidate receives `Approve`,
    `ApprovedDisabled`, `Reject`, or `NotSelected`; only approved adapter
    manifests use `Active` or `ApprovedDisabled`. Product projects, central
    pins, locks, generated capabilities, support, and shipping flags remain
    unchanged and false throughout probing.
  - **Effort:** L
  - **Dependencies:** Phases 3 and 4; corrected B1 Tier 1 intake, I-VSD,
    dependency, security, accessibility, CTO, and exact-revision user approval
    for the probe only.
  - **Guidance:** criticality-guardrail, accessibility, ip-clean-room,
    agentic-research.
  - **Result:** Complete. The exact isolated Toolkit `8.4.2` and Microsoft DI
    `10.0.10` plus Abstractions `10.0.10` probes passed, and the post-probe
    dependency/IP, security, and accessibility reviews each returned `Approve`
    for the Toolkit shared-presentation role and the DI executable-root role.
    Avalonia shared/browser/desktop `12.1.1` and Terminal.Gui `2.4.17` stay
    `ApprovedDisabled`, absent, and unresolvable. Corrected builds under
    `DOTNET_CLI_TELEMETRY_OPTOUT=1`, `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1`,
    `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1`, and `DOTNET_NOLOGO=1` with
    isolated package caches recorded zero `AF_INET`/`AF_INET6`
    `connect`/`sendto`; the first traced no-restore run without that
    environment showed SDK workload-advertising egress and was failed. Product
    projects, product/test locks, central pins, the solution, shipping CI,
    generated capabilities, and support/release/shipping flags stayed
    byte-identical and false throughout. Evidence:
    [B1 probe evidence](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-b1-probe-evidence.md),
    `sha256:424ef6b6e3b7b7700b4d26b11149545b0f97fe0165a22890d35a67f9e8e14be8`.

- [x] **SA-515 — Author failing B1 presentation Invariant-Breakers and verify `SetupPresentationModelTests` fails only for absent approved shared owners**
  - **Files:** replace obsolete
    `tests/Event.SetupAssistant.Tests/SetupAssistantWorkspace*`; add focused
    public-seam presentation tests.
  - **Acceptance:** Tests exercise per-session messenger identity/no crosstalk,
    recipient activation/deactivation, duplicate delivery, monotonic generation
    fencing/exhaustion, cancellation and disposal races through exact signals,
    single-settlement operation identity, immutable value-free messages,
    bounded mutable public edit state, direct immutable Core result/byte
    projection, target-owned secret-state exclusion, and disabled-adapter
    non-resolution. They contain no duplicated Core transition table, type-name
    inventory, synthetic framework proof, silent early return, fixed sleep,
    timing poll, secret value, or mock-mirroring verifier.
  - **Focused Red selector:**
    `dotnet run --project tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupPresentationModelTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** L
  - **Dependencies:** SA-510 shared CommunityToolkit graph approval.
  - **Historical evidence:** The earlier 8-test 5/3 and 7/1 observations belong
    to the superseded B0 contract and are not B1 Red evidence.
  - **Result:** B1 Red complete. The obsolete untracked B0
    `SetupAssistantWorkspace*` files were removed and replaced with
    `SetupPresentationModelContract.cs` and `SetupPresentationModelTests.cs`.
    Ten focused tests compile and all ten fail only with
    `missing-approved-owner:ISLAMU.Event.SetupAssistant.Presentation.SetupPresentationSession`.
    No silent early return, synthetic transition table, class-name inventory,
    fixed sleep, timing poll, secret fixture, or product/package reference was
    introduced. The only emitted warning is the pre-existing CA1716 namespace
    warning from the test `Program.cs`.
  - **Guidance:** criticality-guardrail, accessibility, tests rule.

- [x] **SA-518 — Implement the approved framework-neutral CommunityToolkit presentation model and verify generated state/commands, session messaging, Core parity, and cancellation fencing**
  - **State:** Not started. Correction Red and review rebinding must complete
    before any Green product edit.
  - **Correction-Red owned paths:**
    `tests/Event.SetupAssistant.Tests/SetupPresentationModelContract.cs`,
    `tests/Event.SetupAssistant.Tests/SetupPresentationModelTests.cs`,
    `tests/Event.Architecture.Tests/SetupAssistantArchitectureTests.cs`, and
    this SA-518 slice in
    `dev/active/setup-assistant-security-and-portability/setup-assistant-security-and-portability-tasks.md`.
  - **Correction-Red sequence and evidence:** Keep
    `src/Event.SetupAssistant/Event.SetupAssistant.csproj`,
    `src/Event.SetupAssistant/packages.lock.json`, and
    `Directory.Packages.props` unchanged. Replace the common constructor
    short-circuit with owner-local reflection, then compile the two changed
    test projects before recording Red. The focused presentation selector
    compiled and reported **18 total / 18 failed / 0 passed / 0 skipped** only
    for absent product owners: `SetupPresentationSession` (1),
    `SetupWorkspaceId` (1), `SetupOperationGeneration` (1),
    `SetupPresentationOutcome` (1), `SetupPresentationWorkspace` (6),
    `SetupOperationSettledMessage` (2),
    `ISetupOperationGenerationAllocator` (2), and
    `SetupOperationInvalidatedEventArgs` (4). The corrected focused
    architecture selector reported **14 total / 1 failed / 13 passed / 0
    skipped**; the sole intentional failure code is
    `SA518-GRAPH-RATCHET`, covering the absent direct Toolkit reference,
    central pin, exact lock node, and compiled Core/Toolkit closure. One prior
    architecture invocation was invalid because the synthetic unsafe-JSON
    fixture had one extra closing brace; that test-support defect was corrected
    and its one-test selector passed before the recorded 14-test Red. No
    product Green owner exists yet, and SA-518 remains unchecked.
  - **Green owned paths:** exactly `Directory.Packages.props`,
    `src/Event.SetupAssistant/Event.SetupAssistant.csproj`,
    `src/Event.SetupAssistant/packages.lock.json`,
    `src/Event.SetupAssistant/Presentation/SetupOperationGeneration.cs`,
    `src/Event.SetupAssistant/Presentation/SetupWorkspaceId.cs`,
    `src/Event.SetupAssistant/Presentation/SetupPresentationContracts.cs`,
    `src/Event.SetupAssistant/Presentation/SetupPresentationSession.cs`,
    `src/Event.SetupAssistant/Presentation/SetupPresentationWorkspace.cs`,
    the three correction-Red test files named above, and this task ledger.
    Any additional product or support file requires a reviewed SA-518 ledger
    amendment before creation. SA-520, SA-525, accessibility, legal, target,
    capability, CI, solution, release, and shipping paths are excluded.
  - **Green acceptance:** Add the exact central
    `CommunityToolkit.Mvvm` `8.4.2` pin, the sole direct product package
    reference, and a force-evaluated net10 lock containing only Core/Wire
    project nodes plus the direct Toolkit node with no transitive package.
    Implement generated observable state and async execute/cancel commands over
    exact immutable Core result and `ReadOnlyMemory<byte>` identities. One
    injected messenger belongs to each session; typed generations and bounded
    workspace identities cross every public seam. Deterministic invalidation
    precedes cancellation on replacement, cancel, deactivate, and dispose;
    duplicate settlement, stale completion, exhaustion, duplicate/decreasing
    allocation, wrap, reseed, and new epoch all fail closed. The shared owner
    exposes only target-agnostic `NotEvaluated` status and contains no adapter
    registry, DI/Hosting, target UI, I/O, network, telemetry, provider,
    persistence, serializer, service locator, default messenger/Ioc, secret,
    or duplicated Core authority.
  - **Mandatory safe CLI environment:** Before every restore, build, test, or
    publish command, export
    `DOTNET_CLI_TELEMETRY_OPTOUT=1`,
    `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1`,
    `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1`, `DOTNET_NOLOGO=1`, and
    `NUGET_PACKAGES=/tmp/islamu-sa518-packages`. Do not start an application,
    browser, container, live service, or publisher.
  - **Green verification sequence:** Run
    `dotnet restore src/Event.SetupAssistant/Event.SetupAssistant.csproj --force-evaluate`,
    then the same restore with `--locked-mode`; run the focused presentation
    selector
    `dotnet run --project tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupPresentationModelTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`;
    run the focused architecture selector
    `dotnet run --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupAssistantArchitectureTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`;
    then run one `dotnet build --configuration Release --verbosity quiet`.
    All selected tests and the Release build must be Green in one execution;
    warnings or failures introduced by SA-518 belong to SA-518 and block its
    commit. Pre-existing unrelated failures are recorded verbatim and do not
    permit weakening, retrying, or skipping an owned gate.
  - **Immediate planned commit contract:** After all Green gates pass, inspect
    and load the repository `conventional-commit` skill in the same
    implementation session, then inspect
    `git status --short` and `git diff --cached --name-only`; stage only the
    exact Green owned paths with `git add -- <explicit paths>`. If unrelated
    index entries exist, preserve them and use `git commit --only -- <exact
    owned paths>`. Commit immediately with title
    `feat(self-hosting): establish isolated setup presentation state` and
    description `Add a framework-neutral generated MVVM session that projects exact Setup Core outcomes while fencing cancellation, stale completion, and generation exhaustion without activating a presentation target.`
    This is a non-shipping B1 internal activation, so use
    `Changelog: skip` and
    `Changelog-Reason: non-shipping isolated setup presentation foundation with all human targets disabled`.
    No release change fragment or `Change-Id` is required because this slice
    activates no shipped target, external authority, migration, or release;
    no additional trailer is required. Capture
    `sa518_commit=$(git rev-parse HEAD)` and verify
    `git diff-tree --no-commit-id --name-only -r "$sa518_commit"` matches the
    exact staged owned-path list before proceeding to SA-520 or SA-525. Reuse
    this planning-authored title, description, and changelog treatment when
    they remain truthful. A material divergence may override them only after
    this task ledger records the reason, replacement title, description,
    changelog treatment, trailers, owned paths, and verification contract;
    stylistic preference or implementation-time convenience is not an
    override condition.
  - **Effort:** XL
  - **Dependencies:** corrected intentional Red, refreshed exact binding,
    planning-mode I-VSD revalidation, fresh CTO verdict, and exact-revision
    user approval; SA-515 alone no longer opens Green.
  - **Guidance:** accessibility, clean-architecture-rules, ip-clean-room.
  - **Result:** Green. `Event.SetupAssistant` now references only Setup Core and
    CommunityToolkit.Mvvm `8.4.2`; its lock contains the exact approved direct
    node and content hash with no transitive package. Five presentation files
    implement bounded typed workspace identities, monotonic generation,
    session-injected messaging, immutable value-free lifecycle contracts,
    generated observable properties and async commands, exact Core result/byte
    projection, invalidation-before-cancellation, stale/duplicate settlement
    fencing, and fail-closed exhaustion. Focused presentation tests passed
    18/18 and focused Setup architecture tests passed 14/14. The Release build
    passed with zero errors; its 11,230 warnings are the pre-existing
    repository analyzer inventory. Product inventory contains only Toolkit
    `8.4.2`; vulnerability and deprecation audits are empty; the dependency
    license policy passed 655 unique package/version pairs.

- [x] **SA-520 — Resolve the shared Avalonia view slice as Active or ApprovedDisabled and verify only an Active slice satisfies `SetupAvaloniaBindingContractTests`**
  - **Files:** candidate `src/Event.SetupAssistant.Avalonia/**`; separate
    `tests/Event.SetupAssistant.Avalonia.Tests/**`; exact project/lock/
    dependency ratchets and adapter disposition.
  - **Acceptance:** If approved, every AXAML view is a typed `UserControl` with
    compiled bindings and `x:DataType`; selectors/classes/pseudo-classes own
    visual state; code-behind owns no business logic; target-service contracts
    express intents only; no secret value binding, lifetime root, protected
    writer, DI service location, or duplicated Core authority exists. If the
    graph remains blocked, record `ApprovedDisabled`, leave the project
    nonfunctional/unresolvable, and do not restore its blocked test project.
  - **Focused selector when Active:**
    `dotnet run --project tests/Event.SetupAssistant.Avalonia.Tests/Event.SetupAssistant.Avalonia.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupAvaloniaBindingContractTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** SA-518 and an independently approved Avalonia shared-view
    graph; otherwise an approved disabled disposition.
  - **Guidance:** accessibility, clean-architecture-rules, ip-clean-room.
  - **Result:** `ApprovedDisabled`. Avalonia `12.1.1` shared, Browser, and
    Desktop graphs remain absent because telemetry, remote-protocol,
    native/RID, publish, notice/provenance, and rendered-accessibility closure
    is incomplete. No Avalonia project, package, lock node, AXAML view, target
    resolution, support claim, or focused runtime test project exists.
    `DisabledPresentationTargetsMustRemainMachineDisabledAndGraphAbsent`
    verifies evaluated target flags and graph absence without restoring a
    blocked target.

- [x] **SA-525 — Resolve the no-secret Terminal.Gui slice as Active or ApprovedDisabled and verify only an Active slice satisfies `SetupTerminalGuiAdapterTests`**
  - **Files:** candidate `src/Event.SetupAssistant.Terminal/**`; separate
    `tests/Event.SetupAssistant.Terminal.Tests/**`; exact project/lock/
    dependency ratchets and adapter disposition.
  - **Acceptance:** If approved, one bounded disposable adapter maps
    Terminal.Gui events/keys to shared commands, command/property/collection
    changes to controls, and lifecycle/focus to activation/cancellation.
    Teardown retains no handlers or recipients. The target has no secret
    control, buffer, command, message, generator, clipboard/history path, or
    protected writer and cannot invoke Browser/Desktop secret sessions. If the
    graph remains blocked, record `ApprovedDisabled`; the Green BCL wizard
    remains the sole terminal secret path and fallback.
  - **Focused selector when Active:**
    `dotnet run --project tests/Event.SetupAssistant.Terminal.Tests/Event.SetupAssistant.Terminal.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupTerminalGuiAdapterTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** SA-518 and an independently approved Terminal.Gui graph;
    otherwise an approved disabled disposition.
  - **Guidance:** accessibility, clean-architecture-rules, ip-clean-room.
  - **Result:** `ApprovedDisabled`. Terminal.Gui `2.4.17` remains absent
    because its mandatory grammar graph lacks complete component provenance
    and notices. No Terminal project, package, lock node, adapter, secret
    surface, or focused runtime test project exists. The Green BCL terminal
    wizard remains the sole terminal human/secret path, and the architecture
    disposition verifier proves Terminal.Gui cannot be resolved.

- [x] **SA-530 — Implement bundled localization, RTL, keyboard, focus, semantic automation, and error-announcement contracts and verify `SetupAccessibilityContractTests` exposes no secret value or unsupported parity claim**
  - **Files:** new selected-framework resources/styles/accessibility services
    and tests; existing `docs/ACCESSIBILITY.md`, `docs/LOCALIZATION.md`.
  - **Acceptance:** Native controls and automation peers expose stable
    names/roles/states; tab/focus/order/reflow/contrast/non-color/reduced-motion
    behavior is explicit; errors associate and summarize once; all security
    consequences are bundled/localized before secret mode; logical layout
    supports RTL; browser/TUI limitations remain target-labelled; no secret is
    announced or used as automation metadata.
  - **Focused selector:**
    `dotnet run --project tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupAccessibilityContractTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** SA-518 and every Active target adapter (SA-520 and/or
    SA-525); a blocked target is labelled unavailable rather than blocking
    another approved target.
  - **Guidance:** accessibility.
  - **Result:** `ApprovedDisabled` for rendered target accessibility because
    SA-520 and SA-525 selected no Active adapter. The shared model reports only
    target-agnostic `NotEvaluated`, exposes no secret value/automation state,
    and makes no keyboard, focus, RTL, screen-reader, localization, contrast,
    or support claim. Existing Green BCL wizard accessibility limitations stay
    explicit. No framework resource, style, automation peer, or unsupported
    parity test was fabricated.

- [x] **SA-540 — Implement constrained legal-document editor and approved local-template boundary and verify `LegalAuthoringWorkspaceTests` rejects HTML, remote content, unresolved authority, and publication or acceptance mutation**
  - **Files:** new shared legal views/view models/template manifest; focused
    tests and clean-room template evidence.
  - **Acceptance:** Source, outline, sanitized preview, typed placeholders,
    locale comparison, bounded counts, diff, undo/redo, and readiness use the
    shared codec; no embedded browser, remote resource, network spellcheck,
    arbitrary plugin, macro, AI text, or auto-publication exists; templates are
    blank or approved immutable attributed local assets; target handoff remains
    a draft/new-version review.
  - **Focused selector:**
    `dotnet run --project tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*LegalAuthoringWorkspaceTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** SA-518, SA-530, and at least one Active target adapter.
  - **Guidance:** i-vsd, accessibility, ip-clean-room.
  - **Result:** `ApprovedDisabled` because there is no Active human
    presentation adapter. No legal view, remote preview, plugin, spellcheck,
    template pack, publication/acceptance mutation, or target handoff was
    created. The already-Green package-free Core legal draft/codec/readiness
    workflows remain the sole implemented legal authoring authority and no UI
    capability or template provenance is claimed.

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj --configuration Release --verbosity quiet`
- [x] Every Active Avalonia/Terminal target test project passes; each
  ApprovedDisabled target disposition verifier passes without restoring the
  blocked target.

## Phase 5R: Terminal.Gui-Only Replacement

This re-baseline supersedes the historical SA-525/SA-530/SA-540
`ApprovedDisabled` outcomes. It does not rewrite those historical results; it
adds the Project Steward-authorized replacement work that must complete before
Phase 9 D2-11 resumes.

- [x] **SA-525R — Freeze the downstream package contract and author failing dependency/provenance ratchets**
  - **Acceptance:** Bind official `v2.4.17` tag object
    `58f3af1a4afe5d2772be134b2299a0f78f35c93c`, commit
    `d0a0ed9b150d3fc8aacf4ab07b7f7d91264fe6d6`, internal identity
    `ISLAMU.Terminal.Gui`, version `2.4.17-islamu.1`, MIT license/notice
    retention, and the exact allowed patch scope. Architecture tests fail
    until the built package, lock graph, SBOM, and publish inventory contain no
    `TextMateSharp.Grammars`, unused `TextMateSharp`, official `Terminal.Gui`
    package identity, or unapproved editor/highlighting asset.
  - **Verification:** Run only the new Terminal downstream-package
    architecture test class with `--minimum-expected-tests 1`.

- [x] **SA-526 — Build the minimally patched internal package and prove its final closure**
  - **Acceptance:** A persistent repository tool fetches the exact official
    source revision, verifies the tag/commit, applies one minimal recorded
    patch series, retains upstream MIT/copyright/notices plus an ISLAMU
    modification notice, packs the distinct internal identity, and emits a
    deterministic package digest, dependency closure, CycloneDX SBOM, and
    vulnerability/license/provenance evidence. CI rebuilds the package and
    fails on artifact drift or any grammar/editor dependency re-entry.
  - **Constraint:** No unrelated upstream change, permanent fork branding,
    floating ref, source substitution, or manual binary edit is permitted.
  - **Verification:** The package architecture test class passes against the
    rebuilt artifact and the repository dependency-license audit passes.

- [x] **SA-527 — Replace the repository-native console TUI with the sole Terminal.Gui executable target**
  - **Acceptance:** Add `Event.SetupAssistant.Terminal` and its focused test
    project; map Terminal.Gui controls/events to the existing shared
    presentation state and Core workflows. Delete `ConsoleSetupTerminalDriver`
    and every CLI-owned interactive terminal driver/session/fallback path.
    `Event.SetupAssistant.Cli` becomes machine/noninteractive only and does not
    reference Terminal.Gui or advertise `event-setup tui`.
  - **Verification:** `SetupTerminalGuiAdapterTests` prove lifecycle, command,
    collection, focus, resize, cancellation, teardown, and byte-identical Core
    output; architecture tests prove the console fallback is absent.

- [x] **SA-530R — Make Terminal.Gui the only secret-capable terminal UI and prove the trust boundary**
  - **Acceptance:** Secret entry is target-owned, masked, interactive-TTY-only,
    bounded, value-free outside the owned mutable buffer, excluded from
    arguments/environment/captured stdin/stdout/logs/clipboard/history, and
    cleared on completion, cancellation, disposal, signal, and ambiguous
    failure. Protected output preserves the existing Unix transaction
    invariants. No hidden console path or second renderer exists.
  - **Verification:** Terminal security Invariant-Breakers and zero-secret
    diagnostic scans pass with deterministic coordination and no fixed sleeps.

- [x] **SA-540R — Close Terminal.Gui accessibility/localization and file-based legal-authoring behavior**
  - **Acceptance:** Prove keyboard/focus/resize/small-terminal/non-color/
    Unicode/RTL/localized error behavior to the support level Terminal.Gui can
    truthfully provide. Legal authoring remains file-based validate/render/diff
    over Core contracts; no replacement editor, syntax highlighter, grammar
    corpus, plugin, or remote content path is built.
  - **Verification:** Focused accessibility/localization/legal workspace tests
    pass and support documentation states evidenced limitations without parity
    overclaim.

### Phase 5R Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] Rebuild `ISLAMU.Terminal.Gui` from the pinned upstream commit and verify
  package digest, lock closure, SBOM, notices, and publish inventory.
- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.SetupAssistant.Terminal.Tests/Event.SetupAssistant.Terminal.Tests.csproj --configuration Release --verbosity quiet`
- [x] Weighted dependency/IP, Tier 1 security, architecture, accessibility,
  and quality review approves the exact package and target revision.

#### Planned Commit Contract

- **Default title:** `feat(self-hosting)!: make Terminal.Gui the sole setup terminal`
- **Default description:** `Ship the audited ISLAMU-patched Terminal.Gui 2.4.17 package and replace the custom console workflow with one Terminal.Gui presentation target while keeping machine CLI automation independent.`
- **Changelog treatment:** Change fragment `CHG-01M1FCCYVNKA3QG7E14T0BZ493`
- **Required trailers:** `Change-Id: CHG-01M1FCCYVNKA3QG7E14T0BZ493`; `BREAKING CHANGE: Remove the event-setup tui console workflow and require the Terminal.Gui setup executable for human terminal operation.`
- **Commit paths:** Phase 5R-owned package tooling/artifacts, Terminal target,
  CLI fallback removals, focused tests, central dependency/solution/CI files,
  change fragment, dependency/operator documentation, I-VSD report, and this
  workstream's plan/context/tasks/approval artifacts. Resolve the exact file
  list after the upstream patch audit and before staging; the shared dirty
  worktree currently forbids commit execution.
- **Message override:** Yes
- **Reason:** The Project Steward materially replaced the prior
  `ApprovedDisabled`/BCL-fallback phase outcome after its historical commit
  contract was authored. No commit is authorized while Phase 5R shares files
  with unrelated uncommitted work.

## Phase 6: Browser Locality And Secret Boundary

Plan reference: Phase 6 and Sections 3.4–3.6 and 5.5.

- [x] **SA-610 — Author browser secret Invariant-Breakers and verify `BrowserSecretBoundaryTests` fails on the missing target while pinning safe default, preload, request, storage, navigation, expiry, and capability behavior**
  - **Files:** new `tests/Event.SetupAssistant.Browser.Tests/**`.
  - **Acceptance:** Red tests cover fresh/refresh/back-forward/deep-link
    no-secret default, no URL/cookie/local/session/IndexedDB/cache/service-worker
    state, all resources preloaded, no request-capable adapter after transition,
    navigation clearing, idle expiry without fixed sleeps, value-free
    errors/support, and disabled public secret capability.
  - **Focused Red selector:**
    `dotnet run --project tests/Event.SetupAssistant.Browser.Tests/Event.SetupAssistant.Browser.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*BrowserSecretBoundaryTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** L
  - **Dependencies:** Phase 5.
  - **Guidance:** criticality-guardrail, accessibility.
  - **Result:** Intentional Red captured 10 total, 9 owner-local failures and
    one passing generated-capability assertion. Missing owners independently
    named fresh-session defaults, refresh/history, deep links, persistent
    storage, bundled preload, request revocation, navigation clearing, exact
    idle expiry, and value-free failure/support state. Because no browser graph
    is approved, the historical Red was then replaced by three passing
    `ApprovedDisabled` disposition tests rather than a conditional skip or
    dormant runtime assertion.

- [x] **SA-620 — Implement browser no-secret composition and local download and verify every new session remains useful without entering or persisting secrets**
  - **Files:** new `src/Event.SetupAssistant.Browser/**`, static entry, local
    download adapter, tests.
  - **Acceptance:** The approved static client target starts in no-secret mode,
    uses only bundled assets, generates relevant empty placeholders and
    non-secret
    manifests/packages, downloads locally, persists no profile by default, and
    clearly identifies incomplete secret completion and browser permission
    limitations.
  - **Effort:** L
  - **Dependencies:** SA-610.
  - **Guidance:** selected framework's official guidance, i-vsd.
  - **Result:** `ApprovedDisabled`. No approved Browser runtime exists, so no
    static entry point, download adapter, profile persistence, framework
    package, or runtime owner was created. The Green shared/Core/CLI workflows
    remain useful for no-secret composition, while browser target enablement
    stays false and the browser assembly exports no runtime type.

- [x] **SA-630 — Implement per-session browser trust and secret state machine and verify explicit opt-in, preload, network denial, clearing, and truthful origin copy remain bound to the exact release**
  - **Files:** new Browser trust/capability adapters, shared view transitions,
    tests.
  - **Acceptance:** Interstitial shows official origin, version/digest/source,
    origin trust, browser/device/extension limits, desktop alternative, and
    download permissions; no URL/storage preference activates it; entering
    secret mode disables external navigation and request capability; cancel,
    download, expiry, mode change, and navigation clear state; no deterministic
    memory-erasure or “ISLAMU cannot access” claim exists.
  - **Effort:** XL
  - **Dependencies:** SA-620.
  - **Guidance:** criticality-guardrail, i-vsd.
  - **Result:** `ApprovedDisabled`. Secret entry remains false in the generated
    capability; no trust interstitial, secret buffer, request boundary,
    navigation state, expiry state, origin copy, storage, or browser session
    owner exists. No claim that the hosted origin cannot access secrets is
    made.

- [x] **SA-640 — Generate and enforce browser CSP, static-bundle, and release-capability contracts and verify `BrowserReleaseContractTests` rejects remote assets, reporters, service workers, telemetry, diagnostics, or unapproved enablement**
  - **Files:** new Browser CSP/release manifest generation under
    `eng/setup-assistant/**`, static host config, Browser/Architecture tests.
  - **Acceptance:** Policy intent denies connections/forms/framing/objects/
    remote media/fonts/workers/navigation while admitting only pinned
    selected-runtime requirements; source/publish separation is deterministic;
    production has no remote asset, analytics, crash upload, update check,
    source-map values,
    CSP reporter, PWA/service worker, or developer tooling; secret capability
    stays disabled until exact-bundle independent security/legal evidence is
    linked.
  - **Focused selector:**
    `dotnet run --project tests/Event.SetupAssistant.Browser.Tests/Event.SetupAssistant.Browser.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*BrowserReleaseContractTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** SA-630.
  - **Guidance:** ci-cd, criticality-guardrail, ip-clean-room.
  - **Result:** `ApprovedDisabled`. The generator-owned manifest keeps both
    browser target and secret entry false. Focused release/disposition tests
    prove there is no `wwwroot`, index, service worker, web manifest, host
    config, remote-runtime graph, telemetry, reporter, or publishable browser
    surface. No CSP or runtime bundle was fabricated for an absent target.

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.SetupAssistant.Browser.Tests/Event.SetupAssistant.Browser.Tests.csproj --configuration Release --verbosity quiet`

## Phase 7: Desktop Protected Output

Plan reference: Phase 7 and Sections 3.4, 3.7, and 5.6.

- [x] **SA-710 — Author real-filesystem protected-write Invariant-Breakers and verify `DesktopProtectedWriteInvariantTests` fails on missing Windows/Unix adapters for links, permissions, races, atomicity, cleanup, and overwrite**
  - **Files:** new
    `tests/Event.SetupAssistant.Desktop.Tests/DesktopProtectedWriteInvariantTests.cs`
    and target-capability fixtures.
  - **Acceptance:** Red tests subscribe to exact file/state transitions without
    sleeps; cover regular file, directory, symlink/reparse, special file,
    target swap, unsupported permission model, existing file, crash/failure
    cleanup, owner-only verification, no backup, and closed value-free errors.
  - **Focused Red selector:**
    `dotnet run --project tests/Event.SetupAssistant.Desktop.Tests/Event.SetupAssistant.Desktop.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*DesktopProtectedWriteInvariantTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** L
  - **Dependencies:** Phase 5.
  - **Guidance:** criticality-guardrail, tests rule.
  - **Result:** Intentional Red captured 9/9 missing-owner failures. Green
    exercises real Linux files for create-new, explicit overwrite, exact
    bytes, owner-only mode, no backup/sidecar, directory/link/device refusal,
    target-created and target-replaced races between prepare/commit,
    uncommitted cleanup, unsupported Windows behavior, and closed value-free
    results. No sleep, polling, fixture secret, or in-memory filesystem is used.

- [x] **SA-720 — Implement Windows and Unix protected-write adapters and verify permission-first same-directory atomic replacement passes supported-runner invariants without unsafe fallback**
  - **Files:** new `src/Event.SetupAssistant.Desktop/Files/**`, platform target
    files, tests.
  - **Acceptance:** Handle-first target inspection, same-directory temp,
    restrictive current-user ACL or Unix owner read/write mode, flush, atomic
    install/replace, identity/permission post-check, and cleanup are
    target-specific; links/reparse/special/changed targets refuse; unsupported
    protection fails closed; errors never contain content or usernames unless
    explicitly consented.
  - **Effort:** XL
  - **Dependencies:** SA-710.
  - **Guidance:** criticality-guardrail, official .NET file APIs.
  - **Result:** Green on the supported Unix runner. A public two-step
    preparation owns a same-directory mode-0600 temporary file; commit
    revalidates target state, rejects directory/link/device/swap/overwrite
    violations, atomically moves, verifies final mode/length, and always cleans
    uncommitted state. Windows remains truthfully unavailable until an
    ACL-backed implementation receives runner evidence; it returns
    `Unsupported` and never inherits ambient permissions or writes plaintext.
    Focused invariants passed 9/9.

- [x] **SA-730 — Implement desktop composition, save review, and secret lifecycle and verify no autosave, restore, backup, recent-value, clipboard, telemetry, or raw exception path exists**
  - **Files:** new Desktop startup/platform capability adapters, shared save
    views/view models, tests.
  - **Acceptance:** Native picker supplies user intent; exact path and redacted
    key-level diff precede overwrite; secret bytes pass directly to protected
    writer; state clears on all exits; no automatic backup/history/autosave/
    recent-value/clipboard/crash upload; unsupported filesystem warnings and
    explicit lower-assurance override are truthful.
  - **Effort:** L
  - **Dependencies:** SA-720, SA-530.
  - **Guidance:** accessibility, i-vsd.
  - **Result:** `ApprovedDisabled` for desktop composition and secret UI
    because Avalonia Desktop and rendered accessibility remain blocked. The
    package-free protected-write transaction is active independently, but no
    desktop startup, view, save dialog, secret buffer, autosave, restore,
    backup, recent-value, clipboard, telemetry, raw exception, or support
    surface exists. Desktop `SetupTargetEnabled` remains false.

### Phase 7 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.SetupAssistant.Desktop.Tests/Event.SetupAssistant.Desktop.Tests.csproj --configuration Release --verbosity quiet`

## Phase 8: YAML, Directory Composition, And Measured Scale

Plan reference: Phase 8 and Sections 3.13 and 5.10.

- [x] **SA-810 — Author composition Invariant-Breakers and verify `SetupCompositionInvariantTests` rejects ambiguous, unsafe, non-canonical, and oversized source trees before output**
  - **Closure:** C1 Red only. No product, package, lock, generated artifact,
    adapter, scale profile, or documentation change belongs to this slice.
  - **Owned paths:** new
    `tests/Event.Setup.Core.Tests/SetupCompositionInvariantTests.cs`,
    `tests/Event.Setup.Core.Tests/SetupCompositionTestContract.cs` only.
  - **Future public seam:** Tests discover and exercise
    `ISLAMU.Event.Setup.Core.Composition.SetupCompositionCompiler`,
    `SetupCompositionLimits`, typed source/result/failure contracts, and an
    exact directory snapshot/commit barrier. They do not own a parser, merger,
    filesystem policy, serializer, or canonicalization mirror.
  - **Default ceilings:** aggregate source bytes `4,194,304`; YAML documents
    `1`; parser events `131,072`; normalized nodes `65,536`; nesting depth
    `32`; mapping entries per container `4,096`; sequence entries per container
    `4,096`; scalar characters per scalar `65,536`; aggregate scalar
    characters `1,048,576`; directories `256`; files `1,024`; entries per
    directory `256`; relative-path characters `512`; path depth `16`;
    per-file bytes `524,288`; aggregate directory bytes `4,194,304`; aggregate
    directory nodes `65,536`. All arithmetic is checked and exact-boundary
    tests accept `limit` and reject `limit + 1`.
  - **Fourteen independent matrices:** (1) duplicate keys, non-scalar/null
    keys, case and Unicode-normalization collisions; (2) aliases, anchors,
    tags, merge keys, directives, and unsupported node kinds; (3) quoted/
    unquoted scalar parity with explicit Core-owned bool/integer/null/string
    conversion and no locale drift; (4) empty stream/document, scalar or
    sequence root, multi-document, explicit/trailing document content;
    (5) every byte/event/node/depth/mapping/sequence/scalar ceiling and checked
    overflow; (6) rooted, absolute, traversal, escaped, overlong, reserved, and
    normalization-colliding paths; (7) symlink/reparse/junction/hard-link/
    special-file/cycle and unsupported filesystem semantics; (8)
    deterministic add/remove/rename/replace/resize/retarget mutations after
    discovery and after open but before publication commit; (9) duplicate/
    conflicting fragments and deterministic ordinal precedence/order;
    (10) cancellation before discovery, during read/parser/normalization/
    validation/serialization, and at commit with no partial result;
    (11) secret, provider-coordinate, application-data, publication,
    acceptance, and tenant/user authority smuggling; (12) byte-identical
    canonical v1alpha2 JSON, digest, coverage, legal and diagnostic parity
    across accepted JSON/YAML/directory inputs; (13) closed value-free
    failures, logs, metrics, exception surfaces, and source-path omission;
    (14) unknown/disabled/evidence-mismatched profiles fail rather than clamp
    or fall back.
  - **Phase 8 Worst Break:** At an exact publication-commit barrier, mutate a
    previously opened directory entry into a link/changed file while a bounded
    YAML alias/parser-bomb input reaches its final ceiling. The compiler must
    cancel/fail closed with one stable value-free code and produce no model,
    canonical bytes, digest, coverage, metric value, partial file, or retained
    source handle. No sleep, polling, path-only precheck, or mocked filesystem
    may satisfy this breaker.
  - **Focused selector:**
    `dotnet run --project tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupCompositionInvariantTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Red disposition:** Every independent policy/vector test compiles; one
    attributable aggregate test fails only for the absent production compiler.
    Any helper, fixture, timing, unrelated, or multiple-owner failure blocks
    C1 Green.
  - **Immediate planned commit:** After the intentional Red is verified, load
    `conventional-commit`, preserve unrelated index/worktree state, stage only
    the two owned paths above, and use
    `test(self-hosting): lock bounded setup composition invariants`.
    **Literal description:**
    `Define deterministic composition security contracts for ambiguous YAML,
    mutable directories, canonical parity, and bounded resource use.`
    `Pin fourteen invariant matrices, exact source ceilings, and the Phase 8
    publication-commit Worst Break without activating product composition.`
    **Literal footers:**
    `Changelog: skip`
    `Changelog-Reason: test-only security contract for unimplemented setup composition`
    **Message override:** `Not overridden`.
  - **Pre-commit ownership rule:** Inspect the existing index and full diff
    before editing or staging. Any owned path with another contributor's hunk
    blocks the slice until coordinated, separately committed, or clean. Never
    include a conditional correction to an approved Red file unless this
    ledger first records the exact path, defect, replacement contract, and
    fresh verification. Stage and commit only wholly owned explicit paths;
    verify the resulting commit file list and each file content hash against
    the recorded owned set. A material divergence must first replace literal
    title, description, changelog/footer copy, paths, and verification here.
    Commit execution remains subject to the active agent's explicit
    git-authorization boundary.
  - **Effort:** L
  - **Dependencies:** Phases 2–4 plus fresh scale/I-VSD/CTO/user approval.
  - **Guidance:** criticality-guardrail, tests rule.
  - **Result:** Intentional C1 Red passed the fourteen independent
    policy/vector matrices and failed exactly one aggregate assertion for
    absent
    `ISLAMU.Event.Setup.Core.Composition.SetupCompositionCompiler`.
    Selector result: 15 total, 14 passed, 1 failed; no compile, fixture,
    timing, platform, helper, multiple-owner, or unrelated failure occurred.
    Core/test package references, locks, central pins, and product source
    remained unchanged.

- [x] **SA-820 — Implement bounded YAML and directory composition and verify all accepted inputs compile through one normalized model to byte-identical canonical v1alpha2 JSON**
  - **Closure:** C1 Green Core only. CLI, terminal, browser, desktop, source
    pickers, presentation adapters, generated scale profiles, and larger
    limits are excluded and require later independent review.
  - **Owned paths:** `src/Event.Setup.Core/Event.Setup.Core.csproj`;
    `src/Event.Setup.Core/packages.lock.json`;
    new `src/Event.Setup.Core/Composition/SetupCompositionContracts.cs`,
    `src/Event.Setup.Core/Composition/SetupCompositionLimits.cs`,
    `src/Event.Setup.Core/Composition/SetupCompositionCompiler.cs`,
    `src/Event.Setup.Core/Composition/SetupCompositionYamlParser.cs`,
    `src/Event.Setup.Core/Composition/SetupCompositionDirectoryReader.cs`,
    `src/Event.Setup.Core/Composition/SetupCompositionNormalizer.cs`;
    `tests/Event.Setup.Core.Tests/packages.lock.json`; and the unchanged C1 Red
    tests as verification inputs only, never as C1 Green commit paths; plus new
    `docs/SETUP_COMPOSITION.md` and
    `docs/releases/changes/CHG-01M1C8MP8S1T10N8D3D5A7B9CX.yaml`; and the
    tracked-clean
    `tests/Event.Setup.Core.Tests/SetupCoreArchitectureTests.cs` under the
    ratchet amendment below.
  - **Ratchet amendment:** The first full Core run passed 58/59 and proved the
    prior package-free architecture ratchet directly contradicts the approved
    C1 Green scope. Replace only these stale rules: allow the exact
    `YamlDotNet` assembly reference; allow public namespace
    `ISLAMU.Event.Setup.Core.Composition`; allow `System.IO` calls only from
    types in that Composition namespace. Preserve all public `System.IO`/
    `System.Net` dependency bans, every ambient time/random/process/network
    ban, mutable-collection/value-bearing failure bans, all other assembly/
    namespace bans, and add/retain static rejection of YamlDotNet generic
    deserializer/serializer/emitter/dynamic/naming-convention roles. The
    architecture test file was tracked-clean before this amendment and becomes
    wholly C1 Green-owned only for those assertions. Focused 15/15, full Core,
    static role scan, and full Release verification must rerun after the
    amendment. No C1 Red test changes are permitted.
  - **Dependency role:** YamlDotNet `18.1.0` may enter Setup Core only after the
    C1 Red disposition and exact approved one-node content hash are reverified.
    Only parser events and representation/syntax-tree nodes are permitted.
    Generic deserializer/serializer, emitter, naming convention, dynamic type,
    alias/anchor/tag/merge support, and remote resolution remain forbidden by
    static/assembly ratchets.
  - **Acceptance:** All fourteen matrices and the Worst Break pass through one
    normalized immutable Core model. Existing canonical v1alpha2 JSON
    serializer/validator/digest/sensitivity/legal/coverage authorities own
    final output. Source formats add no wire identity; every conflict,
    mutation, cancellation, unsupported filesystem, or ceiling failure occurs
    before any model/bytes/digest/partial output. Linux real-filesystem
    invariants must pass; Windows directory input remains disabled unless a
    Windows runner proves equivalent handle-safe semantics.
  - **Green verification:** locked exact graph resolution; forbidden-role
    static and compiled-assembly closure; focused C1 tests; then one full
    Release build and full Setup Core test project. Phase-attributable failures
    block this slice; unrelated pre-existing warnings remain reported, never
    suppressed.
  - **Immediate planned commit:** After Green verification, load
    `conventional-commit`, stage only exact owned paths, preserve unrelated
    state, and use
    `feat(self-hosting): add bounded setup composition`.
    **Literal description:**
    `Compile bounded JSON, YAML, and directory sources through one immutable
    Setup Core model and the existing canonical v1alpha2 JSON authority.`
    `Reject ambiguous grammar, unsafe filesystem state, smuggled authority,
    cancellation, and resource-limit violations before producing any output.`
    **Literal footer:** `Change-Id: CHG-01M1C8MP8S1T10N8D3D5A7B9CX`.
    **Message override:** `Not overridden`.
  - **Exact change fragment:** Create
    `docs/releases/changes/CHG-01M1C8MP8S1T10N8D3D5A7B9CX.yaml` with:
    ```yaml
    # ABOUTME: Public change fragment for bounded Setup composition source formats.
    # ABOUTME: Records canonical parity and fail-closed parser/filesystem policy.
    Change-Id: CHG-01M1C8MP8S1T10N8D3D5A7B9CX
    Title: "Bounded Setup composition sources"
    Type: feat
    Scope: self-hosting
    Summary: "Setup Core compiles bounded JSON, YAML, and directory sources to byte-identical canonical v1alpha2 JSON without adding wire identity."
    Group: setup-composition
    Supersedes: []
    Impacts:
      Breaking:
        Reference: docs/SETUP_COMPOSITION.md
        Disposition: documented
        Detail: >-
          Ambiguous YAML grammar, unsafe directory entries, unknown files,
          smuggled authority, and inputs above exact ceilings fail closed;
          no permissive or legacy source mode is retained.
      Security:
        Reference: docs/SECURITY_OVERVIEW.md
        Disposition: documented
        Public-Disclosure: documented
        Detail: >-
          Syntax-tree-only parsing, deterministic directory revalidation,
          canonical parity, value-free failures, and no partial output protect
          self-hosted composition.
      Configuration:
        Reference: docs/SETUP_COMPOSITION.md
        Disposition: documented
        Detail: >-
          Canonical defaults bound source bytes, parser structure, paths,
          files, directories, scalar sizes, and normalized nodes.
      Operator:
        Reference: docs/OPERATIONS.md
        Disposition: documented
        Detail: >-
          Operators select one bounded source form and receive canonical output
          only after complete validation; unsupported filesystem semantics
          disable directory input.
    ```
  - **Pre-commit ownership rule:** The existing central YamlDotNet pin is a
    verified input and is not edited or staged. Inspect index/full diff before
    any owned-path edit; another contributor's hunk blocks that file and this
    closure until coordinated, separately committed, or clean. C1 Green never
    edits/stages C1 Red tests. Use explicit-path staging only for wholly owned
    paths; verify committed file list and content hashes exactly. Material
    divergence requires literal ledger replacement and re-review. Commit
    execution remains subject to the active agent's explicit git-authorization
    boundary.
  - **Effort:** XL
  - **Dependencies:** SA-810.
  - **Guidance:** clean-architecture-rules, ip-clean-room.
  - **Result:** Green. Setup Core compiles bounded JSON, restricted YAML, and
    Linux directory snapshots through one normalized model and the existing
    Wire authority. Architecture 10/10, composition 15/15, full Core 61/61,
    exact YamlDotNet 18.1.0 graph, LSP, static-role, canonical parity, and
    Release build gates passed. Windows directory input remains disabled.

- [x] **SA-830 — Add measured scale profiles and verify `SetupCompositionScaleTests` keeps canonical defaults while enabling only evidence-backed limits compatible with the target server**
  - **Closure:** C2 scale only, after C1 Green. It does not change parser
    grammar, normalized model, wire identity, filesystem semantics, UI/CLI
    adapters, or canonical default limits.
  - **Owned paths:** new
    `src/Event.Setup.Core/Composition/SetupCompositionScaleProfile.cs`,
    `tests/Event.Setup.Core.Tests/SetupCompositionScaleTests.cs`,
    `.omo/evidence/20260831-setup-assistant-security-and-portability/phase8-scale-results.md`,
    new `eng/setup-assistant/GenerateSetupCompositionScaleProfiles.cs`,
    `eng/setup-assistant/generated/composition-scale-profiles.json`, and the
    new `docs/SETUP_COMPOSITION_SCALE.md`.
  - **Profiles:** measure `small`, `medium`, `large`, and `ceiling` with exact
    synthetic source kind, directory/file/entry counts, aggregate/per-file
    bytes, depth, nodes/events, mapping/sequence counts, scalar lengths, and
    canonical artifact size/hash. Record OS/architecture, CPU count,
    available/total memory, process limits, filesystem semantics, SDK/runtime,
    commit, warmup/iteration counts, median/p95 elapsed, allocated bytes, peak
    working set, GC counts, stack-overflow disposition, and cancellation.
    Runtime telemetry may emit only closed source-kind/profile/outcome plus
    aggregate bytes/nodes/files/duration; never values, keys, paths, hashes,
    exception text, tenant/user IDs, secrets, or provider/application data.
  - **Acceptance:** The canonical default remains the SA-810 ceiling set.
    Larger profiles activate only when evidence fits client and target-server
    limits. Unknown, disabled, target-incompatible, or evidence-mismatched
    profiles fail closed and never clamp, fall back, or silently replace the
    default.
  - **Focused selector:**
    `dotnet run --project tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupCompositionScaleTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Immediate planned commit:** After deterministic profile tests and
    controlled evidence pass, load `conventional-commit`, preserve unrelated
    state, stage only owned paths, and use
    `perf(self-hosting): record setup composition scale profiles`.
    **Literal description:**
    `Record deterministic small, medium, large, and ceiling composition
    measurements with host and filesystem evidence.`
    `Keep canonical defaults unchanged and fail closed for unknown,
    incompatible, disabled, or evidence-mismatched profiles.`
    **Literal footers:**
    `Changelog: skip`
    `Changelog-Reason: measurement-only governance with unchanged canonical composition defaults`
    **Message override:** `Not overridden`.
  - **Pre-commit ownership rule:** Every C2 owned path is new. Inspect index and
    full diff before editing/staging; any pre-existing or another contributor's
    hunk blocks the path until coordinated, separately committed, or clean.
    Stage only wholly owned explicit paths, then verify the commit file list
    and content hashes exactly. Material divergence requires literal ledger
    replacement and re-review. Commit execution remains subject to the active
    agent's explicit git-authorization boundary.
  - **Effort:** M
  - **Dependencies:** SA-820.
  - **Guidance:** criticality-guardrail.
  - **Result:** Green. The warning-free controlled generator measured
    `small`, `medium`, `large`, and exact 4,096-entry `ceiling` workloads,
    recorded host/revision/time/allocation/GC/cancellation/target evidence, and
    verified canonical size/SHA-256 parity 4/4. Admission rejects unknown,
    disabled, evidence-mismatched, and target-incompatible profiles without
    clamping or fallback. Focused scale 4/4 and architecture 10/10 passed.

### Phase 8 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj --configuration Release --verbosity quiet`

## Phase 9: Live Target Enrollment And Secret-Provider Binding

Plan reference: Phase 9 and Sections 3.14 and 5.11.

### Phase 9 Bound D2 Staging

| Stage | Status | Exact ownership / exit |
|---|---|---|
| D2-0 | Corrected Red approved | Isolated one-file API Red; single HTTP ownership; exact `SetupLiveMilestone` ID/name/operation/milestone; bounded evidence claims |
| D2-1 | Corrected Green approved | Package-free `src/Event.Wire.Contracts/SetupLive/**`; Wire/Core/Architecture contract Red then Green; exclude every P9-008 type |
| D2-2 | Green approved | `src/Explore.Domain/SetupLive/**`; Domain aggregate Red then Green for enrollment, issuance claim, operation, generation, revocation, and fingerprints |
| D2-3 | Corrected Green approved | `src/Explore.Application/Contracts/SetupLive/SetupSecretBindingContracts.cs` plus exact `Contracts/Secrets/ISetupSecretBindingWriter.cs` and `ISetupSecretBindingCommitBarrier.cs`; no `Features/SetupLive` owner exists. `Invalid = 0`, UUIDv7 RFC-variant checks, exact scalar mappings, and borrowed-memory identity are Green. D2-7 owns executable pre-dispatch ordering, revocation race, selected writer/HMAC, idempotency, call-count, cancellation, and lease-disposal proof |
| D2-4 | Green approved | Persistence configurations/repositories/DbSets/lock coordinator; real PostgreSQL race/tenant Green, five-provider model parity, and generator-produced migrations/snapshots for PostgreSQL, MariaDB, MySQL, SQLite, and SQL Server |
| D2-5 | Green approved | Exact API controller/HAL/problem/rate/timeout/body/DI/OpenAPI owner; 16/16 owned scenarios, deterministic PostgreSQL issuance race, and weighted 100/100 Tier 1 approval |
| D2-6 | Green approved | Persisted binding authority fails closed before cache or source access; cold matrix 3/3, warm-cache transition 1/1, full class 16/16, weighted 100/100 approval |
| D2-7 | Green approved | Selected-authority writer, separate value-free readiness port, HMAC commitment, reconciliation-safe idempotency, pre-body authorization, shared generation lease, telemetry closure, and real PostgreSQL both-ordering proof; weighted 100/100 approval |
| D2-8 | Green approved | Canonical OpenAPI/client regeneration, exact typed/media/header closure, behavioral generated-client proof, and byte-identical second pass; weighted 100/100 approval |
| D2-9 | ApprovedDisabled approved | `unix-cli = ApprovedDisabled`: complete target/build-input ratchets prove no platform credential-store/protected-handle owner; no persistence fallback; weighted 100/100 approval |
| D2-10 | Green approved | Separately compiled nested outer adapter implements adapter-owned nonredirecting TLS, fresh ephemeral bearer authentication, private capability custody, exact generation/expiry transitions, complete HAL/method gating including nested-cache invalidation, RFC UUIDv7 mutation fences, exact Ready binding writes, post-dispatch cancellation/timeout authority clearing, bounded failures, and no persistence/log/provider surface; `SetupLiveAdapterSecurityTests` pass 34/34; weighted 100/100 approval |
| D2-11 | In progress | Capability manifest/generator, docs/change fragment, full relevant tests, Release build, weighted MAD, and flags false until closure |

Every stage requires its owning Red/review before behavior. Existing mixed-
author files are narrow-hunk only. EF migrations/model snapshots and generated
API/client artifacts are produced only by canonical generators after their
source models/contracts are Green.

- [ ] **SA-910 — Author live-authority Invariant-Breakers and verify `SetupLiveAuthoritySecurityTests` rejects token leakage, replay, cross-target tenancy, source authority, provider-coordinate disclosure, and secret readback**
  - **Files:** new
    `tests/Event.API.IntegrationTests/Features/SetupLiveAuthoritySecurityTests.cs`
    only. The Setup live-adapter Red moves to the SA-920 contract-first
    checkpoint because no public adapter seam exists and a test-owned or
    reflection mirror would not exercise production behavior.
  - **Acceptance:** Red tests cover enrollment expiry/revocation, tenant scope,
    HAL authority, rejection of protected-profile persistence for the selected
    Unix CLI target, write-only secret binding,
    provider readiness, RFC 7807 errors, and value-free logs/support evidence.
    Every capability operation requires current bearer identity, exact actor
    match, and fresh server authorization. Issuance never uses generic
    response-body idempotency replay; duplicates return a value-free receipt.
    Concurrent revocation fences provider/outbox effects before dispatch.
    Exact route/header/media/HAL/ProblemDetails/persistence/observability
    identities are frozen by
    `../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-phase9-final-intake.md`; a guessed
    404 is invalid. D1 covers absent-owner and no-dispatch paths without
    mirroring future contracts. Provider-success/inverse-dispatch assertions
    begin after the real writer seam exists in SA-930.
    Every test resets/reseeds database and tenant/actor state, time,
    authorization, and telemetry. Commit/dispatch races coordinate only on
    structured event `SetupLiveMilestone` (`19620`) with exact
    `SetupOperation`/`SetupMilestone` values, never arbitrary activity start.
    D1 does not claim writer or resolver/source call counts; those begin after
    the real static seams exist in SA-930.
  - **Focused selector:**
    `dotnet run --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupLiveAuthoritySecurityTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** L
  - **Dependencies:** Phase 8 and fresh I-VSD/CTO approval.
  - **Guidance:** auth-patterns, criticality-guardrail.

- [ ] **SA-920 — Implement target enrollment and revocation with ephemeral Unix CLI authority and verify scoped authority never enters portable artifacts, persistence, or machine/process surfaces**
  - **Files:** new live Setup adapter, enrollment wire contracts, server
    authorization endpoints/handlers, and new
    `tests/Event.SetupAssistant.Tests/SetupLiveAdapterSecurityTests.cs` after
    the generated/public live contract exists and before adapter behavior. The
    selected Unix CLI target has no profile-store, protected-handle, or
    credential-persistence source/tests; adding any requires a fresh target
    disposition before implementation.
  - **Acceptance:** Interactive/device authorization binds exact target and
    tenant; authority exists only in the adapter's bounded in-memory session;
    expiry/revocation clears it; no saved profile, protected handle,
    credential persistence, or long-lived plaintext token exists.
  - **Effort:** XL
  - **Dependencies:** SA-910.
  - **Guidance:** auth-patterns, clean-architecture-rules.

- [ ] **SA-930 — Implement target-local secret-binding/provider readiness and verify Setup can write or test approved bindings without reading raw values or exposing provider coordinates**
  - **Files:** new Setup provider workflows, existing/new server secret-provider
    API/Application adapters, HAL policy, tests and operator docs.
  - **Acceptance:** Allowlisted binding identifiers remain outside portable
    artifacts; provider access is server-authorized and tenant-qualified;
    write/readiness responses are value-free; failures do not fall back.
  - **Effort:** XL
  - **Dependencies:** SA-920.
  - **Guidance:** criticality-guardrail, secret isolation, error-tracking.

### Phase 9 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 10: Live Apply And Direct-Transfer Orchestration

Plan reference: Phase 10 and Sections 3.14 and 5.11.

- [ ] **SA-1010 — Author live-operation contract tests and verify `SetupConfigurationOperationContractTests` rejects stale HAL, expired capabilities, replay, target mismatch, false completion, and local rollback authority**
  - **Files:** new Setup live-operation tests over generated API contracts.
  - **Acceptance:** Red tests pin preview/apply/managed review/history/rollback/
    transfer affordances, header-only capabilities, target scope, receipts,
    pending effects, cancellation, expiry, and resumable failure states.
  - **Focused selector:**
    `dotnet run --project tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupConfigurationOperationContractTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** L
  - **Dependencies:** Phase 9 and green ConfigurationManifest Tier 1,
    tenant-isolation, replay, and atomicity gates. Missing evidence disables
    successor D live work and cannot be waived.
  - **Guidance:** auth-patterns, accessibility.

- [ ] **SA-1020 — Implement live import, managed apply, receipt, effect, cancellation, and forward-rollback workflows and verify every action is server-HAL-gated and target-authoritative**
  - **Files:** new Setup live configuration adapters/workspaces; existing
    generated clients and ConfigurationManifest operation contracts.
  - **Acceptance:** Setup uploads canonical artifacts, preserves capability
    headers, renders server preview/receipt/effect truth, supports distinct
    reviewer/applier roles, and never synthesizes completion or rollback.
  - **Effort:** XL
  - **Dependencies:** SA-1010.
  - **Guidance:** accessibility, auth-patterns, blazor-bff-patterns.

- [ ] **SA-1030 — Implement mutually approved resumable direct-transfer workflows and verify chunk resume, atomic promotion, expiry, replay fencing, source retention, and SSRF-safe destination policy**
  - **Files:** new Setup transfer adapters/workspaces and focused tests;
    existing direct-transfer contracts.
  - **Acceptance:** Both target actors approve; capabilities and chunks remain
    bounded; promotion relies on server atomic claim; cancellation/expiry
    cleans target staging; source state is never deleted.
  - **Focused selector:**
    `dotnet run --project tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupDirectTransferWorkflowTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** SA-1020.
  - **Guidance:** criticality-guardrail, error-tracking.

### Phase 10 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj --configuration Release --verbosity quiet`

## Phase 11: Application-Data And Sovereign Operations Migration

Plan reference: Phase 11 and Sections 3.15 and 5.12.

- [ ] **SA-1110 — Author Tier 1/Tier 2 application-migration Invariant-Breakers and verify `SetupApplicationMigrationInvariantTests` rejects cross-tenant mappings, duplicate replay, checkpoint races, and secret/PII telemetry**
  - **Files:** new Domain/Application/Persistence/API migration tests and MAD
    evidence scaffold.
  - **Acceptance:** Successor E Red tests cover category authority, immutable
    source IDs, target mappings, idempotency, concurrent resume, file integrity,
    privacy, and tenant isolation. The same evidence handoff records, but does
    not complete, successor F slice F1: the dedicated named Worst Break Red is
    independently owned and must run before SA-1140 production code against the
    real owning database/provider contract.
  - **Focused selector:**
    `dotnet run --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupApplicationMigrationInvariantTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** Phase 10 and successor E's fresh Tier 2 custody/erasure,
    Tier 1 tenant, current I-VSD, CTO, user, and named evidence gates. The
    `IVSD-F043/M043` Tier 0 boundary decision is recorded before SA-1110 as
    mapped, but it does not grant successor F implementation approval.
  - **Guidance:** criticality-guardrail, grill-me, dotnet-efcore-guidelines.

- [ ] **SA-1120 — Implement durable migration plans, mappings, checkpoints, protected staging, receipts, and outbox atomicity and verify interruption/replay cannot duplicate or cross tenant boundaries**
  - **Files:** new server Domain/Application/Persistence migration feature,
    generated provider migrations, repositories and tests.
  - **Acceptance:** Category selection and target scope are immutable;
    checkpoints/mappings/idempotency are durable; files are digest-verified;
    commit and effects use transactional outbox; source remains intact.
  - **Effort:** XL
  - **Dependencies:** SA-1110.
  - **Guidance:** cqrs-mediatr-guidelines, dotnet-efcore-guidelines,
    outbox-pattern.

- [ ] **SA-1130 — Implement events, users, registrations, orders, tickets, uploaded-file, and other application-data API/HAL/Setup workflows and verify resumable category progress and recovery remain truthful**
  - **Files:** new migration API/HAL/generated-client/Setup adapters/workspaces,
    tests and operations docs.
  - **Acceptance:** Every category has explicit authorization, compatibility,
    mapping blockers, progress, receipt, retry/cancel, and completion state;
    users/PII follow privacy authority; no configuration artifact carries data.
  - **Effort:** XL
  - **Dependencies:** SA-1120.
  - **Guidance:** auth-patterns, accessibility, criticality-guardrail.

- [ ] **SA-1132 — Activate application-data migration Setup workspaces and verify every category action is HAL-gated, tenant-qualified, resumable, and truthfully recoverable**
  - **Files:** generated migration client contracts; shared Setup presentation
    workspaces; Active target adapters; focused UI/contract tests.
  - **Acceptance:** Setup projects server-authored category scope, blockers,
    checkpoints, progress, receipts, retry/cancel, and completion without
    creating local custody or migration authority. HAL links exclusively gate
    actions; no PII, payload, token, mapping secret, or provider coordinate
    enters presentation messages, diagnostics, support evidence, or portable
    artifacts. An absent presentation target records `ApprovedDisabled`.
  - **Effort:** L
  - **Dependencies:** SA-1130 and at least one Active presentation target;
    otherwise an approved disabled UI disposition.
  - **Guidance:** auth-patterns, accessibility, criticality-guardrail.

- [ ] **SA-1135 — Author the sovereign-payment Worst-Break Red and verify a replayed cross-tenant finalization/refund race produces zero money intent**
  - **Files:** focused real-database/provider-contract concurrency tests and
    Tier 0 decision evidence.
  - **Acceptance:** Deterministic coordination subscribes before triggering the
    public-seam race and uses a bounded timeout without sleeps or internal
    mocks. The intentional Red independently asserts zero cross-tenant rows,
    zero provider/outbox money intent, unchanged checked ledger balances,
    exactly one durable value-free conflict receipt, and zero PII/secret logs.
  - **Focused Red selector:**
    `dotnet run --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupPaymentMigrationWorstBreakTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** D/E contracts; fresh Tier 0 Grill-Me, current I-VSD, CTO,
    exact-revision user approval, and provider/legal/operator evidence.
  - **Guidance:** criticality-guardrail, outbox-pattern, error-tracking.

- [ ] **SA-1140 — Implement sale-control, review, handoff, reconciliation, and refund migration state machines and verify money cannot mutate before target/provider reconciliation and explicit approval**
  - **Files:** new sovereign migration Domain/Application/Persistence/API and
    Setup workflow contracts, real-provider concurrency tests, runbooks.
  - **Acceptance:** Payment operations are separate from data/config imports.
    SA-1135 is already Red for the exact public seam. Checked amounts,
    currencies, provider identities, recipients, and refund allocations
    reconcile; conflicting or stale state pauses; retries are idempotent;
    compensation follows repository-native domain authority.
  - **Focused selector:**
    `dotnet run --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupPaymentMigrationInvariantTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** SA-1135 failing for the intended reason; fresh Tier 0
    Grill-Me, current I-VSD, CTO and exact-revision user
    approval; exact provider/ledger/recipient/currency/refund reconciliation
    and provider/legal/operator evidence.
  - **Guidance:** criticality-guardrail, outbox-pattern, error-tracking.

- [ ] **SA-1145 — Activate sovereign-payment API/HAL and Setup workspaces and verify no UI can synthesize money authority or completion**
  - **Files:** generated sovereign-operation clients; HAL policies; Setup
    presentation workspaces; Active target adapters; focused tests.
  - **Acceptance:** Every review, handoff, reconciliation, retry, cancellation,
    compensation, and refund affordance comes from server HAL. Presentation
    renders pending/unknown/conflicting states truthfully and carries no amount,
    recipient, provider credential, capability, PII, or secret through messages
    or support evidence beyond approved value objects. An absent target records
    `ApprovedDisabled`.
  - **Effort:** XL
  - **Dependencies:** SA-1140 and at least one Active presentation target;
    otherwise an approved disabled UI disposition.
  - **Guidance:** auth-patterns, accessibility, criticality-guardrail.

### Phase 11 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 12: Packaging, Provenance, Documentation, And Agent Skill

Plan reference: Phase 12 and Sections 3.10–3.15, 5.9–5.12, 8, and 9.

- [ ] **SA-1210 — Implement governed multi-target packaging and verify `SetupReleaseContractTests` accepts only evidenced RIDs/formats, approved graphs, immutable identity, and truthful capability/support tiers**
  - **Files:** new `eng/setup-assistant/**`, CI/release integration, package
    manifests and Architecture tests; existing release policy/scope registry.
  - **Acceptance:** Outputs contain only implemented/evidenced offline/live/
    migration capabilities; no commercial tool, mutable artifact, floating
    dependency, or unsupported cross-platform claim enters release.
  - **Focused selector:**
    `dotnet run --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupReleaseContractTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** Each selected owning successor and its verification gate
    are green; an unevidenced successor/capability is omitted rather than
    delaying or broadening an independently shippable subset.
  - **Guidance:** ci-cd intent, ip-clean-room.

- [ ] **SA-1220 — Implement release identity, signing/notarization, SBOM, checksums, provenance, claims, support, incident, migration, and operator contracts and verify every advertised capability has evidence or remains disabled**
  - **Files:** Setup release manifests/evidence, security/configuration/secrets/
    self-hosting/accessibility/localization/operations/troubleshooting docs,
    release governance and scope registry.
  - **Acceptance:** One identity joins version, commit, target, locks, SBOM,
    build manifest, checksums, source, signing, reproducibility and support;
    docs teach composition, live authority, provider binding, transfer,
    application migration, payment recovery, and disabled capability gates.
  - **Effort:** XL
  - **Dependencies:** SA-1210.
  - **Guidance:** ci-cd, i-vsd, conventional-commit, ip-clean-room.

- [ ] **SA-1240 — Create the version-gated `setup-assistant-cli` skill and verify `SetupAssistantSkillContractTests` proves implemented-command routing, no-secret defaults, scoped live approvals, and CLI/schema compatibility**
  - **Files:** new `.agents/skills/setup-assistant-cli/**`, intent registration,
    Architecture tests.
  - **Acceptance:** Skill uses implemented machine commands only, rejects
    secret-bearing inputs and TUI automation, defaults no-secret/dry-run,
    requires explicit approval before every write/live/migration action, and
    never broadens target/payment authority.
  - **Focused selector:**
    `dotnet run --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupAssistantSkillContractTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** L
  - **Dependencies:** SA-420, SA-1220.
  - **Guidance:** `create-agent-context-skill` intent, skill-authoring.

- [ ] **SA-1250 — Complete I-VSD/criticality reconciliation, create the Tier 2 change fragment, and verify final commit composition only after every phase gate is Green**
  - **Files:** existing I-VSD report/dev-doc triad; new append-only change
    fragment; clean-room, MAD/security, privacy, payment, legal, accessibility,
    dependency, release, and verification evidence.
  - **Acceptance:** Every I-VSD mapping matches shipped capabilities; missing
    evidence disables its capability; Tier 0/1 MAD findings are resolved;
    every task/gate is checked; `ReleaseInputPolicy` validates the fragment;
    terminal `feat(setup): ...` composition has exact `Change-Id` and required
    breaking footer; no commit is created without explicit user authorization.
  - **Effort:** M
  - **Dependencies:** SA-1220, SA-1240, every selected capability task, every
    ApprovedDisabled disposition, and all Phase 1–11 verification checkboxes.
    This task precedes the Phase 12 gate and has no self-dependency.
  - **Guidance:** criticality-guardrail, epistemic-mad-review,
    conventional-commit, review-pr, i-vsd.

### Phase 12 Verification — RUN ONCE AFTER SA-1210 THROUGH SA-1250

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- [ ] CLI release gate is Green or explicitly ApprovedDisabled.
- [ ] BCL terminal release gate is Green or explicitly ApprovedDisabled.
- [ ] Shared presentation release gate is Green or explicitly ApprovedDisabled.
- [ ] Avalonia Browser release gate is Green or explicitly ApprovedDisabled.
- [ ] Avalonia Desktop release gate is Green or explicitly ApprovedDisabled.
- [ ] Terminal.Gui release gate is Green or explicitly ApprovedDisabled.
- [ ] Live-control release gate is Green or explicitly ApprovedDisabled.
- [ ] Application-migration release gate is Green or explicitly ApprovedDisabled.
- [ ] Sovereign-payment release gate is Green or explicitly ApprovedDisabled.

## Remaining / Deferred Work

- Live API/HAL/BFF, authorization, provider binding, direct transfer, and
  application-data/payment migration are owned by Phases 9–11 and remain
  blocked by their explicit review and evidence gates. Saved profiles remain
  deferred for the selected Unix CLI target; any future target owns them only
  after a fresh `Active` disposition.
- PWA/service worker, auto-update, downloaded executable plugins/packs, and
  mobile targets require a later approved workstream.
- Hosted browser secret mode is planned only as a gated capability and
  remains disabled until exact-bundle independent security and legal evidence
  passes.
- Native Wayland, AppImage, Flatpak, framework-dependent global tool, and any
  reciprocal AGPL-only executable remain target-specific gated additions.
- Unapproved legal templates are not bundled. Blank authoring remains
  available without implying counsel review.
- Runtime/browser/platform accessibility audits are release evidence, not
  claims inferred from shared code or unit tests.
