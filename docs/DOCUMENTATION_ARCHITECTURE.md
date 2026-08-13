ABOUTME: Defines the repository documentation architecture, ownership model, and quality gates.
ABOUTME: Keeps docs source-grounded, audience-oriented, and safe to evolve without a hosted docs site.

# Documentation Architecture

> **Audience:** Contributors | Operators | Admins | Integrators | AI agents
> **Status:** Implemented
> **Owner:** Contributor Experience
> **Last Verified:** 2026-06-12
> **Source Anchors:** `README.md`, `docs/index.md`, `docs/DOCUMENTATION_STYLE_GUIDE.md`

This repository uses Markdown-first documentation as the canonical operator, contributor, and agent knowledge base. A separately hosted public docs website is deferred until repository docs are accurate, validated, and release-ready.

## Intent Model

Use Diátaxis intent categories to prevent one page from becoming a mixed manual:

| Intent | Reader Question | Use For | Example Docs |
|---|---|---|---|
| Tutorial | How do I learn the path once? | Guided first-run or first-contribution flows | `GETTING_STARTED.md`, future `FIRST_CONTRIBUTION.md` |
| How-to | How do I complete this task safely? | Operator/admin procedures and runbooks | `SELF_HOSTING.md`, `BACKUP_RESTORE_UPGRADE.md` |
| Reference | What are the exact keys/contracts? | Stable facts, settings, APIs, commands | `CONFIGURATION.md`, `SECRETS.md`, `API.md` |
| Explanation | Why is the system designed this way? | Architecture, tradeoffs, governance | `ARCHITECTURE.md`, ADRs, `GOVERNANCE.md` |

If a page needs two intents, split the task steps from the reference table and link between them.

## Audience Paths

Use `README.md` as the public entry point. It should route new readers by task and audience without forcing them into the full documentation inventory. Use `docs/index.md` as the complete documentation map for readers who already know their task or need a specific reference.

| Audience | Start Here | Then Read |
|---|---|---|
| Evaluators | `README.md`, `PROJECT.md` | `ARCHITECTURE.md`, `SECURITY-MODEL.md`, `SELF_HOSTING.md` |
| Local developers | `GETTING_STARTED.md` | `TESTING.md`, `TROUBLESHOOTING.md` |
| Operators | `SELF_HOSTING.md` | `CONFIGURATION.md`, `SECRETS.md`, `OPERATIONS.md`, `BACKUP_RESTORE_UPGRADE.md`, `RELEASE_CHECKLIST.md` |
| Instance and tenant admins | `ADMIN_GUIDE.md` | `ADMIN_HIERARCHY.md`, `DEPLOYMENT_MODES.md`, `AUTHORIZATION_PATTERNS.md`, product feature docs |
| Integrators | `API_COOKBOOK.md` | `API.md`, `API_CHANGELOG.md`, `SECURITY-MODEL.md`, `CONFIGURATION.md` |
| Contributors | `FIRST_CONTRIBUTION.md` | `CONTRIBUTING.md`, `TESTING.md`, `QUICK_REFERENCE.md`, `DOCUMENTATION_STYLE_GUIDE.md` |
| AI agents | `AGENTS.md` | `.agents/contract/README.md`, `.agents/contract/intents.yaml`, `dev/_journal/README.md` |

## Canonical Docs And Owners

Each canonical doc has an owner category. Ownership means the category is responsible for accuracy, not that only that team may edit it.

| Owner | Canonical Docs |
|---|---|
| Platform/Ops | `SELF_HOSTING.md`, `BACKUP_RESTORE_UPGRADE.md`, `OPERATIONS.md`, `CONFIGURATION.md`, `RELEASE_CHECKLIST.md` |
| Security | `SECURITY.md`, `SECRETS.md`, `AUTHORIZATION_PATTERNS.md`, `DEPLOYMENT_TIERS.md` |
| API | `API.md`, `API_CHANGELOG.md`, OpenAPI/client-generation guidance in `GOVERNANCE.md` |
| Frontend | `BLAZOR.md`, `DESIGN_SYSTEM.md`, `ACCESSIBILITY.md`, `RENDER_POLICIES.md` |
| Product/Admin | `ADMIN_HIERARCHY.md`, feature/admin workflow docs |
| Contributor Experience | `CONTRIBUTING.md`, `TESTING.md`, `DOCUMENTATION_STYLE_GUIDE.md`, this document |
| Agent Context | `AGENTS.md`, `.agents/contract/`, `.agents/skills/`, `dev/_journal/` |

## Metadata Policy

New canonical docs and operator-critical docs must include this block immediately below the title:

```markdown
> **Audience:** Operators | Contributors | Admins | Integrators | AI agents
> **Status:** Implemented | Draft | Planned | Mixed
> **Owner:** Platform/Ops | Security | API | Frontend | Product/Admin | Contributor Experience | Agent Context
> **Last Verified:** YYYY-MM-DD
> **Source Anchors:** `path/one`, `path/two`
```

Rules:

- `Audience` can list multiple audience labels separated by `|`.
- `Status` must describe the page as a whole; sections that are not implemented must also be labeled in the section text.
- `Owner` must use one of the owner categories in this page.
- `Last Verified` is the date source anchors were checked, not the edit date for grammar-only changes.
- `Source Anchors` must point to real files or directories that prove the behavior.

Legacy docs can migrate gradually. Do not add metadata mechanically to low-value pages without checking their source anchors.

## Source-Anchor Policy

Documentation must prefer source-grounded claims over inferred behavior:

- Runtime/service facts anchor to code or infrastructure files such as `docker-compose.yml`, `Explore.AppHost/`, or `Explore.API/Program.cs`.
- Configuration tables anchor to binding or compatibility code such as `Explore.API/Extensions/ConfigurationExtensions.cs`.
- Testing commands anchor to `docs/TESTING.md`, `.github/workflows/`, and the relevant test project files.
- Roadmap or future behavior must be explicitly marked `Planned` or `Draft` and must not be presented as implemented.

When a source anchor and a doc disagree, update the doc or create a task to reconcile the source. Do not preserve stale examples for narrative continuity.

## Docs Impact Contract

Every non-trivial change must record one of these outcomes in the PR or dev handoff:

| Outcome | Meaning |
|---|---|
| Updated | Docs changed in the same PR because behavior, commands, config, or operations changed. |
| Not needed | The change is internal and does not affect documented behavior. |
| Deferred | Docs impact exists but is intentionally split; include the follow-up path and reason. |

API contract, operator, security, onboarding, and release changes should default to `Updated` unless proven otherwise.

## Public Docs Website Boundary

`docs/docs-website/` is parked for this phase. Repository Markdown remains the source of truth until the canonical docs, metadata, and release contract are stable. A future public site may mirror selected pages, but it must not become a second source of truth.
