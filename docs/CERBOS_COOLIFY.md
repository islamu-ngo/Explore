ABOUTME: Coolify deployment runbook for the external Cerbos PDP used by ISLAMU Event.
ABOUTME: Covers image pinning, PostgreSQL schema bootstrap, config mount, gRPC routing, and policy upload.

# Cerbos On Coolify

> **Audience:** Operators
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-07-03
> **Source Anchors:** `cerbos/config/.cerbos.yaml`, `cerbos/init/cerbos-schema.sql`, `cerbos/policies/`, `docker-compose.yml`, `docs/OPERATIONS.md`, `docs/AUTHORIZATION.md`

Use this runbook when Cerbos runs as a separate Coolify Docker Image application instead of the local Compose or Aspire `authz` profile. Local development and self-hosted Compose still use the repository-owned `cerbos/` folder directly:

- `cerbos/config/.cerbos.yaml` is the local Cerbos server config.
- `cerbos/init/cerbos-schema.sql` is the local PostgreSQL schema bootstrap.
- `cerbos/policies/` contains derived roles, resource policies, and `_schemas/`.
- `cerbos/tests/` contains native Cerbos policy tests.

## Image

Create a Coolify **Docker Image** application:

| Field | Value |
|---|---|
| Image | `ghcr.io/cerbos/cerbos` |
| Tag | `0.51.0` |

Do not use `latest` for Cerbos. Pin the deployed tag, validate policies against the same Cerbos/Cerbosctl version, and upgrade intentionally.

## Admin Password Hash

Cerbos server config uses a bcrypt password hash, not the plaintext Admin API password. Generate the hash locally:

```bash
mkpasswd -m bcrypt -R 10 | base64 -w0
```

Enter the plaintext Admin API password when prompted. Copy only the base64 output into `CERBOS_ADMIN_PASSWORD_HASH`. If your shell prompt is appended because `base64 -w0` prints no trailing newline, remove the prompt character. Keep the base64 text valid; the official Cerbos examples include base64 padding.

The plaintext password is still needed by operators and CI jobs that run `cerbosctl`. Store that plaintext value only in the deployment secret that performs policy upload, not in the Cerbos server config.

## Environment Variables

Set these Coolify environment variables on the Cerbos application:

| Key | Required | Purpose |
|---|---:|---|
| `CERBOS_ADMIN_USER` | Yes | Admin API username. |
| `CERBOS_ADMIN_PASSWORD_HASH` | Yes | Base64 bcrypt hash generated above. |
| `CERBOS_PG_URL` | Yes | Cerbos PostgreSQL store URL. |
| `CERBOS_CONFIG` | Yes | Path to mounted config file, normally `/config/conf.yaml`. |

Use this `CERBOS_PG_URL` shape:

```text
postgres://cerbos_user:{password}@{host}:{port}/{db_name}?sslmode=prefer&search_path=cerbos
```

Use the real Cerbos database name in `{db_name}`. Do not leave the database name as `postgres`.

## Config Mount

Mount one file into the Cerbos container:

| Coolify Field | Value |
|---|---|
| Source | `/data/coolify/applications/{application-id}/conf.yaml` |
| Destination | `/config/conf.yaml` |

File content:

```yaml
server:
  httpListenAddr: ":3592"
  grpcListenAddr: ":3593"

  adminAPI:
    enabled: true
    adminCredentials:
      username: ${CERBOS_ADMIN_USER}
      passwordHash: ${CERBOS_ADMIN_PASSWORD_HASH}

engine:
  lenientScopeSearch: true

compile:
  cacheDuration: 30s

storage:
  driver: "postgres"

  postgres:
    url: "${CERBOS_PG_URL}"
    connPool:
      maxLifeTime: 60m
      maxIdleTime: 45s
      maxOpen: 10
      maxIdle: 3
```

## PostgreSQL Bootstrap

Run this script in the Cerbos database before starting the Cerbos container. Replace only `{{CERBOS_DB_NAME}}` and `{{CERBOS_DB_PASSWORD}}`.

