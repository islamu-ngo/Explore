# Codebase Documentation & Skills/Agents Update - Task Checklist

> **Detailed task checklist for comprehensive documentation and skills/agents updates**
>
> **Created**: 2026-02-10
> **Status**: Awaiting Approval
> **Estimated Total Time**: 12-16 hours

---

## PHASE 1: CRITICAL FIXES ⏳ NOT STARTED
**Estimated Time**: 2 hours
**Priority**: CRITICAL - Must complete before Phase 2

### Task 1.1: Remove All BMAD Agents
**Status**: ⏳ Not Started
**Estimated Time**: 15 minutes

- [ ] Identify all BMAD agent files (30+ files)
  - [ ] `.claude/commands/bmad-*.md`
  - [ ] `.claude/commands/bmm-*.md`
  - [ ] `.claude/commands/cis-*.md`
  - [ ] `.claude/commands/tea-*.md`
- [ ] **DECISION NEEDED**: Delete vs archive?
  - [ ] If archive: Move to `.claude/commands/archive/bmad/`
  - [ ] If delete: Remove permanently
- [ ] Verify no references to BMAD agents in other files
- [ ] Test that project-specific agents still work
- [ ] Document removal in changelog or migration notes

**Acceptance Criteria**:
- [ ] All BMAD agents removed from active `.claude/commands/` directory
- [ ] Project-specific agents (11) remain untouched
- [ ] No broken references to BMAD agents
- [ ] Namespace is clean and navigable

**Blockers**: User decision on delete vs archive approach

---

### Task 1.2: Clarify Federation Documentation Status
**Status**: ⏳ Not Started
**Estimated Time**: 1 hour
**File**: `docs/FEDERATION.md`

- [ ] **DECISION NEEDED**: Get clarification from user on what is actually implemented
- [ ] Review current FEDERATION.md content
- [ ] Compare with actual codebase entities (Actor, AtprotoRecord, etc.)
- [ ] Choose approach:
  - [ ] **Option A**: Update to reflect partial implementation
    - [ ] Mark implemented features (entities exist)
    - [ ] Mark unimplemented features (HTTP endpoints, WebFinger)
    - [ ] Add timeline for remaining features
    - [ ] Update diagrams with status badges
  - [ ] **Option B**: Mark all as "planned architecture"
    - [ ] Add dates to roadmap items
    - [ ] Prefix sections with "Planned:"
    - [ ] Move to `docs/roadmap/FEDERATION.md`
- [ ] Ensure no contradictions with CODEBASE_STRUCTURE.md
- [ ] Add clear status section at top of document

**Acceptance Criteria**:
- [ ] Status section clearly states what is vs isn't implemented
- [ ] No contradictions with other documentation
- [ ] Diagrams have appropriate status labels
- [ ] Timeline provided for unimplemented features (if applicable)

**Blockers**: User clarification on actual Federation implementation status

---

### Task 1.3: Fix Cerbos Terminology Inconsistency
**Status**: ⏳ Not Started
**Estimated Time**: 15 minutes
**Files**: `docs/SECURITY.md`, `docs/CONFIGURATION.md`

- [ ] Read current Cerbos references in SECURITY.md
- [ ] Read current Cerbos references in CONFIGURATION.md
- [ ] Identify terminology differences
  - SECURITY.md: "future"
  - CONFIGURATION.md: "not currently wired"
- [ ] Choose unified terminology: "Not implemented - planned for future release"
- [ ] Update SECURITY.md with unified language
- [ ] Update CONFIGURATION.md with unified language
- [ ] If timeline known, add to both files

**Acceptance Criteria**:
- [ ] SECURITY.md and CONFIGURATION.md use identical Cerbos status language
- [ ] No ambiguity about implementation status
- [ ] Timeline added if available

**Blockers**: None

---

## PHASE 2: DOCUMENTATION UPDATES ⏳ NOT STARTED
**Estimated Time**: 4-5 hours
**Priority**: HIGH - Core documentation gaps
**Depends On**: Phase 1 complete

