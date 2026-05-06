ABOUTME: Implementation plan for upgrading repository documentation to enterprise self-hostable quality.
ABOUTME: Focuses on repo Markdown, operator safety, release docs, ownership, and enforceable quality gates.

# Repository Documentation Upgrade Plan

Last Updated: 2026-05-06

## Executive Summary

The repository already has unusually strong technical documentation for senior contributors and AI agents. The remaining weakness is not lack of documentation volume; it is imbalance.

The docs are stronger for people already inside the codebase than for:

- evaluators,
- self-hosting operators,
- instance administrators,
- first-time contributors,
- integrators,
- release managers.

This plan upgrades documentation as a repository-native product surface. It intentionally does **not** introduce a hosted documentation website or docs-site generator yet. Public website work is deferred until the repository documentation is accurate, structured, and maintainable.

The priority is operator trust: install, configure, back up, restore, upgrade, roll back, troubleshoot, and safely contribute.

## Strategic Decision

For this implementation cycle:

- All docs are Markdown files in the repo.
- Do not add MkDocs, Docusaurus, VitePress, or another generator.
- Do not invest in `docs/docs-website/` beyond a short deferral note if needed.
- Treat source files, configuration files, tests, and workflows as the authority.
- Every implemented behavior claim must be traceable to source anchors.
- Every release-impacting change must include docs impact review.

## Problem Statement

The project is self-hostable and enterprise-grade in ambition, but self-hostable users judge trust by operational documentation.

A strong README and architecture docs are not enough. Operators need exact runtime expectations, disaster recovery steps, migration and rollback guidance, security boundaries, and clear support expectations.

The current documentation system needs:

1. Clear information architecture.
2. Source-grounded metadata.
3. Faster docs quality automation.
4. Accurate operator runbooks.
5. Release documentation discipline.
6. Smaller, owned docs domains.
7. Reduced drift between docs, source, compose, AppHost, and workflows.

## In Scope

- Repository Markdown documentation under `docs/`.
- Documentation architecture and metadata policy.
- `docs/index.md` navigation cleanup.
- Operator-critical docs:
  - self-hosting,
  - backup,
  - restore,
  - upgrade,
  - rollback,
  - release checklist,
  - operations split.
- Admin and feature docs for implemented surfaces.
- Contributor docs and GitHub issue/PR templates.
- Agent handoff template.
- Docs validation commands and CI workflow.
- Cleanup of stale placeholders, duplicate content, and planned/implemented ambiguity.

## Out of Scope

- Hosted public documentation website.
- Docs site generator setup.
- Product feature implementation.
- EF Core schema changes.
- Cerbos policy changes.
- UI redesign.
- Marketing copy polishing before operator docs are correct.
- Full manual verification of every production deployment topology in one pass.

## Documentation Model

Use Diátaxis-style document intent:

| Intent | Purpose | Examples |
|---|---|---|
| Tutorial | Learn by doing | `GETTING_STARTED`, first contribution |
| How-to | Complete a task | `SELF_HOSTING`, `BACKUP_RESTORE_UPGRADE`, admin workflows |
| Reference | Exact facts and contracts | `API`, `CONFIGURATION`, `SECURITY`, `DEPLOYMENT_MODES` |
| Explanation | Rationale and tradeoffs | `ARCHITECTURE`, `DOMAIN`, `GOVERNANCE`, ADRs |

Rules:

1. Do not mix runbooks into broad reference docs unless short and unavoidable.
2. Do not duplicate canonical configuration tables in feature docs.
3. Do not claim roadmap behavior as implemented.
4. Prefer links to canonical docs over copied content.
5. Keep docs short enough that users can find answers without reading a book.

## Audience Paths

The updated docs index should support these paths:

| Audience | Entry Point | Needs |
|---|---|---|
| Evaluator | `README.md`, `docs/PROJECT.md`, `docs/GETTING_STARTED.md` | What is it, why use it, how to try it |
| Operator | `docs/SELF_HOSTING.md`, `docs/BACKUP_RESTORE_UPGRADE.md`, `docs/OPERATIONS.md` | Install, secure, operate, recover |
| Admin | `docs/ADMIN_GUIDE.md` | Configure tenants, orgs, templates, storage, email, SEO |
| Integrator | `docs/API.md`, `docs/API_COOKBOOK.md` | Auth, HAL, errors, pagination, examples |
| Contributor | `docs/FIRST_CONTRIBUTION.md`, `docs/CONTRIBUTING.md`, `docs/TESTING.md` | Make safe changes |
| AI Agent | `AGENTS.md`, `CLAUDE.md`, `.claude/contract/intents.yaml`, `dev/HANDOFF_TEMPLATE.md` | Route work and preserve context |

