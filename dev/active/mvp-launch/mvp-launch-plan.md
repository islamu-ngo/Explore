<!-- ABOUTME: Source-grounded launch-closure implementation plan for the MVP release. -->
<!-- ABOUTME: Replaces the stale work-package backlog with current architecture, risks, phases, and verification. -->

# MVP Launch Implementation Plan

Last Updated: 2026-07-04 Europe/Brussels

Status: In implementation. Re-baselined plan is current; Phase 2 email dispatch compliance received its first implementation slice on 2026-07-04; Phase 3 registration integrity now has source/test/docs closure for capacity, event/day/session duplicate parent-intent handling, duplicate email-dispatch-row prevention, API boundary idempotent-create behavior, real PostgreSQL API repeat-submit coverage, waitlist behavior, unauthenticated create rejection, generated-client response agreement, Blazor service safe error mapping, registration UI outcome state/copy, and HAL-gated event-detail registration affordances. Phase 1 runtime proof is not green: 2026-07-04 full-local Aspire attempts proved infrastructure startup, but the launch path is blocked by unstable AppHost/CLI lifecycle evidence, including an earlier `explore-api` `Running -> Finished` transition and a later controlled run where API/Blazor reached listening ports before the Aspire CLI timed out waiting for the AppHost backchannel and `aspire ps` returned no running AppHost.

## 0. Planning Metadata

| Field | Value |
|---|---|
| Request | Refresh and improve `dev/active/mvp-launch` because the old implementation plan is stale. |
| Planning mode | Senior CTO feedback: repository-grounded, implementation-ready rewrite. |
| Contract classification | Planning/documentation workstream. No single `.claude/contract/intents.yaml` intent fully covers it. Implementation slices must re-classify against the relevant concrete intents before editing code. |
| Likely implementation intents | `add-write-endpoint`, `add-get-endpoint`, `add-hal-link`, `add-cqrs-handler`, `update-repository-query`, `add-ef-migration`, `blazor-component-affordance`, `bff-auth-bug`, `openapi-contract-change`, `ci-cd-change`, `external-infrastructure-bootstrap`. |
| Required rule baseline | `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/OPERATIONS.md`, matching `.claude/rules/*.md`, and matching `.agents/skills/*/SKILL.md`. |
| Scope | Launch closure across API, Application, Persistence, Infrastructure, Blazor BFF/client, operations, tests, and docs. |
| Current implementation posture | The repo is not at a greenfield MVP start. Several old backlog items are already implemented and need runtime proof, compliance hardening, or documentation instead of rebuilds. Phase 2 has dispatch-time preference enforcement and unsubscribe affordances; Phase 3 now has persistence duplicate-registration, duplicate dispatch-row, API-boundary, real database-backed repeat-submit, waitlist, unauthenticated-create, generated-client/service, UI state/copy, and HAL-affordance evidence. |

## 1. Executive Summary

This workstream should now be treated as an MVP launch closure program, not as the old May 2026 feature backlog.

The source audit shows that major launch foundations already exist:

- Data Protection key persistence exists through `DataProtectionKeyContext`, service registration, migration-service migration, and persistence integration tests.
- Registration now uses `CreateEventRegistrationCommandHandler` plus `EventRegistrationIntentRepository.CreateWithChildrenAndCapacityAsync` to create the parent intent, child session rows, capacity reservations, and an `EmailDispatchOutbox` row in one serializable transaction.
- Registration duplicate protection is backed by partial unique indexes on active registration intents, plus a child-level unique session/user registration index. Phase 3 tests found the event-scope index was configured but missing from migrations because same-column EF indexes were unnamed; this has been corrected with distinct EF model index names and a focused migration. Duplicate retry attempts are now also proven not to persist a second registration-confirmation dispatch row.
- Registration client/UI handling keeps the generated `BaseCommandResponseOfGuid` contract stable: `EventRegistrationService` maps generated-client failures to bounded safe messages, `EventListRegistrationWorkflow.ResolveOutcome` centralizes confirmed/waitlisted/already-registered/failed states, and event detail registration affordances are proven to render only from the HAL `register` link.
- Basic Email Dispatch Mode exists: `EmailDispatchOutbox`, attempts, receipts, tenant pause, park/replay, stale processing recovery, TickerQ trigger, hosted drain service, health check, metrics, and operations documentation.
- Calendar export, sitemap, robots, render-policy classification, canonical URL helpers, and Open Graph/Twitter metadata have source-complete pieces. `docs/SEO.md` explicitly scopes this as public-discovery primitives, not site-wide SEO automation.
- HAL-driven UI helpers exist and are used heavily on event screens, but some role-helper paths still need launch audit.
- Email unsubscribe token/controller/preference storage exists, and the email dispatch drain now integrates `List-Unsubscribe`, visible unsubscribe links, and dispatch-time preference checks for mapped lifecycle categories when a valid public base URL is configured. Runtime Mailpit proof remains open.
- Runtime startup is the current blocker. On 2026-07-04, Release build passed before startup, full-local Aspire child logs showed the distributed application reached "started," but runtime proof still failed. One attempt showed `explore-api` changing `Running -> Finished`; a later controlled attempt showed API and Blazor listening temporarily, then the installed `13.3.0-preview` Aspire CLI timed out waiting for the AppHost backchannel while this repository pins Aspire `13.4.6`.

