<!-- ABOUTME: I-VSD authority report for registration commerce and admission phases 18C through 25. -->
<!-- ABOUTME: Traces provider-controlled risks, mitigations, evidence gaps, and escalation boundaries. -->

# I-VSD Registration Data Collection, Commerce, And Admission Review

Last Updated: 2026-08-24

## Claim Boundary

This is Islamic Value-Sensitive Design reasoning about provider responsibility in a self-hostable event platform. It is a planning and traceability artifact, not proof that the planned behavior is implemented or effective.

It is **not** a fatwa, Sharia certification, halal/haram ruling, legal opinion, payment-services approval, privacy certification, security certification, or accessibility certification. Religious-legal conclusions about payment structures, fees, reserves, delayed payout, or other finance questions require qualified Islamic scholarly review. Legal, provider-contract, security, privacy, accessibility, and operational conclusions require their own accountable reviewers and evidence.

## Findings And Recommendations

### F1 — Block: Accepted Commercial And Operator Facts Must Precede Payment

The current workstream proves Phase 18 payment attempts and reconciliation, but its future plan had placed the complete buyer refund/operator acceptance snapshot after paid Checkout. A later record cannot prove what a buyer accepted earlier.

**Recommendation:** Phase 18C must fail closed on every new positive Checkout until the order pins the exact merchant, independent instance operator, official/unofficial status, event delivery milestone, currency, line totals, fee, contribution, refund policy version/text/language, support/complaint contact, provider charge type, and statement descriptor accepted before payment. Historical attempts must not receive synthetic acceptance.

### F2 — Block: Refund And Dispute State Must Be Truthful Under Ambiguity

Refund creation, insufficient available balance, customer action, provider timeout, webhook delay, refund failure, and disputes can leave local state non-terminal. Showing request acceptance as financial completion would mislead buyers and overload organizers/support.

**Recommendation:** Reserve refundable allocation atomically across every non-definitively-released state, keep disputes separate from refunds, persist mutation intent before provider I/O, reconcile ambiguous outcomes, and display “refunded” only after authoritative success evidence. Cancellation must create a bounded durable campaign rather than one unbounded transaction over all buyers.

### F3 — Block: Self-Hosting Must Not Borrow ISLAMU Trust

Each self-hoster controls its own deployment, Stripe platform credentials, support capacity, policies, incident response, and legal obligations. Generic branding or tenant-settable official status can falsely imply ISLAMU operation or protection.

**Recommendation:** Store official-instance status outside tenant control; disclose merchant and instance operator before payment; require each deployment to use its own provider credentials; name complaint/refund/dispute/reconciliation owners; and provide stop-sale that preserves webhook, refund, reconciliation, support, and historical reads.

### F4 — High: Admission Credentials Must Minimize Data And Preserve Dignity

QR admission can reduce fraud and queues, but bearer leakage, PII in codes, inaccessible smartphone-only flows, scanner compromise, connectivity loss, or public rejection messages can harm attendees.

**Recommendation:** Encode only a version and high-entropy opaque credential; store only a keyed digest and key version; treat the manual code as equally sensitive; reveal scanner capabilities once; provide accessible camera/HID/manual paths; return bounded door information; avoid offline-validity claims; and use private, respectful, non-enumerating results.

### F5 — High: Check-In Must Be Auditable Without Becoming Surveillance

Attendance evidence is operationally useful but can expose movement, identity, or sensitive participation. Mutable “checked in” flags and deletion of mistakes weaken accountability.

**Recommendation:** Use entitlement-scoped append-only check-in/undo facts, bounded retention and export authority, no credential/PII metric labels, explicit device-loss response, and an authenticated reasoned exception path for genuine outages rather than silent local validation.

### F6 — High: Transfer Requires Fresh Consent And Immediate Credential Rotation

Transfer changes the future attendee without changing purchaser/payment history. Copying consent or leaving the old QR valid violates participant agency and creates duplicate admission.

