// ABOUTME: Requests initial administrator claim for an upstream-authenticated provider account.
// ABOUTME: Carries trusted adapter identity data and no browser-supplied bootstrap selector.

using Explore.Application.Authentication;
using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

[AuthorizeConfiguredAdministratorClaim]
public sealed record ClaimConfiguredInstanceAdministratorCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required ProviderAccountKey AuthenticatedAccount { get; init; }
    public Guid UserId { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool? EmailVerified { get; init; }
}
