// ABOUTME: Repository contract for tenant onboarding completion state.
// ABOUTME: Supports startup checks and completion writes per tenant.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ITenantOnboardingStateRepository : IGenericRepository<TenantOnboardingState, Guid>
{
    Task<TenantOnboardingState?> GetByTenantId(Guid tenantId);
}
