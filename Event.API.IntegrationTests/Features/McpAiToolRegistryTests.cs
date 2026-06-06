// ABOUTME: Tests the read-only MCP registry discovery tool output.
// ABOUTME: Ensures exposed tool contracts stay registry-backed and avoid prompt/provider secrets.

using System.Text.Json;
using Explore.API.Mcp;
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
        tools.GetArrayLength().Should().Be(1);

        var createEventDraft = tools[0];
        createEventDraft.GetProperty("Name").GetString().Should().Be("CreateEventDraft");
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

        var normalized = json.ToLowerInvariant();
        normalized.Should().NotContain("prompt");
        normalized.Should().NotContain("providerendpoint");
        normalized.Should().NotContain("apikey");
    }
}
