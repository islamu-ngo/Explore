<!-- ABOUTME: Architectural decision record for admission credentials, check-in, transfer, and ticket recovery. -->
<!-- ABOUTME: Defines opaque QR credentials, append-only admission facts, scoped scanners, and rotation rules. -->

# ADR-023: Admission Credential, Check-In, Transfer, And Recovery

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-08-13 |
| **Deciders** | ISLAMU Event Platform - Architecture, Security, Registration, and Accessibility workstreams |
| **Supersedes** | The admission, check-in, transfer, and recovery records in `deferred-design-records.md` |
| **Superseded by** | - |

## Context

Orders, participants, assignments, payment state, and admission are different lifecycles. A public ticket identifier, email address, order display ID, or participant record is unsuitable as an admission bearer credential. Admission must support revocation, rotation, entitlement-specific targets, concurrent scans, compensating undo, transfer, and generic recovery without rewriting payment, purchaser, consent, or audit history.

Browser QR detection is not a portable correctness boundary. Camera detection can improve the operator experience where available, but HID scanners and manual entry must remain first-class paths to the same server-authoritative validation.

## Decision

### Admission ticket and credential

1. A confirmed free order or reconciled successful paid order issues one `AdmissionTicket` for each concrete ticket assignment. Issuance is retry-idempotent.
2. The ticket owns a versioned, high-entropy opaque credential. Its QR representation contains no PII, email, amount, order display ID, participant identity, tenant identity, or authorization claims.
3. Persistence stores only a keyed lookup digest and bounded credential metadata. Plaintext is revealed only through the authorized delivery path and never logged or retained as ordinary evidence.
4. Display IDs are for support and presentation only. They never authorize ticket access, recovery, transfer, or admission.
5. Reissue, transfer acceptance, and credential compromise revoke the prior credential and rotate to a new one. Copied prior QR images then fail closed.

### Targets, check-in, and undo

1. Admission targets are Event, event day, or session scopes derived from the ticket's published entitlements and schedule.
2. `AdmissionCheckInEvent` is append-only and records check-in or compensating undo with ticket, target, actor or scanner, reason, and timestamp. Active state is derived and atomically constrained; an undo never deletes history.
3. Duplicate scans are idempotent. Concurrent scans cannot create two active admissions for the same credential and target. Revoked, wrong-tenant, wrong-event, wrong-target, cancelled, unpaid, or ineligible tickets fail closed.
4. Batch scanning returns one result per item so valid admissions are not rolled back by unrelated invalid items.
5. Camera, HID keyboard, and manual entry invoke the same API command. Client duplicate suppression and audio feedback are usability aids, not correctness mechanisms.

### Scanner authority and privacy

1. Staff normally authenticate through the existing BFF/API authorization model. A scanner-only client may use a separate opaque, hashed, expiring, revocable capability scoped to tenant, event, target, actions, and expiry.
2. Scanner capabilities never grant roster, order, payment, refund, attendee-answer, or unrelated event authority. HAL links remain the client source of truth for issuance and operational actions.
3. Scan logs, metrics, ProblemDetails, and audit exports exclude credential plaintext, participant PII, answers, raw camera content, and exact device fingerprints.
4. Operator feedback is visual and announced accessibly; it is never color-, sound-, or toast-only.

### Transfer, correction, and recovery

1. Holder transfer creates a bounded offer with separately protected recipient PII and a single-purpose, hashed, expiring acceptance capability. It is not an ownership rewrite of the order or payment.
2. Atomic acceptance validates policy and participant requirements, updates future holder or assignment state, revokes the old credential, and issues a new credential. Price, purchaser, payment, refund, consent, and audit history remain unchanged.
3. Organizer correction and reissue are separate authorized, audited actions. They do not impersonate a holder transfer.
4. Guest lookup and resend return indistinguishable responses for present and absent email addresses. Recovery uses random, single-use, expiring, rotatable capabilities; email and display IDs never authorize access.
5. Authenticated users receive only account-scoped ticket self-service, further narrowed by HAL relations.

### Online-first boundary

Initial admission validation is online and server-authoritative. Offline-verifiable signed credentials remain deferred until an extension ADR defines signing-key custody, hardware or service boundaries, rotation, revocation distribution, clock skew, compromised-device recovery, and offline audit convergence.

## Rejected Alternatives

1. Encoding a public attendee, order, or ticket ID as the QR credential.
2. Encoding PII or durable authorization claims in QR content.
3. A non-rotatable credential that survives transfer or reissue.
4. A mutable checked-in Boolean or deletion-based checkout.
5. Client-side duplicate suppression as admission correctness.
6. Short IDs as scanner or roster capabilities.
7. Camera-only scanning or reliance on experimental browser detection.
8. Email-based lookup that reveals whether a ticket exists.
9. Transfer that rewrites purchaser, payment, price, or consent evidence.
10. Offline signing before key lifecycle and revocation design is accepted.

## Consequences

- Admission can be revoked and rotated independently from order and participant history.
- Append-only events preserve operational accountability while atomic current state prevents duplicate admission.
- Scanner clients remain least-privilege and accessible across camera, HID, and manual workflows.
- Online validation requires service availability; offline breadth remains an explicit future security decision.
- QR encoder/decoder selection requires a separate clean-room dependency and outbound-license gate before Phase 20 implementation.

## Related

- `dev/active/registration-data-collection/registration-data-collection-plan.md` D26-D28
- ADR-017: Event Participation Authority Model
- ADR-018: Registration Order And Ticketing Aggregate
- `docs/SECURITY-MODEL.md`
- `docs/ACCESSIBILITY.md`
