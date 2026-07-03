<!-- ABOUTME: Re-baselined implementation plan for backend/API health and contract hardening. -->
<!-- ABOUTME: Converts the broad refactor backlog into source-grounded, reviewable Clean Architecture slices. -->

# Backend API Health Refactor - Implementation Plan

Last Updated: 2026-07-03 Europe/Brussels

## 0. Planning Metadata

- **Request:** Update `dev/active/backend-api-health-refactor` so the implementation plan is relevant, correct, and aligned with Senior CTO feedback, repo conventions, Context7 documentation, Tavily research, Clean Architecture, and enterprise self-hostable expectations.
- **Task directory:** `dev/active/backend-api-health-refactor/`
- **Planning status:** Implementation in progress; first bounded health slice completed on 2026-07-03.
- **Primary outcome:** A future implementation agent can continue from this workstream without repeating broad discovery or following stale/bad tasks.
- **Matched intent:** No exact single intent exists for this broad workstream in `.claude/contract/intents.yaml`. Use this fallback contract and then classify each implementation slice under the narrower existing intents: `openapi-contract-change`, `add-get-endpoint`, `add-write-endpoint`, `add-hal-link`, `add-cqrs-handler`, `update-repository-query`, `add-ef-migration`, `blazor-component-affordance`, `bff-auth-bug`, and `cerbos-policy-change` where applicable.
- **Relevant skills loaded:** `senior-cto-feedback`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `error-tracking`, `source-command-check`.
- **Relevant rules loaded:** `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/application-layer.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/tests.md`.
- **Primary layers touched by future implementation:** API, Application, Persistence, Infrastructure, Domain, Blazor client affordance tests, docs, and DevOps evidence.
- **Estimated complexity:** XL. The work crosses security, tenant isolation, HAL authorization, OpenAPI/client generation, CQRS handler contracts, EF query behavior, background side effects, and self-hosting operations.

## 1. Executive Summary

This workstream is a backend/API platform-health refactor, not a feature build and not a generic cleanup pass. It should close known security, contract, authorization, tenant-isolation, operational-health, CQRS, and persistence debts in small, reviewable slices.

The previous plan was directionally useful but too broad and partially stale. It mixed completed implementation history, future work, mega-refactors, behavior tests mislabeled as architecture tests, and speculative tasks that were not tied to source evidence. This re-baseline keeps the valid direction but changes the execution model:

- preserve the current `/health`, `/alive`, and `/metrics` operational contract instead of inventing new endpoints;
- harden security and API/HAL contracts before controller or Application refactors;
- split behavior tests from architecture guardrails;
- avoid broad repository/domain rewrites unless a slice proves a concrete risk;
- treat generated OpenAPI/client artifacts as regenerated evidence, never hand-edited source;
- keep self-hosting, tenant isolation, auditability, idempotency, and observability as first-class acceptance criteria.

