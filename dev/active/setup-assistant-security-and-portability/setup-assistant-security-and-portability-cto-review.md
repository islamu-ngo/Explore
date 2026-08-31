<!-- ABOUTME: Revision-bound Senior CTO review of the Setup Assistant security and portability workstream. -->
<!-- ABOUTME: Records the mandatory split decision, risk gates, review boundaries, and pre-SA-110 corrections. -->

# Senior CTO Feedback

Last Updated: 2026-08-31 Europe/Brussels

## Review Metadata

- **Review mode:** Read-only
- **Reviewed plan:**
  [setup-assistant-security-and-portability-plan.md](setup-assistant-security-and-portability-plan.md)
- **Reviewed plan revision:**
  `sha256:1c5048a532224c16124b4c09da070036e4eeb59c06f8fdf3e695e8c1c991e255`
- **Reviewed tasks:**
  [setup-assistant-security-and-portability-tasks.md](setup-assistant-security-and-portability-tasks.md)
- **Reviewed tasks revision:**
  `sha256:7460b2230f07b25ee569469cfa315653e8687a1927935aed36cead965e4757c2`
- **Reviewed I-VSD report:**
  [i-vsd-setup-assistant-security-and-portability.md](../../../islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md)
- **Reviewed I-VSD revision:**
  `sha256:a4e96fd155b59e4c995c9f7328f4873626826dc21601cf82c95e6d14290b5cea`
- **I-VSD freshness:** Current; disposition `plan-aligned`
- **I-VSD planning input revision:**
  `sha256:055fb1dd8c0dfcdbd809bbfb89cbd2660904469fd3d866d6d6349af091793d4f`
- **Decision:** Split before approval
- **User approval:** The triad records approval on 2026-08-31. This review
  records that state but does not grant, renew, or broaden user approval.
- **Implementation state:** Not started; 0/41 implementation tasks complete.
- **Baseline evidence:**
  [Setup Assistant verification summary](../../../.omo/evidence/20260831-setup-assistant-security-and-portability/summary.md)

The I-VSD report remains current under its integration contract. Its planning
input digest predates status-only triad synchronization, and status-only
synchronization does not invalidate I-VSD. This review does not treat that
mechanical difference as staleness. It does, however, review the exact current
plan text and finds unresolved internal traceability contradictions that must
be corrected before technical approval.

## Executive Verdict

The architecture is substantially better than a typical XL plan: it keeps
portable configuration non-secret, keeps live authority server-side, makes
browser secret entry disabled by default, isolates filesystem and terminal
risks, requires deterministic machine contracts, and preserves clean-room and
outbound-license gates. I would still not approve implementation from this
revision. It is a 12-phase, 41-task program that combines a new offline product,
multiple presentation targets, security-sensitive secret handling, live
control-plane authority, tenant data migration, sovereign payment operations,
and release engineering. All four mandatory right-sizing symptoms apply. The
exact artifacts also disagree about I-VSD mappings and whether live APIs are in
scope. Those are planning-integrity defects, not polish.

**Decision: Split before approval.**

No Setup production task, including SA-110, may start until the pre-SA-110
correction and fresh revision-bound review sequence in this artifact completes.
Later Tier 1, Tier 2, Tier 0, dependency, legal, accessibility, and release
gates remain intact and cannot be waived by this decision.

## 3-Dimensional Scorecard

| Dimension | Score | Status | Key finding |
|---|---:|---|---|
| **Completeness** | 2/5 | Blocker | The broad capability inventory is strong, but the exact plan Section 9 stops at IVSD-F036/M036 and still calls I-VSD stale while the report is current and maps F037-F046/M037-M046. Clean-room/context scope statements also exclude live APIs that Phases 9-11 include. |
| **Correctness** | 3/5 | Warning | Many high-risk Red tasks are named first, but the single Worst Break is not isolated as one deterministic public-seam test. Phase 11 combines tenant, privacy, replay, file, payment, refund, and provider races in one overloaded Red task without an executable provider/coordination matrix. |
| **Coherence** | 2/5 | Blocker | Inner-core, adapter, HAL, outbox, and server-authority boundaries are sound. Delivery coherence fails because independently shippable offline, UI, live-control-plane, application-data, payment, and release intents are one approval unit, and Phase 10 permits an upstream verification waiver. |

