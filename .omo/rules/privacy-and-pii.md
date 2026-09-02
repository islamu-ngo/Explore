---
name: privacy-and-pii
description: Apply when editing user data, registration forms, contact sharing, consent, PII, or privacy erasure workflows.
paths:
  - "src/**/*Privacy*.cs"
  - "src/**/*User*.cs"
  - "src/**/*ContactShare*.cs"
  - "src/**/*Consent*.cs"
  - "src/**/*Erasure*.cs"
related_skills: [clean-architecture-rules, dotnet-efcore-guidelines, error-tracking, auth-patterns]
related_docs: [docs/internal/PRIVACY_ERASURE.md, docs/internal/AI_CONTEXT_SECURITY.md, docs/internal/SECURITY-MODEL.md, docs/internal/SECRETS.md]
minimum_tests: [Event.Persistence.IntegrationTests, Event.Architecture.Tests, Event.Application.UnitTests]
related_intents: [platform-privacy-erasure, update-ai-context-disclosure]
---

<!-- ABOUTME: Path-scoped rules for Tier 2 Privacy & PII Compliance and GDPR Erasure Authority. -->
<!-- ABOUTME: Twin copy at .agents/rules/privacy-and-pii.md. When modifying this file, update both paths. -->

# Privacy and PII Rules (Tier 2 — Privacy)

## Applies To
- `src/**/*Privacy*.cs`, `src/**/*User*.cs`, `src/**/*ContactShare*.cs`, `src/**/*Consent*.cs`, `src/**/*Erasure*.cs`

## Critical Rules & Invariants

| # | Rule | Correct | Wrong |
|---|---|---|---|
| 1 | **Framework Telemetry Redaction** | Annotate sensitive properties with `[PiiData]` or `[SensitiveData]` to trigger `Microsoft.Extensions.Compliance.Redaction`. | Passing raw email, phone, name, or tokens directly into `ILogger` without redaction masks. |
| 2 | **Authority-First Erasure** | Append and commit the immutable authority fact *before* starting local PII hard-deletes or remote provider outbox work. | Deleting database rows first and writing audit records after. |
| 3 | **Anti-Resurrection Fencing** | Check user fence status before rematerializing cache, recreating user profiles, or dispatching background workers. | Allowing raced background tasks or incoming messages to recreate PII for an erased subject. |
| 4 | **Atomic Local Purge** | Execute local hard-deletes, anonymization, application mirror updates, and receipt hash storage within a single serializable transaction. | Partial deletion across uncoordinated database operations. |
| 5 | **Opaque Status Tokens** | Return `202 Accepted` with a short-lived `ErasureReceipt` authentication token; status checks require this receipt token. | Exposing public user deletion status without receipt verification. |
| 6 | **Zero PII in Observability** | Export counts, stage codes, and hashed identifiers in metrics, ProblemDetails, and health checks. | Emitting subject IDs, plaintext emails, or provider payload bodies in error logs or Prometheus metrics. |

## Must Read
- [docs/internal/PRIVACY_ERASURE.md](../../docs/internal/PRIVACY_ERASURE.md)
- [docs/internal/AI_CONTEXT_SECURITY.md](../../docs/internal/AI_CONTEXT_SECURITY.md)
- [docs/internal/SECURITY-MODEL.md](../../docs/internal/SECURITY-MODEL.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests`, `Event.Application.UnitTests`
- Log Scans: AST and integration assertions proving zero unmasked PII entries in test log sinks.

## Related
- Intents: `platform-privacy-erasure`, `update-ai-context-disclosure`
- Agents: `security-privacy-agent.md`, `quality-verifier-agent.md`
- Skills: `error-tracking`, `clean-architecture-rules`, `dotnet-efcore-guidelines`
