---
description: Run the combined single-container topology with durable SQLite storage.
---

# Docker Standalone

The standalone image is the smallest supported operational topology. It combines the application into one non-root container and persists application, Data Protection, and privacy-erasure authority state in one named volume.

## Constraints

* One application replica.
* SQLite for application and privacy-erasure authority data.
* Local durable storage under `/app/data`.
* Initial image target: `linux/amd64`.
* No Kubernetes, Helm, or initial `linux/arm64` packaging.

## Build and run

```bash
docker build -t islamu/event-standalone -f src/Event.Standalone/Dockerfile .
docker volume create event_standalone_data
docker run --rm --name islamu-event-standalone \
  --env-file .env \
  --mount source=event_standalone_data,target=/app/data \
  -p 8080:8080 \
  islamu/event-standalone
```

Review the environment file for this topology. Do not pass the split deployment example unchanged and assume every value applies.

The container applies application, Data Protection, and privacy-authority migrations before binding port `8080`. Default durable files are:

* `/app/data/islamu_event.db`;
* `/app/data/privacy_erasure_authority.db`.

## First-run setup

Check readiness:

```bash
curl --fail http://localhost:8080/health
```

If `SETUP_SECRET` was not supplied, retrieve the generated file without printing the secret:

```bash
docker cp islamu-event-standalone:/app/data/setup-secret ./setup-secret
chmod 600 ./setup-secret
```

Complete onboarding at `http://localhost:8080/setup`, then remove the host copy. Never persist the setup secret in shell history, source control, screenshots, or support bundles.

## Backup and recovery

Back up the named volume and prove it can be restored into an isolated container. Preserve both SQLite files: restoring only the primary application database can resurrect data that the separate privacy-erasure authority fenced.

A standalone deployment is not ready until restart preserves data, migrations complete, Keycloak authentication and authorization work, and `/health` remains free of private paths and credentials.
