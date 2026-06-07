// ABOUTME: Unit tests for deterministic advisory AI evaluation report generation.
// ABOUTME: Verifies required ATCR evaluation dimensions are covered without live provider calls.

using System.Text.Json;
using Explore.Diagnostic.AiEvaluation;
using FluentAssertions;

namespace Explore.Diagnostic.UnitTests.AiEvaluation;

public sealed class AiEvaluationReportGeneratorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 7, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void GenerateCoversRequiredAdvisoryDimensions()
    {
        var report = new AiEvaluationReportGenerator(() => FixedNow).Generate();

        report.AdvisoryOnly.Should().BeTrue();
        report.ContainsHardCiGate.Should().BeFalse();
        report.Results.Select(result => result.Dimension).Should().BeEquivalentTo(Enum.GetValues<AiEvaluationDimension>());
        report.Results.Should().OnlyContain(result => result.Status == AiEvaluationStatus.Pass);
    }

    [Test]
    public void GenerateDoesNotExposeRawRejectedPayloadContentInArtifacts()
    {
        var report = new AiEvaluationReportGenerator(() => FixedNow).Generate();

        var json = AiEvaluationReportWriter.ToJson(report);
        var markdown = AiEvaluationReportWriter.ToMarkdown(report);
        var combined = json + markdown;

        combined.Should().NotContain("018e4e5c");
        combined.Should().NotContain("tenantId");
        combined.Should().NotContain("unsafeField");
        combined.Should().NotContain("Ignore previous instructions");
        combined.Should().NotContain("<tool>");
    }

    [Test]
    public void GenerateIncludesDeterministicMcpProposalFlowEvaluation()
    {
        var report = new AiEvaluationReportGenerator(() => FixedNow).Generate();

        var scenario = report.Results.Single(result => result.Code == "ai.eval.mcp-proposal-flow");

        scenario.Status.Should().Be(AiEvaluationStatus.Pass);
        scenario.Dimension.Should().Be(AiEvaluationDimension.McpProposalFlow);
        scenario.Summary.Should().Contain("confirmation-before-side-effects");
        scenario.Recommendation.Should().Contain("discovery and proposals");
    }

    [Test]
    public void ToJsonUsesStringEnumsForReadableTrendArtifacts()
    {
        var report = new AiEvaluationReportGenerator(() => FixedNow).Generate();

        using var document = JsonDocument.Parse(AiEvaluationReportWriter.ToJson(report));
        var firstResult = document.RootElement.GetProperty("results")[0];

        firstResult.GetProperty("status").GetString().Should().Be("Pass");
        firstResult.GetProperty("dimension").ValueKind.Should().Be(JsonValueKind.String);
    }
}
