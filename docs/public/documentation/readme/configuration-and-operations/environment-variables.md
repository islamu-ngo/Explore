---
description: Comprehensive reference for all deployment and runtime environment variables.
---

# Environment Variables Reference

ISLAMU Event is configured via standard POSIX environment variables, typically populated through a root `.env` file or injected securely via container orchestration. 

Use `.env.example` in the repository root as your baseline template. This document categorizes all available environment variables, default values, sensitivity levels, and operational impacts.

---

## 1. Core Deployment & Networking

| Variable | Requirement | Default | Description |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | **Required** | `Production` | Runtime mode: `Production`, `Staging`, `Development`, or `Testing`. |
| `API_HTTP_PORT` | Optional | `7039` | Internal HTTP port for `Explore.API`. |
| `UI_HTTP_PORT` | Optional | `7002` | Internal HTTP port for `Explore.Blazor` (BFF). |
| `PUBLIC_URL` | **Required** | None | Fully-qualified public HTTPS URL of your application (e.g., `https://events.example.org`). |
| `DEPLOYMENT_MODE` | Optional | `SingleTenant` | Multi-tenancy mode: `SingleTenant` or `multi_tenant`. Must be set before first-run onboarding. |
| `BFF_ADMIN_HOSTS` | Optional | None | Comma-separated list of dedicated admin hostnames (e.g., `admin.example.org`) to render the Instance Console. |

---

## 2. Database & Relational Persistence

The platform supports PostgreSQL, SQLite, SQL Server, and MySQL/MariaDB through structured settings:

| Variable | Requirement | Default | Description |
|---|---|---|---|
| `DATABASE_PROVIDER` | **Required** | `postgres` | Selected provider: `postgres`, `sqlite`, `sqlserver`, or `mysql`. |
| `DATABASE_HOST` | Required (Split) | None | Hostname or IP of the database server. |
| `DATABASE_PORT` | Optional | `5432` | Database port (`5432` for PostgreSQL, `1433` for SQL Server, `3306` for MySQL). |
| `DATABASE_NAME` | Required | `islamu_event` | Target database name. |
| `DATABASE_SCHEMA` | Optional | `public` | Schema namespace for PostgreSQL/SQL Server (clean table names inside it). |
| `DATABASE_TLS_MODE` | Optional | `Prefer` | TLS verification mode: `Disable`, `Prefer`, or `Require`. |
| `DATABASE_RUNTIME_USERNAME` | Required | None | Least-privilege credentials used by `Explore.API` for runtime queries. |
| `DATABASE_RUNTIME_PASSWORD` | **Secret** | None | Password for runtime database user. |
| `DATABASE_MIGRATOR_USERNAME` | Required | None | DDL-capable credentials used by `Event.MigrationService` to apply migrations. |
| `DATABASE_MIGRATOR_PASSWORD` | **Secret** | None | Password for migration service database user. |

---

## 3. Identity Provider (Keycloak)

Keycloak is the mandatory identity authority for user authentication:

| Variable | Requirement | Default | Description |
|---|---|---|---|
| `KEYCLOAK_URL` | **Required** | None | Public or reverse-proxied base URL of Keycloak (e.g., `https://auth.example.org`). |
| `KEYCLOAK_REALM` | **Required** | `islamu` | Keycloak realm name. |
| `KEYCLOAK_BLAZOR_CLIENT_ID` | **Required** | `event-blazor` | OIDC Confidential Client ID configured for the Blazor BFF. |
| `KEYCLOAK_BLAZOR_CLIENT_SECRET` | **Secret** | None | 32-byte hex client secret generated for the BFF client. |
| `KEYCLOAK_REQUIRE_HTTPS_METADATA`| Optional | `true` | Enforce HTTPS metadata validation for OIDC endpoints. |

---

## 4. Secret Authority Management

The platform supports pluggable secret resolution via `Explore.Secrets`:

| Variable | Requirement | Default | Description |
|---|---|---|---|
| `SECRET_PROVIDER` | **Required** | `Environment` | Provider authority: `Environment` (direct `.env` injection), `Infisical`, or `UserSecrets` (dev only). |
| `INFISICAL_URL` | Optional | `https://app.infisical.com` | Infisical server URL if using Infisical. |
| `INFISICAL_PROJECT_ID` | Optional | None | Target Infisical project UUID. |
| `INFISICAL_CLIENT_ID` | Optional | None | Universal Auth Machine Client ID. |
| `INFISICAL_CLIENT_SECRET` | **Secret** | None | Universal Auth Client Secret. |
| `INFISICAL_ENV` | Optional | `prod` | Infisical environment slug (`prod`, `staging`, `dev`). |

---

## 5. Storage Providers (Media & Avatars)

Configure local filesystem storage or S3-compatible cloud storage:

| Variable | Requirement | Default | Description |
|---|---|---|---|
| `STORAGE_PROVIDER` | Optional | `local` | Storage provider: `local` or `s3`. |
| `STORAGE_LOCAL_ROOTPATH` | Optional | `/app/storage-data/local` | Filesystem directory used when `STORAGE_PROVIDER=local`. |
| `STORAGE_S3_ENDPOINT` | Optional | None | S3 API endpoint (e.g., `https://s3.amazonaws.com` or MinIO URL). |
| `STORAGE_S3_BUCKET_NAME` | Optional | None | Dedicated bucket name for platform uploads. |
| `STORAGE_S3_ACCESS_KEY_ID` | **Secret** | None | S3 Access Key ID. |
| `STORAGE_S3_SECRET_ACCESS_KEY` | **Secret** | None | S3 Secret Access Key. |
| `STORAGE_S3_REGION` | Optional | `us-east-1` | S3 Region identifier. |
| `STORAGE_S3_FORCE_PATH_STYLE` | Optional | `true` | Set `true` for MinIO / self-hosted S3; `false` for AWS S3. |

