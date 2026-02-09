using System.Collections.Generic;
using Explore.Application.DTOs.VisibilityType;
using MediatR;

namespace Explore.Application.Features.VisibilityTypes.Requests.Queries;

public class GetVisibilityTypeListRequest : IRequest<List<VisibilityTypeListDto>>
{
}
