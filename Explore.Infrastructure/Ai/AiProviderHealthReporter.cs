// ABOUTME: Evaluates AI provider readiness from validated deployment/admin-controlled settings.
// ABOUTME: Produces safe health metadata without prompts, secrets, endpoints, or provider request IDs.

using Explore.Application.Contracts.Infrastructure.Ai;

namespace Explore.Infrastructure.Ai;

public sealed class AiProviderHealthReporter(AiProviderSettingsValidator validator)
{
    public AiProviderHealth Check(AiProviderSettings settings)
    {
        Dictionary<string, object> data = new(StringComparer.Ordinal)
        {
            ["enabled"] = settings.Enabled,
            ["provider"] = NormalizeProvider(settings.Provider),
            ["endpointConfigured"] = !string.IsNullOrWhiteSpace(settings.EndpointUrl),
            ["apiKeyConfigured"] = !string.IsNullOrWhiteSpace(settings.ApiKey),
            ["modelConfigured"] = !string.IsNullOrWhiteSpace(settings.ModelId),
            ["toolProposalsEnabled"] = settings.ToolProposalsEnabled,
            ["streamingEnabled"] = settings.StreamingEnabled,
            ["timeoutSeconds"] = settings.TimeoutSeconds,
            ["localProviderEndpointsAllowed"] = settings.AllowLocalProviderEndpoints
        };

        var validation = validator.Validate(null, settings);
        if (!validation.Succeeded)
        {
            data["reason"] = "invalid_settings";
            return new AiProviderHealth(
                Enabled: settings.Enabled,
                Healthy: false,
                Status: "invalid_settings",
                Description: "AI provider settings are invalid.",
                Data: data);
        }

        if (!settings.Enabled)
        {
            data["reason"] = "disabled";
            return new AiProviderHealth(
                Enabled: false,
                Healthy: true,
                Status: "healthy_disabled",
                Description: "AI provider integration is intentionally disabled.",
                Data: data);
        }

        if (settings.Provider.Equals(AiProviderSettings.ProviderFake, StringComparison.OrdinalIgnoreCase))
        {
            return new AiProviderHealth(
                Enabled: true,
                Healthy: true,
                Status: "healthy_fake",
                Description: "Deterministic fake AI provider is enabled for tests or local workflows.",
                Data: data);
        }

        if (settings.Provider.Equals(AiProviderSettings.ProviderOpenAiCompatible, StringComparison.OrdinalIgnoreCase))
        {
            return new AiProviderHealth(
                Enabled: true,
                Healthy: true,
                Status: "configured_no_probe",
                Description: "OpenAI-compatible AI provider settings are valid; network probing is deferred to the adapter.",
                Data: data);
        }

        if (settings.Provider.Equals(AiProviderSettings.ProviderOpenAiSdk, StringComparison.OrdinalIgnoreCase)
            || settings.Provider.Equals(AiProviderSettings.ProviderAzureOpenAi, StringComparison.OrdinalIgnoreCase))
        {
            return new AiProviderHealth(
                Enabled: true,
                Healthy: true,
                Status: "configured_no_probe",
                Description: "SDK-backed AI provider settings are valid; network probing is deferred to the adapter.",
                Data: data);
        }

        data["reason"] = "provider_not_configured";
        return new AiProviderHealth(
            Enabled: true,
            Healthy: false,
            Status: "provider_not_configured",
            Description: "AI provider integration is enabled but no runnable provider is configured.",
            Data: data);
    }

    private static string NormalizeProvider(string? provider) =>
        string.IsNullOrWhiteSpace(provider) ? AiProviderSettings.ProviderNone : provider.Trim().ToLowerInvariant();
}