### Detailed CTO Scorecard

| Area | Score | Assessment |
|---|---:|---|
| Strategic fit | 4/5 | Offline setup and portability materially improve self-hosting; live data and payment migration are separate products/programs, not incidental Setup features. |
| I-VSD | 4/5 | Current report has F001-F046/M001-M046, evidence limits, authority boundaries, and later gates. Exact plan traceability is not synchronized. |
| Socratic stress test | 3/5 | Strong threat inventory, but no one named Worst Break test and no measurable query/resource thresholds. |
| Architecture integrity | 4/5 | Package-free contracts, pure Core, outer adapters, server-side authority, and no compatibility shims are correct. |
| Security/trust | 4/5 | Browser-origin truth, disabled secret mode, no value readback, protected handles, and fail-closed semantics are strong; release proof remains absent by design. |
| Multi-tenancy | 3/5 | Correct intended authority, but migration filters, provider matrices, and background tenant-context proof remain later design gates. |
| Data/migrations | 2/5 | Phase 11 names checkpoints/outbox but does not yet specify expand/contract deployment, every-provider migration ownership, query/index shapes, or downgrade/reset evidence precisely enough. |
| API/contracts | 3/5 | HAL and RFC 7807 authority are correct; enrollment operation IDs, idempotency preimages, rate limits, and generated-client split points are not yet concrete. |
| Self-hosting/operations | 3/5 | Configuration, signing, support, incident, and recovery areas are recognized; required/optional services, health, resource ceilings, and upgrade actions are not yet executable per release slice. |
| Testability | 3/5 | Red-before-Green is generally present and mock-mirroring is prohibited; Phase 11 and exact-bundle evidence need sharper independent invariant lanes. |
| Sequencing/right-sizing | 1/5 | All four mandatory split symptoms apply. |
| Dev-doc quality | 2/5 | Rich and resumable, but internally contradictory and too large for one approval/review state. |

## Top Risks

### 1. CRITICAL — Severity: Blocker — The program is not review-sized

**Blocker — do not approve until fixed.**

**Why it matters:** A single review state spanning offline codecs, browser and
desktop secret handling, live tenant authority, PII migration, money mutation,
packaging, and an agent skill makes regressions hard to isolate and approvals
easy to overread. A green early phase could be mistaken for authority to enter
a later Tier 0/1/2 phase.

**Evidence from the plan/codebase:** The plan declares `XL`, 12 phases, and 41
major implementation tasks. Phases 9-11 add API/Application/Persistence/HAL/UI,
privacy, and payment behavior after a separately shippable offline product.
The tasks ledger confirms 0/41 and one umbrella approval state.

