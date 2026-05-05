// ABOUTME: Architecture tests enforcing authorization parity across ResourceKinds, descriptors, Cerbos policies, schemas, and fallback.
// ABOUTME: Catches drift when resource kinds, actions, descriptors, or policies are added/removed without updating all layers.

namespace Event.Architecture.Tests;

using System.Reflection;
using System.Text.RegularExpressions;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Infrastructure.Services;

/// <summary>
/// Ensures authorization artifacts stay aligned across all layers:
/// <see cref="ResourceKinds"/> constants, <see cref="ResourceDescriptors"/> catalog,
/// <see cref="ResourceDescriptorRegistry"/>, <see cref="FallbackAuthorizationService"/>,
/// Cerbos YAML policies, and Cerbos JSON schemas.
/// </summary>
public partial class AuthorizationParityTests
{
    private static readonly string CerbosPoliciesPath = FindCerbosPoliciesPath();
    private static readonly string CerbosSchemasPath = Path.Combine(FindCerbosPoliciesPath(), "_schemas");

    // ──────────────────────────────────────────
    // Data extraction helpers
    // ──────────────────────────────────────────

    /// <summary>
    /// All constant string values declared on <see cref="ResourceKinds"/>.
    /// This is the canonical source of truth for resource kind identifiers.
    /// </summary>
    private static IReadOnlySet<string> GetResourceKindConstants()
    {
        return typeof(ResourceKinds)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet();
    }

    /// <summary>
    /// All resource kind strings exposed by <see cref="ResourceDescriptors"/> public static fields.
    /// Extracted by reading the <c>Kind</c> property of each descriptor via reflection.
    /// </summary>
    private static IReadOnlySet<string> GetDescriptorKinds()
    {
        var kinds = new HashSet<string>();
        var descriptorFields = typeof(ResourceDescriptors)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType.IsGenericType &&
                        f.FieldType.GetGenericTypeDefinition() == typeof(ResourceDescriptor<>));

        foreach (var field in descriptorFields)
        {
            var descriptor = field.GetValue(null);
            if (descriptor is null) continue;

            var kindProp = descriptor.GetType().GetProperty("Kind");
            if (kindProp?.GetValue(descriptor) is string kind)
                kinds.Add(kind);
        }

        return kinds;
    }

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

    /// <summary>
    /// All resource kinds that have a JSON schema file in _schemas/.
    /// Schema files are named {resource_kind}.json.
    /// </summary>
    private static IReadOnlySet<string> GetCerbosSchemaResourceKinds()
    {
        if (!Directory.Exists(CerbosSchemasPath))
            return new HashSet<string>();

        return Directory.GetFiles(CerbosSchemasPath, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != "principal") // principal.json is shared, not a resource kind
            .ToHashSet()!;
    }

    // ──────────────────────────────────────────
    // Provider parity tests (original)
    // ──────────────────────────────────────────

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

