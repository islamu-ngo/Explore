<!-- ABOUTME: Architectural decision record for registration orders, ticket catalogs, inventory, and pricing. -->
<!-- ABOUTME: Defines participant separation, atomic holds, state machines, monetization, and payment boundaries. -->

# ADR-018: Registration Order And Ticketing Aggregate

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-26 |
| **Deciders** | ISLAMU Event Platform — Architecture, Security, Registration workstreams |
| **Supersedes** | User-centric `EventRegistrationIntent` and decorative event/session prices |
| **Superseded by** | ADR-022 for payment, refund, dispute, and Stripe Connect behavior; ADR-024 for external tax/invoice ownership and protected payout |

## Context

An authenticated user is not necessarily the purchaser, participant, or ticket holder. A single user-centric registration intent cannot represent guest checkout, families, company bookings, quantities, deferred assignment, multiple ticket types, or participant-specific data and consent.

Ticket inventory must remain correct across replicas and across different ticket types that consume the same physical capacity. Published prices must remain interpretable after organizers edit future catalogs. The workstream also needs transparent buyer-chosen pricing and optional instance funding without prematurely selecting payment providers.

Hi.Events validates the product need for reservation-first checkout, commercial snapshots, shared capacities, buyer-versus-attendee questions, expiry recovery, and organizer operations. Its attendee-as-ticket model, mutable published products, floating-point money paths, public identifiers as bearer credentials, cache-only idempotency, and transaction-coupled external effects do not satisfy ISLAMU's architecture.

## Decision

### Aggregate ownership

1. `RegistrationOrder` replaces and deletes `EventRegistrationIntent`. It records nullable account linkage, booking-party type, guest capability hash, one currency, and pinned participation, workflow, and ticket-catalog versions.
2. Purchaser PII lives in `RegistrationOrderPii`. Participants and participant PII are separate entities. A participant need not be a User, and one purchaser may manage several independently consenting participants.
3. Each `RegistrationOrderLine` references one ticket type and snapshots every fact needed to interpret the purchase, including name, currency, pricing mode, chosen unit amount in integer minor units, bounds, applicable catalog version, and non-zero platform-fee policy version.
4. Ticket assignments reference a concrete order line. Database and domain rules prevent assignments from exceeding that line's quantity.
5. `EventRegistration` remains the materialized per-session admission row, linked to a participant rather than requiring a User. `AdmissionTicket` now owns independently revocable/rotatable credentials and entitlement delivery; display IDs never authorize access or admission. Append-only check-in facts remain governed by ADR-023 and the later scanner/check-in phase.

### Catalog, pricing, and monetization

1. Published ticket catalog versions are immutable. Edits clone to a new draft; in-flight orders stay pinned to their original version.
2. Ticket pricing has exactly five explicit modes:
   - `FIXED`: organizer-defined amount.
   - `FREE`: no amount fields.
   - `DONATION`: buyer-chosen organizer-directed amount, with an optional minimum; zero is valid only when the minimum is zero.
   - `PAY_WHAT_YOU_CAN`: buyer-chosen amount with optional minimum and suggested amount.
   - `SLIDING_SCALE`: required minimum and suggested amounts with exact “You pay” and “Organizer earns” transparency.
3. Persisted and API monetary amounts use integer minor units in `long ...Minor` fields supplied at the contract boundary. Percentages use integer basis points, where `10_000 = 100%`. The shipped model defines neither decimal-major conversion nor foreign exchange; client calculations are display-only.
4. `PlatformFeePolicy` is versioned instance-scoped configuration with fixed minor-unit charges and basis-point components. It defaults to zero and is managed only by instance administrators.
5. `PlatformContributionSetting` is a separate versioned, instance-scoped, default-off contribution to the instance operator. Its heading, body, and basis-point options are DB-stored; zero is preselected. A positive selection is snapshotted separately and never enters organizer earnings, ticket price, capacity, or organizer export totals.
6. Tenant administrators and organizers cannot enable or modify platform monetization.

### Capacity and lifecycle

