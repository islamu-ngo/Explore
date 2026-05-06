ABOUTME: Task checklist for the repository documentation upgrade implementation.
ABOUTME: Tracks phased Markdown docs, automation, operator runbooks, contributor templates, and verification.

# Repository Documentation Upgrade Tasks

Last Updated: 2026-05-06

## Phase 0: Baseline Verification and Guardrails

- [x] Run baseline architecture docs/context tests.
  - Command:
    ```bash
    dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
    ```
  - Acceptance:
    - [x] Result recorded in `context.md`.
    - [x] Failures are either fixed or explicitly listed as blockers.

- [x] Search current docs for placeholder/stale markers.
  - Check:
    - `{DATE}`
    - `{CONTACT_EMAIL}`
    - `{CONTACT_URL}`
    - `TBD`
    - `TODO`
    - `coming soon`
  - Acceptance:
    - [x] Findings listed or converted into tasks.
    - [x] `docs/ACCESSIBILITY_ARTIFACTS.md` placeholder status is explicitly handled.

- [x] Confirm hosted docs website is out of scope.
  - Acceptance:
    - [x] `plan.md` states no docs generator in this phase.
    - [x] `docs/docs-website/` is not expanded as part of this implementation.
    - [x] Optional note added later if needed.

- [x] Verify stale `/docs-lint` or TUnit command documentation.
  - Acceptance:
    - [x] Unsupported `dotnet test --filter` guidance is identified.
    - [x] Replacement command is planned in Phase 2.

---

## Phase 1: Documentation Architecture and Metadata

- [x] Create `docs/DOCUMENTATION_ARCHITECTURE.md`.
  - Acceptance:
    - [x] Includes Diátaxis-style doc intents.
    - [x] Defines evaluator, operator, admin, integrator, contributor, and AI-agent audience paths.
    - [x] Defines canonical docs.
    - [x] Defines owner categories.
    - [x] Defines metadata schema.
    - [x] Defines source-anchor policy.
    - [x] Defines implemented vs planned labeling policy.
    - [x] States hosted public docs are deferred.

- [x] Update `docs/DOCUMENTATION_STYLE_GUIDE.md`.
  - Acceptance:
    - [x] Requires metadata for new canonical docs.
    - [x] Requires source anchors for drift-prone claims.
    - [x] Adds docs impact rule.
    - [x] Adds release documentation contract summary.
    - [x] Warns against duplicating canonical config/reference tables.
    - [x] Keeps style guide concise.

- [x] Update `docs/index.md`.
  - Acceptance:
    - [x] Has clear audience paths:
      - [x] Evaluators
      - [x] Operators
      - [x] Admins
      - [x] Integrators
      - [x] Contributors
      - [x] AI agents
    - [x] Links all new canonical docs as they are created.
    - [x] Does not become a giant unstructured list.

- [x] Update `docs/DOCUMENTATION_SYNTHESIS.md` if needed.
  - Acceptance:
    - [x] Reflects repo Markdown-first decision.
    - [x] Reflects automation-earlier correction.
    - [x] Reflects release docs contract.

---

## Phase 2: Early Documentation Automation

- [x] Add placeholder/staleness validation.
  - Candidate checks:
    - `{DATE}`
    - `{CONTACT_EMAIL}`
    - `{CONTACT_URL}`
    - accidental `TBD`
    - accidental `TODO`
    - unapproved `coming soon`
  - Acceptance:
    - [x] Check can run locally.
    - [x] Check is documented.
    - [x] Existing intentional placeholders are either removed or allowlisted with reason.

- [x] Add or update markdown relative-link validation.
  - Acceptance:
    - [x] Broken relative docs links fail validation or are covered by existing architecture tests.
    - [x] The documented command matches actual repo behavior.

- [x] Add metadata validation in migration-safe mode.
  - Acceptance:
    - [x] New canonical docs require metadata.
    - [x] Existing docs can be migrated gradually.
    - [x] Missing metadata does not create noisy failures for every legacy page unless intentionally enabled.

- [x] Fix stale `/docs-lint` instructions.
  - Files to inspect:
    - command docs for `/docs-lint`
    - `docs/TESTING.md`
    - `docs/OPERATIONS.md`
    - `docs/CONTRIBUTING.md`
  - Acceptance:
    - [x] No docs recommend unsupported TUnit `--filter` examples.
    - [x] Full architecture test fallback is documented.
    - [x] Local docs validation command exits 0.

