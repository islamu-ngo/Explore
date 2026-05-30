// ABOUTME: Unit tests for AI assistant conversation history and run-status query handlers.
// ABOUTME: Verifies user ownership gates and safe DTO ordering for private assistant history.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.AiAssistant.Handlers.Queries;
using Explore.Application.Features.AiAssistant.Requests.Queries;
using Explore.Domain.Ai;
using NSubstitute;

namespace Event.Application.UnitTests.Features.AiAssistant.Queries;

public sealed class AiConversationQueryHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly IAiConversationRepository _conversationRepository = Substitute.For<IAiConversationRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public AiConversationQueryHandlerTests()
    {
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(_userId);
    }

    [Test]
    public async Task ListHandle_WhenAuthenticated_ReturnsOwnedConversationSummaries()
    {
        var conversation = CreateConversation(title: "Planning");
        _conversationRepository.ListRecentForUserAsync(_userId, 10, Arg.Any<CancellationToken>())
            .Returns([conversation]);

        var handler = new GetAiConversationListQueryHandler(_conversationRepository, _currentUserService);
        var result = await handler.Handle(new GetAiConversationListQuery { Limit = 10 }, CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo(conversation.Id);
        await Assert.That(result[0].Title).IsEqualTo("Planning");
        await Assert.That(result[0].Status).IsEqualTo(nameof(AiConversationStatus.Active));
    }

    [Test]
    public async Task DetailHandle_WhenConversationBelongsToUser_ReturnsOrderedHistory()
    {
        var conversation = CreateConversation();
        conversation.Messages.Add(new AiMessage
        {
            Id = Guid.CreateVersion7(),
            TenantId = _tenantId,
            ConversationId = conversation.Id,
            Sequence = 2,
            Role = AiMessageRole.Assistant,
            Content = "Second",
            CreatedAt = DateTime.UtcNow.AddMinutes(2)
        });
        conversation.Messages.Add(new AiMessage
        {
            Id = Guid.CreateVersion7(),
            TenantId = _tenantId,
            ConversationId = conversation.Id,
            Sequence = 1,
            Role = AiMessageRole.User,
            Content = "First",
            CreatedAt = DateTime.UtcNow.AddMinutes(1)
        });
        _conversationRepository.GetByIdWithDetailsAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var handler = new GetAiConversationDetailQueryHandler(_conversationRepository, _currentUserService);
        var result = await handler.Handle(new GetAiConversationDetailQuery { ConversationId = conversation.Id }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Messages.Count).IsEqualTo(2);
        await Assert.That(result.Messages[0].Sequence).IsEqualTo(1);
        await Assert.That(result.Messages[1].Sequence).IsEqualTo(2);
    }

    [Test]
    public async Task DetailHandle_WhenConversationBelongsToAnotherUser_ReturnsNull()
    {
        var conversation = CreateConversation(userId: Guid.CreateVersion7());
        _conversationRepository.GetByIdWithDetailsAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var handler = new GetAiConversationDetailQueryHandler(_conversationRepository, _currentUserService);
        var result = await handler.Handle(new GetAiConversationDetailQuery { ConversationId = conversation.Id }, CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task RunStatusHandle_WhenRunBelongsToOwnedConversation_ReturnsSafeRunDto()
    {
        var conversation = CreateConversation();
        var run = new AiRun
        {
            Id = Guid.CreateVersion7(),
            TenantId = _tenantId,
            ConversationId = conversation.Id,
            Provider = "fake",
            ModelId = "fake-ai-assistant-v1",
            QueuedAt = DateTime.UtcNow,
            Status = AiRunStatus.Queued
        };
        conversation.Runs.Add(run);
        _conversationRepository.GetByIdWithDetailsAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var handler = new GetAiRunStatusQueryHandler(_conversationRepository, _currentUserService);
        var result = await handler.Handle(new GetAiRunStatusQuery
        {
            ConversationId = conversation.Id,
            RunId = run.Id
        }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(run.Id);
        await Assert.That(result.Status).IsEqualTo(nameof(AiRunStatus.Queued));
        await Assert.That(result.Provider).IsEqualTo("fake");
    }

    [Test]
    public async Task ListHandle_WhenUnauthenticated_ReturnsEmptyWithoutRepositoryCall()
    {
        _currentUserService.IsAuthenticated.Returns(false);
        _currentUserService.UserId.Returns((Guid?)null);

        var handler = new GetAiConversationListQueryHandler(_conversationRepository, _currentUserService);
        var result = await handler.Handle(new GetAiConversationListQuery(), CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(0);
        await _conversationRepository.DidNotReceive().ListRecentForUserAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    private AiConversation CreateConversation(string? title = null, Guid? userId = null)
        => new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = _tenantId,
            UserId = userId ?? _userId,
            Status = AiConversationStatus.Active,
            Title = title,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
}
