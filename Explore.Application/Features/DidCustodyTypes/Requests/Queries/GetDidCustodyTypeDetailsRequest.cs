using Explore.Application.DTOs.DidCustodyType;
using MediatR;

namespace Explore.Application.Features.DidCustodyTypes.Requests.Queries;

public class GetDidCustodyTypeDetailsRequest : IRequest<DidCustodyTypeDto>
{
    public int Id { get; set; }
}
