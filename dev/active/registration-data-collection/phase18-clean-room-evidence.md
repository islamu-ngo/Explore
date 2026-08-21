<!-- ABOUTME: Records source-free official-documentation evidence for Phase 18 paid registration payments. -->
<!-- ABOUTME: Attests independent AFC/SSO design and unchanged Stripe.net dependency licensing. -->

# Phase 18 Clean-Room Evidence

Access date for every source below: **2026-08-21**.

## Official Source Register

| Official source title | URL | Neutral fact retained |
|---|---|---|
| Stripe Connect direct charges | https://docs.stripe.com/connect/direct-charges | Direct-charge payment objects belong to the connected account and requests use connected-account context. |
| The Checkout Sessions API | https://docs.stripe.com/api/checkout/sessions | Hosted Checkout creates a provider session with explicit return navigation and a provider-bounded expiry whose minimum is 30 minutes after creation. |
| Idempotent requests | https://docs.stripe.com/api/idempotent_requests | Mutating requests accept stable idempotency keys; keys must not contain sensitive data. |
| Receive Stripe events in your webhook endpoint | https://docs.stripe.com/webhooks | Webhook delivery is asynchronous and may be duplicated, delayed, or reordered. |
| Resolve webhook signature verification errors | https://docs.stripe.com/webhooks/signature | Signature verification requires the unmodified body, signature header, and endpoint secret. |
| Stripe SDK versioning | https://docs.stripe.com/sdks/versioning?lang=dotnet | SDK releases pin an API version; API and webhook-version upgrades require deliberate review. |
| Handle errors | https://docs.stripe.com/api/errors/handling?lang=dotnet | Stripe SDK failures expose bounded categories and request identifiers suitable for provider-neutral mapping. |
| Stripe.net 52.3.0 package metadata | https://www.nuget.org/packages/Stripe.net/52.3.0 | The already-pinned package is Apache-2.0 and supports this repository's .NET target through its package target frameworks. |

Context7 supplied sanitized facts from the official Stripe documentation sets. AnySearch MCP was unavailable, so no AnySearch evidence is claimed. No external implementation source, snippet, AST, SQL, migration, test, comment, schema, asset, or copied documentation prose entered the implementation context or this repository.

## Source-Free Handoff And Independent Design

The implementation handoff contained only observable interface facts: connected-account request context, stable idempotency, exact-body signature verification, asynchronous/duplicate delivery, hosted return navigation, bounded errors, and request IDs. ISLAMU naming, aggregates, status transitions, effect ordering, persistence, CQRS/API/HAL boundaries, BFF ticket design, Redis atomics, UI composition, tests, and operator documentation were independently derived from the repository's ADR-022, Clean Architecture, outbox, tenant-isolation, integer-minor money, and HAL-authority conventions.

The AFC/SSO review passed:

- **Abstraction:** compared only payment goals, inputs, outputs, failures, and security constraints.
- **Filtration:** removed elements dictated by HTTP, Stripe's public interface, cryptographic verification, idempotency, and asynchronous payment processing.
- **Comparison:** found no retained third-party discretionary structure, sequence, organization, naming, UI hierarchy, tests, or prose; the implementation remains repository-native.
- **Review role:** clean-room AFC/SSO reviewer.
- **Reviewer:** Codex.
- **Review date:** 2026-08-21.
- **Decision:** PASS.

## Implementation Separation Attestation

Phase 18 implementation was performed from the sanitized functional handoff and repository-native ADRs, rules, and existing code only. No third-party source or source-derived structure, sequence, organization, naming, tests, comments, prose, migrations, schemas, or assets were supplied to the implementation context. Context7 facts remained limited to the neutral official-interface facts in the source register; AnySearch was unavailable and no AnySearch result is claimed.

## Dependency Decision

`Stripe.net` **52.3.0** was already present before Phase 18 and remains unchanged. Its recorded Apache-2.0 dependency decision and Infrastructure-only boundary remain valid; Phase 18 adds no dependency and changes no outbound-license path. This record is engineering provenance evidence, not legal certification.

## Verification And Traceability

- Signed webhook and HTTP security coverage: `tests/Event.API.IntegrationTests/Features/StripePaymentWebhookOrderingTests.cs` and `tests/Event.API.IntegrationTests/Features/RegistrationPaymentHttpSecurityTests.cs`.
- Backend contract and generated-client coverage: `tests/Event.API.IntegrationTests/Features/RegistrationPaymentContractTests.cs` and `tests/Explore.Blazor.Client.Tests/Services/RegistrationPaymentGeneratedClientTests.cs`.
- Infrastructure Checkout coverage: `tests/Explore.Infrastructure.Tests/Payments/Stripe/StripeCheckoutAdapterTests.cs`.
- Phase-end commands: `dotnet build --configuration Release --verbosity quiet` and `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` completed successfully.
- Public release fragment: [`docs/releases/changes/CHG-2026-0004.yaml`](../../../docs/releases/changes/CHG-2026-0004.yaml).
- Durable finding: [`dev/_journal/journal.md`](../../_journal/journal.md#2026-08-21-europebrussels--provider-minimum-checkout-window-must-move-the-local-cutoff).
- PR status: pending; no pull request has been created.
- Commit provenance: the Phase 18 feature commit carries `Change-Id: CHG-2026-0004`.

The canonical Phase 18 Release-build and full `Explore.Infrastructure.Tests` phase-end verification boxes are recorded in [`registration-data-collection-tasks.md`](registration-data-collection-tasks.md#phase-18-verification--run-once-after-all-phase-tasks).
