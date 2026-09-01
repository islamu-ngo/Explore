<!-- ABOUTME: Implementation plan for successor B, the Setup Assistant presentation targets workstream. -->
<!-- ABOUTME: Defines the unapproved B0 shared Razor plus no-secret browser candidate, its gates, and its blocked alternatives. -->

# Setup Assistant Presentation Targets — Plan

> **SUPERSEDED AND NON-EXECUTABLE — HISTORICAL RECORD ONLY**
>
> Candidate B0 was never user-approved and has been replaced by successor-B
> revision B1 in
> `dev/active/setup-assistant-security-and-portability/`. No task, probe,
> approval path, fallback, or conditional authorization in this document may be
> executed or transferred to B1.

Last Updated: 2026-08-31 Europe/Brussels

Umbrella workstream:
[setup-assistant-security-and-portability-plan.md](../setup-assistant-security-and-portability/setup-assistant-security-and-portability-plan.md)

Candidate dependency evidence:
[setup-assistant-security-and-portability-dependency-evidence.md](../setup-assistant-security-and-portability/setup-assistant-security-and-portability-dependency-evidence.md)

## 1. Status And Approval Posture

**Lifecycle: superseded.** Successor B still owns actual GUI framework
selection and runtime targets, but B0 is no longer an approval candidate.
Revision B1 exclusively owns the active CommunityToolkit/Avalonia/Terminal.Gui
direction. The shared Razor/browser proposal below is retained only to explain
the rejected historical branch.

All presentation shells remain package-free, disabled, and non-shipped until
their B1 slice receives its own current evidence and approval. B0 cannot be
revived as a fallback and no B0 review may be cited as authority.

The repository is greenfield. No backward compatibility, migration shim, or
legacy presentation path is preserved by this plan.

## 2. Inherited State

- Successor A is Green through SA-110 to SA-430 and Phase 1 to Phase 4. Its
  public contracts in `Event.Wire.Contracts` and `Event.Setup.Core` are the
  only upstream this plan consumes.
- SA-510 framework-neutral Red is **accepted** at 8 total, 5 passed, 3 failed,
  0 skipped. One failure is the intended aggregate: the shared workspace owner
  and the selected-framework adapter are both absent. Two independent
  test-verifier defects were corrected, recompiled with clean diagnostics, and
  not rerun. SA-510 stays unchecked in the umbrella ledger, and this plan does
  not mark it complete.
- The I-VSD report is `current` / `plan-aligned` at reviewed input revision
  `sha256:d2bbba40455c013e20883ab6202f84411bb05f2c20f6060a9e73095f44a8e4b1`,
  with `IVSD-F001` to `IVSD-F046` and matching mitigations preserved. The
  mappings that bind here are `IVSD-F015`, `IVSD-F018`, `IVSD-F019`,
  `IVSD-F023` to `IVSD-F028`, `IVSD-F034`, and `IVSD-F035`.
- Existing CTO and user approvals cover the BCL-only successor-A strategy only.
  Successor B inherits none of them.
- Security and accessibility review facts on record: browser accessibility
  evidence is materially incomplete and must be stated as a target-labelled
  limitation rather than a parity claim; legal template and role claims must be
  blank or approved local assets; no secret value may appear in announcements,
  automation metadata, or diagnostics.

## 3. Candidate Revision B0 — Exact Scope

### 3.1 Shared `Event.SetupAssistant`

Convert only after approval:

- SDK `Microsoft.NET.Sdk.Razor`, target framework `net10.0`.
- `FrameworkReference` on `Microsoft.AspNetCore.App`.
- Exactly one project reference: `Event.Setup.Core`.
- Contents are semantic Razor components, view models, resource files, and
  plain CSS.
- No MudBlazor and no component package of any kind. No JavaScript interop
  package, no icon pack, no theme package.

### 3.2 Browser `Event.SetupAssistant.Browser`

Convert only after approval:

- SDK `Microsoft.NET.Sdk.BlazorWebAssembly`, standalone, no hosted server.
- Exact `net10.0` browser target as supported by the current installed SDK; the
  activation task records the exact resolved target moniker rather than
  assuming one.
- One direct product package: `Microsoft.AspNetCore.Components.WebAssembly`
  `10.0.10`.
- One project reference: `Event.SetupAssistant`.
- Static, no-secret browser only.

Browser capability flags stay false or absent: secret capability, service
worker, PWA manifest, remote assets, network or provider authority, telemetry,
crash or usage reporters, and any persistent storage including
`localStorage`, `sessionStorage`, IndexedDB, cookies, and cache storage.

### 3.3 Desktop

