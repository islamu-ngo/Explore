ABOUTME: Resume context for the repository documentation upgrade implementation.
ABOUTME: Captures decisions, source anchors, CTO feedback, and current execution state.

# Repository Documentation Upgrade Context

Last Updated: 2026-05-06

## Current Source Of Truth

This work upgrades repository documentation only.

A separately hosted public documentation website is explicitly deferred. For now, all documentation work should be implemented as Markdown files inside the repository, primarily under `docs/`, plus GitHub templates under `.github/` and handoff templates under `dev/`.

## Current Goal

Turn the existing documentation set into an enterprise-grade, self-hostable documentation system for:

1. Evaluators deciding whether ISLAMU Event fits their needs.
2. Self-hosting operators installing, backing up, restoring, upgrading, and rolling back.
3. Instance and tenant administrators operating the product.
4. Human contributors making safe changes.
5. AI agents making source-grounded, rule-compliant changes.

## Senior CTO Feedback Integrated

The prior plan was directionally strong but needed these corrections:

1. **Repository Markdown first.**
   - Do not add MkDocs, Docusaurus, VitePress, or any hosted docs-site generator in this phase.
   - The future docs website can later consume or mirror selected repository docs.

2. **Move automation earlier.**
   - Placeholder, stale marker, metadata, and link checks should start before the large rewrite work.
   - Do not wait until the final phase to catch `{DATE}`, `{CONTACT_EMAIL}`, `TBD`, unsupported roadmap language, or broken relative links.

3. **Reduce implementation batch size.**
   - Do not create seven feature docs in one uncontrolled phase.
   - Implement operator-critical docs first, then admin/integrator docs, then feature docs in small batches.

4. **Add explicit ownership.**
   - Metadata must not become bureaucracy.
   - Each canonical doc should have a clear owner category: Platform/Ops, Security, API, Frontend, Product/Admin, Contributor Experience, or Agent Context.

5. **Add release documentation contract.**
   - Enterprise self-hostable software needs release docs that explicitly cover migrations, configuration changes, upgrade notes, rollback notes, backup compatibility, security changes, and docs impact.

6. **Require docs impact for every change.**
   - A feature or refactor is not complete until docs impact is marked as updated, not needed, or deferred with a reason.

## Existing Verified Documentation Anchors

Repository docs already include:

- `docs/index.md` — canonical documentation navigation root.
- `docs/DOCUMENTATION_STYLE_GUIDE.md` — current writing conventions.
- `docs/DOCUMENTATION_SYNTHESIS.md` — current docs improvement direction.
- `docs/DOCUMENTATION_IMPROVEMENT_RESEARCH.md` — prior docs research/audit artifact.
- `docs/CONTRIBUTING.md` — contributor workflow.
- `docs/GOVERNANCE.md` — governance and decision framework.
- `docs/PROJECT.md` — project scope/status context.
- `docs/ARCHITECTURE.md` — architecture overview.
- `docs/API.md` — API conventions and reference.
- `docs/DOMAIN.md` — domain model overview.
- `docs/SECURITY.md` — authentication/authorization/security model.
- `docs/CONFIGURATION.md` — configuration reference.
- `docs/OPERATIONS.md` — operations reference/runbook content.
- `docs/TROUBLESHOOTING.md` — incident and issue triage.
- `docs/SELF_HOSTING.md` — self-hosting guide requiring runtime reconciliation.
- `docs/FEDERATION.md` — implemented/planned federation status requiring cleanup.
- `docs/ACCESSIBILITY_ARTIFACTS.md` — evidence artifact with placeholder risk.
- `docs/DEPLOYMENT_MODES.md` — single-tenant vs multi-tenant runtime behavior.
- `docs/DEPLOYMENT_TIERS.md` — infrastructure tier choices.
- `docs/SECRETS.md` — secrets management reference.
- `docs/TESTING.md` — testing guidance.
- `dev/active/README.md` — active dev-docs pattern.
- `AGENTS.md` — tool-neutral AI-agent entrypoint.
- `CLAUDE.md` — Claude Code/project agent contract.
- `.github/copilot-instructions.md` — Copilot-specific instructions.
- `.claude/contract/README.md` — contribution contract.
- `.claude/contract/intents.yaml` — intent-to-doc/test routing.

