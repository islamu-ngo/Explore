# Claude Code Configuration Refactoring Plan

**Project**: ISLAMU Event
**Task**: Refactor .claude folder from TypeScript to ASP.NET Core/.NET 10 context
**Last Updated**: 2026-01-03
**Status**: Planning Complete - Ready for Implementation

---

## Executive Summary

The `.claude` folder contains configuration for Claude Code (agents, skills, hooks, commands) that was originally designed for a TypeScript/React project. This needs comprehensive refactoring to align with the ISLAMU Event ASP.NET Core/.NET 10 technology stack.

### Critical Finding
The `frontend-dev-guidelines` skill contains React/TypeScript/TanStack patterns that are **completely incompatible** with this project's Blazor/MudBlazor architecture. Additionally, `skill-rules.json` references 5 skills that don't exist in the skills folder.

### Success Criteria
- ✅ All TypeScript/React/Node.js references removed
- ✅ All code examples use C#/.NET 10 patterns
- ✅ 5 missing skills from `skill-rules.json` created
- ✅ Skills activate correctly based on trigger patterns
- ✅ Agents provide project-specific guidance
- ✅ Hooks execute without errors

---

## Current State Analysis

### Technology Stack (Actual Project)
- **Backend**: ASP.NET Core / .NET 10
- **Frontend**: Blazor Server + WebAssembly (Hybrid)
- **UI Library**: MudBlazor (NOT React MUI)
- **Architecture**: Clean Architecture + CQRS (MediatR)
- **Database**: PostgreSQL + PostGIS
- **Auth**: Keycloak (OIDC/JWT)
- **Authz**: Cerbos (Policy Decision Point)
- **Language**: C# 13 (no TypeScript on frontend)

### Skills - Current vs Required

#### ❌ MISMATCH IDENTIFIED

**Referenced in skill-rules.json but MISSING**:
1. `clean-architecture-rules` (Guardrail - BLOCK enforcement)
2. `cqrs-mediatr-guidelines` (Domain - SUGGEST)
3. `blazor-mudblazor-guidelines` (Domain - SUGGEST)
4. `keycloak-auth-debugger` (Domain - SUGGEST)
5. `dotnet-efcore-guidelines` (Domain - SUGGEST)

