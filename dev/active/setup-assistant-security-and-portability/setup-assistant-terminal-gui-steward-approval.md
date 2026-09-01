<!-- ABOUTME: Records the Project Steward's exact downstream Terminal.Gui packaging authorization. -->
<!-- ABOUTME: Binds the temporary patch scope, provenance controls, and mandatory upstream-return condition. -->

# Terminal.Gui Downstream Package — Project Steward Approval

Date: 2026-09-01 Europe/Brussels
Status: Approved
Upstream: `Terminal.Gui` `v2.4.17`
Upstream tag object: `58f3af1a4afe5d2772be134b2299a0f78f35c93c`
Upstream commit: `d0a0ed9b150d3fc8aacf4ab07b7f7d91264fe6d6`
Internal package identity: `ISLAMU.Terminal.Gui`
Initial internal version: `2.4.17-islamu.1`

The Project Steward explicitly authorizes Terminal.Gui as the sole human
terminal framework. The repository-native console TUI and every fallback path
must be removed; the deterministic noninteractive machine CLI remains a
separate product surface.

The authorized downstream package must:

- derive strictly from the official `v2.4.17` commit above;
- carry only a minimal, reviewable patch removing
  `TextMateSharp.Grammars` and the editor or syntax-highlighting integration
  that requires the grammar corpus; remove `TextMateSharp` itself only if the
  pinned-source audit proves it has no remaining framework use;
- retain upstream MIT license, copyright, attribution, source identity, and
  modification notices;
- use the distinct internal package identity above and never impersonate the
  official NuGet artifact;
- bind the final package, patch series, dependency closure, SBOM, notices,
  vulnerability evidence, and reproducibility result by digest;
- fail CI if `TextMateSharp.Grammars` re-enters any project, lock, package,
  publish, or SBOM graph; and
- remain a temporary packaging delta with an explicit migration gate back to
  an official dependency-clean modular Terminal.Gui release.

No unrelated upstream modification or long-lived ISLAMU TUI fork is
authorized. Any patch expansion, upstream rebase, package-identity change, or
new transitive dependency requires a new exact evidence packet and Project
Steward approval.
