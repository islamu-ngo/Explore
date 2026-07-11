// ABOUTME: Strategy interface for AI provider dispatch using lookup-backed integer provider IDs.
// ABOUTME: Each strategy handles one or more provider kinds from the ai_provider_kinds lookup table.

namespace Explore.Application.Contracts.Infrastructure.Ai;

public interface IAiProviderStrategy
{
    int ProviderId { get; }

    bool SupportsProvider(int providerId);

    Task<AiChatProviderResult> SendAsync(AiChatPayload request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiModelDescriptor>> ListAvailableModelsAsync(CancellationToken cancellationToken = default);

    AiProviderHealth CheckHealth(IReadOnlyDictionary<string, object> data);
}
