using System.Collections.Generic;
using Explore.Application.DTOs.UserAuthenticationToken;
using MediatR;

namespace Explore.Application.Features.UserAuthenticationTokens.Requests.Queries;

public class GetUserAuthenticationTokenListRequest : IRequest<List<UserAuthenticationTokenListDto>>
{
}
