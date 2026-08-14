// ABOUTME: Unit tests for fake/replay AI usability report artifact writing.
// ABOUTME: Ensures generated artifacts are redacted, deterministic, and explicit about no live provider usage.

using Explore.Diagnostic.AiReplay;

namespace Explore.Diagnostic.UnitTests.AiReplay;

public sealed class AiReplayReportWriterTests
{
    [Test]
    public async Task WriteCreatesJsonAndMarkdownArtifacts()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "explore-ai-replay-" + Guid.NewGuid().ToString("N"));
        var report = new AiReplayReportGenerator(() => new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero)).Generate();

        try
        {
            var artifact = AiReplayReportWriter.Write(report, outputDirectory);

            await Assert.That(File.Exists(artifact.JsonPath)).IsTrue();
            await Assert.That(File.Exists(artifact.MarkdownPath)).IsTrue();
            await Assert.That(File.ReadAllText(artifact.MarkdownPath)).Contains("Uses live provider credentials: false");
            await Assert.That(File.ReadAllText(artifact.MarkdownPath)).Contains("Contains content-bearing artifacts: false");
            await Assert.That(File.ReadAllText(artifact.MarkdownPath)).Contains("Database side effects detected: false");
            await Assert.That(File.ReadAllText(artifact.MarkdownPath)).Contains("Pass rate: 100.00");
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
