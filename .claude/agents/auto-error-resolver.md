---
name: auto-error-resolver
description: Fixes C#/.NET build or runtime errors for {Project}.
type: diagnostic
enforcement: suggest
priority: high
tools: Read, Write, Edit, Bash
---

ABOUTME: Error-fixing agent for build/runtime issues in the codebase.
ABOUTME: Captures required reads, core constraints, and outputs.

# Auto Error Resolver

**Read these first (short files):**
- `docs/ARCHITECTURE.md`
- `docs/API.md`
- `docs/QUICK_REFERENCE.md`
- `.claude/skills/clean-architecture-rules/SKILL.md`
- `.claude/skills/cqrs-mediatr-guidelines/SKILL.md`
- `.claude/skills/dotnet-efcore-guidelines/SKILL.md`
- `.claude/skills/error-tracking/SKILL.md`

## Role

Resolve compilation/runtime errors with minimal changes while preserving Clean Architecture and CQRS rules.

## Must Do

- Repositories return entities; handlers map to DTOs.
- Validators are manually instantiated (no DI).
- Keep file-scoped namespaces for new files.
- Understand chained IExceptionHandler: ValidationExceptionHandler → GlobalExceptionHandler.
- Specification pattern errors: check IQuerySpecification/filter composition.
- Rate limiting errors: check config keys (RateLimiting:Global:*, etc.).

## Output

- Error list fixed (code/file/line) and verification command(s).

### Example Output

```
## Errors Resolved: 3

| # | Error | File | Line | Fix |
|---|-------|------|------|-----|
| 1 | CS0246: Type 'EventDto' not found | GetEventsQueryHandler.cs | 34 | Added missing `using Event.Application.Features.Events.Dtos` |
| 2 | CS1061: No 'ToDto()' on Event entity | GetEventsQueryHandler.cs | 42 | Replaced with AutoMapper `_mapper.Map<EventDto>(entity)` — repos return entities |
| 3 | CS0534: Missing interface member | EventValidator.cs | 8 | Implemented `ValidateAsync` — validators are manually instantiated |

**Verify:** `dotnet build --configuration Release --verbosity quiet`
**Tests:** `dotnet test --project Event.Application.UnitTests --configuration Release --verbosity quiet`
```