### Task 2.1: Update QUICK_REFERENCE.md with Missing Patterns
**Status**: ⏳ Not Started
**Estimated Time**: 45 minutes
**File**: `docs/QUICK_REFERENCE.md`

- [ ] Add new Section 13: "Delete Commands Return bool"
  - [ ] Write section header
  - [ ] Add WRONG example (BaseCommandResponse)
  - [ ] Add CORRECT example (bool)
  - [ ] Add controller usage example
  - [ ] Add "Why" explanation
- [ ] Update Rule #11: "Entities Include Auditing Fields (ENHANCED)"
  - [ ] Add DeletedAt (DateTime?) field
  - [ ] Add DeletedBy (Guid?) field
  - [ ] Note this provides complete deletion audit trail
- [ ] Update Table of Contents with new section
- [ ] Update "Common Mistakes & Fixes" table
  - [ ] Add delete command error row
- [ ] Verify numbering is sequential (13 rules total)

**Acceptance Criteria**:
- [ ] Section 13 added with complete delete command pattern
- [ ] Rule 11 enhanced with DeletedAt/DeletedBy fields
- [ ] Table of contents reflects new section
- [ ] Common mistakes table includes delete errors
- [ ] All examples follow placeholder syntax

**Blockers**: None

---

### Task 2.2: Document HATEOAS Pattern in API.md
**Status**: ⏳ Not Started
**Estimated Time**: 1 hour
**File**: `docs/API.md`

- [ ] Add new section: "HATEOAS/HAL+JSON Response Format"
- [ ] Write introduction to HATEOAS concept
- [ ] Document HalResource<T> wrapper structure
  - [ ] _links object with rel, href, method
  - [ ] _embedded object for collections
  - [ ] Data payload structure
- [ ] Document HalCollectionResource<T> structure
  - [ ] Collection-specific _links (first, last, next, prev)
  - [ ] _embedded.items array
  - [ ] totalCount, pageNumber, pageSize metadata
- [ ] Document Prefer header support
  - [ ] `Prefer: return=minimal` strips _links
  - [ ] Default behavior includes full HAL
- [ ] Document ResourceAssembler pattern
  - [ ] IResourceAssembler<TDto, TListDto> interface
  - [ ] How to implement custom assemblers
- [ ] Add complete example response (GET /api/events)
- [ ] Document RouteNames constants usage
- [ ] Cross-reference CODEBASE_INSIGHTS.md section 14

**Acceptance Criteria**:
- [ ] HATEOAS section comprehensive and clear
- [ ] Response structure fully documented with examples
- [ ] Prefer header mechanism explained
- [ ] ResourceAssembler pattern documented
- [ ] Real-world example provided
- [ ] Cross-references added

**Blockers**: None

---

### Task 2.3: Add Missing Patterns to DOMAIN.md
**Status**: ⏳ Not Started
**Estimated Time**: 1 hour
**File**: `docs/DOMAIN.md`

- [ ] Add new section: "Module-Specific Entity Extensions"
  - [ ] Document EventIslamicAspect entity
    - [ ] Properties: Prayer times, Madhab, Gender segregation
    - [ ] Relationship to Event (one-to-one)
    - [ ] Module governance connection
  - [ ] Document EventTechAspect entity
    - [ ] Properties: Tech stack, Skill level, Platform
    - [ ] Relationship to Event (one-to-one)
    - [ ] Module governance connection
  - [ ] Explain strategy pattern for module resolution
    - [ ] IModuleService<TEntity, TAspect>
    - [ ] How aspect resolution works
    - [ ] Cross-reference CODEBASE_INSIGHTS.md
- [ ] Add new section: "Event Session Hierarchy"
  - [ ] Document Event → EventSession relationship (one-to-many)
  - [ ] Document EventSession → EventSessionAgendaItem (one-to-many)
  - [ ] Document EventSession → EventSessionSpeaker (many-to-many)
  - [ ] Document EventSession → EventSessionLanguage (one-to-many)
  - [ ] Add relationship diagram (Mermaid or table)
- [ ] Update entity count in overview if needed

