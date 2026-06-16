// ABOUTME: Unit coverage for dedicated program item form mapping and normalization.
// ABOUTME: Protects create/edit session composer transformations during Phase 4 UI decomposition.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models.EventSessions;
using Explore.Blazor.Client.Pages.Events.Sessions;

namespace Explore.Blazor.Client.Tests.Pages.Event;

public sealed class EventSessionFormModelMapperTests
{
    [Test]
    public async Task ApplyCreateContext_UsesDefaultsAndSelectorOptions()
    {
        var session = new CreateEventSessionRequest();
        var locationId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var context = new EventSessionCreateContextDto
        {
            Defaults = new EventSessionCreateDefaultsDto
            {
                RegistrationModeId = 2,
                SessionDate = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero),
                StartTime = "09:15:00",
                EndTime = "10:45:00"
            },
            Locations = [new EventSessionCreateLocationOptionDto { Id = locationId, FullName = "Main Hall" }],
            SessionGroups = [new EventSessionCreateGroupOptionDto { Id = groupId, Name = "Main track" }]
        };

        var state = EventSessionFormModelMapper.ApplyCreateContext(session, context, fallbackRegistrationModeId: 1);

        await Assert.That(session.RegistrationModeId).IsEqualTo(2);
        await Assert.That(state.Locations.Single().Id).IsEqualTo(locationId);
        await Assert.That(state.SessionGroups.Single().Id).IsEqualTo(groupId);
        await Assert.That(state.SessionDate).IsEqualTo(new DateTime(2026, 5, 20));
        await Assert.That(state.StartTime).IsEqualTo(new TimeSpan(9, 15, 0));
        await Assert.That(state.EndTime).IsEqualTo(new TimeSpan(10, 45, 0));
    }

    [Test]
    public async Task TryPrepareCreateRequest_NormalizesScheduleAndCapacity()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var session = new CreateEventSessionRequest
        {
            Title = "  Opening talk  ",
            MaxAudienceAttendees = 0
        };

        var prepared = EventSessionFormModelMapper.TryPrepareCreateRequest(
            session,
            eventId,
            tenantId,
            new DateTime(2026, 6, 1),
            new TimeSpan(9, 30, 0),
            new TimeSpan(10, 30, 0),
            out var validationError);

        await Assert.That(prepared).IsTrue();
        await Assert.That(validationError).IsNull();
        await Assert.That(session.EventId).IsEqualTo(eventId);
        await Assert.That(session.TenantId).IsEqualTo(tenantId);
        await Assert.That(session.Title).IsEqualTo("Opening talk");
        await Assert.That(session.MaxAudienceAttendees).IsNull();
        await Assert.That(session.StartTime).IsEqualTo(DateTimeHelper.ConvertLocalToUtc(new DateTime(2026, 6, 1, 9, 30, 0)));
        await Assert.That(session.EndTime).IsEqualTo(DateTimeHelper.ConvertLocalToUtc(new DateTime(2026, 6, 1, 10, 30, 0)));
    }

    [Test]
    public async Task PopulateUpdateRequest_MapsEditableSessionFieldsAndPrimaryGroup()
    {
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var primaryGroupId = Guid.NewGuid();
        var secondaryGroupId = Guid.NewGuid();
        var request = new UpdateEventSessionRequest();
        var source = new EventSessionDto
        {
            Id = sessionId,
            EventId = eventId,
            Title = "Workshop",
            Description = "Practical work",
            Slug = "workshop",
            MaxAudienceAttendees = 30,
            RegistrationModeId = 2,
            StartTime = new DateTimeOffset(2026, 7, 3, 14, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 7, 3, 15, 30, 0, TimeSpan.Zero),
            IslamicAspect = new EventSessionIslamicAspectDto { ReferencePrayer = (PrayerTime)2, RequiresWudu = true },
            SessionGroups =
            [
                new SessionGroups { EventSessionGroupId = secondaryGroupId, IsPrimary = false, SortOrder = 1 },
                new SessionGroups { EventSessionGroupId = primaryGroupId, IsPrimary = true, SortOrder = 2 }
            ]
        };

        var state = EventSessionFormModelMapper.PopulateUpdateRequest(request, source, eventId);

        await Assert.That(request.Id).IsEqualTo(sessionId);
        await Assert.That(request.EventId).IsEqualTo(eventId);
        await Assert.That(request.Title).IsEqualTo("Workshop");
        await Assert.That(request.Description).IsEqualTo("Practical work");
        await Assert.That(request.IslamicAspect).IsNotNull();
        await Assert.That(request.IslamicAspect!.ReferencePrayer).IsEqualTo((PrayerTime)2);
        await Assert.That(request.IslamicAspect.RequiresWudu).IsTrue();
        await Assert.That(state.PrimarySessionGroupId).IsEqualTo(primaryGroupId);
        await Assert.That(state.SessionDate).IsEqualTo(DateTimeHelper.ConvertUtcToLocal(source.StartTime)!.Value.Date);
        await Assert.That(state.StartTime).IsEqualTo(DateTimeHelper.ConvertUtcToLocal(source.StartTime)!.Value.TimeOfDay);
        await Assert.That(state.EndTime).IsEqualTo(DateTimeHelper.ConvertUtcToLocal(source.EndTime)!.Value.TimeOfDay);
    }

    [Test]
    public async Task TryPrepareUpdateRequest_RejectsMismatchedSessionContext()
    {
        var request = new UpdateEventSessionRequest
        {
            Id = Guid.NewGuid(),
            Title = "Workshop"
        };

        var prepared = EventSessionFormModelMapper.TryPrepareUpdateRequest(
            request,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 6, 1),
            new TimeSpan(9, 0, 0),
            new TimeSpan(10, 0, 0),
            out var validationError);

        await Assert.That(prepared).IsFalse();
        await Assert.That(validationError).IsEqualTo("The session context is invalid. Return to the event draft and try again.");
    }
}
