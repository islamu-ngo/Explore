// ABOUTME: MediatR command for creating a new EventDay within a parent event.
// ABOUTME: Secured via AuthorizeResource for the event_day resource kind.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventDay;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventDays.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventDay, AuthorizationActions.Create)]
public class CreateEventDayCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventDayDto EventDayDto { get; set; }

    string? ISecureRequest.ResourceId => null;
}
