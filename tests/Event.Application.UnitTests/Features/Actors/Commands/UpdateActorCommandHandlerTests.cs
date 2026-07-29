// ABOUTME: Unit tests for grouped actor update command handling.
// ABOUTME: Covers validation, concurrency, OptionalUpdate clear/set behavior, storage linking, and cache invalidation.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Exceptions;
using Explore.Application.Features.Actors.Handlers.Commands;
using Explore.Application.Features.Actors.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Actors.Commands;

public class UpdateActorCommandHandlerTests
{
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly IActorTypeRepository _actorTypeRepository = Substitute.For<IActorTypeRepository>();
    private readonly IStorageObjectRepository _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
    private readonly IAuthorizationProvider _authorizationProvider = Substitute.For<IAuthorizationProvider>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UpdateActorCommandHandler _handler;

    public UpdateActorCommandHandlerTests()
    {
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                return operation(CancellationToken.None);
            });
        _authorizationProvider
            .IsAllowedBatchAsync(Arg.Any<IReadOnlyList<AuthorizationCheck>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var checks = callInfo.Arg<IReadOnlyList<AuthorizationCheck>>();
                return checks.Select(_ => true).ToArray();
            });

        _handler = new UpdateActorCommandHandler(
            _actorRepository,
            _actorTypeRepository,
            _storageObjectRepository,
            _authorizationProvider,
            _tenantContext,
            _unitOfWork,
            _cache);
    }

    [Test]
    public async Task Handle_WhenWrapperHasNoGroups_ReturnsValidationFailureAndDoesNotSave()
    {
        var result = await _handler.Handle(new UpdateActorCommand
        {
            ActorId = Guid.CreateVersion7(),
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            UpdateActorDto = new UpdateActorDto()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Actor update failed.");
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenAnyPresentGroupIsUnauthorized_ThrowsAndDoesNotPartiallyApply()
    {
        var actor = CreateTestActor();
        _actorRepository.GetById(actor.Id).Returns(actor);
        _authorizationProvider
            .IsAllowedBatchAsync(Arg.Any<IReadOnlyList<AuthorizationCheck>>(), Arg.Any<CancellationToken>())
            .Returns([true, false]);

        await Assert.That(async () => await _handler.Handle(new UpdateActorCommand
        {
            ActorId = actor.Id,
            ExpectedConcurrencyStamp = actor.ConcurrencyStamp,
            UpdateActorDto = new UpdateActorDto
            {
                Profile = new UpdateActorProfileDto
                {
                    DisplayName = "Updated Actor"
                },
                Appearance = new UpdateActorAppearanceDto
                {
                    BackgroundColor = OptionalUpdate<string?>.Set("#123456")
                }
            }
        }, CancellationToken.None)).Throws<AuthorizationException>();

        await Assert.That(actor.DisplayName).IsEqualTo("Test Actor");
        await Assert.That(actor.BackgroundColor).IsNull();
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenExpectedConcurrencyStampIsStale_ThrowsConflictAndDoesNotSave()
    {
        var actor = CreateTestActor();
        _actorRepository.GetById(actor.Id).Returns(actor);

        await Assert.That(async () => await _handler.Handle(new UpdateActorCommand
        {
            ActorId = actor.Id,
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            UpdateActorDto = new UpdateActorDto
            {
                Profile = new UpdateActorProfileDto
                {
                    DisplayName = "Updated Actor"
                }
            }
        }, CancellationToken.None)).Throws<ConcurrencyConflictException>();

        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenProfileGroupIsPresent_AppliesOnlyProvidedProfileFields()
    {
        var actor = CreateTestActor();
        actor.ActorTypeId = 1;
        _actorRepository.GetById(actor.Id).Returns(actor);

        var result = await _handler.Handle(new UpdateActorCommand
        {
            ActorId = actor.Id,
            ExpectedConcurrencyStamp = actor.ConcurrencyStamp,
            UpdateActorDto = new UpdateActorDto
            {
                Profile = new UpdateActorProfileDto
                {
                    DisplayName = "Updated Actor"
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(actor.DisplayName).IsEqualTo("Updated Actor");
        await Assert.That(actor.ActorTypeId).IsEqualTo(1);
        await _actorRepository.Received(1).Update(actor);
        await _cache.Received(1).RemoveAsync($"actor:detail:{actor.Id}", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenAppearanceUsesOptionalUpdate_ClearsAndSetsFields()
    {
        var actor = CreateTestActor();
        actor.BackgroundColor = "#AABBCC";
        actor.BackgroundEffect = "Blur";
        actor.BannerColor = "#112233";
        _actorRepository.GetById(actor.Id).Returns(actor);

        var result = await _handler.Handle(new UpdateActorCommand
        {
            ActorId = actor.Id,
            ExpectedConcurrencyStamp = actor.ConcurrencyStamp,
            UpdateActorDto = new UpdateActorDto
            {
                Appearance = new UpdateActorAppearanceDto
                {
                    BackgroundColor = OptionalUpdate<string?>.Set(null),
                    BackgroundEffect = OptionalUpdate<string?>.Set("SoftOverlay")
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(actor.BackgroundColor).IsNull();
        await Assert.That(actor.BackgroundEffect).IsEqualTo("SoftOverlay");
        await Assert.That(actor.BannerColor).IsEqualTo("#112233");
    }

    [Test]
    public async Task Handle_WhenProfileImageIsSet_UpdatesActorAndLinksTrackedStorageWithSingleActorSave()
    {
        var actor = CreateTestActor();
        var tenantId = Guid.CreateVersion7();
        var storageObject = CreateStorageObject(tenantId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorRepository.GetById(actor.Id).Returns(actor);
        _storageObjectRepository.Exists(storageObject.Id).Returns(true);
        _storageObjectRepository.GetById(storageObject.Id).Returns(storageObject);

        var result = await _handler.Handle(new UpdateActorCommand
        {
            ActorId = actor.Id,
            ExpectedConcurrencyStamp = actor.ConcurrencyStamp,
            UpdateActorDto = new UpdateActorDto
            {
                ProfileImage = new UpdateActorProfileImageDto
                {
                    ProfilePictureId = OptionalUpdate<Guid?>.Set(storageObject.Id)
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(actor.ProfilePictureUri).IsEqualTo(storageObject.Uri);
        await Assert.That(storageObject.ActorId).IsEqualTo(actor.Id);
        await _actorRepository.Received(1).Update(actor);
        await _storageObjectRepository.DidNotReceive().Update(Arg.Any<StorageObject>());
    }

    [Test]
    public async Task Handle_WhenProfileImageIsCrossTenant_ReturnsFailureWithoutMutation()
    {
        var actor = CreateTestActor();
        var storageObject = CreateStorageObject(Guid.CreateVersion7());
        _tenantContext.TenantId.Returns(Guid.CreateVersion7());
        _actorRepository.GetById(actor.Id).Returns(actor);
        _storageObjectRepository.Exists(storageObject.Id).Returns(true);
        _storageObjectRepository.GetById(storageObject.Id).Returns(storageObject);

        var result = await _handler.Handle(new UpdateActorCommand
        {
            ActorId = actor.Id,
            ExpectedConcurrencyStamp = actor.ConcurrencyStamp,
            UpdateActorDto = new UpdateActorDto
            {
                ProfileImage = new UpdateActorProfileImageDto
                {
                    ProfilePictureId = OptionalUpdate<Guid?>.Set(storageObject.Id)
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(actor.ProfilePictureUri).IsNull();
        await Assert.That(storageObject.ActorId).IsNull();
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
    }

    [Test]
    public async Task Handle_WhenAppearanceGroupHasNoFieldOperations_ReturnsValidationFailure()
    {
        var result = await _handler.Handle(new UpdateActorCommand
        {
            ActorId = Guid.CreateVersion7(),
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            UpdateActorDto = new UpdateActorDto
            {
                Appearance = new UpdateActorAppearanceDto()
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Appearance group must include at least one field operation.");
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
    }

    private static Actor CreateTestActor()
    {
        var actorId = Guid.CreateVersion7();
        return new Actor
        {
            Id = actorId,
            ActorTypeId = 1,
            ConcurrencyStamp = Guid.CreateVersion7(),
            Pii = new ActorPii
            {
                ActorId = actorId,
                DisplayName = "Test Actor"
            },
            ActorType = null!
        };
    }

    private static StorageObject CreateStorageObject(Guid tenantId) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            FileType = null!,
            Uri = "https://cdn.example.test/actor.png",
            Provider = "local",
            FullName = "actor.png",
            SafeDisplayName = "actor.png",
            Extension = "png",
            ContentType = "image/png",
            Visibility = StorageObjectVisibilities.PublicImage,
            Purpose = StorageObjectPurposes.ProfileImage,
            LifecycleState = StorageObjectLifecycleStates.Active,
            Tenant = null!
        };
}
