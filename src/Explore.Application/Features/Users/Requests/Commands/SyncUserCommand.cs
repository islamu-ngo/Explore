// ABOUTME: MediatR command for synchronizing a user from an external identity provider.
// ABOUTME: Carries trusted adapter identity and profile data from the validated principal.
using Explore.Application.Authentication;
using Explore.Application.DTOs.User;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Commands;

public sealed record SyncUserCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required ProviderAccountKey AccountKey { get; init; }
    public required UserDto UserDto { get; init; }
}
