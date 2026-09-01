<!-- ABOUTME: Working memory for successor B, the Setup Assistant presentation targets workstream. -->
<!-- ABOUTME: Records the unapproved B0 candidate state, accepted SA-510 Red, evidence bindings, and the exact next step. -->

# Setup Assistant Presentation Targets — Context

> **SUPERSEDED AND NON-EXECUTABLE — HISTORICAL RECORD ONLY**
>
> This context records the abandoned B0 Razor/browser candidate. B0 was never
> user-approved, has no current next step, and cannot authorize a package,
> probe, test, shell, or release action. B1 exclusively owns successor B.

Last Updated: 2026-08-31 Europe/Brussels

## Current State

- Candidate revision `B0` is **superseded without execution**. No package, SDK,
  target, or shell was activated. No production file changed for B0.
- SA-510 framework-neutral Red is **accepted**: the lead confirmation observed
  8 total, 7 passed, 1 failed, 0 skipped, 589 ms. The sole failure is the
  aggregate missing shared owner and selected-framework adapter. SA-510 stays
  unchecked in the umbrella ledger.
- All three presentation shells remain package-free and disabled;
  `SetupTargetEnabled=false` holds for browser and desktop.
- **Exact next step:** none. Continue only from the B1 umbrella workstream.

## Documents

- Plan:
  [setup-assistant-presentation-targets-plan.md](setup-assistant-presentation-targets-plan.md)
- Tasks:
  [setup-assistant-presentation-targets-tasks.md](setup-assistant-presentation-targets-tasks.md)
- Umbrella plan:
  [setup-assistant-security-and-portability-plan.md](../setup-assistant-security-and-portability/setup-assistant-security-and-portability-plan.md)
- Umbrella tasks:
  [setup-assistant-security-and-portability-tasks.md](../setup-assistant-security-and-portability/setup-assistant-security-and-portability-tasks.md)
- Umbrella context:
  [setup-assistant-security-and-portability-context.md](../setup-assistant-security-and-portability/setup-assistant-security-and-portability-context.md)
- Dependency evidence:
  [setup-assistant-security-and-portability-dependency-evidence.md](../setup-assistant-security-and-portability/setup-assistant-security-and-portability-dependency-evidence.md)
- B0 intake review:
  [setup-assistant-presentation-targets-intake-review.md](setup-assistant-presentation-targets-intake-review.md)
- I-VSD report:
  [i-vsd-setup-assistant-security-and-portability.md](../../../islamic-value-sensitive-design/i-vsd-setup-assistant-security-and-portability.md)

## Review Bindings

| Item | Value |
|---|---|
| I-VSD reviewed input revision | pending successor-B review |
| I-VSD status / disposition | pending successor-B review |
| CTO review coverage | Successor A BCL-only strategy only; B inherits nothing |
| User approval coverage | Successor A only |
| B plan digest | bound in `setup-assistant-presentation-targets-review-bindings.md` |
| B tasks digest | bound in `setup-assistant-presentation-targets-review-bindings.md` |
| B context digest | bound in `setup-assistant-presentation-targets-review-bindings.md` |

Digests are recorded in a separate immutable review-binding artifact to avoid a
self-referential context digest. Any material triad rewrite requires a new
binding revision. I-VSD and CTO review do not substitute for user approval.

## Candidate B0 Summary

Partial successor approval candidate. Shared `Event.SetupAssistant` becomes
`Microsoft.NET.Sdk.Razor` on `net10.0` with the `Microsoft.AspNetCore.App`
framework reference and a single reference to `Event.Setup.Core`. Browser
`Event.SetupAssistant.Browser` becomes standalone
`Microsoft.NET.Sdk.BlazorWebAssembly` on the exact SDK-supported `net10.0`
browser target with one direct package,
`Microsoft.AspNetCore.Components.WebAssembly 10.0.10`. Desktop stays disabled.
Browser secret capability, service workers, PWA, remote assets, network or
provider authority, telemetry, reporters, and storage stay off.

No weaker fallback exists. Rejection of `B0` leaves successor B inactive.

## Blocked And Not Selected

- **Avalonia 12.1.1:** rejected. Direct dependencies on
  `Avalonia.Remote.Protocol 12.1.1` and `Avalonia.BuildServices 11.3.2`;
  `Avalonia.Remote.Protocol.dll` ships as a runtime library so publish absence
  is unproved; BuildServices documents build-time telemetry unless
  `AVALONIA_TELEMETRY_OPTOUT=1`; native, publish, and accessibility evidence
  incomplete.
- **Terminal.Gui 2.4.17:** blocked upstream, 24-package graph indivisible,
  `TextMateSharp.Grammars 2.0.4` provenance and notices incomplete.
- **Photino.Blazor desktop:** preferred research candidate, not selected. .NET
  10 publisher support, native NOTICE mapping, accessibility bridges, publish
  inventory, protected writes, and six-RID evidence are incomplete.
- **MudBlazor and any component package:** out of scope for `B0`.

## Security And Accessibility Facts In Force

- Browser accessibility evidence is materially incomplete; ship
  target-labelled limitations, never a parity claim (`IVSD-F035`).
- No secret value may appear in the browser artifact, DOM, automation
  metadata, diagnostics, or announcements.
- The browser artifact is treated as fully public; nothing in it may be
  sensitive.
- Legal templates are blank or approved immutable attributed local assets; no
  publication or acceptance mutation (`IVSD-F023`-`IVSD-F028`).
- Bundled localization and RTL only; no remote resource fetch (`IVSD-F019`).

## Decisions

1. Split the shared and browser activation from desktop so a provenance-complete
   subset can ship while desktop stays honestly disabled.
2. Keep every business rule in `Event.Setup.Core`; presentation adapts, never
   restates.
3. Accept no telemetry-bearing component in the activated graph.
4. Roll back activation with file edits only; no destructive git.

## Open Questions

- Exact `net10.0` browser target moniker resolved by the installed SDK is
  recorded at activation time, not assumed now.
- Whether counsel-approved legal templates exist; until then only blank or
  project-authored approved templates ship.

## Posture

Greenfield. No backward compatibility, no migration path, no legacy
presentation surface is preserved.
