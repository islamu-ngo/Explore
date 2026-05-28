<!-- ABOUTME: Phase 4.1/4.2 audit matrix for Layer 3 custom-property quota enforcement. -->
<!-- ABOUTME: Records covered write paths, closed quota gaps, and remaining lifecycle priorities. -->

# Custom-Property Quota Enforcement Audit

Last Updated: 2026-05-28 Europe/Brussels

## Classification

- **Workstream phase:** 4.1 custom-property quota enforcement audit; Phase 4.2 quota-gap implementation slice.
- **Matched intents:** `add-cqrs-handler` for Application-layer write/rebuild handlers, `update-repository-query` for persistence-backed quota reads and projection updaters, plus dev-doc maintenance.
- **Required docs/rules loaded:** `AGENTS.md`, `.claude/contract/intents.yaml`, `docs/QUICK_REFERENCE.md`, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/CUSTOM_PROPERTIES.md`, `docs/adr/ADR-006-custom-properties-runtime-boundary.md`, `.claude/rules/application-layer.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/domain.md`.
- **Skills loaded:** `.claude/skills/cqrs-mediatr-guidelines/SKILL.md`, `.claude/skills/dotnet-efcore-guidelines/SKILL.md`, `.claude/skills/clean-architecture-rules/SKILL.md`.
- **Context7 research:** EF Core docs rechecked named multi-tenant query filters and `AsNoTracking()` read-only query guidance. FluentValidation docs rechecked explicit `ValidateAsync` invocation for asynchronous validators.
- **Baseline:** `dotnet build --configuration Release --verbosity quiet` passed before this audit with existing package/deprecation warnings.
- **Phase 4.2 quota-slice verification:** `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed 1057/1057. `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed 178/179 with the known skipped response-metadata test.

## Quota Registry

| Setting | Effective purpose | Current default |
|---|---|---:|
| `custom_properties.max_definitions_per_tenant_per_entity_scope` | Shared Organization/Group definition count per tenant/scope | 500 |
| `custom_properties.max_definitions_per_event` | Runtime event-local definition count per event | 100 |
| `custom_properties.max_definitions_per_event_session` | Runtime session-local definition count per session | 50 |
| `custom_properties.max_options_per_definition` | Option rows per definition | 200 |
| `custom_properties.max_multi_value_rows_per_value` | Value rows in one multi-value replacement payload | 20 |
| `custom_properties.max_definitions_per_template` | Definitions in one event or session template | 100 |
| `custom_properties.projection_rebuild_batch_size` | Projection rebuild/drain batch size | 500 |
| `custom_properties.sync_apply_max_change_count` | Discrete changes in one template-sync apply plan | 200 |
| `custom_properties.sync_apply_max_payload_bytes` | Serialized sync plan size | 262144 |
| `custom_properties.max_dirty_scope_pending_per_tenant` | Pending dirty-scope rows before inline writes fail | 10000 |
| `custom_properties.projection_discovery_enabled` | Feature gate for projection-backed discovery | false |

Source of truth: `Explore.Domain/Settings/Definitions/CustomPropertyQuotaSettingDefinitions.cs`.

## Enforcement Matrix

| Area | Mutation path | Quota checks found | Status |
|---|---|---|---|
| Shared definitions | `CreateCustomPropertyDefinitionCommandHandler` | `max_definitions_per_tenant_per_entity_scope`; `max_options_per_definition` | Covered |
| Shared definitions | `UpdateCustomPropertyDefinitionCommandHandler` | `max_options_per_definition` | Covered |
| Shared definitions | delete / purge handlers | None needed; they reduce state and purge is dependency-blocked | Covered |
| Event runtime definitions | `CreateEventCustomPropertyDefinitionCommandHandler` | `max_definitions_per_event`; `max_options_per_definition` | Covered |
| Event runtime definitions | `UpdateEventCustomPropertyDefinitionCommandHandler` | `max_options_per_definition` | Covered |
| Event runtime definitions | delete / purge handlers | None needed; delete retires/removes projection, purge is dependency-blocked | Covered |
| Event runtime values | `SetEventCustomPropertyValueCommandHandler` | None needed for single ordinal write; runtime validator enforces shape | Covered |
| Event runtime values | `SetEventCustomPropertyMultiValuesCommandHandler` | `max_multi_value_rows_per_value` | Covered |
| Session runtime definitions | `CreateEventSessionCustomPropertyDefinitionCommandHandler` | `max_definitions_per_event_session`; `max_options_per_definition` | Covered |
| Session runtime definitions | `UpdateEventSessionCustomPropertyDefinitionCommandHandler` | `max_options_per_definition` | Covered |
| Session runtime definitions | delete / purge handlers | None needed; delete retires/removes projection, purge is dependency-blocked | Covered |
| Session runtime values | `SetEventSessionCustomPropertyValueCommandHandler` | None needed for single ordinal write; runtime validator enforces shape | Covered |
| Session runtime values | `SetEventSessionCustomPropertyMultiValuesCommandHandler` | `max_multi_value_rows_per_value` | Covered |
| Event templates | `CreateEventTemplateCommandHandler` | `max_definitions_per_template`; `max_options_per_definition` | Covered |
| Event templates | `UpdateEventTemplateCommandHandler` | `max_definitions_per_template`; `max_options_per_definition` | Covered |
| Session templates | `CreateEventSessionTemplateCommandHandler` | `max_definitions_per_template`; `max_options_per_definition` | Covered |
| Session templates | `UpdateEventSessionTemplateCommandHandler` | `max_definitions_per_template`; `max_options_per_definition` | Covered |
| Event template instantiation | `EventTemplateInstantiationService` | None directly | Acceptable if template creation/update is hardened first; service is in-memory and called from event creation |
| Event template sync | `EventTemplateSyncService.ApplySyncAsync` | `sync_apply_max_change_count`; `sync_apply_max_payload_bytes`; resulting `max_definitions_per_event`; resulting `max_options_per_definition` | Covered |
| Session template sync | `EventSessionTemplateSyncService.ApplySyncAsync` | `sync_apply_max_change_count`; `sync_apply_max_payload_bytes`; resulting `max_definitions_per_event_session`; resulting `max_options_per_definition` | Covered |
| Event projections | `RebuildEventCustomPropertyProjectionCommandHandler` and updater | `projection_rebuild_batch_size`; `max_dirty_scope_pending_per_tenant` | Covered |
| Session projections | `RebuildEventSessionCustomPropertyProjectionCommandHandler` and updater | `projection_rebuild_batch_size`; `max_dirty_scope_pending_per_tenant` | Covered |
| Dirty-scope drain | `DrainCustomPropertyProjectionDirtyScopesCommandHandler` and updater | Uses tenant rebuild batch size | Covered |
| Governance report | `GetCustomPropertyGovernanceReportQueryHandler` | Reads quotas for reporting | Covered as read-only signal |
| Event list / session list projection filters | list query handlers/specification path | Uses projection discovery flag, not cardinality quota | Covered by feature gate, not a write quota |

## Exact Gaps Found In Phase 4.1

1. **Event template options can exceed `max_options_per_definition`.**
   - `CreateEventTemplateCommandHandler` and `UpdateEventTemplateCommandHandler` cap definition count, but they build every nested option collection without checking the per-definition option quota.
   - Risk: a template can become an unbounded option fan-out source for every future event created from it.
   - **Closed in Phase 4.2 quota slice:** both handlers now resolve `max_options_per_definition` before governance/mapping and return `quota_exceeded` with `event_template_definition_options`.

2. **Event session template definitions and options have no quota checks.**
   - `CreateEventSessionTemplateCommandHandler` and `UpdateEventSessionTemplateCommandHandler` validate shape and uniqueness, but they do not inject `ICustomPropertyQuotaResolver`.
   - Risk: a session template can bypass both template definition caps and per-definition option caps.
   - **Closed in Phase 4.2 quota slice:** both handlers now resolve `max_definitions_per_template` and `max_options_per_definition` before governance/mapping and return `quota_exceeded` with session-template scopes.

3. **Template sync caps request size, not resulting runtime cardinality.**
   - `EventTemplateSyncService` and `EventSessionTemplateSyncService` enforce change-count and payload-byte quotas.
   - They do not preflight the resulting event/session definition count after applying selected added definitions.
   - They do not check resulting options per affected definition after selected added options or added definitions.
   - Risk: a previously valid event/session can exceed runtime cardinality through sync even when direct runtime create/update handlers would reject the same shape.
   - **Closed in Phase 4.2 quota slice:** both sync services now preflight selected added definitions and selected added options inside the transaction before writes. Quota failures throw `QuotaExceededException` instead of being downgraded to generic `apply_failed` conflicts.

4. **Template instantiation depends on template governance.**
   - `EventTemplateInstantiationService` copies definitions/options from an already-published template into a new event.
   - This is acceptable only if event template create/update is hardened first. Otherwise event creation can indirectly materialize over-quota runtime definitions/options.
   - **Resolved for quota purposes:** template create/update now enforce definition and option caps before templates can become instantiation sources.

## Hardening Order

1. [x] Patch event template create/update to enforce `max_options_per_definition`.
2. [x] Patch event session template create/update to enforce `max_definitions_per_template` and `max_options_per_definition`.
3. [x] Patch event and session template sync services with preflight helpers that compute resulting runtime counts before writes.
4. [x] Add Application unit tests for each failing quota path:
   - event template option quota create/update,
   - session template definition quota create/update,
   - session template option quota create/update,
   - event sync resulting definition/option quota,
   - session sync resulting definition/option quota.

## Phase 4.2 Quota Implementation Notes

- Template command handlers keep quota enforcement in the Application layer, before governance-policy calls and entity mapping.
- Template sync services compute selected/applicable additions from the client plan, fresh diff, target template, and tracked runtime definitions before calling any mutating repository method.
- Resulting definition count uses current tracked runtime definitions plus applicable selected added definitions.
- Resulting option count is checked for both newly added definitions and selected added options for existing tracked definitions.
- `QuotaExceededException` is allowed to escape sync apply so API exception handling can produce the canonical `quota_exceeded` problem response instead of a misleading sync conflict.

## Design Notes

- Keep quota checks in Application handlers/services because they are business/application governance rules, not EF mapping rules.
- Keep repositories entity-first; use repository count/read methods rather than exposing `IQueryable`.
- Keep validation manually instantiated and pass cancellation tokens through `ValidateAsync`.
- Keep projection quotas inside persistence updaters because dirty-scope backpressure and advisory-lock rebuild batching are persistence-owned operational controls.
