<!-- ABOUTME: Planning-mode I-VSD review for the EF Core-first persistence hardening workstream. -->
<!-- ABOUTME: Maps database portability, tenant safety, critical state, and provider escape hatches to provider responsibility. -->

# I-VSD EF Core-First Persistence Hardening Planning Review

Last Updated: 2026-08-27

## Review Metadata

- Mode: planning
- Subject: EF Core-first multi-provider persistence hardening
- Workstream: `efcore-first-persistence-hardening`
- Report kind: implementation-planning review
- Report status: current
- Disposition: plan-aligned
- Evidence cutoff: 2026-08-27
- Evidence packet:
  [`efcore-first-persistence-hardening-evidence.md`](../dev/active/efcore-first-persistence-hardening/efcore-first-persistence-hardening-evidence.md)
- Evidence-packet revision:
  SHA-256 `f5790b0a6a91a6d2de419598023b08af27f414883f0e049cee2f8bf974311a72`
- Reviewed plan revision:
  SHA-256 `7493c0035e1bf7c4ea5309eeb822cb20e0b422e3da16ca39688395e093474c44`
- Reviewed tasks revision:
  SHA-256 `b375cfbdf4dd4d61757482900cc0721513dd5d15d014cf9b9a426eb96acadc55`
- Review owner: planning agent

## Scope

This review covers provider responsibility created by persistence implementation
choices across PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL. It focuses on:

- reliable behavior across self-hosted provider choices;
- tenant and soft-delete isolation during query and mutation refactors;
- payment, registration inventory, admission, notification, webhook, and
  privacy-erasure state correctness;
- inspectable limits on raw SQL, direct ADO access, and provider internals;
- migration safety and operator-understandable recovery;
- truthful claims about portability and provider parity.

The review does not issue a fiqh ruling, certify database security, prove
reliability, approve implementation, or authorize destructive database actions.

## Claim Boundary

The planned architecture can support Amanah, Justice, Privacy, Promise-Keeping,
and self-hosting stewardship through inspectable persistence boundaries and
real-engine evidence. It cannot prove those outcomes until the named
specification tests, provider-engine tests, migration evidence, mutation score,
and adversarial review are complete.

No claim is made that equal EF APIs produce identical SQL or performance across
providers. Provider parity means equivalent domain outcomes and preserved
invariants, not identical execution plans.

## Findings

### IVSD-PERSIST-001 — Provider Choice Must Not Change Domain Correctness

- Status: open
- Severity: high
- Principles/domains: Justice (`Adl`), Trust (`Amanah`), avoiding harm,
  governance and sustainability
- Provider-controlled decision: whether physical naming and SQL escape hatches
  honor every supported schema/prefix model
- Evidence: raw provider-agnostic statements currently embed physical table
  names that do not match all supported mappings
- Affected stakeholders: self-hosting operators, event organizers, attendees,
  contributors, support operators
- Required mitigation: make EF metadata and naming conventions authoritative,
  remove ordinary SQL, and prove equivalent behavior on each real engine
- Planned mapping: Scenarios `PERSIST-S1`, `PERSIST-S2`, and `PERSIST-S9`;
  Tasks 2.1–2.13, 3.1–3.18, and 7.1–7.7
- Refresh trigger: any provider removal, addition, naming-policy change, or
  unsupported provider-specific behavior

### IVSD-PERSIST-002 — Tenant And Erasure Boundaries Must Fail Closed

- Status: open
- Severity: critical
- Principles/domains: Privacy (`Hurmah al-Khususiyyah`), Trust (`Amanah`),
  avoiding spying (`Tajassus`), avoiding harm
- Provider-controlled decision: whether refactored queries preserve named
  tenant and soft-delete filters and whether erasure remains authority-first
- Evidence: the workstream touches exact-tenant repository paths, cross-tenant
  workers with explicit bypass reasons, and retained erasure authority storage
- Affected stakeholders: users, erased subjects, tenant administrators,
  operators, privacy reviewers
- Required mitigation: test filter preservation and bypass reasons before
  implementation; keep authority append/commit before local purge; validate
  zero-PII telemetry on every provider path
- Planned mapping: Scenarios `PERSIST-S3`, `PERSIST-S5`, and `PERSIST-S10`;
  Tasks 0.6, 0.8, 3.15–3.16, 5.6–5.7, and 7.10
