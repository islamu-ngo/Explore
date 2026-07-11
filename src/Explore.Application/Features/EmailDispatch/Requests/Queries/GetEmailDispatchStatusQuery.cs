// ABOUTME: Query for operator-safe Basic Dispatch Mode status rows scoped to a tenant.
// ABOUTME: Returns sanitized dispatch lifecycle fields without exposing email content or recipients.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EmailDispatch;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Queries;

[AuthorizeResource(ResourceKinds.EmailDispatch, AuthorizationActions.EmailDispatches.View)]
public sealed class GetEmailDispatchStatusQuery : IRequest<BaseCommandResponse<IReadOnlyList<EmailDispatchStatusDto>>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public int Limit { get; set; } = 50;

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["authorizationScope"] = "collection"
        };
}
