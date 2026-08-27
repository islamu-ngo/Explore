<!-- ABOUTME: Active working context for the EF Core-first persistence hardening workstream. -->
<!-- ABOUTME: Records review revisions, planning outcome, resume state, constraints, baselines, risks, and handoff. -->

# EF Core-First Persistence Hardening — Context

Last Updated: 2026-08-27 Europe/Brussels

## Review State

- **Workstream state:** planning complete; implementation not started
- **User approval:** pending
- **Plan revision:** SHA-256
  `7493c0035e1bf7c4ea5309eeb822cb20e0b422e3da16ca39688395e093474c44`
- **Tasks revision:** SHA-256
  `b375cfbdf4dd4d61757482900cc0721513dd5d15d014cf9b9a426eb96acadc55`
- **Evidence revision:** SHA-256
  `f5790b0a6a91a6d2de419598023b08af27f414883f0e049cee2f8bf974311a72`
- **I-VSD revision:** SHA-256
  `e3d218475092632df01befcb86943d9d9ff0552eb5047908026998336fb490c0`
- **I-VSD status / disposition:** current / plan-aligned
- **CTO review:** not requested
- **Product implementation:** blocked until explicit user approval

## SESSION PROGRESS (2026-08-27 Europe/Brussels)

### Planning Outcome

- Classified the work under `update-repository-query` with
  `add-ef-migration` and Tier 0 payment/inventory/admission plus Tier 2
  privacy/tenant overlays.
- Revalidated the persistence audit baseline:
  - 51 EF raw-SQL sites across 26 files;
  - 24 direct ADO markers across nine files;
  - 228 literal table mappings;
  - 79 literal column mappings;
  - 428 literal index names;
  - four provider/EF `.Internal` imports.
- Confirmed the solution uses one ORM, EF Core, with five provider modes.
- Confirmed configurable schemas and schema-less provider prefixes are public
  operator contracts and remain supported.
- Confirmed no overlapping active persistence-hardening workstream exists.
- Bound the completed Registration Data Collection workstream as critical-domain
  authority.
- Bound the plan-blocked Event Ticketing Lifecycle workstream as a downstream
  consumer of these guardrails.
- Researched official EF Core, Npgsql, PostgreSQL, naming-convention, and
  Microting behavior.
- Wrote the evidence packet, implementation plan, tasks ledger, context, and
  plan-aligned planning-mode I-VSD report.
- Made no runtime, migration, test, configuration, or existing workstream edit.

### Planning Decisions

- Retain PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL.
- Retain configurable schemas for PostgreSQL/SQL Server and `ie_` prefixes for
  schema-less providers.
- Retain `EFCore.NamingConventions` as the physical naming authority.
- Enforce EF Core/LINQ first, provider public APIs second, and isolated
  parameterized SQL last.
- Remove ordinary SQL and provider branching from repositories.
- Preserve critical concurrency/transaction/outbox/erasure behavior through
  Red-before-Green invariant tests.
- Keep generated migration ownership and do not hand-edit tracked migration
  history.
- Permit disposable development database recreation only with operator
  approval.
- Isolate unavoidable provider-internal migration constructor coupling and
  package-version test it.
- Add no new dependency.

## Quick Resume

1. Read this context file.
2. Read the first unchecked task in
   [`efcore-first-persistence-hardening-tasks.md`](efcore-first-persistence-hardening-tasks.md).
3. If user approval is present, start Task 0.1 and record the approval revision.
4. Retrieve only the plan section named by the active task.
5. Before product edits, refresh knowledge-graph impact evidence and establish
   the clean baseline once.
6. Do not start with global search-and-replace, migration generation, or SQL
   deletion.

## Key Files And Responsibilities

| File or area | Responsibility |
| --- | --- |
| `efcore-first-persistence-hardening-plan.md` | Behavior contract, architecture, phases, risk, release, and rollback |
| `efcore-first-persistence-hardening-tasks.md` | Sole granular execution/status ledger |
| `efcore-first-persistence-hardening-evidence.md` | Shared repository and external research evidence |
| `islamic-value-sensitive-design/i-vsd-efcore-first-persistence-hardening.md` | Provider-responsibility findings and plan alignment |
| `src/Explore.Persistence/Repositories/` | Domain-facing repository implementations to normalize |
| `src/Explore.Persistence/Configurations/` | Convention and semantic model mappings |
| `src/Explore.Persistence/Database/` | Provider composition, locks, row fences, and database primitives |
| `src/Explore.Persistence/Schema/` | Model namespace, provider constraints, migration extensions, and identifier policy |
| `src/Explore.Persistence/Privacy/ErasureAuthority/` | Retained authority models, functions, and storage boundaries |
| `tests/Event.Architecture.Tests/` | Capability ladder, naming, filters, migration, and Clean Architecture gates |
| `tests/Event.Persistence.IntegrationTests/` | Real model, repository, provider, migration, and concurrency evidence |

