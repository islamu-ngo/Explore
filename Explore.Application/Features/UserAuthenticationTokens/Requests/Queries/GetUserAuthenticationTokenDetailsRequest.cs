// ABOUTME: MediatR query request for fetching a single authentication token by ID.
// ABOUTME: Returns UserAuthenticationTokenDto.
using Explore.Application.DTOs.UserAuthenticationToken;
using MediatR;

namespace Explore.Application.Features.UserAuthenticationTokens.Requests.Queries;

public class GetUserAuthenticationTokenDetailsRequest : IRequest<UserAuthenticationTokenDto>
{
    public Guid Id { get; set; }
}
