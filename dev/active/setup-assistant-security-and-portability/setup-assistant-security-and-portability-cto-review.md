<!-- ABOUTME: Exact-revision Senior CTO review of the BCL-only Setup Assistant successor-A strategy. -->
<!-- ABOUTME: Approves package-free SA-120 scaffolding after user approval while blocking GUI packages and later successors. -->

# Senior CTO Feedback

Last Updated: 2026-08-31 Europe/Brussels

## Review Metadata

- **Review mode:** Read-only
- **Review scope:** Successor A `setup-assistant-foundation-offline` only,
  specifically the BCL-only SA-120 strategy and its A1-A4 architecture boundary
- **Reviewed plan:**
  [setup-assistant-security-and-portability-plan.md](setup-assistant-security-and-portability-plan.md)
- **Reviewed plan revision:**
  `sha256:55bd82962d6813312656dd1d2c1b299389ee24f1f0fceb6ef746e9f1b27b3dfb`
- **Reviewed tasks:**
  [setup-assistant-security-and-portability-tasks.md](setup-assistant-security-and-portability-tasks.md)
- **Reviewed tasks revision:**
  `sha256:6b1e401bb021086ebbce15a99698f78224b29c41757810d1582a759dc37b0e58`
- **Reviewed context:**
  [setup-assistant-security-and-portability-context.md](setup-assistant-security-and-portability-context.md)
- **Reviewed context revision:**
  `sha256:8368af4681bae70dc0b344d76ac84ecb99057c3cf69a36c5f88e27e5e5c4ea4d`
- **Reviewed clean-room evidence:**
  [setup-assistant-security-and-portability-clean-room-evidence.md](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-clean-room-evidence.md)
- **Reviewed clean-room evidence revision:**
  `sha256:6145403b66c97950c28e3e58ed306572fc3046ebe7e4df8635f2f63f92407821`
- **Reviewed dependency evidence:**
  [setup-assistant-security-and-portability-dependency-evidence.md](../../zarchive/setup-assistant-security-and-portability/setup-assistant-security-and-portability-dependency-evidence.md)
- **Reviewed dependency evidence revision:**
  `sha256:5fd00f8b63648bcccaf8f22a37c834eb10c1fc56480263ebc332b3622b26bf41`
- **Aggregate reviewed-input revision:**
  `sha256:d2bbba40455c013e20883ab6202f84411bb05f2c20f6060a9e73095f44a8e4b1`
- **Reviewed I-VSD report:**
  [i-vsd-setup-assistant-security-and-portability.md](../../../islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md)
- **Reviewed I-VSD revision:**
  `sha256:f1eb76aa007f83004404f85f32dc9894f2664c639eb5f9a3037ce6b149229e06`
- **I-VSD freshness:** `current`
- **I-VSD disposition:** `plan-aligned`
- **I-VSD reviewed-input revision:**
  `sha256:d2bbba40455c013e20883ab6202f84411bb05f2c20f6060a9e73095f44a8e4b1`
- **Stable I-VSD mappings:** `IVSD-F001`-`IVSD-F046` and
  `IVSD-M001`-`IVSD-M046` preserved
- **Decision:** Approve
- **Decision scope:** Successor A and its BCL-only SA-120 strategy only
- **User approval:** Not granted by this review; fresh exact-revision user
  approval is required before SA-120 scaffolding resumes
- **Implementation state:** SA-110 focused Red is complete; SA-120 is open;
  no Setup production code, package pin, restore, lock, project scaffold, or
  generated ratchet is claimed

The plan, tasks, context, and clean-room evidence retain the pre-revalidation
lifecycle statement that I-VSD was stale. The current I-VSD report explicitly
supersedes that state and binds those exact five input files through the
aggregate digest above. This review therefore uses the report's authoritative
`current` / `plan-aligned` disposition. It does not reinterpret the obsolete
CTO or user approvals as current authority.

## Executive Verdict

