// ABOUTME: Unit coverage for generated-client webhook operations normalization.
// ABOUTME: Verifies typed HAL collection affordances and sensitive payload results cross the client service boundary intact.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Webhooks;
using Explore.Blazor.Client.Services.Webhooks;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class WebhookOperationsServiceTests
{
    private readonly IEventApiClient _apiClient = Substitute.For<IEventApiClient>();
    private readonly WebhookOperationsService _service;

    public WebhookOperationsServiceTests() =>
        _service = new WebhookOperationsService(
            _apiClient,
            Substitute.For<ILogger<WebhookOperationsService>>());

    [Test]
    public async Task GetBulkReplaysAsync_MapsTypedItemsAndCollectionHalCapabilities()
    {
        var operation = new HalResourceOfWebhookBulkReplayOperationDto
        {
            Id = Guid.CreateVersion7(),
            TenantId = Guid.CreateVersion7(),
            OperationKey = Guid.CreateVersion7(),
            StatusId = 1,
            StatusCode = "QUEUED",
            StatusName = "Queued",
            Filter = new WebhookBulkReplayFilterDto
            {
                FromUtc = DateTimeOffset.UtcNow.AddDays(-1),
                ToUtc = DateTimeOffset.UtcNow,
                MaxItems = 100
            },
            ReasonCode = "incident_recovery",
            ConcurrencyVersion = 1,
            QueuedAt = DateTimeOffset.UtcNow
        };
        var collection = new HalCollectionResourceOfWebhookBulkReplayOperationDto
        {
            _embedded = new HalCollectionEmbeddedOfWebhookBulkReplayOperationDto { Items = [operation] }
        };
        GeneratedHalLinkTestHelper.SetLinks(
            collection,
            ("bulk-replay-preview", "/api/webhooks/bulk-replays/preview", "GET"),
            ("bulk-replays", "/api/webhooks/bulk-replays", "POST"));
        _apiClient.GetWebhookBulkReplaysAsync(100, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(collection));

        var snapshot = await _service.GetBulkReplaysAsync();

        await Assert.That(snapshot.IsSuccess).IsTrue();
        await Assert.That(snapshot.CanPreview).IsTrue();
        await Assert.That(snapshot.CanSchedule).IsTrue();
        await Assert.That(snapshot.Operations.Count).IsEqualTo(1);
        await Assert.That(snapshot.Operations[0].Id).IsEqualTo(operation.Id);
    }

    [Test]
    public async Task GetMessagePayloadAsync_ReturnsGeneratedSensitiveContractWithoutTransformation()
    {
        var messageId = Guid.CreateVersion7();
        var payload = new WebhookMessagePayloadDto
        {
            MessageId = messageId,
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            PayloadBase64 = "e30=",
            PayloadHash = "sha256:payload",
            PayloadByteLength = 2,
            PayloadRetentionUntil = DateTimeOffset.UtcNow.AddDays(1),
            RetrievedAt = DateTimeOffset.UtcNow
        };
        _apiClient.GetWebhookMessagePayloadAsync(messageId, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(payload));

        var result = await _service.GetMessagePayloadAsync(messageId);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Payload).IsSameReferenceAs(payload);
    }
}