## Key Decisions

### Capability Ladder

Native EF Core and LINQ are mandatory first. Public provider APIs/translations
are second. SQL is allowed only through approved provider primitives after a
capability gap is proven.

### Naming

Snake-case conventions own ordinary physical names. Redundant `ToTable`,
`HasColumnName`, `HasDatabaseName`, and constraint strings are removed.
Semantic owned-value flattening and provider constraints remain explicit only
when conventions cannot express them.

### Concurrency

Use conditional set-based mutation, concurrency tokens, unique constraints,
transactions, and appropriate isolation before pessimistic SQL. Retain
provider lock or `SKIP LOCKED` behavior only with race and performance evidence.

### Migrations

Fix model/configuration first, generate every affected provider artifact, and
never patch generated output. Existing merged history remains intact.
Development databases are disposable only through an explicit operator action.

### Critical Invariants

Payment/refund state remains monotonic; inventory cannot oversell; admission
authority cannot duplicate; outboxes remain transactionally paired; tenant
filters fail closed; erasure remains authority-first and anti-resurrection
fenced; telemetry remains zero-PII.

## Constraints And Rules To Remember

- Repositories return entities and never expose `IQueryable`.
- Complex reads use the established specification pattern.
- Context pooling keeps scoped tenant/current-user dependencies property-injected.
- Named tenant and soft-delete filters remain active by default.
- Cross-tenant operations use exact named bypass reasons and exact tenant
  predicates.
- Migration and snapshots are generated artifacts.
- No new ORM, micro-ORM, provider extension, compatibility shim, or hand-written
  migration.
- No secret, connection string, PII, payment payload, erasure subject, or SQL
  parameter value in logs or evidence.
- Fixed sleeps and timing-luck polling are forbidden in concurrency tests.
- Architecture gates must be capable of failing on a synthetic regression.
- Real-engine evidence is required for provider claims.
- Final Tier 0–2 review requires mutation evidence above 85% for owned critical
  persistence logic and anonymized MAD review.

## Validation Baseline

### Planning Validation

- Repository evidence and official documentation inspected.
- Plan paths, test projects, provider migration projects, documentation targets,
  and package versions verified.
- Runtime build/tests intentionally not run because this turn changed planning
  markdown only.
- Triad separation, all ten scenario mappings, all five I-VSD finding mappings,
  exact revision bindings, relative links, and diff whitespace are green.

### Implementation Baseline

Not established. Task 0.3 owns the first and only unchanged code baseline.

## Current Known Risks / Unknowns

### Risks

- SQL paths combining lock, mutation, and returned identifiers may need a
  retained provider primitive after native concurrency design is measured.
- Naming cleanup may generate destructive provider migration operations.
- Microting translation and migration behavior may differ materially from
  Npgsql/SQL Server.
- Npgsql's public migration generator constructor currently depends on an
  internal provider options contract.
- A temporary raw-SQL registry could become permanent unless every entry is
  phase-owned and removed.

### Unknowns

No unknown changes scope, architecture, phase ordering, or verification.
Performance envelopes and the final approved SQL exception set are
implementation evidence, not deferred planning decisions.

## Related Workstreams

- `dev/active/registration-data-collection/`: complete behavioral authority;
  do not edit for this workstream.
- `dev/active/event-ticketing-lifecycle/`: plan-blocked downstream consumer;
  do not begin its persistence implementation before the guardrail phase is
  available.

## Handoff Notes

**Current workstream:** planning complete, awaiting user review.

**Next owner:** user review; then implementation agent if approved.

**Start at:** Task 0.1 in the tasks ledger.

**Do not start with:** migration generation, repository-wide replacements,
deleting SQL, package changes, or developer database recreation.

**First implementation evidence:** approval revision, refreshed impact graph,
clean baseline, and failing provider/invariant tests.

**Planning docs changed:** yes; new task-owned evidence, plan, tasks, context,
and I-VSD report only.
