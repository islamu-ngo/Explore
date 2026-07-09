<!-- ABOUTME: Repository-grounded implementation plan for user, organization, and group notification channel preferences. -->
<!-- ABOUTME: Defines the checkbox matrix, hierarchy, persistence, API, Blazor surface, delivery integration, and verification contract. -->

# Notification Preference Matrix — Implementation Plan

Last Updated: 2026-07-09 Europe/Brussels

## 0. Planning Metadata

- Task name: `notification-preference-matrix`
- Workstream directory: `dev/active/notification-preference-matrix/`
- Current status: PR 1 data foundation/resolver, PR 2 current-user/organization/group CQRS/API/HAL endpoints, PR 3 user/organization/group Blazor matrix surfaces, and PR 4 delivery integration are implemented; operations/docs cleanup and manual QA are pending.
- User request: implement a notification preference matrix where rows are notification categories, columns are Email and In-App, cells are independent checkboxes, multiple channels can be selected, all unchecked means category opt-out, global mute suppresses non-essential notifications, and critical security/billing categories remain locked on.
- CTO review goal: convert the earlier draft into an enterprise-grade, Clean Architecture, self-hostable implementation plan with exact sequencing, risk controls, authorization, tenant isolation, and verification.

## 1. Contract Classification

No exact `.claude/contract/intents.yaml` intent covers a planning-only notification-preference architecture update. This work therefore uses the `/dev-docs` fallback contract.

| Field | Fallback Planning Contract |
| --- | --- |
| id/title | `fallback-dev-docs-plan` / Repository-grounded active implementation plan |
| original planning paths in scope | `dev/active/notification-preference-matrix/**` only |
| implemented PR 1 paths | `Explore.Domain/**`, `Explore.Application/Contracts/**`, `Explore.Persistence/**`, `Event.Persistence.IntegrationTests/**` |
| implemented PR 2 paths | `Explore.Application/DTOs/Notification/**`, `Explore.Application/Features/Notifications/**`, `Explore.API/Controllers/**`, `Explore.API/Hateoas/**`, `Explore.API/OpenApi/**`, `Event.Application.UnitTests/**`, `Event.API.IntegrationTests/**` |
| implemented PR 3 paths | `Explore.Blazor.Client/Contracts/Services/Notifications/**`, `Explore.Blazor.Client/Services/NotificationService.cs`, `Explore.Blazor.Client/Components/Notifications/**`, `Explore.Blazor.Client/Pages/User/Components/SettingsNotifications.*`, `Explore.Blazor.Client/Pages/Organizations/OrganizationProfile.razor`, `Explore.Blazor.Client/Pages/Groups/GroupProfile.razor` |
| implemented PR 4 paths | `Explore.Application/Services/EventPublishedNotificationFanoutService.cs`, `Explore.Application/Services/EventModerationNotificationFanoutService.cs`, `Explore.Application/Services/RegistrationNotificationDeliveryService.cs`, `Explore.Infrastructure/EmailDispatchDrainService.cs`, `Event.Application.UnitTests/**`, `Explore.Infrastructure.Tests/Infrastructure/**` |
| current verification | `Explore.Application` build, `Explore.Persistence` build, `Explore.Infrastructure` build, `Explore.API` no-dependencies build, `Explore.Blazor.Client` build, `Explore.Blazor.Client.Tests` no-dependencies build plus no-build test run, full `Event.Application.UnitTests`, full `Event.Persistence.IntegrationTests`, full `Explore.Infrastructure.Tests`, full `Event.Architecture.Tests` |
| current blocker | Full solution/API dependency builds are blocked by unrelated dirty localization work: `Explore.Infrastructure/Localization/OfflineTranslationProvider.cs` and missing `CultureRegistry` references in Application localization handlers. User instructed to leave these untouched. |
| still forbidden without reclassification | Delivery fanout changes or additional authorization/HAL policy expansion beyond the implemented preference endpoints |

Future implementation agents must re-classify each code slice against `.claude/contract/intents.yaml`. Expected intents include `add-ef-migration`, `add-cqrs-handler`, `add-get-endpoint`, `add-write-endpoint`, `add-hal-link`, `blazor-component-affordance`, and possibly `openapi-contract-change`.

## 2. Evidence Log

