// ABOUTME: Defines the Blazor boundary for tenant reporting-intake governance.
// ABOUTME: Keeps components dependent on server-authored HAL policy instead of generated transport details.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Reporting;

public interface ITenantReportingIntakePolicyService
{
    Task<HalResourceOfTenantReportingIntakePolicyDto> GetAsync(
        CancellationToken cancellationToken = default);

    Task<BaseCommandResponseOfGuid> UpdateAsync(
        bool enabled,
        CancellationToken cancellationToken = default);
}
