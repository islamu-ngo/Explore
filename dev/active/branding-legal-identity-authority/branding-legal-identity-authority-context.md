<!-- ABOUTME: Resumable working context for tenant and instance legal-identity implementation. -->
<!-- ABOUTME: Tracks approved scope, current progress, baseline evidence, blockers, and handoff state. -->

# Branding And Legal-Identity Authority — Context

Last Updated: 2026-08-29 Europe/Brussels

## Review State

- **I-VSD report:**
  `islamic-value-sensitive-design/i-vsd-branding-legal-identity-authority.md`
- **I-VSD reviewed input revision:**
  `sha256:2364e821f8455789cc00fe1c5f6c134c07b57e1db861a1ac6aaea607db2bfcb5`
- **I-VSD status / disposition:** Current / plan-aligned.
- **CTO review:** Not reviewed.
- **User approval:** Approved on 2026-08-28 for the full legal-identity scope,
  intentional breaking changes, and end-to-end implementation.

## SESSION PROGRESS (2026-08-28 Europe/Brussels)

### COMPLETED

- Verified current typed tenant-branding, tenant creation, single-tenant
  synchronization, lifecycle activation, public footer, and paid acceptance
  behavior from repository sources and tests.
- Confirmed current typed documents have no instance/scalar fallback.
- Confirmed public branding can become blank while the paid disclaimer falls
  back to literal `ISLAMU`, contradicting current documentation.
- Confirmed branding locks govern presentation editability and do not establish
  tenant legal authority.
- Confirmed tenant activation currently lacks legal-identity readiness.
- Confirmed paid acceptance distinguishes instance operator and organizer
  merchant but snapshots tenant identity only as prose derived from branding.
- Audited adjacent active/paused workstreams. This work is a focused successor,
  not a revival of `tenant-onboarding-enterprise`.
- Completed criticality/I-VSD intake and received the user's full-scope
  decision.
- Created the approved architectural plan.
- Completed two independent repository-grounded architecture audits.
- Confirmed three direct Active creation paths bypass the lifecycle handler,
  tenant onboarding falsely marks `Identity` complete after branding only, the
  public endpoints have no fail-closed HTTP contract, and paid acceptance does
  not persist the current tenant prose disclaimer.
- Selected the repository-native typed-document design over a proposed
  revision aggregate. Immutable buyer history belongs in
  `PaidOrderAcceptanceSnapshot`; adding address/phone/publication-history
  concepts would exceed the approved minimal public field set.
- Added the Tier 0 `legal-identity-authority-change` contribution intent to own
  the complete source, generated-artifact, test, and documentation blast radius.

- Implemented Phases 1 through 5: Domain identity contracts, instance-identity
  split, atomic provisioning, readiness gating, CQRS and HAL surface, public
  and paid structured disclosure, tenant administration UI, and the
  role-separated footer. Regenerated OpenAPI and the NSwag client.
- Updated canonical configuration, tenancy, payment, footer, API, authorization,
  domain, operations, and self-hosting documentation, and authored the breaking
  change fragment.

### IN PROGRESS

- Closure gates only. No further behavior work is queued.

### NEXT

1. Obtain path-scoped approval to restore the five merged `20260828*_Init`
   catalogs, then generate the additive `AddLegalIdentityAuthority` migrations
   per provider and rerun the pending-model checks (task 4.5).
2. Run the persistence integration gate (V4) once migrations are correct.
3. Run Stryker for `Event.Application.LegalIdentity.MutationTests` and record
   the score (task 6.7).
4. Obtain the anonymized MAD verdict and the manual visual verdict.
5. Rerun the final full affected-project gates and Release build after the
   migration correction (tasks 6.5 and 6.6), then close V6.

### BLOCKERS

- **Migration history replacement.** The worktree replaced the merged
  `20260828*_Init` catalogs with new `20260829*_Init` catalogs. This is not the
  approved strategy and must not be treated as acceptable. Restoration is
  pending path-scoped approval and blocks 4.5, V4, 6.5, 6.6, and V6.
