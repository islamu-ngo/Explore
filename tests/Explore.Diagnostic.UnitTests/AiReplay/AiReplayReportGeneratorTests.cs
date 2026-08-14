// ABOUTME: Unit tests for deterministic fake/replay AI usability report generation.
// ABOUTME: Verifies assistant and MCP replay scenarios run without live providers or content artifacts.

using System.Text.Json;
using Explore.Diagnostic.AiReplay;

namespace Explore.Diagnostic.UnitTests.AiReplay;

public sealed class AiReplayReportGeneratorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 7, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task GenerateCoversNormalCiReplayScenariosWithoutLiveCredentials()
    {
        var report = new AiReplayReportGenerator(() => FixedNow).Generate();

        await Assert.That(report.UsesLiveProviderCredentials).IsFalse();
        await Assert.That(report.ContainsContentBearingArtifacts).IsFalse();
        await Assert.That(report.HasDatabaseSideEffects).IsFalse();
        await Assert.That(report.IsCiSafe).IsTrue();
        await Assert.That(report.FailCount).IsEqualTo(0);
        await Assert.That(report.PassRate).IsEqualTo(1m);
        await Assert.That(report.Results.Select(result => result.Code)).IsEquivalentTo([
            AiReplayScenarioCodes.AssistantRailProposalPreview,
            AiReplayScenarioCodes.McpInspectorContract,
            AiReplayScenarioCodes.McpProposalFirst,
            AiReplayScenarioCodes.McpProjectedToolSelection,
            AiReplayScenarioCodes.McpConfirmationRequired,
            AiReplayScenarioCodes.AssistantRailMissingHal,
            AiReplayScenarioCodes.InvalidPayloadRecovery,
        ]);
    }

    [Test]
    public async Task GenerateIncludesMcpInspectorContractChecklistWithoutRunningLiveClients()
    {
        var report = new AiReplayReportGenerator(() => FixedNow).Generate();

        var inspectorScenario = report.Results.Single(result => result.Code == AiReplayScenarioCodes.McpInspectorContract);

        await Assert.That(inspectorScenario.Status).IsEqualTo(AiReplayScenarioStatus.Pass);
        await Assert.That(inspectorScenario.Summary).Contains("MCP Inspector");
        await Assert.That(inspectorScenario.Diagnostics).Contains("tools/list");
        await Assert.That(inspectorScenario.Diagnostics).Contains("resources/list");
        await Assert.That(inspectorScenario.Diagnostics).Contains("resources/templates/list");
        await Assert.That(inspectorScenario.Diagnostics).Contains("prompts/list");
        await Assert.That(inspectorScenario.Diagnostics).Contains("propose_create_event_draft");
        await Assert.That(inspectorScenario.Diagnostics).Contains("propose_update_event_draft");
        await Assert.That(inspectorScenario.Diagnostics).Contains("propose_publish_event");
        await Assert.That(inspectorScenario.Diagnostics).Contains("propose_create_event_session");
        await Assert.That(inspectorScenario.Diagnostics).Contains("44 registry-projected proposal tools");
        await Assert.That(inspectorScenario.DatabaseSideEffectsDetected).IsFalse();
    }

    [Test]
    public async Task GenerateIncludesMcpProposalVsExecutionGuidance()
    {
        var report = new AiReplayReportGenerator(() => FixedNow).Generate();

        var projectedToolScenario = report.Results.Single(result => result.Code == AiReplayScenarioCodes.McpProjectedToolSelection);
        var confirmationScenario = report.Results.Single(result => result.Code == AiReplayScenarioCodes.McpConfirmationRequired);

        await Assert.That(projectedToolScenario.Status).IsEqualTo(AiReplayScenarioStatus.Pass);
        await Assert.That(projectedToolScenario.Diagnostics).Contains("propose_create_event_draft");
        await Assert.That(projectedToolScenario.Diagnostics).Contains("propose_update_event_draft");
        await Assert.That(projectedToolScenario.Diagnostics).Contains("sub-resource propose_* tools");
        await Assert.That(projectedToolScenario.Diagnostics).Contains("allow-listed");
        await Assert.That(confirmationScenario.Status).IsEqualTo(AiReplayScenarioStatus.Pass);
        await Assert.That(confirmationScenario.Summary).Contains("proposals only");
        await Assert.That(confirmationScenario.Diagnostics).Contains("before confirmation");
    }

    [Test]
    public async Task GenerateDoesNotExposePromptPayloadTenantOrProviderContent()
    {
        var report = new AiReplayReportGenerator(() => FixedNow).Generate();
        var combined = AiReplayReportWriter.ToJson(report) + AiReplayReportWriter.ToMarkdown(report);

        await Assert.That(AiReplayArtifactSafetyPolicy.ContainsContentBearingData(report)).IsFalse();
        await Assert.That(AiReplayArtifactSafetyPolicy.ContainsContentBearingData(combined)).IsFalse();
        await Assert.That(combined).DoesNotContain("Replay fixture");
        await Assert.That(combined).DoesNotContain("018e4e5c");
        await Assert.That(combined).DoesNotContain("OPENAI_API_KEY");
        await Assert.That(combined).DoesNotContain("gpt-4");
        await Assert.That(combined).DoesNotContain("<tool>");
    }

    [Test]
    public async Task ArtifactSafetyPolicyDetectsForbiddenMarkers()
    {
        await Assert.That(AiReplayArtifactSafetyPolicy.ContainsContentBearingData("raw tool payload includes Replay fixture")).IsTrue();
    }

    [Test]
    public async Task ToJsonUsesReadableStringEnums()
    {
        var report = new AiReplayReportGenerator(() => FixedNow).Generate();

        using var document = JsonDocument.Parse(AiReplayReportWriter.ToJson(report));
        var firstResult = document.RootElement.GetProperty("results")[0];

        await Assert.That(firstResult.GetProperty("status").GetString()).IsEqualTo("Pass");
        await Assert.That(firstResult.GetProperty("failureClass").ValueKind).IsEqualTo(JsonValueKind.String);
    }
}
