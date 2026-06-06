// ABOUTME: Enforces endpoint route-name coverage against the Explore.API.Hateoas.RouteNames registry.
// ABOUTME: Keeps controller route names catalog-backed while legacy orphaned constants are pruned separately.

using System.Reflection;

using Event.Api.IntegrationTests.Fixtures;

using Explore.API.Hateoas;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Catches RouteNames drift at test time instead of at runtime. Every endpoint that carries a
/// <see cref="RouteNameMetadata"/> must have a matching <see cref="RouteNames"/> constant so controllers
/// cannot invent names outside the HATEOAS single source of truth. The reverse constant-to-endpoint
/// coverage test remains documented as Phase 2 cleanup until legacy orphaned constants are pruned.
/// </summary>
[ClassDataSource<ContractApiFixture>(Shared = SharedType.PerAssembly)]
public sealed class RouteNameCoverageTests(ContractApiFixture fixture)
{
    private readonly ContractApiFixture _fixture = fixture;

    [Skip("Category: API contract. Removal: enable after legacy orphaned RouteNames constants are pruned or reattached; tracked by dev/active/backend-api-health-refactor Phase 2 route-name cleanup.")]
    [Test]
    public async Task RouteNames_EveryConstantResolvesToExactlyOneEndpoint()
    {
        var constants = GetRouteNameConstants();
        var endpointsByName = GetEndpointsByRouteName();

        var missing = new List<string>();
        var ambiguous = new List<string>();

        foreach (var routeName in constants)
        {
            if (!endpointsByName.TryGetValue(routeName, out var endpoints))
            {
                missing.Add(routeName);
                continue;
            }

            if (endpoints.Count > 1)
            {
                ambiguous.Add($"{routeName} (matched {endpoints.Count} endpoints)");
            }
        }

        await Assert.That(missing).IsEmpty()
            .Because("Every RouteNames constant must resolve to a registered endpoint, otherwise LinkGenerator returns null at runtime and HATEOAS links silently disappear.");
        await Assert.That(ambiguous).IsEmpty()
            .Because("A RouteNames constant matching multiple endpoints is an ambiguity LinkGenerator cannot resolve deterministically.");
    }

    [Test]
    public async Task EndpointRouteNames_EveryNamedEndpointHasMatchingConstant()
    {
        var constantSet = GetRouteNameConstants().ToHashSet(StringComparer.Ordinal);
        var endpointsByName = GetEndpointsByRouteName();

        var orphaned = endpointsByName.Keys
            .Where(name => !constantSet.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        await Assert.That(orphaned).IsEmpty()
            .Because("Every route name registered on an endpoint must be exposed through RouteNames. Orphaned route names mean a controller is inventing names outside the single source of truth.");
    }

    [Test]
    public async Task RouteNames_HasAtLeastOneConstant()
    {
        // Sanity check — protects the other two tests from silently passing if reflection breaks.
        var constants = GetRouteNameConstants();
        await Assert.That(constants).IsNotEmpty().Because("RouteNames must define at least one constant.");
    }

    private static IReadOnlyList<string> GetRouteNameConstants()
    {
        return typeof(RouteNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();
    }

    private Dictionary<string, List<RouteEndpoint>> GetEndpointsByRouteName()
    {
        var dataSource = _fixture.Factory.Services.GetRequiredService<EndpointDataSource>();
        var grouped = new Dictionary<string, List<RouteEndpoint>>(StringComparer.Ordinal);

        foreach (var endpoint in dataSource.Endpoints.OfType<RouteEndpoint>())
        {
            var routeName = endpoint.Metadata.GetMetadata<RouteNameMetadata>()?.RouteName;
            if (string.IsNullOrEmpty(routeName))
            {
                continue;
            }

            if (!grouped.TryGetValue(routeName, out var list))
            {
                list = new List<RouteEndpoint>();
                grouped[routeName] = list;
            }
            list.Add(endpoint);
        }

        return grouped;
    }
}
