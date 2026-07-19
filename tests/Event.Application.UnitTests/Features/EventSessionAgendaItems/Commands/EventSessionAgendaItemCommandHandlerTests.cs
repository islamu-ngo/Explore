// ABOUTME: Unit tests for EventSessionAgendaItem create, update, and delete transaction boundaries.
// ABOUTME: Proves server-owned attachment, cross-event TBA moves, final detach, cancellation, and rollback.

using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.Features.EventSessionAgendaItems.Handlers.Commands;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;
using Explore.Application.Services;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventSessionAgendaItems.Commands;

[Category("EventLocationPrivacy")]
public sealed class EventSessionAgendaItemCommandHandlerTests
{
    [Test]
    public async Task CreateAttachesPhysicalEventLocationInsideUnitOfWork()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorUserId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid sessionId = Guid.CreateVersion7();
        Guid locationId = Guid.CreateVersion7();
        bool insideTransaction = false;
        var agendaRepository = Substitute.For<IEventSessionAgendaItemRepository>();
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var locationRepository = Substitute.For<ILocationRepository>();
        var eventLocationRepository = Substitute.For<IEventLocationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        EventSession parentSession = CreateSession(tenantId, eventId, sessionId);
        EventSessionAgendaItem agendaItem = CreateAgendaItem(tenantId, parentSession, locationId);
        CreateEventSessionAgendaItemDto dto = CreateDto(sessionId, locationId);

