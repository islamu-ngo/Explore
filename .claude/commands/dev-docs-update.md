---
description: Update dev documentation before context compaction
argument-hint: Optional - specific context or tasks to focus on (leave empty for comprehensive update)
---

We're approaching context limits. Please update the development documentation to ensure seamless continuation after context reset.

## Required Updates

### 1. Update Active Task Documentation
For each task in `/dev/active/`:
- Update `[task-name]-context.md` with:
  - Current implementation state
  - Key decisions made this session
  - Files modified and why
  - Any blockers or issues discovered
  - Next immediate steps
  - Last Updated timestamp

- Update `[task-name]-tasks.md` with:
  - Mark completed tasks as ✅
  - Add any new tasks discovered
  - Update in-progress tasks with current status
  - Reorder priorities if needed

### 2. Capture Session Context
Include any relevant information about:
- Complex problems solved
- Architectural decisions made
- Tricky bugs found and fixed
- Integration points discovered
- Testing approaches used
- Performance optimizations made

### 3. Update Memory (if applicable)
- Store any new patterns or solutions in project memory/documentation
- Update entity relationships discovered
- Add observations about system behavior

### 4. Document Unfinished Work
- What was being worked on when context limit approached
- Exact state of any partially completed features
- Commands that need to be run on restart
- Any temporary workarounds that need permanent fixes

### 5. Create Handoff Notes
If switching to a new conversation:
- Exact file and line being edited
- The goal of current changes
- Any uncommitted changes that need attention
- Test commands to verify work

## Additional Context: $ARGUMENTS

**Priority**: Focus on capturing information that would be hard to rediscover or reconstruct from code alone.

## Journaling

Use your **journal** (`dev/_journal/journal.md`) to record important facts, insights, and preferences before you forget.

**For major decisions or requirements**, create dedicated documents:
- `dev/_journal/MAJOR_DECISIONS.md`
- `dev/_journal/journal.md` - General insights, patterns, failures

**Journal format** (`dev/_journal/journal.md`):

```md
## Failed Approaches
- [Date] Tried X approach for Y problem, but it failed because Z.

## Key Decisions
- [Date] Refactored for consistency and fixing logical flow in Bugman X manuscript.

## Deferred Fixes
- [Date] Fix inconsistent character names in Chapter 4.
```

- Each entry must be timestamped and formatted as above.
- Review your journal weekly.
- Search it before starting complex tasks.
- Document architectural decisions and user feedback trends.
- Record issues for later rather than fixing unrelated things mid-task.
- Before starting complex tasks:
  - Search the journal for relevant past experiences.
  - Document decisions and their outcomes.
  - Track recurring user feedback or collaboration patterns.
  - When you find something unrelated but worth fixing, log it instead of fixing it immediately.
  - Review the journal weekly to reinforce learning and memory.
