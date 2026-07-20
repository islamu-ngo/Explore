// ABOUTME: Defines the pre-traffic application boundary for retained location-erasure replay.
// ABOUTME: Keeps startup hosting concerns outside the privacy-erasure orchestration contract.

namespace Explore.Application.Contracts.Services;

public interface ILocationErasureReplayService
{
    Task ReplayAsync(CancellationToken cancellationToken);
}
