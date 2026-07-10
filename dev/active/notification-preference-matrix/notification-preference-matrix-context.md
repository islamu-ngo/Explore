<!-- ABOUTME: Resume context for the Notification Preference Matrix planning workstream. -->
<!-- ABOUTME: Captures verified source facts, Context7 evidence, decisions, constraints, risks, and next-step guidance for implementation agents. -->

# Notification Preference Matrix — Context

Last Updated: 2026-07-11 Europe/Brussels

## 1. Session Progress

### Completed

- Classified this as a planning-only `/dev-docs` workstream update, then began implementation after user approval.
- Loaded repository governance and relevant skills: Clean Architecture, CQRS/MediatR, EF Core, auth, Blazor BFF, Blazor UI, and outbox.
- Used Context7 as requested for current EF Core, ASP.NET Core, Blazor, and browser Push/Notification API guidance.
- Re-read canonical docs for notifications, email notifications, API, Blazor, multi-tenancy, authorization, architecture, domain model, operations, and governance.
- Inspected concrete source anchors for current notification entities, email outbox, notification repository contract, hierarchical settings resolver, and existing static preference category constants.
- Rebased all three workstream files so plan/context/tasks agree on current state, implemented design, PR split, risk controls, and verification.
- Implemented PR 1 data foundation and resolver: Domain metadata/entities, Application contracts, Persistence EF configs/repositories/resolver, runtime lookup seeding, migration `20260709160022_AddNotificationPreferenceMatrixFoundation`, and persistence integration tests.
- Verified PR 1 with `dotnet build --configuration Release --verbosity quiet` and `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --no-progress --maximum-failed-tests 1` passing 259/259 tests.
- Implemented PR 2 current-user, organization, and group CQRS/API/HAL endpoints: matrix DTOs, query handlers, save/mute commands, route names, controller endpoints, HAL policy/assembler, and OpenAPI HAL schema catalog registration.
- Verified PR 2 scoped Application behavior with full `Event.Application.UnitTests` passing 2089/2089 tests; `Explore.Application` and `Explore.Persistence` Release builds passed, and `Explore.API` `--no-dependencies` Release build passed.
- Implemented PR 3 Blazor matrix surface: generated-client service methods for user/organization/group preference matrices, reusable `NotificationPreferenceMatrix` component, user Settings Notifications surface, and organization/group profile Notifications tabs.
- Verified PR 3 with `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet`, `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet --no-dependencies`, and `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet --no-build -- --no-progress --maximum-failed-tests 1` passing 1564/1565 tests with one pre-existing skip.
- Implemented PR 4 delivery integration: in-app event-published, moderation, and registration fallback producers now consult `INotificationPreferenceResolver`; `EmailDispatchDrainService` records matrix-disabled email as skipped before SMTP provider handoff while preserving legacy unsubscribe and outbox semantics.
- Verified PR 4 with `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet`, `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --no-progress --maximum-failed-tests 1` passing 2091/2091 tests, `dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --verbosity quiet`, and `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --maximum-failed-tests 1` passing 710/710 tests.
- Updated canonical docs for API, Domain, Blazor, Notifications, Email Notifications, and Authorization.
- Ran final verification: full Release build passed; Application, Domain, Architecture, Secrets, Persistence, Infrastructure, Blazor integration, and Blazor client test projects passed. `Event.API.IntegrationTests` still has two unrelated event-registration notification-intent FK failures.
- Used Context7 follow-up research to implement Web Push as an end-user browser channel that requests consent only after explicit user action, and kept Svix/webhooks separate as developer-facing external integration settings/App Portal flows.
- Product explicitly activated PR 6 Web Push on 2026-07-10 and required flood prevention through bounded TTL, transport coalescing, displayed-notification replacement, active-tab suppression, and device-level summary grouping.
- The 2026-07-10 Release baseline passed before edits. Discovery found an in-progress backend foundation already in the worktree: `push` metadata, tenant-scoped subscriptions, CQRS/API/HAL endpoints, durable dispatch outbox, VAPID sender, retry/stale-subscription handling, health checks, migration, and focused tests.

