// ABOUTME: Unit tests for dev-only AI tool readiness doctor checks.
// ABOUTME: Ensures readiness reports are review-first and never mutate or scaffold runtime state.

using Explore.Diagnostic.Doctor;
using Explore.Diagnostic.Doctor.Checks;
using FluentAssertions;

namespace Explore.Diagnostic.UnitTests.Doctor.Checks;

public sealed class AiToolReadinessDoctorCheckTests
{
    private const string Root = "/repo";

    [Test]
    public async Task RunAsyncWhenRequiredArtifactsExistReturnsPass()
    {
        var fileSystem = CreateFileSystemWithRequiredArtifacts();
        var check = new AiToolReadinessDoctorCheck(fileSystem, Root);

        var result = await check.RunAsync(CancellationToken.None);

        result.Status.Should().Be(DoctorCheckStatus.Pass);
    }

    [Test]
    public async Task RunAsyncWhenGeneratedInventoryIsMissingReturnsWarn()
    {
        var fileSystem = CreateFileSystemWithRequiredArtifacts(includeInventory: false);
        var check = new AiToolReadinessDoctorCheck(fileSystem, Root);

        var result = await check.RunAsync(CancellationToken.None);

        result.Status.Should().Be(DoctorCheckStatus.Warn);
        result.RedactedEvidence.Should().Contain("missing:docs/AI_AGENT_CONTRACT_INVENTORY.md");
    }

    private static FakeDoctorFileSystem CreateFileSystemWithRequiredArtifacts(bool includeInventory = true)
    {
        var fileSystem = new FakeDoctorFileSystem();
        if (includeInventory)
        {
            fileSystem.AddFile(Path.Combine(Root, "docs/AI_AGENT_CONTRACT_INVENTORY.md"), "inventory");
        }

        fileSystem.AddFile(Path.Combine(Root, "docs/AI_RAG_FOUNDATION.md"), "rag");
        fileSystem.AddFile(Path.Combine(Root, "docs/AI_AGENT_EXPERIENCE_HARDENING.md"), "agent");
        fileSystem.AddFile(Path.Combine(Root, "Event.Application.UnitTests/Features/AiAssistant/Context/AiSafeDataContextSummaryPolicyTests.cs"), "tests");
        fileSystem.AddFile(Path.Combine(Root, "Event.Application.UnitTests/Features/AiAssistant/Plans/AiProposedPlanValidatorTests.cs"), "tests");
        fileSystem.AddFile(Path.Combine(Root, "Event.Application.UnitTests/Features/AiAssistant/Tools/CreateEventDraftAiToolDefinitionTests.cs"), "tests");
        fileSystem.AddFile(Path.Combine(Root, "Event.Application.UnitTests/Features/AiAssistant/Tools/AiToolPayloadGuardTests.cs"), "tests");
        fileSystem.AddFile(Path.Combine(Root, "Explore.Diagnostic.UnitTests/AiReplay/AiReplayReportGeneratorTests.cs"), "tests");
        return fileSystem;
    }
}
