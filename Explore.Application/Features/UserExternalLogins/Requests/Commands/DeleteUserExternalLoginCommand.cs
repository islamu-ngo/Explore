using MediatR;

namespace Explore.Application.Features.UserExternalLogins.Requests.Commands
{
    public class DeleteUserExternalLoginCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
