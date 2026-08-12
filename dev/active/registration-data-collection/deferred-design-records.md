<!-- ABOUTME: Deferred commerce and admission design records for the registration platform. -->
<!-- ABOUTME: Preserves clean-room decisions and triggers without implementing future payment or admission scope. -->

# Registration Data Collection — Deferred Commerce And Admission Design Records

Last Updated: 2026-08-12 Europe/Brussels

These records preserve the approved behavior lessons from `hi-events-report.md` without copying third-party code, SQL, migrations, snippets, or assets. They are not implementation approval. Each record requires a separate workstream and ADR review when its trigger occurs.

## Payment Attempts, Provider Identity, Refunds, And Reconciliation

- **Trigger:** A payment-provider workstream is approved after registration orders reliably stop at `AwaitingPayment`.
- **Extends:** `RegistrationOrder` with a separate `PaymentAttempt` aggregate and provider payment/refund identity records. It must not add payment state to registration approval, inventory holds, or participant state.
- **Required boundary:** Persist provider attempt identity and idempotency before handoff; execute provider calls outside business transactions; reconcile ambiguous acceptance instead of blindly retrying creation; snapshot refund facts independently from order totals.
- **Supersedes report guidance:** `hi-events-report.md` §7.3 and §7.5. Those sections are behavior and failure evidence only; the future ADR becomes ISLAMU's authority.

## Admission Tickets, Credentials, And Transfers

- **Trigger:** Confirmed free orders or settled paid orders need independently revocable admission credentials.
- **Extends:** `EventRegistration` and ticket entitlements with a separate `AdmissionTicket` aggregate.
- **Required boundary:** The display/public ticket ID never authorizes admission. Store only a signed or hashed, rotatable admission credential; transfer revokes the prior credential and issues a new one without rewriting order, participant, or consent history.
- **Supersedes report guidance:** `hi-events-report.md` §5.5, §7.10, and the deferred-admission inventory in §8. The future admission ADR becomes authoritative.

## Check-In Lists, Admission Events, And Scanner Capabilities

- **Trigger:** Organizers need session/day/event check-in against published ticket entitlements.
- **Extends:** `AdmissionTicket`, `TicketTypeEntitlement`, `EventSession`, and a new check-in-list aggregate with append-only admission events.
- **Required boundary:** Lists are entitlement-scoped and time-bounded; one active admission state is enforced per credential and target; scanners authenticate normally or use a scoped expiring capability. Camera and HID scanners are first-class clients, and batch scans return per-item partial results rather than one all-or-nothing response.
- **Supersedes report guidance:** `hi-events-report.md` §5.6, §7.11, and §9.1 items 12–14. The future check-in ADR and scanner threat model become authoritative.

## Ticket Lookup, Resend, And Self-Service Recovery

- **Trigger:** Guests need to recover an order or admission ticket without signing in.
- **Extends:** `RegistrationOrder`, `AdmissionTicket`, and the existing guest-capability infrastructure with a dedicated recovery purpose.
- **Required boundary:** Use hashed, single-purpose, expiring, rotatable capabilities. Lookup and resend responses remain indistinguishable for existing and absent email addresses; display IDs, email addresses, and public order IDs never authorize access.
- **Supersedes report guidance:** `hi-events-report.md` §5.5 and §9.1 item 11. The future recovery ADR becomes authoritative.

## Promo Codes And Reservation-Aware Usage

- **Trigger:** Organizers need governed discounts after payment and refund semantics are approved.
- **Extends:** Immutable `EventTicketCatalogVersion`, `RegistrationOrderLine`, and inventory reservation calculations with versioned promotion definitions and order-line snapshots.
- **Required boundary:** Usage counts include confirmed redemptions and live unexpired reservations, then release when holds expire or orders cancel. Server-side catalog, tenant, visibility, currency, time-window, and quantity checks remain authoritative.
- **Supersedes report guidance:** `hi-events-report.md` §4.8 and §8 Phase 4 notes. The future promotion ADR becomes authoritative.

## Waitlist Offers With Expiry

- **Trigger:** Capacity-constrained ticket types need a fair promotion path from waitlist to reservation.
- **Extends:** `EventCapacityPool`, `RegistrationInventoryHold`, and registration-order lifecycle with a separate waitlist-entry/offer aggregate.
- **Required boundary:** Offers reserve capacity for a bounded expiry, are idempotent, and cannot oversell across ticket types sharing one pool. Expiry releases capacity once; acceptance competes atomically with cancellation and other offers.
- **Supersedes report guidance:** `hi-events-report.md` §8 deferred commercial inventory and the shared-capacity race findings referenced by §11.2. The future waitlist ADR becomes authoritative.

## Add-Ons And General Products

- **Trigger:** Checkout needs non-admission merchandise or optional services.
- **Extends:** `RegistrationOrder` through separate product and order-line concepts; ticket entitlements remain unchanged.
- **Required boundary:** General products never enter admission vocabulary, capacity entitlements, participant assignment, or check-in state. Their pricing and fulfillment snapshots remain distinct even when purchased in the same order.
- **Supersedes report guidance:** `hi-events-report.md` §8 deferred commercial inventory and §11.4 scope discipline. The future product-catalog ADR becomes authoritative.

## Taxes, Fees, And Invoices

- **Trigger:** A jurisdiction and payment workstream defines tax authority, invoice numbering, correction, and retention requirements.
- **Extends:** `RegistrationOrder`, `RegistrationOrderLine`, and `PlatformFeePolicy` with immutable calculation snapshots plus separate invoice documents and lifecycle.
- **Required boundary:** Persist integer minor units and explicit currency; snapshot tax/fee basis and jurisdiction at calculation time; keep platform contributions outside organizer earnings, tax assumptions, and ticket price; never infer a legal invoice from a checkout receipt.
- **Supersedes report guidance:** `hi-events-report.md` §7.3, §7.5, and §8 deferred payment/admission inventory. The future tax/invoice ADR and jurisdiction-specific legal review become authoritative.

## Research Evidence Boundary

- Repository evidence: `registration-data-collection-plan.md` §Phase 14.8 and `hi-events-report.md` §§4.8, 5.5, 5.6, 7.3, 7.5, 7.10, 7.11, 8, 9.1, and 11.4.
- Context7 was available for current ASP.NET Core, EF Core, and MudBlazor documentation relevant to the wider Phase 14 work.
- Tavily MCP was requested but is not registered in this session; no Tavily result or external re-verification is claimed.