`Event.SetupAssistant.Desktop` keeps `SetupTargetEnabled=false` and remains a
package-free ContractShell. Photino.Blazor over the shared Razor layer is the
preferred research candidate, and it is **not selected and not approved**: the
reviewed package line does not establish publisher-backed .NET 10 support,
native Photino/WebView2/WebKitGTK component and NOTICE mapping is incomplete,
accessibility depends on three system-webview bridges without complete
supported-target evidence, and exact publish inventories, protected-write
behavior, and six-RID evidence are unproved.

### 3.4 Blocked Alternatives

| Candidate | Outcome | Exact reason |
|---|---|---|
| Avalonia `12.1.1` shared or target use | **REJECT** | The main package directly depends on `Avalonia.Remote.Protocol` `12.1.1` and `Avalonia.BuildServices` `11.3.2` for `net10.0`; `Avalonia.Remote.Protocol.dll` ships as a runtime library so publish absence cannot be inferred; BuildServices publisher documentation states it collects build-time telemetry unless `AVALONIA_TELEMETRY_OPTOUT=1`; native and publish and accessibility mapping is incomplete. |
| Terminal.Gui `2.4.17` graph | **BLOCK (inherited)** | Mandatory `TextMateSharp.Grammars 2.0.4` lacks component-level provenance and notices; the 24-package graph is indivisible. |
| MudBlazor or any Blazor component package | **BLOCK** | No provenance, notice, publish-inventory, or accessibility review exists, and `B0` needs none of them. |
| Photino.Blazor desktop | **RESEARCH ONLY** | See 3.3. Not selected, not pinned, not restored. |
| Hosted Blazor, server-rendered, or SignalR variants | **BLOCK** | They introduce network and server authority that the no-secret static target forbids. |

## 4. Project And Dependency Graph

```text
Event.Wire.Contracts (existing, package-free)
    <- Event.Setup.Core (BCL only, existing, Green)
        <- Event.SetupAssistant
           (Microsoft.NET.Sdk.Razor, net10.0,
            FrameworkReference Microsoft.AspNetCore.App)
            <- Event.SetupAssistant.Browser
               (Microsoft.NET.Sdk.BlazorWebAssembly, net10.0 browser,
                Microsoft.AspNetCore.Components.WebAssembly 10.0.10)

Event.SetupAssistant.Desktop  (SetupTargetEnabled=false, package-free shell)
Event.SetupAssistant.Cli      (unchanged, BCL only)
```

Rules the graph enforces:

1. `Event.SetupAssistant` references `Event.Setup.Core` and nothing else.
2. `Event.SetupAssistant.Browser` references `Event.SetupAssistant` and the one
   named WebAssembly package and nothing else.
3. Neither project references Application, Domain, Persistence, API, the Blazor
   product app, or any HTTP client.
4. Test projects reference only their owning source project and existing
   repository-approved test infrastructure.
5. Desktop gains no reference, package, or SDK change.

## 5. Target And Capability Matrix

| Capability | Shared | Browser (B0) | Desktop |
|---|---|---|---|
| Activated by `B0` | Yes, after approval | Yes, after approval | No, stays disabled |
| Secret entry or storage | No | No | Blocked |
| Network, provider, or live authority | No | No | Blocked |
| Persistent browser storage | n/a | No | n/a |
| Service worker or PWA | n/a | No | n/a |
| Remote assets or CDN | No | No | Blocked |
| Telemetry or reporters | No | No | Blocked |
| Protected filesystem writes | No | No | Blocked |
| Download of generated no-secret output | n/a | Yes, user-initiated only | Blocked |
| Accessibility claim | Semantic contracts, tested | Target-labelled limitations, no parity claim | None |

## 6. Clean Architecture Ownership And No-Core-Duplication Rules

`Event.Setup.Core` remains the sole owner of manifest contracts, environment
catalogue and activation graph, dotenv codec, readiness, digests, diff and
coverage, secret classification, workflow transitions, and the legal Markdown
codec. Presentation owns rendering, focus, navigation, localization surface,
and intent dispatch.

Forbidden in shared and browser code:

- validators, serializers, relevance rules, or secret classification;
- restating a Core business rule in a component, view model, resource string,
  or CSS selector;
- computing readiness, coverage, digests, or diffs outside Core;
- constructing manifest or dotenv bytes anywhere but Core;
- caching a Core result in a mutable presentation field that can diverge.

Every workspace flow adapts an immutable Core result. Byte-equivalent output
between CLI and browser for identical inputs is a tested contract, not an
assumption.

## 7. Package, Lock, Restore, And Publish Probes

Run for the exact activated graph, once activation is approved:

1. Force-evaluated restore per changed project, then locked restore per changed
   project with `--locked-mode`; a lock diff outside the two activated projects
   stops the task.
2. Committed lock file per changed project, reviewed for exactly one new direct
   package identity and its resolved transitive closure.
3. Package inventory: enumerate every resolved package with exact version, and
   assert no blocked identity (Terminal.Gui, any Avalonia, any component pack)
   appears.
4. NuGet audit for vulnerabilities and deprecation on the locked graph. Any
   advisory, withdrawal, signature failure, or audit-source failure stops the
   task without waiver.
5. License policy check across the recursive closure, plus NOTICE and SBOM
   entries for the one new direct package and its closure.
6. Publish probe for the browser target: produce the publish output, enumerate
   its inventory, and assert no unexpected assembly, no reporter, no service
   worker, and no remote asset reference.
7. Telemetry opt-out: assert no build-time or runtime telemetry component
   exists in the activated graph. Should any future candidate reintroduce one,
   its opt-out variable must be set in the build and proved absent from publish
   output before approval, not after.

## 8. Browser Security Rules

The activated browser target must prove, by test and by publish inventory:

- no outbound network request of any kind at runtime, including fetch, XHR,
  WebSocket, and font or asset requests to a non-local origin;
- no persistent storage write;
- no secret input path, no secret rendering, no secret in DOM attributes,
  automation metadata, or console output;
- all assets are same-origin and shipped in the publish output;
- no `eval`, no dynamic script injection, no third-party script;
- public trust posture: the target is assumed to be served from a public static
  host and must be safe when the entire artifact is world-readable. Nothing in
  the artifact may be a secret, a credential, an internal hostname, or a
  tenant identifier;
- source integrity: every shipped file is repository-authored or comes from the
  one approved package.

Browser secret capability, SA-610 and later, stays separately gated and is not
in `B0`.

## 9. Accessibility, RTL, And Localization

- Semantic HTML elements and ARIA only where semantics are missing; stable
  names, roles, and states.
- Keyboard reachability, visible focus, logical tab order, focus restoration
  after dialog and step transitions.
- Errors associate with their field and summarize once per submit.
- Logical properties for layout so RTL works without a mirrored stylesheet;
  RTL locale rendering is tested.
- Bundled localization resources only, no remote translation fetch. Security
  consequences are localized and bundled.
- Reduced motion, contrast, and non-color status indication.
- Browser limitations are labelled honestly per `IVSD-F035`; no unsupported
  parity claim ships.

## 10. Legal Editor Constraint (SA-540 mapping)

The constrained legal editor uses the Core Markdown codec, rejects HTML, remote
content, unresolved authority, and any publication or acceptance mutation.
Templates are blank or approved immutable attributed local assets. No embedded
browser, plugin, macro, network spellcheck, or generated text.

## 11. Umbrella Mapping

Business rules stay in the umbrella. This plan maps ownership only.

| Umbrella task | Successor B task | Boundary |
|---|---|---|
| SA-510 | B-010, B-020 | Contract Red accepted 7/8 with one missing-owner and adapter aggregate; B selects the exact graph and obtains approvals. Stays unchecked upstream. |
| SA-520 | B-030, B-040 | Shared workspace owners implemented against Core; browser no-secret adapter. |
| SA-530 | B-050 | Shared and browser accessibility, focus, RTL, localization. |
| SA-540 | B-060 | Constrained legal editor. |
| SA-610+ | Not in B0 | Browser secret capability remains separately gated. |
| SA-710+ | Not in B0 | Desktop remains disabled. |

## 12. Approval Boundary

I-VSD disposition and CTO review do **not** grant user approval. Before asking
for approval, record the exact SHA-256 digests of this plan, the tasks file,
and the context file, and name them in the request. Reviews bind to those exact
revisions. Any material rewrite of scope, graph, packages, capabilities, or
gates invalidates every prior review and requires fresh I-VSD, CTO, dependency,
security, and accessibility review before a new approval request.

## 13. Rollback And Removal

Activation is reversible by file edits only:

1. Restore the two `.csproj` files to package-free `ContractShell` form.
2. Restore or delete the affected lock files to their pre-activation content.
3. Remove added Razor, resource, CSS, and adapter files.
4. Leave `SetupTargetEnabled=false` everywhere.
5. Never use destructive git operations for rollback. No reset, no clean, no
   checkout over uncommitted work.

Stop and roll back on any of: graph drift, an unexpected transitive package, a
lock diff outside the two projects, an advisory or signature failure, a publish
inventory surprise, or evidence that a reviewed fact has changed.