Out of scope for this plan: Blazor visual redesign, broad design-system changes, non-backend feature expansion, and compatibility shims for obsolete pre-v1 API shapes.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Runtime health endpoints already exist and use project-specific paths. | CodeGraph verified `Explore.ServiceDefaults/Extensions.cs::MapDefaultEndpoints`: `/health` filters `ready`, `/alive` filters `live`, `/metrics` maps Prometheus scraping. | High | Do not plan `/health/ready` or `/health/live` unless intentionally changing the platform contract. |
| Health status code policy is already explicit. | `MapDefaultEndpoints` maps `Healthy` and `Degraded` to 200, `Unhealthy` to 503. | High | Matches `docs/OPERATIONS.md`. |
| Health response no longer serializes raw check exception messages. | `Explore.ServiceDefaults/Extensions.cs` delegates response writing to `HealthCheckResponseWriter`, which redacts exception text and sensitive data before serializing JSON. | High | Implemented 2026-07-03 with focused API integration tests. |
| Microsoft guidance supports separate readiness and liveness probes with tags/predicates. | Context7 `/dotnet/aspnetcore.docs`; Tavily extracted Microsoft Learn ASP.NET Core health-check docs. | High | Repo uses `/health` and `/alive`, which is valid; path names are project policy. |
| Microsoft guidance supports OpenTelemetry/Prometheus `/metrics`. | Tavily extracted Microsoft Learn ASP.NET Core metrics docs; repo maps `/metrics`. | High | Keep metrics low-cardinality per repo observability rules. |
| API and operations docs define `/alive`, `/health`, `/metrics`. | `docs/API.md`, `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`, `docs/MCP_DEBUGGING.md`, `docs/CONFIGURATION.md`. | High | Product-doc `/health/ready` drift was corrected on 2026-07-03. |
| Phase 0 support artifacts exist. | `endpoint-inventory.md`, `endpoint-classification.md`, `backend-contract-risk-register.md`, `authorization-policy-matrix.md`, `tenant-execution-model.md`, `api-error-catalog.md`. | High | They remain useful but some are stale and need regeneration/reconciliation. |
| Current generated artifacts disagree. | 2026-07-03 `jq`/Markdown diff: `schemas/openapi.json` has `359` paths and `500` operations with no missing endpoint classes; `docs/API_CONTRACT_INVENTORY.md` has `355` paths and `496` operations with one missing endpoint class. | High | R-032 owns the inventory source-of-truth correction. Do not use the historical operation table as authoritative. |
| `/admin/migrate` is the only Markdown-only operation. | CodeGraph/source read: `Explore.API/Program.cs` maps `POST /admin/migrate` only in Development/Testing, requires authorization, lacks endpoint classification, and returns raw migration exception text on failure. | High | R-015/R-032 decide deletion versus explicit Admin/HostAdministration posture. |
| Five webhook message/delivery operations are schema-only. | `schemas/openapi.json` includes `GET /api/webhooks/messages`, `GET /api/webhooks/messages/{messageId}`, `GET /api/webhooks/delivery-attempts`, `GET /api/webhooks/delivery-attempts/{attemptId}`, and `POST /api/webhooks/delivery-attempts/{attemptId}/retry`; `docs/API_CONTRACT_INVENTORY.md` does not. | High | Source shows `WebhooksController` is authenticated/classified; R-033 owns row import and risk tests. |
| New report/moderation/webhook families are security-sensitive. | Source/search evidence for `EventReportsController`, `ModerationReportController`, `IncomingWebhooksController`, and `WebhooksController`; generated artifacts expose these operation families. | High | R-033, R-034, and R-035 own least-privilege auth, HAL gating, signature-as-auth, replay/idempotency, and privacy checks. |
| Tenant filters now fail closed. | `backend-api-health-refactor-context.md` records 2026-06-14 verification; `docs/MULTI_TENANCY.md` states missing tenant no longer broadens to all tenant rows. | Medium-High | Future work should prove bypass call sites, not reimplement fail-closed filters from scratch. |
| Route-name bidirectional guardrails were reportedly closed. | Context and risk register record 452/452 constants resolved and guardrail active. | Medium | Future agents must confirm against current source because the worktree is dirty. |
| API ProblemDetails centralization is largely implemented. | Context and risk register record `ApiProblemCodes`, `ApiProblemFactory`, `CommandResponseResultMapper`, and many controller slices. | Medium | Remaining work should be verification and targeted unmigrated paths, not another broad mapper design. |
| Worktree is currently dirty beyond this workstream. | `git status --short` shows many unrelated modified, deleted, and untracked files. | High | Future implementation agents must not revert unrelated changes and must re-baseline before code edits. |

### 2.2 Existing Implementation

Current backend/API posture:

- `Explore.ServiceDefaults` owns shared operational endpoints. `/health` is readiness, `/alive` is liveness, `/metrics` is Prometheus scraping.
- `Explore.ServiceDefaults.HealthChecks.HealthCheckResponseWriter` serializes shared health JSON and redacts raw exception text plus sensitive health-check data at the endpoint boundary.
- `Explore.API` adds API-specific readiness checks such as storage, reconciliation, SMTP, email dispatch, RabbitMQ dispatch, AI provider/retention, MCP adapter, and Cerbos where configured.
- API errors are expected to flow through chained `IExceptionHandler` and RFC 7807 ProblemDetails with `code`, `traceId`, `timestamp`, and optional `correlationId`.
- HATEOAS/HAL is the source of truth for mutation affordances. UI must check `_links`, not local roles/claims, for per-resource actions.
- MediatR `AuthorizationBehavior` is the server-side resource-authorization boundary for `[AuthorizeResource]`, `IAuthorizedRequest`, and `ISecureRequest`.
- EF tenant filters are the production tenant-isolation enforcement layer. Explicit bypasses require reason, bounded predicates, and tests.
- The workstream already contains a useful risk register and inventories, but the main plan/tasks did not distinguish completed work from current next work cleanly.

