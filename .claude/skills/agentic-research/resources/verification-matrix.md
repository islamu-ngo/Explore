ABOUTME: Proportional verification matrix for agentic research and documentation updates.
ABOUTME: Matches verification depth to the type of change rather than applying one fixed workflow.

# Verification Matrix

## Change Type To Verification Mapping

| Change Type | Minimum Verification | Escalation Trigger |
|---|---|---|
| Docs-only Markdown | Structure review, link/path checks, reference existence, code/doc alignment spot-check | Touches API contracts or architecture rules |
| `.claude` Markdown (skills/agents) | Referenced file existence, consistency with repo conventions, formatting | Modifies enforcement rules or blocking constraints |
| `.claude` JSON (skill-rules, settings) | JSON syntax validation, trigger/reference review, no broken refs | Changes hook wiring or permission model |
| `.claude` hooks (C#) | Build hook project, verify hook runs without error | Modifies security checks or file tracking |
| Tooling or command docs | Confirm commands/settings exist in repo config or documented workflow | Changes CI/CD or deployment procedures |
| Executable behavior | `dotnet build` and affected `dotnet test --project ...` commands | Any shared contract, DI, or middleware change |

## Escalation Rules

1. Start with the minimum sufficient check for the change type.
2. Escalate when the change affects:
   - Shared contracts (interfaces, DTOs, entities)
   - Runtime behavior (middleware, DI, background services)
   - Security-sensitive flows (auth, tenant isolation, secrets)
   - Cross-layer dependencies (Domain → Application → Infrastructure)
3. Architecture test compliance is mandatory for any code change: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`

## Verification Commands Quick Reference

| Check | Command |
|-------|---------|
| Build | `dotnet build --configuration Release --verbosity quiet` |
| Architecture tests | `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` |
| Unit tests | `dotnet test --project <TestProject>.csproj --configuration Release --verbosity quiet` |
| JSON syntax | Open file in editor or use `jq . < file.json` |
| Hook build | `dotnet build` in `.claude/hooks/` directory |
| Link validation | Verify target files exist at referenced paths |
