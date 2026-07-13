// ABOUTME: Unit tests for webhook provider startup configuration validation.
// ABOUTME: Covers Svix feature flags, auth-token secret refs, and self-hosted base URL safety.

using Explore.Domain.Secrets;
using Explore.Infrastructure.Configuration;

namespace Explore.Infrastructure.Tests.Infrastructure.Webhooks;

public sealed class WebhookOptionsValidatorTests
{
    private readonly WebhookOptionsValidator _validator = new();

    [Test]
    public async Task Validate_WhenDefaultLocalOptions_ReturnsSuccess()
    {
        var result = _validator.Validate(null, new WebhookOptions());

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task Validate_WhenSvixPortalEnabledWithoutAuthSecretRef_ReturnsFailure()
    {
        var result = _validator.Validate(null, new WebhookOptions
        {
            Provider = WebhookOptions.ProviderSvix,
            Svix = new WebhookSvixOptions
            {
                AppPortalEnabled = true,
                AuthTokenSecretRef = ""
            }
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("Webhooks:Svix:AuthTokenSecretRef is required");
    }

    [Test]
    public async Task Validate_WhenSvixEventTypeSyncUsesUnknownAuthSecretRef_ReturnsFailure()
    {
        var result = _validator.Validate(null, new WebhookOptions
        {
            Provider = WebhookOptions.ProviderComposite,
            Svix = new WebhookSvixOptions
            {
                SyncEventTypesOnStartup = true,
                AuthTokenSecretRef = "webhooks.svix.missing"
            }
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("Webhooks:Svix:AuthTokenSecretRef must reference a known secret definition");
    }

    [Test]
    public async Task Validate_WhenSvixBaseUrlUsesNonHttpScheme_ReturnsFailure()
    {
        var result = _validator.Validate(null, new WebhookOptions
        {
            Provider = WebhookOptions.ProviderSvix,
            Svix = new WebhookSvixOptions
            {
                BaseUrl = "file:///tmp/svix",
                AuthTokenSecretRef = SecretDefinitionRegistry.Keys.Webhooks.SvixAuthToken
            }
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("Webhooks:Svix:BaseUrl must use http or https");
    }

    [Test]
    public async Task Validate_WhenSvixAuthorityProfileIsIncomplete_ReturnsFailure()
    {
        var result = _validator.Validate(null, new WebhookOptions
        {
            Provider = WebhookOptions.ProviderSvix,
            Svix = new WebhookSvixOptions
            {
                Environment = "",
                ProviderVersion = "",
                CapabilityPolicyVersion = "",
                AuthTokenSecretRef = SecretDefinitionRegistry.Keys.Webhooks.SvixAuthToken
            }
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("Webhooks:Svix:Environment is required");
        await Assert.That(result.FailureMessage).Contains("Webhooks:Svix:ProviderVersion is required");
        await Assert.That(result.FailureMessage).Contains("Webhooks:Svix:CapabilityPolicyVersion is required");
    }
}