## Verified Public Docs Website Files

These files exist but should not drive this phase:

- `docs/docs-website/Introduction.md`
- `docs/docs-website/Support.md`
- `docs/docs-website/Installation/docker.md`
- `docs/docs-website/Installation/docker-compose.md`
- `docs/docs-website/tutorials/GETTING_STARTED.md`

Decision: keep them parked or add a short note that public website publishing is deferred. Do not invest major effort in this folder until repository docs are stable.

## Verified Validation and CI Files

- `.github/workflows/agent-context.yml` — validates docs and agent context.
- `.github/workflows/test.yml` — main test workflow.
- `.github/workflows/codeql.yml` — CodeQL workflow.
- `.github/workflows/deploy-coolify.yml` — deployment workflow.
- `Event.Architecture.Tests/AgentContextLinkTests.cs` — markdown link checks.
- `Event.Architecture.Tests/AgentContextSchemaTests.cs` — agent context schema checks.
- `Event.Architecture.Tests/AgentContextIntentManifestTests.cs` — intent manifest checks.
- `Event.Architecture.Tests/AgentContextDuplicationTests.cs` — duplication checks.

## Verified Runtime and Product Source Anchors for Missing Docs

Operator/runtime:

- `docker-compose.yml`
- `Explore.AppHost/`
- `Event.MigrationService/`
- `.github/workflows/`
- `docs/CONFIGURATION.md`
- `docs/SECRETS.md`
- `docs/DEPLOYMENT_MODES.md`
- `docs/DEPLOYMENT_TIERS.md`

Admin/product:

- `Explore.Blazor.Client/Pages/Admin/`
- `docs/ADMIN_HIERARCHY.md`
- `docs/AUTHORIZATION.md`
- `docs/AUTHORIZATION_PATTERNS.md`

Feature surfaces:

- Storage:
  - `Explore.Infrastructure/Storage/`
  - `Explore.API/Controllers/StorageObjectController.cs`
- Email/SMTP:
  - `Explore.Infrastructure/Mail/`
  - `Explore.Domain/Settings/Definitions/EmailSettingDefinitions.cs`
- Template sync:
  - `Explore.Blazor.Client/Pages/Admin/EventTemplateSync/`
  - `Explore.Blazor.Client/Pages/Admin/EventSessionTemplateSync/`
- Contact sharing:
  - `Explore.Application/Features/ContactShareConsents/`
  - `Explore.API/Controllers/ContactShareConsentController.cs`
- Notifications:
  - `Explore.Application/Features/Notifications/`
  - `Explore.Blazor.Client/Layout/NotificationBell.razor`
- SEO/sitemap:
  - `Explore.API/Controllers/SitemapController.cs`
  - `Explore.Blazor/Controllers/RobotsController.cs`
- Benchmarks:
  - `Event.Benchmarks/`

Test anchors:

- `Event.API.IntegrationTests/`
- `Explore.Blazor.Client.Tests/`
- `Event.Application.UnitTests/`
- `Explore.Infrastructure.Tests/`
- `Event.Architecture.Tests/`

## Documentation Ownership Model

Use the following owner categories in doc metadata:

| Owner Category | Owns |
|---|---|
| Platform/Ops | `SELF_HOSTING`, `BACKUP_RESTORE_UPGRADE`, `OPERATIONS`, deployment, release docs |
| Security | `SECURITY`, `AUTHORIZATION`, `AUTHORIZATION_PATTERNS`, Keycloak, Cerbos, secrets |
| API | `API`, `API_COOKBOOK`, OpenAPI, HAL, errors, pagination |
| Frontend | `BLAZOR`, `DESIGN_SYSTEM`, `ACCESSIBILITY`, render policies |
| Product/Admin | `ADMIN_GUIDE`, feature docs, admin workflows |
| Contributor Experience | `CONTRIBUTING`, `FIRST_CONTRIBUTION`, templates, testing docs |
| Agent Context | `AGENTS`, `CLAUDE`, `.claude/contract`, `dev/active`, handoff templates |

## Required Metadata Policy

Canonical and new docs should include metadata near the top:

