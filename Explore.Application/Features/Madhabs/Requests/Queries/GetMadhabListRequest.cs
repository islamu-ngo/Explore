using Explore.Application.DTOs.Madhab;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.Madhabs.Requests.Queries
{
    public class GetMadhabListRequest : IRequest<List<MadhabListDto>>
    {
    }
}
