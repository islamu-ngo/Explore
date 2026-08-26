// ABOUTME: Defines one-shot browser fragment extraction for admission recovery capabilities.
// ABOUTME: Requires immediate address-bar scrubbing and exposes no storage or logging surface.

namespace Explore.Blazor.Client.Contracts.Interop;

public interface IAdmissionRecoveryFragmentInterop : IAsyncDisposable
{
    ValueTask<string?> TakeCapabilityAsync(CancellationToken cancellationToken = default);
}
