// ABOUTME: Unit tests for confirming and rejecting AI-proposed actions.
// ABOUTME: Verifies tenant ownership, duplicate safety, and CreateEventDraft MediatR execution.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.AiAssistant.Handlers.Commands;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Ai;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.AiAssistant.Commands;

public sealed class AiProposedActionCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _actionId = Guid.CreateVersion7();
    private readonly Guid _conversationId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IAiConversationRepository _conversationRepository = Substitute.For<IAiConversationRepository>();
    private readonly IOrganizationMemberRepository _organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
    private readonly IGroupMemberRepository _groupMemberRepository = Substitute.For<IGroupMemberRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public AiProposedActionCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(_userId);
        _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(_userId, Arg.Any<string>()).Returns([]);
        _groupMemberRepository.GetGroupIdsWhereUserHasPermission(_userId, Arg.Any<string>()).Returns([]);
        _mediator.Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid>
            {
                Success = true,
                Id = _eventId,
                Message = "Created"
            });
    }

    [Test]
    public async Task Confirm_WhenUserIsUnauthenticated_FailsBeforeRepositoryOrMediator()
    {
        _currentUserService.IsAuthenticated.Returns(false);
        _currentUserService.UserId.Returns((Guid?)null);

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unauthenticated");
        await _conversationRepository.DidNotReceive().GetProposedActionForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Confirm_WhenActionBelongsToDifferentUser_FailsClosedWithoutExecution()
    {
        AiProposedAction action = CreateProposedAction(userId: Guid.CreateVersion7());
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("proposed_action_not_found");
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Proposed);
        await _conversationRepository.DidNotReceive().UpdateProposedActionAsync(Arg.Any<AiProposedAction>(), Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Confirm_WhenCreateEventDraftIsProposed_DispatchesCreateEventCommandAndMarksExecuted()
    {
        AiProposedAction action = CreateProposedAction(payloadJson: "{\"title\":\"Community Dinner\"}");
        CreateEventCommand? sentCommand = null;
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);
        _mediator.Send(Arg.Do<CreateEventCommand>(command => sentCommand = command), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid> { Success = true, Id = _eventId });

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(_eventId);
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Executed);
        await Assert.That(action.ResultResourceId).IsEqualTo(_eventId);
        await Assert.That(action.ConfirmedBy).IsEqualTo(_userId);
        await Assert.That(sentCommand).IsNotNull();
        await Assert.That(sentCommand!.Request.Title).IsEqualTo("Community Dinner");
        await Assert.That(sentCommand.Request.Sessions).IsEmpty();
        await Assert.That(sentCommand.Request.Days).IsEmpty();
        await Assert.That(sentCommand.Request.Rooms).IsEmpty();
        await Assert.That(sentCommand.Request.AgendaItems).IsEmpty();
        await _conversationRepository.Received(1).CreateToolExecutionAsync(
            Arg.Is<AiToolExecution>(execution =>
                execution.TenantId == _tenantId
                && execution.ProposedActionId == _actionId
                && execution.ToolName == "CreateEventDraft"
                && execution.Succeeded
                && execution.CompletedAt != null
                && execution.FailureCode == null
                && execution.FailureMessage == null),
            Arg.Any<CancellationToken>());
        await _conversationRepository.Received(1).UpdateProposedActionAsync(action, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Confirm_WhenCreateEventDraftUsesAllowedOrganization_DispatchesOrganizationScopedCreateCommand()
    {
        var organizationId = Guid.CreateVersion7();
        AiProposedAction action = CreateProposedAction(payloadJson: "{\"title\":\"Community Dinner\",\"organizationId\":\"" + organizationId + "\"}");
        CreateEventCommand? sentCommand = null;
        _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(_userId, Arg.Any<string>()).Returns([organizationId]);
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);
        _mediator.Send(Arg.Do<CreateEventCommand>(command => sentCommand = command), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid> { Success = true, Id = _eventId });

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Executed);
        await Assert.That(sentCommand).IsNotNull();
        await Assert.That(sentCommand!.Request.OrganizationId).IsEqualTo(organizationId);
        await Assert.That(sentCommand.Request.Title).IsEqualTo("Community Dinner");
    }

    [Test]
    public async Task Confirm_WhenActionAlreadyExecuted_ReturnsExistingResultWithoutReexecution()
    {
        AiProposedAction action = CreateProposedAction();
        action.Confirm(_userId, DateTime.UtcNow);
        action.MarkExecuted(_eventId);
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(_eventId);
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().CreateToolExecutionAsync(Arg.Any<AiToolExecution>(), Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().UpdateProposedActionAsync(Arg.Any<AiProposedAction>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Confirm_WhenPayloadMappingFails_MarksActionFailedWithoutMediatorDispatch()
    {
        AiProposedAction action = CreateProposedAction(payloadJson: "{\"organizationId\":\"" + Guid.CreateVersion7() + "\"}");
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Failed);
        await Assert.That(action.FailureCode).IsNotNull();
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
        await _conversationRepository.Received(1).CreateToolExecutionAsync(
            Arg.Is<AiToolExecution>(execution =>
                execution.TenantId == _tenantId
                && execution.ProposedActionId == _actionId
                && execution.ToolName == "CreateEventDraft"
                && !execution.Succeeded
                && execution.CompletedAt != null
                && execution.FailureCode == action.FailureCode
                && execution.FailureMessage == action.FailureMessage
                && execution.FailureMessage != action.PayloadJson),
            Arg.Any<CancellationToken>());
        await _conversationRepository.Received(1).UpdateProposedActionAsync(action, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Reject_WhenActionIsProposed_MarksRejectedWithoutExecution()
    {
        AiProposedAction action = CreateProposedAction();
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);

        BaseCommandResponse<Guid> result = await CreateRejectHandler().Handle(CreateRejectCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(_actionId);
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Rejected);
        await Assert.That(action.RejectedBy).IsEqualTo(_userId);
        await _conversationRepository.DidNotReceive().CreateToolExecutionAsync(Arg.Any<AiToolExecution>(), Arg.Any<CancellationToken>());
        await _conversationRepository.Received(1).UpdateProposedActionAsync(action, Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Reject_WhenActionAlreadyRejected_ReturnsSuccessWithoutUpdate()
    {
        AiProposedAction action = CreateProposedAction();
        action.Reject(_userId, DateTime.UtcNow);
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);

        BaseCommandResponse<Guid> result = await CreateRejectHandler().Handle(CreateRejectCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(_actionId);
        await _conversationRepository.DidNotReceive().UpdateProposedActionAsync(Arg.Any<AiProposedAction>(), Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Reject_WhenActionAlreadyExecuted_FailsWithoutSideEffects()
    {
        AiProposedAction action = CreateProposedAction();
        action.Confirm(_userId, DateTime.UtcNow);
        action.MarkExecuted(_eventId);
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);

        BaseCommandResponse<Guid> result = await CreateRejectHandler().Handle(CreateRejectCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_proposed_action_state");
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Executed);
        await _conversationRepository.DidNotReceive().UpdateProposedActionAsync(Arg.Any<AiProposedAction>(), Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
    }

    private ConfirmAiProposedActionCommandHandler CreateConfirmHandler()
        => new(
            _conversationRepository,
            _organizationMemberRepository,
            _groupMemberRepository,
            _tenantContext,
            _currentUserService,
            _mediator);

    private RejectAiProposedActionCommandHandler CreateRejectHandler()
        => new(_conversationRepository, _tenantContext, _currentUserService);

    private ConfirmAiProposedActionCommand CreateConfirmCommand()
        => new() { ProposedActionId = _actionId };

    private RejectAiProposedActionCommand CreateRejectCommand()
        => new() { ProposedActionId = _actionId };

    private AiProposedAction CreateProposedAction(Guid? tenantId = null, Guid? userId = null, string payloadJson = "{\"title\":\"Draft\"}")
    {
        Guid actionTenantId = tenantId ?? _tenantId;
        var conversation = new AiConversation
        {
            Id = _conversationId,
            TenantId = actionTenantId,
            UserId = userId ?? _userId,
            Status = AiConversationStatus.Active
        };

        return new AiProposedAction
        {
            Id = _actionId,
            TenantId = actionTenantId,
            ConversationId = _conversationId,
            Conversation = conversation,
            Kind = AiProposedActionKind.CreateEventDraft,
            Status = AiProposedActionStatus.Proposed,
            PayloadJson = payloadJson,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
    }
}
