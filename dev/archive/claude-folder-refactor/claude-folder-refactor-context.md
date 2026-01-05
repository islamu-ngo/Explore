# Claude Folder Refactoring - Context & Decisions

**Last Updated**: 2026-01-03

---

## Project Context

### ISLAMU Event Technology Stack

```
Frontend:
├── Blazor Server + WebAssembly (Hybrid - Auto render mode)
├── MudBlazor (Material Design component library for Blazor)
├── C# 13 (no TypeScript)
└── SignalR (for Blazor Server communication)

Backend:
├── ASP.NET Core / .NET 10
├── Clean Architecture (Domain → Application → Infrastructure → API)
├── CQRS Pattern (MediatR for commands/queries)
├── FluentValidation (request validation)
├── Entity Framework Core (ORM)
└── PostgreSQL + PostGIS (spatial database)

Auth/Authz:
├── Keycloak (OIDC/JWT identity provider)
└── Cerbos (Policy Decision Point for authorization)

Observability:
├── Serilog (structured logging)
├── OpenTelemetry (distributed tracing)
└── Sentry (error tracking)

Orchestration:
└── .NET Aspire (service orchestration, local development)
```

### Project Structure

```
Explore.sln
├── Explore.Domain/              # Entities, Enums, Value Objects (no dependencies)
├── Explore.Application/         # CQRS Handlers, DTOs, Validators (→ Domain)
├── Explore.Persistence/         # EF Core DbContext, Repositories (→ Application)
├── Explore.Infrastructure/      # External services (→ Application)
├── Explore.API/                 # REST Controllers (→ All)
├── Explore.Blazor/              # Server-side Blazor (BFF pattern)
├── Explore.Blazor.Client/       # WebAssembly components
├── Explore.AppHost/             # Aspire orchestrator
├── Explore.ServiceDefaults/     # Shared Aspire config
├── Explore.Domain.Tests/
├── Explore.Application.Tests/
└── Explore.API.Tests/
```

---

## Key Architectural Decisions

### Decision 1: Clean Architecture Dependency Flow

**Rule**: Dependencies flow inward only

