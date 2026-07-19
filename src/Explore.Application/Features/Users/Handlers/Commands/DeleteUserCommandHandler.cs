// ABOUTME: Handles global user deletion through the retained-authority-first privacy-erasure workflow.
// ABOUTME: Delegates atomic application mutation and replay to the purpose-specific erasure service.

using Explore.Application.Contracts.Services;
using Explore.Application.Features.Users.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.Users.Handlers.Commands;

public sealed class DeleteUserCommandHandler(
    IGlobalLocationPrivacyErasureService erasureService)
    : IRequestHandler<DeleteUserCommand, Unit>
{
    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        await erasureService.EraseUserAsync(request.UserId, cancellationToken);
        return Unit.Value;
    }
}
