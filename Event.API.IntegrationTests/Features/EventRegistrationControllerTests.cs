// ABOUTME: API contract tests for authenticated event registration update error responses.
// ABOUTME: Verifies update failures use RFC7807 ProblemDetails instead of raw command envelopes.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Helpers;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

public sealed class EventRegistrationControllerTests
{
    [Test]
    public async Task Create_WhenEventScopeOmitsSelectedSessions_ReachesCommandHandler()
    {
        var eventId = Guid.NewGuid();
        var authenticatedUserId = Guid.NewGuid();
        using var mediator = new EventRegistrationMediatorStub(_ => new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = Guid.NewGuid(),
            Message = "Event Registration created successfully."
        });
        await using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedJsonRequest(
            HttpMethod.Post,
            "/api/eventregistration",
            new CreateEventRegistrationDto
            {
                EventId = eventId,
                UserId = Guid.NewGuid(),
                RegistrationScopeId = 1,
                SelectedSessionIds = null
            },
            authenticatedUserId);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var command = mediator.LastRequest as CreateEventRegistrationCommand;
        await Assert.That(command).IsNotNull();
        await Assert.That(command!.EventRegistrationDto.EventId).IsEqualTo(eventId);
        await Assert.That(command.EventRegistrationDto.UserId).IsEqualTo(authenticatedUserId);
        await Assert.That(command.EventRegistrationDto.SelectedSessionIds).IsNull();
    }

    [Test]
    public async Task Update_WhenIfMatchIsMissing_ReturnsValidationProblemDetails()
    {
        var registrationId = Guid.NewGuid();
        using var mediator = new EventRegistrationMediatorStub(_ => throw new InvalidOperationException("Mediator should not run when If-Match is missing."));
        await using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedJsonRequest(
            HttpMethod.Patch,
            $"/api/eventregistration/{registrationId}",
            CreateUpdateDto());

        var response = await client.SendAsync(request);

        using var document = await AssertEventRegistrationValidationProblemAsync(
            response,
            "If-Match header is required and must contain the current event registration concurrency stamp.",
            "If-Match header is required and must contain the current event registration concurrency stamp.");
        await Assert.That(mediator.LastRequest).IsNull();
    }

    [Test]
    public async Task Update_WhenCommandValidationFails_ReturnsValidationProblemDetails()
    {
        var registrationId = Guid.NewGuid();
        using var mediator = new EventRegistrationMediatorStub(_ => new BaseCommandResponse<Guid>
        {
            Success = false,
            Message = "Event Registration update failed.",
            Errors = ["UserId not found"]
        });
        await using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();
        var concurrencyStamp = Guid.NewGuid();
        using var request = CreateAuthenticatedJsonRequest(
            HttpMethod.Patch,
            $"/api/eventregistration/{registrationId}",
            CreateUpdateDto(),
            ifMatch: concurrencyStamp);

        var response = await client.SendAsync(request);

        using var document = await AssertEventRegistrationValidationProblemAsync(
            response,
            "Event Registration update failed.",
            "UserId not found");

        var command = mediator.LastRequest as UpdateEventRegistrationCommand;
        await Assert.That(command).IsNotNull();
        await Assert.That(command!.EventRegistrationId).IsEqualTo(registrationId);
        await Assert.That(command.ExpectedConcurrencyStamp).IsEqualTo(concurrencyStamp);
    }

    [Test]
    public async Task Update_WhenRegistrationIsMissing_ReturnsNotFoundProblemDetails()
    {
        var registrationId = Guid.NewGuid();
        using var mediator = new EventRegistrationMediatorStub(_ => new BaseCommandResponse<Guid>
        {
            Success = false,
            Message = "Event Registration not found."
        });
        await using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();
        var concurrencyStamp = Guid.NewGuid();
        using var request = CreateAuthenticatedJsonRequest(
            HttpMethod.Patch,
            $"/api/eventregistration/{registrationId}",
            CreateUpdateDto(),
            ifMatch: concurrencyStamp);

        var response = await client.SendAsync(request);

        using var document = await AssertEventRegistrationNotFoundProblemAsync(response);

        var command = mediator.LastRequest as UpdateEventRegistrationCommand;
        await Assert.That(command).IsNotNull();
        await Assert.That(command!.EventRegistrationId).IsEqualTo(registrationId);
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
        Guid? userId = null,
        Guid? ifMatch = null)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(userId ?? Guid.NewGuid()));
        if (ifMatch.HasValue)
        {
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{ifMatch.Value:D}\"");
        }

        return request;
    }

    private static UpdateEventRegistrationDto CreateUpdateDto() => new()
    {
        ApprovalStatus = new UpdateEventRegistrationApprovalStatusDto
        {
            ApprovalStatusId = Explore.Application.Models.Common.OptionalUpdate<int?>.Set(1)
        }
    };

    private static async Task<System.Text.Json.JsonDocument> AssertEventRegistrationValidationProblemAsync(
        HttpResponseMessage response,
        string expectedDetail,
        string expectedError)
    {
        await ProblemDetailsAssertions.AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "Event registration validation failed");
        await AssertProblemJsonContentTypeAsync(response);

        var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        var root = document.RootElement;
        await Assert.That(root.GetProperty("detail").GetString()).IsEqualTo(expectedDetail);
        await Assert.That(root.GetProperty("code").GetString()).IsEqualTo("validation_failed");

        var errors = root.GetProperty("errors").GetProperty("eventRegistration");
        await Assert.That(errors.GetArrayLength()).IsEqualTo(1);
        await Assert.That(errors[0].GetString()).IsEqualTo(expectedError);
        return document;
    }

    private static async Task<System.Text.Json.JsonDocument> AssertEventRegistrationNotFoundProblemAsync(HttpResponseMessage response)
    {
        await ProblemDetailsAssertions.AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Event registration not found");
        await AssertProblemJsonContentTypeAsync(response);

        var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        var root = document.RootElement;
        await Assert.That(root.GetProperty("detail").GetString()).IsEqualTo("Event registration not found.");
        await Assert.That(root.GetProperty("code").GetString()).IsEqualTo("resource_not_found");
        return document;
    }

    private static async Task AssertProblemJsonContentTypeAsync(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        await Assert.That(contentType).IsEqualTo("application/problem+json");
    }

    private sealed class EventRegistrationMediatorStub(Func<object, BaseCommandResponse<Guid>> responseFactory) : IMediator, IDisposable
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

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

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