## Ownership Model

Each canonical doc gets one owner category:

| Owner Category | Owns |
|---|---|
| Platform/Ops | self-hosting, operations, backup/restore, deployment, release checklist |
| Security | auth, authorization, secrets, Keycloak, Cerbos, security boundaries |
| API | API reference, API cookbook, OpenAPI, HAL, errors |
| Frontend | Blazor, design system, render policies, accessibility |
| Product/Admin | admin guide and product feature docs |
| Contributor Experience | contributing, first contribution, templates, testing |
| Agent Context | AGENTS, CLAUDE, `.claude`, dev handoff docs |

Ownership should appear in metadata, but should also guide review responsibility.

## Required Metadata

New docs and canonical docs should include:

```markdown
> **Audience:** Operators
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-05-06
> **Source Anchors:** `docker-compose.yml`, `Explore.AppHost/`, `docs/CONFIGURATION.md`
```

Metadata rules:

- `Audience` can contain multiple values.
- `Status` must be one of:
  - `Implemented`
  - `Draft`
  - `Planned`
  - `Mixed`
- `Mixed` docs must clearly label implemented vs planned sections.
- `Last Verified` means source anchors were checked, not that prose was edited.
- `Source Anchors` must be real paths.

## Target Repository Docs Additions

Create or update:

```text
docs/
  DOCUMENTATION_ARCHITECTURE.md
  BACKUP_RESTORE_UPGRADE.md
  RELEASE_CHECKLIST.md
  ADMIN_GUIDE.md
  API_COOKBOOK.md
  STORAGE.md
  EMAIL_NOTIFICATIONS.md
  TEMPLATE_SYNC.md
  CONTACT_SHARING.md
  NOTIFICATIONS.md
  SEO.md
  BENCHMARKS.md
  FIRST_CONTRIBUTION.md

dev/
  HANDOFF_TEMPLATE.md

.github/
  ISSUE_TEMPLATE/
    bug_report.yml
    feature_request.yml
    documentation.yml
    ai_agent_task.yml
  PULL_REQUEST_TEMPLATE.md
```

Do not add a docs-site generator in this plan.

## Implementation Phases

## Phase 0: Baseline Verification and Guardrail Setup

### Goal

Start from known repo state and prevent obvious documentation regressions before broad rewrites.

### Work

1. Run or verify baseline architecture docs/context tests.
2. Search for placeholder tokens:
   - `{DATE}`
   - `{CONTACT_EMAIL}`
   - `{CONTACT_URL}`
   - `TBD`
   - `TODO`
   - `coming soon`
3. Identify docs that contain planned/roadmap language inside reference sections.
4. Record known stale docs-lint/TUnit command problems.
5. Confirm that `docs/docs-website/` is deferred for this cycle.

### Acceptance Criteria

- Baseline validation result is recorded in `context.md`.
- Placeholder/stale marker list is known before cleanup starts.
- `docs/docs-website/` is explicitly out of scope or marked deferred.
- No broad rewrite begins without source anchor verification.

### Effort

S

---

## Phase 1: Documentation Architecture and Metadata Policy

### Goal

Define the repository documentation system before creating more docs.

### Files

- New `docs/DOCUMENTATION_ARCHITECTURE.md`
- Update `docs/DOCUMENTATION_STYLE_GUIDE.md`
- Update `docs/index.md`
- Update `docs/DOCUMENTATION_SYNTHESIS.md` if needed

### Work

1. Create `DOCUMENTATION_ARCHITECTURE.md`.
2. Define:
   - audiences,
   - doc intents,
   - canonical docs,
   - ownership categories,
   - metadata schema,
   - source-anchor rules,
   - planned vs implemented labeling.
3. Update style guide with:
   - required metadata,
   - source anchor rules,
   - docs impact requirement,
   - release documentation contract summary.
4. Update docs index by audience path.
5. Add a short policy that hosted public docs are deferred.

### Acceptance Criteria

- Every canonical top-level doc has an intended owner category in the architecture map.
- `docs/index.md` has clear paths for evaluators, operators, admins, integrators, contributors, and AI agents.
- Style guide requires audience, status, owner, last verified, and source anchors for new docs.
- Public docs website generation is explicitly deferred.

### Effort

M

---

