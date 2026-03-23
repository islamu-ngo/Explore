// ABOUTME: Command to reorder footer link groups for the current tenant.
// ABOUTME: Accepts an ordered list of group IDs and updates their Order properties.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

public class ReorderFooterLinkGroupsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    /// <summary>Group IDs in the desired display order (first = 0).</summary>
    public required List<Guid> OrderedGroupIds { get; set; }
}
