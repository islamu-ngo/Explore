using Explore.Application.DTOs.RegistrationMode;
using MediatR;

namespace Explore.Application.Features.RegistrationModes.Requests.Queries;

public class GetRegistrationModeDetailsRequest : IRequest<RegistrationModeDto>
{
    public int Id { get; set; }
}
