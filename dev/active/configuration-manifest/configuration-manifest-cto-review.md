<!-- ABOUTME: Revision-bound Senior CTO approval for ConfigurationManifest Phases 16-23. -->
<!-- ABOUTME: Reviews completeness, correctness, coherence, risk boundaries, and implementation readiness. -->

# Senior CTO Feedback

Last Updated: 2026-08-30 Europe/Brussels

## Review Metadata

- Review mode: Read-only
- Reviewed plan revision:
  `sha256:bae6d042ce734005397554fd69666db89d3a6cd0c7e2cd0b3ae106a22071dd8a`
- Reviewed tasks revision:
  `sha256:5f67f6465c571278601c5ae6a9318cbcbeb96cc819af4fd062c9a5ce8bb459e9`
- Reviewed I-VSD revision:
  `sha256:389383534bc12892108b00846586941d18420f346b4cb15758b0c2bac80511c3`
- I-VSD freshness: Current, disposition `plan-aligned`
- Decision: Approve
- User approval: Granted by the user on 2026-08-30; not granted by this review

## Executive Verdict

The expanded plan is large but technically ready because it is an umbrella
workstream with seven explicit, independently gated delivery boundaries rather
than one omnibus change. It keeps artifact identity separate from target
authority, separates whole-instance and tenant-owned portability, sequences
contract/domain/persistence/API/UI work inward-to-outward, and assigns the
catastrophic tenant-isolation and atomicity failures to test-first
Invariant-Breaker tasks. The current I-VSD revision maps every applicable
finding and explicitly defers the Setup Assistant family. No approval blocker
survives this revision.

**Decision: Approve.**

## 3-Dimensional Scorecard

| Dimension | Status | Key finding |
|---|---|---|
| **Completeness** | Pass | Phases 16-23 cover contracts, legal ownership, preview, apply/recovery, tenant migration, UI, managed operations, generated artifacts, operations, and release. |
| **Correctness** | Pass | Public-contract Red tasks precede authority, lifecycle, session, concurrency, isolation, and transfer implementations; phase gates use the owning test projects. |
| **Coherence** | Pass | Domain owns lifecycle invariants, Application owns orchestration/contracts, Persistence owns EF/transactions, API owns transport/HAL, and Blazor remains generated-client/HAL driven. |

## Top Risks

### 1. CRITICAL — Target authority confusion

**Why it matters:** Artifact metadata selecting a target could let a tenant
package mutate another tenant or whole-instance state.

**Evidence from the plan:** Sections 3.6, 4, 5.4, CM-1610, CM-1810, and
CM-2010 require route/session authority, reject source identifiers as target
authority, and scan tenant packages for other-scope data.

**Required implementation control:** Keep the authenticated route/context as
the sole target selector and preserve wrong-scope tests at contract, API, and
persistence boundaries.

### 2. CRITICAL — Partial or stale bulk mutation

**Why it matters:** A stale preview or one failed section could otherwise leave
configuration, legal publication state, audit, or effects inconsistent.

**Evidence from the plan:** CM-1910 requires real PostgreSQL races before
CM-1920; Section 5.3 binds digest, revisions, mappings, selections, approvals,
ordered leases, one fresh serializable transaction, snapshot, receipt, and
outbox.

**Required implementation control:** Do not weaken the full Persistence phase
gate or substitute mock/call-count tests for observable database state.

### 3. CRITICAL — Legal evidence fabrication

**Why it matters:** Importing published history or acceptance facts would make
untrue accountability claims and could erase the last valid public document.

**Evidence from the plan:** CM-1710 through CM-1730 separate typed source,
target drafts, immutable publication versions, and nonportable acceptance
evidence, with one constrained Markdown rendering contract.

**Required implementation control:** Keep publication/acceptance facts outside
portable artifacts and preserve the last published version on every failed
draft/import.

### 4. WARNING — Advanced direct-transfer complexity

**Why it matters:** SSRF, replay, resumability, and destination proof can turn a
configuration convenience into a network-security boundary.

**Evidence from the plan:** CM-2220 is isolated after package-based import and
requires opt-in mutual approval, destination proof, nonce/digest binding,
expiry, cancellation, bounded resume, duplicate-commit handling, and no source
deletion.

**Required implementation control:** Treat direct transfer as an optional
transport into the same target preview/approval/apply pipeline, never as an
alternate mutation authority.

## What I Would Keep

- Distinct `ConfigurationManifest` and `TenantConfigurationPackage` kinds.
- Closed section registry with truthful supported/omitted/environment-bound
  coverage rather than reflection-based discovery.
- Preview-first import sessions and forward rollback from protected snapshots.
- Real PostgreSQL serial-order tests for the highest-risk mutation path.
- HAL-only UI affordances and server-held BFF tokens.
- Explicit no-secret, no-PII, no-application-data, no-provider-state boundary.
- Clean v1alpha2 cutover with no aliases or compatibility readers.
- Explicit deferral of Avalonia, Terminal.Gui, CLI/TUI, `.env`, and agent-skill
  work until this Definition of Done is proven.

## What Must Remain True During Implementation

1. Close each delivery boundary before starting its dependent boundary.
2. Keep full-project phase tests at phase exits; use focused TUnit selectors
   during active Red/Green work.
