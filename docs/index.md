ABOUTME: Entry point for project documentation with high-signal navigation only.
ABOUTME: Prioritizes non-inferable rules and operational docs over generated or repetitive detail.

# Documentation Index

> **Audience:** Operators | Contributors | Admins | Integrators | AI agents
> **Status:** Implemented
> **Owner:** Contributor Experience
> **Last Verified:** 2026-06-12
> **Source Anchors:** `README.md`, `docs/DOCUMENTATION_ARCHITECTURE.md`, `docs/DOCUMENTATION_STYLE_GUIDE.md`

Start with [README.md](../README.md) if you are new to the project. Use this page when you already know your task or need the complete documentation inventory.

## Task Paths

| I need to... | Start Here | Then Read |
|---|---|---|
| Understand the product and current scope | [PROJECT.md](PROJECT.md) | [ARCHITECTURE.md](ARCHITECTURE.md), [SECURITY-MODEL.md](SECURITY-MODEL.md) |
| Run the app locally | [GETTING_STARTED.md](GETTING_STARTED.md) | [TESTING.md](TESTING.md), [TROUBLESHOOTING.md](TROUBLESHOOTING.md) |
| Self-host or operate a deployment | [SELF_HOSTING.md](SELF_HOSTING.md) | [CONFIGURATION.md](CONFIGURATION.md), [SECRETS.md](SECRETS.md), [OPERATIONS.md](OPERATIONS.md), [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) |
| Troubleshoot a failure | [TROUBLESHOOTING.md](TROUBLESHOOTING.md) | [OPERATIONS.md](OPERATIONS.md), [CONFIGURATION.md](CONFIGURATION.md), [TEST_RELIABILITY.md](TEST_RELIABILITY.md) |
| Configure tenants, admins, or branding | [ADMIN_GUIDE.md](ADMIN_GUIDE.md) | [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md), [DEPLOYMENT_MODES.md](DEPLOYMENT_MODES.md), [MULTI_TENANCY.md](MULTI_TENANCY.md) |
| Integrate with the API | [API_COOKBOOK.md](API_COOKBOOK.md) | [API.md](API.md), [WEBHOOKS.md](WEBHOOKS.md), [INTEGRATIONS.md](INTEGRATIONS.md), [API_CHANGELOG.md](API_CHANGELOG.md), [SECURITY-MODEL.md](SECURITY-MODEL.md) |
| Change authorization or Cerbos behavior | [AUTHORIZATION.md](AUTHORIZATION.md) | [AUTHORIZATION_PATTERNS.md](AUTHORIZATION_PATTERNS.md), [SECURITY-MODEL.md](SECURITY-MODEL.md), [OPERATIONS.md](OPERATIONS.md) |
| Contribute code or docs | [FIRST_CONTRIBUTION.md](FIRST_CONTRIBUTION.md) | [CONTRIBUTING.md](CONTRIBUTING.md), [QUICK_REFERENCE.md](QUICK_REFERENCE.md), [TESTING.md](TESTING.md) |
| Work on Blazor UI | [BLAZOR.md](BLAZOR.md) | [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md), [ACCESSIBILITY.md](ACCESSIBILITY.md), [RENDER_POLICIES.md](RENDER_POLICIES.md) |
| Use an AI coding agent | [../AGENTS.md](../AGENTS.md) | [../.claude/contract/intents.yaml](../.claude/contract/intents.yaml), [QUICK_REFERENCE.md](QUICK_REFERENCE.md), [../dev/_journal/README.md](../dev/_journal/README.md) |

## How To Use This Index

- Prefer the task path first, then use the full inventory below only when you need a specific reference.
- Treat how-to pages as workflows and reference pages as the exact source for keys, contracts, commands, and runtime behavior.
- If two docs disagree, prefer the page with source anchors to code, infrastructure, tests, or workflows, then fix the stale page.

## Read First

- [README.md](../README.md) - product overview and top-level route into the docs.
- [GETTING_STARTED.md](GETTING_STARTED.md) - shortest local build, run, and first-contribution path.
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - implementation constraints that are easy to violate.
- [AGENTS.md](../AGENTS.md) - contribution contract used by contributors and AI agents.

