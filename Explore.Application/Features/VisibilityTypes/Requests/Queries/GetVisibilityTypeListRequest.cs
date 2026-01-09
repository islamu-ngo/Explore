using Explore.Application.DTOs.VisibilityType;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.VisibilityTypes.Requests.Queries
{
    public class GetVisibilityTypeListRequest : IRequest<List<VisibilityTypeListDto>>
    {
    }
}