Approve the exact BCL-only successor-A strategy. The dependency revision is a
material improvement, not a downgrade: A now has no new product package graph,
keeps deterministic machine commands separate from a bounded human terminal
wizard, and reserves GUI/browser/desktop activation for an independently gated
successor. SA-120 may resume package-free scaffolding only after the user
explicitly approves this exact revision. Terminal.Gui 2.4.17 and its complete
24-package graph remain blocked; A must pin, reference, restore, lock, vendor,
or publish none of them. A must likewise pin or restore no Avalonia package and
no replacement GUI/TUI package. Presentation, Browser, and Desktop projects may
exist in A only as package-free, disabled, non-shipped contract shells. They
prove architecture boundaries, not UI, target, accessibility, support, or
release capability. B-G receive no authority from this decision.

**Decision: Approve.** This is technical approval for successor A and the
BCL-only SA-120 strategy only, conditional on fresh exact-revision user
approval before scaffolding resumes.

## Review Lifecycle And Prior Decisions

| Review | Bound revision | Decision | Continuing effect |
|---|---|---|---|
| First review, 2026-08-31 | Plan `1c5048a5...`; tasks `7460b223...` | **Split before approval** | Required independently gated successors and review-sized PR slices; approved no implementation. |
| Follow-up review, 2026-08-31 | Plan `8b4d4600...`; tasks `9f2e5e9a...`; I-VSD `a4e96fd1...` | **Approve - successor A only** | Authorized the then-current A direction after user approval; now revision-obsolete because the package/implementation strategy changed. |
| Current review, 2026-08-31 | Exact hashes in Review Metadata; aggregate `d2bbba40...` | **Approve - BCL-only successor A only** | Re-establishes technical readiness for A's package-free strategy. Fresh exact-revision user approval remains mandatory. |

The prior user-approval artifact is also revision-obsolete. Neither prior CTO
approval nor prior user approval can be inherited by this revision or by B-G.

## 3-Dimensional Scorecard

| Dimension | Score | Status | Key finding |
|---|---:|---|---|
| **Completeness** | 5/5 | Pass | The exact revision preserves 41 SA tasks, 12 phase gates, Scenarios 3.1-3.15, all 46 I-VSD finding/mitigation pairs, and explicit successor/evidence ownership. |
| **Correctness** | 4/5 | Pass | A's package-free dependency decision is fail-closed and testable. SA-110 Red exists; restore, locks, ratchets, terminal behavior, and release claims correctly remain unproved implementation evidence. |
| **Coherence** | 5/5 | Pass | `Event.Wire.Contracts <- Event.Setup.Core <- Event.SetupAssistant.Cli` preserves inward dependency direction; disabled shells expose future adapter boundaries without activating presentation capability. |

### Detailed CTO Scorecard

| Area | Score | Assessment |
|---|---:|---|
| Strategic fit | 5/5 | A lowers the self-hosting barrier with an offline, independently useful product and no GUI-framework supply-chain dependency. |
| I-VSD | 5/5 | Current, plan-aligned, aggregate-bound, authority-limited, and complete through F046/M046; prior approvals are explicitly obsolete. |
| Socratic stress test | 4/5 | Secret leakage, dependency ingress, terminal process surfaces, rollback, tenant boundaries, resource limits, and the later sovereign Worst Break have named controls and owners. |
| Architecture integrity | 5/5 | Pure shared contracts and Core own deterministic behavior; CLI/terminal and future presentation targets remain adapters. |
| Security/trust | 5/5 for A | A has no live target/provider/tenant authority; machine mode is non-secret and interactive secret handling remains TTY-only and value-safe. |
| Multi-tenancy | 5/5 for A | Offline package scope cannot become target authority; D-F remain independently blocked on server-side tenant evidence. |
| Dependency/license | 5/5 | Incomplete provenance/notices and unproved publish exclusions fail closed; no scanner, signature, isolated component fact, or top-level license is treated as approval. |
| Self-hosting/operations | 4/5 | BCL-only operation avoids a new runtime service and package graph; exact command, terminal, lock, RID, and release evidence remains correctly assigned to implementation. |
| Maintainability | 5/5 | The deletion-friendly disabled shells keep future composition roots explicit without duplicating Core behavior or introducing compatibility shims. |
| Accessibility | 4/5 | A requires a truthful terminal support matrix and does not claim GUI parity; B retains independent accessibility gates. Runtime evidence is still absent by design. |
| Testability | 5/5 | High-risk boundaries use focused public-contract Red tests before production behavior, prohibit sleeps/mock mirrors/scraping, and keep phase verification bounded. |
| Sequencing/right-sizing | 5/5 | A remains four serial PR slices (3/3/4/3 tasks); B-G are separate successors rather than hidden continuations. |
| Dev-doc quality | 4/5 | The exact artifacts are detailed and resumable. Their stale-status prose is a lifecycle handoff superseded by the aggregate-bound current I-VSD report, not an implementation ambiguity after this review. |

