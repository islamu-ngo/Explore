// ABOUTME: Architecture tests enforcing authorization provider parity across Cerbos and fallback.
// ABOUTME: Catches drift when new resource kinds are added without updating both providers.

namespace Event.Architecture.Tests;

using System.Reflection;
using System.Text.RegularExpressions;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Infrastructure.Services;

/// <summary>
/// Ensures every resource kind referenced in HATEOAS link policies or the ResourceDescriptorRegistry
/// has matching support in both the FallbackAuthorizationService and Cerbos policy YAML files.
/// </summary>
public partial class AuthorizationParityTests
{
    private static readonly string CerbosPoliciesPath = FindCerbosPoliciesPath();

    /// <summary>
    /// All resource kind strings registered in <see cref="ResourceDescriptorRegistry"/>.
    /// Extracted via reflection from the private ResourceKinds dictionary.
    /// </summary>
    private static IReadOnlySet<string> GetRegisteredResourceKinds()
    {
        var field = typeof(ResourceDescriptorRegistry)
            .GetField("ResourceKinds", BindingFlags.NonPublic | BindingFlags.Static);

        if (field?.GetValue(null) is not IReadOnlyDictionary<Type, string> registry)
            throw new InvalidOperationException("Could not read ResourceDescriptorRegistry.ResourceKinds via reflection.");

        return registry.Values.ToHashSet();
    }

    /// <summary>
    /// All resource kind strings handled in FallbackAuthorizationService.IsAllowedAsync switch.
    /// Extracted by reading the source file and parsing the switch cases.
    /// </summary>
    private static IReadOnlySet<string> GetFallbackHandledResourceKinds()
    {
        var sourceFile = FindSourceFile("FallbackAuthorizationService.cs", "Explore.Infrastructure");
        var source = File.ReadAllText(sourceFile);
        var matches = FallbackSwitchCaseRegex().Matches(source);
        return matches.Select(m => m.Groups[1].Value).ToHashSet();
    }

    /// <summary>
    /// All resource kind strings that have a Cerbos YAML policy file.
    /// Extracted from the cerbos/policies/ directory by reading the resource field.
    /// </summary>
    private static IReadOnlySet<string> GetCerbosPolicyResourceKinds()
    {
        if (!Directory.Exists(CerbosPoliciesPath))
            return new HashSet<string>();

        var kinds = new HashSet<string>();
        foreach (var file in Directory.GetFiles(CerbosPoliciesPath, "*.yaml"))
        {
            var content = File.ReadAllText(file);
            var match = CerbosResourceKindRegex().Match(content);
            if (match.Success)
                kinds.Add(match.Groups[1].Value.Trim('"'));
        }

        return kinds;
    }

    [Test]
    [DisplayName("Every registered resource kind has a case in FallbackAuthorizationService")]
    public async Task RegisteredResourceKinds_ShouldHave_FallbackCase()
    {
        var registered = GetRegisteredResourceKinds();
        var fallbackHandled = GetFallbackHandledResourceKinds();

        var missing = registered.Except(fallbackHandled).ToList();

        await Assert.That(missing)
            .IsEmpty()
            .Because($"These resource kinds are in ResourceDescriptorRegistry but missing from " +
                     $"FallbackAuthorizationService: [{string.Join(", ", missing)}]");
    }

    [Test]
    [DisplayName("Every registered resource kind has a Cerbos YAML policy file")]
    public async Task RegisteredResourceKinds_ShouldHave_CerbosPolicy()
    {
        var registered = GetRegisteredResourceKinds();
        var cerbosPolicies = GetCerbosPolicyResourceKinds();

        var missing = registered.Except(cerbosPolicies).ToList();

        await Assert.That(missing)
            .IsEmpty()
            .Because($"These resource kinds are in ResourceDescriptorRegistry but missing from " +
                     $"cerbos/policies/: [{string.Join(", ", missing)}]");
    }

    [Test]
    [DisplayName("Every PermissionAction enum value has a mapping in ToActionString")]
    public async Task AllPermissionActions_ShouldBe_MappedInToActionString()
    {
        var unmapped = new List<PermissionAction>();

        foreach (var action in Enum.GetValues<PermissionAction>())
        {
            try
            {
                ResourceDescriptorRegistry.ToActionString(action);
            }
            catch (ArgumentOutOfRangeException)
            {
                unmapped.Add(action);
            }
        }

        await Assert.That(unmapped)
            .IsEmpty()
            .Because($"These PermissionAction values are not mapped in ToActionString: " +
                     $"[{string.Join(", ", unmapped)}]");
    }

    [Test]
    [DisplayName("FallbackAuthorizationService handles all resource kinds that have Cerbos policies")]
    public async Task CerbosPolicies_ShouldHave_FallbackCase()
    {
        var cerbosPolicies = GetCerbosPolicyResourceKinds();
        var fallbackHandled = GetFallbackHandledResourceKinds();

        var missing = cerbosPolicies.Except(fallbackHandled).ToList();

        await Assert.That(missing)
            .IsEmpty()
            .Because($"These Cerbos policies have no matching FallbackAuthorizationService case: " +
                     $"[{string.Join(", ", missing)}]");
    }

    private static string FindCerbosPoliciesPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "cerbos", "policies");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "cerbos", "policies");
    }

    private static string FindSourceFile(string fileName, string projectHint)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidates = Directory.GetFiles(dir.FullName, fileName, SearchOption.AllDirectories);
            var match = candidates.FirstOrDefault(c => c.Contains(projectHint, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not find {fileName} in project {projectHint}");
    }

    [GeneratedRegex("""\"(\w+)\"\s*=>""")]
    private static partial Regex FallbackSwitchCaseRegex();

    [GeneratedRegex("""resource:\s*["']?(\w+)["']?""")]
    private static partial Regex CerbosResourceKindRegex();
}
