# Claude Folder Refactoring - Task Checklist

**Last Updated**: 2026-01-03
**Status**: Ready to Start

---

## Phase 1: Skills - Create Missing & Remove Wrong

### 1.1 Create clean-architecture-rules Skill (CRITICAL)

- [ ] Create `.claude/skills/clean-architecture-rules/` directory
- [ ] Create `SKILL.md` with YAML frontmatter
  - [ ] name: clean-architecture-rules
  - [ ] description: Enforces Clean Architecture dependency rules
  - [ ] type: guardrail
  - [ ] enforcement: block
- [ ] Create `resources/` directory
- [ ] Write `resources/dependency-rules.md`
  - [ ] Dependency flow diagram (Domain → App → Infra → API)
  - [ ] What each layer can reference
  - [ ] Visual diagram using ASCII art
- [ ] Write `resources/layer-responsibilities.md`
  - [ ] Domain: Entities, Enums, Value Objects (no dependencies)
  - [ ] Application: Use cases, DTOs, Interfaces (→ Domain only)
  - [ ] Infrastructure: DB, File, API implementations (→ App, Domain)
  - [ ] API/Blazor: Entry points (→ All)
- [ ] Write `resources/violation-examples.md`
  - [ ] Example: Domain importing EF Core
  - [ ] Example: Application importing ASP.NET Controller
  - [ ] Example: Domain importing Infrastructure
  - [ ] Show exact error messages
- [ ] Write `resources/fix-patterns.md`
  - [ ] How to use interfaces (define in App, implement in Infra)
  - [ ] How to use dependency injection
  - [ ] How to move logic to correct layer
- [ ] Add C# code examples from project
  - [ ] Good: Application/Interfaces/IEventRepository.cs
  - [ ] Good: Infrastructure/Repositories/EventRepository.cs
  - [ ] Bad: Domain/Entities/Event.cs importing DbContext
- [ ] Test activation
  - [ ] Test keyword: "dependency"
  - [ ] Test keyword: "reference"
  - [ ] Test keyword: "architecture"
  - [ ] Test file pattern: Domain/**/*.cs with "using Infrastructure"
- [ ] Verify blocking works
  - [ ] Attempt to add wrong dependency
  - [ ] Verify block message appears
  - [ ] Verify message is actionable

### 1.2 Create cqrs-mediatr-guidelines Skill

- [ ] Create `.claude/skills/cqrs-mediatr-guidelines/` directory
- [ ] Create `SKILL.md` with YAML frontmatter
  - [ ] name: cqrs-mediatr-guidelines
  - [ ] description: CQRS patterns with MediatR
  - [ ] type: domain
  - [ ] enforcement: suggest
- [ ] Create `resources/` directory
- [ ] Write `resources/command-patterns.md`
  - [ ] Command structure (IRequest<TResponse>)
  - [ ] Naming conventions (CreateEventCommand, UpdateEventCommand)
  - [ ] Handler pattern (IRequestHandler)
  - [ ] When to use commands (write operations)
  - [ ] Complete example from project
- [ ] Write `resources/query-patterns.md`
  - [ ] Query structure (IRequest<TResponse>)
  - [ ] Naming conventions (GetEventByIdQuery, GetEventListQuery)
  - [ ] Handler pattern
  - [ ] Pagination pattern (PagedResult<T>)
  - [ ] AsNoTracking() for read-only queries
  - [ ] Complete example from project
- [ ] Write `resources/handler-patterns.md`
  - [ ] Handler structure
  - [ ] Constructor injection
  - [ ] CancellationToken usage (always pass to async methods)
  - [ ] Error handling patterns
  - [ ] Mapping entities to DTOs
- [ ] Write `resources/validation-integration.md`
  - [ ] FluentValidation integration
  - [ ] AbstractValidator<T> pattern
  - [ ] Validation pipeline behavior
  - [ ] Where validators belong (Application/DTOs/{Entity}/Validators/)
  - [ ] Example validator from project
- [ ] Write `resources/complete-examples.md`
  - [ ] End-to-end feature (Command + Query + Validator + Controller)
  - [ ] Show all layers working together
  - [ ] Include real code from project
- [ ] Add real examples from `Explore.Application/Features/`
  - [ ] Extract CreateEventCommand example
  - [ ] Extract GetEventByIdQuery example
  - [ ] Extract EventValidator example
- [ ] Test activation
  - [ ] Test keyword: "command"
  - [ ] Test keyword: "query"
  - [ ] Test keyword: "mediatr"
  - [ ] Test file pattern: **/*Command.cs
  - [ ] Test file pattern: **/*Handler.cs

