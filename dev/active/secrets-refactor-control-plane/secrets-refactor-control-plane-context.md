ABOUTME: Session context and key reference map for the Secrets Refactor (Control Plane / Data Plane) task.
ABOUTME: Links to every file being introduced, replaced, or deleted so any session can pick up the refactor without repeating exploration.

# Secrets Refactor - Context

Last Updated: 2026-04-24 (Enterprise Revision v2.0)

## SESSION PROGRESS

### ✅ COMPLETED
- Research: enterprise secrets management 2025-2026, clean architecture DDD, zero-trust rotation, Vault/Infisical enterprise, EF Core value converters, ASP.NET Core Data Protection.
- Oracle architecture review (timed out, gap analysis produced directly).
- Gap analysis: 28 enterprise improvements identified, categorized as MUST-HAVE (10), SHOULD-HAVE (6), NICE-TO-HAVE (4 post-1.0).
- Plan revision: all 28 improvements woven into existing 6-phase structure without adding phases.
- **Phase 1 — COMMITTED as `38ce8098`**: SecretBinding + SecretDefinitionRegistry foundations.
- **Phase 2 — COMMITTED as `fc0b2b5a`**: Discrete Postgres bootstrap via NpgsqlConnectionStringBuilder.
- **Phase 3 runtime pipeline — WRITTEN TO DISK, NOT COMMITTED**: 14 files need updates for enterprise patterns.

### 🟡 IN PROGRESS — Phase 3 (Enterprise Revision)

**Phase 3 requires significant changes to the 14 uncommitted files plus many new files.**

Files already on disk that need updates:
1. `Explore.Domain/Secrets/Events/SecretBindingUpdatedEvent.cs` — add Version, Status, AuditAction
2. `Explore.Application/Contracts/Secrets/ResolvedSecret.cs` — add Version, TtlExpiresAt
3. `Explore.Application/Contracts/Secrets/ISecretResolver.cs` — add ResolveRequiredAsync, ValidateAsync
4. `Explore.Application/Contracts/Secrets/ISecretSource.cs` — ValidateAsync returns SecretValidationDetail
5. `Explore.Application/Contracts/Secrets/IInfisicalClientFactory.cs` — minor updates
6. `Explore.Secrets/Sources/EnvironmentSecretSource.cs` — timeout-only Polly + ValidateAsync
7. `Explore.Secrets/Sources/InlineSecretSource.cs` — timeout-only Polly + ValidateAsync
8. `Explore.Secrets/Sources/InfisicalSecretSource.cs` — full Polly pipeline + ValidateAsync
9. `Explore.Secrets/Infrastructure/InfisicalClientFactory.cs` — minor updates
10. `Explore.Secrets/Observability/SecretResolverMetrics.cs` — resilience event counters
11. `Explore.Secrets/Services/SecretResolver.cs` — MAJOR: HybridCache, Status=Active filter, version-aware, ResolveRequiredAsync, ValidateAsync
12. `Explore.Secrets/Services/AuditingSecretResolverDecorator.cs` — MAJOR: persistent audit via IAuditWriter
13. `Explore.Secrets/HealthChecks/SecretResolverHealthCheck.cs` — MAJOR: per-source granularity + TTL + circuit breaker state
14. `Explore.Secrets/Extensions/SecretResolutionServiceCollectionExtensions.cs` — MAJOR: Polly, HybridCache, audit, resilience

New files to create:
- `Explore.Domain/Secrets/SecretBindingAuditEntry.cs`
- `Explore.Domain/Secrets/SecretBindingAuditAction.cs`
- `Explore.Domain/Secrets/SecretBindingStatus.cs`
- `Explore.Domain/Secrets/SecretValidationCategory.cs`
- `Explore.Application/Contracts/Secrets/SecretValidationDetail.cs`
- `Explore.Application/Contracts/Secrets/SecretNotConfiguredException.cs`
- `Explore.Application/Contracts/Persistence/ISecretBindingAuditRepository.cs`
- `Explore.Application/Contracts/Secrets/IAuditWriter.cs`
- `Explore.Secrets/Resilience/SecretResiliencePipeline.cs`
- `Explore.Secrets/Resilience/SecretResilienceOptions.cs`
- `Explore.Secrets/Services/SecretBindingAuditWriter.cs`
- `Explore.Persistence/Repositories/SecretBindingAuditRepository.cs`
- `Explore.Persistence/Configurations/Entities/SecretBindingAuditEntryConfiguration.cs`
- `Explore.Persistence/Migrations/{timestamp}_AddSecretBindingEnterpriseColumns.cs`
- + Admin surface files (DTOs, validators, commands, queries, handlers, controller, HATEOAS, Cerbos, DI wiring)
- + ~60-70 test files