**Acceptance Criteria**:
- [ ] EventIslamicAspect fully documented
- [ ] EventTechAspect fully documented
- [ ] Module strategy pattern explained
- [ ] Event session hierarchy clarified with diagram
- [ ] All relationships clearly shown
- [ ] Cross-references to CODEBASE_INSIGHTS.md added

**Blockers**: None

---

### Task 2.4: Update CONFIGURATION.md
**Status**: ⏳ Not Started
**Estimated Time**: 45 minutes
**File**: `docs/CONFIGURATION.md`

- [ ] Add new section: "Instance-Level Settings"
  - [ ] Document SystemSetting table structure
  - [ ] Document GovernanceSettingKeys constants
  - [ ] Explain deployment mode switching
  - [ ] Provide example settings
- [ ] Add new section: "Module-Specific Configuration"
  - [ ] Document TenantCapability table
  - [ ] Explain module governance settings
  - [ ] Show how modules are enabled/disabled per tenant
  - [ ] Provide configuration examples
- [ ] Update "BYOK Integration Status" section
  - [ ] Mark storage as "✅ Implemented"
  - [ ] Mark payment/analytics as "⏳ Planned - Q2 2026" or remove if no timeline
  - [ ] Add storage configuration details
- [ ] Verify no contradictions with OPERATIONS.md

**Acceptance Criteria**:
- [ ] Instance settings section comprehensive
- [ ] Module configuration section added
- [ ] BYOK status clarified with accurate implementation state
- [ ] Configuration examples provided
- [ ] No contradictions across documentation

**Blockers**: May need user input on payment/analytics BYOK timeline

---

### Task 2.5: Document OutputCache Pattern in API.md
**Status**: ⏳ Not Started
**Estimated Time**: 30 minutes
**File**: `docs/API.md`

- [ ] Add new section: "Output Caching"
- [ ] Document [OutputCache] attribute usage
  - [ ] PolicyName = "ListData" for list endpoints
  - [ ] PolicyName = "DetailData" for detail endpoints
- [ ] Explain cache policies configuration
  - [ ] Location in Program.cs
  - [ ] Duration settings
  - [ ] VaryByQuery parameters
- [ ] Document cache invalidation strategy
  - [ ] When cache is cleared
  - [ ] How to invalidate programmatically
- [ ] Add controller example with OutputCache attribute
- [ ] Note performance benefits

**Acceptance Criteria**:
- [ ] OutputCache attribute usage documented
- [ ] Policy names explained
- [ ] Configuration location referenced
- [ ] Cache invalidation strategy documented
- [ ] Example provided

**Blockers**: May need to verify actual cache configuration in Program.cs

---

### Task 2.6: Update ABOUTME Statistics
**Status**: ⏳ Not Started
**Estimated Time**: 15 minutes
**Files**: `docs/PROJECT.md`, `MEMORY.md`, any other files referencing coverage

- [ ] Search all documentation for "212" or "500" (old ABOUTME count)
- [ ] Update PROJECT.md with accurate count: 241/1546 files (15.6%)
- [ ] Update MEMORY.md if referenced there
- [ ] Update any other files referencing ABOUTME coverage
- [ ] Calculate percentage correctly: 241/1546 = 15.6%
- [ ] Note that coverage goal may differ from current state

**Acceptance Criteria**:
- [ ] All files updated with accurate 241/1546 count
- [ ] Percentage calculated correctly (15.6%)
- [ ] No outdated statistics remain in documentation

**Blockers**: None

---

## PHASE 3: SKILL & AGENT UPDATES ⏳ NOT STARTED
**Estimated Time**: 3-4 hours
**Priority**: MEDIUM - Quality improvements
**Depends On**: Phase 2 complete

### Task 3.1: Verify and Create Missing Resource Files
**Status**: ⏳ Not Started
**Estimated Time**: 2-3 hours
**Total Files**: 32 across 7 skills

**Process for each resource file**:
1. Check if file exists
2. If exists, verify accuracy and completeness
3. If missing, create with content from relevant SKILL.md section
4. Ensure follows placeholder syntax from TEMPLATE_GLOSSARY.md

