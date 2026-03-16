ABOUTME: Proportional verification matrix for agentic research and documentation updates.
ABOUTME: Matches verification depth to the type of change rather than applying one fixed workflow.

# Verification Matrix

| Change Type | Minimum Verification |
|---|---|
| Docs-only Markdown | Structure review, link/path checks, reference existence, code/doc alignment spot-check |
| `.claude` Markdown | Referenced file existence, consistency with repo conventions, formatting sanity check |
| `.claude` JSON | JSON syntax validation and trigger/reference review |
| Tooling or command docs | Confirm commands/settings exist in repo config or documented workflow |
| Executable behavior | `dotnet build` and affected `dotnet test --project ...` commands |

## Escalation Rule
- Start with the minimum sufficient check.
- Escalate when the change affects shared contracts, repo tooling, runtime behavior, or security-sensitive flows.
