<!-- ABOUTME: Records the product boundary that keeps marketing, accounting, tax, and legal invoicing outside ISLAMU Event. -->
<!-- ABOUTME: Preserves the removed tax/invoice design and defines a future Qonto-centered integration path. -->

# Event Platform Boundary And External Business Integrations Report

> **Status:** Approved product-direction report; future integration work is not implemented
> **Last Updated:** 2026-08-13 Europe/Brussels
> **Applies to:** ISLAMU Event official instances and self-hosted deployments
> **Related workstream:** [`dev/active/registration-data-collection/`](../active/registration-data-collection/)
> **Payment authority:** [`islamic-value-sensitive-design/i-vsd-paid-event-payments-consultation.md`](../../islamic-value-sensitive-design/i-vsd-paid-event-payments-consultation.md)

---

## 1. Product Boundary Decision

ISLAMU Event remains an **event platform and event-management system**. It owns the workflows that must be authoritative for an event to be listed, sold, admitted, operated, cancelled, refunded, and audited. It does not become a general business suite.

ISLAMU Event owns:

- event discovery, publishing, organizer authority, registration, tickets, capacity, waitlists, and event-bound add-ons;
- event checkout pricing, promotions, payments, refunds, disputes, and buyer-facing payment truth;
- attendee data collected for the event, consent, ticket delivery, admission, QR credentials, check-in, and transfer;
- immutable commercial facts needed to explain what Event charged or refunded;
- durable, consent-aware integration triggers and the health/reconciliation state of each connector.

ISLAMU Event does **not** own:

- newsletter composition, drip campaigns, audience segmentation, marketing automation, or campaign analytics;
- a CRM, general merchandise marketplace, enterprise resource planning system, or business workflow suite;
- bookkeeping, chart of accounts, journals, ledgers, bank feeds, expense management, or statutory accounting;
- jurisdictional tax determination, tax registrations, return preparation, tax filing, or tax advice;
- legal invoice/credit-note numbering, document issuance, e-invoicing networks, or accounting retention policy.

Those excluded capabilities belong to external systems. ISLAMU Event integrates with them through explicit, replaceable boundaries. Listmonk is the established email-marketing example; Qonto is the first planned deep finance/invoicing integration. Neither provider is required for core event operation.

`ProtectedDelayedPayout` remains separately approval-gated in the event plan because it changes the buyer-protection and payment-release promise for an event. It is not an accounting or invoicing feature and never turns Event into a bank, escrow service, or ledger.

## 2. Authority And Data Ownership

| Concern | ISLAMU Event authority | External-system authority |
|---|---|---|
| Event and ticket catalog | Published event, ticket/add-on identity, quantity, final configured price, currency | None |
| Checkout | Reservation, promotion, final amount composition, payment attempt, organizer merchant snapshot | Payment provider executes the payment |
| Refund/cancellation | Event policy, refund reason, requested amount, buyer-facing state | Payment provider confirms money movement |
| Marketing | Explicit contact-sharing consent and bounded event/audience facts | Listmonk owns lists, campaigns, templates, scheduling, delivery, and campaign analytics |
| Tax | Preserve only externally supplied references needed to explain a finalized commercial fact | Qonto or another approved finance/tax system owns classification, rates, jurisdiction rules, and compliance |
| Invoice/credit note | Emit a paid/refunded order integration fact and retain the external document reference/status | Qonto owns client records, numbering, document lifecycle, PDF/e-invoice delivery, and credit notes |
| Accounting | Stable order/payment/refund correlation and export facts | Qonto/accounting software owns ledger, transaction matching, categories, statements, and retention |

An external document reference is evidence that the provider accepted or issued something; it is not a second ISLAMU invoice aggregate. Event must never show “invoice issued” or “credit note issued” from a queued request, browser redirect, timeout, or local guess.

## 3. Integration Strategy

### 3.1 Supported integration paths

Future business-system integrations should offer the smallest useful set of paths:

1. **Manual/export path** — bounded CSV or accounting-event export for self-hosters that do not want a live provider connection.
2. **Generic automation path** — signed outbound webhooks or durable integration outbox facts for a self-hoster's own middleware.
3. **Deep native connector** — provider-specific connection, mapping, delivery, reconciliation, health, and recovery, beginning with Qonto.

