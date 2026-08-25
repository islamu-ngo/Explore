// ABOUTME: Command for applying a token-authenticated email category unsubscribe.
// ABOUTME: Used by anonymous one-click and confirmation flows after token validation succeeds.

namespace Explore.Application.Features.EmailUnsubscribe.Requests.Commands;

using Explore.Application.Responses;
using MediatR;

public sealed record UnsubscribeFromEmailCategoryCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid TenantId { get; init; }
    public Guid UserId { get; init; }
    public required string Category { get; init; }
}
