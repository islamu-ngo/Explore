using MediatR;
using Explore.Application.DTOs.Actor;

namespace Explore.Application.Features.Actors.Requests.Queries;

public class GetActorsByTenantRequest : IRequest<List<ActorListDto>>
{
    public Guid TenantId { get; set; }
}
