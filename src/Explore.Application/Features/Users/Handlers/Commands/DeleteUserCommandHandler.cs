// ABOUTME: Handles global user deletion through the retained-authority-first privacy-erasure workflow.
// ABOUTME: Delegates atomic application mutation and replay to the purpose-specific erasure service.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.PrivacyErasure;
using Explore.Application.Features.Users.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.Users.Handlers.Commands;

public sealed class DeleteUserCommandHandler(
    IPrivacyErasureService erasureService)
    : IRequestHandler<DeleteUserCommand, PrivacyErasureStartDto>
{
    public Task<PrivacyErasureStartDto> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken) =>
        erasureService.EraseUserAsync(request.UserId, request.IntentId, cancellationToken);
}
