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

    Task<RegistrationOrderLifecycleResponseDto> SubmitAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<RegistrationOrderLifecycleResponseDto> SubmitAsync(
        Guid orderId,
        Guid tenantId,
        int? platformContributionBasisPoints,
        CancellationToken cancellationToken);

    Task<RegistrationOrderLifecycleResponseDto> ReadyForCheckoutAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<RegistrationOrderLifecycleResponseDto> FinalizeFreeAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<RegistrationOrderLifecycleResponseDto> CancelAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<RegistrationOrderLifecycleResponseDto> RecoverExpiredHoldAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken);
}
