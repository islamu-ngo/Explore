<!-- ABOUTME: Source-grounded launch-closure implementation plan for the MVP release. -->
<!-- ABOUTME: Replaces the stale work-package backlog with current architecture, risks, phases, and verification. -->

# MVP Launch Implementation Plan

Last Updated: 2026-07-04 Europe/Brussels

Status: In implementation. Re-baselined plan is current; Phase 2 email dispatch compliance and reliability controls now have source/test/docs/runtime closure for the registration-confirmation path, including Mailpit delivery, headers, body, and unsubscribe side-effect proof; Phase 3 registration integrity now has source/test/docs closure for capacity, event/day/session duplicate parent-intent handling, duplicate email-dispatch-row prevention, API boundary idempotent-create behavior, real PostgreSQL API repeat-submit coverage, waitlist behavior, unauthenticated create rejection, generated-client response agreement, Blazor service safe error mapping, registration UI outcome state/copy, and HAL-gated event-detail registration affordances. Phase 1 foreground FullLocal runtime proof is now green for distributed-app startup, health, bounded SMTP outage readiness, and public endpoint smoke: Aspire CLI is aligned to `13.4.6`, foreground `aspire run --isolated` keeps API and Blazor Healthy, dynamic Mailpit/Keycloak/Cerbos/MinIO/Svix/Coop endpoints are wired from Aspire resource expressions, the MinIO `explore` bucket is bootstrapped, persisted SMTP settings refresh to each isolated Mailpit port, deterministic launch seed rows are repaired across persistent Development volumes, and public event list/detail/calendar/sitemap/robots/static/error smoke is green. Detached `aspire start`/`aspire run --detach` remains a local Aspire CLI lifecycle limitation in this workspace: official Aspire docs say detached AppHosts should remain inspectable through `aspire ps`, but repeated `13.4.6` isolated runs returned startup JSON, then exited and left `aspire ps --format Json` empty after startup readiness. Treat foreground `aspire run --isolated` as the canonical launch proof path until the CLI behavior changes. Focused tests now prove ASP.NET Core cookie tickets survive a fresh BFF host when the shared Data Protection key ring is persisted through `DataProtectionKeyContext`, Blazor readiness surfaces Data Protection key-store failures through the safe `data-protection-keys` check, and `email-dispatch` readiness degrades on due retry backlog, stale `Processing` rows, and `DeadLettered` rows with PostgreSQL-backed count coverage. Remaining Phase 1 work is full browser/OIDC restart proof.

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

- Data Protection key persistence exists through `DataProtectionKeyContext`, service registration, migration-service migration, persistence integration tests, and a BFF cookie-middleware restart proof that starts a fresh host against the same persisted key context.
- Registration now uses `CreateEventRegistrationCommandHandler` plus `EventRegistrationIntentRepository.CreateWithChildrenAndCapacityAsync` to create the parent intent, child session rows, capacity reservations, and an `EmailDispatchOutbox` row in one serializable transaction.
- Registration duplicate protection is backed by partial unique indexes on active registration intents, plus a child-level unique session/user registration index. Phase 3 tests found the event-scope index was configured but missing from migrations because same-column EF indexes were unnamed; this has been corrected with distinct EF model index names and a focused migration. Duplicate retry attempts are now also proven not to persist a second registration-confirmation dispatch row.
- Registration client/UI handling keeps the generated `BaseCommandResponseOfGuid` contract stable: `EventRegistrationService` maps generated-client failures to bounded safe messages, `EventListRegistrationWorkflow.ResolveOutcome` centralizes confirmed/waitlisted/already-registered/failed states, and event detail registration affordances are proven to render only from the HAL `register` link.
- Basic Email Dispatch Mode exists: `EmailDispatchOutbox`, attempts, receipts, tenant pause, park/replay, stale processing recovery, TickerQ trigger, hosted drain service, health check, metrics, and operations documentation.
- Calendar export, sitemap, robots, render-policy classification, canonical URL helpers, and Open Graph/Twitter metadata have source-complete pieces. `docs/SEO.md` explicitly scopes this as public-discovery primitives, not site-wide SEO automation.
- HAL-driven UI helpers exist and are used heavily on event screens, but some role-helper paths still need launch audit.
- Email unsubscribe token/controller/preference storage exists, and the email dispatch drain now integrates `List-Unsubscribe`, visible unsubscribe links, and dispatch-time preference checks for mapped lifecycle categories when a valid public base URL is configured. Local SMTP/Mailpit readiness, unavailable-Mailpit failure behavior, and registration-driven message delivery/header/body/unsubscribe behavior are now proven through focused runtime checks.
- Runtime startup is no longer the current application blocker. On 2026-07-04, the initial full-local failures were traced to Aspire CLI drift plus AppHost hardcoded endpoint assumptions. After aligning the CLI to `13.4.6`, replacing full-local dependency URLs with Aspire endpoint expressions, bootstrapping the MinIO bucket, refreshing Development SMTP seed rows for `ISLAMU_ASPIRE_MODE=FullLocal`, repairing deterministic launch seed session/schedule projections, and fixing the new control-plane host `InteractiveServer` Razor import, foreground `aspire run --isolated` kept API/Blazor healthy and public event/list/calendar/sitemap/robots/static/error smoke passed. Detached `aspire start --format Json --isolated` and `aspire run --detach --format Json --isolated` are now recorded as a CLI/tooling caveat, not an application readiness gate: they return startup JSON after AppHost readiness, then the AppHost process disappears and `aspire ps --format Json` returns `[]`.

