# Hooks & Agents Configuration (.NET 10)

This guide explains how to configure and customize the C#-based hooks system for the **ISLAMU Event** project.

## 🚀 Quick Start Configuration

### 1. Register Hooks in `.claude/settings.json`

Ensure your `settings.json` is configured to execute the C# scripts using the `.NET CLI`.

```json
{
  "commands": {
    "build": "dotnet build",
    "test": "dotnet test",
    "format": "dotnet format",
    "run": "dotnet run"
  },
  "hooks": {
    "UserPromptSubmit": [
      "dotnet .claude/hooks/SkillTrigger.cs"
    ],
    "PostToolUse": [
      "dotnet .claude/hooks/ContextTracker.cs"
    ],
    "Stop": [
      "dotnet .claude/hooks/FormatCode.cs",
      "dotnet .claude/hooks/BuildCheck.cs"
    ]
  }
}
2. Prerequisites
• .NET 10 SDK must be installed and accessible in your $PATH.
• dotnet format tool (usually included in the SDK).

--------------------------------------------------------------------------------
🛠 Core Hooks (C# Scripts)
Unlike standard Claude hooks which use Bash or TypeScript, this project uses C# Scripting to maintain consistency with the backend architecture.
1. Build Verification (BuildCheck.cs)
Event: Stop Purpose: Validates that the Solution (.sln) compiles correctly after Claude finishes an edit.
• Logic: Finds the .sln file in the root and runs dotnet build --nologo --verbosity quiet.
• Error Handling: If the build fails, it captures the output to .claude/build-cache/last-errors.txt to be read by the auto-error-resolver agent.
• Customization: Edit .claude/hooks/BuildCheck.cs to change build flags (e.g., adding --no-restore for speed).
2. Code Formatting (FormatCode.cs)
Event: Stop Purpose: Enforces the C# Style Guide defined in GOVERNANCE.md.
• Logic: Executes dotnet format.
• Configuration: Reads rules from .editorconfig (if present) or standard .NET defaults.
3. Context Tracker (ContextTracker.cs)
Event: PostToolUse Purpose: Analyzes the Clean Architecture structure to help Claude understand where it is working.
• Detection Logic:
    ◦ Frontend: Checks for Explore.Blazor (Blazor Server/Wasm).
    ◦ Infrastructure: Checks for Explore.Infrastructure (EF Core, PostGIS).
    ◦ Domain: Checks for Explore.Domain (Entities, Value Objects).
• Output: Updates .claude/context-log.json.
4. Skill Trigger (SkillTrigger.cs)
Event: UserPromptSubmit Purpose: Scans the user's prompt for keywords to suggest specialized agents.
• Triggers:
    ◦ "controller", "api" → Suggests auth-route-debugger (Keycloak checks).
    ◦ "razor", "component", "mudblazor" → Suggests frontend-error-fixer.

--------------------------------------------------------------------------------
🤖 Agent Integration
Hooks often delegate complex tasks to Agents. Below is the mapping for this project's architecture:
Hook Trigger
Targeted Agent
Purpose
Build Failure
auto-error-resolver
Parses CSxxxx errors and applies C# fixes.
User Request
code-architecture-reviewer
Validates CQRS pattern and MediatR usage.
User Request
auth-route-debugger
Debugs Keycloak JWT and Cerbos policies.
User Request
frontend-error-fixer
Fixes Blazor lifecycle and MudBlazor syntax.

--------------------------------------------------------------------------------
⚙️ Customization Guide
Excluding Files from Checks
To prevent hooks from scanning specific folders (e.g., migrations), modify the C# logic in the hook files directly.
Example: Modify BuildCheck.cs to ignore specific warnings
// Inside .claude/hooks/BuildCheck.cs
var processInfo = new ProcessStartInfo("dotnet", "build -p:NoWarn=CS1591");
Adjusting Architecture Detection
If you rename the core projects (e.g., from Explore.Infrastructure to Islamu.Data), update ContextTracker.cs:
// Inside .claude/hooks/ContextTracker.cs
Backend = Directory.Exists("Islamu.Data") ? "Clean Architecture" : "Unknown",

--------------------------------------------------------------------------------
🔍 Troubleshooting
Hook Fails with dotnet: command not found
• Cause: The .NET SDK is not in the system PATH visible to Claude.
• Fix: Ensure you can run dotnet --version in your terminal. You may need to alias the command or add it to your shell profile.
Build Check is Too Slow
• Cause: dotnet build is rebuilding the entire solution every time.
• Fix: Edit BuildCheck.cs to target a specific project instead of the whole solution if you are only working on one module:
"Script execution failed"
• Cause: Syntax error in the C# hook file.
• Fix: Run the hook manually to debug:

--------------------------------------------------------------------------------
📂 Cache Management
The hooks create a local cache to store build errors and context states.
• Location: .claude/build-cache/
• Auto-Cleanup: BuildCheck.cs automatically deletes the cache upon a successful build (Exit Code 0).
