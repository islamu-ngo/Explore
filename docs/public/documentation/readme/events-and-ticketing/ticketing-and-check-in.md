---
description: Operate registration, admission credentials, recovery, and online server-authoritative gates.
---

# Ticketing & Check-In Architecture

Registration, admission issuance, and day-of-event check-in are distinct lifecycle stages. A registration records an attendee’s sign-up or purchase intent; an admission entitlement represents the cryptographic token presented at the venue gate.

---

## 1. Admission Issuance & Cryptographic Hashing

* **Entitlement Production**: Triggered automatically upon completing a free event registration or upon receiving verified payment settlement for paid events (see [Paid Events & Payouts](paid-events-and-payouts.md)).
* **Credential Protection**: The database **never** stores plaintext QR ticket tokens or attendee PII in admission tables. Tickets store versioned **HMAC-SHA-256 lookup digests**, guaranteeing that a compromised database snapshot cannot be used to forge valid event entry passes.
* **Session-Level Admissions**: Tickets can optionally expand into session-specific entitlements for multi-track conferences.

---

## 2. Lost-Ticket Recovery

When an attendee requests lost ticket recovery:
* The system responds with an indistinguishable `202 Accepted` response (preventing user enumeration).
* A single-use recovery capability is dispatched via [Durable SMTP Email](../communications-and-notifications/email-smtp.md).
* Accessing the recovery link automatically rotates the ticket token and invalidates the previous QR code atomically.

---

## 3. Server-Authoritative Gate Check-In

* **Online Validation Only**: Gate validation is strictly server-authoritative. If internet connectivity drops at the venue, scans fail closed. The platform intentionally does not implement unverified offline sync queues to prevent double-entry fraud.
* **Target Scoping**: Scanner tokens are cryptographically bounded to a specific event or session target.
* **HATEOAS Affordance Gating**: The volunteer check-in interface displays "Check-In", "Undo", or "Override" buttons strictly based on server-issued [HAL links](../security-and-identity/authorization.md#the-golden-rule-of-client-ui-affordances).

---

## Pre-Door Gate Checklist

1. Verify that email dispatch is healthy for ticket delivery (see [Email SMTP](../communications-and-notifications/email-smtp.md)).
2. Issue a test free and paid ticket through the production path.
3. Scan the test QR code and verify that a second scan flags an idempotent duplicate warning.
4. Verify venue internet connectivity (Wi-Fi or cellular) before doors open.

---

## Related Guides & Next Steps

* **[Paid Events & Payouts](paid-events-and-payouts.md)** — Configure ticket pricing, Stripe Connect, and attendee refund workflows.
* **[Modular Event Aspects](modular-event-aspects.md)** — Relational sector models for tracks, speakers, and prayer schedules.
* **[Email SMTP Configuration](../communications-and-notifications/email-smtp.md)** — Ensure reliable delivery of confirmation and recovery emails.
* **[Authorization & HAL Affordances](../security-and-identity/authorization.md)** — Understand how volunteer gate access is authorized.
