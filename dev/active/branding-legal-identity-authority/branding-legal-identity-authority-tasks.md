<!-- ABOUTME: Test-first execution ledger for tenant and instance legal-identity authority. -->
<!-- ABOUTME: Sequences Red, Green, generation, verification, documentation, and manual QA work. -->

# Branding And Legal-Identity Authority — Task Checklist

Last Updated: 2026-08-29 Europe/Brussels

## Status Summary

- **Overall status:** In implementation; behavior complete through Phase 5,
  closure gates outstanding.
- **Completed:** 32/38 implementation tasks and 4/6 phase verification gates.
  Checkbox totals across this file: 36 checked, 8 unchecked, 44 total.
- **Outstanding implementation tasks:** 4.5 provider migration generation,
  5.5 manual QA visual verdict, 6.5 full affected-project gates, 6.6 final
  Release build and generated-artifact checks, 6.7 mutation and MAD review,
  6.8 real API/UI surface exercise.
- **Outstanding verification gates:** V4 persistence integration and V6 final
  completion.
- **Current priority:** Restore the five merged `20260828*_Init` catalogs under
  path-scoped approval, then generate the additive
  `AddLegalIdentityAuthority` migrations and rerun provider pending-model
  checks (task 4.5).
- **Next recommended slice:** Phase 4 migration correction, then the Phase 6
  closure gates that depend on it.
- **I-VSD report:**
  `islamic-value-sensitive-design/i-vsd-branding-legal-identity-authority.md`
- **I-VSD reviewed input revision:**
  `sha256:2364e821f8455789cc00fe1c5f6c134c07b57e1db861a1ac6aaea607db2bfcb5`
- **I-VSD status / disposition:** Current / plan-aligned.
- **CTO review:** Not reviewed.
- **User approval:** Approved on 2026-08-28 for this full breaking-change
  workstream and end-to-end implementation.

## Implementation Maintenance Rules

- Read the full workstream once at implementation start. On resume, read context
  and tasks first, then only the current plan section.
- Mark a substantial task `IN PROGRESS` only while it is actively spanning
  edits or a handoff; reconcile it immediately when its acceptance evidence
  passes.
- Every behavioral Green task is blocked by its named Red task. A Red task is
  complete only after the new test fails for the intended missing behavior.
- Use one focused TUnit class slice during Red/Green work. Do not repeatedly run
  unchanged full projects.
- Phase verification runs once after all phase tasks. Full affected projects,
  build, manual QA, mutation testing, and MAD review run at closure.
- Use `apply_patch` for edits, `read` for inspection, LSP for symbols and
  diagnostics, generator commands for migrations and clients, and monitors for
  observable long-running state.
- Never hand-edit generated migrations, snapshots, OpenAPI, or NSwag client.
- Do not add backward-compatibility aliases, dual reads/writes, deprecated
  properties, or fallback identity substitution.
- Update this file immediately after each task; update context at phase exits,
  blockers, strategy changes, baseline results, and handoffs.

## Phase 0 — Baseline And Boundaries

- [x] **0.1 Establish the clean Release build baseline**
  - **Plan requirements:** all; precondition only.
  - **Files:** no product edits.
  - **Action:** run `dotnet build --configuration Release --verbosity quiet`
    once before the first product edit.
  - **Acceptance:** exit code 0, or a precisely recorded pre-existing failure in
    context before any product source change.

- [x] **0.2 Confirm exact symbol and caller boundaries with LSP**
  - **Plan requirements:** BLIA-R1 through BLIA-R8.
  - **Files/symbols:** `TenantCreationRequest`, all tenant creation callers,
    `TransitionControlPlaneTenantLifecycleCommandHandler`,
    `PaidOrderAcceptanceSnapshot.Create`, public experience mapping,
    tenant-settings HAL/controller, generated client target.
  - **Acceptance:** exact definitions/references are recorded in context; no
    planned edit depends on an unread or missing symbol.

- [x] **0.3 Record the development migration cut strategy**
  - **Plan requirements:** BLIA-R6, BLIA-R8.
  - **Files:** context only unless the repository already owns a migration
    runbook section that needs updating later.
  - **Acceptance:** retain the five merged `Init` migrations; record the exact
    PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL corrective-generation
    commands; require development database recreation; do not add nullable
    legacy behavior, inferred backfill, or touch Data Protection/privacy
    authority catalogs.