### PR 6 Completed

- Completed standards-correct transport safety (`TTL`, `Urgency`, and Web Push `Topic`), Infisical `/api` VAPID mapping, anonymous plain-text public-key discovery through the generated Blazor API client/service, browser consent/subscription interop, service-worker active-tab suppression, displayed-notification `tag` replacement, bounded summary grouping, opt-out cleanup, connector-time DNS-rebinding/SSRF protection, and local Chromium QA.
- Preserved and hardened the existing partial backend foundation rather than creating a parallel implementation.

### Next Step

Configure operator VAPID keys in a deployed environment and perform final push-network delivery/coalescing verification against supported browsers. Do not change the unrelated event-registration/API failures as part of this workstream.

## 2. Quick Resume

The requested feature is a notification preferences matrix: categories as rows; Email and In-App as independent checkbox columns. Users may enable Email, In-App, both, or neither. Neither checked means explicit opt-out for a non-required category. A global mute suppresses non-essential notifications without deleting row-level choices. Critical categories such as account security and required legal/billing notifications are locked on, shown as disabled checked cells, and enforced server-side.

The implementation must be normalized and scope-aware, not a JSON blob or radio-style enum. It must support user, organization, and group editable surfaces, while aligning with existing Instance/Tenant/Organization/Group/User hierarchy and broader-scope lock semantics. HAL `_links` remain the only source of truth for Blazor action affordances.

Web Push is now an implemented PR 6 end-user channel. It is not the same thing as Svix/product webhooks: Push depends on browser consent, service workers, Push API subscriptions, and VAPID-authenticated delivery; Svix/webhooks deliver product events to external systems through developer settings/App Portal.

## 3. Verified Current-State Facts

| File / Area | Verified Fact | Handoff Meaning |
| --- | --- | --- |
| `docs/QUICK_REFERENCE.md` | Repositories return entities; validators are manually instantiated; lookup IDs are `int`; aggregate IDs are `Guid`; HAL links gate UI; tenant isolation is central. | Do not introduce DTO repositories, DI validators, role-gated Blazor buttons, or tenant-filter bypasses. |
| `docs/GOVERNANCE.md` | Clean Architecture boundaries and transaction rules are explicit. | Resolver/CQRS in Application, EF in Persistence, HAL in API, UI in Blazor. |
| `docs/NOTIFICATIONS.md` | Current notification system is durable in-app notification rows; SSE only hints refresh; no email fanout from notification rows. | In-App and Email preference integrations are separate. |
| `docs/EMAIL_NOTIFICATIONS.md` | Direct SMTP email dispatch exists through `EmailDispatchOutbox`, drain service, tenant pause, skip outcomes, scheduler fallback, and SMTP health. | Email preferences must integrate with existing dispatch paths without weakening skip/unsubscribe behavior. |
| `docs/MULTI_TENANCY.md` | Hierarchical settings cascade and lock semantics exist; tenant resolution must fail closed. | Reuse/align with hierarchy and include tenant/scope in cache keys. |
| `docs/AUTHORIZATION.md` | AuthorizationBehavior and HAL evaluator fail closed; local/Cerbos parity matters. | Org/group endpoints require explicit resource actions and HAL parity tests. |
| `docs/BLAZOR.md` | BFF boundary keeps tokens out of browser; state-changing routes require antiforgery; components use service layer and HAL. | UI must not call API directly or inspect roles/claims for actions. |
| `docs/WEBHOOKS.md` | Product webhooks use Local/Svix/Composite provider modes and backend-generated Svix App Portal URLs; browser never receives Svix API tokens or signing secrets. | Do not model `webhook` as a notification preference matrix channel. |
| `Explore.Domain/Constants/NotificationPreferenceCategories.cs` | Existing static codes are only `registration-confirmations`, `organizer-announcements`, `event-reminders`, `event-updates`. | A richer category metadata model is proposed, not already implemented. |
| `Explore.Application/Contracts/Infrastructure/IHierarchicalSettingsResolver.cs` | Contract supports Instance/Tenant/Organization/Group/User resolution, batch reads, metadata, locks, set/remove, cache invalidation. | Prefer alignment/reuse over creating a second cascade implementation. |
| `Explore.Application/Contracts/Persistence/INotificationRepository.cs` | Existing notification repository returns `Notification` entities and covers in-app notification state. | New preference repositories should follow entity-returning convention. |
| `Explore.Application/Contracts/Persistence/IEmailDispatchOutboxRepository.cs` | Existing repository supports claim/send/fail/skip/tenant pause/operator actions for email outbox. | Email preference suppression likely belongs before enqueue or as skip decision in current dispatch path. |
| `Explore.Domain/Notification.cs` | Tenant-scoped auditable soft-deletable in-app entity with type/scope/reason/dedupe/read/archive/snooze. | Do not mutate delivered rows to represent preferences; gate creation/fanout. |
| `Explore.Domain/EmailDispatchOutbox.cs` | Tenant-scoped durable email outbox with status, attempts, lease, dead-letter/park/unknown, provider/RabbitMQ metadata. | Preserve outbox reliability semantics when adding Email channel checks. |

