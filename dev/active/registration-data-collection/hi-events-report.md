<!-- ABOUTME: Deep source-grounded assessment of Hi.Events ticketing architecture against the ISLAMU registration platform plan. -->
<!-- ABOUTME: Records reusable behaviors, incompatible choices, implementation risks, licensing constraints, and concrete plan recommendations. -->

# Hi.Events Ticketing Architecture Report

> **Audience:** ISLAMU Event contributors, architects, and implementation agents
> **Status:** Research snapshot; not an approved architecture decision
> **Last verified:** 2026-07-20
> **Hi.Events source:** `/home/amir/dev/Github/Hi.Events` at commit `9de8863ae7db5cc8ab17a580df8bd6c5fe79c663` (`develop`, 2026-07-19)
> **ISLAMU source:** `dev/active/registration-data-collection/` as read on 2026-07-20
> **Scope:** Ticket catalog, capacity, checkout, orders, participants, questions, payment, refunds, ticket delivery, self-service, check-in, frontend UX, architecture, security, testing, and licensing

---

## 1. Executive conclusion

Hi.Events is a mature, pragmatic implementation of an event-commerce lifecycle. It demonstrates how a real system connects product selection, expiring reservations, buyer and attendee collection, Stripe and offline payment, ticket delivery, self-service, refunds, shared capacity, waitlists, and staff check-in. Its strongest value to ISLAMU is empirical: it shows which states, failure screens, organizer controls, and operational workflows a production ticketing product eventually needs.

Hi.Events may be used as an AGPLv3 code and behavior source. It should not replace ISLAMU's source architecture wholesale where its implementation is weaker.

The current ISLAMU registration plan is materially stronger in the areas that are hardest to retrofit later:

- immutable published ticket catalogs;
- buyer/order/participant/assignment separation;
- explicit PII and sensitive-value isolation;
- typed atomic answers instead of canonical JSON;
- tenant-wide persistence enforcement;
- hashed, scoped, expiring guest capabilities;
- explicit state-machine rules;
- transactional outbox and durable incoming-event processing;
- HAL-authored action affordances;
- UUIDv7 aggregate identity;
- deterministic capacity-pool locking and fenced holds;
- consent, retention, purpose-gated export, and audited access.

Hi.Events is stronger today in implemented commercial breadth and user experience:

- paid, free, donation, and tiered prices;
- promo-gated and hidden inventory;
- fees, taxes, refunds, invoices, and Stripe Connect;
- offline-payment operations;
- ticket lookup and resend;
- organizer order and attendee management;
- check-in lists, camera scanning, HID scanners, and partial batch results;
- waitlist offers and expiry;
- business-state-specific checkout recovery.

The recommended reuse boundary is therefore:

1. **Adopt behavior and workflow lessons.**
2. **Adapt the concepts to ISLAMU's planned aggregates and infrastructure.**
3. **Reject direct ports of the persistence, authorization, money, idempotency, and side-effect machinery.**
4. **Reuse beneficial Hi.Events code and patterns under AGPLv3 without importing its removable product-branding demand.**

These are technical fit decisions, not licensing restrictions on reuse.

Hi.Events' extra demand for linked “Powered by Hi.Events” branding is treated as a removable AGPLv3 further restriction. It is not an implementation blocker, must not be added to ISLAMU Event, and must not prevent the team from reusing beneficial Hi.Events code or patterns under AGPLv3. Normal AGPLv3 source, notice, and provenance obligations still apply.

---

## 2. Research method and evidence boundary

This report was produced from local repositories only. No Hi.Events service was deployed and no browser-based runtime test was performed.

The ISLAMU review covered every line of:

- `registration-data-collection-context.md`;
- `registration-data-collection-plan.md`;
- `registration-data-collection-tasks.md`;
- `registration-data-collection-consultation.md`.

That is 5,499 lines across the complete active workstream. The comparison is therefore against the plan's actual decisions and phases, not against a generic registration design.

The Hi.Events review covered:

- repository instructions: `AGENTS.md`, `CLAUDE.md`;
- license and provenance: `LICENCE`, `CLA.md`, `VERSION`, manifests, Git state;
- architecture documentation under `backend/docs/`;
- backend and frontend manifests;
- baseline schema, subsequent migrations, generated domain objects, Eloquent models, repository contracts, and repository implementations;
- public and authenticated API routes;
- order creation, completion, cancellation, payment, refund, questions, attendees, capacity, waitlist, ticket lookup, ticket delivery, and check-in paths;
- Laravel events, listeners, jobs, queue configuration, and outbound webhooks;
- React public event, widget, checkout, payment, summary, organizer, question, capacity, attendee, order, and check-in surfaces;
- relevant backend tests and CI configuration.

The inspected Hi.Events tree was clean. Its commit identity is pinned because the manifest's `1.11.0-beta` version is not enough to reconstruct the inspected source; `git describe` identifies it as `v1.7.1-beta-38-g9de8863a`.

### Evidence interpretation

Hi.Events initializes from `backend/database/migrations/schema.sql` and then applies dated migrations. The baseline still uses historical `ticket*` names; a 2024 migration generalized those tables to `product*`. Current generated domain-object abstracts reflect the evolved columns, but do not preserve all foreign-key, check, or index information. Claims about the current schema therefore use all three sources:

1. baseline schema;
2. subsequent migrations;
3. current generated objects and runtime code.

Architecture documents were treated as guidance, not absolute truth. There is observable drift:

- the docs mention Laravel 11 while `backend/composer.json` uses Laravel 12;
- docs describe resources under `app/Http/Resources`, while code uses `app/Resources`;
- docs describe domain objects as immutable, but generated objects expose fluent setters;
- new guidance prefers Spatie Laravel Data, while important checkout DTOs still extend the older base DTO.

Package configuration and executable code were considered authoritative where they disagree with prose.

---

## 3. Hi.Events system shape

### 3.1 Stack

Backend:

- PHP 8.2+;
- Laravel 12;
- PostgreSQL;
- Eloquent behind repositories;
- Spatie Laravel Data;
- JWT authentication plus Sanctum dependencies;
- Stripe and Stripe Connect;
- Brick Money at payment boundaries;
- Spatie Webhook Server;
- Laravel queues and failed-job storage;
- Sentry;
- DOMPDF, Excel export, Liquid templates, and HTMLPurifier.

Frontend:

- React 18;
- TypeScript;
- React Router 7;
- TanStack React Query 5;
- Mantine 8;
- Lingui;
- Vite SSR with Express;
- Stripe Elements;
- `qr-scanner`;
- TipTap and Recharts.

ISLAMU is a different technical environment: .NET, EF Core/Npgsql, MediatR/CQRS, Blazor/MudBlazor, BFF authentication, Cerbos authorization, HAL actions, and durable outbox/inbox infrastructure. That makes direct framework code reuse low-value even before licensing is considered.

### 3.2 Architectural organization

Hi.Events is a DDD-inspired Laravel modular monolith organized horizontally:

```text
HTTP Action / FormRequest
          |
          v
Application Handler
          |
          v
Domain Service
          |
          v
Repository Interface -> Eloquent Repository -> Eloquent Model -> PostgreSQL
          |
          +-> Domain events / Laravel events -> listeners / jobs / webhooks
          |
          +-> JSON Resource -> React client
```

Representative paths:

- HTTP: `backend/app/Http/Actions`, `backend/app/Http/Request`;
- use cases: `backend/app/Services/Application/Handlers`;
- business services: `backend/app/Services/Domain`;
- persistence contracts: `backend/app/Repository/Interfaces`;
- persistence implementations: `backend/app/Repository/Eloquent`;
- record-like domain objects: `backend/app/DomainObjects`;
- response representations: `backend/app/Resources`;
- asynchronous work: `backend/app/Events`, `Listeners`, and `Jobs`.