## Right-Sizing Decision

The umbrella still matches all four right-sizing symptoms, so it remains a
non-executable program index. Approval is viable only because implementation
authority is split:

1. **A1:** SA-110-SA-130, architecture/dependency/CI foundation;
2. **A2:** SA-210-SA-230, wire-contract cutover and pure Core;
3. **A3:** SA-310-SA-340, catalogue/dotenv/offline workflows;
4. **A4:** SA-410-SA-430, deterministic CLI and BCL terminal wizard.

A must remain these serial, independently reviewable slices. Combining A1-A4
into one mega-PR would violate this approval. B-G remain separate approval
units. SA-120 itself is XL but coherent: it establishes one package-free graph,
ten tested project boundaries, lock/ratchet governance, and dependency checks
without implementing product behavior.

## Top Risks

### 1. CRITICAL - Blocker if introduced - Blocked package ingress

**Why it matters:** A single central pin, transitive restore, lock entry,
vendored asset, or publish payload would invalidate the exact dependency and
outbound-license decision.

**Evidence:** The dependency evidence blocks Terminal.Gui 2.4.17's indivisible
24-package graph because TextMateSharp.Grammars 2.0.4 lacks complete component
provenance/notices. Avalonia Desktop/Browser remain blocked by unresolved
native binary/component mapping and unproved `Avalonia.Remote.Protocol` publish
absence.

**Required control:** SA-120 must stop if Terminal.Gui, any member restored for
that feature, Avalonia, a replacement GUI/TUI package, or a package exception
enters A. Do not suppress, waive, partially restore, or rely on signature or
scanner success.

### 2. CRITICAL - Blocker if introduced - Contract shells become fake capability

**Why it matters:** Empty projects can mislead users and release tooling if
they are activated, packaged, tested as runtime targets, or cited as support
evidence.

**Evidence:** Plan Sections 0.1, 5.4, and Phase 1; SA-120; clean-room and
dependency evidence all define the Presentation, Browser, and Desktop projects
as package-free, disabled, non-shipped boundaries only.

**Required control:** Shells may contain only the minimum contracts and disabled
composition needed to enforce the approved dependency graph. Any functional UI,
target activation, package, publish path, accessibility claim, support matrix
entry, or release evidence belongs to successor B after fresh approval.

### 3. WARNING - Major - BCL terminal safety and accessibility remain evidence gates

**Why it matters:** Removing Terminal.Gui removes its graph but also makes
terminal behavior fully product-owned. TTY detection, echo restoration,
signals, resize, recording/scrollback, keyboard/non-color behavior, Unicode/RTL,
and screen-reader limitations cannot be assumed from BCL use.

**Required control:** SA-410-SA-430 must preserve focused adversarial contracts
and publish only evidence-backed terminal support. A narrow or inaccessible
terminal cannot become the only required path, and no GUI claim may be inferred.

There is no present CTO blocker to package-free SA-120 scaffolding after fresh
exact-revision user approval.

## What I Would Keep

- The package-free `Event.Wire.Contracts` and BCL-only `Event.Setup.Core` graph.
- Handwritten deterministic machine commands with versioned JSON and stable
  exits.
- A separate bounded human terminal wizard rather than terminal-screen
  automation or secret-bearing machine surfaces.
- Package-free disabled presentation shells as explicit, non-capable adapter
  boundaries.
- One clean pre-v1 wire-contract cutover with no aliases, type forwards,
  duplicate codecs, or compatibility shims.
- Fail-closed clean-room, license, vulnerability, lock, SBOM, provenance,
  accessibility, and support evidence gates.
- Independent successor ownership for GUI, browser, desktop, live authority,
  application data, payments, and release/agent claims.

## What Must Change Before Implementation

1. A fresh user approval must bind at least the exact plan/tasks revisions and
   the current aggregate/I-VSD revision in Review Metadata. This review does
   not grant or infer that approval.
