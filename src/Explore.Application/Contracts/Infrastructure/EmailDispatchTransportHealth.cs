// ABOUTME: Health result contract for EmailDispatch transport adapters.
// ABOUTME: Allows API health checks to report transport state without referencing broker libraries.

namespace Explore.Application.Contracts.Infrastructure;

public sealed record EmailDispatchTransportHealth(
    bool Enabled,
    bool Healthy,
    string Description,
    IReadOnlyDictionary<string, object> Data);
