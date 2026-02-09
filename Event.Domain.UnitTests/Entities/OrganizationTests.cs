namespace Event.Domain.UnitTests.Entities;

using Explore.Domain;
using Explore.Domain.Interfaces;

public class OrganizationTests
{
    [Test]
    public async Task Organization_ImplementsTenantEntityInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(Organization).GetInterfaces().Contains(typeof(ITenantEntity))).IsTrue();
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
        await Assert.That(IsRequiredProperty<Organization>(nameof(Organization.FullName))).IsTrue();
        await Assert.That(IsRequiredProperty<Organization>(nameof(Organization.ApprovalStatus))).IsTrue();
        await Assert.That(IsRequiredProperty<Organization>(nameof(Organization.Tenant))).IsTrue();
    }

    [Test]
    public async Task Members_DefaultValue_IsExpected()
    {
        var entity = CreateOrganization();

        await Assert.That(entity.Members).IsNull();
    }

    [Test]
    public async Task Actor_DefaultValue_IsExpected()
    {
        var entity = CreateOrganization();

        await Assert.That(entity.ActorId).IsNull();
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
        await Assert.That(entity.MetadataJson).IsNull();
    }

    [Test]
    public async Task ForeignKeyIds_WhenCreated_AreDefaultValue()
    {
        var entity = CreateOrganization();

        await Assert.That(entity.ApprovalStatusId).IsEqualTo(0);
        await Assert.That(entity.TenantId).IsEqualTo(Guid.Empty);
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
            FullName = "Org",
            ApprovalStatus = new ApprovalStatus { MasterCode = "PENDING", FullName = "Pending" },
            Tenant = new Tenant { FullName = "Tenant", Slug = "tenant", IsActive = true }
        };
    }
}
