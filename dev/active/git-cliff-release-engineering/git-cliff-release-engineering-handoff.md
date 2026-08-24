<!-- ABOUTME: Session handoff for continuing the provider-neutral git-cliff release-engineering workstream. -->
<!-- ABOUTME: Captures verified progress, pending gates, operator blockers, and the exact next verification step. -->

HANDOFF CONTEXT
===============

USER REQUESTS (AS-IS)
---------------------
- Session history lookup returned no sessions, so the original implementation request could not be retrieved verbatim.
- "do not care about backward compatibility at all we are in development mode !"
- "forgejo codeberg, tangled, github also. those are the 3"
- "do a session handoff right now ! in implementaiton plan"

GOAL
----
Complete the git-cliff release-engineering implementation plan without fabricating production
signer, tag, version, or provider-settings evidence. Achieved: 25 of 25 implementation tasks and all
eight phase-verification gate pairs. The only thing left in the workstream is the operator act of
creating the real signed tags, which needs steward approval and real signer custody.

WORK COMPLETED
--------------
- I implemented Tasks 1.1 through 6.1 and recorded independent adversarial evidence under `.omo/start-work/evidence/` and `.omo/start-work/ledger.jsonl`.
- I built a standalone .NET 10 release engine under `eng/release/` that owns Conventional Commit policy, change fragments, SemVer/range selection, canonical context, renderer-only git-cliff 2.13.1 execution, exact preparation commit `B`, SSH tag verification, stable-main topology, and deterministic candidate/final manifests.
- I integrated `release-evidence.v1.json` as the single canonical identity consumed by the existing durable evidence bundle; provider run metadata remains noncanonical.
- I implemented and independently confirmed transport-only adapters for Forgejo/Codeberg, Tangled, and GitHub. All three produce identical canonical input and promoted-bundle checksums. Tangled unsupported protected operations require separate operator evidence and fail closed otherwise.
- I fixed security/robustness findings found during review: renderer capability forgery, config grammar bypasses, filesystem links/aliases, exact-B drift, recreated tags, `main` CAS gaps, hung Git process groups, Windows hardlinks, trust-root test races, preview privilege escalation, PR-origin final events, and workflow/manifest trust overclaims.
- I completed Task 6.2 implementation follow-up by removing the unapproved `1.0.0` special case. A verified `changelog-baseline-YYYY-MM-DD` can now lower-bound any steward-approved first governed SemVer release, while a reachable stable SemVer tag blocks baseline reuse. It was independently re-confirmed after Phase 7 against the corrected validator.
- I delivered Phase 7: failing tag-anchored specifications first, then removal of every `refs/heads/*` read from attestation, then the policy/runbook/ADR/governance/adapter-contract re-baseline plus a `refs/heads/v*` protected-ref rule in the repository-settings validator.
- I delivered Task 0.1, the release-governance I-VSD report, clearing the Phase 8 approval blocker.
- I delivered Task 8.1 as an executable dry-run specification and Task 8.3 as a synthetic publication-projection contract with `report-publication-drift`.
- I did not create a real baseline tag, release tag, release directory, commit, push, or protected-ref mutation.

CURRENT STATE
-------------
- Branch: `develop`; handoff HEAD: `f58291aece7f` (session start; no commit was created).
- 25/25 implementation tasks complete; all eight phases closed with green gates.
- Phase 7 removed every `refs/heads/*` read from attestation. A release now re-verifies after its
  branch advances, after the branch is deleted, and in a clone that fetched only `refs/tags/*`, in
  both SHA-1 and SHA-256 repositories, producing byte-identical evidence.
- Validation: full Release build 0 errors; release-engine tests 236/236; architecture tests 444
  total, 443 passed, 0 failed, 1 skipped; three real provider definitions validated with
  `adapter_validation_passed: providers=3`.
- The Phase 1 architecture gate and the Phase 5 literal-command gate, both previously blocked, are
  now closed. The four architecture failures were fixed by their owning workstreams; none was
  release-engineering scope and none was modified here.
- No repository ref was created, moved, deleted, or pushed. Production trust roots remain
  intentionally comment-only.

SUPERSEDED STATE (kept for provenance)
-------------------------------------
- Earlier handoff HEAD: `eee61969a4b6e6757242ae02dd748524ed540713`.
- Hot ledger: 16/18 implementation tasks complete. Task 6.2 remains unchecked because its version-agnostic follow-up has a worker DoneClaim but the independent re-verification timed out before a final verdict.
- Latest Task 6.2 follow-up evidence: focused baseline tests 21/21, prepare regression 1/1, full release-engine suite 197/197, scoped release CLI build 0 warnings and 0 errors.
- The prior independent Task 6.2 review file still says `needs-fix` because it predates the version-agnostic repair; do not treat that stale verdict as current confirmation.
- Task 6.1 final trust review is confirmed: focused 15/15, full release suite 187/187, and three-provider canonical checksum parity.
- Phase 5 release-engine behavior is code-confirmed, but formal build/test checkboxes remain open. 2026-08-15 SDK update: `dotnet workload repair` was run and the host SDK is healthy (`wasm-tools 10.0.109/10.0.100`); the `MSBuildEnableWorkloadResolver=false` workaround is no longer required. The host `MSB4242` half of this blocker is therefore gone and literal .NET commands are runnable again; the checkboxes stay open only because they must still be satisfied by an actual literal run, and because full solution builds have also encountered unrelated shared-worktree authorization API drift. No literal Phase 5 command was re-run as part of the SDK repair.
- Phase 1 architecture gate remains open on unrelated product/architecture failures recorded in the task ledger.
- Production trust roots remain intentionally comment-only. No real signer principals, custody owners, promoted artifact authority, approved first governed version, merged activation commit, or baseline tag exists.
- The shared worktree is extremely dirty across unrelated authorization, promotions, payments, registration, migrations, API, Blazor, and test workstreams. Preserve all unrelated files and never restore/reset them.
- Broad todo status at handoff: 3/7 completed and 4 remaining; implementation, verification, manual surface QA, and persistent closeout are not all complete.

