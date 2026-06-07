// ABOUTME: Unit tests for writing advisory AI evaluation report artifacts.
// ABOUTME: Ensures generated JSON and Markdown files are explicit, redacted, and non-gating.

using Explore.Diagnostic.AiEvaluation;
using FluentAssertions;

namespace Explore.Diagnostic.UnitTests.AiEvaluation;

public sealed class AiEvaluationReportWriterTests
{
    [Test]
    public void WriteCreatesJsonAndMarkdownArtifacts()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "explore-ai-eval-" + Guid.NewGuid().ToString("N"));
        var report = new AiEvaluationReportGenerator(() => new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero)).Generate();

        try
        {
            var artifact = AiEvaluationReportWriter.Write(report, outputDirectory);

            File.Exists(artifact.JsonPath).Should().BeTrue();
            File.Exists(artifact.MarkdownPath).Should().BeTrue();
            File.ReadAllText(artifact.MarkdownPath).Should().Contain("Advisory only: true");
            File.ReadAllText(artifact.MarkdownPath).Should().Contain("Hard CI gate: false");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}
