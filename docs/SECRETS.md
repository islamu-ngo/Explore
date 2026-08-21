ABOUTME: Secret management abstraction with pluggable providers and automatic refresh.
ABOUTME: Covers Explore.Secrets library configuration, health monitoring, and encryption services.

# Secrets Management

The `Explore.Secrets` library provides a provider-agnostic secret management layer with automatic refresh, health monitoring, and encryption.

## Provider Types

| Provider | Enum | Status | Auth Method |
|---|---|---|---|
| None | `0` | Implemented | Environment variables only |
| Infisical | `1` | Implemented | Universal Auth (ClientId + ClientSecret) |
| Vault | `2` | Not implemented | AppRole (RoleId + SecretId) |
| Azure Key Vault | `3` | Not implemented | DefaultAzureCredential |
| AWS Secrets Manager | `4` | Not implemented | Region-based |

When `Provider = None`, secrets come exclusively from environment variables and `appsettings.json`. No external provider is contacted.

## Configuration

### SecretProvider Section

```json
{
  "SecretProvider": {
    "Provider": "None",
    "FailFast": true,
    "Infisical": {
      "Url": "",
      "ProjectId": "",
      "ClientId": "",
      "ClientSecret": "",
      "Environment": "dev",
      "Paths": ["/"]
    }
  }
}
```

### Local Development User Secrets

For local development, the projects share a unified .NET User Secrets ID defined in the `.csproj` files: `event-shared-secrets`.

