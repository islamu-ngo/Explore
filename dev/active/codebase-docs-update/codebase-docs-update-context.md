# Codebase Documentation & Skills/Agents Update - Context

> **Key information for resuming work on comprehensive documentation and skills/agents updates**
>
> **Created**: 2026-02-10
> **Last Updated**: 2026-02-10
> **Status**: Planning Complete - Awaiting User Approval

---

## SESSION PROGRESS (2026-02-10)

### ✅ COMPLETED
- Launched 3 parallel Explore agents to analyze codebase
- Documentation analysis completed (18 files reviewed)
- Skills analysis completed (8 skills reviewed)
- Agents analysis completed (41 agents reviewed - 11 project-specific, 30+ BMAD)
- Codebase architecture verification completed
- Comprehensive implementation plan created

### 🟡 IN PROGRESS
- None (awaiting user approval to proceed)

### ⚠️ BLOCKERS
- ~~Awaiting user approval~~ ✅ **DECISIONS RECEIVED**:
  1. ✅ BMAD agents: **SKIP - Do not touch**
  2. ✅ Federation: **Only entities implemented, no HTTP endpoints, no PDS**
  3. ✅ Sentry: **NOT using Sentry - remove all references**
  4. ✅ Monitoring: **Using Prometheus (metrics) + Loki (logs)**
  5. ✅ Priority: **Execute all 4 phases**

---

## KEY FILES

### Planning Documentation
**dev/active/codebase-docs-update/codebase-docs-update-plan.md**
- Comprehensive 4-phase implementation plan
- 12-16 hour estimated effort
- Detailed task breakdown with acceptance criteria
- Risk assessment and dependencies
- File modification list

### Analysis Reports (from Explore agents)
- Agent abe66c1: Documentation analysis report
- Agent a516e25: Skills and agents analysis report
- Agent aca6352: Codebase architecture verification report

---

## CRITICAL FINDINGS

### 1. BMAD Agents Problem (CRITICAL)
**Issue**: 30+ generic BMAD (Build-Make-Assemble-Deploy) agents in `.claude/commands/` that:
- Reference non-existent `_bmad/` directory
- Fail when invoked
- Pollute namespace and make project agents hard to find
- Add no value to this .NET project

**Decision Needed**: Delete or archive?

---

### 2. Federation Documentation Contradiction (CRITICAL)
**Issue**: FEDERATION.md claims federation is "roadmap feature not implemented" BUT:
- Detailed architecture diagrams suggest implementation
- Entities (Actor, AtprotoRecord, etc.) exist in codebase
- CODEBASE_STRUCTURE.md lists these as real entities

**Decision Needed**: What is actually implemented? What timeline for remaining features?

---

### 3. Missing Documentation Patterns (HIGH PRIORITY)
**Found in Codebase But Not Documented**:
1. Delete commands return `IRequest<bool>` (26 commands) - NOT documented
2. HATEOAS/HAL+JSON response wrapping - Fully implemented, NOT documented
3. OutputCache attributes - Implemented, NOT documented
4. Enhanced soft delete (DeletedAt/DeletedBy) - Implemented, NOT documented
5. ServiceResult<T> pattern - Defined but unused, NOT documented

---

### 4. Resource Files Verification (HIGH PRIORITY)
**32 resource files need verification or creation** across 7 skills:
- auth-patterns: 1 file
- blazor-bff-patterns: 4 files
- blazor-ui-conventions: 7 files
- clean-architecture-rules: 4 files
- cqrs-mediatr-guidelines: 5 files
- dotnet-efcore-guidelines: 5 files
- error-tracking: 6 files

**Decision Needed**: Should we verify existing resources first or create all missing ones?

---

### 5. Sentry Integration Status (HIGH PRIORITY)
**Issue**: error-tracking skill extensively references Sentry but unclear if actually integrated.

**Needs Investigation**:
- Check `Explore.API/Program.cs` for `UseSentry()` call
- Check appsettings for Sentry DSN
- Verify if logging behaviors use Sentry client

**Decision Needed**: If not integrated, should we:
- Remove Sentry references from skill?
- Create task to integrate Sentry?
- Focus on built-in logging only?

---

## DOCUMENTATION QUALITY ASSESSMENT

