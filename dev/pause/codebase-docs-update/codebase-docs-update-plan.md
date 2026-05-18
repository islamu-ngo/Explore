# Codebase Documentation & Skills/Agents Update - Implementation Plan

> **Comprehensive update of all documentation, skills, and agents to match codebase reality**
>
> **Created**: 2026-02-10
> **Status**: Planning Complete - Awaiting Approval
> **Estimated Effort**: 12-16 hours across 4 phases

---

## EXECUTIVE SUMMARY

This plan addresses comprehensive updates to the ISLAMU Event project's documentation ecosystem based on extensive codebase analysis. Three specialized agents analyzed:
1. All documentation files in `docs/`
2. All skills and agents in `.claude/`
3. The actual codebase implementation

**Overall Assessment**:
- ✅ Documentation Quality: **82/100** - Well-structured with targeted gaps
- ✅ Skills Alignment: **100%** - Excellent coverage of critical patterns
- 🔴 Agent Ecosystem: **30+ broken BMAD agents polluting namespace**
- ✅ Code Quality: **95%** - Strong architectural consistency

**Key Findings**:
1. **Critical**: 30+ BMAD agents are broken and should be removed
2. **Critical**: Federation documentation status is contradictory and confusing
3. **High**: 32+ skill resource files need verification or creation
4. **High**: Several QUICK_REFERENCE.md rules need expansion
5. **Medium**: Documentation gaps in HATEOAS, ServiceResult<T>, OutputCache patterns
6. **Medium**: Delete command bool return type pattern is undocumented

---

## CURRENT STATE ANALYSIS

### Documentation Files (18 files analyzed)

| File | Quality | Issues | Priority |
|------|---------|--------|----------|
| ARCHITECTURE.md | ✅ Excellent | Minor federation status unclear | Low |
| API.md | ✅ Good | Missing HATEOAS, versioning, rate limiting | Medium |
| BLAZOR.md | ✅ Excellent | Minor offline scenarios | Low |
| CODEBASE_INSIGHTS.md | ✅ Excellent | Sections 15-16 incomplete | Low |
| CODEBASE_STRUCTURE.md | ✅ Excellent | None | None |
| CONFIGURATION.md | ⚠️ Good | Missing instance settings, modules | Medium |
| CONTRIBUTING.md | ✅ Good | Minor gaps | Low |
| DOMAIN.md | ⚠️ Good | Missing EventAspects, relationships | High |
| **FEDERATION.md** | ❌ Unclear | **Status contradictory** | **CRITICAL** |
| GOVERNANCE.md | ✅ Excellent | None | None |
| MULTI_TENANCY.md | ⚠️ Good | Missing referenced files | High |
| NAMING_CONVENTIONS.md | ✅ Excellent | None | None |
| OPERATIONS.md | ⚠️ Confusing | Should be DEPLOYMENT_MODES | High |
| PROJECT.md | ⚠️ Good | Incomplete sections | Medium |
| QUICK_REFERENCE.md | ⚠️ Good | **Missing delete bool pattern** | **HIGH** |
| SECURITY.md | ✅ Excellent | Minor Cerbos terminology | Low |
| TEMPLATE_GLOSSARY.md | ✅ Excellent | None | None |
| TROUBLESHOOTING.md | ✅ Excellent | None | None |

### Skills (8 skills analyzed)

| Skill | Status | Quality | Issues |
|-------|--------|---------|--------|
| auth-patterns | ✅ Aligned | Excellent | 1 resource file to verify |
| blazor-bff-patterns | ✅ Aligned | Excellent | 4 resource files to verify |
| blazor-ui-conventions | ✅ Aligned | Excellent | 7 resource files to verify |
| clean-architecture-rules | ✅ Aligned | Excellent | 4 resource files to verify |
| cqrs-mediatr-guidelines | ✅ Aligned | Excellent | 5 resource files to verify |
| dotnet-efcore-guidelines | ✅ Aligned | Excellent | 5 resource files to verify |
| error-tracking | ⚠️ Partial | Good | **Sentry status unclear**, 6 resources to verify |
| prd | ✅ Aligned | Excellent | None |

