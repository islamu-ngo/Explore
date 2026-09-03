---
description: Deploy and operate the production split service topology with Docker Compose.
---

# Docker Compose Self-Hosting

Docker Compose is the recommended production topology for operators seeking independently scalable application and infrastructure services. In this split topology, `Explore.API`, `Explore.Blazor` (BFF/UI), and infrastructure dependencies run as distinct containers.

---

## 1. Architecture & Service Topology

The standard production Compose stack includes:

| Service | Container Image | Port / URL | Description |
|---|---|---|---|
| **`event-ui`** | `islamu/event-blazor` | `http://localhost:7002` | Blazor WebAssembly UI and Backend-for-Frontend (BFF). Handles cookies and OIDC auth. |
| **`event-api`** | `islamu/event-api` | `http://localhost:7039` | Core REST API, CQRS handlers, background workers, and business logic. |
| **`event-migrationservice`** | `islamu/event-migrationservice` | One-shot worker | Applies database migrations, Data Protection keys, and default seeds before API/UI start. |
| **`postgres`** | `postgres:16-alpine` | `localhost:5432` (internal) | Primary application relational database. |
| **`keycloak`** | `quay.io/keycloak/keycloak:24.0` | `http://localhost:8080` | Identity Provider (IdP) for user authentication and OIDC tokens. |
| **`keycloak-db`** | `postgres:16-alpine` | Internal only | Dedicated database for Keycloak state. |

### Optional Service Profiles

Additional capabilities can be enabled dynamically via Docker Compose profiles:

| Profile | Services Included | Purpose |
|---|---|---|
| `storage` | `minio` (`:9000`, console `:9001`) | S3-compatible local object storage for avatars and event media. |
| `authz` | `cerbos` (`:3592`, gRPC `:3593`) | External Policy Decision Point (PDP) for fine-grained authorization. |
| `webhooks` | `svix` (`:8071`) | Scalable outbound webhook delivery engine. |
| `mail` | `mailpit` (`:8025`, SMTP `:1025`) | Local email testing capture inbox (for staging/evaluation). |

---

## 2. Prerequisites & Preparation

1. **Host Requirements**:
   - OS: Linux (Ubuntu 22.04+ / Debian 12 recommended)
   - CPU: 2+ vCPUs
   - RAM: 4 GB minimum (8 GB recommended for full stack with Keycloak and Cerbos)
   - Disk: 20 GB SSD storage
   - Software: Docker Engine 24+ and Docker Compose v2.20+

2. **Clone and Prepare Environment**:

```bash
git clone https://github.com/islamu-ngo/Event.git
cd Event

# Copy the configuration schema template
cp .env.example .env
```

3. **Generate Required Secrets**:

Generate cryptographically secure secrets and set them in your `.env` file:

```bash
# Generate Keycloak Blazor Client Secret
openssl rand -hex 32
# Paste as KEYCLOAK_BLAZOR_CLIENT_SECRET in .env

# Generate Database Passwords
openssl rand -base64 24
# Paste as DATABASE_PASSWORD and KEYCLOAK_DB_PASSWORD in .env
```

4. **Verify Configuration**:

Validate the syntax of your rendered Compose configuration:

```bash
docker compose config --quiet
```

---

## 3. Database Migration & Startup Sequence

> [!IMPORTANT]
> **Strict Startup Order**: In split deployments, `event-migrationservice` must exit successfully with code `0` before `event-api` and `event-ui` can serve traffic.

### Step 1: Run Database Migrations

Apply the application database schema, Data Protection key ring, privacy-erasure tables, and initial seeds:

```bash
docker compose run --rm event-migrationservice
```

Ensure the migration logs report successful completion. Migration execution is completely idempotent; re-running it during upgrades is safe.

### Step 2: Start the Stack

Start all core services in detached mode:

```bash
docker compose up -d
```

To start with optional service profiles (e.g., S3 storage and mailpit):

```bash
docker compose --profile storage --profile mail up -d
```

### Step 3: Check Health & Readiness

Confirm all containers are healthy:

```bash
docker compose ps
curl --fail http://localhost:7039/alive
curl --fail http://localhost:7039/health
```

---

## 4. First-Run Setup & Administrator Onboarding

Once containers are running, navigate to the web onboarding wizard or configure headless administrator bootstrapping:

### Option A: Interactive Setup Wizard
1. Access the web interface at `http://localhost:7002/setup`.
2. Retrieve the generated setup secret:
   ```bash
   docker compose exec event-api cat /app/data/setup-secret
   ```
3. Enter the secret and complete instance initialization (setting default instance name, primary tenant, and operator contact).
4. Once completed, the setup secret is permanently locked.

### Option B: Headless Automated Onboarding
To provision the initial administrator non-interactively without the UI wizard, configure the seven bootstrap variables in your `.env` file:

```env
INSTANCE_BOOTSTRAP_MODE=ConfiguredAdministrator
INSTANCE_BOOTSTRAP_ADMIN_PROVIDER=keycloak
INSTANCE_BOOTSTRAP_ADMIN_SUBJECT=admin-user-uuid-from-keycloak
INSTANCE_BOOTSTRAP_BINDING_GENERATION=1
INSTANCE_BOOTSTRAP_ADMIN_EMAIL=admin@example.org
INSTANCE_BOOTSTRAP_ADMIN_FIRST_NAME=System
INSTANCE_BOOTSTRAP_ADMIN_LAST_NAME=Admin
```

When the user logs in through Keycloak with the matching subject claim, administrative privileges are finalized automatically.

---

## 5. Reverse Proxy & TLS Configuration

In production, never expose application or internal database ports directly to the public internet. Terminate TLS at a reverse proxy and forward traffic to the Blazor BFF (`event-ui`) on port `7002`.

### Recommended Port Exposure Map

| Service | Internal Container Port | Exposed to Public Internet? | Reverse Proxy Routing |
|---|---|---|---|
| `event-ui` (BFF) | `7002` | **Yes (via Reverse Proxy)** | `https://events.example.org` |
| `event-api` | `7039` | Optional (internal to BFF) | Forwarded internally via BFF; or route `/api` |
| `keycloak` | `8080` | **Yes (via Reverse Proxy)** | `https://auth.example.org` |
| `postgres` | `5432` | **NO (Isolated network)** | None |
| `cerbos` | `3592` / `3593` | **NO (Internal gRPC)** | None |

### Reverse Proxy Recipes

#### Caddy (Recommended for Auto-HTTPS)
```caddy
events.example.org {
    reverse_proxy event-ui:7002
}

auth.example.org {
    reverse_proxy keycloak:8080 {
        header_up X-Forwarded-Proto {scheme}
        header_up X-Forwarded-Host {host}
    }
}
```

#### Traefik
Add Traefik labels directly to `event-ui` in your `docker-compose.yml`:
```yaml
services:
  event-ui:
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.event-ui.rule=Host(`events.example.org`)"
      - "traefik.http.routers.event-ui.entrypoints=websecure"
      - "traefik.http.routers.event-ui.tls.certresolver=letsencrypt"
      - "traefik.http.services.event-ui.loadbalancer.server.port=7002"
```

#### Nginx
```nginx
server {
    listen 443 ssl http2;
    server_name events.example.org;

    ssl_certificate /etc/letsencrypt/live/events.example.org/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/events.example.org/privkey.pem;

    location / {
        proxy_pass http://127.0.0.1:7002;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## 6. Volume Persistence & Backup Safeguards

Ensure the following named volumes are mounted to durable host storage:

```yaml
volumes:
  postgres_data:        # Primary database state
  keycloak_data:        # Keycloak user accounts & realm config
  local_storage_data:   # Uploaded media and attachments
  data_protection_keys: # ASP.NET Data Protection keys (preserves login sessions)
```

Refer to [Backup, Restore & Upgrade](../configuration-and-operations/backup-restore-upgrade.md) for automated backup routines.

---

## 7. Production Acceptance Checklist

Before opening your instance to users, verify:

- [ ] All containers report healthy via `docker compose ps`.
- [ ] `curl -f http://localhost:7039/health` returns status `Healthy`.
- [ ] TLS certificate is valid and redirects HTTP $\to$ HTTPS.
- [ ] Keycloak login completes and redirects back to the Blazor application.
- [ ] Public event listing is readable anonymously.
- [ ] Authenticated write action displays HAL affordances in the UI.
- [ ] Test email delivery passes via configured SMTP server.
- [ ] Database backups are automated and verified in an isolated test restore.