- [x] Add docs quality workflow.
  - File:
    - `.github/workflows/docs-quality.yml` or update existing docs/context workflow.
  - Acceptance:
    - [x] Docs-only PRs get fast feedback.
    - [x] Placeholder checks run.
    - [x] Link checks run.
    - [x] Metadata checks run in migration-safe mode.
    - [x] Workflow is documented in `docs/CONTRIBUTING.md` and `docs/TESTING.md`.

---

## Phase 3: Operator-Critical Documentation

- [x] Rewrite `docs/SELF_HOSTING.md`.
  - Source anchors:
    - `docker-compose.yml`
    - `Explore.AppHost/`
    - `Event.MigrationService/`
    - `docs/CONFIGURATION.md`
    - `docs/SECRETS.md`
    - `docs/DEPLOYMENT_MODES.md`
    - `docs/DEPLOYMENT_TIERS.md`
  - Acceptance:
    - [x] Docker Compose path matches real compose services and environment keys.
    - [x] Aspire path is clearly separate from self-hosted production path.
    - [x] Required vs optional services are explicit.
    - [x] Setup secret expectations are explicit.
    - [x] Keycloak expectations are explicit.
    - [x] PostgreSQL expectations are explicit.
    - [x] Object storage expectations are explicit.
    - [x] Cerbos/local authorization expectations are explicit.
    - [x] Local endpoint expectations are accurate.
    - [x] Production TLS/reverse proxy boundary is clear.
    - [x] Metadata and source anchors are present.

- [x] Create `docs/BACKUP_RESTORE_UPGRADE.md`.
  - Acceptance:
    - [x] Includes PostgreSQL backup.
    - [x] Includes PostgreSQL restore.
    - [x] Includes object storage backup/restore.
    - [x] Includes Keycloak realm/config backup boundary.
    - [x] Includes secrets/config backup boundary.
    - [x] Includes restore validation steps.
    - [x] Includes staging/dry-run upgrade guidance.
    - [x] Includes rollback decision tree.
    - [x] Warns about data loss risk.
    - [x] Warns about auth lockout risk.
    - [x] Warns about schema migration rollback risk.
    - [x] States what has and has not been manually verified.

- [x] Create `docs/RELEASE_CHECKLIST.md`.
  - Acceptance:
    - [x] Requires migration notes.
    - [x] Requires config/env changes.
    - [x] Requires breaking changes.
    - [x] Requires upgrade path.
    - [x] Requires rollback path.
    - [x] Requires backup compatibility review.
    - [x] Requires security/auth changes review.
    - [x] Requires docs impact statement.
    - [x] Links to `BACKUP_RESTORE_UPGRADE.md`.
    - [x] Links to `CONTRIBUTING.md`.

- [x] Split `docs/OPERATIONS.md` into reference plus links to runbooks.
  - Acceptance:
    - [x] `OPERATIONS.md` is shorter.
    - [x] Task procedures link to dedicated runbooks.
    - [x] Planned-only content is isolated or removed.
    - [x] Troubleshooting links to exact runbooks.

- [x] Update `docs/TROUBLESHOOTING.md`.
  - Acceptance:
    - [x] Symptom-first structure preserved.
    - [x] Links to self-hosting, backup/restore, config, security, and operations docs.
    - [x] Does not duplicate full runbook procedures.

- [x] Update `docs/CONFIGURATION.md` only where runtime mismatch is found.
  - Acceptance:
    - [x] Runtime keys match compose/AppHost/source anchors.
    - [x] Secrets are referenced through `docs/SECRETS.md`, not duplicated excessively.

---

## Phase 4: Admin and Integrator Documentation

- [x] Create `docs/ADMIN_GUIDE.md`.
  - Source anchors:
    - `Explore.Blazor.Client/Pages/Admin/`
    - `docs/ADMIN_HIERARCHY.md`
    - `docs/AUTHORIZATION.md`
    - `docs/AUTHORIZATION_PATTERNS.md`
  - Acceptance:
    - [x] Includes metadata.
    - [x] Documents instance admin workflows.
    - [x] Documents tenant admin workflows.
    - [x] Documents organization/group admin workflows.
    - [x] Documents template administration.
    - [x] Documents storage administration.
    - [x] Documents email/SMTP administration.
    - [x] Documents localization administration.
    - [x] Documents custom properties administration.
    - [x] Documents analytics administration if implemented.
    - [x] Documents SEO/public discovery administration if implemented.
    - [x] Each workflow states required role/permission.
    - [x] Each workflow states UI entry point.
    - [x] Dangerous operations include recovery notes.