```
┌─────────────────────────────────────────────────────────────┐
│                      DEPENDENCY FLOW                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Explore.Domain (Core)                                      │
│  ↑ No dependencies on anything                             │
│  │                                                          │
│  Explore.Application                                        │
│  ↑ References: Domain only                                 │
│  │                                                          │
│  Explore.Persistence + Explore.Infrastructure               │
│  ↑ References: Application, Domain                         │
│  │                                                          │
│  Explore.API + Explore.Blazor                               │
│  ↑ References: All (Composition Root)                      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Violations to Block**:
- ❌ Domain referencing anything
- ❌ Application referencing Infrastructure or API
- ❌ Infrastructure referencing API

**clean-architecture-rules skill**: BLOCKS these violations

### Decision 2: CQRS Pattern with MediatR

**Pattern**: Separate Commands (write) from Queries (read)


**Location**: `Explore.Application/Features/{Entity}/`
example: `Explore.Application/Features/{Entity}/Handlers/Queries/*RequestHandler.cs`

**cqrs-mediatr-guidelines skill**: Provides these patterns

### Decision 3: Blazor Rendering - Auto Mode (Hybrid)

**Configuration**:
```csharp
// Program.cs
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// App.razor
<HeadOutlet @rendermode="RenderMode.InteractiveAuto" />
<Routes @rendermode="RenderMode.InteractiveAuto" />
```

**Why Auto Mode**:
- Starts with Blazor Server (fast initial load)
- Downloads WebAssembly in background
- Switches to client-side rendering after download
- Best of both worlds

**blazor-mudblazor-guidelines skill**: Covers this pattern

### Decision 4: MudBlazor Component Library

**Example Component**:
```razor
@page "/events"
@using MudBlazor

<MudContainer MaxWidth="MaxWidth.Large">
    <MudGrid>
        <MudItem xs="12" md="6">
            <MudPaper Class="pa-4">
                <MudText Typo="Typo.h5">Event List</MudText>
                <MudButton Variant="Variant.Filled" Color="Color.Primary">
                    Create Event
                </MudButton>
            </MudPaper>
        </MudItem>
    </MudGrid>
</MudContainer>
```

**blazor-mudblazor-guidelines skill**: Provides MudBlazor patterns

### Decision 5: Keycloak Authentication

**Flow**: OpenID Connect (Authorization Code)

```
User → Blazor App → Redirect to Keycloak
     ← Auth Code ← User authenticates
     → Exchange code for JWT →
     ← Access Token + ID Token ←
     → API requests with Bearer token →
```

**Configuration** (appsettings.json):
```json
{
  "Keycloak": {
    "Authority": "https://keycloak.openislamu.org/realms/islamu-dev",
    "ClientId": "explore-api",
    "ClientSecret": "***"
  }
}
```

**keycloak-auth-debugger skill**: Troubleshoots this flow

### Decision 6: EF Core with PostgreSQL + PostGIS

**Entity Configuration Pattern**:
```csharp
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        // PostGIS spatial property
        builder.Property(e => e.Location)
            .HasColumnType("geography (point)");

        builder.HasIndex(e => e.Location)
            .HasMethod("GIST");
    }
}
```

**Location**: `Explore.Persistence/Configurations/`

**dotnet-efcore-guidelines skill**: Provides these patterns

---

## Claude Code Component Architecture

### Skills vs Agents vs Hooks vs Commands

```
┌─────────────────────────────────────────────────────────────┐
│                  CLAUDE CODE COMPONENTS                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  SKILLS (Knowledge Modules)                                 │
│  ├── Auto-activate based on keywords/file patterns         │
│  ├── Provide domain-specific guidance                      │
│  ├── Can be Guardrails (BLOCK) or Helpers (SUGGEST)        │
│  └── Example: clean-architecture-rules blocks violations   │
│                                                             │
│  AGENTS (Specialized AI Personalities)                      │
│  ├── User invokes explicitly or auto-suggested             │
│  ├── Have specific tool access (Bash, Read, Write, etc.)  │
│  ├── Task-focused (debugging, refactoring, testing)        │
│  └── Example: auth-route-debugger for OIDC issues          │
│                                                             │
│  HOOKS (Event-Driven Automation)                            │
│  ├── UserPromptSubmit: Before prompt processing            │
│  ├── PostToolUse: After each tool execution                │
│  ├── Stop: When Claude finishes task                       │
│  └── Example: BuildCheck.cs runs `dotnet build` on Stop    │
│                                                             │
│  COMMANDS (User-Invoked Workflows)                          │
│  ├── Slash commands (/dev-docs, /commit, etc.)             │
│  ├── Scripted sequences of actions                         │
│  └── Example: /dev-docs creates strategic plan             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Skill Activation Mechanism

**skill-rules.json Structure**:
```json
{
  "skills": {
    "skill-name": {
      "type": "guardrail" | "domain",
      "enforcement": "block" | "suggest" | "warn",
      "priority": "critical" | "high" | "medium" | "low",
      "promptTriggers": {
        "keywords": ["auth", "keycloak"],
        "intentPatterns": ["(debug|fix).*?(auth|login)"]
      },
      "fileTriggers": {
        "pathPatterns": ["**/*Controller.cs"],
        "contentPatterns": ["AddAuthentication", "Authorize"]
      },
      "blockMessage": "Message shown when blocked",
      "skipConditions": {
        "sessionSkillUsed": true
      }
    }
  }
}
```

**Activation Priority**:
1. File content patterns (highest specificity)
2. File path patterns
3. Intent patterns (regex on user prompt)
4. Keywords (broadest match)

---

## Critical Files & Dependencies

### Files to Reference During Refactoring

**For Examples**:
- `docs/ARCHITECTURE.md` - Architecture overview
- `docs/CONVENTIONS.md` - Code conventions
- `docs/SECURITY.md` - Auth/Authz patterns
- `src/Explore.Application/Features/` - CQRS examples
- `src/Explore.Blazor/` - Blazor component examples
- `src/Explore.Persistence/Configurations/` - EF Core examples

**For Structure**:
- `.claude/skills/backend-dev-guidelines/` - Template structure
- `.claude/skills/skill-developer/` - Skill creation guide
- `.claude/agents/code-refactor-master.md` - Well-refactored agent

**For Configuration**:
- `.claude/settings.json` - Hook configuration
- `.claude/hooks/CONFIG.md` - Hook setup guide
- `skill-rules.json` - Skill trigger definitions

### External Documentation Sources

**Use Context7 MCP for**:
- .NET 10 documentation (`/microsoft/dotnet`)
- ASP.NET Core patterns (`/dotnet/aspnetcore`)
- Entity Framework Core (`/dotnet/efcore`)
- MudBlazor components (`/mudblazor/mudblazor`)
- Blazor documentation (`/dotnet/blazor`)

---

## Decisions Made During Planning

### Decision: Delete vs Archive frontend-dev-guidelines

**Choice**: Archive (not delete permanently)

**Rationale**:
- Preserve work done
- Might have useful patterns to adapt
- Can restore if decision was wrong

**Action**: Move to `.claude/archive/frontend-dev-guidelines/`

### Decision: Refactor vs Replace backend-dev-guidelines

**Choice**: Refactor (don't replace)

**Rationale**:
- Already has good structure
- Contains valuable content
- Can serve as meta-guide to new specialized skills
- Reduce duplication by referencing specialized skills

**Action**: Update to complement new skills, remove overlapping content

### Decision: Skill Granularity

**Choice**: 5 focused skills (not 1 mega skill)

**Rationale**:
- More precise activation (fewer false positives)
- Easier to maintain and update
- Better performance (load only needed content)
- Allows different enforcement levels (BLOCK vs SUGGEST)

**Skills**: clean-architecture-rules, cqrs-mediatr-guidelines, blazor-mudblazor-guidelines, keycloak-auth-debugger, dotnet-efcore-guidelines

### Decision: Hooks in C# (not Bash/TypeScript)

**Choice**: Already made - C# hooks exist ✓

**Rationale**:
- Native to project stack
- Can reference .NET APIs directly
- No Node.js dependency
- Better Windows compatibility

**Files**: BuildCheck.cs, ContextTracker.cs, FormatCode.cs, SkillTrigger.cs

### Decision: Implementation Order

**Choice**: Skills → Agents → Hooks → Commands → Docs

**Rationale**:
- Skills provide foundation (especially guardrails)
- Agents reference skills in their guidance
- Hooks are already functional (just verify)
- Commands are lowest priority
- Documentation ties everything together

---

## Open Questions for User

1. **Existing Skills**: Keep error-tracking, route-tester, skill-developer as-is?
   - Assumption: Yes (they're project-agnostic)

2. **Skill Activation Sensitivity**: Prefer broader triggers (more help) or narrower (less intrusive)?
   - Assumption: Start broad, narrow based on feedback

3. **Clean Architecture Enforcement**: Block or just warn on violations?
   - Assumption: BLOCK (per skill-rules.json)

4. **Example Source**: Use real project code or create synthetic examples?
   - Assumption: Mix - real code for authenticity, synthetic for clarity

5. **French vs English**: Skills currently mix languages (backend-dev-guidelines is in French)
   - Question: Standardize on English?
   - Assumption: Keep as-is unless user requests change

---

## Success Criteria Expanded

### Functional Requirements
- ✅ clean-architecture-rules blocks Domain → Infrastructure references
- ✅ cqrs-mediatr-guidelines activates when creating new features
- ✅ blazor-mudblazor-guidelines activates on .razor files
- ✅ keycloak-auth-debugger activates on "401 error" prompts
- ✅ dotnet-efcore-guidelines activates when editing DbContext
- ✅ All agents provide C# examples (0 TypeScript references)
- ✅ Hooks execute without errors
- ✅ Commands work with project structure

### Quality Requirements
- ✅ Examples compile and follow project conventions
- ✅ Guidance is specific to ISLAMU Event, not generic
- ✅ Resource files are well-organized and comprehensive
- ✅ Trigger patterns have high precision (low false positives)
- ✅ Documentation is clear and complete

### User Experience Requirements
- ✅ Skills activate when expected
- ✅ Block messages are actionable (tell user how to fix)
- ✅ Agents understand project context
- ✅ Workflow feels natural and helpful
- ✅ Configuration is maintainable

---

## Migration Strategy

### Backward Compatibility

**During Transition**:
1. Archive old skills (don't delete)
2. Keep both old and new during testing
3. Verify new skills work before removing old
4. Document what changed

**Rollback Plan**:
- Archived files can be restored
- Git history preserves all changes
- Skill activation can be disabled in skill-rules.json

### Testing Approach

**Skill Testing**:
```bash
# Test trigger activation
# 1. Type prompt with keywords
# 2. Edit files matching patterns
# 3. Verify skill content loads

# Test blocking
# 1. Attempt wrong dependency reference
# 2. Verify block message appears
# 3. Verify guidance is actionable
```

**Agent Testing**:
```bash
# Test agent knowledge
# 1. Invoke agent explicitly
# 2. Ask for C# example
# 3. Verify it's project-specific
# 4. Check for TypeScript references (should be 0)
```

**Hook Testing**:
```bash
# Test Stop hooks
dotnet .claude/hooks/BuildCheck.cs
dotnet .claude/hooks/FormatCode.cs

# Test actual workflow
# 1. Make code change
# 2. Let Claude finish
# 3. Verify hooks run
# 4. Check logs for errors
```

---

## Maintenance Plan

### Regular Updates
- Review skills quarterly for accuracy
- Update examples when project patterns change
- Refine trigger patterns based on usage data
- Keep documentation synchronized with code

### Feedback Loop
- Track skill activation accuracy
- Collect user feedback on helpfulness
- Adjust enforcement levels (block → warn) if too strict
- Add new skills as new patterns emerge

### Version Control
- Commit each skill creation separately
- Tag major refactoring milestones
- Document breaking changes
- Maintain changelog

---

**Context Status**: ✅ COMPLETE
**Next**: Proceed with implementation using this context as guide
