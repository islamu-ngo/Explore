// ABOUTME: Unit tests for writing advisory AI evaluation report artifacts.
// ABOUTME: Ensures generated JSON and Markdown files are explicit, redacted, and non-gating.

using Explore.Diagnostic.AiEvaluation;

namespace Explore.Diagnostic.UnitTests.AiEvaluation;

public sealed class AiEvaluationReportWriterTests
{
    [Test]
    public async Task WriteCreatesJsonAndMarkdownArtifacts()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "explore-ai-eval-" + Guid.NewGuid().ToString("N"));
        var report = new AiEvaluationReportGenerator(() => new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero)).Generate();

        try
        {
            var artifact = AiEvaluationReportWriter.Write(report, outputDirectory);

            await Assert.That(File.Exists(artifact.JsonPath)).IsTrue();
            await Assert.That(File.Exists(artifact.MarkdownPath)).IsTrue();
            await Assert.That(File.ReadAllText(artifact.MarkdownPath)).Contains("Advisory only: true");
            await Assert.That(File.ReadAllText(artifact.MarkdownPath)).Contains("Hard CI gate: false");
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
