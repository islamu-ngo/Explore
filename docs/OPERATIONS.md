ABOUTME: Operational runbook for startup, health, shutdown, and runtime safeguards.
ABOUTME: Captures current behavior implemented in API, Blazor BFF, migration service, and service defaults.

# Operations

> **Audience:** Operators | Contributors | AI agents
> **Status:** Mixed
> **Owner:** Platform/Ops
> **Last Verified:** 2026-07-04
> **Source Anchors:** `Explore.AppHost/AppHost.cs`, `Explore.API/Program.cs`, `Explore.API/HealthChecks/StorageReadinessHealthCheck.cs`, `Explore.API/HealthChecks/StorageReconciliationHealthCheck.cs`, `Explore.API/BackgroundServices/StorageReconciliationProcessor.cs`, `Explore.Infrastructure/StorageObjectDeletionService.cs`, `Explore.ServiceDefaults/`, `docker-compose.yml`, `docs/SELF_HOSTING.md`, `docs/BACKUP_RESTORE_UPGRADE.md`, `docs/TROUBLESHOOTING.md`

This page is the operational reference for implemented runtime behavior. Task procedures should live in dedicated runbooks and be linked from here.

## Operational Runbooks

| Task | Runbook | Use When |
|---|---|---|
| Install or update a self-hosted stack | [SELF_HOSTING.md](SELF_HOSTING.md) | You need Compose topology, ports, setup secret behavior, Keycloak, MinIO, Cerbos, or reverse-proxy boundaries. |
| Back up, restore, upgrade, or roll back | [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) | You are preparing a release, recovering an environment, or testing disaster recovery. |
| Diagnose repeated symptoms | [TROUBLESHOOTING.md](TROUBLESHOOTING.md) | You have a concrete failure such as `401`, `429`, `504`, unhealthy readiness, setup-secret errors, or secret-provider failures. |
| Validate release readiness | [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) | A change affects migrations, configuration, secrets, security, upgrade paths, or operator docs. |

## Localization Static Bundle Operations

No-TMS and provider-fallback localization bundles are stored under the API
content root:

```text
{ContentRoot}/App_Data/Localization/Bundles/{code}.json
```

The API writes this path through `IBundleFileWriter` using a temp-file then
rename flow. The bundle health endpoint (`GET /api/admin/localization/bundle-health`)
checks whether the directory can be created and written. Admin import/export
endpoints are authenticated and invalidate the translation resolver cache after
successful writes.

For single-instance deployments, the local path is sufficient. For multi-replica
deployments, mount `App_Data/Localization/Bundles` on a shared persistent volume
or replace `IBundleFileWriter` with a distributed implementation. Without shared
storage, one replica can import a bundle while another keeps serving only its
local embedded/writable state.

Back up writable bundles together with other deployment-owned persistent data.
Embedded bundles still provide defaults after restore, but local operator edits
live only in the writable bundle path.

Operational telemetry for this area is exported by the `Explore.Translation`
meter. Watch `islamu.tms.fallback_activated_total` for connected-provider
degradation and `islamu.localization.static_bundle_operation_total` for static
bundle import/export success or validation failures. Metrics use provider,
language, operation, result, and fallback-reason tags only; translation keys,
bundle contents, and TMS secrets must never be emitted as metric tags.

## Read-Only Doctor Diagnostics

`Explore.Diagnostic` includes a read-only doctor CLI for self-hosting and local-environment preflight checks:

```bash
dotnet run --project src/Explore.Diagnostic/Explore.Diagnostic.csproj -- --root .
```

The doctor prints deterministic `PASS`, `WARN`, and `FAIL` results with remediation links. It exits `0` when all checks are `PASS` or `WARN`, and exits `1` when any check is `FAIL`.

Current checks cover:

- .NET SDK version versus `global.json`;
- Docker and Docker Compose availability;
- Aspire CLI availability;
- Compose service topology and BFF `API_ENDPOINT` alignment;
- discrete PostgreSQL bootstrap variables expected by `BootstrapSecretLoader`;
- presence of operator remediation docs;
- review-first AI tool readiness artifacts, generated inventories, registry tests, and agent hardening docs.

Non-negotiable safety boundary: doctor does **not** repair configuration, generate secrets, start containers, start Aspire, run migrations, seed data, call setup write endpoints, or persist setup state. Use it before running Compose/Aspire or when diagnosing a self-hosting setup, then follow the linked remediation docs for corrective action.

Sensitive values are redacted before output. Do not add checks that print raw connection strings, passwords, setup secrets, bearer tokens, cookies, authorization headers, or secret-provider responses.

## Local Startup Topology (Aspire)

`Explore.AppHost/AppHost.cs` selects topology from `ISLAMU_ASPIRE_MODE`, normally through `Explore.AppHost/Properties/launchSettings.json`:

| Launch profile | Mode | Started by Aspire |
|---|---|---|
| `https` | `FullLocal` | Compatibility alias for the contributor full-local topology. |
| `local-default` | `DefaultLocal` | Default lightweight local platform: PostgreSQL, Redis cache, RabbitMQ, Mailpit, Keycloak, Cerbos, MinIO, migrations, API, and Blazor. Svix is added only for explicit `Svix`/`Composite` webhook provider selection. |
| `local-full` | `FullLocal` | The default-local resources plus Coop, Osprey, PgAdmin, Prometheus, Grafana, and other heavy extras. Svix is added only for explicit `Svix`/`Composite` webhook provider selection. |
| `local-core` | `LocalDataExternalPlatform` | PostgreSQL `postgres` with app database `islamu_event_db`, Redis `cache`, Mailpit, `Event.MigrationService`, `Explore.API`, and `Explore.Blazor`. Auth, policy, storage, webhooks, and moderation providers come from Infisical/config. |
| `local-lite` | `ExternalInfra` | Mailpit, `Event.MigrationService`, `Explore.API`, and `Explore.Blazor`. All infrastructure comes from Infisical/config. |

Contributor default:

```bash
aspire run --apphost src/Explore.AppHost/Explore.AppHost.csproj
```

Install the Aspire CLI first if `aspire` is missing:

```bash
curl -sSL https://aspire.dev/install.sh | bash
```

`AspireRunModeExtensions.Parse` defaults a missing `ISLAMU_ASPIRE_MODE` to `DefaultLocal`, so the contributor path does not require a launch-profile name. `aspire run` is interactive and exits when you press `Ctrl+C`.

Foreground isolated run for repeatable infrastructure launch proof:

```bash
aspire run --apphost src/Explore.AppHost/Explore.AppHost.csproj --isolated
```

For concurrent worktrees or repeated infrastructure proofs, isolate the Aspire run and discover ports from Aspire resource metadata in another shell while the foreground run is alive. Interactive Keycloak OIDC login is intentionally unsupported in `--isolated` runs: isolation randomizes the BFF port, while the realm uses exact callback URIs. Use one of the named non-isolated profiles for authentication testing.

```bash
aspire ps --format Json
aspire describe explore-api --apphost src/Explore.AppHost/Explore.AppHost.csproj --format Json
aspire describe mailpit --apphost src/Explore.AppHost/Explore.AppHost.csproj --format Json
```

Detached Aspire commands remain useful for CLI lifecycle investigation, but they are not the current canonical launch proof path for this workspace. Official Aspire CLI documentation says `aspire start` starts an AppHost in the background and leaves it inspectable with `aspire ps`, `aspire describe`, `aspire logs`, and `aspire stop`. On 2026-07-04, local Aspire CLI `13.4.6` repeatedly returned detached startup JSON after AppHost readiness, then the AppHost process disappeared and `aspire ps --format Json` returned `[]`. If that reproduces, use the foreground `aspire run --isolated` path above and inspect the detached child log under `~/.aspire/logs/`.

Maintainer modes:

```bash
ISLAMU_ASPIRE_MODE=FullLocal aspire run --apphost src/Explore.AppHost/Explore.AppHost.csproj
ISLAMU_ASPIRE_MODE=LocalDataExternalPlatform aspire run --apphost src/Explore.AppHost/Explore.AppHost.csproj
ISLAMU_ASPIRE_MODE=ExternalInfra aspire run --apphost src/Explore.AppHost/Explore.AppHost.csproj
```

Launch profiles remain available through `dotnet run` for IDEs and compatibility:

```bash
dotnet run --project src/Explore.AppHost/Explore.AppHost.csproj --launch-profile local-full
dotnet run --project src/Explore.AppHost/Explore.AppHost.csproj --launch-profile local-default
dotnet run --project src/Explore.AppHost/Explore.AppHost.csproj --launch-profile local-core
dotnet run --project src/Explore.AppHost/Explore.AppHost.csproj --launch-profile local-lite
```

`local-full` uses persistent container lifetimes and named volumes for heavy stateful resources so local database, Keycloak, MinIO, RabbitMQ, PgAdmin, and observability state survive AppHost restarts. Every non-isolated named profile publishes API HTTPS on `https://localhost:7039` and Blazor HTTPS on `https://localhost:7177`; internal HTTP endpoints remain dynamically allocated for Aspire service discovery. Isolated runs publish dynamic localhost ports, so use `aspire describe <resource> --format Json` instead of hardcoding resource endpoints. Because Keycloak callback allow-lists remain exact, use a named non-isolated profile—not `--isolated`—for OIDC login/logout verification.

PgAdmin is available as the `pgadmin` browser resource in `local-full` with development credentials `admin@openislamu.org` / `admin`. It imports the PostgreSQL servers from `Explore.AppHost/Config/pgadmin/servers.json`; inside PgAdmin, use container-network hosts `postgres`, `cerbos-db`, `svix-postgres`, and `coop-postgres` on port `5432`, not Aspire dashboard endpoint strings such as `tcp://localhost:35305`. The fixed-password Cerbos, Svix, and Coop databases are covered by `Explore.AppHost/Config/pgadmin/pgpass`; the app `postgres` server may still prompt for the Aspire-generated local database password.

To reset only the local app database while keeping the persistent Postgres container/volume, connect to the `postgres` server and drop/recreate `islamu_event_db`. The Aspire resource alias is `islamu-event-db` because resource names cannot contain underscores. Do not delete the `islamu-event-postgres-data` volume unless you also want to rotate the generated Postgres credentials and lose every database in that server.

Secret and connection priority:

- `local-full` forces `SecretProvider__Provider=None` for child projects, clears Infisical bootstrap identifiers, and supplies local Keycloak, Cerbos, MinIO, Svix, Coop, Mailpit SMTP, storage, and database settings. Contributors should not need Infisical credentials.
- `ConnectionStrings:DefaultConnection` has first priority for EF Core; Aspire `WithReference` supplies it in `local-full` and `local-core`.
- Mailpit SMTP is local in every Aspire profile. Non-isolated runs use the configured development Mailpit ports; isolated runs use Aspire-assigned dynamic ports. Development database seeding uses `MAIL_SMTP_*` values, then `SMTP_*` aliases, then local defaults when `email.smtp_host` is empty or still set to the retired `mailpit.openislamu.org` default. In `ISLAMU_ASPIRE_MODE=FullLocal`, seeding refreshes those Development SMTP rows on each run so persistent local database volumes follow the current isolated Mailpit port.
- Self-hosted local Keycloak may also be configured to use Mailpit or shared SMTP for Keycloak realm email. That is Keycloak realm SMTP plumbing, not product Basic Dispatch configuration: identity lifecycle emails still come from Keycloak and do not create `EmailDispatchOutbox` rows.
- When no connection string is supplied, `BootstrapSecretLoader` resolves PostgreSQL fields from Infisical `/postgresql`, then `POSTGRESQL_*` environment variables, then `Postgresql:*` configuration.
- `local-core` and `local-lite` are maintainer modes. If Infisical bootstrap credentials are present in user secrets or environment variables, raw Infisical bootstrap values can outrank local `POSTGRESQL_*` fallback values. Blank the Infisical bootstrap keys for env-only local debugging.

Keycloak local infrastructure imports the repository realm export from `docker/keycloak/realm-export.json`. Aspire mounts that file into `/opt/keycloak/data/import/realm-export.json` and starts Keycloak with `--import-realm`; Docker Compose mounts the same file and then runs `keycloak-init` to synchronize the confidential Blazor client secret plus managed realm/client security settings. The export contains no client secret. Aspire sets `KC_HTTP_RELATIVE_PATH=/auth`, so its management readiness probe is `/auth/health/ready`. Keycloak skips startup import when the realm already exists in the persistent database; `keycloak-init` repairs the managed policy/client fields, while a disposable database reset is still required for unrelated export-only changes.

Startup dependencies are explicit:

1. Local data profiles create PostgreSQL and Redis first.
2. `local-full` creates platform infrastructure, including CockroachDB before Phase Two Keycloak and Cerbos PostgreSQL before Cerbos.
3. `Event.MigrationService` runs in every profile. Local data profiles provide PostgreSQL through Aspire `WithReference(database, "EventMigrationService")` and `WithReference(database, "DefaultConnection")`; `local-lite` resolves the external database from Infisical/config.
4. `Explore.API` waits for migration completion, local data/cache, and `local-full` platform resources when those resources exist.
5. `Explore.Blazor` waits for API readiness and receives API service discovery through Aspire.
6. Dedicated admin hosts use the same `Explore.Blazor` process and generated API client boundary.

The Blazor BFF resolves the API through Aspire service discovery (`services__explore-api__https__0` / `services__explore-api__http__0`) or `ExploreApi:BaseUrl`. Compose uses `API_ENDPOINT`, defaulting to the internal `islamu-event-api:8080` service. Do not hardcode the Compose/API host port into AppHost documentation.

Aspire local development uses the local filesystem storage provider by default unless a profile supplies S3-compatible settings. AppHost sets `Storage:Local:RootPath` to `storage-data/aspire-local` under the repository root and keeps `StorageReconciliation:DryRun=true`; local platform profiles also supply MinIO-compatible `S3Settings` for workflows that exercise S3 behavior, and bootstrap the `explore` bucket before the API starts. With the default `WEBHOOKS_PROVIDER=Local`, AppHost does not register `svix` or `svix-postgres`, does not inject Svix configuration, and does not add a Svix startup dependency. Explicit `Svix` or `Composite` selection adds pinned `svix/svix-server:v1.96.1`, configures Redis for both `SVIX_QUEUE_TYPE` and `SVIX_CACHE_TYPE`, injects the proven `self-hosted`/`1.96.1`/`svix-self-hosted-1.96.1-v1` tuple, and waits for Svix. Its application token and operational callback secret remain sourced only from the intentionally blank `.env`/`.env.example` fields until an operator enables self-hosted Svix. Local Coop uses an isolated PostgreSQL container plus development-only `DATABASE_*`, `SESSION_SECRET`, `OTEL_SERVICE_NAME`, placeholder Scylla client settings, and no-op warehouse/analytics settings so the review-queue provider can boot without external ClickHouse or production secrets.

Cerbos local infrastructure uses the repository `cerbos/` folder as its source of truth. Aspire and Docker Compose mount `cerbos/config/.cerbos.yaml` into the Cerbos container, mount `cerbos/policies/` read-only for derived roles, policies, and `_schemas`, and initialize the local Cerbos PostgreSQL store from `cerbos/init/cerbos-schema.sql`. The local Cerbos PostgreSQL container uses the Postgres 18 parent data mount (`/var/lib/postgresql`) rather than the legacy direct `data` mount. Do not copy policy files into container images for local development; update the repo-owned `cerbos/` tree and restart or sync the local Cerbos service.

Osprey starts in `local-full` from `ghcr.io/roostorg/osprey/osprey-coordinator:latest` and exposes coordinator ports `19950`/`19951`. Aspire also starts `osprey-kafka`, creates the `osprey.actions_input` topic, and points the coordinator at `osprey-kafka:29092`, because the coordinator's action consumer defaults to Kafka. The API's `Reporting:Osprey` HTTP adapter remains disabled until a compatible HTTP facade endpoint is configured.

Webhook operations:

