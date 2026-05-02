// ABOUTME: Architecture tests enforcing API contract stability invariants at compile time.
// ABOUTME: Every [Http*] action must have Name=, route names must be unique, and ApiExplorer-hidden endpoints are exempted.

namespace Event.Architecture.Tests;

using System.Linq;
using System.Reflection;
using Explore.API.Models;
using Explore.Application.Features.Events.Requests.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NetArchTest.Rules;

public class ApiContractArchitectureTests
{
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    private static readonly Type[] HttpVerbAttributes =
    {
        typeof(HttpGetAttribute),
        typeof(HttpPostAttribute),
        typeof(HttpPutAttribute),
        typeof(HttpPatchAttribute),
        typeof(HttpDeleteAttribute),
        typeof(HttpOptionsAttribute),
        typeof(HttpHeadAttribute)
    };

    [Test]
    [DisplayName("Every non-hidden [Http*] action must have Name= set")]
    public async Task EveryNonHiddenAction_MustHave_RouteName()
    {
        var controllerTypes = Types.InAssembly(ApiAssembly)
            .That()
            .Inherit(typeof(ControllerBase))
            .And()
            .AreNotAbstract()
            .GetTypes()
            .ToList();

        var violations = new List<string>();

        foreach (var controller in controllerTypes)
        {
            var controllerHidden = controller
                .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
                .Cast<ApiExplorerSettingsAttribute>()
                .Any(a => a.IgnoreApi);

            var actions = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(IsHttpAction)
                .ToList();

            foreach (var action in actions)
            {
                if (controllerHidden)
                {
                    continue;
                }

                var actionHidden = action
                    .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
                    .Cast<ApiExplorerSettingsAttribute>()
                    .Any(a => a.IgnoreApi);

                if (actionHidden)
                {
                    continue;
                }

                var httpAttr = action.GetCustomAttributes(true)
                    .FirstOrDefault(a => HttpVerbAttributes.Any(h => h.IsInstanceOfType(a)));

                if (httpAttr is not IRouteTemplateProvider { Name: not null and not "" })
                {
                    violations.Add($"{controller.Name}.{action.Name}");
                }
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("every non-hidden HTTP action must set Name= on its [Http*] attribute for stable operationIds; see docs/GOVERNANCE.md#api-contract-rules");
    }

    [Test]
    [DisplayName("Route names must be unique across all controllers")]
    public async Task RouteNames_MustBe_Unique()
    {
        var controllerTypes = Types.InAssembly(ApiAssembly)
            .That()
            .Inherit(typeof(ControllerBase))
            .And()
            .AreNotAbstract()
            .GetTypes()
            .ToList();

        var names = new Dictionary<string, string>();

        foreach (var controller in controllerTypes)
        {
            var actions = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(IsHttpAction)
                .ToList();

            foreach (var action in actions)
            {
                var httpAttr = action.GetCustomAttributes(true)
                    .FirstOrDefault(a => HttpVerbAttributes.Any(h => h.IsInstanceOfType(a)));

                if (httpAttr is IRouteTemplateProvider { Name: string name })
                {
                    if (names.TryGetValue(name, out var existing))
                    {
                        names[name] = $"{existing}; {controller.Name}.{action.Name}";
                    }
                    else
                    {
                        names[name] = $"{controller.Name}.{action.Name}";
                    }
                }
            }
        }

        var duplicates = names
            .Where(kvp => kvp.Value.Contains(';'))
            .Select(kvp => $"{kvp.Key}: used by {kvp.Value}")
            .ToList();

        await Assert.That(duplicates).IsEmpty()
            .Because("route names must be unique; duplicates cause operationId collisions in OpenAPI");
    }

    [Test]
    [DisplayName("Every controller action must have a unique operation identity (controller.action)")]
    public async Task EveryAction_MustHave_UniqueIdentity()
    {
        var controllerTypes = Types.InAssembly(ApiAssembly)
            .That()
            .Inherit(typeof(ControllerBase))
            .And()
            .AreNotAbstract()
            .GetTypes()
            .ToList();

        var identities = new HashSet<string>();
        var duplicates = new List<string>();

        foreach (var controller in controllerTypes)
        {
            var actions = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(IsHttpAction)
                .ToList();

            foreach (var action in actions)
            {
                var identity = $"{controller.Name}.{action.Name}";
                if (!identities.Add(identity))
                {
                    duplicates.Add(identity);
                }
            }
        }

        await Assert.That(duplicates).IsEmpty()
            .Because("every action must be uniquely identifiable by controller.action; overloaded actions break OpenAPI generation");
    }

    [Test]
    [DisplayName("Public event-list ownership filters must stay actor-backed and nullable")]
    public async Task EventListOwnershipFilters_MustBe_NullableActorBackedContractOnly()
    {
        var requiredFilterNames = new[] { "ActorId", "OrganizationId", "GroupId" };
        var forbiddenContractNames = new[] { "WorkspaceId", "OrganizerScopeId", "OrganizationScopeId", "OrganizationScope", "TenantWorkspace", "ScopeId" };

        var apiProperties = typeof(EventFilterRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var requestProperties = typeof(GetEventListRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var violations = new List<string>();

        foreach (var propertyName in requiredFilterNames)
        {
            if (apiProperties.SingleOrDefault(p => p.Name == propertyName)?.PropertyType != typeof(Guid?))
                violations.Add($"EventFilterRequest.{propertyName} must be Guid? for optional query binding");

            if (requestProperties.SingleOrDefault(p => p.Name == propertyName)?.PropertyType != typeof(Guid?))
                violations.Add($"GetEventListRequest.{propertyName} must be Guid? for optional application filtering");
        }

        foreach (var forbiddenName in forbiddenContractNames)
        {
            if (apiProperties.Any(p => p.Name == forbiddenName))
                violations.Add($"EventFilterRequest must not expose {forbiddenName}; use ActorId/OrganizationId/GroupId only");

            if (requestProperties.Any(p => p.Name == forbiddenName))
                violations.Add($"GetEventListRequest must not expose {forbiddenName}; use ActorId/OrganizationId/GroupId only");
        }

        await Assert.That(violations).IsEmpty()
            .Because("the public /events list ownership contract must remain precise and actor-backed without introducing workspace/scope concepts");
    }

    private static bool IsHttpAction(MethodInfo method)
    {
        if (method.IsSpecialName)
        {
            return false;
        }

        foreach (var attr in HttpVerbAttributes)
        {
            if (method.GetCustomAttributes(attr, inherit: true).Length > 0)
            {
                return true;
            }
        }

        return false;
    }
}