The documented route is `Action -> Handler -> Domain Service -> Repository`, but the repository contract explicitly permits handlers to use repositories directly. The real code uses both forms.

This is not strict Clean Architecture:

- domain services directly depend on Laravel's `DatabaseManager`, translations, and infrastructure event dispatchers;
- domain objects use Laravel collections, Carbon, and framework translation;
- the broad base repository exposes Laravel pagination, collection, and relationship types;
- a repository contract imports an HTTP query DTO;
- no project boundary or architecture test enforces dependency direction.

This trade-off buys speed and convention familiarity. It also allows cross-layer coupling and drift that ISLAMU's assemblies and architecture tests are designed to prevent.

### 3.3 Generated domain objects

`backend/app/Services/Infrastructure/DomainObjectGenerator/ClassGenerator.php` introspects the live database with Doctrine DBAL and regenerates abstract domain-object classes. Concrete subclasses survive regeneration and add relationships or behavior.

These objects are typed database-record mirrors rather than persistence-independent aggregates:

- properties correspond closely to columns;
- hydration is simple array assignment;
- generated classes contain getters, setters, field constants, and `toArray()`;
- state transitions are spread across handlers and services rather than encapsulated by aggregate rules.

The repository layer generally returns these domain objects instead of DTOs. That aligns with one ISLAMU repository invariant. The similarity stops there: ISLAMU should retain narrow aggregate-specific repositories, handler-owned mapping, and framework-independent domain behavior.

---

## 4. Ticketing data model

### 4.1 Core relationship graph

```text
Account
  |
  +-- Organizer
  |
  +-- Event
        |
        +-- EventSetting
        |
        +-- ProductCategory
        |
        +-- Product [TICKET | GENERAL]
        |     |
        |     +-- ProductPrice [PAID | FREE | DONATION | TIERED | REGISTRATION]
        |     +-- ProductQuestion --> Question
        |     +-- ProductTaxAndFee --> TaxAndFee
        |     +-- ProductCapacityAssignment --> CapacityAssignment
        |
        +-- Order
        |     |
        |     +-- OrderItem
        |     +-- Attendee
        |     |     +-- AttendeeCheckIn
        |     +-- QuestionAnswer --> Question
        |     +-- StripePayment / Refund / Invoice
        |
        +-- CheckInList --> allowed Products
        +-- PromoCode
        +-- WaitlistEntry
```

The important conceptual choice is that a “ticket” is no longer a table-level aggregate. A `Product` has an independent `product_type`:

- `TICKET`: purchasing a unit creates an attendee/ticket identity;
- `GENERAL`: purchasing a unit creates no attendee.

The product also has a pricing behavior:

- `PAID`;
- `FREE`;
- `DONATION`;
- `TIERED`;
- `REGISTRATION`.

This is flexible for an event-commerce product because one cart can contain admissions and merchandise. It is less precise for ISLAMU's registration bounded context, where ticket types, entitlements, participant assignments, and eventual admission credentials have different lifecycles.

### 4.2 Event and event settings

`EventDomainObjectAbstract` contains account, organizer, creator, title, description, currency, date/time, location, status, attributes, and soft-delete fields. Internal IDs are generated integers. A legacy event-level ticket quantity field remains even though current availability is product/capacity based.

`EventSettingDomainObjectAbstract` carries substantial ticketing policy:

- reservation timeout, defaulting to 15 minutes;
- whether attendee details are required;
- `PER_TICKET` or `PER_ORDER` attendee collection;
- billing-address requirements;
- enabled payment providers;
- offline-payment instructions and policy;
- marketing opt-in behavior;
- attendee self-edit and resend behavior;
- waitlist settings;
- copy-buyer-details behavior;
- check-in behavior for offline-payment orders.

Hi.Events centralizes many checkout switches in one settings record. The behavior is easy to expose to the frontend, but the table is a broad configuration bag and its `event_id` is not uniquely constrained despite being modeled as one-to-one.

ISLAMU's D10 decision is stronger: typed participation configuration must remain separate from ticket catalog, collection workflow, commercial authority, and provider channel configuration.

### 4.3 Product and product price

`Product` contains:

- event and optional category;
- title and description;
- product and price types;
- sale window;
- minimum and maximum quantity per order;
- visibility and hiding rules;
- promo-only visibility;
- sold-out display behavior;
- ordering/highlight state;
- sales aggregates;
- optional waitlist override.

`ProductPrice` contains:

- product identity;
- numeric price;
- optional tier label;
- independent sale window;
- initial quantity;
- quantity sold;
- display ordering and hiding.

For tiered pricing, separate price rows provide separate labels, dates, prices, and quantities.

Strengths:

- ticket and general-product carts are unified;
- tier inventory can be controlled independently;
- client-submitted persisted prices are not trusted;
- visibility, ownership, sale dates, promo gating, min/max quantities, and stock are validated server-side;
- a product can participate in multiple shared capacities.

Weaknesses:

- published catalog state is mutable in place;
- price history is not a versioned aggregate;
- a price update service acknowledges that reserved products are not fully considered;
- there is no immutable catalog revision pinned by the order;
- `quantity_sold` and `used_capacity` are denormalized counters;
- most state and numeric invariants exist only in application code;
- prices become PHP floats in important runtime paths despite numeric database columns.

ISLAMU should keep `TicketCatalogVersion`, `EventTicketType`, `TicketTypeEntitlement`, and immutable publication. A `GENERAL` add-on abstraction may be useful later, but should not be allowed to dilute the admission vocabulary in Phases 4–6.

### 4.4 Capacity assignments

Hi.Events models a named shared capacity:

- event;
- name;
- nullable capacity, where null means unlimited;
- denormalized `used_capacity`;
- `EVENT` or `PRODUCTS` applicability;
- `ACTIVE` or `INACTIVE` status;
- many-to-many product assignment.

For a price, displayed availability is the minimum of:

1. price stock minus sold units minus live reservations;
2. every active shared capacity linked to that product.

This is the right product behavior: several ticket types can consume one physical capacity.

The implementation is not a complete correctness model:

- no database check guarantees nonnegative capacity;
- no check guarantees `used_capacity <= capacity`;
- the product/capacity association is unique, but active admission/check-in uniqueness is weaker elsewhere;
- availability and mutation depend on counters plus derived reservation totals;
- event-wide checkout locking is coarse;
- shared-capacity validation has a race described in Section 7.

The ISLAMU D11/Phase 5.3 design should retain explicit capacity pools and holds, but use deterministic locking of affected pool rows, active-hold accounting, conditional transitions, and concurrency integration tests.

### 4.5 Order and order item

`Order` combines:

- event, promo, affiliate, and public identifiers;
- session identifier;
- buyer name, email, locale, address, notes, and marketing opt-in;
- reservation deadline;
- order, payment, and refund states;
- currency and financial totals;
- payment provider/gateway;
- tax/fee rollup JSON;
- point-in-time JSON;
- cancellation-statistics marker;
- timestamps and soft deletion.

`OrderItem` records:

- order, product, and product-price references;
- product type;
- quantity;
- item-name snapshot;
- effective price;
- pre-discount price;
- line totals before additions, tax, fee, and gross;
- tax/fee rollup.

The order-line snapshot is one of Hi.Events' best design choices. Product title, effective price, discount basis, tax, fees, and totals remain available when the mutable product definition changes.

It is still not a complete immutable commercial record:

- the line does not pin a catalog revision;
- line currency is inherited from the order;
- participant collection mode and entitlement are not pinned;
- tax/fee history is partly JSON;
- order buyer PII is inline;
- `point_in_time_data` appears declared but unused in the inspected paths.

