// ABOUTME: Query to retrieve instance-level footer governance settings (lock flags).
// ABOUTME: Instance-admin only; used in the instance settings admin UI.

using Explore.Application.DTOs.Footer;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Queries;

public record GetFooterGovernanceSettingsQuery : IRequest<FooterGovernanceSettingsDto>;
