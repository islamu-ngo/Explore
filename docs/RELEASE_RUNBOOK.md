<!-- ABOUTME: Provides the current manual release procedure and future governed-release operator flow. -->
<!-- ABOUTME: Keeps operator approval explicit while defining trust, evidence, and recovery boundaries. -->

# Release Runbook

> **Status:** Current manual procedure active; governed flow is prospective
> **Owner:** Platform/Ops and release operators

## Current procedure

Until a trusted release bundle, release-engine verification, signer policy, provider
protection evidence, and advisory dry run are implemented, operators MUST use
[RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md). The current process is manual SemVer
tags and manually authored GitHub Releases. Do not invoke imagined release-engine
commands, claim automatic approval, or create generated changelog writes on
`develop`.

Generate the existing durable evidence bundle only as the checklist directs:

```sh
dotnet run .ci/scripts/generate-release-evidence-bundle.cs -- artifacts release-evidence
```

## Future governed release flow

1. An operator selects a governed `v<major>.<minor>` line and prepares only
   `release.yaml` and `summary.md`; public high-impact facts are supplied through
   validated change fragments.
2. The trusted bundle validates complete Git objects, policy, range, version,
   impacts, and public inputs. It normalizes context and calls the promoted
   git-cliff renderer with the `VerifiedTrustedBundle` capability returned by
   successful bundle verification. Candidate jobs never pass raw config, lock,
   executable, or digest inputs into rendering. The renderer passes
   `--config <trusted>`, `--from-context <context>`, `--offline`, and `--no-exec`
   from an isolated non-Git working directory after rechecking bundle-owned bytes.
3. The preparation command generates `release-notes.md` and creates the reviewed
   preparation commit `B`. The message explicitly explains its release-metadata
   changelog skip so the generated note does not include itself.
4. The authoritative bundle verifies candidate `B` and emits deterministic
   pre-tag candidate evidence. Review preserves `B` through a fast-forward-only
   compare-and-swap update; any replacement requires regeneration and review.
5. An authorized release operator creates an SSH-signed annotated tag targeting
   exactly `B`. Tag verification records signer, tag object ID, candidate digest,
   policy/tool hashes, and note/context hashes in final evidence.
6. For the newest stable release only, protected `main` advances by normal
   fast-forward to exactly `B`. Prereleases and older-line patches do not move
   `main` backwards.
7. The provider adapter retains artifacts and may publish a derived enriched view,
   but it cannot alter canonical notes, identity, or approval.

## Required controls before activation

### Preparation command

After `docs/releases/<version>/release.yaml` and `summary.md` are reviewed on the
governed release branch, run:

```sh
dotnet run --project eng/release/src/ISLAMU.ReleaseEngineering/ISLAMU.ReleaseEngineering.csproj -- \
  prepare docs/releases/<version>
```

The protected environment supplies the promoted bundle, receipt, detached
signature, principal, expected manifest SHA-256, and expected bundle, policy,
config, and trust versions through `ISLAMU_RELEASE_TRUSTED_BUNDLE`,
`ISLAMU_RELEASE_PROMOTION_RECEIPT`, `ISLAMU_RELEASE_PROMOTION_SIGNATURE`,
`ISLAMU_RELEASE_PROMOTION_PRINCIPAL`, `ISLAMU_RELEASE_MANIFEST_SHA256`,
`ISLAMU_RELEASE_BUNDLE_ID`, `ISLAMU_RELEASE_BUNDLE_VERSION`,
`ISLAMU_RELEASE_POLICY_VERSION`, `ISLAMU_RELEASE_CONFIG_VERSION`, and
`ISLAMU_RELEASE_TRUST_VERSION`.
The command verifies that promotion before rendering, reads the descriptor and
summary, derives the complete local Git range from `Previous-Published-Tag` to
`refs/heads/<Line>`, validates linked public fragments and impact evidence,
atomically creates `release-notes.md`, and writes canonical
`release-context.v1.json` next to the release inputs. The generated notes always
use this order: maintainer summary, release-visible details only, then complete
provider-neutral full commit range. Empty detail/impact sections are omitted.
It prints the exact `docs(release): prepare <version>` commit message with
`Changelog: skip` and `Changelog-Reason: release metadata commit`; it never
commits, tags, pushes, publishes, invokes candidate code, or uses the network.
An identical rerun is a byte-idempotent success. A different existing generated
file, path escape, symbolic link, context/range/fragment drift, renderer failure,
or write failure stops without changing `release.yaml` or `summary.md`.

