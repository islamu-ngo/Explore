// ABOUTME: Strategy for OpenAI-compatible AI providers using raw HTTP dispatch.
// ABOUTME: Delegates to OpenAiCompatibleChatProvider and reports configured_no_probe status.

using Explore.Application.Contracts.Infrastructure.Ai;

namespace Explore.Infrastructure.Ai;

public sealed class OpenAiCompatibleProviderStrategy : IAiProviderStrategy
{
    private readonly OpenAiCompatibleChatProvider _provider;

    public OpenAiCompatibleProviderStrategy(OpenAiCompatibleChatProvider provider)
    {
        _provider = provider;
    }

    public int ProviderId => AiProviderSettings.ProviderOpenAiCompatible;

    public bool SupportsProvider(int providerId) => providerId == ProviderId;

    public Task<AiChatProviderResult> SendAsync(AiChatPayload request, CancellationToken cancellationToken = default) =>
        _provider.SendAsync(request, cancellationToken);

    public Task<IReadOnlyList<AiModelDescriptor>> ListAvailableModelsAsync(CancellationToken cancellationToken = default) =>
        _provider.ListAvailableModelsAsync(cancellationToken);

    public AiProviderHealth CheckHealth(IReadOnlyDictionary<string, object> data) =>
        new(true, true, "configured_no_probe",
            "OpenAI-compatible AI provider settings are valid; network probing is deferred to the adapter.", data);
}
