// ABOUTME: Persists registry-validated AI tool proposals without executing their side effects.
// ABOUTME: Keeps external adapters on the same proposal and confirmation path as provider output.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Features.AiAssistant.Tools;
using Explore.Application.Responses;
using Explore.Domain.Ai;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Handlers.Commands;

public sealed class ProposeAiToolActionCommandHandler(
    IAiConversationRepository conversationRepository,
    IAiToolContractRegistry toolRegistry,
    ICurrentUserService currentUserService)
    : IRequestHandler<ProposeAiToolActionCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        ProposeAiToolActionCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is not { } userId)
        {
            return Failure("AI tool proposals require an authenticated user.", "unauthenticated");
        }

        var definition = ResolveDefinition(request.ToolName);
        if (definition is null || !definition.ExposeToMcp)
        {
            return Failure("AI tool is not available for MCP proposal.", "unknown_tool");
        }

        var validation = toolRegistry.ValidatePayload(definition.Kind, request.PayloadJson);
        if (!validation.Succeeded)
        {
            return Failure(
                validation.FailureMessage ?? "AI tool payload failed validation.",
                validation.FailureCode ?? "invalid_tool_arguments");
        }

        var conversation = await conversationRepository.GetByIdForUpdateAsync(
            request.ConversationId,
            cancellationToken);

        if (conversation is null || conversation.UserId != userId)
        {
            return Failure("AI conversation was not found.", "conversation_not_found");
        }

        if (conversation.Status != AiConversationStatus.Active)
        {
            return Failure("AI conversation is not ready for proposed actions.", "conversation_not_active");
        }

        var proposedAction = conversation.ProposeAction(
            definition.Kind,
            request.PayloadJson,
            messageId: null,
            userId,
            DateTime.UtcNow);

        await conversationRepository.Update(conversation);

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = proposedAction.Id,
            Message = "AI tool action proposed. Confirm before execution."
        };
    }

    private AiToolDefinition? ResolveDefinition(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return null;
        }

        return toolRegistry.Definitions.FirstOrDefault(definition =>
            string.Equals(definition.Name, toolName.Trim(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(definition.Kind.ToString(), toolName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static BaseCommandResponse<Guid> Failure(string message, string failureCode)
        => new()
        {
            Success = false,
            Id = Guid.Empty,
            Message = message,
            Errors = [message],
            FailureCode = failureCode
        };
}