**Recommendation:** Separate holder transfer, organizer correction, and credential reissue; minimize pre-acceptance disclosure; recollect required participant data and consent; atomically accept/reassign/revoke/issue; preserve purchase/refund/audit history; and test accept/cancel/expire/check-in/reissue races on real persistence.

### F7 — High: Waitlist Order Must Be Explainable And Non-Manipulative

Waitlist offers distribute scarce capacity. Hidden priority, paid queue-jumping, undisclosed manual reordering, ambiguous expiry, or confusing existing `Waitlisted` order status with an actual offer can create unfair expectations.

**Recommendation:** Publish FIFO or another explicitly approved policy with deterministic tie-breakers; disclose that position is not a guarantee; minimize public position data; audit exceptional overrides; use bounded expiring offers backed by normal capacity holds; and preserve order under restart/recovery.

### F8 — High: Event Add-Ons Must Stay Optional And Separate From Admission

Add-ons can obscure the actual event price, refundability, fulfillment, and merchant relationship or expand Event into a general commerce, marketing, tax, invoicing, or accounting product.

**Recommendation:** Keep add-ons event-bound and opt-in; show separate immutable price/refund/fulfillment facts; retain one merchant/currency per mixed order; keep add-on inventory and fulfillment separate from admission; ensure add-on-only refund does not revoke a ticket; and emit only bounded post-commit facts to approved specialist systems.

### F9 — Block: `ProtectedDelayedPayout` Is An Approval Decision Before A Coding Decision

Delayed payout can create custody, reserve, consumer, provider-account-control, dispute, and religious-finance questions. Calling it escrow or enabling it from tenant configuration would overstate protection and authority.

**Recommendation:** Allow source-free investigation after Phase 18C, but keep runtime work disabled until dated provider, account-controller/loss-liability, legal, consumer/payment-services, qualified Islamic scholarly, reserve, complaint, dispute, and accountable-operator evidence exists. If the approved control requires preview, raw, undocumented, or unstable provider APIs, record the profile disabled.

## Provider-Responsibility Traceability

| Area | Affected stakeholders | Provider-controlled decisions | Required mitigation | Owning plan gate |
|---|---|---|---|---|
| Buyer acceptance | Buyer, attendee, organizer, operator | Required disclosures, acknowledgement freshness, official-instance representation | Immutable pre-payment snapshot; no fabricated history; HAL-authoritative Checkout | 18C.1–18C.5 |
| Refunds and cancellation | Buyer, organizer, support, operator | Refund floor, allocation, cancellation campaign, support escalation | Atomic reservation, bounded campaign, truthful state, audit and recovery | 19.0–19.5 |
| Disputes | Buyer, organizer, operator | Inquiry/dispute response ownership, evidence route, refund blocking | Independent projection, deadline/owner alerts, provider reconciliation | 19.0, 19.2, 19.4–19.5 |
| Self-host trust | Buyer, organizer, ISLAMU, independent operator | Official status, credentials, risk ceilings, complaint ownership | Instance-owned marker, own credentials, operator disclosure, stop-sale | 18C.2–18C.4 |
| Admission credential | Attendee, door staff, organizer | Credential shape, recovery, revocation, displayed door data | Opaque bearer, keyed digest, anti-enumeration, accessible alternatives | 20.0–20.6 |
| Check-in | Attendee, staff, organizer | Entitlement policy, scanner authority, audit/export, outage exception | Append-only facts, scoped one-time capability, bounded results/retention | 21.0–21.5 |
| Transfer | Purchaser, current holder, recipient, organizer | Eligibility, disclosure, consent, checked-in override | Fresh consent, atomic rotation, separate correction/reissue authority | 22.0–22.5 |
| Waitlist | Waiting participant, organizer, operator | Ordering, tie-breaker, expiry, override | Transparent deterministic policy, audit, normal capacity hold | 23W.0–23W.3 |
| Add-ons | Buyer, attendee, organizer, external specialist | Optionality, price/refund/fulfillment disclosure, product boundary | Separate line/inventory/fulfillment; no admission or accounting authority | 23A.0–23A.3 |
| Delayed payout | Buyer, organizer, operator, provider, reviewers | Whether profile exists, release milestone, blockers, human authority | Approval evidence, non-escrow wording, separation of duties, disabled default | 24.1–24.5 |
| Recovery and closeout | All stakeholders | Backup/restore/key custody, incident ownership, deployment claim | Restore/replay proof, provider/capability fixtures, explicit deployment status | 25.0–25.3 |

