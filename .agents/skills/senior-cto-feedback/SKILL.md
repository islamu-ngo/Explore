---
name: senior-cto-feedback
description: "Load when asked for blunt Senior CTO critique, approval/rejection, risk review, sequencing correction, or rewrite of an existing `dev/active/<task>` implementation plan/context/tasks workstream before coding; not for open-ended CTO advice or direct implementation."
type: workflow
enforcement: suggest
priority: high
---
<!-- ABOUTME: Senior CTO review skill for repository-grounded implementation plans and active dev-doc workstreams. -->
<!-- ABOUTME: Aligns plan critique and rewrites with the implementation-plan skill, ISLAMU Event guardrails, and self-hostable platform expectations. -->

## Must-Read Docs
- [../../../AGENTS.md](../../../AGENTS.md)
- [../../../.agents/CONTEXT_ENGINEERING.md](../../../.agents/CONTEXT_ENGINEERING.md)
- [../implementation-plan/SKILL.md](../implementation-plan/SKILL.md)
- [../implementation-plan/resources/quality-gates.md](../implementation-plan/resources/quality-gates.md)
- [../i-vsd/SKILL.md](../i-vsd/SKILL.md)
- [../grill-me/SKILL.md](../grill-me/SKILL.md)
- [resources/input-contract.md](resources/input-contract.md)
- [resources/islamu-event-guardrails.md](resources/islamu-event-guardrails.md)
- [resources/review-rubric.md](resources/review-rubric.md)
- [resources/enterprise-self-hostable-checklist.md](resources/enterprise-self-hostable-checklist.md)
- [resources/severity-model.md](resources/severity-model.md)
- [resources/output-template.md](resources/output-template.md)
- [resources/plan-rewrite-guidance.md](resources/plan-rewrite-guidance.md)

## Top 5 Invariants
1. Verify I-VSD compliance across the entire workstream: `plan.md`, `context.md`, and `tasks.md` must agree and link a valid `islamic-value-sensitive-design/i-vsd-*.md` report that addresses provider-controlled moral risks; block approval when the deliverable or traceability is missing.
2. Distinguish verified codebase reality from plan aspiration. Do not approve claims you did not verify.
3. Apply the `grill-me` Socratic stress test to the plan's technical claims, including rollback safety, tenant boundaries, query-performance thresholds, operator clarity, failure modes, and edge cases; unresolved material answers block approval.
4. **Test-First Invariant Verification**: Verify that the plan sequences failing contract/invariant tests (Red Phase) *before* production code (Green Phase); block approval for plans with post-hoc test clustering or tautological test risks.
5. Require a sharper sequence or PR split for large or mixed plans; when vendor or pattern dogmatism hides a material fork, invoke `robin-neutral` to steel-man alternatives before deciding.

## Top 5 Anti-Patterns
1. Reviewing only the narrative architecture while ignoring stale or vague `context.md` and `tasks.md`.
2. **Approving Post-Hoc Test Tautology ("The Ugly Mirror")**, which allows agents to write tests after implementation or rely on shallow mock-heavy tests that mirror bugs instead of enforcing invariants.
3. Treating missing migration, tenant-isolation, or operator-recovery detail as a minor documentation issue.
4. Accepting UI/BFF-local authorization or affordance logic instead of API/HAL-authoritative behavior.
5. Producing generic best-practice feedback that does not name files, plan sections, risks, or required corrections.

## Minimal Examples
```text
Review flow:
1. Read plan/context/tasks
2. Compare against the implementation-plan skill and its quality gates
3. Verify referenced files/docs/rules
4. Decide: approve, approve with required changes, split, reject, or defer
5. Return ranked risks, concrete required changes, and a recommended plan rewrite
```

```text
Typical CTO verdict:
The target architecture is reasonable, but I would not approve this as one workstream. Persistence changes, API contract churn, and Blazor/UI enablement need separate slices, and the current tasks file does not prove tenant-isolation verification or self-hoster recovery steps.
```

## Verification Hooks
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Related Skills
- [../i-vsd/SKILL.md](../i-vsd/SKILL.md)
- [../grill-me/SKILL.md](../grill-me/SKILL.md)
- [../robin-neutral/SKILL.md](../robin-neutral/SKILL.md)
- [../cto-consultation/SKILL.md](../cto-consultation/SKILL.md)
- [../clean-architecture-rules/SKILL.md](../clean-architecture-rules/SKILL.md)
- [../cqrs-mediatr-guidelines/SKILL.md](../cqrs-mediatr-guidelines/SKILL.md)
- [../dotnet-efcore-guidelines/SKILL.md](../dotnet-efcore-guidelines/SKILL.md)
- [../auth-patterns/SKILL.md](../auth-patterns/SKILL.md)
- [../blazor-bff-patterns/SKILL.md](../blazor-bff-patterns/SKILL.md)
- [../blazor-ui-conventions/SKILL.md](../blazor-ui-conventions/SKILL.md)
- [../error-tracking/SKILL.md](../error-tracking/SKILL.md)