#### auth-patterns/resources/ (1 file)
- [ ] user-id-extraction.md
  - [ ] Check if exists
  - [ ] If missing: Create with sub → nameidentifier → sid fallback pattern
  - [ ] Include controller example

#### blazor-bff-patterns/resources/ (4 files)
- [ ] bff-configuration.md
  - [ ] YARP configuration template
  - [ ] Program.cs setup
- [ ] token-forwarding.md
  - [ ] CircuitAccessTokenService implementation
  - [ ] AccessTokenForwardingHandler pattern
- [ ] auth-state-management.md
  - [ ] AuthenticationStateProvider usage
  - [ ] Circuit state management
- [ ] service-layer-patterns.md
  - [ ] IEventApiClient abstraction
  - [ ] Service implementation examples

#### blazor-ui-conventions/resources/ (7 files)
- [ ] mudblazor-usage.md
  - [ ] Component catalog
  - [ ] Common patterns
- [ ] component-design.md
  - [ ] Lifecycle methods
  - [ ] Parameter binding
- [ ] state-management.md
  - [ ] Component state
  - [ ] Cascading values
  - [ ] Service injection
- [ ] render-modes.md
  - [ ] InteractiveAuto explanation
  - [ ] When to use Server vs WASM
- [ ] bem-methodology.md
  - [ ] BEM naming convention
  - [ ] Examples
- [ ] theming.md
  - [ ] Dark/light mode switching
  - [ ] MudBlazor theme customization
- [ ] common-patterns.md
  - [ ] Forms
  - [ ] Tables
  - [ ] Dialogs

#### clean-architecture-rules/resources/ (4 files)
- [ ] dependency-rules.md
  - [ ] Layer dependency diagram
  - [ ] Allowed dependencies
- [ ] layer-responsibilities.md
  - [ ] What each layer does
  - [ ] What belongs where
- [ ] violation-examples.md
  - [ ] Common violations
  - [ ] How to spot them
- [ ] fix-patterns.md
  - [ ] How to refactor violations
  - [ ] Before/after examples

#### cqrs-mediatr-guidelines/resources/ (5 files)
- [ ] command-patterns.md
  - [ ] Create/Update/Delete command structure
  - [ ] BaseCommandResponse<T> usage
- [ ] query-patterns.md
  - [ ] Query structure
  - [ ] DTO return types
- [ ] handler-patterns.md
  - [ ] Handler implementation
  - [ ] Validation integration
- [ ] validation-integration.md
  - [ ] Manual validator instantiation
  - [ ] Error handling
- [ ] complete-examples.md
  - [ ] Full CQRS flow examples
  - [ ] End-to-end scenarios

#### dotnet-efcore-guidelines/resources/ (5 files)
- [ ] dbcontext-patterns.md
  - [ ] DbContext structure
  - [ ] Property injection for pooling
- [ ] entity-configuration.md
  - [ ] IEntityTypeConfiguration
  - [ ] Fluent API examples
- [ ] repository-pattern.md
  - [ ] Repository interface and implementation
  - [ ] Generic repository pattern
- [ ] querying-patterns.md
  - [ ] Include vs Select
  - [ ] Query filter usage
- [ ] migrations.md
  - [ ] Migration workflow
  - [ ] Seeding data

#### error-tracking/resources/ (6 files - BLOCKED)
**Note**: Depends on Task 3.2 (Sentry status clarification)

- [ ] api-exception-handling.md
- [ ] mediatr-logging-behavior.md
- [ ] db-performance-monitoring.md
- [ ] blazor-error-boundary.md
- [ ] sentry-middleware-config.md (if Sentry integrated)
- [ ] sentry-testing-endpoints.md (if Sentry integrated)

**Acceptance Criteria**:
- [ ] All 32 resource files exist
- [ ] Content matches or expands on SKILL.md sections
- [ ] No broken references in SKILL.md files
- [ ] All files follow placeholder syntax
- [ ] Examples are accurate and tested

**Blockers**: Task 3.2 must complete before error-tracking resources

---

