// ABOUTME: Validates webhook provider runtime settings before provider selection starts.
// ABOUTME: Rejects unsupported modes and unsafe LocalProvider delivery limits.

using Explore.Domain.Secrets;
using Explore.Infrastructure.Webhooks;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Configuration;

public sealed class WebhookOptionsValidator : IValidateOptions<WebhookOptions>
{
    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        WebhookOptions.ProviderDisabled,
        WebhookOptions.ProviderLocal,
        WebhookOptions.ProviderSvix,
        WebhookOptions.ProviderComposite,
        WebhookOptions.ProviderDryRun
    };

    public ValidateOptionsResult Validate(string? name, WebhookOptions options)
    {
        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.Provider) || !SupportedProviders.Contains(options.Provider.Trim()))
        {
            failures.Add("Webhooks:Provider must be Disabled, Local, Svix, Composite, or DryRun.");
        }

        if (options.DefaultPayloadRetentionDays is < 1 or > 365)
        {
            failures.Add("Webhooks:DefaultPayloadRetentionDays must be between 1 and 365.");
        }

        ValidateSvixOperationalOptions(options.Svix, failures);

        if (!options.IsDisabled && (options.IsProvider(WebhookOptions.ProviderSvix) || options.IsProvider(WebhookOptions.ProviderComposite)))
        {
            ValidateSvixOptions(options.Svix, failures);
        }

        if (options.Local.MaxAttempts is < 1 or > 20)
        {
            failures.Add("Webhooks:Local:MaxAttempts must be between 1 and 20.");
        }

        if (options.Local.TimeoutSeconds is < 1 or > 60)
        {
            failures.Add("Webhooks:Local:TimeoutSeconds must be between 1 and 60.");
        }

        if (options.Local.ConnectTimeoutSeconds is < 1 or > 30)
        {
            failures.Add("Webhooks:Local:ConnectTimeoutSeconds must be between 1 and 30.");
        }

        if (options.Local.ConnectTimeoutSeconds > options.Local.TimeoutSeconds)
        {
            failures.Add("Webhooks:Local:ConnectTimeoutSeconds cannot exceed Webhooks:Local:TimeoutSeconds.");
        }

        if (options.Local.MaxPayloadBytes is < 1024 or > 2 * 1024 * 1024)
        {
            failures.Add("Webhooks:Local:MaxPayloadBytes must be between 1 KiB and 2 MiB.");
        }

        if (options.Local.MaxResponsePreviewBytes is < 0 or > 16 * 1024)
        {
            failures.Add("Webhooks:Local:MaxResponsePreviewBytes must be between 0 and 16 KiB.");
        }

        foreach (var cidr in options.Local.AllowedPrivateCidrs)
        {
            if (!WebhookIpNetwork.TryParse(cidr, out _))
            {
                failures.Add($"Webhooks:Local:AllowedPrivateCidrs contains invalid CIDR '{cidr}'.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateSvixOptions(WebhookSvixOptions options, List<string> failures)
    {
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            if (!Uri.TryCreate(options.BaseUrl.Trim(), UriKind.Absolute, out var baseUrl))
            {
                failures.Add("Webhooks:Svix:BaseUrl must be an absolute URL when configured.");
            }
            else if (!string.Equals(baseUrl.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(baseUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("Webhooks:Svix:BaseUrl must use http or https when configured.");
            }
        }

        if (string.IsNullOrWhiteSpace(options.AuthTokenSecretRef))
        {
            failures.Add("Webhooks:Svix:AuthTokenSecretRef is required when Svix or Composite provider mode is configured.");
            return;
        }

        if (!SecretDefinitionRegistry.IsKnown(options.AuthTokenSecretRef.Trim()))
        {
            failures.Add("Webhooks:Svix:AuthTokenSecretRef must reference a known secret definition.");
        }
    }

    private static void ValidateSvixOperationalOptions(WebhookSvixOptions options, List<string> failures)
    {
        if (options.OperationalWebhookMaxBodyBytes <= 0)
        {
            failures.Add("Webhooks:Svix:OperationalWebhookMaxBodyBytes must be greater than zero.");
        }

        if (!string.IsNullOrWhiteSpace(options.OperationalWebhookSecretRef)
            && !SecretDefinitionRegistry.IsKnown(options.OperationalWebhookSecretRef.Trim()))
        {
            failures.Add("Webhooks:Svix:OperationalWebhookSecretRef must reference a known secret definition when configured.");
        }
    }
}