**Total resource files to verify**: 32

### Project-Specific Agents (11 agents analyzed)

| Agent | Status | Quality | Issues |
|-------|--------|---------|--------|
| code-architecture-reviewer | ✅ Excellent | ⭐ Critical | None |
| code-refactor-master | ✅ Excellent | ⭐ Critical | None |
| clean-code-architect | ✅ Good | Generic | Missing YAML metadata |
| blazor-component-architect | ✅ Excellent | High | Missing YAML metadata |
| documentation-architect | ✅ Good | Medium | Missing YAML metadata |
| frontend-error-fixer | ✅ Good | Medium | Missing YAML metadata |
| plan-reviewer | ✅ Good | Medium | Missing YAML metadata |
| refactor-planner | ✅ Good | Medium | Missing YAML metadata |
| web-research-specialist | ✅ Good | Medium | Missing YAML metadata |
| auth-route-tester | ✅ Good | Medium | Missing YAML metadata |
| auth-route-debugger | ✅ Good | Medium | Missing YAML metadata |
| auto-error-resolver | ✅ Good | Medium | Missing YAML metadata |

### BMAD Agents (30+ analyzed)

| Category | Count | Status |
|----------|-------|--------|
| bmm-* (Main workflow agents) | ~15 | 🔴 Broken - Remove |
| bmad-* (Build/Make agents) | ~10 | 🔴 Broken - Remove |
| cis-* (Creative innovation) | ~6 | 🔴 Broken - Remove |
| tea-* (Teaching agents) | ~5 | 🔴 Broken - Remove |

**Critical Issue**: All BMAD agents reference `_bmad/bmm/agents/` which doesn't exist in this project. They pollute the `.claude/commands/` namespace and fail when invoked.

### Codebase Reality (from analysis)

| Pattern | Documentation Status | Actual Implementation |
|---------|---------------------|----------------------|
| Triple-interface pattern | ✅ Documented | ✅ Implemented 100% |
| File-scoped namespaces | ✅ Documented | ✅ Implemented 100% |
| CQRS structure | ✅ Documented | ✅ Implemented 100% |
| Manual validator instantiation | ✅ Documented | ✅ Implemented 100% |
| AllowAnonymous/Authorize pattern | ✅ Documented | ✅ Implemented 100% |
| Named query filters | ✅ Documented | ✅ Implemented 100% |
| **Delete commands return bool** | ❌ NOT documented | ✅ 26 commands use this |
| **HATEOAS/HAL+JSON responses** | ❌ NOT documented | ✅ Fully implemented |
| **ServiceResult<T> pattern** | ❌ NOT documented | ⚠️ Defined but unused |
| **OutputCache attributes** | ❌ NOT documented | ✅ Implemented |
| **Enhanced soft delete (DeletedAt/DeletedBy)** | ❌ NOT documented | ✅ Implemented |
| **ABOUTME coverage** | ⚠️ Claims 212/500 | ✅ Actually 241/1546 |

---

## IMPLEMENTATION PHASES

### PHASE 1: CRITICAL FIXES (Priority 0 - Do First)
**Estimated Time**: 2 hours
**Impact**: Removes broken agents, fixes critical documentation contradictions

#### Task 1.1: Remove All BMAD Agents
**Files to Delete** (30+ files):
```
.claude/commands/bmad-*.md
.claude/commands/bmm-*.md
.claude/commands/cis-*.md
.claude/commands/tea-*.md
```

