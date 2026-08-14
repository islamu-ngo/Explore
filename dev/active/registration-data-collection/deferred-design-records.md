<!-- ABOUTME: Deferred commerce and admission design records for the registration platform. -->
<!-- ABOUTME: Preserves clean-room decisions and triggers without implementing future payment or admission scope. -->

# Registration Data Collection — Deferred Commerce And Admission Design Records

Last Updated: 2026-08-13 Europe/Brussels

These records preserve the approved behavior lessons from `hi-events-report.md` without copying third-party code, SQL, migrations, snippets, or assets. ADR-022 through ADR-024 now supersede the payment, admission, recovery, promotion, waitlist, add-on, external-business-system, and protected-payout boundaries below. Implementation still follows the phase order and gates in the active plan.

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

## Phase 15 Source Register And Independent-Design Record

### Official interface evidence accessed 2026-08-13

- NuGet Gallery `Stripe.net` 52.3.0 package metadata and license: latest stable release, Apache-2.0, compatible with the repository's `net10.0` target through its supported target frameworks.
- NuGet v3 package index: 52.3.0 is stable; 52.4.0 is prerelease only.
- Stripe.net 52.2.0 and 52.3.0 release records: 52.2.0 pins API `2026-07-29.dahlia`; 52.3.0 adds event parsing helpers and test signature generation without changing that API line.
- Context7 `/stripe/stripe-dotnet` and `/websites/stripe`: instance-based `StripeClient`, per-request `StripeAccount` and `IdempotencyKey`, bounded network retries, raw-body signature verification, connected-account direct-charge Checkout, and asynchronous provider reconciliation facts.
- Context7 `/mdn/content`: browser `BarcodeDetector` requires feature detection and cannot be the only scanner path.
- A temporary isolated `net10.0` restore/probe confirmed assembly `52.3.0.0`, `StripeConfiguration.ApiVersion = 2026-07-29.dahlia`, and the transitive graph `Newtonsoft.Json 13.0.3`, `System.Configuration.ConfigurationManager 9.0.0`, `System.Diagnostics.EventLog 9.0.0`, and `System.Security.Cryptography.ProtectedData 9.0.0`. No package was added to the repository.

Tavily MCP is not registered in this implementation session. Earlier dated Tavily evidence already preserved in the plan/context remains provenance history, but no new Tavily result is claimed.

### AFC/SSO decision

External material was filtered to public interface facts, observable provider constraints, standards, and license metadata. The accepted ADRs independently use ISLAMU's existing aggregate separation, CQRS/Application ports, EF tenant isolation, transactional outbox/inbox, HAL affordances, BFF boundary, integer-minor-unit money, and explicit state machines. Naming, decomposition, operation ordering, persistence relationships, UI authority, tests, and failure taxonomy are repository-native. No third-party source, AST, SQL, migration, test, comment, copied prose, asset, or implementation organization entered the design.

The Phase 15 dependency-policy run exposed metadata failures for `FluentAssertions 8.10.0` and `Microsoft.Data.SqlClient.SNI.runtime 6.0.2`. The assertion dependency was removed on 2026-08-14; the policy now passes, with the steward-approved SNI runtime exception still visible for PostgreSQL-only publication checks.
