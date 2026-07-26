namespace Event.Domain.UnitTests.Entities;

using Explore.Domain;
using Explore.Domain.Interfaces;

public class OrganizationTests
{
    [Test]
    public async Task Organization_DoesNotImplementTenantEntityInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(Organization).GetInterfaces().Contains(typeof(ITenantEntity))).IsFalse();
    }

    [Test]
    public async Task Organization_ImplementsAuditableEntityInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(Organization).GetInterfaces().Contains(typeof(IAuditableEntity))).IsTrue();
    }

    [Test]
    public async Task Organization_ImplementsSoftDeletableInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(Organization).GetInterfaces().Contains(typeof(ISoftDeletable))).IsTrue();
    }

    [Test]
    public async Task RequiredProperties_AreMarkedAsRequired_ExpectedBehavior()
    {
        await Assert.That(IsRequiredProperty<OrganizationPii>(nameof(OrganizationPii.FullName))).IsTrue();
    }

    [Test]
    public async Task TenantParticipations_DefaultValue_IsExpected()
    {
        var entity = CreateOrganization();

        await Assert.That(entity.TenantParticipations).IsNotNull();
        await Assert.That(entity.TenantParticipations).IsEmpty();
    }

    [Test]
    public async Task Actor_DefaultValue_IsExpected()
    {
        var entity = CreateOrganization();

        await Assert.That(entity.Actor).IsNull();
    }

    [Test]
    public async Task IsDeleted_DefaultValue_IsExpected()
    {
        var entity = CreateOrganization();

        await Assert.That(entity.IsDeleted).IsFalse();
    }

    [Test]
    public async Task NullableContactAndAddressProperties_DefaultValue_IsExpected()
    {
        var entity = CreateOrganization();

        await Assert.That(entity.Email).IsNull();
        await Assert.That(entity.Country).IsNull();
        await Assert.That(entity.City).IsNull();
        await Assert.That(entity.Address).IsNull();
        await Assert.That(entity.Postcode).IsNull();
        await Assert.That(entity.WebsiteUrl).IsNull();
    }

    [Test]
    public async Task Identity_WhenCreated_IsDefaultValue()
    {
        var entity = CreateOrganization();

        await Assert.That(entity.Id).IsEqualTo(Guid.Empty);
    }

    private static bool IsRequiredProperty<T>(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName);
        return property is not null && property.GetCustomAttributes(inherit: false).Any(a => a.GetType().Name == "RequiredMemberAttribute");
    }

    private static Organization CreateOrganization()
    {
        return new Organization
        {
            Pii = new OrganizationPii { FullName = "Org" }
        };
    }
}
