// ABOUTME: Tests first-class MCP tools projected from the AI Tool Contract Registry.
// ABOUTME: Verifies schema parity, authorization metadata, SDK hints, and proposal-first command mapping.

using System.Text.Json;
using Explore.API.Mcp;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Features.AiAssistant.Tools;
using Explore.Application.Responses;
using Explore.Domain.Ai;
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
    private static readonly string[] ProjectedProposalToolNames = AiMcpProjectedToolFactory
        .CreateTools(AiToolContractRegistry.CreateDefault())
        .Select(tool => tool.ProtocolTool.Name)
        .Order(StringComparer.Ordinal)
        .ToArray();

    [Test]
    public void CreateTools_ProjectsMcpExposedRegistryDefinitions()
    {
        var tools = AiMcpProjectedToolFactory.CreateTools(AiToolContractRegistry.CreateDefault());

        tools.Select(tool => tool.ProtocolTool.Name)
            .Order(StringComparer.Ordinal)
            .Should()
            .Equal(ProjectedProposalToolNames);

        var tool = tools.Single(candidate => candidate.ProtocolTool.Name == "propose_create_event_draft");
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
        tool.ProtocolTool.Meta!["islamuDestructive"]!.GetValue<bool>().Should().BeFalse();
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
    public void ProjectedToolInputSchema_ForUpdateDraft_RequiresEventIdAndConcurrencyStamp()
    {
        var definition = UpdateEventDraftAiToolDefinition.Create();
        var tool = new AiMcpProjectedProposalTool(definition);

        var schema = tool.ProtocolTool.InputSchema;
        var properties = schema.GetProperty("properties");
        properties.TryGetProperty("eventId", out _).Should().BeTrue();
        properties.TryGetProperty("expectedConcurrencyStamp", out _).Should().BeTrue();
        properties.TryGetProperty("tenantId", out _).Should().BeFalse();
        properties.TryGetProperty("actorId", out _).Should().BeFalse();
        properties.TryGetProperty("eventStatusId", out _).Should().BeFalse();

        var required = schema.GetProperty("required")
            .EnumerateArray()
            .Select(value => value.GetString())
            .ToArray();
        required.Should().Contain("conversationId");
        required.Should().Contain("eventId");
        required.Should().Contain("expectedConcurrencyStamp");
        required.Should().Contain("title");
    }

    [Test]
    public void ProjectedToolInputSchema_ForPublishEvent_RequiresEventIdConcurrencyStampAndReadiness()
    {
        var definition = PublishEventAiToolDefinition.Create();
        var tool = new AiMcpProjectedProposalTool(definition);

        var schema = tool.ProtocolTool.InputSchema;
        var properties = schema.GetProperty("properties");
        properties.TryGetProperty("eventId", out _).Should().BeTrue();
        properties.TryGetProperty("expectedConcurrencyStamp", out _).Should().BeTrue();
        properties.TryGetProperty("readinessIsReady", out _).Should().BeTrue();
        properties.TryGetProperty("readinessErrorCount", out _).Should().BeTrue();
        properties.TryGetProperty("tenantId", out _).Should().BeFalse();
        properties.TryGetProperty("eventStatusId", out _).Should().BeFalse();
        properties.TryGetProperty("publishedAt", out _).Should().BeFalse();

        var required = schema.GetProperty("required")
            .EnumerateArray()
            .Select(value => value.GetString())
            .ToArray();
        required.Should().Contain("conversationId");
        required.Should().Contain("eventId");
        required.Should().Contain("expectedConcurrencyStamp");
        required.Should().Contain("readinessIsReady");
        required.Should().Contain("readinessErrorCount");
    }

    [Test]
    public void ProjectedToolInputSchema_ForDeleteEvent_RequiresDestructiveConfirmation()
    {
        var definition = DeleteEventAiToolDefinition.Create();
        var tool = new AiMcpProjectedProposalTool(definition);

        tool.ProtocolTool.Annotations!.DestructiveHint.Should().BeTrue();
        tool.ProtocolTool.Meta!["islamuDestructive"]!.GetValue<bool>().Should().BeTrue();

        var schema = tool.ProtocolTool.InputSchema;
        var properties = schema.GetProperty("properties");
        properties.TryGetProperty("eventId", out _).Should().BeTrue();
        properties.TryGetProperty("expectedConcurrencyStamp", out _).Should().BeTrue();
        properties.TryGetProperty("managementContextHasDelete", out _).Should().BeTrue();
        properties.TryGetProperty("destructiveSummary", out _).Should().BeTrue();
        properties.TryGetProperty("confirmationPhrase", out _).Should().BeTrue();
        properties.TryGetProperty("tenantId", out _).Should().BeFalse();

        var required = schema.GetProperty("required")
            .EnumerateArray()
            .Select(value => value.GetString())
            .ToArray();
        required.Should().Contain([
            "conversationId",
            "eventId",
            "expectedConcurrencyStamp",
            "managementContextHasDelete",
            "destructiveSummary",
            "confirmationPhrase",
            "acknowledgedConsequences"]);
    }

    [Test]
    public void ProjectedToolInputSchema_ForAspectTools_RequiresModuleAndPermissionContext()
    {
        var upsertIslamic = new AiMcpProjectedProposalTool(UpsertEventIslamicAspectAiToolDefinition.Create());
        var deleteTech = new AiMcpProjectedProposalTool(DeleteEventTechAspectAiToolDefinition.Create());

        var upsertProperties = upsertIslamic.ProtocolTool.InputSchema.GetProperty("properties");
        upsertProperties.TryGetProperty("aspectKind", out _).Should().BeTrue();
        upsertProperties.TryGetProperty("managementContextHasEdit", out _).Should().BeTrue();
        upsertProperties.TryGetProperty("genderMode", out _).Should().BeTrue();
        upsertProperties.TryGetProperty("tenantId", out _).Should().BeFalse();
        upsertIslamic.ProtocolTool.Annotations!.DestructiveHint.Should().BeFalse();

        var deleteProperties = deleteTech.ProtocolTool.InputSchema.GetProperty("properties");
        deleteProperties.TryGetProperty("aspectKind", out _).Should().BeTrue();
        deleteProperties.TryGetProperty("managementContextHasEdit", out _).Should().BeTrue();
        deleteProperties.TryGetProperty("confirmationPhrase", out _).Should().BeTrue();
        deleteProperties.TryGetProperty("concurrencyStamp", out _).Should().BeFalse();
        deleteTech.ProtocolTool.Annotations!.DestructiveHint.Should().BeTrue();
    }

    [Test]
    public void ProjectedToolInputSchema_ForPhaseFiveSubResourceTools_RequiresContextAndServerOwnedExclusions()
    {
        var createSession = new AiMcpProjectedProposalTool(FindSubResourceDefinition(AiProposedActionKind.CreateEventSession));
        var applyTemplateSync = new AiMcpProjectedProposalTool(FindSubResourceDefinition(AiProposedActionKind.ApplyEventTemplateSync));

        var createSessionProperties = createSession.ProtocolTool.InputSchema.GetProperty("properties");
        createSessionProperties.TryGetProperty("conversationId", out _).Should().BeTrue();
        createSessionProperties.TryGetProperty("eventId", out _).Should().BeTrue();
        createSessionProperties.TryGetProperty("expectedConcurrencyStamp", out _).Should().BeTrue();
        createSessionProperties.TryGetProperty("managementContextHasAddSession", out _).Should().BeTrue();
        createSessionProperties.TryGetProperty("title", out _).Should().BeTrue();
        createSessionProperties.TryGetProperty("tenantId", out _).Should().BeFalse();
        createSessionProperties.TryGetProperty("userId", out _).Should().BeFalse();

        var createSessionRequired = createSession.ProtocolTool.InputSchema.GetProperty("required")
            .EnumerateArray()
            .Select(value => value.GetString())
            .ToArray();
        createSessionRequired.Should().Contain([
            "conversationId",
            "eventId",
            "expectedConcurrencyStamp",
            "managementContextHasAddSession",
            "title",
            "startTime",
            "endTime"]);

        var templateSyncProperties = applyTemplateSync.ProtocolTool.InputSchema.GetProperty("properties");
        templateSyncProperties.TryGetProperty("plan", out var planProperty).Should().BeTrue();
        planProperty.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();
        planProperty.GetProperty("properties").TryGetProperty("targetTemplateVersion", out _).Should().BeTrue();
        planProperty.GetProperty("properties").TryGetProperty("modifiedDefinitionKeys", out _).Should().BeTrue();
        templateSyncProperties.TryGetProperty("sourceTemplateVersion", out _).Should().BeFalse();
        templateSyncProperties.TryGetProperty("tenantId", out _).Should().BeFalse();
    }

    [Test]
    public void ProjectedToolInputSchema_ForModerationTools_RequiresHalContextAndHeavyAcknowledgement()
    {
        var lightModeration = new AiMcpProjectedProposalTool(FindModerationDefinition(AiProposedActionKind.LightModerateEvent));
        var heavyModeration = new AiMcpProjectedProposalTool(FindModerationDefinition(AiProposedActionKind.HeavyModerateEvent));

        lightModeration.ProtocolTool.Name.Should().Be("propose_light_moderate_event");
        lightModeration.ProtocolTool.Meta!["islamuRiskClass"]!.GetValue<string>().Should().Be("High");
        lightModeration.ProtocolTool.Annotations!.DestructiveHint.Should().BeFalse();

        var lightProperties = lightModeration.ProtocolTool.InputSchema.GetProperty("properties");
        lightProperties.TryGetProperty("conversationId", out _).Should().BeTrue();
        lightProperties.TryGetProperty("eventId", out _).Should().BeTrue();
        lightProperties.TryGetProperty("expectedConcurrencyStamp", out _).Should().BeTrue();
        lightProperties.TryGetProperty("managementContextHasModerateLight", out _).Should().BeTrue();
        lightProperties.TryGetProperty("reasonCode", out _).Should().BeTrue();
        lightProperties.TryGetProperty("actorId", out _).Should().BeFalse();
        lightProperties.TryGetProperty("tenantId", out _).Should().BeFalse();

        heavyModeration.ProtocolTool.Name.Should().Be("propose_heavy_moderate_event");
        heavyModeration.ProtocolTool.Meta!["islamuRiskClass"]!.GetValue<string>().Should().Be("Critical");
        heavyModeration.ProtocolTool.Annotations!.DestructiveHint.Should().BeTrue();
        heavyModeration.ProtocolTool.Meta!["islamuDestructive"]!.GetValue<bool>().Should().BeTrue();

        var heavyProperties = heavyModeration.ProtocolTool.InputSchema.GetProperty("properties");
        heavyProperties.TryGetProperty("managementContextHasModerateHeavy", out _).Should().BeTrue();
        heavyProperties.TryGetProperty("destructiveSummary", out _).Should().BeTrue();
        heavyProperties.TryGetProperty("confirmationPhrase", out var confirmationPhrase).Should().BeTrue();
        heavyProperties.TryGetProperty("acknowledgedConsequences", out _).Should().BeTrue();
        confirmationPhrase.GetProperty("enum").EnumerateArray().Single().GetString().Should().Be("HEAVY_MODERATE_EVENT");
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
    public void MapArgumentsToCommand_ForUpdateDraft_CreatesProposalCommandWithDraftPayload()
    {
        var conversationId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var concurrencyStamp = Guid.CreateVersion7();
        var tool = new AiMcpProjectedProposalTool(UpdateEventDraftAiToolDefinition.Create());
        var arguments = CreateArguments(
            $$"""
              {
                "conversationId": "{{conversationId}}",
                "summary": "Update an event draft",
                "eventId": "{{eventId}}",
                "expectedConcurrencyStamp": "{{concurrencyStamp}}",
                "title": "Projected MCP update"
              }
              """);

        var command = tool.MapArgumentsToCommand(arguments);

        command.Should().BeEquivalentTo(
            new ProposeAiToolActionCommand
            {
                ConversationId = conversationId,
                ToolName = "UpdateEventDraft",
                Summary = "Update an event draft"
            },
            options => options.Excluding(candidate => candidate.PayloadJson));

        using var payload = JsonDocument.Parse(command.PayloadJson);
        payload.RootElement.TryGetProperty("conversationId", out _).Should().BeFalse();
        payload.RootElement.TryGetProperty("summary", out _).Should().BeFalse();
        payload.RootElement.GetProperty("eventId").GetGuid().Should().Be(eventId);
        payload.RootElement.GetProperty("expectedConcurrencyStamp").GetGuid().Should().Be(concurrencyStamp);
        payload.RootElement.GetProperty("title").GetString().Should().Be("Projected MCP update");
    }

    [Test]
    public void MapArgumentsToCommand_ForPublishEvent_CreatesProposalCommandWithPublishPayload()
    {
        var conversationId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var concurrencyStamp = Guid.CreateVersion7();
        var tool = new AiMcpProjectedProposalTool(PublishEventAiToolDefinition.Create());
        var arguments = CreateArguments(
            $$"""
              {
                "conversationId": "{{conversationId}}",
                "summary": "Publish an event",
                "eventId": "{{eventId}}",
                "expectedConcurrencyStamp": "{{concurrencyStamp}}",
                "readinessIsReady": true,
                "readinessErrorCount": 0
              }
              """);

        var command = tool.MapArgumentsToCommand(arguments);

        command.Should().BeEquivalentTo(
            new ProposeAiToolActionCommand
            {
                ConversationId = conversationId,
                ToolName = "PublishEvent",
                Summary = "Publish an event"
            },
            options => options.Excluding(candidate => candidate.PayloadJson));

        using var payload = JsonDocument.Parse(command.PayloadJson);
        payload.RootElement.TryGetProperty("conversationId", out _).Should().BeFalse();
        payload.RootElement.TryGetProperty("summary", out _).Should().BeFalse();
        payload.RootElement.GetProperty("eventId").GetGuid().Should().Be(eventId);
        payload.RootElement.GetProperty("expectedConcurrencyStamp").GetGuid().Should().Be(concurrencyStamp);
        payload.RootElement.GetProperty("readinessIsReady").GetBoolean().Should().BeTrue();
        payload.RootElement.GetProperty("readinessErrorCount").GetInt32().Should().Be(0);
    }

    [Test]
    public void MapArgumentsToCommand_ForDeleteEvent_CreatesProposalCommandWithDestructivePayload()
    {
        var conversationId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var concurrencyStamp = Guid.CreateVersion7();
        var tool = new AiMcpProjectedProposalTool(DeleteEventAiToolDefinition.Create());
        var arguments = CreateArguments(
            $$"""
              {
                "conversationId": "{{conversationId}}",
                "summary": "Delete an event",
                "eventId": "{{eventId}}",
                "expectedConcurrencyStamp": "{{concurrencyStamp}}",
                "managementContextHasDelete": true,
                "destructiveSummary": "Delete duplicate draft",
                "confirmationPhrase": "DELETE_EVENT",
                "acknowledgedConsequences": true
              }
              """);

        var command = tool.MapArgumentsToCommand(arguments);

        command.Should().BeEquivalentTo(
            new ProposeAiToolActionCommand
            {
                ConversationId = conversationId,
                ToolName = "DeleteEvent",
                Summary = "Delete an event"
            },
            options => options.Excluding(candidate => candidate.PayloadJson));

        using var payload = JsonDocument.Parse(command.PayloadJson);
        payload.RootElement.GetProperty("eventId").GetGuid().Should().Be(eventId);
        payload.RootElement.GetProperty("expectedConcurrencyStamp").GetGuid().Should().Be(concurrencyStamp);
        payload.RootElement.GetProperty("managementContextHasDelete").GetBoolean().Should().BeTrue();
        payload.RootElement.GetProperty("confirmationPhrase").GetString().Should().Be("DELETE_EVENT");
    }

    [Test]
    public void MapArgumentsToCommand_ForAspectTool_CreatesProposalCommandWithAspectPayload()
    {
        var conversationId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var concurrencyStamp = Guid.CreateVersion7();
        var tool = new AiMcpProjectedProposalTool(UpsertEventTechAspectAiToolDefinition.Create());
        var arguments = CreateArguments(
            $$"""
              {
                "conversationId": "{{conversationId}}",
                "summary": "Update Tech aspect",
                "eventId": "{{eventId}}",
                "expectedConcurrencyStamp": "{{concurrencyStamp}}",
                "aspectKind": "tech",
                "managementContextHasEdit": true,
                "skillLevel": 0,
                "requiresLaptop": true
              }
              """);

        var command = tool.MapArgumentsToCommand(arguments);

        command.ToolName.Should().Be("UpsertEventTechAspect");
        command.ConversationId.Should().Be(conversationId);
        command.Summary.Should().Be("Update Tech aspect");
        using var payload = JsonDocument.Parse(command.PayloadJson);
        payload.RootElement.GetProperty("eventId").GetGuid().Should().Be(eventId);
        payload.RootElement.GetProperty("expectedConcurrencyStamp").GetGuid().Should().Be(concurrencyStamp);
        payload.RootElement.GetProperty("aspectKind").GetString().Should().Be("tech");
        payload.RootElement.GetProperty("managementContextHasEdit").GetBoolean().Should().BeTrue();
        payload.RootElement.GetProperty("skillLevel").GetInt32().Should().Be(0);
        payload.RootElement.GetProperty("requiresLaptop").GetBoolean().Should().BeTrue();
    }

    [Test]
    public void MapArgumentsToCommand_ForPhaseFiveSubResourceTool_CreatesProposalCommandWithPayloadOnly()
    {
        var conversationId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var concurrencyStamp = Guid.CreateVersion7();
        var tool = new AiMcpProjectedProposalTool(FindSubResourceDefinition(AiProposedActionKind.CreateEventSession));
        var arguments = CreateArguments(
            $$"""
              {
                "conversationId": "{{conversationId}}",
                "summary": "Create a session",
                "eventId": "{{eventId}}",
                "expectedConcurrencyStamp": "{{concurrencyStamp}}",
                "managementContextHasAddSession": true,
                "title": "Opening session",
                "startTime": "2026-07-01T09:00:00Z",
                "endTime": "2026-07-01T10:00:00Z"
              }
              """);

        var command = tool.MapArgumentsToCommand(arguments);

        command.ToolName.Should().Be("CreateEventSession");
        command.ConversationId.Should().Be(conversationId);
        command.Summary.Should().Be("Create a session");
        using var payload = JsonDocument.Parse(command.PayloadJson);
        payload.RootElement.TryGetProperty("conversationId", out _).Should().BeFalse();
        payload.RootElement.TryGetProperty("summary", out _).Should().BeFalse();
        payload.RootElement.GetProperty("eventId").GetGuid().Should().Be(eventId);
        payload.RootElement.GetProperty("expectedConcurrencyStamp").GetGuid().Should().Be(concurrencyStamp);
        payload.RootElement.GetProperty("managementContextHasAddSession").GetBoolean().Should().BeTrue();
        payload.RootElement.GetProperty("title").GetString().Should().Be("Opening session");
    }

    [Test]
    public void ProjectedToolOptionsSetup_AddsRegistryProjectionToMcpOptions()
    {
        var options = new McpServerOptions();

        new AiMcpProjectedToolOptionsSetup(AiToolContractRegistry.CreateDefault()).Configure(options);

        options.ToolCollection.Should().NotBeNull();
        options.ToolCollection!.PrimitiveNames.Should().Contain(ProjectedProposalToolNames);
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
        foreach (var toolName in ProjectedProposalToolNames)
        {
            McpAdapterTelemetry.NormalizeToolNameForDiagnostics(toolName).Should().Be(toolName);
        }

        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("search_public_events").Should().Be("search_public_events");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("get_public_event").Should().Be("get_public_event");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("get_public_event_program_summary").Should().Be("get_public_event_program_summary");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("list_public_event_sessions").Should().Be("list_public_event_sessions");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("list_my_events").Should().Be("list_my_events");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("get_event_creation_context").Should().Be("get_event_creation_context");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("get_event_publish_readiness").Should().Be("get_event_publish_readiness");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("get_event_program_management_context").Should().Be("get_event_program_management_context");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("get_event_custom_properties_context").Should().Be("get_event_custom_properties_context");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("get_event_registrations_context").Should().Be("get_event_registrations_context");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("get_event_team_context").Should().Be("get_event_team_context");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("get_event_template_catalog_context").Should().Be("get_event_template_catalog_context");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("get_event_template_sync_context").Should().Be("get_event_template_sync_context");
        McpAdapterTelemetry.NormalizeToolNameForDiagnostics("get_event_session_template_sync_context").Should().Be("get_event_session_template_sync_context");
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

    private static AiToolDefinition FindSubResourceDefinition(AiProposedActionKind kind)
        => EventSubResourceAiToolDefinitions.CreateAll().Single(definition => definition.Kind == kind);

    private static AiToolDefinition FindModerationDefinition(AiProposedActionKind kind)
        => EventModerationAiToolDefinitions.CreateAll().Single(definition => definition.Kind == kind);
}
