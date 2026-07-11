// ABOUTME: Public query for retrieving an event's calendar export payload.
// ABOUTME: Returns only published, public events so calendar downloads cannot expose drafts.

using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

public sealed record GetEventCalendarExportRequest(Guid EventId) : IRequest<EventCalendarExportDto?>;