The old plan's largest architectural error is that it still points toward a generic `OutboxMessage` email handler flow. The current canonical path for registration and lifecycle email is the specialized `EmailDispatchOutbox` pipeline. Do not build a parallel generic email-dispatch implementation for launch.

Launch should focus on six closure phases:

1. Runtime proof and deployment readiness.
2. Registration email compliance and dispatch hardening.
3. Registration integrity and concurrency evidence.
4. Security, audit, and HAL affordance cleanup.
5. Public SEO, manifest, accessibility, and UX polish.
6. Contract, generated-client, E2E, docs, and release evidence.

## 2. Source-Grounded Current State

### 2.1 Registration and Capacity

| Claim | Evidence | Launch interpretation |
|---|---|---|
| Registration writes are no longer a simple child-row insert. | `CreateEventRegistrationCommandHandler` builds an `EventRegistrationIntent`, child `EventRegistration` rows, and an optional `EmailDispatchOutbox`. | Plan around the parent intent model. Do not design new registration features against only `EventRegistration`. |
| Capacity reservation is atomic per session. | `EventRegistrationIntentRepository.TryReserveSessionCapacityAsync` updates `event_sessions.current_audience_attendees` only when capacity remains, inside a serializable transaction. | Add runtime/concurrency proof, not a new reservation design. |
| Active duplicate protection exists. | `EventRegistrationIntentConfiguration` has partial unique indexes for event, day, and session-selection scopes; event/session same-column partial indexes now use distinct EF model names; `EventRegistrationConfiguration` has a unique active session/user index; `EventRegistrationRealRuntimeTests` repeats the same session-selection POST through the real PostgreSQL API host. | Event/day/session duplicate races, duplicate email-dispatch row prevention, real API repeat-submit behavior, generated-client agreement, and Blazor UI state/copy are now covered; remaining launch work is runtime/visual release evidence. |
| Waitlisting is represented at child-session level and bubbles to intent status. | Repository sets child `ApprovalStatusId` to approved or waitlisted, then sets parent status based on any waitlisted session; `EventRegistrationRealRuntimeTests` proves a full session returns the waitlist message, leaves capacity unchanged, stores waitlisted child/intent status, and still creates one registration-confirmation dispatch row. | UI copy now reflects mixed session results through shared Blazor outcome classification; runtime visual proof still belongs to release evidence. |
| Blazor registration outcomes are centralized. | `EventListRegistrationWorkflow.ResolveOutcome`, `EventRegistrationService`, `EventRegistration`, `EventList`, `EventPreviewWorkspace`, and Blazor client tests. | Confirmed, waitlisted, idempotent already-registered, and failed states now share one classifier and safe error mapping across registration entry points. |
| Event detail registration affordance is HAL-gated. | `EventDetailsSidebar.CanRegisterSelectedEvent` checks `EventDetail.HasHalLink("register")`; `EventDetailsSidebarTests` proves the button appears only when the `register` HAL relation exists. | Registration action visibility follows API/HAL authority, not local role or claim checks. |

### 2.2 Email Dispatch

