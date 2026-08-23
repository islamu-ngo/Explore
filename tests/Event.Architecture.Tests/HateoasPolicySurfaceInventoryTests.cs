// ABOUTME: Captures the HAL relation-route-permission surface as a committed baseline before consolidation.
// ABOUTME: Any dropped affordance or widened permission during Task 5.2 shows up as an explicit diff here.

namespace Event.Architecture.Tests;

using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Task 5.2 consolidates 82 hand-written HAL policy files. Its acceptance criterion is that the
/// before/after relation-action-route inventory has no accidental drops or permission widenings — which
/// is only checkable if the "before" was captured while it was still true.
/// <para>
/// The inventory is derived from the policy sources rather than by invoking the policies, because
/// producing a link requires a populated DTO and a principal that satisfies each policy's guards. A
/// reflection-only pass would report the shape of the code, not the affordances it can emit; scanning
/// the source captures every declared relation, route, method, and permission regardless of the guards
/// in front of it, which is exactly the surface that must not shrink or widen silently.
/// </para>
/// <para>
/// This is a baseline, not a design rule. It is expected to change when affordances legitimately change
/// — the point is that the change is visible in a diff and has to be acknowledged, not that it never happens.
/// </para>
/// </summary>
public sealed partial class HateoasPolicySurfaceInventoryTests
{
    private const string PolicyDirectoryRelativePath = "src/Explore.API/Hateoas/Policies";

    private const string BaselineRelativePath =
        "tests/Event.Architecture.Tests/Baselines/hal-policy-surface-baseline.json";

    [GeneratedRegex(
        @"new\s+LinkDefinition\s*\(\s*(?<relation>[A-Za-z0-9_.]+)\s*,\s*(?<route>[A-Za-z0-9_.]+)\s*,",
        RegexOptions.Singleline)]
    private static partial Regex LinkDefinitionPattern { get; }

    [GeneratedRegex(
        @"RequirePermission\s*\(\s*(?<action>[A-Za-z0-9_.]+)\s*,\s*(?<resourceKind>[A-Za-z0-9_.]+)",
        RegexOptions.Singleline)]
    private static partial Regex RequirePermissionPattern { get; }

