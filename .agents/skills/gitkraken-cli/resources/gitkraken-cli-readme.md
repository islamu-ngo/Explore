# 🚀 GitKraken CLI

`gk` is GitKraken on the command line. The core functionality is focused on "Work Items" which can be thought of as the feature or issue you are trying to tackle. This allows you to work with multiple repos at once and get the same UX as if you were in a monorepo. We also provide robust AI-powered commit messages and Pull Request generation. It also provides an MCP server that streamlines working with git and your Issue and git hosting providers.


## Table of Contents

- [MCP Server](#mcp-server)
- [Documentation](#documentation)
- [Workflows](#workflows)
- [`git` Command Passthrough](#git-command-passthrough)
- [Installation](#installation)
- [Troubleshooting](#troubleshooting)
- [Support](#support)

## MCP Server

The GitKraken MCP server is a local MCP server that is powerful and easy to use. It wraps git, GitHub, Jira MCP actions as well as provides tools to LLMs that work with GitKraken APIs and functionality. You can find specific installation instructions based on your chosen AI application in the [Help Center](https://help.gitkraken.com/cli/gk-cli-mcp/).

If you want to read more about the MCP server, you can check out the [introduction blog post](https://www.gitkraken.com/blog/introducing-gitkraken-mcp)

## Documentation

`gk help` is going to be your best source for exploring the CLI. But also see the [workflows](#workflows) below.

```bash
Welcome to GitKraken CLI, a premium CLI experience for managing multiple repositories with familiar GIT CLI commands

Usage:
  gk [flags]
  gk [command]

AUTHENTICATING
  auth         Authenticate with the GitKraken platform
  provider     Add or remove provider tokens

CORE COMMANDS
  graph        Display commit graph in current repository
  issue        Manage your issues
  organization Manage your Gitkraken organizations
  work         Interact with your work.
  workspace    Interact with your workspaces. Alias: 'ws'

Additional Commands:
  help         Help about any command
  setup        Display information about your current system configuration
  version      Print the version number of GK CLI

Flags:
  -h, --help   help for gk

Use "gk [command] --help" for more information about a command.
```

## Workflows

Start with a single repo. You can add more later.

In general, your process will look like this:

```bash

# Authenticate
gk auth login

# Navigate to a git repo directory on your filesystem
cd ./path/to/repo

# Then create a Work Item and the current directory
# will be automatically added to the Work Item
gk work create "My new work item"

# Edit files...
# ...

# Commit your changes using AI
gk work commit --ai

# Push your changes
gk work push

# Create a Pull Request
gk work pr create --ai

```

Once you have familiarized yourself with using a single repo, try out creating work items and generating commits and PRs for multiple repos at a time by just adding multiple repos to a new Work Item.

```bash
# Add a repo to the current work item
gk work add ./path/to/repo # path could be as simple as "." if you are in the directory already
```

## `git` Command Passthrough

You can also use `gk` to pass through any `git` command. eg:

```bash
gk status
gk remote -v
# etc
```