## Common Overlooked Failures And Outcomes

**Feature types:** paid registration, refunds/disputes, bearer admission, online check-in, transfer, waitlist, add-ons, and conditional payout.

Common overlooked failures:

- accepted terms are mutable or recorded only after provider handoff;
- self-hosted deployments imply official ISLAMU operation;
- refund requests are shown as completed while provider state is pending, unknown, or failed;
- cancellation enumerates all buyers inside one long transaction;
- refund reservations omit ambiguous attempts and allow over-refund races;
- QR or manual codes contain PII or appear in logs, URLs, referrers, storage, or support artifacts;
- scanner secrets can be retrieved repeatedly or browse attendee data;
- inaccessible users have no dignified non-camera/non-smartphone path;
- transfer inherits consent or leaves the old credential active;
- waitlist policy or manual override is hidden;
- add-ons are preselected, confused with admission, or silently coupled to ticket refunds;
- delayed payout is described as escrow or buyer protection without accountable approval;
- backup restores data but not capability/Data Protection keys, worker fences, or durable cursors.

Possible bad outcomes:

- financial loss, duplicate refund, dispute escalation, or delayed remedy;
- false trust in an independent operator or malicious fork;
- attendee exclusion or humiliating door interactions;
- PII/bearer leakage and copied-ticket admission;
- unfair capacity distribution and support conflict;
- duplicate external effects after restore;
- legal, provider-account, reputation, and operator-liquidity harm;
- unsupported religious or ethical claims about payment behavior.

Positive outcomes if implemented responsibly:

- clearer buyer expectations and stronger evidence of promise-keeping;
- faster remedies with honest asynchronous status;
- reduced admission fraud with less personal data;
- accessible, respectful check-in and recovery;
- consent-preserving ticket transfer;
- transparent and recoverable allocation of scarce capacity;
- event-only commerce without hidden monetization or specialist-domain sprawl;
- truthful self-hosting and deployment responsibility;
- better evidence for legal, privacy, accessibility, security, and scholarly reviewers without claiming their approval.

## Rejected Alternatives

- Backfilling old payment attempts as though new acceptance happened historically.
- Letting tenant administrators weaken the instance refund floor or set official-instance status.
- Treating redirect return, request acceptance, or provider callback arrival as payment/refund/admission success.
- Retrying ambiguous provider mutations with a new idempotency identity.
- Running provider I/O inside business transactions.
- Storing PII in QR codes or using public/display IDs as bearer authority.
- QR-only or camera-only admission.
- Copying consent during transfer.
- Hidden paid waitlist priority or unaudited reordering.
- Preselected add-ons/contributions or a general cross-event storefront.
- Event-owned marketing, bookkeeping, accounting, tax determination, or legal invoice/credit-note issuance.
- Calling delayed payout escrow or implementing it through preview/raw/undocumented provider APIs.
- Sharing ISLAMU provider credentials or brand trust with independent deployments.

## Evidence Reviewed

Repository evidence:

