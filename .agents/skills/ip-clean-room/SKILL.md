---
name: ip-clean-room
description: Enforce source-free external research, independent SSO design, dependency-license compatibility, and auditable provenance.
type: guardrail
enforcement: block
priority: critical
---
<!-- ABOUTME: Blocking workflow skill for clean-room research, implementation handoffs, and dependency review. -->
<!-- ABOUTME: Protects ISLAMU's CLA-backed outbound licensing options and produces audit-ready provenance evidence. -->

## Purpose
Use this skill whenever external behavior, third-party designs, or dependency terms may influence repository work. It separates observation from implementation, requires independent Structure/Sequence/Organization (SSO), and records evidence without claiming legal certification.

## When to Load
- The task researches, benchmarks, compares, clones, replaces, or interoperates with an external product or design.
- A functional specification is derived from public UI, documentation, workflows, standards, or market benchmarks.
- A package, library, image, font, asset, dataset, generator, or commercial version is added or updated.
- Keywords include clean room, IP provenance, SSO, license compatibility, copyleft, source available, proprietary source, or audit readiness.
- Intent ID is `ip-clean-room-governance` or a cross-cutting architecture intent loads this skill.

## When NOT to Load
- Not for a purely internal change proven to use only repository code/docs and no dependency change; record `Not externally informed` during PR review.
- Not as permission to inspect source whose license or access terms are uncertain.
- Not as a substitute for the CLA workflow, dependency scanner, security review, or qualified legal advice.
- Not for copying compatibility code; exact interoperability elements require a recorded necessity and uncertain cases require legal review.
- Not after source contamination as a way to salvage the same implementation context; stop and restart from a sanitized handoff.

## Must-Read Docs
- [../../../AGENTS.md](../../../AGENTS.md)
- [../../../docs/QUICK_REFERENCE.md](../../../docs/QUICK_REFERENCE.md)
- [../../../docs/legal/IP_GOVERNANCE.md](../../../docs/legal/IP_GOVERNANCE.md)
- [../../../legal/CLA.md](../../../legal/CLA.md)
- [../../../docs/legal/CONTRIBUTION_GOVERNANCE.md](../../../docs/legal/CONTRIBUTION_GOVERNANCE.md)
- [../../../docs/DUAL_VERSIONING.md](../../../docs/DUAL_VERSIONING.md)
- [../../../docs/CI_CD_GOVERNANCE.md](../../../docs/CI_CD_GOVERNANCE.md)
- [resources/index.md](resources/index.md)

## Top 5 Invariants
1. Implementation context contains only sanitized functional requirements, repository-native design material, and permitted standards or interface facts, never third-party implementation source or source-derived representations.
2. Literal copying is zero-tolerance across code, SQL, migrations, tests, comments, documentation prose, assets, and generated artifacts.
3. The implementer independently designs naming, decomposition, data relationships, control flow, UI composition, tests, and documentation, then records an AFC/SSO review.
4. No dependency may block an intended ISLAMU outbound licensing path; the CLA never grants rights over third-party material, and exceptions require documented legal/distribution approval.
5. Externally informed work retains a source register, sanitized handoff, implementation-separation attestation, dependency decision, verification evidence, and PR/journal links without retaining restricted expression.

## Top 5 Anti-Patterns
1. **Source-bearing prompt:** Supplying external code, snippets, ASTs, decompiled output, SQL, or internal structural notes contaminates the implementation context.
2. **Cosmetic rewrite:** Translating syntax or changing frameworks while preserving distinctive SSO remains non-literal copying risk.
3. **CLA laundering:** Treating a contributor signature as authority to copy or relicense third-party material invalidates provenance.
4. **Scanner-as-counsel:** Treating a passing automated license audit as proof of commercial-contract or assembled-distribution compatibility leaves material obligations unreviewed.
5. **Anonymous benchmark report:** Removing source identity rather than source expression destroys the audit trail and cannot support a clean-room handoff.

## Minimal Examples
```text
Clean-room handoff:
- Sources: titles/URLs/access dates only
- Observed behavior: inputs, outputs, errors, constraints
- Excluded: source, snippets, ASTs, internal names, copied prose/assets
- ISLAMU design: repository-native aggregate/workflow/API/HAL choices
- Attestation: implementation starts in a fresh source-free context
```

```text
Dependency decision:
- Default build: compatible version, deterministic lock
- Optional commercial build: explicit opt-in and separate rights
- Block: any terms prevent an intended ISLAMU outbound offering
```

## Verification Hooks
- `dotnet run .ci/scripts/validate-dependency-license-policy.cs -- .`
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- `git diff --check -- AGENTS.md docs/legal docs/QUICK_REFERENCE.md .agents/skills/ip-clean-room .claude/rules/ip-clean-room.md .claude/contract/intents.yaml .claude/commands/review-pr.md .github/PULL_REQUEST_TEMPLATE.md dev/_journal/journal.md`
- Manual: complete [SSO And Provenance Review](resources/sso-and-provenance-review.md) and link the evidence in the PR.

## Related Skills
- [../agentic-research/SKILL.md](../agentic-research/SKILL.md)
- [../skill-authoring/SKILL.md](../skill-authoring/SKILL.md)
- [../clean-architecture-rules/SKILL.md](../clean-architecture-rules/SKILL.md)

