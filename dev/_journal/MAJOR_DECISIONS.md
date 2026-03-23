# Major Decisions

Last Updated: 2026-03-16 Europe/Brussels

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
