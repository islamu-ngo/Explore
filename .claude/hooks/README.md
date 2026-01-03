Here is the refactored `README.md` tailored specifically for your **ASP.NET Core / .NET 10** project context on Windows.

---

# Hooks (.NET 10 / C#)

Native C# hooks for Claude Code that enable Clean Architecture tracking, skill auto-activation, and build validation.

---

## What Are Hooks?

Hooks are C# scripts that run at specific points in Claude's workflow:
- **UserPromptSubmit**: Checks your prompt to suggest specialized Agents.
- **PostToolUse**: Tracks which Clean Architecture layers (Domain, Infra, UI) are being modified.
- **Stop**: Runs validation tasks like `dotnet build` or `dotnet format` when Claude finishes.

**Key insight:** Since this is a .NET 10 project, all hooks are written in **C#** and executed directly via the CLI, eliminating the need for Node.js or Bash scripts.

---

## Essential Hooks

### 1. SkillTrigger.cs (UserPromptSubmit)

**Purpose:** Automatically suggests relevant specialized agents based on keywords in your prompt.

**How it works:**
1. Reads the user's prompt from Stdin.
2. Detects keywords like "controller" (suggests `auth-route-debugger`) or "razor" (suggests `frontend-error-fixer`).
3. Injects suggestions directly into Claude's context.

**Configuration (`settings.json`):**
```json
{
  "hooks": {
    "UserPromptSubmit": [
      {
        "type": "command",
        "command": "dotnet .claude/hooks/SkillTrigger.cs"
      }
    ]
  }
}
```

---

### 2. ContextTracker.cs (PostToolUse)

**Purpose:** Tracks file changes to optimize the build process and maintain context.

**How it works:**
1. Monitors `Edit` and `Write` tool calls.
2. Identifies the Clean Architecture layer (e.g., `Explore.Domain`, `Explore.Blazor`).
3. Logs modified layers to `.claude/build-cache/`.
4. Prepares targeted build commands (so we don't rebuild the whole solution if only a UI component changed).

**Configuration (`settings.json`):**
```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Edit|MultiEdit|Write",
        "hooks": [
          {
            "type": "command",
            "command": "dotnet .claude/hooks/ContextTracker.cs"
          }
        ]
      }
    ]
  }
}
```

---

## Quality Assurance Hooks (Stop)

These run when Claude finishes a task or asks for feedback.

### 3. BuildCheck.cs

**Purpose:** Verifies that the code compiles successfully.

**Logic:**
- Reads the cache created by `ContextTracker`.
- Runs `dotnet build` on specific projects if possible, or the full Solution (`.sln`) as a fallback.
- If the build fails, it captures errors for the `auto-error-resolver` agent.

### 4. FormatCode.cs

**Purpose:** Enforces C# coding standards.

**Logic:**
- Runs `dotnet format` on modified files.
- Ensures braces, indentation, and imports match `.editorconfig`.

**Configuration (`settings.json`):**
```json
{
  "hooks": {
    "Stop": [
      {
        "type": "command",
        "command": "dotnet .claude/hooks/FormatCode.cs"
      },
      {
        "type": "command",
        "command": "dotnet .claude/hooks/BuildCheck.cs"
      }
    ]
  }
}
```

---

## Setup & Installation

### 1. Prerequisites
- **.NET 10 SDK** installed.
- **PowerShell 7+** or CMD.

### 2. Verify Script Permissions
Ensure your `.cs` files are accessible.

```powershell
# Verify files exist
Get-ChildItem .claude/hooks/*.cs
```

### 3. Usage
Hooks run automatically based on the events defined in `settings.json`. You do not need to run them manually, though you can test them:

```powershell
# Test compilation hook manually
dotnet .claude/hooks/BuildCheck.cs
```

---

## Troubleshooting

### "Command not found" or "dotnet" error
*   **Cause:** `.NET SDK` is not in your system PATH.
*   **Fix:** Run `dotnet --version` to verify installation.

### Hooks failing on Windows
*   **Cause:** Path separators (`/` vs `\`) or JSON parsing issues.
*   **Fix:** The provided C# scripts handle path normalization automatically. Ensure you are not using `$CLAUDE_PROJECT_DIR` in `settings.json` (Unix syntax) but rather relative paths like `dotnet .claude/hooks/...`.

### Build is too slow
*   **Cause:** The hook is rebuilding the entire solution on every stop.
*   **Fix:** Ensure `ContextTracker.cs` is correctly identifying layers so `BuildCheck.cs` can run targeted builds (e.g., `dotnet build src/Explore.Domain`).