- **No real browser.** Manual visual QA and real-surface API/UI exercise cannot
  complete. Standalone host attempts were blocked by inherited provider and
  privacy startup paths before binding.

## Quick Resume

1. Read this context and
   `branding-legal-identity-authority-tasks.md`.
2. Read only the current phase and referenced decisions from
   `branding-legal-identity-authority-plan.md`.
3. Start from the first unchecked task in the current phase.
4. Preserve Red-before-Green order and update the checkbox immediately when the
   verification criterion passes.
5. Update this context for baseline results, blockers, strategy changes, phase
   exits, and handoffs.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Responsibility |
|---|---|---|---|
| `src/Explore.Domain/Settings/Documents/SettingsDocumentKeys.cs` | Existing | Domain | Register canonical tenant document key |
| `src/Explore.Domain/Settings/Documents/Payloads/TenantDirectoryOperatorIdentitySettings.cs` | New | Domain | Persisted draft/public identity payload |
| `src/Explore.Domain/ValueObjects/TenantDirectoryOperatorIdentity.cs` | New | Domain | Normalized capability-valid identity |
| `src/Explore.Domain/Services/Tenants/TenantDirectoryOperatorReadinessRules.cs` | New | Domain | Closed readiness requirements and reason codes |
| `src/Explore.Application/Contracts/Services/IInstanceOperatorIdentity.cs` | New | Application | General startup-governed instance identity |
| `src/Explore.Application/Contracts/Services/ITenantDirectoryOperatorReadinessEvaluator.cs` | New | Application | Tenant capability readiness boundary |
| `src/Explore.Application/Services/TenantDirectoryOperatorReadinessEvaluator.cs` | New | Application | Resolve and evaluate current tenant identity |
| `src/Explore.Application/Services/TenantCreationService.cs` | Existing | Application | Atomically create tenant and mandatory documents |
| `src/Explore.Application/Features/Tenants/Handlers/Commands/CreateTenant/CreateTenantCommandHandler.cs` | Existing | Application | Direct creation must pass identity readiness for Active |
| `src/Explore.Application/Features/ManagedProviderProvisioning/Handlers/Commands/EnsureManagedProviderClientProvisionedCommandHandler.cs` | Existing | Application | Route managed creation through the common boundary |
| `src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs` | Existing | Application | Collect default-tenant identity and enforce Active readiness |
| `src/Explore.Application/Features/TenantOnboarding/Handlers/Commands/CompleteTenantOnboardingCommandHandler.cs` | Existing | Application | Stop marking Identity complete from branding alone |
| `src/Explore.Application/Features/ControlPlane/Handlers/Commands/TransitionControlPlaneTenantLifecycleCommandHandler.cs` | Existing | Application | Reject Active when identity is incomplete |
| `src/Explore.Application/Features/TenantSettingsDocuments/` | Existing | Application | Add directory-operator GET/PATCH CQRS surface |
| `src/Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs` | Existing | Application | Compose structured tenant/instance disclosures |
| `src/Explore.Application/Services/Registration/PaidOrderAcceptanceService.cs` | Existing | Application | Snapshot structured role identities |
| `src/Explore.Domain/PaidOrderAcceptanceSnapshot.cs` | Existing | Domain | Persist immutable accepted identity facts |
| `src/Explore.Persistence/` | Existing | Persistence | Configure snapshot fields and generate provider migrations |
| `src/Explore.API/Controllers/TenantSettingsDocumentsController.cs` | Existing | API | Expose authorized HAL GET/PATCH routes |
| `src/Explore.Application/Authorization/ResourceDescriptors.cs` | Existing | Application | Register identity authorization descriptor |
| `src/Explore.Application/Authorization/ResourceDescriptorRegistry.cs` | Existing | Application | Register identity HAL resource type |
| `src/Explore.Application/Serialization/ExploreJsonContext.cs` | Existing | Application | Register DTO, patch, payload, and HAL JSON metadata |
| `src/Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs` | Existing | API | Register identity HAL schema |
| `src/Explore.API/Extensions/HateoasAssemblerRegistration.cs` | Existing | API | Register identity HAL policy/assembler |
| `src/Explore.Blazor.Client/Pages/Admin/Tenant/Components/` | Existing/New | Blazor | Add accessible legal-identity administration |
| `src/Explore.Blazor.Client/Layout/Footer.razor` | Existing | Blazor | Render role-separated public disclosures |
| `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` | Generated | Client | Regenerate from OpenAPI; never hand-edit |
| `.env.example` | Existing | Operations | Document non-secret instance operator schema |
| `docs/MULTI_TENANCY.md` | Existing | Docs | Canonical identity/readiness behavior |
| `docs/PAYMENTS.md` | Existing | Docs | Structured multi-party acceptance |
| `docs/FOOTER_MANAGEMENT.md` | Existing | Docs | Public tenant/instance legal sections |

