ABOUTME: Entry point for project documentation with high-signal navigation only.
ABOUTME: Prioritizes non-inferable rules and operational docs over generated or repetitive detail.

# Documentation Index

> **Audience:** Operators | Contributors | Admins | Integrators | AI agents
> **Status:** Implemented
> **Owner:** Contributor Experience
> **Last Verified:** 2026-05-12
> **Source Anchors:** `docs/DOCUMENTATION_ARCHITECTURE.md`, `docs/DOCUMENTATION_STYLE_GUIDE.md`, `.github/workflows/agent-context.yml`

## Audience Paths

| Audience | Start Here | Then Read |
|---|---|---|
| Evaluators | [README.md](../README.md), [PROJECT.md](PROJECT.md) | [ARCHITECTURE.md](ARCHITECTURE.md), [SECURITY.md](SECURITY.md), [SELF_HOSTING.md](SELF_HOSTING.md) |
| Operators | [SELF_HOSTING.md](SELF_HOSTING.md) | [CONFIGURATION.md](CONFIGURATION.md), [SECRETS.md](SECRETS.md), [STORAGE.md](STORAGE.md), [EMAIL_NOTIFICATIONS.md](EMAIL_NOTIFICATIONS.md), [NOTIFICATIONS.md](NOTIFICATIONS.md), [SEO.md](SEO.md), [OPERATIONS.md](OPERATIONS.md), [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md), [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) |
| Admins | [ADMIN_GUIDE.md](ADMIN_GUIDE.md) | [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md), [DEPLOYMENT_MODES.md](DEPLOYMENT_MODES.md), [AUTHORIZATION_PATTERNS.md](AUTHORIZATION_PATTERNS.md), [TEMPLATE_SYNC.md](TEMPLATE_SYNC.md), [CONTACT_SHARING.md](CONTACT_SHARING.md), [NOTIFICATIONS.md](NOTIFICATIONS.md), [SEO.md](SEO.md), [STORAGE.md](STORAGE.md), [EMAIL_NOTIFICATIONS.md](EMAIL_NOTIFICATIONS.md) |
| Integrators | [API_COOKBOOK.md](API_COOKBOOK.md) | [API.md](API.md), [CONTACT_SHARING.md](CONTACT_SHARING.md), [NOTIFICATIONS.md](NOTIFICATIONS.md), [SEO.md](SEO.md), [API_CHANGELOG.md](API_CHANGELOG.md), [SECURITY.md](SECURITY.md), [CONFIGURATION.md](CONFIGURATION.md) |
| Contributors | [FIRST_CONTRIBUTION.md](FIRST_CONTRIBUTION.md), [CONTRIBUTING.md](CONTRIBUTING.md) | [TESTING.md](TESTING.md), [BENCHMARKS.md](BENCHMARKS.md), [QUICK_REFERENCE.md](QUICK_REFERENCE.md), [DOCUMENTATION_STYLE_GUIDE.md](DOCUMENTATION_STYLE_GUIDE.md) |
| AI agents | [../AGENTS.md](../AGENTS.md), [../AGENTS.md](../AGENTS.md) | [../.claude/contract/README.md](../.claude/contract/README.md), [../.claude/contract/intents.yaml](../.claude/contract/intents.yaml), [../dev/_journal/README.md](../dev/_journal/README.md) |

## Read First
- [README.md](../README.md) - product overview and quick start.
- [GETTING_STARTED.md](GETTING_STARTED.md) - short local build, run, and first-contribution path.
- [AGENTS.md](../AGENTS.md) - project rules used by contributors and AI agents.
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - implementation constraints that are easy to violate.

## Core Architecture
- [ARCHITECTURE.md](ARCHITECTURE.md) - layer boundaries, BFF model, CQRS flow.
- [DOMAIN.md](DOMAIN.md) - domain entities and important relationships.
- [API.md](API.md) - API conventions, HAL behavior, filters, caching, error shape.
- [API_COOKBOOK.md](API_COOKBOOK.md) - task-first API integration guidance.
- [BLAZOR.md](BLAZOR.md) - client architecture, render policy, service patterns.
- [DOCK_LAYOUT.md](DOCK_LAYOUT.md) - dock engine, responsive behavior, shell/workspace stacking, inspector overlays, and persistence.
- [OUTBOX_PATTERN.md](OUTBOX_PATTERN.md) - transactional outbox, retry, dead-letter.
- [FOOTER_MANAGEMENT.md](FOOTER_MANAGEMENT.md) - footer data model, templates, governance.
- [CUSTOM_PROPERTIES.md](CUSTOM_PROPERTIES.md) - Layer 3 custom-property governance, projections, templates, purge, export/moderation boundaries.
- [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md) - CSS layers, tokens, wrapper components.

