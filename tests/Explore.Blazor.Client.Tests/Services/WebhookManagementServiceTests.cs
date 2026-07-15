// ABOUTME: Unit coverage for typed-owner webhook management client queries.
// ABOUTME: Proves all five normalized scopes and collection HAL capabilities cross the generated API boundary exactly.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Webhooks;
using Explore.Blazor.Client.Services.Webhooks;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class WebhookManagementServiceTests
{
    private readonly IEventApiClient _apiClient = Substitute.For<IEventApiClient>();
    private readonly WebhookManagementService _service;

    public WebhookManagementServiceTests()
    {
        _service = new WebhookManagementService(
            _apiClient,
            Substitute.For<ILogger<WebhookManagementService>>());
        ConfigureEmptySnapshot();
    }

    [Test]
    public async Task GetSnapshotAsync_ForEveryOwnerKind_ForwardsExactNormalizedScope()
    {
        var organizationId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();
        WebhookOwnerSelection[] owners =
        [
            WebhookOwnerSelection.Tenant,
            WebhookOwnerSelection.ForOrganization(organizationId),
            WebhookOwnerSelection.ForGroup(groupId),
            WebhookOwnerSelection.User,
            WebhookOwnerSelection.Instance
        ];

        foreach (var owner in owners)
        {
            _apiClient.ClearReceivedCalls();

            var snapshot = await _service.GetSnapshotAsync(owner);

            await Assert.That(snapshot.IsSuccess).IsTrue();
            await _apiClient.Received(1).GetWebhookConsumersAsync(
                owner.OwnerKindId,
                owner.OwnerId,
                200,
                null,
                null,
                Arg.Any<CancellationToken>());
            await _apiClient.Received(1).GetWebhookEndpointsAsync(
                owner.OwnerKindId,
                owner.OwnerId,
                null,
                200,
                null,
                null,
                Arg.Any<CancellationToken>());
            await _apiClient.Received(1).GetWebhookMessagesAsync(
                owner.OwnerKindId,
                owner.OwnerId,
                100,
                null,
                null,
                Arg.Any<CancellationToken>());
            await _apiClient.Received(1).GetWebhookDeliveryAttemptsAsync(
                owner.OwnerKindId,
                owner.OwnerId,
                null,
                null,
                100,
                null,
                null,
                Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task GetDeliveryAttemptsAsync_ForOrganization_ForwardsOwnerAndFiltersTogether()
    {
        var organizationId = Guid.CreateVersion7();
        var messageId = Guid.CreateVersion7();
        var endpointId = Guid.CreateVersion7();
        var owner = WebhookOwnerSelection.ForOrganization(organizationId);

        var attempts = await _service.GetDeliveryAttemptsAsync(
            owner,
            messageId,
            endpointId,
            limit: 37);

        await Assert.That(attempts.Count).IsEqualTo(0);
        await _apiClient.Received(1).GetWebhookDeliveryAttemptsAsync(
            (int)WebhookOwnerKind.Organization,
            organizationId,
            messageId,
            endpointId,
            37,
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetSnapshotAsync_MapsSensitiveTabsOnlyFromMessageCollectionRelations()
    {
        var messages = new HalCollectionResourceOfWebhookMessageDto();
        GeneratedHalLinkTestHelper.SetLinks(
            messages,
            (WebhookClientLinkRelations.ProviderPublications, "/api/webhooks/provider-publications", "GET"),
            (WebhookClientLinkRelations.BulkReplayPreview, "/api/webhooks/bulk-replays/preview", "GET"),
            (WebhookClientLinkRelations.BulkReplays, "/api/webhooks/bulk-replays", "GET"));
        _apiClient.GetWebhookMessagesAsync(
                Arg.Any<int?>(),
                Arg.Any<Guid?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(messages));

        var snapshot = await _service.GetSnapshotAsync(WebhookOwnerSelection.Tenant);

        await Assert.That(snapshot.CanViewProviderPublications).IsTrue();
        await Assert.That(snapshot.CanUseBulkReplay).IsTrue();
    }

    private void ConfigureEmptySnapshot()
    {
        _apiClient.GetWebhookEventTypesAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ICollection<WebhookEventTypeDto>>([]));
        _apiClient.GetWebhookConsumersAsync(
                Arg.Any<int?>(),
                Arg.Any<Guid?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HalCollectionResourceOfWebhookConsumerDto()));
        _apiClient.GetWebhookEndpointsAsync(
                Arg.Any<int?>(),
                Arg.Any<Guid?>(),
                Arg.Any<Guid?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HalCollectionResourceOfWebhookEndpointDto()));
        _apiClient.GetWebhookMessagesAsync(
                Arg.Any<int?>(),
                Arg.Any<Guid?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HalCollectionResourceOfWebhookMessageDto()));
        _apiClient.GetWebhookDeliveryAttemptsAsync(
                Arg.Any<int?>(),
                Arg.Any<Guid?>(),
                Arg.Any<Guid?>(),
                Arg.Any<Guid?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HalCollectionResourceOfWebhookDeliveryAttemptDto()));
    }
}
