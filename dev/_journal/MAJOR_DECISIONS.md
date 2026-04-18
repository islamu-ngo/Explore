# Major Decisions

Last Updated: 2026-04-18 Europe/Brussels

## 2026-04-16 Europe/Brussels - Blazor Clean Code Refactor: CTO Review Binding Decisions

### ServiceResult<T> as Structured Error Contract (Not String-Only)
- Decision: All Blazor service methods return `ServiceResult<T>` with `FailureCategory` enum, `ErrorCode`, `UserMessage`, `DeveloperMessage`, `ValidationErrors`, `IsRetryable`, `HttpStatusCode`.
- Why: String-only failures (`return null` or `return "error"`) cannot drive differentiated UI responses. The UI needs machine-readable error categories to select the correct presentation tier (inline validation vs banner vs snackbar vs re-auth flow vs error state).
- Impact: Every service method in `Explore.Blazor.Client/Services/` migrates from try/catch→null to try/catch→ServiceResult. Static factories (`FromApiException`, `TransientFailure`, `SessionExpired`, etc.) standardize construction. `FailureCategory` enum values: Validation, NotFound, Forbidden, SessionExpired, ProviderUnavailable, ProviderMisconfigured, TransientFailure, Unknown.

### Wave-Based Delivery Over Flat Phase Sequencing
- Decision: 21 phases reorganized into 5 delivery waves: A (Safety+Baseline), B (BFF Hardening), C (Service Contract Reform), D (UI Decomposition), E (Conformance+Polish).
- Why: Flat phase lists create false linearity. Semantic phases (e.g., ServiceResult migration) must ship as a complete wave — a half-finished contract reform is worse than none. Waves group related changes that should be reviewed and merged together.
- Impact: Implementation follows wave boundaries. Each wave has acceptance gates. PRs should not cross wave boundaries without justification.

### DynamicAuthSchemeManager Stop-the-Line Protocol
- Decision: Split into Phase 6A (stabilize + test + document current behavior) and Phase 6B (refactor). No refactoring until full behavioral understanding is documented.
- Why: 539-line state machine with dual locking (SemaphoreSlim + object lock), 8 public methods, runtime scheme mutation. Refactoring without understanding risks breaking auth for all users. Architectural decision required: should scheme mutation happen at runtime or only at startup?
- Impact: Phase 6A in Wave B produces test suite + state diagram. Phase 6B deferred until architectural direction is decided.

### State Classification Before Component Decomposition
- Decision: Complex page components (EventList 2651 lines, EventDetail 1747 lines) must classify all internal state as URL/service/local/computed BEFORE any extraction begins.
- Why: Extracting sub-components without understanding state ownership creates prop-drilling, circular dependencies, or broken reactivity. The page coordinator pattern requires knowing which state is the source of truth.
- Impact: Phase 17A (state classification) placed in Wave A, before Phase 15 (component decomposition) in Wave D.

### Operability as First-Class Engineering Concern
- Decision: New Phase X (Operability Diagnostics) added to Wave B. Includes startup config validation, diagnostics endpoints, feature-unavailable vs misconfigured distinction, self-hoster support.
- Why: The Blazor BFF is the entry point for all users. Config errors at startup (wrong auth provider URL, missing secrets, unreachable YARP target) should fail loud with actionable diagnostics, not silently degrade. Self-hosters need clear feedback when misconfigured.
- Impact: Startup validation checklist (auth, YARP, cookies, HTTPS, secrets). Error state distinction matrix maps ServiceResult categories to UI presentations.

### Change Type Classification on Every Task
- Decision: Every task carries a label: STRUCTURAL, BEHAVIORAL, SECURITY, CONTRACT, or OPERATOR. PRs must not mix SECURITY with STRUCTURAL without explicit justification.
- Why: Code reviewers need to know the change risk profile at a glance. A structural split (rename/move) has different review criteria than a behavioral change (logic alteration) or security fix.
- Impact: Task files and PR descriptions carry change type headers. Review checklists differ by type.

## 2026-04-12 Europe/Brussels - EAV Milestone D1: Projection System Architecture Decisions

### Concurrency Exception Translation in UnitOfWork (not MediatR pipeline)
- Decision: `DbUpdateConcurrencyException` → `ConcurrencyConflictException` translation lives in `EfCoreUnitOfWork`, not a MediatR `IPipelineBehavior`.
- Why: Application layer cannot reference `Microsoft.EntityFrameworkCore` under Clean Architecture dependency rules. The UoW already owns EF-specific semantics. All write paths go through `ExecuteInTransactionAsync`, so translation is centralized.
- Impact: `ConcurrencyConflictException` with `Code=concurrent_update` + entity metadata. `GlobalExceptionHandler` maps to 409 + RFC 7807 extensions.

