ABOUTME: Roadmap for a future hosted public documentation website.
ABOUTME: Keeps public-site planning separate from the current repository Markdown source of truth.

# Public Documentation Roadmap

> **Audience:** Contributors | Operators | AI agents
> **Status:** Planned
> **Owner:** Contributor Experience
> **Last Verified:** 2026-05-06
> **Source Anchors:** `docs/DOCUMENTATION_ARCHITECTURE.md`, `docs/index.md`, `docs/docs-website/README.md`

The current documentation implementation keeps repository Markdown as the source of truth. A future public documentation website may mirror selected pages, but it must not become a competing source of operational truth.

## Deferred Until Repository Docs Are Stable

Do not introduce a docs-site generator, publishing pipeline, or public-site navigation model until:

1. canonical repository docs pass metadata, source-anchor, placeholder, and link validation;
2. release documentation and operator runbooks are accurate enough for self-hosters;
3. the project chooses which pages are safe for public mirroring;
4. ownership is clear for keeping mirrored public pages synchronized with source docs.

## Candidate Public Pages

These pages are likely public-site candidates once the hosted docs phase starts:

| Candidate | Source Of Truth | Public-Site Notes |
|---|---|---|
| Product overview | `README.md`, `docs/PROJECT.md` | Keep concise and evaluator-focused. |
| Getting started | `docs/GETTING_STARTED.md`, `docs/SELF_HOSTING.md` | Mirror only verified runnable paths. |
| Self-hosting | `docs/SELF_HOSTING.md`, `docs/BACKUP_RESTORE_UPGRADE.md` | Include clear warnings for unverified restore/rollback steps. |
| API integration | `docs/API_COOKBOOK.md`, `docs/API.md` | Link generated OpenAPI reference rather than duplicating endpoint contracts. |
| Admin guide | `docs/ADMIN_GUIDE.md` | Include implemented-vs-planned labels for admin surfaces. |
| Accessibility | `docs/ACCESSIBILITY.md`, `docs/ACCESSIBILITY_ARTIFACTS.md` | Publish release-specific evidence only after validation is filled in. |

## Non-Goals For Current Phase

- No MkDocs, Docusaurus, VitePress, or other generator configuration.
- No hosted docs deployment workflow.
- No broad rewrite of repository docs into marketing pages.
- No duplicate copies of configuration tables or operator runbooks.

## Required Future Contract

When public docs work begins, each mirrored page needs:

- a source repository path;
- a sync owner;
- a freshness policy;
- a release-docs impact check;
- a clear warning when the public page intentionally omits contributor-only or operator-only detail.
