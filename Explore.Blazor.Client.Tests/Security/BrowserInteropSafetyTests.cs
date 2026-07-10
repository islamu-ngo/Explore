// ABOUTME: Source-level safety tests for Blazor browser interop and rendering security invariants.
// ABOUTME: Prevents reintroducing eval, unsafe DOM sinks, or unreviewed raw HTML rendering in client UI source.

using System.Text.RegularExpressions;

namespace Explore.Blazor.Client.Tests.Security;

public sealed class BrowserInteropSafetyTests
{
    private static readonly Regex EvalInteropPattern = new(
        @"Invoke(?:Void)?Async(?:<[^>]+>)?\s*\(\s*""eval""|\beval\s*\(",
        RegexOptions.Compiled);

    private static readonly Regex DomHtmlInjectionPattern = new(
        @"\b(?:innerHTML|outerHTML|insertAdjacentHTML|document\.write|setHTML)\b\s*(?:\(|=)",
        RegexOptions.Compiled);

    private static readonly Regex RawHtmlRenderingPattern = new(
        @"\b(?:MarkupString|AddMarkupContent)\b",
        RegexOptions.Compiled);

    private static readonly string[] RawHtmlRenderingAllowList =
    [
        Path.Combine("Explore.Blazor.Client", "Pages", "Legal", "CommunityGuidelines.razor")
    ];

    [Test]
    public async Task BlazorSource_DoesNotUseEvalBasedJavaScriptInterop()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceFiles = EnumerateBlazorSourceFiles(repositoryRoot);

        var offenders = sourceFiles
            .Where(path => EvalInteropPattern.IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .Order(StringComparer.Ordinal)
            .ToList();

        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    public async Task BlazorSource_DoesNotUseDomHtmlInjectionSinks()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceFiles = EnumerateBlazorSourceFiles(repositoryRoot);

        var offenders = sourceFiles
            .Where(path => DomHtmlInjectionPattern.IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .Order(StringComparer.Ordinal)
            .ToList();

        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    public async Task BlazorSource_DoesNotUseLegacyGlobalDownloadInterop()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceFiles = EnumerateBlazorSourceFiles(repositoryRoot);

        var offenders = sourceFiles
            .Where(path => File.ReadAllText(path).Contains("downloadFileFromBase64", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .Order(StringComparer.Ordinal)
            .ToList();

        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    public async Task BlazorSource_UsesRawHtmlRenderingOnlyInReviewedAllowlist()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceFiles = EnumerateBlazorSourceFiles(repositoryRoot);

        var offenders = sourceFiles
            .Where(path => RawHtmlRenderingPattern.IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .Where(relativePath => !IsRawHtmlRenderingAllowlisted(relativePath))
            .Order(StringComparer.Ordinal)
            .ToList();

        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    public async Task WebPushServiceWorker_ContainsRequiredFloodAndNavigationGuards()
    {
        var repositoryRoot = FindRepositoryRoot();
        var source = await File.ReadAllTextAsync(Path.Combine(
            repositoryRoot,
            "Explore.Blazor.Client",
            "wwwroot",
            "push-service-worker.js"));

        await Assert.That(source).Contains("clients.matchAll({ type: 'window', includeUncontrolled: true })");
        await Assert.That(source).Contains("client.visibilityState === 'visible'");
        await Assert.That(source).Contains("self.registration.getNotifications()");
        await Assert.That(source).Contains("renotify: false");
        await Assert.That(source).Contains("!value.startsWith('//')");
        await Assert.That(source).Contains("new URL(openPath, self.location.origin)");
    }

    private static IEnumerable<string> EnumerateBlazorSourceFiles(string repositoryRoot)
    {
        foreach (var projectName in new[] { "Explore.Blazor.Client", "Explore.Blazor" })
        {
            var projectRoot = Path.Combine(repositoryRoot, projectName);
            if (!Directory.Exists(projectRoot))
            {
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories))
            {
                if (IsGeneratedOrBuildOutput(path) || !IsSourceFile(path))
                {
                    continue;
                }

                yield return path;
            }
        }
    }

    private static bool IsSourceFile(string path)
        => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
           || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
           || path.EndsWith(".js", StringComparison.OrdinalIgnoreCase);

    private static bool IsGeneratedOrBuildOutput(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Contains("bin")
               || segments.Contains("obj")
               || segments.Contains("node_modules")
               || segments.Contains("_framework");
    }

    private static bool IsRawHtmlRenderingAllowlisted(string relativePath) =>
        RawHtmlRenderingAllowList.Any(allowlistedPath =>
            string.Equals(relativePath, allowlistedPath, StringComparison.OrdinalIgnoreCase));

    private static string FindRepositoryRoot()
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

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
