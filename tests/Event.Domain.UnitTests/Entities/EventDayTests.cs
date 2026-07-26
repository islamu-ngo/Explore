// ABOUTME: Tests the EventDay entity covering interface compliance, required properties, and default values.
// ABOUTME: EventDay is a first-class day aggregate with no behavior methods — tests focus on shape correctness.

namespace Event.Domain.UnitTests.Entities;

public class EventDayTests
{
    [Test]
    public async Task EventDay_ImplementsTenantEntityInterface()
    {
        await Assert.That(typeof(EventDay).GetInterfaces().Contains(typeof(ITenantEntity))).IsTrue();
    }

    [Test]
    public async Task EventDay_ImplementsAuditableEntityInterface()
    {
        await Assert.That(typeof(EventDay).GetInterfaces().Contains(typeof(IAuditableEntity))).IsTrue();
    }

    [Test]
    public async Task EventDay_ImplementsSoftDeletableInterface()
    {
        await Assert.That(typeof(EventDay).GetInterfaces().Contains(typeof(ISoftDeletable))).IsTrue();
    }

    [Test]
    public async Task EventDay_ImplementsConcurrencyAwareInterface()
    {
        await Assert.That(typeof(EventDay).GetInterfaces().Contains(typeof(IConcurrencyAware))).IsTrue();
    }

    [Test]
    public async Task RequiredProperties_AreMarkedAsRequired()
    {
        await Assert.That(IsRequiredProperty<EventDay>(nameof(EventDay.Event))).IsTrue();
        await Assert.That(IsRequiredProperty<EventDay>(nameof(EventDay.Tenant))).IsTrue();
    }

    [Test]
    public async Task BooleanDefaults_WhenCreated_AreExpected()
    {
        var entity = CreateEventDay();

        await Assert.That(entity.IsDeleted).IsFalse();
        await Assert.That(entity.IsPublished).IsFalse();
        await Assert.That(entity.AllowsDayScopeRegistration).IsFalse();
    }

    [Test]
    public async Task NullableProperties_WhenCreated_AreNull()
    {
        var entity = CreateEventDay();

        await Assert.That(entity.Label).IsNull();
        await Assert.That(entity.Description).IsNull();
        await Assert.That(entity.BannerText).IsNull();
        await Assert.That(entity.BannerImageId).IsNull();
        await Assert.That(entity.BannerImage).IsNull();
    }

    [Test]
    public async Task NumericDefaults_WhenCreated_AreZero()
    {
        var entity = CreateEventDay();

        await Assert.That(entity.SortOrder).IsEqualTo(0);
    }

    [Test]
    public async Task LocalDate_WhenCreated_IsDefaultDateOnly()
    {
        var entity = CreateEventDay();

        await Assert.That(entity.LocalDate).IsEqualTo(default(DateOnly));
    }

    [Test]
    public async Task ForeignKeyIds_WhenCreated_AreDefaultGuid()
    {
        var entity = CreateEventDay();

        await Assert.That(entity.EventId).IsEqualTo(Guid.Empty);
        await Assert.That(entity.TenantId).IsEqualTo(Guid.Empty);
    }

    private static bool IsRequiredProperty<T>(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName);
        return property is not null && property.GetCustomAttributes(inherit: false).Any(a => a.GetType().Name == "RequiredMemberAttribute");
    }

    private static EventDay CreateEventDay()
    {
        return new EventDay
        {
            Event = CreateEvent(),
            Tenant = CreateTenant()
        };
    }

    private static global::Explore.Domain.Event CreateEvent()
    {
        return new global::Explore.Domain.Event
        {
            Title = "Event",
            Actor = new Actor
            {
                Pii = new ActorPii { DisplayName = "Actor" },
                ActorType = new ActorType { FullName = "User", MasterCode = "USER" }
            },
            Tenant = CreateTenant(),
            VisibilityType = new VisibilityType { MasterCode = "PUBLIC", FullName = "Public" },
            EventStatus = new EventStatus { MasterCode = "DRAFT", FullName = "Draft" },
            EventFormat = new EventFormat { MasterCode = "ONLINE", FullName = "Online" }
        };
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
