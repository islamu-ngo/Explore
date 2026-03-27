// ABOUTME: Unit tests for UpdateActorCommandHandler covering the nullable-DTO pattern.
// ABOUTME: Verifies Actor update, appearance-only update, combined update, not-found, and validation failure paths.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Handlers.Commands;
using Explore.Application.Features.Actors.Requests.Commands;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Actors.Commands;

public class UpdateActorCommandHandlerTests
{
    private readonly IActorRepository _actorRepository;
    private readonly IActorTypeRepository _actorTypeRepository;
    private readonly IDidCustodyTypeRepository _didCustodyTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly UpdateActorCommandHandler _handler;

    public UpdateActorCommandHandlerTests()
    {
        _actorRepository = Substitute.For<IActorRepository>();
        _actorTypeRepository = Substitute.For<IActorTypeRepository>();
        _didCustodyTypeRepository = Substitute.For<IDidCustodyTypeRepository>();
        _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
        _mapper = Substitute.For<IMapper>();
        _cache = Substitute.For<HybridCache>();

        _handler = new UpdateActorCommandHandler(
            _actorRepository,
            _actorTypeRepository,
            _didCustodyTypeRepository,
            _storageObjectRepository,
            _mapper,
            _cache);
    }

    [Test]
    public async Task Handle_ActorNotFound_ReturnsFailure()
    {
        var actorId = Guid.NewGuid();
        _actorRepository.GetById(actorId).Returns((Actor?)null);

        var command = new UpdateActorCommand
        {
            Id = actorId,
            AppearanceDto = new UpdateActorAppearanceDto { BackgroundColor = "#FF0000" }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Actor not found.");
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
    }

    [Test]
    public async Task Handle_AppearanceOnly_AppliesBackgroundColor()
    {
        var actorId = Guid.NewGuid();
        var actor = CreateTestActor(actorId);
        _actorRepository.GetById(actorId).Returns(actor);

        var command = new UpdateActorCommand
        {
            Id = actorId,
            AppearanceDto = new UpdateActorAppearanceDto { BackgroundColor = "#00FF00" }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(actorId);
        await Assert.That(actor.BackgroundColor).IsEqualTo("#00FF00");
        await _actorRepository.Received(1).Update(actor);
    }

    [Test]
    public async Task Handle_AppearanceOnly_AppliesBackgroundEffect()
    {
        var actorId = Guid.NewGuid();
        var actor = CreateTestActor(actorId);
        _actorRepository.GetById(actorId).Returns(actor);

        var command = new UpdateActorCommand
        {
            Id = actorId,
            AppearanceDto = new UpdateActorAppearanceDto { BackgroundEffect = "SoftOverlay" }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(actor.BackgroundEffect).IsEqualTo("SoftOverlay");
        await _actorRepository.Received(1).Update(actor);
    }

    [Test]
    public async Task Handle_AppearanceOnly_AppliesBannerColor()
    {
        var actorId = Guid.NewGuid();
        var actor = CreateTestActor(actorId);
        _actorRepository.GetById(actorId).Returns(actor);

        var command = new UpdateActorCommand
        {
            Id = actorId,
            AppearanceDto = new UpdateActorAppearanceDto { BannerColor = "#0000FF" }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(actor.BannerColor).IsEqualTo("#0000FF");
    }

    [Test]
    public async Task Handle_AppearanceOnly_AppliesMultipleFields()
    {
        var actorId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var actor = CreateTestActor(actorId);
        _actorRepository.GetById(actorId).Returns(actor);
        _storageObjectRepository.Exists(imageId).Returns(true);

        var command = new UpdateActorCommand
        {
            Id = actorId,
            AppearanceDto = new UpdateActorAppearanceDto
            {
                BackgroundColor = "#FF0000",
                BackgroundEffect = "StrongOverlay",
                BannerColor = "#00FF00",
                BackgroundImageId = imageId
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(actor.BackgroundColor).IsEqualTo("#FF0000");
        await Assert.That(actor.BackgroundEffect).IsEqualTo("StrongOverlay");
        await Assert.That(actor.BannerColor).IsEqualTo("#00FF00");
        await Assert.That(actor.BackgroundImageId).IsEqualTo(imageId);
    }

    [Test]
    public async Task Handle_AppearanceOnly_NullFieldsPreserveExistingValues()
    {
        var actorId = Guid.NewGuid();
        var actor = CreateTestActor(actorId);
        actor.BackgroundColor = "#AABBCC";
        actor.BackgroundEffect = "Blur";
        actor.BannerColor = "#112233";
        _actorRepository.GetById(actorId).Returns(actor);

        var command = new UpdateActorCommand
        {
            Id = actorId,
            AppearanceDto = new UpdateActorAppearanceDto
            {
                BackgroundColor = "#FF0000"
                // Other fields null — should NOT overwrite existing values
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(actor.BackgroundColor).IsEqualTo("#FF0000");
        await Assert.That(actor.BackgroundEffect).IsEqualTo("Blur");
        await Assert.That(actor.BannerColor).IsEqualTo("#112233");
    }

    [Test]
    public async Task Handle_AppearanceInvalidHexColor_ReturnsValidationError()
    {
        var actorId = Guid.NewGuid();
        var actor = CreateTestActor(actorId);
        _actorRepository.GetById(actorId).Returns(actor);

        var command = new UpdateActorCommand
        {
            Id = actorId,
            AppearanceDto = new UpdateActorAppearanceDto { BackgroundColor = "not-a-hex-color" }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Actor appearance update failed.");
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
    }

    [Test]
    public async Task Handle_AppearanceInvalidEffect_ReturnsValidationError()
    {
        var actorId = Guid.NewGuid();
        var actor = CreateTestActor(actorId);
        _actorRepository.GetById(actorId).Returns(actor);

        var command = new UpdateActorCommand
        {
            Id = actorId,
            AppearanceDto = new UpdateActorAppearanceDto { BackgroundEffect = "InvalidEffect" }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
    }

    [Test]
    public async Task Handle_AppearanceNonExistentImageId_ReturnsValidationError()
    {
        var actorId = Guid.NewGuid();
        var nonExistentImageId = Guid.NewGuid();
        var actor = CreateTestActor(actorId);
        _actorRepository.GetById(actorId).Returns(actor);
        _storageObjectRepository.Exists(nonExistentImageId).Returns(false);

        var command = new UpdateActorCommand
        {
            Id = actorId,
            AppearanceDto = new UpdateActorAppearanceDto { BackgroundImageId = nonExistentImageId }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
    }

    [Test]
    public async Task Handle_ActorDtoOnly_ValidatesAndMaps()
    {
        var actorId = Guid.NewGuid();
        var actor = CreateTestActor(actorId);
        _actorRepository.GetById(actorId).Returns(actor);

        var dto = new UpdateActorDto
        {
            Id = actorId,
            ActorTypeId = 1,
            TenantId = Guid.NewGuid(),
            DisplayName = "Updated Name"
        };

        _actorTypeRepository.Exists(1).Returns(true);

        var command = new UpdateActorCommand
        {
            Id = actorId,
            ActorDto = dto
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        _mapper.Received(1).Map(dto, actor);
        await _actorRepository.Received(1).Update(actor);
    }

    [Test]
    public async Task Handle_InvalidActorDto_ReturnsValidationError()
    {
        var actorId = Guid.NewGuid();
        var actor = CreateTestActor(actorId);
        _actorRepository.GetById(actorId).Returns(actor);

        var dto = new UpdateActorDto
        {
            Id = actorId,
            ActorTypeId = 999, // Non-existent type
            TenantId = Guid.NewGuid(),
            DisplayName = "Updated Name"
        };

        _actorTypeRepository.Exists(999).Returns(false);

        var command = new UpdateActorCommand
        {
            Id = actorId,
            ActorDto = dto
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Actor update failed.");
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
    }

    [Test]
    public async Task Handle_InvalidatesCache()
    {
        var actorId = Guid.NewGuid();
        var actor = CreateTestActor(actorId);
        _actorRepository.GetById(actorId).Returns(actor);

        var command = new UpdateActorCommand
        {
            Id = actorId,
            AppearanceDto = new UpdateActorAppearanceDto { BackgroundColor = "#FFFFFF" }
        };

        await _handler.Handle(command, CancellationToken.None);

        await _cache.Received(1).RemoveAsync($"actor:detail:{actorId}", Arg.Any<CancellationToken>());
    }

    private static Actor CreateTestActor(Guid id) =>
        new()
        {
            Id = id,
            Pii = new ActorPii { DisplayName = "Test Actor" },
            ActorType = null!,
            Tenant = null!
        };
}
