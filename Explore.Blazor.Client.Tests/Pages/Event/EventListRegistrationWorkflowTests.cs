// ABOUTME: Unit tests for EventList page-local registration workflow helpers.
// ABOUTME: Verifies session selection, registration lookup, waitlist parsing, and DTO construction stay out of the Razor component.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Pages.Events;

namespace Explore.Blazor.Client.Tests.Pages.Event;

public sealed class EventListRegistrationWorkflowTests
{
    [Test]
    public async Task BuildRegistrationLookup_IndexesOnlyRegistrationsWithEventAndRegistrationIds()
    {
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        var lookup = EventListRegistrationWorkflow.BuildRegistrationLookup(
        [
            new EventRegistrationListDto { EventId = eventId, Id = registrationId },
            new EventRegistrationListDto { EventId = Guid.NewGuid() },
            new EventRegistrationListDto { Id = Guid.NewGuid() }
        ]);

        await Assert.That(lookup.RegisteredEventIds).IsEquivalentTo([eventId]);
        await Assert.That(lookup.RegistrationIdByEventId[eventId]).IsEqualTo(registrationId);
    }

    [Test]
    public async Task ToggleAllSessions_SelectsAndClearsOnlySessionsWithIds()
    {
        var firstSessionId = Guid.NewGuid();
        var secondSessionId = Guid.NewGuid();
        ICollection<EventSessionListDto> sessions =
        [
            new EventSessionListDto { Id = firstSessionId },
            new EventSessionListDto { Id = null },
            new EventSessionListDto { Id = secondSessionId }
        ];

        var selected = EventListRegistrationWorkflow.ToggleAllSessions(sessions, new HashSet<Guid>());
        await Assert.That(selected).IsEquivalentTo([firstSessionId, secondSessionId]);

        selected = EventListRegistrationWorkflow.ToggleAllSessions(sessions, selected);
        await Assert.That(selected).IsEmpty();
    }

    [Test]
    public async Task AreAllSessionsSelected_WhenSelectionIdsDoNotMatchAvailableSessions_ReturnsFalse()
    {
        var availableSessionId = Guid.NewGuid();
        var unavailableSessionId = Guid.NewGuid();
        ICollection<EventSessionListDto> sessions =
        [
            new EventSessionListDto { Id = availableSessionId }
        ];

        var allSelected = EventListRegistrationWorkflow.AreAllSessionsSelected(
            sessions,
            new HashSet<Guid> { unavailableSessionId });

        await Assert.That(allSelected).IsFalse();
    }

    [Test]
    public async Task ToggleSession_ReturnsNewSelectionWithoutMutatingOriginal()
    {
        var existingSessionId = Guid.NewGuid();
        var addedSessionId = Guid.NewGuid();
        var originalSelection = new HashSet<Guid> { existingSessionId };

        var added = EventListRegistrationWorkflow.ToggleSession(originalSelection, addedSessionId);
        var removed = EventListRegistrationWorkflow.ToggleSession(originalSelection, existingSessionId);

        await Assert.That(originalSelection).IsEquivalentTo([existingSessionId]);
        await Assert.That(added).IsEquivalentTo([existingSessionId, addedSessionId]);
        await Assert.That(removed).IsEmpty();
    }

    [Test]
    public async Task BuildSessionRegistrationRequest_IncludesSessionScopeAndConsentDetails()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var request = EventListRegistrationWorkflow.BuildSessionRegistrationRequest(
            eventId,
            userId,
            [sessionId],
            shareEmailWithOrganizer: true,
            organizerName: " Community Organizer ");

        await Assert.That(request.EventId).IsEqualTo(eventId);
        await Assert.That(request.UserId).IsEqualTo(userId);
        await Assert.That(request.RegistrationScopeId).IsEqualTo(3);
        await Assert.That(request.SelectedSessionIds).IsEquivalentTo([sessionId]);
        await Assert.That(request.ShareEmailWithOrganizer).IsTrue();
        await Assert.That(request.ConsentTextAcknowledged).Contains("Community Organizer");
        await Assert.That(request.ConsentUiVersion).IsEqualTo("v1");
    }

    [Test]
    public async Task BuildRegistrationRequest_WhenWholeEventPolicyAndAllSessionsSelected_UsesEventScope()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var firstSessionId = Guid.NewGuid();
        var secondSessionId = Guid.NewGuid();
        ICollection<EventSessionListDto> sessions =
        [
            new EventSessionListDto { Id = firstSessionId },
            new EventSessionListDto { Id = secondSessionId }
        ];

        var request = EventListRegistrationWorkflow.BuildRegistrationRequest(
            eventId,
            userId,
            sessions,
            [firstSessionId, secondSessionId],
            registrationPolicyId: 1,
            shareEmailWithOrganizer: true,
            organizerName: "ISLAMU");

        await Assert.That(request.EventId).IsEqualTo(eventId);
        await Assert.That(request.UserId).IsEqualTo(userId);
        await Assert.That(request.RegistrationScopeId).IsEqualTo(1);
        await Assert.That(request.SelectedSessionIds).IsNull();
        await Assert.That(request.SelectedEventDayId).IsNull();
        await Assert.That(request.ShareEmailWithOrganizer).IsTrue();
        await Assert.That(request.ConsentTextAcknowledged).Contains("ISLAMU");
        await Assert.That(request.ConsentUiVersion).IsEqualTo("v1");
    }

    [Test]
    public async Task BuildRegistrationRequest_WhenSessionSelectionPolicy_UsesSelectedSessionScope()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var selectedSessionId = Guid.NewGuid();

        var request = EventListRegistrationWorkflow.BuildRegistrationRequest(
            eventId,
            userId,
            availableSessions: null,
            [selectedSessionId],
            registrationPolicyId: 3,
            shareEmailWithOrganizer: false,
            organizerName: "ISLAMU");

        await Assert.That(request.EventId).IsEqualTo(eventId);
        await Assert.That(request.UserId).IsEqualTo(userId);
        await Assert.That(request.RegistrationScopeId).IsEqualTo(3);
        await Assert.That(request.SelectedSessionIds).IsEquivalentTo([selectedSessionId]);
        await Assert.That(request.ShareEmailWithOrganizer).IsFalse();
    }

    [Test]
    public async Task IsWaitlistResponse_DetectsWaitlistMessagesCaseInsensitively()
    {
        await Assert.That(EventListRegistrationWorkflow.IsWaitlistResponse("Added to WAITLIST")).IsTrue();
        await Assert.That(EventListRegistrationWorkflow.IsWaitlistResponse("Registered successfully")).IsFalse();
        await Assert.That(EventListRegistrationWorkflow.IsWaitlistResponse(null)).IsFalse();
    }
}
