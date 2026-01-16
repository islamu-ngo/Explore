using Explore.Application.DTOs.DidCustodyType;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.DidCustodyTypes.Requests.Queries
{
    public class GetDidCustodyTypeListRequest : IRequest<List<DidCustodyTypeListDto>>
    {
    }
}
