// ABOUTME: API contract tests for authenticated EmailDispatch operator replay and park actions.
// ABOUTME: Verifies route metadata, MediatR command dispatch, and RFC7807 transition failure mapping.

using System.Net;
using System.Reflection;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Controllers;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.Application.DTOs.EmailDispatch;
using Explore.Application.Hateoas;
using Explore.Application.Features.EmailDispatch;
using Explore.Application.Features.EmailDispatch.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

public sealed class EmailDispatchAdminControllerTests
{
    [Test]
    public async Task ParkDispatch_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var mediator = new EmailDispatchMediatorStub(_ => Success(Guid.NewGuid()));
        using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();

        var response = await client.PutAsync(
            $"/api/admin/email-dispatch/tenants/{Guid.NewGuid()}/outbox/{Guid.NewGuid()}/park?reason=unsafe",
            content: null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ParkDispatch_WithAuthentication_DispatchesCommandAndReturnsSuccess()
    {
        var tenantId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        const string reason = "Provider payload needs manual review.";
        using var mediator = new EmailDispatchMediatorStub(_ => Success(outboxId));
        using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Put,
            $"/api/admin/email-dispatch/tenants/{tenantId}/outbox/{outboxId}/park?reason={Uri.EscapeDataString(reason)}");

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var command = mediator.LastRequest as ParkEmailDispatchCommand;
        await Assert.That(command).IsNotNull();
        await Assert.That(command!.TenantId).IsEqualTo(tenantId);
        await Assert.That(command.OutboxId).IsEqualTo(outboxId);
        await Assert.That(command.Reason).IsEqualTo(reason);
    }

    [Test]
    public async Task ReplayDispatch_WhenInvalidTransition_ReturnsConflictProblemDetails()
    {
        var tenantId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        using var mediator = new EmailDispatchMediatorStub(_ => Failure(
            "Sent email dispatch rows cannot be replayed.",
            EmailDispatchFailureCodes.InvalidTransition));
        using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/admin/email-dispatch/tenants/{tenantId}/outbox/{outboxId}/replay");

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/problem+json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        await Assert.That(root.GetProperty("status").GetInt32()).IsEqualTo((int)HttpStatusCode.Conflict);
        await Assert.That(root.GetProperty("title").GetString()).IsEqualTo("Email dispatch state transition conflict");
        await Assert.That(root.GetProperty("code").GetString()).IsEqualTo(EmailDispatchFailureCodes.InvalidTransition);
        await Assert.That(root.TryGetProperty("traceId", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("timestamp", out _)).IsTrue();
    }

    [Test]
    public async Task ReplayAndParkRoutes_UseStableRouteNamesAndWritePolicy()
    {
        MethodInfo park = typeof(EmailDispatchAdminController).GetMethod(nameof(EmailDispatchAdminController.ParkDispatch))!;
        MethodInfo replay = typeof(EmailDispatchAdminController).GetMethod(nameof(EmailDispatchAdminController.ReplayDispatch))!;

        var parkRoute = park.GetCustomAttribute<HttpPutAttribute>();
        var replayRoute = replay.GetCustomAttribute<HttpPostAttribute>();
        await Assert.That(parkRoute).IsNotNull();
        await Assert.That(parkRoute!.Name).IsEqualTo(RouteNames.ParkEmailDispatch);
        await Assert.That(parkRoute.Template).IsEqualTo("tenants/{tenantId:guid}/outbox/{outboxId:guid}/park");
        await Assert.That(replayRoute).IsNotNull();
        await Assert.That(replayRoute!.Name).IsEqualTo(RouteNames.ReplayEmailDispatch);
        await Assert.That(replayRoute.Template).IsEqualTo("tenants/{tenantId:guid}/outbox/{outboxId:guid}/replay");

        await Assert.That(GetRateLimitPolicy(park)).IsEqualTo(RateLimitingExtensions.WritePolicy);
        await Assert.That(GetRateLimitPolicy(replay)).IsEqualTo(RateLimitingExtensions.WritePolicy);
    }

    [Test]
    public async Task GetStatusRoute_ReturnsHalCollectionResource()
    {
        MethodInfo getStatus = typeof(EmailDispatchAdminController).GetMethod(nameof(EmailDispatchAdminController.GetStatus))!;

        var route = getStatus.GetCustomAttribute<HttpGetAttribute>();
        await Assert.That(route).IsNotNull();
        await Assert.That(route!.Name).IsEqualTo(RouteNames.GetEmailDispatchStatus);
        await Assert.That(route.Template).IsEqualTo("status");
        await Assert.That(getStatus.ReturnType).IsEqualTo(typeof(Task<ActionResult<HalCollectionResource<EmailDispatchStatusDto>>>));
        await Assert.That(GetRateLimitPolicy(getStatus)).IsEqualTo(RateLimitingExtensions.AuthenticatedPolicy);
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

    private static string? GetRateLimitPolicy(MethodInfo method)
        => method.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName;

    private static BaseCommandResponse<Guid> Success(Guid id) => new()
    {
        Id = id,
        Success = true,
        Message = "Email dispatch operation completed."
    };

    private static BaseCommandResponse<Guid> Failure(string message, string failureCode) => new()
    {
        Success = false,
        Message = message,
        FailureCode = failureCode,
        Errors = [message]
    };

    private sealed class EmailDispatchMediatorStub(Func<object, BaseCommandResponse<Guid>> responseFactory) : IMediator, IDisposable
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