Do not build a speculative universal accounting-provider framework. Reuse the existing repository-native integration seams and add the smallest Qonto-specific adapter. Extract a broader abstraction only when a real second finance provider proves the shared contract.

### 3.2 Repository-native execution model

The current Listmonk integration provides the local pattern to reuse:

- non-secret settings are resolved through the governance hierarchy;
- credentials are isolated in the secret store and are never returned by APIs;
- Application owns provider-neutral commands/ports and Infrastructure owns the HTTP client;
- connection testing is explicit and sanitized;
- external side effects start only after the Event transaction commits;
- durable outbox processing is at-least-once, retryable, and observable;
- provider health is bounded and does not expose credentials or customer data;
- API and Blazor actions are emitted and consumed through HAL relations.

Finance integration adds stricter requirements: immutable order/payment/refund correlation, actor-bound connection ownership, provider-side idempotency where supported, local operation claims beyond the provider's idempotency window, monotonic status, reconciliation after ambiguous outcomes, and an auditable disconnect/credential-rotation path.

## 4. Qonto Deep Integration Recommendation

### 4.1 Availability and non-blocking behavior

Qonto is an optional connector, not a dependency of paid events. Qonto currently serves businesses in a bounded set of European markets, while ISLAMU Event targets global organizers. Instances and organizers outside Qonto's supported account footprint must retain export/webhook paths and may later use another approved connector.

Disabling or disconnecting Qonto must never block event publication, Stripe checkout, refunds, ticket delivery, or check-in. It disables new Qonto sync and keeps historical external references and retry/audit evidence readable.

### 4.2 Connection ownership

For a platform serving multiple organizers, the safe default is OAuth 2.0 bound to the actual event-organizer actor and the Qonto organization selected during consent. The conceptual binding is:

```text
(InstanceId, TenantId, OrganizerActorId, ProviderCode, ExternalOrganizationId)
```

Rules:

- the event organizer connects and authorizes its own Qonto organization;
- a tenant or instance administrator may enable/disable the connector as policy, but cannot substitute its Qonto organization as the organizer's finance destination;
- each self-hosted operator supplies its own Qonto developer application credentials;
- OAuth uses CSRF state validation, least-privilege scopes, encrypted refresh-token storage, rotation, revocation, and an explicit disconnect flow;
- `offline_access` is requested only when durable background synchronization is enabled;
- Qonto API-key mode is limited to a clearly labeled single-business/self-automation deployment where current Qonto endpoint access permits it; it is not the multi-organizer default;
- Event never requests or stores a Qonto password, bank credential, full bank feed, or payment-initiation scope for invoicing.

Initial least-privilege scope candidates are `client.read`, `client.write`, `client_invoices.read`, and `client_invoice.write`, plus `offline_access` for background work. Broader scopes such as `organization.read`, transaction access, attachments, or e-invoicing are separate opt-ins justified by an implemented use case. Exact scope names and production approval requirements must be reverified when implementation starts.

### 4.3 Configurable synchronization modes

The future connector should support these organizer-visible modes:

| Mode | Behavior | Safe default |
|---|---|---|
| `Disabled` | No Qonto data transfer | Yes |
| `ExportOnly` | Organizer downloads bounded commercial facts | Available without Qonto credentials |
| `DraftDocuments` | Confirmed paid orders create/update Qonto clients and draft client invoices; organizer reviews/finalizes in Qonto | Recommended first deep mode |
| `ManagedFinalize` | Event requests Qonto finalization only after explicit mapping, legal/accounting approval, and organizer acceptance | Off by default |
| `RefundCreditNotes` | Successful Event refunds queue a Qonto credit-note request linked to the external invoice | Off until document correlation is proven |
| `Reconciliation` | Poll or consume documented Qonto events to refresh external status and surface drift | Required for any automated document mode |

Instance policy may remove modes; tenant policy may narrow; the organizer chooses only from the effective set. No policy tier may change the Qonto organization or rewrite historical document references.

### 4.4 Commercial-event flow

```text
Event order/payment/refund transaction
  -> commit immutable Event commercial fact
  -> enqueue finance-integration operation
  -> Qonto worker resolves organizer connection + mapping
  -> create/reuse client and draft document outside the Event transaction
  -> store external identifiers and provider status
  -> reconcile webhook/poll result
  -> expose bounded health/retry links through HAL
```

