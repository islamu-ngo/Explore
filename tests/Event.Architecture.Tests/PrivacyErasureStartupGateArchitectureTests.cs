// ABOUTME: Guards API startup ordering for retained platform privacy-erasure replay.
// ABOUTME: Ensures the host is built, replayed, and only then started.

using TUnit.Core;

namespace Event.Architecture.Tests;

public sealed class PrivacyErasureStartupGateArchitectureTests
{
    [Test]
    public async Task ApiHost_BuildsBeforeStartingTheHost()
    {
        string program = ReadRepositoryFile("src/Explore.API/Program.cs");

        await Assert.That(program.IndexOf("var app = builder.Build();", StringComparison.Ordinal))
            .IsLessThan(program.IndexOf("app.Run();", StringComparison.Ordinal));
    }

    [Test]
    public async Task ApiHost_ReplaysRetainedErasuresBeforeStartingTheHost()
    {
        string program = ReadRepositoryFile("src/Explore.API/Program.cs");
        int build = program.IndexOf("var app = builder.Build();", StringComparison.Ordinal);
        int replay = program.IndexOf("await PrivacyErasureStartupGate.RunAsync", StringComparison.Ordinal);
        int run = program.IndexOf("app.Run();", StringComparison.Ordinal);

        await Assert.That(replay).IsGreaterThan(build);
        await Assert.That(replay).IsLessThan(run);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file '{relativePath}'.", relativePath);
    }
}
