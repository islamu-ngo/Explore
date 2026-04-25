# Plan: Infrastructure Security Testing (Keycloak & Cerbos)

## CTO Verdict

This is the **right strategic direction** for enterprise-grade, self-hostable software: move critical security behavior out of mocks and into reproducible, containerized infrastructure. For a platform like ISLAMU Event, where Keycloak and Cerbos are not incidental dependencies but part of the product security model, this should become a **first-class integration-test layer**.

However, the current plan is still too “container startup focused.” For enterprise readiness, I would harden it around **test taxonomy, token realism, policy verification, image pinning, fixture boundaries, CI behavior, and failure-mode testing**.

My recommendation: **approve the plan, but revise it before implementation.**

---

## Executive Summary
This plan addresses the gap in security testing by introducing real, containerized instances of Keycloak and Cerbos into the test suites. Unlike traditional mock-only testing, we will maintain a **hybrid test taxonomy**: fast business logic tests remain mocked for speed, while a new "Security Integration" layer exercises the actual OIDC stack and Cerbos YAML policies. By utilizing `Testcontainers`, we eliminate dependencies on cloud instances and ensure the product is truly self-hostable and production-faithful.

## Current State Analysis
- **Authentication**: Bypassed via `TestAuthHandler` using `X-Test-Auth` headers.
- **Authorization**: Bypassed via `StubAuthorizationProvider`.
- **Infrastructure Assets**: 
    - `docker/keycloak/realm-export.json`: Contains the `ISLAMU` realm definition.
    - `cerbos/policies/`: Contains all resource policies.
- **E2E Tests**: Currently disabled in `Explore.Blazor.Client.E2ETests`.
- **Cloud Dependency**: Risk of tests accidentally hitting cloud endpoints if secrets/env vars are present in the environment.

## Proposed Future State

### 1. Test Taxonomy & Layering
We will split tests into three distinct categories:
- **FastIntegration**: Uses `TestAuthHandler` and fakes. No Docker required.
- **SecurityIntegration**: Uses real Keycloak + Cerbos containers. Real JWT bearer validation.
- **E2E**: Full stack (Playwright + Blazor + API + Keycloak + Cerbos + Postgres).

### 2. Token Realism
Security tests will no longer use mock handlers. They will use a `KeycloakTokenClient` helper to acquire real JWTs from the container and send them via standard `Authorization: Bearer` headers, proving the entire OIDC handshake and token validation pipeline works.

### 3. Policy-First Verification
A new `CerbosPolicyContractTests` suite will verify policy decisions independently of the API, using the Cerbos container directly.

### 4. Cloud Isolation Guardrails
Tests will run with `ASPNETCORE_ENVIRONMENT=Testing`. We will add a configuration assertion that fails the test suite if any endpoint contains "openislamu.org" or other cloud-specific strings.

## Implementation Phases

### Phase 0: Test Taxonomy & Infrastructure Assets
Add test categories and prepare deterministic test assets.

- **Task 0.1: Define Test Categories**: Add `FastIntegration`, `SecurityIntegration`, `PolicyContract`, and `E2E` traits/categories.
- **Task 0.2: Prepare Test Realm**: Create `docker/keycloak/ISLAMU-realm.test.json` with deterministic test users and clients.
- **Task 0.3: Pin Versions**: Identify and pin exact versions (e.g., Cerbos `0.51.0`, Keycloak `26.1.2`).

### Phase 1: Security Infrastructure Fixtures
Build the container lifecycle management.

- **Task 1.1: Create KeycloakContainerFixture**: 
    - Mounts `ISLAMU-realm.test.json` to `/opt/keycloak/data/import`.
    - Waits for `/realms/ISLAMU/.well-known/openid-configuration`.
    - Exposes a `KeycloakTokenClient` for acquiring real tokens.
- **Task 1.2: Create CerbosContainerFixture**:
    - Mounts `cerbos/policies` as read-only.
    - Implements health check wait strategy for gRPC.
- **Task 1.3: Compose SecurityInfrastructureFixture**: Combines Keycloak and Cerbos into a single `IAsyncLifetime` fixture.

### Phase 2: API Security Integration
Implement real-auth tests for the API.

- **Task 2.1: Implement SecurityWebApplicationFactory**: A specialized factory that uses real JwtBearer validation and real Cerbos gRPC clients.
- **Task 2.2: Add Negative Auth Tests**: Verify 401 for malformed tokens, 403 for missing roles, and 401 for wrong issuers.
- **Task 2.3: Policy Regression Tests**: Verify that changing a Cerbos policy correctly causes the corresponding API test to fail.

### Phase 3: Blazor BFF Security Verification
Harden the BFF security model.

- **Task 3.1: Verify OIDC Redirects**: Test that anonymous access to protected pages redirects to the local Keycloak container.
- **Task 3.2: Token Forwarding & XSRF**: Verify that the BFF correctly forwards tokens to the API and enforces XSRF protection.

### Phase 4: E2E Smoke Tests
Enable the full stack browser tests.

- **Task 4.1: Enable Playwright SmokeTests**: Run against the full containerized stack.
- **Task 4.2: Category Isolation**: Ensure E2E tests are excluded from standard "Fast" test runs in CI.

## Risk Assessment
- **Startup Latency**: Mitigated by shared fixtures and robust wait strategies.
- **Resource Exhaustion**: Pinning images and using slim alpine-based containers where possible.
- **Token Expiry**: Token lifespan in the test realm should be long enough for the test run but short enough to test expiry scenarios.

## Success Metrics
- [ ] 0% reliance on cloud secrets for security integration tests.
- [ ] 100% verification of Cerbos policies via real PDP checks.
- [ ] Detectable failure if OIDC metadata or issuer is misconfigured.

## Effort Estimate
- Total: 4-6 days of engineering effort.
- Complexity: High (Architecture Alignment).

## Potential Risks & Unknowns
The **BFF session state** in integration tests can be tricky. Mocking the cookie middleware while using a real OIDC provider requires careful configuration to ensure the `AccessTokenForwardingHandler` behaves exactly as it would in production.