ISLAMU Phase 5.2 should keep named snapshot columns, including ticket name, price, currency, catalog version, entitlement-relevant facts, and any policy facts required to interpret the purchase. `RegistrationOrderPii` should remain separate.

### 4.6 Attendee

Hi.Events creates one `Attendee` for each purchased unit whose product type is `TICKET`. General products create no attendee.

Attendee fields include:

- order, event, product, and price references;
- short and public IDs;
- first name, last name, and email;
- locale and notes;
- `ACTIVE`, `AWAITING_PAYMENT`, or `CANCELLED`;
- legacy/current check-in fields;
- soft deletion.

For `PER_ORDER`, buyer identity is copied to every attendee. For `PER_TICKET`, each ticket collects its own attendee identity.

This proves the core buyer-versus-attendee need behind ISLAMU D4, but Hi.Events' attendee is overloaded:

- participant identity;
- ticket credential identity;
- product assignment;
- payment activation state;
- admission state;
- contact PII.

ISLAMU's proposed model is more precise:

```text
Buyer
  -> RegistrationOrder
      -> RegistrationOrderLine
      -> Participant
          -> ParticipantPii
          -> ParticipantTicketAssignment
              -> EventRegistration / eventual AdmissionTicket
```

That separation supports group bookings, deferred assignment, guardians, participants without accounts, multiple session entitlements, privacy erasure, and future credential replacement.

### 4.7 Questions and answers

Questions belong to an event and are classified as:

- `ORDER`; or
- `PRODUCT`, with product associations.

Supported types include:

- address;
- phone;
- single-line and multiline text;
- checkbox;
- radio;
- dropdown;
- multi-select dropdown;
- date.

Questions store options as JSON. Answers store a single JSON value with order plus optional product and attendee references. Required/choice/length validation is performed by custom Laravel validation rules.

Useful behavior:

- buyer questions and per-ticket questions are separate;
- required questions are revalidated server-side;
- product questions can target selected ticket types;
- general products can have answers without attendees;
- organizer answer editing and export exist.

Structural weaknesses:

- no immutable form or question version is pinned;
- historical reports join current question title/type/options;
- answer identity is not uniquely constrained;
- the database does not enforce order-answer versus attendee-answer shape;
- one JSON column is the canonical value for every type;
- sensitive answers are not separated;
- consent evidence is not linked;
- question deletion/editing can change historical interpretation;
- the reporting view does not consistently filter soft-deleted source rows.

This directly validates ISLAMU D5 and Phases 7–8. Do not copy the JSON answer model. Preserve one atomic typed answer row, check constraints, versioned definitions, sensitive-value separation, consent evidence, and normalization/finalization pipelines.

### 4.8 Taxes, fees, promo codes, affiliates, and statistics

Hi.Events includes commercial concerns that ISLAMU intentionally defers:

- fixed and percentage taxes and fees;
- product-level tax/fee assignment;
- order-item and order rollups;
- Stripe Connect application fees;
- optional buyer-paid platform fees;
- promo codes scoped to event/products;
- fixed and percentage discounts;
- affiliates and sales attribution;
- invoices;
- event and daily statistics;
- refunds and partial refunds.

Good lessons:

- mutable tax/fee definitions are snapshotted into order lines;
- fees are calculated before percentage taxes where configured;
- promo usage includes live, unexpired reservations rather than completed orders only;
- derived statistics use a version field and retry logic;
- refund state is orthogonal to order and payment state.

Do not import this code into the initial ISLAMU ticketing phase:

- financial code uses floats in important seams;
- rounding decisions are distributed;
- cancellation inventory is attendee-derived and mishandles general products;
- payment/refund operations lack the durable idempotency expected by ISLAMU;
- the registration plan deliberately stops at `AwaitingPayment`.

Use the model only to ensure Phase 5 leaves clean extension points for `PaymentAttempt`, provider payment, refund, invoice, tax, fee, and settlement records.

---

## 5. End-to-end lifecycle

### 5.1 Public selection and reservation

The public store renders server-computed product availability. The React `SelectProducts` component builds quantities per product price/tier, applies promo codes by refetching the event projection, persists affiliate attribution, and posts the selection to:

`POST /public/events/{event_id}/order`

The backend performs broad validation before entering the handler:

- event ownership;
- product ownership;
- product and price visibility;
- promo eligibility;
- min/max per order;
- donation minimum;
- price ID membership;
- per-price stock;
- shared capacity;
- non-empty selection.

`CreateOrderHandler` then:

1. opens a database transaction;
2. takes `pg_advisory_xact_lock(event_id)`;
3. loads the event and settings;
4. deletes the current session's older reserved order;
5. resolves promo and affiliate state;
6. re-reads per-price availability without cache;
7. creates a `RESERVED` order with `reserved_until`;
8. creates priced order-item snapshots;
9. rolls totals into the order.

Availability is derived with:

```text
price stock
- quantity_sold
- quantities in non-deleted, unexpired RESERVED orders
```

An expired reservation remains stored as `RESERVED` but stops consuming displayed availability. There is no general expired-order cleanup job in the inspected implementation.

The transaction-scoped advisory lock is a good minimal PostgreSQL technique. It serializes checkout creation for an event and protects simple price stock. It is too coarse for popular events and, because the full shared-capacity check is outside the lock, does not close every shared-pool race.

### 5.2 Information collection and completion

The checkout displays a countdown from `reserved_until` and moves through:

```text
Select products -> Reserved order -> Details -> Payment (if needed) -> Summary
```

`CompleteOrderHandler` verifies:

- order exists;
- checkout session matches;
- order is still `RESERVED`;
- buyer data has not already been written;
- reservation has not expired;
- supplied price IDs belong to the order;
- ticket attendee count matches purchased ticket quantity.

Inside a transaction it:

- stores buyer/address/marketing data;
- creates attendee rows for ticket products;
- creates order and product question answers;
- gives attendees `ACTIVE` or `AWAITING_PAYMENT`;
- sets free orders to `COMPLETED/NO_PAYMENT_REQUIRED`;
- leaves paid orders `RESERVED/AWAITING_PAYMENT`;
- increments sold/capacity counters immediately for free orders.

After the transaction it raises order-status and webhook-oriented events. The intent is clear, but state transitions remain spread across handlers and services and are not protected by conditional updates or aggregate concurrency tokens.

### 5.3 Payment

Payment providers are Stripe and offline payment.

Stripe flow:

1. public payment-intent creation verifies checkout session, order state, and expiry;
2. an existing local Stripe record is reused if present;
3. otherwise a Stripe PaymentIntent is created, including customer, automatic payment methods, metadata, connected-account information, and optional application fee;
4. a local `stripe_payments` record is created;
5. Stripe Elements confirms payment in the browser;
6. webhook signature verification accepts configured platform secrets;
7. `PaymentIntentSucceededHandler` completes the order, activates attendees, increments inventory/capacity, records fee information, and emits downstream effects;
8. the payment-return page polls and can query Stripe as a webhook fallback.

If a success is processed after reservation expiry, Hi.Events attempts to refund rather than oversell. This is a valuable product rule, but its implementation uses processing time and performs an external refund within a local transaction.

Offline flow:

1. checkout transitions the order to `AWAITING_OFFLINE_PAYMENT`;
2. inventory is converted from expiring reservation to sold/capacity usage;
3. attendees remain awaiting payment unless event settings permit check-in;
4. an organizer or authorized check-in action marks the order paid;
5. invoice, attendee activation, application-fee accounting, and confirmation follow.

The three independent state axes are valuable:

```text
Order:
RESERVED | AWAITING_OFFLINE_PAYMENT | COMPLETED | ABANDONED | CANCELLED

Payment:
NO_PAYMENT_REQUIRED | AWAITING_PAYMENT | AWAITING_OFFLINE_PAYMENT
| PAYMENT_FAILED | PAYMENT_RECEIVED

Refund:
REFUND_PENDING | PARTIALLY_REFUNDED | REFUNDED | REFUND_FAILED
```

ISLAMU D16 should preserve separate machines, but transitions should be centralized in domain rules and executed with concurrency-aware conditional persistence.

### 5.4 Cancellation and refund

Cancellation:

- decrements statistics;
- decrements sold/capacity counters;
- cancels attendees;
- marks the order cancelled;
- sends email;
- dispatches order and capacity events;
- enables waitlist processing.

Refund:

- validates a Stripe payment exists;
- optionally cancels the order;
- calls Stripe;
- optionally emails the buyer;
- marks refund pending;
- reconciles provider refund events into partial/full/failed state and statistics.

The business coverage is useful. The implementation mixes external payment/mail/webhook work with local database transactions, so rollback cannot undo side effects. Cancellation also derives released inventory from attendees, meaning general products that create no attendee can retain overstated sold/capacity counters.

ISLAMU should eventually model cancellation effects from order lines and holds, persist payment/refund provider identities uniquely, and use inbox/outbox processing for every external transition.

### 5.5 Ticket delivery, lookup, and self-service

Hi.Events does not have a distinct `AdmissionTicket` aggregate. The attendee row is the ticket.

Ticket delivery:

- `SendOrderDetailsService` loads completed orders and attendees;
- `SendAttendeeTicketService` sends one ticket email per distinct attendee email;
- the email links to a public attendee ticket page;
- an ICS calendar file is attached;
- “download” is browser printing through the React print routes, not a server-generated PDF.

The QR code directly encodes `attendee.public_id`. That same value is used as:

- a display identifier;
- a lookup identifier;
- the admission credential submitted by scanners.

It is not a separately modeled signed or hashed admission secret. Editing a holder's email rotates a public page short ID in one self-service path, but does not rotate the attendee public ID encoded in the QR. A transfer-like edit therefore leaves the original QR usable.

Ticket lookup:

1. the buyer submits an email address;
2. completed orders are searched;
3. a 24-hour token is generated;
4. older tokens for that email are deleted;
5. an email is queued;
6. the token returns all completed orders for that email.

The response does not disclose whether the email exists, which is good anti-enumeration behavior. The token and associated email are stored plaintext. Email delivery is initiated from inside a local database transaction.

Self-service includes:

- order confirmation resend;
- attendee ticket resend;
- buyer and attendee edits;
- order and attendee display;
- invoice access;
- ticket printing.

These are useful product requirements for a later ISLAMU phase. They should be implemented through scoped, hashed capabilities and server-authored HAL actions rather than reusable short IDs.

### 5.6 Check-in

Hi.Events contains two overlapping admission implementations.

Legacy check-in writes `checked_in_at`, `checked_in_by`, checkout fields, and related state directly on the attendee.

The newer system introduces:

- `CheckInList`;
- selected allowed products;
- optional activation and expiration;
- `AttendeeCheckIn` records;
- public roster/search endpoints;
- check-in and checkout mutations;
- camera and HID scanner clients;
- check-in webhook events.

The check-in service verifies:

- the list exists and is within its active window;
- the scanned attendee exists;
- the attendee's product is allowed by the list;
- the attendee is not cancelled;
- unpaid attendance is allowed by event settings when relevant;
- no current check-in was found for attendee/list.

It returns successes plus per-attendee errors, allowing useful batch partial-result UX. A permitted scan can also mark an offline-payment order paid.

Operational UX is strong:

- debounced roster search;
- manual check-in and checkout;
- camera QR scanning;
- USB/HID keyboard scanner mode;
- camera selection and flash;
- sound preference;
- focus and offline indicators;
- duplicate-scan suppression;
- lazy attendee lookup;
- clear handling for cancelled, duplicate, and unpaid tickets.

Domain and security weaknesses:

- check-in-list public routes are bearer-style and possession of the short ID permits roster access and mutations;
- no dedicated route rate limits were found beyond the global API limiter;
- the QR value is a public identifier, not an admission secret;
- no unique active-check-in database constraint closes concurrent duplicate scans;
- application read-then-create logic can race;
- checkout soft-deletes the check-in instead of recording a first-class append-only transition;
- raw scanner IP addresses are persisted;
- current and legacy check-in state overlap;
- client duplicate suppression is mistaken for part of correctness;
- list access, event ownership, and staff authorization are weaker than ISLAMU's tenant/Cerbos model.

The future ISLAMU model should be:

```text
ParticipantTicketAssignment
  -> AdmissionTicket
      -> signed or hashed admission credential
      -> AdmissionCheckIn event stream / constrained active admission
```

Order, payment, assignment, credential, and admission states must remain independent. Transfer must revoke or rotate the credential. Staff operations should be authenticated and HAL/Cerbos controlled, or use an explicitly designed, scoped, expiring, revocable scanner capability.

---

## 6. Frontend and product-design lessons

### 6.1 Public surfaces

Hi.Events separates:

- public event homepage;
- embeddable product widget;
- checkout;
- payment return;
- order summary;
- ticket print;
- ticket lookup;
- self-service;
- staff check-in.

This separation is worth preserving. Each surface has different security, navigation, caching, and accessibility requirements.

The event homepage uses SSR, canonical metadata, OpenGraph/Twitter tags, Event JSON-LD, localized content, theme variables, organizer information, availability, consent-aware tracking, and the ticket selector.

The widget can open checkout in a new tab and retains a recovery action in the embedding context. Affiliate and promo information are carried into reservation creation.

### 6.2 Checkout state UX

The React checkout is explicitly reservation-aware:

- it shows a countdown;
- it warns before navigation away;
- it can abandon a reservation;
- it distinguishes details, payment, and summary;
- it handles expiration, abandonment, cancellation, payment failure, payment processing, offline payment, and waitlist expiry;
- it lets users copy buyer details to one or all attendees;
- it conditionally renders per-order or per-ticket collection;
- it routes free orders directly to summary;
- it polls after Stripe return and uses a backend Stripe lookup as a fallback.

This is an important lesson for ISLAMU Phase 5.8. The Blazor implementation should be designed around the order state machine, not around a linear form with Boolean flags.

Recommended public states:

```text
Selection unavailable
Reservation created
Reservation expiring
Reservation expired
Details incomplete
Ready to finalize
Awaiting payment
Payment processing
Awaiting offline payment
Completed
Abandoned
Cancelled
Recovery required
```

The server remains authoritative for price, visibility, inventory, capacity, reservation state, and check-in. Client projections are advisory and stale selections are rejected.

### 6.3 Organizer ticket authoring

Hi.Events' organizer product form demonstrates the breadth users expect:

- ticket versus general product;
- paid, free, donation, and tiered pricing;
- independent tier quantities and sale dates;
- tax/fee selection;
- min/max per order;
- sale windows;
- visibility before and after sale;
- hide sold-out products;
- promo-only products;
- forced hiding;
- waitlists;
- highlighting;
- category ordering.

It also teaches shared capacity well:

- organizers create named pools;
- select affected products;
- see used versus total capacity;
- receive warnings that pool capacity can override product quantity.

For ISLAMU:

- retain a dedicated ticket-catalog authoring surface;
- expose publication/version state clearly;
- teach that immutable published versions require a new draft/revision;
- make entitlements and capacity-pool consumption visible;
- defer general products, taxes, and payments unless explicitly brought into scope;
- gate every action using HAL links rather than local status or role tests.

### 6.4 Organizer orders and attendees

Hi.Events supplies two distinct management projections.