### 2.1 Repository Evidence

| Source | Verified Fact | Plan Impact |
| --- | --- | --- |
| `docs/QUICK_REFERENCE.md` | Repositories return entities; validators are manually instantiated; lookup IDs use `int`; aggregate IDs use `Guid`; HAL links gate UI affordances; tenant isolation is centrally enforced. | Preference storage must be entity-first, lookup-backed, tenant-safe, and HAL-projected. |
| `docs/GOVERNANCE.md` | Clean Architecture ownership: Domain entities/rules, Application CQRS/repositories/contracts, Persistence EF/repositories, API controllers/HAL, Blazor composition/UI. Multi-step writes use `IUnitOfWork.ExecuteInTransactionAsync`. | Resolver and handlers live in Application; EF and migrations live in Persistence; HATEOAS lives in API; UI stays dumb. |
| `docs/API.md` | Controllers dispatch MediatR; route names/templates must be explicit; ProblemDetails is canonical; private preference reads are an authenticated exception to public GET defaults. | Preference GET and write endpoints require authentication/resource authorization and ProblemDetails metadata. |
| `docs/BLAZOR.md` | Browser never owns tokens; BFF/service layer wraps clients; state-changing BFF routes require antiforgery; UI affordances come from HAL `_links`. | Matrix UI cannot inspect roles/claims or call API directly from components. |
| `docs/MULTI_TENANCY.md` | Hierarchical settings cascade exists across Instance, Tenant, Organization, Group, User; broader locks prevent narrower overrides; cache invalidation is scope-aware. | Resolution must respect user/group/org precedence while honoring broader locks and tenant filters. |
| `docs/AUTHORIZATION.md` | AuthorizationBehavior, resource attributes, HAL authorization evaluator, and provider/local fallback fail closed. | Org/group preference endpoints need resource kinds/actions and HAL parity. |
| `docs/NOTIFICATIONS.md` | Current notification feature creates durable in-app rows; SSE is only a refresh hint; notification-to-email fanout is not currently implemented. | In-App preferences gate notification creation/fanout; do not claim existing email fanout. |
| `docs/EMAIL_NOTIFICATIONS.md` | `EmailDispatchOutbox` and `EmailDispatchDrainService` implement direct email dispatch, tenant pause, skip outcomes, scheduler fallback, and SMTP health. | Email preferences must integrate with durable email dispatch paths without weakening existing skip/unsubscribe behavior. |
| `Explore.Domain/Constants/NotificationPreferenceCategories.cs` | Existing category constants are static codes only: registration confirmations, organizer announcements, event reminders, event updates. | A real category metadata model is still proposed work; current constants are not enough for matrix required/default/lock metadata. |
| `Explore.Application/Contracts/Infrastructure/IHierarchicalSettingsResolver.cs` | Resolver contract supports batch loads, lock metadata, scope set/remove/lock/unlock, and cache invalidation. | Reuse or align with this hierarchy instead of inventing a second cascade engine. |
| `Explore.Domain/Notification.cs` | Tenant-scoped in-app notification aggregate includes type, scope, dedupe, read/archive/snooze, reason, audit, soft-delete. | Preference gating should happen before/at fanout, not by editing delivered rows. |
| `Explore.Domain/EmailDispatchOutbox.cs` | Tenant-scoped durable email outbox represents direct email dispatch infrastructure. | Email channel integration is a separate delivery-path slice, not automatic notification-to-email fanout. |

### 2.2 Context7 Evidence

| Library docs | Guidance Used |
| --- | --- |
| Context7 `/dotnet/entityframework.docs` | EF Core model-level `HasQueryFilter` is canonical for soft-delete/multi-tenancy; `IgnoreQueryFilters()` can disable filters and must not remove tenant isolation on request paths; async transactions use `BeginTransactionAsync`, `SaveChangesAsync`, `CommitAsync`; optimistic concurrency/concurrency tokens are the standard conflict pattern. |
| Context7 `/dotnet/aspnetcore.docs` | ASP.NET Core ProblemDetails/validation responses are structured RFC7807 JSON; antiforgery middleware must run after authentication and authorization; endpoint metadata should declare problem/validation outcomes. |
| Context7 `/websites/learn_microsoft_en-us_aspnet_core` | Blazor `EditForm` supports enhanced form handling and named forms; validation uses `DataAnnotationsValidator`/validation summaries/custom server-side errors; Blazor manages validation ARIA attributes. |

