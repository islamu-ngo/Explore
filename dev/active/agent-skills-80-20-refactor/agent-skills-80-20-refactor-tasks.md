# Agent & Skills 80/20 Refactor - Tasks

Last Updated: 2026-02-24

## Phase 0: Baseline & Config 🟡 IN PROGRESS
- [x] Record baseline line counts for: `CLAUDE.md`, all `.claude/skills/*/SKILL.md`, all `.claude/agents/*.md`
  - **Acceptance:** Baseline counts captured in context.md
- [x] Create minimal `opencode.json` with schema, model, instructions list, and permissions
  - **Acceptance:** File exists and uses only minimal instructions (`CLAUDE.md`, `docs/QUICK_REFERENCE.md`)

## Phase 1: Domain Skills ✅ COMPLETE
- [x] Compress `clean-architecture-rules/SKILL.md` to ≤20% size
  - **Acceptance:** WHEN/RULES/EXAMPLE structure + 1 reference file max

## Phase 2: Application Skills ✅ COMPLETE
- [x] Compress `cqrs-mediatr-guidelines/SKILL.md` to ≤20% size
  - **Acceptance:** Essential CQRS rules retained; details in 1 reference file
- [x] Compress `prd/SKILL.md` to ≤20% size
  - **Acceptance:** Minimal PRD workflow + link to reference details

## Phase 3: Infrastructure Skills ✅ COMPLETE
- [x] Compress `dotnet-efcore-guidelines/SKILL.md` to ≤20% size
  - **Acceptance:** Named filters, repository rules retained; 1 reference file
- [x] Compress `error-tracking/SKILL.md` to ≤20% size
  - **Acceptance:** Observability must-haves only; 1 reference file

## Phase 4: Presentation Skills ✅ COMPLETE
- [x] Compress `blazor-ui-conventions/SKILL.md` to ≤20% size
  - **Acceptance:** MudBlazor/BEM must-haves; 1 reference file
- [x] Compress `blazor-bff-patterns/SKILL.md` to ≤20% size
  - **Acceptance:** BFF/YARP/token-forwarding rules retained
- [x] Compress `blazor-css-isolation/SKILL.md` to ≤20% size
  - **Acceptance:** BEM + ::deep rules retained
- [x] Compress `auth-patterns/SKILL.md` to ≤20% size
  - **Acceptance:** Claim extraction + auth boundaries retained

## Phase 5: Agents ✅ COMPLETE
- [x] Create agent compression template (role + tools + 3–7 bullets)
  - **Acceptance:** Template documented in context.md
- [x] Compress all `.claude/agents/*.md` to ≤20% size
  - **Acceptance:** All agents follow template; cross-links to skills/docs only

## Phase 6: Validation & Metrics 🟡 IN PROGRESS
- [x] Run build and tests per CLAUDE.md guidance
  - **Acceptance:** All tests pass; warnings documented
- [ ] Measure post-refactor sizes and record in context.md
  - **Acceptance:** Each SKILL.md and agent file reduced by ~80%

## Quick Resume
1. Read `agent-skills-80-20-refactor-context.md`
2. Pick the next phase above and mark it in progress
3. Update context + checklist as you go

## Session Update (2026-02-24)
- ✅ Refactored CLAUDE.md to include explicit fetch‑before‑act rules and doc/skill index
- ✅ Ran build + all tests (warnings only; see context.md)
- 🟡 Next: phase 0 baseline counts, then skills/agents compression
