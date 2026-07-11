// ABOUTME: MediatR query request for fetching a single indexed DID by ID.
// ABOUTME: Returns IndexedDidDto.
using Explore.Application.DTOs.IndexedDid;
using MediatR;

namespace Explore.Application.Features.IndexedDids.Requests.Queries;

public class GetIndexedDidDetailsRequest : IRequest<IndexedDidDto?>
{
    public required string Did { get; set; }
}
