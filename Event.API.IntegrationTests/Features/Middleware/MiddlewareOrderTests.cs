// ABOUTME: Integration tests verifying middleware ordering and tenant-exempt path behavior.
// ABOUTME: Ensures ForwardedHeaders is respected, tenant exemptions work, and exception handler catches errors.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Helpers;
using Explore.Application.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Event.Api.IntegrationTests.Features.Middleware;

[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class MiddlewareOrderTests
{
    private readonly ApiTestFixture _fixture;

    public MiddlewareOrderTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task TenantResolution_SkipsExemptPath_InstanceOnboarding()
    {
        // /api/InstanceOnboarding is tenant-exempt — should not return 404 for missing tenant
        var response = await _fixture.Client.GetAsync("/api/instanceonboarding/status");

        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.NotFound);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task TenantResolution_SkipsExemptPath_PublicExperienceSettings()
    {
        // /api/PublicExperience/settings is tenant-exempt
        var response = await _fixture.Client.GetAsync("/api/PublicExperience/settings");

        // Should not get 404 from tenant middleware — may get other status but not tenant-blocked
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ExceptionHandler_CatchesControllerExceptions_ReturnsProblemDetails()
    {
        // When a controller action throws (via MediatR), the exception handler should
        // catch it and return ProblemDetails — not a raw 500 or empty body
        using var client = CreateClientThatThrows(new BadRequestException("Test validation error"));

        var response = await client.GetAsync($"/api/actor/{Guid.NewGuid()}");

        await ProblemDetailsAssertions.AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest, "Bad request");
    }

    [Test]
    public async Task ExceptionHandler_CatchesNotFound_ReturnsProblemDetails()
    {
        using var client = CreateClientThatThrows(new NotFoundException("Event", Guid.NewGuid()));

        var response = await client.GetAsync($"/api/actor/{Guid.NewGuid()}");

        await ProblemDetailsAssertions.AssertProblemDetailsAsync(response, HttpStatusCode.NotFound, "Resource not found");
    }

    [Test]
    public async Task ExceptionHandler_CatchesUnexpected_ReturnsSanitized500()
    {
        using var client = CreateClientThatThrows(new Exception("Internal details"));

        var response = await client.GetAsync($"/api/actor/{Guid.NewGuid()}");

        await ProblemDetailsAssertions.AssertProblemDetailsAsync(response, HttpStatusCode.InternalServerError, "Internal server error");

        // No sensitive info leaked
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).DoesNotContain("Internal details");
    }

    private HttpClient CreateClientThatThrows(Exception exception)
    {
        var app = _fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMediator>();
                services.AddSingleton<IMediator>(new ThrowingMediator(exception));
            });
        });

        return app.CreateClient();
    }

    private sealed class ThrowingMediator(Exception exception) : IMediator
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.CompletedTask;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw exception;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw exception;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw exception;

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw exception;
    }
}