### Task 3.2: Clarify Sentry Integration Status
**Status**: ⏳ Not Started
**Estimated Time**: 30 minutes
**File**: `.claude/skills/error-tracking/SKILL.md`
**BLOCKS**: Task 3.1 (error-tracking resources)

- [ ] **Investigate current Sentry integration**:
  - [ ] Check `Explore.API/Program.cs` for `UseSentry()` call
  - [ ] Search for Sentry package references in .csproj files
  - [ ] Check appsettings.json for Sentry DSN configuration
  - [ ] Search codebase for `SentryClient` or `ISentryClient` usage
  - [ ] Verify logging behaviors use Sentry
- [ ] **Document findings**
- [ ] **Take action based on findings**:

**If Sentry IS integrated**:
- [ ] Update error-tracking skill with actual configuration from Program.cs
- [ ] Add Sentry DSN configuration example
- [ ] Document actual logging behavior integration
- [ ] Update resource files to match implementation

**If Sentry NOT integrated**:
- [ ] **DECISION NEEDED**: Remove Sentry or create integration task?
  - [ ] Option A: Remove Sentry references, focus on built-in logging
    - [ ] Update SKILL.md to focus on ILogger, ProblemDetails
    - [ ] Remove Sentry-specific sections
    - [ ] Rename to `logging-and-error-handling` if appropriate
  - [ ] Option B: Create task to integrate Sentry
    - [ ] Keep skill as-is
    - [ ] Create implementation task for Sentry integration

**Acceptance Criteria**:
- [ ] Sentry integration status confirmed (yes/no)
- [ ] error-tracking skill updated to match reality
- [ ] Resource files created based on actual implementation
- [ ] If not integrated, decision made on next steps

**Blockers**: User decision on Sentry approach if not integrated

---

### Task 3.3: Add YAML Metadata to All Agents
**Status**: ⏳ Not Started
**Estimated Time**: 30 minutes
**Files**: 9 agent files

**YAML Template**:
```yaml
---
type: domain|guardrail|utility
enforcement: suggest|enforce|block
priority: critical|high|medium|low
---
```

**Agents to Update**:
- [ ] `.claude/agents/clean-code-architect.md`
  - [ ] Add: type=domain, enforcement=suggest, priority=medium
- [ ] `.claude/agents/documentation-architect.md`
  - [ ] Add: type=utility, enforcement=suggest, priority=low
- [ ] `.claude/agents/frontend-error-fixer.md`
  - [ ] Add: type=utility, enforcement=suggest, priority=medium
- [ ] `.claude/agents/plan-reviewer.md`
  - [ ] Add: type=guardrail, enforcement=enforce, priority=medium
- [ ] `.claude/agents/refactor-planner.md`
  - [ ] Add: type=domain, enforcement=suggest, priority=medium
- [ ] `.claude/agents/web-research-specialist.md`
  - [ ] Add: type=utility, enforcement=suggest, priority=low
- [ ] `.claude/agents/auth-route-tester.md`
  - [ ] Add: type=utility, enforcement=suggest, priority=medium
- [ ] `.claude/agents/auth-route-debugger.md`
  - [ ] Add: type=utility, enforcement=suggest, priority=medium
- [ ] `.claude/agents/auto-error-resolver.md`
  - [ ] Add: type=utility, enforcement=suggest, priority=high

**Acceptance Criteria**:
- [ ] All 9 agents have YAML front matter
- [ ] Classifications are consistent and accurate
- [ ] No syntax errors in YAML

**Blockers**: None

---

### Task 3.4: Expand dotnet-efcore-guidelines Skill
**Status**: ⏳ Not Started
**Estimated Time**: 45 minutes
**File**: `.claude/skills/dotnet-efcore-guidelines/SKILL.md`

- [ ] Add new section: "Complete Entity Auditing Pattern"
  - [ ] Document full entity template with all auditing fields:
    - CreatedAt (DateTime)
    - CreatedBy (Guid?)
    - UpdatedAt (DateTime?)
    - UpdatedBy (Guid?)
    - IsDeleted (bool)
    - DeletedAt (DateTime?)
    - DeletedBy (Guid?)
  - [ ] Show SaveChangesAsync override that populates these
  - [ ] Explain automatic soft delete interception
  - [ ] Reference QUICK_REFERENCE.md Rule #11