### Advisory Lock Coordination via Raw ADO (not EF SqlQueryRaw)
- Decision: Projection updaters acquire `pg_try_advisory_xact_lock` and `pg_try_advisory_xact_lock_shared` via raw `DbConnection.CreateCommand()`, not EF's `Database.SqlQueryRaw<bool>`.
- Why: EF Core's `SqlQueryRaw<T>` doesn't reliably handle scalar boolean returns from PostgreSQL functions. Raw ADO is a single-statement call with no column-name ambiguity.
- Impact: Lock key pair = `fnv1a(ProjectionName)` + `fnv1a(tenantId)`. Command enlists on `Database.CurrentTransaction.GetDbTransaction()`.

### Single-Transaction Rebuild for D1 Baseline
- Decision: `RebuildForTenantAsync` uses a single xact-scoped advisory lock and commits once at the end. Per-batch commit + session-scoped lock deferred to D2 Operability.
- Why: Session-scoped advisory locks require holding the same connection across multiple commits, which is complex with EF Core's pooled DbContext pattern. D1 prioritizes correctness over scalability.
- Impact: Status row only becomes visible after full commit (no "live" Rebuilding status during execution). Acceptable for D1.

### Separate ProjectionTestContainerFixture Using EnsureCreatedAsync
- Decision: Projection integration tests use a dedicated `ProjectionTestContainerFixture` with `EnsureCreatedAsync()` instead of the shared `PostgreSqlContainerFixture` with `MigrateAsync()`.
- Why: Concurrent multi-agent development creates model-vs-migration drift (entities in model without migration files). `EnsureCreatedAsync` creates the schema from the current model, bypassing migration-file issues.
- Impact: Projection tests have their own minimal lookup seeding (5 rows). Existing fixture unchanged for other tests.

## 2026-04-12 Europe/Brussels - Event Scheduling Refactor: Architecture Decisions

### EventDay as First-Class Entity (not derived grouping)
- Decision: `EventDay` is a persistent aggregate member, not a `GROUP BY LocalStartDate` projection.
- Why: Five requirements need stable identity + authored state that a derived projection cannot carry: custom day labels, day-specific descriptions/banners, day-specific publishing state, day-level admin UX (reorder/lock/hide/attach media), day-level registration/business rules needing a FK target.
- Impact: `EventDay` entity with all tenant/audit/soft-delete/concurrency interfaces. Sessions link via nullable `EventDayId` FK.

### Two-Layer Same-Room Overlap Enforcement
- Decision: Layer A (async FluentValidation) is explicitly necessary-but-not-sufficient. Layer B (serializable transaction re-check at save time) is mandatory from day one.
- Why: Application-level validation alone cannot protect against concurrent writes, racing requests, or out-of-band mutations. The plan explicitly rejects the check-then-act pattern.
- Impact: `EventSessionRepository.CreateWithRoomOverlapGuardAsync` and `UpdateWithRoomOverlapGuardAsync` wrap the re-check + save in `IsolationLevel.Serializable`.

### IEventScheduleProjectionCalculator as Single Recompute Authority
- Decision: Stateless domain service in `Explore.Domain/Services/Scheduling/` is the sole authority for UTC→local projection writes. Entities expose `Reschedule()` / `ReprojectLocalTimes()` aggregate methods that accept the calculator.
- Why: Prevents scattered UTC→local conversion across handlers, validators, mappers, and seeders. DST-aware logic is shared identically between `EventSession`, `EventAgendaItem`, and `EventDay` backfill.
- Impact: Handlers call aggregate methods only. Architecture tests should enforce that no handler writes `LocalStart*`/`LocalEnd*` directly.

### EventRegistrationIntent Parent Layer
- Decision: Named `EventRegistrationIntent` (not `EventRegistrationGroup`). Parent carries scope + policy snapshot. `EventRegistration` remains the child concrete session entitlement/access row.
- Why: "Intent" precisely describes what the parent preserves — why the user registered. "Group" is too generic and could be confused with org/user groups.
- Impact: `CreateEventRegistrationDto` repurposed to intent-first shape. Handler creates parent + derived children atomically in serializable tx. NSwag client is stale until Phase 6 regen.

