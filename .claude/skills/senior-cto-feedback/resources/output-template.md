<!-- ABOUTME: Response structure for Senior CTO reviews of implementation-plan workstreams. -->
<!-- ABOUTME: Forces decisive verdicts, artifact-specific critique, and actionable rewrite guidance for /dev-docs plans. -->
# Output Template

Use this structure for Senior CTO feedback.

## Required Structure

```markdown
# Senior CTO Feedback

## Executive Verdict

[One direct paragraph. State whether the plan is strong, weak, risky, over-scoped, under-evidenced, or ready.]

**Decision:** Approve | Approve with required changes | Split before approval | Reject | Defer

## Top Risks

### 1. [Severity] — [Issue]

**Why it matters:**  
[Enterprise/platform/operator reason.]

**Evidence from the plan/codebase:**  
[Concrete evidence. Name plan sections and files when available.]

**Minimum acceptable fix:**  
[Specific correction.]

## What I Would Keep

[Short list of the strongest parts of the plan.]

## What Must Change Before Implementation

[Ranked list. Keep this sharper than “nice to have” improvements.]

## Dev-Docs Quality Assessment

### `...-plan.md`
[Does it satisfy the /dev-docs planning contract? What is missing or strong?]

### `...-context.md`
[Does it preserve resume-critical context, decisions, blockers, and validation baseline?]

### `...-tasks.md`
[Are tasks executable, sequenced, and verifiable?]

## Enterprise / Self-Hosting Assessment

[Discuss config, secrets, deployment, health checks, operations, upgrades, backup/restore/recovery, and operator clarity.]

## Security and Multi-Tenancy Assessment

[Discuss auth/authz, tenant isolation, trust boundaries, BFF/browser boundary, admin scope, and fail-closed behavior.]

## Architecture and Maintainability Assessment

[Discuss layering, CQRS, data ownership, API contracts, UI responsibility, abstractions, and deletion of obsolete paths.]

## Breaking-Change Position

[State which breaking changes are acceptable, which compatibility paths should be deleted, and what migration/docs are required.]

## Implementation Sequencing I Recommend

1. [Foundation / migration / contract]
2. [Application/API behavior]
3. [UI/BFF enablement]
4. [Tests/observability/docs]
5. [Cleanup/removal]

## Verification Bar

[Commands, projects, and specific risk-oriented tests expected before merge.]

## Recommended Plan Rewrite

[Provide a concise improved version of the plan or the precise changes that must be made to the existing plan/context/tasks files.]

Optional sections

## Missing Evidence

[What the current workstream does not prove.]

## Red-Team Failure Scenarios

[How this could fail in production/self-hosted deployments.]

## Alternative Architecture

[Better design if current plan is materially wrong.]

## PR Split Recommendation

[Proposed PR boundaries and why.]

## Operator Runbook Requirements

[What self-hosters need for install/upgrade/recovery.]
```

## Answer Style Rules

- Start with the verdict, not background.
- Avoid generic praise.
- Do not bury blockers at the end.
- Use file paths and plan sections when available.
- Be explicit about what would block approval.
- Prefer fewer, sharper recommendations over long unfocused lists.
- When the user asks to improve the plan, make the rewrite guidance directly actionable for the existing `plan.md`, `context.md`, and `tasks.md`.
- Do not write code unless explicitly requested.
- Do not ask for clarification when a useful review can be produced with assumptions.

## Example Verdicts

### Strong but over-scoped

```markdown
## Executive Verdict

The direction is right, but I would not approve this as one implementation stream. It mixes persistence changes, API contract changes, UI behavior, and operator documentation in a way that makes regressions hard to isolate. Because breaking changes are acceptable, the plan should simplify the contract first, then rebuild the UI around the new canonical shape.

**Decision:** Split before approval.
```

### Wrong layer

```markdown
## Executive Verdict

I would reject the current plan because it places policy decisions in the Blazor/UI layer instead of the API/Application boundary. For an enterprise self-hostable platform, the UI can express affordances, but it cannot be the source of authorization truth.

**Decision:** Reject.
```

### Ready with changes

```markdown
## Executive Verdict

This is a strong plan and the target architecture is credible. I would approve it after adding explicit tenant-isolation tests, a self-hoster upgrade note, and OpenAPI/client regeneration sequencing.

**Decision:** Approve with required changes.
```
