namespace Event.Domain.UnitTests.Entities;

using Explore.Domain;
using Explore.Domain.Interfaces;

public class ActorTests
{
    [Test]
    public async Task Actor_DoesNotImplementTenantEntityInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(Actor).GetInterfaces().Contains(typeof(ITenantEntity))).IsFalse();
    }

    [Test]
    public async Task Actor_ImplementsAuditableEntityInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(Actor).GetInterfaces().Contains(typeof(IAuditableEntity))).IsTrue();
    }

    [Test]
    public async Task Actor_ImplementsSoftDeletableInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(Actor).GetInterfaces().Contains(typeof(ISoftDeletable))).IsTrue();
    }

    [Test]
    public async Task RequiredProperties_AreMarkedAsRequired_ExpectedBehavior()
    {
        await Assert.That(IsRequiredProperty<Actor>(nameof(Actor.ActorType))).IsTrue();
        await Assert.That(IsRequiredProperty<ActorPii>(nameof(ActorPii.DisplayName))).IsTrue();
    }

    [Test]
    public async Task ConditionalIdentity_UserBasedActor_CanBeSet()
    {
        var actor = CreateActor();
        var userId = Guid.NewGuid();

        actor.UserId = userId;
        actor.OrganizationId = null;

        await Assert.That(actor.UserId).IsEqualTo(userId);
        await Assert.That(actor.OrganizationId).IsNull();
    }

    [Test]
    public async Task ConditionalIdentity_OrganizationBasedActor_CanBeSet()
    {
        var actor = CreateActor();
        var organizationId = Guid.NewGuid();

        actor.UserId = null;
        actor.OrganizationId = organizationId;

        await Assert.That(actor.UserId).IsNull();
        await Assert.That(actor.OrganizationId).IsEqualTo(organizationId);
    }

    [Test]
    public async Task FederationIdentities_DefaultValue_IsExpected()
    {
        var actor = CreateActor();

        await Assert.That(actor.AtprotoIdentities).IsNotNull();
        await Assert.That(actor.AtprotoIdentities).IsEmpty();
    }

    [Test]
    public async Task IsDeleted_DefaultValue_IsExpected()
    {
        var actor = CreateActor();

        await Assert.That(actor.IsDeleted).IsFalse();
    }

    [Test]
    public async Task ProfileAndDescriptionProperties_DefaultValue_IsExpected()
    {
        var actor = CreateActor();

        await Assert.That(actor.Description).IsNull();
        await Assert.That(actor.ProfilePictureCid).IsNull();
        await Assert.That(actor.ProfilePictureUri).IsNull();
    }

    private static bool IsRequiredProperty<T>(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName);
        return property is not null && property.GetCustomAttributes(inherit: false).Any(a => a.GetType().Name == "RequiredMemberAttribute");
    }

    private static Actor CreateActor()
    {
        return new Actor
        {
            Pii = new ActorPii { DisplayName = "Actor" },
            ActorType = new ActorType { FullName = "User", MasterCode = "USER" }
        };
    }
}
