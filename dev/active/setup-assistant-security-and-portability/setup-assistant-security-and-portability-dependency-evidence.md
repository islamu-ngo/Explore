<!-- ABOUTME: Decision-complete source-free dependency evidence for Setup Assistant successor A. -->
<!-- ABOUTME: Records blocked Terminal.Gui and Avalonia graphs and selects a package-free BCL terminal foundation. -->

# Setup Assistant Security And Portability — Dependency Evidence

Last Updated: 2026-08-31 Europe/Brussels

## Decision Scope And Method

- **Decision owner:** SA-120 planning handoff; implementation remains open.
- **Evidence cutoff / access date:** 2026-08-31.
- **Research boundary:** Official package registry metadata, package manifests,
  signatures, vulnerability services, framework documentation, and sanitized
  component/notices summaries only. No package source, decompiled output,
  snippets, tests, assets, raw license text, or external prose is retained.
- **Outbound models evaluated:** public AGPL distribution and every intended
  ISLAMU alternative distribution/hosting path. A scanner result or permissive
  top-level license does not cure missing component provenance or notices.
- **Result:** Successor A selects a repository-native package-free product
  graph. Terminal.Gui and every Avalonia package remain absent. Successor B
  must independently select and approve its eventual GUI graph.

## Official Source Register

All sources below were accessed on 2026-08-31. URLs identify the official
registry or publisher surface used by the sanitized review; no package payload
or source expression is reproduced here.

| Evidence | Official URL | Fact retained |
|---|---|---|
| NuGet service index | https://api.nuget.org/v3/index.json | Official registration, package-content, vulnerability, and signature service discovery boundary. |
| Terminal.Gui 2.4.17 registration | https://www.nuget.org/packages/Terminal.Gui/2.4.17 | Exact direct candidate identity and declared dependency metadata. |
| Terminal.Gui version index | https://api.nuget.org/v3-flatcontainer/terminal.gui/index.json | 2.4.17 was the stable candidate evaluated. |
| TextMateSharp.Grammars 2.0.4 registration | https://www.nuget.org/packages/TextMateSharp.Grammars/2.0.4 | Exact mandatory transitive blocker identity. |
| Avalonia 12.1.1 registration | https://www.nuget.org/packages/Avalonia/12.1.1 | Exact shared compile candidate identity. |
| Avalonia version index | https://api.nuget.org/v3-flatcontainer/avalonia/index.json | 12.1.1 was the stable candidate evaluated. |
| Avalonia package publisher documentation | https://docs.avaloniaui.net/docs/get-started/install | Official package-role and target guidance boundary. |
| Avalonia WebAssembly deployment | https://docs.avaloniaui.net/docs/deployment/webassembly | Browser target and publish-output role boundary. |
| Avalonia supported platforms | https://docs.avaloniaui.net/docs/supported-platforms | Desktop/browser runtime target boundary. |
| Avalonia telemetry | https://docs.avaloniaui.net/docs/reference/telemetry | Build telemetry opt-out contract used for conditional scaffolding analysis. |
| NuGet signed packages | https://learn.microsoft.com/en-us/nuget/reference/signed-package-verification-options | Signature verification semantics; integrity is not license/provenance approval. |
| NuGet audit | https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages | Point-in-time vulnerability audit semantics and limitations. |

## Exact Decision Matrix

`APPROVE` means approved for the stated successor and role only. `BLOCK` means
no reference, pin, restore, lock graph, publish output, support claim, or
release inclusion. `CONDITIONAL` records a research conclusion but grants no
package authority to successor A.