- [x] Create `docs/API_COOKBOOK.md`.
  - Source anchors:
    - `docs/API.md`
    - `Explore.API/Controllers/`
  - Acceptance:
    - [x] Includes metadata.
    - [x] Examples are task-first.
    - [x] Covers authentication.
    - [x] Covers tenant context.
    - [x] Covers HAL links.
    - [x] Covers pagination.
    - [x] Covers error shape.
    - [x] Covers idempotency/retry guidance where applicable.
    - [x] Links to generated API/OpenAPI/Scalar instructions.
    - [x] Does not duplicate every endpoint.

- [x] Update `docs/API.md`.
  - Acceptance:
    - [x] Links to `API_COOKBOOK.md`.
    - [x] Keeps canonical API conventions.
    - [x] Removes duplicate headings if present.
    - [x] Keeps generated reference as the endpoint source of truth.

---

## Phase 5: Feature Documentation Batch A — Platform Services

- [x] Create `docs/STORAGE.md`.
  - Source anchors:
    - `Explore.Infrastructure/Storage/`
    - `Explore.API/Controllers/StorageObjectController.cs`
    - `docs/CONFIGURATION.md`
    - `docs/SECRETS.md`
  - Acceptance:
    - [x] Includes metadata.
    - [x] Documents storage configuration.
    - [x] Documents upload/download flow.
    - [x] Documents API surface at high level.
    - [x] Documents authorization boundary.
    - [x] Documents backup/restore impact.
    - [x] Links to troubleshooting.

- [x] Create `docs/EMAIL_NOTIFICATIONS.md`.
  - Source anchors:
    - `Explore.Infrastructure/Mail/`
    - `Explore.Domain/Settings/Definitions/EmailSettingDefinitions.cs`
    - `docs/CONFIGURATION.md`
    - `docs/SECRETS.md`
  - Acceptance:
    - [x] Includes metadata.
    - [x] Documents SMTP settings.
    - [x] Documents secret handling.
    - [x] Documents mail sending boundary.
    - [x] Documents notification/email relationship.
    - [x] Documents troubleshooting.
    - [x] Does not claim unsupported unsubscribe behavior unless verified.

---

## Phase 5: Feature Documentation Batch B — Admin Workflows

- [x] Create `docs/TEMPLATE_SYNC.md`.
  - Source anchors:
    - `Explore.Blazor.Client/Pages/Admin/EventTemplateSync/`
    - `Explore.Blazor.Client/Pages/Admin/EventSessionTemplateSync/`
  - Acceptance:
    - [x] Includes metadata.
    - [x] Documents event template sync.
    - [x] Documents session template sync.
    - [x] Documents admin UI path.
    - [x] Documents required permissions.
    - [x] Documents dangerous operation/recovery notes.

- [x] Create `docs/CONTACT_SHARING.md`.
  - Source anchors:
    - `Explore.Application/Features/ContactShareConsents/`
    - `Explore.API/Controllers/ContactShareConsentController.cs`
  - Acceptance:
    - [x] Includes metadata.
    - [x] Documents consent model.
    - [x] Documents API surface at high level.
    - [x] Documents privacy boundaries.
    - [x] Documents authorization boundaries.
    - [x] Documents export/sharing behavior only if verified.

---

## Phase 5: Feature Documentation Batch C — UX and Discovery

- [x] Create `docs/NOTIFICATIONS.md`.
  - Source anchors:
    - `Explore.Application/Features/Notifications/`
    - `Explore.Blazor.Client/Layout/NotificationBell.razor`
  - Acceptance:
    - [x] Includes metadata.
    - [x] Documents notification lifecycle.
    - [x] Documents UI behavior.
    - [x] Documents user/admin boundaries.
    - [x] Documents read/unread behavior if implemented.
    - [x] Links to email notification doc where relevant.

- [x] Create `docs/SEO.md`.
  - Source anchors:
    - `Explore.API/Controllers/SitemapController.cs`
    - `Explore.Blazor/Controllers/RobotsController.cs`
    - `docs/RENDER_POLICIES.md`
  - Acceptance:
    - [x] Includes metadata.
    - [x] Documents sitemap behavior.
    - [x] Documents robots behavior.
    - [x] Documents public route/render policy implications.
    - [x] Separates implemented SEO from planned SEO.

