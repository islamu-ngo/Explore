<!-- ABOUTME: Current handoff context for the MVP launch implementation workstream. -->
<!-- ABOUTME: Records source evidence, decisions, risks, and next actions after the July 2026 rebaseline. -->

# MVP Launch Context

Last Updated: 2026-07-05 Europe/Brussels

## Current Status

The MVP launch docs have been rebaselined from an old May 2026 work-package backlog into a launch-closure program. Phase 2 email-dispatch compliance and reliability controls have source/test/docs/runtime closure for the registration-confirmation path, Phase 3 registration-integrity source/test/docs work is complete, and the Phase 1 core FullLocal runtime proof is now green for startup, health, SMTP outage readiness, public endpoint smoke, focused BFF Data Protection cookie restart continuity, Data Protection key-store failure visibility, and email-dispatch backlog/dead-letter degraded-health behavior.

This is the important shift:

- The repo already contains substantial launch infrastructure.
- The old docs understated completed work and over-prescribed obsolete implementations.
- The next implementation should verify, harden, and polish existing flows before adding new surface area.
- Registration email must use the existing `EmailDispatchOutbox` pipeline, not a new generic outbox handler path.
- Dispatch-time preference enforcement and unsubscribe affordances now live in the existing `EmailDispatchDrainService`; runtime Mailpit proof is green for registration-confirmation delivery, headers, text/HTML body, and unsubscribe side effects.
- Registration client/UI handling now keeps the generated `BaseCommandResponseOfGuid` contract stable, maps generated-client failures safely, shares one outcome classifier across modal/list/preview registration flows, and gates event-detail registration affordances from HAL.
- The next runtime action is no longer API/AppHost internals diagnosis. Aspire CLI is aligned to `13.4.6`, foreground `aspire run --isolated` is the trusted smoke path, API and Blazor health are green, SMTP/Mailpit outage readiness is proven and bounded to five seconds, registration-driven Mailpit delivery is proven, public event/list/calendar/sitemap/robots/static/error smoke is green, a focused BFF integration test proves a cookie protected by one host can be read by a fresh host when both use the persisted `DataProtectionKeyContext` key store, Blazor `data-protection-keys` readiness now reports key-store reachability/failure safely, and focused API/PostgreSQL tests prove `email-dispatch` readiness degrades on due retry backlog, stale `Processing`, and `DeadLettered` rows. Detached `aspire start --format Json --isolated` and `aspire run --detach --format Json --isolated` are documented as local Aspire CLI lifecycle limitations in this workspace: official Aspire docs say detached AppHosts should remain inspectable through `aspire ps`, but repeated runs returned startup JSON after readiness and then left `aspire ps --format Json` empty. Detached mode is no longer a Phase 1 application-readiness gate. Remaining runtime work is full browser/OIDC restart proof.

## Session Progress - 2026-07-04

Completed during rebaseline:

- Loaded the senior CTO feedback workflow and required project contract docs.
- Read project quick-reference, governance, operations, testing, architecture, API, security, authorization, Blazor, accessibility, design-system, and multi-tenancy docs.
- Read relevant path rules for API controllers, HATEOAS, application layer, persistence, Blazor client/server, and tests.
- Read relevant skills for Clean Architecture, CQRS/MediatR, EF Core, auth, Blazor BFF, Blazor UI, outbox, and error tracking.
- Audited the existing MVP launch plan/context/tasks.
- Verified current implementation reality around registration, email dispatch, unsubscribe, Data Protection, calendar, SEO, health, HAL helpers, and persistence constraints.
- Rewrote `mvp-launch-plan.md`, `mvp-launch-context.md`, and `mvp-launch-tasks.md` around current source evidence.
- Ran post-edit stale-reference, header/date, whitespace, Release build, and architecture-context checks.

Completed during Phase 2 implementation slice:

