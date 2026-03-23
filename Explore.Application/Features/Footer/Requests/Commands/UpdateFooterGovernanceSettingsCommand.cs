// ABOUTME: Command to update instance-level footer governance settings (lock flags).
// ABOUTME: Instance-admin only; adjusts which footer settings tenants can override.

using Explore.Application.DTOs.Footer;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

public class UpdateFooterGovernanceSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required FooterGovernanceSettingsDto Settings { get; set; }
}