### 1.3 Create blazor-mudblazor-guidelines Skill

- [ ] Create `.claude/skills/blazor-mudblazor-guidelines/` directory
- [ ] Create `SKILL.md` with YAML frontmatter
  - [ ] name: blazor-mudblazor-guidelines
  - [ ] description: Blazor + MudBlazor UI patterns
  - [ ] type: domain
  - [ ] enforcement: suggest
- [ ] Create `resources/` directory
- [ ] Write `resources/component-structure.md`
  - [ ] Blazor component lifecycle (OnInitialized, OnParametersSet, OnAfterRender)
  - [ ] @code block organization
  - [ ] Parameter binding ([Parameter] attribute)
  - [ ] EventCallback pattern
  - [ ] Component disposal (IDisposable)
  - [ ] Example from Explore.Blazor
- [ ] Write `resources/mudblazor-components.md`
  - [ ] MudGrid system (MudGrid, MudItem, size prop)
  - [ ] MudButton (Variant, Color, OnClick)
  - [ ] MudTextField, MudSelect patterns
  - [ ] MudDialog pattern (DialogService)
  - [ ] MudTable pattern (data binding)
  - [ ] MudPaper, MudCard for layout
  - [ ] Use Context7 MCP for MudBlazor official docs
- [ ] Write `resources/state-management.md`
  - [ ] CascadingValue / CascadingParameter
  - [ ] EventCallback for child-to-parent communication
  - [ ] StateHasChanged() when to call
  - [ ] Service injection (@inject)
  - [ ] Scoped vs Singleton services in Blazor
- [ ] Write `resources/render-modes.md`
  - [ ] InteractiveServer (SignalR-based)
  - [ ] InteractiveWebAssembly (client-side)
  - [ ] InteractiveAuto (hybrid - project default)
  - [ ] @rendermode directive
  - [ ] Prerendering considerations
  - [ ] Example from project configuration
- [ ] Write `resources/common-patterns.md`
  - [ ] Form patterns (EditForm, DataAnnotationsValidator)
  - [ ] Dialog patterns (MudDialog, DialogService)
  - [ ] Table patterns (MudTable with pagination)
  - [ ] Navigation (NavigationManager)
  - [ ] Loading states (MudProgressCircular)
- [ ] Add real examples from `Explore.Blazor/`
  - [ ] Extract component examples
  - [ ] Show proper MudBlazor usage
- [ ] Test activation
  - [ ] Test keyword: "blazor"
  - [ ] Test keyword: "mudblazor"
  - [ ] Test keyword: "razor"
  - [ ] Test file pattern: **/*.razor
  - [ ] Test content pattern: @page, <Mud

### 1.4 Create keycloak-auth-debugger Skill

- [ ] Create `.claude/skills/keycloak-auth-debugger/` directory
- [ ] Create `SKILL.md` with YAML frontmatter
  - [ ] name: keycloak-auth-debugger
  - [ ] description: Keycloak OIDC/JWT debugging
  - [ ] type: domain
  - [ ] enforcement: suggest
- [ ] Create `resources/` directory
- [ ] Write `resources/oidc-flow.md`
  - [ ] Authorization Code flow diagram
  - [ ] Redirect to Keycloak
  - [ ] Code exchange for tokens
  - [ ] Token refresh flow
  - [ ] Logout flow
  - [ ] Project-specific Keycloak configuration
- [ ] Write `resources/claims-debugging.md`
  - [ ] How to inspect JWT (jwt.io or base64 decode)
  - [ ] Claims structure (sub, roles, email, etc.)
  - [ ] Keycloak role mapping
  - [ ] How to access claims in C# (User.Claims)
  - [ ] Example ClaimsPrincipal inspection code
- [ ] Write `resources/bff-pattern.md`
  - [ ] Backend-for-Frontend pattern
  - [ ] Cookie-based auth in Blazor Server
  - [ ] JWT Bearer auth in API
  - [ ] How they interact
  - [ ] YARP configuration if applicable
- [ ] Write `resources/common-issues.md`
  - [ ] 401 Unauthorized (token missing, expired, invalid)
  - [ ] 403 Forbidden (token valid but insufficient permissions)
  - [ ] CORS issues with Keycloak
  - [ ] Cookie not being sent (SameSite issues)
  - [ ] Redirect loop problems
  - [ ] Debugging checklist
- [ ] Reference project's appsettings.json Keycloak config
  - [ ] Show Authority, ClientId, ClientSecret structure
  - [ ] Show Blazor vs API configuration differences
- [ ] Test activation
  - [ ] Test keyword: "keycloak"
  - [ ] Test keyword: "oidc"
  - [ ] Test keyword: "jwt"
  - [ ] Test keyword: "auth"
  - [ ] Test file pattern: **/Program.cs
  - [ ] Test content pattern: AddAuthentication, Authorize