Order management includes:

- status/refund filters;
- export;
- buyer messaging;
- customer-link copy;
- invoices;
- offline mark-paid;
- refund;
- resend;
- cancellation;
- nested attendee and answer inspection.

Attendee management includes:

- ticket/tier/status filtering;
- export;
- manual attendee creation;
- edit and notes;
- order, ticket, and answer inspection;
- messaging and resend;
- check-in;
- cancellation and reactivation.

The distinction supports the ISLAMU buyer/order/participant split. ISLAMU should add assignment management as its own explicit concept rather than overloading participant rows.

Hi.Events computes organizer action menus locally from order and attendee status. ISLAMU must not copy this. Edit, cancel, refund, resend, assign, admit, export, and sensitive-data actions must exist in the UI only when the corresponding HAL `_links` are present.

### 6.5 Frontend engineering observations

Good:

- React Query owns server state;
- URL parameters own shareable list filters;
- query keys usually include filters and pagination;
- server validation errors map into forms;
- public checkout has business-specific recovery screens;
- serialized SSR data is script escaped;
- HTML storage is purified server-side;
- metadata and localization are broad;
- successful check-ins update only the relevant cached attendee.

Avoid:

- global query invalidation after order creation;
- unscoped mutation invalidations;
- module-global request-specific HTTP/query/localization state during SSR;
- route-substring authentication exceptions;
- giant 500–760-line workflow components;
- `any`-shaped boundary types and manual contract mirroring;
- navigation during render;
- local action authorization;
- unlabeled icon-only scanner controls;
- toast/audio-only scan feedback;
- raw HTML rendering without server purification;
- client-side duplicate-scan suppression as an idempotency mechanism.

The Blazor implementation should split checkout by state and form section, use typed API contracts, refresh the smallest changed HAL resource, provide labeled scanner controls, and announce scan results through an `aria-live` surface.

---

## 7. Concurrency, correctness, security, and privacy findings

The following are not theoretical differences in style. They are concrete implementation risks found in the inspected Hi.Events source.

### 7.1 Shared-capacity reservation race

The broad `validateOverallCapacity()` call runs before `CreateOrderHandler` obtains the event advisory lock. Inside the lock, the handler rechecks per-price availability, but not the complete shared-pool condition across sibling products.

Two carts selecting different products that consume the same pool can both pass the outside validation. They then serialize under the advisory lock, but the second in-lock check can still overlook the first reservation's effect on the shared pool.

ISLAMU requirement:

- lock every affected `EventCapacityPool` row in deterministic order;
- recount active holds inside the transaction;
- create order, lines, holds, and outbox messages together;
- reject if any pool would go negative;
- use real PostgreSQL integration tests for different ticket types sharing the last capacity.

### 7.2 Duplicate completion race

Order completion loads and validates a `RESERVED` order, then updates it and inserts attendees/answers. There is no row lock, concurrency token, or conditional `WHERE status = RESERVED` transition.

Concurrent duplicate completion requests can both observe the original state.

ISLAMU requirement:

- version/fencing or conditional state transition;
- logical uniqueness for participant assignments and answers;
- idempotency key bound to guest capability and operation;
- one transaction for transition, participant creation, finalization, and outbox.

### 7.3 Payment-intent creation race

Payment-intent creation:

- has no Stripe idempotency key;
- does not lock the order;
- checks for an existing local record before the external call;
- has no observed unique constraint on `stripe_payments.order_id` or `payment_intent_id`;
- calls Stripe before local persistence is safely committed.

A concurrent request or crash after Stripe success can create duplicate or orphan intents.

ISLAMU future payment requirement:

- unique provider attempt identity;
- idempotency key derived from a durable attempt;
- provider call outside the business transaction but coordinated through durable state/outbox;
- reconciliation for provider success without local acknowledgement.

### 7.4 Cache-only webhook idempotency

Stripe event and payment-intent handling use cache keys with about one-hour lifetimes. The check and mark are not atomic, and the marker is not a durable audit record.

Duplicate deliveries can outlive the marker or race before it is written, repeating:

- inventory increments;
- affiliate totals;
- application fees;
- status events;
- emails or webhooks.

ISLAMU must use the existing durable incoming-webhook message/effect model with unique provider event IDs and idempotent effects, as already required by D7.

### 7.5 Expired-payment refund coordination

When a paid order is found expired, Hi.Events calls Stripe refund within a local transaction and then throws. Local rollback cannot undo the remote refund. Retrying may call the refund again, and no Stripe idempotency key was observed.

Expiry is evaluated at webhook processing time, not necessarily provider success time. A delayed webhook can therefore cause a payment completed before expiry to be refunded.

ISLAMU future payment rules should record provider timestamps and reconcile late successes through durable, idempotent state transitions.

### 7.6 Cancellation inventory bug for general products

Cancellation releases inventory by counting attendees. General products create no attendees. Cancelling a mixed or merchandise order can leave `quantity_sold` and shared capacity overstated.

Inventory effects must derive from order lines and hold/commit records, not attendee rows.

### 7.7 Price-tier participant mismatch

Completion verifies that submitted price IDs belong to the order and that total attendee count equals total ticket quantity. It does not verify the exact per-price multiset.

An order with one unit in tier A and one in tier B can potentially submit two attendee entries for tier A while satisfying those checks.

ISLAMU's `ParticipantTicketAssignment` should reference a concrete order line or line allocation, with a database constraint preventing over-assignment.

### 7.8 Public order exposure

`GetOrderPublicHandler` verifies checkout session only while an order remains `RESERVED`. Completed and offline-payment orders can be loaded by short ID without that session verification. `OrderResourcePublic` can include:

- buyer name and email;
- address;
- attendees;
- invoice;
- order items;
- public identifiers.

The short ID is used as a bearer-like access key but is not consistently constrained as a unique, hashed, scoped capability.

ISLAMU Phase 3 correctly requires guest capability primitives. Public display IDs must never double as authorization.

### 7.9 Route ownership gaps

Several public actions load an order or attendee by short ID without consistently binding the route's event ID to the entity. Completion can load event settings from the route event while loading the order solely by short ID.

ISLAMU endpoint handlers must verify the complete tenant/event/resource tuple and return generic not-found behavior for cross-scope identifiers.

### 7.10 Scanner and ticket bearer credentials

The check-in-list short ID grants staff-like roster and mutation access. The attendee public ID is the QR credential. Neither is a separate hashed credential with explicit scope, expiry, revocation, or rotation.

ISLAMU must separate:

- public/display identity;
- guest order capability;
- self-service capability;
- scanner capability;
- admission credential.

### 7.11 Duplicate check-in race

The check-in service fetches existing check-ins and then inserts. No unique active `(check_in_list_id, attendee_id)` constraint was found. Concurrent scans can both pass.

Use either:

- an immutable admission-event ledger plus a constrained current-state projection; or
- a unique active admission constraint with an idempotent command.

### 7.12 PII and sensitive logging

Hi.Events stores buyer and attendee PII inline, lookup tokens and checkout sessions in plaintext, and raw check-in IP addresses. Its Sentry setup can attach user email/name/IP, optional query logging can include bindings, and rejected question answers can reach logs.

ISLAMU should retain:

- PII table separation;
- sensitive-answer split;
- hashed capabilities;
- bounded telemetry;
- no answer values or guest secrets in logs;
- purpose-gated reads and exports;
- retention execution.

### 7.13 Tenant isolation

Hi.Events treats `account_id` as the tenant boundary. Authorization services compare the entity's account to a JWT claim, but there is no central global tenant query filter across repositories. Child records often rely on event ancestry and handler predicates.

A missed action check or missing query predicate can expose another account's data.