| Claim | Evidence | Launch interpretation |
|---|---|---|
| Registration confirmation email is created durably in the registration transaction. | `IEventLifecycleEmailOutboxFactory.CreateRegistrationConfirmation` and `CreateWithChildrenAndCapacityAsync(..., emailDispatchOutbox)`. | Keep email creation in Application/Persistence; do not send SMTP from handlers/controllers. |
| Email dispatch has a specialized outbox pipeline. | `EmailDispatchOutbox`, `EmailDispatchAttempt`, `EmailDispatchReceipt`, `EmailDispatchOutboxRepository`, `EmailDispatchDrainService`, TickerQ scheduled trigger, health check. | This is the launch email backbone. Harden it instead of re-architecting it. |
| Operator control exists. | Tenant pause, park, replay, stale processing recovery, attempts, receipts, and health are implemented. | Verify through integration/runtime tests and document operator runbooks. |
| Unsubscribe foundation exists and is now wired into dispatch. | `EmailUnsubscribeController`, `EmailUnsubscribeTokenService`, `UserNotificationPreferenceRepository`, `EmailDispatchDrainService`, skipped dispatch status/tests, and unsubscribe tests. | Remaining launch work is runtime proof through Mailpit and owner confirmation that category-level opt-outs should apply to registration confirmations. |

### 2.3 Public Launch Surface

| Area | Current evidence | Launch gap |
|---|---|---|
| Calendar export | `EventController.GetEventCalendar`, `IcalNetEventCalendarFileBuilder`, calendar integration/unit tests. | Smoke public URL behavior, auth posture, caching, content type, and generated client compatibility. |
| Sitemap | `SitemapController` under `sitemap.xml`; `docs/SEO.md` documents static public routes, published public event URLs, forwarded host/proto handling, and clamped event projection. | Runtime URL proof, endpoint-level tests if missing, and crawlability check. |
| Robots | Blazor `RobotsController`; `docs/SEO.md` documents production allow plus non-production `Disallow: /`. | Runtime proof with correct host/canonical sitemap and environment-sensitive indexing behavior. |
| Canonical and social metadata | `CanonicalUrlHelper`, `CanonicalMetadataTests`, `EventDetail.razor` metadata, and `docs/SEO.md` public-discovery scope. | Verify all public event routes; JSON-LD automation was still absent by source search on 2026-07-04, so add it only if launch owner wants structured-data coverage. |
| PWA/manifest | No web manifest was found by source search on 2026-07-04; only `favicon.ico` and `Icon_landingpage.png` were found as candidate assets. | Decide if installability is launch scope. If yes, add manifest and icons; service worker/offline remains deferred. |

### 2.4 Runtime Evidence Gap

| Claim | Evidence | Launch interpretation |
|---|---|---|
| Full-local infrastructure can be created by Aspire. | 2026-07-04 `aspire start --apphost Explore.AppHost/Explore.AppHost.csproj --format Json --non-interactive --isolated` child log showed PostgreSQL, Redis, RabbitMQ, Mailpit, CockroachDB, Keycloak, Cerbos, MinIO, Svix, Coop, Osprey, Prometheus, Grafana, and migration service reaching ready/running states. | Do not redesign AppHost topology before diagnosing the managed .NET resource lifecycle. |
| Full-local application runtime is not yet proven. | Two 2026-07-04 attempts disagree on the immediate symptom but agree on the result. The earlier child log showed `Distributed application started`, then `explore-api` changed `Running -> Finished`. The later controlled run showed `event-migrationservice` finished, `explore-api` and `explore-blazor` reached `Running`, API listened on `http://localhost:38665`, Blazor listened on `http://localhost:32773`, then the parent CLI exited 2 after timing out waiting for the AppHost backchannel and `aspire ps --format Json` returned `[]`. | Phase 1 must first eliminate the CLI/AppHost lifecycle variable before spending more code time on API internals: align the Aspire CLI to the repo's AppHost SDK version or run a foreground AppHost, then repeat startup and only debug API exit if it still reproduces. |
| Mailpit exists but runtime email proof is blocked by app lifecycle. | Docker showed `mailpit-4ccf7fcf` healthy after the failed run; isolated mode published random local host ports rather than the normal fixed local defaults. | Mailpit evidence must use Aspire resource endpoints or actual Docker port mappings for isolated runs instead of assuming fixed host ports. |
| Aspire CLI drift is now the primary diagnostic variable. | `aspire --version` returned `13.3.0-preview.1.26221.24`; `Explore.AppHost/Explore.AppHost.csproj` and Aspire hosting packages are pinned to `13.4.6`; the parent CLI log waited for `/home/amir/.aspire/cli/backchannels/auxi.sock.*` and timed out even though the child log reached `Distributed application started`; Context7 official docs expect `aspire start --format json` to return a launch result and `aspire ps` to list running AppHosts. | Update/align the Aspire CLI to `13.4.6`, or run the AppHost foreground through `dotnet run --project Explore.AppHost/Explore.AppHost.csproj` as a control, before declaring an application startup defect. |
| Smoke checks after CLI timeout are not valid runtime proof. | After the controlled timeout, `/alive` and `/health` curls failed or returned Blazor 500s because dependencies had already disappeared; the Blazor stack was an EF/Npgsql connection-refused path to `127.0.0.1:35051` through tenant resolution. | Health/public endpoint smoke must run while AppHost/API/Blazor are still alive, using endpoints discovered from Aspire or process environment, not after the parent CLI has torn down or detached from the topology. |