### Candidate verification command

After the reviewed preparation commit `B` is created and the governed release-line
branch still points at it, run the promoted bundle verifier against that exact full
commit object:

```sh
dotnet run --project eng/release/src/ISLAMU.ReleaseEngineering/ISLAMU.ReleaseEngineering.csproj -- \
  verify-candidate docs/releases/<version> <full-B-oid>
```

The command reads the same protected environment variables as `prepare`. It validates
the local repository without lazy fetches or replacement objects, requires
`refs/heads/<Line>` to equal `<full-B-oid>`, requires `B` to be the terminal commit in
the descriptor-selected range, and requires `B`'s terminal footers to be exactly
`Changelog: skip` and `Changelog-Reason: release metadata commit`. It recomputes the
release context through `B`, rerenders the notes with the separately promoted trusted
bundle, and compares both committed generated artifacts byte-for-byte. It then writes
or verifies `docs/releases/<version>/release-candidate.v1.json`.

`release-candidate.v1.json` is pre-tag evidence only. It records schema/object format,
full `B` and range commit IDs, version/line/date, base and previous tag commit IDs,
the exact parent/expected-old and branch/expected-new IDs, trusted
bundle/policy/config/trust/tool hashes, and release descriptor, fragment, summary,
context, and note hashes. It deliberately omits tag object IDs, current time,
provider metadata, identities, emails, raw commit bodies, tokens, and secrets.

Successful output is one bounded line:

```text
release_candidate_verified: docs/releases/<version>/release-candidate.v1.json
```

Failures are also bounded and use stable diagnostic codes, for example:

```text
verify_candidate_failed: git_candidate_not_release_branch_head
verify_candidate_failed: candidate_terminal_commit_not_release_metadata_skip
verify_candidate_failed: candidate_release_context_mismatch
verify_candidate_failed: candidate_release_notes_mismatch
verify_candidate_failed: candidate_manifest_stale
```

Any branch/ref movement, squash/rebase/merge replacement, wrong parent/range,
descriptor, fragment, policy, config, tool, bundle, context, summary, or note drift,
shallow/partial/replace/graft state, object-format mismatch, stale candidate manifest,
or dirty committed generated artifact stops before tagging or publication.

### Tag message and final tag verification commands

After `verify-candidate` succeeds, generate the annotated-tag message from canonical
release sources. Operators do not edit the tag narrative independently:

```sh
dotnet run --project eng/release/src/ISLAMU.ReleaseEngineering/ISLAMU.ReleaseEngineering.csproj -- \
  tag-message docs/releases/<version> > /tmp/islamu-release-tag-message.txt
```

The message contains only the canonical tag name, version, release line, exact
candidate `B`, candidate-manifest SHA-256, and release-note SHA-256. It has no
independently editable summary or final-manifest digest and omits current time,
provider UI state, identities, emails, raw commit bodies, tokens, and secrets.

An authorized operator then creates the SSH-signed annotated tag outside the verifier:

```sh
git -c gpg.format=ssh -c user.signingKey=/path/to/release-signing-key \
  tag -s v<version> <full-B-oid> -F /tmp/islamu-release-tag-message.txt
```

Verify the named tag locally; the command resolves and records its full tag object ID:

```sh
dotnet run --project eng/release/src/ISLAMU.ReleaseEngineering/ISLAMU.ReleaseEngineering.csproj -- \
  verify-tag docs/releases/<version> v<version>
```

