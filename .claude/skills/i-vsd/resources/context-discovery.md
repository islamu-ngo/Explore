<!-- ABOUTME: Context discovery protocol for I-VSD reviews across repository artifacts, MCPs, CLIs, skills, and user-provided sources. -->
<!-- ABOUTME: Ensures moral design analysis starts with an explicit context inventory before deep review or report generation. -->

# Context Discovery

Use this resource before any repository-based or project-context-based I-VSD review. Moral, ethical, and values analysis must not be based only on code, a diff, or a single prompt when broader evidence may exist in the repository, connected tools, installed CLIs, available skills, documentation platforms, or user-provided filesystem paths.

## Discovery Rule

Start with a high-level context inventory before writing findings or generating report files. Prefer project evidence over assumptions. If useful evidence exists but was not reviewed, mark it as `Missing Evidence` or `Not Reviewed`; do not invent project intent.

The first user-facing response after invocation must summarize the available context and ask for confirmation before the deep dive unless the user explicitly requested only the action menu or a short advisory that does not require repository/project research.

## Context Inventory Gate

Before deep analysis, quickly inventory all context channels that may hold product, stakeholder, policy, planning, operational, or implementation evidence:

1. Local repository and filesystem context: docs, README files, text artifacts, code, tests, configuration, reports, plans, and generated outputs.
2. Available agent skills, slash commands, or workflow helpers that can retrieve or interpret project context.
3. Connected MCP servers or tool integrations exposed by the host agent, especially Jira, Confluence, Plane, Linear, GitHub, GitLab, Notion, Slack, Google Drive, knowledge-base, issue-tracker, product-doc, observability, or documentation search integrations.
4. Installed or authenticated CLIs that may expose project context, such as issue trackers, docs platforms, cloud platforms, source control hosts, incident tools, support tools, or analytics tools.
5. User-provided filesystem paths, exported documents, screenshots, URLs, or credentials/access instructions.

Only list context channels that are visible or reasonably checkable in the current environment. Do not pretend an MCP server, CLI, skill, or external platform is available when it is not visible. Do not request credentials directly; ask the user to connect the relevant MCP/CLI or provide exported context/path access.

## First Response Contract

After the inventory gate and before the extensive deep dive, respond to the user with one of these shapes:

```text
I found limited context for an I-VSD review.

Available context:
- <repository docs/code/config/tests found>
- <available skills/MCPs/CLIs found, or "No relevant external context integrations visible">

Missing or weak context:
- <policies, product docs, issue tracker, roadmap, support logs, incident docs, external docs platforms, etc.>

You can improve the review by connecting an MCP/CLI for <Jira/Confluence/Plane/etc.>, or by giving me filesystem paths/exports for additional project context. I can still proceed with clear `Missing Evidence` boundaries if you want.
```

```text
I found enough initial context to begin an I-VSD review.

Available context:
- <local docs/code/config/tests/reports>
- <relevant connected MCPs/CLIs/skills>

Planned outputs:
- `islamic-value-sensitive-design/i-vsd-<action>.md`
- <additional mapped files, including `i-vsd-review-index.md` for multi-report work>

Tell me "agreed" and I will start the deep review and write these Markdown files, or provide corrections/additional context first.
```

If the user confirms, then perform the deeper repository/tool search and write the mapped report files. If the user adds paths or connects tools, include them in the evidence plan before writing. If the user declines or changes scope, reroute through [action-routing.md](action-routing.md).

## Sources To Search

Look beyond source code. Search and read relevant files such as:

- `README*`, `CONTRIBUTING*`, `CHANGELOG*`, `SECURITY*`, `CODE_OF_CONDUCT*`, governance files, roadmap files, and release notes.
- Markdown, plain text, reStructuredText, AsciiDoc, MDX, and other human-readable documentation.
- Product briefs, PRDs, epics, user stories, task files, architecture decision records, design notes, strategy docs, research notes, meeting notes, and implementation plans.
- Terms, privacy notices, acceptable-use policies, moderation policies, data-retention notes, support policies, incident reports, postmortems, risk registers, and compliance documents.
- Configuration and infrastructure files that reveal provider responsibilities, such as authentication, authorization, telemetry, logging, backups, data storage, retention, deployment, billing, AI, moderation, or third-party integrations.
- Tests and examples that document promised behavior, user flows, policy enforcement, safety expectations, or edge cases.

Also check non-repository context sources when visible and relevant:

- MCP servers or hosted integrations that expose tickets, epics, product documentation, architecture notes, decisions, customer support, incidents, analytics, legal/policy docs, or knowledge-base articles.
- CLI tools for project management, documentation platforms, cloud resources, source control hosts, support desks, observability stacks, incident systems, or deployment environments.
- Available skills, prompts, commands, or local workflow helpers that know how to retrieve context from the current organization.
- User-specified directories outside the repository, exported documentation bundles, PDFs, screenshots, URLs, meeting notes, or research archives.

## Search Procedure

1. Identify the requested I-VSD action and likely domains from [action-routing.md](action-routing.md).
2. Inventory local docs/text artifacts, available skills, connected MCP servers/tools, and relevant authenticated CLIs before reading code-heavy areas.
3. Send the first response contract to the user, listing available context, missing context, planned output files, and whether external MCP/CLI/filesystem context would improve the review.
4. After user confirmation, search for value-bearing terms such as privacy, consent, retention, telemetry, tracking, ads, pricing, billing, refund, cancellation, moderation, appeal, abuse, safety, accessibility, security, admin, partner, sponsor, AI, model, ranking, recommendation, policy, terms, rights, user data, deletion, export, lock-in, incident, and support.
5. Search for project-specific vocabulary from the user prompt, product name, feature name, domain model, connected project tools, or action under review.
6. Read the most relevant documents and external context records; cite them in `Evidence Reviewed` with paths, tool/source names, record identifiers, URLs, or clear descriptions.
7. Only then inspect code, diffs, configuration, tests, runtime behavior, or external operational records needed to validate whether implementation matches documented commitments.

## Evidence Priority

Use this order when evidence conflicts:

1. Current implementation and tests for what the system actually does.
2. Current product, policy, legal, operational, and architecture documentation for what the provider commits to do.
3. Connected project systems such as issue trackers, documentation platforms, roadmaps, support desks, incident tools, and knowledge bases for current organizational commitments or operating reality.
4. Planning documents, roadmaps, issues, and design notes for intended direction.
5. User-provided context for unstored business or stakeholder facts.
6. External standards or official documentation only when project context cannot answer the question.

Conflicts are findings. For example, if documentation promises deletion, portability, consent, refunds, appeals, or safety review but implementation evidence does not support that promise, report the mismatch under Truthfulness, Trust, Rights of People, Promise-Keeping, or Non-Harm as applicable.

## Output Requirements

Every repository-based report must include:

- `Evidence Reviewed` listing the code, docs, text artifacts, policies, configs, tests, or user-provided context used.
- `Missing Evidence` listing relevant docs or artifacts that were unavailable, skipped, stale, contradictory, or outside scope.
- `Context Inventory` or equivalent notes covering local repository context, visible MCP/tool integrations, relevant CLIs, available skills, and user-provided paths considered before deep review.
- Findings that distinguish documented commitments from implemented behavior.
- Claim boundaries that state the review is I-VSD design reasoning and traceability, not certification or proof.

## Safety Boundaries

Do not expose secrets, private tenant data, PII, credentials, or proprietary details unnecessarily in generated reports. Summarize sensitive evidence by category when exact text is not needed.
