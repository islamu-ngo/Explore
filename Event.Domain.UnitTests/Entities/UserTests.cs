namespace Event.Domain.UnitTests.Entities;

using System.Reflection;
using Explore.Domain;
using Explore.Domain.Interfaces;

public class UserTests
{
    [Test]
    public async Task User_ImplementsAuditableEntityInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(User).GetInterfaces().Contains(typeof(IAuditableEntity))).IsTrue();
    }

    [Test]
    public async Task User_ImplementsSoftDeletableInterface_ExpectedBehavior()
    {
        await Assert.That(typeof(User).GetInterfaces().Contains(typeof(ISoftDeletable))).IsTrue();
    }

    [Test]
    public async Task User_WhenCreated_HasExpectedDefaults()
    {
        var user = CreateUser();

        await Assert.That(user.EmailVerified).IsNull();
        await Assert.That(user.IsDeleted).IsFalse();
        await Assert.That(user.ActorId).IsNull();
        await Assert.That(user.Actor).IsNull();
    }

    [Test]
    public async Task EmailVerified_WhenSet_CanRepresentVerificationState()
    {
        var user = CreateUser();

        user.EmailVerified = true;
        await Assert.That(user.EmailVerified).IsEqualTo(true);

        user.EmailVerified = false;
        await Assert.That(user.EmailVerified).IsEqualTo(false);

        user.EmailVerified = null;
        await Assert.That(user.EmailVerified).IsNull();
    }

    [Test]
    public async Task AuthProviderMapping_WhenSet_StoresProviderAndIdentifier()
    {
        var user = CreateUser();

        user.AuthProvider = "keycloak";
        user.AuthProviderId = "sub-123";

        await Assert.That(user.AuthProvider).IsEqualTo("keycloak");
        await Assert.That(user.AuthProviderId).IsEqualTo("sub-123");
    }

    [Test]
    public async Task CoreIdentityProperties_AreNonNullableReferenceTypes()
    {
        var context = new NullabilityInfoContext();

        await Assert.That(context.Create(typeof(User).GetProperty(nameof(User.Email))!).WriteState).IsEqualTo(NullabilityState.NotNull);
        await Assert.That(context.Create(typeof(User).GetProperty(nameof(User.FirstName))!).WriteState).IsEqualTo(NullabilityState.NotNull);
        await Assert.That(context.Create(typeof(User).GetProperty(nameof(User.LastName))!).WriteState).IsEqualTo(NullabilityState.NotNull);
    }

    private static User CreateUser()
    {
        return new User
        {
            Email = "user@example.com",
            FirstName = "Test",
            LastName = "User"
        };
    }
}
