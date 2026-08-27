<!-- ABOUTME: I-VSD planning report for the Event Ticketing Lifecycle successor workstream. -->
<!-- ABOUTME: Maps provider responsibilities for purchase governance, admission readiness, transfer, fair return, add-ons, and recovery. -->

# I-VSD Event Ticketing Lifecycle Planning Review

Last Updated: 2026-08-27

## Review Metadata

- Mode: planning
- Subject: event ticketing lifecycle after registration-form delivery
- Workstream: `event-ticketing-lifecycle`
- Report kind: implementation-planning review
- Report status: current
- Disposition: plan-aligned
- Evidence cutoff: 2026-08-27
- Evidence-packet revision: SHA-256 source-packet manifest `c06e94970f738b8fc20b89895f0425c8ca186b85a0184647fd97cfddbcfeb792`
- Reviewed input revision: plan SHA-256 `84bcd73f5d603fcd24f1a4cf9aaeef5e7f041a36e8459b83b173582ea25e24fa`; tasks SHA-256 `0373aa09e4555fda371e073eee17ab7b0bb8ebfeaa4c9c5591268f7813b5397b`
- Revalidation result: every accepted finding and mitigation is mapped to the rewritten S1-S7/WB-1 behavior and Tasks 1.1-9.4; no provider-responsibility contradiction remains
- Refresh trigger: any material change to access/ceiling authority, participant consent, transfer/resale, waitlist/refund priority, add-on admission separation, recovery/key custody, monetization/payout, or mapped scenarios/tasks
- Supersedes: future-work mappings F6–F13 in `i-vsd-registration-data-collection.md`; that report remains authoritative for completed registration/forms/commerce/admission findings F1–F5

## Scope

This report covers provider-controlled design decisions for the successor workstream that begins after the completed Registration Data Collection Phase 21 boundary:

- authenticated, verified-email guest, and name-only registration access;
- monotonic instance/tenant/event purchase ceilings and explicit purchaser actor context;
- order-level versus participant-level requirement scope, consent, approval, and admission readiness;
- ticket transfer, claim, correction, reissue, and credential rotation;
- deterministic waitlist and conditional fair-return reallocation;
- optional event-bound add-ons;
- recovery, deployment capability truth, and explicitly unavailable protected delayed payout.

Completed authoring/provider/payment/refund/admission/check-in implementation remains evidence and dependency, not new scope.

## Claim Boundary

This is Islamic Value-Sensitive Design reasoning about provider responsibility in a self-hostable event platform. It is not a fatwa, Sharia certification, halal/haram ruling, legal opinion, payment-services approval, privacy/security certification, accessibility certification, or proof of implementation effectiveness.

Qualified Sunni scholarly authority owns religious-legal conclusions. Legal, provider-contract, privacy, security, accessibility, stakeholder, and operational claims require their own accountable reviewers and evidence.

## Findings

### IVSD-F001 — Purchase Governance Must Prevent Hoarding Without Blocking Legitimate Groups

- Lifecycle: accepted
- Severity / claim type: High / provider-responsibility design
- Principle and domain: justice, prevention of harm, anti-hoarding; commerce and access
- Stakeholders: individual purchasers, families, organizations, organizers, excluded attendees
- Provider-controlled decision: registration access modes, accepted terms, purchaser context, and instance/tenant/event ceilings
- Evidence: predecessor finding F10/F13; implemented order/capacity/idempotency foundations; missing successor capability search
- Validation level: repository and design evidence; stakeholder validation missing
- Mitigation: `IVSD-M001`
- Owner / next validation: Phases 1-2 / Tasks 1.1-2.6; stakeholder, abuse, privacy, and accessibility review before release

### IVSD-F002 — Participant Requirements Must Be Subject-Correct Before Admission

- Lifecycle: accepted
- Severity / claim type: High / provider-responsibility design
- Principle and domain: dignity, consent, minimization, prevention of harm; privacy and admission
- Stakeholders: participants, guardians, independent adults, organizers, door staff
- Provider-controlled decision: order/participant form scope, consent authority, approval, completion timing, and credential issuance
- Evidence: predecessor finding F11; implemented typed-answer/form/admission/check-in foundations
- Validation level: repository and design evidence; privacy/accessibility/stakeholder validation missing
- Mitigation: `IVSD-M002`
- Owner / next validation: Phases 3-4 / Tasks 3.1-4.6

### IVSD-F003 — Transfer Must Rotate Admission Authority Without Rewriting Commerce Or Consent

