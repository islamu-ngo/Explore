---
name: Agents Documentation
description: Documentation file - not an executable agent
disabled: true
---

# Project Agents

> **Project-Agnostic Autonomous Agents for Complex Tasks**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../docs/TEMPLATE_GLOSSARY.md).

Specialized autonomous agents for complex, multi-step tasks in Clean Architecture .NET projects.

---

## Overview

Agents are autonomous Claude instances that handle specific complex tasks. Unlike skills (which provide inline guidance), agents:

- Run as separate sub-tasks with focused objectives
- Work autonomously with minimal supervision
- Have specialized tool access for their domain
- Return comprehensive reports upon completion

**Key Advantage**: Agents are standalone—just reference the agent name and Claude Code handles the rest.

---

## Available Agents

| Agent | Purpose | When to Use |
|-------|---------|-------------|
| [blazor-component-architect](#blazor-component-architect) | Design and review Blazor components | Component architecture, MudBlazor patterns, BFF compliance |
| [code-architecture-reviewer](#code-architecture-reviewer) | Review code for architectural consistency | After implementing features, before merging changes |
| [code-refactor-master](#code-refactor-master) | Enforce Clean Architecture with CQRS | Architecture violations, pattern compliance |
| [documentation-architect](#documentation-architect) | Create comprehensive documentation | API docs, developer guides, architectural overviews |
| [frontend-error-fixer](#frontend-error-fixer) | Debug Blazor frontend errors | Compilation errors, MudBlazor issues, render mode problems |
| [plan-reviewer](#plan-reviewer) | Review development plans | Before starting complex features, validating approaches |
| [refactor-planner](#refactor-planner) | Create refactoring strategies | Code reorganization, modernizing legacy code |
| [web-research-specialist](#web-research-specialist) | Research technical issues | Debugging obscure errors, finding best practices |
| [auth-route-tester](#auth-route-tester) | Test authenticated API endpoints | Validating endpoint functionality, debugging auth |
| [auth-route-debugger](#auth-route-debugger) | Debug authentication issues | Token issues, cookie problems, permission errors |
| [auto-error-resolver](#auto-error-resolver) | Automatically fix C#/.NET errors | Build failures, systematic error resolution |

---

## Agent Details

### blazor-component-architect

**Purpose**: Design and review Blazor Server + WASM components for {Project}.

**Capabilities**:
- Blazor hybrid rendering patterns (Server + WASM)
- MudBlazor component best practices
- Blazouter route/guard patterns
- BFF architecture validation
- Service layer patterns
- Authentication state management

**Example Usage**:
```
Use the blazor-component-architect agent to review the {Entity}List.razor component
```

---

### code-architecture-reviewer

**Purpose**: Review code for architectural consistency and best practices.

**Capabilities**:
- Clean Architecture layer validation
- CQRS pattern compliance
- Repository pattern enforcement
- Controller pattern review

**Example Usage**:
```
Use the code-architecture-reviewer agent to review the new {Entity} feature
```

---

### code-refactor-master

**Purpose**: Enforce Clean Architecture with CQRS patterns for {Project}.

**Capabilities**:
- Repository pattern enforcement (entities only, no DTOs)
- CQRS validation (BaseCommandResponse<Guid> for commands)
- Validation pattern checks (manual validation in handlers)
- Controller pattern review (AllowAnonymous vs Authorize)

**Example Usage**:
```
Use the code-refactor-master agent to check the {Entity} handlers
```

---

### documentation-architect

**Purpose**: Create comprehensive documentation following project standards.

**Capabilities**:
- C# XML documentation generation
- Swagger/Scalar API annotations
- Architecture documentation
- Developer guides

**Example Usage**:
```
Use the documentation-architect agent to document the {Entity}Session endpoints
```

---

### frontend-error-fixer

**Purpose**: Debug and fix Blazor frontend errors.

**Capabilities**:
- Blazor compilation error resolution
- MudBlazor component debugging
- Blazouter routing/guard debugging
- Render mode issue diagnosis
- Razor syntax fixes

**Example Usage**:
```
Use the frontend-error-fixer agent to fix the error in Create{Entity}.razor
```

---

### plan-reviewer

**Purpose**: Review development plans before implementation.

**Capabilities**:
- .NET best practices validation
- EF Core performance review
- Security assessment
- Clean Architecture compliance

**Example Usage**:
```
Use the plan-reviewer agent to review my implementation plan for user authentication
```

---

### refactor-planner

**Purpose**: Create comprehensive refactoring strategies.

**Capabilities**:
- Legacy code modernization plans
- Technical debt cleanup strategies
- Clean Architecture enforcement
- Step-by-step refactoring guides

**Example Usage**:
```
Use the refactor-planner agent to plan refactoring the {Entity} module
```

---

### web-research-specialist

**Purpose**: Research technical issues and best practices.

**Capabilities**:
- .NET backend library research
- PostGIS solutions investigation
- Ecosystem best practices
- Solution comparison

**Example Usage**:
```
Use the web-research-specialist agent to research pagination strategies for EF Core
```

---

### auth-route-tester

**Purpose**: Test authenticated API endpoints for {Project}.

**Requirements**: Requires active authentication session.

**Capabilities**:
- Authentication endpoint testing
- Authorization validation
- Security regression testing

**Example Usage**:
```
Use the auth-route-tester agent to test the {Entity} controller endpoints
```

---

### auth-route-debugger

**Purpose**: Debug ASP.NET Core authentication issues with OIDC/JWT.

**Requirements**: Requires JWT/cookie-based authentication setup.

**Capabilities**:
- OIDC flow debugging
- Token validation issues
- Cookie configuration problems
- Claim extraction troubleshooting

**Example Usage**:
```
Use the auth-route-debugger agent to debug why users can't access protected endpoints
```

---

### auto-error-resolver

**Purpose**: Automatically resolve C#/.NET compilation and runtime errors.

**Capabilities**:
- Build error resolution
- Type error fixes
- Missing reference detection
- DI registration issues

**Example Usage**:
```
Use the auto-error-resolver agent to fix the current build errors
```

---

## When to Use Agents vs Skills

| Use Agents When... | Use Skills When... |
|-------------------|-------------------|
| Task requires multiple autonomous steps | Need inline guidance while coding |
| Complex analysis is needed | Checking best practices |
| Clear end goal exists | Ongoing development work |
| Want comprehensive report | Want to maintain control |
| **Example**: "Review all controllers" | **Example**: "Creating a new route" |

**Complementary Usage**:
- Use a **skill** for patterns during development
- Use an **agent** to review the result when complete

---

## Creating Custom Agents

Agents are markdown files with YAML frontmatter:

```markdown
---
name: my-custom-agent
description: Brief description of the agent's purpose
tools: Read, Write, Edit, Bash, Grep, Glob
---

# Agent Name

## Purpose
What this agent accomplishes

## Instructions
Step-by-step instructions for autonomous execution

## Tools Available
- Read: For reading files
- Edit: For modifying files
- Bash: For running commands

## Expected Output
What format to return results in
```

**Best Practices**:
- Be specific in instructions
- Break complex tasks into numbered steps
- Specify exactly what to return
- Include examples of good output
- List available tools explicitly

---

## Troubleshooting

### Agent Not Found

Verify the agent file exists:
```powershell
Get-ChildItem .claude\agents\agent-name.md
```

### Agent Fails with Path Errors

Check for hardcoded paths:
```powershell
Select-String -Path ".claude\agents\agent-name.md" -Pattern "~/|/root/|/Users/"
```

Fix by replacing with relative paths or `$CLAUDE_PROJECT_DIR`.

### Agent Returns Incomplete Results

- Ensure the task is well-defined
- Break complex tasks into smaller sub-tasks
- Provide more context in the initial prompt

---

## Related Documentation

- [`docs/ARCHITECTURE.md`](../../docs/ARCHITECTURE.md) - System architecture overview
- [`docs/QUICK_REFERENCE.md`](../../docs/QUICK_REFERENCE.md) - Critical rules and patterns
- [`.claude/skills/`](../skills/) - Inline guidance skills
