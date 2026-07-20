// ABOUTME: Defines the pre-traffic application boundary for retained platform privacy-erasure replay.
// ABOUTME: Keeps startup hosting concerns outside the privacy-erasure orchestration contract.

namespace Explore.Application.Contracts.Services;

public interface IPrivacyErasureReplayService
{
    Task ReplayAsync(CancellationToken cancellationToken);
}
