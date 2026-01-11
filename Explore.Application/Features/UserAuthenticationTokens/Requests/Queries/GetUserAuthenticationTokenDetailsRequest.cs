using MediatR;
using Explore.Application.DTOs.UserAuthenticationToken;

namespace Explore.Application.Features.UserAuthenticationTokens.Requests.Queries
{
    public class GetUserAuthenticationTokenDetailsRequest : IRequest<UserAuthenticationTokenDto>
    {
        public Guid Id { get; set; }
    }
}
