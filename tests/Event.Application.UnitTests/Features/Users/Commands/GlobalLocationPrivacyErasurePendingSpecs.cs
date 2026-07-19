// ABOUTME: Behavioral red specifications for global account deletion's future location-erasure orchestration.
// ABOUTME: Exercises the current command boundary so Todo 10 must fail closed and erase every owned Home.

using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Users.Handlers.Commands;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Users.Commands;

[Category("EventLocationPrivacyPending")]
[Skip("Category: EventLocationPrivacyPending. Removal: Todo 10 implements authority-first ELP-505/515 orchestration at the DeleteUser command boundary.")]
public sealed class GlobalLocationPrivacyErasurePendingSpecs
{
    [Test]
    public async Task AuthorityUnavailable_FailsClosedBeforeDeletingUser()
    {
        var userId = Guid.CreateVersion7();
        await using DeletionHarness harness = CreateHarness(userId);
        harness.Authority
            .AppendAsync(
                Arg.Any<LocationPrivacyErasureIntent>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<LocationPrivacyErasureAuthorityIntent>>(_ =>
                throw new InvalidOperationException("The retained erasure authority is unavailable."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Handler.Handle(new DeleteUserCommand { UserId = userId }, CancellationToken.None));
        await harness.UserRepository.DidNotReceive().Delete(Arg.Any<User>());
    }

    [Test]
    public async Task TwoTenantOwnedHomes_AreTombstonedBeforeUserDeletion()
    {
        var userId = Guid.CreateVersion7();
        var tenantAHome = CreatePrivateHome(Guid.CreateVersion7(), userId, "Owner home A");
        var tenantBHome = CreatePrivateHome(Guid.CreateVersion7(), userId, "Owner home B");
        var unrelatedHome = CreatePrivateHome(Guid.CreateVersion7(), Guid.CreateVersion7(), "Unrelated home");
        await using DeletionHarness harness = CreateHarness(userId);
        harness.LocationRepository
            .GetOwnedPrivateHomesForGlobalErasureAsync(userId, Arg.Any<CancellationToken>())
            .Returns([tenantAHome, tenantBHome]);
        harness.Authority
            .AppendAsync(
                Arg.Any<LocationPrivacyErasureIntent>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                LocationPrivacyErasureIntent intent = call.Arg<LocationPrivacyErasureIntent>();
                DateTime recordedAt = DateTime.UtcNow;
                return Task.FromResult(LocationPrivacyErasureAuthorityIntent.Record(
                    intent.IntentId,
                    1,
                    intent.OwnerUserId,
                    intent.LocationIds,
                    intent.Reason,
                    recordedAt,
                    recordedAt));
            });

        await harness.Handler.Handle(
            new DeleteUserCommand { UserId = userId },
            CancellationToken.None);

        await Assert.That(tenantAHome.LocationPrivacyStateId)
            .IsEqualTo((int)LocationPrivacyStateEnum.Erased);
        await Assert.That(tenantBHome.LocationPrivacyStateId)
            .IsEqualTo((int)LocationPrivacyStateEnum.Erased);
        await Assert.That(tenantAHome.Pii).IsNull();
        await Assert.That(tenantBHome.Pii).IsNull();
        await Assert.That(tenantAHome.FullName).IsEqualTo(Location.ErasedPrivateVenueLabel);
        await Assert.That(tenantBHome.FullName).IsEqualTo(Location.ErasedPrivateVenueLabel);
        await Assert.That(unrelatedHome.LocationPrivacyStateId)
            .IsEqualTo((int)LocationPrivacyStateEnum.Active);
    }

    private static DeletionHarness CreateHarness(Guid userId)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        IGenericRepository<UserPii, Guid> userPiiRepository =
            Substitute.For<IGenericRepository<UserPii, Guid>>();
        IUserAuthenticationTokenRepository tokenRepository =
            Substitute.For<IUserAuthenticationTokenRepository>();
        IActorRepository actorRepository = Substitute.For<IActorRepository>();
        IGenericRepository<ActorPii, Guid> actorPiiRepository =
            Substitute.For<IGenericRepository<ActorPii, Guid>>();
        ILocationRepository locationRepository = Substitute.For<ILocationRepository>();
        ILocationPrivacyErasureAuthority authority =
            Substitute.For<ILocationPrivacyErasureAuthority>();
        HybridCache cache = Substitute.For<HybridCache>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        User user = DataBuilder.User.Generate();
        user.Id = userId;

        userRepository.GetById(userId).Returns(user);
        userRepository.Delete(user).Returns(Task.CompletedTask);
        userPiiRepository.GetById(userId).Returns((UserPii?)null);
        tokenRepository.GetByUser(userId, Arg.Any<CancellationToken>())
            .Returns([]);
        actorRepository.GetActorByUserId(userId).Returns((Actor?)null);
        unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var services = new ServiceCollection();
        services.AddSingleton(userRepository);
        services.AddSingleton(userPiiRepository);
        services.AddSingleton(tokenRepository);
        services.AddSingleton(actorRepository);
        services.AddSingleton(actorPiiRepository);
        services.AddSingleton(locationRepository);
        services.AddSingleton(authority);
        services.AddSingleton(cache);
        services.AddSingleton(unitOfWork);
        ServiceProvider provider = services.BuildServiceProvider();
        var handler = ActivatorUtilities.CreateInstance<DeleteUserCommandHandler>(provider);

        return new DeletionHarness(handler, userRepository, locationRepository, authority, provider);
    }

    private static Location CreatePrivateHome(Guid tenantId, Guid ownerUserId, string name)
    {
        var location = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            FullName = name,
            Country = "BE",
            City = "Brussels",
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        location.ClassifyAsPrivateHome(ownerUserId);
        location.AttachPii(new LocationPii
        {
            LocationId = location.Id,
            Address = $"{name} address",
            Postcode = "1000",
        });
        return location;
    }

    private sealed record DeletionHarness(
        DeleteUserCommandHandler Handler,
        IUserRepository UserRepository,
        ILocationRepository LocationRepository,
        ILocationPrivacyErasureAuthority Authority,
        ServiceProvider Provider) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Provider.DisposeAsync();
    }
}
