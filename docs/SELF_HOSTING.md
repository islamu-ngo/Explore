ABOUTME: Production self-hosting guide covering Docker Compose stack, configuration, and operations.
ABOUTME: Covers all infrastructure services, environment variables, TLS, health checks, and upgrades.

# Self-Hosting

This guide covers deploying the ISLAMU Event platform outside of the development Aspire environment.

## Architecture Overview

The production stack consists of:

| Service | Purpose | Required |
|---------|---------|----------|
| Event API | REST API with CQRS/MediatR | Yes |
| Explore Blazor BFF | Blazor Server + YARP proxy to API | Yes |
| PostgreSQL | Primary database | Yes |
| Keycloak | OIDC identity provider | Yes |
| MinIO / S3 | Object storage for images and files | Yes |
| Reverse proxy | TLS termination, routing | Recommended |

## Docker Compose Stack

A minimal production `docker-compose.yml`:

```yaml
services:
  postgres:
    image: postgres:17
    environment:
      POSTGRES_DB: islamu_event
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5

  keycloak:
    image: quay.io/keycloak/keycloak:latest
    command: start
    environment:
      KC_DB: postgres
      KC_DB_URL: jdbc:postgresql://postgres:5432/keycloak
      KC_DB_USERNAME: ${DB_USER}
      KC_DB_PASSWORD: ${DB_PASSWORD}
      KC_HOSTNAME: ${KEYCLOAK_HOSTNAME}
      KC_PROXY_HEADERS: xforwarded
      KEYCLOAK_ADMIN: ${KC_ADMIN_USER}
      KEYCLOAK_ADMIN_PASSWORD: ${KC_ADMIN_PASSWORD}
    depends_on:
      postgres:
        condition: service_healthy

  minio:
    image: minio/minio:latest
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: ${MINIO_USER}
      MINIO_ROOT_PASSWORD: ${MINIO_PASSWORD}
    volumes:
      - miniodata:/data
    healthcheck:
      test: ["CMD", "mc", "ready", "local"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    image: ghcr.io/islamu/event-api:latest
    environment:
      - Database__ConnectionString=Host=postgres;Database=islamu_event;Username=${DB_USER};Password=${DB_PASSWORD}
      - Keycloak__Authority=https://${KEYCLOAK_HOSTNAME}/realms/islamu
      - Storage__Endpoint=http://minio:9000
      - Storage__AccessKey=${MINIO_USER}
      - Storage__SecretKey=${MINIO_PASSWORD}
    depends_on:
      postgres:
        condition: service_healthy
      keycloak:
        condition: service_started

  blazor:
    image: ghcr.io/islamu/event-blazor:latest
    environment:
      - BFF__ApiBaseUrl=http://api:7039
      - Authentication__Authority=https://${KEYCLOAK_HOSTNAME}/realms/islamu
    depends_on:
      - api
    ports:
      - "443:8443"

volumes:
  pgdata:
  miniodata:
```

## Environment Variables

### Database

| Variable | Description | Example |
|----------|-------------|---------|
| `Database__ConnectionString` | PostgreSQL connection string | `Host=postgres;Database=islamu_event;Username=app;Password=secret` |

### Authentication (Keycloak)

| Variable | Description | Example |
|----------|-------------|---------|
| `Keycloak__Authority` | OIDC authority URL | `https://auth.example.com/realms/islamu` |
| `Keycloak__ClientId` | OIDC client ID | `islamu-event` |
| `Keycloak__ClientSecret` | OIDC client secret (set via `KEYCLOAK_BLAZOR_CLIENT_SECRET` in Infisical) | (from Keycloak) |

### Storage (MinIO / S3)

| Variable | Description | Example |
|----------|-------------|---------|
| `Storage__Endpoint` | S3 endpoint URL | `http://minio:9000` |
| `Storage__AccessKey` | S3 access key | `minioadmin` |
| `Storage__SecretKey` | S3 secret key | (secret) |
| `Storage__BucketName` | Default bucket | `islamu-event` |

### Secret Provider (Optional)

| Variable | Description | Default |
|----------|-------------|---------|
| `SecretProvider__Provider` | Provider type: `None`, `Infisical` | `None` |
| `SecretProvider__FailFast` | Fail startup if secrets unavailable | `false` |
| `SecretProvider__Infisical__Url` | Infisical server URL | — |
| `SecretProvider__Infisical__ProjectId` | Infisical project ID | — |
| `SecretProvider__Infisical__ClientId` | Universal Auth client ID | — |
| `SecretProvider__Infisical__ClientSecret` | Universal Auth client secret | — |

See [SECRETS.md](SECRETS.md) for full provider configuration and [CONFIGURATION.md](CONFIGURATION.md) for all application settings.

