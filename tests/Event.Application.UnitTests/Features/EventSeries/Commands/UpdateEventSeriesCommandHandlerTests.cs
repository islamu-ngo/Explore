// ABOUTME: Unit tests for grouped EventSeries PATCH command handling.
// ABOUTME: Covers validation, concurrency, clear semantics, one-save updates, and event cache invalidation.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSeries;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventSeries.Handlers.Commands;
using Explore.Application.Features.EventSeries.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEventSeries = Explore.Domain.EventSeries;

namespace Event.Application.UnitTests.Features.EventSeries.Commands;

public class UpdateEventSeriesCommandHandlerTests
{
    private readonly IEventSeriesRepository _eventSeriesRepository = Substitute.For<IEventSeriesRepository>();
    private readonly IStorageObjectRepository _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UpdateEventSeriesCommandHandler _handler;

    public UpdateEventSeriesCommandHandlerTests()
    {
        _handler = new UpdateEventSeriesCommandHandler(
            _eventSeriesRepository,
            _storageObjectRepository,
            _cache);
    }

    [Test]
    public async Task Handle_WithEmptyWrapper_ReturnsFailedResponseWithoutSaving()
    {
        var command = new UpdateEventSeriesCommand
        {
            EventSeriesId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventSeriesDto = new UpdateEventSeriesDto()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).Contains("At least one event series update group must be provided.");
        await _eventSeriesRepository.DidNotReceive().Update(Arg.Any<DomainEventSeries>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithMissingSeries_ReturnsFailedResponseWithoutSaving()
    {
        var seriesId = Guid.NewGuid();
        _eventSeriesRepository.GetEventSeriesWithEvents(seriesId).Returns((DomainEventSeries?)null);

        var command = new UpdateEventSeriesCommand
        {
            EventSeriesId = seriesId,
            ActorId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventSeriesDto = new UpdateEventSeriesDto
            {
                Title = new UpdateEventSeriesTitleDto { Value = "Updated series" }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Event series not found.");
        await _eventSeriesRepository.DidNotReceive().Update(Arg.Any<DomainEventSeries>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithStaleConcurrencyStamp_ThrowsConflictWithoutSaving()
    {
        var seriesId = Guid.NewGuid();
        var series = CreateEventSeries(seriesId, Guid.NewGuid());
        series.ConcurrencyStamp = Guid.NewGuid();
        _eventSeriesRepository.GetEventSeriesWithEvents(seriesId).Returns(series);

        var command = new UpdateEventSeriesCommand
        {
            EventSeriesId = seriesId,
            ActorId = series.ActorId,
            TenantId = series.TenantId,
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventSeriesDto = new UpdateEventSeriesDto
            {
                Title = new UpdateEventSeriesTitleDto { Value = "Updated series" }
            }
        };

        await Assert.That(async () => await _handler.Handle(command, CancellationToken.None))
            .Throws<ConcurrencyConflictException>();
        await _eventSeriesRepository.DidNotReceive().Update(Arg.Any<DomainEventSeries>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithDescriptionClear_SavesOnceAndInvalidatesTenantEventList()
    {
        var seriesId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        var series = CreateEventSeries(seriesId, tenantId);
        series.Description = "Ramadan program";
        series.ConcurrencyStamp = stamp;
        _eventSeriesRepository.GetEventSeriesWithEvents(seriesId).Returns(series);

        var command = new UpdateEventSeriesCommand
        {
            EventSeriesId = seriesId,
            ActorId = series.ActorId,
            TenantId = series.TenantId,
            ExpectedConcurrencyStamp = stamp,
            EventSeriesDto = new UpdateEventSeriesDto
            {
                Description = new UpdateEventSeriesDescriptionDto { Value = OptionalUpdate<string?>.Set(null) }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(series.Description).IsNull();
        await _eventSeriesRepository.Received(1).Update(series);
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithMultipleGroups_AppliesAllGroupsAndSavesOnce()
    {
        var seriesId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        var featuredImageId = Guid.NewGuid();
        var series = CreateEventSeries(seriesId, tenantId);
        series.ConcurrencyStamp = stamp;
        _eventSeriesRepository.GetEventSeriesWithEvents(seriesId).Returns(series);
        _storageObjectRepository.GetById(featuredImageId).Returns(new StorageObject
        {
            Id = featuredImageId,
            TenantId = tenantId,
            Tenant = null!,
            FileType = null!,
            Uri = "storage://series",
            Provider = "local",
            FullName = "series.png",
            SafeDisplayName = "series.png",
            Extension = "png",
            ContentType = "image/png",
            Purpose = StorageObjectPurposes.EventImage,
            Visibility = StorageObjectVisibilities.PublicImage,
            LifecycleState = StorageObjectLifecycleStates.Active
        });

        var command = new UpdateEventSeriesCommand
        {
            EventSeriesId = seriesId,
            ActorId = series.ActorId,
            TenantId = series.TenantId,
            ExpectedConcurrencyStamp = stamp,
            EventSeriesDto = new UpdateEventSeriesDto
            {
                Title = new UpdateEventSeriesTitleDto { Value = "Updated series" },
                Slug = new UpdateEventSeriesSlugDto { Value = OptionalUpdate<string?>.Set("updated-series") },
                FeaturedImage = new UpdateEventSeriesFeaturedImageDto { Value = OptionalUpdate<Guid?>.Set(featuredImageId) },
                Publication = new UpdateEventSeriesPublicationDto { IsPublished = true }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(series.Title).IsEqualTo("Updated series");
        await Assert.That(series.Slug).IsEqualTo("updated-series");
        await Assert.That(series.FeaturedImageId).IsEqualTo(featuredImageId);
        await Assert.That(series.IsPublished).IsTrue();
        await _eventSeriesRepository.Received(1).Update(series);
    }

    [Test]
    public async Task Handle_WhenFeaturedImageIsCrossTenant_RejectsBeforeMutationOrSave()
    {
        var seriesId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        var series = CreateEventSeries(seriesId, tenantId);
        series.FeaturedImageId = Guid.NewGuid();
        Guid? originalFeaturedImageId = series.FeaturedImageId;
        series.ConcurrencyStamp = stamp;
        _eventSeriesRepository.GetEventSeriesWithEvents(seriesId).Returns(series);
        _storageObjectRepository.GetById(imageId).Returns(new StorageObject
        {
            Id = imageId,
            TenantId = Guid.NewGuid(),
            Tenant = null!,
            FileType = null!,
            Uri = "storage://series.png",
            Provider = "local",
            FullName = "series.png",
            SafeDisplayName = "series.png",
            Extension = "png",
            ContentType = "image/png",
            Purpose = StorageObjectPurposes.EventImage,
            Visibility = StorageObjectVisibilities.PublicImage,
            LifecycleState = StorageObjectLifecycleStates.Active
        });
        var command = new UpdateEventSeriesCommand
        {
            EventSeriesId = seriesId,
            ActorId = series.ActorId,
            TenantId = tenantId,
            ExpectedConcurrencyStamp = stamp,
            EventSeriesDto = new UpdateEventSeriesDto
            {
                FeaturedImage = new UpdateEventSeriesFeaturedImageDto
                {
                    Value = OptionalUpdate<Guid?>.Set(imageId)
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(series.FeaturedImageId).IsEqualTo(originalFeaturedImageId);
        await _eventSeriesRepository.DidNotReceive().Update(Arg.Any<DomainEventSeries>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static DomainEventSeries CreateEventSeries(Guid id, Guid tenantId)
    {
        var tenant = new Tenant
        {
            Id = tenantId,
            FullName = "Tenant",
            Slug = "tenant",
            TenantStatusId = 1,
            TenantStatus = new TenantStatus
            {
                Id = 1,
                MasterCode = "ACTIVE",
                FullName = "Active",
                IsActiveState = true
            }
        };

        return new DomainEventSeries
        {
            Id = id,
            TenantId = tenantId,
            Title = "Original series",
            Slug = "original-series",
            Description = "Original description",
            ActorId = Guid.NewGuid(),
            IsPublished = false,
            VisibilityTypeId = 1,
            VisibilityType = new VisibilityType
            {
                Id = 1,
                MasterCode = "PUBLIC",
                FullName = "Public"
            },
            Actor = new Actor
            {
                Id = Guid.NewGuid(),
                Pii = new ActorPii { DisplayName = "Organizer" },
                ActorTypeId = 1,
                ActorType = new ActorType
                {
                    Id = 1,
                    MasterCode = "ORGANIZATION",
                    FullName = "Organization"
                }
            },
            Tenant = tenant
        };
    }
}
