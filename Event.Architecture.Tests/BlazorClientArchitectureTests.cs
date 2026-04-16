// ABOUTME: Architecture tests for Blazor Client conventions — component injection rules and service placement.
// ABOUTME: Uses file-scanning since the architecture test project does not reference Explore.Blazor.Client.

namespace Event.Architecture.Tests;

/// <summary>
/// Ensures Blazor components follow injection rules and services live in correct namespaces.
/// </summary>
public class BlazorClientArchitectureTests
{
    private static readonly string? BlazorClientRoot = ResolveBlazorClientRoot();

    // Known pre-existing violations that predate this rule (tracked for cleanup).
    private static readonly HashSet<string> KnownIEventApiClientExceptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pages/Admin/Instance/Components/InstanceTenantsSection.razor"
    };

    [Test]
    public async Task Components_MustNotInject_IEventApiClient_Directly()
    {
        if (BlazorClientRoot is null)
        {
            await Assert.That(true).IsTrue()
                .Because("Blazor.Client source not found at test runtime — skipping");
            return;
        }

        var violations = new List<string>();

        var razorDirs = new[] { "Pages", "Shared" };
        foreach (var dir in razorDirs)
        {
            var searchPath = Path.Combine(BlazorClientRoot, dir);
            if (!Directory.Exists(searchPath)) continue;

            foreach (var file in Directory.EnumerateFiles(searchPath, "*.razor", SearchOption.AllDirectories))
            {
                var content = await File.ReadAllTextAsync(file);
                if (content.Contains("@inject", StringComparison.Ordinal)
                    && content.Contains("IEventApiClient", StringComparison.Ordinal))
                {
                    var relativePath = Path.GetRelativePath(BlazorClientRoot, file);
                    // Normalise path separators for cross-platform comparison
                    var normalised = relativePath.Replace('\\', '/');
                    if (!KnownIEventApiClientExceptions.Any(ex => normalised.EndsWith(ex.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase)))
                    {
                        violations.Add(relativePath);
                    }
                }
            }
        }

        await Assert.That(violations.Count).IsEqualTo(0)
            .Because($"Components must not inject IEventApiClient directly — use a service in Services/. Violations: {string.Join(", ", violations)}");
    }

    [Test]
    public async Task ITranslationService_Implementations_MustLiveInServicesNamespace()
    {
        if (BlazorClientRoot is null)
        {
            await Assert.That(true).IsTrue()
                .Because("Blazor.Client source not found at test runtime — skipping");
            return;
        }

        var servicesDir = Path.Combine(BlazorClientRoot, "Services");
        if (!Directory.Exists(servicesDir))
        {
            await Assert.That(false).IsTrue()
                .Because("Explore.Blazor.Client/Services/ directory not found");
            return;
        }

        var violations = new List<string>();

        // Search all .cs files outside Services/ for ITranslationService implementation
        foreach (var file in Directory.EnumerateFiles(BlazorClientRoot, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(BlazorClientRoot, file);
            // Skip files inside Services/
            if (relativePath.StartsWith("Services" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || relativePath.StartsWith("Services/", StringComparison.OrdinalIgnoreCase))
                continue;
            // Skip generated files
            if (relativePath.Contains("obj" + Path.DirectorySeparatorChar) || relativePath.Contains("/obj/"))
                continue;

            var content = await File.ReadAllTextAsync(file);
            // Look for class declarations implementing the interface, not DI registrations
            if (content.Contains(": ITranslationService", StringComparison.Ordinal)
                && content.Contains("class ", StringComparison.Ordinal))
            {
                violations.Add(relativePath);
            }
        }

        await Assert.That(violations.Count).IsEqualTo(0)
            .Because($"ITranslationService implementations must live in Services/. Violations: {string.Join(", ", violations)}");
    }

    private static string? ResolveBlazorClientRoot()
    {
        // Walk up from test bin to find the solution root
        var baseDir = AppContext.BaseDirectory;
        var candidate = baseDir;
        for (var i = 0; i < 8; i++)
        {
            candidate = Path.GetDirectoryName(candidate);
            if (candidate is null) break;

            var blazorClient = Path.Combine(candidate, "Explore.Blazor.Client");
            if (Directory.Exists(blazorClient))
                return blazorClient;
        }
        return null;
    }
}
