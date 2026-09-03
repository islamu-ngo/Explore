<!-- ABOUTME: Authoring, styling, and structuring GitBook markdown documentation pages. -->
<!-- ABOUTME: Covers frontmatter rules, rich blocks syntax, space/page variables, and Git Sync editing practices. -->

# GitBook Content Authoring & Formatting

A guide for writing, authoring, editing, and formatting GitBook documentation pages in Git-synced repositories, IDEs, or local markdown editors outside the GitBook UI.

## File Organization & Space Anatomy

GitBook spaces follow this conventional layout:
```
/
  .gitbook/
    assets/              # GitBook-managed images and files
    includes/            # Reusable content snippets
    vars.yaml            # Space-level variables
  .gitbook.yaml          # Optional configuration override
  README.md              # Space homepage
  SUMMARY.md             # Table of contents
  getting-started/
    installation.md
    quickstart.md
```

## Frontmatter Standards

Every markdown page can configure frontmatter at the very top of the file:
```yaml
---
description: "Page description for search and SEO"
icon: book-open
hidden: false
vars:
  page_variable: value
layout:
  width: default  # or 'wide' for marketing/landing pages
  tableOfContents:
    visible: true
  pagination:
    visible: true
---
```

### Critical Frontmatter Rule
**Always quote `description:` values containing `:`, `#`, or other YAML-significant characters.** Unquoted colons cause silent Git Sync parse failures with no error message displayed in the UI.
Icon names are Font Awesome icons without the `fa-` prefix (e.g. `book-open`, `bolt`, `code`, `house`).

## Space & Page Variables

- **Space-level variables**: Define in `/.gitbook/vars.yaml`:
  ```yaml
  support_email: "support@example.com"
  min_version: "2.4.0"
  ```
  Render in markdown with: `<code class="expression">space.vars.support_email</code>`.
- **Page-level variables**: Define under frontmatter `vars:` and render with: `<code class="expression">page.vars.variableName</code>`.

## Specialized Rich Blocks

GitBook supports rich blocks that elevate documentation beyond plain prose. Actively select the right block for the job:

| Need | Tag / Syntax | Purpose / Behavior |
|---|---|---|
| Sequential steps | `{% stepper %}...{% endstepper %}` | Step progression with numbered indicators |
| Multi-language / alternatives | `{% tabs %}...{% endtabs %}` | Language or platform toggle without page clutter |
| Alerts / Callouts | `{% hint style="info\|warning\|danger\|success" %}` | Colored callout block |
| Expandable / Details | `<details><summary>Title</summary>Content</details>` | Collapsible technical depth |
| Side-by-side columns | `{% columns %}...{% endcolumns %}` | Parallel 2-column layout |
| Changelog / Timeline | `{% updates %}...{% endupdates %}` | Dated entries with tag filtering and RSS feed |
| Visual card grid | `<table data-view="cards">` | Interactive navigation card matrix |
| Reusable snippets | `{% include ".gitbook/includes/file.md" %}` | Single-source shared boilerplate |
| Flowcharts / Sequences | ` ```mermaid ` | Native diagram rendering (no ASCII boxes) |
| API Endpoints | `{% openapi ... %}` | Embedded interactive API operations |

For full syntax and code examples, consult [blocks.md](blocks.md) and [block-ecosystem.md](block-ecosystem.md).

## Links & Navigation

- **Relative Links (Same Space)**: `[Quickstart](getting-started/quickstart.md)` or `[Overview](../README.md)`.
- **Cross-Space Links (Different Space)**: `https://app.gitbook.com/s/<spaceId>/<path>`. Never use relative paths across space boundaries (they fail to resolve). For spaces not yet created, use `XSPACE_<KEY>` sentinels resolved by [cross-space-links.md](cross-space-links.md).
- **Page Renames**: GitBook automatically maintains redirects when pages are renamed or moved within a space.

## Working with Git Sync

When GitBook is connected to Git, edits flow bi-directionally.

### Choosing Git Sync vs. Direct Change Request Push
1. **Prefer Git Sync**: For spaces with active Git Sync and local checkout availability, edit files directly in Git, commit, and push. Git Sync automatically propagates changes to GitBook.
2. **Direct CR Push**: Use direct API/MCP change-request pushes only when:
   - Space has no Git Sync configured yet.
   - No local Git repository checkout is accessible.
   - The edit is a minor, isolated typo fix.

### Git Sync Branch Previews
When pushing docs changes to a Git branch with an open pull request, GitBook builds a preview and posts the link as a commit status on GitHub/GitLab. Inspect the commit status to surface the rendered preview URL to the user. See [git-sync-previews.md](git-sync-previews.md).

### Two-Link Rule for Change Requests
Whenever an edit is made through a GitBook Change Request, **always report both links**:
1. Editor / diff view: `urls.app` from the change request.
2. Rendered site preview: Site URL with `~/changes/<number>/` appended (e.g. `https://docs.example.com/~/changes/42/`).

## Related Resources

- [blocks.md](blocks.md) — Comprehensive syntax for all custom GitBook blocks.
- [block-ecosystem.md](block-ecosystem.md) — Selection heuristics and smell-vs-fix decision table.
- [frontmatter.md](frontmatter.md) — Full frontmatter options, layout flags, covers, and conditional expressions.
- [configuration.md](configuration.md) — `.gitbook.yaml`, `.gitbook/` structure, and `SUMMARY.md` grammar.
- [markdown.md](markdown.md) — Markdown nuances, Mermaid diagrams, TeX math, and SVG handling.
- [git-sync-previews.md](git-sync-previews.md) — Reading branch previews from GitHub/GitLab commit statuses.