- `Local` works without Svix and is the default self-hosted outgoing provider.
- Local delivery claims durable PostgreSQL work directly and introduces no webhook-specific Redis, Kafka, CDC, or reverse-proxy dependency. Other platform features may still use resources shown in the selected Aspire profile.
- `Svix` and `Composite` require `webhooks.svix.auth_token` to resolve server-side. In local-full this is seeded from `WEBHOOKS_SVIX_AUTH_TOKEN`.
- `webhook-local-delivery` readiness reports LocalProvider queue backlog and stale sending leases.
- `webhook-svix-provider` readiness rejects unknown or zero-evidence provider tuples before resolving secrets and reports only the safe deployment kind, versioned conformance evidence/count, exact-lookup availability, provider selection, and secret-resolution booleans. It never exposes tokens, secret refs, or provider URLs.
- Self-hosted v1.96.1 does not return the request-hash message tag through list/get, so exact automated reconciliation is disabled for that profile; response-loss ambiguity routes to manual reconciliation.
- Incoming Coop, Osprey, and Svix operational callbacks do not depend on the outgoing provider mode.

See [WEBHOOKS.md](WEBHOOKS.md) and [INTEGRATIONS.md](INTEGRATIONS.md) for provider switching, signatures, and callback rules.

## API Startup Behavior

On startup (except `Testing` environment), API performs:

1. `db.Database.Migrate()`
2. `DatabaseSeeder.SeedAsync(...)`

If migration fails, startup fails (application does not continue).

Development catalog reseeding is provider-aware. Relational providers use
set-based cleanup such as `ExecuteDeleteAsync` and bounded SQL where needed;
non-relational test providers materialize and remove tracked rows because EF
Core's in-memory provider cannot translate relational set-based delete
operations. Do not copy the in-memory fallback into production cleanup jobs.

When creating EF Core migrations from scratch in the repository, run the commands from the repo root in this order:

```bash
dotnet ef migrations add init --context DataProtectionKeyContext --project Explore.Persistence --startup-project Explore.API --output-dir Migrations/DataProtection
dotnet ef migrations add init --context ExploreDbContext --project Explore.Persistence --startup-project Explore.API
```

This preserves the dedicated data-protection migration path before the primary `ExploreDbContext` bootstrap migration.

Data Protection key persistence is launch-critical for the Blazor BFF. `Explore.Blazor`
stores authentication cookies, setup-secret cookies, antiforgery state, and other
protected payloads with ASP.NET Core Data Protection. The BFF configures a stable
application name and persists the key ring through `DataProtectionKeyContext`, while
`Event.MigrationService` migrates that dedicated context before the app depends on it.
If the database and `DataProtectionKeys` rows are preserved, a fresh BFF host can read
cookie tickets protected by the previous host. If those rows are lost, existing BFF
auth/setup/antiforgery cookies are intentionally invalid and users must authenticate or
repeat setup actions again. Treat unexpected mass cookie invalidation after restart as a
database/key-ring persistence incident before debugging Keycloak or browser storage.
The Blazor readiness check named `data-protection-keys` queries the same
`DataProtectionKeyContext` key table. If the table or backing database is
unreachable, Blazor `/health` returns unhealthy and logs only the bounded
failure type; health payloads never expose key XML, connection strings, or
database endpoints.

Event/session lifecycle migration notes:

- `20260623101543_AddEventSessionStatusAndNullableSchedule` adds the `event_session_statuses` lookup, backfills existing `event_sessions` to `DRAFT`, and makes session schedule/local projection columns nullable so draft sessions do not need fake times.
- The room-overlap GiST exclusion constraint intentionally ignores rows where `start_time` or `end_time` is null. Do not "fix" that predicate back to all rows; unscheduled drafts must not create unbounded overlap ranges.
- Rollback is development-only and not data-preserving for unscheduled draft rows. Before downgrading past this migration, either delete/resolve unscheduled draft sessions or schedule them through the normal command path so non-null schedule columns can be restored safely.
- Public outputs must continue to use the published/scheduled session gate. Operator backfills or imports may create structurally valid draft rows, but publication and session publish readiness remain Application-layer checks.

## Health and Metrics Endpoints

Via `Explore.ServiceDefaults` + app-specific checks:

- `/health`: readiness (`ready`-tag checks only)
- `/alive`: liveness (`live`-tag checks only)
- `/metrics`: Prometheus scraping endpoint

Status-code contract:

- `Healthy` / `Degraded` -> `200`
- `Unhealthy` -> `503`

Readiness interpretation:

| Check | Host | Healthy | Degraded | Unhealthy |
|---|---|---|---|---|
| `shutdown` | API, Blazor, Control Plane BFF | Process is accepting traffic | Not used | Graceful shutdown is active; remove from load balancer |
| `database` | API, Blazor | EF Core can reach PostgreSQL | Not used | Database unavailable or migration/runtime connectivity failed |
| `data-protection-keys` | Blazor | Persisted ASP.NET Core Data Protection key table is reachable | Not used | BFF key-ring table or backing database is unavailable; existing cookies may fail after restart |
| `distributed-cache` | API, Blazor, Control Plane BFF | Effective cache round-trip works | Configured Redis fell back to in-memory cache | Effective cache round-trip failed |
| `oidc-discovery` | API, Blazor, Control Plane BFF | OIDC metadata valid, or OIDC is not configured | Not used | Configured OIDC metadata endpoint is unreachable or invalid |
| `smtp` | API | SMTP connection/auth succeeds | SMTP is not configured | Configured SMTP is unreachable or authentication fails |
| `email-dispatch` | API | Selected Basic Dispatch trigger is enabled (`TickerQ` scheduler or hosted-service fallback) and outbox counts are below warning thresholds | Dispatch is intentionally disabled, due dispatch backlog crosses threshold, stale `Processing` rows cross threshold, or `DeadLettered` rows cross threshold | TickerQ mode selected while scheduler is disabled; invalid dispatch/scheduler options fail startup; RabbitMQ is not checked in Basic mode |
| `email-dispatch-retention-cleanup` | API | Retention cleanup is enabled in redaction or dry-run mode | Cleanup is intentionally disabled | Invalid retention options fail startup |
| `email-dispatch-rabbitmq` | API | RabbitMQ Dispatch Mode is disabled, or enabled and topology can be declared | Not used | RabbitMQ mode is enabled but the broker/topology is unreachable or invalid |
| `web-push-dispatch` | API | Web Push is disabled, or enabled with bounded backlog/retry/lease/failure counts | Due, stale-processing, or terminal-failure counts crossed configured warning thresholds | Invalid VAPID/worker settings fail startup |
| `idempotency-cleanup` | API | Expired idempotency cleanup is enabled in delete or dry-run mode | Cleanup is intentionally disabled | Invalid cleanup options fail startup |
| `ai-retention-cleanup` | API | AI retention cleanup is enabled in redaction or dry-run mode | Cleanup is intentionally disabled | Invalid cleanup options fail startup |
| `storage` | API | Selected storage provider is available. Local mode verifies the API-owned data root is writable; S3-compatible mode verifies bucket reachability only when selected. | Not used | Selected provider cannot be resolved, selected local root is not writable, or selected S3-compatible storage is missing/unreachable |
| `storage-reconciliation` | API | Storage reconciliation worker is enabled in dry-run or mutation mode | Reconciliation is intentionally disabled | Invalid reconciliation options fail startup |
| `ai-provider` | API | AI provider integration is disabled, deterministic fake provider is enabled, or OpenAI Responses/OpenAI-compatible/Anthropic/Anthropic-compatible/Azure OpenAI settings are valid | Not used | AI provider is enabled but no runnable provider is configured, or provider endpoint/settings fail egress validation |
| `cerbos` | API | Local provider mode is selected, or configured Cerbos PDP passes gRPC health | Not used | Instance `authorization.provider` is `cerbos` and the PDP is missing or unreachable |
| `islamu-event-api` | Blazor, Control Plane BFF | BFF can reach API readiness endpoint | Not used | API readiness endpoint is unavailable or unhealthy |
| `secret_provider` | API, Blazor, Control Plane BFF | Secret backend path is healthy | Secret backend has transient failures within the configured threshold | Secret backend crossed the unhealthy threshold |

Operational rules:

- Point load balancer readiness checks at `/health` and liveness checks at `/alive`.
- Treat `Degraded` as deployable only when the affected dependency is optional for the deployment mode and the response body clearly identifies the dependency.
- Treat `Unhealthy` as non-deployable for rolling updates; fix the dependency or intentionally switch the related feature/provider off.
- Treat `data-protection-keys` unhealthy as a BFF session-continuity blocker. Preserve or restore the `data_protection_keys` table before investigating Keycloak, browser storage, or cookie middleware.
- SMTP readiness is launch-critical when email is enabled. A 2026-07-04 FullLocal proof stopped Mailpit through `aspire resource mailpit stop` and API `/health` correctly returned HTTP 503 with `smtp` Unhealthy, then returned HTTP 200 Healthy after Mailpit restart. The SMTP readiness registration is bounded to five seconds; the follow-up proof returned HTTP 503 in `5.014s` with `smtp` Unhealthy and recovered to HTTP 200 after Mailpit restart.
- Instance Cerbos readiness follows authorization fail-closed semantics: if the operator selected `authorization.provider=cerbos`, an unreachable PDP makes `/health` unhealthy rather than silently falling back to local RBAC.
- Local authorization mode skips Cerbos readiness, so self-hosted/local deployments do not need a Cerbos PDP unless explicitly selected.
- Basic Email Dispatch Mode skips RabbitMQ readiness entirely. A self-hosted deployment can send registration confirmation email with API + PostgreSQL + configured SMTP only. The default trigger is TickerQ `email-dispatch-drain`; the hosted service mode is a fallback over the same drain service. The `email-dispatch` readiness payload also reports safe aggregate outbox counts for due dispatch backlog, retry-scheduled rows, stale processing leases, and dead-letter rows.
- Web Push readiness is healthy while `WebPush:Enabled=false`. When enabled, `web-push-dispatch` exposes only bounded aggregate dispatch counts and thresholds; it never exposes subscription endpoints, browser keys, VAPID material, tenant IDs, payloads, or provider bodies. Push-service `404`/`410` outcomes deactivate stale subscriptions transactionally, while retryable `429`/`5xx` outcomes remain bounded by the dispatch TTL and maximum attempts.
- The control-plane operations endpoint includes a `moderation-reporting` status card for managed reporting routing. It reports aggregate-only provider sync metrics (`pending-sync`, `stuck-pending-sync`, `failed-sync`, `disabled-sync`, `ignored-sync`) and active-tenant lock impact metrics (`reporting-locked-tenants`, `reporting-unlocked-tenants`, `osprey-locked-tenants`, `coop-locked-tenants`). `Reporting:Health:StuckProviderSyncMinutes` defaults to `120`; `Reporting:Health:FailedProviderSyncWarningThreshold` defaults to `1`. These metrics are safe for operators and must not include tenant identifiers, report identifiers, provider URLs, API keys, webhook secrets, correlation IDs, provider payloads, or raw provider errors.
- RabbitMQ Dispatch Mode is optional transport infrastructure. When `EmailDispatchRabbitMq:Enabled=false`, the `email-dispatch-rabbitmq` check is healthy without opening a broker connection. When enabled, missing broker connectivity or failed topology declaration is unhealthy because the operator explicitly selected RabbitMQ transport.
- Idempotency cleanup is an optional operational worker over the PostgreSQL replay cache. `Degraded` means cleanup is intentionally disabled; stale rows remain ignored for replay but are not physically deleted until cleanup is re-enabled.
- AI retention cleanup is an optional operational worker over tenant-owned AI assistant history. `Degraded` means cleanup is intentionally disabled; expired conversations remain readable until cleanup is re-enabled. Dry-run is healthy and records counts without redaction.
- Storage readiness follows the selected instance storage policy. Local-first deployments do not need S3 for `/health`; S3 configuration is probed only when `s3_compatible` is the selected provider. The readiness payload exposes bounded provider/status/failure-code fields and does not include filesystem paths, endpoints, bucket names, access keys, object keys, or secrets.
- Storage reconciliation is dry-run-first. `StorageReconciliation:DryRun=true` reports drift without metadata or provider mutations. Destructive cleanup requires `DryRun=false` plus a specific mutation flag such as `DeleteQuarantinedObjects=true`; health and logs expose bounded settings/counts only.
- Heavy event moderation image deletion is a post-commit provider operation. Redaction commits first, affected image metadata stays unavailable with `delete_requested`, and provider failures return a pending retry result instead of full moderation success. Retry by repeating the heavy-redaction command after fixing provider readiness, or let reconciliation handle eligible delete-requested rows when destructive reconciliation is intentionally enabled. Logs and metrics for this path must not include object keys, filenames, filesystem paths, S3 endpoints, bucket names, credentials, raw provider response bodies, or raw exception text.
- AI provider readiness is intentionally configuration-first. `AiProvider:Enabled=false` is healthy-disabled. If enabled, unsupported providers, missing required provider endpoint/key/model values, local/private endpoints without explicit opt-in, Azure OpenAI non-HTTPS endpoints, embedded endpoint credentials, query strings, or fragments make readiness unhealthy before chat/send is broadly enabled. The readiness payload exposes only bounded booleans and provider/status labels, not endpoint URLs, API keys, model IDs, prompts, responses, provider request IDs, or raw provider errors.

## Deployment Protection and Evidence

GitHub Actions deploys use the `staging` and `production` environments. Configure environment rules in GitHub repository settings, not in application runtime configuration. Code scanning is owned by the `CodeQL Advanced` workflow; keep GitHub CodeQL default setup disabled so advanced SARIF uploads are accepted:

- `production` should require reviewer approval and restrict deployments to `main` and version tags.
- `staging` should use environment-scoped secrets and can deploy automatically from `develop` unless the release process requires review.
- Store Coolify webhook URLs and bearer tokens as environment secrets. Do not print webhook URLs or tokens in workflow logs.

See [CI_CD_GOVERNANCE.md](CI_CD_GOVERNANCE.md) for the required/advisory gate matrix, branch-protection settings, and artifact retention policy. Test lanes that require Docker-backed providers, including Mailpit SMTP evidence, stay explicit runtime evidence rather than hidden prerequisites of the fast build/test gate.

Deploy jobs call the existing Coolify webhook contract through the local `.ci/actions/deploy-coolify` composite action with timeout, retry, redacted error output, transport-failure summaries, and explicit HTTP status validation. Before calling the action, the deploy workflow downloads retained `container-build-*` evidence and resolves the component's expected immutable image tag and digest with `.ci/scripts/resolve-deploy-image-evidence.cs`. The action writes deployment summaries and uploads deployment evidence for 90 days through the caller workflow. Production deploys require configured `PRODUCTION_API_URL` and `PRODUCTION_UI_URL` values for components being deployed; missing production smoke URLs block the Coolify webhook call. When a smoke URL is configured, the action requires both `/alive` and `/health` to return `200` with a bounded retry budget before reporting success. Staging smoke URLs remain optional, but configured staging URLs use the same checks. The reusable container build verifies the pushed GHCR digest's artifact attestation before deploy jobs can invoke Coolify.

Set the GitHub Environment or Repository variable `DEPLOYMENT_FREEZE=true` to block Coolify webhook calls during a deployment freeze. Manual `workflow_dispatch` runs can provide `override_reason` for urgent security releases; the local deploy action records the override reason in deployment evidence before it calls Coolify. Push-triggered deploys do not have an override reason and are blocked while the freeze variable is active.

### Deployment Image Source of Truth

Container builds publish mutable convenience tags (`latest` for production and `develop` for staging), full-commit immutable tags (`sha-${GITHUB_SHA}` for production and `dev-${GITHUB_SHA}` for staging), digest evidence, immutable tag promotion evidence, SBOM/provenance attestations, image scan artifacts, and attestation verification evidence for the pushed GHCR digest. GitHub artifact attestations are the selected SLSA-compatible provenance evidence path: the reusable build verifies the SLSA provenance predicate for the pushed GHCR digest with `gh attestation verify` before dependent deploy jobs can start. ATCR currently uses the scoped `ATCR_PASSWORD` environment secret because public ATCR docs document ATProto OAuth/DPoP, the Docker credential helper/device flow, short-lived registry JWTs behind that helper, and `docker login` with an ATProto app password, but not a GitHub Actions OIDC federation path for CI pushes. Rotate `ATCR_PASSWORD` at least every 90 days and replace it with documented GitHub OIDC or another non-interactive short-lived credential path when ATCR supports that model. Deployable Dockerfiles pin .NET runtime and SDK base images with tag-plus-digest references; Dependabot Docker update PRs are the expected path for refreshing those base digests.

Production must not rely on `latest` as the source of truth once deployment promotion is complete.

Decision path:

