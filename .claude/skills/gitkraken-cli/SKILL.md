---
name: gitkraken-cli
description: Git operations via GitKraken CLI MCP tools. Covers branching, commits, PRs, stashing, and issue workflows with conventional-commit formatting.
type: workflow
enforcement: guide
priority: high
---

ABOUTME: GitKraken CLI MCP tool usage patterns for git operations.
ABOUTME: Always loads conventional-commit skill for commit message formatting.

# GitKraken CLI MCP Tools

> **Git Operations via MCP**
>
> Use GitKraken MCP tools instead of raw `git` CLI commands.
> Commit messages follow the `conventional-commit` skill format.

## Purpose

Provide structured git workflows using the GitKraken CLI MCP server. These tools offer richer integration than raw git commands — branch management, PR creation, issue linking, and intelligent commit composition.

## When This Skill Activates

- Any git operation: commit, branch, push, pull, stash, checkout
- PR creation, review, or management
- Issue lookup or linking
- Keywords: commit, branch, push, pull, PR, merge, stash, blame, worktree

## Prerequisite

**Always load the `conventional-commit` skill** when this skill is active. Commit messages must follow the conventional-commit format defined there.

## Available Tools

### Core Git Operations

| Tool | Purpose | Key Parameters |
|---|---|---|
| `git_status` | Working tree status | `directory` |
| `git_add_or_commit` | Stage files or create commits | `directory`, `action` (`add`/`commit`), `files[]`, `message` |
| `git_log_or_diff` | View history or changes | `directory`, `action` (`log`/`diff`), `revision_range`, `since`, `until`, `authors[]` |
| `git_branch` | List or create branches | `directory`, `action` (`list`/`create`), `branch_name` |
| `git_checkout` | Switch branches | `directory`, `branch` |
| `git_fetch` | Fetch from remote | `directory` |
| `git_pull` | Pull from remote | `directory` |
| `git_push` | Push to remote | `directory` |
| `git_stash` | Stash changes | `directory`, `name`, `include_untracked`, `staged_only` |
| `git_blame` | Line-by-line attribution | `directory`, `file` |
| `git_worktree` | Manage worktrees | `directory`, `action` (`list`/`add`), `path`, `branch` |

### GitLens Features

| Tool | Purpose | When to Use |
|---|---|---|
| `gitlens_commit_composer` | AI-assisted commit organization | Large changesets that need splitting into logical commits |
| `gitlens_launchpad` | Prioritized PR dashboard | Checking open PRs, finding what needs attention |
| `gitlens_start_review` | PR review in dedicated worktree | Code review workflows |
| `gitlens_start_work` | Branch from issue with linking | Starting work on a tracked issue |

### Issue Management

| Tool | Purpose | Key Parameters |
|---|---|---|
| `issues_assigned_to_me` | List my issues | `provider` |
| `issues_get_detail` | Get issue details | `provider`, `issue_id`, `repository_name`, `repository_organization` |
| `issues_add_comment` | Comment on issue | `provider`, `issue_id`, `comment` |

### Pull Request Management

| Tool | Purpose | Key Parameters |
|---|---|---|
| `pull_request_create` | Create PR | `provider`, `repository_name`, `repository_organization`, `title`, `source_branch`, `target_branch` |
| `pull_request_get_detail` | Get PR details | `provider`, `pull_request_id`, `repository_name`, `repository_organization` |
| `pull_request_get_comments` | List PR comments | `provider`, `pull_request_id`, `repository_name`, `repository_organization` |
| `pull_request_create_review` | Submit PR review | `provider`, `pull_request_id`, `review`, `approve` |
| `pull_request_assigned_to_me` | List my PRs | `provider` |

## Non-Inferable Rules (Must Follow)

1. **`directory` is always required** — use the workspace root: `/home/amir/ISLAMU/Github/Event`.
2. **`provider` defaults to `"github"`** — always pass it explicitly for clarity.
3. **Repository coordinates**: `repository_organization` = `"IslamuOrg"`, `repository_name` = `"Event"`.
4. **Commit messages follow `conventional-commit` skill** — load it and apply format.
5. **Always `git_status` before committing** — verify what will be staged.
6. **Never `git_push --force`** unless user explicitly requests it.
7. **`git_add_or_commit` with `action: "add"`** stages files; `action: "commit"` creates the commit from staged changes.
8. **Prefer `gitlens_commit_composer`** for multi-file changes — it organizes changes into logical commits automatically.
9. **Prefer `gitlens_start_work`** when starting from a GitHub issue — it creates the branch and links the issue.
10. **Branch naming**: use `type/scope-description` matching conventional-commit types (e.g., `feat/api-taxonomy-crud`, `fix/persistence-soft-delete`).

## Common Workflows

### Commit Workflow

```
1. git_status(directory=...) → review changes
2. git_add_or_commit(directory=..., action="add", files=[...]) → stage specific files
3. git_add_or_commit(directory=..., action="commit", message="feat(api/taxonomy): implement category CRUD endpoints")
```

### PR Workflow

```
1. git_push(directory=...) → push branch
2. pull_request_create(
     provider="github",
     repository_name="Event",
     repository_organization="IslamuOrg",
     title="feat(api/taxonomy): implement category CRUD endpoints",
     source_branch="feat/api-taxonomy-crud",
     target_branch="main"
   )
```

### Start Work from Issue

```
1. issues_get_detail(provider="github", issue_id="42", repository_name="Event", repository_organization="IslamuOrg")
2. gitlens_start_work(directory=..., issue_url="https://github.com/IslamuOrg/Event/issues/42")
```

### Review PR

```
1. pull_request_get_detail(provider="github", pull_request_id="99", repository_name="Event", repository_organization="IslamuOrg", pull_request_files=true)
2. pull_request_get_comments(...)
3. pull_request_create_review(..., review="LGTM — clean implementation", approve=true)
```

### Stash and Switch

```
1. git_stash(directory=..., name="wip-taxonomy", include_untracked=true)
2. git_checkout(directory=..., branch="main")
3. git_pull(directory=...)
```

## Resources

- [gitkraken-cli-readme.md](resources/gitkraken-cli-readme.md) — GitKraken CLI reference

## Related Documentation

- `.claude/skills/conventional-commit/SKILL.md` — commit message format (MUST load)
- `docs/CONTRIBUTING.md` — contributor workflow

**Enforcement Level**: GUIDE