**Currently Exist but NOT in skill-rules.json**:
1. ✅ `backend-dev-guidelines` - Good, but overlaps with missing skills
2. ❌ `frontend-dev-guidelines` - **WRONG TECH STACK** (React/TS instead of Blazor/C#)
3. ✅ `error-tracking` - Sentry integration, project-agnostic
4. ✅ `route-tester` - API testing patterns, good
5. ✅ `skill-developer` - Meta-skill for creating skills, essential

### Agents - Status Review

| Agent | Status | Issues Found |
|-------|--------|--------------|
| `auth-route-tester` | ✅ Good | Properly C# focused |
| `auth-route-debugger` | ⚠️ Check | Need to verify no TS patterns |
| `auto-error-resolver` | ⚠️ Check | Need to verify |
| `code-architecture-reviewer` | ⚠️ Check | Need to verify |
| `code-refactor-master` | ✅ Excellent | Fully .NET focused |
| `documentation-architect` | ⚠️ Check | Need to verify |
| `frontend-error-fixer` | ❌ Mixed | Has browser-tools MCP refs + Blazor |
| `plan-reviewer` | ⚠️ Check | Need to verify |
| `refactor-planner` | ⚠️ Check | Need to verify |
| `web-research-specialist` | ⚠️ Check | Need to verify |

### Hooks - Status

✅ **Good News**: C# hooks already created:
- `BuildCheck.cs` - Compilation verification
- `ContextTracker.cs` - File change tracking
- `FormatCode.cs` - Code formatting
- `SkillTrigger.cs` - Auto-suggest agents

✅ Old TypeScript/Bash hooks deleted

⚠️ **Need to verify**: Proper configuration in `settings.json`

### Commands - Status

- ✅ `dev-docs` - Working well
- ✅ `dev-docs-update` - Working well
- ⚠️ `route-research-for-testing` - Need to verify

---

## Proposed Future State

### Skills Architecture

```
.claude/skills/
├── clean-architecture-rules/      [NEW - CRITICAL]
│   ├── SKILL.md
│   └── resources/
│       ├── dependency-rules.md
│       ├── layer-responsibilities.md
│       ├── violation-examples.md
│       └── fix-patterns.md
│
├── cqrs-mediatr-guidelines/       [NEW]
│   ├── SKILL.md
│   └── resources/
│       ├── command-patterns.md
│       ├── query-patterns.md
│       ├── handler-patterns.md
│       ├── validation-integration.md
│       └── complete-examples.md
│
├── blazor-mudblazor-guidelines/   [NEW - Replaces frontend-dev-guidelines]
│   ├── SKILL.md
│   └── resources/
│       ├── component-structure.md
│       ├── mudblazor-components.md
│       ├── state-management.md
│       ├── render-modes.md
│       └── common-patterns.md
│
├── keycloak-auth-debugger/        [NEW]
│   ├── SKILL.md
│   └── resources/
│       ├── oidc-flow.md
│       ├── claims-debugging.md
│       ├── bff-pattern.md
│       └── common-issues.md
│
├── dotnet-efcore-guidelines/      [NEW]
│   ├── SKILL.md
│   └── resources/
│       ├── entity-configuration.md
│       ├── migrations-workflow.md
│       ├── postgis-integration.md
│       ├── performance-patterns.md
│       └── seeding-data.md
│
├── backend-dev-guidelines/        [REFACTOR - Reduce overlap]
│   ├── SKILL.md (Update to be meta-guide)
│   └── resources/ (Keep existing, reference new skills)
│
├── error-tracking/                [KEEP AS-IS]
├── route-tester/                  [KEEP AS-IS]
└── skill-developer/               [KEEP AS-IS]
```

**Decision**: DELETE `frontend-dev-guidelines` entirely (wrong tech stack)

### Agents - All C#/.NET Focused

All 10 agents will:
- Use C# code examples exclusively
- Reference .NET 10 APIs and patterns
- Align with project structure (Explore.Domain, Explore.Application, etc.)
- Remove any TypeScript/React/Node.js references

---

## Implementation Phases

### Phase 1: Skills - Create Missing & Remove Wrong
**Priority**: CRITICAL
**Estimated Effort**: 6-8 hours

#### 1.1 Create clean-architecture-rules (HIGHEST PRIORITY)
- **Type**: Guardrail
- **Enforcement**: BLOCK
- **Purpose**: Prevent dependency violations (Domain → Application → Infrastructure → API)

**Tasks**:
- [x] Create SKILL.md with YAML frontmatter
- [ ] Write resources/dependency-rules.md (dependency flow diagram)
- [ ] Write resources/layer-responsibilities.md (what code goes where)
- [ ] Write resources/violation-examples.md (common mistakes)
- [ ] Write resources/fix-patterns.md (how to resolve violations)
- [ ] Add C# examples from actual project code
- [ ] Test activation with keywords: "dependency", "reference", "architecture"
- [ ] Test blocking on file patterns: Domain importing Infrastructure

**Acceptance Criteria**:
- Skill blocks when attempting to add wrong dependency
- Block message is clear and actionable
- Provides specific fix guidance

#### 1.2 Create cqrs-mediatr-guidelines
- **Type**: Domain
- **Enforcement**: SUGGEST
- **Purpose**: MediatR command/query patterns

**Tasks**:
- [ ] Create SKILL.md with YAML frontmatter
- [ ] Write resources/command-patterns.md (Command structure, naming conventions)
- [ ] Write resources/query-patterns.md (Query structure, pagination patterns)
- [ ] Write resources/handler-patterns.md (Handler implementation, CancellationToken)
- [ ] Write resources/validation-integration.md (FluentValidation in pipeline)
- [ ] Write resources/complete-examples.md (End-to-end feature examples)
- [ ] Add real examples from Explore.Application
- [ ] Test activation with keywords: "command", "query", "mediatr"

**Acceptance Criteria**:
- Activates when creating new features
- Provides correct CQRS patterns
- Examples compile and follow project conventions

#### 1.3 Create blazor-mudblazor-guidelines
- **Type**: Domain
- **Enforcement**: SUGGEST
- **Purpose**: Blazor + MudBlazor UI patterns

**Tasks**:
- [ ] Create SKILL.md with YAML frontmatter
- [ ] Write resources/component-structure.md (Blazor lifecycle, @code blocks, parameters)
- [ ] Write resources/mudblazor-components.md (MudGrid, MudButton, MudDialog usage)
- [ ] Write resources/state-management.md (CascadingValue, EventCallback patterns)
- [ ] Write resources/render-modes.md (Server vs WASM vs Auto - project uses Auto)
- [ ] Write resources/common-patterns.md (Forms, dialogs, tables with MudBlazor)
- [ ] Use Context7 MCP for MudBlazor documentation
- [ ] Add examples from Explore.Blazor and Explore.Blazor.Client
- [ ] Test activation with keywords: "blazor", "mudblazor", "razor"

**Acceptance Criteria**:
- Activates on .razor and .razor.cs files
- Provides MudBlazor-specific guidance (not generic React)
- Covers Server/WASM hybrid rendering model

#### 1.4 Create keycloak-auth-debugger
- **Type**: Domain
- **Enforcement**: SUGGEST
- **Purpose**: OIDC/JWT troubleshooting

**Tasks**:
- [ ] Create SKILL.md with YAML frontmatter
- [ ] Write resources/oidc-flow.md (Authorization Code flow, token exchange)
- [ ] Write resources/claims-debugging.md (How to inspect JWT, map Keycloak roles)
- [ ] Write resources/bff-pattern.md (Backend-for-Frontend with YARP if applicable)
- [ ] Write resources/common-issues.md (401/403 troubleshooting checklist)
- [ ] Reference project's Keycloak configuration (appsettings.json patterns)
- [ ] Test activation with keywords: "keycloak", "oidc", "jwt", "claims"

**Acceptance Criteria**:
- Activates on auth issues
- Provides actionable debugging steps
- References project's actual Keycloak setup

#### 1.5 Create dotnet-efcore-guidelines
- **Type**: Domain
- **Enforcement**: SUGGEST
- **Purpose**: EF Core + PostgreSQL + PostGIS patterns

**Tasks**:
- [ ] Create SKILL.md with YAML frontmatter
- [ ] Write resources/entity-configuration.md (IEntityTypeConfiguration, fluent API)
- [ ] Write resources/migrations-workflow.md (add-migration, update-database, script generation)
- [ ] Write resources/postgis-integration.md (Spatial queries, NetTopologySuite)
- [ ] Write resources/performance-patterns.md (AsNoTracking, projections, includes)
- [ ] Write resources/seeding-data.md (HasData vs custom seed classes)
- [ ] Add examples from Explore.Persistence
- [ ] Test activation with keywords: "dbcontext", "migration", "entity"

**Acceptance Criteria**:
- Activates when working with data layer
- Provides PostgreSQL-specific guidance
- Covers PostGIS spatial features

#### 1.6 DELETE frontend-dev-guidelines
**Tasks**:
- [ ] Backup the skill (move to .claude/archive/)
- [ ] Remove from skill-rules.json if referenced
- [ ] Verify no agents/commands reference it
- [ ] Update .claude/README.md if it mentions this skill

**Acceptance Criteria**:
- Skill folder removed
- No broken references in other files

#### 1.7 Refactor backend-dev-guidelines
**Tasks**:
- [ ] Update SKILL.md description to position it as meta-guide
- [ ] Update resources to reference new skills where appropriate
- [ ] Remove content that's now in specialized skills (to avoid duplication)
- [ ] Keep the checklist and workflow sections
- [ ] Ensure no TypeScript examples remain

**Acceptance Criteria**:
- Complements new skills without duplicating content
- Serves as entry point for backend development
- All examples are C#/.NET 10

---

### Phase 2: Agents - Deep C# Refactoring
**Priority**: HIGH
**Estimated Effort**: 3-4 hours

#### Agent Refactoring Checklist (Apply to Each)

For each agent file:
- [ ] Remove all TypeScript/JavaScript references
- [ ] Remove all React/Node.js/npm references
- [ ] Replace with C#/.NET 10 examples
- [ ] Update to reference project structure (Explore.*)
- [ ] Verify tool usage is appropriate (Bash for dotnet commands, not npm)
- [ ] Update description if needed
- [ ] Test activation manually

#### 2.1 frontend-error-fixer
**Current Issues**:
- References "browser-tools MCP"
- Mixed Blazor and generic frontend patterns

**Refactoring Tasks**:
- [ ] Remove browser-tools references (not applicable for Blazor Server)
- [ ] Focus exclusively on Blazor Server + WASM debugging
- [ ] Add MudBlazor-specific error patterns
- [ ] Reference dotnet watch for hot reload
- [ ] Add Blazor circuit debugging patterns
- [ ] Update methodology section

**Acceptance Criteria**:
- 100% Blazor/MudBlazor focused
- No generic frontend references

#### 2.2 auth-route-debugger
**Status**: Already good, but verify

**Verification Tasks**:
- [ ] Check for any TS patterns
- [ ] Ensure Keycloak examples are correct
- [ ] Verify Cerbos authorization examples

#### 2.3 Remaining Agents
For each of: auto-error-resolver, code-architecture-reviewer, documentation-architect, plan-reviewer, refactor-planner, web-research-specialist:

- [ ] Read agent file
- [ ] Identify TypeScript/React references
- [ ] Replace with C# examples
- [ ] Verify tool usage
- [ ] Update if needed

---

### Phase 3: Hooks - Verification & Configuration
**Priority**: MEDIUM
**Estimated Effort**: 1-2 hours

#### 3.1 Verify C# Hooks
**Tasks**:
- [ ] Read BuildCheck.cs - ensure it handles .csproj and .sln correctly
- [ ] Read ContextTracker.cs - verify layer detection logic
- [ ] Read FormatCode.cs - ensure it uses .editorconfig
- [ ] Read SkillTrigger.cs - verify agent suggestion logic
- [ ] Test execution: `dotnet BuildCheck.cs` (if standalone)
- [ ] Check for Windows path handling (\ vs /)

#### 3.2 Verify settings.json Configuration
**Tasks**:
- [ ] Read .claude/settings.json and .claude/settings.local.json
- [ ] Verify hook commands are correct
- [ ] Verify matchers are correct (Edit|Write for ContextTracker)
- [ ] Test hooks activate on appropriate events
- [ ] Update CONFIG.md if configuration changed

**Acceptance Criteria**:
- Hooks execute without errors
- ContextTracker logs modified layers correctly
- BuildCheck runs targeted builds when possible
- SkillTrigger suggests correct agents

---

### Phase 4: Commands - Review & Update
**Priority**: LOW
**Estimated Effort**: 1 hour

#### 4.1 Review Existing Commands
**Tasks**:
- [ ] Read dev-docs.md - verify it's project-appropriate
- [ ] Read dev-docs-update.md - verify it's project-appropriate
- [ ] Read route-research-for-testing.md - check for TS patterns
- [ ] Update any TypeScript references to C#
- [ ] Ensure commands reference correct directory structure

**Acceptance Criteria**:
- All commands use C# examples
- Commands align with project workflows

---

### Phase 5: Documentation & Testing
**Priority**: FINAL
**Estimated Effort**: 1-2 hours

#### 5.1 Update Documentation
**Tasks**:
- [ ] Create/update .claude/README.md with overview
- [ ] Document skill activation patterns
- [ ] Document agent purposes and when to use them
- [ ] Create troubleshooting guide if needed
- [ ] Update this plan document as COMPLETED

#### 5.2 Integration Testing
**Tasks**:
- [ ] Test clean-architecture-rules blocks wrong dependencies
- [ ] Test cqrs-mediatr-guidelines activates on new feature creation
- [ ] Test blazor-mudblazor-guidelines activates on .razor files
- [ ] Test keycloak-auth-debugger activates on auth issues
- [ ] Test dotnet-efcore-guidelines activates on DbContext files
- [ ] Test agents provide correct guidance
- [ ] Test hooks execute on appropriate events
- [ ] Create test scenarios document

**Acceptance Criteria**:
- All skills activate correctly
- All agents provide project-specific guidance
- No errors in hooks
- Documentation is complete and accurate

---

## Risk Assessment & Mitigation

### Risk 1: Scope Creep
**Impact**: High
**Probability**: Medium
**Mitigation**:
- Follow phased approach strictly
- Complete Phase 1 before moving to Phase 2
- Use TodoWrite tool to track progress
- Get user sign-off between phases if needed

### Risk 2: Breaking Existing Workflows
**Impact**: Medium
**Probability**: Low
**Mitigation**:
- Archive old skills before deletion (don't delete immediately)
- Test new skills before removing old ones
- Document changes in migration guide
- Provide rollback plan

### Risk 3: Skill Activation Failures
**Impact**: Medium
**Probability**: Medium
**Mitigation**:
- Test trigger patterns thoroughly
- Reference skill-developer for correct patterns
- Start with broad triggers, narrow if too many false positives
- Test with real user prompts and file edits

### Risk 4: Content Quality - Generic Examples
**Impact**: High
**Probability**: Medium
**Mitigation**:
- Use Context7 MCP for official .NET/Blazor/MudBlazor documentation
- Extract real examples from project codebase
- Reference actual project structure (Explore.Domain, etc.)
- Have user review critical skills before finalization

### Risk 5: Incomplete TypeScript Removal
**Impact**: Low
**Probability**: Low
**Mitigation**:
- Use grep to search for "typescript", "react", "npm", "node", "tsx"
- Review each agent/skill systematically
- Create verification checklist

---

## Success Metrics

### Quantitative Metrics
- [ ] 5 new skills created and functional
- [ ] 0 TypeScript references in .claude folder
- [ ] 10 agents updated with C# examples
- [ ] 4 hooks verified and tested
- [ ] 100% skill activation accuracy on test scenarios

### Qualitative Metrics
- [ ] Skills provide accurate, project-specific guidance
- [ ] Agents understand project architecture
- [ ] Hooks improve development workflow
- [ ] User can successfully enforce Clean Architecture with skills
- [ ] Documentation is clear and comprehensive

---

## Dependencies & Prerequisites

### Tools/Resources Needed
- ✅ Context7 MCP (for .NET/Blazor/MudBlazor docs)
- ✅ Access to project codebase for examples
- ✅ skill-developer skill (for creating new skills)
- ✅ backend-dev-guidelines skill (as structural template)
- ✅ .NET 10 SDK (for testing C# hooks)

### Knowledge Prerequisites
- ✅ Understanding of Clean Architecture layers
- ✅ CQRS + MediatR patterns
- ✅ Blazor component lifecycle
- ✅ MudBlazor component library
- ✅ EF Core + PostgreSQL
- ✅ Keycloak OIDC/JWT flows
- ✅ Claude Code skill/agent structure

---

## Timeline Estimate

| Phase | Estimated Time | Dependencies |
|-------|---------------|--------------|
| Phase 1: Skills | 6-8 hours | None |
| Phase 2: Agents | 3-4 hours | Phase 1 (skills as reference) |
| Phase 3: Hooks | 1-2 hours | None (can parallelize) |
| Phase 4: Commands | 1 hour | None (can parallelize) |
| Phase 5: Documentation & Testing | 1-2 hours | All previous phases |
| **TOTAL** | **12-17 hours** | Spread across 3-5 work sessions |

---

## Next Steps

1. **Get User Approval** on this plan
2. **Start Phase 1.1**: Create clean-architecture-rules skill (highest priority)
3. **Use TodoWrite** to track task progress
4. **Request user feedback** after Phase 1 completion
5. **Proceed systematically** through remaining phases

---

## Notes

- This refactoring is essential for providing accurate, project-specific guidance
- The current state (TypeScript patterns) actively misleads and confuses development
- Clean Architecture enforcement through skills will prevent costly architectural mistakes
- Phased approach allows for iterative improvement and user feedback
- Can be completed over multiple sessions with clear checkpoints

---

**Plan Status**: ✅ COMPLETE - Ready for Implementation
**Next Action**: Get user approval and begin Phase 1.1