---

## 6. Email (SMTP & Notifications)

| Variable | Requirement | Default | Description |
|---|---|---|---|
| `EMAIL_SMTP_HOST` | **Required** | None | Outgoing SMTP server hostname. |
| `EMAIL_SMTP_PORT` | **Required** | `587` | SMTP port (`587` for STARTTLS, `465` for SSL, `1025` for local Mailpit). |
| `EMAIL_FROM_ADDRESS` | **Required** | None | Default sender email address (e.g., `events@example.org`). |
| `EMAIL_FROM_NAME` | Optional | `ISLAMU Event` | Default sender display name. |
| `SMTP_USERNAME` | **Secret** | None | SMTP authentication username. |
| `SMTP_PASSWORD` | **Secret** | None | SMTP authentication password. |
| `EMAIL_SMTP_SECURITY` | Optional | `StartTls` | Security mode: `None`, `Auto`, `SslOnConnect`, `StartTls`, `StartTlsWhenAvailable`. |

---

## 7. Outgoing Webhooks & Svix

| Variable | Requirement | Default | Description |
|---|---|---|---|
| `WEBHOOKS_PROVIDER` | Optional | `Local` | Outgoing delivery engine: `Disabled`, `Local`, `Svix`, `Composite`, or `DryRun`. |
| `SVIX_SERVER_URL` | Optional | `http://svix:8071` | URL of self-hosted Svix instance when `WEBHOOKS_PROVIDER=Svix`. |
| `SVIX_AUTH_TOKEN` | **Secret** | None | Administrative auth token for Svix API. |

---

## 8. Authorization & Cerbos PDP

| Variable | Requirement | Default | Description |
|---|---|---|---|
| `AUTHORIZATION_PROVIDER` | Optional | `local` | Authorization mode: `local` (in-database RBAC) or `cerbos`. |
| `CERBOS_GRPC_ENDPOINT` | Optional | `http://cerbos:3593` | gRPC endpoint of the external Cerbos Policy Decision Point. |
| `CERBOS_USE_TLS` | Optional | `false` | Enable TLS when communicating with external Cerbos PDP over gRPC. |
| `CERBOS_PLAINTEXT_MODE` | Optional | `true` | Set `true` for internal container networks (`h2c`); `false` when TLS is active. |

---

## 9. Privacy Erasure Authority Topology

Enforces GDPR right-to-erasure and anti-resurrection fences:

| Variable | Requirement | Default | Description |
|---|---|---|---|
| `PRIVACY_ERASURE__AUTHORITY__TOPOLOGY` | **Required** | `EmbeddedSqlite` | Storage mode: `EmbeddedSqlite` (dedicated local SQLite file), `CoLocated` (in primary DB), or `ExternalDatabase`. |
| `PRIVACY_ERASURE__AUTHORITY__CONNECTIONSTRING` | Optional | None | Connection string if using `ExternalDatabase` topology. |

---

## 10. Headless Onboarding & Legal Identity

Required for non-interactive administrative bootstrapping and legal compliance:

| Variable | Requirement | Default | Description |
|---|---|---|---|
| `SETUP_SECRET` | Optional | None | Pre-shared secret to unlock `/setup`. If omitted, generated automatically in volume. |
| `INSTANCE_BOOTSTRAP_MODE` | Optional | `Interactive` | Mode: `Interactive` (web wizard) or `ConfiguredAdministrator` (headless). |
| `INSTANCE_BOOTSTRAP_ADMIN_PROVIDER` | Optional | None | Required if headless: `keycloak` or `atproto`. |
| `INSTANCE_BOOTSTRAP_ADMIN_SUBJECT` | Optional | None | Required if headless: exact user ID subject claim issued by Keycloak or ATProto DID. |
| `INSTANCE_BOOTSTRAP_BINDING_GENERATION` | Optional | None | Required if headless: positive integer generation counter. |
| `INSTANCE_BOOTSTRAP_ADMIN_EMAIL` | Optional | None | Administrator email address. |
| `INSTANCE__OPERATORIDENTITY__OPERATORID` | **Required** | None | UUIDv7 unique identifier for the operating legal entity. |
| `INSTANCE__OPERATORIDENTITY__PUBLICNAME` | **Required** | None | Public brand name of the deploying organization. |
| `INSTANCE__OPERATORIDENTITY__LEGALNAME` | **Required** | None | Full legal registered entity name. |
| `INSTANCE__OPERATORIDENTITY__PUBLICCONTACTEMAIL` | **Required** | None | Public contact email for legal and compliance inquiries. |
| `INSTANCE__OPERATORIDENTITY__TERMSURL` | **Required** | None | Public URL for terms of service. |
| `INSTANCE__OPERATORIDENTITY__PRIVACYURL` | **Required** | None | Public URL for privacy policy. |