- [ ] Add new section: "Named Query Filters Pattern"
  - [ ] Explain EF Core 10+ named filter feature
  - [ ] Show QueryFilterNames constants usage
  - [ ] Provide tenant filter example
  - [ ] Provide soft delete filter example
  - [ ] Show IgnoreQueryFilter("FilterName") usage
  - [ ] Reference QUICK_REFERENCE.md Rule #12

- [ ] Update relevant resource files:
  - [ ] entity-configuration.md (add query filter examples)
  - [ ] dbcontext-patterns.md (add SaveChangesAsync override)

**Acceptance Criteria**:
- [ ] Auditing pattern section comprehensive with full example
- [ ] Named query filters section complete
- [ ] QueryFilterNames constants documented
- [ ] IgnoreQueryFilter usage shown
- [ ] Cross-references to QUICK_REFERENCE.md added
- [ ] Resource files updated

**Blockers**: None

---

## PHASE 4: DOCUMENTATION ENHANCEMENTS ⏳ NOT STARTED
**Estimated Time**: 3-4 hours
**Priority**: LOW - Nice to have improvements
**Depends On**: Phase 3 complete

### Task 4.1: Document ServiceResult<T> Pattern in BLAZOR.md
**Status**: ⏳ Not Started
**Estimated Time**: 30 minutes
**File**: `docs/BLAZOR.md`

- [ ] Add new section: "ServiceResult<T> Pattern (Available for Future Use)"
- [ ] Document class structure:
  - [ ] Success<T> factory method
  - [ ] Failure<T> factory methods
  - [ ] FromApiException helper
  - [ ] FromException helper
- [ ] Note current status: Defined but not currently in use
- [ ] Explain when pattern would be useful
  - [ ] Standardized error handling
  - [ ] Consistent service responses
  - [ ] Clear success/failure distinction
- [ ] Provide example usage (hypothetical):
  - [ ] Service implementation
  - [ ] Component consumption
  - [ ] Error display
- [ ] Note migration path if pattern is adopted in future

**Acceptance Criteria**:
- [ ] ServiceResult<T> pattern documented
- [ ] Current status clearly marked as "available but unused"
- [ ] Benefits explained
- [ ] Example usage provided
- [ ] Migration notes included

**Blockers**: None

---

### Task 4.2: Create TESTING.md Guide
**Status**: ⏳ Not Started
**Estimated Time**: 2 hours
**New File**: `docs/TESTING.md`

- [ ] Create new file `docs/TESTING.md`
- [ ] Add sections:
  1. **Testing Philosophy and Standards**
     - [ ] TDD principles (from CLAUDE.md)
     - [ ] Test quality standards
     - [ ] Coverage expectations

  2. **TUnit Framework Overview**
     - [ ] Why TUnit vs xUnit/NUnit
     - [ ] Key features
     - [ ] Installation and setup

  3. **Test Project Organization**
     - [ ] Event.Application.UnitTests
     - [ ] Event.Domain.UnitTests
     - [ ] Event.Architecture.Tests
     - [ ] Explore.Secrets.UnitTests
     - [ ] Event.Persistence.IntegrationTests
     - [ ] Event.API.IntegrationTests
     - [ ] Explore.Blazor.Client.Tests

  4. **Test Naming Conventions**
     - [ ] Method naming pattern
     - [ ] Class organization
     - [ ] Namespace structure

  5. **AAA Pattern (Arrange-Act-Assert)**
     - [ ] Explanation
     - [ ] Examples

  6. **Mocking Strategy**
     - [ ] When to mock
     - [ ] Repository mocking
     - [ ] Service mocking
     - [ ] HttpContext mocking

  7. **Test Data Builders and Fixtures**
     - [ ] Creating test data
     - [ ] Fixture patterns
     - [ ] Shared test utilities

  8. **Running Tests**
     - [ ] Per-project execution (CRITICAL - no solution-level testing)
     - [ ] Command examples with --project flag
     - [ ] TRX report generation for failure analysis
     - [ ] Verbosity levels

  9. **CI/CD Integration**
     - [ ] Pipeline configuration
     - [ ] Automated test runs
     - [ ] Test reporting

  10. **Common Testing Patterns**
      - [ ] Testing validators
      - [ ] Testing handlers (commands/queries)
      - [ ] Testing repositories
      - [ ] Testing controllers
      - [ ] Testing Blazor components