### 2.5 Security, BFF, and HAL

| Area | Current evidence | Launch gap |
|---|---|---|
| BFF token boundary | Project docs and BFF rules require tokens to remain server-side and antiforgery for proxied writes. | Regression tests and runtime smoke should verify no token leaks and writes remain protected. |
| HAL affordance gating | `HalResourceExtensions` and event pages use `_links` for many edit/delete/register actions. | Audit remaining `RoleHelper`/local-role paths and distinguish harmless display helpers from action affordance gates. |
| Setup secret boundary | `docs/SECURITY-MODEL.md` defines stripping and server-side setup secret rules. | Verify metadata endpoint rate limiting, 429 behavior, and no secret leakage. |
| Audit and PII | Audit, tenant, and PII docs are in place. | Ensure launch-critical writes and admin actions have audit evidence and that audit views are permission-gated. |

## 3. Target Future State

The launch target is a small, self-hostable MVP where the critical public and registration workflows can be exercised end-to-end in a reproducible environment.

### 3.1 Registration Control Flow

```text
User submits registration
  -> API write endpoint, authenticated and antiforgery-protected through BFF
  -> MediatR command handler validates with manually instantiated validator
  -> Handler resolves event, user, scope, sessions, policy snapshot, and email target
  -> Repository opens serializable transaction
  -> Session capacities are reserved with conditional SQL updates
  -> EventRegistrationIntent and EventRegistration children are inserted
  -> EmailDispatchOutbox row is inserted in the same transaction
  -> TickerQ or hosted drain service claims dispatch work
  -> Drain service checks tenant pause, unsubscribe/preference state, receipt idempotency, and SMTP provider result
  -> Attempt, receipt, status, metrics, and health state are updated
```

### 3.2 Architectural Principles

- Clean Architecture remains strict: Domain has entities and invariants; Application owns commands/queries and mapping; Persistence owns EF and SQL; Infrastructure owns provider integrations; API/Blazor own transport/UI.
- Repositories return entities, not DTOs.
- Validators are manually instantiated in handlers.
- Public `GET` endpoints are anonymous when intended; writes require authorization.
- HAL `_links` are the source of truth for UI action affordances.
- Outbox-style side effects are durable database intents. Controllers and handlers do not call SMTP, RabbitMQ, TickerQ jobs, or external providers directly.
- Tenant isolation is fail closed. Any filter bypass must be named and constrained by exact tenant predicates.
- Pre-v1 compatibility allows breaking changes, but contract changes still require generated clients, tests, and docs to move together.

## 4. Non-Negotiable Constraints

1. Every edited source file must keep the two-line `ABOUTME:` header.
2. Do not create a second generic registration-email pipeline.
3. Do not put access tokens in browser storage or generated client state.
4. Do not gate UI write actions through role IDs when the API resource supplies HAL links.
5. Do not bypass tenant query filters without a documented reason and exact tenant predicate.
6. Do not run solution-level `dotnet test`; use project-level test commands.
7. Do not mark launch work complete without runtime evidence for registration, email dispatch, and public crawlable endpoints.
8. Do not make self-hosting require SaaS-only infrastructure.

## 5. Major Decisions

### Decision 1: Use Basic Email Dispatch Mode for launch

Registration and lifecycle email must use `EmailDispatchOutbox`. The launch plan should harden this path with compliance, metrics, tests, and runbooks.

Rejected direction: adding a new generic registration email handler on `OutboxMessage`.

### Decision 2: Runtime proof precedes more feature work

Several old backlog items are source-complete but not proven as a deployable system. The next implementation effort should start with Aspire/Compose runtime proof, health checks, Mailpit email evidence, sitemap/robots/calendar smoke, and Data Protection restart checks.

### Decision 3: Email compliance is launch critical

