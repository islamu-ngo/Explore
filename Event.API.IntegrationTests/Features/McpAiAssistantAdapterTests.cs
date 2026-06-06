// ABOUTME: Tests MCP assistant tools, resources, and prompts over MediatR boundaries.
// ABOUTME: Verifies MCP surfaces remain proposal-first and omit raw tool payload details.

using System.Text.Json;
using Explore.API.Mcp;
using Explore.Application.DTOs.Ai;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Features.AiAssistant.Requests.Queries;
using Explore.Application.Responses;
using FluentAssertions;
using MediatR;
using NSubstitute;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class McpAiAssistantAdapterTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [Test]
    public async Task ProposeAiToolActionAsync_DelegatesToMediatRAndReturnsSafeResult()
    {
        var conversationId = Guid.CreateVersion7();
        var proposedActionId = Guid.CreateVersion7();
        _mediator.Send(Arg.Any<ProposeAiToolActionCommand>(), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid>
            {
                Success = true,
                Id = proposedActionId,
                Message = "AI tool action proposed. Confirm before execution."
            });

        var tool = new AiAssistantMcpTools(_mediator);

        var json = await tool.ProposeAiToolActionAsync(
            conversationId,
            "CreateEventDraft",
            "{ \"title\": \"MCP draft\" }",
            "Draft an event",
            CancellationToken.None);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("Success").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("Id").GetGuid().Should().Be(proposedActionId);
        document.RootElement.GetProperty("Message").GetString().Should().Contain("Confirm");

        await _mediator.Received(1).Send(
            Arg.Is<ProposeAiToolActionCommand>(command =>
                command.ConversationId == conversationId &&
                command.ToolName == "CreateEventDraft" &&
                command.PayloadJson.Contains("MCP draft") &&
                command.Summary == "Draft an event"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListConversationsAsync_ReturnsSafeConversationSummaries()
    {
        var conversationId = Guid.CreateVersion7();
        _mediator.Send(Arg.Any<GetAiConversationListQuery>(), Arg.Any<CancellationToken>())
            .Returns([
                new AiConversationSummaryDto
                {
                    Id = conversationId,
                    Status = "Active",
                    Title = "Event planning",
                    Provider = "fake",
                    ModelId = "fake-model",
                    LastMessageSequence = 3,
                    CreatedAt = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc)
                }
            ]);

        var resources = new AiAssistantMcpResources(_mediator);

        var json = await resources.ListConversationsAsync(CancellationToken.None);

        using var document = JsonDocument.Parse(json);
        var conversations = document.RootElement.GetProperty("Conversations");
        conversations.GetArrayLength().Should().Be(1);
        conversations[0].GetProperty("Id").GetGuid().Should().Be(conversationId);
        conversations[0].GetProperty("Title").GetString().Should().Be("Event planning");
        json.Should().NotContain("PayloadJson");

        await _mediator.Received(1).Send(
            Arg.Is<GetAiConversationListQuery>(query => query.Limit == 10),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetConversationAsync_OmitsRawProposedActionPayloads()
    {
        var conversationId = Guid.CreateVersion7();
        var proposedActionId = Guid.CreateVersion7();
        _mediator.Send(Arg.Any<GetAiConversationDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns(new AiConversationDto
            {
                Id = conversationId,
                Status = "Active",
                Title = "Detail",
                LastMessageSequence = 1,
                CreatedAt = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc),
                Messages = [
                    new AiMessageDto
                    {
                        Id = Guid.CreateVersion7(),
                        Sequence = 1,
                        Role = "User",
                        Content = "Sensitive prompt text",
                        CreatedAt = new DateTime(2026, 6, 3, 12, 1, 0, DateTimeKind.Utc)
                    }
                ],
                ProposedActions = [
                    new AiProposedActionDto
                    {
                        Id = proposedActionId,
                        Kind = "CreateEventDraft",
                        Status = "Proposed",
                        PayloadJson = "{ \"title\": \"Secret draft payload\" }",
                        CreatedAt = new DateTime(2026, 6, 3, 12, 2, 0, DateTimeKind.Utc)
                    }
                ]
            });

        var resources = new AiAssistantMcpResources(_mediator);

        var json = await resources.GetConversationAsync(conversationId, CancellationToken.None);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("Found").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("ProposedActions")[0].GetProperty("Id").GetGuid().Should().Be(proposedActionId);
        document.RootElement.GetProperty("Messages")[0].GetProperty("HasContent").GetBoolean().Should().BeTrue();
        json.Should().NotContain("PayloadJson");
        json.Should().NotContain("Secret draft payload");
        json.Should().NotContain("Sensitive prompt text");
    }

    [Test]
    public void CreateEventDraftWithConfirmationPrompt_RequiresProposalAndConfirmation()
    {
        var prompt = new AiAssistantMcpPrompts().CreateEventDraftWithConfirmation();

        prompt.Should().Contain("list_ai_tool_contracts");
        prompt.Should().Contain("propose_ai_tool_action");
        prompt.Should().Contain("wait for");
        var normalized = prompt.ToLowerInvariant();
        normalized.Should().NotContain("apikey");
        normalized.Should().NotContain("provider endpoint");
        normalized.Should().NotContain("tenant id");
    }
}