#pragma warning disable CS0618 // PermissionAction is obsolete — bridge tests must still exercise it
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
#pragma warning restore CS0618

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

    // ──────────────────────────────────────────
    // ResourceKinds catalog parity tests (Phase 6)
    // ──────────────────────────────────────────

    [Test]
    [DisplayName("Every ResourceKinds constant has a Cerbos YAML policy file")]
    public async Task ResourceKindConstants_ShouldHave_CerbosPolicy()
    {
        var constants = GetResourceKindConstants();
        var cerbosPolicies = GetCerbosPolicyResourceKinds();

        var missing = constants.Except(cerbosPolicies).ToList();

        await Assert.That(missing)
            .IsEmpty()
            .Because($"These ResourceKinds constants have no Cerbos policy: [{string.Join(", ", missing)}]");
    }

    [Test]
    [DisplayName("Every ResourceKinds constant has a FallbackAuthorizationService case")]
    public async Task ResourceKindConstants_ShouldHave_FallbackCase()
    {
        var constants = GetResourceKindConstants();
        var fallbackHandled = GetFallbackHandledResourceKinds();

        var missing = constants.Except(fallbackHandled).ToList();

        await Assert.That(missing)
            .IsEmpty()
            .Because($"These ResourceKinds constants have no FallbackAuthorizationService case: [{string.Join(", ", missing)}]");
    }

    // ──────────────────────────────────────────
    // Descriptor catalog parity tests (Phase 6)
    // ──────────────────────────────────────────

    [Test]
    [DisplayName("Every ResourceDescriptors entry uses a valid ResourceKinds constant")]
    public async Task DescriptorKinds_ShouldBe_InResourceKinds()
    {
        var descriptorKinds = GetDescriptorKinds();
        var constants = GetResourceKindConstants();

        var unknown = descriptorKinds.Except(constants).ToList();

        await Assert.That(unknown)
            .IsEmpty()
            .Because($"These descriptor kinds are not in ResourceKinds: [{string.Join(", ", unknown)}]");
    }

    [Test]
    [DisplayName("ResourceDescriptorRegistry values are a subset of ResourceKinds constants")]
    public async Task RegistryValues_ShouldBe_InResourceKinds()
    {
        var registered = GetRegisteredResourceKinds();
        var constants = GetResourceKindConstants();

        var orphaned = registered.Except(constants).ToList();

        await Assert.That(orphaned)
            .IsEmpty()
            .Because($"These registry resource kinds have no ResourceKinds constant: [{string.Join(", ", orphaned)}]");
    }

    // ──────────────────────────────────────────
    // Schema coverage tests (Phase 6)
    // ──────────────────────────────────────────

    [Test]
    [DisplayName("Every Cerbos resource policy has a matching JSON schema file")]
    public async Task CerbosPolicies_ShouldHave_SchemaFile()
    {
        var cerbosPolicies = GetCerbosPolicyResourceKinds();
        var schemas = GetCerbosSchemaResourceKinds();

        var missing = cerbosPolicies.Except(schemas).ToList();

        await Assert.That(missing)
            .IsEmpty()
            .Because($"These Cerbos policies have no JSON schema in _schemas/: [{string.Join(", ", missing)}]");
    }

    [Test]
    [DisplayName("Principal JSON schema file exists")]
    public async Task PrincipalSchema_ShouldExist()
    {
        var principalSchemaPath = Path.Combine(CerbosSchemasPath, "principal.json");

        await Assert.That(File.Exists(principalSchemaPath))
            .IsTrue()
            .Because("cerbos/policies/_schemas/principal.json must exist for Cerbos schema validation.");
    }

    [Test]
    [DisplayName("Every Cerbos policy YAML references its schema")]
    public async Task CerbosPolicies_ShouldReference_Schema()
    {
        if (!Directory.Exists(CerbosPoliciesPath))
        {
            await Assert.That(true).IsTrue(); // skip if no policies dir
            return;
        }

        var missingRef = new List<string>();

        foreach (var file in Directory.GetFiles(CerbosPoliciesPath, "*.yaml"))
        {
            var content = File.ReadAllText(file);
            var resourceMatch = CerbosResourceKindRegex().Match(content);
            if (!resourceMatch.Success) continue; // derived_roles.yaml etc.

            if (!content.Contains("principalSchema:", StringComparison.OrdinalIgnoreCase) ||
                !content.Contains("resourceSchema:", StringComparison.OrdinalIgnoreCase))
            {
                missingRef.Add(Path.GetFileName(file));
            }
        }

        await Assert.That(missingRef)
            .IsEmpty()
            .Because($"These Cerbos policy files do not reference their schema: [{string.Join(", ", missingRef)}]");
    }

    [Test]
    [DisplayName("HATEOAS link policies use explicit AuthorizationActions permission metadata")]
    public async Task AllLinkPoliciesHaveExplicitPermissionActions()
    {
        var policyDirectory = FindHateoasPoliciesPath();
        var violations = new List<string>();

        foreach (var file in Directory.GetFiles(policyDirectory, "*LinkPolicy.cs"))
        {
            var source = File.ReadAllText(file);
            var fileName = Path.GetFileName(file);

            if (source.Contains(".WithPermission(", StringComparison.Ordinal))
                violations.Add($"{fileName}: use RequirePermission(...) instead of setting raw permission metadata");

            if (source.Contains("PermissionAction.", StringComparison.Ordinal))
                violations.Add($"{fileName}: use AuthorizationActions string constants instead of PermissionAction enum values");

            foreach (Match match in MissingAuthorizationActionRegex().Matches(source))
            {
                violations.Add($"{fileName}: RequirePermission call does not start with AuthorizationActions at index {match.Index}");
            }
        }

        await Assert.That(violations)
            .IsEmpty()
            .Because("HATEOAS links are authorization affordances; permission-bound links must name explicit actions and cannot fall back to HTTP method inference.");
    }

    // ──────────────────────────────────────────
    // Utility methods
    // ──────────────────────────────────────────

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

    private static string FindHateoasPoliciesPath()
    {
        var eventLinkPolicyPath = FindSourceFile("EventLinkPolicy.cs", "Explore.API");
        return Path.GetDirectoryName(eventLinkPolicyPath)
            ?? throw new DirectoryNotFoundException("Could not resolve Explore.API/Hateoas/Policies.");
    }

    [GeneratedRegex("""\"(\w+)\"\s*=>""")]
    private static partial Regex FallbackSwitchCaseRegex();

    [GeneratedRegex("""resource:\s*["']?(\w+)["']?""")]
    private static partial Regex CerbosResourceKindRegex();

    [GeneratedRegex(@"\.RequirePermission\s*\(\s*(?!AuthorizationActions\.)", RegexOptions.Singleline)]
    private static partial Regex MissingAuthorizationActionRegex();
}