### 2.3 Existing Tests And Verification Coverage

Known coverage and required verification lanes:

- `Event.Architecture.Tests`: Clean Architecture, endpoint classification, route names, auth metadata, response metadata, agent-context/doc governance.
- `Event.API.IntegrationTests`: HTTP behavior, auth gates, HAL, ProblemDetails, OpenAPI contract, rate limit/idempotency/timeouts where applicable.
- `Event.Application.UnitTests`: CQRS handler behavior, authorization metadata, idempotency/concurrency/audit use cases.
- `Event.Persistence.IntegrationTests`: EF tenant filters, query filters, repositories, migrations, index/model assertions.
- `Explore.Blazor.Client.Tests`: bUnit affordance gating by HAL links.

Known verification caveats from the existing context:

- Some prior API integration runs were blocked by Docker/Testcontainers or host shutdown/OpenFeature lifecycle issues.
- Some full build/Blazor verification was previously blocked by a Razor syntax issue in `AiActionResultCard.razor`.
- A later architecture run reportedly failed for unrelated `.claude` agent-context link/manifest issues, not the latest backend/API slices.
- Because the worktree is now dirty, these caveats must be rechecked before future implementation claims.

### 2.4 Existing Documentation And Contracts

Relevant docs/contracts:

- `docs/API.md`: middleware order, auth, HAL, ProblemDetails, OpenAPI/client generation, `/health`/`/alive`/`/metrics`.
- `docs/OPERATIONS.md`: health/readiness semantics, readiness checks, metrics, runbooks, retention/partitioning posture.
- `docs/SECURITY-MODEL.md` and `docs/AUTHORIZATION.md`: BFF trust boundary, Keycloak/JWT, resource authorization, Cerbos/local provider behavior, fail-closed rules.
- `docs/MULTI_TENANCY.md`: tenant resolution order and fail-closed query filters.
- `docs/TESTING.md`: TUnit projects, per-project commands, integration host profiles, skip governance.
- `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, and `Explore.Blazor.Client/Clients/EventApiClient.g.cs`: generated artifacts; do not hand-edit.
- Active workstream support artifacts listed in section 2.1.

### 2.5 Current Pain Points / Improvement Areas

1. **Plan artifact drift.** The old plan dated 2026-06-13/14 mixes implementation notes, historical progress, and future tasks. This makes it hard for a future agent to know the real next slice.
2. **Inventory drift.** `endpoint-inventory.md` records a historical 399-operation baseline plus manual overrides. On 2026-07-03 the current schema/inventory artifacts differ: `schemas/openapi.json` has 500 operations, while `docs/API_CONTRACT_INVENTORY.md` has 496. Future contract work must fix the inventory source mismatch and regenerate/re-import the row-level table before relying on classifications.
3. **Health endpoint docs drift.** Product docs were corrected on 2026-07-03. Workstream docs still mention `/health/ready` only as a rejected/deferred endpoint alias; `Explore.AppHost` still contains intentional external infrastructure health checks such as MinIO/Cerbos.
4. **Health payload redaction risk.** The shared response writer now redacts raw exceptions and suspicious data. Individual checks must still prefer bounded provider/status/failure-code data so operator output stays useful.
5. **Overbroad architecture-test tasks.** Behavior such as idempotency, concurrency, audit emission, and field-shape privacy should be proven with targeted unit/integration tests first. Architecture tests should guard structural invariants only.
6. **Speculative tenant execution enum.** Current tenant filters are already fail-closed. A new execution-mode abstraction should be introduced only where it simplifies audited bypass/system-scope handling, not as a prerequisite rewrite.
7. **Repository cleanup overreach.** Read-only `AsNoTracking` is correct, but tracked aggregate loads are valid for mutation paths. Do not globally convert repository reads without separating read and mutation contracts.
8. **Controller decomposition risk.** Splitting controllers before confirming route/HAL/OpenAPI stability can break links and generated clients. Splits must be resource/use-case driven and backed by behavior tests.
9. **Persistence/domain cleanup too broad for a final phase.** Removing all Domain `DataAnnotations`/mapping attributes is probably a dedicated workstream unless scoped to one aggregate with migration/model tests.
10. **Unclear first implementation slice.** The old task file had many partially completed items and long implementation-history paragraphs. The next slice must be obvious and small.

### 2.6 Unknowns After Investigation

- Which current dirty worktree changes are intentional user work versus incomplete implementation slices. Future agents must inspect before editing source.
- Whether current `Event.Architecture.Tests`, `Event.API.IntegrationTests`, and build blockers still reproduce on 2026-07-03.
- Whether the inventory generator should exclude Testing/Development-only minimal endpoints such as `/admin/migrate`, include them with explicit Admin/HostAdministration metadata, or switch the Markdown inventory to the same build-time OpenAPI source used by `schemas/openapi.json`.
- Whether the five schema-only webhook message/delivery operations already have enough behavior coverage or need new API/Application tests before being accepted into the governed contract.
- Whether every readiness check's data/exception output is bounded and production-safe.
- Whether each remaining P0/P1 auth/HAL risk still exists after current untracked/modified work.

## 3. Proposed Future State

The target is not a giant "make backend enterprise-grade" PR. The target is a sequence of slices that leave the platform easier to operate, safer to self-host, and easier to maintain:

1. Re-baseline the workstream against the current branch and current generated API contract.
2. Close security/HAL/auth gaps before structural refactors.
3. Lock API contract/error/route/OpenAPI behavior with tests and generated artifacts.
4. Decompose controllers only behind stable routes and HAL policies.
5. Refactor Application/CQRS hotspots with behavior-preserving tests and explicit transaction/idempotency/concurrency boundaries.
6. Improve Persistence/query/reliability hotspots only where evidence shows risk.
7. Update operator docs, health/metrics redaction, changelogs, and validation evidence.

## 4. Non-Negotiable Constraints

- Repositories return entities, never DTOs. Mapping stays in Application handlers/read-model mappers.
- Validators are manually instantiated; do not inject `IValidator<T>`.
- Domain stays pure. No EF Core, ASP.NET Core, MediatR, AutoMapper, or infrastructure dependencies in Domain.
- GET endpoints may be anonymous only when data is intentionally public. Anonymous reads must not expose user IDs, emails, full names, roles, memberships, grants, invitations, revocation metadata, private tenant data, or sensitive storage/admin metadata.
- Writes require `[Authorize]` plus handler/resource authorization when ownership or admin authority matters.
- HAL `_links` are the source of truth for Blazor action affordances.
- Tenant filters fail closed; bypasses require bounded predicates, reason, operation name, logging/audit where sensitive, and tests.
- Controllers remain transport/composition boundaries: MediatR dispatch, HAL assembly, response mapping.
- Query handlers return DTO/list/page/null results, not command envelopes.
- Commands return `BaseCommandResponse<TId>` or established delete/result patterns.
- No external HTTP, SMTP, broker publish, provider deletion, or scheduler side effect inside DB transactions. Use transactional outbox or approved background worker patterns.
- Generated OpenAPI, inventory, and NSwag client files are regenerated through documented commands, not hand-edited.
- Health, logs, metrics, traces, and ProblemDetails must not expose secrets or high-cardinality/user-controlled data.
- No backward-compatibility shims for obsolete pre-v1 behavior unless explicitly approved.

## 5. Architecture And Design Decisions

### Decision 1 - Preserve the current operational endpoint contract

- **Decision:** Keep `/health` for readiness, `/alive` for liveness, and `/metrics` for Prometheus unless a separate migration plan intentionally changes paths.
- **Why:** Current code, API docs, operations docs, and self-hosting docs already use these paths. Microsoft docs require separation of readiness/liveness semantics, not specific path names.
- **Alternatives considered:** Add `/health/ready` and `/health/live` aliases. Rejected for this workstream because it adds duplicated operational contract and docs/client confusion.
- **Consequences:** Fix docs that incorrectly mention `/health/ready`; do not create compatibility aliases by default.

### Decision 2 - Treat health payload safety as an implementation concern

- **Decision:** Audit and, if needed, redact health check `error` and `data` fields rather than assuming all health checks are safe.
- **Why:** The shared writer emits exception messages. Some dependency checks may contain endpoint/path/provider details unless carefully bounded.
- **Consequences:** Add API/ServiceDefaults tests for production-safe health output and update docs/runbooks.

### Decision 3 - Re-baseline before code edits

- **Decision:** The first implementation slice after this plan is a source/inventory/verification re-baseline.
- **Why:** The current worktree has extensive unrelated changes. Implementing from stale 2026-06-13 task rows would be unsafe.
- **Consequences:** Future code agents must confirm current source before touching API/Application/Persistence files.

### Decision 4 - Security and HAL correctness come before decomposition

- **Decision:** Finish P0 data exposure, resource authorization, and HAL affordance correctness before controller splits or handler cleanup.
- **Why:** Structural cleanup can move bugs around; it does not fix authorization or data exposure.
- **Consequences:** Controller decomposition remains blocked until high-risk auth/HAL issues have tests or explicit deferrals.

### Decision 5 - Behavioral risks get behavioral tests

- **Decision:** Use architecture tests for structural rules and unit/integration tests for behavior.
- **Why:** Idempotency replay, concurrency conflicts, audit emission, tenant isolation, and field-shape privacy are runtime behavior. Reflection tests alone cannot prove them.
- **Consequences:** Replace broad "add architecture test for X behavior" tasks with exact test-project expectations.

### Decision 6 - No speculative tenant abstraction

- **Decision:** Do not implement a new `TenantExecutionMode` enum just because the old plan proposed it. Introduce it only if it is the simplest way to formalize audited host-admin/background/system scopes.
- **Why:** Current fail-closed filters already exist. The real remaining risk is bypass semantics and auditability.
- **Consequences:** Phase 1C focuses on semantic bypass proof, reason-coded APIs, and health/operations visibility.

### Decision 7 - Persistence changes stay evidence-led

- **Decision:** Do not perform repo-wide no-tracking, cursor, index, hard-delete, or cancellation-token churn without selecting concrete endpoints/repositories.
- **Why:** Some tracked reads are valid; broad changes create risk without guaranteed improvement.
- **Consequences:** Persistence phases require a selected hotspot, test, and rollback/failure path.

## 6. Implementation Phases

### Phase 0 - Re-baseline and Approval Gate

- **Goal:** Make the current state trustworthy before more code changes.
- **Depends on:** This re-baselined plan.
- **Relevant files:** this workstream's plan/context/tasks; `endpoint-inventory.md`; `backend-contract-risk-register.md`; generated OpenAPI/inventory files only through generation workflow.
- **Acceptance criteria:**
  - The user approves or corrects this re-baselined plan.
  - Current `git status` and unrelated dirty files are recorded in context before implementation.
  - Current blockers are rechecked: architecture context failures, API integration/Docker issues, Blazor build issue.
  - Endpoint inventory and risk register are reconciled with the current branch or explicitly marked stale. As of 2026-07-03 the summary diff is recorded, but the row-level operation table remains historical until regenerated/re-imported.
- **Verification:** docs consistency grep; no source tests required unless generated artifacts are touched.

### Phase 1 - Security, Authorization, Tenant, and HAL Corrections

- **Goal:** Close the highest-risk data exposure and authorization/HAL drift before structural refactoring.
- **Relevant files:** P0 controllers/handlers/HAL policies/components identified by refreshed inventory.
- **Acceptance criteria:**
  - Event registration, tenant role grant, organization member, footer management, AI assistant, storage, email admin, and setup/bootstrap access rules are classified by resource/action.
  - Anonymous identity-bearing responses are blocked or replaced with safe public projections.
  - Blazor actions are gated by HAL links; broad route/menu checks remain the only allowed role/claim UI exception.
  - Tenant bypasses have bounded predicates, reasons, and semantic tests.
  - Self-host bootstrap/admin behavior has audit and missing-provider ProblemDetails coverage.
- **Verification:** `Event.API.IntegrationTests`, `Event.Application.UnitTests`, `Explore.Blazor.Client.Tests`, `Event.Persistence.IntegrationTests`, and relevant `Event.Architecture.Tests` slices by touched files.

### Phase 2 - API Contract, ProblemDetails, OpenAPI, and Operational Health

- **Goal:** Make API errors, route names, OpenAPI metadata, generated clients, and health/metrics contracts stable.
- **Relevant files:** `Explore.API/ExceptionHandling/**`, controllers, OpenAPI transformers, `Explore.ServiceDefaults/Extensions.cs`, docs.
- **Acceptance criteria:**
  - Remaining ad hoc error paths are migrated or explicitly deferred.
  - `ProblemDetails` metadata and snapshots cover representative validation/auth/not-found/conflict/rate-limit paths.
  - Route names and HAL references remain bidirectionally covered.
  - OpenAPI operation IDs, endpoint class, rate-limit, cache, and tenant posture metadata are current.
  - Health output is bounded and does not leak raw secrets, paths, endpoints, object keys, credentials, provider response bodies, or raw exception text.
  - Docs consistently reference `/health`, `/alive`, and `/metrics`.
- **Verification:** API contract tests, architecture tests, focused health endpoint tests, OpenAPI/client generation workflow when source contract changes.

### Phase 3 - Controller Decomposition Behind Stable Contracts

- **Goal:** Reduce API transport complexity without changing external semantics accidentally.
- **Relevant files:** large controllers selected from refreshed inventory, API request mapper classes, HAL assemblers/policies.
- **Acceptance criteria:**
  - Each split is resource/use-case justified, not method-count driven.
  - Existing route names remain stable unless a breaking change is recorded.
  - Controllers do not inject repositories or perform business orchestration.
  - API request-to-query mapping moves into small API-local mappers where it reduces controller noise.
  - Behavior tests prove old high-risk flows through new controller boundaries.
- **Verification:** API integration tests for changed controller families; route/HAL/OpenAPI tests.

### Phase 4 - Application/CQRS Use-Case Refactor

- **Goal:** Normalize handlers and use-case services without bypassing MediatR behaviors or transaction boundaries.
- **Relevant files:** selected commands/queries/handlers, Application services, unit tests.
- **Acceptance criteria:**
  - Oversized handlers such as event creation or AI run processing are split into narrow collaborators only after characterization tests.
  - Queries return DTO/list/page/null contracts, not command response envelopes.
  - Commands keep `BaseCommandResponse<TId>` or established delete/result patterns.
  - Idempotency, optimistic concurrency, audit, cache invalidation, and transaction boundaries are explicit for each changed use case.
  - Collaborators do not independently call `SaveChangesAsync` inside a broader unit-of-work unless the design explicitly permits it.
- **Verification:** `Event.Application.UnitTests`; CQRS architecture tests; API tests when response contracts change.

### Phase 5 - Persistence, Query Shape, Pagination, Indexes, and Reliability

- **Goal:** Improve data access where evidence shows tenant, performance, cancellation, lifecycle, or reliability risk.
- **Relevant files:** selected repositories, EF configurations, query specs, migrations, persistence tests.
- **Acceptance criteria:**
  - Repository contracts remain entity-first except explicitly named read-model/query-store ports.
  - Read-only paths use `AsNoTracking`; mutation paths keep tracked aggregate loads where needed.
  - DTO-shaped repository inputs are replaced with specifications/query objects/read-model ports.
  - Hard delete is quarantined behind explicit lifecycle/admin paths.
  - Cursor/keyset pagination is added only for selected high-volume endpoints with stable order, cursor binding, and schema/docs updates.
  - Index and migration work includes model assertions and rollback/reset notes for self-hosters.
  - Outbox/TickerQ/RabbitMQ transitions remain idempotent and retry-safe.
- **Verification:** `Event.Persistence.IntegrationTests`, model/index assertions, Application/API tests where contract changes.

### Phase 6 - Final Guardrails, Docs, and Release Evidence

- **Goal:** Turn temporary planning allowances into durable safeguards or explicit deferrals.
- **Relevant files:** docs, architecture tests, API changelog, generated artifacts, risk register.
- **Acceptance criteria:**
  - Every open risk row is mitigated or explicitly deferred with owner/date/rationale.
  - No temporary skipped/failing test remains without `Category:` and `Removal:` metadata.
  - Docs and generated artifacts match implemented behavior.
  - Full targeted verification evidence is recorded in context.
- **Verification:** build plus per-project test commands required by touched intents; no solution-level `dotnet test`.

## 7. Testing Strategy

Use the smallest test lane that proves the risk:

- **Architecture tests:** structural invariants only: layer dependencies, route-name coverage, endpoint classification metadata, no controller repository injection, no API domain entity responses, HAL source-of-truth guardrails, skip governance, ABOUTME headers.
- **API integration tests:** HTTP status, auth/authorization, ProblemDetails shape, HAL link sets, OpenAPI metadata, health endpoint response safety, rate-limit/idempotency/timeouts.
- **Application unit tests:** handler orchestration, manual validators, authorization metadata, audit/idempotency/concurrency/cache behavior.
- **Persistence integration tests:** tenant filters, bypass predicates, repository contracts, migrations/indexes/query shape.
- **Blazor client tests:** mutation affordances render only when `_links` contains the required rel.

Canonical commands are project-level. Do not use solution-level `dotnet test`.

## 8. Documentation, Configuration, And Operations Impact

Expected docs updates by slice:

- `docs/API.md`: route/error/HAL/OpenAPI/health contract changes.
- `docs/API_CHANGELOG.md`: every breaking pre-v1 API/client behavior change.
- `docs/OPERATIONS.md`: health/readiness/metrics, background workers, runbooks, retention, failure modes.
- `docs/SECURITY-MODEL.md` and `docs/AUTHORIZATION.md`: auth/resource policy/provider behavior.
- `docs/MULTI_TENANCY.md`: tenant resolution, fail-closed filters, bypass semantics.
- `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`, `docs/BACKUP_RESTORE_UPGRADE.md`: operator-visible config, upgrade, health, recovery paths where touched.
- `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`: regenerated only through the documented workflow.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Treat identity-bearing data exposure as a security issue, not a DTO polish issue.
- Instance admin, tenant admin, organization admin, group admin, standard user, and machine/API-key principals must remain distinct.
- Cerbos selected at instance level fails closed; local fallback is only when local mode or explicit BYO-open behavior is configured.
- HAL links encode per-resource affordances; UI must not duplicate resource authorization.
- Setup secret is bootstrap-only, not an identity.
- API-key scopes are a ceiling and do not bypass resource authorization.
- Rate limiting and idempotency must be preserved for high-risk writes and anonymous ingestion.
- Health/logs/metrics/traces must expose bounded operational labels only.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

- **Multi-tenancy:** Applicable. Every tenant-scoped read/write and background/system path needs explicit tenant behavior.
- **Federation:** Applicable only when touching ATProto/PDS/MCP/outbox paths. Do not expand protocol scope inside this refactor.
- **Localization:** Applicable only when touching localization admin/provider endpoints or error copy.
- **Accessibility:** Applicable for Blazor affordance changes; action visibility must not create inaccessible hidden state.
- **Product:** Applicable. The platform is pre-v1, self-hostable, white-label, and breaking changes are acceptable only when they simplify the durable contract.

## 11. Observability And Operations

- Use OpenTelemetry, Prometheus, Loki/structured logs. Do not introduce Sentry.
- Keep metric dimensions bounded. Do not tag raw URLs, IDs, object keys, prompts, provider errors, emails, subjects, or exception text.
- Preserve `/metrics`.
- Preserve readiness/liveness separation.
- Review health check payloads for safe data and safe exception handling.
- For background workers, expose durable state and low-cardinality metrics rather than only warning logs.

## 12. Migration And Compatibility Plan

- Breaking changes are allowed before v1.0 but must be documented in `docs/API_CHANGELOG.md` and reflected in generated OpenAPI/client artifacts.
- EF migrations must be small, focused, and non-destructive unless the user explicitly approves development reset behavior.
- Operator-visible behavior changes need self-hosting/upgrade/recovery notes.
- Do not preserve obsolete endpoints or tests only for compatibility.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Stale inventory drives wrong implementation. | High | High | Phase 0 re-baseline from current source/OpenAPI. | Endpoint tests disagree with inventory. | Phase 0 |
| Health responses leak provider details. | Medium | High | Add safe-output tests and redact generic exception messages if needed. | `/health` body contains endpoint/path/key/raw exception strings. | Phase 2 |
| Authorization drift remains after controller cleanup. | Medium | High | Close resource policies/HAL affordance tests before decomposition. | Missing/extra HAL links; 401/403 matrix failures. | Phase 1 |
| Tenant bypass semantics are assumed safe. | Medium | High | Add semantic bypass tests per call site. | Cross-tenant fixture reads wrong tenant rows. | Phase 1/5 |
| Broad repository cleanup breaks tracked mutation flows. | Medium | Medium | Split read-only and tracked contracts per use case. | Concurrency/update tests fail or entities not tracked. | Phase 5 |
| OpenAPI/client drift after breaking changes. | High | Medium | Regenerate through documented workflow after stable source changes. | Contract parity/client naming tests fail. | Phase 2/6 |
| Integration tests remain environment-blocked. | Medium | Medium | Record blocker, isolate no-infra tests, run Docker lanes when available. | Docker/Testcontainers or host shutdown failures. | Phase 0/6 |
| Dirty worktree hides unrelated user changes. | High | Medium | Read and work only scoped files; do not revert unrelated changes. | `git status` changes outside planned files. | All phases |

## 14. Success Metrics And Definition Of Done

- Security P0 rows are closed or explicitly deferred with owner/rationale.
- No anonymous endpoint returns identity/membership/role/grant/private tenant data unless explicitly approved and tested.
- HAL action affordances are API-authoritative and UI consumes `_links`.
- API ProblemDetails contract is centralized, documented, and tested.
- Route names and HAL policy route references are covered in both directions.
- `/health`, `/alive`, `/metrics` behavior is documented and safe for operators.
- Query handlers do not use command envelopes for read data.
- Persistence changes remain entity-first, tenant-safe, and tested.
- All generated artifacts are regenerated, not hand-edited.
- Plan/context/tasks are current at handoff.

## 15. Implementation Agent Contract - Keep Dev Docs Current

Future implementation agents must:

1. Read this plan, `backend-api-health-refactor-context.md`, and `backend-api-health-refactor-tasks.md` before editing.
2. Re-run intent classification for the specific slice.
3. Re-read matching docs/rules/skills for files touched.
4. Update this plan when architecture/scope/phasing changes.
5. Update context after each meaningful slice with files changed, decisions, validation, blockers, and next step.
6. Check off tasks immediately when completed and add newly discovered tasks.
7. Do not report done unless dev docs match reality.
8. Include a developer teaching summary in final responses.

## 16. Progress Reporting Contract

Implementation slice summaries should use:

- **Implemented:** what changed, patterns used, files/classes involved, and data/control flow.
- **Verified:** exact commands or manual checks.
- **Remaining:** known gaps and blockers.
- **Next:** the next concrete slice.
- **Docs updated:** whether plan/context/tasks and product docs are current.

## 17. Potential Risks And Unknowns

The hardest part is not controller splitting or handler extraction. The hardest part is keeping authorization, tenant isolation, HAL affordances, OpenAPI generation, and self-hosting operations aligned while the branch is already very active. Future agents should resist broad cleanup. Pick one risk boundary, prove it with the right test layer, update the docs, then move to the next slice.

## 18. Research Notes

- Context7 official ASP.NET Core docs (`/dotnet/aspnetcore.docs`) confirmed readiness/liveness health checks should be separate and can be filtered by tags/predicates through `HealthCheckOptions`.
- Tavily extracted Microsoft Learn ASP.NET Core health-check docs: readiness means ready to receive traffic; liveness means process should be restarted only when unhealthy. The examples use `/health/ready` and `/health/live`, but path names are not mandatory.
- Tavily extracted Microsoft Learn ASP.NET Core metrics docs: OpenTelemetry metrics plus Prometheus scraping endpoint are the recommended OSS Prometheus/Grafana path.
- Repo source (`Explore.ServiceDefaults/Extensions.cs`) currently implements this guidance with project-specific `/health`, `/alive`, and `/metrics` paths.
