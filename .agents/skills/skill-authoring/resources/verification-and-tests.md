<!-- ABOUTME: Verification matrix for skill-authoring and agent-context infrastructure changes. -->
<!-- ABOUTME: Lists schema, link, intent-manifest, diff, and build checks to run proportionally. -->

# Verification And Tests

## Minimum Commands

Run these for a new or materially changed skill:

```bash
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Run a diff whitespace check for files touched:

```bash
git diff --check -- .agents/contract/intents.yaml .agents/skills tests/Event.Architecture.Tests/AgentContextPolicyTests.cs
```

When the full architecture project has unrelated failures, still prove the context-policy lane with its focused TUnit filter:

```bash
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/AgentContextPolicyTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
```

## When To Run More

Run the full build when shared test infrastructure, project files, or application code changed.

## Manual Checks

- `SKILL.md` has valid required frontmatter.
- The description alone supports the pre-load decision and disambiguates adjacent skills.
- The loaded body contains no repeated activation section.
- Resource links resolve.
- Resource index links every resource.
- Resource files start with two `ABOUTME` comments.
- No skip-list exception was added.
- No claim exceeds the available evidence.

## Expected Failures

Do not use VSTest-style `--filter` examples for this TUnit project. If a filtered command is required in the future, use a verified TUnit `--treenode-filter` expression and record the command that actually ran.
