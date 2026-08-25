// ABOUTME: MediatR query for checking whether a user account exists.
// ABOUTME: Returns bool.
using MediatR;

namespace Explore.Application.Features.Users.Requests.Queries;

public sealed record CheckUserExistsQuery : IRequest<bool>
{
    public required string Email { get; init; } = string.Empty;
}
