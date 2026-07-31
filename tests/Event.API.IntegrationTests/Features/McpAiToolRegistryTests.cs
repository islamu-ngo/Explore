// ABOUTME: Tests the read-only MCP registry discovery tool output.
// ABOUTME: Ensures exposed tool contracts stay registry-backed and avoid prompt/provider secrets.

using System.Text.Json;
using Explore.API.Mcp;
using Explore.Application.Authorization;
using Explore.Application.Features.AiAssistant.Tools;
using FluentAssertions;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class McpAiToolRegistryTests
{
    [Test]
    public void ListAiToolContracts_ReturnsSafeRegistryBackedContracts()
    {
        var tool = new AiToolRegistryMcpTools(AiToolContractRegistry.CreateDefault());

        var json = tool.ListAiToolContracts();

        using var document = JsonDocument.Parse(json);
        var tools = document.RootElement.GetProperty("Tools");
        var expectedMcpToolNames = AiToolContractRegistry.CreateDefault()
            .Definitions
            .Where(definition => definition.ExposeToMcp)
            .Select(AiMcpProjectedToolFactory.BuildToolName)
            .ToArray();
        tools.GetArrayLength().Should().Be(expectedMcpToolNames.Length);

        tools.EnumerateArray()
            .Select(tool => tool.GetProperty("McpToolName").GetString())
            .Should()
            .BeEquivalentTo(expectedMcpToolNames);

        var createEventDraft = tools.EnumerateArray().Single(tool =>
            tool.GetProperty("Name").GetString() == "CreateEventDraft");
        createEventDraft.GetProperty("Name").GetString().Should().Be("CreateEventDraft");
        createEventDraft.GetProperty("McpToolName").GetString().Should().Be("propose_create_event_draft");
        createEventDraft.GetProperty("ConfirmationMode").GetString().Should().Be("Required");
        createEventDraft.GetProperty("AllowedPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain("title");
        createEventDraft.GetProperty("ForbiddenPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain("tenantId");
        createEventDraft.GetProperty("RequiredAuthorization").GetProperty("Action").GetString().Should().Be("create");

        var updateEventDraft = tools.EnumerateArray().Single(tool =>
            tool.GetProperty("Name").GetString() == "UpdateEventDraft");
        updateEventDraft.GetProperty("McpToolName").GetString().Should().Be("propose_update_event_draft");
        updateEventDraft.GetProperty("AllowedPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain(["eventId", "expectedConcurrencyStamp", "title"]);
        updateEventDraft.GetProperty("ForbiddenPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain(["tenantId", "actorId", "eventStatusId", "sessions"]);
        updateEventDraft.GetProperty("RequiredAuthorization").GetProperty("Action").GetString().Should().Be("update");

        var publishEvent = tools.EnumerateArray().Single(tool =>
            tool.GetProperty("Name").GetString() == "PublishEvent");
        publishEvent.GetProperty("McpToolName").GetString().Should().Be("propose_publish_event");
        publishEvent.GetProperty("AllowedPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain(["eventId", "expectedConcurrencyStamp", "readinessIsReady", "readinessErrorCount"]);
        publishEvent.GetProperty("ForbiddenPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain(["tenantId", "actorId", "eventStatusId", "publishedAt", "outboxMessages"]);
        publishEvent.GetProperty("RequiredAuthorization").GetProperty("Action").GetString().Should().Be("update");

        var deleteEvent = tools.EnumerateArray().Single(tool =>
            tool.GetProperty("Name").GetString() == "DeleteEvent");
        deleteEvent.GetProperty("McpToolName").GetString().Should().Be("propose_delete_event");
        deleteEvent.GetProperty("AllowedPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain(["eventId", "expectedConcurrencyStamp", "managementContextHasDelete", "confirmationPhrase"]);
        deleteEvent.GetProperty("ForbiddenPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain(["tenantId", "actorId", "eventStatusId", "sessions", "concurrencyStamp"]);
        deleteEvent.GetProperty("RequiredAuthorization").GetProperty("Action").GetString().Should().Be("delete");

        var upsertIslamicAspect = tools.EnumerateArray().Single(tool =>
            tool.GetProperty("Name").GetString() == "UpsertEventIslamicAspect");
        upsertIslamicAspect.GetProperty("McpToolName").GetString().Should().Be("propose_upsert_event_islamic_aspect");
        upsertIslamicAspect.GetProperty("AllowedPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain(["aspectKind", "managementContextHasEdit", "genderMode"]);

        var deleteTechAspect = tools.EnumerateArray().Single(tool =>
            tool.GetProperty("Name").GetString() == "DeleteEventTechAspect");
        deleteTechAspect.GetProperty("McpToolName").GetString().Should().Be("propose_delete_event_tech_aspect");
        deleteTechAspect.GetProperty("AllowedPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain(["aspectKind", "managementContextHasEdit", "confirmationPhrase", "acknowledgedConsequences"]);

        var createSession = tools.EnumerateArray().Single(tool =>
            tool.GetProperty("Name").GetString() == "CreateEventSession");
        createSession.GetProperty("McpToolName").GetString().Should().Be("propose_create_event_session");
        createSession.GetProperty("AllowedPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain(["eventId", "expectedConcurrencyStamp", "managementContextHasAddSession", "title", "startTime", "endTime"]);
        createSession.GetProperty("ForbiddenPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain(["tenantId", "actorId", "userId", "createdAt", "updatedAt"]);
        createSession.GetProperty("RequiredAuthorization").GetProperty("ResourceKind").GetString().Should().Be(ResourceKinds.EventSession);
        createSession.GetProperty("RequiredAuthorization").GetProperty("Action").GetString().Should().Be(AuthorizationActions.Create);

        var assignGroup = tools.EnumerateArray().Single(tool =>
            tool.GetProperty("Name").GetString() == "AssignSessionToEventSessionGroup");
        assignGroup.GetProperty("McpToolName").GetString().Should().Be("propose_assign_session_to_event_session_group");
        assignGroup.GetProperty("AllowedPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain(["eventId", "expectedConcurrencyStamp", "groupId", "sessionId", "isPrimary", "sortOrder"]);
        assignGroup.GetProperty("ForbiddenPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .NotContain("groupId");

        var templateSync = tools.EnumerateArray().Single(tool =>
            tool.GetProperty("Name").GetString() == "ApplyEventTemplateSync");
        templateSync.GetProperty("McpToolName").GetString().Should().Be("propose_apply_event_template_sync");
        templateSync.GetProperty("AllowedPayloadFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .Contain(["eventId", "expectedConcurrencyStamp", "baseProvenanceVersion", "plan", "confirmationPhrase"]);
        templateSync.GetProperty("RequiredAuthorization").GetProperty("Action").GetString().Should().Be(AuthorizationActions.CustomPropertyTemplates.SyncApply);

        var normalized = json.ToLowerInvariant();
        normalized.Should().NotContain("prompt");
        normalized.Should().NotContain("providerendpoint");
        normalized.Should().NotContain("apikey");
    }
}
