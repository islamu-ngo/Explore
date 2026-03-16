using System.Net;
using System.Net.Http.Json;
using Explore.Blazor.Client.Services.Http;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Tests.Services.Http;

public class BrowserCredentialsMessageHandlerTests
{
    [Test]
    public async Task SendAsync_AddsXsrfHeader_ForMutatingRequests()
    {
        var logger = Substitute.For<ILogger<BrowserCredentialsMessageHandler>>();
        var jsRuntime = new FakeJsRuntime("test-xsrf-token");
        var handler = new BrowserCredentialsMessageHandler(logger, jsRuntime)
        {
            InnerHandler = new CaptureHandler()
        };

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };

        using var response = await client.PostAsJsonAsync("/bff/setup-secret", new { secret = "abc" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(((CaptureHandler)handler.InnerHandler!).LastRequest).IsNotNull();
        await Assert.That(((CaptureHandler)handler.InnerHandler!).LastRequest!.Headers.TryGetValues("X-CSRF-TOKEN", out var values)).IsTrue();
        await Assert.That(values!.Single()).IsEqualTo("test-xsrf-token");
    }

    [Test]
    public async Task SendAsync_DoesNotAddXsrfHeader_ForGetRequests()
    {
        var handler = new BrowserCredentialsMessageHandler(Substitute.For<ILogger<BrowserCredentialsMessageHandler>>(), new FakeJsRuntime("token"))
        {
            InnerHandler = new CaptureHandler()
        };

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };

        using var response = await client.GetAsync("/auth/status");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(((CaptureHandler)handler.InnerHandler!).LastRequest!.Headers.Contains("X-CSRF-TOKEN")).IsFalse();
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class FakeJsRuntime(string token) : IJSRuntime, IDisposable
    {
        private readonly FakeJsModule _module = new(token);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            object? result = identifier switch
            {
                "import" => _module,
                _ => default(TValue)
            };

            return new ValueTask<TValue>((TValue)result!);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeJsModule(string token) : IJSObjectReference
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            object? result = identifier == "getCookie" ? token : default(TValue);
            return new ValueTask<TValue>((TValue)result!);
        }
    }
}