Exact new paths may follow an already-established neighboring namespace if LSP
or source inspection finds a more precise owner before the first edit. Such a
path-only adjustment does not change the plan; a layer or contract change does.

## Key Decisions

1. Cosmetic branding, tenant directory-operator identity, instance operator
   identity, and organizer merchant identity remain separate.
2. The new tenant document key is
   `tenant.directory_operator_identity`.
3. The persisted payload may be incomplete while Provisioning; a normalized
   value is produced only for a named readiness capability.
4. Tenant creation writes branding and directory-operator documents in one
   transaction.
5. Active transition and paid/public composition fail closed with closed reason
   codes.
6. General instance identity is split from payment-specific checkout
   governance and remains startup-controlled.
7. Public and paid contracts expose structured role facts, not a canonical
   English disclaimer.
8. Paid acceptance snapshots exact tenant identity values and source revision.
9. Instance branding locks never replace or lock tenant accountable identity.
10. Old scalar identity semantics, literal fallbacks, prose DTOs, and old
    operator configuration keys are removed in one cut.
11. Activation and public disclosure require every field except
    `RegistrationIdentifier` and `TermsUrl`; paid commerce additionally
    requires `TermsUrl`.
12. Missing/corrupt public identity returns non-cacheable RFC 7807 `503` with
    code `tenant_identity_unavailable` from both settings and shell.
13. Instance operator identity uses fail-fast startup validation before
    onboarding is served.
14. Existing merged `Init` migrations remain; five corrective application
    migrations are generated after the final model and old development
    databases are recreated.

## Constraints And Rules To Remember

- Tier 0/1 criticality requires adversarial tests, real provider migration
  evidence, mutation testing above the repository threshold, MAD review, and a
  comprehensive teaching handoff.
- No hand-edited EF migration or generated client.
- No hard-coded configuration values or test secrets.
- No compatibility aliases, dual reads, or dual writes.
- Repositories return entities/documents; handlers map DTOs.
- Validators are manually instantiated.
- HAL controls UI edit affordances; the API remains the authorization boundary.
- Anonymous public surfaces expose only explicitly public identity fields.
- Logs/metrics contain reason codes and approved identifiers, never identity
  payloads.
- No prose-pinning tests and no timing sleeps/polling.
- Preserve OrganizerDirect, connected-account fencing, immutable acceptance,
  integer money, and payment idempotency.
- Use `apply_patch` for all edits and `read` for file inspection.

## Validation Baseline

- **Worktree before planning edits:** clean.
- **Planning evidence digest:**
  `2364e821f8455789cc00fe1c5f6c134c07b57e1db861a1ac6aaea607db2bfcb5`.
- **Product Release build baseline:** passed on 2026-08-28 in 12.96 seconds with
  402 warnings and 0 errors. The warnings are pre-existing analyzer output; no
  product source had been edited.
