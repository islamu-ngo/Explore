---
description: >-
  Operate registration, admission credentials, recovery, and online
  server-authoritative gates.
---

# Ticketing & Check-In

Registration, admission, and check-in are separate lifecycles. A registration records participation or purchase intent; an admission is the entitlement presented at a gate.

## Admission issuance

A confirmed free registration or an exactly reconciled paid registration can produce admission entitlements. Ticket purchases may expand into session-level admission slots.

Admission aggregates do not store plaintext bearer credentials or attendee PII. They store versioned HMAC-SHA-256 lookup digests so a database read does not reveal a usable ticket.

## Lost-ticket recovery

Recovery returns an indistinguishable `202 Accepted`, issues a single-use capability, and rotates the admission credential atomically. Responses do not disclose whether a particular person or ticket exists.

## Check-in authority

Check-in is server-authoritative and online-only. Connectivity loss denies validation. There is no offline validation/submission queue and no implemented emergency override.

Scanner capabilities are scoped to one exact admission target. Check-in and undo are append-only facts with bounded reason codes, idempotency, and no-PII audit data. The UI exposes check-in, undo, scanner capability, stop, restore, and reconciliation only when current HAL links permit them.

## Gate acceptance

Before opening doors:

1. issue one free and one paid entitlement through their real authority paths;
2. scan one valid credential;
3. repeat it to verify idempotent duplicate handling;
4. present a credential for the wrong target;
5. exercise lost-ticket recovery and prove the old credential is invalid;
6. check undo reason and audit output;
7. confirm network loss denies validation rather than caching an allow.

Plan venue connectivity accordingly. Do not advertise offline admission unless the product later ships an explicit cryptographic/offline trust model.
