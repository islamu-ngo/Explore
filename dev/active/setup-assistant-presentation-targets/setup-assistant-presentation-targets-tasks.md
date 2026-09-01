<!-- ABOUTME: Execution ledger for successor B, the Setup Assistant presentation targets workstream. -->
<!-- ABOUTME: Sequences approval, graph activation, test-first shared and browser work, verification, and blocked re-entry gates. -->

# Setup Assistant Presentation Targets — Task Checklist

> **SUPERSEDED AND NON-EXECUTABLE — HISTORICAL RECORD ONLY**
>
> Every B0 task below is closed without execution. B0 was never user-approved,
> no B0 completion is claimed, and no task or conditional probe path may be
> resumed. Successor-B revision B1 is owned by the umbrella workstream.

Last Updated: 2026-08-31 Europe/Brussels

## Status Summary

- **Overall status:** Superseded without execution. Candidate `B0` was never
  user-approved and no production file changed.
- **Completed:** 0/9 executable tasks.
- **Current priority:** None. No task in this ledger is executable.
- **Replacement:** B1 in the umbrella workstream exclusively owns successor B.
- **Plan:**
  [setup-assistant-presentation-targets-plan.md](setup-assistant-presentation-targets-plan.md)
- **Context:**
  [setup-assistant-presentation-targets-context.md](setup-assistant-presentation-targets-context.md)
- **Umbrella ledger:**
  [setup-assistant-security-and-portability-tasks.md](../setup-assistant-security-and-portability/setup-assistant-security-and-portability-tasks.md)
- **Dependency evidence:**
  [setup-assistant-security-and-portability-dependency-evidence.md](../setup-assistant-security-and-portability/setup-assistant-security-and-portability-dependency-evidence.md)
- **B0 intake review:**
  [setup-assistant-presentation-targets-intake-review.md](setup-assistant-presentation-targets-intake-review.md)

SA-510 remains unchecked in the umbrella ledger. Nothing in this file marks an
umbrella task complete.

## Rules

1. Every implementation task is test-first: author the failing test, observe the
   named failure once, then implement until it passes.
2. Each task is atomic and independently revertible.
3. No task touches Application, Domain, Persistence, API, the Blazor product
   app, CI, or the solution beyond the two activated projects.
4. Rollback is file edits only. No destructive git.
5. Any graph or evidence drift stops the task, reverts activation edits, and
   leaves the shells disabled.

## Historical Task Ledger — Never Execute

- [ ] **B-010 — Record exact revisions and obtain approval**
  - Compute and record SHA-256 digests of the B plan, tasks, and context files
    in the context Review Bindings table.
  - Bind the fresh dependency, security, accessibility, and target disposition
    in `setup-assistant-presentation-targets-intake-review.md`.
  - Request fresh I-VSD revalidation and fresh CTO review bound to the final
    digests.
  - Request exact-revision user approval for candidate `B0` as a partial
    approval: shared plus browser no-secret only.
  - **Done when:** user approval names the exact three digests. Any material
    rewrite invalidates the reviews and restarts this task.
  - **Dependencies:** none.

- [ ] **B-020 — Activate only the shared Razor plus browser no-secret graph**
  - Convert `Event.SetupAssistant` to `Microsoft.NET.Sdk.Razor`, `net10.0`,
    `FrameworkReference Microsoft.AspNetCore.App`, reference only
    `Event.Setup.Core`.
  - Convert `Event.SetupAssistant.Browser` to standalone
    `Microsoft.NET.Sdk.BlazorWebAssembly` on the exact SDK-supported `net10.0`
    browser target, with the single direct package
    `Microsoft.AspNetCore.Components.WebAssembly 10.0.10` and a reference to
    the shared project. Record the resolved target moniker in the context.
  - Run force-evaluated restore, then locked restore for both projects; update
    the two tracked lock files without creating a git commit.
  - Run package inventory, NuGet vulnerability and deprecation audit, license
    policy, NOTICE, and SBOM checks on the locked closure.
  - Run the browser publish probe and record the full publish inventory.
  - **Done when:** exactly one new direct package identity appears, no blocked
    identity appears anywhere in the closure, audits are clean, and the publish
    inventory contains no reporter, service worker, or remote asset.
  - **On drift:** revert both `.csproj` files and both lock files with file
    tools, delete added files, keep `SetupTargetEnabled=false`, stop.
  - **Dependencies:** B-010.

