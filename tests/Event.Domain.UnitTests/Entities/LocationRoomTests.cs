// ABOUTME: Tests the LocationRoom entity covering interface compliance, required properties, and default values.
// ABOUTME: LocationRoom is a data entity under Location for room-aware scheduling — no behavior methods to test.

namespace Event.Domain.UnitTests.Entities;

public class LocationRoomTests
{
    [Test]
    public async Task LocationRoom_ImplementsTenantEntityInterface()
    {
        await Assert.That(typeof(LocationRoom).GetInterfaces().Contains(typeof(ITenantEntity))).IsTrue();
    }

    [Test]
    public async Task LocationRoom_ImplementsAuditableEntityInterface()
    {
        await Assert.That(typeof(LocationRoom).GetInterfaces().Contains(typeof(IAuditableEntity))).IsTrue();
    }

    [Test]
    public async Task LocationRoom_ImplementsSoftDeletableInterface()
    {
        await Assert.That(typeof(LocationRoom).GetInterfaces().Contains(typeof(ISoftDeletable))).IsTrue();
    }

    [Test]
    public async Task LocationRoom_ImplementsConcurrencyAwareInterface()
    {
        await Assert.That(typeof(LocationRoom).GetInterfaces().Contains(typeof(IConcurrencyAware))).IsTrue();
    }

    [Test]
    public async Task RequiredProperties_AreMarkedAsRequired()
    {
        await Assert.That(IsRequiredProperty<LocationRoom>(nameof(LocationRoom.Location))).IsTrue();
        await Assert.That(IsRequiredProperty<LocationRoom>(nameof(LocationRoom.Name))).IsTrue();
        await Assert.That(IsRequiredProperty<LocationRoom>(nameof(LocationRoom.Tenant))).IsTrue();
    }

    [Test]
    public async Task BooleanDefaults_WhenCreated_AreExpected()
    {
        var entity = CreateLocationRoom();

        await Assert.That(entity.IsDeleted).IsFalse();
    }

    [Test]
    public async Task NullableProperties_WhenCreated_AreNull()
    {
        var entity = CreateLocationRoom();

        await Assert.That(entity.Slug).IsNull();
        await Assert.That(entity.Description).IsNull();
        await Assert.That(entity.Capacity).IsNull();
    }

    [Test]
    public async Task NumericDefaults_WhenCreated_AreZero()
    {
        var entity = CreateLocationRoom();

        await Assert.That(entity.SortOrder).IsEqualTo(0);
    }

    [Test]
    public async Task ForeignKeyIds_WhenCreated_AreDefaultGuid()
    {
        var entity = CreateLocationRoom();

        await Assert.That(entity.LocationId).IsEqualTo(Guid.Empty);
        await Assert.That(entity.TenantId).IsEqualTo(Guid.Empty);
    }

    [Test]
    public async Task Name_WhenSet_ReturnsExpectedValue()
    {
        var entity = CreateLocationRoom();

        await Assert.That(entity.Name).IsEqualTo("Main Hall");
    }

    private static bool IsRequiredProperty<T>(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName);
        return property is not null && property.GetCustomAttributes(inherit: false).Any(a => a.GetType().Name == "RequiredMemberAttribute");
    }

    private static LocationRoom CreateLocationRoom()
    {
        return new LocationRoom
        {
            Location = CreateLocation(),
            Name = "Main Hall",
            Tenant = CreateTenant()
        };
    }

    private static Location CreateLocation()
    {
        var location = new Location
        {
            FullName = "Conference Center",
            Country = "Belgium",
            City = "Brussels",
            Tenant = CreateTenant()
        };
        location.SetManualAddress("1 Place du Grand Sablon", "1000");
        return location;
    }

    private static Tenant CreateTenant()
    {
        return new Tenant
        {
            FullName = "Tenant",
            Slug = "tenant",
            TenantStatusId = 2,
            TenantStatus = new TenantStatus { Id = 2, MasterCode = "ACTIVE", FullName = "Active", IsActiveState = true }
        };
    }
}
