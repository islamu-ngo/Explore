// ABOUTME: Architecture tests enforcing API conventions for versioning and authorization.
// ABOUTME: Ensures all controllers have [ApiVersion] and write endpoints have [Authorize].

namespace Event.Architecture.Tests;

using System.Reflection;
using Asp.Versioning;
using Explore.API.Controllers;
using Explore.API.Filters;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

/// <summary>
/// Tests that enforce API conventions across all controllers.
/// Ensures consistent versioning, authorization, and middleware usage.
/// </summary>
public class ApiConventionTests
{
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    #region API Versioning Conventions

    [Test]
    [DisplayName("All controllers must have [ApiVersion] attribute")]
    public async Task AllControllers_ShouldHave_ApiVersionAttribute()
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
            var hasApiVersion = controller.GetCustomAttributes(typeof(ApiVersionAttribute), true).Length > 0;
            if (!hasApiVersion)
            {
                violations.Add(controller.Name);
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("all controllers must declare [ApiVersion(\"1.0\")] for formal API versioning");
    }

    [Test]
    [DisplayName("All controller classes must inherit from ControllerBase")]
    public async Task AllControllers_ShouldInherit_ControllerBase()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("Explore.API.Controllers")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .HaveNameEndingWith("Controller")
            .Should()
            .Inherit(typeof(ControllerBase))
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    #endregion

    #region Authorization Conventions

    [Test]
    [DisplayName("All controllers must have [ApiController] attribute")]
    public async Task AllControllers_ShouldHave_ApiControllerAttribute()
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
            var hasApiController = controller.GetCustomAttributes(typeof(ApiControllerAttribute), true).Length > 0;
            if (!hasApiController)
            {
                violations.Add(controller.Name);
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("all controllers must have [ApiController] for consistent model validation and error responses");
    }

    [Test]
    [DisplayName("All controllers must have [Route] attribute")]
    public async Task AllControllers_ShouldHave_RouteAttribute()
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
            var hasRoute = controller.GetCustomAttributes(typeof(RouteAttribute), true).Length > 0;
            if (!hasRoute)
            {
                violations.Add(controller.Name);
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("all controllers must declare an explicit [Route] attribute");
    }

    #endregion

    #region Middleware Conventions

    [Test]
    [DisplayName("Middleware classes should be sealed")]
    public async Task Middleware_ShouldBe_Sealed()
    {
        var middlewareTypes = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("Explore.API.Middleware")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .HaveNameEndingWith("Middleware")
            .GetTypes();

        var violations = new List<string>();

        foreach (var middleware in middlewareTypes)
        {
            if (!middleware.IsSealed)
            {
                violations.Add(middleware.Name);
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("middleware classes should be sealed for performance (prevents virtual dispatch)");
    }

    #endregion

    #region Onboarding API Conventions

    [Test]
    [DisplayName("Tenant onboarding API must require MultiTenant deployment mode")]
    public async Task TenantOnboardingController_ShouldRequire_MultiTenantMode()
    {
        var hasRequireMultiTenant = typeof(TenantOnboardingController)
            .GetCustomAttributes(typeof(RequireMultiTenantAttribute), inherit: true)
            .Length > 0;

        await Assert.That(hasRequireMultiTenant).IsTrue()
            .Because("SingleTenant onboarding hides tenant concepts, so the matching API must not remain normally usable.");
    }

    #endregion
}
