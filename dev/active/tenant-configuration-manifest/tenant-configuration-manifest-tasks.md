<!-- ABOUTME: Execution ledger for tenant manifest bootstrap and reporting-intake policy implementation. -->
<!-- ABOUTME: Tracks vertical slices, acceptance evidence, phase verification, and deferred reconciliation work. -->

# Tenant Configuration Manifest And Reporting-Intake Policy — Task Checklist

Last Updated: 2026-08-21 Europe/Brussels

## Status Summary

- **Overall status:** Draft; awaiting user approval
- **Completed:** 0/17 implementation tasks
- **Current priority:** `TCM-110 — Enforce effective submission and approval policy`
- **Next recommended slice:** Phase 1 policy integrity
- **Known blockers:** user approval; direct overlap must be resolved before editing already-dirty `.agents/contract/intents.yaml`, `.env.example`, or README
- **I-VSD:** [islamic-value-sensitive-design/i-vsd-tenant-configuration-manifest.md](../../../islamic-value-sensitive-design/i-vsd-tenant-configuration-manifest.md)
- **Plan:** [tenant-configuration-manifest-plan.md](tenant-configuration-manifest-plan.md)
- **Context:** [tenant-configuration-manifest-context.md](tenant-configuration-manifest-context.md)

## Implementation Maintenance Rules

1. This file is the hot execution ledger. Update it during implementation, not in a later cleanup pass.
2. Check a substantial task immediately after all acceptance items pass. Small related items may be reconciled together no later than phase end.
3. Keep implementation checkboxes separate from phase build/test checkboxes.
4. A phase completes only after every task and both phase-verification checkboxes pass.
5. Run phase verification once: one Release build and at most the one selected project test. Do not repeat green commands.
6. Update context after a phase, decision, blocker, validation failure, scope discovery, or handoff.
7. Update the plan only when scope, architecture, phase order, acceptance, risk, or verification strategy changes.
8. On cold resume, read context and the current task, then only the referenced plan section.
9. Preserve unrelated shared-workspace changes. Re-read dirty files before patching and ask if a direct conflict cannot be merged safely.
10. Never hand-edit EF migrations/model snapshots, generated API clients, or the generated schema artifact.
11. Tests are written with the owning behavior task; do not create separate QA, test, docs-review, or reporting tasks.
12. No fixed sleeps, timing-luck polling, weakened ratchets, skipped tests, suppressed diagnostics, or compatibility shims.

## Phase 1: Publication And Reporting Policy Integrity ⏳ NOT STARTED

Plan reference: Section 5.4 and Phase 1.

### [ ] TCM-110 — Enforce effective submission and approval policy

**Owning layers:** Application, Domain policy inputs, tests

**Implementation:**

- [ ] Inventory every backend path that can submit, create, import, federate, automate, approve, or publish an event.
- [ ] Add failing tests proving current unsafe bypasses or proving each path is already safe.
- [ ] Enforce disabled submission paths for user, organization, and group actors.
- [ ] Enforce admin approval as a real publication boundary for non-privileged actors.
- [ ] Classify import, federation, automation, and administrator paths explicitly.
- [ ] Keep lifecycle validation and authorization responsibilities separate.
- [ ] Update context with any newly discovered publication path or scope.

**Acceptance:**

- [ ] No non-privileged actor can cause public publication when approval is required.
- [ ] Disabled submission paths fail in Application regardless of controller/UI behavior.
- [ ] Tests exercise direct handlers and cannot pass through timing luck or over-isolated mocks.

### [ ] TCM-120 — Add reporting-intake setting and invariant evaluator

**Owning layers:** Domain setting definition, Application setting group/policy/validation, mutation paths

**Implementation:**

- [ ] Add `event_reporting.intake_enabled` with default `true` and tenant maximum scope.
- [ ] Add a dedicated typed reporting-intake policy group; do not add it to external-provider runtime options.
- [ ] Add a pure effective-publication-policy evaluator with stable result/reason codes.
- [ ] Add a complete proposed-state invariant validator.
- [ ] Integrate validation with tenant policy apply.
- [ ] Integrate validation with generic Control Plane setting writes.
- [ ] Integrate validation with tenant-plan assignment preflight.
- [ ] Protect any direct hierarchical guarded-key mutation path.
- [ ] Add truth-table, lock, stale-read, concurrent-change, and bypass regression tests.

