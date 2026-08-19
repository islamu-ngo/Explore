// ABOUTME: MediatR command for route-ID EventDay PATCH updates.
// ABOUTME: Carries If-Match concurrency stamp and grouped update payload.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventDay;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventDays.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventDay, AuthorizationActions.Update)]
public class UpdateEventDayCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventDayId { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
    public required UpdateEventDayDto EventDayDto { get; set; }

    string? ISecureRequest.ResourceId => EventDayId.ToString();
}