## Core Architecture
- [ARCHITECTURE.md](ARCHITECTURE.md) - layer boundaries, BFF model, CQRS flow.
- [DOMAIN.md](DOMAIN.md) - domain entities and important relationships.
- [API.md](API.md) - API conventions, HAL behavior, filters, caching, error shape.
- [API_CONTRACT_INVENTORY.md](API_CONTRACT_INVENTORY.md) - generated inventory of every OpenAPI operation.
- [API_CONTRACT_TEST_DEBT.md](API_CONTRACT_TEST_DEBT.md) - governed inventory of intentionally skipped API contract tests and removal criteria.
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
- [CI_CD_RUNBOOKS.md](CI_CD_RUNBOOKS.md) - maintainer runbooks for rerunning CI/CD gates without bypassing controls.
- [legal/CLA.md](../legal/CLA.md) - active ISLAMU Event contributor license agreement.
- [legal/CONTRIBUTION_GOVERNANCE.md](legal/CONTRIBUTION_GOVERNANCE.md) - CLA legal contribution decision record and automation threat model.
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
- [../.claude/contract/README.md](../.claude/contract/README.md) - Contribution Contract (eight questions every change must answer).
- [../.claude/contract/intents.yaml](../.claude/contract/intents.yaml) - intent → context map (paths, rules, skills, tests, docs).
- [../.claude/rules/README.md](../.claude/rules/README.md) - path-scoped rule files auto-loaded by editing scope.
- [../.agents/skills/_SKILL_SCHEMA.md](../.agents/skills/_SKILL_SCHEMA.md) - schema every migrated `SKILL.md` follows.
- [../.claude/agents/_AGENT_SCHEMA.md](../.claude/agents/_AGENT_SCHEMA.md) - schema every subagent file follows.
- [../.claude/benchmarks/README.md](../.claude/benchmarks/README.md) - cold-start agent benchmark scenarios.
- [../dev/_journal/README.md](../dev/_journal/README.md) - durable findings log + promotion rules.