The old plan's largest architectural error is that it still points toward a generic `OutboxMessage` email handler flow. The current canonical path for registration and lifecycle email is the specialized `EmailDispatchOutbox` pipeline. Do not build a parallel generic email-dispatch implementation for launch.

Launch should focus on six closure phases:

1. Runtime proof and deployment readiness.
2. Registration email compliance and dispatch hardening.
3. Registration integrity and concurrency evidence.
4. Security, audit, and HAL affordance cleanup.
5. Public SEO, manifest, accessibility, and UX polish.
6. Contract, generated-client, runtime QA, docs, and release evidence.

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
| Unsubscribe foundation exists and is now wired into dispatch. | `EmailUnsubscribeController`, `EmailUnsubscribeTokenService`, `UserNotificationPreferenceRepository`, `EmailDispatchDrainService`, skipped dispatch status/tests, unsubscribe tests, and `RegistrationFlowTests` Mailpit header/body proof. | Runtime proof is green for registration confirmation. Remaining launch work is owner confirmation that category-level opt-outs should apply to registration confirmations. |

### 2.3 Public Launch Surface

| Area | Current evidence | Launch gap |
|---|---|---|
| Calendar export | `EventController.GetEventCalendar`, `IcalNetEventCalendarFileBuilder`, calendar integration/unit tests, and FullLocal runtime smoke returning HTTP 200 `text/calendar` for seeded event `018e4e5c-7f00-7000-8000-000000000061`. | Broader cache/generated-client checks remain only if later API contract work changes the surface. |
| Sitemap | `SitemapController` under `sitemap.xml`; `docs/SEO.md` documents static public routes, published public event URLs, forwarded host/proto handling, and clamped event projection; FullLocal smoke returned XML with deterministic event URLs. | Edge crawlability/status-state tests remain launch polish, not current runtime blocker. |
| Robots | Blazor `RobotsController`; `docs/SEO.md` documents production allow plus non-production `Disallow: /`; FullLocal smoke returned development `Disallow: /`. | Production-host verification remains release evidence. |
| Canonical and social metadata | `CanonicalUrlHelper`, `CanonicalMetadataTests`, `EventDetail.razor` metadata, `docs/SEO.md` public-discovery scope, and FullLocal event-detail smoke proving canonical/Open Graph/Twitter title metadata on the seeded public event. | Verify remaining public event states; JSON-LD automation was still absent by source search on 2026-07-04, so add it only if launch owner wants structured-data coverage. |
| PWA/manifest | No web manifest was found by source search on 2026-07-04; only `favicon.ico` and `Icon_landingpage.png` were found as candidate assets. | Decide if installability is launch scope. If yes, add manifest and icons; service worker/offline remains deferred. |

### 2.4 Runtime Evidence Gap

