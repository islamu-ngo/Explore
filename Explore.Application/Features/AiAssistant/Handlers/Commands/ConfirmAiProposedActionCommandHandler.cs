// ABOUTME: Confirms AI-proposed actions and executes supported tools through existing CQRS commands.
// ABOUTME: Enforces tenant and conversation ownership before mutating proposed-action state.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.AiAssistant.Actions;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Ai;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Handlers.Commands;

public sealed class ConfirmAiProposedActionCommandHandler(
    IAiConversationRepository conversationRepository,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService,
    IMediator mediator) : IRequestHandler<ConfirmAiProposedActionCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        ConfirmAiProposedActionCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is not { } userId)
        {
            return Failure("AI proposed action confirmation requires an authenticated user.", ["User is not authenticated."], "unauthenticated");
        }

        AiProposedAction? action = await conversationRepository.GetProposedActionForUpdateAsync(request.ProposedActionId, cancellationToken);
        if (!IsActionVisibleToCurrentPrincipal(action, tenantContext.TenantId, userId))
        {
            return Failure("AI proposed action was not found.", ["Proposed action was not found."], "proposed_action_not_found");
        }

        if (action!.Status == AiProposedActionStatus.Executed)
        {
            return Success(action.ResultResourceId ?? action.Id, "AI proposed action was already executed.");
        }

        if (action.Status == AiProposedActionStatus.Rejected)
        {
            return Failure("AI proposed action was already rejected.", ["Rejected proposed actions cannot be confirmed."], "proposed_action_rejected", action.Id);
        }

        if (action.Status == AiProposedActionStatus.Failed)
        {
            return Failure("AI proposed action previously failed.", ["Failed proposed actions cannot be confirmed again."], "proposed_action_failed", action.Id);
        }

        var utcNow = DateTime.UtcNow;
        if (action.Status == AiProposedActionStatus.Proposed)
        {
            action.Confirm(userId, utcNow);
        }

        CreateEventDraftAiToolExecutionResult executionResult = await ExecuteAsync(action, cancellationToken);
        if (!executionResult.Succeeded)
        {
            action.MarkFailed(
                executionResult.FailureCode ?? "ai_tool_execution_failed",
                executionResult.FailureMessage ?? "AI tool execution failed.");
            await conversationRepository.UpdateProposedActionAsync(action, cancellationToken);

            return Failure(
                "AI proposed action confirmation failed.",
                [action.FailureMessage ?? "AI tool execution failed."],
                action.FailureCode,
                action.Id);
        }

        action.MarkExecuted(executionResult.ResultResourceId!.Value);
        await conversationRepository.UpdateProposedActionAsync(action, cancellationToken);

        return Success(executionResult.ResultResourceId.Value, "AI proposed action confirmed and executed.");
    }

    private async Task<CreateEventDraftAiToolExecutionResult> ExecuteAsync(
        AiProposedAction action,
        CancellationToken cancellationToken)
    {
        if (action.Kind != AiProposedActionKind.CreateEventDraft)
        {
            return CreateEventDraftAiToolExecutionResult.Failure(
                "unsupported_action_kind",
                "AI proposed action kind is not supported for confirmation.");
        }

        var executor = new CreateEventDraftAiToolExecutor(mediator);
        return await executor.ExecuteAsync(action.PayloadJson, CreateEventDraftAiActionMappingContext.Empty, cancellationToken);
    }

    private static bool IsActionVisibleToCurrentPrincipal(AiProposedAction? action, Guid tenantId, Guid userId)
        => action?.Conversation is not null
            && action.TenantId == tenantId
            && action.Conversation.TenantId == tenantId
            && action.Conversation.UserId == userId;

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new()
    {
        Success = true,
        Id = id,
        Message = message
    };

    private static BaseCommandResponse<Guid> Failure(
        string message,
        IEnumerable<string> errors,
        string? failureCode = null,
        Guid id = default) => new()
        {
            Success = false,
            Id = id,
            Message = message,
            Errors = errors.ToList(),
            FailureCode = failureCode
        };
}
