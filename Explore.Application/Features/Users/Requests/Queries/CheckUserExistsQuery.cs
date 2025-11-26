using MediatR;

namespace Explore.Application.Features.Users.Requests.Queries
{
    public class CheckUserExistsQuery : IRequest<bool>
    {
        public string Email { get; set; } = string.Empty;
    }
}