The unsubscribe controller and token service are not enough. Launch email must include:

- Visible unsubscribe URL in appropriate non-transactional lifecycle emails.
- `List-Unsubscribe` and `List-Unsubscribe-Post` headers where applicable.
- Dispatch-time preference checks so stale queued work does not ignore opt-out.
- Tests proving opted-out users are skipped with an explicit non-send terminal state.

Current implementation note: mapped lifecycle categories, including `RegistrationConfirmation`, are preference-controlled at dispatch time. If the product owner wants true transactional registration confirmations to bypass opt-out while still offering unsubscribe for other lifecycle categories, that is a follow-up design change to `EmailDispatchDrainService.ResolvePreferenceCategory`.

### Decision 4: Registration integrity source work is complete, but tests must remain schema-aware

The correct next work is runtime release proof, not schema invention. Phase 3 persistence tests did expose a concrete schema/model gap: EF Core collapsed repeated unnamed same-column `HasIndex` calls, so the event-scope partial unique index was configured but absent from migrations. The fix is a focused migration plus named EF model indexes. Blazor client work confirmed the create response shape remains `BaseCommandResponseOfGuid`, so no NSwag regeneration was required for the current registration outcome changes. Future registration integrity work should keep tests close to real PostgreSQL schema behavior, generated-client service behavior, and HAL-gated UI behavior.

### Decision 5: Public launch polish should be small and verifiable

Add JSON-LD, manifest/icons, accessibility fixes, and SEO snapshots only where the current source lacks coverage. `docs/SEO.md` already documents sitemap, robots, public render policy, and tenant public-experience controls; do not turn this workstream into a broad SEO platform rewrite.

## 6. Implementation Phases

### Phase 0: Plan Review and Evidence Lock

Objective: make the plan trustworthy before code changes.

Actions:

- Review this plan, context, and tasks with the product/engineering owner.
- Confirm current dirty worktree ownership before implementation starts.
- Re-run source evidence checks for the exact branch head that will be implemented.
- Re-classify each implementation slice against `.claude/contract/intents.yaml`.
- Load matching `.claude/rules/*.md` and `.agents/skills/*/SKILL.md` before each slice.

Exit criteria:

- Owner agrees this is the MVP closure scope.
- Any deferred items are explicitly recorded.
- A fresh baseline build result is captured, or unrelated failures are documented.

### Phase 1: Runtime Launch Proof

Objective: prove the existing application can run in a self-hostable launch shape.

Actions:

- Start the distributed app through the documented `local-full` Aspire path first, because it is the contributor default and supplies local Keycloak, Cerbos, Mailpit, storage, and observability without Infisical credentials.
- For worktrees or concurrent runs, use `--isolated`, then discover ports through Aspire/Docker resource endpoints instead of hardcoding Mailpit or dashboard ports.
- If `aspire start` times out but child logs show the app briefly started, do not mark runtime proof complete. Capture the parent log, child log, `aspire ps --format Json`, Docker resource status, process URLs, and API/Blazor resource logs or foreground AppHost output.
- Align the Aspire CLI/AppHost toolchain before chasing application code. The repo pins Aspire `13.4.6`, but the observed CLI was `13.3.0-preview.1.26221.24`; update the CLI or run a foreground AppHost control so the backchannel timeout is removed from the diagnosis.
- After CLI alignment or a foreground control run, diagnose only the symptom that still reproduces. If `explore-api` again changes `Running -> Finished`, capture API resource output directly; if API stays up but `aspire start` still times out, treat the AppHost/CLI backchannel as the blocker.
- Confirm PostgreSQL migrations include app data, Data Protection keys, TickerQ store, and email dispatch tables.
- Confirm Data Protection keys survive app restart and cookie/session continuity is not broken.
- Confirm `/health/live`, readiness, and email-dispatch health behavior under healthy and degraded dependencies.
- Smoke public `GET` endpoints: event detail, calendar `.ics`, `sitemap.xml`, `robots.txt`, branded error pages, and relevant static assets.
- Confirm Redis optional/fallback behavior if Redis is disabled for self-hosted MVP.

Exit criteria:

- Runtime proof is reproducible from docs.
- `aspire ps` lists a stable AppHost, and API/Blazor remain running long enough to smoke health and public URLs.
- Any AppHost backchannel, CLI drift, or API early-exit cause is fixed or explicitly documented with a safe operator recovery path.
- Failure modes are documented with actionable operator messages.
- No launch-critical component depends on an undocumented SaaS service.

