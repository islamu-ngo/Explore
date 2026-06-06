// ABOUTME: Runtime AI provider selector that delegates to the configured Infrastructure adapter.
// ABOUTME: Fails closed when static provider settings are disabled, invalid, or unsupported.

using Explore.Application.Contracts.Infrastructure.Ai;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Ai;

public sealed class RuntimeAiChatProvider : IAiChatProvider, IAiModelCatalog
{
    private readonly IOptions<AiProviderSettings> _options;
    private readonly AiProviderSettingsValidator _validator;
    private readonly FakeAiChatProvider _fakeProvider;
    private readonly OpenAiCompatibleChatProvider _openAiCompatibleProvider;
    private readonly MicrosoftExtensionsAiChatProvider? _microsoftExtensionsProvider;

    public RuntimeAiChatProvider(
        IOptions<AiProviderSettings> options,
        AiProviderSettingsValidator validator,
        FakeAiChatProvider fakeProvider,
        OpenAiCompatibleChatProvider openAiCompatibleProvider,
        MicrosoftExtensionsAiChatProvider? microsoftExtensionsProvider = null)
    {
        _options = options;
        _validator = validator;
        _fakeProvider = fakeProvider;
        _openAiCompatibleProvider = openAiCompatibleProvider;
        _microsoftExtensionsProvider = microsoftExtensionsProvider;
    }

    public Task<IReadOnlyList<AiModelDescriptor>> ListAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryResolveProvider(out var provider, out _)
            || provider is not IAiModelCatalog modelCatalog)
        {
            return Task.FromResult<IReadOnlyList<AiModelDescriptor>>([]);
        }

        return modelCatalog.ListAvailableModelsAsync(cancellationToken);
    }

    public Task<AiChatProviderResult> SendAsync(AiChatPayload request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryResolveProvider(out var provider, out var failure))
        {
            return Task.FromResult(failure!);
        }

        return provider.SendAsync(request, cancellationToken);
    }

    private bool TryResolveProvider(out IAiChatProvider provider, out AiChatProviderResult? failure)
    {
        provider = _fakeProvider;
        failure = null;

        var settings = _options.Value;
        if (!settings.Enabled)
        {
            failure = AiChatProviderResult.Failure(
                "provider_disabled",
                "AI provider integration is disabled.");
            return false;
        }

        var validation = _validator.Validate(null, settings);
        if (!validation.Succeeded)
        {
            failure = AiChatProviderResult.Failure(
                "invalid_settings",
                "AI provider settings are invalid.");
            return false;
        }

        if (settings.Provider.Equals(AiProviderSettings.ProviderFake, StringComparison.OrdinalIgnoreCase))
        {
            provider = _fakeProvider;
            return true;
        }

        if (settings.Provider.Equals(AiProviderSettings.ProviderOpenAiCompatible, StringComparison.OrdinalIgnoreCase))
        {
            provider = _openAiCompatibleProvider;
            return true;
        }

        if (settings.Provider.Equals(AiProviderSettings.ProviderOpenAiSdk, StringComparison.OrdinalIgnoreCase)
            || settings.Provider.Equals(AiProviderSettings.ProviderAzureOpenAi, StringComparison.OrdinalIgnoreCase))
        {
            if (_microsoftExtensionsProvider is null)
            {
                failure = AiChatProviderResult.Failure(
                    "provider_not_configured",
                    "SDK-backed AI provider is not registered.");
                return false;
            }

            provider = _microsoftExtensionsProvider;
            return true;
        }

        failure = AiChatProviderResult.Failure(
            "provider_not_configured",
            "No runnable AI provider is configured.");
        return false;
    }
}
