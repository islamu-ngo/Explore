namespace Event.Architecture.Tests;

using NetArchTest.Rules;
using System.Reflection;

/// <summary>
/// Tests that enforce naming conventions across the codebase.
/// Consistent naming improves code discoverability and maintainability.
/// </summary>
public class NamingConventionTests
{
    private static readonly Assembly ApplicationAssembly = typeof(Explore.Application.ApplicationServicesRegistration).Assembly;
    private static readonly Assembly PersistenceAssembly = typeof(Explore.Persistence.ExploreDbContext).Assembly;

    #region Handler Naming Conventions

    [Test]
    public async Task CommandHandlers_ShouldEndWith_CommandHandler()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceContaining("Handlers.Commands")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("CommandHandler")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task QueryHandlers_ShouldEndWith_Handler()
    {
        // The codebase uses both "RequestHandler" and "QueryHandler" naming
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceContaining("Handlers.Queries")
            .Or()
            .ResideInNamespaceContaining("Queries")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .HaveNameEndingWith("Handler")
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    #endregion

    #region Repository Naming Conventions

    [Test]
    public async Task Repositories_ShouldEndWith_Repository()
    {
        // Exclude generic repository base class
        var result = Types.InAssembly(PersistenceAssembly)
            .That()
            .ResideInNamespace("Explore.Persistence.Repositories")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .AreNotGeneric()
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    #endregion

    #region Validator Naming Conventions

    [Test]
    public async Task Validators_ShouldEndWith_Validator()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceContaining("Validators")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    #endregion

    #region DTO Naming Conventions

    [Test]
    public async Task DTOs_ShouldEndWith_Dto()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceContaining("DTOs")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotResideInNamespaceContaining("Validators")
            .Should()
            .HaveNameEndingWith("Dto")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    #endregion

    #region Interface Naming Conventions

    [Test]
    public async Task Interfaces_ShouldStartWith_I()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    #endregion
}
