// ABOUTME: Application contract for retained-authority-first global account erasure and replay.
// ABOUTME: Exposes deletion plus sequence-zero recovery without leaking persistence or provider details.

namespace Explore.Application.Contracts.Services;

public interface IGlobalLocationPrivacyErasureService
{
    Task EraseUserAsync(Guid userId, CancellationToken cancellationToken);
    Task ReplayPendingAsync(CancellationToken cancellationToken);
}
