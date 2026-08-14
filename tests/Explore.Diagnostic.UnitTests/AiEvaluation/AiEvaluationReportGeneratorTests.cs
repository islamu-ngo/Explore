// ABOUTME: Unit tests for deterministic advisory AI evaluation report generation.
// ABOUTME: Verifies required ATCR evaluation dimensions are covered without live provider calls.

using System.Text.Json;
using Explore.Diagnostic.AiEvaluation;

namespace Explore.Diagnostic.UnitTests.AiEvaluation;

public sealed class AiEvaluationReportGeneratorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 7, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task GenerateCoversRequiredAdvisoryDimensions()
    {
        var report = new AiEvaluationReportGenerator(() => FixedNow).Generate();

        await Assert.That(report.AdvisoryOnly).IsTrue();
        await Assert.That(report.ContainsHardCiGate).IsFalse();
        await Assert.That(report.Results.Select(result => result.Dimension)).IsEquivalentTo(Enum.GetValues<AiEvaluationDimension>());
        await Assert.That(report.Results.All(result => result.Status == AiEvaluationStatus.Pass)).IsTrue();
    }

    [Test]
    public async Task GenerateDoesNotExposeRawRejectedPayloadContentInArtifacts()
    {
        var report = new AiEvaluationReportGenerator(() => FixedNow).Generate();

        var json = AiEvaluationReportWriter.ToJson(report);
        var markdown = AiEvaluationReportWriter.ToMarkdown(report);
        var combined = json + markdown;

        await Assert.That(combined).DoesNotContain("018e4e5c");
        await Assert.That(combined).DoesNotContain("tenantId");
        await Assert.That(combined).DoesNotContain("unsafeField");
        await Assert.That(combined).DoesNotContain("Ignore previous instructions");
        await Assert.That(combined).DoesNotContain("<tool>");
    }

    [Test]
    public async Task GenerateIncludesDeterministicMcpProposalFlowEvaluation()
    {
        var report = new AiEvaluationReportGenerator(() => FixedNow).Generate();

        var scenario = report.Results.Single(result => result.Code == "ai.eval.mcp-proposal-flow");

        await Assert.That(scenario.Status).IsEqualTo(AiEvaluationStatus.Pass);
        await Assert.That(scenario.Dimension).IsEqualTo(AiEvaluationDimension.McpProposalFlow);
        await Assert.That(scenario.Summary).Contains("event-management");
        await Assert.That(scenario.Summary).Contains("confirmation-before-side-effects");
        await Assert.That(scenario.Recommendation).Contains("discovery and proposals");
    }

    [Test]
    public async Task ToJsonUsesStringEnumsForReadableTrendArtifacts()
    {
        var report = new AiEvaluationReportGenerator(() => FixedNow).Generate();

        using var document = JsonDocument.Parse(AiEvaluationReportWriter.ToJson(report));
        var firstResult = document.RootElement.GetProperty("results")[0];

        await Assert.That(firstResult.GetProperty("status").GetString()).IsEqualTo("Pass");
        await Assert.That(firstResult.GetProperty("dimension").ValueKind).IsEqualTo(JsonValueKind.String);
    }
}
