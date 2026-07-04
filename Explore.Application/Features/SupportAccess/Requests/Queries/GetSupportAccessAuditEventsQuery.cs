// ABOUTME: Authorized query for bounded audit events attached to a support-access session.
// ABOUTME: Carries target tenant context so authorization can enforce tenant-audit visibility.

using Explore.Application.Authorization;
using Explore.Application.DTOs.SupportAccess;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.SupportAccess.Requests.Queries;

[AuthorizeResource(ResourceKinds.SupportAccessSession, AuthorizationActions.SupportAccessSessions.ViewAudit)]
public sealed class GetSupportAccessAuditEventsQuery : IRequest<PaginatedResult<SupportAccessAuditEventDto>>, ISecureRequest
{
    public Guid TargetTenantId { get; init; }
    public Guid SessionId { get; init; }
    public int Limit { get; init; } = 100;

    string? ISecureRequest.ResourceId => SessionId == Guid.Empty ? null : SessionId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TargetTenantId.ToString("D"),
        ["sessionId"] = SessionId.ToString("D")
    };
}