- **Phase policy:** one Release build and at most one fastest relevant
  non-browser project gate at each phase exit; full affected projects only at
  completion.
- **Planning-document check:** plan/context/tasks/I-VSD passed
  `git diff --check`.
- **Architecture project check:** 518/532 passed, 13 failed, 1 skipped. Focused
  `AgentContextPolicyTests` proved its two failures are pre-existing repository
  violations in `AGENTS.md`, `.agents/CONTEXT_ENGINEERING.md`,
  `.agents/skills/implementation-plan/SKILL.md`, and
  `.agents/skills/senior-cto-feedback/SKILL.md`; none names or reads this
  workstream. Other failures concern existing admissions, authorization,
  privacy inventory, generated contracts, and controller-size ratchets.
- **LSP baseline:** workspace symbol indexing times out because the shared
  workspace includes ignored `.tmp/aws-sdk-net` .NET Framework 4.7.2 projects
  without reference assemblies. Use direct file-level LSP operations where
  possible and AST/source evidence otherwise; do not modify the unrelated
  `.tmp` tree.
- **Intent contract:** focused `AgentContextCriticalityTests` passed 5/5 after
  registering `legal-identity-authority-change`. YAML LSP validation is
  unavailable because `yaml-language-server` is not installed; no external
  package was installed.
- **Post-correction architecture recheck:** unchanged at 532 total, 518 passed,
  13 failed, 1 skipped. No new failure references the legal-identity intent or
  workstream; the same pre-existing admissions, authorization, agent-context,
  privacy-inventory, generated-contract, and controller-ratchet debt remains.
- **Corrected plan digest:**
  `fbf862fe9270ca2420a3c013e982d4bfd7447c1489bf32f8e08fb4d74e6ec84e`.

## Exact Boundary Evidence

- `TenantCreationRequest` has exactly two runtime call sites:
  `CreateTenantCommandHandler.cs:108` and
  `ApplyConfigurationManifestCommandHandler.cs:268`.
- `EnsureManagedProviderClientProvisionedCommandHandler.cs:573` and
  `CompleteInstanceOnboardingCommandHandler.cs:271` create `Tenant` directly
  and can currently write Active without the common creation service.
- Existing lifecycle activation is owned by
  `TransitionControlPlaneTenantLifecycleCommandHandler`; the new shared
  readiness policy must also run inside the common creation boundary because
  three creation flows do not dispatch that handler.
- Paid acceptance construction is owned by
  `PaidOrderAcceptanceService.cs:268` and
  `PaidOrderAcceptanceSnapshot.Create`; organizer provider authority is pinned
  by `OrganizerPaymentRecipientSnapshot`, while current merchant identity is
  only `MerchantDisclosureText`.
- Public failure mapping must cover both
  `PublicExperienceController.GetSettings` and `GetShell`; the latter is
  output-cached.
- The identity API registry set is `ResourceDescriptors`,
  `ResourceDescriptorRegistry`, `ExploreJsonContext`,
  `HalOpenApiSchemaCatalog`, and `HateoasAssemblerRegistration`.

## Migration Cut Strategy

- Preserve the merged PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL
  `20260828*_Init` application migrations.
- After the final EF model, stage the design-time assemblies exactly as
  `docs/OPERATIONS.md` requires, then run these generated corrective commands:

```bash
env Database__Provider=PostgreSql dotnet ef migrations add AddLegalIdentityAuthority --context ExploreDbContext --project src/Explore.Persistence/Explore.Persistence.csproj --startup-project src/Explore.Persistence/Explore.Persistence.csproj --output-dir Migrations --no-build
env Database__Provider=Sqlite Database__Migrator__Database="$PWD/.artifacts/islamu-event-migrations.db" Database__Host= Database__Port= Database__Username= Database__Password= dotnet ef migrations add AddLegalIdentityAuthority --context ExploreDbContext --project src/Explore.Persistence.Migrations.Sqlite/Explore.Persistence.Migrations.Sqlite.csproj --startup-project src/Explore.Persistence/Explore.Persistence.csproj --output-dir Migrations --no-build
env Database__Provider=SqlServer Database__Port=1433 dotnet ef migrations add AddLegalIdentityAuthority --context ExploreDbContext --project src/Explore.Persistence.Migrations.SqlServer/Explore.Persistence.Migrations.SqlServer.csproj --startup-project src/Explore.Persistence/Explore.Persistence.csproj --output-dir Migrations --no-build
env Database__Provider=MariaDb Database__Port=3306 Database__ServerFlavor=MariaDb Database__ServerVersion=11.4 dotnet ef migrations add AddLegalIdentityAuthority --context ExploreDbContext --project src/Explore.Persistence.Migrations.MariaDb/Explore.Persistence.Migrations.MariaDb.csproj --startup-project src/Explore.Persistence/Explore.Persistence.csproj --output-dir Migrations --no-build
env Database__Provider=MySql Database__Port=3306 Database__ServerFlavor=MySql Database__ServerVersion=8.4 dotnet ef migrations add AddLegalIdentityAuthority --context ExploreDbContext --project src/Explore.Persistence.Migrations.MySql/Explore.Persistence.Migrations.MySql.csproj --startup-project src/Explore.Persistence/Explore.Persistence.csproj --output-dir Migrations --no-build
```

- Recreate existing development databases before applying this breaking cut.
  Do not delete merged migrations, infer old legal identity, backfill old paid
  acceptance, or touch Data Protection/privacy-authority catalogs.

## Phase 1 Verification Evidence

- `TenantDirectoryOperatorIdentityTests`: 4/4 passed after the behavioral Red
  failed 3/4 against the compile-safe scaffold.
- `InstanceOperatorIdentityOptionsTests`: 3/3 passed after the behavioral Red
  failed 3/3 against fail-closed scaffolding.
- `Event.Domain.UnitTests`: 1,073/1,073 passed.
- `Event.Application.UnitTests`: 4,825/4,825 passed.
- `IPaidCheckoutGovernance` now owns only payment operations and activation;
  immutable `IInstanceOperatorIdentity` owns platform legal identity and is
  bound from `Instance:OperatorIdentity` with startup validation.

## Phase 2 Creation Evidence

- `TenantCreationServiceTests`: 4/4 passed.
- `CreateTenantCommandHandlerTests`: 6/6 passed.
- `EnsureManagedProviderClientProvisionedCommandHandlerTests`: 12/12 passed.
- `CompleteInstanceOnboardingCommandHandlerTests`: 9/9 passed.
- `ApplyConfigurationManifestCommandHandlerTests`: 37/37 passed.
- Direct, manifest, managed-provider, and single-tenant onboarding creation now
  converge on `ITenantCreationService`, which creates branding plus
  `tenant.directory_operator_identity` in the caller-owned transaction and
  rejects Active before writes when identity is incomplete.
- `TenantDirectoryOperatorReadinessEvaluatorTests`: 4/4 passed after the
  scaffold Red failed 3/4.
- `TransitionControlPlaneTenantLifecycleCommandHandlerTests`: 11/11 passed;
  activation now rechecks the exact tenant-owned document and makes no status
  or audit write when readiness is missing, incomplete, foreign, or corrupt.
- `CompleteTenantOnboardingCommandHandlerTests`: 6/6 passed; branding no longer
  marks the `Identity` step complete, and the normalized tenant-owned identity
  is created or revised in the same transaction as policies/onboarding state.
- Phase 2 `Event.Application.UnitTests`: 4,837/4,837 passed.

## Phase 3 CQRS And HAL Evidence

- Directory identity query tests: 2/2 passed.
- Directory identity grouped patch tests: 3/3 passed, including partial draft,
  exact tenant binding, stale revision, cache invalidation, and actor audit.