### 1.5 Create dotnet-efcore-guidelines Skill

- [ ] Create `.claude/skills/dotnet-efcore-guidelines/` directory
- [ ] Create `SKILL.md` with YAML frontmatter
  - [ ] name: dotnet-efcore-guidelines
  - [ ] description: EF Core + PostgreSQL patterns
  - [ ] type: domain
  - [ ] enforcement: suggest
- [ ] Create `resources/` directory
- [ ] Write `resources/entity-configuration.md`
  - [ ] IEntityTypeConfiguration<T> pattern
  - [ ] Fluent API (HasKey, Property, HasIndex, etc.)
  - [ ] Table naming conventions
  - [ ] Column type mapping
  - [ ] Relationships (HasOne, HasMany, WithMany)
  - [ ] Example from Explore.Persistence/Configurations/
- [ ] Write `resources/migrations-workflow.md`
  - [ ] Add-Migration command (dotnet ef migrations add)
  - [ ] Update-Database (dotnet ef database update)
  - [ ] Script-Migration for production
  - [ ] Rollback migration
  - [ ] Migration best practices
  - [ ] Project context (Explore.Persistence)
- [ ] Write `resources/postgis-integration.md`
  - [ ] NetTopologySuite integration
  - [ ] Geography vs Geometry types
  - [ ] Spatial queries (Distance, Within, Contains)
  - [ ] Index configuration (GIST)
  - [ ] Example: Event.Location property
- [ ] Write `resources/performance-patterns.md`
  - [ ] AsNoTracking() for read queries
  - [ ] Select projections (avoid loading full entities)
  - [ ] Include() vs explicit loading
  - [ ] AsSplitQuery() for large includes
  - [ ] Batch updates/deletes
  - [ ] N+1 query problem and solutions
- [ ] Write `resources/seeding-data.md`
  - [ ] HasData() method
  - [ ] Custom seed classes
  - [ ] When to seed (migrations vs startup)
  - [ ] Test data seeding
- [ ] Add real examples from `Explore.Persistence/`
  - [ ] Extract entity configuration examples
  - [ ] Show migration files
  - [ ] Show DbContext setup
