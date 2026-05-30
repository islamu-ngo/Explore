// ABOUTME: Resolves tenant-governed AI assistant availability for Application handlers.
// ABOUTME: Keeps send/history handlers fail-closed before provider calls or persistence mutations.

using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Settings.Groups;

namespace Explore.Application.Features.AiAssistant;

internal static class AiAssistantAvailability
{
    public static string NormalizeProvider(string? provider)
        => string.IsNullOrWhiteSpace(provider)
            ? AiProviderDefaults.ProviderNone
            : provider.Trim().ToLowerInvariant();

    public static string? ResolveDisabledReason(AiAssistantSettingGroup settings)
    {
        var provider = NormalizeProvider(settings.Provider);

        if (!settings.Enabled)
            return "disabled";

        if (provider == AiProviderDefaults.ProviderNone)
            return "provider_not_configured";

        if (provider != AiProviderDefaults.ProviderFake && provider != AiProviderDefaults.ProviderOpenAiCompatible)
            return "provider_unsupported";

        if (provider == AiProviderDefaults.ProviderOpenAiCompatible)
        {
            if (string.IsNullOrWhiteSpace(settings.EndpointUrl))
                return "endpoint_not_configured";

            if (!settings.HasApiKey)
                return "api_key_not_configured";

            if (!settings.HasModel)
                return "model_not_configured";
        }

        return null;
    }

    public static string ResolveModelId(AiAssistantSettingGroup settings)
        => NormalizeProvider(settings.Provider) == AiProviderDefaults.ProviderFake
            ? AiProviderDefaults.FakeModelId
            : settings.ModelId.Trim();
}
