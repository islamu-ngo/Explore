// ABOUTME: Command to partially update instance-level footer governance lock flags.
// ABOUTME: Instance-admin only; carries a dedicated presence-aware write contract.

using Explore.Application.DTOs.Footer;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

public sealed record UpdateFooterGovernanceSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; init; }
    public required PatchFooterGovernanceSettingsDto Patch { get; init; }
}
