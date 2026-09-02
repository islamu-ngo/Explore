<!-- ABOUTME: Local active workstreams and ephemeral agent working memory directory. -->
<!-- ABOUTME: Documents the gitignored Dev-Doc Triad pattern, tool conventions, and graduation rules. -->

# Active Workstreams (`dev/active/`)

This directory holds **active, local development workstreams** governed by the Dev-Doc Triad (`*-plan.md`, `*-tasks.md`, `*-context.md`).

## Gitignore Policy & Rationale

- Subdirectories under `dev/active/` are **gitignored** (`dev/active/*`).
- **Why?** Task checkboxes, granular phase execution statuses, dirty worktree logs, and session contexts are ephemeral local working memory. Gitignoring them prevents commit log churn, PR noise, and branch merge conflicts.
- **Tracked Anchor:** Only `dev/active/README.md` is committed to git to anchor the directory structure.

## Tool Access Guidelines for AI Agents & Developers

- **Direct File Tools:** AI agents and IDEs should access files in `dev/active/<task>/` using native harness file tools by deterministic path.
- **No Ad-Hoc Bash Script Hacks:** Direct harness file tools interact directly with the OS filesystem and work seamlessly on gitignored files. Do not use ad-hoc shell scripts for file manipulation (respecting Critical Rule #9).
- **Discovery:** If an agent needs to discover active workstreams, use native harness directory listing or inspect the known task path. Note that recursive fuzzy codebase search tools skip gitignored paths by default.

## Graduation of Durable Artifacts

Active workstream docs are working memory during execution. Durable knowledge must not remain trapped in an ephemeral folder:
- **Durable Decisions & Findings:** Record in `dev/_journal/` (findings, decisions).
- **Architectural Standards:** Record as an ADR in `docs/internal/adr/`.
- **Deep Research / Analysis:** Record in `dev/report/`.
- **Task Completion:** When a workstream is verified and its production deliverables are committed to git, its local folder in `dev/active/<task>/` can be safely removed or archived locally.