```markdown
> **Audience:** Operators | Contributors | Admins | Integrators | AI agents
> **Status:** Implemented | Draft | Planned | Mixed
> **Owner:** Platform/Ops | Security | API | Frontend | Product/Admin | Contributor Experience | Agent Context
> **Last Verified:** YYYY-MM-DD
> **Source Anchors:** `path/one`, `path/two`
```

Rules:

- Do not add metadata mechanically to every low-value doc in one noisy commit.
- Apply first to canonical docs, operator-critical docs, and all new docs.
- `Source Anchors` must point to real files or folders verified during the task.
- If behavior is planned, label it planned in the affected section, not only at page level.

## Current Blockers

None known for the documentation implementation slice.

Focused documentation quality checks pass for metadata, source anchors, placeholder markers, and unsupported TUnit filter guidance. Full `Event.Architecture.Tests` also passed after the Phase 4 admin/integrator documentation updates.

## Known Risks

1. `SELF_HOSTING.md` may be stale against `docker-compose.yml` and `Explore.AppHost/`.
2. `ACCESSIBILITY_ARTIFACTS.md` has placeholder values that weaken trust.
3. Current docs may mix implemented behavior with roadmap/future behavior.
4. Operator docs cannot be considered correct until commands are tested in a clean environment.
5. Docs automation can become noisy if introduced too strictly before metadata migration is complete.

## Execution Guidance

Start with the smallest enforceable foundation:

1. Create `docs/DOCUMENTATION_ARCHITECTURE.md`.
2. Update `docs/DOCUMENTATION_STYLE_GUIDE.md`.
3. Update `docs/index.md`.
4. Add early placeholder/staleness checks.
5. Fix known stale docs-lint/TUnit command documentation.
6. Rewrite `docs/SELF_HOSTING.md`.
7. Create `docs/BACKUP_RESTORE_UPGRADE.md`.
8. Create `docs/RELEASE_CHECKLIST.md`.

Do not start broad feature documentation until operator-critical docs and the release docs contract are in place.

## Implementation Progress

Completed on 2026-05-06:

1. Phase 0 baseline/context discovery and stale-marker inventory.
2. Phase 1 documentation architecture, style guide, index, and synthesis updates.
3. Phase 2 migration-safe documentation quality tests and stale TUnit/docs-lint cleanup.
4. Phase 3 operator-critical docs: self-hosting, backup/restore/upgrade, release checklist, operations runbook routing, troubleshooting, configuration/secrets drift corrections.

Current iteration:

- Phase 4 admin and integrator documentation was implemented on 2026-05-06.
- `docs/ADMIN_GUIDE.md` now documents source-grounded instance, tenant, organization, group, template, storage, SMTP, localization, custom-property, analytics, and public-discovery administration workflows.
- `docs/API_COOKBOOK.md` now provides task-first integration guidance for authentication, tenant context, HAL/minimal responses, pagination, ProblemDetails, idempotency, and generated OpenAPI/Scalar reference usage.
- `docs/API.md` now carries canonical metadata and links to `docs/API_COOKBOOK.md` while keeping generated OpenAPI/Scalar output as the endpoint source of truth.
- Phase 5 Batch A platform-service documentation was implemented on 2026-05-06.
- `docs/STORAGE.md` now documents S3-compatible storage configuration, upload/download flow, API authorization boundaries, backup/restore impact, and troubleshooting.
- `docs/EMAIL_NOTIFICATIONS.md` now documents direct SMTP delivery, sensitive SMTP settings, admin test workflow, health checks, and the boundary from in-app notifications.
- Oracle review for Phase 5 Batch A was completed on 2026-05-06. Blocking findings were fixed: the future `NOTIFICATIONS.md` link was converted to plain text, storage upload-url authorization was narrowed to authentication, and metadata create/update/delete kept the `storage_object` resource-authorization boundary.
- Phase 5 Batch A docs now also clarify that no inspected product workflow currently calls `IEmailService.SendAsync`, notification-to-email fanout is not implemented, and SMTP/S3 settings can briefly reflect resolver cache behavior.
- Phase 5 Batch B admin-workflow documentation was implemented on 2026-05-06.
- `docs/TEMPLATE_SYNC.md` now documents event/session template sync routes, API surface, resource authorization, apply/conflict behavior, dangerous operations, and implemented UI caveats.
- `docs/CONTACT_SHARING.md` now documents organizer-scoped contact share consent, registration capture, user withdrawal, organization read/export API boundaries, privacy boundaries, and verified export behavior.
- Oracle review for Phase 5 Batch B was completed on 2026-05-06. Blocking findings were fixed: contact sharing capture prerequisites now match service defaults, template-sync stale-base wording now refers to event/session provenance changes, endpoint routes are explicit, audit logging is scoped to applied changes, and resource-kind wording matches the source value.
- Phase 5 Batch C UX/discovery documentation was implemented on 2026-05-06.
- `docs/NOTIFICATIONS.md` now documents authenticated in-app notification lifecycle, API endpoints, inbox UI behavior, read/archive/snooze/soft-delete boundaries, and the separation from SMTP email delivery.
- `docs/SEO.md` now documents sitemap and robots behavior, public route render-policy implications, tenant public-experience controls, and unsupported SEO automation claims to avoid.
- Oracle review for Phase 5 Batch C was completed on 2026-05-06. No blocking findings were reported; the non-blocking SEO route/source-anchor review was applied by adding direct sitemap/public-experience source anchors and updating the `/home`/`/welcome` caveat to reference `Explore.Blazor.Client/Routes.razor`.
- Phase 5 Batch D engineering-evidence documentation was implemented on 2026-05-06.
- `docs/BENCHMARKS.md` now documents the `Event.Benchmarks` BenchmarkDotNet project, run commands, interpretation boundaries, and the separate `.claude/benchmarks` cold-start agent benchmark suite.
- Oracle review for Phase 5 Batch D was completed on 2026-05-06. The blocking caching-suite wording issue was fixed so `ConcurrentDictionary` is documented for lookup only, while enumeration is scoped to `FrozenDictionary` and `Dictionary`; the runtime evidence wording was also tightened to controlled-run microbenchmark evidence.
- Phase 6 contributor and agent workflow templates were implemented on 2026-05-06.
- Added GitHub issue forms for bug reports, feature requests, documentation issues, and AI-agent tasks; added `.github/PULL_REQUEST_TEMPLATE.md` with documentation impact, validation, UI, operator/release, and handoff sections.
- Added `docs/FIRST_CONTRIBUTION.md` for short docs-only and small-bug PR paths, plus `dev/HANDOFF_TEMPLATE.md` and `dev/active/README.md` handoff guidance.
- Updated `docs/CONTRIBUTING.md`, `docs/index.md`, and documentation metadata validation for the first-contribution guide.
- Phase 7 existing docs cleanup started on 2026-05-06.
- `README.md` is now a concise landing page with product positioning, pre-1.0 maturity warning, Compose/Aspire quick starts, role-based canonical docs links, implemented capability boundaries, and no stale badge block.
- `docs/GETTING_STARTED.md` is now a short runnable local onboarding path with metadata/source anchors, Aspire and Compose options, verified command shapes, canonical validation guidance, and deeper links instead of duplicating the full test matrix.
- `docs/API.md` now keeps canonical API conventions while tightening OpenAPI source-of-truth wording, Development/Testing endpoint availability, Production Compose caveats, notification filter naming, and generated-client export instructions.
- `docs/BLAZOR.md` now has metadata/source anchors and is a concise contributor guide for BFF endpoints, proxy/token forwarding, setup-secret trust boundaries, API client generation, render-policy consumption, and service/state patterns without duplicating design/accessibility/localization docs.
- Next iteration should continue Phase 7 existing docs cleanup with configuration/security/federation cleanup after this slice remains green.

## Validation Notes

Baseline architecture validation on 2026-05-06 passed before documentation changes:

```text
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
146 total, 146 succeeded, 0 failed, 0 skipped
```

Known baseline warnings were unrelated package warnings: MailKit moderate vulnerability warning, Microsoft.CodeAnalysis version constraint warning, and NU1510 prune warnings.

Phase 4 validation on 2026-05-06 passed after adding `docs/ADMIN_GUIDE.md`, `docs/API_COOKBOOK.md`, and metadata coverage for `docs/API.md`:

```text
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
150 total, 150 succeeded, 0 failed, 0 skipped
```

Phase 5 Batch A validation on 2026-05-06 passed after adding `docs/STORAGE.md`, `docs/EMAIL_NOTIFICATIONS.md`, index links, and metadata coverage for both docs:

```text
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
150 total, 150 succeeded, 0 failed, 0 skipped
```

Post-Oracle Phase 5 Batch A validation on 2026-05-06 passed after fixing the storage authorization wording and the future notifications-doc link:

```text
Focused docs-quality tests: 4 total, 4 succeeded, 0 failed, 0 skipped
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
150 total, 150 succeeded, 0 failed, 0 skipped
```

Post-Oracle Phase 5 Batch B validation on 2026-05-06 passed after fixing contact-sharing capture wording and template-sync conflict/route/audit wording:

```text
Focused docs-quality tests: 4 total, 4 succeeded, 0 failed, 0 skipped
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
150 total, 150 succeeded, 0 failed, 0 skipped
```

Phase 5 Batch C validation on 2026-05-06 passed after adding `docs/NOTIFICATIONS.md`, `docs/SEO.md`, index links, and metadata coverage for both docs:

```text
Focused docs-quality tests: 4 total, 4 succeeded, 0 failed, 0 skipped
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
150 total, 150 succeeded, 0 failed, 0 skipped
```

Post-Oracle Phase 5 Batch C validation on 2026-05-06 passed after tightening `docs/SEO.md` source anchors and route wording:

```text
Focused docs-quality tests: 4 total, 4 succeeded, 0 failed, 0 skipped
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
150 total, 150 succeeded, 0 failed, 0 skipped
```

Phase 5 Batch D validation on 2026-05-06 passed after adding `docs/BENCHMARKS.md`, index links, and metadata coverage for the benchmark doc:

```text
Focused docs-quality tests: 4 total, 4 succeeded, 0 failed, 0 skipped
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
150 total, 150 succeeded, 0 failed, 0 skipped
```

Post-Oracle Phase 5 Batch D validation on 2026-05-06 passed after tightening `docs/BENCHMARKS.md` caching-suite and evidence wording:

```text
Focused docs-quality tests: 4 total, 4 succeeded, 0 failed, 0 skipped
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
150 total, 150 succeeded, 0 failed, 0 skipped
```

Phase 6 validation on 2026-05-06 passed after adding contributor issue forms, the pull request template, `docs/FIRST_CONTRIBUTION.md`, handoff guidance, index links, and metadata coverage for the first-contribution guide:

```text
GitHub issue form YAML parse: 4 forms parsed successfully
Focused docs-quality tests: 4 total, 4 succeeded, 0 failed, 0 skipped
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
150 total, 150 succeeded, 0 failed, 0 skipped
```

Post-Oracle Phase 6 validation on 2026-05-06 passed after clarifying the first-contribution issue-form wording, adding metadata to `docs/CONTRIBUTING.md`, and extending stale TUnit filter guidance checks to the pull request and handoff templates:

```text
Focused docs-quality tests: 4 total, 4 succeeded, 0 failed, 0 skipped
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
150 total, 150 succeeded, 0 failed, 0 skipped
```

Phase 7 README/getting-started slice validation on 2026-05-06 passed after replacing the top-level README, rewriting `docs/GETTING_STARTED.md`, updating `docs/index.md`, and adding metadata coverage for `docs/GETTING_STARTED.md`:

```text
Focused docs-quality tests: 4 total, 4 succeeded, 0 failed, 0 skipped
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
150 total, 150 succeeded, 0 failed, 0 skipped
```

Oracle review for the Phase 7 README/getting-started slice completed on 2026-05-06. Blocking findings were fixed by documenting the Compose `API_ENDPOINT=http://explore-api:8080/` override, removing Swagger/OpenAPI from Production Compose endpoint tables, and replacing the unsupported public-instance “follow organizations” claim with source-backed browse/account/publish/register behavior.

Post-Oracle Phase 7 README/getting-started validation on 2026-05-06 passed after applying the source-grounding fixes:

```text
Focused docs-quality tests: 4 total, 4 succeeded, 0 failed, 0 skipped
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
150 total, 150 succeeded, 0 failed, 0 skipped
```

Minimum validation after documentation architecture changes:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet
```

If `--no-build` fails due to missing build artifacts, run:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Do not use unsupported TUnit `--filter` examples in docs unless locally verified.
