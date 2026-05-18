// ABOUTME: Verifies feature-flag client loading through the shared API executor.
// ABOUTME: Locks authenticated flag hydration and safe unauthenticated fallback behavior.

using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Refit;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class FeatureFlagClientServiceTests
{
    [Test]
    public async Task LoadFlagsAsync_WithSuccessfulResponse_HydratesFeatureState()
    {
        using var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {"beta-dashboard":true,"legacy-flow":false}
            """, Encoding.UTF8, "application/json")
        });
        using var httpClient = CreateClient(handler);
        var featureState = new FeatureStateContainer();
        var service = CreateService(httpClient, featureState);

        await service.LoadFlagsAsync();

        await Assert.That(featureState.IsEnabled("beta-dashboard")).IsTrue();
        await Assert.That(featureState.IsEnabled("legacy-flow")).IsFalse();
        await Assert.That(handler.Requests.Single().RequestUri?.PathAndQuery).IsEqualTo("/api/features/my-flags");
    }

    [Test]
    public async Task LoadFlagsAsync_WithUnauthorizedResponse_LeavesExistingFlagsUnchanged()
    {
        using var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var httpClient = CreateClient(handler);
        var featureState = new FeatureStateContainer();
        featureState.SetFlags(new Dictionary<string, bool> { ["existing"] = true });
        var service = CreateService(httpClient, featureState);

        await service.LoadFlagsAsync();

        await Assert.That(featureState.IsEnabled("existing")).IsTrue();
        await Assert.That(featureState.All.Count).IsEqualTo(1);
    }

    [Test]
    public async Task LoadFlagsAsync_WithTransportFailure_DoesNotThrowOrClearFlags()
    {
        using var handler = new RecordingHandler(_ => throw new HttpRequestException("network failed"));
        using var httpClient = CreateClient(handler);
        var featureState = new FeatureStateContainer();
        featureState.SetFlags(new Dictionary<string, bool> { ["existing"] = true });
        var service = CreateService(httpClient, featureState);

        await service.LoadFlagsAsync();

        await Assert.That(featureState.IsEnabled("existing")).IsTrue();
        await Assert.That(featureState.All.Count).IsEqualTo(1);
    }

    private static FeatureFlagClientService CreateService(HttpClient httpClient, FeatureStateContainer featureState)
    {
        var api = RestService.For<IFeatureFlagApi>(httpClient);
        return new FeatureFlagClientService(
            api,
            featureState,
            NullLogger<FeatureFlagClientService>.Instance);
    }

    private static HttpClient CreateClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://client.test")
        };
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}
