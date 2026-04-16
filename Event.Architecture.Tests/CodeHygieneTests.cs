// ABOUTME: Regression-prevention tests for code hygiene patterns established during the clean code refactor.
// ABOUTME: Guards: no controller-local GetCurrentUserId, identity-accessing controllers inherit ExploreControllerBase.

namespace Event.Architecture.Tests;

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

public class CodeHygieneTests
{
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    #region Controller Base Class Conventions

    [Test]
    [DisplayName("Controllers that access user identity must inherit from ExploreControllerBase")]
    public async Task ControllersAccessingIdentity_ShouldInherit_ExploreControllerBase()
    {
        var controllerTypes = Types.InAssembly(ApiAssembly)
            .That()
            .Inherit(typeof(ControllerBase))
            .And()
            .AreNotAbstract()
            .GetTypes();

        var exploreBaseType = ApiAssembly.GetTypes()
            .Single(t => t.Name == "ExploreControllerBase");

        var violations = new List<string>();

        foreach (var controller in controllerTypes)
        {
            var declaredMethods = controller.GetMethods(
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            var accessesIdentity = declaredMethods.Any(m =>
                m.Name is "GetCurrentUserId" or "GetUserId");

            if (accessesIdentity && !exploreBaseType.IsAssignableFrom(controller))
            {
                violations.Add(controller.Name);
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("controllers accessing user identity must inherit from ExploreControllerBase instead of defining local methods");
    }

    [Test]
    [DisplayName("No controller should define a local GetCurrentUserId method")]
    public async Task NoController_ShouldDefine_GetCurrentUserId()
    {
        var controllerTypes = Types.InAssembly(ApiAssembly)
            .That()
            .Inherit(typeof(ControllerBase))
            .And()
            .AreNotAbstract()
            .GetTypes();

        var violations = new List<string>();

        foreach (var controller in controllerTypes)
        {
            var methods = controller.GetMethods(
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            if (methods.Any(m => m.Name == "GetCurrentUserId"))
            {
                violations.Add(controller.Name);
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("controllers must use ExploreControllerBase.CurrentUserId instead of local GetCurrentUserId methods");
    }

    #endregion
}