| Claim | Evidence | Launch interpretation |
|---|---|---|
| Full-local infrastructure can be created by Aspire. | 2026-07-04 foreground `aspire run --apphost Explore.AppHost/Explore.AppHost.csproj --isolated` proved the topology can start with PostgreSQL, Redis, RabbitMQ, Mailpit, CockroachDB, Keycloak, Cerbos, MinIO, Svix, Coop, Osprey, Prometheus, Grafana, migration service, API, Blazor, and control-plane host ready/running or completed as expected. Detached `aspire start`/`aspire run --detach` reached readiness but did not stay registered in this local CLI session. | AppHost topology is viable for MVP local-full proof. Remaining runtime work should target full browser/OIDC restart continuity, not a topology rewrite or detached CLI workaround. |
| Full-local application runtime is now proven for health. | Foreground `aspire run --isolated` reached stable API/Blazor runtime. API `/alive` and `/health` returned HTTP 200 Healthy; Blazor `/alive` and `/health` returned HTTP 200 Healthy. Detached lifecycle is documented as a local CLI limitation: startup JSON and logs show readiness, but the process disappears before `aspire ps` can inspect it. | Phase 1 can move from "can it start" to full browser/OIDC restart continuity and remaining non-SMTP degraded-health evidence. Use foreground orchestration for repeatable launch proof. |
| Data Protection key persistence supports cookie-ticket restart continuity. | `Explore.Blazor.IntegrationTests/Endpoints/BffDataProtectionCookieRestartTests.cs` starts one TestServer host, signs in with real ASP.NET Core cookie middleware, captures the protected cookie, starts a second fresh host with the same `DataProtectionKeyContext` key store, and proves the second host authenticates the original ticket. `DataProtectionKeyStoreHealthCheckTests` proves Blazor readiness reports the persisted key table as healthy when reachable and unhealthy with bounded failure metadata when the key store is missing. | The middleware-level risk and BFF key-store failure signal are covered. Full AppHost/browser/OIDC login-then-restart remains release evidence. |
| Dynamic endpoint wiring is now the AppHost contract. | FullLocal AppHost now supplies Keycloak, Cerbos, MinIO, Mailpit, Svix, and Coop settings from Aspire endpoint expressions instead of hardcoded localhost ports. Keycloak metadata returned issuer `http://localhost:<dynamic>/auth/realms/ISLAMU`, matching the isolated endpoint. | Do not reintroduce fixed host ports for isolated proof. Smoke checks must discover endpoints through Aspire resource metadata. |
| Mailpit is reachable and SMTP settings refresh per run. | Foreground run exposed Mailpit SMTP port `42037`; detached run exposed `45665`. In both cases the API environment and persisted `system_settings` rows matched the current dynamic port (`email.smtp_host="localhost"`, `email.smtp_port=<dynamic>`, `email.smtp_security="None"`), and API `smtp` health was Healthy. `RegistrationFlowTests` now proves a registration-confirmation row drains to one Mailpit message with expected recipient, subject, text, HTML, dispatch/correlation headers, `List-Unsubscribe`, `List-Unsubscribe-Post`, and an unsubscribe POST that disables the registration-confirmation preference. | Registration-driven Mailpit delivery is no longer a launch evidence gap. Remaining runtime work should focus on full browser/OIDC restart proof and browser/visual release proof. |
| SMTP outage readiness behavior is bounded and proven. | On 2026-07-04, foreground FullLocal `aspire run --isolated` exposed API `http://localhost:33675` and Mailpit SMTP port `45967`. Baseline API `/health` returned HTTP 200 with `X-Health-Status: Healthy`, `smtp` Healthy, and `time_total=0.233597`. After `aspire resource mailpit stop`, `aspire wait mailpit --status down` succeeded and API `/health` returned HTTP 503 with `X-Health-Status: Unhealthy`, `smtp` Unhealthy, `time_total=5.014349`, and `durationMs=5000.9584`. Restarting Mailpit restored API `/health` to HTTP 200 Healthy with `time_total=0.104138`. | The health contract correctly identifies configured-but-unreachable SMTP as non-deployable readiness and now fails fast enough for normal rolling-update readiness windows. The five-second timeout is enforced by the ASP.NET Core health-check registration and covered by an API integration test. |
| MinIO storage is bootstrapped by the AppHost. | The `minio-bootstrap` container exited with code 0 and logged `Bucket created successfully local/explore`; `mc stat local/explore` confirmed the bucket exists in `us-east-1`; API `storage` health was Healthy. | Storage readiness no longer depends on manual bucket creation for FullLocal. |
| Aspire CLI drift is resolved, but detached mode is not reliable here. | The old `13.3.0-preview.1.26221.24` CLI was backed up to `/tmp/aspire-13.3.0-preview.1.26221.24`; `aspire --version` now reports `13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248`; `aspire doctor --format Json` passes CLI/.NET/Docker and only warns about dev certificate trust. Official Aspire docs say `aspire start` should leave a background AppHost inspectable with `aspire ps`, but this workspace reproduced the opposite after startup readiness. | The previous AppHost backchannel timeout is no longer the active application blocker. For launch proof, use foreground `aspire run --isolated`; keep detached lifecycle as a CLI/tooling follow-up. |
| Public seed discovery now matches public filters. | FullLocal smoke exposed that deterministic seed sessions were draft and persisted seed events could have null `LastSessionEndUtc`. `SeedData` now creates published sessions and event end summaries, while `DatabaseSeeder` repairs existing Development catalog rows; a PostgreSQL seed regression recreates stale persisted rows and passes. | Public list/calendar smoke is now green and protected against stale FullLocal volumes. |
| Public smoke passed after seed repair. | `GET /api/event?pageSize=6` returned six seeded public events; event detail rendered with canonical/social metadata and security headers; event `.ics`, `sitemap.xml`, `robots.txt`, `/Error`, favicon, and landing icon all returned expected content/status. | Browser visual proof and SEO/status edge cases remain Phase 5/6 work. |