## Phase 2: Early Documentation Automation

### Goal

Move cheap validation earlier so the repo stops accepting obvious docs quality regressions.

### Files

- New or updated docs validation script/test.
- New or updated `.github/workflows/docs-quality.yml` or equivalent.
- Update `docs/TESTING.md`.
- Update `docs/CONTRIBUTING.md`.

### Work

1. Add placeholder/staleness check.
2. Add metadata check in warning or soft-fail mode for initial migration.
3. Add markdown relative-link check, reusing existing architecture tests where possible.
4. Fix stale `/docs-lint` documentation.
5. Document local docs validation commands.
6. Make docs-only PRs get fast feedback.

### Acceptance Criteria

- `{DATE}`, `{CONTACT_EMAIL}`, `{CONTACT_URL}`, accidental `TBD`, and unapproved `coming soon` are detected.
- Broken relative links fail or are clearly covered by existing architecture tests.
- Metadata check applies at least to new canonical docs.
- Docs no longer recommend unsupported TUnit `--filter` commands unless verified.
- Contributors can run docs checks locally.

### Effort

M

### Important Constraint

Do not make metadata enforcement too strict before migration. Start with warning/allowlist mode if necessary.

---

## Phase 3: Operator-Critical Documentation

### Goal

Make a self-hosting operator successful without reading source code.

### Files

- Rewrite `docs/SELF_HOSTING.md`
- New `docs/BACKUP_RESTORE_UPGRADE.md`
- New `docs/RELEASE_CHECKLIST.md`
- Update `docs/OPERATIONS.md`
- Update `docs/TROUBLESHOOTING.md`
- Update `docs/CONFIGURATION.md` if mismatches are found
- Update `docs/SECRETS.md` if needed

### Work

1. Reconcile `SELF_HOSTING.md` against:
   - `docker-compose.yml`,
   - `Explore.AppHost/`,
   - `Event.MigrationService/`,
   - `docs/CONFIGURATION.md`,
   - `docs/SECRETS.md`.
2. Separate Docker Compose and Aspire paths.
3. Document setup secret expectations.
4. Document Keycloak expectations.
5. Document PostgreSQL expectations.
6. Document object storage expectations.
7. Document Cerbos/local authorization provider expectations.
8. Create backup/restore/upgrade/rollback runbook.
9. Create release checklist with docs impact section.
10. Split `OPERATIONS.md` into reference plus links to task runbooks.

### Acceptance Criteria

- Self-hosting docs match real service names, ports, profiles, and environment keys.
- Operators can distinguish development Aspire from self-hosted Docker Compose.
- Backup runbook covers:
  - PostgreSQL,
  - object storage,
  - Keycloak realm/config,
  - secrets boundary,
  - config files/environment,
  - restore validation.
- Upgrade runbook covers:
  - pre-upgrade checks,
  - migration risk,
  - staging/dry run,
  - post-upgrade validation.
- Rollback runbook covers:
  - when rollback is safe,
  - when DB restore is required,
  - auth lockout risks,
  - schema migration risk.
- Release checklist requires:
  - migration notes,
  - config changes,
  - breaking changes,
  - backup compatibility,
  - rollback notes,
  - security/auth changes,
  - docs impact.

### Effort

L

### CTO Priority

This phase is more important than feature polish.

---

## Phase 4: Admin and Integrator Documentation

### Goal

Document the product surfaces that admins and API consumers need most.

### Files

- New `docs/ADMIN_GUIDE.md`
- New `docs/API_COOKBOOK.md`
- Update `docs/API.md` only to link cookbook/generated reference

### Work

1. Create admin guide around actual UI entry points.
2. Document admin workflows:
   - instance setup,
   - tenant administration,
   - organization/group administration,
   - templates,
   - storage,
   - email,
   - localization,
   - custom properties,
   - analytics,
   - SEO,
   - policy/authorization boundaries.
3. Create API cookbook with task-first examples.
4. Keep endpoint reference in `API.md`; do not duplicate all endpoints.
5. Include HAL, auth, tenant context, errors, pagination, and permissions.

### Acceptance Criteria

- Admin guide states required roles/permissions for each workflow.
- Admin guide identifies UI entry point and expected result.
- Dangerous admin operations include recovery or rollback notes.
- API cookbook is task-first, not endpoint-dump style.
- API cookbook links to generated API docs/OpenAPI instructions where available.
- Authenticated API examples state required permissions.

### Effort

L

---

## Phase 5: Feature Documentation in Controlled Batches

