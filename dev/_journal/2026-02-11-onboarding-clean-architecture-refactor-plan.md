# Onboarding Features - Clean Architecture Refactoring Plan

**Date**: 2026-02-11
**Status**: 🚧 In Progress

## Problem Statement

The `TenantOnboarding` and `InstanceOnboarding` features violate Clean Architecture and CQRS best practices:

### Anti-Patterns Identified

1. **❌ `Common/` folders** - Non-standard folder structure not used elsewhere in codebase
2. **❌ Static helper classes** - Contains significant business logic (should be services)
3. **❌ Repository parameters** - Passes injected repositories to static methods (breaks DI)
4. **❌ No abstractions** - No interfaces, violates Dependency Inversion Principle
5. **❌ Poor testability** - Static methods are hard to mock/test
6. **❌ Inconsistency** - Doesn't match established patterns in Events, Organizations, etc.

### Current Structure (Anti-Pattern)

```
Features/TenantOnboarding/
├── Handlers/
│   ├── Commands/
│   └── Queries/
├── Requests/
│   ├── Commands/
│   └── Queries/
└── Common/                 ← ❌ Should not exist!
    └── TenantPolicySettingHelpers.cs  ← ❌ Static helper with business logic

Features/InstanceOnboarding/
├── Handlers/
│   ├── Commands/
│   └── Queries/
├── Requests/
│   ├── Commands/
│   └── Queries/
└── Common/                 ← ❌ Should not exist!
    ├── InstanceGovernanceSettingHelpers.cs  ← ❌ Static helper
    └── InstanceStorageSettingHelpers.cs      ← ❌ Static helper
```

### Problematic Code Example

```csharp
// ❌ ANTI-PATTERN: Handler passes repositories to static helper
public class GetTenantPolicySettingsQueryHandler
{
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ITenantSettingRepository _tenantSettingRepository;

    public async Task<TenantPolicySettingsDto> Handle(...)
    {
        // ❌ Passing injected dependencies to static method!
        return await TenantPolicySettingHelpers.ReadEffectiveTenantSettingsAsync(
            _systemSettingRepository,
            _tenantSettingRepository,
            _tenantRepository,
            _tenantContext.TenantId);
    }
}
```

## Solution - Clean Architecture Refactoring

### Target Structure (Clean Architecture)

```
Features/TenantOnboarding/
├── Handlers/
│   ├── Commands/
│   └── Queries/
├── Requests/
│   ├── Commands/
│   └── Queries/
└── Services/               ← ✅ New: Proper application services
    ├── ITenantPolicySettingService.cs
    └── TenantPolicySettingService.cs

Features/InstanceOnboarding/
├── Handlers/
│   ├── Commands/
│   └── Queries/
├── Requests/
│   ├── Commands/
│   └── Queries/
└── Services/               ← ✅ New: Proper application services
    ├── IInstanceGovernanceSettingService.cs
    ├── InstanceGovernanceSettingService.cs
    ├── IInstanceStorageSettingService.cs
    └── InstanceStorageSettingService.cs
```

### Refactored Code Pattern

```csharp
// ✅ CLEAN ARCHITECTURE: Service interface
public interface ITenantPolicySettingService
{
    Task<TenantPolicySettingsDto> ReadEffectiveTenantSettingsAsync(Guid tenantId);
    Task ApplyTenantSettingsAsync(Guid tenantId, Guid? userId, TenantPolicySettingsDto settings);
}

// ✅ CLEAN ARCHITECTURE: Service implementation with injected repositories
public class TenantPolicySettingService : ITenantPolicySettingService
{
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ITenantSettingRepository _tenantSettingRepository;
    private readonly ITenantRepository _tenantRepository;

    public TenantPolicySettingService(
        ISystemSettingRepository systemSettingRepository,
        ITenantSettingRepository tenantSettingRepository,
        ITenantRepository tenantRepository)
    {
        _systemSettingRepository = systemSettingRepository;
        _tenantSettingRepository = tenantSettingRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantPolicySettingsDto> ReadEffectiveTenantSettingsAsync(Guid tenantId)
    {
        // Business logic here (previously in static helper)
    }
}

// ✅ CLEAN ARCHITECTURE: Handler injects service
public class GetTenantPolicySettingsQueryHandler
{
    private readonly ITenantPolicySettingService _policySettingService;
    private readonly ITenantContext _tenantContext;

    public GetTenantPolicySettingsQueryHandler(
        ITenantPolicySettingService policySettingService,
        ITenantContext tenantContext)
    {
        _policySettingService = policySettingService;
        _tenantContext = tenantContext;
    }

    public async Task<TenantPolicySettingsDto> Handle(...)
    {
        // ✅ Clean: Just call the service!
        return await _policySettingService.ReadEffectiveTenantSettingsAsync(
            _tenantContext.TenantId);
    }
}
```

## Implementation Plan

### Phase 1: Create Service Interfaces and Implementations

