<!-- ABOUTME: Hot execution ledger for the Setup Assistant security and portability workstream. -->
<!-- ABOUTME: Sequences invariant-first implementation, phase gates, release evidence, and final changelog composition. -->

# Setup Assistant Security And Portability — Task Checklist

Last Updated: 2026-08-30 Europe/Brussels

## Status Summary

- **Overall status:** Draft; implementation not started.
- **Completed:** 0/28 implementation tasks; phase verification is tracked
  separately.
- **Current priority:** User review and revision-bound Senior CTO review.
- **Next recommended slice:** `SA-110`.
- **Upstream disposition:** ConfigurationManifest was closed for archival by
  explicit user decision on 2026-08-30. SA-110 pins its current
  v1alpha2/schema/registry/import-preview outputs as the frozen extraction
  baseline; retired later phases are not implementation evidence.
- **Blocker:** User approval and revision-bound CTO review are not yet
  recorded.
- **Plan:**
  [setup-assistant-security-and-portability-plan.md](setup-assistant-security-and-portability-plan.md)
- **Context:**
  [setup-assistant-security-and-portability-context.md](setup-assistant-security-and-portability-context.md)
- **Clean-room evidence:**
  [setup-assistant-security-and-portability-clean-room-evidence.md](setup-assistant-security-and-portability-clean-room-evidence.md)
- **I-VSD report:**
  [i-vsd-setup-assistant-security-and-portability.md](../../../islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md)
- **I-VSD reviewed input revision:**
  `sha256:8c86d6be6f612861bba9c4ea641a451722fe0d6b5feccad09ac310b5cdce1637`
- **I-VSD status / disposition:** `current` / `plan-aligned`.
- **CTO review:** Not reviewed.
- **User approval:** Awaiting approval for this exact workstream revision.

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

- [ ] **SA-110 — Author failing Setup architecture and security-boundary contracts and verify `SetupAssistantArchitectureTests` fails only because the new projects and ratchets do not exist**
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
  - **Dependencies:** User approval and revision-bound CTO review.
  - **Guidance:** criticality-guardrail, clean-architecture-rules,
    ip-clean-room, tests rule.

- [ ] **SA-120 — Approve and pin the complete Setup dependency graphs and verify locked restore, vulnerability audit, license policy, and focused architecture contracts pass with no exception added**
  - **Files:** existing `Directory.Packages.props`,
    `Directory.Build.props`, `Explore.slnx`; new five Setup source projects,
    focused test projects, `packages.lock.json` files, and dependency evidence
    under this workstream.
  - **Acceptance:** Evaluate Avalonia `12.1.1` and Terminal.Gui `2.4.17` as
    candidates across direct, transitive, native, build, test, asset, font, and
    packaging roles. Record exact license/obligations/outbound impact. Block or
    replace every unknown/incompatible component. Do not include Avalonia
    professional tooling or production diagnostics.
  - **Effort:** XL
  - **Dependencies:** SA-110.
  - **Guidance:** ip-clean-room, agentic-research, CI/CD governance.

- [ ] **SA-130 — Wire Setup source, lock, and generated-output governance into CI and verify source is tracked while only build/publish/release output is ignored**
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

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 2: Shared Wire Contracts And Headless Core

Plan reference: Phase 2 and Sections 3.1, 3.2, 3.8, 5.1, and 5.2.

- [ ] **SA-210 — Author failing v1alpha2 and legal-codec extraction invariants and verify `SetupContractExtractionTests` detects every byte, diagnostic, limit, schema, and collection-ownership drift**
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

- [ ] **SA-220 — Move v1alpha2 wire contracts and constrained legal Markdown into `Event.Wire.Contracts` and verify old owners are deleted with all schema/server callers migrated**
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

- [ ] **SA-230 — Implement package-free `Event.Setup.Core` workflow contracts and verify `SetupCoreArchitectureTests` proves pure deterministic behavior with no I/O or ambient authority**
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

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Wire.Contracts.UnitTests/Event.Wire.Contracts.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Environment Catalogue And Offline Workflows

Plan reference: Phase 3 and Sections 3.1–3.4, 3.8, 5.2, and 5.3.

- [ ] **SA-310 — Author failing catalogue and dotenv Invariant-Breakers and verify `EnvironmentCatalogueInvariantTests` rejects cycles, drift, fake secrets, irrelevant keys, defaults, duplicates, injection syntax, and value-bearing diagnostics**
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

- [ ] **SA-320 — Implement the canonical environment catalogue and generator/check tool and verify `.env.example`, Compose, startup, secret registry, and documentation anchors converge without scraping prose**
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

- [ ] **SA-330 — Implement the explicit dotenv codec, readiness, and approved local secret generation and verify no-secret and secret outputs remain separate, deterministic, and value-safe**
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

