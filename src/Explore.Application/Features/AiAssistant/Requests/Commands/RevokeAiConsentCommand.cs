// ABOUTME: MediatR command for revoking an active AI context disclosure consent grant.
// ABOUTME: Transitions the grant to Revoked status and triggers transcript hygiene via the domain service.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Commands;

public sealed class RevokeAiConsentCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required Guid GrantId { get; init; }
    public required Guid RevokedByUserId { get; init; }
}
