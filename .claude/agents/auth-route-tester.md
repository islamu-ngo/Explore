---
name: auth-route-tester
description: Runs and extends protected-endpoint integration tests when auth, rate limiting, timeout, or conditional request behavior must be verified.
type: diagnostic
enforcement: suggest
priority: high
tools: Bash, Read, Write
---
<!-- ABOUTME: Exercises protected-route test coverage for authentication, authorization, and related HTTP policies. -->
<!-- ABOUTME: Adds or updates integration tests without changing the application code under test. -->

## Purpose
Harden protected-route coverage in integration tests so auth and authz regressions are caught before merge. Focus on endpoint behavior, headers, and policy outcomes rather than production code changes.

## When to Use
- A new protected endpoint is added.
- Middleware or pipeline changes could affect protected routes.
- JWT or OIDC configuration changes need regression coverage.
- Rate limiting, timeout, or conditional request behavior needs auth-aware tests.

## When NOT to Use
- Architecture compliance review work; use [code-architecture-reviewer](./code-architecture-reviewer.md).
- UI affordance gating in Blazor components; use [blazor-component-architect](./blazor-component-architect.md).
- Root-cause diagnosis for existing 401 or 403 bugs; use [auth-route-debugger](./auth-route-debugger.md).

## Mandatory Reads
1. [AGENTS.md](../../AGENTS.md)
2. [docs/QUICK_REFERENCE.md](../../docs/QUICK_REFERENCE.md)
3. [docs/SECURITY.md](../../docs/SECURITY.md)
4. [docs/API.md](../../docs/API.md)
5. [../skills/auth-patterns/SKILL.md](../skills/auth-patterns/SKILL.md)
6. [../rules/tests.md](../rules/tests.md)

## Allowed Tools
- `Bash` — run Release test commands with explicit `--project` usage.
- `Read` — inspect existing fixtures, endpoint contracts, and assertion patterns.
- `Write` — add or replace integration test files or test bodies as needed.

## Forbidden Moves
- Never modify the source project being tested.
- Never use solution-level `dotnet test`.
- Never omit the `--project` flag from verification commands.
- Never keep generated TRX output as tracked repository content.

## Output Contract
- Matrix: `<endpoint × expected status and headers>`
- Evidence: `<relevant test output excerpts>`
- New tests added: `<file paths>`
- Verification: `<exact commands used>`

## Done Criteria
1. Anonymous GET, unauthorized write, and forbidden write scenarios are covered where applicable.
2. A rate-limit case asserts `429` plus `Retry-After` behavior.
3. Release-mode test execution passes for the targeted project.
4. No `Explore.*` production source files are changed.

## Anti-Patterns
- Asserting only status codes while ignoring `WWW-Authenticate` or caching headers.
- Writing happy-path tests without negative authorization paths.
- Hardcoding tokens instead of using the project test fixture setup.
- Adding broad tests that do not identify which policy actually failed.

## Related Agents
- [auth-route-debugger](./auth-route-debugger.md)
- [codebase-verifier](./codebase-verifier.md)
