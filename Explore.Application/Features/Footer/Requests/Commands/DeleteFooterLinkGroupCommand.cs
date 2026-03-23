// ABOUTME: Command to delete a footer link group and all its child links.
// ABOUTME: Validates group ownership before deletion.

using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

public class DeleteFooterLinkGroupCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
}