**Minimum acceptable fix:** Adopt the worktree/PR program split in
[PR Split Recommendation](#pr-split-recommendation), preserve task IDs and
full objective, and make every successor boundary independently reviewed and
approved.

### 2. CRITICAL — Severity: Blocker — Exact dev-doc and I-VSD traceability disagree

**Blocker — do not approve until fixed.**

**Why it matters:** An implementation agent cannot tell whether F037-F046 are
approved mappings or blocked future analysis. That ambiguity sits directly on
live tenant authority, PII custody, and payment mutation.

**Evidence from the plan/codebase:** Plan metadata says `current` /
`plan-aligned` and names F037-F046, while plan Section 9 records the superseded
`8c86...` revision, says `stale` / `changes-required`, maps only F001-F036, and
says SA-810-SA-1140 await new findings. The current I-VSD report is
`plan-aligned`, maps F037-F046/M037-M046, and names the later gates. Context
validation text likewise says new findings are pending even though context
metadata says revalidation completed.

**Minimum acceptable fix:** Replace the stale Section 9 status and mapping body
with the current report's complete F001-F046/M001-M046 mapping; synchronize
context/tasks status statements without changing provider-controlled behavior
or weakening any gate.

### 3. CRITICAL — Severity: Blocker — Scope provenance excludes work now in scope

**Blocker — do not approve until fixed.**

**Why it matters:** The implementation boundary cannot rely on a source-free
handoff that says live APIs are excluded while the plan authorizes live target,
provider, migration, and payment implementation.

**Evidence from the plan/codebase:** The clean-room evidence Identity and
Resolved Architecture Decision 3 define an offline product that does not call
live APIs. Context Key Decision 10 says live APIs are outside the approved first
workstream. Plan Phases 9-11 and tasks SA-910-SA-1140 include exactly those
surfaces. The I-VSD report did expand repository evidence for those phases, but
the workstream's implementation handoff remains contradictory.

**Minimum acceptable fix:** Make the clean-room handoff and context accurately
state that offline implementation is the first delivery program and that live,
data, and payment work are separate successor workstreams governed by the same
source-free constraints and their own evidence packets.

### 4. CRITICAL — Severity: Critical — The Worst Break is not one executable invariant

**Critical — must be added to the implementation plan.**

**Why it matters:** The catastrophic failure is a replayed or mis-scoped live
migration mutating another tenant's payment/refund state while reporting
completion. It combines confidentiality, tenant integrity, and irreversible
financial harm.

**Evidence from the plan/codebase:** SA-1110 lists cross-tenant mappings,
duplicate replay, checkpoint races, PII telemetry, and money mutation in one
class. SA-1140 adds provider reconciliation. Neither task defines one
synchronized, real-database, public-seam scenario with exact one-winner/no-money-
mutation/final-state assertions. Plan Section 7.2 does not list the Phase 9-11
adversarial scenarios despite those phases now being in scope.

**Minimum acceptable fix:** In the payment successor workstream, add one named
Red scenario before production code: two concurrent/replayed requests with a
mismatched tenant or stale capability race finalization/refund; assert zero
cross-tenant rows, zero provider/outbox money intent, unchanged checked ledger
balances, one durable value-free conflict receipt, and no PII/secret in logs.
Use deterministic coordination, a bounded timeout, and the real owning database
and provider contract. Do not use sleeps or mocks.

### 5. CRITICAL — Severity: Critical — A user waiver can bypass an upstream security gate

**Critical — must be added to the implementation plan.**

**Why it matters:** ConfigurationManifest atomicity, replay, tenancy, and
receipt truth are prerequisites to Setup live apply. A waiver cannot convert
missing safety evidence into a valid dependency.

**Evidence from the plan/codebase:** Plan Phase 10 depends on green upstream
ConfigurationManifest gates **or an explicit user waiver**; the tasks use
`green/waived ConfigurationManifest gates`.

**Minimum acceptable fix:** Remove the waiver path. Missing Tier 1/tenant/
atomicity evidence disables live apply/direct transfer. It must never authorize
a compatibility shim, local rollback authority, or weaker fallback.

## What I Would Keep

- One canonical v1alpha2 JSON wire contract and no compatibility aliases.
- `Event.Wire.Contracts` plus a package-minimal deterministic
  `Event.Setup.Core`; no server/UI/provider dependencies in Core.
- Separate non-secret portable artifacts and deployment-local dotenv output.
- Browser no-secret default and immutable disabled hosted-secret capability.
- Truthful origin trust, memory-erasure, accessibility, and support claims.
- Platform-specific protected-write adapters that fail closed.
- Non-secret machine CLI and human-only TTY secret completion.
- Server-owned target, tenant, provider, import, transfer, payment, and legal
  publication authority; Setup follows current HAL affordances.
- Transactional outbox, durable checkpoints, idempotency, source retention, and
  value-free receipts for later migration work.
- Complete dependency graph review, AFC/SSO evidence, lock files, SBOM,
  checksum, signing/provenance, and no scanner-as-counsel shortcut.
- Explicit rejection of backward-compatibility shims and unapproved release
  targets.

## What Must Change Before Implementation

### Pre-SA-110 corrections

1. Add the mandatory program/worktree/PR boundaries below to plan, context, and
   tasks while preserving all 41 SA IDs, scenarios, mitigations, and dependency
   order.
2. Synchronize exact I-VSD state and all F001-F046/M001-M046 mappings across the
   triad. Status-only synchronization does not invalidate the current I-VSD.
3. Correct clean-room/context scope so offline, live, application-data, and
   payment work have truthful separate implementation handoffs.
4. Remove every `green or waived` / `green/waived` upstream safety-gate path.
5. Add the named Worst Break ownership to the payment successor boundary; do
   not pretend its later Red test has already run.
6. Recompute plan/tasks hashes and obtain a fresh read-only CTO review of the
   corrected exact revision. If the correction preserves behavior, provider
   authority, and all IVSD task mappings, it is sequencing/status correction
   and does not itself stale I-VSD. Any material change to scope, authority,
   defaults, data custody, payment behavior, or mapped mitigations requires
   planning-mode I-VSD revalidation first.
7. Record explicit user approval for the corrected exact revision. This review
   does not supply it.

**Exact pre-SA-110 action:** revise only the planning triad and source-free
handoff to encode the split, complete the current I-VSD synchronization, remove
the waiver, and bind fresh review/user approval to the new hashes. Then and only
then may the foundation worktree start SA-110 Red.

### Later gates; not prerequisites for writing the corrected SA-110 Red test

- SA-120: exact direct/transitive/native/tooling/asset/font/package dependency
  review, locked restore, repository scanner, AFC/SSO, and outbound-license
  decision. Unknown or incompatible material blocks the target.
- SA-610-SA-640: exact-bundle browser request/storage/CSP/origin evidence plus
  independent security and legal review before hosted secret mode is enabled.
- SA-710-SA-730: target filesystem/ACL/link/atomicity evidence before claiming
  desktop secret-output support.
- SA-910: fresh Tier 1 tenant, authorization, replay, provider-coordinate, and
  readback review before live authority work.
- SA-1110: fresh Tier 2 custody/retention/staging/erasure/anti-resurrection
  decisions and Tier 0 hold/finalization, payout, and refund-allocation intake.
- SA-1140: exact provider/ledger/recipient/currency/refund reconciliation,
  authorized actors, and unknown-outcome recovery evidence.
- Release: signing/notarization, SBOM, provenance, accessibility, legal,
  security, privacy, payment, operator recovery, and support evidence for each
  exact shipped target/capability. Missing evidence disables only that target
  or capability; it never weakens a Tier 0/1/2 gate.

## Dev-Docs Quality Assessment

### `setup-assistant-security-and-portability-plan.md`

The plan is unusually strong on behavioral scenarios, trust boundaries,
rejected alternatives, release claims, and fail-closed posture. It correctly
separates Section 3 behavior from Section 5 architecture and keeps granular
checkboxes out of the plan. It is not approval-ready because Section 9 is a
superseded I-VSD state, the scope is a program rather than one workstream, the
Phase 10 waiver is unacceptable, and scale/query/upgrade acceptance lacks
measurable thresholds.

### `setup-assistant-security-and-portability-context.md`

The context is resumable and honestly records the unrelated Release-build
failure. It contradicts itself by recording expanded live phases while Key
Decision 10 excludes live APIs, and its validation list still says expanded
findings await revalidation. It must become program-level context with a clear
active successor/worktree and no umbrella approval inference.

### `setup-assistant-security-and-portability-tasks.md`

The ledger has observable acceptance, Red-first security tasks, no mock-
mirroring, no fixed sleeps, and one build/test phase gate. Forty-one major tasks
are beyond reviewable capacity. SA-1110 is overloaded, backend and UI changes
are mixed in later phases, and release waits for the entire program instead of
producing evidence per shippable subset. Preserve IDs while assigning each to
an independently reviewed boundary.

### `setup-assistant-security-and-portability-clean-room-evidence.md`

The evidence has the required official-source register, source-free functional
specification, clean-room attestation, dependency stop condition, and evidence
limits. It is adequate for the offline product but not truthful as the sole
handoff for newly included live/data/payment scope. Create scoped source-free
successor evidence rather than ingesting or copying external implementation
material. Candidate package metadata remains evidence, not approval.

## Islamic Value-Sensitive Design (I-VSD) Assessment

The reviewed report at revision
`a4e96fd155b59e4c995c9f7328f4873626826dc21601cf82c95e6d14290b5cea`
is current and `plan-aligned`. It distinguishes provider responsibility from
fatwa, Sharia, legal, privacy, security, accessibility, and license
certification. F001-F046 map one-to-one to M001-M046 and cover protective
defaults, origin trust, secret non-disclosure, legal authority, accessibility,
dependency stewardship, tenant authority, PII custody, migration truth,
sovereign money, source retention, and human agency.

The report's own Planning Handoff correctly maps F037-F046/M037-M046 to
Scenarios 3.13-3.15 and SA-810-SA-1250. The defect is in the exact plan Section
9, not in the current I-VSD report. Status-only triad synchronization does not
invalidate I-VSD. The proposed split also need not invalidate it if it preserves
provider-controlled behavior and every existing mapping. Material scope,
authority, privacy, payment, dependency-outbound, or mitigation changes do
trigger refresh.

This CTO review grants no user, provider, legal, privacy, payment, release,
accessibility, or scholarly approval.

## Socratic Stress-Testing & Worst Break Audit Findings

### The Worst Break Catastrophic Scenario Check

**Worst Break:** A stale/replayed live capability and tenant mismatch race a
payment/refund migration, mutate money or durable intent for the wrong tenant,
and produce a false completed receipt.

The plan recognizes each ingredient but does not own it as one dedicated
failing-first scenario. SA-1110/SA-1140 are not enough as written because they
bundle many independent invariants and do not specify exact deterministic race
coordination and final-state assertions. The payment successor workstream must
add the dedicated Red scenario defined in Top Risk 4 before any payment
migration production code.

### Grill-Me Stress-Test Findings

| Challenge | Finding | Approval consequence |
|---|---|---|
| Rollback safety | Offline adapters fail closed and live work correctly prefers server forward recovery. Phase 10's waiver breaks the dependency proof. | Remove waiver pre-SA-110; live work remains later-gated. |
| Tenant boundary | Server route/context/HAL authority is correctly intended. Exact migration key/filter/bypass/background-context and provider-matrix proof is deferred. | Fresh Tier 1 review before SA-910 and real-provider tenant tests before merge. |
| Query/resource performance | SA-830 says measured profiles, but no memory/time/cardinality threshold exists. Migration lists no batch, index, lock-duration, or query-count ceiling. | Set named profiles and representative cardinalities in each successor before its implementation. |
| Operator clarity | Pending/unknown/reconciled states, source retention, value-free receipts, and disabled capabilities are strong. Required/optional services and per-slice recovery commands are not yet concrete. | Add operator contract to each release slice. |
| Dependency failure | Unknown packages and provider unavailability fail closed. Scanner is correctly not treated as counsel. | Keep SA-120 and provider gates blocking. |
| Edge cases | Browser channels, file races, terminal leakage, composition ambiguity, replay, erasure, and refund conflicts are named. Their combination in one umbrella hides ownership. | Assign each to the split boundary that can independently prove it. |
| Recovery truth | The plan distinguishes committed, pending, unknown, compensated, and reconciled. | Preserve these machine-consumed states; no prose-only completion tests. |

## Enterprise / Self-Hosting Assessment

| Self-hoster question | Current answer |
|---|---|
| Required services | Offline Core/CLI should require none beyond the local runtime; live/data/payment successors depend on existing server services. Exact per-slice list must be written. |
| Optional services | Browser hosting, secret providers, live transfer, and payment capability are optional/gated; package-specific optionality needs generated capability metadata. |
| Environment variables | A canonical catalogue is planned but not implemented. Keys/defaults/activation/startup behavior are SA-310/SA-320 evidence. |
| Safe defaults | No-secret, no telemetry, disabled hosted-secret mode, no raw readback, and no unprotected write fallback are correct. |
| Secrets and rotation | Values remain local or provider write-only; CI signing credentials remain external. Enrollment/profile handle revocation and signing-key incident rotation need per-slice runbooks. |
| DNS/proxy/TLS | Relevant only to hosted browser/live target; exact origin, security headers, TLS, and proxy requirements remain release evidence. |
| Database migrations | None for offline product; live/data/payment successors require generated migrations for all affected providers and expand/contract where tenancy/money tables change. |
| Breaking config/data | v1alpha2 owner cutover and generator-owned `.env.example` are intentional breaks; self-hoster update/reset steps must ship with their slice. |
| Health | Offline `doctor` is planned; live dependencies require server health/readiness without leaking coordinates or values. |
| Recovery | Protected-write cleanup, capability revocation, checkpoint resume, source retention, and provider reconciliation are sound directions; commands and evidence remain later gates. |
| Disable/revert | Target/capability omission is preferred; no compatibility shim or unsafe fallback. Signed prior artifact and forward server recovery are the rollback model. |
| Constrained resources | Named composition profiles are planned but have no thresholds yet; desktop/browser/CLI runtime support claims require measured evidence. |

This is credible self-hostable architecture, but not yet an operator-executable
single release plan. Release the offline subset independently; do not make it
wait for or imply support for live migration or sovereign payments.

## Security and Multi-Tenancy Assessment

- **Authentication/authorization:** Offline operation needs none. Live writes
  must use short-lived, target-qualified, revocable server authority. HAL link
  presence is an affordance, not cached authorization; the server reauthorizes
  every mutation.
- **Browser/BFF boundary:** Browser code never becomes authority and never sees
  server bearer tokens. Any unsafe BFF endpoint requires antiforgery and strips
  browser-controlled privileged headers. Setup must not forward target, tenant,
  setup secret, provider, or policy authority from raw browser input.
- **Secrets:** No portable value, raw readback, provider coordinates, argument,
  environment transport, machine JSON, logs, traces, support reports, browser
  stores, or accessibility text. No permissive fallback when provider readiness
  is unavailable.
- **Tenant isolation:** Target server context is authoritative. Mappings,
  checkpoints, idempotency, caches, outbox rows, staging, and receipts must be
  tenant-qualified. Any query-filter bypass must be named, scoped, and tested.
- **Privacy:** Application-data migration is a separate custody contract. It
  must preserve authority-first erasure, anti-resurrection fencing, purpose,
  retention, staging disposal, and payload-free evidence.
- **Payments:** `OrganizerDirect`, immutable recipient/currency facts,
  deterministic partial-refund allocation, provider/ledger reconciliation,
  checked arithmetic, idempotency, and unknown-outcome parking remain
  authoritative. Setup never derives money truth from configuration.
- **Fail-closed posture:** Missing authorization, policy, provider, lineage,
  digest, mapping, evidence, or support disables the operation. There is no
  local authority synthesis and no gate waiver.

## Architecture and Maintainability Assessment

The intended dependency direction is correct:

```text
Event.Wire.Contracts <- Event.Setup.Core <- CLI / shared Avalonia
                                          <- Browser adapter
                                          <- Desktop adapter

Setup live adapter -> generated HTTP/HAL contracts -> API transport
API -> Application -> Domain
API -> Persistence/Infrastructure adapters
```

Domain should own migration/payment state invariants; Application owns use-case
orchestration and authorization ports; Persistence owns EF mapping, locks,
indexes, transactions, checkpoints, and outbox atomicity; API owns HTTP,
ProblemDetails, OpenAPI, operation IDs, and HAL; Setup presentation consumes
server-provided capabilities and never owns policy. Repositories return
entities, generated client shape remains generator-owned, and migrations remain
generated.

The new modules are deep where they centralize codecs, catalogue activation,
protected writes, and durable migration state. Avoid shallow Setup services that
only forward repositories or generated clients. The deletion test should be
applied at each split review.

## Breaking-Change Position

The pre-v1 posture is correct:

- move v1alpha2 contracts once and delete old owners;
- delete obsolete tests and generated contracts when replaced;
- do not add namespace aliases, type forwards, duplicate serializers, command
  aliases, route aliases, dual reads, or fallback adapters;
- regenerate schemas, OpenAPI/NSwag, environment templates, locks, migrations,
  SBOMs, and release manifests from source;
- document operator migration/reset and release impact for each shipped slice.

A missing gate is not a reason for a compatibility shim. Disable the affected
capability or target.

## Implementation Sequencing I Recommend

1. Correct and reapprove the program split; no production implementation.
2. Foundation/dependency/architecture Red and Green: SA-110-SA-130.
3. Shared wire/core cutover: SA-210-SA-230.
4. Environment/offline portability, then stable CLI/TUI: SA-310-SA-430.
5. Independently review and ship presentation adapters: SA-510-SA-730.
6. Add composition/scale as a separate Core/adapter enhancement: SA-810-SA-830.
7. After fresh Tier 1 review, build live server authority before Setup UI:
   SA-910-SA-1030 split backend-first and adapter-second.
8. After fresh Tier 2 review, build application-data migration as its own
   server-first workstream: SA-1110-SA-1130.
9. After independent Tier 0 intake/review, build payment operations as a
   separate sovereign workstream: SA-1140.
10. Produce release evidence per shippable subset; run program closeout only
    after all selected capabilities are independently green.

## PR Split Recommendation

All four mandatory right-sizing symptoms apply:

| Symptom | Applies | Evidence |
|---|---|---|
| Multi-intent “and also” scope | Yes | Offline product, dotenv, legal editor, CLI/TUI, browser, desktop, live target, provider binding, direct transfer, application data, payment migration, packaging, and skill. |
| More than 8-10 major tasks | Yes | 41 major SA tasks across 12 phases. |
| Migration + API churn + UI in a big program | Yes | Phases 9-11 cross Domain/Application/Persistence/API/HAL/generated client/Setup UI. |
| Backend can ship before UI | Yes | Shared contracts/Core, live server authority, and migration engine are independently verifiable before Avalonia adapters. |

Therefore the skill mandates **Split before approval**.

| Worktree / successor boundary | Independently reviewable PRs | Entry gate | Exit / dependency |
|---|---|---|---|
| **A. `setup-assistant-foundation-offline`** | A1 SA-110-SA-130 architecture/dependency/CI; A2 SA-210-SA-230 wire/core; A3 SA-310-SA-340 catalogue/offline; A4 SA-410-SA-430 CLI/TUI | Corrected revision, current I-VSD, fresh CTO, recorded user approval | Shippable non-secret offline CLI/TUI; no live authority. |
| **B. `setup-assistant-presentation-targets`** | B1 SA-510-SA-540 shared Avalonia/legal; B2 SA-610-SA-640 browser; B3 SA-710-SA-730 desktop | A public contracts stable; target dependency/security/accessibility gates | Each target can ship or remain disabled independently. |
| **C. `setup-assistant-composition-scale`** | C1 SA-810/SA-820 canonical composition; C2 SA-830 measured profiles | A2/A3 stable | Canonical JSON unchanged; profile support separately evidenced. |
| **D. `setup-assistant-live-control-plane`** | D1 SA-910 Red + server enrollment/authorization contracts; D2 SA-920/SA-930 server behavior and generated contract; D3 SA-1010 Red + SA-1020/SA-1030 Setup adapters/UI | Fresh Tier 1/I-VSD/CTO gate; upstream ConfigurationManifest gates green, never waived | Server backend ships safely before any Setup affordance; no local authority. |
| **E. `setup-application-data-migration`** | E1 SA-1110 privacy/tenant Red; E2 SA-1120 Domain/Persistence/outbox; E3 SA-1130 API/HAL/generated client; E4 Setup UI activation | Fresh Tier 2 custody/erasure and Tier 1 tenant review | Category migration independent of payment and configuration. |
| **F. `setup-sovereign-payment-migration`** | F1 dedicated Worst Break Red and Tier 0 decision record; F2 SA-1140 Domain/Persistence/provider reconciliation; F3 API/HAL/Setup activation | Exact Tier 0 Grill-Me, I-VSD, CTO, provider/legal/operator approvals | No money mutation before reconciliation; can remain permanently disabled. |
| **G. `setup-release-and-agent-contract`** | G1 SA-1210/SA-1220 per selected target/capability; G2 SA-1240 only after CLI schema ships; G3 SA-1250 program reconciliation | Each owning worktree green | Release evidence describes only implemented/evidenced subset. |

Dependencies are one-way: A -> B/C -> D -> E; F depends on D/E contracts but
is independently optional; G runs per shippable subset and again for final
program reconciliation. Do not merge D/E/F into one worktree. Do not expose
new HAL mutation links in a backend PR until its corresponding activation PR is
ready and authorized.

## Operator Runbook Requirements

Each shipped subset must document:

- exact required/optional services, environment keys, defaults, validation,
  startup failure, restart need, and capability manifest;
- secret sources, enrollment/profile/signing-key rotation and revocation;
- canonical origin/TLS/proxy/CSP requirements for browser hosting;
- generated migration order, expand/contract behavior, reset/downgrade caveat,
  and provider support;
- health/readiness/doctor checks and value-free diagnostic codes;
- backup boundaries: configuration is not backup; source retention and
  protected staging/checkpoint recovery;
- cancellation, resume, unknown outcome, reconciliation, compensation, and
  safe disablement procedures;
- exact artifact identity, locks, SBOM, checksums, signatures/notarization,
  provenance, support tier, accessibility evidence, and known limitations;
- incident handling for compromised origin, package/signing key, secret
  exposure, dependency advisory, cross-tenant event, and payment mismatch.

## Verification Bar

### Planning correction verification

- Exactly two leading ABOUTME lines in every changed Markdown artifact.
- Required plan/context/tasks/I-VSD/CTO headings and synchronized review state.
- Complete IVSD-F001-F046/M001-M046 mapping with no orphan task/scenario.
- Current SHA-256 hashes recorded after the last edit.
- Every repository-local Markdown link resolves.
- `git diff --check -- dev/active/setup-assistant-security-and-portability islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md`
- No .NET build/test for the Markdown-only correction.

### Implementation and release evidence

- Focused deterministic Red selector before each security, concurrency, state,
  tenant, privacy, or payment Green task.
- One Release build and at most one owning project test at each PR/phase exit,
  without sleeps, polling luck, mock-mirroring, source/prose scraping, app
  startup, browser automation, Docker, or Aspire.
- Real provider/database tests where persistence, locks, tenant filters,
  migration, outbox, or money invariants are claimed; all affected supported
  providers must have generated migration/model evidence.
- Zero-PII/secret/capability log-sink scans and bounded RFC 7807 errors.
- Tier 0/1/2 reviewer-readable evidence and anonymized MAD review at the owning
  boundary.
- Dependency validation:
  `dotnet run .ci/scripts/validate-dependency-license-policy.cs -- .`, plus
  human target/outbound review; scanner success is not license approval.
- Exact target publish/install/request/storage/filesystem/accessibility/signing/
  provenance evidence before advertising that target.

The current baseline is correctly classified: on 2026-08-31,
`ConfigurationManifestSchemaArtifactTests` passed 2/2 in Release configuration.
The repository Release build failed before Setup implementation with 10
`CS0103` errors in unrelated
`tests/Event.Domain.UnitTests/ValueObjects/AtprotoDidTests.cs` and reported 1026
pre-existing analyzer warnings. That is shared-worktree baseline evidence, not
a Setup design defect and not a Setup pass.

## Recommended Plan Rewrite

Keep the full objective as a program, but change the triad from one executable
mega-workstream into an umbrella index plus the seven successor boundaries
above. Preserve every scenario, SA ID, I-VSD mapping, fail-closed control, and
dependency. The umbrella owns objective, dependency graph, current successor,
and final reconciliation only. Each successor owns its current state, Red/Green
tasks, one approval state, evidence, release subset, and rollback/runbook.

Before SA-110, the smallest acceptable correction is:

1. synchronize current I-VSD text/mappings;
2. correct source-free scope statements;
3. remove the upstream waiver;
4. encode worktree/PR ownership and approval boundaries;
5. add Worst Break ownership without pretending it is already evidenced;
6. bind fresh hashes, CTO review, and user approval.

Do not add compatibility shims, weaken a Tier 0/1/2 gate, approve stale release
evidence, or start implementation while those corrections are pending.

## Missing Evidence

The missing evidence is appropriate for pre-implementation status but blocks
claims and later phase entry: exact package graphs and licenses; runtime
catalogue/defaults; browser request/storage/CSP proof; desktop ACL/filesystem
matrix; measured scale thresholds; enrollment/replay/provider-coordinate
proof; multi-provider migrations and tenant races; privacy custody/erasure
rehearsal; payment/provider/ledger reconciliation; stakeholder comprehension;
platform accessibility; package install/upgrade; signing/notarization; SBOM,
provenance, and reproducibility.

None of this absence justifies weakening a gate. It determines which successor
can start and which target/capability remains disabled.
