using MediatR;
using Explore.Application.DTOs.UserExternalLogin;
using System.Collections.Generic;

namespace Explore.Application.Features.UserExternalLogins.Requests.Queries
{
    public class GetUserExternalLoginListRequest : IRequest<List<UserExternalLoginListDto>>
    {
    }
}
