# Deployment Tiers

> Infrastructure scaling guide for the ISLAMU Event platform.
> Each tier represents a progressively more resilient and scalable deployment model.

---

## Overview

The platform supports three deployment tiers, designed to grow with your community:

| Tier | Name | Target | Authorization | Database | Cerbos |
|------|------|--------|---------------|----------|--------|
| 1 | **Humble** | Small communities, development | LocalAuthorizationProvider | Single PostgreSQL | Optional |
| 2 | **Community** | Medium multi-tenant SaaS | Cerbos + Local fallback | PG primary + replica | 2 instances (HA) |
| 3 | **Ummah-Scale** | Large-scale, high-security | Cerbos HA cluster | Separate PG clusters | N instances |

---

## Tier 1 — Humble Self-Hoster

The simplest deployment. A single Docker Compose stack running all services on one machine.

```
┌──────────────────────────────────────────────────────┐
│                   Single Host                         │
│                                                      │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────┐   │
│  │ Blazor   │  │   API    │  │    Keycloak       │   │
│  │ (BFF)    │──│          │  │    (Auth)         │   │
│  └──────────┘  └────┬─────┘  └──────────────────┘   │
│                     │                                 │
│               ┌─────┴──────┐                          │
│               │ PostgreSQL │  (shared: app + KC)      │
│               └────────────┘                          │
│                                                      │
│  Cerbos: DISABLED (LocalAuthorizationProvider)       │
│  Storage: Local filesystem or MinIO (optional)       │
└──────────────────────────────────────────────────────┘
```

### Components

| Service | Configuration | Notes |
|---------|---------------|-------|
| PostgreSQL | Single instance, shared by app + Keycloak | `docker-compose up -d` |
| Keycloak | Single instance | Handles OIDC/OAuth 2.0 |
| API | Single instance | `AuthorizationProvider = "local"` |
| Blazor BFF | Single instance | Cookie-based auth, YARP proxy |
| Cerbos | **Not deployed** | Set `authorization.provider = local` in SystemSettings |
| MinIO | Optional (`--profile storage`) | Object storage for uploads |

### Authorization Behavior

- `RuntimeAuthorizationProvider` delegates to `LocalAuthorizationProvider` exclusively
- Local provider handles basic RBAC: role hierarchy checks, permission lookups from RolePermission table
- Covers ~95% of authorization decisions for simple deployments
- No network calls for auth — all decisions are in-process

### Resource Estimates

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| CPU | 2 cores | 4 cores |
| RAM | 4 GB | 8 GB |
| Disk | 20 GB | 50 GB |

### Upgrade Path to Tier 2

1. Enable Cerbos: `docker compose --profile authz up -d`
2. Initialize Cerbos schema: `psql -f cerbos/init/cerbos-schema.sql`
3. Set `authorization.provider = cerbos` in SystemSettings
4. RuntimeAuthorizationProvider will auto-switch to CerbosAuthorizationProvider with Local fallback

---

## Tier 2 — Community Hub

A production-ready deployment with high availability for authorization and database read scaling.

```
┌────────────────────────────────────────────────────────────┐
│                    Load Balancer (Caddy/Traefik)           │
│                         │                                   │
│         ┌───────────────┼───────────────┐                   │
│         ▼               ▼               ▼                   │
│  ┌──────────┐    ┌──────────┐    ┌──────────────┐          │
│  │ Blazor   │    │   API    │    │  Keycloak    │          │
│  │ (BFF)    │    │ (2 inst) │    │  (clustered) │          │
│  └──────────┘    └────┬─────┘    └──────────────┘          │
│                       │                                     │
│         ┌─────────────┼─────────────┐                       │
│         ▼             ▼             ▼                        │
│  ┌────────────┐ ┌──────────┐ ┌──────────────────┐          │
│  │ PostgreSQL │ │  Redis   │ │ Cerbos (2 inst)  │          │
│  │ Primary +  │ │ (cache)  │ │  PG overlay +    │          │
│  │  Replica   │ │          │ │  disk fallback   │          │
│  └────────────┘ └──────────┘ └──────────────────┘          │
└────────────────────────────────────────────────────────────┘
```

### Components

| Service | Configuration | Notes |
|---------|---------------|-------|
| PostgreSQL | Primary + 1 read replica | Read replica for queries, primary for writes |
| Cerbos | 2 instances, PG overlay driver | Both read from same policy store |
| Redis | Single instance | Session cache, distributed lock for PolicySync |
| API | 2 instances behind LB | Stateless, JWT validation |
| Blazor BFF | 1-2 instances | Sticky sessions for cookie auth |
| Keycloak | Clustered (2 nodes) | Shared KC database |

### Authorization Behavior

- `RuntimeAuthorizationProvider` delegates to `CerbosAuthorizationProvider` (primary)
- Circuit breaker (Polly): trips after 50% failure rate over 30s, breaks for 15s
- On Cerbos failure: automatic fallback to `LocalAuthorizationProvider`
- PolicySyncService pushes role-permission changes to both Cerbos instances via Admin API

