---
description: >-
  Operate organizer-direct Stripe Connect payments, reconciliation, refunds, and
  payout boundaries.
---

# Paid Events & Payouts

Stripe Connect is the initial payment adapter. The implemented commercial model is organizer-direct rather than platform escrow.

## Organizer-direct flow

1. The organizer completes provider-hosted onboarding.
2. Checkout is created in the organizer's provider context.
3. Signed provider events and reconciliation establish payment truth.
4. Provider-managed schedules govern payout timing.

A browser success or cancellation page is navigation only. It never establishes terminal payment state. Paid publication and checkout fail closed until required instance/tenant/operator identity, payment governance, provider connection, and policy facts are complete.

## Refunds

Durable provider-backed workflows cover:

* buyer refund requests;
* material-change responses;
* organizer create/retry actions;
* campaign reads and bounded resumption;
* reconciliation with provider evidence.

A refund is complete only when provider-confirmed evidence says so. Pending allocation or an accepted request must not be presented as refunded.

## Payout boundary

Provider-managed schedules determine payout timing. ISLAMU Event does not claim escrow, accounting, tax calculation, invoice issuance, banking, universal provider liability, or guaranteed settlement dates. `ProtectedDelayedPayout` is deferred; do not present it as an active protection model.

Deployment operators and organizers must define legal identity, merchant/provider agreements, fee/refund policy, support ownership, tax/invoice responsibility, and incident recovery for their jurisdiction.

## Acceptance

Exercise provider onboarding, checkout, signed webhook validation, reconciliation after a missing/delayed callback, failed and successful payment, buyer refund request, organizer action, retry, and provider-confirmed completion. Keep provider IDs, raw errors, idempotency material, and PII out of public responses and operational evidence.
