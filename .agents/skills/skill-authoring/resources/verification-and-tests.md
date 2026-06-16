<!-- ABOUTME: Verification matrix for skill-authoring and agent-context infrastructure changes. -->
<!-- ABOUTME: Lists schema, link, intent-manifest, diff, and build checks to run proportionally. -->

# Verification And Tests

## Minimum Commands

Run these for a new or materially changed skill:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Run a diff whitespace check for files touched:

```bash
git diff --check -- .claude/contract/intents.yaml .agents/skills Event.Architecture.Tests/AgentContextLinkTests.cs
```

When the full architecture project has unrelated failures, still prove the agent-context lane with focused TUnit filters:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/AgentContextSchemaTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/AgentContextIntentManifestTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/AgentContextLinkTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
```

## When To Run More

Run the full build when shared test infrastructure, project files, or application code changed.

## Manual Checks

- `SKILL.md` has required frontmatter and sections in order.
- Top 5 lists have exactly five numbered items each.
- Must-read links resolve.
- Resource index links every resource.
- Resource files start with two `ABOUTME` comments.
- No skip-list exception was added.
- No claim exceeds the available evidence.

## Expected Failures

Do not use VSTest-style `--filter` examples for this TUnit project. If a filtered command is required in the future, use a verified TUnit `--treenode-filter` expression and record the command that actually ran.
