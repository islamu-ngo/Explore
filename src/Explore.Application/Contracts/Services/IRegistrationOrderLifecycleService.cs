// ABOUTME: Application contract for transaction-safe registration-order lifecycle recovery.
// ABOUTME: Lets background workers invoke lifecycle recovery without referencing persistence implementations.

using Explore.Application.DTOs.RegistrationOrders;

namespace Explore.Application.Contracts.Services;

public interface IRegistrationOrderLifecycleService
{
    Task<RegistrationOrderLifecycleResponse> RecoverExpiredHoldAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken);
}
