// ABOUTME: Authorized command for starting an actor-bound support-access session.
// ABOUTME: Carries target tenant, mode, duration, reason, and ticket metadata into the Application layer.

using Explore.Application.Authorization;
using Explore.Application.DTOs.SupportAccess;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.SupportAccess.Requests.Commands;

[AuthorizeResource(ResourceKinds.SupportAccessSession, AuthorizationActions.SupportAccessSessions.Start)]
public sealed class StartSupportAccessSessionCommand : IRequest<SupportAccessSessionCommandResponseDto>, ISecureRequest
{
    public Guid TargetTenantId { get; init; }
    public Guid? TargetTenantUserId { get; init; }
    public SupportAccessModeEnum Mode { get; init; } = SupportAccessModeEnum.ReadOnly;
    public int DurationMinutes { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public string ReasonText { get; init; } = string.Empty;
    public string? TicketReference { get; init; }

    string? ISecureRequest.ResourceId => TargetTenantId == Guid.Empty ? null : TargetTenantId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TargetTenantId.ToString("D"),
        ["mode"] = Mode.ToString()
    };
}