1. Preferred: configure Coolify Docker Image resources to deploy explicit image digests. Public Coolify v4.x source/UI evidence supports Docker Image hash input and normalizes SHA-256 references to `image@sha256:<digest>`; ISLAMU still needs live Coolify application configuration/deployment evidence proving the resource consumed the expected digest.
2. Fallback: configure Coolify to deploy immutable full-commit tags (`sha-${GITHUB_SHA}` for production and `dev-${GITHUB_SHA}` for staging) until live digest consumption is configured and proven. The reusable container build records those primary-registry tag references and verifies that each resolves to the built digest before deployment jobs can start; the deploy workflow resolves the retained promotion artifact again and records the expected immutable image tag plus expected image digest in deployment evidence.
3. Temporary risk: mutable tags remain convenience aliases and must not be used as the release source of truth while live Coolify digest/tag consumption proof is being collected.

Do not remove digest/SBOM/provenance evidence even if Coolify temporarily consumes immutable tags rather than digests.

## Graceful Shutdown Contract

API and Blazor include shutdown-aware checks for rolling deployments.

API specifics:

- grace window: 25 seconds
- during shutdown, health checks become unhealthy so load balancers stop routing traffic

## Request Protection Defaults

API runtime protections include:

### Rate Limiting (`RateLimiting` config section)

| Policy | Mechanism | Partition Key | Defaults |
|---|---|---|---|
| `global` | Token bucket | Successfully authenticated API key ID when present, otherwise remote IP | 200 tokens, replenish 40/10s. Localhost exempt for anonymous/IP traffic |
| `authenticated` | Sliding window | API key ID when present, otherwise `User.Identity.Name` | 200 requests/60s, 4 segments |
| `write` | Fixed window | API key ID when present, otherwise `User.Identity.Name` | 30 requests/60s |
| `PublicIngestion` | Fixed window | IP | 60 requests/60s |
| `setup_secret` | Fixed window | IP | 5 requests/60s |
| `AnalyticsRelay` | Fixed window | IP | 120 requests/60s |
| `AiAssistant` | Fixed window | API key ID when present, otherwise authenticated user ID | 12 sends/60s |

**Rejection**: `429 Too Many Requests` with RFC 6585 ProblemDetails, `Retry-After` when available, plus `X-RateLimit-Limit` and `X-RateLimit-Remaining` headers. Successfully authenticated API keys are throttled per key ID; no-key, malformed, invalid, revoked, or inactive API-key traffic remains in the anonymous/IP partition. External API-key authentication metrics use bounded `outcome`, `tenant_id`, and `owner_type` tags only, never raw keys, secrets, or request paths.

**Testing override**: All rate limiters are replaced with `NoLimiter` in `Testing` environment unless a specific integration factory opts back into the global limiter to assert `429`, `Retry-After`, and per-key/IP partition behavior.

**Config keys** (all under `RateLimiting` section):
- `Global:TokenLimit`, `Global:ReplenishmentPeriodSeconds`, `Global:TokensPerPeriod`
- `Authenticated:PermitLimit`, `Authenticated:WindowSeconds`, `Authenticated:SegmentsPerWindow`
- `Write:PermitLimit`, `Write:WindowSeconds`
- `PublicIngestion:PermitLimit`, `PublicIngestion:WindowSeconds`
- `SetupSecret:PermitLimit`, `SetupSecret:WindowSeconds`
- `AnalyticsRelay:PermitLimit`, `AnalyticsRelay:WindowSeconds`
- `AiAssistant:PermitLimit`, `AiAssistant:WindowSeconds`

AI assistant send requests have layered abuse controls:
- API rate limiting uses the `AiAssistant` policy before the request reaches MediatR.
- Application handlers enforce `ai_assistant.daily_message_limit`, `ai_assistant.daily_tenant_message_limit`, and `ai_assistant.concurrent_run_limit` before provider calls.
- Idempotency replay is evaluated before quota checks so successful retries do not consume additional provider calls.
- AI run cancellation is persisted through the authenticated cancel-run API for queued/in-progress runs only. Run-status HAL exposes `cancel-run` only while a run is cancellable; completed runs return safe conflict ProblemDetails. The send-message pipeline already passes the request `CancellationToken` into provider calls, but cross-request provider abort orchestration is not a scheduler/registry feature yet.
- AI run progress uses authenticated polling, not streaming. `SendAiMessage` returns the run-status route and clients poll `GET /api/ai/assistant/conversations/{conversationId}/runs/{runId}` until the run reaches a terminal state. `ai_assistant.streaming_enabled` remains reserved and disabled until a future slice hardens streaming transport, proxy buffering, request cancellation, timeout behavior, auth, logging, and non-streaming fallback.
- 429 and quota ProblemDetails must not include prompts, model responses, selected reference content, provider request IDs, endpoint URLs, API keys, or raw provider errors.

MCP adapter operations are opt-out at startup:
- The MCP adapter endpoint is mapped by default through the startup ceiling `Mcp:Enabled=true` at `/mcp`. Set `Mcp:Enabled=false` only when the endpoint must be unmapped at startup.
- Runtime governance then resolves `mcp.enabled` through the instance/tenant settings cascade. Effective exposure is `Mcp:Enabled && resolved(mcp.enabled)`, so instance administrators can turn the adapter off without changing endpoint path/stateless startup posture.
- The adapter exposes a bounded readiness check named `mcp-adapter`, read-only registry discovery, anonymous-safe public event reads (`search_public_events`, `get_public_event`, `get_public_event_program_summary`, `list_public_event_sessions`), authenticated event-management reads (`list_my_events`, `get_event_creation_context`, `get_event_publish_readiness`, program/custom-property/registration/team/template/sync contexts), the authenticated `event_management_context` resource template, first-class registry-projected `propose_*` tools, safe AI conversation resources, and event-management confirmation prompts. `list_my_events` derives the principal from the authenticated request and delegates to `GetMyEventsRequest`; it does not accept a caller-supplied user id. `get_event_creation_context` delegates to `GetEventCreationContextRequest` and returns bounded tenant policy flags plus publisher options without MCP-side role/claim inference or internal role IDs. `event_management_context` delegates detail visibility to `GetEventDetailsRequest`, materializes REST HAL through the event resource assembler, and derives edit/delete/publish/publish-readiness/add-session/session-create-context availability from `_links`. `get_event_publish_readiness` also materializes REST HAL first and calls `GetEventPublishReadinessRequest` only when `_links` contains `publish-readiness`; otherwise it returns a bounded `not_found` or `not_available` descriptor. Phase 5 read tools use the same authenticated event-management policy, MediatR queries, bounded descriptors, and event HAL/domain-authority gates before returning program/session, custom-property, registration, team, template, or sync context data. Mutating MCP tools persist proposed actions through MediatR and require the normal product/API confirmation path before side effects occur.
- The selected product transport is API-hosted stateless Streamable HTTP. Keep `Mcp:Stateless=true`; no MCP session affinity should be required for API replicas.
- Do not add `WithStdioServerTransport()` to the product API host. `stdio` remains a local/developer diagnostic transport and needs a separate host/runbook decision before use.
- `Mcp:EnableLegacySse=true` is the default startup ceiling for future governance only; `mcp.enable_legacy_sse` records runtime intent, but current runtime legacy SSE remains unavailable because the official SDK legacy mode requires stateful in-memory sessions and weaker request backpressure than Streamable HTTP.
- Instance administrators can lock tenant MCP overrides with `governance.lock_tenant_mcp` and `governance.lock_tenant_mcp_legacy_sse`. Multi-tenant tenant administrators can override only unlocked values; single-tenant deployments use the existing single-tenant bypass semantics.
- Keep MCP SDK registration explicit. The API host uses explicit `WithTools<T>()`, `WithResources<T>()`, `WithPrompts<T>()`, and registry-projected tool options instead of assembly scanning. This keeps startup behavior reviewable and avoids avoidable Native AOT/reflection risk; Native AOT publication is not supported until a dedicated publish profile and verification gate exist.
- MCP is API-key-first for external clients. The endpoint is mapped anonymously so SDK authorization filters can expose only explicitly anonymous-safe registry discovery and public event reads without credentials; scoped tools/resources/prompts carry scope-aware policies and require a valid bearer session or non-empty `X-API-Key` principal. API keys need `mcp:read` for generic MCP read resources, `mcp:read` plus event read-equivalent scope authority for protected event-management reads such as `list_my_events`, `get_event_creation_context`, `get_event_publish_readiness`, and `event_management_context`, and `mcp:propose` for proposal tools/prompts; no key, a blank `X-API-Key` header, invalid keys, revoked keys, or valid keys without the required MCP/domain scope combination can see only anonymous-safe capabilities. Requests that send both `Authorization` and a non-empty `X-API-Key` return a redacted bad-request response. When rate limiting is enabled, valid MCP API keys are partitioned by key ID while anonymous/blank/invalid/revoked MCP requests are partitioned by remote IP and still return normal `429` responses without echoing credentials.
- MCP tools must be registry-backed and mutating tools must use the proposal/confirmation path; MCP must not mutate repositories directly.
- Production MCP exposure must use the same trusted HTTPS boundary as the API. Local `curl -k` is acceptable only for developer certificate troubleshooting and must not appear in production runbooks, support evidence, or client configuration.
- MCP health, logs, metrics, traces, and errors must not include prompts, selected reference content, tool payloads, provider responses, provider endpoint URLs, API keys, tenant IDs, user IDs, or raw provider exceptions. Health reports only safe effective-state booleans such as `startupEnabled`, `runtimeEnabled`, and legacy-SSE requested/enabled state. Bounded MCP telemetry uses `Explore.Mcp` as both ActivitySource and Meter name with allow-listed tool/outcome/failure-code tags only.

MCP recovery and operator actions:
- If `mcp-adapter` is degraded because MCP is disabled, confirm that startup `Mcp:Enabled=false` or runtime `mcp.enabled=false` was intentional.
- If MCP was enabled at startup but must be rolled back immediately, set runtime `mcp.enabled=false` from instance governance. Requests to the mapped MCP path return `404` while the rest of the API remains available.
- If MCP must be unmapped entirely, set `Mcp:Enabled=false` and restart the API. Inspect only bounded startup/configuration errors. Do not capture prompts, payloads, provider responses, API keys, endpoint URLs, tenant IDs, or raw MCP request bodies in support tickets.
- If an MCP API key or captured client configuration is suspected leaked, revoke the key first, verify invalid/revoked-key traffic only sees anonymous-safe discovery and remains IP-partitioned, then create a new least-privilege key for the specific operator smoke scenario.
- If external agents report mutation failures, inspect the returned failure code from the projected `propose_*` tool or generic `propose_ai_tool_action`, then inspect the normal AI conversation/proposed-action API state. Do not bypass the confirmation flow or write repositories directly.
- If a client requires legacy SSE, treat that as a new architecture decision. Startup and runtime governance can record intent, but the current adapter intentionally supports stateless Streamable HTTP only and reports `legacySseRuntimeEnabled=false`.
- If a deployment requests Native AOT for the API host, treat MCP as unverified until a dedicated `dotnet publish` profile proves the SDK, explicit static registrations, and registry-projected dynamic tools all survive trimming/AOT without losing schema metadata.

MCP local debugging, Inspector, and redacted contract smoke:
- The full local debug and client-smoke runbook is [MCP_DEBUGGING.md](MCP_DEBUGGING.md). It includes Debug-build startup, redacted `.vscode/mcp.json`/`.mcp.json` templates, Inspector, GitHub Copilot Agent Mode, JSON-RPC fallback, and compatibility gates.
- First run the deterministic replay report. This is the CI-safe contract check and uses no live provider credentials:
  `dotnet run --project src/Explore.Diagnostic/Explore.Diagnostic.csproj --configuration Release --no-restore -- ai-replay-report --output /tmp/explore-ai-replay-mcp-inspector`
- Use MCP Inspector only for manual local/staging smoke against fake or disposable data. Current MCP docs start Inspector with `npx -y @modelcontextprotocol/inspector`; connect it to the API's Streamable HTTP URL, for example `https://<redacted-host>/mcp`.
- Configure `X-API-Key: <redacted-api-key>` for normal scoped machine smoke, leave credentials blank for anonymous-safe discovery, or use `Authorization: Bearer <redacted-token>` only for user-delegated local smoke. For multi-tenant routing, use the same trusted tenant binding as normal API traffic, such as host/subdomain routing or `X-Tenant-Slug: <redacted-tenant-slug>`. Do not mix bearer and API-key credentials in one request.
- Discovery checklist: initialize the connection, then list tools, resources, resource templates, and prompts. Anonymous or invalid-key tool surface is `list_ai_tool_contracts`, `search_public_events`, `get_public_event`, `get_public_event_program_summary`, and `list_public_event_sessions` only; a valid key with `mcp:read` plus event read-equivalent scope can also expose protected event-management reads such as `list_my_events`, `get_event_creation_context`, `get_event_publish_readiness`, `event_management_context`, and the Phase 5 program/custom-property/registration/team/template/sync context tools. Generic MCP read resources such as `ai_conversations` still require MCP read scope, while proposal tools/prompts require `mcp:propose`.
- Safe call checklist: call `list_ai_tool_contracts`; optionally call `search_public_events`, `get_public_event`, `get_public_event_program_summary`, or `list_public_event_sessions` for published public event data only; optionally call protected reads such as `get_event_publish_readiness` only for disposable events where REST HAL currently exposes `publish-readiness`; optionally call one representative Phase 5 context tool only for disposable event data; optionally call `propose_create_event_draft` or `propose_update_event_draft` only against a disposable test conversation with fixture data. Stop after a proposed action is returned. Do not call confirm/reject API endpoints from Inspector, do not write repositories, and do not assert that an event was created.
- Redaction checklist: retain only scenario codes, pass/fail status, redacted endpoint path, redacted auth mode, and bounded failure categories. Do not retain Inspector screenshots, browser storage, exports, proxy logs, prompts, selected-reference content, raw tool payloads, provider responses, tenant/user identifiers, bearer/API-key values, model IDs, raw MCP request/response bodies, or raw exceptions.
- If a command-line HTTP smoke is needed instead of Inspector, use the same redaction rules and send JSON-RPC methods such as `tools/list`, `resources/list`, `resources/templates/list`, and `prompts/list` with `ProtocolVersion: 2025-06-18`, `Accept: application/json, text/event-stream`, and `Content-Type: application/json`. Do not paste real credentials or response bodies into tickets.
- Automated MCP protocol coverage lives in `McpProtocolContractTests`, `EventManagementMcpPublicReadTests`, and `EventManagementMcpAuthenticatedReadTests`: it exercises `initialize`, discovery lists, registry discovery calls, public event list/detail/program/session parity, authenticated my-events/creation-context/publish-readiness reads, HAL-derived event-management context parity, generic/projected proposal calls, disabled endpoint behavior, and redaction failure paths through `WebApplicationFactory` without Inspector, Copilot, live providers, product confirmation calls, or database side effects beyond disposable proposed-action fixtures.
- Review-first MCP debug readiness is covered by `McpDebugReadinessDoctorCheck`. The doctor only checks docs/tests/ignore-rule presence and redacted content markers; it does not start servers, call live endpoints, generate tokens, persist config, run migrations, or print secrets.

MCP protocol/client compatibility reviews:
- Run this review before upgrading `ModelContextProtocol.AspNetCore`, changing MCP headers/protocol versions, or supporting a client that requests behavior outside the current stateless Streamable HTTP surface.
- Current allowed capability posture is intentionally small: anonymous-safe registry discovery, anonymous-safe public event list/detail/program/session reads backed by MediatR queries plus a `Published` + `Public` MCP gate, authenticated event-management reads/resources backed by MediatR and REST HAL affordances including HAL-gated publish readiness, registry-projected proposal tools, and no server-to-client requests. Stateless mode means no `Mcp-Session-Id`, no API session affinity, no legacy SSE runtime transport, no resource subscriptions, and no sampling/elicitation/roots/server-initiated notifications.
- Treat these as ADR-gated changes: stateful sessions, session migration/resumability, GET/DELETE MCP endpoints, legacy SSE, sampling, elicitation, roots, completions, progress notifications, tool/resource/prompt list-changed notifications, dynamic non-registry tool changes, client-specific compatibility shims, or any use of SDK annotations as authorization authority.
- Review checklist: read the SDK/protocol release notes, compare generated MCP tool/resource/prompt surface, rerun focused MCP API tests including `McpProtocolContractTests`, rerun `ai-replay-report`, repeat the redacted Inspector checklist when endpoint behavior changes, and verify docs/configuration still say default `/mcp`, stateless, API-key-first, and proposal-first.
- Compatibility evidence matrix: Inspector and VS Code/Copilot smoke are manual and redacted; WebApplicationFactory JSON-RPC tests are CI-safe; official C# SDK client transport remains a future upgrade target when it fits the in-memory test host; curl is a local fallback only.
- Evidence may include package version, protocol version, scenario codes, pass/fail status, and bounded failure categories only. Do not preserve raw MCP request/response bodies, prompts, payload JSON, provider data, tenant/user identifiers, endpoint URLs, bearer/API-key values, model IDs, or raw exceptions.
- Rollback posture: if a client requires unsupported stateful/SSE/server-to-client behavior, keep `Mcp:Enabled=false` for that deployment or keep the current minimal surface unchanged until the ADR, implementation, tests, self-hosting docs, and redaction runbook are complete.