2. Terminal.Gui 2.4.17 and its complete 24-package graph remain blocked. No
   direct, transitive-for-feature, partial-profile, vendored, lock, publish, or
   exception path is allowed.
3. A pins/restores no Avalonia package. The conditional research fact about
   non-shipped compile scaffolding is not a selection or approval.
4. No replacement GUI/TUI package or package-policy exception enters A.
5. Presentation, Browser, and Desktop projects stay package-free, disabled,
   non-functional, non-shipped, and absent from support/release claims.
6. Selected test projects may use only already-approved repository test
   infrastructure; that does not authorize a new product dependency.
7. Locked restore, point-in-time vulnerability audit, repository dependency
   license validation, exact locks, and both SA-110 fail-closed ratchets must
   exist and pass before SA-120 is complete.
8. Any changed graph, shell activation, provenance/notice gap, vulnerability,
   signature failure, publish-role uncertainty, or approval revision stops the
   task and requires the named re-entry sequence.

After exact-revision user approval, SA-120 may resume directly from the existing
SA-110 focused Red. It does not need a new SA-110 production-code phase, and no
production code is currently claimed.

## Dev-Docs Quality Assessment

### `setup-assistant-security-and-portability-plan.md`

The plan evaluates the changed strategy rather than the obsolete package
revision. A is BCL-only; B is framework-neutral; shell activation and all later
successors have explicit independent gates. The plan remains large only as the
canonical umbrella and is executable through successor/PR slices, not as one
approval unit.

### `setup-assistant-security-and-portability-context.md`

The context accurately records SA-110 Red, SA-120 open, no scaffolding, the
blocked graphs, unrelated build baseline, and the next approval sequence. Its
pre-revalidation stale-status text is superseded by the current aggregate-bound
I-VSD report and this exact review.

### `setup-assistant-security-and-portability-tasks.md`

The ledger preserves 41 unique implementation tasks and 12 phase gates. SA-120
has explicit package-free acceptance and stop conditions; A's CLI/terminal work
is separated from B's GUI/browser/desktop work. Focused tests assert public
invariants rather than timing luck, internal mock calls, or prose/source text.

### Clean-room and dependency evidence

The evidence is source-free and decision-complete for planning. It records
functional/package facts without retaining third-party expression, independently
derives the BCL wizard from repository-native contracts, and correctly treats
missing graph provenance or notices as blocking. It claims no restore, lock,
SBOM, publish, runtime, security, accessibility, or release proof.

## Islamic Value-Sensitive Design (I-VSD) Assessment

The current report at
`sha256:f1eb76aa007f83004404f85f32dc9894f2664c639eb5f9a3037ce6b149229e06`
is `current` / `plan-aligned` for aggregate reviewed-input revision
`sha256:d2bbba40455c013e20883ab6202f84411bb05f2c20f6060a9e73095f44a8e4b1`.
It preserves exactly 46 stable findings and 46 stable mitigations,
`IVSD-F001`-`IVSD-F046` mapping one-to-one to
`IVSD-M001`-`IVSD-M046`. Plan Section 9 retains all mappings through Scenarios
3.1-3.15 and SA-110-SA-1250.

F013/M013, F015/M015, F022/M022, F029/M029, and F035/M035 directly govern the
changed dependency strategy: incomplete provenance fails closed, shells are not
support evidence, replacement requires adversarial proof, deterministic CLI
and bounded terminal access remain first-class, and terminal accessibility is
reported separately. The report explicitly states that prior CTO/user approvals
are obsolete and grants no implementation authority itself.

No security, accessibility, legal, privacy, payment, release, scholarly, or
user gate is weakened by this CTO decision. Any material package/framework
selection, shell activation, target claim, authority/default/data/payment
change, or mapped mitigation change triggers the report's refresh rules.

## Socratic Stress-Testing & "Worst Break" Audit Findings

### The Worst Break Catastrophic Scenario Check

The umbrella's catastrophic failure remains a stale/replayed capability plus
tenant mismatch racing payment finalization/refund and producing cross-tenant
money mutation or false completion. It is not an A risk because A has no live,
tenant, provider, persistence, or money authority. Successor F slice F1 still
owns the dedicated deterministic Red before SA-1140 against the real owning
database/provider contract, with zero cross-tenant rows, zero provider/outbox
money intent, unchanged checked balances, one value-free conflict receipt, and
zero PII/secret logs. This review neither executes nor approves F.

