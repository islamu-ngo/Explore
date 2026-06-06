// ABOUTME: MCP tool methods for proposal-first AI assistant actions.
// ABOUTME: Delegates through MediatR so external MCP clients never mutate repositories directly.

using System.ComponentModel;
using System.Text.Json;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using MediatR;
using ModelContextProtocol.Server;

namespace Explore.API.Mcp;

[McpServerToolType]
public sealed class AiAssistantMcpTools(IMediator mediator)
{
    [McpServerTool(
        Name = "propose_ai_tool_action",
        Title = "Propose AI tool action",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Validate a registry-backed AI tool payload and persist it as a proposed action. The action is not executed until a user confirms it through the normal API/HAL flow.")]
    public async Task<string> ProposeAiToolActionAsync(
        Guid conversationId,
        string toolName,
        string payloadJson,
        string? summary = null,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new ProposeAiToolActionCommand
            {
                ConversationId = conversationId,
                ToolName = toolName,
                PayloadJson = payloadJson,
                Summary = summary
            },
            cancellationToken);

        return JsonSerializer.Serialize(
            new AiMcpCommandResultDescriptor(
                response.Success,
                response.Id == Guid.Empty ? null : response.Id,
                response.Message,
                response.FailureCode,
                response.Errors),
            AiToolRegistryMcpJsonContext.Default.AiMcpCommandResultDescriptor);
    }

    public sealed record AiMcpCommandResultDescriptor(
        bool Success,
        Guid? Id,
        string? Message,
        string? FailureCode,
        IReadOnlyList<string> Errors);
}