PENDING TASKS
-------------
- **Task 8.2 (operator-blocked).** Execute the first governed milestone from
  `docs/RELEASE_RUNBOOK.md`: verify the signed `changelog-baseline-YYYY-MM-DD` tag, author
  `docs/releases/<version>/release.yaml` and `summary.md`, run `prepare`, `verify-candidate`,
  `verify-tag`, and `verify-main`, then confirm the tag re-verifies in a fresh tag-only clone
  offline. Requires a steward-approved version, a merged activation commit, real signer custody,
  and the repository branch ruleset covering `refs/heads/v*` with a `creation` rule. Do not
  fabricate any of these.
- **Task 8.4 (deferred).** Do not implement `open-maintenance-line` until a real backport to an
  already-released line exists. The documented manual `git switch -c release/<M>.<m> v<M>.<m>.<p>`
  is the complete solution until then.

KEY FILES
---------
- `dev/active/git-cliff-release-engineering/git-cliff-release-engineering-tasks.md` - authoritative hot checklist and blocked phase gates.
- `dev/active/git-cliff-release-engineering/git-cliff-release-engineering-context.md` - resumable decisions, verified milestones, and blockers.
- `dev/active/git-cliff-release-engineering/git-cliff-release-engineering-plan.md` - strategic plan and Task 6.2/6.3 acceptance criteria.
- `eng/release/src/ISLAMU.ReleaseEngineering/BaselineCommand.cs` - non-mutating signed baseline verification CLI.
- `eng/release/src/ISLAMU.ReleaseEngineering/BaselineEvidencePolicy.cs` - strict baseline name/evidence contract.
- `eng/release/src/ISLAMU.ReleaseEngineering/GitRepositoryValidator.cs` - authoritative Git topology, baseline reuse, and stable-tag checks.
- `eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ReleaseBaselineVerificationTests.cs` - signed baseline, SHA formats, recreation, and first-release tests.
- `.ci/scripts/validate-release-provider-adapters.cs` - strict three-provider transport-plan validator.
- `.ci/release/adapter-contract.md` - provider-neutral preview/final lane and checksum contract.
- `.omo/start-work/evidence/git-cliff-task-6.2-review.md` - stale pre-repair review; useful only for the original hardcoded-version finding.

IMPORTANT DECISIONS
-------------------
- ISLAMU owns policy, identity, ranges, and evidence; git-cliff is presentation-only and runs offline/no-exec from a promoted bundle.
- Candidate and final evidence are separate to avoid a tag/manifest hash cycle; exact `B` is the candidate, tag target, and newest-stable main target.
- `summary.md` is the sole human narrative; `release-notes.md` is fully generated with no split markers.
- Production trust is fixed-path and fail-closed. Test trust-root mutation is isolated in the test assembly; no shipped AsyncLocal/friend override remains.
- The first governed release version is a steward decision, never hardcoded. A non-SemVer baseline is allowed only before any reachable governed stable SemVer tag.
- Forgejo/Codeberg and Tangled current final workflows are validated `no-checkout-discovery` no-ops; activated release execution must migrate to explicit trusted default-branch proof. GitHub already uses environment-approved default-branch checkout.
- Tangled does not claim undocumented protected-ref/release controls; those actions require separate external operator evidence.
- Existing semantic-versioning documents are frozen pre-automation planning/history, not generated release truth.

EXPLICIT CONSTRAINTS
--------------------
- "do not care about backward compatibility at all we are in development mode !"
- "Every session must start with a green build."
- "Every file must start with a two-line `ABOUTME:` comment summary."
- "EF Core migrations are generated artifacts: Never hand-edit migration or model-snapshot files."
- "Never ingest third-party copyleft, source-available, proprietary, or otherwise incompatible source code, snippets, ASTs, SQL, migrations, tests, comments, or assets into implementation context or copy them into this repository."
- "Repositories return entities, never DTOs (map in handlers)."
- "Validators are manually instantiated (no DI)."
- "GET = `[AllowAnonymous]`, write = `[Authorize]`."
- "HAL links are the single source of truth for UI."

CONTEXT FOR CONTINUATION
------------------------
- Start by reading this handoff, context, and tasks; open only plan Task 6.2/6.3 sections and current changed symbols.
- Do not trust green test totals without manual CLI proof and independent AdversarialVerify. Several earlier green suites hid real trust, CAS, alias, and privilege defects.
- Do **not** use `MSBuildEnableWorkloadResolver=false` any more. 2026-08-15 SDK update: `dotnet workload repair` was run and the host SDK is healthy (`wasm-tools 10.0.109/10.0.100`); the `MSBuildEnableWorkloadResolver=false` workaround is no longer required. Run the literal phase commands directly, and keep literal phase checkboxes open until those exact commands pass.
- Current Task 6.2 implementation evidence is newer than its independent review. The immediate next action is a fresh independent review, not more implementation.
- Real baseline activation cannot happen in this worktree: it needs the eventual merged activation commit, approved first version, production signer roots, and an operator-created signed annotated tag. Synthetic/disposable tags are allowed for QA; never fabricate repository evidence.
- Use provider official docs only through the clean-room handoff already recorded under this workstream. Do not inspect provider source or copy workflow examples.
- Before stopping again, update this file, tasks, and context with the latest verdict and exact blockers.
