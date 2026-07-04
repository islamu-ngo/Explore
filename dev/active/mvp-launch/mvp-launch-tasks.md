<!-- ABOUTME: Executable task checklist for the MVP launch closure workstream. -->
<!-- ABOUTME: Orders implementation around runtime proof, email compliance, registration integrity, security, public polish, and release evidence. -->

# MVP Launch Tasks

Last Updated: 2026-07-04 Europe/Brussels

Status: In implementation. Phase 2 email dispatch compliance has a completed source/test/docs slice; Phase 3 registration integrity has completed source/test/docs coverage for capacity, event/day/session duplicate intent handling, duplicate email-dispatch-row prevention, API boundary idempotent-create behavior, real PostgreSQL API repeat-submit behavior, waitlist behavior, unauthenticated create rejection, generated-client response agreement, Blazor service safe error mapping, registration UI outcome state/copy, and HAL-gated event-detail registration affordances. Phase 1 runtime proof is blocked by 2026-07-04 full-local Aspire attempts where infrastructure started but AppHost lifecycle was not stable: one attempt showed `explore-api` exiting, and a later controlled run showed API/Blazor listening temporarily before the `13.3.0-preview` Aspire CLI timed out waiting for the AppHost backchannel while the repo pins Aspire `13.4.6`. Runtime Mailpit proof and broader launch closure remain open.

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
- [ ] Align the Aspire CLI/AppHost toolchain before treating the current evidence as an application startup defect. The repo pins Aspire `13.4.6`; the installed CLI is `13.3.0-preview.1.26221.24`. Update the CLI or run a foreground AppHost control with `dotnet run --project Explore.AppHost/Explore.AppHost.csproj`.
- [ ] Repeat `aspire start --apphost Explore.AppHost/Explore.AppHost.csproj --format Json --non-interactive --isolated` after CLI alignment or foreground-control evidence.
- [ ] Capture parent log, child log, `aspire ps --format Json`, process URLs, and API/Blazor resource output for the aligned rerun.
- [ ] If `explore-api` still changes `Running -> Finished` after CLI alignment or foreground control, diagnose that symptom directly from API resource output; the CLI child log did not expose an application exception in the first attempt.
- [ ] Confirm `aspire ps --format Json` lists a stable AppHost.
- [ ] Confirm PostgreSQL starts and migrations run.
- [ ] Confirm TickerQ storage/schema is available.
- [ ] Confirm Mailpit or local SMTP capture is available for email proof. Under `--isolated`, discover Mailpit endpoints from Aspire/Docker instead of assuming fixed host ports.
- [ ] Confirm optional dependencies have documented fallback behavior.

Acceptance:

- [ ] A fresh developer/operator can reproduce the startup path from docs.
- [ ] No required launch dependency is undocumented.
- [ ] API and Blazor remain running long enough to smoke health and public URLs.

Validation evidence:

- [ ] Startup command and URL(s).
- [ ] Relevant logs showing API, Blazor, PostgreSQL, TickerQ, and Mailpit readiness.
- [ ] CLI/AppHost backchannel result, and API early-exit root cause or explicit operator recovery note if that symptom still reproduces.

### 1.2 Data Protection persistence proof

- [ ] Log in through the BFF flow.
- [ ] Restart the app while preserving the database.
- [ ] Confirm persisted Data Protection keys allow auth/session continuity as expected.
- [ ] Confirm migration service covers `DataProtectionKeyContext`.

Acceptance:

- [ ] Cookie/session behavior is documented after restart.
- [ ] Data Protection key storage failures surface in health/logs.

### 1.3 Health and degraded dependency proof

- [ ] Smoke liveness and readiness endpoints.
- [ ] Confirm email-dispatch health in normal state.
- [ ] Confirm health behavior when SMTP/Mailpit is unavailable.
- [ ] Confirm behavior when email dispatch backlog contains stale processing, retry, or dead-letter rows.
- [ ] Confirm idempotency cleanup, storage, and queue/TickerQ health are represented in operations docs.

Acceptance:

- [ ] Health results are actionable and not misleading.
- [ ] Operators can identify the failing dependency and next step.

### 1.4 Public endpoint smoke

- [ ] Smoke public event detail route.
- [ ] Smoke calendar `.ics` endpoint and content type.
- [ ] Smoke `sitemap.xml`.
- [ ] Smoke `robots.txt`.
- [ ] Smoke branded error pages.
- [ ] Smoke static assets referenced by event pages under current CSP/security headers.

Acceptance:

- [ ] Public launch URLs work without authentication where intended.
- [ ] CSP/security headers do not break launch-critical assets.

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
- [ ] Test stale processing recovery moves abandoned rows to unknown and updates receipts.
- [ ] Test retry/dead-letter behavior with a failing provider.
- [ ] Test receipt idempotency prevents duplicate provider sends for the same publish event.

Acceptance:

- [ ] Email dispatch can be operated without database surgery.

### 2.5 Runtime email proof

- [ ] Register for an event through the real API/BFF path.
- [ ] Confirm one `EmailDispatchOutbox` row is created in the registration transaction.
- [ ] Trigger or wait for the TickerQ/hosted drain.
- [ ] Confirm one Mailpit message is delivered when preferences allow.
- [ ] Confirm subject, recipient, body, custom headers, and unsubscribe behavior.

Acceptance:

- [ ] Registration confirmation and at least one preference-controlled lifecycle email are proven through the real dispatch path.

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

- [ ] Verify setup metadata endpoint rate limiting and 429 response behavior.
- [ ] Verify setup secret is stripped server-side and never exposed to the browser.
- [ ] Verify proxied writes require antiforgery.
- [ ] Verify access/refresh tokens remain server-side.
- [ ] Add regression tests where coverage is missing.

Acceptance:

- [ ] The BFF boundary matches `docs/SECURITY-MODEL.md` and `blazor-bff-patterns`.

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

### 6.3 Release evidence pack

- [ ] Capture runtime proof for registration success.
- [ ] Capture runtime proof for waitlist/duplicate behavior.
- [ ] Capture Mailpit proof for delivered email and unsubscribe headers/links.
- [ ] Capture health normal/degraded results.
- [ ] Capture calendar/sitemap/robots public endpoint responses.
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
- [ ] Phase 1 runtime proof complete.
- [ ] Phase 2 email compliance complete.
- [x] Phase 3 registration integrity complete.
- [ ] Phase 4 security/audit/HAL cleanup complete.
- [ ] Phase 5 public polish complete.
- [ ] Phase 6 contracts/tests/docs/evidence complete.
- [ ] `mvp-launch-context.md` updated with final evidence.
- [ ] `dev/_journal/journal.md` updated for durable findings.
- [ ] Final implementation summary teaches the architecture and concrete flow changed.
