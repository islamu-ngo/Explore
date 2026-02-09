using System.Collections.Generic;
using Explore.Application.DTOs.DidCustodyType;
using MediatR;

namespace Explore.Application.Features.DidCustodyTypes.Requests.Queries;

public class GetDidCustodyTypeListRequest : IRequest<List<DidCustodyTypeListDto>>
{
}
