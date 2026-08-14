// ABOUTME: Unit tests for review-first MCP debug readiness doctor checks.
// ABOUTME: Ensures the doctor reports redacted docs/tests readiness without invoking MCP clients.

using Explore.Diagnostic.Doctor;
using Explore.Diagnostic.Doctor.Checks;

namespace Explore.Diagnostic.UnitTests.Doctor.Checks;

public sealed class McpDebugReadinessDoctorCheckTests
{
    private const string Root = "/repo";

    [Test]
    public async Task RunAsyncWhenDebugArtifactsExistReturnsPass()
    {
        var fileSystem = CreateFileSystemWithRequiredArtifacts();
        var check = new McpDebugReadinessDoctorCheck(fileSystem, Root);

        var result = await check.RunAsync(CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(DoctorCheckStatus.Pass);
    }

    [Test]
    public async Task RunAsyncWhenProtocolHarnessIsMissingReturnsWarn()
    {
        var fileSystem = CreateFileSystemWithRequiredArtifacts(includeProtocolHarness: false);
        var check = new McpDebugReadinessDoctorCheck(fileSystem, Root);

        var result = await check.RunAsync(CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(DoctorCheckStatus.Warn);
        await Assert.That(result.RedactedEvidence).Contains("missing:Event.API.IntegrationTests/Features/McpProtocolContractTests.cs");
    }

    [Test]
    public async Task RunAsyncWhenDocsContainLikelySecretReturnsWarnWithoutEchoingSecret()
    {
        var fileSystem = CreateFileSystemWithRequiredArtifacts();
        fileSystem.AddFile(
            Path.Combine(Root, "docs/MCP_DEBUGGING.md"),
            "McpProtocolContractTests EventManagementMcpAuthenticatedReadTests resources/templates/list event_management_context propose_create_event_draft propose_update_event_draft redacted Bearer eyJsecret");
        var check = new McpDebugReadinessDoctorCheck(fileSystem, Root);

        var result = await check.RunAsync(CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(DoctorCheckStatus.Warn);
        await Assert.That(result.RedactedEvidence).Contains("docs:possible-secret");
        await Assert.That(result.RedactedEvidence).DoesNotContain("eyJsecret");
    }

    private static FakeDoctorFileSystem CreateFileSystemWithRequiredArtifacts(bool includeProtocolHarness = true)
    {
        var fileSystem = new FakeDoctorFileSystem();
        fileSystem.AddFile(
            Path.Combine(Root, "docs/MCP_DEBUGGING.md"),
            "McpProtocolContractTests EventManagementMcpAuthenticatedReadTests resources/templates/list event_management_context propose_create_event_draft propose_update_event_draft redacted");
        fileSystem.AddFile(Path.Combine(Root, "docs/adr/ADR-011-local-mcp-stdio-diagnostic-host.md"), "stdio decision");
        fileSystem.AddFile(Path.Combine(Root, ".gitignore"), ".mcp.json\n.vscode/*");
        fileSystem.AddFile(Path.Combine(Root, "Event.API.IntegrationTests/Features/McpProjectedToolTests.cs"), "tests");
        fileSystem.AddFile(Path.Combine(Root, "Explore.Diagnostic/AiReplay/AiReplayScenarioCodes.cs"), "codes");
        fileSystem.AddFile(Path.Combine(Root, "Explore.Diagnostic/AiEvaluation/AiEvaluationReportGenerator.cs"), "eval");
        fileSystem.AddFile(Path.Combine(Root, "Explore.Diagnostic.UnitTests/AiReplay/AiReplayReportGeneratorTests.cs"), "tests");
        fileSystem.AddFile(Path.Combine(Root, "Explore.Diagnostic.UnitTests/AiEvaluation/AiEvaluationReportGeneratorTests.cs"), "tests");

        if (includeProtocolHarness)
        {
            fileSystem.AddFile(Path.Combine(Root, "Event.API.IntegrationTests/Features/McpProtocolContractTests.cs"), "tests");
        }

        return fileSystem;
    }
}