### Phase 2: Registration Email Compliance and Dispatch Hardening

Objective: make registration and lifecycle email safe enough for launch.

Actions:

- Audit `EmailDispatchDrainService` message construction and `EventLifecycleEmailOutboxFactory` email bodies.
- Add unsubscribe URLs and RFC-friendly unsubscribe headers for mapped lifecycle categories when public base URL configuration is valid.
- Enforce `UserNotificationPreference` at dispatch time before sending mapped lifecycle messages.
- Keep the category-level registration-confirmation opt-out choice explicit in docs and tests.
- Record skipped sends with a clear status, attempt/receipt semantics, metrics, and operator visibility.
- Verify tenant pause, park/replay, retry, dead-letter, stale processing recovery, and receipt idempotency.
- Prove the registration confirmation reaches Mailpit through the real drain path.

Exit criteria:

- A registration creates exactly one durable dispatch row and, when preferences allow, one delivered email.
- Opt-out behavior is deterministic and visible.
- Operators can diagnose failed, parked, skipped, replayed, and dead-lettered dispatches.

Progress as of 2026-07-04:

- Implemented dispatch-time preference skip in `EmailDispatchDrainService`.
- Added terminal `Skipped` statuses for outbox, attempt, receipt, drain result, API/HAL command behavior, RabbitMQ DLQ replay safety, and EF repository transitions.
- Added unsubscribe headers/body footer generation using `IEmailUnsubscribeTokenService` and configured public base URL.
- Added unit and PostgreSQL integration tests for headers, preference skip, terminal operator behavior, and skipped repository settlement.
- Runtime Mailpit proof remains open.

### Phase 3: Registration Integrity and Concurrency Evidence

Objective: prove users cannot overbook, duplicate-register, or receive misleading registration state.

Actions:

- Add focused persistence/integration tests for concurrent registration attempts against a low-capacity session.
- Add API tests for duplicate registration responses and unique-index violation mapping.
- Test event-scope, day-scope, and session-selection-scope registrations.
- Test mixed approved/waitlisted child-session outcomes and returned copy.
- Verify capacity counters remain correct after rollback, duplicate attempt, and waitlist path.
- Verify generated clients and Blazor services handle the final response shape.
- Centralize Blazor registration outcome classification so modal, list, and preview-workspace flows cannot drift.
- Verify event detail registration affordances are rendered from HAL `register` links only.

Exit criteria:

- Capacity never exceeds configured session maximum.
- Duplicate attempts are idempotent or return a stable user-facing conflict response.
- UI copy accurately reflects approved versus waitlisted sessions.
- Blazor services/components display safe bounded errors and never raw generated-client, provider, or database exception details.

Progress as of 2026-07-04:

- Added PostgreSQL-backed tests for capacity-one concurrent session-selection registrations, duplicate session-selection parent-intent handling, and rollback after failed child-row persistence.
- Kept the existing serializable transaction and conditional SQL capacity update as the core design, aligned with EF Core execution-strategy transaction guidance.
- Hardened the serializable transaction wrapper so already-completed Npgsql rollback cleanup does not mask the original retryable transaction failure.
- Added a `WasExisting` repository result marker so duplicate parent-intent unique-index races can return the existing registration id without creating duplicate child rows, duplicate capacity increments, or follow-on consent side effects.
- Added event-scope and day-scope duplicate tests against PostgreSQL using an alternate session to prove attempted duplicate capacity increments roll back.
- Fixed the missing event-scope partial unique index with distinct EF model index names and migration `AddEventRegistrationEventScopeUniqueIndex`; the migration fails clearly if existing duplicate active event-scope intents must be cleaned before applying it.
- Added API boundary coverage proving the create endpoint preserves an idempotent existing-id response.
- Added PostgreSQL persistence proof that a duplicate retry carrying a second registration-confirmation outbox object returns the existing intent and leaves exactly one `EmailDispatchOutbox` row tied to the original intent.
- Added `EventRegistrationRealRuntimeTests` using `RealRuntimeApiFixture` to POST the same session-selection registration twice through the API against PostgreSQL and prove the same intent id is returned with one parent intent, one child registration, one registration-confirmation outbox row, and one capacity reservation.
- Expanded `EventRegistrationRealRuntimeTests` to cover a full session waitlist response and unauthenticated POST rejection. The waitlist test proves child/intent `Waitlisted` status, unchanged capacity, and one registration-confirmation dispatch row; the auth test proves no intent, child registration, or dispatch row is created without authentication.
- Confirmed the generated create-registration response contract remains `BaseCommandResponseOfGuid`, so no OpenAPI/NSwag regeneration was required for the current Blazor outcome changes.
- Updated `EventRegistrationService` to convert generated-client `ApiException` statuses into safe `FailureCode`, `Message`, and `Errors` values instead of surfacing raw response bodies or exception messages.
- Added shared Blazor outcome classification through `EventListRegistrationWorkflow.ResolveOutcome` and aligned `EventRegistration`, `EventList`, and `EventPreviewWorkspace` on confirmed, waitlisted, already-registered, and failed states.
- Updated `EventPreviewWorkspace` to use the same policy-aware `BuildRegistrationRequest` path as `EventList`, so whole-event registration remains available when the registration policy requires it.
- Proved event detail registration action visibility is gated by the event detail HAL `register` link.
- Added/updated Blazor client tests covering safe service errors, idempotent already-registered UI state, shared outcome classification, full `EventList` behavior, and HAL-gated sidebar registration affordances.
- Phase 3 source/test/docs work is complete. A create-specific `403` remains intentionally out of scope until a future resource authorization rule makes `POST /api/eventregistration` capable of returning `403`; ownership-denial behavior is covered on registration reads.

