ABOUTME: Defines the repository documentation architecture, ownership model, and quality gates.
ABOUTME: Keeps docs source-grounded, audience-oriented, and safe to evolve without a hosted docs site.

# Documentation Architecture

> **Audience:** Contributors | Operators | Admins | Integrators | AI agents
> **Status:** Implemented
> **Owner:** Contributor Experience
> **Last Verified:** 2026-09-02
> **Source Anchors:** `README.md`, `docs/index.md`, `docs/DOCUMENTATION_STYLE_GUIDE.md`

This repository uses Markdown-first documentation as the canonical operator, contributor, and agent knowledge base, complemented by our [official hosted public documentation portal](https://islamu.gitbook.io/islamu-event).

## Intent Model

Use Diátaxis intent categories to prevent one page from becoming a mixed manual:

| Intent | Reader Question | Use For | Example Docs |
|---|---|---|---|
| Tutorial | How do I learn the path once? | Guided first-run or first-contribution flows | `GETTING_STARTED.md`, `FIRST_CONTRIBUTION.md`, `DEVELOPER_GUIDE.md` |
| How-to | How do I complete this task safely? | Developer blueprints, operator/admin procedures and runbooks | `CONTRIBUTOR_RECIPES.md`, `SELF_HOSTING.md`, `BACKUP_RESTORE_UPGRADE.md` |
| Reference | What are the exact keys/contracts? | Stable facts, settings, APIs, commands | `CONFIGURATION.md`, `SECRETS.md`, `API.md` |
| Explanation | Why is the system designed this way? | Architecture, request flows, tradeoffs, governance | `ARCHITECTURE_OVERVIEW.md`, `REQUEST_FLOWS.md`, `ARCHITECTURE.md`, ADRs, `GOVERNANCE.md` |

If a page needs two intents, split the task steps from the reference table and link between them.

## Audience Paths

Use `README.md` as the public entry point. It should route new readers by task and audience without forcing them into the full documentation inventory. Use `docs/index.md` as the complete documentation map for readers who already know their task or need a specific reference.

| Audience | Start Here | Then Read |
|---|---|---|
| Evaluators | `README.md`, `PROJECT.md` | `ARCHITECTURE_OVERVIEW.md`, `SECURITY-MODEL.md`, `SELF_HOSTING.md` |
| Local developers | `GETTING_STARTED.md` | `DEVELOPER_GUIDE.md`, `TESTING.md`, `TROUBLESHOOTING.md` |
| Operators | `SELF_HOSTING.md` | `CONFIGURATION.md`, `SECRETS.md`, `OPERATIONS.md`, `BACKUP_RESTORE_UPGRADE.md`, `RELEASE_CHECKLIST.md` |
| Instance and tenant admins | `ADMIN_GUIDE.md` | `ADMIN_HIERARCHY.md`, `DEPLOYMENT_MODES.md`, `AUTHORIZATION_PATTERNS.md`, product feature docs |
| Integrators | `API_COOKBOOK.md` | `API.md`, `API_CHANGELOG.md`, `SECURITY-MODEL.md`, `CONFIGURATION.md` |
| Contributors | `DEVELOPER_GUIDE.md` | `ARCHITECTURE_OVERVIEW.md`, `REQUEST_FLOWS.md`, `CONTRIBUTOR_RECIPES.md`, `CONTRIBUTING.md`, `QUICK_REFERENCE.md` |
| AI agents | `AGENTS.md` | `.agents/contract/README.md`, `.agents/contract/intents.yaml`, `dev/_journal/README.md` |

## Canonical Docs And Owners

Each canonical doc has an owner category. Ownership means the category is responsible for accuracy, not that only that team may edit it.

| Owner | Canonical Docs |
|---|---|
| Platform/Ops | `SELF_HOSTING.md`, `BACKUP_RESTORE_UPGRADE.md`, `OPERATIONS.md`, `CONFIGURATION.md`, `RELEASE_CHECKLIST.md` |
| Security | `SECURITY_OVERVIEW.md`, `SECURITY-MODEL.md`, `SECRETS.md`, `AUTHORIZATION_PATTERNS.md`, `DEPLOYMENT_TIERS.md` |
| API | `API.md`, `API_CHANGELOG.md`, OpenAPI/client-generation guidance in `GOVERNANCE.md` |
| Frontend | `BLAZOR.md`, `DESIGN_SYSTEM.md`, `ACCESSIBILITY.md`, `RENDER_POLICIES.md` |
| Product/Admin | `ADMIN_HIERARCHY.md`, `ADMISSION_AND_REGISTRATION.md`, feature/admin workflow docs |
| Contributor Experience | `DEVELOPER_GUIDE.md`, `ARCHITECTURE_OVERVIEW.md`, `REQUEST_FLOWS.md`, `RECORD_CONTRACTS.md`, `CONTRIBUTOR_RECIPES.md`, `CONTRIBUTING.md`, `TESTING.md`, `DOCUMENTATION_STYLE_GUIDE.md`, this document |
| Agent Context | `AGENTS.md`, `AGENTIC_CONTEXT_ENGINEERING.md`, `.agents/contract/`, `.agents/skills/`, `dev/_journal/` |

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

## Dual-Documentation Architecture (`docs/public/` vs `docs/internal/`)

To preserve clarity for all audiences, the documentation is strictly partitioned into two parallel tracks:

```text
docs/
├── README.md               <-- The Grand Router
├── public/                 <-- GitBook Hosted Portal (Adopters, Operators & Integrators)
└── internal/               <-- Engineering Brain (Contributors & AI Coding Agents)
```

1. **`docs/public/` (Public GitBook Portal)**: Curated, task-oriented guides for community admins, operators deploying with Docker/Coolify, and external API consumers. Avoids internal CQRS or EF Core plumbing.
2. **`docs/internal/` (Engineering Brain)**: Source of technical truth, Clean Architecture rules, invariants, MediatR request shapes, database locks, tenant filters, and context engineering contracts.

### Documentation Twin Parity Matrix

Every public guide in `docs/public/` projects technical facts from an authoritative technical anchor in `docs/internal/`. Whenever behavior changes, both twins must be updated in the same pull request:

| Public Adopter Guide (`docs/public/`) | Technical Source Anchor (`docs/internal/`) | Synchronized Content |
|---|---|---|
| `documentation/readme/self-hosting/docker-compose.md` | `SELF_HOSTING.md` | Service topologies, compose variables, volume persistence, ports |
| `documentation/readme/self-hosting/coolify-cerbos-traefik.md` | `CERBOS_COOLIFY.md` | Coolify deployment templates, Traefik reverse-proxy labels, Cerbos sidecar |
| `documentation/readme/configuration-and-operations/environment-variables.md` | `CONFIGURATION.md` | Required/optional env vars, default values, fallback behaviors |
| `documentation/readme/configuration-and-operations/secrets.md` | `SECRETS.md` | Keycloak secrets, database credentials, data protection key rings |
| `documentation/readme/configuration-and-operations/backup-restore-upgrade.md` | `BACKUP_RESTORE_UPGRADE.md` | Backup routines, restore verification, database migration commands |
| `documentation/readme/events-and-ticketing/custom-properties.md` | `CUSTOM_PROPERTIES.md` | Custom registration property types, schema rules, projection guarantees |
| `documentation/readme/security-and-identity/authentication.md` | `AUTHORIZATION.md` & `SECURITY-MODEL.md` | Keycloak realm setup, OIDC client redirect URIs, tenant isolation |
| `api-reference/readme/api-cookbook.md` | `API_COOKBOOK.md` & `API.md` | API integration workflows, authentication headers, error shapes |
| `api-reference/readme/api-changelog.md` | `API_CHANGELOG.md` | Breaking changes, new endpoints, deprecated operations |

### Dual-Documentation Parity Protocol

1. **Adopter Projection Rule**: When updating a public doc in `docs/public/`, write instructions from the perspective of an operator or integrator. Do not mention internal C# classes, MediatR handlers, or EF Core entity configurations.
2. **Technical Depth Rule**: When updating an internal doc in `docs/internal/`, document the full architectural reality: class names, invariants, concurrency behaviors, tenant filters, and rollback mechanics.
3. **Intent Enforcement**: Every intent in `.agents/contract/intents.yaml` affecting external contracts declares both internal and public twins in `docs_to_update`.
