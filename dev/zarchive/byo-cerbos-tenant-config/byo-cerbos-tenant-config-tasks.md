# Tasks: Per-Tenant Cerbos Configuration — BYO + Instance Isolation

> Last Updated: 2026-02-23

## Phase 1: Domain & Constants
- [x] 1.1 Add `GovernanceSettingKeys.Cerbos` section with all setting keys + flat aliases
- [x] 1.2 Add `InfrastructureSecretSettingKeys.Cerbos` section for BYO credentials

## Phase 2: Application Layer — Contract & Model
- [x] 2.1 Create `ICerbosConfigResolver` interface (mirrors ISmtpConfigResolver)
- [x] 2.2 Create `CerbosConfiguration` model (POCO)
- [x] 2.3 Create `CerbosMode` and `CerbosFailureMode` enums

## Phase 3: Infrastructure Layer — CerbosConfigResolver
- [x] 3.1 Create `CerbosConfigResolver` (mirrors SmtpConfigResolver, 5-min cache, cascading settings)

## Phase 4: Infrastructure Layer — Authorization Refactor
- [x] 4.1 Refactor `RuntimeAuthorizationProvider` for BYO routing
- [x] 4.2 Add Safe-Mode to `FallbackAuthorizationService`
- [x] 4.3 Per-tenant HttpClient management for BYO endpoints
- [x] 4.4 `CerbosAuthorizationService` BYO-aware overloads

## Phase 5: DI Registration
- [x] 5.1 Register `CerbosConfigResolver` and related services in `InfrastructureServicesRegistration.cs`

## Phase 6: Testing & Verification
- [ ] 6.1 Unit tests for `CerbosConfigResolver`
- [ ] 6.2 Unit tests for `RuntimeAuthorizationProvider` BYO routing
- [ ] 6.3 Unit tests for Safe-Mode `FallbackAuthorizationService`
- [x] 6.4 Architecture tests pass
- [x] 6.5 Full build + all 7 test suites pass (1,494+ tests)
