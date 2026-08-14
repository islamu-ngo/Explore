// ABOUTME: Guards Stripe.net ownership and keeps the Stripe surface confined to Infrastructure.
// ABOUTME: Verifies the central pin and repository source usage stay inside the Infrastructure boundary.

using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Event.Architecture.Tests;

public sealed class StripeDependencyBoundaryTests
{
    private const string ExpectedVersion = "52.3.0";
    private const string PackageName = "Stripe.net";

    [Test]
    public async Task StripeNet_MustBePinnedCentrallyToExpectedStableVersion()
    {
        var document = XDocument.Load(ContextSystemHelpers.RepoPath("Directory.Packages.props"));
        var version = document.Descendants()
            .Where(element => element.Name.LocalName == "PackageVersion")
            .Where(element => string.Equals(element.Attribute("Include")?.Value, PackageName, StringComparison.Ordinal))
            .Select(element => element.Attribute("Version")?.Value)
            .SingleOrDefault();

        await Assert.That(version).IsEqualTo(ExpectedVersion);
    }

    [Test]
    public async Task StripeNet_MustBeReferencedOnlyByInfrastructureProject()
    {
        var projectFiles = Directory.EnumerateFiles(Path.Combine(ContextSystemHelpers.RepoRoot, "src"), "*.csproj", SearchOption.AllDirectories);
        var owners = new List<string>();

        foreach (var projectFile in projectFiles)
        {
            var projectName = Path.GetFileNameWithoutExtension(projectFile);
            var document = XDocument.Load(projectFile);
            if (document.Descendants()
                .Where(element => element.Name.LocalName == "PackageReference")
                .Any(element => string.Equals(element.Attribute("Include")?.Value, PackageName, StringComparison.Ordinal)))
            {
                owners.Add(projectName);
            }
        }

        await Assert.That(owners).IsEquivalentTo(["Explore.Infrastructure"]);
    }

    [Test]
    public async Task StripeTypes_MustStayInsideInfrastructureSourceFiles()
    {
        var sourceFiles = Directory.EnumerateFiles(Path.Combine(ContextSystemHelpers.RepoRoot, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Explore.Infrastructure{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.EndsWith(".g.cs", StringComparison.Ordinal))
            .ToArray();

        var violations = new List<string>();

        foreach (var file in sourceFiles)
        {
            var source = await File.ReadAllTextAsync(file);
            if (ContainsStripeReference(source))
            {
                violations.Add(Path.GetRelativePath(ContextSystemHelpers.RepoRoot, file).Replace('\\', '/'));
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("Stripe types and package usage must stay inside Explore.Infrastructure.");
    }

    private static bool ContainsStripeReference(string source)
    {
        var withoutBlockComments = Regex.Replace(
            source,
            @"/\*.*?\*/",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        var withoutComments = Regex.Replace(
            withoutBlockComments,
            @"//.*$",
            string.Empty,
            RegexOptions.Multiline | RegexOptions.CultureInvariant);

        return Regex.IsMatch(
            withoutComments,
            @"\bStripe\.(?:StripeClient|StripeClientOptions|StripeConfiguration|RequestOptions|RawRequestOptions|EventUtility|StripeResponse|StripeStreamedResponse|SystemNetHttpClient|Account(?:Service|CreateOptions|GetOptions|Link(?:Service|CreateOptions)?|Capabilities(?:Options)?|Requirements|Controller(?:Options|FeesOptions|LossesOptions|StripeDashboardOptions)|CapabilitiesCardPaymentsOptions|CapabilitiesTransfersOptions)?)\b|\bStripeClient(?:Options)?\b|using\s+Stripe;",
            RegexOptions.CultureInvariant);
    }
}
