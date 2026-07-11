// ABOUTME: Authorized query for bounded support-access session history by target tenant.
// ABOUTME: Tenant predicate is explicit so audit/history reads cannot degrade into cross-tenant listing.

using Explore.Application.Authorization;
using Explore.Application.DTOs.SupportAccess;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.SupportAccess.Requests.Queries;

[AuthorizeResource(ResourceKinds.SupportAccessSession, AuthorizationActions.SupportAccessSessions.List)]
public sealed class ListSupportAccessSessionsQuery : IRequest<PaginatedResult<SupportAccessSessionDto>>, ISecureRequest
{
    public Guid TargetTenantId { get; init; }
    public int Limit { get; init; } = 100;

    string? ISecureRequest.ResourceId => TargetTenantId == Guid.Empty ? null : TargetTenantId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TargetTenantId.ToString("D")
    };
}
