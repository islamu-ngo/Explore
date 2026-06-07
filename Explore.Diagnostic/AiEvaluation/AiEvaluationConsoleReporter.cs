// ABOUTME: Formats advisory AI evaluation report output for local operators and CI artifacts.
// ABOUTME: Prints only scenario status metadata and generated file paths, never prompts or payloads.

namespace Explore.Diagnostic.AiEvaluation;

public static class AiEvaluationConsoleReporter
{
    public static void WriteHelp(TextWriter writer)
    {
        writer.WriteLine("Explore advisory AI evaluation report");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine("  dotnet run --project Explore.Diagnostic -- ai-eval-report [--root <path>] [--output <path>]");
        writer.WriteLine();
        writer.WriteLine("The report is advisory and deterministic; normal runs do not call live AI providers.");
    }

    public static void WriteReport(TextWriter writer, AiEvaluationReport report, AiEvaluationReportArtifact artifact)
    {
        writer.WriteLine("Explore AI Evaluation Report");
        writer.WriteLine("============================");
        writer.WriteLine();
        writer.WriteLine($"Summary: {report.PassCount} PASS, {report.WarnCount} WARN, {report.FailCount} FAIL");
        writer.WriteLine($"Advisory only: {report.AdvisoryOnly.ToString().ToLowerInvariant()}");
        writer.WriteLine($"JSON: {artifact.JsonPath}");
        writer.WriteLine($"Markdown: {artifact.MarkdownPath}");
    }
}
