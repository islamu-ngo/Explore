<!-- ABOUTME: Implementation plan for the Registration Data Collection & Participation Platform workstream. -->
<!-- ABOUTME: Converts the two CTO consultation reports into evidence-grounded, phased, executable slices. -->

# Registration Data Collection & Participation Platform — Implementation Plan

Last Updated: 2026-08-15 Europe/Brussels

---

## Handoff - 2026-08-15 Europe/Brussels: Phase 17 Verification Approved / User Acceptance Pending

### Current State
- Tasks 17.1-17.5 are implemented and independently confirmed. Promotions are versioned, event/catalog-scoped local commercial state with fixed-minor or basis-point discounts, deterministic integer-minor allocation, immediate server-timed revocation affecting only future redemptions, ticket eligibility, validity windows, total redemption limits, and verified-purchaser limits. Orders and lines retain immutable pre-discount, discount, and post-discount snapshots; discounts are applied before platform fees and voluntary contribution.
- `PromotionReservation` owns the live redemption slot. Apply/remove/finalize/cancel/reject/expiry paths are idempotent, count Active plus Consumed use, release or consume exactly once, and share deterministic persistence lock ordering with capacity holds. Five generated `AddEventPromotionCodes` migrations and snapshots cover PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL; all five migration-owner pending-model checks passed.
- Promotion lookup uses the dedicated instance/server-only `promotions.code_lookup_hmac_key` secret with qualified versions `v{LookupKeyVersion}` and non-secret `Promotions:CodeLookup:ActiveKeyVersion`. Infrastructure computes tenant/event-scoped HMAC-SHA256 digests, resolves every persisted candidate key version, compares in fixed time, and fails closed when retained key material is unavailable. Plaintext is returned only once from create/rotate commands; ordinary API, HAL, generated-client, UI, logs, and evidence expose only masked display labels and safe totals.
- Organizer management and authenticated/guest order apply/remove flows are CQRS/API operations governed by exact commercial/order authority, write or PublicTransactional controls, idempotency, rate limits, and server-authored HAL relations. The generated NSwag client now carries a typed `HalResourceOfPromotionManagementDto` collection. Studio and checkout surfaces use those relations only, clear one-time plaintext on lifecycle changes, cancel stale work, and announce separate organizer, discount, fee, contribution, and final totals accessibly.

### Validation
- Independent Wave 1-3 and final F1-F5 verdicts are confirmed. Evidence is retained under `.omo/evidence/phase17-domain/`, `.omo/evidence/phase17-application/`, `.omo/evidence/phase17-persistence/`, `.omo/evidence/phase17-api/`, `.omo/evidence/phase17-ui/`, and `.omo/evidence/phase17-closeout/`.
- Current closeout evidence includes Domain 805/805, Secrets 230/230, API promotion controller 7/7, promotion privacy Architecture 4/4, Studio promotions 12/12, checkout recovery 41/41, registration-order service 6/6, event-promotion service 2/2, lifecycle 42/42, full Application 3708/3708, five-provider model parity/pending-model checks, one-winner redemption races, and nine SQLite lifecycle scenarios. The resolver-disabled Release build passes with zero errors.
- Deterministic UI artifacts are `.omo/evidence/phase17-ui/studio.html` and `.omo/evidence/phase17-ui/checkout.html`; F4 approved their visual/accessibility structure with explicit no-browser/no-pixel limitations. No browser, Aspire, Docker, live database, or live provider execution is claimed. The full Persistence environment gap remains precisely attributed and focused Phase 17 persistence passes 16/16.

### Next Action
1. Surface the five unconditional PASS receipts and wait for user acceptance before making the final Phase 17 completion declaration.
2. Keep Phase 18 and every later payment/refund/admission/payout task unchecked until a separate implementation request starts that slice.

## Handoff - 2026-08-14 Europe/Brussels: Phase 16.4 Complete

### Current State
- Phase 16 remains in progress and Task 16.4 is independently confirmed complete. `Stripe.net` 52.3.0 is centrally pinned and Infrastructure-only; provider-neutral Application contracts orchestrate connected-account creation, hosted onboarding links, and readiness while provider I/O stays outside database transactions.
- A durable account-create operation fence is persisted before remote I/O. Deterministic rejection releases its active slot; ambiguous, canceled, interrupted, or unknown dispatch becomes explicit manual reconciliation rather than blind retry. Signed Connect account events use strict raw-body signature, API-version, Test/Live-mode, identity, timestamp, and historical ownership checks before durable inbox capture and monotonic asynchronous readiness projection.
- The global readiness worker polls bounded Pending/Restricted connections through a named tenant-filter bypass that preserves soft delete, then applies provider observations through tenant-scoped serializable reloads. Checkout/payment, refund/dispute, publication, Cerbos, HAL, and UI remain outside Task 16.4.

### Validation
- Focused final gates passed: operation Domain 4/4, organizer payment Application 35/35, payment persistence 26/26, Stripe Infrastructure 45/45, incoming webhook API 14/14, readiness worker API 2/2, endpoint classification 4/4, Stripe dependency boundary 3/3, and provider migration ownership 5/5.
- Release build exited 0 with 6,319 existing warnings and no errors; dependency policy passed for 646 package/version pairs; `git diff --check` is clean. Independent goal, QA, quality, security, and repository-context reviews pass after the tenant-worker, interrupted-fence, mode-isolation, signed-ingestion, and webhook-documentation repairs.

### Next Action
1. Start Task 16.5: Admin/Studio configuration, publication preflight, Cerbos, and HAL relation gating.
2. Keep Phase 16 verification unchecked until Task 16.5 is complete; do not start Phase 18 Checkout/payment or later admission/payout scope.

## Handoff - 2026-08-14 Europe/Brussels: Phase 16.3 Complete

### Current State
- Phase 16 remains in progress and Task 16.3 is independently confirmed complete. Paid-event policy collections and organizer payment readiness currencies use normalized child rows; portable active-slot uniqueness, tenant/soft-delete filters, entity-returning repository composition, permanent historical provider-account ownership, and tenant-safe replacement lineage are persisted.
- Organizer connection replacement retires the old active slot, inserts the successor, and records the reverse lineage in three saves inside one serializable unit of work. Cross-tenant historical ownership conflicts return only neutral or caller-owned identifiers.
- `payments.stripe.platform_secret_key` and `payments.stripe.webhook_secret` are instance/server-only, non-bootstrap secret definitions under `/stripe`; connected account IDs remain provider identity. PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL generated migrations/snapshots and DBML describe the same seven-table shape. No Stripe package, provider I/O, onboarding, webhook intake, reconciliation, API, Cerbos, HAL, UI, or publication wiring was added.

### Validation
- Focused final gates passed: organizer connection Domain 12/12, paid-policy Domain 18/18, organizer connection Application 19/19, payment persistence 23/23, provider model construction/parity 29/29, Stripe secrets 1/1, and CQRS architecture 5/5.
- Release build exited 0 with 758 existing warnings and no errors; `git diff --check` is clean. PostgreSQL reports no pending model changes. The four alternate-provider CLI reruns were configuration-invalid before comparison, while their generated snapshots were unchanged by the repair and the five-provider model suite passed.
- Independent goal, QA, quality, security, and repository-context reviews pass after the replacement-order, identifier-disclosure, and secret-documentation repairs.

### Next Action
1. Start Task 16.4: official `Stripe.net` hosted onboarding, account events, and readiness reconciliation.
2. Keep Phase 16 open and leave Tasks 16.4-16.5 plus Phase 16 verification unchecked until their implementation evidence exists.

## Handoff - 2026-08-13 Europe/Brussels: Phase 16.1 Complete

### Current State
- Phase 16 is in progress and Task 16.1 is independently confirmed complete. `PaidEventPolicyVersion` models instance and tenant versioned ceilings with nullable `TenantId` plus active revisions. `PaidEventPolicyRules` validates tenant subset/narrowing, allowed organizer kinds reuse `ActorTypeEnum`, currencies reuse `CurrencyMetadata` in EUR/USD/MAD/SAR/AED order and require explicit confirmation, disabled/inactive/invalid policies fail closed, venue suggestion never authorizes, refund hard-floor protection uses source-named `PaidEventRefundProtection` values and cannot be removed, risk ceilings are currency-qualified via `PaidEventPolicyCurrencyRiskLimit`, and first-paid-event/far-future review thresholds narrow only.
- No `EventTicketCatalog` publication wiring, Stripe package, persistence, migration, connection, provider, API, or UI work was added.

### Validation
- TDD and review evidence: initial RED for missing types; first adversarial review rejected the version for fail-open currency/refund handling, ambiguous thresholds, and enumerable exposure; repairs added regressions; second review found a hard-floor bypass and false-positive test; final repair captured RED.
- Focused Domain paid-policy tests passed 18/18, full Domain passed 767/767, Release build exited 0 errors with 758 existing warnings, `git diff --check` was clean, and the final independent verifier confirmed the result.

### Next Action
1. Start Task 16.2: actor-bound organizer payment-provider connection and historical snapshots.
2. Keep Phase 16 open and leave Tasks 16.2-16.5 plus Phase 16 verification unchecked until their implementation evidence exists.

## Handoff - 2026-08-13 Europe/Brussels: Phase 15 Complete

### Current State
- Phase 15 tasks 15.1-15.4 are complete. ADR-022 accepts the `OrganizerDirect` paid-event model: the event organizer actor owns the connected account, Stripe SDK access stays behind Infrastructure-only capability adapters, commercial snapshots pin recipient/currency/policy evidence, and provider I/O is never performed inside database transactions.
- ADR-023 accepts the admission model: `AdmissionTicket` owns only an opaque rotatable credential, a keyed digest is the authority for lookup, display IDs/email/PII never authorize, and online-first check-in/undo is append-only and entitlement-targeted.
- ADR-024 accepts the event-platform boundary: Listmonk/Qonto/accounting/tax/legal-invoice concerns remain external specialist domains, waitlists/add-ons stay event-bound, and `ProtectedDelayedPayout` remains approval-gated by Stripe, legal, Islamic-finance, and operator evidence before Phase 24.
- Canonical agent paths were repaired to `.agents/contract` and `.agents/rules`; `.claude` paths are historical compatibility references only. Focused `PrivacyErasureIntentGovernanceTests` passed 7/7. The concurrently deleted `AgentContextGovernanceTests.cs` was not restored.
- Phase 16 is in progress. Task 16.1 is complete; no runtime code, package pin, migration, provider configuration, payment/admission implementation, or approval-gated payout work has started. Task 16.2 is next.

### Validation
- Final Release build rerun exited 0 with 0 warnings and 0 errors.
- Focused governance passed 7/7; `.agents/contract/intents.yaml` parsed and the intent validation names ADR-022, ADR-023, and ADR-024.
- Full `Event.Architecture.Tests` was executed and is not globally green: 370 total, 365 succeeded, 1 skipped, 4 failed. The four unrelated dirty-worktree failures are `BlazorProductionBackendContracts_ShouldComeFromGeneratedApiClient`, `DTOs_ShouldEndWith_Dto`, `Runtime tenant-filter bypasses must use approved reason constants`, and `InventoryCoversCurrentEfAndDesignatedProviderSurfaces`.
- Phase 15-owned verification is green under the selected gate. `git diff --check` is clean for the three workstream files, and YAML parse/intent validation passed. The dependency-policy failures found during Phase 15 were remediated on 2026-08-14 by removing `FluentAssertions`; the policy now passes, with the approved `Microsoft.Data.SqlClient.SNI.runtime 6.0.2` exception still visible.

### Research And Tool Evidence
- Tavily MCP was unavailable; no Tavily research is claimed. Context7 and official documentation evidence were used instead, with Context7 IDs `/stripe/stripe-dotnet`, `/websites/stripe`, and `/mdn/content`.
- Official NuGet/Stripe evidence plus an isolated metadata probe established `Stripe.net` 52.3.0, Apache-2.0, `net10.0` compatibility, assembly `52.3.0.0`, and API `2026-07-29.dahlia`.
- The transitive graph is `Newtonsoft.Json` 13.0.3, `System.Configuration.ConfigurationManager` 9.0.0, `System.Diagnostics.EventLog` 9.0.0, and `System.Security.Cryptography.ProtectedData` 9.0.0.

### Next Action
1. Start Phase 16.2 only: actor-bound organizer payment-provider connection and historical snapshots.
2. Keep remaining Phase 16 checkboxes unchecked until implementation evidence exists, and do not start runtime/package/migration/provider work before its task scope.

## Handoff — 2026-08-13 Europe/Brussels: Event-Platform Boundary Re-Baseline Planned

### Current State
- Phase 14 remains complete. The approved scope now extends this workstream with Phases 15–25 for paid-event governance, Stripe Connect `OrganizerDirect`, promotions, payments, refunds, admission tickets and QR credentials, check-in, ticket transfer, buyer self-service, waitlist offers, event-bound add-ons, a conditional protected-payout profile, and production hardening.
- Runtime implementation is intentionally **not started** by this planning update. The current contribution contract forbids payment-provider and admission-credential implementation before their ADRs are accepted, so Phase 15 is the mandatory decision/contract gate.
- Stripe support is now explicitly planned around Stripe's official `Stripe.net` NuGet SDK, isolated to Infrastructure behind small Application-owned capability ports. “Full Stripe support” means the complete approved Event payment surface: Connect onboarding/readiness, direct-charge Checkout, signed Connect webhooks, payment/refund/dispute reconciliation, and the separately approval-gated payout profile. It does not mean exposing every Stripe product or adding Billing, Tax, Invoicing, Terminal, Issuing, or Treasury scope.
- The authoritative payment design input is [`islamic-value-sensitive-design/i-vsd-paid-event-payments-consultation.md`](../../../islamic-value-sensitive-design/i-vsd-paid-event-payments-consultation.md). `deferred-design-records.md` remains source-free design inventory and is superseded only when the new ADRs are accepted.
- ISLAMU Event remains an event platform, not an email-marketing, accounting, tax, or invoicing product. The removed tax/invoice phase and future Listmonk/Qonto integration direction are preserved in [`dev/report/event-platform-boundary-and-external-business-integrations.md`](../../report/event-platform-boundary-and-external-business-integrations.md), outside this active implementation scope.

### Research And Tool Evidence
- Tavily MCP was unavailable on 2026-08-13; no Tavily research is claimed. Context7 and official documentation evidence were used instead, with Context7 IDs `/stripe/stripe-dotnet`, `/websites/stripe`, and `/mdn/content`.
- Official NuGet/Stripe evidence plus an isolated metadata probe identified `Stripe.net` **52.3.0** as the current stable release (Apache-2.0; compatible with `net10.0`; assembly `52.3.0.0`), the instance-based `StripeClient` as the recommended API, `RequestOptions.StripeAccount`/`IdempotencyKey` as the per-request Connect/idempotency controls, built-in bounded network retries, and `2026-07-29.dahlia` as the pinned stable API line. The transitive graph is `Newtonsoft.Json` 13.0.3, `System.Configuration.ConfigurationManager` 9.0.0, `System.Diagnostics.EventLog` 9.0.0, and `System.Security.Cryptography.ProtectedData` 9.0.0.
- Official Qonto documentation confirmed customer-facing OAuth, least-privilege invoice/client scopes, client-invoice finalization and linked credit-note operations, signed retrying webhooks, bounded provider idempotency, rate limits, and Qonto's non-global account footprint. These facts inform the deferred report only; no Qonto runtime phase is added here.
- That dependency-policy run exposed metadata failures for `FluentAssertions 8.10.0` and `Microsoft.Data.SqlClient.SNI.runtime 6.0.2`. The assertion dependency was removed on 2026-08-14; the policy now passes and retains the approved SNI runtime exception in its output.
- The browser `BarcodeDetector` API is experimental and non-Baseline. It may be feature-detected as an optimization, but the QR phase requires a clean-room, outbound-license-compatible encoder/decoder decision plus HID and manual-entry fallbacks.

### Next Action
1. Phase 15 is complete; start Phase 16.1 next.
2. Preserve all Phase 14 evidence and unrelated worktree edits. Do not reinterpret planning checkboxes as implementation progress.

## Handoff — 2026-08-13 Europe/Brussels: Phase 14 Complete

### Current State
- Tasks 14.1–14.8 are complete. Guest orders link only through an explicit authenticated claim with verified normalized-email equality and capability scope; templates pin immutable published provenance and instantiate independent drafts; provider switches affect future launches while retained attempts can be explicitly superseded; analytics expose minimum-cell aggregates only for approved non-sensitive fields; company CSV assignment is whole-batch, serializable, idempotent, and amendment-audited; and CSV, Google Sheets, and webhook sinks run after commit with approved-field filtering.
- Final review found and repaired two Phase 14 defects before closeout: attempt supersession no longer requires the replacement to reuse the old channel/form, and duplicate CSV `(registrationOrderLineId, ordinal)` keys are rejected during parsing before participant IDs or PII are created.
- Task 14.7 remains HAL-authoritative and accessibility-announced. Task 14.8 remains design-only; no payment, admission, check-in, promotion, tax, or invoicing runtime scope was introduced.

### Validation
- Release build passes with 0 errors and 5,644 existing warnings. The full Blazor Client gate passes 2,340 with one governed skip.
- Focused receipts pass: Domain account/template 11; guest-claim Application 23; template Application 4; analytics Application 2; CSV/amendment Application 8; provider management 37; restart normalization 6; sink worker 7; sink adapters 3; API/HAL 16; Studio surfaces 10; persistence analytics 2, templates 3, and supersession model 1; architecture 15.
- EF reports no pending model changes for PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL. Regenerated `AddRegistrationAmendments` IDs are PostgreSQL `20260812222235`, SQLite `20260812222358`, SQL Server `20260812222356`, MariaDB `20260812222351`, and MySQL `20260812222353`; all were produced with `dotnet ef` and no generated artifact was hand-edited.
- Changed C# diagnostics are clean and `git diff --check` passes. Final Oracle recheck returned `APPROVE`. Per the Phase 14 testing strategy, no browser, Aspire, Docker, or live-service verification was run.

### Next Action
1. Treat Phase 14 as complete; future commerce/admission work starts from `deferred-design-records.md` under a separate approved workstream.
2. Preserve the shared Phase 13, address-geocoding, and agent-governance changes; do not reset or attribute them to Phase 14.

## Handoff — 2026-08-12 Europe/Brussels: Phase 13 Implementation Complete / API Gate Next

### Current State
- Tasks 13.1–13.4 are implemented: typed consent subjects with append-only history; independent participant consent and child-marketing rejection; purpose/event-provenance-filtered audited export; immutable retention deadlines and bounded cleanup; and permission-bound Studio `export-attendees` inside the Attendees page.
- The shipped relation name is `export-attendees`. It describes the UI placement; the underlying API contract remains the consent-filtered `ExportSharedContacts` action and audited organization export route.

### Validation
- Release build is clean. Focused Domain, Application, API/HAL, Blazor, Architecture, PostgreSQL retention, five-provider EF-model, OpenAPI, and NSwag checks are green as recorded in the task/context ledgers.
- The selected broad API gate executed 2,211 tests: 2,197 passed, 1 skipped, and 13 unrelated TickerQ schema, authorization/startup guardrail, and participation-migration fixtures failed. No Phase 13-owned test failed, but the phase checkbox remains open because the mandated project is not globally green.
- Live Aspire/browser QA is blocked by persisted PostgreSQL `28P01` and RabbitMQ `ACCESS_REFUSED` credential drift. AppHost was stopped and named volumes were not reset.
- Direct final review repaired exact-tenant recipient approval, actor/organization binding, organization-read event provenance, transactional cleanup, and privacy-erasure consent-history ordering. Final focused reruns pass Domain 6, Application 10, API/HAL 1, Blazor 2, Architecture 26, and PostgreSQL retention/erasure/provenance 10. The OpenAI Oracle request failed upstream and its non-OpenAI fallback timed out without a result; graph-backed and direct review report no unresolved owned finding.

### Next Action
1. Retain the exact broad-suite and environment caveats without weakening the implemented contracts.
2. Clear shared API fixture debt and persisted local-infrastructure credential drift before claiming a globally green gate or browser proof; otherwise Phase 14 remains optional.

---

## Handoff — 2026-08-11 Europe/Brussels: Phase 12 Complete / Phase 13 Next

### Current State
- Phase 12 Google Forms is complete. The implementation is pinned to `GOOGLE_FORMS|GOOGLE_WORKSPACE|v1|ISLAMU_EVENT_GOOGLE_FORMS_PUBSUB_V1|2026-08-11` and covers OAuth credential binding, scoped provider metadata, public origin pinning to `https://docs.google.com`, descriptor-aware no-webhook-secret connection validation, managed create/batchUpdate/publish verification, managed preflight provision/subscription execution, OIDC-authenticated Pub/Sub notify-only intake, seven-day watch create/renew, immediate initial sweep, six-hour missed-notification recovery sweeps, opaque continuation cursors, identifiers-only durable queueing, independent failure counters/backoff, server-derived binding capabilities, and explicit `system.registration_attempt_token -> entry.<digits>` mapping.
- The adapter does not advertise submission sink, auto-finalize, Drive/file upload, or live Google tenant proof. `attemptId|attemptToken` is capability/correlation-only; `AccountRequired` parks.

### Validation
- Final green counts: build 0 warnings/errors; Domain 747; Application 3,568; Architecture 369 plus 1 governed skip; Secrets 224; Infrastructure 1,224; Blazor Integration 425; Blazor Client 2,330 plus 1 governed skip; focused Persistence provider 21; API callback 8; contract invariants 34; snapshots 4; parity 11; inventory 1; Google adapter 37; lifecycle 5; correlation 6. Current generated application migrations are PostgreSQL `20260811202610`, SQLite `20260811202805`, SQL Server `20260811202946`, MariaDB `20260811203009`, and MySQL `20260811203034`; EF has-pending checks pass for all five.
- Broad Persistence reached 811/988 with 174 known cascade failures and 3 skips. Broad API reached 2,192/2,210 before two Phase 12 contract fixes; focused owned failures are green after repair, but no full API rerun is claimed. Browser verification was blocked by persisted PostgreSQL `28P01` and RabbitMQ `ACCESS_REFUSED`; resources were stopped safely.
- Final implementation reviewers for requirements, security, and quality all returned `APPROVE`; see [Phase 12 Final Review Receipts](../../../.omo/start-work/artifacts/phase12/final-review.md).

### Next Action
1. Start Phase 13 consent, attendee-data surfaces, and audited exports.
2. Preserve provider privacy boundaries: identifiers-only sweeps, no Drive metadata, no shared secret for Google Pub/Sub, no caller-supplied Google capabilities, and no live-Google claims without real tenant evidence.

---

## Handoff — 2026-08-01 Europe/Brussels: Task 7.5 Confirmed / Tasks 7.6–7.7 Current

### Current State
- Task 7.5, the authoring Application/API/Cerbos slice, is checked; the implementation-task ledger is **40/88**. Phase 7 remains in progress, with Tasks 7.6 and 7.7 now current.
- The confirmed design includes the complete workflow/form/version graph, normalized consent legal metadata in the canonical artifact bundle and SHA-256, bounded preflight at `POST /api/events/{eventId}/registration-forms/{formId}/versions/{versionId}/preflight`, real PostgreSQL TestServer coverage, persisted-Event HAL/request authorization with caller authority attributes stripped and rebuilt, generated migration `20260801144215_init`, and deterministic OpenAPI/NSwag/inventory outputs.
- Historical `needs-fix` evidence and the authoritative repair are both retained. The repair verifier is `confirmed`/`APPROVE` at `0.99`; no Task 7.6 or 7.7 product work is claimed.

### Validation
- Repair receipts cover consent hash sensitivity, bounded workflow graph reads, exact preflight route, real PostgreSQL TestServer publication/concurrency/immutability, persisted-Event HAL trust, fallback/Cerbos parity, generated contract determinism, full Release build, EF no-pending-model, and cleanup.
- Exact verifier and executor artifacts: `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.5-repair-adversarial-verify.md`, `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.5-repair-done-claim.md`, `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.5-adversarial-verify.md`, and `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.5-done-claim.md`.

### Next Action
1. Start Task 7.6 Studio form builder and keep its mutation/navigation affordances HAL-driven.
2. Start Task 7.7 requirement attachment and walk-in standalone questionnaires only within its declared scope.
3. Preserve immutable published versions, Application-owned artifact generation, persisted-Event authority enrichment, generated-artifact-only migrations, and the explicit exclusion of Task 7.8 localization changes.

## Handoff — 2026-08-01 Europe/Brussels: Task 7.4 Confirmed / Task 7.5 Current

### Current State
- Task 7.4, deterministic JSON Schema 2020-12 artifacts, is checked; the implementation-task ledger is **39/88**. Phase 7 remains in progress and Task 7.5, authoring Application/API/Cerbos, is current.
- The durable artifact contract is exactly four deterministic artifacts per immutable form version: data schema, UI layout, closed condition/rule logic, and the currently empty provider-mapping shape reserved for Task 9.3. The data schema identifies JSON Schema 2020-12.
- `FormSchemaArtifactGenerator` uses non-indented `System.Text.Json` canonical UTF-8 bytes and lowercase SHA-256 over the complete bundle. `FormSchemaArtifactPublicationService` owns generate-and-publish from the live relational aggregate; the Domain exposes only an internal atomic pinning seam, so callers cannot provide artifacts or hashes.
- Persistence stores all four artifact values plus the 64-character hash and enforces draft-all-null versus published/retired-all-non-null columns. EF generated `20260801132046_init`; no migration or snapshot was hand-edited.
- The initial adversarial review found the public caller-authored `Publish(string ...)` authority defect. The repair removed that seam, added the Application facade/internal Domain pinning boundary, and the independent repair verifier confirmed the result at `0.99`.
- Task 7.3 uses nine syntax tokens — `equals`, `notEquals`, `in`, `contains`, `exists`, `compare`, `all`, `any`, and `not` — to implement ten semantic operations because numeric and date comparison are distinct typed cases of `compare`. No tenth token is introduced.
- Exact receipts: generated-property/focused Domain tests `11/11`, manual harness `15/15`, PostgreSQL `1/1`, scoped Release builds `0` errors, EF no pending model changes for generated `20260801123546_init`, and final adversarial QA `confirmed`/`APPROVE` at `0.99`.
- Evidence: `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.3-done-claim.md`, `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.3-adversarial-verify-final.md`, and `.omo/evidence/task-7.3-final-stop-hook-verification.md`.
- The confirmed decision is one required normalized BCP-47 `LanguageTag` per immutable `RegistrationFormVersion`; labels, descriptions, option text, and validation messages stay whole-version content. There is no translation table, per-field fallback, mixed-language merge, or `MULTILINGUAL` capability claim.
- Form content language remains separate from UI culture: RTL continues through `CultureRegistry`/`LanguageProvider`/`MudRTLProvider` and logical CSS. A language mismatch selects a matching published version only when the workflow exposes one; otherwise the workflow default is used.

### Validation
- Exact Task 7.4 receipts: Application generator/publication `32/32` twice, Domain atomicity `8/8`, Task 7.3 rule compatibility `11/11`, Persistence mapping `3/3`, Persistence rule compatibility `2/2`, rootless PostgreSQL `1/1`, scoped Domain/Application/Persistence/API Release builds `0` errors, EF no pending model changes, and facade-only manual harness exit `0` with all four artifact equality predicates true.
- Evidence: `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.4-done-claim.md`, `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.4-adversarial-verify.md`, `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.4-publish-authority-repair-done-claim.md`, `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.4-publish-authority-repair-adversarial-verify.md`, and `.omo/evidence/task-7.4-publish-authority-repair-verify/`.
- The initial `needs-fix` is retained as historical evidence; the confirmed repair is the authority for completion. This closeout changes documentation, ledger, and the durable Domain artifact description only.

### Next Action
1. Start Task 7.5 authoring Application/API/Cerbos against the verified pinned artifact and immutable form-version boundaries.
2. Preserve Application-owned generation, internal atomic Domain pinning, whole-version fallback, UI-culture-driven RTL, and generated-artifact-only migration workflow.

## Handoff — 2026-08-01 Europe/Brussels: Task 7.2 Complete

### Current State
- Task 7.2, the immutable form/version/section/field/option model, is checked; the implementation-task ledger is **36/88**. Phase 7 remains in progress and Task 7.3, the bounded condition language, is next.
- The five Task 7.2 entities are `RegistrationForm`, `RegistrationFormVersion`, `RegistrationFormSection`, `RegistrationFormField`, and `RegistrationFormFieldOption`; `FormVersionRules` is pure, while `RegistrationFormRule` remains owned by Task 7.3. Published versions freeze the graph, retire explicitly, and deep-clone to fresh IDs while preserving provenance, normalized `LanguageTag`, governance, and `Namespace/Key` identity. `LanguageTag` is required per version; translation tables and `MULTILINGUAL` remain deferred to Task 7.8.
- The independent needs-fix finding was repaired at the aggregate seam: `RegistrationFormVersion.AddField` now rejects a normalized, active `Namespace/Key` duplicate across different sections, with the EF version-wide unique index retained as defense in depth.

### Validation
- Exact receipts: Domain `6/6` (`task-7.2-repair-gate-domain.trx`), Persistence metadata `3/3` (`task-7.2-repair-gate-persistence.trx`), naming `11/11`, manual harness `8/8` runtime predicates, rootless Podman/PostgreSQL `1/1` (`task-7.2-postgresql.trx`), scoped Domain/Persistence/API Release builds `0` errors, and EF no-pending-model result for generated `20260801114559_init`.
- Independent repair review is `confirmed`/`APPROVE`: `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.2-repair-adversarial-verify.md`; original `needs-fix`: `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.2-adversarial-verify.md`; DoneClaim: `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.2-done-claim.md`.
- Cleanup removed Task 7.2 PostgreSQL/Ryuk resources; only pre-existing containers remain. The broader Clean Architecture `14/15`, generated-init `5/6`, and repository-wide whitespace/build findings remain unrelated baselines; Phase 7 and full clean architecture are not claimed complete.

### Next Action
1. Start Task 7.3 bounded condition language against the verified immutable form-version aggregate.
2. Preserve generated-artifact-only migration workflow and the required per-version `LanguageTag` boundary.

## Handoff — 2026-08-01 Europe/Brussels: Task 7.1 Complete

### Current State
- Phase 6 and its owned verification debt are complete. Task 7.1, `Workflow + requirement + channel skeleton`, is checked; Phase 7 remains in progress because Tasks 7.2–7.8 and the Phase 7 verification boxes are still open.
- Task 7.1 ships `RegistrationWorkflow`, `RegistrationRequirement`, and `RegistrationChannel`; four normalized lookup row/enum families; pure ALL/ANY evaluation; typed applicability; explicit Native/provider-bound channels; positive owner-scoped ordinals; and duplicate guards. `SkippedByRegistrant` is an evaluation outcome only; Task 8.5 owns durable subject fulfillment/skip persistence.
- Fresh evidence is green: Domain 11/11, characterization 1/1, Domain architecture 6/6, naming 11/11, and the six-predicate harness; Persistence 4/4; rootless Podman-backed PostgreSQL 1/1; scoped Release builds 0 errors; EF reports no pending model changes for generated `20260801000023_init`; and all seven protected migration hashes are unchanged.
- Independent code review is PASS with no findings and adversarial QA is `confirmed`. Cleanup leaves only the pre-existing `happy_curie` container. The full solution baseline is not green: two unrelated Blazor test compile errors assign `int`/`int?` values to `GuestRecoveryPolicyEnum?`; no full-build success is claimed.

### Next Action
1. Start Task 7.2: form/version/section/field/option model with immutability.
2. Keep Phase 7 in progress until Tasks 7.2–7.8 and the two Phase 7 verification commands are complete; retain the unrelated full-build baseline blocker.

### Blockers
- No product decision is blocking. The Domain/Persistence naming is settled: keep `Purpose`, `RegistrationProviderBindingId`, and explicit positive `Ordinal`; do not introduce shadow properties or compatibility overloads.
- Full-solution verification remains blocked by two unrelated Blazor test compile errors assigning `int`/`int?` to `GuestRecoveryPolicyEnum?`; Task 7.1 scoped builds and tests are independently green.
- The shared worktree remains dirty. Preserve unrelated edits and never reset or revert them.

### Modified Files
- Task 7.1 Domain source, four lookup row/enum families, Persistence configurations/DbSets/seeding, generated `20260801000023_init` artifacts, DBML, and focused tests are complete.
- Reviewable harness, stop-hook, code-review, QA, and final evidence are under `.omo/start-work/artifacts/registration-data-collection-phase7/` and `.omo/evidence/`.

### Validation
- Domain receipts: Task 7.1 selector 11/11; unchanged participant characterization 1/1; Domain Clean Architecture 6/6; naming 11/11; harness exit 0 with `required_complete`, `required_skip_rejected`, `optional_skip_recorded`, `blocking_effect_satisfied`, `ordering_enforced`, and `tenant_isolated` all true.
- Persistence receipts: focused 4/4; rootless Podman/PostgreSQL 1/1; scoped Release builds 0 errors; EF `has-pending-model-changes` reports no changes; generated `20260801000023_init`; seven protected hashes unchanged; container cleanup leaves only `happy_curie`.
- Evidence: `.omo/evidence/task-7.1-final-verification.md`, `.omo/evidence/task-7.1-persistence-stop-hook-verification.md`, `.omo/evidence/task-7.1-code-review.md`, and `.omo/start-work/artifacts/registration-data-collection-phase7/task-7.1-session-handoff-verify.md`.

### Documentation Impact
- Task 7.1 closeout synchronizes the plan, context, task ledger, and `docs/DOMAIN.md`; generated migration/schema artifacts remain generated outputs and are not hand-edited.

### Risks And Notes For The Next Agent
- Never write registrant-specific skip state onto the shared authoring requirement. Task 7.1 returns a pure outcome; Task 8.5 persists fulfillment/skip evidence per registration subject.
- Never hide ordering in EF shadow state. Positive requirement/channel ordinals and duplicate guards are Domain invariants.
- Keep requirement evaluation and provider semantics in C#/EF. No triggers, stored procedures, functions, provider branches, raw business SQL, or PostgreSQL-only Task 7.1 mappings.
- Migrations and snapshots are generated artifacts only. Fix entity/configuration/seeder source, then delete/regenerate the unapplied development migration through EF CLI.
- PostgreSQL is one runtime proof, not a claim for MySQL, MariaDB, or SQL Server. Do not treat Task 7.1 completion as Phase 7 completion.

## 0. Planning Metadata

- **Original request:** Create a full implementation plan in `dev/active/registration-data-collection/` from the combined consultation document `registration-data-collection-consultation.md` (Report No. 1: registration data collection + forms provider architecture; Report No. 2: participation modes, community-reported listings, guest registration, ticket types, group bookings). Backward compatibility explicitly waived — the platform is in full development mode and the model should be replaced, not patched.
- **Re-baseline request (2026-07-20):** Integrate the decision/design/data-model findings from `hi-events-report.md` (Hi.Events ticketing research — behavior lessons only, not its tech stack or code), and expand ticket pricing beyond Hi.Events: free, paid, donation, pay-what-you-can with optional minimum (Gumroad-style 0-allowed input), Leanpub-style sliding scale (minimum + suggested price, dual linked "You pay" / "Organizer earns" sliders with exact platform-share transparency), plus a LaunchGood-style platform-contribution ("tip the platform") checkout dropdown — default 0, quick 5/10/15/20% options showing percentage + computed amount, DB-stored messaging, enableable by **instance administrators only** (never tenant admins).
- **Licensing correction (2026-07-21):** ISLAMU Event operates under a CLA that enables dual-licensing (offering the software under a non-AGPLv3 license to recipients who cannot use AGPLv3). Therefore **zero code may be copied from the Hi.Events repository** — copying AGPLv3-licensed third-party code would contaminate the codebase and destroy the dual-licensing capability. Hi.Events remains a *behavior, design, and data-model* reference only; the report's §10 code-reuse permission is explicitly overridden by this workstream (see §4.13, D19).
- **Studio integration re-baseline (2026-07-26):** Treat the implemented workspace shell from `dev/active/dynamic-event-management-ui/` as current architecture. Organizer ticketing, orders, attendees, registration forms, and provider operations extend the existing Studio workspace and its single contextual sidebar; they do not create a parallel `/events/manage` navigation system. Public and guest checkout remains outside Studio.
- **Commerce/admission re-baseline request (2026-08-13):** Activate the deferred payment and admission inventory inside this workstream. Add ADR-gated implementation phases for Stripe Connect `OrganizerDirect`, actor-bound merchant onboarding, effective currency policy, provider-neutral payment/refund attempts, local promotion codes, cancellation/refund/dispute reconciliation, `AdmissionTicket`/QR/check-in, transfers, anti-enumeration recovery/self-service, waitlists, event-bound add-ons, a separately gated `ProtectedDelayedPayout` profile, and production hardening. The payment phases must cite `islamic-value-sensitive-design/i-vsd-paid-event-payments-consultation.md` as their product-risk authority.
- **Product-boundary correction (2026-08-13):** ISLAMU Event owns event operations only. Email marketing remains delegated to Listmonk; bookkeeping, tax determination, legal invoice/credit-note issuance, and accounting remain delegated to Qonto or other external systems. The former tax/invoice phase is removed from this active plan and preserved with the future integration design in `dev/report/event-platform-boundary-and-external-business-integrations.md`.
- **Task directory:** `dev/active/registration-data-collection/`
- **Planning status:** Phases 0–15 retain their recorded implementation state, Phase 15 is complete, and Phase 16 is in progress with Tasks 16.1 and 16.2 independently confirmed. On 2026-08-13 the user approved planning expansion through Phase 25; 47 implementation tasks remain unchecked and Phase 16.3 is next. This ledger closeout changes no runtime source, package, migration, or provider configuration.
- **Matched intent:** `registration-data-collection`, the dedicated cross-cutting intent created in Phase 0. Its related granular intents remain `add-write-endpoint`, `add-get-endpoint`, `add-hal-link`, `add-cqrs-handler`, `add-ef-migration`, `update-repository-query`, `blazor-component-affordance`, `cerbos-policy-change`, and `openapi-contract-change`.
- **Relevant skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `outbox-pattern`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, `error-tracking`.
- **Relevant rules:** `.agents/rules/domain.md`, `application-layer.md`, `efcore-persistence.md`, `efcore-migrations.md`, `api-controllers.md`, `api-hateoas.md`, `blazor-server.md`, `blazor-client.md`, `tests.md`, `ip-clean-room.md`.
- **Primary layers touched:** Domain, Application, Persistence, Infrastructure, API, Blazor (BFF + WASM), Cerbos policies, Docs, DevOps (compose profiles for Formbricks, later phases).
- **Complexity:** **XL** — 26 phases, 141 implementation tasks, three form-provider channels, one payment provider, multiple security-sensitive capability surfaces, provider reconciliation, commercial and admission state machines, and Studio/public self-service integration. The expansion is deliberately phased so the default safe path (`OrganizerDirect`, online admission validation, local promotions) lands before optional event breadth and the conditional protected-payout profile.

