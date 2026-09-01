<!-- ABOUTME: Revision-bound Senior CTO review of successor-B candidate B0 and its restricted graph/publish probe. -->
<!-- ABOUTME: Conditionally approves probe readiness while preserving user, release, desktop, secret, and shipping gates. -->

# Senior CTO Feedback — Setup Assistant Presentation Targets B0

> **SUPERSEDED AND NON-AUTHORIZING — HISTORICAL RECORD ONLY**
>
> B1 replaced B0 before user approval or probing. Every conditional CTO
> permission below is revoked as execution authority and cannot be transferred,
> refreshed, or used as precedent for B1.

Last Updated: 2026-08-31 Europe/Brussels

## Review Metadata

- Review mode: Read-only; this review is the only created artifact.
- Binding ID: `setup-assistant-presentation-targets-b0-20260831`.
- Binding artifact revision: `sha256:d061551fdab2ad452adf2d9802aa05724e490bab8148a6a87e7f903abf44a836`.
- Reviewed plan revision: `sha256:d7483c2d20da9bdbd23e826bf1ee56f431a8d6fa23019f2a5506171298a56d27`.
- Reviewed tasks revision: `sha256:3110628d5b3106ba266baf0716ae7699e974edf5d1b242230e66427d5a7e01e1`.
- Reviewed context revision: `sha256:e783ed6de19368f4d3cdef80cbfd2f164c7c3f844dd0c175cfc0163ba729748b`.
- Reviewed intake revision: `sha256:3ce321d693429eb602f7d2f3436e4cf3bf5c4d6326675956051784caaac4a63b`.
- Reviewed I-VSD report: [`i-vsd-setup-assistant-presentation-targets-b0.md`](../../../islamic-value-sensitive-design/i-vsd-setup-assistant-presentation-targets-b0.md).
- Reviewed I-VSD revision: `sha256:40c38655e38555406478268b737836aa04a928832957f96ae3c960a200335b4b`.
- I-VSD freshness: Current; `plan-review` / `plan-aligned` for this exact binding.
- Current user approval: Never granted.
- Decision lifecycle: **Superseded before probe; no current authority**.

The four bound artifact digests, binding digest, and I-VSD digest were recomputed
from the workspace and match the values above. Any material change named by the
binding or I-VSD refresh triggers invalidates this decision.

## Executive Verdict

Candidate B0 is the smallest legitimate successor-B evidence path currently
available: it activates no desktop framework, no secret-capable browser path,
no server/BFF/API authority, and no component library. Shared semantic Razor
above `Event.Setup.Core`, followed by a standalone static Blazor WebAssembly
host, preserves the inward dependency direction and avoids speculative desktop
adapter baggage in the package graph. The exact graph is not yet proved; B-020
is correctly an evidence-producing, fail-closed probe rather than an
implementation or release gate.

The bound documents contain two ambiguities that must not be carried into probe
claims: they record both the initial SA-510 5-pass/3-fail run and the later
accepted 7-pass/1-fail confirmation, and they use “zero network” language that
would literally exclude the same-origin bootstrap retrieval required by a
served WebAssembly application. These do not require a triad rewrite before the
restricted probe, provided the operator follows the corrections below and does
not claim B0 implementation, support, or release readiness.

**Historical decision only: superseded before probe.**

This decision no longer advances any binding or action. It grants no probe,
implementation, user, release, support, or shipping authority.

## 3-Dimensional Scorecard

| Dimension | Status | Key finding |
|---|---|---|
| **Completeness** | Pass for B-020; Warning beyond it | Graph, lock, audit, provenance, publish, rollback, and exclusion gates are present. Runtime security, accessibility, legal-template, and support evidence correctly remain later gates. |
| **Correctness** | Warning | SA-510 is a useful source-free invariant Red, but its run history must be reported as two observations, and static publish inspection cannot prove later runtime no-network/no-storage behavior. |
| **Coherence** | Pass | `Event.Setup.Core -> Event.SetupAssistant -> Event.SetupAssistant.Browser` is Clean Architecture-compatible; Desktop, product Blazor, API, Application, Domain, Persistence, and live authority remain outside the graph. |