### Overall Scores
- **Documentation Completeness**: 75% (3 files incomplete, federation unclear)
- **Documentation Accuracy**: 85% (some contradictions found)
- **Skills Alignment**: 100% (all 8 skills excellent)
- **Agent Health**: 26% (30+ broken BMAD agents vs 11 working project agents)

### Files by Status

**Excellent (No Changes Needed)**:
- ARCHITECTURE.md
- CODEBASE_INSIGHTS.md
- CODEBASE_STRUCTURE.md
- GOVERNANCE.md
- NAMING_CONVENTIONS.md
- QUICK_REFERENCE.md (minor additions needed)
- SECURITY.md (terminology fix only)
- TEMPLATE_GLOSSARY.md
- TROUBLESHOOTING.md

**Good (Minor Updates)**:
- API.md (add HATEOAS, OutputCache)
- BLAZOR.md (add ServiceResult<T>)
- CONFIGURATION.md (add instance settings, fix BYOK)
- CONTRIBUTING.md
- DOMAIN.md (add EventAspects, session hierarchy)
- PROJECT.md (complete TODOs)

**Needs Work**:
- FEDERATION.md (clarify status - CRITICAL)
- OPERATIONS.md (confusing, should be DEPLOYMENT_MODES)
- MULTI_TENANCY.md (verify referenced files exist)

**Missing**:
- TESTING.md (comprehensive test guide)
- SKILLS_AGENTS_REFERENCE.md (quick lookup matrix)

---

## SKILLS STATUS

### All Skills Aligned ✅
1. **auth-patterns** - Excellent, 1 resource to verify
2. **blazor-bff-patterns** - Excellent, 4 resources to verify
3. **blazor-ui-conventions** - Excellent, 7 resources to verify
4. **clean-architecture-rules** - Excellent, 4 resources to verify
5. **cqrs-mediatr-guidelines** - Excellent, 5 resources to verify
6. **dotnet-efcore-guidelines** - Excellent, 5 resources to verify + needs expansion
7. **error-tracking** - Good, Sentry status unclear, 6 resources to verify
8. **prd** - Excellent, no changes needed