```sql
CREATE SCHEMA IF NOT EXISTS cerbos;

SET search_path TO cerbos;

CREATE TABLE IF NOT EXISTS policy (
    id bigint NOT NULL PRIMARY KEY,
    kind VARCHAR(128) NOT NULL,
    name VARCHAR(1024) NOT NULL,
    version VARCHAR(128) NOT NULL,
    scope VARCHAR(512),
    description TEXT,
    disabled BOOLEAN DEFAULT FALSE,
    definition BYTEA
);

CREATE TABLE IF NOT EXISTS policy_dependency (
    policy_id BIGINT,
    dependency_id BIGINT,
    PRIMARY KEY (policy_id, dependency_id),
    FOREIGN KEY (policy_id) REFERENCES cerbos.policy(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS policy_ancestor (
    policy_id BIGINT,
    ancestor_id BIGINT,
    PRIMARY KEY (policy_id, ancestor_id),
    FOREIGN KEY (policy_id) REFERENCES cerbos.policy(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS policy_revision (
    revision_id SERIAL PRIMARY KEY,
    action VARCHAR(64),
    id BIGINT,
    kind VARCHAR(128),
    name VARCHAR(1024),
    version VARCHAR(128),
    scope VARCHAR(512),
    description TEXT,
    disabled BOOLEAN,
    definition BYTEA,
    update_timestamp TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS attr_schema_defs (
    id VARCHAR(255) PRIMARY KEY,
    definition JSON
);

CREATE OR REPLACE FUNCTION process_policy_audit()
RETURNS TRIGGER AS $policy_audit$
BEGIN
    IF (TG_OP = 'DELETE') THEN
        INSERT INTO policy_revision(action, id, kind, name, version, scope, description, disabled, definition)
        VALUES('DELETE', OLD.id, OLD.kind, OLD.name, OLD.version, OLD.scope, OLD.description, OLD.disabled, OLD.definition);
    ELSIF (TG_OP = 'UPDATE') THEN
        INSERT INTO policy_revision(action, id, kind, name, version, scope, description, disabled, definition)
        VALUES('UPDATE', NEW.id, NEW.kind, NEW.name, NEW.version, NEW.scope, NEW.description, NEW.disabled, NEW.definition);
    ELSIF (TG_OP = 'INSERT') THEN
        INSERT INTO policy_revision(action, id, kind, name, version, scope, description, disabled, definition)
        VALUES('INSERT', NEW.id, NEW.kind, NEW.name, NEW.version, NEW.scope, NEW.description, NEW.disabled, NEW.definition);
    END IF;

    RETURN NULL;
END;
$policy_audit$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS policy_audit ON policy;

CREATE TRIGGER policy_audit
AFTER INSERT OR UPDATE OR DELETE ON policy
FOR EACH ROW EXECUTE PROCEDURE process_policy_audit();

DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'cerbos_user') THEN
        CREATE ROLE cerbos_user WITH LOGIN PASSWORD '{{CERBOS_DB_PASSWORD}}';
    ELSE
        ALTER ROLE cerbos_user WITH LOGIN PASSWORD '{{CERBOS_DB_PASSWORD}}';
    END IF;
END
$$;

GRANT CONNECT ON DATABASE "{{CERBOS_DB_NAME}}" TO cerbos_user;
GRANT USAGE ON SCHEMA cerbos TO cerbos_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON cerbos.policy, cerbos.policy_dependency, cerbos.policy_ancestor, cerbos.attr_schema_defs TO cerbos_user;
GRANT SELECT, INSERT, DELETE ON cerbos.policy_revision TO cerbos_user;
GRANT USAGE, SELECT ON cerbos.policy_revision_revision_id_seq TO cerbos_user;
```

`{{CERBOS_DB_NAME}}` must be the actual database created for Cerbos. Do not grant against a default maintenance database.

## Domains, Ports, And Traefik gRPC

Expose both Cerbos ports:

```text
3592,3593
```

Use separate public hostnames for HTTP/Admin API and gRPC/PDP traffic, for example:

```text
https://cerbos-api.example.org:3592,https://cerbos-grpc.example.org:3593
```

For the gRPC route, set Traefik's service scheme to `h2c` on the service generated for the `3593` route:

```text
traefik.http.services.https-{route-index}-{application-id}.loadbalancer.server.scheme=h2c
```

Replace `{route-index}` and `{application-id}` with the values Coolify generated for the gRPC route. Do not apply this label to the HTTP/Admin API route unless that route is also serving h2c.

DNS must point both hostnames at the Coolify server. Add an `A` record for IPv4 and an `AAAA` record when IPv6 is available. Keep TLS enabled at the proxy boundary.

## Smoke Test

Install `grpcurl`, then verify the gRPC endpoint:

```bash
grpcurl cerbos-grpc.example.org:443 list
```

If this fails with protocol errors, recheck that the gRPC route uses Traefik `h2c` and that the request is reaching Cerbos port `3593`.

## Upload Policies And Schemas

Run these commands from the repository root after the Coolify Cerbos app is healthy. Use the same Cerbosctl version as the deployed Cerbos server tag.

```bash
docker run --rm -it \
  -v "$PWD/cerbos/policies/_schemas:/schemas:ro" \
  ghcr.io/cerbos/cerbosctl:0.51.0 \
  --server=cerbos-grpc.example.org:443 \
  --username={admin-user} \
  --password="{admin-password}" \
  put schema -R /schemas
```

```bash
docker run --rm -it \
  -v "$PWD/cerbos/policies:/policies:ro" \
  ghcr.io/cerbos/cerbosctl:0.51.0 \
  --server=cerbos-grpc.example.org:443 \
  --username={admin-user} \
  --password="{admin-password}" \
  put policy -R /policies
```

Wrap the password in quotes when it contains spaces. Do not paste real passwords into issue comments, logs, screenshots, or support bundles.

## ISLAMU Event Configuration

Point ISLAMU Event at the Coolify Cerbos PDP only after the PDP is healthy and policies are uploaded:

| Key | Value |
|---|---|
| `Cerbos:GrpcEndpoint` / `CERBOS_GRPC_ENDPOINT` | `https://cerbos-grpc.example.org:443` |
| `Cerbos:UseTls` / `CERBOS_USE_TLS` | `true` |
| `Cerbos:PlaintextMode` / `CERBOS_PLAINTEXT_MODE` | `false` |
| `Cerbos:AdminApi:Endpoints:0` | Admin API endpoint for package sync/status when enabled. |
| `Cerbos:AdminApi:AdminUsername` | Admin API username. |
| `CERBOS_ADMIN_PASSWORD` | Canonical Environment-authority Admin API password; never mapped into application configuration. |

If the instance authorization provider is set to Cerbos and the PDP is unreachable, authorization fails closed. Switch back to local authorization only through an explicit operator action.

## Related

- [AUTHORIZATION.md](AUTHORIZATION.md) - provider behavior, fail-closed semantics, and policy upload notes.
- [OPERATIONS.md](OPERATIONS.md) - local Cerbos package operations and incident triage.
- [SELF_HOSTING.md](SELF_HOSTING.md) - Docker Compose profiles and required runtime keys.
- [SECRETS.md](SECRETS.md) - secret ownership and redaction rules.