## Governance And Rules
- [GOVERNANCE.md](GOVERNANCE.md) - design and code governance.
- [CI_CD_GOVERNANCE.md](CI_CD_GOVERNANCE.md) - GitHub Actions gates, branch protection, deployment environments, and CI/CD evidence policy.
- [NAMING_CONVENTIONS.md](NAMING_CONVENTIONS.md) - naming rules for entities, CQRS, services, tests.
- [TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md) - placeholder syntax used in generic templates.
- [FIRST_CONTRIBUTION.md](FIRST_CONTRIBUTION.md) - shortest safe path for docs-only and small-bug PRs.
- [CONTRIBUTING.md](CONTRIBUTING.md) - contribution workflow and validation steps.
- [DOCUMENTATION_ARCHITECTURE.md](DOCUMENTATION_ARCHITECTURE.md) - docs architecture, ownership, metadata, source-anchor, and quality-gate policy.
- [DOCUMENTATION_STYLE_GUIDE.md](DOCUMENTATION_STYLE_GUIDE.md) - documentation writing rules.
- [PUBLIC_DOCS_ROADMAP.md](PUBLIC_DOCS_ROADMAP.md) - future hosted public-docs boundary and candidate pages.
- [BLAZOR_DEV_WORKFLOW.md](BLAZOR_DEV_WORKFLOW.md) - stop → build → run → wait → inspect loop for Blazor visual verification.

## AI Agent Contribution Context
- [../AGENTS.md](../AGENTS.md) - tool-neutral entrypoint, Contribution Contract, rule authority, cold-start flow.
- [../AGENTS.md](../AGENTS.md) - Claude Code-specific bootloader.
- [../.claude/contract/README.md](../.claude/contract/README.md) - Contribution Contract (eight questions every change must answer).
- [../.claude/contract/intents.yaml](../.claude/contract/intents.yaml) - intent → context map (paths, rules, skills, tests, docs).
- [../.claude/rules/README.md](../.claude/rules/README.md) - path-scoped rule files auto-loaded by editing scope.
- [../.claude/skills/_SKILL_SCHEMA.md](../.claude/skills/_SKILL_SCHEMA.md) - schema every migrated `SKILL.md` follows.
- [../.claude/agents/_AGENT_SCHEMA.md](../.claude/agents/_AGENT_SCHEMA.md) - schema every subagent file follows.
- [../.claude/benchmarks/README.md](../.claude/benchmarks/README.md) - cold-start agent benchmark scenarios.
- [../dev/_journal/README.md](../dev/_journal/README.md) - durable findings log + promotion rules.

## Runtime And Operations
- [CONFIGURATION.md](CONFIGURATION.md) - environment/config settings and secret providers.
- [SECRETS.md](SECRETS.md) - multi-provider secret management (Explore.Secrets library).
- [STORAGE.md](STORAGE.md) - S3-compatible object storage configuration, API boundaries, and backup impact.
- [EMAIL_NOTIFICATIONS.md](EMAIL_NOTIFICATIONS.md) - SMTP email delivery and its boundary from in-app notifications.
- [NOTIFICATIONS.md](NOTIFICATIONS.md) - in-app notification lifecycle, inbox UI, and API boundaries.
- [SEO.md](SEO.md) - sitemap, robots, render-policy, and public-discovery SEO behavior.
- [SECURITY.md](SECURITY.md) - auth/authz model and trust boundaries.
- [OPERATIONS.md](OPERATIONS.md) - deployment, health, observability.
- [TESTING.md](TESTING.md) - TUnit framework, test projects, TDD workflow.
- [BENCHMARKS.md](BENCHMARKS.md) - BenchmarkDotNet runtime benchmarks and cold-start agent benchmark boundaries.
- [SELF_HOSTING.md](SELF_HOSTING.md) - Docker Compose, Keycloak, MinIO, reverse proxy.
- [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) - backup, restore, upgrade, and rollback runbook.
- [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) - release readiness and documentation contract.
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - practical issue resolution.
- [DEPLOYMENT_MODES.md](DEPLOYMENT_MODES.md) - single-tenant vs multi-tenant runtime behavior.
- [DEPLOYMENT_TIERS.md](DEPLOYMENT_TIERS.md) - infrastructure tier choices.

