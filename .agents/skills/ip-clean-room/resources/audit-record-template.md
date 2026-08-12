<!-- ABOUTME: Template for sanitized research handoffs and durable clean-room provenance records. -->
<!-- ABOUTME: Records reproducible evidence without retaining third-party source expression. -->

# Audit Record Template

```text
Date (Europe/Brussels):
Intent / issue / workstream:
Feature or dependency:

Source register:
- Title / URL or authoritative terms / access date / access basis
- Observed facts only; no excerpts, code, screenshots, or source-derived structure

Functional specification:
- Actors and goals
- Inputs and outputs
- State transitions and errors
- Constraints and acceptance criteria
- Assumptions / unresolved questions

Clean-room attestation:
- No third-party source, snippet, AST, decompiled artifact, SQL, migration,
  test, comment, prose, or asset is included or supplied to implementation.
- Research context ended; implementation started from this sanitized handoff.

Independent design / SSO:
- ISLAMU source anchors
- Repository-native architecture choices
- Constrained/commonplace elements and rationale
- Discretionary differences
- AFC/SSO reviewer and decision: pass | redesign | legal review

Dependency decision:
- None, or component/version/role/license/obligations/outbound-mode decision
- Scanner command/result and separate approval when applicable

Evidence:
- Handoff path / commit / PR
- Tests and build
- Journal entry
```

Do not add hashes of restricted content, copied excerpts, or attachments that would recreate the material the clean-room boundary excludes.

