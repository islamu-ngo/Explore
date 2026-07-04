<!-- ABOUTME: Executable task checklist for the MVP launch closure workstream. -->
<!-- ABOUTME: Orders implementation around runtime proof, email compliance, registration integrity, security, public polish, and release evidence. -->

# MVP Launch Tasks

Last Updated: 2026-07-04 Europe/Brussels

Status: In implementation. Phase 2 email dispatch compliance and reliability controls now have source/test/docs/runtime coverage for registration-confirmation delivery, headers, body, and unsubscribe behavior; Phase 3 registration integrity has completed source/test/docs coverage for capacity, event/day/session duplicate intent handling, duplicate email-dispatch-row prevention, API boundary idempotent-create behavior, real PostgreSQL API repeat-submit behavior, waitlist behavior, unauthenticated create rejection, generated-client response agreement, Blazor service safe error mapping, registration UI outcome state/copy, and HAL-gated event-detail registration affordances. Phase 1 foreground FullLocal startup, health, bounded SMTP outage readiness, registration-driven Mailpit delivery, public endpoint smoke, focused BFF Data Protection cookie restart proof, Data Protection key-store failure visibility, and email-dispatch backlog/dead-letter degraded-health proof are now green: Aspire CLI is aligned to `13.4.6`, foreground `aspire run --isolated` keeps API and Blazor HTTP 200 Healthy, dynamic dependency endpoints are supplied by Aspire resource expressions, the MinIO bucket is bootstrapped, persisted SMTP settings refresh to each isolated Mailpit port, deterministic launch seed rows repair across persistent Development volumes, configured-SMTP outage returns HTTP 503 with `smtp` Unhealthy in about five seconds, event list/detail/calendar/sitemap/robots/static/error smoke passes, a fresh BFF host can authenticate a cookie ticket protected by an earlier host when both use the same persisted `DataProtectionKeyContext`, Blazor `data-protection-keys` readiness reports persisted key-table reachability/failure safely, and `email-dispatch` readiness now degrades on due retry backlog, stale `Processing`, and `DeadLettered` rows. Detached `aspire start`/`aspire run --detach` is documented as a local Aspire CLI limitation rather than an application-readiness gate because repeated isolated runs reached startup readiness, returned JSON, then disappeared from `aspire ps`. Full browser/OIDC restart proof, security/HAL audit, public polish, and broader launch closure remain open.

## Task Rules

- [ ] Before each phase, classify the concrete code change against `.claude/contract/intents.yaml`.
- [ ] Read the intent's `must_read_docs`, matching `.claude/rules/*.md`, and matching `.agents/skills/*/SKILL.md`.
- [ ] Confirm `paths_in_scope` before editing.
- [ ] Preserve unrelated dirty worktree changes.
- [ ] Keep every edited source file's two-line `ABOUTME:` header.
- [ ] Prefer project patterns over new abstractions.
- [ ] Run project-level tests only; do not run solution-level `dotnet test`.
- [ ] Update this file as work completes or scope changes.

## Phase 0 - Plan Review and Baseline

Goal: make sure implementation starts from a current, agreed, and measurable baseline.

### 0.1 Owner review

- [ ] Review `mvp-launch-plan.md`, `mvp-launch-context.md`, and this task list with the owner.
- [ ] Confirm that this is a launch-closure scope, not a redesign.
- [ ] Confirm deferred items: offline PWA, generic outbox rewrite, major navigation redesign, external API-key public rollout, RSS/Atom, advanced autosave/undo.
- [ ] Record any owner additions or removals directly in this workstream.

Acceptance:

- [ ] Owner-approved phase order exists.
- [ ] Deferred scope is explicit.

### 0.2 Worktree and baseline health

- [ ] Run `git status --short` and identify unrelated changes.
- [ ] Avoid editing files already changed by someone else unless they are part of the selected phase.
- [ ] Run the baseline build if practical:

```bash
dotnet build --configuration Release --verbosity quiet
```

- [ ] If baseline build fails, capture exact failing projects/errors and determine whether they predate MVP launch edits.
- [ ] Run architecture/context tests before code work:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Acceptance:

- [ ] Baseline result is recorded in this context or in a linked handoff note.
- [ ] Unrelated failures are separated from MVP launch work.

### 0.3 Source evidence refresh

- [x] Re-open the files named in `mvp-launch-context.md`.
- [x] Re-check whether email dispatch now has unsubscribe headers and preference checks.
- [x] Re-check whether JSON-LD and manifest/icons now exist. Result: JSON-LD automation and a web manifest were not found; only favicon/landing icon assets were found.
- [ ] Re-check whether current generated clients are in sync with public API changes.

Acceptance:

- [ ] The implementation agent can state what is already implemented and what remains before editing code.

## Phase 1 - Runtime Launch Proof

Goal: prove the current system can run in a self-hostable launch shape.

### 1.1 Start the distributed app

