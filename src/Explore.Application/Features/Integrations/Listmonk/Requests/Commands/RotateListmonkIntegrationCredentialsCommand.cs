// ABOUTME: Command for rotating tenant-scoped Listmonk API credentials.
// ABOUTME: Stores plaintext inputs only through encrypted SecretBinding metadata.

using Explore.Application.DTOs.Integrations;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Integrations.Listmonk.Requests.Commands;

public sealed class RotateListmonkIntegrationCredentialsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public RotateListmonkIntegrationCredentialsDto Dto { get; set; } = new();
}