- [ ] Add to CLAUDE.md references
- [ ] Cross-reference in CONTRIBUTING.md

**Acceptance Criteria**:
- [ ] TESTING.md created and comprehensive
- [ ] All test projects documented
- [ ] TUnit framework explained
- [ ] Per-project execution emphasized
- [ ] AAA pattern explained with examples
- [ ] Mocking strategy documented
- [ ] Referenced from CLAUDE.md and CONTRIBUTING.md

**Blockers**: None

---

### Task 4.3: Complete PROJECT.md Sections
**Status**: ⏳ Not Started
**Estimated Time**: 30 minutes
**File**: `docs/PROJECT.md`

- [ ] Review all sections for incomplete content
- [ ] Address line 151: "Moderation system: Still todo"
  - [ ] **DECISION NEEDED**: Complete or remove?
  - [ ] If implementing, document moderation features
  - [ ] If removing, move to roadmap or delete
- [ ] Address "Liturgical temporal engine" section
  - [ ] Detail the feature or mark as future
  - [ ] If implemented, explain how it works
  - [ ] If planned, add to roadmap with timeline
- [ ] Search for other TODO or incomplete markers
- [ ] Ensure all sections provide value

**Acceptance Criteria**:
- [ ] No "Still todo" placeholders remain
- [ ] All sections complete or properly noted as future
- [ ] Removed items moved to ROADMAP.md if appropriate
- [ ] Document provides complete project overview

**Blockers**: User decisions on moderation and liturgical features

---

### Task 4.4: Create Skill/Agent Reference Matrix
**Status**: ⏳ Not Started
**Estimated Time**: 1 hour
**New File**: `docs/SKILLS_AGENTS_REFERENCE.md`

- [ ] Create new file `docs/SKILLS_AGENTS_REFERENCE.md`
- [ ] Add Introduction section explaining purpose
- [ ] Create "Rules → Skills" Matrix:
  ```markdown
  | CLAUDE.md Rule | Skills Covering | Enforcement Level |
  |----------------|-----------------|-------------------|
  | #1: Repositories → Entities | dotnet-efcore, cqrs-mediatr | Block |
  | #2: Manual Validators | clean-architecture, cqrs-mediatr | Block |
  ...
  ```
- [ ] Create "Rules → Agents" Matrix:
  ```markdown
  | CLAUDE.md Rule | Agents Enforcing | Usage |
  |----------------|------------------|-------|
  | #1, #2, #3 | code-refactor-master | Refactoring compliance |
  | All rules | code-architecture-reviewer | PR review |
  ...
  ```
- [ ] Create "Skill → Resources" Mapping:
  - [ ] List each skill
  - [ ] Show resource files
  - [ ] Quick description
- [ ] Create "Agent → Skills" Mapping:
  - [ ] List each agent
  - [ ] Show which skills it uses
  - [ ] When to use it
- [ ] Add "Quick Lookup by Rule Number" section:
  - [ ] Rule number → Skills → Agents → Resources
  - [ ] Fast navigation path
- [ ] Add "Usage Scenarios" section:
  - [ ] "I'm creating a new entity" → Use X skill, Y agent
  - [ ] "I'm refactoring CQRS" → Use X skill, Y agent
  - [ ] "I'm reviewing a PR" → Use X agent
  - [ ] etc.

**Acceptance Criteria**:
- [ ] Matrix created with all rules mapped
- [ ] All skills mapped to rules
- [ ] All agents mapped to skills and rules
- [ ] Quick lookup section provides fast navigation
- [ ] Usage scenarios help users find right tools
- [ ] Cross-referenced from CLAUDE.md

