<!-- ABOUTME: Clean-room evidence packet for the Event Ticketing Lifecycle successor workstream. -->
<!-- ABOUTME: Carries source-free Phase 22+ constraints without third-party implementation expression. -->

# Event Ticketing Lifecycle Clean-Room Evidence

Last Updated: 2026-08-27 Europe/Brussels

## Provenance

- Source packet SHA-256: `c06e94970f738b8fc20b89895f0425c8ca186b85a0184647fd97cfddbcfeb792`
- Manifest composition: SHA-256 listing of the two predecessor I-VSD reports and ADR-016/017/018/022/023/024; this handoff is deliberately outside its own digest.
- Primary authority: `islamic-value-sensitive-design/i-vsd-registration-data-collection.md` revision `d7723403e6d8b1a70854599a3c4812091290cf505bea7ea0a4558a5e6532d237`
- Secondary payment context: `islamic-value-sensitive-design/i-vsd-paid-event-payments-consultation.md` revision `44e90e5ccb88ba7e98503f0f1b98c00b7bdfaf85d623aff8f7ff882a2a90cb36`
- Successor I-VSD report: `islamic-value-sensitive-design/i-vsd-event-ticketing-lifecycle.md`
- Predecessor research: `dev/active/registration-data-collection/phase22-plus-clean-room-evidence.md` revision `035d7d706169cb0a2b1ff1da8ee35709f8112c361d7b07073588faee2f8f2843`

## Sanitized Functional Constraints

1. Enforce access mode, accepted terms, explicit purchaser actor/context, and the strictest event/tenant/instance ceiling before order mutation.
2. Pin order-versus-participant requirement scope and withhold active admission credentials until subject-correct data, consent, and approval are complete.
3. Transfer future-holder authority atomically, rotate the credential, recollect recipient facts, and preserve purchaser/payment/refund/check-in history.
4. Use deterministic exact-ticket-type waitlist reallocation and request fair-return refunds only after replacement payment is reconciled.
5. Keep add-ons optional, event-bound, separately inventoried/fulfilled, and outside admission and specialist business-system authority.
6. Restore application data together with capability/Data Protection keys, outbox/inbox, Quartz/fences, and provider cursors; prove no duplicate effects or authority resurrection.
7. Keep `ProtectedDelayedPayout` absent. Stable public provider APIs and all named approvals are prerequisites for a separate future workstream.

## Repository-Native Design Anchors

- Existing Domain aggregates: `RegistrationOrder`, `RegistrationParticipant`, `EventTicketType`, `AdmissionTicket`, and `PaymentAttempt`
- Existing transaction/recovery primitives: `IdempotencyMiddleware`, transactional outbox, `OutboxProcessor`, Quartz one-pass jobs, EF tenant filters/locks/concurrency, Data Protection/keyed-digest capabilities
- Existing presentation boundaries: CQRS/MediatR, controller/HAL policies, server-side BFF, generated NSwag client, accessible Blazor components
- Existing evidence: Phase 18C–21 payment/refund/admission/check-in tests, migrations, generated contracts, mutation and MAD records
- Missing by bounded search: concrete `TicketTransfer`, `WaitlistOffer`, and `EventAddOn` production types; successor tasks mark their exact files as new/bounded discovery

## External Public Documentation Register

The 2026-08-27 hardening review retrieved these official documents directly:

| Official source | Source-free functional constraint retained |
|---|---|
| [Stripe webhooks](https://docs.stripe.com/webhooks) | deliveries can retry, duplicate, and arrive out of order; provider observations are monotonic evidence, not transaction truth |
| [EF Core concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency) | optimistic conflicts require explicit resolution/retry behavior |
| [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions) | transaction ownership, execution strategies, and savepoints must be composed deliberately |
| [PostgreSQL explicit locking](https://www.postgresql.org/docs/current/explicit-locking.html) | explicit locks need bounded scope and consistent acquisition order |
| [PostgreSQL transaction isolation](https://www.postgresql.org/docs/current/transaction-iso.html) | serializable executions can abort and must be safe to retry |
| [OWASP Transaction Authorization](https://cheatsheetseries.owasp.org/cheatsheets/Transaction_Authorization_Cheat_Sheet.html) | authorization is server-side, operation-specific, and resistant to request manipulation |
| [ASP.NET Core Data Protection key management](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-management?view=aspnetcore-10.0) | retained key availability and rotation are part of restore correctness |

WCAG 2.2/status-message constraints remain sourced through repository-canonical `docs/ACCESSIBILITY.md` and the accessibility skill verification matrix.

Context7 was not registered in the review session and configured web-search providers returned no results. The reviewer therefore used direct official-document retrieval and records that limitation instead of claiming Context7/web-search evidence.

No external code, snippet, schema, SQL, migration, test, comment, asset, or expressive organization may enter implementation context.

## AFC / SSO / Dependency Decision

- AFC filtration retained only abstract behavior, risk, and official public API facts.
- Naming, phase order, aggregates, flows, tests, and UI structure are independently derived from ISLAMU repository boundaries.
- No new dependency is selected or justified; existing framework/repository primitives are sufficient.
- No intended outbound licensing path changes.
- Suspected contamination requires discarding affected output and restarting from this handoff.