- [Registration implementation plan](../dev/active/registration-data-collection/registration-data-collection-plan.md)
- [Registration context](../dev/active/registration-data-collection/registration-data-collection-context.md)
- [Registration task ledger](../dev/active/registration-data-collection/registration-data-collection-tasks.md)
- [Paid-event payments I-VSD consultation](i-vsd-paid-event-payments-consultation.md)
- [I-VSD compliance check](i-vsd-compliance-check.md)
- [Flexible event end-times I-VSD review](i-vsd-flexible-event-end-times.md)
- [ADR-017 event participation authority](../docs/adr/ADR-017-event-participation-authority-model.md)
- [ADR-022 paid-event commerce and Stripe Connect](../docs/adr/ADR-022-paid-event-commerce-and-stripe-connect.md)
- [ADR-023 admission credential, check-in, transfer, and recovery](../docs/adr/ADR-023-admission-credential-check-in-transfer-recovery.md)
- [ADR-024 external integrations and protected-payout boundaries](../docs/adr/ADR-024-external-business-integrations-and-protected-payout-boundaries.md)
- Current Phase 18 handoff, payment implementation evidence, self-hosting/configuration/operations documentation, and the `registration-data-collection` intent.

Official functional documentation reviewed on 2026-08-24:

- [Stripe idempotent requests](https://docs.stripe.com/api/idempotent_requests)
- [Stripe refunds](https://docs.stripe.com/refunds)
- [Stripe dispute lifecycle](https://docs.stripe.com/disputes/how-disputes-work)
- [Stripe Connect direct charges](https://docs.stripe.com/connect/direct-charges)
- [EF Core concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [ASP.NET Core rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)
- [ASP.NET Core Data Protection configuration](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0)

External documentation informed only source-free functional requirements. No third-party source code, snippets, ASTs, tests, migrations, or assets were ingested.

## Missing Evidence

- Buyer, attendee, organizer, disability/accessibility, support, and independent self-hoster stakeholder review.
- Production terms, privacy/retention notices, refund/cancellation/material-change/dispute policy, and supported-language review.
- Live Stripe platform/account/country/controller/loss-liability/fee/negative-balance evidence.
- Legal and consumer/payment-services review for enabled jurisdictions and payment methods.
- Qualified Islamic scholarly review for finance questions, including fees, contributions, reserve/control, and delayed payout.
- Accessibility evidence for acknowledgement, QR/manual ticket, camera/HID/manual scanning, queue messaging, and door exceptions.
- Privacy/security review for capability entropy, key custody/rotation, check-in retention/export, transfer PII, and restore.
- Named staffed owners and service levels for complaints, refunds, disputes, reconciliation, accessibility support, security incidents, and provider restrictions.
- Real deployment backup/restore, migration application, and provider launch evidence.

Missing evidence remains a visible gate; it is not replaced with confidence or inferred from test fixtures.

## Escalation Needed

| Question | Required accountable reviewer | Fail-closed result without evidence |
|---|---|---|
| Finance structure, fees, contribution, reserve, delayed payout, religious-legal claims | Qualified Islamic scholarly authority | No ruling/certification claim; protected profile disabled |
| Consumer rights, payment services, refunds, disputes, terms, tax boundary | Qualified legal counsel | Paid profile limited or disabled for affected deployment |
| Connect control, account liability, provider capability, fees, webhooks | Stripe/provider specialist and accountable operator | Capability absent; no workaround through raw/preview API |
| Capability/QR/check-in/transfer privacy and key custody | Security and privacy reviewers | Surface remains disabled or narrowed |
| Buyer acceptance and scanner accessibility | Accessibility reviewer plus affected users | No production readiness claim |
| Complaints, fraud, event authenticity, refunds, disputes, incidents | Named trust/safety and support operators | Stop-sale; preserve reconciliation/remedy paths |
| Official ISLAMU status and independent-fork representation | Project Steward / governance authority | Deployment represented as independent |

## Context Inventory

- Repository/workspace planning docs, ADRs, I-VSD reports, intent/rules, current implementation handoffs, code/config/tests, and operator documentation were available.
- Official Stripe and Microsoft documentation was available through web search/fetch.
- AnySearch and Context7 MCPs were not available in this session, so their use is not claimed.
- No project support incidents, production analytics, legal files, scholarly decisions, live provider account evidence, or stakeholder-study exports were available.