**Blockers**: None

---

### Task 4.5: Verify Missing Referenced Documentation
**Status**: ⏳ Not Started
**Estimated Time**: 30 minutes

**Files to Check**:
- [ ] `docs/ADMIN_HIERARCHY.md` (referenced in MULTI_TENANCY.md)
  - [ ] Verify file exists
  - [ ] If exists, read and validate completeness
  - [ ] If incomplete, note gaps
  - [ ] If missing, decide: create stub or remove reference

- [ ] `docs/DEPLOYMENT_MODES.md` (referenced in MULTI_TENANCY.md)
  - [ ] Verify file exists
  - [ ] If exists, validate completeness
  - [ ] If missing, decide: create or remove reference

- [ ] `docs/EXTENSIBILITY.md` (referenced in MULTI_TENANCY.md)
  - [ ] Verify file exists
  - [ ] If exists, validate completeness
  - [ ] If missing, decide: create or remove reference

- [ ] `docs/RENDER_POLICIES.md` (mentioned in analysis)
  - [ ] Verify file exists
  - [ ] If exists, validate completeness
  - [ ] If missing, decide: create or remove reference

- [ ] `docs/MODULAR_EVENTS.md` (mentioned in CODEBASE_STRUCTURE.md)
  - [ ] Verify file exists
  - [ ] If exists, validate completeness
  - [ ] If missing, decide: create or remove reference

**For Each Missing File**:
- [ ] **Option A**: Create stub with "Coming soon" and planned date
- [ ] **Option B**: Remove reference if not planned
- [ ] **Option C**: Add to documentation backlog
- [ ] Update MULTI_TENANCY.md or referencing files accordingly

**Acceptance Criteria**:
- [ ] All referenced files verified (exist or references removed)
- [ ] No broken documentation links
- [ ] Missing files either created, removed, or backlogged
- [ ] Referencing files updated accordingly

**Blockers**: User decisions on which docs to create vs remove

---

## SUMMARY STATUS

### Overall Progress
- [ ] **Phase 1**: Critical Fixes (0/3 tasks) - ⏳ Not Started
- [ ] **Phase 2**: Documentation Updates (0/6 tasks) - ⏳ Not Started
- [ ] **Phase 3**: Skill & Agent Updates (0/4 tasks) - ⏳ Not Started
- [ ] **Phase 4**: Documentation Enhancements (0/5 tasks) - ⏳ Not Started

**Total Tasks**: 18
**Completed**: 0
**In Progress**: 0
**Not Started**: 18

---

## BLOCKERS SUMMARY

### User Decisions Required
1. **BMAD Agents**: Delete or archive? (Task 1.1)
2. **Federation Status**: What is actually implemented? (Task 1.2)
3. **Sentry Integration**: Is it integrated? If not, remove or integrate? (Task 3.2)
4. **BYOK Timeline**: Payment/analytics planned dates or remove? (Task 2.4)
5. **PROJECT.md Features**: Complete or remove moderation/liturgical sections? (Task 4.3)
6. **Missing Docs**: Create or remove references? (Task 4.5)

### Task Dependencies
- Phase 2 depends on Phase 1 complete
- Phase 3 depends on Phase 2 complete
- Phase 4 depends on Phase 3 complete
- Task 3.1 (error-tracking resources) depends on Task 3.2 (Sentry status)

---

## NEXT STEPS

1. **Get user approval** on overall plan
2. **Get user decisions** on blockers listed above
3. **Start Phase 1** - Critical fixes
4. **Update this task file** as work progresses
5. **Mark tasks complete** as they finish
6. **Update context file** after major milestones

---

**Ready to Proceed?** ✅ Awaiting user approval and blocker resolution
## Context Reset Session Update (2026-02-15 21:26 Europe/Brussels)

- Status update: No task-state changes in this session for this track.
- Priority update: Keep existing ordering; analytics work was handled in a separate track.
- Next step: Resume from current in-progress or highest-priority unchecked item.

## Context Reset Session Update (2026-02-23 18:12 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority focused on admin consolidation handoff in navbar customization track.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in this task file.
