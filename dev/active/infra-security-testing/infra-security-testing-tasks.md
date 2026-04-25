# Tasks: Infrastructure Security Testing

## Phase 0: Test Taxonomy & Infrastructure Assets
- [ ] **0.1** Define test categories/traits (`Fast`, `Security`, `E2E`)
- [ ] **0.2** Create `docker/keycloak/ISLAMU-realm.test.json` with deterministic test data
- [ ] **0.3** Pin `Testcontainers.Keycloak` to `4.11.0` and Cerbos to `0.51.0` in `Directory.Packages.props`
- [ ] **0.4** Create `CerbosPolicyContractTests.cs` to verify policies independently

## Phase 1: Security Infrastructure Fixtures
- [ ] **1.1** Implement `KeycloakContainerFixture.cs` with OIDC metadata wait strategy
- [ ] **1.2** Implement `CerbosContainerFixture.cs` with gRPC health check wait strategy
- [ ] **1.3** Implement `SecurityInfrastructureFixture.cs` (composite fixture)
- [ ] **1.4** Implement `KeycloakTokenClient.cs` for acquiring real tokens from the container

## Phase 2: API Security Integration
- [ ] **2.1** Create `SecurityWebApplicationFactory.cs` (uses real JWT validation)
- [ ] **2.2** Implement `SecurityIntegration` test suite covering:
    - [ ] Happy path with real tokens
    - [ ] Negative: No token (401)
    - [ ] Negative: Malformed token (401)
    - [ ] Negative: Wrong issuer/audience (401)
    - [ ] Negative: Valid token, missing role (403)
- [ ] **2.3** Add configuration guardrail to fail if cloud endpoints are detected

## Phase 3: Blazor BFF Security Verification
- [ ] **3.1** Update `BlazorBffWebApplicationFactory` to support real OIDC flow
- [ ] **3.2** Implement tests for:
    - [ ] OIDC challenge redirect to container
    - [ ] Callback and cookie issuance
    - [ ] Access token forwarding to API
    - [ ] WASM-side token absence verification

## Phase 4: E2E Smoke Tests
- [ ] **4.1** Create `E2EInfrastructureFixture` (Postgres + Keycloak + Cerbos + API + BFF)
- [ ] **4.2** Enable `SmokeTests.cs` in `Explore.Blazor.Client.E2ETests`
- [ ] **4.3** Verify full login/logout flow in a real browser via Playwright

## Phase 5: Documentation & Cleanup
- [ ] **5.1** Document how to run different test categories locally and in CI
- [ ] **5.2** Remove any redundant code or accidental cloud references

## Last Updated: 2026-04-25