### Goal

Document under-documented implemented surfaces without creating shallow docs.

### Batch A: Platform Services

Files:

- New `docs/STORAGE.md`
- New `docs/EMAIL_NOTIFICATIONS.md`

Source anchors:

- `Explore.Infrastructure/Storage/`
- `Explore.API/Controllers/StorageObjectController.cs`
- `Explore.Infrastructure/Mail/`
- `Explore.Domain/Settings/Definitions/EmailSettingDefinitions.cs`

Acceptance:

- Storage doc covers config, API surface, upload/download flow, backup impact, security boundary.
- Email doc covers SMTP settings, notification sending boundary, troubleshooting, secret handling.

### Batch B: Admin Workflows

Files:

- New `docs/TEMPLATE_SYNC.md`
- New `docs/CONTACT_SHARING.md`

Source anchors:

- `Explore.Blazor.Client/Pages/Admin/EventTemplateSync/`
- `Explore.Blazor.Client/Pages/Admin/EventSessionTemplateSync/`
- `Explore.Application/Features/ContactShareConsents/`
- `Explore.API/Controllers/ContactShareConsentController.cs`

Acceptance:

- Template sync doc explains event/session template sync behavior and admin flow.
- Contact sharing doc explains consent, privacy, export, API, and authorization boundaries.

### Batch C: User Experience and Discovery

Files:

- New `docs/NOTIFICATIONS.md`
- New `docs/SEO.md`

Source anchors:

- `Explore.Application/Features/Notifications/`
- `Explore.Blazor.Client/Layout/NotificationBell.razor`
- `Explore.API/Controllers/SitemapController.cs`
- `Explore.Blazor/Controllers/RobotsController.cs`

Acceptance:

- Notifications doc covers lifecycle, UI behavior, user/admin boundary.
- SEO doc covers sitemap, robots, public routes, render policy concerns.

### Batch D: Engineering Evidence

Files:

- New `docs/BENCHMARKS.md`

Source anchors:

- `Event.Benchmarks/`

Acceptance:

- Benchmarks doc explains purpose, how to run, how to interpret, and what not to infer.

### Global Acceptance for Every Feature Doc

- Metadata present.
- Implemented vs planned clear.
- Source anchors real and verified.
- Links to config/security/API/troubleshooting where relevant.
- Does not duplicate canonical configuration tables.

### Effort

XL, but executed as four smaller reviewable batches.

---

## Phase 6: Contributor and Agent Workflow

### Goal

Make human and AI contributions safer and easier to review.

### Files

- New `.github/ISSUE_TEMPLATE/bug_report.yml`
- New `.github/ISSUE_TEMPLATE/feature_request.yml`
- New `.github/ISSUE_TEMPLATE/documentation.yml`
- New `.github/ISSUE_TEMPLATE/ai_agent_task.yml`
- New `.github/PULL_REQUEST_TEMPLATE.md`
- New `docs/FIRST_CONTRIBUTION.md`
- New `dev/HANDOFF_TEMPLATE.md`
- Update `dev/active/README.md`
- Update `docs/CONTRIBUTING.md`

### Work

1. Add issue templates.
2. Add PR template.
3. Add first contribution guide.
4. Add agent handoff template.
5. Require docs impact in PR template:
   - Updated,
   - Not needed,
   - Deferred with reason.
6. Require verification evidence in PR template.
7. Link templates to relevant docs.

### Acceptance Criteria

- Bug reports request reproduction, expected/actual behavior, affected paths, logs/screenshots.
- Feature requests request user problem, non-goals, affected docs/code paths.
- Docs issues request stale/incorrect location and proposed source anchor.
- AI-agent tasks request context, files touched, validation, risks.
- PR template requires docs impact and tests run.
- First contribution guide lets a junior contributor make a docs-only PR without reading all governance docs.
- Handoff template is short enough to use during context compaction.

### Effort

M

---

## Phase 7: Existing Docs Cleanup and Consolidation

### Goal

Reduce drift and improve trust in high-traffic docs.

### Files

- `README.md`
- `docs/GETTING_STARTED.md`
- `docs/CONTRIBUTING.md`
- `docs/API.md`
- `docs/BLAZOR.md`
- `docs/CONFIGURATION.md`
- `docs/SECURITY.md`
- `docs/FEDERATION.md`
- `docs/ACCESSIBILITY_ARTIFACTS.md`
- `docs/OPERATIONS.md`
- `docs/TROUBLESHOOTING.md`

