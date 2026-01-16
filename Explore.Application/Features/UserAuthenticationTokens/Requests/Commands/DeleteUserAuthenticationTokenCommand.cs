using MediatR;

namespace Explore.Application.Features.UserAuthenticationTokens.Requests.Commands
{
    public class DeleteUserAuthenticationTokenCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
