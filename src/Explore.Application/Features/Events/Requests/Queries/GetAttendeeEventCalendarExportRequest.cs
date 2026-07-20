// ABOUTME: Authenticated query for a registration-scoped attendee calendar export.
// ABOUTME: Uses a distinct response contract so exact location data cannot enter the public path.

using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

public sealed record GetAttendeeEventCalendarExportRequest(Guid EventId)
    : IRequest<AttendeeEventCalendarExportDto?>;
