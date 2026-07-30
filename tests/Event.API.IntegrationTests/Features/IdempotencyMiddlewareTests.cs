// ABOUTME: Integration tests for IdempotencyMiddleware covering key validation, replay, and passthrough.
// ABOUTME: Verifies the full middleware pipeline: invalid keys return ProblemDetails, valid keys cache and replay.

using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Middleware;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IO;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Tests the Idempotency-Key middleware behavior through the real ASP.NET Core pipeline.
/// Verifies key validation (length, whitespace), GET passthrough, write method processing,
/// and response replay on duplicate keys.
/// </summary>
[ClassDataSource<RealRuntimeApiFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RealRuntimeDb")]
public class IdempotencyMiddlewareRealRuntimeTests(RealRuntimeApiFixture fixture)
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
    public async Task Post_WhenValidationFailureOccurs_DoesNotReplaySameKeyRetry()
    {
        await _fixture.ResetDatabaseAsync();

        var idempotencyKey = Guid.NewGuid().ToString("N");

        var firstRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Post, "/api/event");
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        firstRequest.Content = new StringContent(
            """{"title":"Invalid Event"}""", Encoding.UTF8, "application/json");

        var firstResponse = await _fixture.Client.SendAsync(firstRequest);
        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var secondRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Post, "/api/event");
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        secondRequest.Content = new StringContent(
            """{"title":"Invalid Event Retry"}""", Encoding.UTF8, "application/json");

        var secondResponse = await _fixture.Client.SendAsync(secondRequest);

        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(secondResponse.Headers.Contains("X-Idempotency-Replay")).IsFalse();
    }

    [Test]
    public async Task Middleware_WhenSameKeyAndEquivalentJsonPayload_ReplaysStoredResponse()
    {
        var repository = new InMemoryIdempotencyRepository();
        var nextCallCount = 0;

        var first = await InvokeMiddlewareAsync(
            repository,
            """{"title":"Replay","metadata":{"b":2,"a":1}}""",
            async context =>
            {
                nextCallCount++;
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"created":true}""");
            });

        var second = await InvokeMiddlewareAsync(
            repository,
            """
            {
              "metadata": { "a": 1, "b": 2 },
              "title": "Replay"
            }
            """,
            _ => throw new InvalidOperationException("Replay request should not invoke the next delegate."));

        await Assert.That(first.StatusCode).IsEqualTo(StatusCodes.Status201Created);
        await Assert.That(second.StatusCode).IsEqualTo(StatusCodes.Status201Created);
        await Assert.That(second.Headers["X-Idempotency-Replay"].ToString()).IsEqualTo("true");
        await Assert.That(second.Body).IsEqualTo("""{"created":true}""");
        await Assert.That(nextCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task Middleware_WhenSameKeyHasDifferentPayload_ReturnsConflict()
    {
        var repository = new InMemoryIdempotencyRepository();

        await InvokeMiddlewareAsync(
            repository,
            """{"title":"Original"}""",
            async context =>
            {
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"created":true}""");
            });

        var conflict = await InvokeMiddlewareAsync(
            repository,
            """{"title":"Changed"}""",
            _ => throw new InvalidOperationException("Conflicting request should not invoke the next delegate."));

        await Assert.That(conflict.StatusCode).IsEqualTo(StatusCodes.Status409Conflict);
        await Assert.That(conflict.Body).Contains("idempotency_key_reuse");
        await Assert.That(conflict.Headers.ContainsKey("X-Idempotency-Replay")).IsFalse();
    }

    [Test]
    public async Task Middleware_WhenSameKeyHasDifferentContentType_ReturnsConflict()
    {
        var repository = new InMemoryIdempotencyRepository();

        await InvokeMiddlewareAsync(
            repository,
            """{"title":"Original"}""",
            async context =>
            {
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"created":true}""");
            });

        var conflict = await InvokeMiddlewareAsync(
            repository,
            """{"title":"Original"}""",
            _ => throw new InvalidOperationException("Conflicting request should not invoke the next delegate."),
            contentType: "application/vnd.event+json; charset=utf-8");

        await Assert.That(conflict.StatusCode).IsEqualTo(StatusCodes.Status409Conflict);
        await Assert.That(conflict.Body).Contains("idempotency_key_reuse");
        await Assert.That(conflict.Headers.ContainsKey("X-Idempotency-Replay")).IsFalse();
    }

    [Test]
    public async Task Middleware_WhenSameKeyHasDifferentRoute_ReturnsConflict()
    {
        var repository = new InMemoryIdempotencyRepository();

        await InvokeMiddlewareAsync(
            repository,
            """{"title":"Original"}""",
            async context =>
            {
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"created":true}""");
            });

        var conflict = await InvokeMiddlewareAsync(
            repository,
            """{"title":"Original"}""",
            _ => throw new InvalidOperationException("Conflicting request should not invoke the next delegate."),
            path: "/api/idempotency-test/other");

        await Assert.That(conflict.StatusCode).IsEqualTo(StatusCodes.Status409Conflict);
        await Assert.That(conflict.Body).Contains("idempotency_key_reuse");
        await Assert.That(conflict.Headers.ContainsKey("X-Idempotency-Replay")).IsFalse();
    }

    [Test]
    public async Task Middleware_WhenSameKeyHasDifferentMethod_ReturnsConflict()
    {
        var repository = new InMemoryIdempotencyRepository();

        await InvokeMiddlewareAsync(
            repository,
            """{"title":"Original"}""",
            async context =>
            {
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"created":true}""");
            });

        var conflict = await InvokeMiddlewareAsync(
            repository,
            """{"title":"Original"}""",
            _ => throw new InvalidOperationException("Conflicting request should not invoke the next delegate."),
            method: HttpMethods.Put);

        await Assert.That(conflict.StatusCode).IsEqualTo(StatusCodes.Status409Conflict);
        await Assert.That(conflict.Body).Contains("idempotency_key_reuse");
        await Assert.That(conflict.Headers.ContainsKey("X-Idempotency-Replay")).IsFalse();
    }

    [Test]
    public async Task Middleware_WhenSameKeyHasDifferentPrincipal_ReturnsConflict()
    {
        var repository = new InMemoryIdempotencyRepository();

        await InvokeMiddlewareAsync(
            repository,
            """{"title":"Original"}""",
            async context =>
            {
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"created":true}""");
            });

        var conflict = await InvokeMiddlewareAsync(
            repository,
            """{"title":"Original"}""",
            _ => throw new InvalidOperationException("Conflicting request should not invoke the next delegate."),
            userId: "other-user");

        await Assert.That(conflict.StatusCode).IsEqualTo(StatusCodes.Status409Conflict);
        await Assert.That(conflict.Body).Contains("idempotency_key_reuse");
        await Assert.That(conflict.Headers.ContainsKey("X-Idempotency-Replay")).IsFalse();
    }

    [Test]
    public async Task Middleware_WhenSameKeyUsesDifferentTenant_DoesNotReplayAcrossTenant()
    {
        var repository = new InMemoryIdempotencyRepository();
        var firstTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
        var secondTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000002");
        var nextCallCount = 0;

        await InvokeMiddlewareAsync(
            repository,
            """{"title":"Original"}""",
            async context =>
            {
                nextCallCount++;
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"tenant":1}""");
            },
            tenantId: firstTenantId);

        var second = await InvokeMiddlewareAsync(
            repository,
            """{"title":"Original"}""",
            async context =>
            {
                nextCallCount++;
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"tenant":2}""");
            },
            tenantId: secondTenantId);

        await Assert.That(second.StatusCode).IsEqualTo(StatusCodes.Status201Created);
        await Assert.That(second.Body).IsEqualTo("""{"tenant":2}""");
        await Assert.That(second.Headers.ContainsKey("X-Idempotency-Replay")).IsFalse();
        await Assert.That(repository.Records.Count).IsEqualTo(2);
        await Assert.That(nextCallCount).IsEqualTo(2);
    }

    [Test]
    public async Task Middleware_WhenValidationFailureOccurs_DoesNotPersistRecord()
    {
        var repository = new InMemoryIdempotencyRepository();
        var nextCallCount = 0;

        var first = await InvokeMiddlewareAsync(
            repository,
            """{"title":"Invalid"}""",
            async context =>
            {
                nextCallCount++;
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync("""{"title":"Validation failed"}""");
            });

        var second = await InvokeMiddlewareAsync(
            repository,
            """{"title":"Corrected"}""",
            async context =>
            {
                nextCallCount++;
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"created":true}""");
            });

        await Assert.That(first.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(second.StatusCode).IsEqualTo(StatusCodes.Status201Created);
        await Assert.That(second.Headers.ContainsKey("X-Idempotency-Replay")).IsFalse();
        await Assert.That(repository.Records.Count).IsEqualTo(1);
        await Assert.That(nextCallCount).IsEqualTo(2);
    }

    [Test]
    public async Task Middleware_ForSvixAppPortalAccess_DoesNotPersistOrReplayShortLivedToken()
    {
        var repository = new InMemoryIdempotencyRepository();
        var nextCallCount = 0;

        var first = await InvokeMiddlewareAsync(
            repository,
            "{}",
            async context =>
            {
                nextCallCount++;
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"url":"https://svix.example/first"}""");
            },
            path: "/api/webhooks/svix/app-portal");

        var second = await InvokeMiddlewareAsync(
            repository,
            "{}",
            async context =>
            {
                nextCallCount++;
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"url":"https://svix.example/second"}""");
            },
            path: "/api/webhooks/svix/app-portal");

        await Assert.That(first.StatusCode).IsEqualTo(StatusCodes.Status200OK);
        await Assert.That(second.StatusCode).IsEqualTo(StatusCodes.Status200OK);
        await Assert.That(second.Body).IsEqualTo("""{"url":"https://svix.example/second"}""");
        await Assert.That(second.Headers.ContainsKey("X-Idempotency-Replay")).IsFalse();
        await Assert.That(repository.Records).IsEmpty();
        await Assert.That(nextCallCount).IsEqualTo(2);
    }

    private static async Task<MiddlewareResult> InvokeMiddlewareAsync(
        IIdempotencyRepository repository,
        string body,
        RequestDelegate next,
        string idempotencyKey = "same-key",
        string method = "POST",
        string path = "/api/idempotency-test",
        string contentType = "application/json; charset=utf-8",
        string? userId = "test-user",
        Guid? tenantId = null)
    {
        var effectiveTenantId = tenantId ?? Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
        var services = new ServiceCollection()
            .AddSingleton<ITenantContext>(new StaticTenantContext(effectiveTenantId))
            .AddSingleton(repository)
            .AddSingleton<IProblemDetailsService, JsonProblemDetailsService>()
            .BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = services,
            User = CreatePrincipal(userId)
        };

        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.ContentType = contentType;
        context.Request.Headers["Idempotency-Key"] = idempotencyKey;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Response.Body = new MemoryStream();

        var middleware = new IdempotencyMiddleware(
            next,
            new RecyclableMemoryStreamManager(),
            NullLogger<IdempotencyMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var responseBody = await reader.ReadToEndAsync();

        return new MiddlewareResult(
            context.Response.StatusCode,
            context.Response.Headers.ToDictionary(pair => pair.Key, pair => pair.Value.ToString()),
            responseBody);
    }

    private static ClaimsPrincipal CreatePrincipal(string? userId)
    {
        return userId is null
            ? new ClaimsPrincipal(new ClaimsIdentity())
            : new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", userId)],
                authenticationType: "Test"));
    }

    private sealed record MiddlewareResult(
        int StatusCode,
        Dictionary<string, string> Headers,
        string Body);

    private sealed class StaticTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
    }

    private sealed class InMemoryIdempotencyRepository : IIdempotencyRepository
    {
        public List<IdempotencyRecord> Records { get; } = [];

        public Task<IdempotencyRecord?> FindAsync(
            string key,
            Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Records.FirstOrDefault(record =>
                record.Key == key
                && record.TenantId == tenantId
                && record.ExpiresAt > DateTime.UtcNow));
        }

        public Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IdempotencyClaim> TryClaimAsync(
            IdempotencyRecord record,
            CancellationToken cancellationToken = default)
        {
            var existing = Records.FirstOrDefault(candidate =>
                candidate.Key == record.Key
                && candidate.TenantId == record.TenantId
                && candidate.ExpiresAt > DateTime.UtcNow);
            if (existing is not null)
            {
                return Task.FromResult(new IdempotencyClaim(existing, IsOwner: false));
            }

            Records.Add(record);
            return Task.FromResult(new IdempotencyClaim(record, IsOwner: true));
        }

        public Task<bool> CompleteAsync(
            Guid recordId,
            int statusCode,
            string? responseBody,
            string? contentType,
            CancellationToken cancellationToken = default)
        {
            var record = Records.FirstOrDefault(candidate => candidate.Id == recordId);
            if (record is null || record.StatusCode != IdempotencyRecord.InProgressStatusCode)
            {
                return Task.FromResult(false);
            }

            record.StatusCode = statusCode;
            record.ResponseBody = responseBody;
            record.ContentType = contentType;
            return Task.FromResult(true);
        }

        public Task<bool> ReleaseAsync(Guid recordId, CancellationToken cancellationToken = default)
        {
            var record = Records.FirstOrDefault(candidate => candidate.Id == recordId);
            if (record is null || record.StatusCode != IdempotencyRecord.InProgressStatusCode)
            {
                return Task.FromResult(false);
            }

            Records.Remove(record);
            return Task.FromResult(true);
        }

        public Task<int> CountExpiredAsync(
            DateTime expiresBeforeUtc,
            int batchSize,
            CancellationToken cancellationToken = default)
        {
            var count = Records
                .Where(record => record.ExpiresAt <= expiresBeforeUtc)
                .OrderBy(record => record.ExpiresAt)
                .Take(batchSize)
                .Count();

            return Task.FromResult(count);
        }

        public Task<int> DeleteExpiredAsync(
            DateTime expiresBeforeUtc,
            int batchSize,
            CancellationToken cancellationToken = default)
        {
            var expired = Records
                .Where(record => record.ExpiresAt <= expiresBeforeUtc)
                .OrderBy(record => record.ExpiresAt)
                .Take(batchSize)
                .ToArray();

            foreach (var record in expired)
            {
                Records.Remove(record);
            }

            return Task.FromResult(expired.Length);
        }
    }


    internal sealed class JsonProblemDetailsService : IProblemDetailsService
    {
        public async ValueTask WriteAsync(ProblemDetailsContext context)
        {
            context.HttpContext.Response.ContentType = "application/problem+json";
            await context.HttpContext.Response.WriteAsync(
                JsonSerializer.Serialize(context.ProblemDetails),
                context.HttpContext.RequestAborted);
        }

        public async ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            await WriteAsync(context);
            return true;
        }
    }
}