## 3. Source-Grounded Current State

### 3.1 What Exists Today

- Durable in-app notifications exist through `Notification`, notification repositories, notification API endpoints, HAL links, and notification documentation.
- Durable direct email dispatch exists through `EmailDispatchOutbox`, outbox/drain repositories and services, SMTP configuration, tenant pause, skip outcomes, and health checks.
- A hierarchical settings resolver contract exists and already models Instance/Tenant/Organization/Group/User scopes with lock semantics.
- Some static notification preference category constants exist, but only as codes for prior email-responsibility work.
- HAL authorization and BFF patterns exist and are mandatory for any UI action affordance.

### 3.2 What PR 1 Added

- `NotificationPreferenceCategory` and `NotificationPreferenceChannel` lookup-like metadata with stable `int` IDs, codes, required/default channel metadata, and sort order.
- `NotificationChannelPreference` tenant-scoped cell rows and `NotificationPreferenceProfile` tenant-scoped global mute/profile rows with audit, soft-delete, concurrency, named tenant/soft-delete filters, check constraints, and filtered unique indexes per scope target.
- Application contracts for preference repositories and `INotificationPreferenceResolver`.
- Persistence repositories and `NotificationPreferenceResolver`, which batches metadata/profile/cell reads and resolves defaults, hierarchy, locks, required categories, and global mute.
- Runtime lookup seeding in `LookupTableSeeder`, Respawn lookup preservation in `PostgreSqlContainerFixture`, and migration `20260709160022_AddNotificationPreferenceMatrixFoundation`.
- Regression coverage in `NotificationPreferenceMatrixPersistenceTests`; the full `Event.Persistence.IntegrationTests` project passed 259/259 tests.

### 3.3 What PR 2 Added

- UI-ready notification preference matrix DTOs for categories, channels, cells, and scoped mute state.
- Current-user, organization, and group matrix queries backed by `INotificationPreferenceResolver`, including group parent-organization context when resolving group preferences.
- Current-user, organization, and group save/mute commands with required-category and broader-lock validation plus unit-of-work-wrapped upserts.
- Authenticated API endpoints under the existing controller conventions:
  - `GET/PUT api/notification/preferences/me`
  - `PUT api/notification/preferences/me/mute`
  - `GET/PUT api/organization/{id}/notification-preferences`
  - `PUT api/organization/{id}/notification-preferences/mute`
  - `GET/PUT api/group/{id}/notification-preferences`
  - `PUT api/group/{id}/notification-preferences/mute`
- HAL resource assembly for `NotificationPreferenceMatrixDto` with server-authored `self`, `save`, and `set-mute` links for user, organization, and group scopes.
- OpenAPI HAL schema catalog coverage for `HalResourceOfNotificationPreferenceMatrixDto`.
- Application regression coverage for current-user and scoped org/group matrix behavior. Full `Event.Application.UnitTests` passed 2089/2089 tests after the scoped handler tests were added.

### 3.4 What PR 3 Added

- `NotificationService` and `INotificationService` methods for current-user, organization, and group preference matrix load/save/mute operations using the generated BFF-safe API client.
- A reusable `NotificationPreferenceMatrix` Blazor component that renders category rows, channel checkbox columns, required/locked copy, scoped global mute, and save/mute actions from the HAL projection.
- HAL-only affordance gating in Blazor: save and mute controls enable only when the matrix resource contains the `save` and `set-mute` links; no role or claim checks were added to component code.
- Current-user settings integration through the existing `/settings?section=notifications` surface.
- Organization and group profile Notifications tabs using the same reusable matrix component and generated organization/group preference endpoints.
- Scoped CSS isolation with BEM-style classes, logical properties, no `!important`, and no broad MudBlazor global overrides.

### 3.5 What PR 4 Added

