// ABOUTME: Defines the Setup secret-write pre-dispatch synchronization barrier.
// ABOUTME: Exposes no payload, provider coordinate, or secret readback surface.

namespace Explore.Application.Contracts.Secrets;

public interface ISetupSecretBindingCommitBarrier
{
    Task WaitBeforeProviderDispatchAsync(
        CancellationToken cancellationToken);
}
