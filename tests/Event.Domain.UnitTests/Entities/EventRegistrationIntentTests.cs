// ABOUTME: Tests the EventRegistrationIntent entity covering interface compliance, required properties, and default values.
// ABOUTME: This parent aggregate captures user registration intent — tests verify shape and nullable FK defaults.

namespace Event.Domain.UnitTests.Entities;

public class EventRegistrationIntentTests
{
    [Test]
    public async Task EventRegistrationIntent_ImplementsTenantEntityInterface()
    {
        await Assert.That(typeof(EventRegistrationIntent).GetInterfaces().Contains(typeof(ITenantEntity))).IsTrue();
    }

    [Test]
    public async Task EventRegistrationIntent_ImplementsAuditableEntityInterface()
    {
        await Assert.That(typeof(EventRegistrationIntent).GetInterfaces().Contains(typeof(IAuditableEntity))).IsTrue();
    }

    [Test]
    public async Task EventRegistrationIntent_ImplementsSoftDeletableInterface()
    {
        await Assert.That(typeof(EventRegistrationIntent).GetInterfaces().Contains(typeof(ISoftDeletable))).IsTrue();
    }

    [Test]
    public async Task EventRegistrationIntent_ImplementsConcurrencyAwareInterface()
    {
        await Assert.That(typeof(EventRegistrationIntent).GetInterfaces().Contains(typeof(IConcurrencyAware))).IsTrue();
    }

    [Test]
    public async Task RequiredProperties_AreMarkedAsRequired()
    {
        await Assert.That(IsRequiredProperty<EventRegistrationIntent>(nameof(EventRegistrationIntent.Event))).IsTrue();
        await Assert.That(IsRequiredProperty<EventRegistrationIntent>(nameof(EventRegistrationIntent.User))).IsTrue();
        await Assert.That(IsRequiredProperty<EventRegistrationIntent>(nameof(EventRegistrationIntent.RegistrationScope))).IsTrue();
        await Assert.That(IsRequiredProperty<EventRegistrationIntent>(nameof(EventRegistrationIntent.Tenant))).IsTrue();
    }

    [Test]
    public async Task BooleanDefaults_WhenCreated_AreExpected()
    {
        var entity = CreateEventRegistrationIntent();

        await Assert.That(entity.IsDeleted).IsFalse();
    }

    [Test]
    public async Task NullableNavigationProperties_WhenCreated_AreNull()
    {
        var entity = CreateEventRegistrationIntent();

        await Assert.That(entity.SelectedEventDayId).IsNull();
        await Assert.That(entity.SelectedEventDay).IsNull();
        await Assert.That(entity.RegistrationPolicySnapshotId).IsNull();
        await Assert.That(entity.RegistrationPolicySnapshot).IsNull();
        await Assert.That(entity.ApprovalStatusId).IsNull();
        await Assert.That(entity.ApprovalStatus).IsNull();
    }

    [Test]
    public async Task ForeignKeyIds_WhenCreated_AreDefaultValues()
    {
        var entity = CreateEventRegistrationIntent();

        await Assert.That(entity.EventId).IsEqualTo(Guid.Empty);
        await Assert.That(entity.UserId).IsEqualTo(Guid.Empty);
        await Assert.That(entity.TenantId).IsEqualTo(Guid.Empty);
        await Assert.That(entity.RegistrationScopeId).IsEqualTo(0);
    }

    [Test]
    public async Task RequiredNavigationProperties_WhenCreated_AreNotNull()
    {
        var entity = CreateEventRegistrationIntent();

        await Assert.That(entity.Event).IsNotNull();
        await Assert.That(entity.User).IsNotNull();
        await Assert.That(entity.RegistrationScope).IsNotNull();
        await Assert.That(entity.Tenant).IsNotNull();
    }

    private static bool IsRequiredProperty<T>(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName);
        return property is not null && property.GetCustomAttributes(inherit: false).Any(a => a.GetType().Name == "RequiredMemberAttribute");
    }

    private static EventRegistrationIntent CreateEventRegistrationIntent()
    {
        return new EventRegistrationIntent
        {
            Event = CreateEvent(),
            User = CreateUser(),
            RegistrationScope = CreateRegistrationScope(),
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
                ActorType = new ActorType { FullName = "User", MasterCode = "USER" },
                Tenant = CreateTenant()
            },
            Tenant = CreateTenant(),
            VisibilityType = new VisibilityType { MasterCode = "PUBLIC", FullName = "Public" },
            EventStatus = new EventStatus { MasterCode = "DRAFT", FullName = "Draft" },
            EventFormat = new EventFormat { MasterCode = "ONLINE", FullName = "Online" }
        };
    }

    private static User CreateUser()
    {
        return new User
        {
            Pii = new UserPii { Email = "test@example.com", FirstName = "Test", LastName = "User" }
        };
    }

    private static RegistrationScope CreateRegistrationScope()
    {
        return new RegistrationScope
        {
            Id = 1,
            MasterCode = "EVENT",
            FullName = "Whole Event"
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