### Work

1. Remove unresolved placeholders.
2. Normalize metadata for canonical docs.
3. Remove stale roadmap claims or move them into planned sections.
4. Reduce duplication by linking to canonical docs.
5. Keep README concise.
6. Make federation status explicitly implemented vs planned.
7. Make accessibility artifacts either current evidence or clearly unreleased template.
8. Ensure Blazor doc covers contributor-useful service/state patterns without becoming a dump.

### Acceptance Criteria

- No accidental `{DATE}`, `{CONTACT_EMAIL}`, `{CONTACT_URL}`, or stale `TBD`.
- README is concise and links to canonical docs.
- Federation status cannot be misread as fully implemented protocol support if not true.
- Accessibility artifact has real current status or explicit pre-release template status.
- Config docs match runtime source anchors.
- Security docs separate current behavior from planned work.

### Effort

L

---

## Phase 8: Future Public Docs Preparation

### Goal

Prepare for later external docs hosting without doing it now.

### Files

- Optional update to `docs/docs-website/README.md` or equivalent note.
- Optional `docs/PUBLIC_DOCS_ROADMAP.md`.

### Work

1. Add a short note that hosted public docs are deferred.
2. Identify which repo docs are future candidates for public docs.
3. Avoid duplicating content into `docs/docs-website/` now.

### Acceptance Criteria

- No generator added.
- No separate public docs structure maintained in parallel.
- Future public docs strategy is documented but not implemented.

### Effort

S

## Release Documentation Contract

Every release must answer:

1. What changed?
2. Are there EF/database migrations?
3. Are there config or environment variable changes?
4. Are there secret/identity-provider changes?
5. Are there authorization/security changes?
6. Are there breaking API or UI behavior changes?
7. What is the upgrade path?
8. What is the rollback path?
9. Was backup/restore compatibility reviewed?
10. Which docs were updated?

This belongs in `docs/RELEASE_CHECKLIST.md` and in the PR template.

## Verification Plan

Minimum verification after each phase:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

For docs-only changes, if the repo supports a faster docs/context test path, document it after verifying it locally.

Additional verification:

- Search for placeholder tokens.
- Validate relative links.
- Verify metadata on new canonical docs.
- Verify source anchors exist.
- For self-hosting docs, test commands in a clean environment when possible.
- For backup/restore docs, do not mark as fully verified until restore validation is actually tested.

## Risk Assessment

| Risk | Impact | Mitigation |
|---|---|---|
| Docs drift from runtime | Operators fail install or upgrade | Source anchors and release checklist |
| Metadata becomes bureaucracy | Contributors ignore it | Apply first to canonical/operator docs only |
| Docs automation too noisy | Contributors bypass checks | Start with warning/allowlist mode |
| Feature docs become shallow | Low trust | Batch feature docs by domain |
| Public docs deferred too long | Adoption suffers | Repo docs become source for future public docs |
| Rollback docs are theoretical | False operator confidence | Mark restore validation status honestly |
| Roadmap looks implemented | User trust damage | Implemented/planned section policy |

## Success Metrics

- A new evaluator understands the platform and can reach the local app from repo docs.
- A self-hosting operator can install, back up, restore, upgrade, and roll back using repo docs.
- Every canonical doc has audience, status, owner, last verified date, and source anchors.
- Every operator-critical doc is linked from `docs/index.md`.
- Docs-only PRs receive fast automated feedback.
- PRs include a docs impact statement.
- Placeholder tokens are detected automatically.
- Release checklist exists and covers upgrade/rollback/docs impact.
- Future public docs can be built from the repository docs without rewriting from scratch.

## Priority Order

1. `docs/DOCUMENTATION_ARCHITECTURE.md`
2. `docs/DOCUMENTATION_STYLE_GUIDE.md`
3. `docs/index.md`
4. Early placeholder/link/metadata automation
5. Fix stale docs-lint/TUnit documentation
6. `docs/SELF_HOSTING.md`
7. `docs/BACKUP_RESTORE_UPGRADE.md`
8. `docs/RELEASE_CHECKLIST.md`
9. `docs/OPERATIONS.md` split/cleanup
10. `docs/ADMIN_GUIDE.md`
11. `docs/API_COOKBOOK.md`
12. GitHub issue/PR templates
13. `docs/FIRST_CONTRIBUTION.md`
14. `dev/HANDOFF_TEMPLATE.md`
15. Feature docs in batches
16. Existing docs cleanup
17. Future public docs roadmap