**Acceptance:**

- [ ] Intake defaults enabled for existing and new tenants.
- [ ] External `Reporting:Mode`, `Reporting:Enabled`, `LocalOnly`, Osprey, and Coop behavior is unchanged.
- [ ] Every unsafe mutation path returns the same stable failure reason.
- [ ] No controller- or UI-only business validation exists.

### [ ] TCM-130 — Enforce intake policy on report reads, writes, and HAL

**Owning layers:** Application reporting, API HAL, tests

**Implementation:**

- [ ] Resolve effective intake policy in event report options.
- [ ] Reject direct report submission when intake is disabled.
- [ ] Omit event report, correction, and unsafe-link report HAL relations governed by intake.
- [ ] Preserve local canonical reporting whenever intake is enabled.
- [ ] Map stable Application failures to repository-standard RFC 7807 responses.
- [ ] Add report option, submission, and HAL regression coverage.

**Acceptance:**

- [ ] HAL, options, and direct POST agree for enabled/disabled/locked tenants.
- [ ] Bypassing HAL cannot submit a report while intake is disabled.
- [ ] No external provider is required for enabled local reporting.

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 2: Reporting Policy API Contract ⏳ NOT STARTED

Plan reference: Phase 2.

### [ ] TCM-210 — Add tenant reporting-intake policy query and mutation contract

**Owning layers:** Application, API, HAL, API integration tests

**Implementation:**

- [ ] Add query/update requests, DTOs, authorization facts, handlers, and manually instantiated validators.
- [ ] Return enabled/source/lock state plus server-authored `CanDisable` and stable reason.
- [ ] Add thin authenticated controller actions with route names, rate limits, request timeouts, response metadata, and immutable failure policies.
- [ ] Add HAL relations for view/update and omit unauthorized or locked affordances.
- [ ] Register HAL/OpenAPI wrapper schemas explicitly.
- [ ] Add authorization, wrong-tenant, lock, safe/unsafe update, and ProblemDetails tests.

**Acceptance:**

- [ ] API state is derived from effective settings, not request fields.
- [ ] Tenant-setting authorization and tenant isolation fail closed.
- [ ] Operation ids and HAL relations are stable and covered.

### [ ] TCM-220 — Reconcile tenant policy composition and generated contracts

**Owning layers:** Application tenant settings, API contract generation

**Implementation:**

- [ ] Integrate intake policy into current tenant policy read/write composition where required.
- [ ] Remove duplicated client/server policy calculations.
- [ ] Regenerate OpenAPI and the NSwag client after server stabilization.
- [ ] Update `docs/API_CHANGELOG.md` with the intentional development contract.
- [ ] Update contract tests; never hand-edit generated client output.

**Acceptance:**

- [ ] One server-authored policy contract drives all consumers.
- [ ] Generated client and schema reflect the final operation ids/types.
- [ ] No compatibility alias for pre-implementation proposal names exists.

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Manifest Contract, Catalog, And Schema ⏳ NOT STARTED

Plan reference: Sections 3.1–3.2, 5.1–5.3, and Phase 3.

### [ ] TCM-310 — Add strict manifest contracts and setting catalog

**Owning layers:** Application

**Implementation:**

- [ ] Add versioned manifest contract types under an Application tenant-manifest feature.
- [ ] Add an explicit manifest setting/document catalog referencing `SettingRegistry`.
- [ ] Opt in only tenant-safe non-sensitive settings and approved typed documents.
- [ ] Add manual structural/semantic/cross-reference validators.
- [ ] Reject duplicate tenant slugs, unknown keys, invalid scopes/types/allowed values, and sensitive keys.
- [ ] Add source-generated JSON metadata where required by repository serialization.
- [ ] Add catalog and contract unit tests.

**Acceptance:**

- [ ] Manifest types do not live in Domain or reference API/Persistence.
- [ ] New registry settings are not exposed automatically.
- [ ] Contract uses canonical flat setting keys and separate documents.