### RegistrationPolicyRules — Null Policy = Flexible
- Decision: When `Event.RegistrationPolicyId` is null, `RegistrationPolicyRules.IsScopeAllowed` treats it as `Flexible`, accepting all scopes.
- Why: Events created before the registration-policy field landed must still accept registrations during rollout. The null-means-Flexible convention avoids a migration that force-assigns policies to existing events.
- Impact: Pure domain function, single file (`Explore.Domain/Services/Registration/RegistrationPolicyRules.cs`).

## 2026-03-30 Europe/Brussels - API Testing Enterprise Grade: Technology Stack Decisions

### Database Reset: Respawn v7.0.0
- Decision: Use Respawn v7.0.0 with `DbAdapter.Postgres` for deterministic intertest data cleanup.
- Why: Industry standard for .NET test database reset. Analyzes FK relationships once, then performs fast ordered truncation. Lookup tables preserved via `TablesToIgnore`.
- Impact: Must add `Respawn v7.0.0` to `Directory.Packages.props` and `Event.API.IntegrationTests.csproj`.

### Schema Setup: MigrateAsync() Over EnsureCreated()
- Decision: Use `MigrateAsync()` for all PostgreSQL-backed test fixtures (both API and persistence).
- Why: Exercises the real migration chain, catching migration ordering issues and schema drift.

### Test Parallelism: Parallel From Day One
- Decision: Run PostgreSQL-backed API tests in parallel with per-test Respawn reset for isolation.
- Why: TUnit runs parallel by default. Per-test `ResetAsync()` before seeding gives each test a deterministically clean database.

### Contract Host Database: Keep EF InMemory
- Decision: Keep EF InMemory for the Contract host only. RealRuntime and Stress use PostgreSQL.
- Why: Contract tests verify serialization/ProblemDetails/HAL/headers — provider doesn't matter. Keeps them fast and Docker-independent.

### TUnit Fixture Model: SharedType.Keyed for Host Profiles
- Decision: Use TUnit `SharedType.Keyed` with keys "Contract", "RealRuntime", "Stress" for API test fixtures.
- Why: Allows multiple fixture types to coexist in the same assembly without lifecycle interference.

## 2026-03-29 Europe/Brussels - Customization Sidebar: UX Architecture Decisions

### Sticky RightSidebar Over Overlay Drawer
- Decision: Replaced MudDrawer overlay with a custom content-pushing sticky `RightSidebar` common component (`Components/Common/RightSidebar.razor`).
- Why: Overlay drawers obscure page content, making it impossible to see customization effects in real-time. Sticky sidebar pushes main content left, preserving visibility. Component is reusable for future AI assistant panel.

### EventCard Progressive Disclosure (Icons + Hover)
- Decision: Replace verbose text labels with icon badges (visibility/audience/format) using MudTooltip. CompactGrid uses `+N more` chip with hover reveal for hidden fields.
- Why: Reduces visual clutter while keeping all information accessible. Icons are universally understood; tooltips provide full labels on demand. Hover reveal avoids permanent information overload in compact layouts.

### UserSettingsService Auth Branching (API vs localStorage)
- Decision: Single `UserSettingsService` branches on authentication state — authenticated users persist via API settings endpoints, anonymous users use browser localStorage. SSR-safe via `IJSRuntime` availability check.
- Why: Avoids two separate service implementations. Anonymous users get instant UX without account creation. No anonymous→authenticated migration (D7 decision) keeps implementation simple. localStorage is acceptable for non-critical display preferences.

### Feature-Flag Bypass for Development
- Decision: Hardcode `_showCustomizationButton = true` instead of reading from settings feature-flag infrastructure.
- Why: Feature-flag infrastructure depends on tenant config that isn't reliably available in all dev environments. Must re-enable before production deployment.

## 2026-03-29 Europe/Brussels - EAV Milestone C: EventSession Layer 3 Parity

### Session Templates: Owned Children of Event Templates
- Decision: `EventSessionTemplate` has `EventTemplateId` FK — session blueprints exist only under an event template. Uniqueness is `(EventTemplateId, SessionTemplateKey, Version)`.
- Why: Sessions are child aggregates of events. Template hierarchy mirrors runtime hierarchy. Prevents orphaned session templates.

### Session Instantiation: Mirror Event Pattern Exactly
- Decision: `EventSessionTemplateInstantiationService` uses identical in-memory instantiation + handler persistence pattern as `EventTemplateInstantiationService`. Same two-pass provenance matching algorithm.
- Why: Architectural consistency — same patterns reduce cognitive load and enable shared test strategies. Session-specific differences are minimal (FK target is EventSessionId instead of EventId).

### Session Creation: Optional SessionTemplateId (Guid?)
- Decision: `CreateEventSessionDto.SessionTemplateId` is `Guid?`. Null = no template. Non-null = fetch, guard published+active, instantiate inside transaction.
- Why: Mirrors event creation pattern exactly. Zero breaking changes to existing session creation flow.

