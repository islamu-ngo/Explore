// ABOUTME: MediatR command for removing a user external login record by ID.
// ABOUTME: Carries the target login record ID.
using MediatR;

namespace Explore.Application.Features.UserExternalLogins.Requests.Commands;

public class DeleteUserExternalLoginCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