### 2.5 Security, BFF, and HAL

| Area | Current evidence | Launch gap |
|---|---|---|
| BFF token boundary | Project docs and BFF rules require tokens to remain server-side and antiforgery for proxied writes. Storage upload session/proxy endpoints now use `ValidateAntiforgery()` and same-process InteractiveServer calls use a short-lived Data Protection protected `X-ISLAMU-BFF-SELF-CALL` token bound to method/path/host/user. `Event.Web.BffHosting` reads `access_token` from the server-side auth ticket only for outbound API forwarding, strips browser credential headers before proxying, and `AuthStateSerializationPolicy` keeps browser auth state display-only. | Phase 4.1 BFF boundary checks are now covered by storage/setup/header/token regressions; remaining Phase 4 work moves to audit and HAL cleanup. |
| Embedded control-plane ownership | Public instance administration is hosted by `Explore.Blazor` and implemented in `Explore.Blazor.Client`, reusing `Event.Web.BffHosting` for the server-owned browser session and proxy boundary. Commercial management and hosting orchestration are owned by the separate Event-Control-Plane repository. | Keep public Event APIs/HAL self-contained and do not reintroduce private control-plane projects or dependencies into this repository. |
| HAL affordance gating | `HalResourceExtensions` and event pages use `_links` for many edit/delete/register actions. | Audit remaining `RoleHelper`/local-role paths and distinguish harmless display helpers from action affordance gates. |
| Setup secret boundary | `docs/SECURITY-MODEL.md` defines stripping and server-side setup secret rules. BFF setup-secret tests now prove browser-controlled `X-Setup-Secret` is ignored/removed, only BFF-owned resolver output is forwarded, and the real setup-secret limiter returns `429` ProblemDetails with `Retry-After` before upstream validation. | Setup-secret launch boundary is covered for Phase 4.1; keep future setup endpoints on the same server-owned resolver and limiter pattern. |
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

Several old backlog items are source-complete but not proven as a deployable system. Core Aspire runtime proof, health checks, Mailpit email evidence, sitemap/robots/calendar smoke, focused BFF Data Protection cookie continuity, Data Protection key-store failure visibility, and email backlog/dead-letter health behavior are now green. The next implementation effort should focus on full browser/OIDC restart checks and release/visual evidence.

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
- Keep the Aspire CLI aligned with the repo's AppHost SDK. The current proven CLI is `13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248`; the pre-update binary is temporarily backed up at `/tmp/aspire-13.3.0-preview.1.26221.24`.
- Use `aspire run --apphost Explore.AppHost/Explore.AppHost.csproj --isolated` as the canonical launch proof command. `aspire start`/`aspire run --detach` are official detached commands, but in this local `13.4.6` workspace they repeatedly returned startup JSON and then left no inspectable AppHost in `aspire ps`; use detached mode only for CLI lifecycle investigation until that tooling behavior changes.
- Discover runtime ports from `aspire describe <resource> --format Json`; do not assume fixed Mailpit, MinIO, Keycloak, API, or Blazor ports under `--isolated`.
- Confirm PostgreSQL migrations include app data, Data Protection keys, TickerQ store, and email dispatch tables.
- Confirm Data Protection keys survive fresh-host restart at the cookie middleware level, then separately prove full browser/OIDC session continuity through AppHost restart when the foreground runtime is available.
- Confirm `/health/live`, readiness, and email-dispatch health behavior under healthy and degraded dependencies. SMTP/Mailpit unavailable behavior, bounded SMTP timeout, and email-dispatch due backlog/stale-processing/dead-letter degraded health are proven.
- Smoke public `GET` endpoints: event detail, calendar `.ics`, `sitemap.xml`, `robots.txt`, branded error pages, and relevant static assets. Status: green for one FullLocal seeded-public path after seed repair.
- Confirm Redis optional/fallback behavior if Redis is disabled for self-hosted MVP.

