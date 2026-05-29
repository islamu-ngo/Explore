<!-- ABOUTME: Scoring and decision rubric for Senior CTO reviews of /dev-docs implementation-plan workstreams. -->
<!-- ABOUTME: Focuses on architecture integrity, self-hosting, security, sequencing, and whether future agents can safely implement from the plan. -->
# Senior CTO Review Rubric

Use this rubric to evaluate implementation plans for enterprise-grade self-hostable software.

Score only when useful. Prefer practical judgment over mechanical scoring.

The main target is a `/dev-docs` workstream, so include artifact quality in the review, not just architecture quality.

## 1. Strategic Fit

| Score | Meaning |
|---|---|
| 5 | Clearly advances product/platform strategy with limited accidental complexity |
| 3 | Useful but scope, sequencing, ownership, or operator value is unclear |
| 1 | Adds complexity without a strong platform reason |

Questions:

- Does this solve a real product/platform problem?
- Is this the right layer of the system for the capability?
- Does it reduce future complexity or create a new permanent burden?
- Is the plan aligned with self-hostable and enterprise expectations?
- Is the workstream narrow enough to execute without turning into an unreviewable mega-PR?

## 2. Architecture Integrity

| Score | Meaning |
|---|---|
| 5 | Clean boundaries, correct ownership, simple contracts |
| 3 | Mostly correct, but some orchestration or responsibility leakage |
| 1 | Mixed layers, fat components/controllers, hidden coupling |

Check:

- Domain owns domain invariants.
- Application/CQRS owns use-case orchestration.
- Persistence owns data access, not DTO mapping.
- API is transport and contract boundary, not business logic owner.
- Blazor/BFF does not become a security authority.
- UI uses server-provided affordances where applicable.
- Generated clients and OpenAPI stay stable and intentional.

## 3. Security and Trust Boundaries

| Score | Meaning |
|---|---|
| 5 | Trust boundaries are explicit, enforced server-side, and tested |
| 3 | Basic security exists but edge cases are under-specified |
| 1 | UI/client/config is trusted incorrectly or authz is vague |

Check:

- No privileged behavior depends on browser-controlled headers or client-only checks.
- Unsafe BFF endpoints use antiforgery where applicable.
- Authorization is enforced server-side.
- Machine/API key callers have scoped authority when relevant.
- Secrets never enter UI or logs.
- Fail-closed behavior is explicit for authz and policy calls.
- Admin/operator actions are separated from tenant/admin actions.

## 4. Multi-Tenancy and Isolation

| Score | Meaning |
|---|---|
| 5 | Tenant resolution, data filters, admin overrides, and cross-tenant operations are explicit |
| 3 | Tenant behavior is mentioned but not fully tested |
| 1 | Tenant isolation is assumed, not designed |

Check:

- Tenant context source is clear.
- Query filters or explicit tenant predicates are preserved.
- Any filter bypass is named, scoped, and tested.
- Instance-admin operations cannot casually leak tenant data.
- Single-tenant and multi-tenant mode behavior is defined.
- Tenant-scoped configuration has governance/lock behavior where needed.

## 5. Data Model and Migration Quality

| Score | Meaning |
|---|---|
| 5 | Data ownership, constraints, indexes, migrations, and rollback/reset paths are clear |
| 3 | Entity shape is plausible but migration/index/constraint story is incomplete |
| 1 | Data is modeled ad hoc or persistence impact is hand-waved |

Check:

- New tables have clear ownership and lifecycle.
- Indexes match expected query patterns.
- Constraints protect invariants.
- Seed IDs and lookup IDs are stable where relevant.
- Soft delete/audit/tenant markers are correct.
- Migration order and data migration are explicit.
- Breaking data changes have a reset, migration, or operator runbook.

## 6. API and Contract Quality

| Score | Meaning |
|---|---|
| 5 | Contracts are canonical, named, generated, tested, and documented |
| 3 | API works but naming/versioning/client regeneration is incomplete |
| 1 | Contracts are improvised or duplicate paths/semantics |

Check:

- Single canonical route shape.
- Stable route names and operation IDs.
- Clear request/response DTOs.
- HAL/HATEOAS behavior considered where relevant.
- RFC7807 ProblemDetails used for errors.
- OpenAPI export/client generation is sequenced.
- Breaking contract changes are explicit.

## 7. Self-Hosting and Operations

| Score | Meaning |
|---|---|
| 5 | Operators can deploy, configure, observe, recover, and upgrade |
| 3 | Basic config exists but docs/health/recovery are incomplete |
| 1 | Works only for local dev or SaaS assumptions |

Check:

- Environment variables and defaults are documented.
- Docker Compose/deployment impact is known.
- Health checks cover new dependencies.
- Logs/metrics/traces expose failure modes.
- Upgrade path is documented.
- Failure mode is safe and understandable.
- External dependencies are optional or clearly required.
- Single-server and constrained-resource scenarios are considered.

## 8. Testability and Verification

| Score | Meaning |
|---|---|
| 5 | Tests map to risks and run in the right lanes |
| 3 | Some tests exist but not enough for the risk profile |
| 1 | Testing is generic or missing critical integration cases |

Check:

- Unit tests cover domain/application logic.
- Integration tests cover persistence/API behavior.
- Architecture tests enforce conventions.
- BFF tests cover cookie/token/header behavior.
- API contract tests cover OpenAPI/HAL/ProblemDetails.
- E2E/manual lane is used only where needed.
- Tests are per-project, not solution-level.
- Obsolete compatibility tests are deleted when breaking changes are accepted.

## 9. Sequencing and Delivery Safety

| Score | Meaning |
|---|---|
| 5 | Work is split into reviewable, independently verifiable slices |
| 3 | Sequence is plausible but risks large PRs or late discovery |
| 1 | Big-bang plan with mixed concerns and no rollback |

Check:

- Migration and model changes happen before UI reliance.
- Contract stabilization happens before client/UI churn.
- Security/tenant tests land before feature expansion.
- Docs update with behavior changes.
- Each PR has clear exit criteria.
- Rollback/reset path exists for self-hosters.

## 10. Dev-Docs Quality

| Score | Meaning |
|---|---|
| 5 | `plan.md`, `context.md`, and `tasks.md` are consistent, current, and implementation-ready |
| 3 | Useful artifacts exist, but one file is stale, vague, or inconsistent |
| 1 | The workstream is not resumable by another agent without rediscovery |

Check:

- `plan.md` distinguishes verified evidence from assumptions.
- `context.md` has current progress, next step, blockers, and validation baseline.
- `tasks.md` maps cleanly to phases and verification.
- Status across all three files agrees.
- Another implementation agent could resume without re-asking the user for core context.

## CTO Decision Labels

Use one:

- **Approve** — plan is ready to implement.
- **Approve with required changes** — direction is right; named changes must be made first.
- **Split before approval** — scope is too large or mixed.
- **Reject** — wrong architecture or unacceptable risk.
- **Defer** — valuable, but not the right time or missing a foundational dependency.