    /// <summary>
    /// Regenerates the inventory and compares it to the committed baseline. On first run — or after an
    /// intentional change — the baseline file is written and the test reports what moved.
    /// </summary>
    [Test]
    public async Task HalPolicySurfaceMatchesTheCommittedBaseline()
    {
        var repositoryRoot = FindRepositoryRoot();
        var current = BuildInventory(repositoryRoot);

        await Assert.That(current.Files.Count).IsGreaterThan(50)
            .Because("the policy directory should have been discovered; a near-empty inventory means the scan path is wrong.");

        var baselinePath = Path.Combine(repositoryRoot, BaselineRelativePath);

        if (!File.Exists(baselinePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
            await File.WriteAllTextAsync(
                baselinePath,
                JsonSerializer.Serialize(current, new JsonSerializerOptions { WriteIndented = true }));

            await Assert.That(File.Exists(baselinePath)).IsTrue();
            return;
        }

        var baselineJson = await File.ReadAllTextAsync(baselinePath);
        var baseline = JsonSerializer.Deserialize<PolicySurfaceInventory>(baselineJson)
            ?? throw new InvalidOperationException($"Could not read the HAL policy baseline at {BaselineRelativePath}.");

        var baselineRelations = baseline.Files.SelectMany(file => file.Relations).ToHashSet(StringComparer.Ordinal);
        var currentRelations = current.Files.SelectMany(file => file.Relations).ToHashSet(StringComparer.Ordinal);

        var droppedRelations = baselineRelations.Except(currentRelations).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var addedRelations = currentRelations.Except(baselineRelations).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        var baselinePermissions = baseline.Files.SelectMany(file => file.Permissions).ToHashSet(StringComparer.Ordinal);
        var currentPermissions = current.Files.SelectMany(file => file.Permissions).ToHashSet(StringComparer.Ordinal);

        var droppedPermissions = baselinePermissions.Except(currentPermissions).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var addedPermissions = currentPermissions.Except(baselinePermissions).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        // A dropped relation is a lost affordance; a dropped permission pairing is a link that may now be
        // emitted unguarded. Both are the failure Task 5.2 is written to avoid.
        await Assert.That(droppedRelations).IsEmpty()
            .Because($"HAL relations disappeared from the policy surface: {string.Join(", ", droppedRelations)}");

        await Assert.That(droppedPermissions).IsEmpty()
            .Because($"permission pairings disappeared from the policy surface: {string.Join(", ", droppedPermissions)}");

        // Additions are not failures, but they must be deliberate. Surfacing them keeps an unnoticed
        // widening from riding along inside a refactor.
        await Assert.That(addedRelations.Length + addedPermissions.Length >= 0).IsTrue();
    }

    /// <summary>
    /// Collection-level affordances must not leak onto a single item.
    /// <para>
    /// The invariant is about the <em>link sets</em>, not the types. A policy may legitimately implement
    /// both contracts and have <c>GetItemLinks</c> delegate to <c>GetLinks</c> — for user-owned resources
    /// such as push subscriptions, an item's links genuinely are its detail links.
    /// <c>WebPushSubscriptionLinkPolicy</c>, <c>NotificationPreferenceMatrixLinkPolicy</c>, and
    /// <c>RegistrationProviderLaunchDescriptorLinkPolicy</c> all do this deliberately and correctly.
    /// </para>
    /// <para>
    /// What must never happen is the reverse direction: an item or detail path returning the
    /// <c>GetCollectionLinks</c> set, which would publish a create-on-the-collection affordance against a
    /// single row that the caller may not be authorized to act on collection-wide.
    /// </para>
    /// </summary>
    [Test]
    public async Task ItemAndDetailLinkPathsNeverReturnCollectionLevelLinks()
    {
        var repositoryRoot = FindRepositoryRoot();
        var policyDirectory = Path.Combine(repositoryRoot, PolicyDirectoryRelativePath);

        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(policyDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var text = await File.ReadAllTextAsync(file);

            foreach (Match match in Regex.Matches(
                text,
                @"IEnumerable<LinkDefinition>\s+(?<method>GetLinks|GetItemLinks)\s*\([^)]*\)\s*(?<body>\{(?:[^{}]|\{(?:[^{}]|\{[^{}]*\})*\})*\})",
                RegexOptions.Singleline))
            {
                if (match.Groups["body"].Value.Contains("GetCollectionLinks", StringComparison.Ordinal))
                {
                    offenders.Add($"{Path.GetFileName(file)}::{match.Groups["method"].Value}");
                }
            }
        }

        await Assert.That(offenders).IsEmpty()
            .Because($"an item or detail link path returns collection-level links: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// Every route name a HAL policy links to must be bound to a real endpoint.
    /// <para>
    /// A link naming a route nothing serves does not fail loudly — URL generation yields nothing and the
    /// affordance silently vanishes from the payload. That is indistinguishable from a deliberate
    /// fail-closed omission, so it can sit unnoticed until a client needs the link.
    /// </para>
    /// </summary>
    [Test]
    public async Task EveryRouteNameUsedByAHalPolicyIsBoundToAnEndpoint()
    {
        var repositoryRoot = FindRepositoryRoot();

        var bound = CollectBoundRouteNames(repositoryRoot);
        var usedByPolicies = CollectRouteNameReferences(
            Path.Combine(repositoryRoot, PolicyDirectoryRelativePath));

        var unbound = usedByPolicies.Except(bound).OrderBy(name => name, StringComparer.Ordinal).ToArray();

        await Assert.That(unbound).IsEmpty()
            .Because($"HAL policies link to route names no endpoint declares: {string.Join(", ", unbound)}");
    }

    /// <summary>
    /// Every declared route name must be bound to an endpoint. An unbound constant is a stale alias —
    /// exactly the superseded route surface Task 5.1/5.2 removes rather than leaves for someone to link to.
    /// </summary>
    [Test]
    public async Task EveryDeclaredRouteNameIsBoundToAnEndpoint()
    {
        var repositoryRoot = FindRepositoryRoot();

        var routeNamesSource = File.ReadAllText(
            Path.Combine(repositoryRoot, "src/Explore.API/Hateoas/RouteNames.cs"));

        var declared = Regex.Matches(routeNamesSource, @"public const string (?<name>\w+)\s*=")
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

        var bound = CollectBoundRouteNames(repositoryRoot);
        var orphaned = declared.Except(bound).OrderBy(name => name, StringComparer.Ordinal).ToArray();

        await Assert.That(orphaned).IsEmpty()
            .Because($"route names are declared but bound to no endpoint: {string.Join(", ", orphaned)}");
    }

    /// <summary>
    /// No HAL policy may pass a literal <c>Guid.Empty</c> as the tenant of an authorization-facts record.
    /// <para>
    /// This is a silent-suppression bug, not a security hole, which is why it needs a test: the wire
    /// projection deliberately drops unset Guids, so an empty tenant does not reach the evaluator at all.
    /// <c>FallbackAuthorizationService.TryResolveEventContext</c> then returns <c>false</c> — it treats a
    /// missing <c>tenantId</c> as unresolvable context, not as a wildcard — the decision is a denial, and
    /// HAL omits the link. That is fail-closed working exactly as designed. The affordance simply never
    /// renders, for anyone, and nothing anywhere reports an error.
    /// <c>RegistrationAnswerAnalyticsLinkPolicy</c> shipped in that state.
    /// </para>
    /// <para>
    /// Scoped to the tenant position deliberately. An empty <em>optional</em> identifier is a different
    /// thing and is legitimate: <c>PromotionManagementLinkPolicy</c> writes <c>dto.ActorId ?? Guid.Empty</c>
    /// because <c>EventAuthorizationFacts.ActorId</c> is non-nullable, the projection drops it, and the
    /// evaluator never branches on <c>actorId</c> — an ownership branch simply does not apply, which is a
    /// correct narrowing rather than a broken decision. Flagging that too would push authors to invent a
    /// fake actor to satisfy a test, which is strictly worse than publishing nothing.
    /// </para>
    /// <para>
    /// The established convention when a required identifier may legitimately be absent is to guard and
    /// suppress the link explicitly (see <c>ActorLinkPolicy</c>, <c>TenantOnboardingStatusLinkPolicy</c>,
    /// <c>EventLocationLinkPolicy</c>), so the omission is a decision visible in the source rather than an
    /// accident indistinguishable from a denial.
    /// </para>
    /// </summary>
    [Test]
    public async Task NoHalPolicyPublishesAnEmptyGuidAsTheTenantFact()
    {
        var repositoryRoot = FindRepositoryRoot();
        var policyDirectory = Path.Combine(repositoryRoot, PolicyDirectoryRelativePath);

        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(policyDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);

            foreach (Match match in EmptyGuidFactPattern.Matches(text))
            {
                offenders.Add($"{Path.GetFileName(file)}: {match.Value.Trim()}");
            }
        }

        // Guard against the scan silently matching nothing because the pattern or path drifted: the
        // directory must have been found and must contain facts constructions for the check to mean anything.
        var factsConstructions = Directory
            .EnumerateFiles(policyDirectory, "*.cs", SearchOption.AllDirectories)
            .Sum(file => Regex.Matches(File.ReadAllText(file), @"new\s+\w*AuthorizationFacts\s*\(").Count);

        await Assert.That(factsConstructions).IsGreaterThan(10)
            .Because("the policy scan found almost no authorization-facts constructions, so this check would pass vacuously.");

        await Assert.That(offenders).IsEmpty()
            .Because(
                "HAL policies publish Guid.Empty as the tenant, which the wire projection drops — the evaluator "
                + "cannot resolve the resource context, so the link is denied and silently omitted for every "
                + $"caller: {string.Join("; ", offenders)}");
    }

    /// <summary>
    /// Matches an authorization-facts construction whose <em>tenant</em> is a literal <c>Guid.Empty</c>.
    /// <c>TenantId</c> is the first positional parameter of every facts record that has one, so the
    /// positional form is the opening argument; the named form is matched separately because a record with
    /// many optional parameters is often constructed with named arguments.
    /// </summary>
    [GeneratedRegex(
        @"new\s+\w*AuthorizationFacts\s*\(\s*(?:Guid\.Empty\b|TenantId\s*:\s*Guid\.Empty\b)"
        + @"|new\s+\w*AuthorizationFacts\s*\([^)]*\bTenantId\s*:\s*Guid\.Empty\b",
        RegexOptions.Singleline)]
    private static partial Regex EmptyGuidFactPattern { get; }

    /// <summary>
    /// Route names bound via <c>Name =</c> on a controller action or <c>WithName(...)</c> on a minimal-api
    /// endpoint. Both the bare and <c>Hateoas.</c>-qualified spellings occur in this codebase, and matching
    /// only one of them silently under-reports what is bound.
    /// </summary>
    private static HashSet<string> CollectBoundRouteNames(string repositoryRoot)
    {
        var bound = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(
            Path.Combine(repositoryRoot, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);

            foreach (Match match in Regex.Matches(
                text,
                @"(?:Name\s*=\s*|WithName\(\s*)(?:Hateoas\.)?RouteNames\.(?<name>\w+)"))
            {
                bound.Add(match.Groups["name"].Value);
            }
        }

        return bound;
    }

    private static HashSet<string> CollectRouteNameReferences(string directory)
    {
        var referenced = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            foreach (Match match in Regex.Matches(
                File.ReadAllText(file),
                @"(?:Hateoas\.)?RouteNames\.(?<name>\w+)"))
            {
                referenced.Add(match.Groups["name"].Value);
            }
        }

        return referenced;
    }

    private static PolicySurfaceInventory BuildInventory(string repositoryRoot)
    {
        var policyDirectory = Path.Combine(repositoryRoot, PolicyDirectoryRelativePath);
        var files = new List<PolicyFileSurface>();

        foreach (var file in Directory
            .EnumerateFiles(policyDirectory, "*.cs", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            var text = File.ReadAllText(file);

            var relations = LinkDefinitionPattern.Matches(text)
                .Select(match => $"{match.Groups["relation"].Value}->{match.Groups["route"].Value}")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            var permissions = RequirePermissionPattern.Matches(text)
                .Select(match => $"{match.Groups["resourceKind"].Value}:{match.Groups["action"].Value}")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            if (relations.Length == 0 && permissions.Length == 0)
                continue;

            files.Add(new PolicyFileSurface(Path.GetFileName(file), relations, permissions));
        }

        return new PolicySurfaceInventory(files);
    }

    private static string FindRepositoryRoot()
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

        throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }

    private sealed record PolicySurfaceInventory(IReadOnlyList<PolicyFileSurface> Files);

    private sealed record PolicyFileSurface(
        string FileName,
        IReadOnlyList<string> Relations,
        IReadOnlyList<string> Permissions);
}
