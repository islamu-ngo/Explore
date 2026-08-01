// ABOUTME: Typed Studio attendee aggregate combining an order resource and its participant collection.
// ABOUTME: Keeps the public transport shape outside pure interface contract files for architecture hygiene.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services;

public sealed record StudioAttendeeOrder(
    HalResourceOfRegistrationOrderDto Order,
    HalResourceOfRegistrationOrderParticipantsDto Participants);
