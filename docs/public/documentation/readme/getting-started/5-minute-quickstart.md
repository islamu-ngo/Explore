---
description: Evaluate ISLAMU Event locally in under five minutes using Docker.
---

# 5-Minute Quickstart

The fastest way to evaluate ISLAMU Event on your local machine or testing server is using Docker. You do not need to install the .NET SDK or any compilers.

---

## Prerequisites

You only need:
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or Docker Engine (v24+)
- A web browser

---

## Option 1: Instant Single-Container Standalone (Fastest)

Run the all-in-one standalone image with an ephemeral or persistent volume:

```bash
docker run -d \
  --name islamu-event-quickstart \
  -p 8080:8080 \
  ghcr.io/islamu-ngo/event-standalone:latest
```

Wait a few seconds for database initialization, then open your browser:
- **URL**: [http://localhost:8080](http://localhost:8080)
- **Setup Wizard**: [http://localhost:8080/setup](http://localhost:8080/setup)

Retrieve the generated setup secret to begin the setup wizard:
```bash
docker cp islamu-event-quickstart:/app/data/setup-secret ./setup-secret
cat ./setup-secret
```

Enter the secret in the setup wizard, create your test organization, and explore the platform!

---

## Option 2: Full Split Topology with Docker Compose

If you want to evaluate the full stack with independent PostgreSQL and Keycloak services:

```bash
# 1. Clone the repository
git clone https://github.com/islamu-ngo/Event.git
cd Event

# 2. Copy the pre-configured environment template
cp .env.example .env

# 3. Apply database migrations
docker compose run --rm event-migrationservice

# 4. Start all services
docker compose up -d
```

### Accessing Endpoints

| Service | Endpoint |
|---|---|
| **Web Interface (BFF/UI)** | [http://localhost:7002](http://localhost:7002) |
| **REST API** | [http://localhost:7039](http://localhost:7039) |
| **Keycloak Administration** | [http://localhost:8080](http://localhost:8080) |
| **Mailpit (Local Email Capture)** | [http://localhost:8025](http://localhost:8025) |

---

## Next Steps

- Learn about platform architecture in [Architecture & Request Flows](architecture-and-request-flows.md).
- Plan your production deployment under [Self-Hosting](../self-hosting/).
- Understand platform roles and administrative controls in [Admin Hierarchy](../administration-and-branding/admin-hierarchy.md).