Maintainers using Infisical-backed Aspire profiles must create or edit the shared `secrets.json` file:
- **Linux/macOS:** `/home/{user}/.microsoft/usersecrets/event-shared-secrets/secrets.json`
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\event-shared-secrets\secrets.json`

Add the bootstrap credentials for your maintainer developer environment inside this file:
```json
{
  "Infisical:Url": "https://example.com",
  "Infisical:ProjectId": "",
  "Infisical:Environment": "dev",
  "Infisical:ClientId": "",
  "Infisical:ClientSecret": ""
}
```

> [!IMPORTANT]
> The contributor default Aspire profile is `local-full`. It starts local infrastructure and sets `SecretProvider:Provider=None` for child projects, so contributors should not need Infisical credentials.
> Maintainer profiles intentionally differ:
> - `local-core` starts local PostgreSQL/Redis but loads auth, policy, storage, webhook, and provider settings from Infisical/config.
> - `local-lite` starts the migration worker, API, and Blazor, with all infrastructure loaded from Infisical/config.
> If Infisical is not used in a maintainer profile, supply equivalent settings through environment variables or appsettings before running the AppHost.

### Docker Compose Environment Files

The repository root `.env.example` mirrors the supported Infisical folder layout and documents which service consumes each key. Copy it to `.env` for local Compose runs; `.env` is intentionally ignored by git.

Docker Compose uses `.env` for interpolation before starting containers. The Compose file then passes explicit `environment:` entries into each service. Do not rely on a broad `env_file: .env` import because it would place unrelated secrets into containers that do not need them.

### Primary database credentials

The database contract is structured; do not store or inject a raw connection
string. Endpoint metadata uses `DATABASE_PROVIDER`, `DATABASE_HOST`,
`DATABASE_PORT`, `DATABASE_NAME`, `DATABASE_SCHEMA`, `DATABASE_TLS_MODE`, and
`DATABASE_TRUST_SERVER_CERTIFICATE`. MariaDB and MySQL automatically infer
server flavor from `DATABASE_PROVIDER` and default to modern LTS versions (`11.4`
for MariaDB, `8.4` for MySQL); operators on custom engine versions can optionally
supply `DATABASE_SERVER_VERSION`.

`DATABASE_SCHEMA` is non-secret namespace metadata. PostgreSQL and SQL Server
use it as the application and Data Protection schema and keep clean table names
such as `users`. SQLite, MariaDB, and MySQL always apply the fixed `ie_` prefix
and do not use this field for table placement. Prefer a separate SQLite file or
MariaDB/MySQL database for each deployment instance; never invent or store a
configurable prefix secret.

| Compose key | Direct .NET key | Consumer |
|---|---|---|
| `DATABASE_RUNTIME_USERNAME`, `DATABASE_RUNTIME_PASSWORD` | `Database:Runtime:Username`, `Database:Runtime:Password` | API/runtime processes only |
| `DATABASE_MIGRATOR_USERNAME`, `DATABASE_MIGRATOR_PASSWORD` | `Database:Migrator:Username`, `Database:Migrator:Password` | `Event.MigrationService` only |

SQLite has no database credentials. Its `DATABASE_NAME` value is a
persisted local file path and is deployment configuration, not a secret. Do not
give runtime services the migrator role, and never expose either role to the
Blazor client.

### Privacy-erasure authority credentials

| Compose / Infisical key | Direct .NET key | Consumer |
|---|---|---|
| `DATABASE_RUNTIME_USERNAME`, `DATABASE_RUNTIME_PASSWORD` (in `/database/erasure`) or `DATABASE_ERASURE_RUNTIME_USERNAME` | `Database:Erasure:Runtime:Username`, `Database:Erasure:Runtime:Password` | API only, and only for `ExternalDatabase` |
| `DATABASE_MIGRATOR_USERNAME`, `DATABASE_MIGRATOR_PASSWORD` (in `/database/erasure`) or `DATABASE_ERASURE_MIGRATOR_USERNAME` | `Database:Erasure:Migrator:Username`, `Database:Erasure:Migrator:Password` | `Event.MigrationService` only |

For `ExternalDatabase`, endpoint metadata is supplied under `/database/erasure` in Infisical (or via `DATABASE_ERASURE_HOST`, `PORT`, `DATABASE_NAME`, `TLS_MODE`, and `TRUST_SERVER_CERTIFICATE` / `PrivacyErasureAuthorityDatabase:*`); the provider is fixed to PostgreSQL. Use separate
roles: runtime receives only authority append/read function execution, while
migrator owns schema and grants. Never pass either authority credential to
`Explore.Blazor` or `Explore.Blazor.Client`. Rotate the migrator credential
independently of runtime. These values are unused in `EmbeddedSqlite` topology;
that mode has no database username/password and protects its dedicated local
file with filesystem permissions. Its nonsecret deployment fields are
`PrivacyErasureAuthorityEmbedded:Path` (default
`/app/data/privacy_erasure_authority.db`), `WriterReplicaCount=1`, and
`BusyTimeoutSeconds=30`.

There are two Infisical paths through the application:

- `SecretProvider:Provider=Infisical` controls the `ISecretResolver` provider used by settings/secret-binding resolution.
- Non-empty bare `Infisical:*` bootstrap values enable the startup compatibility loaders that fetch Infisical paths directly into `IConfiguration`.

For full local runs, keep `SECRET_PROVIDER=None` and leave `INFISICAL_*` blank
so local structured `DATABASE_*`, Keycloak, Cerbos, and storage values remain
authoritative. Infisical loads primary database configuration directly from `/database`
using `DATABASE_*` keys mapped to the structured `Database:*` configuration section.

### SecretRefresh Section

| Key | Default | Purpose |
|---|---|---|
| `Enabled` | `true` | Enable periodic secret refresh |
| `RefreshInterval` | `00:05:00` | Polling interval |
| `InitialDelay` | `00:00:10` | Delay before first refresh |
| `BaseBackoffDelay` | `00:00:05` | Base delay for exponential backoff |
| `MaxBackoffDelay` | `00:05:00` | Maximum backoff cap |
| `JitterFactor` | `0.1` | Randomization factor to prevent thundering herd |
| `UnhealthyThreshold` | `3` | Consecutive failures before unhealthy status |

Backoff formula: `BaseBackoffDelay × 2^(failures - 1)`, capped at `MaxBackoffDelay`, plus jitter.

### Encryption Section

| Key | Purpose |
|---|---|
| `CurrentKeyVersion` | Active encryption key version (integer) |
| `KeyVersions` | Dictionary of version → key material |
| `MasterKeyEnvironmentVariable` | Env var holding the master key |

Encryption uses AES-256-GCM with 12-byte nonce and 16-byte authentication tag.

### ATProto OAuth Session Envelopes

AT Protocol authentication has three purpose-separated, instance-only key rings:

| Secret key | Consumer | Purpose |
|---|---|---|
| `auth.atproto.oauth_client_private_jwks` | Blazor BFF | P-256/ES256 OAuth `private_key_jwt` assertions and short-lived bootstrap assertions. |
| `auth.atproto.session_encryption_keyring` | Infrastructure | AES-256-GCM encryption of the complete persisted CarpaNet OAuth session. |
| `auth.atproto.session_jwt_private_jwks` | API | P-256/ES256 signing and validation of short-lived first-party ATProto session JWTs. |

Never reuse a key between these purposes. Key IDs may be persisted or advertised where the protocol requires them; private key values, OAuth tokens, DPoP keys, and decrypted session JSON must remain inside the owning server process.

`auth.atproto.session_encryption_keyring` is an instance-only secret used exclusively for durable CarpaNet OAuth sessions. Its value is strict JSON with one active AES-256 key and zero or more retired read keys:

```json
{"keys":[{"kid":"2026-07","k":"<base64url-encoded-32-byte-key>","status":"active"},{"kid":"2026-06","k":"<base64url-encoded-32-byte-key>","status":"retired"}]}
```

Key IDs are persisted as metadata; key material is never stored in the application database. The encrypted envelope contains the complete token set and private DPoP JWK. AES-GCM associated data binds the ciphertext to the tenant, user, provider, subject DID, normalized PDS URI, OAuth client signing-key ID, and envelope version, so copying a row across any of those boundaries fails authentication.

To rotate, publish a new active key and mark the previous active key retired. A successfully restored session is rewritten under the active key. Keep each retired key available until no session row references its key ID; removing it earlier forces affected users to authenticate again. Unknown keys, malformed key rings, ciphertext tampering, or binding mismatches fail closed as reauthentication and must never be repaired by inventing session data or restoring plaintext credential columns.

OAuth client and session-JWT signing rings follow the same overlap rule: exactly one active signing key, with previous keys retained as retired verification/session keys until their pinned OAuth sessions or issued JWTs have expired or been revoked. Rotate one purpose at a time, verify readiness, and only then remove an unused retired key. Removing an in-use session-encryption or OAuth-client key deliberately invalidates the affected sessions and requires those users to sign in again.

Only the BFF OAuth-client signing ring is evaluated by the `atproto-authentication` readiness check. Validate the encryption and session-JWT rings with a controlled sign-in/session refresh before removing retired keys; their consumers otherwise fail closed when persisting/restoring a session or issuing/validating a first-party JWT.

### Promotion Code Lookup HMAC Key

Promotion lookup uses the dedicated registry key `promotions.code_lookup_hmac_key`. It is instance-only, server-only, non-bootstrap secret material; it must not share a key with encryption, signing, webhook, payment, or provider credentials. Each usable key requires a qualified `SecretBinding` whose qualifier is exactly `v{version}`, such as `v1`. The default initial source coordinates are `/promotions/PROMOTIONS_CODE_LOOKUP_HMAC_KEY` for Infisical and `PROMOTIONS_CODE_LOOKUP_HMAC_KEY` for environment-variable bindings. Overlapping versions must point to distinct immutable source coordinates; do not repoint both `v1` and `v2` bindings at one environment variable whose value is replaced.

The value must be standard Base64 that decodes to at least 32 bytes. Infrastructure normalizes the attendee-entered code, scopes the input to tenant and event, and computes HMAC-SHA256. The database stores only the digest and positive lookup-key version as private persistence metadata. Management and checkout read contracts never expose either value or an internal promotion-code identifier; they expose only a masked display label. Organizer code creation and code rotation return the organizer-entered plaintext once, and the Studio component clears it after acknowledgement, context change, failure, or disposal.

Rotate the lookup trust root in this order:

1. Create the new secret value at a distinct external coordinate and add its instance binding under the next qualifier, for example `v2`, without changing or deleting `v1`.
2. Set `Promotions:CodeLookup:ActiveKeyVersion` to the new positive version and restart the API replicas so new publish/code-rotation writes pin that version.
3. Confirm controlled create/apply behavior without copying raw codes, digests, binding coordinates, or secret material into logs, screenshots, health output, or support artifacts.
4. Retain every older qualified key while any active promotion-code row references its `LookupKeyVersion`. Remove an old binding only after an authoritative database/administrative check shows no active code uses it.

Changing the HMAC key value in place under an existing qualifier invalidates lookup for every active code pinned to that version and is not a supported rotation. A missing, malformed, or too-short qualified key fails closed: code publishing/rotation or application does not fall back to another source or compute an unkeyed digest.

### Rotation Section

| Key | Default | Purpose |
|---|---|---|
| `Enabled` | `false` | Enable automatic key rotation |
| `GracePeriod` | `00:00:30` | Overlap window during rotation |
| `MaxConcurrentRotations` | `5` | Parallel rotation limit |

## Key Mapping

Infisical uses `SCREAMING_SNAKE_CASE` with path-based sections. The provider maps bidirectionally:

| Infisical Path + Key | .NET Configuration Key |
|---|---|
| `/atproto/ATPROTO_OAUTH_CLIENT_PRIVATE_JWKS` | `auth.atproto.oauth_client_private_jwks`; resolves to BFF `Atproto:OAuthClientPrivateJwks` |
| `/atproto/ATPROTO_SESSION_ENCRYPTION_KEYRING` | `auth.atproto.session_encryption_keyring`; consumed by Infrastructure session-envelope protection |
| `/atproto/ATPROTO_SESSION_JWT_PRIVATE_JWKS` | `auth.atproto.session_jwt_private_jwks`; consumed by API first-party session JWT signing/validation |
| `/keycloak/REALM_NAME` | `Keycloak:RealmName` |
| `/keycloak/KEYCLOAK_CLIENT_ID` | Nonsecret browser/BFF client metadata mapped to `Keycloak:ClientId` for API onboarding detection. |
| `/keycloak/KEYCLOAK_BLAZOR_CLIENT_SECRET` | Blazor BFF `Keycloak:ClientSecret` and Compose `keycloak-init` client-secret sync input |
| `/keycloak/KEYCLOAK_API_CLIENT_SECRET` | Optional legacy/future Compose `keycloak-init` sync input for deployments that intentionally make the API resource-server client confidential; not needed by the current bearer-only API audience client |
| `/keycloak/KEYCLOAK_SMTP_*` | Optional Compose `keycloak-init` realm SMTP bootstrap. Leave `KEYCLOAK_SMTP_HOST` blank to preserve existing Keycloak SMTP settings; set host/port/from to apply deployment-managed SMTP. |
| `/api/CONTROL_PLANE_REGISTRATION_CREDENTIALS` | `management.control_plane_registration_credentials` | Directional managed control-plane registration credentials. This key is instance-only and is bound only through managed inline-encrypted application secret storage, not as a startup bootstrap key. |
| `/api` or `/cerbos` + `AUTHORIZATION_PROVIDER` | Non-secret `Authorization:Provider` deployment intent. Blank keeps manual Local-first onboarding; `local` or `cerbos` makes the provider deployment-owned and skips the choice page. |
| root or AI path + `AI_TOOL_PROPOSALS_ENABLED` | `AiProvider:ToolProposalsEnabled` |
| `/database/DATABASE_PROVIDER` | Primary database provider: `PostgreSql`, `Sqlite`, `SqlServer`, `MariaDb`, `MySql` |
| `/database/DATABASE_HOST` | Primary database host |
| `/database/DATABASE_PORT` | Primary database port |
| `/database/DATABASE_NAME` | Primary database name or SQLite file path |
| `/database/DATABASE_SCHEMA` | Application schema (default: `islamu_event`; PostgreSQL and SQL Server) |
| `/database/DATABASE_TLS_MODE` | TLS mode: `Prefer`, `Required`, `Disabled` |
| `/database/DATABASE_TRUST_SERVER_CERTIFICATE` | `false` (default: strict CA verification) or `true` (accept self-signed certs) |
| `/database/DATABASE_SERVER_VERSION` | Optional override for MariaDB/MySQL (defaults: MariaDB `11.4`, MySQL `8.4`; e.g. `10.11`, `8.0`) |
| `/database/DATABASE_RUNTIME_USERNAME` | Runtime database username |
| `/database/DATABASE_RUNTIME_PASSWORD` | Runtime database password |
| `/database/DATABASE_MIGRATOR_USERNAME` | Migrator database username |
| `/database/DATABASE_MIGRATOR_PASSWORD` | Migrator database password |
| `/database/ERASURE_TOPOLOGY` | Privacy erasure topology: `EmbeddedSqlite`, `CoLocated`, `ExternalDatabase` |
| `/database/erasure/DATABASE_PROVIDER` | External authority provider (fixed to `PostgreSql`) |
| `/database/erasure/DATABASE_HOST` | External authority PostgreSQL host |
| `/database/erasure/DATABASE_PORT` | External authority PostgreSQL port (default: `5432`) |
| `/database/erasure/DATABASE_NAME` | External authority PostgreSQL database name |
| `/database/erasure/DATABASE_TLS_MODE` | External authority TLS mode: `Prefer`, `Required`, `Disabled` |
| `/database/erasure/DATABASE_TRUST_SERVER_CERTIFICATE` | `false` (default: strict CA verification) or `true` (accept self-signed certs) |
| `/database/erasure/DATABASE_RUNTIME_USERNAME` | External authority runtime username (function-execution role) |
| `/database/erasure/DATABASE_RUNTIME_PASSWORD` | External authority runtime password |
| `/database/erasure/DATABASE_MIGRATOR_USERNAME` | External authority migrator username (schema/admin role) |
| `/database/erasure/DATABASE_MIGRATOR_PASSWORD` | External authority migrator password |
| storage path + `STORAGE_S3_*` | `Storage:S3*` (for example `/storage/STORAGE_S3_ENDPOINT` → `Storage:S3Endpoint`) |
| `/smtp/MAIL_SMTP_HOST` | `smtp.host` secret binding default; Development seed maps it to `email.smtp_host` when no SMTP setting exists |
| `/smtp/MAIL_SMTP_PORT` | `smtp.port` secret binding default; Development seed maps it to `email.smtp_port` when no SMTP setting exists |
| `/smtp/MAIL_SMTP_USERNAME` | `smtp.username` / `email.smtp_username` secret-bearing SMTP username |
| `/smtp/MAIL_SMTP_PASSWORD` | `smtp.password` / `email.smtp_password` secret-bearing SMTP password |
| `/smtp/MAIL_SMTP_FROM_ADDRESS` | `smtp.from_address` secret binding default; Development seed maps it to `email.from_address` when no SMTP setting exists |
| `/smtp/MAIL_SMTP_FROM_NAME` | `smtp.from_name` secret binding default; Development seed maps it to `email.from_name` when no SMTP setting exists |
| `/cerbos/CERBOS_USE_POLICY_SCOPE` | `Cerbos:UsePolicyScope` |
| `/reporting/OSPREY_API_KEY` | `reporting.osprey_api_key` tenant Osprey provider credential |
| `/reporting/OSPREY_WEBHOOK_SECRET` | `reporting.osprey_webhook_secret` tenant Osprey callback/signing secret when a deployment uses one |
| `/reporting/COOP_API_KEY` | `reporting.coop_api_key` tenant Coop provider credential |
| `/reporting/COOP_WEBHOOK_SECRET` | `reporting.coop_webhook_secret` tenant Coop callback HMAC secret |
| `/stripe/STRIPE_PLATFORM_SECRET_KEY` | `payments.stripe.platform_secret_key` instance/server-only Stripe platform secret owned by each self-hoster |
| `/stripe/STRIPE_WEBHOOK_SECRET` | `payments.stripe.webhook_secret` instance/server-only Stripe webhook signing secret owned by each self-hoster |
| `/promotions/PROMOTIONS_CODE_LOOKUP_HMAC_KEY` | `promotions.code_lookup_hmac_key` instance/server-only HMAC material; each binding uses a positive `v{version}` qualifier |
| `/registration-providers/REGISTRATION_PROVIDER_API_TOKEN` | `registration_provider.api_token` tenant provider API/OAuth credential binding. Google Forms uses this for the OAuth access token or refresh-token envelope. |
| `/registration-providers/REGISTRATION_PROVIDER_WEBHOOK_SECRET` | `registration_provider.webhook_secret` tenant callback signing secret binding for providers that use shared callback secrets. Google Forms Pub/Sub does not use it because callback authentication is Google OIDC. |
| `/integrations/listmonk/LISTMONK_API_USERNAME` | `integrations.listmonk.api_username` |
| `/integrations/listmonk/LISTMONK_API_KEY` | `integrations.listmonk.api_key` |
| `/registration-providers/REGISTRATION_PROVIDER_API_TOKEN` | `registration_provider.api_token` |
| `/registration-providers/REGISTRATION_PROVIDER_WEBHOOK_SECRET` | `registration_provider.webhook_secret` |
| `/api/VAPID_SUBJECT` | `WebPush:VapidSubject` |
| `/api/VAPID_PUBLIC_KEY` | `WebPush:VapidPublicKey` |
| `/api/VAPID_PRIVATE_KEY` | `WebPush:VapidPrivateKey` |
| raw process environment + `STORAGE_S3_*` | consumed directly by the S3 resolver as a compatibility fallback |

The three ATProto rows use the same uppercase name as their default environment-variable name as well as their Infisical key. Environment variable format otherwise uses double-underscore separators for .NET keys, for example `S3Settings__Endpoint`. Storage also accepts raw `STORAGE_S3_*` variables for deployment compatibility. Primary database bootstrap uses discrete structured fields rather than a single URL-form connection string; the PostgreSQL-only compatibility loader remains a fallback for older development inputs. SMTP secret-provider defaults use the user-facing `MAIL_SMTP_*` names; local Compose also exports older `SMTP_*` aliases for compatibility with development seeding. Registration-provider credentials are tenant-scoped secret definitions and must be bound through `SecretBinding`; use the bounded `Qualifier` field when several tenant connections need distinct API tokens or webhook secrets for the same key. Connection DTOs carry binding IDs only and never carry secret values.

Stripe secrets are instance-scoped, server-only, and optional while paid events are disabled. `Payments:Stripe:Mode=Test` requires a platform key beginning `sk_test_`; `Live` requires `sk_live_`. The Connect endpoint uses only the dedicated webhook binding, never the platform key or an outgoing-webhook secret. Rotate platform and endpoint secrets deliberately with the matching Stripe mode and endpoint configuration; retain no secret value in logs, support artifacts, browser DTOs, OpenAPI, or the DBML reference.

Compose Keycloak bootstrap consumes `KEYCLOAK_ADMIN` and `KEYCLOAK_ADMIN_PASSWORD` only inside the one-shot `keycloak-init` container. Those credentials are not application runtime secrets and must not be stored in governance settings or copied into support artifacts. The init logs redact client secret values.

The checked-in Keycloak realm exports never contain the confidential Blazor BFF client secret. Compose requires `KEYCLOAK_BLAZOR_CLIENT_SECRET` and fails closed when it is absent. Local Aspire generates a secret parameter when that deployment value is absent, persists it in the AppHost user-secrets store, and injects the same value into `keycloak-init`, the API, and the BFF without rendering it as ordinary resource configuration.

External-Keycloak setup bootstrap accepts a one-time Keycloak admin or service-account username/password through the setup UI. Treat that credential as operator input for a single setup request, not as a platform-managed secret. ISLAMU must not save it to appsettings, environment variables, Infisical paths, database governance settings, logs, traces, screenshots, or support bundles. After a successful bootstrap, only the runtime Keycloak OIDC values and BFF client secrets are stored according to the normal authentication secret ownership model.

Paid-event hosted onboarding never returns payment platform secrets, provider account identifiers, or connection identifiers to the browser. `Payments:OrganizerDirect:ProviderCode` and `ConnectPlatformId` are server configuration, not secret values, but are still omitted from browser readiness contracts.

Keycloak onboarding and administrator reads always redact the runtime client secret. They may expose only configured/source/editability metadata plus nonsecret authority and client ID. For application-managed ownership, a stored secret wins and a deployment value is only a bootstrap fallback. For deployment-managed ownership, the deployment source is authoritative, stored database secrets are ignored, and setup writes do not persist a replacement.

Keycloak configuration writes and secret rotation derive ownership/configured state from authoritative server-side configuration instead of trusting client-supplied ownership metadata. A new confidential BFF client requires a secret; a blank write is valid only when the server already resolves an effective secret that was redacted from the browser read. Deployment-managed rotation returns operator action guidance and never writes a replacement to application storage.

### Onboarding And Setup Credentials

The `/setup` route is a pre-authentication operator gateway, not a browser-owned secret workflow. Browser-supplied setup, tenant, authorization, and provider-administration headers are stripped before proxying. The BFF/server replaces them only from trusted server-owned state, and the API validates the resulting authority again.

The following values must never be persisted in browser storage, returned in browser-facing DTOs, or copied into logs, traces, screenshots, diagnostics, or support artifacts:

- access, refresh, and provider tokens;
- the setup secret or a recoverable derivative;
- temporary provider administrator usernames/passwords or service-account credentials;
- raw provider request or response bodies.

An explicit `SETUP_SECRET` is always authoritative until onboarding completes and locks setup mode. When it is empty, the API writes one random secret to `SETUP_SECRET_FILE` with `0600` permissions and logs only a `docker cp` retrieval instruction. The platform default is `/tmp/islamu-event/setup-secret`; split Compose overrides it to the `setup_data` volume at `/app/bootstrap/setup-secret`, and standalone uses `/app/data/setup-secret`. The file is deleted when onboarding completes; setting a non-empty `SETUP_SECRET` also removes and overrides any generated file. After validation, the BFF keeps the secret in a protected, HttpOnly 30-minute rolling session and requires re-entry after 30 minutes without setup activity.

Retrieve the generated fallback only from the Docker host, copy it to an owner-protected local file, enter it at `/setup`, then remove the local copy. Do not include the generated file in backups. An unmounted temp file may be replaced with a new secret after container recreation; the old value immediately stops working. Read-only containers and rolling or multi-replica API deployments must use an explicit shared `SETUP_SECRET` from their platform secret manager.

Rerunning verification or completion does not grant permission to read a stored secret back. Application-managed credentials remain write-only and are rotated through the owning server operation. Deployment-managed credentials remain authoritative in their configured environment/secret provider; rotate them there, refresh or restart as required, and confirm only through configured/readiness metadata. Do not overwrite a deployment-managed value from onboarding to repair drift.

See [SELF_HOSTING.md](SELF_HOSTING.md#first-run-setup-secret) for the operator flow and [TROUBLESHOOTING.md](TROUBLESHOOTING.md#onboarding-recovery-matrix) for recovery without disclosing credentials.


## Ownership Model

Secrets use a platform-wide ownership model so environment variables and external secret providers are not permanent live overrides by accident. The ownership source is metadata; browser-facing DTOs expose only configured/source/editability flags and never raw values, ciphertext, provider tokens, or resolved secret coordinates.

| Mode | Source types | UI behavior | Runtime meaning |
|---|---|---|---|
| Application-managed | `InlineEncrypted` / application-stored encrypted values | Editable in ISLAMU Event admin/setup UI, masked after save | Saved application settings are the runtime authority; deployment values can only prefill/import until an operator saves. |
| Deployment-managed | `EnvironmentVariable` or `Infisical` binding, or an explicit deployment-managed key list | Read-only badge in UI; rotate outside the app | Values are controlled by environment, appsettings, or the secret provider and changes require provider refresh or redeploy/restart. |
| Deployment bootstrap | Environment/secret-provider value exists but no application-managed value has been saved | Editable prefill with “Bootstrap from Deployment” badge | The value helps first-run setup only. If modified and saved, application-managed settings take precedence from then on. |

Do not merge application-managed and deployment-managed values for the same field at runtime. The `SecretResolver` dispatches through one `SecretBinding` source and intentionally does not fallback to another source after a binding is selected. For settings still migrating to the shared secret control plane, the same contract applies at the DTO/UI boundary: deployment values may prefill forms, but they do not silently override saved application settings unless that key is explicitly marked deployment-managed.

Reporting provider secrets are server-side tenant settings. API keys and webhook secrets for Osprey and Coop must never be returned in browser DTOs, HAL links, health checks, logs, metrics, traces, screenshots, issue templates, or support bundles; browser/control-plane surfaces may expose only configured/source/editability metadata. Routing update actions are write-only for secret values: supplying a new Osprey/Coop API key or webhook secret rotates that tenant value, while omitting the field or sending it blank preserves the currently stored secret. There is no implicit clear-secret endpoint and no readback path; confirm rotation through configured flags, provider readiness checks, and secret-provider audit trails.

Current migrated surface: Cerbos authorization settings expose endpoint and Admin API credential ownership metadata. `AUTHORIZATION_PROVIDER` is non-secret deployment intent, while `CERBOS_ADMIN_USERNAME` and `CERBOS_ADMIN_PASSWORD` are server-side deployment secrets resolved from environment configuration or Infisical. The browser normally sees only configured flags and ownership metadata. During an explicit setup sync, an operator may instead submit a complete one-time pair; it exists only in the Blazor server circuit and request pipeline, overrides deployment credentials for that call, is cleared after the call, and is never written to `SystemSetting`, returned by an API, or logged. `CERBOS_ADMIN_PASSWORD_HASH` is the Cerbos server verifier and cannot authenticate an Admin API client; keep the matching plaintext password only in deployment secrets or enter it for one sync. Reporting provider secret keys are registered as sensitive hierarchical settings for the moderation routing foundation. Listmonk API username/key values are registered server-side secret bindings; admin updates are write-only and browser DTOs expose configured flags only. Stripe `payments.stripe.platform_secret_key` and `payments.stripe.webhook_secret` are instance/server-only definitions for self-hoster-owned platform credentials. Promotion lookup resolves the qualified instance-only `promotions.code_lookup_hmac_key` binding for every digest operation. SMTP, S3, OAuth, localization/TMS, and AI keys still have area-specific storage/UI paths and must not be documented as fully migrated until their resolvers use the shared ownership metadata consistently.

Web Push VAPID keys are deployment configuration. Infisical `/api/VAPID_PRIVATE_KEY` maps to `WebPush:VapidPrivateKey`; it is a server-only secret and must never appear in browser configuration, API responses, HAL links, logs, traces, health data, screenshots, or support artifacts. `VAPID_PUBLIC_KEY` is intentionally public and is returned by `GET /vapid-public-key` as plain text and by `GET /api/notification/web-push/config`. Browser subscription endpoints and `p256dh`/`auth` material are stored tenant-scoped and are never echoed by subscription status DTOs.

## ISecretProvider Interface

| Method | Returns | Purpose |
|---|---|---|
| `InitializeAsync` | `Task` | One-time provider setup |
| `GetSecretAsync` | `Task<string?>` | Single secret by key |
| `GetSecretWithMetadataAsync` | `Task<SecretWithMetadata?>` | Secret with version and timestamps |
| `GetSecretsByPathAsync` | `Task<IDictionary>` | All secrets under a path |
| `RefreshAsync` | `Task` | Force refresh from provider |
| `GetHealthAsync` | `Task<HealthCheckResult>` | Provider health status |

Properties: `ProviderType`, `SupportsRefresh`.

## Health Check

Registered as `secret_provider` with tag `secrets`.

| Status | Condition |
|---|---|
| Healthy | Fewer than `UnhealthyThreshold` consecutive failures |
| Degraded | 1–2 consecutive failures |
| Unhealthy | ≥ `UnhealthyThreshold` consecutive failures |

Health data includes: provider type, supports refresh, consecutive failure count, last successful refresh timestamp.

## Metrics

Meter name: `Explore.Secrets`

| Instrument | Type | Purpose |
|---|---|---|
| `secrets_refresh_total` | Counter | Total refresh attempts |
| `secrets_refresh_failures_total` | Counter | Failed refresh attempts |
| `secrets_refresh_duration_seconds` | Histogram | Refresh operation duration |
| `secrets_consecutive_failures` | UpDownCounter | Current consecutive failure count |
| `secrets_last_refresh_timestamp_seconds` | Gauge | Unix timestamp of last successful refresh |

## Refresh Service

A `BackgroundService` using `PeriodicTimer` that:

1. Waits `InitialDelay` before first poll.
2. Calls `ISecretProvider.RefreshAsync()` at each `RefreshInterval`.
3. On success, calls `IConfigurationRoot.Reload()` to propagate changes.
4. On failure, applies exponential backoff with jitter.
5. Logs structured warnings with correlation IDs.

## Audit Decorator

Wraps `ISecretProvider` to log all secret access operations. Sensitive keys are redacted (matches: `password`, `secret`, `key`, `token`, `credential`, `connectionstring`, `apikey`).

Audit entries track: Operation, ProviderType, KeyPattern (redacted), Timestamp, UserId (extracted via `sub` → `nameidentifier` → `sid` fallback), CorrelationId.

## DI Registration

| Method | Purpose |
|---|---|
| `AddSecretProvider` | Core provider registration |
| `AddSecretManagement` | Full setup (provider + refresh + health + metrics + encryption) |
| `AddSecretObservability` | Health checks + metrics |
| `AddSecretMetrics` | Prometheus metrics only |
| `AddSecretHealthCheck` | Health check only |
| `AddSecretRefreshService` | Background refresh service |
| `AddEncryptionService` | AES-256-GCM encryption |
| `AddKeyRotationService` | Automatic key rotation |
| `AddRotationAwareHttpClientFactory` | HttpClient with rotating credentials |
| `AddRotationAwareDbContextFactory<T>` | DbContext with rotating connection strings |
| `AddConnectionRotation<T>` | Generic connection rotation |

Typical startup: `services.AddSecretManagement(configuration)` registers everything.

## Related

- [CONFIGURATION.md](CONFIGURATION.md) — application settings
- [SELF_HOSTING.md](SELF_HOSTING.md) — environment variable reference
- [OPERATIONS.md](OPERATIONS.md) — health checks and metrics
