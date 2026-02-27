ABOUTME: Entry point for project documentation with high-signal navigation only.
ABOUTME: Prioritizes non-inferable rules and operational docs over generated or repetitive detail.

# Documentation Index

## Read First
- [README.md](../README.md) - product overview and quick start.
- [CLAUDE.md](../CLAUDE.md) - project rules used by contributors and AI agents.
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - implementation constraints that are easy to violate.

## Core Architecture
- [ARCHITECTURE.md](ARCHITECTURE.md) - layer boundaries, BFF model, CQRS flow.
- [DOMAIN.md](DOMAIN.md) - domain entities and important relationships.
- [API.md](API.md) - API conventions, HAL behavior, filters, caching, error shape.
- [BLAZOR.md](BLAZOR.md) - client architecture, render policy, service patterns.

## Governance And Rules
- [GOVERNANCE.md](GOVERNANCE.md) - design and code governance.
- [NAMING_CONVENTIONS.md](NAMING_CONVENTIONS.md) - naming rules for entities, CQRS, services, tests.
- [TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md) - placeholder syntax used in generic templates.
- [CONTRIBUTING.md](CONTRIBUTING.md) - contribution workflow and validation steps.

## Runtime And Operations
- [CONFIGURATION.md](CONFIGURATION.md) - environment/config settings and secret providers.
- [SECURITY.md](SECURITY.md) - auth/authz model and trust boundaries.
- [OPERATIONS.md](OPERATIONS.md) - deployment, health, observability.
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
- [FEDERATION.md](FEDERATION.md) - current federation implementation status vs roadmap.

## Reference And Change History
- [PROJECT.md](PROJECT.md) - product context and current scope.
- [API_CHANGELOG.md](API_CHANGELOG.md) - API-specific change log.
- [semantic_versioning/CHANGELOG.md](semantic_versioning/CHANGELOG.md) - release history.
- [semantic_versioning/v0.1.0.md](semantic_versioning/v0.1.0.md) - v0.1.0 historical notes.
- [adr/ADR-001-authorization-provider-architecture.md](adr/ADR-001-authorization-provider-architecture.md) - authorization architecture decision record.