| Candidate / target / role | Outcome | Exact reason and consequence |
|---|---|---|
| Successor A product graph: BCL plus existing package-free `Event.Wire.Contracts` | **APPROVE** | No new external product package. This is the selected A graph. |
| Repository-native bounded interactive terminal wizard in `Event.SetupAssistant.Cli` | **APPROVE** | Implement with BCL console/terminal primitives; keep deterministic machine CLI separate and non-secret. Product-owned adversarial tests govern TTY, redirection, echo, signals, resize, scrollback, and accessibility limitations. |
| Ten planned Setup project shells, lock files, and two generated fail-closed ratchets | **APPROVE for SA-120 scaffolding** | Shells establish the already-tested project graph only. `Event.SetupAssistant`, Browser, and Desktop shells are package-free, disabled, non-shipped contract boundaries, not functional UI or support evidence. No scaffolding is claimed to exist yet. |
| Terminal.Gui 2.4.17 direct package | **BLOCK** | Its mandatory graph includes TextMateSharp.Grammars 2.0.4, whose bundled grammar components lack complete component-level provenance and notices. No stable corrected package or supported no-grammar profile exists. |
| Complete Terminal.Gui 2.4.17 24-package resolved graph | **BLOCK** | The graph is indivisible for the supported candidate. Do not pin or restore Terminal.Gui or any member solely for this feature; do not create an exception, suppress a notice, or vendor a grammar subset. |
| TextMateSharp.Grammars 2.0.4 mandatory runtime/content role | **BLOCK** | Component-to-origin/license/notice coverage is incomplete, so redistribution obligations cannot be proven for all outbound paths. |
| Avalonia 12.1.1 shared compile dependency for non-shipped scaffolding | **CONDITIONAL, NOT SELECTED** | Potentially acceptable only with `AVALONIA_TELEMETRY_OPTOUT=1` and deterministic publish exclusions. A does not need it and therefore must not pin or restore it. |
| Avalonia BuildServices build-only role | **CONDITIONAL, NOT SELECTED** | Build-only use requires telemetry opt-out and proof it is absent from publish output. This does not approve any runtime target or A package reference. |
| ANGLE component in the browser/native analysis | **LICENSE RESOLVED, TARGET STILL BLOCKED** | BSD-3-Clause status is resolved; this isolated result does not cure the remaining runtime graph or approve a target. |
| Avalonia Desktop runtime graph | **BLOCK** | Broad SkiaSharp/HarfBuzz native notices contain unresolved reciprocal, custom, or unreviewed alternatives without binary-to-component mapping. |
| Avalonia Browser runtime graph | **BLOCK** | The same native/component provenance issue remains, and exact publish absence of `Avalonia.Remote.Protocol` is unproved. |
| `Avalonia.Remote.Protocol` publish role | **BLOCK UNTIL ABSENCE PROVED** | No release may assume an exclusion that has not been demonstrated against the exact publish graph/artifact. |
| Successor B GUI/browser/desktop implementation | **BLOCK PENDING FRESH SELECTION** | B retains all user outcomes but must select a provenance-complete GUI graph, or obtain new authoritative Avalonia component/build evidence, then receive fresh I-VSD, CTO, user, dependency, security, and accessibility approval. No package or target is silently inherited. |

## Source-Free Graph Summaries And Obligations

### Terminal graph TG-2.4.17

- **Resolved identity:** one direct Terminal.Gui 2.4.17 node and 23 mandatory
  resolved dependency nodes, 24 packages total for the evaluated graph.
- **Blocking path:** `Terminal.Gui 2.4.17` -> mandatory syntax/text processing
  closure -> `TextMateSharp.Grammars 2.0.4` -> bundled grammar components with
  incomplete component-level origin/license/notice mapping.
- **Coverage result:** top-level package metadata is insufficient because the
  distributed grammar content has component-specific notice/source
  obligations. The missing mapping prevents complete NOTICE/source-offer and
  outbound compatibility determinations.
- **Whole-graph consequence:** all 24 resolved nodes are rejected as the
  Terminal.Gui candidate graph. Individually permissive nodes do not create a
  supported partial profile, and no graph member is approved or pinned by this
  record.
- **Required correction to reconsider:** a stable publisher-supported graph
  whose every distributed grammar/component has authoritative provenance,
  license expression, notices, and applicable source obligations, or an
  official supported profile that excludes the grammar closure. Neither exists
  in the reviewed stable line.

### Avalonia AV-12.1.1

- **Shared compile closure:** Avalonia 12.1.1 can be evaluated for a
  non-shipped shell only when build telemetry is disabled with
  `AVALONIA_TELEMETRY_OPTOUT=1` and all runtime/remote/build-only material is
  proven absent from publish output. This conditional fact is not selected for
  A.
- **Build closure:** BuildServices is build-only under the reviewed model and
  requires the telemetry opt-out plus publish/SBOM exclusion evidence.
- **Desktop runtime closure:** SkiaSharp/HarfBuzz native payload notice sets
  enumerate broad alternatives, including reciprocal/custom/unreviewed terms,
  without authoritative mapping from each shipped binary to the applicable
  component and notice. Desktop is blocked as a whole.
- **Browser runtime closure:** ANGLE is resolved as BSD-3-Clause, but the wider
  native/component mapping remains incomplete. The exact publish graph also
  has not proved `Avalonia.Remote.Protocol` absent. Browser is blocked as a
  whole.
- **Obligations not yet dischargeable:** exact binary/component inventory,
  applicable license and patent terms, notices, source/source-offer duties,
  publish exclusions, SBOM identity, telemetry state, and compatibility across
  all ISLAMU outbound models.
