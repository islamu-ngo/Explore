// ABOUTME: Provider-neutral model catalog contract for exposing safe AI model choices to bootstrap flows.
// ABOUTME: Implementations must not leak provider credentials, raw endpoint data, or provider-specific SDK objects.

namespace Explore.Application.Contracts.Infrastructure.Ai;

public interface IAiModelCatalog
{
    Task<IReadOnlyList<AiModelDescriptor>> ListAvailableModelsAsync(CancellationToken cancellationToken = default);
}
