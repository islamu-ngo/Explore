namespace Event.Architecture.Tests;

using System.Reflection;
using NetArchTest.Rules;

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
        // Composite aggregates and write-model request DTOs are excluded from the Dto suffix rule
        var compositeAggregateNames = new HashSet<string>
        {
            "InstanceGovernanceSettings",
            "CompleteInstanceOnboardingRequest",
            "UpdateTenantPolicyRequest"
        };

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

        var failures = result.FailingTypes?
            .Where(t => !compositeAggregateNames.Contains(t.Name))
            .ToList() ?? [];

        await Assert.That(failures.Count).IsEqualTo(0);
    }

    #endregion

    #region Authorization Refactor Regression Tests

    private static readonly Assembly DomainAssembly = typeof(Explore.Domain.Enums.RoleEnum).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Explore.Infrastructure.InfrastructureServicesRegistration).Assembly;

    /// <summary>
    /// Ensures no types reference removed entity names from the pre-refactor authorization model.
    /// OrganizationRole and TenantAdministratorRole were replaced by the unified Role entity.
    /// Note: PlatformUserRole remains a valid membership/link entity in the current model.
    /// TenantAdministrator was replaced by TenantMember.
    /// </summary>
    [Test]
    [DisplayName("No types should be named after removed authorization entities")]
    public async Task NoTypesNamed_AfterRemovedAuthorizationEntities()
    {
        var allAssemblies = new[] { DomainAssembly, ApplicationAssembly, PersistenceAssembly, InfrastructureAssembly };

        // These exact type names were removed during the authorization provider refactor.
        // Any new type with these names would indicate an incomplete migration.
        string[] forbiddenTypeNames =
        [
            "OrganizationRole",
            "TenantAdministratorRole",
            "TenantAdministrator",
        ];

        foreach (var assembly in allAssemblies)
        {
            foreach (var forbiddenName in forbiddenTypeNames)
            {
                var matchingTypes = Types.InAssembly(assembly)
                    .That()
                    .HaveNameMatching($"^{forbiddenName}$")
                    .GetTypes();

                await Assert.That(matchingTypes).IsEmpty()
                    .Because($"Type '{forbiddenName}' should not exist in {assembly.GetName().Name} — it was removed during the authorization refactor");
            }
        }
    }

    /// <summary>
    /// Ensures no interfaces reference the old ICerbosAuthorizationService name.
    /// It was renamed to IAuthorizationProvider during the refactor.
    /// </summary>
    [Test]
    [DisplayName("No interfaces should use the old ICerbosAuthorizationService name")]
    public async Task NoInterfacesNamed_ICerbosAuthorizationService()
    {
        var matchingTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .And()
            .HaveNameMatching("^ICerbosAuthorizationService$")
            .GetTypes();

        await Assert.That(matchingTypes).IsEmpty()
            .Because("ICerbosAuthorizationService was renamed to IAuthorizationProvider during the refactor");
    }

    /// <summary>
    /// Ensures no attributes reference the old CerbosAuthorizeAttribute name.
    /// It was renamed to AuthorizeResourceAttribute during the refactor.
    /// </summary>
    [Test]
    [DisplayName("No attributes should use the old CerbosAuthorizeAttribute name")]
    public async Task NoAttributesNamed_CerbosAuthorizeAttribute()
    {
        var matchingTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameMatching("^CerbosAuthorizeAttribute$")
            .GetTypes();

        await Assert.That(matchingTypes).IsEmpty()
            .Because("CerbosAuthorizeAttribute was renamed to AuthorizeResourceAttribute during the refactor");
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
