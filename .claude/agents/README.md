---
name: Agents Documentation
description: Documentation file - not an executable agent
disabled: true
---

# Agents

Specialized agents for complex, multi-step tasks.

---

## What Are Agents?

Agents are autonomous Claude instances that handle specific complex tasks. Unlike skills (which provide inline guidance), agents:
- Run as separate sub-tasks
- Work autonomously with minimal supervision
- Have specialized tool access
- Return comprehensive reports when complete

**Key advantage:** Agents are **standalone** - just copy the `.md` file and use immediately!

---

## Available Agents (11)

### blazor-component-architect
**Purpose:** Design and review Blazor Server + WASM components for ISLAMU Event

**When to use:**
- Designing new Blazor components
- Reviewing component architecture
- Refactoring Blazor pages
- Validating MudBlazor usage
- Checking BFF pattern compliance

**Key Features:**
- Blazor hybrid rendering patterns (Server + WASM)
- MudBlazor component best practices
- BFF architecture validation
- Service layer patterns
- Authentication state management

**Integration:** ✅ Copy as-is

---

### code-architecture-reviewer
**Purpose:** Review code for architectural consistency and best practices

**When to use:**
- After implementing a new feature
- Before merging significant changes
- When refactoring code
- To validate architectural decisions

**Integration:** ✅ Copy as-is

---

### code-refactor-master
**Purpose:** Enforce Clean Architecture with CQRS patterns for ISLAMU Event

**When to use:**
- Reviewing code for Clean Architecture violations
- Checking CQRS pattern compliance (commands vs queries)
- Validating repository pattern (entities not DTOs)
- Ensuring FluentValidation with repository injection
- Reviewing authentication/authorization patterns

**Key Features:**
- Repository pattern enforcement (entities only, no DTOs)
- CQRS validation (BaseCommandResponse<Guid> for commands)
- Validation pattern checks (manual validation in handlers)
- Controller pattern review (AllowAnonymous vs Authorize)
- Actual ISLAMU Event entity names (Event, Organization, User, Actor, etc.)

**Integration:** ✅ Copy as-is

---

### documentation-architect
**Purpose:** Create comprehensive documentation

**When to use:**
- Documenting new features
- Creating API documentation
- Writing developer guides
- Generating architectural overviews

**Integration:** ✅ Copy as-is

---

### frontend-error-fixer
**Purpose:** Debug and fix Blazor frontend errors

**When to use:**
- Blazor compilation errors
- Browser console errors
- MudBlazor component errors
- Render mode issues
- Build failures

**Integration:** ⚠️ May reference screenshot paths - update if needed

---

### plan-reviewer
**Purpose:** Review development plans before implementation

**When to use:**
- Before starting complex features
- Validating architectural plans
- Identifying potential issues early
- Getting second opinion on approach

**Integration:** ✅ Copy as-is

---

### refactor-planner
**Purpose:** Create comprehensive refactoring strategies

**When to use:**
- Planning code reorganization
- Modernizing legacy code
- Breaking down large files
- Improving code structure

**Integration:** ✅ Copy as-is

---

### web-research-specialist
**Purpose:** Research technical issues online

**When to use:**
- Debugging obscure errors
- Finding solutions to problems
- Researching best practices
- Comparing implementation approaches

**Integration:** ✅ Copy as-is

---

### auth-route-tester
**Purpose:** Test authenticated API endpoints

**When to use:**
- Testing auth
- Validating endpoint functionality
- Debugging authentication issues

**Integration:** ⚠️ Requires auth

---

### auth-route-debugger
**Purpose:** Debug authentication issues

**When to use:**
- Auth failures
- Token issues
- Cookie problems
- Permission errors

**Integration:** ⚠️ Requires JWT cookie-based auth

---

### auto-error-resolver
**Purpose:** Automatically fix TypeScript compilation errors

**When to use:**
- Build failures with TypeScript errors
- After refactoring that breaks types
- Systematic error resolution needed

**Integration:** ⚠️ May need path updates

---

## How to Integrate an Agent

### Standard Integration (Most Agents)

**Step 1: Copy the file**
```powershell
Copy-Item showcase\.claude\agents\agent-name.md your-project\.claude\agents\
```

**Step 2: Verify (optional)**
```powershell
# Check for hardcoded paths
Select-String -Path "your-project\.claude\agents\agent-name.md" -Pattern "~/git/|/root/git/|/Users/"
```

**Step 3: Use it**
Ask Claude: "Use the [agent-name] agent to [task]"

That's it! Agents work immediately.

---

### Agents Requiring Customization

**frontend-error-fixer:**
- May reference screenshot paths
- Ask user: "Where should screenshots be saved?"
- Update paths in agent file

**auth-route-tester / auth-route-debugger:**
- Require JWT cookie authentication
- Update service URLs from examples
- Customize for user's auth setup

**auto-error-resolver:**
- May have hardcoded project paths
- Update to use `$CLAUDE_PROJECT_DIR` or relative paths

---

## When to Use Agents vs Skills