---

## Handoff — 2026-07-31 Europe/Brussels: Phase 6

### Current State
- Phase 5 is complete under the shared-baseline blocker policy. Tasks 6.1 through 6.5 are independently verified, bringing the ledger to 37/88 implementation tasks; Phase 6 implementation is complete, but its verification/privacy audit and the broader workstream remain open.
- The registration-order lifecycle contract is frozen at `RegistrationOrderLifecycleResponseDto` and `GuestRegistrationOrderLifecycleResponseDto`. Focused Application, API, Blazor, contract-generation, Release-build, and EF model checks are green.
- Task 6.2 persists those entities and lookups, requires order-qualified participant lineage on admissions, enforces active participant/session and order-line/ordinal uniqueness, and adds a PostgreSQL row-locking quantity trigger. Free finalization now resolves assigned participants or retry-stable PII-free unnamed placeholders according to all five collection modes before transactional effects.
- Task 6.3 adds add/update/assign/bulk/defer CQRS handlers, atomic participant/assignment amendments, confirmed-order admission reassignment/materialization, and purchaser-actor booking-party limit rechecks before finalization effects. The preserved non-test family harness confirms five participants, five concrete assignments, and five unique Confirmed admissions without emitting PII.
- Task 6.4 exposes those handlers through event/order-bound guest-capability and authenticated participant/assignment routes, keeps reads private/no-store and writes idempotent/rate-limited, and emits organizer navigation plus mutation affordances exclusively through authorization-gated HAL relations. A preserved WebApplicationFactory harness confirms valid guest access, generic wrong-scope/malformed 404 behavior, exact manageable relations, and no capability disclosure.
- Task 6.5 consumes the generated participant client in authenticated/guest recovery, renders per-ticket optional/required/deferred fields from pinned catalog metadata, keeps copied buyer details editable, and adds actor/event Studio attendee routes. Actor, event, per-order row, and order-operation visibility fail closed on their exact HAL relations; direct reruns and the independent adversarial audit are confirmed.
- Full Architecture is not broadly green: 327/337 passed, 9 unrelated failures remain, and 1 test is skipped. Docker-backed PostgreSQL assertions remain fixture-blocked; database apply/revert, browser, Aspire, and visual-runtime results remain unclaimed.

### Next Action
1. Read `registration-data-collection-context.md` and `registration-data-collection-tasks.md`, then read only this Phase 6 section plus the required rules and skills.
2. Run the Phase 6 Release/Application verification commands, then complete the participant-PII inventory/privacy audit without weakening the required admission-participant linkage.
3. Keep owned Phase 6 evidence separate from unrelated shared Architecture/OpenAPI debt and do not infer Docker/browser runtime success.

### Blockers
- The shared Architecture baseline remains non-green for unrelated failures, so Phase 6 verification must keep owned evidence separate from that baseline.
- Database rollout evidence remains absent. Three PostgreSQL scenarios were discovered but fixture-blocked by unavailable Docker before assertions; the corrected four-scenario class compiles, but runtime success must not be inferred.
- The worktree is shared and dirty. Do not reset, revert, delete, or overwrite unrelated files.

### Modified Files
- Task 6.5 added the pinned ticket-line read metadata, generated-client participant adapters, registration participant editor, actor/event Studio attendee pages/navigation, isolated token CSS, focused Application/bUnit/service tests, and preserved render-tree evidence.
- This state-only closeout changes only `.omo/start-work/ledger.jsonl`, the current Phase 6 handoffs in `registration-data-collection-plan.md` and `registration-data-collection-context.md`, and its `.omo/evidence/` report; task checkboxes are untouched.

### Validation
- Fresh direct Task 6.5 reruns passed the pinned-catalog Application selector, participant/Studio/service bUnit selectors, generated-client drift check, and zero-error Blazor Client build.
- The preserved render-tree harness produced five authorized/fail-closed states at three width contracts with every predicate true. Independent accessibility and design reviews passed; the final adversarial audit returned `confirmed` at 0.99 after adding exact negative order-operation and production per-order row-filter characterization tests.
- Docker-backed PostgreSQL runtime remains pending from Task 6.2; no database runtime pass or apply/revert is inferred.
- Handoff-doc verification: `git diff --check -- dev/active/registration-data-collection/registration-data-collection-plan.md dev/active/registration-data-collection/registration-data-collection-context.md dev/active/registration-data-collection/registration-data-collection-tasks.md`.

### Documentation Impact
- Updated the current Phase 6 handoffs in the plan/context and appended the independently confirmed Task 6.5 evidence record. No historical handoff, task checkbox, journal, or canonical product/operator document changed.

### Risks
- Preserve buyer != participant, split participant PII, order-qualified lineage, nullable unnamed pre-assignment, and required admission participant linkage.
- Preserve Task 6.5's generated-client-only access and HAL-only checkout/Studio navigation, rows, and actions; never replace the exact relation gates with local roles, claims, status, provenance, or capability booleans.
- The Phase 6 privacy audit must inventory split participant PII accurately without exposing purchaser or attendee data in logs/evidence.

### Notes For Next Agent
- Task 6.5 evidence: `.omo/start-work/artifacts/registration-data-collection-phase6/task-6.5-done-claim.md`; direct verification: `.omo/evidence/task-6.5-direct-verification.md`; confirmed audit: `.omo/evidence/task-6.5-direct-adversarial-verify.md`; visual design: `.omo/evidence/registration-data-collection-phase6-task65-clone-fidelity.md`; accessibility: `.omo/start-work/artifacts/registration-data-collection-phase6/task-6.5-visual-accessibility-review.md`.
- Phase 6 verification and participant-PII privacy inventory are next. Do not start Phase 7 until their owned gates are adjudicated.
- Keep implementation clean-room: zero Hi.Events code, SQL, migration, snippet, or asset copy. Use only the approved behavior report and this plan.
- Load the `registration-data-collection` intent's required Domain/Application/Persistence/API/Blazor rules and listed skills for the slice being implemented.
- Preserve the shared dirty worktree. Do not reset or revert unrelated changes, and do not turn unavailable Docker/database/browser checks into completion claims.

## 1. Executive Summary

ISLAMU Event today models participation with four booleans/strings on `Event` (`IsRegistrationRequired`, `IsUserReported`, `EventUrl`, `ExternalRegistrationUrl`), a strictly user-centric registration aggregate (`EventRegistrationIntent` → `EventRegistration`, both requiring an authenticated `User`), and no way to collect organizer-defined registration data at all.

This workstream replaces that with the platform's strongest differentiator:

> **ISLAMU Event owns the registration workflow and the normalized registration record. A form provider (built-in Native, Formbricks, Google Forms, Microsoft Forms) supplies a versioned collection channel and evidence of completion. Listing an event, managing participation, collecting data, selling tickets, and receiving attendee information are separate authorities — possessing one never grants the others.**

What ships, in dependency order:

1. **Provenance & authority model** — community-reported listings, organizer claims, typed public actions, external-link security.
2. **Typed participation configuration** — information-only / walk-in / external-managed / platform-managed, advance-registration obligation, identity-access mode; HAL-authored participation CTAs (zero actions is valid).
3. **Guest transaction security** — a new `PublicTransactional` endpoint classification with capability tokens, dedicated rate limits, antiforgery, and idempotency.
4. **Ticketing & capacity** — versioned ticket catalogs with **five pricing modes** (fixed, free, donation, pay-what-you-can with optional minimum, sliding scale with minimum + suggested price and transparent "Organizer earns" display), shared capacity pools, entitlements, atomic inventory holds, and **instance-admin-only platform monetization** (fee-transparency policy + optional "tip the platform" contribution with DB-stored messaging — defaults all zero/off for self-hosted freedom).
5. **Order aggregate** — buyer/order/lines/participants/assignments replacing the user-centric intent; free-order confirmation; `AwaitingPayment` boundary for the future payment workstream.
6. **Registration Data Collection bounded context** — immutable form versions, typed one-row-per-answer storage, bounded condition language, consent evidence, requirement fulfillment, idempotent finalization.
7. **Provider framework + Formbricks (deep), Microsoft Forms, Google Forms** — capability-authoritative channels reusing the hardened incoming-webhook intake.
8. **Consent & attendee-data surfaces** — typed consent subjects, verified-recipient rule, audited exports.
9. **Paid-event commerce** — actor-bound Stripe Connect onboarding, effective currency narrowing, local promotions, direct-charge Checkout, independent payment/refund/dispute state, and cancellation reconciliation.
10. **Admission operations** — independently revocable `AdmissionTicket` credentials rendered as QR, online check-in, compensating undo, transfer/reissue rotation, anti-enumeration recovery, and account/guest self-service.
11. **Deferred breadth made explicit** — reservation-safe waitlists, event-bound add-ons kept outside admission vocabulary, an approval-only delayed-payout profile, and production/pilot hardening. Marketing, accounting, tax, and legal invoicing stay in external integrations documented separately.

**Still outside the re-baseline:** email campaign/marketing tooling; accounting, bookkeeping, tax determination, invoice/credit-note issuance, and bank reconciliation; Qonto runtime integration; a second payment provider or speculative provider factory; provider-controlled adaptive pricing/internal FX; ticket resale/marketplace settlement; offline signed admission credentials before a key-lifecycle extension to ADR-023; automatic account creation from guest data; AT Protocol federation of orders/payments/tickets; a generic tenant/instance-admin merchant recipient; and undocumented provider APIs. `ProtectedDelayedPayout` is a conditional future profile, never the default and never described as escrow.

A deep research pass over Hi.Events (`hi-events-report.md`, pinned commit `9de8863a`) confirmed this plan's architectural choices — immutable catalogs, typed answers, hashed capabilities, durable effects, explicit state machines ("No evidence from Hi.Events justifies reversing D1–D16") — and contributed a production **behavior catalog** (reservation-before-PII checkout, visible expiry + state-specific recovery screens, commercial snapshots, shared-capacity visualization, buyer-vs-participant question separation) plus a hardened list of concurrency/security acceptance criteria from its concrete defects (report §7), now embedded in Phases 4–8.

---

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---|---|
| `Event` models participation via booleans/strings | Verified: `src/Explore.Domain/Event.cs` lines 38–48, 77 (`Price`, `CurrencyCode`, `IsRegistrationRequired`, `IsUserReported`, `EventUrl`, `ExternalRegistrationUrl`) | High | Exactly the fields both reports say must be replaced |
| `Event` already has string provenance fields | Verified: `src/Explore.Domain/Event.cs` lines 95–96 (`ProvenanceSource`, `ProvenanceExternalId`) | High | Import provenance only; no authority model |
| Registration intent is user-centric | Verified: `src/Explore.Domain/EventRegistrationIntent.cs` — required `UserId`/`User`, `RegistrationScopeId`, `SelectedEventDayId`, `RegistrationPolicySnapshotId`, `ApprovalStatusId` | High | Cannot represent guests, quantities, participants |
| Per-session registration requires a User | Verified: `src/Explore.Domain/EventRegistration.cs` — required `UserId`, `EventSessionId`, nullable `EventRegistrationIntentId`, `AtprotoRecordId` | High | Keep as materialized admission row, rewire to participants |
| Registration lookups exist | Verified: `RegistrationScope.cs`, `RegistrationMode.cs` (`Open/ApprovalRequired/InviteOnly/Closed`), `EventRegistrationPolicy.cs`, `Enums/RegistrationScopeEnum.cs`, `Enums/RegistrationModeEnum.cs` | High | `RegistrationMode` maps 1:1 to the consultation's admission-decision mode |
| Pure domain policy rules exist | Verified: `src/Explore.Domain/Services/Registration/RegistrationPolicyRules.cs::IsScopeAllowed`, `ResolveInitialApprovalStatus` | High | Pattern to follow for new state machines |
| Registration CRUD feature exists | Verified: `src/Explore.Application/Features/EventRegistrations/` (Create/Update/Delete handlers, 5 query handlers); `src/Explore.API/Controllers/EventRegistrationController.cs` (7 named routes) | High | Will be replaced by order-centric features |
| HAL policy for registrations exists | Verified: `src/Explore.API/Hateoas/Policies/EventRegistrationLinkPolicy.cs`, `EventLinkPolicy.cs`; `LinkRelations.cs` has `registration`/`registrations` | High | |
| Contact-share consent is user-centric | Verified: `src/Explore.Domain/EventContactShareConsent.cs` — required `UserId`, `RecipientActorId`, snapshots (`EmailSnapshot`, `ConsentTextSnapshot`, `ConsentUiVersion`), nullable `SourceEventRegistrationIntentId` | High | Snapshot pattern is correct; subject typing needed |
| Custom-property system is the Layer 3 reference | Verified: `src/Explore.Domain/CustomPropertyDefinition.cs` (namespaced keys, typed validation metadata, exposure/governance flags), `CustomPropertyValue.cs` (one row per value, typed columns, `Ordinal`); `docs/CUSTOM_PROPERTIES.md` defines Layer 1/2/3 boundary | High | Reuse primitives/vocabulary, **not** tables |
| Hardened incoming-webhook intake exists | Verified: `src/Explore.Domain/IncomingWebhookMessage.cs` (exact bytes, payload hash, retention, fenced processing, redrive), `IncomingWebhookEffectOutbox.cs` (leases, fencing, dead-letter) | High | Provider callbacks extend this; no new mechanism |
| Endpoint classes are Public/Authenticated/Admin/PublicTransactional | Verified: `src/Explore.API/Attributes/EndpointClass.cs`; `tests/Event.Architecture.Tests/EndpointClassificationArchitectureTests.cs`; `tests/Event.Architecture.Tests/PublicTransactionalGovernanceTests.cs` | High | Phase 3 implemented |
| UnitOfWork transaction pattern exists | Verified: `src/Explore.Application/Contracts/Persistence/IUnitOfWork.cs`; `src/Explore.Persistence/EfCoreUnitOfWork.cs`; `docs/GOVERNANCE.md` command-handler review checklist | High | All multi-step writes (holds, finalization) use it |
| Idempotency middleware exists | Verified: `docs/ARCHITECTURE.md` §Idempotency — `Idempotency-Key` header, `IdempotencyRecord`, `(Key, TenantId)` replay 24h | High | Guest create/finalize will require it |
| Secrets bindings support Instance and Tenant scopes only | Verified: `src/Explore.Domain/Secrets/SecretBinding.cs`, `Enums/SecretScope.cs` (`Instance = 0, Tenant = 1`), `SecretDefinitionRegistry.cs` | High | Org-scoped provider connections deferred (D15) |
| Cerbos policies exist per resource incl. registrations | Verified: `cerbos/policies/islamuevent_event_registration.yaml`, `islamuevent_event.yaml`, `islamuevent_event_contact_share_consent.yaml`, `derived_roles.yaml` | High | New resources need new policies + parity tests |
| Three generated migration baselines exist | Verified 2026-07-26: `Migrations/20260720162943_init.cs`, `Migrations/DataProtection/20260720163113_init.cs`, and `Migrations/PrivacyErasureAuthority/20260720163243_init.cs`; `.claude/contract/intents.yaml::platform-privacy-erasure.migration_history_exception` remains the authority for the clean reset | High | Registration owns only later additive generated migrations; it never regenerates history |
| Erasure workstream remains active | Verified: `dev/active/optional-retained-erasure-authority/optional-retained-erasure-authority-context.md` | High | Its generated init lanes are now the registration schema baseline |
| Blazor registration UX exists and is client-generated-contract-only | Verified: `src/Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor`, `Pages/Events/EventListRegistrationWorkflow.cs`, `Clients/EventApiClient.g.cs`; QUICK_REFERENCE rule 23 (Blazor isolation) | High | Client work happens only against regenerated `IEventApiClient` |
| Workspace shell and Studio are implemented | Verified: `src/Explore.Blazor.Client/Services/Shell/WorkspaceRegistry.cs`, `Components/Shell/Workspaces/StudioWorkspaceNavigation.razor`, `StudioEventNavigation.razor`, `Pages/Studio/StudioEventShell.razor`, `Routes.razor` | High | Actor navigation currently exposes Overview/Events; an event route replaces it in the same sidebar with HAL-derived sections |
| Studio event navigation is relation-driven and regression-tested | Verified: `tests/Explore.Blazor.Client.Tests/Pages/Studio/StudioEventNavigationTests.cs`, `Pages/Studio/StudioPagesTests.cs`, `Components/Shell/WorkspaceNavigationHostTests.cs`, `Routing/RoutesConfigurationTests.cs` | High | Existing mapped relations cover Details, Schedule, legacy Registration, Publication, Team, and Danger zone |
| No actor-level operational HAL resource exists for cross-event Orders/Attendees navigation | Not found: inspected `UiShellContextDto` consumers, `StudioWorkspaceNavigation`, Studio routes, and Studio tests | High | Task 5.7 introduces one compact authenticated Studio context resource; no role-derived or per-link probe requests |
| None of the target entities exist | Not found: searched `rg "RegistrationWorkflow|RegistrationOrder|RegistrationAttempt|TicketType|CapacityPool|Formbricks|RegistrationParticipant|EventPublicAction|EventProvenance|GuestAccessToken|InventoryHold"` across `src/` — only unrelated hits (`EventListRegistrationWorkflow.cs` UI helper, `ManagedControlPlaneRegistration*`) | High | Everything in §3 is new |
| Provider capabilities (Formbricks/Google/Microsoft) | Source-derived: consultation §2, §14–16 with dated citations (Formbricks Standard Webhooks + headless APIs; Google Forms Pub/Sub watches with 1-week renewal and post-2026-06-30 unpublished-by-default; Microsoft Forms connector-only, org accounts) | Medium | External research tools (anysearch/context7 MCP) were unavailable in this session; re-verification tasks are embedded in Phases 10–12 |
| Rate limiting, caching, ETag, ProblemDetails conventions | Verified: `docs/QUICK_REFERENCE.md` (policies table, `NoLimiter` in Testing, chained `IExceptionHandler`, weak ETags) | High | New `public_transactional` policy must join this table |
| Metrics live in bounded-dimension registries | Verified: `src/Explore.Application/Telemetry/BusinessMetrics.cs`, `WebhookTelemetryDimensions.cs` | High | New registration metrics follow the same pattern |
| Hi.Events research report exists and was fully read | Verified: `dev/active/registration-data-collection/hi-events-report.md` (1,591 lines; Hi.Events pinned at commit `9de8863a`, `develop` 2026-07-19) | High | Research snapshot — behavior catalog + risk findings, **not** an architecture authority |
| Hi.Events validates D1–D16; its commercial breadth is a deferred-feature inventory | Source-derived: report §8 comparison table + §11.1 ("No evidence from Hi.Events justifies reversing D1–D16") and §11.3/§11.4 scope discipline | High | Adopt behavior/workflow lessons; reject its persistence, authorization, money, idempotency, and side-effect machinery |
| Hi.Events concrete defects catalogued (shared-capacity race, duplicate-completion race, cache-only webhook idempotency, public-ID-as-bearer access, per-price multiset gap, attendee-derived inventory release, plaintext tokens/PII logging) | Source-derived: report §7.1–§7.14 | High | Converted into binding acceptance criteria in Phases 4–8 (§11.2 of the report) |
| Hi.Events supports paid/free/donation pricing with per-order minimums; mutable in place | Source-derived: report §4.3 (`ProductPrice` types `PAID/FREE/DONATION/TIERED/REGISTRATION`; "published catalog state is mutable in place") | High | D17 exceeds this breadth while keeping immutable versioned catalogs |
| Hi.Events branding demand is a removable AGPLv3 further restriction | Source-derived: report §10 (FSF analysis reference) | High | Moot for code: §4.13 forbids **any** code copy from Hi.Events (CLA/dual-licensing protection); the report's §10 code-reuse permission is overridden by this workstream |
| Phase 2 typed participation source and contracts are implemented | Verified 2026-07-28: `EventParticipationConfiguration`, `EventAuthorityRules`, `ConfigureEventParticipationCommandHandler`, `EventParticipationController`, `EventLinkPolicy`, generated OpenAPI/NSwag, Studio participation editor, and focused tests | High | Tasks 2.1 through 2.5 are source/contract complete; phase and migration rollout are not complete |
| Participation management does not reuse generic event-update authority | Verified: `ConfigureEventParticipationCommand` requests `AuthorizationActions.Events.ManageRegistrations`; fallback maps it to `PermissionCodes.EventRegistrationManage`; organizer controllers are derived from `OrganizerActor`, and an explicit event-role assignment may grant `EventRegistrationManage` | High | A community reporter receives no implicit `EventOwner` or participation-management authority |
| Public participation has three distinct HAL relations | Verified: `LinkRelations.StartRegistration`, `SignInToRegister`, and `ExternalRegistration`; `EventLinkPolicy` emits them by participation mode and authentication state | High | Studio uses only `configure-participation`; attendee relations never authorize Studio |
| Public-action filtering is shared by API and federation output | Verified: `EventMappingProfile` filters `EventDto.PublicActions` through `EventAuthorityRules.IsPublicActionAllowed`; `AtprotoEventPublicationSnapshotFactory.BuildUris` applies the same rule before emitting a registration URI | High | Stale or mode-incompatible external registration actions do not leak through either output |
| Guest recovery is a string enum contract | Verified: `GuestRecoveryPolicyEnum`; `ContractInvariantsTests.OpenApiDocument_PublicEnumSchemasUseStringValues`; `InstanceOnboardingOpenApiContractTests.GeneratedClient_MustUse_GuestRecoveryPolicyEnum_Contract` | High | Exact literals are `VerifiedEmailRequired`, `UnverifiedEmailAccepted`, `EmailOptional`, `CapabilityLinkOnly`, and `NoRecovery` |
| Participation/ticketing migration artifact is committed | Verified from commit `ff30795a2 feat(persistence/ticketing): add participation schema` | High | Owns `20260728152646_AddParticipationHandlingModes.cs`, its designer, and the snapshot; contains participation, catalog/type/entitlement/pool, fee/fixed-charge, and contribution setting/option schema. Database application/runtime rollout is not evidenced |

#### 2026-08-13 Commerce And Admission Research Addendum

Clean-room scope: only public functional constraints were retained. No external implementation source, snippets, schemas, migrations, tests, assets, or expressive workflow structure entered this plan. The I-VSD consultation remains the payment-risk synthesis; the sources below revalidate implementation constraints, not architecture authority.