## Required Pre-Probe Corrections

These are execution constraints recorded by this review. They do not authorize
editing any bound artifact. If satisfying one requires a material change to the
bound scope, graph, capability matrix, package, SDK/TFM, rollback, or I-VSD
mapping, stop and create a new binding instead.

1. **Preserve the accepted SA-510 evidence chronology.** Record the initial
   selector as 8 total, 5 passed, 3 failed, 0 skipped, 457 ms. Record the later
   accepted lead confirmation separately as 8 total, 7 passed, 1 failed, 0
   skipped, 589 ms, with the sole remaining aggregate covering the absent
   shared owner and selected-framework adapter. Do not merge those observations
   or claim SA-510 Green. B-030 still owns the next full eight-test execution.
2. **Keep release capability disabled during B-020.** B-020 may evaluate the
   Razor/WebAssembly SDK and package graph and publish it into ignored probe
   output, but `SetupTargetEnabled` and
   `eng/setup-assistant/generated/browser-release-capabilities.json` remain
   false. No generated capability ratchet, CI file, solution file, product
   component, test, or public asset is changed by this probe. A successful
   probe proves the candidate graph only; it does not activate a releasable
   browser capability.
3. **Do not negotiate a different target during restore.** The bound target is
   `net10.0` under SDK `10.0.302`; record the resolved WebAssembly runtime
   identifier/runtime packs separately. If the SDK requires another TFM, an
   unapproved workload installation, another direct package, or a change to
   `Directory.Packages.props`, stop. Do not adapt the candidate in place.
4. **Use repository-central package ownership.** The only new direct product
   package identity may be
   `Microsoft.AspNetCore.Components.WebAssembly`, resolved by the existing
   central `10.0.10` pin. The shared project may add only the
   `Microsoft.AspNetCore.App` framework reference and `Event.Setup.Core`
   project reference. Any other direct product package or any blocked
   transitive/build/workload/publish identity fails the probe.
5. **Bound restore side effects.** Force evaluation may update only the shared
   and browser locks, after which both projects must restore in locked mode.
   Capture pre-probe lock digests and exact direct, transitive, framework,
   runtime-pack, workload, build, and publish inventories. A lock change outside
   those two projects, audit-source failure, advisory, deprecation, signature
   failure, unknown provenance/license/NOTICE obligation, or missing SBOM role
   is a stop condition, not a waiver candidate.
6. **Interpret the network exclusion precisely.** Same-origin, read-only GETs
   needed to load the checked publish artifact's HTML, framework bootstrap,
   assemblies, ICU/resource data, and bundled assets are transport mechanics,
   not product network authority. Every application-initiated API/provider
   request, non-local origin, remote font/asset, beacon, reporter, CSP report,
   WebSocket, EventSource, form submission, update check, service worker, or
   dynamic script path remains forbidden. No claim of runtime zero-network
   behavior may be made from B-020 static inventory alone.
7. **Treat every published byte as public.** Reject source maps, secrets,
   credentials, tenant identifiers, internal hostnames, private endpoints,
   development-server assets, reporters, service-worker/PWA artifacts, or
   unexpected assemblies. B-020 may establish absence in the inspected
   artifact; runtime request and storage assertions remain B-050/B-080 evidence.
8. **Exercise rollback exactly as designed on any failure.** Restore both
   project files and lock files from captured pre-probe bytes using file edits,
   remove only probe-created ignored output, preserve failure evidence, and
   leave all target flags false. No destructive Git operation, fallback
   package, route/adapter shim, reduced target, or undocumented exception is
   permitted.

## Technical Readiness And Sequencing

The approval sequence is correct and fail-closed:

1. Exact binding and current I-VSD review.
2. This revision-bound CTO decision.
3. Exact-revision user approval naming binding
   `setup-assistant-presentation-targets-b0-20260831`.
