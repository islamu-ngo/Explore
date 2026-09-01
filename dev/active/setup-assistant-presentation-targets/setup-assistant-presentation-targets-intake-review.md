<!-- ABOUTME: Dependency, security, accessibility, and target intake review for successor-B candidate B0. -->
<!-- ABOUTME: Conditionally permits an exact shared/browser graph probe while retaining all shipping and secret gates. -->

# Setup Assistant Presentation Targets — B0 Intake Review

> **SUPERSEDED AND NON-AUTHORIZING — HISTORICAL RECORD ONLY**
>
> This intake no longer permits a restore, publish, implementation, or
> approval step. Its conditional disposition expired when B1 replaced B0.

Last Updated: 2026-08-31 Europe/Brussels

## Bound Candidate

- Shared: `Event.SetupAssistant`, `Microsoft.NET.Sdk.Razor`, `net10.0`,
  `Microsoft.AspNetCore.App`, and `Event.Setup.Core` only.
- Browser: `Event.SetupAssistant.Browser`,
  `Microsoft.NET.Sdk.BlazorWebAssembly`, exact installed-SDK browser TFM,
  `Microsoft.AspNetCore.Components.WebAssembly` `10.0.10`, and the shared
  project only.
- Desktop: disabled package-free contract shell.
- Browser secret capability: disabled.

This review authorizes no package activation, probe, implementation, or user
approval. All historical conditional language below is non-executable.

## Dependency And Provenance Disposition

**Historical disposition only: superseded before any probe.**

The shared project adds no product package. The browser candidate stays on the
repository's Microsoft/.NET 10 package line. Activation must stop and be
removed if locked restore reveals an unlisted package, unsupported TFM,
advisory, signature/audit failure, incompatible license, source map,
development server, service worker, telemetry/reporting component, or publish
asset outside the approved static no-secret role.

Before B0 can be called implemented, the probe must record:

- exact direct/transitive/build/workload graph and lock digests;
- package signatures, audit result, license-policy result, SBOM, and NOTICE;
- exact `wwwroot` publish inventory and digest;
- proof that development-server/build-only nodes do not ship;
- local-only assets and no remote protocol, designer, diagnostics, telemetry,
  reporter, service-worker, or update component.

Avalonia `12.1.1` remains blocked. Its main package directly depends on
`Avalonia.Remote.Protocol` `12.1.1` and `Avalonia.BuildServices` `11.3.2`;
publisher documentation confirms build-time telemetry unless opted out.
Native/publish/accessibility evidence remains incomplete.

Photino remains an unselected research candidate. Publisher-backed .NET 10
support, native component/NOTICE mapping, system-webview support,
accessibility, publish inventory, protected writes, and six-RID evidence are
not complete.

## Security Disposition

**Historical disposition only: superseded before implementation.**

The browser adapter must begin and remain useful in no-secret mode. It may
consume only bundled static assets and package-free shared/Core behavior. It
has no API, provider, HTTP, database, telemetry, logging, AI, live-target, or
secret-provider authority.

Release validation must reject:

- secret-capable browser state or stored mode decisions;
- fetch/XHR/WebSocket/EventSource/beacon/form/navigation adapters;
- cookies, local/session storage, IndexedDB, Cache API, service workers, PWA,
  analytics, crash upload, CSP reporting, remote fonts/assets, or source maps;
- direct Application/Domain/API/Blazor-project dependencies;
- UI-owned validation, serialization, relevance, sensitivity, readiness,
  legal publication/acceptance, or target mapping.

Browser secret mode remains a separate SA-610+ decision and is not approved.

## Accessibility And Localization Disposition

**Historical disposition only: superseded before evidence collection.**

Shared Razor uses native semantic HTML before custom controls. B0 must prove
keyboard completion, deterministic focus movement/restoration, labels,
descriptions, required/error association, one error summary, status
announcements, non-color state, logical RTL layout, Arabic reading/tab order,
200% zoom/reflow, reduced motion, contrast, stable value-free automation
identifiers, and no secret/value/path content in accessible names.

No browser/desktop screen-reader parity claim is approved by this intake.
Browser automation and representative assistive-technology runs are required
before support claims. Desktop accessibility is out of B0 scope.

## Target And Capability Decision

| Capability | B0 disposition |
|---|---|
| Shared framework-neutral workspace owners | Implement after approval |
| Shared semantic Razor components | Probe and implement after approval |
| Static browser no-secret target | Probe and implement after approval |
| Browser secret entry | Disabled |
| Desktop target | Disabled |
| Live/network/provider authority | Forbidden |
| Release/support claim | Blocked until publish and accessibility evidence |

## Stop Conditions

Stop, remove the activation edits with file tools, and restore disabled shells
if any exact graph, audit, provenance, publish, security, or accessibility
condition fails. There is no compatibility shim, fallback package, weaker
target, or scanner waiver.

## Sources

- [Umbrella dependency evidence](../setup-assistant-security-and-portability/setup-assistant-security-and-portability-dependency-evidence.md)
- [B0 plan](setup-assistant-presentation-targets-plan.md)
- [B0 context](setup-assistant-presentation-targets-context.md)
- [B0 tasks](setup-assistant-presentation-targets-tasks.md)
- [Avalonia package metadata](https://api.nuget.org/v3-flatcontainer/avalonia/12.1.1/avalonia.nuspec)
- [Avalonia BuildServices telemetry](https://www.nuget.org/packages/Avalonia.BuildServices/11.3.2)
- [Avalonia accessibility](https://docs.avaloniaui.net/docs/app-development/accessibility)
- [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms)