- Lifecycle: accepted
- Severity / claim type: High / provider-responsibility design
- Principle and domain: agency, honesty, prevention of fraud and exploitation; admission and privacy
- Stakeholders: purchaser, current holder, recipient, guardian, organizer, door staff
- Provider-controlled decision: transfer eligibility, recipient disclosure, claim requirements, credential rotation, correction/reissue authority
- Evidence: predecessor finding F6; ADR-023; existing credential/check-in fences; no transfer implementation found
- Validation level: repository and design evidence; privacy/security/accessibility review missing
- Mitigation: `IVSD-M003`
- Owner / next validation: Phase 5 / Tasks 5.1-5.6

### IVSD-F004 — Fair Return Must Be Explainable And Must Not Promise An Unearned Refund

- Lifecycle: accepted
- Severity / claim type: High / provider-responsibility design
- Principle and domain: justice, transparency, avoidance of gharar and exploitation; scarcity and refunds
- Stakeholders: original holder, waitlisted buyer, organizer, support operator
- Provider-controlled decision: queue order, ticket-type parity, public-stock priority, seller withdrawal during buyer checkout, buyer continuity, replacement-payment proof, provider-aware expiry, and refund timing
- Evidence: predecessor findings F7/F12; implemented capacity holds and provider-neutral refund truth; no waitlist-offer implementation found
- Validation level: repository and design evidence; legal/stakeholder/operator validation missing
- Mitigation: `IVSD-M004`
- Owner / next validation: Phases 6 and 8 / Tasks 6.1-6.8 and 8.1-8.4

### IVSD-F005 — Add-Ons Must Remain Optional And Separate From Admission

- Lifecycle: accepted
- Severity / claim type: High / provider-responsibility design
- Principle and domain: transparency, freedom from coercion, bounded provider responsibility; commerce and fulfillment
- Stakeholders: buyers, attendees, organizers, fulfillment staff
- Provider-controlled decision: optionality, price/refund/fulfillment disclosure, inventory, admission separation, specialist-system boundary
- Evidence: predecessor finding F8; ADR-024; no event-add-on implementation found
- Validation level: repository and design evidence; buyer/operator validation missing
- Mitigation: `IVSD-M005`
- Owner / next validation: Phase 7 / Tasks 7.1-7.6

### IVSD-F006 — Recovery Must Restore Authority Without Resurrecting Or Duplicating Harm

- Lifecycle: accepted
- Severity / claim type: High / operational provider responsibility
- Principle and domain: amanah, accountability, prevention of harm; operations and security
- Stakeholders: all purchasers/participants, organizers, self-hosters, support and security operators
- Provider-controlled decision: backup scope/order, key custody, replay fencing, capability status, operator truth
- Evidence: implemented outbox, Quartz, Data Protection/keyed-digest, payment/refund/admission recovery patterns; predecessor validation gaps
- Validation level: repository and design evidence; real restore/operator evidence missing
- Mitigation: `IVSD-M006`
- Owner / next validation: Phases 8-9 / Tasks 8.1-9.4

### IVSD-F007 — Protected Delayed Payout Is An Escalation Gate, Not Runtime Scope

- Lifecycle: accepted
- Severity / claim type: Block / authority and evidence boundary
- Principle and domain: truthfulness, avoidance of misleading custody claims, qualified authority; finance and governance
- Stakeholders: buyers, organizers, connected-account owners, operators, complainants
- Provider-controlled decision: whether any delayed-release capability is advertised, configured, scheduled, or implemented
- Evidence: predecessor finding F9; ADR-022/024; official Stripe reserve/control documentation; missing provider/legal/scholarly/operator approvals
- Validation level: public provider and repository evidence only
- Mitigation: `IVSD-M007`
- Escalation boundary: separate future I-VSD/ADR/workstream after all named approvals and stable typed public APIs
- Owner / next validation: Phase 9 / Tasks 9.1-9.4 asserts absent/unavailable

## Recommendations

### IVSD-M001 — Version And Enforce Access/Actor/Ceiling Truth

Pin access mode, accepted terms, purchaser actor/context, and effective policy lineage. Enforce the strictest instance/tenant/event ceiling atomically and prevent context switching from multiplying entitlement.

### IVSD-M002 — Make Completion And Approval Admission Preconditions

Declare order versus participant scope, keep typed answers canonical, require subject-correct consent/approval, and withhold active credentials until all required participant facts are complete.

### IVSD-M003 — Use Atomic Future-Holder And Credential Rotation

Keep purchaser/payment/refund truth immutable; collect recipient-owned data and consent; enforce policy, expiry, and reapproval; rotate the credential in the same authoritative transition; provide no resale or money movement.

### IVSD-M004 — Use Deterministic Commercially Equivalent Reallocation

Publish stable order/tie-breakers, prioritize compatible released supply before public stock, use normal holds, and request the original-holder refund only after replacement payment is reconciled. Buyer-transparent rebinding is permitted only when tenant, event, ticket type, policy/catalog lineage, currency, accepted terms, admission entitlement, gross minor-unit amount, and refund-funding compatibility are equal. If no such supply exists, or payment handoff is possible or ambiguous, fail seller withdrawal privately and retain the binding until authoritative reconciliation. A local deadline alone never proves provider failure.