### 📋 NEXT SESSION ENTRY POINT

**Read in order:**
1. This file (you are here)
2. `dev/active/secrets-refactor-control-plane/phase-3-implementation-plan.md` — full execution blueprint with enterprise patterns
3. `dev/active/secrets-refactor-control-plane/secrets-refactor-control-plane-tasks.md` — checkbox list
4. Verify the 14 uncommitted Phase 3 files still exist via `git status`

**Remaining work (in order):**
- 3.1 EF migration for enterprise columns + audit table
- 3.2 Domain: audit entry, status, validation category enums + SecretBinding updates
- 3.3 Domain event update for versioning
- 3.4 Application contracts: SecretValidationDetail, SecretNotConfiguredException, IAuditWriter, ISecretBindingAuditRepository
- 3.5 Resilience pipeline (Polly)
- 3.6 Per-source implementations with Polly + structured validation
- 3.7 Core resolver with HybridCache + version-aware + Status=Active filter
- 3.8 Auditing decorator with persistent audit trail
- 3.9 Per-source health check with granularity
- 3.10 Tenant isolation query filter
- 3.11 DI registration + resilience configuration
- 3.12-3.19 Admin surface, tests, verification, commit

**Key entity reality check (from committed Phase 1 code):**
- `SecretBinding` primary key: `string SettingKey` (NOT `int SecretKeyId`)
- `SecretScope` enum: `Instance = 0, Tenant = 1`
- `SecretSourceType` enum: `Infisical = 0, InlineEncrypted = 1, EnvironmentVariable = 2`
- Factory methods: `CreateInfisical/CreateInlineEncrypted/CreateEnvironmentVariable`, `SwitchTo*`, `RecordValidation`
- Entity is `IAuditableEntity` (NOT `ISoftDeletable`) → hard delete
- Registry is `SecretDefinitionRegistry` with `FrozenDictionary`

**Enterprise changes that require EF migration:**
- `SecretBinding.Version` (int, default 1)
- `SecretBinding.Status` (SecretBindingStatus enum: Active=0, Pending=1, Previous=2)
- `SecretBinding.TtlExpiresAt` (DateTime?)
- `SecretBinding.LastRotatedAt` (DateTime?)
- `SecretBinding.LastValidationCategory` (SecretValidationCategory enum)
- NEW `SecretBindingAuditEntries` table
- Updated filtered unique indexes (include `Status = Active`)

**Verbatim user directives still in force:**
- NO backward compatibility (dev mode)
- Enterprise-grade quality, clean architecture, design patterns
- Single Phase 3 commit at end
- Follow repo conventions and industry best practices

### ⚠️ BLOCKERS
- **None** — clean handoff. Runtime files are on disk for reference, enterprise additions not yet started.

## Enterprise Architecture Decisions (ADRs)

### ADR-001: DB as Control Plane, External Sources as Data Plane
One `SecretBinding` row per (SettingKey, Scope, ScopeId) declares the single source. No fallback chain.

### ADR-002: Normalized Metadata Columns over Polymorphic JSON
CHECK constraint enforcing exactly one metadata group per SourceType. Indexable, type-safe.

### ADR-003: Persistent Audit Trail (Not Just Structured Logs)
`SecretBindingAuditEntry` table for every mutation. 1% sampled reads via decorator logs.

### ADR-004: Versioned Rotation (Blue/Green)
`Version` (int) + `Status` (Active/Pending/Previous). Only Active resolved. Promotion is atomic.

### ADR-005: HybridCache over IMemoryCache
L1 memory + L2 distributed (Redis). Tag-based invalidation for multi-instance.

### ADR-006: Polly Resilience on External Calls
Retry (3x exponential backoff) + Circuit breaker (5 failures → 30s open) + Timeout (10s Infisical, 5s others) + Bulkhead (20 concurrent).

### ADR-007: Structured Validation Results
`SecretValidationCategory` enum for actionable diagnostics. API sees category only, not diagnostic message.

### ADR-008: Tenant Isolation via Query Filter
EF Core global query filter on SecretBinding: `Scope == Instance || ScopeId == _currentTenantId`. Admin bypass with `IgnoreQueryFilters()` gated by Cerbos.

