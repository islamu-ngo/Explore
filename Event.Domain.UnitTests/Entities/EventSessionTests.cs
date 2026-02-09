namespace Event.Domain.UnitTests.Entities;

using Explore.Domain;
using Explore.Domain.Interfaces;

public class EventSessionTests
{
    [Test]
    public async Task EventSession_ImplementsTenantEntityInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(EventSession).GetInterfaces().Contains(typeof(ITenantEntity))).IsTrue();
    }

    [Test]
    public async Task EventSession_ImplementsAuditableEntityInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(EventSession).GetInterfaces().Contains(typeof(IAuditableEntity))).IsTrue();
    }

    [Test]
    public async Task EventSession_ImplementsSoftDeletableInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(EventSession).GetInterfaces().Contains(typeof(ISoftDeletable))).IsTrue();
    }

    [Test]
    public async Task RequiredProperties_AreMarkedAsRequired_ExpectedBehavior()
    {
        await Assert.That(IsRequiredProperty<EventSession>(nameof(EventSession.Event))).IsTrue();
        await Assert.That(IsRequiredProperty<EventSession>(nameof(EventSession.Tenant))).IsTrue();
    }

    [Test]
    public async Task MaxAudienceAttendees_DefaultValue_IsExpected()
    {
        var entity = CreateEventSession();

        await Assert.That(entity.MaxAudienceAttendees).IsNull();
    }

    [Test]
    public async Task CurrentAudienceAttendees_DefaultValue_IsExpected()
    {
        var entity = CreateEventSession();

        await Assert.That(entity.CurrentAudienceAttendees).IsNull();
    }

    [Test]
    public async Task StartTime_DefaultValue_IsExpected()
    {
        var entity = CreateEventSession();

        await Assert.That(entity.StartTime).IsEqualTo(default(DateTimeOffset));
    }

    [Test]
    public async Task EndTime_DefaultValue_IsExpected()
    {
        var entity = CreateEventSession();

        await Assert.That(entity.EndTime).IsEqualTo(default(DateTimeOffset));
    }

    [Test]
    public async Task IsDeleted_DefaultValue_IsExpected()
    {
        var entity = CreateEventSession();

        await Assert.That(entity.IsDeleted).IsFalse();
    }

    [Test]
    public async Task OptionalNavigationAndSlugProperties_DefaultValue_IsExpected()
    {
        var entity = CreateEventSession();

        await Assert.That(entity.LocationId).IsNull();
        await Assert.That(entity.Location).IsNull();
        await Assert.That(entity.Title).IsNull();
        await Assert.That(entity.Slug).IsNull();
        await Assert.That(entity.RegistrationModeId).IsNull();
        await Assert.That(entity.RegistrationMode).IsNull();
        await Assert.That(entity.Description).IsNull();
    }

    private static bool IsRequiredProperty<T>(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName);
        return property is not null && property.GetCustomAttributes(inherit: false).Any(a => a.GetType().Name == "RequiredMemberAttribute");
    }

    private static EventSession CreateEventSession()
    {
        return new EventSession
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
                DisplayName = "Actor",
                ActorType = new ActorType { FullName = "User", MasterCode = "USER" },
                Tenant = CreateTenant()
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
            IsActive = true
        };
    }
}
