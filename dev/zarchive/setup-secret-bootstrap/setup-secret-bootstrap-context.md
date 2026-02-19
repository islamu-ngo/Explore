# Setup Secret Bootstrap Mode - Context (v2)

> **v2**: Eliminated Discovery Mode per user feedback. Secret is ALWAYS required.

## SESSION PROGRESS (2026-02-15)

### ✅ ALL PHASES COMPLETE
- **Phase 1**: Core Infrastructure — `ISetupSecretProvider` interface, `SetupSecretProvider` singleton, DI registration, startup logging, DTO updates, query/command handler integration
- **Phase 2**: API Layer — `SetupSecretRequiredAttribute` filter, rate limiting (5/min), `validate-secret` endpoint, audit logging, attributes on 4 write endpoints
- **Phase 3**: Blazor BFF — YARP header sanitization, redirect logic (`/` ↔ `/setup`), model updates, `ValidateSecretAsync`, `Setup.razor` page (250 lines), `/auth/login` alias
- **Phase 4**: Safety — Live countdown timer (1-second ticks), instance start timestamp display on timeout
- **Phase 5**: Testing — 12 `SetupSecretProvider` tests, 5 `SetupSecretRequiredFilter` tests, 4 `InstanceOnboardingService` tests updated, full build + **713 tests pass, 0 failures**
- **Bug fix**: `GenerateCryptoRandomSecret()` used 24 bytes (exactly 32 Base64 chars) — after stripping `+`/`/`/`=`, string could be < 32 chars. Fixed by using 48 bytes for headroom.
- **Architectural correction**: Blazor BFF has NO access to Application/Infrastructure — validation moved to API endpoint, BFF startup logging removed

### Previous Session (2026-02-14)
- Full codebase analysis, v1 plan rejected (Discovery Mode), v2 plan created
- 3 refinements applied (timeout enforcement, Console.WriteLine, YARP sanitization)
- All dev docs created

---

## Key Design: Always-Required Secret

**CRITICAL**: There is NO open/discovery mode. The setup secret is mandatory in ALL cases:
- If `SETUP_SECRET` env var is set -> use that value
- If `SETUP_SECRET` env var is missing -> auto-generate 32-char crypto-random token, log to STDOUT
- Users can: shut down, set env var, restart -> secret persists
- Without env var: new random secret generated each restart

---

## Key Architecture: ISetupSecretProvider

```
ISetupSecretProvider (Application layer - interface)
  -> SetupSecretProvider (Infrastructure layer - Singleton implementation)
     Properties: IsSetupModeActive, IsFromEnvironmentVariable, IsTimedOut, InstanceStartedAt
     Methods: ValidateSecret(string?), Lock()

  Used by:
  -> SetupSecretRequiredAttribute (API TypeFilterAttribute on write endpoints)
  -> GetInstanceOnboardingStatusQueryHandler (populates DTO)
  -> CompleteInstanceOnboardingCommandHandler (calls Lock() on completion)
  -> /bff/validate-setup-secret (Blazor BFF minimal API endpoint)
  -> Startup logging (both API and Blazor Program.cs)
```

---

## Key Files

### Domain Layer (NO changes needed)
- **`Explore.Domain/InstanceBootstrapState.cs`** - Entity tracking first-run completion (IsCompleted, CompletedByUserId, SelectedDeploymentMode, CreatedAt)
- **`Explore.Domain/InstanceAdministrator.cs`** - Maps users to instance admin role (UserId, GrantedAt, GrantedBy)
- **`Explore.Domain/SystemSetting.cs`** - Key-value settings entity
- **`Explore.Domain/Constants/GovernanceSettingKeys.cs`** - Setting key constants

### Application Layer (new interface + DTO updates)
- **`Explore.Application/Contracts/Services/ISetupSecretProvider.cs`** - NEW interface
- **`Explore.Application/DTOs/Onboarding/InstanceOnboardingStatusDto.cs`** - ADD `IsSetupModeActive`, `SetupSecretFromEnvironment`, `InstanceStartedAt`
- **`Explore.Application/Contracts/Persistence/IInstanceBootstrapStateRepository.cs`** - Existing: `GetCurrent()`
- **`Explore.Application/Contracts/Persistence/IInstanceAdministratorRepository.cs`** - Existing: `HasAnyInstanceAdministrator()`, `IsUserInstanceAdmin(Guid)`, `GetByUserId(Guid)`
- **`Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetInstanceOnboardingStatusQueryHandler.cs`** - INJECT `ISetupSecretProvider`, populate new DTO fields
- **`Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`** - INJECT `ISetupSecretProvider`, call `Lock()` after completion
- **`Explore.Application/Serialization/ExploreJsonContext.cs`** - Verify AOT compat after DTO changes

### Infrastructure Layer (new service)
- **`Explore.Infrastructure/Services/SetupSecretProvider.cs`** - NEW Singleton: reads env var or generates crypto-random token, timing-safe validation
- **`Explore.Infrastructure/InfrastructureServicesRegistration.cs`** - Register singleton

### API Layer (new filter + modifications)
- **`Explore.API/Filters/SetupSecretRequiredAttribute.cs`** - NEW TypeFilterAttribute with inner IAsyncActionFilter
- **`Explore.API/Controllers/InstanceOnboardingController.cs`** - Apply `[SetupSecretRequired]` + `[EnableRateLimiting("SetupSecret")]` to write endpoints + audit logging
- **`Explore.API/Program.cs`** - Startup logging + `AddRateLimiter` with `SetupSecret` fixed window policy (5/min) + `app.UseRateLimiter()`