1. `EventCapacityPool` may be shared by several ticket types. A `RegistrationInventoryHold` records quantity, absolute expiry, and `Active`, `Consumed`, `Released`, `Expired`, or `Cancelled` state.
2. Hold creation locks every affected pool in deterministic order, recounts active holds inside one short PostgreSQL transaction, validates the pinned catalog and quantity limits, and persists the order, lines, holds, and required outbox records atomically.
3. Expiry is released by an idempotent background worker. Finalization and expiry use conditional transitions so only one can win.
4. Inventory release derives from order lines and holds, never participant rows.
5. Order, attempt, submission, approval, payment, and future refund states are independent. Domain rule classes own exhaustive transitions; `ApprovalStatus` remains only the organizer verdict.
6. Duplicate finalization returns the original result and creates no additional participants, registrations, answers, holds, or outbox records.

### Payment boundary

1. An all-zero order follows free confirmation after requirements, approval, and capacity checks.
2. Any positive organizer-directed total or platform contribution stops at `AwaitingPayment`.
3. This workstream does not create payment intents, capture money, estimate processor fees, refund, calculate tax, settle payouts, or issue invoices.
4. A future payment ADR must define durable provider-attempt identity, idempotent external calls outside business transactions, reconciliation of ambiguous success, capture/refund state, fees/taxes, and payout ownership before payment implementation begins.

### Hi.Events evidence and licensing boundary

1. Adopt behavior: reserve before PII, visible expiry and abandon controls, state-specific recovery, immutable commercial snapshots, shared-capacity visualization, buyer/participant question separation, and anti-enumeration recovery.
2. Adapt concepts onto ISLAMU's order, participant, assignment, hold, outbox, HAL, and Cerbos authorities.
3. Reject Hi.Events persistence, authorization, money, idempotency, credential, and side-effect machinery.
4. Copy no Hi.Events code, SQL, migrations, snippets, or assets. ISLAMU's CLA supports dual licensing; third-party AGPLv3 code from authors who did not sign the CLA would break that capability. Implementation is clean-room from the approved report and plan. This decision explicitly overrides the code-reuse permission in `hi-events-report.md` §10.

## Rejected alternatives

The following consultation anti-patterns are forbidden:

10. Making only `EventRegistrationIntent.UserId` nullable while retaining the one-user aggregate.
11. Storing one quantity field on a user registration instead of modeling order lines and participants.
12. Modeling ticket types as custom-form choices.
13. Using Event or EventSession `Price` as the authoritative price after ticket types exist.
14. Enforcing family or company quantity limits only in the UI.
15. Claiming hard per-user limits for anonymous, unverified registrants.
16. Running provider HTTP calls inside the capacity-reservation transaction.
17. Letting multiple unsynchronized systems independently own the same capacity pool.
19. Automatically sharing purchaser consent with all adult participants.
23. Using Layer 3 custom properties for provenance, registration authority, ticket limits, or payment status.
24. Adding client-side capability booleans or role checks instead of server-authored HAL links.
25. Building payment-provider integration before the order and inventory aggregate is stable.

Also rejected are attendee-as-ticket modeling, mutable published prices, floating-point money, public/display IDs as authorization, cache-only idempotency, participant-derived inventory release, and external calls inside database transactions.

## Consequences

- Registration, HAL, Cerbos, API contracts, generated clients, and Blazor checkout receive an intentional development-mode breaking replacement; no compatibility shims or dual writes remain.
- More entities are required, but purchaser, participant, assignment, admission, PII, consent, inventory, and commercial facts gain independent lifecycles.
- Real PostgreSQL concurrency tests are mandatory for sibling ticket types competing for the final shared-pool capacity and for finalization-versus-expiry races.
- All persisted and API commercial amounts remain integer minor units supplied at the contract boundary. The shipped model defines neither decimal-major conversion nor foreign exchange, and pinned snapshots keep the integer amounts interpretable.
- Self-hosted instances remain zero-fee and contribution-disabled by default.
- Payment remains a named future dependency rather than leaking provider concerns into the stable order aggregate.

## Related

- `dev/active/registration-data-collection/registration-data-collection-consultation.md` Report 2 §§11, 13–21, 33
- `dev/active/registration-data-collection/registration-data-collection-plan.md` D4, D11, D16–D19
- `dev/active/registration-data-collection/hi-events-report.md` §§7, 9–11 (behavior evidence only; §10 code permission overridden)
- ADR-002: Outbox Pattern
- ADR-016: Registration Data Collection Context And Provider Channels
- ADR-017: Event Participation Authority Model
