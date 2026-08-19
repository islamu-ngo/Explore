// ABOUTME: Tests MCP-style AI tool proposal command behavior and fail-closed ownership checks.
// ABOUTME: Ensures external adapters persist proposals only through registry validation and conversation aggregates.

using System.Text.Json;
using System.Text.Json.Nodes;
using Explore.Application.Authorization;
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
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IAuthorizationProvider _authorizationProvider = Substitute.For<IAuthorizationProvider>();
    private readonly IAiToolContractRegistry _toolRegistry = AiToolContractRegistry.CreateDefault();

    public ProposeAiToolActionCommandHandlerTests()
    {
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(_userId);
        _authorizationProvider
            .AuthorizeAsync(
                Arg.Any<AuthorizationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Allow(AuthorizationProviderMetadata.Runtime));
        _eventRepository
            .GetAuthorizationTargetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<Explore.Domain.Event?>(CreateEvent((Guid)call[0]!, _tenantId)));
        _authorizationProvider.ClearReceivedCalls();
        _eventRepository.ClearReceivedCalls();
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
    public async Task Handle_WhenToolAuthorizationIsDenied_FailsWithoutMutation()
    {
        var eventId = Guid.CreateVersion7();
        var conversation = CreateConversation(_userId);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _authorizationProvider
            .AuthorizeAsync(
                Arg.Is<AuthorizationRequest>(request =>
                    request != null &&
                    request.ResourceKind == ResourceKinds.Event &&
                    request.ResourceId == eventId.ToString() &&
                    request.Action == AuthorizationActions.Events.ModerateHeavy),
                Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Deny(AuthorizationProviderMetadata.Runtime));

        var result = await CreateHandler().Handle(CreateCommand(
            toolName: "HeavyModerateEvent",
            payloadJson: CreateHeavyModerationPayload(eventId)), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("tool_authorization_denied");
        await Assert.That(conversation.ProposedActions).IsEmpty();
        await _conversationRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Test]
    public async Task Handle_WhenModerationPayloadIsAuthorized_PassesTargetContextToAuthorizationProvider()
    {
        var eventId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var conversation = CreateConversation(_userId, actorId: actorId);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var result = await CreateHandler().Handle(CreateCommand(
            toolName: "HeavyModerateEvent",
            payloadJson: CreateHeavyModerationPayload(eventId)), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(conversation.ProposedActions.Count).IsEqualTo(1);
        await Assert.That(conversation.ProposedActions.Single().Kind).IsEqualTo(AiProposedActionKind.HeavyModerateEvent);

        // The facts describe the event that was actually loaded, not what the model asked for. The
        // conversation's actor is not published: a proposal is decided on the caller's own authority.
        await _authorizationProvider.Received(1).AuthorizeAsync(
            Arg.Is<AuthorizationRequest>(request =>
                request != null &&
                request.ResourceKind == ResourceKinds.Event &&
                request.ResourceId == eventId.ToString() &&
                request.Action == AuthorizationActions.Events.ModerateHeavy &&
                Equals(request.Facts, new EventScopedAuthorizationFacts(_tenantId, eventId))),
            Arg.Any<CancellationToken>());
        await _conversationRepository.Received(1).Update(conversation);
    }

    [Test]
    public async Task Handle_WhenEventTargetIsCrossTenant_UsesResolvedTargetTenantForAuthorization()
    {
        var eventId = Guid.CreateVersion7();
        var targetTenantId = Guid.CreateVersion7();
        var conversation = CreateConversation(_userId);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _eventRepository.GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Explore.Domain.Event?>(CreateEvent(eventId, targetTenantId)));

        var result = await CreateHandler().Handle(CreateCommand(
            toolName: "HeavyModerateEvent",
            payloadJson: CreateHeavyModerationPayload(eventId)), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _authorizationProvider.Received(1).AuthorizeAsync(
            Arg.Is<AuthorizationRequest>(request =>
                request != null &&
                request.ResourceKind == ResourceKinds.Event &&
                request.ResourceId == eventId.ToString() &&
                request.Action == AuthorizationActions.Events.ModerateHeavy &&
                Equals(request.Facts, new EventScopedAuthorizationFacts(targetTenantId, eventId))),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenEventTargetCannotBeResolved_FailsClosedWithoutPersistingProposal()
    {
        var eventId = Guid.CreateVersion7();
        var conversation = CreateConversation(_userId);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _eventRepository.GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Explore.Domain.Event?>(null));

        var result = await CreateHandler().Handle(CreateCommand(
            toolName: "HeavyModerateEvent",
            payloadJson: CreateHeavyModerationPayload(eventId)), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("tool_authorization_denied");
        await Assert.That(conversation.ProposedActions).IsEmpty();
        await _authorizationProvider.DidNotReceiveWithAnyArgs().AuthorizeAsync(default!, default);
        await _conversationRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Test]
    public async Task Handle_WhenPayloadIsValid_PersistsProposedActionWithoutExecutingTool()
    {
        var actorId = Guid.CreateVersion7();
        var conversation = CreateConversation(_userId, actorId: actorId);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(conversation.ProposedActions.Count).IsEqualTo(1);
        var proposedAction = conversation.ProposedActions.Single();
        await Assert.That(proposedAction.Id).IsEqualTo(result.Id);
        await Assert.That(proposedAction.Kind).IsEqualTo(AiProposedActionKind.CreateEventDraft);
        await Assert.That(proposedAction.CreatedBy).IsEqualTo(_userId);
        await Assert.That(proposedAction.ActingActorId).IsEqualTo(actorId);
        await Assert.That(proposedAction.PayloadJson).Contains("MCP event draft");
        await _conversationRepository.Received(1).Update(conversation);
    }

    [Test]
    public async Task Handle_WhenUpdateDraftPayloadIsValid_PersistsUpdateProposedActionWithoutExecutingTool()
    {
        var conversation = CreateConversation(_userId);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var result = await CreateHandler().Handle(CreateCommand(
            toolName: "UpdateEventDraft",
            payloadJson: $$"""
              {
                "eventId": "{{Guid.CreateVersion7()}}",
                "expectedConcurrencyStamp": "{{Guid.CreateVersion7()}}",
                "expectedParticipationConfigurationConcurrencyStamp": "{{Guid.CreateVersion7()}}",
                "participationConfiguration": { "participationHandlingModeId": 1, "advanceRegistrationObligationId": 1 },
                "title": "Updated MCP event draft"
              }
              """), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(conversation.ProposedActions.Count).IsEqualTo(1);
        var proposedAction = conversation.ProposedActions.Single();
        await Assert.That(proposedAction.Kind).IsEqualTo(AiProposedActionKind.UpdateEventDraft);
        await Assert.That(proposedAction.PayloadJson).Contains("Updated MCP event draft");
        await _conversationRepository.Received(1).Update(conversation);
    }

    [Test]
    public async Task Handle_WhenPublishPayloadIsValid_PersistsPublishProposedActionWithoutExecutingTool()
    {
        var conversation = CreateConversation(_userId);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var result = await CreateHandler().Handle(CreateCommand(
            toolName: "PublishEvent",
            payloadJson: $$"""
              {
                "eventId": "{{Guid.CreateVersion7()}}",
                "expectedConcurrencyStamp": "{{Guid.CreateVersion7()}}",
                "readinessIsReady": true,
                "readinessErrorCount": 0
              }
              """), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(conversation.ProposedActions.Count).IsEqualTo(1);
        var proposedAction = conversation.ProposedActions.Single();
        await Assert.That(proposedAction.Kind).IsEqualTo(AiProposedActionKind.PublishEvent);
        await Assert.That(proposedAction.PayloadJson).Contains("readinessIsReady");
        await _conversationRepository.Received(1).Update(conversation);
    }

    [Test]
    public async Task Handle_WhenRemainingPhaseFourPayloadsAreValid_PersistsExpectedProposedActionKinds()
    {
        var cases = new[]
        {
            new ProposalCase(
                "DeleteEvent",
                $$"""
                  {
                    "eventId": "{{Guid.CreateVersion7()}}",
                    "expectedConcurrencyStamp": "{{Guid.CreateVersion7()}}",
                    "managementContextHasDelete": true,
                    "destructiveSummary": "Delete duplicate event.",
                    "confirmationPhrase": "DELETE_EVENT",
                    "acknowledgedConsequences": true
                  }
                  """,
                AiProposedActionKind.DeleteEvent),
            new ProposalCase(
                "UpsertEventIslamicAspect",
                $$"""
                  {
                    "eventId": "{{Guid.CreateVersion7()}}",
                    "expectedConcurrencyStamp": "{{Guid.CreateVersion7()}}",
                    "aspectKind": "islamic",
                    "managementContextHasEdit": true,
                    "genderMode": 0
                  }
                  """,
                AiProposedActionKind.UpsertEventIslamicAspect),
            new ProposalCase(
                "DeleteEventIslamicAspect",
                $$"""
                  {
                    "eventId": "{{Guid.CreateVersion7()}}",
                    "expectedConcurrencyStamp": "{{Guid.CreateVersion7()}}",
                    "aspectKind": "islamic",
                    "managementContextHasEdit": true,
                    "destructiveSummary": "Remove stale Islamic aspect.",
                    "confirmationPhrase": "DELETE_ISLAMIC_ASPECT",
                    "acknowledgedConsequences": true
                  }
                  """,
                AiProposedActionKind.DeleteEventIslamicAspect),
            new ProposalCase(
                "UpsertEventTechAspect",
                $$"""
                  {
                    "eventId": "{{Guid.CreateVersion7()}}",
                    "expectedConcurrencyStamp": "{{Guid.CreateVersion7()}}",
                    "aspectKind": "tech",
                    "managementContextHasEdit": true,
                    "skillLevel": 0
                  }
                  """,
                AiProposedActionKind.UpsertEventTechAspect),
            new ProposalCase(
                "DeleteEventTechAspect",
                $$"""
                  {
                    "eventId": "{{Guid.CreateVersion7()}}",
                    "expectedConcurrencyStamp": "{{Guid.CreateVersion7()}}",
                    "aspectKind": "tech",
                    "managementContextHasEdit": true,
                    "destructiveSummary": "Remove stale Tech aspect.",
                    "confirmationPhrase": "DELETE_TECH_ASPECT",
                    "acknowledgedConsequences": true
                  }
                  """,
                AiProposedActionKind.DeleteEventTechAspect)
        };

        foreach (var proposalCase in cases)
        {
            var conversation = CreateConversation(_userId);
            _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
                .Returns(conversation);

            var result = await CreateHandler().Handle(CreateCommand(
                toolName: proposalCase.ToolName,
                payloadJson: proposalCase.PayloadJson), CancellationToken.None);

            await Assert.That(result.Success).IsTrue();
            await Assert.That(conversation.ProposedActions.Count).IsEqualTo(1);
            await Assert.That(conversation.ProposedActions.Single().Kind).IsEqualTo(proposalCase.Kind);
            await _conversationRepository.Received().Update(conversation);
            _conversationRepository.ClearReceivedCalls();
        }
    }

    [Test]
    public async Task Handle_WhenPhaseFivePayloadsAreValid_PersistsExpectedProposedActionKinds()
    {
        foreach (var definition in EventSubResourceAiToolDefinitions.CreateAll())
        {
            var conversation = CreateConversation(_userId);
            _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
                .Returns(conversation);

            var result = await CreateHandler().Handle(CreateCommand(
                toolName: definition.Name,
                payloadJson: BuildMinimalPayloadJson(definition)), CancellationToken.None);

            await Assert.That(result.Success).IsTrue();
            await Assert.That(conversation.ProposedActions.Count).IsEqualTo(1);
            await Assert.That(conversation.ProposedActions.Single().Kind).IsEqualTo(definition.Kind);
            await _conversationRepository.Received().Update(conversation);
            _conversationRepository.ClearReceivedCalls();
        }
    }

    private ProposeAiToolActionCommandHandler CreateHandler()
        => new(_conversationRepository, _eventRepository, _toolRegistry, _currentUserService, _authorizationProvider);

    private ProposeAiToolActionCommand CreateCommand(
        string toolName = "CreateEventDraft",
        string payloadJson = "{ \"title\": \"MCP event draft\", \"participationConfiguration\": { \"participationHandlingModeId\": 1, \"advanceRegistrationObligationId\": 1 } }")
        => new()
        {
            ConversationId = _conversationId,
            ToolName = toolName,
            PayloadJson = payloadJson,
            Summary = "Propose an event draft"
        };

    private AiConversation CreateConversation(
        Guid userId,
        AiConversationStatus status = AiConversationStatus.Active,
        Guid? actorId = null)
        => new()
        {
            Id = _conversationId,
            TenantId = _tenantId,
            UserId = userId,
            ActorId = actorId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.CreateVersion7()
        };

    private static Explore.Domain.Event CreateEvent(Guid id, Guid tenantId)
        => new()
        {
            Id = id,
            TenantId = tenantId,
            Title = "Authorization target event",
            Actor = null!,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };

    private static string CreateHeavyModerationPayload(Guid eventId)
        => $$"""
          {
            "eventId": "{{eventId}}",
            "expectedConcurrencyStamp": "{{Guid.CreateVersion7()}}",
            "managementContextHasModerateHeavy": true,
            "reasonCode": "policy-review",
            "destructiveSummary": "Restrict event visibility until policy review completes.",
            "confirmationPhrase": "HEAVY_MODERATE_EVENT",
            "acknowledgedConsequences": true
          }
          """;

    private static string BuildMinimalPayloadJson(AiToolDefinition definition)
    {
        using var schema = JsonDocument.Parse(definition.JsonSchema);
        var root = schema.RootElement;
        var properties = root.GetProperty("properties");
        var payload = new JsonObject();

        foreach (var field in root.GetProperty("required").EnumerateArray())
        {
            var fieldName = field.GetString()!;
            payload[fieldName] = CreateValidValue(properties.GetProperty(fieldName));
        }

        return payload.ToJsonString();
    }

    private static JsonNode? CreateValidValue(JsonElement schema)
    {
        if (schema.TryGetProperty("enum", out var enumValues) && enumValues.GetArrayLength() > 0)
        {
            return JsonNode.Parse(enumValues[0].GetRawText());
        }

        if (schema.TryGetProperty("type", out var type))
        {
            if (type.ValueKind == JsonValueKind.Array)
            {
                foreach (var candidate in type.EnumerateArray())
                {
                    var value = CreateValueForType(candidate.GetString(), schema);
                    if (value is not null)
                    {
                        return value;
                    }
                }

                return null;
            }

            return CreateValueForType(type.GetString(), schema);
        }

        return "value";
    }

    private static JsonNode? CreateValueForType(string? type, JsonElement schema)
        => type switch
        {
            "string" when schema.TryGetProperty("format", out var format) &&
                string.Equals(format.GetString(), "uuid", StringComparison.OrdinalIgnoreCase)
                => Guid.CreateVersion7().ToString(),
            "string" when schema.TryGetProperty("format", out var format) &&
                string.Equals(format.GetString(), "date-time", StringComparison.OrdinalIgnoreCase)
                => "2026-07-01T09:00:00Z",
            "string" when schema.TryGetProperty("format", out var format) &&
                string.Equals(format.GetString(), "date", StringComparison.OrdinalIgnoreCase)
                => "2026-07-01",
            "string" => "value",
            "integer" => 1,
            "number" => 1,
            "boolean" => true,
            "array" => new JsonArray(),
            "object" => CreateObjectValue(schema),
            _ => null
        };

    private static JsonObject CreateObjectValue(JsonElement schema)
    {
        var value = new JsonObject();
        if (!schema.TryGetProperty("properties", out var properties) ||
            !schema.TryGetProperty("required", out var requiredFields))
        {
            return value;
        }

        foreach (var requiredField in requiredFields.EnumerateArray())
        {
            var fieldName = requiredField.GetString()!;
            value[fieldName] = CreateValidValue(properties.GetProperty(fieldName));
        }

        return value;
    }

    private sealed record ProposalCase(string ToolName, string PayloadJson, AiProposedActionKind Kind);
}
