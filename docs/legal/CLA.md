<!-- ABOUTME: Contributor License Agreement for inbound ISLAMU Event contributions. -->
<!-- ABOUTME: Grants ISLAMU nonprofit broad rights to relicense and offer the platform for social-impact needs. -->

# ISLAMU Event Contributor License Agreement

> **Version:** 1.0  
> **Status:** Active pending legal review refinement  
> **Owner:** ISLAMU nonprofit | Platform/Ops  
> **Maintainer of record:** Amir Akrari  
> **Purpose:** Preserve ISLAMU nonprofit's ability to provide, sell, sublicense, or relicense ISLAMU Event under alternative terms when the default project license would prevent a legitimate social-impact deployment.  

A new CLA version is published by bumping the `Version` line above and updating the `path-to-signatures` version segment in `.github/workflows/cla.yml` (e.g. `signatures/v2/cla.json`). Earlier signatures remain valid only for the CLA version under which they were recorded; a contributor must re-sign when the version changes.

This Contributor License Agreement (CLA) applies to contributions submitted to ISLAMU Event, including API, Blazor, infrastructure, workflow, documentation, configuration, generated artifacts, tests, and related project materials.

This document is not personal legal advice. Contributors should only sign if they have the rights needed to grant these permissions.

## Agreement

By posting the CLA signature comment on a pull request, you and ISLAMU agree to the terms below.

## Definitions

**Contribution** means any code, documentation, design, test, configuration, workflow, generated artifact update, or other material you submit intentionally to ISLAMU Event.

**You** means the individual contributor and, if applicable, the organization on whose behalf the contribution is submitted.

**ISLAMU** means the ISLAMU nonprofit organization, its authorized successors, affiliates, sublicensees, distribution partners, and the ISLAMU Event maintainer of record acting for ISLAMU Event.

**Amir Akrari** means the ISLAMU Event maintainer of record, acting jointly with ISLAMU as the recipient of the rights granted below.

## Copyright License Grant

You and ISLAMU agree: you grant ISLAMU and Amir Akrari the ability to use the Contributions in any way. You hereby grant a perpetual, non-exclusive, worldwide, fully paid-up, royalty-free, irrevocable copyright license to reproduce, prepare derivative works of, publicly display, publicly perform, sublicense, and distribute your Contribution and such derivative works. You are able to grant these rights. You represent that you are legally entitled to grant the above license. The Contributions are your original work. You represent that the Contributions are your original works of authorship.

In addition to the grant above, you grant ISLAMU and Amir Akrari a worldwide, perpetual, irrevocable, non-exclusive, royalty-free, transferable, and sublicensable license to use your Contributions in any way needed for ISLAMU Event.

This includes the rights to copy, modify, prepare derivative works, publish, distribute, publicly perform, publicly display, host, operate, provide as a service, sell, offer for sale, sublicense, relicense, and otherwise exploit the Contributions as part of ISLAMU Event or related offerings.

This grant explicitly allows ISLAMU and Amir Akrari to provide ISLAMU Event, including its API, Blazor applications, infrastructure, documentation, deployment assets, and related components, under any license or commercial terms chosen by ISLAMU. That includes open-source, source-available, proprietary, commercial, nonprofit, humanitarian, public-sector, and special social-impact licensing arrangements.

## Patent License Grant

If your Contribution would otherwise be covered by patent claims that you can license, you grant ISLAMU, Amir Akrari, and downstream recipients a worldwide, perpetual, royalty-free patent license to make, have made, use, sell, offer for sale, import, and otherwise transfer the Contribution as part of ISLAMU Event.

## Contributor Ownership

You keep ownership of your Contributions. This CLA does not prevent you from using, licensing, or publishing your own Contributions elsewhere.

## Representations

You confirm that:

- you have the legal right to submit the Contribution and grant this CLA;
- your Contribution is your original work or you have permission to submit it;
- if your employer, client, school, or another organization owns rights in the Contribution, you are authorized to submit it under this CLA;
- you are not knowingly submitting material that violates another party's rights;
- you will identify third-party code, assets, generated output, or license restrictions that are not obvious from the Contribution.

## Moral Rights

To the extent allowed by law, you waive or agree not to assert moral rights that would interfere with ISLAMU's exercise of this CLA. Where waiver is not allowed, you consent to ISLAMU exercising the rights granted above.

## No Warranty

Contributions are provided as-is, without warranties or conditions of any kind, unless required by applicable law or separately agreed in writing.

## How To Sign

Every non-bot contributor to a pull request must sign this CLA by posting the following exact comment on the pull request:

```text
I have read the CLA Document and I hereby sign the CLA
```

The ISLAMU CLA workflow (powered by [cla-assistant/github-action][cla-action-link], pinned at `contributor-assistant/github-action@ca4a40a7d1004f18d9960b404b97e5f30a505a08 # v2.6.1`) records each signature in `signatures/v1/cla.json` on the `main` branch. The recorded entry stores the contributor's GitHub username, user ID, the pull request number, the signing comment ID and body, and an ISO-8601 timestamp.

If the CLA version changes, the workflow switches the signatures file to a new version segment (for example `signatures/v2/cla.json`) and contributors must re-sign the new version.

To re-run the CLA check after signing, post a comment containing only `recheck`.

The CLA workflow runs on `pull_request_target` and `issue_comment` events only. It does not checkout, build, test, or execute pull-request head code.

[cla-action-link]: https://github.com/contributor-assistant/github-action