- Used Context7 official docs for current MailKit/MimeKit message/header behavior before changing email construction.
- Added terminal `Skipped` status to `EmailDispatchOutbox`, `EmailDispatchAttempt`, and `EmailDispatchReceipt`.
- Added `SkippedCount` and `EmailDispatchDrainOutcome.Skipped` to the drain service contract.
- Extended `EmailDispatchDrainService` to enforce mapped `UserNotificationPreference` before SMTP handoff, record opted-out rows as `Skipped` with `recipient_unsubscribed`, generate unsubscribe URLs through `IEmailUnsubscribeTokenService`, and add `List-Unsubscribe`, `List-Unsubscribe-Post`, and visible unsubscribe affordances when public base URL configuration is valid.
- Added repository methods for skipped outbox/receipt settlement and prevented skipped rows from being parked/replayed.
- Updated RabbitMQ DLQ replay decision to park already-skipped pointers.
- Updated API/HAL operator behavior so skipped rows do not receive replay or park affordances.
- Added focused Application, Infrastructure, API integration, and Persistence integration tests for skipped transitions and unsubscribe affordances.
- Re-verified the existing Phase 2.4 reliability controls: PostgreSQL transition tests cover stale-processing recovery to `Unknown` plus receipt updates and receipt/processing duplicate-claim idempotency; Infrastructure drain tests cover failing-provider retry scheduling and dead-lettering after retry budget exhaustion.
- Updated `docs/API.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, and `docs/SECURITY-MODEL.md` with current behavior.

Completed during Phase 3 implementation slice:

- Used Context7 EF Core documentation to confirm the explicit-transaction pattern: user-initiated transactions must run inside the execution strategy delegate when retries are enabled.
- Added `Event.Persistence.IntegrationTests/Repositories/EventRegistrationIntentRepositoryTests.cs` with PostgreSQL-backed registration capacity tests.
- Proved concurrent registration attempts against a capacity-one session produce one approved child registration, waitlist the rest, and keep `current_audience_attendees` at `1`.
- Hardened `EventRegistrationIntentRepository` rollback cleanup so an already-completed Npgsql transaction does not mask the original retryable serializable transaction failure.
- Proved capacity increments roll back when `SaveChangesAsync` fails after reservation updates, using the child-level unique session/user index as the failure trigger.
- Added a duplicate session-selection persistence test proving the parent intent unique index returns the existing intent without adding another child row or capacity increment.
- Added `WasExisting` to `EventRegistrationIntentCreationResult`, translated duplicate parent-intent `23505` violations in `EventRegistrationIntentRepository`, and taught `CreateEventRegistrationCommandHandler` to return the existing-id response without running contact-share consent side effects.
- Added duplicate event-scope and day-scope PostgreSQL persistence tests using a seeded `EventDay` plus an alternate session, proving parent-intent duplicates return the existing id and roll back attempted capacity increments.
- Found and fixed a real EF model/schema gap: repeated unnamed `HasIndex` calls over `(TenantId, EventId, UserId)` caused EF Core to keep only the later session-selection partial index in migrations/snapshot. The fix gives the event and session-selection indexes distinct EF model names and adds a corrective migration for `ix_event_registration_intents_unique_event_scope`.
- Added a migration preflight check that raises a clear exception if duplicate active event-scope intents already exist, avoiding silent data deletion while still making operator cleanup explicit.
- Added an API boundary test proving `POST /api/eventregistration` returns HTTP 200 with the existing id when the command layer reports `Event Registration already exists.`.
- Added PostgreSQL persistence coverage proving a duplicate registration retry with a second `EmailDispatchOutbox` object still leaves only one pending registration-confirmation dispatch row tied to the original intent.
- Added real database-backed API repeat-submit coverage in `EventRegistrationRealRuntimeTests`: the same authenticated session-selection POST through `RealRuntimeApiFixture` returns the same intent id and leaves one parent intent, one child registration, one registration-confirmation outbox row, and one capacity reservation in PostgreSQL.
- Expanded `EventRegistrationRealRuntimeTests` with PostgreSQL-backed API coverage for a full session waitlist response and unauthenticated create rejection. The waitlist path stores waitlisted intent/child status, leaves `current_audience_attendees` unchanged, and still creates one registration-confirmation outbox row; the unauthenticated path returns `401` and creates no registration state.
- Confirmed the generated create-registration response contract remains `BaseCommandResponseOfGuid`; no OpenAPI/NSwag regeneration was required for the current Blazor outcome work.
- Updated `Explore.Blazor.Client/Services/EventRegistrationService.cs` so generated-client `ApiException` failures map to bounded `FailureCode`, `Message`, and `Errors` values instead of raw exception or response text.
- Updated `Explore.Blazor.Client/Pages/Events/EventListRegistrationWorkflow.cs` with shared confirmed/waitlisted/already-registered/failed outcome classification.
- Aligned `EventRegistration`, `EventList`, and `EventPreviewWorkspace` on the shared outcome classifier.
- Updated `EventPreviewWorkspace` to use the same policy-aware registration request builder as `EventList`, preserving whole-event registration when policy requires it.
- Proved `EventDetailsSidebar` renders the registration action only when the event detail HAL resource includes the `register` link.
- Added/updated Blazor client tests for safe registration-service error mapping, already-registered state, shared outcome classification, full EventList behavior, and HAL-gated sidebar registration affordances.

Completed during Phase 1.3 email-dispatch health slice:

- Used Context7 official ASP.NET Core health-check docs and the existing `LocalWebhookDeliveryHealthCheck` pattern to keep the readiness check DI-safe and scoped-service aware.
- Added primitive count methods to `IEmailDispatchOutboxRepository` and `EmailDispatchOutboxRepository` for due dispatch backlog, retry-scheduled rows, stale `Processing` leases, and `DeadLettered` rows, using the existing named cross-tenant worker bypass.
- Updated `EmailDispatchHealthCheck` so enabled Basic Dispatch Mode queries those counts and returns `Degraded` when due backlog, stale processing, or dead-letter thresholds are reached.
- Added `EmailDispatchProcessor` health thresholds (`HealthDueDispatchWarningThreshold`, `HealthStaleProcessingWarningThreshold`, `HealthDeadLetterWarningThreshold`) with startup validation.
- Updated `docs/CONFIGURATION.md` and `docs/OPERATIONS.md` so self-hosters see the threshold defaults, safe health payload fields, and distinction between future retry backoff rows and due backlog.
- Added focused API health-check tests, infrastructure settings-validator tests, and a PostgreSQL-backed repository count test.
- Updated `docs/BLAZOR.md` with the registration service/outcome/HAL guidance.

Completed during Phase 1 FullLocal runtime proof slice:

- Backed up the old Aspire CLI at `/tmp/aspire-13.3.0-preview.1.26221.24` and installed the official `13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248` CLI, matching the repository AppHost SDK.
- Updated `Explore.AppHost/AppHost.cs` so FullLocal Keycloak, Cerbos, MinIO, Mailpit, Svix, and Coop settings come from Aspire endpoint expressions instead of fixed localhost ports.
- Added a `minio-bootstrap` `minio/mc:latest` container so the `explore` bucket is created before the API starts.
- Updated `Explore.Persistence/Seed/DatabaseSeeder.cs` so Development SMTP settings are refreshed when `ISLAMU_ASPIRE_MODE=FullLocal`, preventing persistent local database volumes from keeping stale isolated Mailpit ports.
- Verified targeted Debug AppHost build passed after the runtime wiring changes.
- Verified foreground `aspire run --apphost Explore.AppHost/Explore.AppHost.csproj --isolated` kept the distributed app alive, with `aspire wait` reporting `explore-api` and `explore-blazor` Healthy.
- Verified foreground API `/alive` and `/health` returned HTTP 200 Healthy, including Healthy checks for OIDC discovery, SMTP, RabbitMQ email dispatch topology, storage, webhook local delivery, Svix provider configuration, Cerbos, and database.
- Verified foreground Blazor `/alive` and `/health` returned HTTP 200 Healthy, including Healthy checks for cache, OIDC discovery, database, and API readiness.
- Verified Keycloak OIDC metadata issuer used the dynamic isolated localhost endpoint.
- Verified Mailpit SMTP dynamic port reached both API environment and persisted `system_settings` rows (`email.smtp_host`, `email.smtp_port`, `email.smtp_security`).
- Verified `minio-bootstrap` exited with code 0, logged bucket creation, and `mc stat local/explore` confirmed the bucket exists in `us-east-1`.
- Re-tested one earlier detached `aspire start --apphost Explore.AppHost/Explore.AppHost.csproj --format Json --isolated`; it returned JSON, stayed registered in `aspire ps`, and produced healthy API/Blazor resources.
- Verified detached API `/health` returned HTTP 200 Healthy with 18 checks, detached Blazor `/health` returned HTTP 200 Healthy with 7 checks, and persisted SMTP settings refreshed to the detached run's Mailpit port.
- Recorded the later detached lifecycle caveat: after additional code/test changes, another `aspire start --format Json --isolated` returned JSON but lost AppHost registration (`aspire ps --format Json` returned `[]`), with logs showing `Client disconnected from auxiliary backchannel`. Foreground `aspire run --isolated` remained healthy and was used for public smoke.
- Stopped the foreground/detached AppHosts after proof and cleaned orphaned DCP/API/Blazor child processes left after Aspire stop.
- Fixed a separate launch-seed discovery defect found during public smoke: deterministic Islamic event sessions were persisted as `Draft`, and existing persisted events could keep `LastSessionEndUtc = null`, so the public list and calendar filters treated source-visible seed events as non-current/non-exportable.
- Updated `SeedData.CreateSession` so deterministic launch sessions are `Published`, updated `SeedData.CreateEvent` so `LastSessionEndUtc` is populated, and updated `DatabaseSeeder.EnsureIslamicEventCatalogAsync` to repair those status/schedule-summary fields on existing Development seed rows across persistent FullLocal volumes.
- Added `DatabaseSeederTests.SeedAsync_InDevelopment_RepairsLaunchCatalogDiscoveryFieldsAcrossStartups` to recreate stale persisted seed rows and prove a later Development seed repairs all catalog sessions/events.
- Verified public runtime smoke through FullLocal isolated Aspire after the seed repair: public event list returned six events, event detail rendered with canonical/Open Graph/Twitter metadata and security headers, event calendar returned `text/calendar` with `BEGIN:VCALENDAR`, sitemap returned XML with deterministic event URLs, robots returned development `Disallow: /`, branded error/static assets rendered, and `aspire ps --format Json` returned `[]` after shutdown cleanup.
- Kept the embedded control-plane shell compile-safe when AppHost built the public Blazor host. Current UI ownership is `Explore.Blazor` and `Explore.Blazor.Client`, reusable authentication/proxy infrastructure remains in `Event.Web.BffHosting`, and commercial management/orchestration belongs to the separate Event-Control-Plane repository. The focused `Explore.Blazor` Release build passed with 0 errors.
- Re-ran detached Aspire after the control-plane fix. `aspire run --detach --apphost Explore.AppHost/Explore.AppHost.csproj --isolated --format Json` returned `appHostPid=3623272`, `cliPid=3618280`, and log `/home/amir/.aspire/logs/cli_20260704T164306504_detach-child_351672a4ed3846a3931c7d5f915ad84a.log`; logs reached resource readiness and `Notifying AppHost startup readiness`, but immediate `aspire ps --format Json` returned `[]`, `aspire describe` reported no AppHost, and both PIDs were gone.
- Hardened SMTP readiness by registering `SmtpHealthCheck` with `timeout: TimeSpan.FromSeconds(5)` in `Explore.API/Program.cs`.
- Added `SmtpHealthCheckRegistrationTests` to assert the real API host registration uses the five-second timeout, `Unhealthy` failure status, and readiness/SMTP/infrastructure tags.
- Proved bounded configured-SMTP outage readiness through foreground FullLocal isolated Aspire. Baseline API `http://localhost:33675/health` returned HTTP 200 Healthy with `smtp` Healthy and `Connection successful`; stopping Mailpit on SMTP port `45967` with `aspire resource mailpit stop` made `aspire wait mailpit --status down` succeed and changed API `/health` to HTTP 503 with `X-Health-Status: Unhealthy`, `smtp` Unhealthy, `time_total=5.014349`, and `durationMs=5000.9584`; restarting Mailpit restored API `/health` to HTTP 200 Healthy with `time_total=0.104138`.

Completed during Phase 2.5 registration email runtime proof slice:

- Confirmed the current Mailpit API shape against official Mailpit API/OpenAPI docs before extending the E2E fixture: `/api/v1/message/{ID}/headers` returns header arrays, and message details expose HTML/text content.
- Extended `MailpitContainerFixture` and `AppHostFixture` so E2E tests can fetch Mailpit HTML bodies and raw headers, not only summary metadata and plain text.
- Updated `RegistrationFlowTests` to prove the runtime registration-confirmation path through AppHost, the real API, `TickerQScheduledEmailDispatchTrigger`, `EmailDispatchDrainService`, SMTP, and Mailpit.
- The E2E now verifies exactly one sent `EmailDispatchOutbox` row, succeeded attempt count, completed receipt count, Mailpit subject/recipient, expected text body, HTML body, `X-Email-Dispatch-ID`, `X-Correlation-ID`, `List-Unsubscribe`, `List-Unsubscribe-Post: List-Unsubscribe=One-Click`, visible unsubscribe URL in text and HTML, and a tenant-aware unsubscribe POST that disables `NotificationPreferenceCategories.RegistrationConfirmations` for the dispatch user.
- AppHost E2E wiring now supplies `PublicBaseUrl` for the API resource so generated unsubscribe links are present during runtime proof. The test posts the generated token back through the reachable AppHost API endpoint while preserving the delivered link shape.

Completed during Phase 1.2 Data Protection restart proof slice:

- Used Context7 official ASP.NET Core Data Protection docs to confirm `PersistKeysToDbContext<TContext>()` is the framework-supported path for sharing the key ring across app instances/restarts and that cookie authentication depends on Data Protection.
- Added `Explore.Blazor.IntegrationTests/Endpoints/BffDataProtectionCookieRestartTests.cs`.
- The test starts one fresh TestServer BFF host, signs in through real ASP.NET Core cookie middleware, captures the auth cookie, disposes that host, starts a second fresh host with the same in-memory EF `DataProtectionKeyContext` root, and proves the second host authenticates the original protected ticket as `restart-user`.
- Verified `Event.MigrationService/Worker.cs` resolves `DataProtectionKeyContext` and runs `Database.MigrateAsync`, so the migration service covers the dedicated Data Protection key schema.
- Added an Operations note that preserving Data Protection key rows preserves BFF auth/setup/antiforgery cookie readability across restarts, while deleting or losing those rows intentionally invalidates existing protected cookies and requires re-authentication or setup retry.

Completed during Phase 1.2 Data Protection health visibility slice:

- Added `Explore.Blazor/HealthChecks/DataProtectionKeyStoreHealthCheck.cs` and registered it as Blazor readiness check `data-protection-keys`.
- The check opens the same scoped `DataProtectionKeyContext` used by ASP.NET Core `PersistKeysToDbContext<TContext>()`, counts key rows to prove the table is reachable, returns healthy with only safe `keyCount`/store metadata, and returns unhealthy with a bounded `failureType` plus a safe warning log when the key store cannot be reached.
- Added `Explore.Blazor.IntegrationTests/Endpoints/DataProtectionKeyStoreHealthCheckTests.cs` to prove reachable and missing key-store behavior without exposing key XML or connection-string data.
- Updated `docs/OPERATIONS.md` so operators treat `data-protection-keys` unhealthy as a BFF session-continuity/key-ring persistence incident before debugging Keycloak, browser storage, or cookie middleware.

Completed during Phase 4.1 BFF storage antiforgery slice:

- Used Context7 official ASP.NET Core antiforgery docs to confirm Minimal API endpoints need explicit `IAntiforgery.ValidateRequestAsync`-style validation for browser-accessible cookie-authenticated unsafe requests, and that disabling antiforgery is only appropriate for routes not vulnerable to browser cookie CSRF.
- Updated `Explore.Blazor/Extensions/BffStorageEndpoints.cs` so both `/bff/storage/upload-session` and `/bff/storage/upload-proxy` call the existing BFF `ValidateAntiforgery()` endpoint filter.
- Preserved the InteractiveServer self-call path by using `BffCookieForwardingHandler` and `BffSelfCallTokenService`: server-originated storage calls receive a short-lived Data Protection protected `X-ISLAMU-BFF-SELF-CALL` header bound to method, path, host, and authenticated user, while browser requests without CSRF or that protected token fail before upload-session logic runs.
- Added focused storage boundary tests proving authenticated storage POSTs without CSRF/self-call proof return `400 Antiforgery validation failed`, and that a valid same-process self-call token lets an upload-session request proceed without exposing browser tokens.
- Updated `docs/SECURITY-MODEL.md` and `docs/BLAZOR.md` so storage upload session/proxy are no longer documented as antiforgery exceptions.
- Used Context7 official ASP.NET Core rate-limiting docs to confirm endpoint-specific `RequireRateLimiting` policies need `UseRateLimiter` after routing.
- Added setup-secret BFF rate-limit coverage that runs the real `AddBffRateLimiting` policy with testing disablement turned off, proves a second setup-secret POST in the same partition returns `429 Too Many Requests` ProblemDetails with `Retry-After`, and proves the upstream setup validation API is not called for the rejected request.
- Reconfirmed existing setup-secret boundary coverage: browser-controlled `X-Setup-Secret` is ignored by the BFF setup endpoint, stripped by forwarding handlers/sanitizers, and replaced only from BFF-owned resolver output.

Completed during Phase 4.1 BFF token-boundary slice:

- Used Context7 official ASP.NET Core auth guidance to confirm the important split: `SaveTokens` stores OIDC tokens in the server-side authentication ticket, while `AddAuthenticationStateSerialization` controls the separate browser-visible Blazor auth-state projection.
- Confirmed the shared BFF proxy resolves `access_token` only from server-side authentication properties through `HttpContext.GetTokenAsync("access_token")`, validates it with `EventBffTokenSafety`, strips browser-controlled credential/token headers before proxying, and writes the bearer token only to outbound API requests.
- Confirmed `/auth/status` returns only `isAuthenticated` plus display name and does not echo browser `Authorization` headers.
- Added explicit token-shaped claim regressions to `AuthStateSerializationPolicyTests` so `access_token`, `refresh_token`, and `id_token` stay out of Blazor's browser-readable auth-state payload.
- Added `BffCurrentUserEndpointTokenBoundaryTests` to prove `/bff/me` filters token-shaped claims and values even when the authenticated principal contains them.
- Phase 4.1 BFF boundary checks are now closed for setup-secret stripping/rate limiting, storage upload antiforgery/self-call handling, browser credential-header stripping, and browser-visible access/refresh token leakage.

Completed during July 5 launch-closure slices:

- Phase 4.2 audit evidence is complete. `Explore.Persistence/ExploreDbContext.SaveChanges.cs` now records `UpdatedBy` whenever a current user exists for modified `IAuditableEntity` rows, even when handlers set `UpdatedAt` themselves; a PostgreSQL-backed `GenericRepositoryTests` regression proves actor evidence survives handler-supplied timestamps.
- Phase 4.3 HAL affordance cleanup is complete for the concrete create-event nav gap. `EventCreationEligibilityService` now uses the API `EventCreationContextDto.PublisherOptions.CanPublish` contract instead of local role checks; focused Blazor client tests prove personal, organization, group, and no-publisher routing behavior.
- Phase 4.4 security-header polish is complete. Public/shared BFF hosts emit CSP, frame/content/referrer protections, and `Permissions-Policy`; Blazor integration tests prove headers on app shell, error, static asset, manifest, robots, and no-Keycloak startup routes.
- Phase 5.1 SEO metadata is complete. Event detail pages emit canonical/OG/Twitter metadata, schema.org `Event` JSON-LD for crawlable public events, and `robots noindex,nofollow` for non-public or non-crawlable states; source tests and raw-HTML safety checks are green.
- Phase 5.2 sitemap/robots proof is complete. Persistence tests prove sitemap events are tenant-filtered and limited to published public events; Blazor runtime tests prove non-production robots disallows crawling and production robots uses forwarded proto/host for the canonical sitemap URL.
- Phase 5.3 manifest/install metadata is complete and white-label-safe. `/manifest.webmanifest` is served by `BffManifestEndpoints` from DB-backed public-experience branding with generic fallback; the static tenant-branded manifest file was deleted so it cannot shadow the dynamic endpoint.
- White-label remediation is complete for runtime/user-facing surfaces. Public shell/manifest/EventDetail metadata use DB-backed public-experience branding where available; legal/setup/error/AI/admin/email/operator copy now uses generic platform wording. Remaining `ISLAMU`/`islamu` strings are technical identifiers only: auth client IDs, Cerbos/resource kinds, metrics, hidden schema markers, and internal BFF protocol headers.
- Phase 5.4 automated accessibility and placeholder cleanup is complete, but desktop/mobile browser visual QA remains blocked. Source guards, `MainLayoutTests`, `ErrorPagesTests`, and shared component accessibility tests passed; fake tenant member rows and “coming soon” settings copy were removed. The Debug/AppHost OpenAPI build blocker was fixed by limiting build-time OpenAPI generation to Release while keeping the documented Release contract-generation path green, and Keycloak later recovered to Healthy; live Blazor page routes still cannot be honestly visually proven because the local API fails during migration on duplicate existing `events(tenant_id, public_code)` data.
- Phase 6.1 contract sync is complete. `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, and `Explore.Blazor.Client/Clients/EventApiClient.g.cs` were regenerated through project builds.
- Phase 6.2 feasible required verification is complete. Full Release build, Architecture, Domain, Application, Persistence integration, Blazor client, BFF no-Keycloak, and targeted API OpenAPI contract tests passed. Full API integration remains blocked by gateway-timeout/status failures tied to the local API startup/runtime state after the deterministic HAL schema gap was fixed.

Current blocker:

- Local Aspire runtime visual QA is blocked by `explore-api` failing while applying `AddEventPublicCode` with PostgreSQL `23505` on duplicate `(tenant_id, public_code)` rows before `ix_events_tenant_public_code` can be created. The owner corrected that migrations must not be manually edited; the manual edit was reverted, `dotnet ef migrations add eventmoderation --context ExploreDbContext --project Explore.Persistence --startup-project Explore.API` produced an empty migration, and it was removed via `dotnet ef migrations remove`. A later migration cannot repair data needed by this earlier migration; recovery needs owner-approved database cleanup/reset or a proper generated workflow that can run before the failing constraint. Latest runtime check after the Debug/OpenAPI fix: Aspire starts, Keycloak is Healthy, migration service exits 0, and `explore-api` still exits with the same `23505` unique-index creation failure.

In progress:

- Remaining MVP launch closure is blocked on owner action for local runtime data. Phase 4 security/audit/HAL cleanup, Phase 5 source/test public polish, Phase 6 contract sync, required feasible test evidence, docs, and journal updates are complete; runtime/browser/OIDC proof remains open until the duplicate `public_code` data is cleaned/reset with approval or explicitly accepted as a local-data blocker.

Known context note:

- `AGENTS.md` imports `@RTK.md`, but `RTK.md` was not present at the repository root during this rebaseline. This was not treated as an MVP launch blocker, but context owners may want to resolve it separately if architecture/context tests require it.

## Quick Resume

If resuming this workstream:

1. Open `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, and `docs/OPERATIONS.md`.
2. Re-open the relevant rules and skills for the implementation slice.
3. Read these three files:
   - `dev/active/mvp-launch/mvp-launch-plan.md`
   - `dev/active/mvp-launch/mvp-launch-context.md`
   - `dev/active/mvp-launch/mvp-launch-tasks.md`
4. Confirm the worktree status and identify unrelated changes before editing.
5. Start with Phase 0 in the tasks file unless the owner explicitly selects a later phase.

## Current Source Evidence

### Registration and Capacity