### [ ] TCM-320 — Add bounded strict JSON ingestion

**Owning layers:** Infrastructure, host-facing contracts

**Implementation:**

- [ ] Add typed options for `TENANT_MANIFEST_PATH` and `TENANT_MANIFEST_MODE`.
- [ ] Support `Off`, `ValidateOnly`, and `Bootstrap` only.
- [ ] Add convention discovery at `/etc/islamu-event/bootstrap/tenant-configuration.json`.
- [ ] Read one absolute regular non-symlink file once with a 4 MiB maximum.
- [ ] Reject invalid UTF-8, duplicate properties, unknown properties, case mismatches, unsupported version/kind, and trailing content.
- [ ] Compute SHA-256 over the exact bytes and keep content out of diagnostics.
- [ ] Add missing/absent/permission/oversize/symlink/duplicate/version/secret tests.

**Acceptance:**

- [ ] Explicit missing/unreadable paths fail; absent convention path is a no-op.
- [ ] Present invalid files fail before writes.
- [ ] Parser uses no new runtime JSON/YAML dependency.

### [ ] TCM-330 — Generate and govern JSON Schema Draft 2020-12

**Owning layers:** Application contract metadata, tooling, schema artifact, architecture tests

**Implementation:**

- [ ] Add a deterministic BCL-only schema generator tool.
- [ ] Generate `schemas/tenant-configuration-manifest-v1.schema.json`.
- [ ] Include immutable `$id`, Draft 2020-12 `$schema`, required properties, constraints, enums, and `additionalProperties: false`.
- [ ] Add an equality test between generated and checked-in schema.
- [ ] Package the exact schema artifact with releases.
- [ ] Publish/serve the exact bytes before docs advertise the canonical schema URL.
- [ ] Update schema/configuration documentation and provenance evidence.

**Acceptance:**

- [ ] Schema drift fails deterministically.
- [ ] Generated artifact is never hand-edited.
- [ ] No third-party source, schema expression, or unapproved package is ingested.

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 4: Transactional Tenant Bootstrap And Audit ⏳ NOT STARTED

Plan reference: Phase 4 and Sections 10, 12, 13.

### [ ] TCM-410 — Add manifest operation and tenant-result persistence

**Owning layers:** Domain audit entities, Application repository contracts, Persistence

**Implementation:**

- [ ] Add manifest operation and per-tenant result entities with UUIDv7 IDs.
- [ ] Persist version/kind/name/mode/digest/status/reason/timestamps and changed key names only.
- [ ] Add entity configurations, tenant-safe indexes, and repository implementation.
- [ ] Add failure recording that remains separate from the rolled-back configuration transaction.
- [ ] Extend the intent contract before editing newly required paths.
- [ ] Generate migrations/snapshots for every supported provider using canonical tooling.
- [ ] Add persistence and provider migration tests.

**Acceptance:**

- [ ] No raw manifest, values, secrets, report content, or PII is persisted in audit data.
- [ ] Migrations are generated, reversible, and model-accurate.
- [ ] Operation/result queries cannot cross tenant boundaries.

### [ ] TCM-420 — Add compile, preflight, and atomic bootstrap command

**Owning layers:** Application, Persistence transaction

**Implementation:**

- [ ] Add one manifest compile/preflight/apply command and service.
- [ ] Extract repository-native tenant creation behavior for reuse without nested MediatR dispatch.
- [ ] Resolve slugs, locks, catalog entries, documents, and effective policy before writes.
- [ ] Validate all items before opening the apply transaction.
- [ ] Apply all new tenants/settings/documents and audit results atomically.
- [ ] Skip existing tenants wholesale as `SkippedExisting`.
- [ ] Roll back all configuration on any write conflict/failure.
- [ ] Invalidate caches and publish notifications only after commit.
- [ ] Add rerun, same/different digest, multi-item, race, rollback, cancellation, and failure-audit tests.

**Acceptance:**

- [ ] Invalid manifests create no partial tenants/settings/documents.
- [ ] Bootstrap never fills or overwrites an existing tenant.
- [ ] Repeated startup is idempotent and diagnostically clear.

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 5: Standalone And Split Startup Integration ⏳ NOT STARTED

