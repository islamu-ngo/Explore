// ABOUTME: MediatR query request to determine the appropriate tenant to redirect a user to from root.
// ABOUTME: Returns UserTenantRedirectionDto.

using System;
using Explore.Application.DTOs.User;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Queries;

public class ResolveUserTenantRedirectionRequest : IRequest<UserTenantRedirectionDto>
{
    public Guid UserId { get; set; }
}
