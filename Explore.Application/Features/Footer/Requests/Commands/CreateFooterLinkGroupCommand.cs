// ABOUTME: Command to create a new footer link group for the current tenant (or instance when tenantId is null).
// ABOUTME: Order is auto-assigned as max+1.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

public class CreateFooterLinkGroupCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required string Title { get; set; }
    /// <summary>Null = instance-default group (instance admin only).</summary>
    public Guid? TenantId { get; set; }
}
