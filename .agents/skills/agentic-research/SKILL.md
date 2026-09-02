---
name: agentic-research
description: "Load when a task asks to research, verify, compare, or look up framework/package behavior, release notes, standards, RFCs, CVEs, or unfamiliar APIs; use repository evidence first, then official docs, and not for codebase navigation alone."
type: workflow
enforcement: suggest
priority: high
---

ABOUTME: Local-first research skill for repo inspection, official docs lookup, and safe external research.
ABOUTME: Read the linked resources before escalating beyond the repository.

# Agentic Research

## Non-Inferable Rules (Must Follow)
- Inspect the **local repository first**: code, tests, configuration, docs, and existing `.agents` guidance outrank everything else for repo behavior.
- Route broad codebase inventories and documentation discovery to an economical read-only scout. Give exact queries and use the locations-only result cap in `.agents/CONTEXT_ENGINEERING.md`.
- Keep a `path + heading/symbol + revision` ledger and never repeat an unchanged search or read already represented in the current context.
- Use **official documentation tooling** for framework, library, runtime, package, or migration uncertainty.
- Use **external research** only when the answer is not in the repo or official docs, or when you need standards, advisories, or ecosystem comparison.
- Protect sensitive data: never paste secrets, tokens, connection strings, private tenant data, PII, or unnecessary proprietary code into external tools.
- Summarize findings into a repo-relevant conclusion; do not dump raw search output into docs or answers.
- Validate relevance before continuing. If the source does not directly answer the repo question, keep searching or stop and state uncertainty.
- Do not guess APIs, package defaults, or breaking-change details when the repo or official docs can prove them.

## Resources (Load Only For The Named Need)
- [source-selection.md](resources/source-selection.md) - when the correct evidence tier is unclear.
- [security-boundaries.md](resources/security-boundaries.md) - before any external query.
- [verification-matrix.md](resources/verification-matrix.md) - when selecting verification for a changed artifact.

Do not load all resources by default. The scout returns findings and source handles, never raw files, search dumps, or copied documentation.

## Related Skills
- `clean-architecture-rules`
- `auth-patterns`

## Related Documentation
- [`AGENTS.md`](../../../AGENTS.md) - repo operating rules and tool hierarchy.
- [`docs/internal/GOVERNANCE.md`](../../../docs/internal/GOVERNANCE.md) - agentic engineering governance.