Plan reference: Sections 3.2, 8, 12 and Phase 5.

### [ ] TCM-510 — Integrate post-migration pre-traffic bootstrap

**Owning layers:** `Event.Standalone`, `Event.MigrationService`, Development API startup

**Implementation:**

- [ ] Run standalone validation/application after migration and before API/Blazor startup.
- [ ] Register only required manifest dependencies in `Event.MigrationService`.
- [ ] Run split bootstrap after migration and before the one-shot service exits.
- [ ] Keep split API replicas out of bootstrap.
- [ ] Use the same sequence in Development API only when that host owns migrations.
- [ ] Fail explicit invalid startup with non-zero exit and safe structured diagnostics.
- [ ] Add deterministic options, service-composition, host-order contract, invalid-file, cancellation, and rerun tests without starting Docker, a browser, or external services.

**Acceptance:**

- [ ] One topology owner applies a manifest before traffic.
- [ ] Standalone and split behavior share the same Application contract.
- [ ] No replica race or background API reconciliation exists.

### [ ] TCM-520 — Add deployment, standalone image, and recovery contract

**Owning layers:** Compose, environment example, self-host/operator docs, host tests

**Implementation:**

- [ ] Wire manifest env/path and read-only mount to `event-migrationservice` in Compose.
- [ ] Document `docker run --env-file` plus a separate read-only bind mount.
- [ ] Keep configuration under `/etc/islamu-event/bootstrap` and mutable data under `/app/data`.
- [ ] Document non-root read permissions and path diagnostics.
- [ ] Document a version/digest-pinned derived image using `COPY --chown`.
- [ ] Update `.env.example`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`, and required README links without overwriting unrelated dirty edits.
- [ ] Document modes, rerun, skip, validation, rollback, disablement, and schema workflow.

**Acceptance:**

- [ ] `.env` supplies a container-internal path; docs explicitly state it does not mount the host file.
- [ ] Convention discovery never uses `/app/data`.
- [ ] Enterprise examples avoid floating image tags.

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`

## Phase 6: Authenticated Export, HAL, And BFF Integration ⏳ NOT STARTED

Plan reference: Section 3.3 and Phase 6.

### [ ] TCM-610 — Add override and portable export queries

**Owning layers:** Application

**Implementation:**

- [ ] Add an authorized tenant-scoped export query.
- [ ] Implement `Overrides` default and explicit `Portable` view.
- [ ] Export only manifest-catalog settings/documents.
- [ ] Omit sensitive values and emit non-secret omission metadata.
- [ ] Preserve deterministic key/property ordering, encoding, and line endings.
- [ ] Add wrong-tenant, inherited, default, locked, document, ordering, and secret tests.

**Acceptance:**

- [ ] Overrides do not silently flatten inherited/default values.
- [ ] Portable output clearly declares flattened effective values.
- [ ] Output is stable for version-control diffing and is not represented as backup.

### [ ] TCM-620 — Add tenant and Control Plane export endpoints

**Owning layers:** API controllers, HAL, OpenAPI, API tests

**Implementation:**

- [ ] Add authenticated tenant-self and arbitrary-tenant Control Plane GET endpoints over the same query.
- [ ] Add route names, rate limits, request timeouts, authorization, response metadata, media type, and attachment filename.
- [ ] Add HAL export relations only for authorized callers.
- [ ] Register OpenAPI/HAL wrapper types and regenerate client artifacts.
- [ ] Update API changelog and endpoint/HAL/auth/secret tests.

**Acceptance:**

- [ ] Wrong-tenant and unauthorized export fail closed.
- [ ] Tenant-self and Control Plane outputs use identical manifest semantics.
- [ ] No controller business logic or direct secret handling exists.

### [ ] TCM-630 — Add server/BFF download integration

**Owning layers:** Blazor server/BFF integration

**Implementation:**

- [ ] Add generated-client-backed BFF service methods for both export views.
- [ ] Preserve bounded content, filename, content type, and ProblemDetails mapping.
- [ ] Keep access tokens and privileged headers out of browser-controlled code.
- [ ] Add authenticated download success/failure integration coverage.

