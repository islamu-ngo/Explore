<!-- ABOUTME: Records source-free Phase 21 framework and security research for admission check-in. -->
<!-- ABOUTME: Preserves provenance, clean-room constraints, and independent ISLAMU design decisions. -->

# Phase 21 Admission Check-In Clean-Room Evidence

Access date: 2026-08-26

## Source Register

| Source | URL | Functional facts retained |
|---|---|---|
| Microsoft Learn, “Rate limiting middleware in ASP.NET Core” | https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0 | ASP.NET Core supports named endpoint policies, partitioned limiters, bounded or disabled request queues, rejection metadata, and built-in metrics. Limits must be validated under representative load. |
| Microsoft Learn, “Policy-based authorization in ASP.NET Core” | https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0 | Policies combine requirements fail-closed; endpoint and resource authorization must not depend on handler ordering. |
| OWASP Cheat Sheet Series, “Forgot Password Cheat Sheet” | https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html | Opaque capabilities should be CSPRNG-generated, sufficiently long, stored securely, purpose-bound, expiring, revocable or single-use where applicable, rate-limited, and excluded from enumerating responses. |
| MDN, “Barcode Detection API” | https://developer.mozilla.org/en-US/docs/Web/API/Barcode_Detection_API | Native barcode detection is capability-detected and format-specific; browser availability cannot be treated as the admission correctness boundary. |

The requested web-search provider was invoked, but its configured DuckDuckGo backend returned no
results. The official URLs above were fetched directly. Context7 MCP is not registered in this
session, so no Context7 result is claimed.

## Sanitized Functional Handoff

- A scanner request is always authorized and validated by the server. Camera, HID, and manual
  entry are interchangeable input adapters to the same command.
- Scanner capability material is opaque, high entropy, tenant/event/target/action/expiry scoped,
  stored only as a keyed digest, disclosed only by the issuance response, and absent from later
  reads, logs, traces, metrics, audit exports, browser storage, and support artifacts.
- Admission mutations use a named, partitioned, fail-closed rate policy with no unbounded queue.
  Saturation returns a bounded retryable HTTP outcome; it never enables local validation.
- Authorization combines authenticated staff authority or a valid narrow scanner capability with
  persisted tenant, event, target, ticket, credential, and lifecycle facts. Missing or conflicting
  authority denies the operation generically.
- Native camera detection is optional. Unsupported, denied, disconnected, or ambiguous detection
  leaves HID and labeled manual entry available without changing server-side semantics.

## Independent Repository-Native Design

ISLAMU owns a dedicated append-only admission fact model rather than adapting an external ticketing
workflow. Domain classes represent admission target, policy, check-in/undo fact, and scanner
capability lifecycles. Application CQRS coordinates entity-returning repositories. Persistence uses
tenant-qualified EF Core mappings and the existing provider-neutral row-fence protocol. API
authorization remains Cerbos/local-provider plus HAL affordance authority. Blazor uses the existing
BFF, generated NSwag client, native scanner abstraction, scoped in-memory state, and accessibility
announcer.

No third-party source, snippets, ASTs, SQL, migrations, tests, comments, assets, naming,
decomposition, or workflow organization is retained in this handoff. No new runtime or test
dependency is approved or required.

## AFC / SSO Decision

- **AFC filtering:** retained only security, protocol, framework capability, and failure-mode facts.
- **Independent choices:** append-only facts plus an active-state fence; transaction-per-item batch
  processing; repository-native HAL/Cerbos authority; no BFF token store; no offline queue.
- **SSO outcome:** implementation structure, names, sequencing, persistence shape, API resources,
  UI composition, tests, and operations are derived from ADR-023 and existing ISLAMU architecture.
- **Dependency decision:** use existing .NET, EF Core, ASP.NET Core, MudBlazor, NSwag, and repository
  scanner abstractions only; no package or asset change.
