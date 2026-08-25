// ABOUTME: MediatR query request for fetching all event registration policies.
// ABOUTME: Returns list of EventRegistrationPolicyListDto (Open, ApprovalRequired, InvitationOnly).

using Explore.Application.DTOs.EventRegistrationPolicy;
using MediatR;

namespace Explore.Application.Features.EventRegistrationPolicies.Requests.Queries;

public sealed record GetEventRegistrationPolicyListRequest : IRequest<List<EventRegistrationPolicyListDto>>
{
}
