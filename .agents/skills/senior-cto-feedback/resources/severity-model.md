<!-- ABOUTME: Severity model for ranking CTO feedback on implementation-plan workstreams. -->
<!-- ABOUTME: Distinguishes approval blockers from critical plan gaps, important changes, and optional polish. -->
# Severity Model

Use this severity model to rank feedback.

## Severity Labels

### Blocker

A blocker means the plan should not proceed.

Use for:

- tenant isolation risk,
- auth/security bypass,
- irreversible data loss without migration/runbook,
- broken self-hosting bootstrap,
- production deploy cannot be operated or recovered,
- major architecture boundary violation that will spread,
- missing contract strategy for large API/client change,
- background processing without idempotency when side effects matter,
- a `/dev-docs` workstream too incomplete to execute safely.

Required wording:

```text
Blocker — do not approve until fixed.
```

### Critical

A critical issue can proceed only if explicitly addressed in the plan before implementation starts.

Use for:

- insufficient tests for a high-risk change,
- migration strategy incomplete,
- observability missing for an operational feature,
- breaking change not documented,
- external dependency failure mode unclear,
- plan combines too many high-risk changes in one PR,
- `context.md` or `tasks.md` missing key resume or verification content.

Required wording:

```text
Critical — must be added to the implementation plan.
```

### Major

A major issue is important and should normally be fixed in the same workstream.

Use for:

- weak sequencing,
- naming/API ergonomics concerns,
- maintainability concerns,
- incomplete documentation,
- inefficient data access likely to matter later,
- duplicated logic that will drift,
- tasks that are too vague for another agent to execute safely.

Required wording:

```text
Major — should be corrected before merge or split into a follow-up with owner/date.
```

### Moderate

A moderate issue should be tracked but does not block.

Use for:

- polish,
- local developer experience,
- minor docs gaps,
- non-critical UI consistency,
- test cleanup,
- minor refactoring.

Required wording:

```text
Moderate — track as follow-up.
```

### Minor

A minor issue is optional polish.

Use for:

- naming improvements,
- small copy edits,
- formatting,
- small ergonomics improvements.

Required wording:

```text
Minor — optional improvement.
```

## Priority Order

When several issues exist, rank in this order:

1. Security and authorization.
2. Tenant isolation and data leakage.
3. Data loss and migration risk.
4. Self-hosting bootstrap and recovery.
5. API contract correctness.
6. Operational observability.
7. Test coverage.
8. Maintainability.
9. Plan-artifact resumability and execution clarity.
10. UI/UX polish.
11. Documentation polish.

## Risk Statement Format

Use this format:

```text
[Severity] — [Issue]
Why it matters:
Evidence:
Minimum acceptable fix:
```

Example:

```text
Blocker — tenant context can be missing in the write path.
Why it matters: a missing tenant context can create cross-tenant data or bypass isolation.
Evidence: the plan adds a background command handler but does not describe how tenant scope is set.
Minimum acceptable fix: add explicit tenant scope resolution, tests for missing/wrong tenant, and a fail-closed path.
```

## CTO Escalation Language

Use strong language when warranted:

- “I would not approve this.”
- “This is a platform integrity issue, not a polish issue.”
- “This must be split.”
- “This is the wrong layer for this responsibility.”
- “Because breaking changes are allowed, keeping this compatibility layer is unjustified.”
- “This is acceptable for a prototype, not for an enterprise self-hostable platform.”