---

## Phase 5: Feature Documentation Batch D — Engineering Evidence

- [x] Create `docs/BENCHMARKS.md`.
  - Source anchors:
    - `Event.Benchmarks/`
  - Acceptance:
    - [x] Includes metadata.
    - [x] Documents benchmark project purpose.
    - [x] Documents how to run benchmarks.
    - [x] Documents how to interpret results.
    - [x] Documents what benchmarks do not prove.
    - [x] Links to performance/operations docs if relevant.

---

## Phase 6: Contributor and Agent Workflow

- [x] Add `.github/ISSUE_TEMPLATE/bug_report.yml`.
  - Acceptance:
    - [x] Requests reproduction steps.
    - [x] Requests expected/actual behavior.
    - [x] Requests affected version/branch.
    - [x] Requests logs/screenshots where relevant.
    - [x] Requests affected docs/code paths if known.

- [x] Add `.github/ISSUE_TEMPLATE/feature_request.yml`.
  - Acceptance:
    - [x] Requests user problem.
    - [x] Requests proposed behavior.
    - [x] Requests non-goals.
    - [x] Requests affected docs/code paths.
    - [x] Requests self-hosting/operator impact if relevant.

- [x] Add `.github/ISSUE_TEMPLATE/documentation.yml`.
  - Acceptance:
    - [x] Requests stale/incorrect doc path.
    - [x] Requests expected correction.
    - [x] Requests source anchor if known.
    - [x] Requests whether issue affects operators, admins, contributors, or agents.

- [x] Add `.github/ISSUE_TEMPLATE/ai_agent_task.yml`.
  - Acceptance:
    - [x] Requests task context.
    - [x] Requests files likely in scope.
    - [x] Requests required docs/rules.
    - [x] Requests validation expectations.
    - [x] Requests handoff expectations.

- [x] Add `.github/PULL_REQUEST_TEMPLATE.md`.
  - Acceptance:
    - [x] Requires summary.
    - [x] Requires docs impact:
      - [x] Updated
      - [x] Not needed
      - [x] Deferred with reason
    - [x] Requires tests/validation run.
    - [x] Requires screenshots for UI changes.
    - [x] Requires migration/config notes if relevant.
    - [x] Requires release checklist link for release-impacting changes.
    - [x] Requires agent-context update if applicable.

- [x] Create `docs/FIRST_CONTRIBUTION.md`.
  - Acceptance:
    - [x] Includes metadata.
    - [x] Provides short docs-only PR path.
    - [x] Provides short small-bug PR path.
    - [x] Links to `CONTRIBUTING.md`.
    - [x] Links to `TESTING.md`.
    - [x] Includes exact verification commands.
    - [x] Does not duplicate all governance rules.

- [x] Create `dev/HANDOFF_TEMPLATE.md`.
  - Acceptance:
    - [x] Includes current state.
    - [x] Includes next action.
    - [x] Includes blockers.
    - [x] Includes modified files.
    - [x] Includes validation.
    - [x] Includes risks.
    - [x] Short enough for frequent use.

- [x] Update `dev/active/README.md`.
  - Acceptance:
    - [x] Explains when to create/update handoff.
    - [x] Links `dev/HANDOFF_TEMPLATE.md`.

- [x] Update `docs/CONTRIBUTING.md`.
  - Acceptance:
    - [x] References issue templates.
    - [x] References PR template.
    - [x] References first contribution guide.
    - [x] References docs validation commands.
    - [x] States docs impact requirement.

---

## Phase 7: Existing Docs Cleanup

- [x] Update `README.md`.
  - Acceptance:
    - [x] Concise product positioning.
    - [x] Accurate maturity/breaking-change statement.
    - [x] Links canonical repo docs.
    - [x] No stale badges or broken links.
    - [x] Does not become a full docs page.

- [x] Update `docs/GETTING_STARTED.md`.
  - Acceptance:
    - [x] Short runnable path.
    - [x] Commands verified.
    - [x] Links deeper docs instead of duplicating them.

- [x] Update `docs/API.md`.
  - Acceptance:
    - [x] Canonical API conventions remain.
    - [x] Links `API_COOKBOOK.md`.
    - [x] Generated reference instructions are accurate.
    - [x] Duplicate sections removed.

