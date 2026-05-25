// ABOUTME: Query for operator-safe Basic Dispatch Mode status rows scoped to a tenant.
// ABOUTME: Returns sanitized dispatch lifecycle fields without exposing email content or recipients.

using Explore.Application.DTOs.EmailDispatch;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Queries;

public sealed class GetEmailDispatchStatusQuery : IRequest<BaseCommandResponse<IReadOnlyList<EmailDispatchStatusDto>>>
{
    public Guid TenantId { get; set; }
    public int Limit { get; set; } = 50;
}