- Refresh trigger: tenant-filter, erasure-ordering, retained-authority, RLS, or
  telemetry changes

### IVSD-PERSIST-003 — Financial And Admission State Must Remain Monotonic

- Status: open
- Severity: critical
- Principles/domains: Justice (`Adl`), Trust (`Amanah`), avoiding harm,
  economic and commercial ethics
- Provider-controlled decision: concurrency, claim, retry, idempotency, and
  fencing semantics used for inventory, payment, refund, admission, outbox, and
  webhook flows
- Evidence: the affected persistence layer contains provider locks, raw upserts,
  conditional updates, lease fences, and critical outboxes
- Affected stakeholders: buyers, organizers, refund recipients, attendees,
  finance operators, support staff
- Required mitigation: author adversarial race tests before replacing each
  persistence seam; use native conditional mutation and concurrency tokens
  first; preserve transaction and outbox boundaries
- Planned mapping: Scenarios `PERSIST-S4` and `PERSIST-S5`; Tasks 0.7,
  3.3–3.4, 4.1–4.10, and 7.1–7.5
- Refresh trigger: any payment, inventory, refund, admission, idempotency,
  lease, or outbox contract change

### IVSD-PERSIST-004 — Escape Hatches Must Be Inspectable And Exceptional

- Status: open
- Severity: high
- Principles/domains: Truthfulness (`Sidq`), Trust (`Amanah`),
  governance and sustainability
- Provider-controlled decision: where raw SQL, direct commands, provider
  internals, or special engine behavior are permitted
- Evidence: 51 raw EF SQL sites, 24 direct command markers, and internal
  provider migration dependencies exist without a general architecture gate
- Affected stakeholders: maintainers, security reviewers, operators,
  downstream self-hosters
- Required mitigation: enforce a machine-readable capability ladder; isolate
  approved primitives; derive identifiers from EF metadata; parameterize
  values; require capability and provider tests
- Planned mapping: Scenarios `PERSIST-S6` and `PERSIST-S8`; Tasks 1.1–1.10,
  5.1–5.11, and 6.1–6.4
- Refresh trigger: new raw API, provider package upgrade, migration extension,
  or allowlist change

### IVSD-PERSIST-005 — Portability Claims Require Real-Engine Evidence

- Status: open
- Severity: high
- Principles/domains: Truthfulness (`Sidq`), Promise-Keeping (`Wafa`),
  governance and sustainability
- Provider-controlled decision: whether “supported” means model construction
  only or verified runtime behavior
- Evidence: official EF guidance warns that fake providers do not establish
  translation or database behavior; the repository supports five engines
- Affected stakeholders: operators choosing a provider, contributors,
  release managers, support teams
- Required mitigation: define parity scenarios, execute real-engine lifecycle
  and concurrency tests, record provider limitations, and block release on
  unverified claims
- Planned mapping: Scenarios `PERSIST-S7` and `PERSIST-S9`; Tasks 6.5–6.14,
  7.1–7.11, and 8.1–8.10
- Refresh trigger: provider/version change, new translation, performance
  exception, or release-support statement

## Recommendations

1. Make the EF Core model, LINQ, tracked entities, set-based mutation APIs,
   transactions, and concurrency tokens the mandatory first rung.
2. Keep provider translations and public provider APIs as the second rung.
3. Permit SQL only through narrowly named provider-primitive types after the
   first two rungs are shown insufficient.
4. Require EF metadata and `ISqlGenerationHelper` for unavoidable physical
   identifiers and parameter APIs for every value.
5. Preserve all existing tenant, payment, inventory, admission, outbox,
   idempotency, and erasure invariants with Red-before-Green tests.
6. Treat real-engine parity and transparent operational limitations as release
   requirements, not optional confidence work.
7. Do not add a provider extension dependency without a separate provenance,
   license, maintenance, and capability decision.

## Stakeholders

| Stakeholder | Interest or exposure |
| --- | --- |
| Users and erased subjects | Confidentiality, correct deletion fencing, and no data resurrection |
| Buyers and refund recipients | Monotonic financial state and no duplicate or lost effects |
| Event organizers | Accurate capacity, admission, notifications, and provider-independent operation |
| Self-hosting operators | Honest provider support, predictable migrations, backup/restore, and rollback |
| Contributors and maintainers | Clear abstractions, enforceable rules, and bounded provider complexity |
| Security/privacy reviewers | Tenant isolation, RLS/session behavior, zero-PII evidence, and auditable exceptions |
| Release/support teams | Reproducible provider evidence and documented limitations |

