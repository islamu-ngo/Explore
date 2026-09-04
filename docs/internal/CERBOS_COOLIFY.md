<!-- ABOUTME: Architectural pointer and reference for external Cerbos PDP on Coolify. -->
<!-- ABOUTME: Directs self-hosters to the public runbook and developers to AUTHORIZATION.md. -->

# Cerbos on Coolify (Architecture & Operational Runbook Pointer)

> **Audience:** Operators | Developers
> **Status:** Consolidated
> **Owner:** Platform/Ops
> **Last Verified:** 2026-09-04
> **Source Anchors:** `cerbos/config/.cerbos.yaml`, `cerbos/policies/`, `docs/internal/AUTHORIZATION.md`, `docs/internal/AUTHORIZATION_PATTERNS.md`

## Authoritative Documentation Pointers

This document previously contained a duplicate operator deployment runbook for deploying Cerbos as a standalone Docker application on Coolify with Traefik gRPC proxying.

To prevent drift and adhere to the single-source-of-truth documentation architecture:

### 1. For Self-Hosters and Infrastructure Operators
The step-by-step Coolify deployment guide (including Docker image pinning, PostgreSQL storage backend, Traefik gRPC labels, admin password hashing, and `cerbosctl` policy synchronization) is maintained in the **public documentation**:

👉 **[Deploying Cerbos on Coolify with Traefik (Public Documentation)](../public/documentation/readme/self-hosting/coolify-cerbos-traefik.md)**

### 2. For Platform Developers and Contributors
The C# authorization pipeline, gRPC client interceptors, claim extraction, role derivation, and PDP decision caching are documented in the **internal architectural specifications**:

👉 **[Authorization Architecture & Invariants](AUTHORIZATION.md)**  
👉 **[Authorization Patterns & Policy Enforcement](AUTHORIZATION_PATTERNS.md)**  

Local development continues to use the root-level repository assets:
- `cerbos/config/.cerbos.yaml`: Local Cerbos server configuration
- `cerbos/init/cerbos-schema.sql`: PostgreSQL schema bootstrap
- `cerbos/policies/`: Policy definitions, derived roles, and schemas
- `cerbos/tests/`: Automated Cerbos policy test suites
