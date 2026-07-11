// ABOUTME: Unit tests for cancelling AI assistant provider runs through Application handlers.
// ABOUTME: Verifies ownership checks, cancellable state transitions, and terminal-state failures.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.AiAssistant.Handlers.Commands;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Domain.Ai;
using NSubstitute;

namespace Event.Application.UnitTests.Features.AiAssistant.Commands;

public sealed class CancelAiRunCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _conversationId = Guid.CreateVersion7();
    private readonly Guid _runId = Guid.CreateVersion7();
    private readonly IAiConversationRepository _conversationRepository = Substitute.For<IAiConversationRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public CancelAiRunCommandHandlerTests()
    {
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(_userId);
    }

    [Test]
    public async Task Handle_WhenUserIsUnauthenticated_FailsBeforeRepositoryCall()
    {
        _currentUserService.IsAuthenticated.Returns(false);
        _currentUserService.UserId.Returns((Guid?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unauthenticated");
        await _conversationRepository.DidNotReceive().GetByIdForUpdateAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenConversationBelongsToAnotherUser_FailsClosed()
    {
        var conversation = CreateConversation(Guid.CreateVersion7(), AiRunStatus.InProgress);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        var handler = CreateHandler();

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("run_not_found");
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenRunIsInProgress_CancelsRunAndActivatesConversation()
    {
        var conversation = CreateConversation(_userId, AiRunStatus.InProgress);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        var handler = CreateHandler();

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        var run = conversation.Runs.Single();
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(_runId);
        await Assert.That(run.Status).IsEqualTo(AiRunStatus.Cancelled);
        await Assert.That(run.CompletedAt).IsNotNull();
        await Assert.That(conversation.Status).IsEqualTo(AiConversationStatus.Active);
        await _conversationRepository.Received(1).Update(conversation);
    }

    [Test]
    public async Task Handle_WhenRunAlreadyCancelled_ReturnsSuccessWithoutUpdate()
    {
        var conversation = CreateConversation(_userId, AiRunStatus.Cancelled);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        var handler = CreateHandler();

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(_runId);
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenRunSucceeded_FailsWithoutMutation()
    {
        var conversation = CreateConversation(_userId, AiRunStatus.Succeeded);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        var handler = CreateHandler();

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("run_not_cancellable");
        await Assert.That(conversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Succeeded);
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    private CancelAiRunCommandHandler CreateHandler() => new(_conversationRepository, _currentUserService);

    private CancelAiRunCommand CreateCommand() => new()
    {
        ConversationId = _conversationId,
        RunId = _runId
    };

    private AiConversation CreateConversation(Guid userId, AiRunStatus status)
    {
        var conversation = new AiConversation
        {
            Id = _conversationId,
            TenantId = _tenantId,
            UserId = userId,
            Status = status is AiRunStatus.Queued or AiRunStatus.InProgress ? AiConversationStatus.Running : AiConversationStatus.Active,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ConcurrencyStamp = Guid.CreateVersion7()
        };

        conversation.Runs.Add(new AiRun
        {
            Id = _runId,
            TenantId = _tenantId,
            ConversationId = _conversationId,
            Provider = "fake",
            ModelId = "fake-ai-assistant-v1",
            Status = status,
            QueuedAt = DateTime.UtcNow.AddMinutes(-5),
            StartedAt = status is AiRunStatus.InProgress or AiRunStatus.Succeeded or AiRunStatus.Failed or AiRunStatus.Cancelled
                ? DateTime.UtcNow.AddMinutes(-4)
                : null,
            CompletedAt = status is AiRunStatus.Succeeded or AiRunStatus.Failed or AiRunStatus.Cancelled
                ? DateTime.UtcNow.AddMinutes(-1)
                : null
        });

        return conversation;
    }
}
