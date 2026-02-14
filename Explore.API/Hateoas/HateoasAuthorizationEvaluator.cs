namespace Explore.API.Hateoas;

using System.Reflection;
using System.Security.Claims;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Hateoas;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public sealed class HateoasAuthorizationEvaluator : IHateoasAuthorizationEvaluator
{
    private readonly IAuthorizationProvider _authorizationProvider;
    private readonly ILogger<HateoasAuthorizationEvaluator> _logger;

    public HateoasAuthorizationEvaluator(
        IAuthorizationProvider authorizationProvider,
        ILogger<HateoasAuthorizationEvaluator> logger)
    {
        _authorizationProvider = authorizationProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<bool>> AreLinksAllowedAsync(IReadOnlyList<LinkDefinition> definitions, ClaimsPrincipal? user, HttpContext httpContext)
    {
        if (definitions.Count == 0)
            return [];

        var results = new bool[definitions.Count];
        var checks = new List<(int Index, AuthorizationCheck Check)>();

        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            if (!PassesStaticChecks(definition, user))
            {
                results[i] = false;
                continue;
            }

            var check = BuildCheck(definition);
            if (check is null)
            {
                results[i] = true;
                continue;
            }

            checks.Add((i, check));
        }

        if (checks.Count == 0)
            return results;

        try
        {
            var batch = checks.Select(x => x.Check).ToArray();
            var allowed = await _authorizationProvider.IsAllowedBatchAsync(batch);

            for (var i = 0; i < checks.Count; i++)
            {
                var index = checks[i].Index;
                results[index] = i < allowed.Count && allowed[i];
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HATEOAS batch authorization failed; denying permission-bound links.");

            foreach (var check in checks)
            {
                results[check.Index] = false;
            }

            return results;
        }
    }

    private static bool PassesStaticChecks(LinkDefinition definition, ClaimsPrincipal? user)
    {
        if (definition.Condition is not null && !definition.Condition())
            return false;

        if (definition.RequiresAuth && user?.Identity?.IsAuthenticated != true)
            return false;

        if (definition.RequiredRoles is { Length: > 0 })
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var hasRequiredRole = definition.RequiredRoles.Any(user.IsInRole);
            if (!hasRequiredRole)
                return false;
        }

        return true;
    }

    private static AuthorizationCheck? BuildCheck(LinkDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.PermissionResourceKind))
            return null;

        var action = definition.PermissionAction ?? MapMethodToAction(definition.Method);
        if (string.IsNullOrWhiteSpace(action))
            return null;

        var resourceId = definition.PermissionResourceId
            ?? ExtractResourceId(definition.RouteValues)
            ?? definition.RouteName;

        var attrs = definition.PermissionResourceAttributes;
        return new AuthorizationCheck(definition.PermissionResourceKind, resourceId, action, attrs);
    }

    private static string? MapMethodToAction(string? method)
    {
        return method?.ToUpperInvariant() switch
        {
            "GET" => "read",
            "POST" => "create",
            "PUT" => "update",
            "PATCH" => "update",
            "DELETE" => "delete",
            _ => null
        };
    }

    private static string? ExtractResourceId(object? routeValues)
    {
        if (routeValues is null)
            return null;

        if (routeValues is IReadOnlyDictionary<string, object> readOnlyDictionary)
            return TryGetId(readOnlyDictionary.ToDictionary(x => x.Key, x => (object?)x.Value));

        if (routeValues is IDictionary<string, object> dictionary)
            return TryGetId(dictionary.ToDictionary(x => x.Key, x => (object?)x.Value));

        var values = routeValues
            .GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(x => x.Name, x => x.GetValue(routeValues));

        return TryGetId(values);
    }

    private static string? TryGetId(IReadOnlyDictionary<string, object?> values)
    {
        return TryGet(values, "id")
            ?? TryGet(values, "tenantId")
            ?? TryGet(values, "organizationId")
            ?? TryGet(values, "did")
            ?? TryGet(values, "userId");
    }

    private static string? TryGet(IReadOnlyDictionary<string, object?> values, string key)
    {
        foreach (var pair in values)
        {
            if (!pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                continue;

            return pair.Value?.ToString();
        }

        return null;
    }
}