The payload is data minimization by design: stable correlation, organizer, buyer fields required for the selected Qonto operation, event/ticket/add-on line descriptions, integer minor-unit totals, currency, payment/refund timestamps, and externally governed mapping references. It excludes attendee answers, ticket QR credentials, registration capabilities, unrelated purchaser profile data, Stripe secrets, and raw payment-provider payloads.

Tax rate, tax code, legal numbering, and document wording are not calculated by the Event domain. Automation is enabled only when the organizer's Qonto configuration or another approved external mapping supplies every provider-required classification. Missing or ambiguous mapping parks the operation for organizer action; it never guesses.

### 4.5 Reliability and provider truth

Current official Qonto documentation establishes the following implementation constraints:

- customer-facing integrations use OAuth 2.0; unattended work needs refresh-token access;
- the Business API exposes client, client-invoice, finalization, and linked credit-note operations under explicit scopes;
- Qonto can own automatic invoice numbering, and credit notes are linked to existing invoices;
- supported idempotent endpoints use `X-Qonto-Idempotency-Key`, but the documented response cache expires after 30 minutes, so Event must also retain a durable local operation identity and reconcile before recreating a resource;
- webhook callbacks use an HMAC-SHA256 signature over timestamp plus exact raw body, retry on failed/slow acknowledgement, and require fast durable intake before asynchronous processing;
- rate limiting and `429` responses require bounded backoff, while repeated authorization failures must disable retries and surface credential repair.

Do not assume every invoice lifecycle change has a webhook. The implementation must pin the supported event set at development time and use scheduled reconciliation for any missing lifecycle signal.

## 5. Listmonk Boundary

The same product rule applies to email marketing:

- Event decides whether a contact may be shared, why, for which event/organizer, and with which consent evidence;
- the Listmonk connector synchronizes only approved contact/audience facts after commit;
- Listmonk owns subscriber-list operation, campaigns, templates, sending schedules, delivery analytics, and unsubscribe processing;
- Event may show bounded connection/sync health but must not grow a campaign builder or clone Listmonk features.

Other future integrations follow the same pattern: Event emits event-domain facts and consumes bounded provider status; the specialized system owns its professional domain.

## 6. Preserved Removed Tax/Invoice Design

The following design was removed from the active registration implementation plan on 2026-08-13. It is preserved here so the analysis is not lost. It is **not approved Event-owned scope**. A future finance-integration workstream may translate these requirements into provider-owned mappings and acceptance criteria without recreating an internal accounting/tax/invoice subsystem.

### Former Phase 24: Jurisdiction-Gated Tax, Fees, Invoices, And Credit Notes

**Former goal:** Persist truthful tax/fee/invoice facts only where an approved jurisdiction/provider configuration defines their legal authority.

**Former dependencies:** Payment/refund phases and general-product lines.

**Former acceptance boundary:** No global tax guess; merchant/jurisdiction/provider/version/basis/rate/amount snapshots in minor units; invoice separate from receipt; immutable issued documents with correction/credit note; one legal numbering authority; retention/access/audit; feature absent without approved configuration.

**Former relevant files:** Tax capability/configuration/snapshots; invoice/credit-note aggregates/numbering/documents; optional Stripe Tax adapter after conformance; API/HAL/Blazor/operator docs.

**Former guidance:** Domain/Application/EF/API/HAL/Blazor rules, `ip-clean-room`, and `error-tracking`.

**Former phase verification:** One Release build and `tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj` after all phase tasks.

**Former rollback:** Disable new taxable publication/invoice issue for the affected configuration; preserve issued evidence and correct through credit notes, never mutation.

#### Former Task 24.1: Jurisdiction, merchant, legal-review, and provider capability gate

- **Type:** investigate + create
- **Layer:** Docs/Domain/Configuration
- **Files:** tax/invoice capability configuration and evidence record; canonical configuration/admin docs.
- **Description:** Require exact merchant country/jurisdiction, responsible party, registration IDs, approved tax provider/manual authority, invoice numbering/retention/correction policy, and dated counsel/accounting evidence before enablement.
- **Acceptance:** Missing, expired, or unknown evidence disables automation; a Stripe receipt is never represented as a legal invoice; tenant policy cannot broaden instance jurisdiction.
- **Dependencies:** Former Task 15.3 and former Phase 23.
- **Effort:** L

