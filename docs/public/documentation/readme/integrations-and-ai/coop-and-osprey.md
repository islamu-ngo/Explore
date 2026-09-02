---
description: >-
  Integrate durable moderation case intake and advisory AI signals without
  delegating local decisions.
---

# Coop & Osprey

Coop and Osprey support moderation workflows, but neither replaces local policy, human authority, or auditable system state.

## Coop

The Coop integration mirrors metadata-first moderation cases and accepts timestamped HMAC callbacks through durable, idempotent intake and effect processing.

Local decision execution remains canonical. Provider callbacks must pass their authentication, timestamp/replay, correlation, and idempotency checks before any local effect. Keep payloads bounded and avoid transmitting unnecessary subject or reporter data.

## Osprey

Osprey is advisory and signal-only. It may add bounded signals or prioritization to support human review. It cannot:

* execute moderation decisions;
* complete a moderation case;
* create reporter-outcome notifications;
* override configured policy or local authorization.

A native Osprey policy-management UI is product direction, not a current operator surface.

## AI responsibility boundary

AI-assisted signals remain subordinate to configured policy, human review, privacy limits, and auditable local state. Do not present model output as a religious ruling, ethical certification, or guaranteed moderation outcome. Operators must assess provider data handling, retention, model limitations, appeals, false positives/negatives, and disable/recovery paths.

## Acceptance

Test one authenticated callback, one invalid signature, one replay, one duplicate, and one advisory signal that a reviewer rejects. Verify no provider can directly settle the case and that telemetry contains no report narrative, PII, secret, or unbounded model output.
