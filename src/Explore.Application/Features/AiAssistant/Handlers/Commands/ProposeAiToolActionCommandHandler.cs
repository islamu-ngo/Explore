// ABOUTME: Persists registry-validated AI tool proposals without executing their side effects.
// ABOUTME: Keeps external adapters on the same proposal and confirmation path as provider output.

using System.Text.Json;
using Explore.Application.Authorization;
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
    IEventRepository eventRepository,
    IAiToolContractRegistry toolRegistry,
    ICurrentUserService currentUserService,
    IAuthorizationProvider authorizationProvider)
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

        if (!await IsToolProposalAuthorizedAsync(definition, conversation, request.PayloadJson, cancellationToken))
        {
            return Failure(
                "AI tool proposal is not authorized for the selected resource.",
                "tool_authorization_denied");
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

    private async Task<bool> IsToolProposalAuthorizedAsync(
        AiToolDefinition definition,
        AiConversation conversation,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        if (definition.RequiredAuthorization is null)
        {
            return true;
        }

        using var payload = JsonDocument.Parse(payloadJson);
        var authorizationContext = await BuildAuthorizationContextAsync(
            definition.RequiredAuthorization,
            conversation,
            payload.RootElement,
            cancellationToken);

        if (authorizationContext is null)
        {
            return false;
        }

        return await authorizationProvider.IsAllowedAsync(
            definition.RequiredAuthorization.ResourceKind,
            authorizationContext.Value.ResourceId,
            definition.RequiredAuthorization.Action,
            authorizationContext.Value.ResourceAttributes,
            cancellationToken);
    }

    private async Task<(string ResourceId, IDictionary<string, object> ResourceAttributes)?> BuildAuthorizationContextAsync(
        AiToolAuthorizationRequirement requirement,
        AiConversation conversation,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        var attributes = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["tenantId"] = conversation.TenantId
        };

        if (conversation.ActorId is { } actorId)
        {
            attributes["actorId"] = actorId;
        }

        AddGuidAttribute(payload, attributes, "eventId");
        AddGuidAttribute(payload, attributes, "organizationId");
        AddGuidAttribute(payload, attributes, "groupId");

        if (TryGetGuid(payload, "eventId", out var eventId))
        {
            var targetEvent = await eventRepository.GetAuthorizationTargetByIdAsync(eventId, cancellationToken);
            if (targetEvent is null)
            {
                return null;
            }

            attributes["tenantId"] = targetEvent.TenantId;
        }

        if (requirement.ResourceKind == ResourceKinds.Event &&
            requirement.Action == AuthorizationActions.Create)
        {
            attributes["authorizationPhase"] = AuthorizationPhases.PreCreate;
        }

        return (
            ResolveResourceId(requirement.ResourceKind, payload, conversation.TenantId),
            attributes);
    }

    private static string ResolveResourceId(string resourceKind, JsonElement payload, Guid tenantId)
    {
        var fieldNames = resourceKind switch
        {
            ResourceKinds.Event => new[] { "eventId" },
            ResourceKinds.EventSession => ["sessionId", "eventId"],
            ResourceKinds.EventSessionGroup => ["groupId", "eventId"],
            ResourceKinds.EventDay => ["dayId", "eventId"],
            ResourceKinds.EventAgendaItem => ["agendaItemId", "eventId"],
            ResourceKinds.CustomPropertyDefinition => ["definitionId", "eventId"],
            ResourceKinds.CustomPropertyValue =>
            [
                "eventCustomPropertyDefinitionId",
                "definitionId",
                "eventId"
            ],
            ResourceKinds.CustomPropertyTemplate =>
            [
                "templateId",
                "sessionTemplateId",
                "eventTemplateId",
                "sessionId",
                "eventId"
            ],
            _ => Array.Empty<string>()
        };

        foreach (var fieldName in fieldNames)
        {
            if (TryGetGuid(payload, fieldName, out var value))
            {
                return value.ToString();
            }
        }

        return tenantId.ToString();
    }

    private static void AddGuidAttribute(
        JsonElement payload,
        Dictionary<string, object> attributes,
        string fieldName)
    {
        if (TryGetGuid(payload, fieldName, out var value))
        {
            attributes[fieldName] = value;
        }
    }

    private static bool TryGetGuid(JsonElement payload, string fieldName, out Guid value)
    {
        value = Guid.Empty;
        return payload.ValueKind == JsonValueKind.Object &&
            payload.TryGetProperty(fieldName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            Guid.TryParse(property.GetString(), out value);
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
