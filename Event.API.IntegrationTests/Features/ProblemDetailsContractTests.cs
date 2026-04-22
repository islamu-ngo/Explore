// ABOUTME: Integration tests verifying ProblemDetails contract consistency.
// ABOUTME: Ensures GlobalExceptionHandler and ValidationExceptionHandler return RFC 7807 compliant responses.

using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Helpers;
using Explore.Application.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Event.Api.IntegrationTests.Features;

[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class ProblemDetailsContractTests
{
    private readonly ApiTestFixture _fixture;

    public ProblemDetailsContractTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GlobalExceptionHandler_OnNotFoundException_ReturnsProblemDetailsWithTraceIdAndTimestamp()
    {
        using var client = CreateClientThatThrows(new NotFoundException("Event", Guid.NewGuid()));

        var response = await client.GetAsync($"/api/actor/{Guid.NewGuid()}");

        await ProblemDetailsAssertions.AssertProblemDetailsAsync(response, HttpStatusCode.NotFound, "Resource not found");

        using var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        var root = document.RootElement;

        // Verify traceId and timestamp extensions are present
        await Assert.That(root.TryGetProperty("traceId", out var traceId)).IsTrue();
        await Assert.That(traceId.GetString()).IsNotNull();
        await Assert.That(traceId.GetString()!.Length > 0).IsTrue();

        await Assert.That(root.TryGetProperty("timestamp", out var timestamp)).IsTrue();
        await Assert.That(timestamp.GetString()).IsNotNull();

        // Verify RFC 7807 required fields
        await Assert.That(root.TryGetProperty("status", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("title", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("detail", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("type", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("instance", out _)).IsTrue();
    }

    [Test]
    public async Task GlobalExceptionHandler_OnBadRequestException_Returns400ProblemDetails()
    {
        using var client = CreateClientThatThrows(new BadRequestException("Invalid input data"));

        var response = await client.GetAsync($"/api/actor/{Guid.NewGuid()}");

        await ProblemDetailsAssertions.AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest, "Bad request");

        using var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        var detail = document.RootElement.GetProperty("detail").GetString();
        await Assert.That(detail).IsEqualTo("Invalid input data");
    }

    [Test]
    public async Task GlobalExceptionHandler_OnUnhandledException_SanitizesInternalDetails()
    {
        const string sensitiveMessage = "SQL connection to prod-db-01.internal failed";

        using var client = CreateClientThatThrows(new InvalidOperationException(sensitiveMessage));

        var response = await client.GetAsync($"/api/actor/{Guid.NewGuid()}");

        await ProblemDetailsAssertions.AssertProblemDetailsAsync(response, HttpStatusCode.InternalServerError, "Internal server error");

        using var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        var root = document.RootElement;

        var detail = root.GetProperty("detail").GetString() ?? string.Empty;
        await Assert.That(detail).DoesNotContain(sensitiveMessage);
        await Assert.That(root.TryGetProperty("stackTrace", out _)).IsFalse();
    }

    [Test]
    public async Task ValidationExceptionHandler_ReturnsErrorsKeyedByProperty()
    {
        // Use FluentValidation.ValidationException (not Application.Exceptions.ValidationException)
        // because the handler keys errors by property only for FluentValidation exceptions.
        var failures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Name", "Name is required."),
            new("Name", "Name must be at most 100 characters."),
            new("Email", "Email is not valid.")
        };

        using var client = CreateClientThatThrows(
            new FluentValidation.ValidationException(failures));

        var response = await client.GetAsync($"/api/actor/{Guid.NewGuid()}");

        await ProblemDetailsAssertions.AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest, "Validation failed");

        using var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        var root = document.RootElement;

        await Assert.That(root.TryGetProperty("errors", out var errors)).IsTrue();

        // FluentValidation errors are grouped by lowercase property name
        await Assert.That(errors.TryGetProperty("name", out var nameErrors)).IsTrue();
        await Assert.That(nameErrors.GetArrayLength()).IsEqualTo(2);

        await Assert.That(errors.TryGetProperty("email", out var emailErrors)).IsTrue();
        await Assert.That(emailErrors.GetArrayLength()).IsEqualTo(1);
    }

    [Test]
    public async Task ProblemDetails_ContentType_IsApplicationProblemJsonOrApplicationJson()
    {
        using var client = CreateClientThatThrows(new NotFoundException("Resource", Guid.NewGuid()));

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/actor/{Guid.NewGuid()}");
        request.Headers.Add("Accept", "application/json");

        var response = await client.SendAsync(request);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        var isValid = contentType is "application/problem+json" or "application/json";
        await Assert.That(isValid).IsTrue();
    }

    [Test]
    public async Task ProblemDetails_InstanceField_ContainsRequestPath()
    {
        var eventId = Guid.NewGuid();
        using var client = CreateClientThatThrows(new NotFoundException("Event", eventId));

        var response = await client.GetAsync($"/api/actor/{eventId}");

        using var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        var root = document.RootElement;

        await Assert.That(root.TryGetProperty("instance", out var instance)).IsTrue();
        var instanceValue = instance.GetString() ?? string.Empty;
        await Assert.That(instanceValue).Contains("/api/actor/");
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

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => throw exception;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw exception;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw exception;

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw exception;
    }
}