## 4. Context7 Evidence

- EF Core docs (`/dotnet/entityframework.docs`): use model-level `HasQueryFilter` for soft delete and multi-tenancy; avoid broad `IgnoreQueryFilters()` on request paths because it can remove tenant isolation; async transactions and optimistic concurrency are the canonical tools for multi-row writes and conflicts.
- ASP.NET Core docs (`/dotnet/aspnetcore.docs`): ProblemDetails/validation payloads are structured RFC7807 JSON; antiforgery middleware belongs after authentication/authorization; endpoint metadata should describe problem and validation responses.
- Blazor docs (`/websites/learn_microsoft_en-us_aspnet_core`): `EditForm` supports enhanced form handling and named forms; validation uses validators/summaries/custom server-side errors; Blazor manages validation ARIA attributes.
- Blazor docs (`/websites/learn_microsoft_en-us_aspnet_core_blazor`): Blazor components access browser APIs through JavaScript interop; WebAssembly/client execution runs inside the browser security sandbox; PWA/offline behavior uses service workers.
- MDN Web Docs (`/mdn/content`): check support with `"Notification" in window`, inspect `Notification.permission`, request permission only after user interaction, stop bothering the user after denial, subscribe with `serviceWorkerRegistration.pushManager.subscribe()` from a user gesture, and send the resulting `PushSubscription` endpoint/key material to the server.
- MDN Web Docs (`/mdn/content`): `NotificationOptions.tag` replaces a previously displayed notification with the same tag; `ServiceWorkerRegistration.getNotifications()` inspects displayed notifications; `clients.matchAll({ type: "window" })` supports active-window suppression/focus behavior; `notificationclick` should focus an existing app window before opening a new one.
- Context7 Web Push docs (`/web-push-libs/web-push`): the transport `TTL` bounds push-service retention, `Urgency` communicates delivery priority, and Web Push `Topic` coalesces pending messages for one subscription. `Topic` is limited to 32 URL/filename-safe Base64 characters. This is distinct from display-layer `tag` replacement.

## 5. Key Decisions

1. Use checkbox semantics, not radio semantics.
2. Store matrix state as relational category/channel/scope rows.
3. Keep category/channel metadata server-defined with stable codes and `int` lookup-like IDs.
4. Keep global mute separate from row choices so unmute restores individual selections.
5. Enforce required categories in Application command/resolver logic and render disabled checked cells in UI.
6. Keep effective preference resolution behind one Application service.
7. Keep API projections UI-ready: cells include enabled/editable/locked/source/reason metadata.
8. Gate Blazor save/reset/mute by HAL `_links` only.
9. Split delivery integration into In-App and Email slices because current systems are separate.
10. Add `push` only as the dedicated PR 6 slice with browser consent UX, service-worker registration, subscription persistence, VAPID/operator configuration, durable delivery, and opt-out cleanup.
11. Apply flood control at both layers: Web Push `Topic` coalesces queued transport messages; Notification `tag` replaces already displayed notifications. Do not describe `tag` as a push-service collapse key.
12. Every push send uses an explicit category policy for bounded `TTL` and `Urgency`; stale social/product updates expire rather than accumulating over long offline periods.
13. The service worker suppresses OS notifications while a same-origin app window is visible, posts a refresh message to open clients, and caps displayed notifications by replacing excess items with one non-sensitive summary.
14. Push payloads contain refresh/navigation hints and stable category/topic identifiers only; durable authenticated notification state remains the source of truth.
15. Svix/webhooks are not notification channels; keep them in webhook/developer settings and App Portal flows.