**Expansion Needed**:
- dotnet-efcore-guidelines: Add complete auditing pattern (Rule #11) and named query filters (Rule #12)

---

## AGENTS STATUS

### Project-Specific Agents (11) ✅
All functional and well-aligned:
- code-architecture-reviewer ⭐ (CRITICAL - use heavily)
- code-refactor-master ⭐ (CRITICAL - use heavily)
- clean-code-architect
- blazor-component-architect
- documentation-architect
- frontend-error-fixer
- plan-reviewer
- refactor-planner
- web-research-specialist
- auth-route-tester
- auth-route-debugger
- auto-error-resolver

**Issue**: 9 missing YAML metadata (type, enforcement, priority)

### BMAD Agents (30+) 🔴
All broken and should be removed:
- bmm-* (main workflow agents)
- bmad-* (build/make agents)
- cis-* (creative innovation)
- tea-* (teaching agents)

**All reference non-existent `_bmad/` directory and fail when invoked.**

---

## CODEBASE VERIFICATION RESULTS

### Architecture Compliance: 95% ✅

| Pattern | Documented | Implemented | Match? |
|---------|------------|-------------|--------|
| Triple-interface pattern | ✅ | ✅ 100% | ✅ |
| File-scoped namespaces | ✅ | ✅ 100% | ✅ |
| CQRS structure | ✅ | ✅ 100% | ✅ |
| Manual validator instantiation | ✅ | ✅ 100% | ✅ |
| AllowAnonymous/Authorize | ✅ | ✅ 77/51 | ✅ |
| Named query filters | ✅ | ✅ 100% | ✅ |
| Delete → bool | ❌ | ✅ 26 cmds | ❌ |
| HATEOAS/HAL+JSON | ❌ | ✅ 100% | ❌ |
| ServiceResult<T> | ❌ | ⚠️ Unused | ⚠️ |
| OutputCache | ❌ | ✅ | ❌ |
| Enhanced soft delete | ❌ | ✅ | ❌ |

### Statistics
- **Total C# files**: 1,546
- **Files with ABOUTME**: 241 (15.6%)
- **Domain entities**: ~85
- **Commands with BaseCommandResponse**: 63
- **Commands with bool (delete)**: 26
- **Queries**: 118
- **API endpoints**: 149+
- **Controllers with metadata**: 100%

---

## IMPLEMENTATION PHASES SUMMARY

### Phase 1: CRITICAL FIXES (2 hours)
1. Remove all BMAD agents
2. Clarify Federation.md status
3. Fix Cerbos terminology inconsistency

### Phase 2: DOCUMENTATION UPDATES (4-5 hours)
1. Update QUICK_REFERENCE.md (add rule 13, enhance rule 11)
2. Document HATEOAS in API.md
3. Add EventAspects to DOMAIN.md
4. Update CONFIGURATION.md (instance settings, modules)
5. Document OutputCache in API.md
6. Update ABOUTME statistics

### Phase 3: SKILL & AGENT UPDATES (3-4 hours)
1. Verify and create 32 resource files
2. Clarify Sentry integration status
3. Add YAML metadata to 9 agents
4. Expand dotnet-efcore-guidelines skill

### Phase 4: DOCUMENTATION ENHANCEMENTS (3-4 hours)
1. Document ServiceResult<T> in BLAZOR.md
2. Create TESTING.md guide
3. Complete PROJECT.md sections
4. Create SKILLS_AGENTS_REFERENCE.md matrix
5. Verify missing referenced docs

**Total Estimated Time**: 12-16 hours

---

## DEPENDENCIES & BLOCKERS

### External Dependencies
1. Product owner approval for Federation status language
2. Tech lead decision on Sentry integration
3. Architecture team review of new documentation sections

### Internal Dependencies
- Phase 1 must complete before Phase 2
- Sentry investigation (Task 3.2) blocks resource file creation (Task 3.1)
- All doc updates must complete before skill updates

### Blockers
1. User decision on BMAD agent removal approach
2. User clarification on Federation implementation status
3. User confirmation on Sentry integration

---

## QUICK RESUME INSTRUCTIONS

**To continue this work:**

1. **Get user approval** on:
   - BMAD agent removal (delete vs archive)
   - Federation status (what's implemented?)
   - Sentry integration (yes/no?)
   - Timeline and priorities

2. **Start with Phase 1** (critical fixes):
   - Remove/archive BMAD agents
   - Fix FEDERATION.md
   - Align Cerbos terminology

3. **Work through phases sequentially** following the plan

4. **Update this context file** after each major milestone

---

## RELATED FILES

### Analysis Reports (Read-Only Reference)
- Documentation analysis: See agent abe66c1 output
- Skills/agents analysis: See agent a516e25 output
- Codebase verification: See agent aca6352 output

### Planning Files
- `codebase-docs-update-plan.md` - Detailed implementation plan
- `codebase-docs-update-tasks.md` - Task checklist (to be created)

### Documentation to Update (11 files)
- docs/FEDERATION.md (CRITICAL)
- docs/QUICK_REFERENCE.md (HIGH)
- docs/API.md (HIGH)
- docs/DOMAIN.md (HIGH)
- docs/CONFIGURATION.md (MEDIUM)
- docs/SECURITY.md (LOW)
- docs/BLAZOR.md (MEDIUM)
- docs/PROJECT.md (MEDIUM)
- docs/TESTING.md (NEW)
- docs/SKILLS_AGENTS_REFERENCE.md (NEW)

### Skills to Update (7 skills)
- All skills in `.claude/skills/` need resource file verification
- `dotnet-efcore-guidelines` needs expansion

### Agents to Update (9 agents)
- All need YAML metadata
- See list in plan

### Files to Delete (30+ BMAD agents)
- All `.claude/commands/bmad-*.md`
- All `.claude/commands/bmm-*.md`
- All `.claude/commands/cis-*.md`
- All `.claude/commands/tea-*.md`

---

## NOTES

- This is a comprehensive update affecting documentation, skills, and agents
- Most documentation is excellent quality (82/100)
- Skills are 100% aligned with project
- Main issue is 30+ broken BMAD agents polluting namespace
- Several undocumented patterns found in codebase need to be added to docs
- 32 resource files across skills need verification or creation
- No code changes required - documentation and configuration only