## Phase 1 — Domain And Instance Authority Contracts

- [x] **1.0 SCAFFOLD: Add compile-safe identity contract signatures**
  - **Files:** new payload/value/readiness code signatures and instance identity
    contract with deliberately non-satisfying behavior.
  - **Acceptance:** Domain/Application test projects compile; no readiness
    scenario can pass before the Red tests; no persistence, API, or UI behavior
    is introduced.

- [x] **1.1 RED: Specify directory-operator payload and readiness invariants**
  - **Scenarios:** BLIA-R1 single organization; BLIA-R3 incomplete/corrupt;
    BLIA-R9 telemetry-safe reasons.
  - **Files:** new focused Domain unit tests for payload normalization,
    operator-kind codes, ISO country code, URI/email bounds, capability-specific
    required fields, and immutable readiness reasons.
  - **Acceptance:** the focused test class compiles and fails behaviorally
    against the scaffold for normalization, the capability matrix, malformed
    field codes, and immutable reasons; no production behavior is added here.
  - **Verification:** one sliced Domain test command for the new class.

- [x] **1.2 GREEN: Implement directory-operator Domain contracts**
  - **Blocked by:** 1.1.
  - **Files:** `SettingsDocumentKeys`, new payload/defaults, normalized value
    object, closed operator-kind/readiness codes, and readiness rules under
    `Explore.Domain`.
  - **Acceptance:** draft payloads can be represented; valid capability profiles
    are constructed only through normalized factories; collections/reasons are
    immutable; focused tests pass without exception suppression.
  - **Verification:** rerun the exact 1.1 slice once.

- [x] **1.3 RED: Specify general instance operator startup validation**
  - **Scenarios:** BLIA-R5 tenant footer, BLIA-R7 valid/incomplete startup,
    BLIA-R8 configuration cut.
  - **Files:** focused Application/hosting option-validation tests.
  - **Acceptance:** tests fail because general instance identity is still
    bundled in `PaidCheckoutGovernanceOptions` and old keys remain accepted.
  - **Verification:** one sliced owning test class.

- [x] **1.4 GREEN: Split general instance identity from checkout governance**
  - **Blocked by:** 1.3.
  - **Files:** new `IInstanceOperatorIdentity`/options/validator; payment-specific
    checkout options; DI/host registration; configuration binding;
    `.env.example` schema later finalized in task 6.3.
  - **Acceptance:** general identity validates independently; paid activation
    composes both contracts; old operator fields/keys are not read; no hard-coded
    values or secrets are introduced.
  - **Verification:** rerun the exact 1.3 slice once.

### Phase 1 Verification Gate

- [x] **V1 Verify Domain authority contracts**
  - Run once:
    `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`
  - Update context with pass count or caused/pre-existing failures.

## Phase 2 — Atomic Provisioning And Lifecycle Readiness

- [x] **2.1 RED: Specify atomic mandatory-document tenant creation**
  - **Scenarios:** BLIA-R2 success and document failure rollback.
  - **Files:** `TenantCreationServiceTests` plus focused caller tests for direct,
    single-tenant, managed-provider, and onboarding creation.
  - **Acceptance:** tests require both explicit typed seeds, atomic rollback,
    and Active rejection for incomplete identity across direct creation,
    managed provisioning, configuration manifest, and single-tenant
    onboarding; tests fail before production changes.
  - **Verification:** one sliced `TenantCreationServiceTests` command.

- [x] **2.2 GREEN: Seed directory-operator identity in every creation path**
  - **Blocked by:** 2.1.
  - **Files:** two explicit tenant-creation document seed contracts,
    `TenantCreationRequest`, `TenantCreationService`, direct creation,
    single-tenant onboarding, managed-provider provisioning, and configuration
    manifest creation callers.
  - **Acceptance:** all production callers converge on the common service; one
    transaction owns tenant and documents; Active requests validate the
    supplied identity before any write; cache invalidation occurs only after
    commit; no post-create repair is required.
  - **Verification:** rerun the exact 2.1 slice once.