| Primary source | Source-free fact used | Planning consequence |
|---|---|---|
| [`i-vsd-paid-event-payments-consultation.md`](../../../islamic-value-sensitive-design/i-vsd-paid-event-payments-consultation.md) | Organizer-recipient invariant, `OrganizerDirect`, policy narrowing, explicit currency, refund floor, payout/legal/scholarly boundaries | Authority for Phases 15–19 and 24–25 |
| [`Stripe.net` 52.3.0 on NuGet](https://www.nuget.org/packages/Stripe.net/52.3.0), [tagged SDK README](https://github.com/stripe/stripe-dotnet/blob/v52.3.0/README.md), and [Apache-2.0 license](https://github.com/stripe/stripe-dotnet/blob/v52.3.0/LICENSE) | Current stable official .NET SDK; Apache-2.0; supported package targets are compatible with this repository's `net10.0`; `StripeClient` is the recommended non-global entry point | Phase 15 records the exact pin/license/API evidence; Phase 16 adds the centrally managed package only after the gate; Stripe SDK types remain inside Infrastructure |
| [Stripe SDK versioning](https://docs.stripe.com/sdks/versioning?lang=dotnet), [v52.2.0](https://github.com/stripe/stripe-dotnet/releases/tag/v52.2.0), and [v52.3.0](https://github.com/stripe/stripe-dotnet/releases/tag/v52.3.0) | SDK releases pin an API version; 52.2.0 moved to `2026-07-29.dahlia`, while 52.3.0 release notes add event helpers and announce no later stable API line. The same API line is therefore a dated research inference, to be verified through `StripeConfiguration.ApiVersion` after restore | Pin SDK and webhook endpoint API versions together; reject/park mismatched webhook versions and rehearse upgrades explicitly |
| [`Stripe.net` per-request configuration](https://www.nuget.org/packages/Stripe.net/52.3.0) | `RequestOptions` carries `StripeAccount` and `IdempotencyKey`; the SDK supports a custom `HttpClient` and automatically retries selected transient failures with idempotency protection | Never use legacy global API-key/client state; reuse the repository HTTP transport, set explicit bounded retry/timeout policy, and avoid a second blind retry layer around mutations |
| [Stripe direct charges](https://docs.stripe.com/connect/direct-charges) | Charge/payment objects and balance live on the connected account; connected-account context is required | D21/D23; snapshot and use the organizer account on every provider operation |
| [Stripe connected-account capabilities](https://docs.stripe.com/connect/account-capabilities) | Requested capabilities drive verification requirements and may change/disable | Paid publication uses live readiness, not an onboarding-return flag |
| [Stripe embedded onboarding](https://docs.stripe.com/connect/embedded-onboarding) | Requirement updates are asynchronous and must be monitored | `account.updated`/reconciliation path in Phase 16 |
| [Stripe Connect webhooks](https://docs.stripe.com/connect/webhooks) and [webhook guidance](https://docs.stripe.com/webhooks) | Connected-account events are a separate scope; deliveries can duplicate and should be handled asynchronously | Signed inbox, dedupe, bounded queue, monotonic transitions |
| [Stripe webhook signature guidance](https://docs.stripe.com/webhooks/signature) and [API upgrades](https://docs.stripe.com/upgrades) | Signature verification needs the unmodified request body, signature header, and endpoint secret; endpoint payload version is independently pinned | `EventUtility.ConstructEvent` verifies exact UTF-8 body with its default recency check; endpoint API version must equal `StripeConfiguration.ApiVersion`; never disable version-mismatch safety |
| [Stripe idempotent requests](https://docs.stripe.com/api/idempotent_requests) | Mutating requests support stable idempotency keys; keys must not contain sensitive data | Persist retry identity before external calls; ambiguous outcomes reconcile |
| [Stripe currencies](https://docs.stripe.com/currencies) | Provider amounts use currency minor units and support varies by currency/country/method | Preserve `long ...Minor`, provider/account intersection, no internal FX |
| [Stripe Checkout discounts](https://docs.stripe.com/payments/checkout/discounts) | Provider promotions have provider-side restrictions checked at redemption | Keep local promotion/order truth and send final charge composition only |
| [Stripe refunds](https://docs.stripe.com/refunds) | Refund lifecycle is event-driven and Connect behavior depends on charge type | Independent `RefundAttempt`; never equate request acceptance with success |
| [Stripe .NET error handling](https://docs.stripe.com/api/errors/handling?lang=dotnet) and [request IDs](https://docs.stripe.com/api/request_ids) | `StripeException` exposes bounded error classification, HTTP status, and provider request ID | Map SDK errors to provider-neutral outcomes; retain request IDs for support/telemetry without leaking raw provider messages, secrets, or buyer data |
| [OWASP forgot-password guidance](https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html) | Recovery responses resist enumeration; tokens are random, stored securely, single-use, and expiring | Ticket lookup/resend capability contract in Phases 20/22 |
| [OWASP session management](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html) | Bearer secrets require CSPRNG entropy, scope, expiry, and safe handling | QR/scanner/transfer token rules; no PII or local-storage authority |
| [.NET cryptography model](https://learn.microsoft.com/en-us/dotnet/standard/security/cryptography-model) | `RandomNumberGenerator` and HMAC primitives are native platform capabilities | Prefer .NET cryptographic primitives over a custom token framework |
| [MDN BarcodeDetector](https://developer.mozilla.org/en-US/docs/Web/API/BarcodeDetector) | Browser detection is experimental, secure-context-only, and not universally available | Feature detection only; dependency/license gate plus HID/manual fallback |

**Documentation tooling:** Tavily MCP was unavailable, so no Tavily research is claimed. Context7 and official documentation evidence were used instead, including Context7 IDs `/stripe/stripe-dotnet`, `/websites/stripe`, and `/mdn/content`; official NuGet/Stripe evidence plus an isolated metadata probe established `Stripe.net` 52.3.0, Apache-2.0, `net10.0` compatibility, assembly `52.3.0.0`, API `2026-07-29.dahlia`, and transitives `Newtonsoft.Json` 13.0.3, `System.Configuration.ConfigurationManager` 9.0.0, `System.Diagnostics.EventLog` 9.0.0, and `System.Security.Cryptography.ProtectedData` 9.0.0. No Stripe implementation source was inspected or copied; only package metadata, licensing, public SDK usage/versioning documentation, and release notes informed this source-free contract. The earlier dependency-policy failures were remediated on 2026-08-14 by removing `FluentAssertions`; the policy now passes and retains the approved `Microsoft.Data.SqlClient.SNI.runtime 6.0.2` exception in its output.

### 2.2 Existing Implementation (by owning layer)

- **Domain.** `Event` aggregate (219 lines) carries schedule projection methods and the four participation booleans/strings. Registration is `EventRegistrationIntent` (why the user registered: Event/Day/SessionSelection scope, policy snapshot, approval status) → `EventRegistration` (per-session access row). Both require `User`. `RegistrationMode` (admission policy), `RegistrationScope`, `EventRegistrationPolicy` are `int`-keyed lookups with enum mirrors. `RegistrationPolicyRules` centralizes scope/approval rules. `EventContactShareConsent` stores per-organizer email-share consent with text/UI-version snapshots. The custom-property subsystem (definition/option/value + per-entity clones for Event, EventSession, templates) demonstrates the repo's typed-metadata-row pattern and its governance vocabulary (`ExposureLevel`, `IsExportable`, min/max/regex/URL-scheme constraints).
- **Application.** `Features/EventRegistrations/` implements user-centric CRUD (19K-line create handler includes policy/approval resolution). `Features/ContactShareConsents/`, `Features/RegistrationModes|RegistrationScopes|EventRegistrationPolicies/` expose lookups. `Features/Webhooks/` owns the durable intake/effect processing. `Authorization/` carries `AuthorizationActions`, resource descriptors, Cerbos wiring. `Telemetry/BusinessMetrics.cs` is the bounded metrics registry. `Contracts/Persistence/IUnitOfWork.cs` + `IIdempotencyRepository` provide transactionality and replay.
- **Persistence.** `ExploreDbContext` partials (`DbSets`, `QueryFilters`, `SaveChanges`) enforce tenant isolation and named `SoftDelete` filters centrally. `Configurations/Entities/*Configuration.cs` per entity. `Seed/LookupTableSeeder.cs` seeds stable lookup IDs. `EfCoreUnitOfWork` implements the transaction pattern. **Migrations: the three privacy-erasure-owned generated init lanes now exist; registration schema changes land only as later additive generated migrations.**
- **Infrastructure.** External service adapters (Listmonk, control-plane provisioning, webhook providers) with `InfrastructureServicesRegistration.cs`. No form-provider adapters exist.
- **API.** `EventRegistrationController` (7 named routes, endpoint classifications, rate-limited writes), HAL link policies per entity (detail + collection separated), `RouteNames.cs`, `EndpointClassificationTransformer` → `x-endpoint-class` OpenAPI extension. Idempotency middleware. Chained exception handlers → RFC 7807.
- **Blazor.** BFF (`Explore.Blazor`) + WASM client (`Explore.Blazor.Client`) strictly isolated from backend layers; all data via generated `EventApiClient.g.cs`; affordances gated by `_links` presence only. `EventRegistration.razor` + `EventListRegistrationWorkflow.cs` implement the current user-centric attendee flow. The implemented Studio workspace owns organizer navigation: `StudioWorkspaceNavigation` shows actor-level Overview/Events, then replaces itself with `StudioEventNavigation` on `/studio/events/{eventId}/...`; `StudioEventContextState` shares one event HAL resource between the sidebar and `StudioEventShell`.
- **Authorization.** Cerbos policy-per-resource (`cerbos/policies/islamuevent_event_registration.yaml` etc.) with derived roles and JSON schemas; parity tests in API integration suite.

### 2.3 Existing Tests And Verification Coverage

- Verified test projects (all under `tests/`): `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Architecture.Tests` (incl. `EndpointClassificationArchitectureTests`, `ApiContractArchitectureTests`), `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests` (incl. `ContractInvariantsTests`, authorization parity), `Explore.Blazor.IntegrationTests`, `Explore.Blazor.Client.Tests` (incl. `ApiClientNamingTests`, `StudioEventNavigationTests`, `StudioPagesTests`, `WorkspaceNavigationHostTests`, and `RoutesConfigurationTests`), `Explore.Secrets.UnitTests`.
- Protected behavior today: registration CRUD contract routes, endpoint classification completeness, HAL policy emission, tenant filters, lookup seeding parity, typed participation validation, explicit participation-management authorization, HAL CTA synthesis, API/federation public-action filtering, string-enum generation, and bounded outbound engagement metrics.
- Phase 2 focused evidence is green: Domain 53/53, Application 15/15, fallback/Cerbos service 119/119, participation controller 10/10, HAL 20/20, OpenAPI parity 11/11, architecture contract 10/10 with one unrelated skip, and Persistence integration 4/4 from the available Docker-backed lane. `Explore.API` and `Explore.Blazor.Client` Release builds pass.
- The selected full API phase test is not green for two externally owned `EventSessionSpeakerControllerTests`: `CollectionEditLink_UsesOnlyRelationshipIdForCanonicalPatchRoute` expects no `eventSessionId` route value, and `Update_WhenIfMatchIsMissing_ReturnsValidationProblemDetails` expects title `Event session speaker validation failed` but receives `Validation failed`. A 2026-07-28 focused rerun executed six tests, four passed and two failed. This workstream does not fix speaker behavior.
- **Gaps (all new-scope):** zero coverage for guest flows, capability tokens, ticket/capacity concurrency, form versioning, answer normalization, provider callbacks for registration, consent subjects beyond `User`, provenance authority, public-transactional abuse controls, or Studio ticket/order/attendee/form/integration sections. §23/§31 of the consultation define the target matrices; they are folded into phase tasks below.
- Known baseline note: `MEMORY.md` records 15 shared pre-existing test failures attributed to upstream webhook fallout (islamu.ngo import status). Implementation agents must snapshot the failing set at Phase 1 start and never attribute those failures to this workstream.

### 2.4 Existing Documentation And Contracts

- Canonical docs to update as behavior lands: `docs/DOMAIN.md`, `docs/API.md` + `docs/API_CHANGELOG.md`, `docs/AUTHORIZATION.md`, `docs/SECURITY-MODEL.md`, `docs/CONTACT_SHARING.md`, `docs/WEBHOOKS.md`, `docs/OUTBOX_PATTERN.md` (reference only), `docs/CUSTOM_PROPERTIES.md` (boundary note), `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md` (Formbricks profile), `docs/INTEGRATIONS.md`, `docs/ACCESSIBILITY.md` (reference), `schemas/islamu-event.md`, `schemas/openapi_islamu-event.json` (regenerated), ADRs in `docs/adr/` (next free numbers: ADR-016+; verified ADR-001…ADR-015 exist).
- Contracts: OpenAPI document + generated `EventApiClient.g.cs` (regeneration is a governed discrete step per `docs/GOVERNANCE.md` §API Contract Rules); Cerbos policies + `_schemas`; `LookupTableSeeder` stable IDs; `RouteNames`/`LinkRelations` constants.
- Research evidence: `dev/active/registration-data-collection/hi-events-report.md` — Hi.Events ticketing architecture assessment (behavior catalog, defect-derived acceptance criteria, deferred-feature inventory). Cited throughout Phases 4–8 and Task 14.8; do not edit. **Its §10 code-reuse permission is superseded**: this workstream forbids any Hi.Events code copy (§4.13 — CLA/dual-licensing protection); only behavior, design, and data-model lessons may be used.
- Workspace-shell contract: `docs/adr/ADR-019-workspace-shell-composition.md`, `docs/BLAZOR.md`, `docs/DOCK_LAYOUT.md`, and the implemented `dynamic-event-management-ui` workstream. Organizer event navigation must extend the one `shell.workspace-nav` provider; event-level navigation replaces actor-level navigation rather than adding a third sidebar.

### 2.5 Current Pain Points / Improvement Areas (evidence-tied)

1. **Ambiguous booleans.** `IsRegistrationRequired` cannot distinguish nine participation scenarios (consultation §4; `Event.cs:46`). `IsUserReported` grants no authority model (`Event.cs:47`).
2. **Authority conflation.** `Event.ActorId` simultaneously implies submitter/organizer/editor/data-recipient (`Event.cs:34–36`); community contributors would inherit organizer powers.
3. **Account-centric registration.** Required `UserId` on both registration entities blocks guests, families, companies, quantities, unnamed holders (`EventRegistrationIntent.cs:18–20`).
4. **No data collection.** There is no way to ask a registrant anything; organizers must leave the platform, losing typed/filterable/governed data — the core USP gap.
5. **Price is decoration.** `Event.Price`/`EventSession.Price` are display fields with no inventory, limits, snapshots, or currency policy.
6. **Consent subject rigidity.** `EventContactShareConsent.UserId` cannot represent purchasers, participants, or guest contacts (§22 of Report 2).
7. **Public writes are unclassified.** The `Public` endpoint class semantics say "No tenant mutation" (`docs/GOVERNANCE.md`); guest registration cannot be expressed without a new class.
8. **The original plan predates Studio.** Organizer UI tasks still name standalone `Pages/Events/Manage/**` locations and do not define routes or sidebar relations for ticketing, orders, attendees, forms, or integrations. Implementing them unchanged would fragment the shipped workspace model.

### Handoff: 2026-07-31 Europe/Brussels (Phase 5 complete under shared-baseline policy; Phase 6.1 next)
- **Current state:** Phase 5 is complete under the shared-baseline blocker policy. The registration-order lifecycle source receipt is frozen at `RegistrationOrderLifecycleResponseDto` / `GuestRegistrationOrderLifecycleResponseDto`; focused Application/API/Blazor lanes, API/OpenAPI/NSwag generation, and canonical Release builds are green. Shared Architecture remains 327/337 passed with 9 failed and 1 governed skip, and the persistence lane is 99 passed / 574 Docker-blocked.
- **Next action:** Start Phase 6.1 participant + PII + assignment domain model, then continue with Phase 6.2 persistence + `EventRegistration` participant linkage.
- **Blockers / risks:** Do not claim broader Architecture completion, Docker-backed persistence proof, database apply/revert evidence, browser evidence, or Phase 6 started status.
- **Evidence:** EF reports no pending model changes; `schemas/islamu-event.md` is the only current-model DBML parity change; no database apply/revert claim is made.
- **Notes for next contributor/agent:** Keep the resume note limited to verified evidence only and preserve the current shared-baseline attribution.

### 2.6 Unknowns After Investigation

| Unknown | What was searched/read | Resolving task |
|---|---|---|
| Whether the committed participation/ticketing migration has been applied to a database | Commit `ff30795a2` proves only the source artifact, designer, and snapshot | Require explicit database application/runtime rollout evidence before making an operational claim |
| Whether `Event.ActorId` should be renamed to `PublishedByActorId` or kept with documented semantics | `Event.cs`, usage breadth not fully enumerated | Task 1.1 (bounded investigation inside the slice) |
| Cerbos resource shape for claims/orders/exports (attribute names, derived roles) | `cerbos/policies/*.yaml`, `derived_roles.yaml` read; no target policies exist | Tasks 1.6, 5.7, 13.4 |
| File-upload malware scanning/quarantine capability | `StorageObject` exists; no scanner found (searched `rg -i "malware|clamav|quarantine" src`) | Task 8.8 (File field type gated behind investigation) |
| Form content localization strategy (per-language field labels vs single language per version) | Resolved by Task 7.8: required immutable per-version `LanguageTag`, whole-version fallback, no translation table or `MULTILINGUAL` claim | Task 7.8 (confirmed) |
| Guest antiforgery flow through the BFF (cookie + antiforgery token pairing for anonymous order sessions) | `docs/SECURITY-MODEL.md` headings; `Explore.Blazor` pipeline not traced for anonymous POSTs | Task 3.2 |
| AT Protocol implications of orders/participants (existing `AtprotoRecordId` on `EventRegistration`) | `EventRegistration.cs:46–48`; `docs/FEDERATION.md` not deep-read | Task 5.9 (decision: keep nullable, defer federation) |
| Current Formbricks/Google/Microsoft API surface as of implementation date | External tools unavailable this session; consultation citations dated 2026-07 | Tasks 10.1, 11.1, 12.1 (conformance re-verification) |
| Exact actor-level authority contract for cross-event Orders/Attendees navigation | Inspected `UiShellContextDto`, `StudioWorkspaceNavigation`, current Studio routes/tests; no operational HAL resource exists | Task 5.7 creates `StudioContextDto` + `GET /api/studio/context`; later phases add relations without changing the shell contract |

---

## 3. Proposed Future State

Target ownership (small text trees; authoritative shape from consultation §29 and Final CTO recommendations of both reports):

```text
Event
├── Provenance & organizer authority (EventProvenanceType, SubmittedByUserId, OrganizerActorId, SourcePublisherName, SourceUrl)
├── EventParticipationConfiguration (handling mode, advance-registration obligation, identity-access mode; admission via RegistrationMode)
├── EventPublicAction[] (typed, ordered, health-tracked; zero is valid; one primary CTA)
├── EventOrganizerClaim[] (auditable claim workflow)
├── EventTicketCatalogVersion[] → EventTicketType[] → TicketTypeEntitlement[]; EventCapacityPool[]
└── RegistrationWorkflow → RegistrationRequirement[] (criticality, sync mode, applicability) → RegistrationChannel[]

RegistrationOrder (buyer/guest capability, one currency, catalog+workflow+participation versions pinned)
├── RegistrationOrderPii / RegistrationParticipant[] + RegistrationParticipantPii
├── RegistrationOrderLine[] (price/name snapshots) → RegistrationTicketAssignment[]
├── RegistrationInventoryHold[] (Active/Consumed/Released/Expired/Cancelled)
├── PromotionRedemptionReservation[] → applied promotion/discount snapshots
├── PaymentAttempt[] → RefundAttempt[] / PaymentDisputeProjection[]
├── RegistrationRequirementFulfillment[]
├── RegistrationAttempt[] → RegistrationSubmission[] → RegistrationAnswer[] / RegistrationAnswerFile[] / RegistrationSubmissionIssue[]
├── EventRegistration[] (materialized per-session admission rows, participant-linked)
└── AdmissionTicket[] → TicketTransferOffer[] / AdmissionCheckInEvent[]

Provider plane: RegistrationProviderConnection → RegistrationProviderBinding (+ capabilities, field/option mappings,
schema revisions, sync mode, trust level) → RegistrationChannel; callbacks ride IncomingWebhookMessage → IncomingWebhookEffectOutbox.

Commerce plane: PaidEventPolicy → OrganizerPaymentProviderConnection → PaymentAttempt; Stripe Connect is the sole initial
adapter and every remote operation is pinned to the organizer connected account, durable idempotency, webhook evidence,
and reconciliation. Waitlist offers and event-bound add-ons remain separate lifecycle owners. Marketing and finance facts
leave Event only through consent-aware, post-commit integrations; Listmonk/Qonto own their specialist domains.
```

Behavioral invariants:

- External completion is **evidence**, never confirmation; only the ISLAMU finalization transaction (capacity + approval + dedup + validation) confirms.
- Attempts pin form version, binding, and mapping revision; "current form" is never resolved after a response arrives.
- Published form versions and ticket catalog versions are immutable; edits create new versions; in-flight work stays pinned.
- Answers are typed rows (exactly one populated value column, DB CHECK), multi-subject (`Order/Purchaser/Participant/TicketAssignment/SessionSelection`), never canonical JSON.
- All UI affordances are HAL-authored; no client role checks; community contributors receive only contributor-safe relations.
- Callback controllers never mutate registrations; workers claim durable effects with fencing and finalize via Application commands inside `IUnitOfWork`.
- Sync mode (`NONE`/`COMPLETION_ONLY`/`SELECTED_FIELDS`/`FULL_CANONICAL`/`MIRROR_ONLY`) is per channel/binding; `NONE` stores nothing and never fulfills required requirements.
- Consent is immutable evidence with typed subjects; purchasing never implies consent; claim approval never grants historical data.

User/operator experience: organizers configure participation, tickets, workflows, and channels inside the implemented Studio workspace; attendees see one accurate primary CTA per event ("Register on ISLAMU" / "Register on organizer website" / "View original event page"), guests complete orders with capability-token management links, and provider health/drift is visible to organizers without exposing attendee data.

Studio route and sidebar ownership:

| Organizer surface | Canonical route | Sidebar authority | Owning phase |
|---|---|---|---|
| Actor Overview / Events | `/studio`, `/studio/events` | Existing Studio workspace availability and managed-actor context | Implemented prerequisite |
| Cross-event Orders | `/studio/orders` | `StudioContextDto._links.view-registration-orders` | Phase 5 |
| Cross-event Attendees | `/studio/attendees` | `StudioContextDto._links.view-participants` | Phases 6/13 |
| Event Registration | `/studio/events/{eventId}/registration` | `EventDto._links.configure-participation` | Phase 2 |
| Event Ticketing | `/studio/events/{eventId}/tickets` | `manage-ticket-types` or `manage-capacity-pools` | Phase 4 |
| Event Orders | `/studio/events/{eventId}/orders` | `view-registration-orders` | Phase 5 |
| Event Attendees | `/studio/events/{eventId}/attendees` | `view-participants` | Phases 6/13 |
| Event Forms | `/studio/events/{eventId}/forms` | `manage-registration-workflow` | Phase 7 |
| Event Integrations | `/studio/events/{eventId}/integrations` | `manage-registration-channels` or `view-registration-provider-health` | Phase 9 |
| Event Payments | `/studio/events/{eventId}/payments` | `manage-event-payments` or `view-payment-reconciliation` | Phases 16–19 |
| Event Promotions | `/studio/events/{eventId}/promotions` | `manage-event-promotions` | Phase 17 |
| Event Check-in | `/studio/events/{eventId}/check-in` | `check-in-admissions` or `view-check-in-summary` | Phase 21 |
| Cross-event Tickets | `/tickets` for attendee self-service; no Studio placeholder | Ticket/order resource HAL | Phases 20/22 |

Public/guest checkout and native form completion remain under `/registration/**`; ticket recovery/self-service remains under capability-scoped `/tickets/**`; they are attendee flows, not Studio pages. No navigation placeholder appears before its API resource emits the exact relation.

---

## 4. Non-Negotiable Constraints

From the repository contract (AGENTS.md §5, QUICK_REFERENCE, GOVERNANCE):

1. Repositories return entities; mapping in handlers. Validators manually instantiated. `Guid` aggregates / `int` lookups / `long` cursors. File-scoped namespaces; two-line `ABOUTME:` headers.
2. GET = `[AllowAnonymous]`+`Public`; writes = `[Authorize]`+`Authenticated` — **except** the new, explicitly governed `PublicTransactional` class introduced by Phase 3 (this is the sanctioned exception route; it must be added to governance docs and architecture tests, never improvised per-endpoint).
3. HAL links are the single source of truth for UI affordances; no DTO capability booleans (`CanViewAttendees` is a named anti-pattern).
4. Tenant isolation via central query filters; every new entity implements `ITenantEntity` (+ audit/soft-delete/concurrency interfaces per `docs/GOVERNANCE.md` entity requirements); `IgnoreQueryFilters` only with the named `SoftDelete` filter.
5. Multi-step writes inside `IUnitOfWork.ExecuteInTransactionAsync`; IDs/timestamps precomputed; **no provider HTTP, email, webhook, or broker call inside a transaction**; side effects post-commit via outbox.
6. Normalized lookups expose `Id`/`Code`/`Name`; stable IDs in `LookupTableSeeder`; no persisted-enum wrappers in API contracts.
7. Controller authoring standard: explicit template, `Name = RouteNames.X`, endpoint classification, `[ProducesResponseType]`, operation-ID naming (`{Controller}_{Action}`); OpenAPI/NSwag regeneration is a discrete governed step.
8. Blazor isolation: client consumes only the generated `IEventApiClient`; no backend project references.
9. Logs/metrics/traces/ProblemDetails must never contain answers, emails, guest tokens, provider payloads, or unbounded labels (consultation NFR-09/10 + repo telemetry pattern).
10. **Migration-baseline coordination:** the privacy-erasure-owned init lanes remain the baseline. Commit `ff30795a2` owns the later additive `20260728152646_AddParticipationHandlingModes` migration, designer, and snapshot. Treat this as a committed artifact only; do not infer database application or runtime rollout.
11. Consultation anti-pattern lists (§24 Report 1, §33 Report 2) are binding forbidden-moves for every phase.
12. Dev-mode waiver: backward compatibility, dual writes, and compatibility shims are **forbidden**, not merely unnecessary — replaced members are deleted.
13. **Hi.Events reuse rule (research-derived, D19 — NO CODE COPY):** Hi.Events is a behavior catalog, never an architecture authority and **never a code source**. Because ISLAMU Event uses a CLA to enable dual-licensing (offering the software under a non-AGPLv3 license to recipients who cannot accept AGPLv3), **copying any code, file, snippet, migration, SQL, or verbatim asset from the Hi.Events repository is forbidden** — third-party AGPLv3 code would contaminate the codebase and break the dual-licensing capability, and its authors are not ISLAMU CLA signatories. All implementation is clean-room: agents may read the *report* (`hi-events-report.md`) for behavior, design, and data-model lessons, but must not open, transcribe, or paraphrase-translate Hi.Events source files into ISLAMU code. This supersedes the report's §10 code-reuse permission. Independently, the reject-list (report §9.3) remains binding: no mutable published prices, no JSON canonical answers, no public/display IDs as authorization, no cache-only idempotency, no float money, no inventory release derived from attendee rows, no external calls inside business transactions, no local status/role authorization in the UI. The "Powered by Hi.Events" branding must obviously never appear in ISLAMU Event.
14. **Money is integer, explicit, and instance-fair:** persisted/API amounts use integer minor units supplied at the contract boundary; the shipped model defines neither decimal-major conversion nor foreign exchange. Platform fee and platform contribution default to zero/off on every self-hosted instance; monetization configuration is instance-admin-only (D18) — tenant-level enablement is a forbidden move.
15. **Studio is the organizer UI boundary:** organizer pages use canonical `/studio/**` routes and the existing `StudioWorkspaceNavigation` / `StudioEventNavigation` replacement model. Event sections come only from the loaded event `_links`; cross-event sections come only from `StudioContextDto._links`. No local role checks, dead sidebar placeholders, third sidebar, or parallel `/events/manage` navigation tree.
16. **Money recipient is immutable organizer authority:** normal administration can never select an instance/tenant administrator merchant or reroute an existing catalog/order/payment/refund. Each self-hoster owns separate Stripe Connect credentials and must disclose its operator identity.
17. **Payment truth is asynchronous:** browser return URLs, Checkout session creation, and accepted refund requests are not success. Only verified provider evidence plus monotonic local transitions and reconciliation may advance payment/refund truth.
18. **Provider calls stay outside transactions:** persist attempts, inbox/outbox work, snapshots, and idempotency first; perform Stripe HTTP afterward; reconcile ambiguous timeouts before retry. Webhooks dedupe and acknowledge without directly orchestrating multi-aggregate business writes.
19. **Credentials are capabilities, not identifiers:** QR, scanner, transfer, lookup, and resend tokens are high-entropy, purpose/audience scoped, expiring and rotatable; store only keyed hashes; never log, persist in analytics, expose through referers, or use display IDs/email as authority.
20. **Admission and commerce history is append-only:** transfer/reissue revokes the prior credential; check-in undo appends a compensating fact; no ticket, check-in, payment, refund, dispute, external-document reference, or consent history is rewritten to simulate a new state.
21. **No production-compliance claim from code:** Stripe approval, merchant/country/currency corridors, consumer/payment-services/tax/invoice obligations, incident ownership, and Islamic-finance review are external launch gates. The product must disclose unavailable/unapproved profiles instead of pretending configuration equals compliance.

---

## 5. Architecture And Design Decisions

### D1 — Dedicated Registration Data Collection bounded context
- **Decision:** New `Registration*` entities own forms/answers; the custom-property subsystem's typed primitives and governance vocabulary (typed value columns, `Ordinal`, namespaced `Namespace+Key` identity, min/max/regex/URL-scheme constraint fields, exposure flags) are mirrored, but **no custom-property table, entity, or projection is reused**.
- **Why:** Event properties describe the event; registration answers describe a participant relationship — different subjects, privacy boundaries, retention, and lifecycle (consultation §1). `docs/CUSTOM_PROPERTIES.md` already forbids workflow-critical state in Layer 3.
- **Alternatives considered:** (a) Reuse `EventCustomPropertyValue` with a "registration" namespace — rejected: wrong parent aggregate, wrong retention/privacy model, anti-pattern §24.2. (b) JSONB response documents — rejected: kills typed filtering/analytics, anti-pattern §24.3.
- **Consequences:** More tables, but each with a single honest owner; shared validation value objects extracted to `Explore.Domain/ValueObjects` for reuse by both systems.
- **Files/layers:** Domain + Persistence (Phases 7–8).

### D2 — Workflow → Requirement → Channel model; five orthogonal provider dimensions
- **Decision:** `RegistrationWorkflow` (per event/purpose) contains `RegistrationRequirement`s (`ALL` at workflow level); each requirement offers `RegistrationChannel`s (`ANY` within a requirement). A channel binds one provider binding with explicit **SchemaAuthority**, **PresentationMode**, **CollectionMode**, **CompletionMode**, **TrustLevel**, and **AnswerSyncMode** — never a single provider enum.
- **Why:** The four providers are not functionally equivalent (consultation §2–4, §10 Report 2); a provider enum conflates ownership, rendering, storage, completion, and trust.
- **Alternatives:** single `RegistrationFormProvider` enum or a `CompositeFormProvider` — both explicitly rejected by the consultation (§3, §4).
- **Consequences:** Slightly more configuration surface; organizers get simultaneous providers, fallbacks, and per-event sync policies without combinatorial provider classes.
- **Files/layers:** Domain + Application (Phases 7, 9).

### D3 — Capability-segregated provider interfaces, fail-closed capability tuples
- **Decision:** Small interfaces (`IRegistrationProviderDescriptor`, `IRegistrationPresentationProvider`, `IRegistrationSchemaReader`, `IRegistrationFormProvisioner`, `IRegistrationSubmissionWriter/Reader`, `IRegistrationCallbackVerifier`, `IRegistrationSubscriptionManager`, `IRegistrationReconciliationProvider`, `IRegistrationSubmissionSink`) registered per provider in Infrastructure. Effective capability = proven profile ∩ connection config ∩ tenant governance ∩ mapping compatibility ∩ authorization. Capability profiles bind to an exact tuple (`ProviderCode`, `DeploymentKind`, `ApiVersion`, `AdapterPolicyVersion`, `ConformanceEvidenceRevision`); unknown tuples fail closed for automatic finalization.
- **Why:** Mirrors the already-shipped webhook capability-authority pattern (verified in `WebhookProviderCapabilityLookup.cs` et al.); avoids `NotSupportedException` interface pollution (anti-pattern §24.10/11).
- **Alternatives:** one fat `IRegistrationFormProvider` — rejected.
- **Consequences:** More interfaces; honest static capability truth; `IRegistrationSubmissionSink` cleanly separates mirror/export destinations from collection.
- **Files/layers:** Application contracts + Infrastructure adapters (Phases 9–12).

### D4 — Buyer–Order–Participant–Ticket aggregate replaces the user-centric intent
- **Decision:** `RegistrationOrder` (nullable `AccountUserId`, `BookingPartyType`, guest capability hash, pinned catalog/workflow/participation versions) replaces `EventRegistrationIntent`, which is **deleted**. `EventRegistration` survives as the materialized per-session admission row but its required `UserId` is replaced by `RegistrationParticipantId` (+ optional `LinkedUserId` denormalization for user-scoped queries). `EventContactShareConsent.SourceEventRegistrationIntentId` is rewired to `SourceRegistrationOrderId`.
- **Why:** Report 2 §11/§16 prove the account-centric aggregate cannot express guests, quantities, families, companies, or deferred assignment; making `UserId` nullable alone is anti-pattern §33.10.
- **Alternatives:** incremental patching of the intent — explicitly rejected by the Final CTO recommendation.
- **Consequences:** Breaking change to registration features, HAL policies, Cerbos policies, DTOs, generated client, and Blazor flow — sanctioned by dev-mode waiver; per-session uniqueness moves from `(User, Session)` to assignment-based uniqueness.
- **Files/layers:** Domain/Persistence/Application/API/Blazor (Phases 5–6).

### D5 — One row per atomic typed answer; sensitive-value split; no canonical JSON
- **Decision:** `RegistrationAnswer` has typed value columns (`Text/Integer/Decimal/Boolean/Date/Time/Instant/OptionId/SensitiveValueId`) with a DB `CHECK (num_nonnulls(...) = 1)` plus type-agreement checks; multivalue via `Ordinal`; subjects via `AnswerSubjectTypeId + AnswerSubjectId`. Sensitive classifications store ciphertext in `RegistrationSensitiveAnswerValue` (key-versioned, optional governed blind index). A short-retention raw provider payload copy lives only on the intake message (existing retention machinery), never as answer truth.
- **Why:** Consultation §6, §18; preserves the platform differentiator (typed, filterable, governed data).
- **Alternatives:** JSONB canonical store — rejected (§24.3).
- **Consequences:** More rows and constraints; straightforward, indexable queries; encryption needs a key-versioning strategy (reuse Data Protection stack — implemented in Task 8.3).
- **Files/layers:** Domain + Persistence (Phase 8).

### D6 — JSON Schema 2020-12 as generated, immutable, content-hashed interchange artifact
- **Decision:** Relational rows stay authoritative; each published `RegistrationFormVersion` generates four artifacts (data schema, UI schema, logic schema, provider-mapping schema), content-hashed into `SchemaHash`.
- **Why:** Consultation §8; enables provider adapters, SDK consumers, and stable validation pointers without making JSON the source of truth.
- **Consequences:** A deterministic serializer + hash test (schema hash stability) is mandatory.
- **Files/layers:** Application service + Domain fields (Phase 7).

### D7 — Provider callbacks extend the existing incoming-webhook intake
- **Decision:** Formbricks/Microsoft callbacks and Google Pub/Sub pushes are ingested as `IncomingWebhookMessage` rows (exact bytes, provider proof verification, dedup) that enqueue `IncomingWebhookEffectOutbox` effects; a registration worker claims effects (fenced), re-verifies, fetches where supported, normalizes, validates, fulfills, and finalizes via Application commands. Callback controllers never touch registration aggregates.
- **Why:** The intake pattern is already hardened (verified §2.1); consultation §11/§26 require exactly this shape.
- **Alternatives:** bespoke registration callback tables — rejected (duplicate unsafe mechanism).
- **Consequences:** Registration effect kinds join the webhook effect vocabulary; retention/redrive machinery is inherited for free.
- **Files/layers:** API controllers + Application effects + Infrastructure verifiers (Phase 9).

### D8 — New `EndpointClass.PublicTransactional`
- **Decision:** Add a fourth endpoint classification for narrowly-scoped anonymous mutations (guest order start/continue/finalize, guest capability-token management), with mandatory: dedicated `public_transactional` rate-limit policy, antiforgery for browser-origin same-site requests, required `Idempotency-Key` on create/finalize, capability-token authorization, minimal order-scoped exposure, and PII-free logging. Architecture tests enforce that `PublicTransactional` actions carry all required attributes.
- **Why:** Report 2 §12; the existing `Public` class explicitly promises "no tenant mutation" and must not be weakened.
- **Alternatives:** overloading `Public` — rejected; per-endpoint ad-hoc `[AllowAnonymous]` writes — forbidden.
- **Consequences:** Governance docs, OpenAPI `x-endpoint-class`, client-generation filters, and Cerbos scaffolding all learn the new class once (Phase 3), then Phase 5 consumes it.
- **Files/layers:** API attributes/tests/middleware + docs (Phase 3).

### D9 — Provenance and four-authority model; `ActorId` semantics narrowed, not overloaded
- **Decision:** Add `EventProvenanceTypeId` (lookup: `ORGANIZER_CREATED/COMMUNITY_REPORTED/TENANT_CURATED/IMPORTED/FEDERATED`), `SubmittedByUserId`, `OrganizerActorId` (nullable), `SourcePublisherName`, `SourceUrl` to `Event`; delete `IsUserReported` and `EventUrl`. `Event.ActorId` keeps its column but is redefined and documented as **publishing authority** (`PublishedByActorId` semantics); a Phase 1 in-slice investigation decides whether a physical rename is cheap enough now (dev mode favors renaming if usage count permits). Listing, participation-management, data-collection, and commercial authority are computed from typed state, never from listing ownership.
- **Why:** Report 2 §3/§7; provenance must be historical and non-removable.
- **Consequences:** Cerbos event policy gains `provenance_type`, `organizer_actor_id`, `submitted_by_user_id`, `organizer_verification_status` attributes; UI badge derived server-side.
- **Files/layers:** Domain/Persistence/API/Cerbos/Blazor (Phase 1).

### D10 — Typed participation configuration; prices move to the ticket catalog
- **Decision:** `EventParticipationConfiguration` (1:1 with Event) carries `ParticipationHandlingModeId` (`INFORMATION_ONLY/WALK_IN/EXTERNAL_MANAGED/PLATFORM_MANAGED`), `AdvanceRegistrationObligationId` (`NOT_APPLICABLE/OPTIONAL/REQUIRED`), `IdentityAccessModeId` (`ACCOUNT_REQUIRED/GUEST_ALLOWED/CAPABILITY_TOKEN_ALLOWED`) plus guest-recovery policy; admission decisions keep using the existing `RegistrationMode` lookup. `Event.IsRegistrationRequired` is deleted in Phase 2; `Event.Price`/`CurrencyCode` and `EventSession.Price` are deleted in Phase 4, and display price is derived from the published ticket catalog.
- **Why:** Report 2 §4/§13.4; one registration engine, not two.
- **Consequences:** Every event gets a participation configuration (seeded default `PLATFORM_MANAGED`-equivalent is **not** assumed — creation flows must set it explicitly per "no implicit business defaults" rule; event create/update commands gain required configuration input).
- **Files/layers:** Domain/Persistence/Application/API/Blazor (Phases 2, 4).

### D11 — Capacity pools with atomic holds inside `IUnitOfWork`
- **Decision:** `EventCapacityPool` (+ `HoldDurationSeconds`, oversell policy) shared across ticket types; `RegistrationInventoryHold` with states `Active/Consumed/Released/Expired/Cancelled`; hold creation atomically validates catalog version, quantity limits, and pool capacity using row/counter locking inside one transaction; a background sweeper releases expired holds; hold policies `NO_HOLD_UNTIL_READY/TIMED_HOLD_ON_SELECTION/APPROVAL_NO_HOLD/WAITLIST_WHEN_FULL`.
- **Why:** Report 2 §14/§20; NFR-04 concurrency safety across replicas.
- **Consequences:** Requires a deliberate locking strategy in PostgreSQL (e.g., `SELECT ... FOR UPDATE` on pool counters via repository method) — folded into Task 5.3 with a persistence-level race test.
- **Files/layers:** Domain/Persistence/Application + background service (Phase 5).

### D12 — Consent is immutable evidence with typed subjects
- **Decision:** New `RegistrationConsentRecord` (purpose, exact text snapshot, text/UI versions, language, granted/withdrawn, provider/submission source) with typed subject reference (`User/RegistrationPurchaser/RegistrationParticipant/GuestContact`); `EventContactShareConsent` is evolved to the same subject typing (dev-mode breaking change) and its verified-recipient rule is enforced (`OrganizerActorId` present + verified; never on unclaimed reported events).
- **Why:** Consultation §18 (Report 1) + §22 (Report 2); a Boolean answer is not consent evidence (anti-pattern §24.16).
- **Files/layers:** Domain/Application/API (Phases 8, 13).

### D13 — Clean-baseline schema strategy (no data migrations)
- **Decision:** The privacy-erasure workstream's three generated init lanes are the baseline. Later model/configuration/seeder changes use additive migrations. Commit `ff30795a2` contains the committed participation/ticketing artifact. No legacy-data backfills exist anywhere in this plan; consultation §30.1 migration defaults are recorded as N/A for the clean baseline.
- **Why:** The generated init migrations and snapshots exist; `platform-privacy-erasure` forbids restoring old migration IDs and mandates generated migrations only.
- **Consequences:** Registration never owns baseline regeneration or history rewriting. A committed migration artifact does not prove it was applied to a database.
- **Files/layers:** Persistence (all phases).

### D14 — Trust levels and sync modes govern finalization
- **Decision:** `RegistrationTrustLevel` lookup (`FirstParty/SignedProvider/AuthenticatedProviderFetch/DelegatedAutomation/UserReturnOnly/ManualImport`) and `AnswerSyncMode` lookup (`NONE/COMPLETION_ONLY/SELECTED_FIELDS/FULL_CANONICAL/MIRROR_ONLY`) are typed lookups on bindings/channels; event/tenant policy sets a minimum trust level for automatic finalization; below it → `NeedsReconciliation` for organizer review.
- **Why:** Consultation §3, §10 (Report 2 §10), Microsoft `DelegatedAutomation` classification (§16).
- **Files/layers:** Domain lookups + Application finalization policy (Phase 9).

### D15 — Provider credentials via `SecretBinding`, tenant scope first
- **Decision:** `RegistrationProviderConnection` references credentials exclusively through the existing Explore.Secrets `SecretBinding`/`SecretDefinitionRegistry` (scopes today: Instance, Tenant). Organization/group-scoped connections are deferred until `SecretScope` grows (recorded as deferred work, not silently hacked).
- **Why:** Verified `SecretScope` enum; consultation requires secret references, never inline credentials.
- **Files/layers:** Domain/Secrets + Infrastructure (Phase 9).

### D16 — State machines are explicit lookups, never overloaded `ApprovalStatus`
- **Decision:** Three independent machines as typed lookups with domain rule classes (mirroring `RegistrationPolicyRules`): `RegistrationOrderStatus` (`Draft/AwaitingIdentity/AwaitingParticipantDetails/AwaitingRequirements/ReadyForCheckout/AwaitingPayment/AwaitingApproval/Waitlisted/Confirmed/Expired/Cancelled/NeedsReconciliation`), `RegistrationAttemptStatus` (`Created/Launched/Submitted/Expired/Superseded/Cancelled`), `RegistrationSubmissionStatus` (`Received/ProviderVerified/Normalized/Valid/Invalid/RequirementFulfilled/Finalized/Rejected/NeedsReconciliation`). `ApprovalStatus` remains solely the organizer approval verdict.
- **Why:** Consultation §10; anti-pattern §24.9. Hi.Events independently validates separated order/payment/refund axes (report §5.3) but spreads transitions across handlers without concurrency guards — ISLAMU centralizes transitions in rule classes and persists them conditionally.
- **Files/layers:** Domain enums/lookups + `Services/Registration/` rules (Phases 5, 8).

### D17 — Five ticket pricing modes with server-validated buyer-chosen prices
- **Decision:** `EventTicketType` gains a required `TicketPricingModeId` lookup with exactly five modes, each with mode-specific fields and validation in a pure `TicketPricingRules` class:
  - `FIXED` — organizer-set `PriceAmount`; buyer chooses nothing.
  - `FREE` — no amounts anywhere; always the free confirmation path.
  - `DONATION` — buyer-chosen amount to the **organizer**; organizer may set `MinimumPriceAmount` (0 allowed); checkout input **defaults to 0** when the minimum is 0 (Gumroad-style "0+" field), so free admission with optional giving is one mode, not two tickets.
  - `PAY_WHAT_YOU_CAN` — buyer-priced admission with optional `MinimumPriceAmount` and optional `SuggestedPriceAmount` used as input placeholder/preset; 0 permitted iff minimum is 0.
  - `SLIDING_SCALE` — Leanpub-style: required `MinimumPriceAmount` + required `SuggestedPriceAmount` (≥ minimum). Checkout renders **two linked sliders**: "You pay" (bounded below by minimum, defaulting to suggested) and "Organizer earns" (the exact amount the organizer receives after the instance's platform fee policy, D18). Dragging either slider recomputes the other, so the payer sees precisely what does *not* go to the organizer — transparency by construction.
  The buyer-chosen unit price is validated **server-side** against the pinned catalog version's mode bounds and snapshotted per order line (`ChosenUnitPriceAmountSnapshot`); a zero-total order follows the free confirmation path; any positive total stops at `AwaitingPayment` (charging is the payment workstream's job).
- **Why:** User requirement to exceed Hi.Events' paid/free/donation breadth (report §4.3 proves market demand for `DONATION` with minimum, but its prices are mutable in place and become PHP floats — both rejected). Sliding-scale transparency strengthens both community trust and the ISLAMU SaaS story.
- **Alternatives considered:** Hi.Events-style mutable `ProductPrice` rows — rejected (violates immutable catalogs); one generic "flexible price" mode with UI hints — rejected (the five modes have genuinely different validation and UX semantics); modeling donation as a separate general product — rejected (dilutes admission vocabulary, report §4.1 lesson).
- **Consequences:** Mode-specific widgets in checkout (numeric input, dual sliders); API and persistence accept already-normalized integer minor units and define neither decimal-major conversion nor foreign exchange; `TicketPricingRules` becomes part of publish preflight; tier-style multi-price options can later map onto multiple ticket types without a new mechanism.
- **Files/layers:** Domain/Persistence (Phase 4), Application/API/Blazor (Phase 5).

### D18 — Instance-scoped platform monetization: fee transparency + optional platform contribution
- **Decision:** Two instance-admin-only, DB-stored, default-off/zero concepts, deliberately separated because the money has different destinations:
  1. **`PlatformFeePolicy`** (instance scope; percentage + fixed components; default 0/0): the amount the instance operator retains from organizer-directed payments. Used to compute the "Organizer earns" figure in `SLIDING_SCALE` UX and organizer-facing price previews (with an "excluding payment-processing fees" disclaimer until the payment workstream defines processor costs). Snapshotted (policy version) on order lines whenever non-zero.
  2. **`PlatformContributionSetting`** (instance scope): a LaunchGood-style optional checkout add-on directed to the **instance operator**, never the organizer. Contains: enable flag; DB-stored heading and body text (e.g., "Help us help the Ummah — because this instance doesn't charge a platform fee, we rely on the generosity of donors like you 💓") — fully data-driven, never hardcoded, localization-ready; a configurable quick-percentage option list seeded `0` (default, preselected) plus `5 / 10 / 15 / 20`, rendered as a dropdown whose options show the percentage on the left and the server-computed currency amount (percentage × order total) on the right. The buyer's selection persists on the order as `RegistrationOrderPlatformContribution` (percentage, computed amount, setting-version snapshot).
  Enablement, text, and percentages are managed exclusively through `Admin`-classified endpoints restricted to **instance administrators**; tenant admins can neither see-in-management, enable, nor alter them (fail-closed authorization tests). Rationale: instance operators pay for/host the software; tenant-level enablement is an abuse vector.
- **Why:** User requirement; enables the "ISLAMU nonprofit single-tenant instance with fully free Islamic events funded by voluntary contributions" scenario, and a transparent commercial SaaS offering — while every self-hosted instance stays zero-fee by default (open-source fairness).
- **Alternatives considered:** Hardcoded tip UI — rejected (must be per-instance data); tenant-level enablement — explicitly rejected by requirement; contribution as a ticket order line — rejected (different money destination; would corrupt organizer earnings/capacity semantics); governance-settings key-value storage — rejected for the structured content (typed versioned entities chosen so orders can snapshot deterministically), while the *enablement lock* semantics follow the existing instance-lock convention of the 5-tier settings cascade.
- **Consequences:** Order totals = organizer-directed line totals + instance-directed contribution, kept separate everywhere (DTOs, exports, future payment split); contribution > 0 on an otherwise free order makes the order payable — until payments ship, the contribution UI is composed only where a payment path will exist; `OrganizerEarningsCalculator` is a pure, decimal-exact Application service.
- **Files/layers:** Domain entities + Admin API + Blazor instance settings (Phase 4, Task 4.5); order component + checkout composition (Phase 5, Task 5.10).

### D19 — Hi.Events is a behavior catalog only: concepts yes, code never
- **Decision:** Adopt Hi.Events' behavioral lessons: reserve inventory **before** collecting PII; visible reservation expiry with countdown, navigation-away warning, explicit abandon, and business-state-specific recovery screens; snapshot every mutable commercial fact on order lines; shared-capacity visualization (used vs total, pool-overrides-product warning); buyer questions separated from participant questions with buyer-to-participant copy controls; anti-enumeration ticket/order lookup. Adapt its concepts onto ISLAMU aggregates per the report's §9.2 mapping table. Reject its persistence, authorization, money, idempotency, and side-effect machinery wholesale (§9.3, now constraint §4.13). **Code reuse is categorically forbidden** — ISLAMU Event's CLA-based dual-licensing model (non-AGPLv3 licenses for recipients who cannot use AGPLv3) would be destroyed by importing third-party AGPLv3 code whose authors never signed the CLA; the report's §10 code-reuse permission is overridden. Implementation is clean-room from the report and this plan only. Its concrete defects (§7) become mandatory acceptance criteria in Phases 4–8. Its commercial breadth (Stripe/refunds/invoices/taxes/promo/affiliates/add-ons/waitlist offers/check-in/transfers/lookup) is a deferred-feature inventory recorded once in Task 14.8 — not silent scope growth.
- **Why:** Report §14 final recommendation (behavior catalog) + the ISLAMU CLA/dual-licensing business model; converts someone else's production experience into cheap, early test criteria at zero licensing risk.
- **Alternatives considered:** AGPLv3 code reuse with provenance pinning (the report's §10 suggestion) — rejected: legally compatible for a pure-AGPLv3 project but fatal to CLA-based dual-licensing.
- **Consequences:** Zero new phases from the report; strengthened acceptance criteria; one new docs task (14.8); no-code-copy rule in §4.13; implementation agents never open the Hi.Events repository during coding.
- **Files/layers:** Cross-cutting; ADR-018 records the rationale (Task 0.2).

### D20 — Registration organizer operations extend the implemented Studio workspace
- **Decision:** Keep the shipped `WorkspaceRegistry` and single `shell.workspace-nav` architecture. `StudioWorkspaceNavigation` continues to own actor-level navigation and swaps to `StudioEventNavigation` for event routes. Event sections are added only when the shared `EventDto` contains their management relation. Cross-event Orders and Attendees use one new authenticated, private/no-store HAL resource, `StudioContextDto`, returned by `GET /api/studio/context?actorId={optionalActorHint}` and consumed through a scoped `IStudioContextService`; the server validates the actor hint against current principal authority.
- **Why:** Studio, route classification, actor switching, shared event context, and relation-mapped navigation already exist. Reusing them keeps deep links, mobile projection, RTL, accessibility landmarks, persisted shell state, and revocation behavior consistent while preserving HAL as the action/navigation authority.
- **Alternatives considered:** standalone `Pages/Events/Manage/**` navigation — rejected as a second organizer product; hardcoded role-based Studio links — rejected because they drift from server authority; one API probe per sidebar item — rejected as noisy and race-prone; a third event sidebar — rejected by ADR-019.
- **Consequences:** Organizer pages land under `/studio/**`; attendee checkout remains under `/registration/**`. Phase tasks extend the centralized `Routes.razor`, `StudioEventNavigation`, `StudioEventShell` or focused Studio pages, and their existing test suites. Actor Overview/Events stay as the base navigation; additional cross-event links render only from `StudioContextDto._links`. Check-in, Communications, and Analytics remain absent until an owning workstream ships both route and HAL relation.
- **Relation mapping:** `configure-participation` → Registration; `manage-ticket-types|manage-capacity-pools` → Ticketing; `view-registration-orders` → Orders; `view-participants` → Attendees; `manage-registration-workflow` → Forms; `manage-registration-channels|view-registration-provider-health` → Integrations. `export-consented-contacts` is an action inside Attendees, not another sidebar destination.
- **Files/layers:** API/Application contract in Phase 5; Blazor `Routes.razor`, `Pages/Studio/**`, `StudioWorkspaceNavigation.razor`, `StudioEventNavigation.razor`, and focused client tests across Phases 2/4/5/6/7/9/13.

### D21 — `OrganizerDirect` is the only first-release payment profile
- **Decision:** Stripe Connect direct charges are created in the event organizer actor's connected-account context. Every self-hosted operator supplies its own Stripe platform credentials. There is no tenant/instance-admin fallback merchant, pooled administrator recipient, or historical recipient rewrite.
- **Why:** The I-VSD payment consultation identifies direct charges as the safest generally self-hostable allocation of merchant funds, refunds, disputes, and negative-balance responsibility. It also prevents a malicious normal administrator configuration from silently rerouting organizer proceeds.
- **Consequences:** Paid publication requires an eligible organizer actor and active connected account. The catalog and each `PaymentAttempt` snapshot organizer actor, connected account, merchant country, currency, charge profile, and policy versions. Account replacement affects future sales only.
- **Files/layers:** Domain/Application/Infrastructure/API/Blazor (Phases 15–19).

### D22 — Paid-event policy is a narrowing hierarchy; currency is explicit and immutable
- **Decision:** Effective paid-event policy is `hard invariant ∩ instance ceiling ∩ tenant narrowing ∩ organizer/event choice ∩ live provider/account capability`. Instance settings control allowed organizer kinds, verification floor, profiles, currencies, and risk ceilings; tenants may only narrow. The organizer confirms one ISO currency for a published catalog/order. Location may suggest but never decides; online events always require explicit choice.
- **Why:** This preserves safe upstream defaults while supporting Belgium-only `{EUR}` instances and global ISLAMU deployments ordered `EUR`, `USD`, `MAD`, `SAR`, `AED` without ambiguous “dirham” labels or internal FX.
- **Consequences:** Provider capability is rechecked at publication and checkout. Adaptive pricing and mixed-currency orders remain disabled until a future FX/presentment ADR.
- **Files/layers:** Domain/Persistence/Application/API/Blazor (Phase 16).

### D23 — Payment state is provider-neutral, durable, and reconciled; Stripe is one adapter
- **Decision:** `PaymentAttempt` owns local payment intent, provider identity, idempotency, connected-account/currency/amount snapshots, and a monotonic state machine independent from order, approval, form attempt, and refund state. Application defines only the small provider capabilities its use cases consume (connected-account readiness, Checkout/payment retrieval, refund retrieval, and separately gated payout control); Infrastructure implements them with the official, exact-pinned stable `Stripe.net` SDK. Stripe-hosted Checkout creation and all provider calls run outside business transactions from durable work; signed Connect webhooks enter an idempotent inbox; scheduled reconciliation resolves ambiguity.
- **Why:** Browser return navigation and provider request acceptance are not payment confirmation. Duplicate/delayed webhooks and ambiguous timeouts are normal provider behavior.
- **Consequences:** Keep Stripe models/options/exceptions inside `Explore.Infrastructure`; map them to provider-neutral commands, observations, and failure categories at the boundary. Use one configured instance-based `StripeClient` path with request-scoped `StripeAccount` and durable `IdempotencyKey`; never use legacy global `StripeConfiguration.ApiKey`, beta/alpha packages, undocumented parameters, or raw Stripe API requests. Separate adapters by responsibility rather than creating a god service, provider factory, or inheritance framework before a second provider exists. Provider conformance tests exercise the same Application contracts. Persist retry identity before handoff and never include PII in idempotency keys.
- **Files/layers:** Domain/Application/Persistence/Infrastructure/API/Blazor (Phase 18).

#### Stripe.net support matrix for this workstream

| Owned capability | Official SDK boundary | Owning phase and completion evidence |
|---|---|---|
| Connected-account creation/linking, required capabilities, readiness, restrictions | Typed Connect account/account-link operations through the configured `StripeClient`; provider events translated after strict verification | Phase 16; hosted-return false-positive, duplicate event, restricted account, and reconciliation fixtures |
| Direct-charge hosted Checkout and payment retrieval | Typed Checkout/payment operations with `RequestOptions.StripeAccount` + durable `IdempotencyKey` | Phase 18; header/body/idempotency, SCA/async status, timeout, retry, and reconciliation fixtures |
| Connect webhook authenticity and routing | `EventUtility.ConstructEvent` on the raw body; strict endpoint/SDK API-version match; minimal normalized durable envelope | Phases 16/18/19; signed, duplicate, out-of-order, wrong-mode/account, and version-mismatch fixtures |
| Partial/full refunds and dispute projection | Typed refund/retrieval operations in the original connected-account context; allowlisted refund/dispute observations | Phase 19; pending-balance, partial/full, late-success, error, and dispute fixtures |
| Protected payout control | Typed stable SDK operations only when the separately approved Stripe/legal/operator contract permits them | Phase 24; disabled on missing approval or any need for preview/raw/undocumented access |
| Transport, errors, version upgrades, telemetry | Shared HTTP transport, explicit bounded SDK retry/timeout, provider-neutral `StripeException` mapping, bounded request-ID retention | Phases 16/25; deterministic transport conformance and documented package/API/webhook upgrade drill |

This matrix is the meaning of full Stripe support here. Stripe Billing/subscriptions, Tax, Invoicing, Payment Links, Terminal, Issuing, Treasury, and unrelated API resource families remain outside the Event product boundary.

### D24 — Refund protection is a runtime floor, not editable terms text
- **Decision:** Versioned refund policy snapshots may be stricter than an instance minimum but never weaker. Organizer cancellation, duplicate/incorrect charge, and material non-delivery trigger the configured mandatory remedy. `RefundAttempt` and dispute projections remain independent, asynchronous, idempotent, and pinned to the original connected account; `Requested`/`Pending` never renders as `Succeeded`.
- **Why:** The I-VSD consultation rejects blanket no-refund terms and records direct-charge insufficient-balance refunds as potentially pending.
- **Consequences:** Cancellation first stops sales locally, then writes one outbox job per captured payment. Provider webhooks/reconciliation own terminal truth; buyer and operator surfaces expose unresolved states honestly.
- **Files/layers:** Domain/Application/Persistence/Infrastructure/API/Blazor (Phase 19).

### D25 — Promotions are local commercial truth and reserve usage atomically
- **Decision:** Versioned event-scoped promotion definitions support fixed-minor and basis-point discounts, validity windows, currency/minimum-subtotal rules, eligible ticket/product sets, total and per-purchaser limits, and revocation for future use. Application of a normalized code creates a live redemption reservation counted with confirmed redemptions; expiry/cancellation releases it exactly once. Orders snapshot the applied promotion and discount; Stripe receives only the final immutable charge composition.
- **Why:** Provider-owned promotion codes would duplicate catalog authority, impede a future payment adapter, and race with local inventory holds.
- **Consequences:** Promotion lookup is rate-limited and code values are never logged. Discount arithmetic stays in integer minor units with deterministic allocation and no negative lines/totals.
- **Files/layers:** Domain/Persistence/Application/API/Blazor (Phase 17).

### D26 — `AdmissionTicket` owns a rotatable opaque credential, never a display ID
- **Decision:** A confirmed free order or reconciled successful paid order issues one `AdmissionTicket` per concrete ticket assignment. Its QR contains a versioned, high-entropy opaque credential with no PII, amount, email, order display ID, or authorization claims; persistence stores only a keyed lookup hash and bounded metadata. Reissue/transfer rotates and revokes the prior credential.
- **Why:** Display IDs are enumerable and immutable signed payloads are hard to revoke. Online opaque validation reuses the platform's tenant, state, entitlement, and audit authorities.
- **Consequences:** Phase 1 admission is online. Offline-verifiable signed credentials require a later ADR-023 extension for signing-key custody, rotation, revocation distribution, clock skew, and compromised-device recovery. QR encoding/decoding requires a clean-room dependency/license gate; experimental browser detection is optimization only.
- **Files/layers:** Domain/Persistence/Application/API/Blazor/Infrastructure (Phase 20).

### D27 — Check-in is an append-only, entitlement-targeted admission fact
- **Decision:** Check-in targets are event/day/session scopes resolved from ticket entitlements and published schedules. `AdmissionCheckInEvent` records check-in, compensating undo, actor/scanner, target, reason, and time; active state is derived/enforced atomically. Duplicate scans are idempotent, wrong-tenant/event/target and revoked credentials fail closed, and batch input returns per-item results.
- **Why:** Deleting or toggling a Boolean destroys audit and creates race ambiguity. One event-level flag cannot represent sessions, days, re-entry, or undo.
- **Consequences:** Scanner users authenticate normally or use a narrow, expiring, revocable scanner capability. Camera, HID keyboard, and manual entry share one API path and HAL relation.
- **Files/layers:** Domain/Persistence/Application/API/Blazor (Phase 21).

### D28 — Transfer and recovery are capability-scoped workflows, not ownership rewrites
- **Decision:** A transfer creates an expiring offer with split recipient PII and a hashed single-purpose acceptance capability. Atomic acceptance changes future holder/participant assignment, revokes and rotates admission credentials, preserves order/payment/price/consent/audit history, and never reroutes money. Ticket lookup/resend returns indistinguishable responses and uses random, single-use, expiring capabilities; authenticated users get account-scoped self-service.
- **Why:** Email/display IDs cannot authorize access, and transfer must invalidate copied QR images without corrupting purchaser evidence.
- **Consequences:** Transfer deadlines, max count, checked-in prohibition, guardian/company restrictions, and eligible ticket types are published policy. Organizer correction/reissue is a separate audited action.
- **Files/layers:** Domain/Persistence/Application/API/Blazor (Phases 20, 22).

### D29 — Event breadth stays event-bound; specialist business domains stay external
- **Decision:** Waitlist entries/offers are separate from orders until a bounded offer reserves capacity. Event-bound add-ons and fulfillment never become ticket entitlements or check-in state. Email marketing remains owned by Listmonk; bookkeeping, tax determination, legal invoice/credit-note issuance, and accounting remain owned by Qonto or another approved external system. Event may emit post-commit commercial/contact facts and retain bounded sync references, but does not create specialist-domain aggregates or UIs.
- **Why:** These capabilities may consume event facts but do not share lifecycle, authorization, legal authority, or product purpose with event registration and admission.
- **Consequences:** Phase 23 stays limited to event-bound waitlists/add-ons. The former internal tax/invoice phase is removed and preserved in `dev/report/event-platform-boundary-and-external-business-integrations.md`; any Qonto implementation becomes a separate optional workstream and never blocks core paid events.
- **Files/layers:** ADR/Docs plus future provider-specific integration work; active Event scope ends at Phase 23 for this decision.

### D30 — `ProtectedDelayedPayout` is approval-only and never called escrow
- **Decision:** A strict post-event release profile is not part of the default payment implementation. It may be built only after Stripe confirms the account/control model and country corridors, Belgian/EU counsel accepts the regulatory/consumer allocation, qualified Islamic-finance review is recorded, and an operator owns reserves, disputes, complaints, and reconciliation. It uses an explicit `SettlementReleaseAt` milestone, never public event midnight.
- **Why:** Stripe manual payouts have country-specific limits, are not escrow, and strict platform control can shift negative-balance and operational risk to the operator.
- **Consequences:** Phase 24 stays disabled/blocked when any approval is absent. `OrganizerDirect` remains functional and truthful without it; long-advance/open-ended events cannot receive a false held-until-event promise.
- **Files/layers:** ADR/Domain/Application/Infrastructure/API/Blazor/Ops (Phase 24).

---

## 6. Implementation Phases

> Phase ordering is a strict dependency chain except where noted. Each phase is a reviewable vertical slice ending in exactly one Release build and at most one test-project run. Tasks fold their own tests and doc updates in; there are no standalone QA/doc tasks. NSwag client regeneration + `schemas/openapi_islamu-event.json` refresh is a discrete final step of any phase that changes the API surface (kept inside the phase's last API/Blazor task per governance).

### Phase 0: Governance, ADRs, And Contract Intent
- **Goal:** Lock the architecture in decision records, create the machine-readable contribution-contract intent, and resolve the migration-baseline ordering with the erasure workstream before any code.
- **Depends on:** Nothing.
- **Relevant files:** `docs/adr/ADR-016-registration-data-collection-context.md` (new), `docs/adr/ADR-017-event-participation-authority-model.md` (new), `docs/adr/ADR-018-registration-order-ticketing-aggregate.md` (new), `.claude/contract/intents.yaml` (existing), `docs/GOVERNANCE.md` (existing, PublicTransactional forward note), `dev/active/registration-data-collection/*` (existing).
- **Related skills/rules:** `skill-authoring` conventions for intent shape; `docs/DOCUMENTATION_STYLE_GUIDE.md`.
- **Acceptance criteria:** Three ADRs accepted-status with decision/consequences sections mirroring D1–D16; new `registration-data-collection` intent lists must-reads, skills, rules, paths, minimum tests, docs, unique acceptance (incl. consultation anti-pattern lists as forbidden moves); migration-ordering decision recorded in context file.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Docs-only phase; revert files. If architecture tests flag intent-schema violations, fix the YAML before proceeding.

#### Task 0.1: Author ADR-016 (Registration Data Collection bounded context & provider channels)
- **Type:** create
- **Layer:** Docs
- **Files:** `docs/adr/ADR-016-registration-data-collection-context.md` (new)
- **Description:** Record D1, D2, D3, D5, D6, D7, D14 with context, decision, alternatives, consequences; cite consultation sections; include the invariant block ("ISLAMU owns the workflow and normalized record...").
- **Acceptance Criteria:**
  - [ ] ADR follows existing ADR file conventions (verified against ADR-015 format)
  - [ ] All anti-patterns from consultation §24 listed as rejected alternatives
- **Dependencies:** none
- **Effort:** M
- **Required Skills/Rules:** documentation style guide

#### Task 0.2: Author ADR-017 (participation authority) and ADR-018 (order/ticket aggregate)
- **Type:** create
- **Layer:** Docs
- **Files:** `docs/adr/ADR-017-event-participation-authority-model.md` (new), `docs/adr/ADR-018-registration-order-ticketing-aggregate.md` (new)
- **Description:** ADR-017 records D8, D9, D10, D12 (four authorities, provenance, participation config, PublicTransactional, consent subjects). ADR-018 records D4, D11, D16, D17, D18 (buyer–order–participant–ticket, capacity pools/holds, state machines, five pricing modes, instance monetization, `AwaitingPayment` boundary, payment explicitly out of scope) **and** the Hi.Events research evidence: why ISLAMU does not use "attendee-as-ticket" or mutable published products (report §8/§11.1), the adopt/adapt/reject boundary (D19, report §9), and the **no-code-copy rule**: ISLAMU's CLA-based dual-licensing forbids copying any Hi.Events (AGPLv3) code — behavior/design/data-model lessons only, clean-room implementation, report §10's code-reuse permission explicitly overridden.
- **Acceptance Criteria:**
  - [ ] Report 2 §33 anti-patterns listed as rejected alternatives
  - [ ] Payment boundary documented as a named future ADR dependency
  - [ ] Hi.Events adopt/adapt/reject boundary + CLA/dual-licensing **no-code-copy** rule recorded, citing `hi-events-report.md` §9–§10 and stating the override of §10
- **Dependencies:** 0.1
- **Effort:** M

#### Task 0.3: Add `registration-data-collection` intent to the contribution contract
- **Type:** modify
- **Layer:** Docs
- **Files:** `.claude/contract/intents.yaml` (existing)
- **Description:** Model the entry on the `webhook-delivery-redesign` intent: triggers, must_read_docs (ADRs 016–018, this plan/context/tasks, DOMAIN/API/AUTHORIZATION/SECURITY-MODEL/WEBHOOKS/CONTACT_SHARING/MULTI_TENANCY/OUTBOX_PATTERN/TESTING), load_skills/rules, paths_in_scope (Domain/Application/Persistence/Infrastructure/API/Blazor registration+ticket+participation+provenance globs, cerbos, tests, docs), minimum_tests (all nine projects), docs_to_update, unique_acceptance (external completion ≠ confirmation; attempts pin versions; typed answers; HAL authority; PublicTransactional controls; no provider IO in transactions), forbidden_without_approval (both consultation anti-pattern lists condensed), verification_commands.
- **Acceptance Criteria:**
  - [ ] YAML parses; architecture tests that validate contract files stay green
  - [ ] Intent cross-references this workstream's three dev docs
- **Dependencies:** 0.1, 0.2
- **Effort:** M

#### Task 0.4: Resolve migration-baseline ordering with the erasure workstream
- **Type:** investigate
- **Layer:** DevOps
- **Files:** `dev/active/registration-data-collection/registration-data-collection-context.md` (existing), `src/Explore.Persistence/Migrations/` (observe only)
- **Description:** Verify the three privacy-erasure-owned init lanes and record that registration's first allowed migration is a generated additive migration after the owning phase's model/configuration/seeder work is complete. Do not create or delete any migration artifact in this task.
- **Acceptance Criteria:**
  - [ ] Decision + date recorded in context Key Decisions
- [ ] Tasks/context record that the baseline exists and registration must not regenerate or rewrite it
- **Dependencies:** none
- **Effort:** S

---

### Phase 1: Event Provenance, Organizer Authority, And Public Actions
- **Goal:** Deliver the discovery-only product mode: typed provenance, contributor vs organizer separation, moderated external actions, claim workflow, non-removable community badge — with authorization fail-closed.
- **Depends on:** Phase 0.
- **Relevant files:** existing — `src/Explore.Domain/Event.cs`, `src/Explore.Persistence/Configurations/Entities/EventConfiguration.cs`, `src/Explore.Persistence/ExploreDbContext.DbSets.cs`, `ExploreDbContext.QueryFilters.cs`, `src/Explore.Persistence/Seed/LookupTableSeeder.cs`, `src/Explore.Application/Features/Events/**`, `src/Explore.API/Controllers/EventController.cs` (name verified at implementation), `src/Explore.API/Hateoas/Policies/EventLinkPolicy.cs`, `src/Explore.API/Hateoas/RouteNames.cs`, `src/Explore.Application/Hateoas/LinkRelations.cs`, `cerbos/policies/islamuevent_event.yaml`, `src/Explore.Blazor.Client/Pages/Events/**`; new — `src/Explore.Domain/EventProvenanceType.cs`, `Enums/EventProvenanceTypeEnum.cs`, `EventPublicAction.cs`, `EventPublicActionKind.cs`, `Enums/EventPublicActionKindEnum.cs`, `EventPublicActionHealthState.cs`, `Enums/EventPublicActionHealthStateEnum.cs`, `EventOrganizerClaim.cs`, `EventOrganizerClaimStatus.cs`, `Enums/EventOrganizerClaimStatusEnum.cs`, `ValueObjects/ExternalActionUrl.cs`, plus configurations/features/policies listed per task.
- **Related skills/rules:** `domain.md`, `efcore-persistence.md`, `application-layer.md`, `api-controllers.md`, `api-hateoas.md`, `blazor-client.md`, `cqrs-mediatr-guidelines`, `auth-patterns`.
- **Acceptance criteria:** FR-PROV-01…08 satisfied; contributor authorization matrix (§23) enforced server-side; `IsUserReported`/`EventUrl` deleted; community badge non-removable and HAL/DTO-derived; external links validated (HTTPS, scheme allowlist, no open redirect).
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Slice is additive except the two deleted Event fields; if downstream compilation reveals unexpectedly broad `EventUrl` usage, complete the removal within the phase (no compatibility shim) and record the discovered surface in context.

#### Task 1.1: Provenance typed state on `Event` + `ActorId` semantics decision
- **Type:** modify + investigate
- **Layer:** Domain
- **Files:** `src/Explore.Domain/Event.cs` (existing), `src/Explore.Domain/EventProvenanceType.cs` (new), `src/Explore.Domain/Enums/EventProvenanceTypeEnum.cs` (new)
- **Description:** Add `EventProvenanceTypeId` (int FK, required), `SubmittedByUserId` (Guid?, FK User), `OrganizerActorId` (Guid?, FK Actor), `SourcePublisherName` (string?), keep existing `SourceUrl` semantics on the new `EventPublicAction` instead of Event; delete `IsUserReported` and `EventUrl`. Enumerate `ActorId` usages (`rg "ActorId" src/`), decide rename vs documented narrowing, execute the decision in this slice, and record it. Add domain rule class `Services/Registration/EventAuthorityRules.cs` (new) computing listing/participation/data/commercial authority from typed state; unit-test it.
- **Acceptance Criteria:**
  - [ ] `IsUserReported`/`EventUrl` no longer exist anywhere in `src/`
  - [ ] `EventAuthorityRules` denies organizer authority when `OrganizerActorId` is null (fail-closed test)
  - [ ] Provenance is required state — no implicit default in the entity
- **Dependencies:** 0.4 (ordering only for persistence artifacts)
- **Effort:** L

#### Task 1.2: `EventPublicAction` + kinds + health states + URL value object
- **Type:** create
- **Layer:** Domain
- **Files:** new domain files listed above; `src/Explore.Domain/ValueObjects/ExternalActionUrl.cs` (new)
- **Description:** Typed, ordered action collection (`ORIGINAL_SOURCE`, `EXTERNAL_EVENT_PAGE`, `EXTERNAL_REGISTRATION`, `OPTIONAL_QUESTIONNAIRE`, `LIVESTREAM`, `ORGANIZER_CONTACT`), health states (`PendingReview/Active/Broken/Unsafe/Disabled/Expired`), `IsPrimary` with a domain rule enforcing at most one primary participation CTA, and `ExternalActionUrl` value object (normalized parse, HTTPS-only default, blocked schemes, no fragments of credential userinfo, stored destination domain for disclosure).
- **Acceptance Criteria:**
  - [ ] Value-object tests reject `javascript:`, `data:`, `file:`, protocol-relative and userinfo URLs
  - [ ] Domain rule test: two primary actions rejected; zero actions valid
- **Dependencies:** 1.1
- **Effort:** M

#### Task 1.3: `EventOrganizerClaim` aggregate
- **Type:** create
- **Layer:** Domain
- **Files:** `src/Explore.Domain/EventOrganizerClaim.cs` (new), `EventOrganizerClaimStatus.cs` (new), `Enums/EventOrganizerClaimStatusEnum.cs` (new)
- **Description:** Fields per consultation §8 (`ClaimantActorId`, status `Pending/EvidenceRequired/Approved/Rejected/Withdrawn/Expired`, evidence type/reference, reviewer, decision reason code, `ConcurrencyStamp`); domain transition methods (no silent status sets); approval sets `Event.OrganizerActorId` but **never** touches historical data (rule + test).
- **Acceptance Criteria:**
  - [ ] Invalid transitions throw; approval effect limited to organizer assignment
- **Dependencies:** 1.1
- **Effort:** M

#### Task 1.4: Persistence for Phase 1 entities
- **Type:** create/modify
- **Layer:** Persistence
- **Files:** new `src/Explore.Persistence/Configurations/Entities/{EventPublicActionConfiguration,EventOrganizerClaimConfiguration,EventProvenanceTypeConfiguration,EventPublicActionKindConfiguration,EventPublicActionHealthStateConfiguration,EventOrganizerClaimStatusConfiguration}.cs`; existing `ExploreDbContext.DbSets.cs`, `ExploreDbContext.QueryFilters.cs`, `Seed/LookupTableSeeder.cs`, `schemas/islamu-event.md`; repository contracts/implementations move to Task 1.5 when real access paths exist.
- **Description:** Tenant + soft-delete filters, stable lookup IDs in the seeder (document ID ranges in `schemas/islamu-event.md`), and a provider-neutral non-unique `(TenantId, EventId)` index. The one-active-primary rule belongs to the existing serializable Application/EF unit-of-work boundary, not a filtered index, trigger, stored procedure, or provider-specific SQL. Do not add speculative repositories before Application consumers exist. The reset main Explore chain is generated as `20260731204537_init`; never hand-edit its migration, designer, or snapshot. This proves model/artifact parity, not database application or runtime rollout.
- **Acceptance Criteria:**
  - [x] Lookup seeder parity (enum ↔ seeded rows) covered by exact/idempotent missing-row repair tests
  - [x] Named Tenant and SoftDelete filters verified for fail-closed, cross-tenant, and independent-filter behavior
  - [x] Concurrent primary-action commands yield one committed primary through `IUnitOfWork.ExecuteSerializableAsync`, with no Phase 1 provider-specific business-rule DDL
  - [x] Generated `20260731204537_init` matches the model; PrivacyErasureAuthority/DataProtection chains are unchanged; database application remains unclaimed
- **Dependencies:** 1.1–1.3; migration generation additionally depends on `atproto-federation-actor-lifecycle` Task 2.1 because both workstreams modify the shared Event/Actor model snapshot
- **Effort:** L

#### Task 1.5: Application features — public actions, claims, provenance exposure
- **Type:** create/modify
- **Layer:** Application
- **Files:** new `src/Explore.Application/Features/EventPublicActions/{Requests,Handlers}/**`, `Features/EventOrganizerClaims/{Requests,Handlers}/**`; existing `Features/Events/**` (event DTOs gain provenance + actions), `src/Explore.Application/DTOs/**`, AutoMapper profiles, validators (manual instantiation)
- **Description:** Commands: manage actions (organizer/curator), submit/withdraw claim, review claim (curator). Correction suggestions and unsafe-link reports reuse the existing hardened `SubmitEventReportCommand` intake with stable `event_correction_suggestion` / `unsafe_external_link` subcategory codes rather than duplicating report persistence. Queries: actions by event, claims by event/claimant. Event DTOs expose normalized lookups (`ProvenanceTypeId/Code/Name`) and the ordered action collection with semantic labels; **no** capability booleans. `BaseCommandResponse<Guid>` conventions; `IUnitOfWork` where multi-write (claim approval writes claim + event).
- **Acceptance Criteria:**
  - [ ] Contributor cannot invoke registration/ticket/attendee features (authorization action constants added; fail-closed handler checks)
  - [ ] Claim approval command is transactional and idempotent under retry
- **Dependencies:** 1.4
- **Effort:** L

#### Task 1.6: API + Cerbos + HAL for provenance/claims/actions
- **Type:** create/modify
- **Layer:** API
- **Files:** new `src/Explore.API/Controllers/EventPublicActionController.cs`, `EventOrganizerClaimController.cs`, new `src/Explore.API/Hateoas/Policies/{EventPublicActionLinkPolicy,EventOrganizerClaimLinkPolicy}.cs`; existing `EventLinkPolicy.cs`, `RouteNames.cs`, `LinkRelations.cs`, `cerbos/policies/islamuevent_event.yaml`, new `cerbos/policies/islamuevent_event_organizer_claim.yaml` + `_schemas` entry
- **Description:** Named routes + classifications (`Public` reads, `Authenticated` writes), ProducesResponseType coverage, HAL relations `view-original-source`, `external-event-page`, `external-registration`, `optional-questionnaire`, `claim-event`, `suggest-correction`, `report-external-link`; Cerbos attributes per D9; redirect endpoint resolves stored action IDs only (no `?url=`); `noopener/noreferrer` guidance in DTO metadata. Regenerate OpenAPI + NSwag client as the discrete contract step; update `docs/API_CHANGELOG.md`.
- **Acceptance Criteria:**
  - [x] Contract invariants + endpoint-classification architecture tests pass on focused current-source selectors
  - [x] Cerbos service/fallback parity tests cover claim review and machine/unrelated deny-by-default behavior; native CLI compilation remains an environment gap
  - [x] No open-redirect endpoint exists; non-test in-process HTTP proves hostile caller URL input cannot override the stored validated destination
- **Dependencies:** 1.5
- **Effort:** L

#### Task 1.7: Blazor — community badge, provenance panel, claim/correction flows
- **Type:** create/modify
- **Layer:** Blazor
- **Files:** existing `src/Explore.Blazor.Client/Pages/Events/**` (detail + card components), new `Components/Events/EventProvenancePanel.razor` (+ `.razor.css` BEM), new claim submission dialog components
- **Description:** Card badge "Community reported" + detail panel (source domain, last-checked date, suggest-correction, claim, report-unsafe-link) rendered **only** from DTO/HAL data; all mutating affordances gated by `_links`; accessible labels; RTL-safe; external links marked with domain disclosure.
- **Acceptance Criteria:**
  - [ ] Badge cannot be hidden by contributor-controlled data (derives from provenance lookup code)
  - [ ] bUnit tests: affordances absent without links; badge present for `COMMUNITY_REPORTED`
- **Dependencies:** 1.6
- **Effort:** M

---

### Phase 2: Typed Participation Configuration And HAL Participation Actions
- **Status:** Tasks 2.1 through 2.5 have checked implementation boxes. Commit `ff30795a2` supplies the participation/ticketing migration artifact. Corrected verification has executed; broad API remains non-green from environment/shared failures, and database application/runtime rollout remains unproved.
- **Goal:** Replace `IsRegistrationRequired` with the typed participation model, make the API author every participation CTA (zero-action events valid, external-managed first-class), and make Studio Registration the canonical organizer configuration surface.
- **Depends on:** Phase 1.
- **Relevant files:** existing — `src/Explore.Domain/Event.cs`, `Features/Events/**`, `EventLinkPolicy.cs`, seeder, Cerbos event policy, Blazor event detail, `src/Explore.Blazor.Client/Pages/Studio/StudioEventShell.razor`, `Components/Shell/Workspaces/StudioEventNavigation.razor`, `Routes.razor`; new — `src/Explore.Domain/EventParticipationConfiguration.cs`, `ParticipationHandlingMode.cs` + enum, `AdvanceRegistrationObligation.cs` + enum, `IdentityAccessMode.cs` + enum, configuration/feature/policy files per task.
- **Related skills/rules:** as Phase 1.
- **Acceptance criteria:** FR-ACT-01…07 satisfied; §5 scenario table representable; labels semantically accurate; no completion inference from clicks; `IsRegistrationRequired` deleted.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** If event-creation flows in Blazor break on the new required configuration, ship the organizer configuration UI inside this phase (Task 2.4) before deleting the boolean — deletion is the last commit of the phase.

#### Task 2.1: `EventParticipationConfiguration` + three mode lookups
- **Status:** Source complete; focused Domain checks pass 53/53.
- **Type:** create/modify
- **Layer:** Domain
- **Files:** new domain files above; existing `src/Explore.Domain/Event.cs` (delete `IsRegistrationRequired`), extend `Services/Registration/EventAuthorityRules.cs`
- **Description:** 1:1 entity with the three lookup FKs + guest-recovery policy fields (verified-email-required / unverified-accepted / email-optional / capability-link-only / no-recovery) + `ConcurrencyStamp`; domain rules validating legal combinations (e.g., `EXTERNAL_MANAGED` forbids native workflow attachment; `INFORMATION_ONLY` forbids registration actions) with unit tests for the §5 scenario table.
- **Acceptance Criteria:**
  - [x] All ten §5 scenarios constructible; illegal combinations rejected with typed errors
  - [x] `GuestRecoveryPolicyEnum` remains an OpenAPI and generated-client string enum with the exact five public literals
- **Dependencies:** Phase 1 complete
- **Effort:** M

#### Task 2.2: Persistence + seeding for participation lookups
- **Status:** Source and committed migration artifact exist. Corrected verification has executed, EF reports no pending model changes after the additive legacy-pricing migration, and database application/runtime rollout remains unproved.
- **Type:** create/modify
- **Layer:** Persistence
- **Files:** new configurations for the four entities; existing DbSets/QueryFilters/LookupTableSeeder; additive migration per 0.4 gate
- **Acceptance Criteria:**
  - [x] Stable IDs documented; seeder parity green
  - [x] Commit `ff30795a2` owns `20260728152646_AddParticipationHandlingModes.cs`, its designer, and the snapshot
  - [ ] Database application/runtime rollout is evidenced separately
- **Dependencies:** 2.1
- **Effort:** M

#### Task 2.3: Application + API — configure-participation and action synthesis
- **Status:** Source and generated contracts complete; focused Application 3/3, API/HAL 15/15, and API inventory 1/1 checks pass.
- **Type:** create/modify
- **Layer:** Application + API
- **Files:** new `Features/EventParticipation/{Requests,Handlers}/**`; existing event detail queries, `EventLinkPolicy.cs`, `RouteNames.cs`, `LinkRelations.cs`
- **Description:** `configure-participation` requires `ManageRegistrations`. Verified organizer controllers are resolved from `OrganizerActor`; explicitly assigned event roles require `EventRegistrationManage`. Community reporters receive no implicit `EventOwner`. The event public read model emits `start-registration`, `sign-in-to-register`, or `external-registration` according to participation mode and authentication state. `EventDto.PublicActions` and ATProto registration URIs are both filtered through `EventAuthorityRules`. HAL relation `configure-participation` is management-only. Generated contract and changelog are current.
- **Acceptance Criteria:**
  - [x] API integration tests: per-mode link emission matrix (information-only: none; external-managed: external only; platform-managed: native)
  - [x] External labels: "View original event page" pre-verification vs "Register on organizer website" post-verification
  - [x] Authorization tests allow OrganizerActor controllers or explicit `EventRegistrationManage` assignment and deny community reporters, unrelated controllers, tenant admins, instance admins, and machines
- **Dependencies:** 2.2
- **Effort:** L

#### Task 2.4: Blazor — Studio participation configuration + public CTA rendering
- **Status:** Source complete and `Explore.Blazor.Client` Release builds. bUnit execution remains blocked by externally owned compiler errors.
- **Type:** create/modify
- **Layer:** Blazor
- **Files:** existing `Pages/Events/Components/EventRegistration.razor` (public CTA refactor), `Pages/Studio/StudioEventShell.razor`, `Components/Shell/Workspaces/StudioEventNavigation.razor`, `Routes.razor`; new Studio-owned `Components/Studio/ParticipationConfigurationEditor.razor` (+ scoped CSS)
- **Description:** Organizer editor lives at `/studio/events/{eventId}/registration` and both the page and sidebar entry require the event's `configure-participation` link. Public detail renders CTAs strictly from `_links`/action DTOs with accurate labels; zero-action events show only platform actions (save/share/calendar/report); outbound external clicks are recorded via the Phase 2 aggregate engagement endpoint (no identity). Replace the current legacy `registration|registrations` sidebar predicate with the management-specific relation; attendee `start-registration` links never authorize Studio.
- **Acceptance Criteria:**
  - [x] Source/contract: no public registration CTA without `start-registration`, `sign-in-to-register`, or `external-registration`; Studio Registration is absent without `configure-participation`; external CTA never claims ISLAMU registration
  - [ ] bUnit execution after the 17 externally owned `ProgramSectionsDialogTests.cs` compiler errors are fixed outside this workstream
- **Dependencies:** 2.3
- **Effort:** M

#### Task 2.5: Aggregate outbound-engagement counter
- **Status:** Source complete; focused metric checks pass 2/2.
- **Type:** create
- **Layer:** Application + API
- **Files:** new `Features/EventPublicActions/Requests/Commands/RecordActionEngagementCommand.cs` + handler; new metrics in `src/Explore.Application/Telemetry/BusinessMetrics.cs` (existing file)
- **Description:** Anonymous, aggregate-only engagement recording (`EventId`, `ActionId`, `OccurredAt`, surface, outcome) with bounded metric labels (`action_kind`, `outcome`); no user identity, no per-user rows; a click is never called a registration anywhere (metric names reviewed).
- **Acceptance Criteria:**
  - [x] Metric labels bounded; no tenant/event IDs in labels; focused tests pass
- **Dependencies:** 2.3
- **Effort:** S

---

### Phase 3: Guest Transaction Security Foundation (`PublicTransactional`)
- **Status:** Tasks 3.1 through 3.3 are source complete and verification has executed. The canonical build has 0 errors and 5,162 worktree-wide warnings; Architecture is 330/340 passed, 9 unrelated failed, 1 skipped, so the phase gate is not globally green. The Phase 2 migration artifact exists in commit `ff30795a2`, but database application is not evidenced. No Phase 5 product endpoint/persistence work is complete.
- **Goal:** Introduce the governed anonymous-mutation endpoint class and the reusable guest capability-token primitives before any guest-facing endpoint exists.
- **Depends on:** Phase 0 (ADR-017); independent of Phases 1–2 code but sequenced after them to keep one schema stream.
- **Relevant files:** `src/Explore.API/Attributes/EndpointClass.cs`, `EndpointClassificationAttribute.cs`, `src/Explore.API/OpenApi/EndpointClassificationTransformer.cs`, `src/Explore.API/Extensions/RateLimitingExtensions.cs` (policy), `src/Explore.API/Program.cs` (middleware order), `src/Explore.Application/Contracts/Services/IGuestCapabilityTokenService.cs` (contract), `src/Explore.Infrastructure/Services/GuestCapabilityTokenService.cs` (implementation), `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs` (DI), `src/Explore.Domain/ValueObjects/CapabilityTokenHash.cs`, `tests/Event.Architecture.Tests/EndpointClassificationArchitectureTests.cs`, `docs/GOVERNANCE.md`, `docs/QUICK_REFERENCE.md`, `docs/SECURITY-MODEL.md`.
- **Related skills/rules:** `auth-patterns`, `blazor-bff-patterns`, `api-controllers.md`, `tests.md`.
- **Acceptance criteria:** FR-GUEST-04/06 foundations; the class exists with enforced invariants (rate policy, antiforgery, idempotency, classification) and zero endpoints yet; governance docs updated; `Public` semantics untouched.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` *(repeat justified: the classification governance tests extended here live in this project)*
- **Rollback / failure handling:** Foundation-only phase; nothing consumes it until Phase 5. Revert cleanly if the antiforgery investigation (3.2) demands a different token transport.

#### Task 3.1: `EndpointClass.PublicTransactional` + enforcement
- **Status:** Source complete; focused PublicTransactional governance passes 6/6, implementation-pass endpoint classification passes 4/4, and idempotency passes 6/6.
- **Type:** modify/create
- **Layer:** API + tests
- **Files:** `src/Explore.API/Attributes/EndpointClass.cs` (existing), `EndpointClassificationTransformer.cs` (existing), `tests/Event.Architecture.Tests/EndpointClassificationArchitectureTests.cs` (existing), new `tests/Event.Architecture.Tests/PublicTransactionalGovernanceTests.cs`
- **Description:** Add enum value + XML doc; transformer emits `x-endpoint-class: PublicTransactional`; new architecture tests assert every `PublicTransactional` action carries `[AllowAnonymous]`, the `public_transactional` rate-limit policy, and idempotency requirement metadata for create/finalize verbs. Architecture tests forbid API antiforgery metadata on `PublicTransactional` actions because same-site browser writes are protected at the BFF proxy boundary; direct bearer/API-key traffic is not browser-antiforgery validated. Update `docs/GOVERNANCE.md` classification table + `docs/QUICK_REFERENCE.md` rules and rate-limit table.
- **Acceptance Criteria:**
  - [x] Governance enforces anonymous classification, `public_transactional`, required idempotency metadata/middleware, and the OpenAPI idempotency boolean
- **Dependencies:** ADR-017
- **Effort:** M

#### Task 3.2: `public_transactional` rate policy + antiforgery decision
- **Status:** Source complete; the policy is 10 requests per 60 seconds per IP and uses `NoLimiter` in `Testing`. Anonymous browser mutations use BFF proxy antiforgery, while direct API clients do not use the browser antiforgery pairing. BFF proxy checks pass 20/20.
- **Type:** create + investigate
- **Layer:** API
- **Files:** `src/Explore.API/Program.cs` (existing — rate limiter section only; middleware order preserved), `docs/SECURITY-MODEL.md`
- **Description:** Dedicated fixed-window IP+session policy (limits documented; disabled in `Testing` like others); investigate + document the BFF antiforgery path for anonymous browser mutations (cookie+token pairing through `Explore.Blazor` YARP) and record the decision; wire idempotency middleware applicability to the new class.
- **Acceptance Criteria:**
  - [x] Policy registered and documented; `Testing` uses `NoLimiter`
  - [x] Anonymous BFF proxy antiforgery and direct API distinction recorded in context
- **Dependencies:** 3.1
- **Effort:** M

#### Task 3.3: Guest capability-token primitives
- **Status:** Source complete; Domain capability checks pass 3/3 and Infrastructure capability checks pass 5/5.
- **Type:** create
- **Layer:** Application (contract) + Infrastructure (impl)
- **Files:** new `src/Explore.Application/Contracts/Services/IGuestCapabilityTokenService.cs`, new `src/Explore.Infrastructure/Services/GuestCapabilityTokenService.cs`, new `src/Explore.Domain/ValueObjects/CapabilityTokenHash.cs`, registration in `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs` (existing)
- **Description:** High-entropy (≥256-bit) token generation, constant-time hash comparison, storage of hash only, scoping payload (order id + purpose), expiry/rotation policy hooks; explicit "token ≠ identity proof" doc comment; unit tests incl. timing-safe comparison and non-guessability (format).
- **Acceptance Criteria:**
  - [x] 256-bit token primitives reveal plaintext once, retain hashes only, and compare hashes in constant time
- **Dependencies:** 3.1
- **Effort:** M

#### Phase 3 historical verification checkpoint (superseded by the current corrected matrix)
- The focused PublicTransactional governance, endpoint-classification implementation pass, idempotency, BFF proxy, Domain capability, and Infrastructure capability checks pass 6/6, 4/4, 6/6, 20/20, 3/3, and 5/5 respectively.
- Oracle follow-up result: **PASS**, with no Critical or High issues. The governance metadata-bypass and secret-formatting findings were fixed.
- At that checkpoint, the new rate-policy test stopped before discovery on six unrelated `CustomPropertyDefinitionControllerTests` errors caused by missing DTO members.
- At that checkpoint, the canonical Release build reported 12 unrelated errors: six in that API test and six in Blazor client custom-property generated-contract call sites.
- At that checkpoint, full Architecture executed 315 tests: 304 passed, 10 unrelated tests failed, and 1 was skipped. The new `PublicTransactional` checks weren't among the failures.
- **Superseded next slice:** Phase 4 later received review findings and source corrections. The current corrected verification matrix and direct final review supersede this checkpoint. Commit `ff30795a2` supersedes the earlier missing-migration claim.

---

### Phase 4: Ticket Catalog, Capacity Pools, Entitlements, And Instance Monetization
- **Status:** SOURCE COMPLETE / CORRECTED VERIFICATION EXECUTED / DOCKER RUNTIME PROOF BLOCKED. Tasks 4.1 through 4.5 are implemented, the final diff has no unresolved High or Major review finding, and owned focused lanes are green. Broad projects are not globally green, and the two PostgreSQL row-lock tests remain unavailable without Docker.
- **Goal:** Versioned admission products with five pricing modes (D17), shared capacity pools and session/day entitlements; Studio Ticketing authoring; instance-admin-only monetization configuration (D18); delete decorative prices.
- **Depends on:** Phases 1–2 (organizer authority + participation config).
- **Relevant files:** existing — `Event.cs`, `EventSession.cs` (Price removal), seeder, DbContext partials, event manage API, `Routes.razor`, `StudioEventNavigation.razor`, `StudioEventShell.razor`; new — `src/Explore.Domain/EventTicketCatalogVersion.cs`, `EventTicketType.cs`, `TicketTypeEntitlement.cs`, `EventCapacityPool.cs`, `PlatformFeePolicy.cs`, `PlatformContributionSetting.cs`, `PlatformContributionOption.cs`, lookups `TicketCatalogStatus`, `TicketPricingMode`, `ParticipantDataCollectionMode`, `EntitlementScopeType`, `EntitlementSelectionRule`, `CapacityOversellPolicy` (+ enums), `Services/Registration/TicketPricingRules.cs`, configurations, `Features/EventTicketing/**`, `Features/PlatformMonetization/**`, `EventTicketTypeController.cs` etc., HAL policies, Cerbos `islamuevent_event_ticket_type.yaml`, Studio ticketing page/components.
- **Related skills/rules:** as Phase 1 + `efcore-migrations.md`.
- **Acceptance criteria:** FR-TICKET-01/02/08/09/13 (authoring side); published versions immutable (edit → new version, domain-enforced); all five D17 pricing modes authorable with mode-specific validation; new platform-managed Events create no bootstrap ticket catalog; Studio creates an explicit currency-selected draft that supports all five pricing modes; no `XXX` bootstrap catalog; one currency per active catalog enforced; API and persistence accept integer minor-unit amounts without claiming decimal-major or FX conversion; hidden/cross-event ticket identifiers produce generic not-found (report §11.2); monetization settings instance-admin-only and default off/zero; `Event.Price`/`CurrencyCode`/`EventSession.Price` deleted with display price derived from the published catalog.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Price deletion is the phase's final task; all read models (list/detail/SEO/AT Proto lexicon projections if any reference price — enumerate via `rg "\.Price" src/`) must compile against derived display price before deletion commits.

#### Phase 4 verification evidence
- Oracle session `ses_052476c4cffeqCboMjdwpnB2xM` initially returned **FAIL**.
- Corrected mechanisms: legacy Event/EventSession prices removed; price summary sourced from the published catalog; no `XXX` bootstrap catalog; published plus draft/management pool reference guard; non-public ticket child mutators; EF conflict translation bounded to concurrency and named published-catalog/capacity-pool unique constraints; Studio editor request cancellation.
- Release build: 0 errors and 5,162 worktree-wide warnings.
- Current focused evidence: pricing 19 + 10 + 11 + 19 + 13 + 36; pool 4 + 13 + 5 architecture; persistence 11 + 2 architecture; Studio 12 + 14 + 76.
- Corrected project execution: Domain 602/602; Application 3,374/3,377 after fixing two stale removed-price AI assertions, with exactly three unrelated failures; Architecture 330/340 passed, 9 failed, 1 skipped; Secrets 205/205; Infrastructure non-runtime 1,151/1,151; Persistence 96/698 passed and 602 environment/provider-heavy failures, with focused ticketing 11/11; API 1,571/2,114 passed and 543 environment/shared failures, with all seven focused ticketing/monetization classes 23/23; Blazor Integration 398/398; Blazor Client 2,250/2,252 passed, 1 unrelated failure, 1 skipped.
- The three unrelated Application failures are `PublishEventCommandHandlerTests.Handle_WithEnabledAtproto_StagesEventOutboxAfterLocalSaveInsideTransactionWithoutPdsCall`, `UpdateOrganizationCommandHandlerTests.Handle_WhenRequesterIsNotOrgAdmin_ReturnsAuthorizationFailureAndDoesNotSave`, and `EventLocationDisclosureContractTests.Contracts_AreImmutableRecordsAndDoNotReuseGenericLocationDto`. The Blazor Client failure is `LaunchAccessibilitySourceTests.LaunchCriticalPages_ShouldPreserveAccessibilityContracts`.
- Generated additive `src/Explore.Persistence/Migrations/20260729183118_RemoveLegacyEventPricing.cs`, its designer, and the snapshot drop the two nonnegative-price checks and Event/EventSession `price`/`currency_code`. `dotnet ef migrations has-pending-model-changes` reports no changes. Database application remains unproved.
- Verification execution and direct final review are complete, but the broad matrix is not globally green and the Docker-backed row-lock proof remains unavailable.
- Phase 5 remains blocked on accepting or executing the PostgreSQL row-lock proof; Task 5.4 atomic idempotency remains a separate prerequisite.

#### Task 4.1: Catalog domain model with immutable publication and five pricing modes
- **Status:** Source complete; Domain 602/602 passes.
- **Type:** create
- **Layer:** Domain
- **Files:** new entities/lookups listed above; new `Services/Registration/TicketCatalogRules.cs`; new `Services/Registration/TicketPricingRules.cs`; new `src/Explore.Domain/TicketPricingMode.cs` + `Enums/TicketPricingModeEnum.cs`
- **Description:** Catalog version states `Draft/Published/Retired`; publication freezes; mutation of published members throws; new-version cloning; eligibility typed fields (`MinimumAge`, `MaximumAge`, `RequiresGuardian`, `RequiresApproval`); quantity-limit fields (`PerOrder/PerAccount/PerVerifiedContact/PerBookingParty`); entitlements per §15 with selection rules `AllIncluded/FixedSelection/ChooseOne/ChooseUpToN`; one-currency rule across a version. Pricing per D17: `TicketPricingModeId` (`FIXED/FREE/DONATION/PAY_WHAT_YOU_CAN/SLIDING_SCALE`) with `FixedPriceMinor`, `MinimumPriceMinor`, and `SuggestedPriceMinor`; `TicketCatalogRules` enforces per-mode field consistency (FIXED requires price; FREE forbids amounts; DONATION/PWYC allow minimum ≥ 0 with 0-input semantics; SLIDING_SCALE requires minimum + suggested ≥ minimum). The contract accepts already-normalized integer minor units and defines neither decimal-major conversion nor foreign exchange.
- **Acceptance Criteria:**
  - [x] Domain unit tests: publish-freeze, clone-to-draft, currency uniformity, entitlement legality vs Event/Day/Session references
  - [x] Pricing-mode validation matrix unit-tested (each of the five modes × valid/invalid/boundary amounts, incl. 0-allowed cases)
  - [x] Persisted/API money uses `long ...Minor`, percentages use integer basis points (`10_000 = 100%`), and the shipped model accepts already-normalized minor units without decimal-major or FX conversion
- **Dependencies:** Phase 2
- **Effort:** L

#### Task 4.2: Persistence + seeding
- **Status:** Source complete; focused ticketing Persistence passes 11/11. Broad Persistence is 96/698 passed with 602 environment/provider-heavy failures, so the project is not globally green.
- **Type:** create/modify
- **Layer:** Persistence
- **Files:** new configurations + repositories (`IEventTicketCatalogRepository` etc.); existing DbSets/QueryFilters/seeder; additive migration per gate
- **Description:** Filtered unique indexes (one active published catalog per event), FK cascades reviewed (no cascade delete into published versions), stable lookup IDs, integration tests for immutability at DB level (concurrency stamp) and tenant filters.
- **Acceptance Criteria:**
  - [x] Persistence tests: published version rows unmodifiable via optimistic concurrency; pool shared by two ticket types resolves single capacity row
  - [x] Hidden/cross-tenant/cross-event ticket-type lookups return generic not-found (report §7.9/§11.2 lesson)
- **Dependencies:** 4.1
- **Effort:** L

#### Task 4.3: Authoring Application + API + Cerbos + HAL
- **Status:** Source complete; all seven focused ticketing/monetization API classes pass 23/23. Broad Application and API remain non-green only for the current unrelated/environment results recorded above.
- **Type:** create
- **Layer:** Application + API
- **Files:** new `Features/EventTicketing/**` (catalog/ticket/pool/entitlement commands+queries, DTOs, validators), new controllers + link policies, `RouteNames`/`LinkRelations` additions (`manage-ticket-types`, `manage-capacity-pools`), new Cerbos policy + schema
- **Description:** Organizer-only writes (verified organizer authority via Phase 1 rules — community contributor forbidden test); publish command runs one-currency + entitlement + pricing-mode preflight (`TicketPricingRules`); public event read model derives display price ("from X" across active types; "Free / pay what you can" labels for buyer-priced modes). Event HAL emits `manage-ticket-types` and/or `manage-capacity-pools` only when the corresponding Studio Ticketing operations are authorized. Contract regeneration + changelog.
- **Acceptance Criteria:**
  - [x] Contributor-denied and curator-delegation provider parity tests
  - [x] Publish preflight rejects mixed currencies, orphan entitlements, and pricing-mode field inconsistencies
  - [x] Event HAL omits both Ticketing relations for community contributors and external-managed/listing-only events
- **Dependencies:** 4.2
- **Effort:** L

#### Task 4.4: Studio ticket authoring + price display migration + field deletion
- **Status:** Source complete; Studio focused lanes pass 12 + 14 + 76. Broad Blazor Client is 2,250/2,252 passed with one unrelated failure and one skip; browser visual QA is not claimed.
- **Type:** create/modify/delete
- **Layer:** Blazor + Domain
- **Files:** new `Pages/Studio/StudioEventTicketing.razor` (+ scoped CSS and child editors under `Components/Studio/Ticketing/`); modify `Routes.razor`, `StudioEventNavigation.razor`, existing event display components; final commit deletes `Event.Price`, `Event.CurrencyCode`, `EventSession.Price` and updates every reader
- **Description:** Add `/studio/events/{eventId}/tickets`. `StudioEventNavigation` renders Ticketing when either `manage-ticket-types` or `manage-capacity-pools` exists and reuses the shared `StudioEventContextState`; the page renders only controls authorized by their exact relations. The catalog editor covers types, pools, entitlements, windows, limits, pricing modes, and shared-capacity visualization per the Hi.Events organizer lesson. Public price display comes from the derived DTO; delete decorative price fields last.
- **Acceptance Criteria:**
  - [x] Legacy decorative Event/EventSession price members are removed; ticket catalog minor-unit fields own ticket pricing
  - [x] bUnit: Ticketing navigation/route uses `manage-ticket-types OR manage-capacity-pools`; ticket and pool controls are independently gated by their exact relations; catalog item mutations use item HAL links
- **Dependencies:** 4.3
- **Effort:** L

#### Task 4.5: Instance monetization configuration (fee policy + platform contribution)
- **Status:** Corrected source, completed verification evidence, and direct final review are recorded. The UI lives at `/settings/instance`; the API is the separate Admin-class `GET|PUT /api/instance/settings/platform-monetization` resource. The remaining Phase 4 evidence gap is the Docker-backed PostgreSQL row-lock proof.
- **Type:** create
- **Layer:** Domain + Persistence + Application + API + Blazor
- **Files:** new `src/Explore.Domain/PlatformFeePolicy.cs`, `PlatformContributionSetting.cs`, `PlatformContributionOption.cs` (typed option rows: percentage + sort order — no JSON blob); new configurations + DbSets; new `Features/PlatformMonetization/{Requests,Handlers}/**`; new Admin controller actions (`EndpointClass.Admin`, instance-admin authorization) + `RouteNames`; new Blazor instance-admin settings page; `docs/CONFIGURATION.md` + `docs/ADMIN_GUIDE.md` sections
- **Description:** Per D18. Both concepts instance-scoped, versioned (edits create a new version so order snapshots stay deterministic), and **default off/zero**. `PlatformFeePolicy`: percentage + fixed components used only for organizer-earnings computation/display until payments ship. `PlatformContributionSetting`: enable flag; DB-stored heading + body text (localization-ready, example seeded as documentation not data: "Help us help the Ummah…"); option list seeded `0 (default, preselected), 5, 10, 15, 20` percent — fully editable by the instance admin. Authorization: instance administrators only; tenant admins must have no read-write management path (fail-closed tests). No hardcoded strings, percentages, or amounts anywhere in API or client.
- **Acceptance Criteria:**
  - [x] Query and command handlers independently recheck instance-admin authority; tenant-scoped roles have no management path
  - [x] Defaults are off/zero on a fresh instance; enabling is explicit and edits create new active revisions
  - [x] Heading/body/basis-point options round-trip through the separate API resource; platform contribution remains separate from organizer earnings
- **Dependencies:** 4.1
- **Effort:** L

---

### Phase 5: Registration Orders, Inventory Holds, And Guest Checkout Core
- **Goal:** Replace the user-centric aggregate with the order aggregate; atomic capacity holds; free-order confirmation; `AwaitingPayment` boundary; guest capability access; Studio order operations at event and actor scope.
- **Depends on:** Phases 3, 4.
- **Relevant files:** existing (to delete/rewire) — `src/Explore.Domain/EventRegistrationIntent.cs` (delete), `EventRegistration.cs` (rewire), `EventContactShareConsent.cs` (FK rewire), `Features/EventRegistrations/**` (replace), `EventRegistrationController.cs` (replace), `EventRegistrationLinkPolicy.cs` (replace), `cerbos/policies/islamuevent_event_registration.yaml` (evolve), Blazor `EventRegistration.razor`/`EventListRegistrationWorkflow.cs` (replace), Studio routes/navigation; new — `RegistrationOrder.cs`, `RegistrationOrderPii.cs`, `RegistrationOrderLine.cs`, `RegistrationInventoryHold.cs`, `RegistrationOrderPlatformContribution.cs`, `RegistrationOrderStatus` + `BookingPartyType` + `RegistrationInventoryHoldStatus` + `CapacityHoldPolicy` lookups (+ enums), `Services/Registration/RegistrationOrderRules.cs`, `Contracts/Services/IOrganizerEarningsCalculator.cs` + implementation, features `Features/RegistrationOrders/**`, controllers `RegistrationOrderController.cs` (+ guest surface), `StudioContextDto` + query/controller/assembler, background `src/Explore.API/BackgroundServices/InventoryHoldExpiryWorker.cs`, Studio order pages/service.
- **Related skills/rules:** all path rules; `outbox-pattern` for post-commit notifications.
- **Acceptance criteria:** FR-TICKET-03/12/14, FR-GUEST-01…07, NFR-03/04; §31.5 core matrix rows (concurrent last-seat, expired-hold release, snapshot protection, free-order confirm, paid order stops at `AwaitingPayment`); buyer-chosen prices (D17) validated server-side and snapshotted; platform contribution (D18) composed only when instance-enabled; old aggregate deleted; capability-token order access with generic 404 on cross-tenant/guessed IDs; Hi.Events §7-derived criteria (deterministic pool locking, conditional completion transition, per-line allocation) all enforced.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` *(repeat justified: hold/capacity race-safety and new uniqueness constraints are database-level guarantees)*
- **Rollback / failure handling:** This is the highest-risk slice. Sequence inside the phase: (a) new aggregate lands green alongside old, (b) new endpoints/UX land, (c) old aggregate + endpoints deleted in the final commits. If (c) uncovers unexpected dependents (AT Proto records, notifications, email templates), finish rewiring within the phase — no dual-write, no shim; record discoveries in context.

#### Task 5.1: Order aggregate + status machine + PII separation
- **Type:** create
- **Layer:** Domain
- **Files:** new entities/lookups above; `Services/Registration/RegistrationOrderRules.cs`
- **Description:** Per consultation §16/§21: nullable `AccountUserId`, `PurchaserActorId`, `BookingPartyType`, pinned `TicketCatalogVersionId`/`ParticipationConfigurationVersion`/`RegistrationWorkflowVersionId` (nullable until Phase 7), `GuestAccessTokenHash`, timestamps, `ConcurrencyStamp`; `RegistrationOrderPii` separate entity (contact name/email/normalized email/phone/org name, minimal-by-policy); status transitions as pure rules class incl. free path (`Draft→AwaitingRequirements→ReadyForCheckout→Confirmed`), paid boundary (`→AwaitingPayment` terminal for this workstream), approval path, expiry.
- **Acceptance Criteria:**
  - [ ] Transition table unit-tested exhaustively (illegal transitions throw)
  - [ ] No PII fields on `RegistrationOrder` itself
- **Dependencies:** Phases 3, 4
- **Effort:** L

#### Task 5.2: Order lines with snapshots and buyer-chosen prices
- **Type:** create
- **Layer:** Domain + Persistence
- **Files:** new `RegistrationOrderLine.cs` + configuration
- **Description:** `TicketTypeId`, `Quantity`, `UnitPriceAmountSnapshot`, `ChosenUnitPriceAmountSnapshot` (buyer-priced modes, D17), `CurrencyCodeSnapshot`, `LineSubtotalSnapshot`, `TicketTypeNameSnapshot`, `TicketPricingModeSnapshot`, `TicketCatalogVersionId`, `PlatformFeePolicyVersionSnapshot` (nullable; set when a non-zero fee policy is in effect, D18); snapshot invariants (later catalog edits never mutate lines — persistence test); chosen prices validated by `TicketPricingRules` against the **pinned** catalog version's bounds, never against the current catalog (Hi.Events lesson: order lines don't pin a revision — report §4.5); explicit rounding at line-subtotal computation.
- **Acceptance Criteria:**
  - [ ] Price-revision test: new catalog version leaves existing lines byte-identical
  - [ ] Chosen price below the pinned minimum is rejected server-side regardless of client payload; 0-amount donation/PWYC line accepted when the pinned minimum is 0
- **Dependencies:** 5.1
- **Effort:** M

#### Task 5.3: Atomic hold reservation + expiry sweeper
- **Type:** create
- **Layer:** Persistence + Application + API background service
- **Files:** new `RegistrationInventoryHold.cs` + configuration; new repository methods with explicit locking (`IRegistrationInventoryRepository`); new `Features/RegistrationOrders/Handlers/Commands/CreateOrderWithHoldCommandHandler.cs`; new `src/Explore.API/BackgroundServices/InventoryHoldExpiryWorker.cs`
- **Description:** One transaction: validate catalog version → enforce per-order/account/verified-contact limits → check type+pool capacity under row locks → create order+lines → reserve → persist holds (per §20.2, via `IUnitOfWork`; IDs precomputed; zero external IO). **Every affected `EventCapacityPool` row is locked in deterministic order and active holds are recounted inside the transaction** — the Hi.Events shared-capacity race (validation outside its event advisory lock, report §7.1) is the named counter-example; a coarse event-level lock is explicitly rejected. Hold policies per D11; reservation-before-PII sequencing (report §9.1.1): the hold exists before any buyer/participant data is collected. Worker releases expired holds with fenced claims and fresh scopes per item (webhook-worker pattern); expired/released/consumed transitions are conditional and idempotent; an order whose hold expired before finalization gets a defined recovery path (re-reserve or waitlist), never silent oversell.
- **Acceptance Criteria:**
  - [ ] Real-PostgreSQL race test: two concurrent orders for **different ticket types sharing one pool** cannot jointly exceed the pool (report §11.2 holds criteria — not mock-based)
  - [ ] Expired hold returns capacity (worker test); waitlist-when-full path creates `Waitlisted` order; expiry-vs-finalization overlap resolves via the defined recovery path
- **Dependencies:** 5.2
- **Effort:** XL

#### Task 5.4: Guest order flow (PublicTransactional endpoints)
- **Type:** create
- **Layer:** API + Application
- **Files:** new `RegistrationOrderController.cs` guest actions (`start-guest-registration`, get/continue/amend/cancel by capability token), consuming Phase 3 primitives
- **Description:** `PublicTransactional` classification; identity-access-mode enforcement (`ACCOUNT_REQUIRED` rejects anonymous start; `GUEST_ALLOWED`/`CAPABILITY_TOKEN_ALLOWED` accept per config); token issued once on order creation; email management link when email supplied; name-only path shows booking reference + loss warning; idempotency-key required on create/finalize; no account auto-creation. The Hi.Events public-order exposure defect (completed orders loadable by short ID without session verification, report §7.8/§7.9) is the named counter-example: display/public identifiers never authorize anything, and every guest lookup verifies the full tenant/event/order tuple.
- **Prerequisite gate:** Before the first Phase 5 `PublicTransactional` endpoint, replace the generic `IdempotencyMiddleware` `FindAsync` → execute → `SaveAsync` window with an atomic in-progress key claim or business-transaction-owned dedupe. Concurrent identical keys must not execute twice, and required claim-persistence failures must fail closed. This gate does not require a migration design now.
- **Acceptance Criteria:**
  - [ ] §31.3 matrix covered in API tests (anonymous rejected on account-required; token scoped to its order; guessed ID → generic 404; expired token fails safely; no silent account)
  - [ ] Display/public order identifiers grant zero access on every endpoint (explicit test); capability rotation invalidates the prior token; capability values never appear in logs (log-assertion test)
  - [ ] Atomic in-progress key claim or business-transaction-owned dedupe is implemented before endpoint exposure; concurrent identical keys execute once, and required claim-persistence failure fails closed
- **Dependencies:** 5.3, Phase 3, atomic idempotency prerequisite above
- **Effort:** L

#### Task 5.5: Authenticated order flow + finalization + outbox events
- **Type:** create
- **Layer:** Application + API
- **Files:** new commands (submit, finalize-free, cancel), HAL policy `RegistrationOrderLinkPolicy.cs` (new), `RouteNames`/`LinkRelations` additions
- **Description:** Finalization transaction: re-validate requirements placeholder (until Phase 8), consume holds via **conditional state transition** (`WHERE status = ...` / concurrency token — the Hi.Events duplicate-completion race, report §7.2, is the named counter-example), apply admission mode (`RegistrationMode`: open/approval/invite/closed) + waitlist, materialize `EventRegistration` rows from entitlements, write outbox events (`registration.confirmed` etc. via existing webhook ledger) — notifications strictly post-commit. Cancellation/release effects derive from **order lines and holds, never from participant/registration rows** (Hi.Events general-product inventory bug, report §7.6).
- **Acceptance Criteria:**
  - [ ] Duplicate finalize (idempotency) returns the original result; a concurrent second completion cannot create additional registrations, answers, or outbox rows (conditional-transition test)
  - [ ] Rollback leaves no partial rows; outbox row written in-transaction, delivered post-commit; cancellation releases every line's inventory including zero-participant lines
- **Dependencies:** 5.3
- **Effort:** L

#### Task 5.6: Rewire `EventRegistration` + delete `EventRegistrationIntent`
- **Type:** modify/delete
- **Layer:** Domain + Persistence + Application
- **Files:** `EventRegistration.cs` (existing — `RegistrationParticipantId` required FK once Phase 6 lands; interim: `RegistrationOrderId` + nullable participant), `EventRegistrationIntent.cs` (delete), `EventContactShareConsent.cs` (FK → `SourceRegistrationOrderId`), `Features/EventRegistrations/**` (queries rewritten order-centric; commands deleted), seeder/scope/policy lookups retained
- **Description:** Delete the intent aggregate and its handlers/controller routes; organizer registration views become order/participant queries; uniqueness moves to assignment-level; `RegistrationScope`/`EventRegistrationPolicy` remain as workflow vocabulary consumed by entitlements.
- **Acceptance Criteria:**
  - [ ] `rg "EventRegistrationIntent" src/ tests/ --glob '!src/Explore.Persistence/Migrations/**'` → zero runtime/current-model hits; immutable historical migration files remain untouched
  - [ ] Organizer list views function against orders
- **Dependencies:** 5.5
- **Effort:** L

#### Task 5.7: Order Cerbos/HAL + actor-level Studio context
- **Type:** create/modify
- **Layer:** API + Cerbos
- **Files:** evolve `cerbos/policies/islamuevent_event_registration.yaml` → order semantics or new `islamuevent_registration_order.yaml` + schema; HAL relations `start-registration`, `start-guest-registration`, `sign-in-to-register`, `view-registration-orders`; new `StudioContextDto`, Application query/handler, `StudioController.GetContext`, resource assembler/link policy
- **Description:** Server-side authorization on order visibility (buyer, linked account, organizer permission-gated), attendee surfaces hidden for external-managed events; parity tests. Add authenticated `GET /api/studio/context?actorId={optionalActorHint}` with `PrivateNoStore`: validate the optional actor through existing managed-actor authority, support the personal fallback, and return one HAL resource for actor-level operational navigation. Base Events remains existing UI; emit `view-registration-orders` only when the selected actor can operate at least one platform-managed event. Phase 6 extends the same resource with Attendees without changing the shell contract.
- **Acceptance Criteria:**
  - [ ] Organizer of external-managed event receives no order/attendee-management links (FR-PRIV-05)
  - [ ] Unauthorized actor hints fail closed; `StudioContextDto` contains no role booleans or tenant-wide event data
- **Dependencies:** 5.5
- **Effort:** M

#### Task 5.8: Blazor checkout + Studio order management UX
- **Type:** create/modify
- **Layer:** Blazor
- **Files:** attendee flow: replace `Pages/Events/Components/EventRegistration.razor` with `Pages/Registration/{TicketSelection,OrderDetails,OrderConfirmation,GuestOrderAccess}.razor`; organizer flow: new `Pages/Studio/StudioOrders.razor`, `StudioEventOrders.razor`, scoped `IStudioContextService`; modify `Routes.razor`, `StudioWorkspaceNavigation.razor`, `StudioEventNavigation.razor`; update `EventListRegistrationWorkflow.cs`
- **Description:** Checkout is structured around the **order state machine, not a linear form** (Hi.Events UX lesson, report §6.2): distinct surfaces/screens for reservation created / expiring / expired, details incomplete, ready to finalize, awaiting payment (boundary placeholder), completed, abandoned, cancelled, and recovery-required; hold expiry countdown with navigation-away warning and explicit abandon action; guest vs sign-in branch per identity-access mode. Pricing widgets follow D17/D18. Add actor `/studio/orders` and event `/studio/events/{eventId}/orders` views. Actor navigation reads only `StudioContextDto._links`; event navigation reads only the shared event `_links`. Both list/detail actions remain order-resource HAL-gated.
- **Acceptance Criteria:**
  - [ ] bUnit: attendee and Studio affordances by `_links` only; actor/event Orders sidebar links disappear when their source relation is absent; hold expiry visible; free order reaches confirmed state; recovery screens render per status
  - [ ] bUnit: sliding-scale sliders stay linked and honor the minimum; donation/PWYC input accepts 0 only when minimum is 0; contribution dropdown shows "percentage — computed amount" pairs from DTO data with 0 preselected
- **Dependencies:** 5.4, 5.5, 5.7, 5.10 (+ contract regeneration)
- **Effort:** XL

#### Task 5.9: AT Proto + notification dependents sweep
- **Type:** investigate/modify
- **Layer:** Application
- **Files:** `rg "EventRegistration" src/` dependents outside replaced features (AtprotoRecordId usage, email templates, notification handlers)
- **Description:** Bounded sweep rewiring every dependent to order/participant semantics; decision recorded for AT Proto (keep `AtprotoRecordId` nullable on `EventRegistration`, defer order federation).
- **Acceptance Criteria:**
  - [ ] Build green with zero references to deleted members; decisions in context
- **Dependencies:** 5.6
- **Effort:** M

#### Task 5.10: Platform-contribution order component + organizer-earnings transparency
- **Type:** create
- **Layer:** Domain + Application + API
- **Files:** new `src/Explore.Domain/RegistrationOrderPlatformContribution.cs` + configuration; new `src/Explore.Application/Contracts/Services/IOrganizerEarningsCalculator.cs` + `Services/Registration/OrganizerEarningsCalculator.cs` (pure, decimal-exact); order-totals composition in `Features/RegistrationOrders/**`; checkout-composition DTO additions
- **Description:** Per D18. When the instance's `PlatformContributionSetting` is enabled, checkout composition exposes the DB-stored heading/body plus the dropdown options — each option carries the percentage and the **server-computed** currency amount (percentage × organizer-directed order total), with `0` preselected. The buyer's selection persists as `RegistrationOrderPlatformContribution` (percentage, computed amount, setting-version snapshot). Contribution money is instance-operator-directed and is excluded from organizer totals, organizer earnings, capacity semantics, and organizer-facing exports everywhere. `IOrganizerEarningsCalculator` computes "Organizer earns" = organizer-directed line totals − platform fee (D18 policy snapshot), powering the sliding-scale slider and organizer price previews, with an "excluding payment-processing fees" disclaimer until the payment workstream defines processor costs. Totals rule: any positive total (lines + contribution) → `AwaitingPayment`; all-zero → free confirmation path; the contribution control is composed only when a payment path will exist for the order (rule enforced at composition, so free-only instances without payments never dead-end a buyer).
- **Acceptance Criteria:**
  - [ ] Contribution absent by default and entirely hidden when the instance setting is disabled; selection of 0 stores no contribution row
  - [ ] Dropdown amounts computed server-side and delivered via DTO — the client performs no authoritative money math
  - [ ] Organizer-earnings figure equals line totals minus fee policy exactly (decimal + rounding tests); contribution never leaks into organizer earnings, organizer totals, or organizer exports
- **Dependencies:** 4.5, 5.2
- **Effort:** L

---

### Phase 6: Participants, Group Bookings, And Data-Collection Modes
- **Goal:** Buyer ≠ participant: families, companies, deferred assignment, per-ticket data modes, per-booking-party limits, group amendments.
- **Depends on:** Phase 5.
- **Relevant files:** new — `RegistrationParticipant.cs`, `RegistrationParticipantPii.cs`, `RegistrationTicketAssignment.cs`, `ParticipantType` + `AssignmentStatus` lookups (+ enums), features/UI per task; existing — order features, `EventRegistration.cs` (participant FK becomes required).
- **Related skills/rules:** as Phase 5.
- **Acceptance criteria:** FR-TICKET-04/05/06/07/10; §17 family + company configurations; §31.5 participant rows (child-required/adult-optional, deferred company assignment, per-booking-party limit).
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Additive on the Phase 5 aggregate; assignment-uniqueness index changes are the risky part — verify in the persistence suite when it next runs (Phase 9) and record as pending evidence.

#### Task 6.1: Participant + PII + assignment domain model
- **Type:** create
- **Layer:** Domain
- **Files:** new entities/lookups above; extend `RegistrationOrderRules.cs`
- **Description:** Participant types `Adult/Child/Dependent/Employee/Guest/Unnamed`; guardian self-reference; PII split entity; assignment (`OrderLineId`, `ParticipantId?`, `Ordinal`, status incl. deferred); rules: `PER_TICKET_REQUIRED` blocks confirmation until assigned, `DEFERRED_ASSIGNMENT` sets deadline, guardian required by ticket eligibility.
- **Acceptance Criteria:**
  - [ ] Rules unit tests for all five `ParticipantDataCollectionMode` values
- **Dependencies:** Phase 5
- **Effort:** L

#### Task 6.2: Persistence + `EventRegistration` participant linkage
- **Type:** create/modify
- **Layer:** Persistence
- **Files:** new configurations/repositories; `EventRegistration.cs` — `RegistrationParticipantId` required, `LinkedUserId` denormalized nullable; assignment-based uniqueness (participant × session)
- **Description addition (Hi.Events §7.7 lesson):** every `RegistrationTicketAssignment` references a **concrete order line**, and a DB constraint prevents assignments from exceeding that line's quantity — the exact per-line multiset is enforced, not just the total count (Hi.Events validates only the total, allowing tier-mismatch).
- **Acceptance Criteria:**
  - [ ] Unique index prevents double admission of one participant to one session while allowing multiple unnamed tickets pre-assignment
  - [ ] DB-level test: assignments per order line cannot exceed the line quantity; a tier-A/tier-B order cannot carry two tier-A assignments
- **Dependencies:** 6.1
- **Effort:** M

#### Task 6.3: Group booking commands + limits
- **Type:** create
- **Layer:** Application
- **Files:** new participant/assignment commands (add/update/assign/bulk-assign/defer), limit enforcement extension in hold/finalize handlers (`MaximumQuantityPerBookingParty` via purchaser actor)
- **Description:** Company bulk assignment (CSV import deferred to Phase 14 — command accepts collection payload now); anonymous-enforcement honesty: per-account/verified-contact limits only when identity exists; organizer UI copy required by 6.5.
- **Acceptance Criteria:**
  - [ ] Family scenario (2×Adult+3×Child, child PII required, adult optional) passes end-to-end handler test
- **Dependencies:** 6.2
- **Effort:** L

#### Task 6.4: API + HAL for participants/assignments
- **Type:** create
- **Layer:** API
- **Files:** participant/assignment controller actions on the order surface (authenticated + capability-token variants), event and `StudioContextDto` `view-participants` organizer relations; contract regeneration
- **Acceptance Criteria:**
  - [ ] Capability-token guest can manage only own order's participants; organizer sees participants only with permission-gated link
- **Dependencies:** 6.3
- **Effort:** M

#### Task 6.5: Blazor group-booking + Studio attendee UX
- **Type:** create/modify
- **Layer:** Blazor
- **Files:** extend checkout pages with participant editors per data-mode; new `Pages/Studio/StudioAttendees.razor`, `StudioEventAttendees.razor`; modify `Routes.razor`, `StudioWorkspaceNavigation.razor`, `StudioEventNavigation.razor`; deferred-assignment reminder surfaces
- **Description:** Per-ticket participant forms rendered by mode; buyer-to-participant **copy controls** ("copy my details to first / to all participants" — Hi.Events checkout lesson, report §9.1.9) with per-participant override; "Hard per-person limits require an account or a verified contact method" organizer hint (§19.5); accessible repeatable form groups. Add actor `/studio/attendees` and event `/studio/events/{eventId}/attendees`; each sidebar link requires `view-participants`, and rows/actions use participant/order resource links.
- **Acceptance Criteria:**
  - [ ] bUnit: child ticket requires participant fields; unnamed employee tickets confirm with deferred deadline visible; copy-buyer-details remains editable; Attendees actor/event links and operations disappear without `view-participants`
- **Dependencies:** 6.4
- **Effort:** L

---

### Phase 7: Registration Form Authoring Core
- **Goal:** The canonical form system: workflows, requirements, immutable versions, typed fields/options, bounded conditions, JSON Schema artifacts.
- **Depends on:** Phase 5 (orders exist to attach workflows); Phase 6 recommended (subject types).
- **Relevant files:** new Domain — `RegistrationWorkflow.cs`, `RegistrationRequirement.cs` (with `Criticality`, `CanSkip`, `CompletionEffect`, `AnswerSyncMode`, `AppliesToSubjectType` per Report 2 §10.4), `RegistrationForm.cs`, `RegistrationFormVersion.cs` (`Version/Status/SchemaHash/PublishedAt/RetiredAt/SourceTemplate*/ConcurrencyStamp`), `RegistrationFormSection.cs`, `RegistrationFormField.cs`, `RegistrationFormFieldOption.cs`, `RegistrationFormRule.cs`, lookups: `RegistrationFieldType` (17 portable types + `OpaqueExternal`), `RequirementCriticality`, `RequirementCompletionEffect`, `AnswerSyncMode`, `AnswerSubjectType`, `DataClassification`, field governance fields (D12/§18 flags incl. `RetentionPolicyId`, `OrganizerVisibility`, `RequiresExplicitConsent`, `IsProviderTransferAllowed`); shared validation value objects extracted per D1; Application — `Features/RegistrationForms/**` authoring commands/queries + `Services/Registration/FormSchemaArtifactGenerator.cs`; API — form authoring controllers + HAL (`manage-registration-workflow`); Cerbos `islamuevent_registration_form.yaml`; Blazor — form builder.
- **Related skills/rules:** all path rules; documentation style for `docs/DOMAIN.md` new section.
- **Acceptance criteria:** §23 canonical-form matrix (immutable versions, all types, multivalue ordering, option retirement, conditional requiredness, schema-hash stability); bounded condition language only (`equals/notEquals/in/contains/exists/compare/all/any/not`) — no scripts; composite decomposition (name/address/matrix/ranking) documented in field-type registry.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` *(repeat justified: version immutability, condition evaluation, and schema-hash stability are pure domain logic)*
- **Rollback / failure handling:** Self-contained authoring plane; runtime (Phase 8) not yet wired. Schema-hash algorithm is frozen on first publish — treat the hash serializer as contract from day one (test pins a golden hash).

#### Task 7.1: Workflow + requirement + channel skeleton
- **Type:** create
- **Layer:** Domain + Persistence
- **Files:** `RegistrationWorkflow.cs`, `RegistrationRequirement.cs`, `RegistrationChannel.cs` (channel references binding — nullable provider fields until Phase 9; Native channel kind works standalone), lookups + configurations + seeder
- **Description:** Workflow per event/purpose; requirement completion semantics (`ALL` mandatory at workflow, `ANY` across channels, optional/informational/post-registration never block per FR-SYNC-03); applicability (`AllOrders/SpecificTicketType/EveryParticipant/LeadBookerOnly/ChildParticipants/SpecificSessionSelection`) as typed rules.
- **Acceptance Criteria:**
  - [x] Requirement evaluation rules unit-tested incl. skip recording (`SkippedByRegistrant`)
- **Dependencies:** Phase 5
- **Effort:** L
- **Evidence:** Domain 11/11, characterization 1/1, architecture 6/6, naming 11/11, six-predicate harness; Persistence 4/4; rootless Podman/PostgreSQL 1/1; scoped Release builds 0 errors; EF no pending model changes for generated `20260801000023_init`; seven protected migration hashes unchanged; code review PASS with no findings; adversarial QA `confirmed`. Durable subject fulfillment/skip persistence remains Task 8.5.

#### Task 7.2: Form/version/section/field/option model with immutability
- **Type:** create
- **Layer:** Domain + Persistence
- **Files:** listed above + configurations; `Services/Registration/FormVersionRules.cs` (new)
- **Description:** Published versions immutable (domain + concurrency enforcement); edit → new draft; field dual identity (immutable version-field ID + stable `Namespace/Key` machine identity, e.g., `platform.registration/email`); option stability; field governance flags per §18; validation constraint fields mirroring the custom-property vocabulary via shared value objects.
- **Acceptance Criteria:**
  - [ ] Immutability + provenance tests (template fields recorded, never silently synced)
  - [ ] Provider question IDs impossible to store as canonical identity (no such column; mapping-only by construction)
- **Dependencies:** 7.1
- **Effort:** XL

#### Task 7.3: Bounded condition language
- **Type:** create
- **Layer:** Domain
- **Files:** `RegistrationFormRule.cs`, `Services/Registration/FormConditionEvaluator.cs` (new)
- **Description:** Typed condition AST with nine syntax tokens representing ten semantic operations (numeric and date comparison are distinct typed cases of `compare`), referencing earlier answers in the same version; visibility + requiredness effects only; explicit test that the evaluator surface cannot mutate state or perform IO (pure function over answer snapshot).
- **Acceptance Criteria:**
  - [ ] Evaluator property tests; forbidden-construct list asserted (no arbitrary expressions representable)
- **Dependencies:** 7.2
- **Effort:** M

#### Task 7.4: JSON Schema 2020-12 artifact generation
- **Type:** create
- **Layer:** Application
- **Files:** new `Contracts/Services/IFormSchemaArtifactGenerator.cs` + `Services/Registration/FormSchemaArtifactGenerator.cs` (Application service, no IO), version fields `SchemaHash` + stored artifacts
- **Description:** Deterministic generation of data/UI/logic/mapping artifacts; content hash pinned at publish; golden-file tests for hash stability across runs and machines (culture-invariant serialization).
- **Acceptance Criteria:**
  - [ ] Same version → identical hash on repeat generation; hash changes on any field mutation
- **Dependencies:** 7.2, 7.3
- **Effort:** M

#### Task 7.5: Authoring Application + API + Cerbos
- **Type:** create
- **Layer:** Application + API
- **Files:** `Features/RegistrationForms/**` (workflow/requirement/form/version/field/option/rule commands + queries, validators), controllers, event HAL (`manage-registration-workflow`), Cerbos policy + parity
- **Description:** Verified-organizer-only authoring (contributor forbidden test); publish preflight (fields valid, options complete, conditions resolvable, consent fields carry purpose + text version); contract regeneration + changelog.
- **Acceptance Criteria:**
  - [ ] Publish rejects unresolvable conditions and consent fields without purpose codes
- **Dependencies:** 7.4
- **Effort:** L

#### Task 7.6: Studio form builder
- **Type:** create
- **Layer:** Blazor
- **Files:** new `Pages/Studio/RegistrationForms/**` (builder shell, section list, field editor per type, option editor, condition editor, version timeline); modify `Routes.razor`, `StudioEventNavigation.razor`
- **Description:** Add `/studio/events/{eventId}/forms`. The sidebar and builder require `manage-registration-workflow`; version states are visible; publish flow shows preflight results; drag-ordering has a keyboard fallback. Reuse `StudioEventContextState`; do not create a second management shell.
- **Acceptance Criteria:**
  - [ ] bUnit: Forms sidebar link absent without `manage-registration-workflow`; published version read-only; new-version flow works against mocked client
- **Dependencies:** 7.5
- **Effort:** XL

#### Task 7.7: Requirement attachment to participation + walk-in standalone questionnaires
- **Type:** create/modify
- **Layer:** Application + API
- **Files:** workflow attach/detach commands; participation-config validation extension (walk-in/info-only may attach only `NoRegistrationEffect` requirements per FR-SYNC-06)
- **Acceptance Criteria:**
  - [ ] Walk-in event exposes `optional-questionnaire` action without any order/registration creation
- **Dependencies:** 7.5, Phase 2
- **Effort:** M

#### Task 7.8: Form localization strategy
- **Type:** investigate + modify
- **Layer:** Domain/Application
- **Files:** `docs/LOCALIZATION.md` read; field/section label storage decision (single-language per version vs per-language columns/table)
- **Description:** Bounded investigation; implement the chosen minimal model (recommended: per-version `LanguageTag` + optional translation table deferred); record decision.
- **Acceptance Criteria:**
  - [ ] Decision recorded; RTL rendering unaffected; `MULTILINGUAL` capability honestly reported as absent until implemented
- **Dependencies:** 7.2
- **Effort:** M

---

### Phase 8: Native Collection Runtime
- **Goal:** The reference implementation every provider is tested against: attempts, submissions, typed multi-subject answers, normalization/validation pipeline, consent evidence, requirement fulfillment, idempotent finalization, native Blazor renderer.
- **Depends on:** Phases 6, 7.
- **Relevant files:** new Domain — `RegistrationAttempt.cs` (pins intent/requirement/channel/form-version/binding/mapping-revision + `AttemptTokenHash`, expiry, supersession), `RegistrationSubmission.cs` (per §5 fields incl. `PayloadHash`, verification/normalization/validation statuses, `TrustLevel`, `SupersedesSubmissionId`), `RegistrationSubmissionRevision.cs`, `RegistrationAnswer.cs` (D5 shape), `RegistrationSensitiveAnswerValue.cs`, `RegistrationAnswerFile.cs`, `RegistrationSubmissionIssue.cs`, `RegistrationRequirementFulfillment.cs`, `RegistrationFinalizationEffect.cs`, `RegistrationConsentRecord.cs`, status lookups per D16; `Services/Registration/AnswerNormalizationRules.cs`; Application — `Features/RegistrationSubmissions/**` pipeline handlers; API — native submission endpoints (authenticated + PublicTransactional guest variant); Blazor — native form renderer.
- **Related skills/rules:** all path rules; `outbox-pattern`.
- **Acceptance criteria:** §7 pipeline implemented (decode→normalize→parse→field constraints→cross-field rules→persist→safe output); §23 registration-correctness rows for the native path (concurrent attempts, duplicate finalization effect, rollback, outbox-after-commit); consent evidence immutable; optional requirement skip works; answers never appear in logs/metrics/ProblemDetails.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` *(repeat justified: the normalization/validation pipeline handlers are Application-owned and this is the fastest deterministic coverage)*
- **Rollback / failure handling:** Native runtime is additive; finalization extends the Phase 5 transaction. If the answer CHECK constraints fight EF value conversions, resolve at configuration level (owned columns), never by weakening constraints; record in context.

#### Phase 8 Current Status — 2026-08-09 Europe/Brussels

**Production implementation:** Complete in source. The Clean Architecture/CQRS path accepts typed, subject-scoped answers through `NormalizeRegistrationSubmissionCommandHandler`; it applies the Phase 7 cross-field evaluator, persists safe issues, writes atomic answer rows, and snapshots immutable consent text/version/language evidence. Sensitive values use the shared ASP.NET Core Data Protection protector with purpose `Explore.RegistrationSensitiveAnswerValue` and version `v1`.

**Finalization and surfaces:** Requirement fulfillment and attempt consumption are atomic with one fenced `RegistrationFinalizationEffect`; the worker performs the later lifecycle transition. Authenticated and `PublicTransactional` guest submission routes use server-owned subjects, hashed capability tokens, idempotency, and HAL affordances. The Blazor renderer implements all 17 portable field contracts with conditional visibility, RTL/accessibility announcements, and optional skip. File answers remain quarantined until explicit audited manual release; scanner execution, infected-file disposition, and automatic clean-file release remain deferred.

**Generated provider baselines:** SQLite `20260809144252_InitialSqliteApplication`, PostgreSQL `20260809144354_InitialPostgreSqlApplication`, SQL Server `20260809144515_InitialSqlServerApplication`, MariaDB `20260809144607_InitialMariaDbApplication`, and MySQL `20260809144652_InitialMySqlApplication`. All five were regenerated through EF tooling and report no pending model changes.

**Verification status:** Complete. Release build succeeds with zero errors; Application passes 3,458/3,458; Architecture passes 181 with one governed skip; protected idempotency middleware passes 8/8; native registration HTTP passes 7/7; and native Blazor transport mapping passes 1/1. Real HTTP retries preserve encrypted attempt and guest-order capabilities, execute the command once, and persist `dp:v1:` replay envelopes without plaintext capabilities. Independent Oracle review returned `APPROVE` with no Phase 8-owned findings. Broad Persistence and API projects still contain unrelated shared-worktree failures recorded in the context ledger.

#### Task 8.1: Attempt + submission + status machines
- **Type:** create
- **Layer:** Domain + Persistence
- **Files:** entities above + configurations; uniqueness `(ProviderBindingId, ProviderResponseId, ProviderResponseRevision)` prepared (nullable binding for native); attempt-token hash single-use
- **Acceptance Criteria:**
  - [x] Duplicate submission insert → acknowledged no-op (unique index test)
  - [x] Attempt supersession rules unit-tested (late superseded evidence retained, cannot finalize)
  - [x] Answer identity uniqueness constrained at DB level (one answer row set per submission/field/subject/ordinal — Hi.Events lacks this, report §4.7) — completed by Task 8.2
- **Dependencies:** Phase 7
- **Effort:** L

#### Task 8.2: Typed answer storage + CHECK constraints + subjects
- **Type:** create
- **Layer:** Domain + Persistence
- **Files:** `RegistrationAnswer.cs`, `RegistrationSensitiveAnswerValue.cs` + configurations with raw-SQL check constraints (`num_nonnulls(...) = 1` + type agreement)
- **Description:** Subject typing per §18 Report 2 (`RegistrationOrder/Purchaser/Participant/TicketAssignment/SessionSelection`); multivalue `Ordinal`; option FK to version options; subject-shape checks (an order-scoped field cannot carry a participant subject and vice versa — Hi.Events leaves this unenforced, report §4.7).
- **Acceptance Criteria:**
  - [x] DB-level test: two value columns populated → constraint violation; wrong-type column for declared field type → violation
  - [x] Subject-shape constraint test: answer subject type must match the field's declared applicability
- **Dependencies:** 8.1
- **Effort:** L

#### Task 8.3: Normalization + validation pipeline
- **Type:** create
- **Layer:** Application (+ Domain value objects)
- **Files:** new `Features/RegistrationSubmissions/Handlers/Commands/{SubmitNativeFormCommandHandler,ValidateSubmissionService}.cs`; Domain normalizers (NFC, E.164 phone, email dual-value, ISO country, BCP-47, URL scheme allowlist, decimal/date/instant parsing)
- **Description:** Per §7: reject don't coerce; no HTML in text; output-context encoding left to renderers; cross-field rules via Phase 7 evaluator; issues recorded as `RegistrationSubmissionIssue` rows; sensitive classifications routed to encrypted store (Data Protection stack, key version recorded — investigation folded here).
- **Acceptance Criteria:**
  - [x] Type matrix unit tests (all 17 portable types, valid + invalid + boundary)
  - [x] Sensitive answer round-trips encrypted; plaintext absent from DB row (implementation uses the Data Protection purpose/version protector)
- **Dependencies:** 8.2
- **Effort:** XL

#### Task 8.4: Consent evidence records
- **Type:** create
- **Layer:** Domain + Application
- **Files:** `RegistrationConsentRecord.cs` + configuration; consent-field handling in the pipeline (purpose, exact text snapshot, versions, language, subject reference)
- **Acceptance Criteria:**
  - [x] Consent answer produces immutable evidence row; withdrawal timestamps supported; Boolean-only consent impossible for consent-typed fields
- **Dependencies:** 8.3
- **Effort:** M

#### Task 8.5: Requirement fulfillment + idempotent finalization effect
- **Type:** create
- **Layer:** Application
- **Files:** `RegistrationRequirementFulfillment.cs`, `RegistrationFinalizationEffect.cs` + handlers extending Phase 5 finalization (all-mandatory-fulfilled gate before `ReadyForCheckout`)
- **Description:** Fulfillment recorded per intent/requirement with source submission; finalization effect is durable + fenced (webhook-outbox pattern) so provider and native paths share one finalizer; canonical flow §10 enforced end-to-end.
- **Acceptance Criteria:**
  - [x] Duplicate finalization effect executes once (fencing); optional requirements never block (FR-SYNC-03)
- **Dependencies:** 8.3, Phase 5
- **Effort:** L

#### Task 8.6: Native submission API surface
- **Type:** create
- **Layer:** API
- **Files:** attempt-launch + submit endpoints on the order surface (authenticated; guest variant `PublicTransactional` with capability token); HAL `optional-questionnaire`, requirement-progress relations; contract regeneration
- **Acceptance Criteria:**
  - [x] Answers absent from ProblemDetails on validation failure (issue codes + field keys only)
  - [x] Typed requirement progress, pinned attendee-safe form metadata, server-authored subjects, and state-dependent submit/skip HAL affordances are generated into the NSwag client
- **Dependencies:** 8.5
- **Effort:** M

**Completion evidence (2026-08-03):** Authenticated and guest launch/submit HTTP scenarios pass 6/6, including a real MediatR + EF repository flow that reads exact progress descriptors, persists only the attempt capability hash, records an optional skip, fails closed on wrong channel lineage, and proves that two published forms yield neither a progress descriptor nor a directly launchable form. A real PostgreSQL two-consumer race passes 1/1 and proves exactly one atomic skip success, consumed attempt, fulfillment, and finalization effect. Native OpenAPI invariants pass 2/2, the broader OpenAPI parity suite passes 11/11, inventory generation passes 1/1, the generated client and canonical 37-project Release build are green, and LSP reports zero errors. Receipt: `.omo/evidence/phase86-DONE_CLAIM.md`.

#### Task 8.7: Native Blazor form renderer
- **Type:** create
- **Layer:** Blazor
- **Files:** new `Components/Registration/FormRenderer/**` (renderer shell + one component per field type + condition-driven visibility + skip control + consent blocks + progress)
- **Description:** Renders pinned form version from DTO; client-side hints only (server validation authoritative); optional requirements show "Optional" + "Skip and continue"; keyboard-complete; RTL; announced processing status.
- **Acceptance Criteria:**
  - [x] Per-type renderer and condition-toggle coverage; skip flow remains a non-error transition
- **Dependencies:** 8.6
- **Effort:** XL

#### Task 8.8: File answers (gated)
- **Type:** investigate + create
- **Layer:** Domain + Infrastructure
- **Files:** `RegistrationAnswerFile.cs` (metadata, quarantine state, scan status, storage reference to `StorageObject`); upload path investigation (existing storage endpoints, MIME sniffing, size limits; malware scanning availability)
- **Description:** If no scanner exists, ship quarantine-by-default (files never exposed until manually released) and record scanner integration as deferred work — File field type remains publishable only when the deployment enables the file pipeline.
- **Acceptance Criteria:**
  - [x] Quarantined file inaccessible via any read endpoint; decision + deferral recorded
- **Dependencies:** 8.3
- **Effort:** L

---

### Phase 9: Provider Framework (Connections, Bindings, Capabilities, Callbacks, Reconciliation)
- **Goal:** Everything provider-agnostic: connections with secret bindings, bindings with mapping/drift state, capability tuples, sync modes, callback intake extension, reconciliation, health — no concrete external provider yet.
- **Depends on:** Phase 8.
- **Relevant files:** new Domain — `RegistrationProviderConnection.cs`, `RegistrationProviderBinding.cs` (per §5 binding fields incl. presentation/collection/completion/sync/trust + publication/synchronization/health state), `RegistrationProviderCapability.cs`, `RegistrationProviderFieldMapping.cs`, `RegistrationProviderOptionMapping.cs`, `RegistrationProviderSchemaRevision.cs`, lookups (`RegistrationProviderKind`, `SchemaAuthority`, `PresentationMode`, `CollectionMode`, `CompletionMode`, `RegistrationTrustLevel`, `SchemaDriftClass`); Application — capability-segregated contracts (D3) in `Contracts/Services/Registration/`, `Features/RegistrationProviders/**`, launch-descriptor service, drift classifier, reconciliation commands; API — `POST /api/integrations/registration/{provider}/{bindingId}/callback` family riding the incoming-webhook intake, binding-management endpoints, health read models; Infrastructure — provider registry + Null/Native descriptors; Secrets — new `SecretDefinitionRegistry` entries; Blazor — connection/binding management UI; CSP `frame-src` allowlist plumbing for embeds.
- **Related skills/rules:** `outbox-pattern`, `auth-patterns`, webhook rules by analogy; `docs/WEBHOOKS.md`.
- **Acceptance criteria:** §23 provider-conformance + callback-reliability matrices for the framework level (dedup, out-of-order, stale timestamp, unknown tuple fails closed); sync modes enforced (`NONE` stores nothing, `COMPLETION_ONLY` stores no answers — FR-SYNC-04/05); drift classes per §17 with fail-closed behavior; embeds only from connection-approved domains, server-generated iframes, accessible titles, new-tab fallback.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` *(repeat justified: callback intake endpoints, duplicate acknowledgment, and binding-resolution behavior are API-level contracts)*
- **Rollback / failure handling:** Callback endpoints fail closed to 2xx-acknowledge-and-park (`NeedsReconciliation`) rather than 5xx loops; framework ships behind absent-provider reality (no adapter yet), so nothing is user-visible until Phase 10.

#### Task 9.1: Provider configuration domain model + secret definitions
- **Type:** create
- **Layer:** Domain + Persistence + Secrets
- **Files:** entities/lookups above + configurations + seeder; `src/Explore.Domain/Secrets/SecretDefinitionRegistry.cs` (existing — add registration provider secret definitions, tenant scope)
- **Acceptance Criteria:**
  - [x] Credentials representable only as secret-binding references (no secret columns); `Explore.Secrets.UnitTests` addition for new definitions
- **Dependencies:** Phase 8
- **Effort:** L

#### Task 9.2: Capability contracts + registry + effective-capability resolution
- **Type:** create
- **Layer:** Application + Infrastructure
- **Files:** the ten D3 interfaces; `RegistrationProviderRegistry` (Infrastructure); effective-capability resolver (proven ∩ configured ∩ governance ∩ mapping ∩ authorization); capability tuple entity wiring
- **Acceptance Criteria:**
  - [x] Unknown tuple → automatic finalization refused, redirect/manual channels still offered (fail-closed test)
- **Dependencies:** 9.1
- **Effort:** L

#### Task 9.3: Field/option mapping + schema revision + drift classifier
- **Type:** create
- **Layer:** Domain + Application
- **Files:** mapping entities; `Services/Registration/SchemaDriftClassifier.cs` (new, pure); mapping-revision pinning on attempts (Phase 8 fields already present)
- **Description:** Drift classes `NoDrift/AdditiveOptionalChange/LabelOnlyChange/MappingRequired/RequiredFieldRemoved/TypeChanged/OptionSetChanged/UnsupportedChange` with §17 behaviors; mappings never silently rewritten after submissions exist (guard + test).
- **Acceptance Criteria:**
  - [x] Classifier unit-tested per class; fail-closed classes block binding publication
- **Dependencies:** 9.1
- **Effort:** L

#### Task 9.4: Callback intake extension + registration effect worker
- **Type:** create
- **Layer:** API + Application
- **Files:** new `src/Explore.API/Controllers/RegistrationProviderCallbackController.cs` (bounded-bytes read, binding resolution without tenant disclosure, provider-proof verification hook, insert-or-acknowledge `IncomingWebhookMessage`, one unique registration effect, prompt return); new `Features/RegistrationSubmissions/Handlers/Commands/ProcessProviderSubmissionEffectHandler.cs` worker path (fenced claim → re-verify → fetch where supported → normalize → validate → fulfill → finalize via Phase 8 effect)
- **Acceptance Criteria:**
  - [x] Controller provably never touches order/registration aggregates (architecture test on namespace references)
  - [x] Duplicate callback acknowledged; callback-before-user-return and user-return-before-callback orderings both converge (handler tests)
- **Dependencies:** 9.2, 9.3
- **Effort:** XL

#### Task 9.5: Sync-mode enforcement + trust-level finalization policy
- **Type:** create
- **Layer:** Application
- **Files:** pipeline extensions: `NONE` (no storage, no fulfillment), `COMPLETION_ONLY` (evidence only, fulfillment iff verified+correlated+unexpired per §10.3), `SELECTED_FIELDS` (approved mappings only), `FULL_CANONICAL`, `MIRROR_ONLY` (sink path stub for Phase 10); minimum-trust-level policy gate → `NeedsReconciliation` below threshold
- **Acceptance Criteria:**
  - [x] FR-SYNC-01…07 handler tests; completion-only stores zero `RegistrationAnswer` rows
- **Dependencies:** 9.4
- **Effort:** L

#### Task 9.6: Reconciliation + provider health
- **Type:** create
- **Layer:** Application + API
- **Files:** reconciliation commands (poll checkpoint fetch abstraction, manual import queue, `NeedsReconciliation` organizer queue); health read model per binding (connection validity, callback age, drift, reconciliation lag — bounded fields per §21); event HAL relations `manage-registration-channels` / `view-registration-provider-health`
- **Acceptance Criteria:**
  - [x] Health surface exposes no attendee data; reconciliation queue lists parked submissions with issue codes only
- **Dependencies:** 9.4
- **Effort:** L

#### Task 9.7: Channels + embed/CSP + Studio provider UI
- **Type:** create
- **Layer:** API + Blazor
- **Files:** channel CRUD on requirements (attach binding, order, fallback); server-generated iframe descriptors from approved connection domains; CSP `frame-src` allowlist wiring (investigate current CSP source in BFF/API — bounded); new `Pages/Studio/StudioEventIntegrations.razor`; modify `Routes.razor`, `StudioEventNavigation.razor`; attendee-facing processing-status pattern with intent-status polling
- **Acceptance Criteria:**
  - [x] Arbitrary organizer iframe HTML impossible (no such input path); non-allowlisted domain refused server-side
  - [x] Completion never inferred from iframe navigation (UI polls order/requirement status only)
  - [x] Integrations sidebar link is absent unless `manage-registration-channels` or `view-registration-provider-health` exists
- **Dependencies:** 9.5, 9.6
- **Effort:** XL

---

### Phase 10: Formbricks Provider (Deep Integration)
- **Goal:** First external provider at full depth, in consultation §22 Phase-3 order: BYO link/embed → signed callback → API fetch → schema import/mapping → managed provisioning → headless renderer → mirror sink → files/multilingual conformance.
- **Depends on:** Phase 9.
- **Relevant files:** new Infrastructure — `src/Explore.Infrastructure/Services/Registration/Providers/Formbricks/{FormbricksDescriptor,FormbricksCallbackVerifier,FormbricksSchemaReader,FormbricksSubmissionReader,FormbricksSubmissionWriter,FormbricksFormProvisioner,FormbricksSubscriptionManager,FormbricksReconciliationProvider,FormbricksSubmissionSink}.cs` + HTTP client wiring; docker/compose optional profile for self-hosted Formbricks (`docker-compose.yml`, `docs/SELF_HOSTING.md`); conformance-evidence fixtures under `tests/Explore.Infrastructure.Tests/Registration/Formbricks/`.
- **Related skills/rules:** `error-tracking`; webhook signature-verification patterns already in repo.
- **Acceptance criteria:** All four modes (A BYO, B managed provisioning with publish preflight §14, C headless via ISLAMU backend proxy, D mirror sink); Standard-Webhooks verification (`webhook-id/timestamp/signature`, `whsec_`, HMAC-SHA256 raw body, replay tolerance) sharing the crypto core with existing webhook code; API v1 pinned with tested deployment tuple; unknown Formbricks version fails closed.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Each mode is independently disableable via capability rows; a failing mode is withdrawn by capability, not by code rollback. All HTTP behind adapters — no live-service tests; fixtures capture recorded payloads.

#### Task 10.1: Conformance re-verification + capability profile pin
- **Type:** investigate
- **Layer:** Infrastructure
- **Files:** capability tuple seed + `tests/.../Formbricks/conformance-notes.md` (new, in-repo evidence)
- **Description:** Re-verify (against current official docs at implementation time) webhook header profile, API v1 endpoints used, headless schema/response contracts, domain-split and S3 file-upload requirements; pin the supported version tuple; record deltas from consultation citations.
- **Acceptance Criteria:**
  - [ ] Evidence file lists exact endpoints/headers with dates; capability rows match it
- **Dependencies:** Phase 9
- **Effort:** M

#### Task 10.2: Signed callback verifier + BYO link/embed (Modes A start)
- **Type:** create
- **Layer:** Infrastructure + API
- **Files:** `FormbricksCallbackVerifier.cs` (Standard Webhooks profile over shared HMAC core), binding setup for existing surveys, link/embed presentation descriptors
- **Acceptance Criteria:**
  - [ ] §23 callback rows: valid/invalid signature, stale timestamp, duplicate, out-of-order — all against recorded fixtures
- **Dependencies:** 10.1
- **Effort:** L

#### Task 10.3: Management-API response fetch + schema import/mapping (Mode A complete)
- **Type:** create
- **Layer:** Infrastructure + Application
- **Files:** `FormbricksSchemaReader.cs`, `FormbricksSubmissionReader.cs`; import → frozen `ExternalImported` ISLAMU version; fingerprint + mapping revision recorded
- **Acceptance Criteria:**
  - [ ] `responseFinished` → fetch → normalize → fulfill end-to-end handler test on fixtures; drift on re-import classified per Phase 9
- **Dependencies:** 10.2
- **Effort:** L

#### Task 10.4: Managed provisioning (Mode B) with publish preflight
- **Type:** create
- **Layer:** Infrastructure + Application
- **Files:** `FormbricksFormProvisioner.cs`, `FormbricksSubscriptionManager.cs` (webhook registration)
- **Description:** Create/update survey from canonical version; §14 preflight (required fields supported, options mapped, no unsupported mandatory condition, webhook registered, survey active, fingerprint matches); ambiguous provider acceptance never auto-retried without reconciliation (webhook-intent lesson).
- **Acceptance Criteria:**
  - [ ] Preflight failure blocks binding publication with typed reasons
- **Dependencies:** 10.3
- **Effort:** L

#### Task 10.5: Headless mode (C) through ISLAMU backend
- **Type:** create
- **Layer:** Application + Infrastructure
- **Files:** `FormbricksSubmissionWriter.cs`; native renderer submits to ISLAMU Event API (canonical validation/persistence first), optional post-commit Formbricks response write via outbox-driven sink call
- **Acceptance Criteria:**
  - [ ] Browser never talks to Formbricks response endpoints for registration (no such URL in client DTOs); Formbricks write failure never affects finalized order
- **Dependencies:** 10.3
- **Effort:** M

#### Task 10.6: Mirror sink (Mode D) + self-host profile + files/multilingual conformance
- **Type:** create
- **Layer:** Infrastructure + DevOps
- **Files:** `FormbricksSubmissionSink.cs` (approved-fields-only, post-commit, retried via outbox); optional compose profile + `docs/SELF_HOSTING.md` (workspace-per-tenant isolation, domain split, pinned image); capability truth for FILE_UPLOAD/MULTILINGUAL per 10.1 evidence
- **Acceptance Criteria:**
  - [ ] Sink transfers only `IsProviderTransferAllowed` fields; compose profile optional and absent from default profiles
- **Dependencies:** 10.4, 10.5
- **Effort:** L

---

### Phase 11: Microsoft Forms Provider (Connector Channel)
- **Goal:** Link/embed presentation + versioned Power Automate callback solution at `DelegatedAutomation` trust; org-account scope; Excel remains a sink/reconciliation source, never transaction authority.
- **Depends on:** Phase 9 (Phase 10 not required — parallelizable after 9).
- **Relevant files:** new Infrastructure — `Providers/MicrosoftForms/MicrosoftFormsRegistrationProviderDescriptor.cs`; versioned flow template artifact `docs/integrations/microsoft-forms-flow-template.md`; manual mapping UI reuse from Phase 9; `docs/INTEGRATIONS.md`. An importable solution export is a tenant-generated deployment artifact because Power Platform connection references, publisher identity, and the published flow require a real Microsoft 365/Dataverse environment; the repository must not fabricate one.
- **Acceptance criteria:** Callback envelope per §16 (provider code, binding, form/response IDs, attempt token, timestamp, mapped values, contract version, idempotency key) with API-key or signature verification; `TrustLevel = DelegatedAutomation` policy gating (tenant/event decides auto-finalize vs review); personal-account forms offered only as redirect/iframe/manual-reconciliation; no undocumented Microsoft APIs anywhere.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` *(repeat justified: a new provider adapter + envelope verifier lands in this project)*
- **Rollback / failure handling:** Channel disabled by capability; callbacks park to reconciliation on verification failure; setup wizard test-event verification gates activation.

#### Task 11.1: Conformance re-verification + connector contract pin
- **Type:** investigate
- **Layer:** Infrastructure
- **Files:** conformance-notes evidence file; capability tuple seed
- **Description:** Re-verify Forms connector triggers/actions and org-account restriction against current Microsoft Learn docs; version the callback envelope contract (`connector contract version` field).
- **Acceptance Criteria:**
  - [ ] Evidence file dated; personal-account limitation documented in organizer-facing copy
- **Dependencies:** Phase 9
- **Effort:** S

#### Task 11.2: Callback endpoint profile + envelope verifier
- **Type:** create
- **Layer:** Infrastructure + API
- **Files:** `MicrosoftFormsCallbackVerifier.cs`; binding-scoped API-key secret (tenant-scope secret definition); intake rides Phase 9 controller
- **Acceptance Criteria:**
  - [ ] Envelope validation matrix (missing token, stale timestamp, bad key, duplicate idempotency key) on fixtures
- **Dependencies:** 11.1
- **Effort:** M

#### Task 11.3: Versioned Power Automate solution + setup wizard + manual mapping
- **Type:** create
- **Layer:** Docs + Application + Blazor
- **Files:** versioned flow template doc; Studio setup flow (create binding → configure the documented template → send test event → verify → activate); manual field-mapping UI (Phase 9 mapping surfaces) since schema read is unsupported. A real tenant may export its validated solution through its deployment process.
- **Acceptance Criteria:**
  - [ ] Test-event verification required before channel activation; mapping completeness enforced for required canonical fields
- **Dependencies:** 11.2
- **Effort:** L

#### Task 11.4: Reconciliation import (CSV/Excel) path
- **Type:** create
- **Layer:** Application
- **Files:** manual-import command accepting organizer-uploaded response export mapped through binding mappings; `ManualImport` trust level; never auto-finalizes above policy
- **Acceptance Criteria:**
  - [ ] Imported rows dedupe against callback-received responses (same response ID → no-op)
- **Dependencies:** 11.2
- **Effort:** M

---

### Phase 12: Google Forms Provider (Pub/Sub Channel)
- **Goal:** OAuth connection, form import/provision + explicit publication, field mapping, `RESPONSES` watch lifecycle with renewal, checkpointed response fetch, drift, Drive-file policy.
- **Depends on:** Phase 9 (parallelizable with 10/11 after 9).
- **Status:** Complete as of 2026-08-11. Implementation uses `GoogleFormsRegistrationProviderDescriptor.cs`, `RegistrationProviderSubscriptionLifecycleService.cs`, `RegistrationProviderSubscriptionLifecycleWorker.cs`, and provider subscription state persistence rather than separate per-method files.
- **Relevant files:** `src/Explore.Infrastructure/Services/Registration/Providers/GoogleForms/GoogleFormsRegistrationProviderDescriptor.cs`; `src/Explore.Application/Services/Registration/RegistrationProviderSubscriptionLifecycleService.cs`; `src/Explore.API/BackgroundServices/RegistrationProviderSubscriptionLifecycleWorker.cs`; `src/Explore.Domain/RegistrationProviderConnection.cs`; `src/Explore.Domain/RegistrationProviderSubscriptionState.cs`; focused tests in `tests/Explore.Infrastructure.Tests/Registration/GoogleForms/` and provider lifecycle/Application/API/Persistence lanes.
- **Acceptance criteria:** managed preflight can provision the form and subscription before schema comparison; Pub/Sub notification → Google OIDC-authenticated intake → response-sweep effect → checkpointed list responses → identifiers-only queued provider-submission effects → standard `registration.provider_submission` worker. Watch renewal runs before seven-day expiry; the first recovery sweep is immediate, later sweeps run every six hours, and timestamp overlap plus opaque continuations preserve unfinished batches; correlation via prefilled attempt token is capability-only; API-created forms explicitly publish and verify accepting state; no headless-submission, auto-finalize, webhook-secret, or Drive/file capability is advertised.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` *(repeat justified: Google adapter incl. watch lifecycle + Pub/Sub verification is new Infrastructure surface)*
- **Rollback / failure handling:** Watch loss degrades to polling reconciliation automatically (health surface flags it); OAuth failure disables channel by capability with organizer-visible health state.

#### Task 12.1: Conformance re-verification + OAuth connection model
- **Type:** investigate + create
- **Layer:** Infrastructure
- **Files:** conformance evidence; connection entity fields per §15; Google OAuth secret definitions (tenant scope)
- **Acceptance Criteria:**
  - [x] Evidence dated; scopes minimal and listed; token refresh recorded on connection
- **Dependencies:** Phase 9
- **Effort:** M

#### Task 12.2: Form select/import/provision + explicit publication + mapping
- **Type:** create
- **Layer:** Infrastructure + Application
- **Files:** `GoogleFormsSchemaReader.cs`, `GoogleFormsProvisioner.cs`; import → frozen version; provision → explicit publish step with verification
- **Acceptance Criteria:**
  - [x] Unpublished API-created form blocks activation with typed reason
- **Dependencies:** 12.1
- **Effort:** L

#### Task 12.3: Pub/Sub intake + watch lifecycle + checkpoint fetch
- **Type:** create
- **Layer:** Infrastructure + API + Application
- **Files:** `GooglePubSubIntakeVerifier.cs` (intake auth), `GoogleFormsSubscriptionManager.cs` (watch create/renew), renewal background job, `GoogleFormsSubmissionReader.cs` (list after checkpoint, dedupe)
- **Acceptance Criteria:**
  - [x] Notification-without-data flow fetches separately (fixture test); missed-notification reconciliation sweep recovers responses; watch expiry alert emitted at bounded metric
- **Dependencies:** 12.2
- **Effort:** XL

#### Task 12.4: Correlation policy + Drive-file handling decision
- **Type:** create + investigate
- **Layer:** Application
- **Files:** prefilled-token correlation (single-use, expiring, not identity proof); below-threshold → `NeedsReconciliation`; Drive file policy (copy into ISLAMU storage + quarantine per 8.8, or capability off) — decide and implement minimal
- **Acceptance Criteria:**
  - [x] Token-only correlation cannot auto-finalize when event policy requires authenticated respondent; decision recorded
- **Dependencies:** 12.3
- **Effort:** M

---

### Phase 13: Consent, Attendee-Data Surfaces, And Audited Exports
- **Goal:** Verified-recipient consent rule, typed consent subjects, per-participant consent independence, audited/purpose-gated exports, retention execution.
- **Depends on:** Phases 6, 8 (subjects + consent records exist); Phase 1 (verified organizer).
- **Relevant files:** existing — `EventContactShareConsent.cs` + features + Cerbos policy + `docs/CONTACT_SHARING.md`; new — consent-subject typing shared with `RegistrationConsentRecord`, export feature `Features/RegistrationExports/**`, audit events (pattern: ADR-007 durable audit trail), retention sweeps for answers/PII per field `RetentionPolicyId`.
- **Acceptance criteria:** FR-PRIV-01…06; §31.6 matrix (no prompt on unclaimed reported events, contributor never receives attendee links, purchaser consent self-only, withdrawn consent excluded, claim approval never reinterprets old consent, exports audited + tenant-scoped + field-level `IsExportable` gated).
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` *(repeat justified: consent gating and export authorization are API-surface contracts with Cerbos parity)*
- **Rollback / failure handling:** Export endpoints ship disabled-by-policy default; enable per tenant after audit evidence reviewed.

#### Task 13.1: Typed consent subjects on `EventContactShareConsent`
- **Type:** modify
- **Layer:** Domain + Persistence + Application
- **Files:** `EventContactShareConsent.cs` (subject type + subject ID replacing required `UserId`; dev-mode breaking), consent features, `docs/CONTACT_SHARING.md`
- **Acceptance Criteria:**
  - [ ] `User/RegistrationPurchaser/RegistrationParticipant/GuestContact` subjects representable; prompts only with verified `OrganizerActorId` (FR-PRIV-02, §22.1 rule tests)
- **Dependencies:** Phases 6, 8
- **Effort:** L

#### Task 13.2: Per-participant consent independence
- **Type:** create
- **Layer:** Application + Blazor
- **Files:** consent prompts per participant in checkout/requirement flow; guardian-for-child operational info per policy; child marketing disabled by default
- **Acceptance Criteria:**
  - [ ] Purchaser consent never copied to adult participants (handler test); no-retroactive-consent list (§22.4) asserted
- **Dependencies:** 13.1
- **Effort:** M

#### Task 13.3: Audited, purpose-gated exports + retention execution
- **Type:** create
- **Layer:** Application + API + Infrastructure
- **Files:** export commands (field-level `IsExportable` + purpose + active retention + consent state filters), audit event records, retention sweep job clearing expired answers/PII/consent-excluded data (respecting erasure-workstream boundaries — coordinate, don't duplicate)
- **Acceptance Criteria:**
  - [ ] Export excludes withdrawn consent and non-exportable fields; every export writes an audit row; retention sweep test clears expired answers only
- **Dependencies:** 13.1
- **Effort:** L

#### Task 13.4: Attendee-management HAL/Cerbos completion + Studio export action
- **Type:** create/modify
- **Layer:** API + Cerbos + Blazor
- **Files:** `view-participants`, `export-consented-contacts` relations finalized; Cerbos policies for export resources; existing Phase 6 Studio attendee pages/components; contributor/curator/instance-admin matrix (§23) parity and bUnit tests
- **Acceptance Criteria:**
  - [ ] §31.6 authorization rows all covered by parity tests; Studio export action appears only from `export-consented-contacts` and stays inside Attendees rather than adding another sidebar item
- **Dependencies:** 13.3
- **Effort:** M

---

### Phase 14: Advanced Orchestration, Hardening, And Deferred Integrations
- **Goal:** Close the long tail once the core is proven: guest→account linking, form templates/packs, provider migration tooling, analytics projections, bulk company import, generalized sinks, localization completion.
- **Depends on:** Phases 9–13 (any subset usable as prerequisites per task).
- **Relevant files:** per task; all new surfaces follow established patterns.
- **Acceptance criteria:** Each sub-feature lands with its own tests; nothing here blocks the platform being production-meaningful after Phase 13.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Fully additive; tasks independently deferrable.

#### Task 14.1: Guest order → account linking (explicit verification flow)
- **Type:** create — email-verified claim of a guest order into an authenticated account (§31.3); never silent. **Effort:** M
#### Task 14.2: Form templates/packs (tenant + platform blueprints with provenance)
- **Type:** create — `RegistrationFormTemplate` per §5 authoring tables; template changes never rewrite runtime versions. **Effort:** L
#### Task 14.3: Provider switching + attempt supersession tooling
- **Type:** create — organizer-facing channel switch (future attempts only), explicit restart-with-fallback, late-callback retention proof (§17). **Effort:** M
#### Task 14.4: Governed analytics projections over answers
- **Type:** create — `IsAnalyticsRelevant`/`IsOperationallyFilterable` projections (custom-property projection pattern), organizer dashboards without raw PII. **Effort:** L
#### Task 14.5: Company CSV bulk assignment + amendment flows
- **Type:** create — bulk participant assignment import; `RegistrationAmendment` controlled post-finalization changes. **Effort:** L
#### Task 14.6: Generalized submission sinks (Excel/Sheets/webhook consumers)
- **Type:** create — additional `IRegistrationSubmissionSink` implementations; approved-fields, post-commit, audited. **Effort:** M
#### Task 14.7: Blazor affordance-gating + Studio route/sidebar + accessibility audit
- **Type:** modify — client tests asserting every new mutating affordance and Studio section is `_links`-gated; route table covers all canonical `/studio/**` paths; actor navigation is replaced, not stacked, on event routes; keyboard/RTL/announcement audit per `docs/ACCESSIBILITY.md`. **Effort:** M
#### Task 14.8: Deferred commerce/admission design records (Hi.Events-informed)
- **Type:** create
- **Layer:** Docs
- **Files:** new `dev/active/registration-data-collection/deferred-design-records.md` (or `docs/adr/` notes when a decision is ripe)
- **Description:** Concise deferred design records per report §11.4, each citing the relevant `hi-events-report.md` section so future agents do not re-research the same repository: `PaymentAttempt` + provider payment/refund identity + reconciliation (unique provider attempt identity, idempotency keys, provider calls outside transactions — report §7.3/§7.5); `AdmissionTicket` with a rotatable signed/hashed admission credential that is **never** the display/public ID, transfer revoking/rotating the credential (report §5.5/§7.10); check-in lists mapped to ticket entitlements/sessions with append-only admission events, unique-active-admission constraint, authenticated or scoped-expiring scanner capabilities, camera/HID scanner UX with partial batch results (report §5.6/§7.11); ticket lookup/resend/self-service via hashed, single-purpose, expiring recovery capabilities without email enumeration (report §5.5); promo codes whose usage counts include live unexpired reservations (report §4.8); waitlist offers with expiry; optional add-ons/general products kept out of the admission vocabulary; and the former taxes/fees/invoices snapshot design, now removed from active scope and preserved in `dev/report/event-platform-boundary-and-external-business-integrations.md`.
- **Acceptance Criteria:**
  - [x] Each record names its trigger, the ISLAMU aggregates it extends, and the report sections it supersedes
- **Effort:** M

---

> **Mandatory payment-phase input:** Implementers for Phases 15–19 and 23–25 must read [`islamic-value-sensitive-design/i-vsd-paid-event-payments-consultation.md`](../../../islamic-value-sensitive-design/i-vsd-paid-event-payments-consultation.md) before acting. Its recipient, refund, currency, payout, self-hosting, disclosure, legal, and Islamic-finance boundaries are acceptance constraints; it is not a certification.

### Phase 15: Commerce And Admission ADR / Contract Gate
- **Goal:** Convert the deferred records, I-VSD payment consultation, and Event/external-business-system boundary into accepted repository authority before any payment, refund, payout, promotion, or admission credential code.
- **Depends on:** Phase 14; ADR-016 through ADR-018 remain accepted prerequisites.
- **Relevant files:** new `docs/adr/ADR-022-paid-event-commerce-and-stripe-connect.md`, `ADR-023-admission-credential-check-in-transfer-recovery.md`, `ADR-024-external-business-integrations-and-protected-payout-boundaries.md`; existing `.agents/contract/intents.yaml`, `docs/{DOMAIN,API,AUTHORIZATION,SECURITY-MODEL,WEBHOOKS,CONFIGURATION,SELF_HOSTING}.md`, `Directory.Packages.props`, `src/Explore.Infrastructure/Explore.Infrastructure.csproj`, this workstream, `deferred-design-records.md`, `dev/report/event-platform-boundary-and-external-business-integrations.md`, and the I-VSD payment consultation. Package files are evidence/planned targets only in Phase 15 and are not modified until Phase 16.
- **Related skills/rules:** `ip-clean-room`, `agentic-research`, `clean-architecture-rules`, `auth-patterns`, `outbox-pattern`, `error-tracking`, `ip-clean-room.md`.
- **Acceptance criteria:** ADR-022 locks D21–D25 plus the official `Stripe.net`/SOLID boundary and the exact in-scope Stripe capability matrix; ADR-023 locks D26–D28 and an online-first scanner threat model; ADR-024 locks D29–D30, keeps marketing/accounting/tax/invoicing external, and records explicit protected-payout legal/provider/scholarly gates. The intent no longer forbids approved event implementation after those ADRs, but does not authorize deferred business-system integrations. No runtime/dependency change occurs in this phase.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Docs/contract only. If any ADR remains Proposed or the intent cannot express the new forbidden moves, stop; Phases 16–25 remain blocked.

#### Task 15.1: Accept ADR-022 for paid-event commerce, merchant authority, and Stripe Connect
- **Type:** create
- **Layer:** Docs/Architecture
- **Files:** `docs/adr/ADR-022-paid-event-commerce-and-stripe-connect.md`; references to ADR-017/018 and `i-vsd-paid-event-payments-consultation.md`.
- **Description:** Record `OrganizerDirect`, actor-bound connected accounts, configuration narrowing, explicit currency, local promotions, `PaymentAttempt`/`RefundAttempt`, direct-charge application-fee/contribution disclosure, cancellation/refund floor, disputes, webhook/idempotency/reconciliation, and self-hosted operator separation. `ProtectedDelayedPayout` is explicitly excluded from the default decision.
- **Acceptance Criteria:** accepted ADR; no fallback admin merchant; return URL is not readiness/payment proof; provider calls outside transactions; one immutable currency/recipient per commercial snapshot.
- **Dependencies:** Phase 14
- **Effort:** M

#### Task 15.2: Accept ADR-023 for admission credentials, check-in, transfer, and recovery
- **Type:** create
- **Layer:** Docs/Architecture/Security
- **Files:** `docs/adr/ADR-023-admission-credential-check-in-transfer-recovery.md`; `docs/SECURITY-MODEL.md` threat-model links.
- **Description:** Define `AdmissionTicket`, opaque QR credential hashing/rotation, entitlement targets, append-only check-in/undo, scoped scanner capabilities, transfer/reissue, generic recovery, online-first validation, and the explicit offline-signing deferral.
- **Acceptance Criteria:** display IDs/email never authorize; no QR PII; transfer rotates; scanner capability is tenant/event/target/action/expiry scoped; batch scan results are per-item.
- **Dependencies:** 15.1
- **Effort:** M

#### Task 15.3: Accept ADR-024 for external business integrations, event-bound breadth, and protected payout
- **Type:** create
- **Layer:** Docs/Architecture
- **Files:** `docs/adr/ADR-024-external-business-integrations-and-protected-payout-boundaries.md`; `dev/report/event-platform-boundary-and-external-business-integrations.md`.
- **Description:** Keep waitlist offers and event-bound add-ons distinct from admission, and keep email marketing, bookkeeping, accounting, tax determination, and legal invoice/credit-note issuance outside Event behind optional integrations. Record that strict payout control is conditional on Stripe, legal, Islamic-finance, and operator approvals and is never escrow.
- **Acceptance Criteria:** Event/external ownership matrix accepted; Listmonk/Qonto integrations remain non-blocking separate workstreams; no add-on-as-ticket or receipt-as-invoice claim; no midnight settlement inference; optional Phase 24 cannot block `OrganizerDirect`.
- **Dependencies:** 15.1, 15.2
- **Effort:** M

#### Task 15.4: Re-baseline contribution contract, primary-doc evidence, and dependency gates
- **Type:** modify + investigate
- **Layer:** Docs/Contract
- **Files:** `.agents/contract/intents.yaml`, `deferred-design-records.md`, `dev/report/event-platform-boundary-and-external-business-integrations.md`, canonical docs named above, `Directory.Packages.props`, `src/Explore.Infrastructure/Explore.Infrastructure.csproj`, dependency policy evidence.
- **Description:** Replace the current “future ADR” implementation prohibition with accepted-ADR references and new phase paths/tests, while explicitly forbidding Event-owned marketing/accounting/tax/invoice scope in this intent. Revalidate and record the exact stable `Stripe.net` version, its pinned Stripe API version, target-framework compatibility, Apache-2.0/license obligations, transitive audit, and webhook endpoint version; the 2026-08-13 research baseline is `Stripe.net` 52.3.0 + `2026-07-29.dahlia`. Define the supported matrix as Connect hosted onboarding/readiness, direct-charge Checkout/payment retrieval, signed Connect webhook intake, refunds/disputes, reconciliation, and conditional payout controls only. Record that Billing/subscriptions, Tax, Invoicing, Payment Links, Terminal, Issuing, Treasury, previews, undocumented parameters, and raw SDK requests are outside this workstream. Refresh QR evidence through Context7 when available; otherwise record official-doc fallback. Run the repository dependency-license policy before selecting any package. Do not inspect third-party source.
- **Acceptance Criteria:** intent parses and names ADR-022..024; source register and SSO decision recorded; stable package/API/webhook versions are mutually compatible and centrally pin-ready; every proposed dependency has compatible outbound-license evidence; no Stripe type may cross Infrastructure; unavailable Context7 is stated, never fabricated.
- **Dependencies:** 15.1–15.3
- **Effort:** M

---

### Phase 16: Paid-Event Policy, Organizer Stripe Connection, And Currency Readiness
- **Goal:** Let an eligible organizer actor connect its own Stripe merchant account and publish paid catalogs only within the effective policy/currency/account intersection.
- **Depends on:** Phase 15.
- **Relevant files:** new Domain `PaidEventPolicyVersion`, `OrganizerPaymentProviderConnection`, readiness/status lookups and pure rules; Persistence configurations/repositories/generated migrations; Application `Features/PaidEventPolicies/**`, `Features/OrganizerPaymentConnections/**` plus a small connected-account capability port; `Explore.Infrastructure/Payments/Stripe/Connect/**` adapter/composition; `Directory.Packages.props`; `src/Explore.Infrastructure/Explore.Infrastructure.csproj`; API/Cerbos/HAL/Studio/Admin surfaces; instance Stripe `SecretBinding` definitions/configuration.
- **Related skills/rules:** Domain/Application/EF/API/HAL/Blazor rules; `auth-patterns`, `outbox-pattern`, `error-tracking`, `ip-clean-room`.
- **Acceptance criteria:** organization-only upstream default; ISLAMU can require locally verified organizations; self-hosters can explicitly allow groups/users; tenant narrows only; Belgium can lock EUR; global choice orders EUR/USD/MAD/SAR/AED but requires organizer confirmation; no bank/KYC data stored; account return navigation never marks ready; connected-account replacement is future-only. The exact stable `Stripe.net` package is centrally pinned once in Infrastructure, uses instance-based `StripeClient` configuration with shared repository-managed HTTP transport, and exposes no SDK type or secret outside Infrastructure.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Disable paid publication and onboarding links without deleting connection/audit history. Unknown Stripe account/capability state fails closed.

#### Task 16.1: Versioned paid-event policy hierarchy and currency rules
- **Status:** Complete. Domain-only policy entities/rules and regressions are independently confirmed; no publication wiring, package, persistence, migration, connection, provider, API, or UI scope was added.
- **Type:** create
- **Layer:** Domain
- **Files:** new paid-policy entities/lookups and `Services/Registration/PaidEventPolicyRules.cs`; existing currency metadata and ticket-catalog publication rules.
- **Description:** Model instance ceilings and tenant narrowing for enabled profiles, organizer actor kinds, local verification, currency set/default/order, sales ceilings, refund floor, and review thresholds. Venue derives a suggestion only; organizer confirmation pins the catalog currency.
- **Acceptance Criteria:** tenant broadening rejected; ambiguous “dirham” absent; zero effective currencies blocks paid publication; all rules pure and exhaustively tested.
- **Dependencies:** Phase 15
- **Effort:** L

#### Task 16.2: Actor-bound organizer payment-provider connection and historical snapshots
- **Status:** Complete. Domain/Application money-recipient aggregate and CQRS flows are independently confirmed; no persistence, secret/config, package, provider I/O, onboarding, callback, reconciliation, API, Cerbos, HAL, UI, or publication scope was added.
- **Type:** create
- **Layer:** Domain/Application
- **Files:** `OrganizerPaymentProviderConnection`, status/rule classes, repositories and CQRS requests/handlers.
- **Description:** Bind provider account identity to tenant + organizer actor + owning instance Connect platform. Store bounded provider readiness/country/capability/requirements summaries, not KYC/bank data. Historical catalog/attempt snapshots never follow account replacement.
- **Acceptance Criteria:** admin/session actor cannot substitute recipient; connection uniqueness is tenant/actor/provider/platform scoped; replacement stops/republishes future paid sales only.
- **Dependencies:** 16.1
- **Effort:** L

#### Task 16.3: Persistence, instance secrets, configuration, and generated migrations
- **Status:** Complete. Normalized policy/connection persistence, portable uniqueness and filters, repository/DI, two instance Stripe secret definitions, five generated provider migrations/snapshots, DBML, staged replacement persistence, and cross-tenant identifier-safe conflicts are independently confirmed. No Task 16.4 runtime Stripe scope was added.
- **Type:** create/modify
- **Layer:** Persistence/Secrets/DevOps
- **Files:** entity configurations/DbSets/query filters/repositories/seeder, `SecretDefinitionRegistry`, configuration validation, generated provider migrations, DBML.
- **Description:** Store Stripe platform key/webhook secret by instance `SecretBinding`; store connected account IDs as provider identity, not secrets. Add portable constraints/indexes and generate all supported-provider migrations through EF tooling.
- **Acceptance Criteria:** tenant/soft-delete filters; no secret values in DB/logs; all five EF models have parity; no migration/snapshot hand edit.
- **Dependencies:** 16.1, 16.2
- **Effort:** L

#### Task 16.4: Stripe hosted onboarding, account events, and readiness reconciliation
- **Type:** create
- **Layer:** Infrastructure/Application/API
- **Files:** `Directory.Packages.props`; `src/Explore.Infrastructure/Explore.Infrastructure.csproj`; Application connected-account capability contract/results; `Explore.Infrastructure/Payments/Stripe/StripeClient` composition and `Connect/**`; Connect webhook verifier/effect handler; reconciliation worker; provider conformance evidence.
- **Description:** After Task 15.4 passes, add the exact stable `Stripe.net` package centrally and reference it only from Infrastructure. Resolve current instance secret/mode through existing secret infrastructure, configure the recommended non-global `StripeClient` over the shared HTTP transport with explicit bounded timeout/network retries, and map `StripeException`/request IDs into bounded provider-neutral outcomes. Persist/reuse the connected account identity, create hosted onboarding links outside transactions, treat return/refresh as navigation, verify signed connected-account events, project `charges_enabled`/requirements/capability state, and reconcile non-ready accounts. Request only required capabilities. Do not add another generic Stripe wrapper, provider factory, preview SDK, raw Stripe API request, or undocumented field.
- **Acceptance Criteria:** stable package/API/license pin matches Task 15.4 and restore audit; no static/global key or SDK model crosses Infrastructure; duplicate/out-of-order account events idempotent; ambiguous account creation reconciles before retry; unavailable/restricted account removes paid-publication HAL relation; tests use the SDK's supported custom-`HttpClient` seam and native fake handler, not live Stripe or SDK-internal mocks.
- **Dependencies:** 16.2, 16.3
- **Effort:** XL

#### Task 16.5: Admin/Studio configuration, publication preflight, Cerbos, and HAL
- **Status:** Complete. Instance and tenant policy administration, exact-organizer paid-commerce authorization, fresh publication preflight, HAL-gated Admin/Studio behavior, bounded hosted onboarding/readiness contracts, regenerated OpenAPI/NSwag/inventory, and canonical documentation are independently confirmed. Browser contracts retain identifiers only as server-side `[JsonIgnore]` HAL metadata.
- **Type:** create/modify
- **Layer:** Application/API/Cerbos/Blazor
- **Files:** Admin paid-policy page, Studio payment connection/readiness surface, event/catalog publish preflight, route/link constants/policies, generated OpenAPI/NSwag/docs.
- **Description:** Instance admins set the ceiling; tenant admins narrow; authorized organizer actors connect/view their merchant and confirm currency. Publish-paid appears only when every policy, authority, verification, connection, capability, disclosure, and currency condition passes.
- **Acceptance Criteria:** all actions relation-gated; return URL cannot enable paid catalog; unsupported choices hidden with safe explanations; contracts private/no-store where account metadata appears.
- **Dependencies:** 16.1–16.4
- **Effort:** XL

---

### Phase 17: Promotion Codes And Immutable Checkout Pricing
- **Status:** Tasks 17.1-17.5, canonical documentation, phase-end verification, and F1-F5 are approved. User acceptance remains the final declaration boundary; Phase 18 has not started.
- **Goal:** Add provider-neutral, reservation-aware discounts before any paid Checkout session is created.
- **Depends on:** Phase 16 and existing order/hold/catalog core.
- **Relevant files:** new `EventPromotion`, `EventPromotionCode`, `PromotionRedemptionReservation`, promotion/status/discount lookups/rules; order/line commercial snapshots; CQRS/API/Cerbos/HAL; Studio promotion and attendee checkout surfaces.
- **Related skills/rules:** Domain/Application/EF/API/HAL/Blazor rules; `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`.
- **Acceptance criteria:** fixed-minor and basis-point discounts; immutable/versioned eligibility; total/per-verified-purchaser/time/currency/ticket/product limits; live reservations count; deterministic integer-minor allocation; no provider promotion authority; code lookup does not log or reveal the code.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Disable new redemption while preserving applied order snapshots. Expiry/cancellation releases only active reservations once.

#### Task 17.1: Promotion definitions, code secrecy, and discount rules
- **Type:** create
- **Layer:** Domain
- **Files:** promotion aggregates/lookups and `PromotionRules`/`DiscountAllocationRules`.
- **Description:** Model event/catalog scope, fixed or percentage discount, optional cap/minimum, window, eligible lines, max uses, per-purchaser rule, and future revocation. Persist a keyed lookup digest/key version and masked suffix; return plaintext only at create/rotate boundary.
- **Acceptance Criteria:** no negative total/line; currency mismatch rejected; percentage uses basis points; organizer cannot edit a published definition in place.
- **Dependencies:** Phase 16
- **Effort:** L

#### Task 17.2: Reservation-aware redemption and immutable order snapshots
- **Type:** create/modify
- **Layer:** Domain/Application
- **Files:** redemption reservation, `RegistrationOrder`/line discount snapshots, hold/finalization/expiry services.
- **Description:** Reserve promotion usage atomically with inventory/order pricing, count live unexpired plus confirmed use, consume on finalization, release on expiry/cancel, and snapshot exact definition/version/code mask/allocation.
- **Acceptance Criteria:** concurrent final use has one winner; retry is idempotent; unverified guest per-person limit is never advertised as hard.
- **Dependencies:** 17.1
- **Effort:** L

#### Task 17.3: Persistence constraints, locking, and generated migrations
- **Type:** create/modify
- **Layer:** Persistence
- **Files:** configurations/repository/DbSets/filters/generated migrations/DBML.
- **Description:** Portable uniqueness and state constraints; deterministic lock ordering with capacity holds; filtered/provider-specific indexes only when portable alternatives cannot preserve correctness.
- **Acceptance Criteria:** real persistence races cover last redemption, expiry-versus-confirm, and cross-tenant code equality; five-provider model parity.
- **Dependencies:** 17.2
- **Effort:** L

#### Task 17.4: Promotion CQRS, API, authorization, rate limits, and HAL
- **Type:** create
- **Layer:** Application/API/Cerbos
- **Files:** `Features/EventPromotions/**`, controller, policies, routes/relations, endpoint classification, OpenAPI/NSwag.
- **Description:** Organizer CRUD/publish/revoke and order-scoped apply/remove commands. PublicTransactional redemption is order-capability scoped, rate-limited, idempotent, generic on invalid codes, and server recalculates from pinned facts.
- **Acceptance Criteria:** contributor/tenant admin without commercial authority denied; invalid/exhausted codes do not reveal reason useful for enumeration; UI has no capability booleans.
- **Dependencies:** 17.1–17.3
- **Effort:** L

#### Task 17.5: Studio and checkout promotion UX
- **Type:** create/modify
- **Layer:** Blazor
- **Files:** `/studio/events/{eventId}/promotions`, checkout pricing summary, isolated CSS/tests.
- **Description:** HAL-gated promotion management; accessible apply/remove/status; display organizer amount, discount, fee, contribution, and final total separately in one currency.
- **Acceptance Criteria:** no relation means no action/navigation; masked codes only after creation; totals announced accessibly and RTL-safe.
- **Dependencies:** 17.4
- **Effort:** M

---

### Phase 18: Payment Attempts, Stripe Direct-Charge Checkout, And Reconciliation
- **Goal:** Move positive orders from `AwaitingPayment` to confirmed only through durable, reconciled Stripe Connect direct-charge evidence.
- **Depends on:** Phases 16–17.
- **Relevant files:** new `PaymentAttempt` + lookups/rules/repository; small Application Checkout/payment-retrieval contracts; `Explore.Infrastructure/Payments/Stripe/Checkout/**` adapter and shared Stripe composition; exact-byte webhook verifier/normalized inbox translator; outbox/effect/reconciliation worker; checkout API/BFF/Blazor; order lifecycle integration.
- **Related skills/rules:** `outbox-pattern`, `cqrs-mediatr-guidelines`, `auth-patterns`, `blazor-bff-patterns`, `error-tracking`, all layer rules and IP/dependency gate.
- **Acceptance criteria:** one provider-neutral aggregate and capability-focused Stripe adapters with no SDK leakage; direct charge on snapshotted organizer account; final amount/currency immutable; disclosed platform fee/contribution separate; hosted Checkout; no card data; return navigation not completion; signed duplicate-safe webhooks; reconciliation for timeout/delay/orphans; payment and order states independent. SDK retry, request timeout, `StripeException`, request-ID telemetry, connected-account header, API version, and webhook version behavior are explicit and tested.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` *(repeat justified: Phase 16 covers account onboarding; Phase 18 adds the separate money-moving Checkout/webhook adapter contract)*
- **Rollback / failure handling:** Disable new Checkout creation; preserve attempts and continue webhook/reconciliation processing. Never delete or recreate an ambiguous accepted attempt.

#### Task 18.1: `PaymentAttempt` aggregate, snapshots, and monotonic rules
- **Type:** create
- **Layer:** Domain
- **Files:** payment aggregate/status lookups/rules; `RegistrationOrder` relationship.
- **Description:** Store order, recipient actor/account, merchant country, provider/profile/API revision, currency, organizer amount, platform fee/contribution, total, idempotency identity, provider IDs, expiry, and state transitions (`Created/DispatchPending/RequiresAction/Processing/Succeeded/Failed/Cancelled/Unknown`).
- **Acceptance Criteria:** totals compose exactly; recipient/currency immutable; no provider enum controls domain behavior; success/failure cannot regress.
- **Dependencies:** Phase 17
- **Effort:** L

#### Task 18.2: Durable creation claim, persistence, outbox, and generated migrations
- **Type:** create
- **Layer:** Application/Persistence
- **Files:** repository/configurations/DbSets/filters, checkout-request outbox/effect, generated migrations.
- **Description:** Atomically claim one payable order attempt and persist stable provider idempotency before dispatch. Concurrent start requests return the existing active attempt/session status.
- **Acceptance Criteria:** no duplicate active attempt for one order/composition revision; transaction contains no provider call; five-provider EF parity.
- **Dependencies:** 18.1
- **Effort:** L

#### Task 18.3: Stripe direct-charge Checkout adapter
- **Type:** create
- **Layer:** Infrastructure
- **Files:** Application Checkout/payment-retrieval contracts and result types; `Explore.Infrastructure/Payments/Stripe/Checkout/**`; shared Stripe client/error mapper; deterministic transport/conformance fixtures.
- **Description:** Use `StripeClient.V1` Checkout services asynchronously to create hosted Checkout in the connected-account context using the exact immutable currency/line totals and disclosed application-fee/contribution composition. Every mutation supplies `RequestOptions.StripeAccount` from the immutable recipient snapshot and the persisted `RequestOptions.IdempotencyKey`; retrieval uses the same account context. Supply bounded metadata IDs only, success/cancel same-origin URLs, and supported payment methods/currency from live capability. Capture provider object/request identifiers, map `StripeException` type/code/status into bounded outcomes, honor cancellation before dispatch, and treat cancellation/timeout after possible dispatch as ambiguous until retrieval proves otherwise.
- **Acceptance Criteria:** no Stripe SDK type in Application/API; no PII in metadata/idempotency/logs; no adaptive pricing; no independent Polly retry around provider mutations; explicit bounded SDK network retry/timeout policy; HTTP timeout/connection ambiguity becomes `Unknown` and reconciliation, not blind recreation; connected-account header and stable idempotency are asserted through the custom-`HttpClient` test seam.
- **Dependencies:** 18.2
- **Effort:** XL

#### Task 18.4: Signed Connect webhook intake and payment reconciliation
- **Type:** create
- **Layer:** API/Application/Infrastructure
- **Files:** callback endpoint; `EventUtility` verifier and provider-neutral envelope translator; incoming effect kind/handler; reconciliation worker/health projection; signed webhook fixtures.
- **Description:** Read the raw UTF-8 body once and verify it synchronously with `EventUtility.ConstructEvent`, the endpoint-specific secret, default non-zero timestamp tolerance, and strict SDK API-version matching. Validate `livemode`, top-level connected-account ID, allowlisted event family, provider object ID, and payload bounds; durably insert a minimal normalized envelope with unique event ID/payload hash before returning `2xx`, then retrieve authoritative Checkout/payment state in the connected-account context and apply monotonic transitions asynchronously. Dedupe both event ID and `(event type, provider object ID)`; periodically retrieve non-terminal/unknown attempts.
- **Acceptance Criteria:** webhook endpoint version equals `StripeConfiguration.ApiVersion`; API-version mismatch/signature failure/wrong mode/unknown account are rejected or parked with bounded alerts, never parsed loosely; duplicate/delayed/out-of-order fixtures use `EventUtility.GenerateSignatureHeader`; no raw buyer/card payload enters general logs; unknown/orphan event is parked without tenant disclosure; handler acknowledges promptly only after durable intake and processes business effects asynchronously.
- **Dependencies:** 18.2, 18.3
- **Effort:** XL

#### Task 18.5: Paid-order finalization and capacity race integration
- **Type:** modify
- **Layer:** Application/Domain
- **Files:** registration-order lifecycle/finalization service and payment effect handler.
- **Description:** A reconciled successful payment becomes one input to existing requirements/approval/capacity finalization. Conditional transitions ensure payment success, hold expiry, cancellation, and duplicate effects cannot double-confirm or oversell; admission issuance remains an outbox trigger for Phase 20.
- **Acceptance Criteria:** payment success alone cannot bypass missing requirements/approval/capacity; duplicate success returns original result; paid failure does not corrupt holds/order history.
- **Dependencies:** 18.4
- **Effort:** L

#### Task 18.6: Checkout/status API, BFF, Blazor, HAL, and generated contracts
- **Type:** create/modify
- **Layer:** API/Cerbos/Blazor
- **Files:** order payment endpoints/policies/links, BFF redirect endpoint, attendee checkout/recovery status, Studio payment view, OpenAPI/NSwag/docs.
- **Description:** Start payment is PublicTransactional/idempotent/order-capability scoped; BFF performs safe same-origin redirection; client polls authoritative status and renders processing/unknown/failed/retry states. Studio sees bounded reconciliation state, never card/customer secrets.
- **Acceptance Criteria:** no link means no action; success URL cannot confirm; retry relation only for safely retryable/reconciled state; private/no-store responses.
- **Dependencies:** 18.3–18.5
- **Effort:** XL

---

### Phase 19: Refunds, Cancellation, Rescheduling, Disputes, And Buyer Protection
- **Goal:** Implement the I-VSD refund floor and truthful asynchronous financial closure for paid events.
- **Depends on:** Phase 18.
- **Relevant files:** new refund-policy snapshots, `RefundAttempt`, dispute projection/rules/repositories; event cancellation/reschedule commands; Application refund/retrieval contracts; `Explore.Infrastructure/Payments/Stripe/Refunds/**` adapter plus webhook observation mapping; outbox/reconciliation; attendee/Studio support surfaces.
- **Related skills/rules:** `outbox-pattern`, Domain/Application/EF/API/HAL/Blazor rules, `error-tracking`, `auth-patterns`.
- **Acceptance criteria:** cancellation stops sales atomically; one idempotent refund intent per captured payment/amount; original connected account only; pending insufficient-balance truth; partial/full refunds; contribution/fee refund policy explicit; disputes projected; reschedule/material-change choice; audit and notification; no editable terms bypass.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep event non-selling and refund attempts reconciling. Operator intervention may retry/reconcile, never mark success manually without provider evidence.

#### Task 19.1: Versioned refund floor and order acceptance snapshot
- **Type:** create/modify
- **Layer:** Domain/Application
- **Files:** refund policy/version/rules, event paid-publication requirements, order commercial snapshot.
- **Description:** Encode mandatory cancellation, material-change, duplicate/incorrect-charge, non-delivery, and buyer-change-of-mind floor; organizers may be more generous only. Snapshot buyer-visible text/version and support/merchant disclosures before payment.
- **Acceptance Criteria:** tenant/organizer cannot weaken instance floor; policy change future-only; accepted text and language retained.
- **Dependencies:** Phase 18
- **Effort:** L

#### Task 19.2: `RefundAttempt` and dispute projection domain/persistence
- **Type:** create
- **Layer:** Domain/Persistence
- **Files:** refund/dispute entities/status/rules/configurations/repositories/generated migrations.
- **Description:** Independent requested/pending/succeeded/failed/requires-action/unknown lifecycle with original charge/account/currency, amount allocation, reason, provider identity/idempotency, and audit. Disputes are provider projections, not locally editable truth.
- **Acceptance Criteria:** cumulative successful/pending refund cannot exceed captured amount; monotonic transitions; provider/account immutable; model parity.
- **Dependencies:** 19.1
- **Effort:** L

#### Task 19.3: Cancellation/reschedule workflow and transactional refund fanout
- **Type:** create/modify
- **Layer:** Application
- **Files:** event lifecycle commands/handlers, order query/repository paths, refund outbox messages, notification intents.
- **Description:** Cancellation immediately stops sales and writes retry-stable refund work for all affected captured payments in the same local transaction. Material change/reschedule notifies buyers and records accept-new-terms or refund choice. Deletion with paid orders is rejected.
- **Acceptance Criteria:** restart-safe fanout; zero missed/double jobs; large events bounded/paged; failures do not reopen sales.
- **Dependencies:** 19.2
- **Effort:** XL

#### Task 19.4: Stripe refunds/disputes, webhooks, and reconciliation
- **Type:** create
- **Layer:** Infrastructure/Application/API
- **Files:** Application refund/retrieval contracts and result types; `Explore.Infrastructure/Payments/Stripe/Refunds/**`; shared Stripe error mapper; webhook observation mappings; reconciliation/health worker; deterministic transport/signed-event fixtures.
- **Description:** Use the exact-pinned `Stripe.net` refund/retrieval services outside transactions. Every create/retrieve supplies the original `RequestOptions.StripeAccount`; every mutation supplies the persisted `RequestOptions.IdempotencyKey`. Map provider refund/dispute objects and `StripeException` outcomes to bounded local observations, capture request IDs, process only allowlisted signed refund/dispute event families, and retrieve authoritative pending/unknown state. Explicitly decide application-fee/contribution refund allocation from the snapshotted policy; never infer allocation from mutable current settings.
- **Acceptance Criteria:** custom-transport assertions prove original account/idempotency and no double retry; insufficient-balance pending, partial/full, duplicate, rate-limit, timeout, late-success, and dispute lifecycle fixtures; application-fee behavior explicit; dispute creates bounded action projection; no platform/recipient substitution; no raw Stripe error or SDK model escapes Infrastructure.
- **Dependencies:** 19.2, 19.3
- **Effort:** XL

#### Task 19.5: Buyer/organizer refund and cancellation surfaces, HAL, metrics, and runbook
- **Type:** create/modify
- **Layer:** Application/API/Cerbos/Blazor/Ops
- **Files:** refund commands/queries/controllers/policies/links, Studio payments, attendee order self-service, metrics/health/docs, OpenAPI/NSwag.
- **Description:** Authorized organizers can refund within policy; trust/safety cancellation is separately authorized; buyers request eligible refunds and view exact state. Metrics use bounded provider/state/reason categories; runbook covers pending balance, disputes, failed fanout, reconciliation, and communications.
- **Acceptance Criteria:** “refunded” only on succeeded; every action HAL-gated/audited; PII-free telemetry; generic cross-tenant responses.
- **Dependencies:** 19.1–19.4
- **Effort:** XL

---

### Phase 20: `AdmissionTicket`, QR Credential, Delivery, Lookup, And Self-Service
- **Goal:** Issue independently revocable admission credentials after free confirmation or reconciled paid confirmation and let account/guest holders recover them safely.
- **Depends on:** Phase 19 for paid orders; existing confirmed free path remains valid.
- **Relevant files:** new `AdmissionTicket`, credential status/version/rules/repository; issuance outbox/handler; QR renderer/decoder dependency decision; recovery capability/delivery; ticket API/BFF/Blazor/email templates.
- **Related skills/rules:** Domain/Application/EF/API/HAL/BFF/Blazor rules, `outbox-pattern`, `auth-patterns`, `ip-clean-room`.
- **Acceptance criteria:** one ticket per concrete assignment; issue idempotently; high-entropy opaque QR with keyed hash only and no PII; rotation/revocation; accessible printable/digital surface; generic resend; account tickets and guest capability; no offline validation claim; dependency license gate.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Stop new rendering/delivery while preserving tickets; manual organizer lookup uses authenticated bounded details, never bypasses credential state.

#### Task 20.1: Admission ticket and credential lifecycle domain
- **Type:** create
- **Layer:** Domain
- **Files:** `AdmissionTicket`, status/reason lookups, `AdmissionCredentialRules`.
- **Description:** Link tenant/event/order/line/assignment/participant and immutable catalog facts. Model Active/Suspended/Revoked/Cancelled/Transferred/Expired plus credential generation/version/rotation metadata and display reference separate from authority.
- **Acceptance Criteria:** no ticket before confirmed order; one live credential; public ID never validates; cancellation/full relevant refund revokes through explicit transition.
- **Dependencies:** Phase 19
- **Effort:** L

#### Task 20.2: Idempotent issuance, persistence, and generated migrations
- **Type:** create
- **Layer:** Application/Persistence
- **Files:** issuance service/outbox handler, repository/configurations/DbSets/filters/generated migrations.
- **Description:** Consume confirmation effect and issue one ticket per assignment with CSPRNG credential, keyed lookup digest/key version, and retry-stable IDs. Store no plaintext token after issuance response materialization.
- **Acceptance Criteria:** replay produces no duplicate; assignment/ticket uniqueness; tenant filters; five-provider parity.
- **Dependencies:** 20.1
- **Effort:** L

#### Task 20.3: QR representation and clean-room encoder/decoder gate
- **Type:** investigate + create
- **Layer:** Infrastructure/Blazor
- **Files:** dependency evidence, QR rendering service, client scanning abstraction used later by Phase 21.
- **Description:** Select the smallest outbound-license-compatible QR encoder/decoder after official docs/license validation. Encode only version + opaque token. Feature-detect native `BarcodeDetector` but supply supported-library, HID, and manual fallbacks; apply size/error-correction/contrast/accessibility limits.
- **Acceptance Criteria:** dependency-policy command green; no third-party source ingestion; deterministic decode fixtures; no secret in logs/DOM persistence/local storage.
- **Dependencies:** Phase 15.4, 20.2
- **Effort:** L

#### Task 20.4: Ticket lookup, resend, and account/guest recovery
- **Type:** create
- **Layer:** Domain/Application/API
- **Files:** recovery capability purpose, commands/queries/controller/rate policy/email outbox.
- **Description:** Account users list authorized tickets. Guests request a same-origin recovery link using uniform responses/timing, rate limits, random single-use expiring hashed capabilities, and verified side-channel delivery. Resend rotates recovery capability, not admission credential unless reissue is requested.
- **Acceptance Criteria:** email/display ID never grants access; cross-tenant and absent address indistinguishable; tokens no-store/redacted; abuse limits and audit.
- **Dependencies:** 20.2
- **Effort:** L

#### Task 20.5: Ticket API/HAL/BFF/Blazor delivery and self-service
- **Type:** create/modify
- **Layer:** API/Cerbos/BFF/Blazor
- **Files:** ticket detail/QR/print endpoints, BFF capability bridge, `/tickets/**` pages, attendee order links, email templates, OpenAPI/NSwag/docs.
- **Description:** Render ticket identity, event/session entitlements, holder, status, QR, and support/refund/transfer affordances from HAL. Serve QR only after account/order/recovery capability authorization and never cache privately scoped content.
- **Acceptance Criteria:** accessible non-QR text/manual code; print and mobile layouts; no local role/status action gating; revoked ticket clearly non-validating.
- **Dependencies:** 20.3, 20.4
- **Effort:** XL

---

### Phase 21: Admission Targets, Online Check-In, Scanner Capabilities, And Undo
- **Goal:** Give authorized event staff reliable online camera/HID/manual check-in with append-only audit and entitlement-aware results.
- **Depends on:** Phase 20.
- **Relevant files:** new admission target/policy/check-in entities/rules/repositories; check-in CQRS/API/Cerbos/HAL; scanner capability; Studio check-in UI and bounded metrics.
- **Related skills/rules:** Domain/Application/EF/API/HAL/BFF/Blazor rules, `auth-patterns`, `error-tracking`.
- **Acceptance criteria:** event/day/session targets; single-entry or configured re-entry; time windows; atomic duplicate protection; compensating undo; revoked/transferred/wrong-scope rejection; scoped scanner tokens; camera/HID/manual parity; per-item batch response; no offline claim.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Disable scanner capability issuance; authenticated manual lookup remains. Existing check-in facts are never deleted.

#### Task 21.1: Admission targets, policies, and append-only event model
- **Type:** create
- **Layer:** Domain
- **Files:** admission target/check-in policy/event/status lookups and rules.
- **Description:** Resolve target from event/day/session and ticket entitlements; model early/late window, entry count/re-entry, and CheckIn/Undo facts with actor/scanner/reason/time.
- **Acceptance Criteria:** target not entitled rejected; undo requires active check-in and reason; no mutable Boolean source of truth.
- **Dependencies:** Phase 20
- **Effort:** L

#### Task 21.2: Atomic persistence and check-in/undo concurrency
- **Type:** create
- **Layer:** Persistence/Application
- **Files:** repositories/configurations/DbSets/filters/generated migrations, command handlers.
- **Description:** Resolve credential hash tenant-safely, lock active ticket/target state, append one fact, and return idempotent AlreadyCheckedIn/NotCheckedIn outcomes. Batch has bounded size and transaction per item or bounded chunk, not all-or-nothing.
- **Acceptance Criteria:** concurrent duplicate has one active result; undo/check-in race deterministic; cross-event/tenant generic reject; model parity.
- **Dependencies:** 21.1
- **Effort:** XL

#### Task 21.3: Check-in API, Cerbos, HAL, and scanner capability issuance
- **Type:** create
- **Layer:** Application/API/Cerbos
- **Files:** controllers/policies/routes/relations, scanner capability service, rate policies, OpenAPI/NSwag.
- **Description:** Normal authenticated staff actions plus organizer-issued scanner capabilities scoped to tenant/event/targets/actions/expiry/device label and individually revocable. API returns bounded holder/ticket/result data needed at the door.
- **Acceptance Criteria:** capability cannot browse attendees or other events; expiry/revocation immediate; no secret in response after issuance; HAL controls issue/revoke/check-in/undo.
- **Dependencies:** 21.2
- **Effort:** XL

#### Task 21.4: Camera, HID, and manual scanner client flow
- **Type:** create
- **Layer:** Blazor/BFF
- **Files:** `/studio/events/{eventId}/check-in`, scanner components/JS isolation/CSS/tests.
- **Description:** Camera uses approved decoder and secure-context permission handling; HID keyboard scanners and manual input feed the same bounded queue. Prevent duplicate rapid submissions; announce success/already/wrong/revoked without relying on color/sound.
- **Acceptance Criteria:** exact HAL gate; accessible focus/live regions; RTL; graceful camera denial/unsupported browser; no token persistence.
- **Dependencies:** 20.3, 21.3
- **Effort:** XL

#### Task 21.5: Check-in summary, export-safe audit, observability, and runbook
- **Type:** create/modify
- **Layer:** Application/API/Blazor/Ops
- **Files:** summary queries/HAL, bounded metrics, audit/runbook/docs.
- **Description:** Provide counts by target/status without raw PII metrics, authorized minimal audit, scanner health, and procedures for device loss, credential compromise, mistaken scan/undo, queue saturation, and connectivity outage.
- **Acceptance Criteria:** cardinality bounded; no admission credential in audit/log; summary cannot enumerate outside organizer authority.
- **Dependencies:** 21.2–21.4
- **Effort:** M

---

### Phase 22: Ticket Transfer, Reissue, Reassignment, And Holder Self-Service
- **Goal:** Transfer future admission safely without rewriting purchase/payment/consent history or leaving copied QR credentials valid.
- **Depends on:** Phases 20–21.
- **Relevant files:** transfer policy on ticket catalog/type; new `TicketTransferOffer` + split PII/capability; transfer CQRS/API/Cerbos/HAL/notifications; self-service and Studio correction UI.
- **Related skills/rules:** Domain/Application/EF/API/HAL/BFF/Blazor rules, `outbox-pattern`, `auth-patterns`.
- **Acceptance criteria:** published transfer policy; pending/accepted/declined/cancelled/expired; acceptance capability; required transferee data; atomic assignment/holder change and credential rotation; checked-in/expired/nontransferable denial; consent not copied; no resale/money movement.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Disable new offers; pending offers can expire/cancel. Never restore a revoked credential; issue a fresh credential through audited reissue.

#### Task 22.1: Published transfer policy and domain rules
- **Type:** create/modify
- **Layer:** Domain
- **Files:** ticket type/catalog transfer policy snapshots and rules.
- **Description:** Configure allowed flag, deadline, maximum transfers, checked-in restriction, recipient/guardian/company constraints, and whether organizer approval is required. Published/in-flight facts immutable.
- **Acceptance Criteria:** policy cannot change existing eligibility silently; transfer never changes merchant/currency/price/refund policy.
- **Dependencies:** Phase 21
- **Effort:** M

#### Task 22.2: Transfer offer, recipient PII, capability, persistence, and migrations
- **Type:** create
- **Layer:** Domain/Persistence
- **Files:** transfer entities/status/rules/configurations/repositories/generated migrations.
- **Description:** Store offer lifecycle and split/encrypted recipient contact; acceptance token is random, purpose-scoped, single-use, expiring, keyed-hashed. One active offer per ticket.
- **Acceptance Criteria:** no token/plain email logs; cross-tenant generic lookup; concurrent accept/cancel/expire has one winner; model parity.
- **Dependencies:** 22.1
- **Effort:** L

#### Task 22.3: Atomic acceptance, participant requirements, and credential rotation
- **Type:** create
- **Layer:** Application
- **Files:** transfer commands/handlers, participant/assignment/form fulfillment integration, admission credential service/outbox.
- **Description:** Invite, accept, decline, cancel, expire. Acceptance verifies/recollects required transferee data and consent, then atomically reassigns holder/participant, accepts offer, revokes old credential, and issues the new credential/outbox notification.
- **Acceptance Criteria:** retry idempotent; copied old QR immediately invalid; purchaser/order/payment/audit unchanged; adult consent never inherited.
- **Dependencies:** 22.2
- **Effort:** XL

#### Task 22.4: Organizer correction/reissue versus holder transfer
- **Type:** create
- **Layer:** Application/API/Cerbos
- **Files:** organizer reassign/reissue commands, audit reason codes, policies/HAL.
- **Description:** Separate data correction, credential reissue, and organizer-forced reassignment from holder transfer. Require commercial/admission authority, reason, notification, and affected-state checks.
- **Acceptance Criteria:** no silent transfer; contributors denied; reissue rotates even for same holder; checked-in override is explicit policy/action.
- **Dependencies:** 22.3
- **Effort:** L

#### Task 22.5: Transfer/self-service API, notifications, and Blazor surfaces
- **Type:** create/modify
- **Layer:** API/BFF/Blazor
- **Files:** transfer/reissue endpoints/links, ticket pages, Studio attendees/check-in, email templates, OpenAPI/NSwag/docs.
- **Description:** Holder actions come only from ticket HAL; recipient link enters a capability-scoped same-origin flow; organizer actions stay in Studio. Show deadlines, policy, pending state, and exact effects accessibly.
- **Acceptance Criteria:** no enumeration; action absent after expiry/check-in/revocation; recipient sees minimum pre-acceptance data; all notifications outbox-driven.
- **Dependencies:** 22.3, 22.4
- **Effort:** XL

---

### Phase 23: Waitlist Offers, Event-Bound Add-Ons, And Separate Fulfillment
- **Goal:** Add remaining event-bound checkout breadth without becoming a general commerce/accounting product, polluting ticket/admission semantics, or weakening shared-capacity correctness.
- **Depends on:** Phases 17–22.
- **Relevant files:** waitlist entry/offer/policy; capacity/hold integration; event-add-on catalog/order lines/inventory/fulfillment; CQRS/API/HAL/Blazor/notifications.
- **Related skills/rules:** Domain/Application/EF/API/HAL/Blazor rules, `outbox-pattern`, `dotnet-efcore-guidelines`.
- **Acceptance criteria:** fair explicit waitlist policy; bounded expiring offers reserve shared capacity; atomic accept/expire; add-ons are event-scoped and never create entitlements/check-in; one currency/merchant per mixed order; promotion eligibility explicit; fulfillment independent from admission/refund allocation; tax/accounting/invoice ownership absent from Event.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Disable new waitlist offers/add-ons; preserve offers/orders/fulfillment history. Expiry releases reserved capacity/inventory once.

#### Task 23.1: Waitlist policy, entry, and offer domain
- **Type:** create
- **Layer:** Domain
- **Files:** waitlist policy/entry/offer/status/rules.
- **Description:** Event/ticket/pool scope, explicit FIFO or approved priority policy, join identity, expiry, max active offers, and states. Entry does not equal order or admission.
- **Acceptance Criteria:** deterministic ordering; privacy-minimized public position; one active entry/offer per scoped identity.
- **Dependencies:** Phase 22
- **Effort:** L

#### Task 23.2: Capacity-triggered offer reservation and persistence races
- **Type:** create
- **Layer:** Application/Persistence
- **Files:** waitlist repositories/configurations/migrations, capacity release outbox handler, offer acceptance/expiry worker.
- **Description:** Released capacity schedules bounded offers; offer atomically owns a normal inventory hold until expiry; acceptance creates/continues an order; timeout releases once and advances queue.
- **Acceptance Criteria:** no shared-pool oversell; cancellation/offer/normal checkout race deterministic; retry/fanout idempotent; model parity.
- **Dependencies:** 23.1
- **Effort:** XL

#### Task 23.3: Waitlist API, HAL, notifications, and self-service
- **Type:** create
- **Layer:** Application/API/Cerbos/Blazor
- **Files:** commands/queries/controllers/policies/links, event/checkout/Studio pages, email outbox, contracts/docs.
- **Description:** Join/leave/accept via account or order-scoped capability; organizer sees bounded queue/offer health and policy actions. Generic public responses resist event-contact enumeration.
- **Acceptance Criteria:** HAL-only actions; expired offer cannot revive; notifications contain same-origin capability only; accessible countdown/state.
- **Dependencies:** 23.2
- **Effort:** L

#### Task 23.4: Event-add-on catalog and mixed-order snapshots
- **Type:** create
- **Layer:** Domain/Persistence
- **Files:** event-add-on/catalog/version/order-line/fulfillment entities/rules/configurations/migrations.
- **Description:** Optional event-scoped merchandise or service add-on with immutable name, final configured price, promotion eligibility, and separate finite/unlimited inventory. Mixed order keeps one connected merchant and currency; add-ons do not assign participants or entitlements. Tax/accounting classification and legal documents stay external.
- **Acceptance Criteria:** no add-on line in admission/check-in queries; no general cross-event storefront; deterministic totals/refund allocation; provider-portable model parity; no Event tax/invoice aggregate.
- **Dependencies:** Phase 18, 23.1
- **Effort:** XL

#### Task 23.5: Event-add-on inventory, fulfillment, promotion, payment, and UI integration
- **Type:** create/modify
- **Layer:** Application/API/Cerbos/Blazor
- **Files:** event-add-on/fulfillment CQRS, checkout composition, promotion/payment/refund integration, Studio add-on/orders UI, contracts/docs.
- **Description:** Reserve/consume/release add-on inventory with order holds; route line totals through existing payment/refund snapshots; fulfill independently with auditable status and no check-in effects.
- **Acceptance Criteria:** partial add-on refund does not revoke ticket; cancelled ticket allocation does not cancel a delivered add-on unless policy says; all controls HAL-gated; no marketing/accounting/invoice UI is introduced.
- **Dependencies:** 23.4
- **Effort:** XL

---

### Phase 24: Conditional `ProtectedDelayedPayout` Profile
- **Goal:** Implement strict release control only if every approval in D30 is real; otherwise keep the profile unavailable without blocking normal paid events.
- **Depends on:** Phase 19 plus Stripe, legal, Islamic-finance, and operator approvals; independent of deferred finance integrations.
- **Relevant files:** ADR-024 approval addendum; settlement policy/release aggregate; separate Application payout-control contract; `Explore.Infrastructure/Payments/Stripe/Payouts/**` adapter using the already pinned SDK only when supported by the approved contract; risk/reconciliation/incident surfaces.
- **Related skills/rules:** Domain/Application/EF/API/HAL/Blazor rules, `outbox-pattern`, `error-tracking`, `ip-clean-room`.
- **Acceptance criteria:** explicit `SettlementReleaseAt`, country holding-limit validation, non-escrow disclosure, stop/review/cancel/refund gates, operator liability/reserve ownership, connected-account/dashboard constraints, reconciliation. No normal configuration path can bypass missing approvals.
- **Phase-end verification (run once after all tasks, only when the phase is unblocked):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Disable new protected catalogs; preserve existing release/refund obligations and reconciliation. Never silently fall back to `OrganizerDirect` after a buyer accepted held-release terms.

#### Task 24.1: Approval evidence and exact Stripe control contract
- **Type:** investigate + modify
- **Layer:** Docs/Configuration
- **Files:** ADR-024 addendum, provider conformance/approval record, legal/scholarly/operator evidence references.
- **Description:** Pin platform/connected countries, fees/losses controller, Dashboard/control type, payout schedule/manual capability, holding limit, reserve/complaint/dispute owner, and exact buyer/organizer disclosures.
- **Acceptance Criteria:** all approvals dated and attributable; any missing fact keeps profile disabled; “escrow” absent.
- **Dependencies:** Phase 15.3, Phase 19
- **Effort:** L

#### Task 24.2: Settlement milestone, hold-limit, and release domain/persistence
- **Type:** create
- **Layer:** Domain/Persistence
- **Files:** settlement policy/release/status/rules/configurations/repositories/migrations.
- **Description:** Snapshot explicit release timestamp/milestone and review buffer, independent of public end time. Validate sale window against country limit; block release on cancellation, suspension, dispute, unresolved refund, account restriction, or operator review.
- **Acceptance Criteria:** multi-day/open-ended/online scenarios; release exactly once; no midnight inference; model parity.
- **Dependencies:** 24.1
- **Effort:** XL

#### Task 24.3: Stripe payout controls, release worker, and reconciliation
- **Type:** create
- **Layer:** Application/Infrastructure
- **Files:** separate Application payout-control contract/results; `Explore.Infrastructure/Payments/Stripe/Payouts/**`; outbox worker; allowlisted provider event mappings; reconciliation/health.
- **Description:** Only after Task 24.1 proves the stable pinned SDK/API supports the exact approved control model, configure/monitor the payout schedule, issue release outside transactions with request-scoped connected account and stable idempotency, map provider errors/request IDs, and reconcile payout/balance/account states. Detect organizer/manual paths that invalidate the promise and stop sales/escalate. If the required operation needs a preview, undocumented parameter, raw Stripe API request, or broader controller role, keep the profile disabled and return to the approval gate.
- **Acceptance Criteria:** no conditional payout code contaminates the default Checkout/refund adapters; ambiguous release reconciles; stale worker fenced; no release with unresolved blockers; account/control drift alert; SDK/API support is stable, typed, and conformance-tested.
- **Dependencies:** 24.2
- **Effort:** XL

#### Task 24.4: Protected-profile publication, disclosure, HAL/UI, and incident operations
- **Type:** create/modify
- **Layer:** Application/API/Cerbos/Blazor/Ops
- **Files:** publish preflight, buyer/organizer/operator pages/links, metrics/health/runbook, contracts/docs.
- **Description:** Profile appears only under approved instance policy and eligible country/account/window. Disclose non-escrow, release milestone, operator, remedies, and risk. Provide stop-sale/review/reconcile/release actions with separation of duties and durable audit.
- **Acceptance Criteria:** tenant/organizer cannot enable; buyer acceptance snapshotted; all actions HAL-gated; incident ownership and alerts testable.
- **Dependencies:** 24.1–24.3
- **Effort:** XL

---

### Phase 25: Self-Hosted Safety, Official-Instance Trust, Pilot Hardening, And Closeout
- **Goal:** Make the implemented commerce/admission stack operable without implying that every fork is ISLAMU-protected.
- **Depends on:** Phases 16–23; Phase 24 only if approved/enabled.
- **Relevant files:** configuration/secrets/health/metrics/runbooks/admin disclosures; risk/complaint/review controls; integration fixtures; canonical docs/OpenAPI/NSwag/DBML/intent/workstream closeout.
- **Related skills/rules:** `error-tracking`, `ip-clean-room`, `auth-patterns`, `outbox-pattern`, all affected rules.
- **Acceptance criteria:** every self-hoster uses own Connect platform; official/unofficial operator disclosure; test/live mode separation; secret/webhook rotation; bounded reconciliation health; organizer/event risk ceilings and stop-sale; complaints/incidents/disputes ownership; legal/scholarly/Stripe launch checklist; no production-ready claim from test fixtures alone.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Global payment kill switch stops new paid publication/Checkout but preserves webhook/refund/reconciliation/admission reads. Admission kill switches are target/action specific and preserve audit.

#### Task 25.1: Self-hosted configuration, secret rotation, and operator-origin disclosure
- **Type:** modify/create
- **Layer:** DevOps/API/Blazor/Docs
- **Files:** structured config/validation, secret definitions/rotation, setup/admin/public disclosure surfaces, `docs/{CONFIGURATION,SECRETS,SELF_HOSTING,SECURITY-MODEL}.md`.
- **Description:** Separate test/live Stripe keys and webhook endpoints; validate public origins; rotate secrets with overlap; disclose instance operator and whether official ISLAMU-hosted; never distribute ISLAMU credentials/brand trust to forks.
- **Acceptance Criteria:** startup fails closed on mixed mode/origin/secret mismatch; redacted diagnostics; official marker cannot be set through tenant settings.
- **Dependencies:** Phases 16–23
- **Effort:** L

#### Task 25.2: Organizer/event risk limits, review, complaints, and stop-sale
- **Type:** create/modify
- **Layer:** Domain/Application/API/Cerbos/Blazor
- **Files:** risk policy/review/case commands, paid publication/checkout gates, Admin/Studio surfaces/HAL/audit.
- **Description:** Versioned first-event/high-value/velocity ceilings, local verification/evidence, complaint intake, incident suspension, and stop-sale without recipient redirection. Stripe KYC/Radar evidence complements but never replaces local event trust.
- **Acceptance Criteria:** fail-closed thresholds; reviewer separation; suspension stops new sales while refunds/webhooks continue; bounded reason codes and audit.
- **Dependencies:** Phases 16–19
- **Effort:** XL

#### Task 25.3: Reconciliation, observability, recovery drills, and provider fixture matrix
- **Type:** modify/create
- **Layer:** Application/Infrastructure/API/Ops
- **Files:** payment/refund/account/payout/admission health, bounded metrics/alerts, runbooks, `Stripe.net` transport/signature/conformance fixtures, SDK/API upgrade checklist.
- **Description:** Cover success, SCA, duplicate/delayed/out-of-order webhook, signature/API-version/mode mismatch, ambiguous timeout, rate limit, SDK retry, insufficient-balance refund, partial refund, cancellation restart, dispute, account restriction, recipient change, unsupported currency, and protected-profile windows. Exercise adapter headers/bodies/error mapping plus workers through deterministic custom-`HttpClient` and `EventUtility.GenerateSignatureHeader` fixtures; live accounts and Stripe CLI sandbox drills remain external launch gates.
- **Acceptance Criteria:** all in-scope Stripe capabilities have provider-neutral contract evidence; no PII/high-cardinality IDs; provider request IDs are retained only in bounded support/audit context; orphan/contradictory state alerts; dead-letter/reconcile actions audited and HAL-gated; exact package/API/webhook versions documented; test/live evidence distinguished.
- **Dependencies:** Phases 18–24 as enabled
- **Effort:** XL

#### Task 25.4: Canonical contract/docs synchronization and workstream completion audit
- **Type:** modify
- **Layer:** Docs/Contracts
- **Files:** `.agents/contract/intents.yaml`, `docs/{DOMAIN,API,API_CHANGELOG,AUTHORIZATION,SECURITY-MODEL,WEBHOOKS,OUTBOX_PATTERN,BLAZOR,CONFIGURATION,SECRETS,SELF_HOSTING,CONTACT_SHARING}.md`, OpenAPI/NSwag/DBML, this plan/context/tasks.
- **Description:** Regenerate contracts, map every D21–D30 invariant and phase acceptance to shipped code/tests/docs, record external launch gaps, and retire `deferred-design-records.md` entries only when implemented. Preserve clean-room source register and dependency evidence.
- **Acceptance Criteria:** no stale “future payment/admission” claims; Event/external-business-system boundary and deferred report remain explicit; no optional Phase 24 completion claim without approvals; task/plan/context counts match; generated artifacts deterministic.
- **Dependencies:** 25.1–25.3
- **Effort:** L

---

## 7. Testing Strategy

One fastest relevant non-browser project per phase, exactly one `dotnet test` command at phase end (assignments and repeat-justifications inline in each phase above): P0 Architecture, P1 Domain, P2 API, P3 Architecture, P4 Persistence, P5 Persistence, P6 Application, P7 Domain, P8 Application, P9 API, P10 Infrastructure, P11 Infrastructure, P12 Infrastructure, P13 API, P14 Blazor.Client, P15 Architecture, P16 Infrastructure, P17 Persistence, P18 Infrastructure, P19 Application, P20 Domain, P21 API, P22 Application, P23 Persistence, P24 Infrastructure (conditional), P25 API.

Contract-mandated projects not selected above are recorded as contract requirements and folded into owning task acceptance; a phase may substitute its one selected project only when the dominant risk changes and the reason is recorded before execution. No E2E, Playwright, browser automation, Chrome DevTools MCP, visual QA, live-app smoke, Aspire/Docker startup, or manual runtime verification is planned in implementation phases. Stripe adapters use deterministic contract fixtures through `Stripe.net`'s supported custom-`HttpClient` seam and signed webhook helper; no extra mocking package or `stripe-mock` dependency is planned. Application tests replace only the small provider capability ports. Live Stripe accounts/CLI, country/currency corridors, legal opinions, Islamic-finance review, and production incident staffing are external launch evidence, not automated phase gates and never inferred from fixture success.

## 8. Documentation, Configuration, And Operations Impact

- **Docs:** Existing ADR-016..018 remain authoritative; ADR-022..024 land in P15. `docs/DOMAIN.md`, `API.md`, `API_CHANGELOG.md`, `AUTHORIZATION.md`, `SECURITY-MODEL.md`, `WEBHOOKS.md`, `OUTBOX_PATTERN.md`, `BLAZOR.md`, `CONFIGURATION.md`, `SECRETS.md`, `SELF_HOSTING.md`, `ADMIN_GUIDE.md`, `CONTACT_SHARING.md`, `OPERATIONS.md`, `TROUBLESHOOTING.md`, DBML, OpenAPI, and NSwag are updated only by phases that materially change them. The I-VSD consultation and `dev/report/event-platform-boundary-and-external-business-integrations.md` are referenced, not edited during implementation unless their decisions change.
- **Configuration:** exact stable `Stripe.net` + pinned API/webhook endpoint version; instance Stripe Connect keys/webhook secrets and test/live mode; explicit SDK timeout/network-retry settings; paid-event policy ceilings and tenant narrowing; organizer kinds; currency set/default; refund/risk limits; QR/token key versions; optional protected-profile approvals. Safe defaults are payments off until configured, organization-only when enabled upstream, EUR-only only when the operator chooses it, bounded SDK retries with no duplicate resilience layer, strict webhook API-version matching, and `ProtectedDelayedPayout` unavailable. Qonto/accounting/invoice configuration does not belong to this workstream.
- **Operations:** account/payment/refund/dispute/payout reconciliation, admission issuance, waitlist expiry, and check-in health join the existing fenced/outbox worker model. Kill switches stop new sales or scoped scanner actions while preserving webhook/refund/reconciliation and historical reads. Runbooks own dead letters, ambiguous provider acceptance, secret rotation, device compromise, cancellation batches, disputes, and operator communications.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- **Trust boundaries:** provider callbacks verified before intake persistence (D7); external completion never confirms (§10); capability tokens hashed, single-purpose, expiring, never identity proof (D8/§12); embeds from approved domains only, server-generated, CSP-allowlisted; no open redirects; SSRF-guarded link checking (no private/metadata networks).
- **Authorization:** Cerbos policy-per-resource with provenance/organizer attributes; four authorities never implied by each other; contributor matrix (§23) enforced server-side and tested; HAL links are the only client affordance/navigation authority; fail-closed on ambiguous organizer authority (NFR-02). The Studio context endpoint revalidates the optional actor hint against the principal, returns `PrivateNoStore`, and exposes relations rather than roles or tenant-wide event data.
- **Privacy:** PII split entities (order/participant PII, sensitive answer ciphertext); field-level classification/purpose/retention/visibility/exportability governance (§18); consent immutable evidence with typed subjects; third-party processing disclosure before external-form launch; completion-only mode stores zero answers; logs/metrics/traces/ProblemDetails free of answers/emails/tokens/payloads (NFR-09) — asserted by tests in P8/P9.
- **Abuse:** `public_transactional` rate policy, idempotency, antiforgery, quotas per verified contact, best-effort-only honesty for anonymous limits (§19.5), link-reporting + moderation path, file quarantine default.
- **Monetization:** platform fee policy and platform-contribution enablement are instance-admin-only `Admin`-class surfaces — tenant admins, organizers, and curators fail closed (D18); API and persistence accept already-normalized integer minor units and define no decimal-major or FX conversion; buyer-chosen prices are validated server-side against pinned catalog bounds (D17); contribution money is segregated from organizer earnings in every DTO, export, and future payment split; display/public identifiers never authorize (Hi.Events §7.8 counter-example); monetization content is DB-stored, so no hardcoded solicitation text ships in the product.
- **Payments:** organizer actor and connected account are pinned before paid publication; direct charges use that account; platform fee/contribution is separately disclosed; every remote mutation has durable idempotency and reconciliation; signed provider evidence is monotonic; refunds/disputes stay independent; payment UI never claims success from navigation or request acceptance.
- **Admission:** QR/scanner/transfer/recovery secrets are CSPRNG capabilities stored as keyed digests and redacted everywhere; online validation checks tenant/event/target/entitlement/status; transfer/reissue rotates; check-in/undo appends audit; scanner capabilities are narrow/revocable; no display ID, email, or client-side role/status becomes authority.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Rationale |
|---|---|---|
| Multi-tenancy | **Applicable** | Every tenant-owned entity uses central filters; instance policy is a ceiling and tenant policy only narrows; provider/capability lookup never discloses another tenant or operator account |
| Federation (AT Proto) | **Deferred** | Orders, payment/refund facts, admission credentials, and transfer capabilities remain local authority; `EventRegistration.AtprotoRecordId` does not federate them |
| Localization | **Applicable** | Exact ISO currency code/name, merchant/refund/payout disclosures, ticket/recovery/check-in status, add-on/fulfillment labels, and event email content are localized; RTL remains logical |
| Accessibility | **Applicable** | Checkout totals, processing/refund status, QR alternative text/manual code, camera permission fallback, HID/manual scanner, live announcements, focus, and non-color results are task acceptance |
| Product | **Applicable** | Official/unofficial operator, merchant, currency, fees/contribution, refund floor, and non-escrow status are truthful; unavailable profiles/actions are absent, not aspirational placeholders |

## 11. Observability And Operations

Bounded metrics only (`provider`, `operation`, `outcome`, `profile`, `currency`, `failure_category`, `order_status`, `payment_status`, `refund_status`, `admission_outcome`) are added per phase — never tenant/event/order/payment/ticket/credential/provider-object IDs, email, answer, code, or amount as a label. Health surfaces report aggregate account readiness, reconciliation age/depth, unknown/orphan counts, refund backlog, dispute cases, admission issuance lag, scanner failure categories, and waitlist expiry lag. Workers use leases/fencing and structured reason codes; raw provider errors are redacted and retained only in governed bounded evidence when necessary.

## 12. Migration And Compatibility Plan

Clean-baseline strategy per D13 remains: **no compatibility aliases, data backfills for unshipped shapes, shims, or dual writes**. Phases 16–24 add entities/configurations first, then regenerate unapplied development migrations through EF for PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL; migration/designer/snapshot files are never hand-edited. Breaking names/contracts may be corrected directly before release. Model parity does not prove database rollout. Once real payment, refund, ticket, check-in, external-document-reference, or settlement history exists in a deployed environment, that history is immutable evidence and future development changes use additive corrective migrations, not destructive rewriting.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---|---|---|---|---|
| Registration accidentally rewrites the privacy-erasure-owned baseline | Low | High — corrupts migration history | Additive generated migrations only; no baseline regeneration or hand-edited snapshots | Any registration change to an existing init migration or snapshot | Every persistence phase |
| Committed migration artifacts are mistaken for database rollout | Medium | High | Treat `ff30795a2`, `20260729183118_RemoveLegacyEventPricing`, and the no-pending-model result as artifact/model evidence only; require explicit database-application and runtime-rollout evidence before deployment claims | Ledger or release note claims schema is applied without deployment evidence | Task 2.2 / release owner |
| Phase 5 aggregate replacement ripples wider than mapped (AT Proto, emails, notifications) | High | High | In-phase dependents sweep (5.9); delete-last sequencing; `rg` gates in acceptance | Build breaks on delete commits | 5.6/5.9 |
| Capacity race bugs under multi-replica load | Medium | High — oversell | Explicit locking in one transaction; persistence race tests; hold sweeper fencing | Race test flakes; pool counter drift | 5.3 |
| Scope explosion across 26 phases | High | High | Strict ADR/dependency order; default launch stops after P23/P25; P24 conditional; specialist marketing/accounting/tax/invoicing stay external; no second payment provider/offline credential/resale/affiliates | tasks.md drift or new unapproved aggregate | all |
| Provider API drift vs consultation citations (dated 2026-07) | Medium | Medium | Conformance re-verification tasks (10.1/11.1/12.1); capability tuples fail closed | Conformance evidence deltas | 10.1, 11.1, 12.1 |
| Schema-hash instability breaking published-version identity | Low | High | Golden-hash tests, culture-invariant serializer frozen at first publish | Hash test failure | 7.4 |
| Guest abuse of PublicTransactional surface | Medium | Medium | Dedicated rate policy, idempotency, antiforgery, quotas, honest limits | Rate-limit metrics, reconciliation queue spikes | 3.1–3.3, 5.4 |
| PII leakage via logs/exports/metrics | Low | Critical | NFR-09 assertions in tests; bounded label review; export gating + audit | Log-scan test failures | 8.3, 13.3 |
| OpenAPI/NSwag churn destabilizing client | Medium | Medium | Regeneration as discrete per-phase final step; naming tests; changelog discipline | `ApiClientNamingTests` failures | every API phase |
| Answer CHECK constraints vs EF model friction | Medium | Low | Raw-SQL check constraints in configurations; persistence tests | Migration generation errors | 8.2 |
| Consent/subject refactor breaking existing contact-share flows | Medium | Medium | P13 owns the full vertical incl. docs; parity tests | Contact-share API test failures | 13.1 |
| Money-math defects in pricing modes / fee policy / contribution (rounding, currency, float drift) | Medium | High | Decimal-only value objects, explicit per-currency rounding rules, exhaustive decimal tests; Hi.Events float-money defect is the named counter-example | Failing decimal tests; totals mismatch between line/order/earnings figures | 4.1, 5.2, 5.10 |
| Tenant-level actors gaining monetization control (abuse of contribution/fees) | Low | Medium | Instance-admin-only Admin endpoints; fail-closed authorization tests; no tenant-visible management surface | Authorization parity failures; unexpected 200s in monetization tests | 4.5 |
| Hi.Events commercial breadth pulling deferred features into active phases | Medium | Medium | D19 scope discipline; deferred inventory lives only in Task 14.8 records | Discovered tasks referencing promo/refund/check-in outside P14 | 14.8 |
| AGPLv3 code contamination from Hi.Events breaking CLA dual-licensing | Low | Critical | §4.13 no-code-copy rule; clean-room implementation from report + plan only; agents never open the Hi.Events repo while coding; PR review watches for transcribed code | Code resembling Hi.Events sources; references to its repo paths in diffs | All phases; rule recorded in ADR-018 (0.2) |
| Studio actor-context over-disclosure or stale navigation authority | Low | High | Validate actor hints server-side; `PrivateNoStore`; relation-only DTO; event pages re-authorize operations and use event/resource HAL links | Unauthorized actor hint succeeds; role/event inventory appears in context DTO; sidebar action survives missing relation | 5.7, 5.8, 6.4, 14.7 |
| Wrong merchant/account receives charge or refund | Low | Critical | Actor-bound connection, immutable catalog/attempt snapshot, connected-account request context, no admin fallback | provider account differs from snapshot; recipient mutation after sale | 16.2, 18.1–18.4, 19.4 |
| Duplicate charge/refund after timeout or webhook replay | Medium | Critical | Durable idempotency before handoff, inbox dedupe, monotonic rules, reconciliation before retry | multiple provider objects for one local attempt; contradictory terminal state | 18.2–18.4, 19.2–19.4 |
| Provider/KYC approval is mistaken for genuine-event trust | Medium | High | Local organizer/event verification, risk ceilings, complaints, stop-sale, cancellation/refunds | fraud complaints, velocity threshold, evidence review failure | 16.1, 26.2 |
| Promotion/hold races oversubscribe usage or change totals | Medium | High | One transaction/lock order; live reservations count; immutable order composition | confirmed uses exceed limit; provider/local total mismatch | 17.2–17.3 |
| QR/display/recovery identifier becomes a reusable bearer leak | Medium | Critical | CSPRNG opaque credential, keyed hash, no PII/log/storage, scope/expiry/rotation, generic recovery | credential in telemetry/referrer; old QR validates after rotation | 20.1–20.5, 22.2–22.3 |
| Check-in or transfer race admits/reassigns twice | Medium | High | Atomic active-state constraints, append-only facts, one active offer, compensating undo | two successful concurrent scans/transfers | 21.2, 22.2–22.3 |
| Refund shown complete while Stripe balance leaves it pending | Medium | High | Independent attempt state, provider event/reconciliation truth, exact UI language | local succeeded without provider success evidence | 19.2–19.5 |
| Specialist integration scope leaks accounting/tax/invoicing into Event | Medium | High | D29 product boundary; removed design preserved in the external-integration report; future Qonto work is separate and optional | Event-owned ledger, tax engine, invoice numbering/document aggregate, or campaign UI appears in this workstream | 15.3, 23.4–23.5, 25.4 |
| Delayed payout creates custody/liability or false escrow promise | Medium | Critical | Optional P24 hard gate; explicit milestone/limits/non-escrow disclosure; Stripe/legal/scholarly approval | profile enabled without all approvals; sales exceed hold limit | 24.1–24.4 |
| Malicious fork implies ISLAMU protection or uses shared credentials | Medium | Critical | Per-instance Connect platform, official-origin disclosure, no shared secrets/normal admin official marker | duplicate official branding/origin or ISLAMU account ID on fork | 25.1 |

## 14. Success Metrics And Definition Of Done

Functional success retains every original registration/form/ticket/pricing/consent scenario and adds: verified organization connects its own Stripe account; user/group organizer is allowed only by explicit narrowing policy; Belgium EUR lock and global organizer-confirmed EUR/USD/MAD/SAR/AED; local promo reservation and immutable discount; successful/SCA/delayed/duplicate/unknown payment; cancellation and pending/succeeded/partial refund; dispute projection; free and paid `AdmissionTicket` issuance; revoked QR rejection; event/session check-in, duplicate and undo; transfer acceptance rotates old QR; generic resend/recovery; account/guest self-service; waitlist offer expiry; event-add-on fulfillment separate from admission; explicit absence of Event-owned marketing/accounting/tax/invoice features; and official/self-hosted operator disclosure. Every buyer sees merchant, operator, currency, totals, fee/contribution, refund policy, and any non-escrow release promise before payment.

Per phase, the automated gate is exactly one Release build plus at most one selected non-browser project. The expanded default workstream is Done when P15–P23 and P25 are complete in addition to the preserved P0–P14 state, the intent/ADRs and I-VSD invariants hold, generated artifacts/docs match behavior, and external launch gaps are explicit. P24 is complete only if its approvals exist and it ships; otherwise it remains intentionally unavailable and does not block an `OrganizerDirect` production decision. Qonto/Listmonk specialist work remains a separate optional workstream. “Implemented” never means legally, regulatorily, contractually, or Sharia certified.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. At first implementation start, read plan, context, and tasks once; on cold resume, read context + tasks first, then only the plan sections for the current phase or changed decision.
2. During an uninterrupted session, do not reread unchanged plan/context/tasks after every task; keep the current task in working context and reopen only the exact section needed.
3. Start from the highest-priority unchecked task unless the user overrides.
4. `tasks.md` is the hot execution ledger: check a substantial task immediately after its implementation acceptance criteria are met; reconcile smaller completed tasks together no later than phase end.
5. Implementation-task and phase-verification checkboxes stay separate; a phase is complete only after its build and selected test checkboxes pass.
6. Update the status summary, completed count, current priority, next recommended slice, discovered tasks, deferred work, and `Last Updated` whenever task state changes.
7. Update context after a completed phase, meaningful decision, blocker, failed validation, material discovery, or before pause/compaction/transfer; do not rewrite for trivia.
8. Update the plan only when scope, architecture, phase order, acceptance criteria, risks, or validation strategy changes.
9. Record failed validation with known cause and next recovery action in tasks/context without marking the phase complete.
10. Before pausing, compaction, transfer, or PR creation: reconcile affected tasks, add a dated handoff, and identify unrelated dirty files the next contributor must avoid.
11. Run phase verification only after all phase tasks, with one Release build and at most one selected project test; do not repeat green commands or start the application/browser.
12. Never report completion when repository reality and the task ledger disagree.

Every implementation summary must teach: what changed and why; patterns/libraries/protocols used (CQRS/MediatR, UnitOfWork, outbox, HAL affordance gating, capability segregation, idempotency/fencing, tenant isolation); important files/classes/handlers with responsibilities; data/control flow; repo conventions honored; verification performed; remaining work; dev-doc update status.

## 16. Progress Reporting Contract

After each implementation slice:

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: yes/no with reason
```

For completed work, `Docs updated` must confirm `tasks.md` reconciliation; report context and plan separately as updated or unchanged-because-no-trigger.

## 17. Potential Risks & Unknowns

The highest-risk new slice is **Phase 18–19**: one wrong connected-account context, idempotency boundary, amount composition, or webhook transition can misdirect money, double charge/refund, or confirm an unpaid order. Keep provider identities and local snapshots immutable, persist intent before I/O, and treat reconciliation as required behavior rather than recovery polish. The second risk is **Phase 20–22**: QR, scanner, recovery, and transfer are bearer-capability surfaces; a low-entropy/display identifier, leaked token, or non-atomic rotation makes copied tickets valid. Online opaque validation is intentionally chosen before offline signatures. The third risk is **Phase 24**: payout control is not primarily a coding problem. Missing Stripe contract, consumer/payment-services, operations, or Islamic-finance evidence must remove the profile from HAL/configuration; it must not become a best-effort default. A parallel product risk is integration scope creep: Qonto/Listmonk may receive bounded event facts, but accounting, tax, invoicing, and marketing must stay outside Event as recorded in the deferred report. Phase 15.4 used Context7/official documentation and an isolated metadata probe; Tavily MCP was unavailable and no Tavily research is claimed.
