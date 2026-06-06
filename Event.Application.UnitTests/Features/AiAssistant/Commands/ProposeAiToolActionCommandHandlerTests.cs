// ABOUTME: Tests MCP-style AI tool proposal command behavior and fail-closed ownership checks.
// ABOUTME: Ensures external adapters persist proposals only through registry validation and conversation aggregates.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.AiAssistant.Handlers.Commands;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Features.AiAssistant.Tools;
using Explore.Domain.Ai;
using NSubstitute;

namespace Event.Application.UnitTests.Features.AiAssistant.Commands;

public sealed class ProposeAiToolActionCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _conversationId = Guid.CreateVersion7();
    private readonly IAiConversationRepository _conversationRepository = Substitute.For<IAiConversationRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IAiToolContractRegistry _toolRegistry = AiToolContractRegistry.CreateDefault();

    public ProposeAiToolActionCommandHandlerTests()
    {
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(_userId);
    }

    [Test]
    public async Task Handle_WhenUserIsUnauthenticated_FailsBeforeRepositoryCall()
    {
        _currentUserService.IsAuthenticated.Returns(false);
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unauthenticated");
        await _conversationRepository.DidNotReceiveWithAnyArgs()
            .GetByIdForUpdateAsync(default, default);
    }

    [Test]
    public async Task Handle_WhenToolIsUnknown_FailsWithoutRepositoryCall()
    {
        var command = CreateCommand(toolName: "DeleteEverything");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unknown_tool");
        await _conversationRepository.DidNotReceiveWithAnyArgs()
            .GetByIdForUpdateAsync(default, default);
    }

    [Test]
    public async Task Handle_WhenPayloadIsInvalid_FailsBeforeRepositoryCall()
    {
        var command = CreateCommand(payloadJson: "{ \"tenantId\": \"not-allowed\" }");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("forbidden_tool_argument");
        await _conversationRepository.DidNotReceiveWithAnyArgs()
            .GetByIdForUpdateAsync(default, default);
    }

    [Test]
    public async Task Handle_WhenConversationBelongsToAnotherUser_FailsClosed()
    {
        var conversation = CreateConversation(Guid.CreateVersion7());
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("conversation_not_found");
        await _conversationRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Test]
    public async Task Handle_WhenConversationIsInactive_FailsWithoutMutation()
    {
        var conversation = CreateConversation(_userId, AiConversationStatus.Archived);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("conversation_not_active");
        await Assert.That(conversation.ProposedActions).IsEmpty();
        await _conversationRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Test]
    public async Task Handle_WhenPayloadIsValid_PersistsProposedActionWithoutExecutingTool()
    {
        var conversation = CreateConversation(_userId);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(conversation.ProposedActions.Count).IsEqualTo(1);
        var proposedAction = conversation.ProposedActions.Single();
        await Assert.That(proposedAction.Id).IsEqualTo(result.Id);
        await Assert.That(proposedAction.Kind).IsEqualTo(AiProposedActionKind.CreateEventDraft);
        await Assert.That(proposedAction.PayloadJson).Contains("MCP event draft");
        await _conversationRepository.Received(1).Update(conversation);
    }

    private ProposeAiToolActionCommandHandler CreateHandler()
        => new(_conversationRepository, _toolRegistry, _currentUserService);

    private ProposeAiToolActionCommand CreateCommand(
        string toolName = "CreateEventDraft",
        string payloadJson = "{ \"title\": \"MCP event draft\" }")
        => new()
        {
            ConversationId = _conversationId,
            ToolName = toolName,
            PayloadJson = payloadJson,
            Summary = "Propose an event draft"
        };

    private AiConversation CreateConversation(Guid userId, AiConversationStatus status = AiConversationStatus.Active)
        => new()
        {
            Id = _conversationId,
            TenantId = _tenantId,
            UserId = userId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
}