## Runtime And Operations
- [CONFIGURATION.md](CONFIGURATION.md) - environment/config settings and secret providers.
- [SECRETS.md](SECRETS.md) - multi-provider secret management (Explore.Secrets library).
- [STORAGE.md](STORAGE.md) - local-first object storage, optional S3-compatible configuration, reconciliation, API boundaries, and backup impact.
- [EMAIL_NOTIFICATIONS.md](EMAIL_NOTIFICATIONS.md) - SMTP email delivery and its boundary from in-app notifications.
- [NOTIFICATIONS.md](NOTIFICATIONS.md) - in-app notification lifecycle, inbox UI, and API boundaries.
- [WEBHOOKS.md](WEBHOOKS.md) - outgoing webhook providers, LocalProvider/SvixProvider setup, signatures, health, and rollout.
- [WEBHOOK_OPERATIONS_RUNBOOK.md](WEBHOOK_OPERATIONS_RUNBOOK.md) - webhook readiness, bounded metrics, SLOs, alerts, and Local/self-hosted Svix incident recovery.
- [INTEGRATIONS.md](INTEGRATIONS.md) - incoming provider callbacks, Coop/Osprey/Svix operational verification, and idempotency.
- [SEO.md](SEO.md) - sitemap, robots, render-policy, and public-discovery SEO behavior.
- [SECURITY.md](SECURITY.md) - auth/authz model and trust boundaries.
- [SECURITY-MODEL.md](SECURITY-MODEL.md) - platform security invariants, privilege isolation, and privacy boundaries.
- [PRIVACY_ERASURE.md](PRIVACY_ERASURE.md) - Privacy Erasure Authority, concrete meaning, storage topologies (EmbeddedSqlite, CoLocated, ExternalDatabase), independent restore-isolation rationale, and self-hoster decision guide.
- [OPERATIONS.md](OPERATIONS.md) - deployment, health, observability.
- [TESTING.md](TESTING.md) - TUnit framework, test projects, TDD workflow.
- [TEST_RELIABILITY.md](TEST_RELIABILITY.md) - flaky/deferred runtime, stress, and manual-test tracking.
- [BENCHMARKS.md](BENCHMARKS.md) - BenchmarkDotNet runtime benchmarks, API pipeline benchmarks, PostgreSQL/Testcontainers benchmark runs, and cold-start agent benchmark boundaries.
- [AI_RAG_FOUNDATION.md](AI_RAG_FOUNDATION.md) - tenant-safe summary-only boundary for future AI vector/RAG support.
- [AI_AGENT_CONTRACT_INVENTORY.md](AI_AGENT_CONTRACT_INVENTORY.md) - generated inventory of registry-governed AI agent tool contracts.
- [AI_AGENT_EXPERIENCE_HARDENING.md](AI_AGENT_EXPERIENCE_HARDENING.md) - guarded context summaries, proposal-only plan previews, and fake/replay AI usability reports.
- [MCP_DEBUGGING.md](MCP_DEBUGGING.md) - redacted local MCP debugging, Inspector/Copilot smoke, JSON-RPC tests, and compatibility gates.
- [SELF_HOSTING.md](SELF_HOSTING.md) - Docker Compose, Keycloak, local storage volume, optional MinIO, reverse proxy.
- [CERBOS_COOLIFY.md](CERBOS_COOLIFY.md) - Coolify deployment runbook for external Cerbos PDP, PostgreSQL storage, gRPC routing, and policy upload.
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
- [ERP_INTEGRATION_GUIDE.md](ERP_INTEGRATION_GUIDE.md) - operator & partner guide for integrating ISLAMU Event as a white-label module in enterprise ERP platforms.
- [WEBHOOKS.md](WEBHOOKS.md) - outgoing webhook provider extension point and self-hosted Svix integration.
- [INTEGRATIONS.md](INTEGRATIONS.md) - incoming integration callback boundary.
- [COOP_INTEGRATION.md](COOP_INTEGRATION.md) - multi-tenant human review dashboard integration and escalation flow.
- [OSPREY_INTEGRATION.md](OSPREY_INTEGRATION.md) - single-tenant machine moderation challenges and hybrid policy engine.
- [MODULAR_EVENTS.md](MODULAR_EVENTS.md) - event aspect composition model.
- [CUSTOM_PROPERTIES.md](CUSTOM_PROPERTIES.md) - governed custom fields on existing resources; see ADR-006 for the runtime-schema boundary.
- [LEXICONS.md](LEXICONS.md) - ATProto-compatible NSID lexicon hierarchy and evolution.
- [FEDERATION.md](FEDERATION.md) - current federation implementation status vs roadmap.

## Reference And Change History
- [PROJECT.md](PROJECT.md) - product context and current scope.
- [CODEBASE_STRUCTURE.md](CODEBASE_STRUCTURE.md) - file and folder map.
- [CODEBASE_INSIGHTS.md](CODEBASE_INSIGHTS.md) - non-intuitive patterns and hidden knowledge.
- [BENCHMARKS.md](BENCHMARKS.md) - runtime benchmark project, API performance harnesses, Testcontainers-backed benchmark lane, and benchmark interpretation rules.
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
- [adr/ADR-009-postgresql-partitioning-deferral.md](adr/ADR-009-postgresql-partitioning-deferral.md) - PostgreSQL partitioning deferral, activation gates, and runbook requirements.
- [adr/ADR-010-mcp-adapter-hosting-strategy.md](adr/ADR-010-mcp-adapter-hosting-strategy.md) - MCP adapter hosting, transport, auth, tenancy, and disable posture.
- [adr/ADR-011-local-mcp-stdio-diagnostic-host.md](adr/ADR-011-local-mcp-stdio-diagnostic-host.md) - local-only stdio diagnostic-host decision and deferral.
- [adr/ADR-021-keycloak-authentication-standard.md](adr/ADR-021-keycloak-authentication-standard.md) - standardizing ERP authentication on Keycloak and authorization on Cerbos across all deployment tiers.