- In-app fanout gates for event-published notifications, event moderation notifications, and registration-confirmation fallback notifications through `INotificationPreferenceResolver` before non-required `Notification` rows are created.
- Required-category behavior stays centralized in the resolver; trust-safety moderation paths keep resolving as required/enabled.
- Direct email dispatch gating in `EmailDispatchDrainService` after tenant pause, processing claim, receipt claim, and legacy unsubscribe checks, but before SMTP provider handoff.
- Matrix-disabled email dispatches are durably recorded as skipped with failure category `recipient_notification_preference_disabled` while preserving attempts, receipts, metrics, tenant pause, operator park/replay, retry, and dead-letter semantics.
- Regression coverage for in-app event-update suppression, moderation required delivery defaults, registration fallback resolver wiring, and email drain matrix skip behavior.

### 3.6 What Does Not Exist Yet

- No general notification-to-email fanout exists; Email channel work applies to the existing direct email dispatch paths only.
- Final canonical docs cleanup and manual QA are still pending.

## 4. Future State

Build a normalized, HAL-driven notification preference system:

1. Category rows and channel columns are server-defined metadata.
2. Email and In-App cells are independent booleans.
3. Both channels checked means both are allowed when a delivery path exists.
4. Both unchecked means explicit opt-out for a non-required category.
5. Global mute suppresses non-essential notifications without deleting per-category choices.
6. Required categories remain enabled server-side and render as disabled checked cells with lock/reason copy.
7. Effective resolution is computed once in Application and consumed by API, Blazor, in-app fanout, and email dispatch integrations.
8. User/org/group edit affordances are exposed only through HAL links after authorization evaluation.

## 5. Product Semantics

### 5.1 Matrix Behavior

| Category | Email | In-App |
| --- | --- | --- |
| Account security | disabled checked | disabled checked |
| Billing and legal | disabled checked | disabled checked |
| Registration status | checkbox | checkbox |
| Event updates | checkbox | checkbox |
| Organization updates | checkbox | checkbox |
| Group updates | checkbox | checkbox |
| Trust and safety | checkbox by default; subtype may be required later | checkbox by default; subtype may be required later |
| Product announcements | checkbox | checkbox |
| Marketing | checkbox, default off | checkbox, default off |

Rules:

- Required categories are enforced by server metadata and command/resolver logic, not by UI disabled state alone.
- Marketing starts opt-out by default unless product/legal explicitly changes that requirement.
- Billing/legal can exist as metadata before billing workflows exist; do not implement billing workflows merely to satisfy the row.
- Trust/safety requiredness may need subtype split before implementation if current flows mix essential and non-essential messages.

### 5.2 Scope Semantics

Supported editable surfaces:

- User: authenticated user's own notification preferences.
- Organization: organization default/policy for organization-scoped notifications.
- Group: group default/policy for group-scoped notifications.

Effective precedence:

1. User explicit value wins for user-owned notifications.
2. Group explicit value applies when the notification has group context, unless overridden by an allowed user value.
3. Organization explicit value applies when the notification has organization context, unless group/user is more specific.
4. Tenant and Instance provide seeded/operator defaults.
5. Broader locks prevent narrower overrides.
6. Required category/channel metadata overrides every explicit disabled value.

Use the existing hierarchy language carefully: the resolver may read broad-to-narrow for computation, but the effective value is deepest applicable scope unless locked.

## 6. Architecture Decisions

| Decision | Rationale | Consequence |
| --- | --- | --- |
| Use relational preference rows, not JSON blobs. | Matrix cells need per-category/channel edits, locks, defaults, unique constraints, audit, and delivery-path queries. | More schema upfront, less parsing and fewer divergent resolvers later. |
| Keep category/channel metadata as lookup-like data with `int` IDs. | Repo convention uses `int` for lookups and stable codes for seeded metadata. | Migrations/seeds must keep IDs/codes stable. |
| Keep matrix resolution in Application. | Controllers, Blazor, email workers, and notification fanout must not duplicate hierarchy/lock logic. | Add one resolver contract and tests before UI. |
| Keep API projections UI-ready. | Blazor should not compute requiredness, editability, lock source, or inherited value. | Query DTO includes categories, channels, cells, effective source, editability, mute state, and HAL-ready actions. |
| Gate all UI actions by HAL links. | Project invariant: HAL is the client source of truth for affordances. | Save/reset/mute buttons render/enable only from `_links`; no role checks in components. |
| Integrate Email and In-App separately. | In-app notifications and direct email dispatch are different current systems. | Delivery integration is split into two PRs and must not claim notification-to-email fanout exists. |