### Phase 4: Security, Audit, and HAL Cleanup

Objective: remove launch-class authorization and governance risks.

Actions:

- Audit setup metadata, setup-secret stripping, rate limiting, and 429 response shape.
- Confirm BFF token forwarding keeps tokens server-side and antiforgery applies to proxied writes.
- Audit launch-critical write endpoints for audit log coverage.
- Ensure audit/event admin views are permission-gated and tenant-scoped.
- Review `RoleHelper` usage. Replace any local role-based action affordance gating with HAL link checks; keep role helpers only for labels, colors, and non-action display when appropriate.
- Verify CSP/security headers do not break event images, maps, fonts, calendar downloads, or error pages.

Exit criteria:

- Launch-critical actions are controlled by API authorization and represented to UI through HAL links.
- No client-only role decision is required to hide or show a write action.
- Security header and setup flows have regression evidence.

### Phase 5: Public SEO, Accessibility, Manifest, and UX Polish

Objective: make public event pages crawlable, shareable, and usable.

Actions:

- Add or verify event JSON-LD if still absent.
- Verify canonical, Open Graph, Twitter, noindex, and status handling across public event states.
- Add a minimal web app manifest and icon set if installability is in launch scope.
- Run accessibility checks for public event detail, registration, auth boundary, admin-critical forms, and error pages.
- Fix heading order, landmarks, focus restore, aria labels, keyboard flows, contrast, and reduced-motion issues found by tests or visual QA.
- Remove launch-visible placeholder/TODO text from public surfaces.

Exit criteria:

- Public event pages are shareable with correct metadata.
- Registration and event detail flows meet WCAG 2.2 AA expectations for launch-critical paths.
- Manifest scope is explicit: minimal install metadata only, unless the owner separately approves offline/PWA work.

### Phase 6: Contract, E2E, Docs, and Release Evidence

Objective: leave a verifiable release trail.

Actions:

- Regenerate OpenAPI/NSwag clients after any API contract changes.
- Update snapshot tests for HAL and response shapes.
- Run project-level unit, integration, architecture, Blazor, and selected E2E tests according to `docs/TESTING.md`.
- Capture traces/screenshots/log snippets for registration, email delivery, calendar, sitemap, robots, error pages, and health degradation.
- Update `docs/API.md`, `docs/BLAZOR.md`, `docs/OPERATIONS.md`, `docs/SECURITY-MODEL.md`, and release notes only where behavior changed.
- Log durable non-obvious findings in `dev/_journal/journal.md`.

Exit criteria:

- Build is green or unrelated failures are documented with evidence.
- Required project-level tests are green.
- Release owner can replay the smoke path from docs without agent memory.

## 7. Verification Strategy

Minimum verification for this workstream after implementation:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

Add targeted E2E only when runtime dependencies are available:

```bash
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
```

Do not run solution-level `dotnet test`.

## 8. Documentation and Operations Impact

Docs likely touched by implementation:

- `docs/API.md`: endpoint behavior, rate limiting, response contracts, OpenAPI notes.
- `docs/BLAZOR.md`: BFF/client behavior, generated clients, HAL affordance guidance.
- `docs/OPERATIONS.md`: email dispatch runbook, health checks, TickerQ, Mailpit/local proof, degraded dependency behavior.
- `docs/SECURITY-MODEL.md`: setup-secret boundary, email preference/unsubscribe posture, audit/privacy behavior.
- `docs/TESTING.md`: only if new required verification patterns are introduced.
- `dev/_journal/journal.md`: durable findings from implementation and verification.

Environment/config likely affected:

- SMTP/Mailpit settings for local proof.
- Public base URL/canonical host for sitemap, robots, calendar, unsubscribe links, and metadata.
- TickerQ storage and scheduling settings.
- Data Protection key storage settings.
- Optional Redis/cache settings.
- CSP/static asset configuration.

## 9. Risk Register

| Risk | Severity | Mitigation |
|---|---:|---|
| Email sends without unsubscribe/preference enforcement for non-transactional messages. | Medium | Dispatch integration, tests, metrics, and docs are now implemented for mapped lifecycle categories; runtime Mailpit proof and owner confirmation of registration-confirmation opt-out semantics remain. |
| Runtime dependencies are source-complete but not reproducible by operators. | High | Phase 1 now starts with the observed 2026-07-04 full-local Aspire failures: align the `13.3.0-preview` CLI with the repo's `13.4.6` AppHost SDK or run a foreground AppHost control, repeat startup, then debug `explore-api` resource output only if an API exit still reproduces. Health/public/Mailpit smoke must run while API/Blazor are still alive. |
| Duplicate/concurrent registrations produce confusing UX or database exceptions. | Low | Persistence tests now cover capacity, event/day/session duplicate parent-intent races, duplicate email-dispatch-row prevention, and rollback behavior; API tests now cover command-boundary existing-id behavior plus real PostgreSQL repeat-submit, waitlist, and unauthenticated-create behavior; Blazor client tests cover generated-client service agreement, safe error mapping, already-registered state, waitlist copy, and HAL-gated registration affordances. Remaining risk is runtime/visual release evidence. |
| UI exposes write actions using local role assumptions. | Medium | Phase 4 audits `RoleHelper` action gates and replaces with HAL checks. |
| Public pages share poorly or are inaccessible. | Medium | Phase 5 keeps existing sitemap/robots/render-policy behavior, then fills only proven gaps: JSON-LD if owner wants structured data, manifest/icons if installability is launch scope, and accessibility/visual QA. |
| Dirty worktree hides unrelated build failures. | Medium | Phase 0 captures baseline and separates unrelated failures from launch work. |
| Plan drifts again. | Medium | Context/tasks files now record current evidence, next actions, and explicit deferred scope. |

## 10. Deferred Unless Owner Re-Scopes

These are not launch blockers by default:

- Full offline PWA/service worker behavior.
- Public API-key rollout beyond already required health/security checks.
- Calendar feeds beyond current event `.ics` export.
- RSS/Atom feeds.
- Major navigation redesign or sidebar docking if not needed for critical launch flows.
- Generic outbox re-architecture.
- Advanced draft autosave/undo flows for all admin forms.
- Enterprise SSO, external secret managers, or multi-node production orchestration beyond self-hostable MVP proof.

## 11. Implementation Agent Contract

Before any code edit under this workstream:

1. Re-open `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/OPERATIONS.md`, the relevant `.claude/rules/*.md`, and relevant skills.
2. State the concrete intent from `.claude/contract/intents.yaml`.
3. Confirm files are in `paths_in_scope`.
4. Confirm minimum tests before editing.
5. Preserve user/unrelated worktree changes.
6. Implement in small vertical slices.
7. Update this workstream context and tasks as evidence changes.

## 12. Definition of Done

The MVP launch workstream is complete when:

- Runtime proof exists for registration, email dispatch, health, public event detail, calendar, sitemap, robots, and branded errors.
- Registration capacity, duplicate behavior, waitlist behavior, and confirmation email are verified through project-level tests and runtime smoke.
- Email compliance choices are explicit, implemented, tested, and documented.
- HAL affordance gating is respected for launch-critical UI actions.
- Accessibility and SEO launch checks are green or have explicit owner-approved exceptions.
- OpenAPI/generated clients/snapshots/docs are synchronized with behavior.
- Build and required project-level tests are green, or unrelated pre-existing failures are documented with exact evidence.
