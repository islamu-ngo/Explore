---
name: agentic-research
description: Local-first research workflow for selecting the right evidence source before using official docs or external research.
type: domain
enforcement: suggest
priority: high
---

ABOUTME: Local-first research skill for repo inspection, official docs lookup, and safe external research.
ABOUTME: Read the linked resources before escalating beyond the repository.

# Agentic Research

> **Purpose**
>
> Select the safest and most authoritative evidence source for the task. Start with this repository, escalate to official docs only when needed, and use external research sparingly and safely.

## When This Skill Activates
- Keywords: official docs, breaking change, migration, upgrade, nuget, package behavior, RFC, standard, security advisory, CVE
- File patterns: `**/*.csproj`, `**/Directory.Packages.props`, `**/global.json`, `**/*.props`, `**/*.targets`

## Non-Inferable Rules (Must Follow)
- Inspect the **local repository first**: code, tests, configuration, docs, and existing `.claude` guidance outrank everything else for repo behavior.
- Use **official documentation tooling** for framework, library, runtime, package, or migration uncertainty.
- Use **external research** only when the answer is not in the repo or official docs, or when you need standards, advisories, or ecosystem comparison.
- Protect sensitive data: never paste secrets, tokens, connection strings, private tenant data, PII, or unnecessary proprietary code into external tools.
- Summarize findings into a repo-relevant conclusion; do not dump raw search output into docs or answers.
- Validate relevance before continuing. If the source does not directly answer the repo question, keep searching or stop and state uncertainty.
- Do not guess APIs, package defaults, or breaking-change details when the repo or official docs can prove them.

## Activation Guidance
- **Unfamiliar .NET / NuGet / framework API**: inspect local usage first, then use official docs.
- **Migration or upgrade uncertainty**: compare repo config with official release notes and migration docs.
- **Standards / RFC / advisory work**: use external research after local context is understood.
- **Security or breaking change investigation**: verify against official docs and high-authority sources before proposing changes.
- **Architecture comparison**: anchor the comparison in current repo constraints before looking outward.

## Resources (Read Before Applying)
- [source-selection.md](resources/source-selection.md) - source hierarchy and escalation rules.
- [security-boundaries.md](resources/security-boundaries.md) - safe external-tool usage and data minimization.
- [verification-matrix.md](resources/verification-matrix.md) - proportional verification by change type.

## Related Skills
- `clean-architecture-rules`
- `auth-patterns`

## Related Documentation
- [`CLAUDE.md`](../../../CLAUDE.md) - repo operating rules and tool hierarchy.
- [`docs/GOVERNANCE.md`](../../../docs/GOVERNANCE.md) - agentic engineering governance.
