// ABOUTME: Architecture guardrails for the descriptor-driven dock layout refactor.
// ABOUTME: Prevents central panel enums and page-level shell compensation from returning.

namespace Event.Architecture.Tests;

using System.Text.RegularExpressions;

public class DockLayoutArchitectureTests
{
    private static readonly string BlazorClientRoot = ResolveBlazorClientRoot();

    [Test]
    public async Task DockPanelIds_MustNotBeModeledAsCentralEnums()
    {
        var enumPattern = new Regex(@"\benum\s+(?<name>\w+)", RegexOptions.CultureInvariant);
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(BlazorClientRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsGenerated(file))
                continue;

            var content = await File.ReadAllTextAsync(file);
            foreach (Match match in enumPattern.Matches(content))
            {
                var enumName = match.Groups["name"].Value;
                if (!enumName.Contains("Panel", StringComparison.OrdinalIgnoreCase))
                    continue;

                var lineNumber = content[..match.Index].Count(c => c == '\n') + 1;
                violations.Add($"{GetRelativePath(file)}:{lineNumber} — enum {enumName}");
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("dock panels must use component-owned DockPanelId constants/descriptors, not a central panel enum. " +
                     $"Violations: {string.Join(", ", violations)}");
    }

    [Test]
    public async Task PageCss_MustNotCompensateForShellDockPanels()
    {
        var bannedShellCompensationPatterns = new[]
        {
            new Regex(@"--isl-dock-shell-", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
            new Regex(@"main-layout__main--ai-open", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
            new Regex(@"\bai-rail\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
            new Regex(@"\bshell-ai\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
            new Regex(@"width\s*:\s*calc\(\s*100%\s*\+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
        };

        var knownLegacyCompensation = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Pages/Events/EventList.razor.css"
        };

        var violations = new List<string>();
        var pagesRoot = Path.Combine(BlazorClientRoot, "Pages");
        foreach (var cssFile in Directory.EnumerateFiles(pagesRoot, "*.razor.css", SearchOption.AllDirectories))
        {
            var relative = GetRelativePath(cssFile);
            if (knownLegacyCompensation.Contains(relative))
                continue;

            var content = await File.ReadAllTextAsync(cssFile);
            foreach (var pattern in bannedShellCompensationPatterns)
            {
                foreach (Match match in pattern.Matches(content))
                {
                    var lineNumber = content[..match.Index].Count(c => c == '\n') + 1;
                    violations.Add($"{relative}:{lineNumber} — {match.Value.Trim()}");
                }
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("page-scoped CSS must not compensate for global shell dock panels. " +
                     "Use DockLayoutHost grid tracks and dock descriptors instead. " +
                     $"Violations: {string.Join(", ", violations)}");
    }

    [Test]
    public async Task DockCss_MustLayerWorkspaceInspectorAboveAppChrome()
    {
        var clientDockOverlayCss = await File.ReadAllTextAsync(Path.Combine(
            BlazorClientRoot,
            "Components/Docking/DockOverlayHost.razor.css"));
        var globalTokensCss = await File.ReadAllTextAsync(Path.Combine(
            ResolveRepoRoot(),
            "Explore.Blazor/wwwroot/css/tokens.css"));

        await Assert.That(clientDockOverlayCss).Contains("z-index: var(--isl-dock-z-inspector, 1400);")
            .Because("workspace inspectors must paint above page content, shell chrome, and the footer.");
        await Assert.That(clientDockOverlayCss).Contains(".dock-overlay-host--workspace .dock-overlay-host__backdrop")
            .Because("workspace inspector backdrops must dim the whole app chrome below the inspector panel.");
        await Assert.That(clientDockOverlayCss).Contains("z-index: var(--isl-dock-z-workspace-backdrop, 1100);")
            .Because("workspace backdrop must stay below the inspector panel inside the overlay stacking context.");
        await Assert.That(globalTokensCss).Contains("--isl-dock-z-shell: 1200;");
        await Assert.That(globalTokensCss).Contains("--isl-dock-z-workspace-backdrop: 1100;");
    }

    [Test]
    public async Task DockCss_MustKeepShellSidePanelsFixedBelowHeader()
    {
        var clientDockSideCss = await File.ReadAllTextAsync(Path.Combine(
            BlazorClientRoot,
            "Components/Docking/DockSideHost.razor.css"));
        var mainLayoutCss = await File.ReadAllTextAsync(Path.Combine(
            BlazorClientRoot,
            "Layout/MainLayout.razor.css"));
        var globalComponentsCss = await File.ReadAllTextAsync(Path.Combine(
            ResolveRepoRoot(),
            "Explore.Blazor/wwwroot/css/components.css"));

        await Assert.That(mainLayoutCss).Contains("position: fixed;")
            .Because("the shell header must remain visible while event-list content scrolls.");
        await Assert.That(mainLayoutCss).Contains("margin-block-start: var(--mud-appbar-height, 4rem);")
            .Because("fixed shell chrome must reserve its height before the dock host content starts.");
        await Assert.That(clientDockSideCss).Contains("position: fixed;")
            .Because("shell side panels must not drift while the page scrolls.");
        await Assert.That(globalComponentsCss).Contains("position: fixed !important;")
            .Because("the global cross-component shell dock rule must match the isolated component rule.");
        await Assert.That(globalComponentsCss).DoesNotContain("position: sticky !important;")
            .Because("sticky shell docks move during scroll boundary transitions.");
    }

    private static string ResolveBlazorClientRoot()
    {
        var repoRoot = ResolveRepoRoot();
        var clientRoot = Path.Combine(repoRoot, "Explore.Blazor.Client");

        if (!Directory.Exists(clientRoot))
            throw new DirectoryNotFoundException($"Explore.Blazor.Client not found at '{clientRoot}'. Test binary dir: '{AppContext.BaseDirectory}'");

        return clientRoot;
    }

    private static string ResolveRepoRoot()
    {
        var testDir = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
    }

    private static bool IsGenerated(string filePath) =>
        filePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
        filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        filePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

    private static string GetRelativePath(string fullPath) =>
        Path.GetRelativePath(BlazorClientRoot, fullPath).Replace('\\', '/');
}
