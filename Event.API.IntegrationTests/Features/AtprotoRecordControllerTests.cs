// ABOUTME: API contract tests for authenticated AT Protocol record command error responses.
// ABOUTME: Verifies update failures use RFC7807 ProblemDetails instead of anonymous JSON/envelopes.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Helpers;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Features.AtprotoRecords.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

public sealed class AtprotoRecordControllerTests
{
    [Test]
    public async Task Update_WhenRouteAndBodyIdsDiffer_ReturnsValidationProblemDetails()
    {
        var routeId = Guid.NewGuid();
        var bodyId = Guid.NewGuid();
        using var mediator = new AtprotoRecordMediatorStub(_ => throw new InvalidOperationException("Mediator should not run for ID mismatch."));
        using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedJsonRequest(
            HttpMethod.Put,
            $"/api/atprotorecord/{routeId}",
            CreateUpdateDto(bodyId));

        var response = await client.SendAsync(request);

        using var document = await AssertAtprotoRecordValidationProblemAsync(
            response,
            "AT Protocol record ID mismatch.",
            "AT Protocol record ID mismatch.");
        await Assert.That(mediator.LastRequest).IsNull();
    }

    [Test]
    public async Task Update_WhenCommandValidationFails_ReturnsValidationProblemDetails()
    {
        var recordId = Guid.NewGuid();
        using var mediator = new AtprotoRecordMediatorStub(_ => new BaseCommandResponse<Guid>
        {
            Success = false,
            Message = "AtprotoRecord update failed.",
            Errors = ["Record key is required."]
        });
        using var factory = CreateFactoryWithMediator(mediator);
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedJsonRequest(
            HttpMethod.Put,
            $"/api/atprotorecord/{recordId}",
            CreateUpdateDto(recordId));

        var response = await client.SendAsync(request);

        using var document = await AssertAtprotoRecordValidationProblemAsync(
            response,
            "AtprotoRecord update failed.",
            "Record key is required.");

        var command = mediator.LastRequest as UpdateAtprotoRecordCommand;
        await Assert.That(command).IsNotNull();
        await Assert.That(command!.AtprotoRecordDto.Id).IsEqualTo(recordId);
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

    private static HttpRequestMessage CreateAuthenticatedJsonRequest<TValue>(HttpMethod method, string url, TValue body)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(Guid.NewGuid()));
        return request;
    }

    private static UpdateAtprotoRecordDto CreateUpdateDto(Guid id) => new()
    {
        Id = id,
        Did = "did:plc:testrecord",
        Collection = "app.bsky.feed.post",
        RecordKey = "record-key"
    };

    private static async Task<System.Text.Json.JsonDocument> AssertAtprotoRecordValidationProblemAsync(
        HttpResponseMessage response,
        string expectedDetail,
        string expectedError)
    {
        await ProblemDetailsAssertions.AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "AT Protocol record validation failed");

        var document = await ProblemDetailsAssertions.ReadAsJsonAsync(response);
        var root = document.RootElement;
        await Assert.That(root.GetProperty("detail").GetString()).IsEqualTo(expectedDetail);
        await Assert.That(root.GetProperty("code").GetString()).IsEqualTo("validation_failed");

        var errors = root.GetProperty("errors").GetProperty("atprotoRecord");
        await Assert.That(errors.GetArrayLength()).IsEqualTo(1);
        await Assert.That(errors[0].GetString()).IsEqualTo(expectedError);
        return document;
    }

    private sealed class AtprotoRecordMediatorStub(Func<object, BaseCommandResponse<Guid>> responseFactory) : IMediator, IDisposable
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
