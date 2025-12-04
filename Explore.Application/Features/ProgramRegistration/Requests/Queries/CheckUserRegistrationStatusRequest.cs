using MediatR;

namespace Explore.Application.Features.ProgramRegistration.Requests.Queries
{
    public class CheckUserRegistrationStatusRequest : IRequest<bool>
    {
        public Guid UserId { get; set; }
        public Guid ProgramId { get; set; }
    }
}