# Setup Secret Bootstrap Mode - Task Checklist (v2)

> **v2**: Eliminated Discovery Mode. Secret is ALWAYS required.
> Auto-generated if env var missing. Enterprise hardening throughout.
> **ALL PHASES COMPLETE** — Build passes, 713 tests pass (247 + 439 + 27), 0 failures.

## Phase 1: Core Infrastructure - SetupSecretProvider ✅ COMPLETE

- [x] **1.1** Create `ISetupSecretProvider` interface
- [x] **1.2** Create `SetupSecretProvider` singleton implementation (BUG FIX: 48 bytes for crypto-random gen headroom)
- [x] **1.3** Register `SetupSecretProvider` as Singleton
- [x] **1.4** Add startup logging in API (`Console.WriteLine` box + `LogWarning`)
- [x] **1.5** ~~Add startup logging in Blazor BFF~~ **REMOVED** — Blazor BFF is separate deployable
- [x] **1.6** Update `InstanceOnboardingStatusDto` with new fields

## Phase 2: API Layer - Secret Validation & Rate Limiting ✅ COMPLETE

- [x] **2.1** Create `SetupSecretRequiredAttribute` action filter
- [x] **2.2** Register rate limiter policy (`SetupSecret`, 5/min fixed window)
- [x] **2.3** Apply `[SetupSecretRequired]` + `[EnableRateLimiting]` to 4 write endpoints
- [x] **2.4** Update `GetInstanceOnboardingStatusQueryHandler` with `ISetupSecretProvider`
- [x] **2.5** Add audit logging on instance claim (`LogWarning` with userId + IP)
- [x] **2.6** Call `Lock()` after completion in `CompleteInstanceOnboardingCommandHandler`
- [x] **2.7** Add `POST /api/InstanceOnboarding/validate-secret` API endpoint

## Phase 3: Blazor BFF Layer - Setup Route & UI ✅ COMPLETE

- [x] **3.1** ~~BFF validate endpoint~~ **MOVED TO API** as task 2.7
- [x] **3.2** YARP header sanitization (`Remove` before `Add` for `X-Setup-Secret`)
- [x] **3.3** Updated redirect logic: `/` → `/setup` (not completed) / `/setup` → `/` (completed)
- [x] **3.4** Updated `InstanceOnboardingStatusModel` + `ValidateSecretAsync` + `SetupSecretValidationResult`
- [x] **3.5** Created `Setup.razor` page (250 lines, full flow)
- [x] **3.6** Added `/auth/login` alias endpoint

## Phase 4: Safety & Polish ✅ COMPLETE

- [x] **4.1** 60-minute proximity timer (enforcement in provider, UI display in Setup.razor)
- [x] **4.2** System info: live countdown timer (1-second ticks via `System.Threading.Timer`) + instance start timestamp on timeout

## Phase 5: Testing & Verification ✅ COMPLETE

- [x] **5.1** `SetupSecretProviderTests.cs` — 12 tests (env var, auto-gen, validation, lock, bootstrap, timeout, logging)
- [x] **5.2** `SetupSecretRequiredFilterTests.cs` — 5 tests (410 gone, 403 wrong/missing/empty, pass-through)
- [x] **5.3** Updated `InstanceOnboardingServiceTests.cs` — 4 new tests (setup fields, validate valid/invalid/error)
- [x] **5.4** Full build: `dotnet build Explore.sln` — 0 errors
- [x] **5.5** All tests green: 247 + 439 + 27 = **713 tests, 0 failures**