### IVSD-M005 — Bound Add-Ons To The Event Transaction

Make add-ons opt-in with separate immutable price/refund/fulfillment facts, independent inventory/fulfillment, one merchant/currency, and no admission or specialist-business-system authority.

### IVSD-M006 — Treat Keys, Fences, And Cursors As Restore-Critical State

Test restore order for application data, Data Protection/digest keys, outbox/inbox, Quartz/fences, durable business-idempotency identities, and provider cursors. Restore into stop-sale/recovery-only mode, cancel every pre-restore transfer/waitlist/recovery capability, rotate or reissue active admission credentials, reject stale workers, and fail closed on any missing authority before reopening.

### IVSD-M007 — Keep Protected Delayed Payout Absent

Do not create runtime source, configuration, migration, scheduler, API/HAL/client surface, preview/raw provider call, or escrow wording. Reopen only through a separate evidence-backed workstream.

Rejected alternatives:

- One monolithic continuation of the forms workstream: rejected because it obscures ownership, review state, and completion.
- Transfer before purchase/participant governance: rejected because recipient authority would depend on unresolved access, consent, and admission gates.
- Combined waitlist/add-on phase: rejected because scarcity/refund and optional-commerce failures have independent rollback and review boundaries.
- Feature-flagged protected payout: rejected because a flag does not create missing authority or stable provider capability.

## Stakeholders

- Purchasers acting personally or for a family, group, or organization
- Participants, minors/dependents, guardians, and independent adult recipients
- Waitlisted buyers and original ticket holders
- Organizers, fulfillment staff, door staff, support, privacy/security, and incident operators
- Tenant and instance administrators
- Independent self-hosters and the ISLAMU Project Steward
- External payment/provider, legal, accessibility, privacy/security, and qualified scholarly reviewers

## I-VSD Principles And Domains

- Justice and non-manipulation in scarce-capacity allocation
- Truthfulness in price, refund, queue, merchant, capability, and deployment claims
- Dignity, privacy, minimization, and subject-correct consent
- Prevention of fraud, hoarding, exploitation, duplicate authority, and operational harm
- Amanah and accountability through durable audit, recovery, and named ownership
- Bounded provider responsibility: event operations remain distinct from marketing, accounting, tax, invoicing, banking, and escrow

## Validation Gaps

- Stakeholder review with purchasers, participants, guardians, organizers, waitlisted users, accessibility users, staff, and self-hosters
- Privacy/security review of recipient contact, capabilities, key custody, retention/export, and restore
- Accessibility review of registration, approval, transfer, queue, status/countdown, add-on, and support-desk experiences
- Legal/consumer review of refund/reallocation terms and supported jurisdictions
- Staffed operator ownership and service levels for queue, refund, fraud, accessibility, provider, and restore incidents
- Real backup/restore and provider deployment evidence

## Escalation Needed

- Qualified scholarly authority: finance structure, fees/contributions, reserves/control, delayed payout, and religious-legal claims
- Qualified legal/payment-services counsel: consumer rights, conditional refund, terms, and supported jurisdictions
- Provider specialist and accountable operator: exact Stripe account/controller/liability/capability evidence
- Security/privacy reviewers: capability entropy, key rotation/custody, recipient data, and restore
- Accessibility reviewers and affected users: all status, input, transfer, waitlist, and support paths

Without evidence, the affected surface remains disabled/narrowed and no certification or production-readiness claim is made.

## Evidence Reviewed

- Source-packet manifest SHA-256 `c06e94970f738b8fc20b89895f0425c8ca186b85a0184647fd97cfddbcfeb792`
- Successor plan SHA-256 `84bcd73f5d603fcd24f1a4cf9aaeef5e7f041a36e8459b83b173582ea25e24fa`
- Successor tasks SHA-256 `0373aa09e4555fda371e073eee17ab7b0bb8ebfeaa4c9c5591268f7813b5397b`
- `i-vsd-registration-data-collection.md` SHA-256 `d7723403e6d8b1a70854599a3c4812091290cf505bea7ea0a4558a5e6532d237`
- `i-vsd-paid-event-payments-consultation.md` SHA-256 `44e90e5ccb88ba7e98503f0f1b98c00b7bdfaf85d623aff8f7ff882a2a90cb36`
- Predecessor plan SHA-256 `42ef4342117d07097a06dd2d22c4892a5f18d67f73a0082950dc3811d355494a`
- Predecessor tasks SHA-256 `12517d9c29af7e6bff232d18190971579ca4229b364698ca907b8bea8c64d640`
- Predecessor clean-room handoff SHA-256 `035d7d706169cb0a2b1ff1da8ee35709f8112c361d7b07073588faee2f8f2843`
- ADR-016 through ADR-018 and ADR-022 through ADR-024
- Repository graph blast radius, current aggregate/middleware/scheduler paths, and bounded test inventory dated 2026-08-27
- Official Microsoft, OWASP, W3C, EU, EDPB, and Stripe documentation recorded in the clean-room evidence