## 6. Key Files And Responsibilities

| Area | Likely Files / Patterns | Responsibility |
| --- | --- | --- |
| Domain metadata | new/extended lookup-like entities near notification/domain lookup patterns | category/channel stable codes, default states, required metadata |
| Domain preference rows | new tenant-scoped auditable soft-deletable entities | explicit scoped cell choices and global mute/profile state |
| Application contracts | `Explore.Application/Contracts/**` | repository contracts, resolver contract, DTOs/projections |
| Application handlers | feature folder under `Explore.Application/**` | queries/commands, manual validators, transaction orchestration, DTO mapping |
| Persistence | `Explore.Persistence/**` configs/repos/migrations | EF configuration, filters, constraints, seed data, entity repositories |
| API | `Explore.API/**` controller/HAL policy/authorization metadata | authenticated endpoints, ProblemDetails, HAL links, resource checks |
| Blazor/BFF | existing settings components/services after path inspection | reusable matrix UI, service calls, antiforgery-safe state changes, HAL gating |
| In-app delivery | existing notification fanout/producers | resolver call before non-required notification rows are created |
| Email delivery | existing email dispatch factory/drain/outbox paths | resolver call/skip behavior without weakening existing unsubscribe/pause semantics |
| Web Push | `WebPushSubscription`, `WebPushDispatchOutbox`, CQRS/API endpoints, `WebPushSubscriptionPanel`, `web-push.js`, `push-service-worker.js`, `WebPushDispatchDrainService`, and `WebPushNotificationSender` | explicit browser consent, tenant/user/device storage, VAPID delivery, TTL/topic/tag flood controls, and stale-subscription cleanup |
| Webhooks/Svix | existing webhook settings and Svix App Portal flow documented in `docs/WEBHOOKS.md` | external-system delivery outside notification preference matrix |
| Tests | per-project test projects only | focused domain/application/persistence/API/Blazor/architecture coverage |

## 7. Constraints And Rules

- Every new source file starts with two ABOUTME comment lines.
- Domain must not reference EF Core, MediatR, ASP.NET Core, or Blazor.
- Application must not use `ExploreDbContext` directly.
- Repositories return entities, not DTOs.
- Validators are manually instantiated.
- Multi-row writes use the unit-of-work transaction pattern.
- Tenant filters must remain active on runtime request paths.
- Preference GETs are private and require authentication/resource authorization despite the public-content GET default.
- HAL `_links` is the UI source of truth for actions.
- Browser never sees access tokens; Blazor components use BFF/service layer.
- State-changing BFF/API interactions need antiforgery where applicable.
- Controllers/handlers/domain objects do not perform direct SMTP/RabbitMQ/provider side effects.
- Existing unsubscribe/skip behavior must remain at least as restrictive after Email channel integration.
- Web Push permission prompts and `PushManager.subscribe()` must happen only after an explicit user click/tap, never on page load.
- `Notification.permission === "denied"` must be respected with helper copy and no repeated prompt loop.
- Web Push transport coalescing must use the standards `Topic` header; display replacement must use `NotificationOptions.tag` with `renotify: false`.
- TTL must be explicit and bounded per category. Retry scheduling must never outlive the message TTL.
- The service worker must suppress popups when an app window is visible, notify open clients to refresh, and replace an excessive displayed set with one generic summary.
- Subscription endpoints are untrusted input: validate HTTPS/key shapes during enrollment and enforce DNS/private-network blocking plus redirect refusal at provider handoff.
- Do not add a `webhook` channel to notification preference metadata; webhook/Svix permissions remain separate developer integration configuration.

