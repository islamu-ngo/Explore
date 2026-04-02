// ABOUTME: Stress tests for rate limiting middleware using StressApiFixture (rate limiting enabled).
// ABOUTME: Verifies 429 responses, ProblemDetails structure, Retry-After header, and rate limit headers.

using System.Net;
using System.Text;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Seeds;
using Explore.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Rate limiting enforcement tests using the Stress host profile.
/// Rate limiting is enabled with low thresholds to trigger 429 responses.
/// Targets Authenticated and Write policies (Global exempts loopback IPs).
/// </summary>
[ClassDataSource<StressApiFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("StressDb")]
public class StressRateLimitingTests(StressApiFixture fixture)
{
    private readonly StressApiFixture _fixture = fixture;

    [Test]
    public async Task WriteEndpoint_ExceedingLimit_ShouldReturn429()
    {
        await _fixture.ResetDatabaseAsync();

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);

        var userId = Guid.NewGuid();
        HttpResponseMessage? rateLimitedResponse = null;

        // Send requests exceeding the write limit (configured to 3)
        for (var i = 0; i < 10; i++)
        {
            var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/event", userId);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { Title = $"Rate limit test {i}" }),
                Encoding.UTF8,
                "application/json");

            var response = await _fixture.Client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitedResponse = response;
                break;
            }
        }

        await Assert.That(rateLimitedResponse).IsNotNull();
        await Assert.That(rateLimitedResponse!.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task RateLimited_ShouldReturnRetryAfterHeader()
    {
        await _fixture.ResetDatabaseAsync();

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);

        var userId = Guid.NewGuid();
        HttpResponseMessage? rateLimitedResponse = null;

        for (var i = 0; i < 10; i++)
        {
            var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/event", userId);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { Title = $"Retry-After test {i}" }),
                Encoding.UTF8,
                "application/json");

            var response = await _fixture.Client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitedResponse = response;
                break;
            }
        }

        if (rateLimitedResponse is not null)
        {
            var hasRetryAfter = rateLimitedResponse.Headers.Contains("Retry-After");
            await Assert.That(hasRetryAfter).IsTrue();
        }
    }

    [Test]
    public async Task RateLimited_ShouldReturnProblemDetailsBody()
    {
        await _fixture.ResetDatabaseAsync();

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);

        var userId = Guid.NewGuid();
        HttpResponseMessage? rateLimitedResponse = null;

        for (var i = 0; i < 10; i++)
        {
            var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/event", userId);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { Title = $"ProblemDetails test {i}" }),
                Encoding.UTF8,
                "application/json");

            var response = await _fixture.Client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitedResponse = response;
                break;
            }
        }

        if (rateLimitedResponse is not null)
        {
            var content = await rateLimitedResponse.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            await Assert.That(json.RootElement.TryGetProperty("status", out var status)).IsTrue();
            await Assert.That(status.GetInt32()).IsEqualTo(429);

            await Assert.That(json.RootElement.TryGetProperty("title", out _)).IsTrue();
        }
    }

    [Test]
    public async Task GetEndpoint_Unauthenticated_ShouldNotBeRateLimited_ByWritePolicy()
    {
        // GET endpoints are AllowAnonymous and not subject to Write policy
        for (var i = 0; i < 10; i++)
        {
            var response = await _fixture.Client.GetAsync("/api/event");
            // GET should not hit write rate limit
            await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.TooManyRequests);
        }
    }
}
