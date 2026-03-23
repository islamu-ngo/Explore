// ABOUTME: Command to update a footer link's label, URL, and display options.
// ABOUTME: Validates the link's parent group belongs to the current tenant.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

public class UpdateFooterLinkCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public Guid LinkId { get; set; }
    public required string Label { get; set; }
    public required string Url { get; set; }
    public bool OpenInNewTab { get; set; }
    public bool IsActive { get; set; }
}