#### Former Task 24.2: Immutable tax/fee calculation snapshots and allocation

- **Type:** create
- **Layer:** Domain/Application/Persistence
- **Files:** tax calculation/line/fee snapshots/rules/configurations/migrations.
- **Description:** Preserve the candidate requirement to record taxable basis, inclusive/exclusive mode, jurisdiction, tax code/rate source/version, rounding/allocation, and separate platform/processor/organizer fee categories. Reuse integer minor-unit/currency metadata.
- **Acceptance:** Line allocations sum exactly; refunds reverse original facts proportionally or explicitly rather than silently recomputing history; contribution treatment is explicit.
- **Dependencies:** Former Task 24.1.
- **Effort:** XL
- **Boundary correction:** These are external-provider facts/references, not an Event tax-calculation aggregate.

#### Former Task 24.3: Tax quote adapter and checkout/payment reconciliation

- **Type:** create
- **Layer:** Application/Infrastructure
- **Files:** narrow tax-quote port and approved Stripe Tax adapter/conformance fixtures; checkout/payment/refund integration.
- **Description:** Preserve the candidate narrow provider quote path in the organizer connected-account context, outside Event transactions, with persisted quote identity/version and revalidation before Checkout.
- **Acceptance:** No speculative tax-provider factory; no provider call inside a business transaction; supported country/currency combinations fail closed; provider total reconciles to the local commercial snapshot; expired or ambiguous quotes block rather than guess.
- **Dependencies:** Former Task 24.2.
- **Effort:** XL
- **Boundary correction:** This remains deferred until a real event-checkout requirement proves that tax must affect the charged amount; it is not part of the Qonto document-sync baseline.

#### Former Task 24.4: Invoice/credit-note lifecycle and immutable document generation

- **Type:** create
- **Layer:** Domain/Application/Infrastructure/Persistence
- **Files:** invoice/document/credit-note entities/rules/repositories; numbering service; renderer/storage; migrations.
- **Description:** Preserve the candidate requirements for authoritative post-payment issue, atomic legal numbering, exact merchant/buyer/event/line/tax/payment references, immutable corrections through credit/replacement documents, and protected document PII.
- **Acceptance:** No unexplained gaps/duplicates beyond documented legal policy; issued document immutable; receipt is not invoice; storage access audited and retention-configured.
- **Dependencies:** Former Tasks 24.2 and 24.3.
- **Effort:** XL
- **Boundary correction:** Qonto or another approved provider owns numbering, rendering, issuance, delivery, retention, and credit-note lifecycle. Event stores only the minimal external reference/status and its own immutable commercial facts.

#### Former Task 24.5: Tax/invoice API, HAL, Studio/attendee UI, and operator documentation

- **Type:** create/modify
- **Layer:** API/Cerbos/Blazor/Ops
- **Files:** admin configuration; invoice issue/read/download/credit endpoints/policies/links; Studio/attendee pages; OpenAPI/NSwag/docs.
- **Description:** Preserve the candidate requirements for configured actions only, private/no-store document access, bounded buyer/merchant status, generic cross-tenant failures, audit, localization, accessibility, provider outage/numbering failure recovery, refund/credit reconciliation, retention, and jurisdiction disablement.
- **Acceptance:** HAL-only issue/download/credit; generic cross-tenant 404; document audit; accessible localized money/tax labels.
- **Dependencies:** Former Tasks 24.1 through 24.4.
- **Effort:** XL
- **Boundary correction:** Event surfaces connector configuration, sync health, retry/reconcile, and provider document links when safe. It does not host an invoice editor, numbering control, tax console, or accounting dashboard.

## 7. Candidate Future Workstream

Create a separate `dev/active/external-finance-integrations/` workstream only when implementation is approved. Its lean sequence should be:

1. accept an ADR for the Event/external-finance authority boundary and exact supported Qonto use case;
2. define the minimal post-commit commercial-event export contract from existing order/payment/refund facts;
3. implement actor-bound Qonto OAuth, secret isolation, settings, connection test, and disconnect/rotation;
4. ship `DraftDocuments` first with client mapping, durable operation identity, reconciliation, health, HAL, and bounded Studio UI;
5. add finalization, credit notes, transaction matching, attachments, or e-invoicing only as individually approved slices with current official evidence.