### Blazor BFF Layer (new endpoint + page + modifications)
- **`Explore.Blazor/Program.cs`** - `/bff/validate-setup-secret` endpoint, startup logging, redirect `/` -> `/setup`, YARP `X-Setup-Secret` forwarding
- **`Explore.Blazor.Client/Services/InstanceOnboardingService.cs`** - Update `InstanceOnboardingStatusModel` with new fields
- **`Explore.Blazor.Client/Pages/Setup.razor`** - NEW setup page (secret input -> validate -> sessionStorage -> login -> complete)
- **`Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs`** - Verify AOT compat

### Persistence Layer (NO changes needed)
- **`Explore.Persistence/Repositories/InstanceBootstrapStateRepository.cs`** - `GetCurrent()` impl
- **`Explore.Persistence/Repositories/InstanceAdministratorRepository.cs`** - `HasAnyInstanceAdministrator()` impl

### Tests
- **`Explore.Blazor.Client.Tests/Services/InstanceOnboardingServiceTests.cs`** - Update for new model fields
- NEW tests for `SetupSecretProvider` and `SetupSecretRequiredAttribute`

---

## Important Decisions (v2 + Refinements)

1. **Always-required secret** (NO Discovery Mode) - User explicitly rejected first-come-first-served
2. **Auto-generation when env var missing** - `RandomNumberGenerator.GetBytes(24)` for 32-char crypto-random token
3. **Defense in depth** - Secret validated at BOTH API level (TypeFilterAttribute) AND Blazor BFF level
4. **`X-Setup-Secret` HTTP header** for transmission (not query param, not cookie)
5. **`CryptographicOperations.FixedTimeEquals`** for timing-attack-safe comparison (prevents side-channel)
6. **Rate limiting** - 5 requests/minute fixed window on setup endpoints (`AddFixedWindowLimiter` + `[EnableRateLimiting]`)
7. **Audit logging** - `LogWarning` with userId + IP when instance is claimed
8. **`sessionStorage`** to persist validated secret across Keycloak OIDC redirect (cleared on tab close)
9. **Singleton lifecycle** for `SetupSecretProvider` with `Lock()` method
10. **Lazy bootstrap state check** via `IServiceProvider` scope to avoid DI ordering issues (singleton can't inject scoped)
11. **HTTP 410 Gone** when setup is locked (bootstrap complete) — semantically correct: resource permanently gone
12. **60-minute proximity timer** on unclaimed instances (safety feature)
13. **No database schema changes needed** - `InstanceBootstrapState.CreatedAt` already exists
14. **TypeFilterAttribute pattern** for `SetupSecretRequiredAttribute` (enables DI in action filter attributes)
15. **REFINEMENT 1: Timeout enforced at provider level** - `ValidateSecret()` returns false if `DateTime.UtcNow - InstanceStartedAt > 60 minutes`. Boot timestamp captured at constructor time (NOT from DB — `InstanceBootstrapState.CreatedAt` is set at completion, not boot). Prevents API-level bypass by malicious actors who skip the UI.
16. **REFINEMENT 2: `Console.WriteLine` for secret logging** - Guarantees visibility in all environments (bypasses Serilog log-level filters). Matches established Infisical bootstrap pattern in codebase (`InfisicalConfigurationProvider.cs` lines 60-123). `LogWarning` used for structured context alongside.
17. **REFINEMENT 3: YARP header sanitization** - `Remove("X-Setup-Secret")` BEFORE `Add()` in YARP `AddRequestTransform`. Prevents "Header Injection" where external user spoofs the secret header to bypass BFF validation.
18. **ARCHITECTURAL CORRECTION: Blazor BFF is a separate deployable** — No access to Application/Infrastructure layers. Secret validation and logging are API-only concerns. BFF communicates with API via HTTP/YARP only. Task 1.5 (BFF startup logging) removed. Task 3.1 moved from BFF endpoint to API endpoint (`POST /api/InstanceOnboarding/validate-secret`).

---

## Technical Constraints

- **Clean Architecture**: `ISetupSecretProvider` interface in Application, implementation in Infrastructure
- **Keycloak still required**: Secret is a *pre-auth gate*, not auth replacement. Users must still login via Keycloak.
- **No scripts**: Per CLAUDE.md — never write or execute script files
- **File-scoped namespaces**: Per codebase convention
- **ABOUTME comments**: Every new file needs two-line ABOUTME prefix
- **TDD**: Write failing tests first per CLAUDE.md rules
- **AOT serialization**: DTO changes must be reflected in both `ExploreJsonContext.cs` AND `AppJsonSerializerContext.cs`
- **Repositories return entities, never DTOs**: Map in handlers
- **Commands return BaseCommandResponse<Guid>**: Per project convention

---

## Pre-Existing LSP Errors (Not Our Problem)

- `Explore.ServiceDefaults/Extensions.cs` - Aspire component references resolve at build time
- `Explore.Blazor/Program.cs` - Same category of Aspire references

---

## Quick Resume

To continue this work:
1. Read this file for current state and key decisions
2. Read `setup-secret-bootstrap-tasks.md` for the implementation checklist
3. Read `setup-secret-bootstrap-plan.md` for detailed phase descriptions
4. All 3 refinements are applied to all 3 dev docs — plan is ready for user approval
5. Get user approval on final refined v2 plan
6. Start with Phase 1 (Core Infrastructure - SetupSecretProvider) — no dependencies
7. Follow TDD: write failing test -> implement -> verify
