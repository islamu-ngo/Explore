using System.Net;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Helpers;
using Explore.Application.Exceptions;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class ExceptionHandlingIntegrationTests
{
    private readonly ApiTestFixture _fixture;

    public ExceptionHandlingIntegrationTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task ExceptionPipeline_WhenMediatorThrowsValidationException_ReturnsProblemDetailsBadRequest()
    {
        var validationResult = new ValidationResult([
            new ValidationFailure("ApprovalStatusId", "ApprovalStatusId does not exist.")
        ]);

        using var client = CreateClientThatThrows(new Explore.Application.Exceptions.ValidationException(validationResult));
        var response = await client.GetAsync($"/api/actor/{Guid.NewGuid()}");

        await ProblemDetailsAssertions.AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest, "Validation failed");

        using var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        var root = document.RootElement;

        await Assert.That(root.TryGetProperty("errors", out var errors)).IsTrue();
        await Assert.That(errors.TryGetProperty("validation", out _)).IsTrue();
    }

    [Test]
    public async Task ExceptionPipeline_WhenMediatorThrowsNotFoundException_ReturnsProblemDetailsNotFound()
    {
        using var client = CreateClientThatThrows(new NotFoundException("Organization", Guid.NewGuid()));
        var response = await client.GetAsync($"/api/actor/{Guid.NewGuid()}");

        await ProblemDetailsAssertions.AssertProblemDetailsAsync(response, HttpStatusCode.NotFound, "Resource not found");

        using var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        var detail = document.RootElement.GetProperty("detail").GetString() ?? string.Empty;
        await Assert.That(detail).Contains("was not found");
    }

    [Test]
    public async Task ExceptionPipeline_WhenMediatorThrowsUnhandledException_ReturnsSanitizedProblemDetails()
    {
        const string sensitiveMessage = "Sensitive internals should not be exposed";

        using var client = CreateClientThatThrows(new Exception(sensitiveMessage));
        var response = await client.GetAsync($"/api/actor/{Guid.NewGuid()}");

        await ProblemDetailsAssertions.AssertProblemDetailsAsync(response, HttpStatusCode.InternalServerError, "Internal server error");

        using var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        var root = document.RootElement;

        var detail = root.GetProperty("detail").GetString() ?? string.Empty;
        await Assert.That(detail).IsEqualTo("An unexpected error occurred.");
        await Assert.That(detail).DoesNotContain(sensitiveMessage);
        await Assert.That(root.TryGetProperty("stackTrace", out _)).IsFalse();
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
        {
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw exception;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            throw exception;
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw exception;
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw exception;
        }
    }
}