- **Successor consequence:** no Avalonia package is approved, pinned, restored,
  locked, or published in A. B starts from a framework-neutral contract and
  obtains a new exact graph decision; this evidence does not silently approve
  any Avalonia package, target, or version.

## Vulnerability Findings At The Evidence Cutoff

- **Selected A product graph:** no new third-party product package is selected,
  so SA-120 introduces no new NuGet product-package advisory surface.
- **Blocked candidate graphs:** the sanitized metadata supplied for this record
  contains no advisory identifier or zero-advisory attestation. This evidence
  therefore makes no "no known vulnerabilities" claim for Terminal.Gui or
  Avalonia. Their legal/provenance blockers are independently decisive.
- **Implementation/re-entry requirement:** run a fresh locked-graph NuGet audit
  after the ten shells and locks exist and again for an exact release graph.
  Any advisory, unavailable/withdrawn package, signature failure, audit-source
  failure, or graph drift stops the affected task/target; it is not waived.

## Content, Signature, And Integrity Evidence

- The review compared official registration identities, dependency metadata,
  role inventories, notice/provenance summaries, and target publish
  obligations without retaining package payloads.
- Avalonia candidate packages passed signed-package integrity verification for
  the inspected identities. Signature success proves package integrity and
  signer binding only; it does not prove component provenance, notice
  completeness, outbound compatibility, or publish exclusion.
- Terminal.Gui's decision does not rely on signature status: mandatory grammar
  provenance/notices are incomplete, so the graph is blocked even if transport
  and package integrity checks succeed.
- No lock file, SBOM, package pin, restore output, publish output, or generated
  ratchet is claimed by this planning evidence. Those remain SA-120
  implementation artifacts.

## Selected Successor A Graph

```text
Event.Wire.Contracts (existing, package-free)
    <- Event.Setup.Core (BCL only)
        <- Event.SetupAssistant.Cli (BCL only: machine CLI + interactive wizard)
        <- Event.SetupAssistant (BCL-only disabled presentation contract shell)
            <- Event.SetupAssistant.Browser (BCL-only disabled target shell)
            <- Event.SetupAssistant.Desktop (BCL-only disabled target shell)
```

The five matching focused test shells reference only their owning source shell
and repository-approved existing test infrastructure. SA-120 will create all
five source shells, five test shells, one lock file per project, and the two
SA-110 generated fail-closed ratchets. The Browser/Desktop/presentation shells
exist only to close the tested project graph; they contain no UI framework,
runtime target, shipped capability, support claim, or dependency exception.

## Stop Conditions And Re-entry

Stop SA-120 or the affected successor immediately on any of the following:

1. a Terminal.Gui or Avalonia `PackageReference`, central pin, transitive
   restore, lock entry, vendored asset, or publish payload appears in A;
2. any new TUI/GUI package or package-policy exception is proposed;
3. a shell becomes functional, shipped, or support evidence before its owning
   successor is approved;
4. graph, signature, vulnerability, package ownership, license, notice,
   source-offer, patent, trademark, telemetry, native binary, or publish-role
   evidence is missing or changes;
5. `Avalonia.Remote.Protocol` absence is assumed rather than proved for an
   exact publish artifact;
6. a reciprocal/custom/unreviewed native alternative lacks authoritative
   binary/component mapping; or
7. the I-VSD, CTO, and user approvals are not bound to the changed exact
   plan/tasks revision.

There is no waiver, scanner override, compatibility shim, or "temporary" pin
path. Re-entry requires a provenance-complete replacement graph or new
publisher-authoritative evidence, a fresh vulnerability/license/outbound
review, planning-mode I-VSD revalidation, fresh revision-bound CTO review, and
exact-revision user approval.

## Clean-Room And SSO Attestation

- **AFC filtration:** retained facts are functional/security constraints,
  official package identities, graph roles, and redistribution obligations.
  No expressive implementation material was retained.
- **Independent structure/sequence/organization:** the selected terminal wizard
  follows ISLAMU's existing handwritten deterministic CLI pattern and uses a
  bounded linear state machine over `Event.Setup.Core`; it does not reproduce a
  third-party TUI's names, hierarchy, layout, operation ordering, tests, or
  documentation.
- **Repository-native anchors:** package-free Wire Contracts, pure Setup Core,
  stable machine JSON, fail-closed secret boundaries, architecture ratchets,
  and existing central lock/license governance.
- **Decision:** `pass` for this source-free planning handoff; implementation
  must consume this evidence and repository sources only.
- **Reviewer/date:** SA-120 dependency research handoff, 2026-08-31.