- Directory identity HAL policy tests: 2/2 passed.
- Tenant settings authenticated controller tests: 9/9 passed.
- Tenant settings anonymous controller tests: 3/3 passed.
- `HateoasRegistrationGraphTests`: 7/7 passed.
- `ContractInvariantsTests`: 35/35 passed.
- Build-time OpenAPI generation now opts out only from runtime startup
  validation through the existing `OpenApiGenerationMode`; API, Standalone,
  Blazor, and test runtime hosts retain fail-fast instance identity validation.

## Phase 4 Public And Paid Disclosure Evidence

- Responsibility separation is final: cosmetic branding, tenant
  directory-operator identity, instance operator identity, and organizer
  merchant identity are four distinct contracts. No path substitutes one for
  another and no literal `ISLAMU` fallback remains.
- Onboarding creation is atomic and concurrency-safe. Tenant plus both
  mandatory documents commit in one caller-owned transaction, Active is
  rejected before any write when identity is incomplete, and cache
  invalidation happens only after commit.
- Recipient lineage is exact. `OrganizerPaymentRecipientSnapshot` keeps
  organizer provider authority while `PaidOrderAcceptanceSnapshot` now pins
  structured tenant identity values together with the source document ID and
  revision, so later identity edits cannot rewrite accepted history.
- Telemetry stays payload-free. Readiness and corruption emit closed reason
  codes and approved identifiers only; no identity field values reach logs or
  metrics.
- Public fail-closed behavior is uniform across the cacheable and non-cacheable
  paths: settings and shell both return non-cacheable RFC 7807 `503` with
  `tenant_identity_unavailable`, and only successful `200` output is cached.
- API 503 behavior tests: 2/2 passed.
- Onboarding HTTP mapping test: 1/1 passed.

## Phase 5 Administration And Presentation Evidence

- HAL governs the GET/PATCH authorization story end to end. The controller only
  dispatches, assembles, and shapes; server tenant context drives both verbs;
  the permission-matched edit relation and the authorization decision agree.
- Active-state mutation is serialized by a shared lock through
  `TenantDirectoryOperatorIdentityMutationLockKeys`, so concurrent patch and
  activation cannot interleave into a half-valid Active tenant.
- The Blazor admin surface is fail-closed and accessible: editability comes from
  HAL rather than inferred roles, stale stamps reload without discarding user
  intent, validation and live status are announced, and URL/email inputs render
  as LTR islands inside RTL layouts. No timing sleeps appear in the specs.
- Authorized settings API tests: 12/12 passed.
- Admin component/service tests: 4/4 passed.
- Footer tests: 2/2 passed.
- Ticket selection tests: 9/9 passed.
- Payment panel tests: 25/25 passed.
- Manual visual QA is **not** complete. No real browser was available, and
  Standalone attempts were blocked by inherited provider and privacy startup
  paths before the host could bind.

## Phase 6 Cutover, Deployment, And Documentation Evidence

- Compose and startup wiring is updated: `docker-compose.yml`,
  `src/Explore.API/appsettings.json`, `src/Event.Standalone/appsettings.json`,
  and `.env.example` carry the `Instance:OperatorIdentity` schema. Build-time
  OpenAPI generation opts out of runtime startup validation only through the
  existing `OpenApiGenerationMode`; every runtime host keeps fail-fast
  validation.
- Generated artifacts are deterministic and regenerated, never hand-edited.
  Current hashes:
  - OpenAPI `schemas/openapi_islamu-event.json`:
    `sha256:b32102aee5a724997e784ce3beb6b9b8a11b3a0278e9db1d9c4a58911bb30bc8`
  - Generated client `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`:
    `sha256:1c57d2a097c2c31c9908101d9998c40dc2b61980aa81b6e5e14c7e4a48e09f2e`
