// ABOUTME: Tests ticket catalog draft creation handler authority, uniqueness, and domain validation.
// ABOUTME: Proves only valid platform-managed events can create the first draft.

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
public sealed class CreateEventTicketCatalogDraftCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    public CreateEventTicketCatalogDraftCommandHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>())
            .Returns(CreatePlatformEvent());
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns((EventTicketCatalogVersion?)null);
    }

    [Test]
    public async Task Handle_WithValidPlatformEvent_CreatesDraft()
    {
        var result = await CreateHandler().Handle(
            new CreateEventTicketCatalogDraftCommand { EventId = _eventId, CurrencyCode = "USD" },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Ticket catalog draft created.");
        await _catalogs.Received(1).AddAsync(
            Arg.Is<EventTicketCatalogVersion>(catalog =>
                catalog.EventId == _eventId
                && catalog.TenantId == _tenantId
                && catalog.VersionNumber == 1
                && catalog.TicketCatalogStatusId == (int)TicketCatalogStatusEnum.Draft),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenEventIsMissing_ReturnsNotFound()
    {
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns((DomainEvent?)null);

        var result = await CreateHandler().Handle(
            new CreateEventTicketCatalogDraftCommand { EventId = _eventId, CurrencyCode = "USD" },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await _catalogs.DidNotReceive().AddAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenCatalogAlreadyExists_ReturnsNotFound()
    {
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 1));

        var result = await CreateHandler().Handle(
            new CreateEventTicketCatalogDraftCommand { EventId = _eventId, CurrencyCode = "USD" },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await _catalogs.DidNotReceive().AddAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenCurrencyIsInvalid_ReturnsValidationFailure()
    {
        var result = await CreateHandler().Handle(
            new CreateEventTicketCatalogDraftCommand { EventId = _eventId, CurrencyCode = "BAD" },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await _catalogs.DidNotReceive().AddAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRepositoryReportsConcurrencyConflict_ReturnsConflict()
    {
        _catalogs.AddAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The catalog version was created by another request."));

        var result = await CreateHandler().Handle(
            new CreateEventTicketCatalogDraftCommand { EventId = _eventId, CurrencyCode = "USD" },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_concurrency_conflict");
        await Assert.That(result.Message).IsEqualTo("Ticketing configuration was updated by another request.");
        await Assert.That(result.Errors).Contains("The catalog version was created by another request.");
    }

    private CreateEventTicketCatalogDraftCommandHandler CreateHandler() => new(_events, _catalogs, _tenant);

    private DomainEvent CreatePlatformEvent() => new()
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
            (int)ParticipationHandlingModeEnum.PlatformManaged,
            (int)AdvanceRegistrationObligationEnum.Required,
            (int)IdentityAccessModeEnum.AccountRequired,
            guestRecoveryPolicy: null,
            DateTime.UtcNow)
    };
}
