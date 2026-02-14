# Setup Secret Bootstrap Mode - Task Checklist (v2)

> **v2**: Eliminated Discovery Mode. Secret is ALWAYS required.
> Auto-generated if env var missing. Enterprise hardening throughout.

## Phase 1: Core Infrastructure - SetupSecretProvider ⏳ NOT STARTED

- [ ] **1.1** Create `ISetupSecretProvider` interface
  - File: `Explore.Application/Contracts/Services/ISetupSecretProvider.cs`
  - Properties: `IsSetupModeActive`, `IsFromEnvironmentVariable`, `IsTimedOut`, `InstanceStartedAt`
  - Methods: `ValidateSecret(string?)`, `Lock()`
  - ABOUTME comment required
  - Acceptance: Interface compiles, follows Clean Architecture
- [ ] **1.2** Create `SetupSecretProvider` singleton implementation
  - File: `Explore.Infrastructure/Services/SetupSecretProvider.cs`
  - Read SETUP_SECRET env var via `IConfiguration["SETUP_SECRET"]` at construction
  - If missing: generate 32-char crypto-random token via `RandomNumberGenerator.GetBytes(24)` + base64
  - Capture `_instanceStartedAt = DateTime.UtcNow` at construction (boot timestamp for timeout)
  - **REFINEMENT 1**: `ValidateSecret` returns false if `DateTime.UtcNow - InstanceStartedAt > 60 minutes` (timeout enforced at provider level, not just UI)
  - `ValidateSecret` uses `CryptographicOperations.FixedTimeEquals` (timing-safe, prevents side-channel)
  - `Lock()` sets internal `_isLocked = true`
  - Lazy bootstrap check via `IServiceProvider` scope (singleton can't inject scoped repos)
  - `IsTimedOut` property: `DateTime.UtcNow - _instanceStartedAt > TimeSpan.FromMinutes(60)`
  - ABOUTME comment required
  - Acceptance: Correct mode resolution, timing-safe comparison, 32-char crypto-random generation, timeout enforcement in ValidateSecret
- [ ] **1.3** Register `SetupSecretProvider` as Singleton
  - File: `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
  - `services.AddSingleton<ISetupSecretProvider, SetupSecretProvider>()`
  - Acceptance: DI resolves correctly
- [ ] **1.4** Add startup logging in API
  - File: `Explore.API/Program.cs`
  - If env var present: `LogInformation` confirmation message
  - **REFINEMENT 2**: If auto-generated: `Console.WriteLine` for the secret box (guaranteed visibility, bypasses log filters). `LogWarning` for structured context. Matches Infisical bootstrap pattern (`InfisicalConfigurationProvider.cs` lines 60-123).
  - Internal `GetSecretForLogging()` method — only called at startup, never exposed via API
  - Acceptance: Correct log output in both cases, `Console.WriteLine` used for secret visibility
- [ ] **1.5** Add startup logging in Blazor BFF
  - File: `Explore.Blazor/Program.cs`
  - **REFINEMENT 2**: Same `Console.WriteLine` pattern as API (defense in depth — both need the secret)
  - Acceptance: Blazor console shows appropriate log with same visibility guarantees
- [ ] **1.6** Update `InstanceOnboardingStatusDto` with new fields
  - File: `Explore.Application/DTOs/Onboarding/InstanceOnboardingStatusDto.cs`
  - Add: `IsSetupModeActive` (bool), `SetupSecretFromEnvironment` (bool), `InstanceStartedAt` (DateTime?)
  - Verify `ExploreJsonContext.cs` AOT compat (should auto-detect if type already registered)
  - Verify `AppJsonSerializerContext.cs` AOT compat
  - Acceptance: DTO compiles, serialization works, no breaking changes

## Phase 2: API Layer - Secret Validation & Rate Limiting ⏳ NOT STARTED

- [ ] **2.1** Create `SetupSecretRequiredAttribute` action filter
  - File: `Explore.API/Filters/SetupSecretRequiredAttribute.cs`
  - Pattern: `TypeFilterAttribute` with inner `IAsyncActionFilter` (enables DI resolution per ASP.NET Core docs)
  - Resolve `ISetupSecretProvider` from DI
  - If `!IsSetupModeActive` -> return 410 Gone `{ error: "Setup already completed" }`
  - Read `X-Setup-Secret` header from request
  - If `!ValidateSecret(header)` -> return 403 Forbidden `{ error: "Invalid setup secret" }`
  - Pass through on valid secret
  - ABOUTME comment required
  - Acceptance: Filter compiles, applies as `[SetupSecretRequired]`
- [ ] **2.2** Register rate limiter policy
  - File: `Explore.API/Program.cs`
  - `AddRateLimiter` with `AddFixedWindowLimiter("SetupSecret", ...)`: 5 requests/minute, `QueueLimit = 0`
  - Add `app.UseRateLimiter()` after `UseRouting`, before `UseAuthorization`
  - Acceptance: Rate limiter registered, returns 429 on exceed
- [ ] **2.3** Apply `[SetupSecretRequired]` + `[EnableRateLimiting]` to write endpoints
  - File: `Explore.API/Controllers/InstanceOnboardingController.cs`
  - Apply to: POST /complete, PUT /settings, PUT /storage-settings, POST /test-storage
  - Do NOT apply to: GET /status, GET /settings
  - Acceptance: Write endpoints reject invalid secrets (403), locked (410), rate limited (429)
- [ ] **2.4** Update `GetInstanceOnboardingStatusQueryHandler`
  - File: `Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetInstanceOnboardingStatusQueryHandler.cs`
  - Inject `ISetupSecretProvider`
  - Populate: `IsSetupModeActive`, `SetupSecretFromEnvironment`, `InstanceStartedAt` (from `bootstrapState?.CreatedAt`)
  - Acceptance: GET /status returns new fields correctly
- [ ] **2.5** Add audit logging on instance claim
  - File: `Explore.API/Controllers/InstanceOnboardingController.cs`
  - `LogWarning("Instance claimed by admin (userId: {UserId}) from IP: {IpAddress}. Bootstrap mode disabled.", userId, HttpContext.Connection.RemoteIpAddress)`
  - Acceptance: Audit log entry visible with userId + IP at Warning level
- [ ] **2.6** Call `Lock()` on `SetupSecretProvider` after completion
  - File: `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`
  - Inject `ISetupSecretProvider`, call `Lock()` after successful completion
  - Acceptance: SetupSecretProvider transitions to locked state

## Phase 3: Blazor BFF Layer - Setup Route & UI ⏳ NOT STARTED

- [ ] **3.1** Add `/bff/validate-setup-secret` endpoint
  - File: `Explore.Blazor/Program.cs`
  - POST minimal API endpoint: validate secret via `ISetupSecretProvider.ValidateSecret()`
  - Return `{ valid: bool }` or 410 if locked
  - Apply `.RequireRateLimiting("SetupSecret")`
  - Acceptance: Endpoint works, returns correct responses
- [ ] **3.2** Forward `X-Setup-Secret` header through YARP proxy (with header injection prevention)
  - File: `Explore.Blazor/Program.cs` YARP transform section (lines 357-375)
  - **REFINEMENT 3**: FIRST `Remove("X-Setup-Secret")` from proxy request, THEN `Add()` from incoming request
  - Prevents "Header Injection" where external user spoofs `X-Setup-Secret` to bypass BFF validation
  - Pattern: `transformContext.ProxyRequest.Headers.Remove("X-Setup-Secret");` then conditionally add
  - Acceptance: API receives header when proxied through BFF. Externally-injected headers are stripped before forwarding.
- [ ] **3.3** Update startup redirect: `/` -> `/setup`
  - File: `Explore.Blazor/Program.cs` (currently ~line 522)
  - Change redirect target from `/startup` to `/setup`
  - Add guard: if bootstrap complete and path is `/setup` -> redirect to `/`
  - Preserve `returnUrl` query parameter through redirects
  - Acceptance: Root routes to `/setup`, `/setup` redirects after completion
- [ ] **3.4** Update `InstanceOnboardingStatusModel` in Blazor Client
  - File: `Explore.Blazor.Client/Services/InstanceOnboardingService.cs`
  - Add: `IsSetupModeActive`, `SetupSecretFromEnvironment`, `InstanceStartedAt`
  - Acceptance: Model mirrors DTO
- [ ] **3.5** Create `Setup.razor` page
  - File: `Explore.Blazor.Client/Pages/Setup.razor`
  - Route: `/setup`
  - Flow: check status -> show secret input -> validate via BFF -> store in `sessionStorage` -> login via Keycloak -> retrieve from `sessionStorage` -> complete onboarding
  - Help text: "Check the `SETUP_SECRET` environment variable, or check the API server logs."
  - Forward `X-Setup-Secret` header on API calls
  - Acceptance: Full flow works end-to-end

## Phase 4: Safety & Polish ⏳ NOT STARTED

- [ ] **4.1** Add 60-minute proximity timer (UI flag only — enforcement already in provider per Refinement 1)
  - File: `Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetInstanceOnboardingStatusQueryHandler.cs`
  - Populate `SetupTimedOut` from `_setupSecretProvider.IsTimedOut` (provider already enforces timeout in `ValidateSecret`)
  - Add `SetupTimedOut` (bool) to DTO + both serialization contexts
  - Setup page shows: "Setup window has expired. Please restart the application."
  - Acceptance: Timeout detected and shown after 60 min. Provider-level enforcement already blocks secret validation.
- [ ] **4.2** Show system info on setup page
  - File: `Explore.Blazor.Client/Pages/Setup.razor`
  - Display "Instance started at {time} UTC" + remaining setup time countdown
  - Acceptance: Timestamp and countdown visible

## Phase 5: Testing & Verification ⏳ NOT STARTED

- [ ] **5.1** Write unit tests for `SetupSecretProvider`
  - Test: env var used when present (`IsFromEnvironmentVariable == true`)
  - Test: auto-generated secret is 32 chars
  - Test: `ValidateSecret` returns true for correct secret
  - Test: `ValidateSecret` returns false for wrong/null secret
  - Test: `ValidateSecret` returns false after 60-minute timeout (Refinement 1)
  - Test: `IsTimedOut` returns true after 60 minutes
  - Test: `Lock()` transitions to locked mode (`IsSetupModeActive == false`)
  - Test: lazy bootstrap check (DB already completed -> locked from start)
- [ ] **5.2** Write unit tests for `SetupSecretRequiredAttribute`
  - Test: 410 when locked (`!IsSetupModeActive`)
  - Test: 403 when `X-Setup-Secret` header missing
  - Test: 403 when `X-Setup-Secret` header has wrong value
  - Test: pass-through when secret is correct
- [ ] **5.3** Update existing `InstanceOnboardingService` tests
  - Update for new DTO/model fields
  - Acceptance: Existing tests still pass
- [ ] **5.4** Full build + test suite pass
  - `dotnet build --configuration Release --verbosity quiet`
  - Run all test projects individually per CLAUDE.md
  - Acceptance: Zero regressions
- [ ] **5.5** Architecture tests pass
  - `dotnet test --project Event.Architecture.Tests --configuration Release --verbosity quiet`
  - Acceptance: No Clean Architecture violations
