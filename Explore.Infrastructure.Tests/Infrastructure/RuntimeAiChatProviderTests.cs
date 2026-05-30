// ABOUTME: Unit tests for the runtime AI provider selector.
// ABOUTME: Verifies fail-closed behavior and routing to fake or OpenAI-compatible adapters.

using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Telemetry;
using Explore.Domain.Ai;
using Explore.Infrastructure.Ai;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class RuntimeAiChatProviderTests
{
    [Test]
    public async Task SendAsync_WhenDisabled_FailsWithoutCallingHttp()
    {
        var handler = new RecordingMessageHandler(_ => throw new InvalidOperationException("HTTP should not be called."));
        var provider = CreateProvider(new AiProviderSettings(), handler);

        var result = await provider.SendAsync(CreateRequest("Hello"));

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Error!.Code).IsEqualTo("provider_disabled");
        await Assert.That(handler.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task SendAsync_WhenFakeProviderConfigured_UsesDeterministicFakeProvider()
    {
        var handler = new RecordingMessageHandler(_ => throw new InvalidOperationException("HTTP should not be called."));
        var provider = CreateProvider(new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderFake
        }, handler);

        var result = await provider.SendAsync(CreateRequest("Plan a community dinner"));

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Response!.AssistantMessage).Contains("Plan a community dinner");
        await Assert.That(result.Response.ProviderRequestId).IsEqualTo("fake-provider");
        await Assert.That(handler.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task ListAvailableModels_WhenFakeProviderConfigured_ReturnsFakeModel()
    {
        var provider = CreateProvider(new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderFake
        });

        var models = await provider.ListAvailableModelsAsync();

        await Assert.That(models.Count).IsEqualTo(1);
        await Assert.That(models[0].Id).IsEqualTo(FakeAiChatProvider.ModelId);
    }

    [Test]
    public async Task SendAsync_WhenOpenAiCompatibleConfigured_DelegatesToOpenAiAdapter()
    {
        var handler = new RecordingMessageHandler(_ => JsonResponse("""
            {
              "choices": [
                { "message": { "content": "OpenAI-compatible response" }, "finish_reason": "stop" }
              ],
              "usage": { "prompt_tokens": 8, "completion_tokens": 4, "total_tokens": 12 }
            }
            """));
        var provider = CreateProvider(CreateOpenAiSettings(), handler);

        var result = await provider.SendAsync(CreateRequest("Hello"));

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Response!.AssistantMessage).IsEqualTo("OpenAI-compatible response");
        await Assert.That(handler.Calls).IsEqualTo(1);
        await Assert.That(handler.RequestUri).IsEqualTo(new Uri("https://ai.example.test/v1/chat/completions"));
    }

    [Test]
    public async Task ListAvailableModels_WhenOpenAiCompatibleConfigured_ReturnsConfiguredModel()
    {
        var handler = new RecordingMessageHandler(_ => throw new InvalidOperationException("HTTP should not be called."));
        var provider = CreateProvider(CreateOpenAiSettings(), handler);

        var models = await provider.ListAvailableModelsAsync();

        await Assert.That(models.Count).IsEqualTo(1);
        await Assert.That(models[0].Id).IsEqualTo("gpt-test");
        await Assert.That(handler.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task SendAsync_WhenConfiguredEndpointIsUnsafe_FailsBeforeHttpCall()
    {
        var handler = new RecordingMessageHandler(_ => throw new InvalidOperationException("HTTP should not be called."));
        var provider = CreateProvider(new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderOpenAiCompatible,
            EndpointUrl = "http://localhost:11434/v1",
            ApiKey = "test-key",
            ModelId = "gpt-test"
        }, handler);

        var result = await provider.SendAsync(CreateRequest("Hello"));

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Error!.Code).IsEqualTo("invalid_settings");
        await Assert.That(handler.Calls).IsEqualTo(0);
    }

    private static RuntimeAiChatProvider CreateProvider(
        AiProviderSettings settings,
        RecordingMessageHandler? handler = null)
    {
        handler ??= new RecordingMessageHandler(_ => throw new InvalidOperationException("HTTP should not be called."));
        var client = new HttpClient(handler);
        var factory = new StaticHttpClientFactory(client);
        var options = Options.Create(settings);
        var validator = new AiProviderSettingsValidator();
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        var openAiProvider = new OpenAiCompatibleChatProvider(
            factory,
            options,
            validator,
            new BusinessMetrics(meterFactory));

        return new RuntimeAiChatProvider(
            options,
            validator,
            new FakeAiChatProvider(),
            openAiProvider);
    }

    private static AiProviderSettings CreateOpenAiSettings() => new()
    {
        Enabled = true,
        Provider = AiProviderSettings.ProviderOpenAiCompatible,
        EndpointUrl = "https://ai.example.test/v1",
        ApiKey = "test-key",
        ModelId = "gpt-test",
        MaxInputTokens = 8000,
        MaxOutputTokens = 1024,
        TimeoutSeconds = 30
    };

    private static AiChatRequest CreateRequest(string userMessage) => new(
        "gpt-test",
        [new AiChatMessage(AiMessageRole.User, userMessage)],
        "You are a safe assistant.",
        new AiChatOptions(8000, 1024, 0.2m, 30, ToolProposalsEnabled: false, StreamingEnabled: false));

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StaticHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class RecordingMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public RecordingMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public int Calls { get; private set; }
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            RequestUri = request.RequestUri;
            return Task.FromResult(_responseFactory(request));
        }
    }
}