## 7. Proposed Data Model

Names may change during implementation, but these concepts must remain.

### 7.1 Category Metadata

`NotificationPreferenceCategory` or equivalent lookup-like entity:

- `int Id`
- stable `MasterCode`
- `FullName`
- `Description`
- `bool IsRequired`
- `string? RequiredReasonCode`
- `bool DefaultEmailEnabled`
- `bool DefaultInAppEnabled`
- `int SortOrder`
- audit/soft-delete only if consistent with current lookup base class

### 7.2 Channel Metadata

`NotificationPreferenceChannel` or equivalent lookup-like entity:

- `int Id`
- stable `MasterCode` values: `email`, `in_app`
- `FullName`
- `int SortOrder`

Do not add push/SMS/webhook channels in this feature. Add a new channel only when an actual delivery surface exists.

### 7.3 Scoped Preference Cells

`NotificationChannelPreference` or equivalent aggregate/entity:

- `Guid Id`
- `Guid TenantId`
- `SettingScope Scope`
- nullable target id: `UserId`, `OrganizationId`, `GroupId`
- `int CategoryId`
- `int ChannelId`
- `bool IsEnabled`
- `bool IsLocked`
- audit and soft-delete fields

Persistence requirements:

- Exactly one target ID for user/org/group rows.
- Unique active row per `(TenantId, Scope, TargetId, CategoryId, ChannelId)`.
- Tenant and soft-delete filters are model-level EF filters.
- Runtime code must never disable tenant filtering; if deleted rows are needed, ignore only soft-delete filters.
- Consider optimistic concurrency if concurrent matrix saves are expected; otherwise make full save idempotent and last-write-wins within one transaction.

### 7.4 Scoped Profile / Global Mute

Use a small scoped profile entity or settings-group-backed equivalent:

- `Guid Id`
- `Guid TenantId`
- `SettingScope Scope`
- nullable target id
- `bool MuteNonEssential`
- optional `bool IsLocked`
- audit/soft-delete fields

Keep global mute separate from cell rows so unmute restores previous cell choices.

## 8. Application Design

### 8.1 Resolver Contract

Create one Application contract for effective decisions, shaped like:

```text
(tenantId, recipientUserId, optional organizationId, optional groupId, categoryCode, channelCode)
  -> enabled, isRequired, isLocked, effectiveSourceScope, lockReason
```

The resolver must:

- batch metadata and scoped rows to avoid per-cell N+1 queries;
- return enabled for required categories;
- apply global mute only to non-required categories;
- treat all unchecked non-required cells as explicit opt-out;
- honor broader locks before narrower values;
- expose enough metadata for API projection and delivery decisions;
- use cancellation tokens end to end.

### 8.2 CQRS Requests

Queries:

- Get current user's effective matrix.
- Get organization's effective matrix.
- Get group's effective matrix.

Commands:

- Save editable scoped cell choices.
- Set scoped global mute.
- Reset scoped overrides when product confirms reset UX.

Handler rules:

- Manually instantiate validators.
- Repositories return entities; handlers map to DTOs.
- Validation/auth/read-only prefetch happens before transaction.
- Multi-row saves use `IUnitOfWork.ExecuteInTransactionAsync`.
- Required/locked cell write attempts fail with explicit validation/ProblemDetails semantics; do not silently weaken required policy.

### 8.3 API Projection

The API response should be directly renderable:

- categories: id/code/name/description/isRequired/requiredReason/sortOrder
- channels: id/code/name/sortOrder
- cells: categoryCode/channelCode/isEnabled/isEditable/isLocked/lockReason/effectiveSourceScope/defaultSourceScope
- `muteNonEssential`: value/editable/lock metadata
- `_links`: `self`, `save`, `set-mute`, `reset-overrides` where authorized

## 9. API And Authorization Plan

Prefer explicit authenticated endpoints:

- `GET /api/notifications/preferences/me`
- `PUT /api/notifications/preferences/me`
- `PUT /api/notifications/preferences/me/mute`
- `GET /api/organizations/{organizationId}/notification-preferences`
- `PUT /api/organizations/{organizationId}/notification-preferences`
- `GET /api/groups/{groupId}/notification-preferences`
- `PUT /api/groups/{groupId}/notification-preferences`

