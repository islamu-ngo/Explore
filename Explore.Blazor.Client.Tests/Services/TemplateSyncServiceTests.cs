// ABOUTME: Focused tests for client-side event and session template sync HTTP service wrappers.
// ABOUTME: Verifies ApiClientExecutor-backed routing, response parsing, and failure behavior.

using System.Net;
using System.Text;
using System.Text.Json;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.Responses;
using Explore.Blazor.Client.Services.EventSessionTemplateSync;
using Explore.Blazor.Client.Services.EventTemplateSync;
using Refit;
using EventApplyRequest = Explore.Blazor.Client.Models.EventTemplateSync.EventTemplateSyncApplyRequest;
using EventHistoryItem = Explore.Blazor.Client.Models.EventTemplateSync.EventTemplateSyncHistoryItemDto;
using EventPlan = Explore.Blazor.Client.Models.EventTemplateSync.TemplateSyncPlanDto;
using EventTemplateDiff = Explore.Blazor.Client.Models.EventTemplateSync.TemplateDiffDto;
using EventTemplateOutcome = Explore.Blazor.Client.Models.EventTemplateSync.TemplateSyncOutcomeDto;
using SessionApplyRequest = Explore.Blazor.Client.Models.EventSessionTemplateSync.EventSessionTemplateSyncApplyRequest;
using SessionHistoryItem = Explore.Blazor.Client.Models.EventSessionTemplateSync.EventSessionTemplateSyncHistoryItemDto;
using SessionPlan = Explore.Blazor.Client.Models.EventSessionTemplateSync.TemplateSyncPlanDto;
using SessionTemplateOutcome = Explore.Blazor.Client.Models.EventSessionTemplateSync.TemplateSyncOutcomeDto;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class TemplateSyncServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Test]
    public async Task EventTemplateSyncService_GetDiffAsync_ReadsExpectedRoute()
    {
        using var handler = new RecordingHandler(_ => CreateJsonResponse(CreateEventDiff()));
        var service = new EventTemplateSyncService(CreateEventApi(handler));
        var eventId = Guid.NewGuid();

        EventTemplateDiff? result = await service.GetDiffAsync(eventId, 7);

        await Assert.That(result).IsNotNull();
        await Assert.That(handler.LastRequest?.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(handler.LastRequest?.RequestUri?.PathAndQuery)
            .IsEqualTo($"/api/events/{eventId}/template-sync/diff?templateVersion=7");
    }

    [Test]
    public async Task EventTemplateSyncService_ApplySyncAsync_ReadsCommandResponse()
    {
        var outcome = new EventTemplateOutcome([], [], [], 8, DateTimeOffset.UtcNow);
        var response = new BaseCommandResponse<EventTemplateOutcome>
        {
            Success = true,
            Message = "Applied",
            Id = outcome
        };
        using var handler = new RecordingHandler(_ => CreateJsonResponse(response));
        var service = new EventTemplateSyncService(CreateEventApi(handler));
        var eventId = Guid.NewGuid();

        BaseCommandResponse<EventTemplateOutcome> result = await service.ApplySyncAsync(
            eventId,
            new EventApplyRequest(new EventPlan { TargetTemplateVersion = 8 }, 7));

        await Assert.That(result.Success).IsTrue();
        await Assert.That(handler.LastRequest?.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(handler.LastRequest?.RequestUri?.PathAndQuery)
            .IsEqualTo($"/api/events/{eventId}/template-sync/apply");
    }

    [Test]
    public async Task EventTemplateSyncService_GetHistoryAsync_ThrowsWhenApiFails()
    {
        using var handler = new RecordingHandler(_ => CreateProblemResponse(HttpStatusCode.Conflict));
        var service = new EventTemplateSyncService(CreateEventApi(handler));

        await Assert.That(async () => await service.GetHistoryAsync(Guid.NewGuid()))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task EventTemplateSyncService_ApplySyncAsync_WithJsonNullBody_ThrowsPreviousApplyMessage()
    {
        using var handler = new RecordingHandler(_ => CreateJsonNullResponse());
        var service = new EventTemplateSyncService(CreateEventApi(handler));

        InvalidOperationException? exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.ApplySyncAsync(
                Guid.NewGuid(),
                new EventApplyRequest(new EventPlan { TargetTemplateVersion = 8 }, 7)));

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).IsEqualTo("Failed to read apply response.");
    }

    [Test]
    public async Task EventTemplateSyncService_GetHistoryAsync_WithJsonNullBody_ThrowsPreviousHistoryMessage()
    {
        using var handler = new RecordingHandler(_ => CreateJsonNullResponse());
        var service = new EventTemplateSyncService(CreateEventApi(handler));

        InvalidOperationException? exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GetHistoryAsync(Guid.NewGuid()));

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).IsEqualTo("Failed to read history response.");
    }

    [Test]
    public async Task EventSessionTemplateSyncService_GetHistoryAsync_ReadsExpectedRoute()
    {
        var response = new PaginatedResult<SessionHistoryItem>
        {
            Items = [],
            PageNumber = 2,
            PageSize = 5,
            TotalCount = 0
        };
        using var handler = new RecordingHandler(_ => CreateJsonResponse(response));
        var service = new EventSessionTemplateSyncService(CreateSessionApi(handler));
        var sessionId = Guid.NewGuid();

        PaginatedResult<SessionHistoryItem> result = await service.GetHistoryAsync(sessionId, page: 2, pageSize: 5);

        await Assert.That(result.PageNumber).IsEqualTo(2);
        await Assert.That(handler.LastRequest?.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(handler.LastRequest?.RequestUri?.PathAndQuery)
            .IsEqualTo($"/api/event-sessions/{sessionId}/template-sync/history?page=2&pageSize=5");
    }

    [Test]
    public async Task EventSessionTemplateSyncService_ApplySyncAsync_ReadsCommandResponse()
    {
        var outcome = new SessionTemplateOutcome([], [], [], 9, DateTimeOffset.UtcNow);
        var response = new BaseCommandResponse<SessionTemplateOutcome>
        {
            Success = true,
            Message = "Applied",
            Id = outcome
        };
        using var handler = new RecordingHandler(_ => CreateJsonResponse(response));
        var service = new EventSessionTemplateSyncService(CreateSessionApi(handler));
        var sessionId = Guid.NewGuid();

        BaseCommandResponse<SessionTemplateOutcome> result = await service.ApplySyncAsync(
            sessionId,
            new SessionApplyRequest(new SessionPlan { TargetTemplateVersion = 9 }, 8));

        await Assert.That(result.Success).IsTrue();
        await Assert.That(handler.LastRequest?.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(handler.LastRequest?.RequestUri?.PathAndQuery)
            .IsEqualTo($"/api/event-sessions/{sessionId}/template-sync/apply");
    }

    [Test]
    public async Task EventSessionTemplateSyncService_ApplySyncAsync_WithJsonNullBody_ThrowsPreviousApplyMessage()
    {
        using var handler = new RecordingHandler(_ => CreateJsonNullResponse());
        var service = new EventSessionTemplateSyncService(CreateSessionApi(handler));

        InvalidOperationException? exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.ApplySyncAsync(
                Guid.NewGuid(),
                new SessionApplyRequest(new SessionPlan { TargetTemplateVersion = 9 }, 8)));

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).IsEqualTo("Failed to read apply response.");
    }

    [Test]
    public async Task EventSessionTemplateSyncService_GetHistoryAsync_WithJsonNullBody_ThrowsPreviousHistoryMessage()
    {
        using var handler = new RecordingHandler(_ => CreateJsonNullResponse());
        var service = new EventSessionTemplateSyncService(CreateSessionApi(handler));

        InvalidOperationException? exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GetHistoryAsync(Guid.NewGuid()));

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).IsEqualTo("Failed to read history response.");
    }

    private static IEventTemplateSyncApi CreateEventApi(HttpMessageHandler handler) =>
        RestService.For<IEventTemplateSyncApi>(CreateClient(handler));

    private static IEventSessionTemplateSyncApi CreateSessionApi(HttpMessageHandler handler) =>
        RestService.For<IEventSessionTemplateSyncApi>(CreateClient(handler));

    private static HttpClient CreateClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://bff.test/")
        };
    }

    private static EventTemplateDiff CreateEventDiff()
    {
        return new EventTemplateDiff(7, 6, [], [], [], [], [], [], []);
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage CreateProblemResponse(HttpStatusCode statusCode)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                """
                {"status":409,"title":"Conflict","detail":"Template version changed."}
                """,
                Encoding.UTF8,
                "application/problem+json")
        };
    }

    private static HttpResponseMessage CreateJsonNullResponse()
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(responder(request));
        }
    }
}