- [x] **2.3 RED: Specify capability readiness and Active rejection**
  - **Scenarios:** BLIA-R3 activation incomplete and corrupt active identity;
    BLIA-R4 cross-tenant isolation.
  - **Files:** new readiness evaluator tests and
    `TransitionControlPlaneTenantLifecycleCommandHandlerTests`.
  - **Acceptance:** tests fail because the lifecycle handler currently checks
    only transition/capacity and no shared readiness evaluator exists.
  - **Verification:** one sliced Application test command covering the new
    readiness class or lifecycle class.

- [x] **2.4 GREEN: Implement readiness evaluator and lifecycle gate**
  - **Blocked by:** 2.3.
  - **Files:** Application readiness contract/result/service, DI registration,
    lifecycle handler, failure-code mapping, bounded PII-free logs/metrics.
  - **Acceptance:** readiness is tenant-filtered, read-only, capability-specific,
    and immutable; Active rejection makes no state/audit write; reason codes are
    stable and payload-free.
  - **Verification:** rerun the exact 2.3 slice once.

- [x] **2.5 RED: Specify truthful tenant-onboarding Identity completion**
  - **Scenarios:** BLIA-R2 atomic provisioning; BLIA-R3 activation/public
    readiness.
  - **Files:** `CompleteTenantOnboardingCommandHandlerTests`, onboarding DTO and
    Blazor flow tests.
  - **Acceptance:** tests fail because branding alone currently marks
    `Identity` complete; incomplete legal identity cannot complete onboarding.

- [x] **2.6 GREEN: Integrate identity into live tenant onboarding**
  - **Blocked by:** 2.5.
  - **Files:** tenant onboarding request/validator/handler/service/UI and tests.
  - **Acceptance:** identity fields are collected separately from branding,
    applied atomically, and the `Identity` step is complete only when Activation
    readiness passes.

### Phase 2 Verification Gate

- [x] **V2 Verify atomic provisioning and readiness**
  - Run once:
    `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - Update context before moving to API work.

## Phase 3 — CQRS, HAL, API, And Generated Client

- [x] **3.1 RED: Specify directory-operator query and patch semantics**
  - **Scenarios:** BLIA-R4 authorized update and stale/cross-tenant rejection.
  - **Files:** new Application query/patch handler tests and validators.
  - **Acceptance:** tests cover projection, presence-aware merge, explicit clear
    rules, normalization, concurrency conflict, tenant context, cache
    invalidation, and immutable HAL capability metadata; tests fail first.
  - **Verification:** one sliced Application handler test command.

- [x] **3.2 GREEN: Implement directory-operator CQRS and validation**
  - **Blocked by:** 3.1.
  - **Files:** DTOs, patch presence wrappers, manual validators, requests,
    handlers, settings-document service/repository calls, cache invalidation.
  - **Acceptance:** handlers remain single-purpose and cancellation-aware;
    repositories return documents/entities; absent versus explicit-null behavior
    is correct; no controller validation is added.
  - **Verification:** rerun the exact 3.1 slice once.

- [x] **3.3 RED: Specify HAL and HTTP contract behavior**
  - **Scenarios:** BLIA-R4 authorized update and cross-tenant rejection;
    BLIA-R8 contract cutover.
  - **Files:** API integration and HAL policy/assembler tests.
  - **Acceptance:** tests require authorized GET/PATCH, RFC 7807 failures,
    idempotent/stamp behavior, and `self` plus permission-matched edit relation;
    tests fail first.
  - **Verification:** one sliced API integration test class.

- [x] **3.4 GREEN: Implement HAL endpoint and resource assembly**
  - **Blocked by:** 3.3.
  - **Files:** `TenantSettingsDocumentsController`, authorization descriptors
    and registry, `ExploreJsonContext`, HAL policy/assembler/schema catalog,
    ProblemDetails mapping, and DI registration.
  - **Acceptance:** controller only dispatches/assembles/shapes; GET/PATCH use
    server tenant context; authorization and HAL relations agree; public cache
    tag is evicted once after success.
  - **Verification:** rerun the exact 3.3 slice once.

- [x] **3.5 Regenerate OpenAPI and NSwag client**
  - **Blocked by:** 3.4.
  - **Files:** checked-in OpenAPI and
    `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
  - **Acceptance:** repository generator exits 0; generated diff contains only
    intended identity route/DTO/prose-removal changes; no generated file is
    manually patched; AOT/serialization architecture tests remain valid.
  - **Verification:** run the repository's exact OpenAPI/client generation
    verification command once.

