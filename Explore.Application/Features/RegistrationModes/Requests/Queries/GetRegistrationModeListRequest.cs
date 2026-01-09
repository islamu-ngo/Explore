using Explore.Application.DTOs.RegistrationMode;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.RegistrationModes.Requests.Queries
{
    public class GetRegistrationModeListRequest : IRequest<List<RegistrationModeListDto>>
    {
    }
}