### ADR-009: Lease/TTL Metadata
`TtlExpiresAt` (DateTime?) for dynamic secret expiration. `LastRotatedAt` for rotation tracking. Health check degrades on expired TTL.

### ADR-010: File-Based Secret Source (Phase 5)
`SecretSourceType.File` + `FilePath` column. Docker/K8s `/run/secrets/` support. Deferred to Phase 5.

## Quick Resume

1. Read `secrets-refactor-control-plane-plan.md` (strategy + ADRs).
2. Read `phase-3-implementation-plan.md` (execution blueprint).
3. Read `secrets-refactor-control-plane-tasks.md` and find the first unchecked task.
4. Mark the task `in_progress` via `todowrite`.
5. Follow the task's file path + acceptance criteria.
6. Run `dotnet build --configuration Release --verbosity quiet`, then test projects individually per AGENTS.md.
7. On PR close, update this file and the tasks file.

## Key Files - New (to be created or updated)

### Domain layer (`Explore.Domain/`) — NEW in Phase 3
- `Explore.Domain/Secrets/SecretBindingAuditEntry.cs`
- `Explore.Domain/Secrets/SecretBindingAuditAction.cs`
- `Explore.Domain/Secrets/SecretBindingStatus.cs`
- `Explore.Domain/Secrets/SecretValidationCategory.cs`
- `Explore.Domain/Secrets/Events/SecretBindingUpdatedEvent.cs` (UPDATE on disk)

### Application layer (`Explore.Application/`) — NEW in Phase 3
- `Explore.Application/Contracts/Secrets/SecretValidationDetail.cs`
- `Explore.Application/Contracts/Secrets/SecretNotConfiguredException.cs`
- `Explore.Application/Contracts/Secrets/IAuditWriter.cs`
- `Explore.Application/Contracts/Persistence/ISecretBindingAuditRepository.cs`
- (Plus: DTOs, Commands, Queries, Handlers, Notification Handlers, Validators)

### Secrets infrastructure (`Explore.Secrets/`) — NEW in Phase 3
- `Explore.Secrets/Resilience/SecretResiliencePipeline.cs`
- `Explore.Secrets/Resilience/SecretResilienceOptions.cs`
- `Explore.Secrets/Services/SecretBindingAuditWriter.cs`
- (Plus: UPDATE all 14 existing on-disk files)

### Persistence (`Explore.Persistence/`) — NEW in Phase 3
- `Explore.Persistence/Repositories/SecretBindingAuditRepository.cs`
- `Explore.Persistence/Configurations/Entities/SecretBindingAuditEntryConfiguration.cs`
- `Explore.Persistence/Migrations/{timestamp}_AddSecretBindingEnterpriseColumns.cs`

### Committed Phase 1-2 files (unchanged)
- See previous context file versions for full list of committed files.

## Key Files - Deleted (PR 6)

Same as previous revision — see `secrets-refactor-control-plane-context.md` history.

## Open Questions (tracked for PR reviews)

1. Should the admin UI expose a "Copy Infisical path from registry default" button? (Probably yes; low cost.)
2. For tenant-scoped bindings, does the descriptor expose instance fallback metadata? (Default: yes, gate behind Cerbos `tenant:read_instance_secret_metadata`.)
3. Does setup-secret UI migrate into the new `/Admin/Secrets` page, or stay in setup/onboarding flow? (Phase 4 decides; leaning: stays in setup but mirrors the card design.)
4. `ITenantContext` injection — does this already exist in the codebase, or must it be created? (Check `Explore.Application/Services/` for tenant context patterns.)

## Reference Docs To Open When Working On Specific Tasks

- `docs/ARCHITECTURE.md` - Clean Architecture enforcement.
- `docs/SECURITY-MODEL.md` - BFF + multi-client audience validation + Cerbos.
- `docs/QUICK_REFERENCE.md` - Critical rules.
- `docs/SECRETS.md` - Will be rewritten at end of Phase 6.
- `.claude/skills/` — all project skills as listed in previous context.

## Session Handoff — 2026-05-03 Europe/Brussels

No implementation work was performed for this active task during the sidebar dock refactor handoff session. Existing context, plan, and task files remain the authoritative state for this workstream. Do not infer progress or blockers here from the sidebar/dock-specific changes unless a future session explicitly broadens scope.
