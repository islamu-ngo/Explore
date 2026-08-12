# Agent Architecture Modernization Tasks

- [ ] **Phase 1: Subagent Schema & Governance**
  - [ ] Refactor `.agents/agents/_AGENT_SCHEMA.md` (paths, enum fixes, 5-agent active portfolio list)
  - [ ] Refactor `.agents/agents/README.md` (5-agent role selection guide, usage rules)
- [ ] **Phase 2: Subagent Refactoring & Modernization**
  - [ ] Refactor `.agents/agents/architect-agent.md`
  - [ ] Refactor `.agents/agents/backend-engineer-agent.md`
  - [ ] Refactor `.agents/agents/presentation-engineer-agent.md`
  - [ ] Refactor `.agents/agents/quality-verifier-agent.md`
  - [ ] Refactor `.agents/agents/librarian-agent.md`
- [ ] **Phase 3: Repository Cross-Reference Alignment**
  - [ ] Update `AGENTS.md` (Section 9 path to `.agents/agents/README.md`)
  - [ ] Update `docs/index.md` (Line 79 reference to `.agents/agents/_AGENT_SCHEMA.md`)
  - [ ] Update `.agents/skills/_SKILL_SCHEMA.md`
  - [ ] Update `.agents/contract/schema.json`
- [ ] **Phase 4: Verification & Build Validation**
  - [ ] Audit line counts (50-120 target, ≤160 max) and 10 required sections
  - [ ] Verify all markdown links resolve
  - [ ] Run `dotnet build --configuration Release --verbosity quiet`