For A, the worst plausible break is secret disclosure through terminal process
surfaces or an accidentally introduced package/publish dependency. SA-110's
architecture/security Red plus SA-120 and SA-410-SA-430 place those boundaries
before production behavior and fail closed on the relevant ingress paths.

### Grill-Me Stress-Test Findings

| Challenge | Finding | Decision |
|---|---|---|
| Rollback | SA-120 can remove unshipped shells and generated scaffolding; it must never retain a blocked pin or weaker fallback. | Pass |
| Tenant authority | A has none; artifact scope is not target authority. D-F retain server/HAL gates. | Pass for A; later successors unapproved |
| Resource bounds | Core/CLI use canonical bounded inputs; measured larger profiles remain successor C. | Pass for A |
| Operator clarity | Machine versus human terminal surfaces, secret/no-secret output, readiness, and unsupported capabilities are distinct. | Pass; runtime evidence remains required |
| Dependency failure | Unknown provenance, notices, advisory state, integrity, or publish role stops the task. | Pass |
| Accessibility | BCL use creates no parity claim; terminal evidence and independently approved alternatives remain mandatory. | Pass with release gate |

## Enterprise / Self-Hosting Assessment

A is an honest offline product boundary: it adds no service, database,
provider, remote health endpoint, hosted dependency, telemetry, or live
credential authority. BCL-only operation reduces bootstrap and supply-chain
burden while preserving deterministic catalogue, manifest, legal, dotenv, and
CLI workflows. Required environment keys/defaults/activation rules, generated
ownership, command schema, local doctor codes, terminal support, locks, RIDs,
and upgrade/removal guidance remain implementation/release evidence. Disabled
presentation shells cannot be advertised as self-hosting capability.

## Security and Multi-Tenancy Assessment

- Setup Core remains network-, persistence-, provider-, telemetry-, AI-, and
  server-layer-free.
- Portable artifacts remain non-secret and carry no target, provider, tenant,
  operational, PII, or money authority.
- Machine CLI mode is non-secret. Human secret completion is interactive
  TTY-only and excludes arguments, environment, captured stdin, stdout/stderr,
  history, clipboard, logs, and support evidence.
- No user-interface project enforces authorization or synthesizes HAL authority.
- B-G retain fresh Tier 0/1/2, I-VSD, CTO, user, legal, security, privacy,
  accessibility, provider, and release gates as applicable.

## Architecture and Maintainability Assessment

The approved A dependency direction is:

```text
Event.Wire.Contracts (package-free)
    <- Event.Setup.Core (BCL only)
        <- Event.SetupAssistant.Cli (BCL machine CLI + human terminal adapter)
```

`Event.SetupAssistant`, Browser, and Desktop are disabled outer-boundary
contracts only. This is an honest Clean Architecture seam because it enforces
future dependency direction while containing no UI behavior or release claim.
It would become fake capability only if code, packages, publish targets,
support metadata, or tests represented it as functional. The plan and SA-120
explicitly prohibit that transition.

The design avoids shallow duplicated validators and serializers: Core owns
portable rules and deterministic output, while adapters own process, terminal,
filesystem, browser, or UI concerns. The pre-v1 contract move deletes old
owners without aliases or compatibility baggage.

## Accessibility Assessment

Approval does not equate BCL console primitives with accessible UI. A must
publish only measured terminal behavior and limitations across keyboard,
non-color status, width/resize, signals, echo restoration, Unicode/RTL,
recorders/scrollback, screen readers, and supported terminal environments.
Successor B preserves independently approved GUI alternatives but has no
framework, target, or accessibility authority yet. Package-free shells are not
accessibility evidence.

## Dependency And License Assessment

The package-free A strategy is the strongest available outbound-license and
maintenance result. Terminal.Gui's top-level metadata cannot cure incomplete
mandatory grammar provenance/notices. Avalonia signature integrity, ANGLE's
resolved license, BuildServices conditionality, or telemetry opt-out cannot
cure unresolved runtime component mapping or prove Remote Protocol absence.
No exception is warranted because A needs neither graph.