- [x] Update `docs/BLAZOR.md`.
  - Acceptance:
    - [x] Contributor-useful service/state/render guidance.
    - [x] BFF security boundaries are clear.
    - [x] Does not duplicate all design/accessibility docs.

- [ ] Update `docs/CONFIGURATION.md`.
  - Acceptance:
    - [ ] Runtime keys match source anchors.
    - [ ] Environment variable examples are accurate.
    - [ ] Secret values are not exposed.
    - [ ] Links `SECRETS.md`.

- [ ] Update `docs/SECURITY.md`.
  - Acceptance:
    - [ ] Implemented security behavior clearly separated from planned work.
    - [ ] Keycloak, BFF, Cerbos/local fallback boundaries are clear.
    - [ ] Links authorization docs.

- [ ] Update `docs/FEDERATION.md`.
  - Acceptance:
    - [ ] Implemented foundation vs roadmap is unambiguous.
    - [ ] No protocol support is overstated.
    - [ ] Links lexicon/outbox docs where relevant.

- [ ] Update `docs/ACCESSIBILITY_ARTIFACTS.md`.
  - Acceptance:
    - [ ] No accidental `{DATE}` placeholders remain.
    - [ ] Contact placeholders removed or explicitly labeled as unreleased template.
    - [ ] Test evidence is either current or marked as template.
    - [ ] Release gate checklist remains useful.

- [ ] Update `docs/OPERATIONS.md`.
  - Acceptance:
    - [ ] Reference-focused.
    - [ ] Links runbooks.
    - [ ] Removes AI-agent operational content if better owned elsewhere, or clearly separates it.

- [ ] Update `docs/TROUBLESHOOTING.md`.
  - Acceptance:
    - [ ] Symptom-first.
    - [ ] Links exact runbooks.
    - [ ] Avoids duplicated long procedures.

---

## Phase 8: Future Public Docs Preparation

- [ ] Add optional deferral note for `docs/docs-website/`.
  - Acceptance:
    - [ ] States public docs hosting is deferred.
    - [ ] States repo docs are current source of truth.
    - [ ] Does not introduce generator config.

- [ ] Optionally create `docs/PUBLIC_DOCS_ROADMAP.md`.
  - Acceptance:
    - [ ] Lists future hosted docs candidates.
    - [ ] Does not duplicate current repo docs.
    - [ ] Keeps public site work separate from current implementation.

---

## Final Verification

- [ ] Run architecture docs/context tests.
  ```bash
  dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
  ```

- [ ] Run build if needed.
  ```bash
  dotnet build --configuration Release --verbosity quiet
  ```

- [ ] Run docs quality workflow locally if script exists.
  - Acceptance:
    - [ ] Placeholder checks pass.
    - [ ] Relative links pass.
    - [ ] Metadata checks pass or produce only accepted migration warnings.

- [ ] Validate self-hosting docs against source.
  - Acceptance:
    - [ ] `docker-compose.yml` service names match docs.
    - [ ] AppHost path matches docs.
    - [ ] Environment keys match docs.
    - [ ] Setup secret docs match runtime behavior.

- [ ] Validate operator runbooks honestly.
  - Acceptance:
    - [ ] Backup instructions are source-grounded.
    - [ ] Restore validation steps are included.
    - [ ] Any untested restore/rollback steps are labeled as not yet manually verified.

- [ ] Verify all new docs have metadata.
  - Acceptance:
    - [ ] Audience present.
    - [ ] Status present.
    - [ ] Owner present.
    - [ ] Last verified present.
    - [ ] Source anchors present.

- [ ] Verify `docs/index.md` links all new canonical docs.
  - Acceptance:
    - [ ] No orphan canonical docs.
    - [ ] Audience paths remain clear.

## Quick Resume

Next recommended task:

1. Start Phase 7 existing docs cleanup with `README.md`.
2. Keep README concise: product positioning, maturity/breaking-change statement, links to canonical docs, no full docs-page duplication.
3. Then update `docs/GETTING_STARTED.md` with a short runnable path and verified commands.
4. After each cleanup slice, update this tracker and run focused documentation quality checks.

Operator-critical docs, the release checklist, Phase 4 admin/integrator docs, Phase 5 feature docs, and Phase 6 contributor workflow templates now exist. Continue remaining docs cleanup incrementally so each slice remains reviewable.