Exit criteria:

- Runtime proof is reproducible from docs.
- Foreground `aspire run --isolated` keeps API/Blazor running long enough to smoke health and public URLs. Detached `aspire start --format Json --isolated` is documented as a local Aspire CLI limitation, not a launch blocker, unless a future CLI update re-proves stable detached inspection through `aspire ps`.
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
- Prove the registration confirmation reaches Mailpit through the real AppHost drain path.

Exit criteria:

- A registration creates exactly one durable dispatch row and, when preferences allow, one delivered email.
- Opt-out behavior is deterministic and visible.
- Operators can diagnose failed, parked, skipped, replayed, and dead-lettered dispatches.

Progress as of 2026-07-04:

- Implemented dispatch-time preference skip in `EmailDispatchDrainService`.
- Added terminal `Skipped` statuses for outbox, attempt, receipt, drain result, API/HAL command behavior, RabbitMQ DLQ replay safety, and EF repository transitions.
- Added unsubscribe headers/body footer generation using `IEmailUnsubscribeTokenService` and configured public base URL.
- Added unit and PostgreSQL integration tests for headers, preference skip, terminal operator behavior, skipped repository settlement, stale-processing recovery, retry/dead-letter behavior, and receipt idempotency.
- API SMTP health, configured-SMTP outage readiness, bounded SMTP probe behavior, dynamic Mailpit setting proof, and registration-driven Mailpit delivery proof are green. The runtime proof checks the durable dispatch row, Mailpit message, text/HTML body, custom headers, one-click unsubscribe headers, visible unsubscribe link, and persisted preference disablement after POST.

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
- Confirm BFF token forwarding keeps tokens server-side and antiforgery applies to proxied writes. Storage upload session/proxy are now covered by `ValidateAntiforgery()` plus a protected same-process self-call token; setup-secret stripping and 429 limiter behavior are covered; explicit browser-visible token-leak checks now cover auth-state serialization and `/bff/me`.
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

### Phase 6: Contract, Runtime QA, Docs, and Release Evidence

Objective: leave a verifiable release trail.

Actions:

- Regenerate OpenAPI/NSwag clients after any API contract changes.
- Update snapshot tests for HAL and response shapes.
- Run project-level unit, integration, architecture, and Blazor tests plus selected manual browser QA according to `docs/TESTING.md`.
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
| Email sends without unsubscribe/preference enforcement for non-transactional messages. | Low | Dispatch integration, tests, metrics, docs, and registration-driven Mailpit proof are now implemented for mapped lifecycle categories; owner confirmation of registration-confirmation opt-out semantics remains. |
| Runtime dependencies are source-complete but not fully launch-proven by operators. | Medium | Core foreground FullLocal startup, health, bounded SMTP outage readiness, registration-driven Mailpit delivery, public endpoint smoke, focused BFF Data Protection cookie restart proof, Data Protection key-store health/log visibility, and email-dispatch backlog/dead-letter health behavior are now green on Aspire CLI/AppHost `13.4.6` plus focused API/PostgreSQL/Blazor tests. Dynamic endpoints, MinIO bucket bootstrap, SMTP seed refresh, deterministic seed repair, and event list/detail/calendar/sitemap/robots/static/error smoke are verified. Detached local CLI lifecycle is documented separately. Remaining work is full browser/OIDC restart proof. |
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

- Runtime proof exists for health, bounded SMTP outage readiness, registration-driven Mailpit email delivery, public event detail, calendar, sitemap, robots, branded errors through foreground FullLocal Aspire, BFF cookie-middleware Data Protection restart continuity and key-store failure visibility through focused integration tests, and email-dispatch backlog/dead-letter health behavior through focused API/PostgreSQL tests; full browser/OIDC restart evidence remains required before closing the workstream.
- Registration capacity, duplicate behavior, waitlist behavior, and confirmation email are verified through project-level tests and runtime smoke.
- Email compliance choices are explicit, implemented, tested, and documented.
- HAL affordance gating is respected for launch-critical UI actions.
- Accessibility and SEO launch checks are green or have explicit owner-approved exceptions.
- OpenAPI/generated clients/snapshots/docs are synchronized with behavior.
- Build and required project-level tests are green, or unrelated pre-existing failures are documented with exact evidence.
