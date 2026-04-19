// ABOUTME: Architecture tests enforcing that every public HTTP action carries an EndpointClassification.
// ABOUTME: Fails the build when a controller or action is not tagged Public/Authenticated/Admin.

namespace Event.Architecture.Tests;

using System.Linq;
using System.Reflection;
using Explore.API.Attributes;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

/// <summary>
/// Enforces that every controller action in Explore.API has an effective
/// <see cref="EndpointClassificationAttribute"/>. An effective attribute is one declared
/// on the action itself OR inherited from the controller type. Missing classifications
/// break OpenAPI vendor-extension emission and downstream client ergonomics.
/// See <c>docs/GOVERNANCE.md#api-contract-rules</c> for policy details.
/// </summary>
public class EndpointClassificationArchitectureTests
{
    private static readonly Assembly ApiAssembly = typeof(EndpointClassificationAttribute).Assembly;

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
    [DisplayName("Every controller must declare an [EndpointClassification] (controller-level or action-level on every action)")]
    public async Task EveryController_DeclaresEndpointClassification_EffectivelyOnEveryAction()
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
            var controllerHas = controller
                .GetCustomAttributes(typeof(EndpointClassificationAttribute), inherit: true)
                .Length > 0;

            var actions = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(IsHttpAction)
                .ToList();

            if (actions.Count == 0)
            {
                continue;
            }

            foreach (var action in actions)
            {
                var actionHas = action
                    .GetCustomAttributes(typeof(EndpointClassificationAttribute), inherit: true)
                    .Length > 0;

                if (!controllerHas && !actionHas)
                {
                    violations.Add($"{controller.Name}.{action.Name}");
                }
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("every HTTP action must be classified Public/Authenticated/Admin; see docs/GOVERNANCE.md#api-contract-rules");
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
