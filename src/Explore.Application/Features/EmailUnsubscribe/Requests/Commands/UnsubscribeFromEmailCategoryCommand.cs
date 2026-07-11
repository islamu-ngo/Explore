// ABOUTME: Command for applying a token-authenticated email category unsubscribe.
// ABOUTME: Used by anonymous one-click and confirmation flows after token validation succeeds.

namespace Explore.Application.Features.EmailUnsubscribe.Requests.Commands;

using Explore.Application.Responses;
using MediatR;

public sealed class UnsubscribeFromEmailCategoryCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public required string Category { get; set; }
}