## 8. Validation Baseline

Documentation validation for this update:

- Read back plan/context/tasks.
- Confirm all three files agree on current state, implemented state, PR split, and verification.
- Run scoped docs/diagnostic inspection where available.

Implementation baseline:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.CleanArchitectureTests
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.CqrsPatternTests
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AuthorizationParityTests
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

Run only tests relevant to touched paths, plus Release build. Do not use solution-level `dotnet test` as the canonical path.

Current verification evidence:

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed.
- `dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet` passed.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet --no-dependencies` passed.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --no-progress --maximum-failed-tests 1` passed 2089/2089 tests.
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --no-progress --maximum-failed-tests 1` passed 259/259 tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --maximum-failed-tests 1` passed 262/263 tests with one pre-existing skip.
- Full `Event.API.IntegrationTests` previously reached only unrelated event-registration failures caused by `notification_intents` foreign keys to `notification_categories`; new notification preference HAL/OpenAPI failures were cleared.
- `dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --verbosity quiet` passed.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --maximum-failed-tests 1` passed 710/710 tests.
- PR 6 verification on 2026-07-11: Domain 320/320, Application 2107/2107, Persistence 270/270, Infrastructure 746/746, Architecture 262 passed with one existing skip, Secrets 202/202, Blazor integration 230/230, and Blazor client 1578 passed with one existing skip.
- Full API integration reached 1682 passed, three existing skips, and the same two unrelated event-registration `notification_intents.category_id` foreign-key failures documented above; no Web Push API/HAL failure occurred.
- Release build passed across 28 projects with zero errors. JavaScript syntax, Compose rendering, diff whitespace, public Web Push configuration, static asset delivery, and real Chromium module/service-worker loading passed.
- Chromium observed zero permission requests and zero service-worker registrations while loading browser state; explicit worker registration produced root scope. Responsive 375/768/1280 checks had no horizontal overflow. Full provider-network delivery remains environment-dependent because local Web Push is intentionally disabled without operator VAPID keys.

## 9. Known Risks / Unknowns

- Billing may be future/tenant-conditional; do not invent billing workflows for this feature.
- Trust-safety requiredness may need a category/subtype split before implementation.
- Organization/group authorization rules require concrete resource actions and parity tests.
- Email channel semantics must not pretend current in-app notifications automatically send email.
- Concurrency requirements for matrix saves are not yet product-specified; start idempotent/transactional unless conflict UX is requested.
- Browser/provider behavior still varies by platform; deployed-environment acceptance must cover supported-browser denied/default/granted states, subscription expiration, permanent push-service failures, and live VAPID delivery/coalescing before production enablement.
- Web Push payloads must avoid sensitive bodies; prefer minimal hints that cause the authenticated app to refresh durable notification state.
- `NotificationOptions.tag` does not coalesce messages waiting at a push service. Missing the transport `Topic` header can still produce a startup burst even when display replacement later reduces the visible stack.
- Do not conflate Web Push with Svix/webhooks; one is end-user browser delivery, the other is external-system event delivery.

## 10. Handoff Notes

### 2026-07-11 Europe/Brussels

PR 1 through PR 5 are implemented. PR 6 adds browser Push subscription persistence, authenticated CQRS/API/HAL flows, explicit Blazor consent and opt-out UX, VAPID configuration, a durable dispatch worker, bounded TTL/urgency/topic delivery policy, active-tab suppression, stable display-tag replacement, generic summaries, click-to-focus handling, permanent stale-subscription cleanup, and connector-time DNS-rebinding/SSRF protection. Context7 evidence informed the browser and transport boundaries; Svix/webhooks remain outside the matrix. Canonical automated verification and local Chromium safety checks are complete; external push-service delivery requires operator VAPID configuration.

Before starting code, re-open `.claude/contract/intents.yaml`, path-specific `.claude/rules/*.md`, and the relevant canonical docs for the exact paths being edited. Update this context file before any handoff or context compaction.