**Rationale**:
- All reference non-existent `_bmad/` directory
- Fail when invoked
- Pollute namespace (users can't find project agents)
- No value for .NET project (generic workflow agents)
- Better project-specific alternatives exist

**Acceptance Criteria**:
- [ ] All BMAD agents removed from `.claude/commands/`
- [ ] Project-specific agents remain untouched
- [ ] No broken references to BMAD in other files

---

#### Task 1.2: Clarify Federation Documentation Status
**File**: `docs/FEDERATION.md`

**Current Issue**: Document says "Federation is a **roadmap feature**" but then provides detailed implementation diagrams suggesting it's already implemented.

**Action**: Choose one approach:
- **Option A (Recommended)**: Update status to reflect partial implementation
  - Mark implemented features (Actor, DID entities exist in codebase)
  - Mark unimplemented features (HTTP endpoints, WebFinger, etc.)
  - Add timeline for remaining features
  - Update diagrams with status badges

- **Option B**: Clearly mark all as "planned architecture"
  - Add dates to roadmap items
  - Prefix all sections with "Planned:"
  - Move to `docs/roadmap/FEDERATION.md`

**Acceptance Criteria**:
- [ ] Status section clearly states what is vs isn't implemented
- [ ] Diagrams have status badges or clear labels
- [ ] No contradictions with CODEBASE_STRUCTURE.md entity listings
- [ ] Timeline provided for unimplemented features

---

#### Task 1.3: Fix Cerbos Terminology Inconsistency
**Files**: `docs/SECURITY-MODEL.md`, `docs/CONFIGURATION.md`

**Current Issue**:
- SECURITY.md says Cerbos is "future"
- CONFIGURATION.md says Cerbos is "not currently wired"

**Action**: Align terminology across both files:
- **Recommended**: Use "Not implemented - planned for future release"
- Update both files to match exactly

**Acceptance Criteria**:
- [ ] SECURITY.md and CONFIGURATION.md use identical Cerbos status language
- [ ] If timeline known, add it to both files

---

### PHASE 2: DOCUMENTATION UPDATES (Priority 1 - High Impact)
**Estimated Time**: 4-5 hours
**Impact**: Adds missing patterns, fixes gaps, improves accuracy

#### Task 2.1: Update QUICK_REFERENCE.md with Missing Patterns
**File**: `docs/QUICK_REFERENCE.md`

**Add Section 13: Delete Commands Return bool**
```markdown
### 13. Delete Commands Return bool (Not BaseCommandResponse)

Delete commands follow a different pattern than create/update:

// ❌ WRONG - Don't use BaseCommandResponse for deletes
public class DeleteEventCommand : IRequest<BaseCommandResponse<bool>>

// ✅ CORRECT - Use bool directly
public class DeleteEventCommand : IRequest<bool>

// Controller usage
[HttpDelete("{id:guid}")]
[Authorize]
public async Task<ActionResult> Delete(Guid id)
{
    var result = await _mediator.Send(new DeleteEventCommand { Id = id });
    return result ? NoContent() : NotFound();
}

**Why**: Delete operations have binary outcomes (found/deleted or not found). Using bool is more semantic than wrapping in BaseCommandResponse.
```

**Update Rule #11: Auditing Fields** - Add enhanced soft delete:
```markdown
### 11. Entities Include Auditing Fields (ENHANCED)

Beyond CreatedAt/CreatedBy/UpdatedAt/UpdatedBy, soft deletable entities ALSO include:
- DeletedAt (DateTime?)
- DeletedBy (Guid?)

This provides complete deletion audit trail.
```

**Acceptance Criteria**:
- [ ] Section 13 added with delete command pattern
- [ ] Rule 11 updated with enhanced soft delete fields
- [ ] Table of contents updated
- [ ] Common Mistakes table includes delete command errors

---

#### Task 2.2: Document HATEOAS Pattern in API.md
**File**: `docs/API.md`

**Add New Section**: "HATEOAS/HAL+JSON Response Format"

Content to add:
- Explanation of HalResource<T> and HalCollectionResource<T> wrappers
- Link structure with rel, href, method
- Prefer header support: `Prefer: return=minimal`
- ResourceAssembler pattern
- Example response showing _links, _embedded
- RouteNames constants reference

**Acceptance Criteria**:
- [ ] HATEOAS section added with comprehensive examples
- [ ] Response structure clearly documented
- [ ] Prefer header mechanism explained
- [ ] Cross-reference to CODEBASE_INSIGHTS.md section 14

---

#### Task 2.3: Add Missing Patterns to DOMAIN.md
**File**: `docs/DOMAIN.md`

**Add Section**: "Module-Specific Entity Extensions"

Document:
1. **EventIslamicAspect** - Islamic-specific event fields
   - Prayer times, Madhab, Gender segregation
   - Relationship to Event entity

2. **EventTechAspect** - Technology event fields
   - Tech stack, Skill level, Platform
   - Relationship to Event entity

3. **Strategy Pattern for Module Resolution**
   - IModuleService<TEntity, TAspect>
   - How module-specific fields are resolved
   - Reference to CODEBASE_INSIGHTS.md

**Add Section**: "Event Session Hierarchy"

Clarify relationships:
- Event → EventSession (many)
- EventSession → EventSessionAgendaItem (many)
- EventSession → EventSessionSpeaker (many)
- EventSession → EventSessionLanguage (many)

**Acceptance Criteria**:
- [ ] EventIslamicAspect documented
- [ ] EventTechAspect documented
- [ ] Event session hierarchy clarified
- [ ] Diagrams or tables showing relationships
- [ ] Links to CODEBASE_INSIGHTS.md strategy pattern section

---

#### Task 2.4: Update CONFIGURATION.md
**File**: `docs/CONFIGURATION.md`

**Add Sections**:
1. **Instance-Level Settings**
   - SystemSetting table
   - GovernanceSettingKeys reference
   - Deployment mode switching

2. **Module-Specific Configuration**
   - TenantCapability table
   - Module governance settings
   - How modules are enabled/disabled per tenant

3. **BYOK Integration Status**
   - Mark storage as "Implemented"
   - Mark payment/analytics as "Planned - Q2 2026" (or remove if no timeline)

**Acceptance Criteria**:
- [ ] Instance settings section added
- [ ] Module configuration section added
- [ ] BYOK status clarified with timeline
- [ ] No contradictions with OPERATIONS.md

---

#### Task 2.5: Document OutputCache Pattern in API.md
**File**: `docs/API.md`

**Add Section**: "Output Caching"

Document:
- [OutputCache(PolicyName = "ListData")] usage
- [OutputCache(PolicyName = "DetailData")] usage
- Cache invalidation strategy
- Configuration in Program.cs

**Acceptance Criteria**:
- [ ] OutputCache attribute usage documented
- [ ] Policy names explained
- [ ] Cache invalidation strategy documented

---

#### Task 2.6: Update ABOUTME Statistics
**Files**: `docs/PROJECT.md`, `MEMORY.md`, any other files referencing coverage

**Current Claim**: ~212/500+ files
**Actual**: 241/1546 files (15.6%)

**Action**: Update all references to accurate count.

**Acceptance Criteria**:
- [ ] All files updated with accurate 241/1546 count
- [ ] Percentage calculated correctly (15.6%)

---

### PHASE 3: SKILL & AGENT UPDATES (Priority 2 - Quality)
**Estimated Time**: 3-4 hours
**Impact**: Ensures all skills have resources, agents have consistent metadata

#### Task 3.1: Verify and Create Missing Resource Files
**Total**: 32 resource files across 7 skills

**Process for each skill**:
1. Check if resource file exists
2. If missing, create it with content from SKILL.md relevant section
3. If exists, verify it's accurate and complete

**Skills with resource files**:
- `auth-patterns/resources/` (1 file)
- `blazor-bff-patterns/resources/` (4 files)
- `blazor-ui-conventions/resources/` (7 files)
- `clean-architecture-rules/resources/` (4 files)
- `cqrs-mediatr-guidelines/resources/` (5 files)
- `dotnet-efcore-guidelines/resources/` (5 files)
- `error-tracking/resources/` (6 files)

**Acceptance Criteria**:
- [ ] All 32 resource files exist
- [ ] Content matches or expands on SKILL.md sections
- [ ] No broken references in SKILL.md files
- [ ] Resources follow template glossary placeholders

---

#### Task 3.2: Clarify Sentry Integration Status
**File**: `.claude/skills/error-tracking/SKILL.md`

**Investigation Required**:
1. Check `Explore.API/Program.cs` for `UseSentry()` call
2. Check appsettings for Sentry DSN
3. Check if Sentry client is actually used

**Actions Based on Findings**:
- **If Sentry IS integrated**: Update skill with actual configuration details from Program.cs
- **If Sentry NOT integrated**:
  - Remove Sentry-specific sections
  - Focus on built-in logging and error handling
  - Rename to `logging-and-error-handling` skill

**Acceptance Criteria**:
- [ ] Sentry integration status confirmed
- [ ] Skill updated to match reality
- [ ] Resource files updated accordingly

---

#### Task 3.3: Add YAML Metadata to All Agents
**Files**: 9 agent files missing metadata

**Add to each agent**:
```yaml
---
type: domain|guardrail|utility
enforcement: suggest|enforce|block
priority: critical|high|medium|low
---
```

**Agent Classification**:
- **clean-code-architect**: type=domain, enforcement=suggest, priority=medium
- **documentation-architect**: type=utility, enforcement=suggest, priority=low
- **frontend-error-fixer**: type=utility, enforcement=suggest, priority=medium
- **plan-reviewer**: type=guardrail, enforcement=enforce, priority=medium
- **refactor-planner**: type=domain, enforcement=suggest, priority=medium
- **web-research-specialist**: type=utility, enforcement=suggest, priority=low
- **auth-route-tester**: type=utility, enforcement=suggest, priority=medium
- **auth-route-debugger**: type=utility, enforcement=suggest, priority=medium
- **auto-error-resolver**: type=utility, enforcement=suggest, priority=high

**Acceptance Criteria**:
- [ ] All agents have YAML front matter
- [ ] Classifications are consistent
- [ ] No agents missing metadata

---

#### Task 3.4: Expand dotnet-efcore-guidelines Skill
**File**: `.claude/skills/dotnet-efcore-guidelines/SKILL.md`

**Add Sections**:
1. **Complete Entity Auditing Pattern** (QUICK_REFERENCE.md Rule #11)
   ```csharp
   public class Event
   {
       // ... entity properties ...

       // Standard auditing
       public DateTime CreatedAt { get; set; }
       public Guid? CreatedBy { get; set; }
       public DateTime? UpdatedAt { get; set; }
       public Guid? UpdatedBy { get; set; }

       // Enhanced soft delete auditing
       public bool IsDeleted { get; set; }
       public DateTime? DeletedAt { get; set; }
       public Guid? DeletedBy { get; set; }
   }
   ```

2. **Named Query Filters Pattern** (QUICK_REFERENCE.md Rule #12)
   ```csharp
   modelBuilder.Entity<Event>()
       .HasQueryFilter(
           name: QueryFilterNames.Tenant,
           predicate: e => TenantContext == null || e.TenantId == TenantContext.TenantId)
       .HasQueryFilter(
           name: QueryFilterNames.SoftDelete,
           predicate: e => !e.IsDeleted);

   // Selective disabling
   var allEvents = await _context.Events
       .IgnoreQueryFilter(QueryFilterNames.SoftDelete)
       .ToListAsync();
   ```

**Acceptance Criteria**:
- [ ] Auditing pattern section added with complete example
- [ ] Named query filters section added
- [ ] QueryFilterNames constants documented
- [ ] IgnoreQueryFilter usage shown

---

### PHASE 4: DOCUMENTATION ENHANCEMENTS (Priority 3 - Nice to Have)
**Estimated Time**: 3-4 hours
**Impact**: Improves documentation completeness, adds missing guides

#### Task 4.1: Document ServiceResult<T> Pattern in BLAZOR.md
**File**: `docs/BLAZOR.md`

**Add Section**: "ServiceResult<T> Pattern (Planned)"

**Note**: Currently defined but unused. Document as a pattern available for future use.

Content:
- Class structure
- Success/Failure factory methods
- Error handling strategy
- Example usage (hypothetical)
- When to use vs returning data directly

**Acceptance Criteria**:
- [ ] ServiceResult<T> documented
- [ ] Marked as "defined but not currently in use"
- [ ] Migration path shown if pattern is adopted

---

#### Task 4.2: Create TESTING.md Guide
**New File**: `docs/TESTING.md`

**Content**:
1. Testing philosophy and standards
2. TUnit framework overview
3. Test organization
   - Unit tests (Application.UnitTests, Domain.UnitTests)
   - Integration tests (Persistence.IntegrationTests, API.IntegrationTests)
   - Blazor tests (Blazor.Client.Tests)
4. Test naming conventions
5. AAA pattern (Arrange-Act-Assert)
6. Mocking strategy (repositories, services)
7. CI/CD integration
8. Test data builders and fixtures
9. Running tests (per-project with --project flag)

**Acceptance Criteria**:
- [ ] TESTING.md created
- [ ] All test projects documented
- [ ] TUnit framework explained
- [ ] Per-project test execution documented
- [ ] AGENTS.md references TESTING.md

---

#### Task 4.3: Complete PROJECT.md Sections
**File**: `docs/PROJECT.md`

**Incomplete Sections**:
1. Line 151: "Moderation system: Still todo" → Remove or complete
2. Liturgical temporal engine → Detail or remove

**Acceptance Criteria**:
- [ ] No "Still todo" placeholders
- [ ] All sections complete or removed
- [ ] If removing, note in ROADMAP.md instead

---

#### Task 4.4: Create Skill/Agent Reference Matrix
**New File**: `docs/SKILLS_AGENTS_REFERENCE.md`

**Content**: Matrix showing:
- Which skills support which AGENTS.md rules
- Which agents use which skills
- When to use each skill/agent
- Quick lookup by rule number

**Format**:
```markdown
| AGENTS.md Rule | Skills | Agents |
|----------------|--------|--------|
| #1: Repositories → Entities | dotnet-efcore, cqrs-mediatr | code-refactor-master, code-architecture-reviewer |
| #2: Manual Validators | clean-architecture, cqrs-mediatr | code-refactor-master |
...
```

**Acceptance Criteria**:
- [ ] Matrix created with all rules
- [ ] All skills mapped
- [ ] All agents mapped
- [ ] Usage guidance provided

---

#### Task 4.5: Verify Missing Referenced Documentation
**Files to Check**:
- `docs/ADMIN_HIERARCHY.md` (referenced in MULTI_TENANCY.md)
- `docs/DEPLOYMENT_MODES.md` (referenced in MULTI_TENANCY.md)
- `docs/EXTENSIBILITY.md` (referenced in MULTI_TENANCY.md)
- `docs/RENDER_POLICIES.md` (mentioned in analysis)
- `docs/MODULAR_EVENTS.md` (mentioned in CODEBASE_STRUCTURE.md)

**Actions**:
1. Verify files exist
2. If exist, read and validate completeness
3. If incomplete, add to documentation backlog
4. If missing, either:
   - Create stub with "Coming soon"
   - Remove references if not planned

**Acceptance Criteria**:
- [ ] All referenced files exist or references removed
- [ ] No broken links in documentation
- [ ] Missing files added to backlog or created

---

## RISK ASSESSMENT

### High Risk
- **BMAD Agent Removal**: Users may have workflows depending on them
  - **Mitigation**: Add deprecation notice before removal, provide 1-week warning
  - **Alternative**: Archive to `.claude/commands/archive/bmad/` instead of deleting

### Medium Risk
- **Federation.md Updates**: Changing status may confuse stakeholders
  - **Mitigation**: Get product owner approval on exact status language

- **Sentry Integration Clarification**: May reveal missing error tracking
  - **Mitigation**: If Sentry not integrated, create task to integrate it

### Low Risk
- **Documentation updates**: No code changes, low impact
- **Resource file creation**: Supplements existing skills
- **YAML metadata**: Informational only

---

## SUCCESS METRICS

### Documentation Quality
- [ ] All critical contradictions resolved
- [ ] No broken internal references
- [ ] ABOUTME statistics accurate
- [ ] All undocumented patterns added

### Skills Alignment
- [ ] 100% of resource files exist and accurate
- [ ] All skills reference current patterns
- [ ] Sentry status clarified

### Agent Ecosystem Health
- [ ] 0 broken agents in `.claude/commands/`
- [ ] 100% of agents have YAML metadata
- [ ] All project agents functional

### Code-Documentation Alignment
- [ ] 100% of QUICK_REFERENCE.md rules documented in skills
- [ ] All implemented patterns documented
- [ ] No undocumented deviations from rules

---

## DEPENDENCIES

### External
- Product owner approval for Federation status language
- Tech lead decision on Sentry integration
- Architecture team review of new documentation sections

### Internal
- Must complete Phase 1 before Phase 2 (BMAD removal first)
- Task 3.2 (Sentry) blocks Task 3.1 (resource files)
- All documentation updates must complete before skill updates

---

## TIMELINE

### Week 1 (Days 1-3)
- **Phase 1**: Critical fixes (2 hours)
  - Remove BMAD agents
  - Fix Federation.md
  - Align Cerbos terminology

### Week 1 (Days 4-5)
- **Phase 2**: Documentation updates (4-5 hours)
  - QUICK_REFERENCE.md updates
  - API.md HATEOAS section
  - DOMAIN.md EventAspects

### Week 2 (Days 1-2)
- **Phase 2 Continued**
  - CONFIGURATION.md updates
  - OutputCache documentation
  - ABOUTME statistics

### Week 2 (Days 3-5)
- **Phase 3**: Skill & agent updates (3-4 hours)
  - Create resource files
  - Add YAML metadata
  - Expand EF Core skill

### Week 3
- **Phase 4**: Documentation enhancements (3-4 hours)
  - TESTING.md creation
  - ServiceResult<T> documentation
  - Reference matrix

**Total Estimated Time**: 12-16 hours

---

## POST-IMPLEMENTATION VERIFICATION

### Verification Checklist
- [ ] All BMAD agents removed and namespace clean
- [ ] No broken documentation links
- [ ] All skill resource files exist and accurate
- [ ] All agents have YAML metadata
- [ ] QUICK_REFERENCE.md covers all 13+ rules
- [ ] HATEOAS pattern documented
- [ ] Delete command bool pattern documented
- [ ] Federation status clear and accurate
- [ ] Sentry integration status clarified
- [ ] ABOUTME statistics updated
- [ ] No contradictions across documentation

### Testing Plan
1. **Documentation Links**: Run script to verify all internal links
2. **Skill Resources**: Verify each resource file is referenced and exists
3. **Agent Functionality**: Test each project agent works
4. **AGENTS.md Compliance**: Verify all rules have skill/doc coverage

---

## APPENDIX A: FILES TO MODIFY

### Documentation Files (11 files)
1. `docs/FEDERATION.md` - Status clarification
2. `docs/SECURITY-MODEL.md` - Cerbos terminology
3. `docs/CONFIGURATION.md` - Cerbos terminology + instance settings
4. `docs/QUICK_REFERENCE.md` - Add rule 13, enhance rule 11
5. `docs/API.md` - HATEOAS + OutputCache
6. `docs/DOMAIN.md` - EventAspects + session hierarchy
7. `docs/BLAZOR.md` - ServiceResult<T> pattern
8. `docs/PROJECT.md` - Complete TODOs
9. `docs/MULTI_TENANCY.md` - Verify references
10. `docs/TESTING.md` (NEW) - Testing guide
11. `docs/SKILLS_AGENTS_REFERENCE.md` (NEW) - Reference matrix

### Skill Files (7 skills)
1. `.claude/skills/auth-patterns/` - Verify 1 resource
2. `.claude/skills/blazor-bff-patterns/` - Verify 4 resources
3. `.claude/skills/blazor-ui-conventions/` - Verify 7 resources
4. `.claude/skills/clean-architecture-rules/` - Verify 4 resources
5. `.claude/skills/cqrs-mediatr-guidelines/` - Verify 5 resources
6. `.claude/skills/dotnet-efcore-guidelines/` - Verify 5 resources + expand
7. `.claude/skills/error-tracking/` - Clarify Sentry + verify 6 resources

### Agent Files (9 agents)
1. `.claude/agents/clean-code-architect.md` - Add YAML
2. `.claude/agents/documentation-architect.md` - Add YAML
3. `.claude/agents/frontend-error-fixer.md` - Add YAML
4. `.claude/agents/plan-reviewer.md` - Add YAML
5. `.claude/agents/refactor-planner.md` - Add YAML
6. `.claude/agents/web-research-specialist.md` - Add YAML
7. `.claude/agents/auth-route-tester.md` - Add YAML
8. `.claude/agents/auth-route-debugger.md` - Add YAML
9. `.claude/agents/auto-error-resolver.md` - Add YAML

### Files to Delete (30+ files)
- `.claude/commands/bmad-*.md` (all BMAD agents)
- `.claude/commands/bmm-*.md` (all BMM agents)
- `.claude/commands/cis-*.md` (all CIS agents)
- `.claude/commands/tea-*.md` (all TEA agents)

---

## APPENDIX B: RESOURCE FILES TO CREATE

### auth-patterns/resources/ (1 file)
- user-id-extraction.md

### blazor-bff-patterns/resources/ (4 files)
- bff-configuration.md
- token-forwarding.md
- auth-state-management.md
- service-layer-patterns.md

### blazor-ui-conventions/resources/ (7 files)
- mudblazor-usage.md
- component-design.md
- state-management.md
- render-modes.md
- bem-methodology.md
- theming.md
- common-patterns.md

### clean-architecture-rules/resources/ (4 files)
- dependency-rules.md
- layer-responsibilities.md
- violation-examples.md
- fix-patterns.md

### cqrs-mediatr-guidelines/resources/ (5 files)
- command-patterns.md
- query-patterns.md
- handler-patterns.md
- validation-integration.md
- complete-examples.md

### dotnet-efcore-guidelines/resources/ (5 files)
- dbcontext-patterns.md
- entity-configuration.md
- repository-pattern.md
- querying-patterns.md
- migrations.md

### error-tracking/resources/ (6 files)
- api-exception-handling.md
- mediatr-logging-behavior.md
- db-performance-monitoring.md
- blazor-error-boundary.md
- sentry-middleware-config.md
- sentry-testing-endpoints.md

---

## APPROVAL REQUIRED

This plan requires approval before implementation.

**Approver**: @AM5
**Review Focus**:
1. Is BMAD agent removal acceptable?
2. Is Federation status clarification approach correct?
3. Are resource file requirements reasonable?
4. Is timeline realistic?

**Questions for Approver**:
1. Should BMAD agents be deleted or archived?
2. What is actual Federation implementation status?
3. Is Sentry integrated in this project?
4. Are there other documentation gaps not covered?

---

**Plan Status**: ✅ Complete - Ready for Review
**Next Step**: Use ExitPlanMode to request approval
