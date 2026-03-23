// ABOUTME: Command to delete a single footer link from a group.
// ABOUTME: Validates the link's parent group belongs to the current tenant.

using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

public class DeleteFooterLinkCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public Guid LinkId { get; set; }
}
