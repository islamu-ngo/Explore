using System.Collections.Generic;
using Explore.Application.DTOs.UserExternalLogin;
using MediatR;

namespace Explore.Application.Features.UserExternalLogins.Requests.Queries;

public class GetUserExternalLoginListRequest : IRequest<List<UserExternalLoginListDto>>
{
}