| File | Evidence |
|---|---|
| `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs` | Creates registration intent, child session rows, capacity-aware result, and an email dispatch outbox row through Application-layer orchestration. Validator is manually instantiated. |
| `Explore.Application/Contracts/Persistence/IEventRegistrationIntentRepository.cs` | Exposes `CreateWithChildrenAndCapacityAsync`, existing-intent lookup, and registered-user fanout batch. |
| `Explore.Persistence/Repositories/EventRegistrationIntentRepository.cs` | Runs serializable transaction, reserves capacity through conditional SQL update, inserts parent/children, inserts optional `EmailDispatchOutbox`, preserves original retryable transaction failures when rollback cleanup sees an already-completed Npgsql transaction, catches parent-intent duplicate `23505` violations for event/day/session scopes, and uses named tenant-filter bypass for exact tenant fanout. |
| `Explore.Persistence/Configurations/Entities/EventRegistrationIntentConfiguration.cs` | Has partial unique indexes for active event, day, and session-selection registration intents; same-column event/session indexes use distinct EF model names so both survive migrations. |
| `Explore.Persistence/Configurations/Entities/EventRegistrationConfiguration.cs` | Has a unique active child registration index per tenant/event/session/user. |
| `Event.Application.UnitTests/Features/EventRegistrations/Commands/CreateEventRegistrationCommandHandlerTests.cs` | Tests available capacity, waitlist message, event-scope child rows, and outbox creation. |
| `Event.Persistence.IntegrationTests/Repositories/EventRegistrationIntentRepositoryTests.cs` | Proves capacity-one concurrency, event/day/session duplicate parent-intent idempotency, duplicate registration-confirmation dispatch-row prevention, alternate-session rollback, and child-level unique failure rollback against PostgreSQL. |
| `Event.API.IntegrationTests/Features/EventRegistrationControllerTests.cs` | Proves the create endpoint preserves an idempotent command success response with the existing registration id. |
| `Event.API.IntegrationTests/Features/EventRegistrationRealRuntimeTests.cs` | Uses `RealRuntimeApiFixture` and PostgreSQL/Testcontainers to prove session-selection create success, repeat-submit idempotency, full-session waitlist behavior, one registration-confirmation outbox row, capacity preservation, and unauthenticated create rejection. |
| `Explore.Blazor.Client.E2ETests/Flows/CriticalFlows/RegistrationFlowTests.cs` | Exercises API registration and checks Mailpit/email dispatch when full runtime dependencies are available. |

Launch implication:

- Registration is source-implemented enough to verify and harden. Do not redesign it unless tests expose a defect.

### Email Dispatch

| File | Evidence |
|---|---|
| `Explore.Domain/EmailDispatchOutbox.cs` | Durable dispatch intent with pending/processing/sent/retry/dead-letter/parked/unknown/skipped states. |
| `Explore.Domain/EmailDispatchAttempt.cs` | Attempt ledger for each send try, including skipped-by-preference attempts. |
| `Explore.Domain/EmailDispatchReceipt.cs` | Idempotent receipt tracking keyed to provider/publish events, including skipped receipts. |
| `Explore.Application/Services/EventLifecycleEmailOutboxFactory.cs` | Builds lifecycle email outbox rows, including registration confirmation. Dispatch appends unsubscribe affordances when the row maps to a preference-controlled category and public base URL is configured. |
| `Explore.Infrastructure/EmailDispatchDrainService.cs` | Drains pending rows, recovers stale processing rows, respects tenant pause, claims receipts, enforces mapped `UserNotificationPreference`, adds unsubscribe headers/body links when public base URL is configured, sends through `IEmailService`, records attempts, and updates statuses/metrics. |
| `Explore.Persistence/Repositories/EmailDispatchOutboxRepository.cs` | Implements worker polling, operator status, tenant pause, park, replay, stale processing recovery, receipt claim/update, skipped settlement, and status transitions. |
| `Explore.API/Scheduling/TickerQScheduledEmailDispatchTrigger.cs` | Schedules email dispatch drain through TickerQ. |
| `Explore.API/HealthChecks/EmailDispatchHealthCheck.cs` | Health coverage for email dispatch backlog/failure state. |
| `docs/OPERATIONS.md` | Describes Basic Email Dispatch Mode, TickerQ, hosted worker fallback, tenant pause, and operator signals. |

Launch implication:

- The launch task is now runtime proof and remaining reliability coverage, not inventing a new email framework.

### Unsubscribe and Preferences

| File | Evidence |
|---|---|
| `Explore.API/Controllers/EmailUnsubscribeController.cs` | Public unsubscribe endpoint exists. |
| `Explore.Infrastructure/Mail/Unsubscribe/EmailUnsubscribeTokenService.cs` | Token generation/validation exists. |
| `Explore.Application/Contracts/Services/IEmailUnsubscribeTokenService.cs` | Application contract exists. |
| `Explore.Persistence/Repositories/UserNotificationPreferenceRepository.cs` | Preference persistence exists. |
| `Event.API.IntegrationTests/Features/EmailUnsubscribeControllerTests.cs` | API integration coverage exists. |
| `Explore.Infrastructure.Tests/Infrastructure/EmailUnsubscribeTokenServiceTests.cs` | Token service coverage exists. |

Launch implication:

- Foundation is now integrated into dispatch for mapped lifecycle categories and proven through Mailpit for registration confirmation. Remaining evidence: owner confirmation that registration confirmations should remain category-preference controlled rather than transactional-exempt.

### Public Surface

| File | Evidence |
|---|---|
| `Explore.API/Controllers/EventController.cs` | Calendar endpoint exists on event controller; runtime smoke returned HTTP 200 `text/calendar` for the seeded Sisters Quran & Tafsir event after launch seed repair. |
| `Explore.API/Services/Calendar/IcalNetEventCalendarFileBuilder.cs` | iCal builder exists. |
| `Event.API.IntegrationTests/Features/Calendar/IcalNetEventCalendarFileBuilderTests.cs` | Calendar builder coverage exists and passed in the focused Phase 1 public-smoke verification. |
| `Explore.API/Controllers/SitemapController.cs` | `sitemap.xml` controller exists. |
| `Explore.Blazor/Controllers/RobotsController.cs` | `robots.txt` controller exists. |
| `Explore.Blazor.Client/Helpers/CanonicalUrlHelper.cs` | Canonical URL helper exists. |
| `Explore.Blazor.Client.Tests/Seo/CanonicalMetadataTests.cs` | Canonical metadata tests exist. |
| `Explore.Blazor.Client/Pages/Events/EventDetail.razor` | Event detail includes canonical/Open Graph/Twitter metadata. |
| `docs/SEO.md` | Documents sitemap, robots, public render-policy classification, and tenant public-experience controls; explicitly says JSON-LD automation and site-wide SEO automation are not proven. |

Launch implication:

- Calendar/sitemap/robots/canonical/social metadata are now source-complete with one successful FullLocal public smoke pass for the seeded event and public crawl endpoints. JSON-LD automation was not found by source search, no web manifest was found beyond existing favicon/landing icon assets, and broader SEO/status-edge visual proof remains open.

### HAL and UI Authorization

| File | Evidence |
|---|---|
| `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs` | Central HAL link helper methods exist. |
| Event detail/list/edit/sidebar components | Many launch-critical event actions already use `HasHalLink(...)`. |
| `Explore.Blazor.Client/Helpers/RoleHelper.cs` | Role helper still exists for labels/colors and some eligibility/admin logic. |
| `Explore.Blazor.Client/Services/EventCreationEligibilityService.cs` and selected admin/member components | Some paths still derive behavior from roles or combine role display with HAL checks. |

Launch implication:

- Do a targeted audit. Replace local role-based action affordance gates with HAL links; keep role helpers where they only format labels, colors, or select role options.

### Data Protection and Health

| File | Evidence |
|---|---|
| `Explore.Persistence/DataProtectionKeyContext.cs` | Persisted Data Protection key context exists. |
| `Explore.Persistence/DataProtectionKeyContextFactory.cs` | Design-time/runtime factory exists. |
| `Explore.Persistence/Extensions/DataProtectionServiceCollectionExtensions.cs` | Service registration exists. |
| `Event.MigrationService/Worker.cs` | Migration service migrates Data Protection key context. |
| `Event.Persistence.IntegrationTests/DataProtection/DataProtectionKeyPersistenceTests.cs` | Persistence tests exist. |
| `Explore.Blazor.IntegrationTests/Endpoints/BffDataProtectionCookieRestartTests.cs` | BFF-level cookie middleware restart proof exists: one host issues a protected cookie, a second fresh host using the same persisted key context authenticates it. |
| `docs/OPERATIONS.md` | Current health table covers API, Blazor, email dispatch, queue, idempotency cleanup, storage, TickerQ, and related checks. |

Launch implication:

- Middleware-level cookie restart continuity and Data Protection key-store health/log visibility are covered. Full AppHost/browser/OIDC restart proof is still required.

### Runtime Proof - 2026-07-04

