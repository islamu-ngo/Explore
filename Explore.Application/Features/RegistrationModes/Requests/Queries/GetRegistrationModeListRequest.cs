using System.Collections.Generic;
using Explore.Application.DTOs.RegistrationMode;
using MediatR;

namespace Explore.Application.Features.RegistrationModes.Requests.Queries;

public class GetRegistrationModeListRequest : IRequest<List<RegistrationModeListDto>>
{
}