## Multi-Tenancy And Platform Control
- [MULTI_TENANCY.md](MULTI_TENANCY.md) - tenant isolation and resolution.
- [ADMIN_GUIDE.md](ADMIN_GUIDE.md) - task-focused instance, tenant, organization, and group administration.
- [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md) - authority boundaries across instance, tenant, organization.
- [AUTHORIZATION_PATTERNS.md](AUTHORIZATION_PATTERNS.md) - request and pipeline authorization patterns.
- [TEMPLATE_SYNC.md](TEMPLATE_SYNC.md) - event and session template synchronization workflow.
- [CONTACT_SHARING.md](CONTACT_SHARING.md) - registration contact share consent, withdrawal, organization access, and export boundaries.
- [NOTIFICATIONS.md](NOTIFICATIONS.md) - notification inbox behavior and user-owned lifecycle actions.
- [SEO.md](SEO.md) - public discovery, robots, sitemap, and render-policy boundaries.
- [RENDER_POLICIES.md](RENDER_POLICIES.md) - route render policy governance.

## Extensibility And Federation
- [EXTENSIBILITY.md](EXTENSIBILITY.md) - modular extension strategy.
- [MODULAR_EVENTS.md](MODULAR_EVENTS.md) - event aspect composition model.
- [CUSTOM_PROPERTIES.md](CUSTOM_PROPERTIES.md) - governed custom fields on existing resources; see ADR-006 for the runtime-schema boundary.
- [LEXICONS.md](LEXICONS.md) - ATProto-compatible NSID lexicon hierarchy and evolution.
- [FEDERATION.md](FEDERATION.md) - current federation implementation status vs roadmap.

## Reference And Change History
- [PROJECT.md](PROJECT.md) - product context and current scope.
- [CODEBASE_STRUCTURE.md](CODEBASE_STRUCTURE.md) - file and folder map.
- [CODEBASE_INSIGHTS.md](CODEBASE_INSIGHTS.md) - non-intuitive patterns and hidden knowledge.
- [BENCHMARKS.md](BENCHMARKS.md) - runtime benchmark project and benchmark interpretation rules.
- [ACCESSIBILITY.md](ACCESSIBILITY.md) - WCAG AA compliance, service contracts, testing.
- [ACCESSIBILITY_ARTIFACTS.md](ACCESSIBILITY_ARTIFACTS.md) - statement, AT matrix, release gate checklist.
- [PUBLIC_DOCS_ROADMAP.md](PUBLIC_DOCS_ROADMAP.md) - deferred public documentation website plan.
- [API_CHANGELOG.md](API_CHANGELOG.md) - API-specific change log.
- [semantic_versioning/CHANGELOG.md](semantic_versioning/CHANGELOG.md) - release history.
- [semantic_versioning/v0.1.0.md](semantic_versioning/v0.1.0.md) - v0.1.0 historical notes.
- [adr/ADR-001-authorization-provider-architecture.md](adr/ADR-001-authorization-provider-architecture.md) - authorization provider architecture.
- [adr/ADR-002-outbox-pattern.md](adr/ADR-002-outbox-pattern.md) - outbox pattern for reliable messaging.
- [adr/ADR-003-css-layer-architecture.md](adr/ADR-003-css-layer-architecture.md) - CSS @layer architecture.
- [adr/ADR-004-accessibility-architecture.md](adr/ADR-004-accessibility-architecture.md) - accessibility service architecture.
- [adr/ADR-005-footer-customization.md](adr/ADR-005-footer-customization.md) - tenant footer customization.
- [adr/ADR-006-custom-properties-runtime-boundary.md](adr/ADR-006-custom-properties-runtime-boundary.md) - custom properties versus runtime schema engine boundary.
- [adr/ADR-008-email-dispatch-state-machine.md](adr/ADR-008-email-dispatch-state-machine.md) - email dispatch state machine and optional RabbitMQ dispatch profiles.
