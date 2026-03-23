// ABOUTME: MediatR query request for fetching all registration modes.
// ABOUTME: Returns IEnumerable<RegistrationModeDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.RegistrationMode;
using MediatR;

namespace Explore.Application.Features.RegistrationModes.Requests.Queries;

public class GetRegistrationModeListRequest : IRequest<List<RegistrationModeListDto>>
{
}