- [ ] Test activation
  - [ ] Test keyword: "dbcontext"
  - [ ] Test keyword: "migration"
  - [ ] Test keyword: "entity"
  - [ ] Test file pattern: **/*DbContext.cs
  - [ ] Test content pattern: DbSet<, IEntityTypeConfiguration

### 1.6 Archive frontend-dev-guidelines

- [ ] Create `.claude/archive/` directory if not exists
- [ ] Move `.claude/skills/frontend-dev-guidelines/` to `.claude/archive/frontend-dev-guidelines/`
- [ ] Add README.md in archive explaining why it was archived
- [ ] Verify no references to frontend-dev-guidelines in:
  - [ ] skill-rules.json
  - [ ] Agent files
  - [ ] Command files
  - [ ] Hook files
- [ ] Test that Claude Code still works without it

### 1.7 Refactor backend-dev-guidelines

- [ ] Read current SKILL.md
- [ ] Update description to position as "meta-guide"
- [ ] Update resources to reference new specialized skills:
  - [ ] Reference clean-architecture-rules for dependency rules
  - [ ] Reference cqrs-mediatr-guidelines for CQRS patterns
  - [ ] Reference dotnet-efcore-guidelines for data access
- [ ] Remove duplicated content now covered by specialized skills
- [ ] Keep high-level workflow and checklist sections
- [ ] Ensure all examples are C#/.NET 10 (verify no TS)
- [ ] Test that it complements rather than duplicates new skills

---

## Phase 2: Agents - Deep C# Refactoring

### 2.1 Refactor frontend-error-fixer Agent

- [ ] Read current agent file
- [ ] Remove all "browser-tools MCP" references
- [ ] Remove generic frontend debugging patterns
- [ ] Focus exclusively on Blazor Server + WASM
- [ ] Update error patterns section:
  - [ ] Blazor circuit disconnected errors
  - [ ] Razor compilation errors (RZxxxx)
  - [ ] MudBlazor component errors
  - [ ] SignalR connection issues
  - [ ] WASM loading failures
- [ ] Update methodology:
  - [ ] Check browser console for Blazor errors
  - [ ] Check server logs for SignalR errors
  - [ ] Use `dotnet watch` for hot reload
  - [ ] Inspect Blazor circuit state
- [ ] Add MudBlazor-specific error patterns
- [ ] Remove any React/npm references
- [ ] Test agent understanding with sample prompt

### 2.2 Verify auth-route-tester Agent

- [ ] Read agent file
- [ ] Verify no TypeScript references
- [ ] Verify Keycloak examples are accurate
- [ ] Verify API endpoint patterns match project (/api/v1/)
- [ ] Verify curl examples use Bearer tokens correctly
- [ ] Verify Cerbos references if any
- [ ] Test with sample security testing scenario

### 2.3 Verify auth-route-debugger Agent

- [ ] Read agent file
- [ ] Check for TypeScript patterns
- [ ] Verify OIDC flow description is accurate
- [ ] Verify Keycloak configuration examples
- [ ] Update if needed to match project
- [ ] Test with auth debugging scenario

### 2.4 Review auto-error-resolver Agent

- [ ] Read agent file
- [ ] Check for npm/node/TypeScript references
- [ ] Verify it focuses on .NET compilation errors
- [ ] Update to reference dotnet build, dotnet test
- [ ] Ensure error patterns are C# specific (CS0246, etc.)
- [ ] Update if needed

### 2.5 Review code-architecture-reviewer Agent

- [ ] Read agent file (already marked as excellent)
- [ ] Verify no TypeScript references
- [ ] Verify Clean Architecture understanding matches project
- [ ] Ensure it references correct project layers
- [ ] Update if needed

### 2.6 Review documentation-architect Agent

- [ ] Read agent file
- [ ] Check for TypeScript/JSDoc references
- [ ] Update to C# XML documentation (/// <summary>)
- [ ] Reference Swagger/Scalar documentation
- [ ] Ensure examples are C#
- [ ] Update if needed

### 2.7 Review plan-reviewer Agent

- [ ] Read agent file
- [ ] Check for technology-specific patterns
- [ ] Verify it understands .NET project context
- [ ] Update if needed

### 2.8 Review refactor-planner Agent

- [ ] Read agent file
- [ ] Verify it understands Clean Architecture
- [ ] Check for TypeScript refactoring patterns
- [ ] Update to C# refactoring patterns
- [ ] Update if needed

### 2.9 Review web-research-specialist Agent

- [ ] Read agent file
- [ ] Verify it can research .NET topics
- [ ] Check for web framework references (should be agnostic)
- [ ] Update if needed

### 2.10 Final Agent Verification

- [ ] Search all agent files for "typescript"
- [ ] Search all agent files for "react"
- [ ] Search all agent files for "npm"
- [ ] Search all agent files for "node"
- [ ] Search all agent files for "tsx"
- [ ] Verify all found instances are removed or justified
- [ ] Test 2-3 agents with real scenarios

---

## Phase 3: Hooks - Verification & Configuration

### 3.1 Verify BuildCheck.cs

- [ ] Read BuildCheck.cs code
- [ ] Verify it handles .csproj files
- [ ] Verify it handles .sln files
- [ ] Verify it reads ContextTracker cache
- [ ] Verify it runs targeted builds when possible
- [ ] Verify error capture for auto-error-resolver
- [ ] Test execution: `dotnet BuildCheck.cs` (if standalone)
- [ ] Verify Windows path handling (\ vs /)

### 3.2 Verify ContextTracker.cs

- [ ] Read ContextTracker.cs code
- [ ] Verify layer detection logic (Domain, Application, etc.)
- [ ] Verify it monitors Edit and Write tools
- [ ] Verify cache location (.claude/build-cache/)
- [ ] Verify cache format is correct
- [ ] Test execution

### 3.3 Verify FormatCode.cs

- [ ] Read FormatCode.cs code
- [ ] Verify it uses `dotnet format`
- [ ] Verify it respects .editorconfig
- [ ] Verify it only formats modified files
- [ ] Test execution

### 3.4 Verify SkillTrigger.cs

- [ ] Read SkillTrigger.cs code
- [ ] Verify agent suggestion logic
- [ ] Verify keyword detection
- [ ] Verify it suggests correct agents for project
- [ ] Update agent suggestions if needed (remove TS-related agents)
- [ ] Test with sample prompts

### 3.5 Verify settings.json Configuration

- [ ] Read `.claude/settings.json`
- [ ] Verify hook commands are correct
- [ ] Verify matchers are correct:
  - [ ] UserPromptSubmit: SkillTrigger.cs
  - [ ] PostToolUse (Edit|Write): ContextTracker.cs
  - [ ] Stop: FormatCode.cs, BuildCheck.cs
- [ ] Test hooks activate on appropriate events
- [ ] Check `.claude/settings.local.json` for overrides

### 3.6 Update CONFIG.md if Needed

- [ ] Read `.claude/hooks/CONFIG.md`
- [ ] Verify it matches current hook configuration
- [ ] Update if configuration has changed
- [ ] Verify examples are accurate

---

## Phase 4: Commands - Review & Update

### 4.1 Review dev-docs Command

- [ ] Read `.claude/commands/dev-docs.md`
- [ ] Verify it's appropriate for .NET project
- [ ] Check for any TypeScript references
- [ ] Verify examples are C#
- [ ] Test command execution

### 4.2 Review dev-docs-update Command

- [ ] Read `.claude/commands/dev-docs-update.md`
- [ ] Verify it's appropriate for .NET project
- [ ] Update if needed

### 4.3 Review route-research-for-testing Command

- [ ] Read `.claude/commands/route-research-for-testing.md`
- [ ] Check for TypeScript/npm patterns
- [ ] Verify it uses dotnet test patterns
- [ ] Update to use C# API testing patterns
- [ ] Update if needed

---

## Phase 5: Documentation & Testing

### 5.1 Create/Update .claude/README.md

- [ ] Create or update `.claude/README.md`
- [ ] Document skill architecture and purpose
- [ ] List all skills with descriptions
- [ ] List all agents with descriptions
- [ ] Document hook system
- [ ] Document commands
- [ ] Provide usage examples
- [ ] Include troubleshooting section

### 5.2 Integration Testing - Skills

- [ ] Test clean-architecture-rules:
  - [ ] Attempt Domain → Infrastructure reference
  - [ ] Verify block message
  - [ ] Verify guidance is actionable
- [ ] Test cqrs-mediatr-guidelines:
  - [ ] Create new feature prompt
  - [ ] Verify skill activates
  - [ ] Verify content is relevant
- [ ] Test blazor-mudblazor-guidelines:
  - [ ] Edit .razor file
  - [ ] Verify skill activates
  - [ ] Verify MudBlazor examples are correct
- [ ] Test keycloak-auth-debugger:
  - [ ] Prompt about auth issue
  - [ ] Verify skill activates
  - [ ] Verify guidance is project-specific
- [ ] Test dotnet-efcore-guidelines:
  - [ ] Edit DbContext file
  - [ ] Verify skill activates
  - [ ] Verify examples are correct

### 5.3 Integration Testing - Agents

- [ ] Test frontend-error-fixer with Blazor error
- [ ] Test auth-route-tester with API endpoint
- [ ] Test code-refactor-master with refactoring task
- [ ] Verify agents provide C# examples
- [ ] Verify no TypeScript references appear

### 5.4 Integration Testing - Hooks

- [ ] Make code change
- [ ] Let Claude finish task
- [ ] Verify BuildCheck.cs runs
- [ ] Verify FormatCode.cs runs
- [ ] Verify no errors in hook execution
- [ ] Check build-cache/ for ContextTracker output

### 5.5 Create Test Scenarios Document

- [ ] Document test scenarios for each skill
- [ ] Document test prompts
- [ ] Document expected activations
- [ ] Document success criteria
- [ ] Save as `dev/active/claude-folder-refactor/test-scenarios.md`

### 5.6 Update This Plan as COMPLETED

- [ ] Mark all tasks as complete
- [ ] Update status to COMPLETED
- [ ] Document any deviations from plan
- [ ] Document lessons learned
- [ ] Archive this task in dev/completed/

---

## Quick Reference: Skill Creation Checklist

For each new skill:
- [ ] Create skill directory
- [ ] Create SKILL.md with frontmatter (name, description, type, enforcement)
- [ ] Create resources/ directory
- [ ] Write resource markdown files (3-5 files)
- [ ] Add C# code examples from project
- [ ] Test keyword triggers
- [ ] Test file pattern triggers
- [ ] Verify activation works
- [ ] Verify content is relevant and accurate

---

## Quick Reference: Agent Refactoring Checklist

For each agent:
- [ ] Read agent file
- [ ] Identify TypeScript/React/npm references
- [ ] Replace with C# examples
- [ ] Verify tool usage (Bash for dotnet, not npm)
- [ ] Update references to project structure
- [ ] Test with sample prompt
- [ ] Verify understanding is correct

---

**Status**: ⏳ Ready to Start
**Estimated Total Time**: 12-17 hours
**Recommended Sessions**: 3-5 work sessions with breaks

**Start with**: Phase 1.1 - Create clean-architecture-rules (highest priority)
