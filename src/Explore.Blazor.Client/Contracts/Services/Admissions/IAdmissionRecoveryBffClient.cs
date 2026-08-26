// ABOUTME: Defines the typed low-level client for the same-origin admission recovery BFF bridge.
// ABOUTME: Keeps raw HTTP construction outside feature services while preserving explicit outcomes.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Http;

namespace Explore.Blazor.Client.Contracts.Services.Admissions;

public interface IAdmissionRecoveryBffClient
{
    Task<ApiResult<AdmissionTicketRecoveryDeliveryDto>> ConsumeAsync(
        string capability,
        CancellationToken cancellationToken = default);
}