- [x] Attempt full-local Aspire startup through the contributor path with `ISLAMU_ASPIRE_MODE=FullLocal`, local secret-provider override, and `--isolated`.
- [x] Capture the 2026-07-04 startup failure: parent CLI timed out, child AppHost briefly started, `explore-api` changed `Running -> Finished`, and `aspire ps --format Json` returned `[]`.
- [x] Capture the 2026-07-04 controlled rerun: parent CLI again exited 2 after waiting for the AppHost backchannel, while the child log reached `Distributed application started`, `event-migrationservice` finished, `explore-api` and `explore-blazor` reached `Running`, API listened on `http://localhost:38665`, Blazor listened on `http://localhost:32773`, and `aspire ps --format Json` still returned `[]`.
- [x] Confirm local infrastructure containers remained available after the failed run, including PostgreSQL, Redis, RabbitMQ, Mailpit, Keycloak, Cerbos, MinIO, Prometheus, and Grafana.
- [x] Align the Aspire CLI/AppHost toolchain before treating the earlier evidence as an application startup defect. Result: old `13.3.0-preview.1.26221.24` CLI backed up to `/tmp/aspire-13.3.0-preview.1.26221.24`; current CLI is `13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248`.
- [x] Replace FullLocal hardcoded localhost dependency assumptions with Aspire endpoint expressions for Keycloak, Cerbos, MinIO, Mailpit, Svix, and Coop.
- [x] Add FullLocal MinIO bucket bootstrap so `local/explore` exists before API storage health runs.
- [x] Refresh Development SMTP seed settings when `ISLAMU_ASPIRE_MODE=FullLocal` so persistent local volumes do not retain stale isolated Mailpit ports.
- [x] Repeat foreground `aspire run --apphost Explore.AppHost/Explore.AppHost.csproj --isolated` after CLI alignment and AppHost wiring fixes.
- [x] Repeat detached `aspire start --apphost Explore.AppHost/Explore.AppHost.csproj --format Json --isolated` after CLI alignment and AppHost wiring fixes.
- [x] Capture launch JSON, `aspire ps --format Json`, process state, resource descriptions, dynamic endpoints, API/Blazor health, DB SMTP settings, Keycloak metadata, and MinIO bootstrap output for the aligned reruns, including the later detached lifecycle flake.
- [x] Confirm `explore-api` no longer changes `Running -> Finished` during the aligned proof and remains healthy long enough for direct smoke checks.
- [x] Re-run detached lifecycle after the latest `Client disconnected from auxiliary backchannel` flake and classify the result. Result: `aspire run --detach --apphost Explore.AppHost/Explore.AppHost.csproj --isolated --format Json` returned startup JSON and AppHost logs reached readiness, but immediate `aspire ps --format Json` returned `[]`; detached mode is documented as local CLI/tooling follow-up, not a Phase 1 launch blocker.
- [x] Confirm PostgreSQL starts and migrations run through `event-migrationservice` finishing under FullLocal.
- [x] Confirm TickerQ/email-dispatch health is available in the API health response.
- [x] Confirm Mailpit or local SMTP capture is available for email proof. Under `--isolated`, the API health check and persisted `email.smtp_port` followed the dynamically discovered Mailpit SMTP ports.
- [x] Confirm configured-SMTP outage behavior. Result: stopping Mailpit through `aspire resource mailpit stop` made API `/health` return HTTP 503 with `X-Health-Status: Unhealthy` and `smtp` Unhealthy in `5.014s`; restarting Mailpit restored HTTP 200 Healthy.
- [ ] Confirm optional dependencies have documented fallback behavior.

Acceptance:

- [x] A fresh developer/operator can reproduce the startup path from docs for FullLocal startup and health.
- [ ] No required launch dependency is undocumented.
- [x] API and Blazor remain running long enough to smoke health and public URLs.

Validation evidence:

- [x] Startup command and URL(s).
- [x] Relevant logs/resource descriptions showing API, Blazor, PostgreSQL, email dispatch/TickerQ health, Mailpit SMTP readiness, Keycloak, Cerbos, Svix, Coop, and MinIO readiness.
- [x] CLI/AppHost backchannel result, and API early-exit root cause or explicit operator recovery note if that symptom still reproduces. Result: API early exit did not reproduce after CLI alignment and endpoint/seed fixes; detached `aspire start`/`aspire run --detach` reaches readiness but does not remain inspectable in this local CLI session, so foreground `aspire run --isolated` is the operator proof path.

### 1.2 Data Protection persistence proof

- [ ] Log in through the full browser/OIDC BFF flow.
- [ ] Restart the full AppHost/BFF while preserving the database.
- [x] Confirm persisted Data Protection keys allow ASP.NET Core cookie-ticket continuity across fresh BFF hosts. Evidence: `BffDataProtectionCookieRestartTests.CookieTicketSurvivesFreshBffHostWhenKeyRingPersists`.
- [ ] Confirm full browser/OIDC auth-session continuity after AppHost restart.
- [x] Confirm migration service covers `DataProtectionKeyContext`. Evidence: `Event.MigrationService/Worker.cs` resolves `DataProtectionKeyContext` and runs `Database.MigrateAsync`.

Acceptance:

- [x] Cookie/session behavior is documented after restart at the Data Protection key-ring level.
- [x] Data Protection key storage failures surface in health/logs. Evidence: Blazor `data-protection-keys` readiness queries `DataProtectionKeyContext`, returns unhealthy when the key store is unreachable, logs a bounded failure type, and exposes no key XML or connection details.

