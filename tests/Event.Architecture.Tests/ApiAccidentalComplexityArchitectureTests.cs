// ABOUTME: Regression gates for API accidental-complexity reduction architecture rules.
// ABOUTME: Prevents authorization branch creep and controller free-text response classification.

namespace Event.Architecture.Tests;

using System.Reflection;
using System.Text.RegularExpressions;
using Explore.Application.Behaviors;
using NetArchTest.Rules;

public sealed class ApiAccidentalComplexityArchitectureTests
{
    private static readonly Assembly ApplicationAssembly = typeof(AuthorizationBehavior<,>).Assembly;

    [Test]
    [DisplayName("AuthorizationBehavior must not depend on feature namespaces")]
    public async Task AuthorizationBehavior_MustNotDependOn_FeatureNamespaces()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveName("AuthorizationBehavior`2")
            .ShouldNot()
            .HaveDependencyOn("Explore.Application.Features")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue()
            .Because("request-specific authorization lookups belong in closed generic IAuthorizationContextEnricher<TRequest> implementations, not in the shared pipeline behavior.");
    }

    [Test]
    [DisplayName("Controllers must not classify command failures from response message text")]
    public async Task Controllers_MustNotClassify_CommandFailures_FromMessageText()
    {
        var sourceRoot = LocateRepositoryRoot();
        var controllersRoot = Path.Combine(sourceRoot, "src", "Explore.API", "Controllers");
        var forbiddenPatterns = new Dictionary<string, Regex>
        {
            ["Message.Contains"] = new(@"\bMessage\s*\??\.\s*Contains\s*\(", RegexOptions.CultureInvariant),
            ["Message.StartsWith"] = new(@"\bMessage\s*\??\.\s*StartsWith\s*\(", RegexOptions.CultureInvariant),
            ["Message.EndsWith"] = new(@"\bMessage\s*\??\.\s*EndsWith\s*\(", RegexOptions.CultureInvariant),
            ["Message.Equals"] = new(@"\bMessage\s*\??\.\s*Equals\s*\(", RegexOptions.CultureInvariant),
            ["Message equality"] = new(@"\.Message\s*==", RegexOptions.CultureInvariant),
            ["string.Equals(Message)"] = new(@"string\.Equals\s*\([^;]*\.Message", RegexOptions.CultureInvariant)
        };

        var violations = Directory
            .EnumerateFiles(controllersRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .SelectMany(file =>
            {
                var source = File.ReadAllText(file);
                return forbiddenPatterns
                    .SelectMany(pattern => pattern.Value.Matches(source)
                        .Select(match => $"{Path.GetRelativePath(sourceRoot, file)}:{GetLineNumber(source, match.Index)} contains {pattern.Key}"));
            })
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToList();

        await Assert.That(violations).IsEmpty()
            .Because("controllers must map command failures by typed FailureCode values instead of localized or free-text response messages.");
    }

    private static string LocateRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Explore.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing Explore.slnx.");
    }

    private static int GetLineNumber(string source, int index) =>
        source[..index].Count(character => character == '\n') + 1;
}