| Evidence | Result |
|---|---|
| Aspire CLI alignment | Old CLI `13.3.0-preview.1.26221.24+c8e41e142776da4d569f8b30c4c62aa026061715` was backed up to `/tmp/aspire-13.3.0-preview.1.26221.24`; current `aspire --version` is `13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248`. |
| Aspire doctor | `aspire doctor --format Json` passed CLI version, .NET `10.0.300`, and Docker `29.6.1`; only dev-certificate trust remains a warning. |
| Targeted build | `dotnet build Explore.AppHost/Explore.AppHost.csproj --configuration Debug --verbosity quiet` passed with 0 errors after AppHost/seed changes. |
| Foreground startup | `ISLAMU_ASPIRE_MODE=FullLocal SecretProvider__Provider=None Infisical__ProjectId= Infisical__ClientId= Infisical__ClientSecret= aspire run --apphost Explore.AppHost/Explore.AppHost.csproj --isolated` stayed alive and registered through the CLI. |
| Foreground health | `aspire wait explore-api --status healthy` and `aspire wait explore-blazor --status healthy` both succeeded. API `/alive` and `/health` returned HTTP 200 Healthy; Blazor `/alive` and `/health` returned HTTP 200 Healthy. |
| Foreground dynamic dependencies | API environment used `KEYCLOAK_ENDPOINT=http://localhost:33477/auth`, `MAIL_SMTP_PORT=42037`, `S3Settings__Endpoint=http://localhost:36501`, `Reporting__Coop__EndpointUrl=http://localhost:39647`, and `Webhooks__Svix__BaseUrl=http://localhost:35383`. |
| Foreground DB seed proof | PostgreSQL `system_settings` contained `email.smtp_host="localhost"`, `email.smtp_port=42037`, and `email.smtp_security="None"`, matching the foreground Mailpit endpoint. |
| SMTP outage proof | A later foreground isolated run exposed API `http://localhost:33675` and Mailpit SMTP port `45967`. Baseline API `/health` returned HTTP 200 Healthy with `smtp` Healthy in `time_total=0.233597`. After `aspire resource mailpit stop`, API `/health` returned HTTP 503 with `X-Health-Status: Unhealthy`; the `smtp` check was Unhealthy with `durationMs=5000.9584` and the response returned in `time_total=5.014349`. `aspire resource mailpit start` restored Mailpit and API `/health` returned HTTP 200 Healthy in `time_total=0.104138`. |
| OIDC proof | Keycloak metadata returned issuer `http://localhost:33477/auth/realms/ISLAMU`, matching the isolated endpoint supplied to API and Blazor. |
| MinIO proof | `minio-bootstrap` exited with code 0 and logged `Bucket created successfully local/explore`; `mc stat local/explore` confirmed the `explore` bucket exists in `us-east-1`; API `storage` health was Healthy. |
| Detached startup | Official Aspire docs describe `aspire start` as a detached AppHost that remains inspectable through `aspire ps`, `describe`, `logs`, and `stop`. Local evidence on CLI `13.4.6` contradicts that in this workspace: `aspire start --format Json --isolated` and `aspire run --detach --format Json --isolated` returned startup JSON, AppHost logs reached readiness, then `aspire ps --format Json` returned `[]` and the AppHost PID was gone. |
| Detached health | Earlier detached `aspire wait` reported API and Blazor Healthy; API `/health` returned HTTP 200 Healthy with 18 checks; Blazor `/health` returned HTTP 200 Healthy with 7 checks. Later detached runs no longer stayed registered long enough for reliable health or endpoint smoke, so foreground `aspire run --isolated` is now the only trusted launch-proof path. |
| Detached dynamic dependencies | API environment used dynamic endpoints: `KEYCLOAK_ENDPOINT=http://localhost:37701/auth`, `MAIL_SMTP_PORT=45665`, `S3Settings__Endpoint=http://localhost:42201`, `Reporting__Coop__EndpointUrl=http://localhost:41191`, and `Webhooks__Svix__BaseUrl=http://localhost:43243`. |
| Detached DB seed proof | PostgreSQL `system_settings` contained `email.smtp_host="localhost"`, `email.smtp_port=45665`, and `email.smtp_security="None"`, matching the detached Mailpit endpoint. |
| Cleanup | `aspire stop --apphost Explore.AppHost/Explore.AppHost.csproj` removed the AppHost from `aspire ps`; DCP left child processes after stop, and those run-owned PIDs were killed manually. |
| Public seed repair | Seeded event `018e4e5c-7f00-7000-8000-000000000061` initially had published event metadata but draft child sessions and null `last_session_end_utc`, causing public list and calendar filters to exclude it. `SeedData` now emits published sessions and `LastSessionEndUtc`; `DatabaseSeeder` repairs existing Development catalog rows across persistent volumes. |
| Public event list | `GET http://localhost:34857/api/event?pageSize=6` returned `totalCount=6` with `Youth Aqeedah Circle`, `Family Seerah Story Night`, `Arabic for Quran Beginners`, `Brothers Fiqh of Purification Intensive`, `Online Hadith Methodology Webinar`, and `Sisters Quran & Tafsir Morning`. |
| Calendar smoke | `GET http://localhost:34857/api/event/018e4e5c-7f00-7000-8000-000000000061/calendar` returned HTTP 200, `Content-Type: text/calendar; charset=utf-8; v=0.1`, filename `sisters-quran-tafsir-morning.ics`, and VCALENDAR content including `DTSTART:20260706T083000Z` and `DTEND:20260706T103000Z`. |
| Public Blazor event detail | `GET http://localhost:41777/events/018e4e5c-7f00-7000-8000-000000000061` returned HTTP 200 with `Sisters Quran & Tafsir Morning`, canonical URL, Open Graph/Twitter title metadata, and security headers. |
| Sitemap/robots/static/error smoke | API `GET /sitemap.xml` returned HTTP 200 XML with deterministic event URLs; Blazor `GET /robots.txt` returned HTTP 200 `text/plain` with development `Disallow: /`; `favicon.ico`, `/image/Icon_landingpage.png`, and `/Error` rendered with expected content types/security headers. |
| Final runtime cleanup | The foreground AppHost was stopped with Ctrl-C, and `aspire ps --format Json` returned `[]`. |

Launch implication:

- FullLocal foreground startup, health, bounded SMTP outage readiness, dynamic dependency wiring, seed discovery, public endpoint smoke, registration-to-Mailpit email delivery, focused BFF Data Protection cookie continuity, Data Protection key-store failure visibility, and email-dispatch backlog/dead-letter health behavior are now proven. Continue with the remaining runtime proof: full browser/OIDC restart continuity. Detached Aspire lifecycle should be re-tested only after a CLI/runtime tooling update or when specifically investigating the Aspire CLI.

## Validation Results for Phase 1 Detached CLI Follow-up and Embedded Control-Plane Build

Checks run after the later detached lifecycle flake and control-plane host compile failure:

```bash
dotnet build src/Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet
aspire run --detach --apphost Explore.AppHost/Explore.AppHost.csproj --isolated --format Json
aspire ps --format Json
aspire describe --apphost Explore.AppHost/Explore.AppHost.csproj --format Json
ps -fp 3623272 3618280
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress
git diff --check -- src/Explore.Blazor src/Explore.Blazor.Client dev/next/mvp-launch docs/OPERATIONS.md docs/TROUBLESHOOTING.md dev/_journal/journal.md
rg -n "[[:blank:]]+$" src/Explore.Blazor src/Explore.Blazor.Client dev/next/mvp-launch docs/OPERATIONS.md docs/TROUBLESHOOTING.md dev/_journal/journal.md
```

Results:

- Embedded control-plane host build: the public `Explore.Blazor`/`Explore.Blazor.Client` ownership path passed with 0 errors; the UI no longer depends on a separate public control-plane host project.
- Detached Aspire after the control-plane fix returned JSON with `appHostPid=3623272`, `cliPid=3618280`, dashboard URL, and log file.
- AppHost log reached resource readiness and `Notifying AppHost startup readiness`.
- Immediate lifecycle checks failed the official detached contract: `aspire ps --format Json` returned `[]`, `aspire describe` said no AppHost was currently running, and `ps -fp 3623272 3618280` returned no process rows.
- Context7 and official Aspire CLI docs confirm `aspire start`/detached mode is intended to leave an AppHost inspectable by `aspire ps`, so this is documented as a local CLI/tooling limitation rather than an application startup blocker.
- Full Release build after the control-plane/docs update: passed, 28 projects, 0 errors, 8,816 existing warning backlog entries.
- Architecture tests: passed, 255 total, 254 succeeded, 1 known skip.
- `git diff --check`, direct trailing-whitespace scan, and final `aspire ps --format Json` cleanup check passed; no AppHost remained registered.

## Validation Results for SMTP Outage Health Proof and Bounded Timeout

Checks run after the SMTP readiness timeout hardening, foreground FullLocal Mailpit outage proof, and docs/runbook updates:

```bash
env ISLAMU_ASPIRE_MODE=FullLocal SecretProvider__Provider=None Infisical__ProjectId= Infisical__ClientId= Infisical__ClientSecret= aspire run --non-interactive --apphost Explore.AppHost/Explore.AppHost.csproj --isolated
aspire wait explore-api --status healthy --apphost Explore.AppHost/Explore.AppHost.csproj
aspire describe explore-api --apphost Explore.AppHost/Explore.AppHost.csproj --format Json
aspire describe mailpit --apphost Explore.AppHost/Explore.AppHost.csproj --format Json
curl -sS -i -w '\ntime_total=%{time_total}\n' http://localhost:33675/health
aspire resource mailpit stop --apphost Explore.AppHost/Explore.AppHost.csproj
aspire wait mailpit --status down --timeout 60 --apphost Explore.AppHost/Explore.AppHost.csproj
curl -sS -i -w '\ntime_total=%{time_total}\n' http://localhost:33675/health
aspire resource mailpit start --apphost Explore.AppHost/Explore.AppHost.csproj
aspire wait mailpit --status healthy --timeout 90 --apphost Explore.AppHost/Explore.AppHost.csproj
curl -sS -i -w '\ntime_total=%{time_total}\n' http://localhost:33675/health
aspire ps --format Json
pgrep -af "aspire run|Explore.AppHost|Event.MigrationService|Explore.API"
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/SmtpHealthCheckRegistrationTests/*" --minimum-expected-tests 1
git diff --check -- Explore.API/Program.cs Event.API.IntegrationTests/Features/SmtpHealthCheckRegistrationTests.cs dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md docs/OPERATIONS.md docs/TROUBLESHOOTING.md dev/_journal/journal.md
rg -n "[[:blank:]]+$" Explore.API/Program.cs Event.API.IntegrationTests/Features/SmtpHealthCheckRegistrationTests.cs dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md docs/OPERATIONS.md docs/TROUBLESHOOTING.md dev/_journal/journal.md
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress
```

