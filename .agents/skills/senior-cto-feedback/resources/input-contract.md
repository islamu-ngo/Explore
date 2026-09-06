<!-- ABOUTME: Input contract for Senior CTO review of implementation-plan workstreams and missing-context handling. -->
<!-- ABOUTME: Defines what artifacts to read, what minimum evidence is required, and how to refine partial implementation-plan inputs safely. -->
# Input Contract

Use this file to determine whether enough material exists for a useful Senior CTO review and how to proceed when the input is partial. Refinements are applied directly into the active workstream triad without creating review files.

## Preferred Input Shape

Best input is an `implementation-plan` workstream:

- `dev/active/[task-name]/[task-name]-plan.md`
- `dev/active/[task-name]/[task-name]-context.md`
- `dev/active/[task-name]/[task-name]-tasks.md`
- `islamic-value-sensitive-design/i-vsd-[task-name].md`

Also useful:

- `.agents/skills/implementation-plan/SKILL.md` and its resources
- referenced repository docs, rules, skills, and source files;
- related `dev/active/` or `dev/pause/` workstreams;
- a concrete user goal such as “review this before implementation” or “rewrite this plan to be executable.”

Before an approval-capable review, require:

- exact reviewed plan and tasks revisions (Git objects or SHA-256 digests);
- exact reviewed I-VSD revision, `current` status, and `plan-aligned` disposition;
- resolved mappings from every material `IVSD-*` ID to scenario/task, explicit non-applicability, or escalation;
- current user-approval state, which the CTO review cannot change.

## Minimum Reviewable Inputs

You can still provide useful CTO feedback with partial inputs. Use this order:

1. `...-plan.md` only:
   - allowed, but call out missing context/tasks coverage.
2. `plan.md` + `tasks.md`:
   - enough to review sequencing and verification, but resume-state quality is still unknown.
3. `plan.md` + `context.md`:
   - enough to review strategy and continuity, but task executability is still under-specified.
4. `plan.md` + referenced repo files:
   - allowed when the user wants architecture critique more than workflow critique.

If the user provides only a vague idea and no implementation plan, this skill should recommend creating an `implementation-plan` workstream first instead of pretending there is a real plan to approve.

Partial inputs may receive useful feedback but never an `Approve` verdict. A missing/stale I-VSD report or unbound revision is `Changes required`.

## Required Reviewer Checks

For every review, determine:

1. What artifact set was provided?
2. Was the workstream clearly created from the `implementation-plan` skill, or does it diverge?
3. Which claims are verified by files you actually read?
4. Which claims remain assumptions?
5. What is missing that blocks a credible approval?
6. Do the plan/tasks and I-VSD revisions match the review metadata?
7. Did any proposed rewrite trigger I-VSD refresh under its integration contract?

## Missing-Context Handling

When inputs are incomplete:

- do a best-effort review;
- explicitly separate:
  - verified findings,
  - inferred findings,
  - missing evidence;
- avoid fake certainty;
- do not ask for clarification unless the missing input prevents any meaningful decision.

Recommended wording:

- “I can review the architecture direction, but I cannot approve implementation readiness because `...-tasks.md` is missing.”
- “This plan may be viable, but the current artifacts do not prove verification scope or operator impact.”

## Implementation-Plan Alignment Checks

When the plan claims to follow `implementation-plan`, verify these concrete things:

- `Last Updated: YYYY-MM-DD Europe/Brussels` exists in each file;
- `plan.md` includes current state, future state, constraints, decisions, phases, testing, docs/internal/ops/security/multi-tenancy/risk/success sections;
- `context.md` includes session progress, quick resume, key files, decisions, constraints, validation baseline, risks, and handoff notes;
- `tasks.md` includes implementation-maintenance rules, phase breakdown, one build plus at most one selected project test at each phase end, and remaining/deferred work;
- plan/context/tasks agree on current status and next step.
- plan/context/tasks agree on I-VSD path, reviewed-input revision, status/disposition, CTO-review state, and user approval.
- the report is current for the reviewed plan/tasks and every material `IVSD-*` mapping resolves.

If they do not align, treat that as a planning quality issue, not a formatting nit.

## When To Escalate Severity

Raise severity when missing input hides risk in these areas:

- tenant isolation,
- authorization,
- data migration,
- operator recovery,
- external dependency failure behavior,
- contract churn without regeneration/testing,
- background job idempotency or delivery guarantees.

Missing evidence in these categories is often `Blocker` or `Critical`, not `Moderate`.
