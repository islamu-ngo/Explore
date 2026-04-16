// ABOUTME: MediatR command for updating an existing EventDay.
// ABOUTME: Secured via AuthorizeResource for the event_day resource kind.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventDay;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventDays.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventDay, AuthorizationActions.Update)]
public class UpdateEventDayCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventDayDto EventDayDto { get; set; }

    string? ISecureRequest.ResourceId => EventDayDto.Id.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
