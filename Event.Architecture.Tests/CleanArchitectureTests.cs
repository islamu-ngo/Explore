namespace Event.Architecture.Tests;

using System.Reflection;
using NetArchTest.Rules;

/// <summary>
/// Architecture tests that enforce Clean Architecture dependency rules.
/// These tests ensure the codebase maintains proper layering and dependency direction.
/// </summary>
public class CleanArchitectureTests
{
    // Assembly references for architecture testing
    private static readonly Assembly DomainAssembly = typeof(Explore.Domain.Event).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Explore.Application.ApplicationServicesRegistration).Assembly;
    private static readonly Assembly PersistenceAssembly = typeof(Explore.Persistence.ExploreDbContext).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Explore.Infrastructure.InfrastructureServicesRegistration).Assembly;

    // Namespace constants
    private const string DomainNamespace = "Explore.Domain";
    private const string ApplicationNamespace = "Explore.Application";
    private const string PersistenceNamespace = "Explore.Persistence";
    private const string InfrastructureNamespace = "Explore.Infrastructure";
    private const string ApiNamespace = "Explore.API";

    #region Domain Layer Tests

    [Test]
    public async Task Domain_ShouldNotHaveDependencyOn_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task Domain_ShouldNotHaveDependencyOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task Domain_ShouldNotHaveDependencyOn_Persistence()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(PersistenceNamespace)
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task Domain_ShouldNotHaveDependencyOn_Api()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task Domain_ShouldNotHaveDependencyOn_EntityFramework()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task Domain_ShouldNotHaveDependencyOn_AspNetCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    #endregion

    #region Application Layer Tests

    [Test]
    public async Task Application_ShouldNotHaveDependencyOn_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task Application_ShouldNotHaveDependencyOn_Persistence()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(PersistenceNamespace)
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task Application_ShouldNotHaveDependencyOn_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task Application_ShouldNotHaveDependencyOn_EntityFramework()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task Application_ShouldNotHaveDependencyOn_AspNetCore()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    #endregion

    #region Infrastructure Layer Tests

    [Test]
    public async Task Infrastructure_ShouldNotHaveDependencyOn_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task Persistence_ShouldNotHaveDependencyOn_Api()
    {
        var result = Types.InAssembly(PersistenceAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    #endregion
}
