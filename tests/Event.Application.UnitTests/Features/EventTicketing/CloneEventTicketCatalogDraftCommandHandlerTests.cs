// ABOUTME: Tests cloning a published ticket catalog into a new draft.
// ABOUTME: Proves platform authority, published-source requirements, and duplicate-draft rejection.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventTicketing.Handlers.Commands;
using Explore.Application.Features.EventTicketing.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Application.UnitTests.Features.EventTicketing;

[Category("Phase43Ticketing")]
public sealed class CloneEventTicketCatalogDraftCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    public CloneEventTicketCatalogDraftCommandHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>())
            .Returns(CreatePlatformEvent());
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns((EventTicketCatalogVersion?)null);
    }

    [Test]
    public async Task Handle_WithPublishedCatalogAndNoDraft_ClonesPublishedCatalog()
    {
        EventTicketCatalogVersion published = CreatePublishedCatalog();
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(published);

        var result = await CreateHandler().Handle(
            new CloneEventTicketCatalogDraftCommand { EventId = _eventId },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _catalogs.Received(1).AddAsync(
            Arg.Is<EventTicketCatalogVersion>(catalog =>
                catalog.EventId == _eventId
                && catalog.TicketCatalogStatusId == (int)TicketCatalogStatusEnum.Draft
                && catalog.VersionNumber == published.VersionNumber + 1
                && catalog.TicketTypes.Single().Id != published.TicketTypes.Single().Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDraftAlreadyExists_ReturnsValidationFailure()
    {
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreatePublishedCatalog());
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 2));

        var result = await CreateHandler().Handle(
            new CloneEventTicketCatalogDraftCommand { EventId = _eventId },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await Assert.That(result.Errors).Contains("A ticket catalog draft already exists.");
        await _catalogs.DidNotReceive().AddAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPublishedCatalogIsMissing_ReturnsNotFound()
    {
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns((EventTicketCatalogVersion?)null);

        var result = await CreateHandler().Handle(
            new CloneEventTicketCatalogDraftCommand { EventId = _eventId },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await _catalogs.DidNotReceive().AddAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenEventIsNotPlatformManaged_ReturnsNotFoundWithoutReadingCatalog()
    {
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>())
            .Returns(CreateEvent(ParticipationHandlingModeEnum.ExternalManaged));

        var result = await CreateHandler().Handle(
            new CloneEventTicketCatalogDraftCommand { EventId = _eventId },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await _catalogs.DidNotReceive().GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>());
        await _catalogs.DidNotReceive().GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRepositoryReportsConcurrencyConflict_ReturnsConflict()
    {
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreatePublishedCatalog());
        _catalogs.AddAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The catalog version was created by another request."));

        var result = await CreateHandler().Handle(
            new CloneEventTicketCatalogDraftCommand { EventId = _eventId },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_concurrency_conflict");
        await Assert.That(result.Message).IsEqualTo("Ticketing configuration was updated by another request.");
        await Assert.That(result.Errors).Contains("The catalog version was created by another request.");
    }

    [Test]
    public async Task Handle_WhenPersistenceReportsInvalidOperation_PropagatesFailure()
    {
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreatePublishedCatalog());
        _catalogs.AddAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("Persistence failed."));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateHandler().Handle(
                new CloneEventTicketCatalogDraftCommand { EventId = _eventId },
                CancellationToken.None));

        await Assert.That(exception.Message).IsEqualTo("Persistence failed.");
    }

    private CloneEventTicketCatalogDraftCommandHandler CreateHandler() => new(_events, _catalogs, _tenant);

    private EventTicketCatalogVersion CreatePublishedCatalog()
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 1);
        EventTicketType ticket = EventTicketType.Create(
            Guid.CreateVersion7(),
            _tenantId,
            catalog.Id,
            "General admission",
            "USD",
            TicketPricingModeEnum.Free,
            null,
            null,
            null,
            ParticipantDataCollectionModeEnum.None,
            null,
            null,
            null,
            false,
            false,
            null,
            null,
            null,
            null);
        catalog.AddTicketType(ticket, null);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, _tenantId, _eventId, 1));
        catalog.Publish();
        return catalog;
    }

    private DomainEvent CreatePlatformEvent() => CreateEvent(ParticipationHandlingModeEnum.PlatformManaged);

    private DomainEvent CreateEvent(ParticipationHandlingModeEnum mode) => new()
    {
        Id = _eventId,
        TenantId = _tenantId,
        Title = "Ticketing event",
        Actor = null!,
        Tenant = null!,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!,
        ParticipationConfiguration = EventParticipationConfiguration.Create(
            _eventId,
            _tenantId,
            (int)mode,
            (int)AdvanceRegistrationObligationEnum.Required,
            mode == ParticipationHandlingModeEnum.PlatformManaged
                ? (int)IdentityAccessModeEnum.AccountRequired
                : null,
            guestRecoveryPolicy: null,
            DateTime.UtcNow)
    };
}