### Resource Estimates

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| CPU | 8 cores (total) | 16 cores |
| RAM | 16 GB | 32 GB |
| Disk | 100 GB (SSD) | 200 GB (SSD) |

### Upgrade Path to Tier 3

1. Separate Cerbos database from application database
2. Add more Cerbos instances behind dedicated load balancer
3. Consider separate PostgreSQL clusters per concern
4. Enable audit log shipping to centralized logging (Loki/ELK)

---

## Tier 3 — Ummah-Scale

Enterprise-grade deployment with full isolation, zero blast radius between tenants, and horizontal scaling.

```
┌─────────────────────────────────────────────────────────────────┐
│                      Global Load Balancer                       │
│                              │                                   │
│              ┌───────────────┼───────────────┐                   │
│              ▼               ▼               ▼                   │
│       ┌──────────┐    ┌──────────┐    ┌──────────────┐          │
│       │ Blazor   │    │ API (N)  │    │  Keycloak    │          │
│       │ BFF (N)  │    │ Cluster  │    │  Cluster     │          │
│       └──────────┘    └────┬─────┘    └──────────────┘          │
│                            │                                     │
│    ┌───────────────────────┼───────────────────────┐            │
│    ▼                       ▼                       ▼             │
│ ┌──────────────┐    ┌──────────────┐    ┌──────────────────┐    │
│ │ App PG       │    │ Cerbos PG    │    │ Keycloak PG      │    │
│ │ Cluster      │    │ (Dedicated)  │    │ (Dedicated)      │    │
│ │ Primary + N  │    │ Primary + N  │    │ Primary + N      │    │
│ │ Replicas     │    │ Replicas     │    │ Replicas         │    │
│ └──────────────┘    └──────────────┘    └──────────────────┘    │
│                                                                  │
│              ┌──────────────────────────────┐                    │
│              │    Cerbos PDP Cluster (N)    │                    │
│              │    PG overlay driver         │                    │
│              │    Dedicated LB              │                    │
│              └──────────────────────────────┘                    │
│                                                                  │
│  ┌────────┐  ┌──────────┐  ┌────────────┐  ┌───────────────┐   │
│  │ Redis  │  │ Loki     │  │ Prometheus │  │ Grafana       │   │
│  │ Cluster│  │ (Logs)   │  │ (Metrics)  │  │ (Dashboards)  │   │
│  └────────┘  └──────────┘  └────────────┘  └───────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Components

| Service | Configuration | Notes |
|---------|---------------|-------|
| App PostgreSQL | Dedicated cluster, primary + N replicas | Application data only |
| Cerbos PostgreSQL | Dedicated cluster | Policy store, isolated from app data |
| Keycloak PostgreSQL | Dedicated cluster | Identity data, isolated |
| Cerbos PDP | N instances, dedicated LB | PG overlay driver, disk fallback |
| Redis | Cluster mode | Distributed cache, pub/sub for policy invalidation |
| Observability | Prometheus + Loki + Grafana | Full metrics, logs, dashboards |

### Zero Blast Radius

- Database failure in one cluster does not affect others
- Cerbos outage triggers circuit breaker → LocalAuthorizationProvider fallback
- Keycloak outage: existing sessions continue (cookie-based), new logins fail
- Each concern has independent scaling and backup schedules

### Authorization Behavior

- Cerbos cluster with N instances behind dedicated load balancer
- Each instance reads from dedicated Cerbos PostgreSQL cluster
- PolicySyncService broadcasts to all instances via Admin API
- Audit logs shipped to Loki for centralized analysis
- Circuit breaker per-instance with shared state via Redis

### Resource Estimates

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| CPU | 32 cores (total) | 64+ cores |
| RAM | 64 GB | 128+ GB |
| Disk | 500 GB (SSD) | 1 TB+ (NVMe) |
| Nodes | 3 | 5+ |

---

## Tier Comparison

| Feature | Tier 1 (Humble) | Tier 2 (Community) | Tier 3 (Ummah-Scale) |
|---------|-----------------|--------------------|-----------------------|
| Authorization | Local only | Cerbos + Local fallback | Cerbos HA cluster |
| Database HA | None | Primary + replica | Dedicated clusters |
| Cerbos instances | 0 | 2 | N |
| Blast radius | Full (single host) | Partial (shared DB) | Zero (isolated) |
| Policy sync | N/A | Admin API (2 targets) | Admin API (N targets) |
| Observability | Logs only | Logs + basic metrics | Full stack (Prometheus/Loki/Grafana) |
| Recovery time | Manual restart | Auto-failover (minutes) | Auto-failover (seconds) |
| Monthly cost est. | $10-30 | $100-300 | $500+ |

---

## Choosing Your Tier

- **Starting out?** → Tier 1. You can always upgrade later.
- **Running a community SaaS?** → Tier 2. Good balance of reliability and cost.
- **Enterprise / high-security?** → Tier 3. Full isolation and observability.

The platform's `RuntimeAuthorizationProvider` architecture means upgrading tiers is a configuration change, not a code change. Switch `authorization.provider` from `local` to `cerbos` and deploy Cerbos alongside your existing stack.
