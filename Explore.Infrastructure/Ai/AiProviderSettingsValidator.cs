// ABOUTME: Validates resolved AI provider settings before Infrastructure adapters run.
// ABOUTME: Rejects unsupported providers, missing model credentials, and unsafe provider endpoints.

using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Ai;

public sealed class AiProviderSettingsValidator : IValidateOptions<AiProviderSettings>
{
    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        AiProviderSettings.ProviderNone,
        AiProviderSettings.ProviderFake,
        AiProviderSettings.ProviderOpenAiCompatible,
        AiProviderSettings.ProviderOpenAiSdk,
        AiProviderSettings.ProviderAzureOpenAi
    };

    private static readonly HashSet<string> SupportedAzureCredentialModes = new(StringComparer.OrdinalIgnoreCase)
    {
        AiProviderSettings.AzureCredentialModeApiKey,
        AiProviderSettings.AzureCredentialModeDefaultAzureCredential
    };

    public ValidateOptionsResult Validate(string? name, AiProviderSettings options)
    {
        List<string> failures = [];

        if (!SupportedProviders.Contains(options.Provider))
        {
            failures.Add("AiProvider:Provider must be none, fake, openai-compatible, openai-sdk, or azure-openai.");
        }

        if (!string.IsNullOrWhiteSpace(options.EndpointUrl))
        {
            ValidateEndpointSafety(options, failures);
        }

        if (options.Enabled
            && options.Provider.Equals(AiProviderSettings.ProviderOpenAiCompatible, StringComparison.OrdinalIgnoreCase))
        {
            ValidateOpenAiCompatibleSettings(options, failures);
        }

        if (options.Enabled
            && options.Provider.Equals(AiProviderSettings.ProviderOpenAiSdk, StringComparison.OrdinalIgnoreCase))
        {
            ValidateOpenAiSdkSettings(options, failures);
        }

        if (options.Enabled
            && options.Provider.Equals(AiProviderSettings.ProviderAzureOpenAi, StringComparison.OrdinalIgnoreCase))
        {
            ValidateAzureOpenAiSettings(options, failures);
        }

        ValidateLimits(options, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateOpenAiSdkSettings(AiProviderSettings options, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            failures.Add("AiProvider:ApiKey is required for openai-sdk providers.");
        }

        if (string.IsNullOrWhiteSpace(options.ModelId))
        {
            failures.Add("AiProvider:ModelId is required for openai-sdk providers.");
        }
    }

    private static void ValidateAzureOpenAiSettings(AiProviderSettings options, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.EndpointUrl))
        {
            failures.Add("AiProvider:EndpointUrl is required for azure-openai providers.");
        }
        else if (Uri.TryCreate(options.EndpointUrl, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add("AiProvider:EndpointUrl must use HTTPS for azure-openai providers.");
        }

        if (string.IsNullOrWhiteSpace(options.ModelId))
        {
            failures.Add("AiProvider:ModelId is required for azure-openai providers.");
        }

        if (!SupportedAzureCredentialModes.Contains(options.AzureCredentialMode))
        {
            failures.Add("AiProvider:AzureCredentialMode must be api-key or default-azure-credential.");
        }

        if (options.AzureCredentialMode.Equals(AiProviderSettings.AzureCredentialModeApiKey, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(options.ApiKey))
        {
            failures.Add("AiProvider:ApiKey is required for azure-openai providers using api-key authentication.");
        }
    }

    private static void ValidateOpenAiCompatibleSettings(AiProviderSettings options, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.ModelId))
        {
            failures.Add("AiProvider:ModelId is required for openai-compatible providers.");
        }

        if (string.IsNullOrWhiteSpace(options.EndpointUrl))
        {
            failures.Add("AiProvider:EndpointUrl is required for openai-compatible providers.");
        }
    }

    private static void ValidateEndpointSafety(AiProviderSettings options, List<string> failures)
    {
        if (!Uri.TryCreate(options.EndpointUrl, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp))
        {
            failures.Add("AiProvider:EndpointUrl must be an absolute HTTP or HTTPS URL.");
            return;
        }

        if (!string.IsNullOrEmpty(endpoint.UserInfo))
        {
            failures.Add("AiProvider:EndpointUrl must not contain embedded credentials.");
        }

        if (!string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment))
        {
            failures.Add("AiProvider:EndpointUrl must not contain query strings or fragments.");
        }

        if (!options.AllowLocalProviderEndpoints && IsLocalOrPrivateEndpoint(endpoint))
        {
            failures.Add("AiProvider:EndpointUrl must not target local, loopback, link-local, or private network hosts unless local provider endpoints are explicitly allowed.");
        }
    }

    private static void ValidateLimits(AiProviderSettings options, List<string> failures)
    {
        if (options.MaxInputTokens <= 0)
        {
            failures.Add("AiProvider:MaxInputTokens must be greater than zero.");
        }

        if (options.MaxOutputTokens <= 0)
        {
            failures.Add("AiProvider:MaxOutputTokens must be greater than zero.");
        }

        if (options.Temperature < 0m || options.Temperature > 2m)
        {
            failures.Add("AiProvider:Temperature must be between 0 and 2.");
        }

        if (options.TimeoutSeconds <= 0 || options.TimeoutSeconds > 300)
        {
            failures.Add("AiProvider:TimeoutSeconds must be between 1 and 300.");
        }

        if (options.RetentionDays < 0)
        {
            failures.Add("AiProvider:RetentionDays must be zero or greater.");
        }

        if (options.DailyMessageLimit <= 0)
        {
            failures.Add("AiProvider:DailyMessageLimit must be greater than zero.");
        }
    }

    private static bool IsLocalOrPrivateEndpoint(Uri endpoint)
    {
        if (endpoint.IsLoopback)
        {
            return true;
        }

        if (!System.Net.IPAddress.TryParse(endpoint.Host, out var address))
        {
            return string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase);
        }

        byte[] bytes = address.GetAddressBytes();

        return address.AddressFamily switch
        {
            System.Net.Sockets.AddressFamily.InterNetwork =>
                bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254),
            System.Net.Sockets.AddressFamily.InterNetworkV6 => address.IsIPv6LinkLocal || address.IsIPv6SiteLocal,
            _ => false
        };
    }
}
