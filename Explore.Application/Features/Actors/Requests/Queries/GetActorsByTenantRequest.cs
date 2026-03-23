// ABOUTME: MediatR query request for fetching all actors in a tenant.
// ABOUTME: Returns IEnumerable<ActorDto>.
using Explore.Application.DTOs.Actor;
using MediatR;

namespace Explore.Application.Features.Actors.Requests.Queries;

public class GetActorsByTenantRequest : IRequest<List<ActorListDto>>
{
    public Guid TenantId { get; set; }
}
