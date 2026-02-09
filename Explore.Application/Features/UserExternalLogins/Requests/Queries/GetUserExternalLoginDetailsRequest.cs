using Explore.Application.DTOs.UserExternalLogin;
using MediatR;

namespace Explore.Application.Features.UserExternalLogins.Requests.Queries;

public class GetUserExternalLoginDetailsRequest : IRequest<UserExternalLoginDto>
{
    public Guid Id { get; set; }
}