### Session Projection: Mirror Event Projection Shape
- Decision: `EventSessionCustomPropertyProjection` uses same column structure as `EventCustomPropertyProjection` (Namespace, Key, PropertyType, value columns, exposure flags, provenance).
- Why: Unified projection shape enables shared discovery/search/filter infrastructure in Milestone D without session-specific query paths.

## 2026-03-29 Europe/Brussels - EAV Milestone B: Event Template + Runtime Baseline

### Template Instantiation: In-Memory Service + Handler Persistence
- Decision: `EventTemplateInstantiationService` creates in-memory entities only. The `CreateEventCommandHandler` persists via repository calls inside a transaction.
- Why: Keeps instantiation logic testable without DbContext. Handler owns the persistence strategy and transaction boundary.

### Provenance Matching: Two-Pass Algorithm
- Decision: Match existing runtime definitions to template definitions first by `SourceTemplateDefinitionId` (exact lineage), then unmatched by normalized `Namespace+Key` (repair/backfill). Track consumed matches to prevent double-matching.
- Why: SourceId is the canonical provenance link. Namespace+Key fallback only for definitions that lost their lineage (manual creation, data migration).

### Event Creation: Optional TemplateId (Guid?)
- Decision: `CreateEventDto.TemplateId` is `Guid?`. Null = no template, existing flow untouched. Non-null = fetch template, guard published+active, instantiate inside existing transaction.
- Why: Zero breaking changes to existing event creation. Template selection is purely additive. Guard prevents instantiating draft/inactive templates.

### Runtime Definition Edit Flows: Event-Local Only
- Decision: Runtime queries and edit commands operate exclusively on event-local definitions (`GetDefinitionsForEventPaged(eventId)`). No implicit template re-reads during editing.
- Why: Event-local state is the source of truth after instantiation. Template changes require explicit sync (Milestone E).

### Ad-Hoc Definitions: No Provenance
- Decision: Runtime definitions created without a template get `InstantiatedAt = DateTimeOffset.UtcNow` but null provenance fields (SourceTemplateId, SourceTemplateKey, etc.).
- Why: Clean distinction between template-derived and manually-created definitions. Provenance fields are only meaningful for template lineage.

## 2026-03-26 Europe/Brussels - Enterprise Footer Customization: Blazor UI Implementation

### Footer Template Dispatch via Switch (ADR-005)
- Decision: Use `switch` on template key string in `Footer.razor` to dispatch to 4 typed template components (`FooterTemplateStandard3Col`, `FooterTemplateStandard2Col`, `FooterTemplateMinimal`, `FooterTemplateCommunity`).
- Why: 4 fixed templates → compile-time safety, simple to reason about. `DynamicComponent` deferred to Phase 2+ when newsletter/HTML fragment blocks are added.

### Footer Admin: Typed HTTP Client (Not NSwag)
- Decision: Create `IFooterAdminService` + `FooterAdminService` as typed HttpClient service following `ITenantNavigationService` pattern, instead of using NSwag-generated client.
- Why: Footer admin endpoints were not covered by existing NSwag generation. Typed client provides explicit control over models and error handling. Registered via `AddTypedApiClient` with interactive resilience.

### Footer Governance Available in All Deployment Modes
- Decision: Lock toggles shown in both single-tenant and multi-tenant modes. Info alert in single-tenant explains locks have no effect.
- Why: User explicitly requested footer customization for all deployment modes, not just multi-tenant.

