using System.Collections.Generic;
using Explore.Application.DTOs.Madhab;
using MediatR;

namespace Explore.Application.Features.Madhabs.Requests.Queries;

public class GetMadhabListRequest : IRequest<List<MadhabListDto>>
{
}