ISLAMU's EF global tenant filters, resource-aware policies, BFF token handling, and HAL affordance model are stronger and must remain.

### 7.14 Side-effect durability

Hi.Events has Laravel application events and a second domain-event family for outbound webhooks. There is no general transactional outbox.

Important details:

- several webhook “job” classes use queue traits but do not implement `ShouldQueue`;
- the downstream webhook service uses synchronous dispatch;
- some events, mail, and external payment operations occur inside local transactions;
- database and Beanstalkd queue connections are configured without universal after-commit dispatch;
- cache markers substitute for durable inbox records.

ISLAMU's outbox/inbox, leases, retry/dead-letter lifecycle, and pointer-only messaging must not be replaced by this simpler model.

---

## 8. Comparison with the ISLAMU implementation plan

| Concern | Hi.Events | ISLAMU plan | Verdict |
|---|---|---|---|
| Catalog | Mutable `Product`/`ProductPrice` | Immutable `TicketCatalogVersion` and ticket types | Keep ISLAMU |
| Product breadth | Tickets and general merchandise | Admission-focused ticket catalog | Learn, defer add-ons |
| Price modes | Paid/free/donation/tiered | Price snapshots; payment intentionally deferred | Preserve extension seam |
| Buyer/order/attendee | Separate order and attendee, but PII inline | Buyer/order/participant/assignment with PII split | Keep ISLAMU |
| Capacity | Per-price stock plus shared counters | Named pools plus explicit atomic holds | Keep ISLAMU; borrow UX |
| Reservation | `RESERVED` order with expiry | `RegistrationOrder` plus hold lifecycle | Adopt behavior, strengthen mechanics |
| Concurrency | Event advisory lock, counter updates | Deterministic pool locks/fencing in `IUnitOfWork` | Keep ISLAMU |
| Order lines | Strong commercial snapshots | Named immutable snapshots plus catalog pin | Adopt and extend |
| Participant modes | `PER_ORDER` or `PER_TICKET` | Five explicit collection/assignment modes | Keep ISLAMU |
| Forms | Event questions; JSON options/answers | Versioned forms, sections, fields, typed answers | Reject Hi persistence |
| Sensitive data | Inline PII and JSON answers | PII/sensitive split, purpose-gated access | Keep ISLAMU |
| Consent | Marketing timestamp | Immutable typed consent evidence | Keep ISLAMU |
| Guest auth | Plain session/short IDs | Hashed scoped capability tokens | Reject Hi mechanism |
| Authorization | Action-level roles/account checks | Cerbos/resource policy and HAL actions | Keep ISLAMU |
| Tenancy | Convention and predicates | Global tenant filters plus scoped repositories | Keep ISLAMU |
| Payment | Stripe, Connect, offline, refund, invoice | Deferred after `AwaitingPayment` | Use as future requirements reference |
| Admission | Attendee is the ticket | Deferred `AdmissionTicket` after assignment | Keep ISLAMU separation |
| QR | Public attendee ID | Should be signed/hashed rotatable credential | Reject Hi mechanism |
| Check-in | Product-scoped lists and scanner UX | Deferred explicit admission model | Borrow UX, redesign domain/security |
| Callbacks | Laravel handlers, cache dedupe | Durable incoming webhook effects | Keep ISLAMU |
| Outbound effects | Events/jobs, some synchronous | Transactional outbox | Keep ISLAMU |
| API affordances | Static REST resources | HAL links as UI authority | Keep ISLAMU |
| Frontend | React Query and local status gating | Blazor typed services and HAL gating | Borrow UX only |
| IDs | Integer aggregates, random public strings | UUIDv7 aggregates, int lookups, long cursors | Keep ISLAMU |
| Deletion | Broad soft deletion plus FK cascades | Explicit privacy/retention/erasure authority | Keep ISLAMU |
| Testing | Mostly mock-heavy unit tests | Architecture, unit, integration, API, UI, concurrency | Keep ISLAMU |

### Phase-by-phase impact

#### Phase 0: governance and ADRs

Add this report as research evidence, not as an architecture authority. ADR-018 should explicitly record why the platform does not use “attendee as ticket” or mutable products.

#### Phase 3: guest transaction security

Hi.Events validates the need for a checkout session but demonstrates why a session ID is not enough. Preserve:

- hashed-at-rest capabilities;
- per-order scope;
- operation scope;
- expiry and revocation;
- rotation;
- rate limiting;
- antiforgery decision;
- no query-string bearer token unless explicitly unavoidable and redacted.

#### Phase 4: ticket catalog

Carry forward:

- server-authoritative visibility and price checks;
- min/max per order;
- independent price tiers if required;
- shared-capacity visualization;
- hidden and promo-gated tickets as future rules.

Do not weaken:

- immutable publication;
- catalog version pinning;
- entitlements;
- typed money/currency;
- aggregate identity conventions.

#### Phase 5: orders and holds

Strengthen tasks with explicit tests derived from Hi.Events findings:

- different ticket types racing for one shared pool;
- concurrent duplicate order completion;
- exact order-line allocation to participant assignment;
- expiration concurrent with finalization;
- cancellation releasing every line type;
- idempotent abandon/release;
- old capability after rotation;
- cross-event short/public ID attempts.

#### Phase 6: participants and group bookings

Hi.Events validates participant-per-unit expansion and copy-buyer controls. Preserve ISLAMU's broader model:

- deferred assignment;
- purchaser-only collection;
- named participants;
- guardian/household/company cases;
- participant PII separation;
- assignment to a concrete order line;
- future credential rotation on transfer.

#### Phases 7–8: form authoring and runtime

Hi.Events is useful for field-type and checkout-placement UX only. Do not use its answer storage. Keep:

- immutable form versions;
- field and option identities;
- typed values;
- one atomic answer per row;
- subject typing;
- sensitive separation;
- consent evidence;
- localization;
- validation and normalization;
- idempotent requirement finalization.

#### Phase 13: attendee data and exports

Use Hi.Events' organizer screens as a feature inventory. Enforce ISLAMU's audited, purpose-gated, HAL-controlled export and sensitive-data access.

#### Deferred payment and admission

Create future design notes, not immediate implementation scope, for:

- `PaymentAttempt`;
- provider payment and provider event identity;
- refund;
- invoice;
- tax/fee snapshots;
- offline settlement;
- `AdmissionTicket`;
- admission credential rotation;
- check-in list/entitlement;
- check-in event/current state;
- ticket lookup and recovery;
- transfer acceptance and revocation.

---

## 9. Adopt, adapt, reject

### 9.1 Adopt conceptually

1. **Reserve before collecting personal data.**
   Create an expiring order/hold first, then collect buyer and participant information.

2. **Make reservation time visible.**
   Expose `reserved_until`, countdown, abandonment, and explicit expiry recovery.

3. **Keep server authority.**
   The client displays availability; the transaction makes the decision.

4. **Snapshot commercial facts.**
   Store ticket name, price, currency, discount basis, and other mutable facts on the line.

5. **Separate order, payment, refund, participant, and admission states.**
   Do not collapse them into one approval status.

6. **Support named shared capacity pools.**
   Multiple ticket types may consume one physical limit.

7. **Validate ownership and hidden inventory.**
   Cross-event and hidden identifiers should fail generically.

8. **Separate buyer questions from participant questions.**

9. **Provide buyer-to-participant copy controls.**

10. **Design business-state-specific recovery screens.**

11. **Provide ticket lookup and resend without email enumeration.**

12. **Treat camera and HID scanners as first-class check-in modes.**

13. **Use activation windows and product/entitlement scoping for check-in lists.**

14. **Return partial results for batch admission.**

15. **Count active reservations in future promo limits.**

16. **Use optimistic concurrency for derived projections such as statistics.**

