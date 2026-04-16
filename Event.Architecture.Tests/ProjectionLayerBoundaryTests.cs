// ABOUTME: Architecture tests enforcing Layer 2/3 boundary separation for custom property projections.
// ABOUTME: Ensures projection filter types stay in Application layer and do not leak into Domain.

namespace Event.Architecture.Tests;

using System.Reflection;
using NetArchTest.Rules;

public class ProjectionLayerBoundaryTests
{
    private static readonly Assembly DomainAssembly = typeof(Explore.Domain.Event).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Explore.Application.ApplicationServicesRegistration).Assembly;

    [Test]
    public async Task Domain_ShouldNotReference_ProjectionFilterSpecifications()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Explore.Application.Specifications.Events")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task Domain_ShouldNotReference_ProjectionFilterDtos()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Explore.Application.DTOs.CustomPropertyProjection")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task Domain_ShouldNotReference_SessionProjectionFilterSpecifications()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Explore.Application.Specifications.EventSessions")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task ProjectionFilterType_ShouldExist_InApplicationLayer()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameStartingWith("EventCustomPropertyProjectionFilter")
            .Should()
            .ResideInNamespace("Explore.Application.Specifications.Events")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task SessionProjectionFilterType_ShouldExist_InApplicationLayer()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameStartingWith("EventSessionCustomPropertyProjectionFilter")
            .Should()
            .ResideInNamespace("Explore.Application.Specifications.EventSessions")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task CustomPropertyFilterCriterion_ShouldExist_InApplicationLayer()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameStartingWith("CustomPropertyFilter")
            .Should()
            .ResideInNamespace("Explore.Application.DTOs.CustomPropertyProjection")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }

    [Test]
    public async Task EventQuerySpecification_ShouldNotLeak_ToInfrastructure()
    {
        var infrastructureAssembly = typeof(Explore.Infrastructure.InfrastructureServicesRegistration).Assembly;

        var result = Types.InAssembly(infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("Explore.Application.Specifications.Events")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue();
    }
}