4. Restricted B-020 graph/lock/audit/publish probe under the corrections above.
5. Stop for review of the resulting evidence before B-030 or any retained
   capability activation is inferred.

B-020 is the right first executable action because restore and publish are the
only reliable ways to discover the SDK-resolved package/runtime/publish graph.
Implementing shared owners or browser UI before that evidence would invert the
risk sequence. Conversely, desktop research does not need to block this probe:
Desktop has no dependency in the selected runtime graph and remains a disabled,
package-free shell.

## Clean Architecture And Framework-Neutral Contract

The candidate graph has the correct dependency direction. `Event.Setup.Core`
continues to own catalogue activation, validation, relevance, sensitivity,
readiness, serialization, digests, diff/coverage, workflow transitions, and
legal Markdown. Shared presentation may own semantic rendering, focus,
localization surfaces, navigation, and typed user intent. Browser may own only
static hosting and user-initiated download of Core-produced no-secret bytes.

The accepted SA-510 tests are high-value contract tests rather than mock
mirrors. They use immutable source-free vectors, negative fixtures, direct Core
result/byte identity, fail-closed state transitions, public-closure inspection,
and assembly-reference checks. They do not assert repository call counts or
framework mechanics. The deliberate early return in
`FinalPublicSeamIsImmutableClosedFrameworkNeutralAndCoreOnly` is bounded by the
separate prerequisite Red; once all required product types exist, the final
public seam checks become active.

The test contract does name future `Desktop` and `Secret` enum/capability
shapes. That is acceptable only as a framework-neutral closed contract inherited
from SA-510; it is not approval to implement a desktop adapter, admit a browser
secret, persist secret state, or add a desktop package. B0 remains legitimate
because its selected package and runtime graph contains neither capability. Any
production behavior that makes those dormant shapes reachable is scope drift
and invalidates this review.

HAL, CQRS, transactional outbox, tenant query filters, and BFF authorization are
non-applicable to this bounded offline presentation graph: B0 introduces no API,
resource mutation, persistence, tenant data access, or server authority. They
must not be added “for future use.”

## Security, Privacy, Accessibility, And Dependency Conditions

### Worst Break

The catastrophic failure is publishing a world-readable bundle containing a
secret/internal deployment fact or a hidden remote reporting/provider path,
then describing it as local and no-secret. B-020 addresses the earliest
provable portion through exact publish inventory and fail-closed graph review.
B-050 and B-080 must later supply machine-consumed runtime request, storage, DOM,
automation metadata, console, and artifact assertions before implementation or
release acceptance. Static inventory alone is not sufficient.

### Browser trust boundary

The browser receives no bearer token, setup secret, API key, tenant authority,
provider binding, or privileged header. It has no API/BFF path and no local,
session, IndexedDB, cookie, Cache API, service-worker, or filesystem persistence
path. Generated no-secret output remains an explicit user-initiated download of
Core-produced bytes. Empty Core-produced secret placeholders must remain
truthfully `Incomplete`; they are not secret handling and cannot be presented as
ready configuration.

### Accessibility and localization

Semantic Razor is a credible accessibility foundation, not evidence of browser
or assistive-technology support. Keyboard completion, focus restoration,
error association/summary, announcements, 200% zoom/reflow, contrast,
non-color status, reduced motion, Arabic/RTL reading and tab order, and
representative browser/AT evidence remain B-060/B-090 gates. Resources must be
bundled. No parity/support claim is approved by B-020 or by this review.

### Dependency and legal conditions

Microsoft ownership, an MIT label, package signature success, a clean scanner,
or successful publish is individually insufficient. The exact closure must map
package/runtime/build components to provenance, outbound-compatible licenses,
NOTICE/source obligations, SBOM roles, telemetry posture, advisories, and the
published inventory. Legal editor/template behavior remains outside B-020;
substantive templates require the authority and provenance gates in B-070 and
I-VSD `IVSD-F052/M052`.