Validation evidence:

- [x] `dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1` passed: 10 projects, 0 errors, 9 existing warnings.
- [x] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffDataProtectionCookieRestartTests/*" --minimum-expected-tests 1 --no-progress` passed: 1 total, 1 succeeded.
- [x] `git diff --check -- Explore.Blazor/HealthChecks/DataProtectionKeyStoreHealthCheck.cs Explore.Blazor/Program.cs Explore.Blazor.IntegrationTests/Endpoints/DataProtectionKeyStoreHealthCheckTests.cs docs/OPERATIONS.md dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-tasks.md dev/active/mvp-launch/mvp-launch-context.md` passed.
- [x] `dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1` passed: 10 projects, 0 errors, 127 existing warnings.
- [x] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/DataProtectionKeyStoreHealthCheckTests/*" --minimum-expected-tests 1 --no-progress` passed: 2 total, 2 succeeded.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress` passed: 259 total, 258 succeeded, 1 known response-metadata skip.

### 1.3 Health and degraded dependency proof

- [x] Smoke liveness and readiness endpoints for API and Blazor.
- [x] Confirm email-dispatch health in normal state.
- [x] Confirm health behavior when SMTP/Mailpit is unavailable.
- [x] Add bounded SMTP readiness timeout hardening. Result: `smtp` readiness is registered with a five-second ASP.NET Core health-check timeout; stopping Mailpit made API `/health` return HTTP 503 in `5.014s`, and restarting Mailpit restored HTTP 200 Healthy.
- [x] Confirm behavior when email dispatch backlog contains stale processing, retry, or dead-letter rows. Result: `email-dispatch` readiness exposes due/retry/stale/dead-letter aggregate counts, degrades on configured due backlog/stale/dead-letter thresholds, and keeps future retry-scheduled rows visible without treating them as due backlog.
- [ ] Confirm idempotency cleanup, storage, and queue/TickerQ health are represented in operations docs.

Acceptance:

- [ ] Health results are actionable and not misleading. Email-dispatch backlog/dead-letter behavior is now covered; idempotency/storage/queue/TickerQ representation remains open.
- [ ] Operators can identify the failing dependency and next step. Email-dispatch health now identifies due backlog, retry-scheduled count, stale processing leases, and dead-letter rows with thresholds; remaining health surfaces still need review.

Validation evidence:

- [x] `git diff --check -- Explore.Application/Contracts/Persistence/IEmailDispatchOutboxRepository.cs Explore.Persistence/Repositories/EmailDispatchOutboxRepository.cs Explore.Infrastructure/EmailDispatchProcessorSettings.cs Explore.Infrastructure/EmailDispatchProcessorSettingsValidator.cs Explore.API/HealthChecks/EmailDispatchHealthCheck.cs Event.API.IntegrationTests/Features/EmailDispatchHealthCheckTests.cs Explore.Infrastructure.Tests/Fixtures/InMemoryEmailDispatchOutboxRepository.cs Explore.Infrastructure.Tests/Infrastructure/EmailDispatchProcessorSettingsValidatorTests.cs Event.Persistence.IntegrationTests/Repositories/EmailDispatchOutboxTransitionRepositoryTests.cs docs/CONFIGURATION.md docs/OPERATIONS.md` passed.
- [x] `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1` passed: 8 projects, 0 errors, 94 existing warnings.
- [x] `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1` passed: 4 projects, 0 errors, 3 existing warnings.
- [x] `dotnet build Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1` passed: 5 projects, 0 errors, 4 existing warnings.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchHealthCheckTests/*" --minimum-expected-tests 1 --no-progress` passed: 8/8.
- [x] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchProcessorSettingsValidatorTests/*" --minimum-expected-tests 1 --no-progress` passed: 10/10.
- [x] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchOutboxTransitionRepositoryTests/HealthCountMethodsCountDueRetryStaleProcessingAndDeadLetterRowsAcrossTenants" --minimum-expected-tests 1 --no-progress` passed: 1/1.

### 1.4 Public endpoint smoke

- [x] Smoke public event detail route.
- [x] Smoke calendar `.ics` endpoint and content type.
- [x] Smoke `sitemap.xml`.
- [x] Smoke `robots.txt`.
- [x] Smoke branded error pages.
- [x] Smoke static assets referenced by event pages under current CSP/security headers.
- [x] Repair deterministic launch seed rows so public list/calendar filters see the seeded events under persistent Development volumes.

Acceptance:

- [x] Public launch URLs work without authentication where intended.
- [x] CSP/security headers do not break launch-critical assets.

Docs:

- [ ] Update `docs/OPERATIONS.md` if startup, health, or degraded-mode instructions are incomplete.

## Phase 2 - Registration Email Compliance and Dispatch Hardening

Goal: make lifecycle email dispatch safe, observable, and launch-ready.

### 2.1 Audit the existing dispatch path

- [x] Trace registration from command handler to `EmailDispatchOutbox`.
- [x] Trace `EmailDispatchDrainService` from pending row to `IEmailService.SendAsync`.
- [x] Identify message kinds that are transactional versus preference-controlled.
- [x] Confirm which message kinds require visible unsubscribe links.
- [x] Confirm which message kinds require `List-Unsubscribe` headers.
- [x] Confirm current attempt/receipt/status behavior for skipped, failed, retried, and sent messages.

Acceptance:

- [x] The audit distinguishes the current behavior: mapped lifecycle categories, including registration confirmations, are preference-controlled unless the owner later chooses a transactional exemption.

### 2.2 Add unsubscribe headers and visible links where required

- [x] Generate unsubscribe URLs with `IEmailUnsubscribeTokenService` or an existing equivalent.
- [x] Add visible unsubscribe links to applicable plain text and HTML bodies.
- [x] Add `List-Unsubscribe` header.
- [x] Add `List-Unsubscribe-Post: List-Unsubscribe=One-Click` where supported and appropriate.
- [x] Ensure unsubscribe URLs use configured public base URL and tenant context safely.
- [x] Add tests that inspect the final `EmailMessage.CustomHeaders` and rendered body.

Acceptance:

- [x] Applicable mapped lifecycle emails contain both machine-readable and human-visible unsubscribe affordances when public base URL configuration is valid.
- [x] Transactional-only exemption is not currently implemented; registration confirmations remain category-preference controlled and are documented as an owner-confirmation item.

### 2.3 Enforce preferences at dispatch time

- [x] Query `UserNotificationPreference` before sending preference-controlled messages.
- [x] Decide and implement the terminal state for skipped messages.
- [x] Record metrics for skipped-by-preference dispatches.
- [x] Make skipped messages visible to operators without looking like provider failures.
- [x] Ensure tenant pause still wins before send.

Acceptance:

- [x] A user who opted out does not receive preference-controlled lifecycle email from stale queued rows.
- [x] Skips are auditable and do not trigger retry loops.

### 2.4 Verify reliability controls

- [x] Test tenant pause prevents sends.
- [x] Test operator park/replay refuses terminal skipped rows.
- [x] Test replay moves eligible rows back to pending.
- [x] Test stale processing recovery moves abandoned rows to unknown and updates receipts. Evidence: `EmailDispatchOutboxTransitionRepositoryTests.MarkStaleProcessingAsUnknownRecoversOnlyExpiredLeases` passed in the focused PostgreSQL transition suite.
- [x] Test retry/dead-letter behavior with a failing provider. Evidence: `EmailDispatchDrainServiceTests.ProcessSingleAsyncPersistsExpectedSmtpFailureWithoutThrowing` and `ProcessSingleAsyncDeadLettersWhenRetryBudgetIsExhausted` passed in the focused Infrastructure suite.
- [x] Test receipt idempotency prevents duplicate provider sends for the same publish event. Evidence: `TryClaimReceiptRejectsDuplicatePublishEventForTenant`, `ConcurrentProcessingClaimsAllowOnlyOneNodeToSend`, and `ConcurrentReceiptClaimsAllowOnlyOneNodeToOwnPublishEvent` passed in the focused PostgreSQL transition suite.

Acceptance:

- [x] Email dispatch can be operated without database surgery for source-proven states: tenant pause, park/replay, skipped, stale-processing recovery, retry/dead-letter, and receipt idempotency are covered by focused service and PostgreSQL transition tests. Runtime Mailpit delivery is covered in Phase 2.5.

### 2.5 Runtime email proof

- [x] Register for an event through the real AppHost/API path with authenticated tenant context.
- [x] Confirm one `EmailDispatchOutbox` row is created in the registration transaction.
- [x] Trigger or wait for the TickerQ/hosted drain.
- [x] Confirm one Mailpit message is delivered when preferences allow.
- [x] Confirm subject, recipient, text body, HTML body, dispatch/correlation headers, `List-Unsubscribe`, `List-Unsubscribe-Post`, visible unsubscribe links, and tenant-aware unsubscribe behavior.

Acceptance:

- [x] Registration confirmation is proven through the real dispatch path and currently counts as the preference-controlled lifecycle proof because `RegistrationConfirmation` maps to `NotificationPreferenceCategories.RegistrationConfirmations`.

Docs:

- [x] Update `docs/OPERATIONS.md` email dispatch runbook.
- [x] Update `docs/SECURITY-MODEL.md` or privacy docs if preference/unsubscribe behavior changes.

## Phase 3 - Registration Integrity and Concurrency Evidence

Goal: prove registration cannot overbook or duplicate-register users.

### 3.1 Persistence concurrency tests

- [x] Add a PostgreSQL-backed test for concurrent registrations against a session with capacity `1`.
- [x] Assert only one approved child registration is created.
- [x] Assert remaining attempts are waitlisted, conflict-mapped, or idempotent according to the intended UX.
- [x] Assert `current_audience_attendees` never exceeds capacity.
- [x] Assert rollback paths do not increment capacity.

Acceptance:

- [x] Capacity invariants hold under real database concurrency.

### 3.2 Duplicate registration behavior

- [x] Test duplicate event-scope registration.
- [x] Test duplicate day-scope registration.
- [x] Test duplicate session-selection registration at the persistence boundary.
- [x] Map event/day/session-selection parent-intent unique violations to an idempotent existing-intent repository result.
- [x] Ensure the CQRS handler returns `Event Registration already exists.` for repository-detected duplicate races and does not run follow-on consent processing.
- [x] Assert the API boundary preserves an idempotent create success response with the existing registration id.
- [x] Assert parent-intent unique index violations are converted before the API response path rather than surfacing as raw `23505`/500 errors.
- [x] Assert repeated idempotent attempts do not create duplicate email dispatch rows.
- [x] Add real database-backed API repeat-submit coverage when a runtime fixture can seed event/day/session registration end to end.

Acceptance:

- [x] Users get deterministic behavior on repeat submit/double click/network retry through both persistence and real database-backed API coverage.

### 3.3 API and generated client checks

- [x] Add or update API integration tests for registration success, waitlist, duplicate, and unauthenticated cases.
- [x] Confirm create-specific `403` coverage is not currently applicable; `POST /api/eventregistration` has authentication only and binds `UserId` from claims. Add the test if a future resource authorization rule makes create capable of returning `403`.
- [x] Confirm no OpenAPI/NSwag regeneration is required for this slice because the create response contract still returns `BaseCommandResponseOfGuid`.
- [x] Update Blazor service/outcome mapping for the stable generated DTO shape.
- [x] Map generated-client `ApiException` values to safe `FailureCode`, `Message`, and `Errors` values in `EventRegistrationService`.
- [x] Prove user-registration lookup uses the current user's registrations instead of exposing or relying on session registration `UserId` values.

Acceptance:

- [x] API, generated client, and Blazor service agree on response shape and error mapping.

### 3.4 UI state and copy

- [x] Verify event detail registration action appears only from HAL `register` link.
- [x] Verify waitlist copy is clear for mixed approved/waitlisted session results.
- [x] Verify loading, confirmed, idempotent already-registered, waitlist, and failure states.
- [x] Verify no public UI exposes raw exception or database details.
- [x] Align `EventRegistration`, `EventList`, and `EventPreviewWorkspace` on the shared `EventListRegistrationWorkflow.ResolveOutcome` classifier.
- [x] Ensure preview-workspace inline registration uses the same policy-aware `BuildRegistrationRequest` as event-list registration.

Acceptance:

- [x] Registration UX matches actual backend state in bUnit/component coverage.

Docs:

- [x] Confirm `docs/API.md` has no required update for this slice because the API response contract did not change.
- [x] Update `docs/BLAZOR.md` for registration generated-client, service-layer, safe-error, outcome-classification, and HAL-affordance behavior.

## Phase 4 - Security, Audit, and HAL Cleanup

Goal: remove launch-class authorization, token, and audit risks.

### 4.1 Setup and BFF boundary checks

- [x] Verify setup metadata endpoint rate limiting and 429 response behavior. Result: `BffSetupSecretEndpointsTests` now runs the real BFF setup-secret limiter with `DisableInTesting=false`, a one-request window, stable partition cookie, and asserts the second POST returns `429` ProblemDetails with `Retry-After` before upstream validation.
- [x] Verify setup secret is stripped server-side and never exposed to the browser. Evidence: `BffSetupSecretEndpointsTests.SetupSecret_Post_WithBrowserSetupSecretHeader_ValidatesOnlyBodySecret`, `SetupSecretForwardingHandlerTests`, and `BffProxyHeaderSanitizerTests` prove browser-controlled `X-Setup-Secret` is ignored/removed and only BFF-owned resolver output is forwarded.
- [x] Verify proxied writes require antiforgery. Result: storage upload session/proxy endpoints now call the BFF `ValidateAntiforgery()` filter; browser requests without CSRF or a protected self-call token return `400`, while same-process InteractiveServer self-calls use a short-lived Data Protection protected `X-ISLAMU-BFF-SELF-CALL` token bound to method/path/host/user.
- [x] Verify access/refresh tokens remain server-side. Result: `Event.Web.BffHosting` reads `access_token` from the server-side authentication ticket only for outbound API forwarding, browser-controlled token headers are stripped before proxying, `AuthStateSerializationPolicy` serializes display claims only, `/auth/status` returns only auth state/name, and `/bff/me` filters token-shaped claims out of the current-user response.
- [x] Add regression tests where coverage is missing. Storage upload antiforgery/self-call, setup-secret rate-limit/stripping, browser Authorization-header rejection, proxy credential-header stripping, auth-state token exclusion, and `/bff/me` token-claim exclusion are now covered.

Acceptance:

- [x] The BFF boundary matches `docs/SECURITY-MODEL.md` and `blazor-bff-patterns`.

Validation evidence:

- [x] Context7 `/dotnet/aspnetcore.docs` antiforgery guidance checked: browser-accessible cookie-authenticated unsafe Minimal API endpoints need explicit antiforgery validation; disabling antiforgery is only appropriate for routes not vulnerable to browser cookie CSRF.
- [x] Context7 `/dotnet/aspnetcore.docs` Blazor/auth guidance checked: `SaveTokens` can keep OIDC tokens in the server-side cookie ticket, while `AddAuthenticationStateSerialization` is the separate browser-visible projection that must stay display-only.
- [x] `git diff --check -- Explore.Blazor/Extensions/BffStorageEndpoints.cs Explore.Blazor.IntegrationTests/Endpoints/BffStorageUploadProxyTests.cs docs/SECURITY-MODEL.md docs/BLAZOR.md dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md` passed.
- [x] `dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1` passed: 10 projects, 0 errors, 74 existing warnings.
- [x] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffStorageUploadProxyTests/*" --minimum-expected-tests 1 --no-progress` passed: 16/16.
- [x] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffCookieForwardingHandlerTests/*|/*/*/BffSupportAccessEndpointsTests/*" --minimum-expected-tests 1 --no-progress` passed: 7/7.
- [x] `dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1` passed after adding setup-rate-limit coverage: 10 projects, 0 errors, 15 existing warnings.
- [x] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffSetupSecretEndpointsTests/*" --minimum-expected-tests 1 --no-progress` passed: 8/8.
- [x] `dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -p:RunAnalyzers=false -m:1` passed after adding token-boundary coverage: 10 projects, 0 errors, 15 existing warnings.
- [x] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/AuthStateSerializationPolicyTests/*" --minimum-expected-tests 1 --no-progress` passed: 2/2.
- [x] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffCurrentUserEndpointTokenBoundaryTests/*" --minimum-expected-tests 1 --no-progress` passed: 1/1.
- [x] `git diff --check -- Explore.Blazor.IntegrationTests/Services/AuthStateSerializationPolicyTests.cs dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md` passed, and direct trailing-whitespace scan passed for the new `BffCurrentUserEndpointTokenBoundaryTests.cs` file.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress` passed after token-boundary coverage: 259 total, 258 succeeded, 1 known response-metadata skip.

### 4.2 Audit launch-critical writes

- [ ] Identify launch-critical writes: registration, cancellation, event publish/cancel/archive, team/member changes, setup/admin changes, email operator actions.
- [ ] Confirm audit log entries include tenant, actor, entity type/id, action, timestamp, and relevant metadata.
- [ ] Confirm audit views/endpoints are permission-gated and tenant-scoped.
- [ ] Add missing audit writes or tests where gaps are real.

Acceptance:

- [ ] Launch-critical writes can be investigated after the fact.

### 4.3 HAL affordance audit

- [ ] Search launch-critical UI for `RoleHelper`, `IsInRole`, local claim checks, and custom `CanManage` logic.
- [ ] Keep role helpers for labels/colors/role selection only.
- [ ] Replace write-action visibility and enabled states with HAL `HasHalLink(...)`.
- [ ] Add component/service tests for affordance behavior where practical.

Acceptance:

- [ ] UI write affordances follow API-supplied `_links`.

### 4.4 Security headers and asset smoke

- [ ] Verify CSP allows required event images, static assets, calendar downloads, and error pages.
- [ ] Verify security headers are present on public and authenticated routes.
- [ ] Verify no launch-critical inline script/style exception is undocumented.

Acceptance:

- [ ] Security headers are strong and compatible with launch flows.

Docs:

- [ ] Update `docs/SECURITY-MODEL.md` only for actual behavior changes.

## Phase 5 - Public SEO, Accessibility, Manifest, and UX Polish

Goal: make the public launch surface crawlable, shareable, and accessible.

### 5.1 SEO metadata

- [ ] Verify canonical URLs on public event detail.
- [ ] Verify Open Graph and Twitter metadata for event title, description, image, URL, and status edge cases.
- [x] Verify whether JSON-LD already exists. Result: 2026-07-04 source search and `docs/SEO.md` did not find structured-data automation.
- [ ] Add JSON-LD `Event` structured data if still absent.
- [ ] Verify noindex behavior for private/draft/deleted/moderated event states.
- [ ] Add or update metadata tests/snapshots.

Acceptance:

- [ ] Shared event links render useful previews and crawlers see consistent metadata.

### 5.2 Sitemap and robots runtime proof

- [ ] Confirm sitemap contains only public crawlable event URLs.
- [ ] Confirm robots references the correct canonical sitemap URL.
- [ ] Confirm host/base URL config works in local and deployment-like environments.
- [ ] Add tests for edge cases if missing.

Acceptance:

- [ ] Search engines receive the intended crawl map.

### 5.3 Minimal manifest and icons

- [ ] Decide whether installability is launch scope.
- [x] Verify whether a web manifest already exists. Result: no manifest/webmanifest file or manifest link was found; existing assets are limited to `favicon.ico` and `Icon_landingpage.png`.
- [ ] If yes, add a manifest with name, short name, start URL, scope, theme/background colors, display mode, and icons.
- [ ] Add real icon assets or generated approved assets.
- [ ] Add manifest link and smoke it in browser/runtime QA.
- [ ] Do not add offline/service-worker behavior unless separately approved.

Acceptance:

- [ ] Manifest is valid if included, and scope is intentionally minimal.

### 5.4 Accessibility and UX QA

- [ ] Run automated accessibility checks on public event detail, registration, auth boundary, admin-critical form, and error pages.
- [ ] Verify keyboard-only registration flow.
- [ ] Verify focus restore after dialogs and route transitions.
- [ ] Verify headings, landmarks, labels, live regions, contrast, target sizes, and reduced-motion behavior.
- [ ] Remove public placeholder/TODO/debug text.
- [ ] Run visual QA on desktop and mobile for launch-critical pages.

Acceptance:

- [ ] Launch-critical flows meet WCAG 2.2 AA expectations or have owner-approved exceptions.

Docs:

- [ ] Update `docs/BLAZOR.md` or `docs/ACCESSIBILITY.md` only if conventions change.

## Phase 6 - Contract, E2E, Docs, and Release Evidence

Goal: synchronize contracts and leave a release-quality evidence trail.

### 6.1 Contract and generated client sync

- [ ] Regenerate OpenAPI/NSwag clients after API changes.
- [ ] Verify generated client files are the only generated diffs expected.
- [ ] Update API snapshots/HAL snapshots.
- [ ] Verify no manual generated-client edits remain.

Acceptance:

- [ ] API contracts, generated clients, and tests agree.

### 6.2 Required test suite

Run the focused project-level suite appropriate to changed files. The expected launch closure suite is:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

- [ ] Run E2E only when runtime dependencies are available:

```bash
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
```

Acceptance:

- [ ] Required tests are green, or unrelated pre-existing failures are documented with evidence.

Latest Phase 3 Blazor/client verification - 2026-07-04:

- [x] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventListRegistrationWorkflowTests/*|/*/*/EventRegistrationServiceTests/*|/*/*/EventRegistrationTests/*|/*/*/EventDetailsSidebarTests/*" --minimum-expected-tests 1 --no-progress` passed: 10/10.
- [x] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventListTests/*" --minimum-expected-tests 1 --no-progress` passed: 27/27.
- [x] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --no-progress` passed: 1468/1469, 1 known skip.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress` passed: 242/243, 1 known skip.
- [x] `dotnet build --configuration Release --verbosity quiet` passed: 26 projects, 0 errors, 27 warnings.
- [ ] Browser visual QA for registration remains open until an Aspire/Compose runtime with an authenticated, seeded registration page is available.

Latest Phase 1 FullLocal runtime verification - 2026-07-04:

- [x] `dotnet build Explore.AppHost/Explore.AppHost.csproj --configuration Debug --verbosity quiet` passed with 0 errors after AppHost/seed wiring changes.
- [x] `dotnet build Explore.AppHost/Explore.AppHost.csproj --configuration Release --verbosity quiet` passed with 0 errors after the final AppHost nullability cleanup.
- [x] `dotnet build --configuration Release --verbosity quiet` passed with 0 errors after the final AppHost changes; warnings remain existing analyzer/package backlog.
- [x] `git diff --check` passed for the MVP launch docs, AppHost/seeder changes, operations/config/troubleshooting docs, journal entry, and local debug journal; trailing-whitespace scan returned no matches.
- [x] Foreground `aspire run --apphost Explore.AppHost/Explore.AppHost.csproj --isolated` stayed alive; `aspire wait explore-api --status healthy` and `aspire wait explore-blazor --status healthy` both succeeded.
- [x] Foreground API `/alive` and `/health` returned HTTP 200 Healthy; API health included Healthy OIDC, SMTP, database, RabbitMQ email-dispatch topology, storage, webhook, Cerbos, and related checks.
- [x] Foreground Blazor `/alive` and `/health` returned HTTP 200 Healthy; Blazor health included Healthy cache, OIDC, database, and API readiness.
- [x] Foreground Keycloak metadata issuer matched the dynamic isolated endpoint.
- [x] Foreground PostgreSQL `system_settings` contained `email.smtp_port=42037`, matching the dynamic Mailpit SMTP endpoint.
- [x] `minio-bootstrap` exited with code 0 and logged bucket creation; `mc stat local/explore` confirmed the bucket exists in `us-east-1`.
- [x] Earlier detached `aspire start --apphost Explore.AppHost/Explore.AppHost.csproj --format Json --isolated` returned JSON, stayed listed in `aspire ps`, and reported API/Blazor Healthy through `aspire wait`.
- [x] Earlier detached API `/health` returned HTTP 200 Healthy with 18 checks; detached Blazor `/health` returned HTTP 200 Healthy with 7 checks.
- [x] Earlier detached PostgreSQL `system_settings` contained `email.smtp_port=45665`, matching that run's Mailpit SMTP endpoint.
- [x] Re-prove detached lifecycle after the later run returned JSON, lost AppHost registration, and left `aspire ps --format Json` empty. Result: reproduced after the control-plane compile fix with `appHostPid=3623272`; logs reached readiness, but `aspire ps` remained empty and both AppHost/CLI PIDs were gone.
- [x] Fix the control-plane host `InteractiveServer` compile blocker found during detached AppHost startup. Result: `Event.ControlPlane.Blazor/Components/App.razor` now imports `Microsoft.AspNetCore.Components.Web.RenderMode` directly, and the focused control-plane Release build passed with 0 errors.
- [x] Reclassify detached Aspire lifecycle as a local CLI/tooling caveat and update plan/context/operations/troubleshooting/journal guidance so foreground `aspire run --isolated` remains the launch proof path.
- [x] `GET http://localhost:34857/api/event?pageSize=6` returned six seeded public events after seed schedule/status repair.
- [x] `GET http://localhost:34857/api/event/018e4e5c-7f00-7000-8000-000000000061/calendar` returned HTTP 200 `text/calendar` with VCALENDAR content.
- [x] `GET http://localhost:41777/events/018e4e5c-7f00-7000-8000-000000000061` returned HTTP 200 with title, canonical/Open Graph/Twitter metadata, and security headers.
- [x] API `GET /sitemap.xml`, Blazor `GET /robots.txt`, `/Error`, `favicon.ico`, and `/image/Icon_landingpage.png` returned expected content/status under the FullLocal runtime.
- [x] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/DatabaseSeederTests/SeedAsync_InDevelopment_RepairsLaunchCatalogDiscoveryFieldsAcrossStartups" --minimum-expected-tests 1 --no-progress` passed: 1/1.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/GetEventCalendarExportRequestHandlerTests/*" --minimum-expected-tests 1 --no-progress` passed: 4/4.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventControllerCalendarTests/*" --minimum-expected-tests 1 --no-progress` passed: 2/2.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IcalNetEventCalendarFileBuilderTests/*" --minimum-expected-tests 1 --no-progress` passed: 1/1.
- [x] Final runtime cleanup: foreground AppHost stopped and `aspire ps --format Json` returned `[]`.
- [x] Stopped the detached AppHost and cleaned run-owned orphaned DCP/API/Blazor processes after proof.
- [x] Earlier Phase 1 `dotnet build --configuration Release --verbosity quiet` passed: 28 projects, 0 errors, 8,816 existing warning backlog entries.
- [x] Earlier Phase 1 `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress` passed: 255 total, 254 succeeded, 1 known skip.
- [x] Earlier Phase 1 `git diff --check`, direct trailing-whitespace scan, and final `aspire ps --format Json` cleanup check passed.

Latest Phase 2.5 registration Mailpit verification - 2026-07-04:

- [x] `dotnet build Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet` passed: 15 projects, 0 errors.
- [x] `dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/RegistrationFlowTests/*" --minimum-expected-tests 1 --no-progress` passed: 1/1 in 1m 31s.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress` passed after docs update: 258 total, 257 succeeded, 1 known skip.
- [x] `git diff --check` and direct trailing-whitespace scan passed for the touched E2E fixture/test files and MVP launch docs.
- [ ] Latest full solution `dotnet build --configuration Release --verbosity quiet` did not pass because of unrelated dirty-worktree issues outside this slice: `Explore.Blazor.Client.Tests` generated-client anonymous HAL type mismatches and an `Explore.API` static-web-assets cache file lock.

### 6.3 Release evidence pack

- [ ] Capture runtime proof for registration success.
- [ ] Capture runtime proof for waitlist/duplicate behavior.
- [x] Capture Mailpit proof for delivered email and unsubscribe headers/links.
- [ ] Capture health normal/degraded results.
- [x] Capture calendar/sitemap/robots public endpoint responses.
- [ ] Capture SEO/social metadata output.
- [ ] Capture accessibility/visual QA results.
- [ ] Capture operator actions for email pause/park/replay if changed.

Acceptance:

- [ ] Release owner can verify the MVP without reading agent chat history.

### 6.4 Docs and journal

- [ ] Update `docs/API.md` for API behavior changes.
- [ ] Update `docs/BLAZOR.md` for BFF/UI/generated-client behavior changes.
- [ ] Update `docs/OPERATIONS.md` for runtime, health, email dispatch, or deployment changes.
- [ ] Update `docs/SECURITY-MODEL.md` for setup, privacy, unsubscribe, BFF, or audit behavior changes.
- [ ] Add durable findings to `dev/_journal/journal.md` for non-obvious implementation or verification discoveries.

Acceptance:

- [ ] Docs describe actual behavior and operator steps, not planned behavior.

## Deferred Backlog

These are intentionally not in the default MVP launch path:

- [ ] Full offline PWA/service worker behavior.
- [ ] RSS/Atom feeds.
- [ ] Calendar subscription feeds beyond current event `.ics` download.
- [ ] Public external API-key rollout beyond already required security/health checks.
- [ ] Major navigation/sidebar redesign.
- [ ] Generic outbox rewrite for email.
- [ ] Advanced autosave/undo for every admin form.
- [ ] Enterprise SSO, external secret manager, or multi-node orchestration beyond self-hostable MVP proof.

## Completion Checklist

- [ ] Phase 0 owner review complete.
- [ ] Phase 1 runtime proof complete. Foreground startup/health/public endpoint smoke, bounded SMTP outage readiness, registration-driven Mailpit proof, focused BFF Data Protection cookie restart proof, Data Protection key-store failure health/log visibility, and email-dispatch backlog/dead-letter degraded-health behavior are green; detached lifecycle is documented as local CLI/tooling caveat; full browser/OIDC restart proof remains open.
- [ ] Phase 2 email compliance complete.
- [x] Phase 3 registration integrity complete.
- [ ] Phase 4 security/audit/HAL cleanup complete.
- [ ] Phase 5 public polish complete.
- [ ] Phase 6 contracts/tests/docs/evidence complete.
- [ ] `mvp-launch-context.md` updated with final evidence.
- [ ] `dev/_journal/journal.md` updated for durable findings.
- [ ] Final implementation summary teaches the architecture and concrete flow changed.
