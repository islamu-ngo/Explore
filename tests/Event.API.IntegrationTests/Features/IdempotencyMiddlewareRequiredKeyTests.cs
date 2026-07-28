// ABOUTME: Focused middleware tests for endpoints that require an Idempotency-Key request header.
// ABOUTME: Verifies required-key RFC 7807 failures without Docker-backed API fixture dependencies.

namespace Event.Api.IntegrationTests.Features;

using System.Text;
using Explore.API.Attributes;
using Explore.API.Middleware;
using Explore.API.OpenApi;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IO;
using Microsoft.OpenApi;

public class IdempotencyMiddlewareTests
{
    [Test]
    public async Task Middleware_WhenRequiredKeyIsMissing_ReturnsProblemDetailsWithoutInvokingNext()
    {
        var result = await InvokeAsync(idempotencyKey: null);

        await Assert.That(result.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(result.ContentType).IsEqualTo("application/problem+json");
        await Assert.That(result.Body).Contains("Idempotency-Key is required.");
        await Assert.That(result.NextCallCount).IsEqualTo(0);
    }

    [Test]
    public async Task Middleware_WhenRequiredKeyIsWhitespace_ReturnsProblemDetailsWithoutInvokingNext()
    {
        var result = await InvokeAsync(idempotencyKey: " \t");

        await Assert.That(result.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(result.ContentType).IsEqualTo("application/problem+json");
        await Assert.That(result.Body).Contains("Idempotency-Key is required.");
        await Assert.That(result.NextCallCount).IsEqualTo(0);
    }

    [Test]
    public async Task Middleware_WhenRequiredAiMessageKeyIsMissingOrWhitespace_ReturnsProblemDetailsWithoutInvokingNext()
    {
        var missing = await InvokeAsync(idempotencyKey: null, path: "/api/ai/assistant/conversations/test/messages");
        var whitespace = await InvokeAsync(idempotencyKey: " \t", path: "/api/ai/assistant/conversations/test/messages");

        await Assert.That(missing.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(missing.ContentType).IsEqualTo("application/problem+json");
        await Assert.That(missing.Body).Contains("Idempotency-Key is required.");
        await Assert.That(missing.NextCallCount).IsEqualTo(0);
        await Assert.That(whitespace.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(whitespace.ContentType).IsEqualTo("application/problem+json");
        await Assert.That(whitespace.Body).Contains("Idempotency-Key is required.");
        await Assert.That(whitespace.NextCallCount).IsEqualTo(0);
    }

    [Test]
    public async Task Middleware_WhenUnmarkedAiMessageHasKey_PreservesApplicationManagedBypass()
    {
        var result = await InvokeAsync(
            idempotencyKey: "application-managed-key",
            requiresIdempotencyKey: false,
            path: "/api/ai/assistant/conversations/test/messages");

        await Assert.That(result.StatusCode).IsEqualTo(StatusCodes.Status201Created);
        await Assert.That(result.NextCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task OpenApiMetadata_WhenIdempotencyKeyIsRequired_EmitsTrueExtension()
    {
        var operation = new OpenApiOperation();

        var applied = EndpointClassificationTransformer.ApplyIdempotencyKeyRequirement(
            operation,
            [new RequireIdempotencyKeyAttribute()]);

        await Assert.That(applied).IsTrue();
        var extension = operation.Extensions!["x-idempotency-key-required"] as JsonNodeExtension;
        await Assert.That(extension).IsNotNull();
        await Assert.That(extension!.Node.GetValue<bool>()).IsTrue();
    }

    [Test]
    public async Task Middleware_WhenKeyIsOptionalAndMissing_PassesThroughNormally()
    {
        var result = await InvokeAsync(idempotencyKey: null, requiresIdempotencyKey: false);

        await Assert.That(result.StatusCode).IsEqualTo(StatusCodes.Status201Created);
        await Assert.That(result.NextCallCount).IsEqualTo(1);
    }

    private static async Task<MiddlewareResult> InvokeAsync(
        string? idempotencyKey,
        bool requiresIdempotencyKey = true,
        string path = "/api/idempotency-test")
    {
        var services = new ServiceCollection()
            .AddSingleton<ITenantContext>(new StaticTenantContext(Guid.CreateVersion7()))
            .AddSingleton<IIdempotencyRepository, InMemoryIdempotencyRepository>()
            .AddSingleton<IProblemDetailsService, IdempotencyMiddlewareRealRuntimeTests.JsonProblemDetailsService>()
            .BuildServiceProvider();
        var nextCallCount = 0;
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };

        context.Request.Method = HttpMethods.Post;
        context.Request.Path = path;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        context.Response.Body = new MemoryStream();
        if (idempotencyKey is not null)
        {
            context.Request.Headers["Idempotency-Key"] = idempotencyKey;
        }

        if (requiresIdempotencyKey)
        {
            context.SetEndpoint(new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(new RequireIdempotencyKeyAttribute()),
                "required-idempotency-key"));
        }

        var middleware = new IdempotencyMiddleware(
            httpContext =>
            {
                nextCallCount++;
                httpContext.Response.StatusCode = StatusCodes.Status201Created;
                return Task.CompletedTask;
            },
            new RecyclableMemoryStreamManager(),
            NullLogger<IdempotencyMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return new MiddlewareResult(
            context.Response.StatusCode,
            context.Response.ContentType,
            await reader.ReadToEndAsync(),
            nextCallCount);
    }

    private sealed record MiddlewareResult(int StatusCode, string? ContentType, string Body, int NextCallCount);

    private sealed class StaticTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
    }

    private sealed class InMemoryIdempotencyRepository : IIdempotencyRepository
    {
        public Task<IdempotencyRecord?> FindAsync(string key, Guid tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IdempotencyRecord?>(null);

        public Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> CountExpiredAsync(DateTime expiresBeforeUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> DeleteExpiredAsync(DateTime expiresBeforeUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

}