## I-VSD Principles And Domains

| Principle/domain | Planning application |
| --- | --- |
| Trust (`Amanah`) | Preserve critical state and make provider exceptions reviewable. |
| Justice (`Adl`) | A self-hoster's provider choice must not receive weaker domain correctness. |
| Truthfulness (`Sidq`) | Provider support claims require real-engine evidence and documented gaps. |
| Privacy | Tenant and erasure behavior fail closed during every refactor. |
| Avoiding spying (`Tajassus`) | SQL diagnostics and concurrency tests must not expose sensitive values. |
| Avoiding harm | Race, rollback, and partial-failure cases are specification tests, not assumptions. |
| Promise-Keeping (`Wafa`) | Migration, backup, restore, and rollback documentation match shipped behavior. |
| Governance and sustainability | One capability ladder and enforceable architecture tests replace reviewer memory. |
| Economic ethics | Payment/refund/inventory correctness is never traded for portability convenience. |

## Validation Gaps

- No implementation baseline, provider-engine run, migration generation,
  mutation run, or MAD review has started.
- Performance envelopes for portable EF replacements are not yet recorded.
- The exact final SQL exception registry is not yet available.
- User implementation approval has not yet been granted.

## Escalation Needed

No scholarly escalation is required. Escalate to the Project Steward if
implementation proposes removing a supported provider, removing configurable
schemas, adding a provider-extension dependency, weakening a critical domain
invariant, or replacing tracked migration history rather than generating
corrective artifacts.

## Evidence Reviewed

- Shared evidence packet revision stated above
- `AGENTS.md`
- Persistence and migration path rules
- Architecture, domain, multi-tenancy, operations, testing, release, and
  configuration documentation
- Current persistence source and provider project composition
- Registration Data Collection completion context
- Event Ticketing Lifecycle planning context
- Official EF Core, Npgsql, PostgreSQL, naming-convention, and Microting
  documentation linked from the evidence packet

## Missing Evidence

- Final plan and tasks revisions
- Green pre-change build and target test baseline
- Real-engine behavior and concurrency results
- Generated migration and pending-model evidence
- Mutation score for owned persistence logic
- Anonymized MAD review decision

## Context Inventory

- Product surface: backend persistence and migration infrastructure
- Providers: PostgreSQL, SQLite, SQL Server, MariaDB, MySQL
- Critical domains: payments, inventory, admission, notifications, webhooks,
  tenant isolation, privacy erasure
- Deployment: split, combined, local Aspire, containerized, and self-hosted
- Backward compatibility: no runtime compatibility shim requirement; generated
  migration and repository governance still applies
- User decision authority: implementation approval and destructive operator
  actions remain outside this report

## Review Lifecycle

This report is `current / plan-aligned` because:

1. the plan and tasks files exist;
2. every material `IVSD-PERSIST-*` finding maps to named scenarios and tasks;
3. exact plan, tasks, and evidence revisions are recorded;
4. no material planning branch remains unresolved.

Any material scenario, provider-support, migration, tenant, critical-state,
telemetry, or release change makes this report stale and requires revalidation.

### Review History

| Date | Status / disposition | Evidence revision | Reason |
| --- | --- | --- | --- |
| 2026-08-27 | draft / ready-for-planning | `f5790b0a6a91a6d2de419598023b08af27f414883f0e049cee2f8bf974311a72` | Initial planning-mode assessment created from the shared repository evidence packet before drafting the triad. |
| 2026-08-27 | current / plan-aligned | Plan `7493c0035e1bf7c4ea5309eeb822cb20e0b422e3da16ca39688395e093474c44`; Tasks `b375cfbdf4dd4d61757482900cc0721513dd5d15d014cf9b9a426eb96acadc55`; Evidence `f5790b0a6a91a6d2de419598023b08af27f414883f0e049cee2f8bf974311a72` | Final triad revalidated; every material finding maps to scenarios and exact task ranges with no unresolved planning branch. |