### Default Footer Seeded via Runtime Seeder
- Decision: Default link groups (Quick Links: About/Events/Contact + Legal: Terms/Privacy) seeded at runtime via `LookupTableSeeder.SeedDefaultFooterLinkGroupsAsync()` with deterministic GUIDs and `TenantId = null`.
- Why: Follows existing seeding pattern. Avoids EF Core 10 `HasData()` circular FK bug (#36682). Idempotent check prevents re-seeding.

### Community Guidelines Link: Dynamic Runtime Conditional
- Decision: Community guidelines link rendered conditionally in footer templates based on `AllowUserSubmittedEvents || AllowOrganizationSubmittedEvents || AllowGroupSubmittedEvents` — same rule as sidebar in `MainLayout.razor.cs`.
- Why: User explicitly requested same logic as sidebar. Not stored as a DB link since visibility is determined by runtime policy, not admin configuration.

## 2026-03-26 Europe/Brussels - API Enterprise Hardening

### ValidationBehavior: Delete (Option A — Manual Validation)
- Decision: Delete `ValidationBehavior.cs` rather than enabling pipeline validation.
- Why: Per CLAUDE.md rule, validators are manually instantiated in handlers. The behavior was never registered and is dead code. Enabling it would require auditing 617 handlers for double validation — unacceptable risk for the benefit. Manual validation gives handlers explicit control over validation timing and error shaping.
- Files removed: `Explore.Application/Behaviors/ValidationBehavior.cs`

### Idempotency Store: Database-backed (PostgreSQL)
- Decision: Store idempotency keys in PostgreSQL via EF Core, not Redis.
- Why: Auditability — idempotency records need to survive Redis flushes, be queryable for debugging, and participate in the same transactional boundary as the command they protect. Redis would require a separate reliability story. 24-hour TTL via `ExpiresAt` column keeps the table bounded.
- Files: `Explore.Domain/IdempotencyRecord.cs`, `Explore.Persistence/Repositories/IdempotencyRepository.cs`, `Explore.API/Middleware/IdempotencyMiddleware.cs`

### URL Versioning: IApplicationModelConvention (No Controller Modifications)
- Decision: Add URL segment versioning (`/api/v0.1/actor`) alongside existing media-type versioning via a `VersionedRouteConvention` that automatically adds versioned route templates to all controllers.
- Why: Modifying 58 controller files to add a second `[Route]` attribute is fragile and creates merge conflicts. The convention approach is zero-touch for controller authors and automatically applies to new controllers.
- Files: `Explore.API/Extensions/ApiVersioningExtensions.cs`

### Swashbuckle: Kept (User Decision)
- Decision: Do NOT remove Swashbuckle. Keep both Swagger UI and Scalar/native OpenAPI.
- Why: User explicitly requested to keep Swashbuckle. Blazor client generation and existing tooling depends on `/swagger/v0.1/swagger.json`.

### SafeMode: One-Way Latch (No Programmatic Deactivation)
- Decision: Changed `SafeMode` from public get/set to private set with `ActivateSafeMode()` method. Once activated, safe mode persists until instance restart.
- Why: Previously, RuntimeAuthorizationProvider toggled SafeMode on/off per-request in a try/finally. This allowed transient oscillation between safe and normal mode when BYO Cerbos was intermittently failing. The latch pattern is more secure — once the PDP is detected as unreachable, deny-all stays until an operator restarts the instance.

## 2026-03-25 Europe/Brussels - CSS Modernization: @layer Architecture + Design Tokens + Wrapper Components

- Decision: Replace monolithic `StyleGlobal.css` with `@layer`-based cascade architecture (7 layer files), 3-tier design token system, MudBlazor wrapper components, and modern CSS features (oklch, clamp, CSS nesting, container queries).
- Why: The existing 660+ line monolithic CSS file mixed reset, tokens, components, utilities, and global MudBlazor overrides without cascade control. MudBlazor v9 removed `MudGlobal` defaults. Global `.mud-*` class overrides violated CSS isolation skill guidance.
- Key decisions:
  1. **@layer ordering** (`reset → base → tokens → mudblazor-overrides → components → utilities`) — later layers win regardless of specificity.
  2. **3-tier tokens** (Primitives → Semantic → Component) — semantic aliases point to `--mud-palette-*` for dark mode compatibility.
  3. **Wrapper components** (`AppButton`, `AppCard`, `AppTextField<T>`, `AppIconButton`, `AppDialogShell`) — composition via `CaptureUnmatchedValues`, not inheritance.
  4. **DialogOptionsFactory** — static presets (`Small`, `Medium`, `Confirmation`, `Editor`) replace inline `new DialogOptions { ... }`.
  5. **oklch** for all color mixing and shadows — perceptually uniform, replaces `rgba` and `color-mix(in srgb, ...)`.
  6. **Fluid typography** with `clamp()` for H1-H5 — eliminates breakpoint-based typography queries.
  7. **Global `.mud-*` exception policy** — documented whitelist in `mudblazor-overrides.css` header, each block requires justification comment.
  8. **CSS nesting** — native `&` for pseudo-classes/modifiers/media queries. BEM element selectors stay flat (no `&__element` concatenation).
  9. **Container queries** in EventList — 5 viewport media queries converted to `@container` queries.
  10. **DefaultBorderRadius** changed from 8px to 12px in `AppearanceThemeService.cs`.
- Files: `Explore.Blazor/wwwroot/css/` (7 layer files), `Explore.Blazor.Client/Components/Common/` (5 wrapper components), `Explore.Blazor.Client/Services/DialogOptionsFactory.cs`.
- Follow-up: Remaining MudButton/Card/TextField/IconButton → wrapper migrations in ~80 files (Tier 2+3).

## 2026-03-22 Europe/Brussels - Hierarchical Settings Preferences: Theme Storage And Runtime Boundary

- Decision: Keep hierarchical settings for precedence, defaults, and approved user overrides, but do not store the theme catalog as JSON in generic settings. Theme catalogs must be modeled as first-class entities with audit and concurrency support.
- Why: The feature needs admin-managed lists, deterministic defaults, fallback when a selected theme is removed, and safe concurrent editing. Those concerns become brittle if theme definitions are hidden inside settings payloads.
- Consequence: The next implementation slice must start with an ADR, then define theme entities/value objects plus reference-based appearance settings such as `appearance.default_theme_id` and `appearance.theme_mode`.

## 2026-03-22 Europe/Brussels - Hierarchical Settings Preferences: Layouts Must Not Be The Theme Engine

- Decision: `Explore.Blazor.Client/Layout/MainLayout.razor.cs` and `Explore.Blazor.Client/Layout/SetupLayout.razor.cs` should become thin consumers of a dedicated runtime theming service instead of owning precedence and palette composition logic.
- Why: The current layout files already duplicate theme-building code. If precedence, bootstrap, and fallback rules are left there, theming behavior will spread across UI lifecycle code and become hard to test.
- Consequence: Before UI work starts, introduce a dedicated service boundary such as `IThemeCompositionService` or `IAppearanceRuntimeService` and define SSR/bootstrap authority order in the ADR.

## 2026-03-16 Europe/Brussels - Event Scheduling Refactor: Registration Architecture

- Decision: Do not redefine the current session-level `EventRegistration` rows as the abstract parent registration concept. Instead, add a new parent intent/group layer above them and keep the child/session rows as the concrete entitlement/access records.
- Why: The existing platform semantics, UI, and capacity logic are already centered on session-level access. Keeping child rows concrete reduces migration pain, preserves understandable attendance/capacity behavior, and still allows event/day/session-selection policy-aware UX.
- Consequence: Future implementation should choose a dedicated parent name such as `EventRegistrationIntent` or `EventRegistrationGroup`, preserve temporary compatibility for session-level contracts, and backfill intent semantics above existing session rows.

## 2026-03-16 Europe/Brussels - Event Scheduling Refactor: Overlap Validation Strategy

- Decision: Enforce “same room + overlapping time = invalid” first in create/update session DTO validators using async FluentValidation with repository-backed checks.
- Why: This gives fail-fast behavior aligned with the repo’s validator-first patterns and avoids overcommitting to a database-only conflict strategy before the new room model is fully established.
- Consequence: The first implementation slice should add the necessary repository/service checks for validator use, then optionally add stronger persistence hardening later if race conditions or scale demand it.

## 2026-03-13 Europe/Brussels - HTTP Resilience Refactor: Tenant/Auth Ordering Decision

- Decision: Keep the current API ordering assumption that authentication scheme selection does not depend on tenant resolution.
- Why: Direct inspection of `Explore.API/Program.cs` showed the policy scheme switches only on the presence of `X-API-Key`; tenant context is not consulted to decide JWT vs API-key auth.
- Consequence: Phase 3 middleware work should focus on forwarded-header trust, request logging placement, and cancellation propagation rather than forcing tenant resolution ahead of authentication.

## 2026-03-13 Europe/Brussels - Forwarded Headers Trust Model

- Decision: Configure forwarded-header trust explicitly in both API and BFF hosts via `ForwardedHeaders:KnownProxies` and `ForwardedHeaders:KnownNetworks`, with development-only trust-all fallback when the config is empty.
- Why: The previous BFF behavior effectively trusted every proxy by clearing trust lists unconditionally, which is not acceptable as the long-term security baseline.
- Implementation anchors:
  - `Explore.API/Program.cs`
  - `Explore.API/appsettings.json`
  - `Explore.Blazor/Extensions/MiddlewareExtensions.cs`
  - `Explore.Blazor/appsettings.json`

## 2026-02-23 18:12 Europe/Brussels - Admin Consolidation Handoff Scope

- Decision: Consolidate admin UX into two panel pages only:
  - `Explore.Blazor.Client/Pages/Admin/Tenant/TenantAdminSettings.razor`
  - `Explore.Blazor.Client/Pages/Admin/Instance/InstanceAdminSettings.razor`
- Why: User explicitly requested eliminating split between `/admin` and separate admin pages, and matching existing settings-style panel navigation.
- Implication: Legacy `/admin` dashboard and standalone lookup/admin pages are target candidates for removal after migration.

## 2026-02-23 18:12 Europe/Brussels - SMTP Configuration Placement

- Decision: Add SMTP configuration under instance admin panel as a dedicated sidebar section, with a test connection action.
- Why: SMTP credentials are platform-level concern requested for platform/instance administrators.
- Implementation anchor points:
  - UI pattern: `Explore.Blazor.Client/Components/Admin/Instance/InstanceStorageSection.razor`
  - API pattern: `Explore.API/Controllers/InstanceOnboardingController.cs` storage settings/test endpoints
  - Setting keys: `Explore.Domain/Constants/GovernanceSettingKeys.cs` (`EmailSmtp*`, `EmailFrom*`)

## 2026-02-23 18:12 Europe/Brussels - Dev Docs Continuity Protocol

- Decision: Before context reset, update every active context/tasks file with a timestamped checkpoint entry, and add deep handoff detail to the currently active track only.
- Why: Ensures broad continuity for all active tracks while preserving high-signal detail where active implementation is ongoing.

## 2026-02-23 18:47 Europe/Brussels - Admin Consolidation Implementation Completed

- Decision: Complete the consolidation by deleting legacy standalone admin pages/routes after embedding equivalent capabilities into panel sections.
- Why: Prevent duplicate administrative entry points and keep one canonical settings-style admin UX per role.
- Outcome:
  - Tenant administration now hosts organizations + lookup management.
  - Instance administration now hosts SMTP settings + test connection.
  - Navbar admin dropdown routes now point directly to tenant/instance administration pages.

## 2026-02-23 18:47 Europe/Brussels - Verification Baseline for This Delivery

- Decision: Treat successful `dotnet build` + targeted Blazor and Application unit tests as release gate for this session due lack of Razor LSP in environment.
- Why: Ensures functional validation while acknowledging toolchain limitation for `.razor` diagnostics.
- Evidence:
  - Build passed.
  - Blazor client tests passed (522).
  - Application unit tests passed (278).

## 2026-02-27 Europe/Brussels - Blazor Folder Restructure Continuation Baseline

- Decision: Treat `dev/active/blazor-folder-restructure` as implementation-complete with remaining work focused on checklist/doc synchronization and optional full-suite gate validation.
- Why: Core migration, imports, dialog helper refactor, and targeted Blazor test loop are already green; unresolved items are primarily documentation fidelity and broader release assurance.
- Verification anchor:
  - `dotnet build --configuration Release --verbosity quiet` passes (warnings only).
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passes (warnings only).

## 2026-02-27 Europe/Brussels - Context Reset Handoff Policy (Active Docs)

- Decision: Append explicit session checkpoint blocks to every `dev/active/*-context.md` and `dev/active/*-tasks.md` file during context-limit handoff.
- Why: Ensures no active track is left without fresh continuity metadata, reducing reset-time archaeology and ambiguity.

## 2026-02-27 Europe/Brussels - Blazor Client Contracts Boundary

- Decision: Standardize on root `Explore.Blazor.Client/Contracts` for interface contracts and keep `Explore.Blazor.Client/Services` as implementation-only.
- Structure adopted:
  - `Contracts/Services/{Lookup,Events,Organizations}`
  - `Contracts/Providers`
  - `Contracts/Interop`
- Why: Supports future non-service abstractions (providers/interop), improves testability, and avoids conflating API proxy interfaces with concrete service implementations.
- Verification:
  - `dotnet build --configuration Release --verbosity quiet` passed after namespace and Razor import updates.
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed (518 tests).

## 2026-03-03 Europe/Brussels - Notification System: Lookup Entity Refactor

- Decision: Replace string-based `Type` and `EntityType` fields on `Notification` with proper FK lookup entities (`NotificationType`, `NotificationEntityType`) following the ApprovalStatus pattern.
- Why: Type safety, referential integrity, eliminates magic strings, enables filtering/reporting by notification type with proper indexes.
- Pattern: `int Id`, `string MasterCode`, `string FullName`, `string? Description` with companion enum in `Explore.Domain/Enums/`. Seeded via `LookupTableSeeder` at runtime (not HasData, due to EF Core 10 bug #36682).
- Enums: `NotificationTypeEnum` (10 values), `NotificationEntityTypeEnum` (6 values).

## 2026-03-04 Europe/Brussels - Notification System: Materialized Fan-Out with Scope Metadata

- Decision: Notifications stay per-human-user (`UserId` is always the recipient). Added `SourceActorId`, `RecipientContextActorId`, and `NotificationScopeId` (FK→ActorType) for multi-scope targeting.
- Why: Enterprise notification systems need org/group scope without sacrificing read-path performance. Fan-out at write time means read queries stay O(1) per user.
- Architecture:
  - `NotificationScopeId` (int, FK→ActorType) — classifies scope: User(1)=Personal, Organization(2), Group(4), System(5)
  - `SourceActorId` (Guid?, FK→Actor) — who/what triggered the notification
  - `RecipientContextActorId` (Guid?, FK→Actor) — which org/group context for UI differentiation
- Rejected alternatives:
  - Option A (Replace UserId with ActorId): Kills hot read path, requires JOIN for every notification query.
  - Option C (NotificationRecipient junction table): Over-engineered for our scale, adds N+1 risk.
- Verification: 474 tests passing (363 app + 79 domain + 32 architecture).

## 2026-03-04 Europe/Brussels - Bots/System Are Senders Not Receivers

- Decision: Bot and System actors should NOT receive notifications. They should consume domain events or message queues for automation. However, they CAN be notification sources (`SourceActorId`).
- Why: Notifications are best-effort, human-oriented (dismissable, soft-deletable). Bots need guaranteed delivery, ordering, retry semantics. Different delivery guarantees → different mechanisms.
- Implication: Fan-out logic should filter by ActorType=User when distributing org/group notifications to members.

## 2026-03-04 Europe/Brussels - Reuse ActorType as Notification Scope

- Decision: Instead of creating a new `NotificationScope` lookup entity, reuse the existing `ActorType` entity as the scope classifier for notifications.
- Why: ActorType already has the exact values needed (User=1, Organization=2, Group=4, System=5). Creating a duplicate lookup adds no value and introduces synchronization burden.
- Trade-off: Semantic coupling between actor classification and notification scoping, but the domain concepts are genuinely aligned.

## 2026-04-18 Europe/Brussels - Blazor Clean Code Refactor v3 + Wave 0 Implementation

### Wave 0: Blocking Pre-Flight Hotfixes Required
- Decision: 6 stop-the-line defects must merge before any Wave A work: singleton state leakage, async void crash, .Result deadlocks, missing Cache-Control, missing YARP timeout.
- Why: Two singleton services hold per-user/per-circuit state (SetupSecretSessionService, IDynamicAuthSchemeManager). AnalyticsInitializer.razor had async void that could crash Blazor Server. .Result could deadlock Blazor Server. Auth endpoints lacked Cache-Control: no-store. YARP had no request timeout.
- Impact: All 6 hotfixes implemented and verified. Build green (0 errors). 1957 tests pass, 28 pre-existing failures unchanged. Zero new failures.

### Render Mode: InteractiveServer Default, Cohort Migration
- Decision: All 32 pages currently hardcode `@rendermode InteractiveServer`. Project has dynamic configurable render mode. Default should be InteractiveServer app-wide. Phase A0 will use cohort-based migration for pages eligible for InteractiveAuto.
- Why: InteractiveAuto adds WASM download latency for interactive pages but enables faster post-load interactions. Public-facing pages benefit most. Admin/setup pages should stay Server-only.
- Impact: Phase A0 will categorize pages into 4 cohorts (Static SSR, InteractiveAuto public, InteractiveAuto user, InteractiveServer admin).

### Static Field Fix: SetupSecretSessionService
- Decision: Removed `static` keyword from SetupSecretSessionService `_store` and `CleanupExpiredEntries()`. Registration remains Singleton (not changed to Scoped).
- Why: The `static` keyword was redundant on a Singleton — there's only one instance anyway. Making it Scoped would require auditing all consumers for scoped resolution. The `static` keyword on a Singleton field is misleading (suggests legitimate cross-instance sharing) rather than accidental.
- Impact: CircuitAccessTokenService `_tokenStore` LEFT as static because `GetTokenForUser()` static method requires static store for cross-circuit token resolution in AccessTokenForwardingHandler. This is intentional architectural debt documented for Wave B Phase 3.

### Blazor Arch Tests: 13 Tests Planned
- Decision: 13 architecture tests total: 2 existing (IEventApiClient injection, ITranslationService placement) + 11 new. Uses file-scanning approach (no project reference to Blazor assemblies).
- Impact: Tests cover Console.WriteLine, [Inject] interface-only, DialogOptionsFactory, NavigationManager in shared components, IJSRuntime in services, ISnackbar in data services, singleton mutable state, async void, .Result/.Wait(), IConfiguration direct injection, service locator, model classes in interface files. Known exceptions tracked for pre-existing violations being fixed in later phases.