3. Generate migrations, snapshots, schemas, OpenAPI, inventory, and NSwag from
   source; never patch generated output.
4. Preserve manual validators, entity-returning repositories, named query
   filter discipline, ordered leases, and transactional outbox ownership.
5. Record value-minimized evidence for Tier 1 and applicable Tier 0 behavior.
6. Keep legal, security/privacy, accessibility, operations, and scholarly
   escalation claims as release gates rather than certifications.

## Dev-Docs Quality Assessment

### `configuration-manifest-plan.md`

The plan distinguishes current v1alpha1 evidence from the v1alpha2 target,
defines observable requirements and failure scenarios, records authority and
transaction decisions, and now makes its seven delivery boundaries explicit.
The phase-end gates and rollback diagnoses are proportionate to each layer.

### `configuration-manifest-context.md`

The context puts the current phase, first task, approval state, I-VSD status,
deferred-client boundary, and unwaived Persistence gate near the top. Historical
evidence remains clearly separated from current progress.

### `configuration-manifest-tasks.md`

The task ledger sequences the high-criticality Red tasks before production
changes and gives every implementation task observable acceptance criteria.
The former mutation-testing release requirement was removed before this review
because current greenfield governance disables that gate.

## Islamic Value-Sensitive Design Assessment

`islamic-value-sensitive-design/i-vsd-configuration-manifest.md` is current and
`plan-aligned` at the reviewed revision. IVSD-F001 through IVSD-F024 map to
named CM tasks and release escalations. IVSD-F025 through IVSD-F030 remain
accepted but explicitly non-applicable to this workstream and deferred to the
future Setup Assistant plan. The report keeps provider responsibility separate
from legal, security, accessibility, or religious certification.

## Socratic Stress-Testing And Worst-Break Audit

### The Worst Break

The catastrophic scenario is a tenant-authorized package selecting or leaking
another tenant/instance while a multi-section apply partially commits. CM-S1,
CM-1610, CM-1810, CM-1910, and CM-2010 jointly require wrong-scope,
side-effect-free preview, cross-tenant output scans, and real database atomicity
proof before the write implementation can pass.

### Stress-Test Findings

- **Rollback safety:** forward rollback replays current target authority and
  never rewrites history.
- **Tenant boundary:** route/context, not package metadata, selects authority;
  query-filter bypasses remain named and scoped.
- **Operator clarity:** preview categories, modes, omissions, external setup,
  receipts, effect-pending state, snapshot availability, and recovery are
  explicitly named.
- **Dependency failures:** snapshot failure blocks apply; provider effects occur
  after commit through outbox retry; policy-provider failure denies.
- **Retention constants:** exact durations remain legitimately deferrable to
  CM-1810 because bounded expiry and deletion semantics are already fixed.

## Enterprise And Self-Hosting Assessment

No new mandatory external service or secret source is introduced. Startup file
bootstrap remains distinct from Day 2 browser import. The plan covers bounded
temporary storage, health/effect visibility, retention, reset/cutover,
source-retention, snapshot-unavailable behavior, release notes, and operator
recovery. Configuration is explicitly not backup, secret migration, provider
setup, or application-data migration.

## Security And Multi-Tenancy Assessment

Server authorization remains authoritative at endpoint and MediatR resource
boundaries, with Cerbos/local parity and fail-closed behavior. Unsafe BFF
methods require antiforgery, browser input cannot supply privileged target
headers, and bearer tokens remain server-side. Single- and multi-tenant modes
share the contract while tenant packages remain tenant-filtered and
instance-ceiling constrained.

## Architecture And Maintainability Assessment

The design follows Clean Architecture and deep-module boundaries. The section
registry centralizes portability classification without becoming an automatic
authorization mechanism. Import-session state, semantic preview, and atomic
apply are separate Application concepts; EF and lease mechanics remain
Persistence details. API/HAL and Blazor do not duplicate authorization.

## Breaking-Change Position

The v1alpha1 schemas, media types, generated-client identities, and obsolete
tests should be deleted in the v1alpha2 cutover. No aliases, dual reads,
redirects, or deprecated routes are justified in the pre-v1 repository.
Generated corrective migrations or documented development reset—not hand-edited
migration code—own persistence changes.

## Implementation Sequencing

1. Phase 16 contract and registry cutover.
2. Phase 17 legal aggregate and rendering ownership.
3. Phases 18-19 import preview and atomic recovery backend.
4. Phase 20 tenant package/API migration surface.
5. Phase 21 BFF/client administration.
6. Phase 22 optional managed ownership/direct transfer.
7. Phase 23 generated artifacts, docs, evidence, and release.

## Verification Bar

- One Release build and the phase-selected full test project at each boundary.
- Focused public-contract selectors during Red/Green work.
- Real PostgreSQL races for apply/rollback.
- Multi-provider migration/model-current checks.
- Tenant/instance authorization parity and output scans.
- Zero-value/secret/PII telemetry scans.
- Accessibility, localization, RTL, and capability-loss component contracts.
- Final Architecture project, generated-byte stability, I-VSD reconciliation,
  and validated breaking change fragment.

## PR Split Recommendation

Treat the seven boundaries in Plan Section 6.1 as independently reviewable
change sets. Do not create an omnibus commit, and do not create any commit
without separate user authorization.
