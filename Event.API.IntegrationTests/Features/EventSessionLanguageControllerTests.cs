// ABOUTME: API contract tests for event-session language update behavior.
// ABOUTME: Verifies PATCH If-Match validation and route-ID command forwarding.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Helpers;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Features.EventSessionLanguages.Requests.Commands;
using Explore.Application.Features.EventSessionLanguages.Requests.Queries;
using Explore.Application.Features.EventSessions.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Event.Api.IntegrationTests.Features;

public sealed class EventSessionLanguageControllerTests
{
    [Test]
    public async Task Update_WhenIfMatchIsMissing_ReturnsValidationProblemDetails()
    {
        using var mediator = new EventSessionLanguageMediatorStub(_ => throw new InvalidOperationException("Mediator should not run when If-Match is missing."));
        await using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedJsonRequest(
            HttpMethod.Patch,
            "/api/eventsessionlanguage/7",
            CreateUpdateDto());

        var response = await client.SendAsync(request);

        await ProblemDetailsAssertions.AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "One or more validation errors occurred.");
        await Assert.That(mediator.LastRequest).IsNull();
    }

    [Test]
    public async Task Update_WhenCommandValidationFails_ReturnsValidationProblemDetails()
    {
        var eventSessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        using var mediator = new EventSessionLanguageMediatorStub(request => request switch
        {
            GetEventSessionLanguageDetailsRequest => new EventSessionLanguageDto
            {
                Id = 7,
                EventSessionId = eventSessionId,
                LanguageId = 1,
                ConcurrencyStamp = Guid.NewGuid()
            },
            GetEventSessionDetailsRequest => new EventSessionDto
            {
                Id = eventSessionId,
                EventId = eventId,
                EventTitle = "Session parent event",
                ConcurrencyStamp = Guid.NewGuid()
            },
            UpdateEventSessionLanguageCommand => new BaseCommandResponse<int>
            {
                Success = false,
                Message = "Event Session Language update failed.",
                Errors = ["Language not found."]
            },
            _ => throw new InvalidOperationException($"Unexpected request: {request.GetType().Name}")
        });
        await using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();
        var concurrencyStamp = Guid.NewGuid();
        using var request = CreateAuthenticatedJsonRequest(
            HttpMethod.Patch,
            "/api/eventsessionlanguage/7",
            CreateUpdateDto(),
            ifMatch: concurrencyStamp);

        var response = await client.SendAsync(request);

        await ProblemDetailsAssertions.AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "Program validation failed");

        var command = mediator.LastRequest as UpdateEventSessionLanguageCommand;
        await Assert.That(command).IsNotNull();
        await Assert.That(command!.EventSessionLanguageId).IsEqualTo(7);
        await Assert.That(command.EventSessionId).IsEqualTo(eventSessionId);
        await Assert.That(command.EventId).IsEqualTo(eventId);
        await Assert.That(command.ExpectedConcurrencyStamp).IsEqualTo(concurrencyStamp);
    }

    private static WebApplicationFactory<Program> CreateFactoryWithMediator(IMediator mediator)
    {
        var factory = new AuthenticatedWebApplicationFactory();

        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMediator>();
                services.AddSingleton(mediator);
            });
        });
    }

    private static HttpRequestMessage CreateAuthenticatedJsonRequest<TValue>(
        HttpMethod method,
        string url,
        TValue body,
        Guid? ifMatch = null)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(Guid.NewGuid()));
        if (ifMatch.HasValue)
        {
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{ifMatch.Value:D}\"");
        }

        return request;
    }

    private static UpdateEventSessionLanguageDto CreateUpdateDto() => new()
    {
        Language = new UpdateEventSessionLanguageLanguageDto { LanguageId = 2 }
    };

    private sealed class EventSessionLanguageMediatorStub(Func<object, object> responseFactory) : IMediator, IDisposable
    {
        public object? LastRequest { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            object response = responseFactory(request);
            return Task.FromResult((TResponse)response);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult<object?>(responseFactory(request));
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