Successor B starts from outcomes, not Avalonia. It may select a different
provenance-complete framework or reconsider Avalonia only with new authoritative
binary/component/license/notice and exact publish evidence, fresh vulnerability
and outbound review, current I-VSD, fresh CTO review, and exact-revision user
approval. A grants B no package precedent.

## Breaking-Change Position

Use one clean pre-v1 wire-contract cutover. Delete obsolete owners, codecs,
schemas, tests, and namespaces after callers converge. Do not add type forwards,
aliases, dual serializers, command aliases, compatibility adapters, or a
package-backed fallback. A failed gate delays or removes the unshipped slice.

## Implementation Sequencing I Recommend

1. Treat existing SA-110 focused Red as the active invariant baseline.
2. After exact-revision user approval, execute SA-120 package-free scaffolding,
   locks, ratchets, locked restore, vulnerability audit, license validation,
   and focused architecture Green work.
3. Complete SA-130 CI/source-output governance and Phase 1 verification.
4. Continue A2, A3, and A4 serially; do not begin B or another successor.
5. Route each later successor through its own evidence, I-VSD, CTO, and user
   gates before changing a disabled boundary.

## SA-110 And Baseline Evidence

SA-110 is focused Red evidence, not production implementation. Its Release
selector ran six tests: three prerequisite/verifier tests passed and three
failed only for the absent ten Setup projects, browser capability JSON, and
frozen-contract baseline JSON. SA-120 remains unchecked. No Setup source
project, test project shell, package pin, lock, restore, generated ratchet, or
production code is claimed.

The pre-implementation Release build baseline failed before Setup implementation
with 10 `CS0103` errors in unrelated concurrent work at
`tests/Event.Domain.UnitTests/ValueObjects/AtprotoDidTests.cs` and reported 1026
pre-existing analyzer warnings. That is not Setup Green evidence and must not be
misreported as an SA-120 failure or success.

## Verification Bar

Before SA-120 can be marked complete:

- all five source and five focused test projects exist with one lock each;
- product projects use BCL plus existing package-free Wire Contracts only;
- presentation/Browser/Desktop shells are disabled and non-shipped;
- no Terminal.Gui, Avalonia, replacement UI package, or exception appears in
  central pins, references, restore, locks, assets, or publish output;
- both SA-110 generated fail-closed ratchets exist;
- locked restore, point-in-time vulnerability audit, dependency-license policy,
  focused architecture contracts, one Release build, and the Phase 1 selected
  architecture test pass with honest evidence.

No browser, desktop, Docker, Aspire, live service, Playwright, manual runtime,
or unrelated .NET verification is part of this Markdown review.

## PR Split Recommendation

Maintain A1-A4 as four serial PRs. Do not combine them and do not create B-G
implementation branches under A's authority. The umbrella remains useful for
traceability, but each successor remains independently approved and can remain
permanently disabled without creating a shim or weakening another boundary.

## Operator Runbook Requirements

For an eventual A release, document exact supported RIDs/terminals, installation
and removal, lock/SBOM identity, environment catalogue keys/defaults/activation
and restart behavior, no-secret versus secret-output sensitivity, command and
exit contracts, TTY/recording/accessibility limitations, generated-file
ownership, local diagnostics, upgrade/reset behavior, and safe disablement. Do
not claim GUI, browser, desktop, live, migration, payment, signing, package, or
accessibility support from shell or scaffolding evidence.

## Recommended Plan Rewrite

None. This review intentionally changes only the CTO artifact. The exact plan,
tasks, context, clean-room evidence, dependency evidence, and current I-VSD
report are decision-complete for BCL-only successor A. The next authority
transition is fresh exact-revision user approval; after it is recorded, SA-120
may resume package-free scaffolding from the existing SA-110 Red.

## Missing Evidence

Implementation and release proof remains intentionally absent: SA-120 project
shells/locks/ratchets and restore/audit results, Core behavior, CLI schema,
terminal security/accessibility matrix, generated catalogue, packages, SBOM,
signing, runtime targets, and support claims do not yet exist. This absence is
truthful and does not block package-free scaffolding after user approval. It
does block completion/release claims and grants no authority to B-G.