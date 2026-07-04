// ABOUTME: Source-level safety tests for Blazor browser interop security invariants.
// ABOUTME: Prevents reintroducing eval-based JavaScript execution in client UI source.

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
