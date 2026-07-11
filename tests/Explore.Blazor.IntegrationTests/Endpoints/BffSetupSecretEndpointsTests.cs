// ABOUTME: Integration tests for browser-facing setup-secret BFF endpoint sanitization.
// ABOUTME: Verifies local request validation and safe upstream error translation.

using System.Text;
using System.Threading.RateLimiting;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Extensions;
using Explore.Blazor.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class BffSetupSecretEndpointsTests
{
    [Test]
    public async Task SetupSecret_Post_WhenUpstreamForbiddenIncludesRawError_ReturnsSafeProblem()
    {
        using var handler = new ValidateSecretHandler(
            HttpStatusCode.Forbidden,
            """{"valid":false,"error":"provider rejected secret raw-secret-value"}""");
        await using var app = await CreateAppAsync(handler);

        using var response = await app.Client.PostAsJsonAsync("/bff/setup-secret", new { secret = "candidate-secret" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid setup secret.");
        body.Should().NotContain("provider rejected");
        body.Should().NotContain("raw-secret-value");
    }

    [Test]
    public async Task SetupSecret_Post_WhenUpstreamValidFalseIncludesRawError_ReturnsSafeProblem()
    {
        using var handler = new ValidateSecretHandler(
            HttpStatusCode.OK,
            """{"valid":false,"error":"database said exact setup secret hash mismatch"}""");
        await using var app = await CreateAppAsync(handler);

        using var response = await app.Client.PostAsJsonAsync("/bff/setup-secret", new { secret = "candidate-secret" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid setup secret.");
        body.Should().NotContain("database said");
        body.Should().NotContain("hash mismatch");
    }

    [Test]
    public async Task SetupSecret_Post_WhenRequestJsonMalformed_ReturnsSafeBadRequest()
    {
        using var handler = new ValidateSecretHandler(HttpStatusCode.OK, """{"valid":true}""");
        await using var app = await CreateAppAsync(handler);
        using var content = new StringContent("{", Encoding.UTF8, "application/json");

        using var response = await app.Client.PostAsync("/bff/setup-secret", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Setup secret request body must be valid JSON.");
        body.Should().NotContain("JsonException");
        handler.CallCount.Should().Be(0);
    }

    [Test]
    public async Task SetupSecret_Post_WhenSecretTooLong_DoesNotCallApi()
    {
        using var handler = new ValidateSecretHandler(HttpStatusCode.OK, """{"valid":true}""");
        await using var app = await CreateAppAsync(handler);
        var tooLongSecret = new string('a', 513);

        using var response = await app.Client.PostAsJsonAsync("/bff/setup-secret", new { secret = tooLongSecret });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("512 characters or fewer");
        handler.CallCount.Should().Be(0);
    }

    [Test]
    public async Task SetupSecret_Post_WithBrowserSetupSecretHeader_ValidatesOnlyBodySecret()
    {
        using var handler = new ValidateSecretHandler(HttpStatusCode.OK, """{"valid":true}""");
        await using var app = await CreateAppAsync(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/bff/setup-secret");
        request.Headers.Add("X-Setup-Secret", "browser-controlled-secret");
        request.Content = JsonContent.Create(new { secret = "body-secret" });

        using var response = await app.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.CallCount.Should().Be(1);
        handler.CapturedRequestBody.Should().Contain("body-secret");
        handler.CapturedRequestBody.Should().NotContain("browser-controlled-secret");
        handler.CapturedSetupSecretHeader.Should().BeNull();
    }

    [Test]
    public async Task SetupSecret_Post_WhenBrowserFacingRequestIsHttp_SetsNonSecureCookie()
    {
        using var handler = new ValidateSecretHandler(HttpStatusCode.OK, """{"valid":true}""");
        await using var app = await CreateAppAsync(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/bff/setup-secret")
        {
            Content = JsonContent.Create(new { secret = "candidate-secret" })
        };

        using var response = await app.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("setup-secret=", StringComparison.Ordinal));
        var sessionCookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("setup-secret-session=", StringComparison.Ordinal));
        var normalizedCookie = cookie.ToLowerInvariant();
        var normalizedSessionCookie = sessionCookie.ToLowerInvariant();
        normalizedCookie.Should().NotContain("; secure");
        normalizedSessionCookie.Should().NotContain("; secure");
        normalizedCookie.Should().Contain("httponly");
        normalizedSessionCookie.Should().Contain("httponly");
    }

    [Test]
    public async Task SetupSecret_Post_WhenBrowserFacingRequestIsHttps_SetsSecureCookie()
    {
        using var handler = new ValidateSecretHandler(HttpStatusCode.OK, """{"valid":true}""");
        await using var app = await CreateAppAsync(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost/bff/setup-secret")
        {
            Content = JsonContent.Create(new { secret = "candidate-secret" })
        };

        using var response = await app.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("setup-secret=", StringComparison.Ordinal));
        var sessionCookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("setup-secret-session=", StringComparison.Ordinal));
        var normalizedCookie = cookie.ToLowerInvariant();
        var normalizedSessionCookie = sessionCookie.ToLowerInvariant();
        normalizedCookie.Should().Contain("; secure");
        normalizedSessionCookie.Should().Contain("; secure");
        normalizedCookie.Should().Contain("httponly");
        normalizedSessionCookie.Should().Contain("httponly");
    }

    [Test]
    public async Task SetupSecret_Post_WhenRateLimitExceeded_ReturnsProblemDetails429()
    {
        using var handler = new ValidateSecretHandler(HttpStatusCode.OK, """{"valid":true}""");
        await using var app = await CreateAppAsync(handler, useRealSetupRateLimit: true);

        using var firstRequest = CreateSetupSecretRequest("first-secret", "stable-xsrf-partition");
        using var firstResponse = await app.Client.SendAsync(firstRequest);
        using var secondRequest = CreateSetupSecretRequest("second-secret", "stable-xsrf-partition");
        using var secondResponse = await app.Client.SendAsync(secondRequest);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        secondResponse.Headers.Contains("Retry-After").Should().BeTrue();
        var body = await secondResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Too Many Requests");
        body.Should().Contain("Too many setup-secret attempts");
        body.Should().NotContain("second-secret");
        handler.CallCount.Should().Be(1);
    }

    private static async Task<TestBffApp> CreateAppAsync(
        ValidateSecretHandler handler,
        bool useRealSetupRateLimit = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddLogging();

        if (useRealSetupRateLimit)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:DisableInTesting"] = "false",
                ["RateLimiting:SetupSecret:PermitLimit"] = "1",
                ["RateLimiting:SetupSecret:WindowSeconds"] = "60"
            });
            builder.Services.AddBffRateLimiting(builder.Configuration, builder.Environment);
        }
        else
        {
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy(RateLimitingExtensions.SetupSecretPolicy, _ =>
                    RateLimitPartition.GetNoLimiter<string>("test"));
            });
        }

        builder.Services.AddSingleton<SetupSecretSessionService>();
        builder.Services.AddSingleton<ISetupSecretSessionService>(sp => sp.GetRequiredService<SetupSecretSessionService>());
        builder.Services.AddSingleton<ISetupSecretCookieProtector, PassThroughSetupSecretCookieProtector>();
        builder.Services.AddSingleton<ISetupSecretResolver, EmptySetupSecretResolver>();
        builder.Services.AddSingleton(new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://api.example.test/")
        });
        builder.Services.AddSingleton<IEventApiClient>(services =>
            new EventApiClient(services.GetRequiredService<HttpClient>()));

        var app = builder.Build();
        app.UseRouting();
        app.UseRateLimiter();
        app.MapSetupSecretEndpoints();
        await app.StartAsync();

        return new TestBffApp(app, app.GetTestClient());
    }

    private static HttpRequestMessage CreateSetupSecretRequest(string secret, string partitionCookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/bff/setup-secret")
        {
            Content = JsonContent.Create(new { secret })
        };
        request.Headers.Add("Cookie", $"XSRF-TOKEN={partitionCookie}");
        return request;
    }

    private sealed class TestBffApp(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    private sealed class ValidateSecretHandler(HttpStatusCode statusCode, string responseJson) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public string? CapturedRequestBody { get; private set; }
        public string? CapturedSetupSecretHeader { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            CapturedRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            CapturedSetupSecretHeader = request.Headers.TryGetValues("X-Setup-Secret", out var values)
                ? values.SingleOrDefault()
                : null;

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class PassThroughSetupSecretCookieProtector : ISetupSecretCookieProtector
    {
        public string Protect(string secret) => secret.Trim();

        public bool TryUnprotect(string? protectedValue, out string? secret)
        {
            secret = protectedValue?.Trim();
            return !string.IsNullOrWhiteSpace(secret);
        }
    }

    private sealed class EmptySetupSecretResolver : ISetupSecretResolver
    {
        public SetupSecretResolutionResult Resolve(
            HttpContext? httpContext = null,
            HttpRequestMessage? outboundRequest = null)
        {
            return SetupSecretResolutionResult.NotFound("test_secret_missing");
        }
    }
}