## Missing Evidence

No production stakeholder studies, incident history, legal opinion, qualified scholarly decision, live Stripe account/control evidence, independent accessibility/privacy/security audit, or real restore exercise was available.

## Context Inventory

- Repository planning docs, ADRs, intent/rules, implemented Phase 0–21 code/tests/contracts, and prior evidence were available.
- The finalized successor plan and tasks were revalidated against the source-free clean-room evidence and this report's stable findings.
- Context7 and official web documentation had already been reduced to source-free constraints in the predecessor clean-room handoff.
- No third-party code, snippets, schemas, SQL, tests, migrations, assets, or source-derived expressive structure is an implementation input.

## Common Overlooked Failures And Outcomes

- Actor-context switching multiplies purchase allowance and enables hoarding.
- Payment succeeds while participant requirements are incomplete, but a credential is issued anyway.
- Transfer succeeds concurrently with old-credential check-in or inherits another adult's consent.
- One released ticket is offered to both waitlist and public inventory, or refund precedes replacement payment truth.
- Seller withdrawal cancels/reprices/restarts the buyer despite substitute exact-type supply, or frees the only reserved ticket while provider payment is still ambiguous.
- Add-on refund or fulfillment mutates admission authority.
- Restore omits keys/fences/cursors and revives authority or duplicates effects.
- A disabled payout idea leaks into configuration, HAL, generated clients, or marketing language.

## Planning Handoff

- Workstream: `event-ticketing-lifecycle`
- Status: current
- Evidence-packet revision: SHA-256 source-packet manifest `c06e94970f738b8fc20b89895f0425c8ca186b85a0184647fd97cfddbcfeb792`
- Reviewed input revision: plan SHA-256 `84bcd73f5d603fcd24f1a4cf9aaeef5e7f041a36e8459b83b173582ea25e24fa`; tasks SHA-256 `0373aa09e4555fda371e073eee17ab7b0bb8ebfeaa4c9c5591268f7813b5397b`
- Findings and mitigations: `IVSD-F001`→`IVSD-M001`, `IVSD-F002`→`IVSD-M002`, `IVSD-F003`→`IVSD-M003`, `IVSD-F004`→`IVSD-M004`, `IVSD-F005`→`IVSD-M005`, `IVSD-F006`→`IVSD-M006`, `IVSD-F007`→`IVSD-M007`
- Required plan mappings: F001/M001→S1-A/B/C→Tasks 1.1-2.6; F002/M002→S2-A/B/C→Tasks 3.1-4.6; F003/M003→S3-A/B/C→Tasks 5.1-5.6; F004/M004→S4-A/B/C/D/E/F and WB-1→Tasks 6.1-6.8 and 8.1-8.4; F005/M005→S5-A/B/C→Tasks 7.1-7.6; F006/M006→S6-A/B/C and WB-1→Tasks 8.1-9.4; F007/M007→S7-A→Tasks 9.1-9.4
- Escalations required before: production claims or enabling the affected capability; protected delayed payout before any separate planning approval or implementation
- Refresh triggers: material change to access/ceiling defaults, participant/guardian consent, transfer/resale, waitlist/refund priority, add-on admission boundary, recovery/key custody, monetization/payout, provider capability, or mapped tasks/scenarios

## Review Lifecycle

| Date | Previous status | New status | Trigger | Evidence/replacement |
|---|---|---|---|---|
| 2026-08-27 | none | draft | Successor workstream split requested | Source-packet manifest |
| 2026-08-27 | draft | current / plan-aligned | Completed triad mapping revalidated without changing provider-controlled behavior | `dev/active/event-ticketing-lifecycle/` |
| 2026-08-27 | current | current / plan-aligned | User resolved seller-withdrawal concurrency: buyer-transparent exact-type rebinding or private conflict until authoritative release | S4-C/S4-D; Tasks 5.1–5.3 |
| 2026-08-27 | current / plan-aligned | stale / changes-required | Exact successor revision was unbound; CTO hardening changed access guarantees, recovery behavior, scenarios, and task mappings | Finalize triad, record exact revisions in context/CTO, then revalidate independently |
| 2026-08-27 | stale / changes-required | current / plan-aligned | Independent planning-mode revalidation mapped the hardened access, recovery, scenario, and task contracts | Plan `84bcd73f...`; tasks `0373aa09...`; S1-S7/WB-1 |