API requirements:

- Preference GETs are private and must require authenticated/resource-authorized access.
- Writes require `[Authorize]`, request validation, and handler-level resource checks.
- Org/group endpoints need explicit authorization resource kinds/actions in both Cerbos and local fallback policies.
- Routes need explicit names/templates and OpenAPI metadata for success, validation ProblemDetails, unauthorized, forbidden, and not found.
- HAL link policies must run through existing HATEOAS authorization evaluation and fail closed.
- No controller performs delivery side effects.

## 10. Blazor / BFF UX Plan

Build one reusable matrix component after the API projection exists.

UX requirements:

- Host in existing settings/navigation surfaces; inspect paths before adding a new shell.
- Render category rows and Email/In-App checkbox columns.
- Use accessible labels like `{Category} via {Channel}`.
- Render required cells as disabled checked with lock icon/copy.
- Show global mute helper copy: “Mutes non-essential notifications. Account security and required legal notifications still send.”
- Preserve selections while mute is enabled.
- Submit through service layer/BFF-safe clients; no direct token/API calls in components.
- State-changing BFF/API interactions need antiforgery where applicable.
- Show server validation errors in the form using project-standard validation feedback.
- Gate save/reset/mute controls by HAL `_links`, never by roles/claims.

## 11. Delivery Integration Plan

### 11.1 In-App

In-app notification producers/fanout should call the effective resolver before creating non-required `Notification` rows.

Acceptance:

- Opted-out non-required in-app notifications are not created.
- Required in-app notifications still create rows.
- Existing dedupe semantics apply only to rows that are actually created.
- SSE remains a refresh hint and does not carry preference logic.

### 11.2 Email

Email channel work must integrate with existing direct email dispatch boundaries.

Acceptance:

- Non-required opted-out email categories do not enqueue/send via `EmailDispatchOutbox` paths.
- Existing unsubscribe/preference checks remain at least as restrictive as before.
- Tenant pause, operator park/replay, skip outcomes, health checks, and scheduler behavior remain intact.
- Do not create notification-to-email fanout unless separately planned and accepted.

## 12. PR / Phase Split

### PR 1 — Data Foundation And Resolver

- Add category/channel metadata and scoped preference/profile persistence.
- Add repository contracts/implementations returning entities.
- Add migration and seed data.
- Add effective resolver and Application/Persistence tests.

### PR 2 — CQRS, API, HAL, Authorization

- Add matrix queries/commands/validators.
- Add authenticated user/org/group API endpoints.
- Add resource authorization and HAL link policy.
- Add API and authorization parity tests.

### PR 3 — Blazor/BFF Matrix Surface

- Inspect existing settings pages.
- Add reusable matrix component and hosting pages.
- Add service-layer/BFF calls and antiforgery-safe state changes.
- Add component/client tests and browser QA.

### PR 4 — Delivery Integrations

- Gate in-app notification fanout.
- Gate email dispatch paths without weakening current skip/unsubscribe behavior.
- Add resolver integration tests for opted-out and required flows.

### PR 5 — Operations, Docs, Cleanup

- Update canonical docs.
- Verify metrics/logging/health implications.
- Run full required per-project verification and manual QA.
- Close or archive active dev-doc workstream only after implementation lands.

## 13. Testing Strategy

Planning-only verification for this update:

- Read back all three markdown files.
- Confirm plan/context/tasks agree on current state, future state, PR split, and verification.
- Run scoped docs inspection if available.

Future implementation verification by slice:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.CleanArchitectureTests
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.CqrsPatternTests
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AuthorizationParityTests
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

Never run solution-level `dotnet test` as the canonical verification path.

## 14. Manual QA Gate For Implemented Feature

When code exists, manual QA must use the real surface:

1. Sign in through the Blazor/BFF surface.
2. Open the user notification preferences matrix.
3. Toggle Email only, In-App only, both, and neither for a non-required category.
4. Save and reload; persisted choices must survive reload.
5. Enable global mute; non-essential rows/channels must resolve disabled while required rows remain enabled.
6. Attempt to change required rows; UI should prevent and API should reject direct invalid writes.
7. Verify at least one in-app notification path respects opt-out.
8. Verify at least one email dispatch path respects opt-out or records a skip outcome.
9. Repeat org/group surface for an authorized admin/member and for a forbidden user to prove HAL/resource checks fail closed.

