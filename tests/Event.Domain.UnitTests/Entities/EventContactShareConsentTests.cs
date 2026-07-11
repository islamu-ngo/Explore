// ABOUTME: Unit tests for EventContactShareConsent entity — interface compliance and property defaults.
// ABOUTME: Follows the existing entity test pattern in Event.Domain.UnitTests.

using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Interfaces;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Domain.UnitTests.Entities;

public class EventContactShareConsentTests
{
    [Test]
    public async Task EventContactShareConsent_ImplementsTenantEntityInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(EventContactShareConsent).GetInterfaces().Contains(typeof(ITenantEntity))).IsTrue();
    }

    [Test]
    public async Task EventContactShareConsent_ImplementsAuditableEntityInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(EventContactShareConsent).GetInterfaces().Contains(typeof(IAuditableEntity))).IsTrue();
    }

    [Test]
    public async Task EventContactShareConsent_DoesNotImplementSoftDeletable()
    {
        // Consent records must not be soft-deleted — withdrawal is tracked via ConsentStatus
        await Assert.That(typeof(EventContactShareConsent).GetInterfaces().Contains(typeof(ISoftDeletable))).IsFalse();
    }

    [Test]
    public async Task RequiredProperties_AreMarkedAsRequired_ExpectedBehavior()
    {
        await Assert.That(IsRequiredProperty<EventContactShareConsent>(nameof(EventContactShareConsent.PurposeCode))).IsTrue();
        await Assert.That(IsRequiredProperty<EventContactShareConsent>(nameof(EventContactShareConsent.EmailSnapshot))).IsTrue();
        await Assert.That(IsRequiredProperty<EventContactShareConsent>(nameof(EventContactShareConsent.EmailNormalizedSnapshot))).IsTrue();
        await Assert.That(IsRequiredProperty<EventContactShareConsent>(nameof(EventContactShareConsent.ConsentTextSnapshot))).IsTrue();
        await Assert.That(IsRequiredProperty<EventContactShareConsent>(nameof(EventContactShareConsent.ConsentUiVersion))).IsTrue();
    }

    [Test]
    public async Task NavigationProperties_AreNullable_NotRequired()
    {
        // Nav properties use FK IDs for persistence; they're nullable for EF Core flexibility
        await Assert.That(IsRequiredProperty<EventContactShareConsent>(nameof(EventContactShareConsent.Tenant))).IsFalse();
        await Assert.That(IsRequiredProperty<EventContactShareConsent>(nameof(EventContactShareConsent.User))).IsFalse();
        await Assert.That(IsRequiredProperty<EventContactShareConsent>(nameof(EventContactShareConsent.RecipientActor))).IsFalse();
    }

    [Test]
    public async Task NullableNavigationProperties_AreNullByDefault()
    {
        var entity = CreateConsent();

        await Assert.That(entity.SourceEvent).IsNull();
        await Assert.That(entity.SourceEventRegistrationIntent).IsNull();
    }

    [Test]
    public async Task NullableForeignKeys_AreNullByDefault()
    {
        var entity = CreateConsent();

        await Assert.That(entity.SourceEventId).IsNull();
        await Assert.That(entity.SourceEventRegistrationIntentId).IsNull();
        await Assert.That(entity.WithdrawnAt).IsNull();
    }

    [Test]
    public async Task ConsentStatus_DefaultsToZero()
    {
        var entity = CreateConsent();

        // ConsentStatus enum: Granted=1, Withdrawn=2 — default 0 is "unset"
        await Assert.That((int)entity.Status).IsEqualTo(0);
    }

    [Test]
    public async Task ForeignKeyIds_WhenCreated_AreDefaultValue()
    {
        var entity = CreateConsent();

        await Assert.That(entity.TenantId).IsEqualTo(Guid.Empty);
        await Assert.That(entity.UserId).IsEqualTo(Guid.Empty);
        await Assert.That(entity.RecipientActorId).IsEqualTo(Guid.Empty);
    }

    private static bool IsRequiredProperty<T>(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName);
        return property is not null && property.GetCustomAttributes(inherit: false)
            .Any(a => a.GetType().Name == "RequiredMemberAttribute");
    }

    private static EventContactShareConsent CreateConsent()
    {
        return new EventContactShareConsent
        {
            PurposeCode = "TEST",
            EmailSnapshot = "test@example.com",
            EmailNormalizedSnapshot = "test@example.com",
            ConsentTextSnapshot = "Test consent",
            ConsentUiVersion = "v1"
        };
    }

    private static Tenant CreateTenant()
    {
        return new Tenant
        {
            FullName = "Test Tenant",
            Slug = "test-tenant",
            TenantStatus = new TenantStatus
            {
                Id = 2,
                MasterCode = "ACTIVE",
                FullName = "Active",
                IsActiveState = true
            }
        };
    }

    private static User CreateUser()
    {
        return new User
        {
            Pii = new UserPii
            {
                Email = "user@example.com",
                FirstName = "Test",
                LastName = "User"
            }
        };
    }

    private static Actor CreateActor()
    {
        return new Actor
        {
            Pii = new ActorPii { DisplayName = "Test Actor" },
            ActorType = new ActorType { FullName = "Organization", MasterCode = "ORGANIZATION" },
            Tenant = new Tenant
            {
                FullName = "Test Tenant",
                Slug = "test-tenant",
                TenantStatus = new TenantStatus
                {
                    Id = 2,
                    MasterCode = "ACTIVE",
                    FullName = "Active",
                    IsActiveState = true
                }
            }
        };
    }
}