### 9.2 Adapt

| Hi.Events idea | ISLAMU adaptation |
|---|---|
| `Product` with `TICKET` subtype | `EventTicketType` in immutable catalog; add-ons later as separate concern |
| `ProductPrice` tier | Versioned price option or ticket type, depending on entitlement semantics |
| `RESERVED` order | `RegistrationOrder` plus explicit `RegistrationInventoryHold` records |
| Event advisory lock | Deterministic `FOR UPDATE` pool locking or equivalent tenant-scoped lock strategy |
| Attendee per ticket | Participant plus concrete ticket assignment |
| Attendee public ID QR | Rotatable signed/hashed admission credential separate from display ID |
| Check-in list short ID | Authenticated staff resource or scoped expiring scanner capability |
| Soft-deleted check-in | Append-only admission transition with constrained current state |
| Ticket lookup token | Hashed, single-purpose, expiring, revocable recovery capability |
| Order-item JSON rollup | Named typed financial snapshot/value object |
| Laravel event/job | Transactional outbox message and idempotent handler |
| Cache webhook marker | Durable provider-event inbox/effect with unique identity |
| Local status menu | HAL `_links` authored by policy |
| Per-order/per-ticket | ISLAMU's explicit five-mode participant/collection policy |

### 9.3 Reject

- mutable published ticket prices;
- integer IDs for aggregates;
- PHP float money;
- inline order/attendee PII;
- JSON as canonical answer storage;
- plaintext checkout, recovery, or scanner bearer tokens;
- public identifiers used as authorization;
- attendee used as participant, assignment, credential, and admission state;
- cache-only webhook deduplication;
- external calls inside business transactions;
- state transitions distributed without concurrency guards;
- inventory release derived from attendees;
- client-only duplicate-scan suppression;
- local role/status authorization in the UI;
- tenant isolation dependent on every handler remembering predicates;
- direct copying of Laravel/React infrastructure into .NET/Blazor.

---

## 10. Licensing operating rule

Hi.Events and ISLAMU Event are AGPLv3 software. The implementation plan may reuse beneficial Hi.Events code and patterns under AGPLv3.

Hi.Events' root `LICENCE` additionally purports to require linked “Powered by Hi.Events” branding on every generated web page and email. The Free Software Foundation addressed the analogous OnlyOffice product-logo requirement in [“You cannot use the GNU (A)GPL to take software freedom away”](https://www.fsf.org/blogs/licensing/agpl-is-not-a-tool-for-taking-freedom-away). The FSF classified compelled preservation of product branding as a further restriction that recipients may remove under AGPLv3 Section 7.

The operating rule for this workstream is therefore:

- the Hi.Events product-branding demand is not treated as binding;
- no “Powered by Hi.Events” branding is to be added to ISLAMU Event pages, emails, or other product surfaces;
- the branding demand is not a blocker or approval gate for implementation;
- the team may reuse useful Hi.Events code and patterns under AGPLv3;
- copied code must retain valid copyright/license notices, modification history, source availability, and traceable provenance as required by AGPLv3;
- reused code must still be adapted to ISLAMU's architecture, security, privacy, tenancy, HAL, and outbox rules.

For copied code, record the pinned Hi.Events commit and original source path. This is ordinary provenance hygiene, not a separate permission request.

---

## 11. Concrete recommendations for the active plan

### 11.1 Keep the existing core decisions

No evidence from Hi.Events justifies reversing D1–D16. In particular, retain:

- D4 buyer/order/participant/ticket separation;
- D5 typed atomic answers;
- D7 incoming-webhook effects;
- D8 `PublicTransactional`;
- D10 typed participation configuration;
- D11 atomic capacity holds;
- D13 clean-baseline schema;
- D16 explicit state machines.

### 11.2 Add explicit acceptance criteria

Add or ensure the following criteria in implementation tasks:

#### Catalog

- A published catalog version is never mutated.
- An order line pins the catalog version and the exact ticket allocation.
- Hidden/cross-event ticket IDs produce generic not-found behavior.
- Currency and decimal rounding are explicit.

#### Holds

- Every affected pool is locked in deterministic order.
- Reservations for different ticket types consume the same shared pool.
- Expired/released/committed holds have explicit, idempotent transitions.
- An order cannot finalize after its hold expires without a defined recovery path.
- A last-seat race is tested against PostgreSQL, not mocks.

#### Completion

- Completion uses a conditional transition/concurrency token.
- A repeated idempotency key returns the original result.
- A concurrent second completion cannot add participants, answers, or outbox messages.
- Participant assignments cannot exceed their order-line quantity.

#### Public access

- Guest capabilities are stored hashed.
- Display/public IDs never authorize access.
- Event/tenant/order tuple ownership is always verified.
- Capability rotation invalidates the old token.
- Logs never include capability values.

#### Forms

- Answer uniqueness is constrained.
- Subject shape is constrained.
- Historical interpretation is pinned to a form version.
- rejected answers and sensitive values never enter logs.

#### Admission, when implemented

- QR/admission credential is separate from display ID.
- transfer rotates/revokes the credential;
- duplicate concurrent scans are idempotent or conflict deterministically;
- check-in/check-out history is durable;
- scanner authorization is explicit;
- raw IP retention is justified or avoided;
- cancellation/payment changes invalidate admission correctly.

### 11.3 Preserve scope discipline

Do not expand the current implementation phases to clone all Hi.Events commercial features.

Keep deferred unless separately approved:

- Stripe;
- refunds;
- invoices;
- tax and fee configuration;
- affiliates;
- donation pricing;
- general merchandise;
- waitlist promotion;
- admission credential and scanners;
- transfers;
- PDF generation.

The plan should leave extension seams and document future entities, but should still stop at its stated initial commercial boundary.

### 11.4 Add future-work records

Create concise deferred design records for:

- payment attempts and provider reconciliation;
- admission tickets and rotatable credentials;
- check-in lists mapped to ticket entitlements/sessions;
- ticket lookup/recovery;
- transfer acceptance and revocation;
- optional add-ons/general products;
- waitlist offers with expiry.

These records should reference this report so future agents do not rediscover the same repository.

---

## 12. Verification and testing lessons

Hi.Events' backend inventory is weighted toward unit tests, with very few feature tests. CI primarily runs unit tests. Representative order tests use mocks to assert advisory-lock calls and branch behavior, but do not prove PostgreSQL concurrency.

Notably weak or absent coverage includes:

- shared-capacity concurrency;
- duplicate checkout completion;
- duplicate Stripe event delivery;
- payment-intent creation races;
- expired-payment/refund reconciliation;
- cancellation of general products;
- exact tier-to-attendee allocation;
- check-in creation and concurrent duplicate scan;
- checkout/re-entry history;
- scanner authorization and roster exposure;
- QR rotation/revocation;
- cross-event order/attendee access;
- order/attendee/answer transaction integration;
- transfer revocation;
- frontend tests.

ISLAMU's test plan should cover:

1. pure domain transition rules;
2. handler unit tests;
3. EF mapping and check constraints;
4. real PostgreSQL transaction/concurrency tests;
5. API authorization and capability tests;
6. HAL affordance tests;
7. Blazor component and accessibility tests;
8. end-to-end checkout and recovery;
9. outbox/inbox duplicate and retry tests;
10. privacy/logging/export tests.

Mock tests may verify orchestration, but they cannot certify holds, uniqueness, isolation, or concurrent transitions.

---

## 13. Source map

The following local Hi.Events paths are the primary evidence anchors.

### Architecture and configuration