Results:

- Focused API integration test proved `smtp` readiness is registered with a five-second ASP.NET Core health-check timeout, `Unhealthy` failure status, and readiness/SMTP/infrastructure tags.
- Baseline API `/health` returned HTTP 200 with `X-Health-Status: Healthy`; `smtp` was Healthy with `Connection successful` and `time_total=0.233597`.
- `aspire resource mailpit stop` made `aspire wait mailpit --status down` pass, then API `/health` returned HTTP 503 with `X-Health-Status: Unhealthy`; `smtp` was Unhealthy with `Connection test failed: A task was canceled.`, `durationMs=5000.9584`, and `time_total=5.014349`.
- `aspire resource mailpit start` restored Mailpit on the same isolated SMTP port, then API `/health` returned HTTP 200 Healthy, `smtp` Healthy, and `time_total=0.104138`.
- Foreground AppHost was stopped with Ctrl-C. Final `aspire ps --format Json` returned `[]`, and no matching AppHost/API/MigrationService process remained.
- Release build passed, 28 projects, 0 errors, 2,102 existing warning backlog entries.
- Focused SMTP health-check registration test passed, 1 total, 1 succeeded.
- Architecture tests passed, 257 total, 256 succeeded, 1 known API response-metadata skip.

## Validation Results for Phase 2.5 Registration Mailpit Runtime Proof

Checks run after extending the AppHost/Mailpit E2E fixture and registration-flow assertions:

```bash
dotnet build Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/RegistrationFlowTests/*" --minimum-expected-tests 1 --no-progress
```

Results:

- Focused E2E build passed: 15 projects, 0 errors; warnings remain the existing package/analyzer backlog.
- Focused registration critical-flow E2E passed: 1 total, 1 succeeded, 0 failed in 1m 31s.
- The runtime proof exercised real Testcontainers/PostgreSQL, Keycloak auth, AppHost API/Blazor wiring, Mailpit SMTP capture, the TickerQ email-dispatch trigger, and the `EmailDispatchDrainService`.
- The test report was written to `Explore.Blazor.Client.E2ETests/bin/Release/net10.0/TestResults/Explore.Blazor.Client.E2ETests-linux-net10.0-report.html`.
- Diff whitespace check passed for the touched E2E fixture/test files and MVP launch docs, and direct trailing-whitespace scan returned no matches.
- Architecture tests passed after the docs update: 258 total, 257 succeeded, 1 known response-metadata skip.
- Full solution Release build was attempted after the focused E2E proof and did not pass because of unrelated dirty-worktree issues outside this slice: `Explore.Blazor.Client.Tests` has generated-client anonymous HAL type mismatches such as `Anonymous56` versus `Anonymous57`, and `Explore.API` hit a transient static-web-assets cache file lock on `obj/Release/net10.0/rjsmcshtml.dswa.cache.json`.

## Key Decisions

### D1 - Registration email uses specialized EmailDispatchOutbox

Use `EmailDispatchOutbox` as the canonical durable intent for lifecycle email. Do not add a second registration confirmation pipeline through generic `OutboxMessage`.

### D2 - Runtime proof before feature expansion

Start implementation with launch runtime evidence because many old backlog items are already source-complete but not proven in a deployable flow.

### D3 - Email compliance is explicit work

Unsubscribe endpoints and tokens are not enough for launch. Dispatch must integrate headers, visible unsubscribe URLs where applicable, preference checks, metrics, and tests.

### D4 - Registration schema mostly exists

Partial unique indexes and capacity transactions already exist. Add tests first; only add migrations if tests show a real gap.

### D5 - HAL remains the UI affordance contract

Local roles may support display and selection, but launch-critical write affordances must come from `_links`.

### D6 - Manifest/offline split

Minimal manifest/icons can be launch scope if desired. Offline service worker behavior is deferred unless the owner re-scopes it.

### D7 - Keep docs tied to behavior

Do not update broad docs speculatively. Update operations/security/API/Blazor docs only when implementation behavior changes or runtime proof reveals required operator guidance.

## Constraints to Preserve

- Repositories return entities, not DTOs.
- Validators are manually instantiated.
- Use `int` for lookups, `Guid` UUIDv7 for aggregates, and `long` for cursors.
- Public `GET` endpoints use `[AllowAnonymous]` when intended; writes use `[Authorize]`.
- Every source file starts with two `ABOUTME:` lines.
- HAL `_links` are the source of truth for UI action affordances.
- Tenant-filter bypasses require explicit named reasons and exact tenant predicates.
- Project-level tests only; no solution-level `dotnet test`.
- Preserve unrelated dirty worktree changes.

## Current Risks and Unknowns

| Risk or unknown | Current read |
|---|---|
| Registration confirmation opt-out semantics | Current implementation treats `RegistrationConfirmation` as preference-controlled because `NotificationPreferenceCategories.RegistrationConfirmations` exists. Owner should confirm this is intended, or adjust category mapping before launch. |
| Email runtime proof | Unit/integration tests pass for preference/unsubscribe, skipped settlement, stale recovery, retry/dead-letter behavior, and idempotent processing/receipt claims. The registration-confirmation path now has Mailpit proof through the real AppHost/API/drain path, including headers, body, recipient, and unsubscribe side-effect verification. |
| Email dispatch health proof | Focused API tests prove `email-dispatch` readiness reports safe counts and degrades on due retry backlog, stale `Processing`, and `DeadLettered` thresholds. A PostgreSQL-backed repository test proves the count predicates across tenants with the named worker bypass. |
| Registration duplicate API behavior | Repository, handler, persistence tests, API boundary tests, and real PostgreSQL API tests now cover event/day/session parent-intent duplicate races idempotently, including proof that duplicate retries do not persist a second registration-confirmation dispatch row. Real PostgreSQL API coverage now includes success, repeat-submit, waitlist, and unauthenticated rejection. Blazor client coverage now proves generated-client service agreement, safe error mapping, idempotent already-registered state, waitlist copy, and HAL-gated event-detail registration affordances. Remaining risk is runtime/visual release evidence. |
| JSON-LD | Re-checked on 2026-07-04: source search and `docs/SEO.md` did not find JSON-LD automation. Treat as a launch gap only if structured data is in scope. |
| Web app manifest/icons | Re-checked on 2026-07-04: no web manifest was found; existing assets are `Explore.Blazor/wwwroot/favicon.ico` and `Explore.Blazor.Client/wwwroot/image/Icon_landingpage.png`. Decide if installability is launch scope. |
| Runtime proof | Core FullLocal foreground startup, health, bounded SMTP outage readiness, registration-driven Mailpit delivery, public endpoint smoke, focused BFF Data Protection cookie continuity, Data Protection key-store failure visibility, and email-dispatch backlog/dead-letter health behavior are green on Aspire CLI/AppHost `13.4.6` plus focused API/PostgreSQL/Blazor tests, with dynamic endpoints, MinIO bucket bootstrap, SMTP seed refresh, deterministic public seed repair, and event list/detail/calendar/sitemap/robots/static/error smoke verified. Detached Aspire CLI lifecycle is explicitly not stable in this local workspace and is no longer treated as an application-readiness gate. Remaining risks are full browser/OIDC restart proof and visual/browser release evidence. |
| Dirty worktree | Many unrelated files were modified before this rebaseline. Implementation agents must isolate their own changes. |
| Missing `RTK.md` import | `AGENTS.md` references it, but the file was not found during this session. |

## Validation Results for Phase 1 Public Smoke and Seed Repair

