// ABOUTME: Application contract for platform-wide account erasure and retained replay.
// ABOUTME: Exposes deletion plus sequence-zero recovery without leaking persistence or provider details.

namespace Explore.Application.Contracts.Services;

public interface IPrivacyErasureService
{
    Task EraseUserAsync(Guid userId, CancellationToken cancellationToken);
    Task ReplayPendingAsync(CancellationToken cancellationToken);
}
