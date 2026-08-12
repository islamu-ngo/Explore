<!-- ABOUTME: Pull request template for scope, verification, legal status, and clean-room provenance evidence. -->
<!-- ABOUTME: Requires contributors to record docs, release, CLA, dependency, and AI handoff impact. -->

## Summary

-

## Linked Issue / Context

- Closes #
- Relevant plan, context, or source anchors:

## Documentation Impact

Choose one and explain:

- [ ] Updated — docs changed in this PR because behavior, commands, configuration, API contracts, operator flows, or release notes changed.
- [ ] Not needed — change is internal and does not alter documented behavior.
- [ ] Deferred — docs impact exists but is intentionally split; include owner, follow-up path, and reason.

Details:

## Validation Run

Paste exact commands and results. Run projects individually; do not use solution-level `dotnet test`.

- [ ] Build: `dotnet build --configuration Release --verbosity quiet`
- [ ] Architecture/docs checks: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Related project tests:

## UI / Screenshots

- [ ] Not a UI change
- [ ] Screenshots or recordings attached for UI changes

## Release Impact

- [ ] Not applicable - no security, migration, configuration, OpenAPI, operator, or release-note impact.
- [ ] Security/auth impact documented
- [ ] Migration/data/rollback impact documented
- [ ] Configuration/secrets/deployment impact documented
- [ ] OpenAPI/client contract impact documented
- [ ] Operator/self-hosting/release-note impact documented
- [ ] Release-impacting change checked against `docs/RELEASE_CHECKLIST.md`

Details:

## Contributor Legal Status

A bot will post signing instructions on this pull request as soon as it is opened. Every non-bot contributor must sign the [ISLAMU Event Contributor License Agreement v1.0](https://github.com/islamu-ngo/Event/blob/develop/legal/CLA.md) by posting a comment on this pull request with the exact text:

```text
I have read and agree to the ISLAMU Event Contributor License Agreement v1.0, and I confirm that I have the right to submit my contribution under it.
```

To re-run the CLA check after signing, post a comment with `recheck`.

Details:

## IP / Clean-Room / Dependency Provenance

- [ ] Not externally informed and no dependency changed
- [ ] Externally informed work used only a sanitized functional specification; provenance and independent SSO review are linked below
- [ ] New or changed dependencies passed the repository license audit and preserve every intended ISLAMU outbound licensing path, or separate-license approval is linked below
- [ ] No third-party source, snippet, AST, SQL, migration, test, comment, or asset was copied or supplied to the implementation context

Evidence:

## AI Agent / Dev Docs Handoff

- [ ] Not applicable
- [ ] Updated active dev docs, handoff notes, or agent context where applicable

Details:
