// ABOUTME: Unit tests for AI provider settings validation.
// ABOUTME: Protects provider bootstrap from unsupported providers, missing credentials, and unsafe endpoints.

using Explore.Infrastructure.Ai;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class AiProviderSettingsValidatorTests
{
    private readonly AiProviderSettingsValidator _validator = new();

    [Test]
    public async Task Validate_DefaultSettingsReturnsSuccess()
    {
        var result = _validator.Validate(null, new AiProviderSettings());

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task Validate_FakeProviderDoesNotRequireEndpointModelOrKey()
    {
        var result = _validator.Validate(null, new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderFake
        });

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task Validate_OpenAiCompatibleRequiresEndpointModelAndKey()
    {
        var result = _validator.Validate(null, new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderOpenAiCompatible
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("EndpointUrl");
        await Assert.That(result.FailureMessage).Contains("ModelId");
        await Assert.That(result.FailureMessage).Contains("ApiKey");
    }

    [Test]
    public async Task Validate_OpenAiCompatibleAcceptsSafeHttpEndpoint()
    {
        var result = _validator.Validate(null, new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderOpenAiCompatible,
            EndpointUrl = "https://ai.example.test/v1",
            ModelId = "gpt-test",
            ApiKey = "test-key"
        });

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task Validate_EndpointWithEmbeddedCredentialsReturnsFailure()
    {
        var result = _validator.Validate(null, new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderOpenAiCompatible,
            EndpointUrl = "https://user:password@ai.example.test/v1",
            ModelId = "gpt-test",
            ApiKey = "test-key"
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("embedded credentials");
    }

    [Test]
    public async Task Validate_DisabledOpenAiCompatibleDoesNotRequireEndpointModelOrKey()
    {
        var result = _validator.Validate(null, new AiProviderSettings
        {
            Enabled = false,
            Provider = AiProviderSettings.ProviderOpenAiCompatible
        });

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task Validate_LocalEndpointRequiresExplicitOptIn()
    {
        var result = _validator.Validate(null, new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderOpenAiCompatible,
            EndpointUrl = "http://localhost:11434/v1",
            ModelId = "local-model",
            ApiKey = "test-key"
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("local, loopback, link-local, or private network hosts");
    }

    [Test]
    public async Task Validate_LocalEndpointCanBeExplicitlyAllowed()
    {
        var result = _validator.Validate(null, new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderOpenAiCompatible,
            EndpointUrl = "http://localhost:11434/v1",
            ModelId = "local-model",
            ApiKey = "test-key",
            AllowLocalProviderEndpoints = true
        });

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task Validate_QueryStringEndpointReturnsFailure()
    {
        var result = _validator.Validate(null, new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderOpenAiCompatible,
            EndpointUrl = "https://ai.example.test/v1?tenant=browser-controlled",
            ModelId = "gpt-test",
            ApiKey = "test-key"
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("query strings or fragments");
    }

    [Test]
    public async Task Validate_InvalidLimitsReturnFailure()
    {
        var result = _validator.Validate(null, new AiProviderSettings
        {
            MaxInputTokens = 0,
            MaxOutputTokens = 0,
            Temperature = 3m,
            TimeoutSeconds = 0,
            RetentionDays = -1,
            DailyMessageLimit = 0
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("MaxInputTokens");
        await Assert.That(result.FailureMessage).Contains("MaxOutputTokens");
        await Assert.That(result.FailureMessage).Contains("Temperature");
        await Assert.That(result.FailureMessage).Contains("TimeoutSeconds");
        await Assert.That(result.FailureMessage).Contains("RetentionDays");
        await Assert.That(result.FailureMessage).Contains("DailyMessageLimit");
    }
}