**Acceptance:**

- [ ] Client components never call the API directly.
- [ ] Download failures retain traceable safe diagnostics.
- [ ] No temporary server file or unbounded buffer is introduced.

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 7: Tenant Administration And Accessibility ⏳ NOT STARTED

Plan reference: Phase 7 and Sections 9–11.

### [ ] TCM-710 — Add reporting-intake administration

**Owning layers:** Blazor client tenant settings, localization, accessibility

**Implementation:**

- [ ] Integrate reporting intake into the existing tenant admin settings composition.
- [ ] Gate mutation by HAL and use server-authored `CanDisable`/reason state.
- [ ] Explain local intake versus external-provider routing.
- [ ] Preserve an independent correction/legal/copyright contact route in UX/docs.
- [ ] Add localized, accessible, RTL-safe labels, validation, focus, and live announcements.
- [ ] Add enabled/safe/blocked/locked/unauthorized/error/success component tests.
- [ ] Reconcile policy wording with the I-VSD report and canonical configuration docs.

**Acceptance:**

- [ ] UI never calculates authorization or publication safety locally.
- [ ] Keyboard, focus, reflow, contrast, and announcements meet project WCAG 2.2 AA rules.
- [ ] Copy does not make religious or legal guarantees.

### [ ] TCM-720 — Add tenant manifest export interaction

**Owning layers:** Blazor client, generated client/BFF services, operator guidance

**Implementation:**

- [ ] Add a HAL-gated export control in current tenant settings UX.
- [ ] Offer Overrides and Portable views with clear inheritance/secret/backup guidance.
- [ ] Use deterministic accessible filenames.
- [ ] Announce download success/failure without exposing filesystem details.
- [ ] Add component/service tests and final self-host/API documentation links.
- [ ] Record managed reconciliation as deferred with no dormant enum or stub.

**Acceptance:**

- [ ] Export is absent when HAL does not authorize it.
- [ ] Secret omissions and view semantics are understandable before download.
- [ ] UI, API, docs, schema, and I-VSD terminology agree.

### Phase 7 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Remaining / Deferred Work

- Managed `Reconcile` mode with persisted field ownership, drift reporting, conflict policy, deletion semantics, dry-run/diff, takeover rules, and distributed coordination.
- YAML parsing and schema/tooling support.
- Secret-reference providers such as environment, Docker secrets, Infisical, or Kubernetes Secrets.
- Manifest directory/multi-file composition.
- Jurisdiction-specific legal/copyright workflow requirements.
- Production stakeholder studies and moderation outcome metrics.
- File-size changes beyond the v1 4 MiB boundary, only after measured need.

## Synchronization Rules

- Plan, context, and task status must always agree on current phase, next task, blockers, deferred work, and verification.
- A checked implementation task must have acceptance evidence in its task entry or the latest context handoff.
- A checked phase must have one recorded Release build and its selected project test.
- When implementation discovers a new publication or mutation path, update context immediately and change the plan only if scope/architecture/phase order changes.
- When a task changes API, schema, migration, generated client, configuration, or operator behavior, fold the required artifact/docs update into that task before checking it.

## Progressive Maintenance Cadence

- **After a substantial task:** check the task and add concise evidence.
- **At phase end:** run the phase build/test once, check verification, refresh context, and set the next task.
- **On decision/blocker/failure:** refresh context immediately; update plan only for strategy-level change.
- **Before handoff/PR:** reconcile all affected checkboxes, add a dated handoff, identify unrelated dirty files, and state deferred work.

## Read Cadence

- **Cold resume:** context → current task → named plan section.
- **Uninterrupted work:** current task only; reopen exact source/docs as needed.
- **Do not:** reread all workstream artifacts after every task or rerun unchanged green baselines.

## Stale-State Recovery

If artifact state disagrees with code or validation:

1. Inspect the current diff and latest completed behavior.
2. Correct the plan only if architecture/scope/phase order changed.
3. Reconcile context decisions, blockers, baseline, and next task.
4. Reconcile task checkboxes and phase verification from actual evidence.
5. Do not sweep unrelated workstreams or turn the journal into a session log.