- Documentation is updated across `docs/API.md`, `API_CHANGELOG.md`,
  `API_CONTRACT_INVENTORY.md`, `AUTHORIZATION.md`, `CONFIGURATION.md`,
  `DOMAIN.md`, `FOOTER_MANAGEMENT.md`, `MULTI_TENANCY.md`, `OPERATIONS.md`,
  `PAYMENTS.md`, `SECURITY-MODEL.md`, `SELF_HOSTING.md`, `TROUBLESHOOTING.md`,
  ADR-022, and `schemas/islamu-event.md`. The breaking change fragment
  `docs/releases/changes/CHG-01M15E1S6RWXNQ30FB9QAVX6HX.yaml` is authored.

## Full-Suite Residual Failures

- `Explore.Blazor.Client.Tests`: 2,765 passed, 1 failure, 1 skip. The failure
  and the skip are unrelated to legal identity and pre-existing; neither is a
  regression from this workstream.
- The earlier architecture suite run carried unrelated pre-existing failures
  covering admissions, authorization, privacy inventory, generated contracts,
  and controller-size ratchets. Those remain upstream debt and are not
  legal-identity regressions.
- No failure observed so far is caused by this change.

## Migration Strategy Correction

The approved strategy stands unchanged: **preserve the merged
`20260828*_Init` history and add additive `AddLegalIdentityAuthority`
migrations on top of it.**

The current worktree does not match that strategy. It deletes the five merged
catalogs and adds replacement `20260829*_Init` catalogs:

| Provider | Merged catalog (deleted) | Replacement (added) |
|---|---|---|
| PostgreSQL | `20260828151542_Init` | `20260829001843_Init` |
| SQLite | `20260828150101_Init` | `20260829002111_Init` |
| SQL Server | `20260828150652_Init` | `20260829002119_Init` |
| MariaDB | `20260828150932_Init` | `20260829002127_Init` |
| MySQL | `20260828151228_Init` | `20260829002134_Init` |

This replacement is accidental and is **not** acceptable. Restoring the merged
catalogs is path-scoped work that still needs explicit approval before the
additive migrations are generated and the provider pending-model checks rerun.

## Current Known Risks / Unknowns

- Qualified counsel may require additive public fields or different labels;
  engineering cannot decide jurisdiction-specific wording.
- The paid snapshot schema expansion touches all generated providers and is the
  highest persistence risk.
- Splitting instance identity from checkout governance intentionally invalidates
  old environment keys; all hosts must update together.
- Public fail-closed behavior can make one tenant unavailable when identity is
  corrupt. This is intentional but requires bounded operator telemetry and a
  documented repair path.
- The running architecture reviewer may identify a concrete contradiction. If
  so, update the plan before product edits.
- Concurrent unrelated workspace changes appeared after the initial clean
  check: `README.md` is modified and
  `dev/active/cla-anti-saas-governance/` files are deleted. Do not modify,
  restore, stage, or attribute those changes to this workstream.

## Handoff Notes

### Handoff — 2026-08-29 Europe/Brussels

- **Current state:** Behavior is implemented through Phase 5 and documentation
  is updated. 32 of 38 implementation tasks and 4 of 6 verification gates are
  complete. Remaining work is closure only.
- **Next action:** Get path-scoped approval to restore the merged
  `20260828*_Init` catalogs, generate the additive `AddLegalIdentityAuthority`
  migrations, then run V4, Stryker, MAD, the final full gates, and V6.
- **Blockers:** Migration history replacement awaiting restoration approval; no
  real browser for visual and real-surface verification.
- **Validation:** Focused slices pass as recorded above; Blazor client full run
  is 2,765 passed with 1 unrelated pre-existing failure and 1 pre-existing skip.
  Generated OpenAPI and client hashes are recorded in the Phase 6 evidence.
- **Documentation impact:** Canonical docs and the breaking change fragment are
  updated; nothing further is pending there.
- **Risks:** Provider migration correction, startup configuration cut, and
  truthful multi-party attribution.
- **Notes for next contributor/agent:** Do not revive fallback behavior to make
  tests pass. A missing identity is a readiness/corruption failure, not an
  invitation to substitute branding. Do not accept the replacement `Init`
  catalogs as the migration outcome.
