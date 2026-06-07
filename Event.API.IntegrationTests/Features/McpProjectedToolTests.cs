// ABOUTME: Tests first-class MCP tools projected from the AI Tool Contract Registry.
// ABOUTME: Verifies schema parity, authorization metadata, SDK hints, and proposal-first command mapping.

using System.Text.Json;
using Explore.API.Mcp;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Features.AiAssistant.Tools;
using Explore.Application.Responses;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using NSubstitute;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class McpProjectedToolTests
{
    [Test]
    public void CreateTools_ProjectsMcpExposedRegistryDefinitions()
    {
        var tools = AiMcpProjectedToolFactory.CreateTools(AiToolContractRegistry.CreateDefault());

        tools.Should().ContainSingle();
        var tool = tools.Single();
        tool.ProtocolTool.Name.Should().Be("propose_create_event_draft");
        tool.ProtocolTool.Title.Should().Be("Create event draft");
        tool.ProtocolTool.Description.Should().Contain("proposed action");
        tool.ProtocolTool.Annotations.Should().NotBeNull();
        tool.ProtocolTool.Annotations!.ReadOnlyHint.Should().BeFalse();
        tool.ProtocolTool.Annotations.DestructiveHint.Should().BeFalse();
        tool.ProtocolTool.Annotations.IdempotentHint.Should().BeFalse();
        tool.ProtocolTool.Annotations.OpenWorldHint.Should().BeFalse();
        tool.ProtocolTool.Meta!["islamuRiskClass"]!.GetValue<string>().Should().Be("Medium");
        tool.ProtocolTool.Meta!["islamuApprovalMode"]!.GetValue<string>().Should().Be("HumanConfirmationRequired");
        tool.Metadata.OfType<AuthorizeAttribute>().Should().ContainSingle();
    }

    [Test]
    public void CreateTools_ExcludesDefinitionsNotExposedToMcp()
    {
        var hiddenDefinition = CreateEventDraftAiToolDefinition.Create() with { ExposeToMcp = false };
        var registry = new AiToolContractRegistry([hiddenDefinition]);

        var tools = AiMcpProjectedToolFactory.CreateTools(registry);

        tools.Should().BeEmpty();
    }

    [Test]
    public void ProjectedToolInputSchema_PreservesRegistryPayloadFieldsAndAddsProposalEnvelope()
    {
        var definition = CreateEventDraftAiToolDefinition.Create();
        var tool = new AiMcpProjectedProposalTool(definition);

        var schema = tool.ProtocolTool.InputSchema;
        schema.GetProperty("type").GetString().Should().Be("object");
        schema.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();

        var properties = schema.GetProperty("properties");
        properties.TryGetProperty("conversationId", out _).Should().BeTrue();
        properties.TryGetProperty("summary", out _).Should().BeTrue();
        properties.TryGetProperty("title", out _).Should().BeTrue();
        properties.TryGetProperty("tenantId", out _).Should().BeFalse();

        var projectedPayloadFields = properties.EnumerateObject()
            .Select(property => property.Name)
            .Where(name => name is not "conversationId" and not "summary")
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        projectedPayloadFields.Should().Equal(definition.AllowedPayloadFields.Order(StringComparer.OrdinalIgnoreCase));

        var required = schema.GetProperty("required")
            .EnumerateArray()
            .Select(value => value.GetString())
            .ToArray();
        required.Should().Contain("conversationId");
        required.Should().Contain("title");
    }

    [Test]
    public void MapArgumentsToCommand_CreatesGenericProposalCommandWithoutRuntimeEnvelopeFields()
    {
        var conversationId = Guid.CreateVersion7();
        var tool = new AiMcpProjectedProposalTool(CreateEventDraftAiToolDefinition.Create());
        var arguments = CreateArguments(
            $$"""
              {
                "conversationId": "{{conversationId}}",
                "summary": "Draft an event",
                "title": "Projected MCP draft",
                "eventTypeId": 7
              }
              """);

        var command = tool.MapArgumentsToCommand(arguments);

        command.Should().BeEquivalentTo(
            new ProposeAiToolActionCommand
            {
                ConversationId = conversationId,
                ToolName = "CreateEventDraft",
                Summary = "Draft an event"
            },
            options => options.Excluding(candidate => candidate.PayloadJson));

        using var payload = JsonDocument.Parse(command.PayloadJson);
        payload.RootElement.TryGetProperty("conversationId", out _).Should().BeFalse();
        payload.RootElement.TryGetProperty("summary", out _).Should().BeFalse();
        payload.RootElement.GetProperty("title").GetString().Should().Be("Projected MCP draft");
        payload.RootElement.GetProperty("eventTypeId").GetInt32().Should().Be(7);
    }

    [Test]
    public void MapArgumentsToCommand_RejectsUnexpectedOrHiddenFieldsBeforeMediatR()
    {
        var conversationId = Guid.CreateVersion7();
        var tool = new AiMcpProjectedProposalTool(CreateEventDraftAiToolDefinition.Create());
        var arguments = CreateArguments(
            $$"""
              {
                "conversationId": "{{conversationId}}",
                "title": "Projected MCP draft",
                "tenantId": "{{Guid.CreateVersion7()}}"
              }
              """);

        var act = () => tool.MapArgumentsToCommand(arguments);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*tenantId*");
    }

    [Test]
    public void ProjectedToolOptionsSetup_AddsRegistryProjectionToMcpOptions()
    {
        var options = new McpServerOptions();

        new AiMcpProjectedToolOptionsSetup(AiToolContractRegistry.CreateDefault()).Configure(options);

        options.ToolCollection.Should().NotBeNull();
        options.ToolCollection!.PrimitiveNames.Should().Contain("propose_create_event_draft");
    }

    [Test]
    public void ProjectedTool_DoesNotDependOnRepositories()
    {
        var constructorParameters = typeof(AiMcpProjectedProposalTool)
            .GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType.Name);

        constructorParameters.Should().NotContain(name => name.Contains("Repository", StringComparison.Ordinal));
    }

    [Test]
    public async Task InvokeAsync_ResolvesMediatorFromRequestScopeAndPropagatesCancellationToken()
    {
        var conversationId = Guid.CreateVersion7();
        var tool = new AiMcpProjectedProposalTool(CreateEventDraftAiToolDefinition.Create());
        var mediator = Substitute.For<IMediator>();
        var expectedToken = new CancellationTokenSource().Token;
        mediator.Send(Arg.Any<ProposeAiToolActionCommand>(), expectedToken)
            .Returns(new BaseCommandResponse<Guid>
            {
                Success = true,
                Id = Guid.CreateVersion7(),
                Message = "Confirm the proposed action before side effects."
            });
        await using var services = new ServiceCollection()
            .AddSingleton(mediator)
            .BuildServiceProvider();
        var request = new RequestContext<CallToolRequestParams>(
            Substitute.For<McpServer>(),
            new JsonRpcRequest { Method = "tools/call" },
            new CallToolRequestParams
            {
                Name = tool.ProtocolTool.Name,
                Arguments = CreateArguments(
                    $$"""
                      {
                        "conversationId": "{{conversationId}}",
                        "summary": "Draft an event",
                        "title": "Projected MCP draft"
                      }
                      """)
            })
        {
            Services = services
        };

        var result = await tool.InvokeAsync(request, expectedToken);

        result.IsError.Should().BeFalse();
        await mediator.Received(1).Send(
            Arg.Is<ProposeAiToolActionCommand>(command =>
                command.ConversationId == conversationId &&
                command.ToolName == "CreateEventDraft" &&
                command.Summary == "Draft an event" &&
                command.PayloadJson.Contains("Projected MCP draft", StringComparison.Ordinal)),
            expectedToken);
    }

    [Test]
    public void McpAdapterTelemetry_NormalizesUntrustedDiagnosticsDimensions()
    {
        McpAdapterTelemetry.ActivitySourceName.Should().Be("Explore.Mcp");
        McpAdapterTelemetry.MeterName.Should().Be("Explore.Mcp");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("Bearer secret propose_create_event_draft").Should().Be("unknown");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("propose_create_event_draft").Should().Be("propose_create_event_draft");
        McpAdapterTelemetry.NormalizeFailureCodeForDiagnostics("private-prompt-marker").Should().Be("unknown");
        McpAdapterTelemetry.NormalizeFailureCodeForDiagnostics("invalid_tool_arguments").Should().Be("invalid_tool_arguments");
    }

    private static Dictionary<string, JsonElement> CreateArguments(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);
    }
}