## Observability, Network, Storage, And Operations Exclusions

B0 deliberately adds no telemetry, logs, metrics exporter, tracing provider,
health check, background worker, database, migration, cache, queue, API,
operator control plane, or remote dependency. Those omissions are correct for a
static no-secret artifact, not enterprise-readiness gaps to fill in this probe.
Probe evidence itself is the operator diagnostic surface: exact command inputs,
SDK/TFM/RID, graph and lock digests, audit/signature/license results, SBOM and
NOTICE mapping, publish inventory/digests, stop reason, and rollback result.
Evidence must contain no secret-bearing raw log.

Self-hosting remains straightforward at this stage: no service, environment
variable, credential, DNS, TLS, database, backup, migration, or runtime health
contract is introduced. Static-host origin integrity, release identity,
reproducibility, CSP, incident response, deployment, and support instructions
remain release blockers, not prerequisites for discovering the candidate graph.

## I-VSD Assessment

The reviewed report is current and plan-aligned for the exact binding. Stable
successor findings `IVSD-F047` through `IVSD-F053` map one-to-one to
`IVSD-M047` through `IVSD-M053` and to named B0 sections/tasks. Existing
`IVSD-F001` through `IVSD-F046` are explicitly mapped, deferred, or classified
non-applicable. The report correctly preserves public-origin truth, human
agency, accessibility/support honesty, legal draft authority, exact dependency
provenance, reversible activation, and the distinction between evidence and
certification.

No religious-legal conclusion is made. Qualified legal review remains required
for substantive legal templates and claims; qualified Sunni scholarly review
is required only if later behavior or copy introduces a religious-legal
conclusion. This CTO review does not grant either authority.

## Right-Sizing And Breaking-Change Position

The full B0 ledger has nine major tasks, but this approval is intentionally
limited to one independently reviewable risk slice, B-020. It does not combine
migration, API churn, persistence, and UI enablement; none of those layers is in
scope. The backend/API-independent-shipping symptom is also absent because B0
consumes already-Green Core contracts and adds no backend slice. A mandatory PR
split is therefore not triggered for the probe.

No compatibility alias, fallback UI package, deprecated route, adapter shim, or
legacy target is justified in this greenfield repository. A failed B0 probe
returns to disabled package-free shells. Desktop remains disabled until a
separately bound candidate proves its complete native graph, accessibility,
protected-write behavior, and target matrix.

## Evidence Limits

Verified in this review:

- Exact binding and artifact digests.
- Current disabled/package-free shared, browser, and desktop project shells.
- Central package pin for
  `Microsoft.AspNetCore.Components.WebAssembly` `10.0.10`.
- SDK pin `10.0.302` and repository lock/central-package policy.
- Accepted SA-510 test source, negative fixtures, Core parity seam, and recorded
  run chronology.
- Existing Setup project-reference/package architecture ratchets and CI routing.
- The generated browser release capability currently remains false.

Not verified and not claimed:

- Restore success, exact resolved graph, workload/runtime packs, audits,
  signatures, license/NOTICE/SBOM closure, or publish success.
- Published-file inventory, absence of source maps/reporters/remote references,
  or reproducibility.
- Runtime request/storage behavior, browser security, accessibility/AT,
  localization/RTL, legal-template authority, deployment, release, or support.

No implementation, build, restore, publish, or test command was run for this
review.

## Final Decision And Authority Boundary

**Approve with enumerated pre-probe corrections:** the exact B0 candidate may be
presented for exact-revision user approval. If that approval names binding
`setup-assistant-presentation-targets-b0-20260831`, the restricted B-020 locked
graph/publish probe may proceed under this review's eight corrections. A clean
probe is evidence for a later technical decision; it is not approval for B-030,
release-capability activation, browser implementation acceptance, desktop,
secret handling, support, release, or shipping.

CTO review grants neither user approval nor shipping approval.