### Phase 3 Verification Gate

- [x] **V3 Verify API and generated contract**
  - Run once:
    `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - Record operation/component diff evidence in context.

## Phase 4 — Public And Paid Structured Disclosure

- [x] **4.1 RED: Specify structured anonymous public disclosure**
  - **Scenarios:** BLIA-R5 tenant footer and white-label presentation; BLIA-R3
    corrupt active identity.
  - **Files:** public experience handler/service tests and anonymous API tests.
  - **Acceptance:** tests require separate cosmetic branding, tenant operator,
    and instance operator facts; settings and shell return non-cacheable RFC
    7807 `503` with `tenant_identity_unavailable`; no fallback or prose property
    is accepted; cached and uncached paths fail first.
  - **Verification:** one sliced owning test class.

- [x] **4.2 GREEN: Compose structured public tenant and instance identity**
  - **Blocked by:** 4.1.
  - **Files:** public experience DTOs/handler/shell mapping, readiness
    integration, instance identity mapping, cache keys/tags, RFC 7807/unavailable
    mapping.
  - **Acceptance:** one composed response load; only public fields leave the
    server; white-labeling affects visuals but not role disclosure; corruption
    emits bounded reason telemetry; only successful `200` output is cached.
  - **Verification:** rerun the exact 4.1 slice once.

- [x] **4.3 RED: Specify immutable paid multi-party acceptance**
  - **Scenarios:** BLIA-R6 success and later identity changes; BLIA-R3 paid
    readiness failure; BLIA-R9 telemetry.
  - **Files:** Domain snapshot tests, `PaidOrderAcceptanceServiceTests`,
    checkout composition tests, persistence mapping/lifecycle tests.
  - **Acceptance:** tests require structured tenant identity values, identity
    document ID/revision, separate structured organizer/recipient and
    instance/provider facts, historical immutability, paid-publication failure,
    and fail-closed checkout readiness; tests fail first.
  - **Verification:** one sliced Domain or Application class per red seam,
    without running the full project.

- [x] **4.4 GREEN: Persist and map structured paid disclosure**
  - **Blocked by:** 4.3.
  - **Files:** `OrganizerPaymentRecipientSnapshot`,
    `PaidOrderAcceptanceSnapshot`, validation/factory, EF configuration,
    publication preflight/command, activation service, acceptance service/DTO
    mapping, checkout composition, serialization, and idempotent replay.
  - **Acceptance:** structured facts are normalized/bounded and immutable;
    current settings are read once; existing OrganizerDirect, provider fencing,
    policy revisions, money arithmetic, and idempotency stay unchanged.
  - **Verification:** rerun the exact 4.3 slices once.

- [ ] **4.5 Generate and inspect every provider migration**
  - **Blocked by:** 4.4 and task 0.3.
  - **Files:** provider migration directories and model snapshots only through
    the repository migration generator.
  - **Acceptance:** all affected provider migrations generate; `Up`/`Down` and
    column bounds are correct; pending-model tests pass; no artifact is
    hand-edited; no compatibility backfill/runtime fallback is added.
  - **Verification:** run provider generation plus the targeted migration
    lifecycle/pending-model command once.

### Phase 4 Verification Gate

- [ ] **V4 Verify persisted disclosure across providers**
  - Run once:
    `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - Record real-engine/provider exclusions separately; do not claim unrun
    engines passed.

## Phase 5 — Accessible Tenant Administration And Footer

- [x] **5.1 RED: Specify legal-identity admin service and component**
  - **Scenarios:** BLIA-R4 authorized/stale update; BLIA-R1 locked branding
    independence.
  - **Files:** generated-client adapter/service tests and new bUnit component
    tests.
  - **Acceptance:** tests require HAL-gated editability, object-initialized
    generated records, stamp chaining, conflict reload, field semantics,
    validation summary, live status, LTR islands, keyboard/focus behavior, and
    no timing sleeps; tests fail first.
  - **Verification:** one sliced bUnit class.

