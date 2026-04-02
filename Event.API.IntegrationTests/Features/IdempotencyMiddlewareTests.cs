// ABOUTME: Integration tests for IdempotencyMiddleware covering key validation, replay, and passthrough.
// ABOUTME: Verifies the full middleware pipeline: invalid keys return ProblemDetails, valid keys cache and replay.

using System.Net;
using System.Text;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Seeds;
using Explore.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Tests the Idempotency-Key middleware behavior through the real ASP.NET Core pipeline.
/// Verifies key validation (length, whitespace), GET passthrough, write method processing,
/// and response replay on duplicate keys.
/// </summary>
[ClassDataSource<RealRuntimeApiFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RealRuntimeDb")]
public class IdempotencyMiddlewareTests(RealRuntimeApiFixture fixture)
{
    private readonly RealRuntimeApiFixture _fixture = fixture;

    [Test]
    public async Task Get_WithIdempotencyKey_IgnoresKeyAndPassesThrough()
    {
        await _fixture.ResetDatabaseAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/event");
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await _fixture.Client.SendAsync(request);

        // GET requests bypass the idempotency middleware entirely
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.Contains("X-Idempotency-Replay")).IsFalse();
    }

    [Test]
    public async Task Post_WithoutIdempotencyKey_PassesThroughNormally()
    {
        await _fixture.ResetDatabaseAsync();

        // POST without Idempotency-Key header — middleware is opt-in
        var content = new StringContent("""{"title":"No Key Event"}""", Encoding.UTF8, "application/json");
        var response = await _fixture.Client.PostAsync("/api/event", content);

        // Should get 401 (anonymous POST), not a middleware error
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        await Assert.That(response.Headers.Contains("X-Idempotency-Replay")).IsFalse();
    }

    [Test]
    public async Task Post_WithInvalidKey_TooLong_ReturnsBadRequest()
    {
        await _fixture.ResetDatabaseAsync();

        var tooLongKey = new string('x', 129);
        var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/event");
        request.Headers.Add("Idempotency-Key", tooLongKey);
        request.Content = new StringContent("""{"title":"Too Long Key"}""", Encoding.UTF8, "application/json");

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("Idempotency-Key must be at most 128 characters");
    }

    [Test]
    public async Task Post_WithInvalidKey_ContainingWhitespace_ReturnsBadRequest()
    {
        await _fixture.ResetDatabaseAsync();

        var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/event");
        request.Headers.Add("Idempotency-Key", "invalid key with spaces");
        request.Content = new StringContent("""{"title":"Space Key"}""", Encoding.UTF8, "application/json");

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        await Assert.That(contentType).IsEqualTo("application/problem+json");
    }

    [Test]
    public async Task Post_WithValidKey_SecondRequest_ReplaysResponseWithHeader()
    {
        await _fixture.ResetDatabaseAsync();

        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenantResult = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(context);

        var idempotencyKey = Guid.NewGuid().ToString("N");

        // First request — response gets captured by middleware
        var firstRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Post, "/api/event", tenantResult.UserId);
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        firstRequest.Content = new StringContent(
            """{"title":"Idempotent Event"}""", Encoding.UTF8, "application/json");

        var firstResponse = await _fixture.Client.SendAsync(firstRequest);
        var firstStatus = firstResponse.StatusCode;
        var firstBody = await firstResponse.Content.ReadAsStringAsync();

        // Second request — same key should replay cached response
        var secondRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Post, "/api/event", tenantResult.UserId);
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        secondRequest.Content = new StringContent(
            """{"title":"Idempotent Event"}""", Encoding.UTF8, "application/json");

        var secondResponse = await _fixture.Client.SendAsync(secondRequest);

        await Assert.That(secondResponse.StatusCode).IsEqualTo(firstStatus);
        await Assert.That(secondResponse.Headers.Contains("X-Idempotency-Replay")).IsTrue();

        var replayHeaderValue = secondResponse.Headers.GetValues("X-Idempotency-Replay").FirstOrDefault();
        await Assert.That(replayHeaderValue).IsEqualTo("true");

        var secondBody = await secondResponse.Content.ReadAsStringAsync();
        await Assert.That(secondBody).IsEqualTo(firstBody);
    }
}
