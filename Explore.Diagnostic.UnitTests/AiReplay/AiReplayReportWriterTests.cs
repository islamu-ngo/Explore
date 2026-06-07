// ABOUTME: Unit tests for fake/replay AI usability report artifact writing.
// ABOUTME: Ensures generated artifacts are redacted, deterministic, and explicit about no live provider usage.

using Explore.Diagnostic.AiReplay;
using FluentAssertions;

namespace Explore.Diagnostic.UnitTests.AiReplay;

public sealed class AiReplayReportWriterTests
{
    [Test]
    public void WriteCreatesJsonAndMarkdownArtifacts()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "explore-ai-replay-" + Guid.NewGuid().ToString("N"));
        var report = new AiReplayReportGenerator(() => new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero)).Generate();

        try
        {
            var artifact = AiReplayReportWriter.Write(report, outputDirectory);

            File.Exists(artifact.JsonPath).Should().BeTrue();
            File.Exists(artifact.MarkdownPath).Should().BeTrue();
            File.ReadAllText(artifact.MarkdownPath).Should().Contain("Uses live provider credentials: false");
            File.ReadAllText(artifact.MarkdownPath).Should().Contain("Contains content-bearing artifacts: false");
            File.ReadAllText(artifact.MarkdownPath).Should().Contain("Database side effects detected: false");
            File.ReadAllText(artifact.MarkdownPath).Should().Contain("Pass rate: 100.00");
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