Checks run after repairing deterministic launch seed status/schedule summaries and validating the public list/detail/calendar path:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/DatabaseSeederTests/SeedAsync_InDevelopment_RepairsLaunchCatalogDiscoveryFieldsAcrossStartups" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/GetEventCalendarExportRequestHandlerTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventControllerCalendarTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IcalNetEventCalendarFileBuilderTests/*" --minimum-expected-tests 1 --no-progress
```

Results:

- Release build: passed, 28 projects, 0 errors, 8,898 existing warning backlog entries.
- Focused PostgreSQL seed repair regression: passed, 1/1.
- Focused calendar application handler tests: passed, 4/4.
- Focused API calendar controller tests: passed, 2/2.
- Focused iCalendar builder tests: passed, 1/1.
- An initial broad API TUnit filter for `*Calendar*` matched zero tests and exited 8; rerunning with concrete test class filters passed.
- Runtime smoke after the fix passed for public event list, event detail metadata/security headers, event `.ics`, sitemap, robots, branded error page, favicon, and landing icon. The AppHost was stopped after proof and `aspire ps --format Json` returned `[]`.

## Validation Results for Runtime-Plan Refresh

Checks run after updating Phase 1 runtime evidence and SEO/manifest evidence:

```bash
git diff --check -- dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md
rg -n "[[:blank:]]+$" dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress
```

Results:

- Diff whitespace check: passed.
- Trailing-whitespace scan: no matches.
- Architecture tests: passed, 243 total, 242 succeeded, 1 existing documented API response-metadata skip.
- Latest rerun after adding the controlled Aspire backchannel-timeout evidence also passed: diff whitespace check clean, trailing-whitespace scan no matches, and architecture tests passed 243 total, 242 succeeded, 1 existing documented API response-metadata skip.
- CTO skill's sample `--filter` commands were attempted first, but this repository's TUnit/Microsoft Testing Platform runner rejects `--filter`; use `--treenode-filter` or the project-level architecture command above.
- Runtime startup, health, bounded SMTP outage readiness, registration-to-Mailpit delivery, public endpoint proof, focused BFF Data Protection cookie continuity, Data Protection key-store failure visibility, and email-dispatch backlog/dead-letter health behavior are now green for foreground FullLocal isolated Aspire plus focused API/PostgreSQL/Blazor tests. Remaining runtime proof is narrower: full browser/OIDC restart continuity and browser/visual evidence.

## Validation Results for This Rebaseline

Checks run after rewriting the workstream:

```bash
rg -n "RegistrationConfirmedOutbox(H|h)andler|RoutingOutboxMessage(D|d)ispatcher|Last Updated: 2026-0[35]|WP-[0-9]" dev/active/mvp-launch
rg -n "Last Updated: 2026-07-04 Europe/Brussels" dev/active/mvp-launch/*.md
git diff --check -- dev/active/mvp-launch
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Results:

- Stale-reference check: no obsolete handler names, May 2026 date markers, or old work-package IDs remained.
- Header/date check: all three files have `ABOUTME` headers and `Last Updated: 2026-07-04 Europe/Brussels`.
- Diff whitespace check: passed.
- Release build: passed, 25 projects, 0 errors, 27 warnings. Warnings included existing NuGet vulnerability warnings for `AutoMapper` and `Microsoft.OpenApi`.
- Architecture tests: passed, 240 total, 239 succeeded, 1 existing skipped response-metadata rule.

Full implementation verification remains in the plan and tasks file.

## Validation Results for Phase 2 Email Dispatch Slice

Checks run after implementing dispatch-time preference and unsubscribe behavior:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchDrainServiceTests/*|/*/*/EmailDispatchRabbitMqDeadLetterReplayDecisionTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchTickerQJobsTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchOutboxTransitionRepositoryTests/*" --minimum-expected-tests 1 --no-progress
```

Results:

- Release build: passed, 25 projects, 0 errors, existing NuGet vulnerability warning backlog.
- Application unit tests: passed, 1,947/1,947.
- Focused infrastructure dispatch tests: passed, 10/10.
- Focused API TickerQ drain tests: passed, 5/5.
- Focused PostgreSQL email dispatch transition tests: passed, 12/12.
- Full `Explore.Infrastructure.Tests` was also attempted and failed only on unrelated pre-existing `AdminContextTests` role-check expectations: `IsTenantAdminAsync_UsesTenantAdminRoleCheck` and `IsGroupAdminAsync_WhenMembershipHasGroupAdminRole_ReturnsTrue`. The focused dispatch lane passed.

Follow-up Phase 2.4 reliability revalidation on 2026-07-04:

```bash
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchOutboxTransitionRepositoryTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchDrainServiceTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet -- --no-progress
```

Results:

- Focused PostgreSQL email dispatch transition tests: passed, 12/12.
- Focused Infrastructure email dispatch drain tests: passed, 12/12.
- Architecture tests: passed, 257 total, 256 succeeded, 1 existing API response-metadata skip.

## Validation Results for Phase 3 Registration Integrity Slice

Checks run after implementing the duplicate-intent persistence/API boundary slice and duplicate dispatch-row proof:

```bash
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventRegistrationIntentRepositoryTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventRegistrationControllerTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventRegistrationRealRuntimeTests/*" --minimum-expected-tests 3 --no-progress
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --no-progress
git diff --check -- Event.Persistence.IntegrationTests/Repositories/EventRegistrationIntentRepositoryTests.cs Event.API.IntegrationTests/Features/EventRegistrationControllerTests.cs Explore.Persistence/Configurations/Entities/EventRegistrationIntentConfiguration.cs Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs Explore.Persistence/Migrations/20260704120000_AddEventRegistrationEventScopeUniqueIndex.cs
git diff --check -- Event.API.IntegrationTests/Features/EventRegistrationRealRuntimeTests.cs dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md
rg -n "[[:blank:]]+$" Event.API.IntegrationTests/Features/EventRegistrationRealRuntimeTests.cs dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md
```

Results:

- Focused PostgreSQL registration-intent persistence tests: passed, 6/6.
- Debug rerun before the rollback cleanup fix failed with `InvalidOperationException: This NpgsqlTransaction has completed; it is no longer usable.`; the same focused class passed 6/6 after `RollbackIfPendingAsync(...)` preserved the original retryable transaction failure.
- Focused API event-registration controller tests: passed, 6/6.
- Focused real-runtime API create/repeat-submit/waitlist/unauthenticated tests: passed, 3/3.
- Architecture tests: passed, 240 total, 239 succeeded, 1 existing documented skip.
- Release build: passed, 25 projects, 0 errors, 895 warnings. Warnings are existing NuGet vulnerability/analyzer backlog, including `AutoMapper`, `Microsoft.OpenApi`, `Microsoft.CodeAnalysis.NetAnalyzers`, and analyzer findings.
- Full API integration suite: attempted after the focused real-runtime test passed, then aborted after unrelated broader failures had already appeared. At cancellation it reported 502 total, 479 succeeded, 22 failed, and 1 skipped. Observed failures were outside this registration slice, including `EventMultiTagFilterTests.GetEvents_WithMultiTagFilters_ShouldReturnOk` returning `GatewayTimeout` and `ExternalApiOwnerTypeIntegrationTests.UserOwnerKey_InMultiTenantMode_AuthenticatesAndResolvesTenant` failing to parse an empty response while the test host logged unreachable `https://phase0-auth.test/.well-known/openid-configuration`.
- Source diagnostics: focused test compilation and Release build completed with no errors for the edited persistence test; earlier LSP diagnostics remained clean for the edited registration-intent EF configuration, corrective migration, persistence test, and API test.
- Diff/trailing-whitespace checks: passed for the persistence/API slice files; direct trailing-whitespace scan also returned no matches for the untracked real-runtime API test plus MVP launch docs.

Documentation/source note:

- Earlier EF Core verification used local package XML to confirm the `HasIndex(expression, name)` overload semantics that keep same-column event/session partial indexes distinct in the EF model.
- This real-runtime API slice used Context7 against the official ASP.NET Core docs for `WebApplicationFactory` integration-test guidance, then followed the repository's `RealRuntimeApiFixture`/PostgreSQL host-profile conventions for the actual implementation.
- Create-specific `403 Forbidden` coverage was not added because the current `POST /api/eventregistration` contract authenticates the caller and binds `UserId` from claims, but does not perform a resource authorization check that can deny an authenticated caller with `403`. Existing registration read coverage still proves ownership mismatch returns `403`.

## Validation Results for Phase 3 Blazor Client Slice

Checks run after aligning the generated-client service wrapper, registration UI state/copy, and HAL-gated sidebar affordance:

```bash
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventListRegistrationWorkflowTests/*|/*/*/EventRegistrationServiceTests/*|/*/*/EventRegistrationTests/*|/*/*/EventDetailsSidebarTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventListTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --no-progress
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress
dotnet build --configuration Release --verbosity quiet
git diff --check -- Explore.Blazor.Client/Pages/Events/EventListRegistrationWorkflow.cs Explore.Blazor.Client/Services/EventRegistrationService.cs Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor Explore.Blazor.Client/Pages/Events/EventList.razor.cs Explore.Blazor.Client/Components/Events/EventPreviewWorkspace.razor.cs Explore.Blazor.Client.Tests/Pages/Event/EventListRegistrationWorkflowTests.cs Explore.Blazor.Client.Tests/Services/EventRegistrationServiceTests.cs Explore.Blazor.Client.Tests/Pages/Event/EventRegistrationTests.cs Explore.Blazor.Client.Tests/Pages/Event/EventListTests.cs Explore.Blazor.Client.Tests/Components/Event/EventDetailsSidebarTests.cs
rg -n "[[:blank:]]+$" Explore.Blazor.Client/Pages/Events/EventListRegistrationWorkflow.cs Explore.Blazor.Client/Services/EventRegistrationService.cs Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor Explore.Blazor.Client/Pages/Events/EventList.razor.cs Explore.Blazor.Client/Components/Events/EventPreviewWorkspace.razor.cs Explore.Blazor.Client.Tests/Pages/Event/EventListRegistrationWorkflowTests.cs Explore.Blazor.Client.Tests/Services/EventRegistrationServiceTests.cs Explore.Blazor.Client.Tests/Pages/Event/EventRegistrationTests.cs Explore.Blazor.Client.Tests/Pages/Event/EventListTests.cs Explore.Blazor.Client.Tests/Components/Event/EventDetailsSidebarTests.cs
```

Results:

- Focused workflow/service/modal/sidebar tests: passed, 10/10.
- Focused EventList component tests: passed, 27/27.
- Full `Explore.Blazor.Client.Tests`: passed, 1468/1469, with 1 existing documented component-accessibility skip.
- Architecture tests: passed, 242/243, with 1 existing documented API response-metadata skip.
- Release build: passed, 26 projects, 0 errors, 27 existing package/deprecation warnings.
- Diff/trailing-whitespace checks: passed for the Blazor/client slice files.
- Browser visual QA was not run for this slice because no Aspire/Compose runtime with an authenticated, seeded registration page was started. Runtime/visual proof remains tracked under Phase 1 and Phase 6.

Documentation/source note:

- Context7 official Blazor docs were used for current component-state guidance, and bUnit docs were used for current async assertion guidance.
- `docs/BLAZOR.md` now documents registration outcome classification, safe generated-client error handling, and HAL-gated registration actions.

## Validation Results for Phase 1 FullLocal Runtime Slice

Checks run after aligning Aspire CLI, replacing fixed FullLocal endpoints with Aspire endpoint expressions, adding MinIO bootstrap, refreshing Development SMTP seed rows, and updating runtime docs:

```bash
dotnet build Explore.AppHost/Explore.AppHost.csproj --configuration Debug --verbosity quiet
dotnet build Explore.AppHost/Explore.AppHost.csproj --configuration Release --verbosity quiet
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress
git diff --check -- Explore.AppHost/AppHost.cs Explore.Persistence/Seed/DatabaseSeeder.cs dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md docs/OPERATIONS.md docs/CONFIGURATION.md docs/TROUBLESHOOTING.md dev/_journal/journal.md .debug-journal.md
rg -n "[[:blank:]]+$" Explore.AppHost/AppHost.cs Explore.Persistence/Seed/DatabaseSeeder.cs dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md docs/OPERATIONS.md docs/CONFIGURATION.md docs/TROUBLESHOOTING.md dev/_journal/journal.md .debug-journal.md
```

Results:

- AppHost Debug build: passed with 0 errors.
- AppHost Release build: passed with 0 errors.
- Full Release build: passed with 0 errors; warnings remain existing analyzer/package backlog.
- Diff whitespace check: passed.
- Trailing-whitespace scan: no matches.
- Architecture tests: failed on unrelated dirty worktree state. `Rule_1_17_RawHttpJsonHelpers_MustStayIn_ApprovedBoundaries` reports `Explore.Blazor.Client/Services/SupportAccessClientService.cs`; that file was already modified outside this runtime-plan work.
- Runtime proof: foreground FullLocal isolated Aspire reached Healthy and carried the public smoke. Detached FullLocal was exercised with mixed results: one aligned run stayed registered and reached Healthy, while a later run returned JSON and then disappeared from `aspire ps`; keep detached lifecycle re-proof open.

## Validation Results for Phase 1.2 Data Protection Restart Slice

Checks run after adding the focused BFF cookie restart proof:

```bash
dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffDataProtectionCookieRestartTests/*" --minimum-expected-tests 1 --no-progress
```

Results:

- Focused Blazor integration build: passed, 10 projects, 0 errors, 9 existing warnings.
- Focused TUnit node: passed, 1 total, 1 succeeded, 0 skipped.
- Earlier build attempt failed only on the new test's missing imports/name ambiguity (`DataProtectionServiceCollectionExtensions`, `StatusCodes`, `Results`); the import/alias fix cleared those errors.
- The full solution hook/build remains noisy in this dirty worktree because unrelated modified projects carry existing warning/error backlog; this slice verified the modified Blazor integration project directly.

## Validation Results for Phase 1.2 Data Protection Health Visibility Slice

Checks run after adding Blazor `data-protection-keys` readiness and the migrated skill-schema repair needed for the architecture gate:

```bash
git diff --check -- .agents/skills/text-to-lottie/SKILL.md Explore.Blazor/HealthChecks/DataProtectionKeyStoreHealthCheck.cs Explore.Blazor/Program.cs Explore.Blazor.IntegrationTests/Endpoints/DataProtectionKeyStoreHealthCheckTests.cs docs/OPERATIONS.md dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-tasks.md dev/active/mvp-launch/mvp-launch-context.md
dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/DataProtectionKeyStoreHealthCheckTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress
```

Results:

- Diff whitespace check passed for the touched source/docs/context files.
- Focused Blazor integration build passed: 10 projects, 0 errors, 127 existing warnings.
- Focused Data Protection key-store health tests passed: 2 total, 2 succeeded.
- Full architecture tests passed: 259 total, 258 succeeded, 1 known response-metadata skip.

## Validation Results for Phase 1.3 Email Dispatch Health Slice

Checks run after adding the Basic Dispatch Mode backlog/dead-letter readiness proof:

```bash
git diff --check -- Explore.Application/Contracts/Persistence/IEmailDispatchOutboxRepository.cs Explore.Persistence/Repositories/EmailDispatchOutboxRepository.cs Explore.Infrastructure/EmailDispatchProcessorSettings.cs Explore.Infrastructure/EmailDispatchProcessorSettingsValidator.cs Explore.API/HealthChecks/EmailDispatchHealthCheck.cs Event.API.IntegrationTests/Features/EmailDispatchHealthCheckTests.cs Explore.Infrastructure.Tests/Fixtures/InMemoryEmailDispatchOutboxRepository.cs Explore.Infrastructure.Tests/Infrastructure/EmailDispatchProcessorSettingsValidatorTests.cs Event.Persistence.IntegrationTests/Repositories/EmailDispatchOutboxTransitionRepositoryTests.cs docs/CONFIGURATION.md docs/OPERATIONS.md
dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1
dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1
dotnet build Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchHealthCheckTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchProcessorSettingsValidatorTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchOutboxTransitionRepositoryTests/HealthCountMethodsCountDueRetryStaleProcessingAndDeadLetterRowsAcrossTenants" --minimum-expected-tests 1 --no-progress
```

Results:

- Diff whitespace check passed for the touched source/docs files.
- API integration build passed: 8 projects, 0 errors, 94 existing warnings.
- Infrastructure test build passed: 4 projects, 0 errors, 3 existing warnings.
- Persistence integration build passed: 5 projects, 0 errors, 4 existing warnings.
- Focused API health tests passed: 8 total, 8 succeeded.
- Focused infrastructure settings-validator tests passed: 10 total, 10 succeeded.
- Focused PostgreSQL repository count test passed: 1 total, 1 succeeded.

## Validation Results for Phase 4.1 BFF Storage Antiforgery Slice

Checks run after moving storage upload session/proxy endpoints from a documented antiforgery exception to the existing BFF antiforgery/self-call-token filter:

```bash
git diff --check -- Explore.Blazor/Extensions/BffStorageEndpoints.cs Explore.Blazor.IntegrationTests/Endpoints/BffStorageUploadProxyTests.cs docs/SECURITY-MODEL.md docs/BLAZOR.md dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md
dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffStorageUploadProxyTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffCookieForwardingHandlerTests/*|/*/*/BffSupportAccessEndpointsTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffSetupSecretEndpointsTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress
```

Results:

- Context7 official ASP.NET Core antiforgery docs were checked before changing the Minimal API endpoint filter behavior.
- Context7 official ASP.NET Core rate-limiting docs were checked before adding the setup-secret limiter regression.
- Diff whitespace check passed for touched source/docs/context files.
- Focused Blazor integration build passed: 10 projects, 0 errors, 74 existing warnings.
- Focused storage endpoint tests passed: 16 total, 16 succeeded.
- Adjacent BFF cookie-forwarding/support-access tests passed: 7 total, 7 succeeded.
- Focused setup-secret endpoint tests passed: 8 total, 8 succeeded.
- Architecture tests passed: 259 total, 258 succeeded, 1 known response-metadata skip.

Additional token-boundary verification:

```bash
dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/AuthStateSerializationPolicyTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffCurrentUserEndpointTokenBoundaryTests/*" --minimum-expected-tests 1 --no-progress
```

Results:

- Context7 official ASP.NET Core auth-state/token-storage docs were checked before closing the token-boundary gap.
- Focused Blazor integration build passed after adding token-boundary coverage: 10 projects, 0 errors, 15 existing warnings.
- Auth-state serialization tests passed: 2 total, 2 succeeded.
- Current-user BFF token-boundary tests passed: 1 total, 1 succeeded.
- Diff whitespace checks passed for tracked token-boundary test/docs edits, and direct trailing-whitespace scan passed for the new `BffCurrentUserEndpointTokenBoundaryTests.cs` file.
- Architecture tests passed after token-boundary coverage: 259 total, 258 succeeded, 1 known response-metadata skip.

## Handoff

Recommended next action:

1. Run the full browser/OIDC Data Protection restart proof while preserving the FullLocal database volume.
2. Hard-code no ports in future runtime checks; discover API/Mailpit endpoints through `aspire describe`.
3. Run remaining degraded dependency checks for storage, idempotency cleanup, queue, and TickerQ states.
4. Have the owner confirm whether registration confirmations should stay preference-controlled.
5. Run browser/visual proof for the registration flow now that Aspire startup, public endpoint smoke, Mailpit delivery, focused Data Protection cookie continuity, Data Protection key-store failure visibility, and email-dispatch backlog/dead-letter health behavior are stable.

Do not start by adding new registration/email abstractions. The current architecture already has the main pieces; launch work should make them reliable, compliant, observable, and documented.
