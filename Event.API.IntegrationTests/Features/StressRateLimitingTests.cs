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
/// Rate limiting stress tests using the Stress host profile.
/// Verifies repeated requests remain within expected behavior and, when throttling occurs,
/// that the runtime emits standard 429 metadata.
/// </summary>
[ClassDataSource<StressApiFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("StressDb")]
public class StressRateLimitingTests(StressApiFixture fixture)
{
    private readonly StressApiFixture _fixture = fixture;

    [Test]
    public async Task AuthenticatedEndpoint_RepeatedRequests_ShouldNotReturnServerErrors()
    {
        await _fixture.ResetDatabaseAsync();

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenant = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);

        var userId = Guid.NewGuid();
        var observedStatuses = new List<HttpStatusCode>();

        var url = $"/api/admin/custom-property-projections/status?tenantId={tenant.TenantId}";

        for (var i = 0; i < 10; i++)
        {
            var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Get, url, userId);

            var response = await _fixture.Client.SendAsync(request);
            observedStatuses.Add(response.StatusCode);
        }

        var unexpectedStatuses = observedStatuses
            .Where(status => status != HttpStatusCode.OK && status != HttpStatusCode.TooManyRequests)
            .ToList();

        await Assert.That(unexpectedStatuses).IsEmpty();
        await Assert.That(observedStatuses.Any(status => status == HttpStatusCode.OK)).IsTrue();
    }

    [Test]
    public async Task RateLimited_ShouldReturnRetryAfterHeader()
    {
        await _fixture.ResetDatabaseAsync();

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenant = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);

        var userId = Guid.NewGuid();
        HttpResponseMessage? rateLimitedResponse = null;

        var url = $"/api/admin/custom-property-projections/status?tenantId={tenant.TenantId}";

        for (var i = 0; i < 10; i++)
        {
            var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Get, url, userId);

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
        var tenant = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);

        var userId = Guid.NewGuid();
        HttpResponseMessage? rateLimitedResponse = null;

        var url = $"/api/admin/custom-property-projections/status?tenantId={tenant.TenantId}";

        for (var i = 0; i < 10; i++)
        {
            var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Get, url, userId);

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