Advisory AI evaluation reports:
- Generate deterministic ATCR evaluation evidence with:
  `dotnet run --project src/Explore.Diagnostic/Explore.Diagnostic.csproj --configuration Release -- ai-eval-report --output artifacts/ai-evaluation`
- The report is intentionally advisory and non-gating. It covers tool proposal correctness, refusal/safety behavior, prompt-injection resistance, selected-reference groundedness metadata, MCP proposal-flow posture, and event-draft regression using local fake/deterministic checks, so normal CI and operator smoke tests do not require live AI provider credentials or model calls.
- JSON and Markdown artifacts must stay redacted. They may include scenario codes, dimensions, status, summaries, and recommendations, but not prompts, provider responses, selected-reference content beyond deterministic fixture labels, raw tool payloads, tenant/user identifiers, endpoint URLs, API keys, model secrets, or raw exceptions.
- Treat report drift as trend evidence first. Do not promote model-scored or live-provider evaluation checks to hard CI gates until cost, cache stability, false-positive behavior, and provider volatility are reviewed.

Fake/replay AI usability reports:
- Generate deterministic assistant/MCP proposal-flow evidence with:
  `dotnet run --project src/Explore.Diagnostic/Explore.Diagnostic.csproj --configuration Release -- ai-replay-report --output artifacts/ai-replay`
- The report is suitable for normal CI because it uses local fake/replay checks only. It validates assistant rail catalog + plan-preview readiness, MCP Inspector discovery checklist posture, projected MCP tool selection, MCP proposal-first/confirmation-required behavior, missing-HAL blocking, and safe recovery metadata without live provider credentials, screenshots with user content, or database writes.
- The command exits non-zero if a replay scenario fails, a live-provider credential path is used, content-bearing artifacts are detected, or database side effects are detected.
- JSON and Markdown artifacts may include scenario codes, failure classes, pass rates, redacted diagnostics, and artifact paths. They must not include prompts, provider responses, selected-reference content, raw tool payloads, tenant/user identifiers, endpoint URLs, API keys, model secrets, or raw exception bodies.

### Request Timeouts (`RequestTimeouts` config section)

| Policy | Default | Applied To |
|---|---|---|
| `Default` | 30 seconds | Standard operations |
| `Lookup` | 10 seconds | Lookup/fast queries |
| `Complex` | 60 seconds | Complex queries, exports |

Timeout expiry: `504 Gateway Timeout`.

### Response Compression

- Brotli + Gzip at `CompressionLevel.Fastest`, enabled for HTTPS.
- Additional MIME types: `application/json`, `application/hal+json`.

### ETag / Conditional Requests

- SHA256-based weak ETags on JSON/HAL responses.
- Client sends `If-None-Match` → API returns `304 Not Modified` when content unchanged.
- Saves bandwidth for repeat requests.

### Correlation ID

- `CorrelationIdMiddleware` reads `X-Correlation-ID` or `X-Request-ID` from incoming request.
- Uses `HttpContext.TraceIdentifier` when both request headers are absent.
- Pushes to Serilog `LogContext` as `CorrelationId` property for structured log correlation.

### Forwarded Header Trust

- `Explore.API` uses ASP.NET Core forwarded-header middleware with explicit `ForwardedHeadersTrust` configuration.
- Operators must configure trusted reverse-proxy IPs or CIDR networks before relying on `X-Forwarded-Host` for custom-domain or subdomain tenant resolution.
- Without trusted proxy configuration, forwarded host/IP headers are ignored by the API host.

### Security Headers

Added to every response by `SecurityHeadersMiddleware`:
- `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=()`
- `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`
- Non-GET responses additionally receive `Cache-Control: no-store` and `Pragma: no-cache`.

### Business Metrics (OpenTelemetry)

Meter `Explore.Business` exposes source-defined business counters. Counter names and tags are not uniform across all events; check the metric-specific tags before building dashboards.

AI provider tracing uses the `Explore.Ai.Provider` activity source. Provider spans are platform-owned and redacted; they intentionally do not use SDK GenAI middleware because prompts, responses, tool payloads, provider endpoints, model IDs, provider request IDs, tenant/user IDs, API keys, and raw provider errors must not be exported.

Current counters include:

- `explore.events.created` (`tenant_id`, `event_type`)
- `explore.events.published` (`tenant_id`)
- `explore.registrations.created` (`tenant_id`)
- `explore.organizations.created` (`tenant_id`)
- `explore.authorization.decisions` (`resource`, `action`, `result`)
- `explore.support_access.lifecycle_events` (`event_type`, `mode`, `outcome`, `failure_category`) — support-access start/stop/expire/revoke/force-stop decisions; labels intentionally exclude session IDs, actor/user IDs, tenant IDs, ticket references, reason text, and raw exception text.
- `explore.support_access.request_audits` (`event_type`, `outcome`, `persistence_outcome`, `failure_category`) — support-access per-request audit persistence outcomes; labels intentionally exclude route paths, resource IDs, session IDs, actor/user IDs, ticket references, request payloads, and raw storage errors.
- `explore.support_access.session_validation_denials` (`reason`, `mode`) — forwarded support-session validation denials such as kill-switch or write-mode shutdown; labels intentionally exclude forwarded session IDs, actor IDs, tenant IDs, and request headers.
- `explore.support_access.authorization_boundary_denials` (`reason`, `mode`, `action_class`) — runtime support-access boundary denials such as inactive forwarded sessions, read-only write attempts, missing tenant context, and cross-tenant mismatches; labels intentionally exclude resource IDs, session IDs, actor IDs, and tenant IDs.
- `event_role_assignment.changed` (`operation`, `outcome`, `role`)
- `explore.email_dispatch.attempts` (`tenant_id`, `outcome`, `failure_category`) — Basic Dispatch Mode email outcomes; labels intentionally exclude recipient, subject, body, provider message ID, and raw error text.
- `explore.email_dispatch.rabbitmq.publishes` (`tenant_id`, `outcome`, `failure_category`) — optional RabbitMQ pointer-publish outcomes; labels intentionally exclude recipient, subject, body, provider message ID, raw broker error text, and connection strings.
- `explore.email_dispatch.rabbitmq.consumes` (`tenant_id`, `outcome`, `failure_category`) — manual-ack RabbitMQ delivery outcomes; labels intentionally exclude recipient, subject, body, provider message ID, publish event ID, delivery tag, raw broker error text, and connection strings.
- `explore.notifications.fanout_runs` (`tenant_id`, `fanout_kind`, `outcome`) — notification fanout run outcomes; labels intentionally exclude event IDs, actor IDs, subscriber IDs, notification IDs, event titles, and deduplication keys.
- `explore.notifications.fanout_subscribers` (`tenant_id`, `fanout_kind`, `outcome`) — aggregate subscriber decisions for notification fanout; labels intentionally exclude event IDs, actor IDs, subscriber IDs, notification IDs, event titles, and deduplication keys.
- `explore.event_reports.submissions` (`tenant_id`, `outcome`, `failure_category`) — event-report intake outcomes; labels intentionally exclude reporter text, reporter IP/User-Agent values or hashes, event titles, slugs, URLs, report IDs, and raw validation/provider errors.
- `explore.event_reports.workflow_actions` (`tenant_id`, `action`, `outcome`, `failure_category`) — moderation report triage/assign/decide/execute outcomes; labels intentionally exclude report IDs, case IDs, decision IDs, moderator IDs, reporter evidence, safe notes, and raw errors.
- `explore.event_reports.provider_syncs` (`tenant_id`, `provider`, `outcome`, `failure_category`) — report provider sync outcomes for local/Osprey/Coop/composite paths; labels intentionally exclude provider URLs, credentials, external case/signal IDs, payload bodies, reporter evidence, and raw provider errors.
- `explore.event_reports.provider_callbacks` (`tenant_id`, `provider`, `outcome`, `failure_category`) — provider callback outcomes; labels intentionally exclude callback bodies, signatures, provider decision IDs, provider message IDs, report IDs, event IDs, case IDs, reporter evidence, and raw parse/auth errors. Anonymous public-ingestion callbacks such as Svix operational webhooks use the default tenant tag even when a verified payload contains a tenant identifier.
- `explore.webhooks.messages_created` (`event_type`, `provider`, `outcome`) — canonical outgoing webhook message creation outcomes; labels intentionally exclude tenant/resource IDs, payloads, aggregate titles/slugs/URLs, endpoint URLs, and secrets.
- `explore.webhooks.delivery_attempts` (`event_type`, `outcome`, `failure_category`) — LocalProvider delivery attempt outcomes; labels intentionally exclude tenant/resource IDs, endpoint URLs, request payloads, response bodies, headers, and raw transport errors.
- `explore.webhooks.delivery_success` (`event_type`) — LocalProvider successful delivery count without tenant/resource identity.
- `explore.webhooks.delivery_failure` (`event_type`, `outcome`, `failure_category`) — LocalProvider failed delivery count with bounded failure categories only.
- `explore.webhooks.endpoint_disabled` (`failure_category`) — legacy endpoint auto-pause transition count; labels intentionally exclude tenant/resource IDs, endpoint URLs, and secrets.
- `explore.webhooks.manual_retries` (`event_type`, `outcome`, `failure_category`) — manual retry scheduling outcomes; labels intentionally exclude tenant, message, endpoint, and payload identity.
- `explore.webhooks.provider_publish_failure` (`event_type`, `provider`, `failure_category`) — outgoing provider handoff failures before provider-owned fanout; labels intentionally exclude tenant/resource IDs, provider message IDs, endpoint URLs, payloads, secrets, and raw provider errors.
- `explore.webhooks.claim_lag` (`provider`, `operation`) — claim-lag histogram for Local delivery and self-hosted Svix publication/reconciliation.
- `explore.webhooks.processing_outcomes` (`provider`, `operation`, `outcome`) — durable claim and settlement outcomes from the closed enum-backed telemetry vocabulary.
- `explore.webhooks.retries_scheduled` (`provider`, `operation`) and `explore.webhooks.dead_letters` (`provider`, `operation`) — automatic retry and terminal dead-letter transitions.
- `explore.webhooks.publication_unknown_age` (`provider`) and `explore.webhooks.manual_reconciliations` (`provider`) — uncertain publication age observations and operator-owned reconciliation transitions.
- `explore.webhooks.endpoint_auto_pauses` (`provider`) — counts only the transition into automatic pause, not later failures while already paused.
- `explore.webhooks.provider_health_checks` (`provider`, `outcome`) — independent Local, Svix, and Coop-effect readiness observations.
- `explore.webhooks.retention.cleanup_runs` (`mode`, `outcome`) and `explore.webhooks.retention.cleanup_items` (`mode`, `data_kind`) — cleanup pass and bounded evidence-category counts; unknown input collapses to `unknown`.
- `explore.ai.provider.health_checks` (`provider`, `status`, `reason`) — AI provider readiness outcomes; labels intentionally exclude endpoint URLs, API keys, model IDs, prompts, responses, provider request IDs, tenant/user IDs, and raw errors.
- `explore.ai.provider.requests` (`provider`, `outcome`, `failure_category`) — AI provider call outcomes; labels intentionally exclude tenant/user prompt content, selected reference content, raw tool payloads, model IDs, endpoint URLs, API keys, provider request IDs, and raw provider errors.
- `explore.ai.provider.request_duration` (`provider`, `outcome`, `failure_category`) — AI provider request duration histogram in seconds; labels intentionally use the same bounded dimensions as provider request counters.
- `explore.ai.provider.token_usage` (`provider`, `token_type`) — AI provider token usage histogram for input/output/total tokens; labels intentionally exclude prompts, responses, model IDs, endpoints, provider request IDs, tenant/user IDs, and provider errors.
- `explore.ai.provider.proposed_actions` (`provider`, `action_kind`) — aggregate count of provider-returned proposed actions; labels intentionally include only bounded action kinds such as `create_event_draft`, not raw tool arguments or proposal payloads.
- `explore.ai.retention.cleanup_runs` (`mode`, `outcome`) — scheduled AI retention cleanup pass outcomes in `dry_run` or `redact` mode; labels intentionally exclude tenant IDs, prompts, responses, provider IDs, and tool payloads.
- `explore.ai.retention.cleanup_rows` (`mode`, `category`) — bounded aggregate row counts for eligible/redacted AI retention cleanup categories; labels intentionally exclude tenant IDs and content-bearing identifiers.
- `explore.storage.upload_sessions` (`provider`, `operation`, `outcome`, `failure_category`) — provider-neutral upload session create/finalize/cancel outcomes; labels intentionally exclude tenant IDs, user IDs, upload-session IDs, filenames, object keys, paths, endpoints, bucket names, access keys, secrets, and raw exception text.
- `explore.storage.upload_bytes` (`provider`, `outcome`, `failure_category`) — upload byte histogram for accepted/attempted provider writes; labels are bounded to provider and outcome categories.
- `explore.storage.reads` (`provider`, `outcome`, `failure_category`, `visibility`) — metadata-backed storage read outcomes after lifecycle and visibility checks; labels intentionally exclude storage-object IDs, object keys, paths, filenames, tenant IDs, user IDs, and raw provider errors.
- `explore.storage.read_bytes` (`provider`, `outcome`, `visibility`) — read byte histogram for successful metadata-backed provider reads.
- `explore.storage.deletes` (`provider`, `outcome`, `failure_category`) — provider-neutral blob delete plus metadata delete outcomes; labels intentionally exclude storage-object IDs, object keys, paths, filenames, endpoints, bucket names, and raw provider errors.
- `explore.storage.quota_reservations` (`provider`, `operation`, `outcome`, `failure_category`) — quota reserve/release/commit outcomes around upload sessions.
- `explore.storage.quota_bytes` (`provider`, `operation`, `outcome`) — byte histogram for quota reserve/release/commit operations.
- `explore.storage.reconciliation_runs` (`mode`, `outcome`, `failure_category`) — storage drift scan outcomes; labels intentionally exclude tenant IDs, storage-object IDs, object keys, filenames, paths, endpoints, bucket names, and raw provider errors.
- `explore.storage.reconciliation_objects` (`provider`, `category`, `action`, `outcome`, `failure_category`) — aggregate object decisions from reconciliation scans; labels are bounded to provider/category/action/outcome and intentionally exclude identifiers, paths, object keys, filenames, and secrets.
- `explore.storage.provider_tests` (`provider`, `outcome`, `failure_category`) — admin storage provider test outcomes; labels intentionally exclude local filesystem roots, S3 endpoints, bucket names, access keys, secrets, and raw exception text.

### Incoming Coop Effect Operations

`POST /api/integrations/moderation/coop/callback` acknowledges a valid decision callback only after the retained inbox row and unique effect pointer commit. Execution is asynchronous: the pointer worker uses a fenced renewable lease, loads the retained callback, invokes `ProcessCoopDecisionCallbackCommand`, and completes the pointer with an applied-effect receipt only after command success.

Operator sequence:

1. Check `/health/webhooks/coop-effects`. `Degraded` means processing is disabled, the due backlog reached `EffectBacklogWarningThreshold`, or stale leases reached `EffectStaleLeaseWarningThreshold`; `Unhealthy` means the PostgreSQL readiness query failed. The payload contains aggregate counts and settings only.
2. Query `GET /api/admin/incoming-webhook-effects/status?tenantId={tenantId}&limit=50` with an authorized operator identity. Inspect status, generation/fence, attempts, next-attempt/lease/terminal timestamps, and bounded failure evidence. Callback bytes, hashes, provider decision IDs, headers, and raw exceptions are deliberately unavailable.
3. Fix the underlying permanent condition before redrive. Follow the item HAL `redrive` relation only when present, then POST its `expectedProcessingGeneration` and a bounded operator reason. A stale generation, non-dead-lettered row, expired replay window, or missing retained payload fails closed.
4. Confirm the processing generation advanced, an audit event was appended, and the row later becomes `Completed`. Do not edit pointer status, fences, or receipts directly in PostgreSQL.

Incident controls:

- Set `Webhooks:IncomingProcessing:Enabled=false` and restart API replicas to pause the incoming/effect background loops. Intake remains durable. Re-enable only after checking accumulated backlog and database capacity.
- A cancelled or crashed worker leaves its active lease for fenced expiry recovery. Never manually clear a token or reuse a stale claim; a recovered claim receives a new token and higher fence.
- Retention cleanup cannot clear retained callback bytes while an effect is pending, failed, or processing. Completed/dead-lettered pointers permit payload cleanup only after the inbox payload-retention timestamp and replay window have expired. After cleanup, redrive is intentionally unavailable.
- Alert on `explore.webhooks.processing_outcomes{provider="coop",operation="incoming_effect"}`, `explore.webhooks.retries_scheduled`, `explore.webhooks.dead_letters`, and `explore.webhooks.provider_health_checks`. These labels are closed and PII-free; logs include bounded failure type/category only.
- Back up the retained inbox, effect pointer, applied-effect receipt, and webhook audit tables together. Restoring only part of this relationship can remove replay evidence or cause a settled command to appear pending.

### Support Access Operations

Support access is an operator-governed, actor-preserving session model. Browser clients never own support authority; the BFF forwards only a server-owned `X-Support-Access-Session-Id`, and the API revalidates the persisted session, actor, tenant, expiry, mode, and governance settings on each forwarded request.

Operational controls:

- Kill switch: set `support_access.enabled=false` to deny new starts and reject existing forwarded support-access use. Existing BFF stored session references become inert because API validation fails closed.
- Write shutdown: set `support_access.allow_write_mode=false` to reject new write sessions and reject existing forwarded write sessions.
- Emergency revocation: use the support-access force-stop endpoint/action for active sessions. Revocation writes a durable audit event attributed to the operator who force-stopped the session.
- Retention and backup: support-access session and audit-event tables are security evidence. Do not include them in ephemeral cleanup jobs; include them in the normal database backup/restore plan and define retention through governance before purging historical evidence.

Alert-worthy structured logs:

| Event | Signal |
|---|---|
| Write-capable session started | Warning from `StartSupportAccessSessionCommandHandler`; investigate ticket/reference and expiry. |
| Force-stop completed | Warning from `ForceStopSupportAccessSessionCommandHandler`; this is an emergency revocation path. |
| Kill switch denied forwarded use | Warning from `SupportAccessSessionService` plus `explore.support_access.session_validation_denials{reason="support_access_disabled"}`. |
| Cross-tenant mismatch denied | Warning from `RuntimeAuthorizationProvider` plus `explore.support_access.authorization_boundary_denials{reason="support_access_target_tenant_mismatch"}`. |
| Audit persistence failed | Warning from `SupportAccessAuditMiddleware` plus `explore.support_access.request_audits{persistence_outcome="failed"}`. |

Trace tags on active support-access requests use bounded support context such as `support_access.active`, `support_access.mode`, `support_access.allows_writes`, and `support_access.was_forwarded`. They intentionally do not carry ticket text or reason text.

### Notification Fanout Operations

Event-published actor-subscription fanout is handled as an internal outbox side effect:

1. The event publish command writes an internal `EventPublishedNotificationFanoutRequested` outbox row in the same transaction as the event status change.
2. `OutboxProcessor` claims pending rows and calls `CompositeOutboxMessageDispatcher`.
3. The composite dispatcher routes `EventPublishedNotificationFanoutRequested` to `EventPublishedNotificationFanoutService`; retired external `EventPublished` broker rows fail closed as unknown outbox event types.
4. The fanout service creates or resumes `NotificationFanoutRun`, scans active organization/group actor subscriptions for active tenant-local users, skips existing `Notification.DeduplicationKey` values, creates durable in-app notification rows, and marks the run completed or failed.

Operator signals:

| Signal | Meaning |
|---|---|
| `explore.notifications.fanout_runs` | Run-level processing/completed/skipped-completed/failed outcomes by tenant and fanout kind. |
| `explore.notifications.fanout_subscribers` | Aggregate processed, notification-created, and duplicate-skipped subscriber decisions. |
| `NotificationFanoutRun` rows | Durable worker cursor/count/status state for source event/actor/kind tuples. |
| General outbox dead-letter rows | Internal fanout messages that exceeded retry policy and need inspection/replay decisions. |
| Structured fanout logs | Include run/event/tenant IDs and aggregate counts; do not include event title, subscriber identity, notification body, or deduplication key. |

Fanout is at-least-once. Operators should treat `Notification.DeduplicationKey` and `NotificationFanoutRun` state as the duplicate-prevention and progress source of truth rather than inferring success from process logs alone.

### Notification SSE Refresh Operations

`GET /api/notification/stream` is a long-lived authenticated HTTP response that sends one-way SSE refresh hints to the browser notification bell.

Operational expectations:

| Concern | Guidance |
|---|---|
| Delivery truth | SSE is only a refresh hint. Durable `Notification` rows and authenticated list/detail/unread APIs remain the source of truth. |
| Payload safety | Hints include unread count, unread flag, bounded reason, and timestamp only. Do not include notification body, title, entity IDs, user IDs, deduplication keys, or PII. |
| Authentication | Browser `EventSource` uses same-origin cookies through the BFF/API boundary. Do not rely on custom request headers for the stream. |
| Proxy behavior | Do not buffer `text/event-stream`. The API sets `X-Accel-Buffering: no`; reverse proxies should preserve streaming responses. |
| Compression | Do not add `text/event-stream` to response compression MIME types. Compression can delay SSE frames through buffering. |
| Reconnect | Browser `EventSource` reconnects automatically. The endpoint emits SSE IDs and a reconnect interval; polling remains the fallback. |
| Shutdown | The endpoint honors request cancellation. Long-lived streams should end when the client disconnects or the host shuts down. |

No additional configuration keys were added for SSE refresh hints in this implementation slice.

### Basic Email Dispatch Operations

Registration confirmation email is handled as a durable side effect:

1. The registration command creates an `EmailDispatchOutbox` row in the same PostgreSQL transaction as the registration state.
2. TickerQ `email-dispatch-drain` triggers the shared drain service. In fallback mode, `EmailDispatchProcessor` triggers the same service with a hosted timer.
3. The drain service checks tenant pause state, atomically claims one row, and sets tenant context before resolving SMTP settings.
4. For mapped lifecycle categories with a `UserId`, the drain checks `UserNotificationPreference` immediately before SMTP handoff. A disabled preference records a `Skipped` attempt/receipt/outbox result with failure category `recipient_unsubscribed`; it does not call the SMTP provider and it does not retry.
5. SMTP is called through `IEmailService`; handlers and controllers do not send SMTP, publish RabbitMQ, or schedule TickerQ jobs directly.
6. The drain records `EmailDispatchAttempt` and `EmailDispatchReceipt` state, then marks the outbox row `Sent`, `RetryScheduled`, `DeadLettered`, `Unknown`, or `Skipped`.
7. TickerQ `email-dispatch-recovery-scan` marks stale `Processing` rows as `Unknown` after `EmailDispatchProcessor:ProcessingLeaseTimeoutSeconds`; the hosted-service fallback runs the same recovery scan before each drain loop.

When an absolute public base URL is configured through `PublicBaseUrl`, `App:PublicBaseUrl`, or `Application:PublicBaseUrl`, categorized lifecycle messages include `List-Unsubscribe`, `List-Unsubscribe-Post: List-Unsubscribe=One-Click`, and a visible unsubscribe URL appended to the plain text and HTML bodies. Public launch deployments should configure the public base URL; otherwise the dispatch path still sends allowed email, but it cannot emit absolute unsubscribe links.

Operator signals:

| Signal | Meaning |
|---|---|
| `email-dispatch` health check | Selected dispatch mode, TickerQ enabled state, dashboard enabled state, safe dispatch settings, threshold values, and aggregate outbox counts (`dueDispatchCount`, `retryScheduledCount`, `staleProcessingCount`, `deadLetteredCount`). |
| `explore.email_dispatch.attempts` | Outcome counter for sent, skipped, tenant paused, unknown, retry-scheduled, and dead-lettered attempts. |
| TickerQ dashboard | Optional instance-admin-only scheduler internals. It is disabled by default and is not the product/operator source of truth for email delivery state. |
| Structured drain logs | Include dispatch/outbox IDs, tenant IDs, outcomes, retry delay, and normalized failure category; do not include bodies, recipients, subjects, secrets, provider message IDs, or raw SMTP error text. |

Timeout-like SMTP outcomes are recorded as `Unknown` instead of blind retry. Skipped rows are terminal preference/compliance outcomes and are not replayable. Dead-lettered rows remain in PostgreSQL for operator inspection and later replay tooling.

Crash-window recovery is intentionally conservative. If a node dies after claiming a row but before persisting a final delivery state, the stale-processing recovery scan clears the processing lease, marks the outbox row `Unknown` with failure category `processing_lease_expired`, and marks any processing receipt `Unknown`. Operators should inspect the HAL-gated EmailDispatch admin status and replay only when the business context makes another send safe. The recovery path does not infer SMTP success from TickerQ job state.

TickerQ retries are infrastructure retries. Expected SMTP/provider outcomes should be caught by the drain service and persisted in `EmailDispatchOutbox`; only unexpected infrastructure failures should bubble to TickerQ as failed job executions.

TickerQ operational state is stored by the API-owned `ApiTickerQDbContext` in the PostgreSQL `ticker` schema. The schema is fixed to `ticker` by startup validation because the scheduler migration owns concrete table placement; do not change `Scheduler:TickerQ:Schema` without adding a matching migration path. The scheduler DbContext also keeps its EF migration history table in the `ticker` schema so it never reads the primary application's snake_case migration history rows.

Dashboard protection is enforced twice: TickerQ is configured with host authentication, and the API wraps `UseTickerQ()` with an instance-admin authorization guard for the configured dashboard path. If `Scheduler:TickerQ:DashboardEnabled=false`, the dashboard route is not exposed.

The scheduler job catalog is Application-owned through `IScheduledJobRegistry`. Current implemented jobs are:

| Job | Schedule type | Payload | Source of truth |
|---|---|---|---|
| `email-dispatch-drain` | Cron, every 10 seconds | None | `EmailDispatchOutbox` pending/retry state |
| `email-dispatch-recovery-scan` | Cron, every minute | None | Stale `EmailDispatchOutbox` processing leases |
| `event-reminder-dispatch` | One-off time trigger | Pointer-only IDs | Pre-persisted `EmailDispatchOutbox` row |

Planned-only jobs are `general-outbox-drain`, `pds-sync-drain`, `dead-letter-summary`, `waitlist-promotion-scan`, and `tenant-maintenance-scan`. Do not migrate general outbox or PDS workers to TickerQ until EmailDispatch has green multi-node duplicate execution and crash-window recovery proof.

### Lifecycle-Email Operations

PostgreSQL remains the email delivery ledger. Parent-aware content retention is implemented by `EmailDispatchRetentionCleanupProcessor`: it runs bounded transactional passes, supports dry-run, and records only counts and cutoff timestamps in logs.

- Sent and skipped content redacts after the configured 180-day default; attempt and receipt free text/provider IDs follow the selected parent in the same transaction.
- Dead-lettered, `Unknown`, and parked replay material remains until its explicit resolution timestamp, then follows the same retention clock. `ContentRedactedAt` permanently removes replay authority.
- A `Purged` tenant is eligible immediately. Non-sent work and related delivery/receipt state become typed `tenant_deleted` skips before only non-PII ledger metadata remains.
- Run with `EmailDispatchRetention:DryRun=true` first when changing retention policy. Compare the bounded eligible count, then restore mutating mode. Repeated passes are idempotent because already-redacted parents are excluded.

The remaining lifecycle release requirements are:

- process bounded batches with fair tenant rounds, configurable global/per-tenant concurrency and SMTP rate limits, required-work priority, and high/low backlog hysteresis that defers optional reminders without consuming SMTP attempt count;
- expose oldest pending email/fanout age, success/failure and retryable/permanent rates, dead-letter/unknown/parked counts, typed skip counts, fanout progress/lease contention, and bounded tenant backlog without recipient PII;
- provide authenticated HAL-gated controls to pause/drain, suppress a compromised tenant sender, inspect/reconcile/replay eligible failures, adjust rates, and dry-run cleanup.

Eligibility and the occurrence/version fence are checked in the conditional provider-handoff transition. Before that transition, cancellation, consent withdrawal, preference, deletion, and supersession can skip work. After it, in-flight cancellation is not promised; I/O/protocol/process/persistence uncertainty settles as `Unknown` and is never automatically resent.

### Optional RabbitMQ Dispatch Operations

RabbitMQ Dispatch Mode is optional transport infrastructure over the same PostgreSQL-owned `EmailDispatchOutbox` state machine. It declares RabbitMQ topology, publishes pointer-only `EmailDispatchPointer` messages with mandatory routing and publisher confirmations, exposes `email-dispatch-rabbitmq` readiness, wires the local Aspire `messaging` resource, and can run manual-ack dispatch and DLQ replay workers when explicitly enabled. It does **not** replace Basic Dispatch Mode; API + PostgreSQL + SMTP remains sufficient when RabbitMQ is disabled.

Operator signals:

| Signal | Meaning |
|---|---|
| `email-dispatch-rabbitmq` health check | Disabled mode is healthy and independent; enabled mode proves broker connectivity and topology declaration. |
| `explore.email_dispatch.rabbitmq.publishes` | Outcome counter for disabled, confirmed, returned, nacked, failed, and timeout publish attempts. |
| `explore.email_dispatch.rabbitmq.consumes` | Manual-ack dispatch and DLQ replay delivery counter with low-cardinality `tenant_id`, `outcome`, and `failure_category` tags only. |
| Structured RabbitMQ transport logs | Include dispatch IDs, tenant IDs, topology names, outcomes, and normalized failure categories; do not include recipient addresses, subjects, bodies, provider message IDs, raw broker errors, or AMQP connection strings. |

The RabbitMQ payload is a pointer contract only: tenant ID, stable `PublishEventId`, dispatch kind, source IDs, and optional event/registration/user IDs. Email body, subject, recipient, reply-to, SMTP settings, provider message IDs, and raw provider errors remain out of broker payloads and logs.

Manual-ack dispatch consumption is bounded by `EmailDispatchRabbitMq:PrefetchCount`; ACKs are sent only after `IEmailDispatchDrainService.ProcessSingleAsync(...)` returns a durable PostgreSQL-backed outcome. Malformed or missing pointers are rejected to the queue's DLX/DLQ path, while unexpected transient failures are NACKed with requeue.

DLQ replay is opt-in with `EmailDispatchRabbitMq:DeadLetterReplayEnabled=true`. The replay worker consumes the DLQ with bounded `DeadLetterReplayPrefetchCount`, validates tenant/publish-event/event metadata against the database row, resets replayable durable rows before republishing, parks unsafe messages to the parking queue, and ACKs the original DLQ delivery only after replay or parking publish succeeds. Missing parking topology makes `email-dispatch-rabbitmq` unhealthy because topology declaration is part of the enabled RabbitMQ readiness check.

Operational verification commands:

```bash
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=Email]" --minimum-expected-tests 1
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=RabbitMQ]" --minimum-expected-tests 1
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=Email]" --minimum-expected-tests 1
```

Use the first command for SMTP/Mailpit/Basic Dispatch runtime evidence, the second for optional RabbitMQ topology/publish/consumer/DLQ evidence, and the third for API health, scheduler wrapper, and HAL-gated operator contract evidence.

### Keycloak Identity Lifecycle Email Operations

Keycloak account emails are not part of Basic Email Dispatch. The runtime path for ISLAMU-initiated Keycloak lifecycle messages is:

1. Application code calls `IAccountAuthorityLifecycleEmailService` for email verification, password reset, email update verification, or a future required-action workflow.
2. The Infrastructure Keycloak adapter records a local `NotificationIntent` plus account-authority delegation audit when enabled and configured.
3. The adapter asks Keycloak Admin REST to run `execute-actions-email` for the required action (`VERIFY_EMAIL`, `UPDATE_PASSWORD`, or `UPDATE_EMAIL`).
4. Keycloak generates the action token/link, renders the Keycloak email theme, and sends through the Keycloak realm SMTP provider.

Operational boundaries:

| Concern | Guidance |
|---|---|
| Source of truth | Keycloak is the source of truth for required-action tokens, rendered templates, and provider-side delivery. Local delegation audit proves ISLAMU requested the action; it is not delivery state. |
| Product dispatch separation | Do not inspect `email-dispatch` health, `EmailDispatchOutbox`, RabbitMQ dispatch queues, or TickerQ jobs for Keycloak identity email delivery. Use Keycloak realm SMTP/theme settings and Keycloak logs. |
| `keycloak.smtp_mode` | Operational policy label only. `managed` means SMTP is provider-managed outside this deployment. Self-hosted/shared-SMTP modes may configure Keycloak realm SMTP from deployment credentials, but ownership remains Keycloak. |
| Local Mailpit | Local Keycloak can point its realm SMTP at Mailpit for developer inspection. This is separate from product Basic Dispatch Mailpit settings and must not be documented as a production default. |
| Theme sync | Keycloak email templates live in the Keycloak `email` theme type. A future `keycloak.theme_sync_enabled` automation may apply theme assets, but rendered content and sending stay Keycloak-owned. |
| Development theme cache | During theme work, disable Keycloak theme/template caches with the documented Keycloak flags such as `--spi-theme--static-max-age=-1`, `--spi-theme--cache-themes=false`, and `--spi-theme--cache-templates=false`. Do not carry those settings into production guidance without an explicit operator decision. |
| Redaction | Logs, metrics, local results, and delegation audit must not include admin tokens, provider secrets, raw Keycloak response bodies, action tokens, rendered subjects/bodies, SMTP passwords, or theme output. |

If a Keycloak identity email fails, diagnose in this order: Keycloak lifecycle email options and safe URL policy, admin-token acquisition, Keycloak Admin REST status code, realm SMTP configuration, email theme/template availability, then Keycloak server logs. Do not replay through product `EmailDispatchOutbox`.

## Cerbos PDP Operations

### Storage And Package Topology

Static policies, schemas, and derived roles live in repo-root `cerbos/policies/`; native policy tests live in `cerbos/tests/`. The application publishes the bundled package through `IPolicyPackageService` instead of generating ad-hoc role policies at runtime.

`Cerbos:PolicyPackagePath` (environment variable `CERBOS__POLICYPACKAGEPATH`) points the API at the policy package directory. Container deployments should either bundle `cerbos/` into the API image or mount the policy folder read-only, for example `./cerbos/policies:/app/cerbos/policies:ro` with `CERBOS__POLICYPACKAGEPATH=/app/cerbos/policies`. Aspire local development sets `Cerbos__PolicyPackagePath` for `explore-api` to repo-root `cerbos/policies`; direct `dotnet run` also falls back from the default relative `cerbos/policies` path to repo-root policies when launched from a project subdirectory. If the folder is missing, download endpoints return safe `503 ProblemDetails` without host paths.

Package delivery paths:

| Path | Trigger | Notes |
|---|---|---|
| GitHub Actions production publish | `Cerbos Policy Validation` on `push` to `main` after policy validation succeeds | Production CI/CD path. Uses the `production` GitHub Environment approval gate, digest-pinned `cerbosctl`, and repository secrets `CERBOS_SERVER`, `CERBOS_USERNAME`, `CERBOS_PASSWORD`, plus optional `CERBOS_CA_CERT_PEM`. Uploads `_schemas` before policies and retains `cerbos-policy-publish-evidence`. |
| Docker Compose one-shot sync | `docker compose --profile authz run --rm cerbos-policy-sync` | Recommended self-hosting path. Starts the `authz` profile with `cerbos-db`, maps `.env` `CERBOS_ADMIN_USERNAME` to the Cerbos server's `CERBOS_ADMIN_USER` variable, uses `CERBOS_ADMIN_PASSWORD`, recursively uploads policies and `_schemas`, then requests store reload. Set `CERBOS_ADMIN_PASSWORD_HASH` to the hash matching `CERBOS_ADMIN_PASSWORD` before using Admin API sync. |
| Coolify external Cerbos | [CERBOS_COOLIFY.md](CERBOS_COOLIFY.md) | Separate PDP deployment path for Coolify Docker Image resources. Uses PostgreSQL-backed policy storage, a mounted `conf.yaml`, gRPC h2c routing, and manual or CI `cerbosctl` upload from this repo's `cerbos/policies/` tree. |
| Zero-touch boot sync | API startup when complete instance Admin API config exists | Skips safely when endpoint or credentials are incomplete. |
| Setup/Admin UI sync | Operator-triggered setup or admin action | Advanced path shown only when server-side Admin API credentials are already configured; the browser does not collect Cerbos Admin API passwords. Returns safe issue codes for missing config, auth failure, unavailable/rejected package, reload failure, or unknown package status. |
| Manual ZIP fallback | Setup/Admin download endpoint | Always visible in onboarding, including Local RBAC mode. Exports the same bundled package for `cerbosctl put policy --recursive .` and `cerbosctl put schema --recursive _schemas` when Admin API sync is unavailable or intentionally disabled. |

Runtime authorization checks use the PDP gRPC endpoint. Package sync/status uses the Admin API endpoint and credentials. Do not treat a healthy Admin API as proof that runtime PDP checks are healthy, or vice versa.

### Admin API Configuration (`Cerbos:AdminApi` And BYO Admin API)

| Key | Type | Description |
|---|---|---|
| `Endpoint` / `Endpoints` | `string` / `List<string>` | Instance Admin API target(s) for package upload/status/reload. |
| `AdminUsername` | `string` | Basic Auth username; secret-bearing and redacted from reads/logs. |
| `AdminPassword` | `string` | Basic Auth password; secret-bearing and redacted from reads/logs. Docker Compose uses `CERBOS_ADMIN_PASSWORD` for `cerbosctl` and `CERBOS_ADMIN_PASSWORD_HASH` for the Cerbos server config. |
| `Cerbos:PolicyPackagePath` | `string` | API-local path to bundled or mounted `cerbos/policies`; use `CERBOS__POLICYPACKAGEPATH=/app/cerbos/policies` in containers. |
| BYO custom Admin API endpoint/credentials | tenant governance/secret settings | Optional per-tenant package target, preserved even when the tenant custom PDP endpoint is blank. |

Non-local Admin API/PDP endpoints must use safe TLS-capable URLs. Unsafe endpoint changes are rejected before provider settings are persisted. Runtime failure logs must not include raw endpoints, credentials, JWTs/tokens, response bodies, or exception objects/messages.

### Monitoring

| Signal | Meaning |
|---|---|
| Cerbos readiness health check | Follows fail-closed semantics when instance Cerbos mode is active; local mode skips PDP readiness. |
| Package status issue code | Distinguishes Admin API not configured, auth failure, Admin API unavailable/rejected package, reload failure, generic publish failure, and Cerbos package-status unknown. |
| BYO closed safe-mode log | Tenant BYO failure activated provider-instance fallback safe mode; non-instance-admin decisions deny. |
| BYO open fallback log | Tenant explicitly chose local RBAC fallback for BYO PDP failure. |
| Runtime failure type metadata | Safe diagnostic context; no raw endpoints, credentials, JWTs, response bodies, or exception messages. |

### Incident Triage (Cerbos)

1. Check the runtime PDP health and the app Cerbos readiness endpoint.
2. Verify instance `Cerbos:GrpcEndpoint` for runtime checks and `Cerbos:AdminApi:*` for package operations.
3. For BYO tenants, verify `cerbos.mode`, `cerbos.custom_endpoint`, `cerbos.failure_mode`, and optional custom Admin API endpoint/credentials.
4. If `cerbos.mode=custom_endpoint` has a blank PDP endpoint, runtime authorization still follows BYO failure mode; configure the PDP endpoint or temporarily choose local/open behavior only as an explicit operator decision.
5. For package sync failures, inspect the safe issue code before retrying. Prefer `docker compose --profile authz run --rm cerbos-policy-sync` for self-hosted Compose deployments after confirming `CERBOS_ADMIN_PASSWORD_HASH` matches `CERBOS_ADMIN_PASSWORD`, or use setup/admin manual ZIP download plus `cerbosctl put policy --recursive .` and `cerbosctl put schema --recursive _schemas` when Admin API sync is unavailable.
6. For missing HAL affordances, confirm the link was not denied by server-side authorization before debugging route generation.

## Setup Secret Lifecycle

Instance bootstrap uses `ISetupSecretProvider`:

- if setup mode is active and no env secret exists, API auto-generates a setup secret and logs it at startup;
- onboarding endpoints in BFF (`/bff/setup-secret*`) validate and synchronize secret state;
- setup status returns client-safe state labels (`Environment`, `Generated`, `Expired`, `Locked`, `Unavailable`) and operator guidance without exposing raw secrets;
- generated setup secrets expire 60 minutes after API startup, and recovery is to restart the API and use the newly logged generated secret;
- environment-provided setup secrets remain authoritative, but a timed-out setup window still requires an API restart to reopen setup mode.

Setup-secret-gated API onboarding endpoints use the dedicated `SetupSecret` rate-limit policy. Invalid setup secrets return RFC 7807 `403 Forbidden` with code `forbidden`; setup-secret endpoints called after bootstrap completion return RFC 7807 `410 Gone` with code `setup_already_completed`; rate-limit rejection returns RFC 7807 `429 Too Many Requests`.

The convention-first launch path is Setup Secret → Admin Auth → Site Profile → Preflight → Launch. Preflight readiness data is non-sensitive and separates launch blockers from operational warnings.

## External API Key Operations

External API keys are long-lived credentials for non-interactive callers. Operational guarantees differ from interactive JWT flows in three places: rate-limit partitioning, quota enforcement, and usage-metadata freshness.

### Rate-Limit Partitioning

- When an API-key principal is present on `HttpContext.User`, the `global` and `authenticated` rate-limit policies partition on `api-key:{keyId}` instead of remote IP or user ID.
- Partitioning guarantees that one key's burst does not starve other keys sharing the same egress IP.
- Per-key limits use the same token-bucket configuration as `authenticated` (200 requests / 60s sliding). Per-key write limits match `write` (30 requests / 60s fixed).
- Anonymous and JWT callers retain their existing partition keys (IP and user ID respectively); no behavior change for those paths.

### Clustered Deployment Semantics

Three persistence tiers behave differently in multi-node deployments:

| Tier | Storage | Cluster Behavior | Mitigation |
|---|---|---|---|
| Rate limits | In-process `PartitionedRateLimiter` | **Node-local** — N nodes give N× the advertised limit | Deploy a Redis-backed limiter or an ingress-tier limit when strict global enforcement is required |
| Quota credits | PostgreSQL `ExternalApiKeyQuota` table | **Cluster-safe** — atomic `INSERT ... ON CONFLICT` + `UPDATE ... WHERE credits_used + amount <= limit` with row-level lock | No action required |
| Usage metadata | In-memory write-through to `LastUsedAt` / `LastUsedIp` | **Eventually consistent** — 5-minute in-memory throttle per key; races between nodes are acceptable | No action required; metadata is informational, not security-critical |

### Revocation Semantics

- Revocation sets `ExternalApiKeyStatusId` to `Revoked`. The change takes effect on the **next** authentication attempt — in-flight requests already past the auth handler complete normally.
- Cache invalidation for the key row is immediate (HybridCache `RemoveAsync`); no stale reads beyond the auth handler's first lookup.
- Revoked keys emit `explore.external_api_keys.revoked` business metrics tagged with `tenant_id` and `owner_type`.

### Usage Reporting

- Tenant admins call `GET /api/ExternalApiKey/usage-report?from=&to=` and receive a report scoped to their tenant.
- Instance admins call the same endpoint and receive a platform-wide report — optionally narrowed via `tenantId` query parameter.
- Reports are aggregated from request counts + last-used timestamps; no raw request logs are surfaced (privacy boundary).

### Observability

External API-key business counters use the `explore.external_api_keys.*` prefix. Most lifecycle counters include `tenant_id` and `owner_type`; authentication attempts add `outcome`, and throttle events add `policy`.

- `explore.external_api_keys.created`
- `explore.external_api_keys.revoked`
- `explore.external_api_keys.policy_updated`
- `explore.external_api_keys.rotated` (metric exists for future/overlap workflows; do not infer a public rotate endpoint from this metric alone)
- `explore.external_api_keys.authentication_attempts` (+ `outcome` tag: `success` / `invalid` / `inactive` / `expired` / `tenant_mismatch` / `empty_header`)
- `explore.external_api_keys.throttled` (+ `policy` tag)

Structured logs on the auth handler include `key_id` and outcome only — never the secret segment.

## Analytics Operational Contract

Analytics is optional infrastructure. Provider failures must never block normal product flows.

Current operational expectations:

- `NullAnalyticsProvider` is the safe baseline for disabled deployments.
- Runtime provider selection is tenant-aware and settings-driven through `AnalyticsConfigResolver`.
- Provider/network failures are logged and swallowed so command/query flows continue.
- Self-hosted operators may need first-party proxying or custom endpoints via `analytics.endpoint_url` to reduce CSP or ad-blocker loss.
- Browser relay fallback is available at `POST /api/a/t` for operators who cannot or do not want to load vendor analytics scripts in the browser.

Current provider capability tiers:

| Provider | Current operational tier | Notes |
|---|---|---|
| `none` | Disabled | First-class no-op mode |
| `plausible` | Lightweight web analytics | Pageviews and custom events only; no identify/group semantics by design |
| `posthog` | Rich product analytics | Richest current provider: identify, groups, and feature flags |
| `rybbit` | Browser-richer / validate before broad rollout | Current server code is track/pageview-focused; official docs justify browser identify but not server/group/flag parity |
| `rudderstack` | Advanced pipeline / validate before broad rollout | Full event-spec surface in code, but better understood as a CDP/router than a first-party analytics backend |

Self-hoster deployment tiers:

| Tier | Typical operator posture | Expected analytics mode | Operational focus |
|---|---|---|---|
| Tier 0 | Privacy-first / no analytics | `none` | Zero breakage, zero analytics dependency |
| Tier 1 | Lightweight self-hosting | `plausible`-style web analytics | Simple setup, low overhead, pageview/custom event tracking |
| Tier 2 | Product analytics | `posthog`, `rybbit`, or `rudderstack` | Richer events, optional identity semantics, stronger operator validation |
| Tier 3 | Accuracy-sensitive/self-hosted proxy | First-party proxied analytics | Reverse proxy, CSP, and blocker mitigation guidance |

Incident triage for analytics-related issues:

1. Check whether `analytics.enabled` is actually `true` for the affected tenant.
2. Check resolved `analytics.provider` and `analytics.endpoint_url` values.
3. Check browser blocking conditions first for client-side analytics: CSP, ad blockers, reverse proxy pathing.
4. Confirm failures are isolated to analytics logging and not leaking into user-facing requests.

Transport mode quick guide:

| Mode | Browser behavior | Typical operator use | Requirements |
|---|---|---|---|
| `direct` | Loads provider script from vendor/provider host | Fastest setup, cloud-hosted or permissive CSP | Public API/site key, provider host allowed by CSP/network |
| `proxy` | Loads provider script and ingest through a first-party reverse proxy | Self-hosted deployments that want better blocker resistance and simpler CSP | Reverse proxy for script + ingest paths, forwarded host/proto headers, stable first-party path |
| `relay` | No vendor script in browser; client posts pageview/custom events to `/api/a/t` | Strict CSP, privacy-sensitive, or heavily blocked environments | API reachable from browser, provider configured server-side, no public API key required |

Reverse proxy and CSP notes:

- Avoid blocker-friendly path names such as `/analytics`, `/tracking`, or `/stats` when fronting third-party providers; use opaque first-party paths instead.
- Preserve `Host`, `X-Forwarded-Proto`, and `X-Forwarded-For` correctly so provider proxies and relay rate limiting see the intended origin/protocol.
- Keep proxied analytics endpoints on HTTPS; mixed-content browser failures look like random analytics drops.
- `direct` and `proxy` modes need CSP allowances for the chosen script/connect sources; `relay` mode can keep CSP tighter because the browser only talks to the application origin.
- The relay endpoint has its own `AnalyticsRelay` fixed-window rate limit in addition to normal API protections.

Bootstrap and failure behavior:

- Disabled analytics or provider `none` results in a clean no-op bridge.
- `relay` mode initializes even when `analytics.api_key` is empty.
- Script load failures in `direct` or `proxy` mode degrade to a no-op adapter rather than breaking the page.
- Relay/browser failures must be treated as observational loss only; user-facing navigation and commands continue normally.

Client vs server event responsibilities:

- Browser-side analytics is for pageviews and low-risk interaction telemetry only.
- Server-side analytics is for business events that must come from authoritative handlers or workflows.
- Do not emit the same business action from both client and server unless the event is deliberately modeled as correlated-but-distinct.
- Treat browser pageviews as navigation context and server events as domain facts.

Provider-capability caution:

- Do not promise semantic parity across providers just because the interface shape is shared.
- `PostHog` is the only currently validated provider in this repo that supports rich product-analytics behavior plus feature flags.
- `Plausible` should be positioned as the privacy-friendly lightweight tier.
- `Rybbit` should stay in a validated-with-caution tier until official docs justify server-side parity beyond browser events.
- `RudderStack` should be documented as an advanced event pipeline option, especially for operators who want to forward data onward, not as a direct substitute for PostHog dashboards/flags.

Analytics rollout and incident runbook:

1. **Enable / disable**
   - Use `analytics.enabled = false` or provider `none` for a clean global or tenant-level kill switch.
   - For urgent incident response, short-circuit the proxy/relay path to `204` rather than redeploying clients.
2. **Provider switch**
   - Validate the target provider in a staging tenant first.
   - Keep event names and property semantics stable across the switch; do not promise unsupported provider parity.
   - Prefer a short dual-run window only when operators explicitly need migration confidence.
3. **Proxy / relay verification**
   - Confirm the reverse proxy preserves `Host`, `X-Forwarded-Proto`, and `X-Forwarded-For`.
   - Verify CSP permits the required `script-src` / `connect-src` entries for `direct` or `proxy` mode.
   - Verify `relay` mode can reach `/api/a/t` from the browser without a public API key.
4. **Blocked or missing data**
   - Check CSP reports, browser network failures, and ad-blocker interference before suspecting provider code.
   - If traffic is heavily blocked, move to `proxy` or `relay` mode before expanding provider-specific debugging.
5. **Deferred reliability work**
   - Buffered/outbox analytics delivery is intentionally deferred to a follow-up milestone.
   - Current guidance is best-effort delivery for browser analytics and handler-driven best-effort server events; introduce an outbox only if operators need stronger guarantees for business-critical analytics.

### Cookie Consent & Privacy Governance

The platform provides a consent framework; each instance operator is the data controller responsible for their own legal compliance.

Global kill switch:

- `analytics.global_disable_client_tracking = true` immediately disables all browser-side analytics across all tenants.
- Use this for urgent legal/privacy incidents without editing individual tenant settings.
- Server-side analytics continues normally; this only affects browser SDK initialization.

Provider consent matrix:

| Provider | Cookieless Mode | Banner Required | Decline Behavior |
|---|---|---|---|
| `none` | N/A | No | N/A |
| `plausible` | Always cookieless | No | N/A |
| `rybbit` | Always cookieless | No | N/A |
| `posthog` (`cookieless_mode=always`) | Always cookieless | No | N/A |
| `posthog` (`cookieless_mode=on_reject`) | Cookieless after decline | Yes | Configurable: `disable` or `cookieless` (default: `cookieless`) |
| `posthog` (`cookieless_mode=off`) | Consent-managed | Yes | Disable (no cookieless fallback) |
| `rudderstack` | Full consent (v1) | Yes | Disable (full consent required) |

PostHog privacy defaults for self-hosters:

- `posthog_session_replay`: disabled by default. Requires explicit consent if enabled.
- `posthog_autocapture`: disabled by default. When enabled, captures click/input interactions.
- `posthog_heatmaps`: disabled by default. Collects pointer position data.
- `posthog_person_profiles`: `identified_only` by default. Set to `never` for fully anonymous analytics.
- Admin UI shows contextual warnings when enabling features that expand the consent surface.

Consent cookie operational notes:

- Consent cookies are tenant-scoped (`explore_cc_{stableShortKey}` where `stableShortKey` is the first 8 hex chars of the tenant's immutable GUID) — never shared across tenants. Cookie scope is per effective public host (`SameSite=Lax`, `Secure`, `path=/`).
- Default lifetime is 180 days (configurable via `analytics.consent_cookie_lifetime_days`).
- Cookie values are minimal (`accepted`/`declined` only) — not identity artifacts.
- The consent cookie is classified as strictly necessary and does not require its own consent.
- A persistent "Cookie Settings" link in the footer allows users to withdraw consent at any time.

Admin settings management:

- `GET /api/InstanceOnboarding/analytics-governance` returns current governance settings plus computed advisory fields.
- `PUT /api/InstanceOnboarding/analytics-governance` updates all 10 consent/privacy governance keys.
- Auto-computation is advisory: the resolver suggests recommended settings but does not silently overwrite operator choices.
- The resolver returns `ResolveReasons` (e.g., `GlobalKillSwitch`, `ProviderInherentlyCookieless`, `PosthogCookielessOnReject`, `CookieBannerEnabledByOperator`) for diagnostics and admin UX. These are surfaced in the admin settings panel but never exposed in public-facing DTOs.
- Save-time validation rejects illegal combinations (e.g., `Cookieless` decline behavior for providers that don't support cookieless mode, out-of-range cookie lifetime) and returns warnings for suboptimal but allowed configurations (e.g., PostHog features enabled on a non-PostHog provider, session replay with always-cookieless mode).

## Single-Tenant Endpoint Exposure

`BlockInSingleTenantAttribute` behavior:

- in single-tenant mode with hiding enabled, guarded endpoints return `404` (hidden from discovery).

`RequireMultiTenantAttribute` behavior:

- returns `403` with a clear error payload when feature requires multi-tenant mode.

## Localization Operational Runbooks

### Runbook: TMS Provider Is Down
1. Check Grafana `islamu_tms_fallback_activated_total` — if > 0 in 5m, confirm TMS outage.
2. Check container logs for `[LOCALIZATION] TMS ExportTranslations failed` entries.
3. **Immediate mitigation**: Flip `localization.force_offline_mode` to `true` via Admin UI → Localization → Kill-switches → "Save & Apply Kill-switches Now".
4. Investigate TMS provider status (Tolgee dashboard, Weblate status page).
5. When TMS is restored: disable force-offline, verify live translations resume.

### Runbook: Bundle File Lost / Corrupted
1. Navigate to Admin UI → Localization → Offline Bundle Export.
2. Click "Export {LANG}" for each affected language.
3. Verify file appears in `App_Data/Localization/Bundles/{lang}.json`.
4. If export fails, check the health banner — writable path may not be available.

### Runbook: Writable Path Health Banner Red
1. Check deployment topology: single-instance vs multi-replica.
2. Verify `App_Data/Localization/Bundles/` directory exists and has write permissions.
3. For multi-replica without shared storage: this is expected — see `dev/backlog/distributed-bundle-file-writer.md`.
4. For single-instance: check filesystem permissions, disk space.
5. Escalate to SRE if distributed bundle writer is needed.

### Runbook: TMS Credential Replacement
1. Open the Admin UI localization provider settings for the affected instance or tenant.
2. Replace the TMS API credential through the configured settings or secret-provider path.
3. Save the settings and use the provider test action, if available, to verify the new credential.
4. If the credential is managed by an external secret provider, rotate it in that provider first and then refresh or restart the application according to the secret-provider runbook.
5. Keep the old credential active only for the minimum overlap window required by the external TMS provider.

### Localization Metrics & Alerts

| Metric | Alert Threshold | Description |
|--------|----------------|-------------|
| `islamu_tms_fallback_activated_total` | > 0 in 5m | TMS provider failed; page on-call |
| `islamu_translation_fetch_duration_seconds` | p99 > 5s | TMS latency degradation |
| `islamu_translation_fetch_total{result="error"}` | > 10 in 5m | Repeated fetch failures |

## Incident Triage Quick Checks

1. Check `/health` and `/alive`.
2. Check API logs for migration/seeding failures.
3. Check rate-limit/timeouts if clients receive `429` or `504`.
4. Check tenant resolution and deployment mode (`deployment.mode`) if tenant-scoped behavior is wrong.
5. Check setup-secret mode if onboarding is blocked.
6. Check `islamu_tms_fallback_activated_total` if localization is degraded — flip force-offline if needed.

## AI Agent Operational Context

AI-agent workflow rules are not runtime operations. Keep them in [../AGENTS.md](../AGENTS.md), [../AGENTS.md](../AGENTS.md), and [../dev/active/README.md](../dev/active/README.md) so operators do not have to scan agent tooling while diagnosing production behavior.


## Planned Capacity Work

Partitioning is not implemented. Treat partitioning notes as future capacity planning only; do not document partitioned-table behavior as a current operator contract. Revisit this when tenant-scoped or append-only tables approach sizes where normal indexing and query-filter pruning no longer meet SLOs. This is an intentional architecture decision recorded in [ADR-009](adr/ADR-009-postgresql-partitioning-deferral.md): the product stays simple for Tier 1/Tier 2 self-hosters, while Tier 3 operators get clear activation thresholds and a runbook path before PostgreSQL partitioning becomes production behavior.

## Data Lifecycle And Retention Matrix

This matrix is the Phase 6 source of truth for high-growth operational data. It records current behavior and target policy. Unless a row explicitly says cleanup is implemented, no automated retention job currently exists.

Context7 research notes:

- PostgreSQL declarative range partitioning works best when the partition key appears in query and retention predicates. Partition pruning is driven by partition-key constraints, and old data can be removed operationally by detaching or dropping old partitions.
- EF Core migrations can use `migrationBuilder.Sql(...)` for provider-specific database features that EF does not model directly. PostgreSQL partitioning, partition attachment, and concurrent partition maintenance should therefore be introduced through explicit migration SQL/runbooks, not hidden inside normal entity configuration.

Lifecycle classes:

| Class | Meaning | Cleanup posture |
|---|---|---|
| Compliance evidence | Security, admin, audit, or consent evidence | Retain by default; purge only through documented operator retention policy and legal-hold checks. |
| Durable side-effect ledger | Outbox intent, attempts, receipts, and delivery evidence | Completed rows may age out after operator-safe windows; unresolved rows stay until parked/replayed/resolved. |
| User-facing operational state | User inbox or active workflow state | Keep active rows; archive/delete only after user/admin lifecycle rules are explicit. |
| Rebuildable projection/cache | Derived from canonical tables | Safe to rebuild; cleanup should be tied to source deletion or projection rebuild/drain semantics. |
| Ephemeral safety cache | Short-lived duplicate/retry protection | Delete after expiry plus a small clock-skew buffer. |
| External mirror/index | Copy of another system or object-store metadata | Retention follows source/integration policy; never assume local rows can be dropped without reconciliation. |

| Table family | Lifecycle class | Source of truth / owner | Current cleanup | Target default retention | Partitioning trigger and shape | Phase 6 follow-up |
|---|---|---|---|---|---|---|
| `audit_logs` | Compliance evidence | EF audit writes in `ExploreDbContext` | No automated cleanup | 7 years by default for production/self-hosted compliance, configurable only with legal-hold support | Consider monthly `Timestamp` range partitions when table exceeds 100M rows total, 10M rows per tenant, or time-range audit queries miss SLOs | Add retention settings, export-before-purge runbook, and legal-hold guard before any delete job |
| `configuration_change_logs`, `tenant_lifecycle_logs` | Compliance evidence | Admin/governance and tenant lifecycle workflows | No automated cleanup | 7 years by default; tenant lifecycle logs retained for tenant lifetime plus retention window | Consider yearly or monthly `Timestamp`/`TransitionedAt` range partitions only after audit-log partitioning patterns are proven | Keep append-only; add operator export and retention policy before cleanup |
| `event_contact_share_exports`, `event_contact_share_export_items` | Compliance evidence with PII snapshots | Contact-share export workflow | No automated cleanup | 3 years by default, or longer where operator policy requires consent/export evidence | Consider monthly `CreatedAt` range partitions if export volume becomes large; item rows must stay co-located by export lifecycle | Add policy-controlled purge that preserves aggregate counts/audit evidence while deleting email snapshots when retention expires |
| `notifications` | User-facing operational state | Notification handlers/repository | Soft delete/archive only through user workflows; no age cleanup | Keep unread/unsnoozed rows; retain read or archived rows for 365 days by default after last update | Consider monthly `CreatedAt` range partitions when inbox queries exceed index-only performance or table exceeds 50M rows | Add tenant/user-scoped notification retention job with opt-out for compliance notification types |
| `outbox_messages`, `pds_sync_outbox`, `policy_change_outbox` | Durable side-effect ledger | Transactional outbox processors | Processors update status; no completed-row cleanup | Completed rows: 30 days. Failed/dead-lettered rows: retain until operator resolution, then 90 days | Consider monthly `CreatedAt` range partitions when completed rows dominate scans; worker indexes must keep pending/retry rows hot | Add cleanup that deletes only completed/resolved rows and never deletes pending, processing, retry, failed, or dead-letter rows |
| `email_dispatch_outbox` | Durable side-effect ledger with email PII snapshots | Registration/email dispatch state machine | Implemented: bounded/dry-runnable redaction after sent/skipped or explicitly resolved retention cutoff; purged tenants are immediate; `ContentRedactedAt` blocks replay | Sent rows: 180 days. Dead-lettered/unknown/parked rows: retain until operator resolution, then 180 days | Consider monthly `CreatedAt` range partitions when dispatch history exceeds 25M rows or status polling slows | Monitor bounded cleanup duration/counts and add a dedicated readiness signal if operational evidence requires one |
| `email_dispatch_attempts`, `email_dispatch_receipts` | Durable side-effect ledger | Email dispatch drain/consumer idempotency | Implemented: free-text errors and provider IDs redact transactionally with the selected parent; typed outcomes and timestamps remain | Attempts/receipts follow parent retention; failed/unknown evidence stays while parent is unresolved | Partition only with parent strategy; independent partitioning risks expensive parent/child maintenance | Keep parent-aware regression coverage in the persistence gate |
| `ai_conversations`, `ai_messages`, `ai_runs`, `ai_conversation_references`, `ai_proposed_actions`, `ai_tool_executions` | User-facing operational state with provider/prompt sensitivity | AI assistant conversation, proposal, and confirmed-tool audit flows | Implemented: `AiRetentionCleanupProcessor` iterates active tenants, binds tenant context, resolves each tenant's `ai_assistant.retention_days`, supports `AiRetentionCleanup:DryRun`, redacts message content/action payload/reference summaries/failure messages/tool failure messages, and soft-deletes expired conversation shells through tenant-filtered repository cleanup. | 30 days by default via `ai_assistant.retention_days`, tenant-configurable through governance settings | Do not partition initially; cleanup predicates use tenant plus conversation age and should stay index-backed until AI history volume proves otherwise | Monitor `ai-retention-cleanup` readiness and `explore.ai.retention.*` metrics before broad history enablement; never log prompt content, action payloads, provider responses, or model secrets |
| `idempotency_records` | Ephemeral safety cache | `IdempotencyMiddleware` / `IIdempotencyRepository` | Implemented: reads ignore expired rows, and `IdempotencyCleanupProcessor` deletes rows older than `ExpiresAt + IdempotencyCleanup:ExpirationGraceHours` in bounded batches; dry-run is available | Delete after `ExpiresAt + 24h` safety buffer by default | Do not partition initially; TTL delete by `ExpiresAt` should be enough unless write volume is extreme | Monitor `idempotency-cleanup` readiness and cleanup metrics; revisit only if delete volume or index bloat threatens SLOs |
| `custom_property_projection_dirty_scope` | Rebuildable projection/cache backlog | Projection rebuild/drain coordination | Drained rows remain; pending rows are quota-bounded | Pending rows stay until drained; drained rows retained 7 days for diagnostics | No partitioning initially; the table is quota-bounded per tenant | Add drained-row cleanup and metrics for deleted/drained/pending counts |
| `event_custom_property_projections`, `event_session_custom_property_projections` | Rebuildable projection/cache | Projection updaters from Layer 3 values | Rebuild and source deletes replace/remove rows; no age cleanup | No independent age retention; rows live while source values and exposure rules require them | Consider tenant/hash or event-date-adjacent strategy only after projection query SLOs require it; range partitioning by `UpdatedAt` is not useful for most lookup predicates | Keep rebuild-first recovery; add periodic consistency checks before partitioning |
| `external_api_key_quotas` | Operational accounting ledger | External API key quota service | Cascade delete when key is physically deleted; no age cleanup | 24 monthly periods by default for usage reporting | Do not partition initially; one row per key per period should stay small | Add retention by `PeriodEnd` with tenant/admin reporting guardrails |
| `atproto_records`, `indexed_dids`, `sync_states` | External mirror/index | Federation indexer/PDS sync | No automated cleanup | Retain while the indexed actor/record is active or until federation reconciliation marks it stale | Consider partitioning only if indexer query patterns become time-based; current keys are DID/collection/record oriented | Define federation stale-record reconciliation before cleanup |
| `storage_objects` | External mirror/index with blob lifecycle risk | Storage metadata plus external object store | No automated cleanup | Retain metadata while owning domain reference exists; orphan candidates require quarantine before object deletion | Do not partition initially; metadata cleanup depends on object ownership graph, not time alone | Add orphan detector, quarantine window, and blob-delete idempotency before any purge |

Retention implementation rules:

1. Cleanup jobs must be tenant-aware unless they are explicitly instance/system scoped.
2. Cleanup jobs must be dry-run capable before destructive mode is enabled.
3. Metrics must use bounded dimensions only: table family, lifecycle class, outcome, tenant ID when tenant-scoped, and failure category. Do not tag raw entity IDs, emails, setting keys, custom-property keys, subjects, provider message IDs, or exception text.
4. Hard deletion of compliance evidence requires an operator-visible retention policy, legal-hold check, and audit summary.
5. Partitioning work must include migration/runbook rollback behavior. Detaching a partition for archival is preferred over immediate destructive dropping when evidence value is uncertain.

### Partitioning Decision And Activation Runbook

Current decision: PostgreSQL partitioning is deferred. [ADR-009](adr/ADR-009-postgresql-partitioning-deferral.md) is the durable source of truth. Do not add partitioned tables, partition-maintenance workers, or generated partition migrations until an operator need or load-test result crosses the activation gates below.

Decision rationale:

- Tier 1 self-hosters should not inherit high-scale database maintenance before they need it.
- Existing Phase 6 work now has lifecycle classification and one low-risk cleanup implementation for ephemeral idempotency rows.
- PostgreSQL partitioning changes insert routing, migration operations, backup/restore expectations, and retention procedures.
- EF Core does not model PostgreSQL partition lifecycle directly; partition DDL belongs in explicit SQL migrations and runbooks.

Activation gates:

| Gate | Default trigger | Evidence required |
|---|---:|---|
| Total table size | Candidate table exceeds the matrix threshold, for example `audit_logs` over 100M rows or email dispatch history over 25M rows | Database statistics, index bloat report, and table growth trend |
| Tenant concentration | One tenant exceeds the per-tenant threshold for a candidate table, for example `audit_logs` over 10M rows | Tenant-scoped count query and operator impact assessment |
| Query SLO pressure | Normal indexes and query-filter pruning miss production SLOs for time-range or worker scans | Query plans with timing before/after index tuning |
| Retention pressure | Deleting or archiving old rows creates unacceptable locks, vacuum debt, or maintenance windows | Retention dry-run timings and maintenance logs |
| Backup/restore pressure | Backup, restore, or export windows exceed operator objectives because of one append-heavy table family | Backup/restore timing evidence and recovery objective |

Candidate order:

1. `audit_logs`: first candidate only after legal-hold/export posture exists. Use monthly `Timestamp` range partitions because the table is append-only and naturally queried by time.
2. Completed outbox ledgers: consider only after completed/resolved cleanup exists. Keep pending, processing, retry, failed, and dead-letter rows in hot indexes and never partition them in a way that hides unresolved work from operators.
3. `event_contact_share_exports`: consider after PII-aware purge/export policy exists. Partition export items only with the parent export lifecycle.
4. `notifications`: consider only after read/archive retention rules are implemented and compliance notification categories are protected.
5. `email_dispatch_outbox` plus attempts/receipts: defer until parent-aware redaction/retention exists. Independent child partitioning is not allowed because attempts and receipts must follow parent evidence semantics.

Required implementation package before partitioning becomes current behavior:

1. A decision record naming the table family, partition key, partition interval, retention policy, and rollback plan.
2. An explicit PostgreSQL migration using `migrationBuilder.Sql(...)` or an approved migration extension. Do not hide partition DDL in entity configuration.
3. A preflight script that checks existing data fits the proposed partition bounds and reports rows that would fail routing.
4. A partition creation/attachment runbook. New partitions must be created before writes reach their date range.
5. A detach/archive/drop runbook. Detach before destructive drop when evidence value is uncertain.
6. Integration tests proving insert routing, partition-bound rejection, expected query predicates, and rollback/finalize behavior where feasible.
7. Backup/restore documentation covering parent and child partition tables.

Rollback posture:

- Prefer `DETACH PARTITION` over immediate drop for evidence-bearing tables.
- Do not implement destructive `Down()` behavior that silently loses retained evidence.
- If partitioning is disabled or rolled back, operators must have a tested path to keep accepting new writes without data loss.
- Retention cleanup and legal-hold checks must continue to operate by lifecycle class, not by partition name alone.

### Idempotency Cleanup Operations

The `idempotency-cleanup` readiness check reports the current cleanup posture:

- `Healthy` when cleanup is enabled in delete mode or dry-run mode.
- `Degraded` when cleanup is intentionally disabled.

The worker is explicitly instance/system-scoped because `idempotency_records` are an ephemeral replay cache. It does not delete protected audit, dead-letter, email-dispatch, notification, export, or source-of-truth rows. Use `IdempotencyCleanup:DryRun=true` before first enabling destructive cleanup in an environment, then watch logs and `Explore.Business` metrics:

| Metric | Bounded tags | Meaning |
|---|---|---|
| `explore.idempotency.cleanup_runs` | `mode`, `outcome` | One cleanup attempt in `dry_run` or `delete` mode, with `succeeded` or `failed` outcome. |
| `explore.idempotency.cleanup_rows` | `mode`, `outcome` | Eligible row count in dry-run mode or deleted row count in delete mode. |

Metric tags and logs intentionally exclude raw idempotency keys, request paths, response bodies, tenant IDs, and exception text.

### AI Retention Cleanup Operations

The `ai-retention-cleanup` readiness check reports the current AI retention cleanup posture:

- `Healthy` when cleanup is enabled in redaction mode or dry-run mode.
- `Degraded` when cleanup is intentionally disabled.

The worker is tenant-scoped by design. Each pass reads active tenant lookups, sets tenant context for one tenant at a time, resolves that tenant's `ai_assistant.retention_days`, and invokes the tenant-filtered retention cleanup primitive. It does not disable tenant filters and it does not log tenant IDs, prompt content, provider responses, selected references, proposed-action payloads, API keys, model IDs, or raw provider exceptions.

Use `AiRetentionCleanup:DryRun=true` before first enabling destructive redaction in an environment, then watch logs and `Explore.Business` metrics:

| Metric | Bounded tags | Meaning |
|---|---|---|
| `explore.ai.retention.cleanup_runs` | `mode`, `outcome` | One all-tenant cleanup pass in `dry_run` or `redact` mode, with `succeeded`, `partial_failure`, or `failed` outcome. |
| `explore.ai.retention.cleanup_rows` | `mode`, `category` | Aggregate eligible/redacted row counts for bounded categories such as `eligible_conversations`, `redacted_messages`, and `redacted_proposed_actions`. |

If a pass reports partial failure, inspect bounded worker logs and health configuration first. Do not enable verbose logging that prints prompt text, provider responses, tool payloads, reference summaries, or tenant identifiers.

---

## Custom Property Projections (Milestone D)

### What is it?

Custom property projections are denormalized, read-optimized rows derived from Layer 3 EAV runtime values. They keep discovery/search/filter query paths out of the raw normalized EAV joins.

**Source of truth:** `event_custom_property_definitions` + `event_custom_property_values` (and session equivalents).

**Projection tables:** `event_custom_property_projections`, `event_session_custom_property_projections`.

**Coordination tables:** `custom_property_projection_status` (rebuild tracking), `custom_property_projection_dirty_scope` (skip-on-contention backlog).

### What can go wrong?

| Symptom | Likely Cause | Recovery |
|---|---|---|
| Search/filter results are stale | Projection rows not updated after value write | Rebuild projection for the tenant |
| Projection rows missing for an event | Event created while rebuild was in progress; inline write skipped | Drain dirty scopes, or rebuild single event |
| Rebuild hangs | Advisory lock contention or long-running transaction | Check `custom_property_projection_status` for `Rebuilding` state; if stuck >10min, investigate PostgreSQL advisory lock waits |
| Dirty-scope backlog growing | Frequent rebuilds causing inline writers to skip | Drain dirty scopes; consider reducing rebuild frequency |
| Governance report shows stale data | Counts based on runtime definitions, not projection | No action needed; governance report reads from definitions, not projections |

### How to inspect

**Projection status:**
```
GET /api/admin/custom-property-projections/status?tenantId={tenantId}
```
Returns: `State` (Idle/Rebuilding/Failed), `LastRebuildStartedAt`, `LastRebuildCompletedAt`, `RowsProcessed`, `RowsFailed`, `LastErrorMessage`, `PendingDirtyScopeCount`, `OperationalState`, `RequiresOperatorAction`, and `RecommendedAction`.

`OperationalState` is intentionally bounded for dashboarding:

| State | Meaning | First action |
|---|---|---|
| `healthy` | Projection is idle and no dirty-scope backlog is pending | No action |
| `dirty_backlog_pending` | Inline writers skipped during rebuild contention and queued dirty scopes | Drain dirty scopes or run a tenant rebuild |
| `rebuilding` | Rebuild is currently active and not yet stale | Monitor until completion |
| `rebuild_stale` | Rebuild has been active for more than 10 minutes | Investigate PostgreSQL advisory-lock waits and worker health |
| `failed` | Last rebuild failed | Inspect `LastErrorMessage`, fix the root cause, then rebuild |

**Dirty-scope backlog:**
```
GET /api/admin/custom-property-projections/dirty-scopes?tenantId={tenantId}&projectionName=event_custom_property_projection
```
Returns: pending (un-drained) dirty-scope rows with creation timestamps and reasons.

**Projection rows for a specific event:**
```
GET /api/admin/custom-property-projections/events/{eventId}?exposureCeiling=Public
```
Use `exposureCeiling` when inspecting rows for public/export/moderation analysis. Public callers and generated-client consumers must not read raw projection rows without a ceiling.

**Projection rows for a specific event session:**
```
GET /api/admin/custom-property-projections/sessions/{eventSessionId}?exposureCeiling=Public
```

**Governance report (Rule 12):**
```
GET /api/admin/custom-property-definitions/governance-report?tenantId={tenantId}&scope=Event
```
Returns: all active Layer 3 definitions with flags, instance counts, and `PromotionRecommendation`.

### How to recover

**Full tenant rebuild (event projections):**
```
POST /api/admin/custom-property-projections/rebuild
Body: { "tenantId": "{tenantId}" }
```
Acquires advisory lock, rebuilds all projection rows, drains pending dirty scopes on completion. If lock is not acquired (another rebuild running), returns immediately with `lockAcquired: false`.

**Single event rebuild:**
```
POST /api/admin/custom-property-projections/rebuild-single-event
Body: { "eventId": "{eventId}" }
```
Refreshes all projection rows for one event inside a transaction.

**Drain dirty scopes without rebuild:**
```
POST /api/admin/custom-property-projections/drain-dirty-scopes
Body: { "tenantId": "{tenantId}", "projectionName": "event_custom_property_projection" }
```
Processes pending dirty-scope rows without triggering a full rebuild. Idempotent — returns `drainedCount: 0` if no pending rows.

**Session equivalents:** Replace `/rebuild` with `/sessions/rebuild`, `/rebuild-single-event` with `/sessions/rebuild-single`, etc.

### Concurrency model

- **Advisory locks:** Rebuild acquires a PostgreSQL advisory lock keyed on `fnv1a(projectionName), fnv1a(tenantId)`. Only one rebuild runs per projection per tenant.
- **Skip-on-contention:** Inline writers (triggered by value/definition changes) attempt the same lock. If contended (rebuild in progress), they upsert a `custom_property_projection_dirty_scope` row instead of blocking.
- **Drain-on-completion:** The rebuild worker drains all pending dirty scopes after completing its scan, so the skip window is bounded.
- **ConcurrencyStamp:** All mutable EAV entities carry an EF Core `ConcurrencyStamp` (`Guid`). `DbUpdateConcurrencyException` is translated to HTTP 409 with `code: concurrent_update`.
- **Quota errors:** Business quota breaches return HTTP 422 with `code: quota_exceeded`, `quotaKey`, `limit`, `scope`, and optional `actual`/`attempted` fields.
- **Admin purge:** Hard purge is separate from normal delete. It is admin-only, writes an audit summary, and is blocked when historical values, projection rows, audit references, or sync provenance exist.

### Metrics

Use these meters for custom-property projection and lifecycle dashboards:

| Meter | Metric | Safe dimensions |
|---|---|---|
| `Explore.Projections` | `explore.projections.rebuild_total` | `tenant_id`, `projection_type`, `lock_acquired` |
| `Explore.Projections` | `explore.projections.rebuild_failures_total` | `tenant_id`, `projection_type`, `lock_acquired` |
| `Explore.Projections` | `explore.projections.rebuild_duration_seconds` | `tenant_id`, `projection_type`, `lock_acquired` |
| `Explore.Projections` | `explore.projections.drain_total` | `tenant_id`, `projection_type` |
| `Explore.Projections` | `explore.projections.drained_scopes_total` | `tenant_id`, `projection_type` |
| `Explore.Projections` | `explore.projections.dirty_scope_skips_total` | `tenant_id`, `projection_type`, `operation`, `reason` |
| `Explore.Projections` | `explore.projections.quota_exceeded_total` | `tenant_id`, `projection_type`, `quota_key`, `scope` |
| `Explore.Business` | `explore.custom_properties.purge_decisions` | `tenant_id`, `scope`, `outcome`, `blocker_category` |

Do not add raw custom-property `Namespace`, `Key`, display names, event IDs, session IDs, or purge reasons as metric dimensions. Those values are high-cardinality and may expose tenant-specific semantics. Use admin API responses for targeted inspection instead.

### Hard limits

| Setting Key | Default | Platform Max |
|---|---|---|
| `custom_properties.max_definitions_per_tenant_per_entity_scope` | 500 | 5000 |
| `custom_properties.max_definitions_per_event` | 100 | 1000 |
| `custom_properties.max_definitions_per_event_session` | 50 | 500 |
| `custom_properties.max_options_per_definition` | 200 | 2000 |
| `custom_properties.max_multi_value_rows_per_value` | 20 | 200 |
| `custom_properties.projection_rebuild_batch_size` | 500 | 5000 |
| `custom_properties.max_dirty_scope_pending_per_tenant` | 10000 | 100000 |

### Governance report (Rule 12)

The governance report surfaces Layer 3 custom property definitions that may be candidates for promotion to Layer 2 (typed schema) or Layer 1 (universal core), using the Atlassian 4-question framework:

| Recommendation | Trigger |
|---|---|
| `None` | No search/filter/moderation/analytics flags set |
| `ConsiderProjectionFirst` | `IsSearchable` or `IsFilterable` is true |
| `ConsiderLayer2Promotion` | `IsModerationRelevant` or `IsAnalyticsRelevant` is true |
| `ConsiderLayer1Promotion` | `IsModerationRelevant` AND (`IsSearchable` or `IsFilterable`) AND used by ≥30% of tenant's events |

Review quarterly. Promotion is an operational decision, not an automated action.
