// ABOUTME: Tests for the Blazor AI assistant generated-client service wrapper.
// ABOUTME: Verifies safe fallbacks, idempotency propagation, and HAL resource preservation.

using Explore.Blazor.Client.Contracts.Services.Ai;
using Explore.Blazor.Client.Services.Ai;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class AiAssistantClientServiceTests
{
    private readonly IEventApiClient _apiClient = Substitute.For<IEventApiClient>();
    private readonly ILogger<AiAssistantClientService> _logger = Substitute.For<ILogger<AiAssistantClientService>>();

    [Test]
    public async Task SearchReferencesAsync_WhenApiSucceeds_ReturnsHalReferenceItemsWithLinks()
    {
        var referenceId = Guid.CreateVersion7();
        var response = new HalCollectionResourceOfAiReferenceSearchResultDto
        {
            _embedded = new HalCollectionEmbeddedOfAiReferenceSearchResultDto
            {
                Items =
                [
                    new HalResourceOfAiReferenceSearchResultDto
                    {
                        Kind = "Event",
                        ReferenceId = referenceId,
                        DisplayName = "Community Iftar",
                        _links = new Dictionary<string, Anonymous8>
                        {
                            ["event"] = new() { Href = $"/api/events/{referenceId}", Method = "GET" }
                        }
                    }
                ]
            }
        };

        _apiClient.SearchAiReferencesAsync("iftar", 20, null, null, Arg.Any<CancellationToken>())
            .Returns(response);

        var service = CreateService();

        var references = await service.SearchReferencesAsync("iftar", 20);

        await Assert.That(references.Count).IsEqualTo(1);
        await Assert.That(references[0].ReferenceId).IsEqualTo(referenceId);
        await Assert.That(references[0]._links).ContainsKey("event");
    }

    [Test]
    public async Task SearchReferencesAsync_WhenApiFails_ReturnsEmptyList()
    {
        _apiClient.SearchAiReferencesAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException(429));

        var service = CreateService();

        var references = await service.SearchReferencesAsync("iftar", 10);

        await Assert.That(references).IsEmpty();
    }

    [Test]
    public async Task SendMessageAsync_PropagatesIdempotencyKeyInBodyAndHeader()
    {
        var conversationId = Guid.CreateVersion7();
        _apiClient.SendAiMessageAsync(
                conversationId,
                Arg.Any<SendAiMessageRequestDto>(),
                "send-key",
                null,
                null,
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true, Id = Guid.CreateVersion7() });

        var service = CreateService();

        var result = await service.SendMessageAsync(conversationId, "Plan an iftar", "send-key");

        await Assert.That(result.Success).IsTrue();
        await _apiClient.Received(1).SendAiMessageAsync(
            conversationId,
            Arg.Is<SendAiMessageRequestDto>(request =>
                request.Content == "Plan an iftar" && request.IdempotencyKey == "send-key"),
            "send-key",
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConfirmProposedActionAsync_PropagatesIdempotencyKey()
    {
        var conversationId = Guid.CreateVersion7();
        var proposedActionId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        _apiClient.ConfirmAiProposedActionAsync(
                conversationId,
                proposedActionId,
                "confirm-key",
                null,
                null,
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true, Id = eventId });

        var service = CreateService();

        var result = await service.ConfirmProposedActionAsync(conversationId, proposedActionId, "confirm-key");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(eventId);
        await _apiClient.Received(1).ConfirmAiProposedActionAsync(
            conversationId,
            proposedActionId,
            "confirm-key",
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RejectProposedActionAsync_WhenApiFails_ReturnsFailureResult()
    {
        _apiClient.RejectAiProposedActionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException(409));

        var service = CreateService();

        var result = await service.RejectProposedActionAsync(Guid.CreateVersion7(), Guid.CreateVersion7());

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("api_error");
    }

    private AiAssistantClientService CreateService() => new(_apiClient, _logger);

    private static ApiException CreateApiException(int statusCode) =>
        new("API error", statusCode, "{}", new Dictionary<string, IEnumerable<string>>(), null);
}