- [x] **5.2 GREEN: Implement tenant legal-identity administration**
  - **Blocked by:** 5.1 and 3.5.
  - **Files:** admin model/service/component/CSS, parent settings page,
    localization resources.
  - **Acceptance:** MudBlazor v9 and repository wrappers are used; no direct API
    call or role/claim inference; field groups are understandable; conflicts
    preserve user intent; URL/email inputs are directionally safe.
  - **Verification:** rerun the exact 5.1 slice once.

- [x] **5.3 RED: Specify role-separated public footer**
  - **Scenarios:** BLIA-R5 tenant footer/white-label; BLIA-R7 complete operator;
    BLIA-R8 prose removal.
  - **Files:** `FooterTests` and any public-shell component tests.
  - **Acceptance:** tests assert structured names/URLs/roles and DOM semantics,
    not exact prose; the removed disclaimer cannot be consumed; fail first.
  - **Verification:** one sliced `FooterTests` command.

- [x] **5.4 GREEN: Render tenant and instance operator footer sections**
  - **Blocked by:** 5.3 and 4.2.
  - **Files:** `Footer.razor`, isolated CSS/templates if required, public service
    mapping, localization.
  - **Acceptance:** roles are visually and semantically separate; required
    instance disclosure survives cosmetic white-labeling; link text/URLs are
    safe; layout is RTL/logical and responsive.
  - **Verification:** rerun the exact 5.3 slice once.

- [ ] **5.5 Manually QA administration and public presentation**
  - **Blocked by:** 5.2 and 5.4.
  - **Surface:** real browser if available, otherwise the closest rendered
    Blazor surface.
  - **Acceptance:** inspect desktop/mobile widths, 200% zoom/reflow, keyboard
    only, focus/error announcements, light/dark, one RTL locale, complete and
    incomplete states; capture evidence and fix defects before V5.

### Phase 5 Verification Gate

