namespace Event.Domain.UnitTests.Entities;

using Explore.Domain;
using Explore.Domain.Interfaces;

public class ActorTests
{
    [Test]
    public async Task Actor_ImplementsTenantEntityInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(Actor).GetInterfaces().Contains(typeof(ITenantEntity))).IsTrue();
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
        await Assert.That(IsRequiredProperty<Actor>(nameof(Actor.Tenant))).IsTrue();
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
    public async Task FederationProperties_DefaultValue_IsExpected()
    {
        var actor = CreateActor();

        await Assert.That(actor.Did).IsNull();
        await Assert.That(actor.Handle).IsNull();
        await Assert.That(actor.PdsHost).IsNull();
        await Assert.That(actor.DidCustodyTypeId).IsNull();
        await Assert.That(actor.DidCustodyType).IsNull();
        await Assert.That(actor.IndexedAt).IsNull();
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

        await Assert.That(actor.ProfilePictureId).IsNull();
        await Assert.That(actor.ProfilePicture).IsNull();
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
            DisplayName = "Actor",
            ActorType = new ActorType { FullName = "User", MasterCode = "USER" },
            Tenant = new Tenant
            {
                FullName = "Tenant",
                Slug = "tenant",
                TenantStatusId = 2,
                TenantStatus = new TenantStatus { Id = 2, MasterCode = "ACTIVE", FullName = "Active", IsActiveState = true }
            }
        };
    }
}
