// ABOUTME: Command to update tenant-scoped non-secret Listmonk integration settings.
// ABOUTME: Credentials are intentionally excluded and rotated through encrypted secret bindings.

using Explore.Application.DTOs.Integrations;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Integrations.Listmonk.Requests.Commands;

public sealed record UpdateListmonkIntegrationSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public UpdateListmonkIntegrationSettingsDto Dto { get; init; } = new();
}
