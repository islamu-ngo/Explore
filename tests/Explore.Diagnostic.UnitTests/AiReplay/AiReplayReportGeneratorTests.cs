// ABOUTME: Unit tests for deterministic fake/replay AI usability report generation.
// ABOUTME: Verifies assistant and MCP replay scenarios run without live providers or content artifacts.

using System.Text.Json;
using Explore.Diagnostic.AiReplay;
using FluentAssertions;

namespace Explore.Diagnostic.UnitTests.AiReplay;

public sealed class AiReplayReportGeneratorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 7, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void GenerateCoversNormalCiReplayScenariosWithoutLiveCredentials()
    {
        var report = new AiReplayReportGenerator(() => FixedNow).Generate();

        report.UsesLiveProviderCredentials.Should().BeFalse();
        report.ContainsContentBearingArtifacts.Should().BeFalse();
        report.HasDatabaseSideEffects.Should().BeFalse();
        report.IsCiSafe.Should().BeTrue();
        report.FailCount.Should().Be(0);
        report.PassRate.Should().Be(1m);
        report.Results.Select(result => result.Code).Should().BeEquivalentTo(
            AiReplayScenarioCodes.AssistantRailProposalPreview,
            AiReplayScenarioCodes.McpInspectorContract,
            AiReplayScenarioCodes.McpProposalFirst,
            AiReplayScenarioCodes.McpProjectedToolSelection,
            AiReplayScenarioCodes.McpConfirmationRequired,
            AiReplayScenarioCodes.AssistantRailMissingHal,
            AiReplayScenarioCodes.InvalidPayloadRecovery);
    }

    [Test]
    public void GenerateIncludesMcpInspectorContractChecklistWithoutRunningLiveClients()
    {
        var report = new AiReplayReportGenerator(() => FixedNow).Generate();

        var inspectorScenario = report.Results.Single(result => result.Code == AiReplayScenarioCodes.McpInspectorContract);

        inspectorScenario.Status.Should().Be(AiReplayScenarioStatus.Pass);
        inspectorScenario.Summary.Should().Contain("MCP Inspector");
        inspectorScenario.Diagnostics.Should().Contain("tools/list");
        inspectorScenario.Diagnostics.Should().Contain("resources/list");
        inspectorScenario.Diagnostics.Should().Contain("resources/templates/list");
        inspectorScenario.Diagnostics.Should().Contain("prompts/list");
        inspectorScenario.Diagnostics.Should().Contain("propose_create_event_draft");
        inspectorScenario.Diagnostics.Should().Contain("propose_update_event_draft");
        inspectorScenario.Diagnostics.Should().Contain("propose_publish_event");
        inspectorScenario.Diagnostics.Should().Contain("propose_create_event_session");
        inspectorScenario.Diagnostics.Should().Contain("44 registry-projected proposal tools");
        inspectorScenario.DatabaseSideEffectsDetected.Should().BeFalse();
    }

    [Test]
    public void GenerateIncludesMcpProposalVsExecutionGuidance()
    {
        var report = new AiReplayReportGenerator(() => FixedNow).Generate();

        var projectedToolScenario = report.Results.Single(result => result.Code == AiReplayScenarioCodes.McpProjectedToolSelection);
        var confirmationScenario = report.Results.Single(result => result.Code == AiReplayScenarioCodes.McpConfirmationRequired);

        projectedToolScenario.Status.Should().Be(AiReplayScenarioStatus.Pass);
        projectedToolScenario.Diagnostics.Should().Contain("propose_create_event_draft");
        projectedToolScenario.Diagnostics.Should().Contain("propose_update_event_draft");
        projectedToolScenario.Diagnostics.Should().Contain("sub-resource propose_* tools");
        projectedToolScenario.Diagnostics.Should().Contain("allow-listed");
        confirmationScenario.Status.Should().Be(AiReplayScenarioStatus.Pass);
        confirmationScenario.Summary.Should().Contain("proposals only");
        confirmationScenario.Diagnostics.Should().Contain("before confirmation");
    }

    [Test]
    public void GenerateDoesNotExposePromptPayloadTenantOrProviderContent()
    {
        var report = new AiReplayReportGenerator(() => FixedNow).Generate();
        var combined = AiReplayReportWriter.ToJson(report) + AiReplayReportWriter.ToMarkdown(report);

        AiReplayArtifactSafetyPolicy.ContainsContentBearingData(report).Should().BeFalse();
        AiReplayArtifactSafetyPolicy.ContainsContentBearingData(combined).Should().BeFalse();
        combined.Should().NotContain("Replay fixture");
        combined.Should().NotContain("018e4e5c");
        combined.Should().NotContain("OPENAI_API_KEY");
        combined.Should().NotContain("gpt-4");
        combined.Should().NotContain("<tool>");
    }

    [Test]
    public void ArtifactSafetyPolicyDetectsForbiddenMarkers()
    {
        AiReplayArtifactSafetyPolicy.ContainsContentBearingData("raw tool payload includes Replay fixture").Should().BeTrue();
    }

    [Test]
    public void ToJsonUsesReadableStringEnums()
    {
        var report = new AiReplayReportGenerator(() => FixedNow).Generate();

        using var document = JsonDocument.Parse(AiReplayReportWriter.ToJson(report));
        var firstResult = document.RootElement.GetProperty("results")[0];

        firstResult.GetProperty("status").GetString().Should().Be("Pass");
        firstResult.GetProperty("failureClass").ValueKind.Should().Be(JsonValueKind.String);
    }
}