- [x] **V5 Verify Blazor behavior**
  - Run once:
    `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
  - Record visual/manual evidence in context.

## Phase 6 — Breaking Cutover, Documentation, And Closure

- [x] **6.1 Verify architecture absence checks for obsolete contracts**
  - **Scenarios:** BLIA-R8 contract cutover.
  - **Files:** architecture/contract inventory tests.
  - **Acceptance:** repository-wide checks pass only when every old scalar
    tenant identity path, `PaidEventDisclaimerFormatter`,
    `PaidEventDirectoryDisclaimer`, old operator option field/key,
    dual-read/write, and compatibility alias is absent.
  - **Verification:** one sliced Architecture test class.

- [x] **6.2 Remove any remaining obsolete identity and fallback paths**
  - **Blocked by:** all replacement Green tasks.
  - **Files:** old formatter/tests, scalar setting definitions/resolution,
    DTO/prose properties, old option fields/config keys, stale mappings/callers.
  - **Acceptance:** repository-wide symbol/config search finds no runtime use;
    replacement structured contracts compile; no test is weakened or skipped.
  - **Verification:** run the 6.1 architecture slice once after removal.

- [x] **6.3 Update canonical configuration, tenancy, payment, footer, API, and domain docs**
  - **Blocked by:** runtime contract stabilization.
  - **Files:** `.env.example`, `CONFIGURATION.md`, `MULTI_TENANCY.md`,
    `PAYMENTS.md`, ADR-022 if decision semantics changed, `FOOTER_MANAGEMENT.md`,
    `DOMAIN.md`, `AUTHORIZATION.md`, `API.md`, `API_CHANGELOG.md`, contract
    inventory, `schemas/islamu-event.md`, operations/self-hosting repair
    guidance.
  - **Acceptance:** docs state separate roles, capability readiness,
    fail-closed corruption, startup keys, structured contracts, generated
    migration/client ownership, and no fallback; no unverified legal claim is
    presented as law.

- [x] **6.4 Create and validate the breaking change fragment**
  - **Files:** append-only `docs/releases/changes/CHG-2026-NNNN.yaml`.
  - **Acceptance:** valid Tier 2 schema; public scopes `onboarding` and/or
    `registration`; operator and API breaks are explicit; `ReleaseInputPolicy`
    validates; future terminal commit footer uses the same `Change-Id` plus
    `BREAKING CHANGE:` if the user authorizes commits.

- [ ] **6.5 Run affected full project gates once**
  - **Commands:** the affected project list in plan Section 7, each via
    `dotnet test --project ... --configuration Release --verbosity quiet`.
  - **Acceptance:** every exit code is 0 or a pre-existing unrelated failure is
    evidenced without suppression; no unchanged full suite is rerun.

- [ ] **6.6 Run final Release build and generated-artifact checks**
  - **Commands:** `dotnet build --configuration Release --verbosity quiet`,
    pending-model/provider migration checks, OpenAPI/client generation diff
    check, `git diff --check`.
  - **Acceptance:** all exit 0; generated output is current; no manual generated
    edit is present.

- [ ] **6.7 Run Tier 0/1 adversarial, mutation, and MAD review gates**
  - **Coverage:** cross-tenant attempts, malformed/corrupt document, concurrent
    patch/activation, checkout replay after identity changes, startup
    misconfiguration, no-PII telemetry, provider migrations.
  - **Acceptance:** mutation score exceeds the repository threshold for changed
    critical logic; anonymized MAD review reaches pass or all findings are fixed
    and re-reviewed.

- [ ] **6.8 Exercise the real API and UI surfaces**
  - **API:** happy GET/PATCH, stale stamp, unauthorized/cross-tenant, activation
    incomplete/complete, anonymous public settings, paid checkout composition,
    `--help`/equivalent contract discovery where applicable.
  - **UI:** admin edit/conflict and public footer in matching browser/rendered
    surface.
  - **Acceptance:** observed output satisfies BLIA-R1 through BLIA-R9; defects
    are fixed and focused tests rerun before closure.

- [x] **6.9 Reconcile I-VSD, plan, context, tasks, and durable findings**
  - **Acceptance:** I-VSD is `current` / `plan-aligned`; every finding and
    mitigation maps to completed tasks or an explicit qualified-authority
    escalation; task counts/checks are accurate; context contains final
    validation and handoff; plan changes only if strategy changed; record a
    journal finding only if a non-obvious reusable repository behavior emerged.

### Phase 6 Verification Gate

- [ ] **V6 Final completion gate**
  - Confirm Definition of Done in plan Section 15.
  - Confirm no open task, test failure caused by this change, unverified manual
    surface, compatibility shim, or unreviewed Tier 0/1 finding remains.

## Why Each Unchecked Gate Is Still Open

- **4.5 and V4 — provider migrations.** The worktree currently deletes the five
  merged `20260828*_Init` catalogs (PostgreSQL `20260828151542`, SQLite
  `20260828150101`, SQL Server `20260828150652`, MariaDB `20260828150932`,
  MySQL `20260828151228`) and adds replacement `20260829*_Init` catalogs. That
  replacement is not the approved strategy and is not acceptable as-is. The
  merged history must be restored under path-scoped approval, after which the
  additive `AddLegalIdentityAuthority` migrations are generated per provider and
  the pending-model checks are rerun. Until then the persistence integration
  gate cannot be claimed.
- **5.5 — manual QA visual verdict.** Component and service behavior is covered
  by passing bUnit slices, but no real browser was available and Standalone
  host attempts were blocked by inherited provider and privacy startup paths
  before binding. The desktop/mobile, zoom/reflow, light/dark, and RTL visual
  verdicts remain unrendered.
- **6.5 — full affected-project gates.** Blazor client and the focused API,
  Application, Domain, and architecture slices have run. The persistence
  integration project depends on the migration correction, so the full affected
  list has not completed in one pass.
- **6.6 — final build and generated-artifact checks.** The final Release build,
  provider pending-model checks, and generation diff check must rerun *after*
  the migration restoration, not before it.
- **6.7 — mutation and MAD review.** `tests/Event.Application.LegalIdentity.MutationTests`
  exists with its `stryker-config.json`, but no Stryker run has been executed
  and no result is recorded. The anonymized MAD review verdict is likewise not
  yet issued.
- **6.8 — real API and UI surfaces.** Blocked by the same missing browser and
  blocked Standalone startup described in 5.5.
