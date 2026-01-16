using MediatR;
using Explore.Application.DTOs.UserAuthenticationToken;
using System.Collections.Generic;

namespace Explore.Application.Features.UserAuthenticationTokens.Requests.Queries
{
    public class GetUserAuthenticationTokenListRequest : IRequest<List<UserAuthenticationTokenListDto>>
    {
    }
}
