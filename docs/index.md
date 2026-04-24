ABOUTME: Entry point for project documentation with high-signal navigation only.
ABOUTME: Prioritizes non-inferable rules and operational docs over generated or repetitive detail.

# Documentation Index

## Read First
- [README.md](../README.md) - product overview and quick start.
- [GETTING_STARTED.md](GETTING_STARTED.md) - prerequisites, build, run, first change walkthrough.
- [CLAUDE.md](../CLAUDE.md) - project rules used by contributors and AI agents.
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - implementation constraints that are easy to violate.

## Core Architecture
- [ARCHITECTURE.md](ARCHITECTURE.md) - layer boundaries, BFF model, CQRS flow.
- [DOMAIN.md](DOMAIN.md) - domain entities and important relationships.
- [API.md](API.md) - API conventions, HAL behavior, filters, caching, error shape.
- [BLAZOR.md](BLAZOR.md) - client architecture, render policy, service patterns.
- [OUTBOX_PATTERN.md](OUTBOX_PATTERN.md) - transactional outbox, retry, dead-letter.
- [FOOTER_MANAGEMENT.md](FOOTER_MANAGEMENT.md) - footer data model, templates, governance.
- [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md) - CSS layers, tokens, wrapper components.

## Governance And Rules
- [GOVERNANCE.md](GOVERNANCE.md) - design and code governance.
- [NAMING_CONVENTIONS.md](NAMING_CONVENTIONS.md) - naming rules for entities, CQRS, services, tests.
- [TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md) - placeholder syntax used in generic templates.
- [CONTRIBUTING.md](CONTRIBUTING.md) - contribution workflow and validation steps.
- [DOCUMENTATION_STYLE_GUIDE.md](DOCUMENTATION_STYLE_GUIDE.md) - documentation writing rules.
- [BLAZOR_DEV_WORKFLOW.md](BLAZOR_DEV_WORKFLOW.md) - stop → build → run → wait → inspect loop for Blazor visual verification.

## AI Agent Contribution Context
- [../AGENTS.md](../AGENTS.md) - tool-neutral entrypoint, Contribution Contract, rule authority, cold-start flow.
- [../CLAUDE.md](../CLAUDE.md) - Claude Code-specific bootloader.
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
- [SECURITY.md](SECURITY.md) - auth/authz model and trust boundaries.
- [OPERATIONS.md](OPERATIONS.md) - deployment, health, observability.
- [TESTING.md](TESTING.md) - TUnit framework, test projects, TDD workflow.
- [SELF_HOSTING.md](SELF_HOSTING.md) - Docker Compose, Keycloak, MinIO, reverse proxy.
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - practical issue resolution.
- [DEPLOYMENT_MODES.md](DEPLOYMENT_MODES.md) - single-tenant vs multi-tenant runtime behavior.
- [DEPLOYMENT_TIERS.md](DEPLOYMENT_TIERS.md) - infrastructure tier choices.

## Multi-Tenancy And Platform Control
- [MULTI_TENANCY.md](MULTI_TENANCY.md) - tenant isolation and resolution.
- [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md) - authority boundaries across instance, tenant, organization.
- [AUTHORIZATION_PATTERNS.md](AUTHORIZATION_PATTERNS.md) - request and pipeline authorization patterns.
- [RENDER_POLICIES.md](RENDER_POLICIES.md) - route render policy governance.

## Extensibility And Federation
- [EXTENSIBILITY.md](EXTENSIBILITY.md) - modular extension strategy.
- [MODULAR_EVENTS.md](MODULAR_EVENTS.md) - event aspect composition model.
- [LEXICONS.md](LEXICONS.md) - ATProto-compatible NSID lexicon hierarchy and evolution.
- [FEDERATION.md](FEDERATION.md) - current federation implementation status vs roadmap.

## Reference And Change History
- [PROJECT.md](PROJECT.md) - product context and current scope.
- [CODEBASE_STRUCTURE.md](CODEBASE_STRUCTURE.md) - file and folder map.
- [CODEBASE_INSIGHTS.md](CODEBASE_INSIGHTS.md) - non-intuitive patterns and hidden knowledge.
- [ACCESSIBILITY.md](ACCESSIBILITY.md) - WCAG AA compliance, service contracts, testing.
- [ACCESSIBILITY_ARTIFACTS.md](ACCESSIBILITY_ARTIFACTS.md) - statement, AT matrix, release gate checklist.
- [API_CHANGELOG.md](API_CHANGELOG.md) - API-specific change log.
- [semantic_versioning/CHANGELOG.md](semantic_versioning/CHANGELOG.md) - release history.
- [semantic_versioning/v0.1.0.md](semantic_versioning/v0.1.0.md) - v0.1.0 historical notes.
- [adr/ADR-001-authorization-provider-architecture.md](adr/ADR-001-authorization-provider-architecture.md) - authorization provider architecture.
- [adr/ADR-002-outbox-pattern.md](adr/ADR-002-outbox-pattern.md) - outbox pattern for reliable messaging.
- [adr/ADR-003-css-layer-architecture.md](adr/ADR-003-css-layer-architecture.md) - CSS @layer architecture.
- [adr/ADR-004-accessibility-architecture.md](adr/ADR-004-accessibility-architecture.md) - accessibility service architecture.
- [adr/ADR-005-footer-customization.md](adr/ADR-005-footer-customization.md) - tenant footer customization.