## 15. Security, Privacy, Abuse, And Compliance

- Preference reads expose private user/org/group policy data and are not anonymous content endpoints.
- Required categories must be narrow and justified; do not use “required” to bypass user consent for marketing or product announcements.
- Marketing defaults must remain off unless legal/product explicitly changes the consent model.
- Logs and metrics must not include email addresses, raw notification bodies, or preference payloads.
- Provider failures, authorization provider failures, and HAL batch failures must fail closed.
- Tenant isolation must rely on EF tenant filters and resource authorization; never trust client-supplied tenant/scope IDs without server authorization.

## 16. Multi-Tenancy And Self-Hosting Operations

- All preference entities are tenant-scoped where tenant context exists.
- Tenant/instance defaults may be operator-managed later, but this feature's user-facing scope is user/org/group.
- Cache keys, if used, must include tenant and scope target IDs.
- Cache invalidation must occur after successful preference writes.
- Self-hosted operators need deterministic seed data and migration behavior; no runtime dependence on SaaS-only services.
- Email readiness/health remains governed by existing email dispatch and SMTP health checks.

## 17. Observability

Add only low-cardinality operational signals during implementation:

- count of preference writes by scope and result;
- count of delivery suppressions by channel/category code, not by recipient;
- count of required/locked write rejections;
- structured logs with tenant/request correlation but no raw email/body payloads.

## 18. Migration And Compatibility

- This is additive until delivery integration begins.
- Seed defaults must preserve current effective behavior where possible: existing delivered notifications should continue unless a user explicitly opts out or global mute is enabled.
- Do not edit applied migrations. Add focused corrective migrations if needed.
- Provide a rollback stance for each PR: removing UI/API should not delete preference data; delivery integration can be disabled by reverting resolver calls while data remains.

## 19. Risk Register

| Risk | Severity | Mitigation |
| --- | --- | --- |
| Preference system accidentally suppresses critical/security messages. | High | Required metadata enforced in resolver and command handlers; tests for direct invalid writes and delivery paths. |
| Tenant filter disabled during repository queries. | High | Follow EF Core query-filter guidance; never disable Tenant filter on request paths; architecture/persistence tests. |
| UI drifts from authorization rules. | High | HAL `_links` only; no role/claim gating; authorization parity tests. |
| Email preferences are incorrectly applied to non-existent notification-to-email fanout. | Medium | Treat Email channel integration as direct `EmailDispatchOutbox`/lifecycle-email work only unless separately planned. |
| Category model overfits future channels. | Medium | Start with Email and In-App only; relational model permits later channels without speculative UI. |
| Organization/group authorization is underspecified. | Medium | Add explicit resource kinds/actions before endpoint work; require local/Cerbos parity. |
| Concurrent saves lose user intent. | Low/Medium | Prefer idempotent full-matrix save in one transaction; add concurrency token only if UX needs conflict detection. |

## 20. Definition Of Done

The feature is done only when:

- User/org/group matrix APIs exist and are authenticated/resource-authorized.
- Blazor renders a checkbox matrix with HAL-gated save/reset/mute actions.
- Required categories are enforced server-side and displayed accessibly.
- Global mute suppresses non-essential notifications without erasing row choices.
- Effective resolver is used by in-app and email delivery paths selected for this feature.
- Tenant isolation, ProblemDetails, antiforgery/BFF, and outbox boundaries remain intact.
- Release build and relevant per-project tests pass.
- Manual QA has driven the feature through Blazor/API/delivery surfaces.
- Canonical docs are updated and active dev-docs record final verification.

## 21. Implementation Agent Contract

Before editing code, the implementation agent must:

1. Re-read `AGENTS.md`, `.claude/contract/intents.yaml`, `docs/QUICK_REFERENCE.md`, and path-specific rules.
2. Re-open this plan, context, and task checklist.
3. Inspect exact source paths for the slice being changed.
4. Keep production code changes inside the selected PR slice.
5. Update `notification-preference-matrix-context.md` and `notification-preference-matrix-tasks.md` before handoff.
6. Report any deviation from this plan as an explicit decision, not an accidental drift.