The promoted verifier retains the internal exact-object seam for prior published tag
object drift checks; the public command accepts no caller-selected trust root or
object override. `verify-tag` verifies the tag object is an
annotated tag, the OpenSSH signature validates against the promoted allowed-signers
root, the promoted signing policy authorizes the release role/principal/algorithm/date,
the tag's internal name is exactly `v<version>`, the target is exactly `B`, the release
line and descriptor fields still match, the candidate manifest digest and source-file
hashes still match, and the existing `release-evidence.v1.json` is byte-identical on
rerun. It writes or verifies:

```text
docs/releases/<version>/release-evidence.v1.json
```

Successful output is one bounded line:

```text
release_tag_verified: docs/releases/<version>/release-evidence.v1.json
```

Stable failure diagnostics include:

```text
verify_tag_failed: release_tag_not_annotated
verify_tag_failed: release_tag_signature_invalid
verify_tag_failed: release_signer_unauthorized
verify_tag_failed: release_signer_revoked
verify_tag_failed: release_signer_not_current
verify_tag_failed: release_signer_algorithm_forbidden
verify_tag_failed: release_tag_wrong_target
verify_tag_failed: release_tag_name_mismatch
verify_tag_failed: release_tag_message_mismatch
verify_tag_failed: release_candidate_manifest_drift
verify_tag_failed: release_notes_hash_mismatch
verify_tag_failed: release_evidence_manifest_stale
```

Do not delete, recreate, or move tags to repair a failure. Correct the source of drift,
discard stale candidate/final evidence, and repeat the governed candidate and tag flow.

### Stable main verification command

After final tag verification succeeds, resolve the full old `origin/main` commit and the
final tag object ID locally, then verify the protected-ref action before any provider
adapter updates `main`:

```sh
old_main_oid=$(git rev-parse --verify refs/remotes/origin/main^{commit})
tag_object_id=$(git rev-parse --verify refs/tags/v<version>^{object})
dotnet run --project eng/release/src/ISLAMU.ReleaseEngineering/ISLAMU.ReleaseEngineering.csproj -- \
  verify-main docs/releases/<version> "$old_main_oid" "$tag_object_id"
```

The command reads only local Git objects, refs, and `release-evidence.v1.json`. It never
fetches, pushes, signs, tags, commits, updates refs, executes candidate code, or calls a
forge API. Every successful stable action requires `refs/remotes/origin/main` to still
equal the supplied old full OID. Newest stable moves also require the final evidence
target `B` to be a normal fast-forward descendant. If `main` is already at `B`, the
command is idempotent. Older stable-line patches may publish but emit `no-main-move`;
prereleases never advance `main`.

Successful output is one bounded line with exact object IDs and a runbook action:

```text
release_main_verified: action=move-main old=<old-main-oid> new=<full-B-oid> tag=v<version> instruction=update-main-fast-forward
release_main_verified: action=already-at-target old=<full-B-oid> new=<full-B-oid> tag=v<version> instruction=no-op-main-already-at-release
release_main_verified: action=no-main-move old=<old-main-oid> new=<full-B-oid> tag=v<older-version> instruction=publish-release-without-main-update
```

Stable failure diagnostics include:

```text
verify_main_failed: release_main_oid_not_full
verify_main_failed: release_main_expected_old_missing
verify_main_failed: release_main_cas_mismatch
verify_main_failed: release_main_non_fast_forward
verify_main_failed: release_main_tag_object_mismatch
verify_main_failed: release_main_evidence_target_mismatch
verify_main_failed: release_main_prerelease_no_move
```

The provider adapter, not `verify-main`, performs the protected compare-and-swap update
after this local proof. On any failure, discard the protected-ref action and repeat the
candidate/tag/main verification from current local objects.

- The authoritative job MUST download and verify a separately promoted bundle before
  processing candidate data. An operator/provider supplies an immutable canonical
  promotion receipt and detached SSH signature separately from both the bundle and
  candidate checkout. The previously promoted verifier resolves its promoter trust
  root from its own fixed protected application directory, never from a request,
  candidate path, current working directory, or bundle under verification. The signed
  receipt binds the manifest, bundle, policy, config, and trust digests and versions;
  reusing it for the same immutable bundle is idempotent, while using it for any
  different bundle fails. No caller assertion or candidate checkout copy is
  authoritative. Candidate jobs MUST have no signing, publication, deployment,
  registry-write, OIDC, or protected-ref credentials.
