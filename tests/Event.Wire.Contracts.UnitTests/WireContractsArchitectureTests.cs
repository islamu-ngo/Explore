// ABOUTME: Enforces the package-free and layer-neutral Event.Wire.Contracts boundary.
// ABOUTME: Pins the exact assembly identity and forbids project or package dependencies.

using System.Reflection;
using System.Xml.Linq;
using ISLAMU.Wire.Contracts.Admissions;

namespace ISLAMU.Wire.Contracts.UnitTests;

public sealed class WireContractsArchitectureTests
{
    [Test]
    public async Task AssemblyIdentityIsExactlyEventWireContracts()
    {
        Assembly assembly = typeof(AdmissionQrPayloadCodec).Assembly;

        await Assert.That(assembly.GetName().Name).IsEqualTo("Event.Wire.Contracts");
    }

    [Test]
    public async Task ProductProjectHasNoPackageOrProjectReferences()
    {
        string repositoryRoot = FindRepositoryRoot();
        string projectPath = Path.Combine(
            repositoryRoot,
            "src",
            "Event.Wire.Contracts",
            "Event.Wire.Contracts.csproj");
        XDocument project = XDocument.Load(projectPath);
        string[] forbiddenReferences = project.Descendants()
            .Where(element => element.Name.LocalName is "PackageReference" or "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value ?? element.Name.LocalName)
            .ToArray();

        await Assert.That(forbiddenReferences).IsEmpty();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Explore.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
