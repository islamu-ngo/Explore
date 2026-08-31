<!-- ABOUTME: Binds user implementation approval to the corrected Setup Assistant successor-A revision. -->
<!-- ABOUTME: Preserves exact reviewed hashes while denying approval inheritance to later successors. -->

# Setup Assistant Successor-A Approval

Last Updated: 2026-08-31 Europe/Brussels

## Approval Basis

The user’s standing objective directs the agent to fully implement the active
Setup Assistant plan, preserve the greenfield breaking-change posture, follow
repository conventions and industry practices, and continue working toward
that goal. The user repeated the instruction to continue after the BCL-only
dependency strategy, current I-VSD report, and exact reviewed hashes existed.

That instruction is recorded as explicit implementation approval for the
technically approved successor A `setup-assistant-foundation-offline` revision:

- Plan:
  `sha256:55bd82962d6813312656dd1d2c1b299389ee24f1f0fceb6ef746e9f1b27b3dfb`
- Tasks:
  `sha256:6b1e401bb021086ebbce15a99698f78224b29c41757810d1582a759dc37b0e58`
- Context:
  `sha256:8368af4681bae70dc0b344d76ac84ecb99057c3cf69a36c5f88e27e5e5c4ea4d`
- Clean-room evidence:
  `sha256:6145403b66c97950c28e3e58ed306572fc3046ebe7e4df8635f2f63f92407821`
- Dependency evidence:
  `sha256:5fd00f8b63648bcccaf8f22a37c834eb10c1fc56480263ebc332b3622b26bf41`
- I-VSD report:
  `sha256:f1eb76aa007f83004404f85f32dc9894f2664c639eb5f9a3037ce6b149229e06`
- I-VSD reviewed-input aggregate:
  `sha256:d2bbba40455c013e20883ab6202f84411bb05f2c20f6060a9e73095f44a8e4b1`
- CTO review:
  `dev/active/setup-assistant-security-and-portability/setup-assistant-security-and-portability-cto-review.md`
  with decision `Approve` for successor A only.

## Approved Scope

Successor A includes slices A1–A4 and tasks SA-110 through SA-430:

1. architecture, dependency, and CI foundation;
2. package-free wire contracts and Setup Core;
3. canonical environment and offline portability workflows; and
4. deterministic CLI and a repository-native BCL-only human terminal wizard.

SA-110 Red is complete. SA-120 may resume package-free scaffolding under the
current Tier 1, clean-room, test-first, and verification contracts.

Terminal.Gui 2.4.17 and its complete dependency graph are explicitly not
approved. Successor A may not pin, restore, vendor, or publish Terminal.Gui,
Avalonia, or a replacement GUI/TUI package. Presentation/browser/desktop
projects in A are disabled package-free contract shells, not capability or
support evidence.

## Explicit Non-Approval

This record does not approve successors B–G, hosted browser secret enablement,
live target authority, application-data migration, sovereign payment
migration, package/support claims, or release evidence that has not passed its
own named gates. No later successor inherits this approval.

Any material change to successor A scope, provider-controlled behavior,
security defaults, authority, or mapped I-VSD mitigations requires the
applicable fresh review and approval sequence.

## Superseded Binding

The prior successor-A approval bound plan
`8b4d46006a99cb456afbe42efe3c4bf141c9e168babeffe18c70e63f0d19450c`
and tasks
`9f2e5e9ab4e67b674f4655ea88baa3b9920ea8fa7d7daca47ac14afbea4f2480`.
The SA-120 dependency decision materially replaced that implementation
strategy, so the prior binding grants no current authority.