#### 1.1 TenantOnboarding Services
- [x] Create `Features/TenantOnboarding/Services/ITenantPolicySettingService.cs`
- [ ] Create `Features/TenantOnboarding/Services/TenantPolicySettingService.cs`
- [ ] Move all logic from `TenantPolicySettingHelpers` to service
- [ ] Convert static methods to instance methods
- [ ] Keep all private helper methods (normalization, deserialization)

#### 1.2 InstanceOnboarding Services
- [ ] Create `Features/InstanceOnboarding/Services/IInstanceGovernanceSettingService.cs`
- [ ] Create `Features/InstanceOnboarding/Services/InstanceGovernanceSettingService.cs`
- [ ] Create `Features/InstanceOnboarding/Services/IInstanceStorageSettingService.cs`
- [ ] Create `Features/InstanceOnboarding/Services/InstanceStorageSettingService.cs`
- [ ] Move all logic from helper classes to services
- [ ] Share common deserialization methods via base class or separate utility

### Phase 2: Update Handlers to Use Services

#### 2.1 TenantOnboarding Handlers
- [ ] Update `GetTenantPolicySettingsQueryHandler.cs`
- [ ] Update `UpdateTenantPolicySettingsCommandHandler.cs`
- [ ] Update `CompleteTenantOnboardingCommandHandler.cs`
- [ ] Update `GetTenantOnboardingStatusQueryHandler.cs`
- [ ] Remove static helper usages
- [ ] Inject services via constructor

#### 2.2 InstanceOnboarding Handlers
- [ ] Update `GetInstanceGovernanceSettingsQueryHandler.cs`
- [ ] Update `UpdateInstanceGovernanceSettingsCommandHandler.cs`
- [ ] Update `GetInstanceStorageSettingsQueryHandler.cs`
- [ ] Update `UpdateInstanceStorageSettingsCommandHandler.cs`
- [ ] Update `CompleteInstanceOnboardingCommandHandler.cs`
- [ ] Update `GetInstanceOnboardingStatusQueryHandler.cs`

### Phase 3: Register Services in DI Container

- [ ] Add services to `ApplicationServicesRegistration.cs`
```csharp
// Onboarding Services
services.AddScoped<ITenantPolicySettingService, TenantPolicySettingService>();
services.AddScoped<IInstanceGovernanceSettingService, InstanceGovernanceSettingService>();
services.AddScoped<IInstanceStorageSettingService, InstanceStorageSettingService>();
```

### Phase 4: Delete Common/ Folders

- [ ] Delete `Features/TenantOnboarding/Common/TenantPolicySettingHelpers.cs`
- [ ] Delete `Features/TenantOnboarding/Common/` directory
- [ ] Delete `Features/InstanceOnboarding/Common/InstanceGovernanceSettingHelpers.cs`
- [ ] Delete `Features/InstanceOnboarding/Common/InstanceStorageSettingHelpers.cs`
- [ ] Delete `Features/InstanceOnboarding/Common/` directory

### Phase 5: Verification

- [ ] Build solution successfully
- [ ] Run all unit tests
- [ ] Run all integration tests
- [ ] Verify no references to `Common` namespace remain
- [ ] Verify no static helper usages remain

## Benefits of Refactoring

### ✅ Clean Architecture Compliance
- **Dependency Inversion**: Handlers depend on `IService` abstractions
- **Single Responsibility**: Services contain business logic, handlers orchestrate
- **Separation of Concerns**: Clear boundaries between layers

### ✅ Improved Testability
- Can mock `ITenantPolicySettingService` in handler tests
- Can test service logic independently
- No static method dependencies

### ✅ Consistency
- Matches established patterns in Events, Organizations, etc.
- Standard folder structure: `Requests/`, `Handlers/`, `Services/`
- Enterprise-grade maintainability

### ✅ Better Dependency Injection
- Services injected once, reused across handlers
- No passing repositories through static method parameters
- Proper constructor injection

### ✅ Extensibility
- Easy to add caching to services (decorator pattern)
- Easy to add logging/telemetry (AOP)
- Easy to swap implementations for testing

## Breaking Changes

**None** - This is purely an internal refactoring. Public API contracts remain unchanged:
- Same request/response DTOs
- Same command/query interfaces
- Same behavior and validation logic

## Estimated Effort

- **Phase 1**: 2 hours (service creation)
- **Phase 2**: 1 hour (handler updates)
- **Phase 3**: 15 minutes (DI registration)
- **Phase 4**: 5 minutes (cleanup)
- **Phase 5**: 30 minutes (verification)

**Total**: ~4 hours

## Related Documentation

- [ARCHITECTURE.md](../docs/ARCHITECTURE.md) - Clean Architecture layers
- [GOVERNANCE.md](../docs/GOVERNANCE.md) - CQRS patterns and conventions
- [QUICK_REFERENCE.md](../docs/QUICK_REFERENCE.md) - Coding standards

## Completion Criteria

- ✅ Zero `Common/` folders in Features
- ✅ Zero static helper classes with business logic
- ✅ All handlers inject service interfaces
- ✅ All services registered in DI container
- ✅ All tests passing
- ✅ Build successful with zero warnings related to refactoring
