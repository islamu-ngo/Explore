// ABOUTME: Application contract for asynchronous platform account erasure, status, and replay.
// ABOUTME: Keeps callers independent from receipt hashing, authority storage, and transaction details.

using Explore.Application.DTOs.PrivacyErasure;

namespace Explore.Application.Contracts.Services;

public interface IPrivacyErasureService
{
    Task<PrivacyErasureStartDto> EraseUserAsync(
        Guid userId,
        Guid intentId,
        CancellationToken cancellationToken);

    Task<Guid?> AuthenticateReceiptAsync(string receipt, CancellationToken cancellationToken);
    Task<PrivacyErasureStatusDto?> GetStatusAsync(Guid intentId, CancellationToken cancellationToken);
    Task ReplayPendingAsync(CancellationToken cancellationToken);
}
