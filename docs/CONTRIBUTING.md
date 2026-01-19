# Contribution Workflow

## Branch Strategy

```
main                    # Default branch
├── feature/xxx         # New features
├── bugfix/xxx          # Bug fixes
└── refactor/xxx        # Code improvements
```

## Commit Convention

```
type(scope): subject

body (optional)

footer (optional)
```

**Types**: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

**Examples**:
```
feat(events): add prayer-relative scheduling
fix(federation): correct HTTP signature validation
docs(api): update endpoint documentation
```

## Pull Request Process

1. Create a branch from `main`
2. Implement changes with tests
3. Ensure all tests pass: `dotnet test`
4. Ensure code formatting: `dotnet format`
5. Create PR with description and linked issue
6. Request review from maintainers
7. Address feedback
8. Squash and merge when approved

## Issue Templates

- **Bug Report**: Describe bug, steps to reproduce, expected behavior
- **Feature Request**: Describe need, proposed solution, alternatives
- **Task**: Technical work without user-facing change

---

## DTO Change Workflow (API → Blazor Client)

When making changes to DTOs in the API project, follow this specific workflow to ensure the Blazor client is properly updated.

### Why This Workflow Matters

The project uses **.NET Aspire** for orchestration, which:
1. Fetches secrets from Infisical and injects them as environment variables
2. Starts all projects in the correct order
3. **Triggers OpenAPI schema regeneration** in the API project on startup
4. **NSwag watches for schema changes** and automatically regenerates the Blazor API client

**Key Point**: You must run Aspire to regenerate the client. Building alone won't update the generated client code.

### The Vicious Cycle Problem

Understanding **why** this workflow exists prevents frustrating debugging sessions.

#### The Problem: Build Errors Block Client Regeneration

When you change DTOs in the API and then try to build the **full solution**, you enter a vicious cycle:

```
┌─────────────────────────────────────────────────────────────────────┐
│                    ❌ THE VICIOUS CYCLE                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. You change a DTO in the API (add new property)                  │
│     ↓                                                               │
│  2. You try to build the FULL SOLUTION                              │
│     ↓                                                               │
│  3. ❌ BUILD FAILS - Blazor references the OLD generated client     │
│     │   (The generated client doesn't have the new property yet)    │
│     ↓                                                               │
│  4. ❌ ASP.NET API won't run (solution didn't build)                │
│     ↓                                                               │
│  5. ❌ swagger.json can't regenerate (API isn't running)            │
│     ↓                                                               │
│  6. ❌ NSwag can't update the client (no new schema)                │
│     ↓                                                               │
│  7. ❌ Generated client stays outdated                              │
│     ↓                                                               │
│  8. ❌ Build errors persist → BACK TO STEP 3                        │
│                                                                     │
│  💀 YOU ARE STUCK IN AN INFINITE LOOP                               │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

#### The Solution: Build API Separately First

By building **only the API project** first, you break the cycle:

```
┌─────────────────────────────────────────────────────────────────────┐
│                    ✅ THE CORRECT WORKFLOW                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. You change a DTO in the API (add new property)                  │
│     ↓                                                               │
│  2. Build ONLY the API project: `dotnet build Explore.API`          │
│     ↓                                                               │
│  3. ✅ API builds successfully (no Blazor dependency issues)        │
│     ↓                                                               │
│  4. Run Aspire: `dotnet run --project Explore.AppHost`              │
│     ↓                                                               │
│  5. ✅ API starts and generates new swagger.json                    │
│     ↓                                                               │
│  6. ✅ NSwag detects changes, regenerates client                    │
│     ↓                                                               │
│  7. ✅ Generated client now has the new property                    │
│     ↓                                                               │
│  8. NOW update Blazor services/components                           │
│     ↓                                                               │
│  9. ✅ Build full solution: `dotnet build Explore.sln`              │
│                                                                     │
│  🎉 SUCCESS - No build errors!                                      │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

#### Common Trap: Updating Blazor Services Too Early

A particularly frustrating mistake is updating **Blazor services that interact with the generated API client** before the client is regenerated:

```
┌─────────────────────────────────────────────────────────────────────┐
│              ❌ THE SERVICE UPDATE TRAP                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Scenario: You add `Description` property to `EventDto`             │
│                                                                     │
│  1. You update EventDto in API (✅ correct)                         │
│     ↓                                                               │
│  2. You update EventService in Blazor to use `.Description`         │
│     ↓                                                               │
│  3. ❌ ERROR: 'EventDto' does not contain 'Description'             │
│     │                                                               │
│     │   WHY? The generated client (ExploreApiClient.cs) still       │
│     │   has the OLD EventDto without Description!                   │
│     │                                                               │
│     │   Your service references: Generated.EventDto (OLD)           │
│     │   You expect:              Generated.EventDto.Description     │
│     │   Reality:                 Property doesn't exist yet         │
│     ↓                                                               │
│  4. Build fails on BOTH sides:                                      │
│     • API side: Works fine (your DTO has the property)              │
│     • Blazor side: Fails (generated client is outdated)             │
│                                                                     │
│  💡 The generated client is OUT OF SYNC with the API!               │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

**Rule of Thumb**:
- **API-only changes** → Build API only, run Aspire, then update Blazor
- **UI-only changes** (no DTO changes) → Can work directly in Blazor
- **Both API + Blazor changes** → Follow the full checklist, API first

### Standard Workflow

```
API Changes → Build API → Run Aspire → Client Regenerates → Update Blazor UI
```

### Process Checklist for DTO Changes

Follow this checklist whenever you add/modify DTO properties:

```markdown
## DTO Change Checklist

### 1. Application Layer Changes
- [ ] Add/update properties in `{Project}.Application/DTOs/{Entity}/` files
- [ ] Update `{Entity}Dto.cs` (full details)
- [ ] Update `{Entity}ListDto.cs` (list view) if applicable
- [ ] Update `Create{Entity}Dto.cs` / `Update{Entity}Dto.cs` if applicable

### 2. Mapping & Data Access
- [ ] Update `MappingProfile.cs` if new navigation properties
- [ ] Update Repository `.Include()` statements if loading related data
- [ ] Update Handler if new processing logic needed

### 3. Build & Regenerate
- [ ] Build API project only: `dotnet build Explore.API`
- [ ] Start Aspire: `dotnet run --project Explore.AppHost`
- [ ] Wait for swagger.json to regenerate (automatic on API startup)
- [ ] NSwag regenerates client automatically (watches schema file)

### 4. Blazor UI Updates
- [ ] NOW update Blazor components to use new properties
- [ ] Build full solution: `dotnet build Explore.sln`
- [ ] Test the changes in browser
```

### Quick Fix: If You Forgot the Workflow

If you already updated Blazor UI before running Aspire and the generated client is missing properties:

**Option 1: Proper Fix (Recommended)**
```powershell
# Stop any running instances
# Run Aspire to regenerate everything
dotnet run --project Explore.AppHost
```

**Option 2: Manual Fix (Quick Workaround)**
Manually add the missing properties to the generated client file as a temporary fix, then run Aspire later to properly regenerate.

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         .NET Aspire Orchestration                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. Aspire Starts                                                   │
│     └── Fetches secrets from Infisical                              │
│     └── Injects environment variables to all projects               │
│                                                                     │
│  2. API Project Starts                                              │
│     └── Generates swagger.json (OpenAPI schema)                     │
│     └── Schema reflects current DTO structure                       │
│                                                                     │
│  3. Blazor Project Watches                                          │
│     └── NSwag detects swagger.json changes                          │
│     └── Regenerates API client (ExploreApiClient.cs)                │
│                                                                     │
│  4. Blazor Components                                               │
│     └── Use the regenerated client with new properties              │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Common Mistakes

| Mistake | Why It Fails | Fix |
|---------|--------------|-----|
| Build solution without running Aspire | swagger.json not regenerated | Run Aspire first |
| Update Blazor before API | Generated client missing new properties | Follow the checklist order |
| Manually edit generated files | Changes overwritten on next regeneration | Add to source DTOs, regenerate |
| Skip the MappingProfile update | AutoMapper doesn't map new properties | Always update profile |

### Related Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) - Overall system architecture
- [API.md](API.md) - API endpoint patterns
- [BLAZOR.md](BLAZOR.md) - Blazor client architecture
- [GOVERNANCE.md](GOVERNANCE.md) - DTO patterns and conventions
