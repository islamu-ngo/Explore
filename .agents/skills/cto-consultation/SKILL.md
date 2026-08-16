---
name: cto-consultation
description: Professional CTO consultation workflow for feature strategy, architecture direction, infrastructure choices, market positioning, and enterprise self-hosting tradeoffs.
type: workflow
enforcement: suggest
priority: high
---
<!-- ABOUTME: CTO consultation skill for product, architecture, infrastructure, and market-facing feature decisions. -->
<!-- ABOUTME: Grounds strategic advice in the ISLAMU Event architecture, governance docs, and self-hosting constraints. -->

## Purpose
Use this skill when the user wants strategic or architectural consultation rather than immediate implementation. It turns open-ended product, infrastructure, UX, market, compliance, or enterprise-readiness questions into a grounded recommendation with options, tradeoffs, risks, and a decision path.

## When to Load
- Keywords: CTO, consult, architecture decision, product direction, enterprise, self-hosting, market, competition, best practice, legal, juridical, infrastructure decision.
- Feature questions where the user asks whether to build, defer, make optional, make required, expose in UI, or choose between alternatives.
- Examples: scheduler status-surface optionality, organizer analytics dashboard, event discovery vs organization-centric UX, RabbitMQ/Cerbos/MinIO/provider choices, federation roadmap, extensibility packs.
- Any consulting answer that must balance small community deployments, enterprise deployments, white-label tenants, and the public ISLAMU-hosted instance.
- Use together with implementation skills if the consultation becomes a concrete code change.

## When NOT to Load
- Not for narrow code fixes where the desired behavior is already specified.
- Not for pure PRDs where the user explicitly asks only for a requirements document; use [../prd/SKILL.md](../prd/SKILL.md).
- Not for pure framework/API syntax questions where the answer is in official docs.
- Not for legal advice as a substitute for counsel; provide implementation conventions and risk framing only.
- Not for market or competitor claims unless current external evidence can be checked and dated.

## Must-Read Docs
- [../../../AGENTS.md](../../../AGENTS.md)
- [../../../docs/PROJECT.md](../../../docs/PROJECT.md)
- [../../../docs/ARCHITECTURE.md](../../../docs/ARCHITECTURE.md)
- [../../../docs/DOMAIN.md](../../../docs/DOMAIN.md)
- [../../../docs/API.md](../../../docs/API.md)
- [../../../docs/BLAZOR.md](../../../docs/BLAZOR.md)
- [../../../docs/SECURITY-MODEL.md](../../../docs/SECURITY-MODEL.md)
- [../../../docs/MULTI_TENANCY.md](../../../docs/MULTI_TENANCY.md)
- [../../../docs/SELF_HOSTING.md](../../../docs/SELF_HOSTING.md)
- [../../../docs/DEPLOYMENT_TIERS.md](../../../docs/DEPLOYMENT_TIERS.md)
- [../../../docs/CONFIGURATION.md](../../../docs/CONFIGURATION.md)
- [resources/consultation-framework.md](resources/consultation-framework.md)

## Top 5 Invariants
1. The recommendation must preserve the platform promise: enterprise-grade, self-hostable, tenant-governed, and usable by both small communities and larger operators.
2. Product advice must separate Instance, Tenant, Organization, Group, User, BFF, Client, and API authority instead of collapsing them into one admin or UI concept.
3. Infrastructure advice must treat optional dependencies as explicit deployment-mode or tier choices with health, fallback, and operator documentation impacts.
4. UX advice must keep HAL links as the source of truth for per-resource action affordances and keep the API as the hard authorization boundary.
5. Extensibility advice must choose Layer 1 core fields, Layer 2 typed sector schema, or Layer 3 governed custom properties before proposing storage or UI shape.

## Top 5 Anti-Patterns
1. Enterprise-only default, which raises the operational floor and harms single-organization or community self-hosters.
2. Local-role UX gating, which duplicates server authorization and breaks the HAL affordance contract.
3. Runtime-schema sprawl, which turns Layer 3 custom properties into a hidden domain model and bypasses typed policy semantics.
4. Dashboard-as-source-of-truth, which mistakes infrastructure internals such as scheduler dashboards for product/operator state.
5. Undated market claims, which make competitive or legal recommendations stale and unverifiable.

## Minimal Examples
```text
Consultation output shape:
1. Decision needed
2. Current repo facts
3. Options
4. Recommendation
5. Enterprise/self-hosting impact
6. Implementation path
7. Verification and documentation impact
```

```text
Example stance:
Quartz.NET is the single internal scheduler, including for Basic Dispatch Mode. Its status surface stays optional, instance-admin-only, and never the source of truth for email delivery state — `EmailDispatchOutbox` is.
```

## Verification Hooks
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AgentContextSchemaTests`
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AgentContextLinkTests`
- `dotnet build --configuration Release --verbosity quiet`

## Related Skills
- [../agentic-research/SKILL.md](../agentic-research/SKILL.md)
- [../clean-architecture-rules/SKILL.md](../clean-architecture-rules/SKILL.md)
- [../cqrs-mediatr-guidelines/SKILL.md](../cqrs-mediatr-guidelines/SKILL.md)
- [../blazor-ui-conventions/SKILL.md](../blazor-ui-conventions/SKILL.md)
- [../outbox-pattern/SKILL.md](../outbox-pattern/SKILL.md)