- `AGENTS.md`
- `CLAUDE.md`
- `LICENCE`
- `CLA.md`
- `VERSION`
- `backend/composer.json`
- `frontend/package.json`
- `backend/docs/architecture-overview.md`
- `backend/docs/domain-driven-design.md`
- `backend/docs/api-patterns.md`
- `backend/config/queue.php`
- `backend/app/Providers/RouteServiceProvider.php`
- `backend/app/Http/Kernel.php`

### Schema and domain model

- `backend/database/migrations/schema.sql`
- `backend/database/migrations/2024_07_14_031511_create_capacity_assignments_and_associated_tables.php`
- `backend/database/migrations/2024_08_08_032637_create_check_in_lists_tables.php`
- `backend/database/migrations/2024_09_20_032323_rename_tickets_to_products.php`
- `backend/database/migrations/2024_09_20_032838_add_product_type_to_products.php`
- `backend/app/DomainObjects/Generated/EventDomainObjectAbstract.php`
- `backend/app/DomainObjects/Generated/EventSettingDomainObjectAbstract.php`
- `backend/app/DomainObjects/Generated/ProductDomainObjectAbstract.php`
- `backend/app/DomainObjects/Generated/ProductPriceDomainObjectAbstract.php`
- `backend/app/DomainObjects/Generated/OrderDomainObjectAbstract.php`
- `backend/app/DomainObjects/Generated/OrderItemDomainObjectAbstract.php`
- `backend/app/DomainObjects/Generated/AttendeeDomainObjectAbstract.php`
- `backend/app/DomainObjects/Generated/QuestionDomainObjectAbstract.php`
- `backend/app/DomainObjects/Generated/QuestionAnswerDomainObjectAbstract.php`
- `backend/app/DomainObjects/Generated/CapacityAssignmentDomainObjectAbstract.php`
- `backend/app/Services/Infrastructure/DomainObjectGenerator/ClassGenerator.php`

### Reservation, checkout, capacity, and order

- `backend/routes/api.php`
- `backend/app/Http/Actions/Orders/Public/CreateOrderActionPublic.php`
- `backend/app/Http/Actions/Orders/Public/CompleteOrderActionPublic.php`
- `backend/app/Services/Application/Handlers/Order/CreateOrderHandler.php`
- `backend/app/Services/Application/Handlers/Order/CompleteOrderHandler.php`
- `backend/app/Services/Application/Handlers/Order/GetOrderPublicHandler.php`
- `backend/app/Services/Application/Handlers/Order/Public/AbandonOrderPublicHandler.php`
- `backend/app/Services/Domain/Order/OrderCreateRequestValidationService.php`
- `backend/app/Services/Domain/Order/OrderManagementService.php`
- `backend/app/Services/Domain/Order/OrderItemProcessingService.php`
- `backend/app/Services/Domain/Order/OrderCancelService.php`
- `backend/app/Services/Domain/Product/AvailableProductQuantitiesFetchService.php`
- `backend/app/Services/Domain/Product/ProductQuantityUpdateService.php`
- `backend/app/Services/Domain/CapacityAssignment/UpdateCapacityAssignmentService.php`
- `backend/app/Services/Infrastructure/Session/CheckoutSessionManagementService.php`
- `backend/app/Resources/Order/OrderResourcePublic.php`

### Payment and external effects

- `backend/app/Services/Application/Handlers/Order/Payment/Stripe/CreatePaymentIntentHandler.php`
- `backend/app/Services/Application/Handlers/Order/Payment/Stripe/IncomingWebhookHandler.php`
- `backend/app/Services/Application/Handlers/Order/Payment/Stripe/RefundOrderHandler.php`
- `backend/app/Services/Domain/Payment/Stripe/StripePaymentIntentCreationService.php`
- `backend/app/Services/Domain/Payment/Stripe/EventHandlers/PaymentIntentSucceededHandler.php`
- `backend/app/Services/Domain/Payment/Stripe/EventHandlers/PaymentIntentFailedHandler.php`
- `backend/app/Services/Domain/Payment/Stripe/StripePaymentIntentRefundService.php`
- `backend/app/Services/Domain/Order/MarkOrderAsPaidService.php`
- `backend/app/Services/Infrastructure/DomainEvents/DomainEventDispatcherService.php`
- `backend/app/Listeners/Webhook/WebhookEventListener.php`
- `backend/app/Jobs/Order/Webhook/DispatchOrderWebhookJob.php`

### Questions, ticket delivery, recovery, and admission

- `backend/app/Validators/Rules/BaseQuestionRule.php`
- `backend/app/Validators/Rules/OrderQuestionRule.php`
- `backend/app/Validators/Rules/ProductQuestionRule.php`
- `backend/app/Services/Domain/Question/CreateQuestionService.php`
- `backend/app/Services/Domain/Question/EditQuestionAnswerService.php`
- `backend/app/Services/Domain/Attendee/SendAttendeeTicketService.php`
- `backend/app/Services/Domain/Mail/SendOrderDetailsService.php`
- `backend/app/Mail/Attendee/AttendeeTicketMail.php`
- `backend/app/Services/Application/Handlers/TicketLookup/SendTicketLookupEmailHandler.php`
- `backend/app/Services/Application/Handlers/TicketLookup/GetOrdersByLookupTokenHandler.php`
- `backend/app/Services/Domain/CheckInList/CreateAttendeeCheckInService.php`
- `backend/app/Services/Domain/CheckInList/CheckInListDataService.php`
- `backend/app/Services/Application/Handlers/CheckInList/Public/CreateAttendeeCheckInPublicHandler.php`

### Frontend

- `frontend/src/router.tsx`
- `frontend/src/components/layouts/EventHomepage/index.tsx`
- `frontend/src/components/layouts/Checkout/index.tsx`
- `frontend/src/components/layouts/CheckIn/index.tsx`
- `frontend/src/components/routes/product-widget/SelectProducts/index.tsx`
- `frontend/src/components/routes/product-widget/CollectInformation/index.tsx`
- `frontend/src/components/routes/product-widget/PaymentReturn/index.tsx`
- `frontend/src/components/routes/product-widget/OrderSummaryAndProducts/index.tsx`
- `frontend/src/components/forms/ProductForm/index.tsx`
- `frontend/src/components/forms/QuestionForm/index.tsx`
- `frontend/src/components/forms/CheckInListForm/index.tsx`
- `frontend/src/components/common/AttendeeTicket/index.tsx`
- `frontend/src/components/common/AttendeeCheckInTable/QrScanner.tsx`
- `frontend/src/api/order.client.ts`
- `frontend/src/api/check-in.client.ts`
- `frontend/src/api/ticket-lookup.client.ts`

---

## 14. Final architecture recommendation

Use Hi.Events as a production behavior catalog, not as ISLAMU's foundation.

The most valuable lessons to carry into implementation are:

- reserve inventory before collecting PII;
- expose expiry and recovery as first-class UX;
- snapshot every mutable commercial fact;
- model shared capacity explicitly;
- keep buyer, participant, payment, refund, ticket, and admission concepts separate;
- design organizer order/participant tools early;
- plan ticket recovery and resend;
- treat check-in as a real operational subsystem;
- test every state transition under concurrency.

The most important lessons to carry by contrast are:

- a coarse lock is not a complete hold model;
- a public ID is not a capability or admission secret;
- a cache key is not durable idempotency;
- a queued-looking class is not necessarily a durable queue handoff;
- soft deletion is not an audit/event model;
- JSON answers are not typed registration records;
- attendee is not a sufficient long-term ticket aggregate;
- the removable Hi.Events product-branding demand does not block AGPLv3 code reuse and must not be added to ISLAMU Event.

ISLAMU's current registration-data-collection plan should remain the architectural source of truth. Hi.Events should inform acceptance criteria, deferred feature inventory, and UX—not replace the planned bounded context, security model, persistence rules, or licensing posture.
