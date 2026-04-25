# Context: Infrastructure Security Testing

## Key Files & Locations

### Core Security Definitions
- `docker/keycloak/realm-export.json`: Main realm export.
- `docker/keycloak/ISLAMU-realm.test.json`: (To be created) Deterministic test realm.
- `cerbos/policies/`: Directory containing all Cerbos YAML policies.

### Test Fixtures (To be Created/Updated)
- `Event.API.IntegrationTests/Fixtures/KeycloakContainerFixture.cs`: Keycloak lifecycle.
- `Event.API.IntegrationTests/Fixtures/CerbosContainerFixture.cs`: Cerbos lifecycle.
- `Event.API.IntegrationTests/Fixtures/SecurityInfrastructureFixture.cs`: Composite fixture for security infra.
- `Event.API.IntegrationTests/Fixtures/KeycloakTokenClient.cs`: Helper for acquiring real JWTs.
- `Event.API.IntegrationTests/Fixtures/SecurityWebApplicationFactory.cs`: Factory for real-auth tests.

### Legacy/Fast Fixtures (To be Maintained)
- `Event.API.IntegrationTests/Fixtures/TestAuthHandler.cs`: Kept for fast logic tests.
- `Event.API.IntegrationTests/Fixtures/StubAuthorizationProvider.cs`: Kept for fast logic tests.

## Decisions & Patterns

### 1. Hybrid Test Taxonomy
We distinguish between "Fast" and "Security" tests. Fast tests use mocks to ensure high developer velocity. Security tests use real containers to ensure production-faithful infrastructure wiring.

### 2. Real JWT Validation
Security-integrated tests must not use `TestAuthHandler`. They must use the standard ASP.NET Core `AddJwtBearer` authentication scheme pointing to the Keycloak container's OIDC metadata endpoint.

### 3. Pinned Versions
- Keycloak: `26.1.2`
- Cerbos: `0.51.0`
(Aligned with `docker-compose.yml` and enterprise stability requirements).

### 4. Wait Strategies
- Keycloak: Wait for `GET /realms/ISLAMU/.well-known/openid-configuration` to return 200.
- Cerbos: Wait for gRPC health check.

## Dependencies
- **Testcontainers.PostgreSql**: `4.10.0`
- **Testcontainers.Keycloak**: `4.11.0` (Verify support for `--import-realm`)
- **Cerbos.Sdk**: `1.9.1`

## Security Guardrail
All tests must enforce `ASPNETCORE_ENVIRONMENT=Testing`. The `CustomWebApplicationFactory` should assert that no configuration contains cloud-specific URLs (e.g., `openislamu.org`).

## Last Updated: 2026-04-25