- The bundle MUST include the release engine, policy, renderer config, tool pin, and
  SSH signer roots. The provider MUST retain protected-ref and environment approval
  evidence for final operations.
- `B`, release-line head, candidate record, tag target, committed notes, and stable
  `main` target MUST be checked as one exact full-object identity.
- Public canonical artifacts MUST be deterministic and provider-neutral. Capture
  required hashes and inputs without wall-clock, provider, author, raw-body, or token
  data.
- Restricted security inputs MUST remain in the embargo lane outside the public
  checkout. If disclosure is not authorized, stop before public generation or tag.

### Trusted-bundle bootstrap and upgrades

Genesis promotion is a required operator action because no earlier release engine can
validate the first bundle. Independent reviewers must bind the exact source and tool
hashes to a Release build, focused release-engine tests, SBOM, checksums, protected
approval, and an SSH-signed annotated tooling tag. Later upgrades use the same
separate promotion lane and cite the previously promoted bundle; candidate checkout
content is never promotion evidence.

Final attestation receives the promoted bundle plus its separately stored canonical
receipt and detached signature. The already promoted verifier uses only its protected
runtime trust root to verify the receipt before using the receipt-bound manifest
SHA-256, then verifies canonical manifest bytes, required and complete file sets,
  every file hash, and policy/config/trust versions and digests. Reusing the same signed
  receipt for the exact same manifest is an idempotent read-only verification, not a
  replay decision; the verifier stores no receipt registry and accepts no caller replay
  state. Manifest, receipt, signature, trust-root, file-count, per-file, total-size,
  path-length, path-depth, hardlink/link-count, and enumeration limits fail closed.
  Traversal, root aliases, symbolic links, duplicate normalized paths, and
  case-insensitive path collisions also fail before payload hashing. Final jobs must
  reverify the promoted bundle immediately before use from immutable promoted storage.
  Candidate executables run only in an unprivileged preview lane.

Task 5 tag verification must derive `SshTagAuthorizationRequest` signer, signature,
tag-kind, verification-date, and tag-object facts from local Git objects and local
OpenSSH verification evidence. Forge badges, provider UI states, or hosted release
metadata are never signer evidence.

Production activation remains blocked: no real release/tooling principals, public
keys, custody/rotation owners, or promoted artifact store and retention authority are
recorded. `eng/release/trust/allowed-signers` and the verifier-owned
`eng/release/trust/promotion-allowed-signers` are intentionally comment-only until a
reviewed promotion adds real principals and public keys, so activation returns
`trusted_bundle_promotion_authority_not_configured` and fails closed.

### Restricted security input

Embargo input is a separate access-controlled input mounted only after candidate code
has stopped. It must not enter the public checkout, candidate bundle, canonical
context, notes, manifests, checksums, retained diagnostics, logs, or provider
metadata. Diagnostics use bounded codes only and never echo paths, identities,
secrets, provider data, restricted values, or raw exception text. After disclosure
authorization, only the reviewed public disposition and public advisory reference may
cross into canonical generation, and neither field may exactly alias restricted
details, secret material, identities, storage paths, or provider metadata after
Unicode and whitespace normalization; otherwise the release stops.

## Stop and recovery rules

- Stop on missing Git objects, shallow/partial history, ref replacement, policy or
  bundle mismatch, renderer drift, unauthorized/unsigned/replaced tags, or a moved
  branch. Correct the input or promote a new bundle; do not bypass the check.
- If `B` changes after candidate verification, discard its candidate evidence and
  return to generation/review. Do not retarget a tag or force-push `main`.
- If an embargo field appears in a public artifact, stop publication, revoke public
  artifact access where possible, preserve incident evidence without reproducing the
  restricted value, and use the security incident process before retrying.
- If provider transport fails after local verification, retain local evidence and
  resolve the protected adapter failure. The provider adapter is not permitted to
  regenerate or reinterpret the canonical release.
