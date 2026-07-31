// ABOUTME: Application contract for transaction-safe registration-order lifecycle recovery.
// ABOUTME: Lets background workers invoke lifecycle recovery without referencing persistence implementations.

using Explore.Application.DTOs.RegistrationOrders;

namespace Explore.Application.Contracts.Services;

public interface IRegistrationOrderLifecycleService
{
    Task<RegistrationOrderDto?> GetAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<RegistrationOrderLifecycleResponse> SubmitAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<RegistrationOrderLifecycleResponse> SubmitAsync(
        Guid orderId,
        Guid tenantId,
        int? platformContributionBasisPoints,
        CancellationToken cancellationToken);

    Task<RegistrationOrderLifecycleResponse> ReadyForCheckoutAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<RegistrationOrderLifecycleResponse> FinalizeFreeAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<RegistrationOrderLifecycleResponse> CancelAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<RegistrationOrderLifecycleResponse> RecoverExpiredHoldAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken);
}