        sessionRepository.Exists(sessionId).Returns(true);
        sessionRepository.GetById(sessionId).Returns(parentSession);
        locationRepository.Exists(locationId).Returns(true);
        mapper.Map<EventSessionAgendaItem>(dto).Returns(agendaItem);
        eventLocationRepository.FindActivePhysicalAsync(eventId, locationId, Arg.Any<CancellationToken>())
            .Returns((EventLocation?)null);
        eventLocationRepository.AddAsync(Arg.Any<EventLocation>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (!insideTransaction)
                {
                    throw new InvalidOperationException("Attachment escaped the unit of work.");
                }

                return call.ArgAt<EventLocation>(0);
            });
        agendaRepository.Create(agendaItem).Returns(_ =>
        {
            if (!insideTransaction)
            {
                throw new InvalidOperationException("Carrier write escaped the unit of work.");
            }

            return agendaItem;
        });
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<EventSessionAgendaItem>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                insideTransaction = true;
                try
                {
                    return await call.ArgAt<Func<CancellationToken, Task<EventSessionAgendaItem>>>(0)(
                        call.ArgAt<CancellationToken>(1));
                }
                finally
                {
                    insideTransaction = false;
                }
            });
        var handler = new CreateEventSessionAgendaItemCommandHandler(
            agendaRepository,
            sessionRepository,
            locationRepository,
            CreateTenantContext(tenantId),
            unitOfWork,
            CreateAttachmentService(eventLocationRepository, tenantId, actorUserId),
            mapper);

        var result = await handler.Handle(
            new CreateEventSessionAgendaItemCommand { AgendaItemDto = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(agendaItem.EventLocationId).IsNotNull();
        await Assert.That(agendaItem.LocationId).IsEqualTo(locationId);
        await eventLocationRepository.Received(1).AddAsync(
            Arg.Is<EventLocation>(item =>
                item.TenantId == tenantId
                && item.EventId == eventId
                && item.LocationId == locationId
                && !item.IsToBeAnnounced),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateMovesToTbaAndDetachesFinalPreviousReferenceInsideUnitOfWork()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorUserId = Guid.CreateVersion7();
        EventSession previousSession = CreateSession(tenantId, Guid.CreateVersion7(), Guid.CreateVersion7());
        EventSession destinationSession = CreateSession(tenantId, Guid.CreateVersion7(), Guid.CreateVersion7());
        EventLocation previousPlacement = EventLocation.CreatePhysical(
            tenantId,
            previousSession.EventId,
            Guid.CreateVersion7(),
            actorUserId,
            DateTime.UnixEpoch);
        EventSessionAgendaItem agendaItem = CreateAgendaItem(
            tenantId,
            previousSession,
            previousPlacement.LocationId);
        agendaItem.AssignEventLocation(previousPlacement);
        UpdateEventSessionAgendaItemDto dto = UpdateDto(agendaItem.Id, destinationSession.Id, locationId: null);
        bool insideTransaction = false;
        var agendaRepository = Substitute.For<IEventSessionAgendaItemRepository>();
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var locationRepository = Substitute.For<ILocationRepository>();
        var eventLocationRepository = Substitute.For<IEventLocationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();

        sessionRepository.Exists(destinationSession.Id).Returns(true);
        sessionRepository.GetById(destinationSession.Id).Returns(destinationSession);
        agendaRepository.GetById(agendaItem.Id).Returns(agendaItem);
        mapper.Map(dto, agendaItem).Returns(_ =>
        {
            agendaItem.EventSessionId = destinationSession.Id;
            agendaItem.LocationId = null;
            return agendaItem;
        });
        eventLocationRepository.GetForUpdateAsync(previousPlacement.Id, Arg.Any<CancellationToken>())
            .Returns(previousPlacement);
        eventLocationRepository.FindActiveToBeAnnouncedAsync(
                destinationSession.EventId,
                Arg.Any<CancellationToken>())
            .Returns((EventLocation?)null);
        eventLocationRepository.AddAsync(Arg.Any<EventLocation>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<EventLocation>(0));
        eventLocationRepository.HasActiveCarrierReferencesAsync(
                previousPlacement.Id,
                Arg.Any<CancellationToken>())
            .Returns(false);
        agendaRepository.Update(agendaItem).Returns(_ =>
        {
            if (!insideTransaction)
            {
                throw new InvalidOperationException("Carrier move escaped the unit of work.");
            }

            return Task.CompletedTask;
        });
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                insideTransaction = true;
                try
                {
                    await call.ArgAt<Func<CancellationToken, Task>>(0)(call.ArgAt<CancellationToken>(1));
                }
                finally
                {
                    insideTransaction = false;
                }
            });
        var handler = new UpdateEventSessionAgendaItemCommandHandler(
            agendaRepository,
            sessionRepository,
            locationRepository,
            unitOfWork,
            CreateAttachmentService(eventLocationRepository, tenantId, actorUserId),
            mapper);

        var result = await handler.Handle(
            new UpdateEventSessionAgendaItemCommand { AgendaItemDto = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(agendaItem.EventSession).IsSameReferenceAs(destinationSession);
        await Assert.That(agendaItem.EventLocationId).IsNotEqualTo(previousPlacement.Id);
        await Assert.That(agendaItem.EventLocation!.EventId).IsEqualTo(destinationSession.EventId);
        await Assert.That(agendaItem.EventLocation.IsToBeAnnounced).IsTrue();
        await Assert.That(agendaItem.LocationId).IsNull();
        await Assert.That(previousPlacement.IsDeleted).IsTrue();
        await eventLocationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteDetachesFinalReferenceInsideUnitOfWork()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorUserId = Guid.CreateVersion7();
        EventSession session = CreateSession(tenantId, Guid.CreateVersion7(), Guid.CreateVersion7());
        EventLocation placement = EventLocation.CreateToBeAnnounced(
            tenantId,
            session.EventId,
            actorUserId,
            DateTime.UnixEpoch);
        EventSessionAgendaItem agendaItem = CreateAgendaItem(tenantId, session, locationId: null);
        agendaItem.AssignEventLocation(placement);
        bool insideTransaction = false;
        var agendaRepository = Substitute.For<IEventSessionAgendaItemRepository>();
        var eventLocationRepository = Substitute.For<IEventLocationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        agendaRepository.GetById(agendaItem.Id).Returns(agendaItem);
        agendaRepository.Delete(agendaItem).Returns(_ =>
        {
            if (!insideTransaction)
            {
                throw new InvalidOperationException("Delete escaped the unit of work.");
            }

            return Task.CompletedTask;
        });
        eventLocationRepository.HasActiveCarrierReferencesAsync(placement.Id, Arg.Any<CancellationToken>())
            .Returns(false);
        eventLocationRepository.GetForUpdateAsync(placement.Id, Arg.Any<CancellationToken>())
            .Returns(placement);
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                insideTransaction = true;
                try
                {
                    await call.ArgAt<Func<CancellationToken, Task>>(0)(call.ArgAt<CancellationToken>(1));
                }
                finally
                {
                    insideTransaction = false;
                }
            });
        var handler = new DeleteEventSessionAgendaItemCommandHandler(
            agendaRepository,
            unitOfWork,
            CreateAttachmentService(eventLocationRepository, tenantId, actorUserId));

        bool deleted = await handler.Handle(
            new DeleteEventSessionAgendaItemCommand { Id = agendaItem.Id },
            CancellationToken.None);

        await Assert.That(deleted).IsTrue();
        await Assert.That(placement.IsDeleted).IsTrue();
        await eventLocationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePropagatesCancellationToUnitOfWorkWithoutPartialWrites()
    {
        Guid tenantId = Guid.CreateVersion7();
        EventSession session = CreateSession(tenantId, Guid.CreateVersion7(), Guid.CreateVersion7());
        var agendaRepository = Substitute.For<IEventSessionAgendaItemRepository>();
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var locationRepository = Substitute.For<ILocationRepository>();
        var eventLocationRepository = Substitute.For<IEventLocationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        using var cancellation = new CancellationTokenSource();
        CreateEventSessionAgendaItemDto dto = CreateDto(session.Id, locationId: null);
        EventSessionAgendaItem agendaItem = CreateAgendaItem(tenantId, session, locationId: null);

        sessionRepository.Exists(session.Id).Returns(true);
        sessionRepository.GetById(session.Id).Returns(session);
        mapper.Map<EventSessionAgendaItem>(dto).Returns(agendaItem);
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<EventSessionAgendaItem>>>(),
                cancellation.Token)
            .Returns(call =>
            {
                cancellation.Cancel();
                call.ArgAt<CancellationToken>(1).ThrowIfCancellationRequested();
                return Task.FromResult(agendaItem);
            });
        var handler = new CreateEventSessionAgendaItemCommandHandler(
            agendaRepository,
            sessionRepository,
            locationRepository,
            CreateTenantContext(tenantId),
            unitOfWork,
            CreateAttachmentService(eventLocationRepository, tenantId, Guid.CreateVersion7()),
            mapper);

        await Assert.ThrowsAsync<OperationCanceledException>(() => handler.Handle(
            new CreateEventSessionAgendaItemCommand { AgendaItemDto = dto },
            cancellation.Token));
        await eventLocationRepository.DidNotReceive().AddAsync(
            Arg.Any<EventLocation>(),
            Arg.Any<CancellationToken>());
        await agendaRepository.DidNotReceive().Create(Arg.Any<EventSessionAgendaItem>());
    }

    [Test]
    public async Task CreateFailureRollsBackStagedAttachmentAndCarrier()
    {
        Guid tenantId = Guid.CreateVersion7();
        EventSession session = CreateSession(tenantId, Guid.CreateVersion7(), Guid.CreateVersion7());
        var stagedPlacements = new List<EventLocation>();
        var stagedCarriers = new List<EventSessionAgendaItem>();
        var agendaRepository = Substitute.For<IEventSessionAgendaItemRepository>();
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var locationRepository = Substitute.For<ILocationRepository>();
        var eventLocationRepository = Substitute.For<IEventLocationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        CreateEventSessionAgendaItemDto dto = CreateDto(session.Id, locationId: null);
        EventSessionAgendaItem agendaItem = CreateAgendaItem(tenantId, session, locationId: null);

        sessionRepository.Exists(session.Id).Returns(true);
        sessionRepository.GetById(session.Id).Returns(session);
        mapper.Map<EventSessionAgendaItem>(dto).Returns(agendaItem);
        eventLocationRepository.FindActiveToBeAnnouncedAsync(session.EventId, Arg.Any<CancellationToken>())
            .Returns((EventLocation?)null);
        eventLocationRepository.AddAsync(Arg.Any<EventLocation>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                EventLocation placement = call.ArgAt<EventLocation>(0);
                stagedPlacements.Add(placement);
                return placement;
            });
        agendaRepository.Create(agendaItem).Returns(_ =>
        {
            stagedCarriers.Add(agendaItem);
            return Task.FromException<EventSessionAgendaItem>(
                new InvalidOperationException("Forced carrier failure."));
        });
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<EventSessionAgendaItem>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                try
                {
                    return await call.ArgAt<Func<CancellationToken, Task<EventSessionAgendaItem>>>(0)(
                        call.ArgAt<CancellationToken>(1));
                }
                catch
                {
                    stagedPlacements.Clear();
                    stagedCarriers.Clear();
                    throw;
                }
            });
        var handler = new CreateEventSessionAgendaItemCommandHandler(
            agendaRepository,
            sessionRepository,
            locationRepository,
            CreateTenantContext(tenantId),
            unitOfWork,
            CreateAttachmentService(eventLocationRepository, tenantId, Guid.CreateVersion7()),
            mapper);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new CreateEventSessionAgendaItemCommand { AgendaItemDto = dto },
            CancellationToken.None));
        await Assert.That(stagedPlacements).IsEmpty();
        await Assert.That(stagedCarriers).IsEmpty();
    }

    [Test]
    public async Task CreateRejectsCrossTenantParentBeforeUnitOfWork()
    {
        Guid requestTenantId = Guid.CreateVersion7();
        EventSession crossTenantSession = CreateSession(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        var agendaRepository = Substitute.For<IEventSessionAgendaItemRepository>();
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var locationRepository = Substitute.For<ILocationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        CreateEventSessionAgendaItemDto dto = CreateDto(crossTenantSession.Id, locationId: null);

        sessionRepository.Exists(crossTenantSession.Id).Returns(true);
        sessionRepository.GetById(crossTenantSession.Id).Returns(crossTenantSession);
        mapper.Map<EventSessionAgendaItem>(dto).Returns(CreateAgendaItem(
            requestTenantId,
            crossTenantSession,
            locationId: null));
        var handler = new CreateEventSessionAgendaItemCommandHandler(
            agendaRepository,
            sessionRepository,
            locationRepository,
            CreateTenantContext(requestTenantId),
            unitOfWork,
            CreateAttachmentService(
                Substitute.For<IEventLocationRepository>(),
                requestTenantId,
                Guid.CreateVersion7()),
            mapper);

        var result = await handler.Handle(
            new CreateEventSessionAgendaItemCommand { AgendaItemDto = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<EventSessionAgendaItem>>>(),
            Arg.Any<CancellationToken>());
    }

    private static EventLocationAttachmentService CreateAttachmentService(
        IEventLocationRepository repository,
        Guid tenantId,
        Guid actorUserId)
    {
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(actorUserId);
        return new(repository, userContext, CreateTenantContext(tenantId), TimeProvider.System);
    }

    private static ITenantContext CreateTenantContext(Guid tenantId)
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        return tenantContext;
    }

    private static EventSession CreateSession(Guid tenantId, Guid eventId, Guid sessionId) => new()
    {
        Id = sessionId,
        EventId = eventId,
        Event = null!,
        TenantId = tenantId,
        Tenant = null!
    };

    private static EventSessionAgendaItem CreateAgendaItem(
        Guid tenantId,
        EventSession session,
        Guid? locationId) => new()
        {
            Id = Guid.CreateVersion7(),
            EventSessionId = session.Id,
            EventSession = session,
            TenantId = tenantId,
            Tenant = null!,
            Title = "Agenda item",
            StartTime = new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 7, 19, 11, 0, 0, TimeSpan.Zero),
            LocationId = locationId
        };

    private static CreateEventSessionAgendaItemDto CreateDto(Guid sessionId, Guid? locationId) => new()
    {
        EventSessionId = sessionId,
        Title = "Agenda item",
        StartTime = new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero),
        EndTime = new DateTimeOffset(2026, 7, 19, 11, 0, 0, TimeSpan.Zero),
        LocationId = locationId
    };

    private static UpdateEventSessionAgendaItemDto UpdateDto(
        Guid id,
        Guid sessionId,
        Guid? locationId) => new()
        {
            Id = id,
            EventSessionId = sessionId,
            Title = "Moved agenda item",
            StartTime = new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 7, 19, 13, 0, 0, TimeSpan.Zero),
            LocationId = locationId
        };
}