| Use Agents When... | Use Skills When... |
|-------------------|-------------------|
| Task requires multiple steps | Need inline guidance |
| Complex analysis needed | Checking best practices |
| Autonomous work preferred | Want to maintain control |
| Task has clear end goal | Ongoing development work |
| Example: "Review all controllers" | Example: "Creating a new route" |

**Both can work together:**
- Skill provides patterns during development
- Agent reviews the result when complete

---

## Agent Quick Reference

| Agent | Complexity | Customization | Auth Required |
|-------|-----------|---------------|---------------|
| blazor-component-architect | Medium | ✅ None | No |
| code-architecture-reviewer | Medium | ✅ None | No |
| code-refactor-master | High | ✅ None | No |
| documentation-architect | Medium | ✅ None | No |
| frontend-error-fixer | Medium | ⚠️ Screenshot paths | No |
| plan-reviewer | Low | ✅ None | No |
| refactor-planner | Medium | ✅ None | No |
| web-research-specialist | Low | ✅ None | No |
| auth-route-tester | Medium | ⚠️ Auth setup | JWT cookies |
| auth-route-debugger | Medium | ⚠️ Auth setup | JWT cookies |
| auto-error-resolver | Low | ⚠️ Paths | No |

---

## For Claude Code

**When integrating agents for a user:**

1. **Read [CLAUDE_INTEGRATION_GUIDE.md](../../CLAUDE_INTEGRATION_GUIDE.md)**
2. **Just copy the .md file** - agents are standalone
3. **Check for hardcoded paths:**
   ```powershell
   Select-String -Path "agent-name.md" -Pattern "~/git/|/root/"
   ```
4. **Update paths if found** to `$CLAUDE_PROJECT_DIR` or `.`
5. **For auth agents:** Ask if they use JWT cookie auth first

**That's it!** Agents are the easiest components to integrate.

---

## Creating Your Own Agents

Agents are markdown files with optional YAML frontmatter:

```markdown
# Agent Name

## Purpose
What this agent does

## Instructions
Step-by-step instructions for autonomous execution

## Tools Available
List of tools this agent can use

## Expected Output
What format to return results in
```

**Tips:**
- Be very specific in instructions
- Break complex tasks into numbered steps
- Specify exactly what to return
- Include examples of good output
- List available tools explicitly

---

## Troubleshooting

### Agent not found

**Check:**
```powershell
# Is agent file present?
Get-ChildItem .claude\agents\agent-name.md
```

### Agent fails with path errors

**Check for hardcoded paths:**
```powershell
Select-String -Path ".claude\agents\agent-name.md" -Pattern "~/|/root/|/Users/"
```

**Fix:**
```powershell
(Get-Content ".claude\agents\agent-name.md") -replace '~/git/.*project', '$CLAUDE_PROJECT_DIR' | Set-Content ".claude\agents\agent-name.md"
```

## Troubleshooting

### Agent not found

**Check:**
```powershell
# Is agent file present?
Get-ChildItem .claude\agents\agent-name.md
```

### Agent fails with path errors

**Check for hardcoded paths:**
```powershell
Select-String -Path ".claude\agents\agent-name.md" -Pattern "~/|/root/|/Users/"
```

**Fix:**
```powershell
(Get-Content ".claude\agents\agent-name.md") -replace '~/git/.*project', '$CLAUDE_PROJECT_DIR' | Set-Content ".claude\agents\agent-name.md"
```


### Agent not found

**Check:**
```powershell
# Is agent file present?
Get-ChildItem .claude\agents\agent-name.md
```

### Agent fails with path errors

**Check for hardcoded paths:**
```powershell
Select-String -Path ".claude\agents\agent-name.md" -Pattern "~/|/root/|/Users/"
```

**Fix:**
```powershell
(Get-Content ".claude\agents\agent-name.md") -replace '~/git/.*project', '$CLAUDE_PROJECT_DIR' | Set-Content ".claude\agents\agent-name.md"
```


## Troubleshooting

### Agent not found

**Check:**
```powershell
# Is agent file present?
Get-ChildItem .claude\agents\agent-name.md
```

### Agent fails with path errors

**Check for hardcoded paths:**
```powershell
Select-String -Path ".claude\agents\agent-name.md" -Pattern "~/|/root/|/Users/"
```

**Fix:**
```powershell
(Get-Content ".claude\agents\agent-name.md") -replace '~/git/.*project', '$CLAUDE_PROJECT_DIR' | Set-Content ".claude\agents\agent-name.md"
```
## Troubleshooting

### Agent not found

**Check:**
```powershell
# Is agent file present?
Get-ChildItem .claude\agents\agent-name.md
```

### Agent fails with path errors

**Check for hardcoded paths:**
```powershell
Select-String -Path ".claude\agents\agent-name.md" -Pattern "~/|/root/|/Users/"
```

**Fix:**
```powershell
(Get-Content ".claude\agents\agent-name.md") -replace '~/git/.*project', '$CLAUDE_PROJECT_DIR' | Set-Content ".claude\agents\agent-name.md"
```