- [ ] **B-030 — Turn the accepted SA-510 workspace Red Green**
  - Implement the missing shared workspace owner and the selected-framework
    adapter that the accepted Red aggregate names.
  - Rerun the two corrected test-verifier defects so all eight tests execute.
  - **Done when:** `SetupAssistantWorkspaceTests` is 8/8 with zero skips, and no
    Core rule is restated in presentation code.
  - **Dependencies:** B-020.

- [ ] **B-040 — Shared Razor workspace UI (SA-520 shared half)**
  - Red first: component tests for topology and capability selection, typed
    fields, deterministic preview, diff, coverage, explicit sensitivity,
    no-secret primary action, review, clear, and save intents.
  - Implement semantic Razor components, view models, resources, and CSS with
    no component package.
  - **Done when:** tests pass and an assertion proves no validator, serializer,
    relevance rule, or secret classification exists outside Core.
  - **Dependencies:** B-030.

- [ ] **B-050 — Browser no-secret adapter (SA-520 browser half)**
  - Red first: tests asserting zero network requests, zero persistent storage
    writes, same-origin assets only, no `eval` or injected script, no secret
    input path, and byte-equivalent no-secret output versus the CLI for
    identical inputs.
  - Implement the standalone browser host and user-initiated download of
    generated no-secret output.
  - **Done when:** tests pass and the publish inventory still matches B-020.
  - **Dependencies:** B-040.

- [ ] **B-060 — Accessibility, RTL, and localization (SA-530)**
  - Red first: `SetupAccessibilityContractTests` for names, roles, states,
    keyboard reachability, tab order, focus restoration, error association and
    single summary, reduced motion, contrast, non-color status, RTL logical
    layout, and absence of any secret in announcements or automation metadata.
  - Implement bundled localization resources and the focus model.
  - **Done when:** tests pass and browser limitations are target-labelled with
    no parity claim.
  - **Dependencies:** B-050.

- [ ] **B-070 — Constrained legal editor (SA-540)**
  - Red first: `LegalAuthoringWorkspaceTests` rejecting HTML, remote content,
    unresolved authority, and any publication or acceptance mutation.
  - Implement source, outline, sanitized preview, typed placeholders, locale
    comparison, bounded counts, diff, and undo or redo over the Core codec.
  - **Done when:** tests pass and templates are blank or approved immutable
    attributed local assets.
  - **Dependencies:** B-060.

- [ ] **B-080 — Publish and security verification**
  - Rerun locked restore, audits, license, NOTICE, SBOM, and the browser
    publish probe on the final graph.
  - Assert the public-trust rule: the artifact contains no secret, credential,
    internal hostname, or tenant identifier.
  - **Done when:** all probes are clean and the inventory is recorded as
    evidence.
  - **Dependencies:** B-070.

- [ ] **B-090 — Documentation and evidence**
  - Update `docs/ACCESSIBILITY.md` and `docs/LOCALIZATION.md` with the honest
    browser limitation statement.
  - Record the activation evidence packet: graph, locks, audit results,
    publish inventory, and test results.
  - Update the umbrella ledger to reflect delivered B scope. SA-510 is checked
    only by the umbrella owner after B-030 is Green and reviewed.
  - **Dependencies:** B-080.

## Blocked Future Re-entry Gates

Not executable tasks. Each needs its own selection, evidence, and fresh
approvals.

- **Desktop target (SA-710 and later):** blocked. Requires a desktop candidate
  with publisher-backed .NET 10 support, complete native component and NOTICE
  mapping, proved accessibility bridges, exact publish inventory, protected
  writes, and six-RID evidence. Photino.Blazor is research only.
- **Browser secret capability (SA-610 and later):** blocked. Requires a separate
  security review, threat model, and exact-revision approval; nothing in `B0`
  grants it.
- **Avalonia reconsideration:** blocked. Requires new publisher-authoritative
  component, telemetry, publish, and accessibility evidence that resolves the
  `Avalonia.Remote.Protocol` and `Avalonia.BuildServices` findings.
- **Any component package:** blocked pending its own provenance and
  accessibility review.