The future workstream must reference this report and the I-VSD payment consultation. It must not block the core Stripe/admission plan, must not add a generic provider factory before a second provider exists, and must not claim legal/accounting compliance from fixture-green code.

## 8. Risks And Guardrails

| Risk | Guardrail |
|---|---|
| Qonto is treated as globally available | Keep the connector optional; expose export/webhook paths; verify account-country and plan capability at connection time |
| Admin redirects organizer finance data | Bind connection to organizer actor and external organization; no admin fallback; future changes affect new sync only |
| Duplicate external documents after timeout | Durable local operation identity, provider idempotency where supported, lookup/reconciliation before retry |
| Event becomes a tax engine through mapping creep | Mapping references external provider configuration; missing classification parks; no jurisdiction/rate inference in core Domain |
| Refund is shown as a credit note before issuance | Independent sync state; only verified Qonto result advances external document status |
| Excess buyer/attendee data leaves Event | Field allowlist, lawful-purpose/consent gate, no attendee answers or admission secrets, redacted telemetry |
| Provider outage blocks ticketing | Connector work is asynchronous and non-blocking; paid-event checkout/refund/admission remain locally operable |
| Self-hoster assumes ISLAMU operates its finance account | Each instance owns its Qonto app credentials and discloses operator/provider responsibility |

## 9. Source Register And Clean-Room Attestation

### Repository sources

- `docs/INTEGRATIONS.md` — durable intake, privacy-bounded health, and HAL integration rules.
- `src/Explore.Infrastructure/Integrations/Listmonk/ListmonkSyncService.cs` — tenant-scoped settings/secrets and outbox-backed adapter pattern.
- `src/Explore.Infrastructure/HealthChecks/ListmonkIntegrationHealthCheck.cs` — sanitized provider health pattern.
- `src/Explore.API/Controllers/ListmonkIntegrationSettingsController.cs` — sanitized read, authenticated settings, credential rotation, and connection-test surface.
- `dev/active/registration-data-collection/registration-data-collection-plan.md` — removed Phase 24 design and retained payment/admission scope.
- `islamic-value-sensitive-design/i-vsd-paid-event-payments-consultation.md` — organizer-recipient and self-hosted payment-safety authority.

### Official Qonto sources observed through Tavily on 2026-08-13

- [Business API authentication introduction](https://docs.qonto.com/get-started/business-api/authentication/introduction)
- [OAuth flow](https://docs.qonto.com/get-started/business-api/authentication/oauth/oauth-flow)
- [Available OAuth scopes](https://docs.qonto.com/get-started/business-api/authentication/oauth/available-scopes)
- [Create a client invoice](https://docs.qonto.com/api-reference/business-api/expense-management/client-quotes-notes/client-invoices/create-a-client-invoice)
- [Finalize a client invoice](https://docs.qonto.com/api-reference/business-api/expense-management/client-quotes-notes/client-invoices/finalize-a-client-invoice)
- [Create a credit note](https://docs.qonto.com/api-reference/business-api/expense-management/client-quotes-notes/credit-notes/create-a-credit-note)
- [Webhook setup](https://docs.qonto.com/api-reference/business-api/webhooks/setup)
- [Idempotent requests](https://docs.qonto.com/get-started/general/idempotent-requests)
- [Rate limitations](https://docs.qonto.com/get-started/general/rate-limitations)
- [Qonto market availability](https://support-de.qonto.com/hc/en-us/articles/23949292696849-Can-any-organization-open-a-Qonto-account)

Context7 MCP was not registered in this session, so no Context7 result is claimed. The implementation gate requires a current Context7 refresh when available or the same official-documentation fallback with exact API/SDK/version evidence recorded.

This report contains observable interface facts and repository-native requirements only. It includes no third-party source code, snippets, ASTs, SQL, migrations, tests, copied documentation prose, or assets. Future implementation must independently derive its structure from ISLAMU Clean Architecture, CQRS/MediatR, outbox, secret-management, tenant-isolation, and HAL conventions.
