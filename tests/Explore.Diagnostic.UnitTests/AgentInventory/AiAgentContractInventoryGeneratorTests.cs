// ABOUTME: Unit tests for deterministic AI agent contract inventory generation.
// ABOUTME: Verifies manual-section preservation, redaction posture, and generated-doc drift.

using Explore.Diagnostic.AgentInventory;

namespace Explore.Diagnostic.UnitTests.AgentInventory;

public sealed class AiAgentContractInventoryGeneratorTests
{
    [Test]
    public async Task GenerateMarkdownIncludesRegistryToolMetadata()
    {
        var markdown = new AiAgentContractInventoryGenerator().GenerateMarkdown();

        await Assert.That(markdown).Contains("Create event draft");
        await Assert.That(markdown).Contains("HumanConfirmationRequired");
        await Assert.That(markdown).Contains("create-event");
        await Assert.That(markdown).Contains("execution authority");
    }

    [Test]
    public async Task GenerateMarkdownPreservesManualNotes()
    {
        const string existing = """
            # Existing
            <!-- BEGIN MANUAL NOTES -->
            Keep this reviewer note.
            <!-- END MANUAL NOTES -->
            """;

        var markdown = new AiAgentContractInventoryGenerator().GenerateMarkdown(existing);

        await Assert.That(markdown).Contains("Keep this reviewer note.");
    }

    [Test]
    public async Task GenerateMarkdownDoesNotExposeSensitiveContentClasses()
    {
        var markdown = new AiAgentContractInventoryGenerator().GenerateMarkdown();

        var lower = markdown.ToLowerInvariant();
        await Assert.That(lower).DoesNotContain("sk-");
        await Assert.That(lower).DoesNotContain("provider_response");
        await Assert.That(lower).DoesNotContain("tenantid");
        await Assert.That(lower).DoesNotContain("raw_tool_payload");
    }

    [Test]
    public async Task GeneratedDocumentationMatchesInventoryGenerator()
    {
        var root = LocateRepositoryRoot();
        var path = Path.Combine(root, "docs", "AI_AGENT_CONTRACT_INVENTORY.md");
        var existing = File.ReadAllText(path);

        var generated = new AiAgentContractInventoryGenerator().GenerateMarkdown(existing);

        await Assert.That(existing).IsEqualTo(generated);
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
