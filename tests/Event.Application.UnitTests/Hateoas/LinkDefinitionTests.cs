using Explore.Application.Hateoas;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Hateoas;

/// <summary>
/// Unit tests for LinkDefinition record.
/// </summary>
public class LinkDefinitionTests
{
    [Test]
    public async Task Self_ShouldCreateSelfLinkDefinition()
    {
        // Arrange
        var routeName = "GetResourceById";
        var routeValues = new { id = Guid.NewGuid() };

        // Act
        var definition = LinkDefinition.Self(routeName, routeValues);

        // Assert
        await Assert.That(definition.Rel).IsEqualTo(LinkRelations.Self);
        await Assert.That(definition.RouteName).IsEqualTo(routeName);
        await Assert.That(definition.RouteValues).IsNotNull();
        await Assert.That(definition.RequiresAuth).IsFalse();
    }

    [Test]
    public async Task Collection_ShouldCreateCollectionLinkDefinition()
    {
        // Arrange
        var routeName = "GetResources";

        // Act
        var definition = LinkDefinition.Collection(routeName);

        // Assert
        await Assert.That(definition.Rel).IsEqualTo(LinkRelations.Collection);
        await Assert.That(definition.RouteName).IsEqualTo(routeName);
        await Assert.That(definition.RequiresAuth).IsFalse();
    }

    [Test]
    public async Task Edit_ShouldCreateAuthenticatedEditLink()
    {
        // Arrange
        var routeName = "UpdateResource";
        var routeValues = new { id = Guid.NewGuid() };

        // Act
        var definition = LinkDefinition.Edit(routeName, routeValues);

        // Assert
        await Assert.That(definition.Rel).IsEqualTo(LinkRelations.Edit);
        await Assert.That(definition.RouteName).IsEqualTo(routeName);
        await Assert.That(definition.Method).IsEqualTo("PUT");
        await Assert.That(definition.RequiresAuth).IsTrue();
    }

    [Test]
    public async Task Delete_ShouldCreateAuthenticatedDeleteLink()
    {
        // Arrange
        var routeName = "DeleteResource";
        var routeValues = new { id = Guid.NewGuid() };

        // Act
        var definition = LinkDefinition.Delete(routeName, routeValues);

        // Assert
        await Assert.That(definition.Rel).IsEqualTo(LinkRelations.Delete);
        await Assert.That(definition.RouteName).IsEqualTo(routeName);
        await Assert.That(definition.Method).IsEqualTo("DELETE");
        await Assert.That(definition.RequiresAuth).IsTrue();
    }

    [Test]
    public async Task Create_ShouldCreateAuthenticatedCreateLink()
    {
        // Arrange
        var routeName = "CreateResource";

        // Act
        var definition = LinkDefinition.Create(routeName);

        // Assert
        await Assert.That(definition.Rel).IsEqualTo(LinkRelations.Create);
        await Assert.That(definition.RouteName).IsEqualTo(routeName);
        await Assert.That(definition.Method).IsEqualTo("POST");
        await Assert.That(definition.RequiresAuth).IsTrue();
    }

    [Test]
    public async Task Related_ShouldCreateRelatedResourceLink()
    {
        // Arrange
        var rel = "events";
        var routeName = "GetEventsByActor";
        var routeValues = new { actorId = Guid.NewGuid() };

        // Act
        var definition = LinkDefinition.Related(rel, routeName, routeValues);

        // Assert
        await Assert.That(definition.Rel).IsEqualTo(rel);
        await Assert.That(definition.RouteName).IsEqualTo(routeName);
        await Assert.That(definition.RequiresAuth).IsFalse();
    }

    [Test]
    public async Task Action_ShouldCreateActionLink()
    {
        // Arrange
        var rel = "publish";
        var routeName = "PublishResource";
        var method = "POST";

        // Act
        var definition = LinkDefinition.Action(rel, routeName, method, requiresAuth: true);

        // Assert
        await Assert.That(definition.Rel).IsEqualTo(rel);
        await Assert.That(definition.RouteName).IsEqualTo(routeName);
        await Assert.That(definition.Method).IsEqualTo(method);
        await Assert.That(definition.RequiresAuth).IsTrue();
    }

    [Test]
    public async Task When_ShouldAddCondition()
    {
        // Arrange
        var definition = LinkDefinition.Self("GetResource");
        var conditionMet = true;

        // Act
        var conditional = definition.When(() => conditionMet);

        // Assert
        await Assert.That(conditional.Condition is not null).IsTrue();
        await Assert.That(conditional.Condition!()).IsTrue();
    }

    [Test]
    public async Task Authenticated_ShouldSetRequiresAuth()
    {
        // Arrange
        var definition = LinkDefinition.Self("GetResource");

        // Act
        var authenticated = definition.Authenticated();

        // Assert
        await Assert.That(authenticated.RequiresAuth).IsTrue();
    }

    [Test]
    public async Task AdvertisedWhenAnonymous_ShouldKeepAuthenticationRequirement()
    {
        var definition = LinkDefinition.Action("report-event", "SubmitEventReport", "POST")
            .AdvertisedWhenAnonymous();

        await Assert.That(definition.RequiresAuth).IsTrue();
        await Assert.That(definition.AdvertiseWhenAnonymous).IsTrue();
    }

    [Test]
    public async Task WithRoles_ShouldSetRequiredRoles()
    {
        // Arrange
        var definition = LinkDefinition.Self("GetResource");
        var roles = new[] { "Admin", "Manager" };

        // Act
        var withRoles = definition.WithRoles(roles);

        // Assert
        await Assert.That(withRoles.RequiresAuth).IsTrue();
        await Assert.That(withRoles.RequiredRoles).IsNotNull();
        await Assert.That(withRoles.RequiredRoles).Contains("Admin");
        await Assert.That(withRoles.RequiredRoles).Contains("Manager");
    }

    [Test]
    public async Task Edit_WithRoles_ShouldIncludeRoles()
    {
        // Arrange
        var routeName = "UpdateResource";
        var roles = new[] { "Admin" };

        // Act
        var definition = LinkDefinition.Edit(routeName, roles: roles);

        // Assert
        await Assert.That(definition.RequiresAuth).IsTrue();
        await Assert.That(definition.RequiredRoles).IsNotNull();
        await Assert.That(definition.RequiredRoles).Contains("Admin");
    }

    [Test]
    public async Task RecordImmutability_ModifiersShouldReturnNewInstance()
    {
        // Arrange
        var original = LinkDefinition.Self("GetResource");

        // Act
        var authenticated = original.Authenticated();
        var withRoles = original.WithRoles("Admin");

        // Assert - original should be unchanged
        await Assert.That(original.RequiresAuth).IsFalse();
        await Assert.That(original.RequiredRoles).IsNull();

        // Modified versions should have changes
        await Assert.That(authenticated.RequiresAuth).IsTrue();
        await Assert.That(withRoles.RequiredRoles).IsNotNull();
    }
}
