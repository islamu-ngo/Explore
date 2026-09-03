---
description: Practical troubleshooting guide and operational symptom matrix for self-hosters.
---

# Troubleshooting & Operational Health

This guide provides practical resolutions for common issues encountered during deployment, first-run onboarding, database migrations, and authentication.

---

## 1. Quick Diagnostic Checklist

When diagnosing a failure, proceed in this order:

1. **Check Container Status**:
   ```bash
   docker compose ps
   ```
2. **Inspect Process Logs**:
   ```bash
   docker compose logs --tail=100 -f event-api
   docker compose logs --tail=100 -f event-ui
   ```
3. **Verify Liveness and Health Endpoints**:
   ```bash
   curl -i http://localhost:7039/alive
   curl -i http://localhost:7039/health
   ```
4. **Validate Rendered Configuration**:
   ```bash
   docker compose config --quiet
   ```

---

## 2. Common Symptoms & Repairs

### A. Startup & Configuration Failures

| Symptom | Probable Cause | Repair Action |
|---|---|---|
| **API stops with `Instance:OperatorIdentity validation failure`** | Required legal identity fields in `.env` are missing or empty. | Ensure all `INSTANCE__OPERATORIDENTITY__*` variables are populated (e.g., `PUBLICNAME`, `LEGALNAME`, `PUBLICCONTACTEMAIL`, `TERMSURL`, `PRIVACYURL`). |
| **API exits immediately with `SecretProvider configuration invalid`** | Selected secret provider cannot be loaded or is misconfigured. | If using `.env`, set `SECRET_PROVIDER=Environment`. If using Infisical, verify `INFISICAL_CLIENT_ID` and `INFISICAL_CLIENT_SECRET`. |
| **BFF routes to `/setup` instead of the application** | The instance is not yet onboarded or onboarding is still pending. | Complete setup at `/setup` with the setup secret, or sign in as the configured administrator. |

---

### B. Headless Onboarding & Bootstrap Issues

| Symptom | Probable Cause | Repair Action |
|---|---|---|
| **Startup fails with `Bootstrap reason code: GenerationDrift`** | `INSTANCE_BOOTSTRAP_BINDING_GENERATION` was edited to a lower or unchanged value while configuration changed. | Increment `INSTANCE_BOOTSTRAP_BINDING_GENERATION` to a strictly higher positive integer (e.g., from `1` to `2`). |
| **Admin signs in but receives no administrative privileges** | The user ID / subject claim issued by Keycloak does not match `INSTANCE_BOOTSTRAP_ADMIN_SUBJECT`. | Check the Keycloak admin console for the user's exact UUID (`sub` claim) and update `INSTANCE_BOOTSTRAP_ADMIN_SUBJECT` in `.env`. |
| **Onboarding remains in "Pending" status** | Expected behavior. Pending is a healthy state until the configured admin signs in for the first time. | Sign in through the web UI using the designated admin account to finalize onboarding. |

---

### C. Authentication & Keycloak Redirect Errors

| Symptom | Probable Cause | Repair Action |
|---|---|---|
| **Keycloak reports `Invalid parameter: redirect_uri`** | The public URL of the Blazor UI is not allowed in Keycloak client settings. | In Keycloak Admin $\to$ Clients $\to$ `event-blazor`, ensure **Valid Redirect URIs** contains `https://events.example.org/*` and **Web Origins** contains `https://events.example.org`. |
| **Infinite redirect loop between UI and Keycloak** | Reverse proxy is not forwarding `X-Forwarded-Proto: https` or `X-Forwarded-Host`. | In your reverse proxy (Caddy/Traefik/Nginx), ensure headers `X-Forwarded-Proto: https` and `X-Forwarded-Host: $host` are passed to Keycloak. |
| **Login succeeds but API returns `401 Unauthorized`** | Keycloak client secret mismatch between UI and Keycloak server. | Ensure `KEYCLOAK_BLAZOR_CLIENT_SECRET` in `.env` exactly matches the secret generated in Keycloak client credentials. |

---

### D. Database & Migration Failures

| Symptom | Probable Cause | Repair Action |
|---|---|---|
| **`event-migrationservice` exits with connection refused** | PostgreSQL is not ready or database credentials are incorrect. | Check PostgreSQL logs (`docker compose logs postgres`). Verify `DATABASE_HOST`, `DATABASE_NAME`, and `DATABASE_MIGRATOR_PASSWORD`. |
| **API logs `PendingModelChangesWarning` or migration lock** | The API started before migrations completed. | Always run `docker compose run --rm event-migrationservice` before launching `event-api`. |
| **PostgreSQL reports `password authentication failed`** | Mismatch between `.env` password and the initial PostgreSQL volume password. | If changing passwords after initial volume creation, update the user password directly via `ALTER USER postgres WITH PASSWORD 'newpass';` inside PostgreSQL. |

---

### E. Multi-Tenancy & Custom Domains

| Symptom | Probable Cause | Repair Action |
|---|---|---|
| **Request returns `404 Not Found` with `tenant_not_found`** | The incoming hostname does not match any registered tenant subdomain or custom domain. | Ensure the tenant exists in the Instance Console (`/admin/instance/tenants`) and that the custom domain is mapped and verified. |
| **Admin Console cannot be reached** | Instance is running in single-tenant mode or admin hostname is not recognized. | Multi-tenant console requires `DEPLOYMENT_MODE=multi_tenant`. If using dedicated admin hosts, ensure `BFF_ADMIN_HOSTS` matches your domain. |

---

### F. Authorization & Cerbos Denials

| Symptom | Probable Cause | Repair Action |
|---|---|---|
| **All authenticated actions return `403 Forbidden`** | Cerbos PDP is unreachable or policies have not been uploaded. | Verify Cerbos status via `curl http://cerbos:3592/_cerbos/health`. Run policy upload via `cerbosctl`. Note that Cerbos **fails closed** during outages. |
| **`grpcurl` or API cannot connect to Cerbos on port 3593** | Traefik is not configured for HTTP/2 cleartext (`h2c`). | Ensure Traefik loadbalancer scheme is set to `h2c` for the gRPC service port `3593`. |

---

## 3. Safe Health Checks & Telemetry

The API provides sanitized health endpoints that never leak passwords, connection strings, or PII:

- **Liveness**: `GET /alive` $\to$ Returns `200 OK` if the process is running.
- **Readiness**: `GET /health` $\to$ Evaluates database, Keycloak, storage, and Cerbos connections.

Example Healthy Response:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.024",
  "entries": {
    "database": { "status": "Healthy" },
    "keycloak": { "status": "Healthy" },
    "storage": { "status": "Healthy" }
  }
}
```