### Outbox Processor

| Variable | Description | Default |
|----------|-------------|---------|
| `OutboxProcessor__Enabled` | Enable background outbox processing | `true` |
| `OutboxProcessor__PollingIntervalSeconds` | Poll frequency | `5` |
| `OutboxProcessor__BatchSize` | Messages per batch | `100` |
| `OutboxProcessor__MaxRetryCount` | Max retries before dead-letter | `5` |

### Multi-Tenancy

| Variable | Description | Default |
|----------|-------------|---------|
| `DEPLOYMENT_MODE` | Set to `multi_tenant` in the API environment/Infisical `/api` folder to start onboarding in multi-tenant mode. Omit for single-tenant onboarding. | *(unset = single tenant)* |
| `Deployment__DefaultTenantId` | Default tenant UUID | `018e4e5c-7f00-7000-8000-000000000001` |

Most self-hosted installs should leave `DEPLOYMENT_MODE` unset. The first-run flow then launches a single-tenant site, hides tenant mechanics, and creates the internal default tenant during completion. Set `DEPLOYMENT_MODE=multi_tenant` only before first launch when the deployment is intentionally hosting multiple tenants.

During first-run onboarding, complete the flow in this order: enter the setup secret, authenticate the initial administrator, fill the Site Profile, review Preflight, then Launch. Preflight blocks missing critical launch requirements such as auth provider and canonical host configuration, and warns about operational maturity items such as SMTP, object storage, backups, observability, and public exposure policy.

## Keycloak Realm Setup

1. Create a realm named `islamu`
2. Create a client with:
   - Client ID: `islamu-event`
   - Client authentication: ON (confidential)
   - Valid redirect URIs: `https://your-domain.com/*`
   - Web origins: `https://your-domain.com`
3. Enable the following scopes: `openid`, `profile`, `email`
4. Create roles: `admin`, `organizer`, `moderator`
5. Map roles to the `realm_access.roles` claim

The API extracts user ID from JWT claims with fallback order: `sub` → `nameidentifier` → `sid`.

## Database Migrations

Migrations run automatically on startup via `MigrationService`. For manual control:

```bash
dotnet ef database update --project Event.Persistence --startup-project Event.API
```

The migration service is idempotent — safe to run on every deployment.

## Reverse Proxy

Place a reverse proxy (nginx, Caddy, Traefik) in front of the Blazor BFF for TLS termination:

```nginx
server {
    listen 443 ssl;
    server_name your-domain.com;

    ssl_certificate /etc/ssl/certs/your-cert.pem;
    ssl_certificate_key /etc/ssl/private/your-key.pem;

    location / {
        proxy_pass http://blazor:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

The Blazor BFF proxies API requests via YARP — clients never access the API directly.

## Health Checks

The API exposes health endpoints:

| Endpoint | Purpose |
|----------|---------|
| `/health` | Readiness probe for `ready`-tag checks (database, cache, OIDC when configured, SMTP, conditional Cerbos, secrets) |
| `/alive` | Liveness probe for `live`-tag checks |
| `/metrics` | Prometheus metrics endpoint |

Health check components:

| Check | Tag | What It Verifies |
|-------|-----|-----------------|
| PostgreSQL | `db` | Database connectivity |
| Keycloak | `auth` | OIDC provider reachable |
| MinIO | `storage` | Object storage accessible |
| Secret Provider | `secrets` | Secret management healthy (if configured) |

## Monitoring

The application emits:
- **Structured logs** — JSON format compatible with Loki, ELK, or any log aggregator
- **Metrics** — Prometheus-compatible via OpenTelemetry (business metrics, secret refresh, outbox processing)
- **Traces** — OpenTelemetry distributed traces

See [OPERATIONS.md](OPERATIONS.md) for full observability configuration.

## Upgrade Procedure

1. **Backup database** — `pg_dump` before any upgrade
2. **Pull new images** — `docker compose pull`
3. **Stop services** — `docker compose down` (graceful shutdown waits for in-flight requests)
4. **Start services** — `docker compose up -d`
5. **Verify** — check `/health` is `Healthy` or intentionally `Degraded`, and `/alive` returns healthy
6. **Monitor** — watch logs for migration completion and startup errors

Migrations apply automatically. Rollback requires restoring the database backup and reverting to previous images.

## Related

- [GETTING_STARTED.md](GETTING_STARTED.md) — development setup
- [CONFIGURATION.md](CONFIGURATION.md) — all application settings
- [SECRETS.md](SECRETS.md) — secret provider configuration
- [OPERATIONS.md](OPERATIONS.md) — monitoring and observability
- [SECURITY.md](SECURITY.md) — authentication and authorization architecture
