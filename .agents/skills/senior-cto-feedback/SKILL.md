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
- [../i-vsd/resources/integration-contract.md](../i-vsd/resources/integration-contract.md)
- [../grill-me/SKILL.md](../grill-me/SKILL.md)
- [resources/input-contract.md](resources/input-contract.md)
- [resources/islamu-event-guardrails.md](resources/islamu-event-guardrails.md)
- [resources/review-rubric.md](resources/review-rubric.md)
- [resources/enterprise-self-hostable-checklist.md](resources/enterprise-self-hostable-checklist.md)
- [resources/severity-model.md](resources/severity-model.md)
- [resources/output-template.md](resources/output-template.md)
- [resources/plan-rewrite-guidance.md](resources/plan-rewrite-guidance.md)

## Top Invariants
1. Follow I-VSD `plan-review` mode: bind the verdict to exact plan/tasks and I-VSD revisions, require current `IVSD-*` mappings, and block technical approval when the report is missing, stale, or unresolved.
2. Default to read-only review plus `dev/active/<task>/<task>-cto-review.md`. An explicitly requested rewrite that changes a refresh trigger marks I-VSD stale; the reviewer cannot approve its rewritten revision in the same pass. CTO readiness never grants user or scholarly/legal approval.
3. Distinguish verified codebase reality from plan aspiration. Do not approve claims you did not verify.
4. Apply the `grill-me` Socratic stress test to the plan's technical claims, including rollback safety, tenant boundaries, query-performance thresholds, operator clarity, failure modes, and **"The Worst Break" Adversarial Scenario** (the single most catastrophic failure mode); unresolved material answers block approval.
5. **3-Dimensional Evaluation Model**: Evaluate the plan across three distinct dimensions:
   - **Completeness**: Are all declared capabilities, I-VSD mitigations, and requirements present?
   - **Correctness**: Do invariant test scenarios cover boundary conditions, concurrency races, and negative failure paths?
   - **Coherence**: Does the design adhere to Clean Architecture, HAL link affordances, tenant isolation, and transactional outbox patterns?
6. **Invariant-First & Quality-Over-Quantity Verification**: Verify that the plan specifies failing invariant tests (Red Phase) bound to named Scenarios *before* production code for **Core Domain Invariants, Concurrency Races, and Security Boundaries**. Block plans that introduce tautological mock-mirroring tests (`NSubstitute.Received(1)` on internal repositories/caches), framework-testing boilerplate (EF Core cancellation), or raw source-code / CSS text scraping.
7. **Greenfield Breaking Change Posture**: ISLAMU Event is pre-v1 with 0 external adopters. The CTO rejects backward-compatibility shims, deprecated route aliases, and adapter baggage. Approve clean breaking changes and structural simplifications over legacy preservation.
8. **4-Point "Right-Sizing" Rule**: Mandate a PR split ("Split before approval") when 2+ symptoms match: (1) Scope contains multi-intent "and also" clauses, (2) Plan exceeds reviewable task capacity (< 8-10 major tasks), (3) Migration, API contract churn, and UI enablement combined in one big-bang phase, (4) Backend CQRS slice could ship independently of Blazor UI.
9. Require a sharper sequence or PR split for large or mixed plans; when vendor or pattern dogmatism hides a material fork, invoke `robin-neutral` to steel-man alternatives before deciding.

## Top Anti-Patterns
1. Reviewing only the narrative architecture while ignoring stale or vague `context.md` and `tasks.md`, or allowing `plan.md` to be polluted with granular task checklists (`- [ ]`) and session handoffs.
2. **Approving Oversized "And Also" Workstreams**, which allow large multi-layered changes to proceed as single monolithic plans instead of enforcing reviewable PR boundaries.
3. **Approving Backward-Compatibility Shims & Legacy Baggage**, which introduces deprecated endpoint aliases or adapter layers in a greenfield project with zero external users.
4. **Approving Mock-Mirroring Test Bloat ("The Ugly Mirror")**, which allows agents to write tests that mock internal dependencies and assert method calls instead of enforcing domain invariants and contract behavior.
5. **Ignoring Missing Scenarios and Worst-Break Failure Modes**, which allows happy-path-only plans to pass review without negative boundary or concurrency tests.
6. Treating missing migration, tenant-isolation, or operator-recovery detail as a minor documentation issue.
7. Accepting UI/BFF-local authorization or affordance logic instead of API/HAL-authoritative behavior.
8. Producing generic best-practice feedback that does not name files, plan sections, risks, or required corrections.

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