- [ ] **SA-340 — Implement offline manifest, tenant-package, legal, diff, coverage, and readiness workflows and verify `OfflinePortabilityWorkflowTests` produces stable non-secret artifacts without live-target authority**
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

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj --configuration Release --verbosity quiet`

## Phase 4: Versioned CLI And Terminal.Gui TUI

Plan reference: Phase 4 and Sections 3.4, 3.9, 5.2, and 5.7.

- [ ] **SA-410 — Author failing command, machine-schema, and terminal-secret contracts and verify `SetupCliContractTests` fails on the missing executable while pinning help, dry-run, JSON, exits, TTY, and leakage boundaries**
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

- [ ] **SA-420 — Implement deterministic `event-setup` commands and verify machine JSON, text output, exit categories, digests, help, dry-run, and no-secret writes satisfy the public command contract**
  - **Files:** new `src/Event.SetupAssistant.Cli/Commands/**`,
    serialization context, generated command schema, tests.
  - **Acceptance:** Catalogue, manifest, tenant-package, env, legal, doctor,
    and `tui` command families use Setup Core; command parsing is repository
    native; machine output is one versioned object; write operations require
    explicit paths/approval semantics; machine mode cannot enter secret mode;
    unknown/removed commands fail without aliases.
  - **Effort:** XL
  - **Dependencies:** SA-410.
  - **Guidance:** clean architecture, record contracts.

- [ ] **SA-430 — Implement Terminal.Gui human workflows and verify `SetupTerminalSecretBoundaryTests` proves TTY-only masked entry, protected output, state clearing, and byte parity with Core**
  - **Files:** new `src/Event.SetupAssistant.Cli/Tui/**`, focused CLI tests.
  - **Acceptance:** TUI supports the same workspaces and Core outputs; secret
    mode requires an interactive TTY, disables stdout/stderr artifact output,
    retains no history/autosave/clipboard by default, and clears on cancel,
    completion, suspension, signal, resize failure, or navigation; terminal
    limitations are disclosed; agent automation remains machine CLI only.
  - **Focused selector:**
    `dotnet run --project tests/Event.SetupAssistant.Cli.Tests/Event.SetupAssistant.Cli.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupTerminalSecretBoundaryTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** SA-420.
  - **Guidance:** accessibility, criticality-guardrail.

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.SetupAssistant.Cli.Tests/Event.SetupAssistant.Cli.Tests.csproj --configuration Release --verbosity quiet`

## Phase 5: Shared Avalonia Workspaces, Accessibility, And Localization

Plan reference: Phase 5 and Sections 3.1, 3.4, 3.8, 3.10, and 5.4.

- [ ] **SA-510 — Author shared-workspace parity and secret-state contracts and verify `SetupAssistantWorkspaceTests` fails on missing Avalonia adapters without duplicating Core rules**
  - **Files:** new `tests/Event.SetupAssistant.Tests/**`.
  - **Acceptance:** Red tests specify workspace transitions, review/readiness,
    mode boundaries, immutable Core results, cancellation/expiry clearing, and
    byte-equivalent output while rejecting UI-owned validators, serializers,
    relevance rules, or secret classification.
  - **Focused Red selector:**
    `dotnet run --project tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupAssistantWorkspaceTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** M
  - **Dependencies:** Phases 3 and 4.
  - **Guidance:** criticality-guardrail, accessibility.

- [ ] **SA-520 — Implement shared Avalonia shell and product workspaces and verify manifest, environment, legal, review, and readiness flows adapt Core without server or network dependencies**
  - **Files:** new `src/Event.SetupAssistant/**`, shared view models, resources,
    views, styles, tests.
  - **Acceptance:** Workspaces expose progressive topology/capability selection,
    typed fields, deterministic previews/diffs/coverage, explicit sensitivity,
    no-secret primary action, review, clear, and save/download intents; no
    Application/Domain/API/Blazor/network/provider dependency or duplicated
    business rule exists.
  - **Effort:** XL
  - **Dependencies:** SA-510.
  - **Guidance:** accessibility, clean architecture, ip-clean-room.

- [ ] **SA-530 — Implement bundled localization, RTL, keyboard, focus, semantic automation, and error-announcement contracts and verify `SetupAccessibilityContractTests` exposes no secret value or unsupported parity claim**
  - **Files:** new shared Avalonia resources/styles/accessibility services and
    tests; existing `docs/ACCESSIBILITY.md`, `docs/LOCALIZATION.md`.
  - **Acceptance:** Native controls and automation peers expose stable
    names/roles/states; tab/focus/order/reflow/contrast/non-color/reduced-motion
    behavior is explicit; errors associate and summarize once; all security
    consequences are bundled/localized before secret mode; logical layout
    supports RTL; browser/TUI limitations remain target-labelled; no secret is
    announced or used as automation metadata.
  - **Focused selector:**
    `dotnet run --project tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupAccessibilityContractTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** SA-520.
  - **Guidance:** accessibility.

- [ ] **SA-540 — Implement constrained legal-document editor and approved local-template boundary and verify `LegalAuthoringWorkspaceTests` rejects HTML, remote content, unresolved authority, and publication or acceptance mutation**
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
  - **Dependencies:** SA-520, SA-530.
  - **Guidance:** i-vsd, accessibility, ip-clean-room.

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj --configuration Release --verbosity quiet`

## Phase 6: Browser Locality And Secret Boundary

Plan reference: Phase 6 and Sections 3.4–3.6 and 5.5.

- [ ] **SA-610 — Author browser secret Invariant-Breakers and verify `BrowserSecretBoundaryTests` fails on the missing target while pinning safe default, preload, request, storage, navigation, expiry, and capability behavior**
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

- [ ] **SA-620 — Implement browser no-secret composition and local download and verify every new session remains useful without entering or persisting secrets**
  - **Files:** new `src/Event.SetupAssistant.Browser/**`, static entry, local
    download adapter, tests.
  - **Acceptance:** Static WASM starts in no-secret mode, uses only bundled
    assets, generates relevant empty placeholders and non-secret
    manifests/packages, downloads locally, persists no profile by default, and
    clearly identifies incomplete secret completion and browser permission
    limitations.
  - **Effort:** L
  - **Dependencies:** SA-610.
  - **Guidance:** Avalonia official guidance, i-vsd.

- [ ] **SA-630 — Implement per-session browser trust and secret state machine and verify explicit opt-in, preload, network denial, clearing, and truthful origin copy remain bound to the exact release**
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

- [ ] **SA-640 — Generate and enforce browser CSP, static-bundle, and release-capability contracts and verify `BrowserReleaseContractTests` rejects remote assets, reporters, service workers, telemetry, diagnostics, or unapproved enablement**
  - **Files:** new Browser CSP/release manifest generation under
    `eng/setup-assistant/**`, static host config, Browser/Architecture tests.
  - **Acceptance:** Policy intent denies connections/forms/framing/objects/
    remote media/fonts/workers/navigation while admitting only pinned WASM
    requirements; source/publish separation is deterministic; production has
    no remote asset, analytics, crash upload, update check, source-map values,
    CSP reporter, PWA/service worker, or developer tooling; secret capability
    stays disabled until exact-bundle independent security/legal evidence is
    linked.
  - **Focused selector:**
    `dotnet run --project tests/Event.SetupAssistant.Browser.Tests/Event.SetupAssistant.Browser.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*BrowserReleaseContractTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** SA-630.
  - **Guidance:** ci-cd, criticality-guardrail, ip-clean-room.

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.SetupAssistant.Browser.Tests/Event.SetupAssistant.Browser.Tests.csproj --configuration Release --verbosity quiet`

## Phase 7: Desktop Protected Output

Plan reference: Phase 7 and Sections 3.4, 3.7, and 5.6.

- [ ] **SA-710 — Author real-filesystem protected-write Invariant-Breakers and verify `DesktopProtectedWriteInvariantTests` fails on missing Windows/Unix adapters for links, permissions, races, atomicity, cleanup, and overwrite**
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

- [ ] **SA-720 — Implement Windows and Unix protected-write adapters and verify permission-first same-directory atomic replacement passes supported-runner invariants without unsafe fallback**
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

- [ ] **SA-730 — Implement desktop composition, save review, and secret lifecycle and verify no autosave, restore, backup, recent-value, clipboard, telemetry, or raw exception path exists**
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

### Phase 7 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.SetupAssistant.Desktop.Tests/Event.SetupAssistant.Desktop.Tests.csproj --configuration Release --verbosity quiet`

## Phase 8: Packaging, Provenance, Documentation, And Agent Skill

Plan reference: Phase 8 and Sections 3.10–3.12, 5.9, 8, and 9.

- [ ] **SA-810 — Implement governed multi-target packaging and verify `SetupReleaseContractTests` accepts only evidenced RIDs/formats, approved graphs, immutable identity, and truthful support tiers**
  - **Files:** new `eng/setup-assistant/**`, CI/release workflow integration,
    Windows/macOS/Linux/browser/CLI package manifests, Architecture tests;
    existing release policy and scope registry.
  - **Acceptance:** Required stable outputs cover supported Windows/macOS/Linux
    desktop, static Browser, and six CLI RIDs; Linux X11/XWayland is baseline;
    native Wayland, AppImage, Flatpak, global tool, and extra architectures
    remain absent until target gates pass; no commercial packaging tool,
    mutable artifact, floating dependency, or unsupported “cross-platform”
    claim enters release.
  - **Focused selector:**
    `dotnet run --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupReleaseContractTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** XL
  - **Dependencies:** Phases 1–7.
  - **Guidance:** ci-cd intent, ip-clean-room.

- [ ] **SA-820 — Implement release identity, signing/notarization, SBOM, checksums, provenance, claims, support, incident, and operator contracts and verify every advertised capability has matching evidence or remains disabled**
  - **Files:** new/updated Setup release manifests and evidence,
    `docs/SECURITY-MODEL.md`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`,
    `docs/SELF_HOSTING.md`, `docs/ACCESSIBILITY.md`,
    `docs/LOCALIZATION.md`, `docs/OPERATIONS.md`,
    `docs/TROUBLESHOOTING.md`, release governance/runbooks/checklists,
    `eng/release/policy/scope-registry.yaml`.
  - **Acceptance:** One identity joins version, commit, RID/format, lock digest,
    SBOM, build manifest, checksums, source, signature/notarization,
    reproducibility, and support status; public `setup` scope exists; docs teach
    origin trust, no-secret/secret boundaries, desktop/browser/terminal limits,
    recovery, incident, accessibility, localization, packages, and no-live
    authority; signing keys remain external secrets; missing independent
    security/legal/accessibility/package evidence disables the exact claim.
  - **Effort:** XL
  - **Dependencies:** SA-810.
  - **Guidance:** ci-cd, i-vsd, conventional-commit, ip-clean-room.

- [ ] **SA-840 — Create the version-gated `setup-assistant-cli` skill and verify `SetupAssistantSkillContractTests` proves routing, links, examples, no-secret defaults, human approval, and CLI/schema compatibility**
  - **Files:** new
    `.agents/skills/setup-assistant-cli/SKILL.md`,
    focused resources, intent registration, Architecture tests.
  - **Acceptance:** Description routes setup generation/validation/diff/
    explanation while excluding CLI implementation and secret ingestion; body
    uses implemented machine commands only, checks compatible version, defaults
    no-secret/dry-run, never reads secret-bearing files or drives TUI, requires
    approval before writes, and hands secret completion to the local human UI;
    all resources have ABOUTME lines and schema/link checks pass.
  - **Focused selector:**
    `dotnet run --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*SetupAssistantSkillContractTests/*" --minimum-expected-tests 1 --progress off --maximum-parallel-tests 1`
  - **Effort:** L
  - **Dependencies:** SA-420, SA-820.
  - **Guidance:** `create-agent-context-skill` intent, skill-authoring.

### Phase 8 Verification — RUN ONCE AFTER SA-810 THROUGH SA-840

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

- [ ] **SA-850 — Complete I-VSD/criticality reconciliation, create the Tier 2 change fragment, and verify final commit composition only after every phase gate is Green**
  - **Files:** existing I-VSD report and dev-doc triad; new append-only
    `docs/releases/changes/CHG-YYYY-NNNN.yaml`; clean-room, MAD/security, legal,
    accessibility, dependency, release, and verification evidence.
  - **Acceptance:** Every `IVSD-F/M` mapping matches shipped capabilities;
    missing external evidence leaves its target disabled; anonymized Tier 1
    security review is resolved; implementation tasks and phase gates are all
    checked; release engine creates and validates the fragment through
    `ReleaseInputPolicy`; terminal subject uses `feat(setup): ...` and exact
    `Change-Id: CHG-YYYY-NNNN` footer; `BREAKING CHANGE:` appears only when the
    public contract warrants it; no commit is created unless the user
    explicitly authorizes committing.
  - **Effort:** M
  - **Dependencies:** SA-820, SA-840, all Phase 1–8 verification checkboxes.
  - **Guidance:** criticality-guardrail, epistemic-mad-review,
    conventional-commit, review-pr, i-vsd.

## Remaining / Deferred Work

- Live instance API/HAL/BFF operations, OAuth/device authorization, tokens,
  Infisical access, provider connectivity tests, encrypted saved profiles,
  PWA/service worker, auto-update, plugins/downloaded packs, direct transfer,
  application data, and mobile targets require separate approved workstreams.
- Hosted browser secret mode is implemented only as a gated capability and
  remains disabled until exact-bundle independent security and legal evidence
  passes.
- Native Wayland, AppImage, Flatpak, framework-dependent global tool, and any
  reciprocal AGPL-only executable remain target-specific gated additions.
- Unapproved legal templates are not bundled. Blank authoring remains
  available without implying counsel review.
- Runtime/browser/platform accessibility audits are release evidence, not
  claims inferred from shared code or unit tests.
