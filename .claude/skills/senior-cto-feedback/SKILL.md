---
name: senior-cto-feedback
description: Senior CTO review workflow for repository-grounded implementation plans, especially the `/dev-docs` plan/context/tasks workstreams used before coding.
type: workflow
enforcement: suggest
priority: high
---
<!-- ABOUTME: Senior CTO review skill for repository-grounded implementation plans and active dev-doc workstreams. -->
<!-- ABOUTME: Aligns plan critique and rewrites with the /dev-docs contract, ISLAMU Event guardrails, and self-hostable platform expectations. -->

## Purpose
Use this skill when the user wants blunt Senior CTO feedback on an implementation plan before coding starts. The primary target is the repository's `/dev-docs` workflow: `dev/active/[task-name]/[task-name]-plan.md`, `...-context.md`, and `...-tasks.md`. The goal is to decide whether the plan is executable, well-sequenced, safe for a self-hostable multi-tenant platform, and strong enough for another implementation agent to follow without rediscovering the problem.

## When to Load
- The user asks for CTO feedback, plan critique, approval/rejection, or plan rewrite.
- The input is a `dev/active/...` workstream created from `.claude/commands/dev-docs.md`.
- The user wants stronger architecture, sequencing, security, multi-tenancy, operations, or verification expectations in a plan.
- Breaking changes are acceptable, and the main question is whether the proposed direction is worth implementing.
- The user wants the existing `plan.md`, `context.md`, and `tasks.md` improved before implementation.

## When NOT to Load
- Not for direct production implementation unless the user separately asks to implement.
- Not for a vague product idea with no implementation plan; recommend creating a `/dev-docs` workstream first.
- Not for narrow syntax or framework-doc questions where official docs are the primary need.
- Not for a pure PRD or discovery artifact that is intentionally pre-implementation.
- Not for generic praise; this skill is for decisive critique and correction.

## Must-Read Docs
- [../../../AGENTS.md](../../../AGENTS.md)
- [../../../docs/QUICK_REFERENCE.md](../../../docs/QUICK_REFERENCE.md)
- [../../../docs/GOVERNANCE.md](../../../docs/GOVERNANCE.md)
- [../../../docs/OPERATIONS.md](../../../docs/OPERATIONS.md)
- [../../../dev/active/README.md](../../../dev/active/README.md)
- [../../../.claude/commands/dev-docs.md](../../../.claude/commands/dev-docs.md)
- [resources/input-contract.md](resources/input-contract.md)
- [resources/islamu-event-guardrails.md](resources/islamu-event-guardrails.md)
- [resources/review-rubric.md](resources/review-rubric.md)
- [resources/enterprise-self-hostable-checklist.md](resources/enterprise-self-hostable-checklist.md)
- [resources/severity-model.md](resources/severity-model.md)
- [resources/output-template.md](resources/output-template.md)
- [resources/plan-rewrite-guidance.md](resources/plan-rewrite-guidance.md)

## Top 5 Invariants
1. Review the entire `/dev-docs` workstream, not just the main plan; `plan.md`, `context.md`, and `tasks.md` must agree.
2. Distinguish verified codebase reality from plan aspiration. Do not approve claims you did not verify.
3. Favor simpler, more operable, better-tested, more explicit designs over compatibility with weak pre-v1 architecture.
4. Protect tenant isolation, authorization boundaries, and self-hosting/operator clarity before convenience or UI polish.
5. If the plan is directionally right but too large or mixed, require a sharper sequence or PR split instead of giving a soft approval.

## Top 5 Anti-Patterns
1. Reviewing only the narrative architecture while ignoring stale or vague `context.md` and `tasks.md`.
2. Treating missing migration, tenant-isolation, or operator-recovery detail as a minor documentation issue.
3. Accepting UI/BFF-local authorization or affordance logic instead of API/HAL-authoritative behavior.
4. Preserving duplicate contracts, compatibility shims, or obsolete routes “for now” without a named migration reason.
5. Producing generic best-practice feedback that does not name files, plan sections, risks, or required corrections.

## Minimal Examples
```text
Review flow:
1. Read plan/context/tasks
2. Compare against .claude/commands/dev-docs.md
3. Verify referenced files/docs/rules
4. Decide: approve, approve with required changes, split, reject, or defer
5. Return ranked risks, concrete required changes, and a recommended plan rewrite
```

```text
Typical CTO verdict:
The target architecture is reasonable, but I would not approve this as one workstream. Persistence changes, API contract churn, and Blazor/UI enablement need separate slices, and the current tasks file does not prove tenant-isolation verification or self-hoster recovery steps.
```

## Verification Hooks
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AgentContextSchemaTests`
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AgentContextLinkTests`
- `dotnet build --configuration Release --verbosity quiet`

## Related Skills
- [../cto-consultation/SKILL.md](../cto-consultation/SKILL.md)
- [../clean-architecture-rules/SKILL.md](../clean-architecture-rules/SKILL.md)
- [../cqrs-mediatr-guidelines/SKILL.md](../cqrs-mediatr-guidelines/SKILL.md)
- [../dotnet-efcore-guidelines/SKILL.md](../dotnet-efcore-guidelines/SKILL.md)
- [../auth-patterns/SKILL.md](../auth-patterns/SKILL.md)
- [../blazor-bff-patterns/SKILL.md](../blazor-bff-patterns/SKILL.md)
- [../blazor-ui-conventions/SKILL.md](../blazor-ui-conventions/SKILL.md)
- [../error-tracking/SKILL.md](../error-tracking/SKILL.md)
