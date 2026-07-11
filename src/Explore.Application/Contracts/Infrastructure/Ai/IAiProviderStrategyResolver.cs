// ABOUTME: Resolves the appropriate AI provider strategy from registered strategies.
// ABOUTME: Follows the IStrategyResolver convention used by event strategies.

namespace Explore.Application.Contracts.Infrastructure.Ai;

public interface IAiProviderStrategyResolver
{
    IAiProviderStrategy? Resolve(int providerId);
}
