// ABOUTME: Unit tests for deterministic AI agent contract inventory generation.
// ABOUTME: Verifies manual-section preservation, redaction posture, and generated-doc drift.

using Explore.Diagnostic.AgentInventory;
using FluentAssertions;

namespace Explore.Diagnostic.UnitTests.AgentInventory;

public sealed class AiAgentContractInventoryGeneratorTests
{
    [Test]
    public void GenerateMarkdownIncludesRegistryToolMetadata()
    {
        var markdown = new AiAgentContractInventoryGenerator().GenerateMarkdown();

        markdown.Should().Contain("Create event draft");
        markdown.Should().Contain("HumanConfirmationRequired");
        markdown.Should().Contain("create-event");
        markdown.Should().Contain("execution authority");
    }

    [Test]
    public void GenerateMarkdownPreservesManualNotes()
    {
        const string existing = """
            # Existing
            <!-- BEGIN MANUAL NOTES -->
            Keep this reviewer note.
            <!-- END MANUAL NOTES -->
            """;

        var markdown = new AiAgentContractInventoryGenerator().GenerateMarkdown(existing);

        markdown.Should().Contain("Keep this reviewer note.");
    }

    [Test]
    public void GenerateMarkdownDoesNotExposeSensitiveContentClasses()
    {
        var markdown = new AiAgentContractInventoryGenerator().GenerateMarkdown();

        var lower = markdown.ToLowerInvariant();
        lower.Should().NotContain("sk-");
        lower.Should().NotContain("provider_response");
        lower.Should().NotContain("tenantid");
        lower.Should().NotContain("raw_tool_payload");
    }

    [Test]
    public void GeneratedDocumentationMatchesInventoryGenerator()
    {
        var root = LocateRepositoryRoot();
        var path = Path.Combine(root, "docs", "AI_AGENT_CONTRACT_INVENTORY.md");
        var existing = File.ReadAllText(path);

        var generated = new AiAgentContractInventoryGenerator().GenerateMarkdown(existing);

        existing.Should().Be(generated);
    }

    private static string LocateRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
