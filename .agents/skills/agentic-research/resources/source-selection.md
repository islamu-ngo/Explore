ABOUTME: Source selection guidance for agentic research in this repository.
ABOUTME: Explains when to stay local, when to use official docs, and when external research is justified.

# Source Selection

## Evidence Order
1. Local code, tests, config, and project files.
2. Local docs and `.agents` files.
3. Official framework or library docs (Context7, NuGet, Microsoft Learn).
4. External research tools (Tavily, Exa, web search).

## Stay Local When

The answer exists in this repository. Examples:

| Question Type | Where To Look |
|---------------|---------------|
| Controller/handler behavior | `Event.API/Controllers/`, `Event.Application/Features/` |
| DI registration | `**/DependencyInjection.cs`, `Program.cs` |
| Middleware pipeline order | `Event.API/Program.cs` |
| Blazor component behavior | `Explore.Blazor.Client/` |
| Domain entity rules | `Event.Domain/Entities/`, `docs/internal/DOMAIN.md` |
| Repo conventions | `AGENTS.md`, `docs/internal/GOVERNANCE.md`, `docs/internal/QUICK_REFERENCE.md` |
| Test expectations | `Event.Architecture.Tests/`, `docs/internal/TESTING.md` |
| Config/settings | `docs/internal/CONFIGURATION.md`, `appsettings*.json` |

## Escalate To Official Docs When

- Package, framework, runtime, or SDK behavior is unclear.
- You need authoritative migration or breaking-change guidance (e.g., MudBlazor v9, .NET 10).
- Security-sensitive defaults or middleware behavior must be confirmed.
- API surface of a third-party library is in question.

**Preferred tools**: Context7 (library docs), NuGet package pages, Microsoft Learn.

## Escalate To External Research When

- You need standards, RFCs, advisories, ecosystem comparisons, or broader implementation landscape.
- Official docs are insufficient or do not cover the comparison question.
- Looking for production-tested patterns from the OSS ecosystem.

**Preferred tools**: Tavily search/research, Exa web search, grep.app for GitHub code examples.

## Stop Conditions
- The repo already answers the question — do not escalate.
- Official docs confirm the behavior you need — do not escalate further.
- Additional sources are repeating the same conclusion without adding value.
- Two external sources agree on the answer — stop.
