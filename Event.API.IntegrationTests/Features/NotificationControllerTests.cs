// ABOUTME: API contract tests for authenticated notification command error responses.
// ABOUTME: Verifies notification write failures use RFC7807 ProblemDetails instead of anonymous JSON.

using System.Net;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Helpers;
using Explore.Application.Features.Notifications.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

public sealed class NotificationControllerTests
{
    [Test]
    public async Task Archive_WhenNotificationMissing_ReturnsNotFoundProblemDetails()
    {
        var notificationId = Guid.NewGuid();
        using var mediator = new NotificationMediatorStub(_ => NotFoundFailure("Notification was not found."));
        using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Patch,
            $"/api/notification/{notificationId}/archive");

        var response = await client.SendAsync(request);

        using var document = await AssertNotificationNotFoundProblemAsync(response);
        await Assert.That(document.RootElement.GetProperty("detail").GetString()).IsEqualTo("Notification was not found.");

        var command = mediator.LastRequest as ArchiveNotificationCommand;
        await Assert.That(command).IsNotNull();
        await Assert.That(command!.Id).IsEqualTo(notificationId);
        await Assert.That(command.Archive).IsTrue();
    }

    [Test]
    public async Task Snooze_WhenNotificationMissing_ReturnsNotFoundProblemDetails()
    {
        var notificationId = Guid.NewGuid();
        var snoozedUntil = DateTimeOffset.UtcNow.AddHours(2);
        using var mediator = new NotificationMediatorStub(_ => NotFoundFailure("Notification cannot be snoozed because it was not found."));
        using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Patch,
            $"/api/notification/{notificationId}/snooze?snoozedUntil={Uri.EscapeDataString(snoozedUntil.ToString("O"))}");

        var response = await client.SendAsync(request);

        using var document = await AssertNotificationNotFoundProblemAsync(response);
        await Assert.That(document.RootElement.GetProperty("detail").GetString()).IsEqualTo("Notification cannot be snoozed because it was not found.");

        var command = mediator.LastRequest as SnoozeNotificationCommand;
        await Assert.That(command).IsNotNull();
        await Assert.That(command!.Id).IsEqualTo(notificationId);
        await Assert.That(command.SnoozedUntil).IsNotNull();
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

    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(Guid.NewGuid()));
        return request;
    }

    private static async Task<System.Text.Json.JsonDocument> AssertNotificationNotFoundProblemAsync(HttpResponseMessage response)
    {
        await ProblemDetailsAssertions.AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Notification not found");

        var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        await Assert.That(document.RootElement.GetProperty("code").GetString()).IsEqualTo("notification_not_found");
        return document;
    }

    private static BaseCommandResponse<Guid> NotFoundFailure(string message) => new()
    {
        Success = false,
        Message = message,
        FailureCode = "notification_not_found",
        Errors = [message]
    };

    private sealed class NotificationMediatorStub(Func<object, BaseCommandResponse<Guid>> responseFactory) : IMediator, IDisposable
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
